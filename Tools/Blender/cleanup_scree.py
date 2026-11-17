"""Clean up, rig and animate the Scree (QF4) for Unity.

Run headless from the repo root (~4 min):
  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/cleanup_scree.py -- [repo_root] [target_tris] [preview_dir]

Input : Art/Source/scree/scree_v1.glb   (Tripo, unrigged, ~2M tris, one welded island after a 1e-6 weld, 0.98 units tall,
                                         the whole figure turned ~17 deg about the vertical, abdomen + wing covers pointing
                                         back-and-sideways ~40 deg, head turned a little to its right; never modified)
Output: Art/Export/scree/scree_v1_rigged.fbx            (mesh + glow mesh + Humanoid-compatible armature + clips "work", "alert")
        Art/Export/scree/Textures/scree_v1_basecolor.jpg (grayscale, 2048)
        Art/Export/scree/scree_metrics.txt               (fit, corrections, decimation, joints, clip foot contact)
        [preview_dir]/scree_*.png                        (rest views front/side/back/top/face + clip frames)

Steps: weld -> turn the figure square (upper-arm claw line along X, facing -Y) -> scale so the crown (antenna excluded)
is HEIGHT -> origin between the feet on the floor -> swing the abdomen + wing covers about the waist so they extend
straight behind (soft blend at the waist, tanks excluded) -> straighten the head (yaw about the neck, snout onto the
centre line) -> decimate to ~target with the hose, antenna and claws protected -> rig: humanoid bones with Unity names
(Hips, Spine, Chest, UpperChest, Neck, Head, Left/Right Shoulder, UpperArm, LowerArm, Hand, UpperLeg, LowerLeg, Foot,
Toes) + extras (ScreeB_* = the lower arm pair, ScreeAbd1-4 = abdomen chain, ScreeAnt1-2 = antenna) -> distance-based
skin weights -> the upper arm pair is straightened into a T-pose and applied as the rest pose (Unity Humanoid retargets
7's Mixamo clips from it) -> keyed clips (Humanoid bones only; the extras move procedurally in Unity, ScreeRig.cs):
  work  (2.4 s loop): bent over a work surface, the upper arms chisel / lift in turn, lower arms brace, head down.
  alert (1.4 s): startle - straightens up, head snaps up and round, arms lift and draw in, settles watchful.
The digitigrade-looking legs are plain hip-knee-ankle chains (the shin is straight in this model), so retargeted walks
keep the knee bending forward like a human's.
"""
import bpy, bmesh, os, sys, math
import numpy as np
from mathutils import Vector, Quaternion, Matrix

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = os.path.abspath(argv[0]) if len(argv) > 0 else os.getcwd()
TARGET_TRIS = int(argv[1]) if len(argv) > 1 else 45000
PREVIEW = os.path.abspath(argv[2]) if len(argv) > 2 else None
STAGE = os.environ.get("SC_STAGE", "full")      # "fit": stop after the rig fit (debug renders), "poses": clip frames only

HEIGHT = 1.90          # crown height in metres (antenna not counted); 7 is 1.70. Unity reads the FBX as is.
FPS = 30
BODY_YAW = None        # None = measured (deg); turn applied to square the figure
ABD_YAW = None         # None = measured (deg); swing of the abdomen about the waist
ABD_TUCK = 42.0        # deg below horizontal: the abdomen + wing covers tuck down behind the hips (Dan 2026-11-17: 35-50)
HEAD_YAW = None        # None = measured (deg); head straightening about the neck

SRC = os.path.join(ROOT, "Art", "Source", "scree", "scree_v1.glb")
OUT_DIR = os.path.join(ROOT, "Art", "Export", "scree")
TEX_DIR = os.path.join(OUT_DIR, "Textures")
FBX = os.path.join(OUT_DIR, "scree_v1_rigged.fbx")
TEX = os.path.join(TEX_DIR, "scree_v1_basecolor.jpg")
METRICS = os.path.join(OUT_DIR, "scree_metrics.txt")
os.makedirs(TEX_DIR, exist_ok=True)
_metrics = []


def log(*a):
    s = " ".join(str(x) for x in a)
    print("[scree]", s, flush=True)
    _metrics.append(s)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def fmt(v):
    return "(" + ", ".join(f"{c:.3f}" for c in v) + ")"


def tris(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def get_co(me):
    a = np.empty(len(me.vertices) * 3, dtype=np.float64)
    me.vertices.foreach_get("co", a)
    return a.reshape(-1, 3)


def set_co(me, a):
    me.vertices.foreach_set("co", a.astype(np.float32).ravel())
    me.update()


def rot_z(pts, deg, cx=0.0, cy=0.0, w=None):
    """Rotate points about a vertical axis through (cx, cy); w = per-point fraction of the angle."""
    a = np.radians(deg) * (1.0 if w is None else w)
    c, s = np.cos(a), np.sin(a)
    x, y = pts[:, 0] - cx, pts[:, 1] - cy
    out = pts.copy()
    out[:, 0] = cx + c * x - s * y
    out[:, 1] = cy + s * x + c * y
    return out


# ------------------------------------------------------------------ import + weld
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SRC)
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
for o in bpy.data.objects:
    o.select_set(o.type == "MESH")
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1:
    bpy.ops.object.join()
sc = bpy.context.view_layer.objects.active
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for o in list(bpy.data.objects):
    if o != sc:
        bpy.data.objects.remove(o)
sc.name = "scree_body"
log("source", len(sc.data.vertices), "verts", tris(sc), "tris")
bm = bmesh.new()
bm.from_mesh(sc.data)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
bm.to_mesh(sc.data)
bm.free()
me = sc.data
P = get_co(me)
ZT = P[:, 2].max()
log(f"welded: {len(P)} verts, source height {ZT:.4f}")

# ------------------------------------------------------------------ square the figure, scale, origin
band = P[(P[:, 2] > 0.68 * ZT) & (P[:, 2] < 0.86 * ZT)]
xy = band[:, :2] - band[:, :2].mean(0)
ev, evec = np.linalg.eigh(np.cov(xy.T))
e = evec[:, np.argmax(ev)]
proj = xy @ e
t0, t1 = band[np.argmin(proj), :2], band[np.argmax(proj), :2]
d = t1 - t0
yaw = math.degrees(math.atan2(d[1], d[0]))
if yaw > 90: yaw -= 180
if yaw < -90: yaw += 180
yaw = -yaw if BODY_YAW is None else BODY_YAW
legs = P[(P[:, 2] > 0.30 * ZT) & (P[:, 2] < 0.38 * ZT)]
c0 = legs.mean(0)
P = rot_z(P, yaw, c0[0], c0[1])
P[:, 0] -= c0[0]
P[:, 1] -= c0[1]
log(f"body turned {yaw:.1f} deg (upper claw tips line squared to X); legs centre {fmt(c0)}")
# crown: highest slice still wider than the antenna
zs = np.sort(P[:, 2])
crown = None
for zc in np.arange(ZT, 0.8 * ZT, -0.002):
    sl = P[(P[:, 2] > zc - 0.004) & (P[:, 2] < zc)]
    if len(sl) > 20 and np.ptp(sl[:, 0]) > 0.05 * ZT:
        crown = zc
        break
S = HEIGHT / crown
P *= S
P[:, 2] -= P[:, 2].min()
feet = P[P[:, 2] < 0.08 * HEIGHT]
P[:, 0] -= (feet[:, 0].min() + feet[:, 0].max()) / 2
ank = P[(P[:, 2] > 0.09 * HEIGHT) & (P[:, 2] < 0.12 * HEIGHT)]
P[:, 1] -= ank[:, 1].mean()
H = HEIGHT
log(f"crown at {crown:.4f} src -> scale {S:.4f} (crown {HEIGHT} m, antenna tip {P[:, 2].max():.3f} m)")


# All thresholds below are in source units (zf = height / S; measured on the squared source, crown 0.93: abdomen top <= 0.645 crown behind
# the waist, tank bottoms >= 0.70, head band 0.86-0.99).
X, Y, Z = P[:, 0], P[:, 1], P[:, 2]
zf = Z / S

# ------------------------------------------------------------------ abdomen straight behind
abd_w = smooth(0.055, 0.105, Y / S) * smooth(0.662, 0.648, zf) * smooth(0.40, 0.44, zf)
abd = P[abd_w > 0.95]
piv = np.array([np.median(P[(abd_w > 0.2) & (abd_w < 0.5), 0]), 0.06 * S])
dd = abd[:, :2] - piv
tip = abd[np.argmax(np.hypot(dd[:, 0], dd[:, 1]))]
ang = math.degrees(math.atan2(tip[1] - piv[1], tip[0] - piv[0]))
ayaw = (90.0 - ang) if ABD_YAW is None else ABD_YAW
P = rot_z(P, ayaw, piv[0], piv[1], abd_w)
log(f"abdomen: {int((abd_w > 0.5).sum())} verts, tip {fmt(tip)} was {90 - ang:.1f} deg off straight-behind -> swung {ayaw:.1f} deg about {fmt(piv)}")
# tail tucked down (approved design 2026-11-17): pitch the abdomen + wing covers down about a level axis through the
# waist joint, like a resting wasp / grasshopper. The covers move with it (same weights), so they keep lying along it.
pz = float(np.median(P[(abd_w > 0.2) & (abd_w < 0.5), 2]))
py = 0.06 * S
abd_now = P[abd_w > 0.95]
tip0 = abd_now[np.argmax(abd_now[:, 1])]
a0 = math.degrees(math.atan2(pz - tip0[2], tip0[1] - py))          # its current angle below horizontal
tuck = ABD_TUCK - a0
a = np.radians(-tuck) * abd_w
c_, s_ = np.cos(a), np.sin(a)
yy_, zz_ = P[:, 1] - py, P[:, 2] - pz
P[:, 1] = py + c_ * yy_ - s_ * zz_
P[:, 2] = pz + s_ * yy_ + c_ * zz_
abd_now = P[abd_w > 0.95]
tip1 = abd_now[np.argmax(np.hypot(abd_now[:, 1] - py, abd_now[:, 2] - pz))]
log(f"abdomen tucked: was {a0:.1f} deg below horizontal -> pitched {tuck:.1f} deg about the waist (y {py:.3f}, z {pz:.3f}); "
    f"tip now {fmt(tip1)} = {math.degrees(math.atan2(pz - tip1[2], tip1[1] - py)):.1f} deg below horizontal, lowest abdomen point {abd_now[:, 2].min():.3f} m")
X, Y, Z = P[:, 0], P[:, 1], P[:, 2]

# ------------------------------------------------------------------ head straight
hb = P[(zf > 0.89) & (zf < 0.97) & (np.abs(X) < 0.10 * S) & (Y < 0.07 * S)]
hc = hb[:, :2].mean(0)
sn = P[(zf > 0.83) & (zf < 0.875) & (np.abs(X - hc[0]) < 0.06 * S) & (Y < hc[1])]
snout = sn[np.argmin(sn[:, 1])]
hang = math.degrees(math.atan2(snout[0] - hc[0], -(snout[1] - hc[1])))      # + = snout toward +X
hyaw = (-hang) if HEAD_YAW is None else HEAD_YAW
head_w = smooth(0.815, 0.86, zf) * smooth(0.075 * S, 0.055 * S, Y) * smooth(0.14 * S, 0.10 * S, np.abs(X - hc[0]))
P = rot_z(P, hyaw, hc[0], hc[1], head_w)
log(f"head: centre {fmt(hc)}, snout {fmt(snout)} turned {hang:.1f} deg -> straightened by {hyaw:.1f} deg")
set_co(me, P)

# ------------------------------------------------------------------ decimate (hose, antenna, claws protected)
X, Y, Z = P[:, 0], P[:, 1], P[:, 2]
zf = Z / S
protect = (zf > 0.995) | (zf < 0.05) \
    | ((zf > 0.72) & (zf < 0.87) & (Y < -0.03 * S)) \
    | ((np.abs(X) > 0.30 * S) & (zf > 0.68)) \
    | ((np.abs(X) > 0.24 * S) & (zf < 0.56) & (zf > 0.40))
vg = sc.vertex_groups.new(name="protect")
vg.add([int(i) for i in np.nonzero(~protect)[0]], 1.0, "REPLACE")
vg.add([int(i) for i in np.nonzero(protect)[0]], 0.7, "REPLACE")
n0 = tris(sc)
mod = sc.modifiers.new("dec", "DECIMATE")
mod.decimate_type = "COLLAPSE"
mod.ratio = min(1.0, TARGET_TRIS / n0)
mod.vertex_group = "protect"
mod.vertex_group_factor = 1.0
bpy.ops.object.select_all(action="DESELECT")
sc.select_set(True)
bpy.context.view_layer.objects.active = sc
bpy.ops.object.modifier_apply(modifier="dec")
sc.vertex_groups.remove(sc.vertex_groups["protect"])
me = sc.data
bm = bmesh.new()
bm.from_mesh(me)
bnd = sum(1 for e_ in bm.edges if e_.is_boundary)
bm.free()
log(f"decimated {n0} -> {tris(sc)} tris ({int(protect.sum())} source verts protected: hose, antenna, claws); boundary edges {bnd}")

# ------------------------------------------------------------------ material: grayscale 2048 texture
mat = sc.material_slots[0].material
mat.name = "scree_shell"
img = next((nd.image for nd in mat.node_tree.nodes if nd.type == "TEX_IMAGE" and nd.image is not None), None)
if img is not None:
    img2 = img.copy()
    img2.scale(2048, 2048)
    px = np.empty(2048 * 2048 * 4, dtype=np.float32)
    img2.pixels.foreach_get(px)
    px = px.reshape(-1, 4)
    g = 0.2126 * px[:, 0] + 0.7152 * px[:, 1] + 0.0722 * px[:, 2]
    px[:, 0] = px[:, 1] = px[:, 2] = g
    img2.pixels.foreach_set(px.ravel())
    img2.filepath_raw = TEX
    img2.file_format = "JPEG"
    img2.save()
    for nd in mat.node_tree.nodes:
        if nd.type == "TEX_IMAGE":
            nd.image = img2
    log("texture", TEX)

# ------------------------------------------------------------------ fit the rig
P = get_co(me)
X, Y, Z = P[:, 0], P[:, 1], P[:, 2]
zf = Z / S
ABD = (Y > 0.09 * S) & (zf < 0.665) & (zf > 0.08) & ~((Y < 0.16 * S) & (zf > 0.62))
TANK = (Y > 0.06 * S) & (zf > 0.68) & (zf < 0.90) & ~((np.abs(X) < 0.07 * S) & (zf > 0.86))


def shift(guess, mask, r, it=8):
    """Mean-shift; the first passes measure distance in the front view only (X-Z), so the depth (Y) comes from the
    limb itself, then full 3D passes centre it."""
    p = np.array(guess, dtype=float)
    Q = P[mask]
    for k in range(it):
        dv = Q - p
        if k < 3:
            dv[:, 1] = 0.0
        near = Q[np.linalg.norm(dv, axis=1) < r]
        if len(near) < 5:
            log(f"  shift: too few points near {fmt(p)}")
            break
        p = near.mean(0)
    return p


def g(x, z, y=0.0):
    return np.array([x * S, y * S, z * S])


body = ~ABD & ~TANK
mid = body & (np.abs(X) < 0.075 * S)
J = {}
J["Hips"] = shift(g(0, 0.565), mid & (zf > 0.5) & (zf < 0.63), 0.05 * S)
J["Spine"] = shift(g(0, 0.62), mid & (zf > 0.58) & (zf < 0.66), 0.04 * S)
J["Chest"] = shift(g(0, 0.675), mid & (zf > 0.63) & (zf < 0.72), 0.04 * S)
J["UpperChest"] = shift(g(0, 0.73), mid & (zf > 0.69) & (zf < 0.77), 0.04 * S)
J["Neck"] = shift(g(0, 0.79), mid & (zf > 0.765) & (zf < 0.82) & (Y > -0.035 * S), 0.03 * S)
J["Head"] = shift(g(0, 0.845), mid & (zf > 0.82) & (zf < 0.87) & (Y > -0.04 * S), 0.03 * S)
for k in ("Hips", "Spine", "Chest", "UpperChest", "Neck", "Head"):
    J[k][0] = 0.0
J["HeadTop"] = np.array([0.0, J["Head"][1], H])
for sd, sx in (("Left", 1), ("Right", -1)):
    side = body & (X * sx > 0.0)
    J[sd + "Shoulder"] = np.array([0.025 * S * sx, J["UpperChest"][1], 0.765 * S])
    J[sd + "UpperArm"] = shift(g(0.07 * sx, 0.755), side & (zf > 0.70), 0.03 * S)
    J[sd + "LowerArm"] = shift(g(0.185 * sx, 0.712), side & (zf > 0.67), 0.025 * S)
    J[sd + "Hand"] = shift(g(0.305 * sx, 0.737), side & (zf > 0.68), 0.022 * S)
    J[sd + "HandTip"] = shift(g(0.385 * sx, 0.75), side & (zf > 0.68), 0.03 * S)
    J[sd + "UpperLeg"] = shift(g(0.065 * sx, 0.53), side & (zf < 0.58), 0.035 * S)
    J[sd + "LowerLeg"] = shift(g(0.095 * sx, 0.38), side & (zf < 0.45), 0.03 * S)
    J[sd + "Foot"] = shift(g(0.115 * sx, 0.10), side & (zf < 0.16) & (zf > 0.06), 0.025 * S)
    fp = P[side & (zf < 0.06) & (np.abs(X - J[sd + "Foot"][0]) < 0.06 * S)]
    J[sd + "ToeTip"] = np.array([J[sd + "Foot"][0], fp[:, 1].min(), 0.02 * S])
    J[sd + "Toes"] = np.array([J[sd + "Foot"][0], (J[sd + "Foot"][1] + fp[:, 1].min()) / 2, 0.035 * S])
    # the lower arm pair (ScreeB_*): measured from the source figure
    J["B" + sd + "1"] = shift(g(0.055 * sx, 0.655), side & (zf > 0.60) & (zf < 0.70), 0.025 * S)
    J["B" + sd + "2"] = shift(g(0.155 * sx, 0.605), side & (zf > 0.55) & (zf < 0.66), 0.025 * S)
    J["B" + sd + "3"] = shift(g(0.265 * sx, 0.515), side & (zf > 0.46) & (zf < 0.57), 0.022 * S)
    J["B" + sd + "4"] = shift(g(0.31 * sx, 0.47), side & (zf > 0.42) & (zf < 0.53) & (np.abs(X) > 0.25 * S), 0.03 * S)
    J[sd + "UpperLeg"][2] = min(J[sd + "UpperLeg"][2], J["Hips"][2] - 0.02 * S)
# abdomen chain: slice centroids along its (tucked) axis, from the waist to the tip
A = P[ABD]
root = np.array([0.0, 0.07 * S, pz])
ax = A.mean(0) - root
ax /= np.linalg.norm(ax)
tt = (A - root) @ ax
chain = []
for t in np.linspace(0.0, 1.0, 5):
    tv = tt.max() * min(t, 0.97)
    sl = A[np.abs(tt - tv) < 0.02 * S]
    chain.append(sl.mean(0) if len(sl) > 5 else root + ax * tv)
chain[0] = root + ax * 0.0
chain[-1] = root + ax * tt.max()
for i, c in enumerate(chain):
    J[f"Abd{i}"] = c
# antenna
AN = P[(zf > 0.94)]
ab = AN[AN[:, 2] < AN[:, 2].min() + 0.012 * S].mean(0)
atip = AN[np.argmax(np.linalg.norm(AN - ab, axis=1))]
amid_r = np.linalg.norm(atip - ab) / 2
sh_ = AN[np.abs(np.linalg.norm(AN - ab, axis=1) - amid_r) < 0.01 * S]
J["Ant0"], J["Ant1"], J["Ant2"] = ab, sh_.mean(0) if len(sh_) else (ab + atip) / 2, atip
# eyes (glints): the two dark eye bulbs (darkest texels on the front of the head), glint at their upper-outer front
uvl = me.uv_layers.active.data
TPX = np.empty(2048 * 2048 * 4, dtype=np.float32)
img2.pixels.foreach_get(TPX)
TPX = TPX.reshape(2048, 2048, 4)[:, :, 0]
lum = np.ones(len(P))
for poly in me.polygons:
    for li in poly.loop_indices:
        vi = me.loops[li].vertex_index
        u, v = uvl[li].uv
        lum[vi] = min(lum[vi], TPX[min(2047, max(0, int(v * 2047))), min(2047, max(0, int(u * 2047)))])
EYE = {}
m_ = (zf > 0.84) & (zf < 0.92) & (X > 0.012 * S) & (np.abs(X) < 0.075 * S) & (Y < J["Head"][1] + 0.02 * S)
thr = np.percentile(lum[m_], 15)
dark = P[m_ & (lum <= thr)]
cen = dark.mean(0)
near = dark[np.linalg.norm(dark - cen, axis=1) < 0.03]
gl = near[np.argmax((near - cen) @ np.array([0.25, -0.85, 0.45]))]
EYE["Left"] = gl
EYE["Right"] = gl * np.array([-1.0, 1.0, 1.0])          # the head is straightened: mirror the left eye's glint
log(f"eye (left, mirrored to the right): {len(dark)} dark verts, centre {fmt(cen)}, glint {fmt(gl)}")
# tank lights: back of each tank, at the middle of its height
TP = P[TANK & (Y > 0.09 * S)]
TL = []
if len(TP) > 50:
    xs = np.sort(TP[:, 0])
    gap = np.argmax(np.diff(xs))
    split = (xs[gap] + xs[gap + 1]) / 2 if np.diff(xs)[gap] > 0.004 * S else np.median(xs)
    for part in (TP[TP[:, 0] < split], TP[TP[:, 0] >= split]):
        zm = (part[:, 2].min() + part[:, 2].max()) / 2
        q = part[np.abs(part[:, 2] - zm) < 0.02 * S]
        TL.append(q[np.argmax(q[:, 1])] + np.array([0, 0.004 * S, 0]))
for k in sorted(J):
    log(f"joint {k:16s} {fmt(J[k])}")
log("eye glints", fmt(EYE["Left"]), fmt(EYE["Right"]), "tank lights", [fmt(t) for t in TL])

# ------------------------------------------------------------------ armature
arm_data = bpy.data.armatures.new("ScreeRig")
arm = bpy.data.objects.new("ScreeRig", arm_data)
bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="EDIT")
eb = arm_data.edit_bones


def V(a):
    return Vector((float(a[0]), float(a[1]), float(a[2])))


def bone(name, h, t, parent=None, roll_to=(0, -1, 0), connect=False):
    b = eb.new(name)
    b.head, b.tail = V(h), V(t)
    r = Vector(roll_to)
    if abs((b.tail - b.head).normalized().dot(r.normalized())) > 0.95:
        r = Vector((0, 0, 1))
    b.align_roll(r)
    if parent:
        b.parent = eb[parent]
        b.use_connect = connect
    return b


bone("Hips", J["Hips"], J["Spine"])
bone("Spine", J["Spine"], J["Chest"], "Hips", connect=True)
bone("Chest", J["Chest"], J["UpperChest"], "Spine", connect=True)
bone("UpperChest", J["UpperChest"], J["Neck"], "Chest", connect=True)
bone("Neck", J["Neck"], J["Head"], "UpperChest", connect=True)
bone("Head", J["Head"], J["HeadTop"], "Neck", connect=True)
for sd in ("Left", "Right"):
    bone(sd + "Shoulder", J[sd + "Shoulder"], J[sd + "UpperArm"], "UpperChest", roll_to=(0, 1, 0))
    bone(sd + "UpperArm", J[sd + "UpperArm"], J[sd + "LowerArm"], sd + "Shoulder", roll_to=(0, 1, 0), connect=True)
    bone(sd + "LowerArm", J[sd + "LowerArm"], J[sd + "Hand"], sd + "UpperArm", roll_to=(0, 1, 0), connect=True)
    bone(sd + "Hand", J[sd + "Hand"], J[sd + "HandTip"], sd + "LowerArm", roll_to=(0, 1, 0), connect=True)
    bone(sd + "UpperLeg", J[sd + "UpperLeg"], J[sd + "LowerLeg"], "Hips")
    bone(sd + "LowerLeg", J[sd + "LowerLeg"], J[sd + "Foot"], sd + "UpperLeg", connect=True)
    bone(sd + "Foot", J[sd + "Foot"], J[sd + "Toes"], sd + "LowerLeg", roll_to=(0, 0, 1), connect=True)
    bone(sd + "Toes", J[sd + "Toes"], J[sd + "ToeTip"], sd + "Foot", roll_to=(0, 0, 1), connect=True)
    bone(f"ScreeB_Upper_{sd}", J["B" + sd + "1"], J["B" + sd + "2"], "Chest", roll_to=(0, 1, 0))
    bone(f"ScreeB_Fore_{sd}", J["B" + sd + "2"], J["B" + sd + "3"], f"ScreeB_Upper_{sd}", roll_to=(0, 1, 0), connect=True)
    bone(f"ScreeB_Claw_{sd}", J["B" + sd + "3"], J["B" + sd + "4"], f"ScreeB_Fore_{sd}", roll_to=(0, 1, 0), connect=True)
bone("ScreeAbd1", J["Abd0"], J["Abd1"], "Hips", roll_to=(0, 0, 1))
for i in range(2, 5):
    bone(f"ScreeAbd{i}", J[f"Abd{i - 1}"], J[f"Abd{i}"], f"ScreeAbd{i - 1}", roll_to=(0, 0, 1), connect=True)
bone("ScreeAnt1", J["Ant0"], J["Ant1"], "Head")
bone("ScreeAnt2", J["Ant1"], J["Ant2"], "ScreeAnt1", connect=True)
bpy.ops.object.mode_set(mode="OBJECT")
log("bones", len(arm_data.bones))

# ------------------------------------------------------------------ glow mesh: eye glints + tank lights
gbm = bmesh.new()
glow_src = []
for sd in ("Left", "Right"):
    ret = bmesh.ops.create_uvsphere(gbm, u_segments=10, v_segments=6, radius=0.0085)
    bmesh.ops.translate(gbm, verts=ret["verts"], vec=V(EYE[sd]) + Vector((0.0, -0.002, 0.0)))
    glow_src.append(("Head", len(ret["verts"])))
for t in TL:
    ret = bmesh.ops.create_uvsphere(gbm, u_segments=10, v_segments=6, radius=0.011)
    bmesh.ops.translate(gbm, verts=ret["verts"], vec=V(t))
    glow_src.append(("UpperChest", len(ret["verts"])))
gme = bpy.data.meshes.new("scree_glow")
gbm.to_mesh(gme)
gbm.free()
glow = bpy.data.objects.new("scree_glow", gme)
bpy.context.scene.collection.objects.link(glow)
gmat = bpy.data.materials.new("scree_glow")
gmat.diffuse_color = (1, 1, 1, 1)
gme.materials.append(gmat)
i0 = 0
for bn, cnt in glow_src:
    gv = glow.vertex_groups.get(bn) or glow.vertex_groups.new(name=bn)
    gv.add(list(range(i0, i0 + cnt)), 1.0, "REPLACE")
    i0 += cnt

# ------------------------------------------------------------------ skin weights
bones = {b.name: (np.array(b.head_local), np.array(b.tail_local)) for b in arm_data.bones}


def seg_dist(Pts, a, b):
    ab = b - a
    t = np.clip(((Pts - a) @ ab) / max(ab @ ab, 1e-12), 0.0, 1.0)
    return np.linalg.norm(Pts - (a + t[:, None] * ab), axis=1)


names = list(bones)
NB = len(names)
D = np.full((len(P), NB), np.inf)
for j, nm in enumerate(names):
    D[:, j] = seg_dist(P, *bones[nm])
INF = np.inf
ix = {nm: j for j, nm in enumerate(names)}


def block(mask, allowed):
    """Points in mask may only use the allowed bones."""
    cols = [ix[n_] for n_ in names if n_ not in allowed]
    D[np.ix_(np.nonzero(mask)[0], cols)] = INF


def forbid(mask, bad):
    cols = [ix[n_] for n_ in bad]
    D[np.ix_(np.nonzero(mask)[0], cols)] = INF


ABDb = ["ScreeAbd1", "ScreeAbd2", "ScreeAbd3", "ScreeAbd4"]
block(ABD, ABDb + ["Hips"])
forbid(~ABD, ABDb)
ANT = zf > 0.94
block(ANT, ["ScreeAnt1", "ScreeAnt2", "Head"])
forbid(~ANT, ["ScreeAnt1", "ScreeAnt2"])
block(TANK, ["UpperChest", "Chest"])
HOSE = (zf > 0.66) & (zf < 0.87) & (Y < -0.025 * S) & (np.abs(X) < 0.09 * S) | ((zf > 0.70) & (zf < 0.86) & (Y > 0.0) & (Y < 0.08 * S) & (np.abs(X) > 0.04 * S) & (np.abs(X) < 0.12 * S) & ~TANK)
for sd, sx in (("Left", 1), ("Right", -1)):
    limb = [sd + k for k in ("Shoulder", "UpperArm", "LowerArm", "Hand", "UpperLeg", "LowerLeg", "Foot", "Toes")] + \
           [f"ScreeB_{k}_{sd}" for k in ("Upper", "Fore", "Claw")]
    forbid(X * sx < -0.012 * S, limb)
legs_b = [s + k for s in ("Left", "Right") for k in ("UpperLeg", "LowerLeg", "Foot", "Toes")]
forbid(zf > 0.60, legs_b)
arms_b = [s + k for s in ("Left", "Right") for k in ("UpperArm", "LowerArm", "Hand")] + \
         [f"ScreeB_{k}_{s}" for s in ("Left", "Right") for k in ("Upper", "Fore", "Claw")]
forbid((zf < 0.40) | (np.abs(X) < 0.035 * S), arms_b)
forbid(zf < 0.79, ["Head"])
forbid(zf < 0.72, ["Neck"])
forbid(HOSE, arms_b + legs_b)
# hose: from the head (top) to the upper chest (bottom) by height
k_near = 3
order = np.argsort(D, axis=1)[:, :k_near]
dn = np.take_along_axis(D, order, axis=1)
w = 1.0 / (dn * dn + 1e-7) ** 2
w[~np.isfinite(dn)] = 0.0
w /= w.sum(1, keepdims=True) + 1e-12
hz = smooth(0.78, 0.84, zf)
gr = {nm: sc.vertex_groups.new(name=nm) for nm in names}
WT = {}
for i in range(len(P)):
    ws = {}
    if HOSE[i]:
        ws = {"Head": float(hz[i]), "UpperChest": float(1 - hz[i])}
    else:
        for j in range(k_near):
            if w[i, j] > 0.01:
                ws[names[order[i, j]]] = float(w[i, j])
    tot = sum(ws.values()) or 1.0
    for nm, wv in ws.items():
        if wv / tot > 0.005:
            gr[nm].add([i], wv / tot, "REPLACE")
for o in (sc, glow):
    m_ = o.modifiers.new("Armature", "ARMATURE")
    m_.object = arm
    o.parent = arm
log("weights done")

# ------------------------------------------------------------------ pose helpers
pb = arm.pose.bones
for b in pb:
    b.rotation_mode = "QUATERNION"


def upd():
    bpy.context.view_layer.update()


def clear_pose():
    for b in pb:
        b.rotation_quaternion = Quaternion()
        b.location = Vector()
    upd()


def aim(name, direction, twist=0.0):
    """Point a bone along a world direction (armature space), keeping its head; children follow."""
    b = pb[name]
    M = b.matrix.copy()
    cur = Vector(M.col[1][:3]).normalized()
    r = cur.rotation_difference(Vector(direction).normalized())
    if twist:
        r = Quaternion(Vector(direction).normalized(), math.radians(twist)) @ r
    R = r.to_matrix().to_4x4()
    h = M.translation.copy()
    M.translation = Vector()
    M2 = R @ M
    M2.translation = h
    b.matrix = M2
    upd()


def turn(name, axis, deg):
    """Rotate a bone about a world axis through its head. About X: - = lean forward / nod down, + = straighten back / head up
    (the figure faces -Y)."""
    if tuple(axis) == (1, 0, 0):
        deg = -deg
    b = pb[name]
    M = b.matrix.copy()
    h = M.translation.copy()
    M.translation = Vector()
    M2 = Quaternion(Vector(axis), math.radians(deg)).to_matrix().to_4x4() @ M
    M2.translation = h
    b.matrix = M2
    upd()


# ------------------------------------------------------------------ T-pose rest (upper arm pair straight out)
clear_pose()
for sd, sx in (("Left", 1), ("Right", -1)):
    aim(sd + "UpperArm", (sx, 0, 0))
    aim(sd + "LowerArm", (sx, 0, 0))
    aim(sd + "Hand", (sx, 0, 0))
for o in (sc, glow):
    bpy.context.view_layer.objects.active = o
    for o2 in bpy.data.objects:
        o2.select_set(o2 == o)
    bpy.ops.object.modifier_apply(modifier="Armature")
bpy.context.view_layer.objects.active = arm
for o2 in bpy.data.objects:
    o2.select_set(o2 == arm)
bpy.ops.object.mode_set(mode="POSE")
bpy.ops.pose.armature_apply(selected=False)
bpy.ops.object.mode_set(mode="OBJECT")
for o in (sc, glow):
    m_ = o.modifiers.new("Armature", "ARMATURE")
    m_.object = arm
log("rest pose: upper arm pair straightened to a T-pose (applied as rest)")
pb = arm.pose.bones
for b in pb:
    b.rotation_mode = "QUATERNION"
P_REST = get_co(sc.data)


# ------------------------------------------------------------------ previews
def setup_render(res=(720, 720)):
    scn = bpy.context.scene
    scn.render.engine = "BLENDER_WORKBENCH"
    scn.display.shading.light = "STUDIO"
    scn.display.shading.color_type = "TEXTURE"
    scn.render.resolution_x, scn.render.resolution_y = res
    cam = bpy.data.objects.get("pcam")
    if cam is None:
        cd = bpy.data.cameras.new("pcam")
        cd.type = "ORTHO"
        cam = bpy.data.objects.new("pcam", cd)
        scn.collection.objects.link(cam)
    scn.camera = cam
    return scn, cam


def shoot(path, center, direction, ortho):
    scn, cam = setup_render()
    cam.data.ortho_scale = ortho
    dv = Vector(direction).normalized()
    cam.location = Vector(center) + dv * 12
    cam.rotation_euler = (-dv).to_track_quat("-Z", "Y").to_euler()
    cam.data.clip_end = 40
    scn.render.filepath = path
    bpy.ops.render.render(write_still=True)


VIEWS = (("front", (0, -1, 0.02)), ("side", (1, 0, 0.02)), ("back", (0, 1, 0.02)), ("q34", (0.7, -0.7, 0.2)), ("top", (0.0001, 0.0, 1)))
if PREVIEW:
    os.makedirs(PREVIEW, exist_ok=True)
    for vn, dv in VIEWS:
        shoot(os.path.join(PREVIEW, f"scree_rest_{vn}.png"), (0, 0.2, H / 2), dv, 2.6 if vn != "top" else 2.2)
    hc3 = Vector(pb["Head"].head) + Vector((0, 0, 0.09))
    shoot(os.path.join(PREVIEW, "scree_rest_face.png"), hc3, (0.15, -1, 0.05), 0.45)
    shoot(os.path.join(PREVIEW, "scree_rest_side_left.png"), (0, 0.2, H / 2), (-1, 0, 0.02), 2.6)
if STAGE == "fit":
    with open(METRICS, "w") as f:
        f.write("\n".join(_metrics) + "\n")
    log("STAGE fit done")
    sys.exit(0)

# ------------------------------------------------------------------ clips (humanoid bones only)
FOOT = np.nonzero(P_REST[:, 2] < 0.03 * S)[0]


def eval_low():
    dg = bpy.context.evaluated_depsgraph_get()
    ev = sc.evaluated_get(dg)
    mm = ev.to_mesh()
    a = np.empty(len(mm.vertices) * 3)
    mm.vertices.foreach_get("co", a)
    ev.to_mesh_clear()
    a = a.reshape(-1, 3)
    return a[:, 2].min(), a[FOOT, 2].min()


DOWN = {"Left": (0.28, 0.05, -1.0), "Right": (-0.28, 0.05, -1.0)}


def neutral():
    """Arms hanging relaxed (the starting point of both clips)."""
    clear_pose()
    for sd, sx in (("Left", 1), ("Right", -1)):
        aim(sd + "UpperArm", (0.32 * sx, 0.05, -1.0))
        aim(sd + "LowerArm", (0.22 * sx, -0.25, -1.0))
        aim(sd + "Hand", (0.15 * sx, -0.35, -1.0))


def work_pose(t):
    """t in [0,1): the loop. Bent over, the upper arms strike / lift in turn, lower arms brace."""
    neutral()
    ph = 2 * math.pi * t
    turn("Spine", (1, 0, 0), -12)
    turn("Chest", (1, 0, 0), -10 - 2 * math.sin(2 * ph))
    turn("UpperChest", (1, 0, 0), -6)
    turn("Neck", (1, 0, 0), 8)
    turn("Head", (1, 0, 0), -14 + 3 * math.sin(2 * ph + 0.6))
    for sd, sx, off in (("Left", 1, 0.0), ("Right", -1, math.pi)):
        s = math.sin(ph + off)
        up = 0.5 + 0.5 * s                         # 1 = raised, 0 = struck down
        aim(sd + "UpperArm", (0.30 * sx, -0.75 - 0.15 * up, -0.65 + 0.85 * up))
        aim(sd + "LowerArm", (0.08 * sx, -0.55 - 0.3 * (1 - up), -1.0 + 1.2 * up))
        aim(sd + "Hand", (0.05 * sx, -0.9, -0.6 + 0.6 * up))
    for sd in ("Left", "Right"):
        turn(sd + "UpperLeg", (1, 0, 0), 8)
        turn(sd + "LowerLeg", (1, 0, 0), -14)
        turn(sd + "Foot", (1, 0, 0), 6)


def alert_pose(t):
    """t seconds. 0-0.15 startle, 0.15-0.6 straighten + head round, then a watchful hold."""
    neutral()
    a = float(smooth(0.0, 0.18, t))
    b = float(smooth(0.12, 0.55, t))
    c = float(smooth(0.6, 1.3, t))
    turn("Spine", (1, 0, 0), 6 * a - 2 * c)
    turn("Chest", (1, 0, 0), 8 * a - 3 * c)
    turn("UpperChest", (1, 0, 0), 4 * a)
    turn("Neck", (1, 0, 0), 10 * a - 4 * c)
    turn("Head", (1, 0, 0), 16 * a - 8 * c)
    turn("Neck", (0, 0, 1), 18 * b - 12 * c)
    turn("Head", (0, 0, 1), 14 * b - 10 * c)
    for sd, sx in (("Left", 1), ("Right", -1)):
        raise_ = 0.55 * a - 0.2 * c
        aim(sd + "UpperArm", (0.42 * sx, -0.35 * a, -1.0 + raise_))
        aim(sd + "LowerArm", (0.10 * sx, -0.9 * a - 0.2, -1.0 + 1.3 * a - 0.5 * c))
        aim(sd + "Hand", (0.0, -1.0, 0.4 * a))
        turn(sd + "UpperLeg", (1, 0, 0), 6 * a)
        turn(sd + "LowerLeg", (1, 0, 0), -12 * a + 4 * c)


def key_all(frame):
    for b in pb:
        b.keyframe_insert("rotation_quaternion", frame=frame)
        b.keyframe_insert("location", frame=frame)


def new_action(name):
    if arm.animation_data is None:
        arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    return act


def ground():
    """Lower the hips so the lowest foot point stays on the floor (bent knees)."""
    low, _ = eval_low()
    pb["Hips"].location = Vector()
    upd()
    low, _ = eval_low()
    hb_ = pb["Hips"]
    M = hb_.matrix.copy()
    M.translation = M.translation - Vector((0, 0, low))
    hb_.matrix = M
    upd()
    return eval_low()[0]


clips = {}
WORK_F, ALERT_F = int(2.4 * FPS), int(1.4 * FPS)
act = new_action("work")
gaps = []
for f in range(WORK_F + 1):
    work_pose((f % WORK_F) / WORK_F)
    gaps.append(ground())
    key_all(f)
clips["work"] = act
log(f"work: {WORK_F} frames loop; lowest point after grounding {min(gaps) * 1000:.1f}..{max(gaps) * 1000:.1f} mm")
act = new_action("alert")
gaps = []
for f in range(ALERT_F + 1):
    alert_pose(f / FPS)
    gaps.append(ground())
    key_all(f)
clips["alert"] = act
log(f"alert: {ALERT_F} frames; lowest point after grounding {min(gaps) * 1000:.1f}..{max(gaps) * 1000:.1f} mm")

if PREVIEW:
    for nm, frames in (("work", (0, WORK_F // 4, WORK_F // 2, 3 * WORK_F // 4)), ("alert", (0, 5, 15, ALERT_F))):
        arm.animation_data.action = clips[nm]
        for fr in frames:
            bpy.context.scene.frame_set(fr)
            for vn, dv in (("q34", (0.7, -0.7, 0.15)), ("side", (1, 0, 0.02))):
                shoot(os.path.join(PREVIEW, f"scree_{nm}_{fr:02d}_{vn}.png"), (0, 0.1, H / 2), dv, 2.6)

arm.animation_data.action = None
for nm, act in clips.items():
    tr = arm.animation_data.nla_tracks.new()
    tr.name = nm
    tr.strips.new(nm, 0, act)
    tr.mute = True
scn = bpy.context.scene
scn.render.fps = FPS
scn.frame_start = 0
scn.frame_end = WORK_F
clear_pose()

# ------------------------------------------------------------------ export
for o in bpy.data.objects:
    o.select_set(o in (arm, sc, glow))
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(
    filepath=FBX, use_selection=True, object_types={"MESH", "ARMATURE"},
    apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
    use_mesh_modifiers=False, mesh_smooth_type="FACE", add_leaf_bones=False, use_armature_deform_only=False,
    bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True,
    bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0, path_mode="COPY", embed_textures=False)
log("exported", FBX, "tris", tris(sc), "+ glow", tris(glow), "bones", len(arm_data.bones), f"height {HEIGHT} m")
with open(METRICS, "w") as f:
    f.write("\n".join(_metrics) + "\n")
