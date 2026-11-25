# 7: GRAND CIRCUIT — the racing mode (unlocked after the credits)

> **Status (2026-11-24):** Dan's idea, planning chat's design; Dan approved the suggestions (non-lethal pickups, 2 players to start,
> monochrome with glowing track lights). A bonus mode built **after** the main game is locked, in stages. Stage 1 = Round 20.
> **Stage 1 built (Round 20, 2026-11-25, release 1.1.0-beta1):** status, proof and every decision made in `TODO.md` ("Round 20 —
> decisions made", "Round 20 — status"); code `Runtime/Gc*.cs` + `Editor/FightDemoBuilderGrandCircuit.cs` (summary in CLAUDE.md).

## The idea
A Wipeout-style anti-gravity racer using the game's characters, vehicles and worlds: loops, corkscrews, big jumps, boost pads,
shortcuts, AI racers and **split screen**. It unlocks when the player finishes the game (after the credits and Kyle's line: a short
card, "Grand Circuit unlocked", then the main menu shows **Grand Circuit**).

## Look and feel
- **Monochrome with glowing track lights:** the worlds stay in Kyle's misty grayscale; the tracks carry bright white light strips,
  glowing edges, boost pads and gates that pulse, light trails behind the racers. Readable at speed.
- Fast: faster than the story race (top speed ~45–60 m/s with boosts), a strong sense of speed (FOV, streaks, wind, camera shake on
  landings).
- Controller first, button glyphs only (never keyboard keys on screen); keyboard still works for player 1.

## Handling (new: the anti-grav model)
- Vehicles **hover and stick to the track surface** (gravity follows the track's "down"), so loops, corkscrews, banked walls and
  half-pipes work; off the track (a jump), normal gravity until it lands on track again.
- Steer, accelerate, brake, **air-brakes** (LB / RB, Wipeout-style sharp turns), boost (from pads and pickups).
- Bumping other racers pushes, never wrecks; hitting a wall scrapes speed; falling off the course → back on at the last checkpoint
  (a short penalty).
- Each vehicle has stats: top speed, acceleration, handling, weight (who wins a bump).

## Pickups (non-lethal: 7 never kills)
Glowing pads on the track give one at random (weighted by position: more help for those behind):
- **Push** (telekinesis wave): nudges the racer ahead off their line and slows them briefly.
- **Shield**: blocks the next push or bump for a few seconds.
- **Boost**: a burst of speed.
- **Phase** (optional, Stage 2): pass through other racers briefly.
No damage, no destruction, no knock-outs.

## Modes
- **Single race** (1–2 players + AI, choose racer and track, 3 laps).
- **Time trial** (1 player, best lap / best race saved, a ghost of your best).
- **Grand Prix** (Stage 3: all tracks in a row, points).
- **Split screen:** 2 players (top / bottom, or side by side as a setting), each with their own camera, HUD (position, lap, speed, pickup),
  controller assignment on the racer-select screen ("Press A to join"). Up to 4 players later if performance allows.

## Racers and vehicles
| Racer | Vehicle | Character |
|---|---|---|
| **7** | her hover bike | balanced |
| **Duchess** | her teardrop racer (#8) | fast, light, floaty |
| **Tunneler** | his drill rig | slow to start, heavy, wins bumps |
| *Stage 3:* Ratwing | a winged glider | agile, fragile |
| *Stage 3:* the Machinist | a heavy factory sled | top speed, poor turning |
| *Stage 3:* Nela | a smooth Biavian pod | best handling |
| *Stage 3:* Viper, Juniper, others | to design | — |
Vehicles for new racers are built in code in their world's style (no new character models needed; riders sit in / on them).
Ride poses, hands on controls and contact rules from the main game apply.

## Tracks (one per world, built from that world's look)
- **Stage 1:** **Cattalus Grand Circuit (Unlimited)**: the story course rebuilt for anti-grav: a loop through the scratching-post
  column, a corkscrew round the cat-tree towers, a big jump over the toy boulevard, a shortcut through the cardboard quarter.
  **Lunar Loop (the Moon)**: through the hollow Moon's city: a giant loop in the cavern, tunnels, a jump across the mining pit.
- **Stage 2:** Venus gardens (vines, the temple), Mars canyon (big jumps, the crossing), Europa under-ice tube (a glass tube under
  the ice, the Cetaceans outside), the asteroid factory (conveyors, crushers on a fixed rhythm), inside the mothership (the
  interior city, a corkscrew round the shell).

## Music
Reuse `cattalus_race.mp3` for Stage 1; Dan may make new racing tracks per world later (Suno prompts on request).

## Saving
Unlock flag, best times per track / racer, ghosts, the last racer and settings.

## Build checks
- Every track: the AI completes 3 laps on every vehicle with 0 falls-through; loops / corkscrews traversed at the minimum speed
  (with a speed floor or assist so a slow racer never falls off a loop).
- Split screen at 60 fps on Dan's PC settings (2 players + 4 AI).
- The image-quality gate on race captures (both halves of the split screen).
- No keyboard glyphs; button prompts only.

## Stages
- **Stage 1 (Round 20):** the unlock, the menu, anti-grav handling, pickups, 7 / Duchess / Tunneler, Cattalus Unlimited + Lunar Loop,
  single race + time trial, 2-player split screen, AI.
- **Stage 2:** the other five world tracks.
- **Stage 3:** more racers and vehicles, Grand Prix, up to 4 players.
