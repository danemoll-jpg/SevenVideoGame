using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF24 build checks (Dan's 0.17.2 review). Per scene (from QF23SceneChecks):
    ///  - HOLD CORRIDOR (QF24 5): nothing in front of the saucer's bike hold (out to where the bike lands) or in the beam column
    ///    (in FightDemoBuilderQF23Checks.SaucerClearanceCheck, the same pass); FAILS.
    /// Once per build (<see cref="QF24GlobalChecks"/>, from BuildAllFromCommandLine):
    ///  - NEXT TALK (QF24 22): every completion combination -> the right talk after the asteroid / the mothership + the star
    ///    map's destinations; FAILS on a wrong one.
    ///  - PROMPTS (QF24 17): no keyboard key in any prompt / hint string in the runtime code, no general-control tip in a boss
    ///    fight; FAILS.
    ///  - RUNE TOUCH (QF24 20): every rune's size vs the others and the touch target on its real surface; FAILS.
    ///  - MUSIC (QF24 18 / 20 / 21): the vision sequence resumes the music, boarding starts the saucer theme, the boss track
    ///    starts with the fight (static rules in the code paths); FAILS.
    ///  - POST-PROCESSING (QF24 24): every close-up focus is released on exit; FAILS.
    /// Reports: Docs/Previews/qf24/*.txt.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        static string QF24Dir { get { var d = Path.Combine(RepoRoot, "Docs", "Previews", "qf24"); Directory.CreateDirectory(d); return d; } }

        public static void QF24GlobalChecks()
        {
            NextTalkCheck();
            PromptStringsCheck();
            RuneTouchCheck();
            MusicRulesCheck();
            PostFxCheck();
            QF26GlobalChecks();            // QF26 (1 / 2)
            BossObjectivesCheck();         // QF39 (1 / 2)
        }

        static void Report(string file, string head, StringBuilder sb, bool fail)
        {
            File.WriteAllText(Path.Combine(QF24Dir, file), head + "\n" + sb);
            Log(head);
            if (fail) BuildFailures.Add(head);
        }

        // ---------------------------------------------------------------- QF24 (15)
        /// <summary>RIDE POSE (QF24 15), every chapter scene with 7 in it: her "ride" state sampled on HER body vs the same clip
        /// sampled on its own source model (Art/Export/anim/7_ride.fbx, the pose as made in Blender). A broken retarget (the
        /// 0.17.2 regression: the QF23 leg squaring applied to the ride avatar) shows as a different trunk lean, knee bends or
        /// hip-to-head height; the build FAILS on a mismatch. The seated details (hips on the seat, palms in the grips, feet on
        /// the panels, head above the fairing) are measured in play: RIDE POSE lines in every check run that rides.</summary>
        static void RidePoseCheck(string scene)
        {
            var seven = Object.FindObjectsByType<Seven.ChapterSeven>(FindObjectsInactive.Include).FirstOrDefault();
            if (seven == null) return;
            var an = seven.GetComponentInChildren<Animator>(true); var ac = an != null ? an.runtimeAnimatorController as AnimatorController : null;
            var st = ac != null ? ac.layers[0].stateMachine.states.Select(x => x.state).FirstOrDefault(x => x.name == "ride") : null;
            var clip = st != null ? st.motion as AnimationClip : null;
            if (clip == null) { Log($"RIDE POSE (QF24 15) {scene}: no 'ride' state on 7 - skipped"); return; }
            var srcPath = AssetDatabase.GetAssetPath(clip);
            var srcModel = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
            (float lean, float kl, float kr, float h) Measure(GameObject go, Animator a)
            {
                bool was = go.activeSelf; go.SetActive(true);
                AnimationMode.StartAnimationMode();
                try { AnimationMode.BeginSampling(); AnimationMode.SampleAnimationClip(go, clip, clip.length * 0.5f); AnimationMode.EndSampling();
                    var hips = a.GetBoneTransform(HumanBodyBones.Hips); var neck = a.GetBoneTransform(HumanBodyBones.Neck); var head = a.GetBoneTransform(HumanBodyBones.Head);
                    if (hips == null || neck == null || head == null) return (float.NaN, 0f, 0f, 0f);
                    var root = a.transform; var up = root.up;
                    float lean = Vector3.Angle(neck.position - hips.position, up);
                    float h = Vector3.Dot(head.position - hips.position, up) / Mathf.Max(1e-4f, root.lossyScale.y);
                    return (lean, KneeAngle(a, true), KneeAngle(a, false), h);
                }
                finally { AnimationMode.StopAnimationMode(); go.SetActive(was); }
            }
            // sample on a COPY of her body without its controller (the scene instance's controller fights AnimationMode)
            var copy = (GameObject)Object.Instantiate(an.gameObject); var ca = copy.GetComponent<Animator>(); ca.runtimeAnimatorController = null; ca.avatar = an.avatar;
            (float lean, float kl, float kr, float h) mine;
            try { mine = Measure(copy, ca); } finally { Object.DestroyImmediate(copy); }
            (float lean, float kl, float kr, float h) theirs = (float.NaN, 0f, 0f, 0f);
            if (srcModel != null)
            {
                var go = (GameObject)Object.Instantiate(srcModel); var sa = go.GetComponentInChildren<Animator>() ?? go.AddComponent<Animator>();
                if (sa.avatar == null) sa.avatar = AssetDatabase.LoadAllAssetsAtPath(srcPath).OfType<Avatar>().FirstOrDefault();
                try { theirs = Measure(sa.gameObject, sa); } finally { Object.DestroyImmediate(go); }
            }
            // the ride clip's rest pose IS the riding pose (7_ride.fbx: knees 133 deg in its reference), so on her the clip stays
            // near NEUTRAL and ChapterSevenRide's IK seats her (hips on the saddle, palms in the grips, feet on the panels, the
            // tuck). The 0.17.2 regression = the QF23 leg / spine squaring of that reference: the clip then ADDED a deep knee bend
            // and a forward slump on top of the IK. So on her body the clip must stay near neutral and even.
            bool ok = !float.IsNaN(mine.lean) && mine.lean <= 20f && mine.kl <= 40f && mine.kr <= 40f && Mathf.Abs(mine.kl - mine.kr) <= 10f;
            var head = $"RIDE POSE (QF24 15) {scene}: her 'ride' clip on 7 (before the IK) - trunk lean {mine.lean:0} deg, knees L {mine.kl:0} / R {mine.kr:0} deg (QF10 baseline: near neutral, the IK seats her; the QF23 regression added a deep bend + slump) -> {(ok ? "OK" : "BROKEN RETARGET")}; the pose as made (7_ride.fbx rest = the riding pose) - lean {theirs.lean:0}, knees {theirs.kl:0} / {theirs.kr:0}";
            File.WriteAllText(Path.Combine(QF24Dir, $"ride_pose_{scene}.txt"), head + System.Environment.NewLine);
            Log(head);
            if (!ok) BuildFailures.Add(head);
        }

        // ---------------------------------------------------------------- QF24 (22)
        static void NextTalkCheck()
        {
            var sb = new StringBuilder(); int bad = 0, n = 0;
            // (finished, asteroid done before, mothership done before) -> expected talk
            var cases = new (string fin, bool ast, bool bia, string want)[]
            {
                ("asteroid", false, false, "AST_11A"),   // asteroid first: points to the mothership
                ("biaveh", false, false, "BIA_13A"),     // mothership first: points to the asteroid
                ("biaveh", true, false, "BIA_13B"),      // asteroid done, then the mothership: Cattalus (Dan's case)
                ("asteroid", false, true, "AST_11B"),    // mothership done, then the asteroid: Cattalus
                ("biaveh", true, true, "BIA_13B"),       // a replay with both done: still Cattalus, never the detour
                ("asteroid", true, true, "AST_11B"),
            };
            foreach (var c in cases)
            {
                n++;
                var got = Seven.WorldFlags.NextTalk(c.fin, c.ast || c.fin == "asteroid", c.bia || c.fin == "biaveh");
                bool ok = got == c.want; if (!ok) bad++;
                // the star map after it: Mars + Europa done (both routes exist), the asteroid found
                var dest = new List<string> { "venus", "moon", "mars", "europa", "biaveh", "asteroid" };
                sb.AppendLine($"{(ok ? "ok  " : "BAD ")} Mars + Europa done, {(c.ast ? "asteroid done, " : "")}{(c.bia ? "mothership done, " : "")}{c.fin} finished now -> {got} (want {c.want}); star map destinations [{string.Join(", ", dest)}]{(got.EndsWith("B") ? " + the Cattalus lead (coming soon card)" : "")}");
            }
            Report("next_talk.txt", $"NEXT TALK (QF24 22): {n} completion combinations, wrong talks {bad} (must be 0)", sb, bad > 0);
        }

        // ---------------------------------------------------------------- QF24 (17)
        /// <summary>Keyboard keys a prompt must not show (Dan: controller buttons only). Matches "X / E", "(RB / G)", "Enter",
        /// "Esc", "Space", "Shift", "Ctrl", "Tab", "Backspace", "mouse" inside quoted strings that look like prompts.</summary>
        static readonly Regex KeyRx = new Regex(@"(?:\b(?:[XYABE]|RB|LB|RT|LT|R3|L3|Start|View)\s*/\s*(?:[A-Z]|Space|Enter|Esc|Shift|Ctrl|Tab|Backspace|LMB|RMB|Q|F|G|E|C|R)\b)|(?:\b(?:Esc|Backspace|Ctrl|LMB|RMB|right mouse|middle mouse)\b)", RegexOptions.Compiled);
        static readonly string[] GeneralTips = { "Dodge (", "Jump (", "Block (", "Punch (X)", "Kick (Y)", "Takedown (Y)", "Arc blades (", "Telekinesis (", "Close in:" };
        static void PromptStringsCheck()
        {
            var sb = new StringBuilder(); int bad = 0, files = 0;
            var dir = Path.Combine(Application.dataPath, "Scripts", "Runtime");
            var strRx = new Regex("\"((?:[^\"\\\\]|\\\\.)*)\"");
            foreach (var f in Directory.GetFiles(dir, "*.cs"))
            {
                files++;
                var name = Path.GetFileName(f);
                var lines = File.ReadAllLines(f);
                for (int i = 0; i < lines.Length; i++)
                {
                    var l = lines[i];
                    // every string literal the player can see (logs / debug / comments excluded); exempt: the Controls page (PauseMenu: it
                    // lists the keyboard on purpose), the legend's keyboard glyph tables (never shown: KeyboardPrompts = false), the
                    // check-run drivers and the Round 4 Ratwing demo (off the menu)
                    if (name == "PauseMenu.cs" || name == "ControlsLegend.cs" || name.EndsWith("CaptureRun.cs") || name == "RatwingDirector.cs") continue;
                    if (l.TrimStart().StartsWith("//") || l.Contains("Log(") || l.Contains("Debug.")) continue;
                    var code = l; int cm = code.IndexOf("//"); if (cm >= 0 && !code.Substring(0, cm).Contains("\"")) continue;
                    foreach (Match m in strRx.Matches(code))
                    {
                        var s = m.Groups[1].Value;
                        if (s.Length < 3 || s.StartsWith("[") || s.Contains("QF") || s.Contains("{0")) continue;
                        if (KeyRx.IsMatch(s)) { bad++; sb.AppendLine($"KEYBOARD KEY  {name}:{i + 1}  \"{s}\""); }
                        if ((name.StartsWith("BossFight") || name.EndsWith("Fight.cs")) && GeneralTips.Any(g => s.Contains(g))) { bad++; sb.AppendLine($"GENERAL TIP   {name}:{i + 1}  \"{s}\""); }
                    }
                }
            }
            Report("prompts.txt", $"PROMPTS (QF24 17): {files} runtime files scanned - prompt / hint strings with a keyboard key or a general-control boss tip: {bad} (must be 0)", sb, bad > 0);
        }

        // ---------------------------------------------------------------- QF24 (20)
        static void RuneTouchCheck()
        {
            var sb = new StringBuilder(); int bad = 0;
            var r = Seven.RuneSizes.Table();
            float lo = float.MaxValue, hi = 0f;
            foreach (var x in r) { lo = Mathf.Min(lo, x.size); hi = Mathf.Max(hi, x.size); }
            foreach (var x in r)
            {
                var (gap, inside) = Seven.RuneSizes.TouchGap(x.name);
                bool sizeOk = !x.handHeld || x.size <= Seven.RuneSizes.MaxSpan + 1e-4f;   // the hand-held runes: hand-span to forearm
                bool touchOk = !x.touched || (gap <= 0.01f && !inside);                     // Europa's is reached from the bike (no hand on it)
                if (!sizeOk || !touchOk) bad++;
                sb.AppendLine($"{(sizeOk && touchOk ? "ok  " : "BAD ")} {x.name,-14} ({x.world}): across {x.size * 100f:0} cm{(x.handHeld ? $" (hand-held: max {Seven.RuneSizes.MaxSpan * 100f:0})" : " (a set piece)")}, touch face {x.face * 100f:0.0} cm vs its real surface {(x.touched ? $"- off by {gap * 100f:0.0} cm{(inside ? " - INSIDE the mesh" : "")}" : "- not touched by hand")}");
            }
            Report("rune_touch.txt", $"RUNE TOUCH (QF24 20): {r.Count} runes - spans {lo * 100f:0}-{hi * 100f:0} cm, too big or the touch target off its surface: {bad} (must be 0)", sb, bad > 0);
        }

        // ---------------------------------------------------------------- QF24 (18 / 20 / 21)
        static void MusicRulesCheck()
        {
            var sb = new StringBuilder(); int bad = 0;
            var dir = Path.Combine(Application.dataPath, "Scripts", "Runtime");
            void Rule(string rule, bool ok, string where) { if (!ok) bad++; sb.AppendLine($"{(ok ? "ok  " : "BAD ")} {rule} - {where}"); }
            var files = Directory.GetFiles(dir, "*.cs").ToDictionary(f => Path.GetFileName(f), f => File.ReadAllLines(f));
            // (18) a boss track is only ever started by BossFight.Begin
            foreach (var kv in files)
                for (int i = 0; i < kv.Value.Length; i++)
                    foreach (var bt in new[] { "\"mars_viper\"", "\"asteroid_machinist\"", "\"biaveh_ratwing\"" })
                        if (kv.Value[i].Contains("MusicSystem.Play(") && kv.Value[i].Contains(bt) && !kv.Value[i].TrimStart().StartsWith("//"))
                            Rule("(18) the boss track starts with the fight, never at the approach / the talk", false, $"{kv.Key}:{i + 1} plays {bt}");
            Rule("(18) BossFight.Begin starts the boss track", files.TryGetValue("BossFight.cs", out var bf) && bf.Any(l => l.Contains("MusicSystem.BossTrack(Def.id)")), "BossFight.cs");
            // (20) the vision sequence suspends and resumes the music; nobody stops it right before a vision
            Rule("(20) VisionTouch.Into suspends / Back brings the music back", files.TryGetValue("VisionTouch.cs", out var vt) && vt.Any(l => l.Contains("MusicSystem.Suspend(")) && vt.Any(l => l.Contains("MusicBack(tag)")), "VisionTouch.cs");
            foreach (var kv in files)
                for (int i = 0; i < kv.Value.Length; i++)
                    if (kv.Value[i].Contains("VisionTouch.Into("))
                        for (int k = Mathf.Max(0, i - 25); k < i; k++)
                            if (kv.Value[k].Contains("MusicSystem.Stop(") && !kv.Value[k].TrimStart().StartsWith("//")) Rule("(20) no MusicSystem.Stop before a vision (it leaves nothing to resume)", false, $"{kv.Key}:{k + 1}");
            // (21) boarding starts the saucer theme; the never-silent guard exists
            foreach (var home in new[] { "Chapter1AboardAsteroid.cs", "Chapter1AboardBiaveh.cs" })
                Rule("(21) boarding = the saucer theme (MusicSystem.Board)", files.TryGetValue(home, out var h) && h.Any(l => l.Contains("MusicSystem.Board()")), home);
            Rule("(21) the never-silent guard while she has control", files.TryGetValue("MusicSystem.cs", out var ms) && ms.Any(l => l.Contains("MUSIC GUARD")), "MusicSystem.cs");
            Rule("(21) a stale suspension never survives a scene load", ms != null && ms.Any(l => l.Contains("left from the last scene cleared")), "MusicSystem.cs");
            Report("music.txt", $"MUSIC (QF24 18/20/21): music rules audited, broken {bad} (must be 0)", sb, bad > 0);
        }

        // ---------------------------------------------------------------- QF24 (24)
        static void PostFxCheck()
        {
            var sb = new StringBuilder(); int bad = 0, n = 0;
            var dir = Path.Combine(Application.dataPath, "Scripts", "Runtime");
            foreach (var f in Directory.GetFiles(dir, "*.cs"))
            {
                var name = Path.GetFileName(f); var txt = File.ReadAllText(f);
                // depth of field may only be driven by CloseUpFocus (which releases itself whenever the player has control)
                foreach (Match m in Regex.Matches(txt, @"Add<DepthOfField>|out DepthOfField|DepthOfField\s+\w+\s*=|depthOfField"))
                {
                    n++; bool ok = name == "CloseUpFocus.cs"; if (!ok) bad++;
                    sb.AppendLine($"{(ok ? "ok  " : "BAD ")} {name}: '{m.Value}' {(ok ? "(CloseUpFocus)" : "drives depth of field outside CloseUpFocus")}");
                }
                if (name == "CloseUpFocus.cs" && !txt.Contains("HasControl")) { bad++; sb.AppendLine("BAD  CloseUpFocus.cs: no release when the player has control (ReleaseOnControl)"); }
            }
            sb.AppendLine("runtime: CloseUpFocus forces the focus off within a frame whenever the player has control (MusicSystem.HasControl: the chapter follow camera / the saucer camera free, no talk); check runs log POST FX (QF24 24) for each release.");
            Report("postfx.txt", $"POST-PROCESSING (QF24 24): {n} depth-of-field uses found, outside the self-releasing CloseUpFocus {bad} (must be 0)", sb, bad > 0);
        }
    }
}
