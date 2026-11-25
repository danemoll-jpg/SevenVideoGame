"""Round 22: inspect Dan's ShiftStorm jet models (Art/Source/shiftstorm/<Design>/Plane.fbx): objects, empties, bounds, and
orthographic previews (side / top / front) with a 0.25 m grid, to place the seat, grips and foot rests.
  blender --background --factory-startup --python Tools/Blender/inspect_shiftstorm.py -- <repo> <Design> [out_dir]"""
import bpy, sys, os, mathutils
argv = sys.argv[sys.argv.index("--") + 1:]
repo, design = argv[0], argv[1]; out = argv[2] if len(argv) > 2 else os.path.join(repo, "Docs", "Previews", "r22")
bpy.ops.wm.read_factory_settings(use_empty=True)
src = os.path.join(repo, "Art", "Source", "shiftstorm", design, "Plane.fbx")
bpy.ops.import_scene.fbx(filepath=src)
mn = mathutils.Vector((1e9,)*3); mx = -mn
for o in bpy.context.scene.objects:
    print("OBJ", o.name, o.type, tuple(round(v, 3) for v in o.location), tuple(round(v, 3) for v in o.rotation_euler), tuple(round(v, 3) for v in o.scale), "parent", o.parent.name if o.parent else "-")
    if o.type == "MESH":
        print("   tris", sum(len(p.vertices) - 2 for p in o.data.polygons), "mats", [m.name for m in o.data.materials if m])
        for c in o.bound_box:
            w = o.matrix_world @ mathutils.Vector(c); mn = mathutils.Vector(map(min, mn, w)); mx = mathutils.Vector(map(max, mx, w))
print("BOUNDS min", tuple(round(v, 3) for v in mn), "max", tuple(round(v, 3) for v in mx), "size", tuple(round(v, 3) for v in (mx - mn)))
# previews
sc = bpy.context.scene; sc.render.engine = "BLENDER_WORKBENCH"; sc.render.resolution_x = 1200; sc.render.resolution_y = 800
sc.display.shading.light = "STUDIO"; sc.display.shading.color_type = "TEXTURE"
sc.render.film_transparent = False
ctr = (mn + mx) / 2; size = max((mx - mn)) * 1.15
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = "ORTHO"; cam.data.ortho_scale = size
for name, d, up in [("side", (1, 0, 0), (0, 0, 1)), ("side2", (-1, 0, 0), (0, 0, 1)), ("top", (0, 0, 1), (0, 1, 0)), ("front", (0, 1, 0), (0, 0, 1)), ("back", (0, -1, 0), (0, 0, 1)), ("persp", (0.8, -0.9, 0.6), (0, 0, 1))]:
    dv = mathutils.Vector(d).normalized(); cam.location = ctr + dv * size * 3
    cam.rotation_euler = (-dv).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = os.path.join(out, f"{design}_{name}.png"); bpy.ops.render.render(write_still=True)
