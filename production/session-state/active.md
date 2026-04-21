# Session State — Move & Learn

<!-- STATUS -->
Epic: Mirror World Implementation
Feature: Island3 Scene + UI + Audio + Difficulty
Task: Mañana: construir escena, UI, sonidos, dificultad
<!-- /STATUS -->

## Current Task
Implementar Mirror the Word completamente para presentación.
Python fix aplicado (pose_landmarks). StickFigure v4 con cuello, cabeza, movimiento en espacio.
Plan detallado para mañana guardado en `production/session-state/plan-mañana.md`.

## Progress Checklist
- [x] Game concept — `design/gdd/game-concept.md`
- [x] Engine configurado — Unity 2021.3.17f1 LTS
- [x] StickFigureUDP.cs — cilindros 3D, cuello, cabeza (orejas), ojos, glow, movimiento
- [x] MirrorWordGameUDP.cs — thresholds mejorados, HOLD IT!, wrongClip fix, 8s
- [x] Python fix — pose_landmarks (el stickman ahora se mueve en espacio)
- [ ] Island3 escena construida
- [ ] UI mejorada (hold bar, score, feedback)
- [ ] Sonidos conectados
- [ ] Dificultad implementada
- [ ] Mirror the Word jugable end-to-end

## Key Decisions
- pose_landmarks (no pose_world_landmarks) — da traslación real en cámara
- StickFigure: joints = gris oscuro, bones = cyan, cabeza = midpoint orejas [7,8]
- emissionIntensity multiplica colores para Bloom HDR
- Cada joint posicionado directo desde coordenadas [0..1] + offset

## Archivos Modificados Esta Sesión
- `Assets/Scripts/Avatar/StickFigureUDP.cs` — v4 completa
- `Assets/Scripts/Minigames/MirrorWordGameUDP.cs` — thresholds + UX
- `ProyectoGrado_Python/pose_sender_udp.py` — pose_world → pose_landmarks
