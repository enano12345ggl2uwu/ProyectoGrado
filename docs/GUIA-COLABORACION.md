# Guía de Colaboración — Move & Learn (Proyecto de Grado)

> **Para colegas que quieren continuar el desarrollo del juego educativo.**
> 
> Escrita en español para facilitar la colaboración remota.

---

## 📋 Antes de Empezar

1. **Clona o sincroniza el repositorio:**
   ```bash
   git clone <url-del-repo>
   # o si ya lo tienes:
   git pull origin main
   ```

2. **Abre Claude Code** en esta carpeta:
   ```bash
   cd ProyectoGrado
   claude code
   ```

3. **Lee el estado actual:**
   ```
   Abre: production/session-state/active.md
   ```
   Este archivo dice exactamente en qué está trabajando Martin y qué hace falta.

---

## 🎮 Qué es Move & Learn

- **Juego educativo:** Enseña inglés a niños de 6-8 años con TDAH
- **Mecánica:** Usa una cámara web (MediaPipe) para capturar el cuerpo del niño y controlar un monigote en pantalla
- **Minijuegos:** ColorJump (saltar a colores), Mirror the Word (copiar poses)
- **Motor:** Unity 2021.3.17f1 LTS (C#)
- **Estado:** En fase de diseño de sistemas (GDD) — faltan documentos de diseño

---

## 📁 Estructura del Proyecto

```
ProyectoGrado/
├── design/gdd/                 ← Los documentos de diseño van aquí
│   ├── game-concept.md         ✅ Escrito
│   ├── systems-index.md        ✅ Escrito (10 sistemas, 9 MVP + 1 VS)
│   ├── pose-input.md           ← EN PROGRESO (primera GDD)
│   ├── stick-figure-renderer.md
│   ├── game-manager.md
│   ├── audio-feedback.md
│   ├── colorjump.md
│   ├── round-ui-hud.md
│   ├── calibration-screen.md
│   ├── main-menu.md
│   └── end-of-session-summary.md
├── ProyectodeGrado(noborrarpls)/ProyectoGrado/
│   └── Assets/Scripts/          ← Código C# existente
├── ProyectoGrado_Python/
│   └── pose_sender_udp.py      ← Puente MediaPipe → UDP
├── production/
│   └── session-state/active.md  ← 📍 LEE ESTO PRIMERO
└── docs/
    ├── engine-reference/unity/
    ├── architecture/             ← ADRs (decisiones técnicas)
    └── GUIA-COLABORACION.md      ← Este archivo
```

---

## 🚀 Cómo Continuar el Desarrollo

### Paso 1: Lee el Estado Actual

```bash
cat production/session-state/active.md
```

Verás algo como:
```
## Current Task
Systems decomposition complete. Ready to author individual system GDDs.

## Progress Checklist
- [x] Game concept written
- [x] Engine configured
- [x] Systems index written
- [ ] GDDs authored (0/10)
```

Esto te dice qué hace falta hacer.

### Paso 2: Diseña el Siguiente Sistema

Usa el comando `/design-system` en Claude Code para escribir un GDD de diseño.

**Ejemplo:**
```
/design-system pose-input
```

Este comando:
1. Lee el contexto (concepto, sistemas que dependen, código existente)
2. Te hace preguntas sobre cómo debe funcionar
3. Crea el archivo `design/gdd/pose-input.md` con todas las secciones
4. Actualiza automáticamente `production/session-state/active.md`

**Orden recomendado de sistemas** (desde `systems-index.md`):
1. Pose Input (Foundation)
2. Stick Figure Renderer (Foundation)
3. Game Manager (Foundation)
4. Audio Feedback (Foundation)
5. ColorJump Game Logic (Core)
6. Round UI / HUD (Feature)
7. Calibration Screen (Feature)
8. Main Menu (Presentation)
9. End-of-Session Summary (Presentation)
10. Mirror the Word (Vertical Slice — después de que ColorJump esté pulido)

### Paso 3: Revisa el GDD que Escribiste

Después de terminar un GDD:
```
/design-review design/gdd/[system-name].md
```

Esto valida que el documento esté completo y correcto.

---

## 📝 Estructura de un GDD (Game Design Document)

Cada archivo de diseño DEBE tener estas 8 secciones:

1. **Overview** — Párrafo resumen (qué es el sistema en una frase)
2. **Player Fantasy** — Cómo se siente para el jugador
3. **Detailed Rules** — Las mecánicas exactas (sin ambigüedad)
4. **Formulas** — Toda la matemática con variables definidas
5. **Edge Cases** — Qué pasa en situaciones raras
6. **Dependencies** — Qué otros sistemas necesita
7. **Tuning Knobs** — Valores que se pueden ajustar en el inspector
8. **Acceptance Criteria** — Cómo sé que está terminado

Si falta alguna sección, el documento **no está listo**.

---

## 🔧 Herramientas Útiles en Claude Code

| Comando | Qué hace |
|---------|----------|
| `/design-system pose-input` | Diseña un sistema (crea/edita GDD) |
| `/design-review design/gdd/pose-input.md` | Valida un GDD terminado |
| `/map-systems next` | Te dice cuál es el siguiente sistema a diseñar |
| `/help` | Lista todos los comandos disponibles |

---

## 💾 Antes de Hacer Commit

**Checklist antes de pushear:**

1. ✅ Leí `production/session-state/active.md` para saber qué estado dejar
2. ✅ Terminé al menos un GDD completo (8 secciones)
3. ✅ Corrí `/design-review` en el GDD y pasó
4. ✅ Actualicé `production/session-state/active.md` con el progreso
5. ✅ Hice commit con un mensaje claro:
   ```bash
   git add design/gdd/[system].md production/session-state/active.md
   git commit -m "Design pose-input GDD

   - Documented 33-landmark data contract
   - Added threading model for UDP receive
   - Defined smoothing tuning knobs
   - Acceptance criteria for liveness detection"
   ```
6. ✅ Hice push:
   ```bash
   git push origin main
   ```

---

## 🎯 Estado Actual (2026-04-16)

**Completado:**
- ✅ Concepto del juego (game-concept.md)
- ✅ Índice de sistemas (systems-index.md)
- ✅ Configuración del motor (Unity 2021.3.17f1)
- ✅ Preferencias técnicas (.claude/docs/technical-preferences.md)

**En progreso:**
- 🔄 **pose-input.md** — Primera GDD, Martin está en la mitad

**Por hacer:**
- ❌ stick-figure-renderer.md (2/10)
- ❌ game-manager.md (3/10)
- ❌ audio-feedback.md (4/10)
- ❌ colorjump.md (5/10)
- ❌ round-ui-hud.md (6/10)
- ❌ calibration-screen.md (7/10)
- ❌ main-menu.md (8/10) — Esteban está trabajando aquí
- ❌ end-of-session-summary.md (9/10)
- ❌ mirror-the-word.md (10/10, Vertical Slice — hacer después de ColorJump)

**Gate check:**
- ❌ Pre-production gate check (cuando los 9 GDDs MVP estén listos)

---

## ⚠️ Reglas Importantes

### Regla #1: Termina Un Sistema Antes de Empezar Otro

Martin fue explícito: **"Finish one minigame polished before starting the next."**

Esto aplica a TODO:
- No hagas cambios en code mientras diseñas GDDs
- No empieces ColorJump si pose-input no está documentado
- No hagas Mirror the Word hasta que ColorJump esté 100% pulido

### Regla #2: Los GDDs Son La Fuente de Verdad

Si hay conflicto entre:
- Lo que el código dice
- Lo que un GDD dice
- Lo que parece correcto

→ **El GDD gana.** El código debe cambiar para coincidir con el diseño, no al revés.

### Regla #3: Datos Externos, No Hardcodeados

Nunca escribas listas de palabras inglesas dentro del código. Eso debe ir en un archivo de datos.

---

## 🤔 Preguntas Frecuentes

**P: ¿Qué hago si no entiendo cómo funciona algo?**

R: Abre Claude Code y pregunta. Describe lo que viste, qué esperabas, y qué pasó en realidad. Claude te ayudará.

**P: ¿Puedo cambiar el diseño si encuentro un problema?**

R: Sí, pero documenta la decisión:
1. Edita el GDD con la nueva información
2. Explica por qué cambió en un comentario
3. Haz commit con el cambio
4. Actualiza `production/session-state/active.md`

**P: ¿Qué pasa si dos personas trabajamos al mismo tiempo?**

R: Trabajen en sistemas diferentes. El orden recomendado es lineal por una razón — cada sistema depende del anterior. Si trabajas en paralelo, coordina por chat.

**P: Mi amigo no entiende el español / inglés. ¿Qué hago?**

R: Usa Claude Code. Claude traduce y adapta explicaciones automáticamente. Solo dile "explain this in [language]".

---

## 📞 Contacto / Ayuda

Si algo se rompe o no tienes claro qué hacer:

1. **Lee** `production/session-state/active.md` (probablemente ahí esté la respuesta)
2. **Lee** `design/gdd/systems-index.md` (el mapa completo)
3. **Abre Claude Code** y describe el problema
4. **Contacta a Martin** si necesitas contexto sobre decisiones no documentadas

---

## 🎓 Decisiones Clave del Proyecto

Estas NO pueden cambiar sin contactar a Martin:

| Decisión | Razón |
|----------|-------|
| Usar monigote (stick figure), no avatar de Amy | Avatar rigged no funciona bien; el monigote ya está implementado |
| MediaPipe (Python) como puente | Es el setup existente; cambiar requiere semanas de refactor |
| 9 sistemas MVP + 1 Vertical Slice | Son los mínimo para la defensa de tesis |
| Terminar ColorJump antes de Mirror the Word | Regla explícita de diseño; cualquier otra prioridad es expansión de scope |
| Niños de 6-8 años con TDAH | El target no cambia; todo el diseño pivota en esto |

---

**Última actualización:** 2026-04-16  
**Próximo checkpoint:** Cuando pose-input.md esté terminado y revisado  
**Responsable actual:** Martin (puedes continuar tú si él no está)
