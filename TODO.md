# Active Development Plan — 7: The Observer

## Standing rules
- 7 never kills · strictly monochrome · non-sexual presentation · 7 is clearly an adult (details in `CLAUDE.md`).
- Per-item status at the end of every round: **done / not done / deferred (why)**. No silent partial completion.
- Short results: ~20 lines, one line per item + its one check, plus a "For Dan to check" list.

## Completed Tasks
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

## Round 7 — Chapter 1 fixes + sound effects + music — done (2026-11-17), exe-verified off-screen; Dan to play
Plan text moved word-for-word to `TODO-ARCHIVE.md` → "Round 7". Checks: muted autopilot start to finish (45 shots,
0 soft-locks, output peak 0.000000 every run), monochrome max |R−G|,|G−B| = 0 over all chapter shots, all demos load.
1. **Laser door — done.** 9 white beams + soft halos flush with the cell's inner face (z 1.315); the walkable cell ends
   there. Check: 5 runs into the beams (straight, both diagonals, running) → furthest root z 1.050 = she stays inside
   (fixed a one-frame overshoot: 7 is now clamped after moving). Hum (3D) at the beams; they switch off top to bottom.
2. **Hidden release + energy point — done.** Release panel inside a new guard alcove west of the cell; the cat walks
   out of sight, paws it; 7 hears a click + power-down. Check: 1,452 rays from a grid inside the cell → 0 reach the
   panel; panel never in frame/visible in any in-cell shot. Forehead dot dark in the cell, relights (fade + shimmer
   sound) as she steps past the dead beams (z 2.11). Close-ups `08/09_s02_energy_point_*`.
3. **Tractor beam — done.** Ramp removed. Clamps release → saucer lifts 2.2 m and hovers; 8-blade iris opens; a soft
   cone + floor glow; scene 8 at the edge of the light; 7 floats up (upright, arms drifting out — procedural), then the
   cat (held mid-jump frame, legs hanging); iris shuts, light fades, liftoff. **Not done: hair drifting** (the pigtails
   have no bones). Shots `30–37`.
3b. **City reveal (6b) — done, first pass; judge it against Kyle's art.** New `FightDemoBuilderCity.cs`: the prison
   bolted into the base of a colossal stepped host structure (pipes, fence, lamps, freight lifts), four colossal
   siblings, a hollow-Moon shell (R 3 km) with terraces down to a basin, 5 pyramids around a dark depression, aligned
   rings, ~150 stepped buildings / towers / bridges climbing the curve overhead, 4 conveyors with moving lights, 3
   excavators, the "quiet district" that starts up, the terrace edge that breaks and falls with dust. 25.8k tris.
   Fog blends to a city preset as she walks out (bright haze in every direction, overhead key light). Timing: ~6 s of
   only sound + main theme, then the voiced 6b lines, scene 7 from the terrace. `cityRevealDialogue` / `-cityreveal
   primary|alternate|off` (alternate set voiced; the check run played primary). Shots `24–29`. Known: the wide shot is
   dominated by the host wall and 7 is too small to read; the far city is simple boxes (fog hides it).
3c. **Dan's clips — done:** `7_sneak_walk` (sneaking), `7_touch_wall` (older wall + vision; held at frame ~45/98),
   `7_look_around` (city-reveal turn + idle variety after 11 s), `ybot_float` (bot held by telekinesis). TK hold: slow
   breathing, hip sway, faint arm/hand tremble, forearms follow the bot (procedural layer, a few degrees).
3d. **No "To be continued" — done.** After scene 11 free exploration (chair + reveal, gallery TV, sofa; cat roams /
   naps / lap), "Explore the ship" once. Pause menu (Start / Esc): Resume · Settings (music / SFX / voice) · Main menu ·
   Quit, confirms on both. Main menu: Quit (confirm), **Continue** when the save exists (saved at liftoff). Check:
   `-continue` → explore, sit, cat on lap; pause/settings/quit-confirm shots `43–45`. Check runs use a test save key
   and delete it.
4. **SFX — done.** 33 ElevenLabs sound effects (`Tools/Audio/generate_sfx.py`, `Docs/SFX-LIST.md`,
   `Unity/Assets/Audio/SFX/*.wav`, loops cross-faded at the seam). `laser_hum` / `step_metal_3` came back near-silent
   and were re-prompted once.
4b. **7 re-voiced — done.** Eleven v3 (`eleven_v3`, stability 0.5 "natural", similarity 0.8) with audio tags from a
   delivery note per line (`Tools/Voice/seven_delivery.json`, shown in `Docs/VOICE-LINES.md`); 44 lines (39 old + 5
   new 6b). Old takes kept in `Unity/Assets/Audio/Voice/_v1/`. The cat untouched (+4 new 6b lines). Not listened to by
   Code — Dan to judge whether she is still flat.
5. **Mix — done.** Buses: voice 1.0 > SFX 0.55 > music 0.42 (ducked to 35 % under dialogue) > ambience 0.28; 3D hum at
   the beams, footsteps at the feet (metal/rock by zone), cat paws, Scree steps, TK, stones, doors, clamps, saucer hum,
   beam, iris, liftoff, fridge, menu. `AudioKit.cs`.
6. **Music — done.** `MusicSystem.cs`: menu = main_theme; cell → ruins moon_prison; vision = silence; reveal swell
   main_theme; bay moon_prison; beam + liftoff main_theme; aboard aboard_saucer; fight / Ratwing none. 2.5 s cross-fades,
   loops, ducking; missing file = silence. Volume sliders in the pause menu (saved in PlayerPrefs).
7. **Checks — done.** Sound check (the only unmuted step): 5 SFX (laser_power_down, release_click, energy_point,
   tractor_beam, menu_confirm), each ≤ 1.2 s, **5.35 s of sound**, peaks 0.009 / 0.39 / 0.39 / 0.27 / 0.39, then 0.000000.
   Demos: fight 19 shots, saucer 21, Ratwing 16, gallery 21 (17/17 input tests), menu — all output 0.
8. **Docs + commit — done** (this entry, CLAUDE.md).

### For Dan to check (Round 7)
- Walk into the laser door; the cat leaving and the click you only hear; the forehead dot relighting as you step out.
- The city reveal: does it feel like Kyle's paintings? (wide shot, up the curve, pyramids, the falling stone).
- The tractor beam (7 and the cat floating up) — and whether the missing hair drift matters.
- 7's new voice takes vs. `_v1/` (four lines flagged short/long: s02_02, s06_07, s11_01, s06b_a03).
- Music levels / ducking, the SFX (several came back at 0 dBFS raw — listen for clipping), pause-menu sliders.
- After the chapter: explore the saucer; quit the game from the menu; start again and use Continue.

## Round 8 — saucer look, gallery extras, Ratwing flip, fluid fighting, launcher + first release — done (2026-11-17)
Plan text moved word-for-word to `TODO-ARCHIVE.md` → "Round 8". Previews: `Docs/Previews/round8/`. All runs muted
(output 0.000000), off-screen, Below Normal, one heavy job at a time.
1. **Saucer look pass — done, cat rig improved (not perfect).** Near city cluster near-black, fewer/larger towers
   (77 vs 178), few glowing lips, stronger in-fog glows (0.32 → 0.5); a dark overhead hull-edge ring frames the revealed
   view; seated camera moved to the side (yaw 74°, clear of the chair shell); cat lap lift 0.125 → 0.165 m (check: gap
   +0.044 m above her thighs, 0 cat vertices inside her). Cat rig (`cleanup_cat.py`): joints fitted to the mesh, bone-heat
   weights + smoothing, ≤ 4 influences, same 24 bones / clips; worst stretch roughly halved (sit 5.97× → 2.51×, sleep
   9.45× → 3.26×) — `cat_rig_before_after.png`, `cat_rig_notes.txt`. Still rough: deep sit/sleep folds (needs extra
   bones or corrective shapes). Not a third-party (AccuRig/Tripo) rig.
2. **Gallery extras — done.** 7 walks to the sofa and sits (stand_to_sit → sitting_idle) while browsing, camera over her
   shoulder; stands up when leaving. Legend: d-pad left/right glyph. Images decoded once while the main menu is idle
   (check: "24 of 24 from the preload cache … 0 s here"); 17/17 gallery input tests pass.
3. **Ratwing flip — done.** Re-keyed in `cleanup_ratwing.py`: release → head-first beat → wings snap half-open to brake →
   swing upright around the hips over 0.85 s (eased both ends) while the legs come down and reach → crouch landing at
   7.9 s (was 7.55); feet 0 mm off ceiling/floor. Contact sheet `ratwing_flip_before_after.png`; in game `05_mid_drop`.
4. **Fluid fighting — done.** 0.2 s input buffer; combo cancels (a different move may cut the recovery 0.07 s after a
   blow lands); forward step-in (≤ 0.45 m); smooth turn to the target (no snap); dodge / block interrupt recovery;
   40–80 ms hit-pause + camera nudge; bot reaction fade 0.06 → 0.03 s; blends 0.08–0.15 s. Check (`-fightcombo`):
   input → move 0.000–0.015 s; move → next move jab→cross 0.78 s, →hook 0.79, →mma_kick 0.56, →roundhouse 0.94 vs.
   old rules 1.12 / 1.28 / 0.86 / 1.15 s **and every one of those early presses was dropped before** (needed a 2nd
   press); combo of 5 in 2.8 s with 6 connects; dodge out of a recovery starts in 0.018 s. Not exercised: a press during
   the late dodge (test still reports it not starting — check by hand).
5. **Launcher — done; first release published (see CLAUDE.md "Auto-update launcher").** Copied from chastain_rush
   (untouched): `Launcher/` (C++), `LauncherBridge.cs`, `Tools/*Launcher*.py`, `Docs/LAUNCHER.md` (Windows + Steam Deck),
   monochrome icon/cover art. `SevenLauncher.exe`, new signing key (`Builds/PublisherPrivate/`, git-ignored), releases repo
   `danemoll-jpg/seven-releases` (created, public). Release 0.8.0 (build 800) + starter zip — published 2026-11-17: `game-800` (Latest, 88 MB game.zip, signed catalog) and `starter-800`
   (`Seven-Starter-0.8.0.zip`, launcher status check PASS) at https://github.com/danemoll-jpg/seven-releases/releases.
   Not tested: the windowed launcher starting the game and the ready handshake on Dan's PC / the Deck (Dan to try).
6. **Checks — done.** Chapter 1 start to finish: 45 shots, 0 soft-locks, laser door holds (z 1.050), line of sight 0/1,452, monochrome max 0. Fight / saucer / Ratwing / gallery / menu / Continue all
   load and pass. Sound check (only unmuted step): 5 SFX, 5.34 s of sound, then 0.000000. Fixed during the checks: 7 and
   the cat could be snapped across a wall into another walkable zone on a frame hitch (movement now sub-stepped / jump
   steps rejected); the city's glowing carts were built at the origin (inside the cell) until placed on their tracks.

### For Dan to check (Round 8)
- Saucer: does the city below read like Kyle (dark near, pale far)? The seated camera; the cat on your lap.
- The cat walking / sitting / washing / sleeping with the new rig (sleep and deep sit still fold a little).
- Gallery: sit on the sofa, browse with the d-pad; entering the saucer should not hitch.
- Ratwing: the drop and swing upright — weighty enough, any pop?
- Fight demo: mash jab → cross → hook → kicks; does it flow now? Dodge / block out of a combo.
- Launcher: download the starter zip from the seven-releases page, run SevenLauncher.exe, install, play; on the Steam
  Deck add it as a non-Steam game with Proton (Docs/LAUNCHER.md).

## Current Objective — Quick fix QF3: Dan's playthrough of Rounds 7 + 8 (2026-11-17)
Commit the repo as it is first (safety checkpoint). Screenshots from Dan: `Docs/Previews/dan/qf3_cat_lap.png`,
`Docs/Previews/dan/qf3_phantom_wall.png`.

1. **Phantom walls in the prison (regression — not in the Round 6 build).** Throughout the prison there are now big
   flat walls/blocks that you can walk straight through but that block the view (Dan's shot: a huge dark slab over the
   catwalk rails). Most likely the city-reveal / "host building" geometry from `FightDemoBuilderCity.cs` (or atmosphere
   cards) overlapping the prison spaces — renderers with no colliders. Find every one and remove or move it so nothing
   from the city intrudes into the prison, corridors, catwalks, ruins or impound. Keep the city reveal view itself
   intact. **Check:** walk the whole chapter path; list every renderer whose bounds intersect a walkable area and has no
   collider (should be 0 apart from intentional props like fog/particles); compare each scene shot against the Round 6
   shots (`Docs/Previews/round6/`) — no new slabs.
2. **Subtitles: drop "(thought)".** 7 and the cat both talk in thoughts (telepathy), so the label adds nothing. Show just
   the speaker name for both (`ChapterUI.cs`).
3. **Cat on her lap — fix properly (Dan: worse than the first version).** In Dan's shot he floats stretched out across
   her stomach/forearm, legs dangling. He should **lie curled on her thighs** like a real cat: body resting on top of
   both thighs, paws tucked, tail wrapped around, head resting on a paw or up, settled into the lap (no floating gap,
   not sinking in, clear of her forearms and hands). If the rig can't make a good curled pose, use a dedicated static
   curled-cat pose/mesh for the lap (sculpted or posed once in Blender) with only a gentle breathing motion. Her hands:
   one resting near/on him is fine. **Check:** close-ups from the front, both sides and over her shoulder (seated
   camera); measure gap to her thighs (~0–1 cm) and penetration (0); compare with Round 3's `08_lap_closeup.png` — it
   must look clearly better.
4. **Saucer reveal: a black band stays when the ship fades.** Something isn't dissolving — most likely the Round 8 dark
   overhead hull-edge ring. Everything of the ship must fade (or the ring must fade with it) so only 7, the chair and
   the cat remain. **Check:** revealed shots from the seated camera and 3 other angles — no leftover ship geometry.
5. **City reveal: remove the "wrecker" (excavator).** It reads as poorly drawn. Remove the excavators from the view;
   keep conveyors, lifts and the falling terrace stone (that shows the mining). If the view needs more mining activity,
   use a few tiny moving lights or freight lifts instead.
6. **Hide the mouse cursor** during gameplay (it shows in Dan's shot); show it only in menus when the mouse is used.
7. Checks as usual (muted except a brief reported sound check if any audio changes; off-screen; never steal focus),
   rebuild, **publish a release** through the launcher pipeline, update `TODO.md` / `CLAUDE.md`, commit and push,
   report in a few lines per item.

## Next Steps (Do Not Start Yet)
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
