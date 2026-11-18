"""Clean up, rig and animate the black cat (Round 3) for Unity.

Run headless from the repo root:
  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/cleanup_cat.py -- [repo_root] [target_tris] [preview_dir]

Input : Art/Source/cat/cat_v1.glb    (Tripo, unrigged, ~2M tris, 0.98 units tall, faces -Y; never modified)
Output: Art/Export/cat/cat_v1_rigged.fbx              (mesh + armature + clips, one take per clip)
        Art/Export/cat/Textures/cat_v1_basecolor.jpg   (grayscale, 2048)
        [preview_dir]/cat_*.png                        (contact sheet of every clip, if a preview dir is given)

Steps: weld seam duplicates -> straighten the head (the source head is turned toward the viewer) -> keep the whiskers
out of the decimation (procedural whisker strips if the source has no separate whisker parts) -> decimate the body to
~target -> scale so the back/shoulder is SHOULDER_HEIGHT -> origin between the feet on the floor, facing -Y ->
a simple quadruped deform rig fitted to the mesh by script (not Rigify: Rigify's cat metarig would still need every
bone placed by hand; this rig is generated from the mesh instead) -> skin weights -> keyframed clips:
idle, walk, sit, wash, jump, sleep.

Round 8 rig: joints sit at the centre of the mesh cross-sections (legs traced up from the paws slab by slab; elbow /
stifle / hock at the leg's own bends; spine / neck / head from torso and skull sections; tail recentred on its
cross-section each step). Weights = Blender bone heat (solved at 10x scale), side / front-back / tail guards,
4 Laplacian smoothing passes over the mesh edges, max 4 influences, normalized. Same 24 bone names as Round 3, plus earL / earR (Round 10).
Env: CAT_FBX_OUT=<path> writes the FBX elsewhere (experiments), CAT_DEBUG=1 logs posed joint positions.
"""
import bpy, bmesh, os, sys, math
from mathutils import Vector, Quaternion, Matrix

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = os.path.abspath(argv[0]) if len(argv) > 0 else os.getcwd()
TARGET_TRIS = int(argv[1]) if len(argv) > 1 else 18000
PREVIEW = os.path.abspath(argv[2]) if len(argv) > 2 else None
SHOULDER_HEIGHT = 0.25          # real house cat (Unity scales the roaming cat up)

SRC = os.path.join(ROOT, "Art", "Source", "cat", "cat_v1.glb")
OUT_DIR = os.path.join(ROOT, "Art", "Export", "cat")
TEX_DIR = os.path.join(OUT_DIR, "Textures")
FBX = os.environ.get("CAT_FBX_OUT") or os.path.join(OUT_DIR, "cat_v1_rigged.fbx")   # env override = experiments
TEX = os.path.join(TEX_DIR, "cat_v1_basecolor.jpg")
os.makedirs(TEX_DIR, exist_ok=True)
FPS = 30


def log(*a):
    print("[cleanup_cat]", *a, flush=True)


def tris(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def smooth(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SRC)
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
for o in bpy.data.objects:
    o.select_set(o.type == "MESH")
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1:
    bpy.ops.object.join()
cat = bpy.context.view_layer.objects.active
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for o in list(bpy.data.objects):
    if o != cat:
        bpy.data.objects.remove(o)
cat.name = "cat_body"
log("source", len(cat.data.vertices), "verts", tris(cat), "tris")

# ------------------------------------------------------------------ weld + islands (whiskers)
bm = bmesh.new()
bm.from_mesh(cat.data)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
bm.verts.ensure_lookup_table()
# Islands
seen = set()
islands = []
for v in bm.verts:
    if v.index in seen:
        continue
    stack = [v]
    seen.add(v.index)
    isl = []
    while stack:
        x = stack.pop()
        isl.append(x.index)
        for e in x.link_edges:
            y = e.other_vert(x)
            if y.index not in seen:
                seen.add(y.index)
                stack.append(y)
    islands.append(isl)
islands.sort(key=len, reverse=True)
log("islands", len(islands), "sizes", [len(i) for i in islands[:12]])
whisker_islands = []
for isl in islands[1:]:
    cs = [bm.verts[i].co for i in isl]
    mn = Vector((min(c.x for c in cs), min(c.y for c in cs), min(c.z for c in cs)))
    mx = Vector((max(c.x for c in cs), max(c.y for c in cs), max(c.z for c in cs)))
    size = mx - mn
    thin = sorted(size)[0] < 0.006 and max(size) > 0.03
    if mn.y < -0.36 and mn.z > 0.5 and thin and len(isl) < 20000:
        whisker_islands.append(isl)
log("whisker islands found:", len(whisker_islands))
bm.to_mesh(cat.data)
bm.free()

# ------------------------------------------------------------------ square the body to -Y
# The source stands in a 3/4 pose: the line from the hind feet to the front feet is turned off the Y axis.
# Cluster the low (paw/leg) vertices into four legs, then rotate the whole cat so front and hind pairs line up on Y.
me = cat.data
low = [v.co.copy() for v in me.vertices if v.co.z < 0.10]
cen = sum(low, Vector()) / len(low)
cent = [cen + Vector((sx * 0.08, sy * 0.2, 0)) for sx in (-1, 1) for sy in (-1, 1)]
for it in range(12):
    groups = [[] for _ in cent]
    for c in low:
        k = min(range(4), key=lambda i: (c.xy - cent[i].xy).length_squared)
        groups[k].append(c)
    cent = [sum(g, Vector()) / len(g) if g else cent[i] for i, g in enumerate(groups)]
front = sorted(cent, key=lambda c: c.y)[:2]
hind = sorted(cent, key=lambda c: c.y)[2:]
fc_, hc_ = (front[0] + front[1]) / 2, (hind[0] + hind[1]) / 2
body_yaw = math.atan2(fc_.x - hc_.x, -(fc_.y - hc_.y))
log(f"leg clusters front {tuple(round(x, 3) for x in fc_)} hind {tuple(round(x, 3) for x in hc_)} -> body yaw {math.degrees(body_yaw):.1f} deg")
mid_ = (fc_ + hc_) / 2
Rb = Matrix.Rotation(-body_yaw, 3, "Z")
for v in me.vertices:
    d = v.co - Vector((mid_.x, mid_.y, 0))
    v.co = Rb @ d
me.update()

# ------------------------------------------------------------------ straighten the head
co = [v.co.copy() for v in me.vertices]
head_pts = [c for c in co if c.y < -0.40 and c.z > 0.55]
neck_pts = [c for c in co if -0.30 < c.y < -0.24 and c.z > 0.52]
hc = sum(head_pts, Vector()) / len(head_pts)
nc = sum(neck_pts, Vector()) / len(neck_pts)
yaw = math.atan2(hc.x - nc.x, -(hc.y - nc.y))     # + = head turned toward +x
log(f"head centroid {tuple(round(x, 3) for x in hc)} neck {tuple(round(x, 3) for x in nc)} yaw {math.degrees(yaw):.1f} deg")
pivot = Vector((nc.x, nc.y, nc.z))
if abs(math.degrees(yaw)) > 4:
    for v in me.vertices:
        c = v.co
        w = smooth(-0.24, -0.36, c.y) * smooth(0.44, 0.54, c.z)
        if w <= 0:
            continue
        a = -yaw * w          # rotate back toward -Y
        R = Matrix.Rotation(a, 3, "Z")
        v.co = pivot + R @ (c - pivot)
    co = [v.co.copy() for v in me.vertices]
    head_pts = [c for c in co if c.y < -0.40 and c.z > 0.55]
    hc2 = sum(head_pts, Vector()) / len(head_pts)
    log(f"head straightened: centroid x {hc.x:.3f} -> {hc2.x:.3f}")

# ------------------------------------------------------------------ split whiskers, decimate body
bpy.ops.object.select_all(action="DESELECT")
cat.select_set(True)
bpy.context.view_layer.objects.active = cat
whisk_obj = None
if whisker_islands:
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="DESELECT")
    bpy.ops.object.mode_set(mode="OBJECT")
    ids = set(i for isl in whisker_islands for i in isl)
    for v in cat.data.vertices:
        v.select = v.index in ids
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.separate(type="SELECTED")
    bpy.ops.object.mode_set(mode="OBJECT")
    whisk_obj = [o for o in bpy.context.selected_objects if o != cat][0]
    whisk_obj.name = "cat_whiskers"

n0 = tris(cat)
mod = cat.modifiers.new("dec", "DECIMATE")
mod.ratio = min(1.0, TARGET_TRIS / n0)
bpy.ops.object.select_all(action="DESELECT")
cat.select_set(True)
bpy.context.view_layer.objects.active = cat
bpy.ops.object.modifier_apply(modifier="dec")
log("decimated", n0, "->", tris(cat))

if whisk_obj is not None and tris(whisk_obj) > 3000:
    m2 = whisk_obj.modifiers.new("dec", "DECIMATE")
    m2.ratio = 3000 / tris(whisk_obj)
    bpy.context.view_layer.objects.active = whisk_obj
    bpy.ops.object.modifier_apply(modifier="dec")

# ------------------------------------------------------------------ scale + origin
co = [cat.matrix_world @ v.co for v in cat.data.vertices]
back = [c.z for c in co if -0.05 < c.y < 0.05]
back_h = max(back)
feet = [c for c in co if c.z < 0.03]
fc = sum(feet, Vector()) / len(feet)
s = SHOULDER_HEIGHT / back_h
log(f"back height {back_h:.3f} src units -> scale {s:.4f}; feet centre {tuple(round(x, 3) for x in fc)}")
for o in [cat] + ([whisk_obj] if whisk_obj else []):
    for v in o.data.vertices:
        p = v.co
        v.co = Vector(((p.x - fc.x) * s, (p.y - fc.y) * s, p.z * s))
    o.data.update()

# Material: one grayscale texture, 2048.
mat = cat.material_slots[0].material
img = None
for n in mat.node_tree.nodes:
    if n.type == "TEX_IMAGE" and n.image is not None:
        img = n.image
        break
mat.name = "cat_fur"
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

# Whiskers: procedural if the source has none as separate parts.
co = [v.co.copy() for v in cat.data.vertices]
H = SHOULDER_HEIGHT
if whisk_obj is None:
    head = [c for c in co if c.y < -0.95 * H * 1.55 and c.z > 0.9 * H]   # muzzle region
    tip_y = min(c.y for c in co)
    muzzle = [c for c in co if c.y < tip_y + 0.035 and c.z > 0.85 * H]
    mz = sum(muzzle, Vector()) / max(1, len(muzzle))
    wv, wf = [], []
    for side in (-1, 1):
        for k in range(5):
            base = Vector((side * 0.012, mz.y + 0.012, mz.z - 0.006 + (k - 2) * 0.003))
            d = Vector((side * 1.0, 0.35, (k - 2) * 0.12 - 0.05)).normalized()
            L = 0.055 + 0.01 * (2 - abs(k - 2))
            up = Vector((0, 0, 1))
            w = d.cross(up).normalized() * 0.0007
            i = len(wv)
            wv += [base - w, base + w, base + d * L + Vector((0, 0, -0.008))]
            wf.append((i, i + 1, i + 2))
    wm = bpy.data.meshes.new("cat_whiskers")
    wm.from_pydata([tuple(v) for v in wv], [], wf)
    whisk_obj = bpy.data.objects.new("cat_whiskers", wm)
    bpy.context.scene.collection.objects.link(whisk_obj)
    log("procedural whiskers at muzzle", tuple(round(x, 3) for x in mz))
wmat = bpy.data.materials.new("cat_whisker")
whisk_obj.data.materials.clear()
whisk_obj.data.materials.append(wmat)

LEGS = ("FL", "FR", "HL", "HR")

# ------------------------------------------------------------------ fit the rig from mesh cross-sections
# Round 8: every joint is placed at the centre of the mesh cross-section around it (slab centroids / mean-shift),
# instead of fixed fractions of the shoulder height, so the bones run through the middle of legs, body, neck and tail.
def band(pred):
    pts = [c for c in co if pred(c)]
    return (sum(pts, Vector()) / len(pts), len(pts)) if pts else (None, 0)


def mid_extent(pts, axis):
    vals = [getattr(c, axis) for c in pts]
    return (min(vals) + max(vals)) / 2 if vals else None


ymin = min(c.y for c in co); ymax = max(c.y for c in co)


def trace_leg(sx, front, z_end=0.125):
    """Walk up one leg from the paw: a slab every 4 mm, centre = middle of the extents of the slab verts within r of
    the previous centre (mean-shift in xy). The centre may drift at most 3 mm per step, so where the leg opens into
    the chest / belly it keeps the leg's own line instead of sliding onto the body."""
    def col(c):
        return c.x * sx > 0.0 and ((c.y < 0) == front)
    base = [c for c in co if c.z < 0.025 * H / 0.25 and col(c)]
    p = sum(base, Vector()) / len(base)
    p = Vector((p.x, p.y, 0.0))
    line = []
    r = 0.024 * H / 0.25
    z = 0.006
    while z < z_end * H / 0.25:
        q0 = Vector((p.x, p.y))
        q = q0.copy()
        near = []
        for _ in range(4):
            near = [c for c in co if abs(c.z - z) < 0.004 and col(c) and (c.xy - q).length < r]
            if len(near) < 4:
                break
            q = Vector((mid_extent(near, "x"), mid_extent(near, "y")))
        if len(near) < 4:
            break
        dq = q - q0
        if dq.length > 0.003:
            q = q0 + dq.normalized() * 0.003
        p = Vector((q.x, q.y, z))
        line.append((p.copy(), 0))
        z += 0.004
    return line


legs = {}
for name, sx, front in (("FL", 1, True), ("FR", -1, True), ("HL", 1, False), ("HR", -1, False)):
    line = trace_leg(sx, front)
    pts_ = [p for p, _ in line]

    def at_z(zq):
        best = min(pts_, key=lambda p: abs(p.z - zq))
        return best.copy()

    def extreme(z0, z1, key):
        cand = [p for p in pts_ if z0 * H <= p.z <= z1 * H]
        return (min(cand, key=key) if cand else at_z((z0 + z1) / 2 * H)).copy()

    toe_band, _ = band(lambda c: c.z < 0.02 * H / 0.25 and c.x * sx > 0.0 and ((c.y < 0) == front))
    toe = Vector((toe_band.x, min(c.y for c in co if c.z < 0.02 and c.x * sx > 0.0 and ((c.y < 0) == front)) + 0.01 * H / 0.25
                  if front else toe_band.y - 0.012 * H / 0.25, 0.005))
    if front:
        ankle = at_z(0.12 * H)                                   # wrist
        knee = extreme(0.32, 0.50, key=lambda p: -p.y)           # elbow: the leg's rearmost point under the chest
        top = Vector((knee.x * 0.8, knee.y - 0.008 * H / 0.25, 0.70 * H))   # shoulder: a little ahead of the elbow
    else:
        ankle = extreme(0.16, 0.34, key=lambda p: -p.y)          # hock: rearmost point of the lower hind leg
        knee = extreme(0.38, 0.50, key=lambda p: p.y)            # stifle: foremost point of the thigh
        top = Vector((knee.x * 0.8, ankle.y - 0.004 * H / 0.25, 0.66 * H))  # hip: about above the hock
        if knee.y > top.y - 0.015 * H / 0.25:
            knee.y = top.y - 0.015 * H / 0.25                    # the stifle always points forward
    legs[name] = {"toe": toe, "ankle": ankle, "knee": knee, "top": top, "line_top": pts_[-1].z if pts_ else 0}
    log(name, "trace", len(line), "pts up to z", round(legs[name]["line_top"], 3),
        {k: tuple(round(x, 3) for x in v) for k, v in legs[name].items() if k != "line_top"})

# Mirror-average the shoulder / hip joints left-right (the lower joints follow each leg's own stance).
for a_, b_ in (("FL", "FR"), ("HL", "HR")):
    for k in ("top",):
        pa, pb_ = legs[a_][k], legs[b_][k]
        x = (abs(pa.x) + abs(pb_.x)) / 2
        y = (pa.y + pb_.y) / 2
        z = (pa.z + pb_.z) / 2
        legs[a_][k] = Vector((x, y, z))
        legs[b_][k] = Vector((-x, y, z))


# Body line
def body_y(front):
    return sum(legs[n]["top"].y for n in (("FL", "FR") if front else ("HL", "HR"))) / 2


yF, yH = body_y(True), body_y(False)


def body_centre(y0, half=0.012):
    """Centre of the torso cross-section at y0 (above the legs): mid of the top/bottom extents of |x| < 3 cm."""
    # |x| < 1.5 cm keeps the legs out; behind the hips the tail rises above the back, so cap z there.
    zcap = 0.98 * H if y0 > 0 else 1.2 * H
    sl = [c for c in co if abs(c.y - y0) < half and abs(c.x) < 0.015 * H / 0.25 and 0.36 * H < c.z < zcap]
    if len(sl) < 6:
        return None
    top = max(c.z for c in sl)
    bot = min(c.z for c in sl)
    return top, bot


def spine_point(y0):
    tb = body_centre(y0)
    if tb is None:
        return Vector((0, y0, 0.74 * H))
    top, bot = tb
    log(f"torso section y {y0:.3f}: top {top:.3f} bottom {bot:.3f}")
    return Vector((0, y0, bot + 0.58 * (top - bot)))       # spine sits a little above the section's middle


sp_h = spine_point(yH)
sp_m = spine_point((yH + yF) / 2)
sp_f = spine_point(yF)
spine_z = sp_m.z
log("spine points", [tuple(round(x, 3) for x in p) for p in (sp_h, sp_m, sp_f)])
# Hip / shoulder joints just below the spine line at the leg tops.
for n in ("FL", "FR", "HL", "HR"):
    ref = sp_f if n[0] == "F" else sp_h
    legs[n]["top"].z = min(legs[n]["top"].z, ref.z - 0.03 * H / 0.25)
tail_pts = []
# Tail: march from the base (behind the hind legs, just above the back line) upward along the tail's vertices.
tail_v = [c for c in co if c.y > yH - 0.01 and c.z > 0.8 * H]
base_pts = [c for c in tail_v if c.z < 0.92 * H]
upper = [c for c in tail_v if 0.98 * H < c.z < 1.1 * H]
p = sum(base_pts, Vector()) / len(base_pts)
u = sum(upper, Vector()) / len(upper)
dirv = (u - p).normalized()
step = 0.02
tail_pts.append(p.copy())
for i in range(60):
    q = p + dirv * step
    near = [c for c in tail_v if (c - q).length < 0.018]
    if len(near) < 6:
        break
    # Centre of the tail's cross-section at q (verts in a thin slab across the march direction).
    ring = [c for c in near if abs((c - q).dot(dirv)) < 0.005]
    q2 = sum(ring, Vector()) / len(ring) if len(ring) >= 4 else sum(near, Vector()) / len(near)
    nd = (q2 - p)
    if nd.length < 1e-4:
        break
    nd.normalize()
    if nd.dot(dirv) < 0.2:
        break
    dirv = (dirv * 0.5 + nd * 0.5).normalized()
    p = p + dirv * step
    ring = [c for c in tail_v if (c - p).length < 0.018 and abs((c - p).dot(dirv)) < 0.005]
    if len(ring) >= 4:
        p = sum(ring, Vector()) / len(ring)
    tail_pts.append(p.copy())
log("tail points", len(tail_pts), "tip", tuple(round(x, 3) for x in tail_pts[-1]))

tip_y = min(c.y for c in co)
muzzle, _ = band(lambda c: c.y < tip_y + 0.02 and c.z > 0.8 * H)
skull, _ = band(lambda c: c.y < tip_y + 0.09 and c.z > 0.95 * H)

def shift(guess, r, it=8, pred=None):
    """Mean-shift: centroid of the verts within r of the guess (repeated)."""
    p = guess.copy()
    for _ in range(it):
        near = [c for c in co if (c - p).length < r and (pred is None or pred(c))]
        if len(near) < 5:
            break
        p = sum(near, Vector()) / len(near)
    return p


def yz_centre(y0, zmin, half=0.006):
    """Middle of the top/bottom extent of the |x| < 2 cm slab at y0, above zmin."""
    sl = [c for c in co if abs(c.y - y0) < half and abs(c.x) < 0.02 and c.z > zmin]
    if len(sl) < 4:
        return None
    return (max(c.z for c in sl) + min(c.z for c in sl)) / 2


head_c = shift(Vector((0, skull.y + 0.01, skull.z)), 0.032, pred=lambda c: c.z > 0.8 * H)
head_c.x = 0.0
head_base = Vector((0, head_c.y + 0.022 * H / 0.25, head_c.z - 0.016 * H / 0.25))   # atlas: behind/below the skull centre
neck_base = Vector((0, yF - 0.035 * H / 0.25, sp_f.z + 0.015 * H / 0.25))           # front of the chest, base of the neck
log("head centre", tuple(round(x, 3) for x in head_c), "atlas", tuple(round(x, 3) for x in head_base),
    "neck base", tuple(round(x, 3) for x in neck_base))

# Round 10: one bone per ear (the galley scene turns an ear toward 7). The head sits slightly turned, so the ears are
# found as the two highest separate peaks above the skull (not by the x side): tip = peak, base = 2.2 cm lower (scaled
# with H), pulled a quarter of the way toward the skull centre. earL = the peak with the larger x.
EARS = {}
_hc = [c for c in co if c.y < head_c.y + 0.06 * H / 0.25 and c.z > head_c.z]
if _hc:
    t1 = max(_hc, key=lambda c: c.z)
    _far = [c for c in _hc if (c.xy - t1.xy).length > 0.03 * H / 0.25]
    t2 = max(_far, key=lambda c: c.z) if _far else None
    if t2 is not None and t2.z > head_c.z + 0.01:
        for nm, tip in zip(("earL", "earR"), sorted((t1, t2), key=lambda c: -c.x)):
            base = tip.lerp(Vector((head_c.x, head_c.y, tip.z)), 0.25) - Vector((0, 0, 0.022 * H / 0.25))
            EARS[nm] = (base, tip.copy(), 0)
            log(f"ear {nm}: base {tuple(round(x, 3) for x in base)} tip {tuple(round(x, 3) for x in tip)}")
if not EARS:
    log("ears not found: no ear bones")

arm_data = bpy.data.armatures.new("CatRig")
arm = bpy.data.objects.new("CatRig", arm_data)
bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="EDIT")
eb = arm_data.edit_bones


def bone(name, h, t, parent=None, roll_to=Vector((0, 0, 1)), connect=False):
    b = eb.new(name)
    b.head = h
    b.tail = t
    b.align_roll(roll_to)
    if parent:
        b.parent = eb[parent]
        b.use_connect = connect
    return b


bone("root", Vector((0, 0, 0)), Vector((0, -0.08 * H, 0)))
hips_h = sp_h.copy()
mid = sp_m.copy()
bone("pelvis", hips_h, mid, "root")
bone("spine", mid, sp_f, "pelvis", connect=True)
bone("chest", sp_f, neck_base, "spine", connect=True)
bone("neck", neck_base, head_base, "chest", connect=True)
bone("head", head_base, Vector((0, muzzle.y + 0.004, muzzle.z + 0.004)), "neck", connect=True)
for nm, (eb_, et_, sx_) in EARS.items():
    bone(nm, eb_, et_, "head", roll_to=Vector((0, -1, 0)))
prev = "pelvis"
# Resample the tail polyline (rump -> tip) to 6 equal bones.
poly = [hips_h + Vector((0, 0.02, 0.0))] + tail_pts
seglen = [(poly[i + 1] - poly[i]).length for i in range(len(poly) - 1)]
total = sum(seglen)
tail_chain = []
for k in range(7):
    d = total * k / 6
    i = 0
    while i < len(seglen) - 1 and d > seglen[i]:
        d -= seglen[i]
        i += 1
    tail_chain.append(poly[i].lerp(poly[i + 1], min(1.0, d / max(seglen[i], 1e-6))))
log("tail length", round(total, 3))
for i in range(6):
    bone(f"tail{i + 1}", tail_chain[i], tail_chain[i + 1], prev, connect=(i > 0))
    prev = f"tail{i + 1}"
for name, pts in legs.items():
    front = name[0] == "F"
    sx = 1 if name[1] == "L" else -1
    top, knee, ankle, toe = pts["top"], pts["knee"], pts["ankle"], pts["toe"]
    par = "chest" if front else "pelvis"
    bone(f"{name}_upper", top, knee, par, roll_to=Vector((0, 1, 0)))
    bone(f"{name}_lower", knee, ankle, f"{name}_upper", roll_to=Vector((0, 1, 0)), connect=True)
    bone(f"{name}_foot", ankle, toe, f"{name}_lower", roll_to=Vector((0, 0, 1)), connect=True)
bpy.ops.object.mode_set(mode="OBJECT")

# ------------------------------------------------------------------ skin weights
bones = {b.name: (b.head_local.copy(), b.tail_local.copy()) for b in arm_data.bones if b.name != "root"}


def seg_dist(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-9)))
    return (p - (a + ab * t)).length


def weights_for(obj, head_only=False):
    obj.vertex_groups.clear()
    groups = {n: obj.vertex_groups.new(name=n) for n in bones}
    for v in obj.data.vertices:
        p = v.co
        cand = []
        for n, (a, b) in bones.items():
            if head_only and n not in ("head",):
                continue
            leg = n[:2] in ("FL", "FR", "HL", "HR")
            if leg:
                sx = 1 if n[1] == "L" else -1
                front = n[0] == "F"
                if p.x * sx < -0.004 or ((p.y < (yH + yF) / 2) != front):
                    continue
                if p.z > 0.72 * H:
                    continue
            if n.startswith("tail") and p.y < yH - 0.01:
                continue
            if n in ("head", "neck") and p.y > yF + 0.03:
                continue
            d = seg_dist(p, a, b)
            # Legs are thin: below the belly the nearest leg wins strongly.
            if leg and p.z < 0.5 * H:
                d *= 0.6
            cand.append((d, n))
        cand.sort()
        cand = cand[:3]
        ws = [(1.0 / (d * d + 1e-6) ** 2, n) for d, n in cand]
        tot = sum(w for w, _ in ws)
        for w, n in ws:
            if w / tot > 0.02:
                groups[n].add([v.index], w / tot, "REPLACE")
    m = obj.modifiers.new("Armature", "ARMATURE")
    m.object = arm
    obj.parent = arm


def influence_stats(obj):
    n = [sum(1 for g in v.groups if g.weight > 1e-4) for v in obj.data.vertices]
    return max(n), sum(1 for x in n if x == 0)


def smooth_limit_normalize(obj, iters=4, factor=0.5, limit=4, clean=0.01):
    """Laplacian smoothing of all weights over the mesh edges, then drop < clean, keep the 4 largest, normalize."""
    me_ = obj.data
    gname_ = {g.index: g.name for g in obj.vertex_groups}
    W = [{gname_[g.group]: g.weight for g in v.groups if g.weight > 0} for v in me_.vertices]
    nb = [[] for _ in me_.vertices]
    for e in me_.edges:
        a, b = e.vertices
        nb[a].append(b)
        nb[b].append(a)
    for _ in range(iters):
        W2 = []
        for i, w in enumerate(W):
            if not nb[i]:
                W2.append(w)
                continue
            acc = {}
            for j in nb[i]:
                for k, x in W[j].items():
                    acc[k] = acc.get(k, 0.0) + x
            inv = 1.0 / len(nb[i])
            keys = set(w) | set(acc)
            W2.append({k: (1 - factor) * w.get(k, 0.0) + factor * acc.get(k, 0.0) * inv for k in keys})
        W = W2
    groups = {g.name: g for g in obj.vertex_groups}
    for v in me_.vertices:
        for g in list(v.groups):
            groups[gname_[g.group]].remove([v.index])
    for i, w in enumerate(W):
        tot0 = sum(w.values())
        if tot0 <= 0:
            continue
        items = sorted(((x / tot0, k) for k, x in w.items()), reverse=True)
        items = [(x, k) for x, k in items if x >= clean][:limit] or items[:1]
        tot = sum(x for x, _ in items)
        for x, k in items:
            groups[k].add([i], x / tot, "REPLACE")


def heat_weights(obj):
    """Blender bone heat (automatic weights), then guards, smoothing over the mesh, max 4 influences, normalized.
    Verts the heat solver leaves empty fall back to the distance weights."""
    arm_data.bones["root"].use_deform = False
    # Bone heat is unreliable at real cat size (0.25 m); solve at 10x, then put the scale back.
    for o in (obj, arm):
        o.scale = (10, 10, 10)
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    obj.vertex_groups.clear()
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    obj.parent = None
    for o in (obj, arm):
        o.scale = (1, 1, 1)
    for m in list(obj.modifiers):
        if m.type == "ARMATURE":
            obj.modifiers.remove(m)
    bpy.context.view_layer.update()
    heat = {g.name: g for g in obj.vertex_groups}
    for n in bones:
        if n not in heat:
            heat[n] = obj.vertex_groups.new(name=n)
    gname = {g.index: g.name for g in obj.vertex_groups}
    empty = [v.index for v in obj.data.vertices if not any(g.weight > 1e-4 for g in v.groups)]
    log(f"bone heat: {len(obj.vertex_groups)} groups, {len(empty)} verts without weight -> distance fallback")
    if empty:
        tmp = bpy.data.objects.new("tmp_w", obj.data.copy())
        weights_for(tmp)
        tg = {g.index: g.name for g in tmp.vertex_groups}
        for i in empty:
            for g in tmp.data.vertices[i].groups:
                heat[tg[g.group]].add([i], g.weight, "REPLACE")
        md = tmp.data
        bpy.data.objects.remove(tmp)
        bpy.data.meshes.remove(md)
    # Guards: a leg never moves the other side or the other end of the body; the tail never moves the belly/flanks;
    # the upper legs fade out over the back and flanks (no dragged belly/back when a leg swings).
    yMid = (yH + yF) / 2
    for v in obj.data.vertices:
        p = v.co
        for g in list(v.groups):
            n = gname.get(g.group)
            if n is None or n == "root":
                continue
            w = g.weight
            if n[:2] in LEGS:
                sx = 1 if n[1] == "L" else -1
                w *= smooth(-0.004, 0.006, p.x * sx)
                w *= smooth(-0.03, 0.0, (yMid - p.y) if n[0] == "F" else (p.y - yMid))
                if n.endswith("_upper"):
                    tz = legs[n[:2]]["top"].z
                    w *= 1.0 - smooth(tz + 0.0, tz + 0.04, p.z)
            elif n.startswith("tail"):
                w *= smooth(yH - 0.02, yH + 0.005, p.y) if n == "tail1" else smooth(yH - 0.005, yH + 0.02, p.y)
            if w != g.weight:
                g.weight = w
    if "root" in heat:
        obj.vertex_groups.remove(heat["root"])
    smooth_limit_normalize(obj, iters=4, factor=0.5, limit=4)
    for n in bones:
        if n not in obj.vertex_groups:
            obj.vertex_groups.new(name=n)
    m = obj.modifiers.new("Armature", "ARMATURE")
    m.object = arm
    obj.parent = arm
    mx, nz = influence_stats(obj)
    log(f"weights: max influences {mx}, unweighted verts {nz}")


heat_weights(cat)


def ear_weights(obj):
    """Round 10: the ears are weighted by a height mask, not by bone heat: everything above the ear base on that side
    (near the ear's line) follows the ear bone, fading in over a few mm; the rest stays on the head."""
    g = {x.name: x for x in obj.vertex_groups}
    gi = {x.index: x.name for x in obj.vertex_groups}
    for nm in ("earL", "earR"):
        if nm not in g:
            g[nm] = obj.vertex_groups.new(name=nm)
    n_ear = {k: 0 for k in EARS}
    for v in obj.data.vertices:
        p = v.co
        w = {gi[x.group]: x.weight for x in v.groups if x.weight > 0}
        moved = w.pop("earL", 0.0) + w.pop("earR", 0.0)
        if moved:
            w["head"] = w.get("head", 0.0) + moved
        for nm, (eb_, et_, sx_) in EARS.items():
            m = smooth(eb_.z - 0.002, eb_.z + 0.006, p.z) * (1.0 - smooth(0.014, 0.022, (p.xy - et_.xy).length))
            if m > 0.01:
                w = {k: x * (1 - m) for k, x in w.items()}
                w[nm] = m
                n_ear[nm] += 1
        for nm in g:
            if nm != "root":
                g[nm].remove([v.index])
        tot = sum(w.values())
        for k, x in w.items():
            if k in g and x / max(tot, 1e-6) > 0.005:
                g[k].add([v.index], x / tot, "REPLACE")
    log("ear weights: " + ", ".join(f"{k} {n} verts" for k, n in n_ear.items()))


if EARS:
    ear_weights(cat)
weights_for(whisk_obj, head_only=True)
log("weights done")

# ------------------------------------------------------------------ clips
arm.rotation_mode = "XYZ"
pb = arm.pose.bones
for b in pb:
    b.rotation_mode = "QUATERNION"
REST = {b.name: b.bone.matrix_local.to_quaternion() for b in pb}


def rot(name, pitch=0.0, yaw=0.0, roll=0.0):
    """Rest-axis rotation in armature space (degrees): pitch about X (+ = tip goes up for a forward bone),
    yaw about Z, roll about Y. Converted to the bone's local space."""
    q = Quaternion((0, 0, 1), math.radians(yaw)) @ Quaternion((1, 0, 0), math.radians(pitch)) @ Quaternion((0, 1, 0), math.radians(roll))
    r = REST[name]
    return r.inverted() @ q @ r


def set_pose(pose, root_loc):
    for b in pb:
        b.rotation_quaternion = Quaternion()
        b.location = Vector()
        b.scale = Vector((1, 1, 1))
    for n, v in pose.items():
        pb[n].rotation_quaternion = rot(n, *v[:3])
        if len(v) > 3:          # QF3: optional uniform bone scale (the lap pose tucks the legs into the belly)
            pb[n].scale = Vector((v[3], v[3], v[3]))
    pb["root"].location = REST["root"].inverted() @ Vector(root_loc)


# Grounding ignores the tail (it may brush the floor); paws / rump / belly decide the height.
_gi = {g.index: g.name for g in cat.vertex_groups}
BODY_VERTS = [v.index for v in cat.data.vertices
              if v.groups and not _gi[max(v.groups, key=lambda g: g.weight).group].startswith("tail")]


def lowest_point(tail_too=False):
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    ev = cat.evaluated_get(dg)
    m = ev.to_mesh()
    mw = ev.matrix_world
    if tail_too:
        z = min((mw @ v.co).z for v in m.vertices)
    else:
        vs = m.vertices
        z = min((mw @ vs[i].co).z for i in BODY_VERTS)
    ev.to_mesh_clear()
    return z


def key_all(frame):
    for b in pb:
        b.keyframe_insert("rotation_quaternion", frame=frame)
        b.keyframe_insert("location", frame=frame)
        b.keyframe_insert("scale", frame=frame)


def new_action(name):
    if arm.animation_data is None:
        arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    return act


LEGS = ("FL", "FR", "HL", "HR")
clips = {}


def tail_wave(t, amp_yaw, lift, phase_speed=1.0):
    return {f"tail{i + 1}": (lift * (1 if i < 2 else 0.4), 0, amp_yaw * math.sin(2 * math.pi * (t * phase_speed) - i * 0.6)) for i in range(6)}


def clip(name, frames, fn, loop=True, ground=None):
    """ground(t) -> True: put the lowest mesh point on the floor at this frame; otherwise keep the last offset.
    Default: measured once at t=0 (so breathing/bob in the pose function still moves the body)."""
    act = new_action(name)
    off = None
    lows = []
    for f in list(range(0, frames, 2 if frames > 30 else 1)) + [frames]:
        t = 0.0 if (loop and f == frames) else f / frames
        pose, rl = fn(t)
        set_pose(pose, rl)
        if off is None or (ground is not None and ground(t)):
            off = -lowest_point()
        set_pose(pose, (rl[0], rl[1], rl[2] + off))
        key_all(f)
        lows.append((lowest_point(), lowest_point(True)))
    clips[name] = (act, frames)
    log(f"clip {name}: {frames} frames, lowest body point min {min(l[0] for l in lows):.4f} max {max(l[0] for l in lows):.4f} m, "
        f"incl. tail min {min(l[1] for l in lows):.4f} m")


def idle(t):
    b = math.sin(2 * math.pi * t)
    pose = {"chest": (0.8 * b, 0, 0), "neck": (-0.6 * b, 3 * math.sin(2 * math.pi * t * 0.5 + 1), 0), "head": (0, 2 * math.sin(2 * math.pi * t * 0.5), 0)}
    pose.update(tail_wave(t, 10, 6))
    return pose, (0, 0, 0.002 * b)


def walk(t):
    pose = {}
    phases = {"HL": 0.0, "FL": 0.25, "HR": 0.5, "FR": 0.75}
    for leg, ph in phases.items():
        a = 2 * math.pi * (t + ph)
        swing = math.sin(a)                    # + = leg forward (toward -Y)
        lift = max(0.0, math.cos(a))           # foot lifted while swinging forward
        front = leg[0] == "F"
        up = 24 * swing
        pose[f"{leg}_upper"] = (-up if front else -up, 0, 0)
        if front:
            pose[f"{leg}_lower"] = (20 * lift, 0, 0)
            pose[f"{leg}_foot"] = (40 * lift, 0, 0)
        else:
            pose[f"{leg}_lower"] = (35 * lift, 0, 0)
            pose[f"{leg}_foot"] = (-25 * lift, 0, 0)
    bob = math.sin(4 * math.pi * t)
    pose["chest"] = (0, 3 * math.sin(2 * math.pi * t), 0)
    pose["pelvis"] = (0, -3 * math.sin(2 * math.pi * t), 0)
    pose["neck"] = (-1.5 * bob, 0, 0)
    pose.update(tail_wave(t, 8, 14))
    return pose, (0, 0, 0.004 * bob)


SIT_DROP = -0.36 * H


def sit_pose(t, breathe=True):
    b = math.sin(2 * math.pi * t) if breathe else 0
    pose = {
        "pelvis": (-38, 0, 0),          # rear down, chest up (+pitch tips a forward bone down)
        "chest": (-4 + 0.8 * b, 0, 0),
        "neck": (22, 0, 0),
        "head": (16, 2 * math.sin(2 * math.pi * t * 0.5), 0),
        "FL_upper": (36, 0, 0), "FR_upper": (36, 0, 0),
        "FL_lower": (-2, 0, 0), "FR_lower": (-2, 0, 0),
        "FL_foot": (0, 0, 0), "FR_foot": (0, 0, 0),
        "HL_upper": (-50, 0, 0), "HR_upper": (-50, 0, 0),
        "HL_lower": (140, 0, 0), "HR_lower": (140, 0, 0),
        "HL_foot": (-60, 0, 0), "HR_foot": (-60, 0, 0),
        "tail1": (-55, 0, 0), "tail2": (8, 0, 18), "tail3": (0, 0, 22), "tail4": (0, 0, 22),
        "tail5": (0, 0, 20 + 6 * math.sin(2 * math.pi * t)), "tail6": (0, 0, 18 + 8 * math.sin(2 * math.pi * t - 0.6)),
    }
    return pose, (0, 0.05 * H, 0)


def sit(t):
    return sit_pose(t)


def wash(t):
    pose, rl = sit_pose(t, breathe=False)
    lick = math.sin(2 * math.pi * t * 3)
    pose["FL_upper"] = (-50, -10, 0)
    pose["FL_lower"] = (-95, 0, 0)
    pose["FL_foot"] = (-30, 0, 0)
    pose["neck"] = (38 + 4 * lick, 14, 0)
    pose["head"] = (26 + 6 * lick, 12, 0)
    return pose, rl


def jump(t):
    # 0-0.33 crouch, 0.33-0.5 launch (extend), 0.5-0.8 tuck in the air, 0.8-1 land and stand.
    pose = {}
    crouch = smooth(0.0, 0.3, t) * (1 - smooth(0.33, 0.45, t)) + smooth(0.85, 0.93, t) * (1 - smooth(0.93, 1.0, t)) * 0.6
    ext = smooth(0.33, 0.45, t) * (1 - smooth(0.5, 0.65, t))
    tuck = smooth(0.5, 0.65, t) * (1 - smooth(0.8, 0.9, t))
    land = smooth(0.78, 0.85, t) * (1 - smooth(0.9, 1.0, t))
    for leg in LEGS:
        f = leg[0] == "F"
        u = (-25 * crouch + 25 * ext - 30 * tuck * (1 if f else -1) - 20 * land) if f else (35 * crouch - 45 * ext + 30 * tuck)
        lo = (-40 * crouch - 50 * tuck + 10 * ext) if f else (70 * crouch + 60 * tuck - 10 * ext)
        pose[f"{leg}_upper"] = (u, 0, 0)
        pose[f"{leg}_lower"] = (lo, 0, 0)
        pose[f"{leg}_foot"] = ((30 if f else -40) * crouch, 0, 0)
    pose["pelvis"] = (10 * ext - 8 * land, 0, 0)
    pose["neck"] = (-8 * crouch + 6 * ext, 0, 0)
    pose.update({f"tail{i + 1}": (-15 * crouch + 25 * ext, 0, 0) for i in range(6)})
    return pose, (0, 0, 0)


def sleep(t):
    b = math.sin(2 * math.pi * t)
    curl = 44
    pose = {
        "pelvis": (0, curl * 0.5, 0), "spine": (0, curl * 0.7, 0), "chest": (1.2 * b, curl * 0.7, 0),
        "neck": (28, curl * 0.9, 0), "head": (22, curl * 0.6, -18),
    }
    for leg in LEGS:
        f = leg[0] == "F"
        pose[f"{leg}_upper"] = ((60 if f else -75), 0, 0)
        pose[f"{leg}_lower"] = ((-130 if f else 140), 0, 0)
        pose[f"{leg}_foot"] = ((40 if f else -60), 0, 0)
    for i in range(6):
        pose[f"tail{i + 1}"] = (-85 if i == 0 else -4, 0, (26 + 5 * i))    # down onto the floor, wrapping toward the head
    return pose, (0, 0.02 * H, 0)


clip("idle", 60, idle)
clip("walk", 30, walk, ground=lambda t: True)     # paws never dip below the floor
clip("sit", 90, sit)
clip("wash", 48, wash)
clip("jump", 24, jump, loop=False, ground=lambda t: t < 0.45 or t > 0.8)
clip("sleep", 120, sleep)

if os.environ.get("CAT_DEBUG"):
    for nm, fn in (("rest", lambda t: ({}, (0, 0, 0))), ("sit", sit), ("sleep", sleep)):
        pose, rl = fn(0.0)
        set_pose(pose, rl)
        bpy.context.view_layer.update()
        for bn in ("pelvis", "chest", "FL_upper", "FL_lower", "HL_upper", "HL_lower", "tail1", "tail3", "tail6", "neck", "head"):
            b_ = pb[bn]
            log(f"DBG {nm} {bn}: head {tuple(round(x, 3) for x in b_.head)} tail {tuple(round(x, 3) for x in b_.tail)}")

# Push every action to its own NLA track so the FBX exporter writes one take per clip.
arm.animation_data.action = None
for name, (act, frames) in clips.items():
    tr = arm.animation_data.nla_tracks.new()
    tr.name = name
    st = tr.strips.new(name, 0, act)
    tr.mute = True
bpy.context.scene.render.fps = FPS
bpy.context.scene.frame_start = 0
bpy.context.scene.frame_end = 120

# ------------------------------------------------------------------ export
for o in bpy.data.objects:
    o.select_set(o in (arm, cat, whisk_obj))
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(
    filepath=FBX,
    use_selection=True,
    object_types={"MESH", "ARMATURE"},
    apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z",
    axis_up="Y",
    use_mesh_modifiers=False,
    mesh_smooth_type="FACE",
    add_leaf_bones=False,
    bake_anim=True,
    bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=False,
    bake_anim_use_all_actions=True,
    bake_anim_force_startend_keying=True,
    bake_anim_simplify_factor=0.0,
    path_mode="COPY",
    embed_textures=False,
)
log("exported", FBX, "tris", tris(cat), "+ whiskers", tris(whisk_obj), "bones", len(arm_data.bones))
dims = [max(c[i] for c in co) - min(c[i] for c in co) for i in range(3)]
log(f"final size x{dims[0]:.3f} y{dims[1]:.3f} z{dims[2]:.3f} m (shoulder {SHOULDER_HEIGHT} m)")

# ------------------------------------------------------------------ QF3: static curled lap cat
# The rig cannot fold this scanned cat into a convincing curl (Dan: "worse than the first version"), so the lap cat is a
# dedicated static mesh, shaped once from the rest mesh: legs squashed flat under the belly (paws tucked), the tail laid
# down onto the lap, the head lowered and turned a little, then body + tail bent around a vertical axis beside the neck
# into a ring around the head. Unity swaps it in on landing and only breathes it (a slow scale pulse).
LAP_FBX = os.environ.get("CAT_LAP_OUT") or os.path.join(OUT_DIR, "cat_v1_lap.fbx")
LAP_CURL_DEG = float(os.environ.get("LAP_CURL_DEG", "290"))     # ring angle from the neck to the tail tip
LAP_LEG_K = float(os.environ.get("LAP_LEG_K", "0.06"))          # leg height kept (fraction) below the belly line
LAP_BELLY = float(os.environ.get("LAP_BELLY", "0.56"))          # belly line (x shoulder height; torso bottom ~0.53)
LAP_HEAD_PITCH = float(os.environ.get("LAP_HEAD_PITCH", "-16")) # chin down (degrees)
LAP_HEAD_YAW = float(os.environ.get("LAP_HEAD_YAW", "22"))      # head turned toward the inside of the ring
LAP_SIDE = 1.0                                                  # +1: the ring curls to the cat's left (+X)


def dominant_groups(ob):
    gi = {g.index: g.name for g in ob.vertex_groups}
    out = []
    for v in ob.data.vertices:
        w = {}
        for g in v.groups:
            n = gi[g.group]
            k = "tail" if n.startswith("tail") else ("leg" if n[:2] in LEGS else ("head" if n in ("head", "neck") else "body"))
            w[k] = w.get(k, 0.0) + g.weight
        out.append(w)
    return out


_tc = tail_chain
_tseg = [(_tc[i + 1] - _tc[i]).length for i in range(6)]
_tlen = sum(_tseg)
_r_tail = 0.012 * H / 0.25
_tdrop = 0.45 * _tlen
_floor = LAP_BELLY * H * (1.0 - LAP_LEG_K)                       # paw level after the legs are squashed


def _new_tail(s):
    a = _tc[0]
    if s <= _tdrop:
        u = s / _tdrop
        return Vector((0.0, a.y + s * 0.85, a.z + (_floor + _r_tail + 0.002 - a.z) * (u * u * (3 - 2 * u))))
    return Vector((0.0, a.y + _tdrop * 0.85 + (s - _tdrop), _floor + _r_tail + 0.002))


def _new_tan(s):
    return (_new_tail(min(_tlen, s + 0.004)) - _new_tail(max(0.0, s - 0.004))).normalized()


def _project_tail(p):
    best = (1e9, 0.0, _tc[0], Vector((0, 1, 0)))
    acc = 0.0
    for i in range(6):
        a, b = _tc[i], _tc[i + 1]
        ab = b - a
        t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-9)))
        c = a + ab * t
        d = (p - c).length
        if d < best[0]:
            best = (d, acc + _tseg[i] * t, c, ab.normalized())
        acc += _tseg[i]
    return best


_qh = Quaternion((0, 0, 1), math.radians(LAP_HEAD_YAW * LAP_SIDE)) @ Quaternion((1, 0, 0), math.radians(LAP_HEAD_PITCH))


def lap_shape(pts, groups):
    """Rest positions -> laid tail, squashed legs, lowered head (no floor shift, no bend yet)."""
    zb = LAP_BELLY * H
    out = []
    for p, g in zip(pts, groups):
        tot = sum(g.values()) or 1.0
        wt, wl, wh = g.get("tail", 0) / tot, g.get("leg", 0) / tot, g.get("head", 0) / tot
        q = p.copy()
        if wt > 0.01:
            _, s, c, tan = _project_tail(p)
            laid = _new_tail(s) + tan.rotation_difference(_new_tan(s)) @ (p - c)
            q = q.lerp(laid, smooth(0.1, 0.7, wt))
        if q.z < zb and wt < 0.9:      # (the laid tail keeps its round section)
            k = LAP_LEG_K if wl > 0.3 else 1.0 - (1.0 - LAP_LEG_K) * smooth(0.0, 0.3, wl)
            k = k + (1.0 - k) * smooth(0.1, 0.9, wt)
            q.z = zb + (q.z - zb) * k
        if wh > 0.01:
            q = q.lerp(neck_base + _qh @ (q - neck_base), min(1.0, wh * 1.2))
        out.append(q)
    return out


def lap_bend(pts, ya, R):
    """Bend around a vertical axis at x = side*R from y = ya backward (the head side, y < ya, stays rigid)."""
    out = []
    for p in pts:
        if p.y <= ya:
            out.append(p.copy())
            continue
        phi = (p.y - ya) / R
        rho = R - p.x * LAP_SIDE
        out.append(Vector(((R - rho * math.cos(phi)) * LAP_SIDE, ya + rho * math.sin(phi), p.z)))
    return out


for b in pb:
    b.rotation_quaternion = Quaternion()
    b.location = Vector()
    b.scale = Vector((1, 1, 1))
arm.animation_data.action = None
bpy.context.view_layer.update()
_rest = [v.co.copy() for v in cat.data.vertices]
_grp = dominant_groups(cat)
_body = lap_shape(_rest, _grp)
_wt = [g.get("tail", 0) / (sum(g.values()) or 1.0) for g in _grp]
_sf = [0.5 * (1.0 - smooth(0.35, 0.85, w)) for w in _wt]     # the tail itself keeps its exact (round) shape
# smooth the displacement field over the mesh edges: no tears where the tail / legs meet the body
_disp = [q - p for q, p in zip(_body, _rest)]
_nb = [[] for _ in _rest]
for e in cat.data.edges:
    a_, b_ = e.vertices
    _nb[a_].append(b_)
    _nb[b_].append(a_)
for _ in range(int(os.environ.get("LAP_SMOOTH", "8"))):
    _disp = [d.lerp(sum((_disp[j] for j in _nb[i]), Vector()) / len(_nb[i]), _sf[i]) if _nb[i] else d for i, d in enumerate(_disp)]
_body = [p + d for p, d in zip(_rest, _disp)]
_whisk = lap_shape([v.co.copy() for v in whisk_obj.data.vertices], [{"head": 1.0} for _ in whisk_obj.data.vertices])
_ya = neck_base.y + 0.01 * H / 0.25
_len = max(q.y for q in _body) - _ya
_R = _len / math.radians(LAP_CURL_DEG)
_body, _whisk = lap_bend(_body, _ya, _R), lap_bend(_whisk, _ya, _R)
_zmin = min(q.z for q in _body)
_cx = (min(q.x for q in _body) + max(q.x for q in _body)) / 2
_cy = (min(q.y for q in _body) + max(q.y for q in _body)) / 2
_off = Vector((_cx, _cy, _zmin))
lap_objs = []
for src, pts in ((cat, _body), (whisk_obj, _whisk)):
    me = src.data.copy()
    for v, p in zip(me.vertices, pts):
        v.co = p - _off
    me.update()
    ob = bpy.data.objects.new(src.name.replace("cat_", "cat_lap_") if src.name.startswith("cat_") else src.name + "_lap", me)
    bpy.context.scene.collection.objects.link(ob)
    lap_objs.append(ob)
_b = [p - _off for p in _body]
log(f"lap cat: neck->tail tip {_len:.3f} m bent {LAP_CURL_DEG:.0f} deg (radius {_R:.3f} m); footprint "
    f"{max(q.x for q in _b) - min(q.x for q in _b):.3f} x {max(q.y for q in _b) - min(q.y for q in _b):.3f} m, "
    f"height {max(q.z for q in _b):.3f} m (at shoulder height {H} m)")
for o in bpy.data.objects:
    o.select_set(o in lap_objs)
bpy.context.view_layer.objects.active = lap_objs[0]
bpy.ops.export_scene.fbx(filepath=LAP_FBX, use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL",
                         axis_forward="-Z", axis_up="Y", use_mesh_modifiers=False, mesh_smooth_type="FACE",
                         add_leaf_bones=False, bake_anim=False, path_mode="COPY", embed_textures=False)
log("exported", LAP_FBX, "tris", tris(lap_objs[0]), "+ whiskers", tris(lap_objs[1]))

# ------------------------------------------------------------------ previews
if PREVIEW:
    os.makedirs(PREVIEW, exist_ok=True)
    scn = bpy.context.scene
    scn.render.engine = "BLENDER_WORKBENCH"
    scn.display.shading.light = "STUDIO"
    scn.display.shading.color_type = "SINGLE"
    scn.display.shading.single_color = (0.55, 0.55, 0.55)
    scn.render.resolution_x = 420
    scn.render.resolution_y = 320
    camd = bpy.data.cameras.new("pc")
    camd.type = "ORTHO"
    camd.ortho_scale = 0.62
    camo = bpy.data.objects.new("pc", camd)
    scn.collection.objects.link(camo)
    scn.camera = camo
    views = {"side": Vector((1.5, 0, 0.18)), "front34": Vector((0.9, -1.2, 0.35)), "top": Vector((0.001, 0, 1.5))}
    only = [x for x in os.environ.get("CAT_PREVIEW_CLIPS", "").split(",") if x]
    # QF3: the static lap cat from every side (and from below: the side that rests on her thighs)
    for o in (cat, whisk_obj):
        o.hide_render = True
    lap_views = dict(views)
    lap_views.update({"front": Vector((0.001, -1.5, 0.25)), "back": Vector((0.001, 1.5, 0.25)), "otherside": Vector((-1.5, 0, 0.18)), "below": Vector((0.001, 0.0, -1.5))})
    for vn, loc in lap_views.items():
        camo.location = loc
        d = Vector((0, 0, 0.07)) - loc
        camo.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
        scn.render.filepath = os.path.join(PREVIEW, f"cat_lapmesh_{vn}.png")
        bpy.ops.render.render(write_still=True)
    for o in lap_objs:
        o.hide_render = True
    for o in (cat, whisk_obj):
        o.hide_render = False
    for name, (act, frames) in clips.items():
        if only and name not in only:
            continue
        arm.animation_data.action = act
        for fr in sorted(set([0, frames // 4, frames // 2, (3 * frames) // 4])):
            scn.frame_set(fr)
            for vn, loc in views.items():
                camo.location = loc
                d = Vector((0, 0, 0.14)) - loc
                camo.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
                scn.render.filepath = os.path.join(PREVIEW, f"cat_{name}_{fr:03d}_{vn}.png")
                bpy.ops.render.render(write_still=True)
    log("previews in", PREVIEW)
