using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF24 (2) - where her records rest aboard: a small, elegant side table beside the seat she is in (the egg chair has no
    /// armrest), a slim tulip stem with a round top and a thin glowing display ring floating just above it; each collected
    /// record (<see cref="RecordSet"/>, the player's own saves) sits in its own place on the ring - evenly round the circle,
    /// level, each over a small pad of light, turning slowly. Never stacked, never at her knees. Built at runtime (the same
    /// table whichever seat she is in), cleared and rebuilt on every placement so it always equals what she has collected.
    /// </summary>
    public class RecordDisplay : MonoBehaviour
    {
        public const float TopY = 0.66f, RingR = 0.17f, RecR = 0.036f;
        readonly List<Transform> _recs = new List<Transform>();
        public int Count => _recs.Count;
        public IReadOnlyList<Transform> Records => _recs;
        public Vector3 Top => transform.position + Vector3.up * TopY;

        static Material _table, _glow;
        static Material Table()
        {
            if (_table != null) return _table;
            var sh = Shader.Find("Seven/DissolveLit") ?? Shader.Find("Universal Render Pipeline/Lit");
            _table = new Material(sh) { name = "RecordTable" };
            if (_table.HasProperty("_BaseColor")) _table.SetColor("_BaseColor", new Color(0.82f, 0.82f, 0.82f));
            if (_table.HasProperty("_Smoothness")) _table.SetFloat("_Smoothness", 0.85f);
            if (_table.HasProperty("_Metallic")) _table.SetFloat("_Metallic", 0.6f);
            if (_table.HasProperty("_Dissolvable")) _table.SetFloat("_Dissolvable", 0f);
            return _table;
        }

        public static int Built;
        public static RecordDisplay Build(Vector3 floorAt, Vector3 facing, Material recordMat)
        {
            Built++;
            var go = new GameObject("RecordDisplay");
            go.transform.SetPositionAndRotation(floorAt, Quaternion.LookRotation(facing.sqrMagnitude > 1e-4f ? facing : Vector3.forward));
            var d = go.AddComponent<RecordDisplay>();
            d.MakeTable();
            d.Fill(recordMat);
            return d;
        }

        GameObject Part(string n, Mesh mesh, Material m, Vector3 local)
        {
            var g = new GameObject(n); g.transform.SetParent(transform, false); g.transform.localPosition = local;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }

        void MakeTable()
        {
            // a tulip stem: a flared foot, a slim waist, a cup under the round top
            Part("RecordTable_Stem", Lathe(new[] { new Vector2(0.17f, 0f), new Vector2(0.16f, 0.015f), new Vector2(0.05f, 0.07f), new Vector2(0.028f, 0.3f),
                new Vector2(0.035f, 0.5f), new Vector2(0.12f, 0.6f), new Vector2(0.235f, 0.625f), new Vector2(0.24f, 0.64f), new Vector2(0.23f, 0.655f), new Vector2(0f, 0.655f) }, 40), Table(), Vector3.zero);
            // the display ring: a thin band of light floating a little above the top
            if (_glow == null) _glow = Markers.Additive(0.9f);
            Part("RecordTable_Ring", Markers.RingMesh(RingR - 0.006f, RingR + 0.006f, 0f), _glow, Vector3.up * (TopY + 0.004f));
        }

        void Fill(Material mat)
        {
            var recs = RecordSet.Collected(); int n = recs.Count;
            for (int i = 0; i < n; i++)
            {
                float a = (90f + i * 360f / Mathf.Max(1, n)) * Mathf.Deg2Rad;
                var local = new Vector3(Mathf.Cos(a) * RingR, TopY + RecR + 0.012f, Mathf.Sin(a) * RingR);
                var pad = Part("RecordPad_" + recs[i].Name, Markers.RingMesh(0f, 0.03f, 0f), _glow, new Vector3(local.x, TopY + 0.005f, local.z));
                var g = Part(char.ToUpper(recs[i].Name[0]) + recs[i].Name.Substring(1) + "RecordCopy", recs[i].Name == "cube" ? RecordSet.Cube(RecR) : recs[i].Mesh(RecR), mat, local);
                if (mat == null) g.GetComponent<MeshRenderer>().sharedMaterial = Table();
                g.transform.localRotation = Quaternion.Euler(0f, i * 37f, 0f);
                _recs.Add(g.transform);
            }
        }

        void Update() { foreach (var r in _recs) if (r != null) r.Rotate(Vector3.up, 9f * Time.deltaTime, Space.World); }

        /// <summary>Each record in its own place: the smallest distance between two of them, and the lowest one above the top.</summary>
        public string Audit()
        {
            float minGap = 99f, minLift = 99f;
            for (int i = 0; i < _recs.Count; i++)
            {
                minLift = Mathf.Min(minLift, _recs[i].position.y - Top.y);
                for (int j = i + 1; j < _recs.Count; j++) minGap = Mathf.Min(minGap, Vector3.Distance(_recs[i].position, _recs[j].position));
            }
            return $"{_recs.Count} records ({RecordSet.Names()}) on the display ring, each in its place: closest two {minGap * 100f:0.0} cm apart (each {RecR * 200f:0} cm wide), lowest {minLift * 100f:0.0} cm above the table top";
        }

        static Mesh Lathe(Vector2[] prof, int seg)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int s = 0; s <= seg; s++)
            {
                float a = s / (float)seg * Mathf.PI * 2f;
                foreach (var p in prof) v.Add(new Vector3(Mathf.Cos(a) * p.x, p.y, Mathf.Sin(a) * p.x));
            }
            int k = prof.Length;
            for (int s = 0; s < seg; s++)
                for (int i = 0; i < k - 1; i++)
                {
                    int a = s * k + i, b = (s + 1) * k + i;
                    t.Add(a); t.Add(a + 1); t.Add(b); t.Add(b); t.Add(a + 1); t.Add(b + 1);
                }
            var m = new Mesh { name = "RecordTable" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            // outward normals
            var nn = m.normals; if (nn.Length > 2 && Vector3.Dot(nn[1], new Vector3(v[1].x, 0f, v[1].z)) < 0f) { for (int i = 0; i < t.Count; i += 3) { int x = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = x; } m.SetTriangles(t, 0); m.RecalculateNormals(); }
            return m;
        }
    }
}
