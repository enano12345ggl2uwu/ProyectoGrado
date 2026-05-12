# -*- mode: python ; coding: utf-8 -*-
from PyInstaller.utils.hooks import collect_all, collect_data_files

# Recolecta TODO de mediapipe (modelos .tflite, metadata, etc.)
mp_datas, mp_binaries, mp_hiddenimports = collect_all('mediapipe')

# Recolecta datos de opencv (haarcascades, etc.)
cv2_datas = collect_data_files('cv2')

a = Analysis(
    ['pose_sender_udp.py'],
    pathex=[],
    binaries=mp_binaries,
    datas=mp_datas + cv2_datas,
    hiddenimports=mp_hiddenimports + [
        'mediapipe.python.solutions.pose',
        'mediapipe.python.solutions.drawing_utils',
        'mediapipe.python.solutions.drawing_styles',
        'cv2',
    ],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
    optimize=0,
)
pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.datas,
    [],
    name='MoveAndLearn_Pose',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=False,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=True,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
)
