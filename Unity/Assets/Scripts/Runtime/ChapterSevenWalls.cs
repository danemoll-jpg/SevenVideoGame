using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF8 (Dan: "she partly disappears into walls"). 7 walks on walkable zones (rectangles) kept 0.25 m from their
    /// edges; the walls themselves are colliders or only meshes. Her animated body reaches further than that point:
    /// arms swinging along a wall, knees and boots braced against the rock while hanging / shimmying / climbing, the
    /// climb-up clip pushing her chest into the lip. So after the animation, every frame, each body point (head, chest,
    /// hips, elbows, hands when they are free, knees, boots while off the floor) is cast outward from the spine column at
    /// its own height against the level colliders and the visual-only meshes (<see cref="CameraOccluders"/>); whatever
    /// comes closer than a small skin margin pushes her whole body back out, horizontally (eased, max 0.4 m). The push
    /// is rendered only (undone at the start of the next Update), so walking, the zones and the traversal moves are
    /// unchanged; the hand IK on the lip is shifted by the same amount so the hands stay on the stone.
    /// </summary>
    public partial class ChapterSeven
    {
        const float Skin = 0.05f, MaxPush = 0.25f;
        Vector3 _wallPush, _pushApplied;
        public Vector3 WallPush => _wallPush;
        public static int WallFrames, WallPushFrames;
        public static float WallWorstNeed, WallWorstLeft;
        public static string WallWorstAt, WallWorstLeftAt, BodyFit;
        SevenRestPose _restPose;
        /// <summary>QF30 (14): the push the wall clearance WOULD have applied to a posed body (the 1.0.0-rc1 glide's cause), never applied.</summary>
        public static float PosedPushWanted; public static string PosedPushBy;
        static readonly int WallMask = ~((1 << 20) | (1 << 22) | (1 << 5) | (1 << 2));

        static readonly HumanBodyBones[] Core = { HumanBodyBones.Head, HumanBodyBones.UpperChest, HumanBodyBones.Chest, HumanBodyBones.Hips, HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm, HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.LeftShoulder, HumanBodyBones.RightShoulder };
        static readonly HumanBodyBones[] Hands = { HumanBodyBones.LeftHand, HumanBodyBones.RightHand };
        static readonly HumanBodyBones[] Feet = { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.LeftToes, HumanBodyBones.RightToes };

        /// <summary>Start of Update: take back last frame's rendered push (gameplay never sees it).</summary>
        void UndoWallPush()
        {
            if (_pushApplied.sqrMagnitude > 0f) { transform.position -= _pushApplied; _pushApplied = Vector3.zero; }
        }

        Vector3 NeedOf(HumanBodyBones bb, Vector3 root, ref Vector3 need, ref string by)
        {
            var b = Bone(bb); if (b == null) return need;
            var q = b.position;
            var c = new Vector3(root.x, q.y, root.z);
            var d = q - c; d.y = 0f; float len = d.magnitude;
            if (len < 0.03f) return need;
            var dir = d / len; float reach = len + Skin;
            if (!CameraOccluders.AnyRay(c, dir, reach, WallMask, out float hit)) return need;
            float pen = reach - hit;
            if (pen <= 0f) return need;
            // add only what the push so far does not already cover along this direction
            float have = Vector3.Dot(need, -dir);
            if (pen > have) { need += -dir * (pen - have); by = bb.ToString(); }
            return need;
        }

        /// <summary>End of LateUpdate (after the animation and IK): measure and apply the push.</summary>
        void WallClearance()
        {
            if (Anim == null || !Anim.isHuman) return;
            if (BodyFit == null) MeasureBodyFit();
            if (Current == Mode.Float || !CameraOccluders.Ready) { _wallPush = Vector3.zero; return; }
            // QF30 (14): a posed body (kneeling, sitting on the floor, lying) is never pushed: her shins along the floor met the finale
            // chamber's floor rubble and the eased push slid her backwards on her knees (1.0.0-rc1). Logged as what it would have done.
            if (_restPose == null) _restPose = GetComponent<SevenRestPose>();
            if (_restPose != null && _restPose.Pose != SevenRestPose.Kind.None)
            {
                var rootP = transform.position; var needP = Vector3.zero; string byP = null;
                foreach (var bb in Core) NeedOf(bb, rootP, ref needP, ref byP);
                needP.y = 0f; if (needP.magnitude > PosedPushWanted) { PosedPushWanted = needP.magnitude; PosedPushBy = $"{_restPose.Pose} ({byP})"; }
                _wallPush = Vector3.zero; return;
            }
            var root = transform.position;
            var need = Vector3.zero; string by = null;
            foreach (var bb in Core) NeedOf(bb, root, ref need, ref by);
            // QF12 (1): a hand held out by the scene's IK (SevenTableIK: holding the shape, touching the cube) is pinned in the
            // world - pushing her body away from it only stretches the arm, the hand stays in the wall and the push grows
            // and snaps back: her whole body shook through the lines after the first vision. Hands on a target never push.
            if (_tableIK == null) _tableIK = GetComponent<SevenTableIK>();
            bool tableHands = _tableIK != null && _tableIK.isActiveAndEnabled && (_tableIK.RightW > 0.01f || _tableIK.LeftW > 0.01f);
            bool handsBusy = tableHands || Current == Mode.Touch || Current == Mode.Gesture || Current == Mode.Hang || Current == Mode.Climb || _ikW > 0.05f;
            if (!handsBusy) foreach (var bb in Hands) NeedOf(bb, root, ref need, ref by);
            bool offFloor = Current == Mode.Hang || Current == Mode.Climb || Current == Mode.Air;
            if (offFloor) foreach (var bb in Feet) NeedOf(bb, root, ref need, ref by);
            need.y = 0f;
            if (need.magnitude > MaxPush) need = need.normalized * MaxPush;
            // walls on two sides (a narrow gap, flying rubble): the pushes cancel - then none at all instead of see-sawing
            // QF12 (1): a push that turns round no longer snaps to zero in one frame (up to 25 cm: a visible jerk); it
            // eases through zero like any other change
            float rate = Vector3.Dot(need, _wallPush) < 0f ? 1.2f : need.magnitude > _wallPush.magnitude ? 3f : 0.8f;          // out fast, back slowly
            _wallPush = Vector3.MoveTowards(_wallPush, need, rate * Time.deltaTime);
            WallFrames++;
            if (need.sqrMagnitude > 1e-6f) WallPushFrames++;
            if (need.magnitude > WallWorstNeed) { WallWorstNeed = need.magnitude; WallWorstAt = $"{Current} {State} at {root} ({by})"; }
            // what is still inside after this frame's (eased) push
            float left = Mathf.Max(0f, need.magnitude - Vector3.Dot(_wallPush, need.sqrMagnitude > 1e-8f ? need.normalized : Vector3.zero)) - Skin;
            if (left > WallWorstLeft) { WallWorstLeft = left; WallWorstLeftAt = $"{Current} {State} at {root} ({by})"; }
            transform.position += _wallPush; _pushApplied = _wallPush;
        }

        void MeasureBodyFit()
        {
            float r = 0f; string which = "";
            foreach (var bb in new[] { HumanBodyBones.LeftShoulder, HumanBodyBones.RightShoulder, HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm, HumanBodyBones.LeftHand, HumanBodyBones.RightHand, HumanBodyBones.Head, HumanBodyBones.LeftToes, HumanBodyBones.RightToes })
            {
                var b = Bone(bb); if (b == null) continue;
                var d = b.position - transform.position; d.y = 0f;
                if (d.magnitude > r) { r = d.magnitude; which = bb.ToString(); }
            }
            BodyFit = $"zone body radius 0.25 m vs the model in its current pose: furthest point {r:0.00} m from her root ({which}) - the rest is covered by the wall push (skin {Skin} m)";
        }

        public static string WallSummary() =>
            $"wall clearance (QF8): pushed out of a wall in {WallPushFrames} of {WallFrames} frames, largest push needed {WallWorstNeed * 100f:0.0} cm ({WallWorstAt ?? "-"}), worst still inside after the push {WallWorstLeft * 100f:0.0} cm ({WallWorstLeftAt ?? "-"}); {BodyFit}";
    }
}
