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
        public static bool PlayerSim => CatFollowTest > 0 || Playtest > 0 || SceneSelectTest || VenusPlay || VenusPlay2 || EuropaPlay || MarsPlay || QF20Check || QF23Revisit || AstPlay || BiaPlay || CatPlay || FinPlay;
        /// <summary>QF10: -venusplay = a player-style Venus run on a virtual gamepad (B01 -> C05, the telekinesis bridge, a hint
        /// and a skip), <see cref="VenusPlaytest"/>. -venushintfast = the player fallback's hint after 12 s / skip offer after
        /// 30 s instead of 45 s / 2 min (so the check can show both quickly).</summary>
        public static bool VenusPlay { get; private set; }
        /// <summary>QF11: -venusselect &lt;id&gt; = start like the Scene Select entry (C01 B01 B02 B03 C03 C04 C05 C06 B06 C09 C11);
        /// -venusselecttest = every Venus entry in turn, a capture of each where it starts; -menuvenusconfirm (with -menu) = the
        /// "Start Chapter 2 over?" confirm on the menu shot.</summary>
        public static string VenusSelect { get; set; }
        public static bool VenusSelectTest { get; private set; }
        public static bool MenuVenusConfirm { get; private set; }
        /// <summary>Round 12: -novideo = the vision videos count as unavailable (the still fallback); -venusstopatship = the
        /// Venus check run ends at the saucer (no debrief); -venusquick2 = stop after C05 as Round 11 did.</summary>
        public static bool NoVideo { get; private set; }
        /// <summary>QF21: -noqf21fix = 0.16.0's fixed shots (no automatic follow of a walker) - a repro switch; -qf21blades = the
        /// arc-blade body check run (idle / walk / run / between strikes, from the player's camera).</summary>
        public static bool NoQf21Fix { get; private set; }
        public static bool Qf21Blades { get; private set; }
        public static bool VenusStopAtShip { get; private set; }
        public static bool Quick2 { get; private set; }
        /// <summary>Round 12: -venusplay2 = the player-style run of Venus part 2 (VenusPlaytest2), from the observation area to
        /// the destination screen.</summary>
        public static bool VenusPlay2 { get; private set; }
        /// <summary>Round 12 (2d): -openingshot = Chapter 1 with the opening shot, captured, stops in the cell.</summary>
        public static bool OpeningShot { get; private set; }
        public static bool VenusHintFast { get; private set; }
        /// <summary>QF30 (9): -objviews = a capture from the player's camera as each objective appears (every chapter; the finale always).</summary>
        public static bool ObjViews { get; private set; }
        /// <summary>Round 13 check switches: -r13 bridge|tremor|encounter|debrief|console|consolefresh|reactions (each a short
        /// focused run; see TODO.md "Round 13 - status"). Venus ones start at their Scene Select entry.</summary>
        public static string R13 { get; private set; }
        /// <summary>QF12: -menupick "Label|Label|..." = a check run that starts from the real main menu: MenuPilot presses
        /// through the menu on a virtual gamepad (d-pad down to the item whose label starts with each part, then A; a
        /// confirm is answered with A). Combine with -venusplay2 (player-style) or alone (the arrival plays on autopilot).</summary>
        public static string MenuPick { get; private set; }
        /// <summary>QF14: -menupages (with -capture, implies -menu) = every menu page captured off-screen: the main menu, each
        /// chapter page, the longest one scrolled to the bottom, Settings, Controls, the new-game confirm and the pause menu's
        /// Scene select pages (all chapters unlocked on test keys for this run only), then quit.</summary>
        public static bool MenuPages { get; private set; }
        /// <summary>QF12: -stopafter &lt;seconds&gt; = end a check run after that many seconds of game time (the log is written).</summary>
        public static float StopAfter { get; private set; }
        /// <summary>Round 14: -europacp &lt;n&gt; = start the Europa scene at checkpoint n (EuropaLayout.Cp*; check runs: the state a
        /// real playthrough has there); -europa = start the chapter as from the star map (the chair scene aboard, then the dive);
        /// -europarace = the check run rides the optional race; -europaplay = the player-style run (EuropaPlaytest).</summary>
        public static int EuropaCp { get; private set; } = -1;
        public static bool Europa { get; private set; }
        public static bool EuropaRace { get; private set; }
        public static bool EuropaPlay { get; private set; }
        /// <summary>Round 15: -marscp &lt;n&gt; = start the Mars scene at checkpoint n (MarsLayout.Cp*); -mars = start the chapter as
        /// from the star map (the chair scene aboard, then the landing); -marsplay = the player-style run (MarsPlaytest);
        /// -marspick &lt;id&gt; = what a check run picks on the star map (europa / asteroid / biaveh); -routes europa|combined =
        /// fake the world flags for the departure-routing checks.</summary>
        public static int MarsCp { get; private set; } = -1;
        public static bool Mars { get; private set; }
        public static bool MarsPlay { get; private set; }
        /// <summary>Round 16 - the Asteroid: -astcp n (a check run at checkpoint n; 9 = aboard), -asteroid (as from the star
        /// map), -astplay (the player-style run AsteroidPlaytest: from TRV_AST on the console to AST_11).</summary>
        public static int AstCp { get; private set; } = -1;
        /// <summary>Round 17: -biacp n (check run at the mothership's checkpoint n; 0 = BIA_01 aboard, 7 = home), -bia (start the
        /// chapter as from the star map), -biaplay (the player-style run BiavehPlaytest).</summary>
        public static int BiaCp { get; private set; } = -1;
        public static bool Bia { get; private set; }
        public static bool BiaPlay { get; private set; }
        /// <summary>Round 18: -catcp n (check run at Cattalus checkpoint n; 0 = CAT_01 aboard, 6 = home), -cattalus (as from the star map),
        /// -catplay (the player-style run), -catrace (the check run's autopilot races; on by default in check runs), -catlose (the runner-up path).</summary>
        public static int CatCp { get; private set; } = -1;
        /// <summary>Round 19: -fincp n (check run at checkpoint n of the Moon finale; 0 = FIN_01 aboard), -finale (as from the star
        /// map), -finplay (the player-style run: free aboard -> the console -> the star map -> the finale -> the credits), -finretry
        /// (the confrontation run lets her go down once, then retries).</summary>
        public static int FinCp { get; private set; } = -1;
        public static bool Finale { get; private set; }
        public static bool FinPlay { get; private set; }
        public static bool FinRetry { get; private set; }
        public static bool Cattalus { get; private set; }
        public static bool CatPlay { get; private set; }
        public static bool CatLose { get; private set; }
        /// <summary>QF28 (5): -qf28holds = a short Cattalus run that poses 7 with every hand-held rune (close-ups + HOLD lines), then quits.</summary>
        public static bool QF28Holds { get; private set; }
        public static bool CatRace { get; private set; }
        public static bool Asteroid { get; private set; }
        public static bool AstPlay { get; private set; }
        public static bool AstBird { get; private set; }
        public static bool BiaBlades { get; private set; }   // QF23 (21): -biablades - the mothership with the arc blades owned      // -astbird: the check run takes the optional bird scene
        /// <summary>QF16: -qf16check = aboard free exploration: every ambient cat moment, then the passage to each world + window views.</summary>
        public static bool QF16Check { get; private set; }
        /// <summary>QF20 (1): Dan's save rebuilt on the test keys, free exploration aboard, one real X press at the console.</summary>
        public static bool QF20Check { get; private set; }
        public static bool QF23Revisit { get; private set; }
        public static string MarsPick { get; private set; }
        public static string Routes { get; private set; }
        /// <summary>QF15 (2): -europawalltest = the automated wall test (EuropaWallTest; starts at checkpoint 2 unless -europacp is given).</summary>
        public static bool EuropaWallTest { get; private set; }
        /// <summary>QF29 (1): -europadivetest = can a player get stuck at the start of the ice passage (EuropaDiveTest; starts at Europa's arrival).</summary>
        public static bool EuropaDiveTest { get; private set; }
        public static bool NoQF29Fix { get; private set; }
        /// <summary>QF29 (2): a check run's fixed game-time step per frame (-nodeterministic = real time, as before).</summary>
        public const float CaptureStep = 1f / 50f;
        public static bool NoDeterministic { get; private set; }
        public static bool Deterministic => CaptureDir != null && !NoDeterministic;
        /// <summary>QF17: -bossidle = the idle test - in a boss fight nobody presses anything (he must put her down within ~60 s),
        /// the run ends at her first knockdown. -bosscheck = the boss check: a run stops right after the fight.</summary>
        public static bool BossIdle { get; private set; }
        /// <summary>QF31 (10): the stun-reach proof - n forced stuns in the Moon finale's fight, the pilot walks in and strikes each time.</summary>
        public static int StunTest { get; private set; }
        /// <summary>QF31: capture spec for raw memory clips / montage stills ("shot=name:secs,..."; see ClipRecorder).</summary>
        public static string MemClips { get; private set; }
        public static string RecStills { get; private set; }
        public static bool BossCheck { get; private set; }
        /// <summary>QF17: -bossnovice = the pilot plays like a first-time player (the first-time fight length).</summary>
        public static bool BossNovice { get; private set; }
        /// <summary>QF20 (3): the pilot lets her go down once mid-fight (the retry keeps his energy).</summary>
        public static bool BossDownOnce { get; private set; }
        /// <summary>QF17 (5): -holdcheck = the bike-hold check only (the run stops once she is down the beam on the bike).</summary>
        public static bool HoldCheck { get; private set; }
        /// <summary>QF17 (4): -rampcheck = ride every ramp of the world with the riding camera (RampCheck).</summary>
        public static bool RampCheck { get; private set; }
        /// <summary>QF18 (10): -landwalltest = the land bike + on-foot wall test in Mars (LandWallTest).</summary>
        public static bool LandWallTest { get; private set; }

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
                if (args[i] == "-menupages") { MenuPages = true; MenuShot = true; }
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
                if (args[i] == "-objviews") ObjViews = true;
                if (args[i] == "-venusselect" && i + 1 < args.Length) VenusSelect = args[i + 1];
                if (args[i] == "-venusselecttest") { VenusSelectTest = true; VenusSelect = "C01"; }
                if (args[i] == "-menuvenusconfirm") MenuVenusConfirm = true;
                if (args[i] == "-novideo") NoVideo = true;
                if (args[i] == "-noqf21fix") NoQf21Fix = true;
                if (args[i] == "-qf21blades") Qf21Blades = true;
                if (args[i] == "-venusstopatship") VenusStopAtShip = true;
                if (args[i] == "-venusquick2") Quick2 = true;
                if (args[i] == "-r13" && i + 1 < args.Length)
                {
                    R13 = args[i + 1];
                    if (R13 == "bridge") { VenusSelect = "B03"; }
                    if (R13 == "tremor") { VenusSelect = "B02"; }
                    if (R13 == "temple") { VenusSelect = "C03"; }
                    if (R13 == "encounter") { VenusSelect = "C06"; }
                }
                if (args[i] == "-venusplay2") { VenusPlay2 = true; VenusSelect = "C06"; }
                if (args[i] == "-menupick" && i + 1 < args.Length) MenuPick = args[i + 1];
                if (args[i] == "-europacp" && i + 1 < args.Length && int.TryParse(args[i + 1], out var ecp)) EuropaCp = ecp;
                if (args[i] == "-europa") Europa = true;
                if (args[i] == "-europarace") EuropaRace = true;
                if (args[i] == "-europaplay") { EuropaPlay = true; Europa = true; }
                if (args[i] == "-marscp" && i + 1 < args.Length && int.TryParse(args[i + 1], out var mcp)) MarsCp = mcp;
                if (args[i] == "-mars") Mars = true;
                if (args[i] == "-marsplay") { MarsPlay = true; Mars = true; }
                if (args[i] == "-astcp" && i + 1 < args.Length && int.TryParse(args[i + 1], out var acp)) AstCp = acp;
                if (args[i] == "-biacp" && i + 1 < args.Length && int.TryParse(args[i + 1], out var bcp)) BiaCp = bcp;
                if (args[i] == "-bia") Bia = true;
                if (args[i] == "-biaplay") BiaPlay = true;
                if (args[i] == "-catcp" && i + 1 < args.Length && int.TryParse(args[i + 1], out var ccp)) CatCp = ccp;
                if (args[i] == "-cattalus") Cattalus = true;
                if (args[i] == "-catplay") CatPlay = true;
                if (args[i] == "-catlose") CatLose = true;
                if (args[i] == "-fincp" && i + 1 < args.Length && int.TryParse(args[i + 1], out var fcp)) FinCp = fcp;
                if (args[i] == "-finale") Finale = true;
                if (args[i] == "-finplay") FinPlay = true;
                if (args[i] == "-finretry") { FinRetry = true; BossDownOnce = true; }
                if (args[i] == "-qf28holds") QF28Holds = true;
                if (args[i] == "-catrace") CatRace = true;
                if (args[i] == "-asteroid") Asteroid = true;
                if (args[i] == "-astbird") AstBird = true;
                if (args[i] == "-biablades") BiaBlades = true;
                if (args[i] == "-astplay") { AstPlay = true; Asteroid = true; }
                if (args[i] == "-qf16check") { QF16Check = true; Chapter1 = true; }
                if (args[i] == "-qf20check") { QF20Check = true; Chapter1 = true; }
                if (args[i] == "-qf23revisit") { QF23Revisit = true; Chapter1 = true; }
                if (args[i] == "-marspick" && i + 1 < args.Length) MarsPick = args[i + 1];
                if (args[i] == "-routes" && i + 1 < args.Length) Routes = args[i + 1];
                if (args[i] == "-europawalltest") EuropaWallTest = true;
                if (args[i] == "-europadivetest") EuropaDiveTest = true;
                if (args[i] == "-noqf29fix") NoQF29Fix = true;
                if (args[i] == "-nodeterministic") NoDeterministic = true;
                if (args[i] == "-bossidle") { BossIdle = true; BossCheck = true; }
                if (args[i] == "-stuntest" && i + 1 < args.Length && int.TryParse(args[i + 1], out var stn)) { StunTest = stn; BossCheck = true; }
                if (args[i] == "-memclips" && i + 1 < args.Length) MemClips = args[i + 1];
                if (args[i] == "-recstills" && i + 1 < args.Length) RecStills = args[i + 1];
                if (args[i] == "-bosscheck") BossCheck = true;
                if (args[i] == "-bossnovice") BossNovice = true;
                if (args[i] == "-bossdownonce") BossDownOnce = true;
                if (args[i] == "-holdcheck") HoldCheck = true;
                if (args[i] == "-rampcheck") RampCheck = true;
                if (args[i] == "-landwalltest") LandWallTest = true;
                if (args[i] == "-stopafter" && i + 1 < args.Length && float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var sa)) StopAfter = sa;
                if (args[i] == "-openingshot") { OpeningShot = true; Chapter1 = true; }
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
            if (EuropaWallTest && EuropaCp < 0) EuropaCp = EuropaLayout.CpSettlement;
            if (EuropaDiveTest && EuropaCp < 0) EuropaCp = 0;
            if (CaptureDir == null) TestSpeed = 1f;          // only ever for automated check runs
            if (TestSpeed != 1f)
            {
                Time.timeScale = TestSpeed;
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => { if (!PauseMenu.IsOpen) Time.timeScale = TestSpeed; };
            }
            // QF29 (2): deterministic check runs - game time advances a fixed 1/50 s per rendered frame (whatever the PC's speed) and the
            // random generator is seeded on every scene load, so a capture shows the same frame every run (a shot passes or fails, not both)
            if (CaptureDir != null && !NoDeterministic)
            {
                Time.captureDeltaTime = CaptureStep;
                UnityEngine.Random.InitState(29);
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => UnityEngine.Random.InitState(29 + s.buildIndex);
            }
            bool forceFullscreen = CaptureDir == null && !WindowFromCommandLine && !Application.isEditor;
            FullscreenToggle.Install(forceFullscreen);
            Debug.Log($"[Seven] boot: muted={Muted} listenerVolume={AudioListener.volume} capture={CaptureDir ?? "-"} fight={Fight} saucer={Saucer} ratwing={Ratwing} chapter1={Chapter1} forceFullscreen={forceFullscreen} mode={Screen.fullScreenMode}");
        }
    }
}
