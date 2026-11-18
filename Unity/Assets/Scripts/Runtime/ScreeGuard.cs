using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Scree guard / worker. QF4: the real Scree model (Humanoid, 7's idle / walk / look_around retargeted + its own keyed
    /// "alert" and "work" clips, extras via <see cref="ScreeRig"/>); the gray hooded Y Bot "Scree_placeholder" stays as the
    /// fallback (it has no alert / work clips: those steps are skipped).
    /// Stands at its post slowly scanning, carries a lamp (shows where it looks). A noise makes it walk over, study the
    /// spot, then return. Sees 7 inside a view cone (range, half-angle) with a clear line of sight. Never attacks:
    /// being seen only sends 7 back to the room's checkpoint.
    /// </summary>
    public partial class ScreeGuard : FighterBase
    {
        public Vector3 Post;
        public Vector3 PostFacing = Vector3.forward;
        public float Range = 8f, HalfAngle = 45f, ScanAngle = 45f, ScanPeriod = 7f;
        public float WalkSpeed = 1.15f, InvestigateTime = 24f;
        [Tooltip("State played at the post: idle (guards) or work (workers, loop).")]
        public string PostState = "idle";
        public Transform Eye;
        [Tooltip("Keep on the walkable zones (the hall guard). Off for guards standing outside them (the gate).")]
        public bool UseGround = true;
        protected override bool Grounded => UseGround;

        public enum Mode { Post, Alert, ToNoise, Studying, Returning, Still }
        public Mode Current { get; private set; } = Mode.Post;
        Vector3 _noise;
        float _studyUntil;
        float _scanT, _alertUntil;
        ScreeRig _rig;
        /// <summary>Times the alert reaction played (check runs).</summary>
        public int Alerts { get; private set; }

        string PostClip => HasState(PostState) ? PostState : "idle";
        string StudyClip => HasState("look_around") ? "look_around" : "idle";

        protected override void Awake()
        {
            base.Awake();
            _rig = GetComponent<ScreeRig>();
            Play(PostClip, 0f, 1f, true, Random.value);
        }

        public void ResetToPost()
        {
            transform.position = new Vector3(Post.x, Ground.Height(Post.x, Post.z), Post.z);
            transform.rotation = Quaternion.LookRotation(PostFacing);
            Current = Mode.Post; _scanT = 0f;
            Play(PostClip, 0f);
        }

        public void Hear(Vector3 at)
        {
            if (Current == Mode.Still) return;
            _noise = at;
            if (HasState("alert") && Current == Mode.Post)
            {
                // QF4: a short startle first (keyed clip), facing the noise, then it walks over to check.
                Current = Mode.Alert; Alerts++;
                _alertUntil = Time.time + Length("alert") * 0.9f;
                Play("alert", 0.12f);
                if (_rig != null) _rig.Twitch = 1f;
            }
            else Current = Mode.ToNoise;
        }

        public void Stand() { Current = Mode.Still; Play("idle", 0.3f, 1f, false); }

        public bool Distracted => Current == Mode.Alert || Current == Mode.ToNoise || Current == Mode.Studying;

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
            if (UpdateFight()) return;                         // QF9: the terrace fight
            if (_rig != null) _rig.Brace = State == "work" ? 1f : State == "alert" ? 0.35f : 0f;
            switch (Current)
            {
                case Mode.Post:
                {
                    _scanT += Time.deltaTime;
                    float yaw = Quaternion.LookRotation(PostFacing).eulerAngles.y + Mathf.Sin(_scanT / ScanPeriod * Mathf.PI * 2f) * ScanAngle;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, yaw, 0f), 60f * Time.deltaTime);
                    if (State != PostClip) Play(PostClip, 0.3f, 1f, false);
                    break;
                }
                case Mode.Alert:
                    FaceTowards(_noise, 160f);
                    if (Time.time > _alertUntil) Current = Mode.ToNoise;
                    break;
                case Mode.ToNoise:
                    if (Walk(_noise, 1.3f)) { Current = Mode.Studying; _studyUntil = Time.time + InvestigateTime; }
                    break;
                case Mode.Studying:
                    FaceTowards(_noise, 120f);
                    if (State != StudyClip) Play(StudyClip, 0.3f, 1f, false);
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
