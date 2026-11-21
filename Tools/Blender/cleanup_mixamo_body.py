"""Side tasks S1 / S2: a Tripo character made ready for Mixamo's auto-rigger - the same recipe as the guardian's
elarim_body_for_mixamo.fbx (cleanup_r11.py elarim_mixamo). Presets: viper (Mars antagonist, 2.3 m), reptilian (Mars
townspeople, 1.9 m). S3: machinist (the asteroid's Machinist, 4.0 m, 45 k tris; centred on his legs - the claw arm
pulls a mid-height median off-centre - and plate_check: the long front plate must hang free of the thighs).
S4: biavian (Nela / the Biavians, 1.8 m, 36 k tris; extra close-ups of the face, the medallion and the belt buckle).

Usage (repo root):
  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/cleanup_mixamo_body.py -- <repo root> <viper|reptilian|machinist|biavian> [target tris] [preview dir]
In:  Art/Source/<name>/<name>_v1.glb (Tripo, ~2 M tris, A-pose).
Out: Art/Export/<name>/<name>_for_mixamo.fbx (mesh only, grayscale texture embedded), Textures/<name>_v1_basecolor.jpg,
     <name>_mixamo_metrics.txt, previews in Docs/Previews/<name>/ (front / side / back / q34 / head + <name>_for_mixamo_sheet.png).
Steps: join every mesh piece, weld (merge by distance - the Tripo shell comes in loose triangles/patches), drop tiny
floating crumbs, keep Tripo's facing (-Y in Blender = +Z in Unity / Mixamo front), scale to the preset height (full
height), origin at the feet centred on the hips, decimate to the target.
Dan then uploads the FBX to Mixamo, downloads 'FBX for Unity, with skin' to Art/Mixamo/<name>/<name>_body_mixamo.fbx.
"""
import bpy, os, sys, math
import numpy as np

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = os.path.abspath(argv[0]) if argv else os.getcwd()
NAME = argv[1] if len(argv) > 1 else "viper"
PRESETS = {"viper": 2.3,        # Docs/MODELS-NEEDED.md "Viper": ~2.3 m
           "reptilian": 1.9,    # "Reptilians": ~1.9 m
           "machinist": 4.0,    # S3: "The Machinist" (asteroid): ~4 m
           "biavian": 1.8}      # S4: "Biavians" (Nela): ~1.8 m
TARGETS = {"machinist": 45000}
HEIGHT = PRESETS[NAME]
TARGET = int(argv[2]) if len(argv) > 2 else TARGETS.get(NAME, 36000)
PREVIEW = os.path.abspath(argv[3]) if len(argv) > 3 else os.path.join(ROOT, "Docs", "Previews", NAME)
WELD = 0.0006           # metres
CRUMB = 50              # loose parts with fewer vertices than this are deleted
_m = []


def log(*a):
    s = " ".join(str(x) for x in a); print(f"[{NAME}]", s); _m.append(s)


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


def hip_check(ob, need=0.015):
    """S2: the sash's hanging tail (his left hip, +X) and the satchel (his right hip, -X) must not be fused to a thigh, or
    Mixamo's skinning glues them to the leg. Boxes in metres for the 1.9 m reptilian. Tail: below the tunic hem it must be
    its own piece, clear of the leg. Satchel: its front layer vs the trouser below the hem. Logs the gaps; WARNING if < need."""
    from mathutils.kdtree import KDTree
    P = co(ob)
    def gap(a, b):
        kd = KDTree(int(b.sum()))
        for i, p in enumerate(P[b]): kd.insert(p, i)
        kd.balance()
        return min(kd.find(p)[2] for p in P[a])
    box = (P[:, 0] > 0.03) & (P[:, 0] < 0.17) & (P[:, 2] > 0.78) & (P[:, 2] < 0.90)
    E = np.empty(len(ob.data.edges) * 2, dtype=np.int64); ob.data.edges.foreach_get("vertices", E); E = E.reshape(-1, 2)
    par = np.arange(len(P))
    def f(i):
        while par[i] != i: par[i] = par[par[i]]; i = par[i]
        return i
    for a, b in E[box[E[:, 0]] & box[E[:, 1]]]:
        ra, rb = f(a), f(b)
        if ra != rb: par[ra] = rb
    r = np.array([f(i) for i in range(len(P))]); u, c = np.unique(r[box], return_counts=True)
    if len(u) < 2:
        log("WARNING: sash tail: below the hem it is one piece with the left leg (fused)")
    else:
        parts = [r == k for k in u[np.argsort(-c)][:2]]
        tail, leg = sorted(parts, key=lambda m: P[m, 1].mean())     # the tail hangs in front (-Y)
        g = gap(tail, leg)
        log(f"sash tail vs left thigh (below the hem): its own piece, gap {g*1000:.1f} mm" + (" - WARNING: too close" if g < need else " (ok)"))
    sat = (P[:, 0] > -0.22) & (P[:, 0] < -0.06) & (P[:, 1] < -0.085) & (P[:, 2] > 0.88) & (P[:, 2] < 1.06)
    leg = (P[:, 0] > -0.22) & (P[:, 0] < -0.02) & (P[:, 2] > 0.70) & (P[:, 2] < 0.90)
    g = gap(sat, leg)
    log(f"satchel (bottom at z {P[sat, 2].min():.3f} m, rests on the tunic skirt) vs right thigh: gap {g*1000:.1f} mm"
        + (" - WARNING: too close" if g < need else " (ok)"))


def plate_check(ob, need=0.02):
    """S3: the Machinist's long front plate hangs from the gauge belt between his legs. Below the belt it must be its own
    piece with a clear gap to each thigh, or Mixamo's skinning glues it to a leg (the guardian's cape lesson). Per height
    band: the connected pieces inside the band; the plate = the thin front piece between the two leg pieces."""
    P = co(ob); H = HEIGHT
    E = np.empty(len(ob.data.edges) * 2, dtype=np.int64); ob.data.edges.foreach_get("vertices", E); E = E.reshape(-1, 2)
    worst = 9.0
    for z0 in (0.33, 0.36, 0.40, 0.43, 0.46):
        band = (P[:, 2] > z0 * H) & (P[:, 2] < (z0 + 0.025) * H)
        par = np.arange(len(P))
        def f(i):
            while par[i] != i: par[i] = par[par[i]]; i = par[i]
            return i
        for a, b in E[band[E[:, 0]] & band[E[:, 1]]]:
            ra, rb = f(a), f(b)
            if ra != rb: par[ra] = rb
        r = np.full(len(P), -1); idx = np.nonzero(band)[0]; r[idx] = [f(i) for i in idx]
        u, c = np.unique(r[band], return_counts=True)
        parts = [P[r == k] for k, cc in zip(u, c) if cc > 15]
        if len(parts) < 3: log(f"plate at z {z0*H:.2f} m: only {len(parts)} pieces - WARNING: plate fused to a leg?"); worst = 0.0; continue
        parts.sort(key=lambda Q: np.ptp(Q[:, 0]) * np.ptp(Q[:, 1]), reverse=True)
        legs = sorted(parts[:2], key=lambda Q: Q[:, 0].mean())
        mids = [Q for Q in parts[2:] if Q[:, 0].mean() > legs[0][:, 0].max() and Q[:, 0].mean() < legs[1][:, 0].min()]
        if not mids: log(f"plate at z {z0*H:.2f} m: no piece between the legs"); continue
        pl = min(mids, key=lambda Q: Q[:, 1].mean())          # the front-most piece between the legs (front = -Y)
        gl, gr = pl[:, 0].min() - legs[0][:, 0].max(), legs[1][:, 0].min() - pl[:, 0].max()
        worst = min(worst, gl, gr)
        log(f"plate at z {z0*H:.2f} m: its own piece x {pl[:,0].min():+.2f}..{pl[:,0].max():+.2f}; gap to the thighs {gl*100:.0f} / {gr*100:.0f} cm")
    log(f"front plate vs thighs: smallest gap {worst*100:.0f} cm" + (" - WARNING: too close" if worst < need else " (ok - not fused; joined to the body only at the belt)"))


def main():
    out = os.path.join(ROOT, "Art", "Export", NAME); os.makedirs(out, exist_ok=True); os.makedirs(PREVIEW, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(ROOT, "Art", "Source", NAME, f"{NAME}_v1.glb"))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    log(f"imported {len(meshes)} mesh object(s), {sum(tris(o) for o in meshes)} tris")
    bpy.ops.object.select_all(action='DESELECT')
    for o in meshes: o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if len(meshes) > 1: bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active; ob.name = NAME
    for o in list(bpy.context.scene.objects):
        if o != ob: bpy.data.objects.remove(o, do_unlink=True)

    # scale + place first so the weld distance is in metres
    P = co(ob); zmin = P[:, 2].min(); H0 = np.ptp(P[:, 2]); s = HEIGHT / H0
    mid = P[(P[:, 2] > zmin + 0.5 * H0) & (P[:, 2] < zmin + 0.6 * H0)]
    P = (P - np.array([np.median(mid[:, 0]), np.median(mid[:, 1]), zmin])) * s
    set_co(ob, P)
    if NAME == "machinist":
        # centre on his legs (the midpoint of the two legs at knee height), not on a mid-height median the claw arm pulls over
        knee = P[(P[:, 2] > 0.33 * HEIGHT) & (P[:, 2] < 0.36 * HEIGHT)]
        P = P - np.array([(knee[:, 0].min() + knee[:, 0].max()) / 2, (knee[:, 1].min() + knee[:, 1].max()) / 2, 0.0])
        set_co(ob, P)
        log(f"centred on the legs at knee height (x {knee[:,0].min():+.2f}..{knee[:,0].max():+.2f})")
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
    if NAME == "reptilian": hip_check(ob)
    if NAME == "machinist": plate_check(ob)

    # texture: grayscale (the game is monochrome), 2048
    mat = ob.material_slots[0].material if ob.material_slots else None
    img = next((nd.image for nd in mat.node_tree.nodes if nd.type == "TEX_IMAGE" and nd.image is not None), None) if mat else None
    if img is not None:
        tex_dir = os.path.join(out, "Textures"); os.makedirs(tex_dir, exist_ok=True)
        path = os.path.join(tex_dir, f"{NAME}_v1_basecolor.jpg")
        img2 = img.copy(); img2.scale(2048, 2048)
        px = np.empty(2048 * 2048 * 4, dtype=np.float32); img2.pixels.foreach_get(px); px = px.reshape(-1, 4)
        g = 0.2126 * px[:, 0] + 0.7152 * px[:, 1] + 0.0722 * px[:, 2]; px[:, 0] = px[:, 1] = px[:, 2] = g
        img2.pixels.foreach_set(px.ravel()); img2.filepath_raw = path; img2.file_format = "JPEG"; img2.save()
        img2.filepath = path
        for nd in mat.node_tree.nodes:
            if nd.type == "TEX_IMAGE": nd.image = img2
        mat.name = f"{NAME}_v1_mat"
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
    if NAME == "reptilian":   # the satchel and the sash tail at the left hip (character's left = +X)
        views += (("hipL_front", (0, -1, 0.0)), ("hipL_side", (1, 0, 0.0)), ("hipL_back", (0, 1, 0.0)), ("hipL_low", (0.5, -0.6, -0.6)))
    if NAME == "machinist":   # the front plate between the legs (must hang free of the thighs)
        views += (("plate_front", (0, -1, 0.0)), ("plate_low", (0.35, -0.6, -0.55)), ("plate_side", (1, 0, 0.0)))
    if NAME == "biavian":     # S4: the face (eyes, crown ridge), the medallion (chest) and the belt buckle must read
        views += (("face_front", (0, -1, 0.05)), ("face_q34", (0.6, -0.8, 0.1)), ("medallion", (0, -1, 0.0)), ("buckle", (0, -1, 0.0)))
    shots = []
    for vn, dv in views:
        c = Vector((0, 0, HEIGHT / 2)); d = Vector(dv).normalized()
        if vn == "head":
            cd.ortho_scale = HEIGHT * 0.32; c = Vector((0, 0, HEIGHT * 0.88))
        elif vn.startswith("face"):
            cd.ortho_scale = HEIGHT * 0.20; c = Vector((0, 0, HEIGHT * 0.875))
        elif vn == "medallion":
            cd.ortho_scale = HEIGHT * 0.22; c = Vector((0, 0, HEIGHT * 0.72))
        elif vn == "buckle":
            cd.ortho_scale = HEIGHT * 0.22; c = Vector((0, 0, HEIGHT * 0.56))
        elif vn.startswith("plate"):
            cd.ortho_scale = HEIGHT * 0.36; c = Vector((0, 0, HEIGHT * 0.42))
        elif vn.startswith("hipL"):
            cd.ortho_scale = HEIGHT * 0.36; c = Vector((HEIGHT * 0.08, 0, HEIGHT * 0.45))
        else:
            cd.ortho_scale = HEIGHT * 1.12
        cam.location = c + d * 30; cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
        p = os.path.join(PREVIEW, f"{NAME}_for_mixamo_{vn}.png"); scn.render.filepath = p
        bpy.ops.render.render(write_still=True); shots.append(p)
    # sheet: front | side | back
    tiles = []
    for p in shots[:3]:
        im = bpy.data.images.load(p); w, h = im.size
        a = np.empty(w * h * 4, dtype=np.float32); im.pixels.foreach_get(a); tiles.append(a.reshape(h, w, 4))
    sheet = np.concatenate(tiles, axis=1); hh, ww = sheet.shape[:2]
    simg = bpy.data.images.new("sheet", ww, hh, alpha=True); simg.pixels.foreach_set(sheet.ravel())
    simg.filepath_raw = os.path.join(PREVIEW, f"{NAME}_for_mixamo_sheet.png"); simg.file_format = "PNG"; simg.save()
    log("sheet", simg.filepath_raw)

    # export (Mixamo wants the texture inside the FBX)
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    fbx = os.path.join(out, f"{NAME}_for_mixamo.fbx")
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE", path_mode="COPY", embed_textures=True)
    log(f"exported {fbx} ({os.path.getsize(fbx) / 1e6:.1f} MB)")
    with open(os.path.join(out, f"{NAME}_mixamo_metrics.txt"), "w") as f: f.write("\n".join(_m) + "\n")


main()
