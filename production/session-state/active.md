# Session State — Move & Learn

<!-- STATUS -->
Epic: Color Jump Implementation
Feature: Island1 Scene — Jerarquía + DifficultySelector
Task: Configurar hierarchy completo y conectar referencias en Inspector
<!-- /STATUS -->

## Current Task
Configurar la escena de ColorJump (Island1) desde cero en Unity.
El StickFigure ya funciona y se mueve. Las plataformas existen en escena.
El juego no arrancaba porque ColorJumpGameUDP.Start() no llama StartGame().
Solución elegida: agregar DifficultySelector con DifficultyPanel + GamePanel.

## Progress Checklist

### ColorJump (Island1)
- [x] Scripts escritos — ColorJumpGameUDP.cs, DifficultySelector.cs
- [x] Scripts escritos — SizeContourDisplay.cs, SizeSortGameUDP.cs
- [x] StickFigureUDP.cs — funciona, el stickman se mueve
- [x] Plataformas 3D en escena (LeftPlatform, RightPlatform visibles)
- [x] Texto "RED" visible — ColorWordText conectado
- [ ] PoseReceiver — verificar que está en escena con PoseReceiverUDP
- [ ] ColorJumpManager — Add Component AudioSource
- [ ] DifficultySelector — crear GameObject + conectar campos
- [ ] DifficultyPanel — crear con 3 botones dificultad + StartBtn
- [ ] GamePanel — mover UI actual adentro
- [ ] Conectar todos los campos en Inspector (plataformas, textos, audio)
- [ ] Conectar OnClick botones → DifficultySelector.SelectEasy/Medium/Hard/StartGame
- [ ] Probar end-to-end: DifficultyPanel aparece → START → juego arranca + countdown

### Mirror the Word (Island3) — PENDIENTE
- [x] MirrorWordGameUDP.cs — thresholds mejorados, HOLD IT!, 8s
- [x] Python fix — pose_landmarks
- [ ] Island3 escena construida
- [ ] UI: WordText, ScoreText, FeedbackText, CountdownText, HoldFillBar
- [ ] Sonidos conectados
- [ ] Jugable end-to-end

## Jerarquía ColorJump (lo que debe quedar)
```
Scene
├── Main Camera
├── PoseReceiver          [PoseReceiverUDP — Port 5052]
├── StickFigure           [StickFigureUDP]
├── ColorJumpManager      [ColorJumpGameUDP + AudioSource]
├── Platforms
│   ├── LeftPlatform      [Renderer]
│   └── RightPlatform     [Renderer]
├── DifficultySelector    [DifficultySelector]
└── Canvas
    ├── DifficultyPanel   (activo al inicio)
    │   ├── TitleText
    │   ├── EasyBtn
    │   ├── MediumBtn
    │   ├── HardBtn
    │   └── StartBtn
    └── GamePanel         (inactivo al inicio)
        ├── ColorWordText
        ├── ScoreText
        ├── FeedbackText
        └── CountdownText
```

## Key Decisions
- DifficultySelector es por escena (no global) — cada escena conecta su propio juego
- ColorJumpGameUDP NO auto-arranca — depende de DifficultySelector.StartGame()
- pose_landmarks (no pose_world_landmarks) — da traslación real en cámara
- StickFigure: joints = gris oscuro, bones = cyan, cabeza = midpoint orejas [7,8]

## Archivos de Script (sin cambios esta sesión)
- `Assets/Scripts/Minigames/ColorJumpGameUDP.cs`
- `Assets/Scripts/Minigames/DifficultySelector.cs`
- `Assets/Scripts/Minigames/SizeSortGameUDP.cs`
- `Assets/Scripts/Avatar/StickFigureUDP.cs`
- `Assets/Scripts/Avatar/SizeContourDisplay.cs`
