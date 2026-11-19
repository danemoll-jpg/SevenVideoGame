using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF10: the Scree's voice - Dan's buzzing, chirping insect call (Unity/Assets/Audio/SFX/scree_chirp.mp3, 2.7 s), as
    /// 3D positional sound on every Scree (added by <see cref="ScreeGuard"/> itself):
    ///  - occasional idle calls at random intervals, each at a slightly different pitch, often a shorter cut of the clip
    ///    (so a group never sounds cloned);
    ///  - an alert call when it notices 7 (startled by a noise, or sees her, or the fight begins);
    ///  - short, sharper chirps for effort (a punch wind-up), getting hit, being dazed and going down (the fight tutorial).
    /// Always under the dialogue: ducked while anyone speaks. All the knobs are in <see cref="ScreeVoiceSettings"/>.
    /// </summary>
    public class ScreeVoice : MonoBehaviour
    {
        ScreeGuard _g; AudioSource _src; AudioClip _clip;
        float _next, _stopAt = -1f, _fadeOut, _baseVol, _lastAlert = -99f, _seeCheck;
        bool _saw; ScreeGuard.Mode _lastMode;
        static Transform _seven;
        public static int Calls, Alerts, FightChirps;

        void Start()
        {
            _g = GetComponent<ScreeGuard>();
            _clip = AudioKit.Clip(ScreeVoiceSettings.ClipName);
            if (_clip == null) { enabled = false; return; }
            var go = new GameObject("ScreeVoice"); go.transform.SetParent(transform, false); go.transform.localPosition = Vector3.up * 1.5f;
            _src = go.AddComponent<AudioSource>();
            _src.playOnAwake = false; _src.loop = false; _src.clip = _clip;
            _src.spatialBlend = 1f; _src.rolloffMode = AudioRolloffMode.Linear; _src.minDistance = ScreeVoiceSettings.MinDistance; _src.maxDistance = ScreeVoiceSettings.MaxDistance; _src.dopplerLevel = 0f;
            AudioKit.Track(_src, AudioKit.Bus.Sfx, ScreeVoiceSettings.Volume);
            _baseVol = ScreeVoiceSettings.Volume;
            _next = Time.time + Random.Range(1f, ScreeVoiceSettings.IdleMax);
            _g.FightEvent += OnFight;
        }

        void OnDestroy() { if (_g != null) _g.FightEvent -= OnFight; }

        void OnFight(ScreeGuard g, string what)
        {
            switch (what)
            {
                case "windup": Chirp(ScreeVoiceSettings.EffortLength, ScreeVoiceSettings.EffortPitch, 0.85f); break;
                case "hit": Chirp(ScreeVoiceSettings.HitLength, ScreeVoiceSettings.HitPitch, 1f); break;
                case "dazed": Chirp(ScreeVoiceSettings.HitLength * 1.6f, ScreeVoiceSettings.HitPitch * 0.92f, 0.9f); break;
                case "taken_down": Chirp(ScreeVoiceSettings.DownLength, ScreeVoiceSettings.DownPitch, 1f); break;
            }
        }

        /// <summary>A short, sharp cut from the clip (fight sounds).</summary>
        void Chirp(float length, float pitch, float vol)
        {
            FightChirps++;
            Play(Random.Range(0f, Mathf.Max(0f, _clip.length - length - 0.05f)), length, pitch * (1f + Random.Range(-0.04f, 0.04f)), vol);
        }

        void Play(float from, float length, float pitch, float vol)
        {
            _src.pitch = pitch; _src.time = Mathf.Clamp(from, 0f, _clip.length - 0.05f);
            _fadeOut = Mathf.Min(0.12f, length * 0.3f);
            _stopAt = Time.time + length / Mathf.Max(0.3f, pitch);
            _volNow = vol; _src.Play();
        }
        float _volNow = 1f;

        void Update()
        {
            if (_src == null) return;
            // duck under any dialogue
            float duck = DialoguePlayer.AnyBusy ? ScreeVoiceSettings.DuckUnderDialogue : 1f;
            float fade = 1f;
            if (_stopAt > 0f) { float left = _stopAt - Time.time; if (left <= 0f) { _src.Stop(); _stopAt = -1f; } else if (left < _fadeOut) fade = left / _fadeOut; }
            AudioKit.SetBaseVolume(_src, _baseVol * _volNow * duck * fade);
            // the alert call: startled (alert / investigating), sees her, or a fight begins
            bool fighting = _g.InFight;
            if (!fighting && _g.Current != _lastMode && _g.Current == ScreeGuard.Mode.Alert) AlertCall();
            _lastMode = _g.Current;
            if (Time.time > _seeCheck)
            {
                _seeCheck = Time.time + 0.3f;
                if (_seven == null) { var s7 = FindAnyObjectByType<ChapterSeven>(); if (s7 != null) _seven = s7.transform; }
                bool sees = _seven != null && _g.Current != ScreeGuard.Mode.Still && _g.Sees(_seven.position + Vector3.up * 1.0f);
                if (sees && !_saw) AlertCall();
                _saw = sees;
            }
            if (fighting && !_wasFighting) AlertCall();
            _wasFighting = fighting;
            // idle calls
            if (!fighting && Time.time >= _next && !_src.isPlaying)
            {
                _next = Time.time + Random.Range(ScreeVoiceSettings.IdleMin, ScreeVoiceSettings.IdleMax);
                float pitch = 1f + Random.Range(-ScreeVoiceSettings.PitchRange, ScreeVoiceSettings.PitchRange);
                bool cut = Random.value < ScreeVoiceSettings.ShortCutChance;
                float len = cut ? Random.Range(ScreeVoiceSettings.ShortCutMin, ScreeVoiceSettings.ShortCutMax) : _clip.length;
                Calls++;
                Play(cut ? Random.Range(0f, _clip.length - len) : 0f, len, pitch, ScreeVoiceSettings.IdleVolume);
            }
        }
        bool _wasFighting;

        void AlertCall()
        {
            if (Time.time - _lastAlert < ScreeVoiceSettings.AlertCooldown) return;
            _lastAlert = Time.time; Alerts++;
            Play(0f, Mathf.Min(_clip.length, ScreeVoiceSettings.AlertLength), ScreeVoiceSettings.AlertPitch * (1f + Random.Range(-0.03f, 0.03f)), 1f);
            _next = Mathf.Max(_next, Time.time + ScreeVoiceSettings.IdleMin);
        }
    }

    /// <summary>QF10: every Scree voice knob in one place (Dan: "see how it goes" - tune here).</summary>
    public static class ScreeVoiceSettings
    {
        public const string ClipName = "scree_chirp";       // Unity/Assets/Audio/SFX/scree_chirp.mp3 (SFX bank)
        public const float Volume = 0.8f;                   // base level on the SFX bus (0..1)
        public const float IdleVolume = 0.75f;              // idle calls relative to Volume
        public const float MinDistance = 2f, MaxDistance = 26f;   // 3D: full volume within MinDistance, silent beyond MaxDistance
        public const float IdleMin = 8f, IdleMax = 20f;     // seconds between idle calls (random)
        public const float PitchRange = 0.10f;              // idle calls: pitch 1 +- this
        public const float ShortCutChance = 0.6f;           // how often an idle call is a shorter cut of the clip
        public const float ShortCutMin = 0.5f, ShortCutMax = 1.4f;   // its length (s)
        public const float AlertLength = 1.6f, AlertPitch = 1.12f, AlertCooldown = 6f;   // noticing 7
        public const float EffortLength = 0.30f, EffortPitch = 1.25f;                    // punch wind-up
        public const float HitLength = 0.22f, HitPitch = 1.35f;                          // getting hit (dazed: a little longer, lower)
        public const float DownLength = 0.55f, DownPitch = 0.85f;                        // going down (a takedown)
        public const float DuckUnderDialogue = 0.3f;        // volume while anyone speaks
    }
}
