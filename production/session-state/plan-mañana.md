# Plan Detallado — Mirror the Word (Mañana)

**Deadline:** Presentación en 2 días  
**Objetivo:** Mirror the Word 100% jugable + UI + sonidos + dificultad  
**Tiempo disponible:** ~6-8 horas productivas

---

## PARTE 1: Escena Island3 en Unity (45 min)

### 1.1 Crear y guardar la escena
```
File → New Scene → Basic (Built-in)
File → Save As
Carpeta: Assets/Scenes/
Nombre: Island3
```

### 1.2 Configurar Cámara
Selecciona `Main Camera` en Hierarchy:
```
Position:     X=0   Y=0   Z=-10
Rotation:     0   0   0
Clear Flags:  Solid Color
Background:   #0A0A1A (azul muy oscuro, casi negro)
FOV:          60  (default)
```

### 1.3 PoseReceiverUDP (si no existe)
```
Hierarchy → Create Empty → renombrar "PoseReceiver"
Add Component → PoseReceiverUDP
Inspector:
  Port = 5052
  Smoothing = 0.5
```

### 1.4 StickFigure
```
Hierarchy → Create Empty → renombrar "StickFigure"
Position: X=0  Y=0  Z=0
Add Component → StickFigureUDP

Inspector:
  Scale = 5
  Offset = (0, 2, 0)
  Bone Smoothing = 18
  Head Size = 0.55
  Eye Size = 0.13
  Emission Intensity = 1.8
  Enable Glow = ✓
```

### 1.5 MirrorGame GameObject
```
Hierarchy → Create Empty → renombrar "MirrorGame"
Position: X=0  Y=0  Z=0
Add Component → MirrorWordGameUDP
Add Component → AudioSource
  Spatial Blend = 0  (2D, no 3D)

Inspector en MirrorWordGameUDP:
  Round Time = 8
  Hold Time = 1.5
  Feedback Time = 1.8
  Audio Source = (arrastrar AudioSource del mismo GameObject)
  Stick Figure = (arrastrar StickFigure)
```

---

## PARTE 2: Canvas UI (60 min)

### 2.1 Crear Canvas
```
Hierarchy → UI → Canvas
  Render Mode: Screen Space - Overlay
  Canvas Scaler: UI Scale Mode = Scale with Screen Size
```

### 2.2 Crear UI Elements (uno por uno)

**WordText** (la palabra a imitar)
```
Hierarchy → Canvas → Text - TextMeshPro → renombrar "WordText"
Rect Transform:
  Anchor Preset: Top → Center
  Pos Y: -100
  Width: 800  Height: 200
TextMeshPro:
  Text: "HANDS UP"
  Font Size: 90
  Alignment: Center Flush
  Color: White
```

**ScoreText** (puntuación arriba izquierda)
```
Hierarchy → Canvas → Text - TextMeshPro → renombrar "ScoreText"
Rect Transform:
  Anchor Preset: Top Left
  Pos: (120, -50)
  Width: 300  Height: 80
TextMeshPro:
  Text: "Score: 0"
  Font Size: 40
  Color: White (0.2, 1, 0.3) verde neon
```

**CountdownText** (contador arriba derecha)
```
Hierarchy → Canvas → Text - TextMeshPro → renombrar "CountdownText"
Rect Transform:
  Anchor Preset: Top Right
  Pos: (-120, -50)
  Width: 150  Height: 150
TextMeshPro:
  Text: "8"
  Font Size: 80
  Color: White
  Alignment: Center
```

**FeedbackText** (centro pantalla)
```
Hierarchy → Canvas → Text - TextMeshPro → renombrar "FeedbackText"
Rect Transform:
  Anchor Preset: Center
  Pos: (0, 0)
  Width: 600  Height: 150
TextMeshPro:
  Text: "" (vacío al inicio)
  Font Size: 70
  Color: Green (0.2, 1, 0.3)
  Alignment: Center
```

**HoldFillBar** (barra de progreso abajo)
```
Hierarchy → Canvas → Image → renombrar "HoldFillBar"
Rect Transform:
  Anchor Preset: Bottom → Center
  Pos Y: 60
  Width: 600  Height: 30
Image:
  Image Type: Filled
  Fill Method: Horizontal
  Fill Origin: Left
  Fill Amount: 0
  Color: #33FF77 (verde neon brillante)
```

**BackButton** (botón abajo izquierda)
```
Hierarchy → Canvas → Button - TextMeshPro → renombrar "BackButton"
Rect Transform:
  Anchor Preset: Bottom Left
  Pos: (100, 50)
  Width: 150  Height: 60
Button:
  Target Graphic: (la imagen del botón)
  Normal Color: #333333
  Highlighted Color: #555555
  Pressed Color: #111111
  
→ Dentro de BackButton crear un Text (TMP):
  Text: "MENU"
  Font Size: 28
  Color: White
```

### 2.3 Conectar referencias en MirrorGame
Selecciona `MirrorGame` → Inspector → MirrorWordGameUDP:
```
wordText      → arrastrar WordText
scoreText     → arrastrar ScoreText
feedbackText  → arrastrar FeedbackText
countdownText → arrastrar CountdownText
holdFillBar   → arrastrar HoldFillBar
stickFigure   → arrastrar StickFigure
audioSource   → arrastrar AudioSource (del mismo MirrorGame)
```

### 2.4 Conectar botón Back
```
Selecciona BackButton en Hierarchy
Button (Script) → OnClick → "+"
Arrastrar "MirrorGame" → MirrorWordGameUDP → BackToMenu()
```

---

## PARTE 3: Sonidos (30 min)

### 3.1 Obtener archivos de audio
Necesitas dos clips WAV/MP3:
- **correctClip.wav** — sonido cuando aciertas (ej. "ding" positivo, 0.3-0.5s)
- **wrongClip.wav** — sonido cuando fallas (ej. "buzz" negativo, 0.2s)

Opciones:
- **Gratuitas:** Freesound.org, Zapsplat.com, Pixabay Music
- **Simples:** Genera online en Sfxr.me o similar
- **Sugerencias:**
  - Correct: sonido de "success" / "chime" / "power-up"
  - Wrong: sonido de "error buzz" / "negative beep"

### 3.2 Importar a Unity
```
Assets/Sounds/ (crear carpeta si no existe)
→ Arrastra correctClip.wav
→ Arrastra wrongClip.wav
Espera a que Unity los importe
```

### 3.3 Asignar en MirrorGame
Selecciona `MirrorGame` → Inspector → MirrorWordGameUDP:
```
Correct Clip → arrastrar correctClip (importado)
Wrong Clip   → arrastrar wrongClip (importado)
```

### 3.4 Prueba
Play en Unity. Cuando aciertes debe sonar "correctClip".
Cuando se agote el tiempo debe sonar "wrongClip".

---

## PARTE 4: Dificultad (45 min)

### 4.1 Modificar MirrorWordGameUDP.cs
Abre el script y añade esto al inicio de la clase:

```csharp
// Dificultad
[Header("Difficulty")]
public enum DifficultyMode { Easy, Medium, Hard }
public DifficultyMode difficulty = DifficultyMode.Medium;

[SerializeField] private float baseCorrectionTime = 1.5f;  // tiempo base para acertar
[SerializeField] private float baseRoundTime = 8f;        // tiempo base de ronda
```

### 4.2 Función para aplicar dificultad
En Start(), después de UpdateScoreUI():

```csharp
void ApplyDifficulty()
{
    switch (difficulty)
    {
        case DifficultyMode.Easy:
            holdTime = baseCorrectionTime * 1.5f;  // 2.25s para acertar
            roundTime = baseRoundTime * 1.2f;       // 9.6s por ronda
            break;
        case DifficultyMode.Medium:
            holdTime = baseCorrectionTime;          // 1.5s (default)
            roundTime = baseRoundTime;              // 8s (default)
            break;
        case DifficultyMode.Hard:
            holdTime = baseCorrectionTime * 0.8f;   // 1.2s para acertar
            roundTime = baseRoundTime * 0.8f;       // 6.4s por ronda
            break;
    }
}
```

Llama desde Start():
```csharp
void Start()
{
    if (feedbackText) feedbackText.text = "";
    if (holdFillBar)  holdFillBar.fillAmount = 0f;
    ApplyDifficulty();  // ← ADD THIS
    UpdateScoreUI();
    StartCoroutine(GameLoop());
}
```

### 4.3 Ajustar thresholds por dificultad (optional)
En GetParts(), dentro de cada caso (HandsUp, TPose, etc):

```csharp
// Ejemplo para HandsUp
case Pose.HandsUp:
{
    float baseTol = sw * 0.25f;
    float tolMult = difficulty == DifficultyMode.Easy ? 1.5f : 
                    difficulty == DifficultyMode.Hard ? 0.8f : 1f;
    float tol = baseTol * tolMult;
    return new[] { ... };
}
```

### 4.4 Selector de dificultad en Inspector
En Unity, selecciona `MirrorGame` → Inspector → MirrorWordGameUDP:
```
Difficulty = (Easy | Medium | Hard)
```

Prueba cada nivel:
- **Easy:** Thresholds amplios, 2.25s para sostener
- **Medium:** Thresholds normales, 1.5s
- **Hard:** Thresholds estrictos, 1.2s

---

## PARTE 5: Cómo funciona Mirror the Word (Explicación)

### 5.1 Flujo del juego
```
1. INICIO
   ↓
2. Se muestra palabra aleatoria (HANDS UP, T POSE, etc)
   ↓
3. Niño ve al stickman en pantalla
   Intenta imitar la pose
   ↓
4. VALIDACIÓN EN VIVO (cada frame)
   - Partes del stickman correctas → VERDE
   - Partes incorrectas → ROJO
   - Barra de progreso se llena
   ↓
5. Si mantiene 1.5s correctamente
   → "Perfect!" + sonido + +10 pts
   → Stickman todo verde 1s
   ↓
6. Si se agota tiempo (8s) sin completar
   → "Try again!" + sonido wrong
   ↓
7. Pausa 1.8s, vuelve a paso 2
```

### 5.2 Validación técnica (qué revisa)

**HANDS UP:**
- Ambas muñecas ARRIBA de los hombros
- Tolerancia: 25% de distancia entre hombros

**T POSE:**
- Ambos brazos a altura de hombro
- Brazos extendidos hacia los lados (60% ancho de hombros)
- Validación más estricta

**TOUCH HEAD:**
- Cualquiera de las dos manos cerca de la cabeza
- Tolerancia: 55% distancia entre hombros (fácil para niños)

**ARMS WIDE:**
- Distancia entre muñecas > 110% ancho de hombros
- Simplemente brazos bien abiertos

**HANDS DOWN:**
- Ambas manos DEBAJO de las caderas
- Tolerancia: 15% (fácil)

### 5.3 Feedback visual

| Estado | Color | Texto |
|--------|-------|-------|
| Esperando | Blanco | (nombre pose) |
| Sosteniendo | Verde gradual | "HOLD IT!" |
| Correcto | Verde | "Perfect!" |
| Tiempo agotado | Blanco | "Try again!" |

### 5.4 Ciclo de scoring
```
Por cada pose completada: +10 pts
Por ronda (cada 8s): +0 pts si falla

Meta: máximo 50 puntos en 5 poses
      (si el niño completa todas sin fallar)
```

---

## PARTE 6: Checklist de Pruebas (15 min)

### Antes de mostrar:
- [ ] Python script corriendo (puerto 5052)
- [ ] Unity Play mode
- [ ] Stickman aparece y se mueve cuando te mueves
- [ ] Palabra aparece arriba (WordText)
- [ ] Score contador funciona
- [ ] Countdown cuenta hacia atrás desde 8
- [ ] Cuando haces la pose correcta:
  - [ ] Partes se ponen verdes
  - [ ] Barra se llena
  - [ ] A los 1.5s: "Perfect!" aparece
  - [ ] Sonido correctClip suena
  - [ ] Score aumenta +10
- [ ] Cuando falla:
  - [ ] Tiempo agota, "Try again!"
  - [ ] Sonido wrongClip suena
- [ ] Botón MENU regresa (si existe GameManager)
- [ ] Dificultad Easy/Medium/Hard cambia comportamiento

---

## PARTE 7: Orden de ejecución RECOMENDADO (mañana)

**Orden por impacto:**
1. ✅ Escena Island3 (45 min) — **CRÍTICO**, sin esto no juega
2. ✅ Canvas UI (60 min) — **CRÍTICO**, visibilidad
3. ✅ Sonidos (30 min) — **IMPORTANTE**, feedback sensorial
4. ✅ Prueba end-to-end (15 min) — verificar todo funciona
5. ⏱️ Dificultad (45 min) — si hay tiempo, mejora la replicabilidad
6. 🎨 Polish visual (lo que sobre) — animaciones, efectos

**Tiempo total:** ~3-3.5 horas de trabajo real  
**Buffer:** 2-3 horas para debug/ajustes

---

## Notas finales

- **Antes de empezar:** Asegúrate que Python y Unity hablan (PoseReceiverUDP recibe datos en puerto 5052)
- **Si algo no funciona:** Revisa la consola de Unity (Window → General → Console) para mensajes de error
- **Audio:** Si no encuentras clips buenos, puedes usar OneShotAudio o Audacity para grabar/generar
- **Dificultad:** No es crítica para la demo. Si se acaba el tiempo, salta este paso
- **Presentación:** Muestra Easy mode primero (más permisivo) para que se vea bien en vivo
