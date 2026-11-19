using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 13 check runs for the ship's console (-capture dir, muted, off-screen like every check run):
    ///   -r13 console       a test save with Venus finished incl. VEN_C12 (Log + Field guide filled, Mars + Europa on the map)
    ///   -r13 consolefresh  a test save with only Chapter 1 (Venus not reached: the Moon + Venus only, "The cat")
    /// Starts the Saucer scene in free exploration (Chapter 1's Continue start, test keys seven_test_* only - never Dan's
    /// saves, removed again at the end), walks 7 to the console, uses it and captures: the console from the deck, 7 standing
    /// at it, the main menu, the Log, the Field guide list + one entry, the star map with Venus selected, Settings (a setting
    /// changed and checked: before / after value + the bus gain, then put back); "console" also shows Mars's coming-soon card.
    /// Leaves and quits (about a minute).
    /// </summary>
    public class ConsoleCaptureRun : MonoBehaviour
    {
        public static bool Active { get; private set; }
        static string _mode;
        int _fails;
        ChapterCapture Cap => ChapterCapture.I;

        [System.Serializable]
        class VenusSaveMirror      // same fields as VenusState's private save data (JSON in PlayerPrefs "seven_test_venus")
        {
            public int stage = -1;
            public List<string> devicesDisabled = new List<string>();
            public bool wormmasterContained;
            public bool recordAwarded;
            public List<string> conversationsDone = new List<string>();
            public List<string> gardenAssistance = new List<string>();
            public Vector3 bikeAt; public float bikeYaw; public bool bikeKnown; public bool riding;
            public int recordAwards;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Active || DemoBoot.CaptureDir == null || (DemoBoot.R13 != "console" && DemoBoot.R13 != "consolefresh")) return;
            Active = true; _mode = DemoBoot.R13;
            // the menu would load the fight demo for a bare -capture: this run takes over before its Start
            var menu = FindFirstObjectByType<DemoMenu>();
            if (menu != null) menu.enabled = false;
            var go = new GameObject("ConsoleCaptureRun");
            DontDestroyOnLoad(go);
            go.AddComponent<ConsoleCaptureRun>();
            // test saves only (DemoBoot.CaptureDir set: SaveGame / VenusState use the seven_test_* keys)
            SaveGame.ClearTestSave(); VenusState.ClearTestSave();
            SaveGame.SaveProgress(Chapter1State.CpExplore);
            if (_mode == "console")
            {
                var d = new VenusSaveMirror { stage = (int)VenusState.Stage.Complete, wormmasterContained = true, recordAwarded = true, recordAwards = 1 };
                d.devicesDisabled.AddRange(VenusLayout.DeviceIds);
                d.conversationsDone.AddRange(new[] { "VEN_C01", "VEN_B01", "VEN_B02", "VEN_C02", "VEN_B03", "VEN_B03_bridge", "VEN_B04", "VEN_C03", "VEN_C04", "VEN_B05_lines",
                    "VEN_C05", "VEN_WAYDOWN", "VEN_C06", "VEN_C07", "VEN_B06", "VEN_C08", "VEN_C09", "VEN_C10", "VEN_C12", "VEN_B07", "VEN_C11", "VEN_C13" });
                PlayerPrefs.SetString("seven_test_venus", JsonUtility.ToJson(d));
                PlayerPrefs.Save();
            }
            VenusState.Load();
            ChapterCapture.Ensure();
            Cap0($"CONSOLE CHECK ({_mode}): test save - chapter 1 furthest {SaveGame.Furthest}; {VenusState.Summary()}");
            Chapter1State.StartAt(Chapter1State.CpExplore);
        }

        static void Cap0(string s) => ChapterCapture.I?.Log(s);

        IEnumerator Start()
        {
            float t0 = Time.realtimeSinceStartup;
            // wait for the saucer's free exploration and the console
            while ((Chapter1Aboard.I == null || !Chapter1Aboard.I.Explored || ShipConsole.I == null) && Time.realtimeSinceStartup - t0 < 40f) yield return null;
            var con = ShipConsole.I; var ab = Chapter1Aboard.I;
            if (con == null || ab == null || !ab.Explored)
            {
                Fail($"no console / exploration after {Time.realtimeSinceStartup - t0:0} s (console {(con != null)}, aboard {(ab != null)})");
                End(); yield break;
            }
            var dir = ab.Director; var seven = dir.Seven; var cam = dir.Cam;
            Cap.Attach(cam.GetComponent<Camera>());
            con.ScriptInput = true;
            yield return Wait(1.6f);                                   // the fade-in
            Cap.Log($"console: stand spot {con.StandSpot.position}, pilot seat {con.PilotSeat.position}, display {con.Display.position}, menu screen {con.MenuScreen.position}; " +
                    $"reachable [{string.Join(", ", ShipConsole.Reachable())}], ship at {ShipConsole.Here()}, Mars/Europa available {ShipConsole.MarsAvailable}/{ShipConsole.EuropaAvailable}");

            // 1. the console from the deck (wide, 7 in the foreground)
            var root = con.transform; var f = Flat(root.forward).normalized; var r = new Vector3(f.z, 0f, -f.x);
            cam.Hold = true;
            Place(cam.transform, root.position + f * 3.9f + r * -1.3f + Vector3.up * 1.65f, root.position + Vector3.up * 0.95f, 50f);
            yield return Wait(0.3f);
            yield return Cap.Shot("c01_console_from_the_deck");
            cam.Hold = false;

            // 2. walk up to it like a player; the context prompt
            seven.ScriptDriven = true;
            yield return con.WalkTo(con.ApproachPos, 15f);
            yield return con.WalkTo(con.StandSpot.position, 6f);
            yield return Wait(0.4f);
            Cap.Log($"console: 7 {Flat(seven.transform.position - con.StandSpot.position).magnitude:0.00} m from the stand spot, near {con.NearConsole}, context prompt '{dir.ExtraPrompt}' (legend '{dir.Legend?.ContextText}')");
            if (!con.NearConsole || dir.ExtraPrompt != "Use console") Fail("no 'Use console' prompt at the console");
            yield return Cap.Shot("c02_use_console_prompt");

            // 3. use it: she steps in, hands on the desk, the camera over her shoulder
            con.Use();
            float tu = Time.time;
            while (con.Current != ShipConsole.State.Using && Time.time - tu < 5f) yield return null;
            if (con.Current != ShipConsole.State.Using) { Fail("console did not open"); End(); yield break; }
            yield return Wait(0.3f);
            Cap.Log($"console: open; 7 at {seven.transform.position} facing {seven.transform.forward} (stand spot {Flat(seven.transform.position - con.StandSpot.position).magnitude:0.00} m), frozen {seven.Frozen}; " +
                    $"hands: left {(con.HandLeft != null ? Vector3.Distance(BonePos(seven, HumanBodyBones.LeftHand), con.HandLeft.position) : -1f):0.00} m, right {(con.HandRight != null ? Vector3.Distance(BonePos(seven, HumanBodyBones.RightHand), con.HandRight.position) : -1f):0.00} m from the rests");
            // 7 standing at it, seen from the side (the capture places the camera)
            con.CameraOverride = true;
            Place(cam.transform, con.StandSpot.position - Flat(con.StandSpot.forward) * 1.7f + RightOf(con.StandSpot) * 1.5f + Vector3.up * 1.45f, con.StandSpot.position + Flat(con.StandSpot.forward) * 0.45f + Vector3.up * 1.05f, 50f);
            yield return Wait(0.2f);
            yield return Cap.Shot("c03_seven_at_the_console");
            con.CameraOverride = false;
            yield return Wait(1.0f);
            Cap.Log("console: " + con.Describe());
            yield return Cap.Shot("c04_console_main_menu");

            // 4. Log
            con.OpenPage(ShipConsole.Page.Log);
            yield return Wait(0.4f);
            con.Nav(1, 0, false, false); con.Nav(1, 0, false, false);
            yield return Wait(0.3f);
            Cap.Log("console: " + con.Describe());
            int logN = ShipConsole.LogEntries().Count;
            if (logN == 0) Fail("Log is empty");
            yield return Cap.Shot("c05_log");
            con.Nav(0, 0, false, true);

            // 5. Field guide: the list + one entry (the cat first, then the next one)
            con.OpenPage(ShipConsole.Page.Guide);
            yield return Wait(0.4f);
            Cap.Log("console: " + con.Describe());
            yield return Cap.Shot("c06_field_guide_list");
            con.Nav(1, 0, false, false);
            yield return Wait(0.3f);
            Cap.Log("console: " + con.Describe());
            yield return Cap.Shot("c07_field_guide_entry");
            var guide = ShipConsole.GuideEntries();
            var cat = guide.Find(e => e.id == "cat");
            Cap.Log($"console: field guide - the cat is titled '{(cat != null ? ShipConsole.GuideTitle(cat) : "-")}' (Panther only after VEN_B01: {VenusState.SceneDone("VEN_B01")})");
            con.Nav(0, 0, false, true);

            // 6. Star map with Venus selected (+ the rules: only reachable places)
            con.OpenPage(ShipConsole.Page.Map);
            yield return Wait(con.EaseSeconds + 0.4f);
            con.Map.Select("venus");
            yield return Wait(0.3f);
            var shown = con.Map.ShownIds(); shown.Sort();
            var want = new List<string> { "moon", "venus" }; if (_mode == "console") { want.Add("mars"); want.Add("europa"); }
            want.Sort();
            bool mapOk = string.Join(",", shown) == string.Join(",", want);
            Cap.Log($"console: {con.Describe()} - STAR MAP {(mapOk ? "OK" : "WRONG")}: shown [{string.Join(", ", shown)}], expected [{string.Join(", ", want)}]");
            if (!mapOk) Fail("star map shows the wrong bodies");
            if (con.Map.Selected != "venus") Fail("Venus could not be selected");
            yield return Cap.Shot("c08_star_map_venus_selected");
            if (_mode == "console")
            {
                // Mars: set course -> the coming-soon card, back on the map
                con.Map.Select("mars");
                con.Nav(0, 0, true, false); yield return Wait(0.4f);
                yield return Cap.Shot("c09_star_map_mars_confirm");
                con.Nav(0, 0, true, false);
                yield return Wait(2.0f);
                yield return Cap.Shot("c10_coming_soon_mars");
                float tc = Time.time;
                while (con.Busy && con.Current != ShipConsole.State.Using && Time.time - tc < 10f) yield return null;
                yield return Wait(3.0f);
                Cap.Log($"console: after the Mars card - state {con.Current}, page {con.CurrentPage}");
            }
            con.Nav(0, 0, false, true);
            if (con.Map.Confirming) con.Nav(0, 0, false, true);
            yield return Wait(con.EaseSeconds + 0.2f);

            // 7. Settings: the pause menu's rows; change one, check it took effect, put it back
            con.OpenPage(ShipConsole.Page.Settings);
            yield return Wait(0.4f);
            Cap.Log("console: " + con.Describe());
            yield return Cap.Shot("c11_settings");
            con.SelectRow(GameSettings.Music);
            float before = GameSettings.Raw(GameSettings.Music), gainBefore = AudioKit.Gain(AudioKit.Bus.Music);
            con.Nav(0, -1, false, false);
            float after = GameSettings.Raw(GameSettings.Music), gainAfter = AudioKit.Gain(AudioKit.Bus.Music);
            bool took = Mathf.Abs(after - (before - 0.1f)) < 0.01f || (before <= 0f && after <= 0f);
            Cap.Log($"console: SETTING music {before:0.0} -> {after:0.0} (bus gain {gainBefore:0.00} -> {gainAfter:0.00}); pause menu row now '{GameSettings.Label(GameSettings.Music)}' - {(took ? "took effect" : "DID NOT TAKE EFFECT")}");
            if (!took) Fail("music volume change did not take effect");
            yield return Wait(0.2f);
            yield return Cap.Shot("c12_settings_music_lowered");
            bool hints0 = ControlsLegend.HintsOn;
            con.SelectRow(GameSettings.Hints); con.Nav(0, 0, true, false);
            bool hints1 = ControlsLegend.HintsOn;
            Cap.Log($"console: SETTING button hints {(hints0 ? "on" : "off")} -> {(hints1 ? "on" : "off")} - {(hints0 != hints1 ? "took effect" : "DID NOT TAKE EFFECT")}");
            if (hints0 == hints1) Fail("button hints switch did not take effect");
            con.Nav(0, 0, true, false);                                                     // back on
            con.SelectRow(GameSettings.Music); con.Nav(0, 1, false, false);                 // music back up
            Cap.Log($"console: settings restored - {GameSettings.Describe()}");
            con.Nav(0, 0, false, true);                                                     // back to the menu
            yield return Wait(0.3f);

            // 8. leave (B on the main menu)
            con.Nav(0, 0, false, true);
            float tl = Time.time;
            while (con.Current != ShipConsole.State.Off && Time.time - tl < 5f) yield return null;
            yield return Wait(0.6f);
            Cap.Log($"console: left - state {con.Current}, camera held {cam.Hold}, 7 frozen {seven.Frozen}, fov {cam.GetComponent<Camera>().fieldOfView:0}");
            if (con.Current != ShipConsole.State.Off || seven.Frozen) Fail("did not leave the console cleanly");
            yield return Cap.Shot("c13_after_leaving");
            End();
        }

        void End()
        {
            SaveGame.ClearTestSave(); VenusState.ClearTestSave(); PlayerPrefs.Save();
            Cap.Finish($"CONSOLE CHECK ({_mode}): {(_fails == 0 ? "OK" : _fails + " FAILED")} - log {ShipConsole.LogEntries().Count} entries, field guide {ShipConsole.GuideEntries().Count}, star map [{string.Join(", ", ShipConsole.Reachable())}] (test saves removed)");
        }

        void Fail(string s) { _fails++; Cap?.Log("CONSOLE FAIL: " + s); }

        static Vector3 BonePos(SaucerSeven s, HumanBodyBones b)
        {
            var a = s.GetComponent<Animator>();
            var t = a != null && a.isHuman ? a.GetBoneTransform(b) : null;
            return t != null ? t.position : s.transform.position;
        }
        static Vector3 RightOf(Transform t) { var f = Flat(t.forward).normalized; return new Vector3(f.z, 0f, -f.x); }
        static void Place(Transform cam, Vector3 pos, Vector3 look, float fov)
        {
            cam.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos));
            var c = cam.GetComponent<Camera>(); if (c != null) c.fieldOfView = fov;
        }
        static IEnumerator Wait(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    }
}
