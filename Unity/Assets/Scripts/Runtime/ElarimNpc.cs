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
        public float WalkSpeed = 1.5f;
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
