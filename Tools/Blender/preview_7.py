"""Render grayscale previews of the exported FBX.

  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/preview_7.py -- [repo_root] [fbx] [out_dir]

Input : Art/Export/7_v2_mixamo.fbx
Output: Docs/Previews/7_v2_{front,back,side,face}.png  (gray background, flat light, grayscale, Workbench)
"""
import bpy, os, sys, math
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = os.path.abspath(argv[0]) if argv else os.getcwd()
FBX = os.path.abspath(argv[1]) if len(argv) > 1 else os.path.join(ROOT, "Art", "Export", "7_v2_mixamo.fbx")
OUT = os.path.abspath(argv[2]) if len(argv) > 2 else os.path.join(ROOT, "Docs", "Previews")
os.makedirs(OUT, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX)
ob = [o for o in bpy.data.objects if o.type == "MESH"][0]
ws = [ob.matrix_world @ v.co for v in ob.data.vertices]
mn = Vector([min(w[i] for w in ws) for i in range(3)])
mx = Vector([max(w[i] for w in ws) for i in range(3)])
print("[preview_7] imported", ob.name, "tris", sum(len(p.vertices) - 2 for p in ob.data.polygons),
      "bbox", [round(x, 3) for x in mn], [round(x, 3) for x in mx],
      "slots", [s.material.name for s in ob.material_slots],
      "images", [(i.name, tuple(i.size)) for i in bpy.data.images], flush=True)

sc = bpy.context.scene
sc.render.engine = "BLENDER_WORKBENCH"
sh = sc.display.shading
sh.light = "FLAT"
sh.color_type = "TEXTURE"
sh.background_type = "VIEWPORT"
sh.background_color = (0.45, 0.45, 0.45)
sc.render.film_transparent = False
world = bpy.data.worlds.new("gray")  # Workbench renders use the world colour as background
world.color = (0.45, 0.45, 0.45)
sc.world = world
sc.view_settings.view_transform = "Standard"
sc.render.image_settings.color_mode = "BW"
sc.render.resolution_percentage = 100

# A thin dark floor line so "feet on the floor" can be judged.
bpy.ops.mesh.primitive_plane_add(size=3, location=(0, 0, 0))
floor = bpy.context.active_object
fm = bpy.data.materials.new("floor")
fm.diffuse_color = (0.3, 0.3, 0.3, 1)
floor.data.materials.append(fm)

cam_data = bpy.data.cameras.new("cam")
cam = bpy.data.objects.new("cam", cam_data)
sc.collection.objects.link(cam)
sc.camera = cam
H = mx.z - mn.z


def shot(name, loc, target, ortho=None, lens=85, res=(900, 1400)):
    sc.render.resolution_x, sc.render.resolution_y = res
    cam.location = loc
    d = Vector(target) - Vector(loc)
    cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    if ortho:
        cam_data.type = "ORTHO"
        cam_data.ortho_scale = ortho
    else:
        cam_data.type = "PERSP"
        cam_data.lens = lens
    sc.render.filepath = os.path.join(OUT, "7_v2_%s.png" % name)
    bpy.ops.render.render(write_still=True)
    print("[preview_7] wrote", sc.render.filepath, flush=True)


mid = (0, 0, H * 0.5)
eye = 0.02  # tiny downward tilt so the floor plane shows as a line
shot("front", (0, -6, H * 0.5 + eye), mid, ortho=H * 1.1)
shot("back", (0, 6, H * 0.5 + eye), mid, ortho=H * 1.1)
shot("side", (6, 0, H * 0.5 + eye), mid, ortho=H * 1.1)
# Face close-up from slightly to the side and above, so both orbs/pigtails are visible too.
head = (0, 0, H * 0.90)
shot("face", (0.35, -0.75, H * 0.93), head, lens=85, res=(1000, 1000))
# Back-of-head close-ups with studio light + specular so the metal hair ties read as shiny.
sh.light = "STUDIO"
sh.show_specular_highlight = True
# Workbench darkens fully metallic materials (no environment to reflect); for the preview only, show the metal
# hair ties as bright polished silver. The exported material itself is unchanged.
for m in bpy.data.materials:
    if "hairtie_metal" in m.name:
        m.diffuse_color = (0.9, 0.9, 0.9, 1)
        m.metallic = 0.35
        m.roughness = 0.15
shot("head_top", (0.0, 0.5, H * 1.12), head, lens=50, res=(1000, 1000))
shot("head_back", (-0.3, 0.8, H * 0.95), head, lens=85, res=(1000, 1000))
