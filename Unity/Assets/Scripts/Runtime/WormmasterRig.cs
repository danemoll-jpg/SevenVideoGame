using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 12 — Wormmaster's motion, procedural on the rig from Tools/Blender/cleanup_r11.py (wormmaster): Mixamo-named
    /// Humanoid-compatible bones + two extra bones at the end of each tentacle arm (WmArmL1/L2, WmArmR1/R2) + HeadTip.
    /// No mouth and no lip-sync: the head tip swells and pulses with the loudness of his voice line (and glows white,
    /// faintly). Poses: <see cref="Pose.Dominant"/> (above his machinery, arms raised and slowly curling, swaying),
    /// <see cref="PulseGesture"/> (he activates a signal: arms sweep up and curl, the tip flares), <see cref="Pose.Contained"/>
    /// (after C07: his dominance gone - arms lowered and curled in, the body bowed). Never hurt, never killed.
    /// </summary>
    public class WormmasterRig : MonoBehaviour
    {
        public enum Pose { Dominant, Contained }
        public Pose Current = Pose.Dominant;
        public DialoguePlayer Dlg;
        public Light TipLight;
        /// <summary>The voice level 0..1 driving the head tip (for the check run's log).</summary>
        public float Voice { get; private set; }
        public float MaxTipScale { get; private set; } = 1f;
        // QF17 (2): the fight - lash (one arm drawn back, then whipped out, stretched), slam (both arms high, then down),
        // gliding (sway), dazed (slack)
        [System.NonSerialized] public float LashW, LashS, LashSide = 1f, SlamW, SlamS, Dazed, Moving;

        class B { public Transform T; public Quaternion Rest; public Vector3 Axis; public int Side; public int Index; }
        readonly List<B> _armL = new List<B>(), _armR = new List<B>(), _spine = new List<B>();
        B _head, _neck; Transform _tip; Vector3 _tipRest;
        float _pulse = -1f, _pose = 0f;
        readonly float[] _buf = new float[256];
        AudioSource _src;

        static Transform Find(Transform root, string name) { foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name || t.name.EndsWith(":" + name) || t.name == "mixamorig_" + name) return t; return null; }

        void Start()
        {
            var a = GetComponentInChildren<Animator>(); if (a != null) a.enabled = false;           // all motion is procedural
            var up = transform.up; var fwd = transform.forward; var right = transform.right;
            B Make(string n, int side, int i, bool spine)
            {
                var t = Find(transform, n); if (t == null) return null;
                Vector3 axisW;
                if (spine) axisW = right;                                                      // bow forward / back
                else
                {
                    var child = t.childCount > 0 ? t.GetChild(0) : null;
                    var dir = child != null ? (child.position - t.position).normalized : (t.position - t.parent.position).normalized;
                    axisW = Vector3.Cross(dir, up); if (axisW.sqrMagnitude < 1e-4f) axisW = fwd * side; axisW.Normalize();   // rotates the bone toward up
                }
                return new B { T = t, Rest = t.localRotation, Axis = Quaternion.Inverse(t.rotation) * axisW, Side = side, Index = i };
            }
            int k = 0;
            foreach (var n in new[] { "LeftArm", "LeftForeArm", "LeftHand", "WmArmL1", "WmArmL2" }) { var b = Make(n, 1, k++, false); if (b != null) _armL.Add(b); }
            k = 0;
            foreach (var n in new[] { "RightArm", "RightForeArm", "RightHand", "WmArmR1", "WmArmR2" }) { var b = Make(n, -1, k++, false); if (b != null) _armR.Add(b); }
            k = 0;
            foreach (var n in new[] { "Spine", "Spine1", "Spine2" }) { var b = Make(n, 0, k++, true); if (b != null) _spine.Add(b); }
            _neck = Make("Neck", 0, 0, true); _head = Make("Head", 0, 0, true);
            _tip = Find(transform, "HeadTip"); if (_tip != null) _tipRest = _tip.localScale;
            Debug.Log($"[Seven] Wormmaster rig: arms {_armL.Count}+{_armR.Count} bones (curl chain incl. WmArm extras), spine {_spine.Count}, head tip {(_tip != null ? "yes" : "MISSING")}");
        }

        /// <summary>He activates a signal pulse: the arms sweep up and curl, the head tip flares.</summary>
        public void PulseGesture() { _pulse = 0f; }

        void LateUpdate()
        {
            float t = Time.time, dt = Time.deltaTime;
            _pose = Mathf.MoveTowards(_pose, Current == Pose.Contained ? 1f : 0f, dt * 0.6f);
            float pulse = 0f;
            if (_pulse >= 0f) { _pulse += dt / 2.4f; pulse = Mathf.Sin(Mathf.Clamp01(_pulse) * Mathf.PI); if (_pulse >= 1f) _pulse = -1f; }
            // the voice: loudness of his line (no mouth: the head tip swells with it)
            float level = 0f;
            if (Dlg != null && Dlg.Busy && Dlg.Current != null && Dlg.Current.Speaker == "wormmaster")
            {
                if (_src == null || !_src.isPlaying || _src.clip != Dlg.Current.Clip) { _src = null; foreach (var s in FindObjectsByType<AudioSource>()) if (s.isPlaying && s.clip != null && s.clip == Dlg.Current.Clip) { _src = s; break; } }
                if (_src != null) { _src.GetOutputData(_buf, 0); float sum = 0f; foreach (var v in _buf) sum += v * v; level = Mathf.Clamp01(Mathf.Sqrt(sum / _buf.Length) * 9f); }
                else level = 0.35f + 0.25f * Mathf.Abs(Mathf.Sin(t * 9f));                  // subtitles only: a gentle pulse
            }
            Voice = Mathf.Lerp(Voice, level, 1f - Mathf.Exp(-dt * 18f));
            // arms: dominant = raised and slowly curling (Kyle's sketch: arms flung up and out); contained = lowered, curled in
            void Arm(List<B> arm, float phase, float lash)
            {
                foreach (var b in arm)
                {
                    float raise = b.Index == 0 ? Mathf.Lerp(32f + 22f * pulse, -14f, _pose) : 0f;
                    float curl = Mathf.Lerp(7f + 5f * Mathf.Sin(t * 0.8f + phase + b.Index * 0.7f) + 16f * pulse * b.Index / 4f, 22f, _pose);
                    if (b.Index == 0) curl = 0f;
                    // the fight: drawn back and coiled (wind-up), then whipped out straight and long; the slam: both high, then down
                    float lw = LashW * lash, ls = LashS * lash;
                    if (b.Index == 0) raise += 55f * lw - 85f * ls + 70f * SlamW - 95f * SlamS - 30f * Dazed;
                    else curl += 26f * lw - curl * ls - 6f * SlamS + 18f * Dazed;
                    b.T.localRotation = b.Rest * Quaternion.AngleAxis(raise + curl, b.Axis);
                    if (b.Index == 0) { float st = 1f + 0.9f * ls; b.T.localScale = new Vector3(st, st, st); }
                }
            }
            Arm(_armL, 0f, LashSide > 0f ? 1f : 0f); Arm(_armR, 1.7f, LashSide > 0f ? 0f : 1f);
            float sway = Mathf.Sin(t * 0.55f) * 2.5f * (1f - _pose) + Mathf.Sin(t * 3.1f) * 6f * Moving - 14f * SlamS + 8f * SlamW + 12f * Dazed;
            foreach (var b in _spine) b.T.localRotation = b.Rest * Quaternion.AngleAxis(Mathf.Lerp(-2f + sway * 0.4f, 9f, _pose) - 4f * pulse, b.Axis);
            if (_neck != null) _neck.T.localRotation = _neck.Rest * Quaternion.AngleAxis(Mathf.Lerp(3f * Mathf.Sin(t * 0.4f), 12f, _pose), _neck.Axis);
            if (_head != null) _head.T.localRotation = _head.Rest * Quaternion.AngleAxis(Mathf.Lerp(-6f * pulse + 4f * Voice, 16f, _pose), _head.Axis);
            if (_tip != null)
            {
                float k = 1f + 0.32f * Voice + 0.25f * pulse + 0.03f * Mathf.Sin(t * 2.1f);
                _tip.localScale = _tipRest * k; MaxTipScale = Mathf.Max(MaxTipScale, k);
                if (TipLight != null) { TipLight.transform.position = _tip.position; TipLight.intensity = (0.25f + 2.2f * Voice + 2.5f * pulse) * (1f - 0.6f * _pose); }
            }
        }
    }
}
