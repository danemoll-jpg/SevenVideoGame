using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF9: 7's side of the fighting tutorial on the terrace (the two Scree before the city reveal). Reuses the fight demo's
    /// moves on her own controller - jab → cross → hook as a combo on Attack, hold Block, Dodge (a quick step back),
    /// and the finisher clip as a nonlethal takedown on a dazed Scree. She walks (camera-relative) facing her target.
    /// Three unblocked blows put her down (<see cref="FightDown"/>); the director then resets the fight. No combat
    /// anywhere else in the chapter: <see cref="BeginFight"/> / <see cref="EndFight"/> switch it on and off.
    /// </summary>
    public partial class ChapterSeven
    {
        public enum FightAct { Idle, Attack, Block, Dodge, Flinch, Down }
        public FightAct Act { get; private set; }
        public System.Func<ScreeGuard> FightTarget;
        public bool InFight => Current == Mode.Fight;
        public bool FightDown => Act == FightAct.Down;
        public int HitsTaken { get; private set; }
        public int HitsLanded { get; private set; }
        public int Blocks { get; private set; }
        public int Dodges { get; private set; }
        /// <summary>Script (check run) presses: set to true for one frame.</summary>
        [System.NonSerialized] public bool AutoAttack, AutoBlockHeld, AutoDodge, AutoTakedown;
        public event System.Action<string> FightEvent;

        static readonly string[] Combo = { "jab", "cross", "hook" };
        static readonly float[] ComboHit = { 0.32f, 0.36f, 0.42f };
        int _combo; bool _hitDone, _queued; float _flinchUntil;
        ScreeGuard _target;

        public void BeginFight()
        {
            StopTraversal(); Release();
            Current = Mode.Fight; Act = FightAct.Idle; HitsTaken = 0; _combo = 0;
            Crouched = false; Ground.BlockLow = true;
            Play("fight_idle", 0.25f);
        }
        public void EndFight()
        {
            if (Current != Mode.Fight) return;
            Freeze(false);
            Current = Mode.Free; Act = FightAct.Idle;
            Play("idle", 0.4f);
        }
        /// <summary>Back on her feet at the start of the fight (after being knocked down).</summary>
        public void ResetFight() { Freeze(false); Act = FightAct.Idle; HitsTaken = 0; _combo = 0; Play("fight_idle", 0.2f); }

        bool AttackPressed => AutoAttack || !ScriptDriven && Input != null && Input.Attack.WasPressedThisFrame();
        bool BlockHeld => AutoBlockHeld || !ScriptDriven && Input != null && Input.Block.IsPressed();
        bool DodgePressed => AutoDodge || !ScriptDriven && Input != null && Input.Dodge.WasPressedThisFrame();
        bool TakedownPressed => AutoTakedown || !ScriptDriven && Input != null && Input.Takedown.WasPressedThisFrame();

        public bool IsDefending => Act == FightAct.Block || Act == FightAct.Dodge && Tn > 0.04f && Tn < 0.75f;

        void UpdateFight()
        {
            Speed = 0f; Context = null;
            _target = FightTarget != null ? FightTarget() : null;
            bool atk = AttackPressed, dodge = DodgePressed, takedown = TakedownPressed, block = BlockHeld;
            AutoAttack = AutoDodge = AutoTakedown = false;
            // a dazed Scree in reach: the quiet context prompt (the teaching prompt shows the first time only)
            if (_target != null && _target.Dazed && Dist(_target) < 2.4f) Context = "takedown:Takedown";
            switch (Act)
            {
                case FightAct.Down:
                    return;                                              // the director resets the fight
                case FightAct.Flinch:
                    if (Time.time >= _flinchUntil) ToFightIdle();
                    return;
                case FightAct.Attack:
                {
                    FaceTarget(540f);
                    bool fin = State == "finisher";
                    float hitAt = fin ? 0.45f : ComboHit[Mathf.Clamp(_combo, 0, 2)];
                    if (!_hitDone && Tn >= hitAt) { _hitDone = true; LandBlow(fin); }
                    if (atk && !fin) _queued = true;
                    if (Tn >= (fin ? 0.92f : 0.62f))
                    {
                        if (_queued && !fin && _combo < 2) { _combo++; StartBlow(Combo[_combo]); }
                        else if (Tn >= (fin ? 0.97f : 0.8f)) { _combo = 0; ToFightIdle(); }
                    }
                    return;
                }
                case FightAct.Block:
                    FaceTarget(360f);
                    if (Tn >= 0.45f) Freeze(true);
                    if (!block) { Freeze(false); ToFightIdle(); }
                    return;
                case FightAct.Dodge:
                    if (Tn >= 0.85f) ToFightIdle();
                    return;
            }
            // idle / moving
            if (block) { Act = FightAct.Block; Play("block", 0.08f); Blocks++; FightEvent?.Invoke("block"); return; }
            if (dodge)
            {
                Act = FightAct.Dodge; Play("dodge", 0.06f); Dodges++; FightEvent?.Invoke("dodge");
                var away = _target != null ? Flat(transform.position - _target.transform.position).normalized : -transform.forward;
                Slide(away.sqrMagnitude > 0.1f ? away : -transform.forward, 0.9f, 0.35f);
                return;
            }
            if (takedown && _target != null && _target.Dazed && Dist(_target) < 1.9f) { _combo = 0; StartBlow("finisher"); FightEvent?.Invoke("takedown"); return; }
            if (atk) { _combo = 0; StartBlow(Combo[0]); return; }
            var v = MoveInput(out _);
            if (v.sqrMagnitude > 0.04f)
            {
                var step = v * WalkSpeed * 0.8f * Time.deltaTime;
                int n = Mathf.Max(1, Mathf.CeilToInt(step.magnitude / 0.15f));
                for (int i = 0; i < n; i++) { transform.position += step / n; ClampToArena(); }
                if (State != "walk") Play("walk", 0.15f, 1f, false);
                if (_target != null) FaceTarget(400f); else FaceTowards(transform.position + v, 540f);
            }
            else
            {
                if (State != "fight_idle") Play("fight_idle", 0.2f, 1f, false);
                FaceTarget(300f);
            }
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        float Dist(ScreeGuard s) => Flat(s.transform.position - transform.position).magnitude;
        void FaceTarget(float deg) { if (_target != null) FaceTowards(_target.transform.position, deg); }
        void ToFightIdle() { Freeze(false); Act = FightAct.Idle; Play("fight_idle", 0.15f); }

        void StartBlow(string state)
        {
            Act = FightAct.Attack; _hitDone = false; _queued = false;
            // close the gap a little so the blow can land (forgiving: it is a tutorial)
            if (_target != null) { float d = Dist(_target); if (d > 1.15f && d < 2.4f) Slide(Flat(_target.transform.position - transform.position).normalized, d - 1.05f, 0.18f); }
            Play(state, 0.08f);
            FightEvent?.Invoke(state);
        }

        void LandBlow(bool takedown)
        {
            if (_target == null) return;
            var to = Flat(_target.transform.position - transform.position);
            if (to.magnitude > 1.9f) return;                             // whiffed - out of reach
            if (_target.TakeBlow(to.normalized, takedown)) { HitsLanded++; FightEvent?.Invoke(takedown ? "takedown_landed" : "hit"); }
        }

        /// <summary>A Scree's punch reaches her (already checked: in range, she is not defending).</summary>
        public void TakeFightHit(Vector3 dir)
        {
            if (Act == FightAct.Down) return;
            HitsTaken++;
            Freeze(false);
            FightEvent?.Invoke("hit_taken");
            if (HitsTaken >= 3)
            {
                Act = FightAct.Down;
                Play(HasState("crouch_idle") ? "crouch_idle" : "idle", 0.15f);   // down on one knee, winded (landing_hard flipped her in the air)
                Slide(dir, 0.6f, 0.4f);
                FightEvent?.Invoke("down");
                return;
            }
            Act = FightAct.Flinch; _flinchUntil = Time.time + 0.45f;
            Play("fight_idle", 0.05f);
            Slide(dir, 0.45f, 0.25f);
        }
    }
}
