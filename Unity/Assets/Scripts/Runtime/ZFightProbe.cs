using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF18 (11b): the causeway slab shimmered - the same z-fighting as the Moon's door frames (QF9: two surfaces in one
    /// plane), but QF9's check only looked at Unity cube blocks in the Chapter 1 scene. This one looks at every opaque mesh
    /// in a scene (any shape): triangles are bucketed by their plane (normal to 0.002, distance to 2 mm); two triangles of
    /// different renderers with different materials in one bucket, facing the same way, whose outlines overlap, are a
    /// fighting pair. The builder runs it on every scene (build log "COPLANAR CHECK (QF18 11) &lt;scene&gt;: N"), and check runs
    /// run it on objects that move into place at runtime (<see cref="Report"/>).
    /// </summary>
    public static class ZFightProbe
    {
        struct Tri { public int R, M; public Vector2 Min, Max; public Vector3 N; }
        static long Key(Vector3 n, float d)
        {
            long qx = Mathf.RoundToInt(n.x * 500f), qy = Mathf.RoundToInt(n.y * 500f), qz = Mathf.RoundToInt(n.z * 500f), qd = Mathf.RoundToInt(d * 500f);
            return ((qx & 0x7FF) << 53) ^ ((qy & 0x7FF) << 42) ^ ((qz & 0x7FF) << 31) ^ (qd & 0x7FFFFFFF);
        }
        static (Vector3 u, Vector3 v) Basis(Vector3 n) { var u = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized; return (u, Vector3.Cross(n, u)); }

        static bool Usable(Renderer r)
        {
            if (r == null || !(r is MeshRenderer) || !r.enabled || !r.gameObject.activeInHierarchy) return false;
            var m = r.sharedMaterial; if (m == null || m.renderQueue > 2500) return false;          // transparent / additive: never fight
            var mf = r.GetComponent<MeshFilter>(); return mf != null && mf.sharedMesh != null && mf.sharedMesh.isReadable;
        }

        /// <summary>Count fighting triangle pairs between renderer set a and set b (b = null: all of a against each other).</summary>
        public static int Count(IList<Renderer> a, IList<Renderer> b, List<string> pairs, int maxPairs = 200, List<(Renderer x, Renderer y, Vector3 n)> fix = null)
        {
            var buckets = new Dictionary<long, List<Tri>>();
            var all = new List<Renderer>(a); if (b != null) all.AddRange(b);
            var mats = new Dictionary<Material, int>();
            int fromB = b != null ? a.Count : int.MaxValue;
            for (int ri = 0; ri < all.Count; ri++)
            {
                var r = all[ri]; if (!Usable(r)) continue;
                var mesh = r.GetComponent<MeshFilter>().sharedMesh; var tf = r.transform.localToWorldMatrix;
                if (!mats.TryGetValue(r.sharedMaterial, out int mi)) { mi = mats.Count; mats[r.sharedMaterial] = mi; }
                var vs = mesh.vertices; var ts = mesh.triangles;
                for (int i = 0; i + 2 < ts.Length; i += 3)
                {
                    var p0 = tf.MultiplyPoint3x4(vs[ts[i]]); var p1 = tf.MultiplyPoint3x4(vs[ts[i + 1]]); var p2 = tf.MultiplyPoint3x4(vs[ts[i + 2]]);
                    var n = Vector3.Cross(p1 - p0, p2 - p0); float area = n.magnitude; if (area < 1e-4f) continue; n /= area;
                    var (u, v) = Basis(n);
                    var q0 = new Vector2(Vector3.Dot(p0, u), Vector3.Dot(p0, v)); var q1 = new Vector2(Vector3.Dot(p1, u), Vector3.Dot(p1, v)); var q2 = new Vector2(Vector3.Dot(p2, u), Vector3.Dot(p2, v));
                    var t = new Tri { R = ri, M = mi, Min = Vector2.Min(q0, Vector2.Min(q1, q2)), Max = Vector2.Max(q0, Vector2.Max(q1, q2)), N = n };
                    long k = Key(n, Vector3.Dot(n, p0));
                    if (!buckets.TryGetValue(k, out var l)) buckets[k] = l = new List<Tri>();
                    l.Add(t);
                }
            }
            int count = 0; var seen = new HashSet<long>();
            foreach (var kv in buckets)
            {
                var l = kv.Value; if (l.Count < 2) continue;
                for (int i = 0; i < l.Count && i < 400; i++)
                    for (int j = i + 1; j < l.Count && j < 400; j++)
                    {
                        var x = l[i]; var y = l[j];
                        if (x.R == y.R || x.M == y.M) continue;                                      // same renderer / same material: no visible fight
                        if (b != null && (x.R < fromB) == (y.R < fromB)) continue;                    // a vs b only
                        float ou = Mathf.Min(x.Max.x, y.Max.x) - Mathf.Max(x.Min.x, y.Min.x), ov = Mathf.Min(x.Max.y, y.Max.y) - Mathf.Max(x.Min.y, y.Min.y);
                        if (ou <= 0.02f || ov <= 0.02f) continue;
                        long pk = ((long)Mathf.Min(x.R, y.R) << 32) | (uint)Mathf.Max(x.R, y.R);
                        count++;
                        if (seen.Add(pk)) { if (pairs != null && pairs.Count < maxPairs) pairs.Add($"{all[x.R].name} & {all[y.R].name} ({ou:0.00} x {ov:0.00} m)"); fix?.Add((all[x.R], all[y.R], x.N)); }
                    }
            }
            return count;
        }

        /// <summary>Runtime: something moved into place (the lowered drawbridge) vs the level round it.</summary>
        public static void Report(string what, GameObject go)
        {
            if (go == null || DemoBoot.CaptureDir == null) return;
            var a = new List<Renderer>(go.GetComponentsInChildren<Renderer>());
            var bounds = new Bounds(go.transform.position, Vector3.zero); foreach (var r in a) bounds.Encapsulate(r.bounds); bounds.Expand(1f);
            var b = new List<Renderer>();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) if (!a.Contains(r) && r.bounds.Intersects(bounds)) b.Add(r);
            var pairs = new List<string>();
            int n = Count(a, b, pairs);
            var msg = $"Z-FIGHT PROBE (QF18 11) {what}: {n} coplanar overlapping faces with the level round it (must be 0){(pairs.Count > 0 ? " - " + string.Join("; ", pairs) : "")}";
            Debug.Log("[Seven] " + msg); ChapterCapture.I?.Log(msg);
        }
    }
}
