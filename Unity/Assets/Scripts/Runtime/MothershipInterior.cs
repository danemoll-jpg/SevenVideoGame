using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF24 (4) - the inside of the mothership behind the iris, for BIA_02's last stretch (built at runtime round the passage's
    /// exterior saucer, like <see cref="VeyraScene"/>). Dan: after the iris closes they are INSIDE the ship - show that from
    /// outside the saucer: it glides along the interior approach, the guide lights ripple ahead toward the receiving berth, the
    /// vast interior beyond fades into a bright haze (Kyle's look: dark near forms, a glowing misty distance), then it settles
    /// into the berth. Pearly white, monochrome; brighter with distance (the aerial perspective is in the materials, so it
    /// reads the same with or without the scene's fog).
    /// Layout (local: +z = into the ship from the iris, y = the saucer's flight height): a long hall (an inward-facing elliptic
    /// tube, 230 m wide, 150 m high), the floor 46 m below the route with two rows of guide lights, towers / terraces /
    /// spans along both sides fading into the haze, hanging light globes, the receiving berth (a round pad, docking arms set
    /// round it clear of the saucer's bike hold side) at <see cref="BerthZ"/>.
    /// </summary>
    public class MothershipInterior : MonoBehaviour
    {
        public const float BerthZ = 330f, FloorY = -46f, BerthTopY = -10f;
        public Transform Berth;
        readonly List<Renderer> _lights = new List<Renderer>();
        readonly List<float> _lightZ = new List<float>();
        float _t; public float RippleSpeed = 34f;
        static Material _near, _mid, _far, _floor, _dark;

        static Material M(ref Material cache, string n, float g, float e, float smooth = 0.6f)
        {
            if (cache != null) return cache;
            var sh = Shader.Find("Seven/DissolveLit") ?? Shader.Find("Universal Render Pipeline/Lit");
            cache = new Material(sh) { name = n };
            if (cache.HasProperty("_BaseColor")) cache.SetColor("_BaseColor", new Color(g, g, g));
            if (cache.HasProperty("_Smoothness")) cache.SetFloat("_Smoothness", smooth);
            if (cache.HasProperty("_EmissionColor")) { cache.EnableKeyword("_EMISSION"); cache.SetColor("_EmissionColor", new Color(e, e, e)); }
            if (cache.HasProperty("_Dissolvable")) cache.SetFloat("_Dissolvable", 0f);
            return cache;
        }

        public static MothershipInterior Build(Transform parent, Vector3 irisAt)
        {
            var root = new GameObject("MothershipInterior");
            root.transform.SetParent(parent, false); root.transform.position = irisAt; root.transform.rotation = Quaternion.identity;
            var mi = root.AddComponent<MothershipInterior>();
            mi.Make();
            root.SetActive(false);
            return mi;
        }

        GameObject Prim(PrimitiveType t, string n, Vector3 local, Vector3 size, Material m, Quaternion? rot = null)
        {
            var g = GameObject.CreatePrimitive(t); g.name = n; Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(transform, false); g.transform.localPosition = local; g.transform.localScale = size;
            if (rot.HasValue) g.transform.localRotation = rot.Value;
            var r = g.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }

        void Make()
        {
            var near = M(ref _near, "MsIn_near", 0.70f, 0.16f);
            var mid = M(ref _mid, "MsIn_mid", 0.86f, 0.42f);
            var far = M(ref _far, "MsIn_far", 0.96f, 0.85f, 0.3f);
            var floor = M(ref _floor, "MsIn_floor", 0.55f, 0.10f, 0.8f);
            var dark = M(ref _dark, "MsIn_dark", 0.22f, 0.02f, 0.5f);
            // the hall: an inward-facing elliptic tube from the iris wall into the ship, its far end lost in a bright haze
            var hall = new GameObject("MsIn_Hall"); hall.transform.SetParent(transform, false);
            hall.AddComponent<MeshFilter>().sharedMesh = Tube(115f, 75f, -6f, 900f, 64, 24, 12f);
            var hr = hall.AddComponent<MeshRenderer>(); hr.sharedMaterial = mid; hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // the inside face of the hull round the iris (the annulus seen from inside)
            var back = new GameObject("MsIn_IrisWall"); back.transform.SetParent(transform, false); back.transform.localRotation = Quaternion.LookRotation(Vector3.forward); back.transform.localPosition = new Vector3(0f, 0f, 0.4f);   // clear of the outer hull plane (no z-fighting)
            back.AddComponent<MeshFilter>().sharedMesh = IrisAperture.Annulus(8.2f, 130f, 96);
            back.AddComponent<MeshRenderer>().sharedMaterial = near;
            // the far end: a glowing wall of haze (the bright misty distance)
            Prim(PrimitiveType.Quad, "MsIn_HazeEnd", new Vector3(0f, 0f, 880f), new Vector3(260f, 170f, 1f), M(ref _far, "MsIn_far", 0.96f, 0.85f, 0.3f));
            // the floor and its two rows of guide lights along the route
            Prim(PrimitiveType.Cube, "MsIn_Floor", new Vector3(0f, FloorY - 1f, 450f), new Vector3(200f, 2f, 900f), floor);
            Prim(PrimitiveType.Cube, "MsIn_RouteBand", new Vector3(0f, FloorY + 0.05f, BerthZ * 0.5f), new Vector3(14f, 0.3f, BerthZ), dark);
            for (float z = 14f; z <= BerthZ; z += 9f)
                foreach (var s in new[] { -8f, 8f })
                {
                    var g = Prim(PrimitiveType.Sphere, "MsIn_GuideLight", new Vector3(s, FloorY + 0.6f, z), new Vector3(1.3f, 0.5f, 1.3f), Markers.Additive(1f));
                    _lights.Add(g.GetComponent<Renderer>()); _lightZ.Add(z);
                }
            // towers, terraces and spans along both sides - near ones darker (silhouettes), the far ones lost in the glow
            var rnd = new System.Random(24);
            float R() => (float)rnd.NextDouble();
            for (int i = 0; i < 46; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float z = 40f + i * 17f + R() * 10f;
                float x = side * (40f + R() * 55f);
                float h = 35f + R() * 95f, w = 7f + R() * 12f;
                var m = z < 220f ? near : z < 520f ? mid : far;
                Prim(PrimitiveType.Capsule, "MsIn_Tower", new Vector3(x, FloorY + h * 0.5f, z), new Vector3(w, h * 0.5f, w), m);
                // terraces: flat discs round the tower at intervals, planted rims (dark)
                for (float y = FloorY + 14f; y < FloorY + h - 6f; y += 16f + R() * 8f)
                {
                    Prim(PrimitiveType.Cylinder, "MsIn_Terrace", new Vector3(x, y, z), new Vector3(w * 2.2f, 0.7f, w * 2.2f), m);
                    Prim(PrimitiveType.Cylinder, "MsIn_TerraceGreen", new Vector3(x, y + 0.8f, z), new Vector3(w * 2.0f, 0.5f, w * 2.0f), z < 220f ? dark : near);
                }
                if (i % 5 == 2) Prim(PrimitiveType.Cube, "MsIn_Span", new Vector3(x * 0.5f, FloorY + h * 0.6f, z), new Vector3(Mathf.Abs(x), 2.2f, 5f), m);
            }
            // hanging light globes (soft sources inside the haze)
            for (int i = 0; i < 18; i++)
            {
                float z = 30f + i * 26f; float x = (i % 3 - 1) * 46f + (R() - 0.5f) * 20f; float y = 30f + R() * 25f;
                Prim(PrimitiveType.Cylinder, "MsIn_GlobeCord", new Vector3(x, y + 20f, z), new Vector3(0.25f, 20f, 0.25f), near);
                Prim(PrimitiveType.Sphere, "MsIn_Globe", new Vector3(x, y, z), Vector3.one * (3f + R() * 2f), Markers.Additive(1.3f));
            }
            // the receiving berth: a round pad on a tapered column, a soft ring light, docking arms round it - never on the
            // saucer's bike-hold side (+x here: the saucer arrives heading +z, its hold faces its left, kept clear - QF24 5)
            var berth = new GameObject("MsIn_Berth").transform; berth.SetParent(transform, false); berth.localPosition = new Vector3(0f, BerthTopY, BerthZ); Berth = berth;
            Prim(PrimitiveType.Cylinder, "MsIn_BerthPad", berth.localPosition + Vector3.down * 0.6f, new Vector3(22f, 0.6f, 22f), near);
            Prim(PrimitiveType.Cylinder, "MsIn_BerthColumn", berth.localPosition + Vector3.down * 18f, new Vector3(8f, 17f, 8f), near);
            var ring = Prim(PrimitiveType.Cylinder, "MsIn_BerthRing", berth.localPosition + Vector3.up * 0.05f, new Vector3(18f, 0.05f, 18f), Markers.Additive(0.6f));
            foreach (var a in new[] { 150f, 210f, 270f, 330f })
            {
                var q = Quaternion.Euler(0f, a, 0f); var d = q * Vector3.forward;
                Prim(PrimitiveType.Cube, "MsIn_DockArm", berth.localPosition + d * 12.5f + Vector3.up * 2.5f, new Vector3(1.2f, 5f, 1.2f), mid, q);
                Prim(PrimitiveType.Cube, "MsIn_DockArmTip", berth.localPosition + d * 11.2f + Vector3.up * 4.8f, new Vector3(1.0f, 0.8f, 3.2f), mid, q);
            }
        }

        /// <summary>An inward-facing elliptic tube along +z (rx, ry radii, centre height cy), length len.</summary>
        static Mesh Tube(float rx, float ry, float cy, float len, int segs, int rings, float zStart)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int k = 0; k <= rings; k++)
            {
                float z = zStart + len * k / rings;
                for (int i = 0; i <= segs; i++) { float a = i / (float)segs * Mathf.PI * 2f; v.Add(new Vector3(Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry, z)); }
            }
            for (int k = 0; k < rings; k++)
                for (int i = 0; i < segs; i++)
                {
                    int a = k * (segs + 1) + i, b = a + segs + 1;
                    t.Add(a); t.Add(a + 1); t.Add(b); t.Add(b); t.Add(a + 1); t.Add(b + 1);   // inward
                }
            var m = new Mesh { name = "MsIn_Hall" }; m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            // make sure the normals face the axis (inward)
            var n = m.normals; if (n.Length > 0 && Vector3.Dot(n[0], new Vector3(-v[0].x, cy - v[0].y, 0f)) < 0f) { for (int i = 0; i < t.Count; i += 3) { int x = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = x; } m.SetTriangles(t, 0); m.RecalculateNormals(); }
            return m;
        }

        /// <summary>The guide lights ripple forward along the route (a travelling crest), toward the berth.</summary>
        void Update()
        {
            _t += Time.deltaTime;
            float crest = (_t * RippleSpeed) % (BerthZ + 60f);
            for (int i = 0; i < _lights.Count; i++)
            {
                if (_lights[i] == null) continue;
                float d = Mathf.Abs(_lightZ[i] - crest);
                float k = 0.55f + 1.6f * Mathf.Exp(-d * d / 160f);
                _lights[i].transform.localScale = new Vector3(1.3f, 0.5f, 1.3f) * (0.85f + 0.35f * Mathf.Exp(-d * d / 160f));
                var lm = _lights[i].sharedMaterial; if (lm.HasProperty("_Intensity")) lm.SetFloat("_Intensity", k); else if (lm.HasProperty("_BaseColor")) lm.SetColor("_BaseColor", Color.white * k);
            }
        }
        public int LightCount => _lights.Count;
        public static int Shown;
        void OnEnable() { if (_lights.Count > 0) Shown++; }
        /// <summary>World position of the route at distance z from the iris, at the saucer's height.</summary>
        public Vector3 Route(float z) => transform.TransformPoint(new Vector3(0f, 0f, z));
    }
}
