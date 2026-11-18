> **Status (2026-11-17):** spec received from Dan. **The file he sent was cut off** at scene VEN_C10 ("Insert:") — the rest (end of VEN_C10, the saucer return conversation, destination selection, and anything after) is still to come. Do not invent those parts.

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
Saucer entrance, arrival vista, route into the gardens.

VEN_02 — Outer gardens  
Inhabited terraces, water channels, signs of damage, a blocked main walkway and traversable alternate route.

VEN_03 — Temple  
Guardian interaction area and protected cube chamber.

VEN_04 — Lower gardens  
Damaged channels, creature observation area, route to underground infrastructure.

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

Award the cube record exactly once. Reentering the temple or reloading must not duplicate it.

**6. SCENE AND DIALOGUE DEFINITIONS**

Each dialogue line needs a stable ID, speaker, text, delivery type, and optional camera cue.

Camera cues are presentation instructions, not gameplay requirements. If a cue cannot run, the scene must still progress.

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

VEN_C01_03 | 7  
“After the Moon…”

Pause on the view.

VEN_C01_04 | 7  
“It’s beautiful.”

VEN_C01_05 | Cat, telepathic  
“Land somewhere with a roof.”

VEN_C01_06 | 7  
“Why?”

VEN_C01_07 | Cat, telepathic  
“Experience.”

Return control at the landing platform.

SCENE VEN_C02 — GARDEN TREMOR

Trigger: First approach to the damaged outer walkway.  
Delivery: In gameplay; do not lock player movement.

A tremor passes through the area. Water shakes in a vessel; a worker steadies it.

VEN_C02_01 | 7  
“Something’s wrong here.”

VEN_C02_02 | Cat, telepathic  
“I felt that through the landing supports.”

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
“When I touched it, I saw people. A place as it used to be.”

VEN_C03_07 | Guardian  
“What did they show you?”

VEN_C03_08 | 7  
“A door opening.”

Brief pause.

VEN_C03_09 | 7  
“It sounds small when I say it.”

VEN_C03_10 | Guardian  
“Most of a life is small things.”

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

VEN_C04_14 | 7  
“Then I’ll find him.”

VEN_C04_15 | Guardian  
“The creatures are being driven. Please remember that.”

VEN_C04_16 | 7  
“I will.”

Return control. Make the route to the lower gardens available.

SCENE VEN_C05 — DISCOVER THE SIGNAL

Trigger: Reach the creature observation area.  
Delivery: In gameplay.

VEN_C05_01 | 7  
“They aren’t attacking the people.”

VEN_C05_02 | Cat, telepathic  
“What are they doing?”

VEN_C05_03 | 7  
“Following something. Every time that sound starts, they move.”

Play a signal pulse and visibly change the creatures’ direction.

VEN_C05_04 | 7  
“There.”

VEN_C05_05 | Cat, telepathic  
“Then find what’s calling them.”

Result: Set VEN_S04_CONTROL_DISCOVERED.

SCENE VEN_C06 — WORMMASTER INTRODUCTION

Trigger: Enter control chamber.  
Result: Set VEN_S05_ENCOUNTER_ACTIVE.

Camera:
- Low-angle view of Wormmaster above machinery.
- Underlighting in grayscale.
- Over his shoulder toward 7.
- Level medium shot for 7’s replies.

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

Vision content:
1. Water entering a newly completed channel.
2. Hands supporting a young tree.
3. People gathering beneath branches.
4. An Elarim hand placing a seed into an open palm.

Use a replaceable video asset. Until available, use clearly labeled development placeholders or stills. Keep all imagery within the past. No interactive prompts during the vision.

VEN_C09_01 | Guardian  
“What did you see?”

VEN_C09_02 | 7  
“The gardens. Before the towers were finished.”

VEN_C09_03 | 7  
“And someone gave me something.”

VEN_C09_04 | Guardian  
“What?”

VEN_C09_05 | 7  
“A seed.”

Pause on her open hand.

VEN_C09_06 | Guardian  
“That is not part of the record I opened.”

7 raises her gaze.

VEN_C09_07 | 7  
“Then where did it come from?”

VEN_C09_08 | Guardian  
“I don’t know.”

Hold briefly. Avoid a frightened reaction.

SCENE VEN_C10 — RECEIVE THE RECORD

Trigger: Immediately after VEN_C09.  
Result: Award Venus record once; set VEN_S07_RECORD_RECEIVED.

The guardian offers a small cube. The original remains visible in its protected structure.

VEN_C10_01 | Guardian  
“Take this record with you.”

VEN_C10_02 | 7  
“You still have it here?”

VEN_C10_03 | Guardian  
“Yes.”

VEN_C10_04 | 7  
“Then why give me another?”

Guardian looks toward the gardens.

VEN_C10_05 | Guardian  
“You saw how close he came.”

VEN_C10_06 | Guardian  
“If this place were lost, I would want someone to know we had been here.”

Insert: 

*(— file ends here: truncated —)*
