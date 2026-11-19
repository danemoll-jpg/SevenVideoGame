"""Round 12 (B1): the guardian's kneel. Dan's Mixamo "kneel" clip (Art/Mixamo/elarim/elarim_kneel.fbx) never kneels -
its hips and head stay at standing height for all 141 frames (measured) - so C03's "the guardian kneels to her level"
never showed. This makes a real kneel on his own Mixamo skeleton, the same way as 7's riding pose:

Usage (repo root):
  F:\blender\blender.exe --background --factory-startup --python Tools/Blender/make_kneel_pose.py -- <repo root> [preview dir]
In:  Art/Mixamo/elarim/elarim_idle.fbx (the guardian's Mixamo skeleton; only the armature is used).
Out: Art/Export/anim/elarim_kneel_pose.fbx (armature + one looping clip "kneel", 3 s at 30 fps, a subtle breath) and
     previews (side / front / three-quarter) in the preview dir (Docs/Previews/r12 by default).
The pose: on one knee - the left knee on the ground (thigh down, shin back along the ground), the right foot planted
forward (thigh level, shin straight down), the torso upright leaning a little forward, the head bowed to look at what
she holds out, the right forearm resting across the right knee, the left arm relaxed. FightDemoBuilderVenus replaces
the "kneel" state of Elarim.controller with it.
Knobs: SPINE_LEAN, NECK_LEAN, HEAD_LEAN, the leg / arm directions below.
"""
import bpy, os, sys, math
from mathutils import Vector, Matrix, Quaternion

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = os.path.abspath(argv[0]) if argv else os.getcwd()
PREVIEW = os.path.abspath(argv[1]) if len(argv) > 1 else os.path.join(ROOT, "Docs", "Previews", "r12")
OUT_DIR = os.path.join(ROOT, "Art", "Export", "anim")
FPS, FRAMES = 30, 90
SPINE_LEAN = (6.0, 10.0, 14.0)
NECK_LEAN, HEAD_LEAN = 22.0, 38.0
# (forward, up, side-out) per side: "R" = the planted foot forward, "L" = the knee on the ground
LEGS = {"Right": dict(thigh=(1.0, 0.04, 0.12), shin=(0.04, -1.0, 0.02), foot=(0.75, -0.45, 0.0)),
        "Left": dict(thigh=(0.18, -1.0, 0.10), shin=(-1.0, -0.06, 0.04), foot=(-0.45, -0.85, 0.0))}
ARMS = {"Right": dict(up=(0.35, -0.92, 0.12), fore=(0.92, -0.22, -0.18), hand=(0.9, -0.4, 0.0)),
        "Left": dict(up=(0.08, -1.0, 0.16), fore=(0.25, -0.95, 0.05), hand=(0.15, -1.0, 0.0))}

log_lines = []
def log(*a):
    s = " ".join(str(x) for x in a); print("[kneel]", s); log_lines.append(s)


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(ROOT, "Art", "Mixamo", "elarim", "elarim_idle.fbx"), automatic_bone_orientation=False)
arm = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
for o in list(bpy.context.scene.objects):
    if o.type != 'ARMATURE': bpy.data.objects.remove(o, do_unlink=True)
arm.animation_data_clear()
for a in list(bpy.data.actions): bpy.data.actions.remove(a)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode='POSE')
for pb in arm.pose.bones:
    pb.rotation_mode = 'QUATERNION'
    pb.location = (0, 0, 0); pb.rotation_quaternion = (1, 0, 0, 0); pb.scale = (1, 1, 1)
bpy.context.view_layer.update()

MW = arm.matrix_world
def P(n): return arm.pose.bones.get("mixamorig:" + n)
def head_w(n): return MW @ P(n).head
def tail_w(n): return MW @ P(n).tail

# character frame (world): up = +Z; forward from the heel to the toe; left from the right hip to the left hip
U = Vector((0, 0, 1))
F = (head_w("LeftToeBase") - head_w("LeftFoot")); F.z = 0; F.normalize()
Lw = head_w("LeftUpLeg") - head_w("RightUpLeg"); Lw.z = 0; Lw.normalize()
log(f"character frame: forward {tuple(round(c, 3) for c in F)}, left {tuple(round(c, 3) for c in Lw)}")
height = tail_w("HeadTop_End").z if P("HeadTop_End") else tail_w("Head").z
log(f"guardian rest height {height:.3f} (world units of the import)")
scale = height / 3.5   # metres -> import units (the Elarim are 3.5 m)
SHIN_LEN = (head_w("LeftFoot") - head_w("LeftLeg")).length


def dirv(t, side):
    f, u, s = t
    return (F * f + U * u + Lw * (s * side)).normalized()


def aim(name, want_w):
    """Rotate a pose bone about its head so its head->tail points along want_w (world)."""
    pb = P(name)
    if pb is None: return
    bpy.context.view_layer.update()
    h = MW @ pb.head; t = MW @ pb.tail
    cur = (t - h).normalized()
    q = cur.rotation_difference(want_w)
    # world rotation about the head -> armature space -> pose matrix
    Mw = MW @ pb.matrix
    R = Matrix.Translation(h) @ q.to_matrix().to_4x4() @ Matrix.Translation(-h)
    pb.matrix = MW.inverted() @ R @ Mw
    bpy.context.view_layer.update()


def lean(v, deg):
    """Up vector leaned toward forward by deg."""
    a = math.radians(deg)
    return (U * math.cos(a) + F * math.sin(a)).normalized()


def build(breath=0.0, settle=0.0):
    for pb in arm.pose.bones:
        pb.location = (0, 0, 0); pb.rotation_quaternion = (1, 0, 0, 0)
    bpy.context.view_layer.update()
    hips = P("Hips")
    # on one knee: the hips come down by about a shin's length
    drop = SHIN_LEN * 0.93 + settle * 0.004 * scale
    Mh = MW @ hips.matrix
    hips.matrix = MW.inverted() @ (Matrix.Translation(-U * drop) @ Mh)
    bpy.context.view_layer.update()
    for n, d in zip(("Spine", "Spine1", "Spine2"), SPINE_LEAN):
        aim(n, lean(U, d - breath * (1.2 if n == "Spine2" else 0.5)))
    aim("Neck", lean(U, NECK_LEAN))
    aim("Head", lean(U, HEAD_LEAN))
    for side, s in (("Left", 1.0), ("Right", -1.0)):
        g = LEGS[side]; a = ARMS[side]
        aim(side + "UpLeg", dirv(g["thigh"], s))
        aim(side + "Leg", dirv(g["shin"], s))
        aim(side + "Foot", dirv(g["foot"], s))
        aim(side + "ToeBase", dirv((0.9, -0.4, 0.0) if side == "Right" else (-0.2, -1.0, 0.0), s))
        aim(side + "Arm", dirv(a["up"], s))
        aim(side + "ForeArm", dirv(a["fore"], s))
        aim(side + "Hand", dirv(a["hand"], s))


act = bpy.data.actions.new("kneel")
arm.animation_data_create(); arm.animation_data.action = act
for f in range(1, FRAMES + 1, 9):
    ph = (f - 1) / (FRAMES - 1) * 2 * math.pi
    build(breath=0.5 - 0.5 * math.cos(ph), settle=math.sin(ph))
    for pb in arm.pose.bones:
        pb.keyframe_insert("rotation_quaternion", frame=f)
        if pb.name.endswith("Hips"): pb.keyframe_insert("location", frame=f)
# make the loop seamless: last key = first key
build(breath=0.0, settle=0.0)
for pb in arm.pose.bones:
    pb.keyframe_insert("rotation_quaternion", frame=FRAMES)
    if pb.name.endswith("Hips"): pb.keyframe_insert("location", frame=FRAMES)
act.use_fake_user = True
bpy.context.scene.frame_start, bpy.context.scene.frame_end = 1, FRAMES
bpy.context.scene.render.fps = FPS
bpy.context.scene.frame_set(1)

# report: where the hands, knees and feet are relative to the hips (metres, character frame)
def rel(n, tail=False):
    v = (tail_w(n) if tail else head_w(n)) - head_w("Hips")
    return (round(v.dot(F) / scale, 3), round(v.dot(U) / scale, 3), round(v.dot(Lw) / scale, 3))
for n in ("LeftHand", "RightHand", "LeftLeg", "RightLeg", "LeftFoot", "RightFoot", "Head"):
    log(f"{n} vs hips (forward, up, left) m: {rel(n)}")
log(f"head height kneeling: {(head_w('Head').z - min(head_w('LeftFoot').z, head_w('RightFoot').z, head_w('LeftLeg').z)) / scale:.3f} m (standing {(height) / scale:.2f}); left knee above the ground {(head_w('LeftLeg').z - min(head_w('LeftFoot').z, head_w('RightFoot').z)) / scale:.3f} m")
log(f"hips above the feet: {(head_w('Hips').z - min(head_w('LeftFoot').z, head_w('RightFoot').z)) / scale:.3f} m")

bpy.ops.object.mode_set(mode='OBJECT')
os.makedirs(OUT_DIR, exist_ok=True)
out = os.path.join(OUT_DIR, "elarim_kneel_pose.fbx")
bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={"ARMATURE"}, add_leaf_bones=False,
                         bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False,
                         bake_anim_use_all_actions=False, bake_anim_force_startend_keying=True,
                         bake_anim_simplify_factor=0.0, apply_scale_options="FBX_SCALE_NONE",
                         axis_forward="-Z", axis_up="Y", use_armature_deform_only=False)
log("exported", out)

# previews: the posed skeleton as solid sticks (an armature itself does not render)
os.makedirs(PREVIEW, exist_ok=True)
bpy.context.scene.frame_set(1); bpy.context.view_layer.update()
for pb in arm.pose.bones:
    if pb.name.endswith("_End") or "Hand" in pb.name and pb.name.count("Hand") and not pb.name.endswith("Hand"): continue
    h = MW @ pb.head; t = MW @ pb.tail; d = t - h
    if d.length < 1e-4: continue
    bpy.ops.mesh.primitive_cylinder_add(radius=0.035 * scale, depth=d.length, location=(h + t) / 2)
    c = bpy.context.active_object; c.rotation_mode = 'QUATERNION'; c.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(d.normalized())
bpy.ops.mesh.primitive_plane_add(size=4 * scale, location=(head_w("Hips").x, head_w("Hips").y, min(head_w("LeftFoot").z, head_w("RightFoot").z) - 0.02 * scale))
scn = bpy.context.scene
scn.render.engine = "BLENDER_WORKBENCH"; scn.render.resolution_x = scn.render.resolution_y = 640
scn.display.shading.light = "STUDIO"; scn.display.shading.color_type = "SINGLE"
cd = bpy.data.cameras.new("pcam"); cd.type = "ORTHO"; cd.ortho_scale = 2.0 * scale
cam = bpy.data.objects.new("pcam", cd); scn.collection.objects.link(cam); scn.camera = cam
c = head_w("Hips") + U * 0.15 * scale
for nm, d in (("side", -Lw), ("front", F), ("q34", (F - Lw * 0.8 + U * 0.4).normalized())):
    cam.location = c + d * 10 * scale; cam.data.clip_end = 40 * scale
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    scn.render.filepath = os.path.join(PREVIEW, f"kneel_pose_{nm}.png")
    try: bpy.ops.render.render(write_still=True)
    except Exception as e: log("preview failed:", e)
with open(os.path.join(OUT_DIR, "elarim_kneel_metrics.txt"), "w") as f: f.write("\n".join(log_lines) + "\n")
log("done")
