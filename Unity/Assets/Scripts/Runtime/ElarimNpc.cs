using System.Collections.Generic;
using UnityEngine;

namespace Seven
{

    /// <summary>
    /// Round 11 — an Elarim (guardian, attendant, worker): 3.5 m, armoured, caped, glowing softly with brighter eyes.
    /// Dan's Mixamo clips (Humanoid): idle, talk, point, walk, kneel, sword_idle, carry, lift. Calm and dignified -
    /// never hostile to 7. Workers loop a repair task (lift / carry) at their post. Each stands on a ground obstacle
    /// circle (7 and the bike walk / ride round them).
    /// </summary>
    public class ElarimNpc : MonoBehaviour
    {
        public string PostClip = "idle";
        /// <summary>QF13 (3): a dignified walk for a 3.5 m figure (~2 m/s: a human's 1.4 m/s scaled by the square root of
        /// his size). The clip is in place; its playback speed is matched to this (WalkAnimSpeed) so the planted foot stays put.</summary>
        public float WalkSpeed = 2.0f;
        /// <summary>QF13 (3): measured while walking - the clip's own ground speed at playback 1 (m/s, from the planted foot),
        /// the playback speed that matches WalkSpeed, and the planted foot's remaining slide (m/s; ~0 = no skating).</summary>
        public float ClipGroundSpeed { get; private set; } = -1f;
        public float WalkAnimSpeed { get; private set; } = 1f;
        public float FootSlide { get; private set; }
        /// <summary>White emission (the eyes glow brighter than the soft glow of the armour); set by the builder.</summary>
        public Material EyeMaterial;
        Animator _anim; string _state; Vector3? _walkTo; Vector3? _faceTo; float _stopAt;
        int _obstacle = -1;
        public bool Walking => _walkTo.HasValue;
        public static int Reasserts;
        public Animator Anim => _anim;

        void Awake()
        {
            _anim = GetComponentInChildren<Animator>();
            Play(PostClip, 0f);
            AddEyes();
        }

        void Start()
        {
            Ground.Obstacles.Add(new Ground.Circle(new Vector2(transform.position.x, transform.position.z), 0.75f));
            _obstacle = Ground.Obstacles.Count - 1;
        }

        void AddEyes()
        {
            if (_anim == null || !_anim.isHuman) return;
            var head = _anim.GetBoneTransform(HumanBodyBones.Head); if (head == null) return;
            var mat = EyeMaterial; if (mat == null) return;
            float s = transform.lossyScale.y;
            foreach (var side in new[] { -1f, 1f })
            {
                var e = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(e.GetComponent<Collider>());
                e.name = "EyeGlow"; e.GetComponent<Renderer>().sharedMaterial = mat;
                e.transform.SetParent(head, false);
                e.transform.position = head.position + transform.forward * 0.165f + transform.up * 0.11f + transform.right * side * 0.062f;
                e.transform.localScale = Vector3.one * (0.028f / Mathf.Max(0.01f, head.lossyScale.x));
            }
        }

        public void Play(string state, float fade = 0.3f)
        {
            if (_anim == null || state == _state) return;
            if (!HasState(state)) state = "idle";
            _state = state; _anim.CrossFadeInFixedTime(state, fade);
        }
        public bool HasState(string s) => _anim != null && _anim.HasState(0, Animator.StringToHash(s));
        public void WalkTo(Vector3 p, float stopAt = 0.2f) { _walkTo = p; _stopAt = stopAt; Play("walk"); }
        public void Face(Vector3 p) => _faceTo = p;
        public void Stop() { _walkTo = null; Play(PostClip); }

        // ---- QF12 (4): the kneel must show. If the "kneel" clip leaves his hips near standing height (the Round 12 / 13
        // problem: the Blender-made pose did not come through the Humanoid retarget), the pose is made here on top of the
        // animation: hips down to one thigh length above the ground, the left knee on the ground (thigh down, shin back),
        // the right foot planted forward (thigh level, shin down), the torso upright leaning a little, the head bowed.
        // Eased in / out over 0.8 s. Check runs read KneelHipsRatio (hips height / standing hips height: ~0.5 = kneeling).
        float _kneelW, _standHips = -1f; bool _kneelProc;
        public float KneelHipsRatio { get; private set; } = 1f;
        public bool KneelProcedural => _kneelProc;

        void LateUpdate()
        {
            if (_anim == null || !_anim.isHuman) return;
            var hips = _anim.GetBoneTransform(HumanBodyBones.Hips); if (hips == null) return;
            float gy = transform.position.y;
            float h = hips.position.y - gy;
            if (_standHips < 0f && _state != "kneel" && h > 0.3f) _standHips = h;
            bool want = _state == "kneel";
            _kneelW = Mathf.MoveTowards(_kneelW, want ? 1f : 0f, Time.deltaTime / 0.8f);
            if (want && _standHips > 0f && _kneelW > 0.99f && !_kneelProc && h > 0.75f * _standHips) { _kneelProc = true; Debug.Log($"[Seven] {name}: kneel clip keeps the hips at {h / _standHips:0.00} of standing - procedural kneel"); }
            if (!want && _kneelW <= 0f) _kneelProc = false;
            if (_kneelProc && _kneelW > 0f) ProceduralKneel(hips, gy, Mathf.SmoothStep(0f, 1f, _kneelW));
            if (_standHips > 0f) KneelHipsRatio = (hips.position.y - gy) / _standHips;
            WalkMatch();
            RelaxArms();
            CalmSway(hips);
        }

        // ---- QF14 (7): Dan's walk_v2 is Mixamo's "Catwalk Walk Forward". Measured in Blender: hip sway 3.0 % of his height
        // (7's walk 2.4 %), hip roll 12.4 deg (7's 14.8) - a model's runway walk, mild on him. To keep it calm and dignified
        // (not a strut) the side-to-side hip sway and the hip roll are damped by a third while he walks; the playback is
        // matched to his speed (WalkMatch: the planted foot stays put).
        public const float SwayDamp = 0.35f;
        Vector3 _latMean; bool _latInit; public float SwayBefore { get; private set; } public float SwayAfter { get; private set; }
        float _swMin = 9f, _swMax = -9f, _swT;
        void CalmSway(Transform hips)
        {
            if (!(_state == "walk" && _walkTo.HasValue)) { _latInit = false; return; }
            var local = transform.InverseTransformPoint(hips.position);
            if (!_latInit) { _latMean = local; _latInit = true; }
            _latMean = Vector3.Lerp(_latMean, local, Time.deltaTime * 1.2f);
            float lat = local.x - _latMean.x;
            hips.position -= transform.right * lat * SwayDamp * transform.lossyScale.x;
            // the roll about his walking direction, damped the same
            var fwd = transform.forward;
            var up = hips.rotation * Vector3.up;
            float roll = Vector3.SignedAngle(Vector3.ProjectOnPlane(transform.up, fwd), Vector3.ProjectOnPlane(up, fwd), fwd);
            if (Mathf.Abs(roll) < 30f) hips.rotation = Quaternion.AngleAxis(-roll * SwayDamp, fwd) * hips.rotation;
            // measure (check runs): the sway range over the last 2 s, before and after
            _swMin = Mathf.Min(_swMin, lat); _swMax = Mathf.Max(_swMax, lat);
            if ((_swT += Time.deltaTime) > 2f) { SwayBefore = (_swMax - _swMin) * transform.lossyScale.x; SwayAfter = SwayBefore * (1f - SwayDamp); _swMin = 9f; _swMax = -9f; _swT = 0f; }
        }

        // ---- QF13 (3): Dan - "he still uses the awful walk". Why the earlier fixes didn't hold: the rig and the cape were
        // fixed, never the walk itself. Dan's walk clip is in place and strides ~0.54 of his height per step: at his 3.5 m
        // that covers ~3.7 m/s, but he was slid along at 1.5 m/s - his legs paced out steps two and a half times longer
        // than the ground he covered (skating, a hurried shuffle), with the arms held out from the body. Now the clip's
        // ground speed is measured from the planted foot and the playback is matched to his speed (the foot stays where
        // it is planted), he walks at a calm 2 m/s, and his arms hang closer to his sides.
        Transform _planted; float _plantedZ; Vector3 _plantedW; float _minRel = 9f; Quaternion _lastRot;
        void WalkMatch()
        {
            bool walking = _state == "walk" && _walkTo.HasValue;
            if (!walking) { if (_anim.speed != 1f) _anim.speed = 1f; _planted = null; return; }
            var lf = _anim.GetBoneTransform(HumanBodyBones.LeftFoot); var rf = _anim.GetBoneTransform(HumanBodyBones.RightFoot);
            if (lf == null || rf == null) return;
            var low = lf.position.y < rf.position.y ? lf : rf;
            float dt = Mathf.Max(1e-4f, Time.deltaTime);
            float lz = transform.InverseTransformPoint(low.position).z;
            // planted = the lower foot while it is near its lowest (within 4 cm of the lowest seen lately): its stance phase
            float rel = low.position.y - transform.position.y;
            _minRel = Mathf.Min(_minRel + dt * 0.05f, rel);
            bool planted = rel < _minRel + 0.04f * transform.lossyScale.y;
            if (low == _planted && planted && !_anim.IsInTransition(0) && Quaternion.Angle(transform.rotation, _lastRot) < 60f * dt)
            {
                float v = (_plantedZ - lz) / dt / Mathf.Max(0.05f, _anim.speed);         // in place: the planted foot moves back at the clip's speed
                if (v > 0.2f && v < 12f) ClipGroundSpeed = ClipGroundSpeed < 0f ? v : Mathf.Lerp(ClipGroundSpeed, v, 0.05f);
                FootSlide = Mathf.Lerp(FootSlide, (low.position - _plantedW).magnitude / dt, 0.1f);
            }
            _planted = low; _plantedZ = lz; _plantedW = low.position; _lastRot = transform.rotation;
            if (ClipGroundSpeed > 0f) { WalkAnimSpeed = Mathf.Clamp(WalkSpeed / ClipGroundSpeed, 0.4f, 1.3f); _anim.speed = WalkAnimSpeed; }
        }
        void RelaxArms()
        {
            if (_state != "walk" && _state != "idle" && _state != "talk") return;
            foreach (var (up, lo, sgn) in new[] { (HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, -1f), (HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, 1f) })
            {
                var u = _anim.GetBoneTransform(up); var l = _anim.GetBoneTransform(lo); if (u == null || l == null) continue;
                var d = l.position - u.position; var side = Vector3.Dot(d, transform.right) * sgn;     // how far the elbow is out to the side
                if (side <= 0.02f) continue;
                float ang = Mathf.Min(14f, Mathf.Atan2(side, Mathf.Max(0.05f, -Vector3.Dot(d, Vector3.up))) * Mathf.Rad2Deg * 0.6f);
                u.rotation = Quaternion.AngleAxis(-sgn * ang, transform.forward) * u.rotation;
            }
        }

        void ProceduralKneel(Transform hips, float gy, float w)
        {
            Vector3 F = transform.forward, U = Vector3.up, R = transform.right;
            Transform B(HumanBodyBones b) => _anim.GetBoneTransform(b);
            var lUp = B(HumanBodyBones.LeftUpperLeg); var lLo = B(HumanBodyBones.LeftLowerLeg); var lFt = B(HumanBodyBones.LeftFoot); var lTo = B(HumanBodyBones.LeftToes);
            var rUp = B(HumanBodyBones.RightUpperLeg); var rLo = B(HumanBodyBones.RightLowerLeg); var rFt = B(HumanBodyBones.RightFoot); var rTo = B(HumanBodyBones.RightToes);
            if (lUp == null || lLo == null || lFt == null || rUp == null || rLo == null || rFt == null) return;
            float thigh = Vector3.Distance(lUp.position, lLo.position), shin = Vector3.Distance(lLo.position, lFt.position);
            // hips down: the left thigh straight down onto the knee on the ground (knee cap ~ 0.06 of the shin)
            float target = gy + thigh * 0.98f + shin * 0.08f;
            float drop = Mathf.Max(0f, hips.position.y - target);
            hips.position += Vector3.down * drop * w;
            hips.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.AngleAxis(4f, R), w) * hips.rotation;
            void Aim(Transform bone, Transform child, Vector3 dir)
            {
                if (bone == null || child == null) return;
                var cur = child.position - bone.position; if (cur.sqrMagnitude < 1e-6f) return;
                var q = Quaternion.FromToRotation(cur, dir.normalized) * bone.rotation;
                bone.rotation = Quaternion.Slerp(bone.rotation, q, w);
            }
            Aim(rUp, rLo, F * 1.0f + U * 0.04f + R * 0.12f);
            Aim(rLo, rFt, F * 0.04f - U * 1.0f + R * 0.02f);
            if (rTo != null) Aim(rFt, rTo, F * 0.75f - U * 0.45f);
            Aim(lUp, lLo, F * 0.18f - U * 1.0f - R * 0.10f);
            Aim(lLo, lFt, -F * 1.0f - U * 0.06f - R * 0.04f);
            if (lTo != null) Aim(lFt, lTo, -F * 0.45f - U * 0.85f);
            // torso upright with a small lean, the head bowed toward what she holds out
            var spine = B(HumanBodyBones.Spine); var chest = B(HumanBodyBones.Chest); var neck = B(HumanBodyBones.Neck); var head = B(HumanBodyBones.Head);
            if (spine != null && chest != null) Aim(spine, chest, U + F * 0.12f);
            if (chest != null && neck != null) Aim(chest, neck, U + F * 0.2f);
            if (head != null) head.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.AngleAxis(26f, R), w) * head.rotation;
        }

        float _check;
        void Update()
        {
            if (_anim != null && _state != null && (_check += Time.deltaTime) > 0.5f)
            {
                _check = 0f;
                if (!_anim.IsInTransition(0) && !_anim.GetCurrentAnimatorStateInfo(0).IsName(_state)) { Debug.Log($"[Seven] {name}: animator not in '{_state}' - re-asserted"); Reasserts++; _anim.CrossFadeInFixedTime(_state, 0.2f); }
            }
            if (_walkTo.HasValue)
            {
                var d = _walkTo.Value - transform.position; d.y = 0f;
                if (d.magnitude <= _stopAt) { _walkTo = null; Play(PostClip == "walk" ? "idle" : PostClip); }
                else
                {
                    transform.position += d.normalized * Mathf.Min(d.magnitude, WalkSpeed * Time.deltaTime);
                    var p = transform.position; p.y = Ground.Height(p.x, p.z); transform.position = p;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 120f * Time.deltaTime);
                }
            }
            else if (_faceTo.HasValue)
            {
                var d = _faceTo.Value - transform.position; d.y = 0f;
                if (d.sqrMagnitude > 1e-3f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 90f * Time.deltaTime);
            }
            if (_obstacle >= 0 && _obstacle < Ground.Obstacles.Count)
                Ground.Obstacles[_obstacle] = new Ground.Circle(new Vector2(transform.position.x, transform.position.z), 0.75f);
        }
    }
}
