"""Round 18: where a GLB's mass sits (bounds, centroid of the top quarter = the head) - for fitting new Tripo models.
F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/inspect_glb.py -- <file.glb> [preview.png]"""
import bpy, sys, os
from mathutils import Vector
argv = sys.argv[sys.argv.index("--") + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=argv[0])
ms = [o for o in bpy.data.objects if o.type == "MESH"]
pts = []
for o in ms:
    me = o.data
    tris = sum(len(p.vertices) - 2 for p in me.polygons)
    print(f"mesh {o.name}: {len(me.vertices)} verts, {tris} tris, materials {[m.name for m in me.materials if m]}")
    pts += [o.matrix_world @ v.co for v in me.vertices]
mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
print("bounds min", tuple(round(x, 3) for x in mn), "max", tuple(round(x, 3) for x in mx), "size", tuple(round(x, 3) for x in (mx - mn)))
# where the high mass (head) sits along x and y: the top 15% of height
h = mx.z - mn.z
top = [p for p in pts if p.z > mx.z - 0.25 * h]
c = sum(top, Vector()) / max(1, len(top))
allc = sum(pts, Vector()) / len(pts)
print("centroid", tuple(round(x, 3) for x in allc), "top-25% centroid", tuple(round(x, 3) for x in c))
for o in bpy.data.objects:
    if o.type == "MESH":
        for m in o.data.materials:
            if m and m.node_tree:
                for n in m.node_tree.nodes:
                    if n.type == "TEX_IMAGE" and n.image: print("texture", n.image.name, n.image.size[:])
