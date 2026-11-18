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
        const float RideLean = 40f, RideHeadUp = 30f;

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

        /// <summary>In the IK pass (before the hand IK solves): lean the spine and chest toward the forked front.</summary>
        void RideLeanIK()
        {
            if (Current != Mode.Ride || Anim == null) return;
            foreach (var bb in new[] { HumanBodyBones.Spine, HumanBodyBones.Chest })
            {
                var b = Bone(bb); if (b == null) continue;
                var axis = Quaternion.Inverse(b.rotation) * transform.right;
                Anim.SetBoneLocalRotation(bb, b.localRotation * Quaternion.AngleAxis(RideLean * 0.5f, axis));
            }
        }

        void RideLateUpdate()
        {
            if (Current != Mode.Ride) return;
            var head = Bone(HumanBodyBones.Head);
            if (head != null) head.rotation = Quaternion.AngleAxis(-RideHeadUp, transform.right) * head.rotation;
        }
    }
}
