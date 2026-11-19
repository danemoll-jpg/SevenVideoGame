"""Round 14: the Cetaceans of Europa (Dan's Tripo models).

Usage (repo root):
  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/cleanup_cetacean.py -- <repo root> [whale|dolphin|both] [preview dir]
In:  Art/Source/cetacean/whale_v1.glb (the keeper + the huge one under the bike), dolphin_v1.glb (the guide; x0.6 = the young
     one in the vision). Each: one textured mesh, ~1.9 M tris, 1 unit long along X, head toward -X, flukes already flat.
Out: Art/Export/cetacean/<kind>_v1_rigged.fbx (+ Textures/<kind>_v1_basecolor.jpg grayscale, cetacean_metrics.txt) and
     preview renders (side / top / front / three-quarter + a bend test) in Docs/Previews/r14/.
What it does: decimate (whale 30 k, dolphin 16 k tris), turn the head to -Y (= Unity +Z, forward), scale to LENGTH (whale 26 m,
dolphin 2.8 m), centre the body on the origin; then a simple rig - a spine chain of 8 bones from the head to the flukes
along the body's centre line (Spine0 at the head ... Spine7 at the flukes) and one bone per pectoral fin (FinL / FinR, at
the fins' roots) - with smooth weights: each vertex on the two nearest spine bones along the length, fin vertices (out to
the side, below the centre, in the fin band) blended onto their fin bone. No clips: Unity swims it procedurally
(Cetacean.cs: a wave down the spine, an up-and-down fluke beat, a slow fin sway; the keeper's head and fins move with
its voice's loudness).
"""
import bpy, os, sys, math
import numpy as np
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = os.path.abspath(argv[0]) if argv else os.getcwd()
KIND = argv[1] if len(argv) > 1 else "both"
PREVIEW = os.path.abspath(argv[2]) if len(argv) > 2 else os.path.join(ROOT, "Docs", "Previews", "r14")
LENGTH = {"whale": 26.0, "dolphin": 2.8}
TRIS = {"whale": 30000, "dolphin": 16000}
NSPINE = 8
_m = []


def log(*a):
    s = " ".join(str(x) for x in a); print("[cet]", s); _m.append(s)


def tris(ob):
    ob.data.calc_loop_triangles(); return len(ob.data.loop_triangles)


def co(ob):
    a = np.empty(len(ob.data.vertices) * 3, dtype=np.float64); ob.data.vertices.foreach_get("co", a); return a.reshape(-1, 3)


def set_co(ob, P):
    ob.data.vertices.foreach_set("co", P.astype(np.float32).ravel()); ob.data.update()


def gray_texture(ob, name, out_dir):
    tex_dir = os.path.join(out_dir, "Textures"); os.makedirs(tex_dir, exist_ok=True)
    path = os.path.join(tex_dir, f"{name}_basecolor.jpg")
    mat = ob.material_slots[0].material if ob.material_slots else None
    img = next((nd.image for nd in mat.node_tree.nodes if nd.type == "TEX_IMAGE" and nd.image is not None), None) if mat else None
    if img is None: log("no texture"); return
    img2 = img.copy(); img2.scale(2048, 2048)
    px = np.empty(2048 * 2048 * 4, dtype=np.float32); img2.pixels.foreach_get(px); px = px.reshape(-1, 4)
    g = 0.2126 * px[:, 0] + 0.7152 * px[:, 1] + 0.0722 * px[:, 2]
    px[:, 0] = px[:, 1] = px[:, 2] = g
    img2.pixels.foreach_set(px.ravel()); img2.filepath_raw = path; img2.file_format = "JPEG"; img2.save()
    for nd in mat.node_tree.nodes:
        if nd.type == "TEX_IMAGE": nd.image = img2
    mat.name = f"{name}_mat"
    log("texture", path)


def shoot(path, center, direction, ortho):
    scn = bpy.context.scene
    scn.render.engine = "BLENDER_WORKBENCH"; scn.display.shading.light = "STUDIO"; scn.display.shading.color_type = "TEXTURE"
    scn.render.resolution_x, scn.render.resolution_y = 900, 600
    cam = bpy.data.objects.get("pcam")
    if cam is None:
        cd = bpy.data.cameras.new("pcam"); cd.type = "ORTHO"; cam = bpy.data.objects.new("pcam", cd); scn.collection.objects.link(cam)
    scn.camera = cam; cam.data.ortho_scale = ortho
    dv = Vector(direction).normalized(); cam.location = Vector(center) + dv * 200; cam.data.clip_end = 500
    cam.rotation_euler = (-dv).to_track_quat("-Z", "Y").to_euler()
    scn.render.filepath = path; bpy.ops.render.render(write_still=True)


def do(kind):
    out = os.path.join(ROOT, "Art", "Export", "cetacean"); os.makedirs(out, exist_ok=True); os.makedirs(PREVIEW, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(ROOT, "Art", "Source", "cetacean", f"{kind}_v1.glb"))
    ms = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    bpy.ops.object.select_all(action='DESELECT')
    for o in ms: o.select_set(True)
    bpy.context.view_layer.objects.active = ms[0]
    if len(ms) > 1: bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active; ob.name = kind
    bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM'); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for o in list(bpy.context.scene.objects):
        if o.type != 'MESH': bpy.data.objects.remove(o, do_unlink=True)
    n0 = tris(ob)
    mod = ob.modifiers.new("dec", "DECIMATE"); mod.decimate_type = "COLLAPSE"; mod.ratio = min(1.0, TRIS[kind] / max(1, n0))
    bpy.ops.object.modifier_apply(modifier=mod.name)
    log(f"{kind}: decimated {n0} -> {tris(ob)} tris")
    gray_texture(ob, f"{kind}_v1", out)
    P = co(ob)
    # head toward -X in the source -> -Y here (Unity +Z): rotate +90 deg about Z: (x, y) -> (-y, x)
    P = np.stack([-P[:, 1], P[:, 0], P[:, 2]], axis=1)
    L = np.ptp(P[:, 1]); s = LENGTH[kind] / L
    c = (P.min(0) + P.max(0)) / 2
    P = (P - c) * s
    set_co(ob, P)
    head_y, tail_y = P[:, 1].min(), P[:, 1].max()
    log(f"{kind}: length {np.ptp(P[:, 1]):.2f} m (x{s:.2f}), width {np.ptp(P[:, 0]):.2f}, height {np.ptp(P[:, 2]):.2f}; head at y {head_y:.2f} (-Y = Unity forward)")
    # the centre line: the median height / side position in slices along the length
    ys = np.linspace(head_y, tail_y, NSPINE + 1)
    cl = []
    for i in range(NSPINE + 1):
        y = ys[i]; band = np.abs(P[:, 1] - y) < (tail_y - head_y) / NSPINE * 0.6
        q = P[band] if band.any() else P
        cl.append(Vector((float(np.median(q[:, 0])), float(y), float((q[:, 2].min() + q[:, 2].max()) / 2))))
    # the fins: widest points (|x|) in the front 15 - 45 % below the centre line
    t = (P[:, 1] - head_y) / (tail_y - head_y)
    fin_band = (t > 0.15) & (t < 0.45)
    arm_data = bpy.data.armatures.new(kind + "_rig"); arm = bpy.data.objects.new(kind + "_rig", arm_data); bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode='EDIT')
    prev = None
    for i in range(NSPINE):
        b = arm_data.edit_bones.new(f"Spine{i}"); b.head = cl[i]; b.tail = cl[i + 1]
        if prev is not None: b.parent = prev; b.use_connect = True
        prev = b
    fins = {}
    for side, sx in (("FinL", -1.0), ("FinR", 1.0)):
        m = fin_band & (P[:, 0] * sx > 0)
        q = P[m]
        if len(q) == 0: continue
        far = q[np.argsort(q[:, 0] * sx)[-max(1, len(q) // 40):]]
        tip = far.mean(0)
        k = int(np.clip(np.median((q[:, 1] - head_y) / (tail_y - head_y)) * NSPINE, 0, NSPINE - 1))
        root = Vector((cl[k].x + sx * 0.35 * abs(tip[0] - cl[k].x), float(tip[1]) - 0.08 * (tail_y - head_y), cl[k].z - 0.15 * np.ptp(P[:, 2])))
        b = arm_data.edit_bones.new(side); b.head = root; b.tail = Vector(tip.tolist()); b.parent = arm_data.edit_bones[f"Spine{k}"]
        fins[side] = (np.array(root), np.array(tip))
    bpy.ops.object.mode_set(mode='OBJECT')
    # weights
    for g in list(ob.vertex_groups): ob.vertex_groups.remove(g)
    groups = [ob.vertex_groups.new(name=f"Spine{i}") for i in range(NSPINE)]
    seg = t * NSPINE - 0.5
    for vi in range(len(P)):
        a = int(np.floor(seg[vi])); f = seg[vi] - a
        a0 = min(max(a, 0), NSPINE - 1); a1 = min(max(a + 1, 0), NSPINE - 1)
        if a0 == a1: groups[a0].add([vi], 1.0, 'REPLACE')
        else: groups[a0].add([vi], 1.0 - f, 'REPLACE'); groups[a1].add([vi], f, 'ADD')
    nfin = 0
    for side, (root, tip) in fins.items():
        g = ob.vertex_groups.new(name=side); sx = -1.0 if side == "FinL" else 1.0
        span = abs(tip[0] - root[0]) + 1e-6
        for vi in np.nonzero(fin_band & (P[:, 0] * sx > abs(root[0])) & (P[:, 2] < root[2] + 0.1 * np.ptp(P[:, 2])))[0]:
            w = float(np.clip((P[vi, 0] * sx - abs(root[0])) / span * 1.6, 0.0, 1.0))
            if w <= 0.01: continue
            g.add([int(vi)], w, 'REPLACE'); nfin += 1
            for gg in groups:
                try:
                    cur = gg.weight(int(vi)); gg.add([int(vi)], cur * (1.0 - w), 'REPLACE')
                except RuntimeError: pass
    log(f"{kind}: rig {NSPINE} spine bones head->flukes + {len(fins)} fin bones, {nfin} fin vertices")
    ob.parent = arm; md = ob.modifiers.new("arm", "ARMATURE"); md.object = arm
    # previews: side / top / front / three-quarter, then a bend test (the body curved, a fluke beat, fins down)
    Lx = LENGTH[kind]
    for vn, dv in (("side", (1, 0, 0.05)), ("top", (0.001, 0, 1)), ("front", (0, -1, 0.05)), ("q34", (0.7, -0.7, 0.35))):
        shoot(os.path.join(PREVIEW, f"{kind}_{vn}.png"), (0, 0, 0), dv, Lx * 1.15)
    bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode='POSE')
    for i in range(NSPINE):
        pb = arm.pose.bones[f"Spine{i}"]; pb.rotation_mode = 'XYZ'; pb.rotation_euler = (0.18 * math.sin(i * 0.9), 0, 0.12 * math.sin(i * 0.7 + 1))
    for side in fins:
        pb = arm.pose.bones[side]; pb.rotation_mode = 'XYZ'; pb.rotation_euler = (0, 0.5 if side == "FinL" else -0.5, 0)
    bpy.ops.object.mode_set(mode='OBJECT')
    shoot(os.path.join(PREVIEW, f"{kind}_bendtest.png"), (0, 0, 0), (0.7, -0.5, 0.5), Lx * 1.15)
    bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode='POSE')
    for pb in arm.pose.bones: pb.rotation_euler = (0, 0, 0)
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); ob.select_set(True); bpy.context.view_layer.objects.active = arm
    path = os.path.join(out, f"{kind}_v1_rigged.fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH", "ARMATURE"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", use_mesh_modifiers=False, mesh_smooth_type="FACE", add_leaf_bones=False,
                             use_armature_deform_only=False, bake_anim=False, path_mode="COPY", embed_textures=False)
    log("exported", path)


for k in (["whale", "dolphin"] if KIND == "both" else [KIND]): do(k)
with open(os.path.join(ROOT, "Art", "Export", "cetacean", "cetacean_metrics.txt"), "w") as f: f.write("\n".join(_m) + "\n")
log("done")
