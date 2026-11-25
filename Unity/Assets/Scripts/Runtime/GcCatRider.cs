using UnityEngine;

namespace Seven
{
    /// <summary>Round 20: Duchess crouched in her racer's padded bed - the story race's ride pose (CatPoseOverlay.Ride: head up behind
    /// the windscreen, ears back, tail tucked), her root dropped by the pose's measured RideDrop.</summary>
    public class GcCatRider : MonoBehaviour
    {
        CatPoseOverlay _pose; Animator _an;
        void Start()
        {
            _an = GetComponentInChildren<Animator>(); if (_an != null) { _an.applyRootMotion = false; _an.cullingMode = AnimatorCullingMode.AlwaysAnimate; if (_an.HasState(0, Animator.StringToHash("idle"))) _an.Play("idle", 0, 0f); }
            _pose = GetComponent<CatPoseOverlay>() ?? gameObject.AddComponent<CatPoseOverlay>();
            _pose.Ride = 1f; _pose.RideNow();
        }
        void LateUpdate() { if (_pose != null) transform.localPosition = Vector3.down * _pose.RideDrop; }
    }
}
