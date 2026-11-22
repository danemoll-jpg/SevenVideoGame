using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF23 (10, 15) - Panther's cutaways aboard show the REAL saucer interior, in every chapter scene that cuts to him.
    ///  - <see cref="SaveInteriorPrefab"/> (end of the Saucer scene build): the ship's interior (deck, walls, the window frame,
    ///    the console + its screens, the pilot's chair, the furniture, its lights) copied into Assets/Saucer/SaucerInterior.prefab,
    ///    geometry and lights only (no scripts, no colliders).
    ///  - <see cref="PlaceAboardCut"/>: an instance of it far from the level, Panther on the console desk in front of the
    ///    navigation display (the cat model + controller + CatPoseOverlay), a dangling cable to bat at, and the display wired to
    ///    an orthographic map camera over the level (her marker, Ratwing's marker) - the runtime AboardCut.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const string InteriorPrefabPath = "Assets/Saucer/SaucerInterior.prefab";

        static void SaveInteriorPrefab()
        {
            var ship = GameObject.Find("Ship");
            if (ship == null) { Log("WARNING (QF23 10): no 'Ship' in the Saucer scene - no interior prefab"); return; }
            var copy = Object.Instantiate(ship); copy.name = "SaucerInterior";
            int stripped = 0;
            foreach (var c in copy.GetComponentsInChildren<Component>(true).Reverse())
            {
                if (c == null || c is Transform || c is MeshFilter || c is MeshRenderer || c is Light || c is LODGroup) continue;
                if (c is Canvas || c is UnityEngine.UI.Graphic || c is CanvasRenderer || c is UnityEngine.UI.CanvasScaler || c is RectTransform) { Object.DestroyImmediate(c.gameObject); stripped++; continue; }
                Object.DestroyImmediate(c); stripped++;
            }
            // nothing left that could fade it (the reveal keyword off): the cut shows the ship whole
            foreach (var r in copy.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            Directory.CreateDirectory(Path.Combine(ProjectRoot, "Assets/Saucer"));
            PrefabUtility.SaveAsPrefabAsset(copy, InteriorPrefabPath);
            Object.DestroyImmediate(copy);
            Log($"QF23 (10): the saucer interior saved for Panther's cutaways -> {InteriorPrefabPath} ({ship.GetComponentsInChildren<Renderer>(true).Length} renderers, {stripped} components stripped)");
        }

        /// <summary>The cutaway set in a chapter scene, at <paramref name="at"/> (far from the level).</summary>
        static Seven.AboardCut PlaceAboardCut(Transform parent, Vector3 at)
        {
            var pre = AssetDatabase.LoadAssetAtPath<GameObject>(InteriorPrefabPath);
            if (pre == null) { Log("WARNING (QF23 10): " + InteriorPrefabPath + " missing - no aboard cutaways"); return null; }
            var root = new GameObject("AboardCut"); root.transform.SetParent(parent, false); root.transform.position = at;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(pre); inst.transform.SetParent(root.transform, false); inst.transform.localPosition = Vector3.zero;
            var all = inst.GetComponentsInChildren<Transform>(true);
            var seat = all.FirstOrDefault(t => t.name == "PilotSeat"); var disp = all.FirstOrDefault(t => t.name == "NavDisplayCentre");
            var ac = root.AddComponent<Seven.AboardCut>(); ac.Interior = inst.transform; ac.PilotSeat = seat; ac.Display = disp;
            // Panther on the console desk beside the chair, facing the display (where he sits in the route scenes)
            var catFbx = AssetDatabase.LoadAssetAtPath<GameObject>(CatFbx); var catCtl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "/Cat.controller");
            if (catFbx != null && seat != null && disp != null)
            {
                var fwd = Vector3.ProjectOnPlane(seat.forward, Vector3.up).normalized; var right = Vector3.Cross(Vector3.up, fwd);
                var deskAt = seat.position + fwd * 0.62f - right * 0.42f; deskAt.y = disp.position.y - 0.34f;
                var cr = new GameObject("CutPanther"); cr.transform.SetParent(root.transform, false); cr.transform.position = deskAt;
                var cm = (GameObject)PrefabUtility.InstantiatePrefab(catFbx); cm.transform.SetParent(cr.transform, false);
                var can = cm.GetComponent<Animator>() ?? cm.AddComponent<Animator>(); can.runtimeAnimatorController = catCtl; can.applyRootMotion = false; can.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var fur = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Cat_fur.mat"); var wh = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Cat_whisker.mat");
                foreach (var smr in cm.GetComponentsInChildren<SkinnedMeshRenderer>()) { smr.updateWhenOffscreen = true; smr.sharedMaterials = smr.sharedMaterials.Select(mm => mm != null && mm.name.Contains("whisker") ? wh : fur).ToArray(); }
                var toD = Vector3.ProjectOnPlane(disp.position - deskAt, Vector3.up); cr.transform.rotation = Quaternion.LookRotation(toD.sqrMagnitude > 0.01f ? toD : fwd);
                ac.Cat = cr.transform; ac.CatAnim = can; ac.Pose = cr.AddComponent<Seven.CatPoseOverlay>();
                // a dangling cable from under the console lip, within paw reach
                var cable = new GameObject("DanglingCable").transform; cable.SetParent(root.transform, false); cable.position = deskAt + toD.normalized * 0.25f + right * 0.22f + Vector3.up * 0.42f;
                var cv = new List<Vector3>(); var ct = new List<int>(); Sweep(cv, ct, new[] { Vector3.zero, new Vector3(0.01f, -0.14f, 0f), new Vector3(0f, -0.3f, 0.01f) }, 0.006f, false, 5); Ellipsoid(cv, ct, new Vector3(0f, -0.31f, 0.01f), Vector3.one * 0.014f, 3, 6);
                var cg = MeshObject("Cable", SaveMesh(SmoothFinish(cv, ct), "QF23_CutCable_" + parent.name), Markers.DarkGloss(), cable, false); cg.transform.localPosition = Vector3.zero;
                ac.Cable = cable;
            }
            else Log("WARNING (QF23 10): no cat model / no console markers for the cutaway set");
            // the display: what the saucer's instruments see - a live top-down map round her, her marker, his
            if (disp != null)
            {
                var mc = new GameObject("CutMapCam").AddComponent<Camera>(); mc.transform.SetParent(root.transform, false);
                mc.orthographic = true; mc.orthographicSize = 24f; mc.clearFlags = CameraClearFlags.SolidColor; mc.backgroundColor = new Color(0.04f, 0.04f, 0.04f); mc.nearClipPlane = 1f; mc.farClipPlane = 200f;
                mc.cullingMask = ~((1 << 5)); mc.enabled = false; ac.MapCam = mc;
                var scr = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(scr.GetComponent<Collider>()); scr.name = "CutMapScreen"; scr.transform.SetParent(root.transform, false);
                scr.transform.position = disp.position + disp.forward * 0.012f; scr.transform.rotation = Quaternion.LookRotation(-disp.forward, disp.up); scr.transform.localScale = new Vector3(0.55f, 0.42f, 1f);
                var sm = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "QF23_CutMapScreen" }; if (sm.HasProperty("_Cull")) sm.SetFloat("_Cull", 0f); AssetDatabase.CreateAsset(sm, MatDir + "/QF23_CutMapScreen_" + parent.name + ".mat");
                var sr = scr.GetComponent<Renderer>(); sr.sharedMaterial = sm; sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; ac.Screen = sr;
                foreach (var (n, k, size) in new[] { ("CutHerMark", 1.8f, 1.6f), ("CutRatMark", 1.2f, 2.4f) })
                {
                    var mk = new GameObject(n); mk.layer = Seven.AboardCut.MapLayer; mk.transform.SetParent(root.transform, false);
                    mk.AddComponent<MeshFilter>().sharedMesh = SaveMesh(Markers_Ring(size), "QF23_" + n + "_" + parent.name);
                    var mr = mk.AddComponent<MeshRenderer>(); mr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = n }; mr.sharedMaterial.SetColor("_BaseColor", Color.white); AssetDatabase.CreateAsset(mr.sharedMaterial, MatDir + "/QF23_" + n + "_" + parent.name + ".mat");
                    if (n == "CutHerMark") ac.HerMark = mk.transform; else ac.RatMark = mk.transform;
                }
            }
            // the ship's own light, so the cut is lit like the ship even far from the level's lights
            PointLight("CutFill", (seat != null ? seat.position : at) + Vector3.up * 2.2f, 7f, 1.6f, root.transform);
            Log($"QF23 (10): Panther's cutaway set in this scene - the real saucer interior at {at}, Panther on the console desk, the display = a live map round her");
            return ac;
        }
        static Mesh Markers_Ring(float r)
        {
            var v = new List<Vector3>(); var t = new List<int>(); int n = 32; float w = r * 0.22f;
            for (int i = 0; i <= n; i++) { float a = i * Mathf.PI * 2f / n; var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); v.Add(d * (r - w)); v.Add(d * r); }
            for (int i = 0; i < n; i++) { int a = i * 2; t.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2, a, a + 3, a + 1, a, a + 2, a + 3 }); }
            var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
    }
}
