# TODO archive — 7: The Observer

Older rounds, moved word-for-word from TODO.md. Grep it; do not read it in full.

---
# Archived 2026-11-16: Round 1

## (was Current Objective) — Round 1: fight-demo proof of concept
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

- **2c. Forehead mark made flat (Dan, 2026-11-16) — done.** The model had a raised disc on the forehead (3.0 mm proud,
  r 5.6 mm in source units). `cleanup_7.py` presses it onto a surface fitted to the surrounding forehead (now ≤0.16 mm)
  and lays a flat conforming dot over it (r 5.9 mm, 280 tris, 0.3 mm off the skin) with its own material
  `7_forehead_glow` (white emissive). FBX now has **three** slots: `7_body` / `7_hairtie_metal` / `7_forehead_glow`;
  49,999 tris (body 46,647 + cuffs 3,072 + dot 280). Check: `7_v2_forehead.png` (flat, no leftover rim) and
  `7_v2_forehead_side.png` (no bump in profile).

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

### Stage B — STATUS 2026-11-16: done (round 1 complete, waiting for Dan to play it)
Mixamo files: Dan's downloads renamed to the list above. Substitutes: 7_run = Fast Run, 7_jump = Jumping,
7_fight_idle = Bouncing Fight Idle, 7_cross = **Jab Cross** (a 1-2 combo, lands two hits), 7_finisher = Flying Kick,
7_telekinesis = Standing 2H Magic Attack 01, ybot_hit_body = Big Stomach Hit, **ybot_knocked_down = Stunned** (Knocked
Down only existed holding a gun; Stunned falls to the floor: hips 0.90 → 0.15 m, and Getting Up starts at 0.12 m).
Everything is built by one headless command (`FightDemoBuilder.BuildAllFromCommandLine`, see CLAUDE.md).
- **5. Import — done.** FBX copied into `Unity/Assets/Art/Characters/{7,YBot}`; both Humanoid (avatars valid),
  clips retargeted via Copy From Other avatar; idle/walk/run/fight_idle/dazed loop; rotation + height baked into pose;
  horizontal root motion kept only for finisher (2.1 m leap), dodge (0.5 m back), knocked_down (1 m back), get_up.
  Materials remapped by name: `7_body` (URP Lit, grayscale copy of the texture), `7_hairtie_metal` (light gray,
  metallic 0.7, smoothness 0.86 — full metallic 1.0 reflected the room as near-black, so lowered), `7_forehead_glow`
  (white emission ×4 → Bloom), Y Bot light/dark gray matte. Pre-Mixamo FBX kept as `Characters/7/Fallback/7_v2_premixamo.fbx`.
  Hit timings measured from limb-extension peaks in Blender (jab 0.28, cross 0.20+0.33, hook 0.33, roundhouse 0.43,
  mma kick 0.40, finisher 0.29, telekinesis 0.49, bot punch 0.40).
  **LODs still deferred:** two characters in one small room need none; build them when a scene has crowds.
- **6. Arena — done.** `Assets/Scenes/FightDemo.unity`: gray floor + darker sparring mat, gray walls, pillars, white
  emissive light strips, exp² fog, one directional key light with soft shadows, flat ambient, realtime reflection probe,
  global volume (Bloom, Neutral tonemap, vignette, **saturation −100** as a monochrome guarantee). Fixed 3/4 camera
  (pitch 9°, yaw 18°) framing both fighters, distance grows with their separation, SmoothDamp follow.
- **7. Fight controller — done.** `Scripts/Runtime/`: `SevenFighter`, `TrainingBot`, `FighterBase`, `FightInput`
  (Input System, keyboard + gamepad), `FightCamera`, `FightDirector` (legend + reset), `DemoBoot` (-mute / -capture).
  Keys as specified; gamepad: X jab, Y jab-cross, RB hook, B roundhouse, A mma kick, LT block, LB dodge, RT finisher,
  d-pad up telekinesis, Start reset, left stick move. Hits play hit_head/hit_body + knockback; bot punches every
  3–4.5 s at 0.6 speed when within 1.7 m; blocked/dodged punch → bot staggers back; unblocked → 7 is pushed back
  0.35 m (no damage). Finisher → Knocked Down → Getting Up → Dazed 2 s → Fighting Idle. No health, no death.
  Legend = white text on a translucent black box, bottom-left.
- **8. Checks — done.** Exe `Unity/Builds/FightDemo/SevenFightDemo.exe` (125 MB, not in git). Check run started
  minimized/not activated, Below Normal, `-mute -capture`: whole sequence passed with no timeouts. Results in
  `Docs/Previews/round1/` (14 shots + `capture_log.txt`). **Audio: listener volume 0, output RMS 0.000000** (start and
  end). **Monochrome: max R/G/B difference in any pixel of any shot = 0.** **Feet: lowest mesh point 0.001–0.002 m**
  above the floor (idle, after finisher, end). Forehead dot glows white; cuffs read as metal with highlights.
  Not verifiable off-screen: the key legend (IMGUI draws to the window, not the capture texture) — seen only in code.
- **9. Wrap-up — done.** This file, CLAUDE.md, commit + push.

#### Known limitations (Round 1)
- Walking sideways/backwards while facing the bot uses the forward walk clip (no strafe animations downloaded).
- Jump and run clips are imported but unused (no key for them in the spec).
- The bot never walks; it stays where hits leave it and turns to face 7.
- Cuffs are mid-silver, darker than the reference's bright chrome; brighter room or a stronger probe would help.

### Stage B — original spec
5. **Import.** Both characters as **Humanoid** rigs; animations retargeted. Materials: 7 body = URP Lit with the
   base-color texture (grayscale); hair-tie cuffs = URP Lit metallic 1 / smoothness ~0.9, light gray, with a
   reflection probe so they read as chrome (no glow — changed from orbs 2026-11-16); forehead dot (`7_forehead_glow`)
   = white emissive, soft white glow via URP Bloom (no color);
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
- Play `Unity\Builds\FightDemo\SevenFightDemo.exe` and try every key. Does the fighting read clearly? Do the hair-tie
  cuffs and pigtails look right in motion? Is the room bright enough?
- Check shots: `Docs/Previews/round1/01_idle.png` … `14_head_back.png` (punch 03, block 04, knockdown 08, get-up 09).


