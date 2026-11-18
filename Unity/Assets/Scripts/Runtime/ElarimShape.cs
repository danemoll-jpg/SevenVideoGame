using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 10: the vision object as a hand-held piece - a ~15 cm solid pale stone / crystal dodecahedron with thin
    /// glowing edges (Kyle's wireframe look carried by the edges) and the placeholder Elarim marking engraved on one face
    /// (<see cref="MoonLayout.ElarimMarkFile"/>). The meshes are generated here (used by the scene builders); the
    /// component only drives the hover / turn and the edge glow.
    /// Children: "Body" (stone), "Edges" (emissive), "Mark" (the engraved face decal, alpha-clipped texture).
    /// </summary>
    public class ElarimShape : MonoBehaviour
    {
        [Tooltip("Hover + slow turn around the anchor (on the pedestal). Off while carried / on the table.")]
        public bool Hover;
        public Vector3 HoverAnchor;
        public Renderer Edges;
        [Tooltip("QF5: material of the small belt pouch 7 carries it in (CarriedShape).")]
        public Material PouchMaterial;
        [Tooltip("Edge emission (white).")]
        public float Glow = 1.4f;
        float _t, _lastGlow = -1f;
        Material _edgeMat;

        void Update()
        {
            _t += Time.deltaTime;
            if (Hover)
            {
                transform.rotation = Quaternion.Euler(12f * Mathf.Sin(_t * 0.21f), _t * 9f, 8f * Mathf.Sin(_t * 0.17f + 1f));
                transform.position = HoverAnchor + Vector3.up * Mathf.Sin(_t * 0.8f) * 0.012f;
            }
            float g = Glow * (1f + 0.12f * Mathf.Sin(_t * 1.6f));
            if (Edges != null && Mathf.Abs(g - _lastGlow) > 0.01f)
            {
                if (_edgeMat == null) _edgeMat = Edges.material;
                _edgeMat.EnableKeyword("_EMISSION");
                _edgeMat.SetColor("_EmissionColor", Color.white * g);
                _lastGlow = g;
            }
        }

        // ------------------------------------------------------------------------------------- geometry
        /// <summary>The marked face (index into <see cref="Faces"/>).</summary>
        public const int MarkFace = 0;

        /// <summary>Unit dodecahedron faces: outward normal + the 5 vertex indices (counter-clockwise seen from outside).</summary>
        public static List<(Vector3 n, int[] idx)> Faces(List<Vector3> v)
        {
            var res = new List<(Vector3, int[])>();
            float p = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var dirs = new List<Vector3>();
            foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f })
            { dirs.Add(new Vector3(0f, a * p, b)); dirs.Add(new Vector3(a * p, b, 0f)); dirs.Add(new Vector3(a, 0f, b * p)); }
            // the face of the marking first: the one facing +Z (its 'up' is +Y)
            dirs.Sort((x, y) => -Vector3.Dot(x.normalized, Vector3.forward).CompareTo(Vector3.Dot(y.normalized, Vector3.forward)));
            foreach (var d0 in dirs)
            {
                var d = d0.normalized;
                var order = new List<int>();
                for (int i = 0; i < v.Count; i++) order.Add(i);
                order.Sort((x, y) => -Vector3.Dot(v[x], d).CompareTo(Vector3.Dot(v[y], d)));
                float top = Vector3.Dot(v[order[0]], d);
                if (Mathf.Abs(Vector3.Dot(v[order[4]], d) - top) > 1e-3f || Mathf.Abs(Vector3.Dot(v[order[5]], d) - top) < 1e-3f) continue;
                var five = order.GetRange(0, 5);
                var c = Vector3.zero; foreach (var i in five) c += v[i]; c /= 5f;
                var ax = (v[five[0]] - c).normalized; var ay = Vector3.Cross(d, ax);
                five.Sort((x, y) => Mathf.Atan2(Vector3.Dot(v[x] - c, ay), Vector3.Dot(v[x] - c, ax)).CompareTo(Mathf.Atan2(Vector3.Dot(v[y] - c, ay), Vector3.Dot(v[y] - c, ax))));
                res.Add((d, five.ToArray()));
            }
            return res;
        }

        /// <summary>Add a triangle facing along n (Unity's front side: Cross(b - a, c - a) points out).</summary>
        static void Tri(List<Vector3> p, List<int> t, int a, int b, int c, Vector3 n)
        {
            if (Vector3.Dot(Vector3.Cross(p[b] - p[a], p[c] - p[a]), n) >= 0f) { t.Add(a); t.Add(b); t.Add(c); }
            else { t.Add(a); t.Add(c); t.Add(b); }
        }

        static List<Vector3> Verts() => PlatonicShape.Vertices(PlatonicShape.Kind.Dodecahedron);

        /// <summary>Flat-shaded solid body, circumradius r (m).</summary>
        public static Mesh BodyMesh(float r)
        {
            var v = Verts();
            var pos = new List<Vector3>(); var nrm = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            foreach (var (n, idx) in Faces(v))
            {
                int b = pos.Count;
                var c = Vector3.zero; foreach (var i in idx) c += v[i]; c /= 5f;
                var ax = (v[idx[0]] - c).normalized; var ay = Vector3.Cross(n, ax);
                foreach (var i in idx) { pos.Add(v[i] * r); nrm.Add(n); uv.Add(new Vector2(0.5f + Vector3.Dot(v[i] - c, ax), 0.5f + Vector3.Dot(v[i] - c, ay))); }
                for (int k = 1; k < 4; k++) Tri(pos, tri, b, b + k, b + k + 1, n);
            }
            var m = new Mesh { name = "ElarimShape_Body" };
            m.SetVertices(pos); m.SetNormals(nrm); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            m.RecalculateTangents(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Thin edge tubes (hexagonal) + small knots at the corners, slightly outside the body.</summary>
        public static Mesh EdgeMesh(float r, float tube)
        {
            var v = Verts();
            var pos = new List<Vector3>(); var tri = new List<int>();
            foreach (var (a, b) in PlatonicShape.Edges(v))
            {
                var p0 = v[a] * (r + tube * 0.4f); var p1 = v[b] * (r + tube * 0.4f);
                var d = (p1 - p0).normalized;
                var s = Vector3.Cross(d, (p0 + p1).normalized).normalized; var u = Vector3.Cross(s, d);
                int bs = pos.Count;
                for (int k = 0; k < 6; k++)
                {
                    float ang = k * Mathf.PI / 3f;
                    var o = (s * Mathf.Cos(ang) + u * Mathf.Sin(ang)) * tube;
                    pos.Add(p0 + o); pos.Add(p1 + o);
                }
                for (int k = 0; k < 6; k++)
                {
                    int i0 = bs + 2 * k, i1 = bs + 2 * ((k + 1) % 6);
                    var outward = (pos[i0] + pos[i1]) * 0.5f - (p0 + p1) * 0.5f;
                    Tri(pos, tri, i0, i1, i0 + 1, outward); Tri(pos, tri, i1, i1 + 1, i0 + 1, outward);
                }
            }
            var m = new Mesh { name = "ElarimShape_Edges" };
            m.SetVertices(pos); m.SetTriangles(tri, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>The engraving decal: a square on the marked face (UV 0..1, up = toward one corner of the pentagon),
        /// <paramref name="fill"/> = fraction of the face's inscribed circle it spans.</summary>
        public static Mesh MarkMesh(float r, float fill = 0.8f)
        {
            var v = Verts();
            var (n, idx) = Faces(v)[MarkFace];
            var c = Vector3.zero; foreach (var i in idx) c += v[i]; c /= 5f;
            // inscribed circle of the face (unit sphere): distance from centre to an edge midpoint
            float inr = ((v[idx[0]] + v[idx[1]]) * 0.5f - c).magnitude;
            var upv = (v[idx[0]] - c).normalized; var side = Vector3.Cross(upv, n).normalized;
            float h = inr * fill * r;
            var cc = c * r + n * 0.0006f;
            var m = new Mesh { name = "ElarimShape_Mark" };
            m.SetVertices(new List<Vector3> { cc - side * h - upv * h, cc + side * h - upv * h, cc + side * h + upv * h, cc - side * h + upv * h });
            m.SetNormals(new List<Vector3> { n, n, n, n });
            m.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            var t = new List<int>(); var vs = new List<Vector3>(); m.GetVertices(vs);
            Tri(vs, t, 0, 1, 2, n); Tri(vs, t, 0, 2, 3, n);
            m.SetTriangles(t, 0);
            m.RecalculateTangents(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Outward normal of the marked face in the shape's local space.</summary>
        public static Vector3 MarkNormal => Faces(Verts())[MarkFace].n;
        /// <summary>'Up' of the marking in local space (from the face centre toward its first corner).</summary>
        public static Vector3 MarkUp
        {
            get
            {
                var v = Verts(); var (n, idx) = Faces(v)[MarkFace];
                var c = Vector3.zero; foreach (var i in idx) c += v[i]; c /= 5f;
                return (v[idx[0]] - c).normalized;
            }
        }

        /// <summary>World rotation that turns the marked face toward <paramref name="faceDir"/> with the marking's up
        /// along <paramref name="up"/> (projected).</summary>
        public static Quaternion FaceToward(Vector3 faceDir, Vector3 up)
        {
            var local = Quaternion.LookRotation(MarkNormal, MarkUp);
            var world = Quaternion.LookRotation(faceDir, up);
            return world * Quaternion.Inverse(local);
        }
    }
}
