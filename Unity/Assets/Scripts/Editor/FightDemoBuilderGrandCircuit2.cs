using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using T = Seven.GcTrack;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 21 - Grand Circuit Stage 2 (Docs/RACING_MODE.md): the five new tracks' worlds, built round their ribbons (the ribbons are
    /// built at load from the same GcTrack definitions). Each in its world's monochrome look with soft glows (Kyle's misty grayscale):
    /// Venus Gardens (the gardens, the vine canopies over the S, the temple inside the right-hander, the root chasm under the jump, the
    /// giant vine trunk the spiral climbs round, the burrows of the shortcut), Mars Canyon (the canyon floor and its mesas, the two
    /// kingdoms' walled cities, the shared crossing's bridge, the ravine under the big jumps, the carved arch round the loop, the
    /// maintenance route), Europa Under-Ice (the ice ceiling with the light coming through, the sea floor, the glass tube, the great
    /// chamber, the settlement, the Cetaceans swimming alongside), Asteroid Foundry (the machine city: conveyors, the great flywheel the
    /// loop goes round, the furnace hall under the jump, the rail carrier on the shortcut; the presses are built at load by GcGame) and
    /// the Mothership (the egg's shell round everything, the interior city rising up its curve, the suspended bridge, the memory
    /// structures, the transfer chamber under the jump, the quiet passage).
    /// Shared: the ground is a height field carved clear under the ribbon (and deep under every jump); supports stand only where
    /// nothing of the track passes under them.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        // ------------------------------------------------------------------------------------------------ shared helpers
        static Material GcEm(string n, float gray, float em, float smooth = 0.4f) { var mm = Dissolve(n, Gray(gray), smooth, false); mm.EnableKeyword("_EMISSION"); mm.SetColor("_EmissionColor", Color.white * em); EditorUtility.SetDirty(mm); return mm; }

        /// <summary>The track (+ shortcut) sampled every 3 m in plan, bucketed on a 20 m grid: for the ground and the supports.</summary>
        class GcNearIndex
        {
            readonly Dictionary<(int, int), List<(Vector3 p, float hw, T.F f)>> _g = new Dictionary<(int, int), List<(Vector3, float, T.F)>>();
            public GcNearIndex(T t)
            {
                void Add(T tt) { for (int i = 0; i < tt.Count; i += 3) { var p = tt.P[i]; var key = ((int)Mathf.Floor(p.x / 20f), (int)Mathf.Floor(p.z / 20f)); if (!_g.TryGetValue(key, out var l)) _g[key] = l = new List<(Vector3, float, T.F)>(); l.Add((p, tt.HalfW[i], tt.Flag[i])); } }
                Add(t); if (t.Branch != null) Add(t.Branch);
            }
            /// <summary>The lowest track height within (half-width + margin) of (x, z) in plan, or +inf; gap = a jump gap is there.</summary>
            public float LowestNear(float x, float z, float margin, out bool gap)
            {
                gap = false; float best = float.PositiveInfinity; int cx = (int)Mathf.Floor(x / 20f), cz = (int)Mathf.Floor(z / 20f); int r = Mathf.CeilToInt((margin + 12f) / 20f);
                for (int a = -r; a <= r; a++) for (int b = -r; b <= r; b++)
                {
                    if (!_g.TryGetValue((cx + a, cz + b), out var l)) continue;
                    foreach (var (p, hw, f) in l)
                    {
                        float dx = p.x - x, dz = p.z - z, lim = hw + margin;
                        if (dx * dx + dz * dz > lim * lim) continue;
                        if ((f & T.F.Gap) != 0) gap = true;
                        if (p.y < best) best = p.y;
                    }
                }
                return best;
            }
        }

        /// <summary>The ground's height field H(x, z) carved to stay clear under the ribbon (1.8 m within 26 m of it) and dropped deep under
        /// every jump gap (chasmDepth below it). The ground mesh AND every prop / support placement use this one function.</summary>
        static System.Func<float, float, float> GcCarve(GcNearIndex near, System.Func<float, float, float> H, float chasmDepth) => (x, z) =>
        {
            float h = H(x, z);
            float y2 = near.LowestNear(x, z, 26f, out bool gap2);
            if (!float.IsInfinity(y2)) { h = Mathf.Min(h, y2 - 1.8f); if (gap2) h = Mathf.Min(h, y2 - chasmDepth); }
            return h;
        };
        /// <summary>A height-field ground round the track (the carved height, <see cref="GcCarve"/>).</summary>
        static void GcGround(Transform set, string name, Material m, Vector3 c, float span, int n, System.Func<float, float, float> Hc)
        {
            var v = new List<Vector3>(); var tr = new List<int>();
            for (int j = 0; j <= n; j++) for (int i = 0; i <= n; i++) { float x = c.x - span / 2 + span * i / n, z = c.z - span / 2 + span * j / n; v.Add(new Vector3(x, Hc(x, z), z)); }
            for (int j = 0; j < n; j++) for (int i = 0; i < n; i++) { int a = j * (n + 1) + i, b = a + n + 1; tr.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            MeshObject(name, SaveMesh(SmoothFinish(v, tr), "Gc_" + name), m, set, false);
        }

        /// <summary>Supports under the upright parts of the ribbon, every <paramref name="every"/> m, down to ground(x, z) - only where no
        /// other part of the track passes underneath (the spiral, crossings) and never under a loop / the corkscrew / a gap.</summary>
        static void GcSupportsH(T t, AK k, Material mat, System.Func<float, float, float> ground, float every, float r)
        {
            void One(T tt)
            {
                for (int i = 0; i < tt.Count; i += (int)every)
                {
                    var f = tt.Flag[i]; if ((f & (T.F.Gap | T.F.Loop | T.F.Cork)) != 0) continue;
                    var up = tt.R[i] * Vector3.up; if (up.y < 0.85f) continue;
                    var p = tt.P[i] - up * 0.65f; float gy = ground(p.x, p.z); float h = p.y - gy; if (h < 1.2f) continue;
                    bool under = false;
                    foreach (var o in new[] { t, t.Branch }) { if (o == null) continue; for (int j = 0; j < o.Count && !under; j += 3) { if (o.P[j].y > p.y - 3f || o.P[j].y < gy) continue; float dx = o.P[j].x - p.x, dz = o.P[j].z - p.z; if (dx * dx + dz * dz < (o.HalfW[j] + r + 1.5f) * (o.HalfW[j] + r + 1.5f)) under = true; } }
                    if (under) continue;
                    var (v, trr) = k.Of(mat);
                    Append(v, trr, Lathe(new[] { new Vector2(0f, 0f), new Vector2(r * 1.4f, 0f), new Vector2(r, 0.6f), new Vector2(r, h - 0.4f), new Vector2(r * 1.5f, h), new Vector2(0f, h) }, 0f, 360f, 10, null, new Vector3(p.x, gy, p.z)));
                }
            }
            One(t); if (t.Branch != null) One(t.Branch);
        }
        static List<int> GcIdx(T t, T.F f) => Enumerable.Range(0, t.Count).Where(i => (t.Flag[i] & f) != 0).ToList();
        static Vector3 GcCentre(T t, List<int> idx) => idx.Count == 0 ? Vector3.zero : idx.Aggregate(Vector3.zero, (s, i) => s + t.P[i]) / idx.Count;
        static Vector3 GcFlat(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward; }
        /// <summary>Points beside the ribbon on one side (plan right * side), every <paramref name="step"/> m, for scenery rows.</summary>
        static IEnumerable<(int i, Vector3 at, Vector3 right)> GcBeside(T t, float side, float off, int step, T.F skip = T.F.Loop | T.F.Cork | T.F.Gap)
        {
            for (int i = 0; i < t.Count; i += step)
            {
                if ((t.Flag[i] & skip) != 0) continue; var rt = GcFlat(t.R[i] * Vector3.right);
                yield return (i, t.P[i] + rt * side * (t.HalfW[i] + off), rt);
            }
        }
        /// <summary>A loop's frame: arched trusses outside the ribbon round the circle (+ hangers to the ground where low).</summary>
        static void GcLoopFramesH(T t, AK k, Material mat, System.Func<float, float, float> ground)
        {
            for (int i = 0; i < t.Count; i++)
            {
                if ((t.Flag[i] & T.F.Loop) == 0 || i % 10 != 0) continue;
                var up = t.R[i] * Vector3.up; var right = t.R[i] * Vector3.right; var (v, tr) = k.Of(mat);
                foreach (var sg in new[] { -1f, 1f })
                {
                    var a = t.P[i] + right * sg * (t.HalfW[i] + 0.6f) - up * 0.6f; var b = a - up * 1.6f;
                    Sweep(v, tr, new[] { a, b }, 0.25f, false, 6);
                    float gy = ground(b.x, b.z); if (up.y > 0.3f && b.y - gy < 30f && b.y > gy) Sweep(v, tr, new[] { b, new Vector3(b.x, gy, b.z) }, 0.3f, false, 6);
                }
            }
        }
        /// <summary>A ring round the track's heartline at sample i (radius r about the point h above the surface): tubes, arches.</summary>
        static List<Vector3> GcRing(T t, int i, float r, float h, int seg = 20, float a0 = 0f, float a1 = 360f)
        {
            var p = t.P[i] + t.R[i] * Vector3.up * h; var right = t.R[i] * Vector3.right; var up = t.R[i] * Vector3.up; var pts = new List<Vector3>();
            for (int s = 0; s <= seg; s++) { float a = Mathf.Lerp(a0, a1, s / (float)seg) * Mathf.Deg2Rad; pts.Add(p + right * Mathf.Cos(a) * r + up * Mathf.Sin(a) * r); }
            return pts;
        }
        /// <summary>A floor laid in panels: seams on a grid (every <paramref name="step"/> m, within <paramref name="radius"/> of c) following the
        /// carved ground, every third one in the light material (thin flat quads, 5 cm above the ground).</summary>
        static void GcSeams(AK k, Material seam, Material light, Vector3 c, System.Func<float, float, float> Hc, float step, float radius, float w = 0.22f)
        {
            var (sv, st) = k.Of(seam); var (lv, lt) = k.Of(light); int lines = Mathf.CeilToInt(radius / step), segs = Mathf.CeilToInt(radius / 5f);
            for (int axis = 0; axis < 2; axis++)
                for (int li = -lines; li <= lines; li++)
                {
                    bool glow = li % 3 == 0; var (v, tr) = glow ? (lv, lt) : (sv, st); float ww = glow ? w * 1.35f : w;
                    for (int sg = -segs; sg < segs; sg++)
                    {
                        float a0 = sg * 5f, a1 = (sg + 1) * 5f, o = li * step;
                        Vector3 P(float a) { float x = c.x + (axis == 0 ? a : o), z = c.z + (axis == 0 ? o : a); return new Vector3(x, Hc(x, z) + 0.05f, z); }
                        var p0 = P(a0); var p1 = P(a1); if (new Vector2(p0.x - c.x, p0.z - c.z).magnitude > radius) continue;
                        var side = (axis == 0 ? Vector3.forward : Vector3.right) * ww; int b0 = v.Count;
                        v.Add(p0 - side); v.Add(p0 + side); v.Add(p1 + side); v.Add(p1 - side); tr.AddRange(new[] { b0, b0 + 1, b0 + 2, b0, b0 + 2, b0 + 3 });
                    }
                }
        }
        static float GcPerlin(float x, float z, float f, float amp) => (Mathf.PerlinNoise(x * f + 31.7f, z * f + 11.3f) - 0.5f) * 2f * amp;

        // ------------------------------------------------------------------------------------------------ Venus Gardens
        static Transform BuildGcVenusSet(T t)
        {
            var set = new GameObject("VenusSet").transform;
            var grass = Dissolve("Gc_venus_grass", Gray(0.36f), 0.15f, false); var soil = Dissolve("Gc_venus_soil", Gray(0.22f), 0.1f, false);
            var bark = Dissolve("Gc_venus_bark", Gray(0.28f), 0.12f, false); var barkLight = Dissolve("Gc_venus_bark_light", Gray(0.46f), 0.12f, false);
            var leaf = Dissolve("Gc_venus_leaf", Gray(0.5f), 0.25f, false); var leafDark = Dissolve("Gc_venus_leaf_dark", Gray(0.3f), 0.2f, false);
            var stone = Dissolve("Gc_venus_stone", Gray(0.62f), 0.3f, false); var stoneDark = Dissolve("Gc_venus_stone_dark", Gray(0.4f), 0.25f, false);
            var crystal = GcEm("Gc_venus_crystal", 0.9f, 1.3f, 0.7f); var glowSoft = GcEm("Gc_venus_glow_soft", 0.7f, 0.5f);
            var rnd = new System.Random(2101); float R() => (float)rnd.NextDouble();
            var near = new GcNearIndex(t);
            var c = (new Vector3(t.P.Min(p => p.x), 0f, t.P.Min(p => p.z)) + new Vector3(t.P.Max(p => p.x), 0f, t.P.Max(p => p.z))) * 0.5f;
            float gy0 = -1.4f;
            System.Func<float, float, float> H = (x, z) => { float d = new Vector2(x - c.x, z - c.z).magnitude; return gy0 + GcPerlin(x, z, 0.012f, 2.5f) + Mathf.Clamp01((d - 520f) / 500f) * 120f; };
            var Hc = GcCarve(near, H, 38f); GcGround(set, "VenusGround", grass, c, 2600f, 110, Hc);
            var k = new AK();
            // the giant vine trunk the spiral climbs round: a great tapering trunk with vines wound up it, crowned with a leaf canopy
            var sp = GcIdx(t, T.F.SetPiece);
            if (sp.Count > 0)
            {
                var tc = GcCentre(t, sp); float top = sp.Max(i => t.P[i].y) + 40f; float baseY = Hc(tc.x, tc.z);
                float rr = 22f;   // the spiral's radius is ~46 m (inner edge ~38 m): the trunk + its flare (29 m) stand clear inside it
                { var (v, tr) = k.Of(bark); Append(v, tr, Lathe(new[] { new Vector2(0f, 0f), new Vector2(rr * 1.3f, 0f), new Vector2(rr, 8f), new Vector2(rr * 0.85f, top - baseY - 30f), new Vector2(rr * 0.55f, top - baseY), new Vector2(0f, top - baseY + 4f) }, 0f, 360f, 28, null, new Vector3(tc.x, baseY, tc.z))); }
                for (int vi = 0; vi < 5; vi++)
                {
                    var path = new List<Vector3>(); float a0 = vi * 72f;
                    for (int s = 0; s <= 60; s++) { float u = s / 60f; float y = baseY + u * (top - baseY - 6f); float rad = Mathf.Lerp(rr * 1.02f, rr * 0.6f, u) + 1.2f; float a = (a0 + u * 540f) * Mathf.Deg2Rad; path.Add(new Vector3(tc.x + Mathf.Cos(a) * rad, y, tc.z + Mathf.Sin(a) * rad)); }
                    var (v, tr) = k.Of(vi % 2 == 0 ? barkLight : leafDark); Sweep(v, tr, path, 1.6f, false, 8);
                }
                { var (v, tr) = k.Of(leaf); for (int q = 0; q < 9; q++) { float a = q / 9f * Mathf.PI * 2f; Ellipsoid(v, tr, new Vector3(tc.x + Mathf.Cos(a) * 40f, top + 6f + R() * 8f, tc.z + Mathf.Sin(a) * 40f), new Vector3(34f, 10f, 34f), 6, 12); } Ellipsoid(v, tr, new Vector3(tc.x, top + 14f, tc.z), new Vector3(48f, 14f, 48f), 7, 14); }
                { var (v, tr) = k.Of(crystal); for (int q = 0; q < 14; q++) { float a = q / 14f * Mathf.PI * 2f, y = baseY + 6f + q * (top - baseY - 10f) / 14f; Ellipsoid(v, tr, new Vector3(tc.x + Mathf.Cos(a) * (rr * 0.9f), y, tc.z + Mathf.Sin(a) * (rr * 0.9f)), new Vector3(1.1f, 1.8f, 1.1f), 4, 6); } }
            }
            // the vine canopies over the S: great arches of vine, leaf clusters along their tops, hanging lantern crystals on the arches
            for (int i = 0; i < t.Count; i += 9)
            {
                if ((t.Flag[i] & T.F.Tunnel) == 0) continue;
                var arch = GcRing(t, i, t.HalfW[i] + 2.5f, 0f, 14, 0f, 180f); var (v, tr) = k.Of(i % 18 == 0 ? bark : barkLight); Sweep(v, tr, arch, 0.7f, false, 6);
                var top = t.P[i] + t.R[i] * Vector3.up * (t.HalfW[i] + 2.5f); var (lv, lt) = k.Of(i % 27 == 0 ? leafDark : leaf); Ellipsoid(lv, lt, top + Vector3.up * 2f, new Vector3(9f, 3.4f, 7f), 5, 10);
                if (i % 18 == 0) { var (cv, ct) = k.Of(crystal); Ellipsoid(cv, ct, top - t.R[i] * Vector3.up * 1.4f, new Vector3(0.6f, 1.1f, 0.6f), 4, 6); }
            }
            // the temple inside the right-hander after the canopies: a stepped stone temple with columns and a glowing doorway
            {
                int ti = -1; for (int i = 1; i < t.Count; i++) if (t.Curv(i) > 0.5f && (t.Flag[i] & T.F.Tunnel) == 0) { ti = i + 60; break; }
                if (ti > 0 && ti < t.Count)
                {
                    var rt = GcFlat(t.R[ti] * Vector3.right); var at = t.P[ti] + rt * 70f; at.y = Hc(at.x, at.z); float yaw = Quaternion.LookRotation(-rt).eulerAngles.y;
                    var (v, tr) = k.Of(stone); var (dv, dt) = k.Of(stoneDark);
                    for (int s = 0; s < 5; s++) MBox(s % 2 == 0 ? v : dv, s % 2 == 0 ? tr : dt, at + Vector3.up * (s * 4.5f + 2.25f), new Vector3(60f - s * 9f, 4.5f, 46f - s * 7f), yaw, 0.03f);
                    for (int q = -3; q <= 3; q++) { var cp = at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(q * 7.5f, 0f, 26f); Append(v, tr, Lathe(new[] { new Vector2(0f, 0f), new Vector2(1.6f, 0f), new Vector2(1.2f, 0.8f), new Vector2(1.1f, 13f), new Vector2(1.7f, 14f), new Vector2(0f, 14f) }, 0f, 360f, 10, null, cp)); }
                    MBox(v, tr, at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 14.6f, 26f), new Vector3(56f, 1.4f, 4f), yaw, 0f);
                    var (gv, gt) = k.Of(glowSoft); MBox(gv, gt, at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 27f, 12f), new Vector3(6f, 7f, 0.3f), yaw, 0f);
                }
            }
            // the root chasm under the jump: great roots arching across it, far below the racers
            var gap = GcIdx(t, T.F.Gap);
            if (gap.Count > 0)
            {
                foreach (int q in new[] { gap[0], gap[gap.Count / 2], gap[gap.Count - 1] })
                {
                    var rt = GcFlat(t.R[q] * Vector3.right); var mid = t.P[q] + Vector3.down * 26f; var a = mid - rt * 45f; var b = mid + rt * 45f; a.y = Hc(a.x, a.z) + 2f; b.y = Hc(b.x, b.z) + 2f;
                    var path = new List<Vector3>(); for (int s = 0; s <= 12; s++) { float u = s / 12f; path.Add(Vector3.Lerp(a, b, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 10f); }
                    var (v, tr) = k.Of(bark); Sweep(v, tr, path, 2.2f + R(), false, 8);
                }
                var (cv, ct) = k.Of(crystal); foreach (int q in gap.Where((x, j) => j % 9 == 0)) { var p = t.P[q]; p.y = Hc(p.x, p.z) - 30f; Ellipsoid(cv, ct, p + Vector3.up * 3f, new Vector3(2.2f, 5f, 2.2f), 4, 6); }
            }
            // the burrows: the shortcut runs under root arches, soft crystal light inside
            if (t.Branch != null)
            {
                var b = t.Branch;
                for (int i = 8; i < b.Count - 8; i += 5)
                {
                    var arch = GcRing(b, i, b.HalfW[i] + 1.2f, 0f, 12, 0f, 180f); var (v, tr) = k.Of(i % 10 == 0 ? bark : soil); Sweep(v, tr, arch, 0.9f, false, 6);
                    if (i % 15 == 0) { var (cv, ct) = k.Of(crystal); Ellipsoid(cv, ct, b.P[i] + b.R[i] * Vector3.up * (b.HalfW[i] + 0.4f), new Vector3(0.5f, 0.9f, 0.5f), 4, 6); }
                }
            }
            // the gardens: trees with broad leaf crowns, flowering shrubs, lantern crystals on stems, along the run (clear of the ribbon)
            foreach (var sg in new[] { -1f, 1f })
                foreach (var (i, at0, rt) in GcBeside(t, sg, 14f, 22))
                {
                    var at = at0 + rt * sg * R() * 30f; if (!GcClear(t, at, 20f)) continue; at.y = Hc(at.x, at.z);   // (a crown up to 13 m wide must stay out of the road's air - the spiral climbs 42 m)
                    float u = R();
                    if (u < 0.55f)
                    {
                        float h = 14f + R() * 16f; var (v, tr) = k.Of(bark); Sweep(v, tr, new[] { at, at + Vector3.up * h * 0.6f + new Vector3(R() - 0.5f, 0f, R() - 0.5f) * 3f, at + Vector3.up * h }, 0.9f + R() * 0.6f, false, 7);
                        var (lv, lt) = k.Of(R() < 0.5f ? leaf : leafDark); Ellipsoid(lv, lt, at + Vector3.up * (h + 2f), new Vector3(8f + R() * 5f, 4f + R() * 2f, 8f + R() * 5f), 5, 10);
                    }
                    else if (u < 0.85f) { var (lv, lt) = k.Of(leafDark); Ellipsoid(lv, lt, at + Vector3.up * 2f, new Vector3(4f + R() * 3f, 2.5f, 4f + R() * 3f), 4, 8); }
                    else { var (v, tr) = k.Of(barkLight); Sweep(v, tr, new[] { at, at + Vector3.up * 6f }, 0.15f, false, 5); var (cv, ct) = k.Of(crystal); Ellipsoid(cv, ct, at + Vector3.up * 6.6f, new Vector3(0.5f, 0.9f, 0.5f), 4, 6); }
                }
            HillLine(k, new CtMats { hill = leafDark }, c, 1000f, 140f, 2102);
            GcSupportsH(t, k, barkLight, Hc, 9f, 0.7f); GcLoopFramesH(t, k, bark, Hc);
            k.Save("GcVenus", set);
            Log($"GC VENUS SET (R21): the trunk {(sp.Count > 0 ? "at the spiral's centre" : "MISSING")}, the canopies over {GcIdx(t, T.F.Tunnel).Count} m, the chasm under the jump, the burrows {(t.Branch != null ? t.Branch.Length.ToString("0") + " m" : "-")}");
            return set;
        }

        // ------------------------------------------------------------------------------------------------ Mars Canyon
        static Transform BuildGcMarsSet(T t)
        {
            var set = new GameObject("MarsSet").transform;
            var sand = Dissolve("Gc_mars_sand", Gray(0.5f), 0.1f, false); var rock = Dissolve("Gc_mars_rock", Gray(0.34f), 0.12f, false); var rockDark = Dissolve("Gc_mars_rock_dark", Gray(0.2f), 0.1f, false);
            var stone = Dissolve("Gc_mars_stone", Gray(0.6f), 0.25f, false); var stoneDark = Dissolve("Gc_mars_stone_dark", Gray(0.38f), 0.2f, false);
            var metal = Dissolve("Gc_mars_metal", Gray(0.42f), 0.6f, false, 0.6f); var lamp = GcEm("Gc_mars_lamp", 1f, 1.6f); var glowSoft = GcEm("Gc_mars_glow_soft", 0.7f, 0.45f);
            var rnd = new System.Random(2102); float R() => (float)rnd.NextDouble();
            var near = new GcNearIndex(t);
            var c = (new Vector3(t.P.Min(p => p.x), 0f, t.P.Min(p => p.z)) + new Vector3(t.P.Max(p => p.x), 0f, t.P.Max(p => p.z))) * 0.5f;
            float floorY = t.P.Min(p => p.y) - 3f;
            // the canyon: a floor round the circuit, mesas rising beyond it; the ravine (under the bridge and the jumps) cut deep
            var bridge = GcIdx(t, T.F.SetPiece);
            System.Func<float, float, float> H = (x, z) =>
            {
                float d = new Vector2(x - c.x, z - c.z).magnitude; float h = floorY + GcPerlin(x, z, 0.01f, 3f);
                float rise = Mathf.Clamp01((d - 600f) / 260f) * 160f + Mathf.Clamp01((d - 600f) / 260f) * GcPerlin(x, z, 0.004f, 40f);   // (the walls from 600 m: the first jump is 508 m out)
                // the canyon walls in strata: flat ledges with steep risers every 9 m (one long smooth slope read as a single tone)
                return h + rise;
            };
            System.Func<float, float, float> G = (x, z) =>
            {
                float h = H(x, z);
                // the ravine: a channel along the bridge's line, out across the circuit
                if (bridge.Count > 0) { var a = t.P[bridge[0]]; var b = t.P[bridge[bridge.Count - 1]]; var ab = new Vector2(b.x - a.x, b.z - a.z); var ap = new Vector2(x - a.x, z - a.z); float u = Vector2.Dot(ap, ab) / ab.sqrMagnitude; var perp = Vector2.Dot(ap, new Vector2(-ab.y, ab.x).normalized); if (u > 0.15f && u < 0.85f) h -= Mathf.Clamp01(1f - Mathf.Abs(perp) / 60f) * 0f; float across = Mathf.Abs(u - 0.5f) * ab.magnitude; h -= Mathf.Clamp01(1f - across / 50f) * 60f; }
                // the whole canyon in strata: flat ledges with steep risers every 18 m (wider than the ground grid, so they show) (the mesas, the trough across the ravine; one long
                // smooth slope read as a single tone from the air)
                float rel = h - (floorY - 80f), su = Mathf.Max(0f, rel) / 18f, fl = Mathf.Floor(su);
                h = floorY - 80f + (fl + Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1f, su - fl))) * 18f;
                return h;
            };
            var Gc = GcCarve(near, G, 45f); GcGround(set, "MarsGround", sand, c, 2800f, 220, Gc);
            var k = new AK();
            // the two kingdoms: walled cities on the canyon's two shelves (east and west of the ravine), towers with lamps
            foreach (var sg in new[] { -1f, 1f })
            {
                var ctr = c + (bridge.Count > 0 ? GcFlat(t.R[bridge[0]] * Vector3.forward) : Vector3.forward) * sg * 330f;
                for (int q = 0; q < 28; q++)
                {
                    var at = ctr + new Vector3((R() - 0.5f) * 280f, 0f, (R() - 0.5f) * 280f); if (!GcClear(t, at, 14f)) continue; at.y = Gc(at.x, at.z);
                    float w = 10f + R() * 16f, h = 10f + R() * 26f; var (v, tr) = k.Of(sg > 0 ? stone : stoneDark); MBox(v, tr, at + Vector3.up * h * 0.5f, new Vector3(w, h, w * (0.7f + R() * 0.6f)), R() * 90f, 0.06f);
                    if (R() < 0.5f) { var (v2, t2) = k.Of(sg > 0 ? stoneDark : stone); MBox(v2, t2, at + Vector3.up * (h + 4f), new Vector3(w * 0.5f, 8f, w * 0.5f), 0f, 0.04f); }
                    if (q % 3 == 0) { var (lv, lt) = k.Of(lamp); MBox(lv, lt, at + Vector3.up * (h + 0.4f), new Vector3(1.2f, 0.8f, 1.2f), 0f, 0f); }
                }
            }
            // the shared crossing: the bridge's piers into the ravine, a truss under the deck, pylons with lamps along its edges
            for (int q = 0; q < bridge.Count; q += 6)
            {
                int i = bridge[q]; var p = t.P[i]; var rt = GcFlat(t.R[i] * Vector3.right); float gy = Gc(p.x, p.z);
                var (mv, mt) = k.Of(metal); Sweep(mv, mt, new[] { p - rt * (t.HalfW[i] - 0.4f) + Vector3.down * 0.7f, p + rt * (t.HalfW[i] - 0.4f) + Vector3.down * 0.7f }, 0.35f, false, 6);
                if (q % 30 == 0) { var (sv, st) = k.Of(stoneDark); MBox(sv, st, new Vector3(p.x, (gy + p.y - 1f) * 0.5f, p.z), new Vector3(t.HalfW[i] * 1.4f, p.y - 1f - gy, 6f), Quaternion.LookRotation(rt).eulerAngles.y + 90f, 0.02f); }
                if (q % 12 == 0) foreach (var sg in new[] { -1f, 1f }) { var at = p + rt * sg * (t.HalfW[i] + 1.2f); Sweep(mv, mt, new[] { at + Vector3.down * 1f, at + Vector3.up * 8f }, 0.22f, false, 6); var (lv, lt) = k.Of(lamp); MBox(lv, lt, at + Vector3.up * 8.3f, new Vector3(0.8f, 0.5f, 0.8f), 0f, 0f); }
            }
            // the carved arch round the loop: a great stone ring standing round the loop's circle, carved with steps
            var loop = GcIdx(t, T.F.Loop);
            if (loop.Count > 0)
            {
                var lc = GcCentre(t, loop); var side = GcFlat(t.R[loop[0]] * Vector3.right); float rad = 24f + 14f;
                foreach (var sg in new[] { -1f, 1f })
                {
                    var face = lc + side * sg * (12f + 9f + 3f); var fwd = GcFlat(t.R[loop[0]] * Vector3.forward);
                    var pts = new List<Vector3>(); for (int s = 0; s <= 30; s++) { float a = s / 30f * Mathf.PI; pts.Add(face + fwd * Mathf.Cos(a) * rad + Vector3.up * Mathf.Sin(a) * rad + Vector3.down * 2f); }
                    var (v, tr) = k.Of(stone); Sweep(v, tr, pts, 4.5f, false, 8);
                    var (gv, gt) = k.Of(glowSoft); var inner = pts.Select(p => p + (lc - p).normalized * 4.8f).ToList(); Sweep(gv, gt, inner, 0.25f, false, 5);
                }
                var (dv, dt) = k.Of(stoneDark); MBox(dv, dt, new Vector3(lc.x, Gc(lc.x, lc.z) + 3f, lc.z), new Vector3(rad * 2.6f, 6f, 30f), Quaternion.LookRotation(GcFlat(t.R[loop[0]] * Vector3.forward)).eulerAngles.y, 0.05f);
            }
            // the maintenance route: the shortcut runs through a pipe-lined service cut (ribs, pipes, lamps)
            if (t.Branch != null)
            {
                var b = t.Branch;
                for (int i = 6; i < b.Count - 6; i += 6)
                {
                    var arch = GcRing(b, i, b.HalfW[i] + 1f, 0f, 10, 0f, 180f); var (v, tr) = k.Of(metal); Sweep(v, tr, arch, 0.3f, false, 6);
                    foreach (var sg in new[] { -1f, 1f }) { var (rv, rtt) = k.Of(rockDark); MBox(rv, rtt, b.P[i] + b.R[i] * Vector3.right * sg * (b.HalfW[i] + 2.6f) + Vector3.up * 3f, new Vector3(3f, 8f, 6.5f), b.R[i].eulerAngles.y, 0.1f); }
                    if (i % 18 == 0) { var (lv, lt) = k.Of(lamp); MBox(lv, lt, b.P[i] + b.R[i] * Vector3.up * (b.HalfW[i] + 0.7f), new Vector3(2.4f, 0.15f, 0.4f), b.R[i].eulerAngles.y, 0f); }
                }
            }
            // boulders and rock spires along the canyon floor
            foreach (var sg in new[] { -1f, 1f })
                foreach (var (i, at0, rt) in GcBeside(t, sg, 16f, 26))
                {
                    var at = at0 + rt * sg * R() * 40f; if (!GcClear(t, at, 8f)) continue; at.y = Gc(at.x, at.z);
                    var (v, tr) = k.Of(R() < 0.5f ? rock : rockDark);
                    if (R() < 0.6f) Ellipsoid(v, tr, at + Vector3.up * 2f, new Vector3(3f + R() * 5f, 2f + R() * 3f, 3f + R() * 5f), 4, 7);
                    else MBox(v, tr, at + Vector3.up * 9f, new Vector3(5f + R() * 4f, 18f + R() * 16f, 5f + R() * 4f), R() * 90f, 0.12f);
                }
            // rocks strewn over the canyon floor (a bare sand floor read as one flat tone from the air: the jump captures had 3 % edges)
            {
                var mn = new Vector3(t.P.Min(p => p.x), 0f, t.P.Min(p => p.z)) - Vector3.one * 160f; var mx = new Vector3(t.P.Max(p => p.x), 0f, t.P.Max(p => p.z)) + Vector3.one * 160f;
                for (int q = 0; q < 5000; q++)
                {
                    var at = new Vector3(Mathf.Lerp(mn.x, mx.x, R()), 0f, Mathf.Lerp(mn.z, mx.z, R())); if (!GcClear(t, at, 3f)) continue; at.y = Gc(at.x, at.z);
                    float sz = 0.6f + R() * R() * 4f; var (v, tr) = k.Of(R() < 0.55f ? rockDark : rock);
                    Ellipsoid(v, tr, at + Vector3.up * sz * 0.3f, new Vector3(sz * (0.8f + R() * 0.6f), sz * (0.4f + R() * 0.4f), sz * (0.8f + R() * 0.6f)), 3, 6);
                }
                // and a scree of rocks along the whole course, both sides, out to 70 m (the slopes beside the jumps read flat)
                for (int i = 0; i < t.Count; i += 3)
                    foreach (var sg in new[] { -1f, 1f })
                    {
                        var rt = GcFlat(t.R[i] * Vector3.right); var at = t.P[i] + rt * sg * (t.HalfW[i] + 5f + R() * 65f); if (!GcClear(t, at, 3f)) continue; at.y = Gc(at.x, at.z);
                        float sz = 0.5f + R() * R() * 3f; var (v, tr) = k.Of(R() < 0.6f ? rockDark : rock);
                        Ellipsoid(v, tr, at + Vector3.up * sz * 0.3f, new Vector3(sz * (0.8f + R() * 0.6f), sz * (0.4f + R() * 0.4f), sz * (0.8f + R() * 0.6f)), 3, 6);
                    }
                foreach (var gi in GcIdx(t, T.F.Gap).Where((x, j) => j % 6 == 0))
                {
                    var rt = GcFlat(t.R[gi] * Vector3.right);
                    for (int q = 0; q < 6; q++)
                    {
                        var at = t.P[gi] + rt * (R() - 0.5f) * 90f + GcFlat(t.R[gi] * Vector3.forward) * (R() - 0.5f) * 20f; at.y = Gc(at.x, at.z); var (v, tr) = k.Of(q % 2 == 0 ? rockDark : stoneDark);
                        float room = t.P[gi].y - 6f - at.y; if (room < 3f) continue;   // only down in the chasm, well under the jump's arc
                        if (q % 3 == 0) { float hh = Mathf.Min(16f + R() * 14f, room); MBox(v, tr, at + Vector3.up * hh * 0.5f, new Vector3(4f + R() * 4f, hh, 4f + R() * 4f), R() * 90f, 0.14f); }
                        else Ellipsoid(v, tr, at + Vector3.up * 2f, new Vector3(4f + R() * 5f, 2.5f + R() * 3f, 4f + R() * 5f), 4, 7);
                    }
                }
            }
            foreach (var gi in GcIdx(t, T.F.Gap).Where((x, j) => j % 4 == 0))
            {
                var rt = GcFlat(t.R[gi] * Vector3.right);
                foreach (var sg in new[] { -1f, 1f })
                    for (float d = t.HalfW[gi] + 6f; d < 46f; d += 7f)
                    {
                        var at = t.P[gi] + rt * sg * d; float gy = Gc(at.x, at.z); if (t.P[gi].y - gy < 8f) continue;
                        var (v, tr) = k.Of(d % 14f < 7f ? rockDark : rock); MBox(v, tr, new Vector3(at.x, gy + 1.2f, at.z), new Vector3(6f, 2.4f, 5f), Quaternion.LookRotation(rt).eulerAngles.y, 0.2f);
                    }
            }
            GcSupportsH(t, k, stoneDark, Gc, 9f, 0.9f); GcLoopFramesH(t, k, metal, Gc);
            k.Save("GcMars", set);
            Log($"GC MARS SET (R21): the canyon floor at {floorY:0} m, the bridge over {bridge.Count} m, the arch {(loop.Count > 0 ? "round the loop" : "-")}, the maintenance route {(t.Branch != null ? t.Branch.Length.ToString("0") + " m" : "-")}");
            return set;
        }

        // ------------------------------------------------------------------------------------------------ Europa Under-Ice
        static Transform BuildGcEuropaSet(T t)
        {
            var set = new GameObject("EuropaSet").transform;
            var ice = Dissolve("Gc_europa_ice", Gray(0.72f), 0.8f, false); var iceDark = Dissolve("Gc_europa_ice_dark", Gray(0.42f), 0.7f, false);
            var floor = Dissolve("Gc_europa_floor", Gray(0.26f), 0.2f, false); var metal = Dissolve("Gc_europa_metal", Gray(0.5f), 0.7f, false, 0.6f);
            var lightIce = GcEm("Gc_europa_light_ice", 0.95f, 1.1f, 0.8f); var ring = GcEm("Gc_europa_tube_ring", 1f, 1.5f); var glowSoft = GcEm("Gc_europa_glow_soft", 0.7f, 0.5f);
            var rnd = new System.Random(2103); float R() => (float)rnd.NextDouble();
            var near = new GcNearIndex(t);
            var c = (new Vector3(t.P.Min(p => p.x), 0f, t.P.Min(p => p.z)) + new Vector3(t.P.Max(p => p.x), 0f, t.P.Max(p => p.z))) * 0.5f;
            float lowY = t.P.Min(p => p.y), ceil = t.P.Max(p => p.y) + 46f;
            System.Func<float, float, float> H = (x, z) => lowY - 22f + GcPerlin(x, z, 0.008f, 10f) + GcPerlin(x, z, 0.03f, 2f);
            var Hc = GcCarve(near, H, 30f); GcGround(set, "EuropaFloor", floor, c, 2400f, 100, Hc);
            var k = new AK();
            // the ice ceiling far overhead (inside faces down): rough, with bright patches where the light comes through
            {
                var v = new List<Vector3>(); var tr = new List<int>(); int n = 70; float span = 2400f;
                for (int j = 0; j <= n; j++) for (int i = 0; i <= n; i++) { float x = c.x - span / 2 + span * i / n, z = c.z - span / 2 + span * j / n; v.Add(new Vector3(x, ceil + GcPerlin(x, z, 0.01f, 9f) + GcPerlin(x, z, 0.05f, 2f), z)); }
                for (int j = 0; j < n; j++) for (int i = 0; i < n; i++) { int a = j * (n + 1) + i, b = a + n + 1; tr.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b }); }
                MeshObject("EuropaIceCeiling", SaveMesh(SmoothFinish(v, tr), "Gc_EuropaIceCeiling"), iceDark, set, false);
                var (lv, lt) = k.Of(lightIce); for (int q = 0; q < 40; q++) { var at = c + new Vector3((R() - 0.5f) * 1600f, 0f, (R() - 0.5f) * 1600f); at.y = ceil - 2f; Ellipsoid(lv, lt, at, new Vector3(18f + R() * 30f, 1.5f, 14f + R() * 24f), 3, 10); }
                // ice columns hanging from the ceiling (stalactites) - fixed to it, never floating
                var (iv, it) = k.Of(ice); for (int q = 0; q < 50; q++) { var at = c + new Vector3((R() - 0.5f) * 1500f, 0f, (R() - 0.5f) * 1500f); if (!GcClear(t, at, 18f)) continue; float len = 12f + R() * 30f; Append(iv, it, Lathe(new[] { new Vector2(0f, -len), new Vector2(3f + R() * 3f, -2f), new Vector2(6f + R() * 4f, 0f), new Vector2(0f, 0f) }, 0f, 360f, 8, null, new Vector3(at.x, ceil, at.z))); }
            }
            // the glass tube: ribs round the ribbon every 6 m (a glowing ring every 18 m), six struts along it - the glass itself implied
            for (int i = 0; i < t.Count; i += 6)
            {
                if ((t.Flag[i] & T.F.Tunnel) == 0) continue;
                float r = (t.Flag[i] & T.F.Cork) != 0 ? T.CorkR + 2.5f : t.HalfW[i] + 2.2f; float h = (t.Flag[i] & T.F.Cork) != 0 ? 0f : 2.5f;
                if ((t.Flag[i] & T.F.Cork) != 0) { var cork = GcIdx(t, T.F.Cork); var up0 = t.R[cork[0]] * Vector3.up; var axis = t.P[i] + up0 * T.CorkR; var ringPts = new List<Vector3>(); var right0 = t.R[cork[0]] * Vector3.right; for (int s = 0; s <= 20; s++) { float a = s / 20f * Mathf.PI * 2f; ringPts.Add(axis + right0 * Mathf.Cos(a) * r + up0 * Mathf.Sin(a) * r); } var (cv, ct) = k.Of(i % 18 == 0 ? ring : metal); Sweep(cv, ct, ringPts, i % 18 == 0 ? 0.16f : 0.22f, false, 5); continue; }
                var pts = GcRing(t, i, r, h, 20); var (v, tr) = k.Of(i % 18 == 0 ? ring : metal); Sweep(v, tr, pts, i % 18 == 0 ? 0.16f : 0.22f, false, 5);
            }
            for (int s6 = 0; s6 < 6; s6++)
            {
                var path = new List<Vector3>(); float a = (s6 / 6f * 360f + 30f) * Mathf.Deg2Rad;
                for (int i = 0; i < t.Count; i += 3)
                {
                    if ((t.Flag[i] & T.F.Tunnel) == 0 || (t.Flag[i] & T.F.Cork) != 0) { if (path.Count > 1) { var (v, tr) = k.Of(metal); Sweep(v, tr, path, 0.1f, false, 4); } path = new List<Vector3>(); continue; }
                    float r = t.HalfW[i] + 2.2f; if (Mathf.Sin(a) < -0.2f) continue; path.Add(t.P[i] + t.R[i] * Vector3.up * 2.5f + t.R[i] * Vector3.right * Mathf.Cos(a) * r + t.R[i] * Vector3.up * Mathf.Sin(a) * r);
                }
                if (path.Count > 1) { var (v, tr) = k.Of(metal); Sweep(v, tr, path, 0.1f, false, 4); }
            }
            // the great chamber (the drop and the jump): an ice dome rising round that part of the course, glowing fissures in its walls
            var gap = GcIdx(t, T.F.Gap);
            if (gap.Count > 0)
            {
                var gc = GcCentre(t, gap); var (v, tr) = k.Of(ice);
                for (int q = 0; q < 24; q++)
                {
                    float a = q / 24f * Mathf.PI * 2f; var at = gc + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (150f + R() * 30f); if (!GcClear(t, at, 20f)) continue; float y0 = Hc(at.x, at.z);
                    Append(v, tr, Lathe(new[] { new Vector2(0f, 0f), new Vector2(14f + R() * 10f, 0f), new Vector2(8f, (ceil - y0) * 0.6f), new Vector2(12f, ceil - y0), new Vector2(0f, ceil - y0) }, 0f, 360f, 9, null, new Vector3(at.x, y0, at.z)));
                    var (gv, gt) = k.Of(glowSoft); Sweep(gv, gt, new[] { new Vector3(at.x, y0 + 6f, at.z) + (gc - at).normalized * 9f, new Vector3(at.x, ceil - 20f, at.z) + (gc - at).normalized * 9f }, 0.35f, false, 5);
                }
                var (lv, lt) = k.Of(lightIce); var pit = gc; pit.y = Hc(pit.x, pit.z) - 30f; Ellipsoid(lv, lt, pit + Vector3.up * 4f, new Vector3(22f, 3f, 14f), 4, 10);
            }
            // the settlement round the shortcut: domed houses and a lit tower on the sea floor, its street under arches
            if (t.Branch != null)
            {
                var b = t.Branch;
                for (int i = 8; i < b.Count - 8; i += 7)
                {
                    var arch = GcRing(b, i, b.HalfW[i] + 1.4f, 0f, 12, 0f, 180f); var (v, tr) = k.Of(i % 14 == 1 ? ring : metal); Sweep(v, tr, arch, 0.2f, false, 5);
                    foreach (var sg in new[] { -1f, 1f })
                    {
                        if (i % 14 != 1) continue; var at = b.P[i] + b.R[i] * Vector3.right * sg * (b.HalfW[i] + 9f + R() * 8f); at.y = Hc(at.x, at.z); if (!GcClear(t, at, 6f)) continue;
                        var (dv, dt) = k.Of(iceDark); Ellipsoid(dv, dt, at, new Vector3(5f + R() * 3f, 4f + R() * 2f, 5f + R() * 3f), 5, 10, p => p.y < 0f);
                        var (wv, wt) = k.Of(glowSoft); MBox(wv, wt, at + Vector3.up * 1.6f + (b.P[i] - at).normalized * 5.2f, new Vector3(1.4f, 1.2f, 0.3f), Quaternion.LookRotation(GcFlat(b.P[i] - at)).eulerAngles.y, 0f);
                    }
                }
                var mid = b.P[b.Count / 2] + b.R[b.Count / 2] * Vector3.right * 30f; mid.y = Hc(mid.x, mid.z); var (mv, mt) = k.Of(metal); Append(mv, mt, Lathe(new[] { new Vector2(0f, 0f), new Vector2(5f, 0f), new Vector2(3f, 30f), new Vector2(5f, 34f), new Vector2(0f, 36f) }, 0f, 360f, 10, null, mid)); var (rv, rt2) = k.Of(ring); Ellipsoid(rv, rt2, mid + Vector3.up * 37f, Vector3.one * 2f, 5, 8);
            }
            // seaweed / ice spires on the floor along the course
            foreach (var sg in new[] { -1f, 1f })
                foreach (var (i, at0, rt) in GcBeside(t, sg, 14f, 24))
                {
                    var at = at0 + rt * sg * R() * 40f; if (!GcClear(t, at, 8f)) continue; at.y = Hc(at.x, at.z);
                    var (v, tr) = k.Of(R() < 0.6f ? ice : iceDark); float hh = 8f + R() * 30f; Append(v, tr, Lathe(new[] { new Vector2(0f, 0f), new Vector2(2f + R() * 3f, 0f), new Vector2(0.4f, hh), new Vector2(0f, hh) }, 0f, 360f, 6, null, at));
                }
            GcSupportsH(t, k, metal, Hc, 12f, 0.6f); GcLoopFramesH(t, k, metal, Hc);
            k.Save("GcEuropa", set);
            // the Cetaceans: a pod swimming alongside the tube, outside it (the whale + dolphins of the story), on loops beside the course
            var whale = SetupCetacean("whale"); var dolphin = SetupCetacean("dolphin"); int made = 0;
            var tube = GcIdx(t, T.F.Tunnel);
            for (int q = 0; q < 5 && tube.Count > 0; q++)
            {
                bool big = q == 0; var model = big ? whale : dolphin; var matName = big ? "Europa_whale" : "Europa_dolphin";
                float side = q % 2 == 0 ? 1f : -1f, off = big ? 34f : 18f + q * 3f, hh = big ? 10f : 4f + q * 2f;
                var pts = new List<Vector3>(); int start = (q * 97) % tube.Count;
                for (int s = 0; s < 22; s++) { int i = tube[(start + s * 9) % tube.Count]; pts.Add(t.P[i] + GcFlat(t.R[i] * Vector3.right) * side * off + Vector3.up * hh); }
                for (int s = 21; s >= 0; s--) { int i = tube[(start + s * 9) % tube.Count]; pts.Add(t.P[i] + GcFlat(t.R[i] * Vector3.right) * side * (off + 14f) + Vector3.up * (hh + 6f)); }
                var go = new GameObject((big ? "Whale" : "Dolphin") + q); go.transform.SetParent(set, false); go.transform.position = pts[0];
                if (model != null) { var inst = (GameObject)PrefabUtility.InstantiatePrefab(model); inst.name = go.name + "Model"; inst.transform.SetParent(go.transform, false); inst.transform.localScale = Vector3.one * (big ? 0.9f : 1.1f); var mat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/" + matName + ".mat"); foreach (var r in inst.GetComponentsInChildren<SkinnedMeshRenderer>()) { if (mat != null) r.sharedMaterial = mat; r.updateWhenOffscreen = true; } }
                var ce = go.AddComponent<Seven.Cetacean>(); ce.Points = pts.ToArray(); ce.Speed = big ? 9f : 14f; ce.Current = Seven.Cetacean.Mode.Loop; ce.TurnRate = big ? 25f : 50f; made++;
            }
            Log($"GC EUROPA SET (R21): the ice ceiling at {ceil:0} m with the light through it, the sea floor, the glass tube over {tube.Count} m, the great chamber, the settlement, {made} Cetaceans swimming alongside");
            return set;
        }

        // ------------------------------------------------------------------------------------------------ Asteroid Foundry
        static Transform BuildGcAsteroidSet(T t)
        {
            var set = new GameObject("AsteroidSet").transform;
            var rock = Dissolve("Gc_ast_rock", Gray(0.24f), 0.15f, false); var plate = Dissolve("Gc_ast_plate", Gray(0.44f), 0.55f, false, 0.6f); var plateDark = Dissolve("Gc_ast_plate_dark", Gray(0.22f), 0.5f, false, 0.5f);
            var brass = Dissolve("Gc_ast_brass", Gray(0.66f), 0.75f, false, 0.8f); var belt = Dissolve("Gc_ast_belt", Gray(0.14f), 0.2f, false);
            var lamp = GcEm("Gc_ast_lamp", 1f, 1.6f); var furnace = GcEm("Gc_ast_furnace", 1f, 2.2f); var glowSoft = GcEm("Gc_ast_glow_soft", 0.7f, 0.5f);
            var rnd = new System.Random(2104); float R() => (float)rnd.NextDouble();
            var near = new GcNearIndex(t);
            var c = (new Vector3(t.P.Min(p => p.x), 0f, t.P.Min(p => p.z)) + new Vector3(t.P.Max(p => p.x), 0f, t.P.Max(p => p.z))) * 0.5f;
            float gy0 = t.P.Min(p => p.y) - 6f;
            System.Func<float, float, float> H = (x, z) => { float d = new Vector2(x - c.x, z - c.z).magnitude; return gy0 + GcPerlin(x, z, 0.02f, 1.5f) + Mathf.Clamp01((d - 430f) / 300f) * 220f + GcPerlin(x, z, 0.006f, 25f) * Mathf.Clamp01((d - 430f) / 300f); };
            var Hc = GcCarve(near, H, 40f); GcGround(set, "AsteroidFloor", rock, c, 2400f, 100, Hc);
            var k = new AK();
            // the cavern's roof: the asteroid's rock far overhead (inside faces), a few work lights on it
            { var (v, tr) = k.Of(rock); Ellipsoid(v, tr, new Vector3(c.x, gy0 - 40f, c.z), new Vector3(1150f, 420f, 1150f), 12, 24); for (int i = 0; i < tr.Count; i += 3) { int a = tr[i + 1]; tr[i + 1] = tr[i + 2]; tr[i + 2] = a; } }
            // the conveyor run: belts either side of the road (the boost strip is the belt the racers ride), rollers, crates riding them
            for (int i = 0; i < t.Count; i += 4)
            {
                if ((t.Flag[i] & T.F.Boost) == 0 || (t.Flag[i] & T.F.Pickup) == 0) continue;
                var rt = t.R[i] * Vector3.right; foreach (var sg in new[] { -1f, 1f })
                {
                    var p = t.P[i] + rt * sg * (t.HalfW[i] + 3.5f) + Vector3.down * 0.4f; var (bv, bt) = k.Of(belt); MBox(bv, bt, p, new Vector3(4f, 0.5f, 4.1f), t.R[i].eulerAngles.y, 0f);
                    var (rv, rtt) = k.Of(brass); Sweep(rv, rtt, new[] { p - rt * 2.2f + Vector3.down * 0.3f, p + rt * 2.2f + Vector3.down * 0.3f }, 0.3f, false, 6);
                    if (i % 24 == 0) { var crate = new GameObject("BeltCrate"); crate.transform.SetParent(set, false); var cr = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(cr.GetComponent<Collider>()); cr.transform.SetParent(crate.transform, false); cr.transform.localScale = new Vector3(2.4f, 2f, 2.4f); cr.GetComponent<Renderer>().sharedMaterial = plateDark; var mv = crate.AddComponent<Seven.GcMover>(); mv.Mode = Seven.GcMover.Kind.Shuttle; mv.From = p + Vector3.up * 1.3f; mv.To = p + Vector3.up * 1.3f + t.R[i] * Vector3.forward * 40f; mv.Period = 9f; mv.Phase = R() * 9f; crate.transform.localPosition = mv.From; }
                }
            }
            // the great flywheel the loop goes round: a huge spoked wheel standing in the loop's circle, turning
            var loop = GcIdx(t, T.F.Loop);
            if (loop.Count > 0)
            {
                var lc = GcCentre(t, loop); var side = GcFlat(t.R[loop[0]] * Vector3.right); var fwd = GcFlat(t.R[loop[0]] * Vector3.forward);
                float rad = 170f / (2f * Mathf.PI) - 4f;
                foreach (var sg in new[] { -1f, 1f })
                {
                    var wheel = new GameObject("Flywheel" + (sg > 0 ? "R" : "L")).transform; wheel.SetParent(set, false); wheel.position = lc + side * sg * (12f + 8f + 4f); wheel.rotation = Quaternion.LookRotation(side, Vector3.up);
                    var wk = new AK(); var (v, tr) = wk.Of(plate); var rim = new List<Vector3>(); for (int s = 0; s <= 40; s++) { float a = s / 40f * Mathf.PI * 2f; rim.Add(new Vector3(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad, 0f)); } Sweep(v, tr, rim, 2.2f, false, 8);
                    for (int s = 0; s < 8; s++) { float a = s / 8f * Mathf.PI * 2f; var (sv, st) = wk.Of(plateDark); Sweep(sv, st, new[] { Vector3.zero, new Vector3(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad, 0f) }, 0.9f, false, 6); }
                    var (hv, ht) = wk.Of(brass); Append(hv, ht, Lathe(new[] { new Vector2(0f, -2f), new Vector2(4f, -2f), new Vector2(4f, 2f), new Vector2(0f, 2f) }, 0f, 360f, 14, null, Vector3.zero));
                    var (gv, gt) = wk.Of(lamp); for (int s = 0; s < 16; s++) { float a = s / 16f * Mathf.PI * 2f; MBox(gv, gt, new Vector3(Mathf.Cos(a) * (rad - 2.6f), Mathf.Sin(a) * (rad - 2.6f), -1.2f), new Vector3(0.8f, 0.8f, 0.3f), 0f, 0f); }
                    wk.Save("GcFlywheel" + (sg > 0 ? "R" : "L"), wheel);
                    var mv = wheel.gameObject.AddComponent<Seven.GcMover>(); mv.Mode = Seven.GcMover.Kind.Spin; mv.Axis = Vector3.forward; mv.DegPerSec = 14f * sg;
                    var (pv, pt) = k.Of(plateDark); var foot = wheel.position; foot.y = Hc(foot.x, foot.z); MBox(pv, pt, (foot + wheel.position) * 0.5f, new Vector3(5f, wheel.position.y - foot.y, 9f), Quaternion.LookRotation(fwd).eulerAngles.y, 0.04f);
                }
            }
            // the furnace hall under the jump: a deep pit, furnace mouths glowing in its walls, chimneys up into the dark
            var gap = GcIdx(t, T.F.Gap);
            if (gap.Count > 0)
            {
                var gc = GcCentre(t, gap); float pitY = Hc(gc.x, gc.z);
                var (fv, ft) = k.Of(furnace); var (pv, pt) = k.Of(plateDark);
                for (int q = 0; q < 10; q++)
                {
                    float a = q / 10f * Mathf.PI * 2f; var at = gc + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 36f; at.y = pitY + 4f;
                    MBox(pv, pt, at + Vector3.up * 5f, new Vector3(10f, 14f, 8f), Mathf.Atan2(Mathf.Cos(a), Mathf.Sin(a)) * Mathf.Rad2Deg, 0.03f);
                    MBox(fv, ft, at + (gc - at).normalized * 4.1f + Vector3.up * 2.5f, new Vector3(4f, 3f, 0.4f), Mathf.Atan2(Mathf.Cos(a), Mathf.Sin(a)) * Mathf.Rad2Deg, 0f);
                    if (q % 2 == 0) { var top = at + Vector3.up * 90f; if (GcClear(t, at, 4f)) Append(pv, pt, Lathe(new[] { new Vector2(0f, 0f), new Vector2(4f, 0f), new Vector2(3f, 90f), new Vector2(0f, 90f) }, 0f, 360f, 10, null, at + Vector3.up * 12f)); }
                }
            }
            // the rail carrier on the shortcut: an overhead rail along it, the carrier car shuttling along the rail
            if (t.Branch != null)
            {
                var b = t.Branch; var rail = new List<Vector3>();
                for (int i = 0; i < b.Count; i += 4) rail.Add(b.P[i] + b.R[i] * Vector3.up * 7.5f);
                var (rv, rtt) = k.Of(plateDark); Sweep(rv, rtt, rail, 0.5f, false, 6);
                for (int i = 6; i < b.Count - 6; i += 12) { var (v, tr) = k.Of(plate); var arch = GcRing(b, i, b.HalfW[i] + 0.8f, 0f, 10, 0f, 180f); Sweep(v, tr, arch, 0.35f, false, 6); var (lv, lt) = k.Of(lamp); MBox(lv, lt, b.P[i] + b.R[i] * Vector3.up * 6.6f, new Vector3(2f, 0.15f, 0.5f), b.R[i].eulerAngles.y, 0f); }
                var car = new GameObject("RailCarrier").transform; car.SetParent(set, false);
                var ck = new AK(); var (cv, ct) = ck.Of(brass); MBox(cv, ct, new Vector3(0f, -1.8f, 0f), new Vector3(3f, 2.4f, 6f), 0f, 0.02f); var (gv, gt) = ck.Of(lamp); MBox(gv, gt, new Vector3(0f, -3.05f, 2.4f), new Vector3(2f, 0.15f, 0.6f), 0f, 0f); ck.Save("GcRailCarrier", car);
                var mv = car.gameObject.AddComponent<Seven.GcMover>(); mv.Mode = Seven.GcMover.Kind.Shuttle; mv.From = rail[2]; mv.To = rail[rail.Count / 2]; mv.Period = 14f; car.position = mv.From;
            }
            // the machine city along the run: gear towers, pipe runs, chimneys, workshops with lit windows
            foreach (var sg in new[] { -1f, 1f })
                foreach (var (i, at0, rt) in GcBeside(t, sg, 12f, 20))
                {
                    var at = at0 + rt * sg * R() * 36f; if (!GcClear(t, at, 8f)) continue; at.y = Hc(at.x, at.z); float u = R();
                    if (u < 0.35f) { var (v, tr) = k.Of(plateDark); float h = 12f + R() * 30f; MBox(v, tr, at + Vector3.up * h * 0.5f, new Vector3(9f + R() * 8f, h, 9f + R() * 8f), R() * 90f, 0.02f); var (wv, wt) = k.Of(glowSoft); MBox(wv, wt, at + Vector3.up * h * 0.6f + (t.P[i] - at).normalized * 5.5f, new Vector3(4f, 1.2f, 0.3f), Quaternion.LookRotation(GcFlat(t.P[i] - at)).eulerAngles.y, 0f); }
                    else if (u < 0.6f) { var g = new GameObject("Gear").transform; g.SetParent(set, false); g.position = at + Vector3.up * (8f + R() * 6f); g.rotation = Quaternion.LookRotation(rt, Vector3.up); var gk = new AK(); var (v, tr) = gk.Of(brass); float rr = 4f + R() * 4f; var prof = new List<Vector2>(); Append(v, tr, Lathe(new[] { new Vector2(0f, -0.8f), new Vector2(rr, -0.8f), new Vector2(rr, 0.8f), new Vector2(0f, 0.8f) }, 0f, 360f, 16, null, Vector3.zero)); for (int s = 0; s < 12; s++) { float a = s / 12f * Mathf.PI * 2f; MBox(v, tr, new Vector3(Mathf.Cos(a) * (rr + 0.6f), 0f, Mathf.Sin(a) * (rr + 0.6f)), new Vector3(1.2f, 1.4f, 1.2f), -a * Mathf.Rad2Deg, 0f); } gk.Save("GcGear" + i + (sg > 0 ? "R" : "L"), g); g.localRotation *= Quaternion.Euler(90f, 0f, 0f); var mv = g.gameObject.AddComponent<Seven.GcMover>(); mv.Axis = Vector3.up; mv.DegPerSec = (R() < 0.5f ? -1f : 1f) * (10f + R() * 20f); var (pv, pt) = k.Of(plateDark); Sweep(pv, pt, new[] { at, g.position }, 0.8f, false, 6); }
                    else if (u < 0.8f) { var (v, tr) = k.Of(plate); Append(v, tr, Lathe(new[] { new Vector2(0f, 0f), new Vector2(3f, 0f), new Vector2(2.2f, 40f + R() * 30f), new Vector2(0f, 40f) }, 0f, 360f, 8, null, at)); }
                    else { var (v, tr) = k.Of(brass); var a = at + Vector3.up * (3f + R() * 4f); Sweep(v, tr, new[] { a, a + t.R[i] * Vector3.forward * 20f }, 0.7f, false, 6); }
                }
            { var (lv, lt) = k.Of(lamp); foreach (var (i, at, rt) in GcBeside(t, 1f, 2.5f, 36, T.F.Loop | T.F.Cork | T.F.Gap | T.F.Press)) { var (mv, mt) = k.Of(plateDark); Sweep(mv, mt, new[] { at + Vector3.down * 1f, at + Vector3.up * 7f }, 0.18f, false, 6); MBox(lv, lt, at + Vector3.up * 7.2f, new Vector3(0.7f, 0.25f, 0.7f), 0f, 0f); } }
            GcSeams(k, plate, glowSoft, c, Hc, 9f, 520f, 0.2f);   // the foundry floor laid in iron plates (a bare dark floor gave the jump capture 5 % edges)
            GcSupportsH(t, k, plateDark, Hc, 9f, 0.8f); GcLoopFramesH(t, k, plate, Hc);
            k.Save("GcAsteroid", set);
            Log($"GC ASTEROID SET (R21): the foundry floor at {gy0:0} m, the flywheel {(loop.Count > 0 ? "in the loop" : "-")}, the furnace hall under the jump, the rail carrier {(t.Branch != null ? "over the shortcut" : "-")}");
            return set;
        }

        // ------------------------------------------------------------------------------------------------ the Mothership
        static Transform BuildGcMothershipSet(T t)
        {
            var set = new GameObject("MothershipSet").transform;
            var shell = Dissolve("Gc_bia_shell", Gray(0.78f), 0.55f, false); var shellDark = Dissolve("Gc_bia_shell_dark", Gray(0.5f), 0.5f, false);
            var white = Dissolve("Gc_bia_white", Gray(0.86f), 0.6f, false); var pale = Dissolve("Gc_bia_pale", Gray(0.64f), 0.5f, false); var dark = Dissolve("Gc_bia_dark", Gray(0.3f), 0.4f, false);
            var globe = GcEm("Gc_bia_globe", 1f, 1.4f, 0.8f); var memory = GcEm("Gc_bia_memory", 0.85f, 0.7f, 0.8f); var glowSoft = GcEm("Gc_bia_glow_soft", 0.7f, 0.45f);
            var rnd = new System.Random(2105); float R() => (float)rnd.NextDouble();
            var near = new GcNearIndex(t);
            var c = (new Vector3(t.P.Min(p => p.x), 0f, t.P.Min(p => p.z)) + new Vector3(t.P.Max(p => p.x), 0f, t.P.Max(p => p.z))) * 0.5f;
            float gy0 = t.P.Min(p => p.y) - 8f;
            // the shell curves up all round (the interior city rises with it): the floor's height climbs with distance from the centre
            System.Func<float, float, float> H = (x, z) => { float d = new Vector2(x - c.x, z - c.z).magnitude; float rise = Mathf.Max(0f, d - 300f); return gy0 + rise * rise / 900f + GcPerlin(x, z, 0.02f, 0.8f); };
            var Hc = GcCarve(near, H, 34f); GcGround(set, "MothershipFloor", shell, c, 2600f, 110, Hc);
            var k = new AK();
            // the egg's shell closing overhead (inside faces): the far ends lost in the haze
            { var (v, tr) = k.Of(shellDark); Ellipsoid(v, tr, new Vector3(c.x, gy0 + 120f, c.z), new Vector3(1250f, 700f, 1500f), 14, 28, p => p.y < -0.2f); for (int i = 0; i < tr.Count; i += 3) { int a = tr[i + 1]; tr[i + 1] = tr[i + 2]; tr[i + 2] = a; } }
            // the interior city on the rising shell: smooth white towers and terraces, light globes on pillars (never hanging)
            for (int q = 0; q < 140; q++)
            {
                float a = R() * Mathf.PI * 2f, d = 60f + R() * 700f; var at = c + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d * 1.15f); if (!GcClear(t, at, 12f)) continue; at.y = Hc(at.x, at.z);
                float h = 10f + R() * 40f, w = 7f + R() * 12f; var (v, tr) = k.Of(q % 3 == 0 ? pale : white);
                Append(v, tr, Lathe(new[] { new Vector2(0f, 0f), new Vector2(w, 0f), new Vector2(w * 0.8f, h * 0.7f), new Vector2(w * 0.45f, h), new Vector2(0f, h + w * 0.3f) }, 0f, 360f, 12, null, at));
                if (q % 4 == 0) { var (gv, gt) = k.Of(globe); var (pv, pt) = k.Of(dark); var post = at + new Vector3(w + 3f, 0f, 0f); post.y = Hc(post.x, post.z); Sweep(pv, pt, new[] { post, post + Vector3.up * 9f }, 0.25f, false, 6); Ellipsoid(gv, gt, post + Vector3.up * 10.2f, Vector3.one * 1.3f, 6, 10); }
                if (q % 5 == 0) { var (wv, wt) = k.Of(glowSoft); var ring = new List<Vector3>(); for (int s = 0; s <= 16; s++) { float b = s / 16f * Mathf.PI * 2f; ring.Add(at + new Vector3(Mathf.Cos(b) * w * 0.82f, h * 0.55f, Mathf.Sin(b) * w * 0.82f)); } Sweep(wv, wt, ring, 0.18f, false, 4); }
            }
            // the corkscrew round the shell's curve: guide rings round its axis, held from the floor by curved white ribs
            var cork = GcIdx(t, T.F.Cork);
            if (cork.Count > 0)
            {
                var up0 = t.R[cork[0]] * Vector3.up; var right0 = t.R[cork[0]] * Vector3.right;
                for (int q = 0; q < cork.Count; q += 10)
                {
                    int i = cork[q]; var axis = t.P[cork[0]] + up0 * T.CorkR + (t.P[i] - t.P[cork[0]]) - Vector3.Project(t.P[i] - t.P[cork[0]], up0) - Vector3.Project(t.P[i] - t.P[cork[0]], right0);
                    var ring = new List<Vector3>(); for (int s = 0; s <= 24; s++) { float a = s / 24f * Mathf.PI * 2f; ring.Add(axis + right0 * Mathf.Cos(a) * (T.CorkR + 3f) + up0 * Mathf.Sin(a) * (T.CorkR + 3f)); }
                    var (v, tr) = k.Of(q % 20 == 0 ? globe : white); Sweep(v, tr, ring, q % 20 == 0 ? 0.18f : 0.32f, false, 5);
                    var foot = axis + right0 * (T.CorkR + 3f); var fy = Hc(foot.x, foot.z); var (pv, pt) = k.Of(pale); Sweep(pv, pt, new[] { foot, new Vector3(foot.x, fy, foot.z) }, 0.4f, false, 6);
                }
            }
            // the suspended bridge: tall pylons either side, cables swooping down to the deck's edges, the chasm far below
            var bridge = GcIdx(t, T.F.SetPiece);
            if (bridge.Count > 0)
            {
                int i0 = bridge[0], i1 = bridge[bridge.Count - 1]; var (pv, pt) = k.Of(white); var (cv, ct) = k.Of(pale);
                foreach (var end in new[] { i0 + 20, i1 - 20 })
                    foreach (var sg in new[] { -1f, 1f })
                    {
                        var at = t.P[end] + GcFlat(t.R[end] * Vector3.right) * sg * (t.HalfW[end] + 2f); float fy = Hc(at.x, at.z);
                        Append(pv, pt, Lathe(new[] { new Vector2(0f, 0f), new Vector2(2.4f, 0f), new Vector2(1.4f, at.y - fy + 32f), new Vector2(0f, at.y - fy + 34f) }, 0f, 360f, 10, null, new Vector3(at.x, fy, at.z)));
                        var (gv, gt) = k.Of(globe); Ellipsoid(gv, gt, at + Vector3.up * 35f, Vector3.one * 1.6f, 6, 10);
                    }
                foreach (var sg in new[] { -1f, 1f })
                {
                    var a = t.P[i0 + 20] + GcFlat(t.R[i0 + 20] * Vector3.right) * sg * (t.HalfW[i0] + 2f) + Vector3.up * 32f; var b = t.P[i1 - 20] + GcFlat(t.R[i1 - 20] * Vector3.right) * sg * (t.HalfW[i1] + 2f) + Vector3.up * 32f;
                    var cable = new List<Vector3>(); for (int s = 0; s <= 24; s++) { float u = s / 24f; int i = Mathf.RoundToInt(Mathf.Lerp(i0 + 20, i1 - 20, u)); var deck = t.P[i] + GcFlat(t.R[i] * Vector3.right) * sg * (t.HalfW[i] + 1.2f) + Vector3.up * 1.2f; var line = Vector3.Lerp(a, b, u); cable.Add(Vector3.Lerp(line, deck, Mathf.Sin(u * Mathf.PI) * 0.92f)); }
                    Sweep(cv, ct, cable, 0.3f, false, 6);
                    for (int s = 1; s < 24; s += 2) { float u = s / 24f; int i = Mathf.RoundToInt(Mathf.Lerp(i0 + 20, i1 - 20, u)); var deck = t.P[i] + GcFlat(t.R[i] * Vector3.right) * sg * (t.HalfW[i] + 1.2f); Sweep(cv, ct, new[] { deck, cable[s] }, 0.08f, false, 4); }
                }
            }
            // the memory structures before the jump: tall translucent-white columns with slow glowing rings
            var gap = GcIdx(t, T.F.Gap);
            if (gap.Count > 0)
            {
                foreach (var sg in new[] { -1f, 1f })
                    for (int q = 0; q < 6; q++)
                    {
                        int i = Mathf.Max(0, gap[0] - 110 + q * 18); var at = t.P[i] + GcFlat(t.R[i] * Vector3.right) * sg * (t.HalfW[i] + 10f + q % 2 * 6f); at.y = Hc(at.x, at.z); if (!GcClear(t, at, 4f)) continue;
                        var (mv, mt) = k.Of(memory); float h = 24f + R() * 20f; Append(mv, mt, Lathe(new[] { new Vector2(0f, 0f), new Vector2(2.6f, 0f), new Vector2(1.8f, h), new Vector2(0f, h + 3f) }, 0f, 360f, 6, null, at));
                        var (gv, gt) = k.Of(glowSoft); for (int s = 1; s < 4; s++) { var ring = new List<Vector3>(); for (int u = 0; u <= 12; u++) { float a = u / 12f * Mathf.PI * 2f; ring.Add(at + new Vector3(Mathf.Cos(a) * 3.4f, h * s / 4f, Mathf.Sin(a) * 3.4f)); } Sweep(gv, gt, ring, 0.15f, false, 4); }
                    }
                // the transfer chamber under the jump: a round well with platforms round its rim
                var gc = GcCentre(t, gap); float wy = Hc(gc.x, gc.z); var (dv, dt) = k.Of(pale);
                for (int q = 0; q < 8; q++) { float a = q / 8f * Mathf.PI * 2f; var at = gc + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 34f; at.y = wy + 3f; MBox(dv, dt, at, new Vector3(9f, 1.2f, 6f), -a * Mathf.Rad2Deg, 0f); var (gv, gt) = k.Of(globe); Ellipsoid(gv, gt, at + Vector3.up * 1.5f, Vector3.one * 0.8f, 5, 8); }
            }
            // the quiet passage (the shortcut): a smooth white tube of ribs, soft light at its crown
            if (t.Branch != null)
            {
                var b = t.Branch;
                for (int i = 6; i < b.Count - 6; i += 5) { var arch = GcRing(b, i, b.HalfW[i] + 1.2f, 0f, 14, 0f, 180f); var (v, tr) = k.Of(i % 15 == 1 ? glowSoft : white); Sweep(v, tr, arch, 0.4f, false, 6); }
            }
            // the shell's floor in panels: dark seams on a 10 m grid following its curve, every third one a soft light line (the smooth
            // floor read as one tone from the air: the jump captures had 4-6 % edges)
            {
                var (sv, st) = k.Of(dark); var (lv, lt) = k.Of(glowSoft);
                for (int axis = 0; axis < 2; axis++)
                    for (int li = -64; li <= 64; li++)
                    {
                        bool glow = li % 3 == 0; var (v, tr) = glow ? (lv, lt) : (sv, st); float w = glow ? 0.3f : 0.22f;
                        for (int seg = -128; seg < 128; seg++)
                        {
                            float a0 = seg * 5f, a1 = (seg + 1) * 5f, o = li * 10f;
                            Vector3 P(float a) { float x = c.x + (axis == 0 ? a : o), z = c.z + (axis == 0 ? o : a); return new Vector3(x, Hc(x, z) + 0.05f, z); }
                            var p0 = P(a0); var p1 = P(a1); if (new Vector2(p0.x - c.x, p0.z - c.z).magnitude > 640f) continue;
                            var side = (axis == 0 ? Vector3.forward : Vector3.right) * w; int b0 = v.Count;
                            v.Add(p0 - side); v.Add(p0 + side); v.Add(p1 + side); v.Add(p1 - side); tr.AddRange(new[] { b0, b0 + 1, b0 + 2, b0, b0 + 2, b0 + 3 });
                        }
                    }
            }
            GcSupportsH(t, k, pale, Hc, 10f, 0.8f); GcLoopFramesH(t, k, white, Hc);
            k.Save("GcMothership", set);
            Log($"GC MOTHERSHIP SET (R21): the shell's floor from {gy0:0} m rising round the edge, the city on it, the corkscrew's rings, the suspended bridge over {bridge.Count} m, the memory structures, the quiet passage");
            return set;
        }
    }
}
