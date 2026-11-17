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
                if (args[i] == "-soundcheck") SoundCheck = true;
                if (args[i] == "-quick") Quick = true;
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
            bool forceFullscreen = CaptureDir == null && !WindowFromCommandLine && !Application.isEditor;
            FullscreenToggle.Install(forceFullscreen);
            Debug.Log($"[Seven] boot: muted={Muted} listenerVolume={AudioListener.volume} capture={CaptureDir ?? "-"} fight={Fight} saucer={Saucer} ratwing={Ratwing} chapter1={Chapter1} forceFullscreen={forceFullscreen} mode={Screen.fullScreenMode}");
        }
    }
}
