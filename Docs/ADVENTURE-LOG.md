# Adventure log (ship console)

The ship console's **Log**: one short entry per chapter beat 7 has completed, in story order, no dates. Written only
from what happens in the game (`Docs/DIALOGUE.md`, `Docs/CHAPTER2_VENUS.md`). The game reads
`Unity/Assets/Resources/Console/adventure_log.json`; this page mirrors it. To change an entry, edit its `title` / `text`
in the json (and here); to remove one, delete it; to add one, add a line with a new `id` where it belongs in the order.

**Unlock forms:** `ch1>=N` = Chapter 1 checkpoint N reached (0 cell · 1 escape · 2 old passage + ruins · 3 Scree guard ·
4 vision · 5 lunar door + take the shape · 6 city reveal · 7 saucer rescue + beam · 8 aboard · 9 galley · 10 free
exploration; something finished at checkpoint N unlocks at N+1 or later) · `venus>=<Stage>` (Landed, GuardianMet,
HelpAccepted, ControlDiscovered, EncounterActive, ThreatStopped, RecordReceived, Complete) · `scene:<VEN id>` (that Venus
scene completed).

## Chapter 1 — The Moon Prison

### The cat at the cell
`ch1_cell` · unlock `ch1>=1`

Held in a cell beneath the Moon, 7 met a black cat who spoke inside her head. His people had sent him to rescue her. He opened the cell door.

### The older wall
`ch1_escape` · unlock `ch1>=2`

He did not know why she was being held, and his people had not told him why they wanted her out. The guarded way was closed to them, but one wall was older than the prison. They followed it.

### Past the Scree guard
`ch1_scree` · unlock `ch1>=4`

A Scree guard stood in their way. 7 got them past without hurting anyone.

### The door and the shape
`ch1_vision` · unlock `ch1>=6`

Touching a stone shape in the ruins, 7 saw the same doorway whole, long ago: two figures opened it and walked through. She took the shape with her. When she had uncovered the buried lock, she opened the door the way she had seen them do it.

### The terrace
`ch1_fight` · unlock `ch1>=7`

Two Scree blocked the way out of the base. 7 knocked them down; they sat up again afterwards.

### The city inside the Moon
`ch1_city` · unlock `ch1>=7`

Outside, a city filled the Moon’s inner surface, lit windows and traffic curving up overhead. Below the towers, mining machinery was cutting into much older buildings.

### The ship
`ch1_saucer` · unlock `ch1>=9`

7 found her saucer in the impound below the terrace. They released it and went aboard. The cat found it was considerably more ship than he had expected.

### Dinner in the galley
`ch1_galley` · unlock `ch1>=10`

Free-range chicken for the cat. 7 said the vision felt like remembering. The cat recognized an Elarim marking on the shape, and he chose to stay. Next: Venus.

## Chapter 2 — Venus

### Landing, and a name
`ven_landing` · unlock `scene:VEN_B01`

The saucer set down on Venus, where people still live among the gardens. The cat gave his name, Panther, and then grew to the size of a large and powerful cat. He stayed to watch the ship while 7 rode her hover bike toward the temple.

### The broken path
`ven_ride` · unlock `scene:VEN_B03`

The ride ran under hanging gardens and beside water channels. Then a strong tremor broke the path in front of her. 7 laid a fallen slab across the gap and rode on.

### The guardian
`ven_guardian` · unlock `venus>=GuardianMet`

At the temple, 7 showed the guardian the shape from the Moon and told the guardian about the door she had seen. The guardian listened.

### The cube chamber
`ven_cube` · unlock `venus>=HelpAccepted`

The temple’s cube bears the same marking; it holds the Elarim’s record. Before she could touch it, another tremor hit. Someone beneath the gardens was forcing the burrowing creatures through the foundations to make the Elarim give up the temple. 7 said she would find him.

### Beneath the gardens
`ven_signal` · unlock `venus>=ControlDiscovered`

7 rode the maintenance path to the lower gardens. The creatures were not attacking anyone. Every time a signal sounded, they moved.

### The Wormmaster
`ven_wormmaster` · unlock `venus>=EncounterActive`

The signal came from a chamber full of machines below the gardens. The Wormmaster said the Elarim could leave; it was their home, she told him. Three devices were driving the creatures.

### The ground is quiet
`ven_contained` · unlock `venus>=ThreatStopped`

7 shut down the three devices and closed a barrier around the Wormmaster. The creatures went home unharmed. On the ride back, workers were on the paths again and water ran in a repaired channel.

### The record
`ven_record` · unlock `venus>=RecordReceived`

At the cube, 7 saw the first planting on Venus, and someone holding out a seed for her to take. The guardian said that moment is not in their record. The cage opened and she took the Elarim’s cube with her: he wanted someone to carry it, and to know they had been there.

### Back aboard
`ven_aboard` · unlock `venus>=Complete`

7 told Panther what she had seen. She wishes she could remember the face. There are other temples to ask.

Notes: the terrace fight and the city reveal both happen within checkpoint 6, so both unlock at `ch1>=7`. The ship
entry covers the rescue, the beam and the larger interior (checkpoints 7-8) and unlocks at `ch1>=9`.


## Round 14 — Europa entries (Log, mirror of `Resources/Console/adventure_log.json`)

- **Beneath the ice** (`eur_dive`, unlock `europa>=2`): The saucer found a passage through Europa's ice. 7 rode the hover bike down into an ocean, and something enormous passed beneath her.
- **The keeper** (`eur_keeper`, unlock `europa>=3`): A Cetacean guide led her to a settlement built around open swimming passages. Its keeper spoke with her - and a mechanical pulse interrupted them.
- **Tunneler again** (`eur_tunneler`, unlock `europa>=4`): Tunneler's drills were closing the migration routes; he had brought Scree workers who could not leave. 7 went into his underwater base.
- **All of them** (`eur_workers`, unlock `europa>=6`): She opened the holding area. The workers opened doors, showed her the way, and every transport left before the machinery was destroyed.
- **Out** (`eur_escape`, unlock `europa>=7`): Tunneler escaped in his own craft. The base came down behind her as she raced out into open water - and then the calls could be heard again.
- **One voice teaching another** (`eur_record`, unlock `europa>=8`): In the temple she saw a young one learning a call, and knew the answer before it came. The keeper gave her Europa's record, a small icosahedron.
- **An ordinary sound** (`eur_home`, unlock `europa>=9`): Back aboard, in the pilot's chair with Panther in her lap: he purred, and that was one sound she knew.

## Round 15 — Mars entries (Log, mirror of `Resources/Console/adventure_log.json`)

- **Lines across a world** (`mar_arrival`, unlock `mars>=1`): Two fortress cities either side of a ravine, and between them a temple older than every wall round it. Both approaches were barricaded.
- **A small problem with real people** (`mar_crossing`, unlock `mars>=2`): At the checkpoint, water filters waited behind a closed barrier. Viper looked down from his platform - and noticed the records.
- **The old maintenance route** (`mar_route`, unlock `mars>=3`): Under the main crossing an old route still ran. The supplies went through; on the far bank the caretaker had heard a different story.
- **The shared door** (`mar_door`, unlock `mars>=4`): Two controls, one on each bank. Both sides have to open it. Or they used to trust each other.
- **Two controls** (`mar_controls`, unlock `mars>=5`): The worker on one bank, the caretaker on the other, at the same moment. The old bridge aligned.
- **Viper's intervention** (`mar_viper`, unlock `mars>=6`): He divided the crossing with barriers and moving sections. He is secured now - what happens to him is for the people here to decide.
- **History that belongs to both** (`mar_temple`, unlock `mars>=7`): Their two symbols were halves of one design. In the vision both banks built the bridge together. The temple's records show a route out from Mars - and it stops.
- **What she remembers** (`mar_home`, unlock `mars>=9`): They were both so certain. What if she is doing the same? Keep asking, Panther said. He would mention it.


## Chapter — The Asteroid: the Great Machine Age (Round 16)

- **Something on the rock** (`ast_arrival`, unlock `asteroid>=1`): Silver structures through dark rock, rings turning round an opening, small white lights. Something out here is still working.
- **Receiving** (`ast_receiving`, unlock `asteroid>=2`): A scanner called me unregistered material and tried to send me to sorting. It preferred a marked crate in the end. Panther found the service entrance.
- **The city that still works** (`ast_city`, unlock `asteroid>=3`): Factory terraces up the walls, rail carriers between them, machines repeating their work. Empty stations. Nobody at all.
- **Request denied** (`ast_machinist`, unlock `asteroid>=4`): Its caretaker keeps the city ready for its people. He is cutting up the archive to do it. Restoration is ongoing.
- **The arc blades** (`ast_blades`, unlock `asteroid>=5`): Two curved blades of light from a maintenance locker. They cut power lines and stun what lives. I held them as if I knew them. I don't know why.
- **A missed answer** (`ast_drop`, unlock `asteroid>=6`): The platform went, and the connection with it. Panther went very quiet. So did I. I'll keep talking.
- **Maintenance interruption** (`ast_fight`, unlock `asteroid>=7`): He was waiting for them. His tools are locked away and his cradle holds him; nothing of him is broken.
- **A little bird** (`ast_archive`, unlock `asteroid>=8`): They stopped the whole line to mend one small bird. I knew what it would sound like before it moved.
- **Something left for them** (`ast_home`, unlock `asteroid>=10`): He keeps what is left safe now. Preservation mode: the essential lights stay on, and for the first time the cavern is quiet.


## Chapter — The Moon: somewhere to come back to (Round 19)

- **Back by choice** (`fin_return`, unlock `finale>=1`): Last time all I wanted was to get away from it. This time we parked somewhere without a prison attached.
- **The people keeping it running** (`fin_scree`, unlock `finale>=3`): A Scree worker repairs Tunneler's equipment in a section he calls unsafe. They opened the old route for us. I said we'd bring something back.
- **The door I knew** (`fin_door`, unlock `finale>=4`): The door from my first vision. They weren't surprised to see me. I think I belonged here.
- **Witness** (`fin_witness`, unlock `finale>=5`): Seven records in seven sockets, and the gaps between them. The archive called me a witness. I was there. I said yes.
- **Eight more** (`fin_panther`, unlock `finale>=6`): He saw it before I did.
- **Received** (`fin_end`, unlock `finale>=8`): The record is complete, and it isn't mine to keep any more. The Scree have their stations. The worlds have their histories.
