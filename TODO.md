# Active Development Plan — 7: The Observer

## Standing rules
- 7 never kills · strictly monochrome · non-sexual presentation · 7 is clearly an adult (details in `CLAUDE.md`).
- Per-item status at the end of every round: **done / not done / deferred (why)**. No silent partial completion.
- Short results: ~20 lines, one line per item + its one check, plus a "For Dan to check" list.

## Completed Tasks
- **Round 6 — Chapter 1 "The Moon Prison" — done (2026-11-17), played by Dan.** Voiced (ElevenLabs "Seven"/"Cat",
  telepathy treatment, subtitles), cell → escape → Scree → first vision → impound → liftoff → aboard (galley, cat stays).
  Full record in `TODO-ARCHIVE.md` → "Round 6". Dan's feedback drives Round 7.
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

## Current Objective — Round 7: Chapter 1 fixes from Dan's playthrough + sound effects + music
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

## Queued — Round 8 (start right after Round 7, same session; commit + push between them)
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

## Next Steps (Do Not Start Yet)
- **First vision contents** — still undecided (Dan, 2026-11-17). Keep it generic until he decides.
- **Fight reset on Backspace / d-pad down** — DECIDED: OK (Dan, 2026-11-17).
- **Hair ties:** DECIDED (Dan, 2026-11-17): **leave the metal cuffs as they are.** (Glowing clips from Kyle's art not planned.)
- **Forehead dot = 7's "energy point"** (Kyle, 2026-11-17). Keep the plain glowing dot. Gameplay: **Kyle's proposal
  (2026-11-17): its brightness shows how much energy she has left for the shield and telekinesis** — bright when
  full, dimming as she uses powers (no separate on-screen bar). Not yet scheduled; don't build it until a round asks.
- **"7" emblem on her back** (Kyle's art) — DECIDED for now (Dan, 2026-11-17): **no emblem.** May revisit.
- **Saucer look pass** — moved to Round 8 (scheduled).
- **Launcher / auto-update** — moved to Round 8 (Dan: build now; publish every round).
- **Bubble helmet, hover bike, saucer, cat companion models** — deferred; need concept images and prompts first.
- **Big-head proportion variant** — DECIDED: not pursued for now (Dan prefers normal proportions).

## Technical Notes / Blockers
- `7_normal_v2.glb` is ~59 MB; it must go through Git LFS.
- Mixamo strips materials; reassign materials in Unity (Stage B item 5).
- FBX does not carry PBR metallic/smoothness reliably — set the cuff material up in Unity (Stage B item 5).
