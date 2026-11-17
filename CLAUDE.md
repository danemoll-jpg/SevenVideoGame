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

## Commands
- Blender 3.6.1: `F:\blender\blender.exe` (not under Program Files, not on PATH).
  Unity 6000.6.1f1: `C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe`.
- Clean up 7 (from repo root):
  `F:\blender\blender.exe --background --factory-startup --python Tools/Blender/cleanup_7.py -- C:/Users/danmo/SevenVideoGame 50000`
  (args: repo root, target tris, weld 1|0 (default 1), optional output FBX path for experiments).
- Cat (Round 3): `F:\blender\blender.exe --background --factory-startup --python Tools/Blender/cleanup_cat.py -- C:/Users/danmo/SevenVideoGame 18000 [preview_dir]`
  → `Art/Export/cat/cat_v1_rigged.fbx` (+ grayscale texture): squares the body, straightens the head, decimates,
  0.25 m shoulder, scripted 24-bone rig + clips idle/walk/sit/wash/jump/sleep. Must run before the Unity build.
- Ratwing (Round 4): `F:\blender\blender.exe --background --factory-startup --python Tools/Blender/cleanup_ratwing.py -- C:/Users/danmo/SevenVideoGame 55000 [preview_dir]`
  (~2 min) → `Art/Export/ratwing/ratwing_v1_rigged.fbx` (+ grayscale texture, `ratwing_metrics.txt`): 2.1 m (`HEIGHT`),
  decimate with the membranes/claws protected, scripted 38-bone rig (body + wing root/upper + 3 two-segment finger
  spars per wing, fan weights on the membranes, jaw, eye-glint bone) and the clips `sequence` (10 s) + `idle`.
  Tuning knobs at the top: `HEIGHT`, `CEIL` (ledge height, = `RatwingCeil` in `FightDemoBuilderRatwing.cs`), `LAND_Z`;
  poses `P_SLEEP … P_STANCE` and the timeline in `seq_pose`. `RW_STAGE=fit` / `RW_STAGE=poses` env = debug renders only.
  Must run before the Unity build.
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
  paws vs. the floor, lap gap, reveal timings), ~70 s. **Ratwing check run:** same with `-ratwing` →
  `01_cave_wide.png … 11_after_skip.png` + `capture_log.txt` (audio, eye-glint timing, feet vs. ledge while hanging and
  vs. floor after landing incl. raycasts, wingspan, replay/skip), ~25 s. **Gallery check run:** same with `-gallery`
  (implies `-saucer`) → `G01_tv_across_deck.png … G21_left.png` + `capture_log.txt` (image count vs. JSON vs. files,
  idle timing, next/prev/wrap, aspect ratio of every image measured on the screen and in full view, a real-input test of
  every key/pad binding through virtual devices, audio), ~85 s. **Menu shot:** `-capture <dir> -menu` → `00_menu.png`.
  Monochrome check: max |R−G|,|G−B| over every pixel must be 0 (PIL).
- **Chapter 1 (Round 6)**: scene `Chapter1` (Moon level, built by `FightDemoBuilderChapter1.cs`; layout numbers in
  `Runtime/MoonLayout.cs`; `Atmosphere_Moon` prefab; walkable areas = `Ground.Shape.Zones`, rectangles that must
  overlap ≥ 0.6 m) → scenes 9–11 run in the `Saucer` scene (`Chapter1Aboard`, galley nook in `FightDemoBuilderSaucer.cs`).
  Flow + checkpoints: `Chapter1Director.cs`. Switches: `-chapter1`, `-ch1aboard` (start at scene 9).
  **Chapter 1 check run:** `SevenDemo.exe -mute -chapter1 -capture <dir> …` (autopilot, ~4.5 min) → `01_s01_cell_start.png …
  28_s11_to_be_continued.png` + `capture_log.txt` (objectives, checkpoints, any "SOFT-LOCK?" timeouts with positions, voice
  lines played vs. bank, cat distance, audio peak). **Telepathy sound check** (the one allowed unmuted step):
  `SevenDemo.exe -mute -soundcheck -capture <dir> …` → `soundcheck_log.txt` (~2 s of sound, then muted again).
  **SFX sound check** (Round 7): `-mute -soundcheck sfx -capture <dir> …` → 5 SFX, ≤ 1.2 s each (~5.4 s of sound), muted again.
  Round 7 switches: `-ch1cp <n>` (start Chapter 1 at checkpoint n; 4 = the city reveal onward, ~3 min check run),
  `-continue` (like the menu's Continue: explore the saucer after the chapter), `-cityreveal primary|alternate|off`.
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
- Play: run the exe normally → main menu (Continue when a Chapter 1 save exists · Chapter 1 · demos · Quit); Xbox
  controller or keyboard/mouse (the legend bottom-left switches automatically). Esc / Start = pause menu (Resume ·
  Settings · Main menu · Quit, with confirms); fight reset / checkpoint restart = Backspace / d-pad down.

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

## Where things stand
**Rounds 1–8 complete (2026-11-17)** — `Unity\Builds\Seven\SevenDemo.exe` 0.8.0, published through the launcher
(`danemoll-jpg/seven-releases`, `game-800` + `starter-800`): Chapter 1 "The Moon Prison" (laser cell, city reveal,
tractor beam, free saucer exploration, SFX + music, pause menu, Continue) plus the demos (fluid fight, saucer + gallery
TV, Ratwing). Check-run switch added in Round 8: `-fightcombo` (combo timing log). Details in `TODO.md` /
`TODO-ARCHIVE.md`. **Current objective: none** — waiting for Dan's review; every round ends with a published release.
