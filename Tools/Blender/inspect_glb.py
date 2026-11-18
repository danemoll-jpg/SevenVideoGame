"""Round 11: quick facts about generated models (Tripo GLB) before cleanup.

Usage: blender --background --factory-startup --python Tools/Blender/inspect_glb.py -- <file.glb> [<file.glb> ...]
Prints per file: mesh objects, triangles, bounds (Blender Z-up, metres as imported), the axis extents, materials and
textures, and armature / bone count if any.
"""
import bpy, sys
from mathutils import Vector

files = sys.argv[sys.argv.index("--") + 1:]
for f in files:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=f)
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    arms = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
    tris = 0
    mn = Vector((1e9, 1e9, 1e9)); mx = Vector((-1e9, -1e9, -1e9))
    mats = set(); imgs = set()
    for o in meshes:
        dg = bpy.context.evaluated_depsgraph_get(); m = o.evaluated_get(dg).to_mesh()
        m.calc_loop_triangles(); tris += len(m.loop_triangles)
        for v in m.vertices:
            w = o.matrix_world @ v.co
            mn = Vector((min(mn.x, w.x), min(mn.y, w.y), min(mn.z, w.z))); mx = Vector((max(mx.x, w.x), max(mx.y, w.y), max(mx.z, w.z)))
        for s in o.material_slots:
            if s.material:
                mats.add(s.material.name)
                if s.material.node_tree:
                    for n in s.material.node_tree.nodes:
                        if n.type == 'TEX_IMAGE' and n.image: imgs.add(f"{n.image.name} {n.image.size[0]}x{n.image.size[1]}")
    size = mx - mn
    print(f"INSPECT {f}: {len(meshes)} meshes, {tris} tris, bounds min {tuple(round(c,3) for c in mn)} max {tuple(round(c,3) for c in mx)}, "
          f"size x {size.x:.3f} y {size.y:.3f} z {size.z:.3f}, materials {sorted(mats)}, images {sorted(imgs)}, "
          f"armatures {len(arms)} bones {sum(len(a.data.bones) for a in arms)}")
