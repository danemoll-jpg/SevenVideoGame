using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 19 — "The Moon Finale": progression and persistence (a copy of CattalusState). A checkpoint (FinaleLayout.Cp*) that
    /// only moves forward, the one-time scenes, the steps inside a checkpoint (objectives done stay done - the confrontation's
    /// retry keeps them), the runes placed, and the finished-game mark (set once the credits have played; the main menu and the
    /// Log show it). PlayerPrefs "seven_finale" (Scene Select "seven_finale_select"; check runs "seven_test_*").
    /// </summary>
    public static class FinaleState
    {
        /// <summary>QF29: the finale ships in 1.0.0-rc1; in 0.18.4 (QF28 + QF29) it is switched off: CAT_12 ends "to be continued",
        /// the star map's Moon shows the coming-soon card, the menu has no finale entry. One switch.</summary>
        public static readonly bool Released = true;
        [System.Serializable]
        class Data
        {
            public int cp = -1;
            public List<string> done = new List<string>();
            public List<string> steps = new List<string>();
            public int runesPlaced; public bool finished; public int finishes; public int retries;
        }
        static Data _d;
        static bool Test => DemoBoot.CaptureDir != null;
        public static bool SceneSelectRun { get; private set; }
        static string Key => Test ? (SceneSelectRun ? "seven_test_finale_select" : "seven_test_finale") : SceneSelectRun ? "seven_finale_select" : "seven_finale";
        static Data D { get { if (_d == null) Load(); return _d; } }
        public static void Load() { var j = PlayerPrefs.GetString(Key, ""); _d = string.IsNullOrEmpty(j) ? new Data() : JsonUtility.FromJson<Data>(j) ?? new Data(); }
        static void Save() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(_d)); PlayerPrefs.Save(); }
        public static void UseOwnSave() { if (SceneSelectRun) { SceneSelectRun = false; _d = null; } }
        public static void ResetForTest() { if (!Test) return; PlayerPrefs.DeleteKey(Key); _d = new Data(); }
        public static void ClearOwnSave() { UseOwnSave(); PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); _d = new Data(); }
        static Data PeekOwn()
        {
            if (!SceneSelectRun && _d != null) return _d;
            var j = PlayerPrefs.GetString(Test ? "seven_test_finale" : "seven_finale", "");
            return string.IsNullOrEmpty(j) ? new Data() : JsonUtility.FromJson<Data>(j) ?? new Data();
        }
        public static bool OwnReached => PeekOwn().cp >= 0;
        public static bool OwnHasProgress { get { var d = PeekOwn(); return d.cp > 0 || (d.done != null && d.done.Count > 0); } }
        public static bool OwnComplete => PeekOwn().cp >= FinaleLayout.CpDone;
        /// <summary>The whole game finished (the credits played to the end at least once, on the player's own save).</summary>
        public static bool GameFinished => PeekOwn().finished;

        public static bool Reached => D.cp >= 0;
        public static int Checkpoint => Mathf.Max(0, D.cp);
        public static bool Complete => D.cp >= FinaleLayout.CpDone;
        public static bool Reach(int cp) { if (D.cp >= cp) return false; D.cp = cp; Save(); Debug.Log($"[Seven] Finale checkpoint -> {cp} {FinaleLayout.CpNames[Mathf.Clamp(cp, 0, FinaleLayout.CpNames.Length - 1)]}"); return true; }
        public static bool SceneDone(string id) => D.done.Contains(id);
        public static bool CompleteScene(string id) { if (D.done.Contains(id)) return false; D.done.Add(id); Save(); return true; }
        public static bool StepDone(string id) => D.steps.Contains(id);
        public static void DoStep(string id) { if (D.steps.Contains(id)) return; D.steps.Add(id); Save(); }
        public static int RunesPlaced { get => D.runesPlaced; set { D.runesPlaced = Mathf.Clamp(value, 0, 7); Save(); } }
        public static int Retries => D.retries;
        public static void CountRetry() { D.retries++; Save(); }
        /// <summary>The credits have played: the game is finished (the main menu's mark, the Log's last entry).</summary>
        public static void MarkFinished() { D.finished = true; D.finishes++; if (D.cp < FinaleLayout.CpDone) D.cp = FinaleLayout.CpDone; Save(); if (!SceneSelectRun) WorldFlags.MarkGameFinished(); Debug.Log("[Seven] the game is finished (the credits played)"); }
        public static int Finishes => D.finishes;

        /// <summary>The Saucer scene plays FIN_01 (the egg chair, the approach, guiding the saucer in) before the Finale scene.</summary>
        public static bool ArrivalAboard;

        public static void StartFresh()
        {
            UseOwnSave();
            PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); _d = new Data { cp = 0 }; Save();
            Debug.Log("[Seven] the Moon finale started fresh");
            ArrivalAboard = true; Chapter1State.Aboard = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer");
        }
        public static void ContinueOwn()
        {
            UseOwnSave();
            Debug.Log("[Seven] continue the Moon finale: " + Summary());
            if (D.cp <= FinaleLayout.CpArrival) { ArrivalAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); }
            else UnityEngine.SceneManagement.SceneManager.LoadScene("Finale");
        }

        public static readonly (string label, int cp)[] SelectEntries =
        {
            ("The Moon finale: the familiar approach", FinaleLayout.CpArrival), ("The Moon finale: beneath the lights", FinaleLayout.CpBerth),
            ("The Moon finale: the people keeping it running", FinaleLayout.CpJunction), ("The Moon finale: the door from the memory", FinaleLayout.CpDoor),
            ("The Moon finale: seven places, one history", FinaleLayout.CpChamber), ("The Moon finale: Tunneler arrives", FinaleLayout.CpConfront),
            ("The Moon finale: what she carries", FinaleLayout.CpAfter), ("The Moon finale: somewhere to come back to", FinaleLayout.CpEnding),
        };
        static readonly string[][] DoneBy =
        {
            new[] { "FIN_01" }, new[] { "FIN_02" }, new[] { "FIN_03" }, new[] { "FIN_04" }, new[] { "FIN_05", "FIN_06", "FIN_07" },
            new[] { "FIN_08", "FIN_09" }, new[] { "FIN_10", "FIN_11", "FIN_12", "FIN_13" },
        };
        /// <summary>The state a real playthrough has when it reaches checkpoint cp.</summary>
        public static void Fake(int cp, bool onOwnSave)
        {
            if (!onOwnSave) SceneSelectRun = true;
            _d = new Data { cp = cp };
            for (int c = 0; c < cp && c < DoneBy.Length; c++) _d.done.AddRange(DoneBy[c]);
            if (cp > FinaleLayout.CpChamber) _d.runesPlaced = 7;
            if (cp > FinaleLayout.CpJunction) { _d.steps.Add("power"); _d.steps.Add("gate"); }
            Save();
        }
        public static void StartAt(int cp)
        {
            Fake(cp, false);
            Debug.Log($"[Seven] scene select: {FinaleLayout.CpNames[cp]} - {Summary()}");
            if (cp == FinaleLayout.CpArrival) { ArrivalAboard = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); return; }
            UnityEngine.SceneManagement.SceneManager.LoadScene("Finale");
        }
        public static string Summary() => $"Finale state: {(Reached ? FinaleLayout.CpNames[Mathf.Clamp(D.cp, 0, FinaleLayout.CpNames.Length - 1)] : "not started")} (cp {D.cp}), scenes [{string.Join(", ", D.done)}], steps [{string.Join(", ", D.steps)}], runes placed {D.runesPlaced}, confrontation retries {D.retries}, finished {D.finished} ({D.finishes}x)";
    }

    /// <summary>Round 19: the finale's dialogue (Resources/Finale/finale_dialogue.json, from Tools/Voice/make_finale_lines.py) and its
    /// voices (Resources/FinaleVoice/&lt;id&gt;.mp3). Numbered ranges take ONLY the plain numbered lines; proposed lines (_P1) are always
    /// named by id - no line can be taken twice (QF25 3).</summary>
    public static class FinaleDialogue
    {
        [System.Serializable] public class Line { public string id, scene, speaker, label, text, direction, voice; public bool repeat; }
        [System.Serializable] class File { public List<Line> lines; }
        static File _f;
        static File F { get { if (_f == null) { var ta = Resources.Load<TextAsset>("Finale/finale_dialogue"); _f = ta != null ? JsonUtility.FromJson<File>(ta.text) : new File { lines = new List<Line>() }; } return _f; } }
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
        static VoiceBank.Line Make(Line x) => new VoiceBank.Line { Id = x.id, Speaker = x.speaker, Text = x.text, Scene = 800, PauseAfter = 0.45f, Clip = Resources.Load<AudioClip>("FinaleVoice/" + x.id), Label = x.label, Repeat = x.repeat };
        public static List<VoiceBank.Line> Ids(params string[] ids)
        {
            var l = new List<VoiceBank.Line>();
            foreach (var id in ids) { var x = F.lines.Find(y => y.id == id); if (x != null) l.Add(Make(x)); else Debug.LogWarning("[Seven] Finale line missing: " + id); }
            return l;
        }
        public static int Count(string scene) => F.lines.FindAll(x => x.scene == scene).Count;
    }
}
