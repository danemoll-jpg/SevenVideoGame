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


---
# Archived 2026-11-16: Round 2 + Quick fix QF1

## (was Current Objective) — Round 2: orbit camera, telekinesis showcase, foggy outdoor world

### Quick fix QF1 (Dan, 2026-11-16) — demo opens fullscreen
1. The demo starts **fullscreen (borderless, the monitor's native resolution)** on a normal launch, even if an
   earlier test run left Unity's saved setting at windowed (force it at startup when no `-screen-*` flag is given).
   Player Settings default: Fullscreen Window, native resolution.
2. **F11** and the **Xbox View button** toggle fullscreen ↔ windowed (Alt+Enter keeps working). Add it to both legends.
3. Check runs stay windowed/minimized: they pass `-screen-fullscreen 0` explicitly and `-capture` must never force
   fullscreen. Never steal focus while testing.
4. Also commit Dan's new reference images (`Art/Reference/kyle_*.jpg`, `Art/Reference/README.md`) and the CLAUDE.md
   pointer to the index. Rebuild the exe, update this section with per-item status, commit and push.
**QF1 status 2026-11-16 (exe rebuilt, check run muted/minimized):**
- **1. Fullscreen on normal launch — done (code + settings; not launched fullscreen here, that would take focus — Dan to
  confirm).** `FullscreenToggle` (installed by `DemoBoot` before the first scene) forces borderless `FullScreenWindow`
  at the display's native resolution whenever there is no `-capture` and no `-screen-*` / `-window-mode` /
  `-popupwindow` flag, overriding any saved windowed setting. Player Settings: Fullscreen Window, native resolution.
- **2. F11 / Xbox View toggle — done.** F11 or View (`selectButton`) switches fullscreen ↔ windowed 1600×900 (80% of
  the screen if smaller); Alt+Enter left on (`allowFullscreenSwitch`). Row "fullscreen on/off" added to both legends
  (new View-button glyph / F11 key): `Docs/Previews/qf1/legends_fullscreen_row.png`. Toggle not pressed here (focus rule).
- **3. Check runs stay windowed — done, verified.** Check run with `-screen-fullscreen 0 -screen-width 1280
  -screen-height 720`: Player.log `forceFullscreen=False mode=Windowed`, captures 1280×720, started minimized at Below
  Normal, no focus taken; audio listenerVolume 0, output RMS 0.000000; monochrome max |R−G|,|G−B| = 0. The toggle
  ignores input during `-capture` runs.
- **4. Reference images + index committed — done.**
Goal: the same fight demo, but in an outdoor world with the fog and glow of Kyle's concept art
(`Art/Reference/kyle_world_mood.jpg`), a camera the player orbits with the right stick, and a telekinesis move that
reads clearly as a power.
Commit the repo as it is before changing anything (safety checkpoint).

### STATUS 2026-11-16: built and exe-verified off-screen — waiting for Dan to play
Safety checkpoint committed + pushed first (`a4d91b4`). Optional clips `7_tk_hold.fbx` / `ybot_float.fbx` were **not**
present, so the lift holds the peak pose of `7_telekinesis` and the bot hangs in a slowed `dazed` loop.
Checks below come from the final check run (`Docs/Previews/round2/capture_log.txt`).
- **1. Orbit camera — done.** `FightCamera`: right stick orbits (170°/s yaw, 95°/s pitch, 520°/s² ramp, stops 2× faster),
  RMB + drag, R3 / middle mouse / C re-centre to the 3/4 framing (smooth, 0.25 s). Pitch clamped −5…60° (asked 80 → 60).
  Auto-distance kept. Never below ground: camera pulls in if terrain blocks the view and stays ≥0.35 m above the ground
  (lowest measured 0.65 m at pitch −5). Movement is camera-relative (stick up at yaw 138° moved exactly along the camera's
  forward: dot = 1.000); 7 snaps to face the bot when attacking within range, from any angle.
- **2. Telekinesis showcase — done.** Tap T / d-pad up = push (unchanged). Hold = lift: hands glow and light up, forehead
  dot pulses (emission ×4 → up to ×14), motes + ripple rings travel to the bot, bot rises to 0.79–0.83 m with a slow bob,
  white aura + motes around it, 31 rocks/grass clumps near 7 float up and orbit. Release → bot pushed back (2.8 → 5.3 m,
  arena edge stops it), eased down (landed 0.95 s after release), knocked-down → get up → dazed → recovered. Auto-release
  at 4.01 s. Nothing is hurt.
- **3. Outdoor world — done (mood matched; for Dan to judge).** `FightDemo.unity` is now the hill world; the room is kept
  as `FightDemo_Room.unity` (exe `-room`). Dark grassy rise (140k short blades, 3% tilt, curving down at the edge), fog
  sea below, ring-terrace complexes with glowing lips and towers rising out of it on the right, a dark overhead arc
  sweeping from the top-left, glow sources inside the fog, heavy Bloom, saturation −100. Feet measured against the
  ground under each foot: toe bones −0.1…5 cm, lowest mesh point −1.3…0 cm (no floating, no sinking).
  Side-by-side with Kyle's art: `Docs/Previews/round2/side_by_side_01_wide.png` (+ `_01b_wide_level`). Honest read:
  same mood (dark silhouetted foreground, bright glowing fog bank, terraces/towers fading, dark arc on top). Weaker
  than the painting: less glow wrapping the fighters' legs, smoother fog (no wisps / light shafts), terraces crisper and
  a bit higher than Kyle's, simpler structure shapes. Next tuning steps if Dan wants closer: more ground fog near the
  fighters, light-shaft cards, softer terrace edges.
- **4. Reusable Atmosphere — done.** `Unity/Assets/Atmosphere/Atmosphere.prefab` + `Atmosphere_Volume.asset` +
  `SevenHeightFog` full-screen pass (one pass, no ray marching — Steam Deck-friendly, not yet profiled on a Deck).
  Knobs and recipe in `Docs/ATMOSPHERE.md`; `-atmofile <json>` tunes it without rebuilding. Recorded in `CLAUDE.md`.
- **4b. Xbox legend — done.** uGUI (Screen Space – Camera, so it shows in captures): monochrome glyphs (face buttons,
  LB/RB, LT/RT, d-pad up, sticks, R3, Menu) on a dark panel; keyboard/mouse legend when the last input was keys/mouse,
  Xbox by default. Captured both (`08_legend_xbox.png`, `09_legend_keys.png`); over the brightest fog the top rows
  are slightly soft from Bloom bleeding in. The automatic switch itself was only
  checked in code (captures force each mode) — Dan to try with a real pad.
- **5. Checks — done.** Exe rebuilt; shots listed below. Monochrome check max |R−G|,|G−B| = 0 on every shot; audio
  listener volume 0, output RMS 0.000000 (start and end); no focus taken (minimized, not activated, Below Normal). `-room` quick run: room scene still loads, monochrome 0.
- **6. End of round — done.** This file, `CLAUDE.md`, `Docs/ATMOSPHERE.md`; committed and pushed.
Found on the way: `WorldGround` first lived in `Ground.cs`, so the player dropped it ("script is missing") and the
exe crashed with "level0 is corrupted" — fixed by its own file (`CLAUDE.md` notes the symptom).

1. **Orbit camera (right stick).** Right stick orbits the camera around 7: full 360° yaw, pitch clamped to about
   −5°…60°, smooth with a little acceleration. Keyboard/mouse: hold right mouse button and drag. R3 (and middle
   mouse or C) re-centers to the current 3/4 framing. Keep the auto-distance so both fighters stay in view when
   close; the camera must not go below the ground or through terrain.
   **Movement becomes camera-relative** (left stick up = away from the camera). When the bot is within fight
   range, 7 turns to face it as she attacks, so moves still connect from any camera angle.
2. **Telekinesis showcase.** Keep the tap move (T / d-pad up = push). Add **hold T / hold d-pad up = lift**:
   - 7 raises her hands; a white glow builds around her hands and the forehead dot pulses brighter.
   - A visible effect travels to the bot (a white shimmer / ripple wave and drifting light motes — monochrome only).
   - The bot lifts about 0.6–1 m off the ground and hangs there while held (struggling or limp), wrapped in a faint
     white aura. Small rocks and grass debris near 7 float up and orbit slowly while she holds.
   - Release = the bot is pushed back several meters and **set down safely**, lands, gets up dazed, recovers.
     Max hold 4 s, then it releases on its own. Nothing is hurt or killed.
   - Use the existing clips (hold the peak pose of `7_telekinesis` for the lift). If Dan adds the optional clips
     below, use them instead.
3. **Outdoor world like the reference** (replace the training room; keep the room scene as `FightDemo_Room`):
   - 7 and the bot on a dark grassy rise in the foreground (short dark grass, gentle slope curving down at the
     edge), with a flat enough sparring area in the middle.
   - Beyond it, a vast bright fog bank. Huge distant structures — curved tiers, ring-like terraces, tall pillars /
     towers — fade into the fog, with soft glowing light sources inside it. A dark curved overhead mass (like the
     arc at the top of the reference) frames the top of the view.
   - Look: dark foreground silhouettes against a bright, glowing, misty distance; strong height fog near the
     ground; heavy Bloom; low contrast in the distance; strictly monochrome (keep saturation −100).
   - Fog approach is Code's choice (height-fog shader, layered fog cards/particles, light-shaft sprites, etc.),
     but the result must match the reference mood. Keep it reasonable to run on a Steam Deck later.
4. **Make the look reusable.** Build the atmosphere as a reusable setup (e.g. an `Atmosphere` prefab + volume
   profile + fog material/settings, with a few documented knobs) so every future world starts from it. Record it in
   `CLAUDE.md` (see the new "World look" rule).
4b. **Xbox controls on screen.** The on-screen legend shows Xbox controller buttons instead of keyboard keys:
   button glyphs (A / B / X / Y, LB / RB, LT / RT, d-pad, sticks) next to each move name, including the new
   right-stick orbit, R3 re-center and hold-to-lift telekinesis. Glyphs **monochrome** (white on a dark translucent
   panel — no green/red/blue/yellow button colors). If the last input came from keyboard/mouse, switch to the key
   legend automatically; default to Xbox. Rebuild the legend in uGUI or UI Toolkit (not IMGUI) so it appears in the
   off-screen captures and can be checked (Round 1 could only check it in code).
5. **Checks** (muted, off-screen, never take focus): rebuild the exe; capture shots of the camera at 4 orbit angles, the Xbox legend,
   the telekinesis lift (start, held, release, landing), a punch connecting, and the knockdown. Put one wide shot
   side by side with `kyle_world_mood.jpg` in `Docs/Previews/round2/` and look at both: does the fog/glow mood
   match? Monochrome pixel check = 0, audio level = 0, feet on the ground (uneven terrain: measure against the
   ground under each foot).
6. End of round: update `TODO.md` (per-item status, "For Dan to check") and `CLAUDE.md`, commit and push.

### Optional Mixamo downloads for Dan (Code works without them)
FBX for Unity, 30 fps, without skin; closest match if the name differs.
- `Art/Mixamo/7/7_tk_hold.fbx` — a looping spell-casting / magic hold pose with both hands forward
- `Art/Mixamo/YBot/ybot_float.fbx` — Falling Idle (for the bot hanging in the air)

### For Dan to check (after Round 2)
Run `Unity\Builds\FightDemo\SevenFightDemo.exe` (with the Xbox pad). Screenshots in `Docs/Previews/round2/`.
- Does the world feel like Kyle's art? → `side_by_side_01_wide.png`, `side_by_side_01b_wide_level.png`, `01_wide.png`
- Orbit the camera with the right stick during a fight — feel, speed, re-centre (R3), and does movement still make
  sense? → `02_orbit_000.png` … `05_orbit_270.png`, `06_orbit_top_pitch60.png`, `07_orbit_low_pitch-5.png`
- Hold telekinesis on the bot — does it read as a power? → `11_tk_start.png`, `12_tk_held.png`, `12b_tk_held_side.png`,
  `13_tk_release.png`, `14_tk_landing.png`, `15_tk_dazed.png`
- Legend readable, glyphs right, switches to keys when you touch the keyboard? → `08_legend_xbox.png`, `09_legend_keys.png`
- Punch and knockdown still fine on the hill → `10_punch_connect.png`, `16_knockdown.png`
- OPEN for Dan: the optional `7_tk_hold.fbx` / `ybot_float.fbx` would make the lift poses better — still wanted?


