# Active Development Plan — 7: The Observer

## Standing rules
- 7 never kills · strictly monochrome · non-sexual presentation · 7 is clearly an adult (details in `CLAUDE.md`).
- Per-item status at the end of every round: **done / not done / deferred (why)**. No silent partial completion.
- Short results: ~20 lines, one line per item + its one check, plus a "For Dan to check" list.

## Completed Tasks
- **Round 6 — Chapter 1 "The Moon Prison" — done (2026-11-17), exe-verified off-screen (autopilot playthrough,
  0 soft-locks); Dan to play.** Voiced (78 ElevenLabs lines, telepathic cat), cell → Scree hall → vision → impound →
  liftoff → saucer galley → "To be continued". Per-item status in the Round 6 section below.
- **Round 5 — Kyle's art gallery TV on the saucer — done (2026-11-17), exe-verified off-screen; Dan to browse.**
  `Art/Gallery/` + `gallery.json` (all 24 on), loaded at runtime; TV nook with idle slideshow, browse / full view.
  Full record in `TODO-ARCHIVE.md` → "Round 5".
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
