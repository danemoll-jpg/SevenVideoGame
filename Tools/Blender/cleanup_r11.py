"""Round 11 models: the hover bike, the Elarim (guardian / attendant / workers) and the burrowing worm.

Usage (repo root):
  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/cleanup_r11.py -- <repo root> <bike|elarim|worm> [preview dir]
Outputs in Art/Export/<kind>/: <kind>_v1(.fbx|_rigged.fbx), Textures/<kind>_v1_basecolor.jpg (grayscale), <kind>_metrics.txt,
and preview renders (front / side / three-quarter / top) in the preview dir (Docs/Previews/r11 by default).

bike:   the Tripo model lies along X. The forked end is found from the geometry (at the forked tip the centre line is
        empty between the two prongs) and turned to face -Y (= Unity +Z, forward). Scaled to LENGTH_BIKE, bottom at 0,
        decimated to ~40k tris. Static mesh (the hover bob, lean and vent glow are done in Unity). Grip points are
        measured (the two recessed hollows in the fairing: the lowest surface points on each side of the saddle's
        front edge) and written to the metrics for the hand IK.
elarim: rigged to Dan's Mixamo skeleton: the armature of Art/Mixamo/elarim/elarim_idle.fbx is the skeleton Mixamo fitted
        to this very model, so the decimated mesh is aligned to it (height to HeadTop_End, centred on the hips) and
        skinned with automatic weights (nearest-bone fallback), then everything is scaled to HEIGHT_ELARIM (3.5 m).
        QF10: the cape hangs from its own two bones (CapeA / CapeB under Spine2), swung clear of the legs by ElarimCape in Unity. Exports a Humanoid-ready FBX
        with no clips (the eight Mixamo clips are imported separately and retargeted). The sword is exported as a prop.
worm:   lies along X; turned to face -Y, scaled to LENGTH_WORM, an 8-bone chain from tail to head with smooth weights
        along the body, and the keyed clips rise / wriggle / dive / idle_under.
"""
import bpy, bmesh, os, sys, math
import numpy as np
from mathutils import Vector, Matrix

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = os.path.abspath(argv[0]) if argv else os.getcwd()
KIND = argv[1] if len(argv) > 1 else "bike"
PREVIEW = os.path.abspath(argv[2]) if len(argv) > 2 else os.path.join(ROOT, "Docs", "Previews", "r11")
LENGTH_BIKE = 3.0          # QF10: 2.3 -> 3.0 m (Dan: it felt small; Kyle's sketch = a rider small against a long bike)
HEIGHT_ELARIM = 3.5
LENGTH_WORM = 9.0
TRIS = {"bike": 40000, "elarim": 60000, "worm": 30000, "sword": 6000}
FPS = 30
_m = []


def log(*a):
    s = " ".join(str(x) for x in a)
    print("[r11]", s); _m.append(s)


def tris(ob):
    ob.data.calc_loop_triangles(); return len(ob.data.loop_triangles)


def clean():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_glb(path):
    bpy.ops.import_scene.gltf(filepath=path)
    ms = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    ob = ms[0]
    for o in bpy.context.scene.objects:
        if o != ob and o.type != 'MESH': pass
    # apply parent transforms
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return ob


def co(ob):
    a = np.empty(len(ob.data.vertices) * 3, dtype=np.float64); ob.data.vertices.foreach_get("co", a); return a.reshape(-1, 3)


def set_co(ob, P):
    ob.data.vertices.foreach_set("co", P.astype(np.float32).ravel()); ob.data.update()


def decimate(ob, target):
    n0 = tris(ob)
    if n0 <= target: return
    mod = ob.modifiers.new("dec", "DECIMATE"); mod.decimate_type = "COLLAPSE"; mod.ratio = target / n0
    bpy.context.view_layer.objects.active = ob; bpy.ops.object.modifier_apply(modifier=mod.name)
    log(f"decimated {n0} -> {tris(ob)} tris")


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


def setup_render(res=(720, 720)):
    scn = bpy.context.scene
    scn.render.engine = "BLENDER_WORKBENCH"
    scn.display.shading.light = "STUDIO"; scn.display.shading.color_type = "TEXTURE"
    scn.render.resolution_x, scn.render.resolution_y = res
    cam = bpy.data.objects.get("pcam")
    if cam is None:
        cd = bpy.data.cameras.new("pcam"); cd.type = "ORTHO"
        cam = bpy.data.objects.new("pcam", cd); scn.collection.objects.link(cam)
    scn.camera = cam
    return scn, cam


def shoot(path, center, direction, ortho):
    scn, cam = setup_render()
    cam.data.ortho_scale = ortho
    dv = Vector(direction).normalized()
    cam.location = Vector(center) + dv * 30; cam.data.clip_end = 80
    cam.rotation_euler = (-dv).to_track_quat("-Z", "Y").to_euler()
    scn.render.filepath = path
    bpy.ops.render.render(write_still=True)


VIEWS = (("front", (0, -1, 0.05)), ("side", (1, 0, 0.05)), ("back", (0, 1, 0.05)), ("q34", (0.7, -0.7, 0.35)), ("top", (0.0001, 0.0, 1)))


def previews(prefix, center, ortho):
    os.makedirs(PREVIEW, exist_ok=True)
    for vn, dv in VIEWS:
        shoot(os.path.join(PREVIEW, f"{prefix}_{vn}.png"), center, dv, ortho)


def export(objs, path, anim=False):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH", "ARMATURE"},
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             use_mesh_modifiers=False, mesh_smooth_type="FACE", add_leaf_bones=False,
                             use_armature_deform_only=False, bake_anim=anim, bake_anim_use_all_bones=True,
                             bake_anim_use_nla_strips=False, bake_anim_use_all_actions=anim,
                             bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
                             path_mode="COPY", embed_textures=False)
    log("exported", path)


def write_metrics(out_dir, kind):
    with open(os.path.join(out_dir, f"{kind}_metrics.txt"), "w") as f: f.write("\n".join(_m) + "\n")


# ======================================================================================================== bike
def do_bike():
    out = os.path.join(ROOT, "Art", "Export", "hoverbike"); os.makedirs(out, exist_ok=True)
    clean(); ob = import_glb(os.path.join(ROOT, "Art", "Source", "hoverbike", "hoverbike_v1.glb")); ob.name = "hoverbike"
    P = co(ob)
    L = P[:, 0].max() - P[:, 0].min()
    # forked end: in the last 6 % of each end, the share of points near the centre line (|y| < 4 % of the width)
    W = P[:, 1].max() - P[:, 1].min()
    ends = {}
    for sgn in (1, -1):
        tip = P[:, 0].max() if sgn > 0 else P[:, 0].min()
        sel = (P[:, 0] * sgn) > (tip * sgn - 0.06 * L)
        ends[sgn] = (np.abs(P[sel, 1]) < 0.04 * W).mean() if sel.any() else 1.0
    front = 1 if ends[1] < ends[-1] else -1
    log(f"bike: centre-line share at the +X end {ends[1]:.3f}, -X end {ends[-1]:.3f} -> forked front at {'+X' if front > 0 else '-X'}")
    # turn: front -> -Y (Blender forward = Unity +Z)
    ang = -math.pi / 2 if front > 0 else math.pi / 2
    R = np.array([[math.cos(ang), -math.sin(ang), 0], [math.sin(ang), math.cos(ang), 0], [0, 0, 1]])
    P = P @ R.T
    s = LENGTH_BIKE / (P[:, 1].max() - P[:, 1].min())
    P *= s
    P[:, 0] -= (P[:, 0].max() + P[:, 0].min()) / 2; P[:, 1] -= (P[:, 1].max() + P[:, 1].min()) / 2; P[:, 2] -= P[:, 2].min()
    set_co(ob, P)
    log(f"bike: scaled x{s:.3f}: length {P[:,1].ptp():.2f} m (forked front at -Y), width {P[:,0].ptp():.2f} m, height {P[:,2].ptp():.2f} m")
    decimate(ob, TRIS["bike"])
    P = co(ob)
    # saddle: highest-flat region behind the middle; grips: lowest surface hollows in the fairing ahead of the saddle on each side
    mid = (P[:, 1] > -0.15 * LENGTH_BIKE) & (P[:, 1] < 0.25 * LENGTH_BIKE) & (np.abs(P[:, 0]) < 0.12)
    seat_z = np.percentile(P[mid, 2], 30) if mid.any() else P[:, 2].max() * 0.8
    seat_y = float(np.median(P[mid, 1])) if mid.any() else 0.1
    log(f"bike: saddle at y {seat_y:.2f}, z {seat_z:.2f} (Unity: z = {-seat_y:.2f} forward)")
    for side in (-1, 1):
        sel = (P[:, 1] < seat_y - 0.15) & (P[:, 1] > seat_y - 0.75) & (P[:, 0] * side > 0.10) & (P[:, 0] * side < 0.45) & (P[:, 2] > seat_z - 0.05)
        if sel.any():
            Q = P[sel]; k = np.argmin(Q[:, 2] - 0.0 * Q[:, 1])
            g = Q[np.argsort(Q[:, 2])[:max(1, len(Q) // 20)]].mean(axis=0)
            log(f"bike: grip {'right' if side > 0 else 'left'} at (x {g[0]:.3f}, y {g[1]:.3f}, z {g[2]:.3f}) -> Unity ({-g[0]:.3f}, {g[2]:.3f}, {-g[1]:.3f})")
    # QF10: machine-readable points for Unity (Unity frame: x = -Blender x, y = Blender z, z = -Blender y):
    # saddle top (her hips rest on it), the two grips, and a foot point on each side of the body (the widest point of the
    # side panel at mid height, a little behind the saddle) for the foot IK.
    import json
    pts = {"length": float(np.ptp(P[:, 1])), "width": float(np.ptp(P[:, 0])), "height": float(np.ptp(P[:, 2])),
           "saddle": [0.0, float(seat_z), float(-seat_y)]}
    grips = {}
    for side in (-1, 1):
        sel = (P[:, 1] < seat_y - 0.15 * LENGTH_BIKE / 2.3) & (P[:, 1] > seat_y - 0.75 * LENGTH_BIKE / 2.3) & (P[:, 0] * side > 0.10 * LENGTH_BIKE / 2.3) & (P[:, 0] * side < 0.45 * LENGTH_BIKE / 2.3) & (P[:, 2] > seat_z - 0.05)
        if sel.any():
            Q = P[sel]; g = Q[np.argsort(Q[:, 2])[:max(1, len(Q) // 20)]].mean(axis=0)
            grips[side] = [float(-g[0]), float(g[2]), float(-g[1])]
    pts["grip_l"] = grips.get(1, [-0.3, seat_z, 0.6]); pts["grip_r"] = grips.get(-1, [0.3, seat_z, 0.6])
    if pts["grip_l"][0] > 0: pts["grip_l"], pts["grip_r"] = pts["grip_r"], pts["grip_l"]     # left = -x in Unity
    fy = seat_y + 0.10 * LENGTH_BIKE / 2.3
    sl = (np.abs(P[:, 1] - fy) < 0.07) & (P[:, 2] > 0.2 * np.ptp(P[:, 2])) & (P[:, 2] < 0.75 * seat_z + 0.05)
    if sl.any():
        k = np.argmax(np.abs(P[sl, 0])); w = float(np.abs(P[sl, 0]).max()); fz = float(P[sl, 2][k])
    else:
        w, fz = float(np.ptp(P[:, 0]) / 2), float(seat_z * 0.5)
    pts["foot_l"] = [-(w + 0.03), fz, float(-fy)]; pts["foot_r"] = [w + 0.03, fz, float(-fy)]
    json.dump(pts, open(os.path.join(out, "hoverbike_points.json"), "w"), indent=1)
    log(f"bike points (Unity, metres): {json.dumps(pts)}")
    log(f"bike: vents (underside) lowest z {P[:,2].min():.3f}, tris {tris(ob)}")
    gray_texture(ob, "hoverbike_v1", out)
    previews("bike", (0, 0, P[:, 2].ptp() / 2), LENGTH_BIKE * 1.2)
    export([ob], os.path.join(out, "hoverbike_v1.fbx"))
    write_metrics(out, "hoverbike")


# ======================================================================================================== elarim
def do_elarim():
    out = os.path.join(ROOT, "Art", "Export", "elarim"); os.makedirs(out, exist_ok=True)
    clean()
    bpy.ops.import_scene.fbx(filepath=os.path.join(ROOT, "Art", "Mixamo", "elarim", "elarim_idle.fbx"), automatic_bone_orientation=False)
    arm = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
    for o in list(bpy.context.scene.objects):
        if o.type == 'MESH': bpy.data.objects.remove(o, do_unlink=True)          # clip files may carry a placeholder mesh
    arm.animation_data_clear()
    for pb in arm.pose.bones:                     # the import leaves the clip's first-frame pose on the bones: back to rest
        pb.location = (0, 0, 0); pb.rotation_quaternion = (1, 0, 0, 0); pb.rotation_euler = (0, 0, 0); pb.scale = (1, 1, 1)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bones = {b.name: b for b in arm.data.bones}
    def bw(n, tail=False):
        b = bones.get("mixamorig:" + n) or bones.get(n)
        return (arm.matrix_world @ (b.tail_local if tail else b.head_local)) if b else None
    hips = bw("Hips"); top = bw("HeadTop_End") or bw("Head", True); lfoot = bw("LeftToeBase") or bw("LeftFoot")
    sk_h = top.z - min(lfoot.z, hips.z - 1.0 * (top.z - hips.z))
    log(f"elarim skeleton (Mixamo, as fitted to the uploaded model): {len(bones)} bones, hips {tuple(round(c,3) for c in hips)}, head top z {top.z:.3f}, toe z {lfoot.z:.3f}")
    # mesh in
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.gltf(filepath=os.path.join(ROOT, "Art", "Source", "elarim", "elarim_v1.glb"))
    ob = next(o for o in bpy.context.scene.objects if o.type == 'MESH' and o not in before); ob.name = "elarim"
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM'); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for o in list(bpy.context.scene.objects):
        if o.type == 'EMPTY': bpy.data.objects.remove(o, do_unlink=True)
    P = co(ob)
    # Mixamo faces +Y? (its rest skeleton faces -Y in Blender after import: check the toes vs heels)
    toe = bw("LeftToeBase"); foot = bw("LeftFoot")
    face_y = -1.0 if (toe is not None and foot is not None and toe.y < foot.y) else 1.0
    log(f"skeleton faces {'-Y' if face_y < 0 else '+Y'}")
    # align: mesh top -> head top, mesh bottom -> 0 (the skeleton's ground), centre x/y on the hips
    zmin, zmax = P[:, 2].min(), P[:, 2].max()
    ground = min(lfoot.z - 0.02 * (top.z - lfoot.z), 0.0)
    s = (top.z - ground) / (zmax - zmin)
    P = (P - np.array([0, 0, zmin])) * s + np.array([0, 0, ground])
    P[:, 0] += hips.x - np.median(P[(P[:, 2] > hips.z - 0.05 * (top.z - ground)) & (P[:, 2] < hips.z + 0.05 * (top.z - ground)), 0])
    P[:, 1] += hips.y - np.median(P[(P[:, 2] > hips.z - 0.05 * (top.z - ground)) & (P[:, 2] < hips.z + 0.05 * (top.z - ground)), 1])
    set_co(ob, P)
    log(f"mesh aligned to the skeleton: x{s:.3f}")
    decimate(ob, TRIS["elarim"])
    gray_texture(ob, "elarim_v1", out)
    # skin
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active = arm
    try:
        bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    except Exception as e:
        log("automatic weights raised:", e)
    unweighted = sum(1 for v in ob.data.vertices if not any(g.weight > 0.01 for g in v.groups))
    log(f"automatic weights: {unweighted} of {len(ob.data.vertices)} vertices unweighted")
    if unweighted > 0.02 * len(ob.data.vertices):
        # fallback: nearest deforming bone segment (smooth over the two nearest)
        if ob.parent is None: ob.parent = arm
        if not any(m.type == 'ARMATURE' for m in ob.modifiers): ob.modifiers.new("arm", "ARMATURE").object = arm
        for vg in list(ob.vertex_groups): ob.vertex_groups.remove(vg)
        segs = [(b.name, np.array(arm.matrix_world @ b.head_local), np.array(arm.matrix_world @ b.tail_local)) for b in arm.data.bones if b.use_deform and not b.name.endswith("_End")]
        P = co(ob); D = np.empty((len(P), len(segs)))
        for j, (n, a, b) in enumerate(segs):
            ab = b - a; t = np.clip(((P - a) @ ab) / max(1e-9, ab @ ab), 0, 1)
            D[:, j] = np.linalg.norm(P - (a + t[:, None] * ab), axis=1)
        idx = np.argsort(D, axis=1)[:, :2]
        groups = {n: ob.vertex_groups.new(name=n) for n, _, _ in segs}
        for i in range(len(P)):
            d0, d1 = D[i, idx[i, 0]], D[i, idx[i, 1]]; w0 = d1 / max(1e-9, d0 + d1)
            groups[segs[idx[i, 0]][0]].add([i], float(w0), 'REPLACE'); groups[segs[idx[i, 1]][0]].add([i], float(1 - w0), 'REPLACE')
        log("weights: nearest-bone fallback used")
    # QF10: the cape gets its own two bones (CapeA / CapeB, one per half, children of Spine2, pivot at the top of the
    # cape region behind the shoulder blades). Everything well behind the body below Spine2 hangs from them (blended
    # across the middle, and into Spine2 just under the pivot so nothing tears). In Unity, ElarimCape swings each half
    # backward just enough that no leg (thigh / shin / foot capsules) and not the ground ever passes through it - walking,
    # kneeling and standing. Never weighted to the legs (it would stretch between them when he walks).
    Hh = top.z - ground; P = co(ob)
    sh = bw("Spine2") or bw("Spine1")
    back = -face_y
    cape = (P[:, 1] * back > hips.y * back + 0.075 * Hh) & (P[:, 2] < sh.z)
    if cape.any() and "mixamorig:Spine2" in ob.vertex_groups:
        idx = np.nonzero(cape)[0]
        yb = float(np.percentile(P[idx][P[idx, 2] > sh.z - 0.08 * Hh][:, 1], 50)) if (P[idx, 2] > sh.z - 0.08 * Hh).any() else hips.y + back * 0.09 * Hh
        piv_z = sh.z
        bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); bpy.context.view_layer.objects.active = arm
        bpy.ops.object.mode_set(mode='EDIT')
        eb_parent = arm.data.edit_bones["mixamorig:Spine2"]
        inv = arm.matrix_world.inverted()
        for nm, sx in (("CapeA", -1.0), ("CapeB", 1.0)):
            b = arm.data.edit_bones.new(nm)
            b.head = inv @ Vector((hips.x + sx * 0.07 * Hh, yb, piv_z)); b.tail = inv @ Vector((hips.x + sx * 0.07 * Hh, yb, piv_z - 0.3 * Hh))
            b.parent = eb_parent; b.use_connect = False; b.use_deform = True
        bpy.ops.object.mode_set(mode='OBJECT')
        for vg in ob.vertex_groups: vg.remove(idx.tolist())
        ga = ob.vertex_groups.get("CapeA") or ob.vertex_groups.new(name="CapeA")
        gb = ob.vertex_groups.get("CapeB") or ob.vertex_groups.new(name="CapeB")
        gs = ob.vertex_groups["mixamorig:Spine2"]
        band = 0.06 * Hh
        for i in idx.tolist():
            x = P[i, 0] - hips.x; z = P[i, 2]
            wb = min(1.0, max(0.0, 0.5 + x / (0.10 * Hh)))          # across the middle: half and half
            top_k = min(1.0, max(0.0, (z - (piv_z - band)) / band))   # just under the pivot: into Spine2
            c = 1.0 - top_k
            if c * (1 - wb) > 1e-4: ga.add([i], c * (1 - wb), 'REPLACE')
            if c * wb > 1e-4: gb.add([i], c * wb, 'REPLACE')
            if top_k > 1e-4: gs.add([i], top_k, 'REPLACE')
        log(f"cape: {len(idx)} vertices on CapeA / CapeB (pivot z {piv_z:.3f}, back y {yb:.3f}), blended into Spine2 over the top {band:.3f}")
    else:
        log("WARNING: cape region not found - cape stays on the auto weights")
    # scale the rig to 3.5 m (feet at 0)
    k = HEIGHT_ELARIM / (top.z - ground)
    arm.scale = (k, k, k); arm.location = (0, 0, -ground * k)
    bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); ob.select_set(True); bpy.context.view_layer.objects.active = arm
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    Pw = np.array([ob.matrix_world @ v.co for v in ob.data.vertices])
    dg = bpy.context.evaluated_depsgraph_get(); me_ev = ob.evaluated_get(dg).to_mesh()
    Pe = np.array([ob.matrix_world @ v.co for v in me_ev.vertices])
    hb = np.array(arm.matrix_world @ arm.data.bones["mixamorig:Hips"].head_local)
    log(f"elarim bounds (rest) min {Pw.min(0).round(2)} max {Pw.max(0).round(2)}; deformed min {Pe.min(0).round(2)} max {Pe.max(0).round(2)}; hips bone {hb.round(2)}; arm loc {tuple(arm.location)} scale {tuple(arm.scale)}; mesh parent {ob.parent.name if ob.parent else None}")
    log(f"elarim: height {Pw[:,2].max() - Pw[:,2].min():.2f} m (target {HEIGHT_ELARIM}), width {np.ptp(Pw[:,0]):.2f} m, tris {tris(ob)}, bones {len(arm.data.bones)}")
    previews("elarim", (0, 0, HEIGHT_ELARIM / 2), HEIGHT_ELARIM * 1.15)
    export([arm, ob], os.path.join(out, "elarim_v1_rigged.fbx"))
    # the sword (a prop the guardian / workers can carry)
    clean(); sw = import_glb(os.path.join(ROOT, "Art", "Source", "elarim", "elarim_sword_v1.glb")); sw.name = "elarim_sword"
    P = co(sw); ln = np.ptp(P[:, 0]); s2 = 1.6 / ln
    P = P * s2; P[:, 0] -= (P[:, 0].max() + P[:, 0].min()) / 2; P[:, 1] -= (P[:, 1].max() + P[:, 1].min()) / 2; P[:, 2] -= P[:, 2].min()
    set_co(sw, P); decimate(sw, TRIS["sword"]); gray_texture(sw, "elarim_sword_v1", out)
    log(f"sword: 1.6 m along X, tris {tris(sw)}")
    export([sw], os.path.join(out, "elarim_sword_v1.fbx"))
    write_metrics(out, "elarim")


# ======================================================================================================== worm
def do_worm():
    out = os.path.join(ROOT, "Art", "Export", "worm"); os.makedirs(out, exist_ok=True)
    clean(); ob = import_glb(os.path.join(ROOT, "Art", "Source", "wormmaster", "worm_v1.glb")); ob.name = "worm"
    P = co(ob)
    # head end: the thicker end (larger cross-section in the last 10 %)
    L0 = np.ptp(P[:, 0])
    def thick(sgn):
        tip = P[:, 0].max() if sgn > 0 else P[:, 0].min()
        sel = (P[:, 0] * sgn) > (tip * sgn - 0.12 * L0)
        return np.ptp(P[sel, 1]) * np.ptp(P[sel, 2]) if sel.any() else 0
    head = 1 if thick(1) >= thick(-1) else -1
    ang = -math.pi / 2 if head > 0 else math.pi / 2
    R = np.array([[math.cos(ang), -math.sin(ang), 0], [math.sin(ang), math.cos(ang), 0], [0, 0, 1]])
    P = P @ R.T
    s = LENGTH_WORM / np.ptp(P[:, 1]); P *= s
    P[:, 0] -= (P[:, 0].max() + P[:, 0].min()) / 2; P[:, 1] -= (P[:, 1].max() + P[:, 1].min()) / 2; P[:, 2] -= P[:, 2].min()
    set_co(ob, P)
    log(f"worm: head at {'+X' if head > 0 else '-X'} -> -Y; {LENGTH_WORM} m long, {np.ptp(P[:,0]):.2f} m wide, {np.ptp(P[:,2]):.2f} m high")
    decimate(ob, TRIS["worm"]); gray_texture(ob, "worm_v1", out)
    # bone chain tail (+Y) -> head (-Y)
    N = 8
    ad = bpy.data.armatures.new("worm_rig"); arm = bpy.data.objects.new("worm_rig", ad); bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode='EDIT')
    zc = np.ptp(P[:, 2]) * 0.5; prev = None
    y0 = LENGTH_WORM / 2
    for i in range(N):
        b = ad.edit_bones.new(f"seg{i}")
        b.head = (0, y0 - i * LENGTH_WORM / N, zc); b.tail = (0, y0 - (i + 1) * LENGTH_WORM / N, zc)
        if prev: b.parent = prev; b.use_connect = True
        prev = b
    bpy.ops.object.mode_set(mode='OBJECT')
    ob.parent = arm; mod = ob.modifiers.new("arm", "ARMATURE"); mod.object = arm
    P = co(ob); u = (y0 - P[:, 1]) / (LENGTH_WORM / N)               # 0 .. N along the body
    for i in range(N):
        vg = ob.vertex_groups.new(name=f"seg{i}")
        w = np.clip(1.0 - np.abs(u - (i + 0.5)), 0, 1)
        idx = np.nonzero(w > 0.001)[0]
        for k in idx: vg.add([int(k)], float(w[k]), 'REPLACE')
    # clips: rise (head lifts out, 2 s), wriggle (loop, 2 s), dive (head down, 2 s), idle_under (flat, 1 s)
    bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode='POSE')
    def key(frame, rots):
        for i, r in enumerate(rots):
            pb = arm.pose.bones[f"seg{i}"]; pb.rotation_mode = 'XYZ'; pb.rotation_euler = r; pb.keyframe_insert("rotation_euler", frame=frame)
    def act(name, frames):
        a = bpy.data.actions.new(name); arm.animation_data_create(); arm.animation_data.action = a
        for f, rots in frames: key(f, rots)
        a.use_fake_user = True
        return a
    flat = [(0, 0, 0)] * N
    act("idle_under", [(1, flat), (30, flat)])
    rise_end = [(0, 0, 0)] * 4 + [(0.35, 0, 0), (0.45, 0, 0), (0.35, 0, 0), (0.2, 0, 0)]
    act("rise", [(1, flat), (60, rise_end)])
    w1 = [(0, 0, 0.25 * math.sin(i * 1.3)) for i in range(N)]; w2 = [(0, 0, -0.25 * math.sin(i * 1.3)) for i in range(N)]
    act("wriggle", [(1, w1), (30, w2), (60, w1)])
    dive_end = [(0, 0, 0)] * 4 + [(-0.3, 0, 0), (-0.45, 0, 0), (-0.4, 0, 0), (-0.3, 0, 0)]
    act("dive", [(1, rise_end), (60, dive_end)])
    bpy.ops.object.mode_set(mode='OBJECT')
    arm.animation_data.action = bpy.data.actions["idle_under"]
    log(f"worm: {N}-bone chain, clips idle_under / rise / wriggle / dive, tris {tris(ob)}")
    previews("worm", (0, 0, 0.6), LENGTH_WORM * 1.1)
    export([arm, ob], os.path.join(out, "worm_v1_rigged.fbx"), anim=True)
    write_metrics(out, "worm")


{"bike": do_bike, "elarim": do_elarim, "worm": do_worm}[KIND]()
log("done", KIND)
