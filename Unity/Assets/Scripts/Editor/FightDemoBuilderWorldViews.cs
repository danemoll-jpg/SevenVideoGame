using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF18 (2, 3): the views of a world aboard are built from the REAL level, at build time, so they always match it.
    ///  - Resources/Outside/Outside_&lt;world&gt;: every renderer of the level (same meshes, same materials) - the saucer's window
    ///    looks out over the actual plain / ravine / cities / temple (Mars), gardens (Venus), ice field (Europa), placed under
    ///    the ship by <see cref="Seven.SaucerOutside"/> (QF16's procedural ground was a single ravine outline in fog).
    ///  - Resources/Holo/Holo_&lt;world&gt;: the level's big shapes in the console's glowing hologram style (one additive white
    ///    material), shown over the navigation display by <see cref="Seven.HoloDisplay"/> instead of a caption.
    /// No GPU needed (the build runs -nographics): the views are 3D copies, not baked pictures.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        static readonly string[] SkipRoots = { "7", "HoverBike", "ChapterCamera", "ChapterUI", "ControlsLegend", "WorldGround", "GuideLight", "MarsDirector", "VenusDirector", "EuropaDirector", "Chapter1Director" };

        static bool ViewSkip(Renderer r)
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy || !(r is MeshRenderer)) return true;
            var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) return true;
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mf.sharedMesh))) return true;               // only saved meshes can be referenced
            for (var t = r.transform; t != null; t = t.parent)
            {
                if (SkipRoots.Contains(t.name) || t.name.StartsWith("Atmosphere") || t.name.StartsWith("Saucer") || t.name.StartsWith("Jammer") || t.name.StartsWith("Viper")) return true;
                var an = t.GetComponent<Animator>(); if (an != null && an.isHuman) return true;
            }
            return false;
        }

        static void BuildWorldViews()
        {
            var holoMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Holo_glow.mat");
            if (holoMat == null)
            {
                var sh = Shader.Find("Seven/Additive");
                holoMat = new Material(sh != null ? sh : Shader.Find("Universal Render Pipeline/Unlit")) { name = "Holo_glow" };
                AssetDatabase.CreateAsset(holoMat, MatDir + "/Holo_glow.mat");
            }
            if (holoMat.HasProperty("_Intensity")) holoMat.SetFloat("_Intensity", 0.09f);
            if (holoMat.HasProperty("_MainTex")) holoMat.SetTexture("_MainTex", Texture2D.whiteTexture);
            EditorUtility.SetDirty(holoMat);
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(ProjectRoot, "Assets/Resources/Outside"));
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(ProjectRoot, "Assets/Resources/Holo"));
            // (world, scene, window radius, holo region (x0, z0, x1, z1), holo min size)
            var worlds = new (string w, string scene, float radius, Vector4 region, float minSize)[]
            {
                ("mars", MarsScenePath, 2600f, new Vector4(-600f, -130f, 600f, 230f), 3f),
                ("venus", VenusScenePath, 1400f, new Vector4(-260f, -140f, 260f, 420f), 2f),
                ("europa", EuropaScenePath, 1400f, new Vector4(-260f, -180f, 260f, 420f), 2f),
                ("cattalus", CattalusScenePath, 1300f, new Vector4(-340f, -60f, 760f, 900f), 3f),   // Round 18: the window over Cattalus, CAT_01 / CAT_12's exterior
                ("moon", Chapter1ScenePath, 900f, new Vector4(-450f, -450f, 450f, 450f), 4f),        // Round 18: CAT_12's lunar interior (the hologram)
            };
            foreach (var (w, path, radius, region, minSize) in worlds)
            {
                if (!System.IO.File.Exists(System.IO.Path.Combine(ProjectRoot, path))) { Log($"world views: {path} missing"); continue; }
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var all = Object.FindObjectsByType<MeshRenderer>().Where(r => !ViewSkip(r)).ToList();
                // the window view: everything (the real materials)
                var win = new GameObject("Outside_" + w + "_Level"); int nw = 0, tw = 0;
                foreach (var r in all)
                {
                    if (Flat(r.bounds.center).magnitude > radius) continue;
                    var c = Copy(r, win.transform, r.sharedMaterials); nw++; tw += r.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
                }
                PrefabUtility.SaveAsPrefabAsset(win, $"Assets/Resources/Outside/Outside_{w}.prefab"); Object.DestroyImmediate(win);
                // the hologram: the big shapes inside the region, one glowing material
                var holo = new GameObject("Holo_" + w); int nh = 0;
                foreach (var r in all)
                {
                    var b = r.bounds; var ctr = b.center;
                    if (ctr.x < region.x || ctr.x > region.z || ctr.z < region.y || ctr.z > region.w) continue;
                    if (Mathf.Max(b.size.x, b.size.y, b.size.z) < minSize) continue;
                    if (r.sharedMaterial != null && r.sharedMaterial.renderQueue > 2500) continue;
                    // the big flat ground slabs would fill the hologram with a wall of light - the shapes stand on nothing
                    string nm = r.name; if (nm.StartsWith("Plain") || nm.StartsWith("Dune") || nm.Contains("RavineFloor") || nm.StartsWith("ChannelBed") || nm.StartsWith("Ground") || nm.Contains("Terrain") || nm.Contains("IceField") || Mathf.Max(b.size.x, b.size.z) > 300f) continue;
                    Copy(r, holo.transform, new[] { holoMat }); nh++;
                }
                PrefabUtility.SaveAsPrefabAsset(holo, $"Assets/Resources/Holo/Holo_{w}.prefab"); Object.DestroyImmediate(holo);
                Log($"WORLD VIEWS (QF18 2-3) {w}: window view {nw} renderers ({tw / 1000}k triangles) from the real level; hologram {nh} renderers (region {region})");
            }
        }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        static GameObject Copy(Renderer r, Transform parent, Material[] mats)
        {
            var g = new GameObject(r.name); g.transform.SetParent(parent, false);
            g.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation); g.transform.localScale = r.transform.lossyScale;
            g.AddComponent<MeshFilter>().sharedMesh = r.GetComponent<MeshFilter>().sharedMesh;
            var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterials = mats; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            return g;
        }
    }
}
