# Game Concept: Move & Learn

*Created: 2026-04-16*
*Status: Draft*
*Author: Martin (Proyecto de Grado)*

---

## Elevator Pitch

*Move & Learn* is an educational body-motion game where kids aged 6–8 with ADHD learn English vocabulary by physically stepping, posing, and gesturing in front of a webcam. MediaPipe tracks their body and drives an on-screen stick figure that responds to English prompts in real time.

> **10-second test:** "When the game says BLUE, I step onto the blue platform with my real body. The game praises me. I see my English getting better on screen." — passes.

---

## Core Identity

| Aspect | Detail |
| ---- | ---- |
| **Genre** | Educational / Movement-based / Minigame anthology |
| **Platform** | PC (Windows) + USB webcam |
| **Target Audience** | Spanish-speaking kids aged 6–8 with ADHD learning English (ESL) |
| **Player Count** | Single-player |
| **Session Length** | 2–5 minutes (one session = 10 rounds) |
| **Monetization** | None (undergraduate thesis / Proyecto de Grado) |
| **Estimated Scope** | Small (2 weeks solo, polish existing code + one collaborator on menu) |
| **Comparable Titles** | Nintendo Switch Sports, Just Dance Kids, Endless Alphabet |

---

## Visual Identity Anchor

The seed of the art bible — everything visual flows from this.

**Direction:** **Motion-Capture Skeleton Aesthetic** — the player *is* the stick figure. No character model, no clothing, no cosmetic layer between the kid and the game.

**One-line visual rule:** *The body on screen is the body in front of the camera — nothing more, nothing less.*

**Supporting principles:**
1. **High-contrast, saturated colors** for all gameplay elements (platforms, text). *Design test:* If a color is harder to distinguish than a crayon-box primary, we don't use it.
2. **Minimal background chrome** — no decorative art that competes with the body. *Design test:* If removing a visual element doesn't hurt the game, it stays removed.
3. **Oversized, readable typography** (TextMeshPro, bold, sans-serif). *Design test:* A 6-year-old beginner reader must recognize the target word in under 1 second.

**Color philosophy:** Six saturated primaries (RED, BLUE, GREEN, YELLOW, ORANGE, PURPLE) mapped to the same RGB values consistently across every minigame and screen. No palettes beyond these six for gameplay-critical elements.

---

## Core Fantasy

> *"When the game says BLUE, I become the one who finds BLUE — with my whole body."*

The player does not *control* a character; the player *is* the character. English vocabulary stops being something on a page and becomes something the player's body knows how to answer. For a kid with ADHD who struggles to sit through traditional English classes, this is the promise: **"You can learn by moving."**

---

## Unique Hook

It's like *Nintendo Switch Sports*, **and also** every action teaches an English word — no controllers, no reading tests, no menus between play and learning.

The hook is not MediaPipe itself. The hook is **embodied vocabulary recognition**: the word *is* the verb, and the verb *is* the body's action.

---

## Player Experience Analysis (MDA Framework)

### Target Aesthetics (What the player FEELS)

| Aesthetic | Priority | How We Deliver It |
| ---- | ---- | ---- |
| **Challenge** (mastery) | **1** | Progressive mastery of English color vocab, visible per-session score |
| **Sensation** (sensory pleasure) | **2** | Physical movement, audio feedback, bright color flashes on correct answers |
| **Submission** (low-stress flow) | **3** | Short 2-minute loops, no fail state, no punishment — only "Try again!" |
| **Discovery** | **4** | Round-to-round variety (different color pairs), end-of-session mastery reveal |
| Fantasy | N/A | — |
| Narrative | N/A | — |
| Fellowship | N/A | (Single-player MVP) |
| Expression | N/A | (Not a creation game) |

### Key Dynamics (Emergent player behaviors)

- Kids will **physically anticipate** — starting to step toward a color before consciously reading the word (target learning outcome)
- Kids will **replay immediately** after a session ending — the 2-minute loop is short enough to not feel like a commitment
- Kids will **show parents/siblings their score** — visible progress naturally invites sharing

### Core Mechanics (Systems we build)

1. **Body-tracked lateral displacement** — MediaPipe hip landmarks (23, 24) drive left/right stick-figure movement
2. **Word-prompt round system** — 4-second rounds, random target color, two candidate platforms
3. **Session-level progression feedback** — end screen shows score (X/10) and colors mastered

---

## Player Motivation Profile

### Primary Psychological Needs Served (Self-Determination Theory)

| Need | How This Game Satisfies It | Strength |
| ---- | ---- | ---- |
| **Autonomy** | Player chooses when to step, which direction; no forced tutorials | **Supporting** |
| **Competence** | Score, streaks, per-session mastery display; kid sees skill grow | **Core** |
| **Relatedness** | Minimal in MVP (single-player). Post-thesis: classroom / family co-play | **Minimal** |

### Player Type Appeal (Bartle Taxonomy)

- [x] **Achievers** — score, streaks, "mastered" badge per color — HIGH
- [x] **Explorers** — new word combinations each round — LOW (limited by MVP scope)
- [ ] **Socializers** — not served in MVP
- [ ] **Killers/Competitors** — limited (beat own score only)

### Flow State Design

- **Onboarding curve:** Calibration screen (30 seconds) → first round (4 seconds) → no explicit tutorial. The game teaches itself by letting the kid try once.
- **Difficulty scaling:** MVP uses fixed difficulty (all 6 colors, 4-second timer). Post-thesis: adaptive — remove mastered colors, shorten timer.
- **Feedback clarity:** Every round ends with a clear verbal prompt ("Great job!" / "Try again!"), an audio cue, and a score update.
- **Recovery from failure:** No failure state. Wrong answers say "Try again!" and immediately roll to the next round. No dead screens.

---

## Core Loop

### Moment-to-Moment (30 seconds)
Target word appears on screen (e.g., "BLUE"). Two colored platforms shown (one left, one right). Kid physically steps left or right. If they land on the target color → "Great job!" + score +10. If wrong or timeout → "Try again!". Next round begins.

### Short-Term (2 minutes = one session)
Kid plays 10 rounds. Score accumulates. Streaks build. Session ends with summary screen: "You got 7/10! Mastered: RED, BLUE, GREEN."

### Session-Level (5 minutes)
Kid replays once or twice ("again, but better"). Each session is self-contained — no carry-over state in MVP.

### Long-Term Progression
Across multiple sittings, kid builds recognition vocabulary. *(Post-thesis: persistent mastery tracking across sessions; unlock new minigames.)*

### Retention Hooks

- **Curiosity:** Which color pair comes next? Can I beat my last score?
- **Investment:** Visible mastery list at end of session.
- **Social:** Showing parents/siblings the score.
- **Mastery:** The kid *feels* themselves recognizing words faster each round.

---

## Game Pillars

### Pillar 1: Body as Controller

The player's real body *is* the input. No keyboard, no mouse. English is learned through movement, not memorization.

*Design test:* If we can choose between a keyboard feature and a body feature, we pick the body.

### Pillar 2: ADHD-First Pacing

Every round ≤ 10 seconds. Feedback instant (<0.5s). No long tutorials. No punishment screens. Losing focus costs ONE round, not a lesson.

*Design test:* If a choice makes a kid wait more than 2 seconds between actions, we cut it.

### Pillar 3: Visible Progress Every Round

Score, streaks, mastered-words list — kids see themselves getting better in real-time. No abstract long-term goals.

*Design test:* If an action doesn't produce visible feedback on screen, we redesign it.

### Pillar 4: English Through Verbs, Not Vocabulary Lists

The game never shows flashcards. Words appear as commands the body must answer. Learning = recognition + action.

*Design test:* If a mechanic can be replaced by Anki-style flashcards, it doesn't belong in this game.

### Anti-Pillars (What This Game Is NOT)

- **NOT a flashcard app** — memorization without embodiment is exactly what the target audience has already failed at.
- **NOT a full English curriculum** — we teach vocabulary recognition, not grammar or conversation.
- **NOT for neurotypical adults** — every UX choice (pacing, difficulty, tutorial presence) favors ADHD kids 6–8.
- **NOT a commercial product** — thesis scope. No multiplayer, no monetization, no cloud saves, no analytics.

---

## Inspiration and References

| Reference | What We Take From It | What We Do Differently | Why It Matters |
| ---- | ---- | ---- | ---- |
| **Nintendo Switch Sports** | Body-as-input, short session satisfaction, physical fun | Replaces motion controllers with pure camera-based body tracking | Validates that embodied play is mainstream-fun, not a gimmick |
| **Terraria / Minecraft** | Visible micro-progression, "one more session" dopamine | Compresses progression from hours to seconds (per-round mastery) | Validates that chase-loops work even in short sessions |
| **Just Dance Kids** | Camera-driven body mimicry, kid-friendly pacing | Replaces music/rhythm with language/vocabulary | Validates that kids will engage with webcam-based games at home |

**Non-game inspirations:**
- **Total Physical Response (TPR)** — language teaching method where learners respond to commands with full-body actions. Proven pedagogy for early ESL, especially for kids who struggle with traditional instruction.
- **Montessori movement-based learning** — children learn abstract concepts (numbers, letters, colors) through physical manipulation.

---

## Target Player Profile

| Attribute | Detail |
| ---- | ---- |
| **Age range** | 6–8 (early primary school) |
| **Gaming experience** | Casual — Roblox, Minecraft, Switch Sports level of familiarity |
| **Attention profile** | Clinical or suspected ADHD — focused tasks ~2–3 minutes max |
| **Time availability** | 5–15 minutes after school or before screen-time limits |
| **Platform preference** | Family PC with webcam, or school computer lab |
| **English level** | Early ESL — limited reading, basic word recognition |
| **What they're looking for** | Learning that doesn't feel like school homework |
| **What would turn them away** | Long tutorials, reading-heavy menus, "wrong answer" punishment, slow pacing |

---

## Technical Considerations

| Consideration | Assessment |
| ---- | ---- |
| **Recommended Engine** | **Unity** — already chosen, existing code (C#), good Windows desktop support |
| **Key Technical Challenges** | MediaPipe tracking reliability in varied lighting; Python↔Unity UDP bridge stability |
| **Art Style** | Motion-capture skeleton (white bones + cyan joints) + high-contrast platform colors |
| **Art Pipeline Complexity** | **Low** — primitive shapes + TextMeshPro; no 3D modeling, no rigging in MVP |
| **Audio Needs** | Minimal — 2 SFX clips (correct / wrong) + optional background loop |
| **Networking** | None (MediaPipe UDP is local loopback only) |
| **Content Volume** | 1 minigame (ColorJump), 6 colors, ~10 rounds per session |
| **Procedural Systems** | Round generation (random color pairs) — trivial |

**Existing codebase (already working):**
- `PoseReceiverUDP.cs` — MediaPipe landmark receiver over UDP
- `StickFigureUDP.cs` — 33-landmark stick-figure renderer (spheres + line renderers)
- `ColorJumpGameUDP.cs` — complete ColorJump game loop
- `MainMenu.cs` / `Menumanager.cs` — menu work in progress (collaborator: Esteban)

---

## Risks and Open Questions

### Design Risks
- **Loop fatigue after 5 sessions** — only 6 colors, possibly feels repetitive. *Mitigation:* streak bonuses, end-of-session mastery display, stretch goal of 2nd minigame.
- **Calibration friction** — kid may not position correctly in camera frame. *Mitigation:* on-screen overlay showing ideal body position during calibration screen.

### Technical Risks
- **MediaPipe tracking fails in poor lighting** — home/classroom lighting varies wildly. *Mitigation:* calibration screen requires visible pose detection before game starts; documented minimum lighting in thesis.
- **UDP bridge latency** — noticeable lag ruins the embodied feeling. *Mitigation:* already local loopback; measure end-to-end latency as part of thesis evaluation.

### Market Risks
- **Not applicable.** Thesis has no commercial goal.

### Scope Risks
- **Temptation to rig Amy** — rigged avatar is 1–2 weeks of work alone. **Locked answer:** ship stick figure; Amy is post-thesis.
- **Temptation to add Minigame 2 before polishing Minigame 1** — explicit user rule: finish Minigame 1 first.

### Open Questions
- **Playtest access:** can we get 3+ kids in target age range to test before defense? *(If yes → stronger thesis evidence.)*
- **Thesis advisor expectations:** does the advisor expect Amy rigged avatar, or is stick figure acceptable? *(Answer affects scope lock.)*

---

## MVP Definition

**Core hypothesis:** *Kids aged 6–8 with ADHD can recognize English color vocabulary through short, body-motion gameplay sessions.*

### Required for MVP
1. **Stick figure renderer** — already works, ship as-is
2. **ColorJump minigame** — 6 colors, 10-round session, score, audio feedback
3. **Main menu** — Start / Exit, no settings (Esteban in progress)
4. **Calibration screen** — kid stands in frame, confirms stick figure is visible, taps to start
5. **End-of-session screen** — "You got X/10! Mastered: [list]" + Replay / Menu buttons

### Explicitly NOT in MVP
- Amy rigged avatar (stick figure IS the concept)
- Second or third minigame (stretch tiers only)
- Adaptive difficulty
- Cross-session persistence / save data
- Settings menu / audio volume controls
- Localization (Spanish UI, English game content)
- Teacher / classroom mode

### Scope Tiers

| Tier | Content | Features | Timeline |
|------|---------|----------|----------|
| **MVP** | ColorJump, stick figure, menu, calibration, end screen | Core loop + session summary | **Week 2 (thesis defense)** |
| **Vertical Slice** (stretch) | + Minigame 2: **Mirror the Word** (pose imitation, full body) | Only after MVP is polished | Days 10–14 |
| **Alpha** (stretch) | + Minigame 3: **Size Sort** (open/close arms, landmarks 15/16) | Only after Vertical Slice is polished | Future |
| **Full Vision** (post-thesis) | Amy rigged avatar, 5+ minigames, classroom mode, teacher dashboard | Full release | Not this thesis |

---

## Next Steps

- [ ] Validate concept with `/design-review design/gdd/game-concept.md`
- [ ] Configure Unity formally in `.claude/docs/technical-preferences.md` via `/setup-engine`
- [ ] Define visual identity fully via `/art-bible`
- [ ] Decompose into systems via `/map-systems` (likely: Pose Input, Minigame Framework, Round System, Session Summary, Main Menu)
- [ ] Author first system GDD (ColorJump) via `/design-system`
- [ ] Architecture doc via `/create-architecture`
- [ ] Gate-check readiness for Pre-Production via `/gate-check`
- [ ] Prototype playtest session with a target-age kid (thesis evidence)
