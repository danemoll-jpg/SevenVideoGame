using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF23 (24) - old cutscenes must never pop up out of place on a revisit. A save from an older build can have a chapter
    /// marked COMPLETE while scenes added by later rounds are not marked seen; those scenes could then play after the fact.
    /// Once per session (on the player's own saves, never on Scene Select slots or check runs' test keys unless asked):
    ///  - every chapter that is complete gets EVERY scene of it marked seen (the dialogue data's scene ids + the scene keys the
    ///    code uses: "BIA_09_lines", "AST_10_reunion", ...);
    ///  - the combined-records discovery (MAR_13C) is only "due" while it still leads somewhere: a save that has already
    ///    finished the asteroid or the mothership has moved past it - marked seen.
    /// Progression-critical "due" beats that may still play later (listed in TODO QF23): MAR_13C -> the mothership route
    /// (while neither the asteroid nor the mothership is finished), the Cattalus lead (AST_11B / BIA_13B, at the console).
    /// </summary>
    public static class SaveMigration
    {
        static bool _done;
        public static string LastReport { get; private set; }
        static readonly Dictionary<string, string[]> Extra = new Dictionary<string, string[]>
        {
            { "asteroid", new[] { "AST_02_lines", "AST_03_bird", "AST_03_lines", "AST_04_lines", "AST_05_blades", "AST_05_lines", "AST_07_lines", "AST_09_lines", "AST_10_reunion" } },
            { "biaveh", new[] { "BIA_09_lines" } },
            { "venus", new[] { "VEN_B05_lines" } },
        };
        static List<string> SceneIds(string res)
        {
            var l = new List<string>(); var ta = Resources.Load<TextAsset>(res); if (ta == null) return l;
            foreach (Match m in Regex.Matches(ta.text, "\"scene\"\\s*:\\s*\"([A-Z0-9_]+)\"")) if (!l.Contains(m.Groups[1].Value)) l.Add(m.Groups[1].Value);
            return l;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (DemoBoot.CaptureDir == null) Run(); }
        /// <summary>Run the migration (check runs call it on purpose with -qf23revisit after faking an old save).</summary>
        public static string Run(bool force = false)
        {
            if (_done && !force) return LastReport; _done = true;
            var rep = new List<string>();
            int Mark(string chapter, bool complete, string res, System.Func<string, bool> mark)
            {
                if (!complete) return 0; int n = 0;
                var ids = SceneIds(res); if (Extra.TryGetValue(chapter, out var ex)) ids.AddRange(ex);
                foreach (var id in ids) if (mark(id)) n++;
                if (n > 0) rep.Add($"{chapter}: {n} scenes marked seen");
                return n;
            }
            try
            {
                VenusState.UseOwnSave(); EuropaState.UseOwnSave(); MarsState.UseOwnSave(); AsteroidState.UseOwnSave(); BiavehState.UseOwnSave();
                Mark("venus", WorldFlags.VenusDone, "Venus/venus_dialogue", id => VenusState.CompleteScene(id));
                Mark("europa", EuropaState.Complete, "Europa/europa_dialogue", EuropaState.CompleteScene);
                Mark("mars", MarsState.OwnComplete, "Mars/mars_dialogue", MarsState.CompleteScene);
                Mark("asteroid", AsteroidState.OwnComplete, "Asteroid/asteroid_dialogue", AsteroidState.CompleteScene);
                Mark("biaveh", BiavehState.OwnComplete, "Biaveh/biaveh_dialogue", BiavehState.CompleteScene);
                if (WorldFlags.CombinedDue && (WorldFlags.AsteroidDone || WorldFlags.BiavehDone)) { WorldFlags.MarkCombinedSeen(); rep.Add("the combined-records discovery: past it already (the asteroid / mothership finished) - marked seen"); }
            }
            catch (System.Exception e) { rep.Add("failed: " + e.Message); }
            LastReport = $"SAVE MIGRATION (QF23 24): {(rep.Count == 0 ? "nothing to mark" : string.Join("; ", rep))}; {WorldFlags.Summary()}";
            Debug.Log("[Seven] " + LastReport);
            return LastReport;
        }
    }
}
