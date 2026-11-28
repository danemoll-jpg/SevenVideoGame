> **Update (2026-11-19, later):** new scenes **VEN_C12** (the guardian's two locations, after C10) and **VEN_C13** (choosing Mars or Europa, after C11) — Dan.
>
> **Update (2026-11-19):** VEN_B01 name reveal rewritten (Dan); C02_02 = "I felt that from here."; obvious tremors; no ramp — the saucer uses its beam.
>
> **Update (2026-11-18):** VEN_C09 vision video + new lines (Dan). Hover bike added — section 6b, scenes VEN_B01–VEN_B07 (Dan). Round 11 builds VEN_B01–B05, Round 12 VEN_B06–B07. VEN_B01 now holds the cat's name reveal: **Panther** (Dan).
>
> **Status (2026-11-17):** complete spec from Dan (sent in two parts, joined here). Build it in two rounds (Round 11: VEN_01–04, progression, C01–C05; Round 12: encounter, C06–C11). Wormmaster on Venus is provisional for the prototype.

Build the Venus chapter for **7: The Observer** using the existing project’s player controller, interactions, dialogue, save system, and visual conventions. Inspect what already exists before creating replacement systems.

Implement the chapter as playable connected spaces with placeholder assets where necessary. Keep dialogue, objectives, and cinematic cues editable independently of gameplay logic.

**1. CREATIVE RULES**

- Art is strictly black-and-white grayscale.
- 7 never kills. Every encounter must support a nonlethal resolution.
- Cat normally remains aboard the saucer and communicates telepathically.
- Cat does not know 7’s origin or the explanation for her visions.
- Visions are passive scenes from the past. The player does not interact with their contents.
- Facial animation is not required. Convey emotion through posture, framing, hand movements, pauses, and sound.
- Eye close-ups use a steady gaze or gaze shift; do not require crying, smiling, or complex expressions.
- The Elarim are good guardians protecting their temple and its rune.
- Venus represents the First Garden Age.
- Venus’s rune is a cube. The Moon’s rune is a dodecahedron.
- The Moon object bears an Elarim-associated marking. This suggests a connection, not Elarim ownership of the Moon’s rune.
- Do not reveal 7’s immortality or explain her full mission here.
- Later worlds should remain playable in varying orders.

**2. PROVISIONAL CONTENT**

Use these choices for the prototype, but keep them easy to replace:

- Wormmaster is the Venus antagonist.
- His machinery controls burrowing creatures and directs them against the temple foundations.
- His immediate demand is possession of the temple. His deeper motive remains unresolved.
- The guardian gives 7 a small cube containing a copy of Venus’s history. The original remains in the temple.
- Three control devices form the encounter objectives.
- The personal vision ends with someone placing a seed into an open palm.

These are implementation assumptions, not additional approved canon.

**3. CHAPTER OUTCOME**

At the end of the chapter:

- The threat against the gardens has stopped.
- The controlled creatures survive.
- Wormmaster is subdued.
- The Elarim continue repairing their home.
- 7 has received Venus’s historical record.
- She has experienced a personal memory that the guardian says was not part of the record.
- She has a reason to investigate other temples.
- The player returns to the saucer and chooses among destinations supported by the existing game.

Do not automatically force the Moon or another specific world as the next destination.

**4. PLAYABLE SPACE LAYOUT**

Create these connected areas:

VEN_01 — Landing platform  
Saucer entrance, arrival vista, the saucer's storage hatch with the hover bike, the start of the riding route.

VEN_02 — Outer gardens  
Inhabited terraces, water channels, signs of damage. The broad first riding route (section 6b) runs through them to the temple's outer court; a blocked main walkway with a short on-foot alternate route.

VEN_03 — Temple  
Outer court (bike parking — the bike never enters the temple), pedestrian garden, guardian interaction area and protected cube chamber.

VEN_04 — Lower gardens  
The narrower maintenance riding path from the temple, damaged channels, the creature observation area (bike parking), the root chambers and the on-foot route to underground infrastructure.

VEN_05 — Control chamber  
Wormmaster, three control devices, creature routes, and nonlethal containment mechanism.

VEN_06 — Saucer interior  
Reuse the existing interior for the return conversation and destination selection.

The temple and gardens are revisited after the encounter. Reuse their spaces with changed activity and audio rather than creating duplicates.

**5. PROGRESSION**

Use the following ordered chapter stages:

VEN_S00_AVAILABLE  
Venus is accessible.

VEN_S01_LANDED  
Arrival completed. Objective: Find the Elarim temple.

VEN_S02_GUARDIAN_MET  
7 has shown the dodecahedron.

VEN_S03_HELP_ACCEPTED  
Temple inspection interrupted by damage. Objective: Investigate beneath the gardens.

VEN_S04_CONTROL_DISCOVERED  
Player has observed the link between signals and creature movement. Objective: Find the source of the signal.

VEN_S05_ENCOUNTER_ACTIVE  
Wormmaster introduced. Objective: Disable the control devices.

VEN_S06_THREAT_STOPPED  
Devices disabled and Wormmaster contained. Objective: Return to the guardian.

VEN_S07_RECORD_RECEIVED  
Vision and guardian exchange completed. Objective: Return to the saucer.

VEN_S08_COMPLETE  
Saucer conversation completed. Destination selection available.

Persist separately:

- IDs of disabled control devices.
- Whether Wormmaster is contained.
- Whether Venus’s record has been awarded.
- IDs of completed one-time conversations.
- Any optional garden assistance completed.
- The hover bike's parked location and whether 7 is riding it.

Award the cube record exactly once. Reentering the temple or reloading must not duplicate it.

**6. SCENE AND DIALOGUE DEFINITIONS**

Each dialogue line needs a stable ID, speaker, text, delivery type, and optional camera cue.

Camera cues are presentation instructions, not gameplay requirements. If a cue cannot run, the scene must still progress.

**6b. THE HOVER BIKE (Dan, 2026-11-18)**

7 has a hover bike (Kyle's design: `Art/Reference/kyle_hover_bike.jpg`, model reference `Art/Reference/hoverbike_ref.png`;
the **forked end is the front**, long pointed tail behind, low recessed saddle, **two recessed grips molded into the
fairing** — no handlebars). Riding gets her between places; exploring and helping people means getting off.

Chapter sequence with the bike:
Arrival (VEN_C01) → retrieve the bike (VEN_B01) → introductory ride (VEN_B02) → damaged garden route (VEN_B03, carries
VEN_C02) → park at the temple (VEN_B04) → guardian conversation (VEN_C03, VEN_C04) → ride to the lower gardens (VEN_B05,
ends with VEN_C05) → park at the root chambers → on-foot encounter (VEN_C06, VEN_C07) → return ride (VEN_B06) → temple
vision and record (VEN_C08–VEN_C10) → ride to the saucer (VEN_B07) → debrief in the saucer.

Rules for the first playable version:
- Reuse one bike throughout the chapter. Save its parked location and whether 7 is riding.
- Clear mount and dismount interactions; dismount only where there is safe standing space.
- Restore the riding controls and camera correctly after dialogue.
- Keep mission-critical paths reachable if the bike becomes stuck; provide a simple recovery action.
- The bike stays outside the temple interior and the underground encounter.
- No fuel, upgrades, bike combat or racing requirements yet.
- A basic mounting transition is fine if custom animation is unavailable.
- Her hands rest in the recessed fairing grips, her body leaning forward toward the forked front.
- Avoid rewarding collisions: workers and creatures are given way to. The creatures remain victims; 7's riding supports
  her nonlethal character.

SCENE VEN_B01 — LANDING: UNPACKING THE BIKE

Trigger: After VEN_C01, before 7 enters the outer gardens.  
Result: Bike available; objective "Ride to the Elarim temple." (VEN_S01_LANDED's objective, reworded).

The saucer lands on a broad platform beyond the inhabited terraces. The temple is visible in the distance, connected by
winding garden paths. (The cat has gone back aboard after C01; from here he is telepathic from the ship.)

Camera:
- Wide — saucer and landscape: 7 comes down in the beam (no ramp) and looks toward the distant temple.
- Low three-quarter on the bike: its broad front forks frame the rounded nose; the pointed tail extends behind the
  recessed saddle.
- Insert — her hand settling into one of the recessed grips molded into the fairing.
- Side medium — 7 mounting.

VEN_B01_01 | 7 — down the beam, looking out to the temple far across the gardens  
“The temple’s all the way over there. That’s a long walk.”

VEN_B01_02 | Cat, telepathic  
“You have a vehicle.”

She looks back toward the ship.

VEN_B01_03 | 7  
“I know.”

VEN_B01_04 | Cat, telepathic  
“I’m establishing that my advice is useful.”

The ship’s storage area opens, revealing the hover bike. **(Dan, 2026-11-18: make it elegant.)** A seamless panel on
the hull glows along its seam and slides / irises open with soft light from inside; the bike glides out on its hover
glow, turns slowly and settles beside her at riding height with a low hum — unhurried, no pop-in. Once it is out, the
hatch closes and the hull is seamless again (the saucer looks normal). VEN_B07 does the same in reverse.

Low three-quarter on the bike: 7 approaches and slides her hands into the recessed grips. The underside vents hum,
lifting the bike gently. (The cat comes down in the beam with her and stays visible by the ship for the rest of the scene.)

VEN_B01_05 | Cat, telepathic  
“I’ll watch the ship.”

7 starts to mount the hover bike, then pauses. **(Rewritten by Dan, 2026-11-19 — delivery directions are part of the
script: use them for the voice takes, e.g. ElevenLabs v3 tags, and the reactions.)**

VEN_B01_06 | 7 — a light, curious aside as she turns back to him from the bike  
“By the way, what should I call you?”

VEN_B01_07 | Panther, telepathic  
“Panther.”

She looks him up and down.

VEN_B01_08 | 7 — a small, surprised laugh  
“Panther?”

VEN_B01_09 | Panther, telepathic — dryly  
“Ahem. You could say it without looking down at me.”

VEN_B01_10 | 7 — teasing  
“I’d have to lie on the floor.”

**Wide two-shot (keep him fully in frame, large, the whole time).** Panther holds her gaze. Then he stretches — and grows
smoothly into a large, powerful cat.

7 takes a sharp breath and steps back, catching the bike’s fairing with one hand. Her gaze rises with him.

VEN_B01_11 | Panther, telepathic — perfectly composed  
“There. Saved you the trouble.”

VEN_B01_12 | 7 — breathless, followed by a disbelieving laugh  
“You can do that?”

VEN_B01_13 | Panther, telepathic  
“Apparently.”

She looks him over, taking in his enormous paws.

VEN_B01_14 | 7  
“Hang on. We crawled through a prison drain.”

VEN_B01_15 | Panther, telepathic — mildly  
“An excellent time to be small.”

She opens her mouth to argue, then gives a reluctant little chuckle.

VEN_B01_16 | 7  
“Mm. Fair point.”

Panther settles onto his haunches, looking thoroughly pleased with himself. She glances toward the saucer.

VEN_B01_17 | 7 — pointing at him, still amused  
“You’re not getting a bigger bowl.”

VEN_B01_18 | Panther, telepathic — with a thoughtful hum  
“Mmm. We can discuss that when you return.”

7 laughs softly and shakes her head as she settles into the saddle.

VEN_B01_19 | 7 — warmly  
“Look after the ship, Panther.”

He lies down beneath the saucer, still large. *(Dan's script says "beside the ramp" — the saucer has no ramp any more,
so he lies beside the ship / under its beam.)*

VEN_B01_20 | Panther, telepathic — quietly assured  
“I intend to.”

The hover bike rises with a low hum. She gives him one last look before riding toward the gardens. Return control to the
player, riding. (Panther stays large by the ship while she rides off; by VEN_B02 he is back inside at normal size, off
screen.)

**Panther changes size (Dan, 2026-11-18):** from a normal house cat up to elephant size. This scene is the first time
7 (and the player) sees it. Growth / shrinking is a smooth, continuous scale during a natural feline motion (a stretch),
never a pop or flash; the model and rig stay the same (one cat, uniformly scaled; his telepathic voice does not change).

**The cat's name reveal (Dan, 2026-11-18):** the cat is named **Panther**. From VEN_B01_07 on, his subtitle / speaker
label is **"Panther"** everywhere (all later Venus scenes and every later chapter; same ElevenLabs voice `Cat`). Lines
before the reveal (Chapter 1, VEN_C01, VEN_B01_02–05) keep the label "Cat". The script's "Cat" speaker in later scenes
(VEN_B02, B06, B07, C02, C05, C11) is displayed as "Panther".

SCENE VEN_B02 — FIRST RIDE: LET VENUS BE BEAUTIFUL

Delivery: In gameplay; normal riding camera; scenic framing along the route rather than taking control away.

The first stretch is broad, safe and gently curved. Teach steering, acceleration, braking and dismounting through
movement along the route. Give the player time to enjoy riding before any hazard. The route passes beneath hanging
gardens, beside flowing channels and through openings in ancient architecture; the temple appears at several points
ahead to help navigation. Optional pull-offs let the player stop, explore and meet Elarim workers.

At a long open bend:

VEN_B02_01 | 7  
“Are you seeing this?”

VEN_B02_02 | Cat, telepathic  
“I’m seeing the inside of your ship.”

VEN_B02_03 | 7  
“The gardens go all the way down.”

A short pause.

VEN_B02_04 | Cat, telepathic  
“Tell me when you get back.”

(This establishes that the cat does not see through her eyes; her descriptions become part of their companionship.)

SCENE VEN_B03 — THE DAMAGED ROUTE: DISCOVERING THE TROUBLE

Delivery: In gameplay. **Carries VEN_C02** (its trigger moves here; its lines are unchanged).

Ahead, workers signal for 7 to slow down. Water spills from a damaged channel across the route. **(Dan, 2026-11-19: the
tremor must be obvious.)** A strong tremor hits — deep rumble, camera shake, the bike wobbles, dust and stones fall, water
slops from the channel, workers stumble — and in front of her **the path cracks and collapses** into a gully; she brakes
hard. → VEN_C02_01 “Something’s wrong here.” / VEN_C02_02 “I felt that from here.”

The player reaches a clear stopping area before a collapsed walkway, with enough distance to brake comfortably.
Gameplay **(changed by Dan, 2026-11-18)**: dismount and inspect the gap. A long fallen slab / broken column lies beside
it; 7 uses **telekinesis** to lift it and lay it across the hole as a bridge, gets back on the bike and **rides across**.
The bridge stays for return trips. Not a surprise death trap and not a forced jump tutorial. (The garden wall beside the
road stays climbable anywhere for exploring, but the bridge is the way through.)

SCENE VEN_B04 — TEMPLE ARRIVAL: A NATURAL TRANSITION TO WALKING

The riding path ends at an outer court; the temple entrance lies beyond a pedestrian garden. 7 parks the bike beside the
court.

Camera: Wide — arrival. She dismounts; the bike settles into a low idle hover as the guardian approaches.

Continue with VEN_C03 (“What brings you here?” …) and VEN_C04 as written.

SCENE VEN_B05 — THE LOWER GARDENS: RIDING TOWARD THE SOURCE

Trigger: After 7 agrees to help (end of VEN_C04).

VEN_B05_01 | Guardian  
“The lower gardens have a maintenance path. Your vehicle can take you as far as the root chambers.”

VEN_B05_02 | 7  
“And after that?”

VEN_B05_03 | Guardian  
“You’ll have to continue on foot.”

The player returns to the parked bike. The second ride tests the basics: narrower passages with room to brake and turn;
damaged sections that need an alternate route; slow-moving workers and creatures to give way to; safe observation points
overlooking the affected gardens. At the observation area she parks and watches the creatures respond to the signal →
**VEN_C05** as written. She continues on foot into the control chamber; the bike remains outside.

SCENE VEN_B06 — RETURN RIDE: SHOW WHAT CHANGED

Trigger: After VEN_C07 (Wormmaster subdued), when 7 returns to her bike.

Ride back through part of the same landscape. It now shows the outcome: the control pulses have stopped; creatures move
freely instead of converging on the foundations; workers return to the paths; water flows through a repaired channel;
damage is still present. More relaxed than the outward trip.

VEN_B06_01 | Cat, telepathic  
“How’s the garden?”

VEN_B06_02 | 7  
“Still standing.”

VEN_B06_03 | Cat, telepathic  
“And its unwelcome guest?”

VEN_B06_04 | 7  
“Staying put.”

She passes a creature moving calmly beneath the roots.

VEN_B06_05 | 7  
“The others are going home.”

Allow the rest of the ride to breathe. At the temple, park again → VEN_C08, VEN_C09, VEN_C10 as written.

SCENE VEN_B07 — BACK TO THE SAUCER: LET THE MEMORY SETTLE

Trigger: After VEN_C12 (the two locations — VEN_C10 → VEN_C12 → leave the temple). Keep it quiet: a direct route or a shortcut opened during the chapter, so it
is not repetitive travel.

Near the ship:

VEN_B07_01 | Cat, telepathic  
“Did they have answers?”

VEN_B07_02 | 7  
“Some.”

A pause.

VEN_B07_03 | 7  
“I’ll tell you inside.”

She parks the bike in the saucer’s storage area; its hum fades. Continue with **VEN_C11** (the saucer debrief), beginning
with the cat: “Did they know what it was?” — the dodecahedron and the cube are placed together on the table.

SCENE VEN_C01 — ARRIVAL

Trigger: First landing on Venus.  
Replay: Once automatically; optional replay only if supported.  
Result: Set VEN_S01_LANDED.

Camera:
- Extreme wide of saucer emerging beneath clouds.
- Over 7’s shoulder toward the gardens.
- Medium two-shot of 7 and Cat.
- Landscape insert.
- Cat head tracking flying creatures.

VEN_C01_01 | 7  
“Someone still lives here.”

VEN_C01_02 | Cat, telepathic  
“You sound surprised.”

VEN_C01_03 | 7 — quietly, the view taking her in  
“After all that dust and machinery… I’d forgotten a place could look like this.”

Pause on the view. *(QF43 6: “It’s beautiful.” cut; “Land somewhere with a roof.” / “Why?” / “Experience.” replaced.)*

VEN_C01_05 | Cat, telepathic — his head tracking the flying creatures, tail twitching  
“Hm.”

VEN_C01_06 | 7  
“Don’t even think about it.”

VEN_C01_07 | Cat, telepathic  
“I was admiring the wildlife.”

VEN_C01_08 | 7 — amused  
“Mm-hmm.”

Return control at the landing platform.

SCENE VEN_C02 — GARDEN TREMOR

Trigger: First approach to the damaged outer walkway — **now during the ride, see VEN_B03**.  
Delivery: In gameplay; do not lock player movement.

A tremor passes through the area. Water shakes in a vessel; a worker steadies it.

VEN_C02_01 | 7  
“Something’s wrong here.”

VEN_C02_02 | Cat, telepathic  
“I felt that from here.”

The player follows an alternate route to the temple. This route must be usable with existing movement abilities.

SCENE VEN_C03 — MEET THE GUARDIAN

Trigger: Interact with guardian before help is accepted.  
Result: Set VEN_S02_GUARDIAN_MET and guide the player toward the cube chamber.

Camera:
- Wide two-shot at temple entrance.
- Insert of dodecahedron.
- Over 7’s shoulder toward guardian.
- Close view of object between them.

VEN_C03_01 | Guardian  
“What brings you here?”

VEN_C03_02 | 7  
“I found this on the Moon. Someone recognized your marking.”

VEN_C03_03 | Guardian  
“May I see it?”

7 holds it out. Guardian examines it without removing it from inventory.

VEN_C03_04 | Guardian  
“Where on the Moon?”

VEN_C03_05 | 7  
“Beneath the city. In the old passages.”

VEN_C03_06 | 7  
“When I touched it, I saw others. A place as it used to be.”

VEN_C03_07 | Guardian  
“What did they show you?”

VEN_C03_08 | 7  
“A door opening.”

Brief pause.

VEN_C03_09 | 7  
“It sounds small when I say it.”

VEN_C03_10 | Guardian  
“Most of a life is small things.”

He turns toward the temple doors and walks ahead. **(Dan, 2026-11-19.)**

VEN_C03_11 | Guardian  
“Follow me.”

He leads her through the temple to the cube chamber (he walks ahead, waits for her if she falls behind).

SCENE VEN_C04 — CUBE AND INTERRUPTION

Trigger: Reach the cube chamber after VEN_C03.  
Result: Set VEN_S03_HELP_ACCEPTED.

Camera:
- Wide chamber.
- Over 7’s shoulder toward cube.
- Matching inserts of related markings.
- Brief eye close-up.
- Falling dust insert when tremor begins.

VEN_C04_01 | 7  
“The same marking.”

VEN_C04_02 | Guardian  
“Our histories were not always kept apart.”

VEN_C04_03 | 7  
“Is this one like mine?”

VEN_C04_04 | Guardian  
“This holds our record. Yours belongs to the Moon.”

VEN_C04_05 | 7  
“Then why did it show me something that felt like a memory?”

VEN_C04_06 | Guardian  
“Let us see what ours shows you.”

Tremor interrupts before contact with the cube. Attendant arrives.

VEN_C04_07 | Attendant  
“The lower channel has collapsed.”

VEN_C04_08 | Guardian  
“Move everyone off that terrace.”

VEN_C04_09 | 7  
“What’s causing it?”

VEN_C04_10 | Guardian  
“Someone beneath the gardens. He is forcing the burrowing creatures through our foundations.”

VEN_C04_11 | 7  
“Why?”

VEN_C04_12 | Guardian  
“He wants us to surrender the temple.”

VEN_C04_13 | Guardian  
“We cannot keep repairing the ground while he tears it away.”

7 puts the dodecahedron away.

*(QF43 12: she turns from the Guardian toward the gardens on the line; her face framed.)*

VEN_C04_14 | 7 — determined, resolute, quietly fierce  
“Then I’ll find him.”

VEN_C04_15 | Guardian  
“The creatures are being driven. Please remember that.”

VEN_C04_16 | 7  
“I will.”

Return control. Make the route to the lower gardens available.

SCENE VEN_C05 — DISCOVER THE SIGNAL

Trigger: Reach the creature observation area.  
Delivery: In gameplay.

*(QF43 13: the control devices pulse every ~9 s - a deep throbbing boom, a small tremor, a ripple of light from the device, the
creatures surging the same way. At least one full pulse plays as she reaches the overlook, before the first line.)*

VEN_C05_00 | 7  
“Hear that?”

VEN_C05_00b | Cat, telepathic  
“Felt it.”

VEN_C05_01 | 7  
“They aren’t attacking the people.”

VEN_C05_02 | Cat, telepathic  
“What are they doing?”

On the next pulse, as the creatures surge:

VEN_C05_03 | 7  
“Following something. Every time that pulse comes, they move.”

*(Framed on the nearest device glowing with the pulse.)*

VEN_C05_04 | 7  
“There.”

VEN_C05_05 | Cat, telepathic  
“Then find what’s calling them.”

Result: Set VEN_S04_CONTROL_DISCOVERED.

*(QF43 14: the way down. The root gate below the lookout stays shut - jammed from below. She goes round it down the root wall beside
it: a hang off the edge and a shimmy, the shelf, the root chasm jumped between pulses, a hang-and-drop into the burrows, a fallen slab
lifted into place with telekinesis as a step, then the gate's inner release in the pit - the gate opens behind her.)*

VEN_C05_P1 | Cat, telepathic  
“That’s not opening from this side.”

VEN_C05_P2 | 7  
“The root wall beside it goes down. I’ll go around.”

On the shelf, the hanging roots over the chasm thrashing with the pulse:

VEN_C05_P3 | Cat, telepathic  
“Wait for the next one… now.”

The gate sinks open at the top of the pit:

VEN_C05_P4 | Cat, telepathic  
“Good. Now you have a way back.”

SCENE VEN_C06 — WORMMASTER INTRODUCTION

Trigger: Enter control chamber.  
Result: Set VEN_S05_ENCOUNTER_ACTIVE.

Camera:
- Low-angle view of Wormmaster above machinery.
- Underlighting in grayscale.
- Over his shoulder toward 7.
- Level medium shot for 7’s replies.

*(QF43 17: as she enters, before Wormmaster speaks, the camera finds the sunken ring in the floor with the old Elarim pylons.)*

VEN_C06_00a | Cat, telepathic  
“That ring in the floor. It looks like an old holding cell. The Elarim must have used it for the creatures.”

VEN_C06_00b | 7  
“Big enough for him.”

VEN_C06_00c | Cat, telepathic  
“Keep that in mind.”

VEN_C06_01 | Wormmaster  
“They sent someone down at last.”

VEN_C06_02 | 7  
“You’re bringing their gardens down.”

VEN_C06_03 | Wormmaster  
“They can leave.”

VEN_C06_04 | 7  
“It’s their home.”

VEN_C06_05 | Wormmaster  
“Then they should have been more willing to negotiate.”

He activates a signal pulse.

VEN_C06_06 | 7  
“Turn it off.”

VEN_C06_07 | Wormmaster  
“Come and do it yourself.”

Return control.

*(QF43 16/17: he stays up on his perch defending the three devices, any order; only when the third is off does his platform sink and he
comes down. When he is beaten and stunned:)*

VEN_C06_P1 | Cat, telepathic  
“The holding cell. Get him into it.”

*(Prompt "Lift him into the cell" (RT): she lifts him with telekinesis, carries him through the air and sets him in the ring; the pylons
light and a field of light rises on every side and closes into a dome over him.)*

SCENE VEN_C07 — THREAT STOPPED

Trigger: All control devices disabled and Wormmaster contained.  
Result: Set VEN_S06_THREAT_STOPPED.

Use level camera angles now. Remove his visual dominance.

VEN_C07_01 | Wormmaster  
“You don’t know what you’re interfering with.”

VEN_C07_02 | 7  
“I know what was standing above it.”

Creatures withdraw safely. Machinery becomes silent.

SCENE VEN_C08 — RETURN TO GUARDIAN

Trigger: Interact with guardian after VEN_S06.

VEN_C08_01 | Guardian  
“The ground is quiet.”

VEN_C08_02 | 7  
“It will need repairs.”

VEN_C08_03 | Guardian  
“Repairs we can make.”

VEN_C08_04 | Guardian  
“You came with a question.”

Move or guide the player to the cube.

SCENE VEN_C09 — RECORD AND PERSONAL MEMORY

Trigger: Interact with cube after VEN_C08.

Camera:
- Over shoulder toward cube.
- Hand approaches and touches.
- Full-screen vision.
- Match cut from the vision’s open palm to 7’s empty palm.
- Eye close-up, gaze lowered.
- Medium two-shot for the unanswered question.

Vision content **(Dan's video, 2026-11-18: `Art/Video/vision_02_venus.mp4`, 12 s, grayscale, no audio)**: the first
planting, seen through 7's eyes in a bright fog. A robed figure kneels beside a newly planted sapling in fresh earth and
holds out an open palm with a seed in it — toward 7, waiting for her to take it. Behind, another figure walks past
carrying a water jar; grown arches of Elarim architecture (still being built) stand in the haze. The spec's match cut
(the vision's open palm → 7's empty palm) uses the kneeling figure's offered palm. No interactive prompts during the
vision.

**Dialogue after the vision (updated by Dan, 2026-11-18 — replaces the old C09 lines):**

VEN_C09_01 | 7  
“That tree… it was barely up to my knee.”

VEN_C09_02 | Guardian  
“You saw the first planting.”

VEN_C09_03 | 7  
“Someone was holding a seed. They were waiting for me to take it.”

A pause.

VEN_C09_04 | Guardian  
“The planting is in our record. That moment is not.”

VEN_C09_05 | 7  
“I think I knew them.”

Hold briefly. Avoid a frightened reaction.

SCENE VEN_C10 — RECEIVE THE RECORD

Trigger: Immediately after VEN_C09.
Result: Award Venus record once; set VEN_S07_RECORD_RECEIVED.

*(QF43 20: like every other rune, she takes the cube she touched: after the vision the cage opens and she lifts the cube out. On
revisits the cage stands empty and open, a soft glow where the cube sat.)*

VEN_C10_01 | Guardian
“Take it with you.”

VEN_C10_02 | 7
“It belongs to your temple.”

Guardian looks toward the gardens.

VEN_C10_03 | Guardian
“You saw how close he came.”

VEN_C10_04 | Guardian
“If this place were lost, I would want someone to carry it. And to know we had been here.”

Insert: 7 secures the cube beside the dodecahedron.

Return control.

SCENE VEN_C12 — TWO LOCATIONS (Dan, 2026-11-19)

Placement: right after VEN_C10 (the guardian gives 7 the cube) and before she leaves the temple — i.e. before VEN_B07.  
Result: **Mars and Europa become available in the saucer's navigation** (the star map).

7 secures the cube beside the dodecahedron, then pauses.

VEN_C12_01 | 7  
“These came from different worlds. Were your temples connected?”

VEN_C12_02 | Guardian  
“Yes. We exchanged records. Each temple kept something the others could not.”

VEN_C12_03 | 7  
“Do you know where any of the others are?”

The guardian raises a hand; the temple's record projects two glowing places in the air above the empty cage (QF43 21): a red-grey planet with a canyon, an icy moon, each lighting as it is named.

VEN_C12_04 | Guardian  
“Two locations survive in our records. Mars—and Europa.”

**Over 7’s shoulder — the display.** She studies Mars first.

VEN_C12_05 | 7  
“What will I find on Mars?”

VEN_C12_06 | Guardian  
“A temple among the divided kingdoms. It stood before their borders were drawn.”

VEN_C12_07 | 7 — a quiet sigh  
“And now it’s on someone’s side of a border.”

VEN_C12_08 | Guardian — gently  
“Almost certainly.”

Her attention shifts to Europa.

VEN_C12_09 | 7  
“And this one?”

VEN_C12_10 | Guardian  
“Beneath Europa’s ice. The Cetaceans were its keepers.”

VEN_C12_11 | 7  
“Were?”

The guardian pauses.

VEN_C12_12 | Guardian  
“We have not heard from either temple in a long time.”

VEN_C12_13 | 7  
“So we don’t know whether they’re still there.”

VEN_C12_14 | Guardian  
“No. Only where to begin looking.”

7 considers the two locations.

VEN_C12_15 | 7  
“Can you send these to my ship?”

VEN_C12_16 | Guardian  
“Of course.”

A soft tone confirms the transfer.

VEN_C12_17 | Panther, telepathic (to 7)  
“Received. Two destinations.”

VEN_C12_18 | 7, telepathic (to Panther)  
“We’ll look at them together when I get back.”

She turns to the guardian.

VEN_C12_19 | 7 — warmly  
“Thank you. For trusting me with this.”

Her hand rests briefly over the secured cube.

VEN_C12_20 | Guardian  
“Thank you for giving us the chance to keep it.”

7 inclines her head and leaves. Gameplay: Mars and Europa become available in the saucer’s navigation.

SCENE VEN_C11 — SAUCER DEBRIEF

Trigger: Enter saucer after receiving record (after VEN_B07, the ride back).
Result: Set VEN_S08_COMPLETE; enable normal destination selection.

Camera:

Cat near entrance.

Follow 7 to table.

Insert of both geometric objects.

Table-height two-shot.

Open hand insert.

Wide two-shot for final pause.

VEN_C11_01 | Cat, telepathic
“Did they know what it was?”

VEN_C11_02 | 7
“The Moon’s record. Venus has its own.”

VEN_C11_03 | Cat, telepathic
“And the others you saw?”

VEN_C11_04 | 7
“They’re less certain about that.”

VEN_C11_05 | 7
“There was something else. Something the guardian said wasn’t in their record.”

VEN_C11_06 | Cat, telepathic
“What did you see?”

VEN_C11_07 | 7
“Someone giving me a seed.”

Pause.

VEN_C11_08 | 7
“I wish I could remember their face.”

Cat settles closer. Hold the silence.

VEN_C11_09 | 7
“There are other temples.”

VEN_C11_10 | Cat, telepathic
“Then we have somewhere else to ask.”

→ Continue straight into **VEN_C13** (choosing the next world). Do not invent new destination lore beyond what VEN_C12 / C13 say.

SCENE VEN_C13 — CHOOSING THE NEXT WORLD (Dan, 2026-11-19)

Placement: INT. SAUCER — NAVIGATION AREA, straight after VEN_C11 (their conversation about the planting vision).  
Result: the player chooses Mars or Europa; course set; **the other destination remains available**.

7 sits in the pilot’s chair with Panther curled in her lap. One hand rests on his back; the other operates the
navigation controls. The two locations supplied by the Elarim appear ahead of them. Panther’s dialogue is telepathic.

VEN_C13_01 | Panther, telepathic  
“The guardian’s locations. Mars and Europa.”

**Over 7’s shoulder — the display.** She brings Mars closer, absentmindedly stroking Panther.

VEN_C13_02 | 7  
“A temple between divided kingdoms.”

VEN_C13_03 | Panther, telepathic — dryly  
“Convenient. Everyone can argue over it without travelling far.”

VEN_C13_04 | 7 — a small chuckle  
“Perhaps we’ll arrive on a quiet day.”

Panther lifts his head to look up at her.

VEN_C13_05 | Panther, telepathic  
“Mm.”

VEN_C13_06 | 7  
“Such confidence.”

She shifts the display to Europa.

VEN_C13_07 | 7  
“The Cetaceans’ temple is beneath the ice.”

VEN_C13_08 | Panther, telepathic  
“Underwater.”

VEN_C13_09 | 7  
“Yes.”

VEN_C13_10 | Panther, telepathic  
“I thought that deserved saying separately.”

VEN_C13_11 | 7 — teasing  
“You’ll be perfectly dry in here.”

He settles deeper into her lap.

VEN_C13_12 | Panther, telepathic  
“I intend to maintain that arrangement.”

**Medium side shot — both in the chair.** 7 grows quiet, studying the destinations. Her hand comes to rest.

VEN_C13_13 | 7  
“The guardian doesn’t know whether either temple is still there.”

VEN_C13_14 | Panther, telepathic — gently  
“No.”

VEN_C13_15 | 7  
“We could go all that way and find nothing.”

Panther nudges his head beneath her hand. She resumes stroking him.

VEN_C13_16 | Panther, telepathic  
“We found something here.”

A quiet pause.

VEN_C13_17 | 7  
“More questions.”

VEN_C13_18 | Panther, telepathic  
“Those too.”

She releases a soft breath and looks down at him.

VEN_C13_19 | 7  
“All right. Where would you go?”

VEN_C13_20 | Panther, telepathic  
“You’re asking a cat to choose between water and dry land.”

VEN_C13_21 | 7 — laughing softly  
“Fair point.”

VEN_C13_22 | Panther, telepathic — warmer  
“Either way, I’m coming with you.”

Her hand pauses briefly against his fur, then continues.

VEN_C13_23 | 7 — quietly  
“Good.”

Hold the two-shot before opening the choice.

**PLAYER CHOICE**
- **Mars** — Seek the temple among the divided kingdoms.
- **Europa** — Find the Cetacean temple beneath the ice.

If the player chooses **Mars**:

VEN_C13_M1 | 7  
“Mars. Let’s start there.”

VEN_C13_M2 | Panther, telepathic  
“Dry land. An excellent feature in a destination.”

VEN_C13_M3 | 7 — amused  
“I chose it for the temple.”

VEN_C13_M4 | Panther, telepathic  
“Naturally.”

If the player chooses **Europa**:

VEN_C13_E1 | 7  
“Europa. I want to see what’s beneath that ice.”

VEN_C13_E2 | Panther, telepathic — a small sigh  
“Of course you do.”

VEN_C13_E3 | 7  
“You said either way.”

VEN_C13_E4 | Panther, telepathic — affectionately dry  
“I did. A reckless moment of generosity.”

She chuckles and sets the course.

**Close insert — Panther in her lap.** A quiet purr joins the ship’s hum.

**Wide from behind the chair.** The chosen world fills the navigation display. 7 rests back in the chair, Panther
comfortable in her lap, as the saucer begins its journey. The other destination remains available.

*(Implementation note: Mars and Europa chapters don't exist yet — after the course is set and the journey begins, show
a "Chapter 3 — Mars / Europa (coming soon)" card and return to free exploration aboard; both stay selectable on the
star map.)*

7. ENCOUNTER PROTOTYPE

Build an objective-based nonlethal encounter.

Three control devices emit repeated signals.

Creatures follow clearly visible routes associated with active devices.

Telegraph creature movement using sound, ground disturbance, and animation.

Give the player safe observation and traversal spaces.

Devices can be disabled using the existing interaction system.

Disabling each device removes one source of control.

Once all devices are disabled, creatures withdraw.

A final interaction closes a containment barrier around Wormmaster.

Only containment completes the encounter.

No creature death is required, rewarded, or enabled by the encounter objectives.

Use existing abilities where supported. Do not introduce a new power as canon merely to solve this encounter.

Use the existing game’s failure/retry behavior. Do not invent a new explanation for 7’s immortality.

8. PRESENTATION AND INTERRUPTIONS

Dialogue scenes must restore player control and the gameplay camera on completion, skip, or interruption.

Skipping a scene applies its intended progression result exactly once.

Do not run two conversations simultaneously.

Queue or defer Cat’s ambient dialogue during important scenes.

Provide subtitles for telepathic dialogue with a consistent speaker label.

Avoid requiring precise lip sync for the prototype.

Visions must support skip and unavailable-video fallback.

Treat scene completion as a committed checkpoint; interrupted scenes may restart from their beginning.

Save encounter progress at each disabled device and after containment.

9. RETURN VISITS

Before the threat is stopped, the gardens remain damaged and under pressure.

After the threat is stopped:

Signal pulses cease.

Creatures no longer follow hostile control routes.

Repair activity continues.

Water returns in selected channels.

Damage remains visible.

After record collection:

The original cube stays in the temple.

The guardian does not repeat the reward scene.

The player can leave and revisit without losing progress.

10. BUILD ORDER

Inspect and reuse existing project systems.

Create connected graybox areas.

Implement stages, objectives, and persistence.

Add interaction triggers and dialogue.

Make the creature-control encounter completable.

Add record collection and saucer return.

Add camera cues and vision placeholder.

Add environmental state changes and sound.

Replace placeholders with approved art and video.

Prioritize a complete playable path before cinematic polish.

11. ACCEPTANCE CHECKS

The prototype is ready for review when:

A player can land, meet the guardian, investigate, stop the threat, receive the record, and return to navigation.

No required objective involves killing.

The chapter works without facial animation or voice recordings.

Save/reload preserves disabled devices, containment, and record ownership.

Skipping cinematics produces the same progression as watching them.

Reloading or revisiting cannot duplicate the reward.

The original Venus cube remains in the temple.

The Moon dodecahedron is never described as belonging to the Elarim.

Cat offers observations without revealing knowledge he should not possess.

Missing vision media does not block completion.

Completing Venus does not force the remaining worlds into a fixed sequence.

No unsupported powers, final villain motives, or explanations of 7’s identity have been silently added.
