using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF16 (3): 7's resting poses aboard, posed on her own bones after the animator every frame (Blender-made clips were
    /// tried first and came out of the humanoid retarget as the muscle-zero pose): each bone is aimed in her own frame (forward F = the
    /// character's forward, up U = its up, her left L) - the lying pose is authored upright, the staging turns her root onto her
    /// back. Relaxed, fully clothed, arms and legs at rest (the non-sexual presentation rule). A slow breath moves the chest.
    /// </summary>
    public class SevenRestPose : MonoBehaviour
    {
        public enum Kind { None, Lie, SitFloor, Kneel }
        public Kind Pose;
        Animator _a;
        struct P { public float hipDrop; public Vector3 spine; public float neck, head; public Vector3 thigh, shin, foot, upL, foreL, upR, foreR; }
        static readonly P Lie = new P
        {
            hipDrop = 0f, spine = new Vector3(2f, 4f, 6f), neck = 12f, head = 14f,
            thigh = new Vector3(0.30f, -0.95f, 0.06f), shin = new Vector3(-0.12f, -0.99f, 0.03f), foot = new Vector3(0.85f, -0.45f, 0.02f),
            upL = new Vector3(0.22f, -0.95f, 0.16f), foreL = new Vector3(0.22f, -0.30f, -0.93f),        // left hand resting on her stomach (on Panther)
            upR = new Vector3(0.05f, -0.97f, 0.24f), foreR = new Vector3(0.10f, -0.97f, 0.20f),        // right arm loose at her side
        };
        static readonly P Sit = new P
        {
            hipDrop = 0.80f, spine = new Vector3(-4f, 2f, 6f), neck = 10f, head = 6f,
            thigh = new Vector3(0.80f, 0.55f, 0.16f), shin = new Vector3(0.30f, -0.95f, 0.04f), foot = new Vector3(0.88f, -0.45f, 0f),
            upL = new Vector3(0.55f, -0.80f, 0.25f), foreL = new Vector3(0.92f, -0.05f, -0.35f),       // forearms resting across the knees
            upR = new Vector3(0.55f, -0.80f, 0.25f), foreR = new Vector3(0.92f, -0.05f, -0.35f),
        };

        /// <summary>Round 19 (FIN_10): kneeling, sitting back on her heels, leaning a little forward over what is in front of her knees.</summary>
        static readonly P KneelP = new P
        {
            hipDrop = 0.50f, spine = new Vector3(14f, 10f, 8f), neck = 16f, head = 10f,
            thigh = new Vector3(0.93f, -0.33f, 0.10f), shin = new Vector3(-0.96f, -0.26f, 0.04f), foot = new Vector3(-0.85f, -0.5f, 0f),
            upL = new Vector3(0.45f, -0.88f, 0.12f), foreL = new Vector3(0.9f, -0.35f, -0.2f),
            upR = new Vector3(0.45f, -0.88f, 0.12f), foreR = new Vector3(0.9f, -0.35f, -0.2f),
        };
        public static SevenRestPose On(GameObject seven, Kind k)
        {
            var r = seven.GetComponent<SevenRestPose>() ?? seven.AddComponent<SevenRestPose>();
            r.Pose = k; return r;
        }
        void Awake() { _a = GetComponentInChildren<Animator>(); }
        Transform B(HumanBodyBones b) => _a != null ? _a.GetBoneTransform(b) : null;
        static void Aim(Transform bone, Transform child, Vector3 want)
        {
            if (bone == null || child == null) return;
            var cur = child.position - bone.position;
            if (cur.sqrMagnitude < 1e-8f) return;
            bone.rotation = Quaternion.FromToRotation(cur.normalized, want.normalized) * bone.rotation;
        }
        void LateUpdate()
        {
            if (Pose == Kind.None || _a == null) return;
            var p = Pose == Kind.Lie ? Lie : Pose == Kind.Kneel ? KneelP : Sit;
            var F = transform.forward; var U = transform.up; var L = -transform.right;
            Vector3 Dir(Vector3 v, float side) => (F * v.x + U * v.y + L * (v.z * side)).normalized;
            Vector3 Lean(float deg) { float r = deg * Mathf.Deg2Rad; return (U * Mathf.Cos(r) + F * Mathf.Sin(r)).normalized; }
            var hips = B(HumanBodyBones.Hips);
            if (hips == null) return;
            if (p.hipDrop > 0f)
            {
                var head0 = B(HumanBodyBones.Head);
                float height = head0 != null ? Vector3.Dot(head0.position - transform.position, U) + 0.12f : 1.7f;
                // the hips' height above her root, measured each frame from the animator's pose, then dropped to the floor pose
                hips.position = transform.position + Vector3.ProjectOnPlane(hips.position - transform.position, U) + U * Mathf.Max(0.12f, Vector3.Dot(hips.position - transform.position, U) - p.hipDrop * height / 1.7f);
            }
            float breath = 0.5f - 0.5f * Mathf.Cos(Time.time * 2f * Mathf.PI / 4f);
            var spine = B(HumanBodyBones.Spine); var chest = B(HumanBodyBones.Chest); var upper = B(HumanBodyBones.UpperChest);
            var neck = B(HumanBodyBones.Neck); var head = B(HumanBodyBones.Head);
            Aim(spine, chest != null ? chest : neck, Lean(p.spine.x - breath * 0.6f));
            if (chest != null) Aim(chest, upper != null ? upper : neck, Lean(p.spine.y - breath * 0.6f));
            if (upper != null) Aim(upper, neck, Lean(p.spine.z - breath * 1.4f));
            Aim(neck, head, Lean(p.neck));
            foreach (var (side, s) in new[] { ("L", 1f), ("R", -1f) })
            {
                bool l = side == "L";
                var ul = B(l ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
                var ll = B(l ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
                var ft = B(l ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
                var toe = B(l ? HumanBodyBones.LeftToes : HumanBodyBones.RightToes);
                Aim(ul, ll, Dir(p.thigh, s)); Aim(ll, ft, Dir(p.shin, s)); Aim(ft, toe, Dir(p.foot, s));
                var ua = B(l ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
                var la = B(l ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
                var hd = B(l ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
                Aim(ua, la, Dir(l ? p.upL : p.upR, s)); Aim(la, hd, Dir(l ? p.foreL : p.foreR, s));
            }
        }
    }
}
