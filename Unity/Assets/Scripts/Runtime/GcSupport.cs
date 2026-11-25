using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Seven
{
    /// <summary>Round 20: the racing mode's switches (read once). -racingunlocked = the mode open without finishing the game (tests);
    /// -gcproof (the AI, one of each racer, every track, 3 laps; -gconly a,b = those tracks), -gcsplit (a 2-player split race: two virtual pads driven by the bot
    /// + 4 AI), -gcmenu (every setup page captured), -gctrial (a time trial, then a second with its ghost), -gcside (side by side).
    /// Test runs keep their own save keys (never the player's).</summary>
    public static class GcBoot
    {
        static bool _read; static bool _unlocked, _proof, _split, _menu, _trial, _side, _quick, _players, _music; static float _speed = 1f; static string _only;
        static void Read()
        {
            if (_read) return; _read = true;
            var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] == "-racingunlocked") _unlocked = true;
                if (a[i] == "-gcproof") _proof = true;
                if (a[i] == "-gcsplit") _split = true;
                if (a[i] == "-gcmenu") _menu = true;
                if (a[i] == "-gctrial") _trial = true;
                if (a[i] == "-gcside") _side = true;
                if (a[i] == "-gcquick") _quick = true;
                if (a[i] == "-gcplayers") _players = true;
                if (a[i] == "-gcmusictest") _music = true;
                if (a[i] == "-gconly" && i + 1 < a.Length) _only = a[i + 1];
                if (a[i] == "-testspeed" && i + 1 < a.Length) float.TryParse(a[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _speed);
            }
        }
        public static bool Unlocked { get { Read(); return _unlocked; } }
        public static bool Proof { get { Read(); return _proof && DemoBoot.CaptureDir != null; } }
        public static bool Split { get { Read(); return _split && DemoBoot.CaptureDir != null; } }
        public static bool MenuShots { get { Read(); return _menu && DemoBoot.CaptureDir != null; } }
        public static bool Trial { get { Read(); return _trial && DemoBoot.CaptureDir != null; } }
        public static bool Side { get { Read(); return _side; } }
        public static bool Quick { get { Read(); return _quick; } }
        /// <summary>Round 21: -gcplayers = each racer once as the player (the human-like bot through a virtual pad) against the AI.</summary>
        public static bool Players { get { Read(); return _players && DemoBoot.CaptureDir != null; } }
        /// <summary>Round 21: -gconly venus,mars = the proof on those tracks only (iteration).</summary>
        public static string[] Only { get { Read(); return string.IsNullOrEmpty(_only) ? null : _only.Split(','); } }
        /// <summary>QF36: -gcmusictest = every Grand Circuit song: its intro, the hand-over into the loop body and the body's wrap (seeks near each seam).</summary>
        public static bool MusicTest { get { Read(); return _music && DemoBoot.CaptureDir != null; } }
        public static bool Any => Proof || Split || MenuShots || Trial || Players || MusicTest;
        public static float Speed => DemoBoot.TestSpeed;
    }

    /// <summary>Round 20: the saved state - the unlock, best race / lap per track (+ per racer), the trial ghosts, the last choices.
    /// PlayerPrefs "seven_gc_*" (check runs: "seven_test_gc_*").</summary>
    public static class GcSave
    {
        public const int TrialLaps = 3;
        static string K(string k) => (DemoBoot.CaptureDir != null ? "seven_test_gc_" : "seven_gc_") + k;
        public static bool Unlocked => PlayerPrefs.GetInt("seven_gc_unlocked", 0) == 1 || GcBoot.Unlocked || (DemoBoot.CaptureDir != null && PlayerPrefs.GetInt(K("unlocked"), 0) == 1);
        public static void Unlock() { PlayerPrefs.SetInt(DemoBoot.CaptureDir != null ? K("unlocked") : "seven_gc_unlocked", 1); PlayerPrefs.Save(); }
        public static int LastRacer, LastTrack, LastMode; public static bool LastSide;
        public static void Load()
        {
            LastRacer = PlayerPrefs.GetInt(K("last_racer"), 0); LastTrack = PlayerPrefs.GetInt(K("last_track"), 0); LastMode = PlayerPrefs.GetInt(K("last_mode"), 0); LastSide = PlayerPrefs.GetInt(K("side"), 0) == 1;
            GcGame.SideBySide = LastSide || GcBoot.Side;
        }
        public static void Save()
        {
            PlayerPrefs.SetInt(K("last_racer"), LastRacer); PlayerPrefs.SetInt(K("last_track"), LastTrack); PlayerPrefs.SetInt(K("last_mode"), LastMode); PlayerPrefs.SetInt(K("side"), GcGame.SideBySide ? 1 : 0);
            PlayerPrefs.Save();
        }
        public static float Best(string track, bool trial) => PlayerPrefs.GetFloat(K($"best_{(trial ? "trial" : "race")}_{track}"), -1f);
        public static float BestLap(string track) => PlayerPrefs.GetFloat(K($"lap_{track}"), -1f);
        public static float BestBy(string track, string racer) => PlayerPrefs.GetFloat(K($"best_{track}_{racer}"), -1f);
        public static bool IsBestTrial(string track, float t) { float b = PlayerPrefs.GetFloat(K($"best_trial_{track}_ghost"), -1f); if (b < 0f || t <= b + 0.001f) { PlayerPrefs.SetFloat(K($"best_trial_{track}_ghost"), t); return true; } return false; }
        public static void Record(string track, string racer, float time, float lap, bool trial)
        {
            if (time > 0f) { var k = K($"best_{(trial ? "trial" : "race")}_{track}"); float b = PlayerPrefs.GetFloat(k, -1f); if (b < 0f || time < b) PlayerPrefs.SetFloat(k, time); var kr = K($"best_{track}_{racer}"); float br = PlayerPrefs.GetFloat(kr, -1f); if (br < 0f || time < br) PlayerPrefs.SetFloat(kr, time); }
            if (lap > 0f) { var k = K($"lap_{track}"); float b = PlayerPrefs.GetFloat(k, -1f); if (b < 0f || lap < b) PlayerPrefs.SetFloat(k, lap); }
        }
        public static string GhostPath(string track) => Path.Combine(Application.persistentDataPath, (DemoBoot.CaptureDir != null ? "test_" : "") + $"gc_ghost_{track}.bin");
        public static void ResetTest()
        {
            if (DemoBoot.CaptureDir == null) return;   // (test keys only - never the player's)
            foreach (var t in GcTrack.Ids)
            {
                PlayerPrefs.DeleteKey(K($"best_trial_{t}")); PlayerPrefs.DeleteKey(K($"best_race_{t}")); PlayerPrefs.DeleteKey(K($"lap_{t}")); PlayerPrefs.DeleteKey(K($"best_trial_{t}_ghost"));
                foreach (var s in GcStats.All) PlayerPrefs.DeleteKey(K($"best_{t}_{s.Racer}"));
                try { File.Delete(GhostPath(t)); } catch { }
            }
            foreach (var k in new[] { "last_racer", "last_track", "last_mode", "side" }) PlayerPrefs.DeleteKey(K(k));
            PlayerPrefs.Save();
        }
    }

    /// <summary>Round 20: reading a player's pad. Player 1 = the keyboard + (in a 1-player game) any pad, or the pad that joined first;
    /// player 2 = the pad that pressed A to join. Test runs map a player to a virtual pad.</summary>
    public static class GcPads
    {
        public static readonly Gamepad[] Device = new Gamepad[2];
        public static GcControls Read(int player, int players)
        {
            var c = new GcControls();
            var pad = player >= 0 && player < 2 ? Device[player] : null;
            if (pad == null && player == 0 && players <= 1) pad = Gamepad.current;
            if (pad != null && pad.added)
            {
                float st = pad.leftStick.x.ReadValue(); if (Mathf.Abs(st) < 0.12f) st = 0f; st += pad.dpad.right.isPressed ? 1f : pad.dpad.left.isPressed ? -1f : 0f;
                c.Steer = Mathf.Clamp(st, -1f, 1f);
                c.Throttle = Mathf.Max(pad.rightTrigger.ReadValue(), pad.buttonSouth.isPressed ? 1f : 0f);
                c.Brake = Mathf.Max(pad.leftTrigger.ReadValue(), pad.buttonEast.isPressed ? 1f : 0f);
                c.AirL = pad.leftShoulder.isPressed; c.AirR = pad.rightShoulder.isPressed; c.Use = pad.buttonWest.wasPressedThisFrame;
            }
            if (player == 0 && Keyboard.current != null)
            {
                var k = Keyboard.current;
                float ks = (k.rightArrowKey.isPressed || k.dKey.isPressed ? 1f : 0f) - (k.leftArrowKey.isPressed || k.aKey.isPressed ? 1f : 0f);
                if (ks != 0f) c.Steer = ks;
                if (k.upArrowKey.isPressed || k.wKey.isPressed) c.Throttle = 1f;
                if (k.downArrowKey.isPressed || k.sKey.isPressed) c.Brake = 1f;
                if (k.qKey.isPressed) c.AirL = true; if (k.eKey.isPressed) c.AirR = true;
                if (k.spaceKey.wasPressedThisFrame || k.leftCtrlKey.wasPressedThisFrame) c.Use = true;
            }
            return c;
        }
        public static bool PausePressed(int players)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
            foreach (var p in Gamepad.all) if (p.startButton.wasPressedThisFrame) return true;
            return false;
        }
    }

    /// <summary>Round 20: the time trial's ghost - the best run of a track, sampled 10 times a second (position + rotation), replayed
    /// as a translucent white copy of its vehicle.</summary>
    public class GcGhostRecorder
    {
        readonly List<(float t, Vector3 p, Quaternion r)> _s = new List<(float, Vector3, Quaternion)>(); float _next;
        public int Count => _s.Count;
        public void Add(GcRacer r, float t) { if (r == null || t < _next) return; _next = t + 0.1f; _s.Add((t, r.transform.position, r.transform.rotation)); }
        public void Save(string track, string racer)
        {
            try
            {
                using (var w = new BinaryWriter(File.Create(GcSave.GhostPath(track))))
                {
                    w.Write(racer ?? "7"); w.Write(_s.Count);
                    foreach (var (t, p, r) in _s) { w.Write(t); w.Write(p.x); w.Write(p.y); w.Write(p.z); w.Write(r.x); w.Write(r.y); w.Write(r.z); w.Write(r.w); }
                }
                GcGame.Log($"GC GHOST (R20): saved {track} ({_s.Count} samples, {racer})");
            }
            catch (System.Exception e) { GcGame.Log("GC GHOST (R20): could not save - " + e.Message); }
        }
    }
    public class GcGhost
    {
        List<(float t, Vector3 p, Quaternion r)> _s; GameObject _go; public int Samples => _s != null ? _s.Count : 0; public bool Shown;
        public static GcGhost Load(GcGame g, string track)
        {
            var path = GcSave.GhostPath(track); if (!File.Exists(path)) { GcGame.Log($"GC GHOST (R20): no ghost for {track} yet"); return null; }
            try
            {
                var gh = new GcGhost { _s = new List<(float, Vector3, Quaternion)>() }; string racer;
                using (var rd = new BinaryReader(File.OpenRead(path)))
                {
                    racer = rd.ReadString(); int n = rd.ReadInt32();
                    for (int i = 0; i < n; i++) { float t = rd.ReadSingle(); var p = new Vector3(rd.ReadSingle(), rd.ReadSingle(), rd.ReadSingle()); var r = new Quaternion(rd.ReadSingle(), rd.ReadSingle(), rd.ReadSingle(), rd.ReadSingle()); gh._s.Add((t, p, r)); }
                }
                int idx = GcStats.IndexOf(racer);   // Round 22: any racer of the roster
                gh._go = new GameObject("Ghost"); gh._go.transform.SetParent(g.transform, false);
                GcVehicles.Make(g, idx, gh._go.transform, true);
                foreach (var r in gh._go.GetComponentsInChildren<Renderer>()) { var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = g.GhostMat; r.sharedMaterials = mats; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
                foreach (var a in gh._go.GetComponentsInChildren<Animator>()) a.enabled = true;
                GcGame.Log($"GC GHOST (R20): {track} ghost loaded ({gh._s.Count} samples, {racer})");
                return gh;
            }
            catch (System.Exception e) { GcGame.Log("GC GHOST (R20): could not load - " + e.Message); return null; }
        }
        public void Play(float t)
        {
            if (_go == null || _s == null || _s.Count < 2) return;
            int i = 0; while (i < _s.Count - 2 && _s[i + 1].t < t) i++;
            var a = _s[i]; var b = _s[i + 1]; float k = Mathf.Clamp01((t - a.t) / Mathf.Max(1e-3f, b.t - a.t));
            _go.transform.SetPositionAndRotation(Vector3.Lerp(a.p, b.p, k), Quaternion.Slerp(a.r, b.r, k)); Shown = true;
            _go.SetActive(t <= _s[_s.Count - 1].t + 0.5f);
        }
        public Transform Transform => _go != null ? _go.transform : null;
        public void Clear() { if (_go != null) Object.Destroy(_go); }
    }
}
