using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using E = Seven.EuropaLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF38 (3): TEMPLE ENTRANCE - the swim path from the open water through the Cetacean temple's entrance to the rune
    /// (<see cref="EuropaLayout.TemplePath"/>) must be clear for the bike: a capsule of the bike's body radius + a margin round the
    /// bike's centre (0.6 m above its position, as RideProbe sweeps it) against the triangles of every mesh in the scene (the bike
    /// under water collides with every visual mesh since QF15). Fails the build on any block (Dan's 1.1.0-rc1 soft-lock: the hall
    /// was a closed dome). Report: Docs/Previews/qf38/temple_entrance_Europa.txt.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        static void TempleEntranceCheck()
        {
            const float R = HoverBike.BodyRadius + 0.5f, Step = 0.5f;
            var path = E.TemplePath; var lift = Vector3.up * 0.6f;
            var samples = new List<Vector3>();
            for (int i = 1; i < path.Length; i++)
            {
                var a = path[i - 1] + lift; var b = path[i] + lift; int n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / Step));
                for (int k = 0; k <= n; k++) samples.Add(Vector3.Lerp(a, b, k / (float)n));
            }
            var pb = new Bounds(samples[0], Vector3.zero); foreach (var s in samples) pb.Encapsulate(s); pb.Expand(R * 2f);
            var sb = new StringBuilder(); int blocked = 0, meshes = 0; float nearest = float.MaxValue; string nearestWhat = "";
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mf == null || mf.sharedMesh == null) continue;
                var go = mf.gameObject; var nm = go.name;
                bool dynamicPiece = nm.Contains("Guide") || nm.Contains("Helmet") || nm.Contains("Bubble") || nm.Contains("Blast") || nm.Contains("Debris") || nm.Contains("Streak") || nm.Contains("Halo");
                if (dynamicPiece) continue;
                if (!go.activeInHierarchy && nm != "EntranceCollapsed") continue;     // the collapse appears after the escape: it must not close the way either
                var rend = go.GetComponent<MeshRenderer>(); if (rend == null) continue;
                if (!rend.bounds.Intersects(pb)) continue;
                meshes++;
                var m = mf.sharedMesh; var v = m.vertices; var t = m.triangles; var M = mf.transform.localToWorldMatrix;
                var w = new Vector3[v.Length]; for (int i = 0; i < v.Length; i++) w[i] = M.MultiplyPoint3x4(v[i]);
                int hits = 0; float best = float.MaxValue; Vector3 at = Vector3.zero;
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    var A = w[t[i]]; var B = w[t[i + 1]]; var C = w[t[i + 2]];
                    var tb = new Bounds(A, Vector3.zero); tb.Encapsulate(B); tb.Encapsulate(C);
                    if (!tb.Intersects(pb)) continue;
                    tb.Expand(R * 2f);
                    foreach (var s in samples)
                    {
                        if (!tb.Contains(s)) continue;
                        float d = (ClosestOnTriangle(s, A, B, C) - s).magnitude;
                        if (d < best) { best = d; at = s; }
                        if (d < R) { hits++; break; }
                    }
                }
                if (best < nearest) { nearest = best; nearestWhat = nm; }
                if (hits > 0) { blocked++; sb.AppendLine($"BLOCK {QF38PathOf(go.transform)}: {hits} triangle(s) within {R:0.0} m of the bike's swim path (nearest {best:0.00} m at {at})"); }
            }
            var head = $"TEMPLE ENTRANCE (QF38 3) Europa: {blocked} mesh(es) block the swim path open water -> the temple's entrance -> the rune ({samples.Count} samples, {meshes} meshes near it, bike capsule {R:0.0} m; nearest clearance {nearest:0.00} m to '{nearestWhat}'; the entrance at {E.TempleDoor}) (must be 0)";
            var dir = System.IO.Path.Combine(RepoRoot, "Docs", "Previews", "qf38"); Directory.CreateDirectory(dir);
            File.WriteAllText(System.IO.Path.Combine(dir, "temple_entrance_Europa.txt"), head + "\n" + sb);
            Log(head);
            if (blocked > 0) BuildFailures.Add(head);
        }
        static string QF38PathOf(Transform t) { var s = t.name; for (var p = t.parent; p != null; p = p.parent) s = p.name + "/" + s; return s; }

        /// <summary>The closest point of triangle abc to p (Ericson, Real-Time Collision Detection 5.1.5).</summary>
        static Vector3 ClosestOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            var ab = b - a; var ac = c - a; var ap = p - a;
            float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap); if (d1 <= 0f && d2 <= 0f) return a;
            var bp = p - b; float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp); if (d3 >= 0f && d4 <= d3) return b;
            float vc = d1 * d4 - d3 * d2; if (vc <= 0f && d1 >= 0f && d3 <= 0f) return a + ab * (d1 / (d1 - d3));
            var cp = p - c; float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp); if (d6 >= 0f && d5 <= d6) return c;
            float vb = d5 * d2 - d1 * d6; if (vb <= 0f && d2 >= 0f && d6 <= 0f) return a + ac * (d2 / (d2 - d6));
            float va = d3 * d6 - d5 * d4; if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f) return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
            float den = 1f / (va + vb + vc); return a + ab * (vb * den) + ac * (vc * den);
        }
    }
}
