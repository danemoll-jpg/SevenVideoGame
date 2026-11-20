using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using M = Seven.MarsLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 15 — the Mars scene ("Mars — The Divided Kingdoms", Docs/CHAPTER_MARS.md), a first playable pass from simple
    /// shapes on the shared systems: an immense pale plain cut by walls, trenches and elevated roads; two fortress cities far
    /// off either side of a deep ravine; the west settlement (main street, market stalls, side streets), the checkpoint at the
    /// main crossing (barrier, stranded transport, Viper's platform); the maintenance route (the west ramp cut into the bank,
    /// the ravine floor, the broken causeway + its fallen slab, a sheltered passage, lookouts, the east ramp and its service
    /// gate); the east bank, the rims, the shared mechanism on its overlook, the two controls, the relay, the pier walk; the
    /// ancient bridge's two spans (segments misaligned until section 6); the temple island (three lanes with low walls and
    /// gaps, Viper's barriers, the moving section, the broken strip + walkway section, three control pylons) and the temple
    /// (the symbols of both kingdoms as one design, the tetrahedron rune, the display). Characters on Dan's Mixamo rigs:
    /// Viper (Art/Mixamo/viper) and the Reptilians (Art/Mixamo/reptilian; one model, ±10 % sizes, three tones, the guards in a
    /// helmet and a chest plate). Numbers: Seven.MarsLayout. Atmosphere: Atmosphere_Mars (Kyle's fog / glow: dark near
    /// silhouettes, a bright glowing hazy distance, strong ground fog in the ravine, huge structures fading into the haze).
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const string MarsScenePath = "Assets/Scenes/Mars.unity";
        const string AtmoMarsPath = AtmoDir + "/Atmosphere_Mars.prefab";
        const string ReptDir = CharRoot + "/Reptilian", ViperDir = CharRoot + "/Viper";

        static void BuildRound15(AnimatorController sevenCtl, GameObject atmoPrefab, Seven.VoiceBank bank)
        {
            var rept = SetupMixamoFolk("reptilian", ReptDir, out var reptCtl, out var reptMat);
            var viper = SetupMixamoFolk("viper", ViperDir, out var viperCtl, out var viperMat);
            var bike = AssetDatabase.LoadAssetAtPath<GameObject>(CharRoot + "/hoverbike/hoverbike_v1.fbx");
            var atmo = BuildAtmosphereMars(atmoPrefab);
            CopyVideo("vision_04_mars.mp4");
            BuildMarsScene(sevenCtl, bank, atmo, rept, reptCtl, reptMat, viper, viperCtl, viperMat, bike);
        }

        static void CopyVideo(string file)
        {
            var src = Path.Combine(RepoRoot, "Art", "Video", file);
            if (!File.Exists(src)) { Log($"WARNING: Art/Video/{file} missing - the still fallback plays"); return; }
            Directory.CreateDirectory(Path.Combine(ProjectRoot, "Assets/Video"));
            File.Copy(src, Path.Combine(ProjectRoot, "Assets/Video/" + file), true);
            AssetDatabase.ImportAsset("Assets/Video/" + file, ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>Dan's Mixamo body (FBX for Unity, with skin) as a Humanoid, its grayscale texture, and an animator from the
        /// clips MODELS-NEEDED "Mars animation clips" lists (Reptilians: Dan's walk + wave, the Elarim idle / talk / point /
        /// carry; Viper: the Tunneler idle / walk / roar, the Elarim talk / point / kneel pose, the Y Bot hit / knocked down / dazed).</summary>
        static GameObject SetupMixamoFolk(string kind, string dir, out AnimatorController ctl, out Material mat)
        {
            ctl = null; mat = null;
            var src = Path.Combine(RepoRoot, "Art", "Mixamo", kind, kind + "_body_mixamo.fbx");
            if (!File.Exists(src)) { Log($"WARNING: {src} missing - {kind} will be a placeholder"); return null; }
            Directory.CreateDirectory(Path.Combine(ProjectRoot, dir, "Textures"));
            Directory.CreateDirectory(Path.Combine(ProjectRoot, dir, "Clips"));
            var path = dir + "/" + kind + "_body_mixamo.fbx";
            File.Copy(src, Path.Combine(ProjectRoot, path), true);
            var tex = Path.Combine(RepoRoot, "Art", "Export", kind, "Textures", kind + "_v1_basecolor.jpg");
            var texPath = dir + "/Textures/" + kind + "_v1_basecolor.jpg";
            if (File.Exists(tex)) File.Copy(tex, Path.Combine(ProjectRoot, texPath), true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            imp.animationType = ModelImporterAnimationType.Human; imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = false; imp.materialImportMode = ModelImporterMaterialImportMode.None; imp.isReadable = true; imp.SaveAndReimport();
            ApplyMixamoHumanMap(imp, path);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            Log($"{path}: {kind} avatar human={avatar?.isHuman} valid={avatar?.isValid}");
            if (avatar == null || !avatar.isHuman) return null;
            mat = TexturedMat("Mars_" + kind, texPath, kind == "viper" ? 0.55f : 0.35f, 0f);
            mat.SetFloat("_Rim", 0.35f); EditorUtility.SetDirty(mat);
            // the clips: (state, source fbx, loop)
            var list = new List<(string state, string file, bool loop)>();
            string E(string n) => Path.Combine(RepoRoot, "Art", "Mixamo", "elarim", n);
            string T(string n) => Path.Combine(RepoRoot, "Art", "Mixamo", "tunneler", n);
            string Y(string n) => Path.Combine(RepoRoot, "Art", "Mixamo", "YBot", n);
            if (kind == "reptilian")
            {
                list.Add(("idle", E("elarim_idle.fbx"), true)); list.Add(("talk", E("elarim_talk.fbx"), true)); list.Add(("point", E("elarim_point.fbx"), false));
                list.Add(("carry", E("elarim_carry.fbx"), true)); list.Add(("walk", Path.Combine(RepoRoot, "Art", "Mixamo", "reptilian", "reptilian_walk.fbx"), true));
                list.Add(("wave", Path.Combine(RepoRoot, "Art", "Mixamo", "reptilian", "reptilian_wave.fbx"), false));
            }
            else
            {
                list.Add(("idle", T("tunneler_idle.fbx"), true)); list.Add(("walk", T("tunneler_walk.fbx"), true)); list.Add(("roar", T("tunneler_roar.fbx"), false));
                list.Add(("talk", E("elarim_talk.fbx"), true)); list.Add(("point", E("elarim_point.fbx"), false));
                list.Add(("hit", Y("ybot_hit_body.fbx"), false)); list.Add(("down", Y("ybot_knocked_down.fbx"), false)); list.Add(("dazed", Y("ybot_dazed.fbx"), true));
                var kp = Path.Combine(RepoRoot, "Art", "Export", "anim", "elarim_kneel_pose.fbx");
                list.Add(("kneel", File.Exists(kp) ? kp : E("elarim_kneel.fbx"), true));
            }
            var cpath = AnimDir + "/" + (kind == "viper" ? "Viper" : "Reptilian") + ".controller";
            AssetDatabase.DeleteAsset(cpath);
            ctl = AnimatorController.CreateAnimatorControllerAtPath(cpath);
            var sm = ctl.layers[0].stateMachine;
            foreach (var (state, file, loop) in list)
            {
                if (!File.Exists(file)) { Log($"WARNING: {kind} clip {state}: {file} missing"); continue; }
                var dst = dir + "/Clips/" + kind + "_" + state + ".fbx";
                File.Copy(file, Path.Combine(ProjectRoot, dst), true);
                AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceSynchronousImport);
                var ci = (ModelImporter)AssetImporter.GetAtPath(dst);
                ci.animationType = ModelImporterAnimationType.Human; ci.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                ci.materialImportMode = ModelImporterMaterialImportMode.None; ci.importAnimation = true; ci.SaveAndReimport();
                ApplyMixamoHumanMap(ci, dst);
                var cl = ci.defaultClipAnimations;
                // in place: the walk's forward travel is not applied (MarsFolk moves them and matches the playback)
                foreach (var c in cl) { c.name = state; c.loopTime = loop; c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true; c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true; }
                ci.clipAnimations = cl; ci.SaveAndReimport();
                var clip = AssetDatabase.LoadAllAssetsAtPath(dst).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                if (clip == null) continue;
                var st = sm.AddState(state); st.motion = clip; if (state == "idle") sm.defaultState = st;
                Log($"{kind} clip {state} {clip.length:0.00}s humanMotion={clip.humanMotion} (from {Path.GetFileName(file)})");
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static GameObject BuildAtmosphereMars(GameObject atmoPrefab)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(atmoPrefab);
            var a = inst.GetComponent<Seven.Atmosphere>();
            // Kyle's fog / glow on Mars: dark near silhouettes against a bright glowing hazy distance, strong ground fog
            // pooling in the ravine, the fortress cities and the temple fading into the haze
            a.FogStart = 10f;
            a.GroundDensity = 0.012f; a.GroundBase = M.FloorY - 2f; a.GroundFalloff = 9f; a.GroundRange = 120f;
            a.ValleyDensity = 0f;
            a.DistanceDensity = 0.0028f; a.HazeHeight = 140f; a.MaxFog = 0.86f;
            a.NearBrightness = 0.12f; a.FarBrightness = 0.66f; a.BrightnessDistance = 220f; a.ZenithBrightness = 0.72f;
            a.NoiseStrength = 0.4f; a.NoiseScale = 0.03f; a.NoiseSpeed = 0.025f; a.NoiseFadeDistance = 90f;
            a.GlowIntensity = 0.36f; a.GlowRadius = 60f;
            var glows = new[]
            {
                (new Vector3(0f, 18f, 175f), new Vector3(5f, 2f, 1f)), (new Vector3(-480f, 30f, 0f), new Vector3(8f, 3f, 1f)),
                (new Vector3(480f, 30f, 0f), new Vector3(8f, 3f, 1f)), (new Vector3(0f, 60f, 520f), new Vector3(10f, 3f, 1f)),
                (new Vector3(0f, -10f, -70f), new Vector3(4f, 1.4f, 1f)), (new Vector3(-120f, 10f, 0f), new Vector3(3f, 1.4f, 1f)),
                (new Vector3(60f, 8f, 122f), new Vector3(2.4f, 1.2f, 1f)), (new Vector3(0f, 50f, -420f), new Vector3(9f, 3f, 1f)),
            };
            int gi = 0;
            foreach (Transform c in inst.transform)
            {
                if (c.name.StartsWith("Glow") && gi < glows.Length) { c.position = glows[gi].Item1; c.localScale = glows[gi].Item2; gi++; }
                if (c.name == "BackLight") { c.rotation = Quaternion.Euler(28f, 200f, 0f); var l = c.GetComponent<Light>(); l.intensity = 1.1f; l.shadowStrength = 0.6f; }
                if (c.name == "FrontFill") { c.rotation = Quaternion.Euler(50f, 30f, 0f); c.GetComponent<Light>().intensity = 0.45f; }
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(inst, AtmoMarsPath);
            Object.DestroyImmediate(inst);
            return prefab;
        }

        struct MaMats { public Material sand, rock, rockDark, wall, stone, stoneDark, metal, metalDark, glow, cloth, barrier, temple, panel; }

        static void BuildMarsScene(AnimatorController sevenCtl, Seven.VoiceBank bank, GameObject atmoP, GameObject rept, AnimatorController reptCtl, Material reptMat, GameObject viper, AnimatorController viperCtl, Material viperMat, GameObject bike)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null; RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.36f, 0.36f, 0.36f);
            var shs = new SphericalHarmonicsL2(); shs.AddAmbientLight(RenderSettings.ambientLight); RenderSettings.ambientProbe = shs;
            var atmo = (GameObject)PrefabUtility.InstantiatePrefab(atmoP); atmo.name = "Atmosphere_Mars";
            var world = new GameObject("Mars").transform;
            var rnd = new System.Random(15);
            var m = new MaMats
            {
                sand = Dissolve("Mars_sand", Gray(0.6f), 0.15f, false), rock = Dissolve("Mars_rock", Gray(0.38f), 0.2f, false), rockDark = Dissolve("Mars_rock_dark", Gray(0.2f), 0.15f, false),
                wall = Dissolve("Mars_wall", Gray(0.5f), 0.25f, false), stone = Dissolve("Mars_stone", Gray(0.76f), 0.3f, false), stoneDark = Dissolve("Mars_stone_dark", Gray(0.3f), 0.25f, false),
                metal = Dissolve("Mars_metal", Gray(0.4f), 0.6f, false, 0.5f), metalDark = Dissolve("Mars_metal_dark", Gray(0.16f), 0.5f, false, 0.4f), glow = Dissolve("Mars_glow", Color.white, 0.5f, false),
                cloth = Dissolve("Mars_cloth", Gray(0.66f), 0.1f, false), barrier = AdditiveMat("Mars_barrier", 0.9f), temple = Dissolve("Mars_temple", Gray(0.84f), 0.35f, false),
                panel = Dissolve("Mars_panel", Gray(0.3f), 0.4f, false),
            };
            m.glow.EnableKeyword("_EMISSION"); m.glow.SetColor("_EmissionColor", Color.white * 2.2f); EditorUtility.SetDirty(m.glow);
            foreach (var x in new[] { m.sand, m.rock, m.rockDark, m.wall, m.stone, m.stoneDark, m.metal, m.metalDark, m.temple }) { x.SetFloat("_Cull", 0f); EditorUtility.SetDirty(x); }
            Material GlowMat(string n) { var g = Dissolve(n, Gray(0.5f), 0.4f, false); g.EnableKeyword("_EMISSION"); g.SetColor("_EmissionColor", Color.white * 0.25f); EditorUtility.SetDirty(g); return g; }

            // ---------------------------------------------------------------- the plain, the ravine, the trenches for the ramps
            var land = new GameObject("Land").transform; land.SetParent(world, false);
            float fy = M.FloorY;
            Block("PlainW_A", new Vector3(-900f, fy - 2f, -700f), new Vector3(-60f, 0f, 700f), m.sand, land, false);
            Block("PlainW_B", new Vector3(-60f, fy - 2f, -700f), new Vector3(-50f, 0f, -62f), m.sand, land, false);
            Block("PlainW_C", new Vector3(-60f, fy - 2f, -11f), new Vector3(-50f, 0f, 700f), m.sand, land, false);
            Block("PlainE_A", new Vector3(60f, fy - 2f, -700f), new Vector3(900f, 0f, 700f), m.sand, land, false);
            Block("PlainE_B", new Vector3(50f, fy - 2f, -700f), new Vector3(60f, 0f, -62f), m.sand, land, false);
            Block("PlainE_C", new Vector3(50f, fy - 2f, -12.6f), new Vector3(60f, 0f, 700f), m.sand, land, false);
            Block("RavineFloor", new Vector3(-50f, fy - 2f, -700f), new Vector3(50f, fy, 700f), m.rock, land, false);
            // the ramps cut into the banks (a slope surface from the floor up to the bank top)
            {
                var v = new List<Vector3>(); var t = new List<int>();
                foreach (var (x0, x1, z0, z1) in new[] { (-60f, -50f, -62f, -11f), (50f, 60f, -62f, -12.6f) })
                {
                    Quad(v, t, new Vector3(x0, fy, z0), new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1), new Vector3(x1, fy, z0));
                    // a low parapet on the ravine side
                    float xp = x0 < 0 ? x1 - 0.3f : x0 + 0.3f;
                    Quad(v, t, new Vector3(xp, fy + 0.8f, z0), new Vector3(xp, 0.8f, z1), new Vector3(xp, 0f, z1), new Vector3(xp, fy, z0));
                }
                MeshObject("MaintRamps", SaveMesh(Finish(v, t), "Mars_MaintRamps"), m.rock, land);
            }
            // the ravine's walls: rough rock faces (buttresses, ledges) over the block sides
            {
                var v = new List<Vector3>(); var t = new List<int>();
                for (int i = 0; i < 70; i++)
                {
                    float z = -300f + i * 9f + (float)rnd.NextDouble() * 4f; if (z > -66f && z < -8f) continue;
                    foreach (var sx in new[] { -1f, 1f }) Lump(v, t, new Vector3(sx * (M.RavineX - 1.5f), fy + 6f + (float)rnd.NextDouble() * 8f, z), new Vector3(3f, 9f + (float)rnd.NextDouble() * 6f, 4.5f), 0.5f, rnd.Next(1000));
                }
                MeshObject("RavineWallRock", SaveMesh(SmoothFinish(v, t), "Mars_RavineRock"), m.rockDark, land);
            }
            // walls, trenches and elevated roads across the plain (outside the play areas)
            {
                var v = new List<Vector3>(); var t = new List<int>(); var tv = new List<Vector3>(); var tt = new List<int>(); var rv = new List<Vector3>(); var rt = new List<int>();
                foreach (var (x, z, len, yaw) in new[] { (-300f, -160f, 320f, 12f), (-260f, 180f, 280f, -18f), (300f, -150f, 320f, -10f), (280f, 190f, 300f, 22f), (-420f, 60f, 260f, 80f), (420f, -40f, 240f, 95f), (-150f, 320f, 200f, 5f), (160f, -320f, 220f, -6f) })
                    AddBox(v, t, new Vector3(x, 2.5f, z), new Vector3(len, 5f, 2.2f), yaw);
                foreach (var (x, z, len, yaw) in new[] { (-330f, -60f, 240f, 70f), (340f, 70f, 260f, 110f), (-220f, 260f, 200f, 30f), (230f, -250f, 200f, -40f) })
                { AddBox(tv, tt, new Vector3(x, 0.03f, z), new Vector3(len, 0.06f, 5f), yaw); AddBox(v, t, new Vector3(x, 0.4f, z) + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 3f, new Vector3(len, 0.8f, 0.8f), yaw); AddBox(v, t, new Vector3(x, 0.4f, z) - Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 3f, new Vector3(len, 0.8f, 0.8f), yaw); }
                foreach (var (x0, z0, x1, z1) in new[] { (-640f, -40f, -260f, -30f), (260f, 30f, 640f, 40f), (-300f, 240f, -120f, 420f), (130f, -420f, 300f, -240f) })
                {
                    var a = new Vector3(x0, 7f, z0); var b = new Vector3(x1, 7f, z1); var dd = b - a; float yaw = Mathf.Atan2(dd.x, dd.z) * Mathf.Rad2Deg;
                    AddBox(rv, rt, (a + b) * 0.5f, new Vector3(6f, 0.8f, dd.magnitude), yaw);
                    for (float s = 0f; s <= 1f; s += 0.08f) AddBox(rv, rt, Vector3.Lerp(a, b, s) + Vector3.down * 3.6f, new Vector3(1.6f, 7f, 1.6f), yaw);
                }
                MeshObject("PlainWalls", SaveMesh(Finish(v, t), "Mars_PlainWalls"), m.wall, land);
                MeshObject("PlainTrenches", SaveMesh(Finish(tv, tt), "Mars_Trenches"), m.rockDark, land, false);
                MeshObject("ElevatedRoads", SaveMesh(Finish(rv, rt), "Mars_ElevatedRoads"), m.stoneDark, land);
            }
            // the two fortress cities either side of the ravine (silhouettes in the haze): walls, towers, stacked blocks
            foreach (var sx in new[] { -1f, 1f })
            {
                var v = new List<Vector3>(); var t = new List<int>();
                var c = new Vector3(sx * 520f, 0f, 20f);
                for (int i = 0; i < 16; i++) { float a = i * 22.5f; var p = c + Polar(170f, a, 0f); AddBox(v, t, p + Vector3.up * 14f, new Vector3(18f, 28f, 18f), -a); var q = c + Polar(170f, a + 11.25f, 0f); AddBox(v, t, q + Vector3.up * 9f, new Vector3(66f, 18f, 7f), -(a + 11.25f) + 90f); }
                for (int i = 0; i < 46; i++) { var p = c + new Vector3((float)rnd.NextDouble() * 260f - 130f, 0f, (float)rnd.NextDouble() * 260f - 130f); if ((p - c).magnitude > 150f) continue; float h = 16f + (float)rnd.NextDouble() * 50f; AddBox(v, t, p + Vector3.up * h * 0.5f, new Vector3(14f + (float)rnd.NextDouble() * 18f, h, 14f + (float)rnd.NextDouble() * 18f), (float)rnd.NextDouble() * 90f); }
                AddBox(v, t, c + Vector3.up * 55f, new Vector3(26f, 110f, 26f));
                MeshObject(sx < 0 ? "FortressCityWest" : "FortressCityEast", SaveMesh(Finish(v, t), sx < 0 ? "Mars_CityWest_Distant" : "Mars_CityEast_Distant"), m.wall, world);
            }

            // ---------------------------------------------------------------- the west settlement + the east side's buildings
            var town = new GameObject("Settlement").transform; town.SetParent(world, false);
            {
                var v = new List<Vector3>(); var t = new List<int>(); var dv = new List<Vector3>(); var dt = new List<int>(); var lv = new List<Vector3>(); var lt = new List<int>();
                void House(Vector3 p, Vector3 size, float yaw)
                {
                    AddBox(v, t, p + Vector3.up * size.y * 0.5f, size, yaw);
                    var dome = Lathe(new[] { new Vector2(Mathf.Min(size.x, size.z) * 0.45f, 0f), new Vector2(Mathf.Min(size.x, size.z) * 0.32f, size.y * 0.25f), new Vector2(0f, size.y * 0.36f) }, 0f, 360f, 12, null, p + Vector3.up * size.y);
                    int b = dv.Count; dv.AddRange(dome.vertices); dt.AddRange(dome.triangles.Select(k => k + b));
                    AddBox(lv, lt, p + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, size.y * 0.55f, -size.z * 0.5f - 0.02f), new Vector3(size.x * 0.25f, 0.5f, 0.05f), yaw);
                }
                // along the main street (both sides), round the market, along the side streets
                for (float x = -178f; x < -72f; x += 11f)
                    foreach (var sz in new[] { -1f, 1f })
                    {
                        if (x > -140f && x < -100f) continue;
                        var p = new Vector3(x + (float)rnd.NextDouble() * 2f, 0f, sz * (11f + (float)rnd.NextDouble() * 3f));
                        if (sz > 0 && x > -128f && x < -112f) continue; if (sz < 0 && x > -148f && x < -132f) continue;
                        House(p, new Vector3(8f + (float)rnd.NextDouble() * 2f, 5f + (float)rnd.NextDouble() * 4f, 8f), sz > 0 ? 180f : 0f);
                    }
                foreach (var a in new[] { 20f, 60f, 110f, 160f, 200f, 250f, 290f, 340f }) { var p = M.MarketAt + Polar(23f, a, 0f); if (Mathf.Abs(p.z) < 7f) continue; House(p, new Vector3(9f, 6f + (float)rnd.NextDouble() * 3f, 9f), -a + 90f); }
                for (float z = 18f; z < 42f; z += 9f) { House(new Vector3(-131f, 0f, z), new Vector3(7f, 6f, 8f), 90f); House(new Vector3(-109f, 0f, z), new Vector3(7f, 6f, 8f), -90f); }
                for (float z = -40f; z < -8f; z += 9f) { House(new Vector3(-151f, 0f, z), new Vector3(7f, 6f, 8f), 90f); House(new Vector3(-129f, 0f, z), new Vector3(7f, 6f, 8f), -90f); }
                // the east side: a few buildings by the east checkpoint and along the east road
                for (float x = 76f; x < 150f; x += 12f) foreach (var sz in new[] { -1f, 1f }) House(new Vector3(x, 0f, sz * 12f), new Vector3(8f, 6f, 8f), sz > 0 ? 180f : 0f);
                MeshObject("Houses", SaveMesh(Finish(v, t), "Mars_Houses"), m.wall, town);
                MeshObject("HouseDomes", SaveMesh(SmoothFinish(dv, dt), "Mars_HouseDomes"), m.stone, town);
                MeshObject("HouseLights", SaveMesh(Finish(lv, lt), "Mars_HouseLights"), m.glow, town, false);
                // market stalls (awnings on posts, goods on tables)
                var sv = new List<Vector3>(); var st = new List<int>(); var gv = new List<Vector3>(); var gt = new List<int>();
                foreach (var a in new[] { 0f, 45f, 135f, 180f, 225f, 315f })
                {
                    var p = M.MarketAt + Polar(11f, a, 0f); var q = Quaternion.Euler(0f, -a, 0f);
                    AddBox(sv, st, p + Vector3.up * 2.6f, new Vector3(3.6f, 0.1f, 2.4f), -a);
                    foreach (var (ox, oz) in new[] { (-1.6f, -1.1f), (1.6f, -1.1f), (-1.6f, 1.1f), (1.6f, 1.1f) }) AddBox(sv, st, p + q * new Vector3(ox, 1.3f, oz), new Vector3(0.12f, 2.6f, 0.12f), -a);
                    AddBox(gv, gt, p + Vector3.up * 0.9f, new Vector3(2.8f, 0.12f, 1.4f), -a);
                    for (int k = 0; k < 4; k++) AddBox(gv, gt, p + q * new Vector3(-1f + k * 0.65f, 1.15f, 0f), new Vector3(0.4f, 0.4f, 0.4f), -a + k * 11f);
                }
                MeshObject("MarketAwnings", SaveMesh(Finish(sv, st), "Mars_Awnings"), m.cloth, town);
                MeshObject("MarketGoods", SaveMesh(Finish(gv, gt), "Mars_Goods"), m.stoneDark, town);
                Lamp("MarketLamp", M.MarketAt + Vector3.up * 5f, 26f, 1.4f, m.glow, town, new Vector3(0.5f, 0.5f, 0.5f));
                for (float x = -170f; x < -70f; x += 22f) Lamp($"StreetLamp{(int)-x}", new Vector3(x, 4.2f, 6.8f), 14f, 0.9f, m.glow, town, new Vector3(0.3f, 0.3f, 0.3f));
            }

            // ---------------------------------------------------------------- the crossing: the main bridge, checkpoints, barrier, platform, transport
            var cross = new GameObject("Crossing").transform; cross.SetParent(world, false);
            Block("MainBridgeDeck", new Vector3(-50f, -1.2f, -4.5f), new Vector3(50f, 0f, 4.5f), m.stoneDark, cross, false);
            Block("MainBridgeRailS", new Vector3(-50f, 0f, -4.6f), new Vector3(50f, 1.1f, -4.2f), m.wall, cross, false);
            Block("MainBridgeRailN", new Vector3(-50f, 0f, 4.2f), new Vector3(50f, 1.1f, 4.6f), m.wall, cross, false);
            for (int i = -2; i <= 2; i++) Block($"MainBridgePier{i + 2}", new Vector3(i * 20f - 2f, fy, -3f), new Vector3(i * 20f + 2f, -1.2f, 3f), m.wall, cross, false);
            foreach (var sx in new[] { -1f, 1f })
            {
                Block(sx < 0 ? "CheckTowerW_S" : "CheckTowerE_S", new Vector3(sx * 52f - 2f, 0f, -9.5f), new Vector3(sx * 52f + 2f, 9f, -5.5f), m.wall, cross);
                Block(sx < 0 ? "CheckTowerW_N" : "CheckTowerE_N", new Vector3(sx * 52f - 2f, 0f, 5.5f), new Vector3(sx * 52f + 2f, 9f, 9.5f), m.wall, cross);
                Lamp(sx < 0 ? "CheckLampW" : "CheckLampE", new Vector3(sx * 52f, 9.6f, 0f), 30f, 1.6f, m.glow, cross, new Vector3(0.6f, 0.4f, 0.6f));
            }
            var barrier = Block("CheckpointBarrier", M.BarrierAt + new Vector3(-0.15f, 0f, -4.4f), M.BarrierAt + new Vector3(0.15f, 3.4f, 4.4f), m.barrier, cross, false);
            barrier.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Block("BarrierEmitterS", M.BarrierAt + new Vector3(-0.4f, 0f, -4.8f), M.BarrierAt + new Vector3(0.4f, 3.8f, -4.3f), m.metalDark, cross);
            Block("BarrierEmitterN", M.BarrierAt + new Vector3(-0.4f, 0f, 4.3f), M.BarrierAt + new Vector3(0.4f, 3.8f, 4.8f), m.metalDark, cross);
            // Viper's elevated platform (rises in section 3; harsh white light from beneath)
            var plat = new GameObject("ViperPlatform").transform; plat.SetParent(cross, false); plat.position = M.PlatformAt + Vector3.down * 3f;
            { var b = Block("PlatformDeck", plat.position + new Vector3(-2.2f, -0.4f, -2.2f), plat.position + new Vector3(2.2f, 0f, 2.2f), m.metalDark, plat, false);
              Block("PlatformColumn", plat.position + new Vector3(-0.8f, -12f, -0.8f), plat.position + new Vector3(0.8f, -0.4f, 0.8f), m.metal, plat, false);
              Block("PlatformUnderGlow", plat.position + new Vector3(-1.8f, -0.48f, -1.8f), plat.position + new Vector3(1.8f, -0.42f, 1.8f), m.glow, plat, false); }
            var plight = new GameObject("PlatformLight").AddComponent<Light>(); plight.transform.SetParent(plat, false); plight.transform.position = plat.position + Vector3.down * 0.8f; plight.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            plight.type = LightType.Spot; plight.spotAngle = 80f; plight.range = 14f; plight.intensity = 6f; plight.shadows = LightShadows.Soft; plight.enabled = false;
            // the supply transport (a cargo hover-cart with crates of water filters)
            var transport = new GameObject("SupplyTransport").transform; transport.SetParent(cross, false); transport.position = M.TransportAt;
            {
                var v = new List<Vector3>(); var t = new List<int>();
                AddBox(v, t, new Vector3(0f, 0.9f, 0f), new Vector3(2.4f, 0.9f, 5.2f)); AddBox(v, t, new Vector3(0f, 1.8f, 1.8f), new Vector3(2.0f, 1.0f, 1.4f));
                for (int i = 0; i < 6; i++) AddBox(v, t, new Vector3(-0.55f + (i % 2) * 1.1f, 1.75f, -1.6f + (i / 2) * 1.05f), new Vector3(0.9f, 0.8f, 0.9f));
                var tm = MeshObject("TransportBody", SaveMesh(Finish(v, t), "Mars_Transport"), m.metal, transport, true); tm.transform.localPosition = Vector3.zero;
                var g = Block("TransportVent", new Vector3(-0.9f, 0.38f, -2.2f), new Vector3(0.9f, 0.42f, 2.2f), m.glow, transport, false); g.transform.localPosition = Vector3.zero;
                transport.rotation = Quaternion.Euler(0f, 90f, 0f);
            }

            // ---------------------------------------------------------------- the maintenance route on the ravine floor
            var maint = new GameObject("MaintenanceRoute").transform; maint.SetParent(world, false);
            {
                var v = new List<Vector3>(); var t = new List<int>();
                // the channel across the floor (dark), the broken causeway's two stubs over it
                Block("Channel", new Vector3(-7f, fy - 1.6f, -700f), new Vector3(7f, fy + 0.02f, 700f), m.rockDark, maint, false);
                Block("CausewayStubW", new Vector3(-8.5f, fy - 1.6f, -70f), new Vector3(-3.5f, fy + 0.02f, -64f), m.stone, maint, false);
                Block("CausewayStubE", new Vector3(3.5f, fy - 1.6f, -70f), new Vector3(8.5f, fy + 0.02f, -64f), m.stone, maint, false);
                // the sheltered passage: a carved roof on columns over the east floor
                Block("ShelterRoof", new Vector3(18f, fy + 6.5f, -92f), new Vector3(42f, fy + 7.4f, -62f), m.stoneDark, maint, false);
                for (float z = -90f; z <= -64f; z += 6.5f) foreach (var x in new[] { 18.5f, 41.5f }) Block($"ShelterColumn{(int)x}_{(int)-z}", new Vector3(x - 0.6f, fy, z - 0.6f), new Vector3(x + 0.6f, fy + 6.5f, z + 0.6f), m.stone, maint, false);
                // lookouts with lamps (clear stopping points)
                foreach (var p in M.Lookouts) { Lamp($"LookoutLamp{(int)p.x}", p + Vector3.up * 3.2f, 18f, 1.2f, m.glow, maint, new Vector3(0.35f, 0.35f, 0.35f)); Block($"LookoutPost{(int)p.x}", p + new Vector3(-0.15f, 0f, -0.15f), p + new Vector3(0.15f, 3f, 0.15f), m.metal, maint, false); }
                // rubble along the floor (props, outside the zone edges)
                for (int i = 0; i < 40; i++) { var p = new Vector3((float)rnd.NextDouble() * 96f - 48f, fy, -96f - (float)rnd.NextDouble() * 60f); Lump(v, t, p + Vector3.up * 0.8f, Vector3.one * (0.8f + (float)rnd.NextDouble() * 1.6f), 0.4f, rnd.Next(1000)); }
                for (int i = 0; i < 30; i++) { var p = new Vector3((float)rnd.NextDouble() * 96f - 48f, fy, -10f + (float)rnd.NextDouble() * 120f); if (Mathf.Abs(p.x) < 8f) continue; Lump(v, t, p + Vector3.up * 0.8f, Vector3.one * (0.8f + (float)rnd.NextDouble() * 1.8f), 0.4f, rnd.Next(1000)); }
                MeshObject("FloorRubble", SaveMesh(SmoothFinish(v, t), "Mars_FloorRubble"), m.rock, maint);
            }
            var slab = Block("CausewaySlab", new Vector3(-3.5f, -0.3f, -3f), new Vector3(3.5f, 0.3f, 3f), m.stone, maint, false).transform;
            { var ctr = slab.GetComponent<MeshFilter>(); slab.position = M.CausewaySlabRest; slab.rotation = Quaternion.Euler(8f, 30f, -6f); }
            var slabEdges = Block("CausewaySlabEdges", new Vector3(-3.55f, 0.28f, -3.05f), new Vector3(3.55f, 0.34f, 3.05f), GlowMat("Mars_slab_edges"), slab, false).GetComponent<Renderer>();
            slabEdges.transform.localPosition = Vector3.zero; slabEdges.transform.localRotation = Quaternion.identity;
            var gate = Block("ServiceGate", new Vector3(-5f, 0f, -0.3f), new Vector3(5f, 4.5f, 0.3f), m.metalDark, maint, false).transform; gate.position = M.ServiceGateAt + new Vector3(-3f, 0f, 0f);
            var gatePanel = Block("ServicePanel", M.ServiceGatePanel + new Vector3(-0.3f, 0.4f, -0.2f), M.ServiceGatePanel + new Vector3(0.3f, 1.4f, 0.2f), m.panel, maint, false).GetComponent<Renderer>();
            gatePanel.transform.position = M.ServiceGatePanel + new Vector3(1.2f, -0.9f, 0f);

            // ---------------------------------------------------------------- the rims, the overlook + mechanism, the controls, the relay, the pier walk
            var anc = new GameObject("Ancient").transform; anc.SetParent(world, false);
            var mech = MeshObject("SharedMechanism", SaveMesh(Lathe(new[] { new Vector2(0f, 0f), new Vector2(2.6f, 0f), new Vector2(2.4f, 0.8f), new Vector2(1.6f, 1.2f), new Vector2(1.7f, 2.2f), new Vector2(0.8f, 2.6f), new Vector2(0f, 2.7f) }, 0f, 360f, 24, null, M.MechanismAt), "Mars_Mechanism"), m.stone, anc).transform;
            Renderer ChannelLine(string n, Vector3 a, Vector3 b) { var v = new List<Vector3>(); var t = new List<int>(); Sweep(v, t, new[] { a, (a + b) * 0.5f + Vector3.up * 0.2f, b }, 0.12f, false, 6); return MeshObject(n, SaveMesh(SmoothFinish(v, t), "Mars_" + n), GlowMat("Mars_" + n + "_mat"), anc, false).GetComponent<Renderer>(); }
            var chE = ChannelLine("ChannelEast", M.EastControl + Vector3.up * 0.05f, M.MechanismAt + Vector3.up * 0.05f);
            var chW = ChannelLine("ChannelWest", M.WestControl + Vector3.up * 0.05f, M.RelayWest + Vector3.up * 0.05f);
            Renderer Control(string n, Vector3 p) { MeshObject(n + "Base", SaveMesh(Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.9f, 0f), new Vector2(0.7f, 1.1f), new Vector2(0f, 1.15f) }, 0f, 360f, 16, null, p), "Mars_" + n + "Base"), m.stone, anc); return Block(n + "Top", p + new Vector3(-0.5f, 1.12f, -0.5f), p + new Vector3(0.5f, 1.22f, 0.5f), GlowMat("Mars_" + n + "_glow"), anc, false).GetComponent<Renderer>(); }
            var ctlW = Control("ControlWest", M.WestControl); var ctlE = Control("ControlEast", M.EastControl);
            var relay = Block("RelayPylonGlow", M.RelayWest + new Vector3(-0.25f, 2.6f, -0.25f), M.RelayWest + new Vector3(0.25f, 3.1f, 0.25f), GlowMat("Mars_relay_glow"), anc, false).GetComponent<Renderer>();
            Block("RelayPylon", M.RelayWest + new Vector3(-0.3f, 0f, -0.3f), M.RelayWest + new Vector3(0.3f, 2.6f, 0.3f), m.metal, anc, false);
            // the pier walk: steps down the side of the old bridge and a ledge walk with the displaced conduit at its end
            {
                var v = new List<Vector3>(); var t = new List<int>();
                Quad(v, t, new Vector3(-54.6f, 0f, 156f), new Vector3(-54.6f, 0f, 164f), new Vector3(-48f, -4f, 164f), new Vector3(-48f, -4f, 156f));
                MeshObject("PierStairs", SaveMesh(Finish(v, t), "Mars_PierStairs"), m.stone, anc);
                Block("PierWalkDeck", new Vector3(-48.6f, -4.6f, 155f), new Vector3(-44f, -4f, 172f), m.stone, anc, false);
                for (float z = 156f; z < 172f; z += 4f) Block($"PierBracket{(int)z}", new Vector3(-48f, -9f, z - 0.4f), new Vector3(-46f, -4.6f, z + 0.4f), m.stoneDark, anc, false);
                Block("PierRail", new Vector3(-44.3f, -4f, 155f), new Vector3(-44f, -3f, 172f), m.stoneDark, anc, false);
            }
            var conduit = MeshObject("PierConduit", SaveMesh(Lathe(new[] { new Vector2(0f, -0.9f), new Vector2(0.35f, -0.9f), new Vector2(0.35f, 0.9f), new Vector2(0f, 0.9f) }, 0f, 360f, 12, null, Vector3.zero), "Mars_Conduit"), m.metal, anc).transform;
            conduit.position = M.PierConduitRest; conduit.rotation = Quaternion.Euler(70f, 20f, 0f);
            Block("ConduitSocket", M.PierConduit + new Vector3(-0.5f, -1.2f, -0.5f), M.PierConduit + new Vector3(0.5f, -0.9f, 0.5f), m.metalDark, anc, false);

            // ---------------------------------------------------------------- the ancient bridge's two spans (segments, misaligned until section 6)
            Transform Span(string n, float x0, float x1)
            {
                var root = new GameObject(n).transform; root.SetParent(anc, false); root.position = new Vector3((x0 + x1) * 0.5f, 0f, (M.SpanZ0 + M.SpanZ1) * 0.5f);
                int segs = 4; float len = (x1 - x0) / segs;
                for (int i = 0; i < segs; i++)
                {
                    var seg = new GameObject($"Seg{i}").transform; seg.SetParent(root, false); seg.localPosition = new Vector3(-(x1 - x0) * 0.5f + len * (i + 0.5f), 0f, 0f);
                    var v = new List<Vector3>(); var t = new List<int>();
                    AddBox(v, t, new Vector3(0f, -0.6f, 0f), new Vector3(len - 0.1f, 1.2f, 8f));
                    AddBox(v, t, new Vector3(0f, 0.5f, -3.9f), new Vector3(len - 0.1f, 1.0f, 0.3f)); AddBox(v, t, new Vector3(0f, 0.5f, 3.9f), new Vector3(len - 0.1f, 1.0f, 0.3f));
                    AddBox(v, t, new Vector3(0f, -3.2f, 0f), new Vector3(len * 0.5f, 4f, 5f));                         // the arch's key under the deck
                    var go = MeshObject($"{n}_Seg{i}", SaveMesh(Finish(v, t), $"Mars_{n}_Seg{i}"), m.temple, seg); go.transform.localPosition = Vector3.zero;
                    var lv = new List<Vector3>(); var lt = new List<int>(); AddBox(lv, lt, new Vector3(0f, 0.02f, 0f), new Vector3(len - 0.4f, 0.04f, 0.18f));
                    var gl = MeshObject($"{n}_SegLine{i}", SaveMesh(Finish(lv, lt), $"Mars_{n}_SegLine{i}"), GlowMat($"Mars_{n}_line"), seg, false); gl.transform.localPosition = Vector3.zero;
                }
                // the great arch under the span (fixed, rising from the ravine floor)
                var av = new List<Vector3>(); var at = new List<int>(); var ap = new List<Vector3>();
                for (int i = 0; i <= 14; i++) { float a = i / 14f * Mathf.PI; ap.Add(new Vector3(Mathf.Lerp(x0, x1, i / 14f), -1.4f - Mathf.Sin(a) * 0f - (1f - Mathf.Sin(a)) * (-fy - 2f), (M.SpanZ0 + M.SpanZ1) * 0.5f)); }
                Sweep(av, at, ap, 1.4f, false, 8);
                MeshObject(n + "_Arch", SaveMesh(SmoothFinish(av, at), "Mars_" + n + "_Arch"), m.stone, anc);
                return root;
            }
            var spanW = Span("SpanWest", -47f, -20.4f); var spanE = Span("SpanEast", 20.4f, 47f);
            var swing = new GameObject("SwingSegment").transform; swing.SetParent(spanW, true); swing.position = M.SwingSegment;
            { var g = Block("SwingSection", new Vector3(-1.5f, -0.5f, -0.4f), new Vector3(1.5f, 0.5f, 6f), m.stoneDark, swing, false); g.transform.localPosition = Vector3.zero; }

            // ---------------------------------------------------------------- the temple island: the plaza, the lanes, the arena
            var isl = new GameObject("TempleIsland").transform; isl.SetParent(world, false);
            MeshObject("IslandRock", SaveMesh(Lathe(new[] { new Vector2(0f, fy - 1f), new Vector2(30f, fy - 1f), new Vector2(27f, fy + 6f), new Vector2(25f, -3f), new Vector2(24f, -0.6f), new Vector2(0f, -0.6f) }, 0f, 360f, 36, null, new Vector3(0f, 0f, 152f)), "Mars_IslandRock"), m.rockDark, isl);
            // the plaza decks: west lane, centre (south / north), east (south / north) - the broken strip in the east lane left open
            Block("PlazaDeckW", new Vector3(-21f, -0.6f, 124f), new Vector3(-6.7f, 0f, 168f), m.stone, isl, false);
            Block("PlazaDeckC", new Vector3(-6.7f, -0.6f, 124f), new Vector3(6.7f, 0f, 168f), m.stone, isl, false);
            Block("PlazaDeckE_S", new Vector3(6.7f, -0.6f, 124f), new Vector3(21f, 0f, 144.5f), m.stone, isl, false);
            Block("PlazaDeckE_N", new Vector3(6.7f, -0.6f, 148.5f), new Vector3(21f, 0f, 168f), m.stone, isl, false);
            Block("PlazaDeckE_Gap", new Vector3(6.7f, -4.5f, 144.5f), new Vector3(21f, -3.9f, 148.5f), m.rockDark, isl, false);
            // the low ancient walls between the lanes, with their gaps (gates)
            foreach (var (x, z0, z1) in new[] { (-6.7f, 124f, 136f), (-6.7f, 140f, 168f), (6.7f, 124f, 141f), (6.7f, 145f, 156f), (6.7f, 160f, 168f) })
                Block($"LaneWall_{(x < 0 ? "W" : "E")}_{(int)z0}", new Vector3(x - 0.3f, 0f, z0), new Vector3(x + 0.3f, 1.3f, z1), m.stoneDark, isl);
            // Viper's barriers (energy walls) and their emitters
            GameObject EnergyWall(string n, Vector3 a, Vector3 b) { var g = Block(n, Vector3.Min(a, b), Vector3.Max(a, b), m.barrier, isl, false); g.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off; return g; }
            var bWC = EnergyWall("BarrierWestGate", new Vector3(-6.85f, 0f, 136f), new Vector3(-6.55f, 3.2f, 140f));
            var bCE = EnergyWall("BarrierEastGate", new Vector3(6.55f, 0f, 156f), new Vector3(6.85f, 3.2f, 160f));
            var bC = EnergyWall("BarrierCentre", new Vector3(-6.4f, 0f, 150.35f), new Vector3(6.4f, 3.2f, 150.65f));
            var slide = Block("MovingSection", new Vector3(-0.35f, 0f, -2f), new Vector3(0.35f, 2.6f, 2f), m.metalDark, isl, false).transform; slide.position = new Vector3(6.7f, 0f, M.SlideGateZ);
            var fallen = Block("FallenWalkway", new Vector3(-5f, -0.3f, -2.6f), new Vector3(5f, 0f, 2.6f), m.stone, isl, false).transform; fallen.position = M.FallenWalkRest; fallen.rotation = Quaternion.Euler(14f, 8f, -18f);
            var pylons = new List<Renderer>();
            for (int i = 0; i < M.Pylons.Length; i++)
            {
                var p = M.Pylons[i];
                Block($"Pylon{i}", p + new Vector3(-0.35f, 0f, -0.35f), p + new Vector3(0.35f, 2.4f, 0.35f), m.metalDark, isl, false);
                pylons.Add(Block($"PylonGlow{i}", p + new Vector3(-0.4f, 2.4f, -0.4f), p + new Vector3(0.4f, 2.9f, 0.4f), GlowMat($"Mars_pylon_glow{i}"), isl, false).GetComponent<Renderer>());
            }
            var restraint = new GameObject("ViperRestraint"); restraint.transform.SetParent(isl, false);
            { var v = new List<Vector3>(); var t = new List<int>(); for (int k = 0; k < 3; k++) Sweep(v, t, Enumerable.Range(0, 28).Select(i => new Vector3(Mathf.Cos(i / 28f * Mathf.PI * 2f) * 0.75f, 0.3f + k * 0.5f, Mathf.Sin(i / 28f * Mathf.PI * 2f) * 0.75f)).ToList(), 0.035f, true, 6);
              var r = MeshObject("RestraintRings", SaveMesh(SmoothFinish(v, t), "Mars_Restraint"), m.glow, restraint.transform, false); r.transform.localPosition = Vector3.zero; restraint.SetActive(false); }
            // Viper's small machines (three hovering jammers)
            var jammers = new List<Transform>();
            for (int i = 0; i < 3; i++)
            {
                var j = new GameObject($"Jammer{i}").transform; j.SetParent(world, false); j.position = M.JammerFrom[i];
                var v = new List<Vector3>(); var t = new List<int>(); Ellipsoid(v, t, Vector3.zero, new Vector3(0.45f, 0.25f, 0.45f), 6, 10);
                var b = MeshObject($"Jammer{i}_Body", SaveMesh(SmoothFinish(v, t), $"Mars_Jammer{i}"), m.metalDark, j, false); b.transform.localPosition = Vector3.zero;
                var gl = Block($"Jammer{i}_Eye", new Vector3(-0.12f, -0.05f, 0.38f), new Vector3(0.12f, 0.05f, 0.46f), m.glow, j, false); gl.transform.localPosition = Vector3.zero;
                j.gameObject.SetActive(false); jammers.Add(j);
            }

            // ---------------------------------------------------------------- the temple (older than everything round it)
            var tem = new GameObject("Temple").transform; tem.SetParent(isl, false);
            {
                var v = new List<Vector3>(); var t = new List<int>();
                AddBox(v, t, new Vector3(-10.5f, 6f, 180f), new Vector3(1.2f, 12f, 23f)); AddBox(v, t, new Vector3(10.5f, 6f, 180f), new Vector3(1.2f, 12f, 23f));
                AddBox(v, t, new Vector3(0f, 6f, 191.6f), new Vector3(22.2f, 12f, 1.2f));
                AddBox(v, t, new Vector3(-6.6f, 6f, 168.6f), new Vector3(8.4f, 12f, 1.2f)); AddBox(v, t, new Vector3(6.6f, 6f, 168.6f), new Vector3(8.4f, 12f, 1.2f)); AddBox(v, t, new Vector3(0f, 9.5f, 168.6f), new Vector3(4.8f, 5f, 1.2f));
                MeshObject("TempleWalls", SaveMesh(Finish(v, t), "Mars_TempleWalls"), m.temple, tem);
                var roof = Lathe(new[] { new Vector2(14f, 0f), new Vector2(12f, 3f), new Vector2(7f, 6f), new Vector2(0f, 7f) }, 0f, 360f, 4, null, new Vector3(0f, 12f, 180f));
                MeshObject("TempleRoof", SaveMesh(Finish(new List<Vector3>(roof.vertices), new List<int>(roof.triangles)), "Mars_TempleRoof"), m.temple, tem);
                Block("TempleFloorDeck", new Vector3(-10f, -0.6f, 169f), new Vector3(10f, 0f, 191f), m.stoneDark, tem, false);
                Block("TempleStepsDeck", new Vector3(-6f, -0.6f, 162f), new Vector3(6f, 0f, 169.8f), m.stone, tem, false);
                for (int i = 0; i < 3; i++) Block($"TempleStep{i}", new Vector3(-6f, 0f, 162f + i * 1.2f), new Vector3(6f, 0.06f + i * 0.02f, 163.2f + i * 1.2f), m.stone, tem, false);
                foreach (var x in new[] { -7f, 7f }) for (float z = 173f; z < 190f; z += 5.5f) Block($"TempleColumn{(int)x}_{(int)z}", new Vector3(x - 0.5f, 0f, z - 0.5f), new Vector3(x + 0.5f, 12f, z + 0.5f), m.stone, tem);
                PointLight("TempleLight", new Vector3(0f, 8f, 182f), 26f, 1.6f, tem);
            }
            // the symbols now used separately by the two kingdoms - here halves of one design (a ring split by a wave)
            Renderer SymbolHalf(string n, float sx)
            {
                var v = new List<Vector3>(); var t = new List<int>();
                var c = M.SymbolWall + Vector3.back * 0.05f;
                var ring = new List<Vector3>(); for (int i = 0; i <= 16; i++) { float a = (sx < 0 ? 90f : -90f) + i / 16f * 180f; ring.Add(c + new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 1.6f, Mathf.Sin(a * Mathf.Deg2Rad) * 1.6f, 0f)); }
                Sweep(v, t, ring, 0.07f, false, 6);
                var wave = new List<Vector3>(); for (int i = 0; i <= 10; i++) { float y = -1.6f + i * 0.32f; wave.Add(c + new Vector3(Mathf.Sin(i * 0.9f) * 0.42f * sx + sx * 0.06f, y, 0f)); }
                Sweep(v, t, wave, 0.06f, false, 6);
                for (int k = 0; k < 3; k++) { var p = c + new Vector3(sx * (0.7f + k * 0.22f), 0.9f - k * 0.9f, 0f); Ellipsoid(v, t, p, new Vector3(0.12f, 0.12f, 0.04f), 4, 8); }
                return MeshObject(n, SaveMesh(SmoothFinish(v, t), "Mars_" + n), GlowMat("Mars_" + n + "_glow"), tem, false).GetComponent<Renderer>();
            }
            var symW = SymbolHalf("SymbolWestKingdom", -1f); var symE = SymbolHalf("SymbolEastKingdom", 1f);
            // the rune: a tetrahedron (generated in code like the others) on its pedestal; its small counterpart for 7
            var em2 = ElarimMaterials(false);
            var rune = BuildTetrahedronShape("MarsRune", tem, 0.9f, em2, "Mars_Rune").transform; rune.position = M.RuneAt;
            MeshObject("RunePedestal", SaveMesh(Lathe(new[] { new Vector2(0f, -1.25f), new Vector2(0.9f, -1.25f), new Vector2(0.5f, -0.5f), new Vector2(0.8f, -0.42f), new Vector2(0f, -0.42f) }, 0f, 360f, 20, null, M.RuneAt), "Mars_RunePedestal"), m.stone, tem);
            PointLight("RuneLight", M.RuneAt + Vector3.up * 1.8f, 8f, 1.8f, tem);
            var recordTet = BuildTetrahedronShape("MarsRecordCopy", tem, 0.065f, em2, "Mars_RecordTet").transform; recordTet.gameObject.SetActive(false);
            // the display: the temple's old connections - a route leading out from Mars beyond the edge of the solar system,
            // breaking off partway (spec 10, revised): Mars a dot, rings of the system, the route's dashes, then nothing
            var disp = Block("TempleDisplay", M.DisplayAt + new Vector3(-0.05f, -1.1f, -1.6f), M.DisplayAt + new Vector3(0.05f, 1.1f, 1.6f), GlowMat("Mars_display"), tem, false).GetComponent<Renderer>();
            var leads = new GameObject("DisplayRoute"); leads.transform.SetParent(tem, false);
            { var v = new List<Vector3>(); var t = new List<int>(); var o = M.DisplayAt + new Vector3(0.1f, -0.2f, -1.1f);
              Ellipsoid(v, t, o, Vector3.one * 0.1f, 6, 10);
              foreach (var r in new[] { 0.35f, 0.7f, 1.05f }) Sweep(v, t, Enumerable.Range(0, 24).Select(i => o + new Vector3(0f, Mathf.Sin(i / 24f * Mathf.PI * 2f) * r * 0.45f, Mathf.Cos(i / 24f * Mathf.PI * 2f) * r)).ToList(), 0.008f, true, 4);
              for (int k = 0; k < 9; k++) { float u0 = k / 9f, u1 = u0 + 0.06f; var a = o + new Vector3(0f, u0 * 0.95f, u0 * 1.55f); var b = o + new Vector3(0f, u1 * 0.95f, u1 * 1.55f); Sweep(v, t, new[] { a, b }, 0.018f, false, 4); }   // dashes outward, the last ones fading off - it stops
              var lg = MeshObject("DisplayRouteGlow", SaveMesh(SmoothFinish(v, t), "Mars_DisplayRoute"), m.glow, leads.transform, false); lg.transform.localPosition = Vector3.zero; leads.SetActive(false); }
            // ---------------------------------------------------------------- the saucer (hovering outside the occupied approaches)
            var saucer = PlaceSaucer(world, M.SaucerAt);
            var beam = saucer.GetComponent<Seven.SaucerBeam>();
            CheckSaucer("Mars", saucer);

            // ---------------------------------------------------------------- ground, 7, the bike, the camera, the people
            var wg = new GameObject("WorldGround").AddComponent<Seven.WorldGround>();
            wg.Shape = Seven.Ground.Shape.Zones;
            wg.Zones = M.Zones.Select(z => new Seven.WorldGround.ZoneDef { Name = z.Name, Min = new Vector2(z.X0, z.Z0), Max = new Vector2(z.X1, z.Z1), Y0 = z.Y0, Y1 = z.Y1, Axis = z.Axis, Enabled = z.On }).ToArray();
            wg.ZoneObstacles = new Vector3[0];
            var seven = Spawn(SevenDir + "/7_character.fbx", "7", M.SevenDown, Vector3.right, sevenCtl);
            var camGo = new GameObject("ChapterCamera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.62f, 0.62f, 0.62f);
            cam.fieldOfView = 54f; cam.nearClipPlane = 0.08f; cam.farClipPlane = 2500f; camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData(); camData.renderPostProcessing = true; camData.requiresDepthTexture = true; camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camGo.transform.position = M.SevenDown + new Vector3(-3f, 1.8f, 0f);
            var dirGo = new GameObject("MarsDirector");
            var input = dirGo.AddComponent<Seven.ChapterInput>();
            var s7 = seven.AddComponent<Seven.ChapterSeven>(); s7.Input = input; s7.CameraTransform = camGo.transform;
            var ccam = camGo.AddComponent<Seven.ChapterCamera>(); ccam.Target = seven.transform; ccam.Input = input;
            // the bike (as Europa / Venus)
            var bikeRoot = new GameObject("HoverBike").transform; bikeRoot.position = M.BikeDown;
            var visual = new GameObject("Visual").transform; visual.SetParent(bikeRoot, false);
            if (bike != null) { var bm = (GameObject)PrefabUtility.InstantiatePrefab(bike); bm.name = "BikeModel"; bm.transform.SetParent(visual, false); var bmat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Venus_hoverbike_v1.mat"); foreach (var r in bm.GetComponentsInChildren<Renderer>()) r.sharedMaterial = bmat; }
            var bp = LoadBikePoints();
            Transform P(string n, Vector3 lp) { var x = new GameObject(n).transform; x.SetParent(visual, false); x.localPosition = lp; return x; }
            var hb = bikeRoot.gameObject.AddComponent<Seven.HoverBike>();
            hb.Seat = P("Seat", bp.saddle); hb.GripL = P("GripL", bp.gripL); hb.GripR = P("GripR", bp.gripR); hb.FootL = P("FootL", bp.footL); hb.FootR = P("FootR", bp.footR);
            hb.Fairing = P("FairingL", new Vector3(bp.gripL.x - 0.05f, bp.gripL.y + 0.04f, bp.gripL.z - 0.25f)); hb.Visual = visual; hb.Length = bp.length;
            var vl = new GameObject("VentLight").AddComponent<Light>(); vl.transform.SetParent(visual, false); vl.transform.localPosition = new Vector3(0f, -0.15f, 0f); vl.type = LightType.Point; vl.range = 3f; vl.intensity = 0.4f; vl.shadows = LightShadows.None; hb.VentLight = vl;
            // the people: Reptilians (one model; ±10 % size; three tones; guards in a helmet and a chest plate) and Viper
            var tones = new[] { reptMat, null, null };
            if (reptMat != null) { tones[1] = new Material(reptMat) { name = "Mars_reptilian_tone_dark" }; tones[1].SetColor("_BaseColor", Gray(0.78f)); AssetDatabase.CreateAsset(tones[1], MatDir + "/Mars_reptilian_tone_dark.mat");
                                   tones[2] = new Material(reptMat) { name = "Mars_reptilian_tone_light" }; tones[2].SetColor("_BaseColor", Gray(1.0f)); tones[2].SetFloat("_Smoothness", 0.45f); AssetDatabase.CreateAsset(tones[2], MatDir + "/Mars_reptilian_tone_light.mat"); }
            Seven.MarsFolk Folk(string name, string role, Vector3 at, Vector3 face, float scale, int tone)
            {
                GameObject go;
                if (rept != null)
                {
                    go = (GameObject)PrefabUtility.InstantiatePrefab(rept); go.name = name;
                    var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
                    an.avatar = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(rept)).OfType<Avatar>().FirstOrDefault(); an.runtimeAnimatorController = reptCtl; an.applyRootMotion = false;
                    foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()) { r.sharedMaterial = tones[tone % 3] ?? reptMat; r.updateWhenOffscreen = true; }
                    if (role == "guard")
                    {
                        var head = an.GetBoneTransform(HumanBodyBones.Head); var chest = an.GetBoneTransform(HumanBodyBones.UpperChest) ?? an.GetBoneTransform(HumanBodyBones.Chest);
                        if (head != null) { var hv = new List<Vector3>(); var ht = new List<int>(); Ellipsoid(hv, ht, Vector3.zero, new Vector3(0.15f, 0.12f, 0.17f), 6, 10, p => p.y < -0.02f); var h = MeshObject("GuardHelmet", SaveMesh(SmoothFinish(hv, ht), "Mars_GuardHelmet_" + name), m.metal, head, false); h.transform.localPosition = new Vector3(0f, 0.1f, 0.01f); h.transform.localRotation = Quaternion.identity; }
                        if (chest != null) { var cp = Block("GuardChestPlate", new Vector3(-0.19f, -0.16f, 0.06f), new Vector3(0.19f, 0.18f, 0.12f), m.metalDark, chest, false); cp.transform.localPosition = Vector3.zero; cp.transform.localRotation = Quaternion.identity; }
                    }
                }
                else { go = new GameObject(name); Block("Placeholder", new Vector3(-0.3f, 0f, -0.25f), new Vector3(0.3f, 1.9f, 0.25f), m.cloth, go.transform, false); }
                go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(face)); go.transform.localScale = Vector3.one * scale;
                var f = go.AddComponent<Seven.MarsFolk>(); f.Role = role; f.WalkSpeed = role == "family" ? 0.9f : 1.15f;
                return f;
            }
            var guard = Folk("Guard", "guard", M.GuardAt, Vector3.left, 1.05f, 1);
            var worker = Folk("Worker", "worker", M.WorkerAt, Vector3.right, 0.95f, 2);
            var caretaker = Folk("Caretaker", "caretaker", M.CaretakerAt, Vector3.left, 1.0f, 0);
            var drivers = new[] { Folk("Driver0", "worker", M.TransportAt + new Vector3(-3f, 0f, 1.6f), Vector3.right, 0.98f, 0), Folk("Driver1", "worker", M.TransportAt + new Vector3(-4f, 0f, -1.6f), Vector3.right, 1.02f, 1) };
            var folk = new List<Seven.MarsFolk>();
            var roles = new[] { "worker", "trader", "family", "worker", "trader", "family", "worker", "guard" };
            int nf = 0;
            foreach (var (c, count) in new[] { (M.MarketAt, 8), (new Vector3(-160f, 0f, 0f), 3), (new Vector3(-96f, 0f, 2f), 3), (new Vector3(-120f, 0f, 28f), 2), (new Vector3(-140f, 0f, -24f), 2), (new Vector3(-58f, 0f, 9f), 2), (new Vector3(100f, 0f, 0f), 3), (new Vector3(60f, 0f, 6f), 2) })
                for (int i = 0; i < count; i++)
                {
                    var p = c + new Vector3(((i * 37) % 9) - 4f, 0f, ((i * 53) % 7) - 3f);
                    string role = c.x > 40f && i == 0 ? "guard" : c.x > -60f && c.x < -50f ? "guard" : roles[(nf + i) % roles.Length];
                    float scale = role == "family" && i % 2 == 1 ? 0.78f : 0.9f + ((nf * 7 + i * 3) % 5) * 0.05f;      // ±10 %; families with a smaller member
                    folk.Add(Folk($"Folk{nf}_{role}", role, p, Quaternion.Euler(0f, (nf * 73) % 360, 0f) * Vector3.forward, scale, nf + i));
                    nf++;
                }
            Seven.MarsFolk viperF = null;
            if (viper != null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(viper); go.name = "Viper";
                var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
                an.avatar = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(viper)).OfType<Avatar>().FirstOrDefault(); an.runtimeAnimatorController = viperCtl; an.applyRootMotion = false;
                foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()) { r.sharedMaterial = viperMat; r.updateWhenOffscreen = true; }
                go.transform.position = M.ViperBlocks; viperF = go.AddComponent<Seven.MarsFolk>(); viperF.Role = "viper"; viperF.WalkSpeed = 1.2f; viperF.CalmSway = false;
                go.SetActive(false);
            }
            // the guide light (as Europa: a bright core, a soft camera-facing halo, a strong light)
            var gl0 = new List<Vector3>(); var glt = new List<int>(); Ellipsoid(gl0, glt, Vector3.zero, Vector3.one * 0.5f, 10, 14);
            var guideLight = MeshObject("GuideLight", SaveMesh(SmoothFinish(gl0, glt), "Mars_GuideLight"), m.glow, world, false);
            { var hvv = new List<Vector3>(); var htt = new List<int>(); for (int k = 0; k < 24; k++) { var a0 = Polar(2.6f, k * 15f, 0f); var a1 = Polar(2.6f, (k + 1) * 15f, 0f); Quad(hvv, htt, Vector3.zero, new Vector3(a1.x, a1.z, 0f), new Vector3(a0.x, a0.z, 0f), Vector3.zero); }
              var halo = Finish(hvv, htt); halo.colors = halo.vertices.Select(p => new Color(1f, 1f, 1f, Mathf.Lerp(0.85f, 0f, Mathf.Clamp01(p.magnitude / 2.6f)))).ToArray();
              MeshObject("GuideHalo", SaveMesh(halo, "Mars_GuideHalo"), AdditiveMat("Mars_guide_halo", 1.4f), guideLight.transform, false); }
            PointLight("GuideLightLamp", Vector3.zero, 24f, 3f, guideLight.transform).transform.localPosition = Vector3.zero;
            guideLight.SetActive(false);

            var ui = new GameObject("ChapterUI").AddComponent<Seven.ChapterUI>(); ui.Cam = cam;
            var dlg = dirGo.AddComponent<Seven.DialoguePlayer>(); dlg.Bank = bank; dlg.UI = ui;
            var legend = new GameObject("ControlsLegend").AddComponent<Seven.ControlsLegend>(); legend.Cam = cam; legend.Set = Seven.ControlsLegend.Layout.Chapter; legend.HidePanels = true;
            var d = dirGo.AddComponent<Seven.MarsDirector>();
            d.Seven = s7; d.Cam = ccam; d.Input = input; d.Dlg = dlg; d.UI = ui; d.Legend = legend; d.Bike = hb; d.SaucerBeam = beam; d.Saucer = saucer; d.Hold = saucer.GetComponent<Seven.SaucerHold>();
            d.Atmo = atmo.GetComponent<Seven.Atmosphere>(); d.GuideLight = guideLight.transform;
            d.Guard = guard; d.Worker = worker; d.Caretaker = caretaker; d.Viper = viperF; d.Townsfolk = folk.ToArray(); d.Drivers = drivers;
            d.Barrier = barrier; d.Transport = transport; d.ViperPlatform = plat; d.PlatformLight = plight;
            d.CausewaySlab = slab; d.CausewayEdges = slabEdges; d.ServiceGate = gate; d.ServicePanel = gatePanel;
            d.Mechanism = mech; d.ChannelW = chW; d.ChannelE = chE; d.ControlW = ctlW; d.ControlE = ctlE; d.RelayW = relay;
            d.Conduit = conduit; d.SwingSeg = swing; d.SpanW = spanW; d.SpanE = spanE; d.Jammers = jammers.ToArray();
            d.Pylons = pylons.ToArray(); d.BarrierWC = bWC; d.BarrierCE = bCE; d.BarrierCentre = bC; d.SlideBlock = slide; d.FallenWalk = fallen; d.Restraint = restraint;
            d.SymbolW = symW; d.SymbolE = symE; d.Display = disp; d.Rune = rune; d.RecordTet = recordTet; d.DisplayLeads = leads;
            d.Vision4 = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Video/vision_04_mars.mp4");
            AddProbe(new Vector3(-110f, 6f, 0f), new Vector3(240f, 30f, 120f), Vector3.zero);
            AddProbe(new Vector3(0f, -8f, 80f), new Vector3(140f, 40f, 300f), Vector3.zero);
            AddProbe(new Vector3(0f, 6f, 180f), new Vector3(24f, 14f, 26f), Vector3.zero);
            EditorSceneManager.SaveScene(scene, MarsScenePath);
            Log("scene saved " + MarsScenePath + $" ({folk.Count} townsfolk, Viper {(viperF != null ? "rigged" : "missing")}, Reptilians {(rept != null ? "rigged" : "placeholder")})");
        }

        /// <summary>Round 15: a tetrahedron record (the rune / its small copy) in the Elarim stone, its 6 edges faintly lit.</summary>
        static GameObject BuildTetrahedronShape(string name, Transform parent, float radius, ElarimMats em, string meshPrefix)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            MeshObject("Body", SaveMesh(Seven.Tetrahedron.Mesh(radius), meshPrefix + "_Body"), em.stone, root.transform);
            var ev = new List<Vector3>(); var et = new List<int>();
            foreach (var (a, b) in Seven.Tetrahedron.Edges) Sweep(ev, et, new[] { Seven.Tetrahedron.Vertex(a, radius) * 1.002f, Seven.Tetrahedron.Vertex(b, radius) * 1.002f }, radius * 0.02f, false, 5);
            MeshObject("Edges", SaveMesh(SmoothFinish(ev, et), meshPrefix + "_Edges"), em.edges, root.transform, false);
            return root;
        }
    }
}
