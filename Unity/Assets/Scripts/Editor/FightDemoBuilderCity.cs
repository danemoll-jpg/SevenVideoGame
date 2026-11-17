using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using L = Seven.MoonLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 7 (6b): the city inside the Moon, seen from the stone terrace after the escape. Built for the view and for
    /// the fog: few, large, simple forms that dissolve into the bright haze like Kyle's paintings. Only the nearest
    /// structure (the colossal host building the prison is bolted into), the pyramids around the dark depression and a
    /// few of Tunneler's machines read clearly; everything farther is layered silhouettes.
    /// Every city mesh is single-sided with outward faces, so from inside the host (the cell block's catwalk "cavern")
    /// it is simply not there. City objects live on layer 20, lit by their own soft overhead light.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const int CityLayer = 20;

        // ---------------------------------------------------------------------------- geometry helpers (outward faces)
        /// <summary>Quad seen from its front: bottom-left, top-left, top-right, bottom-right.</summary>
        static void CF(List<Vector3> v, List<int> t, Vector3 bl, Vector3 tl, Vector3 tr, Vector3 br)
        {
            int i = v.Count; v.Add(bl); v.Add(tl); v.Add(tr); v.Add(br);
            t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        /// <summary>Oriented box (centre, size, rotation) with outward-facing faces.</summary>
        static void OBox(List<Vector3> v, List<int> t, Vector3 c, Vector3 size, Quaternion q)
        {
            var h = size * 0.5f;
            Vector3 P(float x, float y, float z) => c + q * new Vector3(x * h.x, y * h.y, z * h.z);
            CF(v, t, P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1));   // -z
            CF(v, t, P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), P(-1, -1, 1));       // +z
            CF(v, t, P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), P(-1, -1, -1));   // -x
            CF(v, t, P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1));       // +x
            CF(v, t, P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1));       // top
            CF(v, t, P(-1, -1, 1), P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1));   // bottom
        }
        static void ABox(List<Vector3> v, List<int> t, Vector3 min, Vector3 max) => OBox(v, t, (min + max) * 0.5f, max - min, Quaternion.identity);

        /// <summary>Flip every triangle whose normal points away from what <paramref name="want"/> says.</summary>
        static void Orient(List<Vector3> v, List<int> t, Func<Vector3, Vector3> want)
        {
            for (int i = 0; i < t.Count; i += 3)
            {
                Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                var n = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(n, want((a + b + c) / 3f)) < 0f) { (t[i + 1], t[i + 2]) = (t[i + 2], t[i + 1]); }
            }
        }

        static Mesh CityMesh(List<Vector3> v, List<int> t, string name)
        {
            var m = new Mesh { indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v); m.SetTriangles(t, 0);
            m.RecalculateNormals();
            return SaveMesh(m, name);
        }

        static GameObject CityObject(string name, List<Vector3> v, List<int> t, Material mat, Transform parent)
        {
            var go = MeshObject(name, CityMesh(v, t, "City_" + name), mat, parent, false);
            go.layer = CityLayer;
            return go;
        }

        static Material CityMat(string name, float gray, float smooth = 0.15f, bool twoSided = false, float metallic = 0f)
        {
            var mat = Dissolve(name, Gray(gray), smooth, false, metallic);
            mat.SetFloat("_Cull", twoSided ? 0f : 2f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Vector3 ShellUp(Vector3 p) => (L.ShellCenter - p).normalized;

        /// <summary>Stepped terrace height (flat steps that follow the shell's rise away from the basin centre).</summary>
        static float TerraceY(float r)
        {
            const float step = 110f;
            float r0 = Mathf.Floor(r / step) * step + step;      // each step is level at the height of its outer edge
            var bc = L.BasinCenter;
            return L.ShellY(bc.x + r0, bc.z) + 3f;
        }
        static float CityGround(float x, float z)
        {
            var bc = L.BasinCenter;
            float r = new Vector2(x - bc.x, z - bc.z).magnitude;
            return r < 180f ? bc.y + 1f : r < 1320f ? TerraceY(r) : L.ShellY(x, z);
        }

        // ---------------------------------------------------------------------------- the city
        static Seven.CityMotion BuildCity(Transform world, MoonMats m)
        {
            var root = new GameObject("City").transform; root.SetParent(world, false);
            var host = CityMat("City_host", 0.5f, 0.25f);
            var pale = CityMat("City_pale", 0.6f);
            var paleTwo = CityMat("City_pale_2s", 0.62f, 0.15f, true);
            var shellMat = CityMat("City_shell", 0.4f, 0.05f);
            var dark = CityMat("City_dark", 0.03f, 0.05f);
            var machine = CityMat("City_machine", 0.1f, 0.35f, false, 0.3f);
            var glow = m.glow;
            var rnd = new System.Random(7007);
            float R() => (float)rnd.NextDouble();
            var bc = L.BasinCenter;
            var motion = root.gameObject.AddComponent<Seven.CityMotion>();
            var conveyors = new List<Seven.CityMotion.Conveyor>();
            var lifts = new List<Seven.CityMotion.Lift>();
            var arms = new List<Seven.CityMotion.Arm>();
            var levelC = new Vector3(16f, -10f, -40f);

            // ---- 1. The colossal host structure: stepped tiers rising from far below the terrace; the prison is bolted
            // into its base. Faces only toward the outside (the level's rooms are inside its volume).
            {
                var v = new List<Vector3>(); var t = new List<int>();
                var wv = new List<Vector3>(); var wt = new List<int>();
                for (int k = 0; k < 7; k++)
                {
                    float zf = L.HostFrontZ + k * 9.5f, top = 30f + k * 58f, inset = k * 15f;
                    if (k == 0)
                    {
                        // the lowest tier leaves the mouth of the rough mining passage open (x 15.1..17.9, up to y 1)
                        ABox(v, t, new Vector3(-115f, -120f, zf), new Vector3(L.PassageX.x - 0.5f, top, 80f));
                        ABox(v, t, new Vector3(L.PassageX.y + 0.5f, -120f, zf), new Vector3(145f, top, 80f));
                        ABox(v, t, new Vector3(L.PassageX.x - 0.5f, 1.0f, zf), new Vector3(L.PassageX.y + 0.5f, top, 80f));
                        ABox(v, t, new Vector3(L.PassageX.x - 0.5f, -120f, zf), new Vector3(L.PassageX.y + 0.5f, L.Lower - 0.7f, 80f));
                    }
                    else ABox(v, t, new Vector3(-115f + inset, -120f, zf), new Vector3(145f - inset, top, 80f));
                    // a few deep black openings on each tier face (suggested, not detailed)
                    int n = 2 + (k % 3);
                    for (int w = 0; w < n; w++)
                    {
                        if (k == 0) break;          // the lowest tier holds the prison machinery instead
                        float cx = Mathf.Lerp(-90f + inset, 120f - inset, (w + 0.5f) / n) + (R() - 0.5f) * 12f;
                        float h = 14f + R() * 18f, y0 = top - 58f + 10f + R() * 14f;
                        CF(wv, wt, new Vector3(cx - 4f, y0, zf - 0.15f), new Vector3(cx - 4f, y0 + h, zf - 0.15f), new Vector3(cx + 4f, y0 + h, zf - 0.15f), new Vector3(cx + 4f, y0, zf - 0.15f));
                    }
                }
                // the base below the terrace and bay: a sheer wall down into the haze
                ABox(v, t, new Vector3(-115f, -150f, -112f), new Vector3(145f, L.Bay - 0.6f, L.HostFrontZ));
                CityObject("HostStructure", v, t, host, root);
                CityObject("HostOpenings", wv, wt, dark, root);
                // Tunneler's recent additions: pipes along the face, a fence on the first setback, lamps, vents, lifts.
                var pv = new List<Vector3>(); var pt = new List<int>();
                foreach (var (y, r) in new[] { (6.5f, 0.55f), (8.2f, 0.38f), (12.5f, 0.7f) })
                    Sweep(pv, pt, new[] { new Vector3(-60f, y, L.HostFrontZ - 0.9f), new Vector3(90f, y, L.HostFrontZ - 0.9f) }, r, false, 8);
                foreach (var x in new[] { -24f, 2f, 38f, 61f })
                    Sweep(pv, pt, new[] { new Vector3(x, L.Bay - 40f, L.HostFrontZ - 1.2f), new Vector3(x, 29f, L.HostFrontZ - 1.2f), new Vector3(x, 30.5f, L.HostFrontZ + 2f) }, 0.45f, false, 8);
                for (float x = -100f; x < 130f; x += 6f) ABox(pv, pt, new Vector3(x - 0.12f, 30f, L.HostFrontZ + 1.2f), new Vector3(x + 0.12f, 33.2f, L.HostFrontZ + 1.45f));
                ABox(pv, pt, new Vector3(-100f, 32.9f, L.HostFrontZ + 1.2f), new Vector3(130f, 33.2f, L.HostFrontZ + 1.45f));
                ABox(pv, pt, new Vector3(-100f, 31.4f, L.HostFrontZ + 1.25f), new Vector3(130f, 31.6f, L.HostFrontZ + 1.4f));
                foreach (var (x, y) in new[] { (-38f, 18f), (52f, 22f), (78f, 15f) }) ABox(pv, pt, new Vector3(x - 4f, y - 3f, L.HostFrontZ - 2.2f), new Vector3(x + 4f, y + 3f, L.HostFrontZ));
                CityObject("PrisonAddOns", pv, pt, m.metalDark, root);      // double-sided (swept pipes)
                var lv = new List<Vector3>(); var lt = new List<int>();
                foreach (var (x, y) in new[] { (-50f, 14f), (-18f, 9.5f), (7f, 14f), (31f, 9.5f), (66f, 14f), (88f, 9.5f), (-80f, 30.5f), (-20f, 31f), (40f, 31f), (100f, 30.5f) })
                    ABox(lv, lt, new Vector3(x - 0.6f, y - 0.15f, L.HostFrontZ - 1.6f), new Vector3(x + 0.6f, y + 0.15f, L.HostFrontZ - 1.2f));
                CityObject("PrisonLamps", lv, lt, glow, root);
                // freight lifts on the host face
                foreach (var (x, top) in new[] { (-70f, 160f), (110f, 190f) })
                {
                    var rv = new List<Vector3>(); var rt = new List<int>();
                    ABox(rv, rt, new Vector3(x - 0.5f, -120f, L.HostFrontZ - 1.5f), new Vector3(x + 0.5f, top, L.HostFrontZ - 0.5f));
                    ABox(rv, rt, new Vector3(x + 4.5f, -120f, L.HostFrontZ - 1.5f), new Vector3(x + 5.5f, top, L.HostFrontZ - 0.5f));
                    CityObject("LiftRail" + x, rv, rt, machine, root);
                    var car = Car("LiftCar" + x, new Vector3(6f, 4.5f, 3f), glow, root);
                    car.position = new Vector3(x + 2.5f, 20f, L.HostFrontZ - 2.6f);
                    lifts.Add(new Seven.CityMotion.Lift { Car = car, Bottom = new Vector3(x + 2.5f, -100f, L.HostFrontZ - 2.6f), Top = new Vector3(x + 2.5f, top - 6f, L.HostFrontZ - 2.6f), Period = 50f + R() * 30f, Phase = R() });
                }
            }

            // ---- 2. The inside of the Moon: the shell (landscape at first sight) and broad terraces down into the basin.
            {
                var v = new List<Vector3>(); var t = new List<int>();
                int lat = 40, lon = 96;
                for (int i = 0; i < lat; i++)
                    for (int j = 0; j < lon; j++)
                    {
                        Vector3 S(int a, int b)
                        {
                            float th = Mathf.Lerp(0.25f, 178f, (float)a / lat) * Mathf.Deg2Rad, ph = (float)b / lon * Mathf.PI * 2f;
                            return L.ShellCenter + new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), -Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph)) * L.ShellRadius;
                        }
                        CF(v, t, S(i, j), S(i + 1, j), S(i + 1, j + 1), S(i, j + 1));
                    }
                Orient(v, t, ShellUp);
                CityObject("MoonShell", v, t, shellMat, root);
                // terraces: concentric flat steps from the quiet centre outward (lathe, faces toward the sphere centre)
                var tv = new List<Vector3>(); var tt = new List<int>();
                var prof = new List<Vector2> { new Vector2(180f, bc.y - 2f) };
                for (float r = 180f; r < 1320f; r += 110f) { float y = TerraceY(r + 1f); prof.Add(new Vector2(r, y)); prof.Add(new Vector2(r + 110f, y)); }
                int seg = 120;
                for (int s = 0; s < seg; s++)
                    for (int i = 0; i + 1 < prof.Count; i++)
                    {
                        float a0 = (float)s / seg * 360f, a1 = (float)(s + 1) / seg * 360f;
                        var p00 = bc + Polar(prof[i].x, a0, prof[i].y - bc.y); var p01 = bc + Polar(prof[i + 1].x, a0, prof[i + 1].y - bc.y);
                        var p10 = bc + Polar(prof[i].x, a1, prof[i].y - bc.y); var p11 = bc + Polar(prof[i + 1].x, a1, prof[i + 1].y - bc.y);
                        CF(tv, tt, p00, p01, p11, p10);
                    }
                Orient(tv, tt, ShellUp);
                CityObject("Terraces", tv, tt, pale, root);
                // terrace walls hide their stepped edges with a darker band on the risers (a separate dark ring per step)
            }

            // ---- 3. The quiet centre: monumental pyramids around a dark circular depression.
            {
                var dv = new List<Vector3>(); var dt = new List<int>();
                for (int k = 0; k < 64; k++)
                {
                    var a = bc + Polar(118f, k * 360f / 64f, 1.5f); var b = bc + Polar(118f, (k + 1) * 360f / 64f, 1.5f);
                    dv.Add(bc + Vector3.up * 1.5f); dv.Add(a); dv.Add(b); dt.Add(dv.Count - 3); dt.Add(dv.Count - 2); dt.Add(dv.Count - 1);
                }
                Orient(dv, dt, _ => Vector3.up);
                CityObject("DarkDepression", dv, dt, dark, root);
                var rv = new List<Vector3>(); var rt = new List<int>();
                for (int k = 0; k < 64; k++)
                {
                    float a = k * 360f / 64f;
                    OBox(rv, rt, bc + Polar(126f, a + 2.8f, 4f), new Vector3(16f, 7f, 12f), Quaternion.Euler(0f, -a, 0f));
                }
                CityObject("DepressionRim", rv, rt, pale, root);
                var pv = new List<Vector3>(); var pt = new List<int>();
                for (int k = 0; k < 5; k++)
                {
                    float a = k * 72f + 18f, size = 150f + (k % 2) * 30f, h = 120f + (k % 3) * 18f;
                    var c = bc + Polar(275f, a, 0f); c.y = CityGround(c.x, c.z);
                    var q = Quaternion.Euler(0f, -a, 0f);
                    Vector3 B(float x, float z) => c + q * new Vector3(x * size * 0.5f, 0f, z * size * 0.5f);
                    var apex = c + Vector3.up * h;
                    var corners = new[] { B(-1, -1), B(1, -1), B(1, 1), B(-1, 1) };
                    for (int i = 0; i < 4; i++) { pv.Add(corners[i]); pv.Add(apex); pv.Add(corners[(i + 1) % 4]); pt.Add(pv.Count - 3); pt.Add(pv.Count - 2); pt.Add(pv.Count - 1); }
                    // a stepped base under each
                    OBox(pv, pt, c + Vector3.up * 3f, new Vector3(size * 1.15f, 8f, size * 1.15f), q);
                }
                CityObject("Pyramids", pv, pt, paleTwo, root);
                // circular structures aligned across the basin (an astronomical axis), standing rings
                var cv = new List<Vector3>(); var ct = new List<int>();
                foreach (var (dx, rr) in new[] { (-420f, 70f), (0f, 95f), (460f, 70f) })
                {
                    var c = new Vector3(bc.x + dx, 0f, bc.z + (dx == 0f ? -560f : 0f)); c.y = CityGround(c.x, c.z) + rr + 6f;
                    var axis = Vector3.right;
                    var ring = Enumerable.Range(0, 49).Select(i => c + Quaternion.AngleAxis(i * 7.5f, axis) * (Vector3.up * rr)).ToList();
                    Sweep(cv, ct, ring, rr * 0.06f, true, 8);
                }
                var ringGo = MeshObject("AlignedRings", SaveMesh(Finish(cv, ct), "City_AlignedRings"), paleTwo, root, false); ringGo.layer = CityLayer;
            }

            // ---- 4. Ancient lunar architecture over the whole inner surface: stepped buildings, slender observation
            // towers, bridges. Fewer, larger forms the farther they are; up the curve until they hang overhead.
            {
                var v = new List<Vector3>(); var t = new List<int>();
                var ov = new List<Vector3>(); var ot = new List<int>();
                var towerTops = new List<Vector3>();
                int placed = 0;
                for (int n = 0; n < 900 && placed < 150; n++)
                {
                    float th = Mathf.Lerp(16f, 128f, Mathf.Pow(R(), 0.8f)), ph = R() * 360f;
                    var dir = new Vector3(Mathf.Sin(th * Mathf.Deg2Rad) * Mathf.Cos(ph * Mathf.Deg2Rad), -Mathf.Cos(th * Mathf.Deg2Rad), Mathf.Sin(th * Mathf.Deg2Rad) * Mathf.Sin(ph * Mathf.Deg2Rad));
                    var p = L.ShellCenter + dir * L.ShellRadius;
                    float rFlat = new Vector2(p.x - bc.x, p.z - bc.z).magnitude;
                    if (rFlat < 1320f) p.y = CityGround(p.x, p.z);
                    if ((p - levelC).magnitude < 420f) continue;             // keep the host and the bay clear
                    var up = -dir;
                    var q = Quaternion.FromToRotation(Vector3.up, up) * Quaternion.Euler(0f, R() * 90f, 0f);
                    float scale = Mathf.Lerp(0.7f, 2.4f, Mathf.InverseLerp(16f, 128f, th));
                    if (R() < 0.3f)
                    {
                        // slender observation tower with a disc on top
                        float h = (120f + R() * 220f) * scale, r = (5f + R() * 4f) * scale;
                        OBox(v, t, p + up * h * 0.5f, new Vector3(r * 2f, h, r * 2f), q);
                        OBox(v, t, p + up * h, new Vector3(r * 6f, r * 0.8f, r * 6f), q);
                        towerTops.Add(p + up * h * 0.82f);
                    }
                    else
                    {
                        // stepped building (2-4 tiers), a deep black opening on some
                        int tiers = 2 + rnd.Next(3);
                        float w = (60f + R() * 160f) * scale, d = w * (0.6f + R() * 0.5f), hh = (25f + R() * 55f) * scale, y = 0f;
                        for (int k = 0; k < tiers; k++)
                        {
                            float f = 1f - k * 0.24f;
                            OBox(v, t, p + up * (y + hh * 0.5f), new Vector3(w * f, hh, d * f), q);
                            if (k == 0 && R() < 0.55f) OBox(ov, ot, p + up * (y + hh * 0.4f) + q * new Vector3(0f, 0f, -d * f * 0.5f - 0.3f), new Vector3(w * f * 0.18f, hh * 0.5f, 0.6f), q);
                            y += hh;
                        }
                    }
                    placed++;
                }
                // bridges between neighbouring towers
                for (int i = 0; i + 1 < towerTops.Count && i < 16; i += 2)
                {
                    var a = towerTops[i]; var b = towerTops.OrderBy(x => (x - a).sqrMagnitude).Skip(1).First();
                    if ((b - a).magnitude > 900f) continue;
                    OBox(v, t, (a + b) * 0.5f, new Vector3(8f, 6f, (b - a).magnitude), Quaternion.LookRotation(b - a));
                }
                CityObject("AncientCity", v, t, pale, root);
                CityObject("AncientOpenings", ov, ot, dark, root);
                // "only one of many": colossal stepped siblings of the host, near enough to read as giants
                var sv = new List<Vector3>(); var st = new List<int>();
                foreach (var (c0, w, h) in new[] { (new Vector3(-360f, 0f, -90f), 230f, 640f), (new Vector3(390f, 0f, 30f), 260f, 760f), (new Vector3(-90f, 0f, 560f), 300f, 900f), (new Vector3(260f, 0f, -520f), 180f, 420f) })
                {
                    var c = c0; c.y = CityGround(c.x, c.z) - 20f;
                    for (int k = 0; k < 6; k++)
                    {
                        float f = 1f - k * 0.14f, hk = h / 6f;
                        ABox(sv, st, c + new Vector3(-w * f * 0.5f, k * hk, -w * f * 0.4f), c + new Vector3(w * f * 0.5f, (k + 1) * hk, w * f * 0.4f));
                    }
                    var car = Car("SiblingLift" + c0.x, new Vector3(8f, 6f, 4f), glow, root);
                    car.position = c + new Vector3(w * 0.3f, h * 0.3f, -w * 0.4f - 3f);
                    lifts.Add(new Seven.CityMotion.Lift { Car = car, Bottom = c + new Vector3(w * 0.3f, 10f, -w * 0.4f - 3f), Top = c + new Vector3(w * 0.3f, h * 0.6f, -w * 0.4f - 3f), Period = 60f + R() * 40f, Phase = R() });
                }
                CityObject("Giants", sv, st, host, root);
            }

            // ---- 5. Tunneler's operation cutting through it: conveyors over old avenues with carts of light, excavators
            // biting into terraces, the quiet district that starts up during the reveal, the terrace edge that breaks.
            {
                var mv = new List<Vector3>(); var mt = new List<int>();
                var routes = new[]
                {
                    (new Vector3(-380f, 0f, -420f), new Vector3(320f, 0f, -1250f), false),
                    (new Vector3(-700f, 0f, -980f), new Vector3(560f, 0f, -760f), false),
                    (new Vector3(180f, 0f, -300f), new Vector3(760f, 0f, -1400f), false),
                    (new Vector3(-140f, 0f, -380f), new Vector3(-620f, 0f, -1200f), true),      // the quiet district
                };
                int ci = 0;
                foreach (var (a, b, quiet) in routes)
                {
                    var path = new List<Vector3>();
                    int steps = Mathf.CeilToInt((b - a).magnitude / 45f);
                    for (int i = 0; i <= steps; i++) { var p = Vector3.Lerp(a, b, (float)i / steps); p.y = CityGround(p.x, p.z) + 14f; path.Add(p); }
                    for (int i = 0; i + 1 < path.Count; i++)
                    {
                        var p0 = path[i]; var p1 = path[i + 1];
                        OBox(mv, mt, (p0 + p1) * 0.5f, new Vector3(7f, 1.6f, (p1 - p0).magnitude + 0.5f), Quaternion.LookRotation(p1 - p0));
                        if (i % 3 == 0) OBox(mv, mt, p0 + Vector3.down * 8f, new Vector3(1.6f, 16f, 1.6f), Quaternion.identity);
                    }
                    var carts = new List<Transform>();
                    for (int k = 0; k < 14; k++) { var cart = Car($"Cart{ci}_{k}", new Vector3(3.2f, 1.6f, 4.5f), glow, root); cart.position = path[(k * path.Count / 14) % path.Count] + Vector3.up * 1.6f; carts.Add(cart); }
                    conveyors.Add(new Seven.CityMotion.Conveyor { Path = path.Select(p => p + Vector3.up * 1.6f).ToArray(), Carts = carts.ToArray(), Speed = 7f + R() * 5f, Quiet = quiet });
                    ci++;
                }
                // excavators: dark bodies with a long arm (pivot) and bucket
                foreach (var (p0, yaw, quiet) in new[] { (new Vector3(-120f, 0f, -322f), 200f, true), (new Vector3(260f, 0f, -640f), 130f, false), (new Vector3(-330f, 0f, -780f), 60f, false) })
                {
                    var p = p0; p.y = CityGround(p.x, p.z);
                    var q = Quaternion.Euler(0f, yaw, 0f);
                    OBox(mv, mt, p + Vector3.up * 8f, new Vector3(30f, 16f, 22f), q);
                    var pivot = new GameObject("ExcavatorArm").transform; pivot.SetParent(root, false);
                    pivot.position = p + Vector3.up * 16f; pivot.rotation = q;
                    var av = new List<Vector3>(); var at = new List<int>();
                    OBox(av, at, new Vector3(0f, 10f, 22f), new Vector3(4f, 4f, 48f), Quaternion.Euler(-18f, 0f, 0f));
                    OBox(av, at, new Vector3(0f, 2f, 46f), new Vector3(9f, 8f, 7f), Quaternion.identity);
                    var armGo = CityObject("ArmMesh" + p0.x, av, at, machine, pivot);
                    armGo.transform.localPosition = Vector3.zero; armGo.transform.localRotation = Quaternion.identity;
                    var lamp = Car("ExcavatorLamp" + p0.x, new Vector3(1.6f, 1.2f, 1.2f), glow, pivot);
                    lamp.localPosition = new Vector3(0f, 6f, 4f);
                    arms.Add(new Seven.CityMotion.Arm { Pivot = pivot, Swing = 24f + R() * 14f, Period = 11f + R() * 8f, Phase = R(), Quiet = quiet });
                }
                CityObject("Machines", mv, mt, machine, root);
                // the ancient terrace whose edge breaks: a stepped building in the quiet district with a loose block on top
                var fv = new List<Vector3>(); var ft = new List<int>();
                var fs = L.FallingStoneAt;
                float gy = CityGround(fs.x, fs.z - 30f), topY = fs.y - 9f;
                ABox(fv, ft, new Vector3(fs.x - 45f, gy, fs.z - 70f), new Vector3(fs.x + 45f, topY - 40f, fs.z + 1f));
                ABox(fv, ft, new Vector3(fs.x - 36f, topY - 40f, fs.z - 62f), new Vector3(fs.x + 36f, topY, fs.z - 6f));
                ABox(fv, ft, new Vector3(fs.x - 22f, topY, fs.z - 34f), new Vector3(fs.x + 24f, topY + 4f, fs.z - 8f));
                CityObject("BreakingTerrace", fv, ft, pale, root);
                var bv = new List<Vector3>(); var bt = new List<int>();
                // a broken chunk of the terrace edge (three slabs, not a clean cube)
                OBox(bv, bt, Vector3.zero, new Vector3(26f, 12f, 18f), Quaternion.Euler(4f, 12f, -3f));
                OBox(bv, bt, new Vector3(-5f, 7f, 2f), new Vector3(15f, 7f, 13f), Quaternion.Euler(-9f, 30f, 6f));
                OBox(bv, bt, new Vector3(8f, -6f, -3f), new Vector3(12f, 6f, 10f), Quaternion.Euler(12f, -20f, 9f));
                var stone = CityObject("FallingStone", bv, bt, pale, root);
                stone.transform.position = fs;
                motion.FallingStone = stone.transform;
                motion.StoneLandAt = new Vector3(fs.x + 4f, CityGround(fs.x, fs.z + 22f) + 9f, fs.z + 22f);
                motion.Dust = MakeDust(root, m);
            }

            // ---- 6. Light: the scene's shadowed key light is steered overhead during the city blend (Chapter1Director.CityLook):
            // one broad, diffuse, unexplained source above the city - pale tops, dark streets. (A separate unshadowed light
            // here became URP's main light and lit the interiors through their ceilings.)

            motion.Conveyors = conveyors.ToArray(); motion.Lifts = lifts.ToArray(); motion.Arms = arms.ToArray();
            foreach (var tr in root.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = CityLayer;
            Log($"city: {root.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3)} triangles, {conveyors.Count} conveyors, {lifts.Count} lifts, {arms.Count} excavator arms");
            return motion;
        }

        static readonly Dictionary<string, Mesh> _carMeshes = new Dictionary<string, Mesh>();
        static Transform Car(string name, Vector3 size, Material glow, Transform parent)
        {
            string key = "City_Car_" + size.x + "x" + size.y + "x" + size.z;
            if (!_carMeshes.TryGetValue(key, out var mesh) || mesh == null)
            {
                var v = new List<Vector3>(); var t = new List<int>();
                OBox(v, t, Vector3.zero, size, Quaternion.identity);
                mesh = CityMesh(v, t, key); _carMeshes[key] = mesh;
            }
            var go = MeshObject(name, mesh, glow, parent, false);
            go.layer = CityLayer;
            return go.transform;
        }

        static ParticleSystem MakeDust(Transform parent, MoonMats m)
        {
            var go = new GameObject("CityDust"); go.transform.SetParent(parent, false); go.layer = CityLayer;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 9f);
            main.startSize = new ParticleSystem.MinMaxCurve(10f, 26f);
            main.startColor = new Color(1f, 1f, 1f, 0.5f);
            main.gravityModifier = -0.01f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 600;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 4f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.55f, 0.1f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Fx_additive.mat"));
            mat.SetTexture("_MainTex", FxDotTexture());
            mat.SetFloat("_Intensity", 0.5f);
            AssetDatabase.DeleteAsset(MatDir + "/City_dust.mat");
            AssetDatabase.CreateAsset(mat, MatDir + "/City_dust.mat");
            r.sharedMaterial = mat;
            ps.Play();
            return ps;
        }

        static Texture2D FxDotTexture()
        {
            string path = GenDir + "/City_dot.asset";
            AssetDatabase.DeleteAsset(path);
            var tex = Seven.FxUtil.Dot(64);
            AssetDatabase.CreateAsset(tex, path);
            return tex;
        }
    }
}
