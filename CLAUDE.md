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
**Rounds 1–14 + QF1–QF14 complete (2026-11-19), release 0.14.2.** Round 14 = Chapter "Europa — Beneath the Ice" (first playable pass). Round 11 = Chapter 2 "Venus", part 1 (to VEN_S04); Round 12 = part 2 (to VEN_S08 + destinations). Chapter 1 "The Moon Prison" ends with the galley
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
History in `TODO-ARCHIVE.md`. **Next: Dan plays 0.14.2** (QF14 status in `TODO.md`).
**Before: Dan plays 0.14.1** (QF13 status in `TODO.md`).
**Before: Dan plays 0.14.0** (QF12 + Round 14 status in `TODO.md`).
**Before: Dan plays 0.13.1** (Round 13 status in `TODO.md`), then Round 14.
**Before: Dan plays 0.12.0** (Round 12 status in `TODO.md`), then Round 13.
**Before:** Dan plays 0.10.2, then Round 11
(`Docs/ROUND11.md`: Venus / the Elarim; open: Kyle's Elarim marking, first-play length). See `TODO.md`.
