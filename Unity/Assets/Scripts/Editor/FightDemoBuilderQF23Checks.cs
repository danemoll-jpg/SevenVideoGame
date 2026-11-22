using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF23 build checks on every scene (called from QF18SceneChecks):
    ///  - KNEE CHECK (QF23 8): every humanoid NPC's walk clip sampled on its own body over a full cycle - left vs right knee
    ///    flexion (angle between the thigh and shin directions), min / max / range. The build FAILS if the two ranges differ
    ///    by more than 8 deg (the left-knee bug: clip avatars built from the clip's first frame).
    ///  - SAUCER CLEARANCE (QF23 6): nothing intersects the saucer's hull where it is placed (triangle vs the hull box); FAILS.
    /// Reports: Docs/Previews/qf23/knees_&lt;scene&gt;.txt, saucer_&lt;scene&gt;.txt.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        static string QF23Dir { get { var d = Path.Combine(RepoRoot, "Docs", "Previews", "qf23"); Directory.CreateDirectory(d); return d; } }
        public static readonly List<string> KneeTable = new List<string>();

        static void QF23SceneChecks(string scene)
        {
            KneeCheck(scene);
            SaucerClearanceCheck(scene);
        }

        static float KneeAngle(Animator a, bool left)
        {
            var u = a.GetBoneTransform(left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
            var l = a.GetBoneTransform(left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
            var f = a.GetBoneTransform(left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
            if (u == null || l == null || f == null) return float.NaN;
            return Vector3.Angle(l.position - u.position, f.position - l.position);
        }

        static void KneeCheck(string scene)
        {
            var sb = new StringBuilder(); int bad = 0, n = 0;
            var seen = new HashSet<string>();
            var anims = Object.FindObjectsByType<Animator>(FindObjectsInactive.Include)
                .Where(a => a.isHuman && a.avatar != null && a.GetComponentInParent<Seven.ChapterSeven>() == null && a.GetComponentInParent<Seven.SaucerSeven>() == null && a.name != "7").ToList();
            foreach (var a in anims)
            {
                var ac = a.runtimeAnimatorController as AnimatorController; if (ac == null) continue;
                var walk = ac.layers[0].stateMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == "walk");
                var clip = walk != null ? walk.motion as AnimationClip : null; if (clip == null) continue;
                string key = a.avatar.name + "|" + clip.name; if (!seen.Add(key)) continue;
                float lMin = 999f, lMax = -1f, rMin = 999f, rMax = -1f;
                bool wasActive = a.gameObject.activeSelf; a.gameObject.SetActive(true);
                AnimationMode.StartAnimationMode();
                try
                {
                    for (int i = 0; i < 32; i++)
                    {
                        AnimationMode.BeginSampling();
                        AnimationMode.SampleAnimationClip(a.gameObject, clip, clip.length * i / 32f);
                        AnimationMode.EndSampling();
                        float l = KneeAngle(a, true), r = KneeAngle(a, false);
                        if (float.IsNaN(l) || float.IsNaN(r)) continue;
                        lMin = Mathf.Min(lMin, l); lMax = Mathf.Max(lMax, l); rMin = Mathf.Min(rMin, r); rMax = Mathf.Max(rMax, r);
                    }
                }
                finally { AnimationMode.StopAnimationMode(); a.gameObject.SetActive(wasActive); }
                if (lMax < 0f) continue;
                n++;
                float lr = lMax - lMin, rr = rMax - rMin, diff = lr - rr;
                // the same clip on Y Bot (a clean Mixamo T-pose avatar): a walk can be a little uneven by itself - the NPC must
                // match the clip, not be perfectly symmetric
                var (yl, yr) = YBotKnees(clip); float ydiff = yl - yr;
                bool ok = Mathf.Abs(diff - ydiff) <= 8f && lr > 25f && rr > 25f;
                if (!ok) bad++;
                var row = $"| {a.name} | {clip.name} | {lMin:0}-{lMax:0} ({lr:0}) | {rMin:0}-{rMax:0} ({rr:0}) | {diff:+0;-0} | Y Bot L {yl:0} / R {yr:0} ({ydiff:+0;-0}) | {(ok ? "ok" : "FAIL")} |";
                sb.AppendLine(row); KneeTable.Add($"{scene}: {row}");
            }
            var head = $"KNEE CHECK (QF23 8) {scene}: {n} humanoid NPC walks sampled over a full cycle - left vs right knee range differs from the same clip on Y Bot by more than 8 deg (or a knee bends < 25 deg): {bad} (must be 0)";
            File.WriteAllText(Path.Combine(QF23Dir, $"knees_{scene}.txt"), head + "\n\n| NPC | walk clip | left knee min-max (range) deg | right knee min-max (range) deg | L-R | the same clip on Y Bot (L / R range) | |\n|---|---|---|---|---|---|---|\n" + sb);
            Log(head);
            if (bad > 0) BuildFailures.Add(head);
        }

        static (float l, float r) YBotKnees(AnimationClip clip)
        {
            var pre = AssetDatabase.LoadAssetAtPath<GameObject>(BotDir + "/ybot_character.fbx"); if (pre == null) return (0f, 0f);
            var go = (GameObject)Object.Instantiate(pre); var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
            an.avatar = AssetDatabase.LoadAllAssetsAtPath(BotDir + "/ybot_character.fbx").OfType<Avatar>().FirstOrDefault();
            float lMin = 999f, lMax = -1f, rMin = 999f, rMax = -1f;
            AnimationMode.StartAnimationMode();
            try
            {
                for (int i = 0; i < 32; i++)
                {
                    AnimationMode.BeginSampling(); AnimationMode.SampleAnimationClip(go, clip, clip.length * i / 32f); AnimationMode.EndSampling();
                    float l = KneeAngle(an, true), r = KneeAngle(an, false); if (float.IsNaN(l) || float.IsNaN(r)) continue;
                    lMin = Mathf.Min(lMin, l); lMax = Mathf.Max(lMax, l); rMin = Mathf.Min(rMin, r); rMax = Mathf.Max(rMax, r);
                }
            }
            finally { AnimationMode.StopAnimationMode(); Object.DestroyImmediate(go); }
            return lMax < 0f ? (0f, 0f) : (lMax - lMin, rMax - rMin);
        }

        static void SaucerClearanceCheck(string scene)
        {
            var sb = new StringBuilder(); int total = 0;
            foreach (var beam in Object.FindObjectsByType<Seven.SaucerBeam>(FindObjectsInactive.Exclude))
            {
                var ignore = new List<Transform>();
                foreach (var hb in Object.FindObjectsByType<Seven.HoverBike>(FindObjectsInactive.Include)) ignore.Add(hb.transform);
                foreach (var cs in Object.FindObjectsByType<Seven.ChapterSeven>(FindObjectsInactive.Include)) ignore.Add(cs.transform);
                // the one deliberate exception: the Moon prison's IMPOUND clamps grip the captured saucer (the story - they release
                // it in the rescue); every berth / hover spot must hold it without touching
                var imp = GameObject.Find("Impound"); if (imp != null) { ignore.Add(imp.transform); sb.AppendLine("exempt: the Moon prison's impound clamps (the captured saucer is gripped by design)"); }
                var hits = Seven.SaucerClearance.Intersecting(beam.transform, ignore);
                total += hits.Count;
                sb.AppendLine($"saucer at {beam.transform.position}: {hits.Count} {(hits.Count > 0 ? "- " + string.Join(", ", hits) : "")}");
            }
            var head = $"SAUCER CLEARANCE (QF23 6) {scene}: {total} meshes intersect the saucer's hull where it is placed (must be 0)";
            File.WriteAllText(Path.Combine(QF23Dir, $"saucer_{scene}.txt"), head + "\n" + sb);
            Log(head);
            if (total > 0) BuildFailures.Add(head);
        }
    }
}
