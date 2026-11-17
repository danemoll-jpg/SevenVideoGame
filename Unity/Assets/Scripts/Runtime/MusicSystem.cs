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
            _duck = Mathf.MoveTowards(_duck, DialoguePlayer.AnyBusy ? DuckLevel : 1f, Time.unscaledDeltaTime * (DialoguePlayer.AnyBusy ? 2.5f : 0.8f));
            float g = AudioKit.Gain(AudioKit.Bus.Music) * _duck;
            _a.volume = _aW * g; _b.volume = _bW * g;
        }
    }
}
