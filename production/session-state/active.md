# Session State — Move & Learn

<!-- STATUS -->
Epic: Polish Phase (Pre-Defensa)
Feature: TutorialOverlay v2
Task: Listo para escribir el nuevo TutorialOverlay.cs (propuesta aprobada por usuario)
<!-- /STATUS -->

## Contexto general
Fase de polish previa a la defensa de tesis. Presupuesto del usuario: **una tarde + medio día**.
4 minijuegos ya funcionales (ColorJump, BalloonPop, SizeSort, MirrorWord).
Sesión actual: 2026-05-16 (continuación + nueva sesión nocturna). Retomar 2026-05-17.

---

## ✅ Hecho en esta sesión (2026-05-16)

### 1. RoundProgressBar — Anillo radial de tiempo sobre PoseCursor (COMMIT 6480ca8, pushed)
- Nuevo componente `Assets/Scripts/UI/RoundProgressBar.cs` — anillo radial verde→amarillo→rojo con pulso al <20%
- `Assets/Scripts/UI/RingSpriteGenerator.cs` — genera sprite de anillo hueco por código (con cache)
- `Assets/Scripts/UI/RingSpriteApplier.cs` — helper que aplica el sprite al Image en Awake
- Integrado en los 4 minijuegos (ColorJump, NumberBalloon, SizeSort, MirrorWord)
- **Extra:** ColorJump tiene nuevo `interRoundPause` (2.0/1.5/1.0s Easy/Med/Hard) para dar aire al jugador entre rondas

### 2. PoseCursor debugging + fixes (NO commiteado todavía)
- **Bug encontrado:** `cursorRect.gameObject.SetActive(false)` desactivaba el GameObject del script cuando `cursorRect` apuntaba al parent PoseCursor — quedaba oculto para siempre
- **Fix aplicado en `Assets/Scripts/UI/PoseCursor.cs`:** reemplazado `SetActive` por nuevo método `SetCursorVisible(bool)` que toggles `Graphic.enabled` en hijos en vez de desactivar el GameObject
- **Default Hand Landmark cambiado de 16 → 15** (usuario controlaba con mano equivocada)
- **OJO:** valores ya serializados en escenas existentes NO se actualizan automáticamente — usuario debe poner Hand Landmark=15 manualmente en Inspector de cada escena

### 3. Setup PoseCursor en escena ColorJump (usuario lo hizo)
- Jerarquía: `PoseCursor → RoundProgressRing + DwellRing + CursorDot`
- DwellRing tenía sprite ring funcionando (usuario asignado manualmente, no con RingSpriteApplier)
- RoundProgressRing al inicio no se dibujaba → causa raíz: `ColorJumpManager.RoundProgressBar` estaba en `None` (sin referencia) → ahora wired
- Setting recomendado: `Cursor Rect = PoseCursor (parent)` para que los 3 hijos se muevan juntos

### 4. Cosas que se intentaron y se descartaron
- Agregado `cursorScreenOffsetY` para bajar el cursor → **revertido** (usuario lo rechazó)
- `img.SetAllDirty()` en RingSpriteApplier → revertido (rompía render)

---

## 📋 Pendiente inmediato — Retomar mañana

### TutorialOverlay v2 — APROBADO por usuario, listo para escribir
**Cambios al plan original:** SIN audio narrado, solo imagen placeholder hasta tener video real.

**Decisiones finales (2026-05-16 noche):**
- Formato: **imagen placeholder + texto** (video después cuando esté disponible)
- **Mouse click TAMBIÉN funciona** (además de dwell del PoseCursor) — para testing
- **PlayerPrefs persiste por instalación** (no por sesión)
- `GlobalProgressBar.Refresh()` ya se llama en Start() del IslandSelector — funciona automático

**Textos confirmados (Spanish):**
| Minijuego | Title | Body |
|-----------|-------|------|
| ColorJump | "Color Jump" | "Mira el color en la pantalla. Muévete a la izquierda o derecha para pararte sobre la plataforma del color correcto." |
| BalloonPop | "Balloon Pop" | "Cuando aparezca un número en inglés, toca el globo con ese número usando tu mano." |
| SizeSort | "Size Sort" | "Lee la palabra y cambia tu cuerpo: TALL (alto), SHORT (agachado), WIDE (brazos abiertos), NARROW (brazos pegados), BIG (todo grande), SMALL (todo chico)." |
| MirrorWord | "Mirror Word" | "Imita la pose que aparece en la silueta. Mantén la pose hasta llenar la barra." |

**API del nuevo script:**
- Campos Inspector: `title`, `body`, `placeholderSprite`, `minigameKey` (color/balloon/size/mirror), `showOnStart`, `pauseGame`, `clickToCloseEnabled`, `fadeDuration`
- `Start()`: muestra solo si `!PlayerPrefs.GetInt("tutorial_seen_{key}")`
- Botón "Listo" → fade-out + marca PlayerPrefs + `Time.timeScale = 1`
- Método estático `TutorialOverlay.ResetAll()` para debug
- **NO requiere editar managers** — el TutorialOverlay se autogestiona en Start

**Próximo paso al retomar:**
1. Reescribir `Assets/Scripts/UI/TutorialOverlay.cs` con la nueva API
2. Pasar guía de setup en escena al usuario (asignar refs, configurar texts)
3. Probar en ColorJump primero, después replicar en BalloonPop, SizeSort, MirrorWord

---

## 🔜 Pendientes después del Tutorial Overlay
1. **Pause Menu** — ESC + gesto cruzar brazos 1.5s + auto-pausa por tracking lost >2s
2. **SFX Audit** — revisar dónde faltan sonidos en los 4 minijuegos
3. **StickFigure visual** — cara expresiva + trail + aura combo + skins (estilo Terraria)
4. **Pose lost indicator**
5. **End-of-session summary** ("Hoy aprendiste: X colores, Y números")
6. **Fade-to-black transitions** (0.3s entre escenas)
7. **Combo counter visible** ("x3!" con shake)
8. **Confirmación antes de salir**
9. **Voz narradora pregrabada para la palabra inglesa** del juego

---

## Archivos modificados esta sesión

### Commiteados (6480ca8 — pushed a main)
- `Assets/Scripts/UI/RingSpriteApplier.cs` (NEW)
- `Assets/Scripts/UI/RingSpriteGenerator.cs` (NEW)
- `Assets/Scripts/UI/RoundProgressBar.cs` (NEW)
- `Assets/Scripts/Minigames/ColorJumpGameUDP.cs` (+RoundProgressBar +interRoundPause)
- `Assets/Scripts/Minigames/MirrorWordGameUDP.cs` (+RoundProgressBar)
- `Assets/Scripts/Minigames/NumberBalloonGameUDP.cs` (+RoundProgressBar)
- `Assets/Scripts/Minigames/SizeSortGameUDP.cs` (+RoundProgressBar)

### Sin commitear (modificados después del commit)
- `Assets/Scripts/UI/PoseCursor.cs` — fix de visibilidad (SetActive→Graphic.enabled) + default Hand Landmark 16→15

---

## Decisiones clave de la sesión
- TutorialOverlay v2: **sin audio narrado**, imagen placeholder hasta video real
- TutorialOverlay v2: mouse click habilitado en paralelo al dwell del PoseCursor
- PlayerPrefs del tutorial persisten por instalación (no por sesión)
- `GlobalProgressBar.Refresh()` ya se ejecuta automáticamente en Start del IslandSelector
- RoundProgressBar usa sprite generado por código (RingSpriteGenerator) — funciona pero usuario tuvo problemas; alternativa válida: usar el mismo sprite que DwellRing (manual)
- PoseCursor: `Cursor Rect = PoseCursor (parent)` para que los 3 anillos sigan la mano juntos
- Hand Landmark: 15 (cambio aplicado en código + Inspector de escenas activas)
