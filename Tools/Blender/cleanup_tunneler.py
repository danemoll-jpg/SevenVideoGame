"""Round 14: Tunneler for Europa (on the base's screen and boarding his escape craft).

Usage (repo root):
  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/cleanup_tunneler.py -- <repo root> [preview dir]
In:  Art/Source/tunneler/tunneler_v1.glb (2.0 M tris, geometry only - no UVs / texture; Dan: fine for now).
Out: Art/Export/tunneler/tunneler_v1.fbx: two meshes - "TunnelerBody" and "DrillHead" (the cone above the collar socket,
     separated so Unity can twist / nod it with his voice's loudness, MODELS-NEEDED: "drill head moves as he talks") -
     decimated to ~36 k tris, HEIGHT tall, feet at 0, facing -Y (Unity +Z). No rig yet (a Humanoid rig + Mixamo clips are
     still open in MODELS-NEEDED); Europa only needs him standing on a screen and walking into his craft (moved as a whole).
     Previews: Docs/Previews/r14/tunneler_*.png. Knobs: HEIGHT, HEAD_CUT (fraction of the height where the drill starts).
"""
import bpy, os, sys
import numpy as np
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = os.path.abspath(argv[0]) if argv else os.getcwd()
PREVIEW = os.path.abspath(argv[1]) if len(argv) > 1 else os.path.join(ROOT, "Docs", "Previews", "r14")
HEIGHT, HEAD_CUT, TRIS = 2.6, 0.80, 36000
_m = []
def log(*a):
    s = " ".join(str(x) for x in a); print("[tun]", s); _m.append(s)
def tris(ob):
    ob.data.calc_loop_triangles(); return len(ob.data.loop_triangles)
def co(ob):
    a = np.empty(len(ob.data.vertices) * 3); ob.data.vertices.foreach_get("co", a); return a.reshape(-1, 3)
def set_co(ob, P):
    ob.data.vertices.foreach_set("co", P.astype(np.float32).ravel()); ob.data.update()

out = os.path.join(ROOT, "Art", "Export", "tunneler"); os.makedirs(out, exist_ok=True); os.makedirs(PREVIEW, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.join(ROOT, "Art", "Source", "tunneler", "tunneler_v1.glb"))
ms = [o for o in bpy.context.scene.objects if o.type == 'MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in ms: o.select_set(True)
bpy.context.view_layer.objects.active = ms[0]
if len(ms) > 1: bpy.ops.object.join()
ob = bpy.context.view_layer.objects.active; ob.name = "TunnelerBody"
bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM'); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for o in list(bpy.context.scene.objects):
    if o.type != 'MESH': bpy.data.objects.remove(o, do_unlink=True)
n0 = tris(ob)
md = ob.modifiers.new("dec", "DECIMATE"); md.decimate_type = "COLLAPSE"; md.ratio = min(1.0, TRIS / max(1, n0)); bpy.ops.object.modifier_apply(modifier=md.name)
P = co(ob); zmin = P[:, 2].min(); s = HEIGHT / np.ptp(P[:, 2])
mid = P[(P[:, 2] > zmin + 0.4 * np.ptp(P[:, 2])) & (P[:, 2] < zmin + 0.6 * np.ptp(P[:, 2]))]
P = (P - np.array([np.median(mid[:, 0]), np.median(mid[:, 1]), zmin])) * s
set_co(ob, P)
log(f"tunneler: {n0} -> {tris(ob)} tris, height {np.ptp(P[:, 2]):.2f} m, width {np.ptp(P[:, 0]):.2f}, depth {np.ptp(P[:, 1]):.2f}")
# the drill head: faces whose vertices are all above HEAD_CUT of the height
cut = HEAD_CUT * HEIGHT
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='DESELECT'); bpy.ops.object.mode_set(mode='OBJECT')
ob.data.vertices.foreach_set("select", np.ascontiguousarray(P[:, 2] > cut))
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_mode(type='VERT'); bpy.ops.mesh.separate(type='SELECTED'); bpy.ops.object.mode_set(mode='OBJECT')
head = next((o for o in bpy.context.selected_objects if o != ob), None)
if head is not None:
    head.name = "DrillHead"
    H = co(head); piv = np.array([np.median(H[:, 0]), np.median(H[:, 1]), H[:, 2].min()])
    set_co(head, H - piv); head.location = Vector(piv.tolist())
    log(f"drill head: {len(H)} vertices above {cut:.2f} m, pivot {piv.round(3)}")
mat = bpy.data.materials.new("tunneler_stone"); mat.diffuse_color = (0.45, 0.45, 0.45, 1)
for o in (ob, head):
    if o is not None: o.data.materials.clear(); o.data.materials.append(mat)
scn = bpy.context.scene; scn.render.engine = "BLENDER_WORKBENCH"; scn.render.resolution_x = scn.render.resolution_y = 700
cd = bpy.data.cameras.new("pcam"); cd.type = "ORTHO"; cam = bpy.data.objects.new("pcam", cd); scn.collection.objects.link(cam); scn.camera = cam
for vn, dv in (("front", (0, -1, 0.05)), ("side", (1, 0, 0.05)), ("q34", (0.7, -0.7, 0.3))):
    d = Vector(dv).normalized(); cam.data.ortho_scale = HEIGHT * 1.2; cam.location = Vector((0, 0, HEIGHT / 2)) + d * 30; cam.data.clip_end = 80
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler(); scn.render.filepath = os.path.join(PREVIEW, f"tunneler_{vn}.png"); bpy.ops.render.render(write_still=True)
bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True)
if head is not None: head.select_set(True)
bpy.ops.export_scene.fbx(filepath=os.path.join(out, "tunneler_v1.fbx"), use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL",
                         axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE")
log("exported", os.path.join(out, "tunneler_v1.fbx"))
open(os.path.join(out, "tunneler_metrics.txt"), "w").write("\n".join(_m) + "\n")
