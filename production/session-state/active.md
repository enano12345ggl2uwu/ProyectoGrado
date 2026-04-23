# Session State — Move & Learn

<!-- STATUS -->
Epic: Multi-Minigame Build
Feature: SizeSort Scene + PoseCursor + BalloonPop
Task: Construir escena SizeSort desde 0 en Unity
<!-- /STATUS -->

## Current Task
Construir la escena SizeSort (Island2) desde cero en Unity.
ColorJump tiene la jerarquía documentada abajo — pendiente conectar refs en Inspector.
PoseCursor listo para agregar a cualquier menú (push-forward + dwell fallback).

## Progress Checklist

### ColorJump (Island1)
- [x] Scripts escritos — ColorJumpGameUDP.cs, DifficultySelector.cs
- [x] StickFigureUDP.cs — funciona, el stickman se mueve
- [x] Plataformas 3D en escena (LeftPlatform, RightPlatform visibles)
- [x] Texto "RED" visible — ColorWordText conectado
- [x] PoseReceiverUDP — fix puerto duplicado (enabled=false en Awake) + ReuseAddress
- [x] Puerto cambiado a 7777 (Inspector + Python)
- [ ] DifficultySelector GO — crear + conectar en Inspector
- [ ] DifficultyPanel — crear con 3 botones + StartBtn + OnClick conectados
- [ ] GamePanel — mover UI actual adentro (inactivo al inicio)
- [ ] Probar end-to-end: DifficultyPanel → START → juego arranca

### SizeSort (Island2) — EN PROGRESO
- [x] SizeSortGameUDP.cs — escrito
- [x] Jerarquía documentada (ver abajo)
- [ ] Escena creada en Unity (File > New Scene → "Island2" o "SizeSort")
- [ ] SizeSortManager GO + SizeSortGameUDP component
- [ ] Platform (Cube scale 5,0.1,3)
- [ ] ObjectsContainer GO vacío
- [ ] DifficultySelector GO + conectar sizeSortGame
- [ ] Canvas: DifficultyPanel (activo) + GamePanel (inactivo)
- [ ] Botones OnClick conectados
- [ ] Agregar a Build Settings
- [ ] Probar end-to-end

### BalloonPop — SCRIPTS LISTOS
- [x] BalloonPopGameUDP.cs — escrito (Island3 o escena propia)
- [x] Balloon.cs — helper individual (flotación + sway lateral)
- [x] DifficultySelector soporta balloonPopGame
- [x] MainMenuController tiene PlayBalloonPop()
- [ ] Escena BalloonPop construida en Unity
- [ ] Prefab de globo creado (Sphere + material + Balloon.cs)
- [ ] BalloonManager GO conectado en Inspector
- [ ] Probar end-to-end

### PoseCursor — SCRIPT LISTO
- [x] PoseCursor.cs — escrito (push-forward + dwell fallback)
- [ ] Probado en una escena (recomendado: MainMenu primero)
- [ ] Si Z muy ruidoso → subir pushVelocityThreshold o cambiar a Hybrid (dos manos)

### Mirror the Word (Island3) — PENDIENTE
- [x] MirrorWordGameUDP.cs — thresholds mejorados, HOLD IT!, 8s
- [ ] Escena Island3 construida
- [ ] UI: WordText, ScoreText, FeedbackText, CountdownText, HoldFillBar
- [ ] Jugable end-to-end

## Jerarquía SizeSort (construir esto)
```
Scene (Island2 / SizeSort)
├── Main Camera
├── PoseReceiver          [PoseReceiverUDP — Port 7777]
├── StickFigure           [StickFigureUDP]
├── SizeSortManager       [SizeSortGameUDP + AudioSource]
├── Platform              [Cube scale (5, 0.1, 3) pos (0,-0.5,0)]
├── ObjectsContainer      [vacío, contenedor de objetos a ordenar]
├── DifficultySelector    [DifficultySelector]
│     sizeSortGame → SizeSortManager
└── Canvas
    ├── DifficultyPanel   (activo al inicio = true)
    │   ├── TitleText     (TMP "SELECT DIFFICULTY")
    │   ├── EasyBtn       → SelectEasy()
    │   ├── MediumBtn     → SelectMedium()
    │   ├── HardBtn       → SelectHard()
    │   └── StartBtn      → StartGame()
    └── GamePanel         (activo al inicio = false)
        ├── InstructionText (TMP "Sort: small → large")
        ├── ScoreText       (TMP)
        ├── FeedbackText    (TMP)
        ├── CountdownText   (TMP)
        └── SortUIPanel     (3 slots visuales)
```

## Jerarquía ColorJump (referencia)
```
Scene (Island1 / ColorJump)
├── Main Camera
├── PoseReceiver          [PoseReceiverUDP — Port 7777]
├── StickFigure           [StickFigureUDP]
├── ColorJumpManager      [ColorJumpGameUDP + AudioSource]
├── Platforms
│   ├── LeftPlatform      [Renderer]
│   └── RightPlatform     [Renderer]
├── DifficultySelector    [DifficultySelector]
│     colorJumpGame → ColorJumpManager
└── Canvas
    ├── DifficultyPanel   (activo al inicio = true)
    │   ├── TitleText, EasyBtn, MediumBtn, HardBtn, StartBtn
    └── GamePanel         (activo al inicio = false)
        ├── ColorWordText, ScoreText, FeedbackText, CountdownText
```

## PoseCursor Setup (para cualquier escena)
```
Canvas
└── PoseCursor            ← último hijo del Canvas (siempre al frente)
    ├── CursorDot         ← Image circular 32x32, color amarillo
    └── DwellRing         ← Image Filled Radial 360, fillAmount=0

Inspector PoseCursor.cs:
  cursorRect     → PoseCursor GO
  dwellRingImage → DwellRing
  canvas         → Canvas padre
  handLandmark   → 16 (muñeca derecha)
  pushVelocityThreshold → 1.2 (bajar a 0.8 si no detecta bien)
  dwellTime      → 1.5
```

## Key Decisions
- Puerto UDP: **7777** (cambiado de 5052 por permisos Windows)
- PoseReceiverUDP: Singleton con `enabled=false` en Awake + `ReuseAddress=true`
- DifficultySelector: soporta ColorJump, MirrorWord, SizeSort, BalloonPop
- StickFigure: joints=gris oscuro, bones=cyan, cabeza=midpoint orejas [7,8]
- PoseCursor: push-forward primario, dwell 1.5s como fallback
- GameManager/MusicManager/PoseReceiver: todos tienen fix `enabled=false`

## Archivos Nuevos Esta Sesión
- `Assets/Scripts/UI/PoseCursor.cs` — cursor de mano + push-click
- `Assets/Scripts/Minigames/BalloonPopGameUDP.cs` — juego globos completo
- `Assets/Scripts/Minigames/Balloon.cs` — helper globo individual

## Archivos Modificados Esta Sesión
- `Assets/Scripts/Core/PoseReceiverUDP.cs` — enabled=false + ReuseAddress
- `Assets/Scripts/Core/GameManager.cs` — enabled=false en Singleton
- `Assets/Scripts/Core/MusicManager.cs` — enabled=false en Singleton
- `Assets/Scripts/Minigames/DifficultySelector.cs` — agrega sizeSortGame + balloonPopGame
- `Assets/Scripts/UI/MainMenuController.cs` — agrega balloonPopScene + PlayBalloonPop()
- `Assets/Scripts/Avatar/StickBoneConnector.cs` — summary añadido
- `Assets/Scripts/UI/Menumanager.cs` — summary legacy añadido

## Scripts Python
- Puerto: **7777** (actualizar en pose_sender_udp.py si no se hizo)
- Usar `pose_landmarks` (NO pose_world_landmarks) para coordenadas de cámara reales
