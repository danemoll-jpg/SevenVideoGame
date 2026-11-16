using System;
using UnityEngine;

namespace Seven
{
    /// <summary>7's fight controls. She never kills: every attack only knocks the bot back or down.</summary>
    public class SevenFighter : FighterBase
    {
        public TrainingBot Bot;
        public Transform CameraTransform;
        public FightInput Input;
        public float WalkSpeed = 1.6f;

        public enum Mode { Free, Attack, Block, Dodge, Flinch }
        public Mode Current { get; private set; } = Mode.Free;

        /// <summary>Raised when one of 7's attacks connects (attack state name).</summary>
        public event Action<string> HitLanded;

        // Scripted control for check runs (-capture). When ScriptDriven is true, live input is ignored.
        [NonSerialized] public bool ScriptDriven;
        [NonSerialized] public Vector2 ScriptMove;
        [NonSerialized] public bool ScriptBlockHeld;

        public bool IsBlocking => Current == Mode.Block;
        public bool IsDodging => Current == Mode.Dodge && Tn > 0.06f && Tn < 0.7f;

        class Attack
        {
            public string State;
            public float[] Hits;      // normalized times at which the blow lands (from limb-extension peaks)
            public bool Head;
            public float Range;       // root-to-root distance in metres
            public float Knock;       // knockback metres
            public float End;         // normalized time at which 7 may act again
            public bool Finisher, Push;
        }

        static readonly Attack[] Attacks =
        {
            new Attack { State = "jab", Hits = new[] { 0.28f }, Head = true, Range = 1.45f, Knock = 0.15f, End = 0.62f },
            new Attack { State = "cross", Hits = new[] { 0.20f, 0.33f }, Head = true, Range = 1.5f, Knock = 0.2f, End = 0.6f },
            new Attack { State = "hook", Hits = new[] { 0.33f }, Head = true, Range = 1.35f, Knock = 0.3f, End = 0.66f },
            new Attack { State = "roundhouse", Hits = new[] { 0.43f }, Head = true, Range = 1.75f, Knock = 0.5f, End = 0.75f },
            new Attack { State = "mma_kick", Hits = new[] { 0.40f }, Head = false, Range = 1.75f, Knock = 0.45f, End = 0.72f },
            new Attack { State = "finisher", Hits = new[] { 0.29f }, Range = 2.3f, End = 0.93f, Finisher = true },
            new Attack { State = "telekinesis", Hits = new[] { 0.49f }, Range = 7f, End = 0.8f, Push = true },
        };

        Attack _atk;
        int _hitIdx;
        float _flinchUntil;
        float _blockFreezeAt = 0.32f; // guard is up here (hand-height peak of Center Block)

        protected override void Awake()
        {
            base.Awake();
            Play("fight_idle", 0f);
        }

        Vector2 MoveInput => ScriptDriven ? ScriptMove : (Input != null ? Input.Move.ReadValue<Vector2>() : Vector2.zero);
        bool BlockHeld => ScriptDriven ? ScriptBlockHeld : (Input != null && Input.Block.IsPressed());

        public void DoAttack(string state)
        {
            if (Current == Mode.Block || Current == Mode.Dodge || Current == Mode.Flinch) return;
            if (Current == Mode.Attack && Tn < _atk.End) return;
            _atk = Array.Find(Attacks, a => a.State == state);
            if (_atk == null) return;
            _hitIdx = 0;
            Current = Mode.Attack;
            if (Bot != null && Dist() < _atk.Range * 1.6f) SnapFace(Bot.transform.position);
            Play(state, 0.08f);
        }

        public void DoDodge()
        {
            if (Current == Mode.Dodge || Current == Mode.Flinch) return;
            if (Current == Mode.Attack && Tn < _atk.End) return;
            Current = Mode.Dodge;
            Play("dodge", 0.08f);
        }

        /// <summary>The bot's punch landed on an unguarded 7: short push back, no injury.</summary>
        public void GetHit(Vector3 dir)
        {
            Current = Mode.Flinch;
            _flinchUntil = Time.time + 0.35f;
            Freeze(false);
            Slide(dir, 0.35f, 0.25f);
            Play("fight_idle", 0.1f);
            var cam = CameraTransform != null ? CameraTransform.GetComponent<FightCamera>() : null;
            if (cam != null) cam.Shake(0.06f);
        }

        float Dist()
        {
            var d = Bot.transform.position - transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        void SnapFace(Vector3 target)
        {
            var to = target - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(to);
        }

        protected override void Update()
        {
            base.Update();
            if (!ScriptDriven && Input != null)
            {
                if (Input.Jab.WasPressedThisFrame()) DoAttack("jab");
                if (Input.Cross.WasPressedThisFrame()) DoAttack("cross");
                if (Input.Hook.WasPressedThisFrame()) DoAttack("hook");
                if (Input.Roundhouse.WasPressedThisFrame()) DoAttack("roundhouse");
                if (Input.MmaKick.WasPressedThisFrame()) DoAttack("mma_kick");
                if (Input.Finisher.WasPressedThisFrame()) DoAttack("finisher");
                if (Input.Telekinesis.WasPressedThisFrame()) DoAttack("telekinesis");
                if (Input.Dodge.WasPressedThisFrame()) DoDodge();
            }

            switch (Current)
            {
                case Mode.Free: UpdateFree(); break;
                case Mode.Attack:
                    while (_hitIdx < _atk.Hits.Length && Tn >= _atk.Hits[_hitIdx]) { TryHit(); _hitIdx++; }
                    if (Tn >= 0.97f || (Tn >= _atk.End && (MoveInput.sqrMagnitude > 0.04f || BlockHeld))) ToFree();
                    break;
                case Mode.Block:
                    if (!BlockHeld) { Freeze(false); ToFree(); break; }
                    if (Tn >= _blockFreezeAt) Freeze(true);
                    FaceBot(360f);
                    break;
                case Mode.Dodge:
                    if (Tn >= 0.85f) ToFree();
                    break;
                case Mode.Flinch:
                    if (Time.time >= _flinchUntil) ToFree();
                    break;
            }
            KeepApartFromBot();
        }

        void ToFree()
        {
            Current = Mode.Free;
            Play("fight_idle", 0.18f);
        }

        void UpdateFree()
        {
            if (BlockHeld)
            {
                Current = Mode.Block;
                Play("block", 0.08f);
                return;
            }
            var m = MoveInput;
            if (m.sqrMagnitude > 0.04f)
            {
                var fwd = CameraTransform != null ? CameraTransform.forward : Vector3.forward;
                fwd.y = 0f; fwd.Normalize();
                var right = new Vector3(fwd.z, 0f, -fwd.x);
                var v = (fwd * m.y + right * m.x);
                if (v.sqrMagnitude > 1f) v.Normalize();
                transform.position += v * WalkSpeed * Time.deltaTime;
                if (State != "walk") Play("walk", 0.15f, 1f, false);
                if (Bot != null && Dist() < 6f) FaceBot(540f);
                else FaceTowards(transform.position + v, 540f);
            }
            else
            {
                if (State != "fight_idle") Play("fight_idle", 0.2f, 1f, false);
                FaceBot(240f);
            }
        }

        void FaceBot(float degPerSec)
        {
            if (Bot != null && Dist() < 6f) FaceTowards(Bot.transform.position, degPerSec);
        }

        void TryHit()
        {
            if (Bot == null) return;
            var to = Bot.transform.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist > _atk.Range) return;
            if (Vector3.Dot(transform.forward, to.normalized) < (_atk.Push ? 0.5f : 0.35f)) return;
            bool landed;
            if (_atk.Push) landed = Bot.Push(to.normalized, 1.6f);
            else landed = Bot.TakeHit(_atk.Head, to.normalized, _atk.Knock, _atk.Finisher);
            if (landed) HitLanded?.Invoke(_atk.State);
        }

        void KeepApartFromBot()
        {
            if (Bot == null) return;
            var d = transform.position - Bot.transform.position;
            d.y = 0f;
            float min = Bot.IsDown ? 0.55f : 0.8f;
            if (d.magnitude < min && d.sqrMagnitude > 1e-6f)
                transform.position = Bot.transform.position + d.normalized * min;
        }
    }
}
