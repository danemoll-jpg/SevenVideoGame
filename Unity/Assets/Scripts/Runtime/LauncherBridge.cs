using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 8: the game's side of the auto-update launcher (Launcher/, SevenLauncher.exe; Docs/LAUNCHER.md).
    /// The launcher starts the game as
    ///   SevenDemo.exe -sevenLauncherReady &lt;file&gt; -sevenLauncherNonce &lt;text&gt;
    /// and waits (up to 90 s) for this script to write the nonce into the ready file once the first scene is up and has rendered.
    /// That tells the launcher the start worked; if it never arrives (or the game exits early with an error) the launcher shows
    /// "Startup did not complete" and offers Retry / Restore previous version. The launcher's own self-check adds -mute -batchmode -logFile.
    /// Started directly (no -sevenLauncherReady) nothing happens. Self-contained: installs itself, needs nothing in any scene.
    /// <see cref="Version"/> is this build's release version; the builder writes it to version.txt beside the exe and
    /// Tools/Prepare-LauncherRelease.py refuses to package a build whose version.txt differs from its --version.
    /// </summary>
    public sealed class LauncherBridge : MonoBehaviour
    {
        /// <summary>Release version of this build: 0.&lt;round&gt;.&lt;fix&gt;. Raise it every round before the final build.</summary>
        public const string Version = "0.9.0";
        /// <summary>Release build number: round * 100 + fix (0.8.0 = 800). Must rise with every published release.</summary>
        public const int Build = 900;

        const string ReadyArg = "-sevenLauncherReady", NonceArg = "-sevenLauncherNonce";

        public static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 1; i + 1 < args.Length; i++) if (args[i] == key) return args[i + 1];
            return null;
        }

        /// <summary>True when the game was started by SevenLauncher.exe.</summary>
        public static bool FromLauncher => Argument(ReadyArg) != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartBridge()
        {
            Debug.Log($"[Launcher] 7: The Observer {Version} (build {Build}){(FromLauncher ? ", started by the launcher" : "")}");
            if (!FromLauncher) return;
            var go = new GameObject("Launcher startup handshake");
            DontDestroyOnLoad(go);
            go.AddComponent<LauncherBridge>();
        }

        IEnumerator Start()
        {
            var path = Argument(ReadyArg); var nonce = Argument(NonceArg);
            if (string.IsNullOrEmpty(nonce) || nonce.Length > 128 || string.IsNullOrEmpty(path) || !Path.IsPathRooted(path)) { Destroy(gameObject); yield break; }
            // The first scene is loaded (AfterSceneLoad); let it run a few frames and render once, so a start that crashes on its first frames is not reported as working.
            for (int i = 0; i < 5; i++) yield return null;
            if (!Application.isBatchMode) yield return new WaitForEndOfFrame();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, nonce);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
                Debug.Log("[Launcher] ready signal written");
            }
            catch (Exception e) { Debug.LogWarning("[Launcher] ready signal unavailable: " + e.Message); }
            Destroy(gameObject);
        }
    }
}
