# Conversation reactions

Little listener sounds (laughs, chuckles, "hmm", "mm-hmm", sighs, purrs) placed into the existing dialogue,
after Dan's direction: when one says something funny, the other laughs or giggles; plus the small sounds people
make while talking. Subtle and sparing: a few per scene, **never over a spoken line**, never undercutting a sincere
moment (the cell "Thank you" / "You're welcome", "Remembering.", the seed, "I think I knew them." have none).
Only 7 and the cat / Panther react (not the guardian, the attendant, Wormmaster or the Scree).

## Runtime (Round 13)
- **Where:** `Unity/Assets/Scripts/Runtime/DialoguePlayer.cs` (`PlayOne`, so every line of Chapter 1, the galley, Venus
  and the Venus debrief gets it — `PlayLines` / `PlayScene` / `PlayIds` all go through `PlayOne`) +
  `Runtime/Reactions.cs` (static loader of `reactions.json` and the takes in `Resources/Reactions/`).
- **Flow:** when a line ends **in full** and its id has a placement, the listener waits `delay`, then plays a random
  take of `kind` (never the same take twice in a row), then 0.3 s of silence (`Reactions.GapAfter`); only then may the
  next line start. In `PlayLines` the reaction time counts toward the scripted pause after the line (the pause is
  `max(script pause, reaction)`), so a reaction never makes a long scripted silence longer. Lines played one by one
  with `PlayOne` (the galley, the Venus debrief) wait for the reaction before their own pause.
- **Sound:** quiet (`Reactions.Volume` = 0.5 of a spoken line, voice bus). 7 = her plain 2D voice; the cat / Panther =
  exactly his telepathy treatment (`DialoguePlayer.MakeSource(…, telepathic: true)`: mono, band-limited, short soft
  room), the faint far version in vision mode. No subtitle.
- **Skip:** Y / Enter skipping a line skips its reaction; pressing it during a reaction stops it. Check runs that
  auto-skip lines (`AutoSkipAfter`) skip the reaction of every line they cut short.
- **Never over a line:** a reaction only starts when no dialogue player has a line in progress; a line starting while a
  reaction is still due stops it. Either case logs `REACTION OVERLAP` (must never appear).
- **Body cue:** `Reactions.OnReaction(who, kind, cue)` fires as the sound starts (directors can subscribe). Default
  cue in `DialoguePlayer` (a few degrees added on top of the animated bones in LateUpdate, never replacing a pose or
  an IK target): 7 — `head_tilt` (head roll 5°), `nod` (two small dips), `look_down` (6°), `shoulders` (both
  clavicles up 3.5°), only where `SevenTableIK` is present and enabled (the galley, the Venus debrief); the cat —
  `ears` (both ears swivel twice, 14°) where `CatPoseOverlay` with ear bones is enabled. `tail`, `none` and 7's cues
  outside the galley = the event only.
- **Check-run log** (capture dir set): `reaction: after <lineId> <who> <kind> take <n> (<len> s), next line waited <x> s`,
  or `… skipped (line skipped)`. Switch `-noreactions` = none play.
- **Make / re-make the bank** (repo root, key only from `ELEVENLABS_API_KEY`):
  `python Tools/Voice/generate_reactions.py [--force] [--only seven_hmm | seven_hmm_2 …]` →
  `Unity/Assets/Resources/Reactions/<who>_<kind>_<n>.mp3`; trims silence (ffmpeg), levels every take to −19 dB mean
  (peaks ≤ −1.5 dB) and re-takes any that comes out empty or longer than 2.5 s. `--check` = list the lengths;
  `--level` = re-level existing takes. Takes' wording is the `TAKES` table at the top of the script. A new kind =
  add it to `TAKES`, to `bank` in `reactions.json` and generate; up to 6 takes per kind are picked up.

## How it works / how to edit
- Data: `Unity/Assets/Resources/Reactions/reactions.json`. Each placement = one sound by the listener, played in the
  gap **after** the line `after` and before the next line (after `delay` seconds; the next line waits for it).
- Move a reaction: change its `after` id. Remove one: delete its entry (mind the commas). Change the sound: change
  `kind` (must be in `bank` for that speaker). `cue` = the small body move that goes with it:
  `head_tilt`, `shoulders`, `nod`, `look_down`, `tail`, `ears`, `none` (`none` when the listener is off screen,
  e.g. the cat talking from the ship). No code changes needed; this table is for reading, the json is what the game uses.
- Sounds: 7 = `laugh_soft`, `giggle`, `chuckle`, `nose_laugh`, `hmm`, `mmhmm`, `hm_q` (questioning "hm?"), `sigh`, `oh`.
  Cat / Panther = `chuckle`, `purrhum`, `hm`, `mm`, `purr`, `sigh` (telepathic voice, same as his lines).
- Line ids: Chapter 1 from `Unity/Assets/Audio/Voice/voice_lines.json`; Venus from
  `Unity/Assets/Resources/Venus/venus_dialogue.json`. **VEN_B01 uses the new numbering of Dan's 2026-11-19 rewrite**
  in `Docs/CHAPTER2_VENUS.md` (VEN_B01_06..20); the regenerated `venus_dialogue.json` uses the same ids
  (checked 2026-11-19). The old B01_06..16 numbering no longer applies.
- In VEN_B01_06..20 the laughs / chuckles / hums are part of the lines' delivery (B01_08, 12, 16, 18) and are not
  doubled here; those rows are listed as "scripted" for reference only (not in the json).

# Chapter 1 — The Moon Prison

## 1 First contact

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `s01_05_cat` | The cat. | 7 | oh | head_tilt | she looks back at him: the voice was the cat |
| `s01_07_cat` | Only conversationally. | 7 | nose_laugh | none | his dry joke; a small disbelieving breath, still wary |
| `s01_09_cat` | Yes. We should probably do that first. | 7 | mmhmm | nod | agreeing to the plan |

## 2 Opening the cell

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `s02_03_cat` | I wouldn't have asked you to stand back other... | 7 | nose_laugh | shoulders | his reply to her question is funny |
| `s02_07_cat` | My way in may be unsuitable for you. | 7 | hmm | look_down | she sizes up his small way in |

## 3 What he knows

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `s03_02_cat` | My people. | 7 | hm_q | head_tilt | "My people." raises a question |
| `s03_06_cat` | They didn't tell me. | 7 | hmm | none | taking in that he was not told |
| `s03_10_cat` | No. I was hoping you could explain that part. | 7 | sigh | shoulders | neither of them knows; a light rueful sigh |

## 4 Finding an escape route

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `s04_02_cat` | Older. | 7 | mmhmm | nod | agreeing: older |
| `s04_04_cat` | Convenient. I'd prefer a way out that wasn't... | 7 | chuckle | none | his dry line about the jailers |

## 5 Encountering the Scree

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `s05_01_cat` | Can we get past? | 7 | hmm | none | she weighs the guard before answering |
| `s05_03_cat` | I was rather hoping you'd say that. | 7 | mmhmm | nod | quiet agreement; no one gets hurt |

## 6 The first vision

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `s06_07_7` | I don't know. | cat | hm | ears | he hears she does not know; ears turn to her |
| `s06_09_cat` | We should talk about that. | 7 | mmhmm | nod | yes, later |

## 6b The city reveal

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `s06b_04_cat` | Apparently we escaped just in time for traffic. | 7 | laugh_soft | shoulders | his traffic joke |
| `s06b_05_7` | There are people living all the way up there. | cat | mm | none | he watches her, not the skyline |

## 7 Seeing the saucer

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `s07_04_cat` | Good. I was beginning to worry you meant one... | 7 | chuckle | shoulders | the mining-carts joke |
| `s07_06_cat` | Then we have a destination. | 7 | mmhmm | nod | agreeing: a destination |

## 8 Boarding and escaping

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `s08_04_cat` | An excellent reason for you to go first. | 7 | laugh_soft | head_tilt | his reason to let her go first |

## 9 The larger interior

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `s09_02_7` | Is that a complaint? | cat | hm | ears | he considers whether it is a complaint |
| `s09_03_cat` | I'm reserving judgment until I find the kitchen. | 7 | giggle | shoulders | the kitchen joke |

## 10 The galley

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `s10_02_cat` | Chicken. But only free-range. | 7 | hm_q | head_tilt | "only free-range": she looks back at him |
| `s10_03_cat` | Having experienced confinement, I feel strong... | 7 | nose_laugh | look_down | the confinement joke; she almost smiles |
| `s10_05_cat` | You're letting it get cold. | 7 | oh | look_down | she notices her fork has stopped |
| `s10_30_cat` | Unless this was a farewell dinner. | 7 | laugh_soft | look_down | the farewell-dinner joke, before she answers seriously |
| `s10_36_7` | Venus, then. | cat | purr | tail | he returns to his food, content |

---
# Chapter 2 — Venus

## VEN_C01 Arrival

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `VEN_C01_04` | It's beautiful. | cat | mm | ears | quiet agreement with "It's beautiful." |
| `VEN_C01_07` | Experience. | 7 | nose_laugh | shoulders | "Experience." is a joke about the Moon |

## VEN_B01 Unpacking the bike (name reveal)

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `VEN_B01_04` | I'm establishing that my advice is useful. | 7 | nose_laugh | shoulders | his advice is useful, he insists |
| `VEN_B01_10` | I'd have to lie on the floor. | Panther | hm | ears | dry answer to her tease, before he stretches |
| `VEN_B01_11` | There. Saved you the trouble. | 7 | laugh (scripted in B01_12) | - | "breathless, followed by a disbelieving laugh" is in her line |
| `VEN_B01_15` | An excellent time to be small. | 7 | chuckle (scripted in B01_16) | - | "a reluctant little chuckle" before "Mm. Fair point." is her take |
| `VEN_B01_17` | You're not getting a bigger bowl. | Panther | hum (scripted in B01_18) | - | "with a thoughtful hum" is in his line |
| `VEN_B01_18` | Mmm. We can discuss that when you return. | 7 | (removed) | - | the B01_19 take already opens with a soft laugh ([laughs softly]) - removed so she does not laugh twice |

## VEN_B02 First ride

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `VEN_B02_02` | I'm seeing the inside of your ship. | 7 | chuckle | none | he can only see the inside of the ship |
| `VEN_B02_04` | Tell me when you get back. | 7 | mmhmm | none | she will tell him |

## VEN_C02 Garden tremor (during VEN_B03)

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `VEN_C02_02` | I felt that from here. | 7 | hmm | none | uneasy: the tremor reached the ship |

## VEN_C03 Meet the guardian

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `VEN_C03_03` | May I see it? | 7 | mmhmm | nod | she holds it out |
| `VEN_C03_10` | Most of a life is small things. | 7 | hmm | look_down | she takes in the guardian's answer |

## VEN_C04 Cube and interruption

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `VEN_C04_02` | Our histories were not always kept apart. | 7 | hmm | head_tilt | histories not kept apart |
| `VEN_C04_04` | This holds our record. Yours belongs to the M... | 7 | oh | look_down | hers belongs to the Moon |

## VEN_B05 The lower gardens

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `VEN_B05_03` | You'll have to continue on foot. | 7 | mmhmm | nod | on foot from there |

## VEN_C05 Discover the signal

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `VEN_C05_01` | They aren't attacking the people. | Panther | hm | none | he listens, then asks |
| `VEN_C05_05` | Then find what's calling them. | 7 | mmhmm | none | agreeing: find the source |

## VEN_B06 Return ride

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `VEN_B06_03` | And its unwelcome guest? | 7 | nose_laugh | none | "unwelcome guest": dry |
| `VEN_B06_04` | Staying put. | Panther | chuckle | none | "Staying put." |
| `VEN_B06_05` | The others are going home. | Panther | purrhum | none | the creatures are going home |

## VEN_C08 Return to the guardian

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `VEN_C08_03` | Repairs we can make. | 7 | mmhmm | nod | repairs they can make |

## VEN_C10 Receive the record

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `VEN_C10_03` | Yes. | 7 | hm_q | head_tilt | why give her another, then |

## VEN_B07 Back to the saucer

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `VEN_B07_02` | Some. | Panther | hm | none | fills the scripted pause after "Some." |

## VEN_C11 Saucer debrief

| after line id | line (short) | who | reaction | cue | why |
|---|---|---|---|---|---|
| `VEN_C11_02` | The Moon's record. Venus has its own. | Panther | hm | ears | taking in "Venus has its own." |
| `VEN_C11_08` | I wish I could remember their face. | Panther | purr | none | he settles closer; a low purr in the held silence, comfort not a joke |
| `VEN_C11_10` | Then we have somewhere else to ask. | 7 | mmhmm | nod | agreeing: somewhere else to ask |

---

**Totals:** Chapter 1 — 26 placements, Venus — 24 placements, 50 in all.

## The bank (Round 13, generated 2026-11-19)

45 takes, 3 per kind, all Eleven v3 with audio tags (the cat on v3 too, so they match his v3 lines; the v2 fallback
was not needed). Lengths after trimming (s):

| who | kind | take 1 | take 2 | take 3 |
|---|---|---|---|---|
| 7 | laugh_soft | 1.80 | 1.02 | 2.25 |
| 7 | giggle | 1.61 | 1.65 | 0.96 |
| 7 | chuckle | 0.75 | 0.87 | 0.81 |
| 7 | nose_laugh | 0.79 | 0.26 | 0.50 |
| 7 | hmm | 0.49 | 1.26 | 1.89 |
| 7 | mmhmm | 1.12 | 1.55 | 0.34 |
| 7 | hm_q | 0.37 | 0.37 | 0.45 |
| 7 | sigh | 1.08 | 0.67 | 2.16 |
| 7 | oh | 0.86 | 0.46 | 0.44 |
| cat | chuckle | 1.14 | 1.41 | 0.98 |
| cat | purrhum | 1.12 | 1.12 | 2.00 |
| cat | hm | 0.50 | 0.53 | 0.62 |
| cat | mm | 0.62 | 0.91 | 0.97 |
| cat | purr | 0.70 | 1.34 | 0.98 |
| cat | sigh | 0.93 | 0.80 | 0.55 |

Not checked by ear yet (the key has no speech-to-text access): listen for a take that speaks its tag aloud
("thinking…") and re-take it with `--force --only <who>_<kind>_<n>`.
