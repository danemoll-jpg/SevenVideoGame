using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF17 (3) — the Reptilian architecture kit for Mars (Dan: "Mars buildings are all cubes"). Everything in Mars is built
    /// from these pieces instead of plain boxes:
    ///  - <see cref="MBox"/> / <see cref="MBlock"/>: dressed masonry - battered (sloped) sides, a plinth, a chamfered coping;
    ///    used for every block that used to be a cube (decks, rails, piers, walls, slabs, panels);
    ///  - <see cref="Tower"/>: a battered polygonal tower with a moulding ring, a crenellated parapet and a dome or a pointed roof;
    ///  - <see cref="ReptHouse"/>: a stepped (terraced) house - battered tiers, parapets with merlons, an arched door, arched
    ///    windows (some lit), a roof dome or a corner tower, a balcony, an awning, a banner;
    ///  - <see cref="FortressCity"/>: a curtain wall with towers, a gatehouse, a stepped citadel and terraced quarters
    ///    (silhouettes fading into the haze, Kyle's look);
    ///  - <see cref="RoundSlab"/>: the temple's older, smoother monumental style (rounded corners, gentle batter).
    /// Meshes are collected per material (stone / dark stone / glow / cloth) and saved once per piece.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        // ---------------------------------------------------------------- dressed masonry
        /// <summary>A block with battered sides, a small plinth and a chamfered top - never a plain box.</summary>
        static void MBox(List<Vector3> v, List<int> t, Vector3 c, Vector3 size, float yaw = 0f, float batter = 0.05f)
        {
            float h = size.y, hx = size.x * 0.5f, hz = size.z * 0.5f;
            float minHalf = Mathf.Min(hx, hz);
            float b = Mathf.Min(batter * h, minHalf * 0.35f);             // how far the top steps in
            float ch = Mathf.Min(0.12f, h * 0.12f, minHalf * 0.3f);      // the chamfer
            float pl = Mathf.Min(0.2f, h * 0.08f);                        // the plinth
            float po = Mathf.Min(0.05f, minHalf * 0.1f);                 // plinth outset
            var levels = new List<(float y, float inset)>
            {
                (-h * 0.5f, -po), (-h * 0.5f + pl, -po), (-h * 0.5f + pl, 0f), (h * 0.5f - ch, b), (h * 0.5f, b + ch),
            };
            Loft(v, t, c, hx, hz, yaw, levels);
        }
        static void Loft(List<Vector3> v, List<int> t, Vector3 c, float hx, float hz, float yaw, List<(float y, float inset)> levels)
        {
            var q = Quaternion.Euler(0f, yaw, 0f);
            Vector3[] Ring(float y, float inset)
            {
                float x = Mathf.Max(0.01f, hx - inset), z = Mathf.Max(0.01f, hz - inset);
                return new[] { c + q * new Vector3(-x, y, -z), c + q * new Vector3(x, y, -z), c + q * new Vector3(x, y, z), c + q * new Vector3(-x, y, z) };
            }
            Vector3[] prev = null;
            foreach (var (y, inset) in levels)
            {
                var r = Ring(y, inset);
                if (prev != null && Mathf.Abs(prev[0].y - r[0].y) > 1e-4f || prev != null)
                    for (int k = 0; k < 4; k++) Quad(v, t, prev[k], r[k], r[(k + 1) % 4], prev[(k + 1) % 4]);
                prev = r;
            }
            var top = Ring(levels[levels.Count - 1].y, levels[levels.Count - 1].inset);
            Quad(v, t, top[0], top[3], top[2], top[1]);
            var bot = Ring(levels[0].y, levels[0].inset);
            Quad(v, t, bot[0], bot[1], bot[2], bot[3]);
        }
        static int _mblock;
        /// <summary>The dressed-masonry replacement for Block (same min / max / collider meaning, a real mesh, scale 1).</summary>
        static GameObject MBlock(string name, Vector3 min, Vector3 max, Material mat, Transform parent, bool collider = true)
        {
            var size = new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), Mathf.Abs(max.z - min.z));
            var v = new List<Vector3>(); var t = new List<int>();
            MBox(v, t, Vector3.zero, size, 0f, size.y > 2f ? 0.035f : 0.02f);
            var go = MeshObject(name, SaveMesh(Finish(v, t), $"Mars_blk_{name}_{_mblock++}"), mat, parent);
            go.transform.position = (min + max) * 0.5f;
            if (collider) { var bc = go.AddComponent<BoxCollider>(); bc.size = size; }
            return go;
        }

        /// <summary>QF18 (13): ground, decks, floors and slabs she rides or walks on. QF17 turned every Mars block into dressed
        /// masonry (MBlock: the top steps in by batter + chamfer - 0.9 m on a 22 m tall ground block), so wherever two ground
        /// pieces met their tops pulled apart into an open groove showing the bright fog below: the checkpoint hole (the plain
        /// blocks meeting at x = -60 under the transport), the long glowing strip, the ramp tops. Ground is terrain, not
        /// masonry: straight sides, a flat top that meets its neighbour edge to edge. The holes check (HolesCheck) proves it.</summary>
        static GameObject MGround(string name, Vector3 min, Vector3 max, Material mat, Transform parent, bool collider = false)
        {
            var size = new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), Mathf.Abs(max.z - min.z));
            var v = new List<Vector3>(); var t = new List<int>();
            Loft(v, t, Vector3.zero, size.x * 0.5f, size.z * 0.5f, 0f, new List<(float, float)> { (-size.y * 0.5f, 0f), (size.y * 0.5f, 0f) });
            var go = MeshObject(name, SaveMesh(Finish(v, t), $"Mars_gnd_{name}_{_mblock++}"), mat, parent);
            go.transform.position = (min + max) * 0.5f;
            if (collider) { var bc = go.AddComponent<BoxCollider>(); bc.size = size; }
            return go;
        }

        /// <summary>QF18 (12, 15, 16): a power line / conduit along a path - a glowing core split into segs pieces (so the
        /// light can visibly run along it: <see cref="Seven.PowerLine"/>), metal bands round it every bandStep m.</summary>
        static Seven.PowerLine PowerLineObj(string name, Vector3[] path, int segs, float radius, Material glow, Material bands, Transform parent, float bandStep = 0.7f)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false);
            var cum = new float[path.Length]; for (int i = 1; i < path.Length; i++) cum[i] = cum[i - 1] + Vector3.Distance(path[i - 1], path[i]);
            float L = cum[path.Length - 1];
            Vector3 At(float s) { for (int i = 1; i < path.Length; i++) if (s <= cum[i] || i == path.Length - 1) { float u = Mathf.InverseLerp(cum[i - 1], cum[i], s); return Vector3.Lerp(path[i - 1], path[i], u); } return path[path.Length - 1]; }
            Vector3 Tan(float s) { var a = At(Mathf.Max(0f, s - 0.05f)); var b = At(Mathf.Min(L, s + 0.05f)); var d = b - a; return d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.forward; }
            var rs = new List<Renderer>(); var pts = new List<Vector3>();
            for (int k = 0; k < segs; k++)
            {
                float s0 = L * k / segs, s1 = L * (k + 1) / segs;
                var sub = new List<Vector3> { At(s0) };
                for (int i = 1; i < path.Length - 1; i++) if (cum[i] > s0 + 0.02f && cum[i] < s1 - 0.02f) sub.Add(path[i]);
                sub.Add(At(s1));
                var v = new List<Vector3>(); var t = new List<int>(); Sweep(v, t, sub, radius, false, 7);
                var go = MeshObject($"{name}_{k:00}", SaveMesh(SmoothFinish(v, t), $"Mars_{name}_{k:00}"), glow, root, false);
                go.transform.localPosition = Vector3.zero;
                rs.Add(go.GetComponent<Renderer>()); pts.Add(At((s0 + s1) * 0.5f));
            }
            if (bands != null)
            {
                var bv = new List<Vector3>(); var bt = new List<int>();
                for (float s = 0.15f; s < L; s += bandStep)
                {
                    var c = At(s); var tg = Tan(s); var u = Vector3.Cross(tg, Mathf.Abs(tg.y) < 0.9f ? Vector3.up : Vector3.right).normalized; var w = Vector3.Cross(tg, u);
                    var ring = Enumerable.Range(0, 11).Select(i => c + (u * Mathf.Cos(i / 10f * Mathf.PI * 2f) + w * Mathf.Sin(i / 10f * Mathf.PI * 2f)) * radius * 1.9f).ToList();
                    Sweep(bv, bt, ring, radius * 0.45f, true, 5);
                }
                var bg = MeshObject(name + "_Bands", SaveMesh(SmoothFinish(bv, bt), $"Mars_{name}_Bands"), bands, root, false); bg.transform.localPosition = Vector3.zero;
            }
            var pl = root.gameObject.AddComponent<Seven.PowerLine>(); pl.Segs = rs.ToArray(); pl.Points = pts.ToArray();
            return pl;
        }

        // ---------------------------------------------------------------- towers, arches, windows
        /// <summary>A polygonal frustum (battered tower body) from y0 to y1, radius r0 -> r1.</summary>
        static void Frustum(List<Vector3> v, List<int> t, Vector3 c, float r0, float r1, float y0, float y1, int sides, float rot = 0f, bool cap = true)
        {
            for (int i = 0; i < sides; i++)
            {
                float a0 = rot + i * 360f / sides, a1 = rot + (i + 1) * 360f / sides;
                Quad(v, t, c + Polar(r0, a0, y0), c + Polar(r1, a0, y1), c + Polar(r1, a1, y1), c + Polar(r0, a1, y0));
                if (cap) { int k = v.Count; v.Add(c + Vector3.up * y1); v.Add(c + Polar(r1, a1, y1)); v.Add(c + Polar(r1, a0, y1)); t.Add(k); t.Add(k + 1); t.Add(k + 2); }
            }
        }
        /// <summary>A crenellated parapet ring round a tower top (merlons).</summary>
        static void Merlons(List<Vector3> v, List<int> t, Vector3 c, float r, float y, int n, float w, float h, float th)
        {
            for (int i = 0; i < n; i++) { float a = i * 360f / n; MBox(v, t, c + Polar(r, a, y + h * 0.5f), new Vector3(w, h, th), -a + 90f, 0.03f); }
        }
        /// <summary>Merlons along a straight wall top from a to b.</summary>
        static void MerlonRow(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, float w, float h, float th, float gap)
        {
            var d = b - a; d.y = 0f; float len = d.magnitude; if (len < 0.1f) return;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg + 90f;
            int n = Mathf.Max(1, Mathf.FloorToInt(len / (w + gap)));
            for (int i = 0; i < n; i++) { var p = Vector3.Lerp(a, b, (i + 0.5f) / n); MBox(v, t, p + Vector3.up * h * 0.5f, new Vector3(w, h, th), yaw, 0.03f); }
        }
        /// <summary>A flat arched opening (door / window) laid on a wall face, a hair in front of it.</summary>
        static void ArchInset(List<Vector3> v, List<int> t, Vector3 baseCentre, Vector3 outward, float w, float h, bool pointed = false)
        {
            var o = outward.normalized; var side = Vector3.Cross(Vector3.up, o).normalized;
            var p0 = baseCentre + o * 0.04f;
            float rr = w * 0.5f, hs = Mathf.Max(0.05f, h - rr * (pointed ? 1.3f : 1f));
            var outline = new List<Vector3> { p0 - side * rr, p0 - side * rr + Vector3.up * hs };
            int seg = 10;
            for (int i = 1; i < seg; i++)
            {
                float a = Mathf.PI - i * Mathf.PI / seg;
                float x = Mathf.Cos(a) * rr, y = Mathf.Sin(a) * rr * (pointed ? 1.3f : 1f);
                if (pointed) y *= 1f + 0.25f * (1f - Mathf.Abs(Mathf.Cos(a)));
                outline.Add(p0 + side * x + Vector3.up * (hs + y));
            }
            outline.Add(p0 + side * rr + Vector3.up * hs); outline.Add(p0 + side * rr);
            var cen = p0 + Vector3.up * (h * 0.45f);
            int k0 = v.Count; v.Add(cen); foreach (var p in outline) v.Add(p);
            for (int i = 0; i < outline.Count - 1; i++) { t.Add(k0); t.Add(k0 + 1 + i); t.Add(k0 + 2 + i); }
            t.Add(k0); t.Add(k0 + outline.Count); t.Add(k0 + 1);
        }
        /// <summary>A free-standing arch (gateway) over a road: two battered piers and the arch ring between them.</summary>
        static void Gateway(List<Vector3> v, List<int> t, Vector3 c, float span, float h, float depth, float yaw)
        {
            var q = Quaternion.Euler(0f, yaw, 0f); float pw = 1.6f;
            foreach (var s in new[] { -1f, 1f }) MBox(v, t, c + q * new Vector3(s * (span * 0.5f + pw * 0.5f), h * 0.5f, 0f), new Vector3(pw, h, depth), yaw, 0.03f);
            // the arch: a segmental ring of voussoirs
            int n = 13; float r = span * 0.5f + pw * 0.5f;
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.PI * (i + 0.5f) / n;
                var p = c + q * new Vector3(Mathf.Cos(a) * (span * 0.5f + 0.4f), h + Mathf.Sin(a) * span * 0.32f, 0f);
                MBox(v, t, p, new Vector3(0.95f, 1.0f, depth), yaw - (a * Mathf.Rad2Deg - 90f) * 0f, 0.02f);
            }
            MBox(v, t, c + q * new Vector3(0f, h + span * 0.32f + 0.9f, 0f), new Vector3(span + pw * 2f, 0.9f, depth + 0.3f), yaw, 0.02f);
            MerlonRow(v, t, c + q * new Vector3(-(span * 0.5f + pw), h + span * 0.32f + 1.35f, 0f), c + q * new Vector3(span * 0.5f + pw, h + span * 0.32f + 1.35f, 0f), 0.7f, 0.8f, depth * 0.6f, 0.6f);
        }

        /// <summary>The kit's mesh lists (one per material) for a group of buildings.</summary>
        class Kit
        {
            public readonly List<Vector3> sv = new List<Vector3>(), dv = new List<Vector3>(), rv = new List<Vector3>(), gv = new List<Vector3>(), cv = new List<Vector3>();
            public readonly List<int> st = new List<int>(), dt = new List<int>(), rt = new List<int>(), gt = new List<int>(), ct = new List<int>();
            public void Save(string prefix, Transform parent, Material stone, Material dark, Material roof, Material glow, Material cloth)
            {
                if (sv.Count > 0) MeshObject(prefix + "_Masonry", SaveMesh(Finish(sv, st), "Mars_" + prefix + "_Masonry"), stone, parent);
                if (rv.Count > 0) MeshObject(prefix + "_Roofs", SaveMesh(SmoothFinish(rv, rt), "Mars_" + prefix + "_Roofs"), roof, parent);
                if (dv.Count > 0) MeshObject(prefix + "_Openings", SaveMesh(Finish(dv, dt), "Mars_" + prefix + "_Openings"), dark, parent, false);
                if (gv.Count > 0) MeshObject(prefix + "_LitWindows", SaveMesh(Finish(gv, gt), "Mars_" + prefix + "_LitWindows"), glow, parent, false);
                if (cv.Count > 0) MeshObject(prefix + "_Cloth", SaveMesh(Finish(cv, ct), "Mars_" + prefix + "_Cloth"), cloth, parent);
            }
        }

        /// <summary>A Reptilian tower: battered polygon body, a moulding ring, windows, a crenellated parapet, a roof.</summary>
        static void Tower(Kit k, Vector3 c, float r, float h, int sides, bool pointed, System.Random rnd, bool lit = true)
        {
            Frustum(k.sv, k.st, c, r * 1.12f, r * 1.12f, 0f, 0.6f, sides);                         // plinth
            Frustum(k.sv, k.st, c, r, r * 0.86f, 0.6f, h, sides);                                    // body (battered)
            Frustum(k.sv, k.st, c, r * 0.95f, r * 0.95f, h, h + 0.35f, sides);                       // moulding
            Merlons(k.sv, k.st, c, r * 0.9f, h + 0.35f, sides * 2, Mathf.Min(0.9f, r * 0.5f), 0.9f, 0.4f);
            if (pointed) { var roof = Lathe(new[] { new Vector2(r * 0.8f, 0f), new Vector2(r * 0.55f, r * 1.2f), new Vector2(r * 0.15f, r * 2.4f), new Vector2(0f, r * 2.8f) }, 0f, 360f, sides, null, c + Vector3.up * (h + 0.35f)); Add(k.rv, k.rt, roof); }
            else { var dome = Lathe(new[] { new Vector2(r * 0.82f, 0f), new Vector2(r * 0.8f, r * 0.35f), new Vector2(r * 0.6f, r * 0.75f), new Vector2(r * 0.28f, r * 0.98f), new Vector2(0f, r * 1.05f) }, 0f, 360f, Mathf.Max(12, sides * 2), null, c + Vector3.up * (h + 0.35f)); Add(k.rv, k.rt, dome); }
            // windows round the body, in two or three bands
            for (int band = 0; band < Mathf.Min(3, Mathf.FloorToInt(h / 4f)); band++)
                for (int i = 0; i < sides; i += 2)
                {
                    float a = (i + 0.5f) * 360f / sides; float y = 2.2f + band * 3.4f; float rr = Mathf.Lerp(r, r * 0.86f, y / h);
                    var o = Polar(1f, a, 0f); var p = c + o * (rr * Mathf.Cos(Mathf.PI / sides)) + Vector3.up * y;
                    bool on = lit && rnd.NextDouble() < 0.3;
                    ArchInset(on ? k.gv : k.dv, on ? k.gt : k.dt, p, o, Mathf.Min(0.7f, r * 0.35f), 1.3f, pointed);
                }
        }
        static void Add(List<Vector3> v, List<int> t, Mesh m) { int b = v.Count; v.AddRange(m.vertices); foreach (var i in m.triangles) t.Add(b + i); }

        /// <summary>A Reptilian house: 1-3 battered tiers stepping back (terraces with parapets), an arched door on the street
        /// side, arched windows (some lit), a dome or a corner tower, sometimes a balcony, an awning and a banner.</summary>
        static void ReptHouse(Kit k, Vector3 p, Vector3 size, float yaw, System.Random rnd, bool awning = false)
        {
            var q = Quaternion.Euler(0f, yaw, 0f); var fwd = q * Vector3.back;   // the street side (-z local)
            int tiers = size.y > 7.5f ? 3 : size.y > 5.5f ? 2 : 1;
            float y = 0f; Vector3 s = size; float th = size.y / tiers;
            for (int i = 0; i < tiers; i++)
            {
                var c = p + q * new Vector3(0f, y + th * 0.5f, i * 0.6f);
                MBox(k.sv, k.st, c, new Vector3(s.x, th, s.z), yaw, 0.06f);
                // the terrace parapet with merlons (front and sides)
                float top = y + th;
                var fl = p + q * new Vector3(-s.x * 0.5f + 0.2f, top, i * 0.6f - s.z * 0.5f + 0.2f); var fr = p + q * new Vector3(s.x * 0.5f - 0.2f, top, i * 0.6f - s.z * 0.5f + 0.2f);
                MerlonRow(k.sv, k.st, fl, fr, 0.55f, 0.6f, 0.3f, 0.45f);
                // windows on the front of this tier
                int nw = Mathf.Max(1, Mathf.FloorToInt(s.x / 2.4f));
                for (int w = 0; w < nw; w++)
                {
                    if (i == 0 && nw > 1 && w == nw / 2) continue;                                    // the door's place
                    var wc = p + q * new Vector3(-s.x * 0.5f + s.x * (w + 0.5f) / nw, y + th * 0.38f, i * 0.6f - s.z * 0.5f + th * 0.03f);
                    bool on = rnd.NextDouble() < 0.28;
                    ArchInset(on ? k.gv : k.dv, on ? k.gt : k.dt, wc, fwd, 0.62f, Mathf.Min(1.4f, th * 0.42f), false);
                }
                y += th; s = new Vector3(s.x * 0.74f, s.y, s.z * 0.72f);
            }
            // the door: an arch with a deep dark opening and a raised surround
            var door = p + q * new Vector3(0f, 0f, -size.z * 0.5f - 0.02f);
            MBox(k.sv, k.st, door + Vector3.up * 1.45f + fwd * 0.12f, new Vector3(1.9f, 2.9f, 0.25f), yaw, 0.02f);
            ArchInset(k.dv, k.dt, door + fwd * 0.26f, fwd, 1.25f, 2.35f, rnd.NextDouble() < 0.4);
            // the roof: a dome or a small corner tower
            float topY = y;
            if (rnd.NextDouble() < 0.55)
            {
                float r = Mathf.Min(s.x, s.z) * 0.62f;
                var dome = Lathe(new[] { new Vector2(r, 0f), new Vector2(r * 0.95f, r * 0.4f), new Vector2(r * 0.68f, r * 0.82f), new Vector2(r * 0.3f, r * 1.02f), new Vector2(0f, r * 1.08f) }, 0f, 360f, 14, null, p + q * new Vector3(0f, topY, (tiers - 1) * 0.6f));
                Add(k.rv, k.rt, dome);
            }
            else
            {
                var cc = p + q * new Vector3(size.x * 0.32f, 0f, size.z * 0.18f);
                Tower(k, cc, Mathf.Min(1.4f, size.x * 0.16f), topY + 2.6f, 8, rnd.NextDouble() < 0.5, rnd);
            }
            // a balcony on the second tier (slab on corbels + a low rail)
            if (tiers > 1 && rnd.NextDouble() < 0.6)
            {
                var bc = p + q * new Vector3(-size.x * 0.22f, th + 0.1f, -size.z * 0.5f - 0.55f);
                MBox(k.sv, k.st, bc, new Vector3(2.2f, 0.22f, 1.1f), yaw, 0.01f);
                foreach (var ox in new[] { -0.8f, 0.8f }) MBox(k.sv, k.st, bc + q * new Vector3(ox, -0.45f, 0.25f), new Vector3(0.3f, 0.7f, 0.6f), yaw, 0.15f);
                MBox(k.sv, k.st, bc + q * new Vector3(0f, 0.5f, -0.48f), new Vector3(2.2f, 0.8f, 0.14f), yaw, 0.01f);
            }
            // an awning over the door (cloth, sloped) and a banner down the front
            if (awning || rnd.NextDouble() < 0.35)
            {
                var a0 = p + q * new Vector3(-1.6f, 2.95f, -size.z * 0.5f - 0.05f); var a1 = p + q * new Vector3(1.6f, 2.95f, -size.z * 0.5f - 0.05f);
                var b0 = p + q * new Vector3(-1.6f, 2.35f, -size.z * 0.5f - 1.6f); var b1 = p + q * new Vector3(1.6f, 2.35f, -size.z * 0.5f - 1.6f);
                Quad(k.cv, k.ct, a0, b0, b1, a1);
            }
            if (rnd.NextDouble() < 0.45 && size.y > 4.5f)
            {
                float bx = size.x * (rnd.NextDouble() < 0.5 ? -0.36f : 0.36f);
                var t0 = p + q * new Vector3(bx - 0.4f, size.y / tiers - 0.2f, -size.z * 0.5f - 0.06f); var t1 = p + q * new Vector3(bx + 0.4f, size.y / tiers - 0.2f, -size.z * 0.5f - 0.06f);
                var b0 = t0 + Vector3.down * 2.6f + fwd * 0.05f; var b1 = t1 + Vector3.down * 2.6f + fwd * 0.05f;
                Quad(k.cv, k.ct, t0, b0, (b0 + b1) * 0.5f + Vector3.down * 0.45f, t1);
            }
        }

        /// <summary>A fortress city far off on the plain: a curtain wall with towers and a gatehouse, terraced quarters, a
        /// stepped citadel with great domes and a spire - huge forms fading into the haze.</summary>
        static void FortressCity(Kit k, Vector3 c, float radius, System.Random rnd, bool facesEast)
        {
            int nTowers = 18;
            for (int i = 0; i < nTowers; i++)
            {
                float a = i * 360f / nTowers, a1 = (i + 1) * 360f / nTowers;
                var p = c + Polar(radius, a, 0f); var p1 = c + Polar(radius, a1, 0f);
                Tower(k, p, 7.5f, 30f + (i % 3) * 6f, 8, i % 2 == 0, rnd);
                // the curtain wall between towers: battered, with a wall walk and merlons
                var mid = (p + p1) * 0.5f; var d = p1 - p; float len = d.magnitude - 12f; float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                MBox(k.sv, k.st, mid + Vector3.up * 11f, new Vector3(5f, 22f, len), yaw, 0.08f);
                MerlonRow(k.sv, k.st, mid - d.normalized * len * 0.5f + Vector3.up * 22f + Polar(1.8f, a + 180f / nTowers, 0f) * 0f, mid + d.normalized * len * 0.5f + Vector3.up * 22f, 1.4f, 1.6f, 0.9f, 1.1f);
                // arrow slits
                for (int s = 1; s < 4; s++) { var sp = Vector3.Lerp(p, p1, s / 4f) + Vector3.up * 14f; var o = (sp - c); o.y = 0f; ArchInset(k.dv, k.dt, sp + o.normalized * 2.0f, o, 0.6f, 2.2f, true); }
            }
            // the gatehouse toward the ravine: two great towers and a pointed arch
            var g = c + Polar(radius, facesEast ? 0f : 180f, 0f); var gy = facesEast ? 90f : -90f;
            Gateway(k.sv, k.st, g, 16f, 18f, 9f, gy);
            Tower(k, g + Quaternion.Euler(0f, gy, 0f) * new Vector3(-13f, 0f, 0f), 9f, 40f, 8, true, rnd);
            Tower(k, g + Quaternion.Euler(0f, gy, 0f) * new Vector3(13f, 0f, 0f), 9f, 40f, 8, true, rnd);
            // terraced quarters inside the walls (large-scale houses)
            for (int i = 0; i < 70; i++)
            {
                var p = c + new Vector3((float)rnd.NextDouble() * 2f - 1f, 0f, (float)rnd.NextDouble() * 2f - 1f) * radius * 0.85f;
                if ((p - c).magnitude > radius * 0.82f || (p - c).magnitude < radius * 0.28f) continue;
                float sc = 2.2f + (float)rnd.NextDouble() * 1.6f;
                ReptHouse(k, p, new Vector3(9f, 7f + (float)rnd.NextDouble() * 4f, 9f) * sc, (float)rnd.NextDouble() * 360f, rnd);
            }
            // the citadel: stepped terraces rising in the middle, crowned by great domes and a spire
            float y = 0f; float w = radius * 0.5f;
            for (int i = 0; i < 5; i++)
            {
                float h = 14f - i * 1.2f;
                MBox(k.sv, k.st, c + Vector3.up * (y + h * 0.5f), new Vector3(w, h, w * 0.85f), 12f, 0.07f);
                MerlonRow(k.sv, k.st, c + Quaternion.Euler(0f, 12f, 0f) * new Vector3(-w * 0.48f, y + h, -w * 0.4f), c + Quaternion.Euler(0f, 12f, 0f) * new Vector3(w * 0.48f, y + h, -w * 0.4f), 1.6f, 1.6f, 1f, 1.4f);
                for (int s = 0; s < 6; s++) { var wc = c + Quaternion.Euler(0f, 12f, 0f) * new Vector3(-w * 0.42f + w * 0.84f * (s + 0.5f) / 6f, y + h * 0.4f, -w * 0.425f); bool on = rnd.NextDouble() < 0.4; ArchInset(on ? k.gv : k.dv, on ? k.gt : k.dt, wc, Quaternion.Euler(0f, 12f, 0f) * Vector3.back, 1.6f, 4f, true); }
                y += h; w *= 0.78f;
            }
            foreach (var (ox, oz, r) in new[] { (0f, 0f, 22f), (-30f, 18f, 12f), (28f, -16f, 12f) })
            {
                var dc = c + new Vector3(ox, ox == 0f ? y : y - 28f, oz);
                Frustum(k.sv, k.st, dc, r * 1.05f, r, 0f, 6f, 24);
                Add(k.rv, k.rt, Lathe(new[] { new Vector2(r, 0f), new Vector2(r * 0.96f, r * 0.42f), new Vector2(r * 0.7f, r * 0.85f), new Vector2(r * 0.32f, r * 1.05f), new Vector2(0f, r * 1.1f) }, 0f, 360f, 28, null, dc + Vector3.up * 6f));
            }
            Add(k.rv, k.rt, Lathe(new[] { new Vector2(3.2f, 0f), new Vector2(2.4f, 30f), new Vector2(0.6f, 70f), new Vector2(0f, 82f) }, 0f, 360f, 12, null, c + Vector3.up * (y + 28f)));
        }

        // ---------------------------------------------------------------- the temple's older, smoother style
        /// <summary>A rounded, gently battered slab (the temple): a rounded-rectangle outline lofted up with a slight batter
        /// and a soft rounded top.</summary>
        static void RoundSlab(List<Vector3> v, List<int> t, Vector3 c, Vector3 size, float yaw, float corner, float batter = 0.03f)
        {
            var q = Quaternion.Euler(0f, yaw, 0f);
            int cs = 5;
            List<Vector3> Outline(float y, float inset)
            {
                float hx = size.x * 0.5f - inset, hz = size.z * 0.5f - inset, r = Mathf.Min(corner, Mathf.Min(hx, hz) * 0.95f);
                var pts = new List<Vector3>();
                foreach (var (cx, cz, a0) in new[] { (hx - r, hz - r, 0f), (-hx + r, hz - r, 90f), (-hx + r, -hz + r, 180f), (hx - r, -hz + r, 270f) })
                    for (int i = 0; i <= cs; i++) { float a = (a0 + i * 90f / cs) * Mathf.Deg2Rad; pts.Add(c + q * new Vector3(cx + Mathf.Cos(a) * r, y, cz + Mathf.Sin(a) * r)); }
                return pts;
            }
            float h = size.y; float b = batter * h;
            var lv = new[] { (-h * 0.5f, 0f), (h * 0.5f - 0.25f, b), (h * 0.5f - 0.06f, b + 0.12f), (h * 0.5f, b + 0.3f) };
            List<Vector3> prev = null;
            foreach (var (y, ins) in lv)
            {
                var r = Outline(y, ins);
                if (prev != null) for (int i = 0; i < r.Count; i++) { int j = (i + 1) % r.Count; Quad(v, t, prev[i], r[i], r[j], prev[j]); }
                prev = r;
            }
            int k0 = v.Count; var topC = c + Vector3.up * (h * 0.5f); v.Add(topC); v.AddRange(prev);
            for (int i = 0; i < prev.Count; i++) { t.Add(k0); t.Add(k0 + 1 + (i + 1) % prev.Count); t.Add(k0 + 1 + i); }
        }
    }

    /// <summary>QF18: x.Let(f) = f(x) (builder one-liners that tweak a generated mesh).</summary>
    static class BuilderLet { public static T Let<T>(this T x, System.Func<T, T> f) => f(x); }
}
