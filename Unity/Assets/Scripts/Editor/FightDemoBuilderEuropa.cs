using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using E = Seven.EuropaLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 14 — the Europa scene ("Europa — Beneath the Ice", Docs/CHAPTER_EUROPA.md), a first playable pass built from
    /// simple shapes on the shared systems: the ice field and the sheltered shelf with the saucer (the one shared exterior +
    /// the tractor beam, hovering), the ice passage down to the water, the great chamber under the ice (light through the
    /// ceiling cracks, bubble curtains, ancient structures with small white lights, vast shapes far below), the Cetacean
    /// settlement (platforms round open swimming rings, small vehicles, the receiving platform), the optional race (rings,
    /// carved arches, ice shelves, pillars), the damaged migration route, the industrial approach (pipes, intakes), Tunneler's
    /// base (dock, pressure chamber, corridor, control room + his screen, holding, transport dock, machinery, observation
    /// window over his private dock), the escape tunnel, the open water and the Cetacean temple with the icosahedron.
    /// Models: Art/Export/cetacean (whale = the keeper / the huge one / the pod, dolphin = the guide), Art/Export/tunneler,
    /// Scree (Chapter 1's), the hover bike, 7. Numbers: Seven.EuropaLayout. Atmosphere: Atmosphere_Europa (Kyle's fog / glow,
    /// under water: dark near silhouettes, a bright glowing misty distance).
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const string EuropaScenePath = "Assets/Scenes/Europa.unity";
        const string AtmoEuropaPath = AtmoDir + "/Atmosphere_Europa.prefab";

        static void BuildRound14(AnimatorController sevenCtl, GameObject atmoPrefab, Seven.VoiceBank bank)
        {
            var whale = SetupCetacean("whale");
            var dolphin = SetupCetacean("dolphin");
            var tun = SetupStaticModel("tunneler", "tunneler_v1.fbx");
            var bike = AssetDatabase.LoadAssetAtPath<GameObject>(CharRoot + "/hoverbike/hoverbike_v1.fbx");
            var atmo = BuildAtmosphereEuropa(atmoPrefab);
            CopyVision3();
            BuildEuropaScene(sevenCtl, bank, atmo, whale, dolphin, tun, bike);
        }

        static GameObject SetupCetacean(string kind)
        {
            var path = CopyExport("cetacean", kind + "_v1_rigged.fbx"); if (path == null) return null;
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            imp.animationType = ModelImporterAnimationType.Generic; imp.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            imp.importAnimation = false; imp.materialImportMode = ModelImporterMaterialImportMode.None; imp.SaveAndReimport();
            var mat = TexturedMat("Europa_" + kind, CharRoot + "/cetacean/Textures/" + kind + "_v1_basecolor.jpg", 0.55f);
            mat.SetFloat("_Rim", 0.35f); mat.SetFloat("_RimPower", 2.5f); EditorUtility.SetDirty(mat);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Log($"cetacean {kind}: {path} bones {go.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Spine") || t.name.StartsWith("Fin"))}");
            return go;
        }

        static void CopyVision3()
        {
            var src = Path.Combine(RepoRoot, "Art", "Video", "vision_03_europa.mp4");
            if (!File.Exists(src)) { Log("WARNING: Art/Video/vision_03_europa.mp4 missing - the still fallback plays"); return; }
            Directory.CreateDirectory(Path.Combine(ProjectRoot, "Assets/Video"));
            File.Copy(src, Path.Combine(ProjectRoot, "Assets/Video/vision_03_europa.mp4"), true);
            AssetDatabase.ImportAsset("Assets/Video/vision_03_europa.mp4", ImportAssetOptions.ForceSynchronousImport);
        }

        static GameObject BuildAtmosphereEuropa(GameObject atmoPrefab)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(atmoPrefab);
            var a = inst.GetComponent<Seven.Atmosphere>();
            // under the ice: strong haze, near shapes dark, the distance bright and glowing; light from above
            // (first pass was far too dense: shots 20 m away were white - now shapes read to ~80 m and fade into a glowing haze)
            a.FogStart = 12f;
            a.GroundDensity = 0.003f; a.GroundBase = -112f; a.GroundFalloff = 14f; a.GroundRange = 90f;
            a.ValleyDensity = 0f;
            a.DistanceDensity = 0.0035f; a.HazeHeight = 80f; a.MaxFog = 0.82f;
            a.NearBrightness = 0.08f; a.FarBrightness = 0.48f; a.BrightnessDistance = 160f; a.ZenithBrightness = 0.6f;
            a.NoiseStrength = 0.45f; a.NoiseScale = 0.05f; a.NoiseSpeed = 0.03f; a.NoiseFadeDistance = 70f;
            a.GlowIntensity = 0.32f; a.GlowRadius = 45f;
            var glows = new[]
            {
                (new Vector3(0f, -26f, 180f), new Vector3(4f, 1.2f, 1f)), (new Vector3(90f, -30f, 270f), new Vector3(3f, 1.2f, 1f)),
                (new Vector3(-60f, -30f, 240f), new Vector3(3.5f, 1.2f, 1f)), (new Vector3(-196f, -70f, 150f), new Vector3(3f, 2f, 1f)),
                (new Vector3(0f, 30f, -200f), new Vector3(8f, 2f, 1f)), (new Vector3(-150f, -60f, 420f), new Vector3(2f, 1.2f, 1f)),
                (new Vector3(40f, -100f, 160f), new Vector3(5f, 1.6f, 1f)), (new Vector3(-230f, -60f, 260f), new Vector3(3f, 1.6f, 1f)),
            };
            int gi = 0;
            foreach (Transform c in inst.transform)
            {
                if (c.name.StartsWith("Glow") && gi < glows.Length) { c.position = glows[gi].Item1; c.localScale = glows[gi].Item2; gi++; }
                if (c.name == "BackLight") { c.rotation = Quaternion.Euler(70f, 160f, 0f); var l = c.GetComponent<Light>(); l.intensity = 0.8f; l.shadowStrength = 0.4f; }
                if (c.name == "FrontFill") { c.rotation = Quaternion.Euler(75f, 20f, 0f); c.GetComponent<Light>().intensity = 0.35f; }
            }
            FogBudget(inst.GetComponent<Seven.Atmosphere>(), "Europa", 0.98f, 0.8f, 6f);   // QF28 pass: the QF26 fog-colour rule (<= ~1.0)
            var prefab = PrefabUtility.SaveAsPrefabAsset(inst, AtmoEuropaPath);
            Object.DestroyImmediate(inst);
            return prefab;
        }

        struct EuMats { public Material ice, iceDark, rock, metal, metalDark, glow, stone, stoneDark, pod, crack, shaft, glass, screen, water, bubble, panel; }

        // tunnels: a swept tube seen from inside (winding flipped so the normals face in)
        static void Inside(List<Vector3> v, List<int> t, IList<Vector3> path, float r, int sides = 14)
        {
            int t0 = t.Count; Sweep(v, t, path, r, false, sides);
            for (int i = t0; i < t.Count; i += 3) { int x = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = x; }
        }

        static void BuildEuropaScene(AnimatorController sevenCtl, Seven.VoiceBank bank, GameObject atmoP, GameObject whale, GameObject dolphin, GameObject tun, GameObject bike)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null; RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.3f, 0.3f, 0.3f);
            var shs = new SphericalHarmonicsL2(); shs.AddAmbientLight(RenderSettings.ambientLight); RenderSettings.ambientProbe = shs;
            var atmo = (GameObject)PrefabUtility.InstantiatePrefab(atmoP); atmo.name = "Atmosphere_Europa";
            var world = new GameObject("Europa").transform;
            var rnd = new System.Random(14);
            var m = new EuMats
            {
                ice = Dissolve("Europa_ice", Gray(0.86f), 0.7f, false), iceDark = Dissolve("Europa_ice_dark", Gray(0.55f), 0.6f, false),
                rock = Dissolve("Europa_rock", Gray(0.22f), 0.2f, false), metal = Dissolve("Europa_metal", Gray(0.38f), 0.6f, false, 0.5f),
                metalDark = Dissolve("Europa_metal_dark", Gray(0.14f), 0.5f, false, 0.4f), glow = Dissolve("Europa_glow", Color.white, 0.5f, false),
                stone = Dissolve("Europa_stone", Gray(0.72f), 0.35f, false), stoneDark = Dissolve("Europa_stone_dark", Gray(0.32f), 0.25f, false),
                pod = Dissolve("Europa_pod", Gray(0.62f), 0.7f, false, 0.3f), crack = Dissolve("Europa_crack", Color.white, 0.4f, false),
                shaft = AdditiveMatTex("Europa_shaft", 0.22f), glass = Dissolve("Europa_window", Gray(0.9f), 0.95f, false),
                screen = Dissolve("Europa_screen", Color.black, 0.2f, false), water = AdditiveMat("Europa_water_surface", 0.18f),
                bubble = AdditiveMatTex("Europa_bubble", 0.55f), panel = Dissolve("Europa_panel", Gray(0.3f), 0.4f, false),
            };
            foreach (var x in new[] { m.glow, m.crack }) { x.EnableKeyword("_EMISSION"); x.SetColor("_EmissionColor", Color.white * (x == m.crack ? 1.4f : 2.2f)); EditorUtility.SetDirty(x); }
            foreach (var x in new[] { m.ice, m.iceDark, m.rock, m.metal, m.metalDark, m.stone, m.stoneDark }) { x.SetFloat("_Cull", 0f); EditorUtility.SetDirty(x); }
            m.ice.EnableKeyword("_EMISSION"); m.ice.SetColor("_EmissionColor", Color.white * 0.04f); m.ice.SetFloat("_Rim", 0.4f); EditorUtility.SetDirty(m.ice);
            m.screen.EnableKeyword("_EMISSION"); m.screen.SetColor("_EmissionColor", Color.black); EditorUtility.SetDirty(m.screen);

            // ---------------------------------------------------------------- the surface: the ice field, fractures, the shelf
            {
                var v = new List<Vector3>(); var t = new List<int>();
                foreach (var (x0, z0, x1, z1) in new[] { (-600f, -600f, 600f, 7f), (-600f, 17f, 600f, 600f), (-600f, 7f, -6f, 17f), (6f, 7f, 600f, 17f) })
                    AddBox(v, t, new Vector3((x0 + x1) / 2f, -1f, (z0 + z1) / 2f), new Vector3(x1 - x0, 2f, z1 - z0));
                MeshObject("IceField", SaveMesh(Finish(v, t), "Europa_IceField"), m.ice, world);
                var cv = new List<Vector3>(); var ct = new List<int>();
                for (int i = 0; i < 46; i++)
                {
                    var p = new Vector3((float)rnd.NextDouble() * 900f - 450f, 0.02f, (float)rnd.NextDouble() * 900f - 300f);
                    if (Mathf.Abs(p.x) < 25f && p.z < 30f && p.z > -30f) continue;
                    var pts = new List<Vector3> { p }; float a = (float)rnd.NextDouble() * 360f;
                    for (int k = 0; k < 6; k++) { a += (float)rnd.NextDouble() * 50f - 25f; p += Quaternion.Euler(0f, a, 0f) * Vector3.forward * (10f + (float)rnd.NextDouble() * 20f); pts.Add(p); }
                    Sweep(cv, ct, pts, 0.18f + (float)rnd.NextDouble() * 0.25f, false, 4);
                }
                MeshObject("IceFractures", SaveMesh(Finish(cv, ct), "Europa_Fractures"), m.iceDark, world, false);
                // the shelf's sheltering ridges
                var rv = new List<Vector3>(); var rt = new List<int>();
                for (int i = 0; i < 18; i++) { float a = -60f + i * 7f; var c = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 24f, 0f, -Mathf.Cos(a * Mathf.Deg2Rad) * 24f - 4f); Lump(rv, rt, c, new Vector3(4f, 3f + (float)rnd.NextDouble() * 4f, 3f), 0.4f, rnd.Next(1000)); }
                MeshObject("ShelfRidges", SaveMesh(SmoothFinish(rv, rt), "Europa_ShelfRidges"), m.ice, world);
            }
            // ---------------------------------------------------------------- the ice passage (a tube) and its water surface
            {
                var v = new List<Vector3>(); var t = new List<int>();
                var path = new List<Vector3> { new Vector3(0f, 3f, 9f) }; path.AddRange(E.Dive); path.Add(new Vector3(0f, -40f, 128f));
                Inside(v, t, path, 7.2f, 18);
                MeshObject("IcePassage", SaveMesh(SmoothFinish(v, t), "Europa_Passage"), m.ice, world);
                var w = Block("WaterSurface", new Vector3(-8f, E.WaterY - 0.02f, 24f), new Vector3(8f, E.WaterY, 40f), m.water, world, false);
                w.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            // ---------------------------------------------------------------- the great chamber
            {
                var chamber = new GameObject("GreatChamber").transform; chamber.SetParent(world, false);
                Block("Ceiling", new Vector3(-320f, E.CeilingY, 100f), new Vector3(160f, E.CeilingY + 4f, 470f), m.ice, chamber, false);
                Block("Floor", new Vector3(-320f, -128f, 100f), new Vector3(160f, -124f, 470f), m.rock, chamber, false);
                // cracks in the ceiling with light through them, long shafts of light, curtains of rising bubbles
                var cv = new List<Vector3>(); var ct = new List<int>(); var sv = new List<Vector3>(); var st = new List<int>();
                for (int i = 0; i < 26; i++)
                {
                    var p = new Vector3((float)rnd.NextDouble() * 300f - 160f, E.CeilingY - 0.05f, 120f + (float)rnd.NextDouble() * 220f);
                    var q = p + new Vector3((float)rnd.NextDouble() * 40f - 20f, 0f, (float)rnd.NextDouble() * 40f - 20f);
                    Sweep(cv, ct, new[] { p, (p + q) * 0.5f + Vector3.right * 3f, q }, 0.35f, false, 4);
                    if (i % 2 == 0) { var lv = new List<Vector2> { new Vector2(0.8f, 0f), new Vector2(7f, -60f) }; var sh = Lathe(lv, 0f, 360f, 12, null, p); sv.AddRange(sh.vertices.Select(x => x)); int b = sv.Count - sh.vertexCount; st.AddRange(sh.triangles.Select(x => x + b)); }
                }
                MeshObject("CeilingCracks", SaveMesh(Finish(cv, ct), "Europa_CeilingCracks"), m.crack, chamber, false);
                var shafts = MeshObject("LightShafts", SaveMesh(Finish(sv, st), "Europa_Shafts"), m.shaft, chamber, false); shafts.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                for (int i = 0; i < 7; i++)
                {
                    var bc = new GameObject($"BubbleCurtain{i}").AddComponent<ParticleSystem>(); bc.transform.SetParent(chamber, false);
                    bc.transform.position = new Vector3(-120f + i * 40f, -110f, 150f + (i % 3) * 60f);
                    var mn = bc.main; mn.startLifetime = 24f; mn.startSpeed = 3.5f; mn.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.35f); mn.maxParticles = 600; mn.startColor = new Color(1f, 1f, 1f, 0.6f);
                    var sh2 = bc.shape; sh2.shapeType = ParticleSystemShapeType.Box; sh2.scale = new Vector3(14f, 0.5f, 1.5f); sh2.rotation = new Vector3(-90f, 0f, 0f);
                    var em2 = bc.emission; em2.rateOverTime = 22f;
                    bc.GetComponent<ParticleSystemRenderer>().sharedMaterial = m.bubble;
                }
                // ancient structures descending into the depths, their windows and pathways marked by small white lights
                var av = new List<Vector3>(); var at2 = new List<int>(); var lv2 = new List<Vector3>(); var lt2 = new List<int>(); int ancN = 0;
                foreach (var (x, z, depth, r) in new[] { (-70f, 160f, 70f, 6f), (-30f, 250f, 85f, 8f), (60f, 160f, 60f, 5f), (120f, 210f, 80f, 7f), (-110f, 300f, 75f, 9f), (20f, 320f, 65f, 6f) })
                {
                    var prof = new List<Vector2> { new Vector2(0f, 0f), new Vector2(r, 0f), new Vector2(r * 0.8f, -depth * 0.4f), new Vector2(r * 1.1f, -depth * 0.7f), new Vector2(r * 0.4f, -depth), new Vector2(0f, -depth) };
                    // QF15: one object per structure (the ride camera cuts away the one in its way, not all six)
                    var mesh = Lathe(prof, 0f, 360f, 16, null, new Vector3(x, E.CeilingY, z)); MeshObject($"AncientStructure{ancN}", SaveMesh(Finish(new List<Vector3>(mesh.vertices), new List<int>(mesh.triangles)), $"Europa_Ancient{ancN}"), m.stoneDark, chamber, false); ancN++;
                    for (int i = 0; i < 18; i++) { float a = i * 47f, y = E.CeilingY - 6f - i * depth / 20f; AddBox(lv2, lt2, new Vector3(x, y, z) + Quaternion.Euler(0f, a, 0f) * Vector3.forward * (r * 0.92f), Vector3.one * 0.35f); }
                }
                MeshObject("AncientLights", SaveMesh(Finish(lv2, lt2), "Europa_AncientLights"), m.glow, chamber, false);
                // rock spires rising from the floor (silhouettes)
                var pv = new List<Vector3>(); var pt = new List<int>();
                for (int i = 0; i < 30; i++) { pv.Clear(); pt.Clear(); var c = new Vector3((float)rnd.NextDouble() * 300f - 160f, -124f, 120f + (float)rnd.NextDouble() * 230f); Lump(pv, pt, c + Vector3.up * 10f, new Vector3(5f, 14f + (float)rnd.NextDouble() * 18f, 5f), 0.5f, rnd.Next(1000)); MeshObject($"ChamberSpire{i}", SaveMesh(SmoothFinish(pv, pt), $"Europa_Spire{i}"), m.rock, chamber, false); }
            }
            // ---------------------------------------------------------------- the settlement + the race course
            var vehicles = new List<Transform>(); var rings = new List<Transform>(); var rivals = new List<Transform>();
            Transform registration;
            {
                var st = new GameObject("Settlement").transform; st.SetParent(world, false);
                var v = new List<Vector3>(); var t = new List<int>(); var gv = new List<Vector3>(); var gt = new List<int>();
                // platforms around open swimming passages (rings), broad chambers (half domes)
                int platN = 0;   // QF15: one object per platform group (the ride camera cuts away only the one in its way)
                foreach (var (x, y, z, r) in new[] { (100f, -66f, 300f, 10f), (120f, -58f, 270f, 8f), (84f, -72f, 318f, 9f), (130f, -70f, 316f, 12f) })
                {
                    v.Clear(); t.Clear();
                    var ring = new List<Vector3>(); for (int i = 0; i < 24; i++) { float a = i * 15f * Mathf.Deg2Rad; ring.Add(new Vector3(x + Mathf.Cos(a) * r, y, z + Mathf.Sin(a) * r)); }
                    Sweep(v, t, ring, 1.2f, true, 10);
                    for (int i = 0; i < 24; i += 3) AddBox(gv, gt, ring[i] + Vector3.up * 1.3f, Vector3.one * 0.3f);
                    var dome = Lathe(new[] { new Vector2(r * 1.6f, 0f), new Vector2(r * 1.4f, r * 0.8f), new Vector2(r * 0.6f, r * 1.3f), new Vector2(0f, r * 1.4f) }, 0f, 200f, 16, null, new Vector3(x + r * 2.6f, y - 6f, z + r * 1.5f));
                    int b = v.Count; v.AddRange(dome.vertices); t.AddRange(dome.triangles.Select(k => k + b));
                    MeshObject($"Platform{platN}", SaveMesh(SmoothFinish(v, t), $"Europa_Platform{platN}"), m.stone, st); platN++;
                }
                v.Clear(); t.Clear();
                // the receiving platform
                var rp = Lathe(new[] { new Vector2(0f, 0f), new Vector2(6f, 0f), new Vector2(6.4f, -1.2f), new Vector2(0f, -1.6f) }, 0f, 360f, 32, null, E.Receiving + Vector3.down * 2.2f);
                { int b = v.Count; v.AddRange(rp.vertices); t.AddRange(rp.triangles.Select(k => k + b)); }
                for (int i = 0; i < 12; i++) { float a = i * 30f * Mathf.Deg2Rad; AddBox(gv, gt, E.Receiving + new Vector3(Mathf.Cos(a) * 6.1f, -2.0f, Mathf.Sin(a) * 6.1f), Vector3.one * 0.25f); }
                MeshObject("ReceivingPlatform", SaveMesh(SmoothFinish(v, t), "Europa_Receiving"), m.stone, st);
                MeshObject("PlatformLights", SaveMesh(Finish(gv, gt), "Europa_PlatformLights"), m.glow, st, false);
                // small vehicles moving between the platforms
                for (int i = 0; i < 6; i++)
                {
                    // QF13 (10): believable small craft (rounded hull, canopy, running lights, a thruster glow) - not plain pods
                    var veh = SmallCraft($"Vehicle{i}", st, 3.2f, 1.4f, 1.0f, m, $"Europa_Vehicle{i}");
                    veh.position = new Vector3(80f + i * 9f, -62f + (i % 3) * 3f, 270f + (i % 2) * 30f); vehicles.Add(veh);
                }
                // the race: registration arch, rings, carved arches, ice shelves, great pillars
                var arch = new List<Vector3>(); var archT = new List<int>(); var ap = new List<Vector3>();
                for (int i = 0; i <= 12; i++) { float a = i / 12f * Mathf.PI; ap.Add(E.RaceRegistration + new Vector3(-Mathf.Cos(a) * 6f, -4f + Mathf.Sin(a) * 9f, 0f)); }
                Sweep(arch, archT, ap, 0.7f, false, 8);
                var reg = MeshObject("RaceRegistrationArch", SaveMesh(SmoothFinish(arch, archT), "Europa_RegArch"), m.stone, st, false); registration = reg.transform;
                Lamp("RegistrationLamp", E.RaceRegistration + Vector3.up * 5.2f, 12f, 1.6f, m.glow, st, new Vector3(0.6f, 0.6f, 0.6f));
                for (int g = 0; g < E.RaceGates.Length; g++)
                {
                    var c = E.RaceGates[g]; var nx = E.RaceGates[(g + 1) % E.RaceGates.Length]; var dir = (nx - c).normalized;
                    var rv = new List<Vector3>(); var rt = new List<int>(); var rpts = new List<Vector3>();
                    var side = Vector3.Cross(Vector3.up, dir).normalized;
                    for (int i = 0; i < 20; i++) { float a = i * 18f * Mathf.Deg2Rad; rpts.Add(c + side * Mathf.Cos(a) * 5f + Vector3.up * Mathf.Sin(a) * 5f); }
                    Sweep(rv, rt, rpts, 0.35f, true, 8);
                    var ring = MeshObject($"RaceRing{g}", SaveMesh(SmoothFinish(rv, rt), $"Europa_RaceRing{g}"), m.crack, st, false); rings.Add(ring.transform);
                }
                var pil = new List<Vector3>(); var pilT = new List<int>();
                for (int pi = 0; pi < E.RacePillars.Length; pi++) { var mesh = Lathe(new[] { new Vector2(0f, -60f), new Vector2(4f, -60f), new Vector2(3f, 0f), new Vector2(4.2f, 40f), new Vector2(0f, 40f) }, 0f, 360f, 16, null, E.RacePillars[pi]); MeshObject($"RacePillar{pi}", SaveMesh(Finish(new List<Vector3>(mesh.vertices), new List<int>(mesh.triangles)), $"Europa_Pillar{pi}"), m.stoneDark, st, false); }
                var sv = new List<Vector3>(); var stt = new List<int>();
                AddBox(sv, stt, new Vector3(10f, -64f, 306f), new Vector3(30f, 2f, 18f)); AddBox(sv, stt, new Vector3(-8f, -72f, 262f), new Vector3(20f, 2f, 30f));   // ice shelves overhead
                MeshObject("RaceIceShelves", SaveMesh(Finish(sv, stt), "Europa_RaceShelves"), m.ice, st, false);
                // three rival vehicles (placeholders)
                for (int i = 0; i < 3; i++)
                {
                    var rv2 = SmallCraft($"Rival{i}", st, 3.0f, 1.2f, 0.8f, m, $"Europa_Rival{i}"); rv2.gameObject.SetActive(false); rivals.Add(rv2);
                }
            }
            // ---------------------------------------------------------------- the damaged migration route
            {
                var dmg = new GameObject("DamagedPassage").transform; dmg.SetParent(world, false);
                var v = new List<Vector3>(); var t = new List<int>(); var dv = new List<Vector3>(); var dt = new List<int>();
                Inside(v, t, new[] { E.DamagedPassage, E.DamagedPassage + new Vector3(20f, -6f, 30f), E.DamagedPassage + new Vector3(50f, -10f, 50f) }, 12f, 16);
                for (int i = 0; i < 9; i++) { var a = E.DamagedPassage + new Vector3((float)rnd.NextDouble() * 20f - 10f, -8f, (float)rnd.NextDouble() * 20f); Sweep(dv, dt, new[] { a, a + new Vector3((float)rnd.NextDouble() * 6f - 3f, 12f, (float)rnd.NextDouble() * 6f - 3f) }, 0.6f, false, 6); }
                for (int i = 0; i < 26; i++) Lump(dv, dt, E.DamagedPassage + new Vector3((float)rnd.NextDouble() * 24f - 12f, (float)rnd.NextDouble() * 16f - 8f, (float)rnd.NextDouble() * 24f), Vector3.one * (0.6f + (float)rnd.NextDouble() * 1.6f), 0.4f, rnd.Next(1000));
                MeshObject("PassageWalls", SaveMesh(SmoothFinish(v, t), "Europa_DamagedWalls"), m.rock, dmg);
                MeshObject("ShatteredSupportsDebris", SaveMesh(SmoothFinish(dv, dt), "Europa_Debris"), m.metalDark, dmg, false);
                MeshObject("SealedPassage", SaveMesh(Lathe(new[] { new Vector2(0f, 0f), new Vector2(11f, 0f), new Vector2(11f, 1.2f), new Vector2(0f, 1.2f) }, 0f, 360f, 24, null, E.DamagedPassage + new Vector3(20f, -6f, 30f)), "Europa_SealedPassage"), m.metal, dmg, false);
            }
            // ---------------------------------------------------------------- the industrial approach + the outside of the base
            var screeCtl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "/Scree.controller");
            var screeMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Scree_shell.mat") ?? Dissolve("Scree_shell", Gray(0.8f), 0.2f, false);
            GameObject Scree(string n, Vector3 at, Vector3 face, Transform parent)
            {
                GameObject go;
                if (File.Exists(Path.Combine(ProjectRoot, ScreeFbx)) && screeCtl != null) { go = Spawn(ScreeFbx, n, at, face, screeCtl); foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterial = screeMat; go.AddComponent<Seven.ScreeRig>(); }
                else { go = new GameObject(n); go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(face)); Block("Body", at + new Vector3(-0.35f, 0f, -0.25f), at + new Vector3(0.35f, 1.8f, 0.25f), screeMat, go.transform, false); }
                go.transform.SetParent(parent, true);
                return go;
            }
            {
                var ind = new GameObject("IndustrialApproach").transform; ind.SetParent(world, false);
                var v = new List<Vector3>(); var t = new List<int>(); var pv = new List<Vector3>(); var pt = new List<int>();
                Inside(v, t, new[] { E.Approach[2], E.Approach[3], E.Approach[4], E.Approach[5], E.Approach[6] }, 10f, 16);
                for (int k = 0; k < 4; k++) { var o = new Vector3((k % 2 == 0 ? -1 : 1) * 6.5f, -5f + (k / 2) * 9f, 0f); Sweep(pv, pt, new[] { E.Approach[1] + o, E.Approach[2] + o, E.Approach[3] + o, E.Approach[4] + o, E.Approach[5] + o }, 0.8f, false, 8); }
                for (int i = 0; i < 6; i++) { var c = Vector3.Lerp(E.Approach[2], E.Approach[5], i / 5f) + new Vector3(0f, -8f, 0f); AddBox(pv, pt, c, new Vector3(4f, 3f, 6f)); }
                MeshObject("ApproachTunnel", SaveMesh(SmoothFinish(v, t), "Europa_ApproachTunnel"), m.metalDark, ind);
                MeshObject("PipesAndIntakes", SaveMesh(SmoothFinish(pv, pt), "Europa_Pipes"), m.metal, ind, false);
                for (int i = 0; i < 8; i++) Lamp($"ApproachLamp{i}", Vector3.Lerp(E.Approach[2], E.Approach[6], i / 7f) + Vector3.up * 7f, 16f, 1.4f, m.glow, ind, new Vector3(0.8f, 0.2f, 0.4f));
                // workers labouring near the equipment (fatigue, coercion), a platform outside the bay
                Block("WorkPlatform", E.Approach[5] + new Vector3(-6f, -4.6f, -8f), E.Approach[5] + new Vector3(6f, -4f, -2f), m.metal, ind);
                for (int i = 0; i < 3; i++) { var w = Scree($"ScreeOutside{i}", E.Approach[5] + new Vector3(-4f + i * 4f, -4f, -5f), Vector3.forward, ind); }
            }
            // ---------------------------------------------------------------- the base (dry, on foot)
            var barriers = new List<GameObject>(); var baseLights = new List<Light>(); var transports = new List<Transform>(); var devSlots = new List<Transform>(); var panels = new List<Renderer>();
            GameObject doorHolding, doorTransport, doorMachine, shutter; Renderer screenR; Camera screenCam; Transform tunneler = null, drill = null, craft;
            var screeIn = new List<Transform>();
            {
                var bs = new GameObject("TunnelerBase").transform; bs.SetParent(world, false);
                float y = E.BaseY;
                foreach (var z in E.Zones) { if (z.Name == "Shelf") continue; Block($"Floor_{z.Name}", new Vector3(z.X0, y - 0.4f, z.Z0), new Vector3(z.X1, y, z.Z1), m.metal, bs); }
                // the outer shell: one box round the whole base (seen from inside), a ceiling at 3.6 m
                Block("BaseCeiling", new Vector3(-226f, y + 3.6f, 444f), new Vector3(-147f, y + 4.0f, 508f), m.metalDark, bs, false);
                var wallSegs = new List<(float x0, float z0, float x1, float z1)>();
                void Wall(string n, float x0, float z0, float x1, float z1) { Block(n, new Vector3(x0, y, z0), new Vector3(x1, y + 3.6f, z1), m.metalDark, bs); wallSegs.Add((x0, z0, x1, z1)); }
                // room walls (doors are gaps; the doors themselves are separate blocks)
                Wall("W_DockW", -183.6f, 444f, -183f, 456f); Wall("W_DockE", -176f, 453.5f, -175.4f, 456f);
                Wall("W_PressW", -183.6f, 456f, -183f, 463f); Wall("W_PressE", -179f, 456f, -178.4f, 463f);
                Wall("W_CorrW", -183.6f, 463f, -183f, 480.4f); Wall("W_CorrE", -179f, 463f, -178.4f, 480.4f);
                Wall("W_CtrlS_W", -191.6f, 479.8f, -183f, 480.4f); Wall("W_CtrlS_E", -179f, 479.8f, -170.4f, 480.4f);
                Wall("W_CtrlN_W", -191.6f, 493f, -183f, 493.6f); Wall("W_CtrlN_E", -179f, 493f, -170.4f, 493.6f);
                Wall("W_CtrlW_S", -191.6f, 480.4f, -191f, 486.4f); Wall("W_CtrlW_N", -191.6f, 489.6f, -191f, 493.6f);
                Wall("W_CtrlE_S", -171f, 480.4f, -170.4f, 486.4f); Wall("W_CtrlE_N", -171f, 489.6f, -170.4f, 493.6f);
                Wall("W_HoldS", -209f, 481f, -191.6f, 481.6f); Wall("W_HoldN", -209f, 495f, -191.6f, 495.6f);
                Wall("W_HoldW_S", -209.2f, 481.6f, -208.6f, 486.4f); Wall("W_HoldW_N", -209.2f, 489.6f, -208.6f, 495f);
                Wall("W_TransS", -224.6f, 481f, -209.2f, 481.6f); Wall("W_TransN", -224.6f, 495f, -209.2f, 495.6f);
                Wall("W_MachS", -170.4f, 480f, -148.4f, 480.6f); Wall("W_MachN", -170.4f, 499f, -148.4f, 499.6f); Wall("W_MachE", -149f, 480f, -148.4f, 499.6f);
                Wall("W_WinW", -183.6f, 493.6f, -183f, 504.5f); Wall("W_WinE", -179f, 493.6f, -178.4f, 504.5f);
                // QF28 pass (the image gate, QF26): the base walls were plain dark slabs - the corridor and room shots had almost nothing
                // in them (edge density 4.5 %). Industrial detail on both faces of every wall: structural ribs every 1.6 m, a pipe run
                // at 2.9 m, a skirting strip, and a soft lit strip under the ceiling (white, as every light in the game).
                {
                    var dk = new AK(); var (wrv, wrt) = dk.Of(m.metal); var (wpv, wpt) = dk.Of(m.metalDark); var (wgv, wgt) = dk.Of(m.glow);
                    foreach (var (wx0, wz0, wx1, wz1) in wallSegs)
                    {
                        bool wAlongZ = (wz1 - wz0) > (wx1 - wx0); float wLen = wAlongZ ? wz1 - wz0 : wx1 - wx0; if (wLen < 1.2f) continue;
                        float wcx = (wx0 + wx1) * 0.5f, wcz = (wz0 + wz1) * 0.5f, wHalf = (wAlongZ ? wx1 - wx0 : wz1 - wz0) * 0.5f;
                        foreach (var wsg in new[] { -1f, 1f })
                        {
                            Vector3 WFace(float wu, float wh, float wout) => wAlongZ ? new Vector3(wcx + wsg * (wHalf + wout), y + wh, wz0 + wu) : new Vector3(wx0 + wu, y + wh, wcz + wsg * (wHalf + wout));
                            Vector3 WSz(float walong, float whgt, float wdepth) => wAlongZ ? new Vector3(wdepth, whgt, walong) : new Vector3(walong, whgt, wdepth);
                            for (float wu = 0.6f; wu < wLen - 0.3f; wu += 1.6f) MBox(wrv, wrt, WFace(wu, 1.75f, 0.06f), WSz(0.14f, 3.5f, 0.12f), 0f, 0f);
                            MBox(wrv, wrt, WFace(wLen * 0.5f, 2.9f, 0.16f), WSz(wLen - 0.2f, 0.16f, 0.16f), 0f, 0f);       // the pipe run
                            MBox(wpv, wpt, WFace(wLen * 0.5f, 0.12f, 0.05f), WSz(wLen - 0.1f, 0.24f, 0.1f), 0f, 0f);       // skirting
                            MBox(wgv, wgt, WFace(wLen * 0.5f, 3.42f, 0.05f), WSz(wLen - 0.6f, 0.05f, 0.06f), 0f, 0f);      // the lit strip
                        }
                    }
                    dk.Save("EuBaseWallDetail", bs, false, false);
                }
                doorHolding = Block("Door_Holding", new Vector3(-192.2f, y, 486.4f), new Vector3(-191.4f, y + 3.4f, 489.6f), m.metal, bs);
                doorTransport = Block("Door_Transport", new Vector3(-209.4f, y, 486.4f), new Vector3(-208.6f, y + 3.4f, 489.6f), m.metal, bs);
                doorMachine = Block("Door_Machine", new Vector3(-170.8f, y, 486.4f), new Vector3(-170f, y + 3.4f, 489.6f), m.metal, bs);
                barriers.Add(Block("Barrier_Corridor", new Vector3(-183f, y, 479.4f), new Vector3(-179f, y + 3.4f, 479.9f), m.metalDark, bs));
                foreach (var b in barriers) b.SetActive(false);
                for (int i = 0; i < 9; i++) { var lp = new[] { E.DockStep, E.PressureIn, new Vector3(-181f, y, 470f), E.ControlSpot, E.HoldingSpot, E.TransportDock, new Vector3(-160f, y, 490f), E.WindowSpot, new Vector3(-155f, y, 484f) }[i]; Lamp($"BaseLamp{i}", lp + Vector3.up * 3.4f, 9f, 1.5f, m.glow, bs, new Vector3(0.9f, 0.08f, 0.3f)); baseLights.Add(bs.Find($"BaseLamp{i}_Light").GetComponent<Light>()); }
                // the screen on the control room's north wall; Tunneler stands in a lit "studio" far below the level, filmed by its camera
                var rt = new RenderTexture(640, 400, 16) { name = "Europa_TunnelerScreen" };
                AssetDatabase.CreateAsset(rt, GenDir + "/Europa_TunnelerScreen.renderTexture");
                // the screen is unlit (it shows his picture, it is not lit by the room); dark when off (its renderer hidden)
                var scrMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Europa_screen_unlit.mat");
                if (scrMat == null) { scrMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(scrMat, MatDir + "/Europa_screen_unlit.mat"); }
                scrMat.shader = Shader.Find("Universal Render Pipeline/Unlit"); scrMat.SetTexture("_BaseMap", rt); scrMat.SetColor("_BaseColor", Color.white); EditorUtility.SetDirty(scrMat);
                Block("ScreenFrame", E.ScreenAt + new Vector3(-2.75f, -1.55f, 0f), E.ScreenAt + new Vector3(2.75f, 1.55f, 0.08f), m.metalDark, bs, false);
                var scr = Block("TunnelerScreen", E.ScreenAt + new Vector3(-2.6f, -1.4f, -0.04f), E.ScreenAt + new Vector3(2.6f, 1.4f, -0.02f), scrMat, bs, false); screenR = scr.GetComponent<Renderer>(); screenR.enabled = false;
                var studio = new GameObject("TunnelerStudio").transform; studio.SetParent(bs, false); studio.position = new Vector3(2000f, 400f, 0f);   // far away and high: no height fog, never seen
                if (tun != null)
                {
                    var tm = (GameObject)PrefabUtility.InstantiatePrefab(tun); tm.name = "Tunneler"; tm.transform.SetParent(studio, false); tm.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    tunneler = tm.transform; drill = tm.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "DrillHead");
                    var tmat = Dissolve("Europa_tunneler", Gray(0.5f), 0.35f, false); foreach (var r in tm.GetComponentsInChildren<Renderer>()) r.sharedMaterial = tmat;
                }
                var under = new GameObject("StudioLightFromBelow").AddComponent<Light>(); under.transform.SetParent(studio, false); under.transform.position = studio.position + new Vector3(0f, -0.4f, 1.6f); under.transform.rotation = Quaternion.Euler(-60f, 180f, 0f);
                under.type = LightType.Spot; under.range = 8f; under.spotAngle = 80f; under.intensity = 9f; under.shadows = LightShadows.Soft;
                var scg = new GameObject("ScreenCam"); scg.transform.SetParent(studio, false); scg.transform.position = studio.position + new Vector3(0f, 1.0f, -3.4f); scg.transform.LookAt(studio.position + Vector3.up * 1.7f);
                screenCam = scg.AddComponent<Camera>(); screenCam.targetTexture = rt; screenCam.clearFlags = CameraClearFlags.SolidColor; screenCam.backgroundColor = Color.black; screenCam.fieldOfView = 40f; screenCam.farClipPlane = 20f; screenCam.enabled = false;
                // panels: transport access, lift power, the schematic
                panels.Add(Block("Panel_Transport", E.TransportPanel + new Vector3(-0.3f, 1.0f, -0.4f), E.TransportPanel + new Vector3(0.3f, 1.8f, 0.4f), m.panel, bs, false).GetComponent<Renderer>());
                panels.Add(Block("Panel_Lift", E.LiftPanel + new Vector3(-0.5f, 1.0f, 0.1f), E.LiftPanel + new Vector3(0.5f, 1.8f, 0.3f), m.panel, bs, false).GetComponent<Renderer>());
                panels.Add(Block("Panel_Schematic", E.SchematicAt + new Vector3(-1.4f, 0.8f, 0.1f), E.SchematicAt + new Vector3(1.4f, 2.6f, 0.3f), m.panel, bs, false).GetComponent<Renderer>());
                Block("Panel_HoldingOverride", E.HoldingDoor + new Vector3(1.0f, 1.0f, -2.4f), E.HoldingDoor + new Vector3(1.4f, 1.8f, -2.0f), m.panel, bs, false);
                // the transports (they leave one by one through the west tunnel), the machinery, the device slots
                // QF13 (10): the evacuation transports are real vessels (a rounded hull, a canopy at the bow, lit windows, a
                // thruster ring at the stern), nose west, and they leave through a proper launch tunnel in the dock's west wall
                for (int i = 0; i < 3; i++) { var tr = SmallCraft($"Transport{i}", bs, 4.6f, 2.4f, 2.0f, m, $"Europa_Transport{i}", true); tr.position = new Vector3(-221.6f, y + 1.15f, 484.3f + i * 4f); tr.rotation = Quaternion.Euler(0f, -90f, 0f); transports.Add(tr); }
                {
                    // the launch tunnel: the west wall with a 13 m wide opening, a lit portal frame, the tube running 70 m west
                    float x0 = -224.6f, zA = 481.4f, zB = 494.6f, h = 3.4f;
                    Block("LaunchTube_Floor", new Vector3(x0 - 70f, y - 0.4f, zA), new Vector3(x0, y, zB), m.metal, bs, false);
                    Block("LaunchTube_Ceiling", new Vector3(x0 - 70f, y + h, zA - 0.6f), new Vector3(x0, y + h + 0.4f, zB + 0.6f), m.metalDark, bs, false);
                    Block("LaunchTube_WallS", new Vector3(x0 - 70f, y - 0.4f, zA - 0.6f), new Vector3(x0, y + h, zA), m.metalDark, bs, false);
                    Block("LaunchTube_WallN", new Vector3(x0 - 70f, y - 0.4f, zB), new Vector3(x0, y + h, zB + 0.6f), m.metalDark, bs, false);
                    Block("LaunchTube_End", new Vector3(x0 - 70.6f, y - 0.4f, zA - 0.6f), new Vector3(x0 - 70f, y + h + 0.4f, zB + 0.6f), m.metalDark, bs, false);
                    var fv = new List<Vector3>(); var ft = new List<int>();
                    AddBox(fv, ft, new Vector3(x0 + 0.1f, y + h - 0.15f, (zA + zB) / 2f), new Vector3(0.3f, 0.3f, zB - zA + 1.2f));
                    AddBox(fv, ft, new Vector3(x0 + 0.1f, y + h / 2f, zA - 0.3f), new Vector3(0.3f, h, 0.3f)); AddBox(fv, ft, new Vector3(x0 + 0.1f, y + h / 2f, zB + 0.3f), new Vector3(0.3f, h, 0.3f));
                    MeshObject("LaunchTube_PortalFrame", SaveMesh(Finish(fv, ft), "Europa_LaunchPortal"), m.metal, bs);
                    var lv = new List<Vector3>(); var lt = new List<int>();
                    for (float x = x0 - 4f; x > x0 - 70f; x -= 6f) { AddBox(lv, lt, new Vector3(x, y + h - 0.05f, zA + 0.4f), new Vector3(1.2f, 0.08f, 0.2f)); AddBox(lv, lt, new Vector3(x, y + h - 0.05f, zB - 0.4f), new Vector3(1.2f, 0.08f, 0.2f)); }
                    MeshObject("LaunchTube_StripLights", SaveMesh(Finish(lv, lt), "Europa_LaunchLights"), m.glow, bs, false);
                    for (int k = 0; k < 4; k++) PointLight($"LaunchTubeLight{k}", new Vector3(x0 - 8f - k * 18f, y + 2.6f, (zA + zB) / 2f), 14f, 1.2f, bs);
                }
                var mv = new List<Vector3>(); var mt = new List<int>();
                for (int i = 0; i < 6; i++) { var c = new Vector3(-165f + i * 3f, y, 490f); var mesh = Lathe(new[] { new Vector2(0f, 0f), new Vector2(1.2f, 0f), new Vector2(1.2f, 3.4f), new Vector2(0f, 3.4f) }, 0f, 360f, 14, null, c); int b = mv.Count; mv.AddRange(mesh.vertices); mt.AddRange(mesh.triangles.Select(k => k + b)); }
                MeshObject("Machinery", SaveMesh(Finish(mv, mt), "Europa_Machinery"), m.metal, bs);
                for (int i = 0; i < E.DeviceSpots.Length; i++)
                {
                    var c = E.DeviceSpots[i]; Block($"Connection{i}", c + new Vector3(-0.4f, 0.6f, -0.4f), c + new Vector3(0.4f, 1.6f, 0.4f), m.metalDark, bs);
                    var dev = Block($"Device{i}", c + new Vector3(-0.18f, 1.6f, -0.18f), c + new Vector3(0.18f, 1.85f, 0.18f), m.glow, bs, false); dev.SetActive(false); devSlots.Add(dev.transform);
                }
                // the observation window over his private dock; his craft and dock beyond the glass
                Block("ObservationGlass", new Vector3(-183f, y, 504.5f), new Vector3(-179f, y + 3.4f, 504.6f), GlassMat("Europa_window_glass", 0.1f), bs, false);
                shutter = Block("WindowShutter", new Vector3(-183f, y, 504.6f), new Vector3(-179f, y + 3.4f, 504.9f), m.metalDark, bs, false);
                Block("TunnelerDockFloor", E.TunnelerDock + new Vector3(-8f, -0.4f, -6f), E.TunnelerDock + new Vector3(8f, 0f, 8f), m.metal, bs, false);
                // QF13 (10): his escape craft is a real vessel (a long dark hull, a canopy, fins, a thruster glow) and it leaves
                // through its own docking passage north of his dock - a lit portal and a tube, never through a wall
                var crT = SmallCraft("TunnelerCraft", bs, 7.2f, 3.0f, 2.2f, m, "Europa_TunnelerCraft", true, true); crT.position = E.TunnelerDock + new Vector3(2f, 1.2f, 3f); craft = crT;
                PointLight("TunnelerDockLight", E.TunnelerDock + Vector3.up * 3f, 12f, 2f, bs);
                {
                    var dk = E.TunnelerDock; float z0 = dk.z + 8f, z1 = dk.z + 80f;
                    Block("DockPassage_Floor", new Vector3(dk.x - 4f, dk.y - 0.6f, z0), new Vector3(dk.x + 8f, dk.y - 0.2f, z1), m.metal, bs, false);
                    Block("DockPassage_Ceiling", new Vector3(dk.x - 4.6f, dk.y + 7f, z0), new Vector3(dk.x + 8.6f, dk.y + 7.4f, z1), m.metalDark, bs, false);
                    Block("DockPassage_WallW", new Vector3(dk.x - 4.6f, dk.y - 0.6f, z0), new Vector3(dk.x - 4f, dk.y + 7f, z1), m.metalDark, bs, false);
                    Block("DockPassage_WallE", new Vector3(dk.x + 8f, dk.y - 0.6f, z0), new Vector3(dk.x + 8.6f, dk.y + 7f, z1), m.metalDark, bs, false);
                    Block("DockPassage_End", new Vector3(dk.x - 4.6f, dk.y - 0.6f, z1), new Vector3(dk.x + 8.6f, dk.y + 7.4f, z1 + 0.6f), m.metalDark, bs, false);
                    var fv = new List<Vector3>(); var ft = new List<int>();
                    AddBox(fv, ft, new Vector3(dk.x + 2f, dk.y + 6.8f, z0), new Vector3(13.2f, 0.4f, 0.4f)); AddBox(fv, ft, new Vector3(dk.x - 4.3f, dk.y + 3.2f, z0), new Vector3(0.4f, 7.6f, 0.4f)); AddBox(fv, ft, new Vector3(dk.x + 8.3f, dk.y + 3.2f, z0), new Vector3(0.4f, 7.6f, 0.4f));
                    MeshObject("DockPassage_PortalFrame", SaveMesh(Finish(fv, ft), "Europa_DockPortal"), m.metal, bs);
                    var lv = new List<Vector3>(); var lt = new List<int>();
                    for (float z = z0 + 4f; z < z1; z += 7f) { AddBox(lv, lt, new Vector3(dk.x - 3.7f, dk.y + 6.6f, z), new Vector3(0.2f, 0.1f, 1.4f)); AddBox(lv, lt, new Vector3(dk.x + 7.7f, dk.y + 6.6f, z), new Vector3(0.2f, 0.1f, 1.4f)); }
                    MeshObject("DockPassage_StripLights", SaveMesh(Finish(lv, lt), "Europa_DockLights"), m.glow, bs, false);
                    for (int k = 0; k < 3; k++) PointLight($"DockPassageLight{k}", new Vector3(dk.x + 2f, dk.y + 5f, z0 + 10f + k * 22f), 16f, 1.2f, bs);
                }
                // the workers in the holding area
                for (int i = 0; i < 6; i++) screeIn.Add(Scree($"ScreeWorker{i}", new Vector3(-197.5f - 2.2f * (i % 3), E.BaseY, 491.6f + 1.6f * (i / 3)), Vector3.back, bs).transform);   // by the north wall, off the door line (z 488)
                // the access bay: the pool and the dock edge
                Block("BayPoolFloor", new Vector3(-182f, -101.5f, 442f), new Vector3(-162f, -101f, 458f), m.metalDark, bs, false);
                Block("BayWaterSurface", new Vector3(-176f, -94.35f, 444f), new Vector3(-164f, -94.3f, 456f), m.water, bs, false);
            }
            // ---------------------------------------------------------------- the escape tunnel, the open water, the temple
            Transform collapse, rune, recordIco, blast = null; ParticleSystem ruptures; var debris = new List<Transform>();
            var supports = new List<Transform>();
            {
                var esc = new GameObject("EscapeTunnel").transform; esc.SetParent(world, false);
                var v = new List<Vector3>(); var t = new List<int>();
                Inside(v, t, E.Escape, 7f, 16);
                MeshObject("EscapeWalls", SaveMesh(SmoothFinish(v, t), "Europa_EscapeWalls"), m.metalDark, esc);
                for (int i = 0; i < E.EscapeSupports.Length; i++)
                {
                    var c = E.EscapeSupports[i]; var sv = new List<Vector3>(); var stt = new List<int>();
                    Sweep(sv, stt, new[] { c + new Vector3(-5f, 4f, 1f), c, c + new Vector3(4f, -4.5f, -1f) }, 0.55f, false, 8);
                    supports.Add(MeshObject($"BrokenSupport{i}", SaveMesh(SmoothFinish(sv, stt), $"Europa_Support{i}"), m.metal, esc).transform);
                }
                for (int i = 0; i < E.Escape.Length; i += 2) Lamp($"EscapeLamp{i}", E.Escape[i] + Vector3.up * 5.5f, 14f, 1.2f, m.glow, esc, new Vector3(0.6f, 0.15f, 0.6f));
                var colv = new List<Vector3>(); var colt = new List<int>();
                for (int i = 0; i < 14; i++) Lump(colv, colt, E.EscapeMouth + new Vector3((float)rnd.NextDouble() * 10f - 5f, (float)rnd.NextDouble() * 10f - 5f, (float)rnd.NextDouble() * 6f + 3f), Vector3.one * (1.5f + (float)rnd.NextDouble() * 2f), 0.4f, rnd.Next(1000));
                var col = MeshObject("EntranceCollapsed", SaveMesh(SmoothFinish(colv, colt), "Europa_Collapse"), m.rock, esc); collapse = col.transform; col.SetActive(false);
                // QF13 (14): the blast front that chases her down the tunnel - a white-hot core filling the tube, a boiling
                // shell, a wide halo (faces the camera), its light (lights the tunnel and the obstacles ahead of it); debris
                {
                    var bl = new GameObject("EscapeBlast").transform; bl.SetParent(esc, false);
                    var cv = new List<Vector3>(); var ct = new List<int>(); Ellipsoid(cv, ct, Vector3.zero, new Vector3(5.2f, 5.2f, 3.4f), 12, 18);
                    MeshObject("BlastCore", SaveMesh(SmoothFinish(cv, ct), "Europa_BlastCore"), m.glow, bl, false);
                    var sv2 = new List<Vector3>(); var st2 = new List<int>(); Ellipsoid(sv2, st2, Vector3.zero, new Vector3(6.4f, 6.4f, 5.2f), 12, 18);
                    var shell = Finish(sv2, st2); shell.colors = shell.vertices.Select(p => new Color(1f, 1f, 1f, 0.35f + 0.3f * Mathf.PerlinNoise(p.x * 0.4f + 3f, p.y * 0.4f + p.z * 0.3f))).ToArray();
                    MeshObject("BlastShell", SaveMesh(shell, "Europa_BlastShell"), AdditiveMat("Europa_blast_shell", 0.9f), bl, false);
                    var hv2 = new List<Vector3>(); var ht2 = new List<int>(); for (int k = 0; k < 32; k++) { var a0 = Polar(16f, k * 11.25f, 0f); var a1 = Polar(16f, (k + 1) * 11.25f, 0f); Quad(hv2, ht2, Vector3.zero, new Vector3(a1.x, a1.z, 0f), new Vector3(a0.x, a0.z, 0f), Vector3.zero); }
                    var halo = Finish(hv2, ht2); halo.colors = halo.vertices.Select(p => new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, Mathf.Clamp01(p.magnitude / 16f)))).ToArray();
                    MeshObject("BlastHalo", SaveMesh(halo, "Europa_BlastHalo"), AdditiveMat("Europa_blast_halo", 1.4f), bl, false);
                    var bLight = new GameObject("BlastLight").AddComponent<Light>(); bLight.transform.SetParent(bl, false); bLight.type = LightType.Point; bLight.range = 48f; bLight.intensity = 0f; bLight.shadows = LightShadows.None;
                    bl.gameObject.SetActive(false); blast = bl;
                    var dg = new GameObject("EscapeDebris").transform; dg.SetParent(esc, false);
                    for (int i = 0; i < 16; i++) { var lv = new List<Vector3>(); var lt = new List<int>(); Lump(lv, lt, Vector3.zero, Vector3.one * (0.5f + (float)rnd.NextDouble() * 1.3f), 0.45f, rnd.Next(1000)); var lg = MeshObject($"Debris{i}", SaveMesh(SmoothFinish(lv, lt), $"Europa_EscDebris{i}"), i % 3 == 0 ? m.metalDark : m.rock, dg, false); lg.SetActive(false); debris.Add(lg.transform); }
                }
                ruptures = new GameObject("Ruptures").AddComponent<ParticleSystem>(); ruptures.transform.SetParent(esc, false);
                { var mn = ruptures.main; mn.playOnAwake = false; mn.loop = false; mn.startLifetime = 2.2f; mn.startSpeed = new ParticleSystem.MinMaxCurve(2f, 7f); mn.startSize = new ParticleSystem.MinMaxCurve(0.5f, 2.6f); mn.maxParticles = 2000; mn.startColor = new Color(1f, 1f, 1f, 0.8f);
                  var sh = ruptures.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 4f; var em = ruptures.emission; em.rateOverTime = 0f;
                  ruptures.GetComponent<ParticleSystemRenderer>().sharedMaterial = m.bubble; ruptures.Stop(); }
                // the temple: built for its inhabitants - broad passages, enormous spaces
                var tem = new GameObject("CetaceanTemple").transform; tem.SetParent(world, false);
                var tv = new List<Vector3>(); var tt = new List<int>();
                var hall = Lathe(new[] { new Vector2(46f, -20f), new Vector2(50f, 10f), new Vector2(36f, 34f), new Vector2(0f, 42f) }, 0f, 360f, 32, null, E.TempleCentre);
                { int b = tv.Count; tv.AddRange(hall.vertices); tt.AddRange(hall.triangles.Select(k => k + b)); }
                for (int i = 0; i < 8; i++) { float a = i * 45f; var c0 = E.TempleCentre + Quaternion.Euler(0f, a, 0f) * Vector3.forward * 30f; var ap = new List<Vector3>(); for (int k = 0; k <= 10; k++) { float u = k / 10f * Mathf.PI; ap.Add(c0 + Quaternion.Euler(0f, a, 0f) * new Vector3(-Mathf.Cos(u) * 12f, -18f + Mathf.Sin(u) * 30f, 0f)); } Sweep(tv, tt, ap, 1.6f, false, 10); }
                var temple = MeshObject("TempleHall", SaveMesh(SmoothFinish(tv, tt), "Europa_TempleHall"), m.stone, tem);
                Block("TempleFloor", E.TempleCentre + new Vector3(-46f, -21f, -46f), E.TempleCentre + new Vector3(46f, -19.6f, 46f), m.stoneDark, tem, false);
                for (int i = 0; i < 16; i++) { float a = i * 22.5f; Lamp($"TempleLight{i}", E.TempleCentre + Quaternion.Euler(0f, a, 0f) * Vector3.forward * 40f + Vector3.up * (i % 2 == 0 ? 4f : 16f), 18f, 1.1f, m.glow, tem, new Vector3(0.5f, 0.5f, 0.5f)); }
                // the rune: a large icosahedron on its pedestal, its edges faintly lit; the small copy she receives
                var em2 = ElarimMaterials(false);
                rune = BuildIcosahedronShape("EuropaRune", tem, 1.0f, em2, "Europa_Rune").transform; rune.position = E.RuneAt; rune.rotation = Quaternion.Euler(20f, 30f, 10f);
                MeshObject("RunePedestal", SaveMesh(Lathe(new[] { new Vector2(0f, -3.6f), new Vector2(1.6f, -3.6f), new Vector2(0.8f, -1.6f), new Vector2(1.2f, -1.2f), new Vector2(0f, -1.2f) }, 0f, 360f, 20, null, E.RuneAt), "Europa_RunePedestal"), m.stone, tem);
                PointLight("RuneLight", E.RuneAt + Vector3.up * 2.5f, 10f, 2.2f, tem);
                recordIco = BuildIcosahedronShape("EuropaRecordCopy", tem, 0.07f, em2, "Europa_RecordIco").transform; recordIco.gameObject.SetActive(false);
            }
            // ---------------------------------------------------------------- the saucer (hovering over the shelf) + the beam
            // QF13 (16): the ONE saucer prefab, unchanged - the beam for 7, the bike hold for the bike (QF13 5)
            var saucer = PlaceSaucer(world, E.SaucerAt);
            var beam = saucer.GetComponent<Seven.SaucerBeam>();
            CheckSaucer("Europa", saucer);

            // ---------------------------------------------------------------- ground, 7, the bike, the helmet, the Cetaceans
            var wg = new GameObject("WorldGround").AddComponent<Seven.WorldGround>();
            wg.Shape = Seven.Ground.Shape.Zones;
            wg.Zones = E.Zones.Select(z => new Seven.WorldGround.ZoneDef { Name = z.Name, Min = new Vector2(z.X0, z.Z0), Max = new Vector2(z.X1, z.Z1), Y0 = z.Y, Y1 = z.Y, Axis = 0, Enabled = z.On }).ToArray();
            wg.ZoneObstacles = new Vector3[0];
            var seven = Spawn(SevenDir + "/7_character.fbx", "7", E.SevenDown, Vector3.forward, sevenCtl);
            var camGo = new GameObject("ChapterCamera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            cam.fieldOfView = 54f; cam.nearClipPlane = 0.08f; cam.farClipPlane = 2500f; camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData(); camData.renderPostProcessing = true; camData.requiresDepthTexture = true; camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camGo.transform.position = E.SevenDown + new Vector3(0f, 1.8f, -3f);
            var dirGo = new GameObject("EuropaDirector");
            var input = dirGo.AddComponent<Seven.ChapterInput>();
            var s7 = seven.AddComponent<Seven.ChapterSeven>(); s7.Input = input; s7.CameraTransform = camGo.transform;
            var ccam = camGo.AddComponent<Seven.ChapterCamera>(); ccam.Target = seven.transform; ccam.Input = input;
            // the bubble helmet: a clear glass sphere riding her head
            // QF13 (6): the helmet reads in bright misty water too - Seven/Bubble (a bright fresnel rim, soft highlights and a
            // reflection streak, a faint veil over what is behind it) and a solid collar ring where it seals at her neck
            var glassMat = BubbleMat();
            var hv = new List<Vector3>(); var ht = new List<int>(); Ellipsoid(hv, ht, Vector3.zero, Vector3.one * 0.5f, 18, 28);
            var helmetSphere = MeshObject("BubbleHelmet", SaveMesh(SmoothFinish(hv, ht), "Europa_Helmet"), glassMat, world, false);
            helmetSphere.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var collarMat = Dissolve("Europa_helmet_collar", Gray(0.62f), 0.65f, false, 0.6f);
            var colV = new List<Vector3>(); var colT = new List<int>();
            Sweep(colV, colT, Enumerable.Range(0, 32).Select(i => new Vector3(Mathf.Cos(i / 32f * Mathf.PI * 2f) * 0.148f, 0f, Mathf.Sin(i / 32f * Mathf.PI * 2f) * 0.148f)).ToList(), 0.022f, true, 8);
            Sweep(colV, colT, Enumerable.Range(0, 32).Select(i => new Vector3(Mathf.Cos(i / 32f * Mathf.PI * 2f) * 0.142f, 0.026f, Mathf.Sin(i / 32f * Mathf.PI * 2f) * 0.142f)).ToList(), 0.01f, true, 6);
            var collar = MeshObject("HelmetCollar", SaveMesh(SmoothFinish(colV, colT), "Europa_HelmetCollar"), collarMat, world, false);
            var helmet = seven.AddComponent<Seven.BubbleHelmet>(); helmet.Sphere = helmetSphere.transform; helmet.Collar = collar.transform; helmet.Anim = seven.GetComponent<Animator>();

            var bikeRoot = new GameObject("HoverBike").transform; bikeRoot.position = E.BikeDown;
            var visual = new GameObject("Visual").transform; visual.SetParent(bikeRoot, false);
            if (bike != null) { var bm = (GameObject)PrefabUtility.InstantiatePrefab(bike); bm.name = "BikeModel"; bm.transform.SetParent(visual, false); var bmat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Venus_hoverbike_v1.mat"); foreach (var r in bm.GetComponentsInChildren<Renderer>()) r.sharedMaterial = bmat; }
            var bp = LoadBikePoints();
            Transform P(string n, Vector3 lp) { var x = new GameObject(n).transform; x.SetParent(visual, false); x.localPosition = lp; return x; }
            var hb = bikeRoot.gameObject.AddComponent<Seven.HoverBike>();
            hb.Seat = P("Seat", bp.saddle); hb.GripL = P("GripL", bp.gripL); hb.GripR = P("GripR", bp.gripR); hb.FootL = P("FootL", bp.footL); hb.FootR = P("FootR", bp.footR);
            hb.Fairing = P("FairingL", new Vector3(bp.gripL.x - 0.05f, bp.gripL.y + 0.04f, bp.gripL.z - 0.25f)); hb.Visual = visual; hb.Length = bp.length;
            var vl = new GameObject("VentLight").AddComponent<Light>(); vl.transform.SetParent(visual, false); vl.transform.localPosition = new Vector3(0f, -0.15f, 0f); vl.type = LightType.Point; vl.range = 3f; vl.intensity = 0.4f; vl.shadows = LightShadows.None; hb.VentLight = vl;
            var hl = new GameObject("Headlight").AddComponent<Light>(); hl.transform.SetParent(visual, false); hl.transform.localPosition = new Vector3(0f, 0.6f, 1.6f); hl.type = LightType.Spot; hl.spotAngle = 60f; hl.range = 30f; hl.intensity = 2.5f; hl.shadows = LightShadows.None;
            var bubbles = new GameObject("BikeBubbles").AddComponent<ParticleSystem>(); bubbles.transform.SetParent(bikeRoot, false); bubbles.transform.localPosition = new Vector3(0f, 0.4f, -1.5f);
            { var mn = bubbles.main; mn.startLifetime = 3f; mn.startSpeed = 1.5f; mn.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.16f); mn.simulationSpace = ParticleSystemSimulationSpace.World; mn.gravityModifier = -0.12f; mn.startColor = new Color(1f, 1f, 1f, 0.7f);
              var sh = bubbles.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 18f; sh.radius = 0.3f; sh.rotation = new Vector3(0f, 180f, 0f); var em = bubbles.emission; em.rateOverTime = 0f;
              bubbles.GetComponent<ParticleSystemRenderer>().sharedMaterial = m.bubble; }
            // QF13 (13): speed streaks - fine white motes rushing past the bike at speed (stretched along their motion)
            var streaks = new GameObject("BikeSpeedStreaks").AddComponent<ParticleSystem>(); streaks.transform.SetParent(world, false);
            { var mn = streaks.main; mn.startLifetime = 0.7f; mn.startSpeed = new ParticleSystem.MinMaxCurve(-38f, -26f); mn.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f); mn.simulationSpace = ParticleSystemSimulationSpace.World; mn.startColor = new Color(1f, 1f, 1f, 0.55f); mn.maxParticles = 600;
              var sh = streaks.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(9f, 5f, 2f); var em = streaks.emission; em.rateOverTime = 0f;
              var pr = streaks.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = m.bubble; pr.renderMode = ParticleSystemRenderMode.Stretch; pr.velocityScale = 0.05f; pr.lengthScale = 2f; }

            Seven.Cetacean MakeCetacean(GameObject model, string name, Vector3 at, float scale, string matName)
            {
                var go = new GameObject(name); go.transform.position = at;
                if (model != null)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(model); inst.name = name + "Model"; inst.transform.SetParent(go.transform, false); inst.transform.localScale = Vector3.one * scale;
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/" + matName + ".mat");
                    foreach (var r in inst.GetComponentsInChildren<SkinnedMeshRenderer>()) { r.sharedMaterial = mat; r.updateWhenOffscreen = true; }
                }
                else Block("Placeholder", at + new Vector3(-1f, -1f, -4f) * scale, at + new Vector3(1f, 1f, 4f) * scale, m.stone, go.transform, false);
                return go.AddComponent<Seven.Cetacean>();
            }
            var keeper = MakeCetacean(whale, "Keeper", E.KeeperAt, 0.62f, "Europa_whale");        // ~16 m, ancient and calm
            var huge = MakeCetacean(whale, "HugeOne", E.HugeOnePath0, 1.15f, "Europa_whale");      // ~30 m - its scale reads under the bike
            var guide = MakeCetacean(dolphin, "Guide", E.Receiving + new Vector3(3f, 1f, 2f), 1f, "Europa_dolphin");
            guide.Speed = 9f; keeper.Speed = 4f; huge.Speed = 5.5f; huge.Wave = 5f; keeper.Wave = 5f;
            var pod = new List<Seven.Cetacean>();
            for (int i = 0; i < 5; i++)
            {
                var c0 = new Vector3(60f + i * 22f, -70f - (i % 2) * 12f, 230f + i * 18f);
                var c = MakeCetacean(i % 2 == 0 ? whale : dolphin, $"Pod{i}", c0, i % 2 == 0 ? 0.45f : 1f, i % 2 == 0 ? "Europa_whale" : "Europa_dolphin");
                c.Points = Enumerable.Range(0, 5).Select(k => c0 + new Vector3(Mathf.Cos(k * 1.25f + i) * 30f, Mathf.Sin(k * 0.9f) * 6f, Mathf.Sin(k * 1.25f + i) * 30f)).ToArray();
                c.Speed = i % 2 == 0 ? 3f : 6f; pod.Add(c);
            }
            // the guide light (the way to go): a small soft white orb with its light
            // QF13 (9): a much brighter, bigger guide light that cuts through the fog: a bright core, a wide soft halo (a
            // camera-facing disc, scaled with distance by the director so it never shrinks to a dot) and a strong light
            var gl = new List<Vector3>(); var glt = new List<int>(); Ellipsoid(gl, glt, Vector3.zero, Vector3.one * 0.6f, 10, 14);
            var guideLight = MeshObject("GuideLight", SaveMesh(SmoothFinish(gl, glt), "Europa_GuideLight"), m.glow, world, false);
            { var hvv = new List<Vector3>(); var htt = new List<int>(); for (int k = 0; k < 24; k++) { var a0 = Polar(3.2f, k * 15f, 0f); var a1 = Polar(3.2f, (k + 1) * 15f, 0f); Quad(hvv, htt, Vector3.zero, new Vector3(a1.x, a1.z, 0f), new Vector3(a0.x, a0.z, 0f), Vector3.zero); }
              var halo = Finish(hvv, htt); halo.colors = halo.vertices.Select(p => new Color(1f, 1f, 1f, Mathf.Lerp(0.85f, 0f, Mathf.Clamp01(p.magnitude / 3.2f)))).ToArray();
              MeshObject("GuideHalo", SaveMesh(halo, "Europa_GuideHalo"), AdditiveMat("Europa_guide_halo", 1.6f), guideLight.transform, false); }
            PointLight("GuideLightLamp", Vector3.zero, 40f, 6f, guideLight.transform).transform.localPosition = Vector3.zero;
            guideLight.SetActive(false);

            var ui = new GameObject("ChapterUI").AddComponent<Seven.ChapterUI>(); ui.Cam = cam;
            var dlg = dirGo.AddComponent<Seven.DialoguePlayer>(); dlg.Bank = bank; dlg.UI = ui;
            var legend = new GameObject("ControlsLegend").AddComponent<Seven.ControlsLegend>(); legend.Cam = cam; legend.Set = Seven.ControlsLegend.Layout.Chapter; legend.HidePanels = true;
            var d = dirGo.AddComponent<Seven.EuropaDirector>();
            d.Seven = s7; d.Cam = ccam; d.Input = input; d.Dlg = dlg; d.UI = ui; d.Legend = legend; d.Bike = hb; d.SaucerBeam = beam; d.Saucer = saucer; d.Hold = saucer.GetComponent<Seven.SaucerHold>();
            d.Atmo = atmo.GetComponent<Seven.Atmosphere>(); d.Helmet = helmet; d.Keeper = keeper; d.Guide = guide; d.HugeOne = huge; d.Pod = pod.ToArray();
            d.Vehicles = vehicles.ToArray(); d.RaceRings = rings.ToArray(); d.Rivals = rivals.ToArray(); d.RaceRegistration = registration;
            d.Tunneler = tunneler; d.TunnelerHead = drill; d.TunnelerCraft = craft; d.ScreenCam = screenCam; d.Screen = screenR;
            d.Barriers = barriers.ToArray(); d.DoorHolding = doorHolding; d.DoorTransport = doorTransport; d.DoorMachine = doorMachine; d.WindowShutter = shutter;
            d.Scree = screeIn.ToArray(); d.Transports = transports.ToArray(); d.DeviceSlots = devSlots.ToArray(); d.BasePanels = panels.ToArray();
            d.EscapeSupports = supports.ToArray(); d.EscapeCollapse = collapse; d.Ruptures = ruptures; d.Bubbles = bubbles; hb.Streaks = streaks;
            d.EscapeBlast = blast; d.EscapeDebris = debris.ToArray();
            d.Rune = rune; d.RecordIco = recordIco; d.BaseLights = baseLights.ToArray(); d.GuideLight = guideLight.transform;
            d.Vision3 = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Video/vision_03_europa.mp4");
            AddProbe(new Vector3(0f, -60f, 220f), new Vector3(300f, 100f, 240f), Vector3.zero);
            AddProbe(new Vector3(-186f, E.BaseY + 1.8f, 476f), new Vector3(80f, 6f, 64f), Vector3.zero);
            PlaceAboardCut(new GameObject("Europa").transform, new Vector3(0f, -1500f, 0f));   // QF23 (10, 15a): Panther's cutaways = the real saucer interior
            QF18SceneChecks("Europa", false);   // QF18 (11, 13): report
            EditorSceneManager.SaveScene(scene, EuropaScenePath);
            Log("scene saved " + EuropaScenePath);
        }

        /// <summary>Round 14: clear glass (URP Lit, transparent, almost no tint - monochrome): the helmet, the observation window.</summary>
        static Material BubbleMat()
        {
            var path = MatDir + "/Europa_helmet_bubble.mat"; var sh = Shader.Find("Seven/Bubble");
            var g = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (g == null) { g = new Material(sh); AssetDatabase.CreateAsset(g, path); }
            g.shader = sh; g.SetFloat("_Veil", 0.1f); g.SetFloat("_VeilGray", 0.55f); g.SetFloat("_Rim", 1.5f); g.SetFloat("_RimPower", 2.6f); g.SetFloat("_Highlight", 1f);
            EditorUtility.SetDirty(g); return g;
        }

        /// <summary>QF13 (10): a small craft built from simple shapes in Kyle's style - a rounded hull (forward = +z), a canopy
        /// toward the bow, running lights along the sides, a thruster ring + glow at the stern, optional fins. Returns its root
        /// (meshes local, so it can be moved / turned as one).</summary>
        static Transform SmallCraft(string name, Transform parent, float len, float wid, float hgt, EuMats m, string meshName, bool windows = false, bool fins = false)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false);
            var hv = new List<Vector3>(); var ht = new List<int>();
            Ellipsoid(hv, ht, Vector3.zero, new Vector3(wid * 0.5f, hgt * 0.5f, len * 0.5f), 10, 16);
            Ellipsoid(hv, ht, new Vector3(0f, -hgt * 0.18f, -len * 0.08f), new Vector3(wid * 0.56f, hgt * 0.22f, len * 0.36f), 6, 12);       // a keel / skirt
            if (fins) foreach (var sx in new[] { -1f, 1f }) Ellipsoid(hv, ht, new Vector3(sx * wid * 0.55f, 0f, -len * 0.3f), new Vector3(wid * 0.28f, hgt * 0.06f, len * 0.16f), 4, 8);
            MeshObject(name + "_Hull", SaveMesh(SmoothFinish(hv, ht), meshName + "_Hull"), fins ? m.metalDark : m.pod, root, false);
            var cv = new List<Vector3>(); var ct = new List<int>();
            Ellipsoid(cv, ct, new Vector3(0f, hgt * 0.32f, len * 0.22f), new Vector3(wid * 0.3f, hgt * 0.26f, len * 0.2f), 8, 12);
            MeshObject(name + "_Canopy", SaveMesh(SmoothFinish(cv, ct), meshName + "_Canopy"), m.metalDark, root, false);
            var lv = new List<Vector3>(); var lt = new List<int>();
            int n = windows ? 5 : 2;
            for (int i = 0; i < n; i++) foreach (var sx in new[] { -1f, 1f })
                { float z = Mathf.Lerp(len * 0.22f, -len * 0.2f, n == 1 ? 0.5f : i / (float)(n - 1)); AddBox(lv, lt, new Vector3(sx * wid * 0.47f, hgt * 0.08f, z), new Vector3(0.05f, hgt * 0.09f, len * 0.05f)); }
            AddBox(lv, lt, new Vector3(0f, hgt * 0.05f, len * 0.5f), new Vector3(wid * 0.18f, hgt * 0.08f, 0.06f));                            // the bow light
            MeshObject(name + "_Lights", SaveMesh(Finish(lv, lt), meshName + "_Lights"), m.glow, root, false);
            var tv = new List<Vector3>(); var tt = new List<int>();
            Sweep(tv, tt, Enumerable.Range(0, 20).Select(i => new Vector3(Mathf.Cos(i / 20f * Mathf.PI * 2f) * wid * 0.22f, Mathf.Sin(i / 20f * Mathf.PI * 2f) * hgt * 0.22f, -len * 0.47f)).ToList(), Mathf.Max(0.04f, wid * 0.035f), true, 6);
            MeshObject(name + "_ThrusterRing", SaveMesh(SmoothFinish(tv, tt), meshName + "_Ring"), m.metal, root, false);
            var gv = new List<Vector3>(); var gt = new List<int>(); Ellipsoid(gv, gt, new Vector3(0f, 0f, -len * 0.5f), new Vector3(wid * 0.18f, hgt * 0.18f, len * 0.05f), 6, 10);
            MeshObject(name + "_ThrusterGlow", SaveMesh(SmoothFinish(gv, gt), meshName + "_Glow"), m.glow, root, false);
            var l = PointLight(name + "_Light", Vector3.zero, Mathf.Max(5f, len * 1.6f), 0.9f, root); l.transform.localPosition = new Vector3(0f, 0f, -len * 0.62f);
            return root;
        }

        static Material GlassMat(string name, float alpha)
        {
            var g = Lit(name, new Color(0.95f, 0.95f, 0.95f, alpha), 0.97f);
            g.SetColor("_BaseColor", new Color(0.95f, 0.95f, 0.95f, alpha));
            g.SetFloat("_Surface", 1f); g.SetFloat("_Blend", 0f); g.SetFloat("_ZWrite", 0f);
            g.SetOverrideTag("RenderType", "Transparent"); g.renderQueue = (int)RenderQueue.Transparent; g.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            g.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); g.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); g.EnableKeyword("_EMISSION"); g.SetColor("_EmissionColor", Color.white * 0.05f);
            EditorUtility.SetDirty(g); return g;
        }

        /// <summary>Round 14: an icosahedron record (the rune / its small copy) in the Elarim stone, its 30 edges faintly lit.</summary>
        static GameObject BuildIcosahedronShape(string name, Transform parent, float radius, ElarimMats em, string meshPrefix)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            MeshObject("Body", SaveMesh(Seven.Icosahedron.Mesh(radius), meshPrefix + "_Body"), em.stone, root.transform);
            var ev = new List<Vector3>(); var et = new List<int>();
            foreach (var (a, b) in Seven.Icosahedron.Edges()) Sweep(ev, et, new[] { Seven.Icosahedron.V[a] * radius * 1.002f, Seven.Icosahedron.V[b] * radius * 1.002f }, radius * 0.018f, false, 5);
            MeshObject("Edges", SaveMesh(SmoothFinish(ev, et), meshPrefix + "_Edges"), em.edges, root.transform, false);
            return root;
        }
    }
}
