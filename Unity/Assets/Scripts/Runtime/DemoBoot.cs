using System;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Start-up switches, read before the first scene loads.
    ///   -mute            AudioListener.volume = 0 before any sound can play (every automated run uses it).
    ///   -room            start in the Round 1 training room (FightDemo_Room) instead of the outdoor world.
    ///   -fight / -saucer / -ratwing   skip the demo picker and start that demo (check runs use these).
    ///   -gallery         with -capture: the Round 5 gallery TV check run on the saucer (implies -saucer).
    ///   -menu            with -capture: screenshot the demo picker and quit.
    ///   -capture <dir>   scripted check run: plays a fixed sequence, writes screenshots + capture_log.txt, quits.
    ///                    Never forces fullscreen.
    ///   -screen-* / -window-mode / -popupwindow   window set on the command line: no forced fullscreen.
    /// Normal launch (none of the above): borderless fullscreen at native resolution.
    /// </summary>
    public static class DemoBoot
    {
        public static bool Muted { get; private set; }
        public static string CaptureDir { get; private set; }
        public static bool Room { get; private set; }
        public static bool Fight { get; private set; }
        public static bool Saucer { get; private set; }
        public static bool Ratwing { get; private set; }
        public static bool MenuShot { get; private set; }
        public static bool Quick { get; private set; }
        public static bool Gallery { get; private set; }
        public static bool WindowFromCommandLine { get; private set; }
        /// <summary>-chapter1: start Chapter 1 "The Moon Prison". -ch1aboard: start at its saucer part (scenes 9-11).</summary>
        public static bool Chapter1 { get; private set; }
        public static bool Chapter1Aboard { get; private set; }
        /// <summary>-soundcheck: the one short unmuted telepathy sound check (plays one cat line, measures, quits).</summary>
        public static bool SoundCheck { get; private set; }
        /// <summary>-soundcheck sfx: Round 7's short unmuted check of a few sound effects instead of the telepathy line.</summary>
        public static bool SoundCheckSfx { get; private set; }
        /// <summary>Round 10: -soundcheck galley = a few new scene 10 lines + the eating sounds.</summary>
        public static bool SoundCheckGalley { get; private set; }
        /// <summary>-continue: start like the main menu's Continue (exploring the saucer after Chapter 1).</summary>
        public static bool Continue { get; private set; }
        /// <summary>-cityreveal primary|alternate|off: which 6b dialogue set the city reveal plays (default primary).</summary>
        public static string CityRevealDialogue { get; private set; } = "primary";
        /// <summary>-fightcombo: fight check run that records input timing and a combo sequence (Round 8).</summary>
        public static bool FightCombo { get; private set; }
        /// <summary>-openingtest n (with -capture): QF4 check - run Chapter 1's opening n times with varied timing / 7's
        /// position and movement / frame hitches; every run must reach scene 1 with the cat never inside the cell.</summary>
        public static int OpeningTest { get; private set; }
        /// <summary>-catfollowtest n (with -capture): QF5 check - n runs from the end of the cell rescue out of the cell
        /// block and up into the old passage, 7 driven like a player (virtual gamepad, free camera, varied paths).</summary>
        public static int CatFollowTest { get; private set; }
        /// <summary>QF6 -playtest n (with -capture): n full-chapter runs from the main menu to "Set course" in the galley,
        /// played like a person (virtual gamepad on the real input actions, menus included, no autopilot, no teleports).</summary>
        public static int Playtest { get; private set; }
        /// <summary>QF6 -sceneselecttest (with -capture): every Scene Select entry started from the menu and played (like a
        /// person) until the next entry's checkpoint is reached.</summary>
        public static bool SceneSelectTest { get; private set; }
        /// <summary>QF8: -camsweep = the follow-camera sweep over the whole traversal route (Chapter 1, ~2 min).</summary>
        public static bool CamSweep { get; private set; }
        /// <summary>QF9: -flickertest = door frames measured frame by frame in motion (Chapter 1, ~25 s).</summary>
        public static bool FlickerTest { get; private set; }
        /// <summary>QF9: -fightonly = a check run from checkpoint 6 that stops when the city reveal starts.</summary>
        public static bool FightOnly { get; private set; }
        /// <summary>Round 11: -chapter2 = start Chapter 2 (Venus) at its save (a fresh start in check runs);
        /// -venusskip = check run skipping every line; -venusreload = reload after every committed scene.</summary>
        public static bool Chapter2 { get; private set; }
        public static bool VenusSkip { get; private set; }
        public static bool VenusReload { get; private set; }
        /// <summary>Round 11: -venuschain = a check run aboard that sets course and goes on into Chapter 2 (as in play).</summary>
        public static bool VenusChain { get; private set; }
        /// <summary>Round 11: -venusstatetest = check the Venus progression / persistence rules, write venus_state_test.txt, quit.</summary>
        public static bool VenusStateTest { get; private set; }
        /// <summary>-playtestseed n: offset for the playtest's per-run seeds (repeat a run).</summary>
        public static int PlaytestSeed { get; private set; }
        /// <summary>-playtestfrom k: start at run k+1 (same seeds / habits as in the full test - e.g. repeat one run).</summary>
        public static int PlaytestFrom { get; private set; }
        /// <summary>QF6 -testspeed x (with -capture only): game time runs x times faster (Time.timeScale) so automated
        /// checks stay short (standing rule: under ~20 min of testing per round). Default 1.</summary>
        public static float TestSpeed { get; private set; } = 1f;
        /// <summary>QF5 cat-follow test / QF6 playtest: the chapter runs as for a person (no autopilot, no check-run shortcuts).</summary>
        public static bool PlayerSim => CatFollowTest > 0 || Playtest > 0 || SceneSelectTest || VenusPlay;
        /// <summary>QF10: -venusplay = a player-style Venus run on a virtual gamepad (B01 -> C05, the telekinesis bridge, a hint
        /// and a skip), <see cref="VenusPlaytest"/>. -venushintfast = the player fallback's hint after 12 s / skip offer after
        /// 30 s instead of 45 s / 2 min (so the check can show both quickly).</summary>
        public static bool VenusPlay { get; private set; }
        public static bool VenusHintFast { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-mute") Muted = true;
                if (args[i] == "-room") Room = true;
                if (args[i] == "-fight") Fight = true;
                if (args[i] == "-saucer") Saucer = true;
                if (args[i] == "-ratwing") Ratwing = true;
                if (args[i] == "-menu") MenuShot = true;
                if (args[i] == "-chapter1") Chapter1 = true;
                if (args[i] == "-ch1aboard") { Chapter1 = true; Chapter1Aboard = true; }
                if (args[i] == "-soundcheck") { SoundCheck = true; if (i + 1 < args.Length && args[i + 1] == "sfx") SoundCheckSfx = true; if (i + 1 < args.Length && args[i + 1] == "galley") SoundCheckGalley = true; }
                if (args[i] == "-continue") Continue = true;
                if (args[i] == "-ch1cp" && i + 1 < args.Length && int.TryParse(args[i + 1], out var cp)) { Chapter1 = true; Chapter1State.Checkpoint = cp; }
                if (args[i] == "-fightcombo") { FightCombo = true; Fight = true; }
                if (args[i] == "-cityreveal" && i + 1 < args.Length) CityRevealDialogue = args[i + 1];
                if (args[i] == "-quick") Quick = true;
                if (args[i] == "-openingtest" && i + 1 < args.Length && int.TryParse(args[i + 1], out var ot)) { OpeningTest = ot; Chapter1 = true; }
                if (args[i] == "-catfollowtest" && i + 1 < args.Length && int.TryParse(args[i + 1], out var cf)) { CatFollowTest = cf; Chapter1 = true; Chapter1State.Checkpoint = 1; }
                if (args[i] == "-noqf5catfix") ChapterCat.QF5Fix = false;
                if (args[i] == "-playtest" && i + 1 < args.Length && int.TryParse(args[i + 1], out var pt)) Playtest = pt;
                if (args[i] == "-playtestfrom" && i + 1 < args.Length && int.TryParse(args[i + 1], out var pf)) PlaytestFrom = pf;
                if (args[i] == "-playtestseed" && i + 1 < args.Length && int.TryParse(args[i + 1], out var ps)) PlaytestSeed = ps;
                if (args[i] == "-sceneselecttest") SceneSelectTest = true;
                if (args[i] == "-venusstatetest") VenusStateTest = true;
                if (args[i] == "-noqf9fix") CameraOccluders.QF9Fix = false;
                if (args[i] == "-chapter2") Chapter2 = true;
                if (args[i] == "-venusplay") { VenusPlay = true; Chapter2 = true; }
                if (args[i] == "-venushintfast") VenusHintFast = true;
                if (args[i] == "-venuschain") VenusChain = true;
                if (args[i] == "-venusskip") { VenusSkip = true; Chapter2 = true; }
                if (args[i] == "-venusreload") { VenusReload = true; Chapter2 = true; }
                if (args[i] == "-fightonly") { FightOnly = true; Chapter1 = true; Chapter1State.Checkpoint = 6; }
                if (args[i] == "-flickertest") { FlickerTest = true; Chapter1 = true; Chapter1State.Checkpoint = 6; }
                if (args[i] == "-camsweep") { CamSweep = true; Chapter1 = true; Chapter1State.Checkpoint = 2; }
                if (args[i] == "-noqf6fix") Chapter1Director.QF6Fix = false;
                if (args[i] == "-testspeed" && i + 1 < args.Length && float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ts)) TestSpeed = Mathf.Clamp(ts, 1f, 4f);
                if (args[i] == "-gallery") { Gallery = true; Saucer = true; }
                if (args[i] == "-capture" && i + 1 < args.Length) CaptureDir = args[i + 1];
                if (args[i].StartsWith("-screen-") || args[i] == "-window-mode" || args[i] == "-popupwindow") WindowFromCommandLine = true;
            }
            if (Muted)
            {
                AudioListener.volume = 0f;
                AudioListener.pause = true;
            }
            if (CaptureDir != null) Application.runInBackground = true;
            if (VenusStateTest && CaptureDir != null)
            {
                var fails = VenusState.SelfTest();
                System.IO.Directory.CreateDirectory(CaptureDir);
                System.IO.File.WriteAllText(System.IO.Path.Combine(CaptureDir, "venus_state_test.txt"),
                    (fails.Count == 0 ? "VENUS STATE TEST: all rules hold (forward-only stages, scenes once, record once, reload keeps all)" : "VENUS STATE TEST FAILED: " + string.Join(" | ", fails)) + System.Environment.NewLine);
                Application.Quit();
            }
            if (CaptureDir == null) TestSpeed = 1f;          // only ever for automated check runs
            if (TestSpeed != 1f)
            {
                Time.timeScale = TestSpeed;
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => { if (!PauseMenu.IsOpen) Time.timeScale = TestSpeed; };
            }
            bool forceFullscreen = CaptureDir == null && !WindowFromCommandLine && !Application.isEditor;
            FullscreenToggle.Install(forceFullscreen);
            Debug.Log($"[Seven] boot: muted={Muted} listenerVolume={AudioListener.volume} capture={CaptureDir ?? "-"} fight={Fight} saucer={Saucer} ratwing={Ratwing} chapter1={Chapter1} forceFullscreen={forceFullscreen} mode={Screen.fullScreenMode}");
        }
    }
}
