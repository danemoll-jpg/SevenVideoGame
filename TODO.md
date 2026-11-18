# Active Development Plan — 7: The Observer

## Current Objective
Round 9 is done (release 0.9.0). **Round 10 is queued in `Docs/ROUND10.md`** (Dan, 2026-11-17: the galley scene ends Chapter 1) — move it here when it starts. Note: `Docs/DIALOGUE.md` already holds Dan's Round 10 galley text; `voice_lines.json` was deliberately left at the Round 9 set (83 lines) until Round 10 regenerates it.

## Standing rules
- 7 never kills · strictly monochrome · non-sexual presentation · 7 is clearly an adult (details in `CLAUDE.md`).
- Per-item status at the end of every round: **done / not done / deferred (why)**. No silent partial completion.
- Short results: ~20 lines, one line per item + its one check, plus a "For Dan to check" list.

## Completed Tasks
- **Round 9 — Chapter 1 as the tutorial — done (2026-11-17), exe-verified off-screen; release 0.9.0 (`game-900`).**
  Traversal (jump, grab, hang, shimmy, climb, climb down, let go, wall climb, mantle, crouch), forgiving falls, the
  telekinesis slab, the cat leading the routes, no permanent legend (teaching + context prompts, Controls page, Button
  hints), Kyle's dodecahedron, the lunar door chamber (Dan's addition) and the video vision with the new scene 6 lines
  (Dan's addition). Status in "Round 9" below.
- **Round 7, Round 8, QF3, QF4 — done (2026-11-17), releases 0.8.0–0.8.2.** Laser cell door + hidden release + dark
  energy point in the cell, city reveal (6b), tractor-beam boarding, SFX, re-voiced 7, music, pause menu / Continue /
  Quit, free saucer exploration; saucer look pass, gallery sofa, Ratwing flip re-key, fluid fighting, launcher; phantom
  walls removed, curled lap cat, clean reveal, hidden cursor; opening watchdogs (25/25), clear dialogue cameras, the
  real Scree (1.9 m, tail tucked). Round 7/8/QF3 records in `TODO-ARCHIVE.md`; QF4 record below.
- **Quick fix QF3 — phantom walls, subtitles, curled lap cat, reveal band, excavators, mouse cursor — done
  (2026-11-17), exe-verified off-screen; release 0.8.1 (`game-801`) published.** Status in "Quick fix QF3" below.
- **Round 8 — saucer look + cat rig, gallery sofa/preload, Ratwing drop, fluid fighting, launcher + first release 0.8.0 —
  done (2026-11-17), exe-verified off-screen; Dan to play / install via the launcher.** Status in "Round 8" below.
- **Round 7 — Chapter 1 fixes, city reveal, SFX, re-voiced 7, music, pause menu / Continue — done (2026-11-17),
  exe-verified off-screen; Dan to play.** Per-item status in "Round 7" below; plan in `TODO-ARCHIVE.md` → "Round 7".
- **Round 6 — Chapter 1 "The Moon Prison" — done (2026-11-17), played by Dan.** Voiced (ElevenLabs "Seven"/"Cat",
  telepathy treatment, subtitles), cell → escape → Scree → first vision → impound → liftoff → aboard (galley, cat stays).
  Full record in `TODO-ARCHIVE.md` → "Round 6". Dan's feedback drives Round 7.
- **Round 6 — Chapter 1 "The Moon Prison" — done (2026-11-17), exe-verified off-screen (autopilot playthrough,
  0 soft-locks); Dan to play.** Voiced (78 ElevenLabs lines, telepathic cat), cell → Scree hall → vision → impound →
  liftoff → saucer galley → "To be continued". Per-item status in the Round 6 section below.
- **Round 5 — Kyle's art gallery TV on the saucer — done (2026-11-17), exe-verified off-screen; Dan to browse.**
  `Art/Gallery/` + `gallery.json` (all 24 on), loaded at runtime; TV nook with idle slideshow, browse / full view.
  Full record in `TODO-ARCHIVE.md` → "Round 5".
- **Round 5 — Kyle's art gallery TV on the saucer — done (2026-11-17), exe-verified off-screen; Dan to browse.**
  Record below ("Round 5").
- **QF2 + Round 4 — Ratwing cave scene — done (2026-11-17), exe-verified off-screen; Dan to watch.** Build now at
  `Unity\Builds\Seven\SevenDemo.exe`. Ratwing (2.1 m, 55k tris, scripted 38-bone rig with wing spars) hangs asleep,
  wakes, unfurls, drops and lands in a fog-lit cave (third demo-picker entry). Full record in `TODO-ARCHIVE.md` →
  "QF2 + Round 4". Old `Unity\Builds\FightDemo` can be deleted by Dan.
- **Round 4 + QF2 — done (2026-11-17), exe-verified off-screen; Dan to watch.** Build moved to
  `Unity\Builds\Seven\SevenDemo.exe`; Ratwing cave scene (rigged + keyframed in Blender) on the demo picker. Record below.
- **Round 3 — saucer mock-up — done (2026-11-16), exe-verified off-screen; Dan to play.** Demo picker (Fight / Saucer),
  16 m round saucer deck, rigged black cat (Rigify, from `cat_v1.glb`) that roams large and shrinks onto 7's lap when she
  sits, ship reveal over a foggy city. Full record in `TODO-ARCHIVE.md` → "Round 3".
- **Round 2 + QF1 — done (2026-11-16), exe-verified off-screen; Dan to play.** Right-stick orbit camera
  (camera-relative movement), telekinesis hold-to-lift showcase, outdoor fog/glow hill world (`FightDemo.unity`; room
  kept as `FightDemo_Room`, `-room`), reusable `Atmosphere` prefab (`Docs/ATMOSPHERE.md`), monochrome Xbox/keys legend
  (uGUI), fullscreen by default with F11 / View toggle, Kyle's reference images indexed. Full record in
  `TODO-ARCHIVE.md` → "Round 2 + Quick fix QF1". Open tuning ideas there: more ground fog, light shafts, softer terraces;
  telekinesis could use Kyle's floating wireframe shapes (`kyle_7_telekinesis.jpg`).
- **Round 1 — fight-demo proof of concept — done (2026-11-16), exe-verified off-screen; Dan to play.** 7 rigged in
  Mixamo (1.70 m, ~50k tris, metal cuff hair ties, flat glowing forehead dot) spars with Y Bot in a monochrome room:
  punches, kicks, block, dodge, telekinesis push, finisher → knockdown → get up → dazed → recovers. Full item-by-item
  record (substitute clips, hit timings, checks) in `TODO-ARCHIVE.md` → "Round 1".
- **7 base model v2 — done in chat (2026-11-16).** Tripo model `7_normal_v1.glb` → `Art/Source/7_normal_v2.glb`:
  pigtail texture darkened (×0.57) to match scalp hair; band hair ties covered by a separate mesh `7_hairtie_orbs`
  (two spheres r=0.017 at x=±0.042, y=0.960, z=-0.039 in source units) with white emissive material `7_hairtie_glow`.
  Script kept as `Tools/Model/fix_v1_to_v2.py`. Source facts: 0.98 m tall, ~1.95M triangles, one 4K base-color texture.
  Verified only with rough point-cloud previews, not in Blender or Unity yet.

## Round 9 — Chapter 1 as the tutorial: traversal + controls taught as you need them — done (2026-11-17), exe-verified off-screen; release 0.9.0
Plan text (and Dan's two additions to 8b) moved word-for-word to `TODO-ARCHIVE.md` → "Round 9". Previews + logs:
`Docs/Previews/r9/` (`chapter1/` full autopilot run, `opening/` 25× opening test, `soundcheck/`, `gallery/`,
`clip_measurements.txt`). Every run muted (output peak 0.000000 / listener 0), minimized-not-activated, Below Normal,
one heavy job at a time; monochrome max |R−G|,|G−B| = 0 over all 97 chapter shots.
0. **Safety checkpoint** — committed + pushed first (`66e77e2`: Dan's doc updates + the 11 new clips and `alt/` in LFS).
1. **Control scheme — done** (map in `CLAUDE.md` and the pause menu's Controls page). Changes to the proposal: keyboard
   re-centre moved C → **R** (C is crouch; middle mouse still works); **let go = B / Space / C**; **skip a line = Y / Enter**
   (A is jump now); the mouse orbits **without a button** and the cursor is locked while playing (right mouse is
   telekinesis); T stays as a hidden telekinesis alias. Saucer: X / E interact (A still works there). Fight demo unchanged.
2. **No permanent legend — done.** Legend hidden in the chapter and aboard (0 panels in every shot). Teaching prompts
   (glyph + words, lower middle, monochrome, once per playthrough, kept across checkpoint restarts): move + camera →
   interact ("X Look" at the cell door now starts scene 1; the 20 s watchdog still covers idling) → **jump (first gap)**
   → **crouch (pipe run)** → grab the ledge → hang (climb up · shimmy · let go) → telekinesis (fallen slab) → sit (saucer).
   Jump comes before crouch because the level puts the gap first. Each stays until done (min 2.2 s), fades 1 s after.
   Quiet context prompts afterwards ("A Climb", "B Climb down", "X Touch", "RT Telekinesis"…). Pause menu: **Controls**
   page (Xbox + keyboard) and Settings → **Button hints On / Off** (saved).
3. **Moves — done** (`ChapterSevenTraversal.cs`, data in `MoonTraversal.cs`): standing + running jump (assisted across
   known gaps: lands on the far side from anywhere within ~1 m of the take-off), ledge grab when jumping at a lip, jump-
   to-grab, hang, shimmy L/R, climb up, let go, climb down to a hang at an edge, wall climb, waist-high mantle, crouch-
   sneak toggle (the pipe run only takes her crouched). Hand IK keeps both wrists on the lip: **gap 0.022–0.040 m** on
   all 7 ledges (a runtime lift calibrates the hang height). Lips are pale worn stone / scuffed metal strips.
4. **Falls never kill — done.** A fall with no ground below → white fade → last safe ledge. Deliberate falls in the run:
   short hop into the catwalk gap, let go over the shaft, let go over the terrace gap → **3/3 reset**.
5. **Telekinesis before the Scree — done.** A fallen slab blocks the ruins stair; RT lifts it aside into the shaft; her
   energy point brightens to 1.9× during the lift (also during the Scree stone). Visual only.
6. **Routes — done (story beats and order unchanged):** prison after scene 3: broken catwalk gap → crouch under a low
   pipe run → bulkhead (jump-grab, climb, jump down); scene 4 passage: cracked wall climb → climb down onto the shaft lip
   → shimmy over the dark drop → let go into the ruins → jump → mantle → jump-grab + shimmy + climb → slab (RT) → stair
   → hall (scene 5 as before); after the vision: drop into a mining cut, climb its wall (2 climbs) out onto the terrace
   → reveal; to the impound: broken ramp → climb down + drop → scaffold → jump → scaffold → climb down + drop → bay.
7. **The cat leads — done.** Route mode: he walks / hops to the next ledge or stub and waits (sits, watches her) until
   she catches up; 27 stops logged, 0.5–3.5 m from her; never on a landing spot; 8 m catch-up rule kept in follow mode.
8. **Mixamo clips — done, all of Dan's used:** `hang_idle` (hang), `shimmy` + **mirrored copy** `shimmy_mirror` (shimmy
   R/L), `climb_up` (climb up; also the end of every wall climb, started at 17 %), `jump_to_hang` (jump-to-grab),
   `hang_drop` (let go when the floor is 2.0–2.9 m below the lip), `running_jump` (all jumps; root motion extracted, the
   arc is scripted), `fall_idle`, `landing` (soft landings + the standing jump's end), `landing_hard` (drops > 2.6 m),
   `wall_climb` (cracked wall ×1, mining cut ×2), `crouch_idle`, `sneak_walk` (crouch walk), `alt/7_jumping_up` (standing
   jump). **Stand-ins:** the **mantle = `climb_up` trimmed from 36 %** (`alt/7_climbing_a` is a 4 m walk-up + 2 m climb,
   `_b` a mid-wall climb cycle — neither fits waist height); **climbing down into a hang is procedural** (no clip).
   Not used: `alt/7_hang_idle_b`, `alt/7_running_jump_b` (the main clips fit better). Measurements:
   `Docs/Previews/r9/clip_measurements.txt`.
8b. **Vision object + lunar door + the video vision — done.** Glowing white wireframe **dodecahedron** (0.6 m, slow
   spin, faint pulse, lights the alcove) on a pale pedestal in an alcove; X "Touch" → edges flare → white wash. One
   constant (`MoonLayout.VisionShape`) swaps it for a tetrahedron / cube / octahedron. **Dan's door addition:** chamber and
   door after `ref_lunar_door.png`, in ruins — flowing pale ribs and vaults (two broken off), fallen chunks, skylight slits,
   low fog sheets; pointed-arch door in two leaves split by an S-seam with worn orbit / planet engravings; the round
   three-spoke mechanism beside it; floor circles with a line of small circles to the door; the same ribs through the
   hall exit and across the hall. **Dan's vision addition:** white wash → `vision_01_door.mp4` full-screen (12.1 s,
   1920×1044, height filled, sides cropped 1.7 % each, no stretch; copy in `Unity/Assets/Video/`, LFS) under the vision
   shimmer with the cat's two lines faint → white wash back. New scene 6 middle from `DIALOGUE.md` (4 new ElevenLabs takes:
   7 v3 with delivery tags, the cat v2; old takes kept in `Unity/Assets/Audio/Voice/_r8_s06/`). The door now opens by 7
   touching the mechanism (X "Touch", no dodecahedron outline): its ring lights, the S-seam then the engravings light, the
   leaves part. Scene 6 cameras: 7 and the cat 0 % occluded / outside the frame.
9. **Checks — done.** Autopilot start → finish (`-chapter1`): **0 soft-locks**, 27/27 scripted traversal steps, 3/3 fall
   resets, all 8 lessons shown once and cleared (Xbox + keys shots each), 0 legend panels, every dialogue camera 0 %,
   83/83 lines with audio (the 4 unplayed are the alternate 6b set), laser/release checks as before. Opening test
   **`-openingtest 25` → **25/25 PASS** (cat inside the cell 0 frames; the opening now waits for X Look or the watchdog)**. New SFX (ledge grab, soft / hard landing, climbing scuff, slab lift; `Docs/SFX-LIST.md`):
   brief unmuted check `-soundcheck sfx` → 5 sounds, **5.0 s of sound**, then muted again (output 0.000000): land_soft 0.39, land_hard 0.39, climb_scuff 0.38, slab_lift 0.38 output peak; ledge_grab measured 0.0009 — its file peaks at −3 dBFS in the first 0.1 s and, as the first sound of the check, that transient was missed by the measurement (re-check with it later in the list next time). Gallery check (X/A sit + every binding): `-gallery` real-input test **17 passed, 0 failed**.
   **Length:** the autopilot plays the whole chapter in **7.2 min** (Moon level 5.8 min) — it walks perfect lines and
   cuts every voice line after 0.9 s (160.7 s of voice). **Estimate for a first play: ~15–18 min — the 20–25 min target is
   not reached yet**; more meat would need another traversal / puzzle beat or two (e.g. in the hall or the bay).
10. **Release + docs — done.** 0.9.0 (build 900) published (`game-900`, Latest); `TODO.md`, `CLAUDE.md` updated; pushed.

### For Dan to check (Round 9) — `Docs/Previews/r9/chapter1/`
- Teaching prompts (Xbox / keys): `02/03_t_move`, `04/05_t_interact`, `16/17_t_jump`, `20/21_t_crouch`, `23/24_t_ledge`,
  `25/26_t_hang`, `41/42_t_tk`, `91/92_t_sit`; pause menu Controls page `96_s12_pause_controls.png`.
- Traversal: `19_tr01_across_the_gap`, `22_tr02_crouched_under_pipes`, `27_tr03_hanging_bulkhead` (cat waiting on top),
  `28_tr04_shimmy_bulkhead`, `32_tr06_wall_climb`, `33_tr07_hanging_shaft_lip`, `35_tr08_shimmy_over_the_drop`,
  `36_tr09_dropped_into_the_ruins`, `37/38` terraces, `39_tr12_hanging_terrace_c`, `43/44` slab + bright energy point,
  `65–67` mining cut, `74–76` broken ramp / scaffolds / bay. Fall reset: `18_tr_fall_white.png`.
- Vision + door: `53_s06_shape_in_alcove`, `54_s06_shape_touch_flare`, `52_s06_chamber_before`, `55_s06_whitewash_in`,
  `57_s06_vision_video_frame`, `58_s06_whitewash_out`, `61_s06_touching_mechanism`, `62_s06_emblem_and_engravings_lit`,
  `63_s06_door_parting`, `64_s06_passage_opens`.
- Play: the chapter has no legend now — try it with Button hints On, then Off (Settings). Hang on the bulkhead and
  shimmy; let go over the shaft on purpose (white fade, back on the shelf).

## Quick fix QF4 — cat/opening, dialogue cameras, the real Scree — done (2026-11-17), exe-verified off-screen; release 0.8.2
Plan text moved word-for-word to `TODO-ARCHIVE.md` → "Quick fix QF4". Previews + logs: `Docs/Previews/qf4/`. Every run
muted (output peak 0.000000 / listener 0 in every log), minimized-not-activated, Below Normal, one heavy job at a time;
monochrome max |R−G|,|G−B| = 0 over all 92 final-build shots. No audio changed, so no sound check.
0. **Safety checkpoint** — committed + pushed first (`ecf9ae8`, Scree GLB + references in LFS).
1. **Cat never in the cell, opening always starts — done.** Cause: the cat started in Follow mode and moved one step per
   frame; a long frame (load hitch, up to 333 ms) let one sprint step jump the 0.7 m wall between the corridor and the
   cell zones, and once inside he could never reach his mark, so scene 1 (which waited for "cat arrived") never began.
   Fixes (`ChapterCat.cs`, `Chapter1Director.cs`): his moves are split into ≤ 6 cm sub-steps; a **no-cat box** over the
   cell (`ChapterCat.NoCat`) rejects/pushes out every step, hop end, placement and catch-up; he sits by his opening at
   checkpoint 0 until sent on his fixed route. Scene 1 starts when 7 reaches the door, or by **watchdog 20 s after the
   cat arrives** (45 s after he is sent at most) — 7 is then walked to the door; if he is not on his mark 4 s later he
   is placed there. **Every wait on the cat** (door mark, alcove, crate up/down, back from the alcove) is a watchdog
   (`UntilOr`, in play too) — no story step can hang on the cat's position. **Check:** `-openingtest 25` →
   **25/25 PASS**, cat inside the cell 0 frames in every run (5 variants × 5: idle → watchdog, door at once, wanders,
   waits for the cat, runs along the beams; random start spots; 150–450 ms hitches injected in 12 runs) —
   `Docs/Previews/qf4/opening/capture_log.txt`.
2. **Dialogue cameras keep a clear view — done.** Scene 5's camera sat at x 17.6, outside the passage wall (the passage
   is x 15.6–17.4). Now they step into the hall first; `ChapterCamera` gets the speakers for every conversation
   (`Talk()`), checks line of sight + frame every 0.2 s while lines play and moves to the nearest clear framing (eased
   when the way is open, a cut when a wall is in between — never through geometry); every conversation waits for the
   cat to reach his spot and the camera to settle; scene 7 is now over her shoulder with the cat beside her; "Come on."
   (5-4) is a short two-shot. **Check** (full chapter, 0.1 s samples during every line): 7 and the cat — and the guard
   in 5-1…3 — **occluded 0 %, outside the frame 0 %** in scenes 1, 2, 3, 4, 5, 6, 7, 8 and 6b (`speaker view` lines in
   `Docs/Previews/qf4/chapter1/capture_log.txt`). Not counted (story): the cat while he walks off to the hidden release
   (2-2/2-3) and while hidden in the vision; 6b counts occlusion only (its lines play over the city shots on purpose).
3. **The real Scree — done, tail tucked (Dan's change).** `Tools/Blender/cleanup_scree.py` (~4 min): squares the figure
   (turned 17.7°), crown 1.90 m (antenna tip 2.00), origin at the feet; abdomen + wing covers swung 37.9° to straight
   behind, **then tucked 42° below horizontal** about the waist (the covers move with it); head straightened 12.4°;
   decimated to 45k tris with hose, antenna and claws protected; grayscale 2048 texture. Rig with Unity Humanoid names
   (explicit bone map in `FightDemoBuilderScree.cs`) + extras: lower arm pair, 4-bone abdomen, 2-bone antenna; the upper
   arms straightened into a T-pose rest. 7's `idle` / `walk` / `look_around` retargeted; keyed `work` (2.4 s loop) and
   `alert` (1.4 s); `ScreeRig.cs` moves the lower arms (follow the upper pair, brace forward while working), abdomen and
   antenna (sway; twitch on alert). Eyes = small white glints, tank lights = small white glow (emissive). In Chapter 1:
   the hall guard (alert reaction when the stone lands, then walks over and looks around; still nonlethal), the two gate
   guards, 4 workers in the bay and 2 on the terrace seen in the city reveal (work loop). Placeholder kept as the
   fallback (used automatically if the export is missing). **Checks:** feet — lowest foot/toe bone 0.03–0.08 m above
   the root, root 0.000 m above the ground (idle, alert, walk, work); tucked abdomen vs legs (centre line, needs
   > 0.20 m): **idle 0.315, walk 0.278, work 0.304 m → clears**; no limb tearing seen in the shots.
4. **Rebuild + release — done.** Build everything (exit 0; phantom check city triangles 0); **0.8.2 (build 802)**
   published: `game-802` is **Latest** on `danemoll-jpg/seven-releases`.

**Scree design approved 2026-11-17 — tail tucked down** (Dan).

### For Dan to check (QF4)
- Scree previews (Blender, rest pose): `Docs/Previews/qf4/scree/scree_sheet_rest.png` (front / side / back / ¾ / top /
  other side) — tail tucked, covers along it; side `scree_rest_side.png`, face `scree_rest_face.png`.
- Scree clips: `Docs/Previews/qf4/scree/scree_sheet_clips.png` (work loop frames; alert start / mid / end).
- In game (`Docs/Previews/qf4/chapter1/`): `15_s05_checkonly_scree_guard_idle.png`, `17_s05_checkonly_scree_guard_alert.png`,
  `18_s05_checkonly_scree_guard_walking.png`, `33_s07_checkonly_scree_worker_bay.png`, `34_s07_checkonly_scree_worker_terrace.png`.
- Conversation framing: scene 5 `14_s05_scree_guard.png` (7, the cat and the guard beyond); scene 7
  `32_c06_saucer_spotted.png`; scene 8 `36_s08_at_the_edge_of_the_light.png`.
- Opening runs: `Docs/Previews/qf4/opening/01_op01_scene1_0.png … 05_op05_scene1_4.png` + the 25-line PASS log.
- Play: stand idle in the cell — scene 1 should start by itself ~20 s after the cat sits down outside; the cat never
  comes in. The launcher should offer the 0.8.2 update.

## Next Steps (Do Not Start Yet)
- **Scree model** — done in QF4; Scree design approved 2026-11-17 — tail tucked down.
- **First vision contents** — DECIDED (Dan, 2026-11-17): the video `Art/Video/vision_01_door.mp4` (in since Round 9).
- **Fight reset on Backspace / d-pad down** — DECIDED: OK (Dan, 2026-11-17).
- **Hair ties:** DECIDED (Dan, 2026-11-17): **leave the metal cuffs as they are.** (Glowing clips from Kyle's art not planned.)
- **Forehead dot = 7's "energy point"** (Kyle, 2026-11-17). Keep the plain glowing dot. Gameplay: **Kyle's proposal
  (2026-11-17): its brightness shows how much energy she has left for the shield and telekinesis** — bright when
  full, dimming as she uses powers (no separate on-screen bar). Not yet scheduled; don't build it until a round asks.
- **"7" emblem on her back** (Kyle's art) — DECIDED for now (Dan, 2026-11-17): **no emblem.** May revisit.
- **Bubble helmet, hover bike, saucer, cat companion models** — deferred; need concept images and prompts first.
- **Big-head proportion variant** — DECIDED: not pursued for now (Dan prefers normal proportions).

## Technical Notes / Blockers
- `7_normal_v2.glb` is ~59 MB; it must go through Git LFS.
- Mixamo strips materials; reassign materials in Unity (Stage B item 5).
- FBX does not carry PBR metallic/smoothness reliably — set the cuff material up in Unity (Stage B item 5).
