# Voices — 7: The Observer

Made with ElevenLabs Voice Design (Dan's toolchain). Paste the description into the voice-description box and the
preview text into the text box; generate a few previews, keep the best, save it to the Voice Library with the name below.
All of the cat's lines are telepathic, and 7 also speaks telepathically at times — no lip-sync is needed. In the game,
telepathic lines get a light "inside your head" treatment (close, dry, a touch of soft reverb), added in Unity, not in the voice.

## 7 — voice name `Seven`
Description:
```
A sweet, soft-spoken adult woman in her late twenties to thirties with a gentle, warm, slightly breathy voice that sounds calm and kind. Clear, intimate delivery as if speaking close to the listener, unhurried pacing, quiet confidence, a hint of dry humor. Light and angelic in tone but grown-up and composed, not childlike or squeaky. Neutral American accent, studio-quality recording, no background noise.
```
Preview text:
```
Hey. You shouldn't be here. Go on, before somebody sees you... You came to rescue me? This place was different. Whole. There were people here. Thank you.
```

## The cat (Panther) — voice name `Cat`
Named **Panther** from Venus scene VEN_B01 on (subtitle label "Panther"); same voice.

Description (main pick):
```
An intelligent, particular male cat speaking telepathically: a smooth, low-to-mid adult male voice with a soft velvety purr in the tone and a faint rasp at the edges. Unhurried, precise and quietly amused, with deadpan comic timing and a dry, slightly aristocratic manner, yet capable of sincere warmth. Calm and relaxed, never loud, close to the microphone. Light British accent, studio-quality recording, no background noise.
```
Alternative (if the first sounds too human):
```
A small, sleek black cat who speaks with a soft, slightly nasal, purring male voice: light and smooth, a little feline and mischievous, with a gentle rolling purr on some words. Dry, clever and unhurried, quietly funny, polite and particular. Close, intimate delivery, studio-quality recording, no background noise.
```
Preview text:
```
That would make rescuing you considerably harder. The cat. Only conversationally... Chicken. But only free-range. Having experienced confinement, I feel strongly about it.
```

## Tips
- Generate 3–5 previews per description and pick the one that sounds most like the character; small wording changes
  ("breathier", "lower", "more playful") are the quickest way to steer.
- Use the same voices for every line so 7 and the cat stay consistent. Name files by scene and line,
  e.g. `Audio/Voice/01_first_contact/01_7_hey.wav`, `01_first_contact/03_cat_harder.wav`.

## Tunneler — voice name `Tunneler`
The Moon's villain: runs the mining operation and exploits the Scree. Speaks aloud (no mouth — his drill head twists and
nods with his voice, driven by loudness in Unity). Optional in Unity: a very faint low drill-grind layer under his lines.
Description (main pick):
```
A huge, heavy, inhuman male creature with a very deep, gravelly bass voice that sounds like grinding stone, slow and deliberate, with a rough rumbling texture and a cold, contemptuous calm. A ruthless industrial overseer who never needs to shout because everyone obeys him; menacing, patient, faintly amused by others' weakness. Resonant chest voice, clipped hard consonants, no accent, close to the microphone, studio-quality recording, no background noise.
```
Alternative (if the first sounds too human):
```
A monstrous, low, rumbling voice of a massive creature made of stone rings, deep and grinding like heavy machinery, with a slow, measured, guttural delivery and a slight metallic rasp. Cold, arrogant and menacing, speaking quietly but with enormous weight. Studio-quality recording, no background noise.
```
Preview text:
```
You dig because I say dig. The rock does not care who is tired, and neither do I. Bring me what lies beneath, and perhaps you will see the surface again. Perhaps.
```

## Wormmaster — voice name `Wormmaster`
Venus antagonist (Chapter 2). Smooth and arrogant rather than monstrous — clearly different from Tunneler's grinding
bass. No mouth: his head-tip swells/pulses with his voice in Unity. Lines in `Docs/CHAPTER2_VENUS.md` (VEN_C06/C07).
Description (main pick):
```
A tall, slender, inhuman male creature with a smooth, low-to-mid voice that is soft, wet and slithering, with a faint sibilant hiss on the S sounds. Calm, cool and condescending, speaking slowly as if he has all the time in the world, quietly amused and utterly certain he is in control. Sly and unsettling rather than loud or monstrous. Close to the microphone, studio-quality recording, no background noise.
```
Alternative (if the first sounds too human):
```
An eerie, smooth, serpentine male voice of a giant worm-like creature: low, silky and moist, with a slow drawl, soft hissing sibilants and a slight gurgling undertone. Cold, arrogant and patient, never shouting. Studio-quality recording, no background noise.
```
Preview text:
```
They sent someone down at last. Look around you. Everything beneath this garden answers to me. The ground moves when I ask it to. Do you really think you can stop it?
```

## The Keeper (Cetacean) — voice name `Keeper`
**Status: created and saved by Dan in ElevenLabs (2026-11-19)** — Code uses it by name (`Keeper`) through the API.
Europa's guardian (`Docs/CHAPTER_EUROPA.md`). Convention from the spec: Cetaceans are heard as an understandable voice
**layered over their own calls** — the reason (translation, telepathy…) is still undecided, so don't explain it. In Unity:
the voice line on top, a soft whale-call layer underneath (starts a beat before the words, trails after), underwater
reverb on both. No mouth — the head/fins move slowly with the voice's loudness.
Description (main pick):
```
An ancient, gentle, enormous sea creature with a deep, slow, resonant voice that sounds warm and wise, as if it carries across great distances underwater. Calm, patient and dignified, with long pauses and soft, rounded words, a faint hum or resonance under the voice. Gender-ambiguous, leaning low. No accent, studio-quality recording, no background noise.
```
Alternative (if the first sounds too human):
```
A vast, old whale-like being speaking slowly in a deep, breathy, echoing voice with a soft singing quality, words stretched gently like a long call through water. Serene, kind, unhurried. Studio-quality recording, no background noise.
```
Preview text:
```
You followed the old approach. Then someone still remembers the route. Our young travel through here. Our calls guide them. That is how much of our history travelled. One voice teaching another.
```

## Scree worker — voice name `ScreeWorker` (Europa)
**Status: created and saved by Dan in ElevenLabs (2026-11-21)** - every Scree worker line re-voiced with it
(`Tools/Voice/generate_europa_voices.py`; replaces the premade take). One freed worker in Europa (Tunneler's base). Tired,
wary, slowly finding courage. Speaks aloud. In Unity: Dan's existing Scree noise is layered under each line (EuropaDirector).
Description:
```
A small, weary humanoid creature with a quiet, slightly hoarse mid-pitched voice, worn down by hard labor. Cautious and hesitant at first, speaking in short sentences, with a growing note of hope. Gender-neutral, a faint raspy texture, no accent, close to the microphone, studio-quality recording, no background noise.
```
Preview text:
```
Where are we being taken? All of us? These connections feed the drills. Break them, and he cannot restart them. This section will not survive.
```

## Cetacean calls (SFX, ElevenLabs Sound Effects)
`europa_call_deep` — "deep slow whale song call, long low moan rising and falling, underwater, distant echo".
`europa_call_answer` — "softer higher whale call answering in the distance, underwater, gentle".
`europa_call_learning` — "young whale attempting a short three-note call, slightly wobbly, then an older deep whale repeating it clearly, underwater" (Europa vision).
`europa_dolphin_chirp` — "friendly dolphin clicks and whistles, underwater" (the guide).

## Viper — voice name `Viper`
**Status: created and saved by Dan in ElevenLabs (2026-11-19).**
Mars antagonist (`Docs/CHAPTER_MARS.md`): profits from the division between the two kingdoms; cold, controlled,
contemptuous. Must sound clearly different from Tunneler (grinding bass) and the Wormmaster (wet, slithering).
In Unity: jaw opens with loudness; optional faint hiss layer at the ends of some lines.
Description (main pick):
```
A tall, lean reptilian male with a dry, cool, precise mid-to-low voice and a slight hiss on the S sounds. Clipped, controlled and quietly menacing, like a cold political operator who never raises his voice; superior, dismissive and faintly amused. Crisp consonants, measured pauses, a dry dismissive laugh. Close to the microphone, studio-quality recording, no background noise.
```
Alternative (if the first sounds too human):
```
A cold, raspy, serpentine voice of a towering snake-headed creature: smooth and dry with a sharp hissing edge, low and deliberate, contemptuous and calculating, speaking quietly but with authority. Studio-quality recording, no background noise.
```
Preview text:
```
Someone who understands what happens when we become careless. Then they can accept our terms. What business do you have there? History is precisely the problem.
```

## The Machinist — voice name `Machinist` (asteroid)
**Status: created and saved by Dan in ElevenLabs (2026-11-20).**
The asteroid city's last caretaker (`Docs/CHAPTER_ASTEROID.md`): not evil — dutiful, literal, certain; a hint of
something almost sad under the precision. No mouth: the visor slit glows with his voice.
Description (main pick):
```
A huge, ancient maintenance machine with a deep, calm, resonant male voice that is precise and mechanical but not robotic-sounding: measured, literal and unhurried, every word placed exactly, with a faint metallic resonance and the softest hum of machinery under it. Polite, certain, emotionless on the surface, with a barely perceptible hesitation before difficult answers. Close to the microphone, studio-quality recording, no background noise.
```
Preview text:
```
You have left the receiving route. State your function. Archive access is suspended. Restoration is ongoing. It must be ready when they return.
```
## The facility System announcer — voice name `System` (asteroid)
**Status: created and saved by Dan in ElevenLabs (2026-11-21)** - AST_02's System lines re-voiced with it
(`Tools/Voice/generate_asteroid_voices.py`; replaces the premade take). Flat, recorded announcement; in Unity the chime, the
machine's brass speaking-horns and grille pulse with the voice, and the steam whistle (AsteroidDirector / AstMaintBot).

## Ratwing — voice name `Ratwing` (Biaveh)
Independent thief out to hold the Biavian archive for ransom (`Docs/CHAPTER_BIAVEH.md`). Sly, mocking, opportunistic — must
sound clearly different from Viper (cold, hissing), Tunneler (grinding bass) and the Wormmaster (wet, slithering).
Description:
```
A gaunt, winged creature with a thin, sly, slightly nasal mid-to-high male voice with a dry rasp and a hint of a chittering squeak at the edges. Mocking, opportunistic and quick-witted, a cocky thief who thinks everything has a price; casual, sneering amusement rather than menace, quick delivery with sudden sharp edges when crossed. Close to the microphone, studio-quality recording, no background noise.
```
Preview text:
```
They've found someone to run their errands. They can tidy up when we've agreed on a price. They seem very attached to it. Do you have any idea what's in there?
```

## Nela, the Guide (Biavian) — voice name `Nela` (the mothership)
**Status: created and saved by Dan in ElevenLabs (2026-11-21)** - every Nela line re-voiced with it
(`Tools/Voice/generate_biaveh_voices.py`; replaces the premade "Bella" take).
Telepathic (Panther's "inside your head" treatment in Unity: her lines play on the telepathy source). Calm, kind, courteous;
quiet anger about the theft.
Description:
```
A calm, gentle, androgynous voice with a soft, airy resonance, as if heard from inside your mind rather than through the air: warm, courteous and unhurried, clear and precise, with a quiet strength under the gentleness. Light, intimate delivery, close to the microphone, studio-quality recording, no background noise.
```
Preview text:
```
Welcome. Follow the lights beneath you. Forgive me. Was that too sudden? I will announce myself next time. These are memories people chose to leave with us.
```
Residents (BIA_03, BIA_06): premade voices picked by Code (telepathic treatment) — no saved voices needed.

## Mars: the caretaker, the worker, the guard — voice names `Caretaker`, `MarsWorker`, `MarsGuard`
**Status: created and saved by Dan in ElevenLabs (2026-11-21)** - every caretaker / worker / guard line re-voiced with them
(`Tools/Voice/generate_mars_voices.py`; replaces the premade Bill / Sarah / Daniel takes). Each line keeps its direction.

## Cattalus (Round 18) — voice names `Juniper`, `Duchess`, `Steward`, `Announcer`
All Cattalus cats speak telepathically (Panther's treatment, no lip-sync). Code designs these with ElevenLabs Voice Design (or
picks premade voices) and lists which in the status. Voices match the looks.
- **Juniper** (the script's "Acquaintance"): Panther's old friend, a silver tabby a little older than him. Warm, teasing,
  knowing; a mid-range female voice with a smile in it, unhurried.
- **Duchess** (the script's "Champion"): a large, fluffy white champion. Languid, gracious, a little sleepy and posh; a low,
  velvety older female voice; gets genuinely delighted about boxes.
- **Steward**: the race steward, a neat tuxedo cat. Precise, polite, professional, slightly prim; a crisp middle-aged male voice.
- **Announcer**: the Cat-a-tron's sports announcer **and every Cat-a-tron advert / TV line** (CAT_04_01, 02, 09, CAT_07_01 and any
  race calls). **Dan (2026-11-22): use the "Barker" voice from his game Tee'd Off** (repo `C:\Users\danmo\Tee-dOff`: find the
  Barker's ElevenLabs voice ID in its voice tools / config, or Dan's saved ElevenLabs voice named Barker). Stadium PA treatment
  (slight echo) in the square; the plain voice when heard through the screen close up. Don't design a new announcer voice.

## The Moon finale (Round 19) — voice names `Archive`, `ScreeWorker2`, `RestVoice`
**Status: all three created and saved by Dan in ElevenLabs (2026-11-22).**
Everyone else reuses an existing voice: 7, Panther, Tunneler, the Scree worker (`ScreeWorker`), the Venus Guardian (the "Elarim
Voice"), the Machinist, Nela (the "Biavian Guide").
- **Archive** — the chamber's recorded system voice (FIN_05, FIN_06, FIN_11, FIN_12). Not a character. Design:
  "A calm, soft, ageless voice, neither clearly male nor female, quiet and restrained, slightly distant as if recorded long ago,
  gentle and precise, warm underneath, unhurried with small pauses. Clear studio recording." Preview text: "Records received.
  Witness recognized. Living testimony available. Permission to preserve and share? Record complete." In Unity: a faint
  chamber reverb and a very light shimmer, no robotic effect.
- **ScreeWorker2** — the second Scree voice (FIN_07_04, FIN_12_02), heard over the network. Description:
  "An older, larger humanoid creature of the same weary laboring people, with a low, steady, gravelly voice, slower and calmer than
  the younger workers, worn by years of hard labor but firm and quietly determined, a natural elder others listen to. Speaks in short,
  plain sentences. Gender-neutral, a faint raspy texture, no accent, close to the microphone, studio-quality recording, no background
  noise." Preview text: "These controls belong to the city. He locked us out of them. Send them to the other districts. We have
  waited long enough." In Unity: a light comms filter and Dan's Scree noise underneath, like the first worker.
- **RestVoice** — **QF31: the line is now** "Well done, good and faithful servant! You were faithful over a few things; I will put you in charge of many things. Share your master's joy." (replaces "You can rest now."; plays in heaven over the rising wide shot, QF31). Was: FIN_13's "You can rest now." **Dan (2026-11-22): it's used, and the implication is that it's God / Jesus.**
  Description: "A deep, resonant, majestic male voice of immense warmth and quiet authority, large and full like it fills a vast
  space, yet gentle and loving rather than stern, slow and unhurried, every word calm and certain, as if a father is speaking softly
  to a tired child. Rich low tones, no accent, studio-quality recording, no background noise." Preview text: "You can rest now.
  You have done well. Come home." In Unity: a large, soft, warm space around it (a long gentle reverb, a subtle low swell), never
  harsh or echoing like a PA; it should feel like it surrounds her. Never shown or named on screen.

## The Cat-a-tron commercials (QF41) — voice names `Ad_Zoomies`, `Ad_Bowl`, `Ad_Tuna`, `Ad_Gravity`, `Ad_Birds`, `Ad_Box`
One NEW voice per ad, never a story voice. All of them are made by `Tools/Audio/make_catatron_ads.py`: the Voice Design API, the
lines, the effects, the music bed and the mix.
**Status (2026-11-28, QF42): DESIGNED.** Dan enabled Voice Generation on the key; the script designed and saved all six `Ad_`
voices in the ElevenLabs library (the first of three previews kept), re-voiced every line with them (`--force`) and remixed the
six tracks. The premade stand-ins (last column) are no longer used. To redo one: `python Tools/Audio/make_catatron_ads.py --only <key> --force`
(it reuses the saved `Ad_` voice; delete it in the library first to design it again).

| Ad | Voice name | Description (the Voice Design prompt) | Stand-in before QF42 (no longer used) |
|---|---|---|---|
| Midnight Zoomies | `Ad_Zoomies` | A hyped-up male sports-promo announcer, booming, gravelly, over-the-top monster-truck energy. | Harry (premade) |
| The Empty Bowl Crisis | `Ad_Bowl` | A gentle, sorrowful middle-aged British woman, soft charity-appeal delivery, earnest and slightly over-sentimental. | Alice (premade) |
| Eau de Tuna | `Ad_Tuna` | A sultry, breathy French-accented woman, slow luxury perfume-ad whisper, elegant and seductive, a little absurd. | Laura (premade) + v3 `[French accent]` |
| The Gravity Testing Championship | `Ad_Gravity` | An older refined British male golf commentator, hushed reverent whisper that can explode into a delighted shout. | George (premade) |
| The Bird Channel | `Ad_Birds` | A calm, soothing male nature-documentary narrator, warm and slow, gently amused. | Roger (premade) |
| Box Deluxe | `Ad_Box` | A deep, smooth, velvety male luxury-car-commercial voice, slow and confident, every word savoured. | Brian (premade) |

The race ad (CAT_04) keeps the announcer: `TeedOff Barker`.
