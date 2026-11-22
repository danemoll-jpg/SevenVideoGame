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
                // QF24 (8a): no dangling cable / toy any more (it never read as a toy and was never on the ship).
                // QF24 (8b): he was half hanging off the console. Two measured places (temporary colliders on the interior,
                // raycasts over his footprint - every sample must land on the same flat surface):
                //  - the PILOT'S CHAIR, in one of his larger forms (he changes size): sitting on the seat cushion facing the
                //    display, his head at the display's height (the default for watching / composed / talking);
                //  - fully ON the console top in front of the display (all four paws on it) - for batting at the screen.
                // straight ray-triangle tests on the interior's own meshes (edit-mode physics on a prefab instance is unreliable)
                var surf = new List<(Bounds b, Vector3[] v, int[] t)>();
                foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true))
                {
                    var rr = mf.GetComponent<Renderer>(); if (mf.sharedMesh == null || rr == null || !mf.sharedMesh.isReadable) continue;
                    var mv = mf.sharedMesh.vertices; var wv = new Vector3[mv.Length]; for (int q = 0; q < mv.Length; q++) wv[q] = mf.transform.TransformPoint(mv[q]);
                    surf.Add((rr.bounds, wv, mf.sharedMesh.triangles));
                }
                bool RayDown(Vector3 o, out float hitY)
                {
                    hitY = float.NegativeInfinity; bool any = false;
                    foreach (var sf in surf)
                    {
                        if (o.x < sf.b.min.x || o.x > sf.b.max.x || o.z < sf.b.min.z || o.z > sf.b.max.z || sf.b.min.y > o.y) continue;
                        for (int q = 0; q + 2 < sf.t.Length; q += 3)
                        {
                            Vector3 a0 = sf.v[sf.t[q]], b0 = sf.v[sf.t[q + 1]], c0 = sf.v[sf.t[q + 2]];
                            var e1 = b0 - a0; var e2 = c0 - a0; var h = Vector3.Cross(Vector3.down, e2); float det = Vector3.Dot(e1, h); if (Mathf.Abs(det) < 1e-9f) continue;
                            float f = 1f / det; var sv = o - a0; float u = f * Vector3.Dot(sv, h); if (u < 0f || u > 1f) continue;
                            var qv = Vector3.Cross(sv, e1); float w = f * Vector3.Dot(Vector3.down, qv); if (w < 0f || u + w > 1f) continue;
                            float d = f * Vector3.Dot(e2, qv); if (d <= 0f || d > 1.2f) continue;
                            float y = o.y - d; if (y > hitY) { hitY = y; any = true; }
                        }
                    }
                    return any;
                }
                var fwdD = toD.normalized; var rightD = Vector3.Cross(Vector3.up, fwdD);
                // a footprint: half-length along his body, half-width; samples at the corners, the mid-sides and the centre
                bool Support(Vector3 c, float hl, float hw, float yRef, out float top, out float spread)
                {
                    top = -99f; spread = 99f; float lo = 99f, hi = -99f; int n = 0;
                    foreach (var a in new[] { -1f, 0f, 1f }) foreach (var b in new[] { -1f, 0f, 1f })
                    {
                        var o = c + fwdD * hl * a + rightD * hw * b + Vector3.up * 0.6f;
                        if (!RayDown(o, out float hy)) return false;
                        lo = Mathf.Min(lo, hy); hi = Mathf.Max(hi, hy); n++;
                        if (Mathf.Abs(hy - yRef) > 0.25f) return false;
                    }
                    top = hi; spread = hi - lo; return n == 9;
                }
                // the chair: the seat cushion's top (the cushion ellipsoid: centre 0.445 m, half-height 0.075 m in the chair's
                // frame; the PilotSeat marker sits at 0.60 m) - a big form sitting on it, facing the display
                float chairScale = 2.1f;
                var up2 = seat.up; var chairTop = seat.position - up2 * 0.08f + fwdD * 0.03f;
                float chairSpread = 0f; bool chairOk = RayDown(chairTop + Vector3.up * 0.5f, out float chY) && Mathf.Abs(chY - chairTop.y) < 0.03f;
                if (chairOk) { chairSpread = Mathf.Abs(chY - chairTop.y); chairTop.y = chY; }
                ac.ChairSpot = new GameObject("CutPantherChairSpot").transform; ac.ChairSpot.SetParent(root.transform, false);
                ac.ChairSpot.position = chairTop; ac.ChairSpot.rotation = Quaternion.LookRotation(fwdD);
                ac.ChairScale = chairScale; ac.ChairHalf = new Vector2(0.29f, 0.28f);   // the cushion's own half-sizes
                // the navigation desk in front of the display: its top runs from r 2.01 to 2.34 m (0.77 -> 0.82 m high), the
                // display stands from r 2.30 - so the spot is 0.15 m in front of the display's foot, on the desk top
                var best = disp.position - fwdD * 0.15f; best.y = disp.position.y - 0.442f;
                float deskSpread = 0f; bool deskOk = RayDown(best + Vector3.up * 0.5f, out float dkY) && Mathf.Abs(dkY - best.y) < 0.05f;
                if (deskOk) { deskSpread = Mathf.Abs(dkY - best.y); best.y = dkY; }
                ac.DeskSpot = new GameObject("CutPantherDeskSpot").transform; ac.DeskSpot.SetParent(root.transform, false);
                ac.DeskSpot.position = best; ac.DeskSpot.rotation = Quaternion.LookRotation(fwdD); ac.DeskHalf = new Vector2(0.12f, 0.15f);
                Log($"PANTHER PLACE (QF24 8): the pilot's chair - cushion top {chairTop.y - root.transform.position.y:0.000} m above the deck ({(chairOk ? $"a ray down confirms it within {chairSpread * 100f:0.0} cm" : "from the cushion's built shape")}), his larger form x{chairScale:0.0}; the navigation desk top {best.y - root.transform.position.y:0.000} m ({(deskOk ? $"ray-confirmed within {deskSpread * 100f:0.0} cm" : "from the desk's built shape")}), {Vector3.Distance(best, disp.position):0.00} m from the display (paw reach) - his paws vs these surfaces are measured in every cut (PANTHER PLACE lines in check runs)");
                cr.transform.position = ac.ChairSpot.position; cr.transform.rotation = ac.ChairSpot.rotation; cr.transform.localScale = Vector3.one * chairScale;
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
            Log($"QF23 (10) / QF24 (8): Panther's cutaway set in this scene - the real saucer interior at {at}, Panther in the pilot's chair (larger form) / on the console top to bat at the screen, the display = a live map round her");
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
