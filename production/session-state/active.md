# Session State — Move & Learn

<!-- STATUS -->
Epic: Multi-Minigame Build
Feature: ResultsScreen + BalloonPop + PoseCursor
Task: Pendiente rediseño visual del ResultsPanel
<!-- /STATUS -->

## Current Task
ResultsPanel funciona (aparece, oculta el juego) pero al usuario no le gusta el diseño visual.
Próximo paso: rediseñar el layout/look del ResultsPanel según feedback del usuario.

## Estado por Escena

### ColorJump (Island1) — JUGABLE
- [x] Scripts, StickFigure, plataformas
- [x] DifficultySelector conectado
- [x] ResultsScreen — aparece al terminar, oculta gamePanel
- [ ] ResultsPanel: diseño visual pendiente de aprobación

### BalloonPop — JUGABLE (usa NumberBalloonGameUDP)
- [x] NumberBalloonGameUDP: dead zone ajustable (campo deadZone=1.2u)
- [x] NumberBalloonGameUDP: campo results conectado → muestra ResultsScreen al terminar
- [x] ResultsScreen: oculta gamePanel al mostrar resultados
- [ ] Conectar en Inspector: BalloonManager → NumberBalloonGameUDP → campo Results → ResultsPanel
- [ ] ResultsPanel: diseño visual pendiente de aprobación

### SizeSort (Island2) — FUNCIONAL PARCIAL
- [x] SizeContourDisplay: fix useWorldSpace=true, sortingOrder=100
- [x] Escena existe con refs conectadas
- [ ] Verificar que siluetas se ven en play mode tras ajuste SizeContour Y=1.5

### MirrorWord (Island3) — FUNCIONAL
- [x] MirrorWordGameUDP funcionando
- [ ] ResultsPanel diseño visual pendiente

## Scripts modificados esta sesión (2026-05-10)
- `Assets/Scripts/Avatar/SizeContourDisplay.cs` — useWorldSpace=true, sortingOrder=100
- `Assets/Scripts/Minigames/BalloonPopGameUDP.cs` — rediseño completo spawn lateral
- `Assets/Scripts/Minigames/NumberBalloonGameUDP.cs` — +ResultsScreen, +deadZone
- `Assets/Scripts/UI/PoseCursor.cs` — dwell 3s, hover events, gesto brazo arriba
- `Assets/Scripts/UI/ResultsScreen.cs` — +poseCursor, +gamePanel, oculta gamePanel en Show()

## Decisiones clave
- BalloonPop escena usa NumberBalloonGameUDP (NO BalloonPopGameUDP)
- DifficultySelector.balloonPopGame = null en escena; usa numberBalloonGame
- Puerto UDP: 7777
- PoseCursor: dwell 3s, armRaiseThreshold=0.25, armRaiseCooldown=1s
- ResultsScreen.Show() → gamePanel.SetActive(false) antes de panel.SetActive(true)

## Pendiente (usuario decide)
- Rediseñar visualmente el ResultsPanel (el usuario no le gustó el layout actual)
- Conectar Results en Inspector de BalloonPop escena
- Conectar gamePanel en ResultsScreen de cada escena
