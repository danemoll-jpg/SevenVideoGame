using UnityEngine;

namespace Seven
{
    /// <summary>
    /// 7's black cat. Roams the deck at a larger-than-house-cat size (RoamScale; one constant), loosely following 7:
    /// walks, sits, washes, naps. When 7 sits in her chair it trots over, hops onto her lap shrinking to house-cat size
    /// with a soft white shimmer, and curls up asleep (it follows her thighs every frame, so it moves and tilts with
    /// her). When she stands it hops down and grows back. Never ridden, never hurt.
    /// </summary>
    [DefaultExecutionOrder(100)]   // after 7 (her hips are pinned in her LateUpdate)
    public class CatCompanion : MonoBehaviour
    {
        public SaucerSeven Seven;
        public Animator Anim;
        public Transform ChairPivot;
        public Material Additive;               // Seven/Additive (shimmer motes)

        [Header("Size")]
        [Tooltip("Roaming size (1 = real house cat, 0.25 m at the shoulder). Dan's call: about 2.")]
        public float RoamScale = 2f;
        public float LapScale = 1f;

        [Header("Motion")]
        [Tooltip("Ground speed of the walk clip at anim speed 1 and scale 1 (m/s).")]
        public float StrideSpeed = 0.16f;
        public float StrollSpeed = 0.45f, TrotSpeed = 1.1f;
        public float FollowDistance = 3.2f, BodyRadius = 0.18f;   // radius at scale 1
        [Tooltip("Lap height above the middle of her thighs (m).")]
        public float LapLift = 0.165f;     // Round 8: a little higher, clear of her forearms
        public float HopTime = 0.75f, HopHeight = 0.35f;

        public enum Mode { Roam, ToChair, Waiting, HopUp, OnLap, HopDown }
        public Mode Current { get; private set; } = Mode.Roam;
        public string Action { get; private set; } = "idle";   // current roam behaviour / clip
        public float Scale => transform.localScale.x;

        // Check runs can force a behaviour.
        [System.NonSerialized] public string ForceAction;
        [System.NonSerialized] public Vector3? ForceTarget;

        Vector3 _target;
        float _actionUntil, _speed;
        string _clip;
        float _hopT;
        Vector3 _hopFrom;
        float _scaleFrom, _scaleTo;
        float _sevenStill;
        Vector3 _sevenLast;
        ParticleSystem _motes;
        Renderer[] _renderers;
        MaterialPropertyBlock _mpb;
        float _shimmer;
        static readonly int RimId = Shader.PropertyToID("_Rim");
        float _baseRim = 0.35f;

        void Start()
        {
            transform.localScale = Vector3.one * RoamScale;
            _renderers = GetComponentsInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            foreach (var r in _renderers)
                if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(RimId)) { _baseRim = r.sharedMaterial.GetFloat(RimId); break; }
            _motes = MakeMotes();
            Seven.StartedSitting += () => { Current = Mode.ToChair; _toChairTime = 0f; };
            Seven.StartedStanding += OnSevenStands;
            _sevenLast = Seven.transform.position;
            Choose("sit", 2f);
        }

        // ------------------------------------------------------------------------------------- helpers
        void PlayClip(string clip, float fade = 0.25f, float speed = 1f)
        {
            Anim.speed = speed;
            if (_clip == clip) return;
            _clip = clip;
            Anim.CrossFadeInFixedTime(clip, fade, 0);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        Vector3 LaunchPoint => Flat(ChairPivot.position + ChairPivot.right * 0.95f + ChairPivot.forward * 0.85f);
        Vector3 LapPoint => Seven.LapPoint(LapLift);
        Quaternion LapRotation => Quaternion.LookRotation(-ChairPivot.right, ChairPivot.up);   // curled across her lap

        /// <summary>Walk toward the target; returns true on arrival.</summary>
        bool MoveTo(Vector3 target, float speed)
        {
            var to = Flat(target - transform.position);
            float s = Scale;
            if (to.magnitude < 0.08f * s) { PlayClip("idle", 0.25f); return true; }
            float len = Mathf.Min(to.magnitude, speed * Time.deltaTime);
            // Straight at the target; if furniture blocks the way, sidestep (try turning left / right).
            Vector3 p = transform.position;
            foreach (var turn in new[] { 0f, 45f, -45f, 80f, -80f, 120f, -120f })
            {
                var dir = Quaternion.Euler(0f, turn + _detour, 0f) * to.normalized;
                var q = Ground.ClampToArena(transform.position + dir * len, BodyRadius * s);
                q.y = 0f;
                if (Vector3.Dot(q - transform.position, dir) > len * 0.6f)
                {
                    p = q;
                    if (turn != 0f) _detour = Mathf.Clamp(_detour + turn * 0.02f, -90f, 90f); else _detour *= 0.95f;
                    break;
                }
            }
            // Keep a little room around 7 while walking (not when heading to the lap launch spot).
            if (Current == Mode.Roam)
            {
                var d = Flat(p - Seven.transform.position);
                float min = 0.45f + BodyRadius * s;
                if (d.magnitude < min) p = Seven.transform.position + d.normalized * min;
                p.y = 0f;
            }
            var moved = Flat(p - transform.position);
            transform.position = p;
            if (moved.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(moved), 400f * Time.deltaTime);
            PlayClip("walk", 0.2f, Mathf.Clamp(speed / (StrideSpeed * s), 0.6f, 4f));
            return false;
        }

        void Choose(string action, float seconds)
        {
            Action = action;
            _actionUntil = Time.time + seconds;
            if (action == "wander")
            {
                var a = Random.Range(0f, Mathf.PI * 2f);
                _target = Seven.transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Random.Range(1.5f, 3.5f);
                _target = Ground.ClampToArena(_target, 0.6f); _target.y = 0f;
            }
        }

        // ------------------------------------------------------------------------------------- roam
        void Roam()
        {
            float d7 = Flat(Seven.transform.position - transform.position).magnitude;
            _sevenStill = (Seven.transform.position - _sevenLast).sqrMagnitude < 1e-6f ? _sevenStill + Time.deltaTime : 0f;
            _sevenLast = Seven.transform.position;

            if (ForceAction != null && Action != ForceAction) Choose(ForceAction, 999f);
            if (ForceTarget.HasValue) { Action = "goto"; MoveTo(ForceTarget.Value, StrollSpeed); return; }

            bool resting = Action == "sit" || Action == "wash" || Action == "nap";
            if (ForceAction == null && (d7 > FollowDistance + (resting ? 1.8f : 0f)))
            {
                Action = "follow";
                var side = Flat(transform.position - Seven.transform.position).normalized;
                var t = Seven.transform.position + side * 1.3f;
                MoveTo(t, d7 > FollowDistance + 2f ? TrotSpeed : StrollSpeed);
                return;
            }
            if (Action == "follow" || (ForceAction == null && Time.time > _actionUntil))
            {
                float r = Random.value;
                if (_sevenStill > 6f && r < 0.45f) Choose("nap", Random.Range(9f, 14f));
                else if (r < 0.35f) Choose("sit", Random.Range(3f, 6f));
                else if (r < 0.6f) Choose("wash", Random.Range(3.5f, 5.5f));
                else Choose("wander", 8f);
            }
            switch (Action)
            {
                case "wander":
                    if (MoveTo(_target, StrollSpeed)) Choose("sit", Random.Range(2f, 4f));
                    break;
                case "sit": PlayClip("sit", 0.35f); break;
                case "wash": PlayClip("wash", 0.35f); break;
                case "nap": PlayClip("sleep", 0.5f); break;
                case "walk": MoveTo(_target, StrollSpeed); break;
                default: PlayClip("idle", 0.3f); break;
            }
        }

        // ------------------------------------------------------------------------------------- lap
        void OnSevenStands()
        {
            if (Current == Mode.OnLap || Current == Mode.HopUp)
            {
                StartHop(LaunchPoint, RoamScale, Mode.HopDown);
            }
            else Current = Mode.Roam;
        }

        void StartHop(Vector3 to, float scaleTo, Mode m)
        {
            Current = m;
            _hopT = 0f;
            _hopFrom = transform.position;
            _scaleFrom = Scale; _scaleTo = scaleTo;
            _hopTo = to;
            PlayClip("jump", 0.1f, 0.8f / HopTime);
            _shimmer = 1f;
            _motes.Emit(40);
        }
        Vector3 _hopTo;

        void UpdateHop()
        {
            _hopT += Time.deltaTime / HopTime;
            // Jump clip: crouch until ~0.4, airborne 0.4-0.85, landing after.
            float u = Mathf.InverseLerp(0.4f, 0.85f, _hopT);
            float e = Mathf.SmoothStep(0f, 1f, u);
            var to = Current == Mode.HopUp ? LapPoint : _hopTo;
            var p = Vector3.Lerp(_hopFrom, to, e);
            p.y += Mathf.Sin(u * Mathf.PI) * HopHeight;
            transform.position = p;
            var face = Current == Mode.HopUp ? Flat(to - _hopFrom) : Flat(_hopTo - _hopFrom);
            var endRot = Current == Mode.HopUp ? LapRotation : Quaternion.LookRotation(face.sqrMagnitude > 1e-4f ? face : transform.forward);
            if (face.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.Slerp(Quaternion.LookRotation(face), endRot, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1f, _hopT)));
            transform.localScale = Vector3.one * Mathf.Lerp(_scaleFrom, _scaleTo, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.9f, _hopT)));
            if (_hopT > 0.25f && _hopT < 0.95f) _motes.Emit(Mathf.CeilToInt(90f * Time.deltaTime));
            if (_hopT >= 1f)
            {
                transform.localScale = Vector3.one * _scaleTo;
                if (Current == Mode.HopUp) { Current = Mode.OnLap; _lapSince = Time.time; PlayClip("idle", 0.2f); }
                else { Current = Mode.Roam; transform.rotation = Quaternion.LookRotation(Flat(transform.forward)); Choose("sit", 3f); }
            }
        }
        float _lapSince, _toChairTime, _detour;

        void Update()
        {
            switch (Current)
            {
                case Mode.Roam: Roam(); break;
                case Mode.ToChair:
                    _toChairTime += Time.deltaTime;
                    // Arrive, or close enough after a while (furniture in the way): never keep 7 waiting.
                    if (MoveTo(LaunchPoint, TrotSpeed) || (_toChairTime > 5f && Flat(LaunchPoint - transform.position).magnitude < 0.9f))
                        Current = Mode.Waiting;
                    else if (_toChairTime > 9f) { transform.position = LaunchPoint; Current = Mode.Waiting; }
                    break;
                case Mode.Waiting:
                    PlayClip("sit", 0.3f);
                    var look = Flat(LapPoint - transform.position);
                    if (look.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), 300f * Time.deltaTime);
                    if (Seven.IsSeated && Seven.State == "sitting_idle" && Seven.Tn > 0.35f) StartHop(LapPoint, LapScale, Mode.HopUp);
                    break;
                case Mode.HopUp:
                case Mode.HopDown:
                    UpdateHop(); break;
                case Mode.OnLap:
                    if (Time.time - _lapSince > 0.6f) PlayClip("sleep", 0.6f);
                    break;
            }
            // Shimmer: brighter rim while changing size.
            _shimmer = Mathf.MoveTowards(_shimmer, (Current == Mode.HopUp || Current == Mode.HopDown) ? 1f : 0f, Time.deltaTime * 2.5f);
            foreach (var r in _renderers)
            {
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat(RimId, _baseRim + _shimmer * 1.1f);
                r.SetPropertyBlock(_mpb);
            }
        }

        void LateUpdate()
        {
            if (Current == Mode.OnLap)
            {
                // Ride on her lap: follow her thighs (they move with her breathing and the chair's tilt).
                transform.position = LapPoint;
                transform.rotation = LapRotation;
            }
            var sh = _motes.shape;
            sh.radius = 0.22f * Scale;
            _motes.transform.position = transform.position + Vector3.up * 0.15f * Scale;
        }

        ParticleSystem MakeMotes()
        {
            var go = new GameObject("CatShimmer");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = false;   // keeps simulating; particles come only from Emit()
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor = new Color(1f, 1f, 1f, 1f);
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = new Material(Additive);
            m.SetTexture("_MainTex", FxUtil.Dot(64));
            m.SetFloat("_Intensity", 1.6f);
            r.sharedMaterial = m;
            ps.Play();
            return ps;
        }
    }
}
