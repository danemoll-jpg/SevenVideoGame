"""Field guide portraits for the ship's console (Round 13, item 2).

Usage (from the repo root, hidden, Below Normal):
  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/render_portraits.py -- <repo> [only_key ...]

For every key below: import the exported model, put it in its rest pose (no clip), frame it (head-and-shoulders,
three-quarter or full figure), light it in Kyle's look (soft key, strong white rim, a bright misty glow behind the
dark figure, a little depth fog) and render with Cycles (CPU, denoised). The result is converted to strict grayscale
(R = G = B on every pixel, checked) and saved to Unity/Assets/Resources/Console/Portraits/<key>.png (512 x 640).
A missing model gives a plain placeholder card (dark, a soft glow, no figure) - never an error.
"""
import bpy, sys, os, math
import numpy as np
from mathutils import Vector, Matrix

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
REPO = argv[0] if argv else os.getcwd()
ONLY = set(argv[1:])
OUT = os.path.join(REPO, "Unity", "Assets", "Resources", "Console", "Portraits")
os.makedirs(OUT, exist_ok=True)
W, H = 512, 640
SAMPLES = 48

def log(msg):
    print("[portraits] " + msg, flush=True)

E = os.path.join(REPO, "Art", "Export")
S = os.path.join(REPO, "Art", "Source")
# key: (model files tried in order, framing, yaw degrees (+ = turn the figure's left toward the camera))
# framing: "head" (head and shoulders), "bust" (head to waist), "body" (whole figure)
PORTRAITS = {
    "cat":              ([os.path.join(E, "cat", "cat_v1_rigged.fbx"), os.path.join(S, "cat", "cat_v1.glb")], "body", 35),
    "scree":            ([os.path.join(E, "scree", "scree_v1_rigged.fbx"), os.path.join(S, "scree", "scree_v1.glb")], "bust", 20),
    "elarim_guardian":  ([os.path.join(E, "elarim", "elarim_v1_rigged.fbx"), os.path.join(S, "elarim", "elarim_v1.glb")], "head", 15),
    "elarim_attendant": ([os.path.join(E, "elarim", "elarim_v1_rigged.fbx"), os.path.join(S, "elarim", "elarim_v1.glb")], "bust", -35),
    "elarim":           ([os.path.join(E, "elarim", "elarim_v1_rigged.fbx"), os.path.join(S, "elarim", "elarim_v1.glb")], "body", 25),
    "worm":             ([os.path.join(E, "worm", "worm_v1_rigged.fbx"), os.path.join(S, "wormmaster", "worm_v1.glb")], "body", 12),
    "wormmaster":       ([os.path.join(E, "wormmaster", "wormmaster_v1_rigged.fbx"), os.path.join(S, "wormmaster", "wormmaster_v1.glb")], "bust", 20),
    "tunneler":         ([os.path.join(S, "tunneler", "tunneler_v1.glb")], "body", 25),
    "ratwing":          ([os.path.join(E, "ratwing", "ratwing_v1_rigged.fbx"), os.path.join(S, "ratwing", "ratwing_v1.glb")], "body", 20),
}

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def scene_setup():
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = SAMPLES
    try:
        sc.cycles.use_denoising = True
    except Exception:
        pass
    sc.render.resolution_x, sc.render.resolution_y = W, H
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    sc.render.dither_intensity = 0.0
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.look = "None"
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGB"
    sc.render.image_settings.color_depth = "8"
    world = bpy.data.worlds.new("W")
    sc.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.02, 0.02, 0.02, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.4
    return sc

def import_model(path):
    before = set(bpy.data.objects)
    ext = os.path.splitext(path)[1].lower()
    if ext == ".fbx":
        bpy.ops.import_scene.fbx(filepath=path)
    else:
        bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    for o in new:
        if o.type == "ARMATURE":
            if o.animation_data:
                o.animation_data.action = None
            o.data.pose_position = "REST"
    return new

def gray_material(mat):
    """Keep the model's own (grayscale) texture if it has one; plain satin gray otherwise."""
    if not mat.use_nodes:
        mat.use_nodes = True
    nt = mat.node_tree
    bsdf = next((n for n in nt.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf is None:
        nt.nodes.clear()
        bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
        outn = nt.nodes.new("ShaderNodeOutputMaterial")
        nt.links.new(bsdf.outputs[0], outn.inputs[0])
    has_tex = any(l.to_socket == bsdf.inputs["Base Color"] for l in nt.links)
    if not has_tex:
        bsdf.inputs["Base Color"].default_value = (0.16, 0.16, 0.16, 1)
    else:
        # darker foreground figure (Kyle's look: dark silhouettes against the glowing distance)
        link = next(l for l in nt.links if l.to_socket == bsdf.inputs["Base Color"])
        src = link.from_socket
        nt.links.remove(link)
        mul = nt.nodes.new("ShaderNodeMixRGB")
        mul.blend_type = "MULTIPLY"
        mul.inputs[0].default_value = 1.0
        mul.inputs[2].default_value = (0.5, 0.5, 0.5, 1)
        nt.links.new(src, mul.inputs[1])
        nt.links.new(mul.outputs[0], bsdf.inputs["Base Color"])
    for name in ("Emission Strength",):
        if name in bsdf.inputs:
            bsdf.inputs[name].default_value = 0.0
    bsdf.inputs["Roughness"].default_value = max(0.45, bsdf.inputs["Roughness"].default_value)

def world_bounds(objs):
    dg = bpy.context.evaluated_depsgraph_get()
    pts = []
    for o in objs:
        if o.type != "MESH":
            continue
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        n = len(me.vertices)
        if n:
            co = np.empty(n * 3, dtype=np.float32)
            me.vertices.foreach_get("co", co)
            co = co.reshape(-1, 3)
            M = np.array(ev.matrix_world)
            co = co @ M[:3, :3].T + M[:3, 3]
            pts.append(co)
        ev.to_mesh_clear()
    if not pts:
        return None
    P = np.concatenate(pts)
    return P

def area(name, loc, target, size, energy):
    d = bpy.data.lights.new(name, "AREA")
    d.shape = "DISK"
    d.size = size
    d.energy = energy
    d.color = (1, 1, 1)
    o = bpy.data.objects.new(name, d)
    bpy.context.scene.collection.objects.link(o)
    o.location = loc
    look(o, target)
    return o

def look(o, target):
    direction = Vector(target) - o.location
    o.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()

def glow_backdrop(centre, dist, scale):
    """A large emissive card behind the figure: bright misty centre fading to dark edges (Kyle's glowing distance)."""
    bpy.ops.mesh.primitive_plane_add(size=1)
    p = bpy.context.active_object
    p.name = "Backdrop"
    p.scale = (scale * 3.0, scale * 3.0, 1)
    p.location = (centre.x, centre.y + dist, centre.z)
    p.rotation_euler = (math.radians(90), 0, 0)
    m = bpy.data.materials.new("Glow")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    tc = nt.nodes.new("ShaderNodeTexCoord")
    grad = nt.nodes.new("ShaderNodeTexGradient")
    grad.gradient_type = "SPHERICAL"
    mapn = nt.nodes.new("ShaderNodeMapping")
    mapn.inputs["Location"].default_value = (0, 0, 0)
    mapn.inputs["Scale"].default_value = (2.1, 1.9, 1)
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.0
    ramp.color_ramp.elements[0].color = (0.015, 0.015, 0.015, 1)
    ramp.color_ramp.elements[1].position = 0.85
    ramp.color_ramp.elements[1].color = (0.85, 0.85, 0.85, 1)
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Strength"].default_value = 1.0
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    # spherical gradient is 1 at the centre of the mapped coords: shift the UV so the centre is the card's upper middle
    sub = nt.nodes.new("ShaderNodeVectorMath")
    sub.operation = "SUBTRACT"
    sub.inputs[1].default_value = (0.5, 0.58, 0)
    nt.links.new(tc.outputs["UV"], sub.inputs[0])
    nt.links.new(sub.outputs[0], mapn.inputs["Vector"])
    nt.links.new(mapn.outputs[0], grad.inputs["Vector"])
    nt.links.new(grad.outputs["Fac"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], em.inputs["Color"])
    nt.links.new(em.outputs[0], out.inputs[0])
    p.data.materials.append(m)
    try:
        p.visible_shadow = False
    except Exception:
        pass
    return p

def ground_fog(sc, cam_dist):
    """A little depth fog: the compositor mixes the mist pass toward a pale gray (the far part glows, the figure stays dark)."""
    sc.view_layers[0].use_pass_mist = True
    sc.world.mist_settings.start = cam_dist * 0.9
    sc.world.mist_settings.depth = cam_dist * 2.5
    sc.use_nodes = True
    nt = sc.node_tree
    nt.nodes.clear()
    rl = nt.nodes.new("CompositorNodeRLayers")
    mix = nt.nodes.new("CompositorNodeMixRGB")
    mix.blend_type = "MIX"
    mix.inputs[2].default_value = (0.6, 0.6, 0.6, 1)
    mul = nt.nodes.new("CompositorNodeMath")
    mul.operation = "MULTIPLY"
    mul.inputs[1].default_value = 0.15
    bw = nt.nodes.new("CompositorNodeRGBToBW")
    comp = nt.nodes.new("CompositorNodeComposite")
    nt.links.new(rl.outputs["Mist"], mul.inputs[0])
    nt.links.new(mul.outputs[0], mix.inputs[0])
    nt.links.new(rl.outputs["Image"], mix.inputs[1])
    nt.links.new(mix.outputs[0], bw.inputs[0])
    nt.links.new(bw.outputs[0], comp.inputs["Image"])

def to_gray_png(path):
    """Strict grayscale: R = G = B on every pixel (and alpha 1). Returns the max channel difference after writing."""
    img = bpy.data.images.load(path, check_existing=False)
    n = img.size[0] * img.size[1]
    px = np.empty(n * 4, dtype=np.float32)
    img.pixels.foreach_get(px)
    px = px.reshape(-1, 4)
    g = np.round((px[:, 0] * 0.2126 + px[:, 1] * 0.7152 + px[:, 2] * 0.0722) * 255.0) / 255.0
    px[:, 0] = g; px[:, 1] = g; px[:, 2] = g; px[:, 3] = 1.0
    img.pixels.foreach_set(px.ravel())
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)
    chk = bpy.data.images.load(path, check_existing=False)
    q = np.empty(n * 4, dtype=np.float32)
    chk.pixels.foreach_get(q)
    q = q.reshape(-1, 4)
    diff = float(max(np.abs(q[:, 0] - q[:, 1]).max(), np.abs(q[:, 1] - q[:, 2]).max())) * 255.0
    bpy.data.images.remove(chk)
    return diff

def placeholder(key, path):
    reset()
    sc = scene_setup()
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    cam.location = (0, -4, 1)
    look(cam, (0, 0, 1))
    glow_backdrop(Vector((0, -0.5, 1)), 0.5, 2.5)
    sc.cycles.samples = 8
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    d = to_gray_png(path)
    log(f"{key}: PLACEHOLDER (no model) -> {path} (max |R-G|,|G-B| = {d:.0f})")

def render(key, files, framing, yaw):
    path = os.path.join(OUT, key + ".png")
    src = next((f for f in files if os.path.exists(f)), None)
    if src is None:
        placeholder(key, path)
        return
    reset()
    sc = scene_setup()
    try:
        objs = import_model(src)
    except Exception as ex:
        log(f"{key}: import failed ({ex}) -> placeholder")
        placeholder(key, path)
        return
    meshes = [o for o in objs if o.type == "MESH"]
    if not meshes:
        placeholder(key, path)
        return
    for o in meshes:
        for slot in o.material_slots:
            if slot.material:
                gray_material(slot.material)
        if not o.material_slots:
            m = bpy.data.materials.new("Gray")
            gray_material(m)
            o.data.materials.append(m)
    # turn the whole model (roots only) by yaw about Z, around the origin
    rot = Matrix.Rotation(math.radians(yaw), 4, "Z")
    for o in objs:
        if o.parent is None:
            o.matrix_world = rot @ o.matrix_world
    bpy.context.view_layer.update()
    P = world_bounds(meshes)
    lo, hi = P.min(axis=0), P.max(axis=0)
    size = hi - lo
    height = size[2]
    # framing box (vertical range of the figure shown)
    if framing == "head":
        z0, z1 = hi[2] - height * 0.30, hi[2] + height * 0.03
    elif framing == "bust":
        z0, z1 = hi[2] - height * 0.55, hi[2] + height * 0.04
    else:
        z0, z1 = lo[2] - height * 0.04, hi[2] + height * 0.06
    band = P[(P[:, 2] >= z0) & (P[:, 2] <= z1)]
    if len(band) == 0:
        band = P
    cx = float((band[:, 0].min() + band[:, 0].max()) * 0.5)
    cy = float((band[:, 1].min() + band[:, 1].max()) * 0.5)
    bw = float(band[:, 0].max() - band[:, 0].min())
    fh = (z1 - z0)
    cz = (z0 + z1) * 0.5
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.lens = 70
    cam = bpy.data.objects.new("Cam", cam_data)
    sc.collection.objects.link(cam)
    sc.camera = cam
    # vertical fov fits the band height, horizontal the band width (sensor fit auto: the long side = height here)
    cam_data.sensor_fit = "VERTICAL"
    vfov = 2 * math.atan(cam_data.sensor_height * 0.5 / cam_data.lens)
    hfov = 2 * math.atan(math.tan(vfov / 2) * W / H)
    need = max((fh * 0.5) / math.tan(vfov / 2), (bw * 0.55) / math.tan(hfov / 2))
    front = float(band[:, 1].min())
    dist = need + (cy - front)
    cam.location = (cx, cy - dist, cz + fh * 0.05)
    look(cam, (cx, cy, cz))
    target = Vector((cx, cy, cz))
    span = max(fh, bw)
    # Kyle's look: soft key from the front-left above, white rim from behind both sides, faint fill, glowing distance
    area("Key", (cx - span * 1.2, cy - span * 1.6, cz + span * 1.0), target, span * 1.5, 90 * span * span)
    area("RimL", (cx - span * 1.1, cy + span * 1.3, cz + span * 0.6), target, span * 0.8, 380 * span * span)
    area("RimR", (cx + span * 1.2, cy + span * 1.2, cz + span * 0.4), target, span * 0.8, 300 * span * span)
    area("Fill", (cx + span * 1.5, cy - span * 1.0, cz - span * 0.1), target, span * 2.0, 12 * span * span)
    glow_backdrop(Vector((cx, cy, cz)), max(size[1], span) * 1.6, span * 1.4)
    ground_fog(sc, dist)
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    d = to_gray_png(path)
    log(f"{key}: {os.path.relpath(src, REPO)} framing {framing} yaw {yaw} -> {path} "
        f"(model {size[0]:.2f} x {size[1]:.2f} x {size[2]:.2f} m, max |R-G|,|G-B| = {d:.0f})")

for key, (files, framing, yaw) in PORTRAITS.items():
    if ONLY and key not in ONLY:
        continue
    try:
        render(key, files, framing, yaw)
    except Exception as ex:
        import traceback
        traceback.print_exc()
        log(f"{key}: FAILED ({ex}) -> placeholder")
        try:
            placeholder(key, os.path.join(OUT, key + ".png"))
        except Exception as ex2:
            log(f"{key}: placeholder failed too ({ex2})")
log("done")
