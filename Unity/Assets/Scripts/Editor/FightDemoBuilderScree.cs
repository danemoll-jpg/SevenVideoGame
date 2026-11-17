using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF4: the real Scree (Art/Export/scree/scree_v1_rigged.fbx from Tools/Blender/cleanup_scree.py). Imported as a
    /// Humanoid with an explicit bone map (the extra lower arm pair / abdomen / antenna bones stay generic and move
    /// procedurally in <see cref="Seven.ScreeRig"/>). Controller: 7's Mixamo idle / walk / look_around retargeted, plus the
    /// Scree's own keyed work loop and alert reaction. If the export is missing the Chapter 1 builder falls back to the
    /// gray Y Bot placeholder.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const string ScreeDir = CharRoot + "/Scree";
        const string ScreeFbx = ScreeDir + "/scree_v1_rigged.fbx";

        static readonly string[] ScreeHuman =
        {
            "Hips", "Spine", "Chest", "UpperChest", "Neck", "Head",
            "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand", "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
            "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes", "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes",
        };

        /// <summary>Import the Scree and build its controller; null when the Blender export is missing (placeholder fallback).</summary>
        static AnimatorController SetupScree(AnimatorController sevenCtl, out Material shell, out Material glow)
        {
            shell = glow = null;
            var src = Path.Combine(RepoRoot, "Art", "Export", "scree", "scree_v1_rigged.fbx");
            if (!File.Exists(src)) { Debug.LogWarning("[FightDemoBuilder] Scree export missing (run Tools/Blender/cleanup_scree.py) - using Scree_placeholder"); return null; }
            Directory.CreateDirectory(Path.Combine(ProjectRoot, ScreeDir, "Textures"));
            File.Copy(src, Path.Combine(ProjectRoot, ScreeFbx), true);
            File.Copy(Path.Combine(RepoRoot, "Art", "Export", "scree", "Textures", "scree_v1_basecolor.jpg"),
                Path.Combine(ProjectRoot, ScreeDir, "Textures", "scree_v1_basecolor.jpg"), true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var texPath = ScreeDir + "/Textures/scree_v1_basecolor.jpg";
            var ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
            ti.maxTextureSize = 2048; ti.sRGBTexture = true; ti.SaveAndReimport();
            // pale matte shell (the texture carries the dark joints), eyes + tank lights = small soft white glow
            shell = Dissolve("Scree_shell", new Color(0.86f, 0.86f, 0.86f), 0.18f, false);
            shell.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            shell.SetFloat("_Rim", 0.18f); shell.SetFloat("_RimPower", 3.5f);
            EditorUtility.SetDirty(shell);
            glow = Dissolve("Scree_glow", Color.white, 0.5f, false);
            glow.EnableKeyword("_EMISSION");
            glow.SetColor("_EmissionColor", Color.white * 2.2f);
            EditorUtility.SetDirty(glow);

            var imp = (ModelImporter)AssetImporter.GetAtPath(ScreeFbx);
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = true;
            imp.SaveAndReimport();
            // explicit human map (never the name guesser: the ScreeB_* lower arm bones must not be taken for arms)
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ScreeFbx);
            var all = model.GetComponentsInChildren<Transform>(true);
            var hd = imp.humanDescription;
            hd.human = ScreeHuman.Where(n => all.Any(t => t.name == n))
                .Select(n => new HumanBone { boneName = n, humanName = HumanName(n), limit = new HumanLimit { useDefaultValues = true } }).ToArray();
            hd.skeleton = all.Select(t => new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale }).ToArray();
            hd.hasTranslationDoF = false;
            imp.humanDescription = hd;
            var clips = imp.defaultClipAnimations;
            foreach (var c in clips)
            {
                var n = c.takeName.Contains("|") ? c.takeName.Substring(c.takeName.LastIndexOf('|') + 1) : c.takeName;
                c.name = n;
                c.loopTime = n == "work";
                c.lockRootRotation = true; c.keepOriginalOrientation = true;
                c.lockRootHeightY = true; c.keepOriginalPositionY = true;
                c.lockRootPositionXZ = true; c.keepOriginalPositionXZ = true;
            }
            imp.clipAnimations = clips;
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "scree_shell"), shell);
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "scree_glow"), glow);
            imp.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(ScreeFbx).OfType<Avatar>().FirstOrDefault();
            Log($"{ScreeFbx}: avatar human={avatar?.isHuman} valid={avatar?.isValid}, {hd.human.Length} human bones mapped");
            if (avatar == null || !avatar.isHuman || !avatar.isValid) { Debug.LogWarning("[FightDemoBuilder] Scree avatar invalid - using Scree_placeholder"); return null; }

            var path = AnimDir + "/Scree.controller";
            AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            var sm = ac.layers[0].stateMachine;
            foreach (var st in sevenCtl.layers[0].stateMachine.states.Select(s => s.state).Where(s => s.name == "idle" || s.name == "walk" || s.name == "look_around"))
            {
                var ns = sm.AddState(st.name); ns.motion = st.motion; ns.writeDefaultValues = true;
                if (st.name == "idle") sm.defaultState = ns;
            }
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(ScreeFbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")))
            {
                var ns = sm.AddState(clip.name); ns.motion = clip; ns.writeDefaultValues = true;
                Log($"scree clip {clip.name} {clip.length:0.00}s loop={clip.isLooping} humanMotion={clip.humanMotion}");
            }
            EditorUtility.SetDirty(ac);
            Log($"{path}: states [{string.Join(", ", sm.states.Select(s => s.state.name))}]");
            return ac;
        }

        static string HumanName(string bone)
        {
            // Unity's HumanTrait names: "Left Upper Arm", "Upper Chest", ...
            var match = HumanTrait.BoneName.FirstOrDefault(h => h.Replace(" ", "") == bone);
            if (match == null) throw new Exception("No humanoid bone for " + bone);
            return match;
        }

        /// <summary>The real Scree: Humanoid model, procedural extras, optional chest lamp (guards).</summary>
        static GameObject SpawnScreeModel(string name, Vector3 pos, Vector3 facing, AnimatorController ctl, MoonMats m, bool lamp)
        {
            var go = Spawn(ScreeFbx, name, pos, facing, ctl);
            go.AddComponent<Seven.ScreeRig>();
            var chest = go.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "UpperChest");
            if (lamp && chest != null)
            {
                var fwd = go.transform.forward;
                var lgo = new GameObject("GuardLamp");
                lgo.transform.position = chest.position + fwd * 0.2f + Vector3.down * 0.05f;
                lgo.transform.rotation = Quaternion.LookRotation(fwd + Vector3.down * 0.25f);
                lgo.transform.SetParent(chest, true);
                var l = lgo.AddComponent<Light>();
                l.type = LightType.Spot; l.range = 9f; l.spotAngle = 72f; l.intensity = 4f; l.color = Color.white; l.shadows = LightShadows.None;
                Block("LampBulb", lgo.transform.position - Vector3.one * 0.04f, lgo.transform.position + Vector3.one * 0.04f, m.glow, lgo.transform, false);
            }
            return go;
        }
    }
}
