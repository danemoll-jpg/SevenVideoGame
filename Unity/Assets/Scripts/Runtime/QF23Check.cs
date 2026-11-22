using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// QF23 (23, 24) check run: <c>-qf23revisit</c> (with -capture). A Dan-like save from an older build - every chapter
    /// finished, the newer flags missing (no combinedSeen, no asteroidDone / biavehDone / cattalusSeen, no scene-seen marks for
    /// scenes added later) - is built on the test keys; the save migration runs; then, free aboard, the star map sets course
    /// for every finished world (the Moon, Europa, Mars, the asteroid, the mothership) through the console's own travel path.
    /// Each must give the short arrival only (the passage, the window, a "visited" card) - no story scene, no scene change.
    /// Then the records for every finishing order (asteroid first / mothership first / both). Ends
    /// <c>QF23 REVISIT CHECK: PASS</c> or FAIL with what played.
    /// </summary>
    public class QF23Check : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (DemoBoot.CaptureDir == null || !DemoBoot.QF23Revisit || FindAnyObjectByType<QF23Check>() != null) return;
            var go = new GameObject("QF23Check"); DontDestroyOnLoad(go); go.AddComponent<QF23Check>();
        }
        ChapterCapture Cap => ChapterCapture.I;
        void Log(string s) { Debug.Log("[Seven] QF23 check: " + s); Cap?.Log("QF23 check: " + s); }
        static Chapter1Aboard Ab => Chapter1Aboard.I != null && Chapter1Aboard.I.isActiveAndEnabled ? Chapter1Aboard.I : null;

        /// <summary>Called by the menu before the ship loads: the old save.</summary>
        public static void FakeOldSave()
        {
            VenusState.ClearTestSave(); VenusState.ResetForTest(); VenusState.CompleteScene("VEN_C11"); VenusState.CompleteScene("VEN_C12");
            MarsState.ResetForTest(); MarsState.Fake(MarsLayout.CpDone, true);
            EuropaState.ClearTestSave(); EuropaState.ResetForTest(); for (int c = 0; c <= EuropaLayout.CpDone; c++) EuropaState.Reach(c);
            AsteroidState.ClearTestSave(); AsteroidState.ResetForTest(); AsteroidState.Fake(AsteroidLayout.CpDone, true);
            BiavehState.ClearTestSave(); BiavehState.ResetForTest(); BiavehState.Fake(BiavehLayout.CpDone, true);
            WorldFlags.FakeForTest("oldsave");
            Debug.Log("[Seven] QF23 check: the old save - every chapter finished, newer flags missing: " + WorldFlags.Summary());
        }

        IEnumerator Start()
        {
            ChapterCapture.Ensure();
            while (Ab == null || !Ab.Explored || Ab.InGalleyScene || ShipConsole.I == null) yield return null;
            for (float t = 0f; t < 4f; t += Time.deltaTime) yield return null;
            if (Camera.main != null) Cap.Attach(Camera.main);
            Log($"free exploration aboard with the old save; before the migration: combined due {WorldFlags.CombinedDue}, Cattalus due {WorldFlags.CattalusDue}, asteroid scenes seen {(AsteroidState.SceneDone("AST_10_reunion") ? "yes" : "no")}, mothership BIA_12 seen {(BiavehState.SceneDone("BIA_12") ? "yes" : "no")}");
            Log(SaveMigration.Run(true));
            Log($"after the migration: combined due {WorldFlags.CombinedDue} (must be False), Cattalus due {WorldFlags.CattalusDue} (allowed: the one progression beat left), AST_10_reunion seen {AsteroidState.SceneDone("AST_10_reunion")}, BIA_12 seen {BiavehState.SceneDone("BIA_12")}");
            bool ok = !WorldFlags.CombinedDue && AsteroidState.SceneDone("AST_10_reunion") && BiavehState.SceneDone("BIA_12");
            var scene0 = SceneManager.GetActiveScene().name;
            foreach (var id in new[] { "moon", "europa", "mars", "asteroid", "biaveh" })
            {
                int lines0 = DialoguePlayer.LinesStarted; var con = ShipConsole.I;
                Log($"star map: set course for {id} (finished)");
                yield return con.StartCoroutine(con.TravelForCheck(id));
                for (float t = 0f; t < 20f && (con.Busy || ShipConsole.StoryLock || (Ab != null && Ab.InGalleyScene)); t += Time.deltaTime) yield return null;
                int lines = DialoguePlayer.LinesStarted - lines0; bool same = SceneManager.GetActiveScene().name == scene0;
                bool story = ShipConsole.StoryLock || (Ab != null && Ab.InGalleyScene);
                Log($"REVISIT {id}: lines spoken {lines} (must be 0), scene {(same ? "still the ship" : "CHANGED to " + SceneManager.GetActiveScene().name)}, story scene running {story}, ship at '{SaucerLocation.Current}'");
                yield return Cap.Shot($"qf23_revisit_{id}_the_window");
                if (lines > 0 || !same || story) ok = false;
            }
            // the records for every finishing order (the one saved list)
            foreach (var (label, ast, bia) in new[] { ("asteroid finished, mothership not", true, false), ("mothership finished, asteroid not", false, true), ("both finished", true, true) })
            {
                AsteroidState.Fake(ast ? AsteroidLayout.CpDone : 0, true); BiavehState.Fake(bia ? BiavehLayout.CpDone : 0, true);
                if (ast && !AsteroidState.RecordAwarded) AsteroidState.AwardRecord(); if (bia && !BiavehState.RecordAwarded) BiavehState.AwardRecord();
                var names = RecordSet.Names(); int n = RecordSet.Collected().Count;
                bool want = (!ast || names.Contains("cuboctahedron")) && (!bia || names.Contains("octahedron"));
                Log($"RECORDS (QF23 23) with the {label}: {n} - {names} ({(want ? "ok" : "MISSING one")})");
                if (!want) ok = false;
            }
            Cap.Finish($"QF23 REVISIT CHECK: {(ok ? "PASS" : "FAIL")}");
        }
    }
}
