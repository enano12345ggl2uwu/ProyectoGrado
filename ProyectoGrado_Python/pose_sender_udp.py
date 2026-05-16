"""
pose_sender_udp.py
Envia los 33 landmarks de MediaPipe Pose por UDP a Unity (puerto 7777).
Con ventana de preview para ver camara y esqueleto.

Requisitos:
    pip install mediapipe==0.10.7 opencv-python

Uso:
    python pose_sender_udp.py                # selector visual si hay 2+ camaras
    python pose_sender_udp.py --camera 1     # usa indice 1 directo (sin selector)
    python pose_sender_udp.py --scan-max 10  # escanea hasta indice 9 (default 5)

Salir: presiona 'q' en ventana de preview, o Ctrl+C en consola.
"""

import argparse
import cv2
import mediapipe as mp
import socket
import json
import sys

UDP_IP = "127.0.0.1"
UDP_PORT = 7777
CAM_WIDTH = 640
CAM_HEIGHT = 480
CAM_FPS = 30
MIRROR = True
SHOW_PREVIEW = True

THUMB_W = 320
THUMB_H = 240
GRID_COLS = 2


def scan_cameras(max_index=5):
    """Escanea indices 0..max_index-1 y devuelve lista de camaras disponibles.
    Cada entrada: {'idx': int, 'frame': BGR ndarray, 'w': int, 'h': int}.
    """
    found = []
    print(f"[SCAN] Buscando camaras (indices 0..{max_index - 1})...")
    for idx in range(max_index):
        cap = cv2.VideoCapture(idx, cv2.CAP_DSHOW)
        if not cap.isOpened():
            cap.release()
            continue
        ret, frame = cap.read()
        if ret and frame is not None:
            w = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
            h = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
            found.append({"idx": idx, "frame": frame.copy(), "w": w, "h": h})
            print(f"  [OK] Camara {idx}: {w}x{h}")
        cap.release()
    return found


def _build_thumb(cam):
    """Devuelve un thumbnail etiquetado para una camara."""
    thumb = cv2.resize(cam["frame"], (THUMB_W, THUMB_H))
    label = f"[{cam['idx']}] Presiona {cam['idx']}"
    info = f"{cam['w']}x{cam['h']}"
    cv2.rectangle(thumb, (0, 0), (THUMB_W - 1, THUMB_H - 1), (0, 255, 0), 2)
    # Sombra para legibilidad
    cv2.putText(thumb, label, (11, 31), cv2.FONT_HERSHEY_SIMPLEX,
                0.7, (0, 0, 0), 3)
    cv2.putText(thumb, label, (10, 30), cv2.FONT_HERSHEY_SIMPLEX,
                0.7, (0, 255, 0), 2)
    cv2.putText(thumb, info, (11, 61), cv2.FONT_HERSHEY_SIMPLEX,
                0.55, (0, 0, 0), 3)
    cv2.putText(thumb, info, (10, 60), cv2.FONT_HERSHEY_SIMPLEX,
                0.55, (0, 255, 255), 1)
    return thumb


def select_camera_interactive(cameras):
    """Muestra grid con previews y devuelve el indice elegido (o None si cancela)."""
    if len(cameras) == 0:
        return None
    if len(cameras) == 1:
        idx = cameras[0]["idx"]
        print(f"[INFO] Solo hay una camara disponible (indice {idx}). Usando esa.")
        return idx

    thumbs = [_build_thumb(c) for c in cameras]
    # Rellena celdas vacias con negro si la ultima fila no esta completa
    while len(thumbs) % GRID_COLS != 0:
        thumbs.append(thumbs[0] * 0)

    # Compone grid: filas de GRID_COLS, apiladas verticalmente
    rows = []
    for i in range(0, len(thumbs), GRID_COLS):
        rows.append(cv2.hconcat(thumbs[i:i + GRID_COLS]))
    grid = cv2.vconcat(rows) if len(rows) > 1 else rows[0]

    print("[SELECT] Presiona el numero de la camara que quieres usar (q = salir).")
    valid_keys = {ord(str(c["idx"])): c["idx"] for c in cameras}
    cv2.imshow("Selector de Camara", grid)
    chosen = None
    while True:
        key = cv2.waitKey(0) & 0xFF
        if key == ord('q'):
            break
        if key in valid_keys:
            chosen = valid_keys[key]
            break
    cv2.destroyWindow("Selector de Camara")
    return chosen


def open_camera(idx):
    """Abre y configura una camara por indice. Devuelve cap (o None si falla)."""
    cap = cv2.VideoCapture(idx, cv2.CAP_DSHOW)
    if not cap.isOpened():
        cap.release()
        return None
    ret, _ = cap.read()
    if not ret:
        cap.release()
        return None
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, CAM_WIDTH)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, CAM_HEIGHT)
    cap.set(cv2.CAP_PROP_FPS, CAM_FPS)
    return cap


def resolve_camera(args):
    """Aplica --camera si vino, si no abre el selector. Devuelve cap o None."""
    if args.camera is not None:
        print(f"[INFO] Abriendo camara {args.camera} (flag --camera)...")
        cap = open_camera(args.camera)
        if cap is not None:
            print(f"[OK] Camara {args.camera} lista.")
            return cap
        print(f"[WARN] No se pudo abrir camara {args.camera}. Cayendo al selector.")

    cameras = scan_cameras(max_index=args.scan_max)
    if len(cameras) == 0:
        print("[ERROR] No se encontro ninguna camara. Verifica:")
        print("  1. Camara conectada")
        print("  2. Permisos de camara en Windows")
        print("  3. Otra app no la este usando (Zoom, Teams, OBS, etc)")
        return None

    chosen_idx = select_camera_interactive(cameras)
    if chosen_idx is None:
        print("[STOP] Seleccion cancelada por el usuario.")
        return None

    cap = open_camera(chosen_idx)
    if cap is None:
        print(f"[ERROR] No se pudo abrir camara {chosen_idx} despues de seleccionar.")
        return None
    print(f"[OK] Usando camara {chosen_idx}")
    return cap


def main():
    parser = argparse.ArgumentParser(
        description="Pose sender UDP con selector de camara."
    )
    parser.add_argument("--camera", type=int, default=None,
                        help="Indice de camara a usar (salta el selector).")
    parser.add_argument("--scan-max", type=int, default=5,
                        help="Maximo indice a escanear (default 5).")
    args = parser.parse_args()

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    print(f"[UDP] Enviando a {UDP_IP}:{UDP_PORT}")

    cap = resolve_camera(args)
    if cap is None:
        sock.close()
        sys.exit(1)

    mp_pose = mp.solutions.pose
    mp_draw = mp.solutions.drawing_utils
    pose = mp_pose.Pose(
        model_complexity=1,
        min_detection_confidence=0.5,
        min_tracking_confidence=0.5,
        smooth_landmarks=True
    )

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

            if results.pose_landmarks:
                landmarks = [
                    [lm.x, lm.y, lm.z]
                    for lm in results.pose_landmarks.landmark
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
