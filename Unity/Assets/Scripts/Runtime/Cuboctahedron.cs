using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>Round 16: the asteroid's rune - a cuboctahedron ("vector equilibrium": 8 triangles and 6 squares around one
    /// centre, 12 vertices all at the same distance from it), generated in code like the others. Resting on a square face.</summary>
    public static class Cuboctahedron
    {
        public static readonly Vector3[] V =
        {
            new Vector3(1f, 1f, 0f), new Vector3(1f, -1f, 0f), new Vector3(-1f, 1f, 0f), new Vector3(-1f, -1f, 0f),
            new Vector3(1f, 0f, 1f), new Vector3(1f, 0f, -1f), new Vector3(-1f, 0f, 1f), new Vector3(-1f, 0f, -1f),
            new Vector3(0f, 1f, 1f), new Vector3(0f, 1f, -1f), new Vector3(0f, -1f, 1f), new Vector3(0f, -1f, -1f),
        };
        /// <summary>The 24 edges (vertex pairs at distance sqrt 2 on the unit-cube layout).</summary>
        public static readonly List<(int, int)> Edges = MakeEdges();
        static List<(int, int)> MakeEdges()
        {
            var e = new List<(int, int)>();
            for (int i = 0; i < V.Length; i++) for (int j = i + 1; j < V.Length; j++) if (Mathf.Abs((V[i] - V[j]).sqrMagnitude - 2f) < 1e-3f) e.Add((i, j));
            return e;
        }
        // resting on a square face: the square (0,4,1,5) - x = +1 - turned to face down
        static readonly Quaternion Rest = Quaternion.FromToRotation(Vector3.right, Vector3.down);
        public static Vector3 Vertex(int i, float radius) => Rest * V[i].normalized * radius;

        /// <summary>A flat-shaded cuboctahedron of the given circumradius (the faces found from the vertex set: every plane
        /// through three or four vertices with all others on one side).</summary>
        public static Mesh Mesh(float radius)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            var pts = new Vector3[V.Length]; for (int i = 0; i < V.Length; i++) pts[i] = Vertex(i, radius);
            var faces = new List<int[]>();
            // the 6 squares (axis faces) and the 8 triangles (octant faces), from the unit layout
            for (int ax = 0; ax < 3; ax++)
                foreach (var s in new[] { 1f, -1f })
                {
                    var f = new List<int>(); for (int i = 0; i < V.Length; i++) if (Mathf.Abs(V[i][ax] - s) < 1e-3f) f.Add(i);
                    faces.Add(f.ToArray());
                }
            foreach (var sx in new[] { 1f, -1f }) foreach (var sy in new[] { 1f, -1f }) foreach (var sz in new[] { 1f, -1f })
            {
                var f = new List<int>();
                for (int i = 0; i < V.Length; i++) { var p = V[i]; if (Mathf.Abs(p.x * sx + p.y * sy + p.z * sz - 2f) < 1e-3f) f.Add(i); }
                faces.Add(f.ToArray());
            }
            foreach (var f in faces)
            {
                // order the face's vertices round its centre, outward winding
                var c = Vector3.zero; foreach (var i in f) c += pts[i]; c /= f.Length;
                var n = c.normalized; var a0 = (pts[f[0]] - c).normalized; var a1 = Vector3.Cross(n, a0);
                var order = new List<int>(f); order.Sort((x, y) => Mathf.Atan2(Vector3.Dot(pts[x] - c, a1), Vector3.Dot(pts[x] - c, a0)).CompareTo(Mathf.Atan2(Vector3.Dot(pts[y] - c, a1), Vector3.Dot(pts[y] - c, a0))));
                int b = v.Count; foreach (var i in order) v.Add(pts[i]);
                for (int k = 1; k + 1 < order.Count; k++) { t.Add(b); t.Add(b + k + 1); t.Add(b + k); }
            }
            var m = new Mesh { name = "Cuboctahedron" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            if (Vector3.Dot(m.normals[0], v[0]) < 0f) { for (int i = 0; i < t.Count; i += 3) { int x = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = x; } m.SetTriangles(t, 0); m.RecalculateNormals(); }
            return m;
        }
    }
}
