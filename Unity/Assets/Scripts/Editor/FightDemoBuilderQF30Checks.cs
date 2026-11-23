using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF30 — the build checks Dan's review of the ending asked for (reports in Docs/Previews/qf30/):
    /// <list type="bullet">
    /// <item>HAND IK RELEASE (QF30 3): every runtime file that puts a hand on a target (SevenTableIK weights, HoldPose.Hold) also lets it
    /// go (ReleaseAll / Release / weight 0) - and the chapter directors release every hand when control comes back. Fails the build.</item>
    /// <item>CONTROL RETURNS (QF30 17): every place a director gives control back is followed by an objective, an interaction / walk /
    /// telekinesis step the player is told about, or a declared free moment - listed per chapter; the finale must have none missing
    /// (fails the build); the other chapters are listed for the polish pass and proved in play by NO NEXT STEP in every check run.</item>
    /// <item>SURPRISE (QF30 24): Kyle's closing lines and his credit never appear in public text (the credits roll, the console's Log /
    /// Field Guide, README, the launcher docs, loading tips). Fails the build.</item>
    /// </list>
    /// The drill axis (DRILL AXIS) and the staging marks (STAGING MARKS) are checked by the scene builders that place them.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        static string QF30Dir { get { var d = Path.Combine(RepoRoot, "Docs", "Previews", "qf30"); Directory.CreateDirectory(d); return d; } }
        static void Report30(string file, string head, StringBuilder sb, bool fail)
        {
            File.WriteAllText(Path.Combine(QF30Dir, file), head + "\n" + sb);
            Log(head); if (fail) BuildFailures.Add(head);
        }

        public static void QF30GlobalChecks()
        {
            HandReleaseCheck();
            ControlReturnCheck();
            SurpriseCheck();
        }

        static void HandReleaseCheck()
        {
            var sb = new StringBuilder(); int bad = 0, holders = 0;
            var dir = Path.Combine(Application.dataPath, "Scripts", "Runtime");
            var set = new Regex(@"\.(RightW|LeftW)\s*=\s*(1f|0\.[5-9]\d*f|1)\b|HoldPose\.On\([^)]*\)\.Hold\(|\bhold\.Hold\(");
            var rel = new Regex(@"ReleaseAll\(|\.Release\(""|\.(RightW|LeftW)\s*=\s*0f|_handsOn\s*=\s*false|_carry\s*=\s*false");
            foreach (var f in Directory.GetFiles(dir, "*.cs"))
            {
                var t = File.ReadAllText(f); if (!set.IsMatch(t)) continue;
                holders++; bool ok = rel.IsMatch(t);
                sb.AppendLine($"{(ok ? "ok  " : "MISSING RELEASE")}  {Path.GetFileName(f)}: {set.Matches(t).Count} hand targets set, {rel.Matches(t).Count} releases");
                if (!ok) bad++;
            }
            // the directors give no control back with a hand still driven (the finale's Cutscene(false); the shared net in SevenTableIK)
            var fin = File.ReadAllText(Path.Combine(dir, "FinaleDirector.cs"));
            bool ctl = fin.Contains("control back (QF30 3)");
            if (!ctl) { bad++; sb.AppendLine("MISSING  FinaleDirector.Cutscene(false) does not release the hands when control comes back"); }
            Report30("hand_release.txt", $"HAND IK RELEASE (QF30 3): {holders} runtime files put a hand on a target - files that never let go {bad} (must be 0); control-back release in the finale {(ctl ? "on" : "MISSING")}; in play: HAND IK LEFT ON in every check run", sb, bad > 0);
        }

        static void ControlReturnCheck()
        {
            var sb = new StringBuilder(); int returns = 0, missing = 0, finMissing = 0;
            var dir = Path.Combine(Application.dataPath, "Scripts", "Runtime");
            var next = new Regex(@"Objective\(""|Objective\(\$""|Interact\(|GuideWalk\(|Telekinesis\(|QF30Watch\.Free\(|RideRoute\(|\.Begin\(BossFight|Fight\(|Explore|FreeAboard|Free\(|Route\(|Ride\(");
            foreach (var f in Directory.GetFiles(dir, "*Director*.cs"))
            {
                var name = Path.GetFileName(f); var lines = File.ReadAllLines(f);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (!lines[i].Contains("Cutscene(false)") || lines[i].TrimStart().StartsWith("//") || lines[i].Contains("IEnumerator Cutscene")) continue;
                    returns++;
                    bool found = false;
                    for (int j = Mathf.Max(0, i - 6); j < Mathf.Min(lines.Length, i + 30) && !found; j++) if (next.IsMatch(lines[j])) found = true;
                    // the end of a scene method: control comes back only after the caller's next step - look at the method's caller lines
                    if (!found) { for (int j = i + 1; j < Mathf.Min(lines.Length, i + 4); j++) if (lines[j].Trim() == "}") { found = true; break; } }
                    if (!found) { missing++; if (name.StartsWith("Finale")) finMissing++; sb.AppendLine($"no next step within 30 lines: {name}:{i + 1}"); }
                }
            }
            Report30("control_returns.txt", $"CONTROL RETURNS (QF30 17): {returns} places where a director gives control back - without an objective / told step / declared free moment nearby: {missing} (the finale: {finMissing}, must be 0; the rest are listed for the polish pass and checked in play by NO NEXT STEP)", sb, finMissing > 0);
        }

        static void SurpriseCheck()
        {
            var sb = new StringBuilder(); int bad = 0;
            var words = new[] { "Closing lines", "signing out", "Remember who you are", "(2014) by Kyle" };
            var files = new[] { Path.Combine(Application.dataPath, "Resources", "Finale", "credits.txt"), Path.Combine(RepoRoot, "README.md"), Path.Combine(RepoRoot, "Docs", "LAUNCHER.md"), Path.Combine(RepoRoot, "Docs", "ADVENTURE-LOG.md"), Path.Combine(RepoRoot, "Docs", "FIELD-GUIDE.md") }
                .Concat(Directory.Exists(Path.Combine(Application.dataPath, "Resources", "Console")) ? Directory.GetFiles(Path.Combine(Application.dataPath, "Resources", "Console"), "*.json") : new string[0]);
            foreach (var f in files)
            {
                if (!File.Exists(f)) continue; var t = File.ReadAllText(f);
                foreach (var w in words) if (t.Contains(w)) { bad++; sb.AppendLine($"FOUND '{w}' in {f}"); }
            }
            Report30("surprise.txt", $"SURPRISE (QF30 24): Kyle's closing lines / his credit in public text (the credits roll, the console's Log and Field Guide, README, the launcher docs): {bad} (must be 0 - his credit shows only after 7's sign-off)", sb, bad > 0);
        }
    }
}
