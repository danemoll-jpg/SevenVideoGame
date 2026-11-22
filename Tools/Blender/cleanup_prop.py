"""Round 18: clean up a Tripo prop (no rig) for Unity - Duchess's hover racer, the championship trophy.

F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/cleanup_prop.py -- <repo> <name> <target_tris> <size_m> [rotz_deg] [preview_dir]
  name = cat_racer | cat_trophy: reads Art/Source/<name>/<name>_v1.glb, writes Art/Export/<name>/<name>_v1.fbx (+ Textures/<name>_v1_basecolor.jpg,
  grayscale 2048) and, with a preview dir, <name>_front34 / side / top PNGs.
Steps: join -> weld -> decimate to target -> rotate rotz about Z (the racer's nose to -Y = Unity +Z after the FBX axis conversion)
-> scale so the largest dimension is size_m -> origin at the bottom centre -> grayscale texture -> FBX (Y up, -Z forward).
The trophy also logs its top ring (the open ring that holds the sphere): centre and radius, measured from the mesh
(the highest horizontal ring of vertices below the prongs) -> Art/Export/cat_trophy/cat_trophy_ring.json.
"""
import bpy, bmesh, os, sys, math, json
from mathutils import Vector, Matrix
argv = sys.argv[sys.argv.index("--") + 1:]
ROOT, NAME, TRIS, SIZE = argv[0], argv[1], int(argv[2]), float(argv[3])
ROTZ = float(argv[4]) if len(argv) > 4 else 0.0
PREVIEW = argv[5] if len(argv) > 5 else None
SRC = os.path.join(ROOT, "Art", "Source", NAME, NAME + "_v1.glb")
OUT = os.path.join(ROOT, "Art", "Export", NAME); TEXD = os.path.join(OUT, "Textures"); os.makedirs(TEXD, exist_ok=True)
FBX = os.path.join(OUT, NAME + "_v1.fbx"); TEX = os.path.join(TEXD, NAME + "_v1_basecolor.jpg")
def log(*a): print("[cleanup_prop]", *a, flush=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SRC)
ms = [o for o in bpy.data.objects if o.type == "MESH"]
for o in bpy.data.objects: o.select_set(o.type == "MESH")
bpy.context.view_layer.objects.active = ms[0]
if len(ms) > 1: bpy.ops.object.join()
ob = bpy.context.view_layer.objects.active
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM"); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for o in list(bpy.data.objects):
    if o != ob: bpy.data.objects.remove(o)
ob.name = NAME
n0 = sum(len(p.vertices) - 2 for p in ob.data.polygons)
bm = bmesh.new(); bm.from_mesh(ob.data); bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6); bm.to_mesh(ob.data); bm.free()
mod = ob.modifiers.new("dec", "DECIMATE"); mod.ratio = min(1.0, TRIS / max(1, n0)); bpy.ops.object.modifier_apply(modifier="dec")
n1 = sum(len(p.vertices) - 2 for p in ob.data.polygons)
R = Matrix.Rotation(math.radians(ROTZ), 3, "Z")
co = [R @ v.co for v in ob.data.vertices]
mn = Vector((min(c.x for c in co), min(c.y for c in co), min(c.z for c in co))); mx = Vector((max(c.x for c in co), max(c.y for c in co), max(c.z for c in co)))
k = SIZE / max(mx - mn); ctr = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
for v, c in zip(ob.data.vertices, co): v.co = (c - ctr) * k
ob.data.update()
dims = (mx - mn) * k
log(f"{NAME}: {n0} -> {n1} tris, size x {dims.x:.3f} y {dims.y:.3f} z {dims.z:.3f} m (rotz {ROTZ:.0f})")
mat = ob.material_slots[0].material if ob.material_slots else None
if mat is not None and mat.node_tree:
    img = next((n.image for n in mat.node_tree.nodes if n.type == "TEX_IMAGE" and n.image), None)
    if img is not None:
        img2 = img.copy(); img2.scale(2048, 2048); px = list(img2.pixels)
        for i in range(0, len(px), 4):
            g = 0.2126 * px[i] + 0.7152 * px[i + 1] + 0.0722 * px[i + 2]; px[i] = px[i + 1] = px[i + 2] = g
        img2.pixels[:] = px; img2.filepath_raw = TEX; img2.file_format = "JPEG"; img2.save()
        for n in mat.node_tree.nodes:
            if n.type == "TEX_IMAGE": n.image = img2
        log("texture", TEX)
    mat.name = NAME + "_mat"
if NAME == "cat_trophy":
    # the open ring at the top that holds the sphere: the vertices in the upper part, the horizontal ring with the widest hole
    vs = [v.co.copy() for v in ob.data.vertices]
    H = max(c.z for c in vs); best = None
    # a continuous ring (vertices all the way round: >= 20 of 24 angle bins) - the four prongs above it cover only a few bins
    for z0 in [H * (0.45 + 0.005 * i) for i in range(90)]:
        sl = [c for c in vs if abs(c.z - z0) < H * 0.006]
        if len(sl) < 20: continue
        bins = {int(((math.atan2(c.y, c.x) + math.pi) / (2 * math.pi)) * 24) % 24 for c in sl}
        if len(bins) < 20: continue
        inner = min(math.hypot(c.x, c.y) for c in sl)
        if inner > H * 0.04 and (best is None or z0 > best[0]): best = (z0, inner)
    ring = {"centre": [0.0, 0.0, best[0] if best else H * 0.7], "inner_radius": best[1] if best else 0.1, "height": H}
    json.dump(ring, open(os.path.join(OUT, "cat_trophy_ring.json"), "w"), indent=1)
    log("trophy ring", ring)
bpy.ops.object.select_all(action="DESELECT"); ob.select_set(True); bpy.context.view_layer.objects.active = ob
bpy.ops.export_scene.fbx(filepath=FBX, use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE", path_mode="COPY", embed_textures=False)
log("exported", FBX)
if PREVIEW:
    os.makedirs(PREVIEW, exist_ok=True)
    scn = bpy.context.scene; scn.render.engine = "BLENDER_WORKBENCH"; scn.render.resolution_x = 420; scn.render.resolution_y = 420
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); scn.collection.objects.link(cam); scn.camera = cam
    cam.data.type = "ORTHO"; cam.data.ortho_scale = max(dims) * 1.2; c = Vector((0, 0, dims.z / 2))
    for nm, d in (("front34", Vector((0.7, -0.7, 0.35))), ("side", Vector((1, 0, 0))), ("top", Vector((0, 0, 1)))):
        cam.location = c + d.normalized() * max(dims) * 3; cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
        scn.render.filepath = os.path.join(PREVIEW, f"{NAME}_{nm}.png"); bpy.ops.render.render(write_still=True)
