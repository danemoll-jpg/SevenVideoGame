# Active Development Plan — 7: The Observer

## Standing rules
- 7 never kills · strictly monochrome · non-sexual presentation · 7 is clearly an adult (details in `CLAUDE.md`).
- Per-item status at the end of every round: **done / not done / deferred (why)**. No silent partial completion.
- Short results: ~20 lines, one line per item + its one check, plus a "For Dan to check" list.

## Completed Tasks
- **Round 5 — Kyle's art gallery TV on the saucer — done (2026-11-17), exe-verified off-screen; Dan to browse.**
  Record below ("Round 5").
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
