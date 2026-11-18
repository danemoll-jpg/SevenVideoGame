using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Seven
{
    /// <summary>
    /// Plays voice lines in order with subtitles and the script's pauses. 7 speaks aloud (plain 2D voice); every cat
    /// line is telepathic: close, dry, narrow (mono, centred) with a short soft room reverb and a gentle band-limit, so it
    /// sounds "inside your head". Vision mode plays cat lines faint and far away (quieter, darker, longer reverb).
    /// A / Enter skips the rest of the current line. Everything played is recorded in <see cref="Played"/> for checks.
    /// </summary>
    public class DialoguePlayer : MonoBehaviour
    {
        public VoiceBank Bank;
        public ChapterUI UI;
        [Tooltip("Gap after every line when the script asks for nothing longer (s).")]
        public float DefaultGap = 0.45f;

        AudioSource _seven, _cat, _catFar;
        bool _busy;
        public bool Busy { get => _busy; private set { if (_busy != value) _busyCount += value ? 1 : -1; _busy = value; } }
        static int _busyCount;
        /// <summary>Any dialogue playing anywhere (music ducks under it).</summary>
        public static bool AnyBusy => _busyCount > 0;
        void OnDestroy() { if (_busy) _busyCount--; _busy = false; }
        public VoiceBank.Line Current { get; private set; }
        public readonly List<string> Played = new List<string>();
        /// <summary>Check runs: skip lines automatically (seconds of each line actually waited).</summary>
        [System.NonSerialized] public float AutoSkipAfter = -1f;
        bool _skip;
        public event System.Action<VoiceBank.Line> LineStarted;

        void Awake()
        {
            _seven = MakeSource("Voice_7", false, false);
            _cat = MakeSource("Voice_Cat_Telepathy", true, false);
            _catFar = MakeSource("Voice_Cat_Far", true, true);
            foreach (var s in new[] { _seven, _cat, _catFar }) AudioKit.Track(s, AudioKit.Bus.Voice);   // Round 7: voice bus on top
        }

        /// <summary>The telepathy treatment (also used by the sound check).</summary>
        public static AudioSource MakeSource(string name, bool telepathic, bool far, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false; src.spatialBlend = 0f; src.loop = false;
            src.volume = 1f;
            if (!telepathic) return src;
            // Narrow and centred: mono, no stereo spread, a touch quieter than 7's spoken voice.
            src.panStereo = 0f; src.spread = 0f; src.volume = far ? 0.42f : 0.9f;
            var hp = go.AddComponent<AudioHighPassFilter>(); hp.cutoffFrequency = far ? 300f : 170f;
            var lp = go.AddComponent<AudioLowPassFilter>(); lp.cutoffFrequency = far ? 1900f : 7200f;
            var rv = go.AddComponent<AudioReverbFilter>();
            rv.reverbPreset = AudioReverbPreset.User;
            rv.dryLevel = 0f;                         // the dry voice stays up front
            rv.room = far ? -300f : -1500f;            // soft, short room
            rv.roomHF = far ? -1400f : -900f;
            rv.decayTime = far ? 2.2f : 0.38f;
            rv.decayHFRatio = 0.5f;
            rv.reflectionsLevel = far ? -400f : -1400f;
            rv.reflectionsDelay = 0.006f;
            rv.reverbLevel = far ? 200f : -700f;
            rv.reverbDelay = 0.012f;
            rv.diffusion = 100f; rv.density = 100f;
            return src;
        }

        void Update()
        {
            if (!Busy || (DemoBoot.CaptureDir != null && !DemoBoot.PlayerSim) || PauseMenu.IsOpen) return;
            var kb = Keyboard.current; var gp = Gamepad.current;
            if ((kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) || (gp != null && gp.buttonNorth.wasPressedThisFrame))   // Round 9: Y skips (A is jump in Chapter 1)
                _skip = true;
        }

        public IEnumerator PlayScene(int scene, int from = 1, int to = 999, bool far = false) => PlayLines(Bank.Scene(scene, from, to), far);

        public IEnumerator PlayIds(params string[] ids)
        {
            var l = new List<VoiceBank.Line>();
            foreach (var id in ids) { var x = Bank.Get(id); if (x != null) l.Add(x); else Debug.LogWarning("[Seven] missing voice line " + id); }
            return PlayLines(l, false);
        }

        public IEnumerator PlayLines(List<VoiceBank.Line> lines, bool far)
        {
            while (Busy) yield return null;
            Busy = true;
            foreach (var line in lines)
            {
                yield return PlayOne(line, far && line.IsCat);
                float gap = Mathf.Max(DefaultGap, line.PauseAfter);
                if (AutoSkipAfter >= 0f) gap = Mathf.Min(gap, 0.3f);
                for (float t = 0f; t < gap; t += Time.deltaTime) yield return null;
            }
            Busy = false;
        }

        public IEnumerator PlayOne(VoiceBank.Line line, bool far)
        {
            Current = line;
            Played.Add(line.Id);
            var src = line.IsCat ? (far ? _catFar : _cat) : _seven;
            UI?.ShowSubtitle(line.IsCat ? "Cat" : "7", line.Text, line.IsCat, far);
            LineStarted?.Invoke(line);
            float len = line.Clip != null ? line.Clip.length : 1.2f + line.Text.Length * 0.06f;
            if (line.Clip != null) { src.clip = line.Clip; src.Play(); }
            _skip = false;
            float wait = AutoSkipAfter >= 0f ? Mathf.Min(len, AutoSkipAfter) : len;
            for (float t = 0f; t < wait && !_skip; t += Time.deltaTime) yield return null;
            if (_skip) src.Stop();
            _skip = false;
            UI?.HideSubtitle();
            Current = null;
        }
    }
}
