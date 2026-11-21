# Active Development Plan — 7: The Observer

## Quick fix QF23: Dan's mothership (Round 17) review (Dan, 2026-11-21) — to do (24 items; Dan: "we are getting so close to being done")
Commit first. All standing rules + the repeat-item rule (why the last fix didn't hold, fix the cause, prove it in normal play
from the player's camera, never "done" on a partial fix). Release **0.17.x** (next patch); "QF23 — status"; "For Dan to check (QF23)".


1. **The mothership is white, and it sits within Veyra's rings.** (a) Its hull is **white** — smooth, pearly, luminous white (the
   brightest object in the scene, soft highlights, faint seams and hull lights), not grey. (b) It's **stationed inside the ring
   system**: the ring plane passes around / through its position — ring bands and drifting ice / debris visibly in front of and
   behind it, the planet Veyra huge beyond — not floating off on its own outside the rings (the setting correction: "stationed
   among the rings of Veyra"). Fix every view of it: BIA_01's extreme wide (the saucer above the debris band, the egg emerging
   from the ring plane, an orb crossing the foreground for scale), the approach and the iris, the departure, the star map's
   Veyra system, the window views. Captures of each.

2. **New lines in BIA_01 + the lore (Dan, 2026-11-21):** after "Mm-hmm." (BIA_01_06), `BIA_01_06a–d` (in the spec): 7 — "I thought
   their mothership was in the rings of Saturn." / Panther — "That one is. This is a sister ship." / 7 — "A sister ship. So… the aunt
   ship?" / Panther — (a reluctant chuckle) "That was a terrible joke." Voice them (7 v3, Panther), add to the dialogue data.
   Lore: the Biavians' main mothership is in Saturn's rings; Veyra's is a sister ship (Log / Field Guide may mention it).
3. **New standing rule (Dan, 2026-11-21) — add to `CLAUDE.md`: the ARRIVAL / strategy conversations — when they first arrive at a
   world and talk about it and what to do — happen in the chair at the front window with the "vanish ship" effect on** (the
   see-through hull button from the pilot's chair, QF21 1), so they're floating there looking at the world they're planning for —
   **every single time, every chapter**: Europa EUR_01, Mars MAR_01, the asteroid AST_01, the mothership BIA_01 (+ BIA_02 while still
   aboard), Venus's arrival talk if any, and every later chapter. Seated, Panther in her lap (or on the adjoining seat where the script
   says); the hull fades at the start of the talk and comes back at the end. **The recap / debrief conversations after a world keep
   QF16's varied staging** (sofa, floor, galley, window — Venus C11, EUR_17, MAR_12, AST_10's later chat, BIA_12), and the
   next-destination choices stay at the navigation console. List every scene changed.

4. **Clean up the iris door entry (BIA_02).** The saucer **flies right up to the door** (a closed, seamless circular section of the
   white hull with a fine spiral seam); the conversation about it (BIA_02_06–10: "That's the door?" … "Slowly sounds excellent.")
   happens while they hover in front of it. Then the iris **snaps open just for a moment** — a clear **camera-shutter / aperture
   effect**: the overlapping curved blades rotate together and retract in a spiral, a sharp mechanical whirr-click — only just wider
   than the saucer; the saucer slips through in that instant; the blades **spiral shut again very quickly** behind it (a solid
   clack). Show the shutter clearly: a front shot as the blades rotate open (so the spiral mechanism reads), the pass-through, and
   the reverse shot from inside as they close (BIA_02_11 "There. Plenty of room."). Model the iris as real overlapping blades (8–12
   curved leaves on a rotating ring), not a fading hole. Tidy everything else in the approach (the orb moving aside, timing, no
   waiting on empty shots).

5. **7's arms in the chair with Panther in her lap look wrong (repeat — QF12 10 / QF13 15).** One hand strokes him but awkwardly
   (stiff, odd wrist / elbow, not following the curve of his back); the **other arm clips into her legs**. Fix the seated lap pose
   for every chair scene (the front-window chair with the vanish effect is now used a lot — item 3):
   - **The petting hand:** a natural, relaxed stroke — palm flat and soft on his back, fingers slightly spread, moving from his neck
     toward his tail along the curve of his body, elbow bent comfortably and resting near her side; slow, unhurried, with small
     pauses; occasional scratch behind his ear.
   - **The other arm:** rests naturally — on the chair's armrest, or her hand resting lightly on his side / on her thigh **on top of**
     her leg, never inside it.
   - Measure it: per-frame hand / forearm vs her legs and body and vs Panther (0 frames inside anything, palm within ~1 cm of his
     back while stroking); captures from the scene's actual camera angles (over the shoulder, the side two-shot, the wide).
   Say why QF12 / QF13's fixes didn't cover this (they fixed *where* the hand goes and *releasing* it, not how the arms look while
   seated).

6. **The receiving berth: flat walls cutting straight through the parked saucer** (`Docs/Previews/qf23/dan_bia_berth_walls.png`:
   the saucer on its round pad with a cross of plain grey wall slabs passing through it above and below — "these walls … don't
   work. This makes no sense"). Find what they are (a docking clamp, the beam housing, a mis-placed berth structure) and replace
   them with a berth that **holds the saucer without touching its hull**: a lit landing ring / cradle under it, maybe slim docking
   arms that reach up to its rim, the beam's landing pad beneath — Biavian style (smooth, white, organic), no plain slabs. **Build
   check for every scene: nothing intersects the saucer prefab's bounds** (where it hovers or docks) — fail the build if anything
   does.

7. **Dark strands hanging from 7** (`Docs/Previews/qf23/dan_bia_strands.png`: from behind, two thin dark lines run diagonally from her
   right side / arm down toward the floor). New — "this hasn't been an issue previously", so it came in with the asteroid (Round 16)
   or QF21: most likely **the sheathed arc blades** (QF21 9 gave them a mount on her back / hips — they may be hanging loose, scaled
   wrong, or attached to the wrong bone so they trail), or stretched vertices / a hair or cloth piece stretched by a bad bone. Find
   what it is, fix it, and check her from every side (front / back / sides, standing, walking, running, riding, seated) in every
   chapter with and without the blades. Sheathed blades should read as two neat curved hilts / blades held close on her back,
   following her body — never dangling or trailing.

8. **Every walking character except 7 can't bend the LEFT knee — on every level, all game long** (Dan: "a consistent problem on
   every level … I've had other games with the same walk animations and never had this problem"). This is a **systemic bug, not a
   per-character one** — find the single cause before touching any character:
   - **Measure first:** for every humanoid NPC (the guardian, Elarim, the Scree, Tunneler, Reptilians, Viper, the Machinist, Nela /
     Biavians, Ratwing on foot), log **left vs right knee flexion over a full walk cycle** (angle between upper and lower leg, min /
     max) — and compare with the *same clip* on Y Bot / 7. Write the table into the status.
   - **Then check the shared things they have and 7 doesn't:** the Humanoid avatar each builder makes (`FightDemoBuilder*.cs` explicit
     human maps — is `LeftLowerLeg` mapped to the right bone on each rig? Mixamo rigs name it `mixamorig:LeftLeg`), the avatar's
     muscle / limit settings (a left-knee range clamped to ~0), the clips' import settings (Humanoid source avatar copied from a rig
     with a bad left leg? "Mirror"? per-clip muscle curves), and **any runtime script that writes to the left leg every frame** (foot
     placement / ground alignment / IK hints / the QF13 IK safety net / the cape or blade collision code that pushes the left thigh /
     `SevenRestPose`-style poses applied to NPCs) — something only touching the left side is the strongest clue.
   - **Fix the cause once** for all characters, then prove it: the knee table again (both knees bending equally, matching the clip on
     Y Bot), and side-view capture sequences of each NPC walking. **Add a build / test check** that fails if any humanoid's left and
     right knee ranges differ by more than a few degrees in its walk.
   Say plainly what the cause was.

9. **No long waits watching someone walk while 7 is frozen** (Dan: Nela walks over to the disoriented resident in BIA_03 and 7 is just
   stuck standing there — "I don't like having these long animations where 7 can't move and we have to wait forever for someone to
   walk"). New standing rule for `CLAUDE.md`: **never lock the player while an NPC walks somewhere.** Either (a) **cut** — skip the
   walk with a cut or a quick time-skip so the NPC is already there (2 s max of walking on screen), or (b) **give control back** — the
   player can move while the NPC walks (and the scene picks up when she's close, or the NPC waits for her), or (c) walk them
   **together** (7 walks beside them, player-controlled or a brief shared walk with the camera on both). Go through **every chapter**
   for scripted NPC walks that freeze 7 (the guardian's "Follow me", Nela's walks, the keeper, the caretaker, the Machinist, Viper,
   residents) and fix each; list them with what you chose. Also shorten any other cutscene beat where nothing happens for more than a
   couple of seconds.

10. **Panther's cutaways aboard: show the real saucer interior, not an all-white void — and make them fun.** In the mothership he
    appears in cutaways more than in earlier chapters (Dan: fine), but he's shown in a **blank white space**. Every cutaway to
    Panther aboard (in every chapter) shows him **inside the actual saucer interior** (the console / navigation area, the chair, the
    window with the view outside — QF16 / item 3), lit like the ship. Use the real interior (the Saucer scene's set or a prefab of it
    loaded for cutaways), never an empty background.
    **Show his cat side and his excitement about Ratwing** (the script's "a distinctly feline interest in him"): when Ratwing appears
    or moves on the display, Panther is **glued to the screen** — crouched low, pupils wide, **butt wiggle before a pounce**, tail
    lashing, **chattering / chittering at the screen** (that cat bird-watching sound), **pawing at Ratwing's moving marker** on the
    display; between moments, **batting at a dangling cable / string** near the console, then snapping back to dignified composure
    when he speaks ("Repair the relay." — sits up straight, tail curled neatly). Use it with the script's beats: BIA_03_10 ("Ratwing?" —
    ears snap forward), BIA_04_06–08 (the tail-tip twitch), BIA_05 (the twitching tail cut that "stops"), BIA_06_06–08 (watching his
    marker), BIA_07 (reading his feet), BIA_09 (eyes fixed, tail still on "Wait…"), BIA_09_16–17 (purring). New SFX: cat chatter,
    small thumps / paw taps. Captures of each cutaway.

11. **An audible, LOUD purr.** 7 mentions him purring (BIA_09_16 "You're purring." / "The ship is vibrating.", BIA_12, EUR_17, AST_10)
    but **you never hear it** — the existing `cat_purr` loop is too quiet / mixed under everything / not playing. Make Panther's purr
    **clearly audible and loud** (a rich, deep, rumbling real cat purr, close-miked, on the voice / dialogue bus so music never
    buries it; duck the music while he purrs; a subtle low-frequency rumble). **Whenever a line mentions his purr, the purr is already
    playing and clearly heard.** And **purring in general whenever they're close**: every lap / chair scene, petting, the reunions,
    him pressing his head into her hand, curled beside her on the sofa — a purr that swells when she strokes him and fades when she
    stops; plus a gentle purr layer in the free-roam ship when she sits by him or pets him (if there's a pet interaction). Make 2–3
    purr variations (`cat_purr_a/b/c`, `generate_sfx.py`) so it never sounds looped. Check the mix in every scene that mentions or
    shows it — log the purr's level vs dialogue / music.

12. **Repeat of the Mars problem (QF18 9 / "objectives come from the story" in `CLAUDE.md`): in the mothership the player just
    presses what the objective text says, with no idea why.** Every task (each relay, the bridge alignment, the garden's seam and
    release, the quiet passage's puzzle, the routing control in BIA_10, the partition stations if they stay) must be **explained in
    the story first — by Nela or by Panther, whoever would know** (Nela: how her ship works, what the relays do, what's at stake for
    her people; Panther: what he sees from the saucer's instruments, Ratwing's movements). Before each objective appears, a short
    line or two says **what's wrong, why it matters, and what she'll do about it**; when it's done, a line or a visible result shows
    what it achieved. Where the script has no such line, **write short new ones in their voices** (Nela calm, courteous, precise;
    Panther dry) — keep them brief, voice them with the saved voices (`Nela`, Panther), and **list every new line in the status for Dan
    to review**. Say why Round 17 broke the standing rule despite it being in `CLAUDE.md`, and add a check to the chapter checklist:
    every objective has a preceding story line or on-screen cause.

13. **BIA_06, the bridge and the seedlings — unclear, slow, and a gliding resident** (`Docs/Previews/qf23/dan_bia_seedlings.png`).
    (a) **What happened with the bridge isn't clear.** Before the puzzle, *show* the problem: the suspended bridge hanging out of line /
    retracted with a gap, the stranded residents waiting on the far side (visible from where she arrives), and Nela (or Panther)
    explaining — the relay that controls the bridge is down (item 12). Then the alignment reads as fixing *that*.
    (b) **The resident's "seedlings" read as a grey box with crystal shapes** — nobody can tell what they are, so "Are those all yours?" /
    "They were smaller when I left home." means nothing. Make them **unmistakably plants**: an open tray / basket of young Biavian
    seedlings with soft leaves (gently glowing leaf tips, a few with tiny unfurling fronds), visibly **overgrown and spilling over the
    sides** — so the joke lands (they've grown while she was stuck waiting); she holds it in her arms in front of her. If needed, let 7's
    line point at them ("Are those all seedlings?").
    (c) **The crossing is too slow** — the residents cross briskly (or cut so they're mostly across), no long wait (item 9's rule).
    (d) **The resident carrying the tray glides with no leg movement.** She walks with a proper **carrying walk** (`elarim_carry` or a
    walk with the arms held for the tray) — legs stepping. **Rule + check for every NPC in every chapter: nobody moves without a
    walk / run animation playing at the matching speed** (log any NPC whose position changes while its feet don't — 0 allowed).
    (e) **The resident's voice doesn't match the body:** every Biavian uses Nela's model, which reads as female, but the seedlings
    resident speaks with a man's voice (premade "Chris"). Rule for every chapter: **a character's voice matches how they look.** For
    the mothership: give the residents **some variety in the model** (a second Biavian variant that reads masculine — broader
    shoulders, a squarer jaw, a different suit panel — made from the same model in Blender, plus height / tone differences) and match
    each speaking resident's voice to their look (female residents female voices, male residents male voices). The seedlings resident
    (BIA_06): use the female look with a female voice (re-voice BIA_06_01 / 03), or the male variant if you keep "Chris" — say which.

14. **BIA_07, the garden: things shifting back and forth with no explanation** (Dan: "what are these things even? An in-game explanation
    if they're supposed to be there"). They're meant to be the **memory echo** — the garden as it was long ago, leaking out of the
    damaged archive and overlaying the real one (spec BIA_07; the leaking memories were introduced in BIA_03). Make it read:
    - **Explain it in the story as she arrives** — Nela (proposed, write in her voice, list for Dan): "Careful. This garden is
      remembering itself — the damaged relay is leaking an old memory of it into the present. Some of what you see isn't really
      there." — and Panther if useful ("The old layout and the new one don't agree. Look for what's real.").
    - **Make the echo look like a memory, not a glitch:** the remembered garden appears as soft, translucent, slightly brighter
      ghost-forms (old trees, the old wall, the remembered doorway) that **fade slowly in and out** on a calm rhythm with a gentle
      shimmer — no fast back-and-forth jitter or popping. Echo objects are clearly different from solid ones (translucent, no
      shadows, a faint glow edge), the same look as BIA_03's echo and clearly different from her visions.
    - **Tie it to the puzzle:** the echo shows where the old maintenance door was (the remembered doorway is the clue) → she finds the
      real seam lower down (BIA_07_01–03) → when the relay is reconnected the echo **fades away for good** (the payoff — the garden is
      just the garden again).
    If the shifting is something else (z-fighting, objects snapping between positions), fix that too and say what it was.

15. **How Panther sees — set it up — and fix the Ratwing moment after the garden (BIA_07_04–12).**
    (a) **Panther watching from the saucer has never been explained** (Europa's transports, Mars, the mothership's Ratwing marker — the
    scripts assume it). Establish it in the story:
    - **Generally (once, early — the saucer's instruments):** the saucer's scanners can follow her and what's around her; Panther watches
      on the console's display. Show it the first time he "sees" something in each chapter — a quick cutaway to **the console display
      with her marker and the area around her** (QF23 10's real interior), so the player learns where his eyes are.
    - **On the mothership specifically** — the ship's hull would block the saucer's scanners, so Nela links them (proposed, BIA_04, after
      the relay routes light up; Dan to confirm / edit, otherwise use): **Nela** — "I've linked your ship to our inner sensors. Your
      companion will be able to follow him." / **Panther** — "Delighted." *(far too pleased)*. Then his Ratwing marker / tracking lines
      make sense. Note: the quiet passage (BIA_08) blocks it — that's why he loses her there.
    (b) **BIA_07 — set up the threat before "Close it now":** when Ratwing lands beyond the glass partition, **show the upper service
    opening** above the glass (a camera beat on it) and **the control for it near her** (lit, in reach); Panther points it out first
    (proposed): "There's a service opening above the glass — he can get round through it. The control's beside you." Then **show Ratwing
    glance up at the gap and gather his feet under him** (a clear close shot) before Panther's "He's looking at the upper opening." /
    "Can he get through?" / "Yes. Close it now." — so his explanation afterwards ("He looked at the gap. Then he gathered his feet under
    him.") describes what the player just saw. Give the player a clear moment to press the control (a prompt on it, a short window).
    List the new lines for Dan.

16. **BIA_08, the quiet passage — explain why they'll lose contact, and add Dan's "paws" joke.**
    (a) Nobody explains how they know or why. Before Panther's "I'll lose you beyond that door.", **Nela explains** (proposed; Dan to
    confirm / edit, otherwise use): "The relay beyond this door sits inside the archive's shielding. Nothing reaches through it — not
    our sensors, not our thoughts. Not even yours." Then Panther's line follows naturally (he heard her). Inside, show the shielding
    (a shimmering threshold, the marker / link icon dropping out) so the silence is visibly caused.
    (b) **The "paws" joke (Dan, 2026-11-21)** — after BIA_08_06 (7: "You did the pause."), insert:
    | id | who | line | direction |
    | --- | --- | --- | --- |
    | `BIA_08_06a` | Panther | My… paws? | genuinely confused; cutaway: he looks down at his front paws, then lifts one |
    | `BIA_08_06b` | 7 | Pause. The silence. | a laugh she can't hold in |
    and change `BIA_08_07` to Panther — "Ah. An expressive pause." (composure returning; the paw set back down very neatly). Voice them
    (Panther, 7 v3), add to the spec and the dialogue data.

17. **Remove the big controls pop-up at the start of boss fights** (Dan: "really pointless … they just block the middle of the
    screen"; e.g. "Dodge B · Jump A · Block LT · Strike X · Telekinesis RT …" in `MarsDirectorTemple.cs`, `VenusDirectorPart2.cs`, the
    asteroid and mothership fights). The player already knows telekinesis and the fight buttons. Instead: **only fight-specific
    actions get a short contextual prompt, at the moment they matter**, small and out of the centre (bottom of the screen, like other
    prompts) — e.g. "Dodge (B)" next to the floor line as a swoop telegraphs the first time, "Seal the barrier (X)" at the control when
    he's inside, "Close the partition (X)" at the station, "Takedown (Y)" when he's dazed — each shown the first few times, then not
    again. The full button list stays on the Controls page in the pause menu. Apply to every boss fight (Venus, Mars, the asteroid,
    the mothership) and any other centre-screen help box.

18. **Throwable "junk" must belong where it is** (Dan: random junk lying around — old gears don't belong on the mothership). The
    telekinesis debris was reused from the asteroid. Rule for every chapter: **throwables fit the place and look like they'd be
    there**, and there's a reason they're loose:
    - **The mothership (BIA_09 transfer chamber, anywhere else):** Biavian things — smooth white maintenance canisters / cargo pods,
      cracked panels and fittings Ratwing tore loose (scattered where he forced the entrance — the reason they're on the floor), small
      planters, light-globe housings, segments of a damaged partition; organic, pearly, Biavian style.
    - **The asteroid:** gears, pipe sections, gauge plates (fine there). **Mars:** fallen stonework, Reptilian crates, broken barrier
      pieces. **Venus:** stones, root chunks, broken pottery. **Europa** (underwater): ice chunks, broken machinery from Tunneler's base.
    - Placed with intent (near where they'd have fallen / been left), not a random scatter; clearly loose so the player knows they can
      be thrown. Check every arena and puzzle area in every chapter and list what each now uses.

19. **The Ratwing fight (BIA_09) — make it understandable (Dan's play: his bar stalled at a phase mark and he didn't know why; he
    didn't know what the partitions were).** Keep the design (stun → close in → strikes; partitions shrink his flight space; the
    compartment trap at the end), but explain and signal everything:
    - **Explain the partitions before the fight** — Nela (proposed; Dan to confirm / edit): "Our partitions can wall off sections of
      this shaft. Close them and he'll have less and less room to fly — and nowhere left to run." (Panther, dry, optional: "A smaller
      room. My favourite kind of hunt.")
    - **Panther calls the stun:** the first times Ratwing is knocked down — "He's stunned — hit him now!" (variations, then fewer
      once the player has it).
    - **Panther calls the partition:** when his stamina reaches a phase mark — "Close the partition!" (or "Now the partition — the
      station's lit!") **and a bright beam of light shines down on the station to use** (and the partition it closes glows along its
      edge), so there's no doubt where to go. The bar visibly shows it's waiting (the mark pulses) — never a silent stall.
    - **The final stage must not be a long wait:** by the last phase the closed partitions leave him **very little room** — his only
      flight path leads past / into the compartment, so he goes there within a few seconds of fleeing (no circling around for ages).
      Panther's "Wait…" / "Now." still carry the timing, with a short, clear window to seal it.
    - Proof: a player-style run with the time between "he flees" and "sealed" logged (target well under ~15 s), and captures of the
      beam over the station and the stun call.

20. **Ratwing moves very stiffly.** He's the Round 4 Blender rig (Rigify + wing chains) driven mostly procedurally. Make him feel like
    a fast, agile, wiry flier and a sly talker:
    - **Body:** use real animation for the humanoid part — retarget existing clips through Humanoid where the rig allows (idle, talk,
      point, hit, knocked down, get up — the reused tunneler / YBot / elarim set, damped to his gaunt build), with a hunched, twitchy
      idle (head cocks, shoulders roll, claws flex) instead of a rigid stance. (Item 8's left-knee bug may be part of the stiffness —
      check his knees too.)
    - **Wings:** procedural but alive — overlapping motion and follow-through along each wing chain (the tips lag and settle), a real
      flap cycle in flight (power stroke down, fold-and-lift up), glides with wings spread and small corrections, wings that fold
      neatly when he lands / clings, a snap-open before a dive.
    - **Flight:** banking into turns (body rolls with the turn), anticipation before a swoop (he rears back, gathers, then dives on an
      arc), easing on landings (a crouch and wing-brace), never constant-speed straight lines.
    - Talking: head and shoulders move with his voice's loudness (like Viper), a sneering head tilt on taunts.
    Capture sequences of him idling, talking, swooping and landing.

21. **The arc blades in the Ratwing fight — they shock him** (Dan). If she has them (the asteroid was done first), they're a clear,
    fun part of this fight:
    - **Blade hits shock him:** every arc-blade strike crackles with visible white electric arcs across his body and wings, a sharp
      zap sound, and he **seizes / twitches** for a moment (stun-only — 7 never kills; against living beings the blades shock and stun,
      they don't cut).
    - **A counter for his swoops:** a well-timed blade slash as he dives past (a short window when he's close) **shocks him out of the
      air** — he tumbles down, stunned (an alternative to the telekinesis knock-down, rewarding timing). Charged / combo hits add a
      little more shock time.
    - Make sure the blades are actually **drawn and usable** in the fight (draw on the button, the prompt the first time: "Arc blades
      (…)" small at the bottom), and Panther or 7 can react once ("He doesn't like those." / "Good.").
    - If she doesn't have them yet (mothership before the asteroid), the fight works fully without them (telekinesis + punches /
      kicks) — no reference to blades.
    Log blade hits / shock stuns in the proof run.

22. **She doesn't actually touch the octahedron before the vision (BIA_11) — repeat of QF18 20** (the shared rune-touch → vision →
    return sequence, a standing rule). Use the shared sequence here: a close insert of **her hand reaching out and her fingertips
    meeting the octahedron's face** (hand IK onto the actual surface — no gap, no clipping), the rune answering (its glow surging, light
    running up her hand and arm, her forehead energy point lighting), the push-in to her eyes, white bloom → `vision_06_mothership.mp4`,
    and back: her hand still on it, then the insert of her fingers lifting away (the spec's "fingers lift slowly from the octahedron").
    Say why Round 17 didn't use the shared sequence, and **check every rune-touch in every chapter** (Moon, Venus, Europa, Mars, the
    asteroid, the mothership) uses it — a capture of the moment of contact for each.

23. **The octahedron is missing from her records when they discuss Cattalus (BIA_13B / AST_11B).** With the mothership and the
    asteroid both done she should have **six** — cube (Venus), dodecahedron (Moon), icosahedron (Europa), tetrahedron (Mars),
    cuboctahedron (asteroid), **octahedron (mothership)** — but only the earlier five show. Fix the collection so **every awarded record
    appears wherever the records are shown** (the chair arm / side table, the display when "the two records" are brought together, the
    console's Log / Field Guide) right after it's awarded — driven by one saved list of collected records, not per-scene placement.
    Check every combination of finishing order (mothership first / asteroid first) and every scene that shows them.

24. **Old cutscenes popping up out of place when revisiting finished worlds** (Dan: after "Cattalus — coming soon" he set course to
    other planets and got cutscenes that belong inside chapters he'd finished — ones added in later rounds, so his save had never
    played them). Story scenes must **always make sense in context and never appear later when they shouldn't**:
    - **A completed chapter's story never replays on a revisit.** Travelling to a finished world gives the short arrival (the
      sacred-geometry passage → the world in the window, the vanish view if they talk about it) and free exploration / a "visited"
      card — **no** arrival strategy talk, no first-visit scenes, no mid-chapter beats. (Replaying a chapter is only via the main
      menu's chapter list / Scene Select.)
    - **Save migration:** when a save from an older build has a chapter marked complete, mark **every** scene of that chapter as
      seen — including scenes added by later rounds — so they never trigger after the fact.
    - **Only progression-critical "due" beats may play later**, and only where they make sense (e.g. the combined-records discovery
      MAR_13C → the mothership route, the Cattalus lead — at the console, per QF20); list them explicitly. Everything else is
      chapter-scoped.
    - Test with Dan-like saves: (a) every chapter finished in an older build, then visit each world from the star map; (b) finishing
      chapters in different orders; nothing out of place plays. List what you found playing wrongly and why (was it the missing
      "seen" flags from older saves, or a real trigger bug?).

## Quick fix QF21: Dan's Asteroid review (Dan, 2026-11-21) — done, status below; release 0.16.1
Commit first. All standing rules + the repeat-item rule. Release **0.16.1**; "QF21 — status"; "For Dan to check (QF21)".
Dan: "happy overall" with the Asteroid.

1. **AST_01 (arrival): seat them at the front of the ship and make the saucer "invisible" while they talk.** 7 sits in the seat
   at the front of the ship (the pilot's chair at the front window) with Panther in her lap, as the scene starts (fade in already
   seated). As they talk about the asteroid, **the saucer's hull fades away around them** — floor, walls, ceiling and the outer
   shell dissolve to a faint glowing outline / nothing, so they sit in open space with the asteroid filling the view (the
   observation idea from the concept doc: "the floor, walls and ceiling fading away"). Keep all of their dialogue (AST_01_01–10),
   the chair and the seat beside it stay solid (she still lifts him onto the adjoining seat at AST_01_07). The hull fades back
   when the scene ends / as she heads to disembark. Soft, elegant (a slow dissolve with a shimmer at the edges), monochrome. **Use the
   effect that already exists** — the button that makes the ship see-through when she sits in the pilot's chair (Dan: "it is a
   cool effect") — the scene simply triggers it during the talk and restores it after; don't build a second version. It can
   be used the same way in later arrivals.


2. **AST_02: give "System" — the receiving chamber's maintenance machine and its voice — a cooler design** (Dan: it's generic
   right now). It's the player's first contact with the Great Machine Age, so make it charming and unmistakably of that age
   (riveted, steam-age, wheels and lenses — like the vision and the Machinist): proposal — a squat, rounded brass-and-steel body
   that **trundles on three jointed spindly legs** (or a single gyroscopic wheel), **one large round lens-eye with an iris
   shutter that dilates and narrows as it speaks and scans**, a fan of scanning light from the lens across 7 (head to toe), a
   little **projector that casts the guiding symbol** onto the floor toward the conveyor, a whistle of steam from a small stack
   on each announcement, tiny gauges / a ticking dial on its back. The **System voice** comes visibly from it (the iris and a
   speaker grille pulsing with the words), echoed by brass speaking-horns in the chamber walls that glow with each word. It
   tilts its "head" at her "Hello." and at "Material?" (a beat of mechanical curiosity). Keep it an obstacle, never an enemy.

3. **AST_02: no walking back and forth for the bike.** Today she walks to unlock the service door, then has to walk all the way
   back to the hover bike. Instead: **she gets on the bike first** (the objective / Panther tells her: e.g. Panther — "Take the
   bike. Whatever's through there, it's a long way." — or simply the objective "Get on your bike, then open the service
   entrance"), and the door / its manual control is **usable from the bike** (ride up to it; the prompt works while riding, or
   telekinesis on the control from the saddle). Keep the scanner-redirect puzzle, but it's solved from the bike too. General rule
   for every chapter: never make the player walk back to the bike for no reason — if the next stretch is a ride, let her mount
   first and do the step from the saddle.

4. **AST_03 junctions: tell her if she's on foot.** The three rail-alignment junctions are worked from the bike. If she gets off
   and walks up to one, show a clear prompt instead of nothing — "Use the junction from your bike" (and Panther, once, dry:
   "It's built for something with wheels. Or hover pads.") — and point the marker back at the bike. General rule: any
   interaction that needs the bike (or needs her on foot) says so when she's in the wrong state, never silently ignores her.

5. **The Machinist's footsteps: heavy metal clunks.** Every step he takes lands with a **heavy, deep metallic clunk** — iron
   foot on metal floor, a hiss of the piston legs and a faint rattle of plates — timed to his planted foot (from the clip's foot
   contacts, like 7's footsteps), positional and loud enough to feel his ~4 m weight, with a light camera rumble when he's close.
   His servos whir on turns and arm moves. New SFX via `generate_sfx.py` (`machinist_step_1..4`, `machinist_servo`, `machinist_hiss`;
   vary between takes so it never sounds looped). Check in the reveal (AST_04), walking in the hall, and in the fight.

6. **AST_04 (first meeting): the camera stays fixed while the Machinist walks off screen** — he leaves his repair cradle, walks
   toward the camera and out of frame, and the player stares at an empty shot for a few moments. **The camera follows him**: track
   / pan with him as he walks (keeping him framed, low angle for his scale as the spec says), then settle into the over-the-shoulder
   shot behind 7 as he stops in front of her. General rule for every cutscene: **no shot holds on empty space** — a moving
   character stays in frame (follow, cut ahead of him, or cut to the next shot when he exits); check each scripted move in every
   chapter for a character walking out of a static shot.

7. **Conversations with the Machinist: the over-his-shoulder shots don't work** — the camera sits behind his huge arm / body and
   all you see is the back of his arm. For his conversations (AST_04, AST_07, AST_09): when the shot is from his side, **pull the
   camera back and out to the side** so 7 is clearly visible (her face and body, a little of his shoulder / claw in the edge of
   frame at most) — never a frame filled by his armour. And use **wide two-shots that show both of them together** often — 7
   small at his feet, him towering — so his scale against her reads (Dan likes these); low angles up at him for his lines, high
   angles down on her looking up. The item-7 visibility check (QF18) should also fail a shot where a character's own body fills
   more than ~half the frame and hides the listener; apply it to every big character (Tunneler, Viper, the keeper).

8. **The Machinist gestures too much in conversation** — his arms swing around a lot while talking (probably a borrowed talk clip).
   He's economical and deliberate (the spec): while talking he stands **still and upright**, arms mostly at rest; his "acting" is
   small — a slight head tilt / adjustment, the visor glow pulsing with his voice, the claw opening or closing once, a single
   restrained gesture on a key line at most (e.g. pointing at the archive panel). Use a calm idle (slowed, damped) instead of the
   gesturing talk clip; keep big arm movement for work and the fight.

9. **The arc blades sit curved into her body when she isn't using them.** Hold them **further from her body**: when drawn but idle
   (and while walking / running with them), the blades angle **outward and away** — tips pointing down and out, the curve facing
   away from her — with her arms held slightly out from her sides, so no blade ever passes into or hugs her legs, hips or torso.
   Check the blade's distance from her body every frame in the idle, walk, run and between strikes (a per-frame "blade vs body"
   log: 0 frames inside her, a clear gap of a few cm at least), and adjust the hand grip / blade orientation per clip. When
   sheathed, they rest on her back or hips on their own mount, clear of her too.

10. **The central hall (the Machinist fight) is full of plain blocks — make them into something interesting** (the no-boxes rule).
    Turn every block in the arena into a real Great Machine Age object that also **does something in the fight**:
    - **cover / obstacles** → heavy machine housings: flywheel casings with turning spokes, boiler drums with gauges and valves,
      stacks of riveted crates of archive panels, cooling towers venting steam;
    - **throwable debris** for telekinesis → loose gears, pipe sections, gauge plates, small crates (clearly loose, so the player
      knows what can be thrown);
    - **the conveyor lanes** → real belt conveyors with rollers, carrying archive panels and parts;
    - **the service stations** → big clamp rigs with jaws and pistons (where his tool arm gets locked);
    - **the cradle** → the repair cradle from AST_04, framed and lit;
    - overhead: gantries, chain hoists, a rail carrier crossing, hanging lamps — the hall should feel like a living foundry.
    Keep the fight's layout (cover, lanes, sightlines) — change the look, not the gameplay. Same pass for any other plain blocks
    in the chapter (the controls area, the causeway, the alcove). Captures of the hall from the fight camera before / after.

11. **AST_09 (after the vision, talking to the restrained Machinist): the "TV" is a black screen, and he moves and clips in his
    clamps.** (a) The archive surface she shows him the memory on is just black when the camera looks at it. It must **play the
    memory**: the vision video (`vision_05_asteroid.mp4`) on that surface, in the console-hologram / archive-glow style, synced
    to the script — the workshop and the bird as they talk, **the production line stopping on AST_09_05**, the bird fluttering —
    framed as the spec says ("the enormous caretaker beside the tiny recorded bird"). Show-don't-caption rule: never a blank screen
    the camera lingers on. (b) **He's restrained — he stays still.** In the clamps / cradle he doesn't walk or swing around: body
    locked in place, arms held in the clamps; only his head turns toward the image / her, the visor glows with his voice, the
    claw twitches once. No idle clip that moves his body; nothing passing through the clamps (check every frame). When he's
    released after accepting the preservation task, the clamps open visibly first, then he moves.
    **(c) His walk after the release has the same fixed-camera problem as item 6** — he steps out of the clamps and walks off
    screen while the camera stays put. Follow him (track / pan with him to the protected storage he opens), keeping 7 in the
    shot where possible; never hold on the empty clamps. Item 6's rule — check every scripted walk in every chapter.

## QF21 — status (2026-11-21): done, exe-verified off-screen; release 0.16.1
Every run muted (peak 0), minimized, Below Normal. Proof = captures from the player's / cutscene camera + the log lines below.

**General rules applied across chapters (new standing rules in `CLAUDE.md`, "Cameras, prompts and the bike"):**
- **No shot holds on empty space** (item 6) — every chapter: `ChapterCameraShots.cs`. `ChapterCamera.ShotFollow` = a scripted
  tracking shot; automatic: a person seen in a held shot who walks to its edge with nobody else in frame makes the camera
  follow them (Moon, Venus, Europa, Mars, Asteroid - all use ChapterCamera; MarsFolk / ElarimNpc / 7 / the cat are watched).
  Measured in every check run: `EMPTY SHOTS (QF21 6)` (0 in every run of this fix) and `SHOT FOLLOW` lines. `-noqf21fix` = off.
- **No frame filled by a big body that hides the other one** (item 7) — every chapter: `DialogueFraming.Sees` now rejects a framing
  where a character over 2.4 m who is not the speaker covers more than half the frame (`DialogueFraming.Coverage`) - Machinist,
  Tunneler, Viper, the keeper. The blocked-line log now says what failed ("big body", "wall / prop: name", "person", "out of frame").
- **Wrong state says so** (item 4): Asteroid `Interact(bikeOnly:)` ("Use the junction from your bike", Panther once, marker to the
  bike); Venus B03 slab on the bike ("Get off - the slab needs telekinesis on foot"); Europa interactions while riding ("Leave the
  bike first"). Mars already dismounts on X; the Moon has no bike.
- **Mount before a ride** (item 3): AST_02 reworked. Audit of the other chapters' "get back on the bike" steps: Mars MAR_03→4
  (after the on-foot conversation at the checkpoint, the bike is beside her) and Europa EUR_11/12 (the base is on foot; the bike
  waits at the dock) are story-needed - no change.
- **Talking characters stand still** (item 8): `MachinistRig.Calm` (a held, damped idle while he talks; one gesture at most).
- Camera occluders: moving parts (`AstSpin`, rail-carrier cars) are no longer frozen into the camera's line-of-sight copies
  (their frozen copies blocked AST_08_07 - "STILL BLOCKED" since Round 16 - and it is 0 now).

**Items:**
1. **AST_01**: fade in already seated (pilot's chair at the front window, Panther in her lap). As they talk about the asteroid the
   ship dissolves round them - the existing pilot's-chair effect (`ShipReveal`, run from the pilot's seat via `CenterOverride`,
   slow 6.5 s, the shimmering edge + motes); her chair, the seat beside it (his) and the records on the arm stay solid
   (`ShipReveal.KeepSolid`, 14 renderers). All of AST_01_01-10 kept; she lifts him onto the adjoining seat in open space; the hull
   rebuilds before the scene leaves for the asteroid. Log: `AST_01 (QF21 1)`; captures `aa01a / aa01b / aa03 / aa04b`.
2. **System**: rebuilt (`FightDemoBuilderAsteroidLevel.cs`, `AstMaintBot.cs`): squat rounded brass-and-steel body on **three jointed
   spindly legs** (a tripod gait, one leg lifts at a time), one big lens-eye with an **8-blade iris** (narrows to scan, opens with
   the words), a speaker grille that glows with its voice, a scanning fan of light swept over her, the floor-symbol projector (a
   ring with an arrow), a steam stack that **whistles** on each announcement (new SFX `ast_steam_whistle`), gauges + a ticking dial
   on its back; **four brass speaking-horns** in the chamber walls glow with each word. Head tilts at "Hello." and "Material?".
3. **Bike first**: Panther's new line AST_02_P5 "Take the bike. Whatever's through there, it's a long way." → "Get on your bike" →
   the scanner by telekinesis **from the saddle** → ride up to the door control, **X from the bike** → ride straight through.
   Player-style run: mounted at 21 s, solved from the saddle, no soft-lock.
4. **Junctions on foot**: prompt "Use the junction from your bike", Panther once (new line AST_03_P3 "It's built for something
   with wheels. Or hover pads."), the marker points back at the bike; X on foot answers with the hint. Check-run demo:
   `WRONG STATE (QF21 4) check: on foot 1.7 m from the control - prompt 'Use the junction from your bike', marker on the bike,
   Panther's line played`; capture `a13x_wrong_state_on_foot_at_the_junction_player_camera`.
5. **His footsteps**: new SFX `machinist_step_1..4` (never the same take twice in a row), `machinist_hiss`, `machinist_servo`; the
   plant is read from his foot bones (lifted → down), positional (70 m), a light camera rumble within 14 m, servo whir on turns.
   `MACHINIST STEPS (QF21 5)`: 39 steps in AST_04, 143 in the fight (+42 servo whirs), 67 in AST_09.
6. **The camera follows him** (`TrackWalk`): AST_04 rise → walk to her (14.7 m, **out of frame 0 of ~2200 frames**), "he blocks her
   path", AST_09 to protected storage (40 m, 0 of ~5000 frames), then the over-the-shoulder shot. Automatic follow in the fight
   (`SHOT FOLLOW ... Machinist`). Empty shots 0.
7. **His conversations**: every third line a wide two-shot (7 small at his feet), his lines from low angles, hers from his side
   pulled back and out. `MACHINIST FRAMING (QF21 7)`: 17 lines (AST_04): 5 wide, 6 low on him, 6 her from his side; his body
   6-19 % of the frame on her lines (38 % once), **7 hidden 0 times**.
8. **Calm in conversation**: `MachinistRig.Calm` holds the idle pose (85 %, slow follow), one restrained gesture ("point") on
   AST_07_03 and at the storage; big moves stay for work and the fight.
9. **Arc blades**: drawn but not striking, her arms come a little out from her sides and each blade angles out and away (tips down
   and out, the curve bowing away); in the fight's guard the guard opens wider and the angle is picked per frame from a few
   candidates (the first that clears her body by 4 cm); a frame that still would touch her pulls the light ribbon in for that
   moment. Sheathed: the crossed hilts on her back. `-qf21blades` (idle / walk / run, player camera + front + side): **0 frames
   inside her, smallest gap 27-34 cm**. The fight (`-astcp 6 -bosscheck`): **8,825 frames between strikes, 0 inside her, smallest
   gap 3 cm** (the light pulled in for 148 blade-frames in fast stance changes).
10. **The hall** (`FightDemoBuilderAsteroidFoundry.cs` + the hall builder): service stations → clamp rigs (turned pedestal, four
    pistons, riveted crossbeam, gauge); floor lanes → belt conveyors (rails, low end drums, slats); hoppers → turned banded hoppers
    on chains; the cutter → drive gear, pistons, flue; the cradle → an arch, side lamps, a light ring; outside the arena: two
    flywheel casings (turning spokes), two boiler drums (bands, gauges, valve wheel), crate stacks of archive panels, a cooling tower
    venting steam; five wall flywheels, steam pipes with gauges; overhead gantries with trolleys + chain hoists, a rail carrier
    crossing high up, hanging lamps on chains; throwables = loose gears, pipe sections, gauge plates, small crates. Works: the
    diverter is a gearbox, the alcove console has gauges + a lever. Obstacles `AsteroidLayout.HallProps`; fight layout unchanged.
    Build checks: holes 0, coplanar 0 left, NPC placement 0. Captures: fight shots `boss_machinist_*`, `a16b_*`, `a49c_*`.
    (Found on the way: the station + cradle lamps sat at twice their offset - fixed.)
11. **AST_09**: (a) the memory screen was black because the quad had **no UVs** (the video sampled one corner texel). Now a UV'd
    double-sided screen on `Seven/GalleryScreen` with a soft white rim; the vision video plays synced to the lines (the workshop
    + bird on 01-04, **the line stops on AST_09_05**, the bird flutters after). `MEMORY SURFACE (QF21 11a)`: average 0.75 brightness.
    (b) restrained: `MachinistRig.Restrained` locks his body (no idle clip), only the head turns, the visor glows, the claw
    twitches once; `CLAMPS (QF21 11b)`: 24,537 frames, a bone inside a pad 0, body moved 0.3 cm. The padded arms open visibly
    (`AstClamp.OpenUp`) before he moves. (c) the walk to storage is a tracking shot (above).

**Testing time:** about 60 game-minutes over 15 short runs (mostly `-testspeed 2`) - **over the 20-minute rule**: four rounds of
fix → rebuild → re-run on the same items (the autopilot's ride after the door, the wrong-state demo, the fight blades, AST_08_07).
No player-style run of the whole chapter this time; the player-style run covered AST_02-AST_04.

**Not covered / known:**
- Check runs that start at checkpoint 6 (`-astcp 6`) sit 40 s in the west passage before the hall ("back into the hall (3/4)") -
  the same in Round 16's own runs (the autopilot walks back to the passage's first point); the player-style run never hits it.
- Venus slab / Europa wrong-state prompts are code-only this round (no run reached them).
- The SFX were not listened to (muted rule).

## For Dan to check (QF21)
1. AST_01: the ship dissolves round her chair as they talk, and comes back before she goes down.
2. System in the receiving chamber: legs, the iris, the whistle, the wall horns glowing with its voice, the head tilts.
3. AST_02: she takes the bike first; telekinesis on the scanner and X at the door work from the saddle.
4. A junction on foot: the prompt, Panther's line, the marker back to the bike.
5. The Machinist's footsteps (loud, heavy) in AST_04, in the hall and in the fight.
6. AST_04 / AST_09: the camera goes with him when he walks; never an empty shot.
7. His conversations: wide two-shots, low angles on him, her from his side - never a frame of his armour.
8. He stands still while he talks.
9. The arc blades drawn: out and away from her body; in the fight they pull in rather than touch her.
10. The hall: a working foundry, no plain blocks.
11. AST_09: the memory plays on the screen (the line stopping on AST_09_05); he stays still in the cradle; the arms open first.

## Quick fix QF22: the Biavian contact is at Veyra, not Biaveh (Dan, 2026-11-21) — done, status below; release 0.16.2
Commit first. Small, focused. Release **0.16.2**; "QF22 — status"; "For Dan to check (QF22)".
**Dan's setting correction** (full text in `Docs/CHAPTER_BIAVEH.md`, "Setting correction"): the Biavian mothership is stationed
among the rings of **Veyra**, a planet outside our solar system — **not the Biavian homeworld**. The combined Mars + Europa
records identify an old Biavian contact route that leads to the mothership near Veyra. The asteroid is still found on the way.
Progression unchanged (mothership + asteroid in either order → Cattalus). Update what's already in the game:
1. **Star map:** the second system is **Veyra's** (its star, **Veyra with its rings**, the mothership's position, the asteroid) — not
   "Biaveh's system"; Biaveh the homeworld is not on the map. Labels (Dan's wording): before arrival **"Biavian contact — Veyra"**,
   after the mothership chapter's identification **"Biavian Mothership"**; the asteroid marked there after TRV_AST. The window
   views / panoramas while there show Veyra and its rings.
2. **Choice + navigation text (Dan's wording):** TRV_AST's choice "Continue to Biaveh" → **"Continue to the Biavian mothership"**;
   any travel / coming-soon cards and menu chapter names that say "Biaveh" → "Veyra" / "The Biavian Mothership" as fits.
3. **Lines already voiced that name Biaveh** — re-write + re-voice (Dan's wording where he gave it; the rest proposed by the chat,
   Dan to confirm — otherwise use as written):
   | id | who | old | new |
   | --- | --- | --- | --- |
   | `MAR_13C_07` | 7 | Biaveh. | **Veyra.** |
   | `MAR_13C_08` | Panther | Home of the Biaviian. Beyond this solar system. | **A Biavian contact. Beyond this solar system.** |
   | `MAR_13C_13` | 7 | All right. Biaveh. Together? | **All right. Veyra. Together?** |
   | `TRV_AST_07` | Panther | …or continue to Biaveh and return. | **…or continue to the Biavian mothership and return.** |
   | `AST_11A_01` | Panther | Biaveh is still ahead of us. | **The Biavian signal is still ahead of us.** (Dan's) |
   | `AST_11A_06` | 7 | Biaveh. And something to eat on the way. | **The mothership. And something to eat on the way.** |
   | `AST_11B_01` | 7 | There. The part missing from the Biaveh record. | **There. The part missing from the Biavian record.** |
   Search all dialogue data, the Log, the Field Guide and the specs for any other "Biaveh" that means the destination and list it.
4. **Spelling:** Dan's latest scripts write **"Biavian"** (older docs: "Biaviian") — use "Biavian" everywhere in the game (Field
   Guide, Log, lines, labels).

## QF22 — status (2026-11-21): done, exe-verified off-screen; release 0.16.2
- **Star map** (`StarMap.cs`): the second system is **"THE VEYRA SYSTEM"** - Veyra's star, **Veyra with its rings** (two tilted
  ellipses + a "VEYRA" tag) and the mothership's bright mark beside it, the asteroid there after TRV_AST; the gateway reads "The
  Veyra system →"; the overview "VEYRA'S STAR". The destination's name = `WorldFlags.ContactName`: **"Biavian contact — Veyra"**
  until the mothership chapter identifies it (`WorldFlags.MarkMothershipKnown`, set in Round 17), then **"Biavian Mothership"**.
  Biaveh the homeworld is not on the map. The internal id stays `biaveh` (saves keep working).
- **Window view** (`SaucerOutside`): open space at Veyra - its star, Veyra and its rings (a wide pale debris band + a fainter ring).
- **Text**: TRV_AST's choice "Continue to the Biavian mothership"; the map caption in MAR_13C / AST_11A; the coming-soon card "The
  Biavian Mothership (coming soon)"; the console's world name; the MAR_13C hologram's end node.
- **Re-voiced** (spec tables edited, `make_mars_lines.py` / `make_asteroid_lines.py`, `--force --only`): MAR_13C_07 "Veyra.",
  MAR_13C_08 "A Biavian contact. Beyond this solar system.", MAR_13C_13 "All right. Veyra. Together?", TRV_AST_07 "...or continue to
  the Biavian mothership and return.", AST_11A_01 "The Biavian signal is still ahead of us." (Dan's), AST_11A_06 "The mothership.
  And something to eat on the way.", AST_11B_01 "There. The part missing from the Biavian record." - Dan's / the chat's wording.
- **Other "Biaveh" found** (searched dialogue data, Log, Field Guide, code, specs): none left in what the player sees or hears.
  Still "Biaveh" only as internal ids / file names (`biaveh` star-map id, `Resources/Biaveh`, `biaveh_*.mp3`,
  `CHAPTER_BIAVEH.md`) and in older docs (`STORY.md`, `VOICES.md`, `MUSIC.md` describe the files) - left as they are.
- **Spelling**: "Biavian" everywhere the player reads it.
- **Proof**: `-qf20check` (Dan's save, one X at the console): MAR_13C plays the new lines (`line MAR_13C_07 ... 'Veyra.'`, 08, 13),
  the hologram's route "out to the Biavian contact at Veyra", TRV_AST_07 re-voiced, the choice on the Veyra map (capture
  `qf20_02_trv_ast_the_choice`), `QF20 DISCOVERY CHECK: PASS`; muted, peak 0. Testing ≈ 5 game-min.

## For Dan to check (QF22)
1. The star map's second system: "The Veyra system", Veyra with its rings, "Biavian contact — Veyra".
2. MAR_13C / TRV_AST / AST_11 lines say Veyra / the Biavian mothership; the choice reads "Continue to the Biavian mothership".
3. The window view at Veyra shows the ringed planet.

## Round 17 — status (2026-11-21): first playable pass done, exe-verified off-screen; release 0.17.0
The whole chapter plays from the star map to BIA_13: the title card, BIA_01 + BIA_02 aboard (Veyra, the mothership, the orbs,
Nela's welcome, the iris), BIA_03-BIA_11 in the new scene `Biaveh`, BIA_12 + BIA_13 aboard. Every run was muted (peak 0),
minimized and Below Normal.

**Real assets in (no stand-ins for the Biavians or the vision):**
- **Nela and the residents** = Dan's `Art/Mixamo/biavian/biavian_body_mixamo.fbx` (Kyle's design) as a Humanoid
  (`SetupMixamoFolk("biavian")`, `Biavian.controller`: idle / talk / point / carry / walk / wave / kneel, the Elarim + reptilian
  clips). Residents vary in size (0.9-1.06), tone and medallion glyph.
- **Vision 6** = `Art/Video/vision_06_mothership.mp4` (copied by Build everything; still fallback `Resources/Biaveh/vision_06_still.jpg`),
  with a soft hum as the light travels into the archive and a faint chime, no music.
- **Ratwing** = the Round 4 model (`ratwing_v1_rigged.fbx`) with procedural flight (`RatwingRig`: his rig has no flight clips).
- Voices: all 165 lines voiced (Nela = premade "Bella", Ratwing = Dan's saved "ratwing", the resident = premade "Chris").

**Files:** `Runtime/BiavehLayout.cs` (numbers, zones, routes, checkpoints, the shell), `BiavehState.cs` (+ `BiavehDialogue`),
`BiavehDirector.cs` / `BiavehDirectorFlow.cs` (BIA_03-08) / `BiavehDirectorTemple.cs` (BIA_09-11 + home), `RatwingFight.cs`,
`RatwingRig.cs`, `MemoryEcho.cs`, `Octahedron.cs`, `Chapter1AboardBiaveh.cs` (BIA_01/02 + the runtime exterior, BIA_12/13),
`BiavehPlaytest.cs`; `Editor/FightDemoBuilderBiaveh.cs`; boss table entry `ratwing`; `Tools/Voice/make_biaveh_lines.py` +
`generate_biaveh_voices.py`; SFX `bia_*`, `ratwing_*`; Log + Field Guide entries (`biaveh>=n`).

**Proof (exe, muted, off-screen; every capture monochrome - max |R-G|,|G-B| = 0; audio peak 0 in every shot):**
| Run | Result |
|---|---|
| Player-style `-biaplay -testspeed 2` (free aboard → console → star map "Biavian contact — Veyra" → BIA_01 … BIA_13A) | `MOTHERSHIP PLAYTEST: 1/1, soft-locks 0`, 104 captures; `BIKE HOLD CHECK biaveh: PASS`; `DIALOGUE FRAMING`: 73 lines, still blocked 0, big-body rejections 0; `EMPTY SHOTS (QF21 6)`: 0. One skip (the first point of the ride home: the bike parked against the chamber wall) - fixed (the route now starts at the chamber entry) and re-proved below |
| Boss, same run | `BOSS ratwing`: 2:52, 32 attacks (11.2/min: swoop 7, claw pass 24, wing gust 1), dodged 22, 23 throws = 23 stuns, melee 87 % of the damage (punches 31 %, kicks 51 %); X objectives: second presses 0, to a strike 0; stage three: sealed 8.6 s after it began, mistimed 0 |
| Idle test `-biacp 5 -bossidle` | `BOSS IDLE TEST (ratwing): knocked down after 26.9 s - PASS` |
| Retry `-biacp 5 -biaplay -bosscheck -bossdownonce` | she went down once: `RETRY: his energy 66.0 % before -> 66.0 % after`; fight 2:43, melee 89 %; contained alive, mistimed 0 |
| Home `-biacp 6 -biaplay` (archive → ride home → BIA_12 → BIA_13) | `1/1, soft-locks 0, skips 0` |
| Build checks (Biaveh) | `SAUCER CHECK`: the one prefab, unchanged; `COPLANAR`: 0 left; `HOLES CHECK`: 13 790 rays, 0 holes; `NPC PLACEMENT`: 0 bad; `KICK STATES`: present |

Captures of the boss mid-attack: `boss_ratwing_swoop_telegraph / _active (+ _side)`, `_stagger_*` (player-style run).

## Round 17 — decisions made (Dan away; easy to change)
1. **Nela introduces herself** in BIA_02_01: "Welcome to Veyra. I am Nela. Please hold your course. Our guardians will let
   you through." (TODO said she introduces herself there; the spec line had no name).
2. **BIA_01 / BIA_02 play aboard** (like AST_01): the exterior (Veyra and its rings, the pale debris band, the egg mothership with
   hull lights / traffic / recessed entrances, the three orbs, the iris) is built at runtime round the passage stage's saucer
   (the one prefab). The mothership is shown at a distance that reads (an egg ~1.2 km tall seen from ~1.5 km) - the 26 miles are
   in the line, not literal.
3. **The overlook is on the berth's west edge**; the interior shell rises on all four sides beyond the play area
   (`BiavehLayout.ShellRise`, R 320 m), towers standing out of the curve, hanging gardens, transit lights, bridges overhead. The
   wide reveal thins the haze for the shot.
4. **Panther's cuts aboard** (BIA_04_08 / BIA_05_12 tail-twitch, BIA_08_07, BIA_08_15, BIA_09 purring): a small aboard set in
   the Biaveh scene (his seat, the console arc, the window glow) with the cat model - the chapter runs in its own scene.
5. **BIA_06 puzzle**: the echo of an old span lies across the chasm (it can't be walked on); X at the service controls swings
   the real three segments into line. **BIA_08's solo puzzle**: an airlock pair - panel A opens door A; inside, panel B is dead
   until she lifts its loose coupling into the socket with telekinesis; panel B closes A behind her and opens B. No Panther
   voice inside (no telepathy lines play there), music down.
6. **The route share (BIA_04)** is shown in the world, not as a vision: a line of light runs from her feet along the roads to
   the three relays, three faint beacons stand over them (each goes out when its relay is restored).
7. **The bike through the garden**: an avenue (zone `GardenAvenue`) lets the bike cross the garden to the road west; the
   garden itself is on foot.
8. **Ratwing's fight** (Dan's Decisions): swoop (lunge: a line on the floor, dodge sideways), claw pass (sweep: block or dodge),
   wing gust (ring: jump) from phase 2; telekinesis while he is low after a wasted dive knocks him down, stunned; punches /
   kicks / arc blades (if she found them on the asteroid) do the damage; partition stations (X, while he attacks) in phases
   1 and 2 close a partition and shrink his flight space; his bar = stamina; spent = he flees (no takedown) → **stage three**:
   he circles, comes back through the doorway, inside the compartment; X at the barrier control seals it only while he is
   inside (Panther's "Wait..." / "Now."); pressed in the doorway the barrier stops (never on him) and he slips out - the chance
   resets; after 8 misses the fairness rule seals it.
9. **The optional brief pursuit (BIA_05)** is not built this pass (the relay repair is the required step).
10. **BIA_13 Branch B / the console**: BIA_13B plays when the asteroid is already complete; the "due at the console" rule
    still plays the asteroid's AST_11B (same story beat), so the Cattalus lead is never missed.

## Not covered / known (Round 17)
- ~~3 lines unvoiced~~ - voiced after Dan upgraded ElevenLabs (2026-11-21): BIA_13B_08 / 09 / 10; all 165 lines voiced.
- Visuals are a first pass: the city reads as smooth pale towers in mist; the exterior mothership is primitive-built.
- The optional pursuit (above); residents don't greet her beyond the scripted ones.
- **Testing time went far over the 20-minute rule** this round (several full player-style runs while the test driver was fixed: it
  once set course for the Moon, once got on and off the bike at a route stop - driver bugs, not game bugs). The final proof set
  is one player-style run plus three short targeted runs.
- **First-time-player fight length** (`-bossnovice`) was not run; the player-style fights took 2:43-2:52 (the rule asks ~3-5 min
  for a first-timer, who will be slower) - if it feels short, raise `ratwing` stamina in `boss_tuning.json`.
- Branch B (asteroid already complete) was not played through in a check run (the line order is as in the spec).

## For Dan to check (Round 17)
1. From the star map: "Biavian contact — Veyra" → the journey → BIA_01: Veyra's rings, the mothership, an orb crossing the
   foreground; Nela's welcome; the iris opening, the saucer slipping through, the iris closing behind.
2. The berth: the bike out of the hold, 7 down the beam; Nela at the overlook; the city curving up the shell.
3. The memory echo and the disoriented resident; the route display and the shared route (a line of light, not a vision).
4. Ratwing beneath the high bridge, his drop to the platform, the tail-twitch cut aboard; relay 1; the bridge and the seedlings.
5. The garden: the remembered doorway, the seam, Ratwing at the glass, closing the upper opening.
6. The quiet passage: no Panther inside; the airlock puzzle; "Panther? / Here."
7. The fight: dodge his dives, knock him down with telekinesis, punches / kicks / blades; the partitions; let him knock you down -
   his bar stays; then stage three: wait for "Now." and seal him in.
8. The archive lights rippling through the city; the octahedron; the vision; the record on the chair arm.
9. Back aboard: she sits on the floor, he walks into her lap (no gap); then BIA_13.
10. Nela's voice (premade "Bella") - keep it or name a saved voice.

**Stand-ins: none for the Biavians or the vision** (Dan's real model and video are used). Nothing left to add.

## Round 17: Chapter "The Biavian Mothership (Veyra)", first playable pass — WITH STAND-INS (Dan, 2026-11-21) — done, status above; release 0.17.0
Commit a checkpoint first. Runs overnight — **never stop to ask**: make the reasonable call, list it under "Round 17 — decisions
made". **Every standing rule in `CLAUDE.md` applies from the first line** (as Round 16 — check each against your own captures;
this chapter should arrive clean). Release **0.17.0**; "Round 17 — status"; "For Dan to check (Round 17)".

**The Guide is named Nela** (Dan, 2026-11-21) — all Guide lines are Nela's (dialogue labels, Field Guide, Log); she introduces
herself in BIA_02_01. Her voice: the premade voice you pick (a calm, gentle voice; Kyle's design reads as female).
**The spec: `Docs/CHAPTER_BIAVEH.md`** — read **"Decisions (Dan, 2026-11-21)"** and **the setting correction (Veyra; revised
BIA_01 / BIA_02)** first; then BIA_03–BIA_13 and the implementation essentials.

**UPDATE (Dan, 2026-11-21 05:10): the real Biavian model is IN — `Art/Mixamo/biavian/biavian_body_mixamo.fbx`** (Kyle's
design, Mixamo-rigged with skin; from S4's `Art/Export/biavian/biavian_for_mixamo.fbx`, reference `biavian_ref_front.png`). **Use it
for Nela and the residents — no Zorp stand-in** (ignore the Zorp lines below). Vary residents by size / tone / medallion glyph.
**Stand-ins (originally planned):** Build so they drop out cleanly:
- **Biavians (the Guide, residents):** `Art/Source/biavian_placeholder/Zorp_meshy_rigged.fbx` (Dan's alien from his game Tee'd
  Off; Meshy-rigged humanoid) — **plain monochrome material** (ignore its colour texture / golf clothes), sizes / tones varied,
  calm idle / talk / walk clips (Humanoid retarget, reuse existing). Wrap it so swapping to the real Biavian model later is one
  change (one prefab / one model path). Mark every stand-in in the status.
- **The vision (BIA_11) is IN (Dan, 2026-11-21): `Art/Video/vision_06_mothership.mp4`** (12 s, 1920×1044; still fallback
  `Resources/Biaveh/vision_06_still.jpg`) — the misty memory chamber with hanging crystal-like memory structures; a Biavian seated
  on a stool places a small keepsake into the glowing hollow of a round table, light rises from it; the other comes close and
  rests a hand on the table beside her. Silent — add a soft hum as the light travels into the archive and a faint chime; no
  music. **Use it — no temporary in-engine vision needed** (ignore the stand-in note below for the vision). Wire the full rune-touch → vision sequence with a
  **temporary in-engine render** of the spec's vision (the hazy earlier chamber, two stand-in figures turned away, the object
  placed, the pattern of light travelling into the archive, the hand resting beside theirs — ~12 s, monochrome) so it plays
  properly now; it's replaced by `Art/Video/vision_06_mothership.mp4` when Dan adds it (load it automatically if present).

**What's ready:**
- **Ratwing:** `Art/Source/ratwing/ratwing_v1.glb`, rigged with wing chains (`Art/Export/ratwing/ratwing_v1_rigged.fbx`, Round 4),
  Kyle's art `kyle_ratwing.jpg` / `kyle_ratwing_sketch.jpg`. Voice: use the saved ElevenLabs voice **`Ratwing`** if it exists;
  otherwise a premade voice that fits `Docs/VOICES.md` "Ratwing" (sly, nasal, raspy) — say which.
- **The Guide's voice:** a premade voice picked by you (Dan, 2026-11-21) — same voice for every Guide line, telepathic treatment.
  Residents: other premade voices.
- **Music:** `biaveh_mothership.mp3`, `biaveh_ratwing.mp3` (`Docs/MUSIC.md`: quiet moments fade out — the quiet passage BIA_08
  near-silent, the octahedron chamber + vision, BIA_12).
- **Rune: octahedron** (eight triangular faces, readable from the establishing shot), generated in code.
- **Code-built:** Veyra (a ringed planet, pale debris band), **the egg-shaped mothership** (26 miles across — a convincing sense of
  scale: hull lights, traffic, recessed entrances), **the three guardian orbs** on separate orbital paths, **the mechanical iris
  entrance** (overlapping segments rotating open just wider than the saucer, spiralling shut behind it), the interior city curving
  up the shell (hanging gardens, smooth buildings, transit lights, bridges), the three relays, the suspended bridge, the garden
  with its memory overlay, the shielded passage, the transfer chamber (platforms, partitions, the maintenance compartment), the
  memory chamber with suspended structures — all in Kyle's misty monochrome, no plain boxes.
- **Memory echoes** (BIA_03, BIA_07): translucent, shimmering, clearly distinct from solid structures and from her visions.

**Items** (follow the spec):
1. Getting there: MAR_13C's route / the star map's "Biavian contact — Veyra" (QF22); title card over black from frame one.
2. BIA_01 + BIA_02 (revised): among Veyra's rings, the orbs, the Guide's telepathic welcome, the iris (front shot through it,
   reverse shot as it closes), the guide lights.
3. BIA_03: the receiving berth — **bike out of the bike hold, 7 down the beam**; the interior city reveal; the memory echo and the
   disoriented resident; Ratwing named (Panther's attention).
4. BIA_04: the temple route, the three relays (shown on a display — show, don't caption), the one-way route sharing (a navigation
   impression distinct from visions); objective from the story.
5. BIA_05: first sight of Ratwing (low angle under the bridge; Kyle's design), the exchange, the tail-twitch cut aboard; the
   optional brief pursuit; repair the relay.
6. BIA_06: the suspended bridge puzzle (real structures vs echoes), the residents crossing, the seedlings resident, the
   partition closing one of Ratwing's routes (visible).
7. BIA_07: the overlapping garden — the remembered doorway, the seam, the real release; Ratwing at the glass, Panther's read of
   his feet, closing the upper opening.
8. BIA_08: the quiet passage — park at the entrance (mount / dismount rules), Panther's line before, **no Panther voice inside**,
   a short solo spatial puzzle, the reconnection lines, the Guide's alert.
9. BIA_09 Ratwing per **the Decisions**: swoops and claw passes (dodge / block) → telekinesis knocks him out of the air, stunned →
   close in: punches, kicks (Y), arc blades (stun-only) → **his stamina bar**; partitions shrink his flight space between phases;
   she can lose, he keeps his damage on retry; exhausted, he flees → stage three: Panther's "Don't follow him…" / "Wait…" /
   "Now." → seal the compartment (never on his body / wings; a mistimed try resets the chance) → contained alive; the pleased-noise
   / purring / "the ship is vibrating" lines.
10. BIA_10 restoring the archive (lights rippling through the city); BIA_11 the octahedron chamber, the shared rune-touch → vision
    (stand-in render), the return lines, the record + octahedron awarded once (with the other records).
11. BIA_12: the ride back through restored districts, the beam home, **the reunion straight from the beam-up, no gap** (she sits on
    the floor, he walks into her lap), the full conversation — end there, no extra joke.
12. BIA_13: Branch A (asteroid unfinished → the asteroid) / Branch B (asteroid complete → the Cattalus lead, once — the QF20 rule:
    also at the console if due). Cattalus doesn't exist yet → a "coming soon" card.
13. Progression + saves: the order (mothership + asteroid either order → Cattalus); checkpoints, chapter list + sections, Log +
    Field Guide (the Biavians, the Guide, Ratwing, the mothership) growing with the story.

**Checks + release:** targeted runs with captures; the boss proofs (player-run log: throws / punches / kicks / blades, stamina,
the idle test, retry keeps his damage, the containment timing, a capture sequence); holes / z-fighting / collision checks; one
player-style run from the star map to BIA_13. List every stand-in and exactly what Dan needs to add to replace it. Publish
**0.17.0**; docs; commit + push.

## Side task S4: Nela / the Biavians ready for Mixamo (Dan, 2026-11-21) — DONE (04:32); Dan's rigged body in: `Art/Mixamo/biavian/biavian_body_mixamo.fbx` — skip
Only if Dan's Tripo model `Art/Source/biavian/biavian_v1.glb` exists (reference `Art/Reference/biavian_ref_front.png`, Kyle's
design `kyle_biavian_nela.jpg`; notes in `Docs/MODELS-NEEDED.md` "Biavians"). If it isn't there yet, skip this and say so.
Same as S1–S3 with `Tools/Blender/cleanup_mixamo_body.py` — add a `biavian` preset (**~1.8 m**): weld, decimate to ~30–40 k tris
keeping the face (eyes, the crown ridge), the medallion and the belt buckle readable; A-pose, origin at the feet, texture embedded
→ **`Art/Export/biavian/biavian_for_mixamo.fbx`** + a preview sheet `Docs/Previews/biavian/biavian_for_mixamo_sheet.png`. Then tell
Dan: upload to Mixamo's auto-rigger, download "FBX for Unity, with skin" to `Art/Mixamo/biavian/biavian_body_mixamo.fbx`. Commit
+ push. No release.

## Quick fix QF20: the Biaveh discovery always plays + kicks + bosses don't reset (Dan, 2026-11-20) — done, status below; release 0.15.5
Commit first. Small, focused. Release **0.15.5**; "QF20 — status"; "For Dan to check (QF20)". Prove with real button presses.

1. **The combined-records discovery (MAR_13C → Biaveh) must not be missable.** Dan finished both Mars and Europa and never saw
   it. Today it plays only at the debrief of whichever world is finished second (`WorldFlags.CombinedDue` in
   `Chapter1AboardMars.cs` / `Chapter1AboardEuropa.cs`), so finishing one in an older build, via Scene Select, or any other path
   skips it. Fix: **whenever both are complete and it hasn't played** (`CombinedDue`), play MAR_13C + MAR_13B the next time she
   is aboard and goes to the console / pilot's chair (and at the second debrief as now), then the journey toward Biaveh with
   TRV_AST after the warp. Also check Dan's real save (`seven_mars` / `seven_europa` flags) to see why it didn't trigger, and
   say. The asteroid stays undiscovered until TRV_AST.
2. **Kicks in boss fights.** In the Viper fight Dan only gets punches. QF18 put the MMA kick and the roundhouse at the 4th–5th
   press of the X chain, and only if the scene's animator has those states (`ChapterSevenFight.Chain`: `FightHop &&
   HasState("mma_kick") && HasState("roundhouse")`) — a 3–4 s stun rarely reaches them, and Mars may lack the states. Fix:
   **kicks get their own button in boss fights — Y = kick** (alternating MMA kick / roundhouse; Y is still the takedown when
   he's dazed), X = punches (jab → cross → hook); mixing X and Y chains combos. Every chapter scene's animator has the kick
   states (build check). Update the controls hint / Controls page. Same in the Wormmaster fight. Proof: a player-style Viper
   fight log showing kicks landing (damage by punches vs kicks), with captures of a kick hitting him.
3. **The boss keeps the damage you've done when she goes down** (Dan: "I didn't like how Viper reset to half health every time
   I 'died' — let's make it so he doesn't reset"). When 7 is knocked out and retries, **the boss's energy stays exactly where it
   was** (no refill to the phase's start), the pylons / objectives already done stay done, and the fight resumes in the phase
   he's in; she gets back up with her own health full, a moment to recover (he backs off briefly). Change the "Boss fights"
   rule in `CLAUDE.md` ("retry from the start of the current phase" → "retry keeps his energy and progress"). Same for every
   boss (Wormmaster, the Machinist later). Proof: a run where she goes down mid-fight — his energy before and after the retry
   logged identical.


## QF20 — status (2026-11-20): done, exe-verified off-screen; release 0.15.5 (`game-1505`, published)
**1. Why the discovery never played for Dan (his real save).**
- His registry save (`seven_routes`) reads `combined: true, asteroidFound: true`. Mars and Europa are both complete (`cp 9`).
- His own `Player.log` from today (20:01) shows him finishing Mars (`Mars checkpoint -> 9`). The debrief then took the
  "already played" branch: there is no "Biaveh unlocked" line. So the flag was already true **before** today's Mars run.
- That run itself started with "Mars started fresh" (`Player-prev.log`). "Start Mars over" clears the Mars save but never
  the shared route flags; only a new game does that.
- The older logs are overwritten (Unity keeps two), so the moment the flag was set isn't recorded. The code shows how it
  could be set without him seeing the scene:
  - The flag was set **halfway** through MAR_13C (after line 5), not at its end.
  - It lives in one shared key that Scene Select runs write too. His save shows he played Europa's ending from Scene Select
    (`seven_europa_select`: EUR_17 done).
  - Any earlier pass through a Mars / Europa ending (0.15.0–0.15.2, a Scene Select ending, a quit mid-scene) set it, and
    from then on every debrief skipped it.

**The fix:**
- A second flag, `combinedSeen`, is set **only at the end** of MAR_13C + MAR_13B. "Due" now means both worlds complete and
  not seen to the end.
- Saves that have only the old flag (Dan's) play it once more.
- Whenever it is due, X at the ship's console plays it in the pilot's chair instead of opening the console: MAR_13C +
  MAR_13B, the journey toward Biaveh, TRV_AST, the star map choice (`ShipConsole.Use` → `Chapter1Aboard.CombinedAtConsole`).
  It also still plays at the second debrief, as before.

**2. Kicks.**
- In boss fights **Y = kick** (the MMA kick and the roundhouse, alternating) and **X = punches** (jab → cross → hook, round
  again). Y is the takedown only when he is dazed and in reach.
- Mixing X and Y chains one combo: each blow cancels into the next after its active frame (`ChapterSevenFight.PickBlow`).
- Every chapter scene's animator has the states: build check `KICK STATES CHECK (QF20 2)`, passing on Chapter1 / Venus /
  Europa / Mars.
- Hints, objectives and the Controls page show it (three new "Fight:" rows). The fairness skip moved from Y to View / Enter.

**3. The boss keeps his damage.**
- `BossFight.Retry` no longer restores the phase's starting energy. His energy, the objectives done and his stuns this
  phase all stay.
- She gets up with full health, and he backs off to his side with a 2.6 s pause before his next attack.
- The summary logs `RETRY (QF20): his energy before -> after (identical)`.

**Found on the way: the lock-on toggle never worked.**
- `ChapterInput.LockOn` (R3 / Tab) was created but never enabled, so the toggle in the fight hints did nothing.
- It is enabled now, and a real R3 press toggles it (`LOCK-ON (QF20) toggled ON / OFF` in the log).

**Proof.** Real presses on a virtual gamepad on the real input actions. Every run was muted (peak 0), minimized and
Below Normal.

| run | result |
|---|---|
| `-qf20check` (new, `QF20Check.cs`): Dan's save rebuilt on the test keys, free exploration aboard | walks to the console, **one X press** → MAR_13C in the chair (all 14 lines) → MAR_13B → the journey → TRV_AST → d-pad + A, A on the star map → free exploration; flags `seen`, due `false`; a **second X** opens the console normally. `QF20 DISCOVERY CHECK: PASS` (twice; the first run's driver read the prompt text, which the galley-seat watcher rewrites in player-sim runs - it now reads the console's own "near") |
| Viper `-marsplay -marscp 5 -bosscheck -bossdownonce` (the final 0.15.5 build) | R3 pressed → `LOCK-ON toggled ON`, again → `OFF`; `MELEE (QF20): punches 21 landed = 35 %, kicks 20 landed = 52 %`; she went down once mid-phase 2: `energy 83.9 % before -> 83.9 % after (identical)`; fight 1:20, pinned; X objectives 2 presses, 0 second presses, 0 to a strike |
| Viper, earlier build (same fight code) | punches 37 % / kicks 49 %, retry 83.9 → 83.9 % (identical), fight 1:38 |
| Wormmaster `-venusplay2 -bosscheck -bossdownonce` | punches 40 % / kicks 44 %; retry `84.6 % -> 84.6 % (identical)`; fight 2:19, pinned, containment |
| captures | `boss_viper_kick_hit_mma_kick(_side)`, `boss_viper_kick_hit_roundhouse(_side)`, `boss_wormmaster_kick_hit_mma_kick(_side)` |

Testing time ≈ 9 game-minutes (`-testspeed 2`).

Not covered:
- The QF20 check's screenshots: the player-sim aboard run had no capture camera attached. The driver now attaches one,
  but that build is the next one.
- The kick captures land at the kick's contact frame with her very close to him. The kick reads, but not as a full
  leg extension; the hit moment is in `ChapterSevenFight.HitOf`.
- A first-time-player (`-bossnovice`) fight length with the kept damage. Fights get shorter when a knockdown no longer
  costs him nothing.

## For Dan to check (QF20)
1. Your own save: **Continue** (lands aboard), walk to the ship's console and press X. Instead of the console, she takes
   the pilot's chair with Panther: MAR_13C, MAR_13B, the journey toward Biaveh, TRV_AST and the choice. After that the
   console opens normally.
2. Viper / the Wormmaster: when he's stunned, press **Y** for kicks (MMA kick, roundhouse) and **X** for punches, mixed
   freely. Y is still the takedown when he's dazed at the end.
3. Let him knock you down mid-fight: you get up with full health, and **his bar stays exactly where it was**.
4. R3 / Tab in a boss fight: lock-on now toggles (it never did before).
5. The fairness skip after three knockdowns is now **View / Enter** (Y is the kick).


## Round 16: Chapter "The Asteroid — the Great Machine Age", first playable pass (Dan, 2026-11-20) — done, status below; release 0.16.0
Commit a checkpoint first. If Dan is away, **never stop to ask**: make the most reasonable call, list it under "Round 16 —
decisions made". **All standing rules from `CLAUDE.md` apply from the first line** — monochrome + Kyle's fog / glow (but
readable: thin the fog around the action), non-sexual, 7 never kills, the one saucer prefab (bike always via the bike hold,
beam only for 7 / Panther), the fast bike, one smooth riding camera, complete collision on land and on foot, the holes and
z-fighting build checks, no plain boxes, show-don't-caption, objectives come from the story, no filler tasks, characters shown
talking have lines, every dialogue shot shows the speaker, hand IK released, the boss-fight rule (attacks, can lose,
**telekinesis to stun → close in → strikes, energy bars, the boss keeps his damage on retry**), the shared rune-touch → vision
sequence, the staging variety + Panther's cat moments aboard. **Check each against your own captures before calling it done
— Dan has had to send back every chapter for these; this one should arrive clean.** Release **0.16.0**; "Round 16 — status";
"For Dan to check (Round 16)".

**The spec: `Docs/CHAPTER_ASTEROID.md`** (Dan's script AST_01–AST_11 + implementation notes + **"Decisions (Dan, 2026-11-20)"**
+ the chat's notes). Read the Decisions section first.

**Everything Dan made for it is in the repo:**
- **The Machinist:** Kyle's sketch `Art/Reference/kyle_terminator_villain.jpg` (adapted: no cape; the arm cannon → a maintenance
  claw / cutter arm), reference `machinist_ref_front.png`, Tripo `Art/Source/machinist/machinist_v1.glb`, **rigged body
  `Art/Mixamo/machinist/machinist_body_mixamo.fbx`** (~4 m). The front plate on its own hinge bone (never through his legs);
  the claw on its own bone; no face — the visor slit glows with his voice; small precise head adjustments; deliberate,
  economical movement. Clips: reuse tunneler / YBot (slowed). Voice: **Dan's saved `Machinist`**. The facility **System**
  announcer: a premade voice you pick.
- **7's arc blades** (Decisions): a pair of curved crescent blades of white light (Kyle's `kyle_7_blades_sketch.jpg`), found in
  AST_05's locker with the proposed lines `AST_05_10` / `AST_05_11`; they sever power in machines, only stun living beings; she
  keeps them for later chapters (a draw / sheath on a button, shown on the controls). Clips: `Art/Mixamo/7/blade/` (9 clips,
  single-sword — the second blade rides the off hand; mirror for alternating combos).
- **Music:** `asteroid_city.mp3` (the city onward), `asteroid_machinist.mp3` (the encounter — 1:34, loop cleanly) —
  `Docs/MUSIC.md` (quiet moments fade out: AST_06, the vision, AST_09, the silent cavern, aboard).
- **Vision:** `Art/Video/vision_05_asteroid.mp4` + still `Resources/Asteroid/vision_05_still.jpg` (silent — the machine winding
  down, tool sounds, the bird's uneven chirp + wing flap = the same sound as AST_03_08).
- **Rune: cuboctahedron**, generated in code like the others; the small counterpart awarded once, beside the other records.
- **Code-built** (scripted hard-surface): the maintenance machine, the security machines (escort / restrain — evaded, trapped,
  disabled, never destroyed), the mechanical bird (one repaired mismatched wing; wind-up; uneven chirp + flap), the cargo
  container + receiving scanner, the conveyor, the three factory controls (transport / cooling / archive diverter), the archive
  panels, the cutting mechanism, the service-station clamps, the maintenance cradle — all real, readable, in the Great Machine
  Age style (riveted, steam-age, wheels and pistons, like the vision), with clear feedback when they work.
- Voice every line with an id from the script (`AST_..`), 7 on v3 with the performance notes, Panther telepathic; SFX for the
  chapter (machinery, steam, conveyors, rails, the lost-connection static, the Machinist's servos) → `Docs/SFX-LIST.md`.

**Items** (follow the script; key points):
1. **Getting there:** only after TRV_AST's "Investigate the asteroid" (or later from the star map's Biaveh system, where it's
   marked); main menu chapter list + Scene Select sections; title card over black from the first frame.
2. **AST_01** aboard (the window view: the asteroid with its silver structures, turning rings, white route lights, one intact
   section, one stripped to the frame; Panther's cat moment of being lifted off her lap is in the script).
3. **AST_02 receiving:** the bike out of the hold onto its pad, 7 down the beam onto hers (spec correction), pressurizing, helmet
   off; the maintenance machine scanning her, the System lines; the intro puzzle (redirect the scanner to the container, cross to
   the manual door) — a real little puzzle with a clear payoff.
4. **AST_03 the city that still works:** the wide reveal (factory terraces, rail carriers, repetitive machinery, empty
   workstations); the causeway ride with **three rail-alignment junctions** (each visibly changes the route); the optional bird.
5. **AST_04 the Machinist:** the unfolding reveal, the conversation, the archive panel going into the cutter (insert), the
   shutters closing — escape through the maintenance opening (a short tense run, not a fight).
6. **AST_05:** the observation alcove, the terminal, **the arc blades from the locker** (new lines), then the three controls in a
   compact factory area — each visibly changes the central hall, security machines in the way (blades / telekinesis / dodge;
   nonlethal).
7. **AST_06 the missed answer:** the platform retracts, she drops to the ledge, the connection breaks (static, silence), the
   player climbs while Panther calls; the full exchange unhurried — no combat.
8. **AST_07 the Machinist fight** per the Decisions and the boss rule: his attacks (claw swings, grabs / restraint attempts,
   conveyor lanes, his override runs at the terminal), telekinesis to stun → close in → arc-blade strikes on his **power
   couplings** + punches / kicks → his energy bar; the service-station clamps lock a tool arm at each phase end; the cradle at the
   end; Panther's "enormous arm" lines; he keeps his damage on retry.
9. **AST_08 the archive + vision** (the shared rune-touch sequence; the bird-skipped variant of AST_08_05).
10. **AST_09:** the memory shown on the archive surface beside the restrained Machinist; preservation mode — most lines slow to
    a stop, essential lights stay on, the cavern falls quiet; he opens protected storage; the record + cuboctahedron awarded.
11. **AST_10 back aboard:** the reunion **straight from the beam-up, no gap or wait** (Dan: keep it, but it must not feel like a
    lock-up), forehead to forehead, the purr; then the chair conversation (staging variety rules apply).
12. **AST_11 next destination:** Branch A (Biaveh unfinished) / Branch B (Biaveh complete → the Cattalus lead, once, whichever is
    finished second — the QF20 lesson: also play it at the console if it's due and hasn't played). Biaveh doesn't exist yet →
    Branch A ends on the "coming soon" card for Biaveh.
13. **Progression + saves:** the implementation notes' order, checkpoints for Continue, Log + Field Guide (the Machinist, the
    Machinists, the city, the bird) growing with the story.

**Checks + release:** targeted runs per section with captures you look at; the boss proofs (player-run log with blade / punch /
kick damage, the idle test, the retry keeping his energy, a capture sequence); the holes / z-fighting / collision checks on the
new scene; one player-style run from TRV_AST to AST_11. Publish **0.16.0**; docs; commit + push.


## Round 16 — status (2026-11-21): first playable pass done, exe-verified off-screen; release 0.16.0 (`game-1600`, Latest, published)
**The chapter (items 1-13):**
1. **Getting there.**
   - TRV_AST's "Investigate the asteroid" starts the chapter straight away. The star map's asteroid in the Biaveh system
     does too, once it has been found.
   - Main menu chapter list "The Asteroid" with 10 Scene Select sections. Continue covers it.
   - The title card "THE ASTEROID — THE GREAT MACHINE AGE" shows over black from the first frame.
2. **AST_01:**
   - The pilot's chair, Panther in her lap.
   - The window shows the asteroid: dark rock, silver structures, two rings turning round the recessed opening, white
     route lights, the intact section and the one stripped to its frame.
   - She lifts him off her lap.
3. **AST_02:**
   - The outer gate closes behind the saucer in the receiving chamber.
   - The bike comes out of the hold onto its marked pad, and 7 comes down the beam onto hers.
   - The chamber pressurizes (vents, sound), then her helmet comes off.
   - The maintenance machine scans her, with the System lines.
   - The scanner puzzle (telekinesis turns it onto the marked container → sorting) opens the manual door; the door
     rolls up and light runs round its frame.
4. **AST_03:**
   - The tunnel ride, then the reveal: the camera rises past her shoulder onto the cavern (terraces, rail carriers,
     flywheels, hammers).
   - Three junction spans on piers turn into line, with route lights running ahead; the counter shows "Junctions n / 3".
   - The optional bird at the workstations: picked up, wound, one uneven chirp, the patched wing flaps, put back.
5. **AST_04:**
   - The Machinist rises from his repair cradle (low angle), then over-the-shoulder shots for his scale.
   - The conversation; a panel goes into the cutter, and the insert matches its markings to the temple signal on the
     hall's face.
   - The shutters close. Panther's line sends her to the maintenance opening, which shuts behind her.
6. **AST_05:**
   - Her hand on the alcove glass over the feed line; the terminal shows the bypass route.
   - The locker holds **the arc blades**: AST_05_10 / 11, the first draw.
   - The three controls, each with a look at the hall:
     - control 1: the platforms line up across the pit, and the feed halts;
     - control 2: the blades cut the clamp's power line, the valve turns, the coolant runs, the steam dies, and the
       cutter goes dark;
     - control 3, after AST_06: the diverter turns the panels onto the storage loop.
   - Security machines escort her if they catch her. The blades cut their power and telekinesis pushes them away. They
     are never destroyed.
7. **AST_06:**
   - The assembly retracts and she drops to the ledge.
   - Static, then silence.
   - She climbs the wall herself (A); Panther's lines play over the climb, and the full exchange follows once she is up,
     with no combat.
   - The assembly slides back out.
8. **AST_07:** the dialogue (with the held beat on her), then the boss fight (rule below). Her takedown engages the
   cradle; AST_07_14 / 15.
9. **AST_08:** the archive's turning gimbal round the cuboctahedron, the empty mounting points, the shared touch →
   vision → return (vision_05 video), and the lines. The bird-skipped variant `05S / 05T` played in the release run.
10. **AST_09:**
    - The memory plays on the archive surface beside his cradle, and the line stops on it.
    - X at the terminal sets preservation mode: production eases to a stop, the lights dim, and the ambience goes quiet.
    - "Preservation mode. Essential systems only." Then the cradle opens and protected storage opens; the surviving
      panels go in.
    - The record + cuboctahedron insert (awarded once), then the wide of the quiet cavern.
    - The saucer comes into the cavern, the bike goes into the hold, and she boards in the beam.
11. **AST_10:** straight from the beam-up into the reunion, then the chair talk with the asteroid in the window and an
    archive light switching on out there.
12. **AST_11A:** the lines, then the Biaveh "coming soon" card, then free exploration. Branch B (the Cattalus lead) is
    built: once, and also at the console when due.
13. **Saves, Log and Field Guide:**
    - Checkpoints 0-10, Continue, Scene Select. The blades are kept on the player's save.
    - Log: 9 entries. Field Guide: the Machinist (named from AST_04), the Machinists, the city, the bird; the entries grow
      with the story.

**Assets:**
- 151 voiced lines (`Docs/ASTEROID-LINES.md`): the Machinist = Dan's saved voice; System = the premade "Lily".
- 28 new SFX (`ast_*`).
- Both music tracks are cued: the city onward, and the encounter. The music fades out for AST_06, the vision, AST_09 and
  aboard.
- The Machinist on Dan's Mixamo rig: his own controller; the front plate re-skinned onto a `PlateHinge` bone
  (722 vertices); the visor slit, power couplings and tool assemblies are built in code.
- The blade clips are imported, with mirrored slashes.

**The boss rule (AST_07):**
- He moves and attacks: the cutter / clamp swing, the restraint grab (a lunge), the floor slam (a ring), the lane surge
  (a wave), scrap rain from the hoppers, and the override sweep from the terminal. Each is telegraphed (pose, sound,
  white marker).
- Three phases: at each phase end he reels into a service station and its clamp locks a tool arm. Then he is dazed, and
  the Y takedown engages the cradle.
- He keeps his damage on a retry. Energy bar + her bar on screen.

| proof | result |
|---|---|
| player-style run `-astplay` (the release check: real pad presses from free aboard → console → star map → TRV_AST → the whole chapter → AST_11) | **`ASTEROID PLAYTEST: 1/1, soft-locks 0`**, 17.3 game-min; skips 1 (the passage after the escape - she stopped inside the opening's own zone, which then closed; fixed after, verified by the escape run below); RIDE CAMERA all 0, BIKE vs GEOMETRY ON LAND 0 / 0, FEET body inside geometry 0, DIALOGUE FRAMING 63 lines, still blocked 0; record awarded once; audio peak 0 |
| first-time-player boss run (`-astplay -astcp 6 -bosscheck -bossnovice -bossdownonce`, pad) | **fight 4:29**, attacks 69 (15.4/min), she went down 3x; `RETRY (QF20)`: 66.0 → 66.0 %, 33.0 → 33.0 %, 33.0 → 33.0 % (identical); `MELEE`: arc blades 35 = 51 %, kicks 34 = 35 %, punches 0 (she fights with the blades drawn); melee 90 % of the damage; stuns 9 |
| idle test `-astcp 6 -bossidle` | `BOSS IDLE TEST (machinist): knocked down after 28.5 s` - PASS |
| capture sequence | `boss_machinist_<attack>_telegraph / _active (+ _side)` for every attack, `_stagger_1-3`, `_kick_hit_mma_kick / _roundhouse`, `_station1/2_clamp_locks`, the cradle |
| Auto check runs `-astcp 1 -astbird` (x2) | the whole chapter AST_02 → AST_11 incl. the bird scene: `ASTEROID CHECK RUN DONE`; they found the bend railings, the alcove wall, the clamp reach, the rune walk, the fog, a dozen camera framings - all fixed |
| escape run `-astplay -astcp 3 -stopafter 150` (final build) | out through the maintenance opening (a walking pad pilot on its 2nd, longer try; a runner makes the first), on into the alcove without a skip |
| aboard `-astcp 0`, `-astcp 9` (final build) | the asteroid in the window, Panther onto the desk, the reunion, the chair, AST_11A + "Biaveh (coming soon)"; hand IK released at the end |
| build checks (final build) | `HOLES CHECK (QF18 13) Asteroid: 9945 rays, 0 holes`, `COPLANAR CHECK 0` (61 objects nudged), `NPC PLACEMENT 0`, `SAUCER CHECK (QF13 16) Asteroid: the one prefab, unchanged`, `KICK STATES CHECK (QF20 2) Asteroid` ok; `QF18 build checks: all passed` |
| monochrome | max channel difference **0** over every capture (118 checked in the first run) |

**Testing time: over the 20-minute rule, about 68 game-minutes.**
- Two full Auto check runs (about 21 min) and four tries of the player-style run (about 38 min). The first three tries
  each stopped on a real problem: a stale "Get off" prompt at the hall plaza; the Machinist, then a service station,
  standing in the escape line; and the escape's guide light short of the line that counted as "out".
- The rest were short boss and aboard runs.
- Each fix needed a full rebuild (about 10 min).

**Not covered / known:**
- The reunion is forehead-near-forehead: she bends down to him, she does not lift him to her face (the cat's lift reads
  poorly on his rig). The chair scenes use the Chair staging both times, as the script asks.
- The cavern wide still leans on Kyle's bright haze: the terraces fade into it at their distance (the fog is thinned for
  the reveal).
- The Machinist's front-plate swing is built and logged (722 vertices on `PlateHinge`). I did not take a close-up of it
  while he walks.
- The bird scene was checked in the Auto runs only (the release run skips optional bits). The Branch B / Cattalus lead
  cannot play until Biaveh exists.
- No sound check of the new SFX / voices (every run muted); the SFX were generated and are listed in `Docs/SFX-LIST.md`.

## For Dan to check (Round 16)
1. **Getting in:** from Mars + Europa done, take the console's star map → Biaveh. TRV_AST plays; choose "Investigate the
   asteroid". Or use the main menu → "The Asteroid".
2. **AST_01:** the asteroid in the window (rings turning, route lights, the stripped frame). She lifts Panther onto the
   desk: "I was working."
3. **The receiving chamber:** the bike out of the hold onto its pad, her down the beam, the helmet coming off after the
   pressurizing. The scanner's light on her; RT on the scanner arm turns it onto the marked crate; the door control
   lights; X opens the door.
4. **The causeway:** the three junction levers work from the bike, stopped beside them. Each span turns into line and the
   lights run ahead. Optional: get off at the empty workstations (after junction 2) for the little bird.
5. **The Machinist:** his scale, his visor glowing as he speaks, the panel into the cutter. Then run for the
   maintenance opening.
6. **The blades:** RB / G to draw and sheathe; X cuts the clamp's power line, and cuts the security machines' drive (they
   sag, then recover). Do they read as crescents of light, not sabers?
7. **AST_06:** the drop, the silence, then press A at the wall to climb. Is the exchange unhurried enough?
8. **The fight:**
   - Stun him with RT when he overreaches.
   - Cut his glowing couplings with X (blades drawn) and kick with Y.
   - Each phase end clamps a tool arm into a station.
   - Let him knock you down: his bar stays.
   - About 4½ min for a first try; `boss_tuning.json` "machinist" is the knob.
9. **AST_08 / AST_09:** the vision, the memory on the screen beside his cradle, preservation mode (the cavern goes
   quiet), the record.
10. **AST_10:** straight from the beam-up into the reunion. Does it still feel like a lock-up, at about 15 s?
11. The Log and Field Guide entries for the chapter.

## Round 16 — decisions made (Dan away; easy to change)
1. **AST_01's approach to the docking recess** is shown, not flown: the window frames the asteroid in the chair scene. The
   Asteroid scene then opens inside the receiving chamber as the outer gate closes behind the saucer. There is no saucer
   flying anywhere else in the game.
2. **The intro puzzle (AST_02).**
   - While the receiving scanner's cone of light holds her, the door control's lamp is dark, and the maintenance machine
     keeps itself between her and the control. It is an obstacle, never hit.
   - Telekinesis (RT) on the scanner head turns it onto the marked container, and the container rides the conveyor into
     sorting. The maintenance machine escorts it, and the control's lamp lights.
   - X at the wheel then opens the door: the wheel spins, the door rolls up and light runs round its frame.
3. **Proposed lines so every objective follows from the story:** `AST_02_P1-P4`, `AST_03_P1-P2`, `AST_04_P1`, `AST_05_P1-P4`
   (+ Dan's proposed `AST_05_10 / 11`), `AST_07_P1` (the fight hint, only after 35 s without a stun), `AST_08_05S / 05T` (the
   bird-skipped variant from the script), `AST_09_P1-P3`. All are listed in the spec's new table and voiced.
4. **The junctions:** each is a riveted span on a central pier, set across the causeway for the carriers' rails, so the gap
   is visible. The lever works from the bike when she is stopped beside it, or on foot. The span turns into line, the
   route lights run ahead to the next stop, and its lamp turns white. A counter shows "Junctions n / 3".
5. **AST_04's escape:** the maintenance opening's shutter comes down over 16 s. If she is too slow, a white fade puts her
   back with 4 s more each time; there is no fail screen. The hall stays sealed until AST_07, and nothing she needs is
   inside it.
6. **AST_05:**
   - Panther's line points her to the locker, where she takes the arc blades.
   - The security machines escort her: if they catch her she is walked back to the works' entry, under a fade. After
     three escorts they stand down (fairness).
   - Answers: X with the blades drawn cuts a machine's drive power (disabled 25 s, never destroyed), RT pushes it away
     (stunned 4 s), or she steps out of reach.
   - Control 2's valve is held by a powered clamp, and the blades cut its power line. That gives them a real job right
     away.
   - Each control's change in the central hall is shown by a short look at the hall:
     - control 1: the feed to the cutter halts;
     - control 2: the cutter cools, its saw goes dark;
     - control 3: the panels turn onto the storage loop.
7. **AST_06:**
   - The final assembly retracts under her, and she drops to the maintenance ledge.
   - Static, then silence.
   - She climbs a real wall (A at its foot), and AST_06_05 plays as she hauls herself up. The unhurried exchange follows
     once she is up: she sits back for 07, then gets to her feet.
   - The assembly slides back out afterwards, so she can go back.
8. **The fight (AST_07):**
   - Three phases end at his energy marks (0.66 / 0.33 / dazed). At the end of phases 1 and 2 he reels into a service
     station, whose clamp slams shut on a tool arm: first the cutter, then the clamp tool. These are beats, not X switches:
     the rule says no objectives while he waits.
   - Panther's "enormous arm" lines: 10 / 11 as the fight opens, 12 / 13 at the first clamp.
   - The final takedown (Y) engages his repair cradle.
9. **AST_09:**
   - The memory plays on an archive surface beside his cradle; it is the vision video.
   - X at the terminal sets preservation mode. The cradle opens only after he says "Preservation mode. Essential systems
     only."
   - Then: protected storage, the record, and the wide of the quiet cavern.
   - The way home: Panther brings the saucer down into the cavern over the hall plaza. The bike goes into the hold, and
     she boards in the beam.
10. **AST_10:**
    - It opens already in the reunion: she is kneeling at the boarding area and he walks the last steps.
    - She bends to him, forehead to forehead. The lines and the purr follow, about 15 s, with no walking and no waiting.
    - Then the chair talk (the asteroid through the window, an archive light switching on out there) and AST_11.
11. **AST_11:**
    - Branch A ends on the Biaveh "coming soon" card, then free exploration.
    - Branch B (the Cattalus lead) is built: once, whichever of the asteroid / Biaveh is finished second, and also at the
      console when it is due. It can only play once Biaveh exists.
12. **The arc blades:**
    - RB / G draws and sheathes them; the sheathed hilts are crossed on her back.
    - X cuts while they are drawn.
    - In boss fights X becomes the blade chain: in → out → (mirrored) in → out → the combo. Y still kicks.
    - Dan's clips are single-sword; the second blade rides the off hand, and the mirrored slashes alternate.
13. **The System announcer** is the premade ElevenLabs voice "Lily" (clear, British, middle-aged); its lines are flat
    recorded announcements with a soft chime.
14. **The rune** is a cuboctahedron, 0.42 m, on a turned pedestal inside three slowly turning riveted rings, which keep
    turning in preservation mode. Its small copy (6 cm) joins the records on the chair arm.
15. **AST_01_07 "the adjoining seat":** the saucer has no seat beside the pilot's chair, so she lifts him onto the console
    desk beside her. That is where he sits in the route scenes.
16. **The Machinist during the escape:** after "Request denied" he turns back to his work at the cutter and lets the
    shutters do the rest. When he walked toward her he stood in her way.
17. **First-fight length:** his damage numbers are tuned so a first-time player's fight is about 4½ min. The table is
    `boss_tuning.json` "machinist": `strikeDamage 0.0075 / kickDamage 0.011 / bladeDamage 0.0155`, `stunTime 3.2`.
18. **Service station 2** stands at (-372, 84), out of the line from the meeting spot to the maintenance opening.

## Side task S3: the Machinist ready for Mixamo (Dan, 2026-11-20) — DONE; Dan's rigged body in: `Art/Mixamo/machinist/machinist_body_mixamo.fbx`
Same as S1 / S2 with `Tools/Blender/cleanup_mixamo_body.py` — add a `machinist` preset (**~4.0 m**) — for Dan's Tripo model
`Art/Source/machinist/machinist_v1.glb` (~60 MB; reference `Art/Reference/machinist_ref_front.png`, notes in
`Docs/MODELS-NEEDED.md` "The Machinist"): weld, decimate to ~40–50 k tris keeping the visor, collar, exhaust stacks, chest core,
gauges, the front plate's crescent / glyphs and the claw readable; A-pose upright, origin at the feet, texture embedded →
**`Art/Export/machinist/machinist_for_mixamo.fbx`** + a preview sheet (front / side / back)
`Docs/Previews/machinist/machinist_for_mixamo_sheet.png`. Make sure the **front plate isn't fused to the thighs** (a small gap)
so Mixamo doesn't glue it to the legs; the right forearm ends in the claw tool (Mixamo will treat it as a hand — fine). Then tell
Dan: upload to Mixamo's auto-rigger, download "FBX for Unity, with skin" to `Art/Mixamo/machinist/machinist_body_mixamo.fbx`.
Commit + push. No release.

**S3 — status (2026-11-20): done.**
- **Ready:** `Art/Export/machinist/machinist_for_mixamo.fbx` (5.3 MB; texture embedded, grayscale 2048 also in `Textures/`).
  Preview sheet `Docs/Previews/machinist/machinist_for_mixamo_sheet.png` (front / side / back), plus q34, head and three
  plate close-ups (`_plate_front / _plate_low / _plate_side`); log in `machinist_mixamo_metrics.txt`.
- **The mesh:** Tripo's 1.94 M tris, 990 loose pieces welded (0.6 mm) into one shell, decimated to 45,000 tris. 4.00 m,
  feet at 0, facing −Y in Blender (+Z in Unity / Mixamo). A-pose, arms 56–58° below horizontal.
- **Centring:** new for this preset, he is centred on his legs at knee height. A mid-height median would have put the
  root 15 cm to the side, pulled by the claw arm.
- **Front plate (`plate_check`):** below the belt it is its own piece at every height (1.3–1.85 m). Gaps to the thighs:
  11 / 11, 13 / 12, 16 / 17, 5 / 8 and 7 / 14 cm; the smallest is 5 cm, just under the belt. It is joined to the body only
  at the belt, so it was not fused and needed no cutting.
- **Readable at 45k:** the visor, collar, exhaust stacks, chest core, gauges, the plate's crescent and glyphs, the claw
  and the mechanical hand.
- **Dan:** upload the FBX to Mixamo's auto-rigger (place the markers; the claw counts as his right hand), then download
  "FBX for Unity, with skin" to `Art/Mixamo/machinist/machinist_body_mixamo.fbx`. In Unity the plate still wants its own
  hinge bone at the belt (MODELS-NEEDED note); that is for when he goes into the game.


## Quick fix QF19: the Viper fight's pylons actually work (Dan, 2026-11-20) — done, status below; release 0.15.4
Commit first. Repeat item (QF17 → QF18 → still broken in Dan's play): say why QF18's check passed while Dan's presses did
nothing, fix the cause, prove it with **real button presses** (the virtual gamepad pressing X like a player — never calling
the pylon / channel code directly). Release **0.15.4**; "QF19 — status"; "For Dan to check (QF19)". Small, focused round.

Dan (0.15.3): the objective says "shut his two pylons down" but the counter says "0 / 3"; he stands beside a pylon, presses
X as the prompt says, and **nothing happens**. "Why is it this complicated?" What the chat found in the code:
`BossFight.StartChannel` makes X start a **3-second hold** that cancels on any hit, dodge, hop or flinch, or if she's more
than 2.1 m away — while Viper attacks nonstop — with only a small "Shutting it down… %" in the prompt; **X is also her strike
button** in the fight (the press may go to a strike instead); and the counter shows 3 while only 2 are required.
**Dan's decision: keep the pylons, simplify:**
1. **One press of X next to a pylon shuts it down instantly** — no hold, no channel, no interruption. Give it a generous reach
   (~3 m) and a clear prompt only when in reach ("X: Shut down pylon"). **In reach of a pylon, X uses the pylon, not a
   strike** (a strike only when no pylon is in reach). Remove the channel from every boss objective (Venus devices too).
2. **Big, unmistakable effect** at once: a bang and sparks, its light dies, panels blow open, a power-down sound, its barrier
   / beam drops, a short camera nudge toward it; the prompt is gone.
3. **The counter only counts what's required:** "Pylons 0 / 2" in phase 1 (the objective text matches). Drop the optional third
   pylon (remove its sweep too, or make it part of the platform) — no optional objectives mid-fight.
4. **When both are down**, the platform sinks and he drops down → phase 2 (stun with telekinesis → close in → hand to hand, his
   energy bar), as QF18.
5. Proof: a player-style run where the virtual gamepad walks to each pylon and **presses X once** under fire — both go down on
   the first press, the counter reads 1 / 2 then 2 / 2, the platform sinks; a capture of each pylon's shutdown; check the same
   for the Venus devices.


## QF19 — status (2026-11-20): done, exe-verified off-screen; release 0.15.4 (`game-1504`)
**Why QF18's check passed while Dan's presses did nothing.** The check measured the result ("pylon shut down" turned up in
the log) and never how many presses that took. The pilot played the old design the way no person would:
- It walked to 0.9 m from the pylon's centre and stood there.
- It pressed X about every 6 frames while the 3-second channel ran. A repeat press on the same pylon did nothing
  (`StartChannel` returned early), so mashing cost it nothing.
- After every interruption (a dodge, a hop, a hit) it started again at once, until some 3-second gap between Viper's
  attacks let a channel finish.
The log counted the interruptions (`INTERRUPTED at n %`), but no check failed on them.

Dan pressed once, as the prompt said. That only started a hold, shown as a small "Shutting it down… 0 %" in the prompt.
Three things then undid it: stepping a little away (more than 2.1 m from the centre; the pylon's base already fills
about 1.2 m of that), any dodge or hop, or Viper's next hit. A press that came while she was still in a strike or a
flinch went to a strike instead, because the use only fired from Idle / Block. And the counter said 3 while 2 were
required.

**What changed** (items 1-5):
1. **One press, instant.** The channel is gone from every boss objective (`BossFight.StartChannel / UpdateChannel /
   ChannelProgress` removed; `objectiveTime` dropped from `boss_tuning.json`).
   - X in reach (**3 m from the pylon's centre**) shuts it down at once. Nothing interrupts it, because there is nothing
     left to interrupt.
   - Prompt **"X: Shut down pylon"**, shown only in reach.
   - `ChapterSevenFight`: in reach, X **always** uses the objective, whatever she is doing (mid-strike, block, dodge,
     flinch; only not while she is down). It strikes only when no objective is in reach.
   - Venus: the north device works the same way ("Shut down device", 3 m, one press). The ledge device and the crystal
     were already RT.
2. **The effect** (`MarsPylon`):
   - While it is on, each pylon shows what it does: a flickering white **power beam** runs from its head to Viper's
     platform.
   - On the press, all at once: a bang (`land_hard` + `fight_block`) and a white flash, a burst of 260 sparks, and a
     white shock ring.
   - The beam snaps off, the head stutters and dies, and the panels blow open in 0.45 s. Then the power-down sound.
   - The camera eases ~45 % toward it, with a small shake (`BossFight.NudgeCamera`). The prompt is gone.
3. **The counter counts only what is required:** "Shut down his two pylons (X)" with "Pylons 0 / 2" on its own line, so
   it no longer runs under Viper's bar. The third emitter is part of the platform: it dies when the platform sinks. The
   energy sweep exists only while he is on the platform (removed from phases 2 / 3 in the table), so there is no optional
   objective mid-fight.
4. **Both down → the platform sinks**, he drops to the plaza → phase 2 (stun with TK → close in → hand to hand), as in
   QF18. She now starts the fight **in the middle of the plaza** (`ViperFight.HerStart`). Before, she started 4 m from
   pylon 1, so it went down 0.5 s in, before his first attack.
5. **Proof: the virtual gamepad presses X once** (`BossPilot`):
   - It walks toward the pylon until the X prompt shows, then sends **one** pad press (the pad never drops it any more).
   - A second press at the same objective after 1 s is counted as a failure (`second presses`).
   - Independently, `ChapterSevenFight` counts real pad X presses while an objective prompt showed, and how many of
     those went to a strike.
   - All runs muted, minimized, Below Normal; captures in the session scratchpad.

   | run | result |
   |---|---|
   | Viper `-marsplay -marscp 5 -bosscheck` (final build) | pylon 1 down on press 1 at 3.0 m → **Pylons 1 / 2**; pylon 2 down on press 1 at 2.9 m while his energy sweep was recovering → **2 / 2** → platform sinks → phase 2 → pinned. `X OBJECTIVES (QF19): the pilot pressed X 2x, second presses 0; pad presses at a prompt 2, went to a strike 0`. Fight 1:14, hits 2, down 0× |
   | Viper, the two runs before (same build except the objective text) | the same: 2 presses / 2 pylons / 0 to a strike / 0 second presses; one run shut pylon 2 during the sweep's **telegraph** (under fire). Fights 1:13 / 1:09 |
   | Venus `-venusplay2 -bosscheck` (C06 → containment) | north device down on press 1 at 2.9 m: presses 1, second 0, to a strike 0; ledge + crystal (RT) → 3 / 3; fight 1:58 → containment, creatures harmed 0 |
   | captures | `boss_viper_pylon1/2_shutdown(_side)`, `_after(_side)`; `boss_wormmaster_device1_shutdown / _after`; monochrome check over 47 images: max channel diff **0**; audio peak 0 in every run |

   Testing time ≈ 14 game-minutes (four short Viper runs, one Venus run).

   Not covered:
   - A `-bossnovice` run: the fight is shorter now that phase 1 has no channel (phase 1 took 9-19 s with the expert pilot).
     The first-time length (3-5 min target) is not re-measured; if it plays short, it is a table change.
   - The first Viper run ended by itself at 107 s, mid-phase 3, with a clean shutdown and no quit request. The three
     runs after it finished normally, and I didn't find the cause.
   - A sound check of the new bang.

## For Dan to check (QF19)
1. Mars → the temple fight: walk up to a pylon. "X: Shut down pylon" shows from about 3 m. Press X **once**: it bangs,
   sparks, the beam into his platform dies, the panels blow open, the counter goes 1 / 2. Do the same at the second
   pylon, even while he is attacking: 2 / 2, the platform sinks and he comes down.
2. Near a pylon X never throws a strike. Away from the pylons X strikes as before.
3. The objective reads "Shut down his two pylons (X)" / "Pylons n / 2" and sits clear of Viper's bar.
4. Venus control chamber: the north device goes down with one X press at about 3 m.
5. Does phase 1 now feel too short? It's the gap numbers in `boss_tuning.json` (viper, phase "from his platform").


## Quick fix QF18: show it, don't write it + Dan's Mars review (Dan, 2026-11-20) — done, status below; release 0.15.3
Commit first. All standing rules + the repeat-item rule. 20-minute rule. Release **0.15.3**; "QF18 — status"; "For Dan to
check (QF18)". Dan's screenshot: `Docs/Previews/qf18/dan_mars_window_text.png` (aboard at the Mars arrival: the window is
fog with one ravine outline, and a line of text across the screen describing what should be there).

1. **New standing rule — add "Show, don't caption" to `CLAUDE.md`:** on-screen text is only for titles / chapter cards,
   objectives, control prompts, menus, subtitles of spoken lines and the console's pages. **Never** text describing what the
   player is supposed to be seeing — when the spec describes something to show, build it (even simply), never caption it.
2. **Replace every descriptive caption with the real thing** — found so far:
   - `Chapter1AboardMars.cs:82` "MARS · two fortress cities either side of a ravine · the temple on its island of stone" →
     the navigation display **shows** it: a glowing top-down / three-quarter holographic view of the actual Mars level (the
     plain, the ravine, the two cities, the temple island, the barricaded bridges), the temple highlighted.
   - `Chapter1AboardMars.cs:93` "both approaches barricaded" → the display zooms to the two bridges, the barricades marked.
   - `Chapter1AboardMars.cs:228` "the Mars route + the Cetaceans' sequence · the intervals match" → the display shows **the two
     records side by side** (the Mars route line; the Cetaceans' sequence as a pattern of pulses), their intervals lining up
     with a soft pulse, then **the route completing** out to Biaveh.
   - `Chapter1AboardEuropa.cs:91` "EUROPA · ice field · a passage…" → the display shows the ice field with the passage through
     it marked near the temple coordinates.
   Search every director / aboard script for any other caption of this kind and replace it the same way; list them in the status.
   Build the display views from the real levels (a render of the actual scene from above, baked at build time, shown in the
   console's glowing hologram style) so they always match.
3. **The window views must show the real world, not fog** (QF16 1 was too thin: Mars showed a single ravine outline). Bake a
   panorama of each world's **actual level** from where the saucer hovers (at build time), with Kyle's fog / glow but the
   landmarks clearly readable: Mars — the plain, the ravine, **both fortress cities, the temple on its island** (with QF17's
   new architecture); Europa — the ice field, its fractures, Jupiter; Venus — the gardens, terraces, clouds; the Moon — its
   surface, Earth. Captures from the interior at each world compared with the level itself.
4. **Panther's shoulder perch (MAR_01) looks wrong** — he hangs off the side of her head with his hind legs dangling. He should
   **sit on top of her shoulder**, small, balanced: all four paws on the shoulder (front paws forward, hind paws tucked),
   body along the shoulder line, tail down her back (the tail-flick moment still works), head beside hers looking out of the
   window. Check from the camera angles the scene uses.

5. **The objective box is hard to read — white text washes out** against the bright fog / sky (all worlds). Give it a
   background: a soft dark translucent panel behind "OBJECTIVE" and the text (rounded, slightly blurred edges, in keeping
   with the game's look), plus a subtle text shadow / outline. Do the same for the other on-screen text that sits over the
   world (control prompts, hints, the guide marker's distance, subtitles). Check captures against the brightest backgrounds
   (Venus gardens, Europa's bright water, Mars's fog, the Moon's white fog).

6. **Mars checkpoint (MAR_02): a large bright open gap in the ground** beside the supply transport — Dan's screenshot
   `Docs/Previews/qf18/dan_mars_checkpoint_gap.png` (the glowing white rectangle, bottom right). You don't fall through it,
   so it looks like a hole in the ground mesh showing the bright fog / sky below (or a missing floor tile / an unfinished
   trench). Find what it is; close it with ground, or if it's meant to be something (a trench, a pit), make it read as one
   with real walls and a bottom. Also in the same screenshot: **the supply transport is a plain box with a Reptilian
   standing inside it** (only his feet show below it) — QF17's no-boxes rule applies to vehicles too (a real transport:
   hull, cab, wheels / hover pads, cargo), and nobody stands inside geometry; the Reptilians stand with arms held out
   stiffly (an A-pose look) — give them their idle / talk clips. (The subtitle cut-off in the screenshot is only Dan's crop — not a bug.)
   **One Reptilian stands right on top of the gap** — after fixing it, check nobody is placed over a hole or inside
   geometry (a quick placement scan of every NPC's feet against the ground).

7. **Dialogue shots that show a wall instead of the speaker** — Mars MAR_02, the worker's "Those are water filters. They're
   waiting for them.": the camera looks at the containers and the worker is hidden behind them
   (`Docs/Previews/qf18/dan_mars_worker_hidden.png`; the containers are plain boxes too — QF17's no-boxes rule). Fix this shot,
   then make it a **rule for every dialogue shot in every chapter**: before a line plays, check (a ray from the camera to the
   speaker's head and chest) that the speaker is visible and in frame; if blocked, pick another angle (over the listener's
   shoulder, a side two-shot) or move the camera — never a shot of a wall. Telepathic Panther lines are exempt (he's aboard).
   Run the check over every scripted line in Mars, Europa, Venus and Chapter 1 and list any shots it had to change.

8. **Viper's reveal (MAR_03): the guard blocks Viper most of the time** — `Docs/Previews/qf18/dan_mars_viper_blocked.png`: the
   subtitle says VIPER but the shot is a low close-up of a helmeted guard. **Dan wants close-up shots of Viper while he
   speaks**: for each of his lines a close-up / medium close-up of **Viper himself** on his raised platform — low angle, the
   harsh white light from beneath, his hood and glowing eyes readable, the fog behind him (not washed out — enough contrast
   that his head reads clearly); 7's replies over her shoulder looking up at him; the wide low-angle reveal first as the
   spec says. Move the guards so none stands between the camera and Viper (item 7's visibility check catches the rest).
   Same for his lines in the encounter (MAR_07) and after it.

9. **Objectives must come from the story — she can't just know** (Dan: "how can she just know that she needs to go there
   and how to get there?" — `Docs/Previews/qf18/dan_mars_route_objective.png`: "Take the old maintenance route down into the
   ravine" appears from nowhere after Viper leaves). Add to `CLAUDE.md` ("Show, don't caption" section): **every objective
   follows from something she was told or saw on screen just before** — a line, a sight, a display; never knowledge from
   nowhere. Audit every objective in Mars (and Europa / Venus) against this and list the ones changed.
   **Fix for MAR_03 — new lines after "I noticed him noticing." (proposed by the chat; Dan to confirm or edit — if he hasn't
   by the time you get here, use them as written):**
   | id | who | line | direction |
   | --- | --- | --- | --- |
   | `MAR_03_11` | Worker | There's an old maintenance route under the crossing. The builders used it. | quietly, with a glance at the guard |
   | `MAR_03_12` | 7 | Can the transport get through? | |
   | `MAR_03_13` | Worker | Not anymore. The service gate's been dead for years. | |
   | `MAR_03_14` | 7 | Then I'll take a look. | |
   | `MAR_03_15` | Panther | I see it from here. A ramp cut into the bank, east of you. | telepathic |
   Then a short shot **showing the route's entrance** (old stone steps / the ramp cut into the bank, a half-buried gate), and
   only then the objective changes ("Find another way across" → "Take the old maintenance route down into the ravine").
   Voice them (the worker's premade voice, 7 v3, Panther); add to the spec and `Docs/MARS-LINES.md`.

10. **The west ramp down into the ravine is broken** (the maintenance route, spec 4) — Dan's screenshots
    `dan_mars_ramp_top_gap.png` (a bright open gap at the top where the ramp should meet the ground — the same kind of hole as
    item 6) and `dan_mars_ramp_end_wall.png` (the ramp ends **straight into a wall**; you only get through because the bike
    clips through it). **Not acceptable for gameplay.** Fix: the ramp meets the ground seamlessly at the top (no gap, no lip)
    and opens at the bottom onto the ravine floor / the route onward — a real opening, cut, or turn, never a wall. **Clipping
    through walls must not be possible on land either** (QF15 made collision complete under water; do the same for the land
    bike and for 7 on foot on every world: every solid surface stops her, she slides along it). Then **ride the whole Mars
    route end to end at full speed** (arrival → ravine → causeway → passage → east ramp → service gate → far bank → the
    controls → the plaza → the temple → back) and the other worlds' land routes: the route is continuous with **no gaps,
    no dead ends into walls, no place where getting on only works by clipping**; list every spot fixed. Include the QF15-style
    wall test for the land bike (full speed into and along walls, 0 frames inside geometry).

11. **The broken causeway repeats Venus's bridge puzzle, with no reason, and the restored slab shimmers**
    (`Docs/Previews/qf18/dan_mars_causeway.png`). (a) **Don't repeat Venus's "lift the slab across the gap".** On Venus the
    quake broke the walkway; here it's just broken. Replace it with a **different puzzle that has a visible story reason**
    tied to Mars's division. Proposal: during the division one kingdom **raised and chained an old stone drawbridge section**
    to cut the maintenance route — the raised span, heavy chains, a lock / counterweight, a faded order-sign of one kingdom on
    it; she breaks the lock / swings the counterweight with telekinesis and the span lowers into place (heavy, rumbling,
    dust). Readable at a glance, different from Venus, and it says something about the two sides. (b) **The shimmer** is the
    same z-fighting as the Moon's door frames (QF9: two coplanar surfaces) — the placed section sits flush on / in the
    causeway surface. Fix it (offset, merge, or cut the hole) and **scan every level for coplanar overlapping faces** so it
    can't come back anywhere — note QF9's check (`FightDemoBuilderZFight.cs`) only runs on the **Chapter 1** build; run it on
    every scene (Saucer, Venus, Europa, Mars) and on objects that move into place at runtime (this slab), and log the count. Also in the shot: flat light rectangles on the ground and a
    plain table-like structure — QF17's no-boxes rule.

12. **The east ramp + the sealed service gate** (`Docs/Previews/qf18/dan_mars_service_gate.png`). (a) The east ramp has the
    **same problems as the west ramp** (item 10) — fix it the same way. (b) **The gate "puzzle" is filler** — Dan: "all you do
    is stand up and press a button … just seems like throwing something at you to throw something at you." New standing rule
    for `CLAUDE.md` (with "Boss fights" / "Show, don't caption"): **no filler tasks — every task has a reason the player
    understands, a little real play, and a visible payoff.** For this gate (it's what lets the supply transport through —
    spec 4, so it matters): proposal — the gate's power line was **deliberately cut** during the division; she **follows the
    dead conduit** along the ravine wall to the break (a short on-foot / riding stretch), finds the severed cable ends hanging
    apart, and **pulls them together with telekinesis** while sparks arc (hold steady for a moment); the power runs back along
    the conduit in a visible glow, the gate grinds open, and — the payoff — **the supply transport comes through it**, its
    driver raising a hand to her. Or, if the chat's proposal doesn't fit the layout, anything with the same three parts.
    Apply the same test to every other "press X at the panel" task in Mars, Europa and Venus — list each and what it became
    (or why it stays).

13. **Gaps in the ground are everywhere in Mars — fix them as a class, not one by one** (`dan_mars_gap_strip.png`: a long
    glowing strip between two ground pieces that 7 walks along without falling — so there's collision there but no visible
    floor, or the pieces don't meet; also items 6 and 10). Find the cause in how the Mars level pieces are built / placed (ground
    tiles, ramps, causeways not meeting; a collider without its mesh) and fix it at the source. Then add a **"holes" build
    check for every scene**: cast a dense grid of rays down over every walkable / ridable area and along every route; any ray
    that finds a collider but no visible surface, or sees through to the fog / sky below, is a hole — the build reports them
    and fails above 0. Log the count per scene.

14. **Mimed conversations — the worker "talks" with no dialogue** (`dan_mars_worker_mime.png`: "Talk to the worker by the
    checkpoint (X)" → he gestures, nothing is said). Round 15 decided to mime the lines the spec gives as action only; Dan
    doesn't want that. Rule (add to `CLAUDE.md`): **if a character is shown talking, they have voiced lines.** New lines
    (proposed by the chat; Dan to confirm or edit — if not by the time you get here, use them as written):
    **The worker volunteers (spec 6, start):**
    | id | who | line | direction |
    | --- | --- | --- | --- |
    | `MAR_06_V1` | 7 | The service route's open. The caretaker on the far bank will work the other control. | |
    | `MAR_06_V2` | Worker | The caretaker? From over there? | surprised |
    | `MAR_06_V3` | 7 | They're waiting for someone on this side. | |
    | `MAR_06_V4` | Worker | Then I'll go. Someone should. | a beat; a glance at the guard first |
    | `MAR_06_V5` | 7 | Thank you. | warmly |
    | `MAR_06_V6` | Panther | Two volunteers. Practically a treaty. | telepathic, dry |
    **The guard and the caretaker's "few practical words" (spec 11):**
    | id | who | line | direction |
    | --- | --- | --- | --- |
    | `MAR_11_P1` | Guard | The next load comes at dusk. | stiff, formal |
    | `MAR_11_P2` | Caretaker | We'll have the gate open. | calm |
    | `MAR_11_P3` | Guard | …Good. | after a pause |
    Voice them (the existing premade voices — guard "Daniel", worker "Sarah", caretaker "Bill"; 7 v3; Panther); add to the
    spec and `Docs/MARS-LINES.md`. Check every other chapter for mimed talking and list it.

15. **The controls and mechanisms should look like something** (`dan_mars_controls.png`: the control is a glowing ball on a
    flat wall with a bare rod on the ground; a plain box floats in the sky behind). Model every interactive object in Mars as
    a real, readable thing (scripted Blender is fine), ancient and older than the fortifications:
    - **The two temple controls** (spec 5–6): carved stone-and-bronze pedestals, each with a **big lever or turning wheel**,
      each carved with **one kingdom's half of the shared symbol** (so when both are worked together the halves read as the
      temple's joined design — item 9 of the spec), glowing channel grooves running from them toward the bridge.
    - **The ancient mechanism on the overlook:** gears / counterweights / a housing the two channels feed into.
    - **The relay** on the west rim: a crystal or lens node in a carved housing on a pillar, its glow travelling down the
      channel when it's restored.
    - **The displaced conduit** (pier steps): a segmented stone-and-metal pipe with a glowing core, its broken section
      visibly out of line, so lifting it back with telekinesis makes sense at a glance.
    - **Viper's three small machines** and **his pylons / barrier emitters** in the fight: clearly *his* tech (harsher,
      angular, darker metal, red-less — monochrome — but with hard white light), contrasting with the ancient pieces.
    - Every one gives **feedback** when it works (light running along it, sound, movement). And the floating box in the sky —
      no plain boxes (QF17).

16. **The pier conduit (spec 6, after the first control): Dan went there three times and still doesn't understand what's
    going on or what he's accomplishing** (`dan_mars_pier_conduit.png`: "…down the pier walk under the old bridge and push the
    displaced conduit back into place (RT)" — dark rock walls, the camera half inside the rock, nothing that reads as a pier
    walk or a conduit; the bike can't go there). Make it one clear cause-and-effect chain the player can **see**:
    - **Show the problem first:** when the first control is worked, its power visibly runs along the channel (a glowing line
      toward the bridge) and **stops** at a point under the old bridge — a short camera move follows the light to the break
      and shows the conduit section knocked out of line, sparking. Panther (telepathic, proposed): **"The power stops under the
      bridge. Something down there's knocked loose."**
    - **A readable way down:** real carved steps / a walkway along the pier (not dark rock wedges), lit, the camera kept out
      of the rock (item 7 / QF15 cut-away rules), the conduit in view as she goes.
    - **The fix:** she lifts the section back with telekinesis; the light runs through it, along the channel, **up to the
      far control, which lights up** — so the player sees what it accomplished. Then on to the next step.
    - If this stretch still adds nothing after that, **cut it** (the no-filler rule, item 12) and let the power reach the far
      control directly. Say which you chose.
    Apply the same test to every task in spec 6 (relay, conduit, Viper's machines, the swinging span): the player must know
    *why* before doing it and *see* the result after.
    **Also: 7 clips through the ground badly at this spot** (sinking into / passing through the rock floor on foot). Fix the
    ground and her foot / ground collision here, and include on-foot paths in item 10's collision pass: walk every on-foot
    stretch in Mars (pier steps, the overlook, the plaza, the temple) — feet on the surface, never sinking or passing through
    — with a per-frame "feet vs ground" log and 0 frames inside geometry.

17. **Viper's "three small machines" (spec 6) — what are they, and how would she know?** (`dan_mars_viper_machines.png`: 7 with
    her hands up at a tiny grey disc lost in the fog). Make them a proper, introduced beat:
    - **Introduce them with Viper taunting her** — his voice from a loudspeaker horn on a nearby pylon (or his image on one of
      his emitters), then the machines dropping in. Proposed lines (the chat's; Dan to confirm or edit — otherwise use as
      written; Viper = his saved voice):
      | id | who | line | direction |
      | --- | --- | --- | --- |
      | `MAR_06_D1` | Viper | You've been busy. Let's see how you manage several things at once. | over a loudspeaker; cold, amused |
      | `MAR_06_D2` | Worker | Those are his sentries — they'll wreck the control! | alarmed, pointing |
      | `MAR_06_D3` | Panther | Three of them. All heading for the worker. | telepathic, quick |
      | `MAR_06_D4` | 7 | Not if I can help it. | |
      | `MAR_06_D5` | Viper | Every bridge has a weak point. | mid-fight taunt, over the loudspeaker |
      | `MAR_06_D6` | Viper | Enjoy it. Bridges fall. | after the last sentry goes down |
    - **Make the sentries readable:** Viper's tech (item 15 — angular, dark metal, hard white light), big enough to see clearly
      (~1 m), a visible eye / searchlight sweeping, a buzzing hum, flying in a clear line toward the worker's control and
      **attacking it** (sparks) so the threat is obvious; she knocks each aside with telekinesis (they tumble, smoke, go dark —
      machines, nobody hurt). Each one that reaches the control costs time / damages it (a little pressure, fair).
    - **Contrast in the fog:** the shot above is almost all white — make sure gameplay moments in Mars read: the sentries and
      their targets dark against the fog (or lit), and the fog thinned enough around the action that she, the worker, the
      control and the machines are clearly visible.

18. **The "hold" conversation after the sentries makes no sense** (Dan). Spec 6's "The far control is ready." / "Tell them to
    hold." / "They are holding." comes with nothing set up — the player never sees the caretaker at the far control, "them"
    refers to nobody, and Panther (telepathic, only to 7) can't tell anyone to hold. Replace it with this (proposed by the
    chat; Dan to confirm or edit — otherwise use as written), and **show** each beat:
    | id | who | line | direction / what we see |
    | --- | --- | --- | --- |
    | `MAR_06_H1` | Panther | The caretaker's at the far control. They've raised a lamp — they're ready. | telepathic; **cut to the far bank: the caretaker at their control raising a lamp** |
    | `MAR_06_H2` | 7 | The other side is ready. Hold until I say. | to the worker beside her |
    | `MAR_06_H3` | Worker | Holding. | hands on the lever |
    | — | — | *(the power surges — a loose span section swings out of line)* | cut to the span swinging |
    | `MAR_06_H4` | 7 | Not that one! | startled breath (Dan's line) |
    | `MAR_06_H5` | Panther | Specificity. Always helpful. | dry (Dan's line) |
    | — | — | *(she catches the section with telekinesis and holds it)* | |
    | `MAR_06_H6` | 7 | Now! | a short, relieved laugh |
    | — | — | *(the worker and the caretaker pull together — the spans align)* | the wide of the bridge forming (spec) |
    Remove the old `MAR_06` "ready / hold / holding" lines and their voice files; voice the new ones; update the spec and
    `Docs/MARS-LINES.md`.

19. **The Viper fight — Dan's playtest of QF17's version (repeat item: QF17's proofs passed, Dan still found it unclear and
    not fun). Change it as follows, and update the "Boss fights" rule in `CLAUDE.md` to match for every boss:**
    a. **Don't lock her facing Viper.** She's turned toward him the whole time, so walking to the pylons is awkward. Default:
       she moves freely (camera and movement like normal play). Lock-on is **optional** — a button toggles it (e.g. right stick
       click / LB; Tab on keyboard) for strafing round him in close combat; show the toggle on the controls hint.
    b. **Pylons must show they're done.** Today nothing changes and "X: deactivate" stays after deactivating. When she disables
       one: its light dies, sparks and a power-down sound, a visible collapse / panels falling open, its barrier drops; the
       prompt disappears and it can't be used again; a small counter on screen ("Pylons 1 / 3"). Same for every interactive
       objective in every chapter (the Venus devices too): a clear done state, the prompt gone.
    c. **The defeat loop — Dan's design: throw objects with telekinesis to stun him → close in → fight him hand to hand.**
       "Crazy that she can fight but she literally never uses her fighting skills." So:
       - **Telekinesis only stuns** (a throw that hits = he's **stunned** for ~3–4 s: reels, hood drops, dazed sway, a clear
         visual + sound so the player knows *now*).
       - **Then she goes in close and fights**: her real strikes — jab, cross, hook, MMA kick, roundhouse, the finisher (her
         existing Mixamo clips, Chapter 1's fight system) — combos land while he's stunned and **do the real damage**.
       - He recovers and fights back (strike, sweep, spit) — she has to back off, dodge / block, and set up the next stun.
       - Last phase: when his energy is nearly gone, the finisher → the takedown / containment ring (alive), then MAR_07's
         two-shot.
    d. **An energy bar for Viper** on screen during the fight (top centre, "VIPER", drains with her hits, a notch / shake on
       each stun, phase marks on it), and **her own state visible too** (how many hits she can still take — a small bar, or
       her forehead energy point's brightness as Kyle proposed, plus a red-free white flash when hit). Same for the Wormmaster.
    e. **Apply the same loop to the Wormmaster** (Venus): telekinetic stun → close in → strikes, with his energy bar.
    f. Proof: a player-style fight that **uses melee for most of the damage** (log: damage by strikes vs throws, stuns,
       hits taken, length), a capture sequence of stun → close in → combo, and the bars in frame; the idle test still fails
       within ~60 s.

20. **Make it obvious she touches the rune and goes into a vision** (Dan: "it is almost not obvious that she is having a
    vision"). One shared "touch → vision → return" sequence used for **every** vision (the Moon door, Venus, Europa, Mars, and
    later ones):
    - **The reach:** a close insert of **her hand reaching out and touching the rune** (fingertips meeting the surface — hand IK
      onto the shape, measured, no gap or clipping), a breath.
    - **The rune answers:** its glow surges, light runs up from her fingers along her hand and arm, **her forehead energy point
      lights bright**, a rising tone / shimmer.
    - **Into the vision:** a quick push-in to her eyes (steady gaze, as in the creative rules), the light blooms to white, then
      the vision video fades up — the player can't miss the transition.
    - **Back:** the white fades back to her standing with her hand still on the rune, the glow settling, then **the insert of
      her hand leaving the rune** (Mars spec 9 has it; do it for all), her first line.
    Capture the whole sequence for each world.

21. **Make Mars look like Mars.** The background is plain fog and grey ground: add a real Martian landscape around the play
    area in Kyle's misty monochrome — **sand dunes** (rippled, wind-shaped, rolling away into the haze), rocky mesas and buttes,
    scattered boulders, wind-blown dust drifting across the ground and in the air, a pale small sun through the haze. Visible
    behind the settlements, from the ravine, from the temple, and in the window / panorama views (item 3). And **the "Face on
    Mars"** (Cydonia — the famous face-shaped mesa) as an easter egg, **clearly visible from above** when the saucer is parked
    over Mars (the arrival's extreme wide over the plain and the view from orbit / the saucer window at Mars); from the ground
    it's just a far-off mesa. Keep it a natural rock formation (Kyle's style), not a sculpted head.
22. **The star map: Biaveh and the asteroid aren't in our solar system.** Round 15 put them at the outer edge of our system —
    wrong ("Beyond this solar system"; the asteroid is found "after the warp into its system"). Make the star map two levels:
    **our solar system** (Earth's Moon, Venus, Mars, Europa at Jupiter) and **the Biaveh system** (its star, Biaveh, the asteroid
    with its structures), with a zoomed-out view between them showing our Sun and Biaveh's star and the route line joining
    them (once unlocked). The map opens on the system the saucer is in; selecting Biaveh / the asteroid from our system shows
    the route and switches to their system. Travel to them uses the sacred-geometry passage (QF16) as a longer jump, arriving
    in the Biaveh system (where TRV_AST happens). Biaveh's system stays hidden / unknown until MAR_13C unlocks the route.
    Update the console's star map, the travel code and the window views (open space of the right system while there).

Checks + release: captures of each replaced caption's new visual, the window at each world, the perch; publish **0.15.3**;
docs; commit + push.

## QF18 — status (2026-11-20): done, exe-verified off-screen; release 0.15.3 (`game-1503`)
Proof runs (all muted, minimized, Below Normal; captures + `capture_log.txt` in the session scratchpad, numbers below).

**Repeat items — why the last fix didn't hold, and the real cause fixed**
- **Gaps in the ground (6, 10, 13)** — QF17 turned every Mars block into battered masonry (`MBlock`), the 22 m ground blocks
  too: each top stepped in ~0.9 m, so every ground seam was an open groove onto the bright fog (the strip, the ramp tops).
  Ground / decks / floors are now `MGround` (flat-topped terrain). The "hole" by the transport was the transport's vent glow -
  a white slab lying on the ground beside a box; the transport is now a real hover vehicle (hull, cab, crate racks, four
  hover pads), the slab is gone. **Holes check** on every scene at every build: **Mars 0 holes / 25,045 rays** (the build
  fails above 0); Europa 0; Chapter 1: 1 ray at the cell-door threshold (report only); Venus: 9 on the slab bridge, which is
  laid at runtime (report only - its rays run before it exists).
- **Through walls on land (10)** — QF15 swept only the swimming bike; on land only the zones kept bike / 7 in, and the west
  ramp ended in the bank's end wall with a 0.6 m corner of zone overlap behind it. Ramps now run out onto landings open to the
  floor; **land collision** (`RideProbe.SlideLand`) for the bike and on foot, every world. Full Mars run: **bike on land
  55,550 frames, inside geometry 0, crossed a surface 0**; riding camera inside geometry 0. **`-landwalltest`: 19 attempts
  (14 at full speed into walls / props, 5 on foot), failed 0.** Grass never collides (Venus's
  grass chunks held her - caught by the player-style run, fixed).
- **7 sinking at the pier walk (16)** — the pier stairs were a slope *inside* the solid bank block; and (found this round)
  the new walk under the span head stacked two zone heights, so crossing the span head dropped her to the walk. Stairs + walk
  rebuilt outside the bank wall, north of the span (zones never stack); low plinths (controls, relay, mechanism, pylons, the
  tower) are ground obstacles. `FEET vs GROUND (QF18 16)` logs every on-foot frame.
- **The boss fight (19)** — QF17's proofs counted attacks and answers, but the loop never used her fighting and locked her
  facing him. New loop for every boss (see 19).

**Items**
1. Rule "Show, don't caption" in `CLAUDE.md` (with "objectives come from the story", "no filler tasks", "characters shown
   talking have lines", "every dialogue shot shows the speaker", done states, readable text).
2. Captions replaced — **all four found**: Mars MAR_01 (×2), MAR_13C, Europa EUR_01 → `HoloDisplay`: a glowing hologram built
   from the **real level** (`Resources/Holo/Holo_<world>`, copied at build time): Mars with the temple marked, then zoomed to
   the bridges with the barricades / broken spans marked; MAR_13C the two records side by side, intervals lining up, the route
   completing to Biaveh; Europa the ice field, the passage and the temple coordinates marked. Swept every director / aboard
   script: the other `UI.Hint`s are gameplay hints, not descriptions (kept).
3. Window views = the **real level** copied under the ship (`Resources/Outside/Outside_<world>`: Mars 545 renderers, Venus 324,
   Europa 238), lighter haze; the Moon keeps its surface + Earth backdrop. `-qf16check`: journeys 4, window captures each.
4. Perch: on TOP of her left shoulder (between the clavicle and the arm joint, raised to the skin), along the shoulder line,
   looking out with her (scale 0.6, sitting); captures `qf18_04_perch_front/_side`. Note: at the old spot by her neck he
   vanished inside her pigtail - he now sits at the outer shoulder; from the front he is still partly behind the pigtail.
5. Objective box, hints, marker distance, subtitles on a soft dark rounded panel + outline (`ChapterUI`).
6. See "gaps" above + the transport; the Reptilians' **A-pose arms**: their avatars took Tripo's A-pose as the T-pose
   reference (arms 55° off) - enforced a true T-pose on the bodies and every clip copy (Viper too). **NPC placement scan:
   Mars 30 people, 0 over a hole / inside geometry** (every scene: 0).
7. **Every dialogue shot shows the speaker** (`DialogueFraming`, all chapters): full Mars run **70 lines checked, 17
   reframed, 0 still blocked**; Venus encounter 1 reframed (VEN_C06_03); the worker's filters line now frames the worker by
   the transport.
8. Viper close-ups for each of his lines (low angle, lit from beneath, eye glow, haze thinned / dimmed behind him), her
   replies over her shoulder looking up; the guard steps aside first. Same in MAR_07.
9. MAR_03_11–15 voiced (worker = Sarah, 7 v3, Panther), the shot of the route entrance (the old ramp, a half-buried
   gateway), then the objective. Objective audit (Mars): the route, the drawbridge ("one kingdom raised and chained the span"),
   the gate's cut line, the dark control → relay, the power stopping under the bridge, the sentries - each follows a line or
   a shot now.
10. Ramps + landings + land collision (above); route ridden end to end (full Mars run, all scenes complete); `-landwalltest`.
11. **Drawbridge** (raised and chained by one kingdom, its barred order plaque; TK breaks the lock, the drum runs, the span
    swings down with dust and a boom) instead of Venus's slab; a real channel in the ravine floor; the arcade instead of the
    "table". **Coplanar check + fix on every scene** (any mesh; QF9 only saw Chapter 1's cubes): Mars 1,384 → 0, Venus 936 → 0,
    Europa 256 → 0, Chapter 1 780 → 0; the lowered drawbridge (runtime probe) 0. Saucer interior: 721 left (the fridge panels
    moved off their shared planes; what remains is under / inside props - report only).
12. East ramp = west; the **service gate**: its power line was cut - the camera follows it to the severed ends; she holds RT
    to pull them together (sparks), the light runs up to the gate, it grinds open, the transport comes through and a driver
    raises a hand. "Press X" audit: Mars relay → reset its knocked crystal (reason: the dark control's channel; payoff: the
    light runs); Europa holding door / lift power / schematic / devices and Venus devices kept (each has a spoken reason and a
    visible payoff); Venus devices got a done counter.
13. Holes check (above).
14. MAR_06_V1–V6 and MAR_11_P1–P3 voiced (guard Daniel, worker Sarah, caretaker Bill). Other chapters: no gesture-only
    talking found.
15. Real controls (carved pedestal, bronze wheel, one kingdom's half-symbol each), the mechanism (two great gears, weights),
    the relay (lens crystal in a ribbed housing on a pillar), the conduit (banded pipe, glowing core), Viper's angular pylons
    (hinged panels, hard white head), sentries (angular drones, eye + searchlight), his loudspeaker mast. Feedback = light
    running along `PowerLine`s.
16. One visible chain: dark control → its channel to the relay → the crystal reset → the light runs and stops under the bridge
    (MAR_06_P1) → the steps → the section lifted → the light runs to the control, which lights, the worker waves.
17. Sentries introduced by Viper over the loudspeaker (MAR_06_D1–D6), ~1 m, searchlights, attack the control (sparks, a
    "failing" warning), knocked aside (tumble, smoke, dark); counter 0/3 → 3/3; fog thinned round the action.
18. The hold: MAR_06_H1–H6 (H4–H6 keep Dan's takes), the caretaker raising a lamp at the far control, the worker on the wheel,
    the span section swinging out, caught with TK; old ready / hold / holding lines and files removed.
19. Boss loop (Viper + Wormmaster): free movement, optional lock-on (R3 / Tab); TK only **stuns** (3.2 s), her full chain
    (jab, cross, hook, MMA kick, roundhouse) does the damage; he recovers and counters; energy bar (name, phase marks, notch +
    shake on a stun), her bar + forehead point dimming + white hit flash; pylon done state (light dies, panels fall open,
    prompt gone) + "Pylons n / 3" (the third pylon optional - it stops the sweep). **Player-style first-time runs: Viper 2:53,
    melee 91 % of the damage, 6 stuns; Wormmaster 3:08, melee 89 %, 7 stuns. Idle tests: Viper down after 29.4 s, Wormmaster
    22.7 s.**
20. `VisionTouch` for all four visions (Moon, Venus, Europa, Mars): the reach insert (fingertips 4.0 cm from the rune on Mars),
    the glow surge + light up her arm + the forehead point, the push-in to her eyes, the bloom to white; back with her hand
    still on it, the hand leaving (Venus keeps Dan's open-palm match cut as its return; Europa reaches from the bike).
21. Dune sea, plain dunes, 16 mesas / buttes, boulders, a pale sun, drifting dust (`MarsDust`), the **Cydonia mesa**
    (`MarsLayout.FaceAt`) - a face from above (the arrival's new high opening shot, the window view), a mesa from the ground.
22. Two-level star map (our system / Biaveh's system / the overview with the route between, gateways once unlocked), opens
    on the ship's system (`SaucerLocation.System`); the windows show Biaveh's star in its system.

Not done / known: `FEET vs GROUND` still logs foot overlaps on the pier stairs' step edges (worst 43 cm on one step, body
inside geometry 0) - the stairs are steps, the zone is a slope; the Saucer interior's remaining coplanar faces (report only); Chapter 1 cell-door ray and Venus's
runtime slab bridge in the holes report; check-run autopilot on foot still needed skips at two spots (route geometry is
fine for a player - the player-style Viper run had none).

## For Dan to check (QF18)
1. Mars arrival aboard: the hologram over the plain instead of the caption; the bridges zoom; Panther sitting on her shoulder.
2. The ground: no glowing strips / holes anywhere (checkpoint, ramp tops); the transport is a vehicle; the Reptilians' arms.
3. The west / east ramps at full speed: landings, no walls, no clipping; try riding into walls and props anywhere.
4. MAR_02 worker's line shows the worker; Viper's reveal: close-ups of Viper, readable against the haze.
5. The worker's route lines + the shot of the old ramp before the objective.
6. The drawbridge; the gate's cut line (hold RT) and the transport coming through.
7. Section 6: the dark control → relay → power stopping under the bridge → the steps → the conduit → the control lights;
   the sentries; the hold with the caretaker's lamp.
8. Viper's fight: stun with debris, then fight him hand to hand; the bars; the pylons going dark; lock-on (R3 / Tab).
9. The vision at the rune (and the other worlds'): obvious now?
10. Mars's look (dunes, mesas, dust, the face from above), the window views, the two-level star map.


## Quick fix QF17: real boss fights + Mars clean-up (Dan, 2026-11-20) — done, status below; release 0.15.2
Commit first. All standing rules + the repeat-item rule (why the last fix didn't hold, fix the cause, prove it in normal
play, never "done" on a partial fix). Release **0.15.2**; "QF17 — status"; "For Dan to check (QF17)". The proofs below may
take testing time past 20 minutes — keep it purposeful.

**Dan (2026-11-20): "So far every boss 'fight' is me pushing buttons and the boss just standing there."** The Venus
encounter (QF12 / Round 13) and the Mars encounter (Round 15: lanes, barriers, a moving section, three pylons, then a
takedown) were obstacle courses; neither boss ever threatened her. That stops now.

0. **New standing rule — add "Boss fights" to `CLAUDE.md`** (applies to every boss, now and later):
   - The boss **moves and actively attacks her** — several distinct, **telegraphed** attacks (a clear wind-up: pose, sound,
     light) she must read and answer with her own moves (**dodge, block, jump, telekinesis, her strikes**).
   - **She can lose:** hits knock her back and cost her; enough hits = she goes down → retry from the start of the current
     phase (fair, quick). Still nonlethal for everyone.
   - **Phases (2–3)** that escalate: faster, a new attack, the arena changes. Objectives (switches, pylons) are allowed only
     *while* the boss is attacking — never as the whole fight.
   - The boss is defeated by her **countering him** (stagger him — e.g. a dodge or block into a telekinetic throw of
     debris / his own machinery back at him — then press the advantage), not by flipping switches while he waits.
   - **Proof every time:** (a) a player-style run with a log of boss attacks per minute, hits she took, staggers, phase
     times; (b) an **"idle test"**: a run where the player does nothing must be knocked down within ~60 s (proves the
     threat is real); (c) a capture sequence of the boss mid-attack; (d) a first-time-player length of about 3–5 minutes.
   - Tune from one data table (attack timings, damage, telegraph lengths) so Dan's feedback is a number change.
1. **Rebuild Viper's fight on the island plaza** (spec 7; keep the positioning idea, but he's the threat). Proposal:
   - **Snake strike:** he coils, the hood flares, a hiss — then lunges 6–8 m very fast; dodge sideways (or block, pushed back).
   - **Venom spit:** arcing globs (dark, glossy) that leave burning puddles for a few seconds — zone denial.
   - **Tail / arm sweep** when she's close — jump or block.
   - **His defensive machinery:** barriers slam up to box her in; a rotating energy sweep across a lane she must jump or time.
   - **Phase 1:** he fights from his raised platform (spits, works the machinery); she disables a pylon under fire to bring
     the platform down. **Phase 2:** he comes down and hunts her — strikes and sweeps, the machinery still active; she staggers
     him by dodging a strike and hurling debris / a barrier section with telekinesis. **Phase 3:** enraged — faster combos,
     strike → spit; three staggers and she pins him — the containment ring, alive, left for local custody; then MAR_07's
     two-shot as now.
   - Uses Viper's rig + the reused clips (`viper_*` / tunneler / YBot list) plus procedural coil / lunge / spit motions.
2. **Bring the Venus Wormmaster fight up to the same rule.** He leaves his dais and fights: **lashing attacks with his long
   boneless arms** (the curl bones), a **ground slam that sends a wave of surfacing worms** along a line at her, and he
   **directs worm lunges** as now; phases as above; she counters (dodge a lash → telekinetic throw of root debris / a device
   casing at him → stagger); the three control devices still have to go off, but **under attack**. The worms are still
   victims — stunned and withdrawing, never harmed; the Wormmaster subdued, not killed. Same proofs (idle test included).
3. **Mars buildings are all cubes — make it real architecture.** The two fortress cities, the settlement, the checkpoint,
   the market, the lookouts and the temple: a modular **Reptilian architecture kit** (scripted Blender is fine) — thick
   sloped / battered walls, stepped terraces, arched gates and doorways, towers with domed or pointed roofs, windows,
   balconies, parapets, awnings over the market, banners and walkways; the temple in a different, older, smoother monumental
   style (it predates the fortifications). Kyle's look: `Art/Reference/kyle_world_mood.jpg`, `kyle_city_vehicles.jpg`,
   `kyle_command_center.jpg` (misty, glowing, huge forms fading into haze). **No plain boxes left in view anywhere in Mars.**
   Captures from the player's camera in the settlement, at the checkpoint, the ravine views of both cities, the temple.
4. **Ramps disappear when you drive over them** — almost certainly QF15's cut-away (it hides geometry between the camera and
   the bike) catching the ramp she's on. Fix the rule for every world: **never cut away anything she can ride or walk on, or
   anything at / below the bike** — only walls, roofs and overhead pieces above her. Check every ramp in Mars (and Venus /
   Europa) with a capture while on it.
5. **The bike was already outside the hold when she arrived on Mars** (repeat of the bike-hold rule). Find which start
   path places it outside (the chapter start from the star map / VEN_C13 / MAR_13U, the main menu chapter list, Scene Select
   "arrival", Continue) — on **every** path the bike starts inside the hold, the hatch opens, the bike lowers out, then 7
   comes down the beam. Add a check that fails if the bike is outside the hold before the hatch opens, on every world.
6. **Mars is clunky — a clean-up pass.** Play Mars yourself, player-style, at real speed from the arrival to MAR_13, watch
   the captures, and write a **"clunk list"** in the status: every spot that feels awkward (stalls, fades where a cut or
   continuous play would be better, unclear objectives, getting stuck, characters popping / T-posing / sliding, the camera,
   too-long waits, invisible walls, dead ends). Fix everything on it you can; say what's left.

Checks + release: the boss-fight proofs for Viper and the Wormmaster (player run log, idle test, capture sequence, length),
the Mars captures (architecture, ramps, the bike out of the hold on each start path), the clunk list. Publish **0.15.2**;
docs (the new "Boss fights" rule in `CLAUDE.md`); commit + push.


## QF17 — status (2026-11-20): done, exe-verified off-screen; release 0.15.2 (`game-1502`)
Proofs + captures: `Docs/Previews/qf17/` (attack sheets `viper_attacks_sheet.png`, `wormmaster_attacks_sheet.png`,
`architecture_sheet.png`, folders `viper_attacks/`, `wormmaster_attacks/`, `architecture/`, `ramps/`, `bike_hold/`, the run
logs in `logs/`). Monochrome check over all 113 QF17 captures: max |R−G|,|G−B| = 0. Audio peak 0 in every run (muted). Dan was
not around: every call below was made without asking (noted where it matters).

0. **"Boss fights" rule** — in `CLAUDE.md` (its own section), with the proof list and the one data table
   `Unity/Assets/Resources/Bosses/boss_tuning.json`. One shared system: `BossFight.cs` (attacks + white ground markers that
   brighten and lock before the hit, a white tell light on him, his wind-up pose + a sound; hits knock her back; her hits
   from the table → down → fade → retry from the start of the phase; a wasted committed attack leaves him open → a
   telekinetic throw of debris (the pieces glow when he is open) or three strikes staggers him; phases; the takedown;
   after 3 knockdowns in one part a skip of that part is offered - Y / Enter), `BossPilot.cs` (how a player plays it),
   `ViperFight.cs`, `WormmasterFight.cs`. 7's side (`ChapterSevenFight.cs`): jump A (a hop over sweeps / waves / rings),
   telekinesis RT (the throw), X on an objective uses it (pylons / devices take 3 s at them - a hit or a dodge interrupts),
   LB runs in a boss arena, her hits from the table.
1. **Viper's fight — rebuilt** on the island plaza (now one open arena; his platform rises out of it in front of the
   temple steps). Phase 1 "from his platform": venom spit (3 dark arcing globs onto marked spots → burning puddles for 3 s),
   the energy sweep from his platform (a white wall of light turning across the plaza - jump it), barriers that slam up
   round her (then a spit into the cage); she shuts his two pylons down (X, 3 s) under fire → the platform sinks, he drops
   down. Phase 2 "he hunts her": the snake strike (he coils, the hood flares, a hiss, then a 7.5 m lunge - dodge sideways
   or block), the tail / arm sweep when she is close (jump or block), spit, the third pylon's sweep. Phase 3 "enraged":
   faster, the sweep → strike combo. Only the strike and the sweep leave him open (his spit and his machinery do not);
   4 + 5 staggers; dazed → takedown (Y / E) pins him; the containment ring; MAR_07's two-shot as before. Clips: his rig +
   the reused YBot fight idle / punch / hit / dazed / knocked down / get up and the tunneler's roar / swipe / walk, with the
   coil / strike / spit procedural on his spine, neck and head and the hood flare.
2. **The Wormmaster — to the same rule.** Phase 1 "above his machinery": from the dais he whips one long boneless arm
   along a marked line (lash: dodge), sends a creature (it surfaces and runs a marked line at her: jump or step aside) and
   slams the dais so a wave of surfacing humps rolls at her (jump); she shuts the north device (X, 3 s) and the ledge
   device (RT from below - a call: the old climb is not possible in fight stance) down under attack. Phase 2 "he comes
   down": he leaves the dais and fights - arm sweep when she is close (jump / block), lash, slam wave, creatures; the
   crystal device (RT) + 3 staggers (root-debris throws). Phase 3 "the signal fails": faster, combos sweep → slam, lash →
   shock ring (jump); 4 staggers → dazed → takedown → he drags himself back onto his dais → the console closes the barrier
   (as before). The creatures are victims: only the ones he sends reach her (touching a surfaced one does nothing - the
   first tuning made phase 1 a lottery); they withdraw alive.
   **Proofs (both bosses)** — testing time ~2 h this round (allowed by Dan for QF17; each run on purpose):

   | | Viper | Wormmaster |
   |---|---|---|
   | (a) player-style run (virtual pad), log | 1:40 / 2:06 / 2:17 in the three full-chapter runs (13-15 attacks/min, hits 4-9, down 0-1×, staggers 7-9) | 1:48, 20 attacks (11.1/min), hits 4, down 0×, jumped 13, throws 9 (4 swatted aside), staggers 7 |
   | (a) first-time-player pilot (`-bossnovice`: reads 66 %, takes 60 % of his openings) | final table: 3:19 (50 attacks, 15.1/min, hits 20, down 2×, dodged 13, jumped 7, throws 8, staggers 8) and 2:12 (32 attacks, hits 10, down 1×); earlier tables 2:37 / 5:31 / 7:04 | 3:35, 51 attacks (14.3/min), hits 15, down 2×, dodged 8, jumped 26, throws 17 (8 swatted), staggers 9 |
   | (b) idle test `-bossidle` (nobody presses anything) | knocked down after 29.4 s (8 attacks, 7 hits) - PASS | knocked down after 23.4 s (6 attacks, 6 hits) - PASS |
   | (c) captures mid-attack (player camera + side angle) | `viper_attacks/` (spit, energy sweep, cage, strike, tail sweep, staggers, pinned) | `wormmaster_attacks/` (sent creature, slam wave, lash, arm sweep, shock ring, staggers, he comes down, subdued) |
   | (d) first-time length (target 3-5 min) | 2:12 / 3:19 (final table; a first-time pilot's length swings with its luck) | 3:35 |

   In the story (the full Mars run from the Venus debrief, player-style, real speed) Viper's fight took 1:40-2:17 (expert pilot).
   Tuning on the way (all table numbers): the first table let every wasted spit open him (54 s with the expert pilot), the
   second killed the first-time pilot ten times in phase 2 (10 min), then: her hits 5 → 6 (7 against Viper), the strike costs 1 not 2, fewer
   spits, no strike → spit combo, longer gaps in phases 2-3, only committed attacks open him, objectives take 3 s.
3. **Mars architecture — no plain boxes left.** `Editor/FightDemoBuilderMarsArch.cs`, a Reptilian kit in code (no Blender
   round trip): dressed masonry for every block (battered sides, plinth, chamfered coping - `MBox` / `MBlock`; every
   `Block` / `AddBox` in the Mars builder replaced), towers (battered polygon bodies, moulding, crenellated parapets, domes
   or pointed roofs, window bands), arched gateways (the two checkpoints over the bridge heads, the fortress gatehouses),
   arched doors and windows (dark, some lit white), stepped terraced houses with parapets, balconies, awnings, banners,
   domes or corner towers, market stalls with sloped cloth awnings on turned posts, jars and sacks, turned lanterns, the
   fortress cities (curtain walls with 18 towers, gatehouse toward the ravine, terraced quarters, a stepped citadel with
   great domes and a spire), the temple in an older smoother style (rounded battered walls, a great round-arched doorway,
   a stepped rounded roof with a crown, columns with entasis). Houses are one object each (the camera can cut away just
   the one in its way). Captures from the player's camera: `architecture/` (main street, market, checkpoint, the ravine,
   both rims with the cities in the haze, the extreme wide on arrival, the temple inside).
4. **Ramps vanishing — fixed (repeat).** *Why the last fix didn't hold:* QF15's riding camera sorted meshes by name only -
   "shells" (Ceiling / Floor / Wall / Tunnel …) ease the camera in, *everything else* between the camera and the bike is cut
   away. Ramps, stairs, decks, spans, slabs were never in the rule - and riding **down** a ramp the camera (behind = uphill)
   sat *under* the ramp's surface, so the ramp itself was "in the way" and was cut away. *Now:* no camera ever cuts away
   anything she rides or walks on or anything at / under her (`CameraOccluders.MayCutAway` / `IsWalkableName`: the riding
   camera and the Europa base cut-away), and the riding camera rises above the slope behind her (the line to her clears
   the ground at four points along it, eased - never a step). Proof `-rampcheck`: Mars 2 ramps (6,004 frames) and Venus 3
   ramps (610), ridden up and down: **cut-away under her / walkable 0**; Mars ramp run: aim / position shifts 0, camera
   inside geometry 0. Captures `ramps/`.
5. **Bike outside the hold on Mars — fixed (repeat).** *Why the last fix didn't hold:* QF13 put the hold into each world's
   *arrival cutscene* only. The star map does "Mars has progress ? Continue : start fresh", and Dan had Mars progress from
   0.15.0 - so 0.15.1's star map **continued his save at checkpoint 1**, which restored her on the bike on the plain (no
   hatch, no beam). Continue and Scene Select "the crossing" did the same. *Now:* every start at Mars's landing site plays
   the landing's tail (`Relanding`: the saucer hovering, the hatch opens, the bike lowers out, 7 comes down the beam), and
   the arrival itself is unchanged. **Check on every world:** `BikeHoldWatch` - from the world's first frame to the hatch
   opening, is the bike drawn outside the hold? Logged in every run that plays an arrival. PASS on: Mars from the star map
   (fresh + via the Venus debrief, 3 runs), Mars resumed at the landing site (2 runs), Venus arrival, Europa arrival - 0
   frames each. Captures `bike_hold/`. (A call: Venus / Europa resumes after their own hatch scenes start beside the bike
   where she left it - those scenes are their own Scene Select entries.)
6. **Mars clean-up — the clunk list** (the full chapter played player-style at real speed three times, from the Venus
   debrief's star map through the arrival, the fight and MAR_12 / MAR_13 aboard: 1/1, 0 soft-locks, 0 skips, ~11.5
   game-min each; captures + the riding-camera log). Fixed:
   - the encounter was an obstacle course with Viper standing still → the fight (1);
   - every building a box → the kit (3); the lane walls' dark "footing" blobs on the plaza looked like holes → gone;
   - ramps vanishing under the bike (4); the riding camera under the ramp surface / inside it (306 frames in the first
     run) → lifted above the slope;
   - the bike outside the hold on resume (5);
   - two market stalls stood in the middle of the road (the bike rode through them) and a market house sat half on the
     road (rode through its corner, the camera clipped it) → moved off the road, stalls solid;
   - the kit's meshes were named "…_Walls", so the riding camera treated every house as a tunnel wall and pulled in tight
     behind her (a low close-up of her back at the checkpoint and on the ravine floor) → renamed;
   - `m09` framed Viper from below (his crotch) → over her shoulder up to his face; Viper's intro two-shot was taken from
     19 m apart and showed only a wall → she walks up to him first; the record insert `m37` was framed at her hip → in
     her hand at chest height (non-sexual framing rule);
   - the barrier cage was an opaque white cylinder hiding her → low faint rings; the energy sweep had no visible beam and
     the wind-up no light → a white wall of light, a white tell light;
   - the follow camera in the fight now eases toward him when he leaves the middle of the view.
   **Left (not fixed this round):** the riding camera still has a few small moves where the two maintenance ramps meet
   the ravine floor (5 aim / 6 position shifts, ~11 in/out reversals per full run, all at the two ramp feet; the camera
   is behind the ramp's low parapet ~1,000 frames there) and 60-150 frames inside geometry at the east ramp foot;
   on foot the plaza's pylons sometimes stand between the follow camera and her in phase 1; Viper's wind-up for the
   strike uses the tunneler's roar (arms spread) - reads, but a real coil clip would read better; the chapter is short
   for a first play (~8 min of Mars play at the pilot's pace); the getting on / off the bike for every on-foot step (13
   X presses) is a lot of stop-and-go.

## For Dan to check (QF17)
- **Viper** (Scene Select "Mars: Viper's intervention"): he raises his platform and attacks at once - spit (marked spots,
  burning puddles), the energy sweep (jump A), the barrier cage; shut the two pylons (X, hold your ground 3 s). He comes
  down and hunts you: dodge B / block LT the strike, jump the sweep; when he has wasted one, the debris glows - RT throws
  it and staggers him; strike while he reels. Enraged: faster. Takedown Y / E when he is dazed. Try standing still: he
  should put you down in well under a minute.
- **The Wormmaster** (Scene Select "Venus: the way down + the control chamber"): the same rules; the north device X, the
  ledge device RT from below, then he comes down; the crystal RT; then the console as before.
- Numbers you want changed (her hits, his speed, wind-up lengths, openings, staggers per phase) are all in
  `Unity/Assets/Resources/Bosses/boss_tuning.json` - tell me the change.
- **Mars**: ride through the settlement, the checkpoint, the ravine and the rims - real buildings everywhere, the two
  fortress cities in the haze, the temple older and smoother. Ride down both maintenance ramps: they never vanish.
- **The bike hold**: from the star map with an existing Mars save, and Continue: the hatch opens, the bike lowers out, then
  7 comes down the beam.


## Side task S2: Reptilian ready for Mixamo (Dan, 2026-11-19) — DONE; Dan's rigged body in: `Art/Mixamo/reptilian/reptilian_body_mixamo.fbx`
**Status:** `Tools/Blender/cleanup_mixamo_body.py -- <repo> reptilian` (the S1 script, now shared, presets viper /
reptilian; ~3 min) → `Art/Export/reptilian/reptilian_for_mixamo.fbx` (4.1 MB, 35,944 tris, 1.90 m, feet at 0, faces
front, arms 57° below horizontal, 201 loose pieces welded into 1, grayscale 2048 texture embedded) +
`reptilian_mixamo_metrics.txt`; previews `Docs/Previews/reptilian/` (sheet, q34, head, `hipL_*` close-ups).
**Satchel + sash tail: not fused, nothing to separate** (measured, logged by `hip_check`): on this model the satchel
hangs at his *right* hip and the knot + tail at his *left*. Below the tunic hem the tail is its own piece, 43 mm in
front of the left thigh; the satchel's bottom (0.91 m) rests on the tunic skirt, 40 mm from the right trouser leg.
(Still give the tail / satchel their own short bones in Unity, as `MODELS-NEEDED.md` says.) **Dan:** upload the FBX to
Mixamo's auto-rigger, download "FBX for Unity, with skin" to `Art/Mixamo/reptilian/reptilian_body_mixamo.fbx`.

Same as S1 (Viper, `cleanup_viper.py`, now `cleanup_mixamo_body.py`) for Dan's Tripo model `Art/Source/reptilian/reptilian_v1.glb` (~57 MB;
reference `Art/Reference/reptilian_ref_front.png`, notes in `Docs/MODELS-NEEDED.md` "Reptilians"): weld, decimate to
~30–40 k tris keeping the face, hands, sash, satchel and the wrapped forearms readable, A-pose upright, **~1.9 m**, origin
at the feet, texture embedded → **`Art/Export/reptilian/reptilian_for_mixamo.fbx`** + a preview sheet (front / side / back)
`Docs/Previews/reptilian/reptilian_for_mixamo_sheet.png`. Check the satchel and the sash's hanging tail aren't fused to
the left thigh (separate them by a small gap if they are) so Mixamo's skinning doesn't glue them to the leg. Then tell
Dan: upload to Mixamo's auto-rigger, download "FBX for Unity, with skin" to `Art/Mixamo/reptilian/reptilian_body_mixamo.fbx`.
Commit + push. No release.

## Side task S1: Viper ready for Mixamo (Dan, 2026-11-19) — DONE; Dan's rigged body in: `Art/Mixamo/viper/viper_body_mixamo.fbx`
**Status:** `Tools/Blender/cleanup_viper.py` (now `cleanup_mixamo_body.py … viper`, ~3 min) → `Art/Export/viper/viper_for_mixamo.fbx` (3.4 MB, 35,996 tris,
2.30 m, feet at 0, faces front (-Y Blender = +Z Mixamo), arms 55° below horizontal, 121 loose pieces welded into 1,
grayscale 2048 texture embedded) + `viper_mixamo_metrics.txt`; previews `Docs/Previews/viper/` (sheet front / side /
back, q34, head close-up). **Dan:** upload the FBX to Mixamo's auto-rigger, download "FBX for Unity, with skin" to
`Art/Mixamo/viper/viper_body_mixamo.fbx`.

Dan's Tripo model `Art/Source/viper/viper_v1.glb` (56 MB, ~2 M tris; reference `Art/Reference/viper_ref_front.png`,
Kyle's design `kyle_viper.jpg`, notes in `Docs/MODELS-NEEDED.md` "Viper") — Mixamo won't take it (GLB, too heavy).
Like the guardian's `elarim_body_for_mixamo.fbx`: weld the loose pieces, decimate to ~30–40 k tris keeping the hood,
fangs, claws and the belly scales readable, A-pose facing +Z / upright, real-world scale (~2.3 m), origin at the feet,
texture embedded → **`Art/Export/viper/viper_for_mixamo.fbx`**, plus a preview sheet (front / side / back)
`Docs/Previews/viper/viper_for_mixamo_sheet.png`. Then tell Dan: upload that file to Mixamo's auto-rigger, download
"FBX for Unity, with skin" to `Art/Mixamo/viper/viper_body_mixamo.fbx`. Commit + push. No release.


## Quick fix QF15: the bike camera + walls in Europa (Dan, 2026-11-19) — done, status below; release 0.14.3
Commit first. All standing rules and the repeat-item rule (say why the last fix didn't hold, fix the cause, prove it in
normal play, never "done" on a partial fix). Dan: **"Two main fixes for Europa now."** Items 1–2 are the focus — do them
properly; item 3 is a small cut. Release **0.14.3**; "QF15 — status"; "For Dan to check (QF15)". Testing time may go over 20 minutes for the
proofs below, but keep it purposeful.

1. **The camera keeps shifting while riding the bike — it must be smooth, always.** QF14 smoothed it only in the race
   (`ChapterCamera.RaceMode`); everywhere else under water (and on Venus) it still swings, pulls in, cuts or re-aims on its
   own. While she rides — any section, any world, outside cutscenes — use **one** bike follow camera: behind and a little
   above the bike, critically damped (no overshoot), following her heading and pitch smoothly; **no automatic swings,
   cuts, resets or "tight spot" snaps**; when geometry is between camera and bike, **fade / cut it away** (as in the base)
   rather than jumping the camera in; if it must come closer, ease in and out slowly with a dead band so it never pumps.
   Look-toward-the-next-ring stays in the race only, as a slow ease. Find and remove every other system that moves the
   camera during riding (guide marker framing, speed FOV, dialogue framing, collision probes) or route them through this
   one with smoothing. **Proof:** a per-frame log during free riding (settlement, descent, approach, race, the way home)
   and on Venus: camera position / aim change per frame vs the bike's — count "shifts" (aim jumps > 3° or position jumps
   > 0.3 m in a frame not caused by the bike's own motion, and in/out reversals of distance within 0.5 s): **0**. Plus a
   capture sequence of a minute of riding.
2. **The bike can still go through walls under water.** QF13's sweep skipped every mesh larger than 60 m (the chamber
   shell, the temple hall, the escape tube, big ice / rock pieces) and relied on swim volumes, which don't match the walls.
   Make collision complete: **every** solid surface in Europa (ice ceiling, chamber walls, structures, pillars, arches,
   the settlement, the base's outside, the tunnels, the temple) stops the bike — she slides along it, never through —
   at the new top speed (25 m/s: sweep the whole frame's movement, no tunnelling). The camera never goes through walls
   either (and never shows the inside of geometry). **Proof:** an automated wall test — ride at full speed straight into
   walls and along them from many angles in every area (at least 150 attempts spread over the chapter, including the big
   shells and the race course) and the free-riding routes: **0 frames with the bike or camera inside geometry**; list
   any spot that failed and was fixed. Also a few captures riding hard along a wall.

3. **Small, from Dan (2026-11-19): cut the reunion at the end of Europa entirely.** After 7 boards in the beam, go
   **straight to the debrief with them already sitting in the pilot's chair** (Panther in her lap) — no helmet-off beat at
   the boarding area, no Panther waiting there, no kneel, no head-against-hand, no "Good." (`EUR_16` is removed; Dan: it
   feels like a lock-up). A short fade from the beam-up into the chair shot is fine. Spec + `Docs/EUROPA-LINES.md` + the
   dialogue data updated; drop `EUR_16_04`'s voice file. Her helmet is simply off in the chair.

Checks + release: the two proofs above, one player-style ride through Europa from the dive to the base and from the
escape to the temple, publish **0.14.3**; docs; commit + push.

## QF15 — status (2026-11-19): done, exe-verified off-screen; release 0.14.3 (`game-1403`)
Captures + logs: `Docs/Previews/qf15/` (the minute of riding `ride_sequence/`, the wall slides `wall_test/`, contact sheets,
the per-frame logs `ride_camera_frames_*.csv`, the wall test and run logs).

1. **The riding camera — fixed (repeat of QF14 5).** *Why QF14's fix didn't hold:* it only switched the follow camera's wall
   logic off inside the race (`RaceMode`). Everywhere else the **on-foot** follow camera still drove the bike: it snapped in
   at once for any mesh on the boom, its "is 7 visible" check looked for her bones on the *bike's* transform (none - it used
   fixed points 1-1.6 m above the bike, often inside the hull or a ceiling) and pulled in / swung / **cut** round whenever
   those points were "hidden", its recentre stopped while that check said "occluded" and then re-aimed, its pitch never
   followed the bike's nose, and the speed FOV pushed in and out by itself. *Now* there is **one riding camera**
   (`ChapterCameraRide.cs`) whenever she rides, in any world, outside cutscenes - none of the on-foot logic runs:
   behind and a little above the bike, its heading and nose pitch followed through a two-stage first-order lag (cannot
   overshoot), turns capped at 140°/s, never below her level; anything in the way that is not a shell around her (pillars,
   arches, rings, structures, platforms, rocks, vehicles) is **cut away** (held 0.8 s) instead of moving the camera; only
   shells (ice ceiling, floors, tunnel / tube walls, the temple hall) bring it closer - seen early by a wide probe, a
   clearance probe and a look-ahead 0.6 s along her path, eased in over 0.5 s, eased out only after 1.2 s fully clear, 0.5 m
   dead band; a last guard keeps the camera itself out of geometry. The race's look-toward-the-next-ring is a slow ease
   (its weight moves at most 0.5/s). Speed FOV: +6° over 1.2 s. A cutscene that ends far round from her heading or with its
   camera behind a wall, and a skip that moves the bike, are **cuts** (counted separately), never a fast swing.
   **Proof** (`ride_camera_frames.csv` every riding frame; a shift = aim change > 3° or offset change > 0.3 m per 60 fps frame
   beyond what the bike's own turn / pitch / sideways travel explains, or an in-out-in of the distance within 0.5 s):
   the player-style Europa run from the descent to the debrief (`-europaplay -europacp 1`, settlement, approach, the base's outside, the escape tube, open water, the temple, the way home): **13,660 riding frames - aim shifts 0, position shifts 0, distance reversals 0, camera inside geometry 0, behind a surface 0**; the race run (`-europacp 2 -europarace`, two races): **10,882 frames - 0 / 0 / 0**, race 3,678 frames 0; Venus (`-venusplay`, B01 + the garden road): **568 frames - 0 / 0 / 0**. Explicit cuts in those runs: 10 + 4 + 4 (cutscene hand-overs far round / behind a wall, one skip teleport). A minute of riding: `ride_sequence/` + `ride_sequence_sheet.png` (one frame has the keeper swimming close past the camera - characters are never cut away).
   *Measured along the way and fixed:* the cutscene hand-over swing (Venus 32 flags → 0), the escape's burst-out framed from
   outside the tube (camera dragged through the wall → now a cut), a skip teleport counted as a move, a race-start swing,
   tiny in/out in the escape tube, the camera dropping below her level when climbing (met the floor), the bike slipping under
   the dock floor in the access bay (the bay's swim box now stops at the dock edge).
2. **Walls under water — fixed (repeat of QF13 10).** *Why QF13's fix didn't hold:* the bike's sweep asked only the
   visual-mesh copy's "solid" layer, which skipped every mesh over 60 m (the chamber's ice ceiling and floor, the ice passage,
   the approach / escape tubes, the temple hall - and the *merged* pillars, ancient structures, spires and platforms, whose
   combined bounds were over 60 m), every surface with a real collider (the base's outside, the dock floors) and the small
   props; sphere sweeps also missed many triangles met from behind (tubes face inward); and the swim-volume clamp ran
   *after* the sweep with no sweep of its own - where a volume reaches past a wall it moved the bike straight through it.
   *Now:* `RideProbe` sweeps the whole frame's move (25 m/s too) against the real colliders **and** every visual mesh
   (Europa: no size cut, both faces), slides along up to four surfaces, pushes out of anything it already touches, and the
   clamp's correction is swept as well; the escape's flying debris glances off the bike. The merged meshes are split per
   piece (so the camera can cut away just the one in its way). **Proof - the wall test** (`-europawalltest`, 157 attempts
   at full speed head-on / 35° / 60° into every area: ice passage 12, great chamber incl. ceiling + floor 36, settlement 24,
   race course 26, damaged passage 4, approach tunnel 14, base outside / bay 6, escape tube 16, open water + temple 13, way
   home 6): **0 failed attempts, bike core inside geometry 0 frames, centre through a surface 0, camera inside geometry 0**
   (57,440 swimming frames). In the free riding runs: *bike core inside geometry **0** / through a surface **0** (24,022 swimming frames in the playthrough, 16,981 in the race run).* *Fixed on the way:* the first wall test failed 68 of
   157 (her own helmet collar counted as a wall - excluded - and one tube wall entered from outside); the second 1 (a sweep
   starting inside the damaged-passage tube: the push-out step did not work with a disabled probe collider - fixed).
   Captures: `wall_test/` (12 slides along walls). In ramming tests the camera still pulls in hard against tube walls
   (9 flagged moves in 57,597 frames of deliberate wall-ramming) - never inside geometry.
3. **The Europa reunion — cut.** After the beam-up a short fade goes straight to the debrief: 7 already in the pilot's chair,
   helmet off, Panther in her lap. `EUR_16` removed (spec, `Docs/EUROPA-LINES.md`, `europa_dialogue.json`), `EUR_16_04.mp3`
   deleted; the Log entry "An ordinary sound" no longer mentions the boarding area.

Also: the star map now reads every connected pad (a second pad holding "current" hid the player's d-pad - found when the
player-style run could not leave Venus's star map). Testing time: about 40 game-minutes - over the 20-minute rule for the
two proofs (allowed by Dan for QF15): 5 wall tests (6.6 min each) while finding the causes, 4 player-style Europa runs, race
and Venus runs.

## For Dan to check (QF15)
- Ride anywhere in Europa (and Venus): the camera stays behind the bike, follows turns and dives smoothly, never swings,
  cuts or jumps by itself; a pillar / arch / ring in the way simply vanishes for a moment; in tunnels it comes a little
  closer and eases back out. The race still turns gently toward the next ring.
- Try to ride through walls, the ice ceiling, the chamber floor, pillars, the tunnels and the temple at full speed: the bike
  slides along every surface.
- Europa's end: after the beam, straight to the chair debrief.


## Round 15: Chapter "Mars — The Divided Kingdoms", first playable pass (Dan, 2026-11-19) — done, status below; release 0.15.0
Commit a checkpoint first. If Dan is away when you get here, **never stop to ask**: make the most reasonable call, list it
under "Round 15 — decisions made", keep going. All standing rules (monochrome, Kyle's fog/glow, non-sexual, 7 never kills,
no exact-spot triggers, captures you look at), **the one saucer prefab (bike always via the bike hold, beam only for 7 /
Panther)**, the fast bike, QF15's one smooth riding camera and full collision, the hand-IK release rules (QF13 15), the
repeat-item rule. 20-minute rule for checks. Release **0.15.0**; "Round 15 — status"; "For Dan to check (Round 15)".

**The spec is `Docs/CHAPTER_MARS.md`** (Dan's script, sections 1–13 + implementation guardrails). Build it as playable
connected spaces like Venus / Europa, reusing every existing system. "PROPOSED" parts are built as written for the
prototype (easy to replace; the guardrails say the symbol, mechanisms, Viper's equipment and the vision await Kyle).

**Everything Dan made for it is in the repo:**
- **Viper:** Kyle's design `Art/Reference/kyle_viper.jpg`, reference `viper_ref_front.png`, Tripo `Art/Source/viper/viper_v1.glb`,
  **rigged body `Art/Mixamo/viper/viper_body_mixamo.fbx`** (~2.3 m). Talking: a jaw bone opens with his voice's loudness (no
  lip-sync); the hood can flare on angry lines. Voice: Dan's saved ElevenLabs voice **`Viper`** (`Docs/VOICES.md`).
- **Reptilians** (one model for the guard, the worker, the caretaker, families, traders; ±10 % size, a few tones; guards get
  a simple helmet / chest plate attached in Unity): `reptilian_ref_front.png`, `Art/Source/reptilian/reptilian_v1.glb`,
  **rigged body `Art/Mixamo/reptilian/reptilian_body_mixamo.fbx`** (~1.9 m). Sash tail + satchel on their own short bones /
  pinned so they never pass through the leg. Voices: premade ElevenLabs voices picked by you via the API (no new saved
  voices) — the guard, the worker and the caretaker clearly different from each other.
- **Animations** (Humanoid retarget; list in `Docs/MODELS-NEEDED.md` "Mars animation clips"): Viper reuses tunneler / elarim /
  YBot clips; Reptilians: new `reptilian_walk.fbx` (Mixamo "Orc Walk" — make sure it reads as an ordinary person, slow / damp
  it if brutish) and `reptilian_wave.fbx`, the rest reused; guards use the idle.
- **Music:** `mars_kingdoms.mp3` (exploration — plains, settlements, ravine, temple, ride back), `mars_viper.mp3` (Viper's
  intervention / the encounter) — `Docs/MUSIC.md`.
- **Vision:** `Art/Video/vision_04_mars.mp4` + still `Resources/Mars/vision_04_still.jpg` (spec section 9 note: the centre
  span lowered into place from both banks; nearly silent — soft stone and wind, no music; play like the other visions).
- **Rune: a tetrahedron**, generated in code like the cube, dodecahedron and icosahedron (Dan).
- Voice all lines with ids `MAR_<scene>_<nn>` (dialogue as data + `Docs/MARS-LINES.md`), 7 on v3 with the spec's delivery
  directions (laughs, breaths, sighs), Panther with his telepathic treatment. SFX for what the chapter needs (barriers,
  machinery, bridge alignment, defensive machinery, crowd / market ambience, wind) → `Docs/SFX-LIST.md`.

**Items**
1. **Getting there:** Mars is playable from the star map / VEN_C13's choice and the main menu's chapter list (works before
   or after Europa). Title card "MARS — THE DIVIDED KINGDOMS" over black from the first frame.
2. **Arrival (spec 1):** the extreme wide over the plain (walls, trenches, elevated roads; two fortress cities either side
   of a deep ravine; the temple on a central stone island, older than the fortifications around it); the pilot's-chair lap
   scene (already seated); land outside the occupied approaches — bike out of the hold, 7 down the beam; Panther aboard.
3. **The crossing (spec 2):** a living Reptilian settlement first (workers, families, traders, guards going about their day),
   then the checkpoint, the stranded supply transport behind the barrier, the guard and the worker.
4. **Viper (spec 3):** the low-angle reveal on the elevated platform, harsh light from beneath; he notices the records; she
   keeps them close; his dry laugh; he orders the crossing secured and leaves.
5. **Both sides of the ravine (spec 4):** the old maintenance route under the main crossing on the bike — broken causeways,
   sheltered passages, broad fast riding sections, clear stopping points; reopen the supply route without attacking the
   guards (nonlethal; a restored service passage the transport crosses); the caretaker on the far bank. Ordinary people on
   both sides, never foolish or warlike.
6. **The shared door (spec 5):** the ancient two-control mechanism overlooking the temple, the conversation.
7. **Two controls (spec 6):** the cooperation sequence — riding between access points, on-foot traversal through the
   ancient bridge structures, redirecting power / repositioning with telekinesis, protecting the repair nonlethally; Panther
   coordinating; the worker and the caretaker on the two controls; the startled "Not that one!"; together → the ancient
   structures align into a passage.
8. **Viper's intervention (spec 7):** a **new kind of encounter — positioning and timing, not Venus's creature fight:** he
   divides the arena with barriers and moving sections; she reconnects routes, disables his control points, closes the
   distance; he's subdued and contained (never killed); the level two-shot after; he's left secured for local custody.
9. **The temple (spec 8–9):** the symbols of both kingdoms as one design (insert), the tetrahedron rune, the vision, the
   return lines, the small counterpart awarded once (insert beside the other records).
10. **A route that stops (spec 10 — revised 2026-11-19):** the temple display shows a route leading out from Mars that
    breaks off partway; the caretaker / 7 / worker lines; sent to the saucer ("Received. Part of a route."). **No new
    destinations unlock here** (the old "asteroid world and Biaveh" leads are gone from the spec).
11. **The ride back (spec 11)** with the transport crossing openly and the guard and caretaker talking; **aboard (spec 12)**
    already seated in the chair; then **departure routing (spec 13 — Dan's revised MAR_13, 2026-11-19)**, saucer stationary:
    - **Europa unfinished → MAR_13U**: only Europa is offered next; no Biaveh, no asteroid.
    - **Europa complete → MAR_13C**: the two records side by side on the display (the Mars route + the Cetaceans' sequence,
      matching intervals, the route resolving) → **Biaveh unlocks** ("Beyond this solar system"); the asteroid stays
      undiscovered.
    - Then **MAR_13B** (shared closing) after either branch; set course.
    - **Europa finished second:** add the same discovery to the **end of Europa** — after EUR_17, if Mars is already complete,
      play MAR_13C + MAR_13B there (the lines work as written: both records are aboard) and unlock Biaveh. It plays **once**,
      whichever of the two is finished second (a saved flag).
    - **Travel event — asteroid discovery (TRV_AST):** on the **first journey toward Biaveh**, after the warp into its system:
      a navigation chime, the display reveals an asteroid with artificial structures, the lines, the player choice
      (Investigate the asteroid / Continue to Biaveh) → **both unlock**. Neither chapter exists yet → after the choice a
      "Chapter — coming soon" card for the one picked, back to free exploration; both stay on the star map. Plays once.
    - Voice every MAR_13U / 13C / 13B / TRV_AST line with its direction (the strokes, the head press, the purr-ish hum as
      animation on the lap pose).
12. **Progression + saves:** the guardrails (completed-world checks; Biaveh only after Mars + Europa; the asteroid only after
    the travel event; Cattalus after Venus + Mars + Europa + asteroid + Biaveh;
    the final Moon mission after Cattalus — just the flags for now), checkpoints for Continue, the chapter + its sections in
    the main menu / pause menu lists, Log + Field Guide entries (Reptilians, the guard, the worker, the caretaker, Viper) with
    the QF13 "grows with the story" unlocks; the tetrahedron with the other records on the chair arm.

**Checks + release (20-minute rule):** targeted runs per section with captures you look at (arrival, settlement, Viper's
reveal, the maintenance route ride, the two controls, the encounter, the temple + vision, the route display, both MAR_13 branches, the Europa-second branch, the asteroid travel event), one
player-style run from the star map to the destination choice, the menu sections start correctly. Publish **0.15.0**; docs;
commit + push.

## Round 15 — status (2026-11-20): first playable pass done, exe-verified off-screen; release 0.15.0 (`game-1500`)
Captures + sheets: `Docs/Previews/r15/` (the autopilot run `run/`, the player-style run `playtest/`, the two MAR_13
branches `routing/`, the menus `menus/`).

1. **Getting there — done.** VEN_C13's Mars choice now starts the chapter (no more "coming soon"); Mars is set from the star
   map at any time (Continue where it left off; done = a "visited" card) and is in the main menu's chapter list (same unlock
   rule as Europa: VEN_C12) with its 9 sections, in the pause menu's Scene select too, and "Continue — Mars: …" when a Mars
   game is in progress. Title card "MARS — THE DIVIDED KINGDOMS" over black from the first frame.
2. **Arrival — done.** The chair scene aboard (Panther in her lap, the navigation display, MAR_01), then the Mars scene opens
   on the extreme wide: the saucer crossing the plain (walls, trenches, elevated roads, the two fortress cities in the haze,
   the ravine, the temple on its island) to the landing outside the approaches; the bike out of the bike hold, 7 down the
   beam, Panther aboard. Saucer check passes (the one prefab).
3. **The crossing — done.** 25 Reptilians going about their day (workers, families, traders, guards; ±10 % sizes, three
   tones, guards with helmet + chest plate; market stalls, street lamps), the checkpoint: barrier, the stranded supply
   transport, the guard (medium two-shot) and the worker (MAR_02).
4. **Viper — done.** He rises on his platform above the checkpoint, lit from beneath (low angle), notices the records, she
   keeps them close, the dry laugh, he orders the crossing secured and sinks away; Panther's private lines in gameplay.
5. **Both sides of the ravine — done.** The maintenance route: the west ramp cut into the bank, the ravine floor (broad riding,
   rubble, two lookouts), the broken causeway (lay the fallen slab with telekinesis), the sheltered passage (a carved roof on
   columns), the east ramp and its sealed service gate (restore its power, X) - the transport comes through the restored
   service passage (nobody attacks the guards); the caretaker on the far bank (MAR_04).
6. **The shared door — done.** The mechanism on its overlook, the two channels, the conversation (MAR_05).
7. **Two controls — done.** Back across to the worker (who volunteers), ride to the relay on the west rim (X), on foot down
   the pier steps beside the old bridge to the displaced conduit (TK), Viper's three small machines going for the worker's
   control knocked aside (TK - machines, nobody hurt), Panther coordinating, the span section that swings out - "Not that
   one!" - held with TK, "Now." - both controls together, the wide: the spans' sections rise and lock into a passage.
8. **Viper's intervention — done (a new kind of encounter).** On the island plaza: three lanes divided by low walls and his
   energy barriers, a moving section that opens and closes on a rhythm (timing), a broken strip she bridges by lifting a
   walkway section (TK), three control pylons to disable (X) - each opens a route; then she closes the distance and
   restrains him (a telekinetic push, he goes down, a containment ring - alive, left for local custody); the two-shot with
   the worker and the caretaker coming from opposite sides (MAR_07).
9. **The temple — done.** The two kingdoms' symbols as one design (insert, held), the tetrahedron rune (code), the vision
   (`vision_04_mars.mp4`, soft stone + wind, no music; still fallback kept), the return lines, the small tetrahedron awarded
   once (`awards 1`), then with the other records on the chair arm aboard.
10. **A route that stops — done (revised spec).** The display shows a route out of the system that breaks off; MAR_10; sent to
    the saucer; no unlocks.
11. **The ride back + aboard + departure routing — done (revised spec).** The barrier open, the transport crossing openly,
    the guard and the caretaker working side by side (MAR_11 as she slows); the bike into the hold, the beam; aboard MAR_12
    (Panther climbs into her lap); MAR_13 by the completed-world flags: **Europa unfinished → MAR_13U** ("Europa, then." -
    Europa starts / continues); **Europa complete → MAR_13C** (the Mars route + the Cetaceans' sequence, Biaveh unlocks) →
    MAR_13B → "Toward Biaveh" → **TRV_AST** (the asteroid, the choice: investigate / continue - both unlock; "coming soon"
    card for the one picked) → free exploration. **Europa finished second:** the same MAR_13C + MAR_13B + TRV_AST play after
    EUR_17, once (a saved flag). All 40 new / revised lines voiced.
12. **Progression + saves — done.** `MarsState` (checkpoints for Continue, Scene Select slot), `WorldFlags` (Biaveh only after
    Mars + Europa; the asteroid only after the travel event; Cattalus after the five; the final Moon after Cattalus -
    flags only); Log (8 entries) + Field Guide (Reptilians, the guard, the worker, the caretaker, Viper; growing with the
    story); a new game clears Mars and the route flags.

**Checks.** The autopilot through the whole chapter (crossing → aboard) and the arrival (title card, chair, the wide, the landing); the two MAR_13 branches (`-routes europa -marscp 8`: MAR_13C at the Mars debrief → Biaveh unlocked → TRV_AST → choice; `-routes mars -europacp 8`: Europa finished second → MAR_13C + MAR_13B + TRV_AST after EUR_17, once); the menus (`-menupages`: Mars in the chapter list + its 9 sections, the pause menu's Scene select; monochrome 0); and the **player-style run from the main menu / VEN_C13's star map to the departure routing** (`-marsplay -menupick "Venus|Venus: the debrief" -routes none`): **MARS PLAYTEST: 1/1, soft-locks 0, skips 1** (12.6 game-min; the one skip - she was still on the pier walk when the bridge aligned and the light pointed across the ravine - fixed after: a guided walk back up the steps; the stretch from the controls to Viper re-run: 0 skips), riding camera **0 aim / 0 position shifts / 0 reversals, camera inside geometry 0** (10,653 riding frames), audio peak 0. Earlier player-style runs needed 7 then 4 skips - all guide / driver issues fixed (lights placed beyond gates, the pier steps and the east gate routed, the bike re-mount, braking at the light). **Testing time: about 90 game-minutes - far over the 20-minute rule**, spent on four full player-style runs and five autopilot / section runs while the chapter's first pass was being made to play through. Not run: Viper's / the Reptilians' animation measured in Blender (sash / satchel vs legs), a sound check of the new SFX.

## Round 15 — decisions made (Dan away; easy to change)
- **Arrival order:** the chair scene aboard first, then the extreme wide over the plain in the Mars scene (one scene change
  instead of two); the title card is first, over black.
- **Viper talking:** Dan's Mixamo rig has no jaw bone - his head and upper body move with his voice's loudness, the hood
  flares (head / neck widen) on his sharper lines. A real jaw needs a jaw bone weighted in Blender (follow-up if wanted).
- **Reptilians' walk (Orc Walk):** played at their real speed (1.15 m/s, families 0.9) with the hip sway damped by a third -
  reads as an ordinary walk in the captures. Sash tail / satchel: no extra bones on the Mixamo rig yet (not measured in
  motion).
- **Voices:** premade ElevenLabs voices - guard "Daniel" (steady, formal), worker "Sarah" (young, warm), caretaker "Bill"
  (old, calm). Only the vocal part of a direction becomes a v3 tag (the tables' stage directions drive camera / animation).
- **Lines the spec gives as action only** are mimed, no new dialogue: the worker volunteering (a wave, a point), the
  guard and the caretaker's "few practical words" (talk animations).
- **Short cuts:** the transport's crossing ends on it coming up through the gate (a fade before); the caretaker taking her
  to the mechanism, and out of the temple to her bike, are fades.
- **Section 6 tasks:** relay (ride + X), the pier conduit (on foot + TK), Viper's three machines (TK), the swinging section
  (TK) - Panther's lines placed between them.
- **The encounter:** lanes + barriers + a rhythm-timed moving section + a TK walkway + three pylons, then a telekinetic
  takedown and a containment ring (Viper lies dazed, alive).
- **MAR_13U ends the chapter by flying to Europa** (her line "Europa, then." selects it) - Europa starts or continues.
- **MAR_13C → straight to the first journey toward Biaveh:** a "Toward Biaveh" card, then TRV_AST right away (both at the
  Mars debrief and after EUR_17); choosing Biaveh later on the star map before the asteroid is found plays the same journey.
- **Star map:** Biaveh at the outer edge ("beyond this solar system"), the asteroid beside it.
- **Mars unlock** in the chapter list = the Europa rule (VEN_C12 sent it to the star map).
- **Field guide:** no portraits for the Mars characters yet.

## For Dan to check (Round 15)
- Chapter list → Mars → Play from the start (or VEN_C13 → Mars): title card, the chair, the wide over the plain, the landing.
- The settlement and the checkpoint: do the Reptilians read as ordinary people? Viper's reveal on his platform.
- The maintenance route on the bike (the causeway slab, the gate), the caretaker, the mechanism.
- The two controls (relay, pier steps + conduit, the three machines, "Not that one!"), the bridge aligning.
- The encounter: barriers, the moving section, the walkway, the three pylons, restraining Viper.
- The temple: the joined symbols, the tetrahedron, the vision, the route that stops.
- Aboard: with Europa unfinished → "Europa, then."; finish Europa afterwards → the combined records + Biaveh + the asteroid
  appear at the end of Europa.
- Decisions above (especially Viper's jaw, the arrival order, MAR_13U flying straight to Europa).


## Quick fix QF16: the view outside, travel between worlds, staging + Panther's cat moments (Dan, 2026-11-20) — done, status below; release 0.15.1
Commit first. All standing rules (monochrome, Kyle's fog/glow, the one saucer prefab). 20-minute rule. Release **0.15.1**;
"QF16 — status"; "For Dan to check (QF16)".

1. **Aboard the saucer, the view outside is the world you're at.** Every window / exterior view from inside the saucer
   (and the console's star map backdrop, if it shows outside) shows **the place the saucer is currently at**: the Moon's
   surface and Earth in the sky; Venus's gardens and clouds; Europa's ice field and Jupiter; Mars's plains, the ravine and
   the two cities; open space only while travelling. Driven by one "where is the saucer" state (set on arrival, saved, used
   on Continue / Scene Select / the chair scenes), not per scene. Use simple, cheap backdrops (a skybox / matte painting /
   a low-detail version of the world's own terrain + atmosphere) in Kyle's misty style. Check captures from the interior
   at each world.
2. **A travel sequence between worlds — "sacred-geometry passage" (Dan's pick, 2026-11-20).** When she sets course, a short
   sequence (about 6–10 s, skippable, then the arrival / title card): seen from outside behind the saucer and through the
   window, **rings of glowing geometric shapes — the rune solids she has collected (cube, dodecahedron, icosahedron,
   tetrahedron, …) — unfold out of the dark ahead, turning slowly, and form a corridor around the saucer**; the saucer
   glides through it; on arrival they **fold shut** behind it and the destination fills the view. Monochrome, soft glow and
   haze (Kyle), slow and elegant — **not** star streaks or a classic warp tunnel. The corridor uses only the shapes she has
   collected so far (it grows over the game; with one record, rings of that one shape). A soft swelling hum + chime, the
   ship's music ducked. Used for every journey (star map, the chapter-end choices, the asteroid travel event happens inside
   it on the first trip to Biaveh). Short capture sequence of one journey.

3. **Vary where the conversations aboard happen (Dan, 2026-11-20): not every talk in the pilot's chair with the cat in her
   lap.** Use different places and poses around the saucer. Staging per scene (the chat's proposal — keep it in one data
   table so Dan can swap any of them):
   | Scene | Staging |
   | --- | --- |
   | Venus C11 (debrief after Venus) | **Both lying on the sofa** on the main deck: 7 on her back, head on the armrest, Panther (house-cat size) stretched out on her stomach; her hand on his back |
   | Venus C13 (choosing the next world) | At the navigation console, 7 in the chair, **Panther sitting on the console** beside the star map (not in her lap) |
   | Europa EUR_01 (arrival) | **Standing at the window** looking down at the ice field (QF16 item 1's view); Panther on the window ledge / console beside her |
   | Europa EUR_17 (debrief) | **7 sitting on the floor, back against the sofa**, Panther curled on the cushion beside her head |
   | Mars MAR_01 (arrival) | **At the window**, Panther small, **perched on her shoulder**, both looking down at the plain |
   | Mars MAR_12 (what she remembers) | **Both lying on the sofa** (quiet, personal talk) |
   | Mars MAR_13 / the Europa-second discovery (route display) | Navigation chair (it needs the display); Panther **on the console**, turning to the display |
   | The galley | keep for a future meal moment (Panther's "I suggest eating before we leave") |
   Scenes start already in their pose after a short fade (no long get-up / lie-down transitions needed). The lines'
   directions that say "in her lap" / "strokes his back" become the matching beat for the new pose (a hand on him, his head
   against her hand, etc.). **Non-sexual presentation rule applies fully to the lying poses**: relaxed, natural,
   fully clothed as always, the camera framing faces and the cat — no body-emphasis angles. Poses: 7 lying = a Mixamo
   clip if Dan adds `Art/Mixamo/7/7_lying_idle.fbx`, otherwise build the pose (like the guardian's kneel); sitting on the
   floor and the shoulder perch can be built poses too. Panther's poses from `CatPoseOverlay` + new lying-on-her / perched
   variants. Hand IK released when each scene ends (QF13 15). Captures of every staging.

4. **Panther does cat things — the humor (Dan, 2026-11-20): "not always, but sometimes."** The joke is the dignified,
   dry-witted telepathic cat still being a cat. Two kinds, all in one editable table (the chat's proposal; mostly wordless —
   body language + 7's reaction sounds from the reaction bank):
   **Scripted moments (one each, in these scenes):**
   - **Venus C13, the star map:** as she brings Mars up, he strolls across the console **right in front of the screen**,
     pauses there, sits; she leans sideways to see round him; he moves on, unhurried (Dan's example).
   - **Mars MAR_13C, the route display:** as the route resolves he **bats at the glowing route line** on the display with a
     paw; on "Those intervals match." he's sitting with his paw still raised.
   - **Europa EUR_01 at the window:** he **paws at the glass** at something drifting past outside, tail twitching.
   - **Mars MAR_01, on her shoulder:** his **tail flicks across her face**; she blows it aside without looking.
   **Ambient while she's free aboard** (random, rare — at most one every 2–4 minutes, never repeat back-to-back):
   - asleep in the light from the window (QF16 1); kneading the sofa, turning round twice, flopping down;
   - sitting **in the pilot's chair** when she walks up — he looks at her, then gets down slowly, with dignity;
   - curled **in the bike's cradle** in the bike bay (QF14 3);
   - **nudging one of the small rune records off the chair arm / console while looking straight at her** — she catches it
     mid-air with telekinesis and puts it back (rare; a small chuckle);
   - chasing a light reflection across the deck, stopping dead when she looks, grooming as if nothing happened;
   - a big stretch and yawn; sitting in a "loaf" staring at nothing.
   **Rules:** never during sincere / emotional lines (the same moments the reaction placements leave alone — e.g. the
   visions' aftermath, "How could I know that?", the reunion beats); never blocking a gameplay choice for more than a couple
   of seconds; the player can always walk away. Build the poses / motions procedurally on his rig (`CatPoseOverlay` etc.)
   and with his growth at house-cat size. Captures of each scripted moment and two or three ambient ones.

## QF16 — status (2026-11-20): done, exe-verified off-screen; release 0.15.1 (`game-1501`)
Checkpoint = the Round 15 commit (the tree was clean). Previews: `Docs/Previews/qf16/` (`1_*` window views, `2_*` the
passage, `3_*` stagings, `4_*` cat moments; `sheet_1..4_*.png` contact sheets). Monochrome check over all 37: 0 non-grey
pixels. Every run muted (audio peak 0.000000), minimized, Below Normal.

1. **The view outside = where the saucer is.** One saved state, `SaucerLocation` (`Runtime/SaucerOutside.cs`; PlayerPrefs
   `seven_ship_at`, check runs `seven_test_ship_at`): "moon" / "venus" / "europa" / "mars", "space" while travelling (not
   saved). Each aboard part sets it on start (Chapter 1 = the Moon, the Venus debrief = Venus, Europa / Mars aboard = that
   world), every journey sets it on arrival, so Continue / Scene Select / the chair scenes all agree.
   `Editor/FightDemoBuilderOutside.cs` builds one cheap backdrop per world at the old city's depth (y −200, the ship
   hovers high): the Moon (grey cratered plain, Earth low in the black sky), Venus (rolling garden hills, dark tree clumps,
   a bright cloud ceiling), Europa (ridged ice field, Jupiter huge in the sky), Mars (the plain, an arc of walls, the ravine,
   the two fortress cities), open space (stars). `SaucerOutside` switches the group and sets the Atmosphere to that
   world's haze (ground mist on the backdrop's ground, light distance haze, Kyle's bright misty distance). The galley
   porthole's disc shows that world too. The Round 3 hollow-Moon city stays only for the saucer demo and Chapter 1's
   scene 9 (still inside the Moon). Captures: `1_window_view_<world>` (+ `_low`, + porthole) at all four worlds, reached by
   real journeys in one run (`-qf16check`).
2. **The sacred-geometry passage** (`Runtime/TravelPassage.cs`, ~8 s, skippable with Y / Enter / A / Esc): the one saucer
   prefab instance (unchanged, `SAUCER CHECK` passes; parked far above the deck in `TravelStage`) glides through the haze
   while rings of her collected solids unfold ahead, turning slowly; then through the window the corridor slides past; on
   arrival the rings fold shut (chime) and the destination's view is there. Shapes = what she has: the dodecahedron always
   (she has it since the Moon), the cube after Venus's record, the icosahedron after Europa's, the tetrahedron after Mars's.
   A soft swelling shimmer (`vision_shimmer`) + the `course_set` / `console_select` chime; the music suspended and resumed.
   Used for every journey: the star map (Europa, Mars, the Moon / Venus), VEN_C13's pick, the destination screen, MAR_13U →
   Europa, and the first trip toward Biaveh (replaces the "Toward Biaveh" card; TRV_AST follows it with the windows on open
   space). Captures `2_passage_*` (a Venus journey a/b/c; the Biaveh journey with dodecahedra + tetrahedra).
3. **Staging per conversation** — one table, `Chapter1AboardStaging.cs` (`Table`), any row can be swapped:
   VEN_C11 + MAR_12 = both on the sofa (7 on her back, head on the armrest, Panther settled on her, her hand by him);
   VEN_C13 + MAR_13 (+ the combined records) = the chair, Panther **on the console** beside the display; EUR_01 = standing at
   the window, Panther at the glass; EUR_17 = on the floor, back against the sofa, Panther on the cushion; MAR_01 = at the
   window, Panther small on her shoulder. Each starts in its pose after a short fade; the scenes' framings (two / lap /
   side / ots / wide) map to staging-specific shots that frame faces and the cat (three-quarter front at the window, over
   the shoulder at the view, never full-length from behind). Hand IK released at every exit.
   **Poses:** 7 lying / sitting on the floor = `SevenRestPose.cs`, her own bones aimed after the animator (Blender-made clips
   were tried first: the humanoid retarget turned them into the muscle-zero pose, so they were dropped). At the window she is
   held in her idle. Captures `3_*`.
4. **Panther's cat moments** (`Chapter1AboardCatMoments.cs`). Scripted: VEN_C13 he strolls across the console, sits right
   in front of the star map, she leans to see round him ("hm?"), he moves on; MAR_13C he bats at the route line on "Hmm. One
   moment." and sits with the paw still raised on "There. Those intervals match."; EUR_01 a mote drifts past outside, he paws
   at the glass, tail twitching; MAR_01 his tail flicks across her face, she blows it aside. Ambient while she is free aboard
   (one every 2–4 min, never the same twice running, only when no scene / line / journey is on, and it stops the moment one
   starts): asleep in the window light (curled), kneading the sofa + two turns + flopping down, sitting in the pilot's chair
   until she walks up (he looks at her, gets down, she laughs through her nose), chasing a light reflection then grooming
   when she looks, a big stretch + yawn, a loaf staring at nothing. Captures `4_*` (all four scripted, six ambient).

**Decisions made (Dan away; easy to change):**
- **Not built:** the bike-cradle nap (the bay isn't reachable on foot) and the record nudge + telekinesis catch (no records
  rest on the chair arm while she explores). Both left for later.
- In the lying stagings Panther sits settled on her chest / on the cushion rather than fully curled (the skinned "sleep"
  clip doesn't read curled; the static curled body is used for the ambient naps).
- EUR_01: there is no window ledge, so Panther sits on the glass floor at the window (the glass moment happens there).
- The windows look down at each world from high above (all backdrops at y −200), not a ground-level view.
- The "Toward Biaveh" title card is replaced by the passage itself.

**Testing:** `-qf16check` (new: free exploration aboard → every ambient moment → journeys Moon → Venus → Europa → Mars →
Moon with window + porthole captures, ~3.5 min), `-venusselect C11` (VEN_C11 → C13 → the passage → MAR_01),
`-marscp 8 -routes europa` (MAR_12 → MAR_13C → the Biaveh passage → TRV_AST), `-europa`, `-europacp 8`. About
**22 minutes** in all — a little over the 20-minute rule (three short rounds of fixes on the stagings: the rest pose, the
cat's pose, the window framings). One crash on the way (`level4 is corrupted`: two MonoBehaviours shared
`TravelPassage.cs`) → split into `TravelStage.cs` / `FaceCamera.cs`, clean build.

## For Dan to check (QF16)
- The windows at each world (Moon, Venus, Europa, Mars) and the porthole disc — misty enough, or more of the world?
- One journey from the star map: the passage (outside, through the window, folding shut) — length, glow, the shapes
  growing as she collects records.
- The sofa scenes (VEN_C11, MAR_12): her lying pose and Panther on her; EUR_17 on the floor; EUR_01 / MAR_01 at the window.
- The cat moments: C13 in front of the star map, the paw on "Those intervals match.", the glass, the tail; the ambient ones
  while exploring (stay aboard for a few minutes).
- Whether any staging should go back to the chair (one line in `Chapter1AboardStaging.Table`).

## Quick fix QF14: Dan's 0.14.1 play — Europa focus + the menu (Dan, 2026-11-19) — done, status below; release 0.14.2
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


## QF14 — status (2026-11-19): done, exe-verified off-screen; release 0.14.2 (`game-1402`, Latest, published)
Captures + sheets: `Docs/Previews/qf14/` (`item<NN>_*`; menus at 16:9 and 16:10 in `menus_16x9/`, `menus_16x10/`; the base
walk's 102 frames in `item06_base_walk/` + its per-frame log `item06_base_camera_frames.csv`).

1. **Every unlocked chapter on the main menu — done.** One table, `Runtime/ChapterList.cs` (name, unlock rule, "play from the
   start", its sections); a new chapter is one new row. Unlocks: Chapter 1 always; Venus once Chapter 1 reached the galley
   (or Venus was reached); Europa once Europa was reached or VEN_C12 (the star map) was seen. **Decision (easy to change):**
   once unlocked a chapter stays unlocked (`seven_unlocked_<id>`), a new game does not take it away.
2. **Main menu cleaned up — done (layout decided by the chat).** Continue (with a save) · Start new game (the existing confirm)
   · Chapter 1 — The Moon · Venus · Europa · Settings · Controls · Quit. The fight demo, saucer demo and Ratwing cave are off
   the menu (their `-fight` / `-saucer` / `-ratwing` switches still work for checks). A chapter opens its page: "Play from
   the start" (Venus / Europa ask before wiping that chapter's progress), its sections, Back. Lists that do not fit scroll
   (stick / d-pad / keys / mouse wheel, the selected row always in view) with a gray / white scroll bar. The pause menu's
   Scene select uses the same chapter → sections pages. Checked at 1280x720 and the Deck's 1280x800 (`-menupages`): 14 pages
   each, monochrome 0; the long Continue row now shrinks to fit (it ran past its bar in the first capture).
   `-menupick` paths: `"Venus|Venus: the debrief"`, `"Europa|Europa: Tunneler's base"`; the old `"Scene select|Chapter 2 —
   Venus|…"` still works.
3. **The bike bay — rebuilt as a real room (repeat, fourth try).** *Why the earlier fixes didn't hold:* every try (R13 dark,
   QF12 glow, QF13 flat unlit) changed the **material** of the same "bay" — the hull's own hatch sector, a floor and ceiling
   3 cm inside the skins from r 1.6 m to the rim. The hull is 0.26 m thick at the rim, so that bay was a thin flat wedge
   (Dan's "gray slab"), and the 0.95 m bike passed **through its ceiling** as it glided out ("the bike's shapes poking
   through it"). No material could fix that. *Now* (`Editor/FightDemoBuilderBikeBay.cs`, inside the one saucer prefab): a
   modelled room set into the hull's thick middle, sized to the bike (3.6 x 2.1 x 1.26 m for the 3.0 x 1.23 x 0.95 m bike):
   a thick rounded chrome frame on a bulkhead at r 3.45 m, curved brushed-steel walls with sweeping chrome ribs and arches,
   panel seams, a recessed ceiling with three round lights (real lights, kept inside the room), three drawers low on each
   wall, a control panel (round button + two small ones) on the right wall, a dark polished floor with a raised chrome track
   from the back stop to the opening, a centre pad and two chrome cradles under the bike, a rounded back wall, the room's own
   reflection probe. The hatch panel now covers only the hull in front of the frame; once it slides away that part is a
   full-height slot, so the bike glides straight out through the frame (8 cm clear above, 18 cm each side). The bike is
   shown sitting in the lit room as soon as the panel slides, and stays in its cradles until the hull is shut when it comes
   home. *Captures:* `item03_bay_vs_reference.png` (Dan's reference beside the bay straight on), the B01 player-camera
   sequence `item03_bay_player_view_sequence.png` (opening → room with the bike → gliding out), Europa
   `item03_europa_bay_open.png`. First capture of this build: the frame and ribs read black (fully metallic, reflecting the
   dark hull) and the room lights lit the slot walls outside — both fixed before release. *Differences from the reference
   that remain:* the 3 m bike fills most of the 3.6 m room, so the back wall, the third light and the back stop are mostly
   behind it; the room is lower than the reference's proportions (the hull is only ~1.4 m thick there); from the player's
   B01 camera (rim height, ~9 m out) the room is small in frame.
4. **The race can be run again — done.** After a finish "Race again" (X, 8 s) puts her back at the arch facing the first
   ring; the arch also re-arms once she has ridden 12 m away. Best place and **best time** are saved (`raceBestTime`) and
   shown after each race ("Place 1 of 4 - 9.5 s (best time)"). Check: two races in a row, `race 2 … place 1 … new best time`.
5. **Race camera — done.** *Cause:* under water round the arches and pillars the follow camera called the spot "tight" and
   swung or **cut** round, snapping in for every mesh, then eased back behind the bike — a reset every few seconds. In the
   race (`ChapterCamera.RaceMode`) there are no swings or cuts, pulling in is smoothed, the camera follows her steering, and
   at the start and after each ring it turns smoothly toward the next ring for ~2 s; the ring marker / arrow shows the whole
   race. Logged: `camera swings / cuts during the race 0 / 0` in both races. `item05_race_camera_3_rings.png` (0.3 / 1.2 /
   2.4 s after rings 1-3). *Left:* pillars sometimes stand between the camera and the bike for a moment (the bike stays in
   view).
6. **Base camera — fixed (repeat of QF13 7).** *Why QF13's fix didn't hold:* it hid the walls in the way but their colliders
   still drove the follow camera: it snapped in for a wall that was no longer drawn, its body check then called the spot
   "tight" and cut / swung round, and AutoBehind eased it back into the same wall — two systems fighting. QF13 measured only
   "her head hidden" (fine) and never the camera's distance. *Now* (`ChapterCamera.Cutaway`): inside the base every part
   except the floors is cut away **instead of** moving the camera (left out of every cast; kept cut 0.6 s, no flicker), the
   camera keeps its boom (min 1.7 m), changes are smoothed with a dead band, no swings or cuts, the focus is locked to her,
   and cut-away mode is on from the first frame after a cutscene. The measure found three more causes, all fixed: a focus
   that trailed her (±0.2 m swings when she started / stopped), the window hall glass / screen not in the cut list (the
   camera sat inside them: 5 black frames), one frame of the old logic after each cutscene. *Proof:* the player-style walk
   (`-europaplay -europacp 4 -testspeed 2`: dock → pressure chamber → corridor → control room → holding area → transport
   dock → machinery devices → window hall → back to the bike): `11382 frames in cut-away mode, camera distance 2.38-2.70 m,
   jitter 1 (one 5 cm reversal), bounces 0, swings 0, cuts 0`; 102 captures, no dark frame after the fade-in. The first
   walk on this build measured 6 bounces / 22 jitters, the second 1 / 3.
7. **The guardian's new walk — in.** `elarim_walk_v2.fbx` (Catwalk Walk Forward) loops on his walk state; its forward travel
   is taken out (root motion not baked into the pose and not applied), ElarimNpc moves him and matches the playback to his
   speed (0.94-1.05). Measured in Blender: hip sway 3.0 % of his height (7's walk 2.4 %), hip roll 12 deg (7's 15) — a mild
   runway walk; to keep it calm the side sway and hip roll are damped by a third at runtime (7.1 → 4.6 cm). Cape on his back
   (top 0.25 m below the shoulders, as the model), leg contact 0.0 cm. `item07_guardian_walk_sheet.png` (player camera +
   side). *Left:* his arms swing a little out from the body (the clip's style; RelaxArms trims it); the hem sometimes brushes
   the ground (0.01 m vs the model's 0.14).

**Checks.** 3 builds; the saucer check passes in all three scenes; audio peak 0 in every run; menus monochrome 0. **Testing
time: about 24 game-minutes — over the 20-minute rule by ~4,** spent on the repeat items (three base walks to prove 0
bounces; one guardian run that did not stop after the walk and ran on ~5 min). Not run: a full player-style run of each
chapter.

## For Dan to check (QF14)
- Main menu: Continue / Start new game / one entry per chapter you've unlocked / Settings / Controls / Quit; no test demos.
  Open Venus (the longest page): it scrolls with a scroll bar. Pause menu → Scene select works the same way. On the Deck too.
- Venus B01 and Europa: the hatch slides and you look into a lit chrome room with the bike in its cradles; it glides out
  through the frame without touching anything. Compare with your reference (`Docs/Previews/qf14/item03_bay_vs_reference.png`).
- Europa race: "Race again" (X) after the finish, or ride back through the arch later; the best time shows. The camera stays
  behind the bike and turns toward the next ring after each one — no resets.
- Tunneler's base on foot, all the way through: the camera stays at the same distance; walls in the way just vanish.
- The guardian's new walk in "Follow me": calm enough? (sway damped by a third — can be more or less.)
- Decisions to confirm: chapters stay unlocked after a new game; Chapter 1's "Play from the start" is the full new game.


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
