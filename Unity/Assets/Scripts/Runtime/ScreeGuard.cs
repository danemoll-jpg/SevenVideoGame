using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Scree guard (placeholder model "Scree_placeholder": a gray Y Bot in a hood, Humanoid walk / idle from 7's clips).
    /// Stands at its post slowly scanning, carries a lamp (shows where it looks). A noise makes it walk over, study the
    /// spot, then return. Sees 7 inside a view cone (range, half-angle) with a clear line of sight. Never attacks:
    /// being seen only sends 7 back to the room's checkpoint.
    /// </summary>
    public class ScreeGuard : FighterBase
    {
        public Vector3 Post;
        public Vector3 PostFacing = Vector3.forward;
        public float Range = 8f, HalfAngle = 45f, ScanAngle = 45f, ScanPeriod = 7f;
        public float WalkSpeed = 1.15f, InvestigateTime = 24f;
        public Transform Eye;
        [Tooltip("Keep on the walkable zones (the hall guard). Off for guards standing outside them (the gate).")]
        public bool UseGround = true;
        protected override bool Grounded => UseGround;

        public enum Mode { Post, ToNoise, Studying, Returning, Still }
        public Mode Current { get; private set; } = Mode.Post;
        Vector3 _noise;
        float _studyUntil;
        float _scanT;

        protected override void Awake()
        {
            base.Awake();
            Play("idle", 0f);
        }

        public void ResetToPost()
        {
            transform.position = new Vector3(Post.x, Ground.Height(Post.x, Post.z), Post.z);
            transform.rotation = Quaternion.LookRotation(PostFacing);
            Current = Mode.Post; _scanT = 0f;
            Play("idle", 0f);
        }

        public void Hear(Vector3 at)
        {
            if (Current == Mode.Still) return;
            _noise = at; Current = Mode.ToNoise;
        }

        public void Stand() { Current = Mode.Still; Play("idle", 0.3f, 1f, false); }

        public bool Distracted => Current == Mode.ToNoise || Current == Mode.Studying;

        Vector3 EyePos => Eye != null ? Eye.position : transform.position + Vector3.up * 1.6f;

        public bool Sees(Vector3 target)
        {
            var eye = EyePos;
            var to = target + Vector3.up * 1.0f - eye;
            var flat = new Vector3(to.x, 0f, to.z);
            if (flat.magnitude > Range) return false;
            if (Vector3.Angle(transform.forward, flat) > HalfAngle) return false;
            return !Physics.Linecast(eye, target + Vector3.up * 1.0f, ~0, QueryTriggerInteraction.Ignore);
        }

        protected override void Update()
        {
            base.Update();
            switch (Current)
            {
                case Mode.Post:
                {
                    _scanT += Time.deltaTime;
                    float yaw = Quaternion.LookRotation(PostFacing).eulerAngles.y + Mathf.Sin(_scanT / ScanPeriod * Mathf.PI * 2f) * ScanAngle;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, yaw, 0f), 60f * Time.deltaTime);
                    if (State != "idle") Play("idle", 0.3f, 1f, false);
                    break;
                }
                case Mode.ToNoise:
                    if (Walk(_noise, 1.3f)) { Current = Mode.Studying; _studyUntil = Time.time + InvestigateTime; }
                    break;
                case Mode.Studying:
                    FaceTowards(_noise, 120f);
                    if (State != "idle") Play("idle", 0.3f, 1f, false);
                    if (Time.time > _studyUntil) Current = Mode.Returning;
                    break;
                case Mode.Returning:
                    if (Walk(Post, 0.15f)) { Current = Mode.Post; _scanT = 0f; }
                    break;
            }
        }

        bool Walk(Vector3 to, float stopAt)
        {
            var d = to - transform.position; d.y = 0f;
            if (d.magnitude <= stopAt) { Play("idle", 0.3f, 1f, false); return true; }
            var step = d.normalized * Mathf.Min(d.magnitude - stopAt + 0.01f, WalkSpeed * Time.deltaTime);
            transform.position += step;
            FaceTowards(transform.position + d, 220f);
            if (State != "walk") Play("walk", 0.25f, 0.75f, false);
            return false;
        }
    }
}
