# Chapter 1 — story trigger audit (QF6, 2026-11-18)

**Rule from QF6 on:** no trigger may depend on one exact moment or spot. A trigger fires when the story state says it
should **and** 7 is at *or past* it (further along the route, or in any later area). Every step the flow waits on is
declared (`Chapter1Director.Step`) with the area its trigger lives in; the **story watchdog** (`StoryWatchdog`) re-shows
the objective if it goes missing while the player has control, and if 7 is ever further along than the step the story
waits for (4 s, outside cutscenes / resets) it logs `WATCHDOG` and returns her (white fade, no harm) to that step's spot.
In a correct flow the second part never fires — the playtest counts it (must stay 0).

Areas (`Chapter1Director.AreaOf`, in route order): 0 cell · 1 cell block · 2 old passage + ruins · 3 Scree hall ·
4 vision chamber · 5 mining cut · 6 terrace + ramp · 7 bay floor. Only traversal links join them, in this order.

Checkpoints (restart with d-pad down / Backspace, **Continue save**, **Scene Select entry**): 0 cell · 1 escape (cat
leads) · 2 old passage + ruins · 3 Scree guard · 4 vision + take the shape · 5 the buried lock + the lunar door · 6 city reveal · 7 saucer
rescue + beam · 8 aboard · 9 galley · 10 free exploration. QF6 added 3, 5 and 7 and makes them persistent (quit and
Continue resumes there; before QF6 only "aboard" was saved).

## Dan's 0.10.1 soft-lock — the cause

Dan's own `Player.log` (0.10.1, `%USERPROFILE%\AppData\LocalLow\Dan\7 The Observer — Demo\`): every cat-leash line up to the
vision chamber, then 7 at z −39.3 (through the open lunar door), down the ramp, last at (21.56, −10.00, −63.34) on the bay
floor, quit. There is **no** `dodecahedron -> Hand/Hip` line (`CarriedShape.Set` always logs one): he never took the shape.
Save: `seven_ch1_aboard = 1` from an earlier playthrough; checkpoints were not saved in 0.10.1.

`Scene6()` opened the door, played lines 9–11, then `TakeShape()` waited for X "Take" at the pedestal (1.4 m). The city
reveal (6b) and the bay / saucer objective were in `Scene7And8()`, which only started **after** the take. The door was
already open, so walking out without pressing "Take" left the flow waiting for ever: no 6b, no lever prompt, no saucer.
The blocking order came in Round 10; **QF5 made it likely**: mirroring the mechanism to the right of the door (x 19.35 →
13.65) moved 7, at the end of the door scene, from 2.6 m to 6.4 m from the pedestal, with the open door right beside her.
The cat leash, the fade-and-reappear and the pouch had nothing to do with it (the leash kept the cat with him all the way).
The autopilot always pressed "Take", so every check run passed. **Reproduced** with the new playtest and `-noqf6fix`.

**Fix (with Dan's QF6 notes):**
- **No "Take" prompt and no button.** She takes the shape as soon as she walks up to the pedestal (within 1.5 m). The beat
  stays: she looks at it in her hand, then it goes into the pouch as before.
- **No point of no return.** The door opens in the mechanism scene. The walkable doorway behind it (zone `VisionExit`)
  and the one-way drop into the mining cut stay closed until she has the shape.
- If she goes to the doorway first, she turns and draws the shape to her with telekinesis, and then it goes into the
  pouch (`TakeShapeFallback`).
- Nothing in the chapter uses a "Take" prompt any more. `-noqf6fix` restores the 0.10.1 behaviour for the repro.

## Moon level (`Chapter1Director`)

| # | Trigger | Condition (fires when) | How a player could miss it | Fallback |
|---|---|---|---|---|
| 1 | Cat walks to his mark outside the cell | flow start | blocked / hitched | `UntilOr` 4 s → placed on his mark (QF4) |
| 2 | Scene 1 first contact | X "Look" at the cell door (0.8 m) | never goes to the door, idles, runs about | 20 s after the cat arrived / 45 s after he was sent → she walks to the door (QF4, opening test 25/25) |
| 3 | "Stand back from the door" | 7 at z < 0.15 (anywhere in the back half of the cell) | — (the cell holds her; stepping back is always possible) | objective re-shown by the watchdog |
| 4 | Cat to the hidden release, crate, down, back | scripted moves | blocked / hitched | `UntilOr` 25 / 6 / 4 / 8 s → placed (QF4) |
| 5 | "Step out" | z > 2.2 (anywhere past the doorway) | — (only way out) | — |
| 6 | Scene 3 walk-and-talk | x > 4.5 (at or past) | hurries far ahead, backtracks | fires wherever she is past 4.5 |
| 7 | Gate hint + "Find another way out" | on the bulkhead top **or** x > 15.3 (at or past) | jumps down at once, never stands on top | the "past" half |
| 8 | Scene 4 the older wall | X "Look closer" (1.5 m) | walks around elsewhere | the wall is the only way on (gate guarded, no other exit); objective stays |
| 9 | Cat route (prison, passage + ruins, mining cut, bay) | each stop waits for "7 at X" **or 7 level with / past it** (QF5) | hurries past | + leash: > 6 m behind / not closing 3 s → trots / fades in behind her (QF5, cat-follow 25/25) |
| 10 | "Clear the fallen slab" | on terrace C (the only way to the stair) → RT within 7 m | — | stair blocked until moved; prompt stays |
| 11 | Hall entrance → **checkpoint 3** | z < −9.2 (at or past) | — | — |
| 12 | Scene 5 guard + stone | being seen → reset to the entrance (never harm); RT on the loose stone (12 m) | throws the stone and dawdles until the guard is back | being seen resets the stone too; prompt re-armed after every reset |
| 13 | Slipped past → "Come on." → **checkpoint 4** | z < −24.4 (at or past) | — | — |
| 14 | Into the chamber → **"Take the shape"** (QF7: the objective from the start) | z < −30 (at or past) | — | — |
| 15 | **Take the shape → the vision** (QF7: one step) | **automatic:** walks up to the pedestal (1.5 m) or anywhere into the alcove mouth (at or past), no prompt or button; the vision plays as she touches it, and she holds it when it ends → into her pocket → **checkpoint 5** | walks about the chamber | sealed door + buried lock: the only way on; objective stays |
| 16 | **"Uncover the lock"** (QF7: buried under a collapse in the present, shown intact in the vision) | RT telekinesis on the collapse (`MoonLayout.TkRange`, the pile pulses faintly) | — | sealed door; the pile's ground obstacle is removed only when cleared; objective stays |
| 17 | **"Open the door"** | X "Touch" at the exposed lock (1.0 m, right of the door as in the vision) → door opens, lines 9–11 → zone `VisionExit` opens → checkpoint 6 | — | QF6/QF7: the doorway stays closed until the lock is used (she has the shape since the vision) |
| 18 | City look blend | z < −38.6 (at or past) | — | — |
| 18b | **QF9: the terrace fight** (the two Scree before the reveal, fighting tutorial) | she is out of the mining cut onto the terrace (z < −41.3, at or past) → they block the way; an invisible arena (x 13.6–19.6, z −45.3 to −41.45) keeps the fight from being walked around or fallen out of; both Scree down (takedowns) → the reveal | — (the arena holds her there) | knocked down = white fade, fight restarts from its start (no game over); watchdog: 90 s with no blow landed or taken → both Scree sit down and the reveal goes on |
| 19 | 6b city reveal | z < −43.4 (at or past), or the fight won (QF9) | arrives from the cut fast; (before QF6: never, because 17 blocked) | QF6: if she is already well past (down the ramp / bay) the reveal plays from the terrace, then she is put back where she was |
| 20 | Scene 7 lines → **checkpoint 7** → "Get down to the bay…" | end of 6b | — | — |
| 21 | Down in the bay | y < −9.5 (at or past) | — | — |
| 22 | Release lever | RT within 8.5 m of the lever | — | the saucer is clamped: the only thing to do; prompt stays |
| 23 | Scene 8 at the edge of the light | within 3.9 m of the beam centre | walks across the bay first | fires whenever she comes back |
| 24 | Boarding + liftoff | scripted | — | saves "aboard" (checkpoint 8) |

Cross-cutting: **fall reset** (white fade → last safe ledge) never undoes a fired trigger (all waits are "at or past" by
position) and keeps prompts / objectives; **restart** (d-pad down) reloads the current checkpoint with its own world state
(`SetupCheckpoint`: doors, wall, slab, sealed door, shape on the pedestal or at her hip, city look, cat); **Continue**
starts there too; **during another scene**: no trigger is armed during a cutscene (the flow is sequential), dialogue-only
walk-and-talks never block movement; **while carrying the shape**: nothing waits on it except the take itself (17).
Teaching prompts never block (they clear on done or when a cutscene takes over).

## Venus part 2 (`VenusDirectorPart2`, Round 12) — every wait is an `Until` with a hint (45 s) and a skip (2 min)
| Step | Waits for | Fires at or past | Skip |
|---|---|---|---|
| The way down | 7 in the control chamber (x > 104.5, below y −28) | anywhere past the entrance | placed at the chamber entrance |
| Each device (3) | the device count goes up | the device is off however it was done | the next device is disabled for her |
| Containment | the console (X) | — | the barrier closes |
| B06 | on the bike; then the temple court (x < 46, z > 190) | anywhere in the court | mounted / placed riding in the court |
| C08 | X at the guardian (within 4 m) | — | the talk starts |
| C09 | the cube chamber (z > 258), then X at the cube | anywhere in the chamber | placed / the touch starts |
| B07 | on the bike, near the ship (z < 40), at the ship (z < 12) | anywhere past each line | placed riding near the ship |
| Return visit | X at the ramp foot ("Board the saucer") | — | — |

## Saucer (`Chapter1Aboard`)

| # | Trigger | Condition | How a player could miss it | Fallback |
|---|---|---|---|---|
| 25 | Scene 9 (the larger interior) | arrival | — | — (saved: checkpoint 8) |
| 26 | "Find the kitchen" → scene 10 | X "Open the fridge" within 1.1 m of the fridge | explores the deck first | free roam; prompt + objective stay |
| 27 | Scene 10 the galley | cutscene (lines, silences) | quits during it | saved at its start (checkpoint 9): Continue replays it |
| 28 | "Set course for Venus" → placeholder | X "Set course" at her galley seat (1.1 m), repeatable | — | always available in free exploration (checkpoint 10) |

## Points of no return (QF6 rule, whole game)

**Rule:** the player must never be able to pass a point she cannot come back from while something the story needs is
still behind it. Every one-way move has to meet one of these:
- **(a)** the way forward stays closed until everything behind it is done, or
- **(b)** she can walk back.

Free falls into a chasm are not one-way: a white fade returns her to `ChapterSeven.LastSafe`. One-way moves are the
assisted jumps, hang-downs and let-gos in `MoonTraversal` (jump links only go `From → To`). Wall climbs, grabs and
mantles go up, which is forward on this route.

| # | Crossing | Kind | Behind it (story) | Verdict |
|---|---|---|---|---|
| 1 | Cell door / laser door | walk, both ways once open | — | (b) |
| 2 | Catwalk gap (x 8.1 → 10.65) | assisted jump, east only | Cell + scenes 1–2. These are done before the cell door opens (`CellDoor` zone). The scene 3 trigger (x > 4.5) comes before the gap and fires at or past it. The cat comes along by route and leash | (a) |
| 3 | Pipe run | crouch walk, both ways | — | (b) |
| 4 | Bulkhead (grab up on the west, jump down on the east) | one-way east | Gate hint fires on the top **or** past it. Nothing is waiting behind | (a) |
| 5 | Cracked wall climb → shelf-edge hang-down → shaft lip → let go onto the ruins ledge | one-way down | Scene 4 ("Look closer"). The climb base is in zone `Passage0`, which only opens when the wall sinks at the end of scene 4 | (a) |
| 6 | Ruins gap (x 10.7 → 8.45), terrace B mantle, terrace C grab | jump west, then climbs up | Nothing. The slab is ahead, on terrace C | (a) |
| 7 | Stair terrace C → ruins floor → hall | walk, both ways (opens when the slab is moved) | Slab (counted as done at the hall entrance; the stair is the only way down) | (b) |
| 8 | Hall ↔ vision chamber | walk, both ways | — | (b) |
| 9 | **Lunar door → doorway → hang down into the mining cut** (Dan's "stairs past the open door") | **one-way down** | **Vision, mechanism, the shape.** 0.10.1: the doorway was walkable as soon as the door opened, with the shape still on the pedestal → Dan's soft-lock | **(a) since QF6:** zone `VisionExit` (the doorway and its hang-down spot) opens only after the door scene (QF7: she has the shape from the vision on; the door opens only after the buried lock is uncovered and touched) |
| 10 | Mining cut wall climb → terrace | up | Nothing (the pit) | (a) |
| 11 | Terrace → ramp → broken ramp end hang-down → scaffold gap → scaffold edge hang-down → bay | one-way down | The 6b city reveal. It fires at z < −43.4, before the ramp end at z −55, and also plays if she is somehow past it. The lever, scene 8 and the beam are all ahead, in the bay | (a) |
| 12 | Bay → saucer (the beam) | story | Nothing left on the Moon (lever and scene 8 come first) | (a) |
| 13 | Saucer deck, galley nook, art TV nook | one deck, all walkable both ways; no traversal moves | — | (b) |
| V1 | Venus (QF10): low walls with a mantle face (the garden-wall shelf beside the stopping area; Moon: terrace B step) — climbable **anywhere** along the face | climb up / jump down anywhere along it | Nothing (exploring). Jump down works anywhere along the same face | (b) |
| V2 | Venus (QF10, 6f): the gully at the collapse (z 118–128) | the slab bridge, both ways once laid | VEN_B03: the bridge only exists after the telekinesis beat; the gully is a natural boundary across the whole valley (no way round on foot); falling in = back to the last safe spot; the bridge stays for return trips (saved `VEN_B03_bridge`) | (b) |
| V3 | Venus: the maintenance gate | walk / ride, opens after C04 | Its zones open only after C04 (VEN_S03) | (b) |
| V4 | Venus (Round 12): the root gate → the root pit (steps) → the root tunnel → the root chamber → the deep tunnel → the control chamber | walk, both ways (slopes and steps, no drops) | The gate opens only after C05 (VEN_S04); every step is walkable back up | (b) |
| V5 | VEN_05: the raised ledge in the south-west corner (1 m wall, `LedgeMantle`) | climb up / jump down anywhere along its face | The ledge device is up there; jumping down works anywhere along the same face | (b) |
| V6 | VEN_05: the creature troughs | walk, both ways | A creature that reaches her knocks her back (white fade, back on the last walkway spot) - never a fall, never a dead end | (b) |
| V7 | C07 → B06: after containment she is back beside her bike at the lookout (a cut) | story | Nothing is left below: the devices, the barrier and the creatures are done (saved); the tunnels stay open | (a) |
| V8 | B07: the ride back to the saucer, then aboard (C11) | story | The record is in her pocket (awarded once, saved); Venus can be revisited from the destination screen with its changed state | (a) |

Skips are tolerated. For example, a jump off terrace C that misses the stair: the slab is treated as done at the hall
entrance.

**Adding a one-way move?** Add it to this table. The zone or link that leads into it must be enabled only after the last
story step behind it, or add a way back.

## How this is checked (QF6 testing rule: under ~20 min of automated testing per round)
Repetition is no longer the safety net. The rules above are (at or past, the watchdog, no points of no return). The
checks:
- `-testspeed 2`: game time runs twice as fast.
- `-sceneselecttest`: every Scene Select entry starts with the right state and plays to the next.
- `-playtest 1`: one player-style run, main menu to "Set course", including the "skips the pedestal" habit.
- `-ch1aboard`: the galley.

The 25-run playtest and the 25-run cat-follow test are no longer standing checks. The switches still exist for
investigating a bug.

## Round 15 — Mars (2026-11-20)

| # | Place | One-way? | Verdict |
|---|---|---|---|
| M1 | West ramp down into the ravine (maintenance route) | no - a ramp both ways | ok |
| M2 | The broken causeway | closed until the slab is laid (TK); open both ways after | ok (the way forward stays closed until done) |
| M3 | East ramp + the service gate | the gate opens from the ramp side (panel X), then both ways | ok |
| M4 | The aligned spans to the temple island | closed until section 6 aligns them; both ways after | ok |
| M5 | The arena (Viper's barriers, the moving section, the broken strip) | each gate opens as its pylon is disabled; the moving section opens every 5.4 s; the walkway (TK) stays | ok - nothing behind her is needed |
| M6 | The pier walk (steps down beside the west span) | steps both ways | ok |

Every Mars wait is `MarsDirector.Until` (hint 45 s / 25 s riding, skip 2 min / 60 s riding); riding / walking steps fire at
or past their place (route points ahead count).

## The Asteroid (Round 16)

| # | Place | One-way? | Verdict |
|---|---|---|---|
| A1 | The service entrance (receiving chamber → tunnel) | closed until the scanner is turned and the door control used; both ways after | ok (the way forward stays closed until done) |
| A2 | The three causeway junctions | each span is closed (turned for the carriers) until she aligns it; both ways after | ok |
| A3 | The hall seals (AST_04): the shutters close, she leaves through the maintenance opening, which shuts behind her | the hall is closed until AST_07 | ok - nothing she needs is behind her (the bike waits at the hall plaza; she returns through the hall after the fight); a slow escape is a white fade back with more time, never a fail |
| A4 | The cargo platforms across the pit (control 1) | closed until control 1; both ways after | ok |
| A5 | The foundry passage (control 2) | closed by steam until control 2; both ways after | ok |
| A6 | The final transport assembly (AST_06): it retracts, she drops 4.4 m to the maintenance ledge | the ledge's only way out is the wall climb (A) up to the west works; the assembly slides back out once she is up (both ways after); a climb that takes over 150 s is helped up (the skip) | ok |
| A7 | The west passage back to the hall (AST_07) | closed until the diverter (control 3); both ways after | ok |
| A8 | The temple archive | shuttered until the fight is won; both ways after | ok |

Every asteroid wait is `AsteroidDirector.Until` (hint 45 s / 25 s riding, skip 2 min / 60 s riding); riding / walking
steps fire at or past their place (route points ahead count); the junction levers work from the bike too.
