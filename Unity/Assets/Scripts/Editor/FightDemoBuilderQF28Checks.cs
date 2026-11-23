using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF28 (3): VEHICLE FRONT - every vehicle's travel direction (its root's +Z: the bike's and the racers' movement code drive
    /// transform.forward) must match its model's front, measured from the model itself:
    ///  - a hover bike: the grips ahead of the saddle;
    ///  - a racer (Duchess's, the highlights' ghost): the narrow nose / windscreen end vs the wide end with the two fan pods,
    ///    measured from the mesh's own width along its length (Dan's 0.18.2 racer was imported nose to -Z: it flew backwards).
    /// FAILS the build above 25 deg. Reports Docs/Previews/qf28/vehicle_front_&lt;scene&gt;.txt.
    /// <see cref="MeasureRacer"/> is also what the Cattalus builder uses to turn the racer model round and find its padded bed.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        static string QF28Dir { get { var d = Path.Combine(RepoRoot, "Docs", "Previews", "qf28"); Directory.CreateDirectory(d); return d; } }

        /// <summary>A racer's front (horizontal unit vector, in <paramref name="root"/>'s space) and its padded bed (the top of the
        /// cushion in the middle of the cavity, root space). Slices along the long axis: the nose end is the narrow one.</summary>
        public static bool MeasureRacer(Transform root, out Vector3 front, out Vector3 bed, out string why)
        {
            front = Vector3.forward; bed = new Vector3(0f, 0.36f, 0f); why = "";
            var vs = new List<Vector3>();
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var m = mf.sharedMesh; if (m == null || !m.isReadable) continue;
                var toRoot = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                foreach (var v in m.vertices) vs.Add(toRoot.MultiplyPoint3x4(v));
            }
            if (vs.Count < 50) { why = "no readable mesh"; return false; }
            float x0 = vs.Min(v => v.x), x1 = vs.Max(v => v.x), z0 = vs.Min(v => v.z), z1 = vs.Max(v => v.z);
            bool alongZ = (z1 - z0) >= (x1 - x0);
            float lo = alongZ ? z0 : x0, len = alongZ ? z1 - z0 : x1 - x0;
            const int N = 10;
            var wMin = new float[N]; var wMax = new float[N]; var top = new float[N];
            for (int i = 0; i < N; i++) { wMin[i] = float.MaxValue; wMax[i] = float.MinValue; top[i] = float.MinValue; }
            foreach (var v in vs)
            {
                float a = alongZ ? v.z : v.x, o = alongZ ? v.x : v.z;
                int i = Mathf.Clamp((int)((a - lo) / len * N), 0, N - 1);
                wMin[i] = Mathf.Min(wMin[i], o); wMax[i] = Mathf.Max(wMax[i], o); top[i] = Mathf.Max(top[i], v.y);
            }
            float W(int i) => wMax[i] > wMin[i] ? wMax[i] - wMin[i] : 0f;
            float endLo = (W(0) + W(1)) * 0.5f, endHi = (W(N - 1) + W(N - 2)) * 0.5f;
            float sgn = endHi < endLo ? 1f : -1f;                          // the narrow end leads
            front = alongZ ? new Vector3(0f, 0f, sgn) : new Vector3(sgn, 0f, 0f);
            // the bed: the middle slice with the lowest top (the open cavity), its cushion = the highest vertex in its middle column
            int bi = 3; for (int i = 3; i <= 6; i++) if (top[i] < top[bi]) bi = i;
            float aC = lo + (bi + 0.5f) / N * len, oC = (wMin[bi] + wMax[bi]) * 0.5f, half = W(bi) * 0.12f, y = float.MinValue;
            foreach (var v in vs) { float a = alongZ ? v.z : v.x, o = alongZ ? v.x : v.z; if (Mathf.Abs(a - aC) < len * 0.06f && Mathf.Abs(o - oC) < half) y = Mathf.Max(y, v.y); }
            if (y == float.MinValue) y = top[bi];
            bed = alongZ ? new Vector3(oC, y, aC) : new Vector3(aC, y, oC);
            why = $"length {len:0.00} m along {(alongZ ? "z" : "x")}, end widths {endLo:0.00} / {endHi:0.00} m (the narrow end is the nose), bed slice {bi} cushion top {y:0.00} m";
            return true;
        }

        /// <summary>QF28 (3): every vehicle in the scene - travel direction (+Z of its root) vs its model's measured front.</summary>
        static void VehicleFrontCheck(string scene)
        {
            var sb = new StringBuilder(); int n = 0, bad = 0;
            foreach (var hb in Object.FindObjectsByType<Seven.HoverBike>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (hb.Seat == null || hb.GripL == null || hb.GripR == null) { sb.AppendLine($"  {hb.name}: no seat / grips - not measured"); continue; }
                var f = hb.transform.InverseTransformPoint((hb.GripL.position + hb.GripR.position) * 0.5f) - hb.transform.InverseTransformPoint(hb.Seat.position); f.y = 0f;
                float ang = Vector3.Angle(f, Vector3.forward); n++; if (ang > 25f) bad++;
                sb.AppendLine($"  bike '{hb.name}': grips ahead of the saddle at {ang:0} deg from its travel direction {(ang > 25f ? "<- WRONG WAY" : "ok")}");
            }
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name.EndsWith("Racer") && t.GetComponentInChildren<MeshFilter>(true) != null))
            {
                if (!MeasureRacer(t, out var f, out _, out var why)) { sb.AppendLine($"  racer '{t.name}': {why}"); continue; }
                float ang = Vector3.Angle(f, Vector3.forward); n++; if (ang > 25f) bad++;
                sb.AppendLine($"  racer '{t.name}': nose at {ang:0} deg from its travel direction {(ang > 25f ? "<- WRONG WAY" : "ok")} ({why})");
            }
            if (n == 0) return;
            var head = $"VEHICLE FRONT (QF28 3) {scene}: {n} vehicles, travelling backwards / sideways {bad}";
            File.WriteAllText(Path.Combine(QF28Dir, $"vehicle_front_{scene}.txt"), head + "\n" + sb);
            Log(head); Log(sb.ToString().TrimEnd());
            if (bad > 0) BuildFailures.Add(head);
        }

        /// <summary>QF28 (5): HOLD CHECK - every hand hold on her real rig (her idle pose evaluated in the editor): the sphere in both
        /// hands and in her left palm, the cube / the octahedron in both hands, the dodecahedron in each hand (CarriedShape's grip).
        /// FAILS on a finger bone inside the object (deeper than 3 mm) or a wrist bent past 70 deg.</summary>
        static void HoldCheck(string scene)
        {
            var seven = Object.FindObjectsByType<Seven.ChapterSeven>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            var an = seven != null ? seven.GetComponentInChildren<Animator>(true) : null;
            if (an == null || !an.isHuman) return;
            var ac = an.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            var clip = ac != null ? ac.layers[0].stateMachine.states.Select(x => x.state).FirstOrDefault(x => x.name == "idle")?.motion as AnimationClip : null;
            if (clip == null) { Log($"HOLD CHECK (QF28 5) {scene}: no idle clip - skipped"); return; }
            var bones = an.GetComponentsInChildren<Transform>(true); var saved = bones.Select(b => (b, b.localPosition, b.localRotation, b.localScale)).ToList();
            var root = seven.transform; var rootSaved = (root.position, root.rotation);
            root.SetPositionAndRotation(new Vector3(0f, 500f, 0f), Quaternion.identity);
            var sb = new StringBuilder(); int bad = 0, n = 0;
            var graph = UnityEngine.Playables.PlayableGraph.Create("QF28HoldCheck");
            try
            {
                var outp = UnityEngine.Animations.AnimationPlayableOutput.Create(graph, "out", an);
                var cp = UnityEngine.Animations.AnimationClipPlayable.Create(graph, clip); outp.SetSourcePlayable(cp);
                void Pose() { cp.SetTime(0.5f); graph.Evaluate(0f); }
                var tmp = new GameObject("QF28_HoldCheckShape"); var mf = tmp.AddComponent<MeshFilter>();
                void Check(string what, Mesh m, float sphereR, bool left, bool right, Vector3 at)
                {
                    Pose(); mf.sharedMesh = m; tmp.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 20f, 0f));
                    var surf = sphereR > 0f ? new Seven.HandHold.Surface(tmp.transform, sphereR) : new Seven.HandHold.Surface(tmp.transform, m);
                    Seven.HandHold.Cup(an, root, surf, left, right, 1f);
                    foreach (var r in new[] { false, true })
                    {
                        if (r ? !right : !left) continue;
                        var rep = Seven.HandHold.Measure(an, r, surf); n++;
                        bool ok = rep.Deepest >= Seven.HandHold.InsideMax && rep.Wrist <= Seven.HandHold.WristMax; if (!ok) bad++;
                        sb.AppendLine($"  {what}, {(r ? "right" : "left")} hand: deepest finger point {rep.Deepest * 1000f:0.0} mm ({rep.Where}), fingertips touching {rep.Touching} of 5, wrist bend {rep.Wrist:0} deg {(ok ? "ok" : "<- FAIL")}");
                    }
                }
                var c0 = root.position + Vector3.up * 1.18f;
                Check("the sphere (both hands, CAT_09)", Seven.CattalusSphere.Mesh(Seven.RuneSizes.SphereR), Seven.RuneSizes.SphereR, true, true, c0 + Vector3.forward * 0.34f);
                Check("the sphere (left palm, CAT_09_09)", Seven.CattalusSphere.Mesh(Seven.RuneSizes.SphereR), Seven.RuneSizes.SphereR, true, false, c0 + Vector3.forward * 0.34f + Vector3.left * 0.04f + Vector3.down * 0.02f);
                Check("the cube (both hands)", Seven.RecordSet.Cube(0.42f * 0.866f), 0f, true, true, c0 + Vector3.forward * 0.42f);
                Check("the octahedron (both hands)", Seven.Octahedron.Mesh(Seven.RuneSizes.OctaR), 0f, true, true, c0 + Vector3.forward * 0.42f);
                // the dodecahedron: CarriedShape's grip - against the palm (centre + normal * (0.8 R + 12 mm)), fingers closed to it
                foreach (var r in new[] { true, false })
                {
                    Pose();
                    if (!Seven.HandHold.Palm(an, r, out var pc, out var pn, out var al)) continue;
                    mf.sharedMesh = Seven.ElarimShape.BodyMesh(Seven.MoonLayout.ShapeRadius);
                    tmp.transform.SetPositionAndRotation(pc - pn * 0.012f + pn * (Seven.MoonLayout.ShapeRadius * 0.8f + 0.018f), Quaternion.LookRotation(al, pn) * Quaternion.Euler(18f, 25f, 0f));
                    var surf = new Seven.HandHold.Surface(tmp.transform, mf.sharedMesh);
                    Seven.HandHold.CurlTo(an, r, surf, 1f);
                    var rep = Seven.HandHold.Measure(an, r, surf); n++;
                    bool ok = rep.Deepest >= Seven.HandHold.InsideMax && rep.Wrist <= Seven.HandHold.WristMax; if (!ok) bad++;
                    sb.AppendLine($"  the dodecahedron ({(r ? "right" : "left")} hand, CarriedShape): deepest finger point {rep.Deepest * 1000f:0.0} mm ({rep.Where}), fingertips touching {rep.Touching} of 5, wrist bend {rep.Wrist:0} deg {(ok ? "ok" : "<- FAIL")}");
                }
                Object.DestroyImmediate(tmp);
            }
            finally
            {
                graph.Destroy();
                foreach (var (b, p, q, sc) in saved) { if (b == null) continue; b.localPosition = p; b.localRotation = q; b.localScale = sc; }
                root.SetPositionAndRotation(rootSaved.position, rootSaved.rotation);
            }
            var head = $"HOLD CHECK (QF28 5) {scene}: {n} hand holds on her rig, a finger inside the object or a wrist past {Seven.HandHold.WristMax:0} deg: {bad}";
            File.WriteAllText(Path.Combine(QF28Dir, $"hold_check_{scene}.txt"), head + "\n" + sb);
            Log(head); Log(sb.ToString().TrimEnd());
            if (bad > 0) BuildFailures.Add(head);
        }

        /// <summary>QF26 rule (QF28 pass): a world's fog colour - the tone plus its in-fog glows - stays at or below ~1.0 (the shader
        /// clamps at 1.25). 0.18.2's gate logs showed seven worlds sitting AT the clamp ("fog colour ahead 1.25") in almost every
        /// shot: one bright flat haze, the frames failing for tonal range. Measured over 96 view directions from five camera spots
        /// round the level: the far tone is brought down to 0.8 (zenith 0.85) if above, then the glow strength scaled until the brightest
        /// direction is at most <paramref name="budget"/>. The glows keep their places and sizes (Kyle's soft lights in the fog).</summary>
        static void FogBudget(Seven.Atmosphere a, string world, float budget = 0.98f, float farCap = 0.8f, float contrast = 6f)
        {
            if (a == null) return;
            Log($"FOG BUDGET (QF26 rule) {world}: {a.FitFogBudget(budget, farCap)}");
            if (a.LastBudget > 1.05f) BuildFailures.Add($"FOG BUDGET (QF26 rule) {world}: fog colour {a.LastBudget:0.00} > 1.0");
            // QF28 pass: the world's own post-processing profile - the shared one had contrast -8 (a flat, low-contrast grade: many
            // frames sat at tonal range 0.27-0.29, one mid-grey); the same bloom / vignette / monochrome with more contrast
            var vol = a.GetComponentInChildren<UnityEngine.Rendering.Volume>(true);
            if (vol != null && vol.sharedProfile != null && !float.IsNaN(contrast))
            {
                var path = $"Assets/Atmosphere/Profile_{world.Split(' ')[0]}.asset";
                AssetDatabase.DeleteAsset(path);
                var prof = Object.Instantiate(vol.sharedProfile); prof.name = "Profile_" + world.Split(' ')[0];
                AssetDatabase.CreateAsset(prof, path);
                var comps = prof.components.ToList(); prof.components.Clear();
                foreach (var c in comps) { var cc = Object.Instantiate(c); cc.name = c.name; prof.components.Add(cc); AssetDatabase.AddObjectToAsset(cc, prof); }
                if (prof.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out var ca)) { float c0 = ca.contrast.value; ca.contrast.value = contrast; Log($"GRADE (QF28 pass) {world}: contrast {c0:0} -> {contrast:0} ({path})"); }
                EditorUtility.SetDirty(prof); vol.sharedProfile = prof;
            }
        }
    }
}
