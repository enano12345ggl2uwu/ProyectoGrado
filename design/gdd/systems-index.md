# Systems Index: Move & Learn

> **Status**: Approved
> **Created**: 2026-04-16
> **Last Updated**: 2026-04-16
> **Source Concept**: design/gdd/game-concept.md

---

## Overview

*Move & Learn* is a body-motion educational game that uses a MediaPipe webcam bridge to track the player's real body and drive an on-screen stick figure through English vocabulary minigames. The system architecture is intentionally minimal — a thin Foundation layer (UDP pose input, audio, session state) supports two Core gameplay systems (ColorJump and Mirror the Word), with Feature systems handling calibration and UI, and Presentation systems handling the menu and end-of-session summary.

All systems serve the four pillars: Body as Controller, ADHD-First Pacing, Visible Progress Every Round, and English Through Verbs. Any system that doesn't serve at least one pillar was excluded.

---

## Systems Enumeration

| # | System | Category | Priority | Status | Design Doc | Depends On |
|---|---|---|---|---|---|---|
| 1 | Pose Input | Core | MVP | Implemented | — | *(none)* |
| 2 | Stick Figure Renderer | Core | MVP | Implemented | — | Pose Input |
| 3 | Game Manager / Session State | Core | MVP | Partial | — | *(none)* |
| 4 | Audio Feedback (inferred) | Audio | MVP | Minimal | — | *(none)* |
| 5 | ColorJump Game Logic | Gameplay | MVP | Partial | — | Pose Input, Game Manager, Audio Feedback |
| 6 | Round UI / HUD (inferred) | UI | MVP | Partial | — | ColorJump Game Logic |
| 7 | Calibration Screen (inferred) | UI | MVP | Not Started | — | Pose Input, Stick Figure Renderer |
| 8 | Main Menu | UI | MVP | In Progress | — | Game Manager |
| 9 | End-of-Session Summary (inferred) | UI | MVP | Not Started | — | Game Manager |
| 10 | Mirror the Word Game Logic | Gameplay | Vertical Slice | Not Started | — | Pose Input, Game Manager, Audio Feedback, Round UI/HUD |

---

## Categories

| Category | Description | Systems in This Game |
|---|---|---|
| **Core** | Foundation systems everything depends on | Pose Input, Stick Figure Renderer, Game Manager |
| **Gameplay** | The mechanics that teach English | ColorJump Game Logic, Mirror the Word Game Logic |
| **Audio** | Sound feedback | Audio Feedback |
| **UI** | Player-facing displays and screens | Round UI/HUD, Calibration Screen, Main Menu, End-of-Session Summary |

---

## Priority Tiers

| Tier | Definition | Systems | Target |
|---|---|---|---|
| **MVP** | Core loop functional and testable. Without these, the thesis hypothesis can't be tested. | #1–9 | Thesis defense |
| **Vertical Slice** | Second minigame polished. Demonstrates the minigame framework generalizes. | #10 | This week (after ColorJump is polished) |

---

## Dependency Map

### Foundation Layer (no dependencies)

1. **Pose Input** — everything body-tracking depends on this; the entire thesis fails if UDP is unreliable
2. **Game Manager / Session State** — global scoring and scene transitions; ColorJump calls `AddScore()` on this singleton
3. **Audio Feedback** — standalone SFX manager; must exist before game loop testing (ADHD-First Pacing requires instant audio)

### Core Layer (depends on Foundation)

1. **Stick Figure Renderer** — depends on: Pose Input
2. **ColorJump Game Logic** — depends on: Pose Input, Game Manager, Audio Feedback
3. **Mirror the Word Game Logic** — depends on: Pose Input, Game Manager, Audio Feedback, Round UI/HUD

### Feature Layer (depends on Core)

1. **Round UI / HUD** — depends on: ColorJump Game Logic (round state, countdown, word display)
2. **Calibration Screen** — depends on: Pose Input (confirm detection), Stick Figure Renderer (body preview)

### Presentation Layer (depends on Feature)

1. **Main Menu** — depends on: Game Manager (scene transition API)
2. **End-of-Session Summary** — depends on: Game Manager (final score + mastery list)

---

## Recommended Design Order

| Order | System | Priority | Layer | Est. Effort | Notes |
|---|---|---|---|---|---|
| 1 | Pose Input | MVP | Foundation | S | Already implemented — document contracts and failure modes |
| 2 | Stick Figure Renderer | MVP | Foundation | S | Already implemented — document visual spec |
| 3 | Game Manager / Session State | MVP | Foundation | S | Define session contract: what data it owns, how mastery is tracked |
| 4 | Audio Feedback | MVP | Foundation | S | Tiny system — document clip assignments and bus routing |
| 5 | ColorJump Game Logic | MVP | Core | M | Core learning mechanic — most thesis-critical GDD |
| 6 | Round UI / HUD | MVP | Feature | S | 6-year-old readability requirements; word size, contrast, timer clarity |
| 7 | Calibration Screen | MVP | Feature | S | Prevents invisible pose-not-detected failures |
| 8 | Main Menu | MVP | Presentation | S | Esteban's responsibility — document scene transitions expected |
| 9 | End-of-Session Summary | MVP | Presentation | S | The mastery reveal moment — thesis hypothesis evidence screen |
| 10 | Mirror the Word Game Logic | VS | Core | M | Build after ColorJump polished; reuses all Foundation systems |

*Effort: S = 1 design session, M = 2–3 design sessions.*

---

## Circular Dependencies

- None found.

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|---|---|---|---|
| **Pose Input** | Technical | UDP bridge drops on slow machines or poor webcam drivers; all gameplay input fails silently | Calibration screen confirms pose detection before any round starts; error display in stick figure viewport |
| **Mirror the Word Game Logic** | Technical | Pose comparison accuracy — how similar is "similar enough"? 100% accuracy is impossible with MediaPipe noise | Use landmark angle thresholds + tolerance window (not pixel-perfect matching); start with 3–4 simple poses |
| **ColorJump Game Logic** | Design | 4-second timer may be too short or too long for 6-year-olds with ADHD; threshold `0.15` may not map well to real-world standing positions | Expose `roundTime` and `moveThreshold` as inspector fields (already done); playtest with target-age kids before defense |

---

## Progress Tracker

| Metric | Count |
|---|---|
| Total systems identified | 10 |
| Design docs started | 0 |
| Design docs reviewed | 0 |
| Design docs approved | 0 |
| MVP systems designed | 0 / 9 |
| Vertical Slice systems designed | 0 / 1 |

---

## Next Steps

- [ ] Design MVP systems in order (run `/design-system [system-name]`)
- [ ] Start with: `/design-system pose-input` (Foundation, already exists — document it)
- [ ] Run `/design-review` on each completed GDD
- [ ] Finish ColorJump polish before starting Mirror the Word (#10)
- [ ] Run `/gate-check pre-production` when all 9 MVP GDDs are approved
