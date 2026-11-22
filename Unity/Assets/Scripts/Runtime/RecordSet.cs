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
    /// QF24 (2) - the repeat: Dan's real save (read from his PlayerPrefs) HAS all five - the asteroid's record included - so
    /// the list was not short; the QF23 fix put the copies in a 42 cm row along the pilot chair's armrest, and in the chair he
    /// saw them the row ran back into the chair's shell / her thigh, one hidden and the rest a crooked stack at her knees. And
    /// the list read each chapter's CURRENT slot (a Scene Select slot while one was in use). Now: the player's OWN saves
    /// (+ a completed chapter counts as its record), and they rest on <see cref="RecordDisplay"/> (a side table with a
    /// display ring beside the seat), each in its own place.
    /// </summary>
    public static class RecordSet
    {
        public struct Rec { public string Name, World; public Func<bool> Has; public Func<float, Mesh> Mesh; }
        public static readonly Rec[] All =
        {
            new Rec { Name = "dodecahedron", World = "the Moon", Has = () => true, Mesh = r => ElarimShape.BodyMesh(r) },
            new Rec { Name = "cube", World = "Venus", Has = () => VenusState.OwnRecordAwarded || VenusState.OwnSceneDone("VEN_C12"), Mesh = r => Cube(r) },
            new Rec { Name = "icosahedron", World = "Europa", Has = () => EuropaState.OwnRecordAwarded || EuropaState.OwnComplete, Mesh = r => Icosahedron.Mesh(r) },
            new Rec { Name = "tetrahedron", World = "Mars", Has = () => MarsState.OwnRecordAwarded || MarsState.OwnComplete, Mesh = r => Tetrahedron.Mesh(r) },
            new Rec { Name = "cuboctahedron", World = "the asteroid", Has = () => AsteroidState.OwnRecordAwarded || AsteroidState.OwnComplete, Mesh = r => Cuboctahedron.Mesh(r) },
            new Rec { Name = "octahedron", World = "the mothership", Has = () => BiavehState.OwnRecordAwarded || BiavehState.OwnComplete, Mesh = r => Octahedron.Mesh(r) },
        };
        public static List<Rec> Collected() { var l = new List<Rec>(); foreach (var r in All) { bool h = false; try { h = r.Has(); } catch { } if (h) l.Add(r); } return l; }
        public static string Names() { var n = new List<string>(); foreach (var r in Collected()) n.Add(r.Name); return string.Join(", ", n); }

        static readonly Dictionary<float, Mesh> _cubes = new Dictionary<float, Mesh>();
        public static Mesh Cube(float r)
        {
            if (_cubes.TryGetValue(r, out var m) && m != null) return m;
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); m = UnityEngine.Object.Instantiate(g.GetComponent<MeshFilter>().sharedMesh); if (Application.isPlaying) UnityEngine.Object.Destroy(g); else UnityEngine.Object.DestroyImmediate(g);
            var v = m.vertices; float k = r * 2f / Mathf.Sqrt(3f); for (int i = 0; i < v.Length; i++) v[i] *= k; m.vertices = v; m.RecalculateBounds();
            _cubes[r] = m; return m;
        }
    }
}
