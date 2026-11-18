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
            _anim.SetLookAtPosition(LookPos);
        }

        public bool Settled => Mathf.Abs(_r - RightW) < 0.01f && Mathf.Abs(_l - LeftW) < 0.01f;
    }
}
