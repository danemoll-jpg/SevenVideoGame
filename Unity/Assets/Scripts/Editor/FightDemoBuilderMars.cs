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
            ApplyMixamoHumanMap(imp, path, true);   // QF18 (6): a true T-pose reference (the A-pose arms)
            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            Log($"{path}: {kind} avatar human={avatar?.isHuman} valid={avatar?.isValid}");
            if (avatar == null || !avatar.isHuman) return null;
            mat = TexturedMat((kind == "machinist" ? "Ast_" : kind == "biavian" ? "Bia_" : "Mars_") + kind, texPath, kind == "viper" ? 0.55f : kind == "machinist" ? 0.6f : kind == "biavian" ? 0.45f : 0.35f, kind == "machinist" ? 0.45f : 0f);
            mat.SetFloat("_Rim", 0.35f); EditorUtility.SetDirty(mat);
            // the clips: (state, source fbx, loop)
            var list = new List<(string state, string file, bool loop)>();
            string E(string n) => Path.Combine(RepoRoot, "Art", "Mixamo", "elarim", n);
            string T(string n) => Path.Combine(RepoRoot, "Art", "Mixamo", "tunneler", n);
            string Y(string n) => Path.Combine(RepoRoot, "Art", "Mixamo", "YBot", n);
            if (kind == "reptilian" || kind == "biavian")
            {
                // Round 17: the Biavians (Nela, the residents) use the same calm set - idle / talk / point / carry (the seedlings)
                // / walk / wave, + the kneel pose (Nela beside the disoriented resident)
                list.Add(("idle", E("elarim_idle.fbx"), true)); list.Add(("talk", E("elarim_talk.fbx"), true)); list.Add(("point", E("elarim_point.fbx"), false));
                list.Add(("carry", E("elarim_carry.fbx"), true)); list.Add(("walk", Path.Combine(RepoRoot, "Art", "Mixamo", "reptilian", "reptilian_walk.fbx"), true));
                list.Add(("wave", Path.Combine(RepoRoot, "Art", "Mixamo", "reptilian", "reptilian_wave.fbx"), false));
                if (kind == "biavian") { var kp2 = Path.Combine(RepoRoot, "Art", "Export", "anim", "elarim_kneel_pose.fbx"); list.Add(("kneel", File.Exists(kp2) ? kp2 : E("elarim_kneel.fbx"), true)); }
            }
            else
            {
                list.Add(("idle", T("tunneler_idle.fbx"), true)); list.Add(("walk", T("tunneler_walk.fbx"), true)); list.Add(("roar", T("tunneler_roar.fbx"), false));
                list.Add(("talk", E("elarim_talk.fbx"), true)); list.Add(("point", E("elarim_point.fbx"), false));
                list.Add(("hit", Y("ybot_hit_body.fbx"), false)); list.Add(("down", Y("ybot_knocked_down.fbx"), false)); list.Add(("dazed", Y("ybot_dazed.fbx"), true));
                // QF17 (1): his fight - the YBot stance / punch / get-up and the tunneler's swipe (sweep), on his rig
                list.Add(("fight_idle", Y("ybot_fight_idle.fbx"), true)); list.Add(("punch", Y("ybot_punch.fbx"), false));
                list.Add(("swipe", T("tunneler_swipe.fbx"), false)); list.Add(("get_up", Y("ybot_get_up.fbx"), false));
                var kp = Path.Combine(RepoRoot, "Art", "Export", "anim", "elarim_kneel_pose.fbx");
                list.Add(("kneel", File.Exists(kp) ? kp : E("elarim_kneel.fbx"), true));
            }
            var cpath = AnimDir + "/" + (kind == "viper" ? "Viper" : kind == "machinist" ? "Machinist" : kind == "biavian" ? "Biavian" : "Reptilian") + ".controller";   // Round 16: the Machinist's own
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
                ApplyMixamoHumanMap(ci, dst, true);
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
            // QF18 (13): ground pieces are flat-topped terrain (MGround) - they meet edge to edge, no grooves
            MGround("PlainW_A", new Vector3(-900f, fy - 5f, -700f), new Vector3(-60f, 0f, 700f), m.sand, land);
            MGround("PlainW_B", new Vector3(-60f, fy - 5f, -700f), new Vector3(-50f, 0f, M.RampFootZ - M.RampLanding), m.sand, land);
            MGround("PlainW_C", new Vector3(-60f, fy - 5f, -11f), new Vector3(-50f, 0f, 700f), m.sand, land);
            MGround("PlainE_A", new Vector3(60f, fy - 5f, -700f), new Vector3(900f, 0f, 700f), m.sand, land);
            MGround("PlainE_B", new Vector3(50f, fy - 5f, -700f), new Vector3(60f, 0f, M.RampFootZ - M.RampLanding), m.sand, land);
            MGround("PlainE_C", new Vector3(50f, fy - 5f, -12.6f), new Vector3(60f, 0f, 700f), m.sand, land);
            // QF18 (11): the ravine floor with a real channel down its middle (before it was a dark strip 2 cm high, so the
            // "broken causeway" had no gap to cross): floor W / E, the channel bed 4 m down from z -300 to 110, closed ends
            MGround("RavineFloorW", new Vector3(-50f, fy - 5f, -700f), new Vector3(-7f, fy, 700f), m.rock, land);
            MGround("RavineFloorE", new Vector3(7f, fy - 5f, -700f), new Vector3(50f, fy, 700f), m.rock, land);
            MGround("RavineFloorS", new Vector3(-7f, fy - 5f, -700f), new Vector3(7f, fy, M.ChannelZ0), m.rock, land);
            MGround("RavineFloorN", new Vector3(-7f, fy - 5f, M.ChannelZ1), new Vector3(7f, fy, 700f), m.rock, land);
            MGround("ChannelBed", new Vector3(-7f, fy - 5f, M.ChannelZ0), new Vector3(7f, fy - M.ChannelDepth, M.ChannelZ1), m.rockDark, land);
            // QF18 (10, 12a): the ramps - a solid wedge cut into the bank (top surface, the ravine-side face down to the floor),
            // meeting the bank top flush at the top and running out onto a flat landing at the foot that opens east onto the
            // ravine floor (before: it ended head-on in the bank's 22 m end wall, the only way on was a 0.6 m corner of zone
            // overlap behind the wall, so the bike went through it). A parapet only where the drop is over a metre.
            {
                var v = new List<Vector3>(); var t = new List<int>(); var pv = new List<Vector3>(); var pt = new List<int>();
                foreach (var (x0, x1, zTop) in new[] { (-60f, -50f, -11f), (50f, 60f, -12.6f) })
                {
                    float zF = M.RampFootZ;
                    Quad(v, t, new Vector3(x0, fy, zF), new Vector3(x0, 0f, zTop), new Vector3(x1, 0f, zTop), new Vector3(x1, fy, zF));
                    float xr = x0 < 0 ? x1 : x0;                                   // the ravine-side face of the wedge
                    Quad(v, t, new Vector3(xr, fy, zF), new Vector3(xr, fy, zTop), new Vector3(xr, 0f, zTop), new Vector3(xr, fy, zF));
                    // the landing at the foot (the floor level, open to the east / west onto the ravine floor)
                    MGround(x0 < 0 ? "RampLandingW" : "RampLandingE", new Vector3(x0, fy - 5f, zF - M.RampLanding), new Vector3(x1, fy, zF), m.rock, land);
                    // the wedge's fill under the slope (so the ravine never sees under the ramp)
                    MGround(x0 < 0 ? "RampFillW" : "RampFillE", new Vector3(x0, fy - 5f, zF), new Vector3(x1, fy, zTop), m.rock, land);
                    // a low parapet on the ravine side, from the top down to where the drop is about a metre
                    float xp = x0 < 0 ? x1 - 0.35f : x0 + 0.35f, zP = zF + (zTop - zF) * (1.2f / -fy);
                    float yAt(float z) => Mathf.Lerp(fy, 0f, (z - zF) / (zTop - zF));
                    for (float z = zTop; z > zP + 0.01f; z -= 3f)
                    {
                        float zb = Mathf.Max(zP, z - 3f);
                        var a = new Vector3(xp, yAt(z), z); var b = new Vector3(xp, yAt(zb), zb);
                        MBox(pv, pt, (a + b) * 0.5f + Vector3.up * 0.45f, new Vector3(0.45f, 0.9f + Mathf.Abs(a.y - b.y), Mathf.Abs(z - zb) - 0.08f), 0f, 0.02f);
                    }
                }
                MeshObject("MaintRamps", SaveMesh(Finish(v, t), "Mars_MaintRamps"), m.rock, land);
                MeshObject("MaintRampParapets", SaveMesh(Finish(pv, pt), "Mars_MaintRampParapets"), m.wall, land);
            }
            // QF18 (9): the route's entrance - an old half-buried gateway over the top of the west ramp (the shot after the
            // worker tells her about it shows this), worn steps beside it
            {
                var kg = new Kit();
                Gateway(kg.sv, kg.st, new Vector3(-55f, -1.1f, -12.4f), 9.4f, 5.4f, 1.6f, 0f);
                kg.Save("MaintGateway", land, m.wall, m.rockDark, m.stone, m.glow, m.cloth);
            }
            // the ravine's walls: rough rock faces (buttresses, ledges) over the block sides - kept clear of the ramps, their
            // landings and (west) the pier walk under the old bridge
            {
                var v = new List<Vector3>(); var t = new List<int>();
                for (int i = 0; i < 70; i++)
                {
                    float z = -300f + i * 9f + (float)rnd.NextDouble() * 4f; if (z > M.RampFootZ - M.RampLanding - 6f && z < -6f) continue;
                    foreach (var sx in new[] { -1f, 1f })
                    {
                        if (z > 134f && z < 184f) continue;                              // the span heads, the pier walk
                        Lump(v, t, new Vector3(sx * (M.RavineX + 0.6f), fy + 6f + (float)rnd.NextDouble() * 8f, z), new Vector3(2.4f, 9f + (float)rnd.NextDouble() * 6f, 4.5f), 0.5f, rnd.Next(1000));
                    }
                }
                MeshObject("RavineWallRock", SaveMesh(SmoothFinish(v, t), "Mars_RavineRock"), m.rockDark, land);
            }
            // walls, trenches and elevated roads across the plain (outside the play areas)
            {
                var v = new List<Vector3>(); var t = new List<int>(); var tv = new List<Vector3>(); var tt = new List<int>(); var rv = new List<Vector3>(); var rt = new List<int>();
                foreach (var (x, z, len, yaw) in new[] { (-300f, -160f, 320f, 12f), (-260f, 180f, 280f, -18f), (300f, -150f, 320f, -10f), (280f, 190f, 300f, 22f), (-420f, 60f, 260f, 80f), (420f, -40f, 240f, 95f), (-150f, 320f, 200f, 5f), (160f, -320f, 220f, -6f) })
                    MBox(v, t, new Vector3(x, 2.5f, z), new Vector3(len, 5f, 2.2f), yaw);
                foreach (var (x, z, len, yaw) in new[] { (-330f, -60f, 240f, 70f), (340f, 70f, 260f, 110f), (-220f, 260f, 200f, 30f), (230f, -250f, 200f, -40f) })
                { MBox(tv, tt, new Vector3(x, 0.045f, z), new Vector3(len, 0.06f, 5f), yaw); MBox(v, t, new Vector3(x, 0.4f, z) + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 3f, new Vector3(len, 0.8f, 0.8f), yaw); MBox(v, t, new Vector3(x, 0.4f, z) - Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 3f, new Vector3(len, 0.8f, 0.8f), yaw); }
                foreach (var (x0, z0, x1, z1) in new[] { (-640f, -40f, -260f, -30f), (260f, 30f, 640f, 40f), (-300f, 240f, -120f, 420f), (130f, -420f, 300f, -240f) })
                {
                    var a = new Vector3(x0, 7f, z0); var b = new Vector3(x1, 7f, z1); var dd = b - a; float yaw = Mathf.Atan2(dd.x, dd.z) * Mathf.Rad2Deg;
                    MBox(rv, rt, (a + b) * 0.5f, new Vector3(6f, 0.8f, dd.magnitude), yaw);
                    for (float s = 0f; s <= 1f; s += 0.08f) MBox(rv, rt, Vector3.Lerp(a, b, s) + Vector3.down * 3.6f, new Vector3(1.6f, 7f, 1.6f), yaw);
                }
                MeshObject("PlainWalls", SaveMesh(Finish(v, t), "Mars_PlainWalls"), m.wall, land);
                MeshObject("PlainTrenches", SaveMesh(Finish(tv, tt), "Mars_Trenches"), m.rockDark, land, false);
                MeshObject("ElevatedRoads", SaveMesh(Finish(rv, rt), "Mars_ElevatedRoads"), m.stoneDark, land);
            }
            BuildMarsLandscape(world, m.sand, m.rock, m.rockDark, rnd);     // QF18 (21): dunes, mesas, boulders, the Face, a pale sun, dust
            // QF17 (3): the two fortress cities either side of the ravine - real Reptilian fortifications (FortressCity):
            // curtain walls with towers and a gatehouse toward the ravine, terraced quarters, a stepped citadel with domes
            foreach (var sx in new[] { -1f, 1f })
            {
                var kc = new Kit();
                FortressCity(kc, new Vector3(sx * 520f, 0f, 20f), 170f, rnd, sx < 0);
                kc.Save(sx < 0 ? "FortressCityWest" : "FortressCityEast", world, m.wall, m.rockDark, m.stone, m.glow, m.cloth);
            }

            // ---------------------------------------------------------------- the west settlement + the east side's buildings
            var town = new GameObject("Settlement").transform; town.SetParent(world, false);
            {
                // QF17 (3): Reptilian houses from the kit - stepped tiers, parapets, arched doors and windows, domes / towers
                // one object per house, so a camera can cut away just the one in its way (as Europa's split meshes, QF15)
                int hn = 0;
                void House(Vector3 p, Vector3 size, float yaw) { var kh = new Kit(); ReptHouse(kh, p, size, yaw, rnd, Mathf.Abs(p.x - M.MarketAt.x) < 30f); kh.Save($"House{hn++:00}", town, m.wall, m.rockDark, m.stone, m.glow, m.cloth); }
                // along the main street (both sides), round the market, along the side streets
                for (float x = -178f; x < -72f; x += 11f)
                    foreach (var sz in new[] { -1f, 1f })
                    {
                        if (x > -140f && x < -100f) continue;
                        var p = new Vector3(x + (float)rnd.NextDouble() * 2f, 0f, sz * (11f + (float)rnd.NextDouble() * 3f));
                        if (sz > 0 && x > -128f && x < -112f) continue; if (sz < 0 && x > -148f && x < -132f) continue;
                        House(p, new Vector3(8f + (float)rnd.NextDouble() * 2f, 5f + (float)rnd.NextDouble() * 4f, 8f), sz > 0 ? 180f : 0f);
                    }
                foreach (var a in new[] { 20f, 60f, 110f, 160f, 200f, 250f, 290f, 340f }) { var p = M.MarketAt + Polar(23f, a, 0f); if (Mathf.Abs(p.z) < 12.5f) continue; House(p, new Vector3(9f, 6f + (float)rnd.NextDouble() * 3f, 9f), -a + 90f); }
                for (float z = 18f; z < 42f; z += 9f) { House(new Vector3(-131f, 0f, z), new Vector3(7f, 6f, 8f), 90f); House(new Vector3(-109f, 0f, z), new Vector3(7f, 6f, 8f), -90f); }
                for (float z = -40f; z < -8f; z += 9f) { House(new Vector3(-151f, 0f, z), new Vector3(7f, 6f, 8f), 90f); House(new Vector3(-129f, 0f, z), new Vector3(7f, 6f, 8f), -90f); }
                // the east side: a few buildings by the east checkpoint and along the east road
                for (float x = 76f; x < 150f; x += 12f) foreach (var sz in new[] { -1f, 1f }) House(new Vector3(x, 0f, sz * 12f), new Vector3(8f, 6f, 8f), sz > 0 ? 180f : 0f);

                // market stalls (awnings on posts, goods on tables)
                var sv = new List<Vector3>(); var st = new List<int>(); var gv = new List<Vector3>(); var gt = new List<int>();
                // QF17 (6): the stalls stand round the square, never on the road through it (the bike rode through two of them)
                foreach (var a in new[] { 40f, 70f, 110f, 140f, 220f, 250f, 290f, 320f })
                {
                    var p = M.MarketAt + Polar(12.5f, a, 0f); var q = Quaternion.Euler(0f, -a, 0f);
                    // a sloped cloth awning (high at the back, low over the customer), a scalloped hem
                    Quad(sv, st, p + q * new Vector3(-1.9f, 2.9f, 1.3f), p + q * new Vector3(-1.9f, 2.25f, -1.4f), p + q * new Vector3(1.9f, 2.25f, -1.4f), p + q * new Vector3(1.9f, 2.9f, 1.3f));
                    for (int k = 0; k < 6; k++) { var h0 = p + q * new Vector3(-1.9f + k * 0.633f, 2.25f, -1.4f); var h1 = p + q * new Vector3(-1.9f + (k + 1) * 0.633f, 2.25f, -1.4f); Quad(sv, st, h0, h0 + Vector3.down * 0.01f, (h0 + h1) * 0.5f + Vector3.down * 0.28f, h1); }
                    foreach (var (ox, oz, hh) in new[] { (-1.75f, -1.25f, 2.25f), (1.75f, -1.25f, 2.25f), (-1.75f, 1.15f, 2.9f), (1.75f, 1.15f, 2.9f) }) Sweep(gv, gt, new[] { p + q * new Vector3(ox, 0f, oz), p + q * new Vector3(ox, hh, oz) }, 0.06f, false, 6);
                    MBox(gv, gt, p + Vector3.up * 0.82f, new Vector3(2.8f, 0.16f, 1.4f), -a, 0.02f);
                    foreach (var (ox, oz) in new[] { (-1.25f, -0.55f), (1.25f, -0.55f), (-1.25f, 0.55f), (1.25f, 0.55f) }) Sweep(gv, gt, new[] { p + q * new Vector3(ox, 0f, oz), p + q * new Vector3(ox, 0.74f, oz) }, 0.05f, false, 5);
                    for (int k = 0; k < 4; k++)
                    {
                        var jp = p + q * new Vector3(-1f + k * 0.65f, 0.9f, 0f);
                        if (k % 2 == 0) { var jar = Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.14f, 0f), new Vector2(0.22f, 0.15f), new Vector2(0.18f, 0.32f), new Vector2(0.09f, 0.4f), new Vector2(0.11f, 0.46f), new Vector2(0f, 0.46f) }, 0f, 360f, 10, null, jp); int jb = gv.Count; gv.AddRange(jar.vertices); gt.AddRange(jar.triangles.Select(i => i + jb)); }
                        else Lump(gv, gt, jp + Vector3.up * 0.18f, new Vector3(0.24f, 0.2f, 0.2f), 0.2f, k * 13 + (int)a);
                    }
                }
                MeshObject("MarketAwnings", SaveMesh(Finish(sv, st), "Mars_Awnings"), m.cloth, town);
                MeshObject("MarketGoods", SaveMesh(SmoothFinish(gv, gt), "Mars_Goods"), m.stoneDark, town);
                MarsLamp("MarketLamp", M.MarketAt + Vector3.up * 5f, 26f, 1.4f, m.glow, town, new Vector3(0.5f, 0.5f, 0.5f));
                for (float x = -170f; x < -70f; x += 22f) MarsLamp($"StreetLamp{(int)-x}", new Vector3(x, 4.2f, 6.8f), 14f, 0.9f, m.glow, town, new Vector3(0.3f, 0.3f, 0.3f));
            }

            // ---------------------------------------------------------------- the crossing: the main bridge, checkpoints, barrier, platform, transport
            var cross = new GameObject("Crossing").transform; cross.SetParent(world, false);
            MGround("MainBridgeDeck", new Vector3(-50f, -1.2f, -4.5f), new Vector3(50f, 0f, 4.5f), m.stoneDark, cross);
            MBlock("MainBridgeRailS", new Vector3(-50f, 0f, -4.6f), new Vector3(50f, 1.1f, -4.2f), m.wall, cross, false);
            MBlock("MainBridgeRailN", new Vector3(-50f, 0f, 4.2f), new Vector3(50f, 1.1f, 4.6f), m.wall, cross, false);
            for (int i = -2; i <= 2; i++) MBlock($"MainBridgePier{i + 2}", new Vector3(i * 20f - 2f, fy - M.ChannelDepth - 0.5f, -3f), new Vector3(i * 20f + 2f, -1.2f, 3f), m.wall, cross, false);
            foreach (var sx in new[] { -1f, 1f })
            {
                // QF17 (3): the checkpoint's two watchtowers and the gateway arch over the bridge head
                var kk = new Kit();
                Tower(kk, new Vector3(sx * 52f, 0f, -7.5f), 2.1f, 9f, 8, true, rnd);
                Tower(kk, new Vector3(sx * 52f, 0f, 7.5f), 2.1f, 9f, 8, true, rnd);
                Gateway(kk.sv, kk.st, new Vector3(sx * 53.5f, 0f, 0f), 9.6f, 6.4f, 2.2f, 90f);
                kk.Save(sx < 0 ? "CheckpointWest" : "CheckpointEast", cross, m.wall, m.rockDark, m.stoneDark, m.glow, m.cloth);
                foreach (var tz in new[] { -7.5f, 7.5f }) { var bc = cross.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(sx * 52f, 4.5f, tz); bc.size = new Vector3(4.2f, 9f, 4.2f); }
                MarsLamp(sx < 0 ? "CheckLampW" : "CheckLampE", new Vector3(sx * 52f, 9.6f, 0f), 30f, 1.6f, m.glow, cross, new Vector3(0.6f, 0.4f, 0.6f));
            }
            var barrier = MBlock("CheckpointBarrier", M.BarrierAt + new Vector3(-0.15f, 0f, -4.4f), M.BarrierAt + new Vector3(0.15f, 3.4f, 4.4f), m.barrier, cross, false);
            barrier.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            MBlock("BarrierEmitterS", M.BarrierAt + new Vector3(-0.4f, 0f, -4.8f), M.BarrierAt + new Vector3(0.4f, 3.8f, -4.3f), m.metalDark, cross);
            MBlock("BarrierEmitterN", M.BarrierAt + new Vector3(-0.4f, 0f, 4.3f), M.BarrierAt + new Vector3(0.4f, 3.8f, 4.8f), m.metalDark, cross);
            // Viper's elevated platform (rises in section 3; harsh white light from beneath)
            var plat = new GameObject("ViperPlatform").transform; plat.SetParent(cross, false); plat.position = M.PlatformAt + Vector3.down * 3f;
            { var b = MBlock("PlatformDeck", plat.position + new Vector3(-2.2f, -0.4f, -2.2f), plat.position + new Vector3(2.2f, 0f, 2.2f), m.metalDark, plat, false);
              MBlock("PlatformColumn", plat.position + new Vector3(-0.8f, -12f, -0.8f), plat.position + new Vector3(0.8f, -0.4f, 0.8f), m.metal, plat, false);
              MBlock("PlatformUnderGlow", plat.position + new Vector3(-1.8f, -0.48f, -1.8f), plat.position + new Vector3(1.8f, -0.42f, 1.8f), m.glow, plat, false); }
            var plight = new GameObject("PlatformLight").AddComponent<Light>(); plight.transform.SetParent(plat, false); plight.transform.position = plat.position + Vector3.down * 0.8f; plight.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            plight.type = LightType.Spot; plight.spotAngle = 80f; plight.range = 14f; plight.intensity = 6f; plight.shadows = LightShadows.Soft; plight.enabled = false;
            // the supply transport (a cargo hover-cart with crates of water filters)
            var transport = new GameObject("SupplyTransport").transform; transport.SetParent(cross, false); transport.position = M.TransportAt;
            {
                // QF18 (6): a real hover transport - a hull with a rounded nose, the cab with dark windows, cargo racks of banded
                // crates (the water filters), four hover pads with soft glow rings under it; it floats 0.55 m up
                var v = new List<Vector3>(); var t = new List<int>(); var dv = new List<Vector3>(); var dt = new List<int>(); var cv = new List<Vector3>(); var ct = new List<int>(); var gv = new List<Vector3>(); var gt = new List<int>();
                float lift = 0.55f;
                Loft(v, t, new Vector3(0f, lift, -0.2f), 1.25f, 2.7f, 0f, new List<(float, float)> { (0f, 0.25f), (0.12f, 0.05f), (0.55f, 0f), (0.7f, 0.08f) });   // the hull (chamfered tray)
                Add(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(1.2f, 0f), new Vector2(1.15f, 0.5f), new Vector2(0.9f, 0.62f), new Vector2(0f, 0.64f) }, -90f, 90f, 10, null, new Vector3(0f, lift + 0.06f, 2.5f)));   // the rounded nose
                MBox(v, t, new Vector3(0f, lift + 1.25f, 1.75f), new Vector3(2.1f, 1.1f, 1.3f), 0f, 0.06f);                                           // the cab
                MBox(dv, dt, new Vector3(0f, lift + 1.38f, 2.42f), new Vector3(1.8f, 0.45f, 0.04f), 0f, 0f);                                          // its windows
                foreach (var sx in new[] { -1.06f, 1.06f }) MBox(dv, dt, new Vector3(sx, lift + 1.38f, 1.8f), new Vector3(0.04f, 0.42f, 0.9f), 0f, 0f);
                foreach (var sx in new[] { -1.15f, 1.15f }) for (float z = -2.6f; z <= 0.9f; z += 0.7f) MBox(v, t, new Vector3(sx, lift + 1.15f, z), new Vector3(0.08f, 0.9f, 0.08f), 0f, 0f);   // rack posts
                foreach (var sx in new[] { -1.15f, 1.15f }) MBox(v, t, new Vector3(sx, lift + 1.6f, -0.85f), new Vector3(0.08f, 0.08f, 3.6f), 0f, 0f);   // rack rails
                for (int i = 0; i < 6; i++)
                {
                    var c = new Vector3(-0.52f + (i % 2) * 1.04f, lift + 1.1f, -2.2f + (i / 2) * 1.0f);
                    MBox(cv, ct, c, new Vector3(0.88f, 0.78f, 0.88f), 0f, 0.02f);
                    foreach (var dy in new[] { -0.22f, 0.22f }) MBox(v, t, c + Vector3.up * dy, new Vector3(0.92f, 0.06f, 0.92f), 0f, 0f);       // the bands round each crate
                }
                foreach (var (px, pz) in new[] { (-0.9f, -2.1f), (0.9f, -2.1f), (-0.9f, 1.8f), (0.9f, 1.8f) })
                {
                    Add(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.38f, 0f), new Vector2(0.42f, 0.12f), new Vector2(0.3f, 0.2f), new Vector2(0f, 0.22f) }, 0f, 360f, 12, null, new Vector3(px, lift - 0.2f, pz)));
                    Sweep(gv, gt, Enumerable.Range(0, 13).Select(i => new Vector3(px + Mathf.Cos(i / 12f * Mathf.PI * 2f) * 0.3f, lift - 0.21f, pz + Mathf.Sin(i / 12f * Mathf.PI * 2f) * 0.3f)).ToList(), 0.03f, true, 5);
                }
                var tm = MeshObject("TransportBody", SaveMesh(Finish(v, t), "Mars_Transport"), m.metal, transport, true); tm.transform.localPosition = Vector3.zero;
                var tw = MeshObject("TransportWindows", SaveMesh(Finish(dv, dt), "Mars_TransportWindows"), m.metalDark, transport, false); tw.transform.localPosition = Vector3.zero;
                var tc = MeshObject("TransportCrates", SaveMesh(Finish(cv, ct), "Mars_TransportCrates"), m.cloth, transport, true); tc.transform.localPosition = Vector3.zero;
                var g = MeshObject("TransportPads", SaveMesh(SmoothFinish(gv, gt), "Mars_TransportPads"), GlowMat("Mars_transport_pads"), transport, false); g.transform.localPosition = Vector3.zero;
                transport.rotation = Quaternion.Euler(0f, 90f, 0f);
            }

            // ---------------------------------------------------------------- the maintenance route on the ravine floor
            var maint = new GameObject("MaintenanceRoute").transform; maint.SetParent(world, false);
            {
                var v = new List<Vector3>(); var t = new List<int>();
                // QF18 (11): the sheltered passage - an arcade (columns with capitals, a barrel vault, a cornice) instead of a
                // flat slab on posts (Dan: "a plain table-like structure")
                {
                    var av = new List<Vector3>(); var at = new List<int>(); var cv = new List<Vector3>(); var ct = new List<int>();
                    float x0 = 18.5f, x1 = 41.5f, z0 = -90f, z1 = -64f, ys = fy + 5.2f, r = (x1 - x0) * 0.5f;
                    for (float z = z0; z <= z1 + 0.1f; z += 6.5f)
                        foreach (var x in new[] { x0, x1 })
                            Add(cv, ct, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.95f, 0f), new Vector2(0.9f, 0.4f), new Vector2(0.62f, 0.62f), new Vector2(0.6f, 3.4f), new Vector2(0.54f, ys - fy - 0.6f), new Vector2(0.86f, ys - fy - 0.2f), new Vector2(0.95f, ys - fy), new Vector2(0f, ys - fy) }, 0f, 360f, 16, null, new Vector3(x, fy, z)));
                    // the vault: a half-ring section extruded along z, 0.7 m thick, plus a cornice on each side
                    int nA = 18;
                    for (int i = 0; i < nA; i++)
                    {
                        float a0 = Mathf.PI * i / nA, a1 = Mathf.PI * (i + 1) / nA; float cx = (x0 + x1) * 0.5f;
                        Vector3 VP(float a, float rr, float z) => new Vector3(cx - Mathf.Cos(a) * rr, ys + Mathf.Sin(a) * rr * 0.55f, z);
                        Quad(av, at, VP(a0, r + 0.7f, z0 - 1f), VP(a0, r + 0.7f, z1 + 1f), VP(a1, r + 0.7f, z1 + 1f), VP(a1, r + 0.7f, z0 - 1f));
                        Quad(av, at, VP(a0, r, z1 + 1f), VP(a0, r, z0 - 1f), VP(a1, r, z0 - 1f), VP(a1, r, z1 + 1f));
                        Quad(av, at, VP(a0, r, z0 - 1f), VP(a0, r + 0.7f, z0 - 1f), VP(a1, r + 0.7f, z0 - 1f), VP(a1, r, z0 - 1f));
                        Quad(av, at, VP(a1, r, z1 + 1f), VP(a1, r + 0.7f, z1 + 1f), VP(a0, r + 0.7f, z1 + 1f), VP(a0, r, z1 + 1f));
                    }
                    foreach (var x in new[] { x0, x1 }) MBox(av, at, new Vector3(x, ys - 0.15f, (z0 + z1) * 0.5f), new Vector3(1.6f, 0.5f, z1 - z0 + 2.4f), 0f, 0.02f);
                    MeshObject("ShelterVault", SaveMesh(Finish(av, at), "Mars_ShelterVault"), m.stoneDark, maint);
                    MeshObject("ShelterColumns", SaveMesh(Finish(cv, ct), "Mars_ShelterColumns"), m.stone, maint);
                }
                // QF18 (11): the drawbridge. During the division one kingdom raised the old causeway's span and chained it up
                // to cut the maintenance route: the span stands hinged on the west abutment; its chain runs from the span's tip
                // over a pulley on the winding tower to the drum at the tower's foot, held by a heavy lock; the kingdom's order
                // plaque (its half of the shared symbol, barred) is fixed to the tower. She breaks the lock with telekinesis,
                // the drum runs free and the span comes down across the channel (MarsDrawbridge).
                {
                    var hinge = M.DrawHinge; var tw = M.DrawTower;
                    // the hinge blocks either side of the span + the iron axle, the corbel on the east wall the tip rests on
                    foreach (var dz in new[] { -M.DrawWidth * 0.5f - 0.7f, M.DrawWidth * 0.5f + 0.7f }) MBlock($"DrawHingeBlock{(dz < 0 ? "S" : "N")}", hinge + new Vector3(-1.2f, 0f, dz - 0.6f), hinge + new Vector3(0.9f, 1.3f, dz + 0.6f), m.stone, maint, false);
                    { var axv = new List<Vector3>(); var axt = new List<int>(); Sweep(axv, axt, new[] { hinge + new Vector3(0f, 0.25f, -M.DrawWidth * 0.5f - 1.1f), hinge + new Vector3(0f, 0.25f, M.DrawWidth * 0.5f + 1.1f) }, 0.22f, false, 10); MeshObject("DrawAxle", SaveMesh(SmoothFinish(axv, axt), "Mars_DrawAxle"), m.metalDark, maint, false); }
                    MBlock("DrawCorbelE", new Vector3(6.4f, fy - 1.3f, hinge.z - M.DrawWidth * 0.5f), new Vector3(7.05f, fy - 0.68f, hinge.z + M.DrawWidth * 0.5f), m.stone, maint, false);
                    // the span (pivot = the hinge; local +x across the channel)
                    var span = new GameObject("Drawbridge").transform; span.SetParent(maint, false); span.position = hinge;
                    var dk = MGround("DrawDeck", new Vector3(0.1f, -0.64f, -M.DrawWidth * 0.5f), new Vector3(M.DrawLen, 0.06f, M.DrawWidth * 0.5f), m.stone, span); dk.transform.localPosition = new Vector3(M.DrawLen * 0.5f + 0.05f, -0.29f, 0f);
                    var sv2 = new List<Vector3>(); var st2 = new List<int>(); var iv = new List<Vector3>(); var it2 = new List<int>();
                    foreach (var sz in new[] { -1f, 1f }) MBox(sv2, st2, new Vector3(M.DrawLen * 0.5f, -0.25f, sz * (M.DrawWidth * 0.5f + 0.18f)), new Vector3(M.DrawLen, 1.1f, 0.36f), 0f, 0.02f);   // side beams (below the deck top, outside the zone)
                    for (float x = 1.2f; x < M.DrawLen - 0.5f; x += 2.2f) MBox(iv, it2, new Vector3(x, -0.66f, 0f), new Vector3(0.24f, 0.08f, M.DrawWidth + 0.5f), 0f, 0.01f);   // iron bands under it
                    foreach (var sz in new[] { -1.4f, 1.4f }) Sweep(iv, it2, Enumerable.Range(0, 13).Select(i => new Vector3(M.DrawLen - 0.3f, 0.32f + Mathf.Sin(i / 12f * Mathf.PI * 2f) * 0.22f, sz + Mathf.Cos(i / 12f * Mathf.PI * 2f) * 0.22f)).ToList(), 0.05f, true, 6);   // chain eyes at the tip
                    var sb = MeshObject("DrawBeams", SaveMesh(Finish(sv2, st2), "Mars_DrawBeams"), m.stoneDark, span); sb.transform.localPosition = Vector3.zero;
                    var ib = MeshObject("DrawIron", SaveMesh(SmoothFinish(iv, it2), "Mars_DrawIron"), m.metalDark, span, false); ib.transform.localPosition = Vector3.zero;
                    // the winding tower (round, battered, a crown), the pulley wheel on a bracket at its top, the drum at its foot
                    var kt = new Kit(); Tower(kt, tw, 1.7f, M.DrawPulleyH - 0.6f, 8, false, rnd, false); kt.Save("DrawTower", maint, m.wall, m.rockDark, m.stoneDark, m.glow, m.cloth);
                    var pulleyAt = tw + M.DrawPulleyOff; var drumAt = tw + M.DrawDrumOff;
                    var pul = MeshObject("DrawPulley", SaveMesh(Lathe(new[] { new Vector2(0f, -0.18f), new Vector2(0.7f, -0.18f), new Vector2(0.62f, -0.06f), new Vector2(0.55f, 0f), new Vector2(0.62f, 0.06f), new Vector2(0.7f, 0.18f), new Vector2(0f, 0.18f) }, 0f, 360f, 20, null, Vector3.zero), "Mars_DrawPulley"), m.metalDark, maint, false).transform;
                    pul.position = pulleyAt; pul.rotation = Quaternion.Euler(90f, 0f, 0f);   // its axis along z
                    MBlock("DrawPulleyBracket", tw + new Vector3(0.4f, M.DrawPulleyH - 0.9f, 0.6f), pulleyAt + new Vector3(0.25f, 0.2f, 0.35f), m.metalDark, maint, false);
                    var drum = MeshObject("DrawDrum", SaveMesh(Lathe(new[] { new Vector2(0f, -1.0f), new Vector2(0.75f, -1.0f), new Vector2(0.75f, -0.85f), new Vector2(0.5f, -0.8f), new Vector2(0.5f, 0.8f), new Vector2(0.75f, 0.85f), new Vector2(0.75f, 1.0f), new Vector2(0f, 1.0f) }, 0f, 360f, 18, null, Vector3.zero), "Mars_DrawDrum"), m.metal, maint, false).transform;
                    drum.position = drumAt; drum.rotation = Quaternion.Euler(0f, 0f, 90f);       // its axis along x
                    foreach (var sx in new[] { -1.15f, 1.15f }) MBlock($"DrawDrumCheek{(sx < 0 ? "W" : "E")}", drumAt + new Vector3(sx - 0.15f, -drumAt.y + fy, -0.5f), drumAt + new Vector3(sx + 0.15f, 1.0f, 0.5f), m.stoneDark, maint, false);
                    // the chain links (placed along the chain each frame by MarsDrawbridge)
                    var links = new GameObject("DrawChain").transform; links.SetParent(maint, false);
                    var lv = new List<Vector3>(); var lt = new List<int>(); Sweep(lv, lt, Enumerable.Range(0, 13).Select(i => new Vector3(Mathf.Sin(i / 12f * Mathf.PI * 2f) * 0.07f, 0f, Mathf.Cos(i / 12f * Mathf.PI * 2f) * 0.13f)).ToList(), 0.028f, true, 5);
                    var linkMesh = SaveMesh(SmoothFinish(lv, lt), "Mars_DrawChainLink");
                    for (int i = 0; i < 140; i++) { var l = MeshObject($"Link{i:000}", linkMesh, m.metalDark, links, false); l.transform.localPosition = Vector3.zero; }
                    // the lock: a heavy bronze clamp on the drum (its shackle round the chain); it shatters in three pieces
                    var lockRoot = new GameObject("DrawLock").transform; lockRoot.SetParent(maint, false); lockRoot.position = M.DrawLock;
                    for (int k = 0; k < 3; k++)
                    {
                        var pv = new List<Vector3>(); var ptt = new List<int>();
                        if (k == 0) MBox(pv, ptt, new Vector3(0f, -0.1f, 0f), new Vector3(0.7f, 0.55f, 0.35f), 0f, 0.03f);
                        else { float s = k == 1 ? -1f : 1f; Sweep(pv, ptt, Enumerable.Range(0, 7).Select(i => new Vector3(s * 0.09f + Mathf.Cos(i / 6f * Mathf.PI * 0.5f + (s < 0 ? Mathf.PI * 0.5f : 0f)) * 0.26f, 0.18f + Mathf.Sin(i / 6f * Mathf.PI * 0.5f + (s < 0 ? Mathf.PI * 0.5f : 0f)) * 0.3f, 0f)).ToList(), 0.06f, false, 8); }
                        var piece = MeshObject($"LockPiece{k}", SaveMesh(SmoothFinish(pv, ptt), $"Mars_DrawLockPiece{k}"), m.metal, lockRoot, false); piece.transform.localPosition = Vector3.zero;
                    }
                    var lockGlow = MBlock("DrawLockGlow", new Vector3(-0.37f, -0.4f, -0.19f), new Vector3(0.37f, -0.36f, 0.19f), GlowMat("Mars_drawlock_glow"), lockRoot, false); lockGlow.transform.localPosition = new Vector3(0f, -0.38f, 0f);
                    // the order plaque: the west kingdom's half of the shared symbol, barred across - "closed by order"
                    {
                        var pq = new List<Vector3>(); var pqt = new List<int>(); var pc = tw + new Vector3(1.55f, 3.2f, 1.1f); var nrm = new Vector3(0.75f, 0f, 0.66f).normalized; var rt = Vector3.Cross(Vector3.up, nrm);
                        MBox(pq, pqt, pc - nrm * 0.05f, new Vector3(1.5f, 1.2f, 0.14f), Mathf.Atan2(nrm.x, nrm.z) * Mathf.Rad2Deg, 0.02f);
                        MeshObject("OrderPlaque", SaveMesh(Finish(pq, pqt), "Mars_OrderPlaque"), m.stoneDark, maint, false);
                        var sy = new List<Vector3>(); var syt = new List<int>();
                        var ring = Enumerable.Range(0, 11).Select(i => { float a = Mathf.PI * 0.5f + i / 10f * Mathf.PI; return pc + nrm * 0.04f + rt * Mathf.Cos(a) * 0.38f + Vector3.up * Mathf.Sin(a) * 0.38f; }).ToList();
                        Sweep(sy, syt, ring, 0.03f, false, 5);
                        Sweep(sy, syt, Enumerable.Range(0, 7).Select(i => pc + nrm * 0.04f + rt * (-0.04f - Mathf.Sin(i * 0.9f) * 0.1f) + Vector3.up * (-0.38f + i * 0.127f)).ToList(), 0.025f, false, 5);
                        Sweep(sy, syt, new[] { pc + nrm * 0.07f - rt * 0.55f - Vector3.up * 0.42f, pc + nrm * 0.07f + rt * 0.55f + Vector3.up * 0.42f }, 0.045f, false, 6);   // the bar across it
                        MeshObject("OrderPlaqueSymbol", SaveMesh(SmoothFinish(sy, syt), "Mars_OrderPlaqueSymbol"), m.stone, maint, false);
                    }
                    var db = span.gameObject.AddComponent<Seven.MarsDrawbridge>();
                    db.Span = span; db.Drum = drum; db.Links = links; db.Lock = lockRoot; db.LockGlow = lockGlow.GetComponent<Renderer>();
                    db.Pulley = pulleyAt; db.DrumTop = drumAt + Vector3.up * 0.55f + Vector3.forward * 0.0f; db.TipLocal = new Vector3(M.DrawLen - 0.3f, 0.32f, 0f);
                    db.Set(0f);
                }
                // lookouts with lamps (clear stopping points)
                foreach (var p in M.Lookouts)
                {
                    PointLight($"LookoutLamp{(int)p.x}_Light", p + Vector3.up * 2.9f, 18f, 1.2f, maint);
                    MeshObject($"LookoutPost{(int)p.x}", SaveMesh(Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.45f, 0f), new Vector2(0.4f, 0.3f), new Vector2(0.16f, 0.45f), new Vector2(0.12f, 2.9f), new Vector2(0.5f, 3.05f), new Vector2(0.55f, 3.25f), new Vector2(0.3f, 3.6f), new Vector2(0.06f, 3.85f), new Vector2(0f, 3.9f) }, 0f, 360f, 12, null, p), $"Mars_LookoutPost{(int)p.x}"), m.metal, maint);
                    MeshObject($"LookoutLantern{(int)p.x}", SaveMesh(Lathe(new[] { new Vector2(0f, 3.05f), new Vector2(0.28f, 3.08f), new Vector2(0.3f, 3.2f), new Vector2(0f, 3.24f) }, 0f, 360f, 10, null, p), $"Mars_LookoutLantern{(int)p.x}"), m.glow, maint, false);
                }
                // rubble along the floor (props, outside the zone edges)
                for (int i = 0; i < 40; i++) { var p = new Vector3((float)rnd.NextDouble() * 96f - 48f, fy, -96f - (float)rnd.NextDouble() * 60f); Lump(v, t, p + Vector3.up * 0.8f, Vector3.one * (0.8f + (float)rnd.NextDouble() * 1.6f), 0.4f, rnd.Next(1000)); }
                for (int i = 0; i < 30; i++) { var p = new Vector3((float)rnd.NextDouble() * 96f - 48f, fy, -10f + (float)rnd.NextDouble() * 120f); if (Mathf.Abs(p.x) < 8f) continue; Lump(v, t, p + Vector3.up * 0.8f, Vector3.one * (0.8f + (float)rnd.NextDouble() * 1.8f), 0.4f, rnd.Next(1000)); }
                MeshObject("FloorRubble", SaveMesh(SmoothFinish(v, t), "Mars_FloorRubble"), m.rock, maint);
            }
            var bridgeObj = maint.Find("Drawbridge");
            var slab = bridgeObj; var slabEdges = bridgeObj.GetComponent<Seven.MarsDrawbridge>().LockGlow;
            // QF18 (12): the service gate - a gateway in the old masonry over the east ramp's top, a heavy ribbed door that
            // sinks into its slot, seam lights along its top (dark while unpowered), the power housing beside it, and its
            // power line down the ramp's inner wall - deliberately cut during the division (the two severed ends hang apart)
            var gk = new Kit(); Gateway(gk.sv, gk.st, M.ServiceGateAt, M.GateSpan, 4.8f, 1.8f, 0f); gk.Save("ServiceGateway", maint, m.wall, m.rockDark, m.stoneDark, m.glow, m.cloth);
            var gate = new GameObject("ServiceGate").transform; gate.SetParent(maint, false); gate.position = M.ServiceGateAt;
            {
                var dv = new List<Vector3>(); var dt = new List<int>(); float w = M.GateSpan + 0.2f, h = 4.7f;
                MBox(dv, dt, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, 0.22f), 0f, 0.0f);
                for (float x = -w * 0.5f + 0.5f; x < w * 0.5f - 0.2f; x += 0.9f) MBox(dv, dt, new Vector3(x, h * 0.5f, -0.16f), new Vector3(0.16f, h - 0.2f, 0.14f), 0f, 0.0f);
                foreach (var y in new[] { 0.6f, 2.3f, 4.0f }) MBox(dv, dt, new Vector3(0f, y, -0.25f), new Vector3(w - 0.1f, 0.3f, 0.12f), 0f, 0.0f);
                var door = MeshObject("GateDoor", SaveMesh(Finish(dv, dt), "Mars_GateDoor"), m.metalDark, gate, false); door.transform.localPosition = Vector3.zero;
                var seam = MBlock("GateSeam", new Vector3(-w * 0.5f, h - 0.08f, -0.14f), new Vector3(w * 0.5f, h - 0.02f, 0.14f), GlowMat("Mars_gate_seam"), gate, false); seam.transform.localPosition = new Vector3(0f, h - 0.05f, 0f);
            }
            var housingAt = M.ServiceGateAt + new Vector3(M.GateSpan * 0.5f + 1.9f, 0f, -1.4f);
            MBlock("GateHousing", housingAt + new Vector3(-0.5f, 0f, -0.45f), housingAt + new Vector3(0.5f, 2.2f, 0.45f), m.metalDark, maint, false);
            var gatePanel = MBlock("GateHousingLight", housingAt + new Vector3(-0.42f, 1.5f, -0.48f), housingAt + new Vector3(0.42f, 1.9f, -0.46f), GlowMat("Mars_gate_housing"), maint, false).GetComponent<Renderer>();
            // the power line: from the housing down the inner wall of the ramp to the cut, the live feed below it
            float RampY(float z) => Mathf.Lerp(fy, 0f, (z - M.RampFootZ) / (-12.6f - M.RampFootZ));
            var wallX = 59.72f;
            var upper = new List<Vector3> { housingAt + new Vector3(0f, 1.2f, -0.5f), new Vector3(wallX, 1.3f, -13.6f) };
            for (float z = -16f; z > M.GateBreakZ + 1.2f; z -= 3f) upper.Add(new Vector3(wallX, RampY(z) + 1.3f, z));
            upper.Add(new Vector3(wallX, RampY(M.GateBreakZ + 1.1f) + 1.3f, M.GateBreakZ + 1.1f));
            upper.Reverse();                                                    // segment 0 = at the cut, the last one = the housing
            var gateLine = PowerLineObj("GateLine", upper.ToArray(), 12, 0.065f, GlowMat("Mars_gate_line"), m.metalDark, maint);
            var feed = new List<Vector3>(); for (float z = M.RampFootZ + 2f; z < M.GateBreakZ - 1.1f; z += 3f) feed.Add(new Vector3(wallX, RampY(z) + 1.3f, z)); feed.Add(new Vector3(wallX, RampY(M.GateBreakZ - 1.1f) + 1.3f, M.GateBreakZ - 1.1f));
            var feedLine = PowerLineObj("GateFeed", feed.ToArray(), 5, 0.065f, GlowMat("Mars_gate_feed"), m.metalDark, maint);
            // the two severed ends (they hang apart; telekinesis pulls them together)
            Transform CableEnd(string n, Vector3 at, Vector3 droop)
            {
                var root = new GameObject(n).transform; root.SetParent(maint, false); root.position = at;
                var cv = new List<Vector3>(); var ctt = new List<int>(); var pts = Enumerable.Range(0, 8).Select(i => { float u = i / 7f; return droop * u + Vector3.down * Mathf.Sin(u * Mathf.PI * 0.5f) * 0.25f * u; }).ToList();
                Sweep(cv, ctt, pts, 0.07f, false, 8);
                var go = MeshObject(n + "_Cable", SaveMesh(SmoothFinish(cv, ctt), "Mars_" + n), m.metalDark, root, false); go.transform.localPosition = Vector3.zero;
                var tip = new List<Vector3>(); var tipt = new List<int>(); Ellipsoid(tip, tipt, droop, new Vector3(0.1f, 0.1f, 0.1f), 5, 8);
                var tg = MeshObject(n + "_Tip", SaveMesh(SmoothFinish(tip, tipt), "Mars_" + n + "_Tip"), GlowMat("Mars_" + n + "_tip"), root, false); tg.transform.localPosition = Vector3.zero;
                return root;
            }
            var endUp = CableEnd("GateCableEndUpper", new Vector3(wallX, RampY(M.GateBreakZ + 1.1f) + 1.3f, M.GateBreakZ + 1.1f), new Vector3(-0.25f, -1.05f, -0.15f));
            var endLo = CableEnd("GateCableEndLower", new Vector3(wallX, RampY(M.GateBreakZ - 1.1f) + 1.3f, M.GateBreakZ - 1.1f), new Vector3(-0.85f, -1.15f, 0.55f));

            // ---------------------------------------------------------------- the rims, the overlook + mechanism, the controls, the relay, the pier walk
            var anc = new GameObject("Ancient").transform; anc.SetParent(world, false);
            // QF18 (15): the shared mechanism on the overlook - a carved stone housing, two great bronze gear wheels standing
            // on it (they turn when the spans align), a central axle, two weights hanging on chains, the channel feeding in
            var mech = MeshObject("SharedMechanism", SaveMesh(Lathe(new[] { new Vector2(0f, 0f), new Vector2(2.6f, 0f), new Vector2(2.5f, 0.3f), new Vector2(2.25f, 0.45f), new Vector2(2.2f, 1.6f), new Vector2(2.45f, 1.75f), new Vector2(2.45f, 1.95f), new Vector2(0f, 1.95f) }, 0f, 360f, 28, null, M.MechanismAt), "Mars_Mechanism"), m.stone, anc).transform;
            var gears = new List<Transform>();
            foreach (var gx in new[] { -0.9f, 0.9f })
            {
                var g = new GameObject(gx < 0 ? "MechGearW" : "MechGearE").transform; g.SetParent(anc, false); g.position = M.MechanismAt + new Vector3(gx, 3.35f, 0f); g.rotation = Quaternion.Euler(0f, 0f, 90f);
                var gv = new List<Vector3>(); var gt = new List<int>();
                Add(gv, gt, Lathe(new[] { new Vector2(0f, -0.14f), new Vector2(1.25f, -0.14f), new Vector2(1.25f, 0.14f), new Vector2(0f, 0.14f) }, 0f, 360f, 32, null, Vector3.zero));
                for (int k = 0; k < 18; k++) { var a = k * 20f; MBox(gv, gt, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0f, 1.38f), new Vector3(0.24f, 0.26f, 0.3f), a, 0.0f); }
                var gm = MeshObject(g.name + "_Wheel", SaveMesh(Finish(gv, gt), "Mars_" + g.name), m.metal, g, false); gm.transform.localPosition = Vector3.zero; gm.transform.localRotation = Quaternion.identity;
                gears.Add(g);
            }
            {
                var mv2 = new List<Vector3>(); var mt2 = new List<int>();
                foreach (var gx in new[] { -1.4f, 1.4f }) MBox(mv2, mt2, M.MechanismAt + new Vector3(gx, 2.7f, 0f), new Vector3(0.35f, 1.6f, 0.6f), 0f, 0.03f);     // the gear stands
                Sweep(mv2, mt2, new[] { M.MechanismAt + new Vector3(-1.7f, 3.35f, 0f), M.MechanismAt + new Vector3(1.7f, 3.35f, 0f) }, 0.12f, false, 10);   // the axle
                foreach (var gz in new[] { -2.0f, 2.0f })
                {
                    Sweep(mv2, mt2, new[] { M.MechanismAt + new Vector3(0f, 3.35f, gz * 0.4f), M.MechanismAt + new Vector3(0f, 1.6f, gz) }, 0.04f, false, 5);
                    Add(mv2, mt2, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.32f, 0.05f), new Vector2(0.36f, 0.6f), new Vector2(0.2f, 0.75f), new Vector2(0f, 0.78f) }, 0f, 360f, 12, null, M.MechanismAt + new Vector3(0f, 0.85f, gz)));
                }
                MeshObject("MechFrame", SaveMesh(SmoothFinish(mv2, mt2), "Mars_MechFrame"), m.metalDark, anc);
            }
            // QF18 (15, 16): the channels - power lines whose light visibly runs. West: the relay on the rim -> north along the
            // rim -> down the bank wall -> along the wall under the old bridge (the displaced section) -> up -> the west control.
            var chWA = PowerLineObj("ChannelWestA", new[] { M.RelayWest + new Vector3(0f, 2.2f, 0f), M.RelayWest + new Vector3(0f, 0.12f, 0.6f), new Vector3(-52.6f, 0.12f, 110f), new Vector3(-52.6f, 0.12f, 140f), new Vector3(-50.3f, 0.12f, 141f), new Vector3(-50.3f, -3.0f, 142.5f), new Vector3(-50.3f, -3.0f, M.PierConduit.z - 1.05f) }, 16, 0.08f, GlowMat("Mars_channelWA"), m.metalDark, anc, 0.9f);
            var chWB = PowerLineObj("ChannelWestB", new[] { new Vector3(-50.3f, -3.0f, M.PierConduit.z + 1.05f), new Vector3(-50.3f, -3.0f, 159.2f), new Vector3(-50.3f, 0.12f, 159.9f), new Vector3(-53.5f, 0.12f, 159.9f), new Vector3(-58.2f, 0.12f, 153.4f), new Vector3(-59.0f, 0.12f, 152.2f), M.WestControl + new Vector3(0.75f, 0.9f, 0f) }, 8, 0.08f, GlowMat("Mars_channelWB"), m.metalDark, anc, 0.9f);
            var chE = PowerLineObj("ChannelEast", new[] { M.EastControl + new Vector3(-0.9f, 0.9f, 0f), M.EastControl + new Vector3(-1.2f, 0.12f, -1f), new Vector3(60f, 0.12f, 140f), M.MechanismAt + new Vector3(0f, 0.12f, 3.0f), M.MechanismAt + new Vector3(0f, 1.0f, 2.3f) }, 10, 0.08f, GlowMat("Mars_channelE"), m.metalDark, anc, 1.2f);
            // QF18 (15): the two temple controls - carved stone-and-bronze pedestals, a big bronze wheel on each, one kingdom's
            // half of the shared symbol carved on each slanted top (worked together, the halves make the temple's design)
            (Renderer glow, Transform wheel) Control(string n, Vector3 p, float side, Vector3 face)
            {
                var yaw = Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg; var q = Quaternion.Euler(0f, yaw, 0f);
                MeshObject(n + "Base", SaveMesh(Lathe(new[] { new Vector2(0f, 0f), new Vector2(1.05f, 0f), new Vector2(1.0f, 0.22f), new Vector2(0.78f, 0.34f), new Vector2(0.62f, 0.5f), new Vector2(0.58f, 0.95f), new Vector2(0.8f, 1.05f), new Vector2(0.84f, 1.18f), new Vector2(0f, 1.18f) }, 0f, 360f, 8, null, p), "Mars_" + n + "Base"), m.stone, anc);
                var tv = new List<Vector3>(); var tt = new List<int>();
                MBox(tv, tt, p + q * new Vector3(0f, 1.32f, 0f), new Vector3(1.3f, 0.28f, 1.1f), yaw, 0.02f);
                MeshObject(n + "Top", SaveMesh(Finish(tv, tt), "Mars_" + n + "TopStone"), m.stoneDark, anc);
                // the carved half-symbol on the top (half ring + wave + three dots, as on the temple wall)
                var sv = new List<Vector3>(); var stt = new List<int>(); var c = p + Vector3.up * 1.48f;
                var ring = Enumerable.Range(0, 13).Select(i => { float a = (side < 0 ? 90f : -90f) + i / 12f * 180f; return c + q * new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 0.42f, 0f, Mathf.Sin(a * Mathf.Deg2Rad) * 0.42f); }).ToList();
                Sweep(sv, stt, ring, 0.03f, false, 5);
                Sweep(sv, stt, Enumerable.Range(0, 8).Select(i => c + q * new Vector3(Mathf.Sin(i * 0.9f) * 0.1f * side + side * 0.02f, 0f, -0.42f + i * 0.12f)).ToList(), 0.025f, false, 5);
                for (int k = 0; k < 3; k++) Ellipsoid(sv, stt, c + q * new Vector3(side * (0.2f + k * 0.06f), 0f, 0.22f - k * 0.22f), new Vector3(0.04f, 0.02f, 0.04f), 4, 6);
                var glow = MeshObject(n + "Symbol", SaveMesh(SmoothFinish(sv, stt), "Mars_" + n + "Symbol"), GlowMat("Mars_" + n + "_glow"), anc, false).GetComponent<Renderer>();
                // the bronze wheel on the front face (toward the operator), turned when it is worked
                var wh = new GameObject(n + "Wheel").transform; wh.SetParent(anc, false); wh.position = p + q * new Vector3(0f, 0.95f, -0.78f); wh.rotation = q;
                var wv = new List<Vector3>(); var wt = new List<int>();
                Sweep(wv, wt, Enumerable.Range(0, 21).Select(i => new Vector3(Mathf.Cos(i / 20f * Mathf.PI * 2f) * 0.5f, Mathf.Sin(i / 20f * Mathf.PI * 2f) * 0.5f, 0f)).ToList(), 0.045f, true, 6);
                for (int k = 0; k < 6; k++) { float a = k * 60f * Mathf.Deg2Rad; Sweep(wv, wt, new[] { Vector3.zero, new Vector3(Mathf.Cos(a) * 0.5f, Mathf.Sin(a) * 0.5f, 0f) }, 0.025f, false, 5); Ellipsoid(wv, wt, new Vector3(Mathf.Cos(a) * 0.55f, Mathf.Sin(a) * 0.55f, -0.04f), new Vector3(0.05f, 0.05f, 0.07f), 4, 6); }
                Add(wv, wt, Lathe(new[] { new Vector2(0f, -0.12f), new Vector2(0.1f, -0.12f), new Vector2(0.1f, 0.12f), new Vector2(0f, 0.12f) }, 0f, 360f, 10, null, Vector3.zero));
                var wm = MeshObject(n + "WheelMesh", SaveMesh(SmoothFinish(wv, wt), "Mars_" + n + "Wheel"), m.metal, wh, false); wm.transform.localPosition = Vector3.zero; wm.transform.localRotation = Quaternion.identity;
                return (glow, wh);
            }
            var (ctlW, wheelW) = Control("ControlWest", M.WestControl, -1f, Vector3.right);
            var (ctlE, wheelE) = Control("ControlEast", M.EastControl, 1f, Vector3.left);
            // QF18 (15, 16): the relay on the west rim - a carved pillar, a housing of four ribs on top, the lens crystal in it
            // (knocked askew; she seats it back, X, and its light runs down the channel)
            MeshObject("RelayPillar", SaveMesh(Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.7f, 0f), new Vector2(0.62f, 0.3f), new Vector2(0.42f, 0.45f), new Vector2(0.36f, 2.0f), new Vector2(0.55f, 2.15f), new Vector2(0.6f, 2.3f), new Vector2(0f, 2.3f) }, 0f, 360f, 12, null, M.RelayWest), "Mars_RelayPillar"), m.stone, anc);
            { var rv = new List<Vector3>(); var rt = new List<int>(); for (int k = 0; k < 4; k++) { float a = k * 90f + 45f; var b = M.RelayWest + Polar(0.45f, a, 2.3f); var top = M.RelayWest + Polar(0.18f, a, 3.45f); Sweep(rv, rt, new[] { b, M.RelayWest + Polar(0.5f, a, 2.85f), top }, 0.05f, false, 5); } Add(rv, rt, Lathe(new[] { new Vector2(0f, 3.4f), new Vector2(0.24f, 3.42f), new Vector2(0.12f, 3.6f), new Vector2(0f, 3.62f) }, 0f, 360f, 8, null, M.RelayWest)); MeshObject("RelayHousing", SaveMesh(SmoothFinish(rv, rt), "Mars_RelayHousing"), m.metal, anc, false); }
            var relayT = new GameObject("RelayCrystal").transform; relayT.SetParent(anc, false); relayT.position = M.RelayWest + Vector3.up * 2.85f; relayT.rotation = Quaternion.Euler(38f, 0f, 24f);
            var relay = MeshObject("RelayCrystalMesh", SaveMesh(Lathe(new[] { new Vector2(0f, -0.38f), new Vector2(0.2f, 0f), new Vector2(0f, 0.38f) }, 0f, 360f, 6, null, Vector3.zero), "Mars_RelayCrystal"), GlowMat("Mars_relay_glow"), relayT, false).GetComponent<Renderer>();
            relay.transform.localPosition = Vector3.zero;
            // QF18 (16): the pier walk - carved steps down the OUTSIDE of the bank wall north of the old bridge (before they
            // were a slope inside the solid bank, so 7 walked through the ground and the camera sat in the rock), a lit ledge
            // walk along the wall under the west span, the conduit along the wall with its displaced section
            {
                var v = new List<Vector3>(); var t = new List<int>(); var lv = new List<Vector3>(); var lt = new List<int>();
                int steps = 10; float z0 = M.PierStairZ0, z1 = M.PierStairZ1;
                for (int i = 0; i < steps; i++)
                {
                    float za = Mathf.Lerp(z0, z1, i / (float)steps), zb = Mathf.Lerp(z0, z1, (i + 1) / (float)steps);
                    float ytop = Mathf.Lerp(-4f, 0f, (i + 1) / (float)steps);
                    MBox(v, t, new Vector3(-48.8f, (ytop - 4.6f) * 0.5f, (za + zb) * 0.5f), new Vector3(2.4f, ytop + 4.6f, zb - za + 0.02f), 0f, 0f);
                }
                MBox(v, t, new Vector3(-48.8f, -2.3f, z1 + 1.3f), new Vector3(2.4f, 4.6f, 2.6f), 0f, 0f);                         // the landing at the top
                for (float z = M.PierWalkZ0 + 1.5f; z < z0; z += 3f) MBox(lv, lt, new Vector3(-48.5f, -5.6f, z), new Vector3(1.4f, 2f, 0.6f), 0f, 0.03f);   // corbels under the walk
                for (float z = M.PierWalkZ0; z < z1 + 2.4f; z += 2.2f) MBox(lv, lt, new Vector3(-47.35f, z < z0 ? -3.55f : Mathf.Lerp(-4f, 0f, Mathf.InverseLerp(z0, z1, z)) + 0.45f, z + 1f), new Vector3(0.3f, 0.9f, 1.9f), 0f, 0.02f);   // the low parapet
                MeshObject("PierStairs", SaveMesh(Finish(v, t), "Mars_PierStairs"), m.stone, anc);
                MeshObject("PierParapet", SaveMesh(Finish(lv, lt), "Mars_PierParapet"), m.stoneDark, anc);
                MGround("PierWalkDeck", new Vector3(-50f, -4.6f, M.PierWalkZ0), new Vector3(-47.2f, -4f, z0 + 0.02f), m.stone, anc);
                foreach (var z in new[] { 156.4f, 159.6f, 163f, 167f }) MarsLamp($"PierLamp{(int)z}", new Vector3(-49.75f, z < z0 ? -1.9f : Mathf.Lerp(-4f, 0f, Mathf.InverseLerp(z0, z1, z)) + 2.1f, z), 9f, 1.1f, m.glow, anc, new Vector3(0.24f, 0.24f, 0.24f));
                // the abutments that join the bank to the spans (a 3 m hole in the walkway before)
                MGround("SpanAbutW", new Vector3(-50f, -1.2f, M.SpanZ0 - 1f), new Vector3(-46.3f, 0f, M.SpanZ1 + 1f), m.temple, anc);
                MGround("SpanAbutE", new Vector3(46.3f, -1.2f, M.SpanZ0 - 1f), new Vector3(50f, 0f, M.SpanZ1 + 1f), m.temple, anc);
            }
            // the displaced conduit section: a 2.1 m banded pipe with a glowing core, knocked out of line onto the walk
            var conduit = new GameObject("PierConduit").transform; conduit.SetParent(anc, false);
            {
                var cv = new List<Vector3>(); var ctt = new List<int>(); var kv = new List<Vector3>(); var kt = new List<int>();
                for (float zz = -0.95f; zz <= 0.96f; zz += 0.45f) Sweep(cv, ctt, Enumerable.Range(0, 11).Select(i => new Vector3(Mathf.Cos(i / 10f * Mathf.PI * 2f) * 0.15f, Mathf.Sin(i / 10f * Mathf.PI * 2f) * 0.15f, zz)).ToList(), 0.036f, true, 5);
                foreach (var zz in new[] { -1.02f, 1.02f }) Add(cv, ctt, Lathe(new[] { new Vector2(0f, -0.05f), new Vector2(0.22f, -0.05f), new Vector2(0.22f, 0.05f), new Vector2(0f, 0.05f) }, 0f, 360f, 10, null, Vector3.zero).Let(mm => { var vs = mm.vertices; for (int i = 0; i < vs.Length; i++) vs[i] = new Vector3(vs[i].x, vs[i].z, vs[i].y + zz); mm.vertices = vs; mm.RecalculateNormals(); return mm; }));
                Sweep(kv, kt, new[] { new Vector3(0f, 0f, -1.05f), new Vector3(0f, 0f, 1.05f) }, 0.08f, false, 7);
                var cm = MeshObject("ConduitBands", SaveMesh(SmoothFinish(cv, ctt), "Mars_ConduitBands"), m.metalDark, conduit, false); cm.transform.localPosition = Vector3.zero;
                var core = MeshObject("ConduitCore", SaveMesh(SmoothFinish(kv, kt), "Mars_ConduitCore"), GlowMat("Mars_conduit_core"), conduit, false); core.transform.localPosition = Vector3.zero;
                conduit.position = M.PierConduitRest; conduit.rotation = M.PierConduitRestRot;
            }
            // the sockets either side of the gap on the wall
            foreach (var dz in new[] { -1.12f, 1.12f }) MeshObject($"ConduitSocket{(dz < 0 ? "S" : "N")}", SaveMesh(Lathe(new[] { new Vector2(0f, -0.08f), new Vector2(0.24f, -0.08f), new Vector2(0.24f, 0.08f), new Vector2(0f, 0.08f) }, 0f, 360f, 10, null, Vector3.zero).Let(mm => { var vs = mm.vertices; for (int i = 0; i < vs.Length; i++) vs[i] = new Vector3(vs[i].x, vs[i].z, vs[i].y) + M.PierConduit + new Vector3(0f, 0f, dz); mm.vertices = vs; mm.RecalculateBounds(); return mm; }), $"Mars_ConduitSocket{dz}"), m.metalDark, anc, false);

            // ---------------------------------------------------------------- the ancient bridge's two spans (segments, misaligned until section 6)
            Transform Span(string n, float x0, float x1)
            {
                var root = new GameObject(n).transform; root.SetParent(anc, false); root.position = new Vector3((x0 + x1) * 0.5f, 0f, (M.SpanZ0 + M.SpanZ1) * 0.5f);
                int segs = 4; float len = (x1 - x0) / segs;
                for (int i = 0; i < segs; i++)
                {
                    var seg = new GameObject($"Seg{i}").transform; seg.SetParent(root, false); seg.localPosition = new Vector3(-(x1 - x0) * 0.5f + len * (i + 0.5f), 0f, 0f);
                    var v = new List<Vector3>(); var t = new List<int>();
                    MBox(v, t, new Vector3(0f, -0.6f, 0f), new Vector3(len, 1.2f, 8f), 0f, 0f);   // QF18 (13): full length - no 10 cm cracks between the segments
                    MBox(v, t, new Vector3(0f, 0.5f, -3.9f), new Vector3(len - 0.1f, 1.0f, 0.3f)); MBox(v, t, new Vector3(0f, 0.5f, 3.9f), new Vector3(len - 0.1f, 1.0f, 0.3f));
                    MBox(v, t, new Vector3(0f, -3.2f, 0f), new Vector3(len * 0.5f, 4f, 5f));                         // the arch's key under the deck
                    var go = MeshObject($"{n}_Seg{i}", SaveMesh(Finish(v, t), $"Mars_{n}_Seg{i}"), m.temple, seg); go.transform.localPosition = Vector3.zero;
                    var lv = new List<Vector3>(); var lt = new List<int>(); MBox(lv, lt, new Vector3(0f, 0.02f, 0f), new Vector3(len - 0.4f, 0.04f, 0.18f));
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
            { var g = MBlock("SwingSection", new Vector3(-1.5f, -0.5f, -0.4f), new Vector3(1.5f, 0.5f, 6f), m.stoneDark, swing, false); g.transform.localPosition = Vector3.zero; }

            // ---------------------------------------------------------------- the temple island: the plaza, the lanes, the arena
            var isl = new GameObject("TempleIsland").transform; isl.SetParent(world, false);
            MeshObject("IslandRock", SaveMesh(Lathe(new[] { new Vector2(0f, fy - 1f), new Vector2(30f, fy - 1f), new Vector2(27f, fy + 6f), new Vector2(25f, -3f), new Vector2(24f, -0.6f), new Vector2(0f, -0.6f) }, 0f, 360f, 36, null, new Vector3(0f, 0f, 152f)), "Mars_IslandRock"), m.rockDark, isl);
            // the plaza decks: west lane, centre (south / north), east (south / north) - the broken strip in the east lane left open
            MGround("PlazaDeckW", new Vector3(-21f, -0.6f, 124f), new Vector3(-6.7f, 0f, 162f), m.stone, isl);
            MGround("PlazaDeckC", new Vector3(-6.7f, -0.6f, 124f), new Vector3(6.7f, 0f, 162f), m.stone, isl);
            MGround("PlazaDeckE", new Vector3(6.7f, -0.6f, 124f), new Vector3(21f, 0f, 162f), m.stone, isl);
            MGround("PlazaDeckNW", new Vector3(-21f, -0.6f, 162f), new Vector3(-6f, 0f, 168f), m.stone, isl);
            MGround("PlazaDeckNE", new Vector3(6f, -0.6f, 162f), new Vector3(21f, 0f, 168f), m.stone, isl);
            // QF17 (1): the plaza is one open arena now (the boss fight moves) - the old lane walls are gone; Viper's raised platform
            var dais = new GameObject("ViperDais").transform; dais.SetParent(isl, false); dais.position = Seven.ViperFight.DaisAt;
            {
                float top = Seven.ViperFight.DaisTop;
                var dm = MeshObject("DaisBody", SaveMesh(Lathe(new[] { new Vector2(0f, -0.1f), new Vector2(3.0f, -0.1f), new Vector2(2.95f, 0.5f), new Vector2(2.55f, 0.62f), new Vector2(2.3f, top - 0.55f), new Vector2(2.75f, top - 0.35f), new Vector2(2.75f, top), new Vector2(0f, top) }, 0f, 360f, 32, null, Vector3.zero), "Mars_ViperDais"), m.metalDark, dais, false);
                dm.transform.localPosition = Vector3.zero;
                var v = new List<Vector3>(); var t = new List<int>();
                Sweep(v, t, Enumerable.Range(0, 33).Select(i => new Vector3(Mathf.Cos(i / 32f * Mathf.PI * 2f) * 2.78f, top - 0.18f, Mathf.Sin(i / 32f * Mathf.PI * 2f) * 2.78f)).ToList(), 0.05f, true, 6);
                var rg = MeshObject("DaisGlowRing", SaveMesh(SmoothFinish(v, t), "Mars_ViperDaisRing"), m.glow, dais, false); rg.transform.localPosition = Vector3.zero;
                dais.position = Seven.ViperFight.DaisAt + Vector3.down * top;     // sunk until he raises it
            }
            // Viper's barriers (energy walls) and their emitters
            GameObject EnergyWall(string n, Vector3 a, Vector3 b) { var g = MBlock(n, Vector3.Min(a, b), Vector3.Max(a, b), m.barrier, isl, false); g.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off; return g; }
            var bWC = EnergyWall("BarrierWestGate", new Vector3(-6.85f, 0f, 136f), new Vector3(-6.55f, 3.2f, 140f));
            var bCE = EnergyWall("BarrierEastGate", new Vector3(6.55f, 0f, 156f), new Vector3(6.85f, 3.2f, 160f));
            var bC = EnergyWall("BarrierCentre", new Vector3(-6.4f, 0f, 150.35f), new Vector3(6.4f, 3.2f, 150.65f));
            var slide = MBlock("MovingSection", new Vector3(-0.35f, 0f, -2f), new Vector3(0.35f, 2.6f, 2f), m.metalDark, isl, false).transform; slide.position = new Vector3(6.7f, 0f, M.SlideGateZ);
            var fallen = MBlock("FallenWalkway", new Vector3(-5f, -0.3f, -2.6f), new Vector3(5f, 0f, 2.6f), m.stone, isl, false).transform; fallen.position = M.FallenWalkRest; fallen.rotation = Quaternion.Euler(14f, 8f, -18f);
            // QF18 (15, 19b): Viper's pylons - clearly HIS tech (angular, dark metal, a hard white emitter), three hinged armour
            // panels that fall open when she shuts one down (MarsPylon: the light dies, sparks, the panels open, the head slumps)
            var pylons = new List<Renderer>(); var pylonObjs = new List<Seven.MarsPylon>();
            for (int i = 0; i < M.Pylons.Length; i++)
            {
                var p = M.Pylons[i];
                var root = new GameObject($"Pylon{i}").transform; root.SetParent(isl, false); root.position = p;
                var bm = MeshObject($"Pylon{i}_Body", SaveMesh(Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.78f, 0f), new Vector2(0.7f, 0.32f), new Vector2(0.42f, 0.5f), new Vector2(0.34f, 2.3f), new Vector2(0.5f, 2.42f), new Vector2(0.28f, 2.55f), new Vector2(0f, 2.55f) }, 0f, 360f, 6, null, Vector3.zero), $"Mars_Pylon{i}"), m.metalDark, root, true);
                bm.transform.localPosition = Vector3.zero;
                var panels = new List<Transform>();
                for (int k = 0; k < 3; k++)
                {
                    float a = k * 120f + 30f;
                    var hinge = new GameObject($"Panel{k}").transform; hinge.SetParent(root, false); hinge.localPosition = Polar(0.44f, a, 0.55f); hinge.localRotation = Quaternion.identity;
                    var pivot = new GameObject("Pivot").transform; pivot.SetParent(hinge, false); pivot.localRotation = Quaternion.Euler(0f, 90f - a, 0f);
                    var pv = new List<Vector3>(); var ptt = new List<int>();
                    MBox(pv, ptt, new Vector3(0f, 0.8f, 0.04f), new Vector3(0.42f, 1.55f, 0.07f), 0f, 0f);
                    MBox(pv, ptt, new Vector3(0f, 1.2f, 0.09f), new Vector3(0.12f, 0.9f, 0.05f), 0f, 0f);
                    var pm = MeshObject($"Pylon{i}_Panel{k}", SaveMesh(Finish(pv, ptt), $"Mars_Pylon{i}_Panel{k}"), m.metal, pivot, false); pm.transform.localPosition = Vector3.zero; pm.transform.localRotation = Quaternion.identity;
                    panels.Add(pivot);
                }
                var head = new GameObject("Head").transform; head.SetParent(root, false); head.localPosition = new Vector3(0f, 2.55f, 0f);
                var hr = MeshObject($"PylonGlow{i}", SaveMesh(Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.3f, 0.18f), new Vector2(0.22f, 0.42f), new Vector2(0f, 0.62f) }, 0f, 360f, 4, null, Vector3.zero), $"Mars_PylonGlow{i}"), GlowMat($"Mars_pylon_glow{i}"), head, false).GetComponent<Renderer>();
                hr.transform.localPosition = Vector3.zero;
                var lamp = PointLight($"Pylon{i}_Lamp", p + Vector3.up * 3.0f, 9f, 3.2f, head); lamp.shadows = LightShadows.None;
                var mp = root.gameObject.AddComponent<Seven.MarsPylon>(); mp.Head = hr; mp.Lamp = lamp; mp.Panels = panels.ToArray(); mp.HeadT = head;
                pylons.Add(hr); pylonObjs.Add(mp);
            }
            var restraint = new GameObject("ViperRestraint"); restraint.transform.SetParent(isl, false);
            { var v = new List<Vector3>(); var t = new List<int>(); for (int k = 0; k < 3; k++) Sweep(v, t, Enumerable.Range(0, 28).Select(i => new Vector3(Mathf.Cos(i / 28f * Mathf.PI * 2f) * 0.75f, 0.3f + k * 0.5f, Mathf.Sin(i / 28f * Mathf.PI * 2f) * 0.75f)).ToList(), 0.035f, true, 6);
              var r = MeshObject("RestraintRings", SaveMesh(SmoothFinish(v, t), "Mars_Restraint"), m.glow, restraint.transform, false); r.transform.localPosition = Vector3.zero; restraint.SetActive(false); }
            // QF18 (17): Viper's sentries - angular dark-metal drones about a metre across, a hard white eye, a searchlight that
            // sweeps ahead (MarsSentry), side fins; they fly at the worker's control and attack it
            var jammers = new List<Transform>();
            for (int i = 0; i < 3; i++)
            {
                var j = new GameObject($"Jammer{i}").transform; j.SetParent(world, false); j.position = M.JammerFrom[i];
                var body = new GameObject("Body").transform; body.SetParent(j, false);
                var v = new List<Vector3>(); var t = new List<int>();
                Add(v, t, Lathe(new[] { new Vector2(0f, -0.28f), new Vector2(0.34f, -0.18f), new Vector2(0.52f, 0f), new Vector2(0.36f, 0.16f), new Vector2(0.14f, 0.3f), new Vector2(0f, 0.32f) }, 0f, 360f, 6, null, Vector3.zero));
                foreach (var sx in new[] { -1f, 1f }) MBox(v, t, new Vector3(sx * 0.62f, 0.02f, -0.1f), new Vector3(0.36f, 0.06f, 0.5f), sx * 18f, 0f);
                MBox(v, t, new Vector3(0f, 0.36f, -0.18f), new Vector3(0.06f, 0.16f, 0.34f), 0f, 0f);
                var b = MeshObject($"Jammer{i}_Body", SaveMesh(Finish(v, t), $"Mars_Jammer{i}"), m.metalDark, body, false); b.transform.localPosition = Vector3.zero;
                var ev = new List<Vector3>(); var et = new List<int>(); Ellipsoid(ev, et, new Vector3(0f, -0.02f, 0.46f), new Vector3(0.13f, 0.09f, 0.07f), 6, 10);
                var gl = MeshObject($"Jammer{i}_Eye", SaveMesh(SmoothFinish(ev, et), $"Mars_JammerEye{i}"), GlowMat($"Mars_jammer_eye{i}"), body, false); gl.transform.localPosition = Vector3.zero;
                var sl = new GameObject("Searchlight").AddComponent<Light>(); sl.transform.SetParent(j, false); sl.transform.localPosition = new Vector3(0f, -0.05f, 0.5f); sl.transform.localRotation = Quaternion.Euler(24f, 0f, 0f);
                sl.type = LightType.Spot; sl.spotAngle = 34f; sl.range = 16f; sl.intensity = 7f; sl.color = Color.white; sl.shadows = LightShadows.None;
                var ms = j.gameObject.AddComponent<Seven.MarsSentry>(); ms.Eye = gl.GetComponent<Renderer>(); ms.Search = sl; ms.Body = body;
                j.gameObject.SetActive(false); jammers.Add(j);
            }
            // QF18 (17): Viper's loudspeaker horn on a mast by the west control (his voice comes from it; its light pulses)
            var horn = new GameObject("ViperHorn").transform; horn.SetParent(world, false); horn.position = M.ViperHornAt;
            {
                var v = new List<Vector3>(); var t = new List<int>();
                Add(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.32f, 0f), new Vector2(0.26f, 0.2f), new Vector2(0.11f, 0.3f), new Vector2(0.09f, 4.4f), new Vector2(0.2f, 4.5f), new Vector2(0f, 4.55f) }, 0f, 360f, 6, null, Vector3.zero));
                var hornMesh = Lathe(new[] { new Vector2(0.08f, 0f), new Vector2(0.12f, 0.25f), new Vector2(0.24f, 0.55f), new Vector2(0.48f, 0.8f), new Vector2(0.44f, 0.82f), new Vector2(0f, 0.3f) }, 0f, 360f, 8, null, Vector3.zero);
                var hv = hornMesh.vertices; var toCtl = (M.WestControl - M.ViperHornAt); toCtl.y = 0f; var q = Quaternion.LookRotation(toCtl.normalized) * Quaternion.Euler(80f, 0f, 0f);
                for (int i = 0; i < hv.Length; i++) hv[i] = q * hv[i] + new Vector3(0f, 4.2f, 0f);
                int hbase = v.Count; v.AddRange(hv); t.AddRange(hornMesh.triangles.Select(i => i + hbase));
                MeshObject("ViperHornMast", SaveMesh(Finish(v, t), "Mars_ViperHorn"), m.metalDark, horn, false).transform.localPosition = Vector3.zero;
                var hl = PointLight("ViperHornLight", M.ViperHornAt + Vector3.up * 4.3f, 7f, 0f, horn); hl.shadows = LightShadows.None;
            }

            // ---------------------------------------------------------------- the temple (older than everything round it)
            var tem = new GameObject("Temple").transform; tem.SetParent(isl, false);
            {
                var v = new List<Vector3>(); var t = new List<int>();
                // QF17 (3): older than the fortifications, smoother - rounded, gently battered walls, a great rounded doorway,
                // a stepped rounded roof with a crown, columns with entasis
                RoundSlab(v, t, new Vector3(-10.5f, 6f, 180f), new Vector3(1.6f, 12f, 23.4f), 0f, 0.7f);
                RoundSlab(v, t, new Vector3(10.5f, 6f, 180f), new Vector3(1.6f, 12f, 23.4f), 0f, 0.7f);
                RoundSlab(v, t, new Vector3(0f, 6f, 191.6f), new Vector3(22.6f, 12f, 1.6f), 0f, 0.7f);
                RoundSlab(v, t, new Vector3(-6.6f, 6f, 168.6f), new Vector3(8.6f, 12f, 1.6f), 0f, 0.7f);
                RoundSlab(v, t, new Vector3(6.6f, 6f, 168.6f), new Vector3(8.6f, 12f, 1.6f), 0f, 0.7f);
                // the doorway: a round arch over the 4.8 m opening (voussoirs blended into the wall), the lintel wall above
                for (int i = 0; i <= 12; i++) { float a = Mathf.PI * i / 12f; RoundSlab(v, t, new Vector3(Mathf.Cos(a) * 2.75f, 7f + Mathf.Sin(a) * 2.5f, 168.6f), new Vector3(1.1f, 1.1f, 1.8f), 0f, 0.4f, 0f); }
                RoundSlab(v, t, new Vector3(0f, 10.8f, 168.6f), new Vector3(5.2f, 2.4f, 1.6f), 0f, 0.5f);
                MeshObject("TempleWalls", SaveMesh(SmoothFinish(v, t), "Mars_TempleWalls"), m.temple, tem);
                var rvv = new List<Vector3>(); var rtt = new List<int>();
                float ry = 12f;
                foreach (var (rw, rd, rh) in new[] { (24f, 26f, 1.4f), (20f, 22f, 1.3f), (15.5f, 17.5f, 1.2f), (11f, 13f, 1.1f), (6.5f, 8.5f, 1f) }) { RoundSlab(rvv, rtt, new Vector3(0f, ry + rh * 0.5f, 180f), new Vector3(rw, rh, rd), 0f, 1.6f, 0.06f); ry += rh; }
                { int rb = rvv.Count; var crown = Lathe(new[] { new Vector2(2.6f, 0f), new Vector2(2.2f, 1.2f), new Vector2(1.2f, 2.4f), new Vector2(0f, 2.9f) }, 0f, 360f, 24, null, new Vector3(0f, ry, 180f)); rvv.AddRange(crown.vertices); rtt.AddRange(crown.triangles.Select(i => i + rb)); }
                MeshObject("TempleRoof", SaveMesh(SmoothFinish(rvv, rtt), "Mars_TempleRoof"), m.temple, tem);
                MGround("TempleFloorDeck", new Vector3(-10f, -0.6f, 169f), new Vector3(10f, 0f, 191f), m.stoneDark, tem);
                MGround("TempleStepsDeck", new Vector3(-6f, -0.6f, 162f), new Vector3(6f, 0f, 169f), m.stone, tem);
                for (int i = 0; i < 3; i++) MBlock($"TempleStep{i}", new Vector3(-6f, 0f, 162f + i * 1.2f), new Vector3(6f, 0.06f + i * 0.02f, 163.2f + i * 1.2f), m.stone, tem, false);
                foreach (var x in new[] { -7f, 7f }) for (float z = 173f; z < 190f; z += 5.5f)
                {
                    var col = MeshObject($"TempleColumn{(int)x}_{(int)z}", SaveMesh(Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.85f, 0f), new Vector2(0.8f, 0.35f), new Vector2(0.6f, 0.55f), new Vector2(0.62f, 4f), new Vector2(0.58f, 9f), new Vector2(0.5f, 11f), new Vector2(0.78f, 11.5f), new Vector2(0.85f, 12f), new Vector2(0f, 12f) }, 0f, 360f, 18, null, new Vector3(x, 0f, z)), $"Mars_TempleColumn{(int)x}_{(int)z}"), m.stone, tem);
                    var ccol = col.AddComponent<CapsuleCollider>(); ccol.center = new Vector3(x, 6f, z); ccol.radius = 0.62f; ccol.height = 12f;
                }
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
            var disp = MBlock("TempleDisplay", M.DisplayAt + new Vector3(-0.05f, -1.1f, -1.6f), M.DisplayAt + new Vector3(0.05f, 1.1f, 1.6f), GlowMat("Mars_display"), tem, false).GetComponent<Renderer>();
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
                        if (chest != null) { var cp = MBlock("GuardChestPlate", new Vector3(-0.19f, -0.16f, 0.06f), new Vector3(0.19f, 0.18f, 0.12f), m.metalDark, chest, false); cp.transform.localPosition = Vector3.zero; cp.transform.localRotation = Quaternion.identity; }
                    }
                }
                else { go = new GameObject(name); MBlock("Placeholder", new Vector3(-0.3f, 0f, -0.25f), new Vector3(0.3f, 1.9f, 0.25f), m.cloth, go.transform, false); }
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
            d.Mechanism = mech; d.ChannelWA = chWA; d.ChannelWB = chWB; d.ChannelE = chE; d.ControlW = ctlW; d.ControlE = ctlE; d.RelayW = relay;
            d.WheelW = wheelW; d.WheelE = wheelE; d.MechGears = gears.ToArray(); d.RelayCrystal = relayT;
            d.GateLine = gateLine; d.GateFeed = feedLine; d.CableEndUpper = endUp; d.CableEndLower = endLo; d.Drawbridge = slab.GetComponent<Seven.MarsDrawbridge>(); d.ViperHorn = horn;
            d.Conduit = conduit; d.SwingSeg = swing; d.SpanW = spanW; d.SpanE = spanE; d.Jammers = jammers.ToArray();
            d.Pylons = pylons.ToArray(); d.PylonObjs = pylonObjs.ToArray(); d.BarrierWC = bWC; d.BarrierCE = bCE; d.BarrierCentre = bC; d.SlideBlock = slide; d.FallenWalk = fallen; d.Restraint = restraint; d.Dais = dais;
            d.SymbolW = symW; d.SymbolE = symE; d.Display = disp; d.Rune = rune; d.RecordTet = recordTet; d.DisplayLeads = leads;
            d.Vision4 = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Video/vision_04_mars.mp4");
            AddProbe(new Vector3(-110f, 6f, 0f), new Vector3(240f, 30f, 120f), Vector3.zero);
            AddProbe(new Vector3(0f, -8f, 80f), new Vector3(140f, 40f, 300f), Vector3.zero);
            AddProbe(new Vector3(0f, 6f, 180f), new Vector3(24f, 14f, 26f), Vector3.zero);
            QF18SceneChecks("Mars", true, () => slab.GetComponent<Seven.MarsDrawbridge>().Set(1f), () => slab.GetComponent<Seven.MarsDrawbridge>().Set(0f));   // QF18 (6, 11, 13)
            EditorSceneManager.SaveScene(scene, MarsScenePath);
            Log("scene saved " + MarsScenePath + $" ({folk.Count} townsfolk, Viper {(viperF != null ? "rigged" : "missing")}, Reptilians {(rept != null ? "rigged" : "placeholder")})");
        }

        /// <summary>QF17 (3): a Mars lamp - a turned lantern (glowing body under a small domed cap), never a slab.</summary>
        static void MarsLamp(string name, Vector3 at, float range, float intensity, Material glow, Transform parent, Vector3? size = null)
        {
            float r = size.HasValue ? Mathf.Max(size.Value.x, size.Value.z) * 0.5f : 0.25f;
            MeshObject(name + "_Glow", SaveMesh(Lathe(new[] { new Vector2(0f, -r), new Vector2(r * 0.6f, -r * 0.9f), new Vector2(r, -r * 0.2f), new Vector2(r * 0.85f, r * 0.6f), new Vector2(0f, r) }, 0f, 360f, 12, null, at), "Mars_" + name + "_Glow"), glow, parent, false);
            PointLight(name + "_Light", at + Vector3.down * 0.25f, range, intensity, parent);
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
