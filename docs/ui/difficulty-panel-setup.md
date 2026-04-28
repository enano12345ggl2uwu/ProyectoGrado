# DifficultyPanel — Setup completo (UI mejorada)

Guía paso a paso para dejar el panel de selección de dificultad con:
- Título "SELECT DIFFICULTY" mejorado (gradiente, outline, glow, character spacing)
- Subtítulo descriptivo
- Fondo del panel con esquinas redondeadas + transparencia
- Overlay oscuro detrás (en lugar de blur, más liviano para Built-in)
- Partículas decorativas
- Animación de entrada **BounceIn** + cascada de botones

> **Engine**: Unity 2021.3.17f1 LTS · **Render**: Built-in
> Todo lo de abajo es Inspector + 1 script (`DifficultyPanelAnimator.cs` ya creado).

---

## 1. Título "SELECT DIFFICULTY"

Selecciona el `TitleText` del DifficultyPanel.

### TextMeshProUGUI (componente)
| Propiedad | Valor |
|---|---|
| Font Asset | Fredoka One SDF (o el que uses) |
| Font Style | **Bold** |
| Font Size | 72–90 (ajusta a tu canvas) |
| Auto Size | OFF |
| Alignment | Center |
| Color Gradient | ✅ ON |
| Color Mode | **Vertical** |
| Top color | `#FFD93D` (amarillo) |
| Bottom color | `#FF8C1A` (naranja, igual que `UITheme.ButtonBase`) |
| Character Spacing | `10` (en Extra Settings o sección Spacing) |

### Material del TMP (Outline + Underlay + Glow)

> **Importante**: crea un **Material Preset** para no romper otros textos.
> Click derecho en el material del TitleText → **Create Material Preset** → llámalo `Title_FredokaOne_SDF`. Asigna ese preset al título.

Abre el preset y configura:

**Outline**
- Color: negro
- Thickness: `0.15`

**Underlay** (sombra dura)
- Color: negro, alpha `0.6`
- Offset X: `1`
- Offset Y: `-1`
- Dilate: `0.2`
- Softness: `0.3`

**Glow**
- Color: `#FFD93D` (amarillo cálido), alpha `0.4`
- Offset: `0`
- Inner: `0`
- Outer: `0.4`
- Power: `1`

> Si no ves Outline / Underlay / Glow en el material, asegúrate de que la shader sea **TextMeshPro/Distance Field** (no la versión "Mobile").

---

## 2. Subtítulo "Choose how you want to play"

Click derecho sobre `DifficultyPanel` → **UI → Text - TextMeshPro**. Renombra a `SubtitleText`.

| Propiedad | Valor |
|---|---|
| Texto | `Choose how you want to play` |
| Font Asset | Fredoka One SDF |
| Font Size | 24–28 |
| Color | blanco con alpha `0.7` |
| Alignment | Center |
| Character Spacing | `5` |

**RectTransform**: posiciónalo justo debajo del título (Y ≈ -60 desde el title).

---

## 3. Fondo del panel — rounded + transparencia

Selecciona `DifficultyPanel`.

1. Asegúrate de que tenga componente **Image** (si no, Add Component → Image).
2. **Sprite del fondo**: usa el que genera `RoundedRectGenerator` (en runtime ya lo hace `UIButtonStyle`, pero el panel necesita su propio sprite). Opciones:

   **Opción A — sprite estático en Inspector**: arrastra cualquier sprite redondeado pre-hecho.

   **Opción B — script auxiliar** (recomendado): crea un componente que genere el sprite en runtime. Si no quieres más scripts, deja el panel sin sprite (color sólido + esquinas cuadradas) — visualmente sigue funcionando.

3. **Color del Image**: `#1E2A4A` con alpha `0.85` (azul oscuro semi-transparente — deja ver el fondo del juego detrás).

4. **Componente Outline** (Add Component → Outline):
   - Effect Color: blanco, alpha `0.3`
   - Effect Distance: X=2, Y=-2

---

## 4. Overlay oscuro detrás del panel

Esto reemplaza al "blur" — mucho más simple y visualmente cumple el mismo rol.

1. Click derecho en el Canvas (no en el panel) → **UI → Image**. Renómbralo `DimOverlay`.
2. **Orden**: el `DimOverlay` debe estar JUSTO ANTES del `DifficultyPanel` en la jerarquía (que el panel quede encima).
3. **RectTransform**: anchor stretch full → ocupa toda la pantalla.
4. **Image**:
   - Color: `#000000` con alpha `0.6`
   - Raycast Target: ✅ ON (bloquea clicks al juego de atrás)
5. **CanvasGroup** (Add Component): para que aparezca/desaparezca con el panel.

> Si quieres que el overlay también haga fade in cuando aparece el panel, añádele un `DifficultyPanelAnimator` con `entryStyle = FadeScale` y `cascadeButtons = false`.

---

## 5. Partículas decorativas (opcional pero recomendado)

Click derecho en el Canvas → **Effects → Particle System**. Renombra `BgParticles`.

> Para que las partículas se rendericen dentro del Canvas necesitas o un Particle System con sorting order alto, o usar UI Particle System (asset gratis del Asset Store). La forma simple **sin assets externos**:
> - Coloca el ParticleSystem como hijo del Canvas
> - En el Renderer del ParticleSystem: Sorting Layer = "UI" (créala si no existe), Order in Layer = entre el DimOverlay y el DifficultyPanel

### Parámetros sugeridos

**Main**
- Duration: 5
- Looping: ✅
- Start Lifetime: 8–12
- Start Speed: 0.3–0.8
- Start Size: 0.1–0.3
- Start Color: blanco con alpha 0.4
- Simulation Space: Local
- Max Particles: 30

**Emission**
- Rate over Time: 3

**Shape**
- Shape: Edge (o Box)
- Posicionado en la parte inferior del panel (las partículas suben)

**Size over Lifetime**: curva que sube de 0 a 1 y baja a 0 (las partículas "respiran")

**Color over Lifetime**: alpha 0 → 0.6 → 0 (fade in/out suave)

**Renderer**
- Material: Default-Particle (o uno con sprite estrellita / círculo)

---

## 6. Animación de entrada (DifficultyPanelAnimator)

El script ya está creado en `Assets/Scripts/UI/DifficultyPanelAnimator.cs`.

### Setup

1. Selecciona `DifficultyPanel`.
2. **Add Component → DifficultyPanelAnimator**.
3. Configura en Inspector:

| Campo | Valor |
|---|---|
| Entry Style | **BounceIn** |
| Duration | `0.5` |
| Cascade Buttons | ✅ ON |
| Buttons (lista) | Easy, Medium, Hard, Start (en ese orden) |
| Cascade Stagger | `0.08` |
| Cascade Duration | `0.3` |

> **Cómo asignar la lista**: en el Inspector, click en el `+` del campo `Buttons` 4 veces, luego arrastra cada botón (el GameObject completo, no solo el componente) desde la jerarquía a cada slot.

### Cómo funciona

- El script se dispara automáticamente cuando el panel se activa (`OnEnable`).
- Si tu panel arranca activo en la escena, la animación corre al cargar la escena.
- Si lo activas/desactivas en runtime (al cambiar entre menú y juego), la animación se reproduce cada vez.

### Probar otros estilos

El componente tiene 3 estilos disponibles. Cambia el dropdown `Entry Style`:
- **BounceIn** ⭐ recomendado para kids
- **FadeScale** — clásico elegante
- **SlideDown** — cae desde arriba
- **None** — desactiva la animación del panel (la cascada sigue funcionando)

---

## 7. Orden final en la jerarquía

```
Canvas
├── DimOverlay              (Image negro alpha 0.6, full screen)
├── BgParticles             (ParticleSystem decorativo)
├── DifficultyPanel         (rounded, semi-transparente)
│   ├── TitleText           (SELECT DIFFICULTY)
│   ├── SubtitleText        (Choose how you want to play)
│   ├── EasyBtn
│   ├── MediumBtn
│   ├── HardBtn
│   ├── DescriptionText
│   └── StartBtn
└── PoseCursor              (último hijo, siempre arriba) ← pendiente
```

---

## 8. Checklist de verificación

Antes de dar por hecho el setup:

- [ ] El título tiene gradiente vertical visible
- [ ] El título tiene outline negro y glow amarillo en el material preset
- [ ] El subtítulo está bajo el título y se lee
- [ ] El panel tiene color azul oscuro semi-transparente (se ve el juego detrás)
- [ ] El DimOverlay oscurece el fondo del juego cuando el panel está visible
- [ ] Al entrar Play Mode, el panel hace BounceIn
- [ ] Después del bounce, los 4 botones aparecen en cascada
- [ ] Los botones siguen funcionando al hacer click después de la animación
- [ ] El botón seleccionado (Easy por defecto) tiene su pulso + outline blanco

---

## 9. Pendiente — Próxima sesión

- [ ] Agregar PoseCursor a MainMenu, ColorJump, MirrorWord, SizeSort, BalloonPop
- [ ] Decidir entre video real de fondo (VideoPlayer) vs sprite-sheet de cielo+nubes
- [ ] Generar `Sky.png` y `Clouds.png` con IA (prompts en `docs/ui/background-prompts.md` si lo creamos)
- [ ] Script `BackgroundScroller.cs` para mover las nubes lento de izq a der
