# Active Development Plan — 7: The Observer

## Standing rules
- 7 never kills · strictly monochrome · non-sexual presentation · 7 is clearly an adult (details in `CLAUDE.md`).
- Per-item status at the end of every round: **done / not done / deferred (why)**. No silent partial completion.
- Short results: ~20 lines, one line per item + its one check, plus a "For Dan to check" list.

## Completed Tasks
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

## Current Objective — Round 9: Chapter 1 as the tutorial — traversal + controls taught as you need them (Dan, 2026-11-17)
Commit the repo as it is first (safety checkpoint). Goal: give the beginning more meat. The design calls for ~30%
Tomb-Raider-style traversal and Chapter 1 has none yet; Chapter 1 is also partly the tutorial, so **no permanent button
legend** — each control is introduced the first time the player needs it. Target length after this: ~20–25 min.
All standing rules apply (monochrome, Kyle's fog/glow, non-sexual presentation, **7 never kills**).

### Controls (one scheme for the chapter; document it in `CLAUDE.md`)
1. **Chapter control scheme** (Xbox / keyboard), proposed — adjust if something clashes, and say what you changed:
   left stick / WASD move · right stick / mouse camera · **A / Space jump** (also grabs ledges when jumping at them) ·
   **X / E interact** (doors, release, touch the wall, sit) · **B / C crouch-sneak** (toggle) · **B / Space while hanging
   = let go** (drop) · **RT / right mouse telekinesis** on a highlighted object · R3 camera re-center · Start / Esc pause.
   Move today's "A / E interact" prompts to X / E. The fight demo keeps its own controls and legend.
2. **No permanent legend in Chapter 1.** Remove the bottom-left legend in the chapter. Instead:
   - **Teaching prompts:** the first time a control is needed, a small, clean prompt appears (glyph + one or two words,
     monochrome, lower middle), stays until the player does it (or ~6 s after they clearly have), then fades. Order:
     move + camera (start of the cell, after a short beat) → interact (cell door / scene 1) → crouch-sneak (first
     time it helps — before the Scree, or a low pipe earlier) → jump (first gap) → grab ledge / climb / shimmy / let go
     (first ledge) → telekinesis (first heavy stone, see 5) → sit (saucer chair).
   - **Context prompts** after that: only when standing at something usable (e.g. "X Touch", "X Sit"), small and quiet.
   - Pause menu gets a **Controls** page with the full map (Xbox / keyboard), and Settings gets **Button hints: On / Off**.
   - Xbox glyphs by default, keys after keyboard use (existing system). No colored glyphs.

### Traversal (Tomb Raider style, short and readable)
3. **Moves for 7:** jump (standing + running), **ledge grab** when jumping at a ledge, **hang**, **shimmy** left / right,
   **climb up** (pull up / mantle), **let go** to drop, **drop down** from a ledge edge onto a ledge below, low
   **mantle** over waist-high obstacles, **crouch-sneak** (Dan's `7_sneak_walk`). Climbable edges read clearly but stay
   in style — pale worn stone lips / scuffed metal edges catching the light, never painted markers or colors.
   Assisted, forgiving controls (snap to ledges within a small radius, no pixel-perfect jumps).
4. **Falls never kill.** A fall into a drop or chasm fades to white and returns 7 to the last safe ledge (no death, no
   injury animation) — in keeping with the game's tone.
5. **Telekinesis taught before the Scree:** in the ruins, a heavy stone block or fallen slab blocks the way or is
   needed as a step — **RT** moves it into place. This teaches the power that she then uses for the Scree distraction.
   Her energy point brightens while she uses it (visual only for now; energy limits come later).
6. **Where the traversal goes** (keep every story beat and its order from `Docs/DIALOGUE.md` exactly as is):
   - **Prison, after scene 3:** a broken catwalk section → the first **jump** across a gap; a low pipe run → **crouch**
     under; a short wall with a ledge → first **ledge grab + climb up**.
   - **Scene 4 → the older passage:** the gap into the ancient structures is up a cracked wall → **climb**, then a
     **shimmy** along a ledge over a dark drop, then **drop down** into the ruins.
   - **Ruins before the vision:** a short multi-step route — jumps between broken terraces, a hang-and-shimmy, the
     **telekinesis** stone (item 5) — then the Scree encounter as now.
   - **After the vision, before the city reveal:** a climb up out of the ruins to the mining passage that leads onto
     the terrace — so the reveal arrives after an effortful climb.
   - **Down to the impound** after the reveal: a couple of drops and ledges to reach the bay.
7. **The cat leads the routes.** He hops ahead to the next ledge or gap and waits there (cats jump well — he shows the
   way without UI), then rejoins her. He never blocks a jump or a ledge. Keep the 8 m catch-up rule (out of sight).
8. **Mixamo clips** (Dan downloads — see below; use them if present, keyframe or pose stand-ins if not, and report
   which). Retarget to 7 as Humanoid; strip root motion where the controller drives movement.
   **Status 2026-11-17: Dan's clips are in** `Art/Mixamo/7/` — `7_hang_idle`, `7_shimmy` (**one direction only — mirror it
   for the other side**), `7_climb_up` (Braced Hang To Crouch), `7_jump_to_hang`, `7_hang_drop` (Freehang Drop),
   `7_running_jump`, `7_fall_idle`, `7_landing` (Falling To Landing), `7_landing_hard` (Hard Landing — for big drops),
   `7_wall_climb` (Climbing Up Wall — use for the cracked-wall climbs), `7_crouch_idle`. Alternates in
   `Art/Mixamo/7/alt/` (`7_climbing_a` / `_b` = "Climbing" variants, `7_hang_idle_b`, `7_jumping_up`, `7_running_jump_b`):
   look at them and pick the best one for each role — **use one of the "Climbing" alternates for the waist-high mantle**
   if it fits, otherwise a trimmed `7_climb_up`. Report which file you used for each move.
8b. **The vision object — one of Kyle's floating shapes (Dan, 2026-11-17).** Replace the glowing wall inscription she
   touches for the first vision (scene 6) with a shape from Kyle's telekinesis painting (`Art/Reference/kyle_7_telekinesis.jpg`):
   a **glowing white wireframe dodecahedron** (12 pentagon faces — the shape ancient astronomers tied to the heavens,
   fitting the Moon's Astronomical Age), about 0.6 m across, hovering above a worn pale-stone pedestal in an alcove,
   rotating very slowly with a faint pulse, lighting the stone around it — styled like Kyle's: thin bright edges with a
   soft glow, monochrome. Prompt: X "Touch". When she reaches out (`7_touch_wall`), the edges flare, the light swells to
   white → the vision as now (contents still generic). Afterwards the shape dims to a faint glow. The "symbol she saw in
   the vision" that opens the passage becomes the same dodecahedron outline glowing on the sealed door. One shape
   constant so Dan can swap to another of Kyle's shapes (tetrahedron, cube, octahedron) without other changes.
   Capture: the shape in its alcove, the touch flare, the outline on the door.

9. **Checks** (muted, off-screen, never steal focus): rebuild; automated playthrough start to finish with traversal
   (scripted inputs) — no soft-locks, every jump / ledge / shimmy completes; deliberate falls reset to the last safe ledge;
   each teaching prompt appears once at the right moment and clears; no legend visible in the chapter; captures of every
   traversal spot + every teaching prompt (Xbox + keys); cat waiting at each next ledge; 7's hands on the ledge while
   hanging (measure the gap); monochrome 0; audio 0 except a brief reported sound check if new SFX (footstep on ledge,
   grab, land). Measure the chapter's new length.
10. Publish a release, update `TODO.md` / `CLAUDE.md`, commit and push, report in ~20 lines.

### For Dan (before Code starts, optional but better): Mixamo clips for 7
FBX for Unity, 30 fps, keyframe reduction none, **without skin**, character = 7 → `Art/Mixamo/7/`. Closest match is fine.
`7_hang_idle.fbx` (Hanging Idle / Braced Hang) · `7_shimmy_left.fbx`, `7_shimmy_right.fbx` (Braced Hang Shimmy, In Place
off) · `7_climb_up.fbx` (Braced Hang To Crouch / Climbing) · `7_jump_to_hang.fbx` (Jumping Up / Jump To Hang) ·
`7_hang_drop.fbx` (Hang Drop / Drop Down) · `7_running_jump.fbx` (Running Jump) · `7_fall_idle.fbx` (Falling Idle) ·
`7_landing.fbx` (Falling To Landing / Hard Landing) · `7_mantle.fbx` (Climbing / Vault, waist-high) ·
`7_crouch_idle.fbx` (Crouching Idle).


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
- **First vision contents** — still undecided (Dan, 2026-11-17). Keep it generic until he decides.
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
