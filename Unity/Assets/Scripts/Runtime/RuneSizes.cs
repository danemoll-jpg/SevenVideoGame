using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF24 (20) - the six runes in one table: their real size (span = twice the vertex radius) and the face distance the touch
    /// code frames with. The build check (FightDemoBuilderQF24Checks.RuneTouchCheck) builds each mesh and measures, along eight
    /// horizontal reach directions at the touch height, where the real surface is: the face constant must lie within those
    /// distances (never deeper - fingertips inside - and never short of every face), and a hand-held rune may not be bigger than
    /// <see cref="MaxSpan"/> (Dan: "about hand-span to forearm-size against her"). In play the touch itself is corrected onto the
    /// mesh surface (VisionTouch.SurfaceGap) and logged.
    /// </summary>
    public static class RuneSizes
    {
        /// <summary>The mothership's octahedron, vertex radius (was 0.45 m: a 0.9 m shape that filled a third of the screen).</summary>
        public const float OctaR = 0.22f;
        /// <summary>Round 18: Cattalus's sphere, radius (20 cm across: hand-sized, it rests between her hands).</summary>
        public const float SphereR = 0.10f;
        public const float MaxSpan = 0.5f;
        public struct Rune { public string name, world; public float size, face; public bool handHeld, touched; public System.Func<Mesh> mesh; }
        public static List<Rune> Table() => new List<Rune>
        {
            new Rune { name = "dodecahedron", world = "the Moon", size = MoonLayout.ShapeRadius * 2f, face = MoonLayout.ShapeRadius * 0.7947f, handHeld = true, touched = true, mesh = () => ElarimShape.BodyMesh(MoonLayout.ShapeRadius) },
            new Rune { name = "cube", world = "Venus", size = 0.42f, face = 0.21f, handHeld = true, touched = true, mesh = () => RecordSet.Cube(0.42f * 0.866f) },
            new Rune { name = "icosahedron", world = "Europa (touched from the bike)", size = 2.0f, face = 0.6f, handHeld = false, mesh = () => Icosahedron.Mesh(1.0f) },
            new Rune { name = "tetrahedron", world = "Mars (a temple piece)", size = 1.8f, face = 0.32f, handHeld = false, touched = true, mesh = () => Tetrahedron.Mesh(0.9f) },
            new Rune { name = "cuboctahedron", world = "the asteroid", size = 0.84f, face = 0.32f, handHeld = false, touched = true, mesh = () => Cuboctahedron.Mesh(0.42f) },
            new Rune { name = "octahedron", world = "the mothership", size = OctaR * 2f, face = OctaR * 0.7071f, handHeld = true, touched = true, mesh = () => Octahedron.Mesh(OctaR) },
            new Rune { name = "sphere", world = "Cattalus", size = SphereR * 2f, face = SphereR, handHeld = true, touched = true, mesh = () => CattalusSphere.Mesh(SphereR) },
        };
        /// <summary>The real surface along 8 horizontal directions at the touch height (a little above the centre): min / max.</summary>
        public static (float min, float max) FaceRange(Mesh m, float lift = 0.02f)
        {
            float lo = float.MaxValue, hi = 0f; var v = m.vertices; var t = m.triangles;
            for (int k = 0; k < 8; k++)
            {
                var dir = Quaternion.Euler(0f, k * 45f, 0f) * Vector3.forward;
                var o = -dir * 5f + Vector3.up * lift; float best = float.MaxValue;
                for (int i = 0; i + 2 < t.Length; i += 3) if (Ray(o, dir, v[t[i]], v[t[i + 1]], v[t[i + 2]], out float d) && d < best) best = d;
                if (best == float.MaxValue) continue;
                float face = 5f - best; lo = Mathf.Min(lo, face); hi = Mathf.Max(hi, face);
            }
            return (lo, hi);
        }
        /// <summary>For the build check: how far the touch's face constant is from the real surface range (0 = within it) and
        /// whether it would put the fingertips INSIDE the mesh (deeper than the nearest face).</summary>
        public static (float gap, bool inside) TouchGap(string name)
        {
            foreach (var r in Table())
            {
                if (r.name != name) continue;
                var m = r.mesh(); if (m == null) return (1f, false);
                var (lo, hi) = FaceRange(m);
                if (r.face < lo - 0.01f) return (lo - r.face, true);
                if (r.face > hi + 0.01f) return (r.face - hi, false);
                return (0f, false);
            }
            return (1f, false);
        }
        static bool Ray(Vector3 o, Vector3 d, Vector3 a, Vector3 b, Vector3 c, out float dist)
        {
            dist = 0f; var e1 = b - a; var e2 = c - a; var h = Vector3.Cross(d, e2); float det = Vector3.Dot(e1, h);
            if (Mathf.Abs(det) < 1e-8f) return false;
            float f = 1f / det; var s = o - a; float u = f * Vector3.Dot(s, h); if (u < 0f || u > 1f) return false;
            var q = Vector3.Cross(s, e1); float w = f * Vector3.Dot(d, q); if (w < 0f || u + w > 1f) return false;
            dist = f * Vector3.Dot(e2, q); return dist > 0f;
        }
    }
}
