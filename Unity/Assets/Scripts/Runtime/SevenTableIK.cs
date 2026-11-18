using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 10 galley: small arm / head moves layered on 7's seated clip with the Animator's IK pass (the Seven
    /// controller has it on): the right hand brings the fork to her mouth, either hand reaches the dodecahedron, her eyes /
    /// head follow a point. Targets are set by <see cref="Chapter1Aboard"/>; weights ease so nothing snaps.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class SevenTableIK : MonoBehaviour
    {
        public Vector3 RightPos, LeftPos, LookPos;
        public Quaternion RightRot = Quaternion.identity, LeftRot = Quaternion.identity;
        [Range(0f, 1f)] public float RightW, LeftW, LookW;
        [Tooltip("How fast the weights ease (per second).")]
        public float Ease = 3f;
        float _r, _l, _look;
        Animator _anim;
        /// <summary>QF7 (emotion without faces): small life on her head and eyes - the gaze shifts a little now and then
        /// around what she looks at, a slow head / neck drift, and while she speaks (<see cref="Talk"/>) the head moves with
        /// the speech (small nods and turns). The Tripo head has no eye bones or eyelids, so gaze = head + look-at weights.</summary>
        public bool Alive = true;
        [Range(0f, 1f)] public float Talk;
        public int GazeShifts { get; private set; }
        Vector3 _gaze, _gazeTo; float _nextGaze, _talk, _t;
        Transform _neckB, _headB;

        void Awake() { _anim = GetComponent<Animator>(); }

        void OnAnimatorIK(int layer)
        {
            float dt = Time.deltaTime;
            _r = Mathf.MoveTowards(_r, RightW, dt * Ease);
            _l = Mathf.MoveTowards(_l, LeftW, dt * Ease);
            _look = Mathf.MoveTowards(_look, LookW, dt * Ease);
            _anim.SetIKPositionWeight(AvatarIKGoal.RightHand, _r);
            _anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            _anim.SetIKPosition(AvatarIKGoal.RightHand, RightPos);
            _anim.SetIKRotation(AvatarIKGoal.RightHand, RightRot);
            _anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, _l);
            _anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            _anim.SetIKPosition(AvatarIKGoal.LeftHand, LeftPos);
            _anim.SetIKRotation(AvatarIKGoal.LeftHand, LeftRot);
            _anim.SetLookAtWeight(_look, 0.15f, 0.7f, 1f, 0.6f);
            _anim.SetLookAtPosition(LookPos + (Alive ? _gaze : Vector3.zero));
        }

        void LateUpdate()
        {
            if (!Alive || _anim == null) return;
            float dt = Time.deltaTime; _t += dt;
            if (_headB == null) { _headB = _anim.GetBoneTransform(HumanBodyBones.Head); _neckB = _anim.GetBoneTransform(HumanBodyBones.Neck); }
            if (_headB == null) return;
            // gaze shifts: a new small offset around the look target every 1.2 - 3.5 s, reached in ~0.15 s (a glance)
            if (_t > _nextGaze)
            {
                _gazeTo = new Vector3(Random.Range(-0.07f, 0.07f), Random.Range(-0.04f, 0.05f), Random.Range(-0.07f, 0.07f));
                _nextGaze = _t + Random.Range(1.2f, 3.5f); GazeShifts++;
            }
            _gaze = Vector3.MoveTowards(_gaze, _gazeTo, dt * 0.6f);
            _talk = Mathf.MoveTowards(_talk, Talk, dt * 3f);
            var up = transform.up; var right = transform.right;
            float yaw = 2.0f * Mathf.Sin(_t * 0.37f) + 1.1f * Mathf.Sin(_t * 0.83f + 1f);
            float pitch = 1.2f * Mathf.Sin(_t * 0.29f + 2f);
            float env = 0.55f + 0.45f * Mathf.Sin(_t * 2f * Mathf.PI * 0.31f);
            pitch += _talk * (2.6f * Mathf.Sin(_t * 2f * Mathf.PI * 1.7f) * env + 1.2f * Mathf.Sin(_t * 2f * Mathf.PI * 0.7f));
            yaw += _talk * 1.8f * Mathf.Sin(_t * 2f * Mathf.PI * 0.45f + 0.5f);
            if (_neckB != null) _neckB.rotation = Quaternion.AngleAxis(yaw * 0.4f, up) * Quaternion.AngleAxis(pitch * 0.4f, right) * _neckB.rotation;
            _headB.rotation = Quaternion.AngleAxis(yaw * 0.6f, up) * Quaternion.AngleAxis(pitch * 0.6f, right) * _headB.rotation;
        }

        public bool Settled => Mathf.Abs(_r - RightW) < 0.01f && Mathf.Abs(_l - LeftW) < 0.01f;
    }
}
