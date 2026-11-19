# Active Development Plan — 7: The Observer

## Quick fix QF14: Dan's 0.14.1 play — Europa focus + the menu (Dan, 2026-11-19) — to do
Commit first. All standing rules; the QF13 rule for repeat items (say why the last fix didn't hold, fix the cause, prove
it from the player's camera in normal play, never "done" on a partial fix). 20-minute rule except where proving a repeat
item needs more. Release **0.14.2**; "QF14 — status"; "For Dan to check (QF14)". Dan is concentrating on Europa now.

1. **The main menu only offers the first two chapters** — Europa isn't there even when unlocked. Every unlocked chapter
   must appear (and future ones automatically, from one chapter list — not hard-coded per chapter).
2. **Clean up the main menu.** New layout: **Start new game** (with the existing confirm when a save exists), then **one
   entry per chapter you've unlocked** (Chapter 1 — The Moon, Venus, Europa, …). **Remove the two old test demos** (the
   fight demo and the cave demo from the early rounds) from the menu — they were for testing. Keep Continue (when a save
   exists, at the top), Settings, Controls and Quit — **decided by the chat, Dan can change it.** **Choosing a chapter
   drills down to its sections** (today's Scene Select entries for that chapter: e.g. Europa — arrival, settlement, base,
   escape, temple, debrief) plus "Play from the start"; the list **scrolls with a visible scroll bar** when it doesn't fit
   on screen (controller and mouse wheel; the selected item always kept in view). The pause menu's Scene select uses the
   same chapter → sections lists. Look at captures of every menu page at 16:9 and the Steam Deck's 16:10.
3. **The bike bay — rebuild it to look like Dan's reference (repeat — the fourth try).** Dan: "after all I have said about
   this bike bay it still looks like that. It's awful … that awful gray mush." His screenshot of 0.14.1:
   `Docs/Previews/qf14/dan_bike_bay_0.14.1.png` (a big flat gray slab with the bike's shapes poking through it). **The look
   he wants: `Art/Reference/bike_bay_ref.png`** — build the bay as a real modelled room **sized to fit the bike** (≈ 3 m
   bike + a little clearance), in polished chrome / brushed steel like the reference and the Round 13 console:
   - a thick **rounded chrome frame** around the opening;
   - **curved side walls** with sweeping chrome ribs that arch up into the ceiling, panel seams;
   - **ceiling: a recessed panel with three round lights** in a row (real light sources that light the bay);
   - **storage drawers** low on both side walls; a small **control panel** (round button + two small buttons) on one wall;
   - **floor:** dark polished metal with a **raised guide track / rails** running from the back to the opening, a centre
     stop at the back, and **two chrome wheel / skid cradles** near the opening that the bike sits in;
   - a rounded back wall; reflections and soft highlights (monochrome).
   Build it as a proper hard-surface model (scripted Blender is fine for this) and put it inside the one saucer prefab
   (QF13 16), behind the seamless hatch. When the hatch opens the player sees **this room with the bike sitting in its
   cradles**, then the bike glides out along the track. **No flat slab, no unlit gray, no bike clipping through panels.**
   Compare your capture from the player's B01 / Europa camera **side by side with the reference** before calling it done.
4. **The Europa race can't be retried.** Let her race again: after a finish (win or lose) offer "Race again" right there,
   and the registration arch stays open to start another race any time she's free in the settlement. Keep the best time.
5. **Race camera keeps resetting — hard to race.** After she passes through a ring, the camera turns smoothly to look
   toward **the next ring** (behind the bike, aimed at the next checkpoint); otherwise it follows her steering and never
   snaps or resets on its own. Show the next ring's marker / arrow (QF13 9) during the race.
6. **Inside Tunneler's base the game becomes unplayable — the camera bounces into 7 again and again, then resets, and
   repeats** (Dan had to quit). This is QF13 item 7's new close camera + wall cut-away fighting itself. Find the cause
   (likely the camera pulling in for a wall, then out when the wall is cut away, every few frames; or two camera systems
   both driving it). Make it stable: smooth with hysteresis, cut walls away **instead of** pulling the camera in, a
   minimum distance from her, never snapping back. Prove it: a **normal play walk through the whole base on foot** (dock →
   pressure chamber → control room → holding area → transports → devices → back to the bike) with a per-frame log of
   camera distance and a "jitter" count (direction reversals of the distance within 0.5 s) — **0 bounces**, and a capture
   sequence of the walk.

7. **The guardian's new walk is in:** `Art/Mixamo/elarim/elarim_walk_v2.fbx` (Dan, Mixamo "Catwalk Walk Forward",
   animation only, downloaded on his rigged body; **not "In Place"** — it moves forward, so take the root motion out /
   bake it into the pose and keep driving his movement from code as now, matching playback to his speed). Rebuild so he
   uses it (QF13: the builder picks it up automatically). It's a runway walk — check it reads as **calm and dignified**
   on him, not a hip-swaying strut; if the sway is too much, damp the hips / play it a little slower and say so. Capture him
   walking ahead in "Follow me" (player camera + side), with the cape (no leg through it).

Checks + release: captures for each (menus at 16:9 + 16:10; the bay beside the reference; the race camera through 3
rings; the guardian's new walk; the base walk sequence), publish **0.14.2**; docs; commit + push.


## Quick fix QF13: Dan's 0.14.0 play (Dan, 2026-11-19) — done, status below; release 0.14.1
Commit first. All standing rules. **20-minute rule.** Release **0.14.1**; "QF13 — status"; "For Dan to check (QF13)".
**Second time for items 1 and 3:** QF12's captures said "clean" / "fixed" and Dan still sees the problem in the release
build. Take the capture **from the gameplay camera the player actually has at that moment** (and one from the angle Dan
would see it), compare it with what Dan describes, and say in the status exactly what was changed and what the capture
shows.

**Dan (2026-11-19): several of these have been asked for over several rounds and still aren't right — the Elarim
guardian for at least four rounds (cape, legs, walk), the bike hold bars for three, the encounter, the arms/IK. This round
they get resolved, not "fixed in the log":** for each repeat item (1, 3, 15, 16, 17, 19) first write in the status why the
previous fix didn't hold (what the earlier check measured vs what Dan saw), then fix the cause, then prove it with
captures / short capture sequences from the player's camera in normal play. If something genuinely can't be solved this
round, say so plainly with what's left and the options — never mark it done on a partial fix.

1. **Bike hold: still white bars when it opens.** Dan wants **an empty opening, no bars of any kind** — black (QF11) or
   white (QF12). Find which objects draw as horizontal bands across the opening from the player's view (the hold's floor
   / ceiling / back wall, the hatch leaves, a lit strip, the glow material) and remove or hide them so the opening shows
   only the bike coming out of the hull. On Venus (B01) and Europa.
2. **Venus B03: the walkway breaks too late.** It waits until she's on top of it and can't move forward. It must break
   **ahead of her, while she's still approaching** (trigger when she's well short of it — e.g. ~25–30 m out — on any
   approach line, never with her on it), so she watches it fall and brakes before the gap. She must never be left standing
   on or stuck at the broken edge.
3. **The guardian: the cape hangs too low, and he still uses the awful walk.** (a) Re-seat the cape on the Mixamo body at
   the height it had on the original model (top at the shoulders, hem where Kyle's / the reference model has it — compare
   with `Art/Source/elarim/elarim_v1.glb`); it shouldn't drag or sit low on his back. (b) The walk: find out whether the
   clip itself is bad or the retarget breaks it. **If Dan has added `Art/Mixamo/elarim/elarim_walk_v2.fbx`** (a new Mixamo
   walk downloaded on his rigged body), use it; otherwise fix the retarget / pick the best walk available and say so.
   He should walk with dignity: upright, steady, natural steps, arms relaxed.
4. **Field Guide: Europa shows up under the Scree before you've been there.** Entries need text that grows with the
   story: add optional `more` paragraphs to an entry, each with its own `unlock`, shown after the main text only once
   unlocked. Scree: the Moon text from `ch1>=7`; the Europa sentence as a `more` part at `europa>=5`. **Tunneler:** unlock
   with Moon-only text after Chapter 1 (she was his prisoner — when the story has shown him; `ch1>=N` of the first time he's
   seen / named), and his Europa part as a `more` at `europa>=4`. Check every entry for the same mistake (nothing mentions a
   place or event the player hasn't reached). Mirror in `Docs/FIELD-GUIDE.md`.
5. **Europa: the bike must come out of the bike hold, not the beam** (spec corrected, `Docs/CHAPTER_EUROPA.md` sections
   2 and 16): the hold opens and lowers the bike out (like Venus B01), 7 comes down in the beam; going home, the bike rises
   back into the hold and it closes seamlessly; 7 boards in the beam. Rule for every world — add it to the "Saucer — fixed
   design" rule in `CLAUDE.md`: **the bike always uses the bike hold; the beam is only for 7 (and the cat).**
6. **The bubble helmet can't be seen** under water (only against a dark background). Make it read clearly in the bright
   misty water while staying monochrome: a bright rim / fresnel edge on the glass, soft highlights and a reflection streak,
   a slight tint or distortion of what's behind it, and a solid collar ring at her neck where it seals. Check captures in
   the bright chamber, the settlement and the base.
7. **Tunneler's base entrance is hard to get through — walls block the view.** Make the access bay → pressure chamber →
   first corridors readable: the camera never ends up behind a wall (use / extend `CameraOccluders` — fade or hide what's
   between camera and 7, or pull the camera in), doorways wide enough, the way through obvious. Capture the whole walk in.
8. **Objectives must say where to go.** "Explore beneath the ice" says nothing (Dan: "what exactly?"). Go through every
   Europa objective (and check Venus too) — each tells the player **what to do and where**: e.g. "Ride down into the great
   chamber" with the guide marker on, then "Follow the lights down toward the settlement", etc. Check the objective text
   fits on screen without being cut off.

9. **The guide light can't be seen — Dan got lost in the first descent** (rode away from the entrance, saw no light at
   all, the hint text didn't help). In every Europa riding section: the next point is **always findable** — an on-screen
   marker / arrow at the screen edge pointing to it when it's off-screen or hidden in fog (with distance or an up / down
   cue, since she swims in 3D), a much brighter, bigger guide light that cuts through the fog, and the guide shown from
   the start of each section (not only after the 45 s hint). If she's far off course, the hint says which way ("Turn
   around — the light is behind you and below"). Shorten the skip offer (now 120 s without progress) to ~60 s in riding
   sections.

10. **Europa clean-up — lots of wall clipping.** Go through the whole chapter (descent, settlement, race, approach, base on
    foot, escape, temple, way home) and fix every place where the bike, 7, the camera, the Cetaceans or vehicles pass through
    walls, ice, pillars or structures: real collision for the bike under water (it slides along surfaces, never through),
    the camera kept out of geometry (`CameraOccluders`), swim paths for the Cetaceans and the settlement vehicles that stay in
    open water. **The Scree escape transports and Tunneler's escape craft** must leave through proper openings / docking
    passages without going through walls, and look like believable vessels (if they're plain boxes, give them a real
    hull shape — rounded, a cockpit / lights, a visible thruster glow — built from simple shapes in Kyle's style). Use the
    walk-blockers / overlap scan from QF12 plus captures along every route.
11. **Tunneler's drill head must spin like a drill when he talks** — continuous rotation around its own axis, faster on
    louder syllables, spinning down when he stops (see the Tunneler note in `Docs/MODELS-NEEDED.md`), not a
    twist back and forth. Same everywhere he appears.
12. **Bike controls under water: up / down on the stick, like left / right.** Left stick X = turn, **left stick Y = pitch
    up / down** (nose up = climb, nose down = dive — like flying), RT = throttle, LT = brake. Remove A / B for ascend /
    descend. (Keyboard: W/S pitch, A/D turn, the existing throttle / brake keys.) Update the tutorial prompts and the
    Controls screen. Default: **push forward = dive, pull back = climb** (like flying), with an "Invert up / down" option in Settings.
13. **The bike is way too slow — make it fast EVERYWHERE (Dan: "faster everywhere" — Venus on land too, and any later
    world)** so getting around never feels like a slog. On land (Venus): much faster top speed and acceleration, tuned so
    the paths, the bridge and the turns still work at speed. Under water: top speed about
    **3× today's** (8.5 → ~25 m/s), quick acceleration, responsive turning, with a sense of speed (stronger bubble trail,
    streaking particles, a little camera FOV push / pull back at speed, the hum rising). Retune the chapter around it: the
    race, the routes, the escape (item 14), and where lines play during rides (don't let her outrun her own dialogue —
    trigger lines by progress). Keep the 2.4–3 m bike readable at speed (the camera a bit further back and lower).
14. **Rebuild the escape ride as a proper movie chase** — Dan: "her escaping on her bike with the explosion behind her
    and her driving away, just like in the movies; the way it happened you couldn't even tell what happened." Make it
    unmistakable:
    - **The detonation is shown:** she triggers it, a wide shot of the base — the first blast erupts (huge white flash, a
      rolling boil of bubbles and debris, the structure buckling), the shockwave hits the camera — then cut to her.
    - **The chase:** she rides flat out down the tunnel with **a wall of explosion visibly racing up behind her** —
      white-hot glow, churning bubbles, debris tumbling, chunks of tunnel collapsing in its wake — always in the frame
      behind her. Use real chase framing the player can still steer in: a low camera in front of her looking back (her
      face-on, the blast filling the tunnel behind), side tracking shots, the normal chase camera for the steering
      stretches; short cuts between them, never confusing about where she's going.
    - **Readable obstacles** (falling supports, a narrowing gap) lit by the blast, with Panther's calls; she dodges them at
      full speed (the new fast bike).
    - **The money shot:** she bursts out of the tunnel mouth into open water a split second ahead of the blast; the
      fireball / shockwave erupts out of the opening behind her (slow motion for a moment is fine), the pressure wave
      catches and spins the bike, she wrestles it back under control — the player sees her recover (spec section 12).
    - Then the stillness of section 13. Keep it fair: a hit means retry from the start of the chase, but the chase should
      feel dangerous, not punishing. Sound: a deep boom, the roar chasing her, the bike screaming, silence after.

15. **Arms stuck in IK poses (Venus start + after the Europa debrief).** (a) At the beginning of the Venus chapter 7
    stands with one arm bent behind her as if already gripping the bike. (b) After the Europa debrief, when she gets up
    from the chair, her arms keep reaching to pet the cat no matter how far she walks — the same odd arm pose. **Fix the
    cause for all of them at once:** every hand / arm IK (bike grips, petting, holding a shape, table, console) must have an
    owner and be **released when its moment ends** (dismount, stand up, scene end, checkpoint load, Scene Select start), and
    a safety net: any hand IK whose target is out of natural reach, or whose condition (seated / riding / holding) is no
    longer true, blends to zero within a fraction of a second. Check every start point (each chapter start, Continue, every
    Scene Select entry) and every stand-up / dismount: arms hang naturally.
16. **One saucer, everywhere — stop changing it per world.** Dan sees different saucer models on different planets even
    though `CLAUDE.md` says "one shared exterior". Make the saucer **one prefab asset** (exterior, belly beam with its iris
    hatch, the bike hold + its seamless hatch, the interior link) built once and **instanced unchanged** in the Moon, Venus,
    Europa and every future scene — no per-scene building, sizes, hover tweaks to the model or materials (only where it
    hovers may differ). **Always the beam, always the bike hold.** Add an automatic check to the build that fails if any
    scene's saucer differs from the prefab (meshes, scale, materials, beam + hold present). Update the "Saucer — fixed
    design" rule to say this.
17. **The guardian — finish it this round** (with item 3: cape height + walk). He has been wrong for four rounds. Besides
    item 3: capture a short sequence of him **standing, talking, walking ahead ("Follow me"), kneeling, getting up**, from
    the player's camera and from the side, and look at it frame by frame: no leg through the cape, cape at the right height,
    natural dignified walk, knees and arms bending naturally. If the cape still can't be made to behave on the Mixamo body,
    make it simpler rather than wrong (e.g. shorter, stiffer, or a separate cloth piece hanging from the shoulders that
    can't reach the legs) and say so.
18. **Cut the bike talk aboard the saucer after Europa** (the bike isn't visible there): remove `EUR_16_01`–`EUR_16_03`
    ("Were you waiting for the bike?" / "Someone had to inspect the damage." / "No damage."). The reunion stays: Panther
    waiting near the boarding area, she kneels beside him, he presses his head against her hand, a quiet pause, then
    Panther's **"Good."** (`EUR_16_04`), then hold. Spec + `Docs/EUROPA-LINES.md` + the dialogue data updated (done in the
    spec by the chat).

19. **The wall and the cube on the way down (Venus) are STILL there — Dan's screenshot:
    `Docs/Previews/qf13/dan_wall_and_cube.png`.** QF12 guessed it was dust particles; it isn't. In the screenshot (objective
    "Find the source of the signal", 7 going down the stairs): a big dark curved wall fills the view ahead and to the left
    of the stairs, and **a glowing white cube floats in mid-air** in front of it. (QF12 called that cube "the tunnel lamp".)
    Fix: (a) find what that curved surface is (a tunnel wall segment / a misplaced or oversized tunnel piece / the camera
    inside the tunnel wall) and make the way down an open, readable tunnel — the stairs lead somewhere you can see; (b) the
    floating cube reads as a mystery object (and looks like the Venus record cube): replace it with a lamp that reads as a
    lamp — fixed to the wall or ceiling, an Elarim-style lantern / crystal, not a floating cube — or remove it. Reproduce
    Dan's exact view (same spot, same camera) and compare your capture with his screenshot. (Also visible in it: her arm
    bent behind her back — item 15.)

Checks + release (20-minute rule): targeted captures per item (gameplay-camera angle for 1, 2, 3, 6, 7, 9, 10, 14, 15, 17, 19; a short capture sequence / GIF of the whole escape), publish
**0.14.1**; docs; commit + push.


## QF13 — status (2026-11-19): done, exe-verified off-screen; release 0.14.1 (`game-1401`)
Captures + sheets: `Docs/Previews/qf13/` (`item<NN>_*.png`; `item19_dan_vs_0.14.1.png` = Dan's screenshot beside the same
view now). Every repeat item below says why the last fix didn't hold, what was changed, and what the capture shows.
New check tools: `ChapterCapture.ShotFrom` (the same instant from a second, fixed angle without moving the player's
camera), the guardian sequence (`GuardSeq`, player camera + side view per frame, with walk / cape numbers), the escape
capture sequence (`qf13_14s_*`, every 0.4 s), the base walk-in sequence (`qf13_07_walk_in_*`).

1. **Bike hold: no bars — fixed (repeat).** *Why QF12 didn't hold:* QF12 made the hold's surfaces one even glow, but
   with a lit material, and checked it from a framing that hid the rest. From the B01 camera the player actually has
   (rim height, 6 m out), four things still drew bands: the belly glow ring ran under the open sector (a white line
   across the opening's bottom), the seam outline stayed lit (emission 1.2) the whole time it was open, the lit floor /
   ceiling caught reflection at grazing angles, and the panel (scaled toward the ground) hung 5 cm below the
   neighbouring hull once slid round. *Now:* the hold's inside is one flat **unlit** tone (`Seven/HoldInterior`, gray
   0.2: reads as a recess, not a panel), the belly ring's arc under the hold rides on the panel, the seam goes dark
   before the panel moves, and the panel sinks about the hull's mid-plane (`SaucerHold`, tuck 0.97) so it slides round
   *inside* the hull. Captures from the player's B01 camera: `item01_hold_open_player_view.png` (an empty dark opening
   with the bike, no lines), `item01_hold_bike_out.png`; before: `item01_hold_before_0.14.0_player_view.png`. Europa
   uses the same hold (item 5). *Still visible while it opens (≈0.9 s):* the panel itself sliding across — a door
   moving, not a band of the hold.
2. **B03 walkway breaks ahead of her — fixed.** Cause: the trigger (z > 96, "at or past") was only checked after B02's
   lines had finished, while she kept riding — so it fired with her on the walkway. Now `CollapseWatch` runs from the
   start of the ride and fires on distance short of the gully (it crosses the whole valley, so any approach line):
   ≤ 30 m or < 3.2 s away + 8 m; she brakes at once; if she ever ends within 6 m of the edge she glides back to 9 m.
   Log at the new land speed: `fired with her 73.2 m short of the gully at 20.4 m/s … she stopped 14.1 m short of the
   broken edge`. `item02_*`.
3. **The guardian's cape + walk — fixed (repeat, with 17).** *Why the last four rounds didn't hold:*
   - The cape logic never had any vertices: the guardian's FBX was imported **without Read/Write**, so `ElarimCape`
     found 0 cape vertices in the game — it never swung clear of a leg, and every "leg contact 0.0 cm" line in QF10 /
     QF12 measured nothing.
   - One rigid bone per half: when it did move, the whole cape moved as a board.
   - The walk: Dan's `elarim_walk` clip itself leans forward with a hand held out palm-up (measured in Blender: hand
     0.27 of his height forward, 0.24 out), and it was played at a stride of ~3.7 m/s while he moved at 1.5 m/s (his feet
     skated, steps hurried).
   *Now:* Read/Write on; the cape re-rigged in two parts per half (`cleanup_r11.py … elarim_rig`: CapeA/B chest→hips,
   CapeA2/B2 hips→hem) so the upper part hangs on his back and only the lower part flares over the legs; it hangs with
   gravity whatever the spine does, rests on the ground when he kneels (no rod), settles back quickly; re-seated to the
   model's height every frame. Measured in game vs the model: **top 0.25 m below the shoulders (model 0.25), hem 0.14 m
   above the ground (model 0.14)**, leg contact 0.0 cm (now a real measurement). The walk: no `elarim_walk_v2.fbx` was
   in the repo, so he uses **7's Mixamo walk** retargeted to his avatar (upright, hands 0.20 / 0.18), at a calm 2 m/s
   with the playback matched to his speed from the planted foot (0.69×) and the arms relaxed. If Dan adds
   `Art/Mixamo/elarim/elarim_walk_v2.fbx`, the builder uses it automatically. `item03_17_guardian_sheet.png`.
4. **Field Guide grows with the story — done.** Entries have optional `more` paragraphs with their own unlock
   (`ShipConsole.GuideMore`): Scree (Moon text `ch1>=7`, Europa part `europa>=5`); Tunneler — Chapter 1 never names or
   shows him, only his mining in the city: unnamed entry from the city reveal (`ch1>=6`, "Whoever runs the prison"),
   named when the keeper names him (`europa>=3`), his base / screen at `europa>=4`, the escape at `europa>=7`. The same
   mistake was in five more entries and is fixed the same way: the guardian (the "driven" plea `HelpAccepted`, the record
   `RecordReceived`), the Elarim (the marking `GuardianMet`, the "histories" quote `HelpAccepted`), the burrowing
   creatures (going home `ThreatStopped`), the Cetaceans ("teach their history" `europa>=9`), the keeper (the record
   `europa>=8`). Mirrored in `Docs/FIELD-GUIDE.md`.
5. **Europa: the bike uses the bike hold — done.** The hold opens, the bike glides out and settles on the shelf, the hold
   closes seamlessly, then 7 comes down in the beam; going home the bike rises back into the hold. Rule added to
   CLAUDE.md "Saucer — fixed design". `item05_*`.
6. **Bubble helmet visible — done.** `Seven/Bubble`: a bright fresnel rim, two highlights + a reflection streak (placed in
   view space, always facing the camera), a faint veil over what is behind; a solid collar ring at the neck.
   `item06_helmet_bright_water.png` (bright water), the base walk-in sheet (indoors).
7. **Base entrance readable — done.** In the base the camera stays close (2.7 m) and high over her shoulder, keeps
   behind her walking direction, and any wall / door / barrier between it and her head is cut away while it is in the
   way. Measured: `10 645 frames inside, walls cut away in 2 219, her head hidden behind something still drawn in 14`
   (0.13 %). The whole walk in from the dock: `item07_base_walk_in_sheet.png`.
8. **Objectives say where — done.** Every Europa objective (and Venus's) now says what and where, e.g. "Ride down the
   ice passage - follow the white light into the water", "Ride down into the great chamber under the ice", "Follow the
   lights down toward the settlement", "Ride to Tunneler's base - follow the pipes down the industrial tunnel". Two lines
   at most in the objective box. Venus keeps each objective's beginning (the check drivers match on it).
9. **Guide light always findable — done.** A much bigger, brighter guide light (core + a wide halo that keeps its size
   on screen, light 6 / 40 m) and an on-screen marker: a diamond with distance (and ↑ / ↓) on it, an arrow at the screen
   edge when it is off screen or behind her. Off course (30 m worse than her best) the hint says which way at once
   ("Turn around - the light is behind you (21 m)"); riding hint after 25 s, skip after 60 s (was 120). `item09_*`.
10. **Europa clean-up — done (with limits).** The bike now has real collision under water: a 1 m sphere swept against
    the level's meshes, sliding along (`HoverBike.SlideSwim`); swim paths checked at load (1 settlement-Cetacean loop
    segment crossed geometry and was lifted clear; vehicles 0); the camera in the escape is kept inside the tunnel. The
    transports and Tunneler's craft are real vessels (rounded hull, canopy, running lights, thruster ring + glow) and
    leave through proper passages: a 13 m launch tunnel in the transport dock's west wall, a lit docking passage north of
    his dock. `item10_vessels_sheet.png`. *Not covered:* meshes larger than 60 m (the chamber shell, the temple hall,
    the escape tube) are kept by the swim volumes, not by the sweep; I did not ride every route by hand.
11. **Tunneler's drill spins — done.** A continuous rotation about the cone's own axis, faster on louder syllables,
    spinning down after he stops (never snapping back): `28 full turns, top speed 1123 deg/s`.
12. **Bike controls under water — done.** Left stick X turns, **Y pitches (push forward = dive, pull back = climb)**,
    RT forward, LT brake; A / B no longer ascend / descend. Keyboard: A/D turn, W dive / S climb, Shift or right mouse
    forward, Q brake. Settings → "Invert up / down (bike under water)". Tutorial prompt + Controls page updated.
13. **Faster everywhere — done.** Land: 24 m/s (was 11), accel 13, brake 20, give-way look-ahead scales with speed.
    Under water: 25 m/s (was 8.5), accel 11, turn 95. Speed streaks, more bubbles, FOV +9° at speed, camera a bit further
    back and lower; the race rivals retuned (19-23.5 m/s). Logged: `speed 25.0 m/s … camera speed push 1.00`.
14. **The escape as a movie chase — done.** The detonation is shown (the wide down the approach: white flash, a boil of
    bubbles and debris, the structure buckling, the shockwave on the camera); the chase: a white-hot blast front fills
    the tunnel and races up behind her (19-47 m behind, in the frame or lighting it in 572 of 1605 frames), supports fall
    in ahead of her lit by it, Panther's calls; framings cut front (looking back at her with the blast behind) → chase →
    side → chase → front → chase, all kept inside the tunnel; the money shot head-on at the mouth: the fireball fills
    the opening as she bursts out (slow motion), the pressure wave spins the bike, the camera stays on her as she
    recovers. A hit = retry from the chase's start. Sequence: `item14_escape_sequence_sheet.png`, `item14_burst_out_sheet.png`.
15. **Arms in old IK poses — fixed (repeat).** *Why:* QF12 fixed *where* the petting hand went, not *when it lets go*:
    the Europa debrief's stroke loop was never stopped (after standing up her hand kept following the cat), and hand
    weights set by a beat that a skip, a scene load or a checkpoint interrupted (B01's hand on the fairing, C04's brace)
    stayed on. *Now:* every stand-up and dismount releases all hand IK (`SevenTableIK.ReleaseAll`), the stroke loop is
    stopped, and a safety net blends any hand to zero within ~0.25 s when its target is out of reach or she walks and the
    target stays behind (logged: e.g. `let go of the left hand: target 1.37 m from the shoulder`). After the Europa
    debrief: `hand IK weights left 0.00 right 0.00`, `item15_after_debrief_walking_arms_free.png`; on the Venus stairs her
    arms hang naturally (`item19_*`).
16. **One saucer everywhere — done (repeat).** *Why:* every scene built its own saucer from the shared functions, each at
    its own height with its own material loads; nothing checked them. Now `Assets/Saucer/Saucer.prefab` is built once per
    build and every scene places an unchanged instance; the build fails if not: `SAUCER CHECK (QF13 16) Chapter1 / Venus /
    Europa: the one prefab, unchanged (32 renderers; beam + hold present)`.
18. **Bike talk aboard cut — done.** EUR_16_01..03 removed (spec, `make_europa_lines.py` keeps "Good." as EUR_16_04,
    its voice file, the three old mp3s deleted); the reunion plays the kneel, his head against her hand, the pause, "Good.".
19. **The wall and the cube on the way down — fixed (repeat).** *Why QF12 missed it:* it guessed dust particles. The
    "curved wall" was the hill's ground mesh: it dipped into the root pit and climbed straight back up across the pit's
    bottom end — a smooth steep face in front of the tunnel mouth. The "cube" was `Pit_Lamp`, a 0.3 m glow box hanging in
    the middle of the pit. Now the ground cells across the pit's edges are left out (43), a rock face stands over a clear
    5 × 5.2 m tunnel mouth, and every root-tunnel lamp is an Elarim crystal lantern on a root bracket fixed to the wall or
    ceiling. Dan's exact view reproduced: `item19_dan_vs_0.14.1.png`.

**Checks.** Builds pass the saucer check in all three scenes. Monochrome: 0 coloured pixels in every sheet. Audio
muted (peak 0) in every run. **Testing time: about 32 min of game time — over the 20-minute rule by ~12 min,** spent on
the repeat items: the guardian needed four runs (the cape vertex bug and the reference-frame bug only showed in the game),
the escape five (cameras outside the tunnel, the burst framing). **Not run this quick fix:** a full player-style run
of Chapter 1 / Venus / Europa (each alone is 5-12 game-min) — every changed beat was captured by a targeted run instead.

## For Dan to check (QF13)
- Venus B01 / B07 and Europa: the hold opens as an empty dark opening with only the bike — no white or black lines.
- Venus B03: the walkway breaks well ahead of you, at full speed too; you stop short of the gap.
- The guardian: standing, talking, kneeling, getting up, walking ahead — cape on his back at the model's height, flowing
  over his legs (never through), an upright calm walk (7's walk for now — drop `elarim_walk_v2.fbx` in to replace it).
- Field guide: nothing about Europa until you've been there (Scree, Tunneler, Cetaceans, keeper; Venus entries too).
- Europa: the helmet in bright water; the base entrance (walls get out of the way); the guide marker / arrow and the
  "which way" hint if you ride off; the new stick pitch (push forward = dive) and Settings → Invert; the speed.
- The escape: detonation, the blast chasing you down the tunnel, the burst out of the mouth, the recovery.
- Aboard after Europa: no bike talk; after the debrief, walk away — arms hang free. One saucer on every world.
- The way down on Venus: an open tunnel mouth with a lantern, no wall, no floating cube.


## Quick fix QF12: Dan's 0.13.0 play (Dan, 2026-11-19) — done, status below; release 0.13.1
Commit first. All standing rules (monochrome, Kyle's fog/glow, non-sexual presentation, no exact-spot triggers, look at
your own captures). **20-minute rule, strictly** — Round 13 ran ~57 game-minutes; use targeted switches and captures,
one release run at the end. Release **0.13.1**; per-item status in "QF12 — status"; "For Dan to check (QF12)".
Dan reports what he SAW in the release build, so "done in the log" is not enough: for every item, look at an in-game
capture from a normal play (not only a test switch) before calling it done.

1. **Chapter 1, after the first vision: 7's body shakes violently until she finishes talking** — only her, not the
   camera or anything else (Dan: "like she is stuck in an animation"). Likely her animation / IK flipping every frame:
   the Animator toggling between two states or retriggering a clip each frame (e.g. the vision exit and the dialogue /
   reaction both driving her), the Round 13 reaction body cues or `SevenTableIK` fighting the standing dialogue pose, or
   an IK target jumping each frame. Log her Animator state + IK weights per frame through that exchange to find it, then
   fix so she stands calm through it. Check the same moment after the Venus vision.
2. **On the Moon, the beam's door (the opening on the saucer's underside) is far below the ship.** Since the beam is
   shared (`BuildSaucerBeam` / `SaucerBeam.cs`), its door must sit flush on the saucer's underside on every world — place
   it from the saucer's actual hull (measured), not a fixed height. Check the Moon and Venus captures.
3. **The bike hold opening: the black bands became white bands — remove them entirely.** When the hatch opens the hold
   reads as an **empty, softly lit opening** (just the bike), no floor / ceiling slabs or strips visible from outside.
4. **The guardian's legs still go through his cape — and his legs move oddly (Dan: part of the cause).** So fix the
   body, not just the cape: **use the Mixamo route now.** Dan is uploading `Art/Export/elarim/elarim_body_for_mixamo.fbx`
   to Mixamo's auto-rigger and saving it ("FBX for Unity", with skin) as `Art/Mixamo/elarim/elarim_body_mixamo.fbx`.
   When it's there: use it as his body (Humanoid), retarget his existing clips (idle / talk / point / walk / kneel / sword
   idle / carry / lift) onto it, and re-attach the cape (`elarim_cape_part.fbx`) on its own bones that follow the hips and
   trail behind the legs, with runtime push-out against the thighs and calves. If the file isn't there yet when you
   reach this item, do items 5–11 first and come back; if it's still missing at the end of QF12, **don't stop** (Dan is asleep): do the best cape fix you can on the current rig, mark the Mixamo step open in the status, and go on to Round 14. Measure the
   cape in the real scenes and camera angles (standing, walking ahead in "Follow me", kneeling) — it must never show a
   leg through it — and look at his legs while walking: natural steps, knees bending forward, no odd twisting. Also
   still open from Round 13: **his kneel in C03 doesn't show** — fix (the new rig may solve it).
5. **The bridge slab must come from the quake.** Right now the piece she lays across the gully is just lying there,
   which makes no sense. The slab she lifts with telekinesis is **a broken piece of the walkway that fell during the
   tremor's collapse (0k)** — it falls with the rest into the gully, lies there broken and dusty, and that's the piece she
   raises and lays across. No separate, ready-made slab anywhere before the collapse.
6. **The cube is missing in the temple — the pedestal is empty.** The original Venus cube must sit in its protected
   structure the whole time (C04's rattling cage, C10: "The original remains visible"); she is given a small copy.
7. **Venus, on the way downstairs: a wall that shouldn't be there, with a cube in front of it.** Find and remove the
   stray wall so the way down is open. (The cube there may be item 6's cube placed in the wrong spot — check; if so,
   move it back to the pedestal, otherwise remove it.)
8. **The boss fight: still nobody attacks — no worms, and no Wormmaster now either. No challenge at all.** Round 13
   logged pulses / lunges / taunts, but Dan saw none of it in the release build. Find out why from a **normal play from
   the main menu / Continue** (not `-r13 encounter`): are the creatures and the Wormmaster spawned, visible, above
   ground, in front of the camera? Does the harass loop only start under a test switch, or after a state a real player
   never reaches? Then make it plainly visible and a real (fair, nonlethal) first-boss challenge: the Wormmaster on
   screen, worms surfacing and lunging at her, shielded devices, knockbacks. Prove it with captures from the normal play.
9. **Arriving on Venus, you briefly see her standing on the ground before the title card.** The title card (or a black
   frame) must cover from the very first frame; she appears only when the arrival scene begins.
10. **Petting Panther: her hand is too low** — she strokes at her knee, not the top of the cat. Every petting moment (the
    chair / lap scenes C11 → C13 and any other) puts her hand IK target **on the top of his back**, measured from his
    actual pose and size, with the stroke moving along his back.
11. **Back on the ship after Venus they should already be in the chair.** After the beam up, C11 starts with 7 seated in
    the pilot's chair and Panther on her lap — no walk to the chair or hop up first.

Checks + release: targeted captures for each item (a normal-play capture for 1, 4, 8, 9), the release run, publish
**0.13.1**; docs; commit + push.


## QF12 — status (2026-11-19): done, exe-verified off-screen; release 0.13.1 (`game-1301`, Latest, published)
Captures + logs: `Docs/Previews/qf12/` (`qf12_items_sheet.png` = one frame per item). New check tools:
`-menupick "Label|Label|..."` (a run that starts in the real main menu: `MenuPilot` presses through it on a virtual
gamepad), `-stopafter <s>` (ends a check run after that much game time), `BodyProbe` (per-frame "body still" line),
`CameraOccluders.WalkBlockers` ("walk blockers" line: geometry standing in walkable ground).

1. **7 shook after the first vision — fixed.** Cause: the QF8 wall push-out treated the hand she holds the shape with
   (scene IK, pinned in the world) like a hand in the wall: pushing her body away never freed the hand, and a push that
   turned round snapped back to zero in one frame (up to 25 cm). Hands on a scene-IK target no longer push; a reversing
   push eases through zero (`ChapterSevenWalls.cs`). Measured from the menu → Scene select → "Vision": `body still
   (QF12) … 631 frames, jerks 0, worst 0.3 cm`; after the Venus vision (C09): `840 frames, jerks 0`. Capture
   `qf12_01_after_vision_talking`.
2. **Moon beam hatch far below the ship — fixed.** Cause: the Moon and Venus saucers saved their hull meshes under the
   same asset names, so the Venus build (hull at 3.35 m) replaced the Moon hull (1.15 m): the Moon hull floated 2.2 m
   above its beam. Hull meshes now carry the world's height in their names, and (root fix) the builder never reuses a
   mesh name in a build (`SaveMesh`: a repeat gets `_2`, logged "mesh name reused"). Log: `beam hatch (QF12 2): … gap
   -0.5 cm`; captures `s07_saucer_released_beam_on`, `s08_at_the_edge_of_the_light` (Moon), `v02a_beam_down` (Venus).
3. **Bike hold bands — removed.** Every surface of the hold is one flat even glow (no albedo, no shine; the hatch
   animation fades the shared material): from outside it reads as a softly lit hollow with the bike. `item3_hold_open.png`.
4. **The guardian on Dan's Mixamo rig — done.** `cleanup_r11.py … elarim_rig` (new, ~1 min): Dan's
   `elarim_body_mixamo.fbx` (Mixamo auto-rig + skin, 0 unweighted vertices) + the cape part aligned by the reference
   body's bounds (exact: 3.500 m both), cape on CapeA/B under Spine2 as before → `elarim_v1_rigged.fbx` (the Round 13
   rig kept as `elarim_v1_rigged_r13.fbx`); all clips retarget (avatar valid). Bend test `elarim_mixamo_bendtest_*`: knees
   bend forward, cape stays behind. **The kneel now shows:** the Blender kneel clip still did not lower his hips through
   the Humanoid retarget, so `ElarimNpc` makes the kneel itself when the clip leaves the hips high (left knee down, right
   foot forward, head bowed; eased in/out): `guardian kneel (QF12): hips at 0.45 of standing height`. Cape vs legs
   kneeling / walking / standing: `worst leg contact 0.0 cm` (captures `v16_c03…kneels`, `v17b…walks_cape`).
5. **The bridge slab comes from the quake.** No slab exists before the collapse; the walkway's middle strip breaks away
   as one long piece, falls with the paving into the gully and lies there tilted and dusty; she lifts THAT piece
   (`VenusDirector.SlabFalls`, `SlabRest`). The old "slab bed" beside the road is ground again.
6. **The temple cube is back.** Cause: the small record copy was built with the same mesh names → the original cube's
   meshes were deleted (empty pedestal). Fixed by the `SaveMesh` rule (item 2). The same bug had also emptied **two of
   the three control-device pylons** in the encounter. `cube (QF12 6) at C09: … renderers on 3/3, 0.00 m from its pedestal`.
7. **The stray "wall" on the way down — most likely found.** The walk-path scan finds no geometry standing in any walkable
   ground (only props on their obstacle circles). What a capture on the way down did show: the burrowing creature's dust
   was drawn as large flat grey squares (no particle texture) — a translucent stacked "wall" right beside her, with the
   glowing cube-shaped tunnel lamp behind. All Venus dust / telekinesis motes now use the soft dot texture. *Dan: if the
   wall you saw was something else, a screenshot will pin it down.*
8. **The boss fight — visible and fighting back.** Cause: the chamber creatures' ground height (`WormCreature.BaseY`) is not
   serialized, so the scene never had it: they swam 18 m above the chamber, inside the rock — invisible, while lunges
   and knockbacks still ran. Now set at load; plus an intro beat (the camera finds the Wormmaster on his dais, his pulse,
   every creature breaks the surface, his taunt), lunges start close in front of the camera, the camera swings to the
   threat, and a white light rides with a lunging creature. From the menu, player-style: `encounter intro: 4 creatures in
   the frame; the Wormmaster on screen`, `lunge 1 (player view): … in the frame yes; the Wormmaster in the frame yes`,
   knockbacks, device shields, fairness pause (`item8_*.png`).
9. **Venus arrival — the first frame is black, 7 not there** (`first frame 1-3: fade 1.00, 7 not there`, from the menu's
   "Chapter 2 — Venus"; `item9_first_frame.png`).
10. **Petting on top of his back.** Her palm target = the top of his drawn body (`CatCompanion.BackTop`), stroking along
    it: `petting (QF12 10): her left palm +0.01..+0.02 m vs the top of his back (palm on his back)` (was her knee).
11. **C11 opens already in the pilot's chair** with Panther curled in her lap (`CatCompanion.PlaceOnLap`; no walk, no hop):
    `C11 opens in the pilot's chair: seated True, hips 0.000 m from the seat point, Panther on her lap True`.

**Checks.** Every item seen in a capture from a run started in the real main menu (items 1, 2 via Chapter 1 Scene select;
4–6 via Venus Scene select "first ride"; 8 player-style from Scene select; 9 via "Chapter 2 — Venus"; 10–11 via Scene
select "debrief"). Monochrome: 245 captures, 0 coloured pixels. Audio muted (peak 0) in every run.
**Testing time: about 25 min of game time — over the 20-minute rule by ~5 min** (two baseline runs on the old build to
find causes ~4.5 min; the encounter needed three tries). **Decision:** the full Chapter 1 player-style release run was
NOT repeated this quick fix (it alone is ~10 game-min); Chapter 1's changes are the wall push, two log/capture lines and
the saucer mesh names, each checked by its own menu-started run.

## For Dan to check (QF12)
- Chapter 1 vision: she stands calm, holding the shape, through "There were people here…".
- Moon bay: the beam's iris sits in the saucer's belly (not below it).
- Venus B01 hold: just a soft-lit opening and the bike; B03: the slab only exists after the quake, and you lift it out of the gully.
- The guardian: kneeling in C03, walking ahead in "Follow me", standing — legs never through the cape, knees bending naturally.
- The temple cube on its pedestal (C04, C09); the way down (anything still in your way? a screenshot helps).
- The encounter: the Wormmaster on his dais, creatures surfacing and lunging at you, shields, knockbacks.
- Arriving on Venus from the menu: black + title card first. Aboard after Venus: already in the chair, hand on his back.


## Round 14: Chapter "Europa — Beneath the Ice", first playable pass (Dan, 2026-11-19) — done, status below; release 0.14.0
**Runs unattended overnight, straight after QF12 (Dan is asleep): never stop to ask.** Make the most reasonable call,
note it under "Round 14 — decisions made" in the status, and keep going. Commit a checkpoint first. All standing rules
(monochrome, Kyle's fog/glow, non-sexual, 7 never kills, no exact-spot triggers, look at your own captures, the
"Saucer — fixed design" rule: beam, no ramp). **20-minute rule** for this round's checks. Release **0.14.0**; per-item
status in "Round 14 — status"; "For Dan to check (Round 14)".

**The spec is `Docs/CHAPTER_EUROPA.md`** (Dan's final text, sections 1–17 + progression notes). Build it as playable
connected spaces with placeholder assets where needed, the way Round 11 did Venus: reuse the player controller, hover
bike, dialogue / voice / reaction systems, saves, Scene Select, the vision player, `MusicSystem`. "PROPOSED" sections in
the spec are to be built as written for the prototype (they stay easy to replace).

**Everything Dan made for it is in the repo:**
- Models: `Art/Source/cetacean/whale_v1.glb` (large Cetacean = the keeper + the huge one under the bike) and
  `dolphin_v1.glb` (the guide; ×0.6 for the young one). Notes in `Docs/MODELS-NEEDED.md` ("Cetaceans — Europa"):
  ~1.9 M tris each → decimate; head toward −X; tails already flat; no Mixamo — a spine bone chain head→tail + a bone per
  flipper, procedural swim in Unity; the keeper's head / flippers move with its voice's loudness. Render preview sheets
  into `Docs/Previews/r14/` (don't stop for Dan — he'll look in the morning). Existing: Tunneler, Scree, 7, Panther, saucer,
  hover bike.
- Music: `europa_underwater.mp3`, `europa_base.mp3` (`Docs/MUSIC.md`, Europa section — where each plays, the cross-fade
  at the pressure chamber, quiet after the escape until the calls).
- Vision: `Art/Video/vision_03_europa.mp4` + still `Resources/Europa/vision_03_still.jpg` (spec section 15: its audio is
  nearly silent; the `europa_call_learning` call carries it; play like the Venus vision).
- Voices (`Docs/VOICES.md`): **Keeper** = Dan's saved voice `Keeper`; **Tunneler** = his existing voice; **Scree worker** =
  a premade ElevenLabs voice you pick (don't create or save a voice), with Dan's scene-1 Scree noise layered under each
  line; 7 (v3 + delivery tags from the spec's directions) and Panther (telepathic treatment) as usual. Cetacean speech =
  the voice line over a soft whale-call layer, underwater reverb on both (spec section 4). Give every line an id
  (`EUR_<scene>_<nn>`), dialogue as data + `Docs/EUROPA-LINES.md`, generate all the voices.
- SFX (`Docs/VOICES.md` "Cetacean calls"): `europa_call_deep`, `europa_call_answer`, `europa_call_learning`,
  `europa_dolphin_chirp` via `generate_sfx.py`; add what else the chapter needs (extraction pulse, drill machinery,
  alarm, pressure chamber, demolition, bubbles / underwater ambience, bike underwater hum) to `Docs/SFX-LIST.md`.

**Items**
1. **Getting there.** On the star map Europa becomes **playable** (Mars stays "coming soon"); choosing Europa in VEN_C13
   (or later from the star map) starts the chapter with a title card ("Europa — Beneath the Ice"; covers from the first
   frame — see QF12 item 9). The chapter works whether or not Mars is done.
2. **Arrival + the dive (spec 1–2):** saucer over the ice field (fractures, Kyle's fog/glow, pale ice); the pilot's-chair
   lap scene with Panther (reuse C13's chair, with the hand on top of his back — QF12 item 10); the saucer hovers over a
   sheltered shelf by the opening; the bike and 7 come down **in the beam**. **Bubble helmet:** a simple transparent sphere
   / glass shader on her head (not a generated model) whenever she's underwater or in an unsafe environment; the seal
   check insert with its soft tone. Panther stays aboard (telepathic) the whole chapter.
3. **The bike underwater (spec 2–3):** the same hover bike now swims: steer, **ascend / descend**, brake; slower, floaty
   handling, bubbles trailing, the underwater hum. A gentle tutorial passage down through the ice into the water.
4. **First descent — wonder (spec 3):** the enormous chamber under the ice: light through the ceiling, curtains of rising
   bubbles, ancient structures descending into the depths with small white lights, vast shapes far below; the huge
   Cetacean passes beneath the bike so its scale reads; the call. Uninterrupted riding; quiet stretch after "Tell me."
5. **The settlement + the keeper (spec 4):** the dolphin guide escorts her (chirps, swims alongside); platforms around
   open swimming passages, small vehicles moving, broad chambers; the receiving platform; the keeper conversation; the
   distant mechanical pulse.
6. **The optional race (spec 5):** a course through carved arches, under ice shelves and round great pillars, with a few
   placeholder rival vehicles; registration lines, overtake / loss / win lines. Optional, no history reward; teaches the
   moves the escape needs. Exploration of the settlement before the mission advances.
7. **The machinery + the way in (spec 6–7):** the damaged migration route (shattered supports, debris, sealed passage,
   creatures turning away from the noise); the pulse drowning a call; Tunneler named; objective "Investigate Tunneler's
   underwater base"; the course turning industrial (pipes, intakes, tunnels); Scree workers laboring (fatigue,
   coercion); she parks in an access bay and goes in on foot through a pressure chamber.
8. **Tunneler's base (spec 8–10):** Tunneler on a screen (lit from below; drill head moves with his voice); barriers close;
   reach the worker area and restore the evacuation route — **no killing guards or workers** (existing nonlethal moves);
   free the Scree (the threshold moment kept simple); workers with agency (they open doors, show safe routes, the
   schematic); place the abstract demolition devices; **detonation unavailable until workers and nearby creature routes
   are clear** — Panther tracks the transports; tension without a timer.
9. **Tunneler escapes + the escape ride (spec 11–12):** across the sealed window he boards his craft and leaves (not a boss
   fight); she mounts, triggers the demolition; the chase out — ruptures following her, readable steering, warnings,
   Panther's calls, the low tracking shot, the burst into open water, the pressure wave she recovers from (don't cut
   away). Fair and retryable: a hit or a crash = restart from the start of the ride.
10. **After the noise + the temple + the vision (spec 13–15):** the quiet moment, the calls answering across open water;
    the temple built for its inhabitants (broad, huge, understood through movement and sound); the rune = **icosahedron,
    generated in code like the cube and dodecahedron**; the vision (item above); the keeper's lines; the record awarded
    once — **insert: 7 secures the small icosahedron beside the cube and the dodecahedron.**
11. **Home (spec 16–17):** the bike recovered in the beam, 7 boards in the beam; helmet off; Panther waiting near the
    boarding area; the reunion; the debrief in the pilot's chair with Panther on her lap (already seated — QF12 item 11),
    the purr, hold the two-shot; back to free exploration with the star map (Mars available, Europa done).
12. **Progression + saves:** the spec's progression notes; checkpoints for Continue; **Scene Select entries** for the main
    beats (arrival, settlement, base, escape, temple, debrief); the Log and Field Guide on the console get Europa entries
    (the Cetaceans, the keeper, the Scree here, Tunneler again). The icosahedron sits with the other records on the chair arm.

**Checks + release (20-minute rule):** targeted runs per section with captures you look at (the descent, the keeper, the
race, the base, the escape ride, the temple + vision, the debrief), one player-style run from VEN_C13's Europa choice to
the debrief, Scene Select entries start correctly. Publish **0.14.0**; docs (`CLAUDE.md` "Where things stand", the status
and "For Dan to check"); commit + push.

## Round 14 — status (2026-11-19): first playable pass done, exe-verified off-screen; release 0.14.0 (`game-1400`, Latest, published)
The whole chapter plays from VEN_C13's star-map choice to the debrief. Captures + logs: `Docs/Previews/r14/`
(`europa_playtest_sheet.png` = the player-style run, `r14_models_sheet.png` = the Cetacean / Tunneler previews).

1. **Getting there — done.** Europa is playable on the star map (Mars stays "coming soon"); choosing it in VEN_C13 or
   later from the console starts the chapter (`EuropaState.StartFresh`; in progress = Continue; done = "visited" card).
   The title card "EUROPA — BENEATH THE ICE" plays over black from the first frame (logged `fade 1.00`).
2. **Arrival + the dive — done.** Pilot's chair with Panther already curled in her lap, hand on top of his back (QF12),
   the navigation-display insert, EUR_01 lines (Saucer scene, `Chapter1AboardEuropa.cs`); then the Europa scene: the
   saucer crosses the ice field and hovers over the shelf, the beam lowers the bike, then 7 in her bubble helmet
   (`BubbleHelmet.cs`: a glass sphere, URP Lit transparent), the seal-check insert + tone, hands into the grips, EUR_02.
   Panther stays aboard (telepathic) all chapter.
3. **The bike underwater — done.** `HoverBike.Swim`: steer, RT forward, A ascend / B descend, LT brake; floaty, bubbles
   trail, muffled hum (`europa_bike_hum`); kept inside the swim space (`EuropaLayout.SwimClamp`). Taught once on the
   gentle ice passage (prompts Steer/Forward, Ascend/Descend, Brake).
4. **First descent — done.** The great chamber: ceiling cracks with light, light shafts, bubble curtains, ancient
   structures with small white lights, spires below; the huge Cetacean (30 m) passes under the bike with the deep call;
   EUR_03; a quiet stretch after "Tell me." (no lines).
5. **Settlement + keeper — done.** The dolphin guide escorts her (chirps, swims alongside), platforms round open swimming
   rings, small vehicles shuttling, the receiving platform, the keeper (16 m whale; head / fins move with his voice, a
   whale call under every Keeper line), the extraction pulse.
6. **Optional race — done (placeholder).** Registration arch → 8 rings through arches, under ice shelves, round pillars,
   3 rival craft; registration / overtake / loss / win lines; result saved, never required (no reward). Free time in
   the settlement until X at the keeper.
7. **Machinery + the way in — done.** The damaged migration route (shattered supports, debris, sealed passage, a Cetacean
   turning away), the pulse over a call, Tunneler named, objective "Investigate Tunneler's underwater base"; pipes /
   intakes / an industrial tunnel; Scree labouring outside; park in the access bay (X), the pressure chamber (music
   crossfades to `europa_base`).
8. **Tunneler's base — done.** On foot, helmet on. Tunneler on the control-room screen (his model filmed in a lit "studio",
   light from below; the drill head turns with his voice's loudness), EUR_08, barriers close; reach the holding area
   (override panel); the threshold moment (EUR_09); workers with agency (one opens the transport access, one opens the
   machinery section, one shows the schematic); evacuation-lift power; three demolition devices (X); Panther tracks the
   transports; detonation only after every worker is out (no timer). Nobody is harmed.
9. **Tunneler escapes + the escape ride — done.** Across the observation glass he boards his craft and leaves (not a boss
   fight); hands into the grips; she triggers the demolition (white flashes, lights fail); the chase: ruptures follow her
   along the tunnel, broken supports to steer round, Panther's calls at the junction, the low tracking shot, burst into
   open water, entrance collapses, the pressure wave throws the bike and she recovers on camera. Caught or crashed =
   white flash, retry from the ride's start.
10. **After the noise + temple + vision — done.** EUR_13 (helmet close-up, the calls answering), the keeper leads to the
    temple (broad arches, a great hall), the rune = icosahedron built in code (`Icosahedron`), the vision video
    (`vision_03_europa.mp4`, `europa_call_learning` carries it, music suspended / resumed, still fallback), EUR_15, the
    record awarded once (`awards 1`), the insert of the small icosahedron.
11. **Home — done.** The bike rises into the beam, 7 boards in the beam (no ramp); aboard: helmet off, Panther waiting by
    the boarding area, she kneels, his head against her hand (EUR_16); the debrief in the chair already seated with
    Panther in her lap, the three records on the chair arm, the purr, the held two-shot (EUR_17); then free exploration.
12. **Progression + saves — done.** `EuropaState` (PlayerPrefs `seven_europa`, Scene Select slot `seven_europa_select`):
    10 checkpoints for Continue (main menu "Continue — Europa: …"), Scene Select entries (arrival, settlement, base, escape,
    temple, debrief — on the Venus page of both menus), new game clears it; the console's Log (7 Europa entries) and Field
    Guide (Cetaceans, the keeper, Tunneler unlocked, the Scree extended) — mirrored in `Docs/ADVENTURE-LOG.md` /
    `Docs/FIELD-GUIDE.md`; the icosahedron sits with the other records on the chair arm.

**Assets made this round:** Cetaceans `Tools/Blender/cleanup_cetacean.py` (whale 30 k / dolphin 16 k tris, 8-bone spine +
2 fin bones, procedural swim `Cetacean.cs`); Tunneler `Tools/Blender/cleanup_tunneler.py` (36 k tris, separate DrillHead,
untextured stone); 131 voice lines (`Tools/Voice/make_europa_lines.py` → `Docs/EUROPA-LINES.md`,
`generate_europa_voices.py` → `Resources/EuropaVoice/`); 16 SFX (`generate_sfx.py`, `Docs/SFX-LIST.md`).

**Checks.** Autopilot runs (`-europacp <n>`) over every section, the one player-style run (`-europaplay`, virtual
gamepad, started in the main menu: Scene select → the Venus debrief → C13 star map → Europa → … → the Europa debrief;
it reached the debrief's records insert at its 12-minute cap; 2 skips taken where the machinery doorway's zones
overlapped only 0.4 m — fixed and re-checked: all three devices placed and the way back walked with no stall), the
debrief from Scene Select to `EUROPA CHAPTER DONE` + free exploration. Monochrome 0 coloured pixels in every capture,
audio peak 0. **Testing time: about 28 min of game time — over the 20-minute rule** (the first fog was unusable and the
base needed door-to-door routing; each fix needed a short re-run).

## Round 14 — decisions made (Dan asleep; easy to change)
- Section 1 order: the title card and the chair scene play aboard first, then the extreme wide over the ice in the
  Europa scene (the saucer interior lives in the Saucer scene).
- Panther speaks with the near telepathic treatment all chapter (clearer than the "far" filter under water).
- Scree worker voice: premade ElevenLabs "Callum – Husky Trickster" (first hoarse/raspy premade found; no voice created),
  with `scree_grunt` under each line.
- Keeper = the whale model at 0.62 (≈ 16 m); the huge one = 1.15 (≈ 30 m); the guide = the dolphin at 2.8 m.
- The helmet stays on inside the base ("conditions require"); it comes off aboard.
- The race is placeholder geometry (rings, simple rival craft); the base, settlement and temple are built from simple
  shapes (first playable pass, as Round 11 did Venus).
- Way home: a short ride, then a cut to the shelf (the keeper's people show her the way up) instead of riding the whole
  way back.
- Europa finished + chosen again on the star map = a "visited" card (no replay yet).
- New portraits for the Field Guide (`cetacean`, `keeper`) are not rendered yet (the entries show without a picture).

## For Dan to check (Round 14)
- Star map / VEN_C13 → Europa: the title card, the chair scene (hand on his back), the beam down with the helmet.
- Riding under water: does the swim handling (A up / B down, floaty) feel right? Fog density under the ice (tuned twice).
- The huge Cetacean passing under the bike; the keeper's voice with the whale call layered under it.
- The race (optional) and whether the rival speeds feel fair.
- The base: Tunneler on the screen, the workers opening doors themselves, the devices, the wait for the transports.
- The escape ride: readable? fair? (ruptures 6 m/s behind a bike that tops out at 8.5 m/s).
- The temple, the vision with the learning call, the icosahedron insert; the reunion and the debrief.
- Open: Tunneler is untextured and unrigged (stands / is moved as a whole); Field Guide portraits for the Cetaceans.


## Round 13: conversation sounds + the ship's console (Dan, 2026-11-19) — done, status below; release 0.13.0
Commit first. All standing rules (monochrome, Kyle's fog/glow, non-sexual presentation, 20-minute testing rule, look at
your own captures, no exact-spot triggers). One release at the end: **0.13.0**; per-item status in "Round 13 — status";
"For Dan to check (Round 13)".

### 0. Fixes from Dan's 0.12.0 play — do these FIRST (Dan, 2026-11-19)
0a. **City reveal (6b) camera:** show them looking up only **briefly**, then cut to **what they're looking at** —
    their view up the curve: the traffic streams overhead, the towers rising into the clouds (POV / over-the-shoulder
    looking up). Then the extreme wide and the lower-districts view as before.
0b. **The saucer has no ramp — lock this in.** Remove the ramp everywhere (it also appeared already extended). Getting
    in and out is **the tractor beam, as in Chapter 1**, on the Moon, on Venus and any later world. Add a "Saucer —
    fixed design" rule to `CLAUDE.md` (one shared exterior; no ramp; beam for boarding / disembarking; the bike hatch
    seamless when closed) so it never drifts again. **The bike hold shows two black bands when it opens** — remove them
    (the opening is lit and clean).
0c. **A new game starts every chapter fresh.** Starting the game from the beginning (Chapter 1 from the menu) and
    playing through "Set course" must arrive at **the start of Chapter 2** (the descent), not at an old Venus save. A
    new playthrough clears later chapters' progress (ask once if saves exist: "Start a new game? Your saved progress
    will be reset."). Continue still resumes.
0d. **The bike hangs up on the telekinesis bridge** unless you hit it at one angle. Any reasonable approach angle must
    ride over: wider bridge, smooth ramped ends meeting the ground on both sides (no lip), and the bike's hover glides
    over small edges instead of snagging. Check from several angles both ways.
0e. **The guardian:** legs still clip through his cape; arms and legs are stiff and awkward. Fix the body for real:
    (1) first find the cause (skin weights on the arms / legs, the Humanoid avatar mapping, twist bones, the clips'
    retarget settings) and fix it; (2) **if he still looks stiff, rig him through Mixamo** — export his body *without
    the cape* as `Art/Export/elarim/elarim_body_for_mixamo.fbx` early in the round and tell Dan to upload it to
    Mixamo's auto-rigger and download it (FBX for Unity, with skin) to `Art/Mixamo/elarim/elarim_body_mixamo.fbx`;
    then re-attach the cape on its own bones (keep QF10's no-clip rule: 0 cm leg contact standing, walking, kneeling).
    Also **he tells her to follow him** after C03 and walks ahead to the cube chamber: new line **VEN_C03_11 |
    Guardian | "Follow me."** (voice it; add to the spec + dialogue data).
0f. **The encounter is too easy — they should be trying to stop her.** Dan saw no worms, and nothing opposed her on the
    way down or while she switched the devices off. Make it a real (still nonlethal) challenge: the **worms** are
    visible and driven at her along the device routes on the way down and in the chamber — she dodges, blocks, and
    pushes them aside / stuns them with telekinesis (they recover and withdraw — never killed, they're victims); the
    **Wormmaster** actively fights back — he fires signal pulses that send worms at her, shifts routes, blocks the
    next device, taunts. Getting hit knocks her back (existing failure / retry). Only when all three devices are off do
    the worms withdraw; then the containment. Tune it as a fair first boss.
0g. **The level music doesn't come back after the visions** (both chapters). After a vision ends, the level's track
    resumes (fade in from where it left off, or restart), every time.
0h. **The hover bike needs a sound:** an audible hum while idle that rises in pitch and adds a soft whoosh with speed,
    a lift-off / settle sound, mount / dismount. The Round 11 bike hum isn't audible — fix the level / routing.
0i. **Birds and other animals on Venus** — at least hear them: bird song and calls, insects, distant animal calls,
    rustling in the grass, varied and positional across the gardens (quieter underground); plus a few visible birds
    crossing the sky and small animals in the grass where cheap.
0j. **The debrief (C11) at the end of the act:** she sits in her chair with **Panther curled on her lap** (the lap
    moment from Chapter 1) for the conversation — not at the table. Keep the lines; the dodecahedron and the cube are
    beside her (on the arm / a side table) for the insert shot.

0k. **The tremor must be obvious, and we see the walkway break** (Dan). VEN_B03 / C02 today: no noticeable tremor and
    the gap is just already there. New beat: as 7 rides toward the damaged stretch, a **strong tremor** hits — deep
    rumble, camera shake, the bike wobbles, dust and small stones fall, water slops out of the channel, grass and trees
    shiver, workers stumble and grab hold — then, in front of her (a short in-engine cut, she brakes hard), **the
    walkway / path cracks and collapses** into the gully with dust and falling stone. C02's lines follow ("Something's
    wrong here." / Panther: **"I felt that from here."** — Dan's new line, replaces "I felt that through the landing
    supports."; re-voice it, update the dialogue data). Then the existing bridge beat: she gets off, lifts the slab across with
    telekinesis, rides over. **Every tremor in the chapter must be just as obvious** — Dan: the second one (C04's interruption in
    the temple) wasn't noticeable either: strong rumble, a **big** camera shake (Dan: don't hold back — no need to
    keep it gentle), dust and stones raining from the ceiling, the guardian and 7 bracing, the cube's cage
    rattling; any later tremors the same. Skippable like other cutscenes.

0l. **VEN_B01 — the name reveal is rewritten again (Dan, 2026-11-19; the spec has the final text, VEN_B01_06–20).**
    New lines and **delivery directions** (casually / a small surprised laugh / dryly / teasing / perfectly composed /
    breathless then a disbelieving laugh / mildly / a reluctant chuckle / pointing, still amused / a thoughtful hum /
    warmly / quietly assured) — record every line fresh with those deliveries (Seven on v3 with audio tags; Panther as
    well as v3 allows), drop the old B01 takes. New beats: her surprised look down at him; his "Ahem."; she gasps and
    steps back catching the fairing, her gaze rising with him as he grows; she starts to argue then chuckles; he settles
    on his haunches pleased with himself; she points at him; she laughs and shakes her head mounting; one last look
    back as the bike rises. He lies down **beside the ship** (no ramp — 0b). Regenerate the dialogue data. These
    laughs / chuckles / hums are part of the lines themselves, not the reaction bank (item 1).

0m. **Two new scenes give the leads to the next worlds (Dan, 2026-11-19 — final text in the spec):**
    - **VEN_C12 "Two locations"** — right after C10, before she leaves the temple: the guardian shows an ancient
      display with **Mars** and **Europa**, explains them, sends them to the ship (a soft tone; Panther:
      "Received. Two destinations."). **Result: Mars and Europa become available on the star map.** (Spec order:
      C10 → C12 → B07.) The guardian walks to the display; over-her-shoulder shot of it (two glowing locations).
    - **VEN_C13 "Choosing the next world"** — straight after C11, in the saucer's **navigation area**: 7 in the
      **pilot's chair with Panther curled in her lap**, one hand stroking him, the other on the navigation controls;
      the star map display ahead (Mars, then Europa brought close); the medium side two-shot; then a **player choice**
      (Mars — "Seek the temple among the divided kingdoms" / Europa — "Find the Cetacean temple beneath the ice") with
      its own short exchange for each; she sets course; close insert of Panther purring; wide from behind the chair as
      the journey begins. Mars / Europa chapters don't exist yet → after the journey starts, a "Chapter 3 — Mars /
      Europa (coming soon)" card, back to free exploration; both stay selectable on the star map.
    - **This changes 0j and the console (item 2):** C11 also happens in the **pilot's chair at the navigation console
      with Panther on her lap** (not the galley), flowing straight into C13. The chrome console gets a **pilot's
      chair** in front of its navigation display for these scenes (the player still uses the console standing).
      Star map rule: only reachable places — the Moon and Venus, **plus Mars and Europa once C12 has happened**.
    - Voice all new lines with their delivery directions (Seven v3, Guardian, Panther), including the stroking /
      purring / head-nudge beats as animation on the lap pose.

### 1. Laughs, giggles, hmms — the sounds people make in conversation
Dan: when one says something funny, the other laughs or giggles; and add "hmm", "mm-hmm" and the other little sounds
people make while talking. Chapter 1 and Venus, every conversation.
- **A reaction bank per character**, made with ElevenLabs (the same voices — `Seven` on v3 with audio tags such as
  [laughs] / [chuckles] / [sighs]; try v3 for `Cat` too, so his match his voice): several takes each of — 7: a soft
  laugh, a small giggle, a quiet chuckle, an amused breath through the nose, "hmm" (thinking), "mm-hmm" (agreeing),
  "hm?" (questioning), a small sigh, a soft "oh". Panther / the cat: a dry low chuckle, an amused purr-hum, "hm",
  "mm", a short pleased purr, a long-suffering sigh. Keep them short, natural, quiet; the cat's get the telepathy
  treatment like his lines. Several takes so repeats don't sound identical.
- **Where they go:** write `Docs/REACTIONS.md` — go through every scene in `Docs/DIALOGUE.md` and
  `Docs/CHAPTER2_VENUS.md` and place reactions: the listener laughs / chuckles after a genuinely funny line (e.g. after
  "Only conversationally.", "Having experienced confinement, I feel strongly about it.", "Apparently we escaped just
  in time for traffic.", "An excellent time to be small.", "You're not getting a bigger bowl."), "hmm" / "mm-hmm"
  while thinking or agreeing, a sigh where it fits. **Subtle and sparing** — a few per scene, never over a spoken
  line, never undercutting a sincere moment (the cell "Thank you" / "You're welcome", "Remembering.", the seed, "I
  think I knew them."). Reactions play with a small body cue (a head tilt, shoulders, his tail / ears). Make the
  placement data (like the dialogue data) so Dan can move or remove one without code. List them all in the status.

### 2. The ship's console — a chrome, retro-futuristic control station she stands at
On the saucer (the main deck near the galley / the larger interior): an **all-chrome retro-futuristic console** —
rounded polished chrome, curved glass screens with a soft glow, chunky switches and dials, the Kyle glow; grayscale
like everything. 7 walks up to it and **stands** at it (context prompt X / E "Use console"; she steps in and rests her
hands on it), the camera moves over her shoulder to the screens, B / Esc leaves. A menu on the screens, in the same
chrome-and-glow style (readable text, the existing UI font):
- **Log** — a log of all her adventures: an entry for each chapter and each major beat she has completed, in order
  (date-less, short — two or three sentences each, plain and in the game's tone; e.g. the escape from the Moon prison,
  the city, Venus and the Elarim, the cube). Entries appear as she reaches them (from the story / Venus state).
  Draft the text in `Docs/ADVENTURE-LOG.md` **from what happens in the game only** — no new lore; Dan will edit it.
- **Field guide** — a picture and a description of each creature and person she meets, unlocked when first met:
  Panther (the cat — "the cat" until his name is revealed on Venus), the Scree, the Elarim (the guardian, the
  attendant), the burrowing creatures, the Wormmaster; later others (Tunneler, Ratwing) as the story meets them. Picture
  = a clean in-engine portrait render of the model (grayscale, Kyle-style lighting). Descriptions in
  `Docs/FIELD-GUIDE.md`, short, written as 7's observations, **only from what the story has shown** (e.g. the Scree:
  work the mines and guard for Tunneler, not evil) — no invented lore; Dan will edit.
- **Star map — choosing the next destination** (Dan): Round 12's destination screen becomes a **planet / star map**
  on the console. The solar system in grayscale with Kyle's glow — the Sun at the centre and only the worlds she can
  reach on their orbits — the saucer's current position marked; move a cursor (stick / mouse) between bodies, each shows its
  name and, once visited, a line from the Log. **Only places she can go to are shown** (Dan): the Moon and Venus — no other
  planets or bodies on the map until the story makes them reachable (then they appear). "Set course"
  on a selected world → the existing travel (the Moon falls away / the descent). The map is the only place to choose
  a destination from now on (the galley seat's "Set course" points her to the console).
- **Settings** — the same settings as the pause menu (audio volumes: music / voices / effects; subtitles; controls /
  invert camera; display), one shared settings system.
- **Upgrades** — not built; leave a slot for it in the menu code but don't show it yet.
- Cutscene rule: the console never opens during a story beat; the galley table / chair still work as before.
- Captures: the console from the deck, 7 standing at it, each screen (Log with a few entries, Field Guide list + one
  entry, the star map with Venus selected, Settings).

### 3. Checks + release (20-minute rule)
A quick run through one funny exchange in each chapter (reactions audible, nothing over a line), the console's screens
with a save that has Venus finished (Log + Field Guide filled) and one that has only Chapter 1, Settings changes take
effect. Publish **0.13.0**; docs; commit + push.

## Round 13 — status (2026-11-19): done, exe-verified off-screen; release 0.13.0 (`game-1300`)
Committed the checkpoint first (`1b86e8d`). Dan's 0m (C12 + C13) was added mid-round and is included.

**Early: the guardian's cape-less body for Mixamo** — `Art/Export/elarim/elarim_body_for_mixamo.fbx` (texture embedded;
cape cut off by `cleanup_r11.py ... elarim_mixamo`, the cape saved as `elarim_cape_part.fbx` in the same coordinates;
sheet `Docs/Previews/r13/elarim_body_for_mixamo_sheet.png`). **Only needed if the rig fix below still looks stiff.**

### 0. Dan's fixes
- **0a City reveal camera — done.** The low two-shot of them looking up is now brief (~1.7 s); then their view up the
  curve over her shoulder (traffic streams, towers into the clouds), a second POV pan following the traffic, then the
  extreme wide and the lower districts as before (`Chapter1Director.Scene6bReveal`, shots `c02b` / `c02c`). Checked only
  through the player-style run (no 6b captures this round).
- **0b No ramp — done.** Ramp removed from Venus; the saucer hovers there (underside 3.35 m) and 7 + the cat come down /
  go up in the tractor beam (`BuildSaucerBeam` shared with Chapter 1, runtime `SaucerBeam.cs`; `VenusDirector.BeamDown`,
  `BeamUp` in B07 and return visits). "Saucer — fixed design" rule in `CLAUDE.md`. The bike bay's two black bands were its
  dark floor + ceiling (gray 0.2) — now pale and softly lit (capture `v06c` checked: clean, lit).
- **0c New game — done.** "Chapter 1" from the menu asks once ("Start a new game? Your saved progress will be reset.")
  when any save exists and clears Chapter 1, Venus and the console's progress (`SaveGame.NewGame`), so "Set course" leads
  to the start of Chapter 2. Continue still resumes. (Logic only; no menu capture this round.)
- **0d Bike on the bridge — done.** Cause: the bridge zone met the road zones edge-to-edge and the bike's 0.25 m pad made a
  0.5 m strip that counted as off-road (the ≥ 0.6 m overlap rule); plus a 3 m-wide slab and axis-only sliding. Now: slab
  4.6 m wide, ramped stone aprons at both ends (zones `BridgeRampS/N`, overlapping 1.4 m), and the bike glides along
  edges (tries the move turned up to ±70°). **`-r13 bridge`: 10/10 approaches across (±35°, ±20°, 0°, both ways), 0 frames
  stuck.**
- **0e The guardian — cause found and fixed; Mixamo export ready.** Cause: Tripo's mesh is ~2000 loose pieces with the cape
  fused around them, so Blender's automatic weights failed on a third of the vertices and the WHOLE body fell back to
  "two nearest bone segments" weights (no smooth joints = stiff, awkward), and the old cape selection ("everything behind
  the hips below the chest") put the backs of his thighs and calves on the cape bones (= legs through the cape). Now
  (`cleanup_r11.py elarim`): weld the pieces, cut the cape off as its own part, skin the body alone (automatic weights:
  0 of 15268 vertices unweighted), only the cape on CapeA/CapeB, 0 leg vertices on cape bones. Bend test previews
  `Docs/Previews/r13/elarim_bendtest.png` (elbows / knees / shoulders bend smoothly). Cape: 0.0 cm leg contact standing,
  kneeling, walking. "Follow me." (VEN_C03_11) voiced + in the spec/data; he walks ahead to the cube chamber and waits if
  she falls behind (`GuardianLeads`). **Still open: his kneel in C03 does not show** (the kneel state is active but the
  pose stays standing — the Blender kneel clip's retarget; a Round 12 leftover) — please look; if the body still looks
  stiff to you, do the Mixamo upload above.
- **0f The encounter fights back — done.** The Wormmaster fires a signal pulse every 4.5–7.5 s (faster as devices fall):
  the nearest creature lunges at her (surfaced, fast, dust + rumble), the device she is nearest is shielded for 3.5 s
  ("Shielded by his signal — wait"), that device's creatures are pulled across her path, and he taunts (4 new short
  lines, `VEN_ENC_T1–4`, his voice — not in the spec, edit/drop in `VenusDirectorPart2.Taunts`). RT on a creature pushes it
  aside and stuns it (6 s, it recovers; never harmed). Contact = the existing knockback/retry; after 3 knockbacks the
  pulses pause 20 s (fairness). On the way down a creature in the root chamber lunges at her too. Check run: pulses 6,
  lunges 6, shields 4, taunts 3, stuns 3, knockbacks 2, creatures harmed 0; captures `enc_lunge_*`.
- **0g Music after visions — done.** `MusicSystem.Suspend/Resume`: the level's track fades back in from where it left
  off after both visions (log: "music after the vision (0g): 'venus_gardens' playing again"; Chapter 1: see the playtest).
- **0h Bike sound — done.** Cause: the bike's `Start` ran before the sound bank was loaded, so the hum loop was never
  created. Now made as soon as the bank exists; idle hum, pitch + level rise with speed, a whoosh that comes in with speed
  (new), lift-off / settle / mount / dismount (new SFX). Verified only by the log (muted rule).
- **0i Venus wildlife — done.** `VenusWildlife.cs`: songbirds from varied 3D positions, distant bird / animal calls, an
  insect bed, rustles in the grass and birds taking off near her; fades out underground. 17 small flapping birds over the
  gardens (`VenusFlyer.Flap`). New SFX: `bird_song_a/b`, `bird_call_far`, `animal_call_far`, `insects_loop`,
  `grass_rustle`, `wing_flutter`. (48 sounds by C03 in the check run.)
- **0j / 0m C11 in the pilot's chair with Panther on her lap — done**, flowing straight into **C13**: she walks to the
  pilot's chair at the console, sits, Panther hops up and curls in her lap (the Chapter 1 lap cat), her left hand strokes
  him; the dodecahedron and the cube on the chair's arm for the insert; C11's lines; then C13 (over-her-shoulder on the
  star map, his head lifts / nudges, the side shot), the **player choice on the star map** (Mars / Europa with Dan's
  labels), the branch lines, the purr insert (`cat_purr`), the wide from behind the chair, then **"Chapter 3 — Mars /
  Europa (coming soon)"** and free exploration (both stay on the star map). All 51 C12/C13 lines voiced with Dan's
  directions (v3 tags).
- **0k Tremors — done.** `CameraShake` (a real shake: 23 cm / 2.5° peak measured; the old one was overwritten every frame).
  B03: as she rides up a strong tremor (rumble, shake, bike wobble, workers stumble, water slops, grass shivers), a short
  cut — she brakes hard and the walkway in front of her cracks and falls into the gully with dust — then C02 with Panther's
  new "I felt that from here." C04: a bigger shake, stones + heavy dust from the ceiling, the cage rattles, 7 and the
  guardian brace.
- **0l B01 rewritten — done.** Dan's VEN_B01_06–20 with every delivery direction voiced into fresh takes (7 and Panther on
  Eleven v3; the old takes replaced); new beats (her look down at him, step back + gaze rising, the chuckle, him on his
  haunches, pointing, the head-shake laugh, one last look as the bike rises); he lies down beside the ship (no ramp).
  Panther now stands 3 m from the bike (he was 9 m away: the two-shots were too wide).
- **0m C12 — done** (see 0j for C13): right after C10 the guardian walks to an ancient display in the cube chamber, Mars
  and Europa glow up, over-her-shoulder on Mars then Europa, the transfer tone, Panther "Received. Two destinations.",
  her thanks. Mars and Europa then appear on the star map.

### 1. Reactions — done
`Docs/REACTIONS.md` + `Resources/Reactions/reactions.json` (49 placements: 26 Chapter 1, 23 Venus; sincere moments left
alone; move/remove one by editing the json). Bank: 15 kinds × 3 takes (`Tools/Voice/generate_reactions.py`, 7 and the cat
on v3; the cat's get his telepathy treatment). Runtime `Reactions.cs`, in `DialoguePlayer.PlayOne`: after the line, a quiet
random take (never the same twice in a row), the next line waits for it; skipped lines skip their reaction; small body cue
(head tilt / nod / shoulders for 7 where the table IK runs, ear twitch for the cat). In the check runs: 0 overlaps.
**Not heard by anyone yet** (muted rule) — please listen; `seven_hmm_3` is the take most likely to need a redo.

### 2. The console — done
Chrome crescent desk with two curved glowing screens on the main deck (in front of the sofa, 296 degrees), a pilot's chair at the navigation
display. X / E "Use console", she stands with her hands on it, the camera goes over her shoulder; B / Esc leaves. Menu: Log
(17 entries, `Docs/ADVENTURE-LOG.md`), Field guide (portraits rendered in Blender, `Docs/FIELD-GUIDE.md`; "The cat" until
B01, then "Panther"), Star map (only reachable places: the Moon + Venus; Mars + Europa after C12; Set course = the existing
travel / "coming soon"), Settings (the pause menu's own settings — music / effects / voice / button hints; there are no
subtitle / invert / display settings in the game yet), Upgrades slot hidden. The galley seat now says "Set course at the
console". Checks: `-r13 console` OK (Venus finished: Log 17, Field guide 7, map Moon/Venus/Mars/Europa, Mars → coming-soon
card, music setting lowered and restored, button hints off/on took effect); `-r13 consolefresh` OK (Chapter 1 only: Log 8,
Field guide 2, map Moon/Venus).

### 3. Checks — **over the 20-minute rule: about 57 game-minutes in total.** Most of it was 7 attempts of the one release run: each found an old gap in the player-style driver (it could not fight the QF9 terrace Scree, a queued button press delayed its pause, it did not know "Set course" now flies to Venus, it did not know the new-game confirm) or a real Round 13 problem (the console stood across her walk to the galley - moved from 236 to 296 degrees on the deck).
- **Release run: `PLAYTEST: 1/1, soft-locks 0, story watchdog fallbacks 0`** (5.0 min at 2x, from the main menu with the new-game confirm, quit + Continue at checkpoints 5 and 10, the terrace fight, the galley, "Set course" → Venus). Reactions in it: 7 heard in full, 0 overlaps.
- `-r13 bridge` 10/10 · `-r13 tremor` (B02 → C04) · `-chapter2 -quick` (beam down, B01) · `-r13 encounter` (C06 → C12 →
  beam up → C11/C13 → star map: `VENUS CHAPTER DONE`) · `-r13 console` / `consolefresh` OK. Audio peak 0 in every run;
  0 non-gray pixels in the console captures.
- Found and fixed by the checks: the bridge gap, a crash when Panther hopped onto her lap, the guardian blocking the C12
  display, two chair shots blocked by the console pods, black "chrome", her hair over the screen, and the player-style
  driver not knowing the QF9 terrace fight (it now walks up, attacks, dodges the telegraphed punch, takes them down).

## For Dan to check (Round 13)
- Listen: B01 (the new takes and laughs), C12 / C13, the reactions, the bike hum + whoosh, Venus birds and insects, the
  tremor + walkway collapse, the Wormmaster's taunts.
- The guardian: do his arms / legs move naturally now? (If not: upload `Art/Export/elarim/elarim_body_for_mixamo.fbx` to
  Mixamo's auto-rigger, download "FBX for Unity, with skin" to `Art/Mixamo/elarim/elarim_body_mixamo.fbx`.) His kneel in C03
  still doesn't show.
- The encounter's difficulty (pulse rate, lunges, shields) — tune in `VenusDirectorPart2.Harass`.
- The console's look and camera, and the chair scene C11 → C13 → the Mars / Europa choice.
- The new-game confirm in the main menu.

## Round 12: QF11 fixes + Venus part 2, all in one — done, status below (Dan, 2026-11-18: "fold everything together")
Commit first. **The spec is `Docs/CHAPTER2_VENUS.md` — it wins.** All standing rules (monochrome, Kyle's fog/glow,
non-sexual presentation, 7 never kills, the 20-minute testing rule, look at your own captures, fire-at-or-past triggers +
player fallbacks on every wait, no points of no return). Order: **Part A (QF11, below) first**, then Part B. One release
at the end: **0.12.0**; per-item status in "Round 12 — status"; "For Dan to check (Round 12)".

### Part B — Venus part 2 (after Part A's items 1–2c)
B1. **Leftovers from earlier rounds:** the guardian's kneel in C03 (QF10 open: the `v16` capture still shows him standing);
    QF8's two open camera spots (the climb-down into the mining cut, one spot right of the lunar door).
B2. **The way down (VEN_04 → VEN_05):** from the parked bike at the observation area, on foot through the root chambers
    into the underground (existing traversal, readable route, no exact-spot triggers). Music `venus_underground.mp3`
    below ground (crossfade at the threshold); its whispers ducked under dialogue.
B3. **Wormmaster** (`Art/Source/wormmaster/wormmaster_v1.glb`; reference `Art/Reference/kyle_wormmaster.jpg`): Blender
    cleanup + a Humanoid-compatible rig with extra bones along the arms so they can curl, and a head-tip bone that
    pulses / swells with his voice (no mouth, no lip-sync). Voice: ElevenLabs voice **`Wormmaster`** if Dan has made it
    (description in `Docs/VOICES.md`); if not, subtitles only and say so. Small worms reuse `worm_v1`.
B4. **VEN_05 control chamber + the encounter (spec section 7):** Wormmaster above his machinery (low angles, grayscale
    underlighting); three control devices emitting signals; creatures on clearly visible routes per device; safe
    observation and traversal spaces; disable each device with existing abilities (interaction / telekinesis —
    no new power); creatures withdraw when all three are off; a final interaction closes a containment barrier
    around him — only containment completes it. Nonlethal everywhere: no creature or Wormmaster death possible.
    Existing failure / retry. Save after each device and after containment. Scenes **C06** (introduction) and
    **C07** (threat stopped — level camera angles, his dominance gone).
B5. **B06 return ride** with the world changed (spec section 9: pulses stop, creatures move freely, workers back on
    the paths, water in a repaired channel, damage still visible) and its lines.
B6. **C08 → C09 → C10 at the temple:** C09 plays Dan's vision video **`Art/Video/vision_02_venus.mp4`** full screen
    (skippable; a still fallback if the video can't play), then the **match cut** from the kneeling figure's offered
    palm to 7's empty palm, and **the new C09 lines** in the spec (VEN_C09_01–05 — "That tree… it was barely up to my
    knee." … "I think I knew them."). C10: the guardian gives her the small cube copy, awarded **exactly once**; the
    original stays in its protected structure.
B7. **B07 ride back + parking:** the bike glides back in through the hatch (the elegant open, reverse of B01; nothing of
    the bike visible through the hull) and the hull is seamless again.
B8. **C11 debrief in the galley:** Panther at normal size near the entrance, follow 7 to the table, **the dodecahedron
    and the cube set together on the table**, the table-height two-shot, open-hand insert, the final wide pause.
    Over-the-shoulder shots must show Panther (QF8 rule).
B9. **Destination selection** (spec: "open the existing navigation flow"): after C11, a simple destination screen on
    the saucer with the worlds that exist (the Moon, Venus) — nothing invented for worlds that don't exist yet.
    Sets `VEN_S08_COMPLETE`. Return visits (section 9) keep their changed states; the reward never duplicates.
B10. **Voices + SFX:** all new lines voiced (Seven v3, Cat as "Panther", Guardian, Attendant, Wormmaster if present);
    SFX for the devices, the containment barrier, creature withdrawal, the hatch.
B11. **Checks (20-minute rule) + the spec's acceptance checks (section 11)**, quickly: one player-style run from the
    observation area to the destination screen; skip-equals-watch for C06–C11; save / reload after a device and after
    the record; captures of every new scene's main shots. Publish **0.12.0**.

### Part A — QF11: Chapter 2 can't be replayed from the start + small fixes (Dan, 2026-11-18)

Dan finished Venus, then picked "Chapter 2 — Venus" to replay it and was dropped at the end. Cause (`DemoMenu.cs`):
"Chapter 2 — Venus", Continue and the Scene Select "Venus" entry all just load the `Venus` scene, which resumes the
`VenusState` save.

1. **"Chapter 2 — Venus" always starts the chapter fresh** from the descent / C01: clear the Venus save first; if a save
   exists, ask "Start Chapter 2 over? Your Venus progress will be reset." (Yes / No). **Continue** is the only thing that
   resumes a save.
2. **Scene Select gets real Venus entries**, each starting with the story state, the bike (parked / riding, the bridge
   laid or not) and Panther exactly as in a real playthrough, without touching the player's own save: arrival (C01) ·
   the bike + Panther (B01) · first ride (B02) · the collapse / telekinesis bridge (B03) · temple arrival + guardian
   (B04 / C03) · cube + tremor (C04) · lower gardens + signal (B05 / C05) · and, once built in Part B: the control
   chamber (C06) · the return ride (B06) · the record (C09 / C10) · the debrief (C11).
2b. **Panther walks in place in the opening cutscene** (Dan, playing Chapter 2 from the start: the arrival, C01 — and
   check Chapter 1's opening too). When he isn't moving he must stand, sit or idle, never play the walk cycle on the
   spot. Fix it at the root, in the cat's animator for every scene: the walk / trot blend is driven by his real ground
   speed (0 → idle, with a short blend), and any scripted "stand here" / "look at the flyers" beat puts him in idle or
   sit. Check: log his speed vs the playing clip through C01, B01 and the Chapter 1 opening — no walk clip while his
   speed is ~0; one capture of each.
2c. **The hover bike sticks out through the saucer's hull before the hatch opens** (Dan's screenshot
   `Docs/Previews/qf11/dan_bike_through_hull.png`: the hatch outline glowing, and parts of the bike poking out through
   the hull / visible in it). Nothing of the bike may show until the hatch is open: keep it hidden (or fully inside an
   interior bay that doesn't intersect the hull — it's now ~3 m long, so check it still fits) until the panel has
   opened, then it glides out. Same when it goes back in (B07). Check with captures from outside the ship before,
   during and after the hatch opens.
2d. **Opening shot of Chapter 1: the Moon, then through the crust to 7** (Dan, 2026-11-18 — establishes that they're
   on the Moon). Before the cell scene starts: open on the Moon hanging in space (grayscale, stars, Kyle's glow — the
   same Moon look as the galley porthole), a slow push-in that accelerates toward the surface, the camera **dives
   through the crust** (rock strata, dust and darkness rushing past, a few seconds), then into the mine tunnels / the
   prison block, and settles on **7 in her dark cell** (energy point dark) — straight into the existing opening.
   ~15–20 s, no dialogue, skippable (A / Esc), plays on "Chapter 1" from the menu and Scene Select's first entry, not
   on Continue. **Don't reveal the hollow interior or the city** — that stays the scene 6b surprise; the dive goes
   from the surface straight into the tunnels. Sound: a low rumble through the crust, then the cell's ambience;
   `moon_prison.mp3` fades in as she appears. **Title card (Dan, yes):** over the Moon at the start, the
   logo (`Art/Logo/logo_7_observer.png` — "7" + THE OBSERVER, the existing Arial Bold image) fades in, holds ~3 s,
   fades out before the push-in speeds up; soft glow, centred, not covering the Moon awkwardly. Captures: the title, the Moon, mid-dive, arriving at the cell.
2e. **The city reveal (scene 6b) is rewritten by Dan** — `Docs/DIALOGUE.md` → "6b The city reveal — rewritten by Dan,
   2026-11-18" is the script; it replaces both old 6b line sets (drop their voice takes). It plays right after the
   Scree fight. What changes:
   - **The city is alive:** nearby buildings with layered balconies, recessed windows, bridges and smooth towers built
     around much older structures; **thousands of tiny white lights** in rooms and streets; the city **curves upward**
     along the inner surface; **cloud banks** drifting between districts hiding tower bases; **streams of vehicle
     lights** on elevated roads and airborne traffic lanes, and headlights moving far overhead across what looks like
     the sky. Near buildings show individual windows; far ones dissolve into clusters of light along the curve.
     Grayscale only — the lights are white points / streaks; animate the traffic (moving light streams) cheaply.
   - **Shots, in order:** over 7's shoulder — the reveal (hold before anyone speaks) · low two-shot, 7 and the cat
     looking up (she tilts her head back, he follows her gaze) · extreme wide, their small figures at the exit (hold;
     the city's distant hum — traffic, wind, faint activity — replaces the prison sounds) · the cat watches her, not
     the skyline · she starts after him, looks back once more · **her view: the lower districts** — mining
     structures cutting into the ancient city, excavation lights on broken foundations, vast machinery disappearing
     beneath inhabited terraces; beauty and damage in one frame. Then control returns.
   - **New lines (6 — voice them: Seven v3, Cat v2):** "I've never seen the inside of a moon before." / "Most people
     insist on looking at the outside." / "Are those cars?" / "Apparently we escaped just in time for traffic." /
     "There are people living all the way up there." / "Come on. Let's find your ship."
   - New SFX: the city's distant hum (traffic, wind, faint activity) as a bed under the scene and the terrace after.
   - Captures of each shot; Panther / the cat fully in frame in the two-shot.
2f. **The saucer looks different in Chapter 1 and Chapter 2** (Dan). Cause: Venus builds its own saucer
   (`FightDemoBuilderVenus.cs`, its own lathe profile / materials / size) instead of Chapter 1's (`BuildBay` in the
   Chapter 1 builder: the ~12 m disc, glowing rim band, low dome, belly glow ring + iris, ramp, silver hull from Round
   10). Make **one shared saucer exterior** (one builder method / prefab) used everywhere the ship is seen from outside —
   Chapter 1's impound bay and liftoff, the Venus landing, and any later world. Chapter 1's look is the reference
   (silver, rim glow, dome, belly iris + beam, ramp); add Venus's storage hatch to it (seamless when closed, as QF10),
   same size in both. Check: the same camera angle of the ship in Chapter 1's bay and on the Venus platform, side by
   side — they must match.
3. **Check + release:** after finishing Venus, "Chapter 2 — Venus" starts at the descent; Continue still resumes; each
   Scene Select entry starts where it says (one capture each). (Released with Part B as 0.12.0.)

## Round 12 — status (2026-11-18): done, exe-verified off-screen; release 0.12.0 (`game-1200`, Latest, published)
**Testing went over the 20-minute rule: about 22 min of game time** (Scene Select test 1.2 min; one autopilot Part 2
run 2.9 min; four player-style Part 2 runs ≈ 13 min — the first three stopped on real bugs (crystal prompt) or on my test
driver (scene change); 2 opening runs, 3 short B01 runs, 4 short 6b runs ≈ 5 min). Muted, off-screen, Below Normal.
Captures: `Docs/Previews/r12/` (`selecttest/`, `part2_auto/`, `part2_player/`, `opening/`, `city_6b/`, `b01_quick/`,
`menu/`, Wormmaster + kneel previews). Monochrome: 0 non-gray pixels in every game capture checked. Audio peak 0 in every run.

**Part A (QF11)**
1. **"Chapter 2 — Venus" starts fresh: done.** It clears the Venus save; with progress saved it first asks "Start Chapter 2
   over? Your Venus progress will be reset." (`menu/00_menu.png`). Continue alone resumes (aboard once she rode back).
2. **Scene Select Venus entries: done** — a "Chapter 2 — Venus >" page in the main and pause menus with 11 entries (C01 · B01
   · B02 · B03 bridge · B04/C03 · C04 · B05/C05 · the way down + C06 · B06 · C08–C10 · C11). Each starts on its own save
   slot (`seven_venus_select`; the player's save is never touched) with the stage, bike (parked / riding), bridge and
   Panther as a real playthrough has them. `-venusselecttest`: all 11 started and captured (`selecttest/`).
2b. **Panther walking in place: done, root cause fixed.** The walk clip now follows his measured ground speed (idle under
   0.06 m/s × scale; the clip rate follows the speed) in `ChapterCat` and the saucer's `CatCompanion`, with a safety net.
   The real cause in C01/B01: his start spot was inside the landed saucer's ground obstacle, so every step was rejected and
   he trod on the spot; steps leading out of an obstacle are allowed now and his C01 spot moved. Logged in every run:
   B01 run 531 walk frames / C01 143, 6b run 815, Part 2 run 466 — **walking in place 0×** everywhere.
2c. **The bike through the hull: done.** The bike is hidden in the bay until the panel has fully opened, then glides out;
   B07 reverses it and hides it inside before the panel closes. Logged: 0 of 2 bike renderers shown while the hull is
   closed (before, during and after) — captures `b01_quick/06–12`, `part2_*/b07_*`.
2d. **Chapter 1 opening shot: done** (`MoonOpening.cs`, ~16 s, skippable A / Esc / Enter). The Moon in space (stars, soft
   glow), the logo fading in, holding ~3 s and out before the push-in speeds up, the surface filling the screen, the dive
   through rock strata, dark tunnels, then the existing title + fade-in on 7 in her cell; `moon_prison` fades in as she
   appears, a rumble through the crust. Plays on "Chapter 1" and Scene Select's first entry, never on Continue. No
   interior / city shown. Captures `opening/01–04`. Drawn in screen space (procedural grayscale textures).
2e. **Dan's rewritten 6b: done.** Old 6b lines and takes dropped; six new lines voiced (Seven v3 / Cat v2; 7's deliveries
   in `seven_delivery.json`). The city is alive (`FightDemoBuilderCity2.cs`): 30 nearby towers with balconies, bridges and
   lit window grids, 10,882 tiny white lights clustered along the curve, 19 animated traffic lanes (elevated roads,
   airborne lanes, headlights far overhead), drifting cloud banks, excavation light clusters. Shots in Dan's order: over
   her shoulder (hold) · low two-shot looking up (she tilts her head back, he follows her gaze) · extreme wide · the cat
   watches her · she looks back once more · her view of the lower districts. Cat in frame: 0.0 % off screen in both cat
   shots. New ambience `amb_city_life` under the scene and the terrace after. Captures `city_6b/37–42`.
   *Weakest shot:* the extreme wide looks down on them from above the host face (prison side), not against the city.
2f. **One shared saucer: done** (`FightDemoBuilderSaucerExt.cs`): Chapter 1's 12 m silver disc, rim glow, dome, under-band,
   seams, belly glow, probe and fill — now also on Venus, with the seamless storage hatch cut into it in both chapters.
   Venus re-placed round the bigger ship (ramp, bay, bike spot, Panther's spots). Side by side, same camera offset:
   `city_6b/43_saucer_side_by_side_moon.png` and `b01_quick/13_saucer_side_by_side_venus.png` — the same ship.
3. **Checks: done** (above).

**Part B (Venus part 2)**
B1. **Leftovers.** *The guardian's kneel: fixed at the cause* — Dan's Mixamo "kneel" never kneels (hips and head at standing
   height in all 141 frames, measured); a real one-knee kneel is made in Blender (`Tools/Blender/make_kneel_pose.py`) and
   replaces the state. *QF8's two camera spots:* the slot right of the lunar door (where the sweep measured 0 %) is closed
   off (nothing needs her there); the follow camera now swings 0.15 s after occlusion while she hangs (was 0.4–0.8 s) for
   the mining-cut climb-down. **Not re-measured** (no `-camsweep` / Chapter 1 run this round — time).
B2. **The way down: done.** Root gate (sinks after C05) → an open root pit with steps cut into the hillside → root tunnel →
   root chamber (hanging roots, glowing root bulbs) → deep tunnel → the control chamber. Walkable both ways, no exact-spot
   triggers (hint + skip). Below ground: `venus_underground` cross-fades in (logged at the threshold), cave ambience, the
   light and fog blend to a cave look; ducked under dialogue by the music system.
B3. **Wormmaster: done.** `cleanup_r11.py wormmaster`: 45k tris, 3.8 m, Humanoid-compatible Mixamo-named skeleton (Unity:
   avatar valid) + two curl bones at the end of each tentacle arm + `HeadTip`. `WormmasterRig.cs`: sways above his
   machinery, arms raised and slowly curling, a sweep-up gesture for the signal pulse, the head tip swells and glows with
   his voice (no mouth); contained = arms lowered and curled, body bowed. Voice: Dan's ElevenLabs voice "wormmaster" (9
   lines). Small worms reuse `worm_v1`. Previews `wormmaster_sheet.png` (incl. a curl test).
B4. **VEN_05 + the encounter: done.** Wormmaster on a dais above his machinery, grayscale underlighting; three devices with
   cables to the dais, each pulsing (ring + sound) and driving the creatures of its trough; troughs telegraphed by dust and
   a rumble before a creature surfaces; walkways safe. North device = X; ledge device = climb the 1 m wall (anywhere) + X;
   crystal device = telekinesis from the walkway. A creature that reaches her knocks her back (white fade, back on the
   walkway, nobody hurt). Each device saves; when all three are off the creatures withdraw (alive) and the console closes
   the containment barrier (saved); only containment completes it. C06 (low angles, over his shoulder, level on her) and
   C07 (level angles, machinery silent). Runs: autopilot 1 knock-back, player run 0 / 2; creatures harmed 0.
B5. **B06: done.** Pulses stop, basin creatures calm (routes away from the foundations), three workers back on the paths,
   water runs in the repaired channel, the gully / rubble / broken channel pieces stay; the five lines, the calm creature.
B6. **C08 → C09 → C10: done.** C09 plays `vision_02_venus.mp4` full screen (skippable; still fallback
   `Resources/Venus/vision_02_still.jpg`, `-novideo`), then the match cut to her empty palm (framed from above on the hand
   only), the eye close-up, the new C09 lines, the two-shot. C10: the small cube from the guardian, awarded once (log:
   awards 1), the original stays in its structure; the insert shows both shapes in her hands (from above).
B7. **B07: done** — the ride back over the slab bridge, the lines near the ship, the hatch opens, the bike glides in, the
   hull seamless again (captures `b07_01–04`).
B8. **C11: done** — Panther (normal size) near the entrance, follows her to the table, the dodecahedron and the cube set
   together, insert of both, table-height two-shot, open-hand insert, he settles closer in the silence, the final wide.
   Seat check 0 bad frames. (Panther is on the table, not over her shoulder: no over-the-shoulder shots in C11.)
B9. **Destination screen: done** — after C11, "Set course" at her galley seat opens: The Moon (Chapter 1 from its start) ·
   Venus (a return visit with its changed state; "Board the saucer" at the ramp) · Stay aboard. Nothing invented,
   nothing forced. `VEN_S08_COMPLETE`.
B10. **Voices + SFX: done** — 42 new Venus lines (Seven v3, Panther, Guardian, Wormmaster), 6 new 6b lines; SFX
   `amb_underground`, `machinery_hum`, `creature_rumble`, `device_off`, `barrier_close`, `creatures_withdraw`,
   `amb_city_life` (the hatch reuses `hatch_iris`).
B11. **Checks: done.** Player-style run (virtual gamepad): **`VENUS PLAYTEST 2: 1/1, soft-locks 0`**, observation area →
   destination screen. Autopilot (every line skipped) with a reload after every committed scene **and after the first
   device**: all restored (devices, containment, record); **skip = watch** (the same stage and scene list, `VEN_S08`);
   record awards 1. Bugs found and fixed on the way: the crystal's telekinesis prompt needed her to face it within 40°
   (a player on the walkway stood there; now generous), B06 left her mounted, the hint named the wrong device.

## For Dan to check (Round 12)
- Menu: "Chapter 2 — Venus" with a save → the confirm; Scene select → "Chapter 2 — Venus >" (each entry).
- Chapter 1 from the menu: the new opening shot (skip with A); 6b with the living city (traffic, lights) and its new lines.
- The saucer: the same ship on the Moon and on Venus; the hatch / bike in and out (nothing through the hull).
- Venus part 2 start to finish: the way down, Wormmaster, the three devices (cross the troughs after the dust passes),
  the barrier, the ride back, the vision + match cut, the cube, the debrief, Set course.
- The guardian's kneel in C03. Panther's walking in C01 / B01.
- Open / not verified: QF8's mining-cut camera (not re-measured); the 6b extreme wide is the weakest frame.

## Quick fix QF10: Venus fixes from Dan's 0.11.0 play (Dan, 2026-11-18) — done, status below
Commit first. Standing rules + 20-minute testing rule; look at your own captures before reporting. Spec changes are
already in `Docs/CHAPTER2_VENUS.md` (VEN_B01, VEN_B03) — it wins.

Dan got stuck at "Find a way round the collapse". Causes found by the chat: the detour mantle only works within 0.9 m
of one point (`VenusLayout.DetourMantle`), nothing shows where, and `VenusDirector.Until(...)` only times out for the
autopilot — a real player waits forever.

1. **Climb anywhere it makes sense.** Mantles and hang-downs (`MoonTraversal.Mantle` / `HangDown`, both points today)
   become segments along the whole climbable edge, like `Ledge` — in Venus *and* Chapter 1. "Climb up" shows anywhere
   along a wall low enough to climb.
2. **The collapse is crossed with telekinesis, on the bike (new VEN_B03, Dan).** Replace the on-foot detour: in the
   stopping area 7 gets off, a long fallen slab / broken column lies beside the gap; with **RT telekinesis** she lifts it
   and lays it across the hole as a bridge (it settles with dust and a heavy thud), gets back on and **rides over it**.
   The slab stays as a bridge for return trips. Make the slab obviously the thing to lift (it's the right length, lit
   edge / the telekinesis highlight when she looks at it); objective "Lay something across the gap" → "Ride across".
   Keep the garden wall climbable (item 1) for exploring, but it isn't the solution any more.
3. **Grass, like Kyle's art.** Venus's gardens get soft grass: tall, swaying grass on the terraces, slopes and verges
   along the road, like `Art/Reference/kyle_world_mood.jpg` ("Grassy rise in the fog") — grayscale, catching the light,
   fading into the fog. GPU-instanced / cheap; not on the road surface itself.
4. **The bike's appearance must be elegant** (Dan didn't like it). New VEN_B01 beat: a seamless panel on the saucer's
   hull glows along its seam and **slides / irises open** with a soft light from inside; the bike **glides out on its
   hover glow**, turns slowly and settles beside her at riding height with a low hum — unhurried, graceful, no pop-in.
   **Afterwards the hatch closes and the hull is seamless again** — the saucer looks normal. Same in reverse when she
   parks it back in VEN_B07 (Round 12).
5. **The guardian's legs clip through his cape.** Fix the cape so legs never pass through it: split the cape onto its
   own bones (spine + hip-following chain) or a simple cloth / collision setup; walking, kneeling and standing must all
   be clean. Check captures of each.
6. **Camera after the cube (VEN_C04, from the tremor on): it stays in one place on the least interesting thing.** Give
   C04 (and check C03 and C05 the same way) real coverage per the spec's cues: wide chamber, over-7's-shoulder to the
   guardian and to the cube, the attendant's arrival, falling dust insert, the guardian towering (low angle on him,
   level on her), medium two-shots for the exchange, cut on speaker change, always framed on who's speaking or reacting.
   Never hold a shot on scenery while people talk. Look at every shot of C03–C05 yourself.
6b. **VEN_B01 rewritten (Dan, 2026-11-18 — the spec has the final text, VEN_B01_06–16):** after "Panther." he stretches
   and grows; 7 steps back catching the fairing; new exchange ("You can do that?" / "Apparently." / "We crawled through
   a prison drain." / "An excellent time to be small." / "You're not getting a bigger bowl." / "We can discuss that
   when you return."), then "Look after the ship, Panther." / he lies down large / "I intend to." Regenerate
   `venus_dialogue.json` + `VENUS-LINES.md` from the spec, voice the new lines (Seven / Cat voices; drop the old
   B01_09–13 takes), new beats: her hand catching the fairing, her look from his paws to the saucer, the head shake.
   **Keep Panther fully in frame** for the whole scene — he half disappears off screen today. Every B01 shot after
   the growth must contain all of him (frame for his large size: wider / pulled back / lower), checked by measuring his
   screen bounds in each capture (0 % off screen).
6c. **The hover bike feels small** (Dan). Scale it up so it reads as a substantial machine under her, matching Kyle's
   sketch proportions (rider small against a long bike — about 3 m long, up from 2.3 m). Re-place the seat point and
   the grip targets after scaling, and re-check her riding pose reaches the grips.
   **Her riding pose: there is no Mixamo clip for it** (Dan looked — no straddle / motorcycle clip). Make it yourself:
   author a riding pose in Blender on 7's rig (legs astride the saddle, knees bent and gripping the body, feet back on
   the bike's sides, hips low, torso leaning forward toward the forked front, arms forward) as a short looping clip with
   a subtle breathing / settle; in Unity, IK her hands into the two fairing grips and her feet onto the sides, and lean
   her body into turns (a few degrees, procedural). Check captures from the side and the riding camera: hands in the
   grips (≤ 2 cm), no legs through the bike, clearly astride — not the chair pose.
6d. **Fighting still feels clunky** (Dan — said before, still true after Round 8's "fluid fighting" and the QF9
   tutorial). She should feel **fast, light and easy to control**. Concretely, in the shared fighting system (Moon
   tutorial, fight demo, anything later):
   - **Snappy starts:** a strike's first active frame within ~0.12–0.15 s of the button; play attack clips faster
     (trim wind-ups, ~1.3–1.5× speed) without losing readability.
   - **Cancels + buffering:** any attack's recovery can cancel into dodge or block; the next attack chains from the
     previous one's second half; inputs pressed up to ~0.2 s early are buffered, never dropped.
   - **Light movement:** quick acceleration / deceleration and turning in fight stance (no sliding, no heavy inertia),
     a short fast dodge with brief invulnerability, she can reposition between hits.
   - **Gentle aim assist:** attacks turn her toward the nearest opponent in front of her (soft lock), so hits connect
     without precise stick work.
   - Light hit feedback: a 2–3 frame hit-pause and a small camera nudge on contact.
   - Report the before / after numbers (button → active frame, recovery → next action) for each move, and capture a
     short sequence of a combo, a dodge-cancel and a block. Still nonlethal against the Scree.
6e. **The Scree get a voice** (Dan made it: `Unity/Assets/Audio/SFX/scree_chirp.mp3` — a buzzing, chirping insect
   call, 2.7 s, 48 kHz stereo). Add it to every Scree as 3D positional sound: occasional idle calls (random intervals,
   ~8–20 s, slight pitch variation ±10 % so a group doesn't sound cloned, shorter cuts from the clip too), an alert call
   when they notice 7, and short, sharper chirps for effort / getting hit / going down in the fight tutorial. Keep it
   under the dialogue (duck during lines). Dan wants to "see how it goes" — easy to tune: put volume, interval and
   pitch range in one place and list them in the status.
6f. **Natural ground, not an elevated path (Dan, 2026-11-18 — `Docs/VENUS-TERRAIN.md`, it wins).** Replace the raised
   road / walkway blocks with continuous sculpted ground out to the fog: the route is a path worn into rolling grassy
   land following the contours; terraces are natural shelves of the hillside, channels are streams, the temple grows
   from a hill, the platform sits on a natural rise; boundaries are natural (steep slopes, rock, water, fog); the
   collapse becomes a gully a stream has cut through the path, and the slab bridges it. Keep everything Round 11 placed,
   moved onto the new ground; the grass (item 3) goes on the new terrain. Check: riding-camera captures along the whole
   route + high wide shots, no platform edges / undersides / void, the bike rides smoothly (no snagging).
7. **Player fallback for every Venus wait** (as Chapter 1's QF6): no progress for ~45 s → re-show the objective with a
   hint and briefly highlight the next step; after ~2 min → offer a skip (fade, the step is done). Audit every `Until`
   in `VenusDirector`; no wait that only the autopilot can escape.
8. **Checks + release:** quick player-style run (virtual gamepad, not autopilot) from B01 to C05 incl. the telekinesis
   bridge; captures of the hatch opening / bike gliding out / hatch closed, the grass, the bridge beat, the guardian
   kneeling and walking (cape), every C03–C05 shot, the hint and the skip; publish **0.11.1**; docs; commit + push;
   per-item status in "QF10 — status"; "For Dan to check (QF10)".

### Dan's change during QF10 (2026-11-18)
- **Riding pose = a low racing tuck** (Dan: `Docs/Previews/qf10/dan_riding_pose.png` = before, `Art/Reference/ref_riding_posture.png`
  = posture wanted): hips at the back of the saddle, torso laid forward, arms forward to the grips, knees tucked against
  the sides, feet back on the rear sides. Same checks + a side capture next to the reference.

## QF10 — status (2026-11-18): done; release 0.11.1 (`game-1101`), then 0.11.2 (`game-1102`, Latest) with the hand-grip fix
**Testing went over the 20-minute rule:** about 31 min of game time, plus one dead run (10 min idle on the main menu).
- 3 short B01 checks (about 70 s each).
- 1 full autopilot Venus run (8.4 min; 3.3 min of it was the autopilot stuck behind the bike-in-the-temple bug, found and
  fixed).
- 4 player-style runs at `-testspeed 2`, about 18 min of game time:
  - the first never left the main menu (a menu bug for the new switch, fixed);
  - the second stopped at the slab (the skip prompt hid telekinesis: a real bug, fixed);
  - the third stopped after C03 (my test driver walked 7 back to her bike: a driver bug, fixed);
  - the fourth passed.
- 1 `-fightonly` run (46 s).

Captures: `Docs/Previews/qf10/` (`venus_run/`, `venus_playtest/`, `b01_quick/`, `fight/`, model / pose previews,
`riding_pose_vs_reference.png`). Monochrome: 0 over 248 game captures (only the Blender workbench previews carry a tint).
Audio peak 0 in every run. No exceptions.
Final player run: **`VENUS PLAYTEST: 1/1, soft-locks 0, hints 3, skips 1, bridge laid, coverage 27 line shots, 0 misses,
VEN_S04`**.

1. **Climb anywhere: done.** Mantles are now wall faces, and hang-downs work anywhere along the ledge's climbable range,
   in both Venus and Chapter 1 (`MoonTraversal.MantleHere / MantleDownHere / HangDownHere`). From the top of a mantle
   wall she can jump back down anywhere along it.
   - Player run: "Climb up" showed 2.5 m from the old spot; up ok, then "Jump down", back on the road ok.
   - Chapter 1's terrace B step uses the same code, but no Chapter 1 run exercised it this round.
2. **The collapse is crossed with telekinesis, on the bike: done.**
   - A 12 m fallen slab lies along the gully's near lip; its edges glow when she looks at it.
   - RT: she gestures, the slab lifts, swings round over the gully and comes down across it with dust, a thud and a camera
     shake. The `Bridge` zone opens and is saved for return trips.
   - Objectives: "Stop before the collapse" → "Lay something across the gap" → "Ride across".
   - Both runs crossed it riding.
   - The garden wall stays climbable for exploring. The old on-foot detour is gone (the gully cuts the whole valley).
3. **Grass: done.** 93,268 blades in 76 chunks (Seven/Grass: wind in the vertex shader, catching the light toward the tip,
   back-light glow, receives shadows, casts none). On the new terrain's verges, hillsides and the landing rise; never on
   the path, the paving, the water or the gully.
4. **Elegant bike reveal: done.**
   - A seamless hull panel: its seam glows white, the panel sinks and slides round under the hull, and soft light comes
     from the bay.
   - The bike glides out on its hover glow, turns slowly and settles beside her.
   - The hatch closes and the hull is seamless again: no seam line, panel offset 0.000, bay light 0.
   - Captures `v06a`–`v06g`.
5. **Cape: done.** The cape is on its own two bones (CapeA / CapeB under the upper chest). `ElarimCape` swings each half
   just enough that no thigh, shin or foot capsule (and not the ground) is inside it, then settles back softly.
   - Worst leg contact: 0.0 cm standing, kneeling and walking (both runs).
   - Bug found on the way: the extra bones aren't driven by the humanoid animation, so the swing piled up (the cape stuck
     out sideways). It now resets to rest every frame.
   - **Open:** the "kneels" capture (`v16`) still shows the guardian standing at that moment.
6. **C03–C05 coverage: done.** Every line is framed on its speaker:
   - over 7's shoulder, low on the guardian (he towers);
   - medium on her at eye level;
   - two-shots;
   - a group shot when the attendant speaks to the guardian;
   - inserts that cut to the speaker after a moment;
   - cut on speaker change;
   - the attendant's arrival over her shoulder, and the falling-dust insert.

   Every line is captured and measured (speaker's head in frame and unblocked):
   - autopilot: 34 shots, 1 miss after an eye close-up (fixed);
   - player run: 27 shots, 0 misses.

6b. **B01 rewritten: done.** New lines B01_09–16 (Seven / Cat voices, old 09–11 takes dropped). Beats:
   - her hand catching the fairing;
   - the look from his paws to the saucer;
   - the head shake;
   - she settles into the saddle;
   - he lies down large.

   Panther is framed for his full grown size: **0.0 % off screen in all 11 shots after the growth.**
6c. **Bigger bike + riding pose: done** (the hand grip finished after 0.11.1 and released in 0.11.2; Dan approved the posture).
   - Bike: 3.0 m (was 2.3); saddle, grips and foot points come from the model and are re-measured in game by raycast.
   - Pose: Blender-made (`Tools/Blender/make_ride_pose.py`, looping breath) and, per Dan, a low racing tuck. Hips are
     held on the back of the saddle, the body is laid forward 42° (+ up to 34° more if the hands need it), she leans into
     turns, and the feet are on the rear side panels (IK).
   - Her arms are at full reach in the tuck, so after the IK each shoulder rolls forward the few degrees that put the palm
     on its grip (`ChapterSevenRide.ShoulderRollMax`, at most 25°).
   - Measured: **palms 1.5 / 1.2 cm from the grips**, hips to saddle 0 cm, feet 0 cm, **legs through the bike 0 of 28
     points**.
   - Two extra short B01 checks (70 s each) after the release.
   - `riding_pose_vs_reference.png` is the side capture beside the reference.
6d. **Fighting: done** (chapter fights + the fight demo). Measured in play, before → after:

   | Move | Button → active frame (s) | Recovery → next attack (s) | Recovery → dodge / block (s) |
   |---|---|---|---|
   | jab | 0.58 → 0.13 | 0.54 → 0.04 | 0.86 → 0.05 |
   | cross | 0.77 → 0.13–0.16 | 0.55 → 0.04 | 0.94 → 0.05 |
   | hook | 0.55 → 0.13 | 0.26 → (end of combo) | 0.49 → 0.14 (measured) |
   | takedown | 0.68 → 0.13 | — | — |

   - Dodge: 0.90 m in 0.35 s → 1.15 m in 0.22 s; invulnerable 0.30 s; can act again after 0.26 s (was 1.90 s).
   - 0.2 s input buffer on every move; quick acceleration in stance.
   - Soft lock: 70° cone, 3.4 m.
   - Hit-pause: 3 frames (4 for a takedown), plus a camera nudge.
   - Clips at 1.45× (fight demo 1.35×).
   - Bursts captured: `qf10_combo_*`, `qf10_dodge_cancel_*`, `qf10_block_*`. Still nonlethal: both Scree down and alive.
6e. **Scree voice: done.** `scree_chirp.mp3` (the SFX bank now takes .mp3 too), 3D on every Scree:
   - idle calls at random intervals with pitch variation and short cuts;
   - an alert call when startled, when one sees her, or when the fight starts;
   - effort / hit / dazed / down chirps;
   - ducked under dialogue.

   Fight run: 19 idle calls, 2 alert calls, 18 fight chirps. **Knobs** (`ScreeVoiceSettings` in `Runtime/ScreeVoice.cs`):

   | Knob | Value |
   |---|---|
   | Volume | 0.8 (idle calls 0.75 of that) |
   | Distance | 2–26 m |
   | Idle interval | 8–20 s |
   | Pitch | ±10 % |
   | Short cuts | 60 %, 0.5–1.4 s |
   | Alert | 1.6 s at ×1.12, 6 s cooldown |
   | Effort | 0.30 s at ×1.25 |
   | Hit | 0.22 s at ×1.35 |
   | Down | 0.55 s at ×0.85 |
   | Under dialogue | ×0.3 |
6f. **Natural ground: done** (`Runtime/VenusTerrain.cs`, `Editor/FightDemoBuilderTerrain.cs`).
   - One height function for the builder and the game (the `Valley` ground zone = height field + walkable mask):
     - a 197 m path worn along a curved centreline that dips off the landing rise and rolls;
     - rolling hills with soft garden shelves either side, dark rock where steep;
     - the platform on a rise; the basin as lowland to the east;
     - a stream beside the path that has undercut it into the gully.
   - A 0.8 m mesh over the playable valley and the hills seen from it, and a coarse mesh out to the fog.
   - Lamps, marker stones and grown arches along the path.
   - Natural boundaries: steep hills, the gully, the water, the fog.
   - Everything Round 11 placed is on the new ground (workers re-seated on it). The bike pitches to the slope (no
     snagging).
   - Captures: `high_wide_0–3` and `route_ride_00–08` (the riding camera along the whole route). No edges, undersides or
     void.
7. **Player fallback: done.** Every Venus wait now has a hint, a step beacon and a skip action (no wait only the autopilot
   can escape).
   - After 45 s without getting closer, the objective comes back with a hint and the step is lit briefly.
   - After 2 min, "Skip this step" (Y / Enter) is offered: a white fade and the step is done.
   - `-venushintfast` = 12 s / 30 s for checks. Player run: 3 hints, 1 skip taken.
   - The skip offer no longer hides the step's own action.
   - Also fixed: the bike was "summoned" into the temple when she was in the cube chamber; it now only comes where it can
     go.
8. **Checks + release: done.** 0.11.1 published.

## For Dan to check (QF10)
- The hatch and the bike gliding out; B01's new exchange with Panther large (always fully in frame).
- The riding tuck (`Docs/Previews/qf10/riding_pose_vs_reference.png`): posture approved; palms now 1.2–1.5 cm from the grips
  (0.11.2).
- The ride through the new valley: the worn path, the grass, the stream, the gully and the slab bridge (RT).
- Climbing the low garden wall anywhere; the hint after 45 s and the skip after 2 min if you wait.
- C03 / C04 cameras; the cape when the guardian walks and kneels (the kneel itself may not show — tell me).
- Fighting feel on the terrace (and the fight demo); the Scree voice (volume / interval knobs above).

## QF9 (Dan, 2026-11-18) — done, status below
Round 11 is paused after stage 1 (committed). Do QF9 first, release it, then resume Round 11 at stage 2 (below) —
and don't stop for Dan at stage 2: save the model previews to `Docs/Previews/r11/` and keep going.

### Chapter 1: the fighting tutorial at the exit + the door-frame flicker (Dan, 2026-11-18)
Commit first. All standing rules apply (monochrome, 7 never kills, the Scree are oppressed people — not monsters),
**20-minute testing rule**, look at your own captures before reporting.

1. **Door frames still flicker** (Dan, playing 0.10.5, in the last chamber before the exit — the lunar-door / vision
   chamber). QF8 marked it "done (cause fixed; not watched in motion)" — it isn't fixed. This time **watch it in
   motion**: record a short sequence of consecutive frames while 7 walks and the camera orbits through that chamber
   (and every other chamber with door frames), measure each door frame's brightness frame to frame, and confirm there
   is no black/grey switching. If the occluder fade is the cause, door frames and other architecture must never fade —
   only the camera pulls in. Report the measurement.
2. **Fighting tutorial with the two Scree at the exit.** When 7 leaves the base, the two Scree there (right before the
   city reveal — find which pair Dan means: the two at the exit / gate on the way out) become **the fighting tutorial**:
   - They block the way out and engage her. Reuse the existing fighting system (the "fluid fighting" from Round 8 /
     the fight demo): its moves, blocks, dodge, hit reactions.
   - **Teach the controls as they're needed**, the same way traversal is taught (first-use prompts, no permanent legend):
     attack, block, dodge, then the finishing move. Keep it short and forgiving — a tutorial, not a challenge.
   - **Nonlethal, always:** the Scree are knocked down / dazed, not killed. The finisher becomes a **takedown** that
     leaves them on the ground, stunned, alive (existing clips: knocked down, dazed, hit reactions; they may sit up
     groggily and stay out of the way). No blood, no deaths, no cruelty. 7 fights to get past them, not to hurt them
     (scene 5's "Without hurting them" still holds).
   - If 7 is knocked down, a quick reset to the start of the fight (existing fight reset), no game over.
   - **When both are down, the city reveal (scene 6b) plays.** Same trigger rules as QF6: no point of no return, a
     watchdog, the fight can't be skipped past by walking around them, and it can't soft-lock (if something goes wrong,
     the reveal still starts).
   - Panther (still just "the cat" in Chapter 1) stays out of the fight — he keeps his distance and rejoins after.
   - No new dialogue unless Dan writes some; ambient grunts / effort sounds only.
3. **Checks + release** (20-minute rule): one run through the fight (win it, and once lose and retry), the city reveal
   after it, captures of each tutorial prompt, a takedown, both Scree down, and the reveal start. Publish **0.10.6**;
   docs; commit + push; per-item status in "QF9 — status"; "For Dan to check (QF9)".

## QF9 — status (2026-11-18): done, exe-verified off-screen; release 0.10.6 (`game-1006`, Latest)
Testing: about 14.5 min of game time:
- seven `-flickertest` runs (~55 s each) while I narrowed down the cause;
- one `-ch1cp 6` run through the fight, the reveal and on to checkpoint 7;
- two short `-fightonly` re-checks.

Captures: `Docs/Previews/qf9/` (`fight/`, `flicker_before_0.10.5/`, `flicker_after/`, `zfight_check_Chapter1.txt`). I looked at
the event frame pairs, every prompt shot, the knockdown, the takedowns, both Scree down and the reveal start. Monochrome: 0
over 112 images.

1. **Door-frame flicker: done — measured in motion.**
   - *Cause:* z-fighting. The level is built from overlapping stone blocks, and 207 pairs of them had faces in exactly
     the same plane, facing the same way. Examples: the exit passage's lintel underside and the ledge block above it
     (both at y 0.6), and the floor tops at the mining-cut exit. Where the two blocks have different materials (dark vs
     pale stone), the surface switched black / grey as the camera moved. That's the doorway in your screenshots.
   - *Fix:* every Chapter 1 build now finds every such pair and moves one face out by 4 mm. Build log:
     `167 found, 172 moved, 0 left`. In-game check: `Z-FIGHT CHECK: 0`.
   - *Also:* architecture (frames, walls, columns, ribs, arches) never fades for the camera any more; only loose props
     (chunks, rubble, crates, pipes) may. The shadow bias was raised to Unity's standard values (depth 1, normal 1) and
     the main shadow map to 4096, against the striped shadow artefacts.
   - *The measurement (`-flickertest`, door open):* 7 walks four routes (through the lunar-door chamber, along the
     chamber's rib corners, through the exit doorway, past the cell-block gate) while the camera orbits. Every frame is
     rendered and read back, and 78 architecture meshes are sampled at points inside their surfaces (not edges, not
     corners). A flicker event = ≥ 30 % of a mesh's points jumping by more than 0.2 in one frame while the view is
     nearly still.
     - **0.10.5:** 94 events (VisionExit_Ceiling 35, VisionExit_Lip 45, the exit walls 14), 572 frames with
       architecture faded.
     - **0.10.6:** 0 real events over 3,173 frames, 0 faded. 7 logged events remain; I checked each frame pair
       (`event_*`): 7's own hair swinging across the sample points, and the 3 cm lip strip seen from 9 m.
2. **The fighting tutorial: done.** The two Scree on the terrace, right after the mining-cut climb and before the city
   reveal:
   - They come between her and the way out (`05`).
   - She fights on her own controller: jab → cross → hook combo, hold Block, Dodge, and the finisher as a takedown.
   - Prompts appear once each, when first needed:
     - Attack: at the start, X / left mouse or F (`07–08`).
     - Block: at the first wind-up, LT / Q (`09–10`).
     - Dodge: at the next wind-up, B / Ctrl (`11`, `13`).
     - Takedown: when a Scree is dazed, Y / E (`15–16`). Later dazes show it as a quiet context prompt.
   - One Scree attacks at a time, with a slow punch and a held wind-up. 3 blows daze a Scree. A dazed Scree recovers if
     left alone.
   - *Nonlethal:* a takedown puts a Scree down; it lies stunned, then sits up groggily and stays out of the way (`14`,
     `17–18`). No blood, no deaths.
   - *Losing:* 3 unblocked blows put her down on one knee → white fade → the fight restarts (`12`). Run: lost once on
     purpose, then won in 38 s.
   - *Rules:* an invisible arena means the fight can't be walked around or fallen out of. Watchdog: 90 s with no blow →
     both Scree sit down and the reveal goes on. `Docs/TRIGGER-AUDIT.md` row 18b.
   - The cat waits at the top of the cut and rejoins afterwards. Sounds only (5 new ElevenLabs SFX: swing, padded hit,
     block, Scree grunt, body fall); no new lines.
   - Both down → the city reveal (`19_qf9_reveal_start`), then on to checkpoint 7.
3. **Checks + release: done.** 0.10.6 published (`game-1006`, Latest). Soft-locks 0, watchdog fallbacks 0, audio peak 0
   (muted).

## For Dan to check (QF9)
- The vision-chamber exit doorway and the mining-cut exit: move and turn the camera; nothing should switch black / grey.
- The terrace fight: the four prompts, blocking and dodging the slow punch, the takedown, losing once (the restart).
- Whether the Scree's "sit up groggily" pose reads right (it's the first part of the bot's get-up clip, held).

## Round 11: Chapter 2 "Venus", part 1 — done (first playable pass), status below (moved here from Docs/ROUND11.md, 2026-11-18)
Landing → gardens → temple → the signal (Dan, 2026-11-17).
**The spec is `Docs/CHAPTER2_VENUS.md` — read all of it first; it wins over this summary.** It is split into two rounds:
- **Round 11 (this):** spec sections 1–6 for VEN_01–VEN_04, the progression / persistence system, the dialogue data
  format, and scenes **VEN_C01–VEN_C05**; **the hover bike + scenes VEN_B01–VEN_B05 (spec section 6b, added 2026-11-18)**; plus the trip from Chapter 1 to Venus.
- **Round 12 (later):** VEN_05 control-chamber encounter (section 7), scenes VEN_C06–VEN_C11 + the bike's VEN_B06–VEN_B07, the vision, the record,
  the saucer debrief and destination selection, return-visit states (section 9), acceptance checks (section 11).
Commit the repo as it is first. Follow spec section 10's build order: **a complete playable path before cinematic polish.**
All standing rules apply (monochrome, Kyle's fog/glow, non-sexual presentation, 7 never kills).

1. **Reuse first** (spec: "inspect what already exists"): 7's controller, traversal, teaching/context prompts,
   interaction, dialogue player + subtitles + telepathy treatment, pause menu, save/Continue, the cat, the saucer
   interior, the Atmosphere setup. Extend them; don't fork them.
2. **Dialogue as data:** every line gets a stable ID (`VEN_C03_04` style, exactly as in the spec), speaker, text,
   delivery type (spoken / telepathic / in-gameplay) and an optional camera cue, stored outside gameplay code
   (JSON or ScriptableObjects) so Dan can edit lines, order and cues without touching logic. Camera cues are
   optional: if one can't run, the scene still progresses. Skipping a scene applies its result exactly once.
   No two conversations at once; the cat's ambient lines queue during important scenes. (Chapter 1 can migrate later.)
3. **Progression + persistence** (spec section 5): stages `VEN_S00_AVAILABLE` … `VEN_S08_COMPLETE` with their objectives;
   persisted separately: disabled device IDs, Wormmaster contained, record awarded, completed one-time conversation
   IDs, optional garden assistance. Scene completion = committed checkpoint; an interrupted scene restarts from its
   beginning. Build the whole system now even though Round 11 only reaches `VEN_S04`.
4. **Getting there:** Chapter 1's "Set course" placeholder now really travels: the saucer leaves the Moon, a short
   transit, sets `VEN_S00_AVAILABLE`, and **descends beneath Venus's clouds** to the landing platform (scene C01). Keep
   it simple — the full destination-selection screen comes in Round 12. Main menu: add "Chapter 2 — Venus" (and
   `-chapter2`); Continue resumes wherever the save is.
5. **The world (graybox first, then a Kyle-style look pass):** VEN_01 landing platform with arrival vista; VEN_02 outer
   gardens (inhabited terraces, water channels, damage, a **blocked main walkway** and an **alternate route that uses
   the existing traversal** — jump / ledge / shimmy / climb); VEN_03 temple (guardian area + protected cube chamber);
   VEN_04 lower gardens (damaged channels, a **creature observation area**, the route down toward VEN_05 — gated for
   now). Look: Venus = **the First Garden Age** — lush gardens and terraces in grayscale, flowing water, tall pale
   towers (some unfinished), soft bright diffuse light under a thick cloud ceiling, Kyle's fog/glow and many depth
   layers; refs `kyle_world_mood`, `kyle_7_lake_moon`, `kyle_7_cliff_light`, `kyle_7_riding_city`. Elarim architecture:
   organic and grown-looking rather than built (fits the garden age), consistent across the temple and terraces.
6. **Characters (placeholders until Kyle's designs exist — name them `_placeholder`):**
   - **Elarim** (guardian, attendant, workers) — **design from Dan/Kyle, 2026-11-17:** **twice as tall as a human
     (~3.5 m), all male, superhero proportions** (Atlas / Superman), handsome, perfect, flawless; **shining polished
     armor**; they **glow** softly all over, and their **eyes glow brighter still**; long **flowing capes**; some carry a
     **sword**. Very intimidating — but they are the good guardians, calm and dignified, never hostile to 7. Until
     Dan's Tripo model arrives (`Art/Source/elarim/`), use a placeholder with those proportions (scale a humanoid to
     3.5 m, armor-like shell material, cape as a simple cloth or rigid sheet, emissive eyes + soft glow), named
     `Elarim_placeholder`. Retargeted idle / walk / look-around; workers do repair loops (steadying a vessel, tending
     channels). **UPDATE 2026-11-18: the real models are in — use them instead of placeholders:**
     `Art/Source/elarim/elarim_v1.glb` + `elarim_sword_v1.glb` (Blender: decimate, split the cape off as cloth, scale to
     3.5 m, Humanoid-compatible rig like the Scree; clips in `Art/Mixamo/elarim/` — idle, talk, point, walk, kneel,
     sword idle, carry, lift; guardian kneels to 7's level in C03/C10), and the burrowing creatures =
     `Art/Source/wormmaster/worm_v1.glb` (simple bone chain: rise / wriggle / dive). Wormmaster (`wormmaster_v1.glb`)
     waits for Round 12. Placeholders only if a model fails, and say so. Scale the temple and garden doorways, steps and walkways for 3.5 m figures — 7 should look small
     beside them; frame C03/C04 so the guardian towers over her (low angles on him, level on her).
   - **Burrowing creatures:** large segmented worm-like creatures that surface and dive, with ground ripples and
     dust; not menacing in themselves. Only seen at a distance this round (C02 ground, C05 observation area).
   - **Flying creatures** for C01 (the cat's head tracks them): small pale gliding shapes high in the haze.
   - **The cat** comes out to the platform for C01 (spec: two-shot of 7 and Cat), then **stays aboard** — his C02 and
     C05 lines are telepathic from the ship.
7. **The Venus cube (the original, in the temple):** match the dodecahedron — pale stone, glowing edges, **the same
   Elarim marking** on one face (still Code's placeholder glyph until Kyle draws it; the spec's "matching inserts of
   related markings" in C04 compare it with the dodecahedron's). It sits in a protected structure and stays there.
8. **Scenes C01–C05** exactly as the spec (lines, results, camera cues): C01 arrival (extreme wide of the saucer under
   the clouds, over 7's shoulder to the gardens, two-shot, landscape insert, the cat tracking flyers); C02 tremor in
   gameplay without locking movement (water shakes in a vessel, a worker steadies it); C03 meeting the guardian (the
   guardian examines the dodecahedron without taking it); C04 the cube chamber and the interruption (falling dust,
   the attendant, 7 puts the dodecahedron away, route to the lower gardens opens); C05 the signal (in gameplay: a
   signal pulse and the creatures **visibly change direction**). Eye close-ups: steady gaze or gaze shift only.
9. **Voices:** 7 and Cat lines with ElevenLabs as before (7 with the expressive settings). Dan is creating voices named
   **`Guardian`** and **`Attendant`** (and later `Wormmaster`) — use them if they exist; if not, the scenes play with
   subtitles only (spec: must work without recordings) and say which lines are waiting for a voice.
10. **Music / SFX:** `venus_gardens.mp3` in `Unity/Assets/Audio/Music/` if Dan has added it (silence otherwise); SFX via
    ElevenLabs: garden ambience (water, birdlike calls, wind in leaves), tremor rumble, falling dust, vessel water
    slosh, creature surfacing/burrowing, the signal pulse (a distinct, slightly unsettling tone), saucer descent.
10b. **The hover bike (spec section 6b, scenes VEN_B01–VEN_B05).** Model: `Art/Source/hoverbike/hoverbike_v1.glb` if
    Dan has added it (Tripo from `Art/Reference/hoverbike_ref.png`), else a placeholder built to that reference — the
    **forked end is the front**, long pointed tail, low saddle, two recessed grips in the fairing, no handlebars; glossy
    pale grey; a soft hover glow + hum from the round underside vents. Riding: steer / accelerate / brake on the left
    stick and triggers, its own riding camera, clear mount / dismount prompts (dismount only on safe standing space),
    a simple recovery action if it gets stuck, the bike's parked spot + riding state saved. 7's riding pose: seated,
    leaning forward, hands IK-placed in the fairing grips (a basic mount transition is fine). Teach riding through the
    broad first route (B02), not with pop-ups beyond the usual first-use prompts. Routes: the broad first ride through
    VEN_02 to the temple court (B02, B03 carrying C02 — collapsed walkway, short on-foot detour), parking at the temple
    court (B04 → C03/C04), the narrower maintenance path through VEN_04 to the observation area (B05 → C05). The bike
    never enters the temple or the underground. Workers and creatures are given way to — no collisions rewarded.
    Same trigger rules as Chapter 1 (fire at-or-past, watchdogs, no points of no return); if 7 is on foot far from
    the bike when the story needs it, the bike can be summoned to her.
10c. **Panther (VEN_B01, Dan 2026-11-18):** the cat's name reveal and his first size change. New cat animations in
    Blender on his rig: a **stretch** (front legs forward, back arched, tail up) during which he scales smoothly from
    normal size to a large, powerful cat; a **lie down** (large, beside the ramp). Uniform scale, no pop or flash; keep
    him grounded (feet on the floor while scaling). His subtitle label becomes **"Panther"** from VEN_B01_07 on
    (everywhere after; earlier lines stay "Cat"). He must be visible at the ramp for the whole scene.
11. **Checks** (muted except a brief reported sound check; off-screen; never steal focus): rebuild; **20-minute testing rule** — one quick run from
    Chapter 1's "Set course" through C05 with no soft-locks (on the bike where the story rides); the alternate route completable with existing moves;
    skip each scene and confirm the same stage results as watching; save/reload at each stage restores correctly;
    captures of each scene's main shots + each area; monochrome 0. Publish a release, update `TODO.md` / `CLAUDE.md`,
    commit and push, report in ~20 lines, including what's still placeholder.

## Round 11 — status (2026-11-18): done as a first playable pass, exe-verified off-screen; release 0.11.0 (`game-1100`, Latest)
Testing: about 18 min of game time.
- Run 1: the whole of part 1, watched, with a reload after every committed scene.
- Run 2: every line skipped, with the same reloads.
- One short B01 check of the riding pose.

Captures: `Docs/Previews/r11/` (model previews, `cat/`, `venus_run1/`, `venus_run2_skip/`, `venus_b01_quick/`). Monochrome:
0 over 62 Venus images. Audio peak 0. No exceptions in run 2.

1. **Reuse first: done.** 7's controller, traversal, prompts, the dialogue player (subtitles, telepathy, labels), the
   follow camera with the QF8 occluders, capture, the pause menu, the cat and the Atmosphere are all reused, not forked.
   The traversal data is now swappable per world (`MoonTraversal.Use`).
2. **Dialogue as data: done.** `Resources/Venus/venus_dialogue.json`: 103 lines with spec IDs, speaker, label, delivery
   and cue, plus each scene's camera cues. Cues are presentation only (an unknown cue is skipped and logged). Lines never
   overlap.
3. **Progression + persistence: done.** `VenusState`: S00–S08, the separate saves, skip = watch. Check:
   - run 1 (watched) and run 2 (skipped) end in the same state: VEN_S04, the same scenes, 0 record awards;
   - 5 reloads, one after each committed scene, each restoring the right place;
   - `-venusstatetest` passes.
4. **Getting there: done.** "Set course" on the saucer now travels: the Moon falls away, a short transit, then Venus
   (VEN_S00) and the descent under the clouds. The main menu has "Chapter 2 — Venus" (also `-chapter2`, and an entry in
   Scene select). Continue resumes the Venus save. **Not run:** the chain from the galley into Venus (`-venuschain`
   exists).
5. **The world: done (graybox + a first look pass).**
   - VEN_01: the platform with the saucer, ramp and storage hatch.
   - VEN_02: the broad garden road with terraces, water channels, hanging-garden arches, the long bend, the damaged
     channel, the collapsed walkway and the detour (mantle → assisted jump → ramp, all existing moves).
   - VEN_03: the court, the pedestrian garden, a 6.2 m doorway, the hall with grown ribs, and the cube chamber with its
     protected cage.
   - VEN_04: the maintenance path (gated until C04), the observation area, the basin, and the shut root gate.
   - Around it: distant pale towers (some unfinished), the cloud ceiling, Atmosphere_Venus. The fog was thinned after
     run 1 so the gardens read.
6. **Characters: done (real models, no placeholders).**
   - The Elarim are 3.5 m, skinned to Dan's Mixamo skeleton with the cape on the spine, a soft glow and white eyes. All
     8 clips import as Humanoid: the guardian kneels in C03, the attendant walks in during C04, and four workers loop
     their repairs (one with a sword).
   - Worms: 9 m, with rise / wriggle / dive clips, dust where they break the surface.
   - Flyers over the platform.
   - The cat comes out for C01 and B01, then stays at the ship.
7. **The Venus cube: done.** Pale stone, glowing edges, the same placeholder Elarim marking, inside its protected
   structure. It stays in the temple.
8. **Scenes C01–C05: done, as the spec**, with their camera cues:
   - descent (extreme wide), over-the-shoulder to the gardens, two-shot, landscape insert, the cat's head following the
     flyers;
   - C02's tremor in gameplay: the vessel's water shakes and a worker steadies it;
   - C03: the guardian kneels and examines the dodecahedron; it never leaves her (log: still hers);
   - C04: dust falls, the attendant arrives, she pockets the dodecahedron, the maintenance path opens;
   - C05 in gameplay: the signal pulse, and the creatures visibly turn (up to 179° in 3 s, logged).
9. **Voices: done.** 58 lines, by voice name: Seven (v3), Cat (v2; "Panther" from B01_07), and Dan's Guardian and
   Attendant voices. None are waiting.
10. **Music / SFX: done.** `venus_gardens.mp3`, plus 8 new ElevenLabs SFX: garden ambience, bike hum, saucer descent,
    tremor, vessel slosh, dust fall, signal pulse, creature surfacing.
10b. **The hover bike: done.** Dan's model with the forked front detected.
   - Controls: steer on the left stick / A-D, RT / W accelerate, LT / S brake, X / E mount and dismount (dismount only on
     standing space), d-pad down recovery, plus a stuck watchdog and summon.
   - It has its own riding camera, gives way near workers, stays out of the temple and underground, and its spot and
     riding state are saved.
   - Riding is taught on first mount.
   - B02 / B03 / B04 / B05 run as the spec.
   - **Approximate:** her riding pose. She sits on the saddle leaning forward, but the seated clip is a chair pose (knees
     up, no straddle), and her hands reach to 0.18 m of the grips, not into them.
10c. **Panther: done.** He stretches (new Blender clip) and grows smoothly to ×2.7 with his paws on the floor, then lies
   down beside the ramp (new clip). The label reads "Panther" from VEN_B01_07.
11. **Checks + release: done.** 0.11.0 published.

**Still placeholder / open:**
- The riding pose (a straddle clip from Mixamo would fix it).
- The Elarim marking (Kyle).
- The cape is rigid with the back (no cloth).
- The architecture is first-pass graybox-plus.
- Wormmaster and the encounter are Round 12.

## For Dan to check (Round 11)
- Set course on the saucer → Venus: the descent, then C01 on the ramp.
- B01: the bike out of the hatch, "Panther" — the stretch and the growth (×2.7: right size?), him lying by the ramp.
- Riding: steering, accelerate / brake, getting off, the bend lines, the tremor, the collapse detour on foot.
- The guardian and attendant (models, glow, voices, the kneel), the cube chamber, the signal at the basin.
- The riding pose (approximate — say if you want a Mixamo riding / straddle clip).

## Round 11 — stage 1 notes (2026-11-18)
Done (build-checked, `-venusstatetest` passes):
- **Dialogue as data:** `Tools/Voice/make_venus_lines.py` → `Unity/Assets/Resources/Venus/venus_dialogue.json` (18 scenes,
  103 lines, 58 in Round 11; stable spec IDs, speaker, subtitle label with **"Panther" from VEN_B01_07 on**, delivery
  spoken / telepathic / in-gameplay, voice name, optional camera cue; scene trigger / result / camera list) +
  readable `Docs/VENUS-LINES.md`. Runtime loader `VenusDialogue`.
- **Progression + persistence:** `VenusState` — stages VEN_S00…S08 with objectives (forward only), one-time scenes
  applied once whether watched or skipped, disabled devices, Wormmaster contained, record awarded exactly once, garden
  help, bike parked spot + riding; PlayerPrefs JSON (`seven_venus`, test key for check runs). `-venusstatetest` checks
  every rule incl. a reload → `VENUS STATE TEST: all rules hold`.
- Assets present: `elarim_v1.glb` + sword, `worm_v1.glb`, `wormmaster_v1.glb`, `hoverbike_v1.glb`, Elarim Mixamo clips
  (animation only — the Elarim needs a scripted Humanoid rig in Blender like the Scree), `venus_gardens.mp3`.

Next stages (in the spec's build order — a playable path before polish):
2. Blender: `cleanup_hoverbike.py` (scale, forked front = +Z, vents, grips), `cleanup_elarim.py` (3.5 m, cape split,
   Humanoid rig for the Mixamo clips), `cleanup_worm.py` (bone chain rise / wriggle / dive), cat clips `stretch` (grows
   to the large Panther, feet grounded) + `lie_down` in `cleanup_cat.py` → previews → **STOP for Dan** (models).
3. Venus graybox scene `Venus` (`FightDemoBuilderVenus.cs`): VEN_01 platform + saucer + storage hatch, VEN_02 terraces /
   channels / blocked walkway + on-foot alternate route (existing traversal), VEN_03 court / garden / guardian area / cube
   chamber, VEN_04 maintenance path / observation area; 3.5 m-scale doorways; Atmosphere_Venus.
4. Flow: "Set course" → transit → descent → C01, B01 (Panther), bike riding (`HoverBike.cs`: mount / dismount on safe
   ground, riding camera, recovery, summon), B02–B05, C02–C05 with `VenusState` + watchdogs; menu entry "Chapter 2 — Venus",
   `-chapter2`, Continue.
5. Voices (7 / Panther; Guardian + Attendant if Dan's voices exist, else subtitles) + SFX + music.
6. Checks (20-minute rule): one run "Set course" → C05, skip-equals-watch, save / reload per stage, captures, monochrome;
   release 0.11.0.

## Quick fix QF8: Chapter 1 clean-up from Dan's 0.10.4 play (Dan, 2026-11-18) — done, status below
Do QF8 first and **release it on its own (0.10.5)**, then go straight on to Round 11 (`Docs/ROUND11.md` — move it into
this file as the Current Objective) in the same session. Commit first. All standing rules apply, **20-minute testing
rule** included. Dan's screenshots: `Docs/Previews/qf8/`. Several of these are shared systems (camera, collision, held
objects, dialogue framing) that Venus will build on, so fix them properly, not just at the one spot.

1. **Blocked views — still — right as you enter the "crofting" (Lara-Croft traversal) area.**
   `dan_blocked_view_0.10.4.png`: at the first "Climb down" ledge the camera is behind a wall — the screen is wall,
   only her boots show. QF7's fix missed this spot. Make the camera rule general: the camera may never end up behind or
   inside geometry relative to 7 (spherecast from her head to the camera; pull in, raise, or swing around the obstacle;
   walls that still block fade cleanly). Check it with a quick automated sweep: walk the whole traversal route and log
   how much of 7 is visible at each point (0 points under ~60 % visible).
2. **The cat wanders off the wrong way** in the vision-chamber stretch (`dan_cat_wanders_0.10.4.png`: "Take the shape",
   the cat far off in the other direction). He catches up at the door, but it looks wrong. There he should stay near 7 —
   follow her into the chamber and sit / watch her and the shape, never head off away from her.
3. **Door frames in the last chamber flicker between black and grey** when 7 / the camera moves. Find the cause
   (z-fighting, the QF7 wall fade toggling on and off, shadow or LOD popping) and fix it; nothing in any chamber may flicker.
4. **Over-the-shoulder shots from 7 are always blocked by her hair** — you can't see the cat. Rework every over-her-shoulder
   framing (all scenes, including the galley): move the camera out and to the side / lower so her shoulder and a sliver of
   her head frame the left or right edge and the cat is clearly in view. Check each capture: the cat ≥ 80 % unobstructed.
5. **The opening interaction needs an exact spot.** At the very start, the prompt to look (which starts the first
   exchange with the cat) only appears if 7 stands in exactly the right place. Make it generous: it triggers from
   anywhere reasonable in the cell (wide area + generous look angle), and if the player doesn't find it within a few
   seconds the scene starts anyway (same at-or-past rule as QF6).
6. **Voice: "Before anyone sees you"** — 7 pauses after "before", which sounds like emphasis. Re-generate the line with
   no break (no comma / ellipsis / tag that causes it; try a few takes) and pick a take with no internal silence over
   ~150 ms (measure it).
7. **Voice: "Without hurting them"** — same problem after "without". Same fix.
8. **She partly disappears into walls.** Collision must keep her whole body out of walls: check the capsule size against
   the model, and the traversal / climb / shimmy / hang animations that push her into the wall (add a wall offset).
9. **The shape floats outside her hands** every time she touches or holds it. Attach it to her hand bone with a proper
   grip offset (palm contact, fingers around it) for every beat — touching it, holding it, putting it in her pocket,
   taking it out and setting it down in the galley. Check captures of each.
10. **Checks + release** (20-minute rule): quick run cell → galley, the camera sweep (item 1), captures of the cell
    opening, the vision chamber, every over-shoulder shot and every shape beat — **look at them yourself before
    reporting**. Publish **0.10.5**; docs; commit + push; per-item status in "QF8 — status"; "For Dan to check (QF8)".
    Then start Round 11.

## QF8 — status (2026-11-18): done except two small open camera spots (below), exe-verified off-screen; release 0.10.5 (`game-1005`, Latest)
Testing: about 23 min of game time, slightly over the 20-minute rule (two camera sweeps 3 + 3.8 min, the full cell → galley
check run 9.5 min, one re-check from checkpoint 4 6.5 min after fixing what the captures showed). No player-style run this time.
Captures: `Docs/Previews/qf8/` — `camsweep2/` (final sweep), `chapter1/` (cell → galley), `chapter1_cp4/` (re-check), `voice/`
(every take + its numbers). I looked at the touch, holding, pocket, galley and over-the-shoulder captures and the sweep's worst shots.

1. **Blocked views: done (two small spots open).** Cause of Dan's shot: the rock there is only a mesh (no collider), so the
   camera's casts never saw it. New shared system `CameraOccluders`: every visual-only mesh becomes a mesh collider in a
   separate physics scene that only the camera / framings / wall check query. The follow camera casts from her body to the
   camera, pulls in, swings or **cuts** to an open view (up to 72° above her in tight corners), and fades only small props.
   New `-camsweep` check: 124 points × 4 camera directions over the whole traversal route. Dan's spot: 100 % of her visible from
   behind / left / right (`camsweep2/05–07`). Sweep: mean 97 %, **4 of 496 views under 60 %**, all at one spot by the lunar
   door's right side (the re-shot capture there shows her in full, so it isn't steady). During play: mean 95 %, 6 of 217
   samples under 60 %, all while she climbs down into the mining cut. **Open:** those two spots.
2. **Cat in the vision chamber: done.** He follows her in and sits 1.1 m beside her watching her, then the shape; in Follow
   mode he now also catches up when she stands still. Log: furthest from her in the chamber 2.6 m.
3. **Door-frame flicker: done (cause fixed; not watched in motion).** The QF7 prop fade tested bounding boxes; the door
   frame's box covers the doorway, so every view through the door switched it off and on (black / grey). Now: exact mesh hits,
   the frame counts as a wall (pull in, not fade), and fades are dithered, eased and held 0.5 s.
4. **Over-the-shoulder: done.** New `ShotCheck` bakes her body + hair and measures how much of the cat a camera really sees.
   Galley: candidates scored (cat ≥ 85 %, her shoulder near an edge) → all 5 shots **cat 100 % unobstructed**. Moon: a
   conversation framing may no longer put one subject behind another's head (0.26 m), and every dialogue capture logs the cat's
   share (`s01` 100 %, `s05` 97–100 %, `s08` 100 %).
5. **Opening prompt: done.** "Look" shows anywhere in the cell while she faces roughly toward the door / cat (110° either
   side), and the scene starts by itself 8 s after the cat is on his mark.
6. **"Before somebody sees you" (`s01_02_7`): done.** (The line reads "somebody"). 6 takes measured with ElevenLabs character
   timestamps + ffmpeg: gap after "Before" 0.035 s, no silence inside the sentence (`Tools/Voice/retake_no_pause.py`).
7. **"Without hurting them" (`s05_02_7`): done.** The old take had a 0.26 s gap inside the phrase; new take: 0.040 s, none.
8. **7 in walls: done.** `ChapterSevenWalls`: after the animation each body point is cast out from her spine column against
   colliders + visual meshes and her whole body is pushed out (rendered only; hand IK compensated). Her model reaches 0.31 m
   from her root vs the 0.25 m zone radius. Log: pushed in ~20 % of frames, largest 25 cm; worst still inside 19.5 cm (her toes
   while hanging at the broken ramp end).
9. **The shape in her hand: done.** `CarriedShape` now sits the shape against her palm (from the hand and knuckle bones) with
   the fingers curled round it, either hand. Pocket = stays in the hand until it reaches the pocket; galley = out of the pocket,
   set down by her hand. Touch beat: she steps to arm's reach and her hand IK works now (the ledge IK used to switch it off).
   Logs: palm contact error 0.000 m when held, at the pocket, out of the pocket, over the table; fingertips on the shape in
   `chapter1_cp4/05`.
10. **Checks + release: done.** 0.10.5 published (`game-1005`, Latest). Soft-locks 0, audio peak 0, seat check 0.

## For Dan to check (QF8)
- The first "Climb down" and the whole climbing stretch: turn the camera into walls; it should pull in, swing or cut, never sit behind rock.
- Vision chamber: the cat comes in with you and sits watching; the door frames don't flicker.
- Opening: press X / E to look from anywhere in the cell (or wait 8 s).
- The two re-voiced lines (`s01_02_7`, `s05_02_7`).
- The shape: her fingers on it at the touch, in her palm when held, set down with her hand in the galley.
- Galley over-the-shoulder shots: the cat in clear view.
- Open: the climb-down into the mining cut (camera briefly too close) and one spot right of the lunar door.

## Quick fix QF7: Dan's playtest of 0.10.2 (Dan, 2026-11-18) — done, status below
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

## QF7 — status (2026-11-18): done except two open points (below), exe-verified off-screen; release 0.10.3 (`game-1003`, Latest)
Testing: about 22.5 min of game time in total, slightly over the 20-minute rule. The breakdown:
- two Chapter 1 check runs from checkpoint 4 (vision chamber → bay → aboard → galley, 7 + 6.5 min);
- the one player-style run (`PLAYTEST: 1/1, soft-locks 0`, 4.7 min at 2×);
- two short chamber-only runs stopped after the lock shot (50 s each) to re-check the chamber framings.

Captures: `Docs/Previews/qf7/`:
- `chapter1_cp4/` (88 shots + log);
- `playtest/`;
- `chamber_framing/` (the final chamber framings);
- `shapekeys/` (facial trial).

I looked at every galley and vision-chamber capture.

1. **Camera in the climbing stretch: done.** `ChapterCamera.Place` now does three things:
   - It reaches the focus point from inside her body. The old cast started at a focus that could sit inside the rock above a lip, so it ignored that rock.
   - It checks her head, chest and hips from the camera every frame. If fewer than two are seen, it pulls in. If that keeps happening (at once while hanging or climbing), it swings round to the nearest open angle, unless the player is turning the camera.
   - Props without colliders that come between the camera and her fade out (pipe run, chunks, ruin columns).

   The check runs log the frames in which her body is hidden. Player-style run: 50 of 20,883 follow frames (0.24 %), mostly the moment she climbs down into the mining cut. Autopilot run: 7 of 16,479 frames.
2. **The shape in one step: done.** The objective is "Take the shape" from entering the chamber. She walks up to it, with no prompt or button; walking into the alcove mouth also counts. She reaches for it, and the vision plays as she touches it. When the vision ends she is holding it (log: `after it she holds the dodecahedron: Hand`). There is no second visit. The QF6 take-fallback is gone because it can't happen any more.
3. **No glow on her: done.** After the vision she puts the shape in a pocket (`CarriedShape.Where.Pocket`): no shape, no pouch, no light. Log on the terrace and bay floor: `shape renderers shown False, lights on her 0`. The old hip light that made Dan's white blob is removed. Edge glow is faint lines only (no bloom halo): 0.35 on the Moon pedestal (light dimmed too) and 0.3 on the galley table (edge material grey 0.58, emission 0.35).
4. **The buried lock: done.** In the present the lock is behind a collapse: a broken rib leaning across, two slabs over the emblem and stone chunks (`LockRubble` in `FightDemoBuilderLunarDoor.cs`). It also blocks walking (ground obstacle). The vision video shows the lock intact, on the right. After the vision:
   - she looks at the pile and says "But I think I know how this opens";
   - objective "Uncover the lock": RT telekinesis, and the pieces fly to the west wall;
   - objective "Open the door": X on the lock, the door opens, lines 9–11.

   The doorway stays closed until then. `Docs/TRIGGER-AUDIT.md` rows 14–17 are updated. Checkpoint 5 is now "The buried lock + the lunar door".
5. **Galley seating + bowl: done.**
   - *Cause of the bad seating:* during the cutscene `SaucerSeven` still counted as grounded, so the arena clamp pushed her root out of the seat's footprint every frame. Now her hips are pinned to the seat point (`SaucerSeven.SeatHips`).
   - *Numeric seat check:* every seated frame, the hips must be within 5 cm of the seat point, over the cushion and 6–18 cm above it. Result: `SEAT CHECK: worst 0.0 cm, frames off the cushion or > 5 cm: 0` (36,555 frames in the check run, 15,474 in the playtest).
   - *Her hand:* it now rests at the plate's edge while the fork is stopped, instead of hovering in the air.
   - *The bowl:* a plain low bowl on the table (19 cm across, 4.2 cm high) replaces the raised feeder. The cat crouches at the front with his front paws planted (two-bone leg IK). Crouch depth and paw position are searched per run. Result: muzzle over the bowl's middle (0.000–0.005 m off-centre), above the food, paws 2 cm outside the rim, no clipping (capture `qf7_11_cat_in_the_bowl_side`).
   - *The cat talking:* when he speaks he lifts his head out of the bowl, looks at her (head and neck turn), then goes back to eating.
   - *After "He settles beside the bowl":* he sits a step back (a paw was in the bowl) and steps forward again to eat.
6. **Emotion without faces: done (shape keys dropped).**
   - *Eyes and head:* the Tripo head has no eye bones or eyelids, so blinking isn't possible. Gaze shifts move her look target a little every 1.2–3.5 s (76 in the scene). The head and neck drift slowly. While she speaks the head moves with the speech (small nods and turns).
   - *The cat:* ear flicks every 2.5–6.5 s (39), a slow head drift, and looking at her when he talks.
   - *Camera:* no face close-ups any more.
     - Four key lines get a medium shot from the deck side with a slow push-in.
     - Three of his lines hold on her reaction ("What did it feel like?", "farewell dinner", "what happens next").
     - "You could go home" pulls back to a wide shot.
     - "That marking" is a medium shot of the cat.
   - *Energy point:* it dims and brightens with her mood (0.5 at "I don't know" … 1.45 at "Venus, then").
   - *Clips:* Dan's emotion clips are imported (`Art/Mixamo/7/emotion`, Humanoid). Used: `sit_idle` (her seat pose; the old one held her head), `sit_talking` (×9 on her seated lines, when her hands are free) and `talking` (×2 at the fridge). The standing ones (thinking, sad_idle, nod_yes, thoughtful_nod, shake_head_no, shrug, look_away) and sit_disapproval / sit_laughing are imported but not used yet: she is seated for the whole talk, and a standing clip would stand her up.
   - *Facial shape keys, trial in Blender:* five keys on the head (`Docs/Previews/qf7/shapekeys/`, before/after sheets).
     - **Eyes closed:** looks wrong (the painted eye smears into a jagged line).
     - **Brows up / brows knit:** barely visible; the inner brow has too few vertices.
     - **Slight smile / sad mouth:** clean but only 3 mm, invisible at the medium framings the galley now uses.
     - Shipping any of them also means a round trip of the rigged FBX through Blender. **Dropped; nothing shipped.** Natural eyes or brows would need a re-topologised face or eyelid geometry.
7. **Checks + release: done.**
   - 0.10.3 published (`game-1003`, Latest).
   - Player-style run `PLAYTEST: 1/1, soft-locks 0, story watchdog fallbacks 0`, with quit + Continue at checkpoints 5 and 10.
   - Check run: soft-lock suspicions 0, audio peak 0 in every shot (muted).

**Open (honest):**
- ~~The three alcove beats in the vision chamber~~ **fixed after the release (your OK, 2026-11-18):**
  - The cause was the chamber geometry. The north side of the alcove mouth is closed by a vault rib's foot, about 0.3 m from her head. The south side was closed by the ruin column at (19.6, −36).
  - That column moved into the corner by the door wall (19.15, −37.45). The rib test in the framing search now uses 20 cm instead of 45 cm. A "no framing" now logs why every candidate was rejected.
  - Short check run: reach 90°, holding it 75°, pocket 90°, the collapse 45°, all medium side or three-quarter shots (`Docs/Previews/qf7/chamber_framing/05, 11, 13, 14`).
  - Released as **0.10.4** (`game-1004`, Latest; launch-checked with `-quick`).
- Testing ran about 4 min over the 20-minute rule (the short framing re-checks, the last one after the column move).

## For Dan to check (QF7)
- The climbing stretch (catwalk gap, pipes, bulkhead, cracked wall, shaft lip, terraces, mining cut, ramp + scaffolds): turn the camera into walls on purpose. It should pull in or swing round, never sit behind rock.
- Vision chamber:
  - "Take the shape" from the start; walking up to it plays the vision; after it she holds it, then it goes into her pocket (no glow on her afterwards).
  - The lock is not visible before the vision (the collapse covers it); RT clears it; X on the lock opens the door.
  - The alcove beats (reach, holding it, pocket) are side medium shots now (after the column move; 0.10.4).
- Galley:
  - She sits in her chair the whole scene.
  - The plain bowl; the cat lifts his head and looks at you when he talks.
  - Medium shots instead of face close-ups; small head and gaze movement; the energy point follows her mood.
  - The shape on the table is faint lines, no halo.
- Shape keys: decide whether a re-topologised face (or eyelid geometry) is worth it later. Blinking is not possible on the current head.

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
- **Round 11 — Chapter 2 "Venus", part 1 (to VEN_S04): Set course travels to Venus, C01–C05 + B01–B05, the hover bike, Panther, the Elarim, the cube, the signal — done as a first playable pass (2026-11-18), exe-verified off-screen; release 0.11.0 (`game-1100`).** Status in "Round 11 — status" above. Next: Dan plays 0.11.0, then Round 12.
- **Quick fix QF9 — door-frame flicker (z-fighting, measured in motion) + the Scree fighting tutorial before the city reveal — done (2026-11-18), exe-verified off-screen; release 0.10.6 (`game-1006`).** Status in "QF9 — status" above.
- **Quick fix QF8 — camera never behind mesh-only walls (occluder physics scene, camera sweep), cat stays near 7 in the vision chamber, door-frame flicker, over-the-shoulder shots that show the cat, generous opening prompt, two re-voiced lines, 7 kept out of walls, the shape in her palm — done (2026-11-18), exe-verified off-screen; release 0.10.5 (`game-1005`).** Status in "QF8 — status" above.
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
