using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using B = Seven.BiavehLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 17 — builds the scene "Biaveh" (Chapter "The Biavian Mothership (Veyra)", Docs/CHAPTER_BIAVEH.md): the interior
    /// city at the bottom of the egg's shell (Kyle's misty monochrome: smooth pale Biavian buildings, hanging gardens, transit
    /// lights, bridges overhead, the city curving up the shell into the haze - no plain boxes), the receiving berth and its
    /// overlook, the roads, the service junction under the high bridge, the chasm with the suspended bridge, the overlapping
    /// garden, the quiet passage with its airlock pair, the temple's transfer chamber (platforms, partitions, the maintenance
    /// compartment), the memory chamber round the octahedron; Nela and the residents on Dan's Biavian model (Kyle's design,
    /// Art/Mixamo/biavian/biavian_body_mixamo.fbx - sizes / tones / medallion glyphs varied), Ratwing (the Round 4 model), the
    /// one saucer prefab, 7, the bike, a small aboard set for Panther's cuts. Numbers in Runtime/BiavehLayout.cs.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const string BiavehScenePath = "Assets/Scenes/Biaveh.unity";
        const string AtmoBiavehPath = AtmoDir + "/Atmosphere_Biaveh.prefab";
        const string BiavianDir = CharRoot + "/Biavian";
        struct BiMats { public Material pale, paleSoft, dark, mid, floor, glow, glowSoft, glass, echo, leaf, stone; }

        static void BuildRound17(AnimatorController sevenCtl, GameObject atmoPrefab, Seven.VoiceBank bank)
        {
            var bia = SetupMixamoFolk("biavian", BiavianDir, out var biaCtl, out var biaMat);
            var bike = AssetDatabase.LoadAssetAtPath<GameObject>(CharRoot + "/hoverbike/hoverbike_v1.fbx");
            var ratCtl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "/Ratwing.controller");
            var atmo = BuildAtmosphereBiaveh(atmoPrefab);
            CopyVideo("vision_06_mothership.mp4");
            BuildBiavehScene(sevenCtl, bank, atmo, bia, biaCtl, biaMat, bike, ratCtl);
        }

        static GameObject BuildAtmosphereBiaveh(GameObject atmoPrefab)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(atmoPrefab);
            var a = inst.GetComponent<Seven.Atmosphere>();
            // inside the mothership: dark near silhouettes, the city glowing and misty as it curves up the shell, ground fog in
            // the chasm and the gardens, soft lights inside the fog
            a.FogStart = 16f;
            a.GroundDensity = 0.014f; a.GroundBase = -12f; a.GroundFalloff = 10f; a.GroundRange = 160f;
            a.ValleyDensity = 0f;
            a.DistanceDensity = 0.0019f; a.HazeHeight = 260f; a.MaxFog = 0.74f;
            a.NearBrightness = 0.08f; a.FarBrightness = 0.52f; a.BrightnessDistance = 260f; a.ZenithBrightness = 0.34f;
            a.NoiseStrength = 0.3f; a.NoiseScale = 0.035f; a.NoiseSpeed = 0.025f; a.NoiseFadeDistance = 80f;
            a.GlowIntensity = 0.42f; a.GlowRadius = 70f;
            var glows = new[]
            {
                (new Vector3(120f, 60f, 320f), new Vector3(10f, 4f, 1f)), (new Vector3(-80f, 90f, 260f), new Vector3(8f, 3f, 1f)), (new Vector3(260f, 120f, 420f), new Vector3(9f, 3f, 1f)),
                (new Vector3(180f, 40f, -160f), new Vector3(7f, 3f, 1f)), (new Vector3(170f, -20f, 0f), new Vector3(4f, 2f, 1f)), (new Vector3(260f, 8f, 66f), new Vector3(3f, 1.4f, 1f)),
                (new Vector3(175f, 24f, 125f), new Vector3(4f, 2f, 1f)), (new Vector3(0f, 12f, 30f), new Vector3(3f, 1.2f, 1f)),
            };
            int gi = 0;
            foreach (Transform c in inst.transform)
            {
                if (c.name.StartsWith("Glow") && gi < glows.Length) { c.position = glows[gi].Item1; c.localScale = glows[gi].Item2; gi++; }
                if (c.name == "BackLight") { c.rotation = Quaternion.Euler(40f, 200f, 0f); var l = c.GetComponent<Light>(); l.intensity = 0.75f; l.shadowStrength = 0.55f; }
                if (c.name == "FrontFill") { c.rotation = Quaternion.Euler(45f, 20f, 0f); c.GetComponent<Light>().intensity = 0.4f; }
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(inst, AtmoBiavehPath);
            Object.DestroyImmediate(inst);
            return prefab;
        }

        // ---------------------------------------------------------------- the kit: smooth Biavian forms (never boxes)
        /// <summary>A smooth rounded tower: a soft-shouldered lathe with banded light rings.</summary>
        static void Tower(AK k, BiMats m, Vector3 at, float r, float h, bool lights = true)
        {
            var (v, t) = k.Of(m.pale);
            Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(r * 1.15f, 0f), new Vector2(r, h * 0.08f), new Vector2(r * 0.86f, h * 0.55f), new Vector2(r * 0.95f, h * 0.85f), new Vector2(r * 0.6f, h * 0.98f), new Vector2(0f, h) }, 0f, 360f, 16, null, at));
            if (!lights) return;
            var (gv, gt) = k.Of(m.glowSoft);
            for (int i = 1; i < 4; i++) { float y = h * (0.25f + i * 0.18f); float rr = Mathf.Lerp(r, r * 0.86f, Mathf.InverseLerp(h * 0.08f, h * 0.55f, y)) + 0.05f; Append(gv, gt, Lathe(new[] { new Vector2(rr, y - 0.25f), new Vector2(rr, y + 0.25f) }, 0f, 360f, 16, null, at)); }
        }
        /// <summary>A hanging garden: a shallow bowl hung by four cables, foliage spilling over its rim.</summary>
        static void HangingGarden(AK k, BiMats m, Vector3 at, float r)
        {
            var (v, t) = k.Of(m.mid); Append(v, t, Lathe(new[] { new Vector2(0f, -r * 0.5f), new Vector2(r * 0.7f, -r * 0.35f), new Vector2(r, 0f), new Vector2(r * 0.95f, 0.1f), new Vector2(0f, 0.1f) }, 0f, 360f, 14, null, at));
            var (lv, lt) = k.Of(m.leaf);
            for (int i = 0; i < 10; i++) { var d = Quaternion.Euler(0f, i * 36f, 0f) * Vector3.right; Sweep(lv, lt, new[] { at + d * r * 0.95f, at + d * r * 1.05f + Vector3.down * r * 0.5f, at + d * r * 1.0f + Vector3.down * r * 0.9f }, r * 0.12f, false, 5); Ellipsoid(lv, lt, at + d * r * 0.4f + Vector3.up * r * 0.25f, new Vector3(r * 0.35f, r * 0.3f, r * 0.35f), 4, 7); }
            var (cv, ct) = k.Of(m.dark); for (int i = 0; i < 4; i++) { var d = Quaternion.Euler(0f, 45f + i * 90f, 0f) * Vector3.right; Sweep(cv, ct, new[] { at + d * r * 0.9f, at + d * r * 0.3f + Vector3.up * 60f }, 0.06f, false, 4); }
        }
        /// <summary>A smooth arched bridge between two points (high overhead).</summary>
        static void ArchBridge(AK k, BiMats m, Vector3 a, Vector3 b, float w, float sag)
        {
            var pts = Enumerable.Range(0, 17).Select(i => Vector3.Lerp(a, b, i / 16f) + Vector3.down * Mathf.Sin(i / 16f * Mathf.PI) * -sag).ToList();
            var (v, t) = k.Of(m.pale); Sweep(v, t, pts, w * 0.5f, false, 8);
            var (gv, gt) = k.Of(m.glowSoft); Sweep(gv, gt, pts.Select(p => p + Vector3.up * w * 0.5f).ToList(), 0.08f, false, 4);
        }
        /// <summary>A rounded control pedestal with a glowing top (relays, stations, controls).</summary>
        static Renderer Pedestal(AK k, BiMats m, Vector3 at, float h, string lampName, Transform parent)
        {
            var (v, t) = k.Of(m.pale); Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.42f, 0f), new Vector2(0.3f, h * 0.3f), new Vector2(0.24f, h * 0.85f), new Vector2(0.36f, h), new Vector2(0f, h + 0.05f) }, 0f, 360f, 14, null, at));
            var lv = new List<Vector3>(); var lt = new List<int>(); Ellipsoid(lv, lt, at + Vector3.up * (h + 0.08f), new Vector3(0.28f, 0.07f, 0.28f), 4, 10);
            return MeshObject(lampName, SaveMesh(SmoothFinish(lv, lt), "Bia_" + lampName), new Material(m.glowSoft) { name = "Bia_" + lampName.ToLower() }, parent, false).GetComponent<Renderer>();
        }
        /// <summary>A memory echo: translucent additive shapes (never solid): seated figures, an old doorway, an old span.</summary>
        static Seven.MemoryEcho Echo(string name, Transform parent, Material mat, System.Action<List<Vector3>, List<int>> shapes)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false);
            var v = new List<Vector3>(); var t = new List<int>(); shapes(v, t);
            MeshObject(name + "_Light", SaveMesh(SmoothFinish(v, t), "Bia_" + name), mat, root, false);
            var e = root.gameObject.AddComponent<Seven.MemoryEcho>(); e.Strength = 0.6f;
            return e;
        }
        static void SeatedFigure(List<Vector3> v, List<int> t, Vector3 at, float yaw)
        {
            var q = Quaternion.Euler(0f, yaw, 0f);
            Ellipsoid(v, t, at + q * new Vector3(0f, 1.25f, 0f), new Vector3(0.2f, 0.26f, 0.22f), 6, 10);           // head
            Ellipsoid(v, t, at + q * new Vector3(0f, 0.8f, 0f), new Vector3(0.22f, 0.32f, 0.16f), 6, 10);           // torso
            Ellipsoid(v, t, at + q * new Vector3(0f, 0.45f, 0.2f), new Vector3(0.2f, 0.1f, 0.3f), 4, 8);            // lap
            Ellipsoid(v, t, at + q * new Vector3(0f, 0.42f, -0.05f), new Vector3(0.32f, 0.04f, 0.3f), 3, 8);        // the seat
        }

        static void BuildBiavehScene(AnimatorController sevenCtl, Seven.VoiceBank bank, GameObject atmoP, GameObject bia, AnimatorController biaCtl, Material biaMat, GameObject bike, AnimatorController ratCtl)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null; RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.34f, 0.34f, 0.34f);
            var shs = new SphericalHarmonicsL2(); shs.AddAmbientLight(RenderSettings.ambientLight); RenderSettings.ambientProbe = shs;
            var atmo = (GameObject)PrefabUtility.InstantiatePrefab(atmoP); atmo.name = "Atmosphere_Biaveh";
            var world = new GameObject("Mothership").transform;
            var m = new BiMats
            {
                pale = Dissolve("Bia_pale", Gray(0.78f), 0.55f, false, 0.1f), paleSoft = Dissolve("Bia_pale_soft", Gray(0.62f), 0.4f, false),
                dark = Dissolve("Bia_dark", Gray(0.16f), 0.5f, false, 0.3f), mid = Dissolve("Bia_mid", Gray(0.42f), 0.45f, false, 0.2f),
                floor = Dissolve("Bia_floor", Gray(0.5f), 0.35f, false), glow = Dissolve("Bia_glow", Color.white, 0.5f, false),
                glowSoft = Dissolve("Bia_glow_soft", Gray(0.55f), 0.4f, false), glass = AdditiveMat("Bia_glass", 0.07f),
                echo = AdditiveMat("Bia_echo", 0.6f), leaf = Dissolve("Bia_leaf", Gray(0.28f), 0.2f, false), stone = Dissolve("Bia_stone", Gray(0.7f), 0.3f, false),
            };
            m.glow.EnableKeyword("_EMISSION"); m.glow.SetColor("_EmissionColor", Color.white * 2.2f); EditorUtility.SetDirty(m.glow);
            m.glowSoft.EnableKeyword("_EMISSION"); m.glowSoft.SetColor("_EmissionColor", Color.white * 0.35f); EditorUtility.SetDirty(m.glowSoft);
            foreach (var x in new[] { m.pale, m.paleSoft, m.dark, m.mid, m.floor, m.leaf, m.stone }) { x.SetFloat("_Cull", 0f); EditorUtility.SetDirty(x); }
            var am = new AsMats { metal = m.mid, metalDark = m.dark, steel = m.pale, plate = m.paleSoft, floor = m.floor, rock = m.mid, rockDark = m.dark, glow = m.glow, glowSoft = m.glowSoft, cable = m.dark, panel = m.dark, glass = m.glass, beam = m.echo, stone = m.stone };

            // ---------------------------------------------------------------- floors (every zone; the chasm left open)
            foreach (var z in B.Zones)
            {
                if (z.Name == "Bridge" || z.Name == "LockDoorA" || z.Name == "LockDoorB" || z.Name == "ArchiveDoorZ" || z.Name == "Compartment") continue;
                MGround("Bia_Floor_" + z.Name, new Vector3(z.X0, -1.5f, z.Z0), new Vector3(z.X1, 0f, z.Z1), z.Name == "Garden" ? m.leaf : z.Name.StartsWith("Road") ? m.mid : m.floor, world);
            }
            foreach (var n in new[] { "LockDoorA", "LockDoorB", "ArchiveDoorZ", "Compartment" }) { var z = B.Zones.First(q => q.Name == n); MGround("Bia_Floor_" + n, new Vector3(z.X0, -1.5f, z.Z0), new Vector3(z.X1, 0f, z.Z1), m.floor, world); }
            var k = new AK();
            // the chasm: its walls dropping into the fog
            { var (v, t) = k.Of(m.dark); MBox(v, t, new Vector3(150f, -40f, 0f), new Vector3(1f, 80f, 40f), 0f, 0f); MBox(v, t, new Vector3(190f, -40f, 0f), new Vector3(1f, 80f, 40f), 0f, 0f); }
            // road edges: low smooth kerbs with light strips (transit lights along the roads)
            foreach (var (a, b2) in new[] { (new Vector3(22f, 0f, -5.2f), new Vector3(150f, 0f, -5.2f)), (new Vector3(22f, 0f, 5.2f), new Vector3(120f, 0f, 5.2f)), (new Vector3(206f, 0f, -5.2f), new Vector3(265f, 0f, -5.2f)), (new Vector3(206f, 0f, 5.2f), new Vector3(255f, 0f, 5.2f)), (new Vector3(214.2f, 0f, 71f), new Vector3(214.2f, 0f, 102f)), (new Vector3(205.2f, 0f, 71f), new Vector3(205.2f, 0f, 102f)) })
            { var (v, t) = k.Of(m.pale); Sweep(v, t, new[] { a + Vector3.up * 0.15f, b2 + Vector3.up * 0.15f }, 0.18f, false, 6); var (gv, gt) = k.Of(m.glowSoft); Sweep(gv, gt, new[] { a + Vector3.up * 0.36f, b2 + Vector3.up * 0.36f }, 0.03f, false, 4); }
            k.Save("Bia_Ground", world);

            // ---------------------------------------------------------------- the city curving up the shell (visual)
            var city = new GameObject("InteriorCity").transform; city.SetParent(world, false);
            var ck = new AK(); var rnd = new System.Random(7);
            System.Func<float, float, Vector3> Shell = (x, z) => new Vector3(x, B.ShellRise(x, z) - 2f, z);
            // the shell's own surface rising on every side beyond the play area (a grid of quads, banded in two tones)
            for (int ix = 0; ix < 36; ix++)
                for (int iz = 0; iz < 28; iz++)
                {
                    float x0 = -460f + ix * 30f, z0 = -380f + iz * 32f, x1 = x0 + 30f, z1 = z0 + 32f;
                    if (B.ShellRise(x0, z0) < 0.5f && B.ShellRise(x1, z1) < 0.5f && B.ShellRise(x0, z1) < 0.5f && B.ShellRise(x1, z0) < 0.5f) continue;
                    var (v, t) = ck.Of((ix + iz) % 5 == 0 ? m.mid : m.paleSoft);
                    Quad(v, t, Shell(x0, z0), Shell(x0, z1), Shell(x1, z1), Shell(x1, z0));
                }
            // smooth buildings standing out of the curving shell (normal to it), terraces of them up the curve
            for (int i = 0; i < 420; i++)
            {
                float x = (float)rnd.NextDouble() * 900f - 460f, z = (float)rnd.NextDouble() * 880f - 380f;
                float rise = B.ShellRise(x, z); if (rise < 3f || rise > 300f) continue;
                var p = Shell(x, z);
                var n = Vector3.Cross(Shell(x, z + 1f) - Shell(x, z - 1f), Shell(x + 1f, z) - Shell(x - 1f, z)).normalized; if (n.y < 0f) n = -n;
                float h = 8f + (float)rnd.NextDouble() * 30f, r = 3f + (float)rnd.NextDouble() * 5f;
                var tv = new AK(); Tower(tv, m, Vector3.zero, r, h, i % 2 == 0);
                foreach (var kv in tv.parts) { var (v, t) = ck.Of(kv.Key); var rot = Quaternion.FromToRotation(Vector3.up, n); int bse = v.Count; v.AddRange(kv.Value.v.Select(q => p + rot * q)); t.AddRange(kv.Value.t.Select(q => q + bse)); }
            }
            for (int i = 0; i < 44; i++) { float x = (float)rnd.NextDouble() * 500f - 340f, z = (float)rnd.NextDouble() * 500f - 200f; float rise = B.ShellRise(x, z); if (rise < 20f) continue; HangingGarden(ck, m, Shell(x, z) + Vector3.up * (16f + (float)rnd.NextDouble() * 40f) + Vector3.right * 20f, 5f + (float)rnd.NextDouble() * 5f); }
            for (int i = 0; i < 16; i++) { float z = -160f + i * 26f; ArchBridge(ck, m, Shell(-150f, z) + Vector3.up * 30f, Shell(-60f - (i % 4) * 10f, z + 40f) + Vector3.up * 34f, 3.6f, 10f); }
            for (int i = 0; i < 10; i++) { float x = -60f + i * 40f; ArchBridge(ck, m, Shell(x, 290f) + Vector3.up * 30f, Shell(x + 30f, 230f) + Vector3.up * 40f, 3.6f, 10f); }
            ck.Save("Bia_City", city);
            // transit lights disappearing into the mist (lines of glowing pods), city lights that ripple when the archive is restored
            var cityLights = new List<Renderer>();
            for (int i = 0; i < 48; i++)
            {
                float z = -180f + i * 9f; var p = Shell(-90f - (i % 5) * 28f, z) + Vector3.up * (14f + (i % 4) * 7f);
                var lv = new List<Vector3>(); var lt = new List<int>(); Ellipsoid(lv, lt, p, new Vector3(3f, 1.2f, 3f), 4, 8);
                cityLights.Add(MeshObject("CityLight" + i, SaveMesh(SmoothFinish(lv, lt), "Bia_CityLight" + i), new Material(m.glowSoft) { name = "Bia_citylight" + i }, city, false).GetComponent<Renderer>());
            }
            var tk = new AK(); for (int l = 0; l < 6; l++) { var (gv, gt) = tk.Of(m.glow); float x0 = -70f - l * 30f; for (int i = 0; i < 40; i++) Ellipsoid(gv, gt, Shell(x0 - i * 2f, -200f + i * 12f + l * 6f) + Vector3.up * (8f + l * 5f), new Vector3(0.5f, 0.4f, 1.4f), 3, 6); } tk.Save("Bia_TransitLights", city, false, false);

            // ---------------------------------------------------------------- the receiving berth, the overlook, the display, the echo
            var berth = new GameObject("ReceivingBerth").transform; berth.SetParent(world, false);
            var bk = new AK();
            { var (v, t) = bk.Of(m.pale); Append(v, t, Lathe(new[] { new Vector2(0f, 0.02f), new Vector2(7.5f, 0.02f), new Vector2(7.8f, 0.08f), new Vector2(0f, 0.08f) }, 0f, 360f, 32)); }
            { var (gv, gt) = bk.Of(m.glowSoft); Append(gv, gt, Lathe(new[] { new Vector2(7.5f, 0.085f), new Vector2(7.8f, 0.085f) }, 0f, 360f, 32)); }
            // the overlook's railing (smooth posts + a rail), the berth's walls curving up behind
            { var (v, t) = bk.Of(m.pale); Sweep(v, t, Enumerable.Range(0, 17).Select(i => new Vector3(-21.6f, 1.05f, -12f + i * 1.5f)).ToList(), 0.06f, false, 6); for (int i = 0; i < 9; i++) Sweep(v, t, new[] { new Vector3(-21.6f, 0f, -12f + i * 3f), new Vector3(-21.6f, 1.05f, -12f + i * 3f) }, 0.05f, false, 5); }
            foreach (var (a, b2) in new[] { (new Vector3(-12f, 0f, -22.4f), new Vector3(22.4f, 0f, -22.4f)), (new Vector3(22.4f, 0f, -22.4f), new Vector3(22.4f, 0f, -5.4f)), (new Vector3(24.4f, 0f, 9f), new Vector3(24.4f, 0f, 20f)) })
            { var (v, t) = bk.Of(m.paleSoft); var d = (b2 - a); float len = d.magnitude; MBox(v, t, (a + b2) * 0.5f + Vector3.up * 6f, new Vector3(Mathf.Abs(d.x) > 0.1f ? len : 0.8f, 12f, Mathf.Abs(d.z) > 0.1f ? len : 0.8f), 0f, 0.3f); }
            // the bench by the passage; the sealed gate (a smooth arch with a dark, closed leaf)
            { var (v, t) = bk.Of(m.pale); Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.9f, 0f), new Vector2(0.95f, 0.42f), new Vector2(0f, 0.46f) }, 0f, 360f, 16, null, B.BenchAt)); }
            { var (v, t) = bk.Of(m.pale); Sweep(v, t, Enumerable.Range(0, 13).Select(i => { float a = i / 12f * Mathf.PI; return B.SealedGate + new Vector3(Mathf.Cos(a) * 2.4f, Mathf.Sin(a) * 2.4f - 3f, 0f); }).ToList(), 0.25f, false, 8); var (dv, dt) = bk.Of(m.dark); MBox(dv, dt, B.SealedGate + new Vector3(0f, -1.8f, 0.2f), new Vector3(4.2f, 2.4f, 0.3f), 0f, 0.15f); }
            // QF23 (6): the berth HOLDS the saucer without touching it - four slim, smooth white arms rising from the landing ring
            // and curving in toward its rim (their glowing pads stop 0.6 m short of the hull), the beam's landing pad beneath
            for (int i = 0; i < 4; i++)
            {
                // QF24 (5): the arm set turned round the saucer so NO arm stands in front of the bike hold (hold at
                // SaucerHold.Angle 41.25 deg - the old 45 deg arm stood right in its opening): 70 / 140 / 220 / 290 deg from it
                var (av, at2) = bk.Of(m.pale); float a = (Seven.SaucerHold.Angle + new[] { 70f, 140f, 220f, 290f }[i]) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var pts = new List<Vector3> { B.SaucerAt + d * 8.6f, B.SaucerAt + d * 8.9f + Vector3.up * 1.6f, B.SaucerAt + d * 8.2f + Vector3.up * 3.2f, B.SaucerAt + d * 7.0f + Vector3.up * 3.9f };
                Sweep(av, at2, pts, 0.16f, false, 10);
                var (gv3, gt3) = bk.Of(m.glowSoft); Ellipsoid(gv3, gt3, B.SaucerAt + d * 6.85f + Vector3.up * 3.9f, new Vector3(0.22f, 0.1f, 0.22f), 4, 8);
                Append(av, at2, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.7f, 0f), new Vector2(0.45f, 0.3f), new Vector2(0f, 0.32f) }, 0f, 360f, 14, null, B.SaucerAt + d * 8.6f));
            }
            { var (gv4, gt4) = bk.Of(m.glowSoft); Append(gv4, gt4, Lathe(new[] { new Vector2(2.2f, 0.09f), new Vector2(2.6f, 0.09f) }, 0f, 360f, 32, null, B.SaucerAt)); }   // the beam's landing pad
            bk.Save("Bia_Berth", berth);
            // the temple-route display: a holo table, the temple at its centre, the three relays round it (they light in BIA_04)
            var disp = new GameObject("RouteDisplay").transform; disp.SetParent(berth, false); disp.position = B.DisplayAt;
            { var dk = new AK(); var (v, t) = dk.Of(m.pale); Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0.32f, 0.5f), new Vector2(0.3f, 0.9f), new Vector2(0.75f, 0.95f), new Vector2(0.78f, 1.0f), new Vector2(0f, 1.0f) }, 0f, 360f, 20)); foreach (var g in dk.Save("RouteDisplay_Table", disp)) g.transform.localPosition = Vector3.zero; }
            var holo = new List<Vector3>(); var holoT = new List<int>(); Sweep(holo, holoT, Enumerable.Range(0, 25).Select(i => new Vector3(Mathf.Cos(i / 24f * Mathf.PI * 2f) * 0.5f, 1.08f, Mathf.Sin(i / 24f * Mathf.PI * 2f) * 0.5f)).ToList(), 0.012f, true, 4);
            Ellipsoid(holo, holoT, new Vector3(0f, 1.12f, 0f), new Vector3(0.07f, 0.09f, 0.07f), 4, 8);
            MeshObject("RouteDisplay_Holo", SaveMesh(SmoothFinish(holo, holoT), "Bia_RouteHolo"), new Material(m.glowSoft) { name = "Bia_routeholo" }, disp, false);
            var marks = new List<Renderer>();
            for (int i = 0; i < 3; i++) { var a = i * 120f + 30f; var mv = new List<Vector3>(); var mt = new List<int>(); Ellipsoid(mv, mt, new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 0.42f, 1.1f, Mathf.Sin(a * Mathf.Deg2Rad) * 0.42f), Vector3.one * 0.045f, 4, 8); marks.Add(MeshObject("RouteMark" + i, SaveMesh(SmoothFinish(mv, mt), "Bia_RouteMark" + i), new Material(m.glowSoft) { name = "Bia_routemark" + i }, disp, false).GetComponent<Renderer>()); }
            var rl = new List<Vector3>(); var rlt = new List<int>(); Sweep(rl, rlt, Enumerable.Range(0, 4).Select(i => { var a = (i % 3) * 120f + 30f; return new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 0.42f, 1.1f, Mathf.Sin(a * Mathf.Deg2Rad) * 0.42f); }).ToList(), 0.008f, false, 4);
            var routeLine = MeshObject("RouteLine", SaveMesh(SmoothFinish(rl, rlt), "Bia_RouteLine"), new Material(m.glowSoft) { name = "Bia_routeline" }, disp, false).GetComponent<Renderer>();
            foreach (Transform c in disp) if (c.name.StartsWith("Route")) c.localPosition = Vector3.zero;
            var echoBerth = Echo("Echo_SeatedFigures", berth, m.echo, (v, t) => { SeatedFigure(v, t, B.EchoAt + new Vector3(-0.8f, 0f, 0.6f), 160f); SeatedFigure(v, t, B.EchoAt + new Vector3(0.9f, 0f, 0.3f), 200f); SeatedFigure(v, t, B.EchoAt + new Vector3(0f, 0f, -0.8f), 10f); Sweep(v, t, new[] { B.EchoAt + new Vector3(-2.5f, 0f, -2f), B.EchoAt + new Vector3(-2.5f, 2.8f, -2f), B.EchoAt + new Vector3(2.5f, 2.8f, -2f), B.EchoAt + new Vector3(2.5f, 0f, -2f) }, 0.03f, false, 4); });

            // ---------------------------------------------------------------- BIA_05: the junction, the high bridge, relay 1, the platform, the shaft
            var junc = new GameObject("ServiceJunction").transform; junc.SetParent(world, false);
            var jk = new AK();
            Tower(jk, m, B.PillarAt, 0.9f, 15f, false);                                       // the structural pillar (she stops behind it)
            ArchBridge(jk, m, new Vector3(110f, B.HighBridgeY, -2f), new Vector3(175f, B.HighBridgeY, -2f), 3.2f, -1.2f);
            foreach (var x in new[] { 128f, 156f }) { var (v, t) = jk.Of(m.pale); Sweep(v, t, new[] { new Vector3(x, B.HighBridgeY - 1.6f, -2f), new Vector3(x, B.HighBridgeY - 0.2f, -2f) }, 0.4f, false, 8); }
            for (int i = 0; i < 6; i++) { var (v, t) = jk.Of(m.dark); Sweep(v, t, new[] { new Vector3(132f + i * 3f, B.HighBridgeY - 1.4f, -3.2f), new Vector3(133.5f + i * 3f, B.HighBridgeY - 2.6f, -2f), new Vector3(132f + i * 3f, B.HighBridgeY - 1.4f, -0.8f) }, 0.12f, false, 5); }   // the supports he hides among
            { var (v, t) = jk.Of(m.pale); Append(v, t, Lathe(new[] { new Vector2(0f, -1.2f), new Vector2(5f, -1.2f), new Vector2(5.4f, -0.3f), new Vector2(5f, 0f), new Vector2(0f, 0f) }, 0f, 360f, 20, null, B.RatwingDrop)); }   // the distant platform
            { var (v, t) = jk.Of(m.dark); Append(v, t, Lathe(new[] { new Vector2(14f, -60f), new Vector2(14f, 120f), new Vector2(15f, 120f), new Vector2(15f, -60f) }, 0f, 360f, 24, null, new Vector3(B.ShaftTop.x, 0f, B.ShaftTop.z))); }   // the vertical shaft
            var lamp1 = RelayPylon(m, B.CabinetAt, B.CabinetView - B.CabinetAt, Vector3.left, "Relay1_Lamp", junc);   // QF24 (10): the redesigned relay (was a cabinet + a lamp)
            // QF24 (10): the old cabinet housing is gone - the relay pylon stands here
            jk.Save("Bia_Junction", junc);
            Transform cabDoor = null;   // QF24 (10): no cabinet door - the relay is open to her hand (its resonance plate)

            // ---------------------------------------------------------------- BIA_06: the suspended bridge (three segments), the echo span, the partition
            var br = new GameObject("SuspendedBridge").transform; br.SetParent(world, false);
            var segs = new List<Transform>(); var parked = new List<Vector3>(); var aligned = new List<Vector3>();
            for (int i = 0; i < 3; i++)
            {
                float x0 = B.ChasmX0 + i * 13.33f;
                var pivot = new GameObject("BridgeSegment" + i).transform; pivot.SetParent(br, false); pivot.position = new Vector3(x0 + 6.67f, 0f, 0f);
                var sk = new AK(); { var (v, t) = sk.Of(m.pale); MBox(v, t, new Vector3(0f, -0.3f, 0f), new Vector3(13.3f, 0.6f, 5.2f), 0f, 0.08f); var (rv, rt) = sk.Of(m.glowSoft); foreach (var s in new[] { -1f, 1f }) Sweep(rv, rt, new[] { new Vector3(-6.6f, 0.9f, s * 2.6f), new Vector3(6.6f, 0.9f, s * 2.6f) }, 0.04f, false, 4); var (cv, ct) = sk.Of(m.dark); foreach (var s in new[] { -1f, 1f }) Sweep(cv, ct, new[] { new Vector3(0f, 0f, s * 2.7f), new Vector3(0f, 40f, s * 2.7f) }, 0.05f, false, 4); }
                foreach (var g in sk.Save("BridgeSegment" + i + "_Mesh", pivot)) g.transform.localPosition = Vector3.zero;
                segs.Add(pivot); parked.Add(new Vector3(0f, i == 1 ? 38f : -32f, i == 1 ? 0f : 8f)); aligned.Add(Vector3.zero);
            }
            var echoBridge = Echo("Echo_OldSpan", br, m.echo, (v, t) => { Sweep(v, t, Enumerable.Range(0, 9).Select(i => new Vector3(B.ChasmX0 + i * 5f, -0.2f - Mathf.Sin(i / 8f * Mathf.PI) * 1.4f, 3.5f)).ToList(), 1.2f, false, 6); Sweep(v, t, Enumerable.Range(0, 9).Select(i => new Vector3(B.ChasmX0 + i * 5f, 1.2f - Mathf.Sin(i / 8f * Mathf.PI) * 1.4f, 5f)).ToList(), 0.05f, false, 4); });
            var bck = new AK(); var lampBC = Pedestal(bck, m, B.BridgeControl, 1.2f, "BridgeControl_Lamp", br); bck.Save("Bia_BridgeControl", br);
            var lever = new GameObject("BridgeControl_Lever").transform; lever.SetParent(br, false); lever.position = B.BridgeControl + Vector3.up * 1.25f;
            { var lv = new List<Vector3>(); var lt = new List<int>(); Sweep(lv, lt, new[] { Vector3.zero, new Vector3(0f, 0.6f, 0.2f) }, 0.035f, false, 6); Ellipsoid(lv, lt, new Vector3(0f, 0.63f, 0.21f), Vector3.one * 0.07f, 4, 8); MeshObject("BridgeControl_LeverArm", SaveMesh(SmoothFinish(lv, lt), "Bia_BridgeLever"), m.pale, lever).transform.localPosition = Vector3.zero; }
            var partA = new GameObject("PartitionA").transform; partA.SetParent(world, false); partA.position = B.PartitionA;
            { var pk = new AK(); var (v, t) = pk.Of(m.paleSoft); MBox(v, t, Vector3.zero, new Vector3(30f, 24f, 1.2f), 0f, 0.5f); var (gv, gt) = pk.Of(m.glowSoft); Sweep(gv, gt, new[] { new Vector3(-15f, -11.6f, -0.7f), new Vector3(15f, -11.6f, -0.7f) }, 0.12f, false, 4); foreach (var g in pk.Save("PartitionA_Mesh", partA)) g.transform.localPosition = Vector3.zero; }
            var seedlings = new GameObject("Seedlings").transform; seedlings.SetParent(world, false);
            // QF23 (13b): an open tray of young Biavian seedlings - soft leaves with gently glowing tips, a few unfurling fronds -
            // visibly OVERGROWN, spilling over the sides (they grew while she waited); carried in both arms in front of her
            {
                var sk = new AK(); var (v, t) = sk.Of(m.pale);
                MBox(v, t, new Vector3(0f, 0.02f, 0f), new Vector3(0.62f, 0.04f, 0.42f), 0f, 0.01f);
                foreach (var zz in new[] { -0.2f, 0.2f }) MBox(v, t, new Vector3(0f, 0.08f, zz), new Vector3(0.62f, 0.12f, 0.022f), 0f, 0.005f);
                foreach (var xx in new[] { -0.3f, 0.3f }) MBox(v, t, new Vector3(xx, 0.08f, 0f), new Vector3(0.022f, 0.12f, 0.42f), 0f, 0.005f);
                var (dv, dt2) = sk.Of(m.dark); MBox(dv, dt2, new Vector3(0f, 0.11f, 0f), new Vector3(0.58f, 0.02f, 0.38f), 0f, 0f);    // the soil
                var (lv2, lt2) = sk.Of(m.leaf); var (gv2, gt2) = sk.Of(m.glow);
                var srnd = new System.Random(6);
                float Rn() => (float)srnd.NextDouble();
                for (int i = 0; i < 24; i++)
                {
                    float x = -0.25f + (i % 6) * 0.1f, z = -0.15f + (i / 6) * 0.1f; var b0 = new Vector3(x, 0.12f, z);
                    bool edge = Mathf.Abs(x) > 0.2f || Mathf.Abs(z) > 0.12f;
                    var outward = new Vector3(x, 0f, z).normalized;
                    float h = 0.14f + Rn() * 0.16f;
                    // the stem: up, then (at the edges) out and over the side, drooping
                    var tip = edge ? b0 + Vector3.up * h * 0.7f + outward * (0.16f + Rn() * 0.1f) + Vector3.down * 0.05f : b0 + Vector3.up * h + new Vector3(Rn() - 0.5f, 0f, Rn() - 0.5f) * 0.06f;
                    Sweep(lv2, lt2, new[] { b0, Vector3.Lerp(b0, tip, 0.5f) + Vector3.up * 0.05f, tip }, 0.006f, false, 4);
                    // the leaves: two to four soft blades round the tip
                    int nl = 2 + (i % 3);
                    for (int kk = 0; kk < nl; kk++)
                    {
                        float a = kk * 360f / nl + i * 37f; var d = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                        Ellipsoid(lv2, lt2, tip + d * 0.035f + Vector3.down * 0.01f * kk, new Vector3(0.022f + Mathf.Abs(d.x) * 0.02f, 0.006f, 0.022f + Mathf.Abs(d.z) * 0.02f), 3, 6);
                        Ellipsoid(gv2, gt2, tip + d * 0.065f, Vector3.one * 0.006f, 2, 4);              // the glowing leaf tip
                    }
                    if (i % 5 == 2) { var c0 = tip + Vector3.up * 0.01f; var pts = new List<Vector3>(); for (int kk = 0; kk <= 8; kk++) { float a = kk * 0.7f; float r = 0.035f * (1f - kk / 10f); pts.Add(c0 + new Vector3(Mathf.Cos(a) * r, 0.02f + kk * 0.006f, Mathf.Sin(a) * r)); } Sweep(lv2, lt2, pts, 0.004f, false, 4); }   // an unfurling frond
                }
                foreach (var g in sk.Save("Seedlings_Mesh", seedlings)) g.transform.localPosition = Vector3.zero;
            }

            // ---------------------------------------------------------------- BIA_07: the garden, the wall with the remembered door, the seam, relay 2, the glass, the opening
            var gar = new GameObject("Garden").transform; gar.SetParent(world, false);
            var gk = new AK();
            // QF24 (12): the planters are Venus-vegetation beds now (BiaGarden, below)
            { var (v, t) = gk.Of(m.paleSoft); MBox(v, t, new Vector3(253f, 5f, 92.6f), new Vector3(34f, 10f, 1.2f), 0f, 0.3f); MBox(v, t, new Vector3(281f, 5f, 92.6f), new Vector3(6f, 10f, 1.2f), 0f, 0.3f); MBox(v, t, new Vector3(275f, 9f, 92.6f), new Vector3(6f, 2f, 1.2f), 0f, 0f); MBox(v, t, new Vector3(235.4f, 5f, 50.5f), new Vector3(0.8f, 10f, 19f), 0f, 0.3f); MBox(v, t, new Vector3(235.4f, 5f, 82f), new Vector3(0.8f, 10f, 20f), 0f, 0.3f); MBox(v, t, new Vector3(284.6f, 5f, 66f), new Vector3(0.8f, 10f, 52f), 0f, 0.3f); }
            var lamp2 = RelayPylon(m, B.Relay2At, B.Relay2View - B.Relay2At, Vector3.left, "Relay2_Lamp", gar);   // QF24 (10)
            var lampOp = Pedestal(gk, m, B.OpeningControl + new Vector3(0f, 0f, 0.4f), 1.2f, "OpeningControl_Lamp", gar);
            gk.Save("Bia_Garden", gar);
            BiaGarden(gar, m);                                                    // QF24 (12)
            var glassV = new List<Vector3>(); var glassT = new List<int>(); Quad(glassV, glassT, new Vector3(272f, 0f, 92.6f), new Vector3(272f, 8f, 92.6f), new Vector3(278f, 8f, 92.6f), new Vector3(278f, 0f, 92.6f)); Quad(glassV, glassT, new Vector3(278f, 0f, 92.62f), new Vector3(278f, 8f, 92.62f), new Vector3(272f, 8f, 92.62f), new Vector3(272f, 0f, 92.62f));
            MeshObject("GlassPartition", SaveMesh(Finish(glassV, glassT), "Bia_GlassPartition"), m.glass, gar, false);
            var seam = new GameObject("SeamPanel").transform; seam.SetParent(gar, false); seam.position = new Vector3(266.6f, 0f, 92.0f);
            { var sk = new AK(); var (v, t) = sk.Of(m.pale); MBox(v, t, new Vector3(0f, 1.3f, 0f), new Vector3(3.8f, 2.6f, 0.4f), 0f, 0.05f); var (gv, gt) = sk.Of(m.glowSoft); Sweep(gv, gt, new[] { new Vector3(-1.9f, 0.05f, -0.22f), new Vector3(-1.9f, 0.7f, -0.22f) }, 0.015f, false, 4); foreach (var g in sk.Save("SeamPanel_Mesh", seam)) g.transform.localPosition = Vector3.zero; }
            var upper = new GameObject("UpperShutter").transform; upper.SetParent(gar, false); upper.position = B.UpperOpening;
            { var sk = new AK(); var (v, t) = sk.Of(m.paleSoft); MBox(v, t, Vector3.zero, new Vector3(6f, 3.2f, 0.6f), 0f, 0.1f); foreach (var g in sk.Save("UpperShutter_Mesh", upper)) g.transform.localPosition = Vector3.zero; }
            var echoGarden = Echo("Echo_OldGarden", gar, m.echo, (v, t) =>
            {
                // the remembered doorway (an arch outline + its leaf) and the old garden's beds overlaid on the present one
                var d = B.EchoDoor + new Vector3(0f, -1.6f, -0.2f);
                Sweep(v, t, Enumerable.Range(0, 13).Select(i => { float a = i / 12f * Mathf.PI; return d + new Vector3(Mathf.Cos(a) * 1.3f, 2.4f + Mathf.Sin(a) * 0.8f, 0f); }).ToList(), 0.06f, false, 5);
                Sweep(v, t, new[] { d + new Vector3(-1.3f, 0f, 0f), d + new Vector3(-1.3f, 2.4f, 0f) }, 0.06f, false, 5); Sweep(v, t, new[] { d + new Vector3(1.3f, 0f, 0f), d + new Vector3(1.3f, 2.4f, 0f) }, 0.06f, false, 5);
                MBox(v, t, d + new Vector3(0f, 1.4f, 0.05f), new Vector3(2.4f, 2.8f, 0.04f), 0f, 0f);
                BiaEchoPlants(v, t);                                              // QF24 (12): translucent versions of the same plants
            });

            // ---------------------------------------------------------------- BIA_08: the quiet passage, the airlock pair, relay 3
            var pas = new GameObject("QuietPassage").transform; pas.SetParent(world, false);
            var pk2 = new AK();
            { var (v, t) = pk2.Of(m.dark); foreach (var z in new[] { 63.0f, 69.0f }) MBox(v, t, new Vector3(195f, 2.2f, z), new Vector3(20f, 4.4f, 0.8f), 0f, 0.1f); MBox(v, t, new Vector3(195f, 4.6f, 66f), new Vector3(20f, 0.4f, 6.8f), 0f, 0f); }   // the shielded tube
            { var (v, t) = pk2.Of(m.dark); foreach (var (a, b2) in new[] { (new Vector3(170f, 0f, 57.6f), new Vector3(186f, 0f, 57.6f)), (new Vector3(170f, 0f, 74.4f), new Vector3(186f, 0f, 74.4f)), (new Vector3(169.6f, 0f, 57.6f), new Vector3(169.6f, 0f, 74.4f)) }) { var d = b2 - a; MBox(v, t, (a + b2) * 0.5f + Vector3.up * 3f, new Vector3(Mathf.Abs(d.x) > 0.1f ? d.magnitude : 0.8f, 6f, Mathf.Abs(d.z) > 0.1f ? d.magnitude : 0.8f), 0f, 0.1f); } }
            { var (gv, gt) = pk2.Of(m.glowSoft); foreach (var z in new[] { 63.6f, 68.4f }) Sweep(gv, gt, new[] { new Vector3(186f, 0.3f, z), new Vector3(205f, 0.3f, z) }, 0.03f, false, 4); }
            var lampA = Pedestal(pk2, m, B.LockPanelA, 1.2f, "PanelA_Lamp", pas); var lampB = Pedestal(pk2, m, B.LockPanelB, 1.2f, "PanelB_Lamp", pas);
            var lamp3 = RelayPylon(m, B.Relay3At, B.Relay3View - B.Relay3At, Vector3.left, "Relay3_Lamp", pas);   // QF24 (10)
            /* QF24 (10): the old relay-3 housing removed (the pylon stands at Relay3At) */
            // QF24 (14): the loose coupling is its OWN object (it was baked into the passage's static mesh, so her telekinesis
            // could never move it): a short ribbed cylinder lying on the lock floor, lifted into the socket by the director
            var coupling = new GameObject("LockCoupling").transform; coupling.SetParent(pas, false); coupling.position = B.LockPanelB + new Vector3(-0.6f, 0.12f, -1.6f);
            {
                var cv = new List<Vector3>(); var ct = new List<int>();
                Append(cv, ct, RotZ(Lathe(new[] { new Vector2(0f, -0.3f), new Vector2(0.12f, -0.3f), new Vector2(0.12f, -0.22f), new Vector2(0.1f, -0.2f), new Vector2(0.1f, 0.2f), new Vector2(0.12f, 0.22f), new Vector2(0.12f, 0.3f), new Vector2(0f, 0.3f) }, 0f, 360f, 14), Vector3.zero));
                var cg = MeshObject("LockCoupling_Body", SaveMesh(SmoothFinish(cv, ct), "Bia_LockCoupling"), m.mid, coupling, false); cg.transform.localPosition = Vector3.zero;
                var gv = new List<Vector3>(); var gt = new List<int>(); Append(gv, gt, RotZ(Lathe(new[] { new Vector2(0.125f, -0.02f), new Vector2(0.13f, 0f), new Vector2(0.125f, 0.02f) }, 0f, 360f, 14), Vector3.zero));
                var glo = MeshObject("LockCoupling_Glow", SaveMesh(SmoothFinish(gv, gt), "Bia_LockCouplingGlow"), new Material(m.glowSoft) { name = "Bia_coupling_glow" }, coupling, false); glo.transform.localPosition = Vector3.zero;
            }
            pk2.Save("Bia_Passage", pas);
            Transform Door(string n, Vector3 at)
            {
                var d = new GameObject(n).transform; d.SetParent(pas, false); d.position = at;
                var dk = new AK(); var (v, t) = dk.Of(m.paleSoft); MBox(v, t, new Vector3(0f, 2.1f, 0f), new Vector3(0.4f, 4.2f, 5.4f), 0f, 0.05f); var (gv, gt) = dk.Of(m.glowSoft); Sweep(gv, gt, new[] { new Vector3(-0.22f, 0.2f, 0f), new Vector3(-0.22f, 4f, 0f) }, 0.02f, false, 4);
                foreach (var g in dk.Save(n + "_Mesh", d)) g.transform.localPosition = Vector3.zero; return d;
            }
            var passDoor = Door("PassageDoor", B.PassageDoor); var lockA = Door("LockDoorA", B.LockOuter); var lockB = Door("LockDoorB", B.LockInner);

            // ---------------------------------------------------------------- BIA_09: the transfer chamber (a tall shaft), platforms, partitions, stations, the compartment
            var ch = new GameObject("TransferChamber").transform; ch.SetParent(world, false);
            var chk = new AK();
            { var (v, t) = chk.Of(m.dark); Append(v, t, Lathe(new[] { new Vector2(34f, -2f), new Vector2(34f, 70f), new Vector2(35f, 70f), new Vector2(35f, -2f) }, 0f, 360f, 40, (a, y) => a > 330f || a < 20f || (a > 72f && a < 108f && y < 40f), new Vector3(175f, 0f, 125f))); }   // the shaft (open to the east: the road comes in)
            for (int i = 0; i < 4; i++) { var a = i * 90f + 45f; var c = new Vector3(175f, 0f, 125f) + Quaternion.Euler(0f, a, 0f) * Vector3.forward * 22f; var (v, t) = chk.Of(m.pale); Append(v, t, Lathe(new[] { new Vector2(0f, -0.6f), new Vector2(3.5f, -0.6f), new Vector2(3.8f, 0f), new Vector2(0f, 0f) }, 0f, 360f, 18, null, c + Vector3.up * (5f + i * 2f))); }   // service platforms (he perches)
            var stationLamps = new List<Renderer>(); for (int i = 0; i < 2; i++) stationLamps.Add(Pedestal(chk, m, B.PartitionStations[i], 1.3f, "Station" + (i + 1) + "_Lamp", ch));
            var barrierLamp = Pedestal(chk, m, B.BarrierControl, 1.3f, "Barrier_Lamp", ch);
            var routingLamp = Pedestal(chk, m, B.RoutingControl, 1.5f, "Routing_Lamp", ch);
            { var (v, t) = chk.Of(m.paleSoft); MBox(v, t, new Vector3(143.5f, 4f, 119.4f), new Vector3(15f, 8f, 0.8f), 0f, 0.1f); MBox(v, t, new Vector3(143.5f, 4f, 130.6f), new Vector3(15f, 8f, 0.8f), 0f, 0.1f); MBox(v, t, new Vector3(143.5f, 8.2f, 125f), new Vector3(15f, 0.4f, 12f), 0f, 0f); }   // the maintenance compartment
            { var (v, t) = chk.Of(m.dark); Sweep(v, t, Enumerable.Range(0, 13).Select(i => { float a = i / 12f * Mathf.PI; return B.ArchiveEntrance + new Vector3(Mathf.Cos(a) * 4.5f, 7f + Mathf.Sin(a) * 2.5f, -0.6f); }).ToList(), 0.5f, false, 8); }
            chk.Save("Bia_Chamber", ch);
            Transform Shutter(string n, Vector3 at, Vector3 size)
            {
                var d = new GameObject(n).transform; d.SetParent(ch, false); d.position = at;
                var dk = new AK(); var (v, t) = dk.Of(m.paleSoft); MBox(v, t, new Vector3(0f, size.y * 0.5f, 0f), size, 0f, 0.1f); var (gv, gt) = dk.Of(m.glowSoft); Sweep(gv, gt, new[] { new Vector3(-size.x * 0.5f, 0.2f, -size.z * 0.6f), new Vector3(size.x * 0.5f, 0.2f, -size.z * 0.6f) }, 0.05f, false, 4);
                foreach (var g in dk.Save(n + "_Mesh", d)) g.transform.localPosition = Vector3.zero; return d;
            }
            var parts = new[] { Shutter("ChamberPartition1", B.Partitions[0], new Vector3(46f, 14f, 0.8f)), Shutter("ChamberPartition2", B.Partitions[1], new Vector3(0.8f, 14f, 44f)) };
            // QF24 (19): the sealing barrier (and the compartment's other exit) has a clear viewing window - a curved Biavian pane
            // with a soft rim light - so the camera can hold on Ratwing inside after it closes
            Transform WindowShutter(string n, Vector3 at, Vector3 size)
            {
                var d = new GameObject(n).transform; d.SetParent(ch, false); d.position = at;
                var dk = new AK(); var (v, t) = dk.Of(m.paleSoft);
                float w = size.z, h = size.y, th = size.x, ow = 3.6f, oy0 = 1.3f, oy1 = 3.9f;
                MBox(v, t, new Vector3(0f, oy0 * 0.5f, 0f), new Vector3(th, oy0, w), 0f, 0.1f);                                       // below the window
                MBox(v, t, new Vector3(0f, (oy1 + h) * 0.5f, 0f), new Vector3(th, h - oy1, w), 0f, 0.1f);                              // above it
                float sw = (w - ow) * 0.5f;
                foreach (var sgn in new[] { -1f, 1f }) MBox(v, t, new Vector3(0f, (oy0 + oy1) * 0.5f, sgn * (ow * 0.5f + sw * 0.5f)), new Vector3(th, oy1 - oy0, sw), 0f, 0.1f);   // the sides
                var (gv, gt) = dk.Of(m.glowSoft);
                Sweep(gv, gt, new[] { new Vector3(-w * 0.5f, 0.2f, -th * 0.6f), new Vector3(w * 0.5f, 0.2f, -th * 0.6f) }, 0.05f, false, 4);
                // the rim light: a rounded loop just inside the opening, on both faces
                foreach (var fx in new[] { -th * 0.55f, th * 0.55f })
                {
                    var loop = new List<Vector3>();
                    for (int i = 0; i <= 40; i++) { float a = i / 40f * Mathf.PI * 2f; float cx = Mathf.Cos(a), cy = Mathf.Sin(a); float sx = Mathf.Sign(cx) * Mathf.Pow(Mathf.Abs(cx), 0.45f), sy = Mathf.Sign(cy) * Mathf.Pow(Mathf.Abs(cy), 0.45f); loop.Add(new Vector3(fx, (oy0 + oy1) * 0.5f + sy * (oy1 - oy0) * 0.47f, sx * ow * 0.47f)); }
                    Sweep(gv, gt, loop, 0.035f, false, 5);
                }
                foreach (var g in dk.Save(n + "_Mesh", d)) g.transform.localPosition = Vector3.zero;
                // the pane: a faint, slightly curved sheet (additive - it reads as glass, the room behind it stays clear)
                var pv = new List<Vector3>(); var pt = new List<int>();
                for (int iy = 0; iy <= 6; iy++) for (int iz = 0; iz <= 10; iz++) { float u = iz / 10f - 0.5f, vv = iy / 6f; pv.Add(new Vector3(-0.06f * Mathf.Cos(u * Mathf.PI), oy0 + vv * (oy1 - oy0), u * ow)); }
                for (int iy = 0; iy < 6; iy++) for (int iz = 0; iz < 10; iz++) { int a = iy * 11 + iz, b = a + 11; pt.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1, a, a + 1, b, a + 1, b + 1, b }); }
                var pane = MeshObject(n + "_WindowPane", SaveMesh(Finish(pv, pt), "Bia_" + n + "_Pane"), m.glass, d, false); pane.transform.localPosition = Vector3.zero;
                return d;
            }
            var outerB = WindowShutter("OuterBarrier", B.OuterBarrier, new Vector3(0.5f, 6f, 10.4f)); var innerB = WindowShutter("InnerBarrier", B.InnerBarrier, new Vector3(0.5f, 6f, 10.4f));
            var archDoor = Shutter("ArchiveDoor", B.ArchiveEntrance, new Vector3(8f, 7f, 0.6f));

            // ---------------------------------------------------------------- BIA_11: the memory chamber, the suspended structures, the octahedron
            var tem = new GameObject("MemoryChamber").transform; tem.SetParent(world, false);
            var tmk = new AK();
            { var (v, t) = tmk.Of(m.paleSoft); Append(v, t, Lathe(new[] { new Vector2(16f, 0f), new Vector2(16f, 16f), new Vector2(10f, 22f), new Vector2(0f, 23f) }, 0f, 360f, 36, (a, y) => a > 255f && a < 285f && y < 9f, new Vector3(175f, 0f, 170f))); }   // the dome (the doorway south)
            { var (v, t) = tmk.Of(m.stone); Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.42f, 0f), new Vector2(0.24f, 0.55f), new Vector2(0.3f, 0.86f), new Vector2(0f, 0.88f) }, 0f, 360f, 20, null, new Vector3(B.OctaAt.x, 0f, B.OctaAt.z))); /* QF23 (22): a slender plinth - the old 1.4 m-wide base kept her out of arm's reach */ }
            var shards = new List<Vector3>(); var shardT = new List<int>();
            for (int i = 0; i < 46; i++) { var a = i * 137.5f; float r = 3.5f + (i % 7) * 1.4f; var p = new Vector3(B.OctaAt.x, 0f, B.OctaAt.z) + Quaternion.Euler(0f, a, 0f) * Vector3.forward * r + Vector3.up * (3f + (i % 5) * 1.8f); if (i % 11 == 4) continue; Ellipsoid(shards, shardT, p, new Vector3(0.18f, 0.9f + (i % 3) * 0.4f, 0.18f), 3, 6); var (cv, ct) = tmk.Of(m.dark); Sweep(cv, ct, new[] { p + Vector3.up * 1f, p + Vector3.up * 14f }, 0.015f, false, 3); }
            tmk.Save("Bia_Temple", tem);
            MeshObject("MemoryStructures", SaveMesh(SmoothFinish(shards, shardT), "Bia_MemoryStructures"), AdditiveMat("Bia_memory_crystal", 0.55f), tem, false);
            PointLight("Temple_Light", new Vector3(175f, 12f, 170f), 34f, 2.4f, tem); PointLight("Octa_Light", B.OctaAt + Vector3.up * 2f, 10f, 2.4f, tem); PointLight("Temple_Fill", new Vector3(175f, 4f, 158f), 18f, 1.4f, tem);
            { var rk = new AK(); var (gv, gt) = rk.Of(m.glowSoft); foreach (var rr in new[] { 4f, 9f, 14f }) Sweep(gv, gt, Enumerable.Range(0, 49).Select(i => new Vector3(175f + Mathf.Cos(i / 48f * Mathf.PI * 2f) * rr, 0.04f, 170f + Mathf.Sin(i / 48f * Mathf.PI * 2f) * rr)).ToList(), 0.05f, true, 4); rk.Save("Bia_TempleRings", tem, false, false); }
            var em = ElarimMaterials(false);
            var octa = BuildOctahedronShape("MothershipRune", world, Seven.RuneSizes.OctaR, em, "Bia_Rune").transform; octa.position = B.OctaAt;
            var recordOcta = BuildOctahedronShape("MothershipRecordCopy", world, 0.06f, em, "Bia_RecordOcta").transform; recordOcta.gameObject.SetActive(false);
            // district lights (each relay's district: lights return when it reconnects)
            Renderer[] District(string n, Vector3 c, int count) { var l = new List<Renderer>(); for (int i = 0; i < count; i++) { var p = c + Quaternion.Euler(0f, i * 360f / count, 0f) * Vector3.forward * (14f + (i % 3) * 6f) + Vector3.up * (6f + (i % 4) * 3f); var lv = new List<Vector3>(); var lt = new List<int>(); Ellipsoid(lv, lt, p, new Vector3(0.6f, 0.4f, 0.6f), 4, 8); l.Add(MeshObject(n + i, SaveMesh(SmoothFinish(lv, lt), "Bia_" + n + i), new Material(m.glowSoft) { name = "Bia_" + n.ToLower() + i }, world, false).GetComponent<Renderer>()); } return l.ToArray(); }
            var d1 = District("District1Light", new Vector3(140f, 0f, 20f), 12); var d2 = District("District2Light", new Vector3(260f, 0f, 70f), 12); var d3 = District("District3Light", new Vector3(178f, 0f, 66f), 10);
            foreach (var (p, r2) in new[] { (new Vector3(0f, 9f, 10f), 30f), (new Vector3(140f, 9f, 0f), 30f), (new Vector3(260f, 9f, 66f), 34f), (new Vector3(190f, 5f, 66f), 16f), (new Vector3(175f, 14f, 125f), 42f), (new Vector3(175f, 9f, 170f), 22f) }) AstLamp("BiaLamp_" + (int)p.x + "_" + (int)p.z, p, r2, 1.1f, m.glow, m.dark, world, 0.3f);

            // ---------------------------------------------------------------- the saucer at the berth, ground, 7, camera, the bike
            var saucer = PlaceSaucer(world, B.SaucerAt);
            var beam = saucer.GetComponent<Seven.SaucerBeam>();
            CheckSaucer("Biaveh", saucer);
            var wg = new GameObject("WorldGround").AddComponent<Seven.WorldGround>();
            wg.Shape = Seven.Ground.Shape.Zones;
            wg.Zones = B.Zones.Select(z => new Seven.WorldGround.ZoneDef { Name = z.Name, Min = new Vector2(z.X0, z.Z0), Max = new Vector2(z.X1, z.Z1), Y0 = z.Y0, Y1 = z.Y1, Axis = z.Axis, Enabled = z.On }).ToArray();
            wg.ZoneObstacles = new Vector3[0];
            var seven = Spawn(SevenDir + "/7_character.fbx", "7", B.SevenDown, Vector3.forward, sevenCtl);
            var camGo = new GameObject("ChapterCamera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.55f, 0.55f, 0.55f);
            cam.fieldOfView = 54f; cam.nearClipPlane = 0.08f; cam.farClipPlane = 2400f; camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData(); camData.renderPostProcessing = true; camData.requiresDepthTexture = true; camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camGo.transform.position = B.SevenDown + new Vector3(-3f, 1.8f, -3f);
            var dirGo = new GameObject("BiavehDirector");
            var input = dirGo.AddComponent<Seven.ChapterInput>();
            var s7 = seven.AddComponent<Seven.ChapterSeven>(); s7.Input = input; s7.CameraTransform = camGo.transform;
            var ccam = camGo.AddComponent<Seven.ChapterCamera>(); ccam.Target = seven.transform; ccam.Input = input;
            var bikeRoot = new GameObject("HoverBike").transform; bikeRoot.position = B.BikePad;
            var visual = new GameObject("Visual").transform; visual.SetParent(bikeRoot, false);
            if (bike != null) { var bm = (GameObject)PrefabUtility.InstantiatePrefab(bike); bm.name = "BikeModel"; bm.transform.SetParent(visual, false); var bmat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Venus_hoverbike_v1.mat"); foreach (var r in bm.GetComponentsInChildren<Renderer>()) r.sharedMaterial = bmat; }
            var bp = LoadBikePoints();
            Transform P(string n, Vector3 lp) { var x = new GameObject(n).transform; x.SetParent(visual, false); x.localPosition = lp; return x; }
            var hb = bikeRoot.gameObject.AddComponent<Seven.HoverBike>();
            hb.Seat = P("Seat", bp.saddle); hb.GripL = P("GripL", bp.gripL); hb.GripR = P("GripR", bp.gripR); hb.FootL = P("FootL", bp.footL); hb.FootR = P("FootR", bp.footR);
            hb.Fairing = P("FairingL", new Vector3(bp.gripL.x - 0.05f, bp.gripL.y + 0.04f, bp.gripL.z - 0.25f)); hb.Visual = visual; hb.Length = bp.length;
            var vl = new GameObject("VentLight").AddComponent<Light>(); vl.transform.SetParent(visual, false); vl.transform.localPosition = new Vector3(0f, -0.15f, 0f); vl.type = LightType.Point; vl.range = 3f; vl.intensity = 0.4f; vl.shadows = LightShadows.None; hb.VentLight = vl;

            // ---------------------------------------------------------------- the Biavians (Kyle's design on Dan's rig): Nela, the resident, the crossers
            Seven.MarsFolk Folk(string n, Vector3 at, Vector3 face, float scale, float tone, int glyph)
            {
                if (bia == null) return null;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(bia); go.name = n;
                var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
                an.avatar = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(bia)).OfType<Avatar>().FirstOrDefault(); an.runtimeAnimatorController = biaCtl; an.applyRootMotion = false;
                var mt = biaMat; if (Mathf.Abs(tone - 1f) > 0.01f) { mt = new Material(biaMat) { name = "Bia_biavian_tone" + glyph }; mt.SetColor("_BaseColor", Gray(tone)); AssetDatabase.CreateAsset(mt, MatDir + "/Bia_biavian_tone" + glyph + ".mat"); }
                foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()) { r.sharedMaterial = mt; r.updateWhenOffscreen = true; }
                go.transform.localScale = Vector3.one * scale;
                // the medallion glyph varies per person (a small lit mark on the medallion)
                var chest = an.GetBoneTransform(HumanBodyBones.UpperChest) ?? an.GetBoneTransform(HumanBodyBones.Chest);
                if (chest != null)
                {
                    var gv = new List<Vector3>(); var gt = new List<int>();
                    for (int i = 0; i <= glyph % 3 + 1; i++) Ellipsoid(gv, gt, new Vector3((i - 1) * 0.012f, (i % 2) * 0.012f, 0f), Vector3.one * 0.006f, 3, 6);
                    var g = MeshObject(n + "_Glyph", SaveMesh(SmoothFinish(gv, gt), "Bia_Glyph_" + n), new Material(m.glowSoft) { name = "Bia_glyph_" + n.ToLower() }, chest, false);
                    g.transform.position = go.transform.position + new Vector3(-0.17f * scale, 1.36f * scale, 0.16f * scale); g.transform.rotation = go.transform.rotation;
                }
                go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(face));
                var f = go.AddComponent<Seven.MarsFolk>(); f.Role = "biavian"; f.WalkSpeed = 1.15f; f.CalmSway = true;
                return f;
            }
            var nela = Folk("Nela", B.NelaAt, Vector3.back, 1f, 1f, 0);
            var resA = Folk("ResidentA", B.ResidentAAt, Vector3.left, 0.94f, 0.82f, 1);
            var crossers = new[] { Folk("Resident_Seedlings", B.FarSide + new Vector3(2f, 0f, -2f), Vector3.left, 0.97f, 0.9f, 2), Folk("Resident_B", B.FarSide + new Vector3(3.4f, 0f, -0.8f), Vector3.left, 1.04f, 0.7f, 3), Folk("Resident_C", B.FarSide + new Vector3(4.8f, 0f, 0.4f), Vector3.left, 0.9f, 1.05f, 4) };
            if (crossers[0] != null) { seedlings.SetParent(crossers[0].Anim != null && crossers[0].Anim.GetBoneTransform(HumanBodyBones.RightHand) != null ? crossers[0].Anim.GetBoneTransform(HumanBodyBones.RightHand) : crossers[0].transform, true); seedlings.position = crossers[0].transform.position + crossers[0].transform.forward * 0.35f + Vector3.up * 1.0f; }
            if (nela == null) Log("WARNING: the Biavian Mixamo body is missing - Art/Mixamo/biavian/biavian_body_mixamo.fbx");

            // ---------------------------------------------------------------- Ratwing (the Round 4 model on its rig)
            var rwRoot = new GameObject("Ratwing"); var body = new GameObject("Body").transform; body.SetParent(rwRoot.transform, false);
            Seven.RatwingRig rrig = null;
            var rwModel = AssetDatabase.LoadAssetAtPath<GameObject>(RatwingFbx);
            if (rwModel != null)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(rwModel); model.name = "RatwingModel"; model.transform.SetParent(body, false);
                var tf = model.GetComponentsInChildren<Transform>(); Transform Bn(string n) => tf.FirstOrDefault(x => x.name == n);
                var head = Bn("head"); var eyesB = Bn("eyes");
                if (head != null && eyesB != null) { var fwd = rwRoot.transform.InverseTransformDirection(eyesB.position - head.position); fwd.y = 0f; float yaw = Vector3.SignedAngle(fwd, Vector3.forward, Vector3.up); model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * model.transform.localRotation; }
                var anim = model.GetComponent<Animator>() ?? model.AddComponent<Animator>(); anim.runtimeAnimatorController = ratCtl; anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var skin = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Ratwing_skin.mat") ?? m.mid; var eyeM = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Ratwing_eyes.mat") ?? m.glow;
                foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>()) { smr.updateWhenOffscreen = true; bool isEyes = smr.sharedMaterials.Any(mm => mm != null && mm.name.ToLower().Contains("eye")) || smr.name.Contains("eyes"); smr.sharedMaterials = smr.sharedMaterials.Select(_ => isEyes ? eyeM : skin).ToArray(); }
                rrig = rwRoot.AddComponent<Seven.RatwingRig>(); rrig.Anim = anim; rrig.Body = body;
            }
            else Log("WARNING: Ratwing's model is missing - " + RatwingFbx);
            rwRoot.transform.position = new Vector3(B.RatwingPerch.x, 0f, B.RatwingPerch.z);

            // ---------------------------------------------------------------- QF23 (10): Panther's cuts = the REAL saucer interior
            // (the old inset was a 3 m disc in the level's fog: an all-white void)
            var cut = PlaceAboardCut(world, new Vector3(0f, -300f, 0f));
            Transform insetCam = null, insetCat = null; Seven.CatPoseOverlay insetPose = null;

            // ---------------------------------------------------------------- the guide light, UI, dialogue, the director
            var gl0 = new List<Vector3>(); var glt = new List<int>(); Ellipsoid(gl0, glt, Vector3.zero, Vector3.one * 0.5f, 10, 14);
            var guideLight = MeshObject("GuideLight", SaveMesh(SmoothFinish(gl0, glt), "Bia_GuideLight"), m.glow, world, false);
            PointLight("GuideLightLamp", Vector3.zero, 24f, 3f, guideLight.transform).transform.localPosition = Vector3.zero;
            guideLight.SetActive(false);
            var ui = new GameObject("ChapterUI").AddComponent<Seven.ChapterUI>(); ui.Cam = cam;
            var dlg = dirGo.AddComponent<Seven.DialoguePlayer>(); dlg.Bank = bank; dlg.UI = ui;
            var legend = new GameObject("ControlsLegend").AddComponent<Seven.ControlsLegend>(); legend.Cam = cam; legend.Set = Seven.ControlsLegend.Layout.Chapter; legend.HidePanels = true;
            var dir = dirGo.AddComponent<Seven.BiavehDirector>();
            dir.Seven = s7; dir.Cam = ccam; dir.Input = input; dir.Dlg = dlg; dir.UI = ui; dir.Legend = legend; dir.Bike = hb; dir.SaucerBeam = beam; dir.Saucer = saucer; dir.Hold = saucer.GetComponent<Seven.SaucerHold>();
            dir.Atmo = atmo.GetComponent<Seven.Atmosphere>(); dir.GuideLight = guideLight.transform;
            dir.Nela = nela; dir.ResidentA = resA; dir.Crossers = crossers.Where(c => c != null).ToArray(); dir.Seedlings = seedlings; dir.Ratwing = rwRoot.transform; dir.RRig = rrig;
            dir.EchoBerth = echoBerth; dir.RouteDisplay = disp; dir.RouteMarks = marks.ToArray(); dir.RouteLine = routeLine;
            BiaDetail(world, m);                                                  // QF24 (16): purposeful detail across the mothership
            dir.Relay1Lamp = lamp1; dir.Cabinet1Door = cabDoor; dir.BridgeSegments = segs.ToArray(); dir.SegmentParked = parked.ToArray(); dir.SegmentAligned = aligned.ToArray(); dir.EchoBridge = echoBridge; dir.BridgeLever = lever; dir.PartitionA = partA; dir.DistrictLights1 = d1;
            dir.EchoGarden = echoGarden; dir.SeamPanel = seam; dir.Relay2Lamp = lamp2; dir.UpperShutter = upper; dir.OpeningLamp = lampOp; dir.DistrictLights2 = d2;
            dir.LockCoupling = coupling; dir.PassageDoor = passDoor; dir.LockDoorA = lockA; dir.LockDoorB = lockB; dir.PanelALamp = lampA; dir.PanelBLamp = lampB; dir.Relay3Lamp = lamp3; dir.DistrictLights3 = d3;
            dir.ChamberPartitions = parts; dir.StationLamps = stationLamps.ToArray(); dir.OuterBarrier = outerB; dir.InnerBarrier = innerB; dir.BarrierLamp = barrierLamp;
            dir.ArchiveDoor = archDoor; dir.RoutingLamp = routingLamp; dir.Octa = octa; dir.RecordOcta = recordOcta; dir.CityLights = cityLights.ToArray();
            dir.DebrisMeshes = BiavianDebrisMeshes(); dir.DebrisMat = m.pale;   // QF23 (18): Biavian debris, not the asteroid's gears
            dir.InsetCam = insetCam; dir.InsetCat = insetCat; dir.InsetPose = insetPose;
            dir.Vision6 = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Video/vision_06_mothership.mp4");
            AddProbe(new Vector3(0f, 10f, 0f), new Vector3(50f, 24f, 50f), Vector3.zero);
            AddProbe(new Vector3(140f, 10f, 0f), new Vector3(140f, 40f, 60f), Vector3.zero);
            AddProbe(new Vector3(260f, 6f, 66f), new Vector3(60f, 20f, 60f), Vector3.zero);
            AddProbe(new Vector3(175f, 14f, 140f), new Vector3(80f, 40f, 100f), Vector3.zero);
            QF18SceneChecks("Biaveh", true, () => { foreach (var s in segs) s.localRotation = Quaternion.identity; }, () => { for (int i = 0; i < segs.Count; i++) segs[i].localRotation = Quaternion.Euler(parked[i]); });
            for (int i = 0; i < segs.Count; i++) segs[i].localRotation = Quaternion.Euler(parked[i]);
            EditorSceneManager.SaveScene(scene, BiavehScenePath);
            Log("scene saved " + BiavehScenePath + $" (Nela {(nela != null ? "on the Biavian rig" : "MISSING")}, Ratwing {(rrig != null ? "rigged" : "MISSING")}, vision 6 {(dir.Vision6 != null ? "in" : "MISSING - the still fallback")})");
        }

        /// <summary>Round 17: an octahedron record (the rune / its small copy) in the Elarim stone, its 12 edges faintly lit.</summary>
        static GameObject BuildOctahedronShape(string name, Transform parent, float radius, ElarimMats em, string meshPrefix)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            MeshObject("Body", SaveMesh(Seven.Octahedron.Mesh(radius), meshPrefix + "_Body"), em.stone, root.transform);
            var ev = new List<Vector3>(); var et = new List<int>();
            foreach (var (a, b) in Seven.Octahedron.Edges) Sweep(ev, et, new[] { Seven.Octahedron.Vertex(a, radius) * 1.002f, Seven.Octahedron.Vertex(b, radius) * 1.002f }, radius * 0.025f, false, 5);
            MeshObject("Edges", SaveMesh(SmoothFinish(ev, et), meshPrefix + "_Edges"), em.edges, root.transform, false);
            return root;
        }
    }
}
