using System.Collections.Generic;
using UnityEngine;
using L = Seven.MoonLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 9 (8b, Dan's addition): the sealed door after the vision and the chamber around it in the style of
    /// Art/Reference/ref_lunar_door.png - in ruins. Smooth pale flowing ribs and arches that grow into each other,
    /// skylight slits; a tall pointed arch door split into two leaves by an S-shaped seam, fine astronomical engravings
    /// (orbit lines with small circles strung on them) on both leaves; the round three-spoke emblem beside it (the
    /// dodecahedron outline from the vision glows in it); engraved concentric circles on the floor with a line of small
    /// circles leading to the door. Ruins: cracked and broken ribs, fallen chunks, missing panels, low drifting fog.
    /// The same ribs run through the hall exit and across the hall so the area reads as one place.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const float ArchHalf = 1.2f, ArchSpring = 1.6f, ArchH = 3.7f, DoorZ = -38.2f, LeafT = 0.1f;
        static float ArchCx => ((ArchH - ArchSpring) * (ArchH - ArchSpring) - ArchHalf * ArchHalf) / (2f * ArchHalf);

        /// <summary>Inside the pointed arch (x from the door centre, y above the floor).</summary>
        static bool InArch(float x, float y)
        {
            if (y < 0f || y > ArchH) return false;
            if (y <= ArchSpring) return Mathf.Abs(x) <= ArchHalf;
            float cx = ArchCx, r = cx + ArchHalf, dy = y - ArchSpring;
            return (x - cx) * (x - cx) + dy * dy <= r * r && (x + cx) * (x + cx) + dy * dy <= r * r;
        }
        /// <summary>The S-shaped seam: x at height y (top part bulges left, lower part right, ends bottom left).</summary>
        static float SeamX(float y) { float t = y / ArchH; return Mathf.Lerp(-1.02f, 0f, t) + 0.42f * Mathf.Sin(2f * Mathf.PI * t); }

        /// <summary>Tube along a Catmull-Rom curve with a radius going r0 -> r1 (flowing ribs), capped ends.</summary>
        static void Tube(List<Vector3> v, List<int> t, IList<Vector3> ctrl, float r0, float r1, int sides = 10, int per = 7)
        {
            var pts = new List<Vector3>();
            for (int i = 0; i < ctrl.Count - 1; i++)
            {
                var p0 = ctrl[Mathf.Max(0, i - 1)]; var p1 = ctrl[i]; var p2 = ctrl[i + 1]; var p3 = ctrl[Mathf.Min(ctrl.Count - 1, i + 2)];
                for (int k = 0; k < per; k++)
                {
                    float s = k / (float)per, s2 = s * s, s3 = s2 * s;
                    pts.Add(0.5f * ((2f * p1) + (-p0 + p2) * s + (2f * p0 - 5f * p1 + 4f * p2 - p3) * s2 + (-p0 + 3f * p1 - 3f * p2 + p3) * s3));
                }
            }
            pts.Add(ctrl[ctrl.Count - 1]);
            int n = pts.Count, b0 = v.Count;
            for (int j = 0; j < n; j++)
            {
                var T = (pts[Mathf.Min(n - 1, j + 1)] - pts[Mathf.Max(0, j - 1)]).normalized;
                var N = Vector3.Cross(T, Vector3.up); if (N.sqrMagnitude < 1e-4f) N = Vector3.Cross(T, Vector3.right);
                N.Normalize(); var B = Vector3.Cross(T, N);
                float r = Mathf.Lerp(r0, r1, j / (float)(n - 1));
                for (int a = 0; a < sides; a++) { float ang = a / (float)sides * Mathf.PI * 2f; v.Add(pts[j] + (N * Mathf.Cos(ang) + B * Mathf.Sin(ang)) * r); }
            }
            for (int j = 0; j < n - 1; j++)
                for (int a = 0; a < sides; a++)
                {
                    int v00 = b0 + j * sides + a, v01 = b0 + j * sides + (a + 1) % sides, v10 = v00 + sides, v11 = v01 + sides;
                    t.AddRange(new[] { v00, v11, v10, v00, v01, v11 });
                }
            // caps (rough broken ends)
            foreach (var (j, flip) in new[] { (0, true), (n - 1, false) })
            {
                int c = v.Count; v.Add(pts[j]);
                for (int a = 0; a < sides; a++)
                {
                    int p = b0 + j * sides + a, q = b0 + j * sides + (a + 1) % sides;
                    if (flip) t.AddRange(new[] { c, p, q }); else t.AddRange(new[] { c, q, p });
                }
            }
        }

        static Vector3 DoorP(float x, float y, float dz = 0f) => new Vector3(L.SealedDoor.x + x, L.Lower + y, DoorZ + dz);

        /// <summary>Half-width of the pointed arch at height y.</summary>
        static float ArchW(float y)
        {
            if (y <= ArchSpring) return ArchHalf;
            float cx = ArchCx, r = cx + ArchHalf, dy = Mathf.Min(y, ArchH) - ArchSpring;
            return Mathf.Max(0f, Mathf.Sqrt(Mathf.Max(0f, r * r - dy * dy)) - cx);
        }

        /// <summary>Horizontal trapezoid strips between two edge functions (smooth edges), front + optional back face.</summary>
        static void Strips(List<Vector3> v, List<int> t, System.Func<float, float> xl, System.Func<float, float> xr, float yA, float yB, float z, bool back, float c = 0.02f)
        {
            for (float y = yA; y < yB - 1e-5f; y += c)
            {
                float y1 = Mathf.Min(yB, y + c);
                float l0 = xl(y), l1 = xl(y1), r0 = xr(y), r1 = xr(y1);
                if (r0 - l0 < 1e-4f && r1 - l1 < 1e-4f) continue;
                foreach (var (zz, front) in back ? new[] { (z, true), (z - LeafT, false) } : new[] { (z, true) })
                {
                    int i0 = v.Count;
                    v.Add(DoorP(l0, y, zz)); v.Add(DoorP(l1, y1, zz)); v.Add(DoorP(Mathf.Max(l1, r1), y1, zz)); v.Add(DoorP(Mathf.Max(l0, r0), y, zz));
                    if (front) t.AddRange(new[] { i0, i0 + 2, i0 + 1, i0, i0 + 3, i0 + 2 });
                    else t.AddRange(new[] { i0, i0 + 1, i0 + 2, i0, i0 + 2, i0 + 3 });
                }
            }
        }

        /// <summary>One door leaf: the arch on its side of the S-seam (exact edges).</summary>
        static Mesh LeafMesh(bool left, string name)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            const float gap = 0.012f;
            if (left) Strips(v, t, y => -ArchW(y), y => Mathf.Min(ArchW(y), SeamX(y) - gap), 0f, ArchH - 0.01f, 0f, true);
            else Strips(v, t, y => Mathf.Max(-ArchW(y), SeamX(y) + gap), y => ArchW(y), 0f, ArchH - 0.01f, 0f, true);
            return SaveMesh(Finish(v, t), name);
        }

        /// <summary>Thin engraved lines on a leaf face: the parts of a polyline that lie on that leaf.</summary>
        static void Engrave(List<Vector3> v, List<int> t, bool left, IList<Vector2> line, float r = 0.009f)
        {
            var run = new List<Vector3>();
            void Flush() { if (run.Count > 1) Sweep(v, t, new List<Vector3>(run), r, false, 4); run.Clear(); }
            foreach (var p in line)
            {
                bool on = InArch(p.x, p.y) && Mathf.Abs(Mathf.Abs(p.x) - ArchHalf) > 0.04f && (left ? p.x < SeamX(p.y) - 0.04f : p.x > SeamX(p.y) + 0.04f);
                if (on) run.Add(DoorP(p.x, p.y, 0.006f)); else Flush();
            }
            Flush();
        }
        static List<Vector2> Arc(Vector2 c, float r, float a0, float a1, int n = 60)
        {
            var l = new List<Vector2>();
            for (int i = 0; i <= n; i++) { float a = Mathf.Lerp(a0, a1, i / (float)n) * Mathf.Deg2Rad; l.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
            return l;
        }
        static List<Vector2> Seg(Vector2 a, Vector2 b, int n = 20) { var l = new List<Vector2>(); for (int i = 0; i <= n; i++) l.Add(Vector2.Lerp(a, b, i / (float)n)); return l; }

        static void LeafEngravings(List<Vector3> v, List<int> t, bool left)
        {
            // orbit lines (big arcs centred off the leaf) and small circles strung along them like planets
            var orbits = left
                ? new[] { (new Vector2(-1.9f, 0.5f), 1.75f), (new Vector2(-1.7f, 0.1f), 2.35f), (new Vector2(-2.0f, 1.2f), 1.25f) }
                : new[] { (new Vector2(1.9f, -0.2f), 1.55f), (new Vector2(1.9f, -0.2f), 2.05f), (new Vector2(2.0f, -0.4f), 2.6f) };
            foreach (var (c, r) in orbits) Engrave(v, t, left, Arc(c, r, 0f, 360f, 140));
            var planets = left
                ? new[] { (new Vector2(-0.55f, 2.05f), 0.09f), (new Vector2(-0.82f, 1.3f), 0.1f), (new Vector2(-0.35f, 2.75f), 0.07f) }
                : new[] { (new Vector2(0.1f, 3.05f), 0.08f), (new Vector2(0.1f, 2.6f), 0.15f), (new Vector2(0.1f, 2.2f), 0.07f), (new Vector2(0.3f, 0.95f), 0.13f), (new Vector2(0.75f, 1.2f), 0.09f), (new Vector2(0.3f, 0.45f), 0.06f) };
            foreach (var (c, r) in planets) Engrave(v, t, left, Arc(c, r, 0f, 360f, 24), 0.008f);
            if (!left) { Engrave(v, t, false, Seg(new Vector2(0.1f, 3.4f), new Vector2(0.1f, 1.9f))); Engrave(v, t, false, Seg(new Vector2(0.3f, 1.3f), new Vector2(0.3f, 0.3f))); }
            else Engrave(v, t, true, Seg(new Vector2(-1.05f, 2.2f), new Vector2(-0.2f, 1.75f)));
        }

        static void BuildLunarDoor(Transform root, MoonMats m, Props props, Transform leftLeaf, Transform rightLeaf)
        {
            var rib = Dissolve("Moon_rib", Gray(0.6f), 0.35f, false);
            var engrave = Dissolve("Moon_engrave", Gray(0.24f), 0.2f, false);
            engrave.EnableKeyword("_EMISSION"); engrave.SetColor("_EmissionColor", Color.black);
            var leaf = Dissolve("Moon_doorleaf", Gray(0.66f), 0.3f, false);
            float y0 = L.Lower;

            // ---- the door: two leaves (each with its engravings), the S-seam line (lights up), the arch frame
            MeshObject("LeafL", LeafMesh(true, "Moon_DoorLeafL"), leaf, leftLeaf);
            MeshObject("LeafR", LeafMesh(false, "Moon_DoorLeafR"), leaf, rightLeaf);
            var er = new List<Renderer>();
            foreach (var (lf, tr) in new[] { (true, leftLeaf), (false, rightLeaf) })
            {
                var v = new List<Vector3>(); var t = new List<int>();
                LeafEngravings(v, t, lf);
                er.Add(MeshObject(lf ? "EngraveL" : "EngraveR", SaveMesh(SmoothFinish(v, t), lf ? "Moon_DoorEngraveL" : "Moon_DoorEngraveR"), engrave, tr, false).GetComponent<Renderer>());
            }
            {
                var v = new List<Vector3>(); var t = new List<int>();
                var seam = new List<Vector3>();
                for (float y = 0.02f; y <= ArchH - 0.02f; y += 0.04f) seam.Add(DoorP(SeamX(y), y, 0.007f));
                Sweep(v, t, seam, 0.013f, false, 5);
                // the seam glow belongs to the left leaf's edge (it parts with it)
                er.Insert(0, MeshObject("SeamGlow", SaveMesh(SmoothFinish(v, t), "Moon_DoorSeam"), engrave, leftLeaf, false).GetComponent<Renderer>());
            }
            props.doorEngrave = er.ToArray();
            props.doorRight = rightLeaf;
            {
                // frame: the wall opening is a rectangle; fill everything outside the arch (exact edges), and a raised pale rim
                var v = new List<Vector3>(); var t = new List<int>();
                Strips(v, t, y => -1.3f, y => -ArchW(y), 0f, 3.75f, 0.02f, false);
                Strips(v, t, y => ArchW(y), y => 1.3f, 0f, 3.75f, 0.02f, false);
                MeshObject("DoorFrameFill", SaveMesh(Finish(v, t), "Moon_DoorFrameFill"), rib, root);
                var rv = new List<Vector3>(); var rt = new List<int>();
                var rim = new List<Vector3>();
                for (float y = 0f; y <= ArchSpring; y += 0.2f) rim.Add(DoorP(-ArchHalf - 0.04f, y, 0.06f));
                for (int i = 0; i <= 24; i++) { float y = Mathf.Lerp(ArchSpring, ArchH, i / 24f); float x = ArchCx - Mathf.Sqrt(Mathf.Max(0f, (ArchCx + ArchHalf) * (ArchCx + ArchHalf) - (y - ArchSpring) * (y - ArchSpring))); rim.Add(DoorP(x - 0.04f, y + 0.02f, 0.06f)); }
                for (int i = 24; i >= 0; i--) { float y = Mathf.Lerp(ArchSpring, ArchH, i / 24f); float x = -(ArchCx - Mathf.Sqrt(Mathf.Max(0f, (ArchCx + ArchHalf) * (ArchCx + ArchHalf) - (y - ArchSpring) * (y - ArchSpring)))); rim.Add(DoorP(x + 0.04f, y + 0.02f, 0.06f)); }
                for (float y = ArchSpring; y >= 0f; y -= 0.2f) rim.Add(DoorP(ArchHalf + 0.04f, y, 0.06f));
                Sweep(rv, rt, rim, 0.05f, false, 8);
                MeshObject("DoorRim", SaveMesh(SmoothFinish(rv, rt), "Moon_DoorRim"), rib, root);
            }

            // ---- the round three-spoke emblem beside the door (the shape's outline glows in it after the vision)
            {
                var c = L.DoorSymbol; var v = new List<Vector3>(); var t = new List<int>();
                List<Vector3> Ring(float r, float z) { var l = new List<Vector3>(); for (int i = 0; i <= 48; i++) { float a = i / 48f * Mathf.PI * 2f; l.Add(c + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, z)); } return l; }
                Sweep(v, t, Ring(0.47f, 0.02f), 0.05f, true, 8);
                Sweep(v, t, Ring(0.33f, 0.015f), 0.025f, true, 6);
                Sweep(v, t, Ring(0.08f, 0.015f), 0.02f, true, 6);
                foreach (var a in new[] { 90f, 210f, 330f }) { var d = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad), 0f); Sweep(v, t, new[] { c + d * 0.08f + Vector3.forward * 0.015f, c + d * 0.33f + Vector3.forward * 0.015f }, 0.02f, false, 6); }
                MeshObject("Emblem", SaveMesh(SmoothFinish(v, t), "Moon_Emblem"), rib, root);
                // the mechanism's inner ring + hub light up when 7 touches it (as the figure does in the vision)
                var gv = new List<Vector3>(); var gt = new List<int>();
                Sweep(gv, gt, Ring(0.255f, 0.03f), 0.012f, true, 5);
                Sweep(gv, gt, Ring(0.045f, 0.03f), 0.012f, true, 5);
                props.doorSymbol = MeshObject("MechanismGlow", SaveMesh(SmoothFinish(gv, gt), "Moon_MechanismGlow"), engrave, root, false).GetComponent<Renderer>();
                var dv = new List<Vector3>(); var dt = new List<int>();
                Ellipsoid(dv, dt, c + Vector3.back * 0.005f, new Vector3(0.5f, 0.5f, 0.03f), 6, 24);
                MeshObject("EmblemRecess", SaveMesh(SmoothFinish(dv, dt), "Moon_EmblemRecess"), m.ancientDark, root, false);
            }

            // ---- QF7: in the present the lock is buried behind a collapse - a broken rib leaning across the wall and stone
            // chunks piled against it, the emblem hidden behind them. The vision shows it intact; afterwards 7 clears the
            // pile with telekinesis (Chapter1Director.UncoverLock: each piece flies off into the chamber's east corner).
            {
                var pile = new GameObject("LockRubble").transform; pile.SetParent(root, false);
                pile.position = new Vector3(L.DoorSymbol.x, y0, L.VisionMin.y);
                var c = L.DoorSymbol;
                int seed = 1700;
                // the broken rib end: from the floor out in the chamber up across the emblem to the wall above it
                {
                    var v = new List<Vector3>(); var t = new List<int>();
                    var a = new Vector3(15.05f, y0 + 0.12f, -37.05f); var b = new Vector3(12.7f, y0 + 2.55f, -37.86f);
                    var mid = (a + b) * 0.5f;
                    Tube(v, t, new[] { a - mid, Vector3.Lerp(a, b, 0.35f) - mid + new Vector3(0f, 0.08f, 0.05f), Vector3.Lerp(a, b, 0.7f) - mid + new Vector3(0f, 0.04f, 0f), b - mid }, 0.27f, 0.2f);
                    var go = MeshObject("RubbleRib", SaveMesh(SmoothFinish(v, t), "Moon_RubbleRib"), rib, pile);
                    go.transform.position = mid;
                }
                // two leaning slabs straight over the emblem, chunks heaped in front and around
                foreach (var (nm, pos, size, rot) in new[] {
                    ("RubbleSlabA", new Vector3(c.x + 0.15f, y0 + 0.95f, -37.74f), new Vector3(1.25f, 1.9f, 0.22f), new Vector3(-9f, 6f, 7f)),
                    ("RubbleSlabB", new Vector3(c.x - 0.35f, y0 + 1.05f, -37.58f), new Vector3(0.9f, 1.55f, 0.2f), new Vector3(-14f, -10f, -11f)) })
                {
                    var sl = GameObject.CreatePrimitive(PrimitiveType.Cube); sl.name = nm; sl.transform.SetParent(pile, false);
                    Object.DestroyImmediate(sl.GetComponent<Collider>());
                    sl.transform.position = pos; sl.transform.localScale = size; sl.transform.rotation = Quaternion.Euler(rot);
                    sl.GetComponent<MeshRenderer>().sharedMaterial = m.ancient;
                }
                foreach (var (x, y, z, s) in new[] { (13.2f, 0.28f, -37.35f, 0.42f), (14.15f, 0.25f, -37.25f, 0.38f), (13.65f, 0.6f, -37.45f, 0.36f),
                                                      (12.75f, 0.22f, -37.6f, 0.3f), (14.6f, 0.18f, -37.55f, 0.28f), (13.9f, 1.75f, -37.62f, 0.27f),
                                                      (13.3f, 1.95f, -37.7f, 0.24f), (13.75f, 0.2f, -36.9f, 0.22f) })
                {
                    var v = new List<Vector3>(); var t = new List<int>();
                    Lump(v, t, Vector3.zero, new Vector3(s * 1.25f, s * 0.8f, s), 0.4f, seed++);
                    var go = MeshObject($"RubbleChunk{seed}", SaveMesh(SmoothFinish(v, t), $"Moon_RubbleChunk{seed}"), seed % 2 == 0 ? m.ancientDark : rib, pile);
                    go.transform.position = new Vector3(x, y0 + y, z);
                }
                props.lockRubble = pile;
                props.extraObstacles.Add(new Vector3(13.7f, -37.45f, 0.75f));       // removed at runtime once the pile is cleared
                // Round 12 (QF8 open spot): the tight slot between the door's right jamb and the moved ruin column - the follow
                // camera had no room there (sweep: 0 % from the sides). Nothing needs her there: it is closed off.
                props.extraObstacles.Add(new Vector3(18.0f, -37.55f, 0.8f));
            }

            // ---- floor engravings: concentric circles, a line of small circles leading to the door
            {
                var v = new List<Vector3>(); var t = new List<int>();
                var fc = new Vector3(L.SealedDoor.x, y0 + 0.004f, -34.6f);
                foreach (var r in new[] { 1.0f, 1.8f, 2.6f, 3.3f })
                {
                    var l = new List<Vector3>(); for (int i = 0; i <= 96; i++) { float a = i / 96f * Mathf.PI * 2f; l.Add(fc + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r)); }
                    // worn: break the outer circles in places
                    var run = new List<Vector3>();
                    for (int i = 0; i < l.Count; i++) { bool worn = r > 2f && (i % 37) < 5; if (!worn) run.Add(l[i]); else { if (run.Count > 1) Sweep(v, t, new List<Vector3>(run), 0.013f, false, 4); run.Clear(); } }
                    if (run.Count > 1) Sweep(v, t, run, 0.013f, false, 4);
                }
                Sweep(v, t, new[] { new Vector3(L.SealedDoor.x, y0 + 0.004f, -31.2f), new Vector3(L.SealedDoor.x, y0 + 0.004f, -37.95f) }, 0.012f, false, 4);
                foreach (var z in new[] { -31.8f, -33.0f, -34.6f, -36.2f, -37.3f })
                {
                    float r = z == -34.6f ? 0.22f : 0.1f; var l = new List<Vector3>();
                    for (int i = 0; i <= 24; i++) { float a = i / 24f * Mathf.PI * 2f; l.Add(new Vector3(L.SealedDoor.x + Mathf.Cos(a) * r, y0 + 0.005f, z + Mathf.Sin(a) * r)); }
                    Sweep(v, t, l, 0.012f, true, 4);
                }
                MeshObject("FloorEngraving", SaveMesh(SmoothFinish(v, t), "Moon_FloorEngraving"), engrave, root, false);
            }

            // ---- flowing ribs (the chamber): framing the door, vaults across, diagonals on the walls - some broken
            {
                var v = new List<Vector3>(); var t = new List<int>();
                float x0 = L.VisionMin.x, x1 = L.VisionMax.x, zs = L.VisionMin.y;
                Vector3 P(float x, float y, float z) => new Vector3(x, y0 + y, z);
                Tube(v, t, new[] { P(14.6f, 0f, zs + 0.45f), P(14.75f, 1.8f, zs + 0.2f), P(15.3f, 3.6f, zs + 0.12f), P(16.5f, 4.8f, zs + 0.15f), P(17.7f, 5.9f, zs + 0.5f), P(18.9f, 6.9f, zs + 1.2f) }, 0.46f, 0.2f);
                Tube(v, t, new[] { P(18.4f, 0f, zs + 0.45f), P(18.25f, 1.8f, zs + 0.2f), P(17.7f, 3.6f, zs + 0.12f), P(16.5f, 4.8f, zs + 0.15f), P(15.3f, 5.9f, zs + 0.5f), P(14.1f, 6.9f, zs + 1.2f) }, 0.46f, 0.2f);
                // vaults: west half / east half meeting high in the middle (two are broken off)
                var vz = new[] { -30.4f, -32.4f, -34.4f, -36.6f };
                for (int i = 0; i < vz.Length; i++)
                {
                    float z = vz[i];
                    bool brokenW = i == 1, brokenE = i == 2;
                    var w = new List<Vector3> { P(x0 + 0.15f, 0f, z), P(x0 + 0.4f, 2.6f, z + 0.25f), P(x0 + 1.6f, 5.2f, z + 0.6f), P(16.5f, 6.65f, z + 0.9f) };
                    var e = new List<Vector3> { P(x1 - 0.15f, 0f, z), P(x1 - 0.4f, 2.6f, z + 0.25f), P(x1 - 1.6f, 5.2f, z + 0.6f), P(16.5f, 6.65f, z + 0.9f) };
                    if (brokenW) w.RemoveRange(3, 1);
                    if (brokenE) e.RemoveRange(2, 2);
                    Tube(v, t, w, 0.36f, brokenW ? 0.22f : 0.15f);
                    Tube(v, t, e, 0.36f, brokenE ? 0.26f : 0.15f);
                }
                // diagonals on both side walls, crossing and growing into the vaults
                foreach (var x in new[] { x0 + 0.12f, x1 - 0.12f })
                {
                    float ix = x < 16.5f ? 0.35f : -0.35f;
                    Tube(v, t, new[] { P(x, 0f, -29.9f), P(x + ix * 0.3f, 2.4f, -31.8f), P(x + ix, 5.0f, -34.6f), P(x + ix * 2.5f, 6.6f, -37.2f) }, 0.28f, 0.12f);
                    Tube(v, t, new[] { P(x, 0f, -37.6f), P(x + ix * 0.3f, 2.2f, -35.6f), P(x + ix, 4.6f, -33.0f), P(x + ix * 2f, 6.2f, -31.0f) }, 0.26f, 0.14f);
                }
                MeshObject("ChamberRibs", SaveMesh(SmoothFinish(v, t), "Moon_ChamberRibs"), rib, root);
            }
            // fallen chunks, rib ends on the floor (away from her paths), dust
            {
                var v = new List<Vector3>(); var t = new List<int>();
                int seed = 900;
                // QF5: the two lumps by the door moved to the east side (the mechanism is on the west side now)
                foreach (var (x, z, s) in new[] { (19.5f, -36.9f, 0.5f), (14.1f, -31.0f, 0.38f), (19.4f, -30.7f, 0.45f), (13.1f, -31.6f, 0.5f), (13.2f, -33.9f, 0.3f), (18.8f, -32.3f, 0.32f), (18.6f, -37.5f, 0.25f) })
                    Lump(v, t, new Vector3(x, y0 + s * 0.35f, z), new Vector3(s * 1.3f, s * 0.7f, s), 0.35f, seed++);
                MeshObject("FallenChunks", SaveMesh(SmoothFinish(v, t), "Moon_FallenChunks"), rib, root);
                foreach (var (x, z) in new[] { (19.5f, -36.9f), (13.1f, -31.6f), (19.4f, -30.7f) }) props.extraObstacles.Add(new Vector3(x, z, 0.55f));
            }
            // skylight slits high up (soft light through gaps in the shell), and low drifting fog sheets
            foreach (var z in new[] { -31.4f, -33.4f, -35.5f })
            {
                Block($"Skylight{z}", new Vector3(14.0f, y0 + 6.93f, z - 0.12f), new Vector3(19.0f, y0 + 6.99f, z + 0.12f), m.glow, root, false);
                PointLight($"SkylightLight{z}", new Vector3(16.5f, y0 + 6.3f, z), 7f, 0.45f, root);
            }
            foreach (var (y, r, a) in new[] { (0.22f, 4.4f, 0.22f), (0.6f, 3.6f, 0.14f) })
            {
                var v = new List<Vector3>(); var t = new List<int>();
                var c = new Vector3(16.5f, y0 + y, -34.2f);
                for (int k = 0; k < 40; k++)
                {
                    var p = c + new Vector3(Mathf.Cos(k / 40f * Mathf.PI * 2f) * r, 0f, Mathf.Sin(k / 40f * Mathf.PI * 2f) * r);
                    var q = c + new Vector3(Mathf.Cos((k + 1) / 40f * Mathf.PI * 2f) * r, 0f, Mathf.Sin((k + 1) / 40f * Mathf.PI * 2f) * r);
                    Quad(v, t, c, q, p, c); Quad(v, t, c, p, q, c);
                }
                var mesh = Finish(v, t);
                var vs = mesh.vertices; var cols = new Color[vs.Length];
                for (int i = 0; i < vs.Length; i++) cols[i] = new Color(1f, 1f, 1f, a * (1f - Mathf.Clamp01((new Vector2(vs[i].x - c.x, vs[i].z - c.z)).magnitude / r)));
                mesh.colors = cols;
                MeshObject($"ChamberFog{y}", SaveMesh(mesh, $"Moon_ChamberFog{y}"), m.ghost, root, false);
            }
        }

        /// <summary>The same flowing ribs through the hall exit (pointed rib arches, two broken) and across the hall.</summary>
        static void BuildCurvyApproach(Transform root, MoonMats m)
        {
            var rib = Dissolve("Moon_rib", Gray(0.6f), 0.35f, false);
            var v = new List<Vector3>(); var t = new List<int>();
            float y0 = L.Lower, xa = L.PassageX.x, xb = L.PassageX.y;
            int k = 0;
            for (float z = L.HallMin.y - 0.7f; z > L.VisionMax.y + 0.4f; z -= 1.05f, k++)
            {
                var w = new List<Vector3> { new Vector3(xa + 0.03f, y0, z), new Vector3(xa + 0.12f, y0 + 1.8f, z - 0.1f), new Vector3(16.5f, y0 + 2.88f, z - 0.25f) };
                var e = new List<Vector3> { new Vector3(xb - 0.03f, y0, z), new Vector3(xb - 0.12f, y0 + 1.8f, z - 0.1f), new Vector3(16.5f, y0 + 2.88f, z - 0.25f) };
                if (k == 2) w.RemoveAt(2);
                if (k == 3) e.RemoveAt(2);
                Tube(v, t, w, 0.13f, 0.08f, 8, 6);
                Tube(v, t, e, 0.13f, 0.08f, 8, 6);
            }
            // across the hall: four great ribs from wall to wall over the pillars, one cracked through
            foreach (var z in new[] { -10.6f, -14.4f, -18.4f, -22.8f })
            {
                bool broken = z == -18.4f;
                var a = new List<Vector3> { new Vector3(L.HallMin.x + 0.05f, y0, z), new Vector3(L.HallMin.x + 0.6f, y0 + 3.6f, z + 0.4f), new Vector3(12.8f, y0 + 5.7f, z + 0.9f), new Vector3(16.5f, y0 + 6.25f, z + 1.1f) };
                var b = new List<Vector3> { new Vector3(L.HallMax.x - 0.05f, y0, z), new Vector3(L.HallMax.x - 0.6f, y0 + 3.6f, z + 0.4f), new Vector3(20.2f, y0 + 5.7f, z + 0.9f), new Vector3(16.5f, y0 + 6.25f, z + 1.1f) };
                if (broken) b.RemoveRange(2, 2);
                Tube(v, t, a, 0.34f, 0.16f);
                Tube(v, t, b, 0.34f, broken ? 0.24f : 0.16f);
            }
            MeshObject("CurvyApproachRibs", SaveMesh(SmoothFinish(v, t), "Moon_CurvyApproachRibs"), rib, root);
            var cv = new List<Vector3>(); var ct = new List<int>();
            Lump(cv, ct, new Vector3(23.2f, y0 + 0.2f, -17.9f), new Vector3(0.6f, 0.35f, 0.5f), 0.35f, 951);
            Lump(cv, ct, new Vector3(22.6f, y0 + 0.15f, -18.9f), new Vector3(0.4f, 0.25f, 0.35f), 0.35f, 952);
            MeshObject("HallFallenRib", SaveMesh(SmoothFinish(cv, ct), "Moon_HallFallenRib"), rib, root);
        }
    }
}
