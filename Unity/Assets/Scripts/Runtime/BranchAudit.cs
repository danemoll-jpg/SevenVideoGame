using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF38 (8) - every branched conversation against the save's real completion state. Walks EVERY completion order (Mars /
    /// Europa x the asteroid / the mothership, plus a quit during the Cattalus lead so it is due at the console) on the check
    /// runs' test keys through the SAME selectors the scenes call (WorldFlags.MarsDepartureTalk / EuropaEndTalk / NextTalk /
    /// ConsoleDue / CattalusLeadTalk, ShipConsole.Reachable / MoonOutcome) and asserts the talk each moment must play. The table
    /// it checks is in TODO.md (QF38 8). `-branchaudit` = this check alone (seconds); the whole-game run calls it first.
    /// Leaves the test keys cleared afterwards.
    /// </summary>
    public static class BranchAudit
    {
        static readonly List<string> _rows = new List<string>();
        static int _bad, _ok;
        static void Expect(string order, string moment, string got, string want)
        {
            bool ok = got == want; if (ok) _ok++; else _bad++;
            _rows.Add($"{(ok ? "ok  " : "BAD ")} [{order}] {moment}: {got}{(ok ? "" : $" (want {want})")}");
        }
        static void ClearAll()
        {
            VenusState.ClearTestSave(); VenusState.ResetForTest(); EuropaState.ClearTestSave(); EuropaState.ResetForTest();
            MarsState.ClearTestSave(); MarsState.ResetForTest(); AsteroidState.ClearTestSave(); AsteroidState.ResetForTest();
            BiavehState.ClearTestSave(); BiavehState.ResetForTest(); CattalusState.ClearTestSave(); CattalusState.ResetForTest();
            FinaleState.ResetForTest(); WorldFlags.Clear();
        }
        static void Done(string w)
        {
            switch (w)
            {
                case "mars": MarsState.Fake(MarsLayout.CpDone, true); break;
                case "europa": for (int c = 0; c <= EuropaLayout.CpDone; c++) EuropaState.Reach(c); break;
                case "asteroid": AsteroidState.Fake(AsteroidLayout.CpDone, true); WorldFlags.MarkAsteroidDone(); break;
                case "biaveh": BiavehState.Fake(BiavehLayout.CpDone, true); WorldFlags.MarkBiavehDone(); break;
            }
        }
        static string Map() => string.Join(",", ShipConsole.Reachable());

        public static string Run()
        {
            if (DemoBoot.CaptureDir == null) return "BRANCH AUDIT: only in check runs (test keys)";
            _rows.Clear(); _bad = 0; _ok = 0;
            foreach (var first in new[] { "mars", "europa" })
                foreach (var pair in new[] { "asteroid", "biaveh" })
                    foreach (var quitDuringLead in new[] { false, true })
                    {
                        string second = first == "mars" ? "europa" : "mars", pair2 = pair == "asteroid" ? "biaveh" : "asteroid";
                        string order = $"{first}->{second}, {pair}->{pair2}{(quitDuringLead ? ", quit during the lead" : "")}";
                        ClearAll();
                        // Venus: VEN_C12 + C13 offer Mars and Europa
                        VenusState.FakeDoneForTest();
                        Expect(order, "after Venus, the star map", Map(), "venus,moon,mars,europa");
                        Expect(order, "the Moon after Venus", ShipConsole.MoonOutcome().Split(' ')[0], "revisit");
                        // the first of Mars / Europa
                        Done(first);
                        if (first == "mars") Expect(order, "MAR_13 (Mars first)", WorldFlags.MarsDepartureTalk, "MAR_13U");
                        else Expect(order, "the end of Europa (Europa first)", WorldFlags.EuropaEndTalk, "EUR_17");
                        Expect(order, "the console after the first of Mars / Europa", WorldFlags.ConsoleDue ?? "the console", "the console");
                        // the second: the combined records (MAR_13C) there, once
                        Done(second);
                        if (second == "mars") Expect(order, "MAR_13 (Mars second)", WorldFlags.MarsDepartureTalk, "MAR_13C");
                        else Expect(order, "the end of Europa (Europa second)", WorldFlags.EuropaEndTalk, "MAR_13C");
                        WorldFlags.MarkCombinedSeen();
                        Expect(order, "the console after MAR_13C", WorldFlags.ConsoleDue ?? "the console", "the console");
                        Expect(order, "the star map before TRV_AST", Map(), "venus,moon,mars,europa,biaveh");
                        WorldFlags.MarkAsteroid();                                                   // TRV_AST
                        Expect(order, "the star map after TRV_AST", Map(), "venus,moon,mars,europa,biaveh,asteroid");
                        // the first of the asteroid / the mothership
                        Done(pair);
                        Expect(order, $"the end of the {(pair == "asteroid" ? "asteroid" : "mothership")} (first)", WorldFlags.NextTalk(pair), pair == "asteroid" ? "AST_11A" : "BIA_13A");
                        Expect(order, "the console after the first of the pair", WorldFlags.ConsoleDue ?? "the console", "the console");
                        Expect(order, "no Cattalus on the map yet", Map().Contains("cattalus") ? "cattalus shown" : "not shown", "not shown");
                        // the second: the Cattalus lead in the version of the chapter finished last
                        Done(pair2);
                        string lead = pair2 == "asteroid" ? "AST_11B" : "BIA_13B";
                        Expect(order, $"the end of the {(pair2 == "asteroid" ? "asteroid" : "mothership")} (second)", WorldFlags.NextTalk(pair2), lead);
                        if (quitDuringLead)
                            Expect(order, "the lead due at the console (quit before it ended)", WorldFlags.ConsoleDue ?? "the console", lead);
                        WorldFlags.MarkCattalusSeen();
                        Expect(order, "the console after the lead", WorldFlags.ConsoleDue ?? "the console", "the console");
                        Expect(order, "Cattalus on the star map", Map().Contains("cattalus") ? "shown" : "missing", "shown");
                        Expect(order, "the Moon before Cattalus is done", ShipConsole.MoonOutcome().Split(' ')[0], "revisit");
                        // Cattalus done: the Moon = the finale, never the completed-chapter revisit
                        CattalusState.Fake(CattalusLayout.CpDone, true);
                        Expect(order, "the Moon after CAT_12", ShipConsole.MoonOutcome(), "THE FINALE");
                        FinaleState.Fake(FinaleLayout.CpDone, true);
                        Expect(order, "the Moon after the finale", ShipConsole.MoonOutcome().Split(' ')[0], "revisit");
                    }
            ClearAll();
            var sb = new System.Text.StringBuilder();
            foreach (var r in _rows) sb.AppendLine("  " + r);
            var head = $"BRANCH AUDIT (QF38 8): 8 completion orders, {_ok + _bad} branch moments, wrong {_bad} (must be 0)";
            Debug.Log("[Seven] " + head + "\n" + sb);
            return head + "\n" + sb.ToString().TrimEnd();
        }
    }
}
