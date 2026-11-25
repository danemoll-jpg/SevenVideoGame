using UnityEngine;

namespace Seven
{
    /// <summary>Round 20: 7 on her bike in the racing mode - the story game's riding pose, the same way it is made there
    /// (ChapterSevenRide): her "ride" state (the Blender-made racing tuck), and in the IK pass her BODY placed on the saddle (the ride
    /// clip carries its body position ~100x too far, so the body is set here, never her root moved by metres), the upper body laid
    /// forward (the tuck + a little more while a palm misses its grip), the palms onto the grips (wrist placed from the palm offset
    /// measured on the pose), the feet on the side panels with the knees along the tank, the gaze ahead; after the animation the hips'
    /// small remaining error is learned into a bounded calibration and the shoulders roll forward the few degrees that put each palm
    /// on its grip. Check runs read <see cref="HipsGap"/> / <see cref="PalmGaps"/>.</summary>
    public class GcSevenRider : MonoBehaviour
    {
        public Transform Seat, GripL, GripR, FootL, FootR, Root;
        public float KneeWidth = 0.24f;
        /// <summary>Round 22: the same riding pose for every humanoid rider (Nela, Viper, the Machinist astride the Scrambler jet): K = the
        /// rider's size against 7's (every body offset scales with it), Who = the rider's name in the check lines.</summary>
        public float K = 1f; public string Who = "7";
        /// <summary>The forward lean of the tuck (7 on her racing bike 42; the Scrambler jet's riders sit taller, 26).</summary>
        public float Tuck = 42f;
        /// <summary>The base pose's animator state: "ride" (7's Blender-made racing tuck) or "sitting_idle" (upright, for a big rider).</summary>
        public string Pose = "ride";
        Animator _an; Vector3 _bodyAdj, _off; float _reachLean; bool _ok;
        public float HipsGap { get; private set; } = 1f;
        public Vector2 PalmGaps { get; private set; }
        const float MaxReachLean = 34f, ShoulderRollMax = 25f;
        void Start()
        {
            _an = GetComponent<Animator>(); if (_an == null) return;
            _an.applyRootMotion = false; _an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            string st = Has(Pose) ? Pose : Has("ride") ? "ride" : Has("sitting_idle") ? "sitting_idle" : "idle"; _an.Play(st, 0, 0f);
            _ok = Seat != null;
        }
        bool Has(string s) => _an.HasState(0, Animator.StringToHash(s));
        Transform Bone(HumanBodyBones b) => _an != null ? _an.GetBoneTransform(b) : null;

        void OnAnimatorIK(int layer)
        {
            if (!_ok || _an == null) return;
            var up = Seat.up; var fw = Seat.forward; var rt = Seat.right;
            _an.bodyPosition = Seat.position + Seat.rotation * (_bodyAdj + Vector3.up * 0.11f * K);
            _an.bodyRotation = Quaternion.AngleAxis(Tuck + _reachLean, rt) * _an.bodyRotation;
            foreach (var (goal, hand, mid, grip) in new[] { (AvatarIKGoal.LeftHand, HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleProximal, GripL), (AvatarIKGoal.RightHand, HumanBodyBones.RightHand, HumanBodyBones.RightMiddleProximal, GripR) })
            {
                if (grip == null) continue; var h = Bone(hand); var m = Bone(mid);
                var palm = h != null && m != null ? (m.position - h.position) * 0.5f : Vector3.zero;
                _an.SetIKPositionWeight(goal, 1f); _an.SetIKPosition(goal, grip.position + up * 0.015f - palm); _an.SetIKRotationWeight(goal, 0f);
            }
            foreach (var (goal, hint, foot, sgn) in new[] { (AvatarIKGoal.LeftFoot, AvatarIKHint.LeftKnee, FootL, -1f), (AvatarIKGoal.RightFoot, AvatarIKHint.RightKnee, FootR, 1f) })
            {
                if (foot == null) continue;
                _an.SetIKPositionWeight(goal, 1f); _an.SetIKPosition(goal, foot.position + up * 0.06f * K);
                _an.SetIKRotationWeight(goal, 0.6f); _an.SetIKRotation(goal, Quaternion.LookRotation(-fw * 0.6f - up * 0.8f, fw));
                _an.SetIKHintPositionWeight(hint, 0.8f); _an.SetIKHintPosition(hint, Seat.position + (fw * 0.40f + rt * sgn * (KneeWidth + 0.10f) - up * 0.12f) * K);
            }
            _an.SetLookAtWeight(0.75f, 0f, 0.8f, 1f, 0.5f); _an.SetLookAtPosition(Seat.position + fw * 14f + up * 1.3f);
        }

        void LateUpdate()
        {
            if (!_ok || _an == null) return;
            var hips = Bone(HumanBodyBones.Hips); if (hips == null) return;
            var want = Seat.position + Seat.up * 0.11f * K; var err = want - hips.position;
            HipsGap = err.magnitude;
            // the body placement's calibration (the clip's body point vs its hips), learned slowly and bounded
            _bodyAdj = Vector3.ClampMagnitude(_bodyAdj + Quaternion.Inverse(Seat.rotation) * err * 0.5f, 0.35f * K);
            // a small error is corrected at once by the root (bounded; a big one never)
            if (err.magnitude < 0.3f * K && Root != null)
            {
                _off = Vector3.ClampMagnitude(_off + (Root.parent != null ? Root.parent.InverseTransformVector(err) : err), 0.6f * K);
                Root.localPosition = _off;   // (Root's parent may be scaled: the offset is in its own space)
            }
            // the shoulders roll forward to put each palm on its grip
            foreach (var (sh, hand, mid, grip) in new[] { (HumanBodyBones.LeftShoulder, HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleProximal, GripL), (HumanBodyBones.RightShoulder, HumanBodyBones.RightHand, HumanBodyBones.RightMiddleProximal, GripR) })
            {
                var s = Bone(sh); var h = Bone(hand); var m = Bone(mid); if (s == null || h == null || grip == null) continue;
                for (int it = 0; it < 2; it++)
                {
                    var palm = m != null ? (h.position + m.position) * 0.5f : h.position;
                    var rot = Quaternion.FromToRotation(palm - s.position, grip.position + Seat.up * 0.015f - s.position);
                    s.rotation = Quaternion.RotateTowards(Quaternion.identity, rot, ShoulderRollMax) * s.rotation;
                }
            }
            float G(HumanBodyBones hand, HumanBodyBones mid, Transform grip) { var h = Bone(hand); var m = Bone(mid); if (h == null || grip == null) return -1f; var palm = m != null ? (h.position + m.position) * 0.5f : h.position; return Vector3.Distance(palm, grip.position + Seat.up * 0.015f); }
            var gl = G(HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleProximal, GripL); var gr = G(HumanBodyBones.RightHand, HumanBodyBones.RightMiddleProximal, GripR);
            PalmGaps = new Vector2(gl, gr);
            float reach = Mathf.Max(gl, gr);
            if (reach > 0.015f) _reachLean = Mathf.Min(MaxReachLean, _reachLean + Mathf.Min(30f, reach * 300f) * Time.deltaTime);
            else if (reach < 0.005f) _reachLean = Mathf.Max(0f, _reachLean - 4f * Time.deltaTime);
        }
    }
}
