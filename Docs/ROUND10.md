# Round 10 (queued — start after Round 9 is committed; move this into TODO.md as the Current Objective first)

## The galley scene — the end of Chapter 1 (Dan, 2026-11-17)
Script: `Docs/DIALOGUE.md` **scene 10 "The galley"** — use it exactly. It **replaces** the old scenes 10, 11 and 12 (first
meal, choosing to stay, discussing the vision); remove those from the chapter. Commit the repo as it is first.
All standing rules apply (monochrome, Kyle's fog/glow look, non-sexual presentation).

1. **The dodecahedron becomes a hand-held object she takes with her.** Change Round 9's item 8b: it is now ~15 cm across,
   a **solid pale stone / crystal dodecahedron with thin glowing edges** (Kyle's wireframe look carried by the edges),
   hovering above the pedestal and slowly turning. After the vision and the door scene, a context prompt X "Take":
   she picks it up and keeps it (it stays with her; no inventory UI needed). **One face carries a small engraved
   marking** — the Elarim mark. Kyle hasn't designed it yet: use a simple, clearly placeholder glyph (a small circle
   with three short curved strokes rising from it, like a sprout — "First Garden Age"), on one constant / texture so
   it's easy to swap. Record it as OPEN in `Docs/MODELS-NEEDED.md`.
2. **Voice lines:** generate every new line of scene 10 with ElevenLabs (7 with the expressive Round 7 settings; the cat as
   before). Reuse the existing files for "So what do you eat? Canned food?", "Chicken. But only free-range." and
   "Having experienced confinement, I feel strongly about it." Add them all to `Docs/VOICE-LINES.md`. Respect the
   script's pauses ("For a while, neither speaks", "A pause", "The ship's hum fills the silence") — real silence, a few
   seconds each, with only the ship's hum and eating sounds.
3. **The galley set:** add a small table with two seats in the galley nook (curved, retro-futurist, fits the ship), a
   plate and utensils for 7, a bowl for the cat on the table. **The window shows the Moon from outside, hanging against
   the dark** (the saucer has left — a pale, softly lit Moon in monochrome, Kyle's glow).
4. **Staging (an in-engine cutscene, player not in control):**
   - Open: 7 at the open refrigerator (the existing fridge), the cat **sitting on the table, tail curled around his
     feet**.
   - "I'll see what we have." → cut / ease to 7 seated at the table with her plate; the cat standing over his bowl,
     eating. 7 eats (fork to mouth, small bites), stops, looks at the dodecahedron beside her plate.
   - She moves the dodecahedron between them; the cat steps around his bowl, leans in, tilts his head to study one face,
     then another. "Turn it over" → she rotates it; the camera finds the marked face (close-up).
   - End: she sets it down marking-up, the cat returns to his food, she picks up her fork and eats. Fade softly.
   - **The cat's mouth moves only when he eats:** his head dips into the bowl (the bowl rim hides the mouth) with small
     chewing motions of the head/jaw; no mouth movement while he "talks". Ears: "one of his ears turns toward her" —
     an ear twitch/turn (add an ear bone to each ear if the rig has none).
   - Camera: calm, intimate framing (two-shot across the table, over-the-shoulder, a close-up of the dodecahedron and
     the marking). Frame 7 from the chest up or side-on at the table — no body-focused angles.
   - Animations: 7 uses the existing sit clips (`Seated Idle`, `Stand To Sit`) plus small keyed arm motions for eating,
     moving and rotating the object (layered on the seated pose). If Dan adds Mixamo clips (see below), use them.
     Cat: sit-on-table pose (from his sit clip, tail curled), eating loop, lean/step, head tilt — keyed in Blender if
     the rig lacks them.
5. **After the scene:** a quiet objective appears once — **"Set course for Venus"** — then the player has free
   exploration of the saucer as now (the cat roams, the lap moment, the gallery TV). The chapter is complete; **Continue**
   loads here. At 7's chair, a new context prompt X "Set course" shows a short, honest placeholder: the Moon falls away
   behind the window and a card "Chapter 2 — Venus (coming soon)", then returns to free exploration. Venus itself is
   not built this round.
6. **Story docs:** `Docs/STORY.md` already notes the new chapter end and the next world (Venus, the Elarim). Remove the
   old "discussing the vision / scene 12 placeholder" references from `TODO.md` / `CLAUDE.md`.
7. **Checks** (muted except a brief reported sound check of a few new lines; off-screen; never steal focus): rebuild;
   automated run of scene 10 start to finish — every line plays with its subtitle, the pauses hold, the cat's mouth
   hidden/still except while eating, the dodecahedron is in her hand / on the table where the script says, the marked
   face is visible in the close-up; captures of each beat (fridge, eating, the object between them, the marking
   close-up, "Venus, then", the set-course placeholder); monochrome 0. Publish a release, update `TODO.md` / `CLAUDE.md`,
   commit and push, report in ~20 lines.

### Optional Mixamo clips for Dan (7, FBX for Unity, 30 fps, without skin) → `Art/Mixamo/7/`
**Status 2026-11-17:** `7_open_fridge.fbx` is in (Mixamo "Opening") — use it at the refrigerator. **No eating clip exists
on Mixamo (Dan checked)** — key the eating yourself on top of `Seated Idle`: right hand brings the fork to the mouth in small,
unhurried bites (~4–6 s apart), left hand resting by the plate, slight head dip to meet the fork, a pause with the fork
held when she stops to look at the dodecahedron. Subtle and natural; no chewing face needed.
