# Models needed — 7: The Observer

Full reference prompts and design rules: claude.ai Project "7 - The Game", doc `7-character-model-reference.md`.

| Model | Status | Files | Notes |
| --- | --- | --- | --- |
| 7 (normal proportions) | v2 cleaned in Blender (`Art/Export/7_v2_mixamo.fbx`, 50k tris, 1.70 m); needs Mixamo | `Art/Reference/7_ref_regular_{front,back,side}.png` → `Art/Source/7_normal_v2.glb` | Polished metal cuff hair ties on both pigtails (`7_ref_hairties_back.jpg`; replaced the glowing orbs 2026-11-16); flat glowing dot on the forehead (never raised); monochrome; full-coverage outfit |
| Y Bot sparring opponent | Use Mixamo's built-in Y Bot | `Art/Mixamo/YBot/` | Neutral gray; demo only |
| Scree (Tunneler's workers / guards) | **Placeholder only** (`Scree_placeholder`: gray Y Bot + hood + robe, built in `FightDemoBuilderChapter1.cs`) — **needs Kyle's design** | — | Round 6; see section below |
| 7's saucer — exterior | **Procedural for now** (Round 6, `FightDemoBuilderChapter1.cs` → `BuildBay`) | — | Wide flat disc, glowing rim, **polished silver hull (Round 10, Dan: exterior = silver — replaces the dark hull from Kyle's art)**, belly iris + tractor beam (per `kyle_7_backpack_saucer`, `kyle_7_on_hull`) |
| Cat (companion) | Model in: `Art/Source/cat/cat_v1.glb` (unrigged) — rig in Round 3 | `Art/Source/cat/` | Black; see section below |

## Cat (7's companion) — Round 3
Status: **model in** (`cat_v1.glb`, from `Art/Reference/cat_ref_34.png`) — rigging/animation in Round 3. Save the image as `Art/Reference/cat_ref_34.png`
(and optionally `cat_ref_side.png`), the model as `Art/Source/cat/cat_v1.glb` (or Tripo rigged FBX + clips).
Known design: **black**, playful, intelligent, changes size. Nothing else is decided (no extra markings invented).

Image prompt (3/4 view):
```
Character model reference, a single sleek black domestic cat, full body from nose to tail tip, centered, nothing cropped. Strictly black-and-white grayscale, no color. Flat even studio lighting, no cast shadows, plain light gray background. Clean painterly concept-art style. Standing on all four legs in a neutral 3/4 front view, legs slightly apart, tail raised and curving up away from the body, ears up, eyes open. Short glossy black fur with a soft dark-gray sheen so the body shape reads, pale gray eyes, alert and intelligent expression, elegant proportions.
```
Optional second view: same prompt with "side profile view" instead of "3/4 front view".
Negative: `color, cartoon, chibi, cropped tail, cropped paws, collar, props, shadows, multiple cats`.
Tripo tips: keep the tail and legs clear of each other so they don't fuse; texture on.

## Ratwing — Round 4
Model in: `Art/Source/ratwing/ratwing_v1.glb` (Tripo, unrigged, from Dan). Wings are separate limbs from the back →
rigged in Blender (Rigify + wing chains), not Mixamo. Reference: `Art/Reference/kyle_ratwing.jpg`.

## Scree — Round 6 (placeholder, needs a design)
The Scree are an oppressed alien people working (and sometimes guarding) for Tunneler in the Moon mines — **not evil**.
No design exists yet. Chapter 1 uses `Scree_placeholder`: Mixamo Y Bot in gray with a hood (face in shadow) and a
robe, Humanoid idle/walk from 7's clips; one guards the ancient hall (carries a white lamp), two stand at the gate,
two work in the impound bay. **Ask Kyle for a concept** (front + side, grayscale); then the usual pipeline
(image → model → Blender cleanup → Mixamo rig → Humanoid). Must stay readable as people, never as monsters.

## 7's saucer exterior — Round 6 (procedural for now)
Built procedurally in the scene builder: ~12 m wide flat disc, light belly, rim band with a white glow ring, dark
glossy upper hull rising to a low dome, a belly glow ring, a hinged ramp on the +X side. Clamped on a cradle in the
impound bay (silver since Round 10); lifts off at the end of scene 8. Replace with a modelled ship later (Kyle: `kyle_7_backpack_saucer`,
`kyle_7_on_hull`); the interior stays "larger inside".

## Scree — concept in (2026-11-17)
Reference: `Art/Reference/scree_concept.jpg` — **not Kyle's drawing**; a reference he sent. Dan and Kyle agreed the Scree get redesigned **in Kyle's style** (smooth, elegant, monochrome, glowing accents). Insectoid worker: upright on two digitigrade insect legs
with clawed feet, long segmented abdomen behind with folded wing covers, slim humanoid torso, **four arms** with
three-clawed hands, compound-eyed insect head with small mandibles and a short antenna, breathing hose from the mouth to
a twin-tank backpack. An oppressed people, not villains — neutral / weary, not menacing.
Next: Dan generates a clean Kyle-style reference (front view, limbs apart) from it → Tripo → `Art/Source/scree/scree_v1.glb`.
**Rig in Blender, not Mixamo** (four arms + insect legs + abdomen; same approach as Ratwing). Replaces `Scree_placeholder`.
**Status 2026-11-17: model in — `Art/Source/scree/scree_v1.glb`** (Tripo, from `Art/Reference/scree_ref_34.png`; unrigged,
~2M tris, one 4K texture, 0.98 units tall). Checked in chat (`Docs/Previews/dan/scree_v1_check.png`): clean limbs, four arms
apart, mask/hose/tanks, smooth Kyle-style shell. Cleanup notes: **the abdomen + wing covers point back-and-sideways at
~45° (top view) — rotate them about the waist so they extend straight behind**; the head is turned slightly — straighten
it in the rest pose; keep the hose and antenna out of decimation; wing covers are solid (fine). Rig in Blender (four
arms, digitigrade legs, abdomen chain, head/antenna), not Mixamo.
**Height: 1.9 m** (Dan, 2026-11-17: a little taller than humans). Built in QF4.
**Built in QF4 (2026-11-17): `Tools/Blender/cleanup_scree.py` → `Art/Export/scree/scree_v1_rigged.fbx`** (1.90 m crown,
45k tris, Humanoid-compatible rig + lower arm pair / abdomen / antenna bones, clips `work` + `alert`; 7's idle / walk /
look_around retargeted in Unity). **Scree design approved 2026-11-17 — tail tucked down** (Dan): the abdomen + wing
covers are straightened behind and tucked 42° below horizontal about the waist (`ABD_TUCK`, range 35–50°), the covers
lying along it; clears the legs in idle / walk / work (centre-line distance ≥ 0.278 m). Previews `Docs/Previews/qf4/scree/`.

## Round 9 — Chapter 1 traversal, the lunar door, the first vision (2026-11-17)
- **No new character models needed.** Ledges, broken catwalk, pipe run, bulkhead, ruin terraces, the fallen slab, Kyle's
  floating dodecahedron and the lunar door chamber are all built procedurally (`FightDemoBuilderTraversal.cs`,
  `FightDemoBuilderLunarDoor.cs`); the door chamber follows `Art/Reference/ref_lunar_door.png`, in ruins.
- **The first vision is a video, not a model:** `Art/Video/vision_01_door.mp4` (12 s, 1920x1044, grayscale, no audio;
  copied to `Unity/Assets/Video/`) — two tall pale figures before the door whole; one touches the round three-spoke
  mechanism; the leaves part along the S-seam; they walk into a bright passage. The in-game door matches it (7 touches
  the same mechanism). The pale figures exist only in the video (no game model planned).
- Optional later (Mixamo, Dan): a dedicated "climb down to hang" clip (the lowering into a hang is procedural) and a
  waist-high "Climbing"/vault clip (the mantle uses a trimmed 7_climb_up — `alt/7_climbing_a/_b` did not fit).

## Round 10 — the galley scene, the hand-held dodecahedron, the silver saucer (2026-11-17)
- **7's saucer exterior = silver** (Dan, 2026-11-17): polished silver metal (light-grey base, metallic 0.96, smoothness
  0.94), slightly darker brushed-silver underside with a dark band under the rim and seam lines; glowing rim, belly glow
  and tractor-beam iris kept. **Replaces the dark hull from Kyle's art.** A realtime reflection probe rides with the ship
  (`SaucerProbe.cs`). Still procedural (`FightDemoBuilderChapter1.cs`).
- **Dodecahedron (hand-held)**: ~15 cm, solid pale stone / crystal with thin glowing edges, procedural
  (`ElarimShape.cs`, used on the Moon pedestal and on the galley table).
- **OPEN — the Elarim marking** (Kyle hasn't designed it yet): placeholder glyph = a small circle with three short
  curved strokes rising from it, like a sprout ("First Garden Age"), `Art/Marks/elarim_mark_placeholder.png` (made by
  `Tools/Model/make_elarim_mark.py`). To swap: replace that PNG (white on transparent, square) or point
  `MoonLayout.ElarimMarkFile` at a new file in `Art/Marks/`, then Build everything.
- Galley set (table, two tulip seats, plate, fork, knife, cat bowl, porthole with the Moon outside): procedural
  (`FightDemoBuilderGalley.cs`).

## Villains with concept art (2026-11-17) — not scheduled yet
| Model | Concept | Notes |
| --- | --- | --- |
| Tunneler | `Art/Reference/kyle_tunneler.jpg` | The Moon's villain. Ring-segmented body, spiral drill tip on the head. Needs a clean A-pose reference (Kyle-style) → Tripo → Blender rig (Humanoid-compatible). **Drill head moves as he talks (Dan, 2026-11-17):** the cone head gets its own bone (separated from the shoulders by a visible groove); in dialogue it twists/spins driven by the voice clip's loudness, with a slight forward nod on stressed words — no face, no lip-sync. Reference in: `Art/Reference/tunneler_ref_front.png` (drill sits in a collar socket) → Tripo → `Art/Source/tunneler/tunneler_v1.glb`. |
| Wormmaster | `Art/Reference/kyle_wormmaster.jpg` | Segmented worm-humanoid + small worms. Placement unapproved. |
| The Mole | `Art/Reference/kyle_the_mole.jpg` | Early version of the Tunneler (Dan) — not a separate model. |
| Ratwing (update) | `Art/Reference/kyle_ratwing_sketch.jpg` | Already modeled; Kyle's sketch matches the hanging pose. |

## Elarim — Venus (2026-11-17)
Design (Dan / Kyle): twice human height (~3.5 m), all male, superhero proportions (Atlas / Superman), handsome and
flawless, shining polished armor, a soft glow all over with even brighter glowing eyes, long flowing capes, some carry a
sword. Very intimidating, but good guardians. Pipeline: Dan generates a Kyle-style A-pose reference (cape hanging
straight, no sword in hand) → Tripo → `Art/Source/elarim/elarim_v1.glb`; sword generated separately as a prop →
`Art/Source/elarim/elarim_sword_v1.glb`. References in (2026-11-17): `Art/Reference/elarim_ref_apose.png` (A-pose, cape hanging, glowing eyes and armor seams) and `Art/Reference/elarim_sword_ref.png` (prop). Glow, eyes and cape motion are added in Unity (emissive + cloth). One model
serves guardian, attendant and workers (variations by cape / headpiece / scale).

## Models in — 2026-11-18 (Dan, Tripo)
- `Art/Source/elarim/elarim_v1.glb` — Elarim (guardian / attendant / workers), textured, 1.86 M tris (decimate), single
  mesh with the cape fused to the body. Plan: Blender — split the cape off as its own mesh (cloth in Unity), scale to
  ~3.5 m, custom Humanoid-compatible rig like the Scree (not a Mixamo upload, so the cape isn't weighted to the legs);
  animate with Mixamo clips downloaded on Y Bot → `Art/Mixamo/elarim/`.
- `Art/Source/elarim/elarim_sword_v1.glb` — sword prop, textured, 1.35 M tris (decimate); glowing edge = emissive in Unity.
- `Art/Source/tunneler/tunneler_v1.glb` — Tunneler, 2.0 M tris, **geometry only (no UVs, no texture)** — fine for now:
  a grey stone triplanar material in Unity (monochrome). Drill head in its collar socket → own bone for the talk motion.
  Custom Humanoid-compatible rig; clips on Y Bot → `Art/Mixamo/tunneler/`. Height still undecided (Dan).

## Wormmaster — reference prompt given to Dan (2026-11-18)
From `Art/Reference/kyle_wormmaster.jpg`: tall, slender, smooth worm-humanoid; no face (the head is a rounded worm tip
with ring segments); long tapering boneless arms with no hands; legs tapering to points; pale glossy segmented skin.
The small burrowing worms are a **separate** model (prompt given too). Expected files:
`Art/Reference/wormmaster_ref_front.png`, `worm_ref.png` → Tripo → `Art/Source/wormmaster/wormmaster_v1.glb`,
`Art/Source/wormmaster/worm_v1.glb`. Rig: custom Humanoid-compatible (Blender) + extra bones along the arms so they
can curl; head tip bone pulses with his voice. Worms: simple bone chain (wriggle / rise / burrow), no Mixamo.

## In — 2026-11-18 (Dan)
- `Art/Source/wormmaster/wormmaster_v1.glb` (textured, 1.93 M tris) and `Art/Source/wormmaster/worm_v1.glb` (textured,
  1.92 M tris) — decimate; rigs as described above. (The Tunneler re-upload was identical: still untextured → stone material.)
- Mixamo clips (Y Bot / Mutant, FBX for Unity, no skin): `Art/Mixamo/elarim/` — `elarim_idle`, `_talk`, `_point`,
  `_walk`, `_kneel`, `_sword_idle`, `_carry`, `_lift`; `Art/Mixamo/tunneler/` — `tunneler_idle`, `_walk`, `_roar`,
  `_swipe` (Mixamo's Mutant set — a slightly larger skeleton; retarget through Humanoid as usual).

## Hover bike — concept in (2026-11-18)
Kyle's design: `Art/Reference/kyle_hover_bike.jpg` (riding shots: `kyle_7_riding_city`, `kyle_7_riding_fog`).
Grayscale only (ignore the colours in the small renders). Pipeline: Dan generates a clean 3/4 model reference
(prompt given in chat) → Tripo → `Art/Source/hoverbike/hoverbike_v1.glb`. Not scheduled in a round yet; 7 needs a
seated riding pose (Mixamo "Motorcycle" / "Driving" style clip) when it is.
- `Art/Source/hoverbike/hoverbike_v1.glb` — hover bike (Dan, Tripo from `hoverbike_ref.png`, 2026-11-18): textured,
  1.92 M tris (decimate), single mesh. Forked end = front. Scale so the saddle suits 7 (bike ≈ 2.4 m long); add empties
  for the seat point and the two fairing grips (hand IK targets); hover glow on the underside vents in Unity.

## Terminator-type villain (no name yet) — concept in (2026-11-19)
Kyle's sketch: `Art/Reference/kyle_terminator_villain.jpg` (see the reference README for the description). The asteroid's
antagonist in `Docs/STORY.md`. Not scheduled; when needed: a clean grayscale A-pose reference in Kyle's style → Tripo →
Humanoid rig (cape on its own bones like the Elarim). Name to be decided by Dan.

## Cetaceans — Europa (2026-11-19)
Prompts: claude.ai project doc `claude/7-cetacean-model-reference.md`. Spec: `Docs/CHAPTER_EUROPA.md`. Not scheduled in a round yet.
- `Art/Source/cetacean/whale_v1.glb` — large Cetacean (the keeper + the huge one that passes under the bike). Dan, Tripo
  from a side view (Tripo has no top-view slot), 2026-11-19: textured, 1.94 M tris (decimate), single mesh, 1 unit long
  along X, head toward −X. **Tail flukes already came out flat (horizontal)** — checked 2026-11-19, no fix needed; just
  confirm in a preview render. Scale huge (≈ 25–30 m) so its size reads as it passes beneath 7's bike.
- `Art/Source/cetacean/dolphin_v1.glb` — small Cetacean (the guide; scaled down ≈ 0.6× for the young one in the Europa
  vision). Same pipeline: textured, 1.95 M tris (decimate), head toward −X, flukes flat. Scale ≈ 2.5–3 m.
- **No Mixamo** (not humanoid): rig each with a simple spine bone chain head→tail plus a bone per pectoral fin; drive the
  swim procedurally in Unity (sine wave down the spine, up-and-down tail beat, slow fin sway). Keeper "talking" = a slow
  head/fin movement synced to the voice clip's loudness — no mouth animation.
- Not modelled (Dan, 2026-11-19): the bubble helmet (a simple transparent sphere/shader in Unity — glass domes break in
  generation) and Europa's rune (icosahedron, generated in code like the cube and dodecahedron).

## Viper — Mars (2026-11-19)
Kyle's design: `Art/Reference/kyle_viper.jpg` (labelled "pit VIPER"; **his name is Viper** — Dan, 2026-11-19). Clean A-pose reference in: `Art/Reference/viper_ref_front.png` (Dan, 2026-11-19). Mars antagonist (`Docs/CHAPTER_MARS.md`). Not scheduled.
Tall, lean, long-armed reptilian humanoid; hooded snake head (cobra-like hood flaring behind the head), glowing slit
eyes, long fangs; a band of ribbed ventral scales from the throat down the chest and belly; long arms with large hands;
long legs ending in dark, pointed, clawed feet. Pipeline: Dan generates a clean A-pose reference in Kyle's style (prompt
given in chat, mouth closed) → Tripo → `Art/Source/viper/viper_v1.glb` → Mixamo auto-rig (Humanoid). Talking: a jaw bone
opens with his voice's loudness (no lip-sync); the hood can flare on angry lines. Suggested height ~2.3 m.

## Reptilians — Mars townspeople (2026-11-19)
One base model reused for the guard, the worker, the caretaker, families and traders (scale ±10 %, a few material
tones; guards get a simple chest plate / helmet prop attached in Unity). Related to Viper but clearly not him: lizard
heads (no cobra hood, no fangs showing), sturdier, ordinary people, simple fitted work clothes — **no capes or long
robes** (the guardian's cape cost four rounds). Prompt given in chat. **Reference in (Dan, 2026-11-19): `Art/Reference/reptilian_ref_front.png`** — friendly lizard head, sleeveless tunic, knotted sash, cross-body satchel at the left hip, wrapped forearms, trousers, three-toed feet. When rigging: the sash's hanging tail and the satchel go on their own short bones (or are trimmed / pinned to the hip) so they never stretch with the thigh or pass through the leg. Expected: the reference
→ Tripo → `Art/Source/reptilian/reptilian_v1.glb` → Code makes `Art/Export/reptilian/reptilian_for_mixamo.fbx` → Mixamo.

## Mars models rigged — in (Dan, 2026-11-19)
- `Art/Mixamo/viper/viper_body_mixamo.fbx` and `Art/Mixamo/reptilian/reptilian_body_mixamo.fbx` — Mixamo auto-rig + skin
  (from S1 / S2's `*_for_mixamo.fbx`). Animation clips: Dan downloads them on these rigged bodies (list in chat,
  file names `viper_<clip>.fbx` / `reptilian_<clip>.fbx` in the same folders).

## Mars animation clips (Dan, 2026-11-19) — Humanoid retarget, reuse existing clips
- **Viper** (nothing new): idle `tunneler/tunneler_idle`, talk `elarim/elarim_talk`, walk `tunneler/tunneler_walk`,
  command `elarim/elarim_point`, angry `tunneler/tunneler_roar`, hit `YBot/ybot_hit_body` / `ybot_hit_head`, subdued
  `YBot/ybot_knocked_down` → `ybot_dazed` (or `elarim/elarim_kneel`).
- **Reptilians**: new from Dan (animation only, on the rigged Reptilian): `reptilian/reptilian_walk.fbx` (Mixamo "Orc
  Walk" — check it reads as an ordinary person, not a brute; slow / damp it if too heavy) and `reptilian/reptilian_wave.fbx`
  ("Waving"). Reused: idle `elarim/elarim_idle` (**guards use the idle too** — Dan found no guard stance), talk
  `elarim/elarim_talk`, point `elarim/elarim_point`, work `elarim/elarim_carry` / `elarim_lift` / `7/7_touch_wall`.
- Viper's voice: **saved by Dan in ElevenLabs as `Viper`** (2026-11-19).

## The Machinist — asteroid (2026-11-20)
Kyle's sketch `Art/Reference/kyle_terminator_villain.jpg`, adapted (Dan): no cape; the arm cannon → a heavy maintenance tool
arm (cutter / clamp). Spec `Docs/CHAPTER_ASTEROID.md`. Pipeline: Dan generates a clean A-pose reference (prompt in chat) →
`Art/Reference/machinist_ref_front.png` → Tripo `Art/Source/machinist/machinist_v1.glb` → Code makes `..._for_mixamo.fbx` →
Mixamo → `Art/Mixamo/machinist/machinist_body_mixamo.fbx`. ~4 m tall. No face: the visor slit glows with his voice; small,
precise head adjustments. Clips: reuse `tunneler_walk` / `tunneler_idle` / `tunneler_swipe` / YBot (slowed, deliberate).
Code-built (hard-surface, scripted): the maintenance machine, the security machines, the mechanical bird, the arc blades
(two curved crescent blades of light on short hilts), the cuboctahedron rune, the service-station clamps, the cradle.
- **Reference in (Dan, 2026-11-20): `Art/Reference/machinist_ref_front.png`** — faithful to Kyle's sketch (dome helmet + slit
  visor, flared collar, two exhaust stacks, chest core, gauge belt, the front plate with the crescent and glyphs, piston legs,
  the claw / cutter tool arm on his right, a mechanical hand on his left). When rigging: **the long front plate hangs between
  his legs** — give it its own hinge bone at the belt that swings forward with the stepping thigh (or shorten it to stop above
  the knees) so it never passes through a leg (the guardian's cape lesson). The tool arm's claw on its own bone (opens / closes).

## 7's arc-blade clips — in (Dan, 2026-11-20, Mixamo, animation only, on 7's rig) — `Art/Mixamo/7/blade/`
`7_blade_fight_idle` ("Sword Fight One"), `7_blade_draw` ("Draw Sword 1"), `7_blade_draw_great` ("Draw A Great Sword 1"),
`7_blade_sheath` ("Withdrawing Sword"), `7_blade_sheath_great` ("Sheath A Great Sword 2"), `7_blade_run` ("Run With Sword"),
`7_blade_combo` ("One Hand Sword Combo"), `7_blade_slash_in` ("Stable Sword Inward Slash"), `7_blade_slash_out` ("Stable Sword
Outward Slash"). They're single-sword clips; the arc blades are a pair (one curved blade per hand, spec: Kyle's
`kyle_7_blades_sketch.jpg`) — the second blade rides the off hand; mirror clips where it helps the combos alternate hands.

## Biavians — the mothership (2026-11-21)
Kyle's design in: `Art/Reference/kyle_biavian_nela.jpg` (file named "nela"): a smooth elongated bald head with a ridge along the
crown, large dark almond eyes with heavy lids, a small nose, a calm face; slender; a pale fitted suit with **dark long sleeves**, a
**dark V-shaped panel** down the centre of the chest narrowing to a point above the waist, a **round medallion with a glyph** on the
left chest, a **wide dark belt with a square buckle** (a circle and four dots); the lower body isn't shown. One model for the Guide
and residents (sizes / tones / medallion glyph vary). Pipeline: Dan generates a full-body A-pose reference (prompt in chat; fitted
trousers, no robe / cape) → `Art/Reference/biavian_ref_front.png` → Tripo `Art/Source/biavian/biavian_v1.glb` → Code makes
`..._for_mixamo.fbx` → Mixamo → `Art/Mixamo/biavian/biavian_body_mixamo.fbx` → replaces the Zorp stand-in (Round 17).

## Cattalus — Panther's homeworld (2026-11-21, Round 18)
Spec: `Docs/CHAPTER_CATTALUS.md`. Built with stand-ins until these are in; each drops in by file name.

**City cats (citizens, Juniper, the Steward, the Announcer):** no new models. Code builds them from Panther's rigged cat
(`cat_v1`) with new fur textures / markings, size and proportion variations, long-haired fur shells and small accessories,
reusing his clips.

**1. Duchess, the champion (optional, recommended)** → `Art/Reference/duchess_ref_34.png` → Tripo (rigged, quadruped)
`Art/Source/duchess/duchess_v1.glb`. Until then: a long-haired white variant of Panther's cat.
```
Character model reference, a single large fluffy long-haired white domestic cat, full body from nose to tail tip, centered, nothing cropped. Strictly black-and-white grayscale, no color. Flat even studio lighting, no cast shadows, plain light gray background. Clean painterly concept-art style. Standing on all four legs in a neutral 3/4 front view, legs slightly apart, a big plumed tail raised and curving away from the body, ears up, eyes open. Thick soft white fur with a full ruff round the neck and feathered paws, pale eyes, calm, gracious, slightly sleepy expression, heavy elegant build, a thin smooth metal band round one foreleg.
```
Negative: `color, cartoon, chibi, cropped tail, cropped paws, clothing, props, shadows, multiple cats`.

**2. Duchess's hover racer** → `Art/Reference/cat_racer_ref.png` → Tripo `Art/Source/cat_racer/cat_racer_v1.glb` (no rig).
Until then: procedural.
```
3D model reference of a single small hover racing vehicle sized for a large cat rider, 3/4 front view, centered, whole vehicle visible. Strictly black-and-white grayscale, no color. Flat even studio lighting, plain light gray background. A low sleek teardrop hull with no wheels, a padded round saddle well in the middle for a crouching cat, a curved low windscreen fairing at the front, two small round thruster pods at the back, a slim racing-number plate on the side, soft rounded retro-futurist styling, playful and elegant, smooth panels with fine seams.
```
Negative: `color, wheels, rider, cat, person, text, logos, shadows, cropped`.

**3. The championship trophy** (the sphere is separate, made in code from Kyle's `kyle_7_sphere.jpg`) →
`Art/Reference/cat_trophy_ref.png` → Tripo `Art/Source/cat_trophy/cat_trophy_v1.glb`. Until then: procedural.
```
3D model reference of a single ornate championship trophy, front 3/4 view, centered, whole object visible. Strictly black-and-white grayscale, no color. Flat even studio lighting, plain light gray background. A tall stepped round pedestal of small carved columns wrapped in rope like miniature scratching posts, two curling side handles shaped like cat tails, small paw-print reliefs round the base, and at the top an open decorative cradle of four slender curved prongs holding nothing, sized to cradle a ball the size of two hands. Polished metal and carved stone.
```
Negative: `color, ball, sphere, text, logos, shadows, cropped, people, cats`.

**4. The colour reference: 7 beside giant Panther in the meadow** (Dan approves this before any meadow clip) →
`Art/Reference/cattalus_meadow_color_ref.png`. Attach `7_ref_regular_front.png` and `cat_ref_34.png` as character references if
the tool allows. The bracketed colours are proposals; Dan sets 7's colours.
```
Cinematic film still in full natural color, warm golden late-afternoon light, soft haze. A wide meadow of tall golden-green grass with drifting seedheads, low rolling hills along the horizon, and a single old tree leaning strongly to one side with one long low branch reaching out almost level with the ground. Beneath the tree a young woman sits leaning against the shoulder of an enormous black cat lying on its belly with its forelegs stretched out in front; the cat is so large that its shoulder would stand above her head. She rests the side of her head against its fur, one hand on its foreleg, knees loosely drawn up, eyes closed, peaceful. The woman: slim and athletic, black hair in two high pigtails tied with small pearl-silver ball ties, a small white dot on her forehead, light freckles; a fitted long-sleeved [deep charcoal] bodysuit with thin [pale silver] seam lines over the shoulders, a segmented [silver] collar at the neck, fitted [charcoal] shorts with small flap pockets, tall armored [sand-gray] boots above the knee with knee plates and straps. The cat: a sleek short-haired black domestic cat at giant scale, glossy fur, [pale gold] eyes, head lowered beside hers, tail curving loosely through the grass near her feet, relaxed and content. Gentle painterly realism, tender and calm, no text.
```
Negative: `cartoon, anime, extra limbs, extra fingers, saddle, armor on the cat, collar on the cat, bared teeth, other animals, other people, text, watermark`.

**5. Videos (Dan, your video tool, image-to-video).** All 1920×1044-ready (Code converts), no speech generated, no text.

*Vision 7* → `Art/Video/vision_07_cattalus.mp4`, about 12 s, **grayscale**. Start frame: an image of the empty meadow and leaning
tree in grayscale, misty.
```
Grayscale, painterly, dreamlike memory, soft misty light, slow gentle camera drift. A broad meadow long ago, the same old tree leaning strongly to one side with one long low branch. A hooded traveler whose face stays hidden in shadow and soft blur kneels beside a simple open wooden box in the grass and gently places a smooth pale ball inside it. A short-haired cat walks up through the grass, sniffs the ball, then deliberately rests one paw on the grass beside the box. The traveler sits down beneath the tree and the cat settles against their side. Long, still hold on the quiet companionship. Monochrome, no speech, no text.
```

*The meadow cinematic* → `Art/Video/color/cattalus_meadow_01.mp4` … `_06.mp4`, **full colour**, about 12 s each. Start each from
the approved colour reference (`cattalus_meadow_color_ref.png`) or from the last frame of the previous clip, and keep the same
characters, tree, hill line and light direction. Shared style line, put at the start of each prompt:
`Cinematic, full natural color, warm golden late-afternoon light, soft haze, gentle painterly realism, the same young woman (black pigtails with pearl ties, charcoal bodysuit, tall armored boots) and the same black cat, the same leaning tree and low hills.`
1. `Wide unhurried shot. The woman walks into the tall golden grass with a small black cat trotting beside her, trailing her fingers through the seedheads. A seedhead drifts past the cat's nose and his eyes follow it. She glances down at him, smiling, says something softly. He springs after the seedhead and she jogs a few steps alongside, laughing.`
2. `Wide shot to show scale. The small cat bounds behind a grassy rise and disappears. The woman slows and turns, looking for him. A huge dark silhouette rises above the grass, and an enormous black cat walks out from behind the rise, graceful and relaxed, his shoulder higher than her head. She steps back in surprise, then laughs with delight and walks closer, looking up at him.`
3. `Medium shot. The enormous black cat lowers his head toward her. She places her palm against his cheek and he leans into it; she rubs his face with both hands, laughing. He steps back and drops into a playful crouch, tail swishing. She laughs, turns and runs; he starts after her.`
4. `Broad side-on tracking shot. She runs through the tall grass laughing, and the enormous cat bounds beside her, matching her pace. She changes direction and he takes a wide graceful curve around her; she disappears behind his shoulder for a moment, then reappears laughing. Playful, gentle, no teeth, no hunting.`
5. `They reach the leaning tree. She slows to a walk, catching her breath, smiling. The enormous cat turns and lowers himself onto his belly in the grass, forelegs stretched out in front. She rests a hand on his shoulder and sits down beside him. Their breathing settles.`
6. `Low intimate three-quarter view, wide enough to show his size. She sits against his shoulder with her knees loosely drawn up and rests the side of her head against his fur, one hand on his foreleg. He lowers his head beside hers; his tail curves loosely through the grass near her feet. Her eyes close. Very still: only breathing, a small ear twitch and the breeze moving the grass.`
Negative for all: `bared teeth, aggression, hunting, extra limbs, extra fingers, saddle, collar, other people, other animals, text, watermark`.

**Cattalus references in (Dan, 2026-11-22):** `Art/Reference/duchess_ref_34.png` (long-haired white cat, ruff, plumed tail, a band on
the front left foreleg), `Art/Reference/cat_racer_ref.png` (teardrop hover racer, round padded bed, windscreen, two rear fan pods,
number 8), `Art/Reference/cat_trophy_ref.png` (paw-print base, rope-wrapped columns, urn with a rope bow, fluffy cat-tail handles,
four curved prongs over an open ring). Next: Dan makes the Tripo models (`Art/Source/duchess/duchess_v1.glb` rigged quadruped,
`cat_racer/cat_racer_v1.glb`, `cat_trophy/cat_trophy_v1.glb`). The sphere sits in the trophy's top ring, held between the prongs.

## Heaven — the new ending (QF31, 2026-11-23; Dan, Grok Imagine, full colour)
Six clips → `Art/Video/color/heaven_01.mp4` … `heaven_06.mp4` (any length ~6–10 s; Code converts). Same 7 and giant Panther as the
colour meadow (`cattalus_meadow_color.mov`); a **sunrise** world: a wide meadow with the old leaning tree, mountains behind, warm
golden-pink light from a low sun, mist in the valleys, birds. Start from a reference still, then each clip from the last frame of
the previous one. Shared style line at the start of every prompt:
`Cinematic, full natural color, a heavenly sunrise, warm golden and soft pink light from a low sun over distant mountains, mist in the valleys, dew on tall grass, gentle painterly realism, the same young woman (black pigtails with pearl ties, charcoal bodysuit, tall armored boots) and the same enormous black cat with golden eyes.`
Reference still (image): `...a vast meadow at sunrise with an old leaning tree, snow-capped mountains behind, the enormous black cat lying asleep in the grass with the young woman curled asleep against his chest, both eyes closed, peaceful, birds in the sky.`
1. `Extreme close-up of the enormous black cat's closed eye, warm sunrise light on his glossy fur and whiskers. Slowly the eye opens, revealing a golden iris that catches the light. Stillness, then a slow blink.`
2. `Close-up of the young woman's face resting against the black cat's fur, her eyes closed, cheeks wet with tears. Her eyes slowly open, she blinks in the warm light, confused, then wonder spreads across her face as she realises where she is.`
3. `Medium shot. She sits up in the dewy grass beside the enormous black cat as he lifts his head toward her. She takes his face in both hands, laughing and crying at once, tears of joy; he presses his head into her hands and closes his eyes contentedly.`
4. `Wide shot. She jumps up laughing and runs through the tall sunlit grass; the enormous cat bounds playfully beside her, matching her pace, circling her; birds lift from the grass around them. Joyful, gentle, no teeth.`
5. `They slow beneath the leaning tree; he lies down on his belly and she sits against his shoulder, resting her head against his fur, both at peace, the sun rising behind the mountains. The camera begins to rise slowly.`
6. `The camera keeps rising, higher and wider, the two of them becoming small beneath the leaning tree in a vast golden meadow; the sun climbs over a range of mountains, mist glows in the valleys, a flock of birds crosses the sky, deer graze at the meadow's edge, a river shines in the distance. Breathtaking, peaceful, endless.`
Negative for all: `bared teeth, aggression, extra limbs, extra fingers, saddle, collar, other people, text, watermark`.
