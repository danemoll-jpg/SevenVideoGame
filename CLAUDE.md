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

## Tech Stack
- Unity **6000.6.1f1**, **URP**, **Input System only** (same as Woodstock Rush / Chastain Rush, so systems can be ported).
- Blender for model cleanup, scripted where possible (`Tools/Blender/*.py`). Find Blender on Dan's PC; record the path under Commands.
- Tripo / Meshy for generated models (Meshy key only via the `MESHY_API_KEY` env var — never write it to the repo, logs or chat).
- Mixamo for rigging and animation (Dan does the uploads/downloads; Code lists exact animations).
- Git remote: `https://github.com/danemoll-jpg/SevenVideoGame.git`, branch `main`.
- Large binaries (`*.glb *.fbx *.psd *.png *.jpg *.wav`) go through **Git LFS** (`.gitattributes`).

## Folder layout
- `Art/Source/` — raw generated models (e.g. `7_normal_v2.glb`). Never edit in place; cleanup writes to `Art/Export/`.
- `Art/Reference/` — approved concept/reference images.
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
- Previews: `F:\blender\blender.exe --background --factory-startup --python Tools/Blender/preview_7.py -- C:/Users/danmo/SevenVideoGame`
  (args: repo root, optional FBX, optional output dir).
- Launch Blender/Unity jobs with `Start-Process ... -WindowStyle Hidden -PassThru`, then `$p.PriorityClass = 'BelowNormal'`;
  one at a time.
- Unity project was created with: `Unity.exe -batchmode -nographics -quit -createProject C:\Users\danmo\SevenVideoGame\Unity
  -cloneFromTemplate "<Editor>\Data\Resources\PackageManager\ProjectTemplates\com.unity.template.urp-blank-17.2.1.tgz" -logFile <file>`.
- Unity batch build / screenshot commands: to be added in Stage B.

## Where things stand
**Round 1 Stage A done (2026-11-16), stopped for Dan.** Repo on Git LFS; Unity 6000.6.1f1 URP project in `Unity/`
(Input System only). `Art/Export/7_v2_mixamo.fbx` is the cleaned 7 (49,999 tris, 1.70 m, origin at feet, two material
slots `7_body` / `7_hairtie_metal`; hair ties are now polished metal cuffs, not glowing orbs), previews in `Docs/Previews/7_v2_*.png`. Next: Dan uploads to Mixamo and drops the
files listed in `TODO.md`; then Stage B (import, arena, fight controller, checks). Open question for Dan: bare-thigh
gap between shorts and boots vs the full-coverage rule.
