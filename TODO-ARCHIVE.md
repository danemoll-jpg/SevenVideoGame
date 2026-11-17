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


---
# Archived 2026-11-17: Round 3

## (was Current Objective) — Round 3: saucer mock-up — DONE (see status below); waiting for Dan to play

Proof of concept, playable but small, like the fight demo. All standing rules apply: strictly monochrome,
Kyle's fog/glow world look (`Atmosphere` prefab), non-sexual presentation, 7 never kills.
Commit the repo as it is before changing anything (safety checkpoint).

**Design sources** (claude.ai project docs, summarized): 7's ship is a visually appealing **retro-futurist flying
saucer**; the **interior is larger than the exterior suggests**, with **curves, carpeting and plants** — comfortable
and inviting; polished surfaces, organic furniture, soft lighting. An **observation area with 7's chair** and the
floor/walls/ceiling **fading away** was proposed earlier and Dan has now asked for it. Ignore "blue illumination" in
the older concept doc: the game is strictly monochrome. The cat: **black**, playful, **changes size**; 7 never rides it.

### Before Code starts — Dan (in parallel, Code works without these)
- Cat model: generate the image from `Docs/MODELS-NEEDED.md` → Tripo. If Tripo offers rigging/animation for a
  **quadruped**, export FBX with walk, idle, sit, jump and lie/sleep clips to `Art/Source/cat/`; otherwise export
  the textured GLB as `Art/Source/cat/cat_v1.glb`.
- Mixamo for 7 (FBX for Unity, 30 fps, without skin) → `Art/Mixamo/7/`: `7_stand_to_sit.fbx` (Stand To Sit),
  `7_sitting_idle.fbx` (Sitting Idle), `7_sit_to_stand.fbx` (Sit To Stand).

### Items
1. **Demo picker.** On launch, a small monochrome menu: *Fight demo* / *Saucer demo* (pad + keys). Menu/Start in a demo
   returns to the menu. Command-line `-saucer` / `-fight` skip the menu (for check runs).
2. **Cat model.**
   - Tripo rigged FBX with clips present → import it (Generic rig), use its clips.
   - **Status 2026-11-16: `Art/Source/cat/cat_v1.glb` is in (Tripo, unrigged, ~2M tris, one 4K texture, normalized
     to ~0.98 units tall — rescale). Checked in chat: clean legs/tail, pale eyes, whiskers modeled. Watch: the head is
     turned ~30° toward the viewer — straighten it in the rest pose before rigging; keep the whiskers out of the
     decimation (separate them first) so they don't break up.**
   - Only `cat_v1.glb` present → Blender cleanup like 7 (scale to a real house cat ≈ 0.25 m at the shoulder,
     origin at feet, ~15–20k tris), rig with **Rigify's cat metarig**, and keyframe simple clips by script:
     idle (breathing, tail sway), walk, sit, jump-up, curl-up sleep loop.
   - Nothing present → a simple stylized black cat placeholder, same clips, clearly named as placeholder.
   - Material: black fur with a dark-gray sheen and rim light so its shape reads against dark carpet; eyes pale gray.
3. **Saucer interior** `Assets/Scenes/Saucer.unity` (generated by a builder script like `FightDemoBuilder`):
   one round main deck (~16 m across, clearly bigger than the hull you'd expect), low dome ceiling with concentric
   curved ribs and soft white cove lighting, thick dark-gray carpet, curved built-in sofas and organic furniture,
   several potted plants (grayscale foliage), polished curved wall panels, a couple of nooks to wander into.
   At the front: **7's chair** — a sculpted pod/egg lounge chair on a pedestal facing a wide curved window that looks
   out and **down at the city**. Collision everywhere; camera never clips through walls or ceiling.
4. **City below.** The saucer hovers ~200 m above a city in Kyle's fog: clusters of towers and ring terraces with
   glowing lights, fading into fog layers. Reuse the `Atmosphere` prefab (tune a "from above" variant). Visible through
   the window, fully visible after the reveal.
5. **Walking and the cat.** 7 uses the existing third-person controller and orbit camera (no combat in this scene).
   The cat wanders the deck — walks, sits, washes, naps — at a **larger-than-house-cat size** (about 2×, tunable; one
   constant if Dan wants it normal size). It follows 7 loosely.
6. **Sit.** Near the chair a prompt shows (A / E): 7 turns and sits (Stand To Sit → Sitting Idle). The cat trots
   over, **shrinks to normal house-cat size with a soft white shimmer** as it hops, jumps onto her lap, turns and curls
   up asleep (attach to the hips/thighs so it moves with her; no clipping). B / Q = stand up: the cat hops off and grows
   back to its roaming size. If no sit clips are present, use a held pose and say so.
7. **Reveal (the ship fades).** While seated, Y / R toggles the reveal: floor, walls, ceiling and furniture dissolve
   away in a wave spreading out from the chair, with a soft white edge glow and drifting motes (monochrome), until only
   **7, her chair and the cat on her lap** remain, hovering high above the city. The chair tilts forward a little so
   she looks down at the city. The camera eases out to a wide over-the-shoulder / side view with the city below.
   Press again → the ship rebuilds in reverse. Standing up while revealed brings the ship back first.
   Camera framing when seated: behind/side or above-shoulder so the city is the subject — no low angles on her legs.
8. **Legend.** Xbox glyphs by default, keys after keyboard use (same system as the fight demo), context-sensitive:
   walking (move, camera, re-center, Sit near the chair), seated (Stand, Reveal / Return, camera).
9. **Checks** (muted, off-screen, never take focus): rebuild the exe; capture: ship interior wide, cat wandering,
   the sit, cat on lap close-up (over-shoulder), reveal halfway, fully revealed wide over the city, ship back, both
   legends. Monochrome max |R−G|,|G−B| = 0, audio level 0, 7's feet and the cat's paws on the floor while walking, cat
   resting on her lap without sinking into her legs (measure). Put the revealed wide shot next to
   `Art/Reference/kyle_world_mood.jpg` in `Docs/Previews/round3/` and judge the mood honestly.
10. End of round: update `TODO.md` (per-item status, "For Dan to check") and `CLAUDE.md`, commit and push.

### Round 3 status (2026-11-16) — built and exe-verified off-screen; Dan to play
Check run: `SevenFightDemo.exe -mute -saucer -capture <dir>` (~70 s). Shots + log in `Docs/Previews/round3/`
(`saucer_capture_log.txt`). Every shot monochrome (max |R−G|,|G−B| = 0), audio level 0 throughout.

1. **Demo picker — done.** `Menu` scene first: Fight demo / Saucer demo (stick or d-pad + A; W/S or arrows + Enter).
   Esc / Start in either demo returns to it. `-fight` / `-saucer` skip it (`-room` and plain `-capture` still go to
   the fight). Fight **reset moved** from Esc/Start to **Backspace / d-pad down** (legend updated). Check:
   `00_menu.png`; fight check run still completes (bot recovers, audio 0, monochrome 0).
2. **Cat model — done, with a simpler rig than planned.** `Tools/Blender/cleanup_cat.py` → `Art/Export/cat/cat_v1_rigged.fbx`:
   body squared to −Y (the source stood turned 21°), head straightened (5° left after that, not 30°), 18k tris, 0.25 m
   at the shoulder, origin between the feet, grayscale 2K texture. **Not Rigify's cat metarig** (it would still need
   every bone placed by hand): a 24-bone quadruped rig fitted to the mesh by script, distance-based weights, and
   scripted clips idle / walk / sit / wash / jump / sleep (grounded per frame: paws/rump on the floor). The source has
   **no separate whiskers** (one mesh), so they decimated away; 10 thin procedural whisker strips replace them.
   Fur: `Seven/DissolveLit` with a white rim 0.35 + smoothness 0.55; eyes come from the texture. Previews only in the
   game shots (crude rig: some stretching at the chest in walk; sleep is a C-curl with the head up).
3. **Saucer interior — done.** `Assets/Scenes/Saucer.unity` from `FightDemoBuilderSaucer.cs`: 16 m round deck, low
   dome with 4 concentric ribs, white cove light + 8 soft point lights, dark banded carpet, polished curved wall
   panels with seams, 3 curved built-in sofas, round table + 3 poufs + 2 side tables, 6 potted plants, 2 nooks with
   benches. Egg pod chair on a tulip pedestal facing a wide curved window + glass floor strip looking down. Collision:
   analytic (round wall, nook side walls, furniture circles); camera stays under the dome/inside the wall. Not done:
   no exterior hull view, so "bigger inside" is only implied.
4. **City below — done.** 200 m below: ~180 stepped towers in 11 clusters with white light bands, 4 ring terraces.
   `Atmosphere_Above.prefab` = "from above" variant of the Atmosphere prefab (`Docs/ATMOSPHERE.md`).
5. **Walking and the cat — done.** Same camera-relative walk + orbit camera; cat roams at **2× (`CatCompanion.RoamScale`)**:
   follows, wanders, sits, washes, naps (when 7 stands still). Check: 7's lowest point while walking −0.016…+0.004 m;
   cat paws −0.001…+0.002 m.
6. **Sit — done (real clips).** A / E near the chair: she walks to the spot, turns to the window, Stand To Sit →
   Seated Idle (Dan's `Stand To Sit` / `Seated Idle` / `Sit To Stand`; the clips don't line up — hips 0.47 m back and
   9 cm lower at the end of Stand To Sit — so her hips are pinned to the seat while seated). Cat trots over, hops
   with a white shimmer + motes shrinking 2× → 1×, curls up asleep on her lap and rides it. B / Q: cat hops off and
   grows back. Checks: seated 2.5 s after A; cat hop 4.2 s after A; **lap gap +0.004 m, 0 of 12 231 cat vertices
   below her thigh surface**. Not done: her forearms rest on her knees in Seated Idle and can touch the cat's back.
7. **Reveal — done.** Y / R while seated: everything but 7, the chair shell and the cat dissolves in a noisy wave from
   the chair (white edge glow, rising white motes; lights and glass fade as it passes), chair tilts 9°, camera eases
   out to a wide side view. Again → rebuilds from the outside in. Standing while revealed returns the ship first
   (4.2 s), then she stands. Shots `11`–`14`.
8. **Legend — done.** Xbox/keys auto-switch; walking and seated layouts + a centre "A Sit" / "E Sit" prompt near the
   chair. Shots `03`, `04`, `09`, `10`.
9. **Checks — done** (list above). Mood vs. Kyle (`compare_revealed_vs_kyle.png`), honestly: the fog-buried towers
   rising out of a bright haze are there, but the frame is **too flat and washed out** — no dark foreground or dark
   overhead mass, weak in-fog glows, plain box towers. Needs a tuning pass (see Next Steps).
10. **End of round — done** (this file, `CLAUDE.md`, commit + push).

Other: the over-the-shoulder seated camera still clips the egg chair's back at some orbit angles; the chair was made
a lower pod to help. Fight demo unchanged otherwise.

### For Dan to check (after Round 3)
- Menu: `Docs/Previews/round3/00_menu.png`. Play: run the exe, pick **Saucer demo**.
- Walk the ship: does it feel like 7's saucer (curvy, carpeted, plants, bigger inside)? — `01_ship_wide.png`,
  `01b_ship_wide_back.png`, `01c_window_chair.png`
- Cat: roaming size right (2×)? — `02_cat_wander.png`, `02b_cat_wash.png`
- Sit down: does the hop + shrink onto her lap read? — `05_sit_down.png`, `06_cat_hop_shimmer.png`, `07_seated.png`,
  `08_lap_closeup.png`, `08b_lap_cat_only.png` (7 hidden)
- Reveal: does the ship fading away land the moment? — `11_reveal_half.png`, `12_revealed_wide.png`,
  `13_revealed_over_shoulder.png`, `13b_revealed_far.png`, `14_ship_back.png`, `compare_revealed_vs_kyle.png`
- Stand up: `15_stand_cat_hops_off.png`, `16_after_stand.png`. Legends: `03`, `04`, `09`, `10`.
- Decide: fight reset is now Backspace / d-pad down (Esc / Start = menu) — OK?


---
# Archived 2026-11-17: QF2 + Round 4

## (was Current Objective) — Round 4 done (2026-11-17), exe-verified off-screen; waiting for Dan to watch it
Next round not set yet. Round 4 record below (move it to `TODO-ARCHIVE.md` when the next round starts).

### Quick fix QF2 (Dan, 2026-11-16) — build folder name — **done**
Builder writes `Unity\Builds\Seven\SevenDemo.exe` (product "7: The Observer — Demo", set in `ConfigurePlayer`); paths
updated in `CLAUDE.md` (no README/legend named the exe). Rebuilt; muted off-screen `-fight` and `-saucer` runs passed
(audio RMS 0, monochrome max 0). Old `Unity\Builds\FightDemo` left in place — **Dan can delete it.**

### Round 4 — Ratwing hangs asleep in a cave, wakes, drops and lands — **done**
Third demo-picker entry **Ratwing** (also `-ratwing`): a 10 s scene, then "A: replay  /  Menu: back" (keys: "Enter:
replay  /  Esc: back"); A skips to the end once it has been seen in full; Esc / Start back to the menu.
1. **Cleanup — done.** `Tools/Blender/cleanup_ratwing.py` (~2 min): 2.1 m standing height (`HEIGHT`, head top; wing hooks
   reach ~2.2 m), origin between the feet, facing −Y → 55,000 tris, X-symmetric, membranes and claws protected
   (vertex-group weighted collapse): **0 hole (boundary) edges**; the membranes keep 73 % of the budget (body ~15k).
   One material, double-sided in Unity; texture grayscale 2048. Metrics in `Art/Export/ratwing/ratwing_metrics.txt`.
2. **Rig — done, with one deviation:** a scripted 38-bone deform rig fitted from mesh slices (hips/spine/chest/neck/
   head, **jaw**, **eyes** (glint), arms, digitigrade legs with toes; per wing: root, upper, 3 two-segment finger spars
   found from the membrane's trailing-edge cusps) instead of a Rigify metarig — a Rigify control rig would have to be
   baked back to deform bones for the FBX anyway (same approach as the Round 3 cat). Membranes get angular "fan"
   weights (folding compresses them like a fan). Wing edge stretch (× rest length, 99th pct / max): wrapped 1.19 / 2.5,
   full spread 1.77 / 2.7 (worst edges are where the wing joins the back), stance 1.12 / 2.3. No tearing visible.
3. **Sequence — done.** Clips `sequence` (10 s @ 30 fps) + `idle` (2 s loop), all keyed by script (`seq_pose`,
   poses `P_SLEEP … P_STANCE`): asleep hanging by the feet with wings wrapped as a cloak, breathing + sway → eyes snap
   on at 3.55 s, head lifts and turns → wings unfurl past full spread, hiss (jaw) → drop with a half-roll upright,
   wings half open → crouch landing at 7.55 s, wings spread → rises to a hunched stance, breathing. Feet pinned to the
   ledge while hanging and to the floor after landing on every frame.
4. **Cave scene — done.** `Assets/Scenes/Ratwing.unity` from `FightDemoBuilderRatwing.cs`: noisy rock shell, 70
   stalactites + stalagmites, boulders, a rock ledge with a flat underside at 4.2 m (`RatwingCeil` = Blender `CEIL`),
   a ragged mouth (rim boulders) onto bright fog with huge spires fading out, `Atmosphere_Cave` prefab variant
   (dark interior, bright mouth), a cave-only post volume (tighter bloom, +contrast, saturation −100), 4 faint light
   shafts, drifting haze particles, dust puff at touchdown. Ratwing reads as a dark silhouette against the mouth.
5. **Camera — done.** Push-in while asleep, hold on the eyes, pull back for the 3.2 m wingspan, follows his hips down
   during the drop, wide on the landing. A-skip after the first full viewing (per session).
6. **Checks — done** (rebuild + muted, minimized/not-activated, Below Normal run, ~17 s): eye glints on at 3.59 s;
   **hanging: feet top 0.0–0.2 mm under the ledge over 249 samples, raycast to the ledge surface 0.0–0.2 mm**; lowest
   point while hanging 1.92 m above the floor; **landed: lowest foot 0.1 mm below to 0.2 mm above the floor** (66
   samples, raycast agrees); wingspan 3.24 m; replay restarts (T=0.99 s after 1 s), skip jumps to the stance;
   **audio output RMS 0.000000 (listener 0)** at start and end; **monochrome max |R−G|,|G−B| = 0** on every shot (also
   menu + `-fight -quick` regression). Wing holes/tearing: none seen in the full-fold and full-spread shots.
   Not perfect: from the close-up angle only one eye glint shows (the other sits behind the cheek); hands stay outside
   the wing cloak while hanging.
7. **End of round — done:** this file, `CLAUDE.md`, commit + push.

### For Dan to check (Round 4)
- Run `Unity\Builds\Seven\SevenDemo.exe` → **Ratwing**. Does the wake-up read as menacing and the drop/landing feel
  weighty? Is 2.1 m the right size (one constant: `HEIGHT` in `cleanup_ratwing.py`)?
- Shots in `Docs/Previews/round4/`: `01_cave_wide`, `02_asleep`, `02b_asleep_close` (wing cloak), `03_eyes_open`,
  `03b_eyes_close`, `04_wings_spread`, `04b_wings_spread_close` (membranes), `05_mid_drop`, `06_landing` (dust),
  `07_crouch`, `08_final_stance`, `09_prompt_pad` / `09b_prompt_keys`, `10_stance_side`, `10b_stance_back`,
  `11_after_skip`, `00_menu`; Blender contact sheets `blender_sequence_front.png` / `_side.png`,
  `blender_head_wake_hiss.png`; logs `capture_log.txt`, `ratwing_metrics.txt`.
- Taste calls: cave brightness (`Atmosphere_Cave` knobs, try with `-atmofile`), dust amount, how long he hangs asleep.
- Old `Unity\Builds\FightDemo` can be deleted.


---
# Archived 2026-11-17: Round 5

## Round 5 — Kyle's art gallery TV on the saucer — done (2026-11-17), exe-verified off-screen; Dan to browse
Checkpoint commit `5416d4b` before any change. Dan's idea: a TV on 7's ship where players scroll through Kyle G. Anderson's
artwork. Check run: `SevenDemo.exe -mute -gallery -capture <dir>` (~85 s); shots + logs in `Docs/Previews/round5/`.

1. **Gallery content folder — done.** `Art/Gallery/` = all 24 `Art/Reference/kyle_*.jpg` + `gallery.json` (`credit`
   "Art by Kyle G. Anderson", then one line per image: `file`, short plain `title` from the reference index, `include`).
   The builder copies `gallery.json` + included images to `StreamingAssets/Gallery/` (git-ignored copy); the game loads
   the list and images at runtime (`GalleryTV.cs`). How to add art: `CLAUDE.md` → "Kyle's art gallery". Adding straight
   into the built `SevenDemo_Data/StreamingAssets/Gallery/` works without a rebuild by design (read at every saucer
   start, from that folder — confirmed by the log path); adding a brand-new file there was not separately tested.
   Check: log "loaded 24, included in gallery.json 24, image files 24 -> MATCH".
2. **Default `include` — done: all 24 `true`** (Dan's decision). Default-off images: **none**.
3. **The TV on the saucer — done.** The back nook (335°) is now a lounge nook: 2.8 × 1.4 m rounded screen with a light
   glossy bezel and a soft white glow line, a low curved console with a glow strip, and a curved sofa out on the deck
   facing it (old nook bench removed there). Idle slideshow: cross-fade 1.6 s, then 8 s hold; the picture throws a soft
   light into the nook (follows the image brightness). Screen + sofa dissolve with the reveal. Check: `G01`, `G02`
   (mid cross-fade), `G03`; log "next automatic step 8.8 s later (expected ~8.8 s)".
4. **Viewing mode — done.** Prompt "A / E View art" in front of the sofa; camera eases (0.9 s) to face the screen
   (2.4 m, 0° off its normal), 7 stands still facing it and is hidden only if she would block the view. ← → / LB RB /
   d-pad / left stick = previous / next (0.3 s cross-fade, wraps both ways), Y / F = full view (letterboxed) and back,
   B / Esc = leave (camera eases back behind her; Esc no longer jumps to the menu while viewing, Start still does).
   Caption: title, "Art by Kyle G. Anderson", n / N. Legends: new "view art" row while walking, own layout while viewing
   (Xbox by default, keys after keyboard use). Checks: `G04`–`G17`, `G20`, `G21`; **real-input test 17/17 PASS** (every key
   and pad binding driven through virtual devices: E/A enter, arrows/RB/LB/d-pad/stick step, F/Y full, Esc/B leave and
   Esc stays in the saucer, 7 free to move after). Fix on the way: the walk camera now accepts the wall nooks as "inside
   the ship" (before, 7 standing in a nook collapsed the camera into her head).
5. **Monochrome — done.** Images are converted to grayscale on load (single-channel textures; screen and full-view
   shaders output v, v, v), so the red "7" logos show gray. Bloom is switched off by a high-priority volume while viewing
   (fades in/out 0.5 s), screen brightness 0.92 then. Check: max |R−G|,|G−B| = **0** over all 21 gallery shots and every
   regression shot.
6. **Checks — done.** Muted (audio listener 0, output RMS 0.000000 at start and end of every run), off-screen
   (minimized, never activated, Below Normal, one job at a time). Image count MATCH (24/24/24). **Aspect ratios
   measured** (image area drawn solid white, bounding box measured in the rendered frame) for all 24 images: worst
   error 0.3 % on the screen and 0.3 % in full view (= 1 px rounding). Regression: saucer run (sit 2.48 s, cat on lap
   gap +0.004 m, reveal/return/stand all fine), Ratwing run, fight `-quick`, menu shot — all completed, no exceptions.
   Note: loading the 24 images takes ~7 s in the background at the start of the saucer scene (one image per frame, the
   TV shows the first piece as soon as it is in; small hitches during that time).
7. **End of round — done.** `TODO.md`, `CLAUDE.md` (how to add art, `-gallery` check run), committed and pushed.

### For Dan to check (after Round 5)
- Play: exe → **Saucer demo** → walk to the TV nook (to 7's right, slightly behind her, at the start) → A / E.
  Does it feel right? Is the order good (edit `Art/Gallery/gallery.json`)? Are the titles OK?
- Screenshots `Docs/Previews/round5/`: `G01_tv_across_deck`, `G02_tv_idle_crossfade`, `G03_nook`,
  `G04_prompt_walk_xbox`, `G05_prompt_walk_keys`, `G07_view_xbox`, `G08_view_keys`, `G09_next_crossfade`,
  `G12_wrap_to_last`, `G13_view_narrowest`, `G14_full_xbox`, `G15_full_keys`, `G16_full_widest`, `G21_left`
  (+ `G18/G19_probe_*` = aspect measurement frames). Logs: `capture_log.txt`, `R_saucer_capture_log.txt`.
- Default-off images: **none** (all 24 on, per your decision).
- To add more of Kyle's art: drop the file in `Art/Gallery/`, add a line to `gallery.json`, rebuild (steps in `CLAUDE.md`).
- Ideas not done (say if wanted): 7 sitting on the TV sofa while watching; a d-pad left/right glyph in the legend
  instead of LB/RB; preloading thumbnails so the first seconds after entering the saucer have no hitches.


---
# Archived 2026-11-17: Round 6

## Current Objective — none: waiting for Dan's review of Round 6 (next round TBD)

## Round 6: Chapter 1 "The Moon Prison" (first real playable chapter) — plan as given, status below

### Round 6 status (2026-11-17) — per item
Check run: `SevenDemo.exe -mute -chapter1 -capture <dir>` (autopilot, ~4.5 min) — cell → "To be continued" with
**0 soft-lock suspicions**, 28 shots, all 78 lines played + subtitled, audio peak 0.000000, monochrome max diff 0.
Record: `Docs/Previews/round6/` (`capture_log.txt`, `soundcheck_log.txt`, `00_menu.png`, `01_…`–`28_…png`).
1. **done** — `Docs/VOICE-LINES.md`: 78 lines (scenes 1–11, ids `s01_01_7` … `s11_10_cat`, pauses from the stage
   directions). Generator `Tools/Voice/make_line_list.py` (also writes `Unity/Assets/Audio/Voice/voice_lines.json`).
2. **done** — 78/78 files via ElevenLabs (`Tools/Voice/generate_voices.py`; voices "Seven" / "Cat" looked up by name;
   key only from `ELEVENLABS_API_KEY`): `Unity/Assets/Audio/Voice/<scene>/<id>.mp3` (mp3 → Git LFS), 143.8 s total.
   Settings: 7 stability 0.55 / speed 0.95, cat stability 0.62 / speed 0.92 (`eleven_multilingual_v2`).
3. **done** — telepathy treatment on every cat line (`DialoguePlayer.MakeSource`): mono, centred, a bit quieter,
   high-pass 170 Hz, low-pass 7.2 kHz, short soft room (0.38 s). Vision: faint "far away" variant (quieter, darker,
   2.2 s reverb). **Sound check** (the only unmuted step, `-soundcheck`): one cat line, **1.84 s of sound**, peak 0.18,
   left = right (centred), muted again → 0. The reverb tail measured 0 after the clip ended (it fits inside the clip's
   own trailing silence) — Dan to judge the amount by ear.
4. **done** — subtitles (speaker name, cat lines in italics marked "(thought)", white on a soft dark band) + dialogue
   player with the script's pauses; A / Enter skips the current line.
5. **done (proof-of-concept look)** — hollow-Moon cavern in Kyle's fog/glow style (`Atmosphere_Moon` prefab): dark cell
   block and catwalk over a bright misty void, rails, mining carts, drills, lamps, scaffolds, huge half-buried ancient
   rings/monoliths fading into the haze; ancient hall + vision chamber; impound bay.
6. **done** — cell with barred door + energy field; the cat comes out of the small vent and sits outside; scene 1 at the
   door; scene 2: "stand back", the cat hops onto a crate, paws the release, bars slide up, 7 steps out.
7. **done** — walk-and-talk (scene 3) along the catwalk; two Scree at a lit gate behind a barricade (visible, not
   fightable); "Look closer" (A / E) at the older wall → scene 4 → the old stones sink, passage down.
8. **done** — one Scree guard (lamp shows where it looks; view cone + line of sight) blocks the ancient hall. Telekinesis
   (X / T) knocks the loose stone off a broken pillar → the guard goes to study the noise → 7 slips past on the far side.
   Being seen = fade + reset to the hall entrance (no combat, no harm). Check run: walked into view once on purpose →
   reset (`10_s05_spotted.png`), then stone + sneak path worked.
9. **done** — touch the glowing inscription: white wash, the chamber whole (columns, arches, glowing frieze, the
   symbol) with 9 pale glowing faceless figures, cat lines faint and far away; 8.4 s; back to ruins; rest of scene 6;
   7's gesture (the symbol she saw) sinks the sealed door. Vision content kept generic.
10. **done** — ledge view of the bay (scene 7), ramp down, telekinesis on the clamp lever (clamps retract, rim glow up,
    ship ramp lowers), scene 8 at the ramp, 7 boards first, then the cat; ramp closes; liftoff shot; fade.
11. **done** — Round 3 saucer reused; galley nook added (curved counter, wall cabinets, refrigerator with a door that
    opens and an inside light). Scene 9 on arrival, scene 10 at the fridge, scene 11, a quiet beat, the cat walks to the
    sofa and curls up asleep, "To be continued", back to the menu. Cat house-cat size (scale 1) throughout.
12. **done** — `ChapterCat`: follows, waits at interaction points, leads (to the release crate, along the catwalk, down
    to the bay), hops/paws. Catch-up (> 8 m behind and out of view → reappears near 7 out of view) exists; **not
    triggered** in the check run (max distance while following/leading 5.4 m).
13. **done** — objective line (top left), A / E and X / T prompts with Xbox glyphs / keys, chapter legend. Checkpoint per
    scene; the spotted fail state resets only the hall. Backspace / d-pad down restarts from the last checkpoint
    (reloads the level) — **not exercised by the check run**.
14. **done** — "Chapter 1 — The Moon Prison" at the top of the menu; `-chapter1` (and `-ch1aboard` = start at scene 9).
15. **done** — `Scree_placeholder` (gray Y Bot + hood with the face in shadow + robe, 7's Humanoid idle/walk) for the
    guard, gate guards and two bay workers; `Docs/MODELS-NEEDED.md` entry (needs Kyle's design).
16. **done** — procedural saucer exterior (flat disc, glowing rim, dark glossy upper hull, belly glow, hinged ramp,
    cradle + 4 clamps); `MODELS-NEEDED.md` "procedural for now".
17. **not done (Dan's step, optional)** — Mixamo clips for 7, save in `Art/Mixamo/7/`: `7_sneak_walk.fbx` ("Sneak
    Walking"), `7_touch_wall.fbx` ("Button Pushing" or similar), `7_look_around.fbx` ("Looking Around"). Substitutes now:
    sneak = walk at 0.62× (1.25 m/s), touch = telekinesis clip held at 45 %, look-around = none.
18. **done** — rebuild ok; autopilot playthrough with 0 soft-locks; shots per scene + vision before/during/after +
    saucer + liftoff (subtitles visible); voice files 78 = subtitles 78 (ids matched, none missing); cat ≤ 5.4 m behind;
    monochrome 0; audio 0 except the sound check. Demos still load: menu, fight (`-quick`), saucer (21 shots), gallery
    (input test 17/17), Ratwing (16 shots) — all 0 exceptions, audio 0.
19. **done** — this file, `CLAUDE.md`, commit + push.
Not verified: real controller/keyboard play of the chapter (only the autopilot ran), the real length with every line
played in full (estimate 8–12 min: 144 s of voice + walking + the Scree room), checkpoint restart key, cat catch-up.
Scene 12 waits for the vision content (as planned).

Commit the repo as it is before changing anything (safety checkpoint) — it includes Dan's new `Docs/STORY.md`,
`Docs/DIALOGUE.md` and `Docs/VOICES.md`. **Read all three first.** Proof-of-concept quality like the demos, but this is
the real opening of the game: it should play start to finish in roughly 10–15 minutes.
All standing rules apply: monochrome, Kyle's fog/glow world look, non-sexual presentation, **7 never kills**.

**Story beats covered** (`Docs/DIALOGUE.md` scenes 1–11; scene 12 waits until the first vision's content is decided):
7 is held in a cell in Tunneler's mining prison inside Earth's hollow Moon → a black cat appears, speaks telepathically,
opens her cell → they talk → the guarded route forces them into an older passage → a Scree guard blocks the way and
they get past without hurting anyone → 7 touches an ancient surface and has her first vision, which opens the way →
they see her impounded saucer → release it, board, escape → aboard the saucer: the larger interior, the first meal
(free-range chicken), and the cat choosing to stay. Ends on "To be continued".

### Voice lines
1. **Line list.** Turn every spoken line in `Docs/DIALOGUE.md` scenes 1–11 into `Docs/VOICE-LINES.md` (id, scene,
   speaker, text), ids like `s01_03_cat`. Skip stage directions and the bracketed placeholder lines of scene 12.
2. **Generate the audio** with the ElevenLabs API: key from the `ELEVENLABS_API_KEY` environment variable only (never
   written to the repo, logs or chat); Dan's saved voices are named **`Seven`** and **`Cat`** — look their voice ids up
   by name. Save as `Unity/Assets/Audio/Voice/<scene>/<id>.wav` (or .mp3) and keep the generator script in `Tools/`.
   Delivery: 7 soft and gentle; the cat dry and unhurried. **If the key is missing or the API refuses, STOP after the
   line list and tell Dan** (he can set the key, or generate the files himself with those exact names).
3. **Telepathy sound:** every cat line plays with a light "inside your head" treatment (close, dry, soft short reverb,
   slightly centered/narrow), so players hear that his mouth isn't moving. 7 speaks aloud normally. No lip-sync.
4. **Subtitles** for every line (white text, speaker name, monochrome), plus a dialogue system that plays lines in
   order with the pauses the script asks for ("a short pause", "a quiet beat"). Lines can be advanced with A / Enter.
   **Sound checks:** voices make sound — keep every test run muted except one short sound check of the telepathy
   effect; say which step played sound and for how long.

### The level (hollow Moon, Tunneler's mining prison)
5. **Look:** a vast cavern inside the Moon in Kyle's fog/glow style (`Atmosphere` prefab): dark rock and industrial
   mining structures (catwalks, rails, mining carts, drills, lamps) in the foreground, a bright glowing haze far
   below/beyond, huge ancient shapes half-buried in the rock. Reference: `Art/Reference/README.md` (world pieces).
6. **Cell (scenes 1–2):** 7 starts in a small rock-and-metal cell with a barred or energy-field door. She can move
   around inside. The cat walks into view outside; the scene-1 dialogue plays when she approaches the door. The cat
   then jumps to a wall release and paws it (scene 2); the door opens.
7. **Corridors (scenes 3–4):** a short walk with a safe spot where scene 3 plays as a walk-and-talk. The obvious route
   is blocked by guards (visible, not fightable); 7 notices an older wall section — interact (A / E) to look closer →
   scene 4 → a gap into ancient structures beneath the facility.
8. **The Scree encounter (scene 5):** one Scree guard (placeholder model — see Models) blocks the ancient passage.
   Nonlethal solution: 7 uses her **telekinesis** to knock something over or roll a stone away from them as a
   distraction; the guard goes to check and she slips past (or a short stealth path). Getting spotted just resets her
   to the start of that room — no combat, no harm. Scene 5 lines play before and after.
9. **The first vision (scene 6):** an ancient wall with a faintly glowing inscription. Touch it (A / E): the screen
   washes to white, and for ~8 s the ruins around her appear **whole** — intact architecture and ghostly figures of
   people (pale, silhouetted, softly glowing, faces not shown), while the cat's lines are heard faintly as if from far
   away ("What is it?", "Can you hear me?"). Then it fades back to the empty ruins and the rest of scene 6 plays. A
   symbol/gesture she saw in the vision opens the passage. Keep the vision contents generic — the real contents are
   undecided.
10. **The impound (scenes 7–8):** a ledge overlooking a large impound bay with **7's saucer** clamped down
    (exterior per Kyle: a wide, flat disc with a glowing rim and a dark glossy hull — `kyle_7_backpack_saucer`,
    `kyle_7_on_hull`). Scene 7 at the viewpoint. Go down, release the clamps (telekinesis on a lever, or a control
    panel), scene 8 at the ramp. Board → the saucer lifts off and leaves the cavern (short in-engine shot), fade.
11. **Aboard (scenes 9–11):** reuse the saucer interior from Round 3. Scene 9 as they enter. Add a small galley nook
    (curved counter, a refrigerator 7 opens) for scene 10. Scene 11 plays after, then a quiet beat, the cat settles on
    the sofa or her chair, and "To be continued". Then return to the menu.
12. **Cat companion:** the cat (normal house-cat size the whole chapter — no size changing yet) follows 7 with simple
    follower behaviour, waits at interaction points, and leads the way where the script says he does (to the release,
    toward the small opening). He never gets stuck: if he falls behind more than ~8 m he catches up out of sight.
13. **Objectives:** a short objective line (e.g. "Follow the cat", "Find another way out", "Release the saucer") and
    interaction prompts with Xbox glyphs / keys as in the demos. Checkpoint at each scene; a fail state only resets
    to the last checkpoint.
14. **Menu:** add **"Chapter 1 — The Moon Prison"** at the top of the picker (also `-chapter1`); keep the demos below.

### Models and animation
15. **Scree (placeholder):** the Scree are an oppressed alien people (not evil) working for Tunneler. No design exists
    yet → use a clearly temporary humanoid: the Y Bot or a hooded, robed silhouette, gray, with Humanoid walk/idle
    retargeted from 7's clips. Name it `Scree_placeholder`. Add a Scree entry to `Docs/MODELS-NEEDED.md` (needs Kyle's design).
16. **Saucer exterior:** build it procedurally like the other builders (disc, rim glow, dark glossy upper hull, a ramp
    that lowers). Add it to `MODELS-NEEDED.md` as "procedural for now".
17. **Optional Mixamo clips for Dan** (Code works without them; list them in the report): `7_sneak_walk.fbx` (Sneak
    Walking), `7_touch_wall.fbx` (closest to touching/pressing a surface, e.g. "Button Pushing"), `7_look_around.fbx`
    (Looking Around). Without them: crouched walk = slowed walk, touch = held arm-forward pose.

### Checks and wrap-up
18. **Checks** (muted, off-screen, never take focus): rebuild; an automated playthrough (scripted inputs or a debug
    "advance" path) from the cell to "To be continued" with no soft-locks; capture one shot per scene + the vision
    (before / during / after) + the saucer exterior + liftoff; subtitles visible in shots; all voice files present and
    matched to their subtitles (count + ids); cat never more than ~8 m behind except by design; monochrome max
    |R−G|,|G−B| = 0; audio 0 except the one sound check. Demos still load (quick regression).
19. End of round: update `TODO.md` (per-item status, "For Dan to check") and `CLAUDE.md`, commit and push.

### For Dan to check (after Round 6)
- **Play Chapter 1** start to finish (menu → "Chapter 1 — The Moon Prison"). Does it flow? Right length? Any spot where
  you get stuck (Backspace / d-pad down restarts from the checkpoint)?
- **Voices + telepathy**: do the "Seven" and "Cat" deliveries fit? Is the cat's "inside your head" treatment too
  strong / too subtle (reverb amount especially)? Any line to regenerate → `python Tools/Voice/generate_voices.py --force --only <id>`.
- **The vision** (`Docs/Previews/round6/13_s06_vision_before.png`, `14_…during_far_voice.png`, `15_…during.png`,
  `16_…after.png`): does it land? Figures too bright?
- **Look**: cell `02_s01_first_contact.png`, catwalk `06_s03_walk_and_talk.png`, gate `07_s04_guarded_route.png`, ancient
  hall `09_s05_scree_guard.png`, bay `19_s07_saucer_from_ledge.png` / `20_s07_saucer_released.png`, liftoff
  `22_s08_liftoff_rising.png`, galley `25_s10_free_range.png`, sofa `27_s11_cat_settles_on_sofa.png`, end `28_s11_to_be_continued.png`
  (all in `Docs/Previews/round6/`).
- **Scree**: ask Kyle for a concept (placeholder now). **Optional Mixamo clips** (item 17).



---
# Archived 2026-11-17: Round 7

## (was Current Objective) — Round 7: Chapter 1 fixes from Dan's playthrough + sound effects + music
Commit the repo as it is before changing anything (safety checkpoint).

### Fixes (Dan, 2026-11-17)
1. **Cell door → laser beams, at the opening.** The barred door sits too far back — 7 could walk through. Replace it
   with a row of horizontal **white laser beams** (monochrome glow, soft hum) set exactly in the cell opening, flush
   with the walls. Solid collision; beams switch off one by one when released. Check: drive 7 into the beams from
   several angles — she must stay inside (measure her position).
2. **The release must be out of her reach and sight.** If 7 could see the lock she could work it with telekinesis.
   Move the release out of view of the cell — around a corner or inside a guard alcove down the corridor. The cat
   walks out of sight to work it; 7 (and the player) only hear it (click + beams powering down), then he walks back.
   Check: line-of-sight from every point inside the cell to the release panel is blocked (raycasts), and it is not
   in any camera shot taken from inside the cell.
   **DECIDED (Dan, 2026-11-17): the laser field dampens her powers.** Her forehead **energy point** is dark the whole
   time she's in the cell and relights (soft brightening glow, small shimmer sound) as she steps out past the dead
   beams. This is why she can't telekinesis her way out; it is also the first use of Kyle's energy-point idea.
3. **Board by tractor beam, no ramp.** Remove the ramp/bridge. After the clamps release, the saucer lifts a little
   and hovers; a soft cone of white light shines down from an iris hatch in its belly. Scene 8 plays at the edge of
   the light ("Get aboard" → "After you" → …): 7 steps into the cone first and floats gently up (upright, arms
   slightly out, hair drifting), drawn in through the hatch; then the cat floats up (paws dangling, tail curling).
   The hatch irises shut, the light fades, liftoff as before. The classic UFO "sucked up" moment.

3b. **New beat — the city reveal (Dan, 2026-11-17). Full description: scene 6b in `Docs/DIALOGUE.md` — build from it.**
   After the ruins and the first vision, they come out of a rough mining passage onto a **narrow stone terrace**. 7 walks
   a few steps and **turns back**. Build the view (Dan's proposals — follow them closely):
   - The **prison is embedded in the base of a colossal ancient structure**; its machinery, fences and pipes are recent
     add-ons bolted to something much older. Looking up, it is only one of many.
   - The **city spans the inside of the Moon**: broad terraces descend into a basin and rise again; farther off,
     buildings climb the **curved horizon until they almost hang overhead**; pale towers and bridges emerge from
     layers of gray distance. Part of it reads as landscape at first.
   - **Ancient lunar architecture:** immense stepped buildings, slender observation towers, circular structures aligned
     with each other (astronomical feel); smooth pale surfaces, deep black openings.
   - **A quiet center:** several monumental pyramids around a **dark circular depression** — a landmark, unexplained.
   - **Tunneler's operation cutting through it:** conveyors across old avenues, excavators biting into terraces,
     freight lifts moving along walls, tiny Scree and machines, some districts busy, others silent. Productive and
     relentless — he is visibly dismantling the city to reach what lies beneath.
   - **Lighting:** one broad, diffuse, unexplained source somewhere above the visible city; it catches pale upper
     surfaces while streets stay dark. Strictly grayscale: scale from layers, silhouettes and **tiny moving lights /
     vehicles**, not glows. Use the `Atmosphere` setup with many depth layers.
   - Build it for the view (big distant geometry, impostors / LODs are fine); keep it reasonable to run.
   - **Most important (Dan): it must feel like Kyle's art come to life.** Lean on the fog and glow to do the work —
     like his paintings, most of the city should be **suggested, not detailed**: big soft silhouettes dissolving into
     bright haze, only the nearest structure and a few key shapes (the prison's colossal host building, the pyramids
     and dark depression, a conveyor or excavator) read clearly; everything farther away is layered shapes fading into
     glowing mist. Prefer fewer, larger forms and soft atmospheric layering over lots of small detail. Judge it side by
     side with `kyle_world_mood`, `kyle_7_riding_city`, `kyle_world_sweeper` and `kyle_7_cliff_light` — if it looks
     more like a detailed 3D city than a painting, add fog and remove detail.
   **Timing:** camera gives the city room to fill the frame; **several seconds of only sound** (distant machinery, wind
   on the terrace, faint city life) and the main theme before anyone speaks; no objective text, no joke; the cat waits
   beside her. **Dialogue (voice it):** 7 "All this was above us?" / CAT "Most of it." / (she follows the curve of the
   city — camera tilts up along it) / 7 "Someone built this." / CAT "Yes." / (a distant section of machinery starts
   moving; **stone falls from an ancient terrace** — staged, visible) / 7 "And they're taking it apart."
   Then **she spots the impound bay holding her saucer** from the terrace (or the cat points it out): scene 7's lines
   play here; the saucer is small but recognizable (its rim glow). Control returns; they head down to it.
   The earlier proposed lines ("I didn't know all this was here." …) are an **alternative**: voice them too and put both
   sets on a switch (`cityRevealDialogue = primary | alternate | off`, default primary).
   **Opening order must be exactly:** first meeting → cell rescue → escape through the ruins and first vision →
   emergence and city reveal → saucer recovery and boarding → first meal and conversation.
   Captures: terrace wide before dialogue, looking up the curve, the pyramids + depression, the falling stone, the
   saucer spotted.

3c. **Dan's optional Mixamo clips — use them if present** (check at the start of the round): `Art/Mixamo/7/7_sneak_walk.fbx`
   (sneaking past the Scree), `7_touch_wall.fbx` (the first vision + the older wall), `7_look_around.fbx` (idle variety /
   the city reveal turn), `Art/Mixamo/YBot/ybot_float.fbx` (the bot
   hanging in the lift). Replace the stand-ins (slowed walk, held poses) where a clip exists; report which were used.
   **No `7_tk_hold.fbx` (Dan, 2026-11-17: nothing on Mixamo fits).** Keep the held peak pose of `7_telekinesis` for the
   lift, but give it life procedurally: slow breathing, a slight sway of the body, a faint strain/tremble in the arms
   and hands, and a small hand adjustment as the bot moves (layered on top of the pose — Animation Rigging / IK or an
   additive offset). Subtle, never shaky.

3d. **No "To be continued" — the chapter ends in free exploration of the saucer, and the game gets an exit (Dan, 2026-11-17).**
   - After scene 11 (the cat chooses to stay), there is no end card and no return to the menu. The player keeps control
     of 7 and can **explore the whole saucer** freely: the deck, the galley, her chair with the reveal, the art-gallery
     TV and the sofa. The cat stays aboard and does his roaming / napping behaviour (normal house-cat size for now), and
     still hops onto her lap when she sits in her chair. `aboard_saucer` music plays.
   - A quiet objective line once, then it disappears: e.g. "Explore the ship".
   - **Exit:** the pause menu (Start / Esc) gets **Resume · Settings (music / SFX / voice volume) · Main menu · Quit
     game**, with a confirm on Main menu / Quit. The main menu also gets a **Quit** entry. Quit closes the game
     cleanly (`Application.Quit`). Esc must no longer jump straight to the menu without asking.
   - **Continue:** save the chapter state when they reach the saucer, and add **Continue** at the top of the main menu
     when a save exists — it loads straight into exploring the saucer after the chapter. "Chapter 1 — The Moon
     Prison" still starts the chapter from the beginning.
   - Remove the "To be continued" sting from the music plan (`main_theme` no longer plays at an end card).

### Sound effects (ElevenLabs Sound Effects API)
4. Generate SFX with the ElevenLabs sound-effects endpoint, key only from `ELEVENLABS_API_KEY` (never in the repo,
   logs or chat). Keep the prompt list in `Docs/SFX-LIST.md` (id, prompt, duration, where used) and the generator in
   `Tools/Audio/`. Save to `Unity/Assets/Audio/SFX/<id>.wav`. Style: quiet, atmospheric, natural — nothing cartoony.
   At least: cavern ambience (wind, distant drills), laser-door hum + beams powering down, the hidden release click,
   7's footsteps (rock, metal catwalk), soft cat paw steps, telekinesis (gathering hum + push whoosh), crate knocked
   over / stone rolling, Scree guard footsteps, vision (white-wash whoosh + soft glassy shimmer), ancient stone door
   sinking, clamp hydraulics releasing, saucer idle hum, tractor beam (rising soft tone), hatch iris, liftoff,
   saucer interior ambience, fridge door, menu move / confirm.
5. Wire them in with a small mix: dialogue always on top, SFX under it, ambience lowest. 3D sounds placed in the
   world (hum at the beams, footsteps at the feet).

4b. **7 sounds monotone — regenerate her lines with more variation (Dan, 2026-11-17).** The cat is fine; leave his
   lines. For every 7 line in `Docs/VOICE-LINES.md`, add a short delivery note from the scene's context (e.g. curious,
   wary, startled, sincere, wry, awed, quiet, determined) and regenerate with more expressive settings: prefer the
   **Eleven v3** model with audio tags from those notes (e.g. `[curious]`, `[whispers]`, `[softly]`, `[surprised]`) if
   the API offers it; otherwise lower **stability (~0.30–0.35)**, raise **style (~0.4–0.5)**, keep similarity high, and
   use the previous/next line as context for intonation. Keep her soft and gentle — variation, not drama. Keep the old
   files in `Unity/Assets/Audio/Voice/_v1/` so Dan can compare; regenerate the new 6b city-reveal lines the same way.
   Report the settings/model used. If she is still flat, say so — Dan can redesign the voice (description in chat).

### Music (Suno — Dan makes the tracks)
6. **Music system:** three tracks from `Docs/MUSIC.md` — `main_theme` (menu, the city-reveal swell, tractor beam /
   liftoff), `moon_prison` (cell through the ruins, including sneaking), `aboard_saucer` (inside the
   ship). The first vision fades the music out and plays no track. Cross-fade ~2–3 s, duck under dialogue, loop
   cleanly; files loaded by name from `Unity/Assets/Audio/Music/`, missing file = silence, no errors. Add music / SFX /
   voice volume sliders in a small pause menu (Start / Esc).

### Checks and wrap-up
7. **Checks** (never steal focus): rebuild; muted automated playthrough start to finish; captures of the laser door
   (on / off), the cat leaving to the hidden release, the tractor beam (7 rising, cat rising, hatch closing).
   **Sound:** one short unmuted sound check of a few SFX (say which and for how long); prove the rest of the runs were
   muted (level 0). Monochrome max 0. Demos still load.
8. End of round: update `TODO.md` (per-item status, "For Dan to check") and `CLAUDE.md`, commit and push.

### For Dan (in parallel)
- Make the music in Suno from `Docs/MUSIC.md` and drop the MP3s into `Unity/Assets/Audio/Music/` (any time; Code's
  system picks them up on the next build).
- Music: **done** — Dan's three tracks are in `Unity/Assets/Audio/Music/` (`main_theme`, `moon_prison`, `aboard_saucer`).


---
# Archived 2026-11-17: Round 8

## (was Current Objective) — Round 8 (started 2026-11-17, right after Round 7)
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

## (was Current Objective) — Quick fix QF3: Dan's playthrough of Rounds 7 + 8 (2026-11-17)
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

## (was Current Objective) — Quick fix QF4: two Chapter 1 bugs + the real Scree model (2026-11-17)
Commit the repo as it is first (safety checkpoint).

1. **Opening soft-lock: the cat walks into the cell and scene 1 never starts.** In one of Dan's runs the cat kept
   entering 7's cell, and the first-contact dialogue didn't trigger, so the game couldn't progress.
   - The cat must **never enter the cell**: the laser beams (and the cell opening) block him too — collision / navmesh
     exclusion for the cat, not only for 7. His approach to his spot outside the door is a fixed, scripted walk.
   - Make the scene-1 trigger **robust**: it fires when 7 comes near the door no matter where the cat is (if he isn't on
     his mark, he is walked/placed there first, out of view if needed). Add a watchdog: if the opening hasn't started
     within ~20 s of the cat arriving, start it anyway. Same safety for every story trigger in the chapter — no scene may
     depend on the cat being in an exact spot without a fallback.
   - **Check:** run the opening many times (e.g. 25 automated runs with varied timing, 7 standing in different parts of
     the cell, moving or idle when the cat arrives) — 25/25 must reach scene 1 and the cat must stay outside every time.
2. **Camera stuck behind a wall during the Scree discussion (scene 5).** When they spot the guard and talk about how to
   get past him, the camera ends up behind a wall.
   - Give dialogue moments a **clear view**: before and during any conversation, check line of sight from the camera to
     both 7 and the cat (and the guard in scene 5); if blocked, move to a clear framing (pull in, swing around, or a
     preset conversation camera for that spot) — smoothly, no snapping through geometry.
   - Fix scene 5 specifically with a good framing that shows 7, the cat and the guard beyond them.
   - **Check:** for every dialogue scene in Chapter 1, sample the camera through the whole conversation: speakers
     visible (raycasts) 100% of the time; capture a shot of each, including scene 5.
3. **The Scree — replace `Scree_placeholder` with the real model (Dan, 2026-11-17).** Model: `Art/Source/scree/scree_v1.glb`
   (Tripo, unrigged, ~2M tris, 4K texture); notes and check renders in `Docs/MODELS-NEEDED.md` and
   `Docs/Previews/dan/scree_v1_check.png`; design refs `Art/Reference/scree_ref_34.png`, `scree_ref_side.png`.
   - **Cleanup** (`Tools/Blender/cleanup_scree.py`, like the others): **height 1.9 m** standing (a little taller than
     humans; 7 is 1.70 — one constant), origin at the feet, ~40–50k tris; **rotate the abdomen + wing covers about the
     waist so they extend straight behind** (they point back-and-sideways ~45° now); **straighten the head** in the rest
     pose; keep the breathing hose, antenna and claws intact when decimating; wing covers solid.
   - **Rig as a Unity Humanoid-compatible skeleton**: the upper arm pair = humanoid arms, extra bones for the lower arm
     pair, an abdomen chain and the antenna. **Reuse existing Mixamo clips by retargeting** — `7_idle`, `7_walk`,
     `7_look_around` (and others if they read well). Lower arms loosely follow the upper ones, abdomen and antenna sway
     gently (procedural). Check the backward-bending (digitigrade) legs don't distort on retargeted walks — foot IK or a
     keyed walk if they do. **Keyframe only what doesn't exist:** a work / carrying loop and a short alert reaction.
   - Materials: pale matte shell, dark joints; eyes get a small soft white glint and the tanks a small light (emissive,
     monochrome) — Kyle's small-glow touch.
   - **Swap it into Chapter 1**: the guard in scene 5 (alert reaction when 7's distraction lands, then walks to check —
     same nonlethal stealth as now) and a few tiny workers in the city reveal (work loop). Keep the placeholder as a
     fallback.
   - **Check:** previews like the other models (front / side / back / face, abdomen straight behind from the top),
     in-game shots of the guard idle / alert / walking and the workers, feet on the ground, no limb tearing.
4. Usual checks (muted, off-screen, never steal focus), rebuild, **publish a release**, update `TODO.md` / `CLAUDE.md`,
   commit and push, report in a few lines per item.

Addition from Dan during the round (2026-11-17): Scree approved with one change — tuck the tail down. When straightening
the abdomen + wing covers, also angle them downward so the abdomen tucks down and slightly under behind the hips
(roughly 35–50° below horizontal, like a resting wasp or grasshopper) instead of sticking straight out. Keep the wing
covers lying along it, make sure it clears the legs in the walk and idle clips, and show it in the side-view preview.
Record this in TODO.md and Docs/MODELS-NEEDED.md as "Scree design approved 2026-11-17 — tail tucked down".
