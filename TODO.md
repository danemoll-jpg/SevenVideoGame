# Active Development Plan — 7: The Observer

## Current Objective — Quick fix QF7: Dan's playtest of 0.10.2 (Dan, 2026-11-18)
Before Round 11 (`Docs/ROUND11.md` stays queued). Commit first. All standing rules apply, **including the 20-minute
testing rule** (no long repeated runs). Dan's screenshot: `Docs/Previews/qf7/dan_galley_0.10.2.png`.

1. **Camera: walls keep getting in the way** in a couple of places in the traversal / climbing stretch (Dan: "the
   Crofting area" = the Lara-Croft-style traversal: climbing, hanging, shimmying, wall climbs, ledges, jumps).
   Wherever a wall comes between the camera and 7 during play, the camera must pull in (spherecast collision) or the
   wall must fade, never block the view. Check every traversal area, not only one.
2. **The shape: one step, from the start.** It is silly to touch the shape, walk to the lock, then walk back to take it.
   New flow: the objective is **"Take the shape"** from the beginning (not "touch it"). She walks up to it, reaches for
   it → the vision plays as she touches it → when the vision ends **she is holding it** (she took it in that moment).
   No second visit, no prompt. Then the lock (item 4).
3. **The shape's glow is far too strong.** After taking it she **puts it in a pocket** — it disappears from view, no light,
   no glow, no bloom anywhere on her (the big white blob in Dan's screenshot is that light). In the galley, on the
   table, keep only thin faint edge lines — no bloom halo, nothing that outshines the scene. Same toned-down look on the Moon pedestal.
4. **The lock should be hidden in the present, revealed by the vision.** Today the lock is plainly visible by the door,
   so the vision teaches nothing. Change: in the present, the lock is **buried behind collapsed rubble** (fallen rib /
   stone chunks in front of it, readable as a collapse); the **vision shows the lock there**, intact, being used. After
   the vision, 7 knows: she **clears the rubble with telekinesis (RT)**, exposing the lock, then uses it to open the door.
   Objective after the vision: "Uncover the lock". Keep it on the right (as in the vision). The door path stays closed until the lock is used (no point of no return).
5. **Galley: she is not sitting in the chair** (Dan's screenshot: her upper body floats behind the chair holding her
   head, her legs are crouched beside it). Fix the seating so she is clearly seated in her chair the whole scene
   (hips on the cushion, back against the backrest, feet on the floor) — check after the new pocket beat too. **Look at
   your own captures before reporting** — this should have been caught. Add a numeric check: hips within 5 cm of the
   seat point every frame she's seated.
   **The cat's bowl:** replace the raised chalice-like feeder with a **plain ordinary cat bowl sitting on the table**.
   Fix the clipping: his muzzle goes into the bowl without passing through its rim or walls (adjust his neck / head
   pose for the low bowl). **When the cat speaks, he lifts his head out of the bowl and looks at her**, then goes back
   to eating.
6. **Emotion without faces (Dan's question).** Close-ups on her face don't work: it's stiff. For this QF:
   - **Eyes and head:** add blinking and eye/gaze movement for 7 (eye bones or eyelid shape keys in Blender — whatever
     the Tripo head allows), small natural head and neck motion while she speaks and listens, looking at the cat / the
     shape / away. Same idea for the cat (blinks, ear and head turns).
   - **Camera:** stop using tight face close-ups. Show emotion with framing instead: medium shots with body language,
     slow push-ins for weight, pulling back for distance, holding on the listener's reaction, and the energy point's
     brightness rising and dimming with her mood.
   - **Facial shape keys — trial only:** in Blender, try a few simple shape keys on her head (eyes close, brows up,
     brows knit, a slight smile, a sad mouth). If they look natural, use them sparingly in the galley; if not, drop
     them and say so — don't ship anything that looks wrong. Report what you tried, with before/after captures.
   - Body-language clips: use the emotional Mixamo clips Dan added to `Art/Mixamo/7/emotion/` (in, 2026-11-18): `7_talking`, `7_thinking`, `7_sad_idle`, `7_nod_yes`, `7_thoughtful_nod`, `7_shake_head_no`, `7_shrug`, `7_look_away`, `7_sit_idle`, `7_sit_talking`, `7_sit_disapproval`, `7_sit_laughing` (talking, thinking,
     sad, nod, shake head, shrug, seated versions).
7. **Checks + release** (20-minute rule): one quick player-style run from the cell to the galley, the vision-chamber
   sequence, and the galley scene. **Look at every capture of the galley and the vision chamber** before reporting.
   Publish **0.10.3**; docs; commit + push; per-item status in "QF7 — status"; captures in `Docs/Previews/qf7/`;
   "For Dan to check (QF7)".

## After QF7 — Dan plays 0.10.3, then Round 11 (`Docs/ROUND11.md`)

## Quick fix QF6: story triggers that never fire + stop the replay grind (Dan, 2026-11-18) — done, status below
Before Round 11 (`Docs/ROUND11.md` stays queued). Commit first. All standing rules apply; muted, off-screen runs.

**What happened (Dan, playing 0.10.1 by hand):** the cat now leaves the cell block, but later **the city reveal (scene
6b) never starts, and neither does the prompt to rescue the saucer** — soft-lock. Dan had to replay the whole chapter
to find it, and it's the same class of bug as before: the autopilot run passes, a real player gets stuck. He does not
want to find another one this way. So this fix is about the cause *and* the method.

1. **Find this bug from Dan's own session first.** Read his logs from the installed game (the launcher install, not
   the build folder): `%USERPROFILE%\AppData\LocalLow\<company>\<product>\Player.log` and `Player-prev.log`, plus his
   save / Continue file — find the last objective, checkpoint and story state he reached and why 6b's trigger and the
   saucer objective never fired. Check whether QF5 caused it (the take-the-shape beat / carrying in her hand / pouch,
   the mirrored mechanism spot, the cat leash, the fade-and-reappear). Report the exact cause.
2. **Audit every story trigger in Chapter 1 (and the saucer/galley)** — every objective, scene start, prompt and
   checkpoint. For each: what must be true for it to fire, and can a player make that false for good (skipping past
   a trigger volume, arriving early or late, from another direction, during another scene, while carrying the shape,
   after a fall reset, after Continue)? Rule from now on: **no trigger may depend on one exact moment or spot.** Every
   trigger fires if the story state says it should and the player is at *or past* it (progress along the route, or
   entering any later area); every objective has a watchdog that re-arms it. Write the audit as a table in
   `Docs/TRIGGER-AUDIT.md` (trigger · condition · how a player could miss it · fallback).
3. **Player-style test for the whole chapter, not just one section.** Extend `-catfollowtest` into a full
   `-playtest` run: virtual gamepad on the real input actions, no autopilot, no teleports, from the cell to the galley
   and "Set course", with varied paths (hurry, wait, backtrack, wander off the route, skip optional bits, fall and
   reset, quit-and-Continue at each checkpoint). At least 25 runs, every one must reach the galley:
   `PLAYTEST: 25/25`. **From now on an autopilot pass does not count as a check for a release — the playtest must pass.**
   Add it to the standing check list in `CLAUDE.md`.
4. **Let Dan jump to any part of the chapter** so he never has to replay everything to test one scene: a **Scene
   Select** page in the pause menu and main menu (cell · escape / cat route · Scree · ruins + vision · lunar door + take
   the shape · city reveal · saucer rescue + beam · aboard · galley · Venus later), each starting with the story state,
   the shape and the cat exactly as they would be there in a real playthrough. (Fine for it to be visible in the
   demo; label it "Scene select".)
5. **Checks + release.** `PLAYTEST: 25/25`, cat-follow 25/25, the full `-chapter1` run, galley run; every Scene Select
   entry starts and plays through to the next one. Publish **0.10.2**; docs; commit + push; per-item status in
   "QF6 — status"; captures in `Docs/Previews/qf6/`; "For Dan to check (QF6)".

### Dan's changes during QF6 (2026-11-18)
6. **The dodecahedron is taken automatically** when 7 walks up to it: no "Take" prompt, no button. Keep the beat in her
   hand and the pouch. Nothing may rely on that prompt any more.
7. **No points of no return** (a rule for the whole game). Never let the player pass a point she can't come back from
   while something the story needs is behind it. Either keep the way forward closed until it's done, or let her walk
   back. Check every one-way drop, ramp, ledge and stair in Chapter 1 and on the saucer, and list them in
   TRIGGER-AUDIT.md.
8. **Testing must never take hours again.** Remove the 25-run playtest and the 25-run cat-follow test from the standing
   checks. All automated testing for a round stays under ~20 minutes: accelerated game speed, at most one player-style
   run, and the trigger-audit rules instead of repetition. Put this rule in CLAUDE.md.

## QF6 — status (2026-11-18): done, exe-verified off-screen; release 0.10.2
Testing conditions for every run:
- Muted: output peak 0.000000, listener 0.
- Minimized and not activated, Below Normal priority, one heavy job at a time.
- Monochrome: max |R−G|,|G−B| = 0 over all new shots.
- Captures and logs in `Docs/Previews/qf6/`.

0. **Checkpoint — done:** `7423bbb`.
1. **Cause — found:**
   - Source: Dan's own 0.10.1 `Player.log` and save, copied to `qf6/dan_0.10.1_session/`.
   - He never took the shape: there is no `dodecahedron ->` line. He walked out through the open lunar door, down to the
     bay floor, and quit there.
   - Why the story stopped: 6b and the saucer objective came after a blocking X "Take" wait, and the door was already
     open.
   - Round 10 built that order. QF5 made it likely: moving the mechanism to the right put her 6.4 m from the pedestal,
     with the door beside her.
   - The cat leash and the pouch were not involved.
   - Details in `Docs/TRIGGER-AUDIT.md`.
2. **Trigger audit — done:** `Docs/TRIGGER-AUDIT.md`.
   - 28 triggers, each with its condition, how a player could miss it, and its fallback.
   - Every trigger fires at or past its place.
   - Every step is declared and watched (`StoryWatchdog`).
   - The flow is split so that 6b and the bay can no longer hang on the take.
3. **Player-style test — done, then cut back at Dan's request:**
   - `-playtest` (`PlaytestRun.cs`): virtual gamepad on the real input actions, real menus, no autopilot, no teleports.
   - The 25-run version was stopped as asked at **22/25 PASS, 0 soft-locks, story watchdog 0** (`playtest_25_stopped_at_22/`).
   - The standing check is now **one** run at 2× speed: `-testspeed 2 -playtest 5 -playtestfrom 4` = **PLAYTEST 1/1,
     soft-locks 0, watchdog 0, 4.6 min** (`playtest_quick/`).
4. **Scene Select — done:** in the main menu and the pause menu, entries cp 0–9 plus "Venus — later" (not selectable).
   Checkpoints are saved for Continue. **SCENE SELECT TEST 10/10** at 2× speed in 5.9 min (`sceneselect/`). The right
   state at each entry is checked: shape on the pedestal or at her hip, slab, doors, cat.
5. **Checks + release:**
   - Playtest 1/1, Scene Select 10/10 (this includes the galley through "Set course").
   - **Repro with the fix off** (`-noqf6fix`, same run): **0/1, SOFT-LOCK**, 7 on the bay floor at (22.45, −10, −73.17)
     with the objective still "Take the shape". This is Dan's 0.10.1 session (`repro_fix_off/`).
   - **Not run (testing rule 8):** the 25-run cat-follow test, the full `-chapter1` autopilot run and a separate
     `-ch1aboard` run. The Scene Select test already plays the galley.
   - Total automated testing this round after Dan's message: 16.3 min of wall-clock time over three runs (4.6 + 5.9 + 5.8; game time ×2).
   - 0.10.2 published (see below).
6. **Automatic take — done:**
   - Walking up to the pedestal (1.5 m) takes it, then the 2 s beat in her hand, then the pouch past the door.
   - The X "Take" prompt is gone; nothing else used it.
   - Scene Select entry 5 logs `dodecahedron -> Hand`, then `-> Hip`.
   - If she goes to the doorway first: telekinesis take (the quick run did this: `pt_take_fallback_telekinesis`).
7. **No points of no return — done:**
   - Found: the doorway behind the lunar door (zone `VisionExit`, the one-way drop into the mining cut) opened with the
     door, before the take. It now opens only after she has the shape.
   - All 13 one-way or two-way crossings in Chapter 1 and on the saucer are listed with verdicts in TRIGGER-AUDIT.md.
     Every other one-way move already has nothing the story needs behind it.
   - The rule is in CLAUDE.md.
8. **Testing rule — done:** in CLAUDE.md (testing rule 4: under ~20 min, `-testspeed`, at most one player-style run).
   - The 25-run checks are removed from the standing checks.
   - New switch `-testspeed x` (1–4, only with `-capture`).

### For Dan to check (QF6)
- Play 0.10.2 the way you did. After the door opens, walk straight to the doorway: she should stop, draw the shape to
  her and pouch it, then the way opens. Or walk to the pedestal: she takes it without a button.
- Main menu → **Scene select**: jump to any part, for example "Lunar door + take the shape" or "City reveal".
- Pause menu → Scene select works mid-game. Quit and **Continue** resumes at the last checkpoint.
- Point-of-no-return table: `Docs/TRIGGER-AUDIT.md`. Tell me if a crossing feels like a trap.

## Quick fix QF5: Dan's playtest of 0.10.0 (Dan, 2026-11-17) — done, status below

Do this **before Round 11** (`Docs/ROUND11.md` stays queued; start it after QF5 is released). Commit the repo as it is
first (it holds new gallery art + reference images added by the chat). All standing rules apply; muted, off-screen runs.

1. **BUG — the cat never leaves the jail, so Dan gets stuck.** Playing 0.10.0 by hand, after the cell rescue the cat stays
   back in the jail / cell block and the game can't go on. Reproduce it with real controller-style play (not only the
   scripted check run — the automated run passed, Dan's play didn't). Fix the cause, then make it impossible to soft-lock
   on the cat anywhere in Chapter 1: if the cat is more than ~6 m from 7, or hasn't moved toward her for ~3 s while she
   moves on, he catches up (trots after her; if blocked, fades out and reappears a step behind her out of camera view).
   No story trigger may wait on the cat's position without that fallback. Keep QF4's rule (the cat never enters the cell).
   Check: run the cell rescue → leaving the block 25 times with varied player paths (walk away fast, wait, backtrack,
   circle) → `CAT FOLLOW TEST: 25/25`, 0 soft-locks.
2. **The lock mechanism must be on the same side in the vision and in the present.** In the vision video
   (`vision_01_door.mp4`) the lock mechanism is on the **right**; at the real lunar door it is on the **left**. Move the
   in-game lock mechanism (and anything that interacts with it: prompt, her reach / telekinesis target, camera framing)
   to the **right** so it matches the vision. Only if that breaks the traversal layout, instead mirror the video
   horizontally (ffmpeg `hflip`, re-encode the same size/format) and say which you did. Capture the vision frame and the
   matching present-day shot side by side for Dan.
3. **Show her taking the dodecahedron with her.** In 0.10.0 it goes into her hand and then simply disappears — Dan:
   "we don't see her taking the dodecahedron with her, which she should." Make it visible: after "Take", hold a short
   beat with it in her hand (camera on her hand / the glow), then she **keeps it visibly with her** — it stays held in her
   hand while she walks out of the chamber, and from then on it rides visibly at her hip (a small glow at her belt / in a
   small pouch, readable from the normal camera) through the rest of Chapter 1 (city reveal, saucer, boarding).
   In the galley she takes it from that same spot before she sets it beside her plate. Keep it ~15 cm, glowing edges.
   Captures: taking it, walking out holding it, at her hip in gameplay, taking it out in the galley.
4. **Checks + release.** Full `-chapter1` run (0 soft-locks), the cat-follow test above, galley run; publish **0.10.1**
   via the launcher; update docs; commit + push; per-item status (done / not done / deferred + why) in "QF5 — status";
   captures in `Docs/Previews/qf5/` and a "For Dan to check (QF5)" list.

## QF5 — status (2026-11-18): done, exe-verified off-screen; release 0.10.1 (`game-1001`, Latest)
Every run muted (output peak 0.000000 / listener 0), minimized-not-activated, Below Normal, one heavy job at a time;
monochrome max |R−G|,|G−B| = 0 over all 305 shots. Captures + logs: `Docs/Previews/qf5/`.
0. **Checkpoint — done:** `77b2c9b` (Kyle's 10 new gallery pieces + 6 reference images, as the chat left them; they are
   also in this build's gallery).
1. **Cat left in the cell block — done.** *Cause:* the cat's traversal-route stops wait for "7 is at X right now"
   conditions (e.g. stop 5 "7 is on top of the bulkhead", stop 2 "the walk-and-talk is over"). A player who hurries past
   (jumps down from the bulkhead before he got up there) makes the condition false for good: he sat on the bulkhead for ever
   (and the next route could not walk him off it). The autopilot always waited for him, so the check runs passed.
   *Reproduced* with a new player-style test (`-catfollowtest n`: virtual gamepad on the real input actions, free camera,
   no autopilot; paths: walks away fast · waits · backtracks past the cell · circles · hurries over the bulkhead during
   the walk-and-talk; hitches every 2nd run): **fix off (`-noqf5catfix`) 2/5** — Dan's path: waited 19.4 s at a stop she had
   passed, 8.7 m behind, still on the bulkhead at the end. *Fix* (`ChapterCat.cs`): a route stop also goes once 7 is level
   with it or past it along the route; plus a **leash** everywhere in Chapter 1 while the player has 7 (never in cutscenes,
   resets, or while she is in her cell): > 6 m behind, or not closing in for 3 s while she moves on → he trots after her;
   blocked (no progress 1.2 s, a ledge or a gap) → he fades out (new `_Fade` dither on `Seven/DissolveLit`, only if seen)
   and reappears a step behind her out of the camera's view, then leads the route on from the first stop ahead of her.
   While leading he may wait up to 14 m ahead (that is the route, not a soft-lock). QF4's rule kept (never in the cell:
   0 frames in every run). Audit: no story trigger waits on the cat without a fallback (all cat waits are `UntilOr`
   watchdogs or bounded `ReadyToTalk`). **CAT FOLLOW TEST: 25/25, 0 soft-locks** (16 runs needed no help, 9 used the
   leash: 14 reappearances, 5 trots; max distance in play 12.3 m on the backtrack path, never > 6 m behind for long).
2. **Lock mechanism on the right — done (moved; the video is unchanged).** The chamber looks toward −z, so the vision's
   "right" is −x: emblem + touch spot mirrored about the door (x 19.35 → 13.65, `MoonLayout.DoorSymbol/MechanismSpot`),
   the two rubble lumps there moved to the other side, the cat's mark and the three cameras of that beat mirrored. Traversal
   untouched (the chamber is symmetric). Side by side: `qf5/vision_vs_present_mechanism_right.png`.
3. **She keeps the dodecahedron visibly — done** (`CarriedShape.cs`). After "Take": a 2 s beat — she raises it to chest
   height and looks at it, camera on her face and the glow, edges brighten; she walks out of the chamber holding it in her
   right hand; past the door (or as soon as she needs both hands) it goes into a small dark-grey pouch at the right of her
   belt and rides there, edges glowing above the rim + a faint white light (checked at the terrace, the ramp, the bay
   floor: 0.01–0.03 m from the belt point; also through the beam boarding). In the galley, seated, she takes it out of the
   pouch, lifts it over the table, her left hand sets it beside her plate (marking down as before), then she picks up her
   fork. Still 15 cm, glowing edges. Side effect: the scripted silence "For a while, neither speaks" now lasts 10.6 s
   (script 6.0 s) because this beat plays inside it — say if it should be shorter.
4. **Checks + release — done.** Full `-chapter1` run: **0 soft-locks, 0 watchdog fallbacks**, 101/105 lines (the 4 = the
   alternate 6b set), speakers 0 % occluded, 130 shots. Galley run (`-ch1aboard`): 0 soft-locks, scene 10 158.5 s, marking
   up 1.00 at the end. Cat-follow test 25/25. **0.10.1 published** via the launcher (component update on 0.10.0; starter zip
   not remade — 0.10.0's starter updates itself).

### For Dan to check (QF5) — `Docs/Previews/qf5/`
- Play the cell rescue → the bulkhead the way you did (hurry, run back, wait) — the cat should always come along; if he
  ever pops in where you can see it, tell me where.
- `vision_vs_present_mechanism_right.png` — vision frame / present day / her hand on the mechanism.
- Dodecahedron (`chapter1/`): `67` in her hand (close-up), `68` walking out holding it, `78`/`79`/`83` at her belt in play,
  `84` full figure with the pouch; galley `100`/`101` (and `galley/04`, `05`) taking it out. Pouch look / side OK?
- Cat test logs: `catfollow/capture_log.txt` (fix on, 25/25), `catfollow_repro_fix_off/capture_log.txt` (fix off, 2/5).

## Round 10 plan (done — status below)
Plan text below as given; status in "Round 10 — status" (added when the round ends).

### The galley scene — the end of Chapter 1 (Dan, 2026-11-17)
Script: `Docs/DIALOGUE.md` **scene 10 "The galley"** — use it exactly. It **replaces** the old scenes 10, 11 and 12 (first
meal, choosing to stay, discussing the vision); remove those from the chapter. Commit the repo as it is first.
All standing rules apply (monochrome, Kyle's fog/glow look, non-sexual presentation).

1. **The dodecahedron becomes a hand-held object she takes with her.** Change Round 9's item 8b: it is now ~15 cm across,
   a **solid pale stone / crystal dodecahedron with thin glowing edges** (Kyle's wireframe look carried by the edges),
   hovering above the pedestal and slowly turning. After the vision and the door scene, a context prompt X "Take":
   she picks it up and keeps it (it stays with her; no inventory UI needed). **One face carries a small engraved
   marking** — the Elarim mark. Kyle hasn't designed it yet: use a simple, clearly placeholder glyph (a small circle
   with three short curved strokes rising from it, like a sprout — "First Garden Age"), on one constant / texture so
   it's easy to swap. Record it as OPEN in `Docs/MODELS-NEEDED.md`.
2. **Voice lines:** generate every new line of scene 10 with ElevenLabs (7 with the expressive Round 7 settings; the cat as
   before). Reuse the existing files for "So what do you eat? Canned food?", "Chicken. But only free-range." and
   "Having experienced confinement, I feel strongly about it." Add them all to `Docs/VOICE-LINES.md`. Respect the
   script's pauses ("For a while, neither speaks", "A pause", "The ship's hum fills the silence") — real silence, a few
   seconds each, with only the ship's hum and eating sounds.
3. **The galley set:** add a small table with two seats in the galley nook (curved, retro-futurist, fits the ship), a
   plate and utensils for 7, a bowl for the cat on the table. **The window shows the Moon from outside, hanging against
   the dark** (the saucer has left — a pale, softly lit Moon in monochrome, Kyle's glow).
4. **Staging (an in-engine cutscene, player not in control):**
   - Open: 7 at the open refrigerator (the existing fridge), the cat **sitting on the table, tail curled around his
     feet**.
   - "I'll see what we have." → cut / ease to 7 seated at the table with her plate; the cat standing over his bowl,
     eating. 7 eats (fork to mouth, small bites), stops, looks at the dodecahedron beside her plate.
   - She moves the dodecahedron between them; the cat steps around his bowl, leans in, tilts his head to study one face,
     then another. "Turn it over" → she rotates it; the camera finds the marked face (close-up).
   - End: she sets it down marking-up, the cat returns to his food, she picks up her fork and eats. Fade softly.
   - **The cat's mouth moves only when he eats:** his head dips into the bowl (the bowl rim hides the mouth) with small
     chewing motions of the head/jaw; no mouth movement while he "talks". Ears: "one of his ears turns toward her" —
     an ear twitch/turn (add an ear bone to each ear if the rig has none).
   - Camera: calm, intimate framing (two-shot across the table, over-the-shoulder, a close-up of the dodecahedron and
     the marking). Frame 7 from the chest up or side-on at the table — no body-focused angles.
   - Animations: 7 uses the existing sit clips (`Seated Idle`, `Stand To Sit`) plus small keyed arm motions for eating,
     moving and rotating the object (layered on the seated pose). If Dan adds Mixamo clips (see below), use them.
     Cat: sit-on-table pose (from his sit clip, tail curled), eating loop, lean/step, head tilt — keyed in Blender if
     the rig lacks them.
5. **After the scene:** a quiet objective appears once — **"Set course for Venus"** — then the player has free
   exploration of the saucer as now (the cat roams, the lap moment, the gallery TV). The chapter is complete; **Continue**
   loads here. At 7's chair, a new context prompt X "Set course" shows a short, honest placeholder: the Moon falls away
   behind the window and a card "Chapter 2 — Venus (coming soon)", then returns to free exploration. Venus itself is
   not built this round.
6. **Story docs:** `Docs/STORY.md` already notes the new chapter end and the next world (Venus, the Elarim). Remove the
   old "discussing the vision / scene 12 placeholder" references from `TODO.md` / `CLAUDE.md`.
7. **Checks** (muted except a brief reported sound check of a few new lines; off-screen; never steal focus): rebuild;
   automated run of scene 10 start to finish — every line plays with its subtitle, the pauses hold, the cat's mouth
   hidden/still except while eating, the dodecahedron is in her hand / on the table where the script says, the marked
   face is visible in the close-up; captures of each beat (fridge, eating, the object between them, the marking
   close-up, "Venus, then", the set-course placeholder); monochrome 0. Publish a release, update `TODO.md` / `CLAUDE.md`,
   commit and push, report in ~20 lines.

### Optional Mixamo clips for Dan (7, FBX for Unity, 30 fps, without skin) → `Art/Mixamo/7/`
**Status 2026-11-17:** `7_open_fridge.fbx` is in (Mixamo "Opening") — use it at the refrigerator. **No eating clip exists
on Mixamo (Dan checked)** — key the eating yourself on top of `Seated Idle`: right hand brings the fork to the mouth in small,
unhurried bites (~4–6 s apart), left hand resting by the plate, slight head dip to meet the fork, a pause with the fork
held when she stops to look at the dodecahedron. Subtle and natural; no chewing face needed.

### Dan's additions during the round (2026-11-17)
8. **Silver saucer exterior** — polished silver upper hull (light-grey base, high metallic + smoothness, soft reflections,
   a bright highlight along the curve), glowing rim + belly glow / tractor-beam iris kept, a slightly darker underside band
   or seam lines, reflection probes where the ship is seen (bay, terrace sighting, liftoff); interior unchanged. Captures:
   bay, terrace, tractor beam, liftoff. Note in `Docs/MODELS-NEEDED.md`: exterior = silver (replaces the dark hull from Kyle's art).
9. **Galley camera direction** — gentle cuts on line changes (not every line); over-the-shoulder both ways (over 7's
   shoulder at the cat; from behind the cat, low at table height, up at 7) as the main shots; close-ups of 7's eyes on
   "Those people…", "But it didn't feel like seeing somewhere new.", "Remembering.", "Maybe they can explain what I saw."
   (eyes and brow, shallow depth of field, slow push-in, energy point visible) and the cat's eyes on "That marking. I've
   seen it before."; two-shot for the quiet beats; faces / shoulders up only. If the Tripo face is soft up close, back off
   and say so. Captures: each over-the-shoulder and each eye close-up.
10. **The logo** — a large "7" with "THE OBSERVER" (small, all caps, wide spacing) under it, Arial Bold, monochrome, as
   images (2048 px wide transparent PNG + white + dark versions; Arial's .ttf never in the repo or the build); on the main
   menu (replacing the title text), the pause menu header and the launcher cover / icon ("7" alone). UI font: Liberation
   Sans Bold (OFL) if text must match. Record: "Logo: '7' + THE OBSERVER in Arial Bold (image); UI font Liberation Sans if
   needed." Capture the main menu with the logo.
- Also (Dan, 21:47): **Docs/DIALOGUE.md scene 10's middle section was rewritten** (after "I don't know."): 5 new lines,
  "There." / "What?" / "They made this?" … removed, the end is now "Venus, then." — built from the new text.

## Round 10 — status (2026-11-17): done, exe-verified off-screen; release 0.10.0
Every run muted (output peak 0.000000 / listener 0), minimized-not-activated, Below Normal, one heavy job at a time;
monochrome max |R−G|,|G−B| = 0 over every shot (154 + 92 + menu). Previews + logs: `Docs/Previews/r10/`.
0. **Safety checkpoint — nothing to commit:** the repo was clean and in sync with origin (ROUND10.md, DIALOGUE.md,
   STORY.md and the open-fridge clip were already in the Round 9 commit `bd8822d`).
1. **Hand-held dodecahedron — done.** 15 cm solid pale stone with thin glowing edges (`ElarimShape.cs`, used on the Moon and
   in the galley), hovers and turns over the pedestal; after the door scene, objective "Take the shape" + context X "Take":
   she reaches, it moves into her hand, then she stows it (hidden; kept from checkpoint 4 on). The Elarim marking on face 0
   = placeholder glyph (circle + three curved strokes, a sprout) `Art/Marks/elarim_mark_placeholder.png`
   (`Tools/Model/make_elarim_mark.py`); swap = replace the PNG or `MoonLayout.ElarimMarkFile`. **OPEN in MODELS-NEEDED.**
2. **Voice lines — done.** Scene 10 = 36 lines (Dan's edited text): the three asked-for takes reused, **33 new
   ElevenLabs takes** in the final scene (7 = Eleven v3 with delivery tags, the cat = v2 as before; 42 generated this
   round, 9 of them dropped by Dan's mid-round edit); `Docs/VOICE-LINES.md` + `voice_lines.json` regenerated (105 lines; old scenes 10–11 takes kept in
   `Unity/Assets/Audio/Voice/_r9_galley_old/`). Real silences measured in the run: neither speaks 6.0 s, "Yes." pause 3.0 s,
   searching 1.8 s, the hum after "Remembering." 4.0 s, watching him 2.2 s (all = script). New SFX `fork_clink`,
   `cat_eating` (eating during the silences), `course_set`. **Sound check** (`-soundcheck galley`): 3 new lines + 2 eating
   sounds, **8.1 s of sound**, peaks 0.55 / 0.71 / 0.69 / 0.38 / 0.20, then muted again → **0.000000**.
3. **Galley set — done** (`FightDemoBuilderGalley.cs`): tulip table + two tulip seats with curved backs, plate with food,
   fork + knife, the cat's bowl on a small raised retro feeder (needed so his muzzle goes below the rim), a round porthole
   in the nook wall: the Moon (pale, softly glowing, cratered grey map) hanging against the dark with faint stars.
4. **Staging — done** (`Chapter1Aboard.Galley()`, in-engine, player not in control): fridge (Dan's `open_fridge` clip),
   the cat sitting on the table, tail curled → fade → 7 seated with her plate, the cat over his bowl eating (head down,
   chewing nods only while eating: 50.9 s; muzzle 0.199 m vs rim 0.212 m; **no mouth movement on his lines**: every cat
   line logs "cat mouth moving: no" except while he is still eating), she takes bites (IK fork-to-mouth on `Seated Idle`),
   stops and looks at the object; ear turn on "One of his ears turns toward her" (**new ear bones `earL`/`earR`** in
   `cleanup_cat.py`); she sets the fork down and places the dodecahedron; he leans closer, tilts one way then the other;
   she rotates it; the camera finds the marked face (facing the camera 0.97); she studies it, thumb on the edge; he
   settles beside the bowl; she sets it down marking up (1.00), "Venus, then.", he eats, she eats, soft fade.
   **Camera (Dan's direction, item 9):** cuts on speaker changes when the shot has held ≥ 3.5 s; over her shoulder at the
   cat and from behind the cat (low, table height) up at her, shallow Bokeh depth of field (her hair / his ears soft);
   two-shot for the silences; eye close-ups with a slow push-in on the four lines (now including "Maybe they can explain
   what I saw.", which Dan's edit added) and the cat's eyes on "That marking. I've seen it before." **Tripo face check:**
   at eyes-and-brow distance (0.30 m) the face texture is soft and the cheek polygons show, so the close-ups are **backed
   off to eyes-and-nose (0.36 m, 26°)**, energy point visible above. Subtitles render on a stacked overlay camera with a
   plain renderer (no fog pass, no depth of field), so they stay sharp.
5. **"Set course for Venus" — done.** Objective once after the scene; free exploration (cat roams, lap, gallery TV);
   Continue loads here. At her galley seat: context X "Set course" → the Moon falls away behind the porthole → card
   "Chapter 2 — Venus (coming soon)" → back to exploring (run: prompt shown, placeholder 1×, then chair + cat on lap).
6. **Story docs — done.** STORY.md already had it; old scene 10–12 references removed from `CLAUDE.md`; DIALOGUE.md is Dan's.
7. **Checks — done.** Full `-chapter1` run: **0 soft-locks**, 27/27 traversal steps, 3/3 fall resets, speakers 0 % occluded
   in scenes 1–8 + 6b, 101/105 lines played + subtitled (the 4 not played = the alternate 6b set), 121 shots. Galley run
   (`-ch1aboard`, final build): 0 soft-locks, scene 10 lasted 154 s. Checkpoint-4 run (city → saucer → galley): 0 soft-locks.
   Opening test not re-run (nothing before scene 6 changed).
8. **Silver saucer — done.** `Saucer_hull` light grey 0.95, metallic 0.6, smoothness 0.9, rim highlight 0.4; underside
   0.74 / 0.45 with a dark band under the rim and seam lines (4 rings + 16 radial panels) + a fine seam on top; glowing
   rim, belly glow, iris + beam unchanged; a realtime reflection probe rides with the ship (`SaucerProbe.cs`, re-rendered as
   it moves: 76 renders in the full run; far geometry replaced by a fog-grey background, so the silver reflects the haze),
   a soft fill light under the disc. A first try at metallic 0.96 read dark grey (it mirrored the dark cavern) — the
   lower metallic + bright probe background fixed it. Captures: bay, low angle, terrace sighting, beam, 7 rising, liftoff.
9. **Camera direction — done** (see 4).
10. **Logo — done.** `python Tools/make_logo.py` typesets "7" + THE OBSERVER from Windows' Arial Bold into
   `Art/Logo/logo_7_observer.png` (2048 px, light grey), `_white`, `_dark`, `logo_7.png`; no font file in the repo or the
   build. Main menu title + pause header use it (`Logo.cs`); launcher cover / icon / poster regenerated from it
   (`Tools/make_launcher_art.py`, launcher rebuilt with the new icon). UI text stays on Unity's built-in LegacyRuntime font
   (Arial-metric, licensed with Unity), so Liberation Sans was not needed. **Logo: '7' + THE OBSERVER in Arial Bold
   (image); UI font Liberation Sans if needed.** Existing installs keep their old launcher icon / cover (the release
   tools only update the game); new players get them in the 0.10.0 starter zip.

### For Dan to check (Round 10) — `Docs/Previews/r10/`
- Galley (`galley/`): `02` fridge, `04` eating two-shot, `05` the cat at his raised bowl, `07` over her shoulder,
  `16` behind the cat, `08/10/11/18` her eye close-ups (backed off to eyes-and-nose — tell me if you want them tighter),
  `15` the cat's eyes, `09` ear turn, `12` the object beside her plate, `13` he leans closer, `14` the marking close-up,
  `19` "Venus, then.", `20` she eats; set course `22`–`25`; pause menu with the logo `29`.
- Dodecahedron on the Moon (`chapter1/`): `53` in the alcove, `65` in her hand.
- Silver saucer (`saucer/`): `19` bay, `20` low angle, `11` terrace sighting, `21` beam on, `23` 7 rising, `27/28` liftoff.
- Logo: `menu/00_menu.png`, `menu/launcher_cover.png`.
- Play: sit through the galley with sound — the silences, the eating sounds, the ear turn; then X "Set course" at her seat.

## Standing rules
- 7 never kills · strictly monochrome · non-sexual presentation · 7 is clearly an adult (details in `CLAUDE.md`).
- Per-item status at the end of every round: **done / not done / deferred (why)**. No silent partial completion.
- Short results: ~20 lines, one line per item + its one check, plus a "For Dan to check" list.

## Completed Tasks
- **Quick fix QF6 — Dan's 0.10.1 soft-lock found (blocking "Take"), automatic take, no points of no return, trigger
  audit + watchdogs, saved checkpoints + Scene select, the 20-minute testing rule — done (2026-11-18), exe-verified
  off-screen; release 0.10.2 (`game-1002`).** Status in "QF6 — status" above.
- **Quick fix QF5 — the cat always comes along (route stops 7 has passed + leash), lock mechanism on the right as in
  the vision, the dodecahedron visibly kept (hand → belt pouch → galley) — done (2026-11-18), exe-verified off-screen;
  release 0.10.1 (`game-1001`).** Status in "QF5 — status" above.
- **Round 10 — the galley scene ends Chapter 1 — done (2026-11-17), exe-verified off-screen; release 0.10.0 (`game-1000`).**
  Hand-held dodecahedron + placeholder Elarim marking, voiced scene 10 (Dan's edited text) as a cutscene with his camera
  direction, the galley set with the Moon in the porthole, "Set course for Venus" placeholder, silver saucer, the logo.
  Status in "Round 10 — status" above.
- **Round 9 — Chapter 1 as the tutorial — done (2026-11-17), exe-verified off-screen; release 0.9.0 (`game-900`).**
  Traversal (jump, grab, hang, shimmy, climb, climb down, let go, wall climb, mantle, crouch), forgiving falls, the
  telekinesis slab, the cat leading the routes, no permanent legend (teaching + context prompts, Controls page, Button
  hints), Kyle's dodecahedron, the lunar door chamber (Dan's addition) and the video vision with the new scene 6 lines
  (Dan's addition). Status in "Round 9" below.
- **Round 7, Round 8, QF3, QF4 — done (2026-11-17), releases 0.8.0–0.8.2.** Laser cell door + hidden release + dark
  energy point in the cell, city reveal (6b), tractor-beam boarding, SFX, re-voiced 7, music, pause menu / Continue /
  Quit, free saucer exploration; saucer look pass, gallery sofa, Ratwing flip re-key, fluid fighting, launcher; phantom
  walls removed, curled lap cat, clean reveal, hidden cursor; opening watchdogs (25/25), clear dialogue cameras, the
  real Scree (1.9 m, tail tucked). Round 7/8/QF3 records in `TODO-ARCHIVE.md`; QF4 record below.
- **Quick fix QF3 — phantom walls, subtitles, curled lap cat, reveal band, excavators, mouse cursor — done
  (2026-11-17), exe-verified off-screen; release 0.8.1 (`game-801`) published.** Status in "Quick fix QF3" below.
- **Round 8 — saucer look + cat rig, gallery sofa/preload, Ratwing drop, fluid fighting, launcher + first release 0.8.0 —
  done (2026-11-17), exe-verified off-screen; Dan to play / install via the launcher.** Status in "Round 8" below.
- **Round 7 — Chapter 1 fixes, city reveal, SFX, re-voiced 7, music, pause menu / Continue — done (2026-11-17),
  exe-verified off-screen; Dan to play.** Per-item status in "Round 7" below; plan in `TODO-ARCHIVE.md` → "Round 7".
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

Round 9 + QF4 status records: `TODO-ARCHIVE.md` → "Archived 2026-11-17: Round 9 + QF4 status records".

## Next Steps (Do Not Start Yet)
- **Scree model** — done in QF4; Scree design approved 2026-11-17 — tail tucked down.
- **First vision contents** — DECIDED (Dan, 2026-11-17): the video `Art/Video/vision_01_door.mp4` (in since Round 9).
- **Fight reset on Backspace / d-pad down** — DECIDED: OK (Dan, 2026-11-17).
- **Hair ties:** DECIDED (Dan, 2026-11-17): **leave the metal cuffs as they are.** (Glowing clips from Kyle's art not planned.)
- **Forehead dot = 7's "energy point"** (Kyle, 2026-11-17). Keep the plain glowing dot. Gameplay: **Kyle's proposal
  (2026-11-17): its brightness shows how much energy she has left for the shield and telekinesis** — bright when
  full, dimming as she uses powers (no separate on-screen bar). Not yet scheduled; don't build it until a round asks.
- **"7" emblem on her back** (Kyle's art) — DECIDED for now (Dan, 2026-11-17): **no emblem.** May revisit.
- **Bubble helmet, hover bike, saucer, cat companion models** — deferred; need concept images and prompts first.
- **Big-head proportion variant** — DECIDED: not pursued for now (Dan prefers normal proportions).

## Technical Notes / Blockers
- `7_normal_v2.glb` is ~59 MB; it must go through Git LFS.
- Mixamo strips materials; reassign materials in Unity (Stage B item 5).
- FBX does not carry PBR metallic/smoothness reliably — set the cuff material up in Unity (Stage B item 5).
