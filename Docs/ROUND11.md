# Round 11 (queued — start after Round 10 is committed; move this into TODO.md as the Current Objective first)

## Chapter 2 "Venus", part 1: landing → gardens → temple → the signal (Dan, 2026-11-17)
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
