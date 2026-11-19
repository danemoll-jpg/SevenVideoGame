using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using V = Seven.VenusLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 11 — the Venus scene (Chapter 2, part 1): VEN_01 landing platform with the saucer and its storage hatch,
    /// VEN_02 the outer gardens (broad riding road, long bend, damaged channel + collapsed walkway, the on-foot detour),
    /// VEN_03 the temple (court, pedestrian garden, the 3.5 m-scale entrance, hall, cube chamber with the original cube
    /// in its protected structure), VEN_04 the lower gardens (maintenance path, creature observation area over the
    /// basin, the shut root-chamber gate). The First Garden Age in grayscale: terraces, hanging gardens, water channels,
    /// tall pale towers (some unfinished) fading into the bright mist under a thick cloud ceiling (Atmosphere_Venus).
    /// Models: Art/Export/hoverbike, elarim (rigged to Dan's Mixamo skeleton, clips Art/Mixamo/elarim), worm; the cat
    /// (Panther) and 7 as in Chapter 1. Numbers: Seven.VenusLayout.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const string VenusScenePath = "Assets/Scenes/Venus.unity";
        const string ElarimDir = CharRoot + "/Elarim";
        const string AtmoVenusPath = AtmoDir + "/Atmosphere_Venus.prefab";

        struct VenusMats { public Material stone, stoneMid, stoneDark, leaf, leafDark, leafPale, water, tower, path, glow, hull, hullUnder, dust, bikeVent, cloud; }

        static void BuildRound11(AnimatorController sevenCtl, GameObject atmoPrefab, Seven.VoiceBank bank)
        {
            AddRideClip(sevenCtl);                                     // QF10: the riding pose (Blender-made, no Mixamo clip exists)
            var catCtl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "/Cat.controller");
            var catFur = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Cat_fur.mat");
            var catWhisker = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Cat_whisker.mat");
            var m = new VenusMats
            {
                stone = Dissolve("Venus_stone", Gray(0.66f), 0.25f, false),
                stoneMid = Dissolve("Venus_stone_mid", Gray(0.48f), 0.2f, false),
                stoneDark = Dissolve("Venus_stone_dark", Gray(0.26f), 0.15f, false),
                leaf = Dissolve("Venus_leaf", Gray(0.30f), 0.2f, false),
                leafDark = Dissolve("Venus_leaf_dark", Gray(0.16f), 0.15f, false),
                leafPale = Dissolve("Venus_leaf_pale", Gray(0.52f), 0.25f, false),
                water = Dissolve("Venus_water", Gray(0.82f), 0.97f, false, 0.2f),
                tower = Dissolve("Venus_tower", Gray(0.86f), 0.4f, false),
                path = Dissolve("Venus_path", Gray(0.58f), 0.2f, false),
                glow = Dissolve("Venus_glow", Color.white, 0.5f, false),
                hull = Dissolve("Saucer_hull", Gray(0.95f), 0.9f, false, 0.6f),
                hullUnder = Dissolve("Saucer_under", Gray(0.74f), 0.8f, false, 0.45f),
                dust = Dissolve("Venus_basin", Gray(0.40f), 0.1f, false),
                bikeVent = Dissolve("Bike_vent", Color.white, 0.5f, false),
                cloud = Dissolve("Venus_cloud", Gray(0.93f), 0.0f, false),
            };
            foreach (var x in new[] { m.glow, m.bikeVent }) { x.EnableKeyword("_EMISSION"); x.SetColor("_EmissionColor", Color.white * 2f); EditorUtility.SetDirty(x); }
            m.water.EnableKeyword("_EMISSION"); m.water.SetColor("_EmissionColor", Color.white * 0.12f); m.water.SetFloat("_Rim", 0.5f);
            m.cloud.EnableKeyword("_EMISSION"); m.cloud.SetColor("_EmissionColor", Color.white * 0.55f); m.cloud.SetFloat("_Cull", 0f);
            var elarim = SetupElarim(sevenCtl, out var elarimCtl, out var elarimMat);
            var worm = SetupGenericModel("worm", out var wormCtl);
            var bike = SetupStaticModel("hoverbike", "hoverbike_v1.fbx");
            var sword = SetupStaticModel("elarim", "elarim_sword_v1.fbx");
            var atmo = BuildAtmosphereVenus(atmoPrefab);
            BuildVenusScene(sevenCtl, catCtl, catFur, catWhisker, bank, m, atmo, elarim, elarimCtl, worm, wormCtl, bike, sword);
        }

        // ---------------------------------------------------------------------------------------------- imports
        static string CopyExport(string kind, string file)
        {
            var src = Path.Combine(RepoRoot, "Art", "Export", kind, file);
            var dir = CharRoot + "/" + kind;
            Directory.CreateDirectory(Path.Combine(ProjectRoot, dir, "Textures"));
            if (!File.Exists(src)) { Log($"WARNING: {src} missing (run Tools/Blender/cleanup_r11.py {kind})"); return null; }
            File.Copy(src, Path.Combine(ProjectRoot, dir, file), true);
            var tdir = Path.Combine(RepoRoot, "Art", "Export", kind, "Textures");
            if (Directory.Exists(tdir)) foreach (var t in Directory.GetFiles(tdir)) File.Copy(t, Path.Combine(ProjectRoot, dir, "Textures", Path.GetFileName(t)), true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return dir + "/" + file;
        }

        static Material TexturedMat(string name, string texPath, float smooth, float metallic = 0f)
        {
            var mat = Dissolve(name, Color.white, smooth, false, metallic);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null) { var ti = (TextureImporter)AssetImporter.GetAtPath(texPath); ti.maxTextureSize = 2048; ti.SaveAndReimport(); mat.SetTexture("_BaseMap", tex); }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static GameObject SetupStaticModel(string kind, string file)
        {
            var path = CopyExport(kind, file); if (path == null) return null;
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            imp.animationType = ModelImporterAnimationType.None; imp.importAnimation = false; imp.isReadable = true;
            imp.materialImportMode = ModelImporterMaterialImportMode.None; imp.SaveAndReimport();
            var mat = TexturedMat("Venus_" + Path.GetFileNameWithoutExtension(file), CharRoot + "/" + kind + "/Textures/" + Path.GetFileNameWithoutExtension(file) + "_basecolor.jpg", kind == "hoverbike" ? 0.75f : 0.6f, kind == "hoverbike" ? 0.35f : 0.5f);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;   // applied on the instance below too
            return go;
        }

        static GameObject SetupGenericModel(string kind, out AnimatorController ctl)
        {
            ctl = null;
            var path = CopyExport(kind, kind + "_v1_rigged.fbx"); if (path == null) return null;
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            imp.animationType = ModelImporterAnimationType.Generic; imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = true; imp.materialImportMode = ModelImporterMaterialImportMode.None; imp.SaveAndReimport();
            var clips = imp.defaultClipAnimations;
            foreach (var c in clips) { var n = c.takeName.Contains("|") ? c.takeName.Substring(c.takeName.LastIndexOf('|') + 1) : c.takeName; c.name = n; c.loopTime = n == "wriggle" || n == "idle_under"; }
            imp.clipAnimations = clips; imp.SaveAndReimport();
            var cpath = AnimDir + "/" + kind + ".controller";
            AssetDatabase.DeleteAsset(cpath);
            ctl = AnimatorController.CreateAnimatorControllerAtPath(cpath);
            var sm = ctl.layers[0].stateMachine;
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")))
            { var st = sm.AddState(clip.name); st.motion = clip; if (clip.name == "wriggle") sm.defaultState = st; }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>The Elarim: Humanoid (Mixamo skeleton), Dan's eight Mixamo clips retargeted.</summary>
        static GameObject SetupElarim(AnimatorController sevenCtl, out AnimatorController ctl, out Material mat)
        {
            ctl = null; mat = null;
            var path = CopyExport("elarim", "elarim_v1_rigged.fbx"); if (path == null) return null;
            mat = TexturedMat("Elarim_armour", ElarimDir + "/Textures/elarim_v1_basecolor.jpg", 0.82f, 0.55f);
            mat.SetFloat("_Rim", 0.55f); mat.SetFloat("_RimPower", 2.2f);
            mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Color.white * 0.12f);     // they glow softly all over
            EditorUtility.SetDirty(mat);
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            imp.animationType = ModelImporterAnimationType.Human; imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            // QF13 (3, 17): Read/Write ON - ElarimCape reads the cape vertices at run time; with it off the vertex list was
            // empty in the game, so the cape never swung clear of his legs and every "leg contact 0.0 cm" check (QF10, QF12) measured nothing
            imp.isReadable = true;
            imp.importAnimation = false; imp.materialImportMode = ModelImporterMaterialImportMode.None; imp.SaveAndReimport();
            ApplyMixamoHumanMap(imp, path);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            Log($"{path}: Elarim avatar human={avatar?.isHuman} valid={avatar?.isValid}");
            if (avatar == null || !avatar.isHuman) return null;
            var clipDir = ElarimDir + "/Clips"; Directory.CreateDirectory(Path.Combine(ProjectRoot, clipDir));
            var loops = new[] { "idle", "talk", "walk", "walk_v2", "sword_idle", "carry", "lift" };
            var states = new List<AnimationClip>();
            foreach (var f in Directory.GetFiles(Path.Combine(RepoRoot, "Art", "Mixamo", "elarim"), "elarim_*.fbx"))
            {
                if (Path.GetFileName(f).Contains("body")) continue;          // QF12: Dan's rigged body (elarim_body_mixamo.fbx) is the model source, not a clip
                var dst = clipDir + "/" + Path.GetFileName(f);
                File.Copy(f, Path.Combine(ProjectRoot, dst), true);
                AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceSynchronousImport);
                var ci = (ModelImporter)AssetImporter.GetAtPath(dst);
                ci.animationType = ModelImporterAnimationType.Human; ci.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                ci.materialImportMode = ModelImporterMaterialImportMode.None; ci.importAnimation = true;
                ci.SaveAndReimport();
                ApplyMixamoHumanMap(ci, dst);
                var state = Path.GetFileNameWithoutExtension(f).Substring("elarim_".Length);
                var cl = ci.defaultClipAnimations;
                // QF14 (7): Dan's walk_v2 is not "In Place": its forward travel is NOT baked into the pose (extracted as root motion,
                // which the Animator does not apply) - he walks on the spot and ElarimNpc moves him, playback matched to his speed
                foreach (var c in cl) { c.name = state; c.loopTime = loops.Contains(state); c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = state != "walk_v2"; c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true; }
                ci.clipAnimations = cl; ci.SaveAndReimport();
                var clip = AssetDatabase.LoadAllAssetsAtPath(dst).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                if (clip != null) { states.Add(clip); Log($"Elarim clip {clip.name} {clip.length:0.00}s humanMotion={clip.humanMotion}"); }
            }
            var cpath = AnimDir + "/Elarim.controller";
            AssetDatabase.DeleteAsset(cpath);
            ctl = AnimatorController.CreateAnimatorControllerAtPath(cpath);
            var sm = ctl.layers[0].stateMachine;
            foreach (var c in states) { var st = sm.AddState(c.name); st.motion = c; if (c.name == "idle") sm.defaultState = st; }
            // QF13 (3b): the walk. Dan's elarim_walk clip itself is the problem (measured in Blender: the torso leans 1-5 deg
            // forward, one hand reaches 0.27 of his height forward, palm up, hands 0.24 out to the sides - on the 3.5 m figure
            // it reads as a stooped, groping walk). Use Dan's new walk if he adds Art/Mixamo/elarim/elarim_walk_v2.fbx;
            // otherwise 7's own Mixamo walk (upright: lean -2..1 deg, hands 0.20 forward / 0.18 out), retargeted through his
            // Humanoid avatar. ElarimNpc then matches its playback to his speed (no skating) and relaxes the arms.
            {
                var walkSt = sm.states.FirstOrDefault(s => s.state.name == "walk").state;
                var v2 = states.FirstOrDefault(c => c.name == "walk_v2");
                var sevenWalk = AssetDatabase.LoadAllAssetsAtPath(SevenDir + "/7_walk.fbx").OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                var use = v2 != null ? v2 : sevenWalk;
                if (walkSt != null && use != null) { walkSt.motion = use; Log($"QF13 (3b): the guardian's walk state plays {(v2 != null ? "Dan's elarim_walk_v2" : "7's Mixamo walk (7_walk.fbx)")} ({use.length:0.00}s, loop {use.isLooping})"); }
            }
            // Round 12 (B1): Dan's Mixamo "kneel" never kneels (hips at standing height in every frame) - the Blender-made
            // kneel (Tools/Blender/make_kneel_pose.py) replaces the state's motion
            var kp = Path.Combine(RepoRoot, "Art", "Export", "anim", "elarim_kneel_pose.fbx");
            if (File.Exists(kp))
            {
                var dst = clipDir + "/elarim_kneel_pose.fbx";
                File.Copy(kp, Path.Combine(ProjectRoot, dst), true);
                AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceSynchronousImport);
                var ci = (ModelImporter)AssetImporter.GetAtPath(dst);
                ci.animationType = ModelImporterAnimationType.Human; ci.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                ci.materialImportMode = ModelImporterMaterialImportMode.None; ci.importAnimation = true; ci.SaveAndReimport();
                ApplyMixamoHumanMap(ci, dst);
                var cl = ci.defaultClipAnimations;
                foreach (var c in cl) { c.name = "kneel"; c.loopTime = true; c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true; c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true; }
                ci.clipAnimations = cl; ci.SaveAndReimport();
                var clip = AssetDatabase.LoadAllAssetsAtPath(dst).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                var kst = sm.states.Select(x => x.state).FirstOrDefault(x => x.name == "kneel") ?? sm.AddState("kneel");
                if (clip != null) { kst.motion = clip; Log($"Elarim kneel: the Blender-made kneel pose ({clip.length:0.00}s, humanMotion={clip.humanMotion}) replaces Dan's kneel clip"); }
            }
            else Log("WARNING: Art/Export/anim/elarim_kneel_pose.fbx missing (run Tools/Blender/make_kneel_pose.py) - the guardian's kneel stays the Mixamo clip");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>Explicit Humanoid map for a Mixamo skeleton (Unity's name guesser did not take the Blender round-trip).</summary>
        static readonly (string mix, string human)[] MixamoMap =
        {
            ("Hips", "Hips"), ("Spine", "Spine"), ("Spine1", "Chest"), ("Spine2", "UpperChest"), ("Neck", "Neck"), ("Head", "Head"),
            ("LeftShoulder", "LeftShoulder"), ("LeftArm", "LeftUpperArm"), ("LeftForeArm", "LeftLowerArm"), ("LeftHand", "LeftHand"),
            ("RightShoulder", "RightShoulder"), ("RightArm", "RightUpperArm"), ("RightForeArm", "RightLowerArm"), ("RightHand", "RightHand"),
            ("LeftUpLeg", "LeftUpperLeg"), ("LeftLeg", "LeftLowerLeg"), ("LeftFoot", "LeftFoot"), ("LeftToeBase", "LeftToes"),
            ("RightUpLeg", "RightUpperLeg"), ("RightLeg", "RightLowerLeg"), ("RightFoot", "RightFoot"), ("RightToeBase", "RightToes"),
        };
        static void ApplyMixamoHumanMap(ModelImporter imp, string path)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var all = model.GetComponentsInChildren<Transform>(true);
            string Find(string mix) => all.FirstOrDefault(t => t.name == "mixamorig:" + mix || t.name == "mixamorig_" + mix || t.name == mix || t.name.EndsWith(":" + mix))?.name;
            var hd = imp.humanDescription;
            var human = new List<HumanBone>();
            foreach (var (mix, hu) in MixamoMap)
            {
                var n = Find(mix); if (n == null) continue;
                var unityName = HumanTrait.BoneName.FirstOrDefault(h => h.Replace(" ", "") == hu) ?? hu;
                human.Add(new HumanBone { boneName = n, humanName = unityName, limit = new HumanLimit { useDefaultValues = true } });
            }
            hd.human = human.ToArray();
            hd.skeleton = all.Select(t => new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale }).ToArray();
            hd.hasTranslationDoF = false;
            imp.humanDescription = hd;
            imp.SaveAndReimport();
            var av = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            Log($"{path}: explicit Mixamo human map {human.Count} bones -> avatar human={av?.isHuman} valid={av?.isValid} (first bone name '{all.Skip(1).FirstOrDefault()?.name}')");
        }

        static GameObject BuildAtmosphereVenus(GameObject atmoPrefab)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(atmoPrefab);
            var a = inst.GetComponent<Seven.Atmosphere>();
            // Kyle's fog / glow: dark near silhouettes against a bright glowing misty distance, soft light inside the fog,
            // strong ground fog in the gardens, a thick bright cloud ceiling (soft diffuse light from above).
            a.FogStart = 8f;
            a.GroundDensity = 0.011f; a.GroundBase = -3f; a.GroundFalloff = 2.6f; a.GroundRange = 70f;
            a.ValleyDensity = 0.006f; a.ValleyBase = -18f; a.ValleyFalloff = 8f;
            a.DistanceDensity = 0.0034f; a.HazeHeight = 60f; a.MaxFog = 1f;
            a.NearBrightness = 0.22f; a.FarBrightness = 1.35f; a.BrightnessDistance = 70f; a.ZenithBrightness = 0.85f;
            a.NoiseStrength = 0.45f; a.NoiseScale = 0.04f; a.NoiseSpeed = 0.02f; a.NoiseFadeDistance = 70f;
            a.GlowIntensity = 0.38f; a.GlowRadius = 40f;
            var glows = new[]
            {
                (new Vector3(30f, 30f, 300f), new Vector3(2.2f, 1.6f, 1f)),       // behind the temple
                (new Vector3(-90f, 20f, 160f), new Vector3(1.6f, 1.2f, 1f)),
                (new Vector3(150f, 10f, 120f), new Vector3(1.8f, 1.2f, 1f)),      // over the basin
                (new Vector3(-60f, 25f, 360f), new Vector3(2.6f, 1.5f, 1f)),
                (new Vector3(110f, 30f, 340f), new Vector3(2.2f, 1.4f, 1f)),
                (new Vector3(0f, 18f, -120f), new Vector3(2.0f, 1.2f, 1f)),
                (new Vector3(30f, 40f, 520f), new Vector3(7f, 1.8f, 1f)),
                (new Vector3(-200f, 20f, 260f), new Vector3(5f, 1.6f, 1f)),
            };
            int gi = 0;
            foreach (Transform c in inst.transform)
            {
                if (c.name.StartsWith("Glow") && gi < glows.Length) { c.position = glows[gi].Item1; c.localScale = glows[gi].Item2; gi++; }
                if (c.name == "BackLight") { c.rotation = Quaternion.Euler(52f, 165f, 0f); var l = c.GetComponent<Light>(); l.intensity = 0.95f; l.shadowStrength = 0.45f; }
                if (c.name == "FrontFill") { c.rotation = Quaternion.Euler(60f, 15f, 0f); c.GetComponent<Light>().intensity = 0.45f; }
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(inst, AtmoVenusPath);
            Object.DestroyImmediate(inst);
            Log("atmosphere variant saved " + AtmoVenusPath);
            return prefab;
        }

        // ---------------------------------------------------------------------------------------------- geometry helpers
        static GameObject SlopeSlab(string name, float x0, float z0, float x1, float z1, float y0, float y1, int axis, Material mat, Transform parent, float thick = 0.6f)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            Vector3 P(float x, float z, float dy) { float u = axis == 0 ? Mathf.InverseLerp(x0, x1, x) : Mathf.InverseLerp(z0, z1, z); return new Vector3(x, Mathf.Lerp(y0, y1, u) + dy, z); }
            var c = new[] { P(x0, z0, 0), P(x1, z0, 0), P(x1, z1, 0), P(x0, z1, 0), P(x0, z0, -thick), P(x1, z0, -thick), P(x1, z1, -thick), P(x0, z1, -thick) };
            int[][] quads = { new[] { 3, 2, 1, 0 }, new[] { 4, 5, 6, 7 }, new[] { 0, 1, 5, 4 }, new[] { 1, 2, 6, 5 }, new[] { 2, 3, 7, 6 }, new[] { 3, 0, 4, 7 } };
            foreach (var q in quads) { int b = v.Count; foreach (var i in q) v.Add(c[i]); t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 }); }
            var go = MeshObject(name, SaveMesh(Finish(v, t), "Venus_" + name), mat, parent);
            go.AddComponent<MeshCollider>();
            return go;
        }

        static void Shrub(List<Vector3> v, List<int> t, System.Random r, Vector3 at, float size)
        {
            Lump(v, t, at + Vector3.up * size * 0.5f, new Vector3(size, size * (0.7f + (float)r.NextDouble() * 0.5f), size), 0.35f, r.Next(1000));
        }

        static void Tree(List<Vector3> v, List<int> t, System.Random r, Vector3 at, float h)
        {
            // a tall slender garden tree: trunk + stacked soft lumps (cypress-like, grown rather than built)
            AddBox(v, t, at + Vector3.up * h * 0.2f, new Vector3(0.25f, h * 0.4f, 0.25f));
            for (int i = 0; i < 4; i++)
            {
                float y = h * (0.35f + i * 0.17f), s = h * (0.24f - i * 0.04f);
                Lump(v, t, at + Vector3.up * y, new Vector3(s, s * 1.4f, s), 0.3f, r.Next(1000));
            }
        }

        // ---------------------------------------------------------------------------------------------- the scene
        static void BuildVenusScene(AnimatorController sevenCtl, AnimatorController catCtl, Material catFur, Material catWhisker, Seven.VoiceBank bank,
            VenusMats m, GameObject atmoVenus, GameObject elarim, AnimatorController elarimCtl, GameObject worm, AnimatorController wormCtl, GameObject bike, GameObject sword)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null; RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.34f, 0.34f, 0.34f);
            var sh = new SphericalHarmonicsL2(); sh.AddAmbientLight(RenderSettings.ambientLight); RenderSettings.ambientProbe = sh;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            var atmo = (GameObject)PrefabUtility.InstantiatePrefab(atmoVenus); atmo.name = "Atmosphere_Venus";
            var world = new GameObject("Venus").transform;
            var rnd = new System.Random(11);

            // ---- QF10 (6f, Docs/VENUS-TERRAIN.md): natural ground. Built floors only where there is architecture (the
            // court, the temple); everything else is one sculpted terrain (VenusTerrain: the worn path, the valley, the hills
            // with their garden shelves, the stream, the gully, the basin) out to the fog.
            foreach (var z in V.Zones)
            {
                if (V.NaturalZone(z.Name)) continue;
                var mat = z.Name.StartsWith("Temple") || z.Name == "CubeRoom" ? m.stone : z.Name.StartsWith("Root") || z.Name == "DeepTunnel" || z.Name.StartsWith("Ctrl") || z.Name == "Control" ? m.stoneDark : m.path;
                if (Mathf.Approximately(z.Y0, z.Y1)) Block($"Floor_{z.Name}", new Vector3(z.X0, z.Y0 - 0.6f, z.Z0), new Vector3(z.X1, z.Y0, z.Z1), mat, world);
                else SlopeSlab($"Floor_{z.Name}", z.X0, z.Z0, z.X1, z.Z1, z.Y0, z.Y1, z.Axis, mat, world);
            }
            // the platform: a broad round deck on its natural rise, with a pale rim
            MeshObject("PlatformDisc", SaveMesh(Lathe(new[] { new Vector2(0f, 0.01f), new Vector2(15f, 0.01f), new Vector2(15.4f, -0.2f), new Vector2(14.5f, -2.5f), new Vector2(0f, -2.5f) }, 0f, 360f, 64), "Venus_PlatformDisc"), m.stone, world);
            BuildVenusTerrain(world, m);
            var gv = new List<Vector3>(); var gt = new List<int>(); var dv = new List<Vector3>(); var dt = new List<int>(); var pv = new List<Vector3>(); var pt = new List<int>(); var trv = new List<Vector3>(); var trt = new List<int>();
            bool Built(float x, float z) { foreach (var zz2 in V.Zones) if (!V.NaturalZone(zz2.Name) && x > zz2.X0 - 0.5f && x < zz2.X1 + 0.5f && z > zz2.Z0 - 0.5f && z < zz2.Z1 + 0.5f) return true; return x * x + z * z < 15.8f * 15.8f; }
            bool InGully(float z) => z > VenusTerrain.GullyZ0 - 0.8f && z < VenusTerrain.GullyZ1 + 0.8f;
            // trees and shrubs on the hillsides and their shelves (never on the path or the valley floor's middle)
            for (int i = 0; i < 520; i++)
            {
                float x = VenusTerrain.GridArea.xMin + (float)rnd.NextDouble() * VenusTerrain.GridArea.width, z = VenusTerrain.GridArea.yMin + (float)rnd.NextDouble() * VenusTerrain.GridArea.height;
                VenusTerrain.OnRoute(x, z, out float dP, out _, out _, out _);
                if (Built(x, z) || InGully(z) || dP < VenusTerrain.Floor + 1.5f) continue;
                var at = new Vector3(x, VenusTerrain.Height(x, z) - 0.15f, z);
                if (dP < 45f && rnd.NextDouble() < 0.28) Tree(trv, trt, rnd, at, 4.5f + (float)rnd.NextDouble() * 5.5f);
                else if (dP < 30f) Shrub(gv, gt, rnd, at, 0.9f + (float)rnd.NextDouble() * 1.6f);
            }
            // a few shrubs at the valley floor's edges (plants border the path, no kerbs)
            {
                float L = VenusTerrain.RouteLength;
                for (float sP = 18f; sP < L - 6f; sP += 4.2f)
                {
                    var c = VenusTerrain.AtS(sP); var dd = VenusTerrain.DirAtS(sP); var rgt = new Vector3(dd.z, 0f, -dd.x);
                    if (InGully(c.z) || c.z > 98f && c.z < 132f) continue;
                    foreach (var side in new[] { -1f, 1f })
                    {
                        if (rnd.NextDouble() < 0.45) continue;
                        float lat = side * (VenusTerrain.Floor - 0.2f + (float)rnd.NextDouble() * 1.6f);
                        var q = c + rgt * lat; q.y = VenusTerrain.Height(q.x, q.z) - 0.1f;
                        if (!Built(q.x, q.z)) Shrub(gv, gt, rnd, q, 0.7f + (float)rnd.NextDouble() * 1.1f);
                    }
                }
                // lamps and marker stones along the path (the way to go), and grown arches with hanging gardens over it
                int li = 0;
                for (float sP = 26f; sP < L - 10f; sP += 27f)
                {
                    var c = VenusTerrain.AtS(sP); var dd = VenusTerrain.DirAtS(sP); var rgt = new Vector3(dd.z, 0f, -dd.x);
                    if (InGully(c.z)) continue;
                    var q = c - rgt * (VenusTerrain.PathHalf + 1.1f); q.y = VenusTerrain.Height(q.x, q.z);
                    Lump(dv, dt, q + Vector3.up * 0.55f, new Vector3(0.35f, 1.2f, 0.35f), 0.25f, rnd.Next(1000));              // a grown post
                    Lamp($"PathLamp_{li++}", q + Vector3.up * 1.45f, 7f, 0.9f, m.glow, world, new Vector3(0.18f, 0.18f, 0.18f));
                    var st2 = c + rgt * (VenusTerrain.PathHalf + 0.6f) + dd * 6f; st2.y = VenusTerrain.Height(st2.x, st2.z);
                    Lump(gv, gt, st2 + Vector3.up * 0.15f, new Vector3(0.55f, 0.35f, 0.45f), 0.5f, rnd.Next(1000));
                }
                foreach (var sA in new[] { 46f, 98f, 160f })
                {
                    var c = VenusTerrain.AtS(sA); var dd = VenusTerrain.DirAtS(sA); var rgt = new Vector3(dd.z, 0f, -dd.x);
                    var arc = new List<Vector3>();
                    for (int i = 0; i <= 16; i++) { float an = i / 16f * Mathf.PI; var q = c + rgt * (-Mathf.Cos(an) * 7.4f); q.y = VenusTerrain.Height(q.x, q.z) - 0.3f + Mathf.Sin(an) * 9.3f + (i == 0 || i == 16 ? 0f : 0.3f); arc.Add(q); }
                    Sweep(pv, pt, arc, 0.55f, false, 8);
                    for (int i = 2; i < 15; i += 2) for (int k = 0; k < 3; k++) Lump(dv, dt, arc[i] + new Vector3(0f, -0.9f - k * 0.9f, 0f) + dd * ((float)rnd.NextDouble() - 0.5f), new Vector3(0.6f, 0.8f, 0.6f), 0.4f, rnd.Next(1000));
                }
            }
            MeshObject("Gardens_Shrubs", SaveMesh(SmoothFinish(gv, gt), "Venus_Shrubs"), m.leaf, world);
            MeshObject("Gardens_Trees", SaveMesh(SmoothFinish(trv, trt), "Venus_Trees"), m.leafDark, world);
            MeshObject("Gardens_HangingArches", SaveMesh(SmoothFinish(pv, pt), "Venus_Arches"), m.stone, world);
            MeshObject("Gardens_HangingPlants", SaveMesh(SmoothFinish(dv, dt), "Venus_Hanging"), m.leafPale, world);
            // the grass (item 3) on the new ground: dense along the valley floor's verges and the shelves near the path,
            // thinner up the hills, round the landing rise - never on the worn path, the paving, the water or the gully
            var grass = new List<GrassPatch>();
            float PathEdge(float x, float z) => VenusTerrain.PathHalf + 0.5f * VenusTerrain.Noise(x * 0.7f, z * 0.7f);
            float GrassAt(float x, float z, float dMin, float dMax)
            {
                if (Built(x, z) || InGully(z)) return float.NaN;
                VenusTerrain.OnRoute(x, z, out float dP, out _, out float lat, out float rz);
                if (dP < PathEdge(x, z) || dP < dMin || dP > dMax) return float.NaN;
                if (rz > 22f && rz < 117f && Mathf.Abs(lat - VenusTerrain.StreamLat) < 1.1f) return float.NaN;     // the stream
                if (x >= 14.8f && x <= 23.6f && z >= 103.8f && z <= 116.2f) return 1.0f;                            // the shelf top
                return VenusTerrain.Height(x, z);
            }
            var ga = VenusTerrain.GridArea;
            grass.Add(new GrassPatch { Name = "valley verges", Area = ga, Density = 7.5f, MinH = 0.35f, MaxH = 0.9f, Height = (x, z) => GrassAt(x, z, 0f, 17f) });
            grass.Add(new GrassPatch { Name = "hillsides", Area = ga, Density = 2.2f, MinH = 0.45f, MaxH = 1.1f, Height = (x, z) => GrassAt(x, z, 17f, 58f) });
            grass.Add(new GrassPatch { Name = "landing rise", Area = new Rect(-55f, -55f, 110f, 80f), Density = 2.4f, MinH = 0.45f, MaxH = 1.05f,
                Height = (x, z) => { float r = Mathf.Sqrt(x * x + z * z); return r > 15.8f && r < 55f && z < 25f ? GrassAt(x, z, 0f, 999f) : float.NaN; } });
            BuildGrass(world, grass);

            // ---- the gully (VEN_B03): the stream has undercut the path and the ground has fallen in - rubble at the bottom
            var rv = new List<Vector3>(); var rt = new List<int>();
            for (int i = 0; i < 40; i++) { float x = 14f + (float)rnd.NextDouble() * 32f, z = V.CollapseZ0 + 1.5f + (float)rnd.NextDouble() * 7f; Lump(rv, rt, new Vector3(x, VenusTerrain.Height(x, z) + 0.2f, z), Vector3.one * (0.5f + (float)rnd.NextDouble() * 1.3f), 0.4f, rnd.Next(1000)); }
            for (int i = 0; i < 9; i++) Lump(rv, rt, new Vector3(25.5f + i * 1.1f, -0.25f, V.CollapseZ1 + 0.3f), new Vector3(0.6f, 0.35f, 0.45f), 0.5f, rnd.Next(1000));   // the broken far lip
            MeshObject("Collapse_Rubble", SaveMesh(SmoothFinish(rv, rt), "Venus_CollapseRubble"), m.stoneDark, world);
            // Round 13 (0k): the walkway before the tremor - the path still spans the undercut (paving slabs on the crust over
            // the gully). The tremor breaks it in front of her: the slabs crack and drop into the gully (VenusDirector.Collapse).
            var walkway = new GameObject("WalkwayIntact").transform; walkway.SetParent(world, false);
            {
                var wr = new System.Random(31);
                int n = 0;
                for (float z = V.CollapseZ0 - 0.2f; z < V.CollapseZ1 + 0.2f; z += 2.1f)
                    for (float x = 26.6f; x < 33.4f; x += 2.3f)
                    {
                        var piece = new GameObject($"Slab{n++}").transform; piece.SetParent(walkway, false);
                        piece.position = new Vector3(x + 1.1f, -0.2f, z + 1.0f);
                        var wpv = new List<Vector3>(); var wpt = new List<int>();
                        AddBox(wpv, wpt, Vector3.zero, new Vector3(2.22f - (float)wr.NextDouble() * 0.12f, 0.4f, 2.04f - (float)wr.NextDouble() * 0.1f));
                        MeshObject("Body", SaveMesh(Finish(wpv, wpt), $"Venus_WalkSlab{n}"), (n % 3 == 0) ? m.stoneMid : m.stone, piece);
                    }
                // the crust under it (falls with it)
                var wcv = new List<Vector3>(); var wct = new List<int>();
                for (int i = 0; i < 18; i++) { float x = 26f + (float)wr.NextDouble() * 8f, z = V.CollapseZ0 + (float)wr.NextDouble() * 10f; Lump(wcv, wct, new Vector3(x, -0.9f, z), new Vector3(1.4f, 0.8f, 1.2f), 0.4f, wr.Next(1000)); }
                MeshObject("Crust", SaveMesh(SmoothFinish(wcv, wct), "Venus_WalkCrust"), m.stoneDark, walkway, false);
            }
            // the garden-wall shelf beside the stopping area: a rock shelf (climbable anywhere along its face, for exploring)
            Block("Detour_Wall", new Vector3(15f, -0.8f, 104f), new Vector3(V.DetourMantle.FaceAt, 1.0f, 116f), m.stoneMid, world);
            Lamp("Detour_Lamp", new Vector3(19.2f, 2.6f, 110f), 7f, 1.0f, m.glow, world, new Vector3(0.18f, 0.18f, 0.18f));
            // QF10: the ground the fallen slab rests on (not walkable: the slab fills it), and the slab itself - a long broken
            // paving slab, the right length for the gap, its edges catching the light (telekinesis lays it across)
            var slab = new GameObject("FallenSlab").transform; slab.SetParent(world, false);
            slab.SetPositionAndRotation(new Vector3(30.4f, VenusTerrain.Height(30.4f, 122.8f) + 0.55f, 122.8f), Quaternion.Euler(4f, 97f, 9f));   // QF12 (5): in the gully (VenusDirector.SlabRest); hidden until the quake breaks it off the walkway
            {
                var sv = new List<Vector3>(); var st = new List<int>();
                AddBox(sv, st, Vector3.zero, new Vector3(V.SlabSize.z, V.SlabSize.y, V.SlabSize.x));          // local: length along z
                MeshObject("Body", SaveMesh(Finish(sv, st), "Venus_FallenSlab"), m.stone, slab);
                // broken ends: a few chipped lumps; a thin pale lip along the top edges (the telekinesis highlight glows here)
                var ev = new List<Vector3>(); var et = new List<int>();
                float hx = V.SlabSize.z * 0.5f, hy = V.SlabSize.y * 0.5f, hz = V.SlabSize.x * 0.5f;
                foreach (var sx in new[] { -hx, hx }) Sweep(ev, et, new[] { new Vector3(sx, hy, -hz), new Vector3(sx, hy, hz) }, 0.03f, false, 6);
                foreach (var sz in new[] { -hz, hz }) Sweep(ev, et, new[] { new Vector3(-hx, hy, sz), new Vector3(hx, hy, sz) }, 0.03f, false, 6);
                var edge = MeshObject("Edges", SaveMesh(SmoothFinish(ev, et), "Venus_FallenSlabEdges"), m.stone, slab, false);
                var rv2 = new List<Vector3>(); var rt2 = new List<int>(); var rr = new System.Random(5);
                for (int i = 0; i < 6; i++) Lump(rv2, rt2, new Vector3((i % 2 == 0 ? -1 : 1) * hx * 0.6f, -hy * 0.2f, (i < 3 ? -hz : hz) + (i < 3 ? 0.2f : -0.2f)), new Vector3(0.5f, 0.35f, 0.4f), 0.5f, rr.Next(1000));
                MeshObject("Chips", SaveMesh(SmoothFinish(rv2, rt2), "Venus_FallenSlabChips"), m.stoneMid, slab);
            }
            slab.gameObject.SetActive(false);
            // Round 13 (0d): the stone aprons at both ends of the laid slab - ramps from the ground up to its top (no lip);
            // shown when the bridge is down
            var ramps = new GameObject("BridgeRamps"); ramps.transform.SetParent(world, false);
            {
                float halfW = 3.8f, sEnd = V.SlabBridge.z - V.SlabSize.x * 0.5f, nEnd = V.SlabBridge.z + V.SlabSize.x * 0.5f;
                void Apron(string n, float zFrom, float zTo)          // top from BridgeTop at zFrom down to the ground at zTo
                {
                    var rv = new List<Vector3>(); var rt = new List<int>();
                    int N = 8;
                    for (int i = 0; i < N; i++)
                    {
                        float z0 = Mathf.Lerp(zFrom, zTo, i / (float)N), z1 = Mathf.Lerp(zFrom, zTo, (i + 1) / (float)N);
                        float y0 = Mathf.Lerp(V.BridgeTop, 0.01f, i / (float)N), y1 = Mathf.Lerp(V.BridgeTop, 0.01f, (i + 1) / (float)N);
                        float g0 = Mathf.Min(0f, Seven.VenusTerrain.Height(V.SlabBridge.x, z0)), g1 = Mathf.Min(0f, Seven.VenusTerrain.Height(V.SlabBridge.x, z1));
                        y0 = Mathf.Max(y0, g0 + 0.01f); y1 = Mathf.Max(y1, g1 + 0.01f);
                        var a = new Vector3(V.SlabBridge.x - halfW, y0, z0); var b = new Vector3(V.SlabBridge.x + halfW, y0, z0);
                        var c = new Vector3(V.SlabBridge.x + halfW, y1, z1); var d2 = new Vector3(V.SlabBridge.x - halfW, y1, z1);
                        if (zTo > zFrom) Quad(rv, rt, a, d2, c, b); else Quad(rv, rt, a, b, c, d2);
                        Quad(rv, rt, a + Vector3.down * 0.5f, a, d2, d2 + Vector3.down * 0.5f); Quad(rv, rt, b, b + Vector3.down * 0.5f, c + Vector3.down * 0.5f, c);
                    }
                    MeshObject(n, SaveMesh(Finish(rv, rt), "Venus_" + n), m.stone, ramps.transform);
                }
                Apron("BridgeRampS", sEnd + 0.3f, 112.4f);
                Apron("BridgeRampN", nEnd - 0.3f, 132.6f);
            }
            ramps.SetActive(false);
            // dust for the landing thud
            var slabDust = new GameObject("SlabDust").AddComponent<ParticleSystem>(); slabDust.transform.position = V.SlabBridge + Vector3.up * 0.2f;
            { var mn = slabDust.main; mn.playOnAwake = false; mn.loop = false; mn.duration = 1.2f; mn.startColor = new Color(0.75f, 0.75f, 0.75f, 0.45f); mn.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.6f); mn.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.8f); mn.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 2.2f); mn.gravityModifier = -0.02f;
              var shp = slabDust.shape; shp.shapeType = ParticleSystemShapeType.Box; shp.scale = new Vector3(4.8f, 0.2f, 12f);
              var em2 = slabDust.emission; em2.rateOverTime = 0f; em2.SetBursts(new[] { new ParticleSystem.Burst(0f, 90) });
              slabDust.GetComponent<ParticleSystemRenderer>().sharedMaterial = AdditiveMatTex("Venus_dust", 0.35f); slabDust.Stop(); }
            // telekinesis motes (her hand -> the slab)
            var tkMotes = new GameObject("TkMotes").AddComponent<ParticleSystem>();
            { var mn = tkMotes.main; mn.playOnAwake = false; mn.loop = true; mn.startColor = new Color(1f, 1f, 1f, 0.8f); mn.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f); mn.startLifetime = 0.5f; mn.startSpeed = 0f; mn.simulationSpace = ParticleSystemSimulationSpace.World;
              var em3 = tkMotes.emission; em3.rateOverTime = 0f; var sh3 = tkMotes.shape; sh3.shapeType = ParticleSystemShapeType.Sphere; sh3.radius = 0.08f;
              tkMotes.GetComponent<ParticleSystemRenderer>().sharedMaterial = AdditiveMatTex("Venus_tk", 1.2f); tkMotes.Stop(); }

            // ---- VEN_03: the temple - grown rather than built; everything at 3.5 m-Elarim scale
            float tz = V.DoorZ;
            Block("Temple_FrontW", new Vector3(14f, 0f, tz - 0.8f), new Vector3(30f - V.DoorHalf, 18f, tz), m.stone, world);
            Block("Temple_FrontE", new Vector3(30f + V.DoorHalf, 0f, tz - 0.8f), new Vector3(46f, 18f, tz), m.stone, world);
            Block("Temple_FrontTop", new Vector3(30f - V.DoorHalf, V.DoorH, tz - 0.8f), new Vector3(30f + V.DoorHalf, 18f, tz), m.stone, world);
            Block("Temple_HallW", new Vector3(21f, 0f, tz), new Vector3(22f, 14f, 262f), m.stoneMid, world);
            Block("Temple_HallE", new Vector3(38f, 0f, tz), new Vector3(39f, 14f, 262f), m.stoneMid, world);
            Block("Temple_HallBackW", new Vector3(21f, 0f, 262f), new Vector3(25f, 14f, 263f), m.stoneMid, world);
            Block("Temple_HallBackE", new Vector3(35f, 0f, 262f), new Vector3(39f, 14f, 263f), m.stoneMid, world);
            Block("Temple_HallRoof", new Vector3(20f, 14f, tz - 1f), new Vector3(40f, 15f, 263f), m.stone, world);
            Block("Temple_CubeW", new Vector3(24f, 0f, 262f), new Vector3(25f, 10f, 275f), m.stone, world);
            Block("Temple_CubeE", new Vector3(35f, 0f, 262f), new Vector3(36f, 10f, 275f), m.stone, world);
            Block("Temple_CubeBack", new Vector3(24f, 0f, 274f), new Vector3(36f, 10f, 275f), m.stone, world);
            Block("Temple_CubeRoof", new Vector3(24f, 10f, 262f), new Vector3(36f, 10.8f, 275f), m.stone, world);
            // organic ribs growing up the hall (Elarim architecture: like roots and stems)
            var tv = new List<Vector3>(); var tt = new List<int>();
            for (float z = tz + 3f; z < 262f; z += 5f)
                foreach (var x in new[] { 22.3f, 37.7f })
                {
                    float ix = x < 30f ? 1f : -1f;
                    Tube(tv, tt, new[] { new Vector3(x, 0f, z), new Vector3(x + ix * 0.6f, 5f, z + 0.5f), new Vector3(x + ix * 3f, 11f, z + 1f), new Vector3(30f, 13.6f, z + 1.2f) }, 0.55f, 0.25f);
                }
            MeshObject("Temple_GrownRibs", SaveMesh(SmoothFinish(tv, tt), "Venus_TempleRibs"), m.stone, world);
            // the protected structure around the original cube: a cage of pale stems, the cube inside on its pedestal
            var cv = new List<Vector3>(); var ct = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                var b = V.CubeAt + new Vector3(Mathf.Cos(a) * 1.5f, -V.CubeAt.y, Mathf.Sin(a) * 1.5f);
                Tube(cv, ct, new[] { b, b + new Vector3(-Mathf.Cos(a) * 0.3f, 1.6f, -Mathf.Sin(a) * 0.3f), b + new Vector3(-Mathf.Cos(a) * 1.2f, 3.1f, -Mathf.Sin(a) * 1.2f), V.CubeAt + Vector3.up * 2.2f }, 0.12f, 0.06f);
            }
            var cage = MeshObject("Cube_ProtectedStructure", SaveMesh(SmoothFinish(cv, ct), "Venus_CubeCage"), m.stone, world);
            MeshObject("Cube_Pedestal", SaveMesh(Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.7f, 0f), new Vector2(0.5f, 0.2f), new Vector2(0.32f, V.CubeAt.y - 0.42f), new Vector2(0.55f, V.CubeAt.y - 0.32f), new Vector2(0f, V.CubeAt.y - 0.32f) }, 0f, 360f, 24, null, new Vector3(V.CubeAt.x, 0f, V.CubeAt.z)), "Venus_CubePedestal"), m.stoneMid, world);
            var em = ElarimMaterials(false);
            var cube = BuildVenusCube(world, m, em);
            // VEN_03 court + garden: planters, trees, a fountain basin
            var fv = new List<Vector3>(); var ft = new List<int>();
            for (int i = 0; i < 14; i++) Tree(fv, ft, rnd, new Vector3((i % 4 < 2 ? 15f : 39.5f) + (float)rnd.NextDouble() * 6f, 0f, 205f + (float)rnd.NextDouble() * 8f + (i % 2) * 18f), 7f + (float)rnd.NextDouble() * 5f);   // QF10: clear of the meeting line
            MeshObject("Court_Trees", SaveMesh(SmoothFinish(fv, ft), "Venus_CourtTrees"), m.leafDark, world);
            MeshObject("Court_Fountain", SaveMesh(Lathe(new[] { new Vector2(0f, 0.4f), new Vector2(2.6f, 0.4f), new Vector2(2.8f, 0.7f), new Vector2(3.0f, 0f), new Vector2(0f, 0f) }, 0f, 360f, 32, null, new Vector3(22f, 0f, 200f)), "Venus_Fountain"), m.stone, world);
            Block("Court_FountainWater", new Vector3(19.8f, 0.42f, 197.8f), new Vector3(24.2f, 0.5f, 202.2f), m.water, world, false);

            // ---- VEN_04: the maintenance path gate (shut until C04), observation railing, the basin, the root gate
            var maintGate = Block("Maint_Gate", new Vector3(44.2f, 0f, 195f), new Vector3(44.8f, 2.4f, 203f), m.stoneDark, world);
            Block("Obs_Rail", new Vector3(87.6f, -6f, 134f), new Vector3(88f, -5f, 152f), m.stone, world);
            var bv = new List<Vector3>(); var bt = new List<int>();
            for (int i = 0; i < 30; i++) Lump(bv, bt, new Vector3(92f + (float)rnd.NextDouble() * 50f, V.BasinCentre.y, 105f + (float)rnd.NextDouble() * 70f), new Vector3(1.5f, 0.6f, 1.5f) * (0.6f + (float)rnd.NextDouble()), 0.5f, rnd.Next(1000));
            MeshObject("Basin_Mounds", SaveMesh(SmoothFinish(bv, bt), "Venus_BasinMounds"), m.stoneDark, world);
            // distant towers (tall, pale, some unfinished) and the cloud ceiling
            var wv = new List<Vector3>(); var wt = new List<int>();
            var towers = new[] { (new Vector3(-80f, -20f, 330f), 140f, false), (new Vector3(120f, -20f, 300f), 110f, true), (new Vector3(-150f, -20f, 120f), 90f, false), (new Vector3(30f, -20f, 420f), 170f, false), (new Vector3(200f, -20f, 200f), 120f, true), (new Vector3(-40f, -20f, -140f), 80f, true) };
            foreach (var (at, h, unfinished) in towers)
            {
                var tw = Lathe(new[] { new Vector2(0f, 0f), new Vector2(9f, 0f), new Vector2(6f, h * 0.3f), new Vector2(4.5f, h * 0.8f), new Vector2(unfinished ? 4.3f : 2f, h), new Vector2(0f, unfinished ? h : h * 1.08f) }, 0f, unfinished ? 300f : 360f, 24, null, at);
                MeshObject($"Tower_{at.x}_{at.z}", SaveMesh(tw, $"Venus_Tower{at.x}_{at.z}"), m.tower, world, false);
            }
            var cloud = Block("CloudCeiling", new Vector3(-600f, 160f, -600f), new Vector3(700f, 162f, 900f), m.cloud, world, false);
            cloud.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            // ---- the saucer (hovering low), its tractor beam, the storage hatch
            // Round 12 (2f): the one shared saucer exterior (Chapter 1's look: the 12 m silver disc, rim glow, dome, belly
            // glow) with the storage hatch toward the bike's spot; its root on the ground, the underside SaucerHover above it
            // QF13 (16): the ONE saucer prefab, unchanged (exterior, beam, bike hold); it hovers (underside 3.35 m), so nothing
            // under it blocks her: no ground obstacle for the hull. No ramp, ever - the beam for her, the hold for the bike.
            var saucer = PlaceSaucer(world, V.SaucerAt);
            var hatch = saucer.Find("HatchPanel");
            var beamC = saucer.GetComponent<Seven.SaucerBeam>();
            CheckSaucer("Venus", saucer);
            var pads = new List<Vector3>();

            // ---- ground
            var wg = new GameObject("WorldGround").AddComponent<Seven.WorldGround>();
            wg.Shape = Seven.Ground.Shape.Zones;
            wg.Zones = V.Zones.Select(z => new Seven.WorldGround.ZoneDef { Name = z.Name, Min = new Vector2(z.X0, z.Z0), Max = new Vector2(z.X1, z.Z1), Y0 = z.Y0, Y1 = z.Y1, Axis = z.Axis, Enabled = z.On }).ToArray();
            pads.Add(new Vector3(22f, 200f, 3.2f));             // the fountain
            pads.Add(new Vector3(V.CubeAt.x, V.CubeAt.z, 1.7f));   // the protected structure
            wg.ZoneObstacles = pads.ToArray();

            // ---- 7, Panther, the camera, the bike
            var seven = Spawn(SevenDir + "/7_character.fbx", "7", V.StepOut, Vector3.forward, sevenCtl);
            var catRoot = SpawnCat("Cat", V.StepOut + new Vector3(-1f, 0f, 0f), Vector3.forward, catCtl, catFur, catWhisker, out var catAnim);
            var camGo = new GameObject("ChapterCamera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.86f, 0.86f, 0.86f);
            cam.fieldOfView = 52f; cam.nearClipPlane = 0.08f; cam.farClipPlane = 3000f;
            camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData(); camData.renderPostProcessing = true; camData.requiresDepthTexture = true; camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camGo.transform.position = V.StepOut + new Vector3(0f, 1.8f, -2.5f);
            var dirGo = new GameObject("VenusDirector");
            var input = dirGo.AddComponent<Seven.ChapterInput>();
            var s7 = seven.AddComponent<Seven.ChapterSeven>(); s7.Input = input; s7.CameraTransform = camGo.transform;
            var ccam = camGo.AddComponent<Seven.ChapterCamera>(); ccam.Target = seven.transform; ccam.Input = input;
            var cat = catRoot.AddComponent<Seven.ChapterCat>(); cat.Seven = seven.transform; cat.Anim = catAnim; cat.Cam = cam;

            var bikeRoot = new GameObject("HoverBike").transform; bikeRoot.position = V.BikeUnpacked;
            var visual = new GameObject("Visual").transform; visual.SetParent(bikeRoot, false);
            if (bike != null)
            {
                var bm = (GameObject)PrefabUtility.InstantiatePrefab(bike); bm.name = "BikeModel"; bm.transform.SetParent(visual, false);
                var bmat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Venus_hoverbike_v1.mat");
                foreach (var r in bm.GetComponentsInChildren<Renderer>()) r.sharedMaterial = bmat;
            }
            else Block("BikePlaceholder", new Vector3(-0.45f, 0f, -1.15f), new Vector3(0.45f, 0.7f, 1.15f), m.hull, visual, false);
            // QF10: the bike is 3 m long now; the saddle top, the two grips and a foot point on each side panel come from the
            // Blender export (Art/Export/hoverbike/hoverbike_points.json); 7's hips sit on the saddle (ChapterSevenRide calibrates)
            var bp = LoadBikePoints();
            var seat = new GameObject("Seat").transform; seat.SetParent(visual, false); seat.localPosition = bp.saddle;
            var gl = new GameObject("GripL").transform; gl.SetParent(visual, false); gl.localPosition = bp.gripL;
            var gr = new GameObject("GripR").transform; gr.SetParent(visual, false); gr.localPosition = bp.gripR;
            var footL = new GameObject("FootL").transform; footL.SetParent(visual, false); footL.localPosition = bp.footL;
            var footR = new GameObject("FootR").transform; footR.SetParent(visual, false); footR.localPosition = bp.footR;
            var fair = new GameObject("FairingL").transform; fair.SetParent(visual, false); fair.localPosition = new Vector3(bp.gripL.x - 0.05f, bp.gripL.y + 0.04f, bp.gripL.z - 0.25f);   // B01: where her hand catches it
            var vent = MeshObject("VentGlow", SaveMesh(Lathe(new[] { new Vector2(0f, 0.01f), new Vector2(0.36f, 0.01f), new Vector2(0f, 0.012f) }, 0f, 360f, 20), "Venus_BikeVent"), m.bikeVent, visual, false);
            var vl = new GameObject("VentLight").AddComponent<Light>(); vl.transform.SetParent(visual, false); vl.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            vl.type = LightType.Point; vl.range = 2.2f; vl.intensity = 0.4f; vl.color = Color.white; vl.shadows = LightShadows.None;
            var hb = bikeRoot.gameObject.AddComponent<Seven.HoverBike>();
            hb.Seat = seat; hb.GripL = gl; hb.GripR = gr; hb.FootL = footL; hb.FootR = footR; hb.Fairing = fair; hb.Visual = visual; hb.VentLight = vl; hb.VentGlow = vent.GetComponent<Renderer>();
            hb.Length = bp.length;

            // ---- the Elarim: the guardian (at the temple entrance garden), the attendant (comes in C04), workers
            Seven.ElarimNpc MakeElarim(string name, Vector3 at, Vector3 facing, string post, bool withSword = false)
            {
                GameObject go;
                if (elarim != null)
                {
                    go = new GameObject(name); go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(facing));
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(elarim); model.name = "ElarimModel"; model.transform.SetParent(go.transform, false);
                    var anim = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
                    anim.runtimeAnimatorController = elarimCtl; anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Elarim_armour.mat");
                    foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>()) { smr.sharedMaterial = mat; smr.updateWhenOffscreen = true; }
                    go.AddComponent<Seven.ElarimCape>();          // QF10: the legs never pass through the cape
                    if (withSword && sword != null)
                    {
                        var hand = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.RightHand) : null;
                        var sw = (GameObject)PrefabUtility.InstantiatePrefab(sword); sw.name = "Sword";
                        sw.transform.SetParent(hand != null ? hand : go.transform, false);
                        sw.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); sw.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                        var smat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Venus_elarim_sword_v1.mat"); foreach (var r in sw.GetComponentsInChildren<Renderer>()) r.sharedMaterial = smat;
                    }
                }
                else
                {
                    // placeholder (only if the model failed): a 3.5 m armoured figure
                    go = new GameObject(name + "_placeholder"); go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(facing));
                    Block("Body", at + new Vector3(-0.5f, 0f, -0.3f), at + new Vector3(0.5f, 3.5f, 0.3f), m.tower, go.transform, false);
                    Log("WARNING: Elarim model missing - Elarim_placeholder used");
                }
                var npc = go.AddComponent<Seven.ElarimNpc>(); npc.PostClip = post; npc.EyeMaterial = m.glow;
                return npc;
            }
            Vector3 OnGround(float x, float z) => new Vector3(x, VenusTerrain.Height(x, z), z);
            var guardian = MakeElarim("Elarim_Guardian", V.GuardianSpot, Vector3.back, "idle");
            var attendant = MakeElarim("Elarim_Attendant", new Vector3(30f, 0f, 250f), Vector3.back, "idle"); attendant.gameObject.SetActive(false);
            var workers = new List<Seven.ElarimNpc>
            {
                MakeElarim("Elarim_Worker_Vessel", OnGround(37.6f, 104.5f), Vector3.left, "carry"),
                MakeElarim("Elarim_Worker_Road", OnGround(32.4f, 84f), Vector3.left, "lift"),
                MakeElarim("Elarim_Worker_Court", new Vector3(17f, 0f, 196f), Vector3.right, "sword_idle", true),
                MakeElarim("Elarim_Worker_Channel", OnGround(9.9f, 32f), Vector3.left, "lift"),
            };
            // C02's vessel (water shakes in it; the worker steadies it)
            var vessel = MeshObject("Vessel", SaveMesh(Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.35f, 0f), new Vector2(0.45f, 0.45f), new Vector2(0.38f, 0.8f), new Vector2(0.34f, 0.8f), new Vector2(0.4f, 0.45f), new Vector2(0.3f, 0.05f), new Vector2(0f, 0.05f) }, 0f, 360f, 24, null, V.VesselAt - Vector3.up * 0.9f), "Venus_Vessel"), m.stone, world).transform;
            var water = MeshObject("VesselWater", SaveMesh(Lathe(new[] { new Vector2(0f, 0.68f), new Vector2(0.37f, 0.68f), new Vector2(0f, 0.681f) }, 0f, 360f, 20, null, V.VesselAt - Vector3.up * 0.9f), "Venus_VesselWater"), m.water, world, false).transform;
            Block("Vessel_Stand", V.VesselAt + new Vector3(-0.5f, -0.95f, -0.5f), V.VesselAt + new Vector3(0.5f, -0.9f, 0.5f), m.stoneDark, world);
            // the burrowing creatures in the basin
            var worms = new List<Seven.WormCreature>();
            for (int i = 0; i < 4; i++)
            {
                var root = new GameObject($"Creature{i}");
                var c0 = V.BasinCentre + new Vector3(Mathf.Cos(i * 1.7f) * 14f, 0f, Mathf.Sin(i * 1.7f) * 16f);
                root.transform.position = c0;
                if (worm != null)
                {
                    var wm = (GameObject)PrefabUtility.InstantiatePrefab(worm); wm.name = "WormModel"; wm.transform.SetParent(root.transform, false);
                    var an = wm.GetComponent<Animator>() ?? wm.AddComponent<Animator>(); an.runtimeAnimatorController = wormCtl; an.applyRootMotion = false;
                    var wmat = TexturedMat("Venus_worm", CharRoot + "/worm/Textures/worm_v1_basecolor.jpg", 0.35f);
                    foreach (var r in wm.GetComponentsInChildren<Renderer>()) r.sharedMaterial = wmat;
                }
                var w = root.AddComponent<Seven.WormCreature>();
                w.Route = Enumerable.Range(0, 5).Select(k => V.BasinCentre + new Vector3(Mathf.Cos(i + k * 1.3f) * 18f, 0f, Mathf.Sin(i + k * 1.3f) * 22f)).ToArray();
                var dust = new GameObject("Dust").AddComponent<ParticleSystem>(); dust.transform.SetParent(root.transform, false);
                var main = dust.main; main.startColor = new Color(0.55f, 0.55f, 0.55f, 0.5f); main.startSize = 2.5f; main.startLifetime = 2.5f; main.startSpeed = 1.2f;
                var emi = dust.emission; emi.rateOverTime = 14f;
                dust.GetComponent<ParticleSystemRenderer>().sharedMaterial = AdditiveMatTex("Venus_dust", 0.35f);
                w.Dust = dust;
                worms.Add(w);
            }
            // flyers high over the platform (C01)
            var flyers = new List<Transform>();
            for (int i = 0; i < 5; i++)
            {
                var fv2 = new List<Vector3> { new Vector3(0f, 0f, 0.6f), new Vector3(-1.4f, 0.1f, -0.4f), new Vector3(1.4f, 0.1f, -0.4f) }; var ft2 = new List<int> { 0, 1, 2, 0, 2, 1 };
                var f = MeshObject($"Flyer{i}", SaveMesh(Finish(fv2, ft2), $"Venus_Flyer{i}"), m.tower, world, false);
                var fl = f.AddComponent<Seven.VenusFlyer>(); fl.Centre = new Vector3(0f, 0f, 30f); fl.Radius = 24f + i * 6f; fl.Height = 30f + i * 5f; fl.Phase = i * 1.3f; fl.Speed = 0.08f + i * 0.01f;
                flyers.Add(f.transform);
            }
            // Round 13 (0i): small birds over the gardens - lower, faster, flapping; a few loose flocks along the road
            {
                var br = new System.Random(77); int bi = 0;
                foreach (var (cx, cz, n) in new[] { (8f, 40f, 4), (24f, 95f, 3), (36f, 150f, 4), (20f, 205f, 3), (-10f, 10f, 3) })
                    for (int i = 0; i < n; i++)
                    {
                        var bdv = new List<Vector3> { new Vector3(0f, 0f, 0.18f), new Vector3(-0.32f, 0.02f, -0.12f), new Vector3(0.32f, 0.02f, -0.12f), new Vector3(0f, 0.01f, -0.22f) };
                        var bdt = new List<int> { 0, 1, 3, 0, 3, 2, 0, 3, 1, 0, 2, 3 };
                        var b = MeshObject($"Bird{bi}", SaveMesh(Finish(bdv, bdt), $"Venus_Bird{bi}"), m.tower, world, false); bi++;
                        b.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                        var fl = b.AddComponent<Seven.VenusFlyer>(); fl.Centre = new Vector3(cx, 0f, cz); fl.Radius = 9f + (float)br.NextDouble() * 14f; fl.Height = 8f + (float)br.NextDouble() * 14f;
                        fl.Phase = (float)br.NextDouble() * 6.28f; fl.Speed = (0.18f + (float)br.NextDouble() * 0.12f) * (br.Next(2) == 0 ? 1f : -1f); fl.Flap = 2.2f + (float)br.NextDouble() * 1.4f;
                    }
            }
            // C04: dust falling from the cube chamber ceiling
            var cdust = new GameObject("CeilingDust").AddComponent<ParticleSystem>(); cdust.transform.position = V.CubeAt + Vector3.up * 8.5f;
            { var mn = cdust.main; mn.playOnAwake = false; mn.loop = false; mn.duration = 3f; mn.startColor = new Color(0.8f, 0.8f, 0.8f, 0.6f); mn.startSize = 0.08f; mn.startLifetime = 3f; mn.startSpeed = 0f; mn.gravityModifier = 0.25f;
              var sh2 = cdust.shape; sh2.shapeType = ParticleSystemShapeType.Box; sh2.scale = new Vector3(8f, 0.1f, 8f); var e2 = cdust.emission; e2.rateOverTime = 120f;
              cdust.GetComponent<ParticleSystemRenderer>().sharedMaterial = AdditiveMatTex("Venus_dust", 0.35f); cdust.Stop(); }
            // Round 13 (0k): stones raining from the ceiling with the dust (C04 and later tremors), and outdoors a stone fall
            var stones = new GameObject("TremorStones").AddComponent<ParticleSystem>(); stones.transform.position = V.CubeAt + Vector3.up * 8f;
            { var mn = stones.main; mn.playOnAwake = false; mn.loop = false; mn.duration = 2.6f; mn.startColor = new Color(0.32f, 0.32f, 0.32f, 1f); mn.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f); mn.startLifetime = 2.2f; mn.startSpeed = 0f; mn.gravityModifier = 1.0f; mn.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
              var sh3 = stones.shape; sh3.shapeType = ParticleSystemShapeType.Box; sh3.scale = new Vector3(9f, 0.1f, 10f); var e3 = stones.emission; e3.rateOverTime = 45f;
              var col = stones.collision; col.enabled = true; col.type = ParticleSystemCollisionType.Planes; col.dampen = 0.6f; col.bounce = 0.25f;
              var plane = new GameObject("StoneFloor").transform; plane.SetParent(stones.transform, false); plane.position = new Vector3(V.CubeAt.x, 0.02f, V.CubeAt.z); col.SetPlane(0, plane);
              stones.GetComponent<ParticleSystemRenderer>().sharedMaterial = Dissolve("Venus_stone_bits", Gray(0.3f), 0.2f, false); stones.Stop(); }

            // the dodecahedron (in her pocket)
            var shape = BuildElarimShape("Dodecahedron", world, em, "Venus_Dodeca");

            var ui = new GameObject("ChapterUI").AddComponent<Seven.ChapterUI>(); ui.Cam = cam;
            var dlg = dirGo.AddComponent<Seven.DialoguePlayer>(); dlg.Bank = bank; dlg.UI = ui;
            var legend = new GameObject("ControlsLegend").AddComponent<Seven.ControlsLegend>(); legend.Cam = cam; legend.Set = Seven.ControlsLegend.Layout.Chapter; legend.HidePanels = true;
            var d = dirGo.AddComponent<Seven.VenusDirector>();
            d.Seven = s7; d.Cat = cat; d.Cam = ccam; d.Input = input; d.Dlg = dlg; d.UI = ui; d.Legend = legend; d.Bike = hb;
            d.Saucer = saucer; d.Hatch = hatch; d.SaucerBeam = beamC; d.Vessel = vessel; d.VesselWater = water; d.Cube = cube; d.CubeCage = cage.transform;
            d.Guardian = guardian; d.Attendant = attendant; d.Workers = workers.ToArray(); d.Worms = worms.ToArray(); d.CeilingDust = cdust;
            d.Flyers = flyers.ToArray(); d.Shape = shape; d.MaintGate = maintGate; d.Atmo = atmo.GetComponent<Seven.Atmosphere>();
            d.Slab = slab; d.BridgeRamps = ramps; d.WalkwayIntact = walkway; d.TremorStones = stones; d.SlabDust = slabDust; d.TkMotes = tkMotes;                 // QF10: the telekinesis bridge
            d.HatchSeam = saucer.Find("HatchSeam").GetComponent<Renderer>(); d.BayLight = saucer.Find("Bay/BayLight").GetComponent<Light>();
            d.BayBack = saucer.Find("Bay/BayBack").GetComponent<Renderer>(); d.Hold = saucer.GetComponent<Seven.SaucerHold>();
            BuildVenusPart2(world, m, em, d, worm, wormCtl, (n, p, f, c) => MakeElarim(n, p, f, c), dlg);      // Round 12
            AddProbe(new Vector3(0f, 3f, 20f), new Vector3(60f, 20f, 60f), Vector3.zero);
            AddProbe(new Vector3(30f, 5f, 250f), new Vector3(30f, 16f, 50f), Vector3.zero);
            ZFightFix("Venus");
            EditorSceneManager.SaveScene(scene, VenusScenePath);
            Log("scene saved " + VenusScenePath);
        }

        [System.Serializable] class BikePointsJson { public float length = 3f; public float[] saddle, grip_l, grip_r, foot_l, foot_r; }
        struct BikePoints { public float length; public Vector3 saddle, gripL, gripR, footL, footR; }
        static BikePoints LoadBikePoints()
        {
            Vector3 V3(float[] a, Vector3 def) => a != null && a.Length == 3 ? new Vector3(a[0], a[1], a[2]) : def;
            var path = Path.Combine(RepoRoot, "Art", "Export", "hoverbike", "hoverbike_points.json");
            var j = File.Exists(path) ? JsonUtility.FromJson<BikePointsJson>(File.ReadAllText(path)) : new BikePointsJson();
            var b = new BikePoints
            {
                length = j.length, saddle = V3(j.saddle, new Vector3(0f, 0.48f, 0f)), gripL = V3(j.grip_l, new Vector3(-0.37f, 0.44f, 0.7f)),
                gripR = V3(j.grip_r, new Vector3(0.37f, 0.44f, 0.7f)), footL = V3(j.foot_l, new Vector3(-0.26f, 0.25f, -0.12f)), footR = V3(j.foot_r, new Vector3(0.26f, 0.25f, -0.12f)),
            };
            // the grips are symmetric in the design: average the two measurements
            float gx = (Mathf.Abs(b.gripL.x) + Mathf.Abs(b.gripR.x)) * 0.5f, gy = (b.gripL.y + b.gripR.y) * 0.5f, gz = (b.gripL.z + b.gripR.z) * 0.5f;
            b.gripL = new Vector3(-gx, gy, gz); b.gripR = new Vector3(gx, gy, gz);
            Log($"bike points ({(File.Exists(path) ? "hoverbike_points.json" : "defaults")}): length {b.length:0.00}, saddle {b.saddle}, grips {b.gripL} / {b.gripR}, feet {b.footL} / {b.footR}");
            return b;
        }

        /// <summary>QF10: 7's riding pose (Tools/Blender/make_ride_pose.py -> Art/Export/anim/7_ride.fbx): imported Humanoid with
        /// its own avatar (explicit Mixamo map; the Blender export has an extra root node, so 7's avatar can't be copied onto
        /// it) and retargeted to 7 as the state "ride".</summary>
        static void AddRideClip(AnimatorController sevenCtl)
        {
            var src = Path.Combine(RepoRoot, "Art", "Export", "anim", "7_ride.fbx");
            if (!File.Exists(src)) { Log("WARNING: Art/Export/anim/7_ride.fbx missing (run Tools/Blender/make_ride_pose.py) - riding uses the seated clip"); return; }
            var dir = SevenDir + "/Ride"; Directory.CreateDirectory(Path.Combine(ProjectRoot, dir));
            var dst = dir + "/7_ride.fbx";
            File.Copy(src, Path.Combine(ProjectRoot, dst), true);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceSynchronousImport);
            var ci = (ModelImporter)AssetImporter.GetAtPath(dst);
            ci.animationType = ModelImporterAnimationType.Human; ci.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            ci.materialImportMode = ModelImporterMaterialImportMode.None; ci.importAnimation = true; ci.SaveAndReimport();
            ApplyMixamoHumanMap(ci, dst);
            var cl = ci.defaultClipAnimations;
            foreach (var c in cl) { c.name = "ride"; c.loopTime = true; c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true; c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true; }
            ci.clipAnimations = cl; ci.SaveAndReimport();
            var clip = AssetDatabase.LoadAllAssetsAtPath(dst).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null) { Log("WARNING: 7_ride.fbx has no clip"); return; }
            var sm = sevenCtl.layers[0].stateMachine;
            foreach (var st0 in sm.states) if (st0.state.name == "ride") sm.RemoveState(st0.state);
            var st = sm.AddState("ride"); st.motion = clip; st.writeDefaultValues = true;
            EditorUtility.SetDirty(sevenCtl);
            Log($"ride clip: {clip.length:0.00}s humanMotion={clip.humanMotion} -> state 'ride' on {AssetDatabase.GetAssetPath(sevenCtl)}");
        }

        /// <summary>The original Venus cube: pale stone, glowing edges, the same Elarim marking (placeholder glyph) on one face.</summary>
        static Transform BuildVenusCube(Transform parent, VenusMats m, ElarimMats em)
        {
            var root = new GameObject("VenusCube").transform; root.SetParent(parent, false); root.position = V.CubeAt;
            root.rotation = Quaternion.Euler(0f, 30f, 0f);
            const float s = 0.42f;
            var v = new List<Vector3>(); var t = new List<int>(); AddBox(v, t, Vector3.zero, Vector3.one * s);
            MeshObject("Body", SaveMesh(Finish(v, t), "Venus_Cube_Body"), em.stone, root);
            var ev = new List<Vector3>(); var et = new List<int>();
            float h = s / 2f;
            var corners = new[] { new Vector3(-h, -h, -h), new Vector3(h, -h, -h), new Vector3(h, h, -h), new Vector3(-h, h, -h), new Vector3(-h, -h, h), new Vector3(h, -h, h), new Vector3(h, h, h), new Vector3(-h, h, h) };
            int[,] edges = { { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 }, { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 }, { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };
            for (int i = 0; i < 12; i++) Sweep(ev, et, new[] { corners[edges[i, 0]], corners[edges[i, 1]] }, 0.006f, false, 6);
            MeshObject("Edges", SaveMesh(SmoothFinish(ev, et), "Venus_Cube_Edges"), em.edges, root, false);
            var mv = new List<Vector3> { new Vector3(-h * 0.8f, -h * 0.8f, -h - 0.002f), new Vector3(h * 0.8f, -h * 0.8f, -h - 0.002f), new Vector3(h * 0.8f, h * 0.8f, -h - 0.002f), new Vector3(-h * 0.8f, h * 0.8f, -h - 0.002f) };
            var mesh = new Mesh { name = "Venus_Cube_Mark" }; mesh.SetVertices(mv); mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) }); mesh.RecalculateNormals();
            MeshObject("Mark", SaveMesh(mesh, "Venus_Cube_Mark"), em.mark, root, false);
            var l = new GameObject("CubeLight").AddComponent<Light>(); l.transform.SetParent(root, false); l.type = LightType.Point; l.range = 5f; l.intensity = 0.8f; l.shadows = LightShadows.None;
            return root;
        }
    }
}
