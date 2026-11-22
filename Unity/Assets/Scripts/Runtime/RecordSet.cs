using System;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF23 (23) - ONE list of the records (the rune solids) she has collected, read from the saved award flags of every world
    /// in a fixed order. Everything that shows the records uses it (the chair arm aboard, the journey's rings, the logs), so a
    /// newly awarded record appears everywhere the moment it is awarded - never per-scene placement that a later chapter
    /// forgot (the octahedron was missing from the arm in BIA_13B / AST_11B: the arm code stopped at the cuboctahedron).
    /// </summary>
    public static class RecordSet
    {
        public struct Rec { public string Name, World; public Func<bool> Has; public Func<float, Mesh> Mesh; }
        public static readonly Rec[] All =
        {
            new Rec { Name = "dodecahedron", World = "the Moon", Has = () => true, Mesh = r => ElarimShape.BodyMesh(r) },
            new Rec { Name = "cube", World = "Venus", Has = () => VenusState.RecordAwarded, Mesh = r => Cube(r) },
            new Rec { Name = "icosahedron", World = "Europa", Has = () => EuropaState.RecordAwarded, Mesh = r => Icosahedron.Mesh(r) },
            new Rec { Name = "tetrahedron", World = "Mars", Has = () => MarsState.RecordAwarded, Mesh = r => Tetrahedron.Mesh(r) },
            new Rec { Name = "cuboctahedron", World = "the asteroid", Has = () => AsteroidState.RecordAwarded, Mesh = r => Cuboctahedron.Mesh(r) },
            new Rec { Name = "octahedron", World = "the mothership", Has = () => BiavehState.RecordAwarded, Mesh = r => Octahedron.Mesh(r) },
        };
        public static List<Rec> Collected() { var l = new List<Rec>(); foreach (var r in All) { bool h = false; try { h = r.Has(); } catch { } if (h) l.Add(r); } return l; }
        public static string Names() { var n = new List<string>(); foreach (var r in Collected()) n.Add(r.Name); return string.Join(", ", n); }

        static readonly Dictionary<float, Mesh> _cubes = new Dictionary<float, Mesh>();
        public static Mesh Cube(float r)
        {
            if (_cubes.TryGetValue(r, out var m) && m != null) return m;
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); m = UnityEngine.Object.Instantiate(g.GetComponent<MeshFilter>().sharedMesh); UnityEngine.Object.Destroy(g);
            var v = m.vertices; float k = r * 2f / Mathf.Sqrt(3f); for (int i = 0; i < v.Length; i++) v[i] *= k; m.vertices = v; m.RecalculateBounds();
            _cubes[r] = m; return m;
        }
    }
}
