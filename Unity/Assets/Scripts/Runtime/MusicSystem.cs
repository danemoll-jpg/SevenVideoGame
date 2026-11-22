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
        float _aW, _bW;              // fade weights
        float _duck = 1f;
        public static string Current { get; private set; }
        public const float DuckLevel = 0.35f;

        static MusicSystem I
        {
            get
            {
                if (_i != null) return _i;
                var go = new GameObject("MusicSystem");
                DontDestroyOnLoad(go);
                _i = go.AddComponent<MusicSystem>();
                _i._a = _i.Src("Music_A"); _i._b = _i.Src("Music_B");
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
                if (sc.name == "FightDemo" || sc.name == "FightDemo_Room" || sc.name == "Ratwing") Stop(1.5f);
                else if (sc.name == "Saucer" && !Chapter1State.Aboard)
                {
                    Play("aboard_saucer", 2.5f);
                    AudioKit.Loop("amb_saucer", null, null, 1f, AudioKit.Bus.Ambience);
                }
            };
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
            if (Current == track && (track == null || m._a.isPlaying)) return;
            var clip = track != null && AudioKit.Music != null ? AudioKit.Music.Get(track) : null;
            AudioKit.Log?.Invoke($"music -> {track ?? "(none)"}{(track != null && clip == null ? " (missing file: silence)" : "")}");
            Current = clip != null ? track : null;
            m.StopAllCoroutines();
            // current becomes the fading-out one
            (m._a, m._b) = (m._b, m._a); (m._aW, m._bW) = (m._bW, m._aW);
            if (clip != null) { m._a.clip = clip; m._a.time = 0f; m._a.Play(); m._aW = 0f; }
            m.StartCoroutine(m.CrossFade(clip != null, fade));
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
            if (stopAtEnd) { _a.Pause(); }
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
            _b.Stop();
            if (!fadeIn) _a.Stop();
        }

        void Update()
        {
            if (_suspended != null && Time.unscaledTime - _suspendTime > 60f) Resume(3f);
            bool duckNow = DialoguePlayer.AnyBusy || Purr.Playing;   // QF23 (11): the music ducks under his purr too
            _duck = Mathf.MoveTowards(_duck, duckNow ? DuckLevel : 1f, Time.unscaledDeltaTime * (duckNow ? 2.5f : 0.8f));
            float g = AudioKit.Gain(AudioKit.Bus.Music) * _duck;
            _a.volume = _aW * g; _b.volume = _bW * g;
        }
    }
}
