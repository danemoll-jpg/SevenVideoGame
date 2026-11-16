# Active Development Plan — 7: The Observer

## Standing rules
- 7 never kills · strictly monochrome · non-sexual presentation · 7 is clearly an adult (details in `CLAUDE.md`).
- Per-item status at the end of every round: **done / not done / deferred (why)**. No silent partial completion.
- Short results: ~20 lines, one line per item + its one check, plus a "For Dan to check" list.

## Completed Tasks
- **Round 1 — fight-demo proof of concept — done (2026-11-16), exe-verified off-screen; Dan to play.** 7 rigged in
  Mixamo (1.70 m, ~50k tris, metal cuff hair ties, flat glowing forehead dot) spars with Y Bot in a monochrome room:
  punches, kicks, block, dodge, telekinesis push, finisher → knockdown → get up → dazed → recovers. Full item-by-item
  record (substitute clips, hit timings, checks) in `TODO-ARCHIVE.md` → "Round 1".
- **7 base model v2 — done in chat (2026-11-16).** Tripo model `7_normal_v1.glb` → `Art/Source/7_normal_v2.glb`:
  pigtail texture darkened (×0.57) to match scalp hair; band hair ties covered by a separate mesh `7_hairtie_orbs`
  (two spheres r=0.017 at x=±0.042, y=0.960, z=-0.039 in source units) with white emissive material `7_hairtie_glow`.
  Script kept as `Tools/Model/fix_v1_to_v2.py`. Source facts: 0.98 m tall, ~1.95M triangles, one 4K base-color texture.
  Verified only with rough point-cloud previews, not in Blender or Unity yet.

## Current Objective — Round 2: orbit camera, telekinesis showcase, foggy outdoor world
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

## Next Steps (Do Not Start Yet)
- **Launcher / auto-update** (copy from `C:\Users\danmo\chastain_rush`) and releases repo — deferred until there is
  more than a demo. OPEN for Dan: publish every round once it exists? Default: yes.
- **Bubble helmet, hover bike, saucer, cat companion models** — deferred; need concept images and prompts first.
- **Big-head proportion variant** — DECIDED: not pursued for now (Dan prefers normal proportions).

## Technical Notes / Blockers
- `7_normal_v2.glb` is ~59 MB; it must go through Git LFS.
- Mixamo strips materials; reassign materials in Unity (Stage B item 5).
- FBX does not carry PBR metallic/smoothness reliably — set the cuff material up in Unity (Stage B item 5).
