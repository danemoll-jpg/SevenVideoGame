using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using A = Seven.AsteroidLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 16 — the asteroid's level geometry: the Great Machine Age (riveted, steam-age: wheels, pistons, rails, gauges),
    /// after the vision's look and Kyle's fog / glow mood. Pieces are built from a small kit - riveted plate (a chamfered
    /// slab with rivet rows), girders, gear wheels, pipes, turned lamps - and saved per piece (the cameras cut away only the
    /// piece in their way; walkable pieces are named Deck / Floor / Platform so they are never cut away). Never a plain box.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        struct AsMats { public Material metal, metalDark, steel, plate, floor, rock, rockDark, glow, glowSoft, cable, panel, glass, beam, stone; }

        /// <summary>Accumulates triangles per material; Save writes one mesh per material (named prefix_material).</summary>
        class AK
        {
            public readonly Dictionary<Material, (List<Vector3> v, List<int> t)> parts = new Dictionary<Material, (List<Vector3>, List<int>)>();
            public (List<Vector3> v, List<int> t) Of(Material m) { if (!parts.TryGetValue(m, out var p)) { p = (new List<Vector3>(), new List<int>()); parts[m] = p; } return p; }
            public List<GameObject> Save(string prefix, Transform parent, bool smooth = false, bool shadows = true)
            {
                var made = new List<GameObject>();
                foreach (var kv in parts)
                {
                    if (kv.Value.v.Count == 0) continue;
                    var mesh = smooth ? SmoothFinish(kv.Value.v, kv.Value.t) : Finish(kv.Value.v, kv.Value.t);
                    made.Add(MeshObject($"{prefix}_{kv.Key.name}", SaveMesh(mesh, $"Ast_{prefix}_{kv.Key.name}"), kv.Key, parent, shadows));
                }
                parts.Clear();
                return made;
            }
        }

        // ---------------------------------------------------------------- the kit
        /// <summary>Riveted plate: a chamfered slab (MBox, no batter) with a row of rivets along its top edges.</summary>
        static void Riveted(AK k, Material m, Material rivets, Vector3 c, Vector3 size, float yaw = 0f, float pitch = 4f)
        {
            var (v, t) = k.Of(m);
            MBox(v, t, c, size, yaw, 0.004f);
            if (rivets == null) return;
            var (rv, rt) = k.Of(rivets);
            var q = Quaternion.Euler(0f, yaw, 0f);
            float hx = size.x * 0.5f - 0.12f, hz = size.z * 0.5f - 0.12f, y = size.y * 0.5f;
            int nx = Mathf.Clamp(Mathf.RoundToInt(size.x * pitch / 4f), 2, 24), nz = Mathf.Clamp(Mathf.RoundToInt(size.z * pitch / 4f), 2, 24);
            float rr = Mathf.Clamp(Mathf.Min(size.x, size.z) * 0.012f, 0.025f, 0.07f);
            for (int i = 0; i <= nx; i++) foreach (var sz in new[] { -1f, 1f }) Ellipsoid(rv, rt, c + q * new Vector3(Mathf.Lerp(-hx, hx, i / (float)nx), y, hz * sz), Vector3.one * rr, 3, 6);
            for (int i = 1; i < nz; i++) foreach (var sx in new[] { -1f, 1f }) Ellipsoid(rv, rt, c + q * new Vector3(hx * sx, y, Mathf.Lerp(-hz, hz, i / (float)nz)), Vector3.one * rr, 3, 6);
        }
        /// <summary>A wall of riveted plates between two points (vertical), height h, thickness th.</summary>
        static void PlateWall(AK k, Material m, Material rivets, Vector3 a, Vector3 b, float y0, float h, float th = 0.6f, float plateW = 4f)
        {
            var d = b - a; d.y = 0f; float len = d.magnitude; if (len < 0.1f) return;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg - 90f;
            int n = Mathf.Max(1, Mathf.RoundToInt(len / plateW)); float w = len / n;
            int rows = Mathf.Max(1, Mathf.RoundToInt(h / 3f)); float rh = h / rows;
            for (int i = 0; i < n; i++)
                for (int r = 0; r < rows; r++)
                {
                    var c = a + d.normalized * (w * (i + 0.5f)); c.y = y0 + rh * (r + 0.5f);
                    // plates rotated to stand vertical: x along the wall, y through the wall (thickness), z up
                    var (v, t) = k.Of(m);
                    MBox(v, t, c, new Vector3(w - 0.04f, rh - 0.04f, th), yaw, 0.004f);
                    if (rivets != null && (i + r) % 2 == 0) { var (rv, rt) = k.Of(rivets); var q = Quaternion.Euler(0f, yaw, 0f); for (int j = 0; j < 4; j++) Ellipsoid(rv, rt, c + q * new Vector3(Mathf.Lerp(-w * 0.42f, w * 0.42f, j / 3f), rh * 0.42f, th * 0.52f), Vector3.one * 0.05f, 3, 6); }
                }
        }
        /// <summary>A gear wheel (disc + rim + teeth + hub + spokes) in the plane normal to axis, as its own GameObject.</summary>
        static GameObject Gear(string name, Vector3 at, Vector3 axis, float r, float th, int teeth, Material m, Transform parent)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            int seg = Mathf.Max(16, teeth * 2);
            var rim = Lathe(new[] { new Vector2(r * 0.78f, -th * 0.5f), new Vector2(r, -th * 0.5f), new Vector2(r, th * 0.5f), new Vector2(r * 0.78f, th * 0.5f), new Vector2(r * 0.78f, -th * 0.5f) }, 0f, 360f, seg);
            Append(v, t, rim);
            var hub = Lathe(new[] { new Vector2(0f, -th * 0.8f), new Vector2(r * 0.16f, -th * 0.8f), new Vector2(r * 0.16f, th * 0.8f), new Vector2(0f, th * 0.8f) }, 0f, 360f, 12);
            Append(v, t, hub);
            for (int s = 0; s < 6; s++) { float a = s * 60f; MBox(v, t, Quaternion.Euler(0f, a, 0f) * new Vector3(r * 0.47f, 0f, 0f), new Vector3(r * 0.62f, th * 0.6f, r * 0.08f), a, 0f); }
            for (int i = 0; i < teeth; i++) { float a = i * 360f / teeth; MBox(v, t, Quaternion.Euler(0f, -a, 0f) * new Vector3(r + r * 0.06f, 0f, 0f), new Vector3(r * 0.13f, th, r * 0.09f), -a, 0f); }
            var go = MeshObject(name, SaveMesh(Finish(v, t), "Ast_" + name), m, parent);
            go.transform.position = at; go.transform.rotation = Quaternion.FromToRotation(Vector3.up, axis.normalized);
            return go;
        }
        static void Append(List<Vector3> v, List<int> t, Mesh m) { int b = v.Count; v.AddRange(m.vertices); t.AddRange(m.triangles.Select(i => i + b)); }
        /// <summary>A turned lamp: a glowing body under a small dark cap, with its light.</summary>
        static Light AstLamp(string name, Vector3 at, float range, float intensity, Material glow, Material cap, Transform parent, float r = 0.22f)
        {
            MeshObject(name + "_Glow", SaveMesh(Lathe(new[] { new Vector2(0f, -r), new Vector2(r * 0.65f, -r * 0.85f), new Vector2(r, -r * 0.1f), new Vector2(r * 0.8f, r * 0.55f), new Vector2(0f, r * 0.62f) }, 0f, 360f, 12, null, at), "Ast_" + name + "_Glow"), glow, parent, false);
            MeshObject(name + "_Cap", SaveMesh(Lathe(new[] { new Vector2(0f, r * 0.6f), new Vector2(r * 1.15f, r * 0.55f), new Vector2(r * 0.9f, r * 0.95f), new Vector2(0f, r * 1.05f) }, 0f, 360f, 12, null, at), "Ast_" + name + "_Cap"), cap, parent);
            return PointLight(name + "_Light", at + Vector3.down * r * 1.4f, range, intensity, parent);
        }
        /// <summary>A pipe along points.</summary>
        static void Pipe(AK k, Material m, IList<Vector3> pts, float r) { var (v, t) = k.Of(m); Sweep(v, t, pts, r, false, 8); }
        /// <summary>A column (turned, with a base and a capital) from y0 to y1.</summary>
        static void Column(AK k, Material m, Vector3 at, float y0, float y1, float r)
        {
            var (v, t) = k.Of(m);
            float h = y1 - y0;
            Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(r * 1.5f, 0f), new Vector2(r * 1.45f, Mathf.Min(0.6f, h * 0.05f)), new Vector2(r, Mathf.Min(0.9f, h * 0.08f)), new Vector2(r, h - Mathf.Min(0.9f, h * 0.08f)), new Vector2(r * 1.4f, h - 0.3f), new Vector2(r * 1.45f, h), new Vector2(0f, h) }, 0f, 360f, 12, null, new Vector3(at.x, y0, at.z)));
        }
        /// <summary>A light ribbon run (route lights, power lines): n glowing segments as separate renderers (PowerLine).</summary>
        static Seven.PowerLine LightRun(string name, IList<Vector3> pts, int n, float r, Material glowBase, Transform parent)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false);
            var pl = root.gameObject.AddComponent<Seven.PowerLine>();
            var segs = new List<Renderer>(); var points = new List<Vector3>();
            float total = 0f; for (int i = 0; i + 1 < pts.Count; i++) total += Vector3.Distance(pts[i], pts[i + 1]);
            Vector3 At(float s) { for (int i = 0; i + 1 < pts.Count; i++) { float l = Vector3.Distance(pts[i], pts[i + 1]); if (s <= l) return Vector3.Lerp(pts[i], pts[i + 1], s / Mathf.Max(0.001f, l)); s -= l; } return pts[pts.Count - 1]; }
            for (int i = 0; i < n; i++)
            {
                float s0 = total * i / n, s1 = total * (i + 0.82f) / n;
                var v = new List<Vector3>(); var t = new List<int>();
                Sweep(v, t, new[] { At(s0), At(s1) }, r, false, 6);
                var mat = new Material(glowBase) { name = glowBase.name + "_" + name + i };
                var go = MeshObject($"{name}_{i}", SaveMesh(Finish(v, t), $"Ast_{name}_{i}"), mat, root, false);
                segs.Add(go.GetComponent<Renderer>()); points.Add(At((s0 + s1) * 0.5f));
            }
            pl.Segs = segs.ToArray(); pl.Points = points.ToArray();
            return pl;
        }

        // ---------------------------------------------------------------- the receiving chamber (AST_02)
        class RcRefs { public Seven.AstScanner scanner; public Seven.AstMaintBot bot; public Transform container; public Renderer marks, doorLamp; public Transform doorWheel; public Seven.AstShutter door, outer; public Seven.PowerLine doorLine; public Renderer[] vents; }
        static RcRefs BuildReceiving(Transform world, AsMats m)
        {
            var r = new RcRefs();
            var rc = new GameObject("ReceivingChamber").transform; rc.SetParent(world, false);
            var k = new AK();
            MGround("Receiving_Floor", new Vector3(-18f, -1.5f, -18f), new Vector3(18f, 0f, 18f), m.floor, rc);
            // the floor's plating seams (dark strips) + the two marked pads (white rings, flush)
            for (int i = -3; i <= 3; i++) { var (v, t) = k.Of(m.metalDark); MBox(v, t, new Vector3(i * 5f, 0.005f, 0f), new Vector3(0.08f, 0.01f, 35f), 0f, 0f); }
            foreach (var (c, rr) in new[] { (A.SevenDown, 2.2f), (A.BikePad, 2.8f) })
            {
                var (v, t) = k.Of(m.glow);
                Append(v, t, Lathe(new[] { new Vector2(rr - 0.18f, 0.012f), new Vector2(rr, 0.012f), new Vector2(rr, 0.02f), new Vector2(rr - 0.18f, 0.02f) }, 0f, 360f, 40, null, c));
            }
            k.Save("Receiving_Pads", rc, false, false);
            // the walls: riveted plates, 22 m high; openings - the outer gate (south), the service entrance (west), sorting (east)
            PlateWall(k, m.plate, m.metalDark, new Vector3(-18.3f, 0f, -18.3f), new Vector3(-12f, 0f, -18.3f), 0f, 22f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(12f, 0f, -18.3f), new Vector3(18.3f, 0f, -18.3f), 0f, 22f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-12f, 0f, -18.3f), new Vector3(12f, 0f, -18.3f), 18f, 4f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-18.3f, 0f, -18.3f), new Vector3(-18.3f, 0f, -7f), 0f, 22f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-18.3f, 0f, -7f), new Vector3(-18.3f, 0f, -1f), 5.2f, 16.8f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-18.3f, 0f, -1f), new Vector3(-18.3f, 0f, 18.3f), 0f, 22f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-18.3f, 0f, 18.3f), new Vector3(18.3f, 0f, 18.3f), 0f, 22f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(18.3f, 0f, 18.3f), new Vector3(18.3f, 0f, 14.6f), 0f, 22f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(18.3f, 0f, 14.6f), new Vector3(18.3f, 0f, 11.4f), 3.2f, 18.8f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(18.3f, 0f, 11.4f), new Vector3(18.3f, 0f, -18.3f), 0f, 22f);
            k.Save("Receiving_Wall", rc);
            // the ceiling: radial girders under a dark dome of plates, round lamps
            for (int i = 0; i < 12; i++) { float a = i * 30f; var (v, t) = k.Of(m.metalDark); MBox(v, t, Polar(9f, a, 21.2f), new Vector3(18f, 1.2f, 0.6f), -a, 0f); }
            { var (v, t) = k.Of(m.plate); Append(v, t, Lathe(new[] { new Vector2(0f, 26f), new Vector2(10f, 25f), new Vector2(19f, 22f), new Vector2(26f, 21.8f) }, 0f, 360f, 24)); }
            k.Save("Receiving_Ceiling", rc);
            foreach (var a in new[] { 45f, 135f, 225f, 315f }) AstLamp("RcLamp" + a, Polar(11f, a, 19f), 30f, 2.2f, m.glow, m.metalDark, rc, 0.4f);
            // the outer gate (a riveted shutter across the south opening, open at first - it closes behind the saucer)
            {
                var gp = new GameObject("OuterGate").transform; gp.SetParent(rc, false); gp.localPosition = Vector3.zero;
                PlateWall(k, m.plate, m.metalDark, new Vector3(-12f, 0f, -18.6f), new Vector3(12f, 0f, -18.6f), 0f, 18f, 0.5f, 6f);
                foreach (var g in k.Save("OuterGate_Plates", gp)) g.transform.localRotation = Quaternion.identity;
                r.outer = gp.gameObject.AddComponent<Seven.AstShutter>(); r.outer.Drop = 18.5f;
            }
            // the service entrance: a riveted roller door in the west opening + the manual door control (a wheel on a post)
            {
                var dp = new GameObject("ServiceDoor").transform; dp.SetParent(rc, false);
                PlateWall(k, m.plate, m.metalDark, new Vector3(-18.3f, 0f, -7f), new Vector3(-18.3f, 0f, -1f), 0f, 5.2f, 0.4f, 3f);
                k.Save("ServiceDoor_Slats", dp);
                r.door = dp.gameObject.AddComponent<Seven.AstShutter>(); r.door.Drop = 5.0f;
                var post = new AK(); Column(post, m.metalDark, A.DoorControl, 0f, 1.3f, 0.09f); post.Save("DoorControl_Post", rc);
                var wheel = new GameObject("DoorWheel").transform; wheel.SetParent(rc, false); wheel.position = A.DoorControl + Vector3.up * 1.32f; wheel.rotation = Quaternion.LookRotation(Vector3.right);
                var wv = new List<Vector3>(); var wt = new List<int>();
                Sweep(wv, wt, Enumerable.Range(0, 25).Select(i => new Vector3(Mathf.Cos(i / 24f * Mathf.PI * 2f) * 0.38f, Mathf.Sin(i / 24f * Mathf.PI * 2f) * 0.38f, 0f)).ToList(), 0.04f, true, 6);
                for (int s = 0; s < 4; s++) { float a = s * 45f * Mathf.Deg2Rad; Sweep(wv, wt, new[] { new Vector3(Mathf.Cos(a) * 0.38f, Mathf.Sin(a) * 0.38f, 0f), new Vector3(-Mathf.Cos(a) * 0.38f, -Mathf.Sin(a) * 0.38f, 0f) }, 0.025f, false, 5); }
                MeshObject("DoorWheel_Rim", SaveMesh(SmoothFinish(wv, wt), "Ast_DoorWheel"), m.steel, wheel).transform.localPosition = Vector3.zero;
                r.doorWheel = wheel;
                var lamp = new List<Vector3>(); var lt = new List<int>(); Ellipsoid(lamp, lt, A.DoorControl + Vector3.up * 1.85f, Vector3.one * 0.11f, 6, 10);
                r.doorLamp = MeshObject("DoorControl_Lamp", SaveMesh(SmoothFinish(lamp, lt), "Ast_DoorLamp"), new Material(m.glowSoft) { name = "Ast_doorlamp" }, rc, false).GetComponent<Renderer>();
                r.doorLine = LightRun("DoorFrameLight", new[] { new Vector3(-17.9f, 0.2f, -7.3f), new Vector3(-17.9f, 5.4f, -7.3f), new Vector3(-17.9f, 5.4f, -0.7f), new Vector3(-17.9f, 0.2f, -0.7f) }, 10, 0.05f, m.glowSoft, rc);
            }
            // the cargo conveyor along the north wall into the sorting slot (rollers, side rails) + the marked container
            {
                var cv = new AK();
                for (float x = A.ConveyorX0; x <= A.ConveyorX1; x += 0.8f) { var (v, t) = cv.Of(m.steel); Append(v, t, RotX(Lathe(new[] { new Vector2(0f, -1.2f), new Vector2(0.14f, -1.2f), new Vector2(0.14f, 1.2f), new Vector2(0f, 1.2f) }, 0f, 360f, 10), new Vector3(x, 0.45f, A.ConveyorZ))); }
                foreach (var sz in new[] { -1.35f, 1.35f }) Riveted(cv, m.metalDark, null, new Vector3((A.ConveyorX0 + A.ConveyorX1) * 0.5f, 0.3f, A.ConveyorZ + sz), new Vector3(A.ConveyorX1 - A.ConveyorX0, 0.6f, 0.2f));
                cv.Save("Conveyor_Belt", rc);
                var ct = new GameObject("CargoContainer").transform; ct.SetParent(rc, false);
                var cvv = new List<Vector3>(); var ctt = new List<int>();
                Append(cvv, ctt, RotZ(Lathe(new[] { new Vector2(0f, -1.6f), new Vector2(0.9f, -1.6f), new Vector2(0.95f, -1.45f), new Vector2(0.95f, 1.45f), new Vector2(0.9f, 1.6f), new Vector2(0f, 1.6f) }, 0f, 360f, 16), new Vector3(0f, 0.95f, 0f)));
                for (int i = -2; i <= 2; i++) Append(cvv, ctt, RotZ(Lathe(new[] { new Vector2(0.94f, -0.06f), new Vector2(1.0f, -0.06f), new Vector2(1.0f, 0.06f), new Vector2(0.94f, 0.06f) }, 0f, 360f, 16), new Vector3(i * 0.65f, 0.95f, 0f)));
                MeshObject("Container_Body", SaveMesh(Finish(cvv, ctt), "Ast_Container"), m.metal, ct);
                var mv = new List<Vector3>(); var mt = new List<int>();
                for (int i = -1; i <= 1; i++) { var c0 = new Vector3(i * 0.9f, 1.92f, 0f); Quad(mv, mt, c0 + new Vector3(-0.3f, 0f, -0.35f), c0 + new Vector3(0.3f, 0f, 0f), c0 + new Vector3(0.18f, 0f, 0f), c0 + new Vector3(-0.42f, 0f, -0.35f)); Quad(mv, mt, c0 + new Vector3(-0.42f, 0f, 0.35f), c0 + new Vector3(0.18f, 0f, 0f), c0 + new Vector3(0.3f, 0f, 0f), c0 + new Vector3(-0.3f, 0f, 0.35f)); }
                r.marks = MeshObject("Container_Marks", SaveMesh(Finish(mv, mt), "Ast_ContainerMarks"), new Material(m.glowSoft) { name = "Ast_containermarks" }, ct, false).GetComponent<Renderer>();
                ct.position = A.ContainerAt + Vector3.up * 0.55f;
                r.container = ct;
                // the sorting slot: a dark mouth with hanging flaps
                var sk = new AK(); Riveted(sk, m.metalDark, m.metal, new Vector3(18.1f, 3.4f, A.ConveyorZ), new Vector3(0.5f, 0.4f, 3.4f));
                for (int i = 0; i < 6; i++) { var (v, t) = sk.Of(m.cable); MBox(v, t, new Vector3(18.0f, 1.7f, A.ConveyorZ - 1.4f + i * 0.56f), new Vector3(0.05f, 3.0f, 0.5f), 0f, 0f); }
                sk.Save("SortingSlot", rc);
            }
            // the receiving scanner (an articulated arm on the north wall) with its lamp head and cone of light
            {
                var sc = new GameObject("ReceivingScanner").transform; sc.SetParent(rc, false); sc.position = A.ScannerAt + new Vector3(0f, 0f, 0.6f);
                var ak = new AK(); Riveted(ak, m.metalDark, m.metal, sc.position + new Vector3(0f, 0f, 0.5f), new Vector3(2.2f, 2.2f, 0.4f));
                Pipe(ak, m.steel, new[] { sc.position + new Vector3(0f, 0f, 0.3f), sc.position + new Vector3(0f, -0.4f, -0.6f) }, 0.16f);
                ak.Save("Scanner_Mount", rc);
                var head = new GameObject("ScannerHead").transform; head.SetParent(sc, false); head.position = A.ScannerAt;
                MeshObject("ScannerHead_Body", SaveMesh(RotX(Lathe(new[] { new Vector2(0f, -0.5f), new Vector2(0.35f, -0.45f), new Vector2(0.42f, 0.1f), new Vector2(0.48f, 0.42f), new Vector2(0.0f, 0.42f) }, 0f, 360f, 14), Vector3.zero), "Ast_ScannerHead"), m.metal, head).transform.localPosition = Vector3.zero;
                var lens = new List<Vector3>(); var lt = new List<int>(); Ellipsoid(lens, lt, new Vector3(0f, 0f, 0.44f), new Vector3(0.36f, 0.36f, 0.06f), 6, 12);
                MeshObject("ScannerHead_Lens", SaveMesh(SmoothFinish(lens, lt), "Ast_ScannerLens"), m.glow, head, false).transform.localPosition = Vector3.zero;
                // the cone: unit length along +z (scaled to the target each frame)
                var cv = new List<Vector3>(); var ctt = new List<int>();
                for (int i = 0; i < 24; i++) { float a0 = i * 15f * Mathf.Deg2Rad, a1 = (i + 1) * 15f * Mathf.Deg2Rad; Quad(cv, ctt, new Vector3(Mathf.Cos(a0) * 0.3f, Mathf.Sin(a0) * 0.3f, 0f), new Vector3(Mathf.Cos(a0) * 0.75f, Mathf.Sin(a0) * 0.75f, 1f), new Vector3(Mathf.Cos(a1) * 0.75f, Mathf.Sin(a1) * 0.75f, 1f), new Vector3(Mathf.Cos(a1) * 0.3f, Mathf.Sin(a1) * 0.3f, 0f)); }
                var coneGo = MeshObject("ScannerCone", SaveMesh(Finish(cv, ctt), "Ast_ScannerCone"), AdditiveMat("Ast_scanner_cone", 0.14f), head, false);
                coneGo.transform.localPosition = new Vector3(0f, 0f, 0.45f);
                var spot = new GameObject("ScannerSpot").AddComponent<Light>(); spot.transform.SetParent(head, false); spot.type = LightType.Spot; spot.spotAngle = 22f; spot.range = 30f; spot.intensity = 2.2f; spot.color = Color.white; spot.shadows = LightShadows.None;
                r.scanner = sc.gameObject.AddComponent<Seven.AstScanner>(); r.scanner.Head = head; r.scanner.Cone = coneGo.GetComponent<Renderer>(); r.scanner.Spot = spot; r.scanner.TargetPoint = A.SevenDown;
            }
            // QF21 (2): "System" - the receiving chamber's maintenance machine, first contact with the Great Machine Age:
            // a squat rounded brass-and-steel body trundling on three jointed spindly legs, one big round lens-eye with an
            // iris shutter (dilates / narrows as it speaks and scans), a speaker grille under it, a fan of scanning light
            // from the lens, a little projector for the guiding symbol, a steam stack that whistles on each announcement,
            // gauges and a ticking dial on its back. Brass speaking-horns in the chamber walls glow with its words.
            {
                var mb = new GameObject("MaintenanceMachine").transform; mb.SetParent(rc, false); mb.position = A.MaintBotHome;
                var bodyT = new GameObject("Maint_BodyPivot").transform; bodyT.SetParent(mb, false); bodyT.localPosition = Vector3.zero;
                var bk = new AK();
                {
                    var (v, t) = bk.Of(m.metal);
                    Append(v, t, Lathe(new[] { new Vector2(0f, 0.62f), new Vector2(0.38f, 0.64f), new Vector2(0.55f, 0.78f), new Vector2(0.62f, 1.0f), new Vector2(0.58f, 1.22f), new Vector2(0.44f, 1.36f), new Vector2(0f, 1.4f) }, 0f, 360f, 22));
                    var (bv2, bt2) = bk.Of(m.metalDark);
                    foreach (var y in new[] { 0.8f, 1.18f }) Append(bv2, bt2, Lathe(new[] { new Vector2(0.62f, y - 0.035f), new Vector2(0.64f, y), new Vector2(0.62f, y + 0.035f) }, 0f, 360f, 22));
                    for (int i = 0; i < 18; i++) Ellipsoid(bv2, bt2, Quaternion.Euler(0f, i * 20f, 0f) * new Vector3(0.63f, 1.0f, 0f), Vector3.one * 0.022f, 3, 6);
                    // the back: two gauges and the dial plate; the steam stack; the projector barrel under the chin
                    var (sv, st) = bk.Of(m.steel);
                    foreach (var gx in new[] { -0.2f, 0.2f }) Append(sv, st, RotX(Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.11f, 0f), new Vector2(0.11f, 0.04f), new Vector2(0f, 0.04f) }, 0f, 360f, 14), new Vector3(gx, 1.15f, -0.6f)));
                    Append(sv, st, RotX(Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.17f, 0f), new Vector2(0.17f, 0.05f), new Vector2(0f, 0.05f) }, 0f, 360f, 16), new Vector3(0f, 0.88f, -0.58f)));
                    Append(sv, st, Lathe(new[] { new Vector2(0f, 1.3f), new Vector2(0.07f, 1.3f), new Vector2(0.07f, 1.62f), new Vector2(0.1f, 1.66f), new Vector2(0.1f, 1.72f), new Vector2(0f, 1.72f) }, 0f, 360f, 10, null, new Vector3(-0.22f, 0f, -0.2f)));
                    Append(sv, st, RotX(Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.06f, 0f), new Vector2(0.08f, 0.16f), new Vector2(0f, 0.16f) }, 0f, 360f, 10), new Vector3(0f, 0.72f, 0.48f)));
                }
                foreach (var g in bk.Save("Maint_Body", bodyT, true)) g.transform.localPosition = Vector3.zero;
                var dial = new GameObject("Maint_DialNeedle").transform; dial.SetParent(bodyT, false); dial.localPosition = new Vector3(0f, 0.88f, -0.64f); dial.localRotation = Quaternion.LookRotation(Vector3.back);
                { var nv = new List<Vector3>(); var nt = new List<int>(); MBox(nv, nt, new Vector3(0f, 0.06f, 0f), new Vector3(0.015f, 0.13f, 0.01f), 0f, 0f); MeshObject("Maint_DialNeedleMesh", SaveMesh(Finish(nv, nt), "Ast_MaintNeedle"), m.metalDark, dial, false).transform.localPosition = Vector3.zero; }
                // the head: the lens-eye in a brass ring, the iris shutter, the speaker grille - it tilts as a whole
                var headT = new GameObject("Maint_Head").transform; headT.SetParent(bodyT, false); headT.localPosition = new Vector3(0f, 1.05f, 0.42f);
                var hk = new AK();
                {
                    var (v, t) = hk.Of(m.metal); Append(v, t, RotX(Lathe(new[] { new Vector2(0.2f, -0.08f), new Vector2(0.36f, -0.1f), new Vector2(0.38f, 0.06f), new Vector2(0.3f, 0.12f), new Vector2(0.27f, 0.1f) }, 0f, 360f, 24), Vector3.zero));
                    var (gv, gt) = hk.Of(m.metalDark); for (int i = 0; i < 12; i++) Ellipsoid(gv, gt, Quaternion.Euler(0f, 0f, i * 30f) * new Vector3(0.33f, 0f, 0.1f), Vector3.one * 0.022f, 3, 6);
                }
                foreach (var g in hk.Save("Maint_HeadRing", headT, true)) g.transform.localPosition = Vector3.zero;
                var lensP = new GameObject("Maint_Lens").transform; lensP.SetParent(headT, false); lensP.localPosition = new Vector3(0f, 0f, 0.02f);
                var lv = new List<Vector3>(); var lt2 = new List<int>(); Ellipsoid(lv, lt2, Vector3.zero, new Vector3(0.24f, 0.24f, 0.07f), 6, 14);
                var lensGo = MeshObject("Maint_LensGlow", SaveMesh(SmoothFinish(lv, lt2), "Ast_MaintLens"), new Material(m.glow) { name = "Ast_maintlens" }, lensP, false); lensGo.transform.localPosition = Vector3.zero;
                var iris = new List<Transform>();
                for (int i = 0; i < 8; i++)
                {
                    var b = new GameObject("Maint_IrisBlade" + i).transform; b.SetParent(headT, false); b.localPosition = new Vector3(0f, 0f, 0.085f); b.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
                    var iv = new List<Vector3>(); var it = new List<int>();
                    Quad(iv, it, new Vector3(0.05f, 0.0f, 0f), new Vector3(0.05f, 0.27f, 0f), new Vector3(0.27f, 0.27f, 0f), new Vector3(0.24f, 0.0f, 0f));
                    Quad(iv, it, new Vector3(0.24f, 0.0f, 0.004f), new Vector3(0.27f, 0.27f, 0.004f), new Vector3(0.05f, 0.27f, 0.004f), new Vector3(0.05f, 0.0f, 0.004f));
                    MeshObject("Maint_IrisBladeMesh" + i, SaveMesh(Finish(iv, it), "Ast_MaintIris" + i), m.metalDark, b, false).transform.localPosition = Vector3.zero;
                    iris.Add(b);
                }
                var grv = new List<Vector3>(); var grt = new List<int>();
                for (int i = 0; i < 4; i++) MBox(grv, grt, new Vector3(0f, -0.2f - i * 0.045f, 0.06f), new Vector3(0.3f - i * 0.05f, 0.018f, 0.02f), 0f, 0f);
                var grille = MeshObject("Maint_Grille", SaveMesh(Finish(grv, grt), "Ast_MaintGrille"), new Material(m.glowSoft) { name = "Ast_maintgrille" }, headT, false); grille.transform.localPosition = Vector3.zero;
                // the scanning fan: a flat wedge of light from the lens (unit length along +z; swept up and down by the runtime)
                var fanP = new GameObject("Maint_ScanFan").transform; fanP.SetParent(headT, false); fanP.localPosition = new Vector3(0f, 0f, 0.1f);
                var fv = new List<Vector3>(); var ft = new List<int>();
                for (int i = 0; i < 12; i++)
                {
                    float a0 = Mathf.Lerp(-22f, 22f, i / 12f) * Mathf.Deg2Rad, a1 = Mathf.Lerp(-22f, 22f, (i + 1) / 12f) * Mathf.Deg2Rad;
                    Quad(fv, ft, Vector3.zero, new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)), new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1)), Vector3.zero);
                    Quad(fv, ft, Vector3.zero, new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1)), new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)), Vector3.zero);
                }
                var fan = MeshObject("Maint_ScanFanMesh", SaveMesh(Finish(fv, ft), "Ast_MaintFan"), AdditiveMat("Ast_maint_fan", 0.12f), fanP, false); fan.transform.localPosition = Vector3.zero; fanP.gameObject.SetActive(false);
                // three jointed spindly legs: hip (swing) -> thigh -> knee (bend) -> shin -> a round foot
                var hips = new List<Transform>(); var knees = new List<Transform>();
                for (int i = 0; i < 3; i++)
                {
                    float yaw = 60f + i * 120f;
                    var hip = new GameObject("Maint_Hip" + i).transform; hip.SetParent(bodyT, false); hip.localPosition = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0.7f, 0.4f); hip.localRotation = Quaternion.Euler(0f, yaw, 0f);
                    var lk = new AK(); var (av, at2) = lk.Of(m.steel);
                    Ellipsoid(av, at2, Vector3.zero, Vector3.one * 0.09f, 4, 8);
                    Sweep(av, at2, new[] { Vector3.zero, new Vector3(0f, 0.12f, 0.36f) }, 0.035f, false, 6);
                    foreach (var g in lk.Save("Maint_Thigh" + i, hip, true)) g.transform.localPosition = Vector3.zero;
                    var knee = new GameObject("Maint_Knee" + i).transform; knee.SetParent(hip, false); knee.localPosition = new Vector3(0f, 0.12f, 0.36f);
                    var sk2 = new AK(); var (kv, kt) = sk2.Of(m.metal);
                    Ellipsoid(kv, kt, Vector3.zero, Vector3.one * 0.06f, 4, 8);
                    Sweep(kv, kt, new[] { Vector3.zero, new Vector3(0f, -0.8f, 0.12f) }, 0.028f, false, 6);
                    var (fv2, ft2) = sk2.Of(m.metalDark); Append(fv2, ft2, Lathe(new[] { new Vector2(0f, -0.84f), new Vector2(0.1f, -0.84f), new Vector2(0.07f, -0.78f), new Vector2(0f, -0.77f) }, 0f, 360f, 10, null, new Vector3(0f, 0f, 0.12f)));
                    foreach (var g in sk2.Save("Maint_Shin" + i, knee, true)) g.transform.localPosition = Vector3.zero;
                    hips.Add(hip); knees.Add(knee);
                }
                // the steam stack's puff (a whistle on each announcement)
                var puff = new GameObject("Maint_StackSteam").AddComponent<ParticleSystem>(); puff.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                puff.transform.SetParent(bodyT, false); puff.transform.localPosition = new Vector3(-0.22f, 1.74f, -0.2f); puff.transform.localRotation = Quaternion.LookRotation(Vector3.up);
                {
                    var mm = puff.main; mm.startLifetime = 1.2f; mm.startSpeed = 1.6f; mm.startSize = 0.35f; mm.startColor = new Color(1f, 1f, 1f, 0.45f); mm.maxParticles = 120; mm.loop = false; mm.playOnAwake = false; mm.duration = 0.5f;
                    var em = puff.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) }); var sh = puff.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 10f; sh.radius = 0.04f;
                    puff.GetComponent<ParticleSystemRenderer>().sharedMaterial = AdditiveMatTex("Ast_maint_puff", 0.6f);
                }
                // the guiding symbol it projects on the floor: a ring with an arrow through it (the route mark), not a plain arrow
                var av3 = new List<Vector3>(); var at3 = new List<int>();
                Sweep(av3, at3, Enumerable.Range(0, 25).Select(i => new Vector3(Mathf.Cos(i / 24f * Mathf.PI * 2f) * 0.55f, 0f, Mathf.Sin(i / 24f * Mathf.PI * 2f) * 0.55f)).ToList(), 0.04f, true, 4);
                Quad(av3, at3, new Vector3(-0.12f, 0.01f, -0.4f), new Vector3(-0.12f, 0.01f, 0.25f), new Vector3(0.12f, 0.01f, 0.25f), new Vector3(0.12f, 0.01f, -0.4f));
                Quad(av3, at3, new Vector3(-0.32f, 0.01f, 0.2f), new Vector3(0f, 0.01f, 0.62f), new Vector3(0f, 0.01f, 0.62f), new Vector3(0.32f, 0.01f, 0.2f));
                var arrow = MeshObject("Maint_FloorArrow", SaveMesh(Finish(av3, at3), "Ast_MaintArrow"), new Material(m.glowSoft) { name = "Ast_maintarrow" }, rc, false); arrow.SetActive(false);
                // the brass speaking-horns in the chamber walls (their mouths glow with each word)
                var horns = new List<Renderer>();
                foreach (var (p, d) in new[] { (new Vector3(-17.6f, 6.5f, 2f), Vector3.right), (new Vector3(17.6f, 6.5f, 2f), Vector3.left), (new Vector3(-4f, 6.5f, 17.6f), Vector3.back), (new Vector3(10f, 6.5f, 17.6f), Vector3.back) })
                {
                    int hn = horns.Count;
                    var hk2 = new AK(); var (hv, ht) = hk2.Of(m.metal);
                    var hm = Lathe(new[] { new Vector2(0.08f, 0f), new Vector2(0.1f, 0.5f), new Vector2(0.2f, 0.9f), new Vector2(0.45f, 1.15f), new Vector2(0.5f, 1.2f) }, 0f, 360f, 16);
                    var q = Quaternion.FromToRotation(Vector3.up, d); var hvv = hm.vertices; for (int i = 0; i < hvv.Length; i++) hvv[i] = q * hvv[i] + p; hm.vertices = hvv; Append(hv, ht, hm);
                    Riveted(hk2, m.metalDark, m.metal, p - d * 0.1f, new Vector3(0.6f, 0.6f, 0.6f));
                    hk2.Save($"SpeakingHorn{hn}", rc, true);
                    var mv = new List<Vector3>(); var mt2 = new List<int>();
                    Sweep(mv, mt2, Enumerable.Range(0, 21).Select(i => p + d * 1.18f + q * new Vector3(Mathf.Cos(i / 20f * Mathf.PI * 2f) * 0.44f, 0f, Mathf.Sin(i / 20f * Mathf.PI * 2f) * 0.44f)).ToList(), 0.035f, true, 5);
                    horns.Add(MeshObject($"SpeakingHorn{hn}_Mouth", SaveMesh(SmoothFinish(mv, mt2), $"Ast_HornMouth{hn}"), new Material(m.glowSoft) { name = $"Ast_hornmouth{hn}" }, rc, false).GetComponent<Renderer>());
                }
                r.bot = mb.gameObject.AddComponent<Seven.AstMaintBot>(); r.bot.Lens = lensP; r.bot.LensGlow = lensGo.GetComponent<Renderer>(); r.bot.Arrow = arrow.GetComponent<Renderer>(); r.bot.Goal = A.MaintBotHome;
                r.bot.Body = bodyT; r.bot.Head = headT; r.bot.Iris = iris.ToArray(); r.bot.Grille = grille.GetComponent<Renderer>(); r.bot.Fan = fanP; r.bot.Hips = hips.ToArray(); r.bot.Knees = knees.ToArray();
                r.bot.Stack = puff; r.bot.Dial = dial; r.bot.Horns = horns.ToArray();
            }
            // pressure vents on the walls (they glow while the chamber pressurizes)
            var vents = new List<Renderer>();
            foreach (var (p, yaw) in new[] { (new Vector3(-17.9f, 2.4f, 8f), 90f), (new Vector3(17.9f, 2.4f, -6f), 90f), (new Vector3(-6f, 2.4f, 17.9f), 0f), (new Vector3(8f, 2.4f, -17.9f), 0f) })
            {
                var gv = new List<Vector3>(); var gt = new List<int>();
                for (int i = 0; i < 5; i++) MBox(gv, gt, p + Quaternion.Euler(0f, yaw, 0f) * new Vector3(-0.6f + i * 0.3f, 0f, 0f), Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.14f, 1.2f, 0.1f), yaw, 0f);
                vents.Add(MeshObject($"PressureVent{vents.Count}", SaveMesh(Finish(gv, gt), $"Ast_PressureVent{vents.Count}"), new Material(m.glowSoft) { name = "Ast_vent" + vents.Count }, rc, false).GetComponent<Renderer>());
            }
            r.vents = vents.ToArray();
            return r;
        }
        static Mesh RotX(Mesh m, Vector3 at) { var q = Quaternion.Euler(90f, 0f, 0f); var v = m.vertices; for (int i = 0; i < v.Length; i++) v[i] = q * v[i] + at; m.vertices = v; m.RecalculateNormals(); m.RecalculateBounds(); return m; }
        static Mesh RotZ(Mesh m, Vector3 at) { var q = Quaternion.Euler(0f, 0f, 90f); var v = m.vertices; for (int i = 0; i < v.Length; i++) v[i] = q * v[i] + at; m.vertices = v; m.RecalculateNormals(); m.RecalculateBounds(); return m; }
    }
}
