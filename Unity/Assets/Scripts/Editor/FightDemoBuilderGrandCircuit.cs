using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using T = Seven.GcTrack;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 20 - the scene "GrandCircuit" (Docs/RACING_MODE.md, Stage 1): the racing mode, separate from the story. Both tracks'
    /// worlds are built here round their ribbons (the ribbons themselves are built at load from the same GcTrack definitions):
    /// Cattalus Unlimited (the cardboard city's kit: mansions, cat-tree towers, the scratching-post column standing in the loop, cat
    /// trees round the corkscrew, the toy boulevard under the jump, the cardboard quarter the shortcut cuts through, the stands, the
    /// hills) and the Lunar Loop (the hollow Moon's cavern: the rock floor far below, the stepped pyramids and terraces of the city,
    /// the mining pit under the jump with its gantries, the tunnels' ribs and lamps, the cavern's dome). Supports carry the track
    /// wherever it stands above the ground. The vehicle templates: 7 on her bike, Duchess in her racer #8, Tunneler on his drill rig
    /// (the story's models and rigs), each about 3-4 m long, nose along +z, underside at y 0.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const string GrandCircuitScenePath = "Assets/Scenes/GrandCircuit.unity";

        /// <summary>Round 20 iteration: only the Grand Circuit scene again (every other scene as the last full build saved it), then the
        /// player. A release is always made with BuildAllFromCommandLine.</summary>
        public static void BuildGrandCircuitFromCommandLine()
        {
            int code = 1;
            try
            {
                _saucerPrefab = null;
                var sevenCtl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "/Seven.controller");
                BuildRound20(sevenCtl);
                EditorBuildSettings.scenes = AllScenes.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
                AssetDatabase.SaveAssets();
                code = BuildPlayer() ? 0 : 1;
            }
            catch (System.Exception e) { Debug.LogError("[FightDemoBuilder] GC BUILD FAILED: " + e); }
            EditorApplication.Exit(code);
        }

        static void BuildRound20(AnimatorController sevenCtl)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null; RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.34f, 0.34f, 0.34f);
            var shs = new SphericalHarmonicsL2(); shs.AddAmbientLight(RenderSettings.ambientLight); RenderSettings.ambientProbe = shs;
            var root = new GameObject("GrandCircuit");
            var g = root.AddComponent<Seven.GcGame>();

            // ---------------------------------------------------------------- materials (monochrome; the light = white emission)
            Material Em(string n, float gray, float em, float smooth = 0.4f) { var mm = Dissolve(n, Gray(gray), smooth, false); mm.EnableKeyword("_EMISSION"); mm.SetColor("_EmissionColor", Color.white * em); EditorUtility.SetDirty(mm); return mm; }
            g.Road = Dissolve("Gc_road", Gray(0.2f), 0.62f, false, 0.25f); g.Under = Dissolve("Gc_under", Gray(0.14f), 0.3f, false, 0.3f);
            g.Wall = Dissolve("Gc_wall", Gray(0.34f), 0.55f, false, 0.5f); g.Frame = Dissolve("Gc_frame", Gray(0.26f), 0.6f, false, 0.6f);
            g.Glow = Em("Gc_glow", 1f, 2.4f, 0.5f); g.GlowSoft = Em("Gc_glow_soft", 0.6f, 0.55f); g.Pad = Em("Gc_boost_pad", 0.9f, 1.6f); g.PickupMat = Em("Gc_pickup_pad", 0.85f, 1.2f);
            foreach (var mm in new[] { g.Road, g.Under, g.Wall }) { mm.SetFloat("_Cull", 0f); EditorUtility.SetDirty(mm); }

            // ---------------------------------------------------------------- the camera template (each player's view is a copy)
            var camGo = new GameObject("CamTemplate"); camGo.transform.SetParent(root.transform, false);
            var cam = camGo.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.6f, 0.6f, 0.6f);
            cam.fieldOfView = 64f; cam.nearClipPlane = 0.15f; cam.farClipPlane = 3000f;
            var cd = cam.GetUniversalAdditionalCameraData(); cd.renderPostProcessing = true; cd.requiresDepthTexture = true; cd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            g.CamTemplate = cam;

            // ---------------------------------------------------------------- the two worlds (one shown at a time)
            var catAtmo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AtmoCattalusPath)); catAtmo.name = "Atmosphere_Cattalus";
            var moonAtmo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AtmoMoonPath)); moonAtmo.name = "Atmosphere_Moon";
            var moonVolP = AssetDatabase.LoadAssetAtPath<VolumeProfile>(AtmoDir + "/Moon_Volume.asset");
            if (moonVolP != null) { var vgo = new GameObject("MoonVolume"); vgo.transform.SetParent(moonAtmo.transform, false); var vol = vgo.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 10f; vol.sharedProfile = moonVolP; }
            // a race wants a brighter Moon than the prison: lift its lights and soften its fog a touch (the instance only)
            foreach (var l in moonAtmo.GetComponentsInChildren<Light>(true)) if (l.type == LightType.Directional) l.intensity *= 1.6f;
            // the Moon's haze is tuned for the prison's rooms; across a 2 km cavern it piled up to a white-out over the pit (the jump
            // captures: white 23-26 %, almost no edges) - thinner with distance and capped lower, the far haze a little darker
            { var a = moonAtmo.GetComponent<Seven.Atmosphere>(); if (a != null) { a.DistanceDensity *= 0.45f; a.MaxFog = Mathf.Min(a.MaxFog, 0.7f); a.FarBrightness *= 0.8f; a.GroundDensity *= 0.5f; Log($"GC LUNAR HAZE (R20): distance density {a.DistanceDensity:0.0000}, max fog {a.MaxFog:0.00}, far brightness {a.FarBrightness:0.00}"); } }
            g.CattalusAtmo = catAtmo; g.LunarAtmo = moonAtmo;
            var ct = T.Cattalus(); var lt = T.Lunar();
            g.CattalusSet = BuildGcCattalusSet(ct).gameObject; g.LunarSet = BuildGcLunarSet(lt).gameObject;
            g.CattalusLowest = ct.P.Min(p => p.y) - 4f; g.LunarLowest = lt.P.Min(p => p.y) - 4f;
            moonAtmo.SetActive(false); g.LunarSet.SetActive(false);
            Log($"GRAND CIRCUIT (R20): Cattalus Unlimited {ct.Length:0} m (closing error {ct.CloseError:0.0} m, shortcut {(ct.Branch != null ? ct.Branch.Length.ToString("0") + " m" : "none")}), Lunar Loop {lt.Length:0} m (closing error {lt.CloseError:0.0} m)");

            // ---------------------------------------------------------------- the vehicle templates
            var tpl = new GameObject("Templates").transform; tpl.SetParent(root.transform, false);
            g.BikeTpl = GcBikeTemplate(tpl); g.SevenTpl = GcSevenTemplate(tpl, sevenCtl); g.RacerTpl = GcRacerTemplate(tpl); g.DuchessTpl = GcDuchessTemplate(tpl); g.RigTpl = GcRigTemplate(tpl);
            foreach (Transform c in tpl) c.gameObject.SetActive(false);

            EditorSceneManager.SaveScene(scene, GrandCircuitScenePath);
            Log($"scene saved {GrandCircuitScenePath} (the Grand Circuit: templates bike {(g.BikeTpl != null)}, 7 {(g.SevenTpl != null)}, racer {(g.RacerTpl != null)}, Duchess {(g.DuchessTpl != null)}, drill rig {(g.RigTpl != null)})");
        }

        // ------------------------------------------------------------------------------------------------ templates
        /// <summary>Puts a template's renderers' bottom at y 0 and reports its length (the racers hover on that).</summary>
        static void GcGround(Transform t, string what, float wantLen = 0f)
        {
            var rs = t.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            if (wantLen > 0f && b.size.z > 0.1f) { float k = wantLen / b.size.z; t.localScale *= k; b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); }
            t.position += Vector3.up * -b.min.y;
            Log($"GC VEHICLE (R20): {what} {b.size.x:0.00} x {b.size.y:0.00} x {b.size.z:0.00} m (scale {t.localScale.x:0.00}), underside at 0");
        }
        static GameObject GcBikeTemplate(Transform parent)
        {
            var bike = AssetDatabase.LoadAssetAtPath<GameObject>(CharRoot + "/hoverbike/hoverbike_v1.fbx");
            var r = new GameObject("BikeTpl").transform; r.SetParent(parent, false);
            var visual = new GameObject("Visual").transform; visual.SetParent(r, false);
            if (bike != null) { var bm = (GameObject)PrefabUtility.InstantiatePrefab(bike); bm.name = "BikeModel"; bm.transform.SetParent(visual, false); var bmat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Venus_hoverbike_v1.mat"); foreach (var rr in bm.GetComponentsInChildren<Renderer>()) rr.sharedMaterial = bmat; }
            var bp = LoadBikePoints();
            Transform P(string n, Vector3 lp) { var x = new GameObject(n).transform; x.SetParent(visual, false); x.localPosition = lp; return x; }
            P("Seat", bp.saddle); P("GripL", bp.gripL); P("GripR", bp.gripR); P("FootL", bp.footL); P("FootR", bp.footR);
            GcGround(visual, "7's bike");
            return r.gameObject;
        }
        static GameObject GcSevenTemplate(Transform parent, AnimatorController sevenCtl)
        {
            var seven = Spawn(SevenDir + "/7_character.fbx", "SevenTpl", Vector3.zero, Vector3.forward, sevenCtl);
            seven.transform.SetParent(parent, false); var an = seven.GetComponent<Animator>(); an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return seven;
        }
        static GameObject GcRacerTemplate(Transform parent)
        {
            var racerModel = AssetDatabase.LoadAssetAtPath<GameObject>(CharRoot + "/cat_racer/cat_racer_v1.fbx");
            var r = new GameObject("RacerTpl").transform; r.SetParent(parent, false);
            var body = new GameObject("Body").transform; body.SetParent(r, false);
            if (racerModel != null)
            {
                var rm = (GameObject)PrefabUtility.InstantiatePrefab(racerModel); rm.name = "RacerModel"; rm.transform.SetParent(body, false);
                if (MeasureRacer(body, out var front, out _, out var why)) { float yaw = Vector3.SignedAngle(front, Vector3.forward, Vector3.up); rm.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * rm.transform.localRotation; rm.transform.localPosition = Quaternion.Euler(0f, yaw, 0f) * rm.transform.localPosition; Log($"GC VEHICLE (R20): the racer turned {yaw:0} deg so its nose leads ({why})"); }
            }
            else { var k = new AK(); var mm = Dissolve("Gc_racer_standin", Gray(0.8f), 0.7f, false, 0.4f); var (v, t) = k.Of(mm); Ellipsoid(v, t, new Vector3(0f, 0.45f, 0f), new Vector3(0.8f, 0.4f, 1.6f), 8, 14); k.Save("RacerStandIn", body); }
            // her racer is sized to her (1.5 m) - among 3-4 m vehicles on a 16 m road it read as a toy: the racer and Duchess together 1.7x
            body.localScale = Vector3.one * 1.7f;
            GcGround(body, "Duchess's racer #8");
            MeasureRacer(body, out _, out var bed, out _); if (racerModel == null) bed = new Vector3(0f, 0.36f, -0.05f);
            var seat = new GameObject("DuchessSeat").transform; seat.SetParent(body, false); seat.localPosition = bed;   // (MeasureRacer gives the bed in the measured root's frame)
            return r.gameObject;
        }
        static GameObject GcDuchessTemplate(Transform parent)
        {
            var dCtl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "/Duchess.controller");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(CharRoot + "/duchess/duchess_v1_rigged.fbx");
            var dMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Cat_duchess_fur.mat"); var whisker = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Cat_whisker.mat");
            if (model == null || dCtl == null) { Log("GC VEHICLE (R20): Duchess's model missing - her racer runs without her"); return null; }
            var droot = new GameObject("DuchessTpl"); droot.transform.SetParent(parent, false);
            var dm = (GameObject)PrefabUtility.InstantiatePrefab(model); dm.name = "DuchessModel"; dm.transform.SetParent(droot.transform, false);
            var head = dm.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "head"); var pelvis = dm.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "pelvis");
            if (head != null && pelvis != null) { var fw = droot.transform.InverseTransformDirection(head.position - pelvis.position); fw.y = 0f; dm.transform.localRotation = Quaternion.Euler(0f, Vector3.SignedAngle(fw, Vector3.forward, Vector3.up), 0f) * dm.transform.localRotation; }
            var an = dm.GetComponent<Animator>() ?? dm.AddComponent<Animator>(); an.runtimeAnimatorController = dCtl; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var smr in dm.GetComponentsInChildren<SkinnedMeshRenderer>()) { smr.updateWhenOffscreen = true; smr.sharedMaterials = smr.sharedMaterials.Select(mm => mm != null && mm.name.Contains("whisker") ? whisker : dMat).ToArray(); }
            droot.transform.localScale = Vector3.one * 1.25f;
            droot.AddComponent<Seven.CatPoseOverlay>();
            return droot;
        }
        static GameObject GcRigTemplate(Transform parent)
        {
            var m = new MoonMats { metal = Dissolve("Moon_metal", Gray(0.34f), 0.6f, false, 0.5f), metalDark = Dissolve("Moon_metal_dark", Gray(0.16f), 0.5f, false, 0.4f), glow = Dissolve("Moon_lamp", Color.white, 0.5f, false), rock = Dissolve("Moon_rock", Gray(0.14f), 0.12f, false) };   // (the rig uses metal / metalDark)
            var soft = Dissolve("Fin_glow_soft", Gray(0.55f), 0.4f, false);
            var craft = new GameObject("RigTpl").transform; craft.SetParent(parent, false);
            var holder = new GameObject("Craft").transform; holder.SetParent(craft, false);
            var rig = BuildTunnelerRig(holder, m, soft);
            var tunModel = AssetDatabase.LoadAssetAtPath<GameObject>(CharRoot + "/tunneler/tunneler_v1.fbx");
            if (tunModel != null && rig != null)
            {
                var tm = (GameObject)PrefabUtility.InstantiatePrefab(tunModel); tm.name = "Tunneler"; tm.transform.SetParent(rig.Hull, false); tm.transform.localPosition = Vector3.up * rig.DeckTop; tm.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                var tmat = Dissolve("Europa_tunneler", Gray(0.5f), 0.35f, false); foreach (var r in tm.GetComponentsInChildren<Renderer>()) r.sharedMaterial = tmat;
                RigTunnelerBody(tm, "GrandCircuit");
                var drill = tm.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "DrillHead");
                if (drill != null) { var dh = Seven.DrillHead.On(drill, tm.transform); dh.Bake(); }
            }
            else Log("GC VEHICLE (R20): Tunneler's model missing - his rig races empty");
            // his rig is a 4.6 m disc: scaled to a racer's size (wins bumps by weight, not by size)
            holder.localScale = Vector3.one * 0.78f;
            GcGround(holder, "Tunneler's drill rig");
            return craft.gameObject;
        }

        // ------------------------------------------------------------------------------------------------ helpers along a track
        /// <summary>True when (x, z) is at least clear metres from every sample of the track (and its shortcut) in plan.</summary>
        static bool GcClear(T t, Vector3 p, float clear)
        {
            float c2 = clear * clear;
            for (int i = 0; i < t.Count; i += 2) { var d = t.P[i] - p; d.y = 0f; if (d.sqrMagnitude < (clear + t.HalfW[i]) * (clear + t.HalfW[i])) return false; }
            if (t.Branch != null) for (int i = 0; i < t.Branch.Count; i += 2) { var d = t.Branch.P[i] - p; d.y = 0f; if (d.sqrMagnitude < (clear + 6f) * (clear + 6f)) return false; }
            return true;
        }
        /// <summary>Supports: a column under the ribbon every <paramref name="every"/> m wherever it stands more than 1.2 m above the
        /// ground (upright parts only: never under a loop's top or the corkscrew's roll - those have their own frames).</summary>
        static void GcSupports(T t, AK k, Material mat, float groundY, float every, float r)
        {
            void Track(T tt)
            {
                for (int i = 0; i < tt.Count; i += (int)every)
                {
                    var f = tt.Flag[i]; if ((f & (T.F.Gap | T.F.Loop | T.F.Cork)) != 0) continue;
                    var up = tt.R[i] * Vector3.up; if (up.y < 0.85f) continue;
                    var p = tt.P[i] - up * 0.65f; float h = p.y - groundY; if (h < 1.2f) continue;
                    var (v, tr) = k.Of(mat);
                    Append(v, tr, Lathe(new[] { new Vector2(0f, 0f), new Vector2(r * 1.4f, 0f), new Vector2(r, 0.6f), new Vector2(r, h - 0.4f), new Vector2(r * 1.5f, h), new Vector2(0f, h) }, 0f, 360f, 10, null, new Vector3(p.x, groundY, p.z)));
                }
            }
            Track(t); if (t.Branch != null) Track(t.Branch);
        }
        /// <summary>The loop's frame: two arched trusses outside the ribbon's edges, from the ground round the circle.</summary>
        static void GcLoopFrames(T t, AK k, Material mat, float groundY)
        {
            for (int i = 0; i < t.Count; i++)
            {
                if ((t.Flag[i] & T.F.Loop) == 0 || i % 10 != 0) continue;
                var up = t.R[i] * Vector3.up; var right = t.R[i] * Vector3.right; var (v, tr) = k.Of(mat);
                foreach (var sg in new[] { -1f, 1f })
                {
                    var a = t.P[i] + right * sg * (t.HalfW[i] + 0.6f) - up * 0.6f; var b = a - up * 1.6f;
                    Sweep(v, tr, new[] { a, b }, 0.25f, false, 6);
                    // hangers down to the ground where the frame is low enough
                    if (up.y > 0.3f && a.y - groundY < 30f) Sweep(v, tr, new[] { b, new Vector3(b.x, groundY, b.z) }, 0.3f, false, 6);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------ Cattalus Unlimited
        static Transform BuildGcCattalusSet(T t)
        {
            var set = new GameObject("CattalusSet").transform;
            var m = new CtMats
            {
                card = Dissolve("Cat_card", Gray(0.6f), 0.15f, false), cardDark = Dissolve("Cat_card_dark", Gray(0.24f), 0.1f, false), cardLight = Dissolve("Cat_card_light", Gray(0.74f), 0.15f, false),
                tape = Dissolve("Cat_tape", Gray(0.86f), 0.55f, false), sisal = Dissolve("Cat_sisal", Gray(0.52f), 0.04f, false), carpet = Dissolve("Cat_carpet", Gray(0.44f), 0.02f, false),
                carpetDark = Dissolve("Cat_carpet_dark", Gray(0.3f), 0.02f, false), fabric = Dissolve("Cat_fabric", Gray(0.66f), 0.1f, false), wood = Dissolve("Cat_wood", Gray(0.38f), 0.25f, false),
                stone = Dissolve("Cat_stone", Gray(0.7f), 0.3f, false), metal = Dissolve("Cat_metal", Gray(0.62f), 0.65f, false, 0.8f), glow = Dissolve("Cat_glow", Color.white, 0.5f, false),
                glowSoft = Dissolve("Cat_glow_soft", Gray(0.55f), 0.4f, false), grass = Dissolve("Cat_grass", Gray(0.4f), 0.1f, false), grassDark = Dissolve("Cat_grass_dark", Gray(0.34f), 0.1f, false),
                leaf = Dissolve("Cat_leaf", Gray(0.42f), 0.15f, false), bark = Dissolve("Cat_bark", Gray(0.3f), 0.1f, false), hill = Dissolve("Cat_hill", Gray(0.56f), 0.1f, false),
                road = Dissolve("Cat_road", Gray(0.36f), 0.3f, false), rail = Dissolve("Cat_rail", Gray(0.5f), 0.1f, false), paper = Dissolve("Cat_paper", Gray(0.9f), 0.2f, false), ground = Dissolve("Cat_ground", Gray(0.46f), 0.2f, false),
            };
            var rnd = new System.Random(2020); float R() => (float)rnd.NextDouble();
            var mn = new Vector3(t.P.Min(p => p.x), 0f, t.P.Min(p => p.z)); var mx = new Vector3(t.P.Max(p => p.x), 0f, t.P.Max(p => p.z)); var c = (mn + mx) * 0.5f;
            const float gy = -1.2f;
            // the ground: the meadow plain (rolling a little), far past the circuit
            {
                var v = new List<Vector3>(); var tr = new List<int>(); int n = 80; float span = 2600f;
                for (int j = 0; j <= n; j++) for (int i = 0; i <= n; i++) { float x = c.x - span / 2 + span * i / n, z = c.z - span / 2 + span * j / n; float far = Mathf.Clamp01((new Vector2(x - c.x, z - c.z).magnitude - 420f) / 500f); v.Add(new Vector3(x, gy + far * (Mathf.PerlinNoise(x * 0.004f, z * 0.004f) * 40f), z)); }
                for (int j = 0; j < n; j++) for (int i = 0; i < n; i++) { int a = j * (n + 1) + i, b = a + n + 1; tr.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
                MeshObject("CattalusGround", SaveMesh(SmoothFinish(v, tr), "Gc_CattalusGround"), m.grass, set, false);
            }
            var k = new AK();
            // the scratching-post column inside the loop: the loop's centre
            var loop = Enumerable.Range(0, t.Count).Where(i => (t.Flag[i] & T.F.Loop) != 0).ToList();
            if (loop.Count > 0)
            {
                // the loop threads through the scratching-post column: two great rope-wrapped posts either side of the loop's circle, a
                // carpeted beam across their tops (the racers loop between the posts, under the beam)
                var lc = loop.Aggregate(Vector3.zero, (s, i) => s + t.P[i]) / loop.Count; float top = loop.Max(i => t.P[i].y) + 8f;
                var side = t.R[loop[0]] * Vector3.right; side.y = 0f; side.Normalize();
                float half = 24f * 0.5f + 7f + 9f; var pa = new Vector3(lc.x, gy, lc.z) - side * half; var pb = new Vector3(lc.x, gy, lc.z) + side * half;
                ScratchPost(k, m, pa, 4.5f, top - gy + 4f, 31); ScratchPost(k, m, pb, 4.5f, top - gy + 4f, 32);
                var (bv, bt) = k.Of(m.carpetDark); MBox(bv, bt, (pa + pb) * 0.5f + Vector3.up * (top - gy + 3f), new Vector3(half * 2f + 10f, 4f, 9f), Quaternion.LookRotation(side).eulerAngles.y + 90f, 0.02f);
                var (gv0, gt0) = k.Of(m.glow); MBox(gv0, gt0, (pa + pb) * 0.5f + Vector3.up * (top - gy + 0.9f), new Vector3(half * 2f, 0.2f, 0.6f), Quaternion.LookRotation(side).eulerAngles.y + 90f, 0f);
            }
            // cat trees round the corkscrew (both sides)
            var cork = Enumerable.Range(0, t.Count).Where(i => (t.Flag[i] & T.F.Cork) != 0).ToList();
            for (int q = 0; q < cork.Count; q += 30) { int i = cork[q]; var rt = t.R[i] * Vector3.right; rt.y = 0f; rt.Normalize(); foreach (var sg in new[] { -1f, 1f }) { var at = t.P[i] + rt * sg * (T.CorkR * 2f + 10f); at.y = gy; CatTree(k, m, at, 26f + R() * 14f, 500 + q + (sg > 0 ? 7 : 0), 1.2f); } }
            // the toy boulevard under the jump (QF R20 image gate: the first version read as one grey tone - range 0.25): a wide dark
            // road from well before the kicker to past the landing with glowing lane lines and lamp posts, giant toys in two rows in dark
            // and bright (balls with dark bands, dark yarn piles, a pale toy mouse), seen from the air
            var gap = Enumerable.Range(0, t.Count).Where(i => (t.Flag[i] & T.F.Gap) != 0).ToList();
            if (gap.Count > 0)
            {
                var (lv, lt) = k.Of(m.paper); var (dv, dt) = k.Of(m.cardDark); var (yv, yt) = k.Of(m.carpetDark); var (gv, gt) = k.Of(m.glow); var (pv, pt) = k.Of(m.metal);
                int g0 = Mathf.Max(0, gap[0] - 60), g1 = Mathf.Min(t.Count - 1, gap[gap.Count - 1] + 60);
                var a = new Vector3(t.P[g0].x, gy + 0.06f, t.P[g0].z); var b = new Vector3(t.P[g1].x, gy + 0.06f, t.P[g1].z); var dir = (b - a).normalized; var side = Vector3.Cross(Vector3.up, dir);
                MBox(dv, dt, (a + b) * 0.5f, new Vector3(22f, 0.12f, (b - a).magnitude), Quaternion.LookRotation(dir).eulerAngles.y, 0f);
                for (float d = 0f; d < (b - a).magnitude; d += 6f) foreach (var lx in new[] { -10f, -3.5f, 3.5f, 10f }) MBox(gv, gt, a + dir * d + side * lx + Vector3.up * 0.07f, new Vector3(0.25f, 0.04f, 3f), Quaternion.LookRotation(dir).eulerAngles.y, 0f);
                for (float d = 0f; d < (b - a).magnitude; d += 18f) foreach (var lx in new[] { -12.5f, 12.5f }) { var at = a + dir * d + side * lx; Sweep(pv, pt, new[] { at, at + Vector3.up * 7f }, 0.18f, false, 6); MBox(gv, gt, at + Vector3.up * 7.1f, new Vector3(1.2f, 0.25f, 0.5f), Quaternion.LookRotation(dir).eulerAngles.y, 0f); }
                for (int q = 0; q < gap.Count; q += 7)
                {
                    int i = gap[q]; var rt = t.R[i] * Vector3.right; rt.y = 0f; rt.Normalize();
                    foreach (var sg in new[] { -1f, 1f })
                    {
                        var at = t.P[i] + rt * sg * (15f + R() * 6f); at.y = gy; float rr = 2.4f + R() * 2.2f;
                        int kind = (q / 7 + (sg > 0 ? 1 : 0)) % 3;
                        if (kind == 0) { Ellipsoid(lv, lt, at + Vector3.up * rr, Vector3.one * rr, 8, 14); for (int s2 = -1; s2 <= 1; s2++) Ellipsoid(dv, dt, at + Vector3.up * (rr + s2 * rr * 0.45f), new Vector3(rr * 1.02f * Mathf.Sqrt(1f - 0.2f * s2 * s2), 0.25f, rr * 1.02f * Mathf.Sqrt(1f - 0.2f * s2 * s2)), 3, 14); }
                        else if (kind == 1) { for (int y2 = 0; y2 < 3; y2++) Ellipsoid(yv, yt, at + Vector3.up * (rr * 0.9f) + GcJitter(rnd) * rr * 0.5f, Vector3.one * rr * 0.9f, 6, 10); }
                        else { Ellipsoid(lv, lt, at + Vector3.up * rr * 0.6f, new Vector3(rr * 0.8f, rr * 0.6f, rr * 1.4f), 6, 12); Ellipsoid(dv, dt, at + Vector3.up * rr * 1.1f + dir * rr * 1.2f, Vector3.one * rr * 0.35f, 4, 8); }
                    }
                }
            }
            // the cardboard quarter round the shortcut + its tunnel of box arches
            if (t.Branch != null)
            {
                var b = t.Branch;
                for (int i = 0; i < b.Count; i += 6)
                {
                    var p = b.P[i]; var rt = b.R[i] * Vector3.right; var up = b.R[i] * Vector3.up; float w = b.HalfW[i] + 0.4f;
                    CardboardBox(k, m, p - rt * w + Vector3.down * 0.6f, new Vector3(0.5f, 5.2f, 2.6f), b.R[i].eulerAngles.y, 900 + i, false, false);
                    CardboardBox(k, m, p + rt * w + Vector3.down * 0.6f, new Vector3(0.5f, 5.2f, 2.6f), b.R[i].eulerAngles.y, 950 + i, false, false);
                    if (i % 12 == 0) { var (av, at2) = k.Of(m.cardLight); MBox(av, at2, p + up * 4.7f, new Vector3(2f * w + 0.6f, 0.4f, 2.4f), b.R[i].eulerAngles.y, 0f); }
                    if (i % 18 == 0) { var (gv2, gt2) = k.Of(m.glow); MBox(gv2, gt2, p + up * 4.45f, new Vector3(2f * w - 1f, 0.08f, 0.3f), b.R[i].eulerAngles.y, 0f); }
                    if (i % 12 == 6) foreach (var sg in new[] { -1f, 1f }) { var at = p + rt * sg * (w + 9f + R() * 5f); at.y = gy; if (GcClear(t, at, 5f)) CardboardMansion(k, m, at, b.R[i].eulerAngles.y + 90f, 2 + (int)(R() * 3f), 1100 + i); }
                }
            }
            // the city along the rest: mansions, cat trees, scratching posts, fountains (clear of the ribbon), lamps on the walls' outside
            for (int i = 0; i < t.Count; i += 24)
            {
                if ((t.Flag[i] & (T.F.Loop | T.F.Cork | T.F.Gap)) != 0) continue;
                var rt = t.R[i] * Vector3.right; rt.y = 0f; if (rt.sqrMagnitude < 0.1f) continue; rt.Normalize();
                foreach (var sg in new[] { -1f, 1f })
                {
                    var at = t.P[i] + rt * sg * (t.HalfW[i] + 12f + R() * 22f); at.y = gy; if (!GcClear(t, at, 7f)) continue;
                    float u = R(); float yaw = Quaternion.LookRotation(-rt * sg).eulerAngles.y;
                    if (u < 0.45f) CardboardMansion(k, m, at, yaw, 2 + (int)(R() * 4f), 1500 + i * 2 + (sg > 0 ? 1 : 0));
                    else if (u < 0.7f) CatTree(k, m, at, 14f + R() * 16f, 1700 + i, 1f);
                    else if (u < 0.85f) ScratchPost(k, m, at, 1.6f + R(), 10f + R() * 12f, 1900 + i);
                    else Fountain(k, m, at);
                }
            }
            // the stands by the line (tufted terraces), the far hills
            { int s0 = 20; var rt = t.R[s0] * Vector3.right; rt.y = 0f; rt.Normalize(); var at = t.P[s0] + rt * -(t.HalfW[s0] + 6f); at.y = gy; Terrace(k, m, at - t.R[s0] * Vector3.forward * 10f, Quaternion.LookRotation(rt).eulerAngles.y, 40f, 10f, 5); }
            HillLine(k, m, c, 900f, 120f, 77);
            // supports under every raised stretch
            GcSupports(t, k, m.sisal, gy, 9f, 0.55f); GcLoopFrames(t, k, m.metal, gy);
            k.Save("GcCattalus", set);
            return set;
        }
        static Vector3 GcJitter(System.Random r) => new Vector3((float)r.NextDouble() - 0.5f, (float)r.NextDouble() - 0.5f, (float)r.NextDouble() - 0.5f);

        // ------------------------------------------------------------------------------------------------ the Lunar Loop
        static Transform BuildGcLunarSet(T t)
        {
            var set = new GameObject("LunarSet").transform;
            var rock = Dissolve("Moon_rock", Gray(0.14f), 0.12f, false); var rockLight = Dissolve("Gc_moon_rock_light", Gray(0.3f), 0.15f, false);
            var ancient = Dissolve("Moon_ancient", Gray(0.46f), 0.3f, false); var ancientDark = Dissolve("Moon_ancient_dark", Gray(0.27f), 0.25f, false);
            var metal = Dissolve("Moon_metal", Gray(0.34f), 0.6f, false, 0.5f); var metalDark = Dissolve("Moon_metal_dark", Gray(0.16f), 0.5f, false, 0.4f);
            var lamp = Dissolve("Gc_moon_lamp", Color.white, 0.5f, false); lamp.EnableKeyword("_EMISSION"); lamp.SetColor("_EmissionColor", Color.white * 1.8f); EditorUtility.SetDirty(lamp);
            var window = Dissolve("Gc_moon_window", Gray(0.7f), 0.4f, false); window.EnableKeyword("_EMISSION"); window.SetColor("_EmissionColor", Color.white * 0.7f); EditorUtility.SetDirty(window);
            var rnd = new System.Random(4040); float R() => (float)rnd.NextDouble();
            var mn = new Vector3(t.P.Min(p => p.x), 0f, t.P.Min(p => p.z)); var mx = new Vector3(t.P.Max(p => p.x), 0f, t.P.Max(p => p.z)); var c = (mn + mx) * 0.5f;
            float gy = t.P.Min(p => p.y) - 14f;
            var gap = Enumerable.Range(0, t.Count).Where(i => (t.Flag[i] & T.F.Gap) != 0).ToList();
            var pitC = gap.Count > 0 ? t.P[gap[gap.Count / 2]] : c;
            // the cavern floor: broken rock, rising toward the walls; the mining pit under the jump (a deep round shaft)
            {
                var v = new List<Vector3>(); var tr = new List<int>(); int n = 90; float span = 2400f;
                float H(float x, float z)
                {
                    float d = new Vector2(x - c.x, z - c.z).magnitude; float wall = Mathf.Clamp01((d - 520f) / 400f);
                    float pit = new Vector2(x - pitC.x, z - pitC.z).magnitude; float hole = Mathf.Clamp01(1f - (pit - 22f) / 16f);
                    return gy + Mathf.PerlinNoise(x * 0.01f, z * 0.01f) * 6f + wall * wall * 260f - hole * 160f;
                }
                for (int j = 0; j <= n; j++) for (int i = 0; i <= n; i++) { float x = c.x - span / 2 + span * i / n, z = c.z - span / 2 + span * j / n; v.Add(new Vector3(x, H(x, z), z)); }
                for (int j = 0; j < n; j++) for (int i = 0; i < n; i++) { int a = j * (n + 1) + i, b = a + n + 1; tr.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
                MeshObject("LunarFloor", SaveMesh(SmoothFinish(v, tr), "Gc_LunarFloor"), rock, set, false);
            }
            var k = new AK();
            // the cavern's dome far overhead (inside faces), soft lamps on it like the city's distant lights
            { var (v, tr) = k.Of(rockLight); Ellipsoid(v, tr, new Vector3(c.x, gy - 60f, c.z), new Vector3(1300f, 520f, 1300f), 14, 28); for (int i = 0; i < tr.Count; i += 3) { int a = tr[i + 1]; tr[i + 1] = tr[i + 2]; tr[i + 2] = a; } }
            // the stepped pyramids and terraces of the city (clear of the ribbon)
            for (int q = 0; q < 26; q++)
            {
                float a = R() * Mathf.PI * 2f, d = 120f + R() * 380f; var at = c + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d); at.y = gy;
                if (!GcClear(t, at, 40f)) continue;
                float bw = 30f + R() * 40f; int steps = 4 + (int)(R() * 5f); float sh = 6f + R() * 5f; var (v, tr) = k.Of(q % 3 == 0 ? ancientDark : ancient); float yaw = R() * 90f;
                for (int s = 0; s < steps; s++) MBox(v, tr, at + Vector3.up * (s * sh + sh * 0.5f), new Vector3(bw * (1f - s / (float)(steps + 1)), sh, bw * (1f - s / (float)(steps + 1))), yaw, 0.04f);
                var (wv, wt) = k.Of(window); for (int s = 1; s < steps; s += 2) MBox(wv, wt, at + Vector3.up * (s * sh + sh * 0.5f) + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * (bw * (1f - s / (float)(steps + 1)) * 0.5f + 0.05f), new Vector3(bw * 0.25f, 1.2f, 0.2f), yaw, 0f);
                var (lv, lt) = k.Of(lamp); MBox(lv, lt, at + Vector3.up * (steps * sh + 1.2f), new Vector3(2f, 2.4f, 2f), yaw, 0f);
            }
            // the tunnels: ribs over the ribbon every 6 m with a lamp strip in each crown
            for (int i = 0; i < t.Count; i += 6)
            {
                if ((t.Flag[i] & T.F.Tunnel) == 0) continue;
                var p = t.P[i]; var rt = t.R[i] * Vector3.right; var up = t.R[i] * Vector3.up; float w = t.HalfW[i] + 0.8f;
                var arch = new List<Vector3>(); for (int a = 0; a <= 12; a++) { float th = Mathf.PI * a / 12f; arch.Add(p + rt * (Mathf.Cos(th) * w) + up * (Mathf.Sin(th) * 8f) - up * 0.6f); }
                var (v, tr) = k.Of(i % 12 == 0 ? metal : metalDark); Sweep(v, tr, arch, 0.35f, false, 6);
                if (i % 12 == 0) { var (lv, lt) = k.Of(lamp); MBox(lv, lt, p + up * 7.3f, new Vector3(3f, 0.15f, 0.5f), t.R[i].eulerAngles.y, 0f); }
                // the tunnel walls: rock slabs outside the ribs
                var (rv, rtt) = k.Of(rock); foreach (var sg in new[] { -1f, 1f }) MBox(rv, rtt, p + rt * sg * (w + 1.6f) + up * 3.5f, new Vector3(2.2f, 9f, 6.4f), t.R[i].eulerAngles.y, 0.1f);
            }
            // the mining pit: a ring of gantries and lamps round the shaft, cables down into it
            if (gap.Count > 0)
            {
                for (int q = 0; q < 10; q++)
                {
                    float a = q / 10f * Mathf.PI * 2f; var at = new Vector3(pitC.x + Mathf.Cos(a) * 46f, gy, pitC.z + Mathf.Sin(a) * 46f);
                    if (!GcClear(t, at, 6f)) continue;
                    var (v, tr) = k.Of(metal); Sweep(v, tr, new[] { at, at + Vector3.up * 34f }, 0.6f, false, 6); Sweep(v, tr, new[] { at + Vector3.up * 34f, new Vector3(pitC.x, gy + 34f, pitC.z) + (at - pitC).normalized * 18f }, 0.4f, false, 6);
                    var (lv, lt) = k.Of(lamp); MBox(lv, lt, at + Vector3.up * 34.6f, new Vector3(1.6f, 0.6f, 1.6f), 0f, 0f);
                }
            }
            // lamp posts outside the walls (white light points along the run) + supports + the loop's frame
            for (int i = 0; i < t.Count; i += 40)
            {
                if ((t.Flag[i] & (T.F.Loop | T.F.Gap | T.F.Tunnel)) != 0) continue; var up = t.R[i] * Vector3.up; if (up.y < 0.9f) continue;
                var rt = t.R[i] * Vector3.right; var at = t.P[i] + rt * (t.HalfW[i] + 2.5f) * (i % 80 == 0 ? 1f : -1f);
                var (v, tr) = k.Of(metalDark); Sweep(v, tr, new[] { at + Vector3.down * 1f, at + Vector3.up * 7f }, 0.18f, false, 6);
                var (lv, lt) = k.Of(lamp); MBox(lv, lt, at + Vector3.up * 7.2f, new Vector3(0.7f, 0.25f, 0.7f), 0f, 0f);
            }
            GcSupports(t, k, ancientDark, gy, 10f, 0.9f); GcLoopFrames(t, k, metal, gy);
            k.Save("GcLunar", set);
            Log($"GC LUNAR SET (R20): floor at {gy:0} m, the pit under the jump at ({pitC.x:0}, {pitC.z:0})");
            return set;
        }
    }
}
