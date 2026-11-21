# 7: The Observer — Project Context & Rules

Read this at the start of every session, then read `TODO.md` for the current round.

## What this is
A monochrome 3D adventure game. Heroine: **7**, a full-grown adult (many years old) who travels seven worlds/ages.
The design record lives in Dan's claude.ai Project "7 - The Game" (not in this repo). Standing creative rules that
apply to every asset and scene:
- **7 never kills.** No death animations, no lethal outcomes. Opponents are knocked down, stunned or captured and recover.
- **Strictly monochrome.** Black, white and grays only. No colored glows, lights, UI or effects. Glow = white emission.
- **Non-sexual presentation.** Full-coverage outfits, neutral poses, no camera framing or art emphasis on her body.
- **7 is clearly an adult** in every model and outfit.
- **World look (Kyle, via Dan, 2026-11-16): every world uses the fog/glow atmosphere** of
  `Art/Reference/kyle_world_mood.jpg` — dark foreground silhouettes against a bright, glowing, misty distance;
  strong ground fog; soft light sources inside the fog; huge structures fading into the haze. Start every scene from
  the reusable `Atmosphere` setup instead of building fog from scratch: `Unity/Assets/Atmosphere/Atmosphere.prefab`
  (component + volume profile + `SevenHeightFog` full-screen pass + lights + `Glow*` sources). Knobs, recipe and
  cost in `Docs/ATMOSPHERE.md`.

## Show, don't caption — STANDING RULES (Dan, QF18, 2026-11-20; every chapter, now and later)
- **On-screen text is only** titles / chapter cards, objectives, control prompts, menus, subtitles of spoken lines and the
  console's pages. **Never** text describing what the player is supposed to be seeing. When a spec describes something to
  show, build it (even simply) — never caption it. Display / window views are built from the real levels (baked at build
  time), so they always match.
- **Objectives come from the story — she can't just know.** Every objective follows from something she was told or saw
  on screen just before (a line, a sight, a display). Never knowledge from nowhere: if the player would ask "how does she
  know that?", add the line / the shot first.
- **No filler tasks.** Every task has (1) a reason the player understands before doing it, (2) a little real play, and
  (3) a visible payoff after it (light running along it, something opening / moving, a person reacting). "Walk up and
  press X at a panel" with nothing before or after is filler — rework it or cut it.
- **Characters shown talking have voiced lines.** No mimed conversations: if a scene shows someone talking, they have
  lines (subtitled + voiced). Proposed lines go in the spec + the line list and get voiced with the existing voices.
- **Every dialogue shot shows the speaker.** Before a line plays, `DialogueFraming` checks (rays from the camera to the
  speaker's head and chest, in frame) that the speaker is visible; if blocked it picks another angle (over the listener's
  shoulder, a side two-shot) or moves the camera — never a shot of a wall. Telepathic Panther lines are exempt.
- **Every interactive objective has a clear done state:** its light changes / dies, it moves or opens, the prompt
  disappears and it can't be used again; counted objectives show a counter ("Pylons 1 / 3").
- **Text over the world is readable:** objectives, prompts, hints, subtitles and marker distances sit on a soft dark
  translucent panel with a text shadow (`ChapterUI` styles) — check captures against the brightest backgrounds.

## Cameras, prompts and the bike — STANDING RULES (Dan, QF21, 2026-11-21; every chapter, now and later)
- **No shot holds on empty space.** A character who walks in a cutscene stays in frame: track / pan with them, cut ahead
  of them, or cut to the next shot when they exit. Scripted: `ChapterCamera.ShotFollow` (the held framing moves with a
  transform; the Asteroid's `TrackWalk`). Automatic in every chapter (`ChapterCameraShots.cs`): a person seen in a held
  shot who walks to its edge with nobody else in frame makes the camera follow them. Every check run logs
  `EMPTY SHOTS (QF21 6)` (must be 0) and `TRACKING (QF21 6)` (out of frame 0 frames).
- **No shot where a big body fills the frame and hides the other one.** `DialogueFraming.Sees` rejects a framing where a
  character over 2.4 m (Machinist, Tunneler, Viper, the keeper) who is not the speaker covers more than half the frame
  (`DialogueFraming.Coverage`). Big characters' conversations: her lines from his side but pulled back and out (her face
  and body clear), his lines from low angles, wide two-shots often (7 small at his feet).
- **Wrong state says so.** An interaction that needs the bike (or needs her on foot) shows what to do when she is in
  the wrong state ("Use the junction from your bike", "Leave the bike first", "Get off - the slab needs telekinesis on
  foot"), the marker points at what she needs (the bike), a companion may say it once - never a silent ignore.
- **Mount before a ride.** Never make her walk back to the bike for no reason: if the next stretch is a ride, she mounts
  first and does the step from the saddle (X / telekinesis from the bike).
- **Talking characters stand still.** A big character in conversation is calm and upright (a held, damped idle - not a
  borrowed gesturing talk clip); at most one restrained gesture on a key line. Restrained characters stay locked.

## Boss fights — STANDING RULE (Dan, QF17, 2026-11-20; every boss, now and later)
Dan: "So far every boss 'fight' is me pushing buttons and the boss just standing there." Never again:
- **The boss moves and actively attacks her** — several distinct, **telegraphed** attacks (a clear wind-up: his pose, a
  sound, a white light, a white marker on the ground where it lands) that she reads and answers with her own moves
  (**dodge, block, jump, telekinesis, her strikes**).
- **She can lose:** hits knock her back and cost her; enough hits = she goes down → a quick retry. **The retry keeps his
  energy and progress (QF20, Dan):** his bar stays exactly where it was, objectives done stay done, the fight goes on in
  his current phase; she gets up with full health and he backs off for a moment. Still nonlethal for everyone (7 never
  kills; the boss is subdued / pinned / contained, alive).
- **2–3 phases that escalate** (faster, a new attack, the arena changes). Objectives (switches, pylons, devices) only
  *while* he is attacking — never the whole fight.
- **He is beaten by her countering him — the loop (QF18, Dan's design):** an attack he wastes (dodged, blocked, jumped)
  leaves him open → **telekinesis only stuns** (a thrown object that hits = he is stunned ~3–4 s: reels, dazed sway, a
  clear visual + sound) → **she closes in and fights hand to hand** (QF20: **X = punches** jab → cross → hook, **Y = kicks**
  MMA kick / roundhouse alternating, mixing them chains one combo; the finisher — her real strikes do the real damage) → he recovers and fights back, she backs off, dodges / blocks and sets
  up the next stun. The last phase ends with the finisher → her takedown (Y / E) pins him. Never by flipping switches
  while he waits. Proof logs damage by strikes vs throws: **melee does most of the damage.**
- **Energy bars:** the boss's bar on screen during the fight (top centre, his name, drains with her hits, a notch /
  shake on each stun, phase marks), and **her own state** (a small bar + her forehead energy point dims with each hit,
  a white flash when hit).
- **Movement is free:** she is never locked facing the boss; lock-on is an optional toggle (R3 / Tab) for strafing in
  close combat, shown on the controls hint.
- **One data table:** `Unity/Assets/Resources/Bosses/boss_tuning.json` (per boss: her hits, open / stagger times, per
  phase: staggers, tempo, gaps, attacks, combos; per attack: telegraph / active / recover / open, damage, knock, reach,
  which answers work). Dan's feedback = a number change there, no code.
- **Proof every time** (a boss is not done without all four):
  (a) a player-style run that plays the fight → `BOSS <id>: fight length …, attacks N (X/min), hits she took, she went
  down N×, dodged / blocked / jumped, throws, strikes, staggers; MELEE (QF20) punches / kicks (/ blades); RETRY (QF20)
  energy before -> after; phases: …` in `capture_log.txt` (`-bossdownonce` = the pilot lets her go down once mid-fight);
  (b) the **idle test** `-bossidle` (nobody presses anything): `BOSS IDLE TEST (<id>): … knocked down after N s` — must be
  within ~60 s;
  (c) a capture sequence of the boss mid-attack (`boss_<id>_<attack>_telegraph / _active (+ _side)`, staggers);
  (d) a first-time-player fight length of about **3–5 minutes**.
- Code: `Runtime/BossFight.cs` (the shared brain: attacks, telegraph markers, hits, open / stagger / phases, retry, idle
  test, the fight camera, captures), `BossPilot.cs` (how a player plays it: check runs + player-style runs),
  `ViperFight.cs`, `WormmasterFight.cs` (per-boss animation, objectives, arena), 7's side in `ChapterSevenFight.cs`
  (`TakeBossHit`, jump `A`, telekinetic throw `RT`, X on an objective, `MaxFightHits`). A new boss = a subclass + a table entry.

## Saucer — fixed design (Dan, 2026-11-19; never let it drift)
- **One saucer, one prefab (QF13):** `Assets/Saucer/Saucer.prefab` — exterior, belly iris + beam, the bike hold + its
  seamless panel — is built ONCE per build (`FightDemoBuilderSaucerExt.SaucerPrefab()`, from `BuildSaucerExterior` +
  `BuildSaucerBeam` at the one hover height `SaucerBeam.ShipBelly` 3.35 m) and every scene places an **unchanged instance**
  (`PlaceSaucer`). Only where it sits may differ (the Moon's clamped ship sits `HoverLift` lower until it lifts off). No
  per-scene building, sizes, hover tweaks or materials. `CheckSaucer` **fails the build** if a scene's saucer differs from
  the prefab (meshes, materials, scales, overrides, beam + hold present) — build log `SAUCER CHECK (QF13 16)`.
- **The bike always uses the bike hold; the beam is only for 7 (and the cat)** — on every world (QF13: Venus B01 / B07,
  Europa arrival / home). The hold opens with `SaucerHold.Open()` (seam lights, panel sinks into the hull and slides
  round inside it, seam dark again). **QF14: behind it is a real modelled room** (`FightDemoBuilderBikeBay.cs`, after
  `Art/Reference/bike_bay_ref.png`: chrome frame on a bulkhead at r 3.45 m, curved steel walls + chrome ribs, three ceiling
  lights, drawers, control panel, track + cradles) with the bike in its cradles; the panel covers only the hull in front of the
  frame (a full-height slot once open). Never go back to a bay made of the hull sector itself (0.26 m thick at the rim: the
  bike went through its ceiling).
- The ~12 m polished silver disc, glowing rim band, low dome, belly glow. No per-world variants.
- **No ramp, ever.** Getting in and out (for 7) is **the tractor beam**: iris hatch in the belly + cone of light
  (`BuildSaucerBeam` + runtime `SaucerBeam.cs`), the same on the Moon, on Venus and any later world. On a world it
  hovers (underside 3.35 m) so 7 can walk under it to the beam.
- **The bike hatch is seamless when closed** (hull panel + hair-thin seam that glows only while it starts to open); the
  bike bay behind it is the chrome room above (QF14), lit only while open.

## Tech Stack
- Unity **6000.6.1f1**, **URP**, **Input System only** (same as Woodstock Rush / Chastain Rush, so systems can be ported).
- Blender for model cleanup, scripted where possible (`Tools/Blender/*.py`). Find Blender on Dan's PC; record the path under Commands.
- Tripo / Meshy for generated models (Meshy key only via the `MESHY_API_KEY` env var — never write it to the repo, logs or chat).
- Mixamo for rigging and animation (Dan does the uploads/downloads; Code lists exact animations).
- Git remote: `https://github.com/danemoll-jpg/SevenVideoGame.git`, branch `main`.
- Large binaries (`*.glb *.fbx *.psd *.png *.jpg *.wav`) go through **Git LFS** (`.gitattributes`).

## Folder layout
- `Art/Source/` — raw generated models (e.g. `7_normal_v2.glb`). Never edit in place; cleanup writes to `Art/Export/`.
- `Art/Reference/` — approved concept/reference images. **`Art/Reference/README.md` indexes every image and what to use it for** — check it before designing any character, effect or world.
- `Art/Export/` — Blender outputs ready for Mixamo/Unity.
- `Art/Mixamo/7/` and `Art/Mixamo/YBot/` — Dan's Mixamo downloads (see TODO for exact names).
- `Tools/Blender/` — Blender scripts. `Tools/Model/` — non-Blender model scripts.
- `Docs/` — `MODELS-NEEDED.md`, previews (`Docs/Previews/`).
- `Unity/` — the Unity project.

## Code Style & Architecture
- Character pipeline (every character/creature): prompts in `Docs/MODELS-NEEDED.md` → Dan gets images → generate model →
  Blender cleanup (scale, origin, facing, decimate, LODs) → previews → **STOP for Dan** → Dan rigs in Mixamo and downloads
  the listed animations → Code imports as **Humanoid**, wires animations, keeps the previous model as a fallback.
- Rounds run in stages with explicit **STOP** points for Dan. Never skip a stop.

## Testing on Dan's PC — STANDING RULES
1. **Always muted**: start every test run with `-mute` (sets `AudioListener.volume = 0` before any sound). Report the
   measured output level; it must be 0. Sound checks unmute only briefly and say so.
2. **Never steal focus**: prefer batch mode / off-screen rendering for screenshots. Otherwise start minimized with
   run-in-background on. If a test truly needs a visible/focused window, **stop and ask Dan first.**
3. One heavy job at a time at Below Normal priority. Clean up temp files at the end of each round.
4. **Testing must never take hours (Dan, QF6).** All automated testing for a round or quick fix stays under **about
   20 minutes of game time in total**.
   - Run at accelerated game speed where possible (`-testspeed 2`, check runs only).
   - At most **one** player-style run through the chapter.
   - Rely on the trigger rules below (fire at or past, watchdogs, no points of no return), not on repeating runs.
   - 25-run tests (playtest, cat-follow, opening) are not part of testing any more. Use them only to investigate a
     specific bug, with Dan's OK.

## Level / story rules (Dan, QF6) — whole game
- **No points of no return.** The player must never be able to pass a point she can't come back from while something
  the story needs is still behind her. Either the way forward stays closed until it's done (the door or path opens only
  after it), or she can walk back. Every one-way drop, ramp, ledge, stair or jump is listed in `Docs/TRIGGER-AUDIT.md`
  with its verdict. Add new ones there.
- **Story triggers fire at or past their place**, never only at one exact moment or spot. Every step the flow waits on
  is declared (`Chapter1Director.Step`) and watched (`StoryWatchdog`).
- **No required pick-up buttons:** story items are taken by walking up to them (the dodecahedron).

## Commands
- Blender 3.6.1: `F:\blender\blender.exe` (not under Program Files, not on PATH).
  Unity 6000.6.1f1: `C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe`.
- Clean up 7 (from repo root):
  `F:\blender\blender.exe --background --factory-startup --python Tools/Blender/cleanup_7.py -- C:/Users/danmo/SevenVideoGame 50000`
  (args: repo root, target tris, weld 1|0 (default 1), optional output FBX path for experiments).
- Cat (Round 3): `F:\blender\blender.exe --background --factory-startup --python Tools/Blender/cleanup_cat.py -- C:/Users/danmo/SevenVideoGame 18000 [preview_dir]`
  → `Art/Export/cat/cat_v1_rigged.fbx` (+ grayscale texture): squares the body, straightens the head, decimates,
  0.25 m shoulder, scripted 24-bone rig + clips idle/walk/sit/wash/jump/sleep. QF3: also `cat_v1_lap.fbx`, the static
  curled lap cat (legs squashed under the belly, tail laid down, body bent `LAP_CURL_DEG` 290 deg into a ring; `LAP_*`
  knobs; `CAT_LAP_OUT` = experiments; previews `cat_lapmesh_*.png`); `CatCompanion` swaps it in on her lap and settles it
  onto her thighs by measuring both meshes. Must run before the Unity build (~4 min).
- Ratwing (Round 4): `F:\blender\blender.exe --background --factory-startup --python Tools/Blender/cleanup_ratwing.py -- C:/Users/danmo/SevenVideoGame 55000 [preview_dir]`
  (~2 min) → `Art/Export/ratwing/ratwing_v1_rigged.fbx` (+ grayscale texture, `ratwing_metrics.txt`): 2.1 m (`HEIGHT`),
  decimate with the membranes/claws protected, scripted 38-bone rig (body + wing root/upper + 3 two-segment finger
  spars per wing, fan weights on the membranes, jaw, eye-glint bone) and the clips `sequence` (10 s) + `idle`.
  Tuning knobs at the top: `HEIGHT`, `CEIL` (ledge height, = `RatwingCeil` in `FightDemoBuilderRatwing.cs`), `LAND_Z`;
  poses `P_SLEEP … P_STANCE` and the timeline in `seq_pose`. `RW_STAGE=fit` / `RW_STAGE=poses` env = debug renders only.
  Must run before the Unity build.
- Scree (QF4): `F:\blender\blender.exe --background --factory-startup --python Tools/Blender/cleanup_scree.py -- C:/Users/danmo/SevenVideoGame 45000 [preview_dir]`
  (~4 min) → `Art/Export/scree/scree_v1_rigged.fbx` (+ grayscale texture, `scree_metrics.txt`): squares the figure, crown
  `HEIGHT` 1.90 m, abdomen + wing covers straight behind and **tucked `ABD_TUCK` 42° down** (design approved 2026-11-17),
  head straightened, 45k tris, Humanoid-named rig (+ `ScreeB_*` lower arms, `ScreeAbd1-4`, `ScreeAnt1-2`), T-pose rest,
  clips `work` + `alert`; `SC_STAGE=fit` = rest previews only. Unity: `FightDemoBuilderScree.cs` (explicit human map,
  `Scree.controller` = 7's idle / walk / look_around + work / alert), `ScreeRig.cs` (procedural extras), `ScreeGuard.cs`
  (alert → walk → look around; `PostState = "work"` for workers). Missing export → the Y Bot placeholder. Must run before
  the Unity build.
- Characters for Mixamo (S1 / S2 / S3): `F:\blender\blender.exe --background --factory-startup --python Tools/Blender/cleanup_mixamo_body.py -- C:/Users/danmo/SevenVideoGame <viper|reptilian|machinist> [tris 36000; machinist 45000] [preview_dir]`
  (~3 min) → `Art/Export/<name>/<name>_for_mixamo.fbx` (welded, viper 2.3 m / reptilian 1.9 m / machinist 4.0 m, texture embedded;
  `TURN_180` if one ever faces away; reptilian also logs `hip_check` = sash tail / satchel vs the thighs; machinist is
  centred on his legs and logs `plate_check` = the front plate free of the thighs, + `_plate_*` close-ups)
  + `Docs/Previews/<name>/<name>_for_mixamo_sheet.png`. Mixamo download goes to `Art/Mixamo/<name>/<name>_body_mixamo.fbx`.
- Previews: `F:\blender\blender.exe --background --factory-startup --python Tools/Blender/preview_7.py -- C:/Users/danmo/SevenVideoGame`
  (args: repo root, optional FBX, optional output dir).
- Launch Blender/Unity jobs with `Start-Process ... -WindowStyle Hidden -PassThru`, then `$p.PriorityClass = 'BelowNormal'`;
  one at a time.
- Unity project was created with: `Unity.exe -batchmode -nographics -quit -createProject C:\Users\danmo\SevenVideoGame\Unity
  -cloneFromTemplate "<Editor>\Data\Resources\PackageManager\ProjectTemplates\com.unity.template.urp-blank-17.2.1.tgz" -logFile <file>`.
- Measure Mixamo clips (frames, limb-extension peaks = hit timings): a throwaway Blender script over `Art/Mixamo/*/*.fbx`
  (bone `mixamorig:*Hand/*Foot` distance from Hips per frame); results recorded in `TODO.md` Stage B item 5.
- **Build everything** (copy Mixamo FBX into Assets, Humanoid import, grayscale texture, materials, animators,
  Atmosphere prefab/profile/fog renderer feature, generated world meshes (`Assets/Art/World/Generated`, git-ignored),
  scenes `Menu` (demo picker, first) + `Chapter1` (Round 6 Moon level, `FightDemoBuilderChapter1.cs`, voice bank from `Unity/Assets/Audio/Voice`) + `FightDemo` (outdoor hill world) + `FightDemo_Room` (Round 1 room) +
  `Saucer` (Round 3; built by `FightDemoBuilderSaucer.cs`, cat from `Art/Export/cat/`, `Atmosphere_Above` prefab) +
  `Ratwing` (Round 4 cave; `FightDemoBuilderRatwing.cs`, model from `Art/Export/ratwing/`, `Atmosphere_Cave` prefab) +
  the art gallery TV on the saucer (Round 5; `FightDemoBuilderGallery.cs` copies `Art/Gallery` to `Assets/StreamingAssets/Gallery`,
  git-ignored), Windows exe at
  `Unity\Builds\Seven\SevenDemo.exe`, product name "7: The Observer — Demo" (QF2; the old `Builds\FightDemo` is obsolete)):
  `Unity.exe -batchmode -nographics -quit -projectPath C:\Users\danmo\SevenVideoGame\Unity -executeMethod Seven.EditorTools.FightDemoBuilder.BuildAllFromCommandLine -logFile <file>`
  (start with `Start-Process -WindowStyle Hidden`, then `.PriorityClass = 'BelowNormal'`; ~5 min; exit code 0 = ok).
  The scenes are generated: change `FightDemoBuilder.cs`, not the scenes by hand. If the exe crashes with
  "level0 is corrupted", check the build log for "script is missing" (every MonoBehaviour must live in a file of the
  same name), then delete `Unity\Builds\Seven` and build again.
- **Check run** (muted, off-screen, never takes focus): start through `Win32_Process.Create` with
  `Win32_ProcessStartup.ShowWindow = 7` (minimized, not activated) and `PriorityClass = 16384` (Below Normal):
  `SevenDemo.exe -mute -capture <dir> -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile <dir>\Player.log`
  → `<dir>\01_wide.png … 17_head_front.png` (orbit angles, legends, punch, telekinesis lift start/held/release/landing,
  knockdown) + `capture_log.txt` (audio level, feet vs. the ground under each foot, camera height, lift height and
  timing), quits by itself (~50 s). Extra switches: `-quick` (look shots only, ~5 s), `-atmofile <json>` (override
  Atmosphere knobs without rebuilding), `-room` (start in the Round 1 room), `-fight` (skip the menu).
  **Saucer check run:** same with `-saucer` → `01_ship_wide.png … 16_after_stand.png` + `capture_log.txt` (audio, feet /
  paws vs. the floor, lap contact = smallest/median gap + clearance to her hands, QF3 reveal audit = ship renderers still
  visible when revealed (must be 0), reveal timings; QF3 shots `08c–08f` lap, `13c–13e` revealed), ~80 s. **Ratwing check run:** same with `-ratwing` →
  `01_cave_wide.png … 11_after_skip.png` + `capture_log.txt` (audio, eye-glint timing, feet vs. ledge while hanging and
  vs. floor after landing incl. raycasts, wingspan, replay/skip), ~25 s. **Gallery check run:** same with `-gallery`
  (implies `-saucer`) → `G01_tv_across_deck.png … G21_left.png` + `capture_log.txt` (image count vs. JSON vs. files,
  idle timing, next/prev/wrap, aspect ratio of every image measured on the screen and in full view, a real-input test of
  every key/pad binding through virtual devices, audio), ~85 s. **Menu shot:** `-capture <dir> -menu` → `00_menu.png`.
  Monochrome check: max |R−G|,|G−B| over every pixel must be 0 (PIL).
- **Chapter 1 (Round 6)**: scene `Chapter1` (Moon level, built by `FightDemoBuilderChapter1.cs`; layout numbers in
  `Runtime/MoonLayout.cs`; `Atmosphere_Moon` prefab; walkable areas = `Ground.Shape.Zones`, rectangles that must
  overlap ≥ 0.6 m) → scenes 9–10 run in the `Saucer` scene (`Chapter1Aboard`, galley nook in `FightDemoBuilderSaucer.cs`).
  Flow + checkpoints: `Chapter1Director.cs`. Switches: `-chapter1`, `-ch1aboard` (start at scene 9).
  **Chapter 1 check run:** `SevenDemo.exe -mute -chapter1 -capture <dir> …` (autopilot, ~8 min; scene 10 plays every line
  in full) → `01_s01_cell_start.png … s12_pause_*.png` + `capture_log.txt` (objectives, checkpoints, any "SOFT-LOCK?"
  timeouts with positions, voice lines played vs. bank, cat distance, audio peak, `cursorVisible` per shot).
  **Galley check run (Round 10):** `-ch1aboard` (~3.5 min) → scene 9 + scene 10 + exploration + set-course placeholder:
  per-line timing / subtitle / "cat mouth moving", every scripted silence measured, marking close-up facing, marking-up,
  cat muzzle vs. bowl rim, eye close-ups + over-the-shoulder captures.
  **Opening test (QF4):** `SevenDemo.exe -mute -openingtest 25 -capture <dir> …` (~6.5 min) → runs Chapter 1's opening
  25× (idle / door at once / wanders / waits for the cat / runs along the beams, random start spots, frame hitches on
  odd runs); `capture_log.txt` must end `OPENING TEST: 25/25` (cat inside the cell 0 frames each run). The normal Chapter 1
  check run also logs `speaker view scene N …` (occluded / outside the frame during every line: must be 0 %), `WATCHDOG`
  fallbacks, Scree feet and `scree tucked abdomen vs legs` (must say "clears").
  **Cat-follow test (QF5; investigation only, not a standing check since QF6):** `-catfollowtest n` = n player-style runs
  from the cell rescue to the cracked wall (must end `CAT FOLLOW TEST: n/n, soft-locks 0`). `-noqf5catfix` = 0.10.0 (repro).
  The cat's leash (`ChapterCat.Leash`, allowed by `Chapter1Director` only while the player has 7): > 6 m behind or not
  closing in for 3 s → trot after her; blocked → fade (`_Fade` on `Seven/DissolveLit`) and reappear a step behind her out
  of view; route stops also go once 7 has passed them (`ChapterCat.ArcOf`).
  **Player-style run (QF6) — the release check, ONE run (testing rule 4):**
  `SevenDemo.exe -mute -testspeed 2 -playtest 5 -playtestfrom 4 -capture <dir> …` (`PlaytestRun.cs`, run 5 of the habit
  table, ~5 min) → the whole chapter from the main menu to "Set course" in the galley, played like a person.
  - Inputs: a virtual gamepad on the real input actions, the real menus (pause menu, Main menu, Continue), a free camera.
    No autopilot, no teleports.
  - The driver reads only what a player sees: the objective, the context prompt, 7's position or hang, the Scree guard.
  - Run 5's habits: it skips optional bits and ignores the pedestal (the doorway take), falls on purpose, quits to the
    menu and Continues at two checkpoints.
  - Must end `PLAYTEST: 1/1, soft-locks 0`. A soft-lock means the story has not moved for 240 s of game time.
  - `-playtest n` with other `-playtestfrom` values gives other habits (hurry / patient / backtrack / wander). More than
    one run is for investigations only.
  - `-noqf6fix` = the 0.10.1 behaviour (repro of Dan's soft-lock).
  - An autopilot pass alone does not count for a release. **Story triggers (QF6 rule):** no trigger may depend on one exact moment or spot — each fires
  at *or past* its place; every step the flow waits on is declared with `Chapter1Director.Step(...)` and watched by
  `StoryWatchdog` (re-shows the objective; returns 7 to the step if she is ever further along — must stay 0). Audit table:
  `Docs/TRIGGER-AUDIT.md` (update it when a trigger is added).
  **Checkpoints / Continue / Scene Select (QF6):** `Chapter1State.Cp*`: 0 cell · 1 escape (cat leads) · 2 old passage + ruins ·
  3 Scree guard · 4 vision · 5 lunar door + take the shape · 6 city reveal · 7 saucer rescue + beam · 8 aboard · 9 galley ·
  10 free exploration. Each is a restart point (d-pad down), the persistent Continue save (`SaveGame.Progress`, PlayerPrefs
  `seven_ch1_progress`; old `seven_ch1_aboard` saves load as 10) and a **Scene select** entry (main menu + pause menu;
  "Venus — later" listed, not selectable); `Chapter1State.StartAt(cp)` + `SetupCheckpoint` give the state a real playthrough
  has there. **Scene Select test:** `-testspeed 2 -sceneselecttest` (~12 min) → every entry started from the menu (6 from the pause menu)
  and played like a person to the next; must end `SCENE SELECT TEST: 10/10`.
  **Phantom-wall proof (QF3):** every build runs `FightDemoBuilderPhantomCheck.cs` on the Chapter 1 scene →
  `Docs/Previews/qf3/phantom_check.txt` + build log: city (layer 20) triangles in a walkable volume and inside the host
  structure must both be **0**; it also lists every non-city renderer without a collider in a walkable volume (props).
  Build the city host as its outer surface only (never nested boxes: their inner faces cut through the prison).
  **Telepathy sound check** (the one allowed unmuted step):
  `SevenDemo.exe -mute -soundcheck -capture <dir> …` → `soundcheck_log.txt` (~2 s of sound, then muted again).
  **SFX sound check** (Round 7): `-mute -soundcheck sfx -capture <dir> …` → 5 SFX, ≤ 1.2 s each (~5.4 s of sound), muted again.
  Round 7 switches: `-ch1cp <n>` (start Chapter 1 at checkpoint n, QF6 numbering; 6 = the city reveal onward, ~3 min check
  run), `-continue` (like the menu's Continue: the saved checkpoint; test runs use their own `seven_test_*` keys),
  `-cityreveal primary|alternate|off`.
  Chapter 1 check-run extras in `capture_log.txt`: laser-door collision (furthest z), line of sight cell → hidden
  release (must be 0), release panel in in-cell shots, energy point, every sound/music cue (`audio:` lines).
- **City reveal (Round 7, scene 6b)**: `FightDemoBuilderCity.cs` (host structure, hollow-Moon shell, terraces,
  pyramids, Tunneler's machines; layer 20, single-sided outward faces so it vanishes from inside the host; reflection
  probes ignore layer 20); runtime motion `CityMotion.cs`; fog blend `Chapter1Director.CityLook` (city preset numbers there).
- **Audio (Round 7)**: SFX `python Tools/Audio/generate_sfx.py [--force] [--only <id> …]` (ElevenLabs sound
  generation, key only from `ELEVENLABS_API_KEY`) → `Unity/Assets/Audio/SFX/<id>.wav`, list in `Docs/SFX-LIST.md`.
  Music = Dan's Suno MP3s in `Unity/Assets/Audio/Music/` (`Docs/MUSIC.md`). Build everything makes
  `Assets/Resources/Audio/SfxBank|MusicBank.asset` (by name; missing file = silence). Mix buses + volumes:
  `AudioKit.cs`; music cues/cross-fades/ducking: `MusicSystem.cs`; pause menu + Continue save: `PauseMenu.cs`.
- **Voices**: `python Tools/Voice/make_line_list.py` (DIALOGUE.md → `Docs/VOICE-LINES.md` + `Unity/Assets/Audio/Voice/voice_lines.json`;
  6b = scenes 65 primary / 66 alternate; 7's delivery notes from `Tools/Voice/seven_delivery.json`),
  then `python Tools/Voice/generate_voices.py [--speaker seven|cat] [--model v3|v2] [--force] [--only <id> …]` (ElevenLabs, key
  only from `ELEVENLABS_API_KEY`, voices "Seven" / "Cat" by name; 7 = Eleven v3 + audio tags, the cat = multilingual v2)
  → `Unity/Assets/Audio/Voice/<scene>/<id>.mp3` (7's Round 6 takes kept in `_v1/`); Build everything makes `VoiceBank.asset`.
- **Chapter 1 traversal + controls (Round 9)** — **Chapter control scheme** (Xbox / keyboard): left stick / WASD move ·
  right stick / mouse camera (no button; cursor locked while playing) · A / Space jump (grabs a ledge when jumping at it) ·
  X / E interact (doors, touch, sit) · B / C crouch-sneak toggle (at an edge: climb down into a hang) · hanging: up / W (or A)
  climb up, left-right shimmy, B / Space / C let go · RT / right mouse telekinesis · LB / Shift run · R3 / middle mouse / R
  re-centre · Y / Enter skip a line · Start / Esc pause · d-pad down / Backspace restart from the checkpoint. The fight demo
  keeps its own controls + legend; the saucer interacts with X / E (A still works). **No permanent legend in the chapter**:
  teaching prompts appear once each when first needed (`Chapter1Director.TeachWatch`, state in `Chapter1State.Taught`),
  then quiet context prompts (`ControlsLegend.Teach` / `.Context`); pause menu → Controls page, Settings → Button hints On/Off
  (`ControlsLegend.HintsOn`, PlayerPrefs). Traversal data (ledges, assisted jumps, grabs, wall climbs, mantles, climb-down
  spots, the fallen slab, cat stubs) in `Runtime/MoonTraversal.cs`; 7's moves in `ChapterSevenTraversal.cs` (hand IK on the
  lip; `MoonTraversal.HangLift` self-calibrates); level geometry `Editor/FightDemoBuilderTraversal.cs`; zones that are not
  walk-connected are joined only by those moves; `Ground.Zone.Low` = crouch only (the pipe run). Falls into a chasm → white
  fade → `ChapterSeven.LastSafe`. Cat routes = `ChapterCat.Route` (stops with "go" conditions). The vision object is
  `MoonLayout.VisionShape` (dodecahedron; swap the constant for tetrahedron / cube / octahedron). The lunar door chamber
  (`Editor/FightDemoBuilderLunarDoor.cs`, after `Art/Reference/ref_lunar_door.png`, in ruins) opens when 7 touches the
  round mechanism (`MoonLayout.MechanismSpot`). **The first vision = `Art/Video/vision_01_door.mp4`** (copied by Build
  everything to `Unity/Assets/Video/`, played by a VideoPlayer into `ChapterUI.SetVideo`: height filled, sides cropped).
  Mixamo clip measurements: `Docs/Previews/r9/clip_measurements.txt`. Chapter 1 check-run extras: `teach:` shown/cleared +
  `t_<lesson>_xbox|keys` shots, `traversal ok:` steps, `deliberate fall` resets (all must be ok), `hands on the ledge` gaps,
  `cat waits at <route> stop`, `traversal summary`, `LEGEND VISIBLE` (must never appear). The SFX sound check
  (`-soundcheck sfx`) now plays the 5 Round 9 traversal sounds.
- Play: run the exe normally → main menu (QF14: Continue (with a save) · Start new game · one entry per unlocked chapter
  (`Runtime/ChapterList.cs` - add a chapter there) → its page: Play from the start + its sections · Settings · Controls ·
  Quit; long lists scroll, `MenuScroll.cs`). The test demos are off the menu (`-fight` / `-saucer` / `-ratwing` still work).
  Menu shots: `-capture <dir> -menupages` (every page, main + pause, run once per resolution). `-menupick` paths:
  `"Venus|Venus: the debrief"`, `"Europa|Europa: Tunneler's base"`. Xbox controller or keyboard/mouse.
  Esc / Start = pause menu (Resume · Settings · Controls · Scene select · Main menu · Quit, with confirms); fight reset /
  checkpoint restart = Backspace / d-pad down.

- **Auto-update launcher + releases (Round 8; every round now ends with a published release)** — details and
  player instructions (Windows, Steam Deck/Proton) in `Docs/LAUNCHER.md`. `SevenLauncher.exe` (C++, `Launcher/`)
  installs/updates the game from GitHub releases of `danemoll-jpg/seven-releases` (public), verifies the signed catalog
  and starts `versions/<build>/SevenDemo.exe -sevenLauncherReady … -sevenLauncherNonce …` (`LauncherBridge.cs` answers).
  Signing key: `Builds/PublisherPrivate/launcher-signing.pem` (git-ignored; made once by `python Tools/New-LauncherKey.py`;
  **never delete, print or replace it**). Version: `LauncherBridge.Version` / `Build` (0.<round>.<fix>, build =
  round×100+fix; bump every round); Build everything writes `Unity\Builds\Seven\version.txt`. gh: `GH` env var or
  `C:/Users/danmo/Racer/Builds/PublisherTools/...` (not on PATH).
  1. Launcher (only if `Launcher/` changed): `powershell -File Tools/Build-Launcher.ps1 -CoreOnly`, then `powershell -File Tools/Build-Launcher.ps1`
     (VS 18 vcvars64, hidden/Below Normal) → `Builds\Launcher\SevenLauncher.exe` + `LauncherChecks.exe`.
  2. First release of a series: `python Tools/Prepare-LauncherRelease.py --version 0.8.0 --build 800 --notes "…" --out Builds/LauncherRelease-800`;
     later rounds: `python Tools/Prepare-ComponentUpdate.py game --previous-catalog Builds/LauncherRelease-<prev>/assets/update-catalog.json
     --previous-manifest Builds/LauncherRelease-<prev>/assets/game-manifest.json --out Builds/LauncherRelease-<n> --version 0.<r>.0 --build <n> --notes "…"`.
  3. `python Tools/Publish-LauncherRelease.py Builds/LauncherRelease-<n>` (dry run), then add `--publish --confirm-repository danemoll-jpg/seven-releases`
     (becomes Latest; `--resume-draft` if the draft isn't listed yet).
  4. Starter zip for new players: `python Tools/Package-LauncherStarter.py --release Builds/LauncherRelease-<n> --out Builds/LauncherStarter-<n>`,
     then publish it the same way (never Latest; after step 3).

## Kyle's art gallery (TV on the saucer) — how to add or change art
- The art lives in `Art/Gallery/` (Git LFS) with `Art/Gallery/gallery.json`: one line per image, in display order:
  `{ "file": "kyle_new_piece.jpg", "title": "Short plain title", "include": true }`. `include` is required
  (`false` hides a piece without deleting it). The credit line ("Art by Kyle G. Anderson") is the `credit` field at the top.
- **Add a piece:** copy the .jpg/.png into `Art/Gallery/`, add its line to `gallery.json` where it should appear
  (watch the commas), then **Build everything** (above). The builder copies `gallery.json` + every included image into
  `Unity/Assets/StreamingAssets/Gallery/` and so into the exe's `SevenDemo_Data/StreamingAssets/Gallery/`.
  Reorder = move lines; hide = `"include": false`; rename = change `title`. Any size/aspect works (shown grayscale,
  never stretched, downscaled to 2048 px on the long side when loading).
- **Without a rebuild:** the game reads `Unity\Builds\Seven\SevenDemo_Data\StreamingAssets\Gallery\gallery.json`
  and the images next to it every time the saucer scene starts, so dropping a file into that built folder and adding
  its line to that built `gallery.json` shows it on the next launch. That copy is overwritten by the next build, so
  make the same change in `Art/Gallery/` too. (Only included images are copied: to switch a hidden piece on in the
  built copy, also copy its file there.)
- Controls on the saucer: walk to the TV in the back nook → A / E "View art"; ← → / LB RB / d-pad / left stick
  = previous / next; Y / F = full view; B / Esc = leave. Code: `GalleryTV.cs` (runtime), `FightDemoBuilderGallery.cs`
  (nook, screen, sofa), shaders `Assets/Gallery/SevenGallery*.shader`.

- **Round 10 — the galley scene (scene 10, Docs/DIALOGUE.md; replaces the old scenes 10–12)**: cutscene in
  `Chapter1Aboard.cs` (`Galley()`; camera: over-the-shoulder both ways on speaker changes, eye close-ups on the key
  lines (`_eyeLines`), two-shot for silences; `CloseUpFocus` = Bokeh DOF from the `Assets/Atmosphere/Galley_Focus.asset`
  profile, which also keeps URP's DOF shaders in the build); 7's arm moves = `SevenTableIK` (IK pass), the cat's eating /
  lean / tilt / ear = `CatPoseOverlay` (ear bones `earL`/`earR` from `cleanup_cat.py`); set = `FightDemoBuilderGalley.cs`
  (tulip table + seats, plate, raised cat feeder, porthole with the Moon). After the scene: objective "Set course for
  Venus", free exploration (Continue starts here); X "Set course" at her galley seat = placeholder (Moon falls away,
  "Chapter 2 — Venus (coming soon)").
- **Dodecahedron** (hand-held, 15 cm, pale stone + faint edge lines, no bloom halo; QF7 glow 0.35 / table 0.3): `ElarimShape.cs`.
  QF7 flow: objective "Take the shape" from entering the chamber; walking up to it (no prompt or button) plays the
  vision as she touches it; she holds it when the vision ends, then puts it in a pocket (`CarriedShape.Where.Pocket`:
  shape, pouch and light all hidden). In the galley she takes it out of the pocket and sets it beside her plate.
  **The lock** (round mechanism, right of the door as in the vision) is buried under a collapse in the present
  (`LockRubble`, `FightDemoBuilderLunarDoor.cs`; ground obstacle). After the vision: "Uncover the lock" (RT telekinesis)
  → "Open the door" (X). The doorway opens only after that (`Docs/TRIGGER-AUDIT.md` rows 14–17).
  **Elarim marking = placeholder** `Art/Marks/elarim_mark_placeholder.png` (`Tools/Model/make_elarim_mark.py`; swap the
  PNG or `MoonLayout.ElarimMarkFile`) — OPEN until Kyle designs it.
- **QF7 cameras / life** (check-run lines):
  - Follow camera (`ChapterCamera.Place`): pulls in or swings round when her body is hidden, and fades collider-less
    props in the way → `follow camera (QF7): … hidden in N of M follow frames`.
  - Cutscene framings in the chamber: `Chapter1Director.FrameHer` (clear lines incl. props and rib meshes, never from
    behind; nothing found = follow view, with the rejection counts) → `framing '<beat>'`. The ruin column south of the
    alcove was moved to (19.15, −37.45) so the alcove beats have a side view.
  - Galley seat: `SaucerSeven.SeatHips` pins her hips to the seat → `SEAT CHECK (QF7)` (frames off the cushion or
    > 5 cm must be 0).
  - Galley cat: plain low bowl, front crouch + paw IK (`CatPoseOverlay.Crouch/PawBack`), and he looks at her when he
    speaks → `cat at his bowl` (paws outside the rim).
  - Head and gaze life: `SevenTableIK.Alive/Talk`, plus the emotion clips `sit_idle`, `sit_talking`, `talking` (from
    `Art/Mixamo/7/emotion/`, copied by Build everything).
  - Galley camera: medium shots, push-ins and reaction holds; no face close-ups.
- **Saucer exterior = silver** (Round 10, replaces the dark hull): materials `Saucer_hull` / `Saucer_under` in
  `FightDemoBuilderChapter1.cs`; `SaucerProbe.cs` = realtime reflection probe riding with the ship (layer 22, never sees itself).
- **Logo: "7" + THE OBSERVER in Arial Bold (image); UI font Liberation Sans if needed.** `python Tools/make_logo.py` →
  `Art/Logo/logo_7_observer{,_white,_dark}.png`, `logo_7.png` (+ `Unity/Assets/Resources/Logo/`); Arial's .ttf is never
  copied into the repo or a build. Used by `Logo.cs` (main menu title, pause header) and `Tools/make_launcher_art.py`
  (cover, icon, poster). UI text stays on Unity's built-in LegacyRuntime font (Arial-metric); bundle Liberation Sans
  Bold (SIL OFL) only if a font must ship.

## Where things stand
**Rounds 1–16 + QF1–QF22 complete (2026-11-21), release 0.16.2.** QF22 = the Biavian contact is the mothership at Veyra (star map, window view, re-voiced lines; `WorldFlags.ContactName`). QF21 = Dan's Asteroid review (the camera rules, wrong-state prompts, mount first, the Machinist's calm / steps / restraint, the blades held out, the foundry hall, System, AST_01's dissolving hull). Round 16 = Chapter "The Asteroid — the Great Machine Age"
(first playable pass). QF20 = the Biaveh discovery can't be missed, kicks on Y,
bosses keep their damage. QF19 = the Viper fight's pylons work (one press). QF18 = show it, don't write it + Dan's Mars review
(the "Show, don't caption" rules above; Mars's ground, ramps, collision, puzzles, story beats, Viper, the boss loop with
energy bars, visions, holograms, window views, the two-level star map). QF17 = real boss fights (the "Boss fights" rule
above: Viper and the Wormmaster rebuilt), the Mars architecture kit, the ramp cut-away rule, the bike-hold rule on every
start path, the Mars clunk pass. QF16 = the view outside per world, the
sacred-geometry passage, varied staging aboard, Panther's cat moments. Round 15 = Chapter "Mars — The Divided Kingdoms" (first playable pass). Round 14 = Chapter "Europa — Beneath the Ice" (first playable pass). Round 11 = Chapter 2 "Venus", part 1 (to VEN_S04); Round 12 = part 2 (to VEN_S08 + destinations). Chapter 1 "The Moon Prison" ends with the galley
scene (Round 10).
- QF5 (Dan's 0.10.0 playtest): the cat can no longer be left behind, and the lock mechanism is on the right as in the
  vision.
- QF6 (Dan's 0.10.1 soft-lock):
  - The shape is taken automatically.
  - There are no points of no return; the doorway stays closed until she has the shape.
  - Every story step is declared and watched.
  - Checkpoints save for Continue, and Scene select is in both menus.
  - New testing rule: under 20 minutes per round.
- QF7 (Dan's 0.10.2 playtest):
  - The follow camera keeps 7's body in view (pull in, swing round, fade collider-less props). Check runs log
    `follow camera (QF7): 7's body hidden in N of M follow frames`.
  - The shape is taken in one step (the vision plays as she touches it), then goes into her pocket (no glow on her).
  - The lock is buried under a collapse until the vision; RT clears it, X opens the door.
  - Galley: hips pinned to the seat (`SEAT CHECK` line), plain bowl + cat front crouch, emotion clips, medium shots
    instead of face close-ups. Facial shape keys were trialled and dropped.

- QF8 (Dan's 0.10.4 play): shared systems Venus builds on —
  - `CameraOccluders` (visual-only meshes as colliders in a separate physics scene; camera, framings and wall checks query it),
    follow camera casts from her body, pulls in / swings / cuts (up to 72°), dithered held fades; `-camsweep` = the camera sweep
    over the traversal route (`CAMERA SWEEP: … under 60 %: N`); check runs log `follow camera visibility (QF8)`.
  - `ShotCheck` (her baked body + hair vs the cat: `cat N % unobstructed`), used for over-the-shoulder framings.
  - `ChapterSevenWalls` (rendered push out of walls, `wall clearance (QF8)` line); `CarriedShape` palm grip + finger curl
    (`grip '…': palm contact error`), `HandLeft` too.
  - `Tools/Voice/retake_no_pause.py <id> <word>` re-takes a line and measures the gap after the word.
- QF9: the door-frame flicker was z-fighting between coplanar level blocks — `FightDemoBuilderZFight.cs` moves them apart at
  every Chapter 1 build (`QF9 z-fighting check … 0 left`, `Docs/Previews/qf9/zfight_check_Chapter1.txt`); architecture
  never fades for the camera (`CameraOccluders.IsLooseProp`); URP shadow bias 1 / 1, main shadow map 4096. `-flickertest`
  = door frames / walls measured frame by frame in motion (`FLICKER TEST: … events N`, `-noqf9fix` = 0.10.5 fade + shadow
  values). **The terrace fight** (fighting tutorial, the two terrace Scree before 6b): `ChapterSevenFight.cs` (7: combo,
  block, dodge, takedown; Attack X/LMB/F, Block LT/Q, Dodge B/Ctrl, Takedown Y/E), `ScreeGuardFight.cs` (slow telegraphed
  punch, dazed after 3 blows, takedown → down → sits up, alive), `Chapter1Director.ExitFight` (arena, first-use prompts,
  restart on knockdown, 90 s watchdog). `-fightonly` = check run from checkpoint 6 that stops at the reveal (~1 min).
- **Round 11 — Venus, part 1** (spec `Docs/CHAPTER2_VENUS.md`):
  - Scene `Venus`: `FightDemoBuilderVenus.cs`, numbers in `Runtime/VenusLayout.cs`, `Atmosphere_Venus`.
  - Flow: `VenusDirector.cs`, C01–C05 + B01–B05. Progression + persistence: `VenusState.cs` (PlayerPrefs `seven_venus`).
  - Dialogue as data: `Tools/Voice/make_venus_lines.py` → `Resources/Venus/venus_dialogue.json`; voices with
    `Tools/Voice/generate_venus_voices.py` → `Resources/VenusVoice/<id>.mp3`.
  - Bike: `HoverBike.cs` + `ChapterSevenRide.cs`. Characters: `ElarimNpc.cs`, `WormCreature.cs`, `VenusFlyer.cs`.
  - Models: `F:lenderlender.exe --background --factory-startup --python Tools/Blender/cleanup_r11.py -- <repo> bike|elarim|worm`
    (Elarim skinned to Dan's Mixamo skeleton from `Art/Mixamo/elarim/elarim_idle.fbx`; Unity uses an explicit Mixamo human
    map). Panther's `stretch` / `lie_down` clips: `cleanup_cat.py`.
  - Switches:
    - `-chapter2`: start Venus (a fresh start in check runs).
    - `-venusskip`: skip every line.
    - `-venusreload`: reload after every committed scene.
    - `-venuschain`: aboard → Set course → Venus.
    - `-venusstatetest`: check the progression / persistence rules.
    - `-chapter2 -quick`: stop after B01.
  - Check-run lines: `scene VEN_… complete`, `RELOAD after …`, `VENUS PART 1 DONE: …`.
- **QF10 (Dan's 0.11.0 play), releases 0.11.1 + 0.11.2 (hands in the bike grips):**
  - Venus ground:
    - Natural sculpted ground: `Runtime/VenusTerrain.cs` is one height function for the builder and the game. The
      `Valley` ground zone uses `Ground.Zone.Field / Mask`; the mesh is built by `Editor/FightDemoBuilderTerrain.cs`.
    - The collapse is a gully, crossed on the bike over the slab she lays with RT (`VenusDirector.Bridge`, saved as
      `VEN_B03_bridge`).
    - Grass: `Seven/Grass` shader, baked chunks (`Editor/FightDemoBuilderGrass.cs`).
  - B01: hull hatch + Panther framing (`FrameAll`, logs "Panther N % off screen").
  - Riding:
    - Bike: 3 m; points in `Art/Export/hoverbike/hoverbike_points.json`, re-measured by raycast in `HoverBike.MeasureBody`.
    - Riding pose = Blender-made racing tuck:
      `F:\blender\blender.exe --background --factory-startup --python Tools/Blender/make_ride_pose.py -- <repo>`
      → `Art/Export/anim/7_ride.fbx` (state `ride`, own avatar), plus IK in `ChapterSevenRide`.
  - Elarim cape: on its own bones (`cleanup_r11.py elarim`), driven by `ElarimCape.cs`.
  - Traversal: mantles and hang-downs work anywhere along their edge (`MoonTraversal.MantleHere` etc.).
  - Fighting feel: `ChapterSevenFight` (numbers + `CameraKick`); fight demo `SevenFighter.AttackSpeed`.
  - Scree voice: `ScreeVoice.cs`, knobs in `ScreeVoiceSettings`.
  - Venus waits: every wait has a hint (45 s) and a skip (2 min).
  - Checks:
    - `-venusplay [-venushintfast] -testspeed 2` = the player-style Venus run, `VenusPlaytest.cs`; must end
      `VENUS PLAYTEST: 1/1`.
    - The C03–C05 "coverage" lines (speaker in frame).
- **Round 12 — QF11 + Venus part 2 (release 0.12.0):**
  - Menu: "Chapter 2 — Venus" always starts fresh (confirm "Start Chapter 2 over?" when a Venus save exists); Continue
    resumes. Scene Select → "Chapter 2 — Venus >" page: 11 entries (`VenusState.SelectEntries`), each on its own save
    slot (`seven_venus_select`; the player's `seven_venus` is never touched).
  - The cat's walk follows his real ground speed (`ChapterCat` / `CatCompanion`; `cat speed vs clip (QF11)` line in every
    check run). The bike is hidden until the hull hatch has opened (`bike vs the hull (QF11)` lines).
  - Part 2 flow: `VenusDirectorPart2.cs` (way down → C06 → encounter → C07 → B06 → C08 → C09/C10 → B07 → aboard);
    debrief + destination screen: `Chapter1AboardVenus.cs` + `DestinationScreen.cs` (the Moon, Venus, Stay aboard).
    Layout: `VenusLayout` (RootPit / tunnels / `Control` zones, devices, troughs); builder `FightDemoBuilderVenus2.cs`;
    `ControlDevice.cs`, `WormmasterRig.cs` (procedural, head tip swells with his voice), `WormCreature` (troughs, telegraph,
    withdraw, calm).
  - Models: `cleanup_r11.py ... wormmaster` (Humanoid-compatible rig + `WmArm*` curl bones + `HeadTip`, ~4 min);
    `F:lenderlender.exe --background --factory-startup --python Tools/Blender/make_kneel_pose.py -- <repo>` (the
    guardian's real kneel; Dan's Mixamo "kneel" never kneels). Both before the Unity build.
  - Vision 2: `Art/Video/vision_02_venus.mp4` (copied by Build everything); still fallback `Resources/Venus/vision_02_still.jpg`.
  - Switches: `-venusselect <id>` (start like a Scene Select entry), `-venusselecttest` (every entry, one capture each),
    `-venusplay2` (player-style run, observation area → destination screen), `-novideo` (still fallback),
    `-venusstopatship`, `-venusquick2` (stop after C05), `-menu -menuvenusconfirm` (menu shot with the confirm).
  - Voices: `generate_venus_voices.py --round 12` (Wormmaster = Dan's voice "wormmaster").
  - Chapter 1 opening shot (2d): `MoonOpening.cs` (procedural screen-space: the Moon + stars + glow, the logo, push-in,
    the dive through the crust, the tunnels → the cell). Plays on "Chapter 1" from the menu and Scene Select's first
    entry (`Chapter1State.Intro`), never on Continue; skippable (A / Esc / Enter). Check: `-openingshot` (~25 s, captures
    `op01–op03` + `s01_cell_start`, then quits).
- **Round 13 (release 0.13.0)** — Dan's 0.12.0 fixes + conversation reactions + the ship's console:
  - Saucer: no ramp anywhere (see "Saucer — fixed design"); Venus hovers it (underside 3.35 m), `VenusDirector.BeamDown/BeamUp`.
  - New game: "Chapter 1" from the menu asks once and clears Chapter 1 / Venus / console progress (`SaveGame.NewGame`);
    `SaveGame.Furthest` = furthest Chapter 1 checkpoint (console Log / Field Guide).
  - Guardian rig: `cleanup_r11.py elarim` welds the Tripo pieces, cuts the cape off (`split_cape`), skins the body alone
    with automatic weights, cape on CapeA/B; bend test previews `elarim_bendtest_*`. `cleanup_r11.py ... elarim_mixamo` →
    `Art/Export/elarim/elarim_body_for_mixamo.fbx` (cape-less body for Mixamo) + `elarim_cape_part.fbx`.
  - Venus: wider slab bridge with ramped ends (`BridgeRampS/N` zones, `BridgeRamps` aprons), bike glides along edges
    (`HoverBike.ClampBike`, `Snags`); bike sound fixed (`EnsureAudio`) + whoosh / lift-off / settle / mount / dismount;
    `CameraShake.Quake` (real shake) + `VenusDirector.Tremor/TremorAndCollapse/Brace` (B03 walkway collapse, C04);
    `VenusWildlife.cs` (birds, insects, distant calls, rustles) + flapping birds (`VenusFlyer.Flap`);
    encounter fights back (`VenusDirectorPart2.Harass`, `WormCreature.Lunge/Stun`, taunts `VEN_ENC_T1-4`);
    C03_11 "Follow me." (`GuardianLeads`); VEN_C12 (`C12()`, the locations display); C11 + C13 in the pilot's chair
    (`Chapter1AboardVenus.VenusDebriefChair / C13`, Panther on her lap via `CatCompanion.HopOntoLap / LapFrame`).
  - Music: `MusicSystem.Suspend/Resume` around both visions (resumes where it left off; 60 s watchdog).
  - Venus dialogue: `make_venus_lines.py` keeps Dan's delivery directions ("7 — casually" → `direction`) and branch ids
    (`VEN_C13_M1`, `_E1`; `VenusDialogue.LineNumber` = 901 / 951); `generate_venus_voices.py` turns directions into v3 tags
    (`TAGS`), `--taunts` = the Wormmaster's encounter barks.
  - Reactions: `Docs/REACTIONS.md`, data `Resources/Reactions/reactions.json`, bank `python Tools/Voice/generate_reactions.py`,
    runtime `Reactions.cs` (hooked in `DialoguePlayer.PlayOne`; `-noreactions` = off).
  - Console: `ShipConsole.cs` (+ star map; Log `Resources/Console/adventure_log.json` = `Docs/ADVENTURE-LOG.md`, Field Guide
    `field_guide.json` = `Docs/FIELD-GUIDE.md`, portraits `Tools/Blender/render_portraits.py` → `Resources/Console/Portraits`).
  - Round 13 check switches: `-r13 bridge|tremor|temple|encounter|console|consolefresh` (focused short runs).
- **QF12 (release 0.13.1)** — Dan's 0.13.0 play (status + "For Dan to check (QF12)" in `TODO.md`):
  - **Builder rule: a mesh name is saved once per build** (`SaveMesh`: repeats get `_2`, logged "mesh name reused") —
    reused names had deleted earlier meshes (empty cube pedestal, two device pylons) and swapped the Moon hull for Venus's.
  - Guardian = Dan's Mixamo rig: `F:lenderlender.exe --background --factory-startup --python Tools/Blender/cleanup_r11.py -- <repo> elarim_rig`
    (Art/Mixamo/elarim/elarim_body_mixamo.fbx + elarim_cape_part.fbx → elarim_v1_rigged.fbx); procedural kneel fallback in `ElarimNpc`.
  - `WormCreature.BaseY` is not serialized — set it at runtime (`RestorePart2`). Particle materials: `AdditiveMatTex` (soft dot), never `AdditiveMat` (squares).
  - Check tools: `-menupick "Label|Label"` (`MenuPilot`: a run starting in the real main menu), `-stopafter <s>`,
    `BodyProbe` ("body still" line), `CameraOccluders.WalkBlockers` ("walk blockers" line).
- **Round 14 — Europa (release 0.14.0)** (spec `Docs/CHAPTER_EUROPA.md`; status, decisions, "For Dan to check" in `TODO.md`):
  - Scene `Europa`: `Editor/FightDemoBuilderEuropa.cs`, numbers + swim volumes + base zones + checkpoints in `Runtime/EuropaLayout.cs`,
    `Atmosphere_Europa`. Flow `EuropaDirector.cs` + `EuropaDirectorBase.cs`; saves `EuropaState.cs` (`seven_europa`, select
    slot `seven_europa_select`); aboard parts (title + chair scene, reunion, debrief) `Chapter1AboardEuropa.cs`.
  - The bike swims: `HoverBike.Swim` (A up / B down, `SwimClamp`). Helmet `BubbleHelmet.cs`. Cetaceans `Cetacean.cs`
    (procedural). Icosahedron `Icosahedron` (code). Guide light = `EuropaDirector.NextMarker`; base rooms routed door to
    door (`RouteTo`); zones must overlap ≥ 0.6 m at doors.
  - Models (before the Unity build): `F:lenderlender.exe --background --factory-startup --python Tools/Blender/cleanup_cetacean.py -- <repo> both`
    (~3 min) and `... cleanup_tunneler.py -- <repo>` (~1 min).
  - Dialogue: `python Tools/Voice/make_europa_lines.py` → `Resources/Europa/europa_dialogue.json` + `Docs/EUROPA-LINES.md`;
    voices `python Tools/Voice/generate_europa_voices.py` → `Resources/EuropaVoice/` (Keeper / Tunneler / Seven / Cat by
    name; Scree = a premade voice).
  - Switches: `-europacp <n>` (check run at checkpoint n), `-europa` (start as from the star map), `-europarace`
    (autopilot rides the race), `-europaplay` (player-style run `EuropaPlaytest.cs`; with
    `-menupick "Scene select|Chapter 2 — Venus|Venus: the debrief"` it starts in the main menu and chooses Europa on the star map).
- **QF13 (release 0.14.1)** — Dan's 0.14.0 play (status + "For Dan to check (QF13)" in `TODO.md`):
  - One saucer prefab (`Assets/Saucer/Saucer.prefab`, `SaucerPrefab` / `PlaceSaucer` / `CheckSaucer`), the bike hold
    (`SaucerHold.cs`, `Seven/HoldInterior`); the bike always uses the hold (Venus B01 / B07, Europa).
  - Guardian: the Elarim FBX is imported **Read/Write on** (ElarimCape reads the cape vertices - it was empty before);
    the cape has four bones (`CapeA/B` chest→hips, `CapeA2/B2` hips→hem; `cleanup_r11.py … elarim_rig`), hangs with
    gravity, is re-seated to the model's height; the walk = `elarim_walk_v2.fbx` if present, else 7's walk, playback
    matched to his speed (`ElarimNpc.WalkMatch`). Check-run lines `guardian (QF13 17) …` (`GuardSeq`, player camera + side).
  - Hand IK lifecycle: `SevenTableIK.ReleaseAll` at every stand-up / dismount + a safety net (out of reach / walking away).
  - Venus: `CollapseWatch` (B03 breaks ahead, on distance + speed); the root pit (ground cells across its edges left out,
    `Tunnel_Headwall`), lamps = `Lantern(...)` (crystal on a root bracket, never a floating box); objectives say where.
  - Bike: land 24 m/s, under water 25 m/s; under water the stick pitches (push forward = dive; Settings "Invert up /
    down", `HoverBike.InvertPitch`), collision under water (`SlideSwim` against `CameraOccluders`), speed streaks + FOV push
    (`ChapterCamera.SpeedKick`).
  - Europa: `Seven/Bubble` helmet + collar; guide light + on-screen marker (`ChapterUI.SetMarker`) + "which way" hints,
    skip after 60 s riding; base cut-away camera (`BaseCutaway`); swim paths checked at load (`ValidateSwimPaths`);
    `SmallCraft` vessels, launch tunnel + docking passage; drill spins (`DrillSpin`); the escape chase (`Detonation`,
    `EscapeBlast`, framings kept inside the tunnel, the burst-out); EUR_16_01..03 cut (`make_europa_lines.py` START).
  - Field guide entries grow: optional `more` paragraphs with their own unlock (`ShipConsole.GuideMore`).
  - Check tools: `ChapterCapture.ShotFrom` (a second angle of the same frame).
- **QF14 (release 0.14.2)** — Dan's 0.14.1 play (status + "For Dan to check (QF14)" in `TODO.md`):
  - Menu: one chapter list (`ChapterList.cs`), chapter → sections pages in the main and pause menus, scrolling lists.
  - The bike bay = a real chrome room (`FightDemoBuilderBikeBay.cs`; `SaucerHold.Sink` / `BayLights` / `BayProbe`).
  - Europa race: "Race again" + the arch re-arms; best time (`EuropaState.RaceBestTime`); `ChapterCamera.RaceMode` (no swings
    or cuts, turns toward the next ring). `-europacp 2 -europarace` runs two races.
  - Base camera: `ChapterCamera.Cutaway` (walls cut away instead of moving the camera; the measure `CutawayJitter /
    CutawayBounces`). Proof run: `-europaplay -europacp 4 -testspeed 2` → `BASE WALK: 1/1, bounces 0`,
    `base_camera_frames.csv`, `qf14_06_walk_*` captures.
  - Guardian walk = `elarim_walk_v2` (root motion not baked, playback matched; `ElarimNpc.CalmSway` damps the hips by a third).
- **QF15 (release 0.14.3)** — the bike camera + walls in Europa (status + "For Dan to check (QF15)" in `TODO.md`):
  - **One riding camera** (`ChapterCameraRide.cs`, partial of `ChapterCamera`): whenever she rides (any world, outside
    cutscenes) none of the on-foot camera logic runs. Behind / above the bike, heading + nose pitch through a two-stage
    first-order lag (never overshoots), turn-rate cap 140°/s, never below her level; non-shell meshes in the way are **cut
    away** (`forceRenderingOff`, held 0.8 s); only shells (names with Ceiling / Floor / Passage / Tunnel / Wall / Hall /
    Tube / Terrain / Ground …) bring it in: wide probe + clearance + look-ahead 0.6 s, eased in 0.5 s, out only after 1.2 s
    clear, dead band 0.5 m; a last guard keeps the camera out of geometry. Hand-overs from a cutscene far round / behind a
    wall and bike teleports (skips) are explicit cuts. Race aim = a slow ease (`_raceW`). Measure in every check run:
    `ride_camera_frames.csv` + `RIDE CAMERA (QF15 1)` line (aim / position shifts, reversals, inside / behind geometry).
  - **Full collision under water**: `RideProbe.cs` (real colliders + every visual mesh, both faces, `Depenetrate`);
    `CameraOccluders.IncludeHuge` (Europa / Mars: no 60 m size cut; two-sided collider copies); the swim clamp's move is
    swept too; flying debris glances off the bike. Measure: `BIKE vs GEOMETRY (QF15 2)`. **Wall test:**
    `-europawalltest -testspeed 2` (`EuropaWallTest.cs`, ~3.5 min) → `WALL TEST (QF15 2): N attempts …, failed 0`.
  - Merged Europa meshes split per piece (`AncientStructure*`, `ChamberSpire*`, `RacePillar*`, `Platform*`) so the camera
    cuts away only the one in its way. Star map input reads every pad (`ShipConsole.ChooseInput`).
  - Europa reunion (EUR_16) cut: the beam-up fades straight into the chair debrief.
- **Round 15 — Mars (release 0.15.0)** (spec `Docs/CHAPTER_MARS.md` incl. Dan's revised sections 10 + 13; status, decisions,
  "For Dan to check" in `TODO.md`):
  - Scene `Mars`: `Editor/FightDemoBuilderMars.cs` (plain, ravine, settlement, checkpoint, maintenance route, rims, mechanism,
    controls, the two spans, the temple island + arena, the temple; `Atmosphere_Mars`); numbers + zones + checkpoints
    `Runtime/MarsLayout.cs`. Flow `MarsDirector.cs` (helpers: `Until`, `RideRoute`, `Interact`, `Telekinesis`, `GuideWalk`,
    `TwoShot` / `ShotClear` = framings with a clear view) + `MarsDirectorFlow.cs` (1-6) + `MarsDirectorTemple.cs` (7-11).
    Saves `MarsState.cs` (`seven_mars`, select slot `seven_mars_select`) + `WorldFlags` (completed-world flags, the
    combined-record / asteroid route flags in `seven_routes`; Biaveh only after Mars + Europa, played once whichever is
    second; the asteroid from the TRV_AST travel event). Aboard: `Chapter1AboardMars.cs` (title + chair arrival, MAR_12,
    MAR_13U / 13C / 13B, `CombinedRecords` - also called after EUR_17 when Europa is finished second -, `TravelAsteroid`,
    `TravelToBiaveh`). Rune = `Tetrahedron` (code).
  - Characters on Dan's Mixamo rigs: `SetupMixamoFolk` (Humanoid, explicit Mixamo map, clips per MODELS-NEEDED);
    `MarsFolk.cs` (walk at its real speed, the Orc walk's hips damped, voice-driven head / body, Viper's hood flare).
  - The bike on land per chapter: `HoverBike.ZoneTest` (Mars = `MarsLayout.BikeZone`).
  - Lines: `python Tools/Voice/make_mars_lines.py` (reads the `**SPEAKER:**` lines and Dan's section-13 tables) →
    `Resources/Mars/mars_dialogue.json` + `Docs/MARS-LINES.md`; voices `python Tools/Voice/generate_mars_voices.py`
    (`Viper` saved voice; premade Daniel = guard, Sarah = worker, Bill = caretaker) → `Resources/MarsVoice/`.
  - Switches: `-marscp <n>` (check run at checkpoint n; 8 = aboard), `-mars` (as from the star map), `-marsplay` (player-style
    run `MarsPlaytest.cs`; with `-menupick "Venus|Venus: the debrief"` it starts in the main menu and picks Mars at VEN_C13),
    `-marspick europa|asteroid|biaveh`, `-routes none|europa|mars` (fake the world flags: neither / Europa done / Mars done).
- **QF16 (release 0.15.1)** — the view outside, the passage, staging, Panther's cat moments (status + "For Dan to check
  (QF16)" in `TODO.md`):
  - **Where the saucer is** = `SaucerLocation` (`Runtime/SaucerOutside.cs`, PlayerPrefs `seven_ship_at`; "space" while
    travelling). Set by each aboard part on start and by every journey on arrival. `SaucerOutside` shows that world's
    backdrop (`Editor/FightDemoBuilderOutside.cs`: Moon + Earth, Venus hills + cloud ceiling, Europa ice + Jupiter, Mars
    plain / walls / ravine / two cities, stars; all at y −200) + its haze preset + the galley porthole disc. The Round 3
    city only for the saucer demo and Chapter 1 scene 9.
  - **Journeys** = `TravelPassage.Run(to)` (~8 s, skippable): rings of her collected solids (dodecahedron always, cube /
    icosahedron / tetrahedron per record), outside shot of the one saucer prefab parked in `TravelStage` (far above the
    deck), then through the window, fold shut. Called from the star map, VEN_C13, the destination screen, MAR_13U, the
    first trip toward Biaveh (`TowardBiaveh`).
  - **Staging table** `Chapter1AboardStaging.Table` (scene → Chair / ChairCatConsole / Sofa / FloorSofa / Window /
    WindowShoulder); `EnterStaging` / `SwitchStaging` / `ExitStaging`; `CutStage` maps the old framing keywords. 7's
    lying / floor-sitting poses = `SevenRestPose.cs` (bones aimed after the animator; Blender pose clips retarget to the
    muscle-zero pose — don't use them).
  - **Cat moments** `Chapter1AboardCatMoments.cs` (scripted C13 / MAR_13C / EUR_01 / MAR_01; ambient loop every 2–4 min
    while free aboard; `CatPoseOverlay.PawTo / TailToward / TailTwitch`, `CatCompanion.ShowCurled`).
  - Check: `-qf16check` (~3.5 min: every ambient moment, then journeys Moon → Venus → Europa → Mars → Moon with window +
    porthole captures, ends `QF16 CHECK: cat moments N, journeys 4`).
- **QF17 (release 0.15.2)** — real boss fights + Mars clean-up (status + "For Dan to check (QF17)" in `TODO.md`):
  - **Boss fights** (rule above): `BossFight.cs` + `BossPilot.cs` + `ViperFight.cs` / `WormmasterFight.cs`, table
    `Resources/Bosses/boss_tuning.json`. Viper: platform (spit, energy sweep, barrier cage; two pylons X under fire) → he
    hunts her (snake strike, tail sweep, spit, the third pylon's sweep) → enraged; the plaza is one open arena (zone
    `Arena`, his raised platform `ViperDais`). Wormmaster: from his dais (lash, sent creatures, slam wave; north device X,
    ledge device RT from below) → he comes down (arm sweep, lash, slam wave; the crystal RT) → the signal fails (shock
    ring, combos); creatures only hurt when he sends one. Switches: `-bossidle` (idle test, ends at her first knockdown),
    `-bosscheck` (the run ends after the fight), `-bossnovice` (the pilot plays like a first-time player). Runs:
    Viper `-marsplay -marscp 5 -bosscheck [-bossnovice]`, idle `-marscp 5 -bossidle`; Wormmaster `-venusplay2 -bosscheck
    [-bossnovice]`, idle `-r13 encounter -bossidle`.
  - **Mars architecture kit** `Editor/FightDemoBuilderMarsArch.cs`: `MBox` / `MBlock` (dressed masonry - battered,
    plinth, chamfer; replaces every cube in the Mars builder), `Tower`, `Gateway`, `ArchInset`, `ReptHouse` (stepped
    terraced houses), `FortressCity`, `RoundSlab` (the temple's older smooth style), `MarsLamp`. No `Block` / `AddBox` in
    `FightDemoBuilderMars.cs` any more.
  - **Cameras never cut away anything she rides or walks on** (`CameraOccluders.MayCutAway` / `IsWalkableName`: ramps,
    stairs, decks, bridges, spans, roads, slabs, platforms, floors; or anything at / under her) - the riding camera and
    the Europa base cut-away. Proof `-rampcheck` (with `-marscp 2` / `-chapter2`) → `RAMP CHECK (QF17 4) … cut-away under her 0`.
  - **Bike hold on every start path**: `BikeHoldWatch` (`BIKE HOLD CHECK (QF17 5) <world>: PASS / FAIL` in every run that
    plays an arrival; `-holdcheck` ends the run once she is on the bike). Mars entered from a save at the landing site
    (Continue, the star map continuing a save, Scene Select "the crossing") plays `Relanding()` (hatch → bike → beam).
- **QF18 (release 0.15.3)** — show it, don't write it + Dan's Mars review (status + "For Dan to check (QF18)" in `TODO.md`):
  - **Standing rules** "Show, don't caption" (top of this file): no descriptive captions, objectives from the story, no
    filler tasks, characters shown talking have voiced lines, every dialogue shot shows the speaker
    (`DialogueFraming`, inside `DialoguePlayer.PlayOne`: every chapter; log `DIALOGUE FRAMING (QF18 7)`), done states,
    readable text over the world (`ChapterUI` soft panels + outlines).
  - **Ground = `MGround`** (flat-topped terrain) in the Mars builder; `MBlock` (battered masonry) only for structures - a
    battered ground block opens a groove at every seam. **Build checks on every scene** (`FightDemoBuilderQF18Checks.cs`):
    `COPLANAR CHECK / FIX (QF18 11)` (any mesh, nudges the smaller of a fighting pair 4 mm), `HOLES CHECK (QF18 13)` (rays
    down over every zone; Mars fails the build above 0), `NPC PLACEMENT (QF18 6)`; reports in `Docs/Previews/qf18/`.
    Runtime: `ZFightProbe.Report` for things that move into place.
  - **Land collision** on every world: `RideProbe.SlideLand` (the bike on land; 7 on foot, `ChapterSeven.FootCollide`),
    measured `BIKE vs GEOMETRY ON LAND (QF18 10)`, `FEET vs GROUND (QF18 16)` (`FeetProbe`); `-landwalltest` (Mars: the
    land bike at full speed into and along walls + on foot, ends `LAND WALL TEST (QF18 10)`).
  - **Mars level**: ramps run out onto landings (`MarsLayout.RampFootZ / RampLanding`), a real channel down the ravine
    floor, the **drawbridge** (`MarsDrawbridge`: raised + chained by one kingdom, the lock broken with TK), the service gate's
    cut **power line** (hold RT to join the ends; `PowerLine` = light that visibly runs), the pier walk outside the bank wall,
    real controls / mechanism / relay / conduit (`FightDemoBuilderMars.cs`), Viper's tech (`MarsPylon`, `MarsSentry`, the
    loudspeaker horn), the landscape (`FightDemoBuilderMarsLand.cs`: dune sea, mesas, boulders, the Cydonia mesa
    `MarsLayout.FaceAt`, a pale sun, `MarsDust`). The Mars folk avatars use a true T-pose reference (their A-pose arms).
  - **Mars story**: named line ids in the spec's tables (`| MAR_06_V1 | Worker | ... |`, `make_mars_lines.py` reads them;
    `MarsDialogue.Ids(...)`); the worker's route lines (MAR_03_11-15), the volunteer (MAR_06_V*), the power chain
    (MAR_06_P1), the sentries (MAR_06_D*), the hold (MAR_06_H*), the guard / caretaker (MAR_11_P*); Viper close-ups
    (`ViperCloseUp`, `OverHerShoulderUp`, `FogThin`, his eye glow).
  - **Boss loop** (rule above): `BossDef.stunTime / strikeDamage / kickDamage / openStrikeScale / dazedAt`,
    `PhaseDef.endAt` (energy marks) in `boss_tuning.json`; `BossFight.Energy`, `DAMAGE (QF18)` in the summary; her full
    chain jab-cross-hook-mma_kick-roundhouse in boss fights; lock-on toggle `ChapterSeven.LockOn` (R3 / Tab);
    `ChapterUI.BossBar / HerBar`; pylon / device done states + counters.
  - **Visions**: `VisionTouch.Into / Back` - the one touch -> vision -> return sequence (Moon, Venus, Europa, Mars).
  - **Aboard**: `HoloDisplay` (navigation hologram from the real level: `Resources/Holo/Holo_<world>`; markers, zoom,
    MAR_13C's two records), window views from the real level (`Resources/Outside/Outside_<world>`, built by
    `FightDemoBuilderWorldViews.cs` after every level; `SaucerOutside.LevelView`), the shoulder perch (`PerchCat`).
  - **Star map** two levels (`StarMap.cs`: our solar system / the Biaveh system / the overview between them, gateways once
    Biaveh is unlocked); `SaucerLocation.System` ("sol" / "biaveh"; the windows show Biaveh's star while there).
- **QF19 (release 0.15.4)** — the Viper fight's pylons work (status + "For Dan to check (QF19)" in `TODO.md`):
  - **Fight objectives are ONE press, instant** (no hold / channel - it is gone from `BossFight`): in reach (pylons /
    the Venus north device: 3 m) X always uses the objective, never a strike, whatever she is doing (`ChapterSevenFight`);
    a strike only with no objective in reach. Prompt "Shut down pylon" / "Shut down device" only in reach.
  - Counters count only what is required (Viper: "Pylons n / 2"; the third emitter belongs to the platform and dies
    with it; the energy sweep only while he is up there). She starts the fight in the plaza's middle (`HerStart`).
  - `MarsPylon`: power beam to his platform while on; shutdown = bang + flash + sparks + shock ring, beam dies, panels
    blow open; `BossFight.NudgeCamera` turns the camera toward it.
  - **Proof rule for any X objective:** the pilot (`BossPilot`) walks up until the prompt shows and presses the pad
    **once**; `X OBJECTIVES (QF19)` in the boss summary: second presses 0, pad presses that went to a strike 0. Never prove
    an objective by mashing or by calling its code.
- **QF20 (release 0.15.5)** — the Biaveh discovery can't be missed + kicks + bosses keep their damage (status + "For Dan to
  check (QF20)" in `TODO.md`):
  - `WorldFlags.CombinedSeen` (set only at the END of MAR_13C + MAR_13B; the old `combined` flag was set halfway and shared by
    every save path); due = Mars + Europa complete and not seen. Whenever due, X at the console plays it in the pilot's chair
    (`ShipConsole.Use` → `Chapter1Aboard.CombinedAtConsole`), then the journey + TRV_AST; also at the second debrief.
    Proof `-qf20check` (`QF20Check.cs`: Dan's save rebuilt on the test keys, one real X press) → `QF20 DISCOVERY CHECK: PASS`.
  - Boss fights: **Y = kick** (`ChapterSevenFight.PickBlow`; takedown only when dazed), X = punches; build check
    `KICK STATES CHECK (QF20 2)` on every chapter scene; Controls page "Fight:" rows; the fairness skip = View / Enter.
  - `BossFight.Retry` keeps his energy / objectives / stuns. `ChapterInput.LockOn` is enabled now (it never was).
- **Round 16 — The Asteroid (release 0.16.0)** (spec `Docs/CHAPTER_ASTEROID.md` incl. Dan's Decisions + the chat's proposed
  lines; status, decisions made, "For Dan to check" in `TODO.md`):
  - Scene `Asteroid`: `Editor/FightDemoBuilderAsteroid.cs` (setup, the Machinist, wiring) + `...AsteroidLevel.cs` (the kit:
    `Riveted`, `PlateWall`, `Gear`, `AstLamp`, `LightRun`; the receiving chamber) + `...AsteroidCavern.cs` (tunnel, cavern
    shell, causeway + junction spans, terraces, rail carriers, flywheels, workstations, the bird) + `...AsteroidHall.cs`
    (the hall, the temple archive, the factory area, the security machines); numbers + zones + checkpoints
    `Runtime/AsteroidLayout.cs`; `Atmosphere_Asteroid`. Machinery at runtime `Runtime/AsteroidMachines.cs` (`AstProduction`
    = preservation mode, `AstSpin`, `AstPiston`, `AstRailCarrier`, `AstArchiveLine`, `AstTurntable`, `AstScanner`,
    `AstMaintBot`, `AstSecurity`, `AstBird`, `AstShutter`, `AstClamp`). Rune = `Cuboctahedron` (code).
  - Flow `AsteroidDirector.cs` (helpers as Mars + security machines + `BladeCut`) + `AsteroidDirectorFlow.cs` (AST_02-04)
    + `AsteroidDirectorHall.cs` (AST_05-09 + home). Saves `AsteroidState.cs` (`seven_asteroid`, select `seven_asteroid_select`;
    the blades are kept on the player's own save). Aboard: `Chapter1AboardAsteroid.cs` (AST_01, AST_10 reunion straight from
    the beam-up, the chair talk, AST_11A / 11B; the Cattalus lead also at the console when due - `WorldFlags.CattalusDue`).
    TRV_AST "Investigate the asteroid" and the star map's asteroid start the chapter; `ChapterList` "The Asteroid".
  - **7's arc blades** `ChapterSevenBlades.cs` (partial of ChapterSeven): RB / G draw / sheathe (`ChapterInput.Blades`),
    X cuts while drawn (on foot: `BladeSwing`; in a boss fight the blade chain, `BossDef.bladeDamage`); her Mixamo clips
    `Art/Mixamo/7/blade/` (+ mirrored slashes); visuals built on her hand bones at runtime; they only stun the living.
  - **The Machinist**: Dan's Mixamo rig via `SetupMixamoFolk("machinist")` (`Machinist.controller`); `MachinistExtras`
    re-skins the front plate onto a `PlateHinge` bone (`MachinistRig` swings it with the thighs), the visor slit glows with
    his voice, power couplings flare on blade hits, two tool assemblies taken into the service-station clamps;
    `MachinistFight.cs` (boss "machinist" in `boss_tuning.json`).
  - Lines: `python Tools/Voice/make_asteroid_lines.py` (every `| AST_.. |` row of the spec) → `Resources/Asteroid/asteroid_dialogue.json`
    + `Docs/ASTEROID-LINES.md`; voices `python Tools/Voice/generate_asteroid_voices.py` (Machinist = Dan's saved voice, System =
    premade) → `Resources/AsteroidVoice/`. SFX `ast_*` in `Tools/Audio/generate_sfx.py`.
  - Switches: `-astcp <n>` (check run at checkpoint n; 0 = AST_01 aboard, 9 = home), `-asteroid` (as from the star map),
    `-astplay` (player-style run `AsteroidPlaytest.cs`: free aboard → console → Biaveh → TRV_AST → the chapter → AST_11; with
    `-astcp n` it starts there - the boss runs), `-astbird` (a check run takes the optional bird).
- **QF21 (release 0.16.1)** — Dan's Asteroid review (status + "For Dan to check (QF21)" in `TODO.md`; standing rules "Cameras,
  prompts and the bike" at the top of this file):
  - Shots: `ChapterCameraShots.cs` (`ChapterCamera.ShotFollow`, automatic follow of a walker, `EMPTY SHOTS (QF21 6)`);
    `AsteroidDirector.TrackWalk` (`TRACKING (QF21 6)`); `DialogueFraming.Coverage` + the big-body rule; blocked lines log why.
    Moving parts (`AstSpin`, rail-carrier cars) are excluded from `CameraOccluders` (frozen copies blocked lines of sight).
  - Machinist: `MachinistRig.Calm` / `Restrained` / `Twitch` / heavy steps (`machinist_step_1..4`, `machinist_hiss`,
    `machinist_servo`; `MACHINIST STEPS (QF21 5)`), `AfterPose`; his framings `HerFromHisSide` / `WideTwoShot` / `MachinistUp`
    (`AsteroidDirectorQF21.cs`; `MACHINIST FRAMING (QF21 7)`); the cradle `AstClamp.OpenUp`; `CLAMPS (QF21 11b)`.
  - AST_01: `ShipReveal.CenterOverride` / `KeepSolid` / `ClearKeep` (the pilot's-chair effect from another seat).
  - System = `AstMaintBot` (legs, iris, grille, fan, stack whistle `ast_steam_whistle`, wall horns, `Tilt`).
  - Wrong state: `AsteroidDirector.Interact(... bikeOnly, bikeLine)`; AST_02_P5 / AST_03_P3 (new Panther lines).
  - Blades: `ChapterSevenBlades` holds them out (arms out, angle per frame vs her bone capsules, the light pulls in rather than
    touch her); `BLADE vs BODY (QF21 9)`; check `-astcp 5 -qf21blades` (idle / walk / run, three angles).
  - The hall: `FightDemoBuilderAsteroidFoundry.cs` (`ClampRig`, `BeltConveyor`, `FlywheelCasing`, `BoilerDrum`, `CrateStack`,
    `CoolingTower`, `Overhead`, `HallDebrisMeshes`), obstacles `AsteroidLayout.HallProps`. The memory screen = a UV'd
    `Seven/GalleryScreen` quad (`MemoryPhase` syncs the video to AST_09; `MEMORY SURFACE (QF21 11a)`).
  - Switches: `-noqf21fix` (no automatic shot follow), `-qf21blades`.
History in `TODO-ARCHIVE.md`. **Next: Dan plays 0.16.2** (QF21 + QF22 status in `TODO.md`).
**Before: Dan plays 0.16.0** (Round 16 status in `TODO.md`).
**Before: Dan plays 0.15.5** (QF20 status in `TODO.md`).
**Before: Dan plays 0.15.4** (QF19 status in `TODO.md`).
**Before: Dan plays 0.15.3** (QF18 status in `TODO.md`).
**Before: Dan plays 0.15.2** (QF17 status in `TODO.md`).
**Before: Dan plays 0.15.1** (QF16 status in `TODO.md`).
**Before: Dan plays 0.15.0** (Round 15 status in `TODO.md`).
**Before: Dan plays 0.14.3** (QF15 status in `TODO.md`).
**Before: Dan plays 0.14.2** (QF14 status in `TODO.md`).
**Before: Dan plays 0.14.1** (QF13 status in `TODO.md`).
**Before: Dan plays 0.14.0** (QF12 + Round 14 status in `TODO.md`).
**Before: Dan plays 0.13.1** (Round 13 status in `TODO.md`), then Round 14.
**Before: Dan plays 0.12.0** (Round 12 status in `TODO.md`), then Round 13.
**Before:** Dan plays 0.10.2, then Round 11
(`Docs/ROUND11.md`: Venus / the Elarim; open: Kyle's Elarim marking, first-play length). See `TODO.md`.
