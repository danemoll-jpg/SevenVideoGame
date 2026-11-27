using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF39 (2) BOSS OBJECTIVES: every boss fight's objective text and counter (<see cref="Seven.BossObjectives"/>) against the
    /// fight's real number of objectives per phase (each fight's ObjectivesPerPhase, the phases in boss_tuning.json, Viper's
    /// pylons in MarsLayout): a number word in the text must be the count, two or more objectives show a counter "n / count",
    /// and no director hard-codes its own objective text or counter beside the table. FAILS the build.
    /// QF39 (1) SWITCH REACH: every boss X objective's prompt uses the one shared reach test (BossFight.NearUse: the prompt at
    /// 3 m, the press accepted to 4.2 m) - never its own distance. FAILS the build.
    /// Report: Docs/Previews/qf39/boss_objectives.txt.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        static void BossObjectivesCheck()
        {
            var dir = Path.Combine(RepoRoot, "Docs", "Previews", "qf39"); Directory.CreateDirectory(dir);
            var sb = new StringBuilder();
            // phases per boss from the tuning table
            var tuning = File.ReadAllText(Path.Combine(Application.dataPath, "Resources", "Bosses", "boss_tuning.json"));
            int PhasesOf(string id)
            {
                var m = Regex.Match(tuning, "\"id\"\\s*:\\s*\"" + id + "\"");
                if (!m.Success) return -1;
                int i = tuning.IndexOf("\"phases\"", m.Index); if (i < 0) return -1;
                int depth = 0, n = 0, k = tuning.IndexOf('[', i);
                for (int j = k; j < tuning.Length; j++)
                {
                    char c = tuning[j];
                    if (c == '[' || c == '{') { depth++; if (c == '{' && depth == 2) n++; }
                    else if (c == ']' || c == '}') { depth--; if (depth == 0) break; }
                }
                return n;
            }
            var bad = Seven.BossObjectives.Check(PhasesOf);
            foreach (var r in Seven.BossObjectives.Rows)
            {
                var real = Seven.BossObjectives.Real(r.boss);
                string shown0 = Seven.BossObjectives.Text(r.boss, r.phase, 0).Replace("\n", " | "), shownAll = Seven.BossObjectives.Text(r.boss, r.phase, r.count).Replace("\n", " | ");
                sb.AppendLine($"{r.boss,-11} phase {r.phase + 1}: objectives {(real != null && r.phase < real.Length ? real[r.phase] : -1)} (table {r.count}) - \"{shown0}\"{(r.counter != null ? $" -> \"{shownAll}\"" : "")}");
            }
            // no director hard-codes its own objective text / counter for a fight
            var rt = Path.Combine(Application.dataPath, "Scripts", "Runtime");
            foreach (var f in Directory.GetFiles(rt, "*Director*.cs"))
            {
                var src = File.ReadAllText(f);
                foreach (Match m in Regex.Matches(src, @"PhaseObjective\s*=(?!\s*null)")) bad.Add($"{Path.GetFileName(f)}: sets PhaseObjective itself (use BossObjectives)");
                foreach (Match m in Regex.Matches(src, @"Objective\(\$?""[^""]*\b(two|three|four)\s+(pylons|devices|partitions|switches|controls)", RegexOptions.IgnoreCase)) bad.Add($"{Path.GetFileName(f)}: its own counted objective text '{m.Value}'");
            }
            // SWITCH REACH (QF39 1): every fight's X objective uses NearUse
            int labels = 0, own = 0;
            foreach (var f in Directory.GetFiles(rt, "*Fight.cs"))
            {
                var src = File.ReadAllText(f);
                int i = src.IndexOf("override string ObjectiveUseLabel()"); if (i < 0) continue;
                labels++;
                int end = src.IndexOf("override void ObjectiveUse()", i); if (end < 0) end = Mathf.Min(src.Length, i + 1200);
                var body = src.Substring(i, end - i);
                bool shared = body.Contains("NearUse(") || body.Contains("InReach()");
                bool ownDist = Regex.IsMatch(body, @"magnitude\s*<\s*[0-9]");
                if (!shared || ownDist) { own++; bad.Add($"{Path.GetFileName(f)}: its switch prompt uses its own distance, not BossFight.NearUse"); }
                sb.AppendLine($"SWITCH REACH {Path.GetFileName(f)}: {(shared && !ownDist ? "the shared reach (prompt 3 m, press 4.2 m)" : "OWN DISTANCE")}");
            }
            if (!File.ReadAllText(Path.Combine(rt, "ViperFight.cs")).Contains("NearUse(")) bad.Add("ViperFight.cs: InReach does not use NearUse");
            foreach (var b in bad) sb.AppendLine("BAD " + b);
            var head = $"BOSS OBJECTIVES (QF39 2): {Seven.BossObjectives.Rows.Length} phase objectives in 5 fights, Viper's pylons {Seven.MarsLayout.Pylons.Length} / required {Seven.ViperFight.Required}; SWITCH REACH (QF39 1): {labels} fights with X switches, own distances {own}; problems {bad.Count} (must be 0)";
            File.WriteAllText(Path.Combine(dir, "boss_objectives.txt"), head + "\n" + sb);
            Log(head);
            if (bad.Count > 0) BuildFailures.Add(head);
        }
    }
}
