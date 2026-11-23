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

        public enum Mode { Free, Attack, Block, Dodge, Flinch, Lift }
        public Mode Current { get; private set; } = Mode.Free;

        /// <summary>Raised when one of 7's attacks connects (attack state name).</summary>
        public event Action<string> HitLanded;

        // Scripted control for check runs (-capture). When ScriptDriven is true, live input is ignored.
        [NonSerialized] public bool ScriptDriven;
        [NonSerialized] public Vector2 ScriptMove;
        [NonSerialized] public bool ScriptBlockHeld;
        [NonSerialized] public bool ScriptTkHeld;

        [Header("Telekinesis lift (hold T / d-pad up)")]
        public float MaxLiftSeconds = 4f;
        public float ThrowDistance = 3.5f;
        /// <summary>Telekinesis phase for the effects: charging (wind-up), lifting (holding the bot), released.</summary>
        public enum TkPhase { None, Charging, Lifting, Released }
        public TkPhase Tk { get; private set; }
        public float TkPhaseTime { get; private set; }     // seconds in the current phase
        public float LiftSeconds { get; private set; }
        public event Action LiftStarted, LiftReleased;
        bool _tkLetGo;   // the button was released since the move started -> tap = push

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
        bool TkHeld => ScriptDriven ? ScriptTkHeld : (Input != null && Input.Telekinesis.IsPressed());

        // ------------------------------------------------------------ Round 8: fluid fighting
        [Header("Feel (Round 8)")]
        [Tooltip("A press during a move is kept this long and fires at the first moment it may (input buffer).")]
        public float BufferTime = 0.2f;
        [Tooltip("QF10: strike clips play this much faster (snappy starts, same hit frames).")]
        public float AttackSpeed = 1.35f;
        [Tooltip("After a blow lands (or whiffs), this many seconds later a different move may cancel the rest of it.")]
        public float CancelDelay = 0.07f;
        public float AttackFade = 0.08f, ChainFade = 0.1f, ReturnFade = 0.12f;
        [Tooltip("Hit-pause on connect (real seconds) and camera nudge.")]
        public float HitPause = 0.05f, FinisherPause = 0.08f, HitNudge = 0.035f;
        string _buffered; float _bufferedAt; bool _dodgeBuffered; float _dodgeAt;
        float _attackStart;
        /// <summary>Check runs: (state, time) every time a move actually starts; (state, press time) for every press.</summary>
        public event Action<string, float> MoveStarted, Pressed;

        bool InRecovery => Current == Mode.Attack && _hitIdx >= _atk.Hits.Length && Tn >= LastHit(_atk) + CancelDelay / Mathf.Max(0.05f, Length(_atk.State));
        static float LastHit(Attack a) => a.Hits[a.Hits.Length - 1];

        bool CanStart(string state)
        {
            switch (Current)
            {
                case Mode.Free: return true;
                case Mode.Attack:
                    if (Tn >= _atk.End) return true;
                    // combo chain: a different move may cancel the recovery once this one's blow has landed
                    return state != _atk.State && !_atk.Finisher && InRecovery;
                case Mode.Dodge: return Tn >= 0.62f;
                default: return false;
            }
        }

        public void DoAttack(string state)
        {
            Pressed?.Invoke(state, Time.time);
            if (Array.Find(Attacks, a => a.State == state) == null) return;
            if (CanStart(state)) StartAttack(state);
            else if (Current != Mode.Lift) { _buffered = state; _bufferedAt = Time.time; }
        }

        void StartAttack(string state)
        {
            bool chained = Current == Mode.Attack;
            _buffered = null;
            Freeze(false);
            _atk = Array.Find(Attacks, a => a.State == state);
            _hitIdx = 0;
            Current = Mode.Attack;
            _attackStart = Time.time;
            if (Bot != null)
            {
                // small step in to close the distance (never through the bot)
                float d = Dist();
                if (!_atk.Push && d > _atk.Range * 0.72f && d < _atk.Range * 1.8f)
                    Slide(Bot.transform.position - transform.position, Mathf.Min(d - _atk.Range * 0.72f, 0.45f), 0.2f);
            }
            // QF10 (shared feel with the chapter fights): strikes play faster and turn her to the bot at once (soft lock)
            if (Bot != null && !_atk.Push)
            {
                var to = Bot.transform.position - transform.position; to.y = 0f;
                if (to.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, to) < 75f) transform.rotation = Quaternion.LookRotation(to);
            }
            Play(state, chained ? ChainFade : AttackFade, _atk.Push || _atk.Finisher ? 1f : AttackSpeed);
            MoveStarted?.Invoke(state, Time.time);
            if (_atk.Push) { _tkLetGo = false; SetTk(TkPhase.Charging); }
        }        void SetTk(TkPhase p) { Tk = p; TkPhaseTime = 0f; }

        public void DoDodge()
        {
            Pressed?.Invoke("dodge", Time.time);
            // dodge interrupts an attack's recovery (after its blow) and a flinch after a short moment
            bool ok = Current == Mode.Free || Current == Mode.Block
                      || (Current == Mode.Attack && (Tn >= _atk.End || _hitIdx >= _atk.Hits.Length))
                      || (Current == Mode.Flinch && Time.time >= _flinchUntil - 0.2f);
            if (!ok) { if (Current != Mode.Lift && Current != Mode.Dodge) { _dodgeBuffered = true; _dodgeAt = Time.time; } return; }
            _dodgeBuffered = false; _buffered = null;
            Freeze(false);
            Current = Mode.Dodge;
            Play("dodge", 0.08f);
            MoveStarted?.Invoke("dodge", Time.time);
        }

        System.Collections.IEnumerator HitStop(float seconds)
        {
            if (PauseMenu.IsOpen || seconds <= 0f) yield break;
            Time.timeScale = 0.05f;
            float t0 = Time.realtimeSinceStartup;
            if (DemoBoot.Deterministic) { for (int i = 0; i < Mathf.Max(1, Mathf.RoundToInt(seconds / DemoBoot.CaptureStep)); i++) { if (PauseMenu.IsOpen) yield break; yield return null; } }   // QF29
            else while (Time.realtimeSinceStartup - t0 < seconds) { if (PauseMenu.IsOpen) yield break; yield return null; }
            if (!PauseMenu.IsOpen) Time.timeScale = DemoBoot.TestSpeed;
        }

        /// <summary>The bot's punch landed on an unguarded 7: short push back, no injury.</summary>
        public void GetHit(Vector3 dir)
        {
            if (Current == Mode.Lift) ReleaseLift();
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
            TkPhaseTime += Time.deltaTime;
            if (!TkHeld) _tkLetGo = true;
            if (Tk == TkPhase.Released && TkPhaseTime > 1.2f) SetTk(TkPhase.None);
            if (!ScriptDriven && Input != null && !PauseMenu.IsOpen)
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

            // buffered presses fire at the first frame they may
            if (_buffered != null && Time.time - _bufferedAt > BufferTime) _buffered = null;
            if (_dodgeBuffered && Time.time - _dodgeAt > BufferTime) _dodgeBuffered = false;
            if (_dodgeBuffered) { var c = Current; DoDodge(); if (Current == Mode.Dodge && c != Mode.Dodge) _dodgeBuffered = false; }
            if (_buffered != null && CanStart(_buffered)) StartAttack(_buffered);

            switch (Current)
            {
                case Mode.Free: UpdateFree(); break;
                case Mode.Attack:
                    // smooth turn toward the target at the start of the move (no snapping)
                    if (Bot != null && Time.time - _attackStart < 0.18f && Dist() < _atk.Range * 1.8f) FaceTowards(Bot.transform.position, 1100f);
                    // block can interrupt the recovery
                    if (BlockHeld && _hitIdx >= _atk.Hits.Length && !_atk.Finisher) { Freeze(false); Current = Mode.Block; Play("block", 0.08f); MoveStarted?.Invoke("block", Time.time); break; }
                    while (_hitIdx < _atk.Hits.Length && Tn >= _atk.Hits[_hitIdx])
                    {
                        _hitIdx++;
                        if (_atk.Push && !_tkLetGo && TryStartLift()) return;
                        TryHit();
                    }
                    if (_atk.Push && Tk == TkPhase.Charging && _hitIdx >= _atk.Hits.Length) SetTk(TkPhase.Released);
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
                case Mode.Lift:
                    LiftSeconds += Time.deltaTime;
                    if (Bot != null) FaceTowards(Bot.transform.position, 90f);
                    if (!TkHeld || LiftSeconds >= MaxLiftSeconds) ReleaseLift();
                    break;
            }
            KeepApartFromBot();
        }

        /// <summary>Button still held at the moment the push would land: lift the bot instead.</summary>
        bool TryStartLift()
        {
            if (Bot == null || !Bot.CanLift) return false;
            var to = Bot.transform.position - transform.position; to.y = 0f;
            if (to.magnitude > _atk.Range || Vector3.Dot(transform.forward, to.normalized) < 0.5f) return false;
            Current = Mode.Lift;
            LiftSeconds = 0f;
            Freeze(true);                      // hold the peak (hands-forward) pose of the magic attack
            Bot.BeginLift(this);
            SetTk(TkPhase.Lifting);
            LiftStarted?.Invoke();
            HitLanded?.Invoke("telekinesis_lift");
            return true;
        }

        /// <summary>Let go: the bot is pushed back several metres and set down safely.</summary>
        public void ReleaseLift()
        {
            if (Current != Mode.Lift) return;
            Freeze(false);                     // finish the push gesture
            Current = Mode.Attack;
            var dir = Bot.transform.position - transform.position; dir.y = 0f;
            Bot.ReleaseLift(dir.normalized, ThrowDistance);
            SetTk(TkPhase.Released);
            LiftReleased?.Invoke();
        }

        /// <summary>
        /// Round 7 (3c): the telekinesis hold is the frozen peak pose of the telekinesis clip; this layers a little
        /// life on top: slow breathing in the chest, a slight sway from the hips, a faint strain tremble in the arms
        /// and hands, and a small hand adjustment that follows the bot's height. Subtle by design (degrees, not tens).
        /// </summary>
        void LateUpdate()
        {
            if (Current != Mode.Lift) { _holdW = 0f; return; }
            _holdW = Mathf.MoveTowards(_holdW, 1f, Time.deltaTime * 2.5f);      // ease the layer in
            float t = LiftSeconds, w = _holdW;
            void Rot(HumanBodyBones b, Vector3 axisLocal, float deg)
            {
                var tr = Bone(b); if (tr != null) tr.localRotation *= Quaternion.AngleAxis(deg * w, axisLocal);
            }
            float breathe = Mathf.Sin(t * 2f * Mathf.PI / 3.6f);                    // ~3.6 s breath
            float sway = Mathf.Sin(t * 2f * Mathf.PI / 5.3f + 0.7f);
            float strain = Mathf.Clamp01(LiftSeconds / MaxLiftSeconds) * 0.6f + 0.4f;   // grows the longer she holds
            float trem(float seed) => (Mathf.PerlinNoise(t * 7.5f, seed) - 0.5f) * 2f;
            Rot(HumanBodyBones.Chest, Vector3.right, -1.6f * breathe);
            Rot(HumanBodyBones.UpperChest, Vector3.right, -1.0f * breathe);
            Rot(HumanBodyBones.Hips, Vector3.forward, 1.3f * sway);
            Rot(HumanBodyBones.Spine, Vector3.forward, -0.9f * sway);
            foreach (var (b, seed, amp) in new[] { (HumanBodyBones.RightUpperArm, 1.1f, 0.7f), (HumanBodyBones.LeftUpperArm, 2.3f, 0.7f),
                                                   (HumanBodyBones.RightLowerArm, 3.7f, 0.9f), (HumanBodyBones.LeftLowerArm, 4.1f, 0.9f),
                                                   (HumanBodyBones.RightHand, 5.3f, 1.6f), (HumanBodyBones.LeftHand, 6.7f, 1.6f) })
                Rot(b, new Vector3(trem(seed), trem(seed + 9f), trem(seed + 17f)).normalized, amp * strain * Mathf.Abs(trem(seed + 31f) + 0.4f));
            // The hands follow the bot a little as it bobs (raise / lower the forearms a degree or two).
            float follow = Bot != null ? Mathf.Clamp((Bot.Height - Bot.LiftHeight) * 30f, -3f, 3f) : 0f;
            Rot(HumanBodyBones.RightLowerArm, Vector3.right, -follow);
            Rot(HumanBodyBones.LeftLowerArm, Vector3.right, -follow);
        }
        float _holdW;

        void ToFree()
        {
            Current = Mode.Free;
            Play("fight_idle", ReturnFade);
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
                if (State != "walk") Play("walk", 0.12f, 1f, false);
                if (Bot != null && Dist() < 6f) FaceBot(540f);
                else FaceTowards(transform.position + v, 540f);
            }
            else
            {
                if (State != "fight_idle") Play("fight_idle", 0.15f, 1f, false);
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
            if (landed)
            {
                HitLanded?.Invoke(_atk.State);
                StartCoroutine(HitStop(_atk.Finisher ? FinisherPause : HitPause));
                var cam = CameraTransform != null ? CameraTransform.GetComponent<FightCamera>() : null;
                if (cam != null) cam.Shake(_atk.Finisher ? HitNudge * 2f : HitNudge);
            }

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
