# Active Development Plan — 7: The Observer

## Standing rules
- 7 never kills · strictly monochrome · non-sexual presentation · 7 is clearly an adult (details in `CLAUDE.md`).
- Per-item status at the end of every round: **done / not done / deferred (why)**. No silent partial completion.
- Short results: ~20 lines, one line per item + its one check, plus a "For Dan to check" list.

## Completed Tasks
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

## Current Objective — Round 4 done (2026-11-17), exe-verified off-screen; waiting for Dan to watch it
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

## Next Steps (Do Not Start Yet)
- **Hair ties → glowing round clips** (Kyle's art: `kyle_7_face_hairclips`, `kyle_7_riding_fog`, `kyle_world_mood`)
  instead of the metal cuffs. Dan 2026-11-17: "probably can be changed" — waiting for his go; small change in
  `cleanup_7.py` (cuff constants / old orb fallback) + emissive material, no re-rig needed if the mesh topology of the body is unchanged.
- **Forehead dot = 7's "energy point"** (Kyle, 2026-11-17). Keep the plain glowing dot. Gameplay: **Kyle's proposal
  (2026-11-17): its brightness shows how much energy she has left for the shield and telekinesis** — bright when
  full, dimming as she uses powers (no separate on-screen bar). Not yet scheduled; don't build it until a round asks.
- **"7" emblem on her back** (Kyle's art) — OPEN, not decided.
- **Saucer look pass:** darker city near the ship / dark overhead hull edge for Kyle's silhouette contrast, stronger
  in-fog glows, less boxy towers; seated camera vs. chair back; cat on-lap pose clear of her forearms; better cat rig
  (proper fitting or a Tripo/AccuRig quadruped rig) if the cat stays a main character.
- **Launcher / auto-update** (copy from `C:\Users\danmo\chastain_rush`) and releases repo — deferred until there is
  more than a demo. OPEN for Dan: publish every round once it exists? Default: yes.
- **Bubble helmet, hover bike, saucer, cat companion models** — deferred; need concept images and prompts first.
- **Big-head proportion variant** — DECIDED: not pursued for now (Dan prefers normal proportions).

## Technical Notes / Blockers
- `7_normal_v2.glb` is ~59 MB; it must go through Git LFS.
- Mixamo strips materials; reassign materials in Unity (Stage B item 5).
- FBX does not carry PBR metallic/smoothness reliably — set the cuff material up in Unity (Stage B item 5).
