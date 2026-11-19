using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF14 (3) - the bike bay as a real modelled room (Dan's reference Art/Reference/bike_bay_ref.png), the fourth try.
    /// Why the earlier holds failed: the "bay" was the hull sector itself (floor / ceiling lathes 3 cm inside the hull skins
    /// from r 1.6 to the rim). The hull is only 0.26 m thick at the rim, so that bay was a thin wedge - flat gray from
    /// outside, and the 0.95 m bike poked through its ceiling as it glided out (Dan's "gray slab with the bike's shapes
    /// poking through it"). Now: a room sized to the bike (3.6 m long, 2.1 m wide, 1.26 m high) sits inside the thick
    /// middle of the hull, its rounded chrome frame on a bulkhead at r 3.45; the hatch panel covers only the hull in front of
    /// the frame (r 3.45 to the rim), which is a full-height slot once the panel has slid away, so the bike glides straight
    /// out through the frame and the slot without touching anything. Polished chrome / brushed steel / a dark polished
    /// floor, three round ceiling lights (real lights), curved walls with sweeping chrome ribs, panel seams, drawers, a
    /// control panel, a raised track from the back stop to the opening and two chrome cradles under the bike. Its own
    /// box-projected reflection probe (sees only the saucer layer) gives the chrome something to reflect.
    /// Local frame of the "Bay" object: +z = the hatch direction (out), x across, y = saucer-local height.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        public const float BayFloorUp = 0.13f;                 // floor above the belly line b0
        public const float BayFront = 3.06f, BayBack = -0.55f, BayFrameFront = 3.30f, BayNotch = 3.45f;
        public const float BayOpenHW = 0.82f, BayOpenBot = 0.16f, BayOpenTop = 1.25f, BayFrameBar = 0.12f;

        // right half of the room's cross-section (x, height above the floor), floor centre -> ceiling centre
        static readonly Vector2[] BayProfile =
        {
            new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0.98f, 0f),                                           // floor 0..2
            new Vector2(1.02f, 0.01f), new Vector2(1.045f, 0.04f), new Vector2(1.05f, 0.08f), new Vector2(1.05f, 0.27f), new Vector2(1.05f, 0.46f),   // cove + drawer wall 3..7
            new Vector2(1.06f, 0.56f), new Vector2(1.05f, 0.70f), new Vector2(1.0f, 0.84f), new Vector2(0.92f, 0.96f),
            new Vector2(0.81f, 1.06f), new Vector2(0.69f, 1.13f), new Vector2(0.6f, 1.17f),                               // curved wall 8..14
            new Vector2(0.56f, 1.18f), new Vector2(0.545f, 1.22f), new Vector2(0.53f, 1.25f), new Vector2(0.5f, 1.26f), new Vector2(0.25f, 1.26f), new Vector2(0f, 1.26f), // ceiling 15..20
        };
        const int BayFloorEnd = 2, BayWallEnd = 14;

        static Vector2 BayNormalIn(int i)
        {
            var p = BayProfile;
            var a = p[Mathf.Max(0, i - 1)]; var b = p[Mathf.Min(p.Length - 1, i + 1)];
            var t = (b - a).normalized; var n = new Vector2(-t.y, t.x);          // walking floor -> ceiling, left of the tangent = inside
            return n;
        }

        /// <summary>The profile point i at the rounded back end (d = how far it is pulled in).</summary>
        static Vector2 BayPoint(int i, float d)
        {
            var p = BayProfile[i];
            if (d <= 0f) return p;
            if (i <= BayFloorEnd) return new Vector2(p.x * (1f - d / 1.05f), 0f);
            var n = BayNormalIn(i);
            var q = p + n * d;
            return new Vector2(Mathf.Max(0f, q.x), Mathf.Max(0f, q.y));
        }

        /// <summary>Build the room into `bay` (local frame above). F = floor height (saucer-local).</summary>
        static void BuildBikeBay(Transform bay, float b0, System.Func<float, float> underY, System.Func<float, float> upperY, float halfSpan, Material hullMat, Material seamMat, SaucerExt ext)
        {
            float F = b0 + BayFloorUp;
            var chrome = Dissolve("Bay_chrome", Gray(0.93f), 0.86f, false, 0.55f);      // QF14: full metallic read black (it reflected the dark hull interior)
            var steel = Dissolve("Bay_steel", Gray(0.76f), 0.7f, false, 0.45f);
            var floorM = Dissolve("Bay_floor", Gray(0.3f), 0.9f, false, 0.6f);
            var lamp = Dissolve("Bay_lamp", Color.white, 0.5f, false);
            lamp.EnableKeyword("_EMISSION"); lamp.SetColor("_EmissionColor", Color.white * 3.2f); EditorUtility.SetDirty(lamp);
            var dark = Dissolve("Bay_dark", Gray(0.08f), 0.6f, false, 0.6f);

            // ---- the room shell: profile swept along z, a rounded back end, a back plate
            const float Rb = 0.35f; int capSteps = 10;
            var zs = new List<(float z, float d)>();
            for (int k = capSteps; k >= 1; k--) { float th = k * 90f / capSteps * Mathf.Deg2Rad; zs.Add((BayBack + Rb - Rb * Mathf.Sin(th), Rb * (1f - Mathf.Cos(th)))); }
            for (float z = BayBack + Rb; z <= BayFront + 1e-3f; z += 0.3f) zs.Add((z, 0f));
            if (zs[zs.Count - 1].z < BayFront - 1e-3f) zs.Add((BayFront, 0f));
            Mesh Shell(int i0, int i1, string nm)
            {
                var v = new List<Vector3>(); var t = new List<int>();
                // columns: left half reversed (ceiling -> floor centre), then right half (floor centre -> ceiling)
                var idx = new List<(int i, float s)>();
                for (int i = i1; i >= i0; i--) idx.Add((i, -1f));
                for (int i = i0; i <= i1; i++) { if (i == 0 && i0 == 0) continue; idx.Add((i, 1f)); }
                if (i0 == 0) { /* the floor centre is shared once */ }
                int cols = idx.Count;
                foreach (var (z, d) in zs)
                    foreach (var (i, s) in idx) { var p = BayPoint(i, d); v.Add(new Vector3(p.x * s, F + p.y, z)); }
                for (int r = 0; r < zs.Count - 1; r++)
                    for (int c = 0; c < cols - 1; c++)
                    {
                        // the two halves meet at the centre only for the floor / ceiling strips
                        if (i0 != 0 && c == (i1 - i0)) continue;
                        int a = r * cols + c, b = a + 1, cc = a + cols, dd = cc + 1;
                        t.Add(a); t.Add(cc); t.Add(b); t.Add(b); t.Add(cc); t.Add(dd);
                    }
                var m = new Mesh { indexFormat = IndexFormat.UInt32 }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals();
                return SaveMesh(m, nm);
            }
            MeshObject("BayFloor", Shell(0, BayFloorEnd, "Bay_Floor"), floorM, bay, false);
            MeshObject("BayWalls", Shell(BayFloorEnd, BayWallEnd, "Bay_Walls"), steel, bay, false);
            MeshObject("BayCeiling", Shell(BayWallEnd, BayProfile.Length - 1, "Bay_Ceiling"), steel, bay, false);
            {
                // the back plate: the fully pulled-in profile, a fan from its middle
                var v = new List<Vector3>(); var t = new List<int>();
                var loop = new List<Vector3>();
                for (int i = BayProfile.Length - 1; i >= 0; i--) { var p = BayPoint(i, Rb); loop.Add(new Vector3(-p.x, F + p.y, BayBack)); }
                for (int i = 1; i < BayProfile.Length; i++) { var p = BayPoint(i, Rb); loop.Add(new Vector3(p.x, F + p.y, BayBack)); }
                v.Add(new Vector3(0f, F + 0.5f, BayBack)); v.AddRange(loop);
                for (int i = 1; i < loop.Count; i++) { t.Add(0); t.Add(i); t.Add(i + 1); }
                var back = MeshObject("BayBack", SaveMesh(Finish(v, t), "Bay_BackPlate"), steel, bay, false);
                ext.BayBack = back.GetComponent<Renderer>();
                var tv = new List<Vector3>(); var tt = new List<int>();
                var rr = RoundRectLoop(0.42f, 0.27f, 0.12f, 6);
                Sweep(tv, tt, rr.Select(q => new Vector3(q.p.x, F + 0.48f + q.p.y, BayBack + 0.012f)).ToList(), 0.016f, true, 6);
                MeshObject("BayBackTrim", SaveMesh(SmoothFinish(tv, tt), "Bay_BackTrim"), chrome, bay, false);
            }

            // ---- panel seams (thin dark lines round the walls + ceiling) and the chrome ribs
            {
                var sv = new List<Vector3>(); var st = new List<int>();
                foreach (var z in new[] { 0.35f, 1.15f, 1.95f, 2.62f })
                {
                    var path = new List<Vector3>();
                    for (int i = BayProfile.Length - 1; i >= 3; i--) { var p = BayProfile[i] + BayNormalIn(i) * 0.003f; path.Add(new Vector3(-p.x, F + p.y, z)); }
                    for (int i = 3; i < BayProfile.Length; i++) { var p = BayProfile[i] + BayNormalIn(i) * 0.003f; path.Add(new Vector3(p.x, F + p.y, z)); }
                    Sweep(sv, st, path, 0.0055f, false, 4);
                }
                // a long seam along each wall above the drawers
                foreach (var s in new[] { -1f, 1f }) Sweep(sv, st, new List<Vector3> { new Vector3(s * 1.047f, F + 0.5f, BayBack + Rb), new Vector3(s * 1.047f, F + 0.5f, BayFront) }, 0.005f, false, 4);
                MeshObject("BaySeams", SaveMesh(SmoothFinish(sv, st), "Bay_Seams"), dark, bay, false);
            }
            {
                var rv = new List<Vector3>(); var rt = new List<int>();
                // the sweeping side ribs: from the frame's top corners back along the ceiling edge, then down the curved
                // wall to the floor cove near the back (the reference's S-curves)
                Vector2 ProfAt(float s)        // s along the wall section 3 (cove) .. 15 (ceiling edge)
                {
                    int i = Mathf.Clamp(Mathf.FloorToInt(s), 3, 14); float f = s - i;
                    var p = Vector2.Lerp(BayProfile[i], BayProfile[i + 1], f);
                    var n = Vector2.Lerp(BayNormalIn(i), BayNormalIn(i + 1), f).normalized;
                    return p + n * 0.035f;
                }
                foreach (var sgn in new[] { -1f, 1f })
                {
                    var path = new List<Vector3>();
                    for (int k = 0; k <= 40; k++)
                    {
                        float u = k / 40f;
                        float z = Mathf.Lerp(BayFront - 0.02f, BayBack + Rb + 0.05f, u);
                        float s = Mathf.Lerp(14.8f, 4.2f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.12f) / 0.78f)));
                        var p = ProfAt(s); path.Add(new Vector3(sgn * p.x, F + p.y, z));
                    }
                    Sweep(rv, rt, path, 0.034f, false, 10);
                    // the ceiling-recess edge trims, front to back
                    var pe = BayProfile[16] + BayNormalIn(16) * 0.012f;
                    Sweep(rv, rt, new List<Vector3> { new Vector3(sgn * pe.x, F + pe.y, BayBack + Rb), new Vector3(sgn * pe.x, F + pe.y, BayFront) }, 0.018f, false, 8);
                    // a vertical rib each side just inside the frame (it arches up into the ceiling)
                    var arch = new List<Vector3>();
                    for (float s = 4.5f; s <= 15.6f; s += 0.25f) { var p = ProfAt(Mathf.Min(s, 14.99f)); arch.Add(new Vector3(sgn * p.x, F + p.y, BayFront - 0.16f)); }
                    Sweep(rv, rt, arch, 0.026f, false, 8);
                }
                MeshObject("BayRibs", SaveMesh(SmoothFinish(rv, rt), "Bay_Ribs"), chrome, bay, false);
            }

            // ---- ceiling: three round lights in the recessed panel (real lights)
            var lights = new List<Light>();
            {
                var lv = new List<Vector3>(); var lt = new List<int>();
                var bv = new List<Vector3>(); var btt = new List<int>();
                int n = 0;
                foreach (var z in new[] { 0.35f, 1.35f, 2.35f })
                {
                    float y = F + 1.255f, r = 0.115f; int b = lv.Count;
                    lv.Add(new Vector3(0f, y, z));
                    for (int k = 0; k <= 32; k++) lv.Add(new Vector3(Mathf.Cos(k * Mathf.PI / 16f) * r, y, z + Mathf.Sin(k * Mathf.PI / 16f) * r));
                    for (int k = 1; k <= 32; k++) { lt.Add(b); lt.Add(b + k); lt.Add(b + k + 1); }
                    Sweep(bv, btt, Enumerable.Range(0, 32).Select(k => new Vector3(Mathf.Cos(k * Mathf.PI / 16f) * (r + 0.012f), y - 0.004f, z + Mathf.Sin(k * Mathf.PI / 16f) * (r + 0.012f))).ToList(), 0.012f, true, 6);
                    var l = new GameObject(n == 0 ? "BayLight" : "BayLight" + n).AddComponent<Light>(); l.transform.SetParent(bay, false);
                    l.transform.localPosition = new Vector3(0f, F + 1.12f, Mathf.Min(z, 2.0f));
                    l.type = LightType.Point; l.range = 1.6f;      // QF14: kept inside the room (2.4 m lit the slot walls outside) l.intensity = 0f; l.color = Color.white; l.shadows = LightShadows.None;
                    lights.Add(l); n++;
                }
                MeshObject("BayLamps", SaveMesh(Finish(lv, lt), "Bay_Lamps"), lamp, bay, false);
                MeshObject("BayLampBezels", SaveMesh(SmoothFinish(bv, btt), "Bay_LampBezels"), chrome, bay, false);
            }
            ext.BayLight = lights[0]; ext.BayLights = lights.ToArray();

            // ---- drawers low on both walls, a small control panel on the right wall
            {
                var dv = new List<Vector3>(); var dt = new List<int>();
                var hv = new List<Vector3>(); var ht = new List<int>();
                foreach (var s in new[] { -1f, 1f })
                    for (int k = 0; k < 3; k++)
                    {
                        float y = F + 0.09f + k * 0.122f + 0.055f;
                        AddBox(dv, dt, new Vector3(s * (1.05f - 0.012f), y, 2.2f), new Vector3(0.024f, 0.108f, 0.6f));
                        AddBox(hv, ht, new Vector3(s * (1.05f - 0.027f), y + 0.028f, 2.2f), new Vector3(0.012f, 0.016f, 0.17f));
                    }
                // the panel: a rounded plate, one round button, two small ones
                AddBox(dv, dt, new Vector3(1.035f, F + 0.64f, 2.78f), new Vector3(0.02f, 0.2f, 0.13f));
                MeshObject("BayDrawers", SaveMesh(Finish(dv, dt), "Bay_Drawers"), steel, bay, false);
                MeshObject("BayHandles", SaveMesh(Finish(hv, ht), "Bay_Handles"), dark, bay, false);
                var pv = new List<Vector3>(); var pt = new List<int>();
                Ellipsoid(pv, pt, new Vector3(1.024f, F + 0.675f, 2.78f), new Vector3(0.012f, 0.036f, 0.036f), 8, 16);
                MeshObject("BayPanelButton", SaveMesh(SmoothFinish(pv, pt), "Bay_PanelButton"), chrome, bay, false);
                var gv = new List<Vector3>(); var gt = new List<int>();
                foreach (var z in new[] { 2.755f, 2.805f }) Ellipsoid(gv, gt, new Vector3(1.024f, F + 0.595f, z), new Vector3(0.008f, 0.013f, 0.013f), 6, 12);
                MeshObject("BayPanelLights", SaveMesh(SmoothFinish(gv, gt), "Bay_PanelLights"), lamp, bay, false);
            }

            // ---- floor: the raised guide track from the back stop to the opening, a centre pad, two cradles under the bike
            {
                var tv = new List<Vector3>(); var tt = new List<int>();
                void Strip(IList<Vector2> path, float w, float h)
                {
                    // a flat bevelled strip on the floor along an (x, z) path
                    var pts = new List<Vector2>();
                    for (int i = 0; i < path.Count - 1; i++) for (int k = 0; k < 8; k++) pts.Add(Vector2.Lerp(path[i], path[i + 1], k / 8f));
                    pts.Add(path[path.Count - 1]);
                    int b = tv.Count;
                    for (int i = 0; i < pts.Count; i++)
                    {
                        var dir = (pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized; var side = new Vector2(dir.y, -dir.x);
                        var c = pts[i];
                        foreach (var (o, y) in new[] { (-0.5f, 0f), (-0.42f, h), (0.42f, h), (0.5f, 0f) })
                        { var q = c + side * (o * w); tv.Add(new Vector3(q.x, F + 0.002f + y, q.y)); }
                    }
                    for (int i = 0; i < pts.Count - 1; i++)
                        for (int k = 0; k < 3; k++)
                        {
                            int a = b + i * 4 + k, a2 = a + 1, c = a + 4, d = c + 1;
                            tt.Add(a); tt.Add(c); tt.Add(a2); tt.Add(a2); tt.Add(c); tt.Add(d);
                        }
                }
                foreach (var s in new[] { -1f, 1f })
                    Strip(new[] { new Vector2(s * 0.06f, -0.05f), new Vector2(s * 0.09f, 0.45f), new Vector2(s * 0.22f, 1.25f), new Vector2(s * 0.38f, 1.85f), new Vector2(s * 0.42f, 2.3f), new Vector2(s * 0.42f, BayFront + 0.02f) }, 0.11f, 0.014f);
                MeshObject("BayTrack", SaveMesh(Finish(tv, tt), "Bay_Track"), chrome, bay, false);
                // the stop at the back: a rounded chrome fin
                var sv = new List<Vector3>(); var stt = new List<int>();
                Ellipsoid(sv, stt, new Vector3(0f, F + 0.02f, -0.36f), new Vector3(0.06f, 0.15f, 0.14f), 10, 16, d => d.y < -0.05f);
                // the centre pad
                var pad = RoundRectLoop(0.15f, 0.1f, 0.05f, 6);
                int pad0 = sv.Count;
                LoopSweep(sv, stt, pad, new[] { new Vector2(0.02f, 0f), new Vector2(0.02f, 0.012f), new Vector2(0.008f, 0.02f), new Vector2(-0.03f, 0.022f), new Vector2(-0.09f, 0.022f) });
                for (int i = pad0; i < sv.Count; i++) { var q = sv[i]; sv[i] = new Vector3(q.x, F + q.z, 0.55f + q.y); }
                // the cradles: rounded U lips round a shallow dish
                foreach (var s in new[] { -1f, 1f })
                {
                    var lp = RoundRectLoop(0.17f, 0.12f, 0.07f, 8);
                    int st0 = sv.Count;
                    LoopSweep(sv, stt, lp, new[] { new Vector2(0.05f, 0f), new Vector2(0.05f, 0.02f), new Vector2(0.04f, 0.045f), new Vector2(0.02f, 0.058f), new Vector2(-0.005f, 0.055f), new Vector2(-0.02f, 0.035f), new Vector2(-0.03f, 0.018f), new Vector2(-0.1f, 0.016f) });
                    for (int i = st0; i < sv.Count; i++) { var q = sv[i]; sv[i] = new Vector3(s * 0.42f + q.x, F + q.z, 1.75f + q.y); }      // under the bike's front half
                }
                MeshObject("BayFloorFittings", SaveMesh(SmoothFinish(sv, stt), "Bay_FloorFittings"), chrome, bay, false);
            }

            // ---- the opening: a thick rounded chrome frame, a short silver sleeve out to the bulkhead, the bulkhead itself
            float yc = F - BayFloorUp + (BayOpenBot + BayOpenTop) * 0.5f, hh = (BayOpenTop - BayOpenBot) * 0.5f;
            float outerHW = BayOpenHW + BayFrameBar, outerHH = hh + BayFrameBar, rIn = 0.24f, rOut = rIn + BayFrameBar;
            {
                var fv = new List<Vector3>(); var ft = new List<int>();
                var loop = RoundRectLoop(BayOpenHW, hh, rIn, 10);
                LoopSweep(fv, ft, loop, new[]
                {
                    new Vector2(0f, BayFront), new Vector2(0f, BayFrameFront - 0.07f), new Vector2(0.012f, BayFrameFront - 0.015f), new Vector2(0.04f, BayFrameFront + 0.02f),
                    new Vector2(0.075f, BayFrameFront + 0.03f), new Vector2(0.105f, BayFrameFront + 0.018f), new Vector2(BayFrameBar, BayFrameFront - 0.005f), new Vector2(BayFrameBar, BayFront),
                });
                for (int i = 0; i < fv.Count; i++) fv[i] = new Vector3(fv[i].x, yc + fv[i].y, fv[i].z);
                MeshObject("BayFrame", SaveMesh(SmoothFinish(fv, ft), "Bay_Frame"), chrome, bay, false);
                // the sleeve: the frame's outer outline carried forward to the curved bulkhead (r = BayNotch)
                var ov = new List<Vector3>(); var ot = new List<int>();
                var outer = RoundRectLoop(outerHW, outerHH, rOut, 10);
                int n = outer.Count;
                foreach (var (p, _) in outer) ov.Add(new Vector3(p.x, yc + p.y, BayFrameFront - 0.005f));
                foreach (var (p, _) in outer) ov.Add(new Vector3(p.x, yc + p.y, Mathf.Sqrt(BayNotch * BayNotch - p.x * p.x) + 0.004f));
                for (int i = 0; i < n; i++) { int a = i, b = (i + 1) % n, c = i + n, d = b + n; ot.Add(a); ot.Add(c); ot.Add(b); ot.Add(b); ot.Add(c); ot.Add(d); }
                MeshObject("BaySleeve", SaveMesh(SmoothFinish(ov, ot), "Bay_Sleeve"), hullMat, bay, false);
                // the bulkhead: the hull's cut face at r = BayNotch across the hatch sector, minus the sleeve's outline
                var bv = new List<Vector3>(); var bt = new List<int>();
                float yLo = underY(BayNotch) + 0.002f, yHi = upperY(BayNotch) - 0.002f;
                int cols = 240;
                float HoleHalf(float x, bool top)
                {
                    // the outline's y extent at x (rounded corners)
                    float ax = Mathf.Abs(x);
                    if (ax >= outerHW) return float.NaN;
                    float e = outerHH;
                    if (ax > outerHW - rOut) { float dx = ax - (outerHW - rOut); e = outerHH - rOut + Mathf.Sqrt(Mathf.Max(0f, rOut * rOut - dx * dx)); }
                    return yc + (top ? e : -e);
                }
                for (int c = 0; c < cols; c++)
                {
                    float a0 = Mathf.Lerp(-halfSpan, halfSpan, (float)c / cols), a1 = Mathf.Lerp(-halfSpan, halfSpan, (float)(c + 1) / cols);
                    Vector3 P(float a, float y) => new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * BayNotch, y, Mathf.Cos(a * Mathf.Deg2Rad) * BayNotch);
                    float x0 = Mathf.Sin(a0 * Mathf.Deg2Rad) * BayNotch, x1 = Mathf.Sin(a1 * Mathf.Deg2Rad) * BayNotch;
                    float lo0 = HoleHalf(x0, false), lo1 = HoleHalf(x1, false), hi0 = HoleHalf(x0, true), hi1 = HoleHalf(x1, true);
                    if (float.IsNaN(lo0) && float.IsNaN(lo1)) { Quad(bv, bt, P(a0, yLo), P(a0, yHi), P(a1, yHi), P(a1, yLo)); continue; }
                    // a column crossing the outline's side: split at the outline (fine columns keep the error < 1 cm)
                    if (float.IsNaN(lo0)) { lo0 = hi0 = yc; } if (float.IsNaN(lo1)) { lo1 = hi1 = yc; }
                    Quad(bv, bt, P(a0, yLo), P(a0, Mathf.Max(yLo, lo0)), P(a1, Mathf.Max(yLo, lo1)), P(a1, yLo));
                    Quad(bv, bt, P(a0, Mathf.Min(yHi, hi0)), P(a0, yHi), P(a1, yHi), P(a1, Mathf.Min(yHi, hi1)));
                }
                MeshObject("BayBulkhead", SaveMesh(Finish(bv, bt), "Bay_Bulkhead"), hullMat, bay, false);
            }

            // ---- its own reflections: a box-projected probe that sees only the saucer layer (the room itself). The box
            // stays clear of the saucer's centre so the hull keeps the ship probe.
            {
                // (probe boxes are world-aligned: a small box round the room's middle, well clear of the ship's axis; the
                // room's renderers are anchored to it)
                var pg = new GameObject("BayReflections"); pg.transform.SetParent(bay, false); pg.transform.localPosition = new Vector3(0f, F + 0.63f, 1.9f);
                var rp = pg.AddComponent<ReflectionProbe>();
                rp.mode = ReflectionProbeMode.Realtime; rp.refreshMode = ReflectionProbeRefreshMode.ViaScripting; rp.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
                rp.size = new Vector3(1.6f, 1.4f, 1.6f); rp.boxProjection = false; rp.resolution = 128; rp.hdr = true; rp.importance = 3;
                rp.nearClipPlane = 0.05f; rp.farClipPlane = 8f; rp.intensity = 1.0f;
                rp.clearFlags = ReflectionProbeClearFlags.SolidColor; rp.backgroundColor = Gray(0.3f);
                rp.cullingMask = 1 << Seven.SaucerProbe.Layer;
                ext.BayProbe = rp;
                foreach (var r in bay.GetComponentsInChildren<Renderer>(true))
                    if (r.name != "BayFrame" && r.name != "BaySleeve" && r.name != "BayBulkhead") { r.probeAnchor = pg.transform; r.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes; }
            }
            Log($"QF14 (3) bike bay: room {BayFront - BayBack:0.00} x {2f * 1.05f:0.00} x {BayProfile[BayProfile.Length - 1].y:0.00} m, opening {2f * BayOpenHW:0.00} x {BayOpenTop - BayOpenBot:0.00} m (bike 3.00 x 1.23 x 0.95), frame bar {BayFrameBar} m, 3 ceiling lights, bulkhead at r {BayNotch}");
        }
    }
}
