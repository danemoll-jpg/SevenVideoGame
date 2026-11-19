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
