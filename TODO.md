# Active Development Plan — 7: The Observer

## Standing rules
- 7 never kills · strictly monochrome · non-sexual presentation · 7 is clearly an adult (details in `CLAUDE.md`).
- Per-item status at the end of every round: **done / not done / deferred (why)**. No silent partial completion.
- Short results: ~20 lines, one line per item + its one check, plus a "For Dan to check" list.

## Completed Tasks
- **7 base model v2 — done in chat (2026-11-16).** Tripo model `7_normal_v1.glb` → `Art/Source/7_normal_v2.glb`:
  pigtail texture darkened (×0.57) to match scalp hair; band hair ties covered by a separate mesh `7_hairtie_orbs`
  (two spheres r=0.017 at x=±0.042, y=0.960, z=-0.039 in source units) with white emissive material `7_hairtie_glow`.
  Script kept as `Tools/Model/fix_v1_to_v2.py`. Source facts: 0.98 m tall, ~1.95M triangles, one 4K base-color texture.
  Verified only with rough point-cloud previews, not in Blender or Unity yet.

## Current Objective — Round 1: fight-demo proof of concept
Goal: a short playable scene where 7 spars with a humanoid training bot (Mixamo **Y Bot**) using punches, kicks,
a block and a finisher; the bot gets knocked down, gets back up dazed. Nothing dies.

### Stage A — STATUS 2026-11-16: done, STOPPED for Dan
- **1. Repo setup — done.** Git LFS (`.gitattributes`: glb/fbx/png/jpg/psd/wav) before the first commit; checkpoint
  commit pushed with the GLB as an LFS object. Unity `.gitignore`, folder layout, Unity 6000.6.1f1 project in `Unity/`
  from the URP Blank template (URP 17.6.0, Input System 1.20.0, `activeInputHandler: 1` = Input System only); template
  tutorial/readme assets removed. Check: `git lfs ls-files` lists the GLB; Unity batch create exited 0.
- **2. Blender cleanup — done.** `Tools/Blender/cleanup_7.py` → `Art/Export/7_v2_mixamo.fbx` (5.4 MB, texture embedded)
  + `Art/Export/Textures/7_v2_basecolor.jpg` (original 4K JPEG bytes, not re-encoded). **49,999 tris** (body 47,439 +
  hair ties 3,072, left undecimated so they stay round); one object `7_v2`, two material slots `7_body` /
  `7_hairtie_metal`; 1.700 m tall, origin at feet centre, transforms applied, faces −Y in Blender (Mixamo front).
  Seam duplicates are welded before decimating: a no-weld test opened hairline cracks across the face.
  Check: re-imported FBX reports 49,999 tris, bbox z 0.000–1.700, both slots, 4096² image.
  LODs (CLAUDE.md pipeline) **deferred** to Stage B: they must come from the rigged mesh, so build them in Unity after Mixamo.
- **3. Previews — done.** `Tools/Blender/preview_7.py` (Workbench, flat light, grayscale, gray background) →
  `Docs/Previews/7_v2_{front,back,side,face,head_top,head_back}.png`, all looked at. Hair ties present on both pigtails;
  pigtails same tone as scalp hair (a few lighter streaks inside the left pigtail); no decimation holes seen; feet on
  the floor; face and hands read fine. Known: soft light/dark smudges on the chin and on top of the shoulders appear in
  both weld and no-weld builds, so they are most likely in the Tripo texture, not caused by decimation.
- **4. STOP — done.** Waiting for Dan's Mixamo files. Audio: no game/player run happened in Stage A (Blender + Unity
  batch project creation only), so no audio level to measure; the muted-run proof starts with the first run in Stage B.

- **2b. Hair ties changed to metal cuffs (Dan, 2026-11-16) — done.** Per `Art/Reference/7_ref_hairties_back.jpg` the
  glowing orbs are replaced by a thick polished-silver cuff around each pigtail base, fitted over the model's original
  band ties (torus R 0.0165, tube 0.003 stretched ×2.4 into a band, source units; constants at the top of
  `cleanup_7.py`). Material `7_hairtie_metal` (metallic 1, roughness 0.12, light gray, **no glow**). The old orbs remain
  as a fallback: 5th script arg `orb`. Check: re-imported FBX has 49,999 tris (body 46,927 + cuffs 3,072), slots
  `7_body` / `7_hairtie_metal`; `head_top` / `head_back` previews show both cuffs seated on the pigtails. Workbench
  can't show chrome reflections, so those two close-ups show the cuffs as bright satin silver (preview-only display).

#### For Dan to check (Stage A)
- `Docs/Previews/7_v2_front.png`, `7_v2_back.png`, `7_v2_side.png`, `7_v2_face.png`, `7_v2_head_top.png`,
  `7_v2_head_back.png` — especially the cuffs vs `Art/Reference/7_ref_hairties_back.jpg` (size/thickness are tweakable).
- **DECIDED (Dan, 2026-11-16): go with this model for now** — metal cuffs accepted as built, and the current outfit
  (bare-thigh band between shorts and over-knee boots) accepted for Round 1. Revisit the outfit in a later model pass
  (fixing it then means re-rigging and re-downloading the Mixamo animations).
- The model is in an A-pose (arms ~45°); Mixamo auto-rig accepts that.

### Stage A — Code (before Dan) — original spec
1. **Repo setup.** Before the first commit, set up Git LFS (`git lfs install`, `.gitattributes` for
   glb/fbx/png/jpg/psd/wav) so the 59 MB GLB never lands in plain git history; then commit the repo as-is
   (safety checkpoint) and add a Unity `.gitignore`. Create the folder layout from `CLAUDE.md`. Create the Unity
   6000.6.1f1 URP project in `Unity/` with the Input System package as the only input backend.
2. **Blender cleanup script** `Tools/Blender/cleanup_7.py` (run headless), input `Art/Source/7_normal_v2.glb`:
   - join `7_hairtie_orbs` into the body object but **keep two material slots** (body texture / hair-tie glow) so Unity
     can assign the emissive material to the orbs;
   - decimate to **~50k triangles** total (check the face, hands and orbs still look right; note the final count);
   - scale to **1.70 m** tall, apply transforms, origin at the feet (floor center), facing correct for Mixamo/Unity;
   - export `Art/Export/7_v2_mixamo.fbx` (FBX, Path Mode Copy + embed textures) and the base-color texture to
     `Art/Export/Textures/`.
3. **Previews.** Render front / back / side / face close-up of the exported FBX to `Docs/Previews/7_v2_*.png`
   (gray background, flat light, grayscale) and look at them yourself. Check: orbs present on both pigtails,
   pigtails same tone as scalp hair, no holes from decimation, feet on the floor.
4. **STOP.** Report to Dan with the preview paths and the Mixamo checklist below. Do not start Stage B until Dan
   says the Mixamo files are in place.

### For Dan — Mixamo (between Stage A and Stage B)
Upload `Art/Export/7_v2_mixamo.fbx` to Mixamo, auto-rig (chin, wrists, elbows, knees, groin). Downloads: **FBX for
Unity, 30 fps, keyframe reduction none.** Character files **with skin, T-pose**; animations **without skin**.
Pick the closest match if a name differs slightly; keep the file names below.

7 → `Art/Mixamo/7/`
- `7_character.fbx` (with skin)
- `7_idle.fbx` — Idle · `7_walk.fbx` — Walking (In Place) · `7_run.fbx` — Running (In Place) · `7_jump.fbx` — Jump
- `7_fight_idle.fbx` — Fighting Idle
- `7_jab.fbx` — Lead Jab · `7_cross.fbx` — Cross Punch · `7_hook.fbx` — Hook Punch
- `7_roundhouse.fbx` — Roundhouse Kick · `7_mma_kick.fbx` — Mma Kick
- `7_block.fbx` — Center Block · `7_dodge.fbx` — Dodging
- `7_finisher.fbx` — Hurricane Kick (or Flying Kick)
- `7_telekinesis.fbx` — Standing Magic Attack (stand-in for telekinesis)

Y Bot → `Art/Mixamo/YBot/`
- `ybot_character.fbx` (Y Bot, with skin)
- `ybot_fight_idle.fbx` — Fighting Idle
- `ybot_hit_head.fbx` — Head Hit · `ybot_hit_body.fbx` — Stomach Hit (or Hit Reaction)
- `ybot_knocked_down.fbx` — Knocked Down · `ybot_get_up.fbx` — Getting Up · `ybot_dazed.fbx` — Dizzy Idle (or Stunned)
- `ybot_punch.fbx` — Punching (so the bot can throw a slow attack for 7 to block/dodge)

### Stage B — Code (after Dan's Mixamo files are in place)
5. **Import.** Both characters as **Humanoid** rigs; animations retargeted. Materials: 7 body = URP Lit with the
   base-color texture (grayscale); hair-tie cuffs = URP Lit metallic 1 / smoothness ~0.9, light gray, with a
   reflection probe so they read as chrome (no glow — changed from orbs 2026-11-16);
   Y Bot = neutral light-gray matte. Keep the pre-Mixamo FBX as a fallback asset.
6. **Arena scene** `Unity/Assets/Scenes/FightDemo.unity`: plain monochrome training room (gray floor, soft fog,
   one key light), fixed 3/4 side camera framing both fighters full-body, gentle follow.
7. **Fight controller** (Input System, keyboard + gamepad):
   - 1/2/3 = jab / cross / hook · 4/5 = roundhouse / mma kick · Space = block · Shift = dodge · F = finisher ·
     T = telekinesis push (bot slides back a short distance) · WASD/left stick = move · Esc = reset scene.
   - Each 7 attack that connects plays the matching Y Bot hit reaction (head vs body) and a small knockback.
   - Every few seconds the bot throws a slow punch; if 7 blocks/dodges in time the bot staggers.
   - **Finisher → bot Knocked Down → Getting Up → Dazed idle for 2 s → Fighting Idle.** The bot always recovers.
     No health bars, no death states.
   - A small on-screen key legend (white text).
8. **Checks** (all muted, no focus stealing): build a Windows exe; capture off-screen screenshots of idle, a punch
   connecting, the block, the knockdown and the get-up; confirm the cuffs read as shiny metal, everything is monochrome, and
   7's feet stay on the floor. Report audio level = 0.
9. End of round: update `TODO.md` (per-item status, "For Dan to check") and `CLAUDE.md` (commands, where things
   stand), commit and push.

### For Dan to check (after Stage B)
- Play `FightDemo` and try every key. Does the fighting read clearly? Do the hair-tie cuffs and pigtails look right in motion?

## Next Steps (Do Not Start Yet)
- **Launcher / auto-update** (copy from `C:\Users\danmo\chastain_rush`) and releases repo — deferred until there is
  more than a demo. OPEN for Dan: publish every round once it exists? Default: yes.
- **Bubble helmet, hover bike, saucer, cat companion models** — deferred; need concept images and prompts first.
- **Big-head proportion variant** — DECIDED: not pursued for now (Dan prefers normal proportions).

## Technical Notes / Blockers
- `7_normal_v2.glb` is ~59 MB; it must go through Git LFS.
- Mixamo strips materials; reassign materials in Unity (Stage B item 5).
- FBX does not carry PBR metallic/smoothness reliably — set the cuff material up in Unity (Stage B item 5).
