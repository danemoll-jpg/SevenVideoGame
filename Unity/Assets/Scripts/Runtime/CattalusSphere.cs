using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 18 — the last rune: a smooth pale sphere (Kyle's Art/Reference/kyle_7_sphere.jpg), hand-sized (RuneSizes.SphereR). It
    /// looks almost unmarked until she touches it: then fine engraved lines emerge across it - two bands round its middle and a
    /// crescent arc on one side - lit white (<see cref="LinesMesh"/>, an additive renderer driven by Glowing).
    /// </summary>
    public static class CattalusSphere
    {
        static readonly Dictionary<float, Mesh> _body = new Dictionary<float, Mesh>(), _lines = new Dictionary<float, Mesh>();
        /// <summary>The body: a fine UV sphere of radius r (readable: the rune touch measures its real surface).</summary>
        public static Mesh Mesh(float r)
        {
            if (_body.TryGetValue(r, out var m) && m != null) return m;
            int lon = 48, lat = 24; var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int j = 0; j <= lat; j++) for (int i = 0; i <= lon; i++)
            {
                float th = j / (float)lat * Mathf.PI, ph = i / (float)lon * Mathf.PI * 2f;
                v.Add(new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph)) * r); uv.Add(new Vector2(i / (float)lon, 1f - j / (float)lat));
            }
            for (int j = 0; j < lat; j++) for (int i = 0; i < lon; i++) { int a = j * (lon + 1) + i, b = a + lon + 1; t.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b }); }
            m = new Mesh { name = "CattalusSphere_" + r };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            _body[r] = m; return m;
        }
        /// <summary>The engraved lines (just proud of the surface): two bands round the middle (open on the crescent's side) and the
        /// crescent arc - after Kyle's sphere.</summary>
        public static Mesh LinesMesh(float r)
        {
            if (_lines.TryGetValue(r, out var m) && m != null) return m;
            var v = new List<Vector3>(); var t = new List<int>(); float w = r * 0.028f, rr = r * 1.006f;
            Vector3 S(float latDeg, float azDeg) { float la = latDeg * Mathf.Deg2Rad, az = azDeg * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(la) * Mathf.Cos(az), Mathf.Sin(la), Mathf.Cos(la) * Mathf.Sin(az)) * rr; }
            void Strip(List<Vector3> pts)
            {
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    var a = pts[i]; var b = pts[i + 1]; var n = a.normalized; var side = Vector3.Cross(b - a, n).normalized * w;
                    int k = v.Count; v.Add(a - side); v.Add(a + side); v.Add(b + side); v.Add(b - side);
                    t.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
                }
            }
            foreach (var lat in new[] { 9f, -11f }) { var p = new List<Vector3>(); for (float az = -150f; az <= 140f; az += 5f) p.Add(S(lat, az)); Strip(p); }
            { var p = new List<Vector3>(); for (float la = -62f; la <= 62f; la += 4f) p.Add(S(la, 168f - 14f * Mathf.Cos(la * Mathf.Deg2Rad))); Strip(p); }   // the crescent arc
            m = new Mesh { name = "CattalusSphereLines_" + r }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            _lines[r] = m; return m;
        }
    }
}
