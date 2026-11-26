using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 22 (Grand Circuit Stage 3): the new racers on Dan's ShiftStorm jets (his own models, Art/Source/shiftstorm/<Design>/Plane.fbx
    /// + Plane_basecolor.jpg, cleaned by ShiftStorm's pipeline: ~4 m, the empties "Seat" / "Steering"):
    ///  - the Scrambler's jet form (a retro motorbike with its wings and fan wheels folded flat), ridden astride by Nela, Viper and the
    ///    a Scree (QF37; Dan's Mixamo bodies / the Scree model) in 7's riding pose (GcSevenRider, scaled to each rider);
    ///  - the Jetstream jet (a rocket-plane with an open cockpit) with Ratwing seated in it (QF37; was Juniper).
    /// Monochrome like everything else: the base colour turned to grayscale, and the parts Dan painted orange (the accents, the engine
    /// lights) split onto a glowing white material ("glow = white emission"). The LOD1 copy is dropped (racers are close to the camera).
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const string ShiftDir = GenDir + "/ShiftStorm";

        static void BuildGcRound22Templates(Transform tpl, AnimatorController sevenCtl, Seven.GcGame g)
        {
            g.ScramblerTpl = GcShiftStormJet(tpl, "Scrambler", true);
            g.JetTpl = GcShiftStormJet(tpl, "Jetstream", false);
            var riderCtl = GcRiderController(sevenCtl);
            var heights = new float[Seven.GcStats.All.Length];
            heights[0] = Seven.GcVehicles.SevenHeight;
            g.NelaTpl = GcHumanRider(tpl, "NelaTpl", "biavian", BiavianDir, "Bia_biavian", 1f, riderCtl, out heights[3]);
            g.ViperTpl = GcHumanRider(tpl, "ViperTpl", "viper", ViperDir, "Mars_viper", 1f, riderCtl, out heights[4]);
            // QF37 (Dan): a Scree on the Scrambler jet and Ratwing in the Jetstream's cockpit replace the Machinist and Juniper
            g.ScreeTpl = GcScreeRider(tpl, riderCtl, out heights[5]);
            g.RatwingTpl = GcRatwingTemplate(tpl);
            g.RiderHeight = heights;
            Log($"GC ROSTER (QF37): racers {string.Join(", ", Seven.GcStats.All.Select(s => s.Racer))}; templates Scrambler jet {(g.ScramblerTpl != null)}, Jetstream {(g.JetTpl != null)}, Nela {(g.NelaTpl != null)} ({heights[3]:0.00} m), Viper {(g.ViperTpl != null)} ({heights[4]:0.00} m), the Scree {(g.ScreeTpl != null)} ({heights[5]:0.00} m), Ratwing {(g.RatwingTpl != null)}");
        }

        /// <summary>A ShiftStorm jet as a template: nose to +z, underside at y 0, grayscale body + glowing accents, the rider's points.</summary>
        static GameObject GcShiftStormJet(Transform parent, string design, bool astride)
        {
            var srcDir = Path.Combine(RepoRoot, "Art", "Source", "shiftstorm", design);
            var fbxSrc = Path.Combine(srcDir, "Plane.fbx"); var texSrc = Path.Combine(srcDir, "Plane_basecolor.jpg");
            if (!File.Exists(fbxSrc)) { Log($"GC JET (R22): {fbxSrc} missing - no {design}"); return null; }
            Directory.CreateDirectory(Path.Combine(ProjectRoot, ShiftDir));
            var fbx = ShiftDir + "/" + design + "_jet.fbx";
            File.Copy(fbxSrc, Path.Combine(ProjectRoot, fbx), true);
            AssetDatabase.ImportAsset(fbx, ImportAssetOptions.ForceSynchronousImport);
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
            imp.animationType = ModelImporterAnimationType.None; imp.importAnimation = false; imp.isReadable = true;
            imp.materialImportMode = ModelImporterMaterialImportMode.None; imp.SaveAndReimport();

            // the texture: Dan's colours -> grayscale (a little lifted so the black parts still read), and the accent mask (his orange)
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false); bool haveTex = File.Exists(texSrc) && src.LoadImage(File.ReadAllBytes(texSrc));
            Material body, glow;
            if (haveTex)
            {
                var px = src.GetPixels(); var gpx = new Color[px.Length];
                for (int i = 0; i < px.Length; i++) { float l = px[i].r * 0.2126f + px[i].g * 0.7152f + px[i].b * 0.0722f; l = Mathf.Lerp(0.07f, 0.95f, Mathf.Pow(l, 0.9f)); gpx[i] = new Color(l, l, l, 1f); }
                var gt = new Texture2D(src.width, src.height, TextureFormat.RGB24, true); gt.SetPixels(gpx); gt.Apply();
                var texPath = ShiftDir + "/" + design + "_gray.png";
                File.WriteAllBytes(Path.Combine(ProjectRoot, texPath), gt.EncodeToPNG()); Object.DestroyImmediate(gt);
                AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceSynchronousImport);
                body = TexturedMat("Gc_" + design + "_jet", texPath, 0.62f, 0.4f);
            }
            else body = Dissolve("Gc_" + design + "_jet", Gray(0.6f), 0.6f, false, 0.4f);
            glow = Dissolve("Gc_" + design + "_glow", Gray(0.92f), 0.5f, false);
            glow.SetColor("_EmissionColor", Color.white * 1.35f); EditorUtility.SetDirty(glow);

            var r = new GameObject(design + "Tpl").transform; r.SetParent(parent, false);
            var visual = new GameObject("Visual").transform; visual.SetParent(r, false);
            var model = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(fbx)); model.name = design + "Model"; model.transform.SetParent(visual, false);
            foreach (var lod in model.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("LOD") && t.name != "LOD0").ToList()) Object.DestroyImmediate(lod.gameObject);
            foreach (var lg in model.GetComponentsInChildren<LODGroup>(true)) Object.DestroyImmediate(lg);
            var mf = model.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault();
            int accent = 0, total = 0;
            if (mf != null && mf.sharedMesh != null)
            {
                var m = mf.sharedMesh; var uv = m.uv; var tris = m.triangles; total = tris.Length / 3;
                var a = new List<int>(); var b = new List<int>();
                bool Orange(Vector2 u)
                {
                    if (!haveTex) return false;
                    var c = src.GetPixelBilinear(u.x, u.y); Color.RGBToHSV(c, out float h, out float s, out float v);
                    return s > 0.55f && v > 0.5f && h > 0.02f && h < 0.14f;   // Dan's orange / amber (the accents and the engine lights)
                }
                for (int i = 0; i < tris.Length; i += 3)
                {
                    int votes = 0;
                    if (uv != null && uv.Length == m.vertexCount)
                    {
                        var u0 = uv[tris[i]]; var u1 = uv[tris[i + 1]]; var u2 = uv[tris[i + 2]];
                        if (Orange((u0 + u1 + u2) / 3f)) votes += 2; if (Orange(u0)) votes++; if (Orange(u1)) votes++; if (Orange(u2)) votes++;
                    }
                    (votes >= 3 ? a : b).AddRange(new[] { tris[i], tris[i + 1], tris[i + 2] });
                }
                accent = a.Count / 3;
                var split = new Mesh { indexFormat = m.vertexCount > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
                split.vertices = m.vertices; split.normals = m.normals; split.uv = uv; if (m.tangents != null && m.tangents.Length == m.vertexCount) split.tangents = m.tangents;
                split.subMeshCount = 2; split.SetTriangles(b, 0); split.SetTriangles(a, 1);
                if (m.normals == null || m.normals.Length != m.vertexCount) split.RecalculateNormals();
                mf.sharedMesh = SaveMesh(split, "Gc_" + design + "_jet_split");
                var mr = mf.GetComponent<MeshRenderer>(); mr.sharedMaterials = new[] { body, glow };
            }
            Object.DestroyImmediate(src);
            // which way is the nose: the rider sits behind the middle (and behind the steering) - turn that way to +z
            var seatSrc = model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Seat");
            var steerSrc = model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Steering");
            var rs = model.GetComponentsInChildren<Renderer>(true); var bb = rs[0].bounds; foreach (var rr in rs) bb.Encapsulate(rr.bounds);
            if (seatSrc != null)
            {
                var fw = (steerSrc != null ? steerSrc.position : bb.center) - seatSrc.position; fw.y = 0f;
                float yaw = Vector3.SignedAngle(fw, Vector3.forward, Vector3.up);
                model.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * model.transform.rotation;
                Log($"GC JET (R22): {design} turned {yaw:0} deg so its nose leads (the seat behind {(steerSrc != null ? "the steering" : "the middle")})");
            }
            else Log($"GC JET (R22): {design} has no Seat empty - left as imported");
            GcGround(visual, design + " jet");
            // the rider's points (in the visual's space, so they scale with the jet)
            Transform P(string n, Vector3 world) { var x = new GameObject(n).transform; x.SetParent(visual, true); x.position = world; x.rotation = r.rotation; return x; }
            if (seatSrc != null)
            {
                seatSrc.name = "Seat_src"; if (steerSrc != null) steerSrc.name = "Steering_src";
                var up = Vector3.up; var f = Vector3.forward; var rt = Vector3.right; float s = visual.lossyScale.x;
                if (astride)
                {
                    var seat = seatSrc.position - up * 0.04f * s;                         // the empty sits 4 cm over the saddle's surface
                    var bar = steerSrc != null ? steerSrc.position : seat + f * 0.49f * s + up * 0.40f * s;
                    P("Seat", seat); P("GripL", bar - rt * 0.37f * s); P("GripR", bar + rt * 0.37f * s);
                    P("FootL", seat + (f * 0.28f - up * 0.14f - rt * 0.36f) * s); P("FootR", seat + (f * 0.28f - up * 0.14f + rt * 0.36f) * s);
                    Log($"GC JET (R22): {design} - saddle {r.InverseTransformPoint(seat)}, bar {r.InverseTransformPoint(bar)} (grips 0.74 m apart), accents {accent} / {total} triangles glowing");
                }
                else
                {
                    P("CatSeat", seatSrc.position);
                    Log($"GC JET (R22): {design} - cockpit seat {r.InverseTransformPoint(seatSrc.position)}, accents {accent} / {total} triangles glowing");
                }
            }
            return r.gameObject;
        }

        /// <summary>The riders' animator: 7's Blender-made racing tuck ("ride", a Humanoid clip - it retargets to any humanoid) + an idle.</summary>
        static AnimatorController GcRiderController(AnimatorController sevenCtl)
        {
            var path = AnimDir + "/GcRider.controller"; AssetDatabase.DeleteAsset(path);
            var ctl = AnimatorController.CreateAnimatorControllerAtPath(path); var sm = ctl.layers[0].stateMachine;
            var ls = ctl.layers; ls[0].iKPass = true; ctl.layers = ls;   // GcSevenRider seats the body in OnAnimatorIK (the ride clip's body point is ~100x off)
            Motion M(string st) => sevenCtl != null ? sevenCtl.layers[0].stateMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == st)?.motion : null;
            var ride = M("ride"); var idle = M("idle"); var sit = M("sitting_idle");
            if (ride != null) { var s = sm.AddState("ride"); s.motion = ride; sm.defaultState = s; }
            if (idle != null) { var s = sm.AddState("idle"); s.motion = idle; if (ride == null) sm.defaultState = s; }
            if (sit != null) { var s = sm.AddState("sitting_idle"); s.motion = sit; }   // an upright seat (a rider whose spine the tuck curls out of sight)
            Log($"GC RIDER (R22): GcRider.controller - ride {(ride != null ? ride.name : "MISSING")}, idle {(idle != null ? idle.name : "MISSING")}, sitting_idle {(sit != null ? sit.name : "MISSING")}");
            return ctl;
        }

        /// <summary>A Mixamo-bodied rider (the story's model and material), standing height measured as built.</summary>
        static GameObject GcHumanRider(Transform parent, string name, string kind, string dir, string matName, float scale, AnimatorController ctl, out float height)
        {
            height = 0f;
            var path = dir + "/" + kind + "_body_mixamo.fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/" + matName + ".mat");
            if (model == null || mat == null) { model = SetupMixamoFolk(kind, dir, out _, out mat); }
            if (model == null) { Log($"GC RIDER (R22): {kind} missing - {name} not built"); return null; }
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model); go.name = kind; go.transform.SetParent(root.transform, false);
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { smr.updateWhenOffscreen = true; if (mat != null) smr.sharedMaterials = smr.sharedMaterials.Select(_ => mat).ToArray(); }
            var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>(); an.runtimeAnimatorController = ctl; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            root.transform.localScale = Vector3.one * scale;
            var rs = go.GetComponentsInChildren<Renderer>(true); if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); height = b.size.y; }
            Log($"GC RIDER (R22): {name} from {path}, scale {scale:0.00}, standing height {height:0.00} m");
            return root;
        }

        /// <summary>QF37: the Scree racer = the Chapter 1 Scree model (Humanoid, its own shell / glow materials as imported) with the
        /// riders' animator; ScreeRig keeps the lower arms, the tucked abdomen and the antennae alive.</summary>
        static GameObject GcScreeRider(Transform parent, AnimatorController ctl, out float height)
        {
            height = 0f;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ScreeFbx);
            if (model == null) { Log("GC RIDER (QF37): the Scree model is missing (" + ScreeFbx + ") - ScreeTpl not built"); return null; }
            var root = new GameObject("ScreeTpl"); root.transform.SetParent(parent, false);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model); go.name = "scree"; go.transform.SetParent(root.transform, false);
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.updateWhenOffscreen = true;
            var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>(); an.runtimeAnimatorController = ctl; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            go.AddComponent<Seven.ScreeRig>();
            var rs = go.GetComponentsInChildren<Renderer>(true); if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); height = b.size.y; }
            Log($"GC RIDER (QF37): ScreeTpl from {ScreeFbx}, standing height {height:0.00} m, materials {string.Join(", ", go.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(m => m.sharedMaterials).Where(m => m != null).Select(m => m.name).Distinct())}");
            return root;
        }

        /// <summary>QF37: Ratwing (the Round 4 model on his rig, the mothership's materials) for the Jetstream's cockpit - wings folded
        /// along his sides (RatwingRig, perched), seated with his claws on the controls (GcRatRider).</summary>
        static GameObject GcRatwingTemplate(Transform parent)
        {
            var rwModel = AssetDatabase.LoadAssetAtPath<GameObject>(RatwingFbx);
            var ratCtl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "/Ratwing.controller");
            if (rwModel == null || ratCtl == null) { ratCtl = SetupRatwing(out _, out _); rwModel = AssetDatabase.LoadAssetAtPath<GameObject>(RatwingFbx); }
            if (rwModel == null) { Log("GC RIDER (QF37): Ratwing's model is missing - RatwingTpl not built"); return null; }
            var root = new GameObject("RatwingTpl"); root.transform.SetParent(parent, false);
            var body = new GameObject("Body").transform; body.SetParent(root.transform, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(rwModel); model.name = "RatwingModel"; model.transform.SetParent(body, false);
            var tf = model.GetComponentsInChildren<Transform>(); Transform Bn(string n) => tf.FirstOrDefault(x => x.name == n);
            var head = Bn("head"); var eyesB = Bn("eyes");
            if (head != null && eyesB != null) { var fwd = root.transform.InverseTransformDirection(eyesB.position - head.position); fwd.y = 0f; float yaw = Vector3.SignedAngle(fwd, Vector3.forward, Vector3.up); model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * model.transform.localRotation; }
            var anim = model.GetComponent<Animator>() ?? model.AddComponent<Animator>(); anim.runtimeAnimatorController = ratCtl; anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var skin = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Ratwing_skin.mat"); var eyeM = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Ratwing_eyes.mat");
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>()) { smr.updateWhenOffscreen = true; bool isEyes = smr.sharedMaterials.Any(mm => mm != null && mm.name.ToLower().Contains("eye")) || smr.name.Contains("eyes"); if (skin != null && eyeM != null) smr.sharedMaterials = smr.sharedMaterials.Select(_ => isEyes ? eyeM : skin).ToArray(); }
            var rr = root.AddComponent<Seven.RatwingRig>(); rr.Anim = anim; rr.Body = null;   // (the seat places his body, not the flight offset)
            root.AddComponent<Seven.GcRatRider>().Model = body;
            root.transform.localScale = Vector3.one * RatwingScale;
            Log($"GC RIDER (QF37): RatwingTpl (the Round 4 model, {(skin != null ? "the mothership's skin" : "imported materials")}), scale {RatwingScale:0.00}");
            return root;
        }
        const float RatwingScale = 0.72f;   // 2.1 m standing -> seated, his head and shoulders over the Jetstream's windscreen
    }
}
