# Active Development Plan — 7: The Observer

## Standing rules
- 7 never kills · strictly monochrome · non-sexual presentation · 7 is clearly an adult (details in `CLAUDE.md`).
- Per-item status at the end of every round: **done / not done / deferred (why)**. No silent partial completion.
- Short results: ~20 lines, one line per item + its one check, plus a "For Dan to check" list.

## Completed Tasks
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

## Current Objective — Round 8 (started 2026-11-17, right after Round 7)
Dan's decisions 2026-11-17: schedule all of these now.

1. **Saucer look pass** (Code's own list from Round 3): darker city near the ship and a dark overhead hull edge for
   Kyle's silhouette contrast; stronger in-fog glows; less boxy towers (fewer, larger, softer forms — same rule as
   the city reveal: fog does the work); better seated camera (not blocked by the chair back); the cat's on-lap pose
   clear of her forearms; **a better cat rig** (properly fitted bones / cleaner weights, or a Tripo/AccuRig quadruped
   rig) — he is a main character. Cat roaming size stays ~2× (Dan: OK).
2. **Gallery extras (Dan: OK):** 7 sits on the TV sofa while browsing (Sitting Idle; camera over her shoulder to the
   screen); a d-pad left/right glyph in the legend instead of LB/RB; preload the gallery images so entering the
   saucer has no hitch.
3. **Ratwing: smooth the flip from hanging to standing (Dan: the only thing wrong with the scene).** The half-roll
   upright reads as a snap. Re-key it as a readable, weighty motion: feet release → he drops head-first for a beat →
   wings snap half-open to brake → his body swings round from the hips (legs arcing down under him) over ~0.6–0.9 s
   → legs reach for the floor → crouch landing. Ease in/out, no frame where the pose jumps; keep feet planted at the
   end. Compare before/after in a Blender contact sheet and in-game shots.
4. **Fighting must feel fluid (Dan: the demo felt very clunky).** Make the fight demo responsive and flowing:
   input buffering (~0.2 s) so presses during a move queue the next one; **combo chains** (jab → cross → hook; kicks
   can follow punches) with cancel windows after each hit lands; shorter, cleaner blends between moves (crossfades
   ~0.08–0.15 s) and idle ↔ move ↔ attack; small forward steps on attacks so she closes distance; smooth turning
   toward the target instead of snapping; dodge and block can interrupt recovery; a brief hit-pause (~40–60 ms) and
   camera nudge on connect; the bot's reactions start on the exact hit frame. Nonlethal rules unchanged. Report
   before/after timings (input → move start, move → next move) and capture a short combo sequence.
5. **Auto-update launcher — build it now (Dan: yes), publish a release every round (Dan: yes).** Copy the standard
   launcher from `C:\Users\danmo\chastain_rush` (read-only — copy, never edit) per the collaboration notes:
   `Launcher/` (C++), `LauncherBridge.cs`, the `Tools/` release scripts, starter README rewritten for 7. Rename to
   `SevenLauncher.exe`; make a new signing key once with `python Tools/New-LauncherKey.py` (kept in
   `Builds/PublisherPrivate/`, git-ignored — never delete, print or replace it); releases repo
   `danemoll-jpg/seven-releases` (create it if missing). Windows + Steam Deck (Proton). From now on every round ends
   with a published release. Record the commands in `CLAUDE.md`.
6. Checks as usual (muted except brief sound checks, off-screen, never steal focus), update `TODO.md` / `CLAUDE.md`,
   publish the first release, commit and push, report in ~20 lines.

## Next Steps (Do Not Start Yet)
- **First vision contents** — still undecided (Dan, 2026-11-17). Keep it generic until he decides.
- **Fight reset on Backspace / d-pad down** — DECIDED: OK (Dan, 2026-11-17).
- **Hair ties:** DECIDED (Dan, 2026-11-17): **leave the metal cuffs as they are.** (Glowing clips from Kyle's art not planned.)
- **Forehead dot = 7's "energy point"** (Kyle, 2026-11-17). Keep the plain glowing dot. Gameplay: **Kyle's proposal
  (2026-11-17): its brightness shows how much energy she has left for the shield and telekinesis** — bright when
  full, dimming as she uses powers (no separate on-screen bar). Not yet scheduled; don't build it until a round asks.
- **"7" emblem on her back** (Kyle's art) — DECIDED for now (Dan, 2026-11-17): **no emblem.** May revisit.
- **Saucer look pass** — moved to Round 8 (scheduled).
- **Launcher / auto-update** — moved to Round 8 (Dan: build now; publish every round).
- **Bubble helmet, hover bike, saucer, cat companion models** — deferred; need concept images and prompts first.
- **Big-head proportion variant** — DECIDED: not pursued for now (Dan prefers normal proportions).

## Technical Notes / Blockers
- `7_normal_v2.glb` is ~59 MB; it must go through Git LFS.
- Mixamo strips materials; reassign materials in Unity (Stage B item 5).
- FBX does not carry PBR metallic/smoothness reliably — set the cuff material up in Unity (Stage B item 5).
