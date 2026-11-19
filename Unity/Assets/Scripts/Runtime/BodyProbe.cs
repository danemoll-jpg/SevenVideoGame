using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF12 (1): measures how still 7's body is through a stretch of a scene (check runs only). Every frame, after all
    /// animation / IK / LateUpdate work, it records the hips, chest and head relative to her root and her root itself; a
    /// frame where the hips jump more than 2.5 cm (or the root more than 2 cm) against the frame before - with no walk -
    /// is a "jerk". The first jerks are logged with everything that drives her body (animator state, IK weights, the wall
    /// push, her mode), and <see cref="Report"/> prints the totals: "body still (QF12): ... jerks N" (must be 0).
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public class BodyProbe : MonoBehaviour
    {
        public string Label = "";
        Animator _a; ChapterSeven _s; SevenTableIK _ik;
        Transform _hips, _chest, _head;
        Vector3 _lh, _lc, _lhead, _lroot; bool _has;
        int _frames, _jerks, _rootJerks; float _worst, _worstRoot; string _worstAt;
        readonly StringBuilder _first = new StringBuilder(); int _firstN;
        readonly HashSet<int> _states = new HashSet<int>(); int _stateChanges, _lastState;

        public static BodyProbe Begin(GameObject seven, string label)
        {
            var p = seven.GetComponent<BodyProbe>() ?? seven.AddComponent<BodyProbe>();
            p.Label = label; p.Reset0(); p.enabled = true; return p;
        }

        void Reset0()
        {
            _a = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(); _s = GetComponent<ChapterSeven>(); _ik = GetComponent<SevenTableIK>();
            _hips = _a != null && _a.isHuman ? _a.GetBoneTransform(HumanBodyBones.Hips) : null;
            _chest = _a != null && _a.isHuman ? _a.GetBoneTransform(HumanBodyBones.Chest) : null;
            _head = _a != null && _a.isHuman ? _a.GetBoneTransform(HumanBodyBones.Head) : null;
            _has = false; _frames = _jerks = _rootJerks = _stateChanges = 0; _worst = _worstRoot = 0f; _worstAt = null; _first.Clear(); _firstN = 0; _states.Clear(); _lastState = 0;
        }

        void LateUpdate()
        {
            if (_hips == null || Time.deltaTime <= 0f) return;
            var root = transform;
            Vector3 h = root.InverseTransformPoint(_hips.position), c = _chest != null ? root.InverseTransformPoint(_chest.position) : h, hd = _head != null ? root.InverseTransformPoint(_head.position) : h;
            Vector3 rp = root.position;
            int st = _a.GetCurrentAnimatorStateInfo(0).fullPathHash;
            if (_has)
            {
                _frames++;
                if (st != _lastState) { _stateChanges++; }
                float dh = Mathf.Max((h - _lh).magnitude, (c - _lc).magnitude * 0.8f, (hd - _lhead).magnitude * 0.6f);
                bool walking = _s != null && _s.Speed > 0.05f;
                float dr = walking ? 0f : (rp - _lroot).magnitude;
                if (dh > _worst) { _worst = dh; _worstAt = Describe(); }
                if (dr > _worstRoot) _worstRoot = dr;
                bool jerk = dh > 0.025f, rj = dr > 0.02f;
                if (jerk) _jerks++;
                if (rj) _rootJerks++;
                if ((jerk || rj) && _firstN < 6) { _firstN++; _first.Append($"\n    frame {_frames}: hips {dh * 100f:0.0} cm, root {dr * 100f:0.0} cm - {Describe()}"); }
            }
            _states.Add(st); _lastState = st;
            _lh = h; _lc = c; _lhead = hd; _lroot = rp; _has = true;
        }

        string Describe()
        {
            var sb = new StringBuilder();
            if (_s != null) sb.Append($"mode {_s.Current} state '{_s.State}'");
            if (_a != null) { var i = _a.GetCurrentAnimatorStateInfo(0); sb.Append($" t {i.normalizedTime:0.00}{(_a.IsInTransition(0) ? " (in transition)" : "")}"); }
            if (_ik == null) _ik = GetComponent<SevenTableIK>();
            if (_ik != null && _ik.isActiveAndEnabled) sb.Append($" tableIK R{_ik.RightW:0.0} L{_ik.LeftW:0.0} look {_ik.LookW:0.0}");
            if (_s != null) sb.Append($" wallPush {_s.WallPush.magnitude * 100f:0.0} cm");
            return sb.ToString();
        }

        public string Report()
        {
            enabled = false;
            return $"body still (QF12) '{Label}': {_frames} frames, jerks {_jerks} (hips > 2.5 cm in a frame), root jerks {_rootJerks} (standing, > 2 cm), worst {_worst * 100f:0.0} cm / root {_worstRoot * 100f:0.0} cm, animator states {_states.Count} / changes {_stateChanges}{(_worstAt != null ? $", worst at {_worstAt}" : "")}{_first}";
        }
    }
}
