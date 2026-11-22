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
    /// Round 13: after a line with a placement in <see cref="Reactions"/> the listener reacts (a quiet laugh / "hmm" /
    /// purr) in the gap, on the same voice treatment as that speaker's lines; the next line waits for it (+ a short gap),
    /// a skipped line skips its reaction, and a small default body cue plays where it is safe (StartCue).
    /// </summary>
    public class DialoguePlayer : MonoBehaviour
    {
        public VoiceBank Bank;
        public ChapterUI UI;
        [Tooltip("Gap after every line when the script asks for nothing longer (s).")]
        public float DefaultGap = 0.45f;

        AudioSource _seven, _cat, _catFar;
        AudioSource _rx7, _rxCat, _rxCatFar;     // Round 13: reaction sources (same treatment, quieter)
        bool _busy, _reacting, _sceneFar;
        float _rxUntil; string _rxName;
        static readonly List<DialoguePlayer> _all = new List<DialoguePlayer>();
        /// <summary>Seconds the last line's reaction held the conversation (delay + sound + gap; 0 = none).</summary>
        public float LastReactionSeconds { get; private set; }
        /// <summary>Reactions played by this player (check runs).</summary>
        public int ReactionsPlayed { get; private set; }
        public bool Busy { get => _busy; private set { if (_busy != value) _busyCount += value ? 1 : -1; _busy = value; } }
        static int _busyCount;
        /// <summary>Any dialogue playing anywhere (music ducks under it).</summary>
        public static bool AnyBusy => _busyCount > 0;
        void OnDestroy() { if (_busy) _busyCount--; _busy = false; _all.Remove(this); if (DemoBoot.CaptureDir != null && LinesTimed > 0) ReportOverlaps(gameObject.scene.name); }
        public VoiceBank.Line Current { get; private set; }
        public readonly List<string> Played = new List<string>();
        /// <summary>Check runs: skip lines automatically (seconds of each line actually waited).</summary>
        [System.NonSerialized] public float AutoSkipAfter = -1f;
        bool _skip;
        public event System.Action<VoiceBank.Line> LineStarted;
        /// <summary>QF23 check runs: every line started in this session (any player).</summary>
        public static int LinesStarted;

        void Awake()
        {
            _seven = MakeSource("Voice_7", false, false);
            _cat = MakeSource("Voice_Cat_Telepathy", true, false);
            _catFar = MakeSource("Voice_Cat_Far", true, true);
            foreach (var s in new[] { _seven, _cat, _catFar }) AudioKit.Track(s, AudioKit.Bus.Voice);   // Round 7: voice bus on top
            _rx7 = MakeSource("Reaction_7", false, false);
            _rxCat = MakeSource("Reaction_Cat_Telepathy", true, false);
            _rxCatFar = MakeSource("Reaction_Cat_Far", true, true);
            foreach (var s in new[] { _rx7, _rxCat, _rxCatFar }) AudioKit.Track(s, AudioKit.Bus.Voice, s.volume * Reactions.Volume);
            _all.Add(this);
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
            if (!(Busy || _reacting) || (DemoBoot.CaptureDir != null && !DemoBoot.PlayerSim) || PauseMenu.IsOpen) return;
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
            _sceneFar = far;
            foreach (var line in lines)
            {
                yield return PlayOne(line, far && line.IsCat);
                float gap = Mathf.Max(DefaultGap, line.PauseAfter);
                if (AutoSkipAfter >= 0f) gap = Mathf.Min(gap, 0.3f);
                gap -= LastReactionSeconds;      // Round 13: a reaction already filled (part of) the pause
                for (float t = 0f; t < gap; t += Time.deltaTime) yield return null;
            }
            _sceneFar = false;
            Busy = false;
        }

        // ---- QF24 (3): ONE dialogue line at a time, game-wide. Every line waits for the line before it (any player) and its
        // reaction to finish - lines fired by events / timers / a second coroutine queue instead of starting over another.
        // A line whose coroutine was stopped mid-way can never block the queue: it counts as speaking only until its own
        // length has passed. Check runs log VOICE QUEUE (a line that had to wait) and VOICE OVERLAP (must be 0).
        float _speakUntil;
        public static int Queued, Overlaps, LinesTimed;
        static readonly List<string> _overlapAt = new List<string>();
        static string SpeakingOrReacting()
        {
            foreach (var d in _all)
            {
                if (d == null) continue;
                if (d.Current != null && Time.time < d._speakUntil) return d.Current.Id;
                if (d._reacting) return "a reaction (" + (d._rxName ?? "due") + ")";
            }
            return null;
        }
        /// <summary>QF24 (3): the summary line for the check run (scene end).</summary>
        // ---- QF25 (3): no line plays twice in one scene unless the script repeats it, and no two lines with the same text
        static string _repScene; static readonly Dictionary<string, int> _repIds = new Dictionary<string, int>(); static readonly Dictionary<string, string> _repText = new Dictionary<string, string>();
        public static int RepeatIds, RepeatTexts, RepeatAllowed;
        static readonly List<string> _repAt = new List<string>();
        static readonly System.Text.RegularExpressions.Regex Bark = new System.Text.RegularExpressions.Regex(@"_(T|POS|LEAD|BEHIND|BARK|AD|CALL)\d", System.Text.RegularExpressions.RegexOptions.Compiled);
        static void NoteRepeat(VoiceBank.Line line)
        {
            var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "#" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
            if (sc != _repScene) { _repScene = sc; _repIds.Clear(); _repText.Clear(); }
            bool allowed = line.Repeat || (line.Id != null && Bark.IsMatch(line.Id));
            string key = line.Id ?? line.Text;
            _repIds.TryGetValue(key, out int n); _repIds[key] = n + 1;
            if (n > 0)
            {
                if (allowed) RepeatAllowed++;
                else { RepeatIds++; if (_repAt.Count < 16) _repAt.Add($"{line.Id} again in {sc}"); LogCapture($"VOICE REPEAT (QF25 3): {line.Id} played a second time in {sc} - '{line.Text}'"); }
            }
            var tk = (line.Speaker ?? "") + "|" + (line.Text ?? "").Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(line.Text) && _repText.TryGetValue(tk, out var other) && other != line.Id && !allowed)
            { RepeatTexts++; if (_repAt.Count < 16) _repAt.Add($"{line.Id} = {other} (same text)"); LogCapture($"VOICE REPEAT (QF25 3): {line.Id} has the same text as {other} (already played in {sc}) - '{line.Text}'"); }
            else _repText[tk] = line.Id;
        }
        public static void ReportOverlaps(string where)
        {
            LogCapture($"VOICE REPEAT (QF25 3) {where}: a line id played twice {RepeatIds}, two ids with the same text {RepeatTexts} (both must be 0); repeats the script asks for {RepeatAllowed}{(_repAt.Count > 0 ? " - " + string.Join("; ", _repAt) : "")}");
            LogCapture($"VOICE OVERLAP (QF24 3) {where}: lines {LinesTimed}, queued behind another {Queued}, overlapping {Overlaps} (must be 0){(_overlapAt.Count > 0 ? " - " + string.Join("; ", _overlapAt) : "")}");
        }

        public IEnumerator PlayOne(VoiceBank.Line line, bool far)
        {
            {
                string waited = null; float q0 = Time.time;
                for (string w = SpeakingOrReacting(); w != null; w = SpeakingOrReacting()) { waited = waited ?? w; yield return null; }
                if (waited != null) { Queued++; LogCapture($"VOICE QUEUE (QF24 3): {line.Id} waited {Time.time - q0:0.00} s for {waited} to finish"); }
                foreach (var d in _all)
                    if (d != null && d.Current != null && d != this && Time.time < d._speakUntil - 0.02f)
                    { Overlaps++; if (_overlapAt.Count < 12) _overlapAt.Add($"{line.Id} over {d.Current.Id} in {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}"); }
            }
            LinesTimed++;
            LastReactionSeconds = 0f;
            StopReactionsUnder(line);
            Current = line;
            Played.Add(line.Id);
            NoteRepeat(line);
            var src = line.IsCat ? (far ? _catFar : _cat) : line.Speaker == "nela" || line.Speaker == "juniper" || line.Speaker == "duchess" || line.Speaker == "steward" ? _cat : _seven;   // Round 18: every Cattalus cat speaks telepathically (Panther's treatment); the announcer is the square's PA   // Nela (the mothership) speaks telepathically too: Panther's treatment
            UI?.ShowSubtitle(line.Label ?? (line.IsCat ? "Cat" : "7"), line.Text, line.IsCat, far);
            LinesStarted++;
            LineStarted?.Invoke(line);
            DialogueFraming.Check(line);          // QF18 (7): the speaker must be visible in a dialogue shot
            float len = line.Clip != null ? line.Clip.length : 1.2f + line.Text.Length * 0.06f;
            if (line.Clip != null) { src.clip = line.Clip; src.Play(); }
            _skip = false;
            float wait = AutoSkipAfter >= 0f ? Mathf.Min(len, AutoSkipAfter) : len;
            _speakUntil = Time.time + wait + 0.25f;
            for (float t = 0f; t < wait && !_skip; t += Time.deltaTime) yield return null;
            bool skipped = _skip || wait < len - 0.01f;   // skipped by the player or cut short by a check run's auto-skip
            if (_skip || skipped) src.Stop();             // QF24 (3): a skipped line's audio stops (check runs' auto-skip too)
            _skip = false;
            UI?.HideSubtitle();
            Current = null; _speakUntil = 0f;
            // Round 13: the listener's reaction in the gap after the line (none if the line was skipped)
            var rx = Reactions.After(line.Id);
            if (rx != null)
            {
                if (skipped) LogCapture($"reaction: after {line.Id} {rx.who} {rx.kind} skipped (line skipped)");
                else yield return PlayReaction(line, rx, far || _sceneFar);
            }
        }

        // ---- Round 13: reactions -------------------------------------------------------------------------------------

        IEnumerator PlayReaction(VoiceBank.Line line, Reactions.Placement p, bool far)
        {
            var clip = Reactions.Pick(p.who, p.kind, out int take);
            if (clip == null) { LogCapture($"reaction: after {line.Id} {p.who} {p.kind} - no take in Resources/Reactions (skipped)"); yield break; }
            bool cat = p.who == "cat";
            var src = cat ? (far ? _rxCatFar : _rxCat) : _rx7;
            float t0 = Time.time;
            _reacting = true;
            _skip = false;
            for (float t = 0f; t < p.delay && !_skip; t += Time.deltaTime) yield return null;
            bool played = false;
            if (!_skip)
            {
                // never over a spoken line: wait (and report it) if any dialogue player has a line in progress
                string over = SpeakingLine();
                if (over != null)
                {
                    LogCapture($"REACTION OVERLAP: after {line.Id} {p.who} {p.kind} would play over '{over}' - waited for it");
                    for (float t = 0f; t < 6f && SpeakingLine() != null && !_skip; t += Time.deltaTime) yield return null;
                }
            }
            if (!_skip)
            {
                src.clip = clip; src.Play(); played = true; ReactionsPlayed++;
                _rxUntil = Time.time + clip.length; _rxName = clip.name;
                Reactions.Fire(p.who, p.kind, p.cue);
                StartCue(p.who, p.cue, clip.length);
                for (float t = 0f; t < clip.length && !_skip; t += Time.deltaTime) yield return null;
                if (_skip) src.Stop();
                _rxUntil = 0f;
                for (float t = 0f; t < Reactions.GapAfter && !_skip; t += Time.deltaTime) yield return null;
            }
            bool skipped = _skip;
            _skip = false;
            _reacting = false;
            LastReactionSeconds = Time.time - t0;
            if (played) LogCapture($"reaction: after {line.Id} {p.who} {p.kind} take {take} ({clip.length:0.00} s), next line waited {LastReactionSeconds:0.00} s{(skipped ? " (skipped)" : "")}");
            else LogCapture($"reaction: after {line.Id} {p.who} {p.kind} skipped during its delay");
        }

        /// <summary>The id of a line some dialogue player is playing right now (game time: from its start until its
        /// wait ends; accelerated check runs keep the same model), or null.</summary>
        static string SpeakingLine()
        {
            foreach (var d in _all)
                if (d != null && d.Current != null) return d.Current.Id;
            return null;
        }

        /// <summary>A line is starting: no reaction may still be due under it (game time; report it - must never happen).
        /// Any reaction audio still ringing (accelerated check runs play audio at real speed) is stopped quietly.</summary>
        void StopReactionsUnder(VoiceBank.Line line)
        {
            foreach (var d in _all)
            {
                if (d == null) continue;
                if (Time.time < d._rxUntil - 0.02f)
                    LogCapture($"REACTION OVERLAP: line {line.Id} started {d._rxUntil - Time.time:0.00} s before the reaction ({d._rxName}) ended - reaction stopped");
                d._rxUntil = 0f;
                foreach (var s in new[] { d._rx7, d._rxCat, d._rxCatFar })
                    if (s != null && s.isPlaying) s.Stop();
            }
        }

        static void LogCapture(string s)
        {
            if (DemoBoot.CaptureDir == null) return;
            if (ChapterCapture.I != null) ChapterCapture.I.Log(s);
            else Debug.Log("[Seven] " + s);
        }

        // ---- default body cue: a small additive head / shoulder / ear move on top of the animation, only where the
        // seated-conversation helpers are present and enabled (SevenTableIK for 7, CatPoseOverlay with ear bones for the
        // cat). Never replaces a pose: it multiplies a few degrees onto the animated bones in LateUpdate (after the IK
        // pass) and takes itself back off if the animator did not write that bone in a frame. "tail" / "none" = the
        // event only (Reactions.OnReaction), as are 7's cues outside the galley.
        class Cue { public Transform Bone, Root; public Vector3 Axis; public float Deg, Dur, T; public int Shape; public Quaternion Base, Set; public bool HasSet; }
        readonly List<Cue> _cues = new List<Cue>();

        void StartCue(string who, string cue, float soundLen)
        {
            if (string.IsNullOrEmpty(cue) || cue == "none" || cue == "tail") return;
            _cues.Clear();
            if (who == "seven")
            {
                var ik = FindAnyObjectByType<SevenTableIK>();
                if (ik == null || !ik.isActiveAndEnabled) return;
                var an = ik.GetComponent<Animator>();
                if (an == null || !an.isActiveAndEnabled || !an.isHuman) return;
                var root = ik.transform;
                var head = an.GetBoneTransform(HumanBodyBones.Head);
                switch (cue)
                {
                    case "head_tilt": AddCue(head, root, 0, 5f, 1.3f, 1); break;            // roll, held
                    case "nod": AddCue(head, root, 1, 4.5f, 0.9f, 2); break;                 // pitch, two dips
                    case "look_down": AddCue(head, root, 1, 6f, 1.6f, 1); break;             // pitch down, held
                    case "shoulders":
                        float d = Mathf.Clamp(soundLen + 0.2f, 0.6f, 1.4f);
                        AddCue(an.GetBoneTransform(HumanBodyBones.RightShoulder), root, 0, 3.5f, d, 0);
                        AddCue(an.GetBoneTransform(HumanBodyBones.LeftShoulder), root, 0, -3.5f, d, 0);
                        break;
                }
            }
            else if (cue == "ears")
            {
                var cp = FindAnyObjectByType<CatPoseOverlay>();
                if (cp == null || !cp.isActiveAndEnabled || !cp.HasEars) return;
                foreach (var t in cp.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "earL") AddCue(t, cp.transform, 2, -14f, 0.5f, 2);
                    else if (t.name == "earR") AddCue(t, cp.transform, 2, 14f, 0.5f, 2);
                }
            }
        }

        /// <param name="axis">0 = the root's forward (roll), 1 = its right (pitch, + = down), 2 = its up (swivel)</param>
        /// <param name="shape">0 = one bump, 1 = ease in / hold / ease out, 2 = two quick dips</param>
        void AddCue(Transform bone, Transform root, int axis, float deg, float dur, int shape)
        {
            if (bone == null || root == null) return;
            _cues.Add(new Cue { Bone = bone, Root = root, Axis = new Vector3(axis == 1 ? 1 : 0, axis == 2 ? 1 : 0, axis == 0 ? 1 : 0), Deg = deg, Dur = dur, Shape = shape });
        }

        static float Envelope(int shape, float u)
        {
            u = Mathf.Clamp01(u);
            switch (shape)
            {
                case 1: return Mathf.SmoothStep(0f, 1f, Mathf.Min(u / 0.25f, (1f - u) / 0.3f));
                case 2: return u < 0.5f ? Mathf.Sin(u / 0.5f * Mathf.PI) : 0.6f * Mathf.Sin((u - 0.5f) / 0.5f * Mathf.PI);
                default: return Mathf.Sin(u * Mathf.PI);
            }
        }

        void LateUpdate()
        {
            for (int i = _cues.Count - 1; i >= 0; i--)
            {
                var c = _cues[i];
                if (c.Bone == null || c.Root == null) { _cues.RemoveAt(i); continue; }
                // the animator did not write this bone since our last frame: take our offset back off first
                if (c.HasSet && c.Bone.localRotation == c.Set) c.Bone.localRotation = c.Base;
                c.T += Time.deltaTime;
                if (c.T >= c.Dur) { _cues.RemoveAt(i); continue; }
                float a = c.Deg * Envelope(c.Shape, c.T / c.Dur);
                var axis = c.Root.TransformDirection(c.Axis);
                c.Base = c.Bone.localRotation;
                c.Bone.rotation = Quaternion.AngleAxis(a, axis) * c.Bone.rotation;
                c.Set = c.Bone.localRotation; c.HasSet = true;
            }
        }
    }
}
