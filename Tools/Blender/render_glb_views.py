"""Round 18: quick orthographic top / side / front renders of a source GLB (to see how a Tripo model stands before rigging).
F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/render_glb_views.py -- <file.glb> <out_dir> [rotz_deg]"""
import bpy, sys, os, math
from mathutils import Vector
argv = sys.argv[sys.argv.index("--") + 1:]
src, out = argv[0], argv[1]; rz = float(argv[2]) if len(argv) > 2 else 0.0
os.makedirs(out, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
ms = [o for o in bpy.data.objects if o.type == "MESH"]
for o in ms:
    o.rotation_euler[2] += math.radians(rz)
bpy.context.view_layer.update()
pts = [o.matrix_world @ v.co for o in ms for i, v in enumerate(o.data.vertices) if i % 50 == 0]
mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts))); mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
c = (mn + mx) / 2; size = max(mx - mn)
scn = bpy.context.scene; scn.render.engine = "BLENDER_WORKBENCH"; scn.render.resolution_x = 420; scn.render.resolution_y = 420
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); scn.collection.objects.link(cam); scn.camera = cam
cam.data.type = "ORTHO"; cam.data.ortho_scale = size * 1.15
for name, d in (("top", Vector((0, 0, 1))), ("side", Vector((1, 0, 0))), ("front", Vector((0, -1, 0)))):
    cam.location = c + d * size * 3
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y" if name != "top" else "Y").to_euler()
    scn.render.filepath = os.path.join(out, f"src_{name}.png"); bpy.ops.render.render(write_still=True)
print("bounds", tuple(round(x, 3) for x in mn), tuple(round(x, 3) for x in mx))
