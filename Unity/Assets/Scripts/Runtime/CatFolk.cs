using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 18 — a Cattalus cat (citizens, Juniper, the Steward, the Announcer on the screen; Duchess on her own rig with the
    /// same bone names): Panther's rigged cat with its own fur, size and accessories, his clips (idle / walk / sit / wash / jump /
    /// sleep / stretch / lie_down) played at the speed that matches its real ground speed (never gliding - the NPC GLIDE probe
    /// counts cats too), plus the procedural layer of <see cref="CatPoseOverlay"/> for the vignettes (head look-at, drinking,
    /// a paw pushing). Cats speak telepathically: no mouth - the head lifts toward the listener, the ears twitch and the tail
    /// curls with the voice's loudness.
    /// </summary>
    public class CatFolk : MonoBehaviour
    {
        public Animator Anim;
        public CatPoseOverlay Pose;
        public string Role;
        /// <summary>Ground speed (m/s) of the walk clip at playback speed 1 for a cat of scale 1 (cat_v1: 0.16; Duchess sets hers).</summary>
        public float StrideSpeed = 0.16f;
        public AudioSource Voice;
        public Transform LookTarget;
        public string State { get; private set; } = "idle";
        /// <summary>Clips this cat plays as another ("sit=idle,wash=idle"): Duchess's auto-rig distorts some clips (for Dan).</summary>
        public string ClipMap;
        string Mapped(string clip) { if (string.IsNullOrEmpty(ClipMap)) return clip; foreach (var kv in ClipMap.Split(',')) { var p = kv.Split('='); if (p.Length == 2 && p[0].Trim() == clip) return p[1].Trim(); } return clip; }
        public bool Busy => _walk != null;
        Coroutine _walk; float _scale = 1f; readonly float[] _buf = new float[256];

        void Awake()
        {
            if (Anim == null) Anim = GetComponentInChildren<Animator>();
            if (Pose == null) Pose = GetComponent<CatPoseOverlay>() ?? gameObject.AddComponent<CatPoseOverlay>();
            _scale = transform.localScale.x;
        }
        public void Play(string clip, float fade = 0.25f, float speed = 1f)
        {
            if (Anim == null || Anim.runtimeAnimatorController == null) return;
            clip = Mapped(clip); State = clip; Anim.speed = speed;
            if (Anim.HasState(0, Animator.StringToHash(clip))) Anim.CrossFadeInFixedTime(clip, fade, 0);
        }
        public void Place(Vector3 p, Vector3 facing) { StopWalk(); transform.position = p; var f = facing; f.y = 0f; if (f.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(f); }
        public void Face(Vector3 p) { var f = p - transform.position; f.y = 0f; if (f.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(f); }
        public void StopWalk() { if (_walk != null) { StopCoroutine(_walk); _walk = null; } }
        /// <summary>Walk (or trot) to a point at a speed; the walk clip's playback matches the real ground speed; then the hold clip.</summary>
        public Coroutine WalkTo(Vector3 to, float speed = 0.5f, string then = "idle") { StopWalk(); _walk = StartCoroutine(Walk(new List<Vector3> { to }, speed, then, false)); return _walk; }
        public Coroutine Path(List<Vector3> pts, float speed, string then = "idle", bool loop = false) { StopWalk(); _walk = StartCoroutine(Walk(pts, speed, then, loop)); return _walk; }
        IEnumerator Walk(List<Vector3> pts, float speed, string then, bool loop)
        {
            _scale = Mathf.Max(0.05f, transform.localScale.x);
            float stride = StrideSpeed * _scale;
            Play("walk", 0.2f, Mathf.Clamp(speed / Mathf.Max(0.01f, stride), 0.4f, 3.2f));
            // off the ground (a ledge, a bench): keep the height it walks at; else follow the ground
            bool onGround = Mathf.Abs(transform.position.y - Ground.Height(transform.position.x, transform.position.z)) < 0.15f;
            do
            {
                foreach (var target in pts)
                {
                    var to = target; to.y = transform.position.y;
                    while ((to - transform.position).sqrMagnitude > 0.0025f * _scale)
                    {
                        var d = to - transform.position; d.y = 0f;
                        var step = Mathf.Min(d.magnitude, speed * Time.deltaTime);
                        var want = Quaternion.LookRotation(d.normalized);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 240f * Time.deltaTime);
                        transform.position += d.normalized * step;
                        if (onGround) { var p = transform.position; p.y = Ground.Height(p.x, p.z); transform.position = p; }
                        yield return null;
                    }
                }
            } while (loop);
            Play(then, 0.3f, 1f);
            _walk = null;
        }
        /// <summary>A hop (onto a low wall / into a box): the jump clip along an arc.</summary>
        public IEnumerator Hop(Vector3 to, float duration = 0.7f, float height = 0.4f)
        {
            StopWalk(); var from = transform.position; Play("jump", 0.1f, 1f);
            var f = to - from; f.y = 0f; if (f.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration) { transform.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * height; yield return null; }
            transform.position = to; Play("idle", 0.2f);
        }

        void LateUpdate()
        {
            if (Pose == null) return;
            // the voice: head lifts toward the listener, an ear twitch on loud syllables (telepathic - no mouth)
            float lvl = 0f;
            if (Voice != null && Voice.isPlaying) { Voice.GetOutputData(_buf, 0); foreach (var s in _buf) lvl = Mathf.Max(lvl, Mathf.Abs(s)); }
            Pose.LookAt = LookTarget != null ? LookTarget.position + Vector3.up * 1.4f : (Vector3?)null;
            Pose.Tilt = Mathf.MoveTowards(Pose.Tilt, lvl > 0.08f ? 0.35f : 0f, Time.deltaTime * 2f);
            Pose.EarToward = lvl > 0.12f && LookTarget != null ? LookTarget.position : (Vector3?)null;
        }
    }
}
