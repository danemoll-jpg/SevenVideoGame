using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using C = Seven.CattalusLayout;
using K = Seven.CattalusCourse;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 18 — builds the scene "Cattalus" (Docs/CHAPTER_CATTALUS.md): Panther's homeworld in Kyle's misty monochrome - the public
    /// berth (docking arms clear of the bike hold), the public gate, the avenue through the cardboard quarter, the cat-tree towers,
    /// the rope-wrapped columns, the old transit tower with its hanging ball, the Cat-a-tron square (the screen that films the real
    /// level, the prize display, the steward's desk, the podium, the quiet planters), the Grand Circuit loop east of the city (the
    /// road, gates, the spiral column, the cat-tree heights, the toy boulevard, the garden descent, the stands), the meadow west of
    /// the city with the leaning tree and the low hill line of vision 7; the cats from Panther's rig with their own fur markings
    /// (baked per variant from his mesh's UVs), Duchess on her own rig (Art/Export/duchess, cleanup_cat.py CAT_NAME=duchess_v1),
    /// her racer and the trophy (Art/Export/cat_racer, cat_trophy; procedural stand-ins when missing), the sphere (code).
    /// Numbers in Runtime/CattalusLayout.cs.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const string CattalusScenePath = "Assets/Scenes/Cattalus.unity";
        const string AtmoCattalusPath = AtmoDir + "/Atmosphere_Cattalus.prefab";

        static void BuildRound18(AnimatorController sevenCtl, GameObject atmoPrefab, Seven.VoiceBank bank)
        {
            CopyVideo("vision_07_cattalus.mp4");
            var still = Path.Combine(RepoRoot, "Art", "Video", "vision_07_still.jpg");
            Directory.CreateDirectory(Path.Combine(ProjectRoot, "Assets/Resources/Cattalus"));
            if (File.Exists(still)) { File.Copy(still, Path.Combine(ProjectRoot, "Assets/Resources/Cattalus/vision_07_still.jpg"), true); AssetDatabase.ImportAsset("Assets/Resources/Cattalus/vision_07_still.jpg", ImportAssetOptions.ForceSynchronousImport); }
            var meadow = MeadowClips();
            var catCtl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "/Cat.controller");
            var catFur = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Cat_fur.mat");
            var catWhisker = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Cat_whisker.mat");
            var duchess = SetupDuchess(out var dCtl, out var dMat);
            var racer = SetupCatProp("cat_racer");
            var trophy = SetupCatProp("cat_trophy");
            var bike = AssetDatabase.LoadAssetAtPath<GameObject>(CharRoot + "/hoverbike/hoverbike_v1.fbx");
            var atmo = BuildAtmosphereCattalus(atmoPrefab);
            BuildCattalusScene(sevenCtl, bank, atmo, catCtl, catFur, catWhisker, duchess, dCtl, dMat, racer, trophy, bike, meadow);
        }

        /// <summary>QF27: the meadow cinematic = Dan's ONE edited clip Art/Video/cattalus_meadow.mp4 (78.8 s, 1920x1044, grayscale,
        /// silent) -> Assets/Video. (Round 18 expected six colour masters; the colour master Art/Video/color/cattalus_meadow_color.mov
        /// is for the heaven ending later and is never read, changed or copied here.) Missing = the in-engine stand-in.</summary>
        static UnityEngine.Video.VideoClip[] MeadowClips()
        {
            var clip = (UnityEngine.Video.VideoClip)null;
            if (File.Exists(Path.Combine(RepoRoot, "Art", "Video", "cattalus_meadow.mp4"))) { CopyVideo("cattalus_meadow.mp4"); clip = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Video/cattalus_meadow.mp4"); }
            Log($"MEADOW CLIP (QF27): {(clip != null ? $"Art/Video/cattalus_meadow.mp4 in ({clip.length:0.0} s, {clip.width}x{clip.height}) - the video cinematic plays" : "MISSING - the in-engine stand-in plays")}");
            return new[] { clip };
        }
        static string FindFfmpeg()
        {
            foreach (var dir in (System.Environment.GetEnvironmentVariable("PATH") ?? "").Split(';')) { try { var f = Path.Combine(dir.Trim(), "ffmpeg.exe"); if (File.Exists(f)) return f; } catch { } }
            var wg = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Packages");
            if (Directory.Exists(wg)) foreach (var f in Directory.GetFiles(wg, "ffmpeg.exe", SearchOption.AllDirectories)) return f;
            return null;
        }

        /// <summary>Duchess: Dan's model on Panther's rig + clips (cleanup_cat.py with CAT_NAME=duchess_v1), her own controller.</summary>
        static GameObject SetupDuchess(out AnimatorController ctl, out Material mat)
        {
            ctl = null; mat = null;
            var path = CopyExport("duchess", "duchess_v1_rigged.fbx"); if (path == null) { Log("Duchess: Art/Export/duchess/duchess_v1_rigged.fbx missing - the long-haired white stand-in (Panther's rig)"); return null; }
            mat = TexturedMat("Cat_duchess_fur", CharRoot + "/duchess/Textures/duchess_v1_basecolor.jpg", 0.3f);
            mat.SetFloat("_Rim", 0.3f); mat.SetFloat("_RimPower", 3f); EditorUtility.SetDirty(mat);
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            imp.animationType = ModelImporterAnimationType.Generic; imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; imp.importAnimation = true;
            imp.materialImportMode = ModelImporterMaterialImportMode.None; imp.animationCompression = ModelImporterAnimationCompression.KeyframeReduction; imp.SaveAndReimport();
            var loops = new[] { "idle", "walk", "sit", "wash", "sleep", "lie_down" };
            var clips = imp.defaultClipAnimations;
            foreach (var c in clips) { var n = c.takeName.Contains("|") ? c.takeName.Substring(c.takeName.LastIndexOf('|') + 1) : c.takeName; c.name = n; c.loopTime = loops.Contains(n); c.loopPose = false; }
            imp.clipAnimations = clips; imp.SaveAndReimport();
            var cpath = AnimDir + "/Duchess.controller"; AssetDatabase.DeleteAsset(cpath);
            ctl = AnimatorController.CreateAnimatorControllerAtPath(cpath);
            var sm = ctl.layers[0].stateMachine;
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")))
            { var st = sm.AddState(clip.name); st.motion = clip; if (clip.name == "idle") sm.defaultState = st; }
            Log("Duchess: Dan's model on Panther's rig (24 bones + ears) with her own clips");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        /// <summary>The racer / the trophy (Tools/Blender/cleanup_prop.py), grayscale texture; null = the procedural stand-in.</summary>
        static GameObject SetupCatProp(string kind)
        {
            var path = CopyExport(kind, kind + "_v1.fbx"); if (path == null) { Log($"{kind}: Art/Export/{kind}/{kind}_v1.fbx missing - procedural stand-in"); return null; }
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            imp.animationType = ModelImporterAnimationType.None; imp.importAnimation = false; imp.isReadable = true; imp.materialImportMode = ModelImporterMaterialImportMode.None; imp.SaveAndReimport();
            var mat = TexturedMat("Cat_" + kind, CharRoot + "/" + kind + "/Textures/" + kind + "_v1_basecolor.jpg", kind == "cat_trophy" ? 0.7f : 0.6f, kind == "cat_trophy" ? 0.6f : 0.3f);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;
            return go;
        }

        static GameObject BuildAtmosphereCattalus(GameObject atmoPrefab)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(atmoPrefab);
            var a = inst.GetComponent<Seven.Atmosphere>();
            // QF26 (3): a SUNNY, warm, lived-in cat city (Dan): clear sunlight and soft shadows, real contrast, haze only in the far
            // distance (Kyle's monochrome look: detail silhouetted against light haze). 0.18.0 had GlowRadius 80 x child scales 5-10
            // = eight glows 400-800 m wide at ~1.1 each: summed, the fog colour was ~10x white - with the NaN quads, the white-out.
            a.FogStart = 70f;
            a.GroundDensity = 0.0016f; a.GroundBase = -1f; a.GroundFalloff = 5f; a.GroundRange = 420f;
            a.ValleyDensity = 0f;
            a.DistanceDensity = 0.0009f; a.HazeHeight = 70f; a.MaxFog = 0.62f;
            a.NearBrightness = 0.6f; a.FarBrightness = 0.84f; a.BrightnessDistance = 420f; a.ZenithBrightness = 0.74f;
            a.NoiseStrength = 0.12f; a.NoiseScale = 0.02f; a.NoiseSpeed = 0.01f; a.NoiseFadeDistance = 160f;
            a.GlowIntensity = 0.12f; a.GlowRadius = 40f;
            // three soft sun-glows low in the far haze (radius 40 x scale.x, intensity 0.12 x scale.y: at most ~0.25 over the haze)
            var glows = new[] { (new Vector3(60f, 45f, 640f), new Vector3(3f, 1.6f, 1f)), (new Vector3(-420f, 40f, 260f), new Vector3(2.5f, 1.2f, 1f)), (new Vector3(480f, 40f, 300f), new Vector3(2.5f, 1.2f, 1f)) };
            int gi = 0;
            foreach (Transform c in inst.transform)
            {
                if (c.name.StartsWith("Glow")) { if (gi < glows.Length) { c.position = glows[gi].Item1; c.localScale = glows[gi].Item2; } else c.gameObject.SetActive(false); gi++; }
                // the sun: high, from behind-left of the berth -> the avenue, the square and the course in sun with soft shadows
                if (c.name == "BackLight") { c.rotation = Quaternion.Euler(47f, 215f, 0f); var l = c.GetComponent<Light>(); l.intensity = 1.55f; l.shadows = LightShadows.Soft; l.shadowStrength = 0.72f; }
                if (c.name == "FrontFill") { c.rotation = Quaternion.Euler(30f, 40f, 0f); c.GetComponent<Light>().intensity = 0.32f; }
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(inst, AtmoCattalusPath);
            Object.DestroyImmediate(inst);
            return prefab;
        }

        // ---------------------------------------------------------------- the cats: Panther's rig, their own fur
        /// <summary>A fur variant baked from Panther's mesh: every texel of his UV layout gets the variant's tone from where it lies
        /// on his body (the bind pose: length / height / side, normalised), so the markings follow the body (tabby bands across the
        /// back, a tuxedo's white chest and paws, spots); his own light details (the eyes, the nose) are kept.</summary>
        static Material CatVariant(string variant, Material baseFur)
        {
            var matPath = MatDir + "/Cat_fur_" + variant + ".mat";
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(CatFbx);
            var smr = src != null ? src.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(r => r.sharedMesh != null ? r.sharedMesh.vertexCount : 0).FirstOrDefault() : null;
            var texSrc = Path.Combine(RepoRoot, "Art", "Export", "cat", "Textures", "cat_v1_basecolor.jpg");
            if (smr == null || smr.sharedMesh == null || !File.Exists(texSrc)) return baseFur;
            var baseTex = new Texture2D(2, 2); baseTex.LoadImage(File.ReadAllBytes(texSrc));
            int N = 512; var px = new Color[N * N]; var done = new bool[N * N];
            var m = smr.sharedMesh; var v = m.vertices; var uv = m.uv; var tri = m.triangles; var b = m.bounds;
            // the body's axes: length = the longest horizontal extent of the bounds
            bool lenZ = b.size.z >= b.size.x;
            float L(Vector3 p) => lenZ ? Mathf.InverseLerp(b.min.z, b.max.z, p.z) : Mathf.InverseLerp(b.min.x, b.max.x, p.x);
            float Hh(Vector3 p) => Mathf.InverseLerp(b.min.y, b.max.y, p.y);
            float S(Vector3 p) => lenZ ? (p.x - b.center.x) / Mathf.Max(1e-4f, b.extents.x) : (p.z - b.center.z) / Mathf.Max(1e-4f, b.extents.z);
            float Tone(Vector3 p)
            {
                float l = L(p), h = Hh(p), s = S(p); float n = Mathf.PerlinNoise(l * 9f + 3f, h * 9f) * 0.08f;
                switch (variant)
                {
                    case "tabby": { float stripe = Mathf.Sin(l * 70f + Mathf.PerlinNoise(h * 5f, l * 3f) * 4f); float back = Mathf.Clamp01((h - 0.45f) * 3f); return Mathf.Lerp(0.66f, 0.3f, Mathf.Clamp01(stripe * 1.6f - 0.3f) * (0.4f + 0.6f * back)) + n; }
                    case "tuxedo": { bool white = h < 0.18f || (h < 0.5f && Mathf.Abs(s) < 0.45f && Mathf.Abs(l - 0.22f) < 0.16f) || (h < 0.5f && Mathf.Abs(s) < 0.3f && Mathf.Abs(l - 0.78f) < 0.12f); return (white ? 0.86f : 0.08f) + n * 0.5f; }
                    case "white": return 0.86f + n;
                    case "gray": return 0.46f + n;
                    case "spotted": { float sp = Mathf.PerlinNoise(l * 14f + 7f, h * 14f + s * 3f); return (sp > 0.62f ? 0.22f : 0.72f) + n; }
                    case "cream": return 0.74f + n;
                    default: return 0.3f + n;
                }
            }
            for (int i = 0; i < tri.Length; i += 3)
            {
                int a0 = tri[i], a1 = tri[i + 1], a2 = tri[i + 2];
                var u0 = uv[a0] * N; var u1 = uv[a1] * N; var u2 = uv[a2] * N;
                int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(u0.x, u1.x, u2.x)), 0, N - 1), x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(u0.x, u1.x, u2.x)), 0, N - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(u0.y, u1.y, u2.y)), 0, N - 1), y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(u0.y, u1.y, u2.y)), 0, N - 1);
                float den = (u1.y - u2.y) * (u0.x - u2.x) + (u2.x - u1.x) * (u0.y - u2.y); if (Mathf.Abs(den) < 1e-6f) continue;
                for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
                {
                    float px0 = x + 0.5f, py0 = y + 0.5f;
                    float w0 = ((u1.y - u2.y) * (px0 - u2.x) + (u2.x - u1.x) * (py0 - u2.y)) / den, w1 = ((u2.y - u0.y) * (px0 - u2.x) + (u0.x - u2.x) * (py0 - u2.y)) / den, w2 = 1f - w0 - w1;
                    if (w0 < -0.02f || w1 < -0.02f || w2 < -0.02f) continue;
                    var p = v[a0] * w0 + v[a1] * w1 + v[a2] * w2;
                    float t = Tone(p);
                    var orig = baseTex.GetPixelBilinear(px0 / N, py0 / N).grayscale;
                    if (orig > 0.33f) t = Mathf.Max(t, orig);                          // keep his light details (eyes, nose highlight)
                    px[y * N + x] = new Color(t, t, t, 1f); done[y * N + x] = true;
                }
            }
            // dilate into the gaps between UV islands (no dark seams)
            for (int it = 0; it < 4; it++) for (int y = 1; y < N - 1; y++) for (int x = 1; x < N - 1; x++) { int k = y * N + x; if (done[k]) continue; foreach (var d in new[] { 1, -1, N, -N }) if (done[k + d]) { px[k] = px[k + d]; done[k] = true; break; } }
            var tex = new Texture2D(N, N, TextureFormat.RGB24, true); tex.SetPixels(px); tex.Apply();
            var pngPath = "Assets/Art/World/Generated/cat_fur_" + variant + ".png";
            File.WriteAllBytes(Path.Combine(ProjectRoot, pngPath), tex.EncodeToPNG()); AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceSynchronousImport);
            var mat = Dissolve("Cat_fur_" + variant, Color.white, 0.5f, false);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath)); mat.SetFloat("_Rim", 0.25f); mat.SetFloat("_RimPower", 3.2f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
        static Seven.CatFolk SpawnFolk(string name, Vector3 at, Vector3 face, float scale, AnimatorController ctl, Material fur, Material whisker, Transform parent, string clip = "idle")
        {
            var root = SpawnCat(name, at, face, ctl, fur, whisker, out var anim);
            root.transform.SetParent(parent, true); root.transform.localScale = Vector3.one * scale;
            var f = root.AddComponent<Seven.CatFolk>(); f.Anim = anim; f.Role = name;
            root.AddComponent<Seven.CatPoseOverlay>();
            return f;
        }
        /// <summary>A small accessory on a cat's bone (the steward's ribbon badge, the announcer's bow tie).</summary>
        static void Accessory(Seven.CatFolk f, string bone, Material m, string meshName, System.Action<List<Vector3>, List<int>> shape)
        {
            var b = f.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == bone); if (b == null) return;
            var v = new List<Vector3>(); var t2 = new List<int>(); shape(v, t2);
            var g = MeshObject(meshName, SaveMesh(SmoothFinish(v, t2), "Cat_" + meshName), m, b, false);
            g.transform.position = b.position; g.transform.rotation = f.transform.rotation;
            g.transform.localScale = Vector3.one / Mathf.Max(0.01f, b.lossyScale.x) * f.transform.localScale.x;
        }

        static void BuildCattalusScene(AnimatorController sevenCtl, Seven.VoiceBank bank, GameObject atmoP, AnimatorController catCtl, Material catFur, Material catWhisker,
            GameObject duchessModel, AnimatorController dCtl, Material dMat, GameObject racerModel, GameObject trophyModel, GameObject bike, UnityEngine.Video.VideoClip[] meadowClips)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null; RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.36f, 0.36f, 0.36f);   // QF26 (3): the sun does the work; shadows stay readable
            var shs = new SphericalHarmonicsL2(); shs.AddAmbientLight(RenderSettings.ambientLight); RenderSettings.ambientProbe = shs;
            var atmo = (GameObject)PrefabUtility.InstantiatePrefab(atmoP); atmo.name = "Atmosphere_Cattalus";
            var world = new GameObject("Cattalus").transform;
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
            m.glow.EnableKeyword("_EMISSION"); m.glow.SetColor("_EmissionColor", Color.white * 2.2f); EditorUtility.SetDirty(m.glow);
            m.glowSoft.EnableKeyword("_EMISSION"); m.glowSoft.SetColor("_EmissionColor", Color.white * 0.35f); EditorUtility.SetDirty(m.glowSoft);
            foreach (var x in new[] { m.card, m.cardDark, m.cardLight, m.tape, m.fabric, m.leaf, m.paper }) { x.SetFloat("_Cull", 0f); EditorUtility.SetDirty(x); }

            // ---------------------------------------------------------------- the ground
            foreach (var z in C.Zones.Where(z => z.Name != "Meadow" && !z.Name.StartsWith("Gate")))
                MGround("Ground_" + z.Name, new Vector3(z.X0, -0.4f, z.Z0), new Vector3(z.X1, 0f, z.Z1), z.Name == "Square" || z.Name == "Berth" ? m.stone : m.ground, world);
            MGround("Ground_GateZ", new Vector3(-4f, -0.4f, 23.8f), new Vector3(4f, 0f, 28.8f), m.stone, world);
            MGround("Ground_Base", new Vector3(-120f, -0.62f, -120f), new Vector3(700f, -0.22f, 900f), m.grassDark, world);
            // the meadow: rolling terrain + tall grass; the low hill line beyond (the hills of vision 7)
            {
                var v = new List<Vector3>(); var t = new List<int>(); int nx = 130, nz = 140; float x0 = C.MeadowX0 - 160f, x1 = C.MeadowX1 + 2f, z0 = C.MeadowZ0 - 140f, z1 = C.MeadowZ1 + 140f;
                for (int j = 0; j <= nz; j++) for (int i = 0; i <= nx; i++) { float x = Mathf.Lerp(x0, x1, i / (float)nx), z = Mathf.Lerp(z0, z1, j / (float)nz); v.Add(new Vector3(x, C.MeadowHeight(x, z), z)); }
                for (int j = 0; j < nz; j++) for (int i = 0; i < nx; i++) { int a = j * (nx + 1) + i, bb = a + nx + 1; t.AddRange(new[] { a, bb, a + 1, a + 1, bb, bb + 1 }); }
                MeshObject("MeadowTerrain", SaveMesh(SmoothFinish(v, t), "Cat_MeadowTerrain"), m.grass, world, false);
                var grass = new List<GrassPatch> { new GrassPatch { Name = "the meadow", Area = new Rect(C.MeadowX0, C.MeadowZ0, C.MeadowX1 - C.MeadowX0, C.MeadowZ1 - C.MeadowZ0), Density = 3.2f, MinH = 0.35f, MaxH = 0.8f, Height = (x, z) => Mathf.Abs(x - C.TreeView.x) < 3f && Mathf.Abs(z - C.TreeView.z) < 3f ? float.NaN : C.MeadowHeight(x, z) } };
                // QF26 (3): the rising rim of the meadow carries grass too (0.18.0 left it a bare, flat-shaded slope - a giant blank surface
                // behind the running shots); thinner than the meadow itself
                foreach (var (nm, rc) in new[] { ("the meadow rim west", new Rect(x0, z0, C.MeadowX0 - x0, z1 - z0)), ("the meadow rim south", new Rect(C.MeadowX0, z0, C.MeadowX1 - C.MeadowX0, C.MeadowZ0 - z0)), ("the meadow rim north", new Rect(C.MeadowX0, C.MeadowZ1, C.MeadowX1 - C.MeadowX0, z1 - C.MeadowZ1)) })
                    grass.Add(new GrassPatch { Name = nm, Area = rc, Density = 0.45f, MinH = 0.4f, MaxH = 1.0f, Height = (x, z) => C.MeadowHeight(x, z) });
                Log($"meadow grass: {BuildGrass(world, grass, 18)} blades");
                var hk = new AK();
                { var (hv, ht) = hk.Of(m.hill); var hr = new System.Random(4); for (int i = 0; i < 22; i++) { float a = Mathf.Lerp(100f, 260f, i / 21f) * Mathf.Deg2Rad, r = 300f + (i % 3) * 90f + (float)hr.NextDouble() * 40f; var c = new Vector3(-220f, 0f, 160f) + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r); float w = 110f + (float)hr.NextDouble() * 80f; Ellipsoid(hv, ht, c + Vector3.down * 18f, new Vector3(w, 34f + (i % 3) * 16f, w * 0.7f), 10, 20); } }
                hk.Save("MeadowHills", world, false, false);
                var tk = new AK(); LeaningTree(tk, m, C.TreeAt, 1.5f); tk.Save("LeaningTree", world, true);
            }

            // ---------------------------------------------------------------- the berth, the gate, the avenue, the quarter
            var city = new GameObject("City").transform; city.SetParent(world, false);
            var ck = new AK();
            { var (v, t) = ck.Of(m.stone); Append(v, t, Lathe(new[] { new Vector2(0f, 0.02f), new Vector2(22f, 0.02f), new Vector2(22.6f, -0.3f) }, 0f, 360f, 48, null, Vector3.zero)); }
            { var (gv, gt) = ck.Of(m.glowSoft); Append(gv, gt, Lathe(new[] { new Vector2(13.6f, 0.04f), new Vector2(14.2f, 0.04f) }, 0f, 360f, 48, null, Vector3.zero)); }
            // the gate: posts, an arch with a hanging ornament, the berth's low cardboard wall either side
            ScratchPost(ck, m, C.GateAt + new Vector3(-4.6f, 0f, 0f), 0.5f, 3.4f, 1); ScratchPost(ck, m, C.GateAt + new Vector3(4.6f, 0f, 0f), 0.5f, 3.4f, 2);
            { var (av, at2) = ck.Of(m.cardDark); var arch = Enumerable.Range(0, 13).Select(i => C.GateAt + new Vector3(Mathf.Lerp(-4.6f, 4.6f, i / 12f), 3.4f + Mathf.Sin(i / 12f * Mathf.PI) * 1.2f, 0f)).ToList(); Sweep(av, at2, arch, 0.12f, false, 6); }
            foreach (var sg in new[] { -1f, 1f }) CardboardBox(ck, m, C.GateAt + new Vector3(sg * 14.5f, 0f, 0f), new Vector3(19f, 1.3f, 0.8f), 0f, 11 + (int)sg, false, false);
            // the avenue: mansions and cat trees on both sides (the lanes behind them), terraces, columns
            int seed = 20;
            foreach (var sg in new[] { -1f, 1f })
            {
                for (float z = 34f; z < 114f; z += 10f)
                {
                    float x = sg * (26f + (seed % 3) * 1.5f);
                    if (sg < 0f && Mathf.Abs(z - C.Vignettes[0].at.z) < 9f) { seed++; continue; }        // the vignette's own mansion stands there
                    if ((seed % 4) == 0) CatTree(ck, m, new Vector3(x, 0f, z), 18f + (seed % 5) * 3f, seed); else CardboardMansion(ck, m, new Vector3(x, 0f, z), sg > 0 ? -90f : 90f, 2 + seed % 3, seed);
                    seed++;
                }
                ScratchPost(ck, m, new Vector3(sg * 15.6f, 0f, 64f), 0.9f, 9f, 3 + (int)sg); ScratchPost(ck, m, new Vector3(sg * 15.6f, 0f, 98f), 0.9f, 9f, 5 + (int)sg);
                Terrace(ck, m, new Vector3(sg * 25f, 0f, 78f), sg > 0 ? -90f : 90f, 8f, 5f, 3);
            }
            // the vignette sets: the mansion's tiny box, the bench, the ledge, the meeting, the counter
            CardboardMansion(ck, m, C.Vignettes[0].at + new Vector3(-4f, 0f, 0f), 90f, 3, 77);
            CardboardBox(ck, m, C.Vignettes[0].at + new Vector3(0.4f, 0f, 0f), new Vector3(0.55f, 0.4f, 0.45f), 0f, 78, false, true);
            { var (bv, bt) = ck.Of(m.wood); MBox(bv, bt, C.Vignettes[3].at + new Vector3(0f, 0.25f, 0f), new Vector3(0.6f, 0.5f, 2.6f), 0f, 0.01f); var (gv, gt) = ck.Of(m.glowSoft); Ellipsoid(gv, gt, C.Vignettes[3].at + new Vector3(0f, 0.52f, 0.05f), new Vector3(0.32f, 0.004f, 0.42f), 2, 12); }   // the bench + its one patch of sun
            { var (lv, lt) = ck.Of(m.cardLight); MBox(lv, lt, C.Vignettes[4].at + new Vector3(0f, 0.45f, 0f), new Vector3(0.28f, 0.9f, 18f), 0f, 0.01f); }   // the narrow ledge beside a perfectly good walkway
            { var (tv, tt) = ck.Of(m.wood); Append(tv, tt, Lathe(new[] { new Vector2(0f, 0.55f), new Vector2(0.9f, 0.55f), new Vector2(0.9f, 0.6f), new Vector2(0f, 0.6f) }, 0f, 360f, 16, null, C.Vignettes[5].at)); Append(tv, tt, Lathe(new[] { new Vector2(0.1f, 0f), new Vector2(0.1f, 0.55f) }, 0f, 360f, 8, null, C.Vignettes[5].at)); ScratchPost(ck, m, C.Vignettes[5].at + new Vector3(1.8f, 0f, 1.2f), 0.18f, 3.2f, 9); }
            { var (cv, ct) = ck.Of(m.card); MBox(cv, ct, C.Vignettes[6].at + new Vector3(0f, 0.5f, 0f), new Vector3(0.7f, 1.0f, 2.2f), 0f, 0.01f); }   // the counter
            Fountain(ck, m, C.FountainAt);
            // sunlit patches: soft bright pools on the paving
            { var (gv, gt) = ck.Of(m.glowSoft); foreach (var sp in C.SunPatches) Ellipsoid(gv, gt, sp + Vector3.up * 0.02f, new Vector3(1.6f, 0.005f, 1.2f), 2, 16); }
            // the transit route: a rail on rope-wrapped posts winding over the avenue and round the square
            { var loop = new List<Vector3>(); for (int i = 0; i < C.TransitLoop.Length; i++) { var a0 = C.TransitLoop[i]; var a1 = C.TransitLoop[(i + 1) % C.TransitLoop.Length]; for (int k = 0; k < 8; k++) loop.Add(Vector3.Lerp(a0, a1, k / 8f)); }
              var (rv, rt) = ck.Of(m.metal); Sweep(rv, rt, loop, 0.28f, true, 8);
              for (int i = 0; i < loop.Count; i += 4) { var p = loop[i]; ScratchPost(ck, m, new Vector3(p.x, 0f, p.z), 0.35f, p.y - 0.3f, 600 + i); } }
            ck.Save("CityAvenue", city);
            var transitBall = TransitTower(ck, m, C.TransitTower, C.TransitTowerH, city); ck.Save("TransitTower", city);
            { var pod = new GameObject("TransitPod").transform; pod.SetParent(city, false); var pv = new List<Vector3>(); var pt = new List<int>(); Ellipsoid(pv, pt, Vector3.down * 0.9f, new Vector3(1.0f, 0.75f, 1.9f), 6, 12); Sweep(pv, pt, new[] { Vector3.zero, Vector3.down * 0.3f }, 0.08f, false, 4); MeshObject("Pod", SaveMesh(SmoothFinish(pv, pt), "Cat_TransitPod"), m.cardLight, pod, true); var gv2 = new List<Vector3>(); var gt2 = new List<int>(); Ellipsoid(gv2, gt2, Vector3.down * 0.75f + Vector3.forward * 1.2f, new Vector3(0.7f, 0.25f, 0.6f), 4, 10); MeshObject("PodWindow", SaveMesh(SmoothFinish(gv2, gt2), "Cat_TransitPodWindow"), m.glowSoft, pod, false); pod.gameObject.AddComponent<Seven.CatTransit>().Speed = 7f; }
            transitBall.gameObject.AddComponent<Seven.CatSwing>().Amp = 6f;
            // the distant skyline: cat-tree towers and stacked mansions fading into the haze all round
            var sk = new AK(); var srnd = new System.Random(31);
            for (int i = 0; i < 70; i++)
            {
                float a = (float)srnd.NextDouble() * 360f, r = 160f + (float)srnd.NextDouble() * 320f; var p = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * r, 0f, 140f + Mathf.Sin(a * Mathf.Deg2Rad) * r);
                if (p.x < -110f && p.z > 0f && p.z < 300f) continue;                                    // never in the meadow
                if (Vector3.Distance(new Vector3(p.x, 0f, p.z), K.At(K.Nearest(p, 0, K.Count))) < 18f) continue;   // never on the course
                if (i % 3 == 0) CatTree(sk, m, p, 30f + (float)srnd.NextDouble() * 40f, 100 + i, 2.2f); else CardboardMansion(sk, m, p, (float)srnd.NextDouble() * 360f, 3 + i % 4, 200 + i);
            }
            sk.Save("Skyline", city, false, false);
            // QF26 (3): the city reads dense and full at the scale of the shots (Dan: "no buildings reduced to specks"): every block
            // round the avenue, the square and the berth approach is built up - stacked cardboard mansions, cat-tree towers and
            // scratching-post columns, tallest toward the square - never on the course, a walkable zone, the transit tower or the meadow
            {
                var fk = new AK(); var frnd = new System.Random(57); int built = 0;
                bool Free(Vector3 p, float r)
                {
                    foreach (var z in C.Zones) if (p.x > z.X0 - r - 3f && p.x < z.X1 + r + 3f && p.z > z.Z0 - r - 3f && p.z < z.Z1 + r + 3f) return false;
                    // in PLAN: the circuit climbs tens of metres (the spiral) - nothing may stand under an elevated section either
                    for (int ci = 0; ci < K.Count; ci += 2) { var cp = K.At(ci); if (new Vector2(cp.x - p.x, cp.z - p.z).sqrMagnitude < (r + 14f) * (r + 14f)) return false; }
                    if (new Vector2(p.x - C.TransitTower.x, p.z - C.TransitTower.z).magnitude < r + 10f) return false;
                    if (p.x < C.MeadowX1 + 6f && p.z > C.MeadowZ0 - 10f) return false;
                    if (new Vector2(p.x, p.z).magnitude < 40f) return false;                                   // the berth's apron + the saucer's way in
                    return true;
                }
                for (float z = -110f; z <= 250f; z += 15f)
                    for (float x = -105f; x <= 150f; x += 15f)
                    {
                        var p = new Vector3(x + ((float)frnd.NextDouble() - 0.5f) * 6f, 0f, z + ((float)frnd.NextDouble() - 0.5f) * 6f);
                        float roll = (float)frnd.NextDouble(); float r = roll < 0.7f ? 6f : 4f;
                        if (!Free(p, r)) continue;
                        float nearSquare = Mathf.Clamp01(1f - new Vector2(p.x, p.z - 146f).magnitude / 160f);
                        if (roll < 0.62f) CardboardMansion(fk, m, p, (float)frnd.NextDouble() * 360f, 2 + (int)(nearSquare * 3f) + frnd.Next(0, 2), 900 + built);
                        else if (roll < 0.84f) CatTree(fk, m, p, 16f + nearSquare * 14f + (float)frnd.NextDouble() * 8f, 1300 + built, 1.4f);
                        else { if (!Free(p + new Vector3(3.5f, 0f, 0f), 5f)) continue; ScratchPost(fk, m, p, 0.8f + (float)frnd.NextDouble() * 0.4f, 8f + (float)frnd.NextDouble() * 6f, 1700 + built); Terrace(fk, m, p + new Vector3(3.5f, 0f, 0f), (float)frnd.NextDouble() * 360f, 7f, 4.4f, 2 + frnd.Next(0, 2)); }
                        built++;
                    }
                fk.Save("CityBlocks", city, false, true);
                Log($"QF26 (3) city blocks: {built} homes, cat trees and post-and-terrace groups filling the blocks round the avenue, the square and the berth approach");
            }

            // ---------------------------------------------------------------- the square: the Cat-a-tron, the prizes, the desk, the podium
            var sq = new GameObject("Square").transform; sq.SetParent(world, false);
            var qk = new AK();
            { var (v, t) = qk.Of(m.cardDark); MBox(v, t, C.ScreenAt + new Vector3(0f, 0f, 0.6f), new Vector3(C.ScreenW + 2f, C.ScreenH + 2f, 0.8f), 0f, 0.01f); }
            ScratchPost(qk, m, C.ScreenAt + new Vector3(-C.ScreenW * 0.5f - 1.8f, -C.ScreenAt.y, 0.6f), 1.1f, C.ScreenAt.y + C.ScreenH * 0.5f + 2f, 12); ScratchPost(qk, m, C.ScreenAt + new Vector3(C.ScreenW * 0.5f + 1.8f, -C.ScreenAt.y, 0.6f), 1.1f, C.ScreenAt.y + C.ScreenH * 0.5f + 2f, 13);
            foreach (var x in new[] { -1f, 1f }) CatTree(qk, m, new Vector3(x * 30f, 0f, 176f), 26f, 40 + (int)x, 1.2f);
            // the prize display: a round plinth (the trophy turns on it), the retreat's low platform
            { var (v, t) = qk.Of(m.stone); Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.9f, 0f), new Vector2(0.8f, 0.6f), new Vector2(0f, 0.6f) }, 0f, 360f, 20, null, C.TrophyAt)); Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(1.3f, 0f), new Vector2(1.25f, 0.2f), new Vector2(0f, 0.2f) }, 0f, 360f, 20, null, C.RetreatAt)); }
            // the steward's desk (cardboard, a registration screen on it), the podium, the low wall, the stands, the planters
            CardboardBox(qk, m, C.DeskAt, new Vector3(2.6f, 0.9f, 1.2f), 180f, 55, false, false);
            { var (v, t) = qk.Of(m.cardDark); MBox(v, t, C.DeskAt + new Vector3(0f, 1.25f, 0.3f), new Vector3(1.2f, 0.7f, 0.06f), 0f, 0.01f); var (gv, gt) = qk.Of(m.glowSoft); MBox(gv, gt, C.DeskAt + new Vector3(0f, 1.25f, 0.26f), new Vector3(1.05f, 0.56f, 0.02f), 0f, 0.001f); }
            Terrace(qk, m, C.PodiumAt, 180f, 5f, 2.6f, 2);
            { var (v, t) = qk.Of(m.cardLight); MBox(v, t, C.LowWall + new Vector3(0f, C.LowWallH * 0.5f, 0f), new Vector3(0.7f, C.LowWallH, 7f), 0f, 0.01f); }
            for (int i = 0; i < 3; i++) Terrace(qk, m, new Vector3(37.6f, 0f, 122f + i * 9f), -90f, 8f, 4.4f, 3);
            { var (v, t) = qk.Of(m.cardLight); foreach (var p in new[] { C.QuietCorner + new Vector3(0.9f, 0f, 1.2f), C.QuietCorner + new Vector3(-2.4f, 0f, 1.6f), C.QuietCorner + new Vector3(2.8f, 0f, -1.4f) }) Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(1.1f, 0f), new Vector2(1.2f, 0.62f), new Vector2(0f, 0.62f) }, 0f, 360f, 18, null, p)); var (lv, lt) = qk.Of(m.leaf); foreach (var p in new[] { C.QuietCorner + new Vector3(-2.4f, 0.6f, 1.6f), C.QuietCorner + new Vector3(2.8f, 0.6f, -1.4f) }) Ellipsoid(lv, lt, p + Vector3.up * 0.6f, new Vector3(1f, 0.8f, 1f), 5, 8); }
            // the start / finish gantry with its four lights
            { var (v, t) = qk.Of(m.sisal); var s0 = K.StartAt; foreach (var x in new[] { -K.HalfWidth - 1f, K.HalfWidth + 1f }) Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.4f, 0f), new Vector2(0.35f, 7.6f), new Vector2(0f, 7.8f) }, 0f, 360f, 10, null, s0 + new Vector3(x, 0f, 0f))); var (bv, bt) = qk.Of(m.cardDark); MBox(bv, bt, s0 + new Vector3(0f, 7.4f, 0f), new Vector3(K.HalfWidth * 2f + 3f, 0.9f, 0.6f), 0f, 0.01f); }
            qk.Save("SquareSet", sq);
            var lights = new List<Renderer>(); for (int i = 0; i < 4; i++) { var lv2 = new List<Vector3>(); var lt2 = new List<int>(); Ellipsoid(lv2, lt2, K.StartAt + new Vector3(-2.4f + i * 1.6f, 7.4f, -0.4f), Vector3.one * 0.32f, 6, 10); lights.Add(MeshObject("StartLight" + i, SaveMesh(SmoothFinish(lv2, lt2), "Cat_StartLight" + i), new Material(m.glowSoft) { name = "Cat_start_light" + i }, sq, false).GetComponent<Renderer>()); }
            foreach (var (p, r2) in new[] { (new Vector3(0f, 8f, 140f), 40f), (new Vector3(0f, 8f, 70f), 34f), (new Vector3(0f, 6f, 10f), 30f), (new Vector3(-30f, 6f, 130f), 20f), (new Vector3(45f, 8f, 150f), 26f) }) AstLamp("CatLamp_" + (int)p.x + "_" + (int)p.z, p, r2, 1.1f, m.glow, m.cardDark, sq, 0.3f);
            // the screen face (its texture = the screen camera's render), the screen camera, the studio below the city (the announcer's desk)
            var face = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(face.GetComponent<Collider>()); face.name = "CatatronFace"; face.transform.SetParent(sq, false);
            face.transform.position = C.ScreenAt + new Vector3(0f, 0f, 0.15f); face.transform.rotation = Quaternion.LookRotation(Vector3.forward); face.transform.localScale = new Vector3(C.ScreenW, C.ScreenH, 1f);
            var faceMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Cat_screen.mat"); if (faceMat == null) { faceMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(faceMat, MatDir + "/Cat_screen.mat"); }
            faceMat.shader = Shader.Find("Universal Render Pipeline/Unlit"); faceMat.SetColor("_BaseColor", Color.white); faceMat.SetFloat("_Cull", 0f); EditorUtility.SetDirty(faceMat); face.GetComponent<Renderer>().sharedMaterial = faceMat;   // unlit: the picture itself, never washed to white
            var scg = new GameObject("CatatronCamera"); scg.transform.SetParent(sq, false); var scam = scg.AddComponent<Camera>(); scam.fieldOfView = 40f; scam.nearClipPlane = 0.05f; scam.farClipPlane = 900f; scam.enabled = false; scam.clearFlags = CameraClearFlags.SolidColor; scam.backgroundColor = Gray(0.6f);
            var studio = new GameObject("CatatronStudio").transform; studio.SetParent(world, false); studio.position = new Vector3(0f, 300f, 0f);
            { var stk = new AK(); var (v, t) = stk.Of(m.cardDark); MBox(v, t, new Vector3(0f, 300f + 0.4f, 0f), new Vector3(1.6f, 0.8f, 0.7f), 0f, 0.01f); var (bv, bt) = stk.Of(m.cardLight); MBox(bv, bt, new Vector3(0f, 300f + 1.4f, -1.2f), new Vector3(4f, 2.8f, 0.1f), 0f, 0.01f); var (gv, gt) = stk.Of(m.glowSoft); for (int i = 0; i < 5; i++) Ellipsoid(gv, gt, new Vector3(-1.6f + i * 0.8f, 300f + 2.4f, -1.14f), Vector3.one * 0.12f, 4, 8); stk.Save("Studio", studio); PointLight("StudioLight", new Vector3(0f, 302.5f, 1.5f), 6f, 2.2f, studio); }
            var desk = new GameObject("AnnouncerDesk").transform; desk.SetParent(studio, false); desk.position = new Vector3(0f, 300f + 0.8f, -0.25f); desk.rotation = Quaternion.LookRotation(Vector3.forward);

            // ---------------------------------------------------------------- the prizes: the trophy (+ the sphere in its ring), the Deluxe Retreat
            var em = ElarimMaterials(false);
            var trophy = new GameObject("Trophy").transform; trophy.SetParent(world, false); trophy.position = C.TrophyAt + Vector3.up * 0.6f;
            Vector3 ringC; float ringR;
            if (trophyModel != null)
            {
                var tm = (GameObject)PrefabUtility.InstantiatePrefab(trophyModel); tm.name = "TrophyModel"; tm.transform.SetParent(trophy, false);
                var rj = Path.Combine(RepoRoot, "Art", "Export", "cat_trophy", "cat_trophy_ring.json");
                float h0 = 1f, cy = 0.7f, rr = 0.1f;
                if (File.Exists(rj)) { var j = JsonUtility.FromJson<TrophyRing>(File.ReadAllText(rj)); h0 = j.height; cy = j.centre[2]; rr = j.inner_radius; }
                float k = (Seven.RuneSizes.SphereR * 0.86f) / Mathf.Max(0.01f, rr);                 // the ring holds the hand-sized sphere
                tm.transform.localScale = Vector3.one * k; ringC = trophy.position + Vector3.up * cy * k; ringR = rr * k;
                Log($"trophy: Dan's model scaled x{k:0.00} ({h0 * k:0.00} m tall) so its top ring (r {rr * k:0.000} m) holds the sphere");
            }
            else
            {
                // the procedural stand-in after cat_trophy_ref.png: a paw-print base, rope columns, an urn with a rope bow, tail handles, four prongs over an open ring
                var tk2 = new AK(); var bp = trophy.position;
                { var (v, t) = tk2.Of(m.metal); Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.34f, 0f), new Vector2(0.32f, 0.12f), new Vector2(0.26f, 0.16f), new Vector2(0.26f, 0.2f), new Vector2(0f, 0.2f) }, 0f, 360f, 24, null, bp)); }
                { var (v, t) = tk2.Of(m.stone); for (int i = 0; i < 4; i++) { var cpos = bp + Quaternion.Euler(0f, i * 90f + 45f, 0f) * Vector3.forward * 0.18f; Append(v, t, Lathe(new[] { new Vector2(0f, 0.2f), new Vector2(0.04f, 0.2f), new Vector2(0.04f, 0.46f), new Vector2(0f, 0.46f) }, 0f, 360f, 8, null, cpos)); } Append(v, t, Lathe(new[] { new Vector2(0f, 0.46f), new Vector2(0.27f, 0.46f), new Vector2(0.26f, 0.5f), new Vector2(0f, 0.5f) }, 0f, 360f, 24, null, bp)); }
                { var (v, t) = tk2.Of(m.metal); Append(v, t, Lathe(new[] { new Vector2(0f, 0.5f), new Vector2(0.12f, 0.52f), new Vector2(0.2f, 0.62f), new Vector2(0.17f, 0.74f), new Vector2(0.13f, 0.8f), new Vector2(0.11f, 0.84f), new Vector2(0.105f, 0.86f) }, 0f, 360f, 24, null, bp)); foreach (var sg in new[] { -1f, 1f }) Sweep(v, t, new[] { bp + new Vector3(sg * 0.18f, 0.6f, 0f), bp + new Vector3(sg * 0.3f, 0.66f, 0f), bp + new Vector3(sg * 0.34f, 0.8f, 0f), bp + new Vector3(sg * 0.28f, 0.92f, 0f) }, 0.035f, false, 6); for (int i = 0; i < 4; i++) { var d = Quaternion.Euler(0f, i * 90f + 45f, 0f) * Vector3.forward; Sweep(v, t, new[] { bp + d * 0.1f + Vector3.up * 0.86f, bp + d * 0.14f + Vector3.up * 0.98f, bp + d * 0.12f + Vector3.up * 1.12f }, 0.012f, false, 5); } }
                tk2.Save("TrophyStandIn", trophy, true);
                ringC = bp + Vector3.up * 0.86f; ringR = 0.105f;
            }
            // the mark the records share, on the setting (the dodecahedron's mark: Kyle's placeholder engraving)
            var markGo = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(markGo.GetComponent<Collider>()); markGo.name = "SettingMark"; markGo.transform.SetParent(trophy, false);
            markGo.transform.position = ringC + Vector3.down * 0.16f + Vector3.back * (ringR + 0.03f); markGo.transform.rotation = Quaternion.LookRotation(Vector3.forward); markGo.transform.localScale = Vector3.one * 0.09f; markGo.GetComponent<Renderer>().sharedMaterial = em.mark;
            var sphere = new GameObject("Sphere").transform; sphere.SetParent(trophy, false);
            sphere.position = ringC + Vector3.up * Seven.RuneSizes.SphereR * 0.45f;   // in its cradle (the trophy carries it until the touch releases it)
            var sphereMat = Dissolve("Cat_sphere", Gray(0.86f), 0.75f, false); sphereMat.SetFloat("_Rim", 0.35f); EditorUtility.SetDirty(sphereMat);
            MeshObject("Body", SaveMesh(Seven.CattalusSphere.Mesh(Seven.RuneSizes.SphereR), "Cat_SphereBody"), sphereMat, sphere, true);
            var sl = MeshObject("Lines", SaveMesh(Seven.CattalusSphere.LinesMesh(Seven.RuneSizes.SphereR), "Cat_SphereLines"), new Material(m.glowSoft) { name = "Cat_sphere_lines" }, sphere, false);
            var spin = trophy.gameObject.AddComponent<Seven.CatSpin>(); spin.DegPerSec = 14f; spin.Also = sphere;
            // the Deluxe Retreat: premium corrugation, an entry hole, an inner shelf, crinkled packing paper
            var retreat = new GameObject("DeluxeRetreat").transform; retreat.SetParent(world, false); retreat.position = C.RetreatAt + Vector3.up * 0.2f; retreat.rotation = Quaternion.LookRotation(Vector3.back);
            var rk = new AK(); CardboardBox(rk, m, Vector3.zero, new Vector3(1.3f, 0.95f, 1.0f), 0f, 61, true, true);
            { var (v, t) = rk.Of(m.cardDark); MBox(v, t, new Vector3(0f, 0.55f, 0.1f), new Vector3(1.1f, 0.04f, 0.7f), 0f, 0.002f); }
            foreach (var go2 in rk.Save("Retreat", retreat, false)) go2.transform.localPosition = Vector3.zero;
            var paper = new GameObject("Paper").transform; paper.SetParent(retreat, false); paper.localPosition = new Vector3(0f, 0.1f, 0f);
            { var pv = new List<Vector3>(); var pt = new List<int>(); var prnd = new System.Random(5); for (int i = 0; i < 26; i++) Ellipsoid(pv, pt, new Vector3(((float)prnd.NextDouble() - 0.5f) * 1.0f, (float)prnd.NextDouble() * 0.12f, ((float)prnd.NextDouble() - 0.5f) * 0.7f), new Vector3(0.12f, 0.06f, 0.1f), 3, 6); MeshObject("PaperCrinkle", SaveMesh(Finish(pv, pt), "Cat_PaperCrinkle"), m.paper, paper, false); }
            // the vision insert: an open wooden box with the dodecahedron's mark on its side (shown only for the insert)
            var insert = new GameObject("VisionInsert").transform; insert.SetParent(world, false); insert.position = new Vector3(0f, 320f, 40f);
            { var ik = new AK(); var (v, t) = ik.Of(m.wood); MBox(v, t, Vector3.up * 0.18f, new Vector3(0.5f, 0.36f, 0.4f), 0f, 0.002f); ik.Save("InsertBox", insert, false); var mk = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(mk.GetComponent<Collider>()); mk.name = "Mark"; mk.transform.SetParent(insert, false); mk.transform.localPosition = new Vector3(0f, 0.2f, -0.21f); mk.transform.localRotation = Quaternion.LookRotation(Vector3.forward); mk.transform.localScale = Vector3.one * 0.22f; mk.GetComponent<Renderer>().sharedMaterial = em.mark; PointLight("InsertLight", insert.position + new Vector3(0.3f, 0.8f, -0.9f), 3f, 1.6f, insert); var mg = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(mg.GetComponent<Collider>()); mg.transform.SetParent(insert, false); mg.transform.localPosition = new Vector3(0f, 0.6f, 1.2f); mg.transform.localScale = new Vector3(6f, 4f, 1f); mg.GetComponent<Renderer>().sharedMaterial = m.glowSoft; }
            insert.gameObject.SetActive(false);

            // ---------------------------------------------------------------- the Grand Circuit
            var course = new GameObject("GrandCircuit").transform; course.SetParent(world, false);
            CourseRoad(course, m);
            var gidx = K.Gates(); var gates = new List<Renderer>(); for (int i = 0; i < gidx.Count; i++) gates.Add(CourseGate(course, m, gidx[i], i));
            var toys = K.Toys(); var toyT = new List<Transform>(); for (int i = 0; i < toys.Count; i++) toyT.Add(CourseToy(course, m, toys[i].i, i));
            // the sections' sets: cardboard blocks along the quarter, cat trees under the heights, planters down the garden
            var csk = new AK(); var crnd = new System.Random(9);
            for (int i = 0; i < K.Count; i += 9)
            {
                var p = K.At(i); var side = Vector3.Cross(Vector3.up, K.Dir(i)); int sec = K.Section[i];
                foreach (var sg in new[] { -1f, 1f })
                {
                    var q = new Vector3(p.x, 0f, p.z) + side * sg * (K.HalfWidth + 9f + (float)crnd.NextDouble() * 6f);
                    if (Vector3.Distance(q, K.At(K.Nearest(q, i, 80))) < K.HalfWidth + 3f) continue;
                    if (q.x < 46f && q.z > 110f && q.z < 180f) continue;                                  // the square's side
                    if (sec == 0 && i % 18 == 0) CardboardMansion(csk, m, q, Quaternion.LookRotation(-side * sg).eulerAngles.y, 2 + i % 3, 300 + i);
                    else if (sec == 2 && i % 18 == 0) CatTree(csk, m, q, p.y + 8f, 400 + i, 1.4f);
                    else if (sec == 4 && i % 9 == 0) { var (lv, lt) = csk.Of(m.leaf); Ellipsoid(lv, lt, new Vector3(q.x, C.MeadowHeight(0f, 0f) * 0f + 1.4f, q.z), new Vector3(2.4f, 1.6f, 2.4f), 5, 8); }
                }
            }
            csk.Save("CourseSets", course);
            // the stands' spectators: some animated cats near the finish, the rest as soft silhouettes
            // (the cats come below with the others)

            // ---------------------------------------------------------------- the saucer at the berth, ground, 7, camera, the bike
            var saucer = PlaceSaucer(world, C.SaucerAt);
            var beam = saucer.GetComponent<Seven.SaucerBeam>();
            CheckSaucer("Cattalus", saucer);
            // the docking arms: two curved arms rising from the plaza to the rim, on the sides away from the bike hold and the beam
            var hold = saucer.GetComponentInChildren<Seven.SaucerHold>();
            var hdir = hold != null ? Vector3.ProjectOnPlane(hold.Dir, Vector3.up).normalized : Vector3.right;
            { var ak = new AK(); var (v, t) = ak.Of(m.metal); foreach (var a in new[] { 90f, 270f }) { var d = Quaternion.Euler(0f, a, 0f) * hdir; Sweep(v, t, new[] { d * 15f, d * 13f + Vector3.up * 2.2f, d * 9f + Vector3.up * 3.2f, d * 7.2f + Vector3.up * 3.3f }, 0.32f, false, 8); } ak.Save("DockingArms", world); }
            var wg = new GameObject("WorldGround").AddComponent<Seven.WorldGround>();
            wg.Shape = Seven.Ground.Shape.Zones;
            wg.Zones = C.Zones.Select(z => new Seven.WorldGround.ZoneDef { Name = z.Name, Min = new Vector2(z.X0, z.Z0), Max = new Vector2(z.X1, z.Z1), Y0 = 0f, Y1 = 0f, Axis = 0, Enabled = z.On }).ToArray();
            wg.ZoneObstacles = new Vector3[0];
            var seven = Spawn(SevenDir + "/7_character.fbx", "7", C.SaucerAt + new Vector3(0f, 0f, 2f), Vector3.forward, sevenCtl);
            var camGo = new GameObject("ChapterCamera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.62f, 0.62f, 0.62f);
            cam.fieldOfView = 54f; cam.nearClipPlane = 0.08f; cam.farClipPlane = 2400f; camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData(); camData.renderPostProcessing = true; camData.requiresDepthTexture = true; camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camGo.transform.position = C.SaucerAt + new Vector3(-3f, 1.8f, -3f);
            var dirGo = new GameObject("CattalusDirector");
            var input = dirGo.AddComponent<Seven.ChapterInput>();
            var s7 = seven.AddComponent<Seven.ChapterSeven>(); s7.Input = input; s7.CameraTransform = camGo.transform;
            var ccam = camGo.AddComponent<Seven.ChapterCamera>(); ccam.Target = seven.transform; ccam.Input = input;
            var bikeRoot = new GameObject("HoverBike").transform; bikeRoot.position = C.BikePad;
            var visual = new GameObject("Visual").transform; visual.SetParent(bikeRoot, false);
            if (bike != null) { var bm = (GameObject)PrefabUtility.InstantiatePrefab(bike); bm.name = "BikeModel"; bm.transform.SetParent(visual, false); var bmat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Venus_hoverbike_v1.mat"); foreach (var r in bm.GetComponentsInChildren<Renderer>()) r.sharedMaterial = bmat; }
            var bpts = LoadBikePoints();
            Transform P(string n, Vector3 lp) { var x = new GameObject(n).transform; x.SetParent(visual, false); x.localPosition = lp; return x; }
            var hb = bikeRoot.gameObject.AddComponent<Seven.HoverBike>();
            hb.Seat = P("Seat", bpts.saddle); hb.GripL = P("GripL", bpts.gripL); hb.GripR = P("GripR", bpts.gripR); hb.FootL = P("FootL", bpts.footL); hb.FootR = P("FootR", bpts.footR);
            hb.Fairing = P("FairingL", new Vector3(bpts.gripL.x - 0.05f, bpts.gripL.y + 0.04f, bpts.gripL.z - 0.25f)); hb.Visual = visual; hb.Length = bpts.length;
            var vl = new GameObject("VentLight").AddComponent<Light>(); vl.transform.SetParent(visual, false); vl.transform.localPosition = new Vector3(0f, -0.15f, 0f); vl.type = LightType.Point; vl.range = 3f; vl.intensity = 0.4f; vl.shadows = LightShadows.None; hb.VentLight = vl;

            // ---------------------------------------------------------------- the cats
            var folk = new GameObject("Cats").transform; folk.SetParent(world, false);
            var catRoot = SpawnCat("Cat", C.SaucerAt + new Vector3(1f, 0f, 2.4f), Vector3.forward, catCtl, catFur, catWhisker, out var catAnim);
            var cat = catRoot.AddComponent<Seven.ChapterCat>(); cat.Seven = seven.transform; cat.Anim = catAnim; cat.Cam = cam;
            catRoot.AddComponent<Seven.CatPoseOverlay>();
            var fur = new Dictionary<string, Material>();
            Material F(string v2) { if (!fur.TryGetValue(v2, out var mm)) { mm = CatVariant(v2, catFur); fur[v2] = mm; } return mm; }
            var juniper = SpawnFolk("Juniper", C.JuniperMeet, Vector3.back, 1.08f, catCtl, F("tabby"), catWhisker, folk);
            var steward = SpawnFolk("Steward", C.StewardAt, Vector3.back, 1.12f, catCtl, F("tuxedo"), catWhisker, folk, "sit");
            Accessory(steward, "chest", m.fabric, "StewardBadge", (v, t) => { Ellipsoid(v, t, new Vector3(0f, 0.01f, 0.06f), new Vector3(0.035f, 0.035f, 0.01f), 4, 8); Sweep(v, t, new[] { new Vector3(-0.01f, -0.01f, 0.06f), new Vector3(-0.02f, -0.06f, 0.06f) }, 0.008f, false, 3); Sweep(v, t, new[] { new Vector3(0.01f, -0.01f, 0.06f), new Vector3(0.02f, -0.06f, 0.06f) }, 0.008f, false, 3); });
            var announcer = SpawnFolk("Announcer", desk.position + Vector3.up * 0.0f, Vector3.forward, 1.25f, catCtl, F("spotted"), catWhisker, studio, "sit");
            Accessory(announcer, "neck", m.cardDark, "BowTie", (v, t) => { Ellipsoid(v, t, new Vector3(-0.03f, 0f, 0.04f), new Vector3(0.03f, 0.018f, 0.01f), 3, 6); Ellipsoid(v, t, new Vector3(0.03f, 0f, 0.04f), new Vector3(0.03f, 0.018f, 0.01f), 3, 6); Ellipsoid(v, t, new Vector3(0f, 0f, 0.045f), Vector3.one * 0.012f, 3, 6); });
            var attendant = SpawnFolk("GateAttendant", C.AttendantAt, Vector3.right, 1.0f, catCtl, F("gray"), catWhisker, folk);
            var gateCat = SpawnFolk("GateCat", C.GateCatAt, Vector3.forward, 0.95f, catCtl, F("cream"), catWhisker, folk);
            // Duchess: her own model + rig; the stand-in = a long-haired white variant of Panther (larger)
            Seven.CatFolk duchess;
            if (duchessModel != null)
            {
                var droot = new GameObject("Duchess"); droot.transform.SetParent(folk, false); droot.transform.SetPositionAndRotation(C.DuchessWait, Quaternion.LookRotation(Vector3.forward));
                var dm = (GameObject)PrefabUtility.InstantiatePrefab(duchessModel); dm.name = "DuchessModel"; dm.transform.SetParent(droot.transform, false);
                var head = dm.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "head"); var pelvis = dm.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "pelvis");
                if (head != null && pelvis != null) { var fw = droot.transform.InverseTransformDirection(head.position - pelvis.position); fw.y = 0f; dm.transform.localRotation = Quaternion.Euler(0f, Vector3.SignedAngle(fw, Vector3.forward, Vector3.up), 0f) * dm.transform.localRotation; }
                var an = dm.GetComponent<Animator>() ?? dm.AddComponent<Animator>(); an.runtimeAnimatorController = dCtl; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (var smr in dm.GetComponentsInChildren<SkinnedMeshRenderer>()) { smr.updateWhenOffscreen = true; smr.sharedMaterials = smr.sharedMaterials.Select(mm => mm != null && mm.name.Contains("whisker") ? catWhisker : dMat).ToArray(); }
                droot.transform.localScale = Vector3.one * 1.25f;
                duchess = droot.AddComponent<Seven.CatFolk>(); duchess.Anim = an; duchess.Role = "Duchess"; duchess.StrideSpeed = 0.19f;
                duchess.ClipMap = "sit=idle,wash=idle,lie_down=idle,sleep=idle,stretch=idle";      // her auto-rig's sit / wash / lie-down distort her face (for Dan): she stands instead
                droot.AddComponent<Seven.CatPoseOverlay>();
            }
            else duchess = SpawnFolk("Duchess", C.DuchessWait, Vector3.forward, 1.35f, catCtl, F("white"), catWhisker, folk, "sit");
            // the vignette cats (mansion box, trickle, courier + sitter, three on the bench, the ledge walker, the meeting chair, the counter)
            var vs = new List<Seven.CatFolk>();
            string[] vv = { "gray", "tabby", "tuxedo", "cream", "spotted", "white", "gray", "tabby", "spotted", "cream" };
            for (int i = 0; i < 10; i++)
            {
                var at = i == 0 ? C.Vignettes[0].at + new Vector3(0.4f, 0.12f, 0f) : i == 1 ? C.FountainAt + new Vector3(3.9f, 0f, -0.8f) : i <= 3 ? C.Vignettes[2].at : i <= 6 ? C.Vignettes[3].at + Vector3.up * 0.5f : i == 7 ? C.Vignettes[4].at + new Vector3(0f, 0.9f, -8f) : i == 8 ? C.Vignettes[5].at + new Vector3(0.6f, 0f, -0.9f) : C.Vignettes[6].at + new Vector3(0.6f, 1.0f, 0f);
                vs.Add(SpawnFolk("Vignette" + i, at, Vector3.forward, 0.85f + (i % 4) * 0.1f, catCtl, F(vv[i]), catWhisker, folk));
            }
            // the meeting's other members (sit round the table), citizens strolling the avenue, the spectators in the stands
            var citizens = new List<Seven.CatFolk>();
            for (int i = 0; i < 3; i++) { var p = C.Vignettes[5].at + Quaternion.Euler(0f, 100f + i * 85f, 0f) * Vector3.forward * 1.1f; var c2 = SpawnFolk("Meeting" + i, p, C.Vignettes[5].at - p, 0.9f + i * 0.08f, catCtl, F(vv[(i + 3) % vv.Length]), catWhisker, folk, "sit"); c2.Anim.Play("sit"); citizens.Add(c2); }
            for (int i = 0; i < 20; i++) citizens.Add(SpawnFolk("Citizen" + i, new Vector3((i % 2 == 0 ? -1f : 1f) * (6f + i), 0f, 36f + i * 9f), Vector3.forward, 0.9f + (i % 3) * 0.15f, catCtl, F(vv[i % vv.Length]), catWhisker, folk));
            var spectators = new List<Seven.CatFolk>();
            for (int i = 0; i < 12; i++) { var p = new Vector3(37.2f + (i % 3) * 1.2f, 0.55f + (i % 3) * 0.55f, 120f + (i / 3) * 7f + (i % 2) * 1.5f); spectators.Add(SpawnFolk("Spectator" + i, p, Vector3.right, 1f, catCtl, F(vv[(i + 2) % vv.Length]), catWhisker, folk, "sit")); }
            // the props the vignettes move: the delivered carton, the dangling ornament, the object on the counter
            var carton = new GameObject("VignetteCarton").transform; carton.SetParent(world, false); carton.position = C.Vignettes[2].at; { var k2 = new AK(); CardboardBox(k2, m, Vector3.zero, new Vector3(0.7f, 0.45f, 0.55f), 0f, 91, false, true); foreach (var g in k2.Save("Carton", carton, false)) g.transform.localPosition = Vector3.zero; }
            var ornament = new GameObject("VignetteOrnament").transform; ornament.SetParent(world, false); ornament.position = C.Vignettes[5].at + new Vector3(1.8f, 3.0f, 1.2f); { var ov = new List<Vector3>(); var ot = new List<int>(); Sweep(ov, ot, new[] { Vector3.zero, Vector3.down * 1.2f }, 0.012f, false, 3); Ellipsoid(ov, ot, Vector3.down * 1.35f, Vector3.one * 0.14f, 5, 8); MeshObject("Ornament", SaveMesh(SmoothFinish(ov, ot), "Cat_Ornament"), m.glowSoft, ornament, false); }
            var cup = new GameObject("VignetteCup").transform; cup.SetParent(world, false); cup.position = C.Vignettes[6].at + new Vector3(0f, 1.0f, 0.3f); { var cv = new List<Vector3>(); var ct = new List<int>(); cv.AddRange(Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.05f, 0f), new Vector2(0.06f, 0.11f), new Vector2(0.055f, 0.11f), new Vector2(0f, 0.02f) }, 0f, 360f, 12).vertices); ct.AddRange(Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.05f, 0f), new Vector2(0.06f, 0.11f), new Vector2(0.055f, 0.11f), new Vector2(0f, 0.02f) }, 0f, 360f, 12).triangles); MeshObject("Cup", SaveMesh(Finish(cv, ct), "Cat_Cup"), m.cardLight, cup, false); }

            // ---------------------------------------------------------------- Duchess's racer (+ the ghost racer for the screen's highlights)
            Transform MakeRacer(string n, Transform parent)
            {
                var r = new GameObject(n).transform; r.SetParent(parent, false);
                if (racerModel != null) { var rm = (GameObject)PrefabUtility.InstantiatePrefab(racerModel); rm.name = "RacerModel"; rm.transform.SetParent(r, false); }
                else
                {
                    // the procedural stand-in after cat_racer_ref.png: a teardrop hull, a round padded bed, a windscreen, two rear fan pods, number 8
                    var rk2 = new AK(); var (v, t) = rk2.Of(m.metal);
                    Ellipsoid(v, t, new Vector3(0f, 0.18f, 0.05f), new Vector3(0.42f, 0.2f, 0.8f), 8, 14);
                    foreach (var sg in new[] { -1f, 1f }) Append(v, t, Lathe(new[] { new Vector2(0f, -0.12f), new Vector2(0.16f, -0.12f), new Vector2(0.17f, 0.12f), new Vector2(0f, 0.12f) }, 0f, 360f, 14, null, new Vector3(sg * 0.38f, 0.22f, -0.62f)));
                    var (pv, pt) = rk2.Of(m.fabric); Ellipsoid(pv, pt, new Vector3(0f, 0.36f, -0.05f), new Vector3(0.3f, 0.07f, 0.34f), 5, 10);
                    var (gv, gt) = rk2.Of(m.glowSoft); Ellipsoid(gv, gt, new Vector3(0f, 0.42f, 0.5f), new Vector3(0.28f, 0.14f, 0.05f), 4, 10, d => d.z < 0f);
                    var (nv, nt) = rk2.Of(m.cardDark); foreach (var sg in new[] { -1f, 1f }) { var c0 = new Vector3(sg * 0.43f, 0.2f, 0.1f); Ellipsoid(nv, nt, c0 + Vector3.up * 0.04f, new Vector3(0.005f, 0.035f, 0.035f), 3, 8); Ellipsoid(nv, nt, c0 - Vector3.up * 0.04f, new Vector3(0.005f, 0.04f, 0.04f), 3, 8); }   // "8"
                    foreach (var g in rk2.Save(n + "Hull", r, true)) g.transform.localPosition = Vector3.zero;
                }
                var lt = new GameObject("RacerGlow").AddComponent<Light>(); lt.transform.SetParent(r, false); lt.transform.localPosition = Vector3.down * 0.1f; lt.type = LightType.Point; lt.range = 2.5f; lt.intensity = 0.5f; lt.shadows = LightShadows.None;
                return r;
            }
            var racer = MakeRacer("DuchessRacer", world); racer.position = K.StartAt + new Vector3(3.5f, 0.65f, 8f);
            if (duchessModel != null || duchess != null) { var dSeat = new GameObject("DuchessSeat").transform; dSeat.SetParent(racer, false); dSeat.localPosition = new Vector3(0f, 0.36f, -0.05f); }
            var ghost = MakeRacer("GhostRacer", world); ghost.position = K.AtDist(60f) + Vector3.up * 0.9f;   // on the course (it flies the loop for the screen)

            // ---------------------------------------------------------------- the screen, the race, the guide light, UI, dialogue, the director
            var screen = face.AddComponent<Seven.CatatronScreen>(); screen.ScreenCam = scam; screen.Face = face.GetComponent<Renderer>(); screen.Trophy = trophy; screen.MarkPoint = markGo.transform; screen.Retreat = retreat; screen.AnnouncerDesk = desk; screen.GhostRacer = ghost;
            var raceGo = new GameObject("GrandCircuitRace"); raceGo.transform.SetParent(world, false);
            var race = raceGo.AddComponent<Seven.CattalusRace>();
            race.Bike = hb; race.Seven = s7; race.Cam = ccam; race.DuchessRacer = racer; race.Duchess = duchess; race.Gates = gates.ToArray(); race.GateIdx = gidx.ToArray();
            race.Toys = toyT.ToArray(); race.ToyIdx = toys.Select(x => x.i).ToArray(); race.ToyPeriod = toys.Select(x => x.period).ToArray(); race.ToyPhase = toys.Select(x => x.phase).ToArray();
            race.StartLights = lights.ToArray(); race.Screen = screen; race.Spectators = spectators.Select(s => s.transform).ToArray();
            var gl0 = new List<Vector3>(); var glt = new List<int>(); Ellipsoid(gl0, glt, Vector3.zero, Vector3.one * 0.5f, 10, 14);
            var guideLight = MeshObject("GuideLight", SaveMesh(SmoothFinish(gl0, glt), "Cat_GuideLight"), m.glow, world, false);
            PointLight("GuideLightLamp", Vector3.zero, 24f, 3f, guideLight.transform).transform.localPosition = Vector3.zero;
            guideLight.SetActive(false);
            var ui = new GameObject("ChapterUI").AddComponent<Seven.ChapterUI>(); ui.Cam = cam;
            var dlg = dirGo.AddComponent<Seven.DialoguePlayer>(); dlg.Bank = bank; dlg.UI = ui;
            var legend = new GameObject("ControlsLegend").AddComponent<Seven.ControlsLegend>(); legend.Cam = cam; legend.Set = Seven.ControlsLegend.Layout.Chapter; legend.HidePanels = true;
            var focus = new GameObject("CattalusFocus"); var fv = focus.AddComponent<Volume>(); fv.isGlobal = true; fv.priority = 100f; fv.weight = 0f; fv.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Atmosphere/Galley_Focus.asset"); var cuf = focus.AddComponent<Seven.CloseUpFocus>();
            race.UI = ui;
            var dir = dirGo.AddComponent<Seven.CattalusDirector>();
            dir.Seven = s7; dir.Cam = ccam; dir.Input = input; dir.Dlg = dlg; dir.UI = ui; dir.Legend = legend; dir.Bike = hb; dir.SaucerBeam = beam; dir.Saucer = saucer; dir.Hold = hold;
            dir.Atmo = atmo.GetComponent<Seven.Atmosphere>(); dir.GuideLight = guideLight.transform; dir.Cat = cat; dir.Focus = cuf;
            dir.Juniper = juniper; dir.Steward = steward; dir.Duchess = duchess; dir.Attendant = attendant; dir.GateCat = gateCat; dir.Announcer = announcer;
            dir.Citizens = citizens.ToArray(); dir.Spectators = spectators.ToArray(); dir.VignetteCats = vs.ToArray();
            var gateLeaf = new GameObject("GateLeaf").transform; gateLeaf.SetParent(world, false); gateLeaf.position = C.GateAt + new Vector3(-3.9f, 0f, 0f);
            { var lv = new List<Vector3>(); var lt = new List<int>(); for (int i = 0; i < 9; i++) Sweep(lv, lt, new[] { new Vector3(0.4f + i * 0.9f, 0.15f, 0f), new Vector3(0.4f + i * 0.9f, 2.1f, 0f) }, 0.05f, false, 5); Sweep(lv, lt, new[] { new Vector3(0.2f, 2.0f, 0f), new Vector3(7.8f, 2.0f, 0f) }, 0.06f, false, 5); Sweep(lv, lt, new[] { new Vector3(0.2f, 0.4f, 0f), new Vector3(7.8f, 0.4f, 0f) }, 0.06f, false, 5); MeshObject("GateBars", SaveMesh(SmoothFinish(lv, lt), "Cat_GateBars"), m.metal, gateLeaf, true); }
            dir.GateLeaf = gateLeaf; dir.TransitBall = transitBall; dir.Screen = screen; dir.Trophy = trophy; dir.Sphere = sphere; dir.SphereLines = sl.GetComponent<Renderer>();
            dir.Retreat = retreat; dir.RetreatPaper = paper; dir.DuchessRacer = racer; dir.Race = race; dir.VignetteProps = new[] { carton, ornament, cup };
            var smallBox = new GameObject("SmallBox").transform; smallBox.SetParent(world, false); smallBox.position = C.SmallBoxAt; { var k3 = new AK(); CardboardBox(k3, m, Vector3.zero, new Vector3(0.36f, 0.24f, 0.3f), 20f, 93, false, true); foreach (var g in k3.Save("SmallBox", smallBox, false)) g.transform.localPosition = Vector3.zero; }
            dir.SmallBox = smallBox;
            dir.Vision7 = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Video/vision_07_cattalus.mp4"); dir.MeadowClips = meadowClips; dir.VisionInsert = insert;
            AddProbe(new Vector3(0f, 10f, 60f), new Vector3(80f, 30f, 140f), Vector3.zero);
            AddProbe(new Vector3(0f, 10f, 145f), new Vector3(80f, 30f, 70f), Vector3.zero);
            AddProbe(new Vector3(-200f, 10f, 160f), new Vector3(220f, 40f, 240f), Vector3.zero);
            QF18SceneChecks("Cattalus", true);
            EditorSceneManager.SaveScene(scene, CattalusScenePath);
            Log("scene saved " + CattalusScenePath + $" (Duchess {(duchessModel != null ? "Dan's model, rigged" : "stand-in")}, racer {(racerModel != null ? "Dan's model" : "stand-in")}, trophy {(trophyModel != null ? "Dan's model" : "stand-in")}, vision 7 {(dir.Vision7 != null ? "in" : "MISSING")}, meadow clip {(meadowClips != null && meadowClips.Length > 0 && meadowClips[0] != null ? "in" : "MISSING")}, course {K.Length:0} m, {gidx.Count} gates, {toys.Count} toys)");
        }
        [System.Serializable] class TrophyRing { public float[] centre; public float inner_radius, height; }
    }
}
