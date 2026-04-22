# Move & Learn — Implementation Guide

Guía completa para montar MainMenu, Calibration, ColorJump, Mirror World (Island3) y Size Sort en Unity 2021.3.17f1 LTS.

Esta guía sirve para ti o para otra IA asistiéndote con el editor de Unity.

---

## 🎯 Contexto crítico (léelo antes de empezar)

- **Engine**: Unity 2021.3.17f1 LTS, C#
- **Input**: Cámara + MediaPipe. El script `ProyectoGrado_Python/pose_sender_udp.py` envía 33 landmarks por UDP a `127.0.0.1:5052`
- **Python DEBE correr** antes de cualquier escena con pose (excepto MainMenu)
- **Convención del frame**: Python usa `cv2.flip(frame, 1)` con `MIRROR=True`. Todos los validators asumen esa convención — si la quitas, se rompen
- **Convención Y**: coords MediaPipe crudas (Y abajo = positivo). Los validators están escritos bajo esta convención
- **Landmarks clave**: 0=nariz, 11/12=hombros, 15/16=muñecas, 23/24=caderas, 25/26=rodillas
- **Singletons existentes** (NO crear otros): `PoseReceiverUDP.Instance`, `GameManager.Instance`

---

## 📦 Scripts creados (10 nuevos)

| Script | Carpeta | Rol |
|---|---|---|
| `ScreenShake.cs` | `Assets/Scripts/Core/` | Singleton — sacude la Main Camera |
| `MusicManager.cs` | `Assets/Scripts/Core/` | Música de fondo persistente |
| `CelebrationBurst.cs` | `Assets/Scripts/Core/` | Partículas + shake + flash al acertar |
| `MainMenuController.cs` | `Assets/Scripts/UI/` | Menú principal unificado |
| `ResultsScreen.cs` | `Assets/Scripts/UI/` | Pantalla final + guarda best score |
| `InstructionsScreen.cs` | `Assets/Scripts/UI/` | Panel "How to play" |
| `TutorialOverlay.cs` | `Assets/Scripts/UI/` | Overlay tutorial primera ronda |
| `CalibrationScreen.cs` | `Assets/Scripts/UI/` | Pantalla status cámara |
| `SizeContourDisplay.cs` | `Assets/Scripts/Avatar/` | Dibuja 2 rectángulos (target + live) |
| `SizeSortGameUDP.cs` | `Assets/Scripts/Minigames/` | Minijuego Size Sort |

**Scripts existentes modificados**:
- `MirrorWordGameUDP.EvaluateCorrect()` → dispara `CelebrationBurst`
- `ColorJumpGameUDP.EvaluateAnswer()` (rama correcta) → dispara `CelebrationBurst`
- `PoseSilhouette` usa `Unlit/Color` (URP-safe)
- `MirrorWord` One-Arm-Up acepta cualquier brazo levantado

---

## 🎬 Orden de implementación

### PASO 1 — Singletons globales en MainMenu (5 min)

En la escena **MainMenu** crea 3 GameObjects vacíos:

1. **`ScreenShake`** → añade `ScreenShake.cs`. Deja `target` vacío (auto-toma la Main Camera).
2. **`MusicManager`** → añade `MusicManager.cs`. Arrastra un `.mp3/.wav` al campo `musicClip`. Volume ~0.4.
3. **`CelebrationBurst`** → añade `CelebrationBurst.cs`. Opcional: arrastra un `ParticleSystem` prefab al campo `burstParticles`.

Los 3 son `DontDestroyOnLoad` → se mantienen entre escenas.

---

### PASO 2 — Build Settings (2 min)

`File → Build Settings → Add Open Scenes` en este orden exacto:

```
0 - MainMenu
1 - Calibration
2 - ColorJump
3 - Island3         (Mirror the Word)
4 - SizeSort
```

---

### PASO 3 — MainMenu scene (15 min)

**Borrar** los scripts antiguos: `MainMenu.cs` y `Menumanager.cs` (redundantes, los reemplaza `MainMenuController.cs`).

Canvas layout:

```
Canvas
├─ Title (TMP)                      "MOVE & LEARN"
├─ BestScoresPanel
│   ├─ bestColorText (TMP)
│   ├─ bestMirrorText (TMP)
│   └─ bestSizeText (TMP)
├─ PlayColorJumpButton  → OnClick: MainMenuController.PlayColorJump
├─ PlayMirrorButton     → OnClick: MainMenuController.PlayMirrorWorld
├─ PlaySizeButton       → OnClick: MainMenuController.PlaySizeSort
├─ CalibrationButton    → OnClick: MainMenuController.OpenCalibration
├─ InstructionsButton   → OnClick: MainMenuController.OpenInstructions
├─ ExitButton           → OnClick: MainMenuController.ExitGame
└─ InstructionsPanel (oculto, con InstructionsScreen.cs)
    ├─ titleText / bodyText (TMP)
    └─ BackButton → OnClick: MainMenuController.CloseInstructions
```

En `MainMenuController` verifica nombres de escena:
- `colorJumpScene = "ColorJump"`
- `mirrorWorldScene = "Island3"`
- `sizeSortScene = "SizeSort"`
- `calibrationScene = "Calibration"`

---

### PASO 4 — Calibration scene (10 min)

Escena nueva con:
- `PoseReceiverUDP` (GameObject con el script — singleton)
- `StickFigureUDP` (para ver que la cámara te copie)
- Main Camera + Light
- `CalibrationManager` GameObject con `CalibrationScreen.cs`
- Canvas: `statusText` (TMP arriba), `hintText` (TMP abajo), `BackButton → CalibrationScreen.BackToMenu()`

---

### PASO 5 — ColorJump scene (ya existe, añadir UI de cierre)

En el Canvas existente añade:

- **`ResultsPanel`** (oculto) con `ResultsScreen.cs`:
  - `minigameKey = "color"`
  - Botones Replay/Menu con OnClick → `ResultsScreen.ReplayScene()` / `BackToMenu()`
- **`DifficultyPanel`** (ya lo tienes, con `DifficultySelector.cs`)
- **Opcional**: `TutorialOverlay` panel oculto. En `ColorJumpGameUDP.Start()` agrega: `StartCoroutine(tutorial.ShowForSeconds("COLOR JUMP","Move left or right to the color word!",3f));`

**Para cerrar sesión tras N rondas**: en `ColorJumpGameUDP.GameLoop()` cuenta rondas, y al llegar al límite llama `results.Show(score, rounds); yield break;`.

---

### PASO 6 — Island3 / Mirror World scene (ya existe, añadir UI)

Misma receta que ColorJump:
- `ResultsPanel` con `minigameKey = "mirror"`
- `TutorialOverlay` con texto: *"Copy the pose you see on screen. Hold still!"*

---

### PASO 7 — SizeSort scene NUEVA (30 min)

1. Duplica `Island3.unity` → renómbrala `SizeSort.unity`
2. **Borra** `MirrorWordGameUDP` y `PoseSilhouette` de la escena
3. Crea GameObject vacío `SizeContour` en posición `(0, 2, 0)` con `SizeContourDisplay.cs`
4. Crea GameObject `SizeSortManager` con `SizeSortGameUDP.cs`
5. En el Inspector arrastra:
   - `contour` ← SizeContour
   - `wordText / scoreText / feedbackText / countdownText / holdFillBar` ← Canvas TMP (reutiliza los que tenía MirrorWord)
   - `audioSource / correctClip / wrongClip`
6. Añade **DifficultyPanel** con `DifficultySelector` → botones llaman `sizeSortGame.StartGame(0/1/2)`
7. Añade **ResultsPanel** con `ResultsScreen.cs`, `minigameKey = "size"`
8. Añade `SizeSort` a Build Settings

**Cómo funciona Size Sort**:
- Vocabulario: **TALL, SHORT, WIDE, NARROW, BIG, SMALL** (6 palabras)
- Mide **width** = distancia muñeca↔muñeca (lm 15↔16) / shoulderWidth
- Mide **height** = |nariz.y − midHip.y| / shoulderWidth
- El niño ajusta cuerpo hasta que el rectángulo vivo coincida con el target (±18%)
- HOLD 1.2s → ronda correcta

---

### PASO 8 — Visual juice (ya automático)

`CelebrationBurst` ya está cableado en los 3 minijuegos. Solo verifica que haya un `ScreenShake` en la escena (o que el de MainMenu haya llegado vía DontDestroyOnLoad).

---

### PASO 9 — Session persistence (automática)

`ResultsScreen` guarda `PlayerPrefs.SetInt("best_color" / "best_mirror" / "best_size", score)`. `MainMenuController.Start()` los lee y los muestra. Sin SQL, sin login.

---

## ⚠️ Gotchas comunes

1. **Python debe estar corriendo** (`python pose_sender_udp.py`) antes de cualquier escena con pose
2. **Si quitas MIRROR=True** en Python → todos los validators se rompen
3. **Encuadre de cámara**: el niño debe verse desde rodillas hasta encima de la cabeza con brazos extendidos. Marca el piso a 2m de la cámara
4. **Orden de botones Easy/Medium/Hard**: debe llamar `StartGame(0)`, `StartGame(1)`, `StartGame(2)` respectivamente
5. **Música por escena**: si quieres música distinta por minijuego, añade un `MusicManager` local en esa escena apuntando a otro clip. El singleton global se autodestruye si ya hay uno

---

## 🐛 Limitaciones conocidas

### Python (`pose_sender_udp.py`)

**NO envía `visibility`** por landmark. Esto causa:
- MirrorWord **SQUAT**: rodillas (25, 26) fuera de frame → validator poco fiable
- SizeSort **WIDE / TALL**: muñecas o nariz clampadas al borde del frame → medición falsa

**Fix pendiente (15 min)**:
1. En `pose_sender_udp.py` línea ~86: `landmarks = [[lm.x, lm.y, lm.z, lm.visibility] for lm in results.pose_landmarks.landmark]`
2. En `PoseReceiverUDP.cs` extender parser para leer 4 números por landmark
3. Gate en validators sensibles: `if (visibility[15] < 0.5f) return false;`

**Workaround de presentación**: asegurar buen encuadre + mover target `WIDE` de 3.4 a 3.0 si hace falta.

### Rendimiento

Si el laptop es lento y hay lag: en `pose_sender_udp.py` cambia `model_complexity=1` → `model_complexity=0`. Pierdes precisión, ganas fluidez.

---

## 📚 Referencias rápidas para otra IA

Si usas otra IA para ayudarte con el editor de Unity, pásale esto:

```
Proyecto: Unity 2021.3.17f1 LTS, C#, educativo para niños 6-8 años.
Input: MediaPipe Pose (Python) → UDP 127.0.0.1:5052 → Unity.
MIRROR=True en Python (frame volteado antes de procesar).
Singletons existentes: PoseReceiverUDP.Instance, GameManager.Instance.
Landmarks: 0=nariz, 11/12=hombros, 15/16=muñecas, 23/24=caderas, 25/26=rodillas.
Convención Y: crudas de MediaPipe (Y abajo = positivo).
3 minijuegos: ColorJump (escena), Mirror World (Island3), Size Sort (nueva).
Menú: MainMenuController. Resultados: ResultsScreen (PlayerPrefs "best_color/mirror/size").
```

---

## ✅ Checklist de presentación

- [ ] 3 singletons en MainMenu (ScreenShake, MusicManager, CelebrationBurst)
- [ ] 5 escenas en Build Settings en orden correcto
- [ ] MainMenu con los 5 botones cableados
- [ ] Calibration funciona (detecta pose en vivo)
- [ ] ColorJump: tutorial + results
- [ ] Mirror World: tutorial + results, 8 poses
- [ ] SizeSort: escena nueva montada, 6 palabras funcionan
- [ ] Best scores se guardan entre sesiones
- [ ] Música de fondo suena
- [ ] Celebración (partículas + shake) al acertar
- [ ] Python (`pose_sender_udp.py`) corre sin errores
- [ ] Encuadre de cámara a 2m, cuerpo completo visible
