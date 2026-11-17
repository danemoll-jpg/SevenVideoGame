using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 7 sound mix. Four buses with fixed base levels so dialogue always sits on top:
    ///   Voice 1.0  >  SFX 0.55  >  Music 0.42 (ducked to 35 % while anyone speaks)  >  Ambience 0.28.
    /// The player's settings (pause menu → Settings) scale Music / SFX (+ ambience) / Voice; saved in PlayerPrefs
    /// (check runs never write them). Every source made here is tracked and follows setting changes live.
    /// Clips come from Resources/Audio/SfxBank (see <see cref="AudioBank"/>): a missing name plays nothing.
    /// -mute still wins: AudioListener.volume = 0 for every automated run.
    /// </summary>
    public static class AudioKit
    {
        public enum Bus { Voice, Sfx, Music, Ambience }
        static readonly float[] Base = { 1.0f, 0.55f, 0.42f, 0.28f };
        static AudioBank _sfx, _music;
        public static AudioBank Sfx => _sfx != null ? _sfx : (_sfx = Resources.Load<AudioBank>("Audio/SfxBank"));
        public static AudioBank Music => _music != null ? _music : (_music = Resources.Load<AudioBank>("Audio/MusicBank"));

        // ---- settings
        static float? _vMusic, _vSfx, _vVoice;
        static bool Saving => DemoBoot.CaptureDir == null;
        public static float MusicVolume { get => _vMusic ??= PlayerPrefs.GetFloat("seven_vol_music", 1f); set { _vMusic = Mathf.Clamp01(value); if (Saving) PlayerPrefs.SetFloat("seven_vol_music", _vMusic.Value); Changed(); } }
        public static float SfxVolume { get => _vSfx ??= PlayerPrefs.GetFloat("seven_vol_sfx", 1f); set { _vSfx = Mathf.Clamp01(value); if (Saving) PlayerPrefs.SetFloat("seven_vol_sfx", _vSfx.Value); Changed(); } }
        public static float VoiceVolume { get => _vVoice ??= PlayerPrefs.GetFloat("seven_vol_voice", 1f); set { _vVoice = Mathf.Clamp01(value); if (Saving) PlayerPrefs.SetFloat("seven_vol_voice", _vVoice.Value); Changed(); } }

        public static float Gain(Bus b)
        {
            float user = b == Bus.Voice ? VoiceVolume : b == Bus.Music ? MusicVolume : SfxVolume;
            return Base[(int)b] * user;
        }

        // ---- tracked sources (loops, voice sources): base volume per source, re-applied on changes
        class Tracked { public AudioSource Src; public Bus Bus; public float Vol; }
        static readonly List<Tracked> _tracked = new List<Tracked>();
        static void Changed()
        {
            _tracked.RemoveAll(t => t.Src == null);
            foreach (var t in _tracked) t.Src.volume = t.Vol * Gain(t.Bus);
        }
        /// <summary>Put an existing source on a bus (its current volume becomes its base level).</summary>
        public static void Track(AudioSource src, Bus bus, float vol = -1f)
        {
            if (src == null) return;
            var t = new Tracked { Src = src, Bus = bus, Vol = vol >= 0f ? vol : src.volume };
            _tracked.Add(t);
            src.volume = t.Vol * Gain(bus);
        }
        public static void SetBaseVolume(AudioSource src, float vol)
        {
            foreach (var t in _tracked) if (t.Src == src) { t.Vol = vol; src.volume = vol * Gain(t.Bus); return; }
        }

        // ---- playing
        public static AudioClip Clip(string name) => Sfx != null ? Sfx.Get(name) : null;

        static AudioSource Make(string name, Vector3? at, Transform parent, bool loop, float minDist, float maxDist)
        {
            var go = new GameObject("Sfx_" + name);
            if (parent != null) { go.transform.SetParent(parent, false); go.transform.localPosition = Vector3.zero; }
            else if (at.HasValue) go.transform.position = at.Value;
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false; s.loop = loop;
            bool spatial = at.HasValue || parent != null;
            s.spatialBlend = spatial ? 1f : 0f;
            s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = minDist; s.maxDistance = maxDist;
            s.dopplerLevel = 0f;
            return s;
        }

        /// <summary>One-shot. at = world position (3D) or null (2D). Returns the source (destroyed after the clip).</summary>
        public static AudioSource Play(string name, Vector3? at = null, float vol = 1f, float pitchJitter = 0f, Bus bus = Bus.Sfx, float maxDist = 30f)
        {
            var clip = Clip(name);
            if (clip == null) return null;
            var s = Make(name, at, null, false, 1.2f, maxDist);
            s.clip = clip; s.volume = vol * Gain(bus);
            s.pitch = 1f + (pitchJitter > 0f ? Random.Range(-pitchJitter, pitchJitter) : 0f);
            s.Play();
            Object.Destroy(s.gameObject, clip.length / Mathf.Max(0.5f, s.pitch) + 0.2f);
            Log?.Invoke($"sfx {name}{(at.HasValue ? " @" + at.Value.ToString("0.0") : "")}");
            return s;
        }

        /// <summary>Same, picking one of the numbered variants (step_rock_1..3).</summary>
        public static AudioSource PlayVariant(string prefix, Vector3? at, float vol, float pitchJitter = 0.05f, float maxDist = 14f)
        {
            var v = Sfx != null ? Sfx.Variants(prefix) : null;
            if (v == null || v.Count == 0) return null;
            var clip = v[Random.Range(0, v.Count)];
            var s = Make(prefix, at, null, false, 0.8f, maxDist);
            s.clip = clip; s.volume = vol * Gain(Bus.Sfx);
            s.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            s.Play();
            Object.Destroy(s.gameObject, clip.length / Mathf.Max(0.5f, s.pitch) + 0.2f);
            return s;
        }

        /// <summary>Looping source (hum, ambience) following a transform or at a point (null both = 2D). Fade in optional.</summary>
        public static AudioSource Loop(string name, Transform parent = null, Vector3? at = null, float vol = 1f, Bus bus = Bus.Sfx, float minDist = 1.5f, float maxDist = 25f)
        {
            var clip = Clip(name);
            if (clip == null) return null;
            var s = Make(name, at, parent, true, minDist, maxDist);
            s.clip = clip;
            Track(s, bus, vol);
            s.time = Random.Range(0f, clip.length * 0.9f);
            s.Play();
            Log?.Invoke($"loop {name} ({bus})");
            return s;
        }

        /// <summary>Fade a tracked source's base volume to a value (and stop + destroy at 0 if asked).</summary>
        public static System.Collections.IEnumerator Fade(AudioSource s, float to, float seconds, bool destroyAtZero = false)
        {
            if (s == null) yield break;
            float from = -1f;
            foreach (var t in _tracked) if (t.Src == s) from = t.Vol;
            if (from < 0f) from = s.volume;
            for (float t = 0f; t < 1f && s != null; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, seconds))
            {
                SetBaseVolume(s, Mathf.Lerp(from, to, t));
                yield return null;
            }
            if (s == null) yield break;
            SetBaseVolume(s, to);
            if (destroyAtZero && to <= 0f) Object.Destroy(s.gameObject);
        }

        /// <summary>Check runs hook every cue here (capture log: which sounds fired when).</summary>
        public static System.Action<string> Log;
    }
}
