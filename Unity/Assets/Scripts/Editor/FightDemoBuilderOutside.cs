using System;
using System.Collections.Generic;
using System.Linq;
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
                // QF43 (5): Dan saw flat grey with chunky white hexagons. The stars were 900 square quads 2.4-8.8 m across at 1400 m,
                // emissive 2.2 - bloom turned each into a blob. Now: small round points (12-sided discs, 0.5-1.7 m across: 1-3 px),
                // in three brightness groups (most faint, a few bright), facing the ship; the sky behind them is near-black
                // (SaucerOutside sets the camera's clear colour for open space)
                var vs = new[] { new List<Vector3>(), new List<Vector3>(), new List<Vector3>() }; var sts = new[] { new List<int>(), new List<int>(), new List<int>() };
                for (int i = 0; i < 1800; i++)
                {
                    var d = UnityEngine.Random.onUnitSphere; float r = 1400f, k = R(), sz = 0.25f + 0.6f * k * k;
                    int grp = k < 0.62f ? 0 : k < 0.92f ? 1 : 2;
                    var c = d * r; var side = Vector3.Cross(d, Mathf.Abs(d.y) > 0.9f ? Vector3.right : Vector3.up).normalized; var up = Vector3.Cross(side, d);
                    var v = vs[grp]; var t = sts[grp];
                    for (int s = 0; s < 12; s++) { float b0 = s / 12f * Mathf.PI * 2f, b1 = (s + 1) / 12f * Mathf.PI * 2f; int o = v.Count; v.Add(c); v.Add(c + (side * Mathf.Cos(b0) + up * Mathf.Sin(b0)) * sz); v.Add(c + (side * Mathf.Cos(b1) + up * Mathf.Sin(b1)) * sz); t.Add(o); t.Add(o + 1); t.Add(o + 2); }
                }
                float[] sglow = { 0.55f, 1.1f, 2.0f };
                for (int grp = 0; grp < 3; grp++)
                {
                    var sm = Lit("Outside_stars" + (grp == 0 ? "" : "_" + grp), Gray(1f), 0f); Emissive(sm, sglow[grp]); sm.SetFloat("_Cull", 0f); EditorUtility.SetDirty(sm);
                    Obj(grp == 0 ? "Outside_Stars" : "Outside_Stars" + grp, Finish(vs[grp], sts[grp]), sm, g, false);
                }
                so.Space = g.gameObject;
            }
            // ---- Round 16: the asteroid (AST_01 / AST_10) - irregular dark rock with silver structures built into it, rings
            // turning round a recessed industrial opening, small white route lights; one section intact, one stripped to its frame
            {
                var g = new GameObject("Outside_Asteroid").transform; g.SetParent(root, false);
                var c = new Vector3(0f, -22f, 165f);
                var rock = Lit("Outside_asteroid_rock", Gray(0.2f), 0.15f); rock.SetFloat("_Cull", 0f); EditorUtility.SetDirty(rock);
                var silver = Lit("Outside_asteroid_silver", Gray(0.78f), 0.8f, 0.85f);
                var lights = Lit("Outside_asteroid_lights", Gray(1f), 0f); Emissive(lights, 2.4f);
                var dark = Lit("Outside_asteroid_dark", Gray(0.05f), 0.2f);
                {
                    var v = new List<Vector3>(); var t = new List<int>(); int lat = 28, lon = 48;
                    for (int i = 0; i <= lat; i++) for (int j = 0; j <= lon; j++)
                    {
                        float th = i / (float)lat * Mathf.PI, ph = j / (float)lon * Mathf.PI * 2f;
                        var n = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                        float h = 1f + 0.22f * (Fbm(n.x * 2.2f + 3f, n.z * 2.2f + n.y * 1.7f) - 0.5f) * 2f;
                        v.Add(c + Vector3.Scale(n, new Vector3(95f, 62f, 70f)) * h);
                    }
                    for (int i = 0; i < lat; i++) for (int j = 0; j < lon; j++) { int a0 = i * (lon + 1) + j, b0 = a0 + lon + 1; t.AddRange(new[] { a0, b0, a0 + 1, a0 + 1, b0, b0 + 1 }); }
                    Obj("Asteroid_Rock", SmoothFinish(v, t), rock, g, false);
                }
                // the recessed opening facing the saucer, two rings turning round it
                var mouth = c + new Vector3(-8f, 6f, -66f);
                { var v = new List<Vector3>(); var t = new List<int>(); Append(v, t, RotX(Lathe(new[] { new Vector2(0f, 4f), new Vector2(15f, 4f), new Vector2(17f, 0f) }, 0f, 360f, 32), mouth)); Obj("Asteroid_Mouth", Finish(v, t), dark, g, false); }
                foreach (var (rr, sp, tilt) in new[] { (24f, 9f, 0f), (31f, -6f, 12f) })
                {
                    var v = new List<Vector3>(); var t = new List<int>();
                    Sweep(v, t, Enumerable.Range(0, 49).Select(i => new Vector3(Mathf.Cos(i / 48f * Mathf.PI * 2f) * rr, Mathf.Sin(i / 48f * Mathf.PI * 2f) * rr, 0f)).ToList(), 1.3f, true, 8);
                    for (int i = 0; i < 12; i++) { float a = i * 30f * Mathf.Deg2Rad; Ellipsoid(v, t, new Vector3(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr, -1.2f), Vector3.one * 0.9f, 3, 6); }
                    var ring = Obj($"Asteroid_Ring{(int)rr}", SmoothFinish(v, t), silver, g, false);
                    ring.transform.position = mouth; ring.transform.rotation = Quaternion.Euler(tilt, 0f, 0f);
                    var spin = ring.AddComponent<Seven.AstSpin>(); spin.Axis = Vector3.forward; spin.Speed = sp; spin.Essential = true;
                }
                // the intact section (towers, domes, gantries) and the section stripped to its framework
                {
                    var v = new List<Vector3>(); var t = new List<int>();
                    for (int i = 0; i < 14; i++)
                    {
                        float ax = -70f + i * 4.5f + R() * 2f, h = 14f + 26f * R();
                        var b = c + new Vector3(ax, 30f + R() * 14f, -42f - R() * 10f);
                        Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(2.2f, 0f), new Vector2(1.6f, h * 0.7f), new Vector2(2.4f, h * 0.75f), new Vector2(1.2f, h), new Vector2(0f, h + 2f) }, 0f, 360f, 10, null, b));
                    }
                    for (int i = 0; i < 5; i++) Ellipsoid(v, t, c + new Vector3(-50f + i * 12f, 40f, -30f), new Vector3(7f, 5f, 7f), 6, 10, p => p.y < c.y + 39f);
                    Obj("Asteroid_IntactSection", SmoothFinish(v, t), silver, g, false);
                    var fv = new List<Vector3>(); var ft = new List<int>();
                    for (int i = 0; i < 7; i++) for (int j = 0; j < 4; j++)
                    {
                        var b = c + new Vector3(30f + i * 7f, 22f + j * 8f, -50f + j * 3f);
                        Sweep(fv, ft, new[] { b, b + Vector3.up * 8f }, 0.35f, false, 4); Sweep(fv, ft, new[] { b + Vector3.up * 8f, b + new Vector3(7f, 8f, 0f) }, 0.3f, false, 4);
                        if ((i + j) % 2 == 0) Sweep(fv, ft, new[] { b, b + new Vector3(7f, 8f, 0f) }, 0.25f, false, 4);
                    }
                    Obj("Asteroid_StrippedFrame", Finish(fv, ft), silver, g, false);
                }
                // small white route lights along the surface
                {
                    var v = new List<Vector3>(); var t = new List<int>();
                    for (int r2 = 0; r2 < 4; r2++) for (int i = 0; i < 26; i++)
                    {
                        float a = (i / 26f) * 220f - 110f + r2 * 9f;
                        var p = c + Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 18f + r2 * 9f - Mathf.Abs(a) * 0.05f, -78f + Mathf.Abs(a) * 0.12f);
                        Ellipsoid(v, t, p, Vector3.one * 0.5f, 3, 6);
                    }
                    Obj("Asteroid_RouteLights", Finish(v, t), lights, g, false);
                }
                // the archive light (switches on in AST_10)
                var al = new GameObject("Asteroid_ArchiveLight").AddComponent<Light>(); al.transform.SetParent(g, false); al.transform.position = mouth + new Vector3(18f, 10f, -6f); al.type = LightType.Point; al.range = 60f; al.intensity = 6f; al.color = Color.white; al.enabled = false;
                { var v = new List<Vector3>(); var t = new List<int>(); Ellipsoid(v, t, mouth + new Vector3(18f, 10f, -2f), Vector3.one * 1.4f, 5, 8); var lm = Lit("Outside_asteroid_archivelamp", Gray(0.4f), 0.2f); Emissive(lm, 0.2f); so.ArchiveLamp = Obj("Asteroid_ArchiveLamp", SmoothFinish(v, t), lm, g, false).GetComponent<Renderer>(); }
                so.ArchiveLight = al;
                so.Asteroid = g.gameObject;
                g.gameObject.SetActive(false);
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
