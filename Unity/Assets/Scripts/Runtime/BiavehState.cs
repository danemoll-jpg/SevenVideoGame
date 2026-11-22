using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 17 — Chapter "The Biavian Mothership (Veyra)": progression and persistence (a copy of AsteroidState). A
    /// checkpoint (BiavehLayout.Cp*) that only moves forward, the one-time scenes, the steps inside a checkpoint (relays),
    /// the record + octahedron awarded exactly once. PlayerPrefs "seven_biaveh" (Scene Select "seven_biaveh_select"; check
    /// runs "seven_test_*"). The arc blades are hers if she found them on the asteroid (the chapters go in either order).
    /// </summary>
    public static class BiavehState
    {
        [System.Serializable]
        class Data
        {
            public int cp = -1;
            public List<string> done = new List<string>();
            public List<string> steps = new List<string>();       // junction1..3, control_transport / cooling / diverter, ...
            public bool recordAwarded; public int recordAwards;
            public bool blades; public bool birdSeen;
        }
        static Data _d;
        static bool Test => DemoBoot.CaptureDir != null;
        public static bool SceneSelectRun { get; private set; }
        static string Key => Test ? (SceneSelectRun ? "seven_test_biaveh_select" : "seven_test_biaveh") : SceneSelectRun ? "seven_biaveh_select" : "seven_biaveh";
        static Data D { get { if (_d == null) Load(); return _d; } }
        public static void Load() { var j = PlayerPrefs.GetString(Key, ""); _d = string.IsNullOrEmpty(j) ? new Data() : JsonUtility.FromJson<Data>(j) ?? new Data(); }
        static void Save() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(_d)); PlayerPrefs.Save(); }
        public static void UseOwnSave() { if (SceneSelectRun) { SceneSelectRun = false; _d = null; } }
        public static void ResetForTest() { if (!Test) return; PlayerPrefs.DeleteKey(Key); _d = new Data(); }
        public static void ClearTestSave() { if (Test) { PlayerPrefs.DeleteKey("seven_test_biaveh"); PlayerPrefs.DeleteKey("seven_test_biaveh_select"); } }
        public static void ClearOwnSave() { UseOwnSave(); PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); _d = new Data(); }
        static Data PeekOwn()
        {
            if (!SceneSelectRun && _d != null) return _d;
            var j = PlayerPrefs.GetString(Test ? "seven_test_biaveh" : "seven_biaveh", "");
            return string.IsNullOrEmpty(j) ? new Data() : JsonUtility.FromJson<Data>(j) ?? new Data();
        }
        public static bool OwnReached => PeekOwn().cp >= 0;
        public static bool OwnHasProgress { get { var d = PeekOwn(); return d.cp > 0 || (d.done != null && d.done.Count > 0); } }
        public static bool OwnComplete => PeekOwn().cp >= BiavehLayout.CpDone;
        /// <summary>The arc blades: hers if she found them on the asteroid (either order - without them the fight is fists only).</summary>
        public static bool BladesOwned => AsteroidState.BladesOwned || D.blades || (DemoBoot.CaptureDir != null && DemoBoot.BiaBlades);
        public static bool PursuitSeen => D.birdSeen;
        public static void SeePursuit() { if (D.birdSeen) return; D.birdSeen = true; Save(); }

        public static bool Reached => D.cp >= 0;
        public static int Checkpoint => Mathf.Max(0, D.cp);
        public static bool Complete => D.cp >= BiavehLayout.CpDone;
        public static bool HasProgress { get { UseOwnSave(); return D.cp > 0 || D.done.Count > 0; } }
        public static bool Reach(int cp) { if (D.cp >= cp) return false; D.cp = cp; Save(); Debug.Log($"[Seven] Mothership checkpoint -> {cp} {BiavehLayout.CpNames[Mathf.Clamp(cp, 0, BiavehLayout.CpNames.Length - 1)]}"); return true; }
        public static bool SceneDone(string id) => D.done.Contains(id);
        public static bool CompleteScene(string id) { if (D.done.Contains(id)) return false; D.done.Add(id); Save(); return true; }
        public static bool StepDone(string id) => D.steps.Contains(id);
        public static void DoStep(string id) { if (D.steps.Contains(id)) return; D.steps.Add(id); Save(); }
        public static int StepsDone(string prefix) { int n = 0; foreach (var s in D.steps) if (s.StartsWith(prefix)) n++; return n; }
        public static bool RecordAwarded => D.recordAwarded;
        /// <summary>QF24 (2): the record on the PLAYER'S OWN save (what she has really collected - shown in every scene, Scene Select included).</summary>
        public static bool OwnRecordAwarded => PeekOwn().recordAwarded;
        public static int RecordAwards => D.recordAwards;
        public static bool AwardRecord() { if (D.recordAwarded) return false; D.recordAwarded = true; D.recordAwards++; Save(); return true; }

        /// <summary>The Saucer scene plays BIA_01 + BIA_02 (among Veyra's rings, the iris) before the Biaveh scene.</summary>
        public static bool ArrivalAboard;
        /// <summary>The Saucer scene plays BIA_12 (the reunion straight from the beam-up) + BIA_13.</summary>
        public static bool HomeAboard;

        public static void StartFresh()
        {
            UseOwnSave();
            PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); _d = new Data { cp = 0 }; Save();
            Debug.Log("[Seven] the Biavian Mothership started fresh");
            ArrivalAboard = true; Chapter1State.Aboard = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer");
        }
        public static void ContinueOwn()
        {
            UseOwnSave();
            Debug.Log("[Seven] continue the Biavian Mothership: " + Summary());
            if (D.cp <= BiavehLayout.CpArrival) { ArrivalAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); }
            else if (D.cp >= BiavehLayout.CpHome) { HomeAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); }
            else UnityEngine.SceneManagement.SceneManager.LoadScene("Biaveh");
        }

        public static readonly (string label, int cp)[] SelectEntries =
        {
            ("The Mothership: among Veyra's rings", BiavehLayout.CpArrival), ("The Mothership: a world inside", BiavehLayout.CpBerth),
            ("The Mothership: the first relay (the bridge)", BiavehLayout.CpRelay1), ("The Mothership: the overlapping garden", BiavehLayout.CpRelay2),
            ("The Mothership: the quiet passage", BiavehLayout.CpRelay3), ("The Mothership: cornering Ratwing (the fight)", BiavehLayout.CpFight),
            ("The Mothership: the octahedron", BiavehLayout.CpTemple), ("The Mothership: back aboard", BiavehLayout.CpHome),
        };
        static readonly string[][] DoneBy =
        {
            new[] { "BIA_01", "BIA_02" }, new[] { "BIA_03", "BIA_04" }, new[] { "BIA_05", "BIA_06" }, new[] { "BIA_07" }, new[] { "BIA_08" },
            new[] { "BIA_09" }, new[] { "BIA_10", "BIA_11" },
        };
        static readonly string[][] StepsBy =
        {
            new string[0], new string[0], new[] { "relay1" }, new[] { "relay2" }, new[] { "relay3" }, new string[0], new string[0],
        };
        /// <summary>The state a real playthrough has when it reaches checkpoint cp.</summary>
        public static void Fake(int cp, bool onOwnSave)
        {
            if (!onOwnSave) SceneSelectRun = true;
            _d = new Data { cp = cp };
            for (int c = 0; c < cp && c < DoneBy.Length; c++) { _d.done.AddRange(DoneBy[c]); _d.steps.AddRange(StepsBy[c]); }
            if (cp > BiavehLayout.CpTemple) { _d.recordAwarded = true; _d.recordAwards = 1; }
            Save();
        }
        public static void StartAt(int cp)
        {
            Fake(cp, false);
            Debug.Log($"[Seven] scene select: {BiavehLayout.CpNames[cp]} - {Summary()}");
            if (cp == BiavehLayout.CpArrival) { ArrivalAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); return; }
            if (cp >= BiavehLayout.CpHome) { HomeAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); return; }
            UnityEngine.SceneManagement.SceneManager.LoadScene("Biaveh");
        }

        public static string Summary() => $"Mothership state: {(Reached ? BiavehLayout.CpNames[Mathf.Clamp(D.cp, 0, BiavehLayout.CpNames.Length - 1)] : "not started")} (cp {D.cp}), scenes [{string.Join(", ", D.done)}], steps [{string.Join(", ", D.steps)}], blades {(BladesOwned ? "hers" : "not found yet (the asteroid)")}, pursuit {(D.birdSeen ? "taken" : "not taken")}, record {(D.recordAwarded ? "awarded" : "not yet")} (awards {D.recordAwards})";
    }

    /// <summary>Round 17: the mothership chapter's dialogue (Resources/Biaveh/biaveh_dialogue.json, from
    /// Tools/Voice/make_biaveh_lines.py) and its voices (Resources/BiavehVoice/&lt;id&gt;.mp3).</summary>
    public static class BiavehDialogue
    {
        [System.Serializable] public class Line { public string id, scene, speaker, label, text, direction, voice; }
        [System.Serializable] class File { public List<Line> lines; }
        static File _f;
        static File F { get { if (_f == null) { var ta = Resources.Load<TextAsset>("Biaveh/biaveh_dialogue"); _f = ta != null ? JsonUtility.FromJson<File>(ta.text) : new File { lines = new List<Line>() }; } return _f; } }
        public static IReadOnlyList<Line> All => F.lines;
        /// <summary>The numbered lines of a scene from..to (named ids like AST_02_P1 go through <see cref="Ids"/>).</summary>
        public static List<VoiceBank.Line> Scene(string scene, int from = 1, int to = 99)
        {
            var l = new List<VoiceBank.Line>();
            foreach (var x in F.lines)
            {
                if (x.scene != scene) continue;
                var tail = x.id.Substring(x.id.LastIndexOf('_') + 1);
                // QF23: inserted lines (BIA_01_06a-d, BIA_08_06a/b) play in order after their number - the leading digits count
                int dl = 0; while (dl < tail.Length && char.IsDigit(tail[dl])) dl++;
                if (dl == 0 || !int.TryParse(tail.Substring(0, dl), out int n)) continue;
                if (n < from || n > to) continue;
                l.Add(Make(x));
            }
            return l;
        }
        static VoiceBank.Line Make(Line x) => new VoiceBank.Line { Id = x.id, Speaker = x.speaker, Text = x.text, Scene = 600, PauseAfter = 0.45f, Clip = Resources.Load<AudioClip>("BiavehVoice/" + x.id), Label = x.label };
        public static List<VoiceBank.Line> Ids(params string[] ids)
        {
            var l = new List<VoiceBank.Line>();
            foreach (var id in ids) { var x = F.lines.Find(y => y.id == id); if (x != null) l.Add(Make(x)); else Debug.LogWarning("[Seven] Biaveh line missing: " + id); }
            return l;
        }
        public static int Count(string scene) => F.lines.FindAll(x => x.scene == scene).Count;
    }
}
