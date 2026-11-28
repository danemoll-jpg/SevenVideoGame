namespace Seven
{
    /// <summary>
    /// QF39 (2): every boss fight's objective text and its counter, in ONE table. Each fight shows its phase's text from here
    /// (<see cref="BossFight.Run"/>), with the counter "Pylons n / 3" whenever the phase has two or more objectives; the count is
    /// the fight's own real number of objectives (<c>ObjectivesPerPhase</c> in each fight class). Build check
    /// <c>BOSS OBJECTIVES (QF39 2)</c> fails the build if a text's number ("his three pylons"), its counter or its count
    /// disagree with the fight (QF19 showed "0 / 3" for two pylons, then removed a pylon instead of fixing the count).
    /// </summary>
    public static class BossObjectives
    {
        public struct Row
        {
            public string boss; public int phase; public string text; public string counter; public int count;
            public Row(string boss, int phase, string text, string counter, int count) { this.boss = boss; this.phase = phase; this.text = text; this.counter = counter; this.count = count; }
        }

        public static readonly Row[] Rows =
        {
            new Row("viper", 0, "Shut down his three pylons (X)", "Pylons", 3),
            new Row("viper", 1, "He hunts you - wear him down", null, 0),
            new Row("viper", 2, "Enraged - wear him down until he falls", null, 0),
            new Row("wormmaster", 0, "He defends them from his perch - shut his three devices down, in any order (X at the north device, RT for the ledge device and the crystal)", "Devices", 3),
            new Row("wormmaster", 1, "He comes down - wear him down", null, 0),
            new Row("wormmaster", 2, "His signal fails - wear him down", null, 0),
            new Row("machinist", 0, "He wants to restrain you - wear him down, the glowing couplings are his weak point", null, 0),
            new Row("machinist", 1, "The other arm's couplings - and watch the lane surges", null, 0),
            new Row("machinist", 2, "The override - wear him down until he stops", null, 0),
            new Row("ratwing", 0, "Close the first partition - the lit station", null, 1),
            new Row("ratwing", 1, "His space is smaller - the second partition, the lit station", null, 1),
            new Row("ratwing", 2, "Wear him out", null, 0),
            new Row("tunneler", 0, "Keep him away from the archive", null, 0),
            new Row("tunneler", 1, "Open the old maintenance barrier - its control by the east alcove", null, 1),
            new Row("tunneler", 2, "Restore the connection he cut - the junction box on the west wall", null, 1),
        };

        public static Row? Find(string boss, int phase)
        {
            foreach (var r in Rows) if (r.boss == boss && r.phase == phase) return r;
            return null;
        }

        /// <summary>The objective as shown: the text, and the counter on its own line when the phase counts objectives.</summary>
        public static string Text(string boss, int phase, int done)
        {
            var r = Find(boss, phase);
            if (!r.HasValue) return null;
            var v = r.Value;
            return v.counter != null && v.count >= 2 ? $"{v.text}\n{Counter(v, done)}" : v.text;
        }
        public static string Counter(Row r, int done) => $"{r.counter} {System.Math.Min(done, r.count)} / {r.count}";

        /// <summary>The fight's real number of objectives per phase (from each fight class).</summary>
        public static int[] Real(string boss) =>
            boss == "viper" ? ViperFight.ObjectivesPerPhase : boss == "wormmaster" ? WormmasterFight.ObjectivesPerPhase :
            boss == "machinist" ? MachinistFight.ObjectivesPerPhase : boss == "ratwing" ? RatwingFight.ObjectivesPerPhase :
            boss == "tunneler" ? TunnelerFight.ObjectivesPerPhase : null;

        static readonly string[] Words = { "zero", "one", "two", "three", "four", "five", "six" };
        /// <summary>The build check: every row against its fight; returns the problems (empty = PASS).</summary>
        public static System.Collections.Generic.List<string> Check(System.Func<string, int> phasesOf)
        {
            var bad = new System.Collections.Generic.List<string>();
            foreach (var boss in new[] { "viper", "wormmaster", "machinist", "ratwing", "tunneler" })
            {
                var real = Real(boss); int n = phasesOf != null ? phasesOf(boss) : real.Length;
                if (real == null) { bad.Add($"{boss}: no ObjectivesPerPhase"); continue; }
                if (n != real.Length) bad.Add($"{boss}: {n} phases in boss_tuning.json, ObjectivesPerPhase has {real.Length}");
                for (int ph = 0; ph < real.Length; ph++)
                {
                    var r = Find(boss, ph);
                    if (!r.HasValue) { bad.Add($"{boss} phase {ph + 1}: no objective text"); continue; }
                    var v = r.Value;
                    if (v.count != real[ph]) bad.Add($"{boss} phase {ph + 1}: the table says {v.count} objective(s), the fight has {real[ph]}");
                    if (real[ph] >= 2 && v.counter == null) bad.Add($"{boss} phase {ph + 1}: {real[ph]} objectives but no counter");
                    if (v.counter != null && real[ph] < 2) bad.Add($"{boss} phase {ph + 1}: a counter for {real[ph]} objective(s)");
                    // a number word in the text ("his three pylons") must be the real count
                    var low = " " + v.text.ToLowerInvariant().Replace("-", " ") + " ";
                    for (int w = 2; w < Words.Length; w++)
                        if (low.Contains(" " + Words[w] + " ") && w != real[ph]) bad.Add($"{boss} phase {ph + 1}: the text says '{Words[w]}', the fight has {real[ph]}: \"{v.text}\"");
                }
            }
            if (MarsLayout.Pylons.Length != ViperFight.Required) bad.Add($"viper: {MarsLayout.Pylons.Length} pylons in MarsLayout, {ViperFight.Required} required");
            return bad;
        }
    }
}
