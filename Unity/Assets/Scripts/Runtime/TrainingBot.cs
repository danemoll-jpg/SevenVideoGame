using System;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Sparring bot. Reacts to hits, throws a slow punch every few seconds, and after the finisher goes
    /// Knocked Down -> Getting Up -> Dazed (2 s) -> Fighting Idle. It always recovers; nothing dies.
    /// </summary>
    public class TrainingBot : FighterBase
    {
        public SevenFighter Seven;
        public bool AutoPunch = true;
        public float PunchSpeed = 0.6f;          // slow, telegraphed
        public Vector2 PunchEvery = new Vector2(3f, 4.5f);

        public enum Mode { Idle, Hit, Punch, Stagger, Down, GetUp, Dazed, Lifted }
        public Mode Current { get; private set; } = Mode.Idle;

        public bool IsDown => Current == Mode.Down || Current == Mode.GetUp;
        public bool IsLifted => Current == Mode.Lifted;
        public bool CanLift => !IsDown && Current != Mode.Lifted;

        [Header("Telekinesis lift")]
        public float LiftHeight = 0.8f;           // metres above the ground while held
        public float LiftRiseTime = 0.45f;
        /// <summary>Current height above the ground (eases up while lifted, eases down when set down).</summary>
        public float Height { get; private set; }
        public float MaxHeightThisLift { get; private set; }
        /// <summary>Raised when the bot touches down after being thrown.</summary>
        public event Action Landed;
        float _heightVel, _liftTime;
        bool _thrown;
        protected override float ExtraHeight => Height;

        /// <summary>Raised when the bot's punch resolves: true = 7 blocked/dodged it.</summary>
        public event Action<bool> PunchResolved;

        const float PunchHitAt = 0.40f;          // right-hand extension peak in "Punching"
        float _punchTimer;
        bool _punchDone;
        float _dazedUntil;

        protected override void Awake()
        {
            base.Awake();
            Play("fight_idle", 0f);
            _punchTimer = UnityEngine.Random.Range(PunchEvery.x, PunchEvery.y);
        }

        float DistToSeven()
        {
            var d = Seven.transform.position - transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        protected override void Update()
        {
            base.Update();
            if (Seven == null) return;
            if (!IsDown) FaceTowards(Seven.transform.position, Current == Mode.Punch ? 90f : Current == Mode.Lifted ? 60f : 200f);
            UpdateHeight();

            switch (Current)
            {
                case Mode.Idle:
                    if (AutoPunch)
                    {
                        _punchTimer -= Time.deltaTime;
                        if (_punchTimer <= 0f)
                        {
                            if (DistToSeven() < 1.7f) StartPunch();
                            else _punchTimer = 0.5f;
                        }
                    }
                    break;
                case Mode.Punch:
                    if (!_punchDone && Tn >= PunchHitAt) ResolvePunch();
                    if (Tn >= 0.9f) ToIdle();
                    break;
                case Mode.Hit:
                case Mode.Stagger:
                    if (Tn >= 0.8f) ToIdle();
                    break;
                case Mode.Lifted:
                    break;
                case Mode.Down:
                    if (Tn >= 1.0f && Height < 0.01f) { Current = Mode.GetUp; Play("get_up", 0.2f); }
                    break;
                case Mode.GetUp:
                    if (Tn >= 0.97f) { Current = Mode.Dazed; Play("dazed", 0.25f); _dazedUntil = Time.time + 2f; }
                    break;
                case Mode.Dazed:
                    if (Time.time >= _dazedUntil) ToIdle();
                    break;
            }
        }

        void UpdateHeight()
        {
            if (Current == Mode.Lifted)
            {
                _liftTime += Time.deltaTime;
                float target = LiftHeight + 0.07f * Mathf.Sin(_liftTime * 2.3f);   // slow hover bob
                Height = Mathf.SmoothDamp(Height, target, ref _heightVel, LiftRiseTime);
                MaxHeightThisLift = Mathf.Max(MaxHeightThisLift, Height);
            }
            else if (Height > 0f)
            {
                // Set down safely: ease to the ground, never drop.
                Height = Mathf.SmoothDamp(Height, 0f, ref _heightVel, _thrown ? 0.22f : 0.3f, 4f);
                if (Height < 0.004f)
                {
                    Height = 0f; _heightVel = 0f;
                    if (_thrown) { _thrown = false; Landed?.Invoke(); }
                }
            }
        }

        /// <summary>7 holds the bot with telekinesis: it floats up and hangs there, struggling.</summary>
        public void BeginLift(SevenFighter by)
        {
            if (!CanLift) return;
            Current = Mode.Lifted;
            _liftTime = 0f;
            MaxHeightThisLift = 0f;
            // Round 7 (3c): Dan's Mixamo "float" clip (hanging in the air) when present; else the woozy dazed loop.
            if (HasState("float")) Play("float", 0.3f, 0.9f); else Play("dazed", 0.3f, 0.75f);
        }

        /// <summary>7 lets go: pushed back several metres while being set down, lands, gets up dazed, recovers.</summary>
        public void ReleaseLift(Vector3 dir, float distance)
        {
            if (Current != Mode.Lifted) return;
            _thrown = true;
            Current = Mode.Down;
            dir.y = 0f;
            transform.rotation = Quaternion.LookRotation(-dir);   // faces 7, falls back away from her
            Play("knocked_down", 0.15f);
            Slide(dir, distance, 0.8f);
        }

        void ToIdle()
        {
            Current = Mode.Idle;
            Play("fight_idle", 0.25f);
            _punchTimer = UnityEngine.Random.Range(PunchEvery.x, PunchEvery.y);
        }

        /// <summary>Throw the slow punch now (also used by the check run).</summary>
        public void StartPunch()
        {
            if (Current != Mode.Idle) return;
            Current = Mode.Punch;
            _punchDone = false;
            Play("punch", 0.15f, PunchSpeed);
        }

        void ResolvePunch()
        {
            _punchDone = true;
            bool defended = Seven.IsBlocking || Seven.IsDodging;
            if (defended)
            {
                // 7 blocked or dodged in time: the bot staggers back.
                Current = Mode.Stagger;
                Play("hit_body", 0.1f);
                Slide(-transform.forward, 0.45f, 0.35f);
            }
            else
            {
                var to = Seven.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude < 1.55f && Vector3.Dot(transform.forward, to.normalized) > 0.5f)
                    Seven.GetHit(to.normalized);
            }
            PunchResolved?.Invoke(defended);
        }

        public bool CanBeHit => !IsDown;

        /// <summary>7's blow lands. Head/body reaction plus knockback, or the knockdown for the finisher.</summary>
        public bool TakeHit(bool head, Vector3 dir, float knock, bool finisher)
        {
            if (!CanBeHit) return false;
            if (finisher)
            {
                Current = Mode.Down;
                transform.rotation = Quaternion.LookRotation(-new Vector3(dir.x, 0f, dir.z)); // fall away from 7
                Play("knocked_down", 0.08f);
                return true;
            }
            Current = Mode.Hit;
            Play(head ? "hit_head" : "hit_body", 0.03f);     // Round 8: the reaction starts on the hit frame
            Slide(dir, knock, 0.25f);
            return true;
        }

        /// <summary>Telekinesis push: the bot slides back a short distance.</summary>
        public bool Push(Vector3 dir, float distance)
        {
            if (!CanBeHit) return false;
            Current = Mode.Hit;
            Play("hit_body", 0.08f);
            Slide(dir, distance, 0.5f);
            return true;
        }
    }
}
