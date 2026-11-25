using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF16 (3): 7's resting poses aboard, posed on her own bones after the animator every frame (Blender-made clips were
    /// tried first and came out of the humanoid retarget as the muscle-zero pose): each bone is aimed in her own frame (forward F = the
    /// character's forward, up U = its up, her left L) - the lying pose is authored upright, the staging turns her root onto her
    /// back. Relaxed, fully clothed, arms and legs at rest (the non-sexual presentation rule). A slow breath moves the chest.
    /// </summary>
    [DefaultExecutionOrder(300)]
    public class SevenRestPose : MonoBehaviour
    {
        public enum Kind { None, Lie, SitFloor, Kneel }
        public Kind Pose;
        Animator _a;
        // QF30 (14): Dan - "she glides backwards on her knees and stands up facing the wrong way". While a pose holds, nothing may move
        // her root or turn her: the root is pinned where the pose began (the cause in 1.0.0-rc1 was the QF8 wall push - her shins along
        // the floor met the chamber's floor rubble and pushed her whole body out, eased: a slow glide; ChapterSevenWalls now leaves a
        // posed body alone). Getting up is eased (Rise: the pose weight goes to 0 over RiseTime), never a snap; she keeps her facing.
        public float Weight { get; private set; }
        public float RiseTime = 1.2f;
        Kind _last = Kind.None; Vector3 _pin; Quaternion _pinR; bool _pinned;
        public static int DriftFrames; public static float WorstDrift, WorstTurn; public static string WorstWhy;
        /// <summary>Hold the current pose at her current spot (call after placing her).</summary>
        public void PinHere() { _pin = transform.position; _pinR = transform.rotation; _pinned = true; }
        /// <summary>Get up: the pose eases out over RiseTime, the root stays pinned until she is up.</summary>
        public void Rise() { _rising = true; }
        bool _rising;
        public bool Posed => Pose != Kind.None && Weight > 0.01f;
        /// <summary>QF30: ease into the pose over 0.8 s (the finale's kneel); the older aboard poses stay as they were (set at once).</summary>
        public bool EaseIn;
        /// <summary>QF30 (15): her head bowed over him (deg added to the spine / neck lean) and her shoulders shaking as she cries (0..1).</summary>
        public float Bow, Sob; float _bow, _sob;
        /// <summary>QF32 (1): extra hip drop (m) - FIN_10's kneel sits lower (knees on the floor, not raised), so her hands reach him.</summary>
        public float Drop;
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
            r.Pose = k; r.Drop = 0f; return r;   // QF32: a new pose starts without FIN_10's extra drop (the caller sets it)
        }
        void Awake() { _a = GetComponentInChildren<Animator>(); }
        Transform B(HumanBodyBones b) => _a != null ? _a.GetBoneTransform(b) : null;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        float _w = 1f;
        void Aim(Transform bone, Transform child, Vector3 want)
        {
            if (bone == null || child == null) return;
            var cur = child.position - bone.position;
            if (cur.sqrMagnitude < 1e-8f) return;
            bone.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(cur.normalized, want.normalized), _w) * bone.rotation;
        }
        public static string DriftReport() => $"POSE DRIFT (QF30 14): frames something tried to move or turn 7 while she knelt / sat / lay {DriftFrames} (pinned back; worst {WorstDrift * 100f:0.0} cm, {WorstTurn:0} deg{(WorstWhy != null ? ", " + WorstWhy : "")}) - she never moves during a pose";
        void LateUpdate()
        {
            if (Pose != _last) { if (Pose != Kind.None) { Weight = _last == Kind.None && EaseIn ? 0f : 1f; _rising = false; } _last = Pose; }
            if (Pose == Kind.None || _a == null) { Weight = 0f; _pinned = false; return; }
            Weight = _rising ? Mathf.MoveTowards(Weight, 0f, Time.deltaTime / Mathf.Max(0.1f, RiseTime)) : Mathf.MoveTowards(Weight, 1f, Time.deltaTime / 0.8f);
            if (_pinned)
            {
                float drift = Vector3.Distance(Flat(transform.position), Flat(_pin)); float turn = Quaternion.Angle(transform.rotation, _pinR);
                if (drift > 0.01f || turn > 1f)
                {
                    DriftFrames++; if (drift > WorstDrift) { WorstDrift = drift; WorstWhy = $"{Pose} at {_pin}"; } WorstTurn = Mathf.Max(WorstTurn, turn);
                    transform.SetPositionAndRotation(new Vector3(_pin.x, transform.position.y, _pin.z), _pinR);
                }
            }
            if (_rising && Weight <= 0f) { Pose = Kind.None; _last = Kind.None; _rising = false; _pinned = false; return; }
            var p = Pose == Kind.Lie ? Lie : Pose == Kind.Kneel ? KneelP : Sit;
            float w = Mathf.SmoothStep(0f, 1f, Weight); _w = w;
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
                hips.position = transform.position + Vector3.ProjectOnPlane(hips.position - transform.position, U) + U * Mathf.Max(0.12f, Vector3.Dot(hips.position - transform.position, U) - p.hipDrop * w * height / 1.7f - Drop * w);
            }
            float breath = 0.5f - 0.5f * Mathf.Cos(Time.time * 2f * Mathf.PI / 4f);
            var spine = B(HumanBodyBones.Spine); var chest = B(HumanBodyBones.Chest); var upper = B(HumanBodyBones.UpperChest);
            var neck = B(HumanBodyBones.Neck); var head = B(HumanBodyBones.Head);
            _bow = Mathf.MoveTowards(_bow, Bow, Time.deltaTime * 12f); _sob = Mathf.MoveTowards(_sob, Sob, Time.deltaTime * 1.5f);
            float shake = _sob * (1.6f * Mathf.Sin(Time.time * 2f * Mathf.PI * 2.3f) * Mathf.Max(0f, Mathf.Sin(Time.time * 2f * Mathf.PI * 0.45f)));
            Aim(spine, chest != null ? chest : neck, Lean(p.spine.x + _bow * 0.4f - breath * 0.6f));
            if (chest != null) Aim(chest, upper != null ? upper : neck, Lean(p.spine.y + _bow * 0.3f - breath * 0.6f + shake));
            if (upper != null) Aim(upper, neck, Lean(p.spine.z + _bow * 0.3f - breath * 1.4f + shake * 1.4f));
            Aim(neck, head, Lean(p.neck + _bow * 0.5f));
            var ik = GetComponent<SevenTableIK>(); bool held = false;
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
                // QF34 (1): an arm the IK owns is the IK's (re-solved below); the hand-over eases (ArmOwn 0..1), never a snap
                float own = ik != null ? ik.ArmOwn(!l) : 0f; if (own > 0.001f) held = true;
                if (own < 0.999f) { float w0 = _w; _w = w0 * (1f - own); Aim(ua, la, Dir(l ? p.upL : p.upR, s)); Aim(la, hd, Dir(l ? p.foreL : p.foreR, s)); _w = w0; }
            }
            if (held) ik.SolveHeldArms();                                   // after the spine / bow moved her shoulders
        }
    }
}
