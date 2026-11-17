# Models needed — 7: The Observer

Full reference prompts and design rules: claude.ai Project "7 - The Game", doc `7-character-model-reference.md`.

| Model | Status | Files | Notes |
| --- | --- | --- | --- |
| 7 (normal proportions) | v2 cleaned in Blender (`Art/Export/7_v2_mixamo.fbx`, 50k tris, 1.70 m); needs Mixamo | `Art/Reference/7_ref_regular_{front,back,side}.png` → `Art/Source/7_normal_v2.glb` | Polished metal cuff hair ties on both pigtails (`7_ref_hairties_back.jpg`; replaced the glowing orbs 2026-11-16); flat glowing dot on the forehead (never raised); monochrome; full-coverage outfit |
| Y Bot sparring opponent | Use Mixamo's built-in Y Bot | `Art/Mixamo/YBot/` | Neutral gray; demo only |
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
