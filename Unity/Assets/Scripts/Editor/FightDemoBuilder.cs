using System;
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
    /// Builds Round 1's fight demo from the repo's art folders, start to finish, so it can run headless:
    ///   Unity.exe -batchmode -quit -projectPath Unity -executeMethod Seven.EditorTools.FightDemoBuilder.BuildAllFromCommandLine
    /// Steps: copy Mixamo FBX files into Assets -> Humanoid import settings -> grayscale texture + materials ->
    /// animator controllers -> FightDemo scene -> Windows player (Unity/Builds/FightDemo/SevenFightDemo.exe).
    /// </summary>
    public static class FightDemoBuilder
    {
        const string CharRoot = "Assets/Art/Characters";
        const string SevenDir = CharRoot + "/7";
        const string BotDir = CharRoot + "/YBot";
        const string MatDir = "Assets/Art/Materials";
        const string AnimDir = "Assets/Art/Animators";
        const string ScenePath = "Assets/Scenes/FightDemo.unity";
        const string ProfilePath = "Assets/Settings/FightDemoVolume.asset";

        static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        public static string ExePath => Path.Combine(ProjectRoot, "Builds", "FightDemo", "SevenFightDemo.exe");

        static readonly string[] Loops = { "idle", "walk", "run", "fight_idle", "dazed" };
        static readonly string[] RootMotion = { "finisher", "dodge", "knocked_down", "get_up" };

        static void Log(string s) => Debug.Log("[FightDemoBuilder] " + s);

        public static void BuildAllFromCommandLine()
        {
            int code = 1;
            try
            {
                BuildAll();
                code = BuildPlayer() ? 0 : 1;
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
            BuildScene(sevenCtl, botCtl, mats);
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
            CopyDir(Path.Combine(RepoRoot, "Art", "Mixamo", "7"), SevenDir, "*.fbx");
            CopyDir(Path.Combine(RepoRoot, "Art", "Mixamo", "YBot"), BotDir, "*.fbx");

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
        struct Mats { public Material body, metal, glow, botBody, botJoints, floor, wall, strip, pillar; }

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
            };
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
            var anim = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
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

        static void BuildScene(AnimatorController sevenCtl, AnimatorController botCtl, Mats mats)
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

            var probeGo = new GameObject("ReflectionProbe");
            probeGo.transform.position = new Vector3(0, 1.6f, 0);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.size = new Vector3(19, 7.2f, 15.5f);
            probe.center = new Vector3(0, 1.9f, 0);
            probe.resolution = 128;
            probe.hdr = true;

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

            var seven = Spawn(SevenDir + "/7_character.fbx", "7", new Vector3(-1.4f, 0, 0), Vector3.right, sevenCtl);
            var bot = Spawn(BotDir + "/ybot_character.fbx", "TrainingBot", new Vector3(1.4f, 0, 0), Vector3.left, botCtl);

            var camGo = new GameObject("FightCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = fog;
            cam.fieldOfView = 38f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 60f;
            camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var fcam = camGo.AddComponent<Seven.FightCamera>();
            fcam.A = seven.transform; fcam.B = bot.transform;
            camGo.transform.rotation = Quaternion.Euler(fcam.ViewEuler);
            camGo.transform.position = new Vector3(0, 0.95f, 0) - camGo.transform.forward * (fcam.MinDistance + 2.8f * fcam.PerMetre);

            var dirGo = new GameObject("FightDirector");
            var input = dirGo.AddComponent<Seven.FightInput>();
            var sf = seven.AddComponent<Seven.SevenFighter>();
            var tb = bot.AddComponent<Seven.TrainingBot>();
            sf.Bot = tb; sf.CameraTransform = camGo.transform; sf.Input = input;
            tb.Seven = sf;
            var director = dirGo.AddComponent<Seven.FightDirector>();
            director.Seven = sf; director.Bot = tb; director.Input = input; director.Cam = fcam;

            Directory.CreateDirectory(Path.Combine(ProjectRoot, "Assets", "Scenes"));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Log("scene saved " + ScenePath);
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Dan";
            PlayerSettings.productName = "7 The Observer - Fight Demo";
            PlayerSettings.runInBackground = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
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
                scenes = new[] { ScenePath },
                locationPathName = ExePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            Log($"player build {report.summary.result}: {ExePath} ({report.summary.totalSize / 1e6:0.0} MB, {report.summary.totalErrors} errors)");
            return report.summary.result == BuildResult.Succeeded;
        }
    }
}
