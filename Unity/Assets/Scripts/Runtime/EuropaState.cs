using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 14 — Chapter "Europa — Beneath the Ice": progression and persistence (spec Docs/CHAPTER_EUROPA.md, progression
    /// notes). A checkpoint (EuropaLayout.Cp*) that only moves forward, the one-time scenes done, the race (optional: entered,
    /// best result - never required), the record awarded exactly once (<see cref="AwardRecord"/>), workers clear, devices
    /// placed. Stored as JSON in PlayerPrefs ("seven_europa"; Scene Select plays on "seven_europa_select"; check runs on
    /// "seven_test_europa*" and remove them again). Continue resumes at the checkpoint (aboard after the temple).
    /// </summary>
    public static class EuropaState
    {
        [System.Serializable]
        class Data
        {
            public int cp = -1;                                       // -1 = Europa not started
            public List<string> done = new List<string>();
            public bool recordAwarded; public int recordAwards;
            public bool raceEntered; public int raceBest = -1;        // finishing place (1 = won), -1 = never finished
            public float raceBestTime = -1f;                           // QF14 (4): her best finishing time (s), -1 = none
            public List<string> devices = new List<string>();
        }
        static Data _d;
        static bool Test => DemoBoot.CaptureDir != null;
        public static bool SceneSelectRun { get; private set; }
        static string Key => Test ? (SceneSelectRun ? "seven_test_europa_select" : "seven_test_europa") : SceneSelectRun ? "seven_europa_select" : "seven_europa";
        static Data D { get { if (_d == null) Load(); return _d; } }
        public static void Load() { var j = PlayerPrefs.GetString(Key, ""); _d = string.IsNullOrEmpty(j) ? new Data() : JsonUtility.FromJson<Data>(j) ?? new Data(); }
        static void Save() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(_d)); PlayerPrefs.Save(); }
        public static void UseOwnSave() { if (SceneSelectRun) { SceneSelectRun = false; _d = null; } }
        public static void ResetForTest() { if (!Test) return; PlayerPrefs.DeleteKey(Key); _d = new Data(); }
        public static void ClearTestSave() { if (Test) { PlayerPrefs.DeleteKey("seven_test_europa"); PlayerPrefs.DeleteKey("seven_test_europa_select"); } }
        public static void ClearOwnSave() { UseOwnSave(); PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); _d = new Data(); }
        /// <summary>QF14 (the chapter list): the player's own save read WITHOUT switching slots (a running Scene Select
        /// session keeps its slot).</summary>
        static Data PeekOwn()
        {
            if (!SceneSelectRun && _d != null) return _d;
            var j = PlayerPrefs.GetString(Test ? "seven_test_europa" : "seven_europa", "");
            return string.IsNullOrEmpty(j) ? new Data() : JsonUtility.FromJson<Data>(j) ?? new Data();
        }
        public static bool OwnReached => PeekOwn().cp >= 0;
        public static bool OwnComplete => PeekOwn().cp >= EuropaLayout.CpDone;   // QF24 (2): a completed chapter counts as its record
        public static bool OwnHasProgress { get { var d = PeekOwn(); return d.cp > 0 || (d.done != null && d.done.Count > 0); } }

        public static bool Reached => D.cp >= 0;
        public static int Checkpoint => Mathf.Max(0, D.cp);
        public static bool Complete => D.cp >= EuropaLayout.CpDone;
        public static bool HasProgress { get { UseOwnSave(); return D.cp > 0 || D.done.Count > 0; } }
        /// <summary>Forward only: a replayed or skipped section never takes the save back.</summary>
        public static bool Reach(int cp) { if (D.cp >= cp) return false; D.cp = cp; Save(); Debug.Log($"[Seven] Europa checkpoint -> {cp} {EuropaLayout.CpNames[Mathf.Clamp(cp, 0, EuropaLayout.CpNames.Length - 1)]}"); return true; }
        public static bool SceneDone(string id) => D.done.Contains(id);
        public static bool CompleteScene(string id) { if (D.done.Contains(id)) return false; D.done.Add(id); Save(); return true; }
        public static bool RecordAwarded => D.recordAwarded;
        /// <summary>QF24 (2): the record on the PLAYER'S OWN save (what she has really collected - shown in every scene, Scene Select included).</summary>
        public static bool OwnRecordAwarded => PeekOwn().recordAwarded;
        public static int RecordAwards => D.recordAwards;
        public static bool AwardRecord() { if (D.recordAwarded) return false; D.recordAwarded = true; D.recordAwards++; Save(); return true; }
        public static bool RaceEntered => D.raceEntered;
        public static int RaceBest => D.raceBest;
        public static float RaceBestTime => D.raceBestTime;
        /// <summary>QF14 (4): a finished race's time; true when it is a new best.</summary>
        public static bool RaceTime(float s) { if (s <= 0f || (D.raceBestTime > 0f && s >= D.raceBestTime)) return false; D.raceBestTime = s; Save(); return true; }
        public static void RaceResult(int place) { D.raceEntered = true; if (place > 0 && (D.raceBest < 0 || place < D.raceBest)) D.raceBest = place; Save(); }
        public static IReadOnlyList<string> Devices => D.devices;
        public static bool HasDevice(string id) => D.devices.Contains(id);
        public static bool PlaceDevice(string id) { if (D.devices.Contains(id)) return false; D.devices.Add(id); Save(); return true; }

        /// <summary>Set when the Saucer scene should play Europa's arrival in the pilot's chair (section 1) before the dive.</summary>
        public static bool ArrivalAboard;
        /// <summary>Set when the Saucer scene should play the reunion + debrief (sections 16-17).</summary>
        public static bool HomeAboard;

        /// <summary>The star map / VEN_C13: start the chapter fresh (the chair scene aboard, then the dive).</summary>
        public static void StartFresh()
        {
            UseOwnSave(); PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); _d = new Data { cp = 0 }; Save();
            Debug.Log("[Seven] Europa started fresh");
            ArrivalAboard = true; Chapter1State.Aboard = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer");
        }
        /// <summary>Main menu Continue: where the save left off (aboard for the reunion / debrief once the temple is done).</summary>
        public static void ContinueOwn()
        {
            UseOwnSave();
            Debug.Log("[Seven] continue Europa: " + Summary());
            if (D.cp >= EuropaLayout.CpHome) { HomeAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); }
            else UnityEngine.SceneManagement.SceneManager.LoadScene("Europa");
        }

        // ---- Scene Select: each entry with the state a real playthrough has there, on its own slot
        public static readonly (string label, int cp)[] SelectEntries =
        {
            ("Europa: arrival (the pilot's chair + the dive)", EuropaLayout.CpArrival), ("Europa: the settlement + the keeper", EuropaLayout.CpSettlement),
            ("Europa: Tunneler's base", EuropaLayout.CpBase), ("Europa: the escape", EuropaLayout.CpEscape),
            ("Europa: the temple + the vision", EuropaLayout.CpTemple), ("Europa: the debrief aboard", EuropaLayout.CpHome),
        };
        public static void StartAt(int cp)
        {
            SceneSelectRun = true; _d = new Data { cp = cp };
            if (cp > EuropaLayout.CpArrival) _d.done.AddRange(new[] { "EUR_01", "EUR_02" });
            if (cp > EuropaLayout.CpDescent) _d.done.Add("EUR_03");
            if (cp > EuropaLayout.CpSettlement) _d.done.AddRange(new[] { "EUR_04", "EUR_05" });
            if (cp > EuropaLayout.CpApproach) _d.done.AddRange(new[] { "EUR_06", "EUR_07" });
            if (cp > EuropaLayout.CpBase) _d.done.Add("EUR_08");
            if (cp > EuropaLayout.CpWorkers) { _d.done.AddRange(new[] { "EUR_09", "EUR_10" }); _d.devices.AddRange(new[] { "dev_0", "dev_1", "dev_2" }); }
            if (cp > EuropaLayout.CpEscape) _d.done.AddRange(new[] { "EUR_11", "EUR_12", "EUR_13" });
            if (cp > EuropaLayout.CpTemple) { _d.done.AddRange(new[] { "EUR_14", "EUR_15" }); _d.recordAwarded = true; _d.recordAwards = 1; }
            Save();
            Debug.Log($"[Seven] scene select: {EuropaLayout.CpNames[cp]} - {Summary()}");
            if (cp == EuropaLayout.CpArrival) { ArrivalAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); return; }
            if (cp >= EuropaLayout.CpHome) { HomeAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); return; }
            UnityEngine.SceneManagement.SceneManager.LoadScene("Europa");
        }

        public static string Summary() => $"Europa state: {(Reached ? EuropaLayout.CpNames[Mathf.Clamp(D.cp, 0, EuropaLayout.CpNames.Length - 1)] : "not started")} (cp {D.cp}), scenes [{string.Join(", ", D.done)}], devices [{string.Join(", ", D.devices)}], race {(D.raceEntered ? "best " + D.raceBest : "not entered")}, record {(D.recordAwarded ? "awarded" : "not yet")} (awards {D.recordAwards})";
    }

    /// <summary>Round 14: Europa's dialogue (Resources/Europa/europa_dialogue.json, from Tools/Voice/make_europa_lines.py) and
    /// its voices (Resources/EuropaVoice/&lt;id&gt;.mp3, Tools/Voice/generate_europa_voices.py).</summary>
    public static class EuropaDialogue
    {
        [System.Serializable] public class Line { public string id, scene, speaker, label, text, direction, voice; }
        [System.Serializable] class File { public List<Line> lines; }
        static File _f;
        static File F { get { if (_f == null) { var ta = Resources.Load<TextAsset>("Europa/europa_dialogue"); _f = ta != null ? JsonUtility.FromJson<File>(ta.text) : new File { lines = new List<Line>() }; } return _f; } }
        public static IReadOnlyList<Line> All => F.lines;
        public static List<VoiceBank.Line> Scene(string scene, int from = 1, int to = 99)
        {
            var l = new List<VoiceBank.Line>();
            foreach (var x in F.lines)
            {
                if (x.scene != scene) continue;
                int n = int.Parse(x.id.Substring(x.id.LastIndexOf('_') + 1));
                if (n < from || n > to) continue;
                l.Add(new VoiceBank.Line { Id = x.id, Speaker = x.speaker, Text = x.text, Scene = 300, PauseAfter = 0.45f, Clip = Resources.Load<AudioClip>("EuropaVoice/" + x.id), Label = x.label });
            }
            return l;
        }
        public static int Count(string scene) => F.lines.FindAll(x => x.scene == scene).Count;
    }
}
