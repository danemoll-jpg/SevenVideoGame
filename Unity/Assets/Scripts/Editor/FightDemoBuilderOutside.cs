using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF16 (1 + 2): what is outside the saucer, in the Saucer scene.
    /// (1) One cheap backdrop per world under / around the ship, seen through the window band and the galley porthole,
    /// in Kyle's look (dark near silhouettes, the distance swallowed by bright haze; the Atmosphere does the work):
    /// the Moon (grey cratered plain, Earth low in the black sky), Venus (garden hills, dark tree clumps, a bright cloud
    /// ceiling), Europa (ice field with ridges, Jupiter huge in the sky), Mars (the plain, the walls, the ravine and the two
    /// fortress cities), open space (stars) while travelling. <see cref="Seven.SaucerOutside"/> switches them by
    /// <see cref="Seven.SaucerLocation"/>. Every ground sits at the Round 3 city's height (y -200): the ship hovers high.
    /// (2) <see cref="Seven.TravelStage"/>: the one saucer prefab, unchanged, far above the deck (its holder inactive)
    /// for the passage's outside shot, and the passage's two materials.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const float OutY = -200f;

        static Texture2D OutTex(string name, int w, int h, Func<float, float, float> f)
        {
            var path = GenDir + "/Outside_" + name + ".asset";
            var tex = new Texture2D(w, h, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat, name = "Outside_" + name };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(f(x / (float)w, y / (float)h) * 255f), 0, 255);
                    px[y * w + x] = new Color32(b, b, b, 255);
                }
            tex.SetPixels32(px); tex.Apply(true);
            var old = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (old != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(tex, path);
            return tex;
        }

        /// <summary>A height-field ground, a square grid centred on the ship.</summary>
        static Mesh OutGround(Func<float, float, float> h, float half = 2600f, int n = 104)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            float step = half * 2f / n;
            for (int j = 0; j <= n; j++)
                for (int i = 0; i <= n; i++)
                {
                    float x = -half + i * step, z = -half + j * step;
                    v.Add(new Vector3(x, OutY + h(x, z), z));
                }
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    int a = j * (n + 1) + i, b = a + 1, c = a + n + 1, d = c + 1;
                    t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d);
                }
            var m = new Mesh { indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        static GameObject OutSphere(string name, Transform parent, Vector3 at, float radius, Material mat, Vector3 euler = default)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); s.name = name;
            UnityEngine.Object.DestroyImmediate(s.GetComponent<Collider>());
            s.transform.SetParent(parent, false); s.transform.position = at; s.transform.localScale = Vector3.one * radius * 2f;
            s.transform.rotation = Quaternion.Euler(euler);
            var r = s.GetComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off;
            return s;
        }
        static GameObject OutBox(string name, Transform parent, Vector3 at, Vector3 size, Material mat, float yaw = 0f)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Cube); s.name = name;
            UnityEngine.Object.DestroyImmediate(s.GetComponent<Collider>());
            s.transform.SetParent(parent, false); s.transform.position = at; s.transform.localScale = size;
            s.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var r = s.GetComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off;
            return s;
        }
        static Material OutMat(string name, float gray, float emit, Texture2D tex = null, bool twoSided = false)
        {
            var m = Lit("Outside_" + name, Gray(gray), 0.05f);
            if (tex != null) { m.SetTexture("_BaseMap", tex); m.SetTexture("_MainTex", tex); }
            if (emit > 0f) { Emissive(m, emit); if (tex != null) m.SetTexture("_EmissionMap", tex); }
            if (twoSided) { m.SetFloat("_Cull", 0f); m.doubleSidedGI = true; }
            EditorUtility.SetDirty(m);
            return m;
        }
        static float Hash(float x, float y) { float h = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f; return h - Mathf.Floor(h); }
        static float Noise(float x, float y)
        {
            float ix = Mathf.Floor(x), iy = Mathf.Floor(y), fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            return Mathf.Lerp(Mathf.Lerp(Hash(ix, iy), Hash(ix + 1, iy), fx), Mathf.Lerp(Hash(ix, iy + 1), Hash(ix + 1, iy + 1), fx), fy);
        }
        static float Fbm(float x, float y) { float s = 0f, a = 0.5f; for (int i = 0; i < 4; i++) { s += a * Noise(x, y); x *= 2.03f; y *= 2.03f; a *= 0.5f; } return s; }

        static void BuildOutside(GameObject atmo, Seven.Chapter1Aboard ch)
        {
            var root = new GameObject("Outside").transform;
            var so = root.gameObject.AddComponent<Seven.SaucerOutside>();
            so.Atmo = atmo != null ? atmo.GetComponent<Seven.Atmosphere>() : null;
            var cityGo = GameObject.Find("World/City");
            so.City = cityGo;
            var rnd = new System.Random(1616);
            float R() => (float)rnd.NextDouble();

            // ---- the Moon: a grey cratered plain, Earth low in the black sky
            {
                var g = new GameObject("Outside_Moon").transform; g.SetParent(root, false);
                var craters = new List<(float x, float z, float r)>();
                for (int i = 0; i < 46; i++) craters.Add(((R() - 0.5f) * 4600f, (R() - 0.5f) * 4600f, 40f + 160f * R() * R()));
                float H(float x, float z)
                {
                    float h = (Fbm(x * 0.002f, z * 0.002f) - 0.5f) * 60f;
                    foreach (var c in craters)
                    {
                        float d = Mathf.Sqrt((x - c.x) * (x - c.x) + (z - c.z) * (z - c.z)) / c.r;
                        if (d < 1.6f) h += d < 1f ? -c.r * 0.22f * (1f - d * d) : c.r * 0.08f * Mathf.Exp(-Mathf.Pow((d - 1.05f) * 4f, 2f));
                    }
                    return h;
                }
                var tex = OutTex("moon_ground", 256, 256, (u, v) => 0.5f + (Fbm(u * 24f, v * 24f) - 0.5f) * 0.5f);
                tex.wrapMode = TextureWrapMode.Repeat;
                var mat = OutMat("moon_ground", 0.42f, 0f, tex);
                mat.SetTextureScale("_BaseMap", new Vector2(40f, 40f));
                Obj("Outside_MoonGround", OutGround(H), mat, g, false);
                var etex = OutTex("earth", 512, 256, (u, v) => { float c = Fbm(u * 10f, v * 6f); float land = c > 0.55f ? 0.45f : 0.22f; float cloud = Mathf.Clamp01((Fbm(u * 14f + 3f, v * 9f) - 0.5f) * 3f); return Mathf.Lerp(land, 0.95f, cloud); });
                OutSphere("Earth", g, new Vector3(-260f, 70f, 1500f), 85f, OutMat("earth", 0.7f, 1.1f, etex), new Vector3(15f, 30f, 0f));
                so.Moon = g.gameObject;
            }
            // ---- Venus: rolling garden hills, dark tree clumps, a bright cloud ceiling
            {
                var g = new GameObject("Outside_Venus").transform; g.SetParent(root, false);
                float H(float x, float z) => (Fbm(x * 0.0012f + 5f, z * 0.0012f) - 0.45f) * 260f + Mathf.Sin(x * 0.004f) * 30f;
                Obj("Outside_VenusHills", OutGround(H), OutMat("venus_hills", 0.30f, 0f), g, false);
                var trees = new List<Vector3>(); var tt = new List<int>();
                var tm = OutMat("venus_trees", 0.08f, 0f);
                for (int i = 0; i < 160; i++)
                {
                    float a = (20f + 140f * R()) * Mathf.Deg2Rad, d = 420f + 1400f * R();
                    float x = Mathf.Cos(a) * d, z = Mathf.Sin(a) * d, s = 14f + 30f * R();
                    OutSphere("Tree", g, new Vector3(x, OutY + H(x, z) + s * 0.6f, z), s, tm).transform.localScale = new Vector3(s * 2f, s * 2.6f, s * 2f);
                }
                var cloudTex = OutTex("venus_cloud", 256, 256, (u, v) => 0.78f + (Fbm(u * 8f, v * 8f) - 0.5f) * 0.35f);
                var cm = OutMat("venus_cloud", 0.85f, 0.9f, cloudTex, true);
                cm.SetTextureScale("_BaseMap", new Vector2(6f, 6f));
                var cloud = Obj("Outside_VenusCloudCeiling", Lathe(new[] { new Vector2(0f, 95f), new Vector2(3200f, 95f) }, 0f, 360f, 48), cm, g, false);
                so.Venus = g.gameObject;
            }
            // ---- Europa: the ice field with ridges and cracks, Jupiter huge in the sky
            {
                var g = new GameObject("Outside_Europa").transform; g.SetParent(root, false);
                float H(float x, float z)
                {
                    float r1 = Mathf.Abs(Mathf.Sin(x * 0.003f + z * 0.0011f)), r2 = Mathf.Abs(Mathf.Sin(x * -0.0014f + z * 0.0034f));
                    return (Fbm(x * 0.003f, z * 0.003f) - 0.5f) * 18f + Mathf.Exp(-r1 * 30f) * 22f + Mathf.Exp(-r2 * 30f) * 16f;
                }
                var tex = OutTex("europa_ice", 256, 256, (u, v) => { float crack = Mathf.Abs(Mathf.Sin(u * 40f + Fbm(u * 8f, v * 8f) * 6f)); return 0.78f - (crack < 0.06f ? 0.35f : 0f) + (Fbm(u * 30f, v * 30f) - 0.5f) * 0.15f; });
                var mat = OutMat("europa_ice", 0.75f, 0.12f, tex);
                mat.SetTextureScale("_BaseMap", new Vector2(30f, 30f));
                Obj("Outside_EuropaIce", OutGround(H), mat, g, false);
                var jtex = OutTex("jupiter", 512, 256, (u, v) => { float b = Mathf.Sin(v * 38f + Fbm(u * 6f, v * 4f) * 2.5f) * 0.5f + 0.5f; float spot = Mathf.Exp(-(Mathf.Pow((u - 0.3f) * 9f, 2f) + Mathf.Pow((v - 0.36f) * 18f, 2f))); return 0.48f + b * 0.32f - spot * 0.22f; });
                OutSphere("Jupiter", g, new Vector3(380f, 240f, 1900f), 360f, OutMat("jupiter", 0.75f, 1.0f, jtex), new Vector3(0f, 0f, -6f));
                so.Europa = g.gameObject;
            }
            // ---- Mars: the plain, the walls, the ravine and the two fortress cities
            {
                var g = new GameObject("Outside_Mars").transform; g.SetParent(root, false);
                float H(float x, float z)
                {
                    float rav = Mathf.Abs(x - 60f + Mathf.Sin(z * 0.002f) * 120f);            // the ravine runs away from the window
                    float h = (Fbm(x * 0.0018f, z * 0.0018f) - 0.5f) * 50f;
                    if (rav < 90f) h -= (1f - rav / 90f) * 160f;
                    float wall = Mathf.Abs(Mathf.Sqrt(x * x + (z - 900f) * (z - 900f)) - 1700f);  // the far walls (an arc of cliffs)
                    if (wall < 260f) h += Mathf.SmoothStep(0f, 1f, 1f - wall / 260f) * 230f;
                    return h;
                }
                var tex = OutTex("mars_ground", 256, 256, (u, v) => 0.45f + (Fbm(u * 20f, v * 20f) - 0.5f) * 0.4f);
                var mat = OutMat("mars_ground", 0.40f, 0f, tex);
                mat.SetTextureScale("_BaseMap", new Vector2(30f, 30f));
                Obj("Outside_MarsPlain", OutGround(H), mat, g, false);
                var fm = OutMat("mars_city", 0.10f, 0f);
                foreach (var cx in new[] { -280f, 420f })
                {
                    float cz = 1050f + R() * 200f;
                    for (int i = 0; i < 26; i++)
                    {
                        float x = cx + (R() - 0.5f) * 220f, z = cz + (R() - 0.5f) * 180f, w = 18f + 30f * R(), ht = 30f + 110f * R() * R();
                        OutBox("Fortress", g, new Vector3(x, OutY + H(x, z) + ht * 0.5f, z), new Vector3(w, ht, w * (0.6f + R())), fm, R() * 90f);
                    }
                    OutBox("FortressWall", g, new Vector3(cx, OutY + H(cx, cz) + 18f, cz - 110f), new Vector3(260f, 36f, 10f), fm);
                }
                so.Mars = g.gameObject;
            }
            // ---- open space while travelling: stars all round, far away
            {
                var g = new GameObject("Outside_Space").transform; g.SetParent(root, false);
                var v = new List<Vector3>(); var t = new List<int>();
                for (int i = 0; i < 900; i++)
                {
                    var d = UnityEngine.Random.onUnitSphere; float r = 1400f, sz = 1.2f + 3.2f * R() * R();
                    var c = d * r; var side = Vector3.Cross(d, Mathf.Abs(d.y) > 0.9f ? Vector3.right : Vector3.up).normalized; var up = Vector3.Cross(side, d);
                    Quad(v, t, c - side * sz - up * sz, c - side * sz + up * sz, c + side * sz + up * sz, c + side * sz - up * sz);
                }
                var sm = Lit("Outside_stars", Gray(1f), 0f); Emissive(sm, 2.2f); sm.SetFloat("_Cull", 0f); EditorUtility.SetDirty(sm);
                Obj("Outside_Stars", Finish(v, t), sm, g, false);
                so.Space = g.gameObject;
            }
            // ---- the galley porthole's discs (the Moon disc exists since Round 10)
            if (ch != null && ch.Moon != null)
            {
                so.DiscMoon = ch.Moon.gameObject;
                Transform Disc(string name, Texture2D tex, float gray)
                {
                    var d = UnityEngine.Object.Instantiate(ch.Moon.gameObject, ch.Moon.parent);
                    d.name = name;
                    var m = Lit("Galley_" + name, Gray(gray), 0.05f); m.SetTexture("_BaseMap", tex); m.SetTexture("_MainTex", tex);
                    Emissive(m, 1.3f); m.SetTexture("_EmissionMap", tex);
                    d.GetComponent<MeshRenderer>().sharedMaterial = m;
                    d.SetActive(false);
                    return d.transform;
                }
                so.DiscVenus = Disc("VenusDisc", OutTex("disc_venus", 256, 128, (u, v) => 0.8f + (Fbm(u * 6f, v * 10f) - 0.5f) * 0.3f), 0.85f).gameObject;
                so.DiscEuropa = Disc("EuropaDisc", OutTex("disc_europa", 256, 128, (u, v) => { float c = Mathf.Abs(Mathf.Sin(u * 30f + Fbm(u * 5f, v * 5f) * 5f)); return 0.82f - (c < 0.08f ? 0.3f : 0f); }), 0.85f).gameObject;
                so.DiscMars = Disc("MarsDisc", OutTex("disc_mars", 256, 128, (u, v) => 0.5f + (Fbm(u * 7f, v * 7f) - 0.5f) * 0.5f), 0.6f).gameObject;
            }
            foreach (var gg in new[] { so.Venus, so.Europa, so.Mars, so.Space }) if (gg != null) gg.SetActive(false);
            Log("QF16 (1): outside the saucer - backdrops Moon / Venus / Europa / Mars / space + porthole discs; city " + (cityGo != null ? "kept for the demo + scene 9" : "NOT FOUND"));

            // ---- (2) the passage's stage: the one saucer, far above the deck, its holder inactive
            var stage = new GameObject("TravelStage").AddComponent<Seven.TravelStage>();
            var holder = new GameObject("TravelStageSaucer").transform; holder.SetParent(stage.transform, false);
            var ts = PlaceSaucer(holder, new Vector3(0f, 3000f, -2600f));
            CheckSaucer("Saucer (the passage's outside shot)", ts);
            holder.gameObject.SetActive(false);
            stage.Saucer = ts;
            var glow = Lit("Passage_shape", Gray(0.82f), 0.3f); Emissive(glow, 0.9f);
            stage.Glow = glow;
            stage.Halo = AdditiveMatTex("Passage_halo", 0.22f);
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); stage.CubeMesh = cube.GetComponent<MeshFilter>().sharedMesh; UnityEngine.Object.DestroyImmediate(cube);
            Log("QF16 (2): the passage stage - the saucer prefab instance at " + ts.position);
        }
    }
}
