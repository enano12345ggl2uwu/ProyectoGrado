"""
pose_sender_udp.py
Envia los 33 landmarks de MediaPipe Pose por UDP a Unity (puerto 5052).
Con ventana de preview para ver camara y esqueleto.

Requisitos:
    pip install mediapipe==0.10.7 opencv-python

Uso:
    python pose_sender_udp.py

Salir: presiona 'q' en ventana de preview, o Ctrl+C en consola.
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
MIRROR = True
SHOW_PREVIEW = True  # Poner False para ocultar ventana

def main():
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    print(f"[UDP] Enviando a {UDP_IP}:{UDP_PORT}")

    mp_pose = mp.solutions.pose
    mp_draw = mp.solutions.drawing_utils
    pose = mp_pose.Pose(
        model_complexity=1,
        min_detection_confidence=0.5,
        min_tracking_confidence=0.5,
        smooth_landmarks=True
    )

    # Probar varios indices de camara
    cap = None
    for idx in [0, 1, 2]:
        print(f"[INFO] Probando camara index {idx}...")
        test = cv2.VideoCapture(idx, cv2.CAP_DSHOW)
        if test.isOpened():
            ret, _ = test.read()
            if ret:
                cap = test
                print(f"[OK] Camara {idx} funciona")
                break
            else:
                test.release()
        else:
            test.release()

    if cap is None:
        print("[ERROR] No se encontro camara. Verifica:")
        print("  1. Camara conectada")
        print("  2. Permisos de camara en Windows")
        print("  3. Otra app no la este usando (Zoom, Teams, etc)")
        sys.exit(1)

    cap.set(cv2.CAP_PROP_FRAME_WIDTH, CAM_WIDTH)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, CAM_HEIGHT)
    cap.set(cv2.CAP_PROP_FPS, CAM_FPS)

    print("[OK] Camara lista. Presiona 'q' en ventana para salir.")

    try:
        while True:
            ret, frame = cap.read()
            if not ret:
                print("[WARN] Frame vacio")
                continue

            if MIRROR:
                frame = cv2.flip(frame, 1)

            rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            results = pose.process(rgb)

            if results.pose_world_landmarks:
                landmarks = [
                    [lm.x, lm.y, lm.z]
                    for lm in results.pose_world_landmarks.landmark
    ]
                payload = {"detected": True, "landmarks": landmarks}

                if SHOW_PREVIEW:
                    mp_draw.draw_landmarks(
                        frame,
                        results.pose_landmarks,
                        mp_pose.POSE_CONNECTIONS
                    )
            else:
                payload = {"detected": False, "landmarks": [[0, 0, 0]] * 33}

            msg = json.dumps(payload)
            sock.sendto(msg.encode("utf-8"), (UDP_IP, UDP_PORT))

            if SHOW_PREVIEW:
                cv2.putText(frame, "Presiona 'q' para salir", (10, 30),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 0), 2)
                cv2.imshow("Pose Sender - Preview", frame)
                if cv2.waitKey(1) & 0xFF == ord('q'):
                    break

    except KeyboardInterrupt:
        print("\n[STOP] Cerrando...")
    finally:
        cap.release()
        sock.close()
        pose.close()
        cv2.destroyAllWindows()

if __name__ == "__main__":
    main()