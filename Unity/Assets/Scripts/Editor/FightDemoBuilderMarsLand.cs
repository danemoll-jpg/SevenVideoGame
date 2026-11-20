using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using M = Seven.MarsLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF18 (21) - "make Mars look like Mars": round the play area, in Kyle's misty monochrome, a real Martian landscape -
    /// a sea of rippled, wind-shaped sand dunes rolling away into the haze, rocky mesas and buttes, scattered boulders, a
    /// pale small sun through the haze, wind-blown dust (MarsDust, at runtime) - and the "Face on Mars" as an easter egg: a
    /// natural, eroded mesa (Kyle's style, never a sculpted head) that reads as a face only from above (the arrival's extreme
    /// wide, the saucer's window at Mars); from the ground it is a far-off mesa.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        static float Dune(float x, float z)
        {
            // long wind-shaped ridges (asymmetric: a gentle windward slope, a steep slip face) + small ripples
            float a = x * 0.0031f + z * 0.0012f, b = x * -0.0011f + z * 0.0027f;
            float ridge = Mathf.Pow(Mathf.Repeat(a + 0.35f * Mathf.Sin(b * 2.1f), 1f), 1.8f);
            float swell = Mathf.Sin(b * 3.3f + Mathf.Sin(a * 1.7f)) * 0.5f + 0.5f;
            float rip = Mathf.Sin(x * 0.11f + z * 0.04f + Mathf.Sin(z * 0.013f) * 3f) * 0.25f;
            return ridge * 22f * (0.55f + 0.45f * swell) + rip;
        }

        static void BuildMarsLandscape(Transform world, Material sand, Material rock, Material rockDark, System.Random rnd)
        {
            var land = new GameObject("MarsLandscape").transform; land.SetParent(world, false);
            // ---- the dune sea round the level's plain (the plain blocks cover x -900..900, z -700..700 at y 0)
            {
                var v = new List<Vector3>(); var t = new List<int>();
                int n = 150; float half = 3200f, st = half * 2f / n;
                var idx = new int[n + 1, n + 1];
                for (int i = 0; i <= n; i++)
                    for (int j = 0; j <= n; j++)
                    {
                        float x = -half + i * st, z = -half + j * st;
                        // distance outside the plain's rectangle: dunes rise from 0 at its edge
                        float dx = Mathf.Max(0f, Mathf.Abs(x) - 880f), dz = Mathf.Max(0f, Mathf.Abs(z) - 680f);
                        float out_ = Mathf.Sqrt(dx * dx + dz * dz);
                        float k = Mathf.SmoothStep(0f, 1f, out_ / 260f);
                        float y = out_ <= 0f ? -3f : Dune(x, z) * k - 0.5f * (1f - k);
                        idx[i, j] = v.Count; v.Add(new Vector3(x, y, z));
                    }
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                    {
                        float cx = -half + (i + 0.5f) * st, cz = -half + (j + 0.5f) * st;
                        if (Mathf.Abs(cx) < 880f - st && Mathf.Abs(cz) < 680f - st) continue;      // under the plain: no faces
                        t.AddRange(new[] { idx[i, j], idx[i, j + 1], idx[i + 1, j + 1], idx[i, j], idx[i + 1, j + 1], idx[i + 1, j] });
                    }
                MeshObject("DuneSea", SaveMesh(SmoothFinish(v, t), "Mars_DuneSea"), sand, land, false);
            }
            // ---- low dune fields on the open plain itself, away from roads and the play areas
            {
                var v = new List<Vector3>(); var t = new List<int>();
                foreach (var (cx, cz, r) in new[] { (-420f, -380f, 160f), (-360f, 420f, 150f), (360f, -420f, 170f), (420f, 380f, 140f), (-700f, 80f, 120f), (700f, -60f, 130f) })
                {
                    int n = 40; var idx = new int[n + 1, n + 1];
                    for (int i = 0; i <= n; i++) for (int j = 0; j <= n; j++)
                    {
                        float x = cx - r + i * 2f * r / n, z = cz - r + j * 2f * r / n;
                        float d = Mathf.Sqrt((x - cx) * (x - cx) + (z - cz) * (z - cz)) / r;
                        float y = Dune(x, z) * 0.35f * Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d) - 0.4f;
                        idx[i, j] = v.Count; v.Add(new Vector3(x, y, z));
                    }
                    for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) t.AddRange(new[] { idx[i, j], idx[i, j + 1], idx[i + 1, j + 1], idx[i, j], idx[i + 1, j + 1], idx[i + 1, j] });
                }
                MeshObject("PlainDunes", SaveMesh(SmoothFinish(v, t), "Mars_PlainDunes"), sand, land, false);
            }
            // ---- mesas and buttes far off (flat-topped, eroded sides, talus at the foot), in the haze
            {
                var v = new List<Vector3>(); var t = new List<int>();
                for (int k = 0; k < 16; k++)
                {
                    float ang = k * 22.5f + (float)rnd.NextDouble() * 12f, dist = 1100f + (float)rnd.NextDouble() * 1500f;
                    var c = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad) * dist, -2f, Mathf.Sin(ang * Mathf.Deg2Rad) * dist);
                    float h = 60f + (float)rnd.NextDouble() * 150f, r = (k % 3 == 0 ? 50f : 120f) + (float)rnd.NextDouble() * 160f;    // buttes (narrow) and mesas (broad)
                    var prof = new[] { new Vector2(r * 1.5f, 0f), new Vector2(r * 1.18f, h * 0.18f), new Vector2(r * 1.02f, h * 0.35f), new Vector2(r * 0.96f, h * 0.88f), new Vector2(r * 0.9f, h), new Vector2(0f, h) };
                    var m = Lathe(prof, 0f, 360f, 18, null, c);
                    var mv = m.vertices; var seed = k * 7.3f;
                    for (int i = 0; i < mv.Length; i++) { var q = mv[i] - c; float a = Mathf.Atan2(q.z, q.x); float rr = new Vector2(q.x, q.z).magnitude; float wob = 1f + 0.16f * Mathf.Sin(a * 3f + seed) + 0.08f * Mathf.Sin(a * 7f + seed * 2f); mv[i] = c + new Vector3(Mathf.Cos(a) * rr * wob, q.y, Mathf.Sin(a) * rr * wob); }
                    m.vertices = mv; Add(v, t, m);
                }
                MeshObject("Mesas", SaveMesh(Finish(v, t), "Mars_Mesas"), rockDark, land, false);
            }
            // ---- scattered boulders on the plain (outside the play zones and roads)
            {
                var v = new List<Vector3>(); var t = new List<int>();
                int placed = 0;
                for (int i = 0; i < 400 && placed < 140; i++)
                {
                    var p = new Vector3((float)rnd.NextDouble() * 1700f - 850f, 0f, (float)rnd.NextDouble() * 1300f - 650f);
                    bool inZone = false; foreach (var z in M.Zones) if (p.x > z.X0 - 6f && p.x < z.X1 + 6f && p.z > z.Z0 - 6f && p.z < z.Z1 + 6f) { inZone = true; break; }
                    if (inZone || Mathf.Abs(p.x) < 62f) continue;
                    float s = 0.6f + (float)rnd.NextDouble() * (rnd.NextDouble() < 0.15 ? 4.5f : 1.6f);
                    Lump(v, t, p + Vector3.up * s * 0.45f, new Vector3(s, s * 0.7f, s * 0.9f), 0.45f, rnd.Next(1000), 0f);
                    placed++;
                }
                MeshObject("Boulders", SaveMesh(SmoothFinish(v, t), "Mars_Boulders"), rock, land, false);
            }
            // ---- the Face on Mars: a natural eroded mesa - an oval plateau with a brow ridge, two eye hollows, a nose ridge and
            // a mouth groove, softened by erosion noise; it only reads as a face from above
            {
                var c = M.FaceAt; float W = 150f, L = 190f, H = 26f;
                int n = 72; var v = new List<Vector3>(); var t = new List<int>(); var idx = new int[n + 1, n + 1];
                for (int i = 0; i <= n; i++) for (int j = 0; j <= n; j++)
                {
                    float u = i / (float)n * 2f - 1f, w = j / (float)n * 2f - 1f;                 // u across (x), w along (z: +w = the brow)
                    float x = u * W * 1.4f, z = w * L * 1.4f;
                    float r = Mathf.Sqrt((u * 1.4f) * (u * 1.4f) + (w * 1.4f) * (w * 1.4f));
                    float plateau = Mathf.Clamp01((1.08f - r) * 5f);                                     // the oval mesa
                    float hgt = plateau * H;
                    float g(float cu, float cw, float su, float sw) { float du = (u * 1.4f - cu) / su, dw = (w * 1.4f - cw) / sw; return Mathf.Exp(-(du * du + dw * dw)); }
                    hgt += 5f * g(0f, 0.55f, 0.75f, 0.12f) * plateau;                                // the brow
                    hgt -= 7f * (g(-0.33f, 0.32f, 0.17f, 0.11f) + g(0.33f, 0.32f, 0.17f, 0.11f)) * plateau;   // the eyes
                    hgt += 6f * g(0f, 0.02f, 0.09f, 0.30f) * plateau;                                // the nose
                    hgt -= 5f * g(0f, -0.42f, 0.38f, 0.07f) * plateau;                               // the mouth
                    hgt += (Mathf.PerlinNoise(x * 0.04f + 13f, z * 0.04f + 7f) - 0.5f) * 5f * plateau + (Mathf.PerlinNoise(x * 0.012f, z * 0.012f) - 0.5f) * 8f * (1f - plateau * 0.5f);
                    idx[i, j] = v.Count; v.Add(c + new Vector3(x, hgt - 1.2f, z));
                }
                for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) t.AddRange(new[] { idx[i, j], idx[i, j + 1], idx[i + 1, j + 1], idx[i, j], idx[i + 1, j + 1], idx[i + 1, j] });
                MeshObject("CydoniaMesa", SaveMesh(SmoothFinish(v, t), "Mars_CydoniaMesa"), rock, land, false);
            }
            // ---- a pale small sun through the haze (a white disc and a soft halo, far off and high)
            {
                var sunAt = new Vector3(-1400f, 900f, 2600f);
                var disc = MeshObject("PaleSun", SaveMesh(Lathe(new[] { new Vector2(0f, -1f), new Vector2(1f, 0f), new Vector2(0f, 1f) }, 0f, 360f, 20, null, Vector3.zero), "Mars_PaleSun"), AdditiveMat("Mars_pale_sun", 2.2f), land, false);
                disc.transform.position = sunAt; disc.transform.localScale = Vector3.one * 55f;
            }
            // ---- wind-blown dust (drifting across the ground and in the air round her, at runtime)
            var dust = new GameObject("MarsDust"); dust.transform.SetParent(world, false);
            var md = dust.AddComponent<Seven.MarsDust>(); md.Mat = AdditiveMatTex("Mars_dust", 0.22f);
            Log("Mars landscape (QF18 21): dune sea, plain dunes, 16 mesas / buttes, boulders, the Cydonia mesa at " + M.FaceAt + ", a pale sun, drifting dust");
        }
    }
}
