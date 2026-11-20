# Field guide (ship console)

The ship console's **Field guide**: 7's short observations of each person or creature she has met, written only from
what the story has shown. The game reads `Unity/Assets/Resources/Console/field_guide.json`; this page mirrors it
(regenerate both with the QF13 script logic: edit the JSON, then this page). `titleBefore` / `textBefore` are shown
until `nameUnlock` is met. **QF13: entries grow with the story** - optional `more` paragraphs follow the main text,
each shown only once its own unlock is met, so nothing mentions a place or event the player hasn't reached.
Unlock forms as in `Docs/ADVENTURE-LOG.md` (`ch1>=N`, `venus>=<Stage>`, `scene:<VEN id>`, `europa>=N`, or `never`).

## Panther (before the name: "The cat")
`cat` · unlock `ch1>=0` · portrait `cat` · name from `scene:VEN_B01`

**Before the name:** A black cat who speaks in my head, only conversationally. His people sent him to get me out of the prison; they did not tell him why, and he came anyway. He has views on chicken: free-range only.

**After:** His name is Panther. He can grow from house-cat size to something very large, smoothly, in the middle of a stretch. He doesn’t see what I see, so I tell him when I get back. He chose to stay.

## The Scree
`scree` · unlock `ch1>=7` · portrait `scree`

Tall, armored people who guard the Moon prison; some of them were working rather than watching. I got past them without hurting anyone. The two on the terrace went down and sat up again afterwards. I don’t know yet whether any of them chose this.

*From `europa>=5`:* On Europa they laboured in Tunneler’s underwater base until the doors were open - then they opened doors themselves and showed me the safe routes.

## The guardian
`elarim_guardian` · unlock `venus>=GuardianMet` · portrait `elarim_guardian`

Keeper of the Elarim temple on Venus and its cube. Asked to see the shape before anything else, and listened to all of it.

*From `venus>=HelpAccepted`:* Asked me to remember that the creatures were being driven.

*From `venus>=RecordReceived`:* Gave me a copy of their record; the original stays in the temple.

## The attendant
`elarim_attendant` · unlock `venus>=HelpAccepted` · portrait `elarim_attendant`

Works at the temple with the guardian. Came into the cube chamber with the news that the lower channel had collapsed.

## The Elarim
`elarim` · unlock `scene:VEN_B03` · portrait `elarim`

The people of the Venus gardens. They tend the terraces and the water channels and keep repairing the ground.

*From `venus>=GuardianMet`:* Their marking is on the shape I found on the Moon.

*From `venus>=HelpAccepted`:* “Our histories were not always kept apart,” the guardian said.

## The burrowing creatures
`worm` · unlock `venus>=ControlDiscovered` · portrait `worm`

Burrowers that live beneath the Venus gardens. They never attacked the people. They were following a signal through the foundations.

*From `venus>=ThreatStopped`:* When the devices stopped, they went home, all of them unharmed.

## The Wormmaster
`wormmaster` · unlock `venus>=ThreatStopped` · portrait `wormmaster`

He drove the burrowing creatures with machines from a chamber under the gardens, to force the Elarim to give up their temple. He told me I don’t know what I’m interfering with. He is contained now, not hurt.

## Tunneler (before the name: "Whoever runs the prison")
`tunneler` · unlock `ch1>=6` · portrait `tunneler` · name from `europa>=3`

**Before the name:** Someone runs the prison I was held in on the Moon, and the mining that is cutting into the city inside it. The Scree do the work. I never saw who.

**After:** He calls himself Tunneler. He ran the Moon prison I was held in, and the mining that is cutting into the city inside the Moon; the Scree did the work.

*From `europa>=4`:* On Europa he spoke to me from a screen in his underwater base: ring-segmented, with a spiral drill for a head that turns when he speaks. His machinery silenced the Cetaceans’ calls, and he worked Scree who could not leave. He promised to take only what nobody would miss.

*From `europa>=7`:* He escaped in his own craft - and he will remember.

## Ratwing
`ratwing` · unlock `never` · portrait `ratwing`

(Not met yet.)

## The Cetaceans
`cetaceans` · unlock `europa>=2` · portrait `cetacean`

Europa’s people live in the ocean under the ice: vast, slow, and they speak - an understandable voice carried on their calls.

*From `europa>=9`:* They teach their history through sound, one voice to another.

## The keeper
`keeper` · unlock `europa>=3` · portrait `keeper`

An ancient Cetacean who keeps the settlement and the old routes. Calm, patient; becomes very still when something does not belong.

*From `europa>=8`:* Gave me Europa’s record and asked me to carry it somewhere it can be heard.

## Round 15 — Mars (mirror of `Resources/Console/field_guide.json`; no portraits yet)

### Reptilians

`reptilians` · unlock `mars>=1`

The people of Mars's two kingdoms: workers, families, traders, guards. Ordinary people on both banks of the ravine; their distrust has its reasons.

*From `mars>=7`:* Their two kingdoms' symbols turned out to be halves of one design.

### The checkpoint guard

`mars_guard` · unlock `mars>=2`

Holds the main crossing on the west bank. He has his orders, and he keeps them.

*From `mars>=9`:* Later I saw him talking with the caretaker by the open barrier. Not friends - but working.

### The worker

`mars_worker` · unlock `mars>=2`

Was with the stranded supply transport - water filters for the other bank.

*From `mars>=5`:* Volunteered to work one of the temple's two controls.

*From `mars>=7`:* “We were taught it was taken from us.”

### The caretaker

`mars_caretaker` · unlock `mars>=3` · before `mars>=3`: "A Reptilian on the far bank" - Waiting at the east checkpoint.

Keeps the approach to the temple on the east bank. Careful, honest about what both sides want.

*From `mars>=5`:* Worked the east control while the worker worked the west.

### Viper

`viper` · unlock `mars>=2`

Profits from the division between the two kingdoms; tells each bank the other means to seize the temple. He noticed the records I carry.

*From `mars>=6`:* Subdued and contained at the temple approach, left for local custody. “They'll turn on each other again.” That is their choice.
