# Field guide (ship console)

The ship console's **Field guide**: 7's short observations of each person or creature she has met, written only from
what the story has shown. The game reads `Unity/Assets/Resources/Console/field_guide.json`; this page mirrors it.
Edit `title` / `text` there (and here). `titleBefore` / `textBefore` are shown until `nameUnlock` is met: the cat is
"The cat" until Venus scene VEN_B01, then "Panther". `portrait` = the picture key. Unlock forms as in
`Docs/ADVENTURE-LOG.md` (`ch1>=N`, `venus>=<Stage>`, `scene:<VEN id>`, or `never`).

## Panther (before the name: "The cat")
`cat` · unlock `ch1>=0` · portrait `cat` · name from `scene:VEN_B01`

**Before the name:** A black cat who speaks in my head, only conversationally. His people sent him to get me out of the prison; they did not tell him why, and he came anyway. He has views on chicken: free-range only.

**After:** His name is Panther. He can grow from house-cat size to something very large, smoothly, in the middle of a stretch. He doesn’t see what I see, so I tell him when I get back. He chose to stay.

## The Scree
`scree` · unlock `ch1>=7` · portrait `scree`

Tall, armored people who guard the Moon prison; some of them were working rather than watching. I got past them without hurting anyone. The two on the terrace went down and sat up again afterwards. I don’t know yet whether any of them chose this.

## The guardian
`elarim_guardian` · unlock `venus>=GuardianMet` · portrait `elarim_guardian`

Keeper of the Elarim temple on Venus and its cube. Asked to see the shape before anything else, and listened to all of it. Asked me to remember that the creatures were being driven. Gave me a copy of their record; the original stays in the temple.

## The attendant
`elarim_attendant` · unlock `venus>=HelpAccepted` · portrait `elarim_attendant`

Works at the temple with the guardian. Came into the cube chamber with the news that the lower channel had collapsed.

## The Elarim
`elarim` · unlock `scene:VEN_B03` · portrait `elarim`

The people of the Venus gardens. They tend the terraces and the water channels and keep repairing the ground. Their marking is on the shape I found on the Moon. “Our histories were not always kept apart,” the guardian said.

## The burrowing creatures
`worm` · unlock `venus>=ControlDiscovered` · portrait `worm`

Burrowers that live beneath the Venus gardens. They never attacked the people. They were following a signal through the foundations. When the devices stopped, they went home, all of them unharmed.

## The Wormmaster
`wormmaster` · unlock `venus>=ThreatStopped` · portrait `wormmaster`

He drove the burrowing creatures with machines from a chamber under the gardens, to force the Elarim to give up their temple. He told me I don’t know what I’m interfering with. He is contained now, not hurt.

## Tunneler
`tunneler` · unlock `never` · portrait `tunneler`

(Not met yet.)

*Note: Not shown in the story so far: only his mining machinery is seen in the city reveal (6b), and no line names him. Write the entry and set an unlock when he appears.*

## Ratwing
`ratwing` · unlock `never` · portrait `ratwing`

(Not met yet.)

*Note: Only in the Round 4 cave demo, not in a chapter. Write the entry and set an unlock when he appears in the story.*

Notes:
- The Scree entry does not name Tunneler: no line in the game names him yet; only his machinery is seen in 6b.
- The Scree unlock is `ch1>=7` so the terrace fight (inside checkpoint 6) is covered by the text.
- The guardian and the attendant are written without he / she (the spec does not say).


## Round 14 — Europa entries (Field Guide, mirror of `Resources/Console/field_guide.json`)

- **Tunneler** (`tunneler`, unlock `europa>=4`): Ring-segmented, with a spiral drill for a head that turns when he speaks. He kept me in the Moon prison; on Europa his machinery silenced the Cetaceans' calls and he worked Scree who could not leave. He promised to take only what nobody would miss. He escaped in his own craft - and he will remember.
- **The Cetaceans** (`cetaceans`, unlock `europa>=2`): Europa's people live in the ocean under the ice: vast, slow, and they speak - an understandable voice carried on their calls. They teach their history through sound, one voice to another.
- **The keeper** (`keeper`, unlock `europa>=3`): An ancient Cetacean who keeps the settlement and the old routes. Calm, patient; becomes very still when something does not belong. Gave me Europa's record and asked me to carry it somewhere it can be heard.
