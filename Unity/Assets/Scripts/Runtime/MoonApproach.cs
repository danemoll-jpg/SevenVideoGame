using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF30 (1): what is outside when the hull vanishes in FIN_01 - the Moon, large ahead of the window (Chapter 1's opening composition
    /// reversed: they are coming back to it), its lit face and bright limb, the lunar entrance they will fly into (a dark opening ringed
    /// with light on its near face), Earth beyond it, the stars round them (SaucerOutside's open space). Built at runtime, monochrome:
    /// the shading is baked into the textures (unlit), so it reads the same whatever the cabin's lights do.
    /// </summary>
    public class MoonApproach : MonoBehaviour
    {
        public static MoonApproach Current { get; private set; }
        public Transform Moon, Entrance, Earth;
        void OnDestroy() { if (Current == this) Current = null; }

        /// <summary>The view from <paramref name="eye"/> along <paramref name="dir"/>: the Moon's centre at <paramref name="dist"/>, radius <paramref name="r"/>.</summary>
        public static MoonApproach Build(Vector3 eye, Vector3 dir, float dist = 1250f, float r = 420f)
        {
            if (Current != null) Destroy(Current.gameObject);
            var root = new GameObject("MoonApproach"); var m = root.AddComponent<MoonApproach>(); Current = m;
            dir = dir.normalized;
            var centre = eye + dir * dist + Vector3.down * r * 0.18f;
            var moonT = new Texture2D(512, 256, TextureFormat.RGBA32, true) { name = "MoonFace", wrapMode = TextureWrapMode.Clamp };
            var L = new Vector3(-0.55f, 0.42f, 0.72f).normalized;              // the sun: upper left, from behind the viewer - a lit face, the terminator on the right
            Bake(moonT, L, MoonAlbedo, 0.08f);
            m.Moon = Sphere("ApproachMoon", root.transform, centre, r, Quaternion.LookRotation(-dir), moonT);
            // the lunar entrance: a dark opening ringed with light on the near face, a little left of and below the centre
            var nLocal = new Vector3(-0.22f, -0.18f, 0.96f).normalized; var nWorld = m.Moon.rotation * nLocal;
            var at = centre + nWorld * (r + 0.5f);
            var ent = new GameObject("ApproachEntrance").transform; ent.SetParent(root.transform, false); ent.position = at; ent.rotation = Quaternion.LookRotation(nWorld);
            var disc = new GameObject("EntranceDark"); disc.transform.SetParent(ent, false); disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            disc.AddComponent<MeshFilter>().sharedMesh = Markers.DiscMesh(r * 0.085f, 32); var dm = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "EntranceDark" }; dm.SetColor("_BaseColor", new Color(0.03f, 0.03f, 0.03f)); if (dm.HasProperty("_Cull")) dm.SetFloat("_Cull", 0f); disc.AddComponent<MeshRenderer>().sharedMaterial = dm;
            var ring = new GameObject("EntranceRingGlow"); ring.transform.SetParent(ent, false); ring.transform.localPosition = Vector3.forward * 0.4f; ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.AddComponent<MeshFilter>().sharedMesh = Markers.RingMesh(r * 0.085f, r * 0.11f, 0f); ring.AddComponent<MeshRenderer>().sharedMaterial = Markers.Additive(1.4f);
            m.Entrance = ent;
            // Earth beyond, smaller, upper right
            var earthT = new Texture2D(256, 128, TextureFormat.RGBA32, true) { name = "EarthFace", wrapMode = TextureWrapMode.Clamp };
            Bake(earthT, L, EarthAlbedo, 0.04f);
            var side = Vector3.Cross(Vector3.up, dir).normalized;
            m.Earth = Sphere("ApproachEarth", root.transform, eye + dir * (dist * 2.4f) + side * dist * 0.95f + Vector3.up * dist * 0.55f, r * 0.42f, Quaternion.LookRotation(-dir), earthT);
            Debug.Log($"[Seven] FIN_01 (QF30 1): the Moon ahead of the window ({dist:0} m, radius {r:0} m: {Mathf.Atan(r / dist) * Mathf.Rad2Deg * 2f:0} deg across), the lunar entrance ringed with light on its near face, Earth beyond");
            return m;
        }
        static Transform Sphere(string name, Transform parent, Vector3 at, float r, Quaternion rot, Texture2D tex)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(at, rot); go.transform.localScale = Vector3.one * r;
            go.AddComponent<MeshFilter>().sharedMesh = UVSphere(48, 24);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name + "_mat" }; mat.SetTexture("_BaseMap", tex); mat.SetColor("_BaseColor", Color.white);
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }
        /// <summary>A UV sphere (radius 1) whose texture coordinates are (longitude, latitude) - so Bake knows each texel's normal.</summary>
        static Mesh UVSphere(int lon, int lat)
        {
            var v = new Vector3[(lon + 1) * (lat + 1)]; var uv = new Vector2[v.Length]; var t = new int[lon * lat * 6]; int k = 0;
            for (int j = 0; j <= lat; j++) for (int i = 0; i <= lon; i++)
            {
                float u = i / (float)lon, w = j / (float)lat; v[j * (lon + 1) + i] = N(u, w); uv[j * (lon + 1) + i] = new Vector2(u, w);
            }
            for (int j = 0; j < lat; j++) for (int i = 0; i < lon; i++)
            {
                int a = j * (lon + 1) + i, b = a + 1, c = a + lon + 1, d = c + 1;
                t[k++] = a; t[k++] = c; t[k++] = b; t[k++] = b; t[k++] = c; t[k++] = d;
            }
            var m = new Mesh { name = "UVSphere", vertices = v, uv = uv, triangles = t }; m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        static Vector3 N(float u, float w) { float lo = u * Mathf.PI * 2f, la = (w - 0.5f) * Mathf.PI; return new Vector3(Mathf.Cos(la) * Mathf.Sin(lo), Mathf.Sin(la), Mathf.Cos(la) * Mathf.Cos(lo)); }
        static void Bake(Texture2D tex, Vector3 L, System.Func<Vector3, float> albedo, float ambient)
        {
            int W = tex.width, H = tex.height; var px = new Color[W * H];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                var n = N((x + 0.5f) / W, (y + 0.5f) / H);
                float lam = Mathf.Clamp01(Vector3.Dot(n, L)); float limb = Mathf.Pow(1f - Mathf.Clamp01(n.z), 3f) * 0.35f * Mathf.Clamp01(lam * 3f);
                float v = Mathf.Clamp01(albedo(n) * (ambient + lam * 0.95f) + limb);
                px[y * W + x] = new Color(v, v, v, 1f);
            }
            tex.SetPixels(px); tex.Apply(true);
        }
        static float MoonAlbedo(Vector3 n)
        {
            float a = 0.72f;
            a -= 0.22f * Mathf.Clamp01((Mathf.PerlinNoise(n.x * 2.1f + 7f, n.y * 2.1f + n.z * 1.3f + 3f) - 0.45f) * 3f);        // the darker maria
            // craters: rings round random centres
            var rnd = new System.Random(19);
            for (int i = 0; i < 60; i++)
            {
                var c = new Vector3((float)rnd.NextDouble() * 2f - 1f, (float)rnd.NextDouble() * 2f - 1f, (float)rnd.NextDouble() * 2f - 1f).normalized; float rr = 0.03f + (float)rnd.NextDouble() * 0.12f;
                float d = Vector3.Angle(n, c) * Mathf.Deg2Rad;
                if (d < rr * 1.25f) a += d < rr * 0.85f ? -0.07f : d < rr ? 0.09f : 0.03f;
            }
            return Mathf.Clamp(a, 0.25f, 0.9f);
        }
        static float EarthAlbedo(Vector3 n)
        {
            float land = Mathf.PerlinNoise(n.x * 2.6f + 11f, n.y * 2.6f + n.z * 1.7f) > 0.52f ? 0.48f : 0.3f;
            float cloud = Mathf.Clamp01((Mathf.PerlinNoise(n.x * 5f + 3f, n.y * 5f + n.z * 4f + 1f) - 0.55f) * 3f);
            return Mathf.Lerp(land, 0.95f, cloud * 0.8f);
        }
    }
}
