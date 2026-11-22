using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF23 (6) - nothing may pass through the parked / hovering saucer. Dan saw a cross of plain wall slabs cutting through
    /// the saucer at the mothership's berth (a runtime bug: doors and partitions moved to the world origin). The same test runs
    /// at build time on every scene (FightDemoBuilderQF23Checks: the build fails) and in every check run while the saucer is
    /// placed (<c>SAUCER CLEARANCE (QF23 6)</c>): every triangle of every other visible mesh is tested against the saucer
    /// hull's box (in the saucer's own space: the disc + dome, not the beam cone, the hold's bay room or the bike).
    /// </summary>
    public static class SaucerClearance
    {
        /// <summary>The hull's local box (the disc rim to the dome top; the belly).</summary>
        public static Bounds HullBox(Transform saucer)
        {
            bool any = false; var b = new Bounds();
            foreach (var mf in saucer.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || Skip(mf.transform, saucer)) continue;
                var r = mf.GetComponent<Renderer>(); if (r == null) continue;
                var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = saucer.InverseTransformPoint(mf.transform.TransformPoint(c));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            // a few cm of skin: a thing that merely touches the rim is still a hit
            b.Expand(0.04f);
            return b;
        }
        /// <summary>Parts of the saucer that are not the hull (the beam cone, the light, the bike hold's room behind the panel).</summary>
        static bool Skip(Transform t, Transform saucer)
        {
            for (var x = t; x != null && x != saucer; x = x.parent)
            {
                var n = x.name;
                if (n.Contains("Beam") || n.Contains("Bay") || n.Contains("Glow") || n.Contains("Light") || n.Contains("Probe") || n.Contains("Cone")) return true;
            }
            return false;
        }

        /// <summary>Every renderer (outside the saucer and the ignore roots) with a triangle inside the saucer's hull box.</summary>
        public static List<string> Intersecting(Transform saucer, IEnumerable<Transform> ignore = null, bool activeOnly = true)
        {
            var hits = new List<string>(); if (saucer == null) return hits;
            var box = HullBox(saucer);
            var wb = new Bounds(saucer.TransformPoint(box.center), Vector3.zero);
            for (int i = 0; i < 8; i++) wb.Encapsulate(saucer.TransformPoint(box.center + Vector3.Scale(box.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            var ign = new HashSet<Transform>(); if (ignore != null) foreach (var t in ignore) if (t != null) ign.Add(t);
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(activeOnly ? FindObjectsInactive.Exclude : FindObjectsInactive.Include))
            {
                if (activeOnly && !mr.enabled) continue;
                if (mr.transform.IsChildOf(saucer)) continue;
                bool skip = false; foreach (var t in ign) if (mr.transform.IsChildOf(t)) { skip = true; break; }
                if (skip) continue;
                if (!mr.bounds.Intersects(wb)) continue;
                var mf = mr.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                var m = mf.sharedMesh; if (!m.isReadable) { hits.Add(mr.name + " (not readable - bounds overlap)"); continue; }
                var v = m.vertices; var tri = m.triangles;
                var l2w = mr.transform.localToWorldMatrix; var w2s = saucer.worldToLocalMatrix;
                var loc = new Vector3[v.Length]; for (int i = 0; i < v.Length; i++) loc[i] = w2s.MultiplyPoint3x4(l2w.MultiplyPoint3x4(v[i]));
                for (int i = 0; i + 2 < tri.Length; i += 3)
                    if (TriBox(loc[tri[i]], loc[tri[i + 1]], loc[tri[i + 2]], box.center, box.extents) && TriInHull(loc[tri[i]], loc[tri[i + 1]], loc[tri[i + 2]], saucer)) { hits.Add(Path(mr.transform)); break; }
            }
            return hits;
        }
        static string Path(Transform t) { var p = t.name; for (int k = 0; k < 3 && t.parent != null; k++) { t = t.parent; p = t.name + "/" + p; } return p; }

        // the hull's real shape: a disc / dome - per height band, the largest radius of its vertices (saucer space)
        static readonly System.Collections.Generic.Dictionary<Transform, (float y0, float dy, float[] r)> _prof = new System.Collections.Generic.Dictionary<Transform, (float, float, float[])>();
        static (float y0, float dy, float[] r) Profile(Transform saucer)
        {
            if (_prof.TryGetValue(saucer, out var p) && p.r != null) return p;
            var pts = new List<Vector3>();
            foreach (var mf in saucer.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || Skip(mf.transform, saucer) || mf.GetComponent<Renderer>() == null || !mf.sharedMesh.isReadable) continue;
                var m = mf.transform.localToWorldMatrix; var w2s = saucer.worldToLocalMatrix;
                foreach (var v in mf.sharedMesh.vertices) pts.Add(w2s.MultiplyPoint3x4(m.MultiplyPoint3x4(v)));
            }
            float y0 = float.MaxValue, y1 = float.MinValue; foreach (var q in pts) { y0 = Mathf.Min(y0, q.y); y1 = Mathf.Max(y1, q.y); }
            int n = 24; float dy = Mathf.Max(0.01f, (y1 - y0) / n); var r = new float[n];
            foreach (var q in pts) { int k = Mathf.Clamp((int)((q.y - y0) / dy), 0, n - 1); r[k] = Mathf.Max(r[k], new Vector2(q.x, q.z).magnitude); }
            for (int k = 0; k < n; k++) r[k] += 0.04f;        // a few cm of skin
            p = (y0 - 0.04f, dy, r); _prof[saucer] = p; return p;
        }
        static bool InHull(Vector3 q, (float y0, float dy, float[] r) p)
        {
            int k = Mathf.FloorToInt((q.y - p.y0) / p.dy); if (k < 0 || k >= p.r.Length) return false;
            return new Vector2(q.x, q.z).magnitude < p.r[k];
        }
        /// <summary>Is any part of the triangle inside the hull's real outline (dense barycentric sampling)?</summary>
        static bool TriInHull(Vector3 a, Vector3 b, Vector3 c, Transform saucer)
        {
            var p = Profile(saucer);
            float span = Mathf.Max((b - a).magnitude, Mathf.Max((c - b).magnitude, (a - c).magnitude));
            int n = Mathf.Clamp(Mathf.CeilToInt(span / 0.1f), 2, 120);
            for (int i = 0; i <= n; i++) for (int j = 0; j <= n - i; j++) { float u = i / (float)n, v = j / (float)n; if (InHull(a + (b - a) * u + (c - a) * v, p)) return true; }
            return false;
        }

        /// <summary>Triangle vs axis-aligned box (separating axis test).</summary>
        public static bool TriBox(Vector3 a, Vector3 b, Vector3 c, Vector3 bc, Vector3 h)
        {
            a -= bc; b -= bc; c -= bc;
            Vector3 e0 = b - a, e1 = c - b, e2 = a - c;
            if (Mathf.Max(a.x, b.x, c.x) < -h.x || Mathf.Min(a.x, b.x, c.x) > h.x) return false;
            if (Mathf.Max(a.y, b.y, c.y) < -h.y || Mathf.Min(a.y, b.y, c.y) > h.y) return false;
            if (Mathf.Max(a.z, b.z, c.z) < -h.z || Mathf.Min(a.z, b.z, c.z) > h.z) return false;
            var n = Vector3.Cross(e0, e1);
            float r = h.x * Mathf.Abs(n.x) + h.y * Mathf.Abs(n.y) + h.z * Mathf.Abs(n.z);
            if (Mathf.Abs(Vector3.Dot(n, a)) > r) return false;
            foreach (var e in new[] { e0, e1, e2 })
                foreach (var ax in new[] { new Vector3(0f, -e.z, e.y), new Vector3(e.z, 0f, -e.x), new Vector3(-e.y, e.x, 0f) })
                {
                    if (ax.sqrMagnitude < 1e-10f) continue;
                    float p0 = Vector3.Dot(a, ax), p1 = Vector3.Dot(b, ax), p2 = Vector3.Dot(c, ax);
                    float rr = h.x * Mathf.Abs(ax.x) + h.y * Mathf.Abs(ax.y) + h.z * Mathf.Abs(ax.z);
                    if (Mathf.Min(p0, p1, p2) > rr || Mathf.Max(p0, p1, p2) < -rr) return false;
                }
            return true;
        }

        /// <summary>Check runs: log the clearance of the saucer(s) in the scene now (the line must say 0).</summary>
        public static int Report(string where, IEnumerable<Transform> ignore = null)
        {
            int n = 0;
            foreach (var sb in Object.FindObjectsByType<SaucerBeam>(FindObjectsInactive.Exclude))
            {
                var hits = Intersecting(sb.transform, ignore);
                n += hits.Count;
                var line = $"SAUCER CLEARANCE (QF23 6) {where}: {hits.Count} things inside the saucer's hull (must be 0){(hits.Count > 0 ? " - " + string.Join(", ", hits.GetRange(0, Mathf.Min(8, hits.Count))) : "")}";
                Debug.Log("[Seven] " + line);
                if (DemoBoot.CaptureDir != null) ChapterCapture.I?.Log(line);
            }
            return n;
        }
    }
}
