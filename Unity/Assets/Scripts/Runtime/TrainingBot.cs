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

        public enum Mode { Idle, Hit, Punch, Stagger, Down, GetUp, Dazed }
        public Mode Current { get; private set; } = Mode.Idle;

        public bool IsDown => Current == Mode.Down || Current == Mode.GetUp;

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
            if (!IsDown) FaceTowards(Seven.transform.position, Current == Mode.Punch ? 90f : 200f);

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
                case Mode.Down:
                    if (Tn >= 1.0f) { Current = Mode.GetUp; Play("get_up", 0.2f); }
                    break;
                case Mode.GetUp:
                    if (Tn >= 0.97f) { Current = Mode.Dazed; Play("dazed", 0.25f); _dazedUntil = Time.time + 2f; }
                    break;
                case Mode.Dazed:
                    if (Time.time >= _dazedUntil) ToIdle();
                    break;
            }
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
            Play(head ? "hit_head" : "hit_body", 0.06f);
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
