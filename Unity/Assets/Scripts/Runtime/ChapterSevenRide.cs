using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 11 / QF10: 7 on the hover bike (<see cref="HoverBike"/>). There is no Mixamo straddle clip, so the pose is
    /// Blender-made (Tools/Blender/make_ride_pose.py -> state "ride": astride the saddle, knees bent and gripping the body,
    /// feet tucked back on the side panels, hips low, torso leaning toward the forked front, arms forward, a subtle breath).
    /// On top of it, in the IK pass: her hands go into the two recessed fairing grips (palm centre on the grip), her feet
    /// onto the side-panel points (knees aimed forward-out along the tank), and she looks ahead. Her hips are held on the
    /// saddle (measured every frame, so the clip's retarget height never matters), she rides the bike's pitch and roll,
    /// and leans her body a few degrees further into turns. The bike carries her: no root motion, no ground clamp, no
    /// wall push. (Without the "ride" state she falls back to the seated clip.)
    /// </summary>
    public partial class ChapterSeven
    {
        public bool Riding => Current == Mode.Ride;
        public const float RideTurnLean = 6f;
        public const float TuckLean = 42f;         // QF10: the racing tuck, degrees forward from upright      // extra body lean into a full turn (degrees, on top of the bike's roll)
        HoverBike _bike;
        Vector3 _rideOffset;                       // root offset from the saddle (bike space), calibrated so the hips sit on it
        float _reachLean;                          // extra forward lean (degrees over the spine) so the palms reach the grips
        bool _rideCal;
        /// <summary>Check runs: palm centre to grip (left, right) and foot to its side-panel point (left, right), metres.</summary>
        public Vector4 RideGaps { get; private set; }
        public float RideHipsGap { get; private set; }
        public float RideReachLean => _reachLean;

        public void BeginRide(HoverBike bike)
        {
            StopTraversal(); Release(); Freeze(false);
            _bike = bike;
            Current = Mode.Ride; Speed = 0f;
            string clip = HasState("ride") ? "ride" : HasState("sitting_idle") ? "sitting_idle" : HasState("sit_idle") ? "sit_idle" : "idle";
            Play(clip, 0.3f);
            if (!_rideCal) { _rideOffset = new Vector3(0f, -0.5f, 0.1f); }
            RideSeat(bike);
        }

        public void EndRide(Vector3 at, Vector3 facing)
        {
            if (Current != Mode.Ride) return;
            Current = Mode.Free; _bike = null;
            Place(at, facing);
        }

        /// <summary>Hips on the saddle, facing the forked front, riding the bike's pitch and roll.</summary>
        public void RideSeat(HoverBike bike)
        {
            if (bike == null || bike.Seat == null) return;
            var seat = bike.Seat;
            transform.SetPositionAndRotation(seat.position + seat.rotation * _rideOffset, seat.rotation);
        }

        /// <summary>In the IK pass: hands in the grips, feet on the side panels, knees forward-out, gaze ahead, the turn lean.</summary>
        void RideLeanIK()
        {
            if (Current != Mode.Ride || Anim == null || _bike == null) return;
            var b = _bike;
            // lean a little further into the turn (the bike's own roll already tilts her with it)
            float lean = -Mathf.Clamp(b.Turn / 14f, -1f, 1f) * RideTurnLean;
            // QF10 (Dan): the racing tuck - the whole upper body laid forward along the bike from the hips (the body
            // rotation in the IK pass; the humanoid retarget flattens the clip's spine lean), a little further if the hands
            // still need it to reach the grips, and into the turns; the feet / hands IK then place the limbs
            Anim.bodyRotation = Quaternion.AngleAxis(lean, transform.forward) * Quaternion.AngleAxis(TuckLean + _reachLean, transform.right) * Anim.bodyRotation;
            // hands: the wrist goes where the palm centre lands on the grip (palm offset measured from the pose itself)
            foreach (var (goal, hand, mid, grip) in new[] { (AvatarIKGoal.LeftHand, HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleProximal, b.GripL),
                                                            (AvatarIKGoal.RightHand, HumanBodyBones.RightHand, HumanBodyBones.RightMiddleProximal, b.GripR) })
            {
                if (grip == null) continue;
                var h = Bone(hand); var m = Bone(mid);
                var palm = h != null && m != null ? (m.position - h.position) * 0.5f : Vector3.zero;
                Anim.SetIKPositionWeight(goal, 1f);
                Anim.SetIKPosition(goal, grip.position + b.transform.up * 0.015f - palm);
                Anim.SetIKRotationWeight(goal, 0f);
            }
            // feet on the side panels, knees forward along the tank and a little out
            foreach (var (goal, hint, foot, sgn) in new[] { (AvatarIKGoal.LeftFoot, AvatarIKHint.LeftKnee, b.FootL, -1f), (AvatarIKGoal.RightFoot, AvatarIKHint.RightKnee, b.FootR, 1f) })
            {
                if (foot == null) continue;
                Anim.SetIKPositionWeight(goal, 1f);
                Anim.SetIKPosition(goal, foot.position + b.transform.up * 0.06f);
                Anim.SetIKRotationWeight(goal, 0.6f);
                Anim.SetIKRotation(goal, Quaternion.LookRotation(-b.transform.forward * 0.6f - b.transform.up * 0.8f, b.transform.forward));   // feet pointing back along the rear sides
                Anim.SetIKHintPositionWeight(hint, 0.8f);
                Anim.SetIKHintPosition(hint, b.Seat.position + b.transform.forward * 0.40f + b.transform.right * sgn * (b.KneeWidth + 0.10f) - b.transform.up * 0.12f);   // knees tucked in against the sides
            }
            Anim.SetLookAtWeight(0.75f, 0.0f, 0.8f, 1f, 0.5f);   // head up, eyes on the way ahead
            Anim.SetLookAtPosition(b.transform.position + b.transform.forward * 14f + Vector3.up * 1.3f);
        }

        void RideLateUpdate()
        {
            if (Current != Mode.Ride || _bike == null) return;
            var b = _bike;
            // hips onto the saddle: measured after the animation, corrected at once (and remembered for the next frames)
            var hips = Bone(HumanBodyBones.Hips);
            if (hips != null && b.Seat != null)
            {
                var want = b.Seat.position + b.Seat.up * 0.11f;                               // pelvis on the back of the saddle (measured)
                var err = want - hips.position;
                RideHipsGap = err.magnitude;
                _rideOffset += Quaternion.Inverse(b.Seat.rotation) * err;
                transform.position += err; _rideCal = true;
            }
            // check-run numbers (after IK and the hips correction: what is rendered)
            float G(HumanBodyBones hand, HumanBodyBones mid, Transform grip)
            {
                var h = Bone(hand); var m = Bone(mid);
                if (h == null || grip == null) return -1f;
                var palm = m != null ? (h.position + m.position) * 0.5f : h.position;
                return Vector3.Distance(palm, grip.position + b.transform.up * 0.015f);
            }
            float F(HumanBodyBones foot, Transform pt) { var f = Bone(foot); return f == null || pt == null ? -1f : Vector3.Distance(f.position, pt.position + b.transform.up * 0.06f); }
            float reach = Mathf.Max(G(HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleProximal, b.GripL), G(HumanBodyBones.RightHand, HumanBodyBones.RightMiddleProximal, b.GripR));
            if (reach > 0.015f) _reachLean = Mathf.Min(22f, _reachLean + Mathf.Min(30f, reach * 300f) * Time.deltaTime);
            else if (reach < 0.005f) _reachLean = Mathf.Max(0f, _reachLean - 4f * Time.deltaTime);
            RideGaps = new Vector4(G(HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleProximal, b.GripL), G(HumanBodyBones.RightHand, HumanBodyBones.RightMiddleProximal, b.GripR),
                                   F(HumanBodyBones.LeftFoot, b.FootL), F(HumanBodyBones.RightFoot, b.FootR));
        }
    }
}
