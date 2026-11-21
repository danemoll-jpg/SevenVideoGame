using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>Round 17 — the mothership's rune: an octahedron (two square pyramids joined base to base, eight triangular
    /// faces, twelve edges), generated in code like the other solids. Flat-shaded so each face reads from the establishing shot.</summary>
    public static class Octahedron
    {
        public static readonly Vector3[] V = { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
        static readonly int[] F = { 0, 4, 2, 0, 2, 5, 0, 5, 3, 0, 3, 4, 1, 2, 4, 1, 5, 2, 1, 3, 5, 1, 4, 3 };
        public static readonly List<(int, int)> Edges = new List<(int, int)> { (0, 2), (0, 3), (0, 4), (0, 5), (1, 2), (1, 3), (1, 4), (1, 5), (2, 4), (4, 3), (3, 5), (5, 2) };
        /// <summary>A little taller than wide (reads as an octahedron, not a diamond square, from a low angle).</summary>
        public static Vector3 Vertex(int i, float radius) => Vector3.Scale(V[i], new Vector3(1f, 1.12f, 1f)) * radius;
        public static Mesh Mesh(float radius)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < F.Length; i += 3) { int b = v.Count; v.Add(Vertex(F[i], radius)); v.Add(Vertex(F[i + 1], radius)); v.Add(Vertex(F[i + 2], radius)); t.Add(b); t.Add(b + 1); t.Add(b + 2); }
            var m = new Mesh { name = "Octahedron" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
    }
}
