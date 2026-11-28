using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 11 — Chapter 2 "Venus" progression and persistence (spec Docs/CHAPTER2_VENUS.md section 5).
    /// Ordered stages VEN_S00_AVAILABLE … VEN_S08_COMPLETE, each with its objective; persisted separately: disabled control
    /// device IDs, Wormmaster contained, record awarded, completed one-time conversation IDs, optional garden assistance,
    /// the hover bike's parked spot and whether 7 is riding. Stages only ever move forward (a replayed or skipped scene can
    /// never take the save back); the record is awarded exactly once (<see cref="AwardRecord"/>); a scene counts as done
    /// only when it completes (<see cref="CompleteScene"/>) - an interrupted scene restarts from its beginning.
    /// Stored as one JSON string in PlayerPrefs (check runs use their own key and remove it again).
    /// The whole system is built now; Round 11 reaches VEN_S04, Round 12 the rest.
    /// </summary>
    public static class VenusState
    {
        public enum Stage { Available = 0, Landed = 1, GuardianMet = 2, HelpAccepted = 3, ControlDiscovered = 4, EncounterActive = 5, ThreatStopped = 6, RecordReceived = 7, Complete = 8 }

        public static readonly string[] StageIds =
        {
            "VEN_S00_AVAILABLE", "VEN_S01_LANDED", "VEN_S02_GUARDIAN_MET", "VEN_S03_HELP_ACCEPTED", "VEN_S04_CONTROL_DISCOVERED",
            "VEN_S05_ENCOUNTER_ACTIVE", "VEN_S06_THREAT_STOPPED", "VEN_S07_RECORD_RECEIVED", "VEN_S08_COMPLETE",
        };
        /// <summary>The objective shown at each stage (S01 reworded by the hover bike, spec VEN_B01).</summary>
        public static readonly string[] Objectives =
        {
            // QF13 (8): each objective says what to do AND where (the start of each stays the same: the check drivers match on it)
            "Land on Venus", "Ride to the Elarim temple - north along the garden road", "Follow the guardian to the cube chamber", "Investigate beneath the gardens - the root gate below the lookout",
            "Find the source of the signal - down the root pit stairs, then east along the tunnels", "Disable the control devices - all three, around the chamber", "Return to the guardian at the temple", "Return to the saucer at the landing platform", null,
        };

        [System.Serializable]
        class Data
        {
            public int stage = -1;                                   // -1 = Venus not reached yet
            public List<string> devicesDisabled = new List<string>();
            public bool wormmasterContained;
            public bool recordAwarded;
            public List<string> conversationsDone = new List<string>();
            public List<string> gardenAssistance = new List<string>();
            public Vector3 bikeAt; public float bikeYaw; public bool bikeKnown; public bool riding;
            public int recordAwards;                                  // check: must never exceed 1
        }

        static Data _d;
        static bool Test => DemoBoot.CaptureDir != null;
        /// <summary>QF11: a Scene Select start plays on its own save slot, so the player's own Venus save is never touched.</summary>
        public static bool SceneSelectRun { get; private set; }
        static string Key => Test ? (SceneSelectRun ? "seven_test_venus_select" : "seven_test_venus") : SceneSelectRun ? "seven_venus_select" : "seven_venus";
        static Data D { get { if (_d == null) Load(); return _d; } }

        public static void Load()
        {
            var json = PlayerPrefs.GetString(Key, "");
            _d = string.IsNullOrEmpty(json) ? new Data() : JsonUtility.FromJson<Data>(json) ?? new Data();
        }
        static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(_d)); PlayerPrefs.Save();
        }
        /// <summary>Check runs: start from nothing, and leave nothing behind.</summary>
        public static void ResetForTest() { if (!Test) return; PlayerPrefs.DeleteKey(Key); _d = new Data(); }
        public static void ClearTestSave() { if (Test) { PlayerPrefs.DeleteKey("seven_test_venus"); PlayerPrefs.DeleteKey("seven_test_venus_select"); } }

        /// <summary>The player's own save (Continue, "Chapter 2 — Venus", the saucer's set course) - leaves Scene Select mode.</summary>
        public static void UseOwnSave() { if (SceneSelectRun) { SceneSelectRun = false; _d = null; } }
        /// <summary>QF11: does the player's own save hold Venus progress? (the main menu's confirm before starting over)</summary>
        public static bool HasProgress { get { UseOwnSave(); return D.stage > (int)Stage.Available || D.conversationsDone.Count > 0; } }
        /// <summary>QF11: "Chapter 2 — Venus" always starts the chapter fresh (the descent, C01): the Venus save is cleared.</summary>
        /// <summary>Round 13 (0c): clear the player's own Venus save without loading anything (a new game).</summary>
        public static void ClearOwnSave() { UseOwnSave(); PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); _d = new Data(); }
        /// <summary>QF14 (the chapter list): the player's own save read WITHOUT switching slots - the pause menu may ask
        /// while a Scene Select session plays on its own slot, which must keep it.</summary>
        static Data PeekOwn()
        {
            if (!SceneSelectRun && _d != null) return _d;
            var json = PlayerPrefs.GetString(Test ? "seven_test_venus" : "seven_venus", "");
            return string.IsNullOrEmpty(json) ? new Data() : JsonUtility.FromJson<Data>(json) ?? new Data();
        }
        public static bool OwnReached => PeekOwn().stage >= 0;
        public static bool OwnHasProgress { get { var d = PeekOwn(); return d.stage > (int)Stage.Available || (d.conversationsDone != null && d.conversationsDone.Count > 0); } }
        public static bool OwnSceneDone(string id) { var d = PeekOwn(); return d.conversationsDone != null && d.conversationsDone.Contains(id); }
        public static void StartFresh()
        {
            UseOwnSave(); PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); _d = new Data();
            Debug.Log("[Seven] Chapter 2 started fresh: the Venus save was reset");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Venus");
        }

        // ---- QF11: Scene Select entries. Each starts with the story state, the bike and Panther exactly as a real
        // playthrough has them there - on the Scene Select slot (the player's own save is never touched).
        public static readonly (string label, string id)[] SelectEntries =
        {
            ("Venus: arrival (C01)", "C01"), ("Venus: the bike + Panther (B01)", "B01"), ("Venus: first ride (B02)", "B02"),
            ("Venus: the collapse - telekinesis bridge (B03)", "B03"), ("Venus: temple arrival + the guardian (B04 / C03)", "C03"),
            ("Venus: the cube + the tremor (C04)", "C04"), ("Venus: lower gardens + the signal (B05 / C05)", "C05"),
            ("Venus: the way down + the control chamber (C06)", "C06"), ("Venus: the return ride (B06)", "B06"),
            ("Venus: the record (C08 - C10)", "C09"), ("Venus: the debrief aboard (C11)", "C11"),
        };
        static readonly string[] Order = { "C01", "B01", "B02", "B03", "C03", "C04", "C05", "C06", "B06", "C09", "C11" };
        public static void StartAt(string entry)
        {
            SceneSelectRun = true; _d = BuildEntry(entry); Save();
            Debug.Log($"[Seven] scene select: Venus {entry} - {Summary()}");
            if (entry == "C11") { Chapter1State.Reset(); VenusAboardStart = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); return; }
            UnityEngine.SceneManagement.SceneManager.LoadScene("Venus");
        }
        /// <summary>Set when the Saucer scene should play the Venus debrief (C11) / the destination screen.</summary>
        public static bool VenusAboardStart;
        /// <summary>The main menu's Continue for Venus: the player's own save, where it left off (aboard once she has
        /// ridden back with the record).</summary>
        public static void ContinueOwn()
        {
            UseOwnSave();
            Debug.Log("[Seven] continue Venus: " + Summary());
            if (SceneDone("VEN_B07")) { VenusAboardStart = true; Chapter1State.Aboard = true; UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer"); }
            else UnityEngine.SceneManagement.SceneManager.LoadScene("Venus");
        }
        static Data BuildEntry(string e)
        {
            int k = System.Array.IndexOf(Order, e); if (k < 0) k = 0;
            var d = new Data { stage = 0 };
            void Done(Stage? st, params string[] ids) { foreach (var i in ids) if (!d.conversationsDone.Contains(i)) d.conversationsDone.Add(i); if (st.HasValue) d.stage = Mathf.Max(d.stage, (int)st.Value); }
            void Bike(Vector3 at, float yaw, bool riding) { d.bikeAt = at; d.bikeYaw = yaw; d.bikeKnown = true; d.riding = riding; }
            if (k > 0) Done(Stage.Landed, "VEN_C01");
            if (k > 1) { Done(Stage.Landed, "VEN_B01"); Bike(VenusLayout.BikeUnpacked, 0f, true); }
            if (k > 2) { Done(null, "VEN_B02"); Bike(new Vector3(30f, 0f, 86f), 0f, true); }
            if (k > 3) { Done(null, "VEN_C02", "VEN_B03", "VEN_B03_bridge"); Bike(new Vector3(30f, 0f, 176f), 0f, true); }
            if (k == 3) { d.conversationsDone.Remove("VEN_B03_bridge"); Done(null, "VEN_C02", "VEN_B03"); Bike(VenusLayout.StopSpot, 0f, true); }
            if (k > 4) { Done(Stage.GuardianMet, "VEN_B04", "VEN_C03"); Bike(VenusLayout.ParkSpot, 180f, false); }
            if (k > 5) Done(Stage.HelpAccepted, "VEN_C04", "VEN_B05_lines");
            if (k > 6) { Done(Stage.ControlDiscovered, "VEN_C05"); Bike(VenusLayout.ObsPark, 180f, false); }
            if (k > 7) { Done(Stage.ThreatStopped, "VEN_WAYDOWN", "VEN_C06", "VEN_C07"); d.stage = (int)Stage.ThreatStopped; foreach (var id in VenusLayout.DeviceIds) d.devicesDisabled.Add(id); d.wormmasterContained = true; }
            if (k > 8) { Done(null, "VEN_B06"); Bike(VenusLayout.ParkSpot, 180f, false); }
            if (k > 9) { Done(Stage.RecordReceived, "VEN_C08", "VEN_C09", "VEN_C10", "VEN_C12", "VEN_B07"); d.recordAwarded = true; d.recordAwards = 1; Bike(VenusLayout.BikeUnpacked, 0f, false); }
            return d;
        }

        public static bool Reached => D.stage >= 0;
        public static Stage Current => (Stage)Mathf.Max(0, D.stage);
        public static string CurrentId => StageIds[(int)Current];
        public static string Objective => Reached ? Objectives[(int)Current] : null;
        public static bool AtLeast(Stage s) => D.stage >= (int)s;

        /// <summary>Move to a stage - forward only. Returns true if it moved (a repeated call is a no-op).</summary>
        public static bool Advance(Stage s)
        {
            if (D.stage >= (int)s) return false;
            D.stage = (int)s; Save();
            Debug.Log($"[Seven] Venus stage -> {StageIds[(int)s]} (objective: {Objectives[(int)s] ?? "-"})");
            return true;
        }

        // ---- one-time conversations: a scene's result is applied when it completes (watched or skipped), exactly once
        public static bool SceneDone(string sceneId) => D.conversationsDone.Contains(sceneId);
        /// <summary>Mark a one-time scene complete and apply its stage result once. Returns false if it was already done.</summary>
        public static bool CompleteScene(string sceneId, Stage? result = null)
        {
            if (D.conversationsDone.Contains(sceneId)) return false;
            D.conversationsDone.Add(sceneId);
            if (result.HasValue && D.stage < (int)result.Value) D.stage = (int)result.Value;
            Save();
            Debug.Log($"[Seven] Venus scene {sceneId} complete{(result.HasValue ? " -> " + StageIds[(int)result.Value] : "")}");
            return true;
        }

        // ---- encounter (Round 12) + record
        public static IReadOnlyList<string> DevicesDisabled => D.devicesDisabled;
        public static bool IsDisabled(string id) => D.devicesDisabled.Contains(id);
        public static bool DisableDevice(string id) { if (D.devicesDisabled.Contains(id)) return false; D.devicesDisabled.Add(id); Save(); return true; }
        public static bool WormmasterContained { get => D.wormmasterContained; set { D.wormmasterContained = value; Save(); } }
        public static bool RecordAwarded => D.recordAwarded;
        /// <summary>QF24 (2): the record on the PLAYER'S OWN save (what she has really collected - shown in every scene, Scene Select included).</summary>
        public static bool OwnRecordAwarded => PeekOwn().recordAwarded;
        public static int RecordAwards => D.recordAwards;
        /// <summary>Award Venus's record - exactly once, whatever happens (replays, reloads, revisits).</summary>
        public static bool AwardRecord()
        {
            if (D.recordAwarded) return false;
            D.recordAwarded = true; D.recordAwards++; Save();
            return true;
        }
        public static bool AssistGarden(string id) { if (D.gardenAssistance.Contains(id)) return false; D.gardenAssistance.Add(id); Save(); return true; }

        // ---- the hover bike
        public static bool BikeKnown => D.bikeKnown;
        public static Vector3 BikeAt => D.bikeAt;
        public static float BikeYaw => D.bikeYaw;
        public static bool Riding => D.riding;
        public static void SaveBike(Vector3 at, float yaw, bool riding)
        {
            D.bikeAt = at; D.bikeYaw = yaw; D.bikeKnown = true; D.riding = riding; Save();
        }

        public static string Summary() =>
            $"Venus state: {(Reached ? CurrentId : "not reached")}, scenes done [{string.Join(", ", D.conversationsDone)}], devices [{string.Join(", ", D.devicesDisabled)}], " +
            $"Wormmaster contained {D.wormmasterContained}, record awarded {D.recordAwarded} (awards {D.recordAwards}), garden help [{string.Join(", ", D.gardenAssistance)}], " +
            $"bike {(D.bikeKnown ? D.bikeAt.ToString() : "-")} riding {D.riding}";

        /// <summary>Self-check of the rules above (forward-only stages, exactly-once scenes and record, reload keeps
        /// everything). Runs with -venusstatetest in a check run; returns the failures (empty = all good).</summary>
        /// <summary>Round 18 (-routes cattalus): Venus finished on the test key (VEN_C12 done, the record awarded).</summary>
        public static void FakeDoneForTest() { if (!Test) return; ResetForTest(); CompleteScene("VEN_C12"); AwardRecord(); }
        public static List<string> SelfTest()
        {
            var fail = new List<string>();
            ResetForTest();
            if (Reached) fail.Add("fresh state already reached Venus");
            Advance(Stage.Available); Advance(Stage.Landed);
            if (Advance(Stage.Available)) fail.Add("a stage went backwards");
            if (!CompleteScene("VEN_C03", Stage.GuardianMet)) fail.Add("first completion refused");
            if (CompleteScene("VEN_C03", Stage.GuardianMet)) fail.Add("a scene completed twice (skip + watch)");
            if (Current != Stage.GuardianMet) fail.Add("scene result not applied");
            AwardRecord(); AwardRecord();
            DisableDevice("dev_a"); DisableDevice("dev_a");
            SaveBike(new Vector3(1, 2, 3), 45f, true);
            _d = null; Load();                                          // reload
            if (Current != Stage.GuardianMet) fail.Add("stage lost on reload");
            if (!SceneDone("VEN_C03")) fail.Add("completed scene lost on reload");
            if (RecordAwards != 1 || !RecordAwarded) fail.Add($"record awarded {RecordAwards} times (must be 1)");
            if (DevicesDisabled.Count != 1) fail.Add("device disabled twice");
            if (!BikeKnown || (BikeAt - new Vector3(1, 2, 3)).sqrMagnitude > 1e-6f || !Riding) fail.Add("bike state lost on reload");
            ResetForTest();
            return fail;
        }
    }

    /// <summary>Round 11: the Venus dialogue data (Resources/Venus/venus_dialogue.json, from Tools/Voice/make_venus_lines.py).
    /// Lines, labels ("Panther" from VEN_B01_07), delivery and camera cues live there, not in code.</summary>
    public static class VenusDialogue
    {
        [System.Serializable] public class Line { public string id, scene, speaker, label, text, delivery, voice, cue; }
        [System.Serializable] public class Scene { public string id, title, trigger, result, delivery; public List<string> cues; public int round; }
        [System.Serializable] class File { public List<Scene> scenes; public List<Line> lines; }
        static File _f;
        static File F
        {
            get
            {
                if (_f != null) return _f;
                var ta = Resources.Load<TextAsset>("Venus/venus_dialogue");
                _f = ta != null ? JsonUtility.FromJson<File>(ta.text) : new File { scenes = new List<Scene>(), lines = new List<Line>() };
                return _f;
            }
        }
        public static IReadOnlyList<Line> All => F.lines;
        public static List<Line> SceneLines(string sceneId) => F.lines.FindAll(l => l.scene == sceneId);
        public static Scene SceneInfo(string sceneId) => F.scenes.Find(s => s.id == sceneId);
        public static Line Get(string id) => F.lines.Find(l => l.id == id);
        /// <summary>A scene's lines n..m as playable voice lines (labels, telepathy, the voice file if there is one).</summary>
        /// <summary>The line's number in its scene: "VEN_C04_07" = 7; Round 13 branch lines "VEN_C13_M1" = 901, "_E1" = 951.</summary>
        public static int LineNumber(string id)
        {
            var suf = id.Substring(id.LastIndexOf('_') + 1);
            if (int.TryParse(suf, out var n)) return n;
            // QF43: an inserted line "VEN_C05_00b" / "VEN_C06_00a" = number 0 (plays in file order with the others of its number)
            if (suf.Length > 1 && char.IsLower(suf[suf.Length - 1]) && int.TryParse(suf.Substring(0, suf.Length - 1), out var ins)) return ins;
            int k = int.TryParse(suf.Substring(1), out var m) ? m : 0;
            return (suf[0] == 'E' ? 950 : 900) + k;
        }
        public static List<VoiceBank.Line> AsVoiceLines(string scene, int from = 1, int to = 99)
        {
            var l = new List<VoiceBank.Line>();
            foreach (var x in SceneLines(scene))
            {
                int n = VenusDialogue.LineNumber(x.id);
                if (n < from || n > to) continue;
                l.Add(new VoiceBank.Line { Id = x.id, Speaker = x.speaker, Text = x.text, Scene = 200, PauseAfter = 0.45f, Clip = Resources.Load<AudioClip>("VenusVoice/" + x.id), Label = x.label });
            }
            return l;
        }
    }
}
