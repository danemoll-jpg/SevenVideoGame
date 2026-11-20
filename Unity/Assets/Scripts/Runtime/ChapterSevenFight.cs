using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF9 / QF10: 7's side of the chapter fighting (the terrace tutorial with the two Scree, and anything later). Reuses
    /// the fight demo's moves on her own controller - jab → cross → hook as a combo on Attack, hold Block, Dodge, and the
    /// finisher clip as a nonlethal takedown on a dazed Scree. Three unblocked blows put her down (<see cref="FightDown"/>);
    /// the director then resets the fight. <see cref="BeginFight"/> / <see cref="EndFight"/> switch it on and off.
    /// QF10 (Dan: "still clunky" - fast, light, easy to control):
    ///  - snappy starts: each strike's wind-up is trimmed so its first active frame comes <see cref="ActiveAfter"/> s after
    ///    the button (clips at <see cref="AttackSpeed"/>x);
    ///  - cancels + buffering: once a blow has landed (or whiffed) its recovery cancels into the next attack, a dodge or a
    ///    block at once; a press up to <see cref="Buffer"/> s early is kept and fires at the first moment it may;
    ///  - light movement: quick acceleration / deceleration, fast turning, a short fast dodge with a brief invulnerability;
    ///  - gentle aim assist: an attack turns her toward the nearest opponent in front of her (soft lock);
    ///  - hit feedback: a 2-3 frame hit-pause and a small camera nudge on contact.
    /// Still nonlethal: takedowns leave the Scree down, stunned, alive.
    /// </summary>
    /// <summary>QF17: anything 7 can fight - the terrace Scree, and the bosses (BossFight).</summary>
    public interface IFightTarget
    {
        Transform transform { get; }
        bool IsOut { get; }
        bool Dazed { get; }
        /// <summary>Her blow lands (already in reach). False = it cannot be hit now.</summary>
        bool TakeBlow(Vector3 dir, bool takedown);
    }

    public partial class ChapterSeven
    {
        public enum FightAct { Idle, Attack, Block, Dodge, Flinch, Down, Hop, Throw }
        public FightAct Act { get; private set; }
        public System.Func<IFightTarget> FightTarget;
        /// <summary>QF10: everyone she may fight (the soft lock picks the nearest in front of her).</summary>
        public System.Func<IEnumerable<IFightTarget>> FightTargets;
        // ---- QF17 (boss fights): the boss sets these for its fight; the terrace tutorial keeps the defaults
        /// <summary>Hits she can take before she goes down (the boss table's "sevenHits").</summary>
        [System.NonSerialized] public int MaxFightHits = 3;
        /// <summary>Movement in stance x this (a boss arena is bigger than the terrace); LB / Shift runs x1.5 on top.</summary>
        [System.NonSerialized] public float FightMoveScale = 1f;
        /// <summary>Jump (A) in the fight: a short hop over sweeps, shock rings and waves (bosses only).</summary>
        [System.NonSerialized] public bool FightHop;
        /// <summary>Telekinesis (RT) in the fight: where it would throw (null = nothing to throw now) and what happens at the
        /// peak of the gesture.</summary>
        public System.Func<Vector3?> TkAim; public System.Action TkFire;
        /// <summary>X / E near a fight objective (a pylon, a device): its label (null = none) and the action - takes the
        /// press instead of a strike.</summary>
        public System.Func<string> FightUseLabel; public System.Action FightUse;
        [System.NonSerialized] public bool AutoHop, AutoTk, AutoUse;
        public bool IsBlocking => Act == FightAct.Block;
        public bool IsDodging => Act == FightAct.Dodge && Time.time - _dodgeStart < DodgeInvuln;
        public bool IsAirborne => Act == FightAct.Hop && _hopY > 0.22f;
        float _hopStart, _hopY, _invulnUntil; bool _tkFired;
        public const float HopTime = 0.62f, HopHeight = 0.85f, HitInvuln = 0.9f;
        public bool InFight => Current == Mode.Fight;
        public bool FightDown => Act == FightAct.Down;
        public int HitsTaken { get; private set; }
        public int HitsLanded { get; private set; }
        public int Blocks { get; private set; }
        public int Dodges { get; private set; }
        /// <summary>Script (check run) presses: set to true for one frame.</summary>
        [System.NonSerialized] public bool AutoAttack, AutoBlockHeld, AutoDodge, AutoTakedown;
        public event System.Action<string> FightEvent;

        // ---- feel (QF10)
        public const float AttackSpeed = 1.45f;     // strike clips play this much faster
        public const float ActiveAfter = 0.13f;     // button -> first active frame (s)
        public const float Buffer = 0.2f;           // a press this early is kept, never dropped
        public const float ChainAfter = 0.05f;      // after the active frame, the next blow / dodge / block may cancel the rest
        public const float FightMoveSpeed = 2.3f, FightAccel = 16f, FightTurn = 900f;
        public const float DodgeDistance = 1.15f, DodgeTime = 0.22f, DodgeInvuln = 0.30f, DodgeRecover = 0.26f;
        public const float HitPauseFrames = 3f;     // at 60 fps: 0.05 s
        public const float AssistRange = 3.4f, AssistCone = 70f;

        static readonly string[] Combo = { "jab", "cross", "hook" };
        static readonly float[] ComboHit = { 0.32f, 0.36f, 0.42f };
        // QF18 (19c): in a boss fight her full chain - jab, cross, hook, the MMA kick, the roundhouse (her Mixamo clips; hit
        // moments measured in Round 1: mma kick 0.40, roundhouse 0.43)
        static readonly string[] BossChain = { "jab", "cross", "hook", "mma_kick", "roundhouse" };
        static readonly float[] BossChainHit = { 0.32f, 0.36f, 0.42f, 0.40f, 0.43f };
        string[] Chain => FightHop && HasState("mma_kick") && HasState("roundhouse") ? BossChain : Combo;
        float[] ChainHit => Chain == BossChain ? BossChainHit : ComboHit;
        int ChainMax => Chain.Length - 1;
        /// <summary>QF18 (19): the blow that is landing now (jab / cross / hook / mma_kick / roundhouse / finisher).</summary>
        public string LastBlow { get; private set; }
        /// <summary>QF18 (19a): optional lock-on (R3 / Tab) - off by default: in a boss fight she moves and faces freely.</summary>
        public static bool LockOn;
        bool FreeFacing => FightHop && !LockOn;
        const float FinisherHit = 0.45f;
        int _combo; bool _hitDone; float _flinchUntil;
        IFightTarget _target;
        Vector3 _fightVel;
        float _atkPressedAt = -9f, _dodgePressedAt = -9f, _dodgeStart = -9f, _blowStart, _blowPressed, _hitAtTn, _activeAt;
        string _bufAtk;                              // buffered: "attack" / "takedown"
        /// <summary>Check runs: (move, button -> active frame s, recovery -> next action s) as measured in play.</summary>
        public readonly List<string> FightTimings = new List<string>();

        public void BeginFight()
        {
            StopTraversal(); Release();
            Current = Mode.Fight; Act = FightAct.Idle; HitsTaken = 0; _combo = 0; _fightVel = Vector3.zero; _bufAtk = null;
            Crouched = false; Ground.BlockLow = true; _hopY = 0f;
            Play("fight_idle", 0.2f);
        }
        public void EndFight()
        {
            if (Current != Mode.Fight) return;
            Freeze(false);
            Current = Mode.Free; Act = FightAct.Idle; _hopY = 0f;
            MaxFightHits = 3; FightMoveScale = 1f; FightHop = false; TkAim = null; TkFire = null; FightUseLabel = null; FightUse = null;
            Play("idle", 0.4f);
        }
        /// <summary>Back on her feet at the start of the fight (after being knocked down).</summary>
        public void ResetFight() { Freeze(false); Act = FightAct.Idle; _hopY = 0f; _invulnUntil = Time.time + 1.5f; HitsTaken = 0; _combo = 0; _fightVel = Vector3.zero; _bufAtk = null; Play("fight_idle", 0.2f); }

        bool AttackPressed => AutoAttack || !ScriptDriven && Input != null && Input.Attack.WasPressedThisFrame();
        bool BlockHeld => AutoBlockHeld || !ScriptDriven && Input != null && Input.Block.IsPressed();
        bool DodgePressed => AutoDodge || !ScriptDriven && Input != null && Input.Dodge.WasPressedThisFrame();
        bool TakedownPressed => AutoTakedown || !ScriptDriven && Input != null && Input.Takedown.WasPressedThisFrame();
        bool HopPressed => AutoHop || !ScriptDriven && Input != null && Input.Jump.WasPressedThisFrame();
        bool TkPressed => AutoTk || !ScriptDriven && Input != null && Input.Telekinesis.WasPressedThisFrame();
        bool RunHeld => ScriptDriven ? ScriptRun : Input != null && Input.Run.IsPressed();

        public bool IsDefending => Act == FightAct.Block || Act == FightAct.Dodge && Time.time - _dodgeStart < DodgeInvuln;

        /// <summary>Where the trimmed wind-up starts (normalized) so the active frame comes ActiveAfter s after the press.</summary>
        float StartTn(string state, float hitTn) => Mathf.Clamp(hitTn - ActiveAfter * AttackSpeed / Length(state), 0f, hitTn - 0.03f);

        /// <summary>QF10 report: per move, button -> active frame and recovery -> next action, before (QF9) and after.</summary>
        public string FightFeelTable()
        {
            var sb = new System.Text.StringBuilder("fight feel (QF10), seconds - move: button->active before / after; recovery->next attack before / after; recovery->dodge or block before / after\n");
            for (int i = 0; i < 3; i++)
            {
                string st = Combo[i]; float L = Length(st), h = ComboHit[i];
                float bBefore = h * L, bAfter = (h - StartTn(st, h)) * L / AttackSpeed;
                float cBefore = Mathf.Max(0f, 0.62f - h) * L, cAfter = ChainAfter;
                float dBefore = Mathf.Max(0f, 0.80f - h) * L, dAfter = ChainAfter;
                sb.AppendLine($"  {st,-8} {bBefore:0.00} / {bAfter:0.00};  {cBefore:0.00} / {cAfter:0.00};  {dBefore:0.00} / {dAfter:0.00}  (clip {L:0.00}s)");
            }
            { float L = Length("finisher"); sb.AppendLine($"  takedown {FinisherHit * L:0.00} / {(FinisherHit - StartTn("finisher", FinisherHit)) * L / AttackSpeed:0.00}"); }
            { float L = Length("dodge"); sb.AppendLine($"  dodge    move 0.35 s for 0.90 m / {DodgeTime:0.00} s for {DodgeDistance:0.00} m; invulnerable ~{0.71f * L:0.00} / {DodgeInvuln:0.00} s; act again after {0.85f * L:0.00} / {DodgeRecover:0.00} s"); }
            sb.AppendLine($"  input buffer: none (QF9 queued only during an attack) / {Buffer:0.00} s for every move; movement in stance {WalkSpeed * 0.8f:0.0} m/s instant / {FightMoveSpeed:0.0} m/s with {FightAccel:0} m/s2; aim assist none / {AssistCone:0} deg cone, {AssistRange:0.0} m");
            return sb.ToString();
        }

        void UpdateFight()
        {
            Speed = 0f; Context = null;
            _target = PickTarget();
            bool atk = AttackPressed, dodge = DodgePressed, takedown = TakedownPressed, block = BlockHeld;
            bool hop = FightHop && HopPressed, tk = TkAim != null && TkPressed, use = false;
            if (FightHop && !ScriptDriven && Input != null && Input.LockOn != null && Input.LockOn.WasPressedThisFrame()) { LockOn = !LockOn; FightEvent?.Invoke(LockOn ? "lock_on" : "lock_off"); }
            AutoAttack = AutoDodge = AutoTakedown = AutoHop = AutoTk = false;
            // QF17: X next to a fight objective uses it instead of striking
            string useLabel = FightUseLabel?.Invoke();
            if ((atk || AutoUse) && useLabel != null && (Act == FightAct.Idle || Act == FightAct.Block)) { atk = false; use = true; }
            AutoUse = false;
            if (use) { Freeze(false); ToFightIdle(); FightUse?.Invoke(); FightEvent?.Invoke("use"); return; }
            if (atk) { _atkPressedAt = Time.time; _bufAtk = "attack"; }
            if (takedown) { _atkPressedAt = Time.time; _bufAtk = "takedown"; }
            if (dodge) _dodgePressedAt = Time.time;
            bool wantAtk = _bufAtk != null && Time.time - _atkPressedAt <= Buffer;
            bool wantDodge = Time.time - _dodgePressedAt <= Buffer;
            if (!wantAtk) _bufAtk = null;
            // a dazed Scree in reach: the quiet context prompt (the teaching prompt shows the first time only)
            if (_target != null && _target.Dazed && Dist(_target) < 2.4f) Context = "takedown:Takedown";
            switch (Act)
            {
                case FightAct.Down:
                    return;                                              // the director resets the fight
                case FightAct.Hop:
                {
                    float u = (Time.time - _hopStart) / HopTime;
                    var mv = MoveInput(out _);
                    if (mv.sqrMagnitude > 0.04f) { var st = mv * FightMoveSpeed * FightMoveScale * Time.deltaTime; transform.position += st; ClampToArena(); }
                    _hopY = u >= 1f ? 0f : Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI) * HopHeight;
                    var p = transform.position; p.y = Ground.Height(p.x, p.z) + _hopY; transform.position = p;
                    if (u >= 1f) { ToFightIdle(); break; }
                    return;
                }
                case FightAct.Throw:
                {
                    var aim = TkAim?.Invoke();
                    if (aim.HasValue) FaceTowards(aim.Value, 720f);
                    if (!_tkFired && Tn >= 0.45f) { _tkFired = true; TkFire?.Invoke(); FightEvent?.Invoke("tk_throw"); SetEnergyPoint(1f, 0.6f); }
                    if (_tkFired && wantDodge) { StartDodge(); return; }
                    if (Tn >= 0.72f) { ToFightIdle(); break; }
                    return;
                }
                case FightAct.Flinch:
                    if (Time.time >= _flinchUntil) ToFightIdle(); else return;
                    break;
                case FightAct.Attack:
                {
                    FaceTarget(FightTurn);
                    bool fin = State == "finisher";
                    float hitAt = fin ? FinisherHit : ChainHit[Mathf.Clamp(_combo, 0, ChainMax)];
                    if (!_hitDone && Tn >= hitAt) { _hitDone = true; _activeAt = Time.time; LandBlow(fin); }
                    bool recovering = _hitDone && Time.time - _activeAt >= ChainAfter / AttackSpeed * 1.0f;
                    if (recovering && !fin)
                    {
                        // cancel the rest of the blow: dodge / block first (defence wins), else the next attack in the chain
                        if (wantDodge) { Timing("recovery->dodge"); StartDodge(); return; }
                        if (hop) { StartHop(); return; }
                        if (tk && TkAim() is Vector3 tkA) { StartThrow(tkA); return; }
                        if (block) { Timing("recovery->block"); StartBlock(); return; }
                        if (wantAtk && _bufAtk == "attack" && _combo < ChainMax) { Timing("recovery->next"); _combo++; StartBlow(Chain[_combo]); return; }
                        if (wantAtk && _bufAtk == "takedown" && _target != null && _target.Dazed && Dist(_target) < 1.9f) { _combo = 0; StartBlow("finisher"); FightEvent?.Invoke("takedown"); return; }
                    }
                    if (Tn >= (fin ? 0.92f : 0.72f)) { _combo = wantAtk && !fin ? _combo : 0; ToFightIdle(); break; }
                    return;
                }
                case FightAct.Block:
                    FaceTarget(FightTurn);
                    if (Tn >= 0.45f) Freeze(true);
                    if (!block) { Freeze(false); ToFightIdle(); break; }
                    if (wantDodge) { StartDodge(); return; }
                    if (hop) { Freeze(false); StartHop(); return; }
                    return;
                case FightAct.Dodge:
                    if (Time.time - _dodgeStart < DodgeRecover) return;
                    ToFightIdle();
                    break;
            }
            // idle / moving (also the frame a cancel or a recovery ends: buffered presses fire here)
            if (hop) { StartHop(); return; }
            if (tk && TkAim() is Vector3 tkAt) { StartThrow(tkAt); return; }
            if (block) { StartBlock(); return; }
            if (wantDodge) { StartDodge(); return; }
            if (wantAtk && _bufAtk == "takedown" && _target != null && _target.Dazed && Dist(_target) < 1.9f) { _combo = 0; StartBlow("finisher"); FightEvent?.Invoke("takedown"); return; }
            if (wantAtk && _bufAtk == "attack") { _combo = Act == FightAct.Idle && Time.time - _activeAt < 0.6f && _combo < ChainMax ? _combo + 1 : 0; StartBlow(Chain[_combo]); return; }
            var v = MoveInput(out _);
            // light movement: quick to start and stop, turns fast, no slide
            float spd = FightMoveSpeed * FightMoveScale * (FightMoveScale > 1f && RunHeld ? 1.5f : 1f);
            var want = v.sqrMagnitude > 0.04f ? v * spd : Vector3.zero;
            _fightVel = Vector3.MoveTowards(_fightVel, want, FightAccel * Time.deltaTime);
            if (_fightVel.sqrMagnitude > 0.0004f)
            {
                var step = _fightVel * Time.deltaTime;
                int n = Mathf.Max(1, Mathf.CeilToInt(step.magnitude / 0.15f));
                for (int i = 0; i < n; i++) { transform.position += step / n; ClampToArena(); }
                string clip = spd > 3.2f && HasState("run") ? "run" : "walk";
                if (State != clip) Play(clip, 0.1f, clip == "run" ? 1f : 1.25f, false);
                if (_target != null && !FreeFacing) FaceTarget(FightTurn); else FaceTowards(transform.position + v, FightTurn);
            }
            else
            {
                if (State != "fight_idle") Play("fight_idle", 0.15f, 1f, false);
                if (!FreeFacing) FaceTarget(500f);
            }
        }

        void StartHop()
        {
            Freeze(false); Act = FightAct.Hop; _hopStart = Time.time; _hopY = 0f;
            if (HasState("jump")) Play("jump", 0.05f, 1.5f, true, 0.18f); else Play("fight_idle", 0.05f);
            FightEvent?.Invoke("hop");
        }
        void StartThrow(Vector3 at)
        {
            Freeze(false); Act = FightAct.Throw; _tkFired = false;
            FaceTowards(at, 720f);
            Play("telekinesis", 0.06f, 1.7f, true, 0.18f);
            SetEnergyPoint(1.9f, 0.15f);
            FightEvent?.Invoke("tk");
        }
        /// <summary>QF17: a boss's attack reaches her (the boss already checked block / dodge / jump). Knocked back by
        /// <paramref name="knock"/> m; <paramref name="damage"/> of her <see cref="MaxFightHits"/>; a short safe window after.</summary>
        public bool TakeBossHit(Vector3 dir, int damage, float knock)
        {
            if (Act == FightAct.Down || Time.time < _invulnUntil) return false;
            _invulnUntil = Time.time + HitInvuln;
            HitsTaken += Mathf.Max(1, damage);
            Freeze(false); _hopY = 0f;
            var p = transform.position; p.y = Ground.Height(p.x, p.z); transform.position = p;
            FightEvent?.Invoke("hit_taken");
            CameraKick.Nudge(dir * 0.09f);
            if (HitsTaken >= MaxFightHits)
            {
                Act = FightAct.Down;
                Play(HasState("crouch_idle") ? "crouch_idle" : "idle", 0.12f);
                Slide(dir, knock + 0.4f, 0.45f);
                FightEvent?.Invoke("down");
                return true;
            }
            Act = FightAct.Flinch; _flinchUntil = Time.time + 0.5f;
            Play(HasState("landing") ? "landing" : "fight_idle", 0.05f, 1.4f);
            Slide(dir, knock, 0.3f);
            return true;
        }
        public bool Invulnerable => Time.time < _invulnUntil;

        void Timing(string what) { FightTimings.Add($"{what} {Time.time - _activeAt:0.000}s"); }

        /// <summary>Soft lock: the nearest opponent in front of her (within the cone and range), else the director's target.</summary>
        IFightTarget PickTarget()
        {
            IFightTarget best = null; float bestD = AssistRange;
            if (FightTargets != null)
                foreach (var s in FightTargets())
                {
                    if (s == null || s.IsOut) continue;
                    var to = Flat(s.transform.position - transform.position); float d = to.magnitude;
                    float ang = Vector3.Angle(Flat(transform.forward), to);
                    var mv = MoveInput(out _);
                    if (mv.sqrMagnitude > 0.1f) ang = Mathf.Min(ang, Vector3.Angle(mv, to));    // or where she is steering
                    if (d < bestD && ang < AssistCone) { best = s; bestD = d; }
                }
            return best ?? (FightTarget != null ? FightTarget() : null);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        float Dist(IFightTarget s) => Flat(s.transform.position - transform.position).magnitude;
        void FaceTarget(float deg) { if (_target != null) FaceTowards(_target.transform.position, deg); }
        void ToFightIdle() { Freeze(false); Act = FightAct.Idle; Play("fight_idle", 0.12f); }

        void StartBlock() { Freeze(false); Act = FightAct.Block; Play("block", 0.06f, 1.3f); Blocks++; FightEvent?.Invoke("block"); _dodgePressedAt = -9f; }

        void StartDodge()
        {
            Freeze(false);
            Act = FightAct.Dodge; Play("dodge", 0.05f, 1.6f); Dodges++; FightEvent?.Invoke("dodge");
            _dodgeStart = Time.time; _dodgePressedAt = -9f;
            // away from the attacker, or along the stick (sidestep) - short and fast
            var mv = MoveInput(out _);
            var away = _target != null ? Flat(transform.position - _target.transform.position).normalized : -transform.forward;
            var dir = mv.sqrMagnitude > 0.1f && Vector3.Dot(mv.normalized, -away) < 0.5f ? mv.normalized : away;
            Slide(dir.sqrMagnitude > 0.1f ? dir : -transform.forward, DodgeDistance, DodgeTime);
        }

        void StartBlow(string state)
        {
            _blowPressed = _atkPressedAt; _bufAtk = null;
            Freeze(false);
            Act = FightAct.Attack; _hitDone = false;
            // soft lock: turn toward the target at once (up to 70 deg), and close the gap a little so the blow lands
            if (_target != null)
            {
                var to = Flat(_target.transform.position - transform.position);
                float ang = Vector3.SignedAngle(Flat(transform.forward), to, Vector3.up);
                transform.rotation = Quaternion.AngleAxis(Mathf.Clamp(ang, -AssistCone, AssistCone), Vector3.up) * transform.rotation;
                float d = to.magnitude; if (d > 1.15f && d < AssistRange) Slide(to.normalized, Mathf.Min(d - 1.05f, 1.2f), 0.12f);
            }
            float hit = state == "finisher" ? FinisherHit : ChainHit[Mathf.Clamp(_combo, 0, ChainMax)];
            Play(state, 0.05f, AttackSpeed, true, StartTn(state, hit));
            _blowStart = Time.time;
            FightEvent?.Invoke(state);
        }

        void LandBlow(bool takedown)
        {
            LastBlow = takedown ? "finisher" : Chain[Mathf.Clamp(_combo, 0, ChainMax)];
            FightTimings.Add($"{LastBlow} button->active {Time.time - _blowPressed:0.000}s");
            if (_target == null) return;
            var to = Flat(_target.transform.position - transform.position);
            if (to.magnitude > (Chain == BossChain && _combo >= 3 ? 2.2f : 1.9f)) return;      // whiffed - out of reach (kicks reach a little further)
            if (_target.TakeBlow(to.normalized, takedown))
            {
                HitsLanded++; FightEvent?.Invoke(takedown ? "takedown_landed" : "hit");
                StartCoroutine(HitPause(takedown ? HitPauseFrames + 1f : HitPauseFrames));
                CameraKick.Nudge(to.normalized * (takedown ? 0.06f : 0.035f));
            }
        }

        System.Collections.IEnumerator HitPause(float frames)
        {
            float prev = Time.timeScale; if (prev < 0.2f) yield break;
            Time.timeScale = prev * 0.06f;
            yield return new WaitForSecondsRealtime(frames / 60f);
            if (!PauseMenu.IsOpen) Time.timeScale = prev;
        }

        /// <summary>A Scree's punch reaches her (already checked: in range, she is not defending).</summary>
        public void TakeFightHit(Vector3 dir)
        {
            if (Act == FightAct.Down) return;
            HitsTaken++;
            Freeze(false);
            FightEvent?.Invoke("hit_taken");
            CameraKick.Nudge(dir * 0.05f);
            if (HitsTaken >= MaxFightHits)
            {
                Act = FightAct.Down;
                Play(HasState("crouch_idle") ? "crouch_idle" : "idle", 0.15f);   // down on one knee, winded (landing_hard flipped her in the air)
                Slide(dir, 0.6f, 0.4f);
                FightEvent?.Invoke("down");
                return;
            }
            Act = FightAct.Flinch; _flinchUntil = Time.time + 0.32f;
            Play("fight_idle", 0.05f);
            Slide(dir, 0.4f, 0.2f);
        }
    }

    /// <summary>QF10: a small camera nudge on contact (decays in ~0.12 s). Added to whatever camera renders after it has
    /// placed itself (runs late), and taken off again before the camera places itself the next frame.</summary>
    [DefaultExecutionOrder(1000)]
    public class CameraKick : MonoBehaviour
    {
        static Vector3 _kick; static float _t;
        Vector3 _applied;
        public static void Nudge(Vector3 v)
        {
            _kick = v; _t = 0f;
            var cam = Camera.main; if (cam != null && cam.GetComponent<CameraKick>() == null) cam.gameObject.AddComponent<CameraKick>();
        }
        void Update() { transform.position -= _applied; _applied = Vector3.zero; }
        void LateUpdate()
        {
            if (_kick == Vector3.zero) return;
            _t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(1f - _t / 0.12f);
            _applied = _kick * k * Mathf.Sin(Mathf.Min(1f, _t / 0.03f) * Mathf.PI * 0.5f);
            transform.position += _applied;
            if (k <= 0f) _kick = Vector3.zero;
        }
    }
}
