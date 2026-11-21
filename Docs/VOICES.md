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

## Scree worker — premade voice (picked by Code)
One freed worker in Europa (Tunneler's base). Tired, wary, slowly finding courage. Speaks aloud.
**No saved voice needed (Dan, 2026-11-19):** Dan has limited voice slots and the Scree aren't one character, so Code
picks a suitable **premade** ElevenLabs voice through the API (`Tools/Voice/generate_voices.py`, key from
`ELEVENLABS_API_KEY`) — tired, hoarse, mid-pitched — and generates the Scree lines with it. Don't create or save a new
voice. Small differences between workers are fine. In Unity: lay Dan's existing Scree noise (from scene 1) under / just
before each line and pitch-shift slightly so it reads as Scree.
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
The facility **System** announcer (AST_02): a premade ElevenLabs voice picked by Code (flat, recorded announcement) — no
saved voice needed.

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

## Nela, the Guide (Biavian) — a premade voice picked by Code (the mothership)
**Dan (2026-11-21): no saved voice — Code picks a premade ElevenLabs voice through the API that fits the description below
(calm, gentle, androgynous, airy) and uses that same voice for every Nela line.** If Dan saves a `BiavianGuide` voice later, use it.
Telepathic (Panther's "inside your head" treatment in Unity). Calm, kind, courteous; quiet anger about the theft. Gender left
open (the script keeps "Guide" as a role until Kyle's design / a name arrive).
Description:
```
A calm, gentle, androgynous voice with a soft, airy resonance, as if heard from inside your mind rather than through the air: warm, courteous and unhurried, clear and precise, with a quiet strength under the gentleness. Light, intimate delivery, close to the microphone, studio-quality recording, no background noise.
```
Preview text:
```
Welcome. Follow the lights beneath you. Forgive me. Was that too sudden? I will announce myself next time. These are memories people chose to leave with us.
```
Residents (BIA_03, BIA_06): premade voices picked by Code (telepathic treatment) — no saved voices needed.
