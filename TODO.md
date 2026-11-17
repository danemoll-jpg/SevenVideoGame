# Active Development Plan — 7: The Observer

## Standing rules
- 7 never kills · strictly monochrome · non-sexual presentation · 7 is clearly an adult (details in `CLAUDE.md`).
- Per-item status at the end of every round: **done / not done / deferred (why)**. No silent partial completion.
- Short results: ~20 lines, one line per item + its one check, plus a "For Dan to check" list.

## Completed Tasks
- **QF2 + Round 4 — Ratwing cave scene — done (2026-11-17), exe-verified off-screen; Dan to watch.** Build now at
  `Unity\Builds\Seven\SevenDemo.exe`. Ratwing (2.1 m, 55k tris, scripted 38-bone rig with wing spars) hangs asleep,
  wakes, unfurls, drops and lands in a fog-lit cave (third demo-picker entry). Full record in `TODO-ARCHIVE.md` →
  "QF2 + Round 4". Old `Unity\Builds\FightDemo` can be deleted by Dan.
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

## Current Objective — Round 5: Kyle's art gallery TV on the saucer
Commit the repo as it is before changing anything (safety checkpoint).
Dan's idea: a TV on 7's ship where players scroll through Kyle G. Anderson's artwork. Small and playable, like the
other demos. All standing rules apply (monochrome, fog/glow look, non-sexual presentation).

1. **Gallery content folder** `Art/Gallery/`: copy Kyle's images there (start from every `Art/Reference/kyle_*.jpg`) plus
   `Art/Gallery/gallery.json` — one entry per image, in display order: `file`, `title` (short, plain; from the
   reference index), `include` (true/false). Credit line for every image: "Art by Kyle G. Anderson".
   The builder copies the included images into the build (`StreamingAssets/Gallery/`) and the game **loads the list and
   images at runtime**, so Dan can add or reorder art by editing the folder + JSON and rebuilding (document how in
   `CLAUDE.md`; if adding images straight into the built `StreamingAssets/Gallery/` works without a rebuild, say so).
2. **Default `include` values: all `true`.** DECIDED (Dan, 2026-11-17): the gallery shows Kyle's original artwork as he
   drew it — the non-sexual presentation rule governs the game itself (models, outfits, animation, camera), not this
   gallery of his art. `include` stays in the JSON so Dan can still hide or reorder pieces later.
3. **The TV on the saucer:** a lounge nook on the deck with a curved sofa facing a large retro-futurist screen
   (rounded, softly glowing bezel, fits the ship's curves). When nobody is using it, it slowly cycles through the
   included art (cross-fade every ~8 s) so it reads as a lit screen from across the deck.
4. **Viewing mode:** near the screen a prompt shows (A / E: "View art"). The camera eases to face the screen; the image
   fills the screen at its own aspect ratio (no stretching). Controls: left / right (d-pad, left stick, LB / RB,
   ← / →) previous / next with a quick cross-fade, wraps around; **Y / F = full view** (image fills the whole display,
   letterboxed) and back; **B / Esc = leave**. Caption under the image: title, "Art by Kyle G. Anderson", "n / N".
   Legend shows these controls (Xbox glyphs by default, keys after keyboard use).
5. **Monochrome:** show the art in grayscale like everything else (keep saturation −100) — a few of Kyle's pieces carry
   a red "7" logo, which will appear gray. Keep the screen readable (bloom must not wash out the images; exclude the
   screen from bloom or tone it down while viewing).
6. **Checks** (muted, off-screen, never take focus): rebuild; capture the TV across the deck (idle slideshow), viewing
   mode, next/previous, full view, leaving, both legends. Verify the image count matches the included entries, aspect
   ratios are preserved (measure), monochrome max |R−G|,|G−B| = 0, audio 0. Saucer sit/reveal and the other demos still
   work (quick regression).
7. End of round: update `TODO.md` (per-item status, "For Dan to check") and `CLAUDE.md`, commit and push.

### For Dan to check (after Round 5)
- Walk to the TV on the saucer and browse. Does it feel right? Is the order good?
- To add more of Kyle's art: drop the file in `Art/Gallery/`, add a line to `gallery.json`, rebuild (exact steps in `CLAUDE.md`).

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
