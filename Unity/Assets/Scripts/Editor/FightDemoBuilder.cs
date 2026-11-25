using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seven.EditorTools
{
    /// <summary>
    /// Builds every demo (Round 3: demo picker + saucer, see FightDemoBuilderSaucer.cs; Round 2: outdoor fog/glow world;
    /// Round 1 room) from the repo's art folders, start to finish, so it can run headless:
    ///   Unity.exe -batchmode -quit -projectPath Unity -executeMethod Seven.EditorTools.FightDemoBuilder.BuildAllFromCommandLine
    /// Steps: copy Mixamo FBX files into Assets -> Humanoid import settings -> grayscale texture + materials ->
    /// animator controllers -> FightDemo scene -> Windows player (Unity/Builds/Seven/SevenDemo.exe).
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const string CharRoot = "Assets/Art/Characters";
        const string SevenDir = CharRoot + "/7";
        const string BotDir = CharRoot + "/YBot";
        const string MatDir = "Assets/Art/Materials";
        const string AnimDir = "Assets/Art/Animators";
        const string ScenePath = "Assets/Scenes/FightDemo.unity";            // Round 2: outdoor fog/glow world
        const string RoomScenePath = "Assets/Scenes/FightDemo_Room.unity";   // Round 1 training room (kept; -room)
        const string ProfilePath = "Assets/Settings/FightDemoVolume.asset";

        static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        public static string ExePath => Path.Combine(ProjectRoot, "Builds", "Seven", "SevenDemo.exe");   // QF2
        /// <summary>Build order: the demo picker first.</summary>
        static string[] AllScenes => new[] { MenuScenePath, Chapter1ScenePath, ScenePath, RoomScenePath, SaucerScenePath, RatwingScenePath, VenusScenePath, EuropaScenePath, MarsScenePath, AsteroidScenePath, BiavehScenePath, CattalusScenePath, FinaleScenePath, GrandCircuitScenePath };

        static readonly string[] Loops = { "idle", "walk", "run", "fight_idle", "dazed", "sitting_idle", "sneak_walk", "float",
                                           "hang_idle", "shimmy", "shimmy_mirror", "crouch_idle", "fall_idle",     // Round 9 traversal loops
                                           "talking", "thinking", "sad_idle", "sit_idle", "sit_talking",               // QF7 emotion clips
                                           "blade_fight_idle", "blade_run" };                                       // Round 16 arc blades
        // Round 9: running_jump carries the root 4.3 m forward - extracted (and ignored: the arc is scripted), not baked.
        static readonly string[] RootMotion = { "finisher", "dodge", "knocked_down", "get_up", "running_jump" };

        static void Log(string s) => Debug.Log("[FightDemoBuilder] " + s);

        public static void BuildAllFromCommandLine()
        {
            int code = 1;
            try
            {
                BuildAll();
                try { QF24GlobalChecks(); } catch (Exception e) { BuildFailures.Add("QF24 global checks threw: " + e.Message); }
                try { QF30GlobalChecks(); } catch (Exception e) { BuildFailures.Add("QF30 global checks threw: " + e.Message); }
                try { QF25GlobalChecks(); } catch (Exception e) { BuildFailures.Add("QF25 global checks threw: " + e.Message); }
                code = BuildPlayer() ? 0 : 1;
                // QF18: a scene that fails a build check (holes, z-fighting, NPC placement) fails the build (the player is still written)
                if (BuildFailures.Count > 0) { Debug.LogError("[FightDemoBuilder] QF18 BUILD CHECKS FAILED: " + string.Join(" | ", BuildFailures)); if (code == 0) code = 2; }
                else Log("QF18 build checks: all passed");
            }
            catch (Exception e)
            {
                Debug.LogError("[FightDemoBuilder] FAILED: " + e);
            }
            EditorApplication.Exit(code);
        }

        [MenuItem("Seven/Build Fight Demo (assets + scene)")]
        public static void BuildAll()
        {
            _saucerPrefab = null; SaucerChecks.Clear();      // QF13 (16): the one saucer prefab is rebuilt once per build
            CopyArt();
            var sevenAvatar = SetupCharacter(SevenDir + "/7_character.fbx");
            var botAvatar = SetupCharacter(BotDir + "/ybot_character.fbx");
            SetupClips(SevenDir, "7_", sevenAvatar);
            SetupClips(BotDir, "ybot_", botAvatar);

            var mats = BuildMaterials();
            Remap(SevenDir + "/7_character.fbx", ("7_body", mats.body), ("7_hairtie_metal", mats.metal), ("7_forehead_glow", mats.glow));
            Remap(BotDir + "/ybot_character.fbx", ("Alpha_Body_MAT", mats.botBody), ("Alpha_Joints_MAT", mats.botJoints));

            var sevenCtl = BuildController(SevenDir, "7_", AnimDir + "/Seven.controller", "fight_idle");
            var botCtl = BuildController(BotDir, "ybot_", AnimDir + "/TrainingBot.controller", "fight_idle");
            var atmo = BuildAtmosphere();
            var bank = BuildVoiceBank();               // Round 6 (before the menu + saucer scenes, which reference it)
            BuildAudioBanks();                         // Round 7: Resources/Audio/SfxBank + MusicBank (by name, missing = silence)
            BuildRoomScene(sevenCtl, botCtl, mats);
            BuildOutdoorScene(sevenCtl, botCtl, mats, atmo);
            BuildRound3(sevenCtl, atmo);
            BuildRound4(atmo);
            BuildRound6(sevenCtl, atmo, bank);
            BuildRound11(sevenCtl, atmo, bank);        // Round 11: Chapter 2 "Venus", part 1
            BuildRound14(sevenCtl, atmo, bank);        // Round 14: Chapter "Europa — Beneath the Ice"
            BuildRound15(sevenCtl, atmo, bank);        // Round 15: Chapter "Mars — The Divided Kingdoms"
            BuildRound16(sevenCtl, atmo, bank);        // Round 16: Chapter "The Asteroid — the Great Machine Age"
            BuildRound17(sevenCtl, atmo, bank);        // Round 17: Chapter "The Biavian Mothership (Veyra)"
            BuildRound18(sevenCtl, atmo, bank);        // Round 18: Chapter "Cattalus - Panther's homeworld"
            BuildRound19(sevenCtl, atmo, bank);        // Round 19: "The Moon Finale" (the last chapter)
            BuildRound20(sevenCtl);                    // Round 20: the Grand Circuit racing mode (unlocked after the credits)
            BuildWorldViews();                         // QF18 (2, 3): the window views + the holograms, from the real levels
            EditorBuildSettings.scenes = AllScenes.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            Log("BuildAll done");
        }

        // ---------------------------------------------------------------------------------------- art
        static void CopyArt()
        {
            void CopyDir(string src, string dst, string pattern)
            {
                Directory.CreateDirectory(Path.Combine(ProjectRoot, dst));
                foreach (var f in Directory.GetFiles(src, pattern))
                {
                    var to = Path.Combine(ProjectRoot, dst, Path.GetFileName(f));
                    if (!File.Exists(to) || new FileInfo(to).Length != new FileInfo(f).Length || File.GetLastWriteTimeUtc(f) > File.GetLastWriteTimeUtc(to))
                        File.Copy(f, to, true);
                }
            }
            CopyDir(Path.Combine(RepoRoot, "Art", "Mixamo", "7"), SevenDir, "7_*.fbx");
            CopyDir(Path.Combine(RepoRoot, "Art", "Mixamo", "7", "emotion"), SevenDir, "7_*.fbx");   // QF7: Dan's body-language clips
            CopyDir(Path.Combine(RepoRoot, "Art", "Mixamo", "7", "blade"), SevenDir, "7_*.fbx");     // Round 16: the arc blades (single-sword clips)
            // Round 3 sit clips: Dan's Mixamo downloads keep Mixamo's names; copy them under the 7_ state names.
            foreach (var (from, to) in new[] { ("Stand To Sit.fbx", "7_stand_to_sit.fbx"), ("Seated Idle.fbx", "7_sitting_idle.fbx"),
                                               ("Sitting Idle.fbx", "7_sitting_idle.fbx"), ("Sit To Stand.fbx", "7_sit_to_stand.fbx") })
            {
                var f = Path.Combine(RepoRoot, "Art", "Mixamo", "7", from);
                var dst = Path.Combine(ProjectRoot, SevenDir, to);
                if (File.Exists(f) && (!File.Exists(dst) || File.GetLastWriteTimeUtc(f) > File.GetLastWriteTimeUtc(dst) || new FileInfo(f).Length != new FileInfo(dst).Length))
                    File.Copy(f, dst, true);
            }
            CopyDir(Path.Combine(RepoRoot, "Art", "Mixamo", "YBot"), BotDir, "*.fbx");
            CopyDir(Path.Combine(RepoRoot, "Art", "Video"), "Assets/Video", "*.mp4");     // Round 9: the first vision
            // Round 9: Dan's alternates used as their own states, and the one-direction shimmy mirrored for the other side.
            foreach (var (from, to) in new[] { (Path.Combine("alt", "7_jumping_up.fbx"), "7_jumping_up.fbx"), ("7_shimmy.fbx", "7_shimmy_mirror.fbx"),
                                               // Round 16: the second blade rides the off hand - the mirrored slashes alternate with the originals
                                               (Path.Combine("blade", "7_blade_slash_in.fbx"), "7_blade_slash_in_mirror.fbx"), (Path.Combine("blade", "7_blade_slash_out.fbx"), "7_blade_slash_out_mirror.fbx") })
            {
                var f = Path.Combine(RepoRoot, "Art", "Mixamo", "7", from);
                var dst = Path.Combine(ProjectRoot, SevenDir, to);
                if (File.Exists(f) && (!File.Exists(dst) || File.GetLastWriteTimeUtc(f) > File.GetLastWriteTimeUtc(dst) || new FileInfo(f).Length != new FileInfo(dst).Length))
                    File.Copy(f, dst, true);
            }

            // Pre-Mixamo model kept as a fallback asset (not used by the scene).
            Directory.CreateDirectory(Path.Combine(ProjectRoot, SevenDir, "Fallback"));
            File.Copy(Path.Combine(RepoRoot, "Art", "Export", "7_v2_mixamo.fbx"),
                Path.Combine(ProjectRoot, SevenDir, "Fallback", "7_v2_premixamo.fbx"), true);

            // Grayscale copy of the base-colour texture.
            var texOut = Path.Combine(ProjectRoot, SevenDir, "Textures", "7_v2_basecolor_gray.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(texOut));
            var src = Path.Combine(RepoRoot, "Art", "Export", "Textures", "7_v2_basecolor.jpg");
            if (!File.Exists(texOut) || File.GetLastWriteTimeUtc(src) > File.GetLastWriteTimeUtc(texOut))
            {
                var t = new Texture2D(2, 2, TextureFormat.RGB24, false);
                t.LoadImage(File.ReadAllBytes(src));
                var px = t.GetPixels32();
                for (int i = 0; i < px.Length; i++)
                {
                    byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(0.2126f * px[i].r + 0.7152f * px[i].g + 0.0722f * px[i].b), 0, 255);
                    px[i] = new Color32(g, g, g, 255);
                }
                t.SetPixels32(px);
                t.Apply();
                File.WriteAllBytes(texOut, t.EncodeToJPG(95));
                UnityEngine.Object.DestroyImmediate(t);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Log("art copied");
        }

        static Avatar SetupCharacter(string path)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            imp.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            Log($"{path}: avatar {(avatar != null ? avatar.name : "MISSING")} human={avatar?.isHuman} valid={avatar?.isValid}");
            if (avatar == null || !avatar.isHuman) throw new Exception("No humanoid avatar for " + path);
            return avatar;
        }

        static string StateName(string file, string prefix) => Path.GetFileNameWithoutExtension(file).Substring(prefix.Length);

        static void SetupClips(string dir, string prefix, Avatar avatar)
        {
            foreach (var path in Directory.GetFiles(Path.Combine(ProjectRoot, dir), prefix + "*.fbx").Select(p => dir + "/" + Path.GetFileName(p)))
            {
                if (path.EndsWith("_character.fbx")) continue;
                var state = StateName(path, prefix);
                var imp = (ModelImporter)AssetImporter.GetAtPath(path);
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                imp.sourceAvatar = avatar;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.importAnimation = true;
                imp.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
                var clips = imp.defaultClipAnimations;
                if (clips.Length == 0) { imp.SaveAndReimport(); clips = imp.defaultClipAnimations; }
                bool root = RootMotion.Contains(state);
                foreach (var c in clips)
                {
                    c.name = state;
                    c.loopTime = Loops.Contains(state);
                    c.lockRootRotation = true; c.keepOriginalOrientation = true;
                    c.lockRootHeightY = true; c.keepOriginalPositionY = true;
                    c.lockRootPositionXZ = !root; c.keepOriginalPositionXZ = true;
                    c.mirror = state.EndsWith("_mirror");      // Round 9: shimmy_mirror = shimmy to the other side
                }
                imp.clipAnimations = clips;
                imp.SaveAndReimport();
                var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                Log($"{path}: clip {clip?.name} {clip?.length:0.00}s loop={Loops.Contains(state)} rootMotion={root} humanMotion={clip?.humanMotion}");
            }
        }

        static void Remap(string path, params (string name, Material mat)[] maps)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            foreach (var (name, mat) in maps)
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), mat);
            imp.SaveAndReimport();
        }

        // ----------------------------------------------------------------------------------- materials
        struct Mats { public Material body, metal, glow, botBody, botJoints, floor, wall, strip, pillar, ground, grass, rock, structure, worldGlow, halo, additive; }

        static Material Lit(string name, Color baseColor, float smoothness, float metallic = 0f)
        {
            Directory.CreateDirectory(Path.Combine(ProjectRoot, MatDir));
            var path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = Shader.Find("Universal Render Pipeline/Lit");
            m.SetColor("_BaseColor", baseColor);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Additive(string name)
        {
            var path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Seven/Additive")); AssetDatabase.CreateAsset(m, path); }
            m.shader = Shader.Find("Seven/Additive");
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Emissive(Material m, float intensity)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.white * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
        }

        static Mats BuildMaterials()
        {
            var texPath = SevenDir + "/Textures/7_v2_basecolor_gray.jpg";
            var ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
            ti.maxTextureSize = 4096;
            ti.sRGBTexture = true;
            ti.SaveAndReimport();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            var mats = new Mats
            {
                body = Lit("7_body", Color.white, 0.3f),
                metal = Lit("7_hairtie_metal", new Color(0.92f, 0.92f, 0.92f), 0.86f, 0.7f),   // polished silver; full metal reads black in a dim room
                glow = Lit("7_forehead_glow", Color.white, 0.5f),
                botBody = Lit("YBot_body", new Color(0.72f, 0.72f, 0.72f), 0.2f),
                botJoints = Lit("YBot_joints", new Color(0.32f, 0.32f, 0.32f), 0.3f),
                floor = Lit("Arena_floor", new Color(0.46f, 0.46f, 0.46f), 0.4f),
                wall = Lit("Arena_wall", new Color(0.66f, 0.66f, 0.66f), 0.1f),
                strip = Lit("Arena_lightstrip", Color.white, 0.5f),
                pillar = Lit("Arena_pillar", new Color(0.30f, 0.30f, 0.30f), 0.3f),
                ground = Lit("World_ground", new Color(0.035f, 0.035f, 0.035f), 0.12f),
                grass = Lit("World_grass", new Color(0.03f, 0.03f, 0.03f), 0.15f),
                rock = Lit("World_rock", new Color(0.2f, 0.2f, 0.2f), 0.2f),
                structure = Lit("World_structure", new Color(0.62f, 0.62f, 0.62f), 0.25f),
                worldGlow = Lit("World_glowstrip", Color.white, 0.5f),
                halo = Lit("World_overhead", new Color(0.1f, 0.1f, 0.1f), 0.1f),
                additive = Additive("Fx_additive"),
            };
            foreach (var m in new[] { mats.grass, mats.structure, mats.worldGlow, mats.halo }) { m.SetFloat("_Cull", 0f); m.doubleSidedGI = true; }
            Emissive(mats.worldGlow, 3.2f);
            mats.body.SetTexture("_BaseMap", tex);
            mats.body.SetTexture("_MainTex", tex);
            Emissive(mats.glow, 4f);    // HDR white -> soft white bloom
            Emissive(mats.strip, 2.2f);
            AssetDatabase.SaveAssets();
            return mats;
        }

        // ---------------------------------------------------------------------------------- animators
        static AnimatorController BuildController(string dir, string prefix, string path, string defaultState)
        {
            Directory.CreateDirectory(Path.Combine(ProjectRoot, AnimDir));
            AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            var sm = ac.layers[0].stateMachine;
            foreach (var f in Directory.GetFiles(Path.Combine(ProjectRoot, dir), prefix + "*.fbx"))
            {
                var p = dir + "/" + Path.GetFileName(f);
                if (p.EndsWith("_character.fbx")) continue;
                var clip = AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
                var st = sm.AddState(clip.name);
                st.motion = clip;
                st.writeDefaultValues = true;
                if (clip.name == defaultState) sm.defaultState = st;
            }
            // Round 9: hand IK (7's hands on the ledge lip while hanging) needs the IK pass on the base layer.
            var layers = ac.layers; layers[0].iKPass = true; ac.layers = layers;
            EditorUtility.SetDirty(ac);
            Log($"{path}: {sm.states.Length} states");
            return ac;
        }

        // ------------------------------------------------------------------------------------- scene
        static GameObject Box(string name, Vector3 pos, Vector3 size, Material m, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }

        static VolumeProfile BuildProfile()
        {
            AssetDatabase.DeleteAsset(ProfilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 1.0f; bloom.intensity.value = 0.9f; bloom.scatter.value = 0.6f; bloom.tint.value = Color.white;
            var ca = profile.Add<ColorAdjustments>(true);
            ca.saturation.value = -100f;        // strict monochrome safety net
            ca.contrast.value = 8f; ca.postExposure.value = 0f; ca.colorFilter.value = Color.white; ca.hueShift.value = 0f;
            var tm = profile.Add<Tonemapping>(true);
            tm.mode.value = TonemappingMode.Neutral;
            var vg = profile.Add<Vignette>(true);
            vg.intensity.value = 0.28f; vg.smoothness.value = 0.45f; vg.color.value = Color.black; vg.rounded.value = false;
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        static GameObject Spawn(string fbx, string name, Vector3 pos, Vector3 facing, RuntimeAnimatorController ctl)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(facing));
            var anim = go.GetComponent<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();
            if (anim.avatar == null) anim.avatar = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Avatar>().First();
            anim.runtimeAnimatorController = ctl;
            anim.applyRootMotion = true;
            var b = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.updateWhenOffscreen = true;
                Log($"{name}: renderer {smr.name} materials [{string.Join(", ", smr.sharedMaterials.Select(m => m ? m.name : "null"))}]");
            }
            Log($"{name}: bind-pose bounds min {b.min} max {b.max} (height {b.size.y:0.00} m)");
            return go;
        }

        // ------------------------------------------------------------------------------ shared fight rig
        /// <summary>7, the bot, the orbit camera, input, telekinesis effects, legend and director.</summary>
        static void SetupFight(AnimatorController sevenCtl, AnimatorController botCtl, Vector3 sevenPos, Vector3 botPos,
            Color clear, float farClip, Material additive)
        {
            var seven = Spawn(SevenDir + "/7_character.fbx", "7", sevenPos, botPos - sevenPos, sevenCtl);
            var bot = Spawn(BotDir + "/ybot_character.fbx", "TrainingBot", botPos, sevenPos - botPos, botCtl);

            var camGo = new GameObject("FightCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = clear;
            cam.fieldOfView = 38f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = farClip;
            camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.requiresDepthTexture = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var fcam = camGo.AddComponent<Seven.FightCamera>();
            fcam.A = seven.transform; fcam.B = bot.transform;
            var mid = (sevenPos + botPos) * 0.5f;
            camGo.transform.rotation = Quaternion.Euler(fcam.DefaultPitch, fcam.ThreeQuarterYaw, 0f);
            camGo.transform.position = mid + Vector3.up * 0.95f - camGo.transform.forward * (fcam.MinDistance + 2.8f * fcam.PerMetre);

            var dirGo = new GameObject("FightDirector");
            var input = dirGo.AddComponent<Seven.FightInput>();
            fcam.Input = input;
            var sf = seven.AddComponent<Seven.SevenFighter>();
            var tb = bot.AddComponent<Seven.TrainingBot>();
            sf.Bot = tb; sf.CameraTransform = camGo.transform; sf.Input = input;
            tb.Seven = sf;

            var fxGo = new GameObject("TelekinesisFX");
            var fx = fxGo.AddComponent<Seven.TelekinesisFX>();
            fx.Seven = sf; fx.Bot = tb; fx.Additive = additive;

            var legendGo = new GameObject("ControlsLegend");
            var legend = legendGo.AddComponent<Seven.ControlsLegend>();
            legend.Cam = cam;

            var director = dirGo.AddComponent<Seven.FightDirector>();
            director.Seven = sf; director.Bot = tb; director.Input = input; director.Cam = fcam;
            director.Fx = fx; director.Legend = legend;
        }

        static void AddGround(Seven.Ground.Shape shape)
        {
            var g = new GameObject("WorldGround").AddComponent<Seven.WorldGround>();
            g.Shape = shape;
        }

        // --------------------------------------------------------------------------- room (Round 1)
        static void BuildRoomScene(AnimatorController sevenCtl, AnimatorController botCtl, Mats mats)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Environment: gray, fogged, no skybox, flat ambient (set as SH so it works without baking).
            var fog = new Color(0.34f, 0.34f, 0.34f);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(RenderSettings.ambientLight);
            RenderSettings.ambientProbe = sh;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.035f;
            RenderSettings.fogColor = fog;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            AddGround(Seven.Ground.Shape.Flat);

            var room = new GameObject("TrainingRoom").transform;
            Box("Floor", new Vector3(0, -0.1f, 0), new Vector3(20, 0.2f, 16), mats.floor, room);
            Box("FloorRing", new Vector3(0, 0.002f, 0), new Vector3(7f, 0.004f, 4.4f), mats.pillar, room);
            Box("BackWall", new Vector3(0, 3.5f, 7.5f), new Vector3(20, 7, 0.3f), mats.wall, room);
            Box("LeftWall", new Vector3(-9.5f, 3.5f, 0), new Vector3(0.3f, 7, 16), mats.wall, room);
            Box("RightWall", new Vector3(9.5f, 3.5f, 0), new Vector3(0.3f, 7, 16), mats.wall, room);
            Box("Ceiling", new Vector3(0, 7.1f, 0), new Vector3(20, 0.2f, 16), mats.pillar, room);
            for (int i = -2; i <= 2; i++)
            {
                Box("Strip" + i, new Vector3(i * 3.6f, 3.4f, 7.33f), new Vector3(0.12f, 4.5f, 0.04f), mats.strip, room);
                Box("CeilingStrip" + i, new Vector3(i * 3.6f, 6.98f, 0), new Vector3(0.18f, 0.04f, 12f), mats.strip, room);
            }
            foreach (var x in new[] { -6.5f, 6.5f })
                foreach (var z in new[] { -3.5f, 4.5f })
                    Box("Pillar", new Vector3(x, 3.5f, z), new Vector3(0.7f, 7, 0.7f), mats.pillar, room);

            AddProbe(new Vector3(0, 1.6f, 0), new Vector3(19, 7.2f, 15.5f), new Vector3(0, 1.9f, 0));

            var lightGo = new GameObject("KeyLight");
            lightGo.transform.rotation = Quaternion.Euler(48f, 30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 2.1f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;

            var volGo = new GameObject("PostProcess");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = BuildProfile();

            SetupFight(sevenCtl, botCtl, new Vector3(-1.4f, 0, 0), new Vector3(1.4f, 0, 0), fog, 60f, mats.additive);

            Directory.CreateDirectory(Path.Combine(ProjectRoot, "Assets", "Scenes"));
            EditorSceneManager.SaveScene(scene, RoomScenePath);
            Log("scene saved " + RoomScenePath);
        }

        static void AddProbe(Vector3 pos, Vector3 size, Vector3 center)
        {
            var probeGo = new GameObject("ReflectionProbe");
            probeGo.transform.position = pos;
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.size = size;
            probe.center = center;
            probe.resolution = 128;
            probe.hdr = true;
            probe.cullingMask = ~(1 << 20);      // Round 7: never reflect the city (layer 20) into interiors
        }

        // ------------------------------------------------------------------------------- Atmosphere
        const string AtmoDir = "Assets/Atmosphere";
        const string AtmoPrefabPath = AtmoDir + "/Atmosphere.prefab";
        const string AtmoProfilePath = AtmoDir + "/Atmosphere_Volume.asset";
        const string FogMatPath = AtmoDir + "/SevenHeightFog.mat";
        const string NoisePath = AtmoDir + "/FogNoise.png";

        /// <summary>Reusable world look: fog material + noise, renderer feature, volume profile, prefab.</summary>
        static GameObject BuildAtmosphere()
        {
            Directory.CreateDirectory(Path.Combine(ProjectRoot, AtmoDir));

            // Tileable fBm value noise (grayscale) for drifting fog variation.
            if (!File.Exists(Path.Combine(ProjectRoot, NoisePath)))
            {
                const int n = 256;
                var rnd = new System.Random(7);
                var tex = new Texture2D(n, n, TextureFormat.RGB24, false);
                var px = new Color32[n * n];
                var grids = new float[5][,];
                for (int o = 0; o < 5; o++)
                {
                    int cells = 4 << o;
                    grids[o] = new float[cells, cells];
                    for (int y = 0; y < cells; y++) for (int x = 0; x < cells; x++) grids[o][x, y] = (float)rnd.NextDouble();
                }
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float v = 0, amp = 0.5f, sum = 0;
                        for (int o = 0; o < 5; o++)
                        {
                            int cells = 4 << o;
                            float fx = (float)x / n * cells, fy = (float)y / n * cells;
                            int x0 = (int)fx, y0 = (int)fy;
                            float tx = fx - x0, ty = fy - y0;
                            tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
                            var g = grids[o];
                            float a = Mathf.Lerp(g[x0 % cells, y0 % cells], g[(x0 + 1) % cells, y0 % cells], tx);
                            float b = Mathf.Lerp(g[x0 % cells, (y0 + 1) % cells], g[(x0 + 1) % cells, (y0 + 1) % cells], tx);
                            v += Mathf.Lerp(a, b, ty) * amp; sum += amp; amp *= 0.5f;
                        }
                        byte c = (byte)Mathf.Clamp(Mathf.RoundToInt(v / sum * 255f), 0, 255);
                        px[y * n + x] = new Color32(c, c, c, 255);
                    }
                tex.SetPixels32(px); tex.Apply();
                File.WriteAllBytes(Path.Combine(ProjectRoot, NoisePath), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(NoisePath, ImportAssetOptions.ForceSynchronousImport);
                var ti = (TextureImporter)AssetImporter.GetAtPath(NoisePath);
                ti.wrapMode = TextureWrapMode.Repeat; ti.sRGBTexture = false; ti.mipmapEnabled = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
            }
            var noise = AssetDatabase.LoadAssetAtPath<Texture2D>(NoisePath);

            // Full-screen fog material + renderer feature on every URP renderer (pass-through where no Atmosphere).
            var fogMat = AssetDatabase.LoadAssetAtPath<Material>(FogMatPath);
            if (fogMat == null) { fogMat = new Material(Shader.Find("Seven/HeightFog")); AssetDatabase.CreateAsset(fogMat, FogMatPath); }
            fogMat.shader = Shader.Find("Seven/HeightFog");
            EditorUtility.SetDirty(fogMat);
            foreach (var rdPath in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" })
                EnsureFogFeature(rdPath, fogMat);

            // Volume profile: heavy bloom, low contrast, strict monochrome.
            AssetDatabase.DeleteAsset(AtmoProfilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, AtmoProfilePath);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 0.85f; bloom.intensity.value = 1.5f; bloom.scatter.value = 0.78f; bloom.tint.value = Color.white;
            bloom.highQualityFiltering.value = false;
            var ca = profile.Add<ColorAdjustments>(true);
            ca.saturation.value = -100f;   // strict monochrome safety net
            ca.contrast.value = -8f; ca.postExposure.value = 0f; ca.colorFilter.value = Color.white; ca.hueShift.value = 0f;
            var tm = profile.Add<Tonemapping>(true);
            tm.mode.value = TonemappingMode.Neutral;
            var vg = profile.Add<Vignette>(true);
            vg.intensity.value = 0.22f; vg.smoothness.value = 0.5f; vg.color.value = Color.black; vg.rounded.value = false;
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);

            // Prefab: Atmosphere component + global Volume + back light + soft front fill + default glow sources.
            var root = new GameObject("Atmosphere");
            var atmo = root.AddComponent<Seven.Atmosphere>();
            atmo.NoiseTexture = noise;
            var vol = root.AddComponent<Volume>();
            vol.isGlobal = true; vol.sharedProfile = profile;

            var back = new GameObject("BackLight");
            back.transform.SetParent(root.transform, false);
            back.transform.rotation = Quaternion.Euler(22f, 200f, 0f);      // from the bright distance toward the viewer
            var bl = back.AddComponent<Light>();
            bl.type = LightType.Directional; bl.color = Color.white; bl.intensity = 1.25f;
            bl.shadows = LightShadows.Soft; bl.shadowStrength = 0.6f;

            var fill = new GameObject("FrontFill");
            fill.transform.SetParent(root.transform, false);
            fill.transform.rotation = Quaternion.Euler(35f, 25f, 0f);
            var fl = fill.AddComponent<Light>();
            fl.type = LightType.Directional; fl.color = Color.white; fl.intensity = 0.3f; fl.shadows = LightShadows.None;

            // Glow sources (scale.x = radius multiplier, scale.y = intensity multiplier). Move these to the bright
            // spots of each world; the last one is a broad bright patch in the distance (like the right of Kyle's art).
            var glows = new[]
            {
                (new Vector3(62, 2, 64), new Vector3(0.8f, 1, 1)), (new Vector3(82, 4, 108), new Vector3(1.1f, 1, 1)),
                (new Vector3(-120, 0, 180), new Vector3(1, 0.8f, 1)), (new Vector3(30, 0, 300), new Vector3(1.5f, 1, 1)),
                (new Vector3(-30, 0, -200), new Vector3(1, 0.8f, 1)), (new Vector3(180, 0, -60), new Vector3(1, 0.8f, 1)),
                (new Vector3(280, 25, 280), new Vector3(6, 1.8f, 1)),
            };
            for (int i = 0; i < glows.Length; i++)
            {
                var g = new GameObject("Glow" + i);
                g.transform.SetParent(root.transform, false);
                g.transform.position = glows[i].Item1;
                g.transform.localScale = glows[i].Item2;
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, AtmoPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Log("atmosphere prefab saved " + AtmoPrefabPath);
            return prefab;
        }

        static void EnsureFogFeature(string rendererPath, Material fogMat)
        {
            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rd == null) { Log("no renderer at " + rendererPath); return; }
            var feature = rd.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault(f => f.name == "SevenHeightFog");
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                feature.name = "SevenHeightFog";
                AssetDatabase.AddObjectToAsset(feature, rd);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);
                var so = new SerializedObject(rd);
                var list = so.FindProperty("m_RendererFeatures");
                var map = so.FindProperty("m_RendererFeatureMap");
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingTransparents;
            feature.fetchColorBuffer = true;
            feature.requirements = ScriptableRenderPassInput.Depth;
            feature.passMaterial = fogMat;
            feature.passIndex = 0;
            feature.SetActive(true);
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(rd);
            Log($"{rendererPath}: SevenHeightFog feature ready ({rd.rendererFeatures.Count} features)");
        }

        // --------------------------------------------------------------------------- outdoor world
        const string GenDir = "Assets/Art/World/Generated";
        static readonly Vector3 ViewOrigin = new Vector3(-1.8f, 1.6f, -6.2f);   // default camera spot on the hill

        // QF12 (6, 2): a mesh name saved twice in one build deleted the first asset - every object already holding it lost
        // its mesh (the original Venus cube vanished when the small record copy was built with the same names). Each name
        // is now used once per build; a repeat gets _2, _3 ... and is logged.
        static readonly System.Collections.Generic.Dictionary<string, int> _meshNames = new System.Collections.Generic.Dictionary<string, int>();
        public static int MeshNameRepeats;
        static Mesh SaveMesh(Mesh m, string name)
        {
            if (_meshNames.TryGetValue(name, out int used)) { _meshNames[name] = used + 1; var n2 = name + "_" + (used + 1); MeshNameRepeats++; Log($"QF12 mesh name reused in this build: '{name}' -> '{n2}' (the first one keeps its mesh)"); name = n2; }
            else _meshNames[name] = 1;
            Directory.CreateDirectory(Path.Combine(ProjectRoot, GenDir));
            var path = GenDir + "/" + name + ".asset";
            AssetDatabase.DeleteAsset(path);
            m.name = name;
            UnshareZeroNormals(m);                       // QF26 (1)
            m.RecalculateBounds();
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        public static int ZeroNormalMeshesFixed;
        /// <summary>QF26 (1): a mesh whose two windings share vertices (a two-sided sheet built as front + back triangles on the same
        /// points) gets zero normals from RecalculateNormals; lit, normalize(0) = NaN -> black specks + a bloom white-out. Any mesh
        /// with a zero normal on a real triangle is rebuilt with every triangle on its own vertices (flat normals, both sides lit).</summary>
        static void UnshareZeroNormals(Mesh m)
        {
            if (m == null || m.subMeshCount != 1 || m.GetTopology(0) != MeshTopology.Triangles) return;
            var n = m.normals; var v = m.vertices; if (n == null || n.Length != v.Length || v.Length == 0) return;
            var t = m.triangles; bool bad = false;
            for (int i = 0; i + 2 < t.Length && !bad; i += 3)
            {
                if (Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).sqrMagnitude < 1e-14f) continue;
                for (int k = 0; k < 3; k++) { float l = n[t[i + k]].sqrMagnitude; if (float.IsNaN(l) || l < 0.25f) { bad = true; break; } }
            }
            if (!bad) return;
            var uv = m.uv; bool hasUv = uv != null && uv.Length == v.Length;
            var nv = new List<Vector3>(t.Length); var nn = new List<Vector3>(t.Length); var nu = new List<Vector2>(t.Length); var nt = new int[t.Length];
            for (int i = 0; i + 2 < t.Length; i += 3)
            {
                var a = v[t[i]]; var b = v[t[i + 1]]; var c = v[t[i + 2]];
                var fn = Vector3.Cross(b - a, c - a); fn = fn.sqrMagnitude > 1e-20f ? fn.normalized : Vector3.up;
                for (int k = 0; k < 3; k++) { nt[i + k] = nv.Count; nv.Add(v[t[i + k]]); nn.Add(fn); if (hasUv) nu.Add(uv[t[i + k]]); }
            }
            m.Clear(); m.indexFormat = nv.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            m.SetVertices(nv); m.SetNormals(nn); if (hasUv) m.SetUVs(0, nu); m.SetTriangles(nt, 0);
            ZeroNormalMeshesFixed++;
            Log($"QF26 zero normals: '{m.name}' had faces sharing vertices with their back faces - split onto their own vertices ({nv.Count} vertices)");
        }

        static GameObject MeshObject(string name, Mesh mesh, Material mat, Transform parent, bool shadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        /// <summary>Radial terrain mesh from WorldGround.HillHeight: fine near the edge, coarse far out.</summary>
        static Mesh HillMesh()
        {
            var c = Seven.WorldGround.PlateauCenter;
            var radii = new List<float> { 0f };
            float r = 0.6f;
            while (r < 140f) { radii.Add(r); r += r < 8f ? 0.8f : r < 22f ? 0.5f : r < 40f ? 1.5f : 6f; }
            const int seg = 192;
            var v = new List<Vector3>(); var t = new List<int>();
            v.Add(new Vector3(c.x, Seven.WorldGround.HillHeight(c.x, c.z), c.z));
            for (int i = 1; i < radii.Count; i++)
                for (int s = 0; s < seg; s++)
                {
                    float a = s * Mathf.PI * 2f / seg;
                    float x = c.x + Mathf.Cos(a) * radii[i], z = c.z + Mathf.Sin(a) * radii[i];
                    v.Add(new Vector3(x, Seven.WorldGround.HillHeight(x, z), z));
                }
            for (int s = 0; s < seg; s++) { t.Add(0); t.Add(1 + (s + 1) % seg); t.Add(1 + s); }
            for (int i = 1; i < radii.Count - 1; i++)
                for (int s = 0; s < seg; s++)
                {
                    int a0 = 1 + (i - 1) * seg + s, a1 = 1 + (i - 1) * seg + (s + 1) % seg;
                    int b0 = a0 + seg, b1 = a1 + seg;
                    t.Add(a0); t.Add(a1); t.Add(b0);
                    t.Add(a1); t.Add(b1); t.Add(b0);
                }
            var m = new Mesh { indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v); m.SetTriangles(t, 0);
            m.RecalculateNormals();
            return m;
        }

        /// <summary>Short dark grass: one thin triangle per blade, denser and a bit taller toward the hill edge.</summary>
        static Mesh GrassMesh(int count, float maxR)
        {
            var rnd = new System.Random(77);
            float R() => (float)rnd.NextDouble();
            var c = Seven.WorldGround.PlateauCenter;
            var v = new List<Vector3>(count * 3); var n = new List<Vector3>(count * 3); var t = new List<int>(count * 3);
            for (int i = 0; i < count; i++)
            {
                float rr = Mathf.Sqrt(R()) * maxR, a = R() * Mathf.PI * 2f;
                float x = c.x + Mathf.Cos(a) * rr, z = c.z + Mathf.Sin(a) * rr;
                float y = Seven.WorldGround.HillHeight(x, z);
                float edge = Mathf.InverseLerp(Seven.WorldGround.PlateauRadius - 2f, Seven.WorldGround.PlateauRadius + 3f, rr);
                float h = Mathf.Lerp(0.04f, 0.10f, R()) * (1f + edge * 1.8f);
                float w = Mathf.Lerp(0.006f, 0.013f, R());
                float yaw = R() * Mathf.PI * 2f;
                var side = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw)) * w;
                var lean = new Vector3(R() - 0.5f, 0f, R() - 0.5f) * h * 0.7f;
                var p = new Vector3(x, y - 0.01f, z);
                int b = v.Count;
                v.Add(p - side); v.Add(p + side); v.Add(p + lean + Vector3.up * h);
                var up = (Vector3.up + new Vector3(R() - 0.5f, 0, R() - 0.5f) * 0.4f).normalized;
                n.Add(up); n.Add(up); n.Add(up);
                t.Add(b); t.Add(b + 2); t.Add(b + 1);
            }
            var m = new Mesh { indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0);
            return m;
        }

        static Mesh RockMesh(int seed)
        {
            var rnd = new System.Random(seed);
            // Low-poly jittered octahedron subdivided once.
            var baseV = new List<Vector3> { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            int[] faces = { 0, 4, 3, 0, 3, 5, 0, 5, 2, 0, 2, 4, 1, 3, 4, 1, 5, 3, 1, 2, 5, 1, 4, 2 };
            var v = new List<Vector3>(); var t = new List<int>();
            for (int f = 0; f < faces.Length; f += 3)
            {
                var a = baseV[faces[f]]; var b = baseV[faces[f + 1]]; var cc = baseV[faces[f + 2]];
                var ab = (a + b).normalized; var bc = (b + cc).normalized; var ca = (cc + a).normalized;
                foreach (var tri in new[] { new[] { a, ab, ca }, new[] { ab, b, bc }, new[] { ca, bc, cc }, new[] { ab, bc, ca } })
                    foreach (var p in tri) { v.Add(p); t.Add(t.Count); }
            }
            var jit = new Dictionary<Vector3, Vector3>();
            for (int i = 0; i < v.Count; i++)
            {
                if (!jit.TryGetValue(v[i], out var j))
                {
                    j = Vector3.Scale(v[i], new Vector3(1f, 0.6f, 0.85f)) * (0.8f + (float)rnd.NextDouble() * 0.4f);
                    jit[v[i]] = j;
                }
                v[i] = j;
            }
            var m = new Mesh();
            m.SetVertices(v); m.SetTriangles(t, 0);
            m.RecalculateNormals();
            return m;
        }

        static Mesh ClumpMesh(int seed)
        {
            var rnd = new System.Random(seed);
            float R() => (float)rnd.NextDouble();
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < 14; i++)
            {
                float a = R() * Mathf.PI * 2f, rr = R() * 0.05f;
                var p = new Vector3(Mathf.Cos(a) * rr, 0f, Mathf.Sin(a) * rr);
                float h = 0.08f + R() * 0.1f, w = 0.012f + R() * 0.01f, yaw = R() * Mathf.PI * 2f;
                var side = new Vector3(Mathf.Cos(yaw), 0, Mathf.Sin(yaw)) * w;
                var lean = new Vector3(p.x, 0, p.z) * 1.5f + new Vector3(R() - 0.5f, 0, R() - 0.5f) * 0.05f;
                int b = v.Count;
                v.Add(p - side); v.Add(p + side); v.Add(p + lean + Vector3.up * h);
                n.Add(Vector3.up); n.Add(Vector3.up); n.Add(Vector3.up);
                t.Add(b); t.Add(b + 2); t.Add(b + 1);
            }
            // A clod of earth under the blades.
            var m = new Mesh();
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0);
            return m;
        }

        /// <summary>Flat ring segment (terrace): annulus between rIn and rOut, thickness th, from angle a0 to a1 (degrees).</summary>
        static void AddRing(List<Vector3> v, List<int> t, Vector3 c, float rIn, float rOut, float th, float a0, float a1, int seg)
        {
            for (int s = 0; s < seg; s++)
            {
                float u0 = Mathf.Lerp(a0, a1, (float)s / seg) * Mathf.Deg2Rad, u1 = Mathf.Lerp(a0, a1, (float)(s + 1) / seg) * Mathf.Deg2Rad;
                Vector3 P(float r, float a, float y) => c + new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                var q = new[]
                {
                    // top, bottom, outer, inner
                    new[] { P(rIn, u0, th), P(rIn, u1, th), P(rOut, u1, th), P(rOut, u0, th) },
                    new[] { P(rOut, u0, 0), P(rOut, u1, 0), P(rIn, u1, 0), P(rIn, u0, 0) },
                    new[] { P(rOut, u0, th), P(rOut, u1, th), P(rOut, u1, 0), P(rOut, u0, 0) },
                    new[] { P(rIn, u0, 0), P(rIn, u1, 0), P(rIn, u1, th), P(rIn, u0, th) },
                };
                foreach (var f in q) Quad(v, t, f[0], f[1], f[2], f[3]);
            }
            // End caps.
            foreach (var a in new[] { a0, a1 })
            {
                float u = a * Mathf.Deg2Rad;
                Vector3 P(float r, float y) => c + new Vector3(Mathf.Cos(u) * r, y, Mathf.Sin(u) * r);
                if (a == a0) Quad(v, t, P(rIn, 0), P(rIn, th), P(rOut, th), P(rOut, 0));
                else Quad(v, t, P(rOut, 0), P(rOut, th), P(rIn, th), P(rIn, 0));
            }
        }

        static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            // Unshared vertices so RecalculateNormals gives flat faces; winding a,b,c,d.
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            t.Add(i); t.Add(i + 2); t.Add(i + 1);
            t.Add(i); t.Add(i + 3); t.Add(i + 2);
        }

        static void AddBox(List<Vector3> v, List<int> t, Vector3 c, Vector3 size, float yaw = 0f)
        {
            var q = Quaternion.Euler(0, yaw, 0);
            Vector3 P(float x, float y, float z) => c + q * Vector3.Scale(new Vector3(x, y, z), size * 0.5f);
            Quad(v, t, P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1));     // top
            Quad(v, t, P(-1, -1, 1), P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1)); // bottom
            Quad(v, t, P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1)); // -z
            Quad(v, t, P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), P(-1, -1, 1));     // +z
            Quad(v, t, P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), P(-1, -1, -1)); // -x
            Quad(v, t, P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1));     // +x
        }

        static Mesh Finish(List<Vector3> v, List<int> t)
        {
            var m = new Mesh { indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v); m.SetTriangles(t, 0);
            m.RecalculateNormals();
            return m;
        }

        /// <summary>One distant complex: stacked partial ring terraces with a glowing lip on each.</summary>
        static void Complex(Transform parent, string name, Vector3 c, float scale, float a0, float a1, int tiers, Material solid, Material glow, int seed)
        {
            var rnd = new System.Random(seed);
            var v = new List<Vector3>(); var t = new List<int>();
            var gv = new List<Vector3>(); var gt = new List<int>();
            for (int i = 0; i < tiers; i++)
            {
                float rIn = (14f + i * 9f + (float)rnd.NextDouble() * 4f) * scale;
                float rOut = rIn + (10f + (float)rnd.NextDouble() * 8f) * scale;
                float y = (i * 8f + (float)rnd.NextDouble() * 3f) * scale;
                float th = (2.2f + (float)rnd.NextDouble()) * scale;
                float s0 = a0 + (float)rnd.NextDouble() * 40f, s1 = a1 - (float)rnd.NextDouble() * 40f;
                AddRing(v, t, c + Vector3.up * y, rIn, rOut, th, s0, s1, 48);
                // Glowing lip: a thin bright band along the outer edge.
                AddRing(gv, gt, c + Vector3.up * (y + th * 0.35f), rOut, rOut + 0.35f * scale, th * 0.25f, s0 + 3f, s1 - 3f, 48);
            }
            // Central drum the rings attach to.
            AddRing(v, t, c + Vector3.down * 20f * scale, 0.1f, 10f * scale, (tiers * 8f + 12f) * scale, 0f, 360f, 32);
            MeshObject(name, SaveMesh(Finish(v, t), "World_" + name), solid, parent, false);
            MeshObject(name + "_Glow", SaveMesh(Finish(gv, gt), "World_" + name + "_Glow"), glow, parent, false);
        }

        static void BuildOutdoorScene(AnimatorController sevenCtl, AnimatorController botCtl, Mats mats, GameObject atmoPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = null;
            RenderSettings.fog = false;                       // the Atmosphere full-screen pass does the fog
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.26f, 0.26f, 0.26f);
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(RenderSettings.ambientLight);
            RenderSettings.ambientProbe = sh;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            AddGround(Seven.Ground.Shape.Hill);

            var atmo = (GameObject)PrefabUtility.InstantiatePrefab(atmoPrefab);
            atmo.name = "Atmosphere";

            var world = new GameObject("World").transform;

            // Foreground: the dark grassy rise.
            MeshObject("Hill", SaveMesh(HillMesh(), "World_Hill"), mats.ground, world);
            var grass = MeshObject("Grass", SaveMesh(GrassMesh(140000, Seven.WorldGround.PlateauRadius + 4f), "World_Grass"), mats.grass, world, false);
            grass.GetComponent<MeshRenderer>().receiveShadows = true;

            // Loose rocks and grass clumps on the sparring area (telekinesis lifts the ones near 7).
            var debrisRoot = new GameObject("Debris").transform;
            debrisRoot.SetParent(world, false);
            var rockMeshes = Enumerable.Range(0, 5).Select(i => SaveMesh(RockMesh(100 + i), "World_Rock" + i)).ToArray();
            var clumpMeshes = Enumerable.Range(0, 3).Select(i => SaveMesh(ClumpMesh(200 + i), "World_Clump" + i)).ToArray();
            var rnd = new System.Random(5);
            float R() => (float)rnd.NextDouble();
            var spots = new List<Vector3>();
            var ac = Seven.WorldGround.ArenaCenter;
            for (int i = 0; i < 90 && spots.Count < 60; i++)
            {
                float rr = 0.5f + Mathf.Sqrt(R()) * 6.0f, a = R() * Mathf.PI * 2f;
                var p = new Vector3(ac.x + Mathf.Cos(a) * rr, 0, ac.z + Mathf.Sin(a) * rr);
                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(-1.4f, -0.5f)) < 0.7f) continue;
                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(1.4f, -0.5f)) < 0.7f) continue;
                if (spots.Any(s => Vector3.Distance(s, p) < 0.45f)) continue;
                spots.Add(p);
            }
            for (int i = 0; i < spots.Count; i++)
            {
                var p = spots[i];
                bool rock = i % 3 != 2;
                float size = rock ? Mathf.Lerp(0.05f, 0.16f, R() * R()) : 1f;
                var go = MeshObject(rock ? "Rock" + i : "Clump" + i, rock ? rockMeshes[i % 5] : clumpMeshes[i % 3], rock ? mats.rock : mats.grass, debrisRoot, rock);
                go.transform.position = new Vector3(p.x, Seven.WorldGround.HillHeight(p.x, p.z) + (rock ? size * 0.35f : -0.005f), p.z);
                go.transform.rotation = rock ? Quaternion.Euler(R() * 360f, R() * 360f, R() * 360f) : Quaternion.Euler(0, R() * 360f, 0);
                go.transform.localScale = Vector3.one * size;
                go.AddComponent<Seven.Debris>();
            }

            // QF32 (3): the sparring ground seen from above was one flat tone (the top-down orbit shot: range 0.09) - old worn
            // flagstones in the grass round the arena, flush with the ground (1.5 cm proud, no colliders), pale weathered stone
            {
                var flagMat = Lit("World_flagstone", new Color(0.30f, 0.30f, 0.30f), 0.08f);
                var fv = new List<Vector3>(); var ft = new List<int>(); var frnd = new System.Random(32);
                float F() => (float)frnd.NextDouble();
                var placed = new List<Vector3>();
                for (int i = 0; i < 400 && placed.Count < 70; i++)
                {
                    float rr = 0.4f + Mathf.Sqrt(F()) * 7.0f, a = F() * Mathf.PI * 2f;
                    var p = new Vector3(ac.x + Mathf.Cos(a) * rr, 0f, ac.z + Mathf.Sin(a) * rr);
                    float s = Mathf.Lerp(0.35f, 0.75f, F());
                    if (placed.Any(q => Vector3.Distance(q, p) < s * 0.9f + 0.3f)) continue;
                    placed.Add(p);
                    float yaw = F() * 360f;
                    // an irregular flat stone: a 7-sided slab, its rim a little lower (weathered edges)
                    int b0 = fv.Count; var cc = new Vector3(p.x, Seven.WorldGround.HillHeight(p.x, p.z) + 0.015f, p.z); fv.Add(cc);
                    for (int k = 0; k < 7; k++)
                    {
                        float ang = (yaw + k * 360f / 7f + F() * 18f) * Mathf.Deg2Rad, rad = s * Mathf.Lerp(0.42f, 0.55f, F());
                        float x = p.x + Mathf.Cos(ang) * rad, z = p.z + Mathf.Sin(ang) * rad;
                        fv.Add(new Vector3(x, Seven.WorldGround.HillHeight(x, z) + 0.008f, z));
                    }
                    for (int k = 0; k < 7; k++) { ft.Add(b0); ft.Add(b0 + 1 + (k + 1) % 7); ft.Add(b0 + 1 + k); }
                }
                MeshObject("SparringFlagstones", SaveMesh(Finish(fv, ft), "World_Flagstones"), flagMat, world, false);
            }

            // The distance: huge ring-terrace complexes, towers and a dark overhead arc, all fading into the fog.
            var far = new GameObject("Distance").transform;
            far.SetParent(world, false);
            // Composed for the default 3/4 view (camera near ViewOrigin, looking ~18° right of +z) like Kyle's art:
            // terrace complex + towers on the right rising out of the fog sea, more complexes fading behind,
            // and a dark overhead mass whose curved edge sweeps from the top-left down toward the towers.
            var o = ViewOrigin;
            Vector3 At(float yaw, float dist, float y) => new Vector3(o.x + Mathf.Sin(yaw * Mathf.Deg2Rad) * dist, y, o.z + Mathf.Cos(yaw * Mathf.Deg2Rad) * dist);
            float Facing(Vector3 c) => Mathf.Atan2(o.z - c.z, o.x - c.x) * Mathf.Rad2Deg;   // ring angle pointing at the viewer
            void Comp(string name, float yaw, float dist, float y, float scale, float span, int tiers, int seed)
            {
                var c = At(yaw, dist, y);
                float f = Facing(c);
                Complex(far, name, c, scale, f - span * 0.5f, f + span * 0.5f, tiers, mats.structure, mats.worldGlow, seed);
            }
            Comp("ComplexA", 42f, 100f, -15f, 0.75f, 200f, 4, 1);     // main terraces, right
            Comp("ComplexA2", 58f, 150f, -19f, 0.9f, 160f, 3, 7);   // behind them, fainter
            Comp("ComplexB", -38f, 235f, -14f, 1.1f, 170f, 4, 2);    // left, fainter
            Comp("ComplexE", 6f, 330f, -17f, 1.7f, 160f, 4, 5);     // far centre
            Comp("ComplexC", 180f, 220f, -15f, 1.4f, 180f, 5, 3);    // behind (orbit views)
            Comp("ComplexD", 105f, 200f, -15f, 1.2f, 180f, 4, 4);    // right side
            Comp("ComplexF", -110f, 210f, -15f, 1.2f, 180f, 4, 6);   // left side

            var tv = new List<Vector3>(); var tt = new List<int>();
            var towers = new[]
            {
                (At(33f, 140f, 60f), new Vector3(14, 250, 14)),
                (At(37.5f, 150f, 60f), new Vector3(5, 250, 5)),
                (At(-22f, 300f, 50f), new Vector3(12, 230, 12)),
                (At(140f, 240f, 60f), new Vector3(14, 250, 14)),
                (At(-150f, 230f, 60f), new Vector3(16, 250, 16)),
                (At(-80f, 260f, 60f), new Vector3(10, 250, 10)),
            };
            foreach (var (c, s) in towers) AddBox(tv, tt, c, s, 12f);
            MeshObject("Towers", SaveMesh(Finish(tv, tt), "World_Towers"), mats.structure, far, false);

            // Overhead mass: a thick curved slab; its lower front edge is the arc seen in the reference.
            var hv = new List<Vector3>(); var ht = new List<int>();
            const int n = 48;
            var edge = new Vector3[n + 1]; var back = new Vector3[n + 1]; var low = new Vector3[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float s = (float)i / n;
                float yaw = Mathf.Lerp(-95f, 85f, s);
                float k = Mathf.Clamp01((85f - yaw) / 100f);
                float elev = 2.5f + 10.2f * Mathf.Pow(k, 2.67f);         // ~12.5° at the top-left, 6° mid, ~3° at the right
                float dist = Mathf.Lerp(110f, 340f, Mathf.Clamp01((yaw + 20f) / 105f));
                var e = At(yaw, dist, o.y + dist * Mathf.Tan(elev * Mathf.Deg2Rad));
                edge[i] = e;
                low[i] = e + Vector3.down * Mathf.Lerp(8f, 22f, s);
                back[i] = Vector3.Lerp(new Vector3(o.x, e.y + 25f, o.z), e, 0.15f) + Vector3.up * 10f;   // underside runs back over the viewer
            }
            for (int i = 0; i < n; i++)
            {
                Quad(hv, ht, low[i], low[i + 1], edge[i + 1], edge[i]);       // front face
                Quad(hv, ht, edge[i], edge[i + 1], back[i + 1], back[i]);     // underside
                Quad(hv, ht, low[i + 1], low[i], back[i], back[i + 1]);       // closing face
            }
            MeshObject("OverheadArc", SaveMesh(Finish(hv, ht), "World_OverheadArc"), mats.halo, far, false);

            AddProbe(new Vector3(0, 1.6f, -1.5f), new Vector3(30, 12, 30), Vector3.zero);

            var sevenPos = new Vector3(-1.4f, Seven.WorldGround.HillHeight(-1.4f, -0.5f), -0.5f);
            var botPos = new Vector3(1.4f, Seven.WorldGround.HillHeight(1.4f, -0.5f), -0.5f);
            SetupFight(sevenCtl, botCtl, sevenPos, botPos, new Color(0.6f, 0.6f, 0.6f), 1500f, mats.additive);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Log("scene saved " + ScenePath);
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Dan";
            PlayerSettings.productName = "7: The Observer — Demo";   // QF2
            PlayerSettings.bundleVersion = Seven.LauncherBridge.Version;   // Round 8: launcher release version
            PlayerSettings.runInBackground = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;   // QF1: borderless, native resolution
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.allowFullscreenSwitch = true;                        // Alt+Enter
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
        }

        // ------------------------------------------------------------------------------------- player
        [MenuItem("Seven/Build Windows Player")]
        public static bool BuildPlayer()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ExePath));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = AllScenes,
                locationPathName = ExePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            Log($"player build {report.summary.result}: {ExePath} ({report.summary.totalSize / 1e6:0.0} MB, {report.summary.totalErrors} errors)");
            if (report.summary.result == BuildResult.Succeeded)
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(ExePath), "version.txt"), Seven.LauncherBridge.Version);   // checked by Tools/Prepare-*.py
            return report.summary.result == BuildResult.Succeeded;
        }
    }
}
