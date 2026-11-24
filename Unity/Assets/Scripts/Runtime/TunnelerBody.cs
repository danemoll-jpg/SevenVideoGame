using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// P2 (9): "Tunneler barely moves, apart from his head." His model (Tripo, no rig; the Mixamo files in Art/Mixamo/tunneler are
    /// Y Bot motion without his skin) is skinned at build time (FightDemoBuilderTunnelerBody: a height blend up his stacked rings -
    /// root / hips / spine / chest / neck - and each arm a shoulder / elbow / claw chain found by flood-filling the arm off the
    /// body; the drill head rides the neck bone). This drives those bones after the animator, everywhere he appears (Europa's
    /// screen, the finale's entrance, FIN_08, the fight, the barrier, his capture):
    ///  - never frozen: slow breathing through the chest, a weight shift side to side in the hips with the spine counter-rolling,
    ///    small drifts of the neck (the drill), the arms hanging with a slight swing;
    ///  - the rig: when what carries him moves (the hover rig bobbing, rocking, lunging, staggering) his body lags with inertia -
    ///    leaning against the acceleration and settling; when the rig drops after a stun he slumps and sways, dazed;
    ///  - talking: his voice (the shared DrillHead's Talk level) pumps the chest forward on loud syllables and lifts the forearms a
    ///    little; plus one gesture per key line (<see cref="Gestures"/>): pointing (at her, at the light well), a sweep toward
    ///    the terminals / his workers, a fist brought down, arms spread;
    ///  - restrained (his capture): <see cref="Locked"/> - breathing only, arms held.
    /// Check runs log `TUNNELER BODY (P2 9)` per line (chest / arm movement in degrees, the gesture) and a summary; each gesture takes
    /// a side capture at its peak (`tun_gesture_<line>`).
    /// </summary>
    [DefaultExecutionOrder(240)]   // after the animator, before DrillHead (250) reads its parent
    public class TunnelerBody : MonoBehaviour
    {
        public Transform Root, Hips, Spine, Chest, Neck, UpperL, ForeL, HandL, UpperR, ForeR, HandR;
        public DrillHead Drill;
        public bool Locked;
        /// <summary>Where a gesture's named target is ("her", "platform", "terminals", "camera"); set by the director. Unset or null =
        /// in front of him (on Europa's screen: toward the camera = toward her).</summary>
        public System.Func<string, Vector3?> Target;
        public enum Kind { None, Point, Sweep, Fist, Spread, Lean }
        /// <summary>The key-line gestures (line id -> what and toward what).</summary>
        public static readonly Dictionary<string, (Kind k, string at)> Gestures = new Dictionary<string, (Kind, string)>
        {
            { "EUR_08_01", (Kind.Point, "her") },          // You.
            { "EUR_08_03", (Kind.Lean, "her") },           // You were supposed to remain where you were put.
            { "EUR_08_06", (Kind.Fist, "her") },           // Leave my operation alone.
            { "EUR_08_08", (Kind.Sweep, "terminals") },    // My workers have obligations.
            { "EUR_08_10", (Kind.Spread, "her") },         // You have no idea what it takes to keep all of this running.
            { "EUR_11_01", (Kind.Fist, "her") },           // You think this changes anything?
            { "EUR_11_03", (Kind.Point, "her") },          // You cannot follow me everywhere.
            { "FIN_07_09", (Kind.Sweep, "terminals") },    // Return to your stations.
            { "FIN_08_01", (Kind.Point, "platform") },     // Shut it down.
            { "FIN_08_03", (Kind.Sweep, "terminals") },    // Those workers answer to me.
            { "FIN_08_05", (Kind.Fist, "her") },           // You don't understand what you're interfering with.
        };

        Quaternion[] _rest; Transform[] _b;
        Vector3 _lastPos, _vel, _acc, _lean; float _t, _seed;
        Kind _g; Vector3 _gAt; float _gW, _gT; bool _gRight = true; string _gLine; float _gTarget;
        VoiceBank.Line _line; DialoguePlayer _dlg; TunnelerRig _rig;
        // measures (check runs)
        float _lineChest, _lineArm; Quaternion _lc, _la; bool _shot, _laSet;
        static readonly List<string> _report = new List<string>();
        public static int Lines, LinesMoved, FrozenFrames, Frames;

        void Awake()
        {
            _b = new[] { Root, Hips, Spine, Chest, Neck, UpperL, ForeL, HandL, UpperR, ForeR, HandR };
            _rest = new Quaternion[_b.Length];
            for (int i = 0; i < _b.Length; i++) if (_b[i] != null) _rest[i] = _b[i].localRotation;
            _seed = Random.Range(0f, 10f); _lastPos = transform.position;
            if (Drill == null) Drill = GetComponentInChildren<DrillHead>(true);
        }
        void Start()
        {
            _dlg = FindFirstObjectByType<DialoguePlayer>();
            if (_dlg != null) _dlg.LineStarted += OnLine;
            _rig = GetComponentInParent<TunnelerRig>();
        }
        void OnDestroy() { if (_dlg != null) _dlg.LineStarted -= OnLine; }

        void OnLine(VoiceBank.Line l)
        {
            if (l == null || l.Speaker != "tunneler") return;
            _line = l; _lineChest = _lineArm = 0f; _shot = false;
            if (Chest != null) _lc = Chest.rotation; _la = Quaternion.identity; _laSet = false;
            if (!Locked && l.Id != null && Gestures.TryGetValue(l.Id, out var g)) Gesture(g.k, Resolve(g.at), l.Id);
            else { _g = Kind.None; _gLine = l.Id; }
        }
        Vector3 Resolve(string at)
        {
            var p = Target?.Invoke(at);
            if (p.HasValue) return p.Value;
            var c = Chest != null ? Chest.position : transform.position + Vector3.up * 1.7f;
            if (at == "terminals") return c + transform.forward * 2.5f + transform.right * 3f;     // off to his side
            return c + transform.forward * 4f;                                                        // in front of him (the camera / her)
        }
        /// <summary>Play a gesture toward a world point (it eases in, holds through the line, eases out after it).</summary>
        public void Gesture(Kind k, Vector3 at, string line = null)
        {
            _g = k; _gAt = at; _gT = 0f; _gLine = line; _gTarget = 1f;
            var c = Chest != null ? Chest.position : transform.position;
            _gRight = Vector3.Dot(at - c, transform.right) >= -0.3f;                                  // the arm on the target's side
        }

        void LateUpdate()
        {
            if (_b == null || Hips == null || Chest == null) return;
            float dt = Mathf.Max(1e-4f, Time.deltaTime); _t += dt;
            // the carrier's motion -> inertia (lean against the acceleration, settle)
            var v = (transform.position - _lastPos) / dt; _lastPos = transform.position;
            if (v.sqrMagnitude > 400f) v = _vel;                                                      // a teleport / cut: ignore
            _acc = Vector3.Lerp(_acc, (v - _vel) / dt, 1f - Mathf.Exp(-dt * 6f)); _vel = v;
            var wantLean = Vector3.ClampMagnitude(-new Vector3(_acc.x, 0f, _acc.z) * 0.9f, 12f);
            _lean = Vector3.Lerp(_lean, wantLean, 1f - Mathf.Exp(-dt * 3f));
            bool dazed = _rig != null && _rig.Dropped;
            float talk = Drill != null ? Mathf.Clamp01(Drill.Talk) : 0f;
            bool speaking = _line != null && _dlg != null && _dlg.Current == _line;
            if (!speaking && _line != null && _gTarget > 0f) _gTarget = 0f;                        // the line ended: the gesture eases out
            if (!speaking && _line != null) EndLine();
            _gT += dt; _gW = Mathf.MoveTowards(_gW, _g == Kind.None ? 0f : _gTarget, dt / 0.45f);
            if (_gW <= 0f && _gTarget <= 0f) _g = Kind.None;
            float g = Mathf.SmoothStep(0f, 1f, _gW);
            // reset to the rest pose (the model has no animator: the bones are ours)
            for (int i = 0; i < _b.Length; i++) if (_b[i] != null) _b[i].localRotation = _rest[i];
            float s = _t + _seed;
            var fwd = transform.forward; var right = transform.right; var up = transform.up;
            // breathing + weight shift + counter-roll (never frozen)
            float breath = Mathf.Sin(s * 2f * Mathf.PI * 0.23f);
            float shift = Mathf.Sin(s * 2f * Mathf.PI * 0.11f) + 0.35f * Mathf.Sin(s * 2f * Mathf.PI * 0.27f + 1.3f);
            float lockK = Locked ? 0.35f : 1f;
            Rot(Hips, fwd, shift * 2.6f * lockK);
            Rot(Spine, fwd, -shift * 1.6f * lockK);
            Rot(Chest, right, breath * 1.4f - (dazed ? 14f : 0f));
            Rot(Neck, fwd, Mathf.Sin(s * 0.53f) * 2.2f * lockK); Rot(Neck, right, Mathf.Sin(s * 0.41f + 2f) * 2f * lockK + (dazed ? 10f : 0f));
            if (dazed) { Rot(Spine, fwd, Mathf.Sin(s * 1.7f) * 6f); Rot(Chest, fwd, Mathf.Sin(s * 1.3f + 1f) * 5f); }
            // inertia from the rig
            var axis = Vector3.Cross(Vector3.up, _lean); float ang = _lean.magnitude;
            if (ang > 0.05f && !Locked) { Spine.rotation = Quaternion.AngleAxis(ang * 0.55f, axis.normalized) * Spine.rotation; Chest.rotation = Quaternion.AngleAxis(ang * 0.45f, axis.normalized) * Chest.rotation; }
            // talking: the chest pumps forward on loud syllables, the forearms lift a little
            if (!Locked) { Rot(Chest, right, talk * 4.5f); Rot(Spine, right, talk * 1.5f); }
            // the arms: a slight hanging swing (and held by the body when locked)
            foreach (var (u, f, sgn) in new[] { (UpperL, ForeL, -1f), (UpperR, ForeR, 1f) })
            {
                Rot(u, right, Mathf.Sin(s * 0.9f + sgn) * 2.5f * lockK);
                if (!Locked) Rot(f, right, -talk * 9f * (sgn > 0 ? 1f : 0.7f));
            }
            // the gesture
            if (g > 0.001f && !Locked) DoGesture(g, fwd, right);
            Measure(speaking);
        }

        void DoGesture(float w, Vector3 fwd, Vector3 right)
        {
            var u = _gRight ? UpperR : UpperL; var f = _gRight ? ForeR : ForeL; var h = _gRight ? HandR : HandL;
            var u2 = _gRight ? UpperL : UpperR; var f2 = _gRight ? ForeL : ForeR;
            var dir = (_gAt - (u != null ? u.position : Chest.position)); if (dir.sqrMagnitude < 0.01f) dir = fwd; dir.Normalize();
            switch (_g)
            {
                case Kind.Point:   // the arm raised and straight at the target, a small jab on the start, the chest turned toward it
                {
                    float jab = Mathf.Exp(-_gT * 4f) * Mathf.Sin(_gT * 14f) * 0.15f;
                    Aim(u, f, dir, w); Aim(f, h, dir, w); TurnChest(dir, w * 0.5f);
                    if (f != null) f.rotation = Quaternion.AngleAxis(jab * 30f, right) * f.rotation;
                    break;
                }
                case Kind.Sweep:   // the arm swings from across his front out toward the target over ~1.2 s, palm open
                {
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_gT / 1.2f));
                    var startDir = (fwd * 0.8f - (_gRight ? 1f : -1f) * right * 0.4f - Vector3.up * 0.2f).normalized;
                    var d = Vector3.Slerp(startDir, (dir + Vector3.down * 0.15f).normalized, k);
                    Aim(u, f, d, w * 0.9f); Aim(f, h, d, w * 0.8f); TurnChest(d, w * 0.35f);
                    break;
                }
                case Kind.Fist:    // the forearm comes up, then down hard (a fist brought down on the yoke), the chest leaning in
                {
                    float up = Mathf.Clamp01(_gT / 0.45f), down = Mathf.Clamp01((_gT - 0.45f) / 0.18f);
                    var raise = (fwd * 0.6f + Vector3.up * 0.8f).normalized; var slam = (fwd * 0.9f + Vector3.down * 0.45f).normalized;
                    var d = Vector3.Slerp(raise, slam, Mathf.SmoothStep(0f, 1f, down));
                    Aim(u, f, (fwd * 0.7f + Vector3.down * 0.4f).normalized, w * 0.8f); Aim(f, h, d, w * Mathf.Max(up, 0.6f));
                    if (h != null) h.rotation = Quaternion.AngleAxis(-35f * w, right) * h.rotation;   // the claw closes
                    Rot(Chest, right, 7f * w * down + 3f * w); Rot(Spine, right, 3f * w);
                    break;
                }
                case Kind.Spread:  // both arms open forward and out, the chest lifted
                {
                    var l = (fwd * 0.7f - right * 0.7f + Vector3.up * 0.05f).normalized; var r = (fwd * 0.7f + right * 0.7f + Vector3.up * 0.05f).normalized;
                    Aim(UpperL, ForeL, l, w * 0.75f); Aim(ForeL, HandL, l, w * 0.6f); Aim(UpperR, ForeR, r, w * 0.75f); Aim(ForeR, HandR, r, w * 0.6f);
                    Rot(Chest, right, -4f * w);
                    break;
                }
                case Kind.Lean:    // he leans in toward her, one forearm raised
                {
                    TurnChest(dir, w * 0.4f); Rot(Spine, right, 6f * w); Rot(Chest, right, 6f * w);
                    Aim(f, h, (dir + Vector3.up * 0.3f).normalized, w * 0.5f);
                    break;
                }
            }
            if (f2 != null && (_g == Kind.Point || _g == Kind.Sweep || _g == Kind.Fist)) Rot(f2, right, -6f * w);   // the other forearm stirs too
        }

        /// <summary>Turn <paramref name="bone"/> so the direction to <paramref name="child"/> points along <paramref name="dir"/> (weight w).</summary>
        /// <summary>The upper arms turn at most this far from the hang (his upper arm is part of the belly's side - a bigger swing stretches
        /// it); the forearm and the claw carry the rest of a gesture.</summary>
        public float UpperArmMax = 38f;
        void Aim(Transform bone, Transform child, Vector3 dir, float w)
        {
            if (bone == null || child == null || w <= 0f) return;
            var cur = child.position - bone.position; if (cur.sqrMagnitude < 1e-6f) return;
            var r = Quaternion.FromToRotation(cur.normalized, dir.normalized);
            if (bone == UpperL || bone == UpperR) { r.ToAngleAxis(out float ang, out var ax); if (ang > 180f) ang -= 360f; if (Mathf.Abs(ang) > UpperArmMax) r = Quaternion.AngleAxis(Mathf.Sign(ang) * UpperArmMax, ax); }
            bone.rotation = Quaternion.Slerp(bone.rotation, r * bone.rotation, w);
        }
        void TurnChest(Vector3 dir, float w)
        {
            var flat = Vector3.ProjectOnPlane(dir, Vector3.up); if (flat.sqrMagnitude < 0.01f) return;
            float a = Mathf.Clamp(Vector3.SignedAngle(Vector3.ProjectOnPlane(transform.forward, Vector3.up), flat, Vector3.up), -35f, 35f);
            Spine.rotation = Quaternion.AngleAxis(a * 0.4f * w, Vector3.up) * Spine.rotation;
            Chest.rotation = Quaternion.AngleAxis(a * 0.6f * w, Vector3.up) * Chest.rotation;
        }
        static void Rot(Transform t, Vector3 axis, float deg) { if (t != null && Mathf.Abs(deg) > 1e-4f) t.rotation = Quaternion.AngleAxis(deg, axis) * t.rotation; }

        // ------------------------------------------------------------------ measures (check runs)
        Quaternion _prevChest, _prevArm; bool _prev;
        void Measure(bool speaking)
        {
            if (DemoBoot.CaptureDir == null) return;
            bool visible = false; foreach (var r in GetComponentsInChildren<Renderer>()) if (r.isVisible) { visible = true; break; }
            if (visible)
            {
                Frames++;
                if (_prev && Quaternion.Angle(_prevChest, Chest.rotation) < 0.001f && Quaternion.Angle(_prevArm, UpperR != null ? UpperR.rotation : Chest.rotation) < 0.001f && Time.deltaTime > 0f) FrozenFrames++;
                _prevChest = Chest.rotation; _prevArm = UpperR != null ? UpperR.rotation : Chest.rotation; _prev = true;
            }
            if (!speaking) return;
            _lineChest = Mathf.Max(_lineChest, Quaternion.Angle(_lc, Chest.rotation));
            var arm = _gRight ? UpperR : UpperL;
            if (arm != null) { if (!_laSet) { _la = arm.rotation; _laSet = true; } _lineArm = Mathf.Max(_lineArm, Quaternion.Angle(_la, arm.rotation)); }
            // the gesture's peak: one side capture of his body
            if (!_shot && _g != Kind.None && _gW > 0.95f && _gT > 0.7f && ChapterCapture.I != null)
            {
                _shot = true;
                var c = Chest.position; var side = transform.right * (_gRight ? 1f : -1f);
                ChapterCapture.I.StartCoroutine(GestureShot("tun_gesture_" + (_gLine ?? "line").ToLowerInvariant() + "_" + _g.ToString().ToLowerInvariant(), c + transform.forward * 4.2f + side * 2.2f + Vector3.up * 0.3f, c + Vector3.down * 0.2f));
            }
        }
        System.Collections.IEnumerator GestureShot(string name, Vector3 cam, Vector3 at)
        {
            var keep = ImageGate.PushSubject();
            ImageGate.Expect(transform, "Tunneler gesturing (his body)", 0f, "tun_gesture_");
            yield return ChapterCapture.I.ShotFrom(name, cam, at, 45f);
            ImageGate.PopSubject(keep);
        }
        void EndLine()
        {
            if (DemoBoot.CaptureDir != null && _line != null)
            {
                Lines++; if (_lineChest > 2f || _lineArm > 4f) LinesMoved++;
                _report.Add($"TUNNELER BODY (P2 9) {_line.Id}: chest moved {_lineChest:0.0} deg, arm {_lineArm:0.0} deg, gesture {(Gestures.TryGetValue(_line.Id ?? "", out var g) && !Locked ? g.k + " at " + g.at : Locked ? "none (restrained)" : "none (talk emphasis only)")}");
                ChapterCapture.I?.Log(_report[_report.Count - 1]);
            }
            _line = null;
        }
        public static string Summary() => $"TUNNELER BODY (P2 9): {Lines} of his lines seen, {LinesMoved} with his body clearly moving (chest > 2 deg or an arm > 4 deg); frozen frames while on screen {FrozenFrames} of {Frames} (must be 0)";
    }
}
