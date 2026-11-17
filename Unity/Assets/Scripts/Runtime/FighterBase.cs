using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Shared pieces for 7 and the training bot: plays Animator states by name (cross-fade), tracks the state's
    /// normalized time itself (so freezing / speed changes stay exact), applies horizontal root motion and
    /// knockback slides, and keeps the fighter inside the arena.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public abstract class FighterBase : MonoBehaviour
    {
        protected Animator Anim;
        readonly Dictionary<string, float> _len = new Dictionary<string, float>();

        public string State { get; private set; }
        /// <summary>Normalized time of the current state (0..1 for one play; keeps counting past 1).</summary>
        public float Tn { get; private set; }
        float _speed = 1f;
        bool _frozen;

        Vector3 _slideVel;
        float _slideDecel;

        protected virtual void Awake()
        {
            Anim = GetComponent<Animator>();
            Anim.applyRootMotion = true;   // handled in OnAnimatorMove
            Anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var c in Anim.runtimeAnimatorController.animationClips) _len[c.name] = Mathf.Max(0.05f, c.length);
        }

        public float Length(string state) => _len.TryGetValue(state, out var l) ? l : 1f;
        /// <summary>True when the controller has a clip of this name (optional Mixamo clips).</summary>
        public bool HasState(string state) => _len.ContainsKey(state);

        protected void Play(string state, float fade = 0.12f, float speed = 1f, bool restart = true, float startNormalized = 0f)
        {
            if (!restart && State == state) return;
            State = state;
            Tn = startNormalized;
            _speed = speed;
            _frozen = false;
            Anim.speed = speed;
            Anim.CrossFadeInFixedTime(state, fade, 0, startNormalized * Length(state));
        }

        protected void Freeze(bool frozen)
        {
            _frozen = frozen;
            Anim.speed = frozen ? 0f : _speed;
        }

        /// <summary>Slide the fighter by roughly <paramref name="distance"/> metres along dir over <paramref name="seconds"/>.</summary>
        public void Slide(Vector3 dir, float distance, float seconds)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f || distance <= 0f) return;
            float v0 = 2f * distance / seconds;
            _slideVel = dir.normalized * v0;
            _slideDecel = v0 / seconds;
        }

        protected virtual void Update()
        {
            if (!_frozen) Tn += Time.deltaTime * _speed / Length(State ?? "");
            if (_slideVel.sqrMagnitude > 0f)
            {
                transform.position += _slideVel * Time.deltaTime;
                _slideVel = Vector3.MoveTowards(_slideVel, Vector3.zero, _slideDecel * Time.deltaTime);
            }
            if (Grounded) ClampToArena();
        }

        /// <summary>False while the character is carried by something else (7 seated in her chair).</summary>
        protected virtual bool Grounded => true;

        void OnAnimatorMove()
        {
            // Horizontal root motion only (most clips bake it into the pose, so this is ~0 for them).
            var d = Anim.deltaPosition;
            d.y = 0f;
            transform.position += d;
        }

        /// <summary>Height above the ground (the bot while held by telekinesis).</summary>
        protected virtual float ExtraHeight => 0f;

        protected void ClampToArena()
        {
            var p = Ground.ClampToArena(transform.position);
            p.y = Ground.Height(p.x, p.z) + ExtraHeight;
            transform.position = p;
        }

        protected void FaceTowards(Vector3 target, float degPerSec)
        {
            var to = target - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 1e-4f) return;
            var want = Quaternion.LookRotation(to);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, degPerSec * Time.deltaTime);
        }

        public Transform Bone(HumanBodyBones b) => Anim != null ? Anim.GetBoneTransform(b) : null;
    }
}
