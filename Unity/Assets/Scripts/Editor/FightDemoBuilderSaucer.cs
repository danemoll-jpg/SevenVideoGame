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

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 3: the demo picker scene (Menu) and the saucer mock-up (Saucer): 7's ship interior, her chair by the
    /// window, the black cat, the city 200 m below in Kyle's fog, and the reveal. Everything is generated here.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const string MenuScenePath = "Assets/Scenes/Menu.unity";
        const string SaucerScenePath = "Assets/Scenes/Saucer.unity";
        const string CatDir = CharRoot + "/Cat";
        const string CatFbx = CatDir + "/cat_v1_rigged.fbx";
        const string AtmoAbovePath = AtmoDir + "/Atmosphere_Above.prefab";
        const float CityY = -200f;

        // Layout (deck floor at y = 0; +Z = the window; angles are atan2(z, x) in degrees).
        static readonly Vector3 ChairPos = new Vector3(0f, 0f, 4.4f);
        const float SeatHipsHeight = 0.646f;         // Sitting Idle hips above the floor (measured from the clip)
        const float WallR = 8.05f, DomeEdgeR = 7.35f, DomeEdgeY = 2.95f, DomeTopY = 3.8f;
        const float WinA0 = 52f, WinA1 = 128f, WinY0 = 0.15f, WinY1 = 2.3f;
        static readonly float[] NookAngles = { 205f, 335f };
        const float NookHalf = 17f, NookR = 9.7f, NookH = 2.35f;
        static readonly (float a0, float a1)[] SofaArcs = { (140f, 182f), (229f, 311f), (-8f, 36f) };

        static void BuildRound3(AnimatorController sevenCtl, GameObject atmoPrefab)
        {
            CopyGallery();                      // Round 5: Art/Gallery -> StreamingAssets/Gallery
            var catCtl = SetupCat(out var catMats);
            var sm = BuildSaucerMaterials();
            BuildMenuScene();
            var above = BuildAtmosphereAbove(atmoPrefab);
            BuildSaucerScene(sevenCtl, catCtl, catMats, sm, above);
        }

        // ------------------------------------------------------------------------------------- menu
        static void BuildMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.3f, 0.3f, 0.3f);
            var camGo = new GameObject("MenuCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.05f);
            camGo.AddComponent<AudioListener>();
            var menu = new GameObject("DemoMenu").AddComponent<Seven.DemoMenu>();
            menu.Cam = cam;
            EditorSceneManager.SaveScene(scene, MenuScenePath);
            Log("scene saved " + MenuScenePath);
        }

        // -------------------------------------------------------------------------------------- cat
        struct CatMats { public Material fur, whisker; }

        static AnimatorController SetupCat(out CatMats mats)
        {
            var src = Path.Combine(RepoRoot, "Art", "Export", "cat", "cat_v1_rigged.fbx");
            if (!File.Exists(src)) throw new Exception("Cat export missing: run Tools/Blender/cleanup_cat.py first (" + src + ")");
            Directory.CreateDirectory(Path.Combine(ProjectRoot, CatDir, "Textures"));
            File.Copy(src, Path.Combine(ProjectRoot, CatFbx), true);
            var texSrc = Path.Combine(RepoRoot, "Art", "Export", "cat", "Textures", "cat_v1_basecolor.jpg");
            File.Copy(texSrc, Path.Combine(ProjectRoot, CatDir, "Textures", "cat_v1_basecolor.jpg"), true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var texPath = CatDir + "/Textures/cat_v1_basecolor.jpg";
            var ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
            ti.maxTextureSize = 2048; ti.sRGBTexture = true; ti.SaveAndReimport();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            mats.fur = Dissolve("Cat_fur", new Color(1f, 1f, 1f), 0.55f, false);
            mats.fur.SetTexture("_BaseMap", tex);
            mats.fur.SetFloat("_Rim", 0.35f); mats.fur.SetFloat("_RimPower", 3.2f);
            mats.whisker = Dissolve("Cat_whisker", new Color(0.78f, 0.78f, 0.78f), 0.3f, false);
            mats.whisker.SetFloat("_Cull", 0f);

            var imp = (ModelImporter)AssetImporter.GetAtPath(CatFbx);
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = true;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            imp.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
            imp.SaveAndReimport();
            var clips = imp.defaultClipAnimations;
            var loops = new[] { "idle", "walk", "sit", "wash", "sleep" };
            foreach (var c in clips)
            {
                var n = c.takeName.Contains("|") ? c.takeName.Substring(c.takeName.LastIndexOf('|') + 1) : c.takeName;
                c.name = n;
                c.loopTime = loops.Contains(n);
                c.loopPose = false;
            }
            imp.clipAnimations = clips;
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "cat_fur"), mats.fur);
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "cat_whisker"), mats.whisker);
            imp.SaveAndReimport();

            var path = AnimDir + "/Cat.controller";
            AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            var smach = ac.layers[0].stateMachine;
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(CatFbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")))
            {
                var st = smach.AddState(clip.name);
                st.motion = clip;
                if (clip.name == "idle") smach.defaultState = st;
                Log($"cat clip {clip.name} {clip.length:0.00}s loop={clip.isLooping}");
            }
            EditorUtility.SetDirty(ac);
            return ac;
        }

        // ---------------------------------------------------------------------------- saucer mats
        static Material Dissolve(string name, Color c, float smooth, bool dissolvable = true, float metallic = 0f)
        {
            Directory.CreateDirectory(Path.Combine(ProjectRoot, MatDir));
            var path = MatDir + "/" + name + ".mat";
            var sh = Shader.Find("Seven/DissolveLit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metallic);
            m.SetColor("_EmissionColor", Color.black);
            m.SetFloat("_Rim", 0f);
            m.SetFloat("_Dissolvable", dissolvable ? 1f : 0f);
            m.SetFloat("_Cull", 0f);          // double-sided: the shader flips back-face normals
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        struct ShipMats
        {
            public Material floor, carpet, wall, dome, rib, seam, cove, sofaBase, fabric, table, pot, leaf, stem, frame, glass,
                chairShell, chairLining, pedestal, city, cityGlow, cityGround, bench;
        }

        static ShipMats BuildSaucerMaterials()
        {
            var m = new ShipMats
            {
                floor = Dissolve("Ship_floor", Gray(0.30f), 0.86f),
                carpet = Dissolve("Ship_carpet", Gray(0.13f), 0.04f),
                wall = Dissolve("Ship_wallpanel", Gray(0.64f), 0.82f),
                dome = Dissolve("Ship_dome", Gray(0.42f), 0.35f),
                rib = Dissolve("Ship_rib", Gray(0.78f), 0.75f),
                seam = Dissolve("Ship_seam", Gray(0.2f), 0.6f),
                cove = Dissolve("Ship_cove", Gray(1f), 0.5f),
                sofaBase = Dissolve("Ship_sofabase", Gray(0.72f), 0.8f),
                fabric = Dissolve("Ship_fabric", Gray(0.24f), 0.08f),
                table = Dissolve("Ship_table", Gray(0.86f), 0.9f),
                pot = Dissolve("Ship_pot", Gray(0.7f), 0.85f),
                leaf = Dissolve("Ship_leaf", Gray(0.2f), 0.3f),
                stem = Dissolve("Ship_stem", Gray(0.16f), 0.2f),
                frame = Dissolve("Ship_frame", Gray(0.22f), 0.8f),
                bench = Dissolve("Ship_bench", Gray(0.3f), 0.1f),
                chairShell = Dissolve("Chair_shell", Gray(0.9f), 0.9f, false),
                chairLining = Dissolve("Chair_lining", Gray(0.2f), 0.08f, false),
                pedestal = Dissolve("Chair_pedestal", Gray(0.75f), 0.85f, true, 0.4f),
                city = Lit("City_tower", Gray(0.32f), 0.3f),
                cityGlow = Lit("City_glow", Color.white, 0.5f),
                cityGround = Lit("City_ground", Gray(0.08f), 0.1f),
            };
            m.cove.SetColor("_EmissionColor", Color.white * 2.6f);
            Emissive(m.cityGlow, 3.0f);
            foreach (var x in new[] { m.city, m.cityGlow }) { x.SetFloat("_Cull", 0f); x.doubleSidedGI = true; }
            m.carpet.SetTexture("_BaseMap", CarpetTexture());
            m.carpet.SetTextureScale("_BaseMap", new Vector2(24f, 1f));

            // Window glass: URP Lit, transparent, faint and glossy (faded out by the reveal).
            var gpath = MatDir + "/Ship_glass.mat";
            var g = AssetDatabase.LoadAssetAtPath<Material>(gpath);
            if (g == null) { g = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(g, gpath); }
            g.shader = Shader.Find("Universal Render Pipeline/Lit");
            g.SetFloat("_Surface", 1f); g.SetFloat("_Blend", 0f);
            g.SetOverrideTag("RenderType", "Transparent");
            g.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); g.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            g.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); g.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            g.SetFloat("_ZWrite", 0f); g.SetFloat("_Cull", 0f);
            g.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            g.renderQueue = (int)RenderQueue.Transparent;
            g.SetColor("_BaseColor", new Color(0.85f, 0.85f, 0.85f, 0.10f));
            g.SetFloat("_Smoothness", 0.96f); g.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(g);
            m.glass = g;
            AssetDatabase.SaveAssets();
            return m;
        }

        static Color Gray(float v) => new Color(v, v, v, 1f);

        /// <summary>Subtle concentric bands for the carpet (u runs around the deck, v outward).</summary>
        static Texture2D CarpetTexture()
        {
            var path = GenDir + "/Ship_carpet_tex.asset";
            Directory.CreateDirectory(Path.Combine(ProjectRoot, GenDir));
            AssetDatabase.DeleteAsset(path);
            const int w = 64, h = 256;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var rnd = new System.Random(3);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float v = (float)y / h;
                    float band = 0.82f + 0.18f * Mathf.SmoothStep(0f, 1f, Mathf.Abs(Mathf.Sin(v * Mathf.PI * 9f)));
                    float n = 0.92f + 0.08f * (float)rnd.NextDouble();
                    float g = band * n;
                    t.SetPixel(x, y, new Color(g, g, g, 1f));
                }
            t.Apply();
            AssetDatabase.CreateAsset(t, path);
            return t;
        }

        // ------------------------------------------------------------------------------- mesh kit
        static Vector3 Polar(float r, float aDeg, float y) => new Vector3(Mathf.Cos(aDeg * Mathf.Deg2Rad) * r, y, Mathf.Sin(aDeg * Mathf.Deg2Rad) * r);

        static List<Vector2> Resample(IList<Vector2> pts, float step)
        {
            var o = new List<Vector2> { pts[0] };
            for (int i = 0; i < pts.Count - 1; i++)
            {
                int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(pts[i], pts[i + 1]) / step));
                for (int k = 1; k <= n; k++) o.Add(Vector2.Lerp(pts[i], pts[i + 1], (float)k / n));
            }
            return o;
        }

        /// <summary>Surface of revolution: profile (r, y) swept from a0 to a1 degrees. skip(angle, y) omits quads.</summary>
        static Mesh Lathe(IList<Vector2> prof, float a0, float a1, int seg, Func<float, float, bool> skip = null, Vector3 offset = default)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            int n = prof.Count;
            float len = 0f; var acc = new float[n];
            for (int i = 1; i < n; i++) { len += Vector2.Distance(prof[i], prof[i - 1]); acc[i] = len; }
            for (int s = 0; s <= seg; s++)
            {
                float a = Mathf.Lerp(a0, a1, (float)s / seg);
                for (int i = 0; i < n; i++)
                {
                    v.Add(offset + Polar(prof[i].x, a, prof[i].y));
                    uv.Add(new Vector2((float)s / seg, len > 0 ? acc[i] / len : 0f));
                }
            }
            for (int s = 0; s < seg; s++)
            {
                float am = Mathf.Lerp(a0, a1, (s + 0.5f) / seg);
                for (int i = 0; i < n - 1; i++)
                {
                    if (skip != null && skip(am, (prof[i].y + prof[i + 1].y) * 0.5f)) continue;
                    int a = s * n + i, b = a + 1, c = a + n, d = c + 1;
                    t.Add(a); t.Add(b); t.Add(c);
                    t.Add(b); t.Add(d); t.Add(c);
                }
            }
            var m = new Mesh { indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals();
            return m;
        }

        /// <summary>Round tube along a path.</summary>
        static void Sweep(List<Vector3> v, List<int> t, IList<Vector3> path, float radius, bool closed, int sides = 8)
        {
            int n = path.Count;
            int start = v.Count;
            for (int i = 0; i < n; i++)
            {
                var tan = (closed ? path[(i + 1) % n] - path[(i - 1 + n) % n]
                          : path[Mathf.Min(i + 1, n - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                var nrm = Vector3.Cross(tan, Vector3.up);
                if (nrm.sqrMagnitude < 1e-4f) nrm = Vector3.Cross(tan, Vector3.right);
                nrm.Normalize();
                var bin = Vector3.Cross(tan, nrm);
                for (int k = 0; k < sides; k++)
                {
                    float a = k * Mathf.PI * 2f / sides;
                    v.Add(path[i] + (nrm * Mathf.Cos(a) + bin * Mathf.Sin(a)) * radius);
                }
            }
            int rings = closed ? n : n - 1;
            for (int i = 0; i < rings; i++)
                for (int k = 0; k < sides; k++)
                {
                    int a = start + i * sides + k, b = start + i * sides + (k + 1) % sides;
                    int c = start + ((i + 1) % n) * sides + k, d = start + ((i + 1) % n) * sides + (k + 1) % sides;
                    t.Add(a); t.Add(c); t.Add(b);
                    t.Add(b); t.Add(c); t.Add(d);
                }
        }

        static Mesh SmoothFinish(List<Vector3> v, List<int> t)
        {
            var m = new Mesh { indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v); m.SetTriangles(t, 0);
            m.SetUVs(0, v.Select(p => new Vector2(p.x, p.z)).ToList());
            m.RecalculateNormals();
            return m;
        }

        /// <summary>Ellipsoid (lat-long). skip(unit direction) omits faces (the egg chair's front opening).</summary>
        static void Ellipsoid(List<Vector3> v, List<int> t, Vector3 c, Vector3 radii, int lat, int lon, Func<Vector3, bool> skip = null)
        {
            int start = v.Count;
            for (int i = 0; i <= lat; i++)
            {
                float th = Mathf.PI * i / lat;
                for (int j = 0; j <= lon; j++)
                {
                    float ph = Mathf.PI * 2f * j / lon;
                    var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), -Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    v.Add(c + Vector3.Scale(d, radii));
                }
            }
            for (int i = 0; i < lat; i++)
                for (int j = 0; j < lon; j++)
                {
                    float th = Mathf.PI * (i + 0.5f) / lat, ph = Mathf.PI * 2f * (j + 0.5f) / lon;
                    var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), -Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    if (skip != null && skip(d)) continue;
                    int a = start + i * (lon + 1) + j, b = a + 1, cc = a + lon + 1, dd = cc + 1;
                    t.Add(a); t.Add(cc); t.Add(b);
                    t.Add(b); t.Add(cc); t.Add(dd);
                }
        }

        static GameObject Obj(string name, Mesh mesh, Material mat, Transform parent, bool shadows = true)
        {
            var go = MeshObject(name, SaveMesh(mesh, "Saucer_" + name), mat, parent, shadows);
            return go;
        }

        // --------------------------------------------------------------------------------- the ship
        static float DomeY(float r) { float k = Mathf.Clamp01(r / DomeEdgeR); return DomeEdgeY + (DomeTopY - DomeEdgeY) * (1f - k * k); }
        static bool InWindow(float a) => a >= WinA0 && a <= WinA1;
        static bool InNook(float a) => NookAngles.Any(n => Mathf.Abs(Mathf.DeltaAngle(a, n)) <= NookHalf);
        static readonly Vector2[] WallProfile =
        {
            new Vector2(WallR, -0.012f), new Vector2(8.3f, 0.5f), new Vector2(8.4f, 1.2f), new Vector2(8.25f, 1.9f),
            new Vector2(7.85f, 2.45f), new Vector2(7.5f, 2.72f),
        };
        static float WallRAt(float y)
        {
            for (int i = 0; i < WallProfile.Length - 1; i++)
                if (y <= WallProfile[i + 1].y) return Mathf.Lerp(WallProfile[i].x, WallProfile[i + 1].x, Mathf.InverseLerp(WallProfile[i].y, WallProfile[i + 1].y, y));
            return WallProfile[WallProfile.Length - 1].x;
        }

        struct ShipRefs { public Transform chairPivot, seat; public List<Light> lights; public List<Renderer> glass; public List<Vector3> obstacles; public TvRefs tv; }

        static ShipRefs BuildShip(ShipMats m)
        {
            var refs = new ShipRefs { lights = new List<Light>(), glass = new List<Renderer>(), obstacles = new List<Vector3>() };
            var ship = new GameObject("Ship").transform;
            var deck = new GameObject("Deck").transform; deck.SetParent(ship, false);

            // Floor: carpet in the middle, polished walkway ring, glass floor in front of the window.
            Obj("Carpet", Lathe(new[] { new Vector2(0f, 0f), new Vector2(6.25f, 0f), new Vector2(6.3f, -0.012f) }, 0f, 360f, 128), m.carpet, deck);
            Obj("Walkway", Lathe(new[] { new Vector2(6.3f, -0.012f), new Vector2(WallR + 0.02f, -0.012f) }, WinA1, WinA0 + 360f, 160), m.floor, deck);
            var glassFloor = Obj("GlassFloor", Lathe(new[] { new Vector2(6.3f, -0.012f), new Vector2(WallR + 0.02f, -0.012f) }, WinA0, WinA1, 48), m.glass, deck, false);
            refs.glass.Add(glassFloor.GetComponent<Renderer>());
            // Under-floor rim so the glass floor has an edge.
            var rv = new List<Vector3>(); var rt = new List<int>();
            Sweep(rv, rt, Enumerable.Range(0, 49).Select(i => Polar(6.3f, Mathf.Lerp(WinA0, WinA1, i / 48f), -0.01f)).ToList(), 0.03f, false);
            foreach (var a in new[] { WinA0, WinA1 }) Sweep(rv, rt, new[] { Polar(6.3f, a, -0.01f), Polar(WallR, a, -0.01f) }, 0.03f, false);
            Obj("GlassFloorRim", SmoothFinish(rv, rt), m.frame, deck);
            // Soft floor cove light along the base of the wall.
            var fv = new List<Vector3>(); var ft = new List<int>();
            Sweep(fv, ft, Enumerable.Range(0, 180).Select(i => Polar(WallR - 0.05f, i * 2f, 0.03f)).ToList(), 0.018f, true, 6);
            Obj("FloorCove", SmoothFinish(fv, ft), m.cove, deck, false);

            // Walls: curved polished panels with a window band and two nook openings.
            var walls = new GameObject("Walls").transform; walls.SetParent(ship, false);
            var prof = Resample(WallProfile, 0.12f);
            Obj("WallPanels", Lathe(prof, 0f, 360f, 240, (a, y) => (InWindow(a) && y > WinY0 && y < WinY1) || (InNook(a) && y < NookH)), m.wall, walls);
            var glassWall = Obj("WindowGlass", Lathe(prof, WinA0, WinA1, 64, (a, y) => !(y > WinY0 && y < WinY1)), m.glass, walls, false);
            refs.glass.Add(glassWall.GetComponent<Renderer>());
            // Window frame: top + bottom arcs and mullions following the wall curve.
            var wv = new List<Vector3>(); var wt = new List<int>();
            foreach (var y in new[] { WinY0, WinY1 })
                Sweep(wv, wt, Enumerable.Range(0, 65).Select(i => Polar(WallRAt(y) - 0.02f, Mathf.Lerp(WinA0, WinA1, i / 64f), y)).ToList(), 0.04f, false);
            for (float a = WinA0; a <= WinA1 + 0.1f; a += (WinA1 - WinA0) / 4f)
                Sweep(wv, wt, Enumerable.Range(0, 13).Select(i => { float y = Mathf.Lerp(WinY0, WinY1, i / 12f); return Polar(WallRAt(y) - 0.02f, a, y); }).ToList(), 0.035f, false);
            Obj("WindowFrame", SmoothFinish(wv, wt), m.frame, walls);
            // Panel seams every 15 degrees (not across the window or the nooks).
            var sv = new List<Vector3>(); var st = new List<int>();
            for (float a = 7.5f; a < 360f; a += 15f)
            {
                if (InWindow(a) || InNook(a)) continue;
                Sweep(sv, st, prof.Select(p => Polar(p.x - 0.01f, a, p.y)).ToList(), 0.012f, false, 5);
            }
            Obj("PanelSeams", SmoothFinish(sv, st), m.seam, walls, false);

            // Nooks: a pocket in the wall with a curved bench, a plant and a soft light.
            for (int k = 0; k < NookAngles.Length; k++)
            {
                float c = NookAngles[k], a0 = c - NookHalf, a1 = c + NookHalf;
                var nook = new GameObject("Nook" + k).transform; nook.SetParent(walls, false);
                Obj($"Nook{k}_Floor", Lathe(new[] { new Vector2(7.9f, 0f), new Vector2(NookR, 0f) }, a0, a1, 24), m.carpet, nook);
                Obj($"Nook{k}_Back", Lathe(Resample(new[] { new Vector2(NookR, -0.01f), new Vector2(NookR + 0.08f, 1.2f), new Vector2(NookR - 0.1f, NookH + 0.01f) }, 0.15f), a0, a1, 24), m.wall, nook);
                Obj($"Nook{k}_Ceiling", Lathe(new[] { new Vector2(7.7f, NookH), new Vector2(NookR, NookH) }, a0, a1, 24), m.dome, nook);
                var side = new List<Vector3>(); var sideT = new List<int>();
                foreach (var a in new[] { a0, a1 })
                {
                    var p0 = Polar(7.85f, a, 0f); var p1 = Polar(NookR, a, 0f);
                    Quad(side, sideT, p0, p0 + Vector3.up * NookH, p1 + Vector3.up * NookH, p1);
                }
                Obj($"Nook{k}_Sides", Finish(side, sideT), m.wall, nook);
                if (k != TvNook)     // Round 5: the TV nook has a console + screen instead of the bench
                {
                    var bv = new List<Vector3>(); var btri = new List<int>();
                    AddRing(bv, btri, Vector3.zero, 9.0f, 9.65f, 0.4f, a0 + 3f, a1 - 3f, 20);
                    Obj($"Nook{k}_Bench", Finish(bv, btri), m.sofaBase, nook);
                    var cv = new List<Vector3>(); var ct = new List<int>();
                    AddRing(cv, ct, Vector3.up * 0.4f, 9.05f, 9.6f, 0.12f, a0 + 4f, a1 - 4f, 20);
                    AddRing(cv, ct, Vector3.up * 0.4f, 9.45f, 9.65f, 0.55f, a0 + 4f, a1 - 4f, 20);
                    Obj($"Nook{k}_Cushions", Finish(cv, ct), m.fabric, nook);
                }
                var lamp = new GameObject($"Nook{k}_Light").AddComponent<Light>();
                lamp.transform.SetParent(nook, false);
                lamp.transform.position = Polar(8.9f, c, 2.0f);
                lamp.type = LightType.Point; lamp.range = 3.6f; lamp.intensity = k == TvNook ? 0.9f : 1.6f; lamp.color = Color.white; lamp.shadows = LightShadows.None;
                refs.lights.Add(lamp);
                var lv = new List<Vector3>(); var lt = new List<int>();
                Sweep(lv, lt, Enumerable.Range(0, 21).Select(i => Polar(NookR - 0.25f, Mathf.Lerp(a0 + 2f, a1 - 2f, i / 20f), NookH - 0.06f)).ToList(), 0.02f, false, 6);
                Obj($"Nook{k}_Glow", SmoothFinish(lv, lt), m.cove, nook, false);
                Plant(nook, $"Nook{k}_Plant", Polar(8.6f, k == 0 ? a0 + 5f : a1 - 3.5f, 0f), 1.0f, 300 + k, m, refs.obstacles);
            }
            refs.tv = BuildGalleryNook(walls, m, refs.obstacles);

            // Dome ceiling, cove light, concentric ribs, central light.
            var ceil = new GameObject("Ceiling").transform; ceil.SetParent(ship, false);
            var domeProf = new List<Vector2>();
            for (int i = 0; i <= 40; i++) { float r = DomeEdgeR * (1f - i / 40f); domeProf.Add(new Vector2(r, DomeY(r))); }
            Obj("Dome", Lathe(domeProf, 0f, 360f, 160), m.dome, ceil);
            Obj("CoveLedge", Lathe(new[] { new Vector2(7.5f, 2.72f), new Vector2(7.2f, 2.72f), new Vector2(7.2f, 2.66f) }, 0f, 360f, 160), m.wall, ceil);
            var gv = new List<Vector3>(); var gt = new List<int>();
            Sweep(gv, gt, Enumerable.Range(0, 180).Select(i => Polar(7.42f, i * 2f, 2.82f)).ToList(), 0.035f, true, 6);
            Obj("CoveLight", SmoothFinish(gv, gt), m.cove, ceil, false);
            var ribV = new List<Vector3>(); var ribT = new List<int>();
            foreach (var r in new[] { 1.5f, 3.0f, 4.5f, 6.0f })
                Sweep(ribV, ribT, Enumerable.Range(0, 120).Select(i => Polar(r, i * 3f, DomeY(r) - 0.05f)).ToList(), 0.05f, true, 8);
            Obj("DomeRibs", SmoothFinish(ribV, ribT), m.rib, ceil);
            Obj("CeilingLight", Lathe(new[] { new Vector2(0f, DomeTopY - 0.06f), new Vector2(0.8f, DomeY(0.8f) - 0.06f) }, 0f, 360f, 48), m.cove, ceil, false);
            for (int i = 0; i < 8; i++)
            {
                var l = new GameObject("CoveLight" + i).AddComponent<Light>();
                l.transform.SetParent(ceil, false);
                l.transform.position = Polar(6.4f, i * 45f + 22.5f, 2.55f);
                l.type = LightType.Point; l.range = 6.5f; l.intensity = 1.25f; l.shadows = LightShadows.None;
                refs.lights.Add(l);
            }
            var cl = new GameObject("CentreLight").AddComponent<Light>();
            cl.transform.SetParent(ceil, false);
            cl.transform.position = new Vector3(0f, DomeTopY - 0.4f, 0f);
            cl.type = LightType.Point; cl.range = 9f; cl.intensity = 1.5f; cl.shadows = LightShadows.None;
            refs.lights.Add(cl);

            // Furniture: curved built-in sofas along the wall, a round table with poufs, plants.
            var furn = new GameObject("Furniture").transform; furn.SetParent(ship, false);
            int si = 0;
            foreach (var (a0, a1) in SofaArcs)
            {
                var bv = new List<Vector3>(); var bt = new List<int>();
                AddRing(bv, bt, Vector3.zero, 6.55f, 7.55f, 0.36f, a0, a1, 40);
                Obj($"Sofa{si}_Base", Finish(bv, bt), m.sofaBase, furn);
                var cv = new List<Vector3>(); var ct = new List<int>();
                AddRing(cv, ct, Vector3.up * 0.36f, 6.6f, 7.45f, 0.14f, a0 + 0.5f, a1 - 0.5f, 40);
                AddRing(cv, ct, Vector3.up * 0.36f, 7.25f, 7.6f, 0.6f, a0 + 0.5f, a1 - 0.5f, 40);
                Obj($"Sofa{si}_Cushions", Finish(cv, ct), m.fabric, furn);
                // Rounded arms at both ends.
                var av = new List<Vector3>(); var at = new List<int>();
                foreach (var a in new[] { a0, a1 }) Ellipsoid(av, at, Polar(7.05f, a, 0.42f), new Vector3(0.55f, 0.22f, 0.55f), 10, 16);
                Obj($"Sofa{si}_Arms", SmoothFinish(av, at), m.sofaBase, furn);
                float arc = (a1 - a0) * Mathf.Deg2Rad * 7.0f;
                int nObs = Mathf.CeilToInt(arc / 0.55f);
                for (int i = 0; i <= nObs; i++) { var p = Polar(7.05f, Mathf.Lerp(a0, a1, (float)i / nObs), 0f); refs.obstacles.Add(new Vector3(p.x, p.z, 0.48f)); }
                si++;
            }
            var tableC = new Vector3(0f, 0f, -2.4f);
            Obj("Table", Lathe(Resample(new[] { new Vector2(0f, 0f), new Vector2(0.38f, 0f), new Vector2(0.32f, 0.04f), new Vector2(0.07f, 0.1f),
                new Vector2(0.06f, 0.36f), new Vector2(0.6f, 0.38f), new Vector2(0.66f, 0.41f), new Vector2(0.62f, 0.44f), new Vector2(0f, 0.44f) }, 0.05f), 0f, 360f, 48, null, tableC), m.table, furn);
            refs.obstacles.Add(new Vector3(tableC.x, tableC.z, 0.68f));
            var pv = new List<Vector3>(); var pt = new List<int>();
            foreach (var a in new[] { 20f, 150f, 260f })
            {
                var p = tableC + Polar(1.35f, a, 0f);
                Ellipsoid(pv, pt, p + Vector3.up * 0.2f, new Vector3(0.36f, 0.21f, 0.36f), 12, 20);
                refs.obstacles.Add(new Vector3(p.x, p.z, 0.36f));
            }
            Obj("Poufs", SmoothFinish(pv, pt), m.fabric, furn);
            // Side tables at sofa ends.
            var stv = new List<Vector3>(); var stt = new List<int>();
            foreach (var a in new[] { 186f, 225f })
            {
                var p = Polar(6.9f, a, 0f);
                Ellipsoid(stv, stt, p + Vector3.up * 0.28f, new Vector3(0.3f, 0.28f, 0.3f), 12, 20);
                refs.obstacles.Add(new Vector3(p.x, p.z, 0.3f));
            }
            Obj("SideTables", SmoothFinish(stv, stt), m.table, furn);
            Plant(furn, "PlantWinL", Polar(6.85f, 136f, 0f), 1.15f, 101, m, refs.obstacles);
            Plant(furn, "PlantWinR", Polar(6.85f, 44f, 0f), 1.0f, 102, m, refs.obstacles);
            Plant(furn, "PlantBack", Polar(6.8f, 315f, 0f), 1.3f, 103, m, refs.obstacles, tall: true);
            Plant(furn, "PlantBackL", Polar(6.8f, 223f, 0f), 0.9f, 104, m, refs.obstacles);
            Plant(furn, "PlantTable", tableC + Polar(1.6f, 85f, 0f), 0.8f, 105, m, refs.obstacles);
            Plant(furn, "PlantLeft", Polar(6.75f, 186f + 8f, 0f), 1.25f, 106, m, refs.obstacles, tall: true);

            // 7's chair: a sculpted egg lounge chair on a tulip pedestal, facing the window.
            var pivot = new GameObject("ChairPivot").transform;
            pivot.SetParent(ship, false);
            pivot.position = ChairPos + Vector3.up * 0.25f;
            pivot.rotation = Quaternion.LookRotation(Vector3.forward);
            var shV = new List<Vector3>(); var shT = new List<int>();
            var shellC = new Vector3(0f, 0.5f, -0.04f); var shellR = new Vector3(0.66f, 0.52f, 0.46f);   // low pod: the camera looks over its back
            Func<Vector3, bool> cut = d => d.z > -0.05f && d.y > -0.5f;   // front opening
            Ellipsoid(shV, shT, shellC, shellR, 28, 40, cut);
            var shell = MeshObject("ChairShell", SaveMesh(SmoothFinish(shV, shT), "Saucer_ChairShell"), m.chairShell, pivot);
            var liV = new List<Vector3>(); var liT = new List<int>();
            Ellipsoid(liV, liT, shellC, shellR * 0.93f, 28, 40, cut);
            MeshObject("ChairLining", SaveMesh(SmoothFinish(liV, liT), "Saucer_ChairLining"), m.chairLining, pivot);
            // Rim along the opening.
            var rimV = new List<Vector3>(); var rimT = new List<int>();
            var rimPath = new List<Vector3>();
            for (int i = 0; i <= 48; i++)
            {
                // Walk the boundary of the cut: the circle where d.z = -0.05 (upper part) closed by the line d.y = -0.5.
                float a = Mathf.Lerp(-30f, 210f, i / 48f) * Mathf.Deg2Rad;
                var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                float zz = -0.05f; float s = Mathf.Sqrt(1f - zz * zz);
                d = new Vector3(d.x * s, Mathf.Max(d.y * s, -0.5f), zz);
                rimPath.Add(shellC + Vector3.Scale(d, shellR * 0.965f));
            }
            Sweep(rimV, rimT, rimPath, 0.03f, false, 8);
            MeshObject("ChairRim", SaveMesh(SmoothFinish(rimV, rimT), "Saucer_ChairRim"), m.chairShell, pivot);
            var cuV = new List<Vector3>(); var cuT = new List<int>();
            Ellipsoid(cuV, cuT, new Vector3(0f, 0.19f, 0.02f), new Vector3(0.44f, 0.06f, 0.4f), 10, 24);
            MeshObject("ChairCushion", SaveMesh(SmoothFinish(cuV, cuT), "Saucer_ChairCushion"), m.chairLining, pivot);
            var seat = new GameObject("SeatPoint").transform;
            seat.SetParent(pivot, false);
            seat.position = ChairPos + Vector3.up * SeatHipsHeight;
            Obj("ChairPedestal", Lathe(Resample(new[] { new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0.47f, 0.035f), new Vector2(0.12f, 0.11f),
                new Vector2(0.075f, 0.2f), new Vector2(0.07f, 0.29f), new Vector2(0f, 0.29f) }, 0.04f), 0f, 360f, 48, null, ChairPos), m.pedestal, furn);
            refs.obstacles.Insert(0, new Vector3(ChairPos.x, ChairPos.z - 0.1f, 0.55f));   // obstacle 0 = the chair
            refs.chairPivot = pivot; refs.seat = seat;
            return refs;
        }

        /// <summary>Potted plant: glossy pot + drooping grayscale leaves (or a tall stem with a crown).</summary>
        static void Plant(Transform parent, string name, Vector3 at, float size, int seed, ShipMats m, List<Vector3> obstacles, bool tall = false)
        {
            var rnd = new System.Random(seed);
            float R() => (float)rnd.NextDouble();
            var go = new GameObject(name).transform; go.SetParent(parent, false);
            float potH = 0.42f * size, potR = 0.24f * size;
            Obj(name + "_Pot", Lathe(Resample(new[] { new Vector2(0f, 0f), new Vector2(potR * 0.7f, 0f), new Vector2(potR, potH * 0.85f),
                new Vector2(potR * 1.05f, potH), new Vector2(potR * 0.92f, potH), new Vector2(0f, potH * 0.95f) }, 0.04f), 0f, 360f, 32, null, at), m.pot, go);
            var lv = new List<Vector3>(); var lt = new List<int>();
            var sv = new List<Vector3>(); var stt = new List<int>();
            var top = at + Vector3.up * potH * 0.95f;
            if (tall)
            {
                float h = 1.3f * size;
                Sweep(sv, stt, new[] { top, top + new Vector3(0.03f, h * 0.5f, 0.02f), top + new Vector3(-0.02f, h, 0.04f) }, 0.03f * size, false, 6);
                top += new Vector3(-0.02f, h, 0.04f);
            }
            int leaves = tall ? 14 : 22;
            for (int i = 0; i < leaves; i++)
            {
                float yaw = (i + R() * 0.6f) / leaves * Mathf.PI * 2f;
                float elev = Mathf.Lerp(tall ? 10f : 35f, tall ? 45f : 75f, R()) * Mathf.Deg2Rad;
                float len = Mathf.Lerp(0.45f, 0.85f, R()) * size * (tall ? 0.9f : 1f);
                float w = Mathf.Lerp(0.07f, 0.12f, R()) * size;
                var dir = new Vector3(Mathf.Cos(yaw) * Mathf.Cos(elev), Mathf.Sin(elev), Mathf.Sin(yaw) * Mathf.Cos(elev));
                var side = Vector3.Cross(dir, Vector3.up).normalized;
                int b = lv.Count;
                const int segs = 7;
                for (int s = 0; s <= segs; s++)
                {
                    float u = (float)s / segs;
                    var p = top + dir * len * u + Vector3.down * (len * 0.55f * u * u);     // droops toward the tip
                    float ww = w * Mathf.Sin(Mathf.PI * Mathf.Clamp(u, 0.05f, 1f)) * (1f - 0.3f * u);
                    var curl = Vector3.up * ww * 0.25f;                                   // slight V fold
                    lv.Add(p - side * ww + curl); lv.Add(p + Vector3.up * 0.002f); lv.Add(p + side * ww + curl);
                }
                for (int s = 0; s < segs; s++)
                {
                    int a = b + s * 3;
                    for (int k = 0; k < 2; k++)
                    {
                        lt.Add(a + k); lt.Add(a + 3 + k); lt.Add(a + k + 1);
                        lt.Add(a + k + 1); lt.Add(a + 3 + k); lt.Add(a + 4 + k);
                    }
                }
            }
            Obj(name + "_Leaves", SmoothFinish(lv, lt), m.leaf, go);
            if (sv.Count > 0) Obj(name + "_Stem", SmoothFinish(sv, stt), m.stem, go);
            obstacles.Add(new Vector3(at.x, at.z, potR + 0.08f));
        }

        // --------------------------------------------------------------------------------- the city
        static void BuildCity(Transform parent, ShipMats m)
        {
            var city = new GameObject("City").transform; city.SetParent(parent, false);
            Obj("CityGround", Lathe(new[] { new Vector2(0f, CityY), new Vector2(2200f, CityY) }, 0f, 360f, 64), m.cityGround, city, false);
            var rnd = new System.Random(2026);
            float R() => (float)rnd.NextDouble();
            var tv = new List<Vector3>(); var tt = new List<int>();
            var gv = new List<Vector3>(); var gt = new List<int>();
            // Clusters (centre xz, radius, towers). The first sits ahead of and below the window.
            var clusters = new List<(Vector2 c, float r, int n)>
            {
                (new Vector2(0f, 95f), 70f, 26), (new Vector2(-150f, 40f), 60f, 16), (new Vector2(170f, 120f), 70f, 18),
                (new Vector2(30f, 300f), 90f, 22), (new Vector2(-260f, 260f), 90f, 16), (new Vector2(300f, -60f), 80f, 14),
                (new Vector2(-280f, -180f), 90f, 14), (new Vector2(40f, -280f), 90f, 16), (new Vector2(0f, -40f), 50f, 10),
                (new Vector2(420f, 400f), 120f, 14), (new Vector2(-480f, 40f), 120f, 12),
            };
            foreach (var (c, r, n) in clusters)
                for (int i = 0; i < n; i++)
                {
                    float a = R() * Mathf.PI * 2f, d = Mathf.Sqrt(R()) * r;
                    var p = new Vector3(c.x + Mathf.Cos(a) * d, CityY, c.y + Mathf.Sin(a) * d);
                    float w = Mathf.Lerp(10f, 26f, R()), h = Mathf.Lerp(35f, 165f, R() * R() + 0.15f);
                    float yaw = R() * 90f;
                    int tiers = 1 + rnd.Next(3);
                    float y = CityY, ww = w;
                    for (int k = 0; k < tiers; k++)
                    {
                        float th = h * (k == 0 ? 0.6f : 0.4f / (tiers - 1));
                        AddBox(tv, tt, new Vector3(p.x, y + th * 0.5f, p.z), new Vector3(ww, th, ww * Mathf.Lerp(0.7f, 1.2f, R())), yaw);
                        // Glowing bands near the top of each tier.
                        AddBox(gv, gt, new Vector3(p.x, y + th - 2.2f, p.z), new Vector3(ww + 0.8f, 0.9f, ww + 0.8f), yaw);
                        y += th; ww *= Mathf.Lerp(0.55f, 0.8f, R());
                    }
                    AddBox(gv, gt, new Vector3(p.x, y + 4f, p.z), new Vector3(0.8f, 8f, 0.8f), yaw);   // antenna light
                }
            Obj("CityTowers", Finish(tv, tt), m.city, city, false);
            Obj("CityLights", Finish(gv, gt), m.cityGlow, city, false);
            // Ring terraces like the hill world's, rising out of the fog below.
            var rings = new[] { (new Vector3(-60f, CityY + 20f, 170f), 1.3f, 4, 11), (new Vector3(230f, CityY + 10f, 300f), 1.8f, 5, 12),
                (new Vector3(-320f, CityY + 15f, -40f), 1.5f, 4, 13), (new Vector3(120f, CityY + 15f, -200f), 1.4f, 4, 14) };
            int ri = 0;
            foreach (var (c, s, tiers, seed) in rings)
            {
                float f = Mathf.Atan2(-c.z, -c.x) * Mathf.Rad2Deg;
                Complex(city, "CityRing" + ri++, c, s, f - 130f, f + 130f, tiers, m.city, m.cityGlow, seed);
            }
        }

        // ---------------------------------------------------------------------------- atmosphere
        static GameObject BuildAtmosphereAbove(GameObject atmoPrefab)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(atmoPrefab);
            var a = inst.GetComponent<Seven.Atmosphere>();
            // "From above": the fog lives 200 m below; the walkable deck is far above any layer.
            a.FogStart = 6f;
            a.GroundDensity = 0.03f; a.GroundBase = CityY; a.GroundFalloff = 22f; a.GroundRange = 900f;
            a.ValleyDensity = 0.006f; a.ValleyBase = CityY + 20f; a.ValleyFalloff = 55f;
            a.DistanceDensity = 0.0016f; a.HazeHeight = 40f;
            a.NearBrightness = 0.8f; a.FarBrightness = 1.02f; a.BrightnessDistance = 450f; a.ZenithBrightness = 0.34f;
            a.NoiseStrength = 0.5f; a.NoiseScale = 0.006f; a.NoiseSpeed = 0.004f; a.NoiseFadeDistance = 600f;
            a.GlowIntensity = 0.32f; a.GlowRadius = 60f;
            var glows = new[]
            {
                (new Vector3(0f, CityY + 60f, 110f), new Vector3(1.2f, 1.1f, 1f)), (new Vector3(-170f, CityY + 50f, 60f), new Vector3(1f, 0.9f, 1f)),
                (new Vector3(190f, CityY + 60f, 140f), new Vector3(1f, 0.9f, 1f)), (new Vector3(40f, CityY + 60f, 330f), new Vector3(1.4f, 1f, 1f)),
                (new Vector3(-280f, CityY + 50f, 280f), new Vector3(1.2f, 0.8f, 1f)), (new Vector3(60f, CityY + 50f, -300f), new Vector3(1.2f, 0.8f, 1f)),
                (new Vector3(700f, -40f, 700f), new Vector3(5f, 1.6f, 1f)),
            };
            int gi = 0;
            foreach (Transform c in inst.transform)
                if (c.name.StartsWith("Glow") && gi < glows.Length) { c.position = glows[gi].Item1; c.localScale = glows[gi].Item2; gi++; }
            var prefab = PrefabUtility.SaveAsPrefabAsset(inst, AtmoAbovePath);
            UnityEngine.Object.DestroyImmediate(inst);
            Log("atmosphere variant saved " + AtmoAbovePath);
            return prefab;
        }

        // ------------------------------------------------------------------------------ the scene
        static void BuildSaucerScene(AnimatorController sevenCtl, AnimatorController catCtl, CatMats catMats, ShipMats sm, GameObject atmoAbove)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.24f, 0.24f);
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(RenderSettings.ambientLight);
            RenderSettings.ambientProbe = sh;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

            var atmo = (GameObject)PrefabUtility.InstantiatePrefab(atmoAbove);
            atmo.name = "Atmosphere_Above";

            var refs = BuildShip(sm);
            BuildCity(new GameObject("World").transform, sm);
            AddProbe(new Vector3(0f, 1.6f, 0f), new Vector3(20f, 4.5f, 20f), new Vector3(0f, 0.4f, 0f));

            var wg = new GameObject("WorldGround").AddComponent<Seven.WorldGround>();
            wg.Shape = Seven.Ground.Shape.Deck;
            wg.DeckRadius = 7.6f;
            wg.DeckSectors = NookAngles.Select(a => new Vector3(a - NookHalf, a + NookHalf, 8.95f)).ToArray();
            wg.DeckObstacles = refs.obstacles.ToArray();

            // 7 starts at the back of the deck, looking toward the window.
            var seven = Spawn(SevenDir + "/7_character.fbx", "7", new Vector3(-1.2f, 0f, -4.6f), new Vector3(0.2f, 0f, 1f), sevenCtl);

            var camGo = new GameObject("SaucerCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.62f, 0.62f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 2500f;
            camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.requiresDepthTexture = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camGo.transform.position = seven.transform.position + new Vector3(0f, 1.8f, -3f);

            var dirGo = new GameObject("SaucerDirector");
            var input = dirGo.AddComponent<Seven.SaucerInput>();
            var s7 = seven.AddComponent<Seven.SaucerSeven>();
            s7.Input = input; s7.CameraTransform = camGo.transform; s7.ChairPivot = refs.chairPivot; s7.SeatPoint = refs.seat;
            var scam = camGo.AddComponent<Seven.SaucerCamera>();
            scam.Seven = s7; scam.Input = input; scam.Chair = refs.chairPivot;
            scam.Nooks = NookAngles.Select(a => new Vector3(a, NookHalf, NookR - 0.45f)).ToArray();
            scam.NookCeiling = NookH - 0.15f;

            // The cat: root (scale, AI) + model child turned so its head points +Z.
            var catRoot = new GameObject("Cat");
            catRoot.transform.position = new Vector3(1.3f, 0f, -3.4f);
            catRoot.transform.rotation = Quaternion.LookRotation(new Vector3(-0.6f, 0f, 1f));
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CatFbx));
            model.name = "CatModel";
            model.transform.SetParent(catRoot.transform, false);
            var head = model.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "head");
            var pelvis = model.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "pelvis");
            if (head != null && pelvis != null)
            {
                var fwd = catRoot.transform.InverseTransformDirection(head.position - pelvis.position); fwd.y = 0f;
                float yaw = Vector3.SignedAngle(fwd, Vector3.forward, Vector3.up);
                model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * model.transform.localRotation;
                Log($"cat model turned {yaw:0.0} deg so its head points +Z");
            }
            else Log("cat: head/pelvis bones not found; facing not corrected");
            var canim = model.GetComponent<Animator>();
            if (canim == null) canim = model.AddComponent<Animator>();
            canim.runtimeAnimatorController = catCtl;
            canim.applyRootMotion = false;
            canim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.updateWhenOffscreen = true;
                smr.sharedMaterials = smr.sharedMaterials.Select(mm => mm != null && mm.name.Contains("whisker") ? catMats.whisker : catMats.fur).ToArray();
            }
            var cb = new Bounds(catRoot.transform.position, Vector3.zero);
            foreach (var r in model.GetComponentsInChildren<Renderer>()) cb.Encapsulate(r.bounds);
            Log($"cat bind-pose bounds size {cb.size} (scale 1: expect ~0.40 m tall incl. raised tail, 0.25 m shoulder)");
            var cat = catRoot.AddComponent<Seven.CatCompanion>();
            cat.Seven = s7; cat.Anim = canim; cat.ChairPivot = refs.chairPivot;
            cat.Additive = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Fx_additive.mat");

            var revGo = new GameObject("ShipReveal");
            var rev = revGo.AddComponent<Seven.ShipReveal>();
            rev.Center = refs.seat; rev.ChairPivot = refs.chairPivot;
            rev.Additive = cat.Additive;
            rev.InteriorLights = refs.lights; rev.Glass = refs.glass;

            var legendGo = new GameObject("ControlsLegend");
            var legend = legendGo.AddComponent<Seven.ControlsLegend>();
            legend.Cam = cam;
            legend.Set = Seven.ControlsLegend.Layout.SaucerWalk;

            var dir = dirGo.AddComponent<Seven.SaucerDirector>();
            dir.Seven = s7; dir.Cam = scam; dir.Cat = cat; dir.Reveal = rev; dir.Legend = legend; dir.Input = input;

            // Round 5: Kyle's art TV.
            var gtv = refs.tv.tv.gameObject.AddComponent<Seven.GalleryTV>();
            gtv.Screen = refs.tv.screen; gtv.UiMaterial = GalleryUiMaterial(); gtv.ScreenLight = refs.tv.light;
            gtv.Seven = s7; gtv.Cam = scam; gtv.Input = input; gtv.Reveal = rev; gtv.ScreenSize = TvSize;
            dir.Gallery = gtv;

            EditorSceneManager.SaveScene(scene, SaucerScenePath);
            Log("scene saved " + SaucerScenePath);
        }
    }
}
