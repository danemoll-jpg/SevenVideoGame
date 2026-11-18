using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 11: 7 on the hover bike (<see cref="HoverBike"/>). Seated (the seated idle clip), hips on the low saddle,
    /// her upper body leaning forward toward the forked front, head up to look ahead; the bike places her hands in the
    /// two recessed grips (IK). The bike carries her: no root motion, no ground clamp, no wall push.
    /// </summary>
    public partial class ChapterSeven
    {
        public bool Riding => Current == Mode.Ride;
        const float RideLean = 24f, RideHeadUp = 18f;

        public void BeginRide(Transform seat)
        {
            StopTraversal(); Release(); Freeze(false);
            Current = Mode.Ride; Speed = 0f;
            string clip = HasState("sitting_idle") ? "sitting_idle" : HasState("sit_idle") ? "sit_idle" : "idle";
            Play(clip, 0.3f);
            RideSeat(seat);
        }

        public void EndRide(Vector3 at, Vector3 facing)
        {
            if (Current != Mode.Ride) return;
            Current = Mode.Free;
            Place(at, facing);
        }

        /// <summary>Called by the bike after it moved (LateUpdate): hips on the saddle, facing the forked front.</summary>
        public void RideSeat(Transform seat)
        {
            if (seat == null) return;
            transform.SetPositionAndRotation(seat.position, Quaternion.Euler(0f, seat.eulerAngles.y, 0f));
        }

        void RideLateUpdate()
        {
            if (Current != Mode.Ride) return;
            var spine = Bone(HumanBodyBones.Spine); var chest = Bone(HumanBodyBones.Chest); var head = Bone(HumanBodyBones.Head);
            if (spine != null) spine.rotation = Quaternion.AngleAxis(RideLean * 0.5f, transform.right) * spine.rotation;
            if (chest != null) chest.rotation = Quaternion.AngleAxis(RideLean * 0.5f, transform.right) * chest.rotation;
            if (head != null) head.rotation = Quaternion.AngleAxis(-RideHeadUp, transform.right) * head.rotation;
        }
    }
}
