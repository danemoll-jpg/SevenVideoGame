using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 15 — Chapter "Mars — The Divided Kingdoms": progression and persistence (spec Docs/CHAPTER_MARS.md, the
    /// implementation guardrails). A checkpoint (MarsLayout.Cp*) that only moves forward, the one-time scenes done, the record
    /// awarded exactly once, the two new leads (the asteroid world, Biaveh) once the temple display has sent them. Stored as
    /// JSON in PlayerPrefs ("seven_mars"; Scene Select plays on "seven_mars_select"; check runs on "seven_test_mars*").
    /// Continue resumes at the checkpoint (aboard for the debrief + the choice).
    /// The completed-world flags (<see cref="WorldFlags"/>): Venus, Mars, Europa, the asteroid world, Biaveh; Cattalus unlocks
    /// once all five are done, the final Moon mission after the last piece on Cattalus (flags only for now).
    /// </summary>
    public static class MarsState
    {
        [System.Serializable]
        class Data
        {
            public int cp = -1;
            public List<string> done = new List<string>();
            public bool recordAwarded; public int recordAwards;
            public bool route;                    // section 10 (revised): the route that stops, sent to the saucer
        }
        static Data _d;
        static bool Test => DemoBoot.CaptureDir != null;
        public static bool SceneSelectRun { get; private set; }
        static string Key => Test ? (SceneSelectRun ? "seven_test_mars_select" : "seven_test_mars") : SceneSelectRun ? "seven_mars_select" : "seven_mars";
        static Data D { get { if (_d == null) Load(); return _d; } }
        public static void Load() { var j = PlayerPrefs.GetString(Key, ""); _d = string.IsNullOrEmpty(j) ? new Data() : JsonUtility.FromJson<Data>(j) ?? new Data(); }
        static void Save() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(_d)); PlayerPrefs.Save(); }
        public static void UseOwnSave() { if (SceneSelectRun) { SceneSelectRun = false; _d = null; } }
        public static void ResetForTest() { if (!Test) return; PlayerPrefs.DeleteKey(Key); _d = new Data(); }
        public static void ClearTestSave() { if (Test) { PlayerPrefs.DeleteKey("seven_test_mars"); PlayerPrefs.DeleteKey("seven_test_mars_select"); } }
        public static void ClearOwnSave() { UseOwnSave(); PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); _d = new Data(); }
        static Data PeekOwn()
        {
            if (!SceneSelectRun && _d != null) return _d;
            var j = PlayerPrefs.GetString(Test ? "seven_test_mars" : "seven_mars", "");
            return string.IsNullOrEmpty(j) ? new Data() : JsonUtility.FromJson<Data>(j) ?? new Data();
        }
        public static bool OwnReached => PeekOwn().cp >= 0;
        public static bool OwnHasProgress { get { var d = PeekOwn(); return d.cp > 0 || (d.done != null && d.done.Count > 0); } }
        public static bool OwnComplete => PeekOwn().cp >= MarsLayout.CpDone;
        public static bool RouteSent => D.route;

        public static bool Reached => D.cp >= 0;
        public static int Checkpoint => Mathf.Max(0, D.cp);
        public static bool Complete => D.cp >= MarsLayout.CpDone;
        public static bool HasProgress { get { UseOwnSave(); return D.cp > 0 || D.done.Count > 0; } }
        public static bool Reach(int cp) { if (D.cp >= cp) return false; D.cp = cp; Save(); Debug.Log($"[Seven] Mars checkpoint -> {cp} {MarsLayout.CpNames[Mathf.Clamp(cp, 0, MarsLayout.CpNames.Length - 1)]}"); return true; }
        public static bool SceneDone(string id) => D.done.Contains(id);
        public static bool CompleteScene(string id) { if (D.done.Contains(id)) return false; D.done.Add(id); Save(); return true; }
        public static bool RecordAwarded => D.recordAwarded;
        public static int RecordAwards => D.recordAwards;
        public static bool AwardRecord() { if (D.recordAwarded) return false; D.recordAwarded = true; D.recordAwards++; Save(); return true; }
        public static void SendRoute() { if (D.route) return; D.route = true; Save(); }

        /// <summary>Set when the Saucer scene should play Mars's arrival in the pilot's chair (section 1) before the landing.</summary>
        public static bool ArrivalAboard;
        /// <summary>Set when the Saucer scene should play the debrief + the destination choice (sections 12-13).</summary>
        public static bool HomeAboard;

        public static void StartFresh()
        {
            UseOwnSave(); PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); _d = new Data { cp = 0 }; Save();
            Debug.Log("[Seven] Mars started fresh");
            ArrivalAboard = true; Chapter1State.Aboard = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer");
        }
        public static void ContinueOwn()
        {
            UseOwnSave();
            Debug.Log("[Seven] continue Mars: " + Summary());
            if (D.cp <= MarsLayout.CpArrival) { ArrivalAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); }
            else if (D.cp >= MarsLayout.CpHome) { HomeAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); }
            else UnityEngine.SceneManagement.SceneManager.LoadScene("Mars");
        }

        public static readonly (string label, int cp)[] SelectEntries =
        {
            ("Mars: arrival (the pilot's chair + the landing)", MarsLayout.CpArrival), ("Mars: the crossing + Viper", MarsLayout.CpCrossing),
            ("Mars: the maintenance route", MarsLayout.CpMaint), ("Mars: the shared door", MarsLayout.CpSharedDoor),
            ("Mars: the two controls", MarsLayout.CpControls), ("Mars: Viper's intervention", MarsLayout.CpViper),
            ("Mars: the temple + the vision", MarsLayout.CpTemple), ("Mars: the ride back", MarsLayout.CpReturn),
            ("Mars: the debrief + the choice aboard", MarsLayout.CpHome),
        };
        static readonly string[][] DoneBy =
        {
            new[] { "MAR_01" }, new[] { "MAR_02", "MAR_03" }, new[] { "MAR_04" }, new[] { "MAR_05" }, new[] { "MAR_06" },
            new[] { "MAR_07" }, new[] { "MAR_08", "MAR_09", "MAR_10" }, new[] { "MAR_11" },
        };
        /// <summary>The state a real playthrough has when it reaches checkpoint cp.</summary>
        public static void Fake(int cp, bool onOwnSave)
        {
            if (!onOwnSave) SceneSelectRun = true;
            _d = new Data { cp = cp };
            for (int c = 0; c < cp && c < DoneBy.Length; c++) _d.done.AddRange(DoneBy[c]);
            if (cp > MarsLayout.CpTemple) { _d.recordAwarded = true; _d.recordAwards = 1; _d.route = true; }
            Save();
        }
        public static void StartAt(int cp)
        {
            Fake(cp, false);
            Debug.Log($"[Seven] scene select: {MarsLayout.CpNames[cp]} - {Summary()}");
            if (cp == MarsLayout.CpArrival) { ArrivalAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); return; }
            if (cp >= MarsLayout.CpHome) { HomeAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); return; }
            UnityEngine.SceneManagement.SceneManager.LoadScene("Mars");
        }

        public static string Summary() => $"Mars state: {(Reached ? MarsLayout.CpNames[Mathf.Clamp(D.cp, 0, MarsLayout.CpNames.Length - 1)] : "not started")} (cp {D.cp}), scenes [{string.Join(", ", D.done)}], record {(D.recordAwarded ? "awarded" : "not yet")} (awards {D.recordAwards}), route {(D.route ? "sent" : "not yet")}";
    }

    /// <summary>Round 15 (implementation guardrails, revised 2026-11-19): the completed-world flags the departure routing and
    /// later chapters read. Biaveh unlocks only when Mars AND Europa are complete - the combined-record discovery (MAR_13C),
    /// played once, whichever of the two is finished second; the asteroid only after the travel event on the first journey
    /// toward Biaveh (TRV_AST). Cattalus after Venus + Mars + Europa + the asteroid + Biaveh; the final Moon mission after
    /// the last piece on Cattalus (flags only). The two route flags live in their own key ("seven_routes"; check runs
    /// "seven_test_routes"), so they hold whichever chapter set them; a new game clears them.</summary>
    public static class WorldFlags
    {
        [System.Serializable] class R { public bool combined, asteroidFound, biavehDone, asteroidDone, combinedSeen; }
        static string Key => DemoBoot.CaptureDir != null ? "seven_test_routes" : "seven_routes";
        static R Get() { var j = PlayerPrefs.GetString(Key, ""); return string.IsNullOrEmpty(j) ? new R() : JsonUtility.FromJson<R>(j) ?? new R(); }
        static void Put(R r) { PlayerPrefs.SetString(Key, JsonUtility.ToJson(r)); PlayerPrefs.Save(); }
        public static void Clear() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }
        /// <summary>Check runs (-routes): "europa" = Europa already complete (the Mars debrief plays MAR_13C);
        /// "mars" = Mars already complete (the end of Europa plays it); "none" = neither; always on the test keys.</summary>
        public static void FakeForTest(string what)
        {
            if (DemoBoot.CaptureDir == null) return;
            Clear();
            if (what == "europa") { EuropaState.ClearTestSave(); EuropaState.ResetForTest(); for (int c = 0; c <= EuropaLayout.CpDone; c++) EuropaState.Reach(c); }
            if (what == "none") { EuropaState.ClearTestSave(); EuropaState.ResetForTest(); }
            if (what == "mars") { MarsState.ResetForTest(); MarsState.Fake(MarsLayout.CpDone, true); }
            // QF20 (1): Dan's 0.15.4 save - Mars + Europa complete, the old flags set (Biaveh unlocked, asteroid found), never seen
            if (what == "dansave")
            {
                MarsState.ResetForTest(); MarsState.Fake(MarsLayout.CpDone, true);
                EuropaState.ClearTestSave(); EuropaState.ResetForTest(); for (int c = 0; c <= EuropaLayout.CpDone; c++) EuropaState.Reach(c);
                Put(new R { combined = true, asteroidFound = true });
            }
            Debug.Log("[Seven] -routes " + what + ": " + Summary());
        }
        public static bool VenusDone => VenusState.SceneDone("VEN_C12");
        public static bool MarsDone => MarsState.OwnComplete || MarsState.Complete;
        public static bool EuropaDone => EuropaState.Complete;
        /// <summary>MAR_13C has played (in the Mars debrief or at the end of Europa) - once.</summary>
        public static bool CombinedPlayed => Get().combined;
        public static void MarkCombined() { var r = Get(); r.combined = true; Put(r); }
        /// <summary>QF20 (1): the discovery was SEEN to its end (MAR_13C through MAR_13B, set only then). The old flag
        /// ("combined", Biaveh unlocked) was set halfway through the scene and is shared by every save path, so a save could
        /// carry it without the player ever having watched it (Dan's 0.15.4 save) - saves without this flag play it once more.</summary>
        public static bool CombinedSeen => Get().combinedSeen;
        public static void MarkCombinedSeen() { var r = Get(); r.combined = true; r.combinedSeen = true; Put(r); }
        /// <summary>The combined-record discovery is due: both worlds done and it has not been seen yet. QF20: it plays at
        /// the second debrief AND, whenever it is due, the next time she is aboard and uses the console - never missable.</summary>
        public static bool CombinedDue => MarsDone && EuropaDone && !CombinedSeen;
        public static bool BiavehUnlocked => CombinedPlayed;
        public static bool AsteroidFound => Get().asteroidFound;
        public static void MarkAsteroid() { var r = Get(); r.asteroidFound = true; Put(r); }
        public static bool AsteroidDone => Get().asteroidDone;   // the chapter does not exist yet
        public static bool BiavehDone => Get().biavehDone;
        public static bool CattalusUnlocked => VenusDone && MarsDone && EuropaDone && AsteroidDone && BiavehDone;
        public static bool FinalMoonUnlocked => false;
        public static string Summary() => $"world flags: Venus {VenusDone}, Mars {MarsDone}, Europa {EuropaDone}, combined records {(CombinedSeen ? "seen" : CombinedPlayed ? "flag set, never seen to the end" : "not yet")} -> Biaveh {(BiavehUnlocked ? "unlocked" : "locked")}, asteroid {(AsteroidFound ? "found" : "undiscovered")}, Cattalus {(CattalusUnlocked ? "unlocked" : "locked")}, final Moon {(FinalMoonUnlocked ? "unlocked" : "locked")}";
    }
    /// <summary>Round 15: Mars's dialogue (Resources/Mars/mars_dialogue.json, from Tools/Voice/make_mars_lines.py) and its
    /// voices (Resources/MarsVoice/&lt;id&gt;.mp3, Tools/Voice/generate_mars_voices.py).</summary>
    public static class MarsDialogue
    {
        [System.Serializable] public class Line { public string id, scene, speaker, label, text, direction, voice; }
        [System.Serializable] class File { public List<Line> lines; }
        static File _f;
        static File F { get { if (_f == null) { var ta = Resources.Load<TextAsset>("Mars/mars_dialogue"); _f = ta != null ? JsonUtility.FromJson<File>(ta.text) : new File { lines = new List<Line>() }; } return _f; } }
        public static IReadOnlyList<Line> All => F.lines;
        public static List<VoiceBank.Line> Scene(string scene, int from = 1, int to = 99)
        {
            var l = new List<VoiceBank.Line>();
            foreach (var x in F.lines)
            {
                if (x.scene != scene) continue;
                if (!int.TryParse(x.id.Substring(x.id.LastIndexOf('_') + 1), out int n)) continue;   // QF18: named ids (MAR_06_V1) go through Ids()
                if (n < from || n > to) continue;
                l.Add(Make(x));
            }
            return l;
        }
        static VoiceBank.Line Make(Line x) => new VoiceBank.Line { Id = x.id, Speaker = x.speaker, Text = x.text, Scene = 400, PauseAfter = 0.45f, Clip = Resources.Load<AudioClip>("MarsVoice/" + x.id), Label = x.label };
        /// <summary>QF18: lines by their ids, in the order given (the spec's named rows: MAR_06_V1, MAR_06_H4 ...).</summary>
        public static List<VoiceBank.Line> Ids(params string[] ids)
        {
            var l = new List<VoiceBank.Line>();
            foreach (var id in ids) { var x = F.lines.Find(y => y.id == id); if (x != null) l.Add(Make(x)); else Debug.LogWarning("[Seven] Mars line missing: " + id); }
            return l;
        }
        public static int Count(string scene) => F.lines.FindAll(x => x.scene == scene).Count;
    }
}
