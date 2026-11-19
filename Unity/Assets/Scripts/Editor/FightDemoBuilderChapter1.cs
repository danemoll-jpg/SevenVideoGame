using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using L = Seven.MoonLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 6: Chapter 1 "The Moon Prison". Voice bank (Assets/Audio/Voice), the Moon level scene
    /// (Assets/Scenes/Chapter1.unity: cell, catwalk over the cavern, older passage, ancient hall with the Scree guard,
    /// vision chamber, impound bay with 7's saucer, procedural saucer exterior), Atmosphere_Moon variant, the
    /// Scree_placeholder (gray hooded Y Bot driven by 7's Humanoid clips). Layout numbers live in Seven.MoonLayout.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const string Chapter1ScenePath = "Assets/Scenes/Chapter1.unity";
        const string VoiceDir = "Assets/Audio/Voice";
        const string VoiceBankPath = VoiceDir + "/VoiceBank.asset";
        const string AtmoMoonPath = AtmoDir + "/Atmosphere_Moon.prefab";

        // ------------------------------------------------------------------------------------ voices
        [Serializable] class JsonLine { public string id, dir, speaker, text; public int scene; public float pauseAfter; }
        [Serializable] class JsonLines { public JsonLine[] lines; }

        static Seven.VoiceBank BuildVoiceBank()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var json = File.ReadAllText(Path.Combine(ProjectRoot, VoiceDir, "voice_lines.json"));
            var data = JsonUtility.FromJson<JsonLines>(json);
            AssetDatabase.DeleteAsset(VoiceBankPath);
            var bank = ScriptableObject.CreateInstance<Seven.VoiceBank>();
            int withClip = 0;
            foreach (var l in data.lines)
            {
                var clipPath = $"{VoiceDir}/{l.dir}/{l.id}.mp3";
                var imp = AssetImporter.GetAtPath(clipPath) as AudioImporter;
                if (imp != null && !imp.forceToMono)
                {
                    imp.forceToMono = true;
                    var s = imp.defaultSampleSettings; s.loadType = AudioClipLoadType.DecompressOnLoad; imp.defaultSampleSettings = s;
                    imp.SaveAndReimport();
                }
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip != null) withClip++;
                bank.Lines.Add(new Seven.VoiceBank.Line { Id = l.id, Scene = l.scene, Speaker = l.speaker, Text = l.text, PauseAfter = l.pauseAfter, Clip = clip });
            }
            AssetDatabase.CreateAsset(bank, VoiceBankPath);
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            Log($"voice bank: {bank.Lines.Count} lines, {withClip} with audio");
            return bank;
        }

        // ------------------------------------------------------------------------------------ round 6
        struct MoonMats
        {
            public Material rock, rockDark, metal, metalDark, grate, ancient, ancientDark, glow, crate, field, ghost, hull, hullUnder, rim,
                inscription, scree, screeHood, black;
        }

        static void BuildRound6(AnimatorController sevenCtl, GameObject atmoPrefab, Seven.VoiceBank bank)
        {
            var catCtl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "/Cat.controller");
            var catFur = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Cat_fur.mat");
            var catWhisker = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Cat_whisker.mat");
            var m = new MoonMats
            {
                rock = Dissolve("Moon_rock", Gray(0.14f), 0.12f, false),
                rockDark = Dissolve("Moon_rock_dark", Gray(0.07f), 0.1f, false),
                metal = Dissolve("Moon_metal", Gray(0.34f), 0.6f, false, 0.5f),
                metalDark = Dissolve("Moon_metal_dark", Gray(0.16f), 0.5f, false, 0.4f),
                grate = Dissolve("Moon_grate", Gray(0.22f), 0.45f, false, 0.3f),
                ancient = Dissolve("Moon_ancient", Gray(0.46f), 0.3f, false),
                ancientDark = Dissolve("Moon_ancient_dark", Gray(0.27f), 0.25f, false),
                glow = Dissolve("Moon_lamp", Color.white, 0.5f, false),
                crate = Dissolve("Moon_crate", Gray(0.24f), 0.2f, false),
                // Round 10 (Dan): the exterior is polished silver (replaces the dark glossy hull from Kyle's art) - light-grey
                // base, high metallic + smoothness; the underside a slightly darker brushed silver for shape against the fog.
                hull = Dissolve("Saucer_hull", Gray(0.95f), 0.9f, false, 0.6f),
                hullUnder = Dissolve("Saucer_under", Gray(0.74f), 0.8f, false, 0.45f),
                rim = Dissolve("Saucer_rim", Color.white, 0.6f, false),
                inscription = Dissolve("Moon_inscription", Color.white, 0.5f, false),
                scree = Dissolve("Scree_body", Gray(0.42f), 0.25f, false),
                screeHood = Dissolve("Scree_hood", Gray(0.2f), 0.1f, false),
                black = Dissolve("Moon_black", Gray(0.01f), 0.0f, false),
            };
            m.glow.SetColor("_EmissionColor", Color.white * 3f);
            m.rim.SetColor("_EmissionColor", Color.white * 1.2f);
            m.inscription.SetColor("_EmissionColor", Color.white * 1.4f);
            m.hull.SetFloat("_Rim", 0.4f);                 // the bright highlight along the curve of the disc
            m.hullUnder.SetFloat("_Rim", 0.15f);
            foreach (var x in new[] { m.glow, m.rim, m.inscription }) { x.EnableKeyword("_EMISSION"); EditorUtility.SetDirty(x); }
            _screeCtl = SetupScree(sevenCtl, out _, out _);          // QF4: the real Scree (null = placeholder fallback)
            m.field = AdditiveMat("Moon_field", 0.8f);
            m.ghost = AdditiveMat("Moon_ghost", 0.3f);
            var atmo = BuildAtmosphereMoon(atmoPrefab);
            BuildChapter1Scene(sevenCtl, catCtl, catFur, catWhisker, bank, m, atmo);
        }

        static Material AdditiveMat(string name, float intensity)
        {
            var path = MatDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Seven/Additive")); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = Shader.Find("Seven/Additive");
            mat.SetTexture("_MainTex", WhiteTex());
            mat.SetFloat("_Intensity", intensity);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static GameObject BuildAtmosphereMoon(GameObject atmoPrefab)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(atmoPrefab);
            var a = inst.GetComponent<Seven.Atmosphere>();
            // Dark rock and machinery close by; a bright glowing haze far below and beyond; strong low fog in the bay.
            a.FogStart = 4f;
            a.GroundDensity = 0.016f; a.GroundBase = L.Bay; a.GroundFalloff = 1.6f; a.GroundRange = 40f;
            a.ValleyDensity = 0.012f; a.ValleyBase = -45f; a.ValleyFalloff = 16f;
            a.DistanceDensity = 0.0075f; a.HazeHeight = 30f; a.MaxFog = 1f;
            a.NearBrightness = 0.07f; a.FarBrightness = 1.2f; a.BrightnessDistance = 45f; a.ZenithBrightness = 0.14f;
            a.NoiseStrength = 0.5f; a.NoiseScale = 0.05f; a.NoiseSpeed = 0.015f; a.NoiseFadeDistance = 50f;
            a.GlowIntensity = 0.32f; a.GlowRadius = 30f;
            var glows = new[]
            {
                (new Vector3(8f, -30f, 45f), new Vector3(1.2f, 1.4f, 1f)),
                (new Vector3(-40f, -22f, 80f), new Vector3(1.5f, 1.1f, 1f)),
                (new Vector3(45f, -26f, 70f), new Vector3(1.6f, 1.2f, 1f)),
                (new Vector3(12f, -38f, -140f), new Vector3(2.2f, 1.5f, 1f)),
                (new Vector3(-30f, -20f, -95f), new Vector3(1.2f, 1.0f, 1f)),
                (new Vector3(60f, -18f, -110f), new Vector3(1.6f, 1.1f, 1f)),
                (new Vector3(0f, -10f, 220f), new Vector3(6f, 1.6f, 1f)),
                (new Vector3(10f, -12f, -260f), new Vector3(6f, 1.6f, 1f)),
            };
            int gi = 0;
            foreach (Transform c in inst.transform)
            {
                if (c.name.StartsWith("Glow") && gi < glows.Length) { c.position = glows[gi].Item1; c.localScale = glows[gi].Item2; gi++; }
                if (c.name == "BackLight") { c.rotation = Quaternion.Euler(22f, 200f, 0f); var l = c.GetComponent<Light>(); l.intensity = 1.1f; l.shadowStrength = 0.8f; }
                if (c.name == "FrontFill") { c.rotation = Quaternion.Euler(35f, 20f, 0f); c.GetComponent<Light>().intensity = 0.22f; }
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(inst, AtmoMoonPath);
            UnityEngine.Object.DestroyImmediate(inst);
            Log("atmosphere variant saved " + AtmoMoonPath);
            return prefab;
        }

        // ------------------------------------------------------------------------------------ helpers
        /// <summary>Axis-aligned block from min to max corner, with a box collider (camera / line of sight).</summary>
        static GameObject Block(string name, Vector3 min, Vector3 max, Material mat, Transform parent, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = (min + max) * 0.5f;
            go.transform.localScale = new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), Mathf.Abs(max.z - min.z));
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Block from (x0..x1) sloping along z: top surface goes from yTop at zTop to yBot at zBot.</summary>
        static GameObject SlopeBlock(string name, float x0, float x1, float zTop, float zBot, float yTop, float yBot, float thick, Material mat, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            var a = new Vector3((x0 + x1) * 0.5f, yTop, zTop); var b = new Vector3((x0 + x1) * 0.5f, yBot, zBot);
            var dir = (b - a);
            go.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            var up = go.transform.up;
            go.transform.position = (a + b) * 0.5f - up * thick * 0.5f;
            go.transform.localScale = new Vector3(Mathf.Abs(x1 - x0), thick, dir.magnitude + 0.02f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Corridor walls (both sides) + ceiling for a straight passage along z between x0..x1.</summary>
        static void Passage(string name, float x0, float x1, float zA, float zB, float floorA, float floorB, float height, Material wall, Transform parent)
        {
            float zMin = Mathf.Min(zA, zB), zMax = Mathf.Max(zA, zB);
            float yLow = Mathf.Min(floorA, floorB) - 0.3f, yHigh = Mathf.Max(floorA, floorB) + height;
            Block(name + "_WallW", new Vector3(x0 - 0.5f, yLow, zMin), new Vector3(x0, yHigh, zMax), wall, parent);
            Block(name + "_WallE", new Vector3(x1, yLow, zMin), new Vector3(x1 + 0.5f, yHigh, zMax), wall, parent);
            if (Mathf.Approximately(floorA, floorB))
            {
                Block(name + "_Floor", new Vector3(x0 - 0.5f, floorA - 0.3f, zMin), new Vector3(x1 + 0.5f, floorA, zMax), wall, parent);
                Block(name + "_Ceiling", new Vector3(x0 - 0.5f, floorA + height, zMin), new Vector3(x1 + 0.5f, floorA + height + 0.4f, zMax), wall, parent);
            }
            else
            {
                SlopeBlock(name + "_Floor", x0 - 0.5f, x1 + 0.5f, zA, zB, floorA, floorB, 0.3f, wall, parent);
                SlopeBlock(name + "_Ceiling", x0 - 0.5f, x1 + 0.5f, zA, zB, floorA + height + 0.4f, floorB + height + 0.4f, 0.4f, wall, parent);
            }
        }

        /// <summary>Room: floor, ceiling, four walls with door gaps (gap = (side, centre, half width, height)).</summary>
        static void Room(string name, Vector2 min, Vector2 max, float floorY, float height, Material wall, Material floor, Transform parent,
            params (char side, float c, float half, float h)[] gaps)
        {
            const float t = 0.5f;
            Block(name + "_Floor", new Vector3(min.x - t, floorY - 0.3f, min.y - t), new Vector3(max.x + t, floorY, max.y + t), floor, parent);
            Block(name + "_Ceiling", new Vector3(min.x - t, floorY + height, min.y - t), new Vector3(max.x + t, floorY + height + 0.5f, max.y + t), wall, parent);
            void Wall(char side, Vector3 a, Vector3 b)
            {
                bool alongX = side == 'N' || side == 'S';
                float s0 = alongX ? a.x : a.z, s1 = alongX ? b.x : b.z;
                var cuts = gaps.Where(g => g.side == side).OrderBy(g => g.c).ToList();
                float cur = s0; int k = 0;
                foreach (var g in cuts)
                {
                    float g0 = g.c - g.half, g1 = g.c + g.half;
                    if (g0 > cur) Seg(cur, g0, floorY - 0.3f, floorY + height);
                    Seg(g0, g1, floorY + g.h, floorY + height);      // lintel
                    cur = g1;
                }
                if (s1 > cur) Seg(cur, s1, floorY - 0.3f, floorY + height);
                void Seg(float u0, float u1, float y0, float y1)
                {
                    var p0 = alongX ? new Vector3(u0, y0, a.z) : new Vector3(a.x, y0, u0);
                    var p1 = alongX ? new Vector3(u1, y1, b.z) : new Vector3(b.x, y1, u1);
                    Block($"{name}_Wall{side}{k++}", p0, p1, wall, parent);
                }
            }
            Wall('N', new Vector3(min.x - t, 0, max.y), new Vector3(max.x + t, 0, max.y + t));
            Wall('S', new Vector3(min.x - t, 0, min.y - t), new Vector3(max.x + t, 0, min.y));
            Wall('W', new Vector3(min.x - t, 0, min.y), new Vector3(min.x, 0, max.y));
            Wall('E', new Vector3(max.x, 0, min.y), new Vector3(max.x + t, 0, max.y));
        }

        static Light PointLight(string name, Vector3 at, float range, float intensity, Transform parent)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.position = at;
            l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = Color.white; l.shadows = LightShadows.None;
            return l;
        }

        static void Lamp(string name, Vector3 at, float range, float intensity, Material glow, Transform parent, Vector3? size = null)
        {
            var s = size ?? new Vector3(0.5f, 0.12f, 0.12f);
            Block(name + "_Glow", at - s * 0.5f, at + s * 0.5f, glow, parent, false);
            PointLight(name + "_Light", at + Vector3.down * 0.25f, range, intensity, parent);
        }

        static void Railing(string name, Vector3 a, Vector3 b, Material mat, Transform parent)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            float len = Vector3.Distance(a, b);
            int posts = Mathf.Max(1, Mathf.CeilToInt(len / 1.6f));
            for (int i = 0; i <= posts; i++) { var p = Vector3.Lerp(a, b, (float)i / posts); Sweep(v, t, new[] { p, p + Vector3.up * 1.05f }, 0.035f, false, 6); }
            Sweep(v, t, new[] { a + Vector3.up * 1.05f, b + Vector3.up * 1.05f }, 0.04f, false, 6);
            Sweep(v, t, new[] { a + Vector3.up * 0.55f, b + Vector3.up * 0.55f }, 0.025f, false, 6);
            MeshObject(name, SaveMesh(SmoothFinish(v, t), "Moon_" + name), mat, parent);
        }

        static Mesh Colored(Mesh mesh, Func<Vector3, float> alpha)
        {
            var vs = mesh.vertices;
            mesh.colors = vs.Select(p => new Color(1f, 1f, 1f, alpha(p))).ToArray();
            return mesh;
        }

        static void Cart(string name, Vector3 at, float yaw, MoonMats m, Transform parent)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            var q = Quaternion.Euler(0f, yaw, 0f);
            AddBox(v, t, at + Vector3.up * 0.75f, new Vector3(1.3f, 0.7f, 0.9f), yaw);
            AddBox(v, t, at + Vector3.up * 1.12f + q * new Vector3(0f, 0f, 0f), new Vector3(1.2f, 0.12f, 0.8f), yaw);   // ore heap
            foreach (var sx in new[] { -0.45f, 0.45f })
                foreach (var sz in new[] { -0.47f, 0.47f })
                    AddBox(v, t, at + q * new Vector3(sx, 0.22f, sz), new Vector3(0.32f, 0.32f, 0.08f), yaw);
            MeshObject(name, SaveMesh(Finish(v, t), "Moon_" + name), m.metalDark, parent);
        }

        static void Drill(string name, Vector3 baseAt, float height, MoonMats m, Transform parent, bool hanging = false)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            float dir = hanging ? -1f : 1f;
            AddBox(v, t, baseAt + Vector3.up * dir * height * 0.35f, new Vector3(1.2f, height * 0.7f, 1.2f));
            MeshObject(name + "_Body", SaveMesh(Finish(v, t), "Moon_" + name + "_Body"), m.metal, parent);
            var cone = Lathe(new[] { new Vector2(0.7f, 0f), new Vector2(0.05f, 1.6f * dir), new Vector2(0f, 1.62f * dir) }, 0f, 360f, 20, null,
                baseAt + Vector3.up * dir * height * 0.7f);
            MeshObject(name + "_Bit", SaveMesh(cone, "Moon_" + name + "_Bit"), m.metalDark, parent);
        }

        // ------------------------------------------------------------------------------------ the scene
        static void BuildChapter1Scene(AnimatorController sevenCtl, AnimatorController catCtl, Material catFur, Material catWhisker,
            Seven.VoiceBank bank, MoonMats m, GameObject atmoMoon)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.16f, 0.16f);
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(RenderSettings.ambientLight);
            RenderSettings.ambientProbe = sh;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

            var atmo = (GameObject)PrefabUtility.InstantiatePrefab(atmoMoon);
            atmo.name = "Atmosphere_Moon";
            var caveProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(AtmoDir + "/Cave_Volume.asset");
            if (caveProfile != null)
            {
                var vgo = new GameObject("MoonVolume"); var vol = vgo.AddComponent<Volume>();
                vol.isGlobal = true; vol.priority = 10f; vol.sharedProfile = caveProfile;
            }

            var world = new GameObject("MoonPrison").transform;
            var props = new Props();
            BuildCellBlock(world, m, props);
            BuildAncient(world, m, props);
            BuildBay(world, m, props);
            BuildVista(world, m);
            props.city = BuildCity(world, m);
            AddProbe(new Vector3(8f, 1.5f, 2f), new Vector3(40f, 8f, 10f), Vector3.zero);
            AddProbe(new Vector3(16.5f, L.Lower + 2f, -24f), new Vector3(20f, 8f, 34f), Vector3.zero);
            AddProbe(L.SaucerCentre + Vector3.up * 3f, new Vector3(40f, 14f, 32f), Vector3.zero);

            // Ground: walkable zones (order matters for heights: special zones first).
            var wg = new GameObject("WorldGround").AddComponent<Seven.WorldGround>();
            wg.Shape = Seven.Ground.Shape.Zones;
            Seven.WorldGround.ZoneDef Z(string n, float x0, float z0, float x1, float z1, float y0, float y1 = float.NaN, int axis = 1, bool on = true)
                => new Seven.WorldGround.ZoneDef { Name = n, Min = new Vector2(x0, z0), Max = new Vector2(x1, z1), Y0 = y0, Y1 = float.IsNaN(y1) ? y0 : y1, Axis = axis, Enabled = on };
            Seven.WorldGround.ZoneDef Low(Seven.WorldGround.ZoneDef z) { z.Low = true; return z; }
            float sx = L.SaucerCentre.x, sz = L.SaucerCentre.z;
            // Round 9: the traversal routes (MoonTraversal) split the level into zones that are not walk-connected;
            // ledges, jumps, climbs and mantles connect them. Flat zones come before the slopes that overlap them.
            float lo = L.Lower;
            wg.Zones = new[]
            {
                Z("Cell", -1.6f, -1.6f, 1.6f, L.CellWallZ0, 0f),
                Z("CellDoor", -L.DoorHalf, 0.6f, L.DoorHalf, 2.6f, 0f, float.NaN, 1, false),
                Z("Corridor", -8f, 2.0f, Seven.MoonTraversal.GapX.x, 4.6f, 0f),
                Z("CatwalkB", Seven.MoonTraversal.GapX.y, 2.0f, 11.4f, 4.6f, 0f),
                Low(Z("PipeRun", 10.8f, 2.0f, 13.6f, 4.6f, 0f)),
                Z("CatwalkC", 13.0f, 2.0f, 14.0f, 4.6f, 0f),
                Z("Bulkhead", 14.0f, 2.0f, 15.3f, 4.6f, Seven.MoonTraversal.BulkheadTop),
                Z("Corridor2", 15.3f, 2.0f, L.BarricadeX - 0.4f, 4.6f, 0f),
                Z("Alcove", L.AlcoveX.x, L.AlcoveBackZ, L.AlcoveX.y, 2.7f, 0f, float.NaN, 1, false),   // on from scene 2
                Z("Passage0", L.PassageX.x, -0.2f, L.PassageX.y, 2.6f, 0f, float.NaN, 1, false),
                Z("CrackTop", L.PassageX.x, -2.4f, L.PassageX.y, -0.2f, 2.8f),
                Z("RuinsLedge", 10.4f, -4.6f, 13.0f, -2.6f, -1.6f),
                Z("TerraceA", 7.0f, -4.8f, 9.2f, -2.6f, -1.6f),
                Z("TerraceB", 6.6f, -6.8f, 9.6f, -4.8f, -0.7f),
                Z("TerraceC", 10.8f, -7.0f, 13.8f, -5.0f, 0.8f),
                Z("StairFoot", 17.8f, -7.0f, 19.6f, -4.7f, lo),
                Z("RuinsFloor", 15.0f, -8.5f, 20.0f, -6.4f, lo),
                Z("RuinsDoor", L.PassageX.x, -9.4f, L.PassageX.y, -7.8f, lo),
                Z("Stair", 13.2f, -5.8f, 18.4f, -4.6f, 1.28f, -2.88f, 0, false),          // on once the slab is moved
                Z("Hall", L.HallMin.x, L.HallMin.y, L.HallMax.x, L.HallMax.y, lo),
                Z("HallExit", L.PassageX.x, -30.3f, L.PassageX.y, L.HallMin.y + 0.8f, lo),
                Z("Vision", L.VisionMin.x, L.VisionMin.y, L.VisionMax.x, L.VisionMax.y, lo),
                Z("VisionExit", L.PassageX.x, -39.4f, L.PassageX.y, L.VisionMin.y + 0.8f, lo, float.NaN, 1, false),
                Z("MinePit", L.PassageX.x, -41.2f, L.PassageX.y, -39.4f, -6.4f),
                Z("MineTop", L.PassageX.x, -42.6f, L.PassageX.y, -41.2f, lo),
                Z("RampTop", L.RampX.x, -50.3f, L.RampX.y, -47.5f, lo),
                Z("Ledge", L.LedgeMin.x, L.LedgeMin.y, L.LedgeMax.x, L.LedgeMax.y, lo),
                Z("RampA", L.RampX.x, -55.0f, L.RampX.y, -49.5f, -4.4f, -2.4f + 0.8f * (2.0f / 4.7f), 1),
                Z("Scaffold1", 19.8f, -58.2f, 23.2f, -55.1f, -7.6f),
                Z("Scaffold2", 19.8f, -62.4f, 23.2f, -59.4f, -7.6f),
                Z("BayNorth", 18.8f, -70.2f, 24.4f, -62.5f, L.Bay),
                Z("Bay", L.BayMin.x, L.BayMin.y, L.BayMax.x, L.BayMax.y, L.Bay),
            };
            var obstacles = new List<Vector3> { new Vector3(sx, sz, L.SaucerRadius + 0.3f) };   // 0 = the saucer
            obstacles.Add(new Vector3(L.CrateTop.x, L.CrateTop.z, 0.3f));
            for (int i = 0; i < 6; i++) { var pp = L.SaucerCentre + Polar(3.5f, i * 60f + 30f, 0f); obstacles.Add(new Vector3(pp.x, pp.z, 0.3f)); }   // cradle posts
            foreach (var p in L.Pillars) obstacles.Add(new Vector3(p.x, p.z, L.PillarR));
            obstacles.Add(new Vector3(L.StonePillar.x, L.StonePillar.z, 0.6f));
            obstacles.Add(new Vector3(L.Lever.x, L.Lever.z, 0.7f));
            obstacles.AddRange(props.extraObstacles);
            foreach (var (wp, _) in ScreeWorkSpots) obstacles.Add(new Vector3(wp.x, wp.z, 0.45f));   // QF4: Scree workers
            wg.ZoneObstacles = obstacles.ToArray();
            ZFightFix("Chapter1");  // QF9: no two block faces in one plane (the black / grey flicker)
            PhantomCheck(wg);       // QF3: nothing phantom in the walkable spaces, no city surface inside the host

            // 7, the cat, the camera.
            var seven = Spawn(SevenDir + "/7_character.fbx", "7", L.SevenStart, Vector3.forward, sevenCtl);
            var catRoot = SpawnCat("Cat", L.VentMouth, Vector3.right, catCtl, catFur, catWhisker, out var catAnim);
            var camGo = new GameObject("ChapterCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.55f, 0.55f);
            cam.fieldOfView = 52f; cam.nearClipPlane = 0.08f; cam.farClipPlane = 15000f;     // Round 7: the city reaches ~6 km
            camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true; camData.requiresDepthTexture = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camGo.transform.position = L.SevenStart + new Vector3(0f, 1.8f, -2.5f);

            var dirGo = new GameObject("Chapter1Director");
            var input = dirGo.AddComponent<Seven.ChapterInput>();
            var s7 = seven.AddComponent<Seven.ChapterSeven>();
            s7.Input = input; s7.CameraTransform = camGo.transform;
            var ccam = camGo.AddComponent<Seven.ChapterCamera>();
            ccam.Target = seven.transform; ccam.Input = input;
            var cat = catRoot.AddComponent<Seven.ChapterCat>();
            cat.Seven = seven.transform; cat.Anim = catAnim; cat.Cam = cam;

            // Scree guard (the hall) + two gate guards (visible on the guarded route, not fightable). QF4: the real Scree.
            var guard = SpawnAnyScree("Scree_Guard", L.GuardPost, Vector3.forward, sevenCtl, m, true);
            var g = guard.AddComponent<Seven.ScreeGuard>();
            g.Post = L.GuardPost; g.PostFacing = Vector3.forward;
            g.Eye = guard.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name.EndsWith("Head"));
            var gates = new List<Seven.ScreeGuard>();
            for (int i = 0; i < L.GatePosts.Length; i++)
            {
                var gg = SpawnAnyScree("Scree_Gate" + i, L.GatePosts[i], Vector3.left + Vector3.forward * (i == 0 ? -0.2f : 0.3f), sevenCtl, m, true);
                var sg = gg.AddComponent<Seven.ScreeGuard>(); sg.Post = L.GatePosts[i]; sg.PostFacing = Vector3.left; sg.UseGround = false; gates.Add(sg);
            }
            // Scree workers (work loop): in the bay (evidence of their work for Tunneler; not guards) and, QF4, on the
            // terrace / bay edge seen in the city reveal (6b). Each stands on its own obstacle circle.
            var workers = new List<Seven.ScreeGuard>();
            int wi = 0;
            foreach (var (p, f) in ScreeWorkSpots)
            {
                var w = SpawnAnyScree("Scree_Worker" + wi++, p, f, sevenCtl, m, false);
                var sg = w.AddComponent<Seven.ScreeGuard>(); sg.Post = p; sg.PostFacing = f; sg.PostState = "work"; sg.ScanAngle = 0f; sg.UseGround = false;
                workers.Add(sg);
            }

            var ui = new GameObject("ChapterUI").AddComponent<Seven.ChapterUI>();
            ui.Cam = cam;
            var dlg = dirGo.AddComponent<Seven.DialoguePlayer>();
            dlg.Bank = bank; dlg.UI = ui;
            var legend = new GameObject("ControlsLegend").AddComponent<Seven.ControlsLegend>();
            legend.Cam = cam; legend.Set = Seven.ControlsLegend.Layout.Chapter; legend.HidePanels = true;   // Round 9: no permanent legend

            var d = dirGo.AddComponent<Seven.Chapter1Director>();
            d.Seven = s7; d.Cat = cat; d.Cam = ccam; d.Input = input; d.Dlg = dlg; d.UI = ui; d.Legend = legend;
            d.Additive = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Fx_additive.mat");
            d.LaserDoor = props.laserDoor; d.LaserBeams = props.laserBeams.ToArray(); d.LaserLight = props.laserLight; d.ReleaseLight = props.releaseLight;
            d.OldWallSlab = props.oldWall; d.Guard = g; d.GateGuards = gates.ToArray(); d.Workers = workers.ToArray(); d.Stone = props.stone;
            d.InscriptionGlow = props.inscription; d.VisionWhole = props.visionWhole; d.VisionRuins = props.visionRuins;
            d.SealedDoorSlab = props.sealedDoor; d.Saucer = props.saucer; d.Hatch = props.hatch; d.Beam = props.beam; d.BeamLight = props.beamLight; d.BeamFloor = props.beamFloor; d.HatchGlow = props.hatchGlow; d.Lever = props.lever;
            d.City = props.city; d.Atmo = atmo.GetComponent<Seven.Atmosphere>();
            d.Clamps = props.clamps.ToArray(); d.RimGlow = props.rimGlow.ToArray(); d.SaucerUnderLight = props.underLight;
            d.Slab = props.slab; d.Shape = props.shape; d.ShapeLight = props.shapeLight; d.DoorSymbol = props.doorSymbol; d.DoorRight = props.doorRight; d.DoorEngrave = props.doorEngrave;   // Round 9
            d.LockRubble = props.lockRubble;                                                                                                    // QF7
            d.VisionVideo = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Video/vision_01_door.mp4");   // the first vision (Dan, 2026-11-17)
            if (d.VisionVideo == null) Log("WARNING: vision video missing (Assets/Video/vision_01_door.mp4) - the in-engine vision is the fallback");

            EditorSceneManager.SaveScene(scene, Chapter1ScenePath);
            Log("scene saved " + Chapter1ScenePath);
        }

        class Props
        {
            public Transform laserDoor, oldWall, stone, sealedDoor, saucer, hatch, beam, lever;
            public Renderer releaseLight, inscription, hatchGlow, beamFloor;
            public List<Renderer> laserBeams = new List<Renderer>();
            public Light laserLight, beamLight;
            public Vector3 releasePanel;
            public Seven.CityMotion city;
            public GameObject visionWhole, visionRuins;
            public List<Transform> clamps = new List<Transform>();
            public List<Renderer> rimGlow = new List<Renderer>();
            public Light underLight;
            public List<Vector3> extraObstacles = new List<Vector3>();
            // Round 9
            public Transform slab, shape;
            public Light shapeLight;
            public Renderer doorSymbol;
            public Transform doorRight;
            public Renderer[] doorEngrave;
            public Transform lockRubble;      // QF7
        }

        static GameObject SpawnCat(string name, Vector3 pos, Vector3 facing, AnimatorController ctl, Material fur, Material whisker, out Animator anim)
        {
            var root = new GameObject(name);
            root.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(facing));
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CatFbx));
            model.name = "CatModel";
            model.transform.SetParent(root.transform, false);
            var head = model.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "head");
            var pelvis = model.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "pelvis");
            if (head != null && pelvis != null)
            {
                var fwd = root.transform.InverseTransformDirection(head.position - pelvis.position); fwd.y = 0f;
                model.transform.localRotation = Quaternion.Euler(0f, Vector3.SignedAngle(fwd, Vector3.forward, Vector3.up), 0f) * model.transform.localRotation;
            }
            anim = model.GetComponent<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctl;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.updateWhenOffscreen = true;
                smr.sharedMaterials = smr.sharedMaterials.Select(mm => mm != null && mm.name.Contains("whisker") ? whisker : fur).ToArray();
            }
            return root;
        }

        static AnimatorController _screeCtl;
        static readonly (Vector3, Vector3)[] ScreeWorkSpots = new[]
        {
                (new Vector3(2.8f, L.Bay, -73.5f), new Vector3(0.6f, 0f, -1f)), (new Vector3(4.6f, L.Bay, -76.4f), new Vector3(-1f, 0f, 0.2f)),
                (new Vector3(17.4f, L.Bay, -88.6f), new Vector3(-0.8f, 0f, 0.5f)), (new Vector3(5.2f, L.Bay, -86.2f), new Vector3(1f, 0f, 0.3f)),
                (new Vector3(11.8f, L.Lower, -47.4f), new Vector3(-0.3f, 0f, -1f)), (new Vector3(12.6f, L.Lower, -42.9f), new Vector3(-1f, 0f, 0.1f)),
        };

        /// <summary>QF4: the real Scree when its export imported cleanly, else the placeholder below.</summary>
        static GameObject SpawnAnyScree(string name, Vector3 pos, Vector3 facing, AnimatorController sevenCtl, MoonMats m, bool lamp)
            => _screeCtl != null ? SpawnScreeModel(name, pos, facing, _screeCtl, m, lamp) : SpawnScree(name.Replace("Scree_", "Scree_placeholder_"), pos, facing, sevenCtl, m, lamp);

        /// <summary>Scree_placeholder: gray Y Bot + hood + robe, Humanoid idle / walk from 7's clips, optional lamp.</summary>
        static GameObject SpawnScree(string name, Vector3 pos, Vector3 facing, AnimatorController ctl, MoonMats m, bool lamp)
        {
            var go = Spawn(BotDir + "/ybot_character.fbx", name, pos, facing, ctl);
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                smr.sharedMaterials = smr.sharedMaterials.Select(_ => m.scree).ToArray();
            var tf = go.GetComponentsInChildren<Transform>();
            Transform B(string n) => tf.FirstOrDefault(x => x.name.EndsWith(":" + n) || x.name == n);
            var head = B("Head"); var hips = B("Hips"); var chest = B("Spine2") ?? B("Spine1");
            var fwd = go.transform.forward;
            if (head != null)
            {
                var v = new List<Vector3>(); var t = new List<int>();
                var c = head.position + Vector3.up * 0.06f - fwd * 0.01f;
                var rot = Quaternion.LookRotation(fwd);
                Ellipsoid(v, t, Vector3.zero, new Vector3(0.15f, 0.19f, 0.17f), 14, 22, dd => dd.z > 0.35f && dd.y < 0.55f && dd.y > -0.7f);
                for (int i = 0; i < v.Count; i++) v[i] = c + rot * v[i];
                var hood = MeshObject("Hood", SaveMesh(SmoothFinish(v, t), "Scree_Hood_" + name), m.screeHood, go.transform);
                hood.transform.SetParent(head, true);
                // the face stays in shadow: a dark disc inside the hood opening
                var fv = new List<Vector3>(); var ft = new List<int>();
                Ellipsoid(fv, ft, Vector3.zero, new Vector3(0.11f, 0.13f, 0.05f), 8, 14);
                for (int i = 0; i < fv.Count; i++) fv[i] = c + fwd * 0.07f + Vector3.down * 0.02f + rot * fv[i];
                var face = MeshObject("FaceShadow", SaveMesh(SmoothFinish(fv, ft), "Scree_Face_" + name), m.black, go.transform);
                face.transform.SetParent(head, true);
            }
            if (hips != null)
            {
                var robe = Lathe(new[] { new Vector2(0.44f, -0.62f), new Vector2(0.36f, -0.2f), new Vector2(0.27f, 0.25f), new Vector2(0.24f, 0.42f) }, 0f, 360f, 24, null,
                    hips.position - go.transform.position);
                var r = MeshObject("Robe", SaveMesh(robe, "Scree_Robe_" + name), m.screeHood, go.transform);
                r.transform.localPosition = Vector3.zero; r.transform.rotation = Quaternion.identity;
                r.transform.position = go.transform.position;
                r.transform.SetParent(hips, true);
            }
            if (lamp && chest != null)
            {
                var lgo = new GameObject("GuardLamp");
                lgo.transform.position = chest.position + fwd * 0.22f;
                lgo.transform.rotation = Quaternion.LookRotation(fwd + Vector3.down * 0.25f);
                lgo.transform.SetParent(chest, true);
                var l = lgo.AddComponent<Light>();
                l.type = LightType.Spot; l.range = 9f; l.spotAngle = 72f; l.intensity = 4f; l.color = Color.white; l.shadows = LightShadows.None;
                var bulb = Block("LampBulb", lgo.transform.position - Vector3.one * 0.05f, lgo.transform.position + Vector3.one * 0.05f, m.glow, lgo.transform, false);
            }
            return go;
        }

        // ------------------------------------------------------------------------------------ cell block + catwalk
        static void BuildCellBlock(Transform world, MoonMats m, Props props)
        {
            var root = new GameObject("CellBlock").transform; root.SetParent(world, false);
            // The cell.
            Room("Cell", new Vector2(-1.6f, -1.6f), new Vector2(1.6f, L.CellWallZ0), 0f, 2.9f, m.rock, m.metalDark, root, ('N', 0f, L.DoorHalf, 2.45f));
            Block("Cell_Bench", new Vector3(-1.6f, 0f, -1.6f), new Vector3(-0.9f, 0.45f, 0.2f), m.metal, root);
            Lamp("Cell_Lamp", new Vector3(0f, 2.82f, -0.6f), 5f, 0.9f, m.glow, root, new Vector3(0.9f, 0.05f, 0.12f));
            // Long rock face of the cell block, with the cell door, the older wall section and the cat's small opening.
            float zf0 = L.CellWallZ0, zf1 = L.CellWallZ1;
            // Round 7: the face west of the door is split around the guard alcove (the hidden release).
            float ax0 = L.AlcoveX.x, ax1 = L.AlcoveX.y;
            Block("Face_A0", new Vector3(-9f, -0.3f, zf0), new Vector3(ax0, 7f, zf1), m.rock, root);
            Block("Face_A_AlcoveLintel", new Vector3(ax0, 2.4f, zf0), new Vector3(ax1, 7f, zf1), m.rock, root);
            Block("Face_A1", new Vector3(ax1, -0.3f, zf0), new Vector3(-L.DoorHalf, 7f, zf1), m.rock, root);
            Block("Alcove_WallW", new Vector3(ax0 - 0.5f, -0.3f, L.AlcoveBackZ - 0.4f), new Vector3(ax0, 2.9f, zf0), m.rock, root);
            Block("Alcove_WallE", new Vector3(ax1, -0.3f, L.AlcoveBackZ - 0.4f), new Vector3(-2.1f, 2.9f, zf0), m.rock, root);
            Block("Alcove_Back", new Vector3(ax0 - 0.5f, -0.3f, L.AlcoveBackZ - 0.4f), new Vector3(-2.1f, 2.9f, L.AlcoveBackZ), m.rock, root);
            Block("Alcove_Ceiling", new Vector3(ax0 - 0.5f, 2.4f, L.AlcoveBackZ - 0.4f), new Vector3(-2.1f, 2.9f, zf1), m.rock, root);
            Block("Alcove_Floor", new Vector3(ax0, -0.25f, L.AlcoveBackZ), new Vector3(ax1, 0f, zf1), m.metalDark, root);
            Block("Alcove_Bench", new Vector3(ax1 - 0.42f, 0f, L.AlcoveBackZ), new Vector3(ax1, 0.42f, L.AlcoveBackZ + 0.95f), m.metal, root);
            Lamp("Alcove_Lamp", new Vector3(-3.0f, 2.32f, 1.1f), 2.6f, 0.5f, m.glow, root, new Vector3(0.3f, 0.04f, 0.1f));
            Block("Face_Lintel", new Vector3(-L.DoorHalf, 2.45f, zf0), new Vector3(L.DoorHalf, 7f, zf1), m.rock, root);
            Block("Face_B", new Vector3(L.DoorHalf, -0.3f, zf0), new Vector3(L.PassageX.x - 0.1f, 7f, zf1), m.rock, root);
            Block("Face_OldTop", new Vector3(L.PassageX.x - 0.1f, 2.6f, zf0), new Vector3(L.PassageX.y + 0.1f, 7f, zf1), m.rock, root);
            Block("Face_C", new Vector3(L.PassageX.y + 0.1f, -0.3f, zf0), new Vector3(34f, 7f, zf1), m.rock, root);
            // Round 7: laser door - horizontal white beams set exactly in the cell opening, flush with the inner wall face.
            // Walking is limited by the walkable zones (the "Cell" zone ends at CellWallZ0), so the beams are solid.
            var laser = new GameObject("LaserDoor").transform; laser.SetParent(root, false); laser.position = new Vector3(0f, 0f, L.LaserZ);
            Block("LaserEmitterW", new Vector3(-L.DoorHalf, 0f, L.LaserZ - 0.04f), new Vector3(-L.DoorHalf + 0.06f, 2.45f, L.LaserZ + 0.08f), m.metalDark, laser);
            Block("LaserEmitterE", new Vector3(L.DoorHalf - 0.06f, 0f, L.LaserZ - 0.04f), new Vector3(L.DoorHalf, 2.45f, L.LaserZ + 0.08f), m.metalDark, laser);
            for (int i = 0; i < L.LaserCount; i++)
            {
                float y = L.LaserY(i);
                var beam = Block("Beam" + i, new Vector3(-L.DoorHalf + 0.06f, y - 0.011f, L.LaserZ - 0.011f), new Vector3(L.DoorHalf - 0.06f, y + 0.011f, L.LaserZ + 0.011f), m.glow, laser, false);
                beam.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                var hv = new List<Vector3>(); var ht = new List<int>();
                Quad(hv, ht, new Vector3(-L.DoorHalf + 0.06f, y - 0.022f, 0f), new Vector3(-L.DoorHalf + 0.06f, y + 0.022f, 0f), new Vector3(L.DoorHalf - 0.06f, y + 0.022f, 0f), new Vector3(L.DoorHalf - 0.06f, y - 0.022f, 0f));
                var halo = MeshObject("Halo" + i, SaveMesh(Colored(Finish(hv, ht), p => 0.2f), "Moon_LaserHalo" + i), m.field, laser, false);
                props.laserBeams.Add(beam.GetComponent<Renderer>());
                props.laserBeams.Add(halo.GetComponent<Renderer>());      // pairs: beam, halo
            }
            props.laserLight = PointLight("LaserLight", new Vector3(0f, 1.2f, L.LaserZ + 0.25f), 3.2f, 0.9f, laser);
            props.laserDoor = laser;
            // Release panel inside the guard alcove (the cat paws it from a small crate) - out of sight of the cell.
            Block("ReleasePanel", L.ReleasePanel - new Vector3(0.2f, 0.25f, 0.03f), L.ReleasePanel + new Vector3(0.2f, 0.25f, 0.03f), m.metalDark, root);
            props.releaseLight = Block("ReleaseLight", L.ReleasePanel + new Vector3(-0.12f, -0.05f, 0.03f), L.ReleasePanel + new Vector3(0.12f, 0.05f, 0.05f), m.glow, root, false).GetComponent<Renderer>();
            props.releasePanel = L.ReleasePanel;
            Block("Crate", new Vector3(L.CrateTop.x - L.CrateSize.x * 0.5f, 0f, L.CrateTop.z - L.CrateSize.z * 0.5f), new Vector3(L.CrateTop.x + L.CrateSize.x * 0.5f, L.CrateSize.y, L.CrateTop.z + L.CrateSize.z * 0.5f), m.crate, root);
            // The cat's small opening: a dark vent at the foot of the wall.
            Block("Vent", new Vector3(L.VentMouth.x - 0.24f, 0f, zf1 - 0.04f), new Vector3(L.VentMouth.x + 0.24f, 0.36f, zf1 + 0.01f), m.black, root, false);
            var gv = new List<Vector3>(); var gt = new List<int>();
            foreach (var y in new[] { 0.36f }) AddBox(gv, gt, new Vector3(L.VentMouth.x, y, zf1 + 0.02f), new Vector3(0.56f, 0.05f, 0.05f));
            AddBox(gv, gt, new Vector3(L.VentMouth.x + 0.6f, 0.2f, zf1 + 0.3f), new Vector3(0.5f, 0.04f, 0.4f), 25f);   // the vent grille, pried off
            MeshObject("VentGrille", SaveMesh(Finish(gv, gt), "Moon_VentGrille"), m.metal, root);
            // Other cells along the wall (closed fields, empty).
            foreach (var x in new[] { 5.2f, 9.6f, -6.8f })
            {
                Block($"OtherCell{x}_Dark", new Vector3(x - 0.6f, 0f, zf1 - 0.05f), new Vector3(x + 0.6f, 2.45f, zf1 + 0.005f), m.black, root, false);
                var ov = new List<Vector3>(); var ot = new List<int>();
                for (int i = 0; i < 7; i++) { float bx = x + Mathf.Lerp(-0.52f, 0.52f, i / 6f); Sweep(ov, ot, new[] { new Vector3(bx, 0f, zf1 + 0.06f), new Vector3(bx, 2.45f, zf1 + 0.06f) }, 0.028f, false, 6); }
                MeshObject($"OtherCell{x}_Bars", SaveMesh(SmoothFinish(ov, ot), $"Moon_OtherBars{x}"), m.metal, root);
            }
            // The older wall section: smooth ancient blocks with faint engraved lines (sinks into the floor in scene 4).
            var old = new GameObject("OldWall").transform; old.SetParent(root, false); old.position = Vector3.zero;
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 3; c++)
                {
                    float off = (r % 2) * 0.33f;
                    float x0 = L.PassageX.x - 0.1f + c * 0.66f + off - (r % 2) * 0.33f, x1 = Mathf.Min(L.PassageX.y + 0.1f, x0 + 0.64f);
                    Block($"OldStone{r}{c}", new Vector3(x0, r * 0.65f, zf0 + 0.05f), new Vector3(x1, r * 0.65f + 0.63f, zf1 + 0.03f), r % 2 == 0 ? m.ancient : m.ancientDark, old);
                }
            var lv = new List<Vector3>(); var lt = new List<int>();
            var ring = Enumerable.Range(0, 33).Select(i => new Vector3(L.OldWall.x + Mathf.Cos(i / 32f * Mathf.PI * 2f) * 0.4f, L.OldWall.y + 0.1f + Mathf.Sin(i / 32f * Mathf.PI * 2f) * 0.4f, zf1 + 0.045f)).ToList();
            Sweep(lv, lt, ring, 0.012f, true, 5);
            Sweep(lv, lt, new[] { new Vector3(L.OldWall.x - 0.7f, 0.35f, zf1 + 0.045f), new Vector3(L.OldWall.x + 0.7f, 0.35f, zf1 + 0.045f) }, 0.01f, false, 5);
            MeshObject("OldWallLines", SaveMesh(SmoothFinish(lv, lt), "Moon_OldWallLines"), m.inscription, old, false);
            props.oldWall = old;


            // Catwalk over the cavern (Round 9: broken section, pipe run, bulkhead - FightDemoBuilderTraversal).
            BuildPrisonTraversal(root, m);
            Cart("Cart_W", new Vector3(-7f, 0f, 4.35f), 90f, m, root);
            Cart("Cart_Gate", new Vector3(23f, 0f, 4.35f), 90f, m, root);
            var sv = new List<Vector3>(); var st = new List<int>();
            for (float x = -8f; x < 34f; x += 6f) { AddBox(sv, st, new Vector3(x, -14f, 4.4f), new Vector3(0.35f, 28f, 0.35f)); AddBox(sv, st, new Vector3(x + 3f, -1.2f, 3.4f), new Vector3(0.2f, 2f, 3f), 0f); }
            MeshObject("CatwalkSupports", SaveMesh(Finish(sv, st), "Moon_CatwalkSupports"), m.metalDark, root);
            foreach (var x in new[] { -5.5f, 3f, 12.5f, 21f, 28f }) Lamp($"WallLamp{x}", new Vector3(x, 2.75f, zf1 + 0.12f), 7.5f, 1.3f, m.glow, root);
            // Barricade before the guarded gate + the gate itself (lit, two Scree guards stand there).
            for (int i = 0; i < 4; i++) Block($"Barricade{i}", new Vector3(L.BarricadeX, 0f, 2.0f + i * 0.72f), new Vector3(L.BarricadeX + 1.1f, 0.9f + (i % 2) * 0.35f, 2.68f + i * 0.72f), m.crate, root);
            Block("Gate_PostA", new Vector3(26.8f, 0f, 1.95f), new Vector3(27.2f, 3.6f, 2.35f), m.metal, root);
            Block("Gate_PostB", new Vector3(26.8f, 0f, 4.4f), new Vector3(27.2f, 3.6f, 4.8f), m.metal, root);
            Block("Gate_Beam", new Vector3(26.8f, 3.3f, 1.95f), new Vector3(27.2f, 3.6f, 4.8f), m.metal, root);
            Lamp("GateLamp", new Vector3(27f, 3.2f, 3.4f), 9f, 2.4f, m.glow, root, new Vector3(0.2f, 0.1f, 2.2f));
            // Rock above and around the wall face (organic silhouette against the haze).
            var rv = new List<Vector3>(); var rt = new List<int>();
            var rnd = new System.Random(11);
            for (float x = -10f; x < 36f; x += 3.2f)
            {
                float s = 2.2f + (float)rnd.NextDouble() * 2.2f;
                Lump(rv, rt, new Vector3(x, 7.5f + (float)rnd.NextDouble() * 3f, zf0 - 0.8f), new Vector3(s, s * 1.4f, 2.2f), 0.35f, (int)(x * 10) + 50);
            }
            MeshObject("CliffAbove", SaveMesh(SmoothFinish(rv, rt), "Moon_CliffAbove"), m.rock, root);
        }

        // ------------------------------------------------------------------------------------ ancient structures
        static void BuildAncient(Transform world, MoonMats m, Props props)
        {
            var root = new GameObject("Ancient").transform; root.SetParent(world, false);
            float x0 = L.PassageX.x, x1 = L.PassageX.y;
            // Older passage down (scene 4 -> 5).
            // Round 9: the older passage climbs a cracked wall, then the shaft lip and the ruin terraces down to the hall.
            BuildPassageRuins(root, m, props);
            // The hall (Scree guard).
            Room("Hall", L.HallMin, L.HallMax, L.Lower, 6.4f, m.ancient, m.ancientDark, root,
                ('N', 16.5f, 0.9f, 3.0f), ('S', 16.5f, 0.9f, 3.0f));
            var pv = new List<Vector3>(); var pt = new List<int>();
            foreach (var p in L.Pillars)
            {
                bool broken = p.z < -18f && p.x > 16f;
                var mesh = Lathe(new[] { new Vector2(0f, 0f), new Vector2(L.PillarR + 0.15f, 0f), new Vector2(L.PillarR + 0.15f, 0.3f), new Vector2(L.PillarR, 0.35f), new Vector2(L.PillarR, broken ? 3.2f : 6.4f), new Vector2(0f, broken ? 3.2f : 6.4f) }, 0f, 360f, 20, null, p);
                var col = MeshObject("Pillar", SaveMesh(mesh, $"Moon_Pillar{p.x}_{p.z}"), m.ancient, root);
                var cc = col.AddComponent<CapsuleCollider>(); cc.center = col.transform.InverseTransformPoint(p + Vector3.up * 3f); cc.radius = L.PillarR; cc.height = 6f;
            }
            // Broken pillar holding the loose stone.
            var bp = Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.6f, 0f), new Vector2(0.55f, 0.3f), new Vector2(0.55f, 1.55f), new Vector2(0.3f, 1.6f), new Vector2(0f, 1.62f) }, 0f, 360f, 20, null, L.StonePillar);
            var bpo = MeshObject("StonePillar", SaveMesh(bp, "Moon_StonePillar"), m.ancientDark, root);
            var bc = bpo.AddComponent<CapsuleCollider>(); bc.center = L.StonePillar + Vector3.up * 0.8f; bc.radius = 0.55f; bc.height = 1.6f;
            var sv = new List<Vector3>(); var st = new List<int>();
            Lump(sv, st, Vector3.zero, Vector3.one * L.StoneRadius, 0.25f, 9);
            var stone = MeshObject("LooseStone", SaveMesh(SmoothFinish(sv, st), "Moon_LooseStone"), m.ancient, root);
            stone.transform.position = L.StoneRest;
            props.stone = stone.transform;
            // Rubble, a work lamp left by the miners, evidence of Tunneler's crews (chains, cart).
            var rv = new List<Vector3>(); var rt = new List<int>();
            var rnd = new System.Random(5);
            for (int i = 0; i < 18; i++)
            {
                float x = Mathf.Lerp(L.HallMin.x + 0.5f, L.HallMax.x - 0.5f, (float)rnd.NextDouble()), z = Mathf.Lerp(L.HallMin.y + 0.5f, L.HallMax.y - 0.5f, (float)rnd.NextDouble());
                if (Mathf.Abs(x - 16.5f) < 3f || (x < 11.4f)) continue;      // keep the middle and the left (sneak) side clear
                float s = Mathf.Lerp(0.15f, 0.45f, (float)rnd.NextDouble());
                Lump(rv, rt, new Vector3(x, L.Lower + s * 0.3f, z), new Vector3(s, s * 0.7f, s), 0.3f, 100 + i);
            }
            MeshObject("Rubble", SaveMesh(SmoothFinish(rv, rt), "Moon_Rubble"), m.ancientDark, root);
            Lamp("HallWorkLamp", new Vector3(23.6f, L.Lower + 2.2f, -18f), 8f, 1.4f, m.glow, root, new Vector3(0.15f, 0.25f, 0.15f));
            Block("HallWorkLampPole", new Vector3(23.55f, L.Lower, -18.05f), new Vector3(23.65f, L.Lower + 2.1f, -17.95f), m.metal, root);
            PointLight("HallFill", new Vector3(16.5f, L.Lower + 5f, -12f), 12f, 0.6f, root);
            PointLight("HallFillLeft", new Vector3(10f, L.Lower + 3f, -17f), 8f, 0.45f, root);

            // Exit corridor -> vision chamber -> sealed door -> passage to the impound ledge.
            Passage("HallExit", x0, x1, L.HallMin.y - 0.5f, L.VisionMax.y, L.Lower, L.Lower, 3.0f, m.ancientDark, root);
            Room("Vision", L.VisionMin, L.VisionMax, L.Lower, 7f, m.ancient, m.ancientDark, root, ('N', 16.5f, 0.9f, 3.0f), ('S', 16.5f, 1.3f, 3.75f),
                ('E', (L.AlcoveZ.x + L.AlcoveZ.y) * 0.5f, (L.AlcoveZ.y - L.AlcoveZ.x) * 0.5f, 2.6f));   // Round 9: the shape's alcove
            var door = new GameObject("SealedDoor").transform; door.SetParent(root, false); door.position = Vector3.zero;
            // Round 9 (8b addition): the lunar door - pointed arch, two leaves parting along an S-seam (ref_lunar_door.png)
            var doorR = new GameObject("SealedDoorR").transform; doorR.SetParent(root, false); doorR.position = Vector3.zero;
            BuildLunarDoor(root, m, props, door, doorR);

            props.sealedDoor = door;
            BuildVisionShape(root, m, props, door);       // Round 9 (8b): Kyle's floating shape replaces the wall inscription
            BuildVisionExitPit(root, m);
            BuildCurvyApproach(root, m);                  // Round 9: the same flowing ribs through the hall exit and the hall                  // Round 9: the mining cut (drop down, climb out) before the terrace
            PointLight("VisionDim", new Vector3(16.5f, L.Lower + 3f, -33f), 9f, 0.6f, root);

            // Vision chamber: ruins now / whole in the vision.
            var ruins = new GameObject("VisionRuins"); ruins.transform.SetParent(root, false);
            var whole = new GameObject("VisionWhole"); whole.transform.SetParent(root, false);
            var colX = new[] { 13.4f, 19.6f };
            var colZ = new[] { -31.2f, -33.6f, -36f };
            var rnd2 = new System.Random(3);
            var rr = new List<Vector3>(); var rtri = new List<int>();
            var wv = new List<Vector3>(); var wt = new List<int>();
            foreach (var cx in colX)
                foreach (var cz in colZ)
                {
                    var b = new Vector3(cx, L.Lower, cz);
                    // QF7: the column south of the shape's alcove stood in every side view of the alcove beats (reach,
                    // holding it, the pocket; the north side is closed by a vault rib's foot) - moved back into the corner
                    // by the door wall, clear of her paths
                    if (cx > 16f && Mathf.Approximately(cz, -36f)) b = new Vector3(19.15f, L.Lower, -37.45f);
                    float hBroken = Mathf.Lerp(0.7f, 2.6f, (float)rnd2.NextDouble());
                    var broken = Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0.42f, 0.25f), new Vector2(0.42f, hBroken), new Vector2(0.2f, hBroken + 0.12f), new Vector2(0f, hBroken + 0.1f) }, 0f, 360f, 16, null, b);
                    MeshObject("RuinColumn", SaveMesh(broken, $"Moon_RuinCol{cx}_{cz}"), m.ancient, ruins.transform);
                    Lump(rr, rtri, b + new Vector3(cx < 16f ? 0.9f : -0.9f, 0.15f, 0.3f), new Vector3(0.45f, 0.25f, 0.35f), 0.35f, (int)(cz * 10));
                    var full = Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.55f, 0f), new Vector2(0.45f, 0.3f), new Vector2(0.42f, 5.6f), new Vector2(0.6f, 5.9f), new Vector2(0.6f, 6.2f), new Vector2(0f, 6.2f) }, 0f, 360f, 20, null, b);
                    MeshObject("WholeColumn", SaveMesh(full, $"Moon_WholeCol{cx}_{cz}"), m.ancient, whole.transform);
                    Sweep(wv, wt, Enumerable.Range(0, 13).Select(i => b + new Vector3(0f, 6.2f, 0f) + new Vector3((cx < 16f ? 1 : -1) * (Mathf.Sin(i / 12f * Mathf.PI) * 0f), 0f, 0f) + new Vector3((cx < 16f ? 1f : -1f) * i / 12f * 3.1f, Mathf.Sin(i / 12f * Mathf.PI) * 0.6f, 0f)).ToList(), 0.13f, false, 8);
                }
            MeshObject("RuinRubble", SaveMesh(SmoothFinish(rr, rtri), "Moon_VisionRubble"), m.ancientDark, ruins.transform);
            MeshObject("WholeArches", SaveMesh(SmoothFinish(wv, wt), "Moon_VisionArches"), m.ancient, whole.transform);
            // Whole: glowing frieze along the walls, hanging lights, the symbol above the door, a brighter fill.
            var gv = new List<Vector3>(); var gt = new List<int>();
            foreach (var y in new[] { 2.2f, 4.6f })
            {
                Sweep(gv, gt, new[] { new Vector3(L.VisionMin.x + 0.05f, L.Lower + y, L.VisionMin.y), new Vector3(L.VisionMin.x + 0.05f, L.Lower + y, L.VisionMax.y) }, 0.03f, false, 5);
                Sweep(gv, gt, new[] { new Vector3(L.VisionMax.x - 0.05f, L.Lower + y, L.VisionMin.y), new Vector3(L.VisionMax.x - 0.05f, L.Lower + y, L.VisionMax.y) }, 0.03f, false, 5);
            }
            var sym = L.Inscription + new Vector3(0f, 3.2f, 0.1f);
            // Round 9: in the vision the shape's outline glows large above the door (the symbol that later opens it)
            MeshObject("WholeSymbol", ShapeOutline(L.VisionShape, sym, Vector3.right, Vector3.up, Vector3.forward, 1.25f, 0.035f, "Moon_VisionWholeSymbol"), m.glow, whole.transform, false);
            foreach (var z in colZ) foreach (var x in new[] { 15.2f, 17.8f }) Ellipsoid(gv, gt, new Vector3(x, L.Lower + 4.4f, z), Vector3.one * 0.16f, 6, 10);
            MeshObject("WholeGlow", SaveMesh(SmoothFinish(gv, gt), "Moon_VisionGlow"), m.glow, whole.transform, false);
            var fill = PointLight("VisionFill", new Vector3(16.5f, L.Lower + 4.5f, -33.5f), 14f, 2.2f, whole.transform);
            // Ghostly figures: pale, silhouetted, softly glowing, no faces.
            var ghosts = new GameObject("Ghosts").transform; ghosts.SetParent(whole.transform, false);
            var prof = new[] { new Vector2(0f, 0f), new Vector2(0.3f, 0.02f), new Vector2(0.26f, 0.7f), new Vector2(0.2f, 1.2f), new Vector2(0.25f, 1.38f), new Vector2(0.12f, 1.5f), new Vector2(0.075f, 1.56f), new Vector2(0.11f, 1.66f), new Vector2(0.105f, 1.78f), new Vector2(0f, 1.86f) };
            var ghostMesh = SaveMesh(Colored(Lathe(prof, 0f, 360f, 20), p => Mathf.Lerp(0.05f, 0.45f, Mathf.Clamp01(p.y / 1.4f))), "Moon_Ghost");
            var spots = new[] { (14.6f, -31.5f, 120f), (18.6f, -32.4f, 230f), (15.4f, -34.6f, 60f), (17.9f, -35.4f, 300f), (14.1f, -36.6f, 20f), (19.2f, -30.6f, 200f), (16.3f, -32.9f, 160f), (13.8f, -33.8f, 90f), (18.9f, -36.8f, 330f) };
            int gi = 0;
            foreach (var (gx, gz, yaw) in spots)
            {
                var go = MeshObject("Ghost" + gi, ghostMesh, m.ghost, ghosts, false);
                go.transform.position = new Vector3(gx, L.Lower, gz);
                go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                float s = 0.92f + (gi % 3) * 0.06f; go.transform.localScale = new Vector3(s, s, s);
                gi++;
            }
            whole.SetActive(false);
            props.visionRuins = ruins; props.visionWhole = whole;
        }

        // ------------------------------------------------------------------------------------ impound bay + saucer
        static void BuildBay(Transform world, MoonMats m, Props props)
        {
            var root = new GameObject("Impound").transform; root.SetParent(world, false);
            // The ledge (viewpoint) and its rock wall, the ramp down.
            Block("Ledge_Floor", new Vector3(L.LedgeMin.x - 0.5f, L.Lower - 0.6f, L.LedgeMin.y - 0.3f), new Vector3(L.LedgeMax.x + 0.5f, L.Lower, L.LedgeMax.y + 0.4f), m.rock, root);
            Block("Ledge_BackW", new Vector3(L.LedgeMin.x - 0.5f, L.Lower, L.LedgeMax.y), new Vector3(L.PassageX.x - 0.5f, L.Lower + 9f, L.LedgeMax.y + 1f), m.rock, root);
            Block("Ledge_BackE", new Vector3(L.PassageX.y + 0.5f, L.Lower, L.LedgeMax.y), new Vector3(L.LedgeMax.x + 0.5f, L.Lower + 9f, L.LedgeMax.y + 1f), m.rock, root);
            Block("Ledge_BackTop", new Vector3(L.PassageX.x - 0.5f, L.Lower + 3f, L.LedgeMax.y), new Vector3(L.PassageX.y + 0.5f, L.Lower + 9f, L.LedgeMax.y + 1f), m.rock, root);
            Block("Ledge_SideW", new Vector3(L.LedgeMin.x - 0.5f, L.Lower, L.LedgeMin.y - 0.3f), new Vector3(L.LedgeMin.x, L.Lower + 4f, L.LedgeMax.y), m.rock, root);
            Railing("LedgeRail", new Vector3(L.LedgeMin.x, L.Lower, L.LedgeMin.y - 0.1f), new Vector3(L.RampX.x - 0.05f, L.Lower, L.LedgeMin.y - 0.1f), m.metal, root);
            Railing("LedgeRailE", new Vector3(L.LedgeMax.x + 0.05f, L.Lower, L.LedgeMax.y), new Vector3(L.LedgeMax.x + 0.05f, L.Lower, L.RampTopZ), m.metal, root);

            BuildBayDescent(root, m);                     // Round 9: broken ramp, two scaffolds, drops down to the bay
            Lamp("LedgeLamp", new Vector3(16.5f, L.Lower + 2.7f, L.LedgeMax.y - 0.15f), 7f, 1.0f, m.glow, root);
            // Cliff below the ledge down to the bay floor.
            var cv = new List<Vector3>(); var ct = new List<int>();
            var rnd = new System.Random(21);
            for (float x = -4f; x < 34f; x += 3f)
            {
                if (x > 19f && x < 24f) continue;
                Lump(cv, ct, new Vector3(x, L.Bay + 2.2f, L.LedgeMin.y + 1.0f), new Vector3(2.2f, 4.6f, 2.4f), 0.3f, (int)x + 300);
            }
            // rock wall high above the ledge (above the ancient rooms' ceilings) and to both sides of it
            for (float x = -14f; x < 40f; x += 5f) Lump(cv, ct, new Vector3(x, 12f + (float)rnd.NextDouble() * 3f, L.LedgeMax.y + 2.5f), new Vector3(3.6f, 6f, 3f), 0.3f, (int)x + 400);
            for (float x = -14f; x < 40f; x += 5f) if (x < 8f || x > 26f) Lump(cv, ct, new Vector3(x, L.Lower, L.LedgeMax.y + 1.5f), new Vector3(3.2f, 8f, 3f), 0.3f, (int)x + 500);
            MeshObject("BayCliff", SaveMesh(SmoothFinish(cv, ct), "Moon_BayCliff"), m.rock, root);
            // Bay floor, rails, carts, drills, lamps on poles, crates.
            Block("Bay_Floor", new Vector3(-12f, L.Bay - 0.5f, -110f), new Vector3(44f, L.Bay, L.LedgeMin.y + 0.5f), m.rockDark, root);
            var tv = new List<Vector3>(); var tt = new List<int>();
            foreach (var x in new[] { 1.0f, 1.45f }) AddBox(tv, tt, new Vector3(x, L.Bay + 0.04f, -82f), new Vector3(0.07f, 0.07f, 40f));
            for (float z = -101f; z < -62f; z += 0.8f) AddBox(tv, tt, new Vector3(1.22f, L.Bay + 0.02f, z), new Vector3(0.8f, 0.04f, 0.12f));
            MeshObject("BayTrack", SaveMesh(Finish(tv, tt), "Moon_BayTrack"), m.metalDark, root);
            Cart("BayCart0", new Vector3(1.22f, L.Bay, -70f), 0f, m, root);
            Cart("BayCart1", new Vector3(1.22f, L.Bay, -72f), 0f, m, root);
            Drill("BayDrill", new Vector3(5.5f, L.Bay, -72.5f), 3f, m, root);
            Drill("HangingDrill", new Vector3(28f, L.Bay + 14f, -88f), 6f, m, root, true);
            foreach (var (x, z) in new[] { (26.5f, -70.5f), (3f, -88f), (27f, -91f), (8f, -66.5f) })
            {
                Block($"Pole{x}", new Vector3(x - 0.08f, L.Bay, z - 0.08f), new Vector3(x + 0.08f, L.Bay + 5.5f, z + 0.08f), m.metal, root);
                Lamp($"PoleLamp{x}", new Vector3(x, L.Bay + 5.6f, z), 13f, 2.2f, m.glow, root, new Vector3(0.6f, 0.12f, 0.3f));
            }
            foreach (var (x, z) in new[] { (28.5f, -77f), (29.3f, -78.2f), (27.8f, -78.6f), (6.5f, -90f) })
            {
                Block($"BayCrate{x}{z}", new Vector3(x - 0.5f, L.Bay, z - 0.5f), new Vector3(x + 0.5f, L.Bay + 0.9f, z + 0.5f), m.crate, root);
                props.extraObstacles.Add(new Vector3(x, z, 0.75f));
            }
            props.extraObstacles.Add(new Vector3(1.22f, -71f, 1.3f));
            props.extraObstacles.Add(new Vector3(5.5f, -72.5f, 1.0f));
            // Release console with the clamp lever.
            Block("Console", L.Lever + new Vector3(-0.5f, 0f, -0.35f), L.Lever + new Vector3(0.5f, 1.0f, 0.35f), m.metalDark, root);
            Block("ConsoleScreen", L.Lever + new Vector3(-0.35f, 1.0f, -0.36f), L.Lever + new Vector3(0.35f, 1.02f, -0.1f), m.glow, root, false);
            var lever = new GameObject("Lever").transform; lever.SetParent(root, false);
            lever.position = L.Lever + new Vector3(0f, 1.0f, 0.15f);
            lever.rotation = Quaternion.Euler(-35f, 0f, 0f);
            var lgv = new List<Vector3>(); var lgt = new List<int>();
            Sweep(lgv, lgt, new[] { Vector3.zero, Vector3.up * 0.55f }, 0.03f, false, 8);
            Ellipsoid(lgv, lgt, Vector3.up * 0.58f, Vector3.one * 0.06f, 6, 10);
            var lgo = MeshObject("LeverHandle", SaveMesh(SmoothFinish(lgv, lgt), "Moon_Lever"), m.metal, lever);
            props.lever = lever;
            PointLight("ConsoleLight", L.Lever + new Vector3(0f, 1.6f, -0.8f), 3f, 0.8f, root);

            // 7's saucer: wide flat disc, glowing rim, polished silver hull (Round 10; was dark glossy); on a cradle, held by four clamps.
            // QF13 (16): the ONE saucer prefab (exterior, beam, bike hold), unchanged. Clamped on its cradle its underside is at
            // SaucerBaseY (1.15 m), so its root sits HoverLift below the bay floor; lifting off by HoverLift puts it at its hover
            // height (3.35 m) - exactly where it hovers on Venus and Europa, so the beam reaches the floor everywhere.
            float b0 = L.SaucerBaseY;
            var saucer = PlaceSaucer(root, L.SaucerCentre + Vector3.down * (Seven.SaucerBeam.ShipBelly - b0));
            props.rimGlow.Add(saucer.Find("RimGlow").GetComponent<Renderer>()); props.rimGlow.Add(saucer.Find("BellyGlow").GetComponent<Renderer>());
            props.underLight = saucer.Find("UnderLight").GetComponent<Light>();
            var col = saucer.gameObject.AddComponent<CapsuleCollider>();
            col.direction = 0; col.center = new Vector3(0f, Seven.SaucerBeam.ShipBelly + 0.6f, 0f); col.radius = 0.9f; col.height = 11f;
            var sbeam = saucer.GetComponent<Seven.SaucerBeam>();
            props.hatch = sbeam.Hatch; props.hatchGlow = sbeam.HatchGlow; props.beamFloor = sbeam.BeamFloor; props.beamLight = sbeam.BeamLight; props.beam = sbeam.Beam;
            props.saucer = saucer;
            CheckSaucer("Chapter1", saucer);
            // Cradle (stays when she lifts off) and clamps.
            var cr = new List<Vector3>(); var crt = new List<int>();
            AddRing(cr, crt, L.SaucerCentre, 2.6f, 4.6f, 0.05f, 0f, 360f, 64);      // flat: 7 walks over it to the beam
            for (int i = 0; i < 6; i++) AddBox(cr, crt, L.SaucerCentre + Polar(3.5f, i * 60f + 30f, (b0 + 0.18f) * 0.5f), new Vector3(0.35f, b0, 0.35f));
            MeshObject("Cradle", SaveMesh(Finish(cr, crt), "Moon_Cradle"), m.metalDark, root);
            foreach (var a in new[] { 45f, 135f, 225f, 315f })
            {
                var clamp = new GameObject("Clamp" + a).transform; clamp.SetParent(root, false);
                clamp.position = L.SaucerCentre + Polar(6.25f, a, 0f);
                clamp.rotation = Quaternion.LookRotation(Polar(1f, a, 0f));
                var cv2 = new List<Vector3>(); var ct2 = new List<int>();
                AddBox(cv2, ct2, new Vector3(0f, (b0 + 1.1f) * 0.5f, 0.3f), new Vector3(0.6f, b0 + 1.1f, 0.6f));     // post
                AddBox(cv2, ct2, new Vector3(0f, b0 + 1.0f, -0.25f), new Vector3(0.55f, 0.28f, 1.2f));              // jaw over the rim
                AddBox(cv2, ct2, new Vector3(0f, b0 + 0.3f, -0.25f), new Vector3(0.55f, 0.22f, 1.0f));              // jaw under the rim
                var cgo = MeshObject("ClampBody", SaveMesh(Finish(cv2, ct2), "Moon_Clamp" + a), m.metal, clamp);
                Block("ClampLight", clamp.position + clamp.rotation * new Vector3(-0.29f, b0 + 0.9f, 0.4f), clamp.position + clamp.rotation * new Vector3(-0.27f, b0 + 1.0f, 0.5f), m.glow, clamp, false);
                props.clamps.Add(clamp);
                var cp = L.SaucerCentre + Polar(6.5f, a, 0f);
                props.extraObstacles.Add(new Vector3(cp.x, cp.z, 0.6f));
            }
        }

        // ------------------------------------------------------------------------------------ the far cavern
        static void BuildVista(Transform world, MoonMats m)
        {
            var root = new GameObject("Vista").transform; root.SetParent(world, false);
            var rnd = new System.Random(77);
            float R() => (float)rnd.NextDouble();
            // Across the void (+Z): mining scaffolds and lamps; huge ancient rings and monoliths half-buried, fading into the haze.
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < 9; i++)
            {
                var c = new Vector3(Mathf.Lerp(-50f, 70f, R()), -25f, Mathf.Lerp(28f, 70f, R()));
                float h = Mathf.Lerp(18f, 40f, R());
                AddBox(v, t, c + Vector3.up * h * 0.5f, new Vector3(1.2f, h, 1.2f));
                AddBox(v, t, c + new Vector3(3f, h * 0.5f, 0f), new Vector3(1.2f, h, 1.2f));
                for (float y = 4f; y < h; y += 5f) AddBox(v, t, c + new Vector3(1.5f, y, 0f), new Vector3(4.5f, 0.35f, 1.4f));
            }
            MeshObject("Scaffolds", SaveMesh(Finish(v, t), "Moon_Scaffolds"), m.metalDark, root, false);
            var gv = new List<Vector3>(); var gt = new List<int>();
            for (int i = 0; i < 16; i++) AddBox(gv, gt, new Vector3(Mathf.Lerp(-50f, 70f, R()), Mathf.Lerp(-12f, 12f, R()), Mathf.Lerp(30f, 72f, R())), new Vector3(1.2f, 0.4f, 0.4f));
            MeshObject("ScaffoldLamps", SaveMesh(Finish(gv, gt), "Moon_ScaffoldLamps"), m.glow, root, false);
            var av = new List<Vector3>(); var at = new List<int>();
            foreach (var (c, r, tilt) in new[] { (new Vector3(-30f, -12f, 120f), 38f, 20f), (new Vector3(70f, -30f, 160f), 50f, -35f), (new Vector3(10f, -40f, -170f), 60f, 15f), (new Vector3(-70f, -20f, -120f), 34f, 60f) })
            {
                var q = Quaternion.Euler(tilt, R() * 90f, 0f);
                Sweep(av, at, Enumerable.Range(0, 64).Select(k => c + q * new Vector3(Mathf.Cos(k / 64f * Mathf.PI * 2f) * r, Mathf.Sin(k / 64f * Mathf.PI * 2f) * r, 0f)).ToList(), r * 0.07f, true, 10);
            }
            for (int i = 0; i < 14; i++)
            {
                bool south = i % 2 == 0;
                var bpos = new Vector3(Mathf.Lerp(-90f, 110f, R()), -50f, south ? Mathf.Lerp(-260f, -120f, R()) : Mathf.Lerp(90f, 260f, R()));
                Spike(av, at, bpos, Mathf.Lerp(5f, 14f, R()), Mathf.Lerp(50f, 120f, R()), false, rnd);
            }
            // Round 7: the old rings + spikes clashed with the city (BuildCity) - no longer added to the scene.
            // MeshObject("AncientShapes", SaveMesh(SmoothFinish(av, at), "Moon_AncientShapes"), m.ancientDark, root, false);
            // (Round 7: the old far rock shell at 420 m is gone - the city inside the Moon replaces it, see BuildCity.)
        }
    }
}
