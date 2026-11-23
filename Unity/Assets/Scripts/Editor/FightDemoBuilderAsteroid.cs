using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using A = Seven.AsteroidLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 16 — builds the scene "Asteroid" (Chapter "The Asteroid — the Great Machine Age", Docs/CHAPTER_ASTEROID.md):
    /// the receiving chamber, the service tunnel, the cavern (the city that still works) with the causeway and its three
    /// junctions, the central maintenance hall, the temple archive, the factory area - plus the Machinist on Dan's Mixamo rig
    /// (FBX for Unity, with skin; his front plate re-skinned onto its own hinge bone here), the one saucer prefab, 7, the
    /// bike, the camera and the AsteroidDirector. Numbers in Runtime/AsteroidLayout.cs. Atmosphere_Asteroid = Kyle's fog /
    /// glow (dark machinery near, a bright hazy distance, soft lights inside the fog, huge terraces fading into it).
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const string AsteroidScenePath = "Assets/Scenes/Asteroid.unity";
        const string AtmoAsteroidPath = AtmoDir + "/Atmosphere_Asteroid.prefab";
        const string MachinistDir = CharRoot + "/Machinist";

        static void BuildRound16(AnimatorController sevenCtl, GameObject atmoPrefab, Seven.VoiceBank bank)
        {
            var mach = SetupMixamoFolk("machinist", MachinistDir, out var machCtl, out var machMat);
            var bike = AssetDatabase.LoadAssetAtPath<GameObject>(CharRoot + "/hoverbike/hoverbike_v1.fbx");
            var atmo = BuildAtmosphereAsteroid(atmoPrefab);
            CopyVideo("vision_05_asteroid.mp4");
            BuildAsteroidScene(sevenCtl, bank, atmo, mach, machCtl, machMat, bike);
        }

        static GameObject BuildAtmosphereAsteroid(GameObject atmoPrefab)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(atmoPrefab);
            var a = inst.GetComponent<Seven.Atmosphere>();
            // inside the asteroid: dark machinery silhouettes near, a bright glowing haze across the cavern, ground fog pooling
            // in the depths below the causeway, the terraces fading into it; thinned round the action by the director
            a.FogStart = 14f;
            a.GroundDensity = 0.016f; a.GroundBase = A.CavernFloor; a.GroundFalloff = 12f; a.GroundRange = 160f;
            a.ValleyDensity = 0f;
            a.DistanceDensity = 0.0016f; a.HazeHeight = 90f; a.MaxFog = 0.72f;
            a.NearBrightness = 0.07f; a.FarBrightness = 0.46f; a.BrightnessDistance = 230f; a.ZenithBrightness = 0.24f;
            a.NoiseStrength = 0.3f; a.NoiseScale = 0.04f; a.NoiseSpeed = 0.03f; a.NoiseFadeDistance = 70f;
            a.GlowIntensity = 0.38f; a.GlowRadius = 55f;
            var glows = new[]
            {
                (new Vector3(-180f, 20f, 40f), new Vector3(9f, 3f, 1f)), (new Vector3(-300f, 12f, -60f), new Vector3(6f, 2f, 1f)),
                (new Vector3(-90f, 30f, 170f), new Vector3(7f, 3f, 1f)), (new Vector3(-420f, 25f, 60f), new Vector3(6f, 2.4f, 1f)),
                (new Vector3(-240f, -20f, 120f), new Vector3(5f, 2f, 1f)), (new Vector3(-120f, -26f, -40f), new Vector3(5f, 1.6f, 1f)),
                (new Vector3(-360f, 18f, 105f), new Vector3(3f, 1.4f, 1f)), (new Vector3(0f, 14f, 0f), new Vector3(2.4f, 1.2f, 1f)),
            };
            int gi = 0;
            foreach (Transform c in inst.transform)
            {
                if (c.name.StartsWith("Glow") && gi < glows.Length) { c.position = glows[gi].Item1; c.localScale = glows[gi].Item2; gi++; }
                if (c.name == "BackLight") { c.rotation = Quaternion.Euler(35f, 150f, 0f); var l = c.GetComponent<Light>(); l.intensity = 0.7f; l.shadowStrength = 0.6f; }
                if (c.name == "FrontFill") { c.rotation = Quaternion.Euler(50f, 330f, 0f); c.GetComponent<Light>().intensity = 0.35f; }
            }
            FogBudget(inst.GetComponent<Seven.Atmosphere>(), "Asteroid", 0.98f, 0.8f, 16f);   // QF28 pass: the QF26 fog-colour rule (<= ~1.0)
            var prefab = PrefabUtility.SaveAsPrefabAsset(inst, AtmoAsteroidPath);
            Object.DestroyImmediate(inst);
            return prefab;
        }

        // ---------------------------------------------------------------- the Machinist's extras on his rig
        class MachinistBits { public Transform plate; public Renderer visor; public Renderer[] couplings; public Transform cutter, clamp; public int plateVerts; }
        /// <summary>His front plate onto its own hinge bone (never through his legs), the visor slit, the power couplings and
        /// the two tool assemblies on his forearms.</summary>
        static MachinistBits MachinistExtras(GameObject inst, AsMats m)
        {
            var b = new MachinistBits();
            var an = inst.GetComponent<Animator>();
            var hips = an.GetBoneTransform(HumanBodyBones.Hips);
            var root = inst.transform;
            var smr = inst.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(s => s.sharedMesh != null ? s.sharedMesh.vertexCount : 0).FirstOrDefault();
            if (smr != null && hips != null)
            {
                var mesh = Object.Instantiate(smr.sharedMesh); mesh.name = "machinist_plate_hinged";
                var baked = new Mesh(); smr.BakeMesh(baked, true);
                var bv = baked.vertices;
                var hinge = new GameObject("PlateHinge").transform; hinge.SetParent(hips, false);
                float hipsZ = hips.position.z - root.position.z;
                hinge.position = root.position + new Vector3(0f, 1.95f, hipsZ + 0.2f);
                var bones = smr.bones.ToList(); int bi = bones.Count; bones.Add(hinge);
                var bind = mesh.bindposes.ToList(); bind.Add(hinge.worldToLocalMatrix * smr.transform.localToWorldMatrix);
                var bw = mesh.boneWeights;
                int n = 0;
                for (int i = 0; i < bv.Length && i < bw.Length; i++)
                {
                    var wpos = smr.transform.position + smr.transform.rotation * bv[i];
                    var rel = wpos - root.position;
                    if (Mathf.Abs(rel.x) < 0.21f && rel.y > 1.22f && rel.y < 1.93f && rel.z > hipsZ + 0.06f)
                    { bw[i] = new BoneWeight { boneIndex0 = bi, weight0 = 1f }; n++; }
                }
                mesh.boneWeights = bw; mesh.bindposes = bind.ToArray();
                var mp = MachinistDir + "/machinist_plate_hinged.asset";
                AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(mesh, mp);
                smr.bones = bones.ToArray(); smr.sharedMesh = mesh;
                b.plate = hinge; b.plateVerts = n;
                Log($"Machinist: the front plate re-skinned onto its own hinge bone 'PlateHinge' at the belt ({n} vertices) - it swings with the thighs, never through them");
            }
            // the visor slit: a thin curved white strip across the front of the narrow dome helmet (glows with his voice)
            var head = an.GetBoneTransform(HumanBodyBones.Head);
            if (head != null)
            {
                var v = new List<Vector3>(); var t = new List<int>();
                float rr = 0.2f;
                for (int i = 0; i <= 12; i++)
                {
                    float a = Mathf.Lerp(-55f, 55f, i / 12f) * Mathf.Deg2Rad;
                    var p = new Vector3(Mathf.Sin(a) * rr, 0f, Mathf.Cos(a) * rr);
                    v.Add(p + Vector3.up * 0.018f); v.Add(p - Vector3.up * 0.018f);
                    if (i > 0) { int k0 = (i - 1) * 2; t.AddRange(new[] { k0, k0 + 2, k0 + 1, k0 + 1, k0 + 2, k0 + 3 }); }
                }
                var vis = MeshObject("Machinist_VisorSlit", SaveMesh(Finish(v, t), "Ast_MachinistVisor"), new Material(m.glowSoft) { name = "Ast_visor" }, head, false);
                vis.transform.position = head.position + Vector3.up * 0.11f + root.forward * 0.03f; vis.transform.rotation = root.rotation;
                b.visor = vis.GetComponent<Renderer>();
            }
            // the power couplings (glow, flare on a blade hit): hips left / right, the chest core, both shoulders
            var coup = new List<Renderer>();
            void Coupling(string n, HumanBodyBones bone, Vector3 off, float r)
            {
                var bt = an.GetBoneTransform(bone); if (bt == null) return;
                var v = new List<Vector3>(); var t = new List<int>(); Ellipsoid(v, t, Vector3.zero, new Vector3(r, r, r * 0.6f), 5, 10);
                var g = MeshObject("Machinist_Coupling_" + n, SaveMesh(SmoothFinish(v, t), "Ast_Coupling_" + n), new Material(m.glowSoft) { name = "Ast_coupling_" + n }, bt, false);
                g.transform.position = bt.position + root.rotation * off; g.transform.rotation = root.rotation;
                coup.Add(g.GetComponent<Renderer>());
            }
            Coupling("HipL", HumanBodyBones.Hips, new Vector3(-0.3f, -0.05f, 0.24f), 0.07f);
            Coupling("HipR", HumanBodyBones.Hips, new Vector3(0.3f, -0.05f, 0.24f), 0.07f);
            Coupling("Chest", HumanBodyBones.Chest, new Vector3(0f, 0.1f, 0.36f), 0.1f);
            Coupling("ShoulderL", HumanBodyBones.LeftUpperArm, new Vector3(0f, 0.12f, 0.05f), 0.07f);
            Coupling("ShoulderR", HumanBodyBones.RightUpperArm, new Vector3(0f, 0.12f, 0.05f), 0.07f);
            b.couplings = coup.ToArray();
            // the tool assemblies: a cutter disc on the right forearm, a two-jaw clamp tool on the left
            var lr = an.GetBoneTransform(HumanBodyBones.RightLowerArm); var hr = an.GetBoneTransform(HumanBodyBones.RightHand);
            if (lr != null && hr != null)
            {
                var tool = new GameObject("CutterTool").transform; tool.SetParent(lr, false); tool.position = Vector3.Lerp(lr.position, hr.position, 0.7f) + root.right * 0.12f;
                var cv = new List<Vector3>(); var ct = new List<int>();
                Append(cv, ct, RotZ(Lathe(new[] { new Vector2(0f, -0.04f), new Vector2(0.36f, -0.04f), new Vector2(0.4f, 0f), new Vector2(0.36f, 0.04f), new Vector2(0f, 0.04f) }, 0f, 360f, 20), Vector3.zero));
                MBox(cv, ct, new Vector3(-0.12f, 0f, 0f), new Vector3(0.24f, 0.2f, 0.36f), 0f, 0f);
                MeshObject("CutterTool_Mesh", SaveMesh(Finish(cv, ct), "Ast_CutterTool"), m.steel, tool).transform.localPosition = Vector3.zero;
                b.cutter = tool;
            }
            var ll = an.GetBoneTransform(HumanBodyBones.LeftLowerArm); var hl = an.GetBoneTransform(HumanBodyBones.LeftHand);
            if (ll != null && hl != null)
            {
                var tool = new GameObject("ClampTool").transform; tool.SetParent(ll, false); tool.position = Vector3.Lerp(ll.position, hl.position, 0.7f) - root.right * 0.12f;
                var cv = new List<Vector3>(); var ct = new List<int>();
                MBox(cv, ct, Vector3.zero, new Vector3(0.22f, 0.28f, 0.34f), 0f, 0f);
                foreach (var s in new[] { -1f, 1f }) MBox(cv, ct, new Vector3(0f, s * 0.16f, 0.28f), new Vector3(0.12f, 0.06f, 0.4f), 0f, 0f);
                MeshObject("ClampTool_Mesh", SaveMesh(Finish(cv, ct), "Ast_ClampTool"), m.steel, tool).transform.localPosition = Vector3.zero;
                b.clamp = tool;
            }
            return b;
        }

        static void BuildAsteroidScene(AnimatorController sevenCtl, Seven.VoiceBank bank, GameObject atmoP, GameObject mach, AnimatorController machCtl, Material machMat, GameObject bike)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null; RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.3f, 0.3f, 0.3f);
            var shs = new SphericalHarmonicsL2(); shs.AddAmbientLight(RenderSettings.ambientLight); RenderSettings.ambientProbe = shs;
            var atmo = (GameObject)PrefabUtility.InstantiatePrefab(atmoP); atmo.name = "Atmosphere_Asteroid";
            var world = new GameObject("Asteroid").transform;
            var m = new AsMats
            {
                metal = Dissolve("Ast_metal", Gray(0.42f), 0.55f, false, 0.55f), metalDark = Dissolve("Ast_metal_dark", Gray(0.16f), 0.45f, false, 0.45f),
                steel = Dissolve("Ast_steel", Gray(0.7f), 0.75f, false, 0.8f), plate = Dissolve("Ast_plate", Gray(0.3f), 0.4f, false, 0.4f),
                floor = Dissolve("Ast_floor", Gray(0.34f), 0.3f, false, 0.3f), rock = Dissolve("Ast_rock", Gray(0.26f), 0.15f, false),
                rockDark = Dissolve("Ast_rock_dark", Gray(0.12f), 0.1f, false), glow = Dissolve("Ast_glow", Color.white, 0.5f, false),
                glowSoft = Dissolve("Ast_glow_soft", Gray(0.5f), 0.4f, false), cable = Dissolve("Ast_cable", Gray(0.08f), 0.3f, false),
                panel = Dissolve("Ast_panel", Gray(0.2f), 0.4f, false), glass = AdditiveMat("Ast_glass", 0.06f), beam = AdditiveMat("Ast_beam", 0.8f),
                stone = Dissolve("Ast_stone", Gray(0.74f), 0.3f, false),
            };
            m.glow.EnableKeyword("_EMISSION"); m.glow.SetColor("_EmissionColor", Color.white * 2.2f); EditorUtility.SetDirty(m.glow);
            m.glowSoft.EnableKeyword("_EMISSION"); m.glowSoft.SetColor("_EmissionColor", Color.white * 0.3f); EditorUtility.SetDirty(m.glowSoft);
            foreach (var x in new[] { m.metal, m.metalDark, m.plate, m.floor, m.rock, m.rockDark, m.stone }) { x.SetFloat("_Cull", 0f); EditorUtility.SetDirty(x); }

            var rc = BuildReceiving(world, m);
            var cav = BuildCavern(world, m);
            var hall = BuildHall(world, m);
            var bladeLight = AdditiveMat("Ast_blade_light", 1.4f);
            BuildWorks(world, m, hall, bladeLight);
            // the rune (a cuboctahedron, generated in code like the others) + its small counterpart for 7
            var em = ElarimMaterials(false);
            var rune = BuildCuboctahedronShape("AsteroidRune", world, 0.42f, em, "Ast_Rune").transform; rune.position = A.RuneAt + Vector3.down * 0.12f;
            var recordCubo = BuildCuboctahedronShape("AsteroidRecordCopy", world, 0.06f, em, "Ast_RecordCubo").transform; recordCubo.gameObject.SetActive(false);

            // ---------------------------------------------------------------- the saucer (inside the receiving chamber)
            var saucer = PlaceSaucer(world, A.SaucerAt);
            var beam = saucer.GetComponent<Seven.SaucerBeam>();
            CheckSaucer("Asteroid", saucer);

            // ---------------------------------------------------------------- ground, 7, camera, bike, helmet, the Machinist
            var wg = new GameObject("WorldGround").AddComponent<Seven.WorldGround>();
            wg.Shape = Seven.Ground.Shape.Zones;
            wg.Zones = A.Zones.Select(z => new Seven.WorldGround.ZoneDef { Name = z.Name, Min = new Vector2(z.X0, z.Z0), Max = new Vector2(z.X1, z.Z1), Y0 = z.Y0, Y1 = z.Y1, Axis = z.Axis, Enabled = z.On }).ToArray();
            wg.ZoneObstacles = new Vector3[0];
            var seven = Spawn(SevenDir + "/7_character.fbx", "7", A.SevenDown, Vector3.forward, sevenCtl);
            var camGo = new GameObject("ChapterCamera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            cam.fieldOfView = 54f; cam.nearClipPlane = 0.08f; cam.farClipPlane = 2000f; camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData(); camData.renderPostProcessing = true; camData.requiresDepthTexture = true; camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camGo.transform.position = A.SevenDown + new Vector3(-3f, 1.8f, -3f);
            var dirGo = new GameObject("AsteroidDirector");
            var input = dirGo.AddComponent<Seven.ChapterInput>();
            var s7 = seven.AddComponent<Seven.ChapterSeven>(); s7.Input = input; s7.CameraTransform = camGo.transform;
            var ccam = camGo.AddComponent<Seven.ChapterCamera>(); ccam.Target = seven.transform; ccam.Input = input;
            // the bubble helmet (as Europa) for the arrival in the unpressurized chamber
            var glassMat = BubbleMat();
            var hv = new List<Vector3>(); var ht = new List<int>(); Ellipsoid(hv, ht, Vector3.zero, Vector3.one * 0.5f, 18, 28);
            var helmetSphere = MeshObject("BubbleHelmet", SaveMesh(SmoothFinish(hv, ht), "Ast_Helmet"), glassMat, world, false);
            helmetSphere.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var collarMat = Dissolve("Europa_helmet_collar", Gray(0.62f), 0.65f, false, 0.6f);
            var colV = new List<Vector3>(); var colT = new List<int>();
            Sweep(colV, colT, Enumerable.Range(0, 32).Select(i => new Vector3(Mathf.Cos(i / 32f * Mathf.PI * 2f) * 0.148f, 0f, Mathf.Sin(i / 32f * Mathf.PI * 2f) * 0.148f)).ToList(), 0.022f, true, 8);
            Sweep(colV, colT, Enumerable.Range(0, 32).Select(i => new Vector3(Mathf.Cos(i / 32f * Mathf.PI * 2f) * 0.142f, 0.026f, Mathf.Sin(i / 32f * Mathf.PI * 2f) * 0.142f)).ToList(), 0.01f, true, 6);
            var collar = MeshObject("HelmetCollar", SaveMesh(SmoothFinish(colV, colT), "Ast_HelmetCollar"), collarMat, world, false);
            var helmet = seven.AddComponent<Seven.BubbleHelmet>(); helmet.Sphere = helmetSphere.transform; helmet.Collar = collar.transform; helmet.Anim = seven.GetComponent<Animator>();
            // the bike (as Mars)
            var bikeRoot = new GameObject("HoverBike").transform; bikeRoot.position = A.BikePad;
            var visual = new GameObject("Visual").transform; visual.SetParent(bikeRoot, false);
            if (bike != null) { var bm = (GameObject)PrefabUtility.InstantiatePrefab(bike); bm.name = "BikeModel"; bm.transform.SetParent(visual, false); var bmat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Venus_hoverbike_v1.mat"); foreach (var r in bm.GetComponentsInChildren<Renderer>()) r.sharedMaterial = bmat; }
            var bp = LoadBikePoints();
            Transform P(string n, Vector3 lp) { var x = new GameObject(n).transform; x.SetParent(visual, false); x.localPosition = lp; return x; }
            var hb = bikeRoot.gameObject.AddComponent<Seven.HoverBike>();
            hb.Seat = P("Seat", bp.saddle); hb.GripL = P("GripL", bp.gripL); hb.GripR = P("GripR", bp.gripR); hb.FootL = P("FootL", bp.footL); hb.FootR = P("FootR", bp.footR);
            hb.Fairing = P("FairingL", new Vector3(bp.gripL.x - 0.05f, bp.gripL.y + 0.04f, bp.gripL.z - 0.25f)); hb.Visual = visual; hb.Length = bp.length;
            var vl = new GameObject("VentLight").AddComponent<Light>(); vl.transform.SetParent(visual, false); vl.transform.localPosition = new Vector3(0f, -0.15f, 0f); vl.type = LightType.Point; vl.range = 3f; vl.intensity = 0.4f; vl.shadows = LightShadows.None; hb.VentLight = vl;
            // the Machinist
            Seven.MarsFolk machF = null; MachinistBits bits = null;
            if (mach != null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(mach); go.name = "Machinist";
                var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
                an.avatar = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(mach)).OfType<Avatar>().FirstOrDefault(); an.runtimeAnimatorController = machCtl; an.applyRootMotion = false;
                foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()) { r.sharedMaterial = machMat; r.updateWhenOffscreen = true; }
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                bits = MachinistExtras(go, m);
                go.transform.SetPositionAndRotation(A.CradleAt, Quaternion.LookRotation(Vector3.back));
                machF = go.AddComponent<Seven.MarsFolk>(); machF.Role = "machinist"; machF.WalkSpeed = 1.1f; machF.CalmSway = false;
            }
            else Log("WARNING: the Machinist's Mixamo body is missing - Art/Mixamo/machinist/machinist_body_mixamo.fbx");
            Seven.MachinistRig rig = null;
            if (machF != null && bits != null)
            {
                rig = machF.gameObject.AddComponent<Seven.MachinistRig>();
                rig.Anim = machF.GetComponent<Animator>(); rig.PlateHinge = bits.plate; rig.Visor = bits.visor; rig.Couplings = bits.couplings; rig.CutterTool = bits.cutter; rig.ClampTool = bits.clamp;
            }
            // the guide light (as Mars)
            var gl0 = new List<Vector3>(); var glt = new List<int>(); Ellipsoid(gl0, glt, Vector3.zero, Vector3.one * 0.5f, 10, 14);
            var guideLight = MeshObject("GuideLight", SaveMesh(SmoothFinish(gl0, glt), "Ast_GuideLight"), m.glow, world, false);
            { var hvv = new List<Vector3>(); var htt = new List<int>(); for (int k = 0; k < 24; k++) { var a0 = Polar(2.6f, k * 15f, 0f); var a1 = Polar(2.6f, (k + 1) * 15f, 0f); Quad(hvv, htt, Vector3.zero, new Vector3(a1.x, a1.z, 0f), new Vector3(a0.x, a0.z, 0f), Vector3.zero); }
              var halo = Finish(hvv, htt); halo.colors = halo.vertices.Select(p => new Color(1f, 1f, 1f, Mathf.Lerp(0.85f, 0f, Mathf.Clamp01(p.magnitude / 2.6f)))).ToArray();
              MeshObject("GuideHalo", SaveMesh(halo, "Ast_GuideHalo"), AdditiveMat("Ast_guide_halo", 1.4f), guideLight.transform, false); }
            PointLight("GuideLightLamp", Vector3.zero, 24f, 3f, guideLight.transform).transform.localPosition = Vector3.zero;
            guideLight.SetActive(false);
            // a canister for the throwable scrap in the fight
            var canMesh = SaveMesh(Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.32f, 0f), new Vector2(0.34f, 0.06f), new Vector2(0.34f, 0.84f), new Vector2(0.3f, 0.9f), new Vector2(0f, 0.9f) }, 0f, 360f, 14), "Ast_ScrapCanister");

            var ui = new GameObject("ChapterUI").AddComponent<Seven.ChapterUI>(); ui.Cam = cam;
            var dlg = dirGo.AddComponent<Seven.DialoguePlayer>(); dlg.Bank = bank; dlg.UI = ui;
            var legend = new GameObject("ControlsLegend").AddComponent<Seven.ControlsLegend>(); legend.Cam = cam; legend.Set = Seven.ControlsLegend.Layout.Chapter; legend.HidePanels = true;
            var d = dirGo.AddComponent<Seven.AsteroidDirector>();
            d.Seven = s7; d.Cam = ccam; d.Input = input; d.Dlg = dlg; d.UI = ui; d.Legend = legend; d.Bike = hb; d.SaucerBeam = beam; d.Saucer = saucer; d.Hold = saucer.GetComponent<Seven.SaucerHold>();
            d.Atmo = atmo.GetComponent<Seven.Atmosphere>(); d.GuideLight = guideLight.transform; d.Helmet = helmet;
            d.Scanner = rc.scanner; d.MaintBot = rc.bot; d.Container = rc.container; d.ContainerMarks = rc.marks; d.DoorWheel = rc.doorWheel; d.DoorLamp = rc.doorLamp; d.ServiceDoor = rc.door; d.OuterGate = rc.outer; d.DoorLine = rc.doorLine; d.PressureVents = rc.vents;
            d.Junctions = cav.junctions; d.JunctionLevers = cav.levers; d.JunctionLamps = cav.jlamps; d.Bird = cav.bird; d.BirdLamp = cav.birdLamp;
            d.Machinist = machF; d.MRig = rig; d.Cradle = hall.cradle; d.Stations = hall.stations; d.Terminal = hall.terminal; d.TerminalScreen = hall.terminalScreen;
            d.ArchiveLine = hall.line; d.CutterPanel = hall.cutterPanel; d.PanelGlyph = hall.panelGlyph; d.SignalGlyph = hall.signalGlyph;
            d.HallDoorShutter = hall.hallDoor; d.HatchShutter = hall.hatch; d.ArchiveShutter = hall.archiveDoor; d.WestShutter = hall.westDoor;
            d.Rune = rune; d.RecordCubo = recordCubo; d.MemoryScreen = hall.memory; d.StorageDoor = hall.storageDoor; d.StoredPanels = hall.stored;
            d.ProductionLights = cav.prodLights.Concat(hall.prodLights).ToArray(); d.ProductionLamps = cav.prodLamps.Concat(hall.prodLamps).ToArray();
            d.ScrapMat = m.metalDark; d.CanisterMat = m.metal; d.CanisterMesh = canMesh; d.DebrisMeshes = HallDebrisMeshes();   // QF21 (10)
            d.AlcoveScreen = hall.alcoveScreen; d.LockerDoor = hall.lockerDoor; d.LockerLamp = hall.lockerLamp; d.LockerBlades = hall.lockerBlades;
            d.TransportLever = hall.transportLever; d.CargoPlatforms = hall.platforms; d.PlatformParked = hall.parked; d.PlatformAligned = hall.aligned;
            d.ClampPower = hall.clampPower; d.ClampJaw = hall.clampJaw; d.ValveWheel = hall.valveWheel; d.Coolant = hall.coolant; d.Steam = hall.steam;
            d.FinalPlat = hall.finalPlat; d.DiverterLever = hall.diverterLever; d.ControlLamps = hall.controlLamps; d.Security = hall.security;
            d.Vision5 = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Video/vision_05_asteroid.mp4");
            AddProbe(new Vector3(0f, 10f, 0f), new Vector3(40f, 24f, 40f), Vector3.zero);
            AddProbe(new Vector3(-180f, 0f, 40f), new Vector3(320f, 80f, 300f), Vector3.zero);
            AddProbe(new Vector3(-360f, 10f, 105f), new Vector3(84f, 28f, 74f), Vector3.zero);
            AddProbe(new Vector3(-366f, 4f, 30f), new Vector3(100f, 16f, 70f), Vector3.zero);
            PlaceAboardCut(new GameObject("Asteroid").transform, new Vector3(0f, -1500f, 0f));   // QF23 (10, 15a): Panther's cutaways = the real saucer interior
            QF18SceneChecks("Asteroid", true, () => { for (int i = 0; i < hall.platforms.Length; i++) hall.platforms[i].position = hall.aligned[i]; }, () => { for (int i = 0; i < hall.platforms.Length; i++) hall.platforms[i].position = hall.parked[i]; });
            EditorSceneManager.SaveScene(scene, AsteroidScenePath);
            Log("scene saved " + AsteroidScenePath + $" (Machinist {(machF != null ? "rigged, plate hinge " + (bits != null ? bits.plateVerts + " vertices" : "-") : "missing")})");
        }

        /// <summary>Round 16: a cuboctahedron record (the rune / its small copy) in the Elarim stone, its 24 edges faintly lit.</summary>
        static GameObject BuildCuboctahedronShape(string name, Transform parent, float radius, ElarimMats em, string meshPrefix)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            MeshObject("Body", SaveMesh(Seven.Cuboctahedron.Mesh(radius), meshPrefix + "_Body"), em.stone, root.transform);
            var ev = new List<Vector3>(); var et = new List<int>();
            foreach (var (a, b) in Seven.Cuboctahedron.Edges) Sweep(ev, et, new[] { Seven.Cuboctahedron.Vertex(a, radius) * 1.002f, Seven.Cuboctahedron.Vertex(b, radius) * 1.002f }, radius * 0.02f, false, 5);
            MeshObject("Edges", SaveMesh(SmoothFinish(ev, et), meshPrefix + "_Edges"), em.edges, root.transform, false);
            return root;
        }
    }
}
