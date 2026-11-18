# QF9 (queued — do right after Round 11 is released; move into TODO.md as the Current Objective first)

## Chapter 1: the fighting tutorial at the exit + the door-frame flicker (Dan, 2026-11-18)
Commit first. All standing rules apply (monochrome, 7 never kills, the Scree are oppressed people — not monsters),
**20-minute testing rule**, look at your own captures before reporting.

1. **Door frames still flicker** (Dan, playing 0.10.5, in the last chamber before the exit — the lunar-door / vision
   chamber). QF8 marked it "done (cause fixed; not watched in motion)" — it isn't fixed. This time **watch it in
   motion**: record a short sequence of consecutive frames while 7 walks and the camera orbits through that chamber
   (and every other chamber with door frames), measure each door frame's brightness frame to frame, and confirm there
   is no black/grey switching. If the occluder fade is the cause, door frames and other architecture must never fade —
   only the camera pulls in. Report the measurement.
2. **Fighting tutorial with the two Scree at the exit.** When 7 leaves the base, the two Scree there (right before the
   city reveal — find which pair Dan means: the two at the exit / gate on the way out) become **the fighting tutorial**:
   - They block the way out and engage her. Reuse the existing fighting system (the "fluid fighting" from Round 8 /
     the fight demo): its moves, blocks, dodge, hit reactions.
   - **Teach the controls as they're needed**, the same way traversal is taught (first-use prompts, no permanent legend):
     attack, block, dodge, then the finishing move. Keep it short and forgiving — a tutorial, not a challenge.
   - **Nonlethal, always:** the Scree are knocked down / dazed, not killed. The finisher becomes a **takedown** that
     leaves them on the ground, stunned, alive (existing clips: knocked down, dazed, hit reactions; they may sit up
     groggily and stay out of the way). No blood, no deaths, no cruelty. 7 fights to get past them, not to hurt them
     (scene 5's "Without hurting them" still holds).
   - If 7 is knocked down, a quick reset to the start of the fight (existing fight reset), no game over.
   - **When both are down, the city reveal (scene 6b) plays.** Same trigger rules as QF6: no point of no return, a
     watchdog, the fight can't be skipped past by walking around them, and it can't soft-lock (if something goes wrong,
     the reveal still starts).
   - Panther (still just "the cat" in Chapter 1) stays out of the fight — he keeps his distance and rejoins after.
   - No new dialogue unless Dan writes some; ambient grunts / effort sounds only.
3. **Checks + release** (20-minute rule): one run through the fight (win it, and once lose and retry), the city reveal
   after it, captures of each tutorial prompt, a takedown, both Scree down, and the reveal start. Publish a release;
   docs; commit + push; per-item status in "QF9 — status"; "For Dan to check (QF9)".
