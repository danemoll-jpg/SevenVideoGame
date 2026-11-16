"""Clean up 7's generated model for Mixamo / Unity.

Run headless from the repo root:
  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/cleanup_7.py -- [repo_root] [target_tris] [weld 1|0] [out_fbx] [ring|orb]

Input : Art/Source/7_normal_v2.glb   (never modified)
Output: Art/Export/7_v2_mixamo.fbx   (FBX, Path Mode Copy + embedded texture)
        Art/Export/Textures/7_v2_basecolor.jpg

Hair ties: "ring" (default, per Art/Reference/7_ref_hairties_back.jpg) builds a glossy metal cuff over each of the
model's original band ties; "orb" keeps the old glowing spheres from the source GLB (fallback).

Steps: merge seam duplicates -> decimate the body to ~target (hair ties kept undecimated) -> join the ties into the
body (two material slots: 7_body / 7_hairtie_metal or 7_hairtie_glow) -> scale to 1.70 m -> apply transforms -> origin at the feet
(floor centre) -> facing -Y in Blender (Blender "front"), which is what Mixamo expects.
"""
import bpy, bmesh, os, sys

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = os.path.abspath(argv[0]) if len(argv) > 0 else os.getcwd()
TARGET_TRIS = int(argv[1]) if len(argv) > 1 else 50000
WELD = (argv[2] != "0") if len(argv) > 2 else True  # weld seam duplicates before decimating
HAIRTIE = argv[4] if len(argv) > 4 else "ring"
TARGET_HEIGHT = 1.70

# Ring cuffs, in source units (Blender coords of the GLB: 0.979 m tall, faces -Y). Centre/axis fitted to the
# original band ties under the orbs; per side x is mirrored.
RING_CENTER = (0.044, 0.040, 0.957)
RING_AXIS = (1.0, 0.25, 0.30)   # roughly along the pigtail where it leaves the tie (mirrored in x)
RING_MAJOR = 0.0165              # centre radius of the cuff
RING_MINOR = 0.0030              # tube radius (thickness ~6 mm in source units)
RING_WIDTH = 2.4                 # stretch along the axis -> a wide band, not a thin wire

SRC = os.path.join(ROOT, "Art", "Source", "7_normal_v2.glb")
OUT_DIR = os.path.join(ROOT, "Art", "Export")
TEX_DIR = os.path.join(OUT_DIR, "Textures")
FBX = os.path.abspath(argv[3]) if len(argv) > 3 else os.path.join(OUT_DIR, "7_v2_mixamo.fbx")  # alt path for experiments
TEX = os.path.join(TEX_DIR, "7_v2_basecolor.jpg")
os.makedirs(TEX_DIR, exist_ok=True)


def tris(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def log(*a):
    print("[cleanup_7]", *a, flush=True)


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SRC)
body = bpy.data.objects["7_body"]
orbs = bpy.data.objects["7_hairtie_orbs"]
log("source tris body", tris(body), "orbs", tris(orbs), "hairtie mode", HAIRTIE)

# --- materials: friendly names, keep slots separate -------------------------------------------
body_mat = body.data.materials[0]
body_mat.name = "7_body"
if HAIRTIE == "orb":
    tie_mat = orbs.data.materials[0]
    tie_mat.name = "7_hairtie_glow"
    # FBX keeps diffuse + emissive colour but not glTF emissive strength; make both white so the orbs
    # read as white everywhere (real glow is set up in Unity).
    bsdf = tie_mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (1, 1, 1, 1)
        bsdf.inputs["Emission"].default_value = (1, 1, 1, 1)
        if "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = 1.0
    ties = orbs
else:
    from mathutils import Matrix, Vector
    bpy.data.objects.remove(orbs)
    tie_mat = bpy.data.materials.new("7_hairtie_metal")
    tie_mat.use_nodes = True
    bsdf = tie_mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (0.8, 0.8, 0.8, 1)   # polished silver/chrome, no glow
    bsdf.inputs["Metallic"].default_value = 1.0
    bsdf.inputs["Roughness"].default_value = 0.12
    tie_mat.diffuse_color = (0.8, 0.8, 0.8, 1)
    rings = []
    for side in (-1, 1):
        bpy.ops.mesh.primitive_torus_add(major_radius=RING_MAJOR, minor_radius=RING_MINOR,
                                         major_segments=48, minor_segments=16)
        r = bpy.context.active_object
        axis = Vector((side * RING_AXIS[0], RING_AXIS[1], RING_AXIS[2])).normalized()
        m = (Matrix.Translation((side * RING_CENTER[0], RING_CENTER[1], RING_CENTER[2]))
             @ axis.to_track_quat("Z", "Y").to_matrix().to_4x4()
             @ Matrix.Diagonal((1, 1, RING_WIDTH, 1)))
        r.data.transform(m)
        r.data.materials.append(tie_mat)
        rings.append(r)
    bpy.ops.object.select_all(action="DESELECT")
    for r in rings:
        r.select_set(True)
    bpy.context.view_layer.objects.active = rings[0]
    bpy.ops.object.join()
    ties = bpy.context.view_layer.objects.active
    ties.name = "7_hairtie_rings"
log("hair ties", ties.name, tris(ties), "tris, material", tie_mat.name)

# --- texture: save the base-colour image next to the export -----------------------------------
img = next(n.image for n in body_mat.node_tree.nodes if n.type == "TEX_IMAGE" and n.image)
img.name = "7_v2_basecolor"
# Write the original packed JPEG bytes (no lossy re-encode), then point the image at that file.
with open(TEX, "wb") as fh:
    fh.write(img.packed_file.data)
img.unpack(method="REMOVE")
img.filepath = TEX  # FBX Path Mode Copy / embed reads from here
img.reload()
log("texture saved", TEX, tuple(img.size))

# --- weld seam duplicates so decimation does not open cracks along UV seams ---------------------
if WELD:
    bm = bmesh.new()
    bm.from_mesh(body.data)
    nv = len(bm.verts)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
    log("welded verts", nv, "->", len(bm.verts))
    bm.to_mesh(body.data)
    bm.free()

# --- decimate the body only (hair ties stay round) ---------------------------------------------
body_target = TARGET_TRIS - tris(ties)
ratio = body_target / tris(body)
bpy.context.view_layer.objects.active = body
mod = body.modifiers.new("decimate", "DECIMATE")
mod.decimate_type = "COLLAPSE"
mod.ratio = ratio
mod.use_collapse_triangulate = True
bpy.ops.object.select_all(action="DESELECT")
body.select_set(True)
bpy.ops.object.modifier_apply(modifier=mod.name)
log("decimated body ratio %.5f -> %d tris" % (ratio, tris(body)))

# --- join the hair ties into the body (two material slots survive) ------------------------------
bpy.ops.object.select_all(action="DESELECT")
ties.select_set(True)
body.select_set(True)
bpy.context.view_layer.objects.active = body
bpy.ops.object.join()
ob = bpy.context.view_layer.objects.active
ob.name = ob.data.name = "7_v2"
bpy.ops.object.shade_smooth()
log("slots", [s.material.name for s in ob.material_slots])

# --- scale to 1.70 m, origin at feet (floor centre), apply transforms ---------------------------
ws = [ob.matrix_world @ v.co for v in ob.data.vertices]
mn = [min(w[i] for w in ws) for i in range(3)]
mx = [max(w[i] for w in ws) for i in range(3)]
s = TARGET_HEIGHT / (mx[2] - mn[2])
cx, cy = (mn[0] + mx[0]) / 2, (mn[1] + mx[1]) / 2
me = ob.data
for v in me.vertices:
    v.co.x = (v.co.x - cx) * s
    v.co.y = (v.co.y - cy) * s
    v.co.z = (v.co.z - mn[2]) * s
me.update()
ob.location = (0, 0, 0)
ob.rotation_euler = (0, 0, 0)
ob.scale = (1, 1, 1)
ws = [v.co for v in me.vertices]
mn = [min(w[i] for w in ws) for i in range(3)]
mx = [max(w[i] for w in ws) for i in range(3)]
log("final bbox min", [round(x, 3) for x in mn], "max", [round(x, 3) for x in mx], "scale", round(s, 4))

# Facing check: the hair ties sit at the back of the head; they must be on +Y (character faces -Y).
tie_idx = [i for i, sl in enumerate(ob.material_slots) if sl.material == tie_mat][0]
oy = [ws[vi].y for p in me.polygons if p.material_index == tie_idx for vi in p.vertices]
log("hair ties mean y %.3f (positive = behind = character faces -Y)" % (sum(oy) / len(oy)))
log("FINAL TRIS", tris(ob))

# --- export ---------------------------------------------------------------------------------------
bpy.ops.object.select_all(action="DESELECT")
ob.select_set(True)
bpy.ops.export_scene.fbx(
    filepath=FBX,
    use_selection=True,
    object_types={"MESH"},
    apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z",
    axis_up="Y",
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    path_mode="COPY",
    embed_textures=True,
    add_leaf_bones=False,
)
log("exported", FBX, "%.1f MB" % (os.path.getsize(FBX) / 1e6))
