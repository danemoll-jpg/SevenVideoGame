"""Clean up, rig and animate Ratwing (Round 4) for Unity.

Run headless from the repo root:
  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/cleanup_ratwing.py -- [repo_root] [target_tris] [preview_dir]

Input : Art/Source/ratwing/ratwing_v1.glb   (Tripo, unrigged, ~1.9M tris, 0.75 units tall incl. wing hooks, faces -Y;
                                             one welded island; never modified)
Output: Art/Export/ratwing/ratwing_v1_rigged.fbx            (mesh + armature + eye-glint mesh + clips "sequence", "idle")
        Art/Export/ratwing/Textures/ratwing_v1_basecolor.jpg (grayscale, 2048)
        Art/Export/ratwing/ratwing_metrics.txt               (fit, decimation, wing stretch, hang / landing contact)
        [preview_dir]/rw_*.png                               (rest views + frames of the sequence, if a preview dir is given)

Steps: weld -> scale so the standing height (head top, wing hooks ignored) is HEIGHT -> origin between the feet on the
floor, facing -Y -> decimate to ~target with the wing membranes, finger spars and claws protected (vertex-group
weighted collapse, X-symmetric) -> scripted deform rig fitted from mesh slices (body: hips/spine/chest/neck/head/jaw,
arms with hand, legs with digitigrade foot + toes; wings: root, upper, fore + 2-segment finger spars found from the
membrane's trailing-edge cusps) -> skin weights (distance-based on the body, angular "fan" weights on the membranes so
a fold compresses them like a fan instead of tearing) -> keyframed clips:
  sequence (10 s, 30 fps): 0-3.5 asleep hanging by the feet, wings wrapped | 3.5-4.5 wakes, head lifts / turns |
  4.5-6.5 wings unfurl in a big stretch, hiss | 6.5-8 drop + half-roll upright, wings half open | 8-10 crouch landing,
  wings spread, rises to a menacing stance.
  idle (2 s loop): the final stance, breathing.
Hanging: the feet's top is pinned to CEIL (the cave ledge underside in Unity, metres above the floor point under him);
landing: the lowest foot point is pinned to the floor. Not Rigify: a Rigify control rig would have to be baked back to
deform bones for the FBX anyway; this deform rig is generated straight from the mesh (Round 3 cat approach).
"""
import bpy, bmesh, os, sys, math
from mathutils import Vector, Quaternion, Matrix

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = os.path.abspath(argv[0]) if len(argv) > 0 else os.getcwd()
TARGET_TRIS = int(argv[1]) if len(argv) > 1 else 55000
PREVIEW = os.path.abspath(argv[2]) if len(argv) > 2 else None
STAGE = os.environ.get("RW_STAGE", "full")      # "fit" stops after the rig fit (debug previews)

HEIGHT = 2.10          # standing height in metres (Dan may change this one constant; Unity reads the FBX as is)
CEIL = 4.20            # ceiling-ledge underside above the floor in the cave (Unity builder uses the same number)
LAND_Z = -0.55         # he lands this far toward the camera (-Y in Blender = toward the camera in Unity)
FPS = 30

SRC = os.path.join(ROOT, "Art", "Source", "ratwing", "ratwing_v1.glb")
OUT_DIR = os.path.join(ROOT, "Art", "Export", "ratwing")
TEX_DIR = os.path.join(OUT_DIR, "Textures")
FBX = os.path.join(OUT_DIR, "ratwing_v1_rigged.fbx")
TEX = os.path.join(TEX_DIR, "ratwing_v1_basecolor.jpg")
METRICS = os.path.join(OUT_DIR, "ratwing_metrics.txt")
os.makedirs(TEX_DIR, exist_ok=True)
_metrics = []


def log(*a):
    s = " ".join(str(x) for x in a)
    print("[cleanup_ratwing]", s, flush=True)
    _metrics.append(s)


def tris(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def smooth(e0, e1, x):
    if e1 == e0:
        return 1.0 if x >= e1 else 0.0
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


def lerp(a, b, t):
    return a + (b - a) * t


def fmt(v):
    return "(" + ", ".join(f"{x:.3f}" for x in v) + ")"


# ------------------------------------------------------------------ import, weld, scale, origin
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SRC)
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
for o in bpy.data.objects:
    o.select_set(o.type == "MESH")
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1:
    bpy.ops.object.join()
rw = bpy.context.view_layer.objects.active
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for o in list(bpy.data.objects):
    if o != rw:
        bpy.data.objects.remove(o)
rw.name = "ratwing_body"
log("source", len(rw.data.vertices), "verts", tris(rw), "tris")

bm = bmesh.new()
bm.from_mesh(rw.data)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
bm.to_mesh(rw.data)
bm.free()
me = rw.data

co = [v.co for v in me.vertices]
head_top = max(c.z for c in co if abs(c.x) < 0.06 and c.y < 0.06)
feet = [c for c in co if c.z < 0.02]
fcx = sum(c.x for c in feet) / len(feet)
fcy = sum(c.y for c in feet) / len(feet)
S = HEIGHT / head_top
log(f"head top {head_top:.4f} src units -> scale {S:.4f}; feet centre ({fcx:.4f}, {fcy:.4f})")
for v in me.vertices:
    p = v.co
    v.co = Vector(((p.x - fcx) * S, (p.y - fcy) * S, p.z * S))
me.update()


def src(x, y, z):
    """Source-unit point (probe measurements) -> metres in the final frame."""
    return Vector(((x - fcx) * S, (y - fcy) * S, z * S))


H = HEIGHT
WING_Y0, WING_Y1 = src(0, 0.06, 0).y, src(0, 0.085, 0).y      # membranes sit behind the back
WING_Z0 = 0.27 * S


def wingness(c):
    """0..1: how much a (rest-pose) point belongs to a wing (behind the back, off the spine, above the hips)."""
    w = smooth(WING_Y0, WING_Y1, c.y) * smooth(0.035 * S, 0.06 * S, abs(c.x)) * smooth(WING_Z0 - 0.02 * S, WING_Z0, c.z)
    # wide of the shoulders and above them, or beside the head: only the wing arm / leading edge lives there
    far = smooth(0.40 / 2.1 * H, 0.46 / 2.1 * H, abs(c.x)) * smooth(1.72 / 2.1 * H, 1.80 / 2.1 * H, c.z)
    high = smooth(0.24 / 2.1 * H, 0.30 / 2.1 * H, abs(c.x)) * smooth(1.98 / 2.1 * H, 2.05 / 2.1 * H, c.z)
    return max(w, far, high)


# ------------------------------------------------------------------ decimate (membranes + claws protected)
PROTECT_W = float(os.environ.get("RW_PROTECT_W", "0.75"))
PROTECT_FACTOR = float(os.environ.get("RW_PROTECT", "1.0"))
protect = rw.vertex_groups.new(name="protect")
n_wing_src = 0
for v in me.vertices:
    c = v.co
    w = wingness(c)
    n_wing_src += w > 0.5
    claws_hand = c.z < 0.36 * S and abs(c.x) > 0.25 * S and c.y < 0.03 * S
    claws_foot = c.z < 0.03 * S
    # Collapse cost is multiplied by 1 + (2 - w1 - w2) * factor: weight 1 = normal, lower = protected (0 = locked).
    protect.add([v.index], PROTECT_W if (w > 0.5 or claws_hand or claws_foot) else 1.0, "REPLACE")
n0 = tris(rw)


def region_tris(ob):
    me_ = ob.data
    n = 0
    for p in me_.polygons:
        if all(wingness(me_.vertices[i].co) > 0.5 for i in p.vertices):
            n += len(p.vertices) - 2
    return n


wing_t0 = region_tris(rw)
mod = rw.modifiers.new("dec", "DECIMATE")
mod.decimate_type = "COLLAPSE"
mod.ratio = min(1.0, TARGET_TRIS / n0)
mod.use_symmetry = True
mod.symmetry_axis = "X"
mod.vertex_group = "protect"
mod.vertex_group_factor = PROTECT_FACTOR
mod.invert_vertex_group = False
bpy.ops.object.select_all(action="DESELECT")
rw.select_set(True)
bpy.context.view_layer.objects.active = rw
bpy.ops.object.modifier_apply(modifier="dec")
rw.vertex_groups.remove(rw.vertex_groups["protect"])
me = rw.data
wing_t1 = region_tris(rw)
bm = bmesh.new()
bm.from_mesh(me)
bnd = sum(1 for e in bm.edges if e.is_boundary)
nonman = sum(1 for e in bm.edges if not e.is_manifold)
bm.free()
log(f"decimated {n0} -> {tris(rw)} tris; wing membranes {wing_t0} -> {wing_t1} tris "
    f"({100 * wing_t0 / n0:.0f}% -> {100 * wing_t1 / max(1, tris(rw)):.0f}% of the mesh); boundary (hole) edges {bnd}, non-manifold {nonman}")

# ------------------------------------------------------------------ material: grayscale 2048 texture
mat = rw.material_slots[0].material
mat.name = "ratwing_skin"
img = next((n.image for n in mat.node_tree.nodes if n.type == "TEX_IMAGE" and n.image is not None), None)
if img is not None:
    img2 = img.copy()
    img2.scale(2048, 2048)
    px = list(img2.pixels)
    for i in range(0, len(px), 4):
        g = 0.2126 * px[i] + 0.7152 * px[i + 1] + 0.0722 * px[i + 2]
        px[i] = px[i + 1] = px[i + 2] = g
    img2.pixels[:] = px
    img2.filepath_raw = TEX
    img2.file_format = "JPEG"
    img2.save()
    for n in mat.node_tree.nodes:
        if n.type == "TEX_IMAGE":
            n.image = img2
    log("texture", TEX)
mat.use_backface_culling = False

# ------------------------------------------------------------------ fit the rig from the mesh
co = [v.co.copy() for v in me.vertices]
WING = [wingness(c) for c in co]
body_pts = [c for c, w in zip(co, WING) if w < 0.2]
wing_pts = [c for c, w in zip(co, WING) if w > 0.8]


def shift(guess, pts, r, it=6, pred=None):
    """Mean-shift: centroid of the points within r of the guess (repeated). Keeps the guess if empty."""
    p = guess.copy()
    for _ in range(it):
        near = [c for c in pts if (c - p).length < r and (pred is None or pred(c))]
        if len(near) < 5:
            break
        p = sum(near, Vector()) / len(near)
    return p


J = {}
J["hips"] = shift(src(0, -0.01, 0.40), body_pts, 0.05 * S, pred=lambda c: abs(c.x) < 0.07 * S)
J["spine"] = shift(src(0, -0.01, 0.47), body_pts, 0.045 * S, pred=lambda c: abs(c.x) < 0.07 * S)
J["chest"] = shift(src(0, -0.01, 0.535), body_pts, 0.045 * S, pred=lambda c: abs(c.x) < 0.07 * S)
J["neck"] = shift(src(0, -0.02, 0.59), body_pts, 0.03 * S, pred=lambda c: abs(c.x) < 0.05 * S)
J["head"] = shift(src(0, -0.035, 0.625), body_pts, 0.03 * S, pred=lambda c: abs(c.x) < 0.05 * S)
for k in ("hips", "spine", "chest", "neck", "head"):
    J[k].x = 0.0
J["head_top"] = Vector((0, J["head"].y, HEIGHT))
WANG = {}
snout = min((c for c in body_pts if abs(c.x) < 0.02 * S and c.z > J["neck"].z), key=lambda c: c.y)
J["snout"] = snout
chin_pts = [c for c in body_pts if abs(c.x) < 0.03 * S and src(0, 0, 0.555).z < c.z < src(0, 0, 0.615).z and c.y < J["head"].y]
J["jaw_tip"] = min(chin_pts, key=lambda c: c.y)           # front of the lower jaw (the mouth is modelled open)
J["jaw"] = Vector((0, J["head"].y + 0.01 * S, lerp(J["jaw_tip"].z, snout.z, 0.45) + 0.01 * S))   # hinge behind the mouth corner
eye_z = src(0, 0, 0.640).z
for side, sx in (("L", 1), ("R", -1)):
    ex = 0.019 * S * sx
    near = [c for c in body_pts if math.hypot(c.x - ex, c.z - eye_z) < 0.006 * S and c.y < J["head"].y]
    e = min(near, key=lambda c: c.y) if near else Vector((ex, snout.y + 0.05, eye_z))
    J[f"eye.{side}"] = e
_ex = (abs(J["eye.L"].x) + abs(J["eye.R"].x)) / 2
_ey = min(J["eye.L"].y, J["eye.R"].y)
_ez = (J["eye.L"].z + J["eye.R"].z) / 2
J["eye.L"], J["eye.R"] = Vector((_ex, _ey, _ez)), Vector((-_ex, _ey, _ez))
for side, sx in (("L", 1), ("R", -1)):
    def m(x, y, z):
        return src(sx * x, y, z)
    sp = lambda c: c.x * sx > 0.02 * S
    J[f"shoulder.{side}"] = shift(m(0.09, -0.01, 0.575), body_pts, 0.03 * S, pred=sp)
    J[f"elbow.{side}"] = shift(m(0.215, -0.03, 0.465), body_pts, 0.025 * S, pred=sp)
    J[f"wrist.{side}"] = shift(m(0.295, -0.07, 0.375), body_pts, 0.022 * S, pred=sp)
    hand = [c for c in body_pts if c.x * sx > 0 and (c - J[f"wrist.{side}"]).length < 0.09 * S and c.z < J[f"wrist.{side}"].z]
    J[f"hand_tip.{side}"] = (sum(hand, Vector()) / len(hand)) if hand else m(0.33, -0.08, 0.31)
    J[f"thigh.{side}"] = shift(m(0.065, -0.005, 0.365), body_pts, 0.035 * S, pred=sp)
    J[f"knee.{side}"] = shift(m(0.08, -0.03, 0.20), body_pts, 0.025 * S, pred=sp)
    J[f"ankle.{side}"] = shift(m(0.115, -0.02, 0.06), body_pts, 0.02 * S, pred=sp)
    toes = [c for c in body_pts if c.x * sx > 0 and c.z < 0.02 * S]
    tip_y = min(c.y for c in toes)
    front = [c for c in toes if c.y < tip_y + 0.03 * S]
    J[f"ball.{side}"] = shift(m(0.12, -0.07, 0.015), body_pts, 0.02 * S, pred=lambda c: c.x * sx > 0 and c.z < 0.04 * S)
    J[f"toe.{side}"] = sum(front, Vector()) / len(front)
    J[f"toe.{side}"].z = 0.004 * S
    # wing
    wp = [c for c in wing_pts if c.x * sx > 0]
    allback = [c for c in co if c.x * sx > 0 and c.y > WING_Y0 - 0.02 * S]
    J[f"wroot.{side}"] = shift(m(0.07, 0.075, 0.58), allback, 0.025 * S)
    J[f"welbow.{side}"] = shift(m(0.13, 0.10, 0.595), allback, 0.02 * S)
    # wrist: highest wing point that is still on the arm (below the hook claw), then mean-shift
    J[f"wwrist.{side}"] = shift(m(0.185, 0.11, 0.72), wp, 0.018 * S)
    # spar tips: local maxima of the membrane's radius around the wrist (trailing-edge cusps + outer tip)
    wr = J[f"wwrist.{side}"]
    bins = {}
    for c in wp:
        d = c - wr
        r = math.hypot(d.x * sx, d.z)
        if r < 0.08 * S:
            continue
        a = math.degrees(math.atan2(d.z, d.x * sx))          # 0 = straight out, -90 = straight down
        if a > 15 or a < -175:
            continue
        b = int(math.floor(a / 2.0))
        if b not in bins or r > bins[b][0]:
            bins[b] = (r, c.copy(), a)
    keys = sorted(bins)
    peaks = []
    for i, b in enumerate(keys):
        r = bins[b][0]
        nb = [bins[k][0] for k in keys if k != b and abs(k - b) <= 5]
        if nb and r >= max(nb):
            peaks.append(bins[b])
    peaks.sort(key=lambda p: -p[0])
    sel = []
    for p in peaks:
        if all(abs(p[2] - q[2]) > 9 for q in sel) and p[0] > 0.2 * S:
            sel.append(p)
        if len(sel) == 4:
            break
    sel.sort(key=lambda p: -p[2])      # leading edge (highest angle) first
    # A cusp lying along the wing arm (wrist -> elbow direction) is where the membrane's trailing edge meets the
    # body side, not a finger: it becomes the "edge" point (membrane there follows the wing root / chest).
    dv = J[f"welbow.{side}"] - wr
    arm_a = math.degrees(math.atan2(dv.z, dv.x * sx))
    WANG[side] = {"arm": arm_a, "r_elbow": math.hypot(dv.x, dv.z)}
    if sel and abs(sel[-1][2] - arm_a) < 14:
        J[f"wedge.{side}"] = sel[-1][1]
        WANG[side]["r_edge"] = sel[-1][0]
        log(f"wing {side} body-edge cusp at angle {sel[-1][2]:.0f} deg (arm {arm_a:.0f}), r {sel[-1][0]:.3f} m")
        sel = sel[:-1]
    WANG[side]["spars"] = []
    for k, (r, c, a) in enumerate(sel):
        J[f"spar{k + 1}.{side}"] = c
        WANG[side]["spars"].append((a, r))
        log(f"wing {side} spar {k + 1}: angle {a:.0f} deg, length {r:.3f} m, tip {fmt(c)}")
    log(f"wing {side} peaks considered: " + ", ".join(f"{p[2]:.0f}/{p[0]:.2f}" for p in peaks[:10]))
NSPAR = min(sum(1 for k in J if k.startswith("spar") and k.endswith(".L")), sum(1 for k in J if k.startswith("spar") and k.endswith(".R")))
for k in sorted(J):
    log(f"joint {k:14s} {fmt(J[k])}")

# ------------------------------------------------------------------ armature
arm_data = bpy.data.armatures.new("RatwingRig")
arm = bpy.data.objects.new("RatwingRig", arm_data)
bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="EDIT")
eb = arm_data.edit_bones


def bone(name, h, t, parent=None, roll_to=Vector((0, -1, 0)), connect=False):
    b = eb.new(name)
    b.head = h
    b.tail = t
    if abs((t - h).normalized().dot(roll_to.normalized())) > 0.95:
        roll_to = Vector((0, 0, 1))
    b.align_roll(roll_to)
    if parent:
        b.parent = eb[parent]
        b.use_connect = connect
    return b


bone("root", Vector((0, 0, 0)), Vector((0, -0.25, 0)))
bone("hips", J["hips"], J["spine"], "root", roll_to=Vector((0, -1, 0)))
bone("spine", J["spine"], J["chest"], "hips", connect=True)
bone("chest", J["chest"], J["neck"], "spine", connect=True)
bone("neck", J["neck"], J["head"], "chest", connect=True)
bone("head", J["head"], J["head_top"], "neck", connect=True)
bone("jaw", J["jaw"], J["jaw_tip"], "head", roll_to=Vector((0, 0, 1)))
eye_mid = (J["eye.L"] + J["eye.R"]) / 2
bone("eyes", eye_mid, eye_mid + Vector((0, -0.06, 0)), "head", roll_to=Vector((0, 0, 1)))
for side in ("L", "R"):
    bone(f"upper_arm.{side}", J[f"shoulder.{side}"], J[f"elbow.{side}"], "chest", roll_to=Vector((0, 1, 0)))
    bone(f"forearm.{side}", J[f"elbow.{side}"], J[f"wrist.{side}"], f"upper_arm.{side}", roll_to=Vector((0, 1, 0)), connect=True)
    bone(f"hand.{side}", J[f"wrist.{side}"], J[f"hand_tip.{side}"], f"forearm.{side}", roll_to=Vector((0, 1, 0)), connect=True)
    bone(f"thigh.{side}", J[f"thigh.{side}"], J[f"knee.{side}"], "hips", roll_to=Vector((0, -1, 0)))
    bone(f"shin.{side}", J[f"knee.{side}"], J[f"ankle.{side}"], f"thigh.{side}", roll_to=Vector((0, -1, 0)), connect=True)
    bone(f"foot.{side}", J[f"ankle.{side}"], J[f"ball.{side}"], f"shin.{side}", roll_to=Vector((0, 0, 1)), connect=True)
    bone(f"toes.{side}", J[f"ball.{side}"], J[f"toe.{side}"], f"foot.{side}", roll_to=Vector((0, 0, 1)), connect=True)
    bone(f"wing_root.{side}", J[f"wroot.{side}"], J[f"welbow.{side}"], "chest", roll_to=Vector((0, 1, 0)))
    bone(f"wing_upper.{side}", J[f"welbow.{side}"], J[f"wwrist.{side}"], f"wing_root.{side}", roll_to=Vector((0, 1, 0)), connect=True)
    for k in range(NSPAR):
        tip = J[f"spar{k + 1}.{side}"]
        mid = J[f"wwrist.{side}"].lerp(tip, 0.5)
        bone(f"wing_f{k + 1}a.{side}", J[f"wwrist.{side}"], mid, f"wing_upper.{side}", roll_to=Vector((0, 1, 0)))
        bone(f"wing_f{k + 1}b.{side}", mid, tip, f"wing_f{k + 1}a.{side}", roll_to=Vector((0, 1, 0)), connect=True)
bpy.ops.object.mode_set(mode="OBJECT")
log("bones", len(arm_data.bones))


# ------------------------------------------------------------------ debug previews of the fit
def setup_render(res=(640, 640), color_type="TEXTURE"):
    scn = bpy.context.scene
    scn.render.engine = "BLENDER_WORKBENCH"
    scn.display.shading.light = "STUDIO"
    scn.display.shading.color_type = color_type
    scn.display.shading.single_color = (0.55, 0.55, 0.55)
    scn.render.resolution_x, scn.render.resolution_y = res
    scn.render.film_transparent = False
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
    d = direction.normalized()
    cam.location = center + d * 12
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.data.clip_end = 40
    scn.render.filepath = path
    bpy.ops.render.render(write_still=True)


if STAGE == "fit":
    sticks = bpy.data.meshes.new("sticks")
    sv, sf = [], []
    for b in arm_data.bones:
        a_, b_ = b.head_local, b.tail_local
        d = (b_ - a_)
        if d.length < 1e-5:
            continue
        side = d.cross(Vector((0, 1, 0)) if abs(d.normalized().y) < 0.9 else Vector((1, 0, 0))).normalized() * 0.012
        side2 = d.cross(side).normalized() * 0.012
        i = len(sv)
        sv += [a_ - side, a_ + side, b_, a_ - side2, a_ + side2]
        sf += [(i, i + 1, i + 2), (i + 3, i + 4, i + 2)]
    sticks.from_pydata([tuple(v) for v in sv], [], sf)
    so = bpy.data.objects.new("sticks", sticks)
    bpy.context.scene.collection.objects.link(so)
    sm = bpy.data.materials.new("stick")
    sm.diffuse_color = (1, 1, 1, 1)
    sticks.materials.append(sm)
    bpy.context.scene.display.shading.show_xray = True
    bpy.context.scene.display.shading.xray_alpha = 0.25
    os.makedirs(PREVIEW, exist_ok=True)
    for vn, d in (("front", Vector((0, -1, 0))), ("back", Vector((0, 1, 0))), ("side", Vector((1, 0, 0)))):
        shoot(os.path.join(PREVIEW, f"rwfit_{vn}.png"), Vector((0, 0, H / 2 + 0.05)), d, 3.2)
    so.hide_render = True
    bpy.context.scene.display.shading.show_xray = False
    for vn, d in (("front", Vector((0, -1, 0))), ("q34", Vector((0.7, -0.7, 0.25)))):
        shoot(os.path.join(PREVIEW, f"rwdec_{vn}.png"), Vector((0, 0, H / 2 + 0.05)), d, 3.2)
    with open(METRICS, "w") as f:
        f.write("\n".join(_metrics) + "\n")
    log("STAGE fit done")
    sys.exit(0)

# ------------------------------------------------------------------ eye glints (two small spheres on the "eyes" bone)
EYE_R = 0.013
bm = bmesh.new()
for side in ("L", "R"):
    c = J[f"eye.{side}"] + Vector((0, -EYE_R * 0.15, 0))      # mostly out of the socket so both read from the side
    ret = bmesh.ops.create_uvsphere(bm, u_segments=10, v_segments=6, radius=EYE_R)
    bmesh.ops.translate(bm, verts=ret["verts"], vec=c)
eme = bpy.data.meshes.new("ratwing_eyes")
bm.to_mesh(eme)
bm.free()
eyes_obj = bpy.data.objects.new("ratwing_eyes", eme)
bpy.context.scene.collection.objects.link(eyes_obj)
emat = bpy.data.materials.new("ratwing_eyeglow")
emat.diffuse_color = (1, 1, 1, 1)
eme.materials.append(emat)
log("eye glints at", fmt(J["eye.L"]), fmt(J["eye.R"]), "radius", EYE_R)

# ------------------------------------------------------------------ skin weights
bones = {b.name: (b.head_local.copy(), b.tail_local.copy()) for b in arm_data.bones}
BODY_BONES = [n for n in bones if not n.startswith("wing") and n not in ("root", "eyes", "jaw")]


def seg_dist(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-9)))
    return (p - (a + ab * t)).length


def side_of(n):
    return n[-1] if n.endswith((".L", ".R")) else None


def body_weights(p):
    cand = []
    for n in BODY_BONES:
        a, b = bones[n]
        sd = side_of(n)
        sx = 1 if sd == "L" else -1
        if sd and p.x * sx < -0.015:
            continue
        leg = n.startswith(("thigh", "shin", "foot", "toes"))
        armb = n.startswith(("upper_arm", "forearm", "hand"))
        if leg and p.z > J["hips"].z + 0.06:
            continue
        if armb and (p.z > J[f"shoulder.{sd}"].z + 0.10 or abs(p.x) < abs(J[f"shoulder.{sd}"].x) * 0.7):
            continue
        if n in ("head", "neck") and p.z < J["chest"].z:
            continue
        d = seg_dist(p, a, b)
        if leg and p.z < J["hips"].z - 0.18:
            d *= 0.6
        if armb and abs(p.x) > abs(J[f"shoulder.{sd}"].x) + 0.10:
            d *= 0.6
        cand.append((d, n))
    cand.sort()
    cand = cand[:3]
    ws = [(1.0 / (d * d + 1e-6) ** 2, n) for d, n in cand]
    tot = sum(w for w, _ in ws)
    return {n: w / tot for w, n in ws}


HINGE = J["jaw"]


def yz_angle(v):
    return math.degrees(math.atan2(v.z - HINGE.z, HINGE.y - v.y))      # 0 = straight forward, + = up


A_CHIN, A_SNOUT = yz_angle(J["jaw_tip"]), yz_angle(J["snout"])
A_MOUTH = (A_CHIN + A_SNOUT) / 2
log(f"jaw: chin {A_CHIN:.0f} deg, snout {A_SNOUT:.0f} deg, mouth line {A_MOUTH:.0f} deg from the hinge {fmt(HINGE)}")


def jaw_weight(p):
    if p.y > HINGE.y + 0.02 or abs(p.x) > 0.13 or p.z < J["neck"].z - 0.10:
        return 0.0
    a = yz_angle(p)
    r = math.hypot(p.y - HINGE.y, p.z - HINGE.z)
    w = 1.0 - smooth(A_MOUTH - 3, A_MOUTH + 3, a)         # below the mouth line
    w *= 1.0 - smooth(A_CHIN - 28, A_CHIN - 45, a) if a < A_CHIN - 28 else 1.0   # not the throat
    w *= smooth(0.03, 0.08, r)
    return w


def wing_weights(p, side):
    sx = 1 if side == "L" else -1
    W = WANG[side]
    wr = J[f"wwrist.{side}"]
    d = p - wr
    r = math.hypot(d.x * sx, d.z)
    a = math.degrees(math.atan2(d.z, d.x * sx))
    out = {}

    def add(n, w):
        if w > 0:
            out[n] = out.get(n, 0.0) + w

    def finger(k, w):
        u = smooth(0.3, 0.7, r / W["spars"][k][1])
        add(f"wing_f{k + 1}a.{side}", w * (1 - u))
        add(f"wing_f{k + 1}b.{side}", w * u)

    def inner(w):
        r_el = W["r_elbow"]
        r_edge = W.get("r_edge", r_el * 1.8)
        u = smooth(r_el * 0.8, r_edge, r)
        add(f"wing_upper.{side}", w * (1 - u))
        add(f"wing_root.{side}", w * u)

    angs = [x for x, _ in W["spars"]]
    if a > 60:
        add(f"wing_upper.{side}", 1.0)
    elif a >= angs[0]:
        finger(0, 1.0)
    elif a <= W["arm"]:
        inner(1.0)
    else:
        seq = angs + [W["arm"]]
        for k in range(len(seq) - 1):
            if seq[k] >= a >= seq[k + 1]:
                t = (seq[k] - a) / (seq[k] - seq[k + 1])
                finger(k, 1 - t)
                if k + 1 < len(angs):
                    finger(k + 1, t)
                else:
                    inner(t)
                break
    nw = 1.0 - smooth(0.05, 0.14, r)
    out = {n: w * (1 - nw) for n, w in out.items()}
    add(f"wing_upper.{side}", nw)
    du = seg_dist(p, *bones[f"wing_upper.{side}"])
    dr = seg_dist(p, *bones[f"wing_root.{side}"])
    aw = 1.0 - smooth(0.035, 0.07, min(du, dr))
    if aw > 0:
        out = {n: w * (1 - aw) for n, w in out.items()}
        iu, ir = 1 / (du ** 4 + 1e-9), 1 / (dr ** 4 + 1e-9)
        add(f"wing_upper.{side}", aw * iu / (iu + ir))
        add(f"wing_root.{side}", aw * ir / (iu + ir))
    return out


rw.vertex_groups.clear()
groups = {n: rw.vertex_groups.new(name=n) for n in bones if n not in ("root", "eyes")}
infl_hist = [0] * 9
for v in me.vertices:
    p = v.co
    wg = wingness(p)
    ws = {}
    if wg < 1.0:
        for n, w in body_weights(p).items():
            ws[n] = ws.get(n, 0) + w * (1 - wg)
    if wg > 0.0:
        for n, w in wing_weights(p, "L" if p.x > 0 else "R").items():
            ws[n] = ws.get(n, 0) + w * wg
    jw = jaw_weight(p)
    if jw > 0:
        ws = {n: w * (1 - jw) for n, w in ws.items()}
        ws["jaw"] = ws.get("jaw", 0) + jw
    top = sorted(ws.items(), key=lambda kv: -kv[1])[:4]
    top = [(n, w) for n, w in top if w > 0.01]
    tot = sum(w for _, w in top)
    infl_hist[len(top)] += 1
    for n, w in top:
        groups[n].add([v.index], w / tot, "REPLACE")
m = rw.modifiers.new("Armature", "ARMATURE")
m.object = arm
rw.parent = arm
eyes_obj.vertex_groups.new(name="eyes").add([v.index for v in eme.vertices], 1.0, "REPLACE")
m2 = eyes_obj.modifiers.new("Armature", "ARMATURE")
m2.object = arm
eyes_obj.parent = arm
log("weights done; influences per vertex histogram", infl_hist[:5])

# ------------------------------------------------------------------ pose machinery
pb = arm.pose.bones
for b in pb:
    b.rotation_mode = "QUATERNION"
REST = {b.name: b.bone.matrix_local.to_quaternion() for b in pb}
AX, AY, AZ = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
IDQ = Quaternion()


def E(p=0.0, y=0.0, r=0.0):
    """Armature-axis rotation (degrees): pitch about X, yaw about Z, roll about Y (applied pitch, roll, then yaw)."""
    return Quaternion(AZ, math.radians(y)) @ Quaternion(AX, math.radians(p)) @ Quaternion(AY, math.radians(r))


def mir(q):
    return Quaternion((q.w, q.x, -q.y, -q.z))


def both(d):
    """Pose given for the .L side (and centre bones) -> both sides (mirrored across X)."""
    out = {}
    for n, q in d.items():
        if n.endswith(".L"):
            out[n] = q
            out[n[:-2] + ".R"] = mir(q)
        else:
            out[n] = q
    return out


def blend(A, B, t):
    t = max(0.0, min(1.0, t))
    return {n: A.get(n, IDQ).slerp(B.get(n, IDQ), t) for n in set(A) | set(B)}


def addp(A, extra):
    out = dict(A)
    for n, q in extra.items():
        out[n] = q @ out.get(n, IDQ)
    return out


def finger_fold(k_amounts, extra_b=None):
    d = {}
    for k in range(NSPAR):
        d[f"wing_f{k + 1}a.L"] = E(r=k_amounts[min(k, len(k_amounts) - 1)])
        if extra_b:
            d[f"wing_f{k + 1}b.L"] = extra_b(k)
    return d


SPAR_ANGS = [a for a, _ in WANG["L"]["spars"]]
ARM_ANG = WANG["L"]["arm"]
FOLD_FULL = [max(0.0, (a - (ARM_ANG + 9))) for a in SPAR_ANGS]   # degrees to swing each finger down to the arm
log("full fold per finger (deg):", [round(x) for x in FOLD_FULL])


def wings(fold, root_yaw=0.0, upper_roll=0.0, root_pitch=0.0, spread=0.0, curl=0.0):
    """fold 0 = rest spread, 1 = fan closed; spread > 0 opens beyond rest (stretch); root_yaw > 0 swings the wing
    forward around the body; curl > 0 bends the finger tips toward the midline (cloak wrap)."""
    d = {f"wing_root.L": E(p=root_pitch, y=-root_yaw), f"wing_upper.L": E(r=upper_roll)}
    for k in range(NSPAR):
        d[f"wing_f{k + 1}a.L"] = E(r=FOLD_FULL[k] * fold - spread * (1 + 0.5 * k), y=-0.35 * curl * (1 - 0.25 * k))
        if curl:
            d[f"wing_f{k + 1}b.L"] = E(y=-curl * (1 - 0.2 * k))
    return d


SLEEP_ARMS = {"upper_arm.L": E(r=44, y=-8), "forearm.L": E(p=-135, y=-45), "hand.L": E(p=-35),
              "thigh.L": E(r=13), "toes.L": E(p=18), "foot.L": E(p=6)}
P_SLEEP = both({**wings(0.28, root_yaw=84, upper_roll=26, curl=48), **SLEEP_ARMS,
                "neck": E(p=18, y=12), "head": E(p=22, y=20), "spine": E(p=4), "chest": E(p=6)})
P_WAKE = both({**wings(0.26, root_yaw=78, upper_roll=24, curl=40), **SLEEP_ARMS,
               "neck": E(p=-6), "head": E(p=-18), "spine": E(p=-2), "chest": E(p=0)})
P_STRETCH = both({**wings(0.0, root_yaw=-12, upper_roll=-12, spread=7),
                  "upper_arm.L": E(r=-18, y=-8), "forearm.L": E(p=-25), "hand.L": E(p=-15),
                  "toes.L": E(p=18), "foot.L": E(p=6),
                  "spine": E(p=-8), "chest": E(p=-10), "neck": E(p=4), "head": E(p=-6), "jaw": E(p=24)})
P_FALL = both({**wings(0.45, root_yaw=8, upper_roll=10),
               "upper_arm.L": E(r=-5, y=-15), "forearm.L": E(p=-35), "hand.L": E(p=-10),
               "thigh.L": E(p=-25), "shin.L": E(p=40), "foot.L": E(p=-20),
               "spine": E(p=6), "chest": E(p=4), "neck": E(p=-4), "head": E(p=-10)})
P_CROUCH = both({**wings(0.0, root_yaw=-6, upper_roll=-6, spread=3),
                 "hips": E(p=24), "spine": E(p=12), "chest": E(p=6), "neck": E(p=-22), "head": E(p=-26),
                 "thigh.L": E(p=-78, r=-6), "shin.L": E(p=112), "foot.L": E(p=-40), "toes.L": E(p=6),
                 "upper_arm.L": E(r=20, y=-30), "forearm.L": E(p=-20), "hand.L": E(p=10), "jaw": E(p=10)})
P_STANCE = both({**wings(0.28, root_yaw=14, upper_roll=6),
                 "hips": E(p=10), "spine": E(p=8), "chest": E(p=7), "neck": E(p=-10), "head": E(p=-14),
                 "thigh.L": E(p=-22, r=-4), "shin.L": E(p=38), "foot.L": E(p=-16),
                 "upper_arm.L": E(r=-6, y=-24), "forearm.L": E(p=-38, y=-10), "hand.L": E(p=-12), "jaw": E(p=6)})

T_WAKE, T_UNFURL, T_DROP, T_DOWN, T_RISE, T_END = 3.5, 4.5, 6.5, 7.55, 8.5, 10.0
SEQ_FRAMES = int(T_END * FPS)
IDLE_FRAMES = 60


def breath_stance(t):
    return math.sin(2 * math.pi * (t - T_END) / (IDLE_FRAMES / FPS))


def stance_pose(t, amt=1.0):
    b = breath_stance(t) * amt
    return addp(P_STANCE, {"chest": E(p=1.6 * b), "neck": E(p=-0.8 * b), "wing_upper.L": E(r=-1.5 * b), "wing_upper.R": E(r=1.5 * b)})


def seq_pose(t):
    """-> (pose, root quaternion, eyes 0..1, mode)  mode: hang | drop | ground"""
    eyes = smooth(T_WAKE + 0.05, T_WAKE + 0.12, t)
    if t < T_WAKE:
        b = math.sin(2 * math.pi * t / 3.2)
        pose = addp(P_SLEEP, {"chest": E(p=1.5 * b), "spine": E(p=0.8 * b)})
    elif t < T_UNFURL:
        pose = blend(P_SLEEP, P_WAKE, smooth(T_WAKE + 0.05, T_WAKE + 0.55, t))
        # the head comes round toward the camera after it lifts
        pose = addp(pose, {"head": E(y=8 * (1 - smooth(T_WAKE + 0.3, T_UNFURL - 0.1, t)) * smooth(T_WAKE, T_WAKE + 0.3, t))})
    elif t < T_DROP:
        u = smooth(T_UNFURL, T_UNFURL + 1.25, t)
        pose = blend(P_WAKE, P_STRETCH, u)
        hiss = smooth(T_UNFURL + 0.4, T_UNFURL + 0.75, t) * (1 - smooth(T_DROP - 0.5, T_DROP - 0.1, t))
        pose = addp(pose, {"jaw": E(p=14 * hiss), "head": E(p=6 * hiss)})
    elif t < T_DOWN:
        pose = blend(P_STRETCH, P_FALL, smooth(T_DROP, T_DROP + 0.45, t))
        pose = blend(pose, P_CROUCH, smooth(T_DOWN - 0.12, T_DOWN, t))
    elif t < T_RISE:
        pose = blend(P_FALL, P_CROUCH, smooth(T_DOWN - 0.12, T_DOWN, t))
        pose = blend(pose, stance_pose(t, 0.0), smooth(T_RISE - 0.15, T_RISE + 0.9, t))
    else:
        pose = blend(P_CROUCH, stance_pose(t, smooth(T_RISE + 0.6, T_END - 0.4, t)), smooth(T_RISE - 0.15, T_RISE + 0.9, t))
    # root: hanging upside down (half-roll about the facing axis), slight pendulum sway, then the half-roll back
    sway = math.sin(2 * math.pi * t / 4.0) * 2.5 * (1 - smooth(T_WAKE, T_UNFURL, t) * 0.6) * (1 - smooth(T_DROP - 0.3, T_DROP, t))
    roll = 180.0 * (1 - smooth(T_DROP + 0.1, T_DOWN - 0.2, t))
    rq = Quaternion(AY, math.radians(roll)) @ Quaternion(AX, math.radians(sway))
    mode = "hang" if t <= T_DROP else ("ground" if t >= T_DOWN else "drop")
    return pose, rq, eyes, mode


def set_pose(pose, rq, loc, eyes=1.0):
    for b in pb:
        b.rotation_quaternion = Quaternion()
        b.location = Vector()
        b.scale = Vector((1, 1, 1))
    for n, q in pose.items():
        if n in pb:
            pb[n].rotation_quaternion = REST[n].inverted() @ q @ REST[n]
    pb["root"].rotation_quaternion = REST["root"].inverted() @ rq @ REST["root"]
    pb["root"].location = REST["root"].inverted() @ Vector(loc)
    s = max(eyes, 0.0005)
    pb["eyes"].scale = Vector((s, s, s))
    bpy.context.view_layer.update()


if STAGE == "poses":
    os.makedirs(PREVIEW, exist_ok=True)
    dbg = [("sleepA", both(wings(0.28, root_yaw=84, upper_roll=26, curl=48))), ("sleepB", both(wings(0.4, root_yaw=95, upper_roll=30, curl=60)))]
    if os.environ.get("RW_DBG"):
        sets = dbg
    else:
        sets = [("sleep", P_SLEEP), ("wake", P_WAKE), ("stretch", P_STRETCH), ("fall", P_FALL), ("crouch", P_CROUCH), ("stance", P_STANCE)]
    for nm, P in sets:
        set_pose(P, Quaternion(), (0, 0, 0), 1.0)
        for bn in ("wing_root.L", "wing_upper.L", "wing_f1b.L", "wing_f3b.L", "upper_arm.L", "forearm.L", "hand.L"):
            log(f"POSE {nm} {bn}: head {fmt(pb[bn].head)} tail {fmt(pb[bn].tail)}")
        for vn, d in (("front", Vector((0, -1, 0.02))), ("side", Vector((1, 0, 0.02))), ("top", Vector((0.001, 0.0, 1)))):
            shoot(os.path.join(PREVIEW, f"pose_{nm}_{vn}.png"), Vector((0, 0.2, 1.1)), d, 3.4)
    log("STAGE poses done")
    sys.exit(0)

_gi = {g.index: g.name for g in rw.vertex_groups}
FOOT_VERTS = [v.index for v in me.vertices if v.groups and _gi[max(v.groups, key=lambda g: g.weight).group].startswith(("foot", "toes"))]
WING_EDGES = [(e.vertices[0], e.vertices[1]) for e in me.edges if WING[e.vertices[0]] > 0.8 and WING[e.vertices[1]] > 0.8]
REST_LEN = [(co[a] - co[b]).length for a, b in WING_EDGES]
log("foot verts", len(FOOT_VERTS), "wing edges", len(WING_EDGES))


def eval_co():
    dg = bpy.context.evaluated_depsgraph_get()
    ev = rw.evaluated_get(dg)
    mm = ev.to_mesh()
    mw = ev.matrix_world
    pts = [mw @ v.co for v in mm.vertices]
    ev.to_mesh_clear()
    return pts


def feet_top():
    pts = eval_co()
    return max(pts[i].z for i in FOOT_VERTS), pts


def feet_low():
    pts = eval_co()
    return min(pts[i].z for i in FOOT_VERTS), pts


def stretch_report(label, pts):
    rr = [((pts[a] - pts[b]).length / max(l, 1e-6), i) for i, ((a, b), l) in enumerate(zip(WING_EDGES, REST_LEN))]
    rr.sort()
    rs = [x for x, _ in rr]
    n = len(rs)
    bad = [i for x, i in rr if x > 2.0]
    if bad:
        cen = sum(((co[WING_EDGES[i][0]] + co[WING_EDGES[i][1]]) / 2 for i in bad), Vector()) / len(bad)
        log(f"  {len(bad)} wing edges ({100 * len(bad) / n:.2f}%) stretched > 2x at {label}, centred near rest point {fmt(cen)}; "
            f"worst at rest point {fmt((co[WING_EDGES[rr[-1][1]][0]] + co[WING_EDGES[rr[-1][1]][1]]) / 2)}")
    log(f"wing edge stretch at {label}: min {rs[0]:.2f} p1 {rs[n // 100]:.2f} median {rs[n // 2]:.2f} p99 {rs[(99 * n) // 100]:.2f} max {rs[-1]:.2f} (x rest length)")


def key_all(frame):
    for b in pb:
        b.keyframe_insert("rotation_quaternion", frame=frame)
        b.keyframe_insert("location", frame=frame)
    pb["eyes"].keyframe_insert("scale", frame=frame)


def new_action(name):
    if arm.animation_data is None:
        arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    return act


def solve_ground(pose, rq, eyes, xy):
    set_pose(pose, rq, (xy[0], xy[1], 0.0), eyes)
    low, _ = feet_low()
    loc = Vector((xy[0], xy[1], -low))
    set_pose(pose, rq, loc, eyes)
    return loc


def solve_hang(pose, rq, eyes):
    set_pose(pose, rq, (0.0, 0.0, CEIL), eyes)
    top, _ = feet_top()
    loc = Vector((0.0, 0.0, CEIL + (CEIL - top)))
    set_pose(pose, rq, loc, eyes)
    return loc


def hips_world():
    return arm.matrix_world @ pb["hips"].head


# touchdown target: hips position of the ground solution at T_DOWN
p_, rq_, e_, _ = seq_pose(T_DOWN)
solve_ground(p_, rq_, e_, (0.0, LAND_Z))
P_LAND = hips_world()
p_, rq_, e_, _ = seq_pose(T_DROP)
solve_hang(p_, rq_, e_)
P_RELEASE = hips_world()
log(f"release hips {fmt(P_RELEASE)} -> touchdown hips {fmt(P_LAND)}")

clips = {}
act = new_action("sequence")
hang_gap, land_gap, hang_low = [], [], []
for f in range(SEQ_FRAMES + 1):
    t = f / FPS
    pose, rq, eyes, mode = seq_pose(t)
    if mode == "hang":
        solve_hang(pose, rq, eyes)
        top, pts = feet_top()
        hang_gap.append(CEIL - top)
        hang_low.append(min(p.z for p in pts))
    elif mode == "ground":
        solve_ground(pose, rq, eyes, (0.0, LAND_Z))
        low, pts = feet_low()
        land_gap.append(low)
    else:
        u = (t - T_DROP) / (T_DOWN - T_DROP)
        target = Vector((lerp(P_RELEASE.x, P_LAND.x, smooth(0, 1, u)), lerp(P_RELEASE.y, P_LAND.y, smooth(0, 1, u)),
                         lerp(P_RELEASE.z, P_LAND.z, u ** 1.7)))
        set_pose(pose, rq, (0, 0, 0), eyes)
        loc = target - hips_world()
        set_pose(pose, rq, loc, eyes)
    key_all(f)
    for label, tt in (("asleep (wings wrapped)", 2.0), ("full spread (stretch)", 5.8), ("landing crouch", 8.2), ("final stance", 10.0)):
        if f == int(tt * FPS):
            stretch_report(label, eval_co())
clips["sequence"] = (act, SEQ_FRAMES)
log(f"hang: feet top vs ceiling gap min {min(hang_gap) * 1000:.1f} max {max(hang_gap) * 1000:.1f} mm; lowest point while hanging "
    f"{min(hang_low):.2f} m above the floor")
log(f"landed: lowest foot point min {min(land_gap) * 1000:.1f} max {max(land_gap) * 1000:.1f} mm above the floor")

act = new_action("idle")
idle_gap = []
for f in range(IDLE_FRAMES + 1):
    t = T_END + (f % IDLE_FRAMES) / FPS
    pose = stance_pose(t)
    solve_ground(pose, Quaternion(), 1.0, (0.0, LAND_Z))
    idle_gap.append(feet_low()[0])
    key_all(f)
clips["idle"] = (act, IDLE_FRAMES)
log(f"idle: lowest foot point {min(idle_gap) * 1000:.1f}..{max(idle_gap) * 1000:.1f} mm")

arm.animation_data.action = None
for name, (act, frames) in clips.items():
    tr = arm.animation_data.nla_tracks.new()
    tr.name = name
    tr.strips.new(name, 0, act)
    tr.mute = True
scn = bpy.context.scene
scn.render.fps = FPS
scn.frame_start = 0
scn.frame_end = SEQ_FRAMES

# ------------------------------------------------------------------ export
set_pose({}, Quaternion(), (0, 0, 0), 1.0)
for o in bpy.data.objects:
    o.select_set(o in (arm, rw, eyes_obj))
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(
    filepath=FBX, use_selection=True, object_types={"MESH", "ARMATURE"},
    apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
    use_mesh_modifiers=False, mesh_smooth_type="FACE", add_leaf_bones=False,
    bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True,
    bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0, path_mode="COPY", embed_textures=False)
log("exported", FBX, "tris", tris(rw), "+ eyes", tris(eyes_obj), "bones", len(arm_data.bones),
    f"CEIL {CEIL} m, LAND_Z {LAND_Z} m, height {HEIGHT} m")
with open(METRICS, "w") as f:
    f.write("\n".join(_metrics) + "\n")

# ------------------------------------------------------------------ previews
if PREVIEW:
    os.makedirs(PREVIEW, exist_ok=True)
    for nm, z, sz in (("ceiling", CEIL, 3.0), ("floor", 0.0, 6.0)):
        pm = bpy.data.meshes.new(nm)
        pm.from_pydata([(-sz, -sz, z), (sz, -sz, z), (sz, sz, z), (-sz, sz, z)], [], [(0, 1, 2, 3)])
        po = bpy.data.objects.new(nm, pm)
        scn.collection.objects.link(po)
    arm.animation_data.action = clips["sequence"][0]
    for tt in (0.0, 1.5, 3.9, 5.2, 5.8, 6.8, 7.1, 7.4, 7.6, 8.2, 9.0, 10.0):
        scn.frame_set(int(tt * FPS))
        for vn, d in (("front", Vector((0.0, -1, 0.05))), ("side", Vector((1, -0.15, 0.05)))):
            shoot(os.path.join(PREVIEW, f"rw_{int(tt * 10):03d}_{vn}.png"), Vector((0, LAND_Z / 2, 2.25)), d, 5.0)
    for tt in (3.0, 4.2, 5.6):
        scn.frame_set(int(tt * FPS))
        bpy.context.view_layer.update()
        hc = arm.matrix_world @ pb["head"].head
        shoot(os.path.join(PREVIEW, f"rw_head_{int(tt * 10):03d}.png"), hc, Vector((0.25, -1, 0.0)), 0.9)
    log("previews in", PREVIEW)
