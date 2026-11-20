"""Side task S1: Viper (Mars) ready for Mixamo's auto-rigger - the same recipe as the guardian's
elarim_body_for_mixamo.fbx (cleanup_r11.py elarim_mixamo).

Usage (repo root):
  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/cleanup_viper.py -- <repo root> [target tris] [preview dir]
In:  Art/Source/viper/viper_v1.glb (Tripo, ~2 M tris, A-pose).
Out: Art/Export/viper/viper_for_mixamo.fbx (mesh only, grayscale texture embedded), Textures/viper_v1_basecolor.jpg,
     viper_mixamo_metrics.txt, and previews in Docs/Previews/viper/ (front / side / back / q34 + viper_for_mixamo_sheet.png).
Steps: join every mesh piece, weld (merge by distance - the Tripo shell comes in loose triangles/patches), drop tiny
floating crumbs, stand him up facing -Y in Blender (= +Z in Unity / Mixamo front), scale to HEIGHT (crown, not the hood
tip, is not special - the full height is used), origin at the feet centred on the hips, decimate to the target.
Dan then uploads the FBX to Mixamo, downloads 'FBX for Unity, with skin' to Art/Mixamo/viper/viper_body_mixamo.fbx.
"""
import bpy, os, sys, math
import numpy as np

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = os.path.abspath(argv[0]) if argv else os.getcwd()
TARGET = int(argv[1]) if len(argv) > 1 else 36000
PREVIEW = os.path.abspath(argv[2]) if len(argv) > 2 else os.path.join(ROOT, "Docs", "Previews", "viper")
HEIGHT = 2.3            # Docs/MODELS-NEEDED.md "Viper": ~2.3 m
WELD = 0.0006           # metres at 2.3 m height
CRUMB = 50              # loose parts with fewer vertices than this are deleted
_m = []


def log(*a):
    s = " ".join(str(x) for x in a); print("[viper]", s); _m.append(s)


def tris(ob):
    ob.data.calc_loop_triangles(); return len(ob.data.loop_triangles)


def co(ob):
    a = np.empty(len(ob.data.vertices) * 3, dtype=np.float64); ob.data.vertices.foreach_get("co", a); return a.reshape(-1, 3)


def set_co(ob, P):
    ob.data.vertices.foreach_set("co", P.astype(np.float32).ravel()); ob.data.update()


def components(ob):
    n = len(ob.data.vertices)
    E = np.empty(len(ob.data.edges) * 2, dtype=np.int64); ob.data.edges.foreach_get("vertices", E); E = E.reshape(-1, 2)
    par = np.arange(n)
    def f(i):
        r = i
        while par[r] != r: r = par[r]
        while par[i] != r: par[i], i = r, par[i]
        return r
    for a, b in E:
        ra, rb = f(a), f(b)
        if ra != rb: par[ra] = rb
    return np.array([f(i) for i in range(n)])


def edit(fn):
    bpy.ops.object.mode_set(mode='EDIT'); fn(); bpy.ops.object.mode_set(mode='OBJECT')


def main():
    out = os.path.join(ROOT, "Art", "Export", "viper"); os.makedirs(out, exist_ok=True); os.makedirs(PREVIEW, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(ROOT, "Art", "Source", "viper", "viper_v1.glb"))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    log(f"imported {len(meshes)} mesh object(s), {sum(tris(o) for o in meshes)} tris")
    bpy.ops.object.select_all(action='DESELECT')
    for o in meshes: o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if len(meshes) > 1: bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active; ob.name = "viper"
    for o in list(bpy.context.scene.objects):
        if o != ob: bpy.data.objects.remove(o, do_unlink=True)

    # scale + place first so the weld distance is in metres
    P = co(ob); zmin = P[:, 2].min(); H0 = np.ptp(P[:, 2]); s = HEIGHT / H0
    mid = P[(P[:, 2] > zmin + 0.5 * H0) & (P[:, 2] < zmin + 0.6 * H0)]
    P = (P - np.array([np.median(mid[:, 0]), np.median(mid[:, 1]), zmin])) * s
    set_co(ob, P)
    log(f"source height {H0:.3f} -> scaled x{s:.4f}: height {np.ptp(P[:,2]):.3f} m, width {np.ptp(P[:,0]):.3f} m (arms), depth {np.ptp(P[:,1]):.3f} m")

    # weld the loose pieces
    n0 = len(ob.data.vertices); c0 = len(np.unique(components(ob)))
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    edit(lambda: (bpy.ops.mesh.select_all(action='SELECT'), bpy.ops.mesh.remove_doubles(threshold=WELD)))
    roots = components(ob); u, c = np.unique(roots, return_counts=True)
    log(f"weld {WELD*1000:.1f} mm: {n0} -> {len(ob.data.vertices)} vertices, loose parts {c0} -> {len(u)} (largest {c.max()} vertices)")
    crumbs = np.isin(roots, u[c < CRUMB])
    if crumbs.any():
        ob.data.vertices.foreach_set("select", np.ascontiguousarray(crumbs))
        bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_mode(type='VERT')
        bpy.ops.mesh.delete(type='VERT'); bpy.ops.object.mode_set(mode='OBJECT')
        log(f"removed {int((c < CRUMB).sum())} crumbs (< {CRUMB} vertices each, {int(crumbs.sum())} vertices)")
    edit(lambda: (bpy.ops.mesh.select_all(action='SELECT'), bpy.ops.mesh.delete_loose()))

    # facing: Tripo GLBs come in facing -Y in Blender (= +Z in Unity / Mixamo), like the guardian. A foot-based guess
    # was wrong for him (his heel spurs reach further back than the toes), so no automatic turn: TURN_180 only if the
    # front preview ever shows his back.
    TURN_180 = False
    if TURN_180:
        P = co(ob); P[:, 0] *= -1; P[:, 1] *= -1; set_co(ob, P)
    log("facing -Y in Blender (Tripo default)" + (" - turned 180" if TURN_180 else ""))

    # decimate
    n = tris(ob)
    if n > TARGET:
        mod = ob.modifiers.new("dec", "DECIMATE"); mod.decimate_type = "COLLAPSE"; mod.ratio = TARGET / n
        mod.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
        log(f"decimated {n} -> {tris(ob)} tris")
    edit(lambda: (bpy.ops.mesh.select_all(action='SELECT'), bpy.ops.mesh.remove_doubles(threshold=0.0005)))
    bpy.ops.object.shade_smooth()

    # A-pose measure: the angle of each arm below the horizontal (hand = the outermost points at each side)
    P = co(ob)
    sh = P[(P[:, 2] > 0.74 * HEIGHT) & (P[:, 2] < 0.80 * HEIGHT)]
    for side, nm in ((1, "+X"), (-1, "-X")):
        hand = P[np.argsort(-P[:, 0] * side)[:200]].mean(axis=0)
        sx = np.percentile(sh[:, 0] * side, 95) * side; sz = 0.77 * HEIGHT
        ang = math.degrees(math.atan2(sz - hand[2], abs(hand[0] - sx)))
        log(f"arm {nm}: hand at (x {hand[0]:.2f}, z {hand[2]:.2f}) -> {ang:.0f} deg below horizontal")
    log(f"final: {tris(ob)} tris, {len(ob.data.vertices)} vertices, height {np.ptp(P[:,2]):.3f} m, feet at z {P[:,2].min():.3f}")

    # texture: grayscale (the game is monochrome), 2048
    mat = ob.material_slots[0].material if ob.material_slots else None
    img = next((nd.image for nd in mat.node_tree.nodes if nd.type == "TEX_IMAGE" and nd.image is not None), None) if mat else None
    if img is not None:
        tex_dir = os.path.join(out, "Textures"); os.makedirs(tex_dir, exist_ok=True)
        path = os.path.join(tex_dir, "viper_v1_basecolor.jpg")
        img2 = img.copy(); img2.scale(2048, 2048)
        px = np.empty(2048 * 2048 * 4, dtype=np.float32); img2.pixels.foreach_get(px); px = px.reshape(-1, 4)
        g = 0.2126 * px[:, 0] + 0.7152 * px[:, 1] + 0.0722 * px[:, 2]; px[:, 0] = px[:, 1] = px[:, 2] = g
        img2.pixels.foreach_set(px.ravel()); img2.filepath_raw = path; img2.file_format = "JPEG"; img2.save()
        img2.filepath = path
        for nd in mat.node_tree.nodes:
            if nd.type == "TEX_IMAGE": nd.image = img2
        mat.name = "viper_v1_mat"
        log(f"texture {img.size[0]}x{img.size[1]} -> grayscale 2048 {path}")
    else:
        log("WARNING: no texture found")
    for extra in ob.material_slots[1:]: log("extra material", extra.material.name if extra.material else None)

    # previews
    scn = bpy.context.scene
    scn.render.engine = "BLENDER_WORKBENCH"; scn.display.shading.light = "STUDIO"; scn.display.shading.color_type = "TEXTURE"
    scn.render.resolution_x, scn.render.resolution_y = 800, 1000
    cd = bpy.data.cameras.new("pcam"); cd.type = "ORTHO"; cd.ortho_scale = HEIGHT * 1.12; cd.clip_end = 80
    cam = bpy.data.objects.new("pcam", cd); scn.collection.objects.link(cam); scn.camera = cam
    from mathutils import Vector
    views = (("front", (0, -1, 0.04)), ("side", (1, 0, 0.04)), ("back", (0, 1, 0.04)), ("q34", (0.7, -0.7, 0.3)), ("head", (0.35, -1, 0.15)))
    shots = []
    for vn, dv in views:
        c = Vector((0, 0, HEIGHT / 2)); d = Vector(dv).normalized()
        if vn == "head":
            cd.ortho_scale = HEIGHT * 0.32; c = Vector((0, 0, HEIGHT * 0.88))
        else:
            cd.ortho_scale = HEIGHT * 1.12
        cam.location = c + d * 30; cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
        p = os.path.join(PREVIEW, f"viper_for_mixamo_{vn}.png"); scn.render.filepath = p
        bpy.ops.render.render(write_still=True); shots.append(p)
    # sheet: front | side | back
    tiles = []
    for p in shots[:3]:
        im = bpy.data.images.load(p); w, h = im.size
        a = np.empty(w * h * 4, dtype=np.float32); im.pixels.foreach_get(a); tiles.append(a.reshape(h, w, 4))
    sheet = np.concatenate(tiles, axis=1); hh, ww = sheet.shape[:2]
    simg = bpy.data.images.new("sheet", ww, hh, alpha=True); simg.pixels.foreach_set(sheet.ravel())
    simg.filepath_raw = os.path.join(PREVIEW, "viper_for_mixamo_sheet.png"); simg.file_format = "PNG"; simg.save()
    log("sheet", simg.filepath_raw)

    # export (Mixamo wants the texture inside the FBX)
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    fbx = os.path.join(out, "viper_for_mixamo.fbx")
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE", path_mode="COPY", embed_textures=True)
    log(f"exported {fbx} ({os.path.getsize(fbx) / 1e6:.1f} MB)")
    with open(os.path.join(out, "viper_mixamo_metrics.txt"), "w") as f: f.write("\n".join(_m) + "\n")


main()
