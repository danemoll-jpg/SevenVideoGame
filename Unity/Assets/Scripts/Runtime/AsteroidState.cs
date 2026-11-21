using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 16 — Chapter "The Asteroid — the Great Machine Age": progression and persistence (as MarsState). A checkpoint
    /// (AsteroidLayout.Cp*) that only moves forward, the one-time scenes, the steps done inside a checkpoint (junctions,
    /// controls), whether she has the arc blades (she keeps them for later chapters - <see cref="BladesOwned"/> also reads
    /// the player's own save from any chapter), the bird (seen or skipped - AST_08_05's variant), the record + cuboctahedron
    /// awarded exactly once. PlayerPrefs "seven_asteroid" (Scene Select "seven_asteroid_select"; check runs "seven_test_*").
    /// </summary>
    public static class AsteroidState
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
        static string Key => Test ? (SceneSelectRun ? "seven_test_asteroid_select" : "seven_test_asteroid") : SceneSelectRun ? "seven_asteroid_select" : "seven_asteroid";
        static Data D { get { if (_d == null) Load(); return _d; } }
        public static void Load() { var j = PlayerPrefs.GetString(Key, ""); _d = string.IsNullOrEmpty(j) ? new Data() : JsonUtility.FromJson<Data>(j) ?? new Data(); }
        static void Save() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(_d)); PlayerPrefs.Save(); }
        public static void UseOwnSave() { if (SceneSelectRun) { SceneSelectRun = false; _d = null; } }
        public static void ResetForTest() { if (!Test) return; PlayerPrefs.DeleteKey(Key); _d = new Data(); }
        public static void ClearTestSave() { if (Test) { PlayerPrefs.DeleteKey("seven_test_asteroid"); PlayerPrefs.DeleteKey("seven_test_asteroid_select"); } }
        public static void ClearOwnSave() { UseOwnSave(); PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); _d = new Data(); }
        static Data PeekOwn()
        {
            if (!SceneSelectRun && _d != null) return _d;
            var j = PlayerPrefs.GetString(Test ? "seven_test_asteroid" : "seven_asteroid", "");
            return string.IsNullOrEmpty(j) ? new Data() : JsonUtility.FromJson<Data>(j) ?? new Data();
        }
        public static bool OwnReached => PeekOwn().cp >= 0;
        public static bool OwnHasProgress { get { var d = PeekOwn(); return d.cp > 0 || (d.done != null && d.done.Count > 0); } }
        public static bool OwnComplete => PeekOwn().cp >= AsteroidLayout.CpDone;
        /// <summary>She found the arc blades (AST_05) - on this run's slot, or on the player's own save (her kit grows).</summary>
        public static bool BladesOwned => D.blades || PeekOwn().blades;
        public static void GiveBlades() { if (D.blades) return; D.blades = true; Save(); }
        public static bool BirdSeen => D.birdSeen;
        public static void SeeBird() { if (D.birdSeen) return; D.birdSeen = true; Save(); }

        public static bool Reached => D.cp >= 0;
        public static int Checkpoint => Mathf.Max(0, D.cp);
        public static bool Complete => D.cp >= AsteroidLayout.CpDone;
        public static bool HasProgress { get { UseOwnSave(); return D.cp > 0 || D.done.Count > 0; } }
        public static bool Reach(int cp) { if (D.cp >= cp) return false; D.cp = cp; Save(); Debug.Log($"[Seven] Asteroid checkpoint -> {cp} {AsteroidLayout.CpNames[Mathf.Clamp(cp, 0, AsteroidLayout.CpNames.Length - 1)]}"); return true; }
        public static bool SceneDone(string id) => D.done.Contains(id);
        public static bool CompleteScene(string id) { if (D.done.Contains(id)) return false; D.done.Add(id); Save(); return true; }
        public static bool StepDone(string id) => D.steps.Contains(id);
        public static void DoStep(string id) { if (D.steps.Contains(id)) return; D.steps.Add(id); Save(); }
        public static int StepsDone(string prefix) { int n = 0; foreach (var s in D.steps) if (s.StartsWith(prefix)) n++; return n; }
        public static bool RecordAwarded => D.recordAwarded;
        public static int RecordAwards => D.recordAwards;
        public static bool AwardRecord() { if (D.recordAwarded) return false; D.recordAwarded = true; D.recordAwards++; Save(); return true; }

        /// <summary>The Saucer scene plays AST_01 (the arrival in the pilot's chair) before the Asteroid scene.</summary>
        public static bool ArrivalAboard;
        /// <summary>The Saucer scene plays AST_10 (the reunion straight from the beam-up) + the chair talk + AST_11.</summary>
        public static bool HomeAboard;

        public static void StartFresh()
        {
            UseOwnSave(); bool blades = PeekOwn().blades;
            PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); _d = new Data { cp = 0, blades = false }; Save();
            Debug.Log("[Seven] the Asteroid started fresh" + (blades ? " (she found the blades on an earlier pass - found again in AST_05)" : ""));
            ArrivalAboard = true; Chapter1State.Aboard = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer");
        }
        public static void ContinueOwn()
        {
            UseOwnSave();
            Debug.Log("[Seven] continue the Asteroid: " + Summary());
            if (D.cp <= AsteroidLayout.CpArrival) { ArrivalAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); }
            else if (D.cp >= AsteroidLayout.CpHome) { HomeAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); }
            else UnityEngine.SceneManagement.SceneManager.LoadScene("Asteroid");
        }

        public static readonly (string label, int cp)[] SelectEntries =
        {
            ("The Asteroid: arrival (the pilot's chair)", AsteroidLayout.CpArrival), ("The Asteroid: receiving", AsteroidLayout.CpReceiving),
            ("The Asteroid: the city that still works", AsteroidLayout.CpCity), ("The Asteroid: the Machinist", AsteroidLayout.CpMachinist),
            ("The Asteroid: the three controls", AsteroidLayout.CpControls), ("The Asteroid: a missed answer", AsteroidLayout.CpDrop),
            ("The Asteroid: the caretaker's objection (the fight)", AsteroidLayout.CpFight), ("The Asteroid: the archive + the vision", AsteroidLayout.CpArchive),
            ("The Asteroid: a different instruction", AsteroidLayout.CpInstruction), ("The Asteroid: back aboard", AsteroidLayout.CpHome),
        };
        static readonly string[][] DoneBy =
        {
            new[] { "AST_01" }, new[] { "AST_02" }, new[] { "AST_03" }, new[] { "AST_04" }, new[] { "AST_05" }, new[] { "AST_06" },
            new[] { "AST_07" }, new[] { "AST_08" }, new[] { "AST_09" },
        };
        static readonly string[][] StepsBy =
        {
            new string[0], new string[0], new[] { "junction1", "junction2", "junction3" }, new string[0], new[] { "control_transport", "control_cooling" },
            new[] { "control_diverter" }, new string[0], new string[0], new string[0],
        };
        /// <summary>The state a real playthrough has when it reaches checkpoint cp.</summary>
        public static void Fake(int cp, bool onOwnSave)
        {
            if (!onOwnSave) SceneSelectRun = true;
            _d = new Data { cp = cp };
            for (int c = 0; c < cp && c < DoneBy.Length; c++) { _d.done.AddRange(DoneBy[c]); _d.steps.AddRange(StepsBy[c]); }
            if (cp >= AsteroidLayout.CpControls + 1 || cp > AsteroidLayout.CpControls) _d.blades = true;
            if (cp > AsteroidLayout.CpInstruction) { _d.recordAwarded = true; _d.recordAwards = 1; }
            Save();
        }
        public static void StartAt(int cp)
        {
            Fake(cp, false);
            Debug.Log($"[Seven] scene select: {AsteroidLayout.CpNames[cp]} - {Summary()}");
            if (cp == AsteroidLayout.CpArrival) { ArrivalAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); return; }
            if (cp >= AsteroidLayout.CpHome) { HomeAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); return; }
            UnityEngine.SceneManagement.SceneManager.LoadScene("Asteroid");
        }

        public static string Summary() => $"Asteroid state: {(Reached ? AsteroidLayout.CpNames[Mathf.Clamp(D.cp, 0, AsteroidLayout.CpNames.Length - 1)] : "not started")} (cp {D.cp}), scenes [{string.Join(", ", D.done)}], steps [{string.Join(", ", D.steps)}], blades {(D.blades ? "hers" : "not yet")}, bird {(D.birdSeen ? "seen" : "not seen")}, record {(D.recordAwarded ? "awarded" : "not yet")} (awards {D.recordAwards})";
    }

    /// <summary>Round 16: the asteroid chapter's dialogue (Resources/Asteroid/asteroid_dialogue.json, from
    /// Tools/Voice/make_asteroid_lines.py) and its voices (Resources/AsteroidVoice/&lt;id&gt;.mp3).</summary>
    public static class AsteroidDialogue
    {
        [System.Serializable] public class Line { public string id, scene, speaker, label, text, direction, voice; }
        [System.Serializable] class File { public List<Line> lines; }
        static File _f;
        static File F { get { if (_f == null) { var ta = Resources.Load<TextAsset>("Asteroid/asteroid_dialogue"); _f = ta != null ? JsonUtility.FromJson<File>(ta.text) : new File { lines = new List<Line>() }; } return _f; } }
        public static IReadOnlyList<Line> All => F.lines;
        /// <summary>The numbered lines of a scene from..to (named ids like AST_02_P1 go through <see cref="Ids"/>).</summary>
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
        static VoiceBank.Line Make(Line x) => new VoiceBank.Line { Id = x.id, Speaker = x.speaker, Text = x.text, Scene = 500, PauseAfter = 0.45f, Clip = Resources.Load<AudioClip>("AsteroidVoice/" + x.id), Label = x.label };
        public static List<VoiceBank.Line> Ids(params string[] ids)
        {
            var l = new List<VoiceBank.Line>();
            foreach (var id in ids) { var x = F.lines.Find(y => y.id == id); if (x != null) l.Add(Make(x)); else Debug.LogWarning("[Seven] Asteroid line missing: " + id); }
            return l;
        }
        public static int Count(string scene) => F.lines.FindAll(x => x.scene == scene).Count;
    }
}
