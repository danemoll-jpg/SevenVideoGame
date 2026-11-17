# Active Development Plan — 7: The Observer

## Standing rules
- 7 never kills · strictly monochrome · non-sexual presentation · 7 is clearly an adult (details in `CLAUDE.md`).
- Per-item status at the end of every round: **done / not done / deferred (why)**. No silent partial completion.
- Short results: ~20 lines, one line per item + its one check, plus a "For Dan to check" list.

## Completed Tasks
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

## Current Objective — Round 4: Ratwing wakes (short animated scene)
Commit the repo as it is before changing anything (safety checkpoint). **Do Quick fix QF2 first** (below), then Round 4.

### Quick fix QF2 (Dan, 2026-11-16) — build folder name — **done (2026-11-17)**
Builder writes `Unity\Builds\Seven\SevenDemo.exe` (product "7: The Observer — Demo"); paths updated in `CLAUDE.md`
(no README/legend named the exe). Rebuilt; muted off-screen `-fight` and `-saucer` runs passed (audio RMS 0, monochrome
max 0). Old `Unity\Builds\FightDemo` left in place — **Dan can delete it.**
The build now holds both demos, so `Builds\FightDemo\SevenFightDemo.exe` no longer makes sense.
1. Build to **`Unity\Builds\Seven\SevenDemo.exe`** (product name "7: The Observer — Demo"). Change the builder
   script's output path (not just the folder), so every future build goes there.
2. Leave the old `Builds\FightDemo` folder alone (deleting is off) — tell Dan he can delete it.
3. Update every path in `CLAUDE.md` (Commands, check-run command, Where things stand) and the README/legend text if it
   names the exe. Rebuild, run the usual muted off-screen check (`-fight` and `-saucer`), mark status here, commit, push.

### Round 4 — Ratwing hangs asleep in a cave, wakes, drops and lands
A few-second animated scene, a third choice on the demo picker (**Ratwing**, also `-ratwing`). No gameplay: it plays,
then shows "A: replay  /  Menu: back". All standing rules apply (monochrome, Kyle's fog/glow look, nobody dies).
Ratwing: an intelligent humanoid vampire bat — an established antagonist (`Art/Reference/kyle_ratwing.jpg`).

**Model:** `Art/Source/ratwing/ratwing_v1.glb` (Tripo, unrigged; checked in chat 2026-11-17). Facts: ~1.95M tris, one 4K
texture, 0.75 units tall with wings spread ~0.98 wide. Gaunt humanoid body with visible ribs, clawed hands and bird-like
clawed feet, large pointed ears, open fanged mouth, furry mane at the neck and waist. **The wings are separate limbs
growing from the back/shoulders, not membranes on the arms** — so Mixamo would rig the body but not the wings.

1. **Cleanup** (`Tools/Blender/cleanup_ratwing.py`): scale to **2.1 m standing height** (taller than 7's 1.70 — one
   constant, Dan may change it), origin at the feet, ~50–60k tris. Protect the thin wing membranes and claws when
   decimating (no holes, keep the membrane outline and finger bones), membrane material double-sided. Previews like 7's.
2. **Rig in Blender** (skip Mixamo — every motion here is custom): Rigify human metarig fitted to the body plus a bone
   chain per wing (shoulder root, arm, 3–4 finger/spar bones) so the wings can fold tight around the body and spread
   wide. Clean weights on the membranes so folding doesn't tear or crumple them badly.
3. **Keyframe the sequence** (~10 s, 30 fps, by script so it can be re-tuned), exported as one clip or a few clips:
   - 0–3.5 s **asleep**: hanging upside down by his feet claws from the cave ceiling, wings wrapped around his body
     like a cloak, slow breathing, a slight sway.
   - 3.5–4.5 s **wakes**: eyes snap open (pale glint), head lifts and turns toward the camera.
   - 4.5–6.5 s **unfurls**: wings open wide in a big stretch, mouth opens in a hiss.
   - 6.5–8 s **drops**: feet release, he falls, flips upright mid-air with wings half open to slow himself.
   - 8–10 s **lands** in a crouch with wings spread (haze/dust puff), rises to a menacing stance, idle breathing.
4. **Cave scene** `Assets/Scenes/Ratwing.unity` (generated by a builder script): dark rocky cave with stalactites and a
   ceiling ledge he hangs from; the cave mouth behind/aside him opens onto Kyle's bright glowing fog (use the
   `Atmosphere` prefab), light shafts and drifting haze inside the cave, his silhouette dark against the glow.
5. **Camera**: slow push-in toward him while he sleeps, a slight hold when the eyes open, then tilt/follow down with
   the drop and frame the landing wide enough to show the wingspan. Skippable with A after the first viewing.
6. **Checks** (muted, off-screen, never take focus): rebuild; capture asleep, eyes open, wings spread, mid-drop,
   landing, final stance. Feet touching the ceiling while hanging (measure the gap), feet on the floor after landing,
   no holes or tearing in the wings at full fold and full spread, monochrome max |R−G|,|G−B| = 0, audio level 0.
7. End of round: update `TODO.md` (per-item status, "For Dan to check") and `CLAUDE.md`, commit and push.

### For Dan to check (after Round 4)
- Does the wake-up read as menacing and the drop/landing feel weighty? Is 2.1 m the right size for him?

## Next Steps (Do Not Start Yet)
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
