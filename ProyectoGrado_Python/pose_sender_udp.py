"""
pose_sender_udp.py
Envía los 33 landmarks de MediaPipe Pose por UDP a Unity (puerto 5052).
Formato JSON: {"detected": true, "landmarks": [[x,y,z], ...]}

Requisitos:
    pip install mediapipe==0.10.7 opencv-python

Uso:
    python pose_sender_udp.py
"""

import cv2
import mediapipe as mp
import socket
import json
import sys

# Config
UDP_IP = "127.0.0.1"
UDP_PORT = 5052
CAM_WIDTH = 640
CAM_HEIGHT = 480
CAM_FPS = 30
MIRROR = True  # Modo espejo

def main():
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    print(f"[UDP] Enviando a {UDP_IP}:{UDP_PORT}")

    mp_pose = mp.solutions.pose
    pose = mp_pose.Pose(
        model_complexity=1,
        min_detection_confidence=0.5,
        min_tracking_confidence=0.5,
        smooth_landmarks=True
    )

    cap = cv2.VideoCapture(0)
    if not cap.isOpened():
        print("[ERROR] No se pudo abrir la cámara")
        sys.exit(1)

    cap.set(cv2.CAP_PROP_FRAME_WIDTH, CAM_WIDTH)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, CAM_HEIGHT)
    cap.set(cv2.CAP_PROP_FPS, CAM_FPS)

    print("[OK] Cámara abierta. Presiona Ctrl+C para salir.")

    try:
        while True:
            ret, frame = cap.read()
            if not ret:
                continue

            if MIRROR:
                frame = cv2.flip(frame, 1)

            rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            results = pose.process(rgb)

            if results.pose_landmarks:
                landmarks = [
                    [lm.x, lm.y, lm.z]
                    for lm in results.pose_landmarks.landmark
                ]
                payload = {"detected": True, "landmarks": landmarks}
            else:
                payload = {"detected": False, "landmarks": [[0, 0, 0]] * 33}

            msg = json.dumps(payload)
            sock.sendto(msg.encode("utf-8"), (UDP_IP, UDP_PORT))

    except KeyboardInterrupt:
        print("\n[STOP] Cerrando...")
    finally:
        cap.release()
        sock.close()
        pose.close()

if __name__ == "__main__":
    main()
