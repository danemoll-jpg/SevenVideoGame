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
