using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 7 music (Docs/MUSIC.md): main_theme · moon_prison · aboard_saucer, loaded by name from
    /// Resources/Audio/MusicBank (built from Assets/Audio/Music; a missing file = silence, no error).
    /// Two sources cross-fade (~2.5 s), loop, and duck under dialogue (<see cref="DialoguePlayer.AnyBusy"/>).
    /// Survives scene loads, so a track can carry on from the Moon into the saucer.
    /// </summary>
    public class MusicSystem : MonoBehaviour
    {
        static MusicSystem _i;
        AudioSource _a, _b;          // _a = current
        AudioSource _aT, _bT;        // QF36: each slot's loop body (an intro + loop track: _a plays the intro once, _aT the body, scheduled)
        float _aW, _bW;              // fade weights
        float _duck = 1f;
        static float _duckUntil;
        /// <summary>QF43 (13): duck the music for a moment (a sound that must be heard over it: Venus's pulse).</summary>
        public static void DuckFor(float seconds) { _duckUntil = Mathf.Max(_duckUntil, Time.unscaledTime + seconds); }
        public static string Current { get; private set; }
        public const float DuckLevel = 0.35f;
        /// <summary>QF31 (A3): a scene can play its track low (her grief: moon_transfer under her sobs). Reset to 1 on every scene load.</summary>
        public static float Level = 1f;

        static MusicSystem I
        {
            get
            {
                if (_i != null) return _i;
                var go = new GameObject("MusicSystem");
                DontDestroyOnLoad(go);
                _i = go.AddComponent<MusicSystem>();
                _i._a = _i.Src("Music_A"); _i._b = _i.Src("Music_B"); _i._aT = _i.Src("Music_A_loop"); _i._bT = _i.Src("Music_B_loop");
                return _i;
            }
        }

        /// <summary>Scene defaults (directors refine them): menu = main theme, the saucer demo = aboard_saucer,
        /// fight / Ratwing demos = no music. Chapter 1 scenes set their own cues.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (sc, mode) =>
            {
                ReportScene();                                             // QF24 (21): the last scene's silence audit
                _silenceUntil = 0f; _silenceWhy = null; _quietFor = 0f; _longestQuiet = 0f; _guardStarts = 0; _sceneName = sc.name; Level = 1f;
                // QF24 (21): a stale suspension from the last scene (a vision whose resume never came) never carries over
                if (_suspended != null) { AudioKit.Log?.Invoke($"music (QF24 21): a suspension of '{_suspended}' left from the last scene cleared on loading {sc.name}"); _suspended = null; }
                if (sc.name == "FightDemo" || sc.name == "FightDemo_Room" || sc.name == "Ratwing") Stop(1.5f);
                else if (sc.name == "Saucer" && !Chapter1State.Aboard)
                {
                    Play("aboard_saucer", 2.5f);
                    AudioKit.Loop("amb_saucer", null, null, 1f, AudioKit.Bus.Ambience);
                }
            };
        }

        // ---- QF24 (18 / 20 / 21): music rules owned by the manager, not patched per scene ---------------------------------
        //  - boarding: <see cref="Board"/> starts the saucer theme with a short fade whatever state the music was in;
        //  - a vision suspends and then RESUMES (VisionTouch.Into / Back own it), and a stale suspension never survives a load;
        //  - never silent while the player has control: after a few seconds of silence with control, the area's theme comes
        //    back (MUSIC GUARD) unless a director declared a deliberate short silence (<see cref="Silence"/>);
        //  - a boss track starts with the fight (BossFight.Begin -> <see cref="BossTrack"/>), never at the approach or the talk.
        static float _silenceUntil, _quietFor, _longestQuiet, _guardNext; static string _silenceWhy, _sceneName; static int _guardStarts;
        public static int GuardStarts => _guardStarts;
        public static float LongestQuiet => _longestQuiet;
        /// <summary>A deliberate silence (the quiet passage, a dramatic pause): the guard leaves it alone for up to <paramref name="seconds"/>.</summary>
        public static void Silence(string why, float seconds) { _silenceUntil = Time.unscaledTime + seconds; _silenceWhy = why; Stop(1.5f); AudioKit.Log?.Invoke($"music (QF24 21): a deliberate silence - {why} (up to {seconds:0} s)"); }
        public static void EndSilence() { _silenceUntil = 0f; _silenceWhy = null; }
        /// <summary>On boarding the saucer (the beam / back in the cabin): the saucer theme, a short fade in, every time.</summary>
        public static void Board(float fade = 2f) { _suspended = null; EndSilence(); Play("aboard_saucer", fade); AudioKit.Log?.Invoke("music (QF24 21): boarding - the saucer theme"); }
        /// <summary>The area's own theme (what the guard brings back).</summary>
        public static string Theme(string scene)
        {
            switch (scene)
            {
                case "Chapter1": return "moon_prison";
                case "Saucer": return "aboard_saucer";
                case "Venus": return "venus_gardens";
                case "Europa": return "europa_underwater";
                case "Mars": return "mars_kingdoms";
                case "Asteroid": return "asteroid_city";
                case "Biaveh": return "biaveh_mothership";
                case "Cattalus": return "cattalus_city";
                case "Finale": return Seven.FinaleDirector.I != null ? Seven.FinaleDirector.I.ThemeNow : "moon_descent";
                case "Menu": return "main_theme";
                default: return null;
            }
        }
        /// <summary>A boss's own track (started by BossFight.Begin, the fight's first moment).</summary>
        public static string BossTrack(string boss) => boss == "tunneler" ? "moon_tunneler" : boss == "viper" ? "mars_viper" : boss == "machinist" ? "asteroid_machinist" : boss == "ratwing" ? "biaveh_ratwing" : boss == "wormmaster" ? "venus_underground" : null;
        /// <summary>Does the player have control right now (a chapter's follow camera, no fade, no hold)?</summary>
        public static System.Func<bool> HasControl = DefaultControl;
        static bool DefaultControl()
        {
            var cam = Camera.main; if (cam == null) return false;
            if (MoonOpening.Playing) return false;   // P3 (3): the opening film (the Moon, the title, the dive) is not control - 7 is locked behind it
            var cc = cam.GetComponent<ChapterCamera>(); if (cc != null) return cc.isActiveAndEnabled && !cc.InShot;
            var sc = cam.GetComponent<SaucerCamera>(); if (sc != null) return !sc.Hold && (Chapter1Aboard.I == null || !Chapter1Aboard.I.Active || !Chapter1Aboard.I.InGalleyScene);
            return false;
        }
        static void ReportScene()
        {
            if (DemoBoot.CaptureDir == null || string.IsNullOrEmpty(_sceneName)) return;
            ChapterCapture.I?.Log($"MUSIC (QF24 21) {_sceneName}: longest silence while she had control {_longestQuiet:0.0} s (must be under 5 s unless declared), the guard brought the theme back {_guardStarts}x, now '{Current ?? "(none)"}'{(_suspended != null ? $", suspended '{_suspended}'" : "")}");
        }
        public static void Report() => ReportScene();

        // ---- QF36: the check numbers (GC MUSIC lines) and a test seek near each seam -------------------------------------
        string _loopTrack; int _lastBodySample; float _playAt; bool _introStarted;
        public static int LoopWraps, IntroHandovers, GapFrames, MaxSongsOutsideFades, OverlapFrames, LateStarts; public static float OverlapSeconds; public static bool IntroPlaying;
        public static void ResetCounters() { LoopWraps = IntroHandovers = GapFrames = MaxSongsOutsideFades = OverlapFrames = LateStarts = 0; OverlapSeconds = 0f; }
        public static bool IsLooped => _i != null && _i._aT.clip != null;
        public static float BodyTime => _i != null && _i._aT.clip != null ? _i._aT.time : -1f;
        public static float IntroTime => _i != null && _i._a.clip != null && _i._a.isPlaying ? _i._a.time : -1f;
        /// <summary>Test only: jump the intro to <paramref name="before"/> s before its end (the body re-scheduled on the same sample).</summary>
        public static bool SeekIntroEnd(float before)
        {
            var m = _i; if (m == null || m._aT.clip == null || m._a.clip == null || !m._a.isPlaying) return false;
            int at = Mathf.Max(0, m._a.clip.samples - Mathf.RoundToInt(before * m._a.clip.frequency));
            m._a.timeSamples = at;
            m._aT.Stop(); m._aT.timeSamples = 0; m._aT.PlayScheduled(AudioSettings.dspTime + (m._a.clip.samples - at) / (double)m._a.clip.frequency);
            return true;
        }
        /// <summary>Test only: jump the loop body to <paramref name="before"/> s before its end (it then wraps to its start).</summary>
        public static bool SeekBodyEnd(float before)
        {
            var m = _i; if (m == null || m._aT.clip == null || !m._aT.isPlaying) return false;
            m._aT.timeSamples = Mathf.Max(0, m._aT.clip.samples - Mathf.RoundToInt(before * m._aT.clip.frequency)); m._lastBodySample = m._aT.timeSamples;
            return true;
        }

        AudioSource Src(string n)
        {
            var go = new GameObject(n); go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false; s.loop = true; s.spatialBlend = 0f; s.volume = 0f;
            return s;
        }

        /// <summary>Cross-fade to a track (no-op if it is already playing). null / missing = fade to silence.</summary>
        public static void Play(string track, float fade = 2.5f)
        {
            var m = I;
            if (_suspended != null) { if (track == _suspended) { Resume(fade); return; } _suspended = null; }
            if (Current == track && (track == null || m._a.isPlaying || m._aT.isPlaying)) return;
            // QF36: a track made of an intro + a seamless loop body (<name>_intro / <name>_loop, Tools/Music/make_gc_loops.py)
            var body = track != null && AudioKit.Music != null ? AudioKit.Music.Get(track + "_loop") : null;
            var clip = body != null ? AudioKit.Music.Get(track + "_intro") : track != null && AudioKit.Music != null ? AudioKit.Music.Get(track) : null;
            bool has = clip != null || body != null;
            AudioKit.Log?.Invoke($"music -> {track ?? "(none)"}{(track != null && !has ? " (missing file: silence)" : "")}{(body != null ? $" (intro {(clip != null ? clip.length : 0f):0.0} s, then the loop body {body.length:0.0} s)" : "")}");
            Current = has ? track : null;
            m.StopAllCoroutines();
            // current becomes the fading-out one
            (m._a, m._b) = (m._b, m._a); (m._aW, m._bW) = (m._bW, m._aW); (m._aT, m._bT) = (m._bT, m._aT);
            m._aT.Stop(); m._aT.clip = null;
            if (body != null)
            {
                double t0 = AudioSettings.dspTime + 0.06;
                m._aT.clip = body; m._aT.loop = true; m._aT.timeSamples = 0;
                if (clip != null) { m._a.clip = clip; m._a.loop = false; m._a.timeSamples = 0; m._a.PlayScheduled(t0); m._aT.PlayScheduled(t0 + clip.samples / (double)clip.frequency); }
                else { m._a.clip = null; m._aT.PlayScheduled(t0); }
                if (clip != null) clip.LoadAudioData(); body.LoadAudioData();
                m._aW = 0f; m._loopTrack = track; m._lastBodySample = 0; m._playAt = Time.unscaledTime; m._introStarted = clip == null; IntroPlaying = false;
            }
            else if (clip != null) { m._a.clip = clip; m._a.loop = true; m._a.time = 0f; m._a.Play(); m._aW = 0f; m._loopTrack = null; m._playAt = Time.unscaledTime; }
            m.StartCoroutine(m.CrossFade(has, fade));
        }
        public static void Stop(float fade = 2.5f) => Play(null, fade);

        // ---- Round 13 (0g): the level's track came back after neither vision (Chapter 1's door vision, Venus C09). A vision
        // now suspends the music (fades it out, keeps the track and its place) and resumes it afterwards: it fades back in
        // from where it left off, every time. A watchdog resumes it if a vision path ever forgets to (60 s, unscaled).
        static string _suspended; static float _suspendedAt, _suspendTime;
        public static bool IsSuspended => _suspended != null;
        public static string SuspendedTrack => _suspended;
        public static void Suspend(float fade = 1.2f)
        {
            var m = I;
            if (Current == null) { _suspended = null; return; }
            _suspended = Current; _suspendedAt = m._a.time; _suspendTime = Time.unscaledTime;
            AudioKit.Log?.Invoke($"music: suspended '{_suspended}' at {_suspendedAt:0.0} s (vision)");
            m.StopAllCoroutines(); m.StartCoroutine(m.FadeA(0f, fade, true));
        }
        public static void Resume(float fade = 3f)
        {
            if (_suspended == null) return;
            var m = I; var track = _suspended; _suspended = null;
            var lb = AudioKit.Music != null ? AudioKit.Music.Get(track + "_loop") : null;
            if (lb != null) { m.StopAllCoroutines(); m._b.Stop(); m._bT.Stop(); m._bW = 0f; m._a.Stop(); m._aT.clip = lb; m._aT.loop = true; m._aT.UnPause(); if (!m._aT.isPlaying) m._aT.Play(); Current = track; m.StartCoroutine(m.FadeA(1f, fade, false)); return; }
            var clip = AudioKit.Music != null ? AudioKit.Music.Get(track) : null;
            if (clip == null) return;
            m.StopAllCoroutines();
            m._b.Stop(); m._bW = 0f;
            m._a.clip = clip; m._a.time = Mathf.Clamp(_suspendedAt, 0f, Mathf.Max(0f, clip.length - 0.1f)); m._a.Play();
            Current = track;
            AudioKit.Log?.Invoke($"music: resumed '{track}' from {m._a.time:0.0} s after the vision (fade in {fade:0.0} s)");
            m.StartCoroutine(m.FadeA(1f, fade, false));
        }
        IEnumerator FadeA(float to, float seconds, bool stopAtEnd)
        {
            float from = _aW;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, seconds)) { _aW = Mathf.Lerp(from, to, t); _bW = Mathf.MoveTowards(_bW, 0f, Time.unscaledDeltaTime); yield return null; }
            _aW = to;
            if (stopAtEnd) { _a.Pause(); _aT.Pause(); }
        }

        IEnumerator CrossFade(bool fadeIn, float seconds)
        {
            float a0 = _aW, b0 = _bW;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, seconds))
            {
                _aW = fadeIn ? Mathf.Lerp(a0, 1f, t) : 0f;
                _bW = Mathf.Lerp(b0, 0f, t);
                yield return null;
            }
            _aW = fadeIn ? 1f : 0f; _bW = 0f;
            _b.Stop(); _bT.Stop();
            if (!fadeIn) { _a.Stop(); _aT.Stop(); }
        }

        void Update()
        {
            if (_suspended != null && Time.unscaledTime - _suspendTime > 60f) Resume(3f);
            // QF24 (21): the never-silent guard (while she has control)
            if (Time.unscaledTime >= _guardNext)
            {
                float step = 0.25f; _guardNext = Time.unscaledTime + step;
                bool control = false; try { control = HasControl != null && HasControl(); } catch { }
                bool silent = Current == null && _suspended == null && !(Time.unscaledTime < _silenceUntil);
                if (control && silent && Theme(_sceneName ?? "") != null)
                {
                    _quietFor += step; _longestQuiet = Mathf.Max(_longestQuiet, _quietFor);
                    if (_quietFor >= 3.5f) { var th = Theme(_sceneName); _guardStarts++; AudioKit.Log?.Invoke($"MUSIC GUARD (QF24 21): {_quietFor:0.0} s of silence with control in {_sceneName} - '{th}' fades back in"); Play(th, 2.5f); _quietFor = 0f; }
                }
                else _quietFor = 0f;
            }
            bool duckNow = DialoguePlayer.AnyBusy || Purr.Playing || Time.unscaledTime < _duckUntil;   // QF23 (11): under his purr too; QF43 (13): under Venus's pulse
            _duck = Mathf.MoveTowards(_duck, duckNow ? DuckLevel : 1f, Time.unscaledDeltaTime * (duckNow ? 2.5f : 0.8f));
            float g = AudioKit.Gain(AudioKit.Bus.Music) * _duck * Mathf.Clamp01(Level);
            _a.volume = _aW * g; _b.volume = _bW * g; _aT.volume = _a.volume; _bT.volume = _b.volume;
            // QF36: the loop's bookkeeping (wraps of the body, frames with a track set but nothing sounding, songs heard at once)
            if (_aT.clip != null)
            {
                if (_aT.isPlaying) { int ts = _aT.timeSamples; if (ts + _aT.clip.frequency < _lastBodySample) LoopWraps++; _lastBodySample = ts; }
                if (_a.isPlaying && _a.clip != null && !_a.loop && _a.timeSamples > 0) { IntroPlaying = true; _introStarted = true; }
                else if (IntroPlaying && !_a.isPlaying) { IntroPlaying = false; if (_aT.isPlaying) IntroHandovers++; }
                // a safety net: the body's scheduled start missed (never expected; logged) - it starts at once rather than leave silence
                if (_suspended == null && Current != null && Current == _loopTrack && _introStarted && !_a.isPlaying && !_aT.isPlaying && !AudioListener.pause && Time.unscaledTime > _playAt + 0.5f)
                { _aT.Play(); LateStarts++; AudioKit.Log?.Invoke($"music (QF36): the loop body of '{Current}' missed its scheduled start - started now"); }
            }
            if (Current != null && !_a.isPlaying && !_aT.isPlaying && !AudioListener.pause && Time.unscaledTime > _playAt + 0.2f) GapFrames++;   // (not in the scheduled start's own first moment)
            int heard = (_aW > 0.01f ? 1 : 0) + (_bW > 0.01f ? 1 : 0);
            if (_aW > 0.99f || _bW < 0.01f) MaxSongsOutsideFades = Mathf.Max(MaxSongsOutsideFades, heard);
            if (_a.isPlaying && _aT.isPlaying && _a.clip != null && _a.timeSamples > 0 && _aT.timeSamples > 0) { OverlapFrames++; OverlapSeconds += Time.unscaledDeltaTime; }
        }
    }
}
