using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 18 — Chapter "Cattalus": progression and persistence (a copy of BiavehState). A checkpoint (CattalusLayout.Cp*) that
    /// only moves forward, the one-time scenes, the steps inside a checkpoint, the race result (Won / RunnerUp, kept as it was:
    /// a loss is never rewritten as a win), the sphere + its record awarded exactly once. PlayerPrefs "seven_cattalus" (Scene
    /// Select "seven_cattalus_select"; check runs "seven_test_*").
    /// </summary>
    public static class CattalusState
    {
        public enum Result { None, Won, RunnerUp }
        [System.Serializable]
        class Data
        {
            public int cp = -1;
            public List<string> done = new List<string>();
            public List<string> steps = new List<string>();
            public int result; public float raceTime, duchessTime; public int races, practices;
            public bool recordAwarded; public int recordAwards;
        }
        static Data _d;
        static bool Test => DemoBoot.CaptureDir != null;
        public static bool SceneSelectRun { get; private set; }
        static string Key => Test ? (SceneSelectRun ? "seven_test_cattalus_select" : "seven_test_cattalus") : SceneSelectRun ? "seven_cattalus_select" : "seven_cattalus";
        static Data D { get { if (_d == null) Load(); return _d; } }
        public static void Load() { var j = PlayerPrefs.GetString(Key, ""); _d = string.IsNullOrEmpty(j) ? new Data() : JsonUtility.FromJson<Data>(j) ?? new Data(); }
        static void Save() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(_d)); PlayerPrefs.Save(); }
        public static void UseOwnSave() { if (SceneSelectRun) { SceneSelectRun = false; _d = null; } }
        public static void ResetForTest() { if (!Test) return; PlayerPrefs.DeleteKey(Key); _d = new Data(); }
        public static void ClearTestSave() { if (Test) { PlayerPrefs.DeleteKey("seven_test_cattalus"); PlayerPrefs.DeleteKey("seven_test_cattalus_select"); } }
        public static void ClearOwnSave() { UseOwnSave(); PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); _d = new Data(); }
        static Data PeekOwn()
        {
            if (!SceneSelectRun && _d != null) return _d;
            var j = PlayerPrefs.GetString(Test ? "seven_test_cattalus" : "seven_cattalus", "");
            return string.IsNullOrEmpty(j) ? new Data() : JsonUtility.FromJson<Data>(j) ?? new Data();
        }
        public static bool OwnReached => PeekOwn().cp >= 0;
        public static bool OwnHasProgress { get { var d = PeekOwn(); return d.cp > 0 || (d.done != null && d.done.Count > 0); } }
        public static bool OwnComplete => PeekOwn().cp >= CattalusLayout.CpDone;
        public static bool OwnRecordAwarded => PeekOwn().recordAwarded;
        public static Result OwnResult => (Result)PeekOwn().result;

        public static bool Reached => D.cp >= 0;
        public static int Checkpoint => Mathf.Max(0, D.cp);
        public static bool Complete => D.cp >= CattalusLayout.CpDone;
        public static bool Reach(int cp) { if (D.cp >= cp) return false; D.cp = cp; Save(); Debug.Log($"[Seven] Cattalus checkpoint -> {cp} {CattalusLayout.CpNames[Mathf.Clamp(cp, 0, CattalusLayout.CpNames.Length - 1)]}"); return true; }
        public static bool SceneDone(string id) => D.done.Contains(id);
        public static bool CompleteScene(string id) { if (D.done.Contains(id)) return false; D.done.Add(id); Save(); return true; }
        public static bool StepDone(string id) => D.steps.Contains(id);
        public static void DoStep(string id) { if (D.steps.Contains(id)) return; D.steps.Add(id); Save(); }
        public static Result RaceResult => (Result)D.result;
        public static float RaceTime => D.raceTime;
        public static float DuchessTime => D.duchessTime;
        public static int Races => D.races;
        public static int Practices => D.practices;
        /// <summary>The race's result - saved once the race is finished, never changed afterwards (the record keeps a real loss).</summary>
        public static void SetResult(Result r, float her, float duchess) { if (D.result != 0) return; D.result = (int)r; D.raceTime = her; D.duchessTime = duchess; Save(); }
        public static void CountRace() { D.races++; Save(); }
        public static void CountPractice() { D.practices++; Save(); }
        public static bool RecordAwarded => D.recordAwarded;
        public static int RecordAwards => D.recordAwards;
        public static bool AwardRecord() { if (D.recordAwarded) return false; D.recordAwarded = true; D.recordAwards++; Save(); return true; }

        /// <summary>The Saucer scene plays CAT_01 (the egg chair, the approach, the berth) before the Cattalus scene.</summary>
        public static bool ArrivalAboard;
        /// <summary>The Saucer scene plays CAT_12 (the pilot's chair, the seven records, the Moon) straight from the beam-up.</summary>
        public static bool HomeAboard;

        public static void StartFresh()
        {
            UseOwnSave();
            PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); _d = new Data { cp = 0 }; Save();
            Debug.Log("[Seven] Cattalus started fresh");
            ArrivalAboard = true; Chapter1State.Aboard = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer");
        }
        public static void ContinueOwn()
        {
            UseOwnSave();
            Debug.Log("[Seven] continue Cattalus: " + Summary());
            if (D.cp <= CattalusLayout.CpArrival) { ArrivalAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); }
            else if (D.cp >= CattalusLayout.CpHome) { HomeAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); }
            else UnityEngine.SceneManagement.SceneManager.LoadScene("Cattalus");
        }

        public static readonly (string label, int cp)[] SelectEntries =
        {
            ("Cattalus: home, apparently", CattalusLayout.CpArrival), ("Cattalus: a door problem", CattalusLayout.CpCity),
            ("Cattalus: the Cat-a-tron", CattalusLayout.CpScreen), ("Cattalus: the Grand Circuit", CattalusLayout.CpRace),
            ("Cattalus: the final memory", CattalusLayout.CpSphere), ("Cattalus: the field", CattalusLayout.CpMeadow),
            ("Cattalus: the way back to the Moon", CattalusLayout.CpHome),
        };
        static readonly string[][] DoneBy =
        {
            new[] { "CAT_01" }, new[] { "CAT_02", "CAT_03" }, new[] { "CAT_04", "CAT_05", "CAT_06" }, new[] { "CAT_07", "CAT_08" }, new[] { "CAT_09" }, new[] { "CAT_10", "CAT_11" },
        };
        /// <summary>The state a real playthrough has when it reaches checkpoint cp (a scene-select race past the race = won).</summary>
        public static void Fake(int cp, bool onOwnSave, Result result = Result.Won)
        {
            if (!onOwnSave) SceneSelectRun = true;
            _d = new Data { cp = cp };
            for (int c = 0; c < cp && c < DoneBy.Length; c++) _d.done.AddRange(DoneBy[c]);
            if (cp > CattalusLayout.CpRace) { _d.result = (int)result; _d.races = 1; _d.raceTime = 146f; _d.duchessTime = result == Result.Won ? 151f : 143f; }
            if (cp > CattalusLayout.CpSphere) { _d.recordAwarded = true; _d.recordAwards = 1; }
            Save();
        }
        public static void StartAt(int cp)
        {
            Fake(cp, false);
            Debug.Log($"[Seven] scene select: {CattalusLayout.CpNames[cp]} - {Summary()}");
            if (cp == CattalusLayout.CpArrival) { ArrivalAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); return; }
            if (cp >= CattalusLayout.CpHome) { HomeAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); return; }
            UnityEngine.SceneManagement.SceneManager.LoadScene("Cattalus");
        }

        public static string ResultText => RaceResult == Result.Won ? $"Grand Circuit champion ({Fmt(D.raceTime)}, Duchess {Fmt(D.duchessTime)})"
            : RaceResult == Result.RunnerUp ? $"Grand Circuit runner-up ({Fmt(D.raceTime)}, Duchess {Fmt(D.duchessTime)}); the trophy by a trade" : "not raced yet";
        public static string Fmt(float s) => s <= 0f ? "-" : $"{(int)(s / 60f)}:{s % 60f:00.00}";
        public static string Summary() => $"Cattalus state: {(Reached ? CattalusLayout.CpNames[Mathf.Clamp(D.cp, 0, CattalusLayout.CpNames.Length - 1)] : "not started")} (cp {D.cp}), scenes [{string.Join(", ", D.done)}], steps [{string.Join(", ", D.steps)}], race {ResultText}, races {D.races}, practice laps {D.practices}, record {(D.recordAwarded ? "awarded" : "not yet")} (awards {D.recordAwards})";
    }

    /// <summary>Round 18: the Cattalus dialogue (Resources/Cattalus/cattalus_dialogue.json, from Tools/Voice/make_cattalus_lines.py)
    /// and its voices (Resources/CattalusVoice/&lt;id&gt;.mp3). Numbered ranges take ONLY the plain numbered lines (CAT_03_05);
    /// added lines (_P1, _CALL1) are always named by id - so no line can be taken twice (QF25 3).</summary>
    public static class CattalusDialogue
    {
        [System.Serializable] public class Line { public string id, scene, speaker, label, text, direction, voice; public bool repeat; }
        [System.Serializable] class File { public List<Line> lines; }
        static File _f;
        static File F { get { if (_f == null) { var ta = Resources.Load<TextAsset>("Cattalus/cattalus_dialogue"); _f = ta != null ? JsonUtility.FromJson<File>(ta.text) : new File { lines = new List<Line>() }; } return _f; } }
        public static IReadOnlyList<Line> All => F.lines;
        public static List<VoiceBank.Line> Scene(string scene, int from = 1, int to = 99)
        {
            var l = new List<VoiceBank.Line>();
            foreach (var x in F.lines)
            {
                if (x.scene != scene) continue;
                var tail = x.id.Substring(x.id.LastIndexOf('_') + 1);
                if (!int.TryParse(tail, out int n)) continue;
                if (n < from || n > to) continue;
                l.Add(Make(x));
            }
            return l;
        }
        static VoiceBank.Line Make(Line x) => new VoiceBank.Line { Id = x.id, Speaker = x.speaker, Text = x.text, Scene = 700, PauseAfter = 0.45f, Clip = Resources.Load<AudioClip>("CattalusVoice/" + x.id), Label = x.label, Repeat = x.repeat };
        public static List<VoiceBank.Line> Ids(params string[] ids)
        {
            var l = new List<VoiceBank.Line>();
            foreach (var id in ids) { var x = F.lines.Find(y => y.id == id); if (x != null) l.Add(Make(x)); else Debug.LogWarning("[Seven] Cattalus line missing: " + id); }
            return l;
        }
        public static int Count(string scene) => F.lines.FindAll(x => x.scene == scene).Count;
    }
}
