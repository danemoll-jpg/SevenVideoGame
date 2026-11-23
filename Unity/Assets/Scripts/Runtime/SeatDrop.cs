using UnityEngine;

namespace Seven
{
    /// <summary>QF28 (4): a cat that rides in a racer's padded bed from the start (the Cat-a-tron highlights' Duchess): crouched at
    /// once, her root lowered by the crouch's drop under the seat point.</summary>
    [DefaultExecutionOrder(300)]
    public class SeatDrop : MonoBehaviour
    {
        void Start()
        {
            var pose = GetComponent<CatPoseOverlay>();
            if (pose == null) return;
            pose.Ride = 1f; pose.RideNow();
            transform.localPosition = Vector3.down * pose.RideDrop;
        }
    }
}
