using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    // =====================================================================================================================
    // QF17 — real boss fights (CLAUDE.md "Boss fights"). One system for every boss: the boss MOVES and ATTACKS her with
    // several telegraphed attacks (a clear wind-up: his pose, a sound, a white light, a white marker on the ground where it
    // will land); she answers with her own moves (dodge, block, jump, telekinesis, her strikes); hits knock her back and
    // cost her; enough hits = she goes down -> a quick retry - QF20: he keeps his energy and progress. 2-3 phases escalate
    // (faster, a new attack, the arena changes). He is beaten by countering him: an attack he wastes (dodged, blocked,
    // jumped, out of reach) leaves him OPEN for a moment - a telekinetic throw of debris then (or three strikes) STAGGERS
    // him; enough staggers end the phase; in the last one he is left dazed and her takedown (Y) pins him. Nonlethal for
    // everyone. Every number is in one data table: Resources/Bosses/boss_tuning.json (Dan's feedback = a number change).
    // Proof lines in every run that meets a boss: "BOSS <id>: ..." (attacks per minute, hits she took, staggers, phase
    // times, fight length), "BOSS IDLE TEST" (-bossidle: nobody presses anything), captures boss_<id>_<attack>_*.
    // =====================================================================================================================

    [System.Serializable] public class BossTable { public BossDef[] bosses; }
    [System.Serializable]
    public class BossDef
    {
        public string id, name;
        public int sevenHits = 5;             // hits she can take before she goes down (retry from the phase start)
        public float sevenMoveScale = 1.6f;   // her movement in stance x this (LB runs x1.5)
        public float openTime = 1.8f;         // a wasted attack leaves him open this long (on top of the attack's own "open")
        public float staggerTime = 3.2f;      // staggered: reeling, her strikes land
        public int strikesForStagger = 3;     // or three strikes while he is open
        public float throwFlight = 0.35f;     // the thrown debris' flight (s)
        public float debrisRespawn = 5f;      // a thrown piece comes back (dust, a new piece) after this
        public float tkRange = 12f;           // debris within this of her can be thrown
        public float objectiveTime = 0f;      // QF19: unused - an X objective is one press (no channel any more)
        // QF18 (19c) - Dan's loop: telekinesis only STUNS, her hand-to-hand strikes do the real damage
        public float stunTime = 3.6f;         // a thrown object that hits while he is open: stunned this long (reels, dazed sway)
        public float throwChip = 0.02f;       // the stun itself costs him a sliver of energy
        public float strikeDamage = 0.045f;   // a jab / cross / hook while he is stunned (fraction of his energy)
        public float kickDamage = 0.065f;     // the MMA kick / roundhouse while he is stunned
        public float bladeDamage = 0.075f;    // Round 16: an arc-blade strike (on a machine's power couplings) while he is stunned
        public float openStrikeScale = 0.3f;  // a strike while he is merely open (not stunned): this share of it
        public float dazedAt = 0.06f;         // last phase: at or under this energy he is dazed -> the finisher / takedown
        public PhaseDef[] phases;
        public AttackDef[] attacks;
        public AttackDef Attack(string id) { foreach (var a in attacks) if (a.id == id) return a; return null; }
    }
    [System.Serializable]
    public class PhaseDef
    {
        public string name;
        public int staggers = 2;              // (QF17; QF18 ends phases by energy - see endAt)
        public float endAt = -1f;             // QF18: the phase ends when his energy reaches this (-1 = its objective ends it)
        public float gapMin = 1.6f, gapMax = 2.6f;   // pause between attacks
        public float tempo = 1f;              // telegraphs / recoveries / speeds x 1/tempo (faster in later phases)
        public float moveSpeed = 2.2f;        // how fast he closes in / circles
        public float keep = 4f;               // the distance he likes to keep
        public string[] attacks;              // which attacks this phase uses
        public string[] combos;               // "a>b": b follows a at once
    }
    [System.Serializable]
    public class AttackDef
    {
        public string id, kind;               // lunge / sweep / spit / wave / ring / beam / cage
        public string answer;                 // what reads it: "dodge sideways", "jump", "block or dodge", ...
        public float telegraph = 0.9f, active = 0.3f, recover = 0.8f, open = 0.8f;
        public int damage = 1; public float knock = 1.2f;
        public float range = 7f, width = 1.2f, angle = 120f, speed = 12f;
        public int count = 1; public float puddle = 0f;
        public bool blockable = true, dodgeable = true, jumpable = false;
        public float minDist = 0f, maxDist = 99f, weight = 1f;
        public string sound = "fight_swing", windSound = "signal_pulse";
    }

    public abstract class BossFight : MonoBehaviour, IFightTarget
    {
        public static BossFight Active { get; private set; }
        public static BossTable Table;
        public static BossDef Load(string id)
        {
            if (Table == null)
            {
                var ta = Resources.Load<TextAsset>("Bosses/boss_tuning");
                Table = ta != null ? JsonUtility.FromJson<BossTable>(ta.text) : new BossTable { bosses = new BossDef[0] };
            }
            foreach (var b in Table.bosses) if (b.id == id) return b;
            Debug.LogWarning("[Seven] boss tuning: no entry " + id);
            return null;
        }

        public ChapterSeven Seven; public ChapterCamera Cam;
        public BossDef Def;
        public int Phase { get; private set; }
        public PhaseDef P => Def.phases[Mathf.Clamp(Phase, 0, Def.phases.Length - 1)];
        public enum St { Idle, Telegraph, Active, Recover, Staggered, Dazed, Pinned, Hold }
        public St State { get; private set; } = St.Hold;
        public AttackDef Cur { get; private set; }
        public float StateT => Time.time - _st0;
        public float StateLen { get; private set; }
        public bool Open => State == St.Recover && Time.time < _openUntil || State == St.Staggered;
        public bool Over { get; private set; }
        public bool Paused;                         // a cutscene beat / retry: no attacks, no hits
        public float Tempo => Mathf.Max(0.3f, P.tempo);
        // what the attack is aimed at (fixed when the telegraph locks)
        public Vector3 From { get; private set; }
        public Vector3 Aim { get; private set; }
        public Vector3 AimDir { get; private set; }
        public float BeamFrom { get; private set; }
        bool _locked;
        // stats (the proof)
        public int Attacks, HitsOnHer, Staggers, Knockdowns, Dodged, Blocked, Jumped, OutOfReach, Throws, Deflected, Strikes;
        // QF18 (19): his energy (1 = full) and where it went
        public float Energy { get; private set; } = 1f;
        public float DamageByStrikes, DamageByThrows; public int StunStrikes, OpenStrikes;
        /// <summary>QF20 (2): her melee damage split - punches (X), kicks (Y), the finisher, and (Round 16) the arc blades.</summary>
        public float DamageByPunches, DamageByKicks, DamageByBlades; public int KicksLanded, PunchesLanded, BladeHits;
        /// <summary>QF20 (3): his energy just before she went down and just after the retry (must be identical).</summary>
        public readonly List<string> RetryEnergy = new List<string>();
        public bool Stunned => State == St.Staggered;
        /// <summary>QF23 (19, 21): fight moments the director answers with lines / light ("stagger", "waiting" = his energy is at a
        /// phase mark but the phase's objective is not done, "blade_shock", "dazed").</summary>
        public event System.Action<string> Moment;
        protected void Raise(string m) { try { Moment?.Invoke(m); } catch (System.Exception e) { Debug.LogWarning("[Seven] boss moment handler: " + e.Message); } }
        /// <summary>QF23 (19): his energy has reached this phase's mark but its objective is not done yet - the fight waits on it.</summary>
        public bool WaitingOnObjective => Def != null && !Over && Phase < Def.phases.Length - 1 && P.endAt >= 0f && Energy <= P.endAt + 1e-4f && !ObjectiveDone;
        /// <summary>QF23 (21): a subclass stuns him (e.g. an arc-blade counter that shocks him out of a dive).</summary>
        protected void Stun(string how) { if (!Over && State != St.Dazed && State != St.Pinned) StaggerHim(how); }
        // QF23 (17): no controls box at the start - each fight-specific action gets a small prompt at the moment it matters
        // (bottom of the screen), the first few times only; the full list stays on the Controls page
        readonly Dictionary<string, int> _tips = new Dictionary<string, int>();
        public int TipsShown { get; private set; }
        protected void Tip(string key, string text, int times = 2, float secs = 2.6f)
        {
            if (AutoPilot && DemoBoot.CaptureDir == null) return;
            _tips.TryGetValue(key, out int n); if (n >= times) return;
            _tips[key] = n + 1; TipsShown++; UI?.Hint(text, secs);
            if (DemoBoot.CaptureDir != null) Log($"FIGHT TIP (QF23 17) '{text}' ({n + 1}/{times}) - small, bottom of the screen");
        }
        public float[] PhaseMarks { get { var m = new float[Def.phases.Length]; for (int i = 0; i < m.Length; i++) m[i] = Def.phases[i].endAt; return m; } }
        float _phaseFloor => P.endAt >= 0f ? P.endAt : Energy;
        public readonly List<string> PhaseLog = new List<string>();
        public readonly Dictionary<string, int> AttackCount = new Dictionary<string, int>();
        public float FightStart { get; private set; }
        float _phaseStart, _st0, _openUntil, _next; int _phaseStaggers, _openStrikes; bool _resolved, _hitThisAttack; string _comboNext;
        Vector3 _lungeStart;
        protected ChapterCapture Cap => ChapterCapture.I;
        protected void Log(string s) { Debug.Log("[Seven] " + s); if (DemoBoot.CaptureDir != null) Cap?.Log(s); }
        protected static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        protected Vector3 Her => Seven.transform.position;

        // ---------------------------------------------------------------- what each boss provides
        protected abstract void Anim(St st, AttackDef a, float u, bool moving, float moveSpeed);
        protected virtual Vector3 Centre => transform.position + Vector3.up * 1.2f;
        /// <summary>Can he walk now (Viper on his platform in phase 1: no).</summary>
        protected virtual bool CanMove => true;
        /// <summary>Is he within her reach (strikes / takedown) at all.</summary>
        protected virtual bool Reachable => true;
        /// <summary>The objective of this phase is done (pylons, devices) - with phase.staggers = 0 that ends the phase.</summary>
        protected virtual bool ObjectiveDone => true;
        /// <summary>Between phases: the beat (he comes down, the arena changes). Runs with the fight paused.</summary>
        protected virtual IEnumerator PhaseBeat(int ended) { yield break; }
        /// <summary>She went down: put both back at the start of the current phase (under a fade).</summary>
        protected abstract void ResetPhase();
        /// <summary>Optional: a TK / X objective near her (pylon, device). Return its aim point (null = none in reach).</summary>
        protected virtual Vector3? ObjectiveTk() => null;
        protected virtual void ObjectiveTkFire() { }
        protected virtual string ObjectiveUseLabel() => null;
        protected virtual void ObjectiveUse() { }
        // ---- QF19: an X objective is ONE press in reach - instant, no hold, nothing interrupts it (the QF17 3-s channel is gone)
        /// <summary>QF19 proof: X presses on the pad (real input, not a script flag) while an objective prompt was showing,
        /// and how many of them went to a strike instead (must be 0).</summary>
        public int PadUsePresses, PadUseToStrike;
        /// <summary>QF19 (2): the camera eases a little toward a thing she just shut down (and a small shake).</summary>
        protected void NudgeCamera(Vector3 at) { StartCoroutine(Nudge(at)); }
        IEnumerator Nudge(Vector3 at)
        {
            CameraShake.Quake(0.35f, 0.35f);
            if (Cam == null) yield break;
            var to = Flat(at - Her); if (to.sqrMagnitude < 0.01f) yield break;
            float want = Quaternion.LookRotation(to).eulerAngles.y, off = Mathf.DeltaAngle(Cam.Yaw, want), turn = Mathf.Clamp(off * 0.45f, -40f, 40f), done = 0f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f) { float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)); Cam.Yaw += turn * k - done; done = turn * k; yield return null; }
        }
        /// <summary>Things on the ground a player steps round (a surfaced creature).</summary>
        public virtual IEnumerable<Vector3> Hazards() { yield break; }
        /// <summary>The context prompt a player sees now (objective / throw / takedown).</summary>
        public virtual string PromptLabel => Paused ? null : ObjectiveUseLabel() ?? (ObjectiveTk().HasValue ? "Telekinesis" : Open && DebrisNear ? "Telekinesis: throw" : State == St.Dazed ? "Takedown" : null);
        public bool PromptIsTk => PromptLabel != null && PromptLabel.StartsWith("Telekinesis");
        /// <summary>The phase was skipped (fairness): complete its objectives.</summary>
        protected virtual void SkipObjectives() { }
        /// <summary>QF18: may this attack be used now (e.g. not once the machine that makes it is shut down)?</summary>
        protected virtual bool Allowed(AttackDef a) => true;
        /// <summary>The pilot (check runs / player-style runs): where an objective of this phase is and how it is used.</summary>
        public virtual (Vector3 at, string how)? PilotObjective() => null;
        /// <summary>Extra visuals of an attack (globs, worms, the beam) - per frame while it is active.</summary>
        protected virtual void AttackFx(AttackDef a, float u) { }
        protected virtual void AttackStarted(AttackDef a) { }
        protected virtual void Arena(float dt) { }
        /// <summary>Round 16: a strike of hers reached him (the Machinist's power couplings flare / dim on a blade hit).</summary>
        protected virtual void StrikeLanded(string blow, bool counted) { }
        /// <summary>Where he stands at the start of a phase / may walk (clamped by the subclass).</summary>
        protected virtual Vector3 ClampBoss(Vector3 p) => p;

        // ---------------------------------------------------------------- debris for the telekinetic throw
        protected class Debris { public Transform T; public Vector3 Home; public bool Gone; public float Back; }
        protected readonly List<Debris> Pieces = new List<Debris>();
        protected void AddDebris(Transform t) { if (t != null) Pieces.Add(new Debris { T = t, Home = t.position }); }
        Debris NearestDebris(float range)
        {
            Debris best = null; float bd = range;
            foreach (var d in Pieces) { if (d.Gone || d.T == null) continue; float k = Flat(d.T.position - Her).magnitude; if (k < bd) { bd = k; best = d; } }
            return best;
        }
        public bool DebrisNear => NearestDebris(Def.tkRange) != null;
        public Vector3? NearestDebrisAt { get { var d = NearestDebris(60f); return d != null ? d.T.position : (Vector3?)null; } }

        // ---------------------------------------------------------------- start / stop
        public void Begin(BossDef def, ChapterSeven seven, ChapterCamera cam)
        {
            Def = def; Seven = seven; Cam = cam; Active = this;
            Phase = 0; _phaseStaggers = 0; Over = false; Paused = false; Energy = 1f; _phaseStartEnergy = 1f; _endPending = false; DamageByStrikes = DamageByThrows = 0f; StunStrikes = OpenStrikes = 0;
            DamageByPunches = DamageByKicks = DamageByBlades = 0f; KicksLanded = PunchesLanded = BladeHits = 0; RetryEnergy.Clear();
            FightStart = _phaseStart = Time.time; _next = Time.time + 2.0f;
            SetState(St.Idle, 0f);
            Seven.BeginFight();
            ArmSeven();
            Markers.Init();
            Log($"BOSS {Def.id}: the fight begins - {Def.phases.Length} phases, she can take {Def.sevenHits} hits, table Resources/Bosses/boss_tuning.json");
            PhaseLog.Add($"phase 1 '{P.name}' from 0:00");
        }
        void ArmSeven()
        {
            Seven.MaxFightHits = Def.sevenHits; Seven.FightMoveScale = Def.sevenMoveScale; Seven.FightHop = true;
            Seven.FightTarget = () => this; Seven.FightTargets = () => new IFightTarget[] { this };
            Seven.TkAim = TkAim; Seven.TkFire = TkFire;
            Seven.FightUseLabel = () => Paused ? null : ObjectiveUseLabel(); Seven.FightUse = ObjectiveUse;
            Seven.FightEvent -= OnSeven; Seven.FightEvent += OnSeven;
        }
        int _herDodges, _herHops, _herBlocks, _herTk;
        public int LockToggles;
        void OnSeven(string e) { if (e == "lock_on" || e == "lock_off") { LockToggles++; Log($"boss {Def.id}: LOCK-ON (QF20) toggled {(e == "lock_on" ? "ON" : "OFF")} by a real R3 / Tab press"); } if (e == "dodge") _herDodgeAt = Time.time; if (e == "hop") _herHopAt = Time.time; if (e == "dodge") _herDodges++; else if (e == "hop") _herHops++; else if (e == "block") _herBlocks++; else if (e == "tk") _herTk++; }
        bool _waitRaised;
        public void End()
        {
            if (UI != null) UI.PulseMark = -1;
            UI?.HideBossBar();
            Seven.FightEvent -= OnSeven;
            Markers.Clear();
            if (Seven.InFight) Seven.EndFight();
            if (Active == this) Active = null;
        }
        void OnDestroy() { if (Active == this) Active = null; Markers.Clear(); }

        public string Summary()
        {
            float len = Time.time - FightStart, min = Mathf.Max(0.01f, len / 60f);
            var per = new List<string>(); foreach (var kv in AttackCount) per.Add($"{kv.Key} {kv.Value}");
            return $"BOSS {Def.id}: fight length {Mathf.FloorToInt(len / 60f)}:{len % 60f:00} ({len:0} s), attacks {Attacks} ({Attacks / min:0.0}/min: {string.Join(", ", per)}), her presses: dodge {_herDodges}, jump {_herHops}, block {_herBlocks}, telekinesis {_herTk}, hits she took {HitsOnHer}, she went down {Knockdowns}x, " +
                   $"answered: dodged {Dodged}, blocked {Blocked}, jumped {Jumped}, out of reach {OutOfReach}; telekinetic throws {Throws} (deflected {Deflected}), stuns {Staggers}, strikes landed {Strikes} ({StunStrikes} while stunned, {OpenStrikes} while open); " +
                   $"DAMAGE (QF18): dealt by her strikes {DamageByStrikes * 100f:0}%, by throws {DamageByThrows * 100f:0}% of a full bar - melee {(DamageByStrikes + DamageByThrows > 0f ? DamageByStrikes / (DamageByStrikes + DamageByThrows) * 100f : 0f):0}% of the damage; energy left {Energy * 100f:0}%; " +
                   $"MELEE (QF20): punches {PunchesLanded} landed = {DamageByPunches * 100f:0}%, kicks {KicksLanded} landed = {DamageByKicks * 100f:0}%{(BladeHits > 0 ? $", arc blades {BladeHits} = {DamageByBlades * 100f:0}%" : "")}; " +
                   $"RETRY (QF20): {(RetryEnergy.Count == 0 ? "she never went down" : string.Join(", ", RetryEnergy))}; phases: {string.Join(" | ", PhaseLog)}; " +
                   $"lock-on toggles (R3 / Tab) {LockToggles}; X OBJECTIVES (QF19): the pilot pressed X {BossPilot.UsePresses}x at objectives, second presses {BossPilot.UseRepeats} (must be 0); X presses on the pad while an objective prompt showed {PadUsePresses}, went to a strike {PadUseToStrike} (must be 0)";
        }

        // ---------------------------------------------------------------- the brain
        protected void SetState(St s, float len) { State = s; StateLen = len; _st0 = Time.time; }

        protected virtual void Update()
        {
            if (Def == null || Over) return;
            float dt = Time.deltaTime;
            Arena(dt);
            UpdateDebris();
            if (Paused || PauseMenu.IsOpen) { Anim(State == St.Dazed || State == St.Pinned ? State : St.Idle, null, 0f, false, 0f); return; }
            if (Seven.FightDown) return;                    // the director runs the retry
            float u = StateLen > 0f ? Mathf.Clamp01(StateT / StateLen) : 1f;
            switch (State)
            {
                case St.Hold: Anim(St.Idle, null, 0f, false, 0f); break;
                case St.Idle:
                {
                    // move: close in / keep his distance / circle her
                    bool moving = false; float spd = 0f;
                    if (CanMove)
                    {
                        var to = Flat(Her - transform.position); float d = to.magnitude;
                        Vector3 want = Vector3.zero;
                        if (d > P.keep + 1f) want = to.normalized;
                        else if (d < P.keep - 1.5f) want = -to.normalized * 0.7f;
                        else want = Vector3.Cross(Vector3.up, to.normalized) * (Mathf.Sin(Time.time * 0.4f + Phase) > 0f ? 0.5f : -0.5f);
                        if (want.sqrMagnitude > 0.01f)
                        {
                            spd = P.moveSpeed * want.magnitude; moving = true;
                            transform.position = ClampBoss(transform.position + want.normalized * spd * dt);
                            var p = transform.position; p.y = Ground.Height(p.x, p.z); transform.position = p;
                        }
                        Face(Her, 300f);
                    }
                    else Face(Her, 200f);
                    Anim(St.Idle, null, 0f, moving, spd);
                    if (_endPending) { _endPending = false; StartCoroutine(NextPhase()); break; }
                    if (Time.time >= _next) { var pick = _counter ? CloseAttack() ?? PickAttack() : PickAttack(); _counter = false; StartAttack(pick); }
                    break;
                }
                case St.Telegraph:
                {
                    Markers.Tell(Centre + Vector3.up * 0.4f, u);
                    // the aim follows her until 75 % of the wind-up, then locks (the marker stops: that is the moment to move)
                    if (!_locked) { Aimed(Cur); if (u >= 0.75f) { _locked = true; } }
                    if (Cur.kind != "spit" && Cur.kind != "beam" && Cur.kind != "wave" && Cur.kind != "cage" || !_locked) Face(Cur.kind == "wave" || Cur.kind == "spit" || Cur.kind == "cage" ? Aim : From + AimDir * 3f, 400f);
                    Markers.Show(this, Cur, u, _locked);
                    Anim(St.Telegraph, Cur, u, false, 0f);
                    if (u >= 1f) { _resolved = false; _hitThisAttack = false; _lungeStart = transform.position; SetState(St.Active, Cur.active / Tempo); AttackStarted(Cur); AudioKit.Play(Cur.sound, Centre, 1f, 0.06f, AudioKit.Bus.Sfx, 60f); Burst("active"); }
                    break;
                }
                case St.Active:
                    Markers.Show(this, Cur, 1f, true);
                    Markers.Tell(Centre + Vector3.up * 0.4f, 1f - u);
                    if (Cur.kind == "beam") Markers.Beam(From, BeamFrom + Cur.angle * u, Cur.range, true);
                    if (Cur.kind == "sweep") Markers.Flash(From, AimDir, Cur.range, Cur.angle, u);
                    if (Cur.kind == "lunge") Markers.Trail(transform.position, u);
                    Anim(St.Active, Cur, u, false, 0f);
                    ActiveFrame(Cur, u);
                    AttackFx(Cur, u);
                    if (u >= 1f)
                    {
                        Markers.Hide(); Markers.Beam(From, 0f, 0f, false); Markers.Tell(Centre, 0f);
                        bool missed = !_hitThisAttack;
                        if (_comboNext != null && Def.Attack(_comboNext) is AttackDef nx) { var c = _comboNext; _comboNext = null; StartAttack(nx); break; }
                        // only his committed attacks (strike, sweep, slam) leave him open when wasted - not his ranged pokes or machinery
                        float rec = Cur.recover / Tempo, open = missed && Cur.open > 0f ? (Cur.open + Def.openTime) : 0f;
                        _openUntil = Time.time + open; _openStrikes = 0;
                        SetState(St.Recover, Mathf.Max(rec, open));
                        if (open > 0f) Log($"boss {Def.id}: '{Cur.id}' wasted - he is OPEN for {open:0.0} s");
                    }
                    break;
                case St.Recover:
                    Anim(St.Recover, Cur, u, false, 0f);
                    if (_endPending) { _endPending = false; StartCoroutine(NextPhase()); break; }
                    if (u >= 1f) { SetState(St.Idle, 0f); _next = Time.time + Random.Range(P.gapMin, P.gapMax) / Tempo; }
                    break;
                case St.Staggered:
                    Anim(St.Staggered, null, u, false, 0f);
                    if (_endPending) { _endPending = false; StartCoroutine(NextPhase()); break; }
                    if (u >= 1f) AfterStagger();
                    break;
                case St.Dazed: Anim(St.Dazed, null, u, false, 0f); break;
                case St.Pinned: Anim(St.Pinned, null, u, false, 0f); break;
            }
        }

        void Face(Vector3 p, float degPerSec)
        {
            var d = Flat(p - transform.position); if (d.sqrMagnitude < 1e-4f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), degPerSec * Time.deltaTime);
        }

        /// <summary>QF18: right after a stun, the attack that answers her being close (a sweep / strike in reach).</summary>
        AttackDef CloseAttack()
        {
            float d = Flat(Her - transform.position).magnitude; if (d > 4.2f) return null;
            foreach (var id in P.attacks) { var a = Def.Attack(id); if (a != null && (a.kind == "sweep" || a.kind == "ring") && d <= a.maxDist && d >= a.minDist) return a; }
            return null;
        }
        AttackDef PickAttack()
        {
            float d = Flat(Her - transform.position).magnitude, sum = 0f;
            var ok = new List<AttackDef>();
            foreach (var id in P.attacks) { var a = Def.Attack(id); if (a == null || !Allowed(a)) continue; if (d < a.minDist || d > a.maxDist) continue; ok.Add(a); sum += a.weight; }
            if (ok.Count == 0) foreach (var id in P.attacks) { var a = Def.Attack(id); if (a != null && Allowed(a)) { ok.Add(a); sum += a.weight; } }
            if (ok.Count == 0) { var a = Def.Attack(P.attacks[0]); ok.Add(a); sum += a.weight; }
            float r = Random.value * sum;
            foreach (var a in ok) { r -= a.weight; if (r <= 0f) return a; }
            return ok[ok.Count - 1];
        }

        protected void StartAttack(AttackDef a)
        {
            if (a == null) { _next = Time.time + 1f; return; }
            Cur = a; _locked = false; Attacks++;
            AttackCount[a.id] = AttackCount.TryGetValue(a.id, out var n) ? n + 1 : 1;
            _comboNext = null;
            if (P.combos != null) foreach (var c in P.combos) { var k = c.Split('>'); if (k.Length == 2 && k[0] == a.id) _comboNext = k[1]; }
            Aimed(a);
            SetState(St.Telegraph, a.telegraph / Tempo);
            AudioKit.Play(a.windSound, Centre, 1f, 0.04f, AudioKit.Bus.Sfx, 70f);
            if (a.dodgeable && !a.jumpable) Tip("dodge", "Dodge  (B / Ctrl)"); else if (a.jumpable) Tip("jump", "Jump  (A / Space)"); else if (a.blockable) Tip("block", "Block  (LT / Q)");
            if (DemoBoot.CaptureDir != null) Log($"boss {Def.id}: attack {Attacks} '{a.id}' ({a.kind}) - telegraph {a.telegraph / Tempo:0.00} s, she is {Flat(Her - transform.position).magnitude:0.0} m away (answer: {a.answer})");
            Burst("telegraph");
        }

        /// <summary>Where an attack comes from (default: him; e.g. a machine pylon's beam, the creature he sends).</summary>
        protected virtual Vector3 Origin(AttackDef a) => transform.position;
        void Aimed(AttackDef a)
        {
            From = Origin(a); From = new Vector3(From.x, Ground.Height(From.x, From.z), From.z);
            var to = Flat(Her - From); if (to.sqrMagnitude < 0.01f) to = transform.forward;
            AimDir = to.normalized;
            switch (a.kind)
            {
                case "lunge": Aim = From + AimDir * a.range; break;
                case "spit":
                {
                    Aim = Her + Flat(Seven.transform.forward) * 0.6f;
                    SpitTargets.Clear();
                    var side = Vector3.Cross(Vector3.up, AimDir);
                    for (int i = 0; i < Mathf.Max(1, a.count); i++)
                    {
                        // a fixed spread round her (the first glob on her): she reads it and steps out between them
                        float k = i == 0 ? 0f : (i % 2 == 1 ? 1f : -1f) * (1.7f + 0.9f * ((i - 1) / 2));
                        float fwd = i == 0 ? 0f : ((i / 2) % 2 == 0 ? 1.1f : -0.9f);
                        var q = Aim + side * k + AimDir * fwd; q.y = Ground.Height(q.x, q.z); SpitTargets.Add(q);
                    }
                    break;
                }
                case "wave": Aim = From + AimDir * a.range; break;
                case "cage": Aim = Her; break;
                case "beam": Aim = Her; BeamFrom = Mathf.Atan2(AimDir.x, AimDir.z) * Mathf.Rad2Deg - a.angle * 0.5f; break;
                default: Aim = From + AimDir * a.range; break;
            }
            Aim = new Vector3(Aim.x, Ground.Height(Aim.x, Aim.z), Aim.z);
        }

        // ---------------------------------------------------------------- where it hits (the same geometry the pilot reads)
        /// <summary>Would this attack hit a body standing at p at its (next) hit moment? Ignores her answer.</summary>
        public bool Danger(Vector3 p, AttackDef a = null, float u = -1f)
        {
            a = a ?? Cur; if (a == null) return false;
            var f = Flat(p - From); float d = f.magnitude;
            switch (a.kind)
            {
                case "lunge": { float t = Mathf.Clamp(Vector3.Dot(f, AimDir), 0f, a.range); return Flat(p - (From + AimDir * t)).magnitude < a.width + 0.3f; }
                case "sweep": return d < a.range + 0.3f && Vector3.Angle(AimDir, f) < a.angle * 0.5f + 8f;
                case "spit": return SpitTargets.Exists(q => Flat(p - q).magnitude < a.width + 0.3f);
                case "wave": { float t = Vector3.Dot(f, AimDir); return t > -0.5f && t < a.range + 0.5f && Flat(p - (From + AimDir * t)).magnitude < a.width * 0.5f + 0.3f; }
                case "ring": return d < a.range + 0.5f;
                case "beam": { var ff = Flat(p - From); if (ff.magnitude > a.range) return false; float ang = Mathf.DeltaAngle(BeamFrom, Mathf.Atan2(ff.x, ff.z) * Mathf.Rad2Deg); return ang > -6f && ang < a.angle + 6f; }
                case "cage": return false;
            }
            return false;
        }
        /// <summary>Seconds until the current attack's hit moment (negative = already active / passed).</summary>
        public float TimeToHit
        {
            get
            {
                if (Cur == null || State != St.Telegraph && State != St.Active) return 99f;
                float left = State == St.Telegraph ? StateLen - StateT : -StateT, act = Cur.active / Tempo;
                float along = Mathf.Max(0f, Vector3.Dot(Flat(Her - From), AimDir)), d = Flat(Her - From).magnitude;
                switch (Cur.kind)
                {
                    case "lunge": return left + act * Mathf.Clamp01(along / Mathf.Max(1f, Cur.range));
                    case "wave": return left + act * Mathf.Clamp01(along / Mathf.Max(1f, Cur.range));
                    case "ring": return left + act * Mathf.Clamp01(d / Mathf.Max(1f, Cur.range));
                    case "spit": return left + act * 0.95f;
                    case "sweep": return left + act * 0.5f;
                    case "beam": { var ff = Flat(Her - From); float ang = Mathf.DeltaAngle(BeamFrom, Mathf.Atan2(ff.x, ff.z) * Mathf.Rad2Deg); return left + act * Mathf.Clamp01(ang / Mathf.Max(1f, Cur.angle)); }
                }
                return left;
            }
        }

        public readonly List<Vector3> SpitTargets = new List<Vector3>();
        readonly List<(Vector3 p, float until, float r)> _puddles = new List<(Vector3, float, float)>();
        public IReadOnlyList<(Vector3 p, float until, float r)> Puddles => _puddles;
        protected void AddPuddle(Vector3 p, float seconds, float r) { _puddles.Add((p, Time.time + seconds, r)); Markers.Puddle(p, r, seconds); }

        void ActiveFrame(AttackDef a, float u)
        {
            switch (a.kind)
            {
                case "lunge":
                {
                    // he travels the marked line fast; anything in his path is hit (once)
                    var p = Vector3.Lerp(_lungeStart, From + AimDir * a.range, Mathf.SmoothStep(0f, 1f, u));
                    p = ClampBoss(p); p.y = Ground.Height(p.x, p.z); transform.position = p;
                    if (!_resolved && Flat(Her - transform.position).magnitude < a.width) Resolve(a, AimDir);
                    if (u >= 1f && !_resolved) { _resolved = true; Missed(a); }
                    break;
                }
                case "sweep":
                    if (!_resolved && u >= 0.5f) { _resolved = true; if (Danger(Her, a)) Resolve(a, Flat(Her - From).normalized); else Missed(a); }
                    break;
                case "spit":
                    if (!_resolved && u >= 0.95f)
                    {
                        _resolved = true; bool any = false;
                        foreach (var q in SpitTargets) { if (a.puddle > 0f) AddPuddle(q, a.puddle / Tempo, a.width); if (!any && Flat(Her - q).magnitude < a.width) { any = true; Resolve(a, Flat(Her - q).normalized); } }
                        AudioKit.Play("water_slosh", Aim, 0.9f, 0.1f, AudioKit.Bus.Sfx, 40f);
                        if (!any) Missed(a);
                    }
                    break;
                case "wave":
                {
                    float front = u * a.range; var fp = From + AimDir * front;
                    var f = Flat(Her - From); float along = Vector3.Dot(f, AimDir);
                    if (!_resolved && Mathf.Abs(along - front) < 0.8f && Flat(Her - (From + AimDir * along)).magnitude < a.width * 0.5f) Resolve(a, AimDir);
                    if (u >= 1f && !_resolved) { _resolved = true; Missed(a); }
                    break;
                }
                case "ring":
                {
                    float r = u * a.range; float d = Flat(Her - From).magnitude;
                    if (!_resolved && Mathf.Abs(d - r) < 0.7f) Resolve(a, Flat(Her - From).normalized);
                    if (u >= 1f && !_resolved) { _resolved = true; Missed(a); }
                    break;
                }
                case "beam":
                {
                    float ang = BeamFrom + a.angle * u; var ff = Flat(Her - From);
                    float her = Mathf.Atan2(ff.x, ff.z) * Mathf.Rad2Deg;
                    if (!_resolved && ff.magnitude < a.range && Mathf.Abs(Mathf.DeltaAngle(ang, her)) < 7f) Resolve(a, Quaternion.Euler(0f, ang + 90f, 0f) * Vector3.forward);
                    if (u >= 1f && !_resolved) { _resolved = true; Missed(a); }
                    break;
                }
                case "cage":
                    if (!_resolved) { _resolved = true; Missed(a, false); CageAt = Aim; CageUntil = Time.time + Mathf.Max(1f, a.puddle / Tempo); AudioKit.Play("mars_barrier_on", Aim, 0.9f); }
                    break;
            }
        }
        public Vector3 CageAt; public float CageUntil;
        public bool Caged => Time.time < CageUntil;

        void Resolve(AttackDef a, Vector3 dir)
        {
            _resolved = true;
            if (Seven.Invulnerable) { OutOfReach++; return; }
            if (a.jumpable && Seven.IsAirborne) { Jumped++; Log($"boss {Def.id}: '{a.id}' - she JUMPED it"); return; }
            if (a.dodgeable && Seven.IsDodging) { Dodged++; Log($"boss {Def.id}: '{a.id}' - she DODGED it"); return; }
            var toBoss = Flat(From - Her).normalized;
            if (a.blockable && Seven.IsBlocking && Vector3.Dot(Flat(Seven.transform.forward), toBoss) > 0.2f)
            {
                Blocked++; AudioKit.Play("fight_block", Her + Vector3.up, 1f, 0.05f);
                Seven.Slide(Flat(dir).normalized, 0.5f, 0.2f);
                Log($"boss {Def.id}: '{a.id}' - she BLOCKED it (pushed back, no damage)");
                return;
            }
            if (Seven.TakeBossHit(Flat(dir).normalized, a.damage, a.knock))
            {
                _hitThisAttack = true; HitsOnHer++;
                AudioKit.Play("fight_hit", Her + Vector3.up, 1f, 0.05f);
                Log($"boss {Def.id}: '{a.id}' HIT her ({Seven.HitsTaken}/{Def.sevenHits}){(Seven.FightDown ? " - she is DOWN" : "")}");
            }
        }
        float _herDodgeAt = -9f, _herHopAt = -9f;
        void Missed(AttackDef a, bool log = true)
        {
            if (!log) return;
            // a dodge / jump that took her out of its reach just before it landed counts as that answer
            if (Time.time - _herDodgeAt < 0.9f) { Dodged++; Log($"boss {Def.id}: '{a.id}' - she DODGED out of its path"); }
            else if (Time.time - _herHopAt < 0.9f && a.jumpable) { Jumped++; }
            else OutOfReach++;
        }

        // puddles (zone denial) and the cage
        protected virtual void LateUpdate()
        {
            if (Def == null || Over) return;
            for (int i = _puddles.Count - 1; i >= 0; i--)
            {
                var pd = _puddles[i];
                if (Time.time > pd.until) { _puddles.RemoveAt(i); continue; }
                if (!Paused && !Seven.FightDown && !Seven.IsAirborne && Flat(Her - pd.p).magnitude < pd.r * 0.85f && !Seven.Invulnerable)
                    if (Seven.TakeBossHit(Flat(Her - pd.p).normalized, 1, 0.9f)) { HitsOnHer++; Log($"boss {Def.id}: a burning puddle caught her ({Seven.HitsTaken}/{Def.sevenHits})"); }
            }
            if (Caged && !Seven.FightDown)
            {
                var f = Flat(Her - CageAt);
                if (f.magnitude > 2.3f) { var p = CageAt + f.normalized * 2.3f; p.y = Her.y; Seven.transform.position = p; }
            }
            Markers.Cage(Caged ? CageAt : (Vector3?)null);
            FightCamera();
            if (UI != null && !Paused) { UI.BossBar(Def.name, Energy, PhaseMarks, Stunned); UI.HerBar(Mathf.Max(0, Def.sevenHits - Seven.HitsTaken), Def.sevenHits); }
            // her forehead energy point dims with each hit she takes (Kyle's idea)
            if (Seven != null && !Paused && Seven.Act != ChapterSeven.FightAct.Throw) Seven.SetEnergyPoint(Mathf.Lerp(0.25f, 1.1f, Mathf.Clamp01(1f - Seven.HitsTaken / (float)Mathf.Max(1, Def.sevenHits))), 0.2f);
        }

        // ---------------------------------------------------------------- her side: strikes, throws, takedown
        public bool IsOut => !Reachable || State == St.Pinned || Over;
        public bool Dazed => State == St.Dazed;
        public bool TakeBlow(Vector3 dir, bool takedown)
        {
            if (IsOut || Paused) return false;
            if (takedown)
            {
                if (State != St.Dazed) return false;
                SetState(St.Pinned, 2f); Over = true;
                PhaseLog.Add($"pinned at {Clock(Time.time - FightStart)}");
                Log($"boss {Def.id}: her takedown pins him - subdued, alive");
                return true;
            }
            // QF18 (19c): her strikes do the real damage - in full while he is stunned, a little while he is merely open
            string blow = Seven != null ? Seven.LastBlow : "jab";
            float dmg = blow == "mma_kick" || blow == "roundhouse" ? Def.kickDamage : blow == "finisher" ? Def.kickDamage * 1.4f : ChapterSeven.IsBlade(blow) ? Def.bladeDamage : Def.strikeDamage;
            StrikeLanded(blow, State == St.Staggered || State == St.Dazed || Open);
            if (State == St.Staggered || State == St.Dazed)
            {
                Strikes++; StunStrikes++; AudioKit.Play("fight_hit", Centre, 0.9f, 0.06f);
                if (State == St.Staggered) Tally(blow, Damage(dmg, true, blow));
                return true;
            }
            if (Open)
            {
                Strikes++; OpenStrikes++; _openStrikes++; AudioKit.Play("fight_hit", Centre, 0.8f, 0.06f);
                Tally(blow, Damage(dmg * Def.openStrikeScale, true, blow + " (open)"));
                return true;
            }
            // not open: he turns the blow aside
            AudioKit.Play("fight_block", Centre, 0.7f, 0.06f);
            return false;
        }

        /// <summary>QF20 (2): who did the damage - punches, kicks (and the first kick that lands is captured), blades.</summary>
        protected void Tally(string blow, float took)
        {
            if (ChapterSeven.IsKick(blow)) { KicksLanded++; DamageByKicks += took; if (DemoBoot.CaptureDir != null && Cap != null && !_shot.Contains("kick_hit_" + blow)) { _shot.Add("kick_hit_" + blow); StartCoroutine(Shoot($"boss_{Def.id}_kick_hit_{blow}", 0f)); } }
            else if (blow != null && blow.StartsWith("blade")) { BladeHits++; DamageByBlades += took; }
            else if (blow != "finisher") { PunchesLanded++; DamageByPunches += took; }
        }

        Vector3? TkAim()
        {
            if (Paused) return null;
            var o = ObjectiveTk(); if (o.HasValue) return o;
            if (IsOut && State != St.Dazed) return null;
            return NearestDebris(Def.tkRange) != null ? Centre : (Vector3?)null;
        }
        void TkFire()
        {
            if (ObjectiveTk().HasValue) { ObjectiveTkFire(); return; }
            var d = NearestDebris(Def.tkRange); if (d == null) return;
            StartCoroutine(Throw(d));
        }
        IEnumerator Throw(Debris d)
        {
            d.Gone = true; Throws++;
            AudioKit.Play("tk_push", d.T.position, 1f, 0.05f);
            var a = d.T.position; var lift = a + Vector3.up * 1.6f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.18f) { d.T.position = Vector3.Lerp(a, lift, Mathf.SmoothStep(0f, 1f, t)); d.T.Rotate(220f * Time.deltaTime, 160f * Time.deltaTime, 0f); yield return null; }
            for (float t = 0f; t < 1f; t += Time.deltaTime / Def.throwFlight) { d.T.position = Vector3.Lerp(lift, Centre, t); d.T.Rotate(500f * Time.deltaTime, 300f * Time.deltaTime, 0f); yield return null; }
            bool open = Open;
            if (open && Reachable)
            {
                AudioKit.Play("stone_fall", Centre, 1f, 0.05f);
                StaggerHim("a telekinetic throw while he was open - STUNNED: close in and fight");
                Damage(Def.throwChip, false, "throw");
            }
            else
            {
                Deflected++;
                AudioKit.Play("fight_block", Centre, 1f, 0.05f);
                Log($"boss {Def.id}: he swats the thrown debris aside (not open - wait until he wastes an attack)");
            }
            // the piece falls away, then a new one is there again later
            var drop = Centre + Flat(Her - Centre).normalized * -1.5f + Vector3.up * 0.5f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f) { d.T.position = Vector3.Lerp(Centre, drop, t) + Vector3.down * t * t * 1.5f; yield return null; }
            d.T.gameObject.SetActive(false);
            d.Back = Time.time + Def.debrisRespawn;
        }
        void UpdateDebris()
        {
            foreach (var d in Pieces)
            {
                if (!d.Gone || d.T == null || d.Back <= 0f || Time.time < d.Back) continue;
                d.T.position = d.Home; d.T.rotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), 0f);
                d.T.gameObject.SetActive(true); d.Gone = false; d.Back = 0f;
            }
            // throwable pieces glow faintly while he is open (what a player looks for)
            foreach (var d in Pieces) if (d.T != null && !d.Gone) Markers.DebrisGlow(d.T, Open && !Paused && Flat(d.T.position - Her).magnitude < Def.tkRange);
        }

        void StaggerHim(string how)
        {
            Staggers++; _phaseStaggers++;
            Markers.Hide(); Markers.Beam(From, 0f, 0f, false); Markers.Tell(Centre, 0f); _comboNext = null;
            SetState(St.Staggered, Def.stunTime > 0f ? Def.stunTime : Def.staggerTime);
            CameraKick.Nudge(Flat(Centre - Her).normalized * 0.05f);
            AudioKit.Play("scree_grunt", Centre, 1f, 0.05f, AudioKit.Bus.Sfx, 40f); AudioKit.Play("signal_pulse", Centre, 0.7f, 0.05f, AudioKit.Bus.Sfx, 40f);
            UI?.BossStunned();
            Tip("strike", Seven != null && Seven.BladesOwned && Seven.BladesDrawn ? "Close in:  Strike (X) · Kick (Y)" : "Close in:  Punch (X) · Kick (Y)", 3);
            Raise("stagger");
            Log($"boss {Def.id}: STAGGERED by {how} ({_phaseStaggers}/{P.staggers} in phase {Phase + 1})");
            Burst("stagger");
        }
        void AfterStagger()
        {
            if (P.endAt < 0f && P.staggers > 0 && _phaseStaggers >= P.staggers && ObjectiveDone) { StartCoroutine(NextPhase()); return; }
            // QF18 (19c): he recovers and fights back at once - a close attack if she is in his face
            SetState(St.Idle, 0f); _next = Time.time + 0.35f / Tempo; _counter = true;
            Log($"boss {Def.id}: he shakes the stun off and fights back (energy {Energy * 100f:0}%)");
        }
        bool _counter;
        /// <summary>QF18 (19c): energy goes down; at the phase's mark the phase ends (overkill never skips a phase); in the
        /// last phase at dazedAt he is dazed - the finisher / takedown.</summary>
        protected float Damage(float d, bool melee, string how)
        {
            if (d <= 0f || Over) return 0f;
            float floor = P.endAt >= 0f ? P.endAt : Energy;
            bool last = Phase >= Def.phases.Length - 1;
            if (last) floor = 0f;
            float before = Energy;
            Energy = Mathf.Max(floor, Energy - d);
            float took = before - Energy;
            if (melee) DamageByStrikes += took; else DamageByThrows += took;
            if (DemoBoot.CaptureDir != null) Log($"boss {Def.id}: {how} -{took * 100f:0.0}% -> energy {Energy * 100f:0}%");
            if (!last && P.endAt >= 0f && Energy <= P.endAt + 1e-4f && ObjectiveDone) { _endPending = true; }
            if (last && Energy <= Def.dazedAt && State != St.Dazed) { _endPending = true; }
            return took;
        }
        bool _endPending;
        /// <summary>The subclass calls this when an objective completes (phase.staggers = 0: the objective ends the phase).</summary>
        protected void ObjectiveProgress()
        {
            bool energyOk = P.endAt < 0f ? P.staggers == 0 : Energy <= P.endAt + 1e-4f;
            if (energyOk && ObjectiveDone && State != St.Hold) StartCoroutine(NextPhase());
        }
        float _phaseStartEnergy = 1f;
        bool _phaseBusy;
        IEnumerator NextPhase()
        {
            if (_phaseBusy) yield break; _phaseBusy = true;
            int ended = Phase;
            PhaseLog[PhaseLog.Count - 1] += $" to {Clock(Time.time - FightStart)} ({Time.time - _phaseStart:0} s)";
            Markers.Hide(); _puddles.Clear(); Markers.ClearPuddles(); CageUntil = 0f;
            if (ended >= Def.phases.Length - 1)
            {
                SetState(St.Dazed, 0f);
                Log($"boss {Def.id}: phase {ended + 1} ends - he is DAZED: her takedown (Y) pins him");
                PhaseLog.Add($"dazed from {Clock(Time.time - FightStart)}");
                _phaseBusy = false; yield break;
            }
            Paused = true; SetState(St.Hold, 0f);
            yield return PhaseBeat(ended);
            Phase = ended + 1; _phaseStaggers = 0; _phaseStart = Time.time; _phaseStartEnergy = Energy; _endPending = false;
            PhaseLog.Add($"phase {Phase + 1} '{P.name}' from {Clock(Time.time - FightStart)}");
            Log($"boss {Def.id}: PHASE {Phase + 1} '{P.name}' (tempo x{P.tempo:0.00}, attacks {string.Join(", ", P.attacks)})");
            Paused = false; SetState(St.Idle, 0f); _next = Time.time + 1.5f;
            _phaseBusy = false;
        }
        static string Clock(float s) => $"{Mathf.FloorToInt(s / 60f)}:{s % 60f:00}";

        /// <summary>She went down: the director fades; she is back on her feet with her own health full. QF20 (3, Dan): he
        /// KEEPS the damage she did - his energy stays exactly where it was, the objectives already done stay done, his
        /// stuns in this phase still count, the fight goes on in the phase he is in; he backs off to his side and gives her a
        /// moment before he attacks again.</summary>
        public void Retry()
        {
            Knockdowns++;
            float before = Energy;
            Markers.Hide(); _puddles.Clear(); Markers.ClearPuddles(); CageUntil = 0f; _comboNext = null;
            ResetPhase();                                   // positions only: he backs off to his side of the arena (Place puts her in free mode)
            Seven.BeginFight(); Seven.ResetFight(); ArmSeven();
            SetState(St.Idle, 0f); _next = Time.time + 2.6f;
            RetryEnergy.Add($"down {Knockdowns}: his energy {before * 100f:0.0}% before -> {Energy * 100f:0.0}% after ({(Mathf.Approximately(before, Energy) ? "identical" : "CHANGED")}), phase {Phase + 1}");
            Log($"boss {Def.id}: she went down ({Knockdowns}x) - she gets up, health full; he keeps his damage: energy {before * 100f:0.0}% -> {Energy * 100f:0.0}% (phase {Phase + 1}, stuns this phase {_phaseStaggers}, objectives kept)");
        }

        // ---------------------------------------------------------------- the director's loop
        public bool AutoPilot;                      // check runs: the pilot drives her script flags
        [System.NonSerialized] public bool AutoSkip;  // the pilot takes the fairness skip
        public ChapterUI UI;
        public System.Func<int, string> PhaseObjective;
        public int DownsThisPhase { get; private set; }
        public bool SkipTaken { get; private set; }
        /// <summary>Runs the fight to the end (he is pinned): the knockdown retry under a fade, the idle test, the objective
        /// per phase, and - fairness - after three knockdowns in one phase a skip of that phase (Y / Enter).</summary>
        public IEnumerator Run(System.Action<string> objective)
        {
            int shown = -1; bool dazedShown = false, offered = false; int phaseOfDowns = 0;
            if (Seven != null && Seven.BladesOwned && !Seven.BladesDrawn) Tip("blades", "Arc blades  (RB / G)", 1, 3.5f);   // QF23 (21): once, small
            while (!Over)
            {
                if (AutoPilot && !Paused) { var it = BossPilot.Think(this, Seven); BossPilot.ApplyAuto(it, Seven); AutoSkip = it.Skip; }
                if (Phase != shown && !Paused) { shown = Phase; objective?.Invoke(PhaseObjective?.Invoke(Phase)); }
                if (State == St.Dazed && !dazedShown) { dazedShown = true; objective?.Invoke("He is dazed - take him down (Y / E)"); Tip("takedown", "Takedown  (Y / E)", 1, 4f); Raise("dazed"); }
                // QF23 (19): the bar never stalls silently - the mark it is waiting at pulses and the director is told
                bool waiting = WaitingOnObjective && !Paused;
                if (UI != null) UI.PulseMark = waiting ? Phase : -1;
                if (waiting && !_waitRaised) { _waitRaised = true; Raise("waiting"); Log($"boss {Def.id}: his energy is at phase {Phase + 1}'s mark - the fight waits on the objective (the mark pulses)"); }
                if (!waiting) _waitRaised = false;
                if (Phase != phaseOfDowns) { phaseOfDowns = Phase; DownsThisPhase = 0; offered = false; }
                if (Seven.FightDown && !Paused)
                {
                    if (DemoBoot.BossIdle)
                    {
                        float t = Time.time - FightStart;
                        Log($"BOSS IDLE TEST ({Def.id}): nobody pressed anything - she was knocked down after {t:0.0} s ({Attacks} attacks, {HitsOnHer} hits) - {(t <= 65f ? "PASS (within ~60 s)" : "FAIL (over 60 s)")}");
                        yield return Cap != null ? Cap.Shot($"boss_{Def.id}_idle_test_down") : null;
                        Cap?.Finish($"BOSS IDLE TEST ({Def.id}): down after {t:0.0} s, attacks {Attacks}, hits {HitsOnHer}");
                        yield break;
                    }
                    Paused = true; DownsThisPhase++;
                    Log($"boss {Def.id}: she is down ({Seven.HitsTaken} hits) - she gets back up; he keeps his damage (QF20)");
                    if (DemoBoot.CaptureDir != null && Knockdowns == 0 && Cap != null) yield return Cap.Shot($"boss_{Def.id}_she_is_down");
                    for (float t = 0f; t < 0.9f; t += Time.deltaTime) yield return null;
                    if (UI != null) yield return UI.Fade(1f, 0.4f, 1f);
                    Retry();
                    if (Cam != null) Cam.SnapBehind();
                    yield return null;
                    if (UI != null) yield return UI.Fade(0f, 0.5f, 1f);
                    Paused = false;
                }
                if (DownsThisPhase >= 3 && !offered && !Paused && State != St.Dazed)
                {
                    offered = true; UI?.Hint("View / Enter: skip this part of the fight", 8f);   // QF20: Y is the kick now
                    Log($"boss {Def.id}: SKIP OFFERED after 3 knockdowns in phase {Phase + 1} (fairness)");
                }
                if (offered && !Paused && State != St.Dazed && (SkipPressed() || AutoSkip)) { SkipTaken = true; offered = false; Log($"boss {Def.id}: phase {Phase + 1} skipped"); SkipObjectives(); StartCoroutine(NextPhase()); }
                if (DemoBoot.BossIdle && Time.time - FightStart > 120f) { Log($"BOSS IDLE TEST ({Def.id}): FAIL - she is still standing after 120 s ({Attacks} attacks, {HitsOnHer} hits)"); Cap?.Finish("BOSS IDLE TEST: FAIL"); yield break; }
                yield return null;
            }
            if (AutoPilot) { Seven.ScriptMove = Vector3.zero; Seven.AutoBlockHeld = false; }
            for (float t = 0f; t < 1.2f; t += Time.deltaTime) yield return null;
            Log(Summary());
        }
        static bool SkipPressed()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current; var gp = UnityEngine.InputSystem.Gamepad.current;
            return (kb != null && kb.enterKey.wasPressedThisFrame) || (gp != null && gp.selectButton.wasPressedThisFrame);
        }

        // ---------------------------------------------------------------- the fight camera: keep him in the picture
        void FightCamera()
        {
            if (Cam == null || Paused || Cam.Subjects.Count > 0) return;
            var toB = Flat(transform.position - Her); if (toB.magnitude < 0.5f) return;
            // QF18 (19a): lock-on off = the camera plays like normal play (no pull toward him); on = it keeps him in view
            if (!ChapterSeven.LockOn) { Cam.Distance = Mathf.Lerp(Cam.Distance, 5.2f, Time.deltaTime * 2f); return; }
            Cam.Distance = Mathf.Lerp(Cam.Distance, 5.6f, Time.deltaTime * 2f); Cam.DefaultPitch = 20f;
            float want = Quaternion.LookRotation(toB).eulerAngles.y;
            float off = Mathf.DeltaAngle(Cam.Yaw, want);
            // only when he drifts out of the middle of the view: a slow ease (the right stick still turns it)
            if (Mathf.Abs(off) > 28f) Cam.Yaw += Mathf.Sign(off) * Mathf.Min(Mathf.Abs(off) - 28f, 120f * Time.deltaTime);
        }

        // ---------------------------------------------------------------- captures (the proof: the boss mid-attack)
        readonly HashSet<string> _shot = new HashSet<string>();
        void Burst(string when)
        {
            if (Cap == null || DemoBoot.CaptureDir == null) return;
            string key = (Cur != null ? Cur.id : "x") + "_" + when;
            if (when == "stagger") key = "stagger_" + Mathf.Min(Staggers, 3);
            if (_shot.Contains(key)) return; _shot.Add(key);
            StartCoroutine(Shoot($"boss_{Def.id}_{key}", when == "telegraph" ? 0.6f * StateLen : when == "active" ? Mathf.Min(0.12f, StateLen * 0.4f) : 0.3f));
        }
        protected IEnumerator Shoot(string name, float delay)
        {
            for (float t = 0f; t < delay; t += Time.deltaTime) yield return null;
            yield return Cap.Shot(name);
            // and from the side of the line between them (a second angle of the same moment)
            var mid = (Her + transform.position) * 0.5f; var side = Vector3.Cross(Vector3.up, Flat(transform.position - Her).normalized);
            float span = Flat(transform.position - Her).magnitude;
            yield return Cap.ShotFrom(name + "_side", mid + side * Mathf.Max(6f, span * 1.1f) + Vector3.up * 3.2f, mid + Vector3.up * 1f, 50f);
        }
    }

    // =====================================================================================================================
    // The white ground markers (telegraphs), the burning puddles, the cage walls, the debris glow - monochrome, additive.
    // =====================================================================================================================
    public static class Markers
    {
        static Material _add; static GameObject _root;
        static MeshRenderer _main, _cageR; static MeshFilter _mainF;
        static readonly List<(GameObject g, float until)> _pud = new List<(GameObject, float)>();
        static readonly Dictionary<Transform, Light> _glow = new Dictionary<Transform, Light>();
        public static Material Additive(float k)
        {
            var sh = Shader.Find("Seven/Additive");
            var m = sh != null ? new Material(sh) : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", Texture2D.whiteTexture);
            if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", k);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white * k);
            return m;
        }
        public static void Init()
        {
            if (_root != null) return;
            _root = new GameObject("BossMarkers");
            _add = Additive(0.6f);
            var g = new GameObject("Telegraph"); g.transform.SetParent(_root.transform, false);
            _mainF = g.AddComponent<MeshFilter>(); _main = g.AddComponent<MeshRenderer>(); _main.sharedMaterial = _add; _main.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var c = new GameObject("Cage"); c.transform.SetParent(_root.transform, false);
            c.AddComponent<MeshFilter>().sharedMesh = CageMesh(2.4f, 1.1f); _cageR = c.AddComponent<MeshRenderer>(); _cageR.sharedMaterial = Additive(0.28f); _cageR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; c.SetActive(false);
            _main.enabled = false;
        }
        public static void Clear() { if (_root != null) Object.Destroy(_root); _root = null; foreach (var p in _pud) if (p.g != null) Object.Destroy(p.g); _pud.Clear(); foreach (var kv in _glow) if (kv.Value != null) Object.Destroy(kv.Value.gameObject); _glow.Clear(); }
        public static void Hide() { if (_main != null) _main.enabled = false; if (_flash != null) _flash.enabled = false; if (_trail != null) _trail.enabled = false; }
        static Light _tell; static MeshRenderer _beam, _flash, _trail; static MeshFilter _flashF; static float _flashA, _flashR;
        /// <summary>The wind-up's white light on him (k 0..1).</summary>
        public static void Tell(Vector3 at, float k)
        {
            if (_root == null) return;
            if (_tell == null) { var g = new GameObject("TellLight"); g.transform.SetParent(_root.transform, false); _tell = g.AddComponent<Light>(); _tell.type = LightType.Point; _tell.color = Color.white; _tell.range = 7f; _tell.shadows = LightShadows.None; }
            _tell.transform.position = at; _tell.intensity = 6f * Mathf.Clamp01(k) * (0.75f + 0.25f * Mathf.Sin(Time.time * 30f));
        }
        /// <summary>The energy sweep itself: a white wall of light from the emitter, turning.</summary>
        public static void Beam(Vector3 from, float yaw, float range, bool on)
        {
            if (_root == null) return;
            if (_beam == null)
            {
                var g = new GameObject("EnergySweep"); g.transform.SetParent(_root.transform, false);
                var m = new Mesh(); m.vertices = new[] { new Vector3(0f, 0.05f, 0f), new Vector3(0f, 1.1f, 0f), new Vector3(0f, 1.1f, 1f), new Vector3(0f, 0.05f, 1f) };
                m.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right }; m.triangles = new[] { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 }; m.RecalculateBounds();
                g.AddComponent<MeshFilter>().sharedMesh = m; _beam = g.AddComponent<MeshRenderer>(); _beam.sharedMaterial = Additive(1.6f); _beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            _beam.enabled = on;
            if (!on) return;
            _beam.transform.position = from + Vector3.up * 0.02f; _beam.transform.rotation = Quaternion.Euler(0f, yaw, 0f); _beam.transform.localScale = new Vector3(1f, 1f, range);
        }
        /// <summary>The sweep / lash: a bright arc where it cuts through, at the hit moment.</summary>
        public static void Flash(Vector3 from, Vector3 dir, float range, float angle, float u)
        {
            if (_root == null) return;
            if (_flash == null) { var g = new GameObject("SweepFlash"); g.transform.SetParent(_root.transform, false); _flashF = g.AddComponent<MeshFilter>(); _flash = g.AddComponent<MeshRenderer>(); _flash.sharedMaterial = Additive(1.4f); _flash.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            if (_flashA != angle || _flashR != range) { _flashA = angle; _flashR = range; _flashF.sharedMesh = SectorMesh(range, angle, range * 0.55f); }
            _flash.enabled = u > 0.3f && u < 0.85f;
            _flash.transform.position = from + Vector3.up * 0.9f;
            _flash.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, -angle * 0.5f, 0f);
        }
        /// <summary>The snake strike: a short white streak along his path.</summary>
        public static void Trail(Vector3 at, float u)
        {
            if (_root == null) return;
            if (_trail == null) { var g = new GameObject("StrikeTrail"); g.transform.SetParent(_root.transform, false); g.AddComponent<MeshFilter>().sharedMesh = RectMesh(0.5f, 2.2f); _trail = g.AddComponent<MeshRenderer>(); _trail.sharedMaterial = Additive(1.2f); _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            _trail.enabled = u < 0.95f; _trail.transform.position = at + Vector3.up * 1.2f; _trail.transform.rotation = Quaternion.LookRotation(_main != null ? _main.transform.forward : Vector3.forward) * Quaternion.Euler(0f, 180f, 0f);
        }
        static string _shape; static float _shapeA, _shapeB; static int _spitN;

        /// <summary>u = wind-up progress: the marker brightens toward the hit; locked = it stops following her.</summary>
        public static void Show(BossFight f, AttackDef a, float u, bool locked)
        {
            if (_main == null) return;
            if (a.kind == "cage" && !locked) { }
            string shape = a.kind; float A = a.range, B = a.kind == "sweep" ? a.angle : a.kind == "beam" ? a.angle : a.width;
            if (shape != _shape || A != _shapeA || B != _shapeB || a.kind == "spit" && !locked || a.kind == "spit" && _spitN != f.Attacks)
            {
                _spitN = f.Attacks;
                if (_mainF.sharedMesh != null) Object.Destroy(_mainF.sharedMesh);
                _shape = shape; _shapeA = A; _shapeB = B;
                switch (a.kind)
                {
                    case "lunge": _mainF.sharedMesh = RectMesh(a.width * 2f, a.range); break;
                    case "wave": _mainF.sharedMesh = RectMesh(a.width, a.range); break;
                    case "sweep": _mainF.sharedMesh = SectorMesh(a.range, a.angle, 0f); break;
                    case "beam": _mainF.sharedMesh = SectorMesh(a.range, a.angle, 0f); break;
                    case "ring": _mainF.sharedMesh = RingMesh(a.range - 0.35f, a.range, 0f); break;
                    case "spit": _mainF.sharedMesh = MultiDisc(f.SpitTargets, a.width, f.transform.position); break;
                    case "cage": _mainF.sharedMesh = RingMesh(2.2f, 2.5f, 0f); break;
                }
            }
            var t = _main.transform;
            var y = Ground.Height(f.From.x, f.From.z) + 0.05f;
            if (a.kind == "spit") { t.position = new Vector3(f.transform.position.x, y, f.transform.position.z); t.rotation = Quaternion.identity; }
            else if (a.kind == "cage") { t.position = new Vector3(f.Aim.x, Ground.Height(f.Aim.x, f.Aim.z) + 0.05f, f.Aim.z); t.rotation = Quaternion.identity; }
            else if (a.kind == "beam") { t.position = new Vector3(f.From.x, y, f.From.z); t.rotation = Quaternion.Euler(0f, f.BeamFrom, 0f); }
            else { t.position = new Vector3(f.From.x, y, f.From.z); t.rotation = Quaternion.LookRotation(f.AimDir); }
            _main.enabled = true;
            float k = (locked ? 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 18f)) : 0.15f + 0.4f * u) * 0.42f;   // readable, never a blown-out white slab
            _main.material.SetFloat("_Intensity", k);
        }
        public static void Cage(Vector3? at)
        {
            if (_cageR == null) return;
            bool on = at.HasValue;
            if (_cageR.gameObject.activeSelf != on) _cageR.gameObject.SetActive(on);
            if (on) _cageR.transform.position = new Vector3(at.Value.x, Ground.Height(at.Value.x, at.Value.z), at.Value.z);
        }
        public static void Puddle(Vector3 p, float r, float seconds)
        {
            var g = new GameObject("VenomPuddle");
            g.transform.position = new Vector3(p.x, Ground.Height(p.x, p.z) + 0.03f, p.z);
            g.AddComponent<MeshFilter>().sharedMesh = DiscMesh(r, 20);
            var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = DarkGloss(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var rim = new GameObject("Burn"); rim.transform.SetParent(g.transform, false); rim.transform.localPosition = Vector3.up * 0.02f;
            rim.AddComponent<MeshFilter>().sharedMesh = RingMesh(r * 0.82f, r, 0f); var rr = rim.AddComponent<MeshRenderer>(); rr.sharedMaterial = Additive(0.8f);
            g.AddComponent<PuddleLife>().Until = Time.time + seconds;
            _pud.Add((g, Time.time + seconds));
        }
        public static void ClearPuddles() { foreach (var p in _pud) if (p.g != null) Object.Destroy(p.g); _pud.Clear(); }
        static Material _dark;
        public static Material DarkGloss()
        {
            if (_dark != null) return _dark;
            var sh = Shader.Find("Seven/DissolveLit") ?? Shader.Find("Universal Render Pipeline/Lit");
            _dark = new Material(sh);
            if (_dark.HasProperty("_BaseColor")) _dark.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.05f));
            if (_dark.HasProperty("_Smoothness")) _dark.SetFloat("_Smoothness", 0.95f);
            if (_dark.HasProperty("_EmissionColor")) _dark.SetColor("_EmissionColor", Color.black);
            return _dark;
        }
        public static void DebrisGlow(Transform t, bool on)
        {
            if (!_glow.TryGetValue(t, out var l) || l == null)
            {
                var g = new GameObject("DebrisGlow"); l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = Color.white; l.range = 2.5f; l.intensity = 0f; l.shadows = LightShadows.None;
                _glow[t] = l;
            }
            l.transform.position = t.position + Vector3.up * 0.9f;
            l.intensity = Mathf.MoveTowards(l.intensity, on ? 2.2f + 0.8f * Mathf.Sin(Time.time * 6f) : 0f, Time.deltaTime * 12f);
        }

        // ---- meshes (flat on the ground, +Z forward)
        public static Mesh RectMesh(float w, float len)
        {
            var m = new Mesh();
            m.vertices = new[] { new Vector3(-w * 0.5f, 0f, 0f), new Vector3(w * 0.5f, 0f, 0f), new Vector3(w * 0.5f, 0f, len), new Vector3(-w * 0.5f, 0f, len) };
            m.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 1, 2, 0, 2, 3 };
            m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        public static Mesh SectorMesh(float r, float ang, float r0)
        {
            var v = new List<Vector3>(); var t = new List<int>(); int n = Mathf.Max(6, (int)(ang / 6f));
            for (int i = 0; i <= n; i++) { float a = (i / (float)n * ang) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)); v.Add(d * r0); v.Add(d * r); }
            for (int i = 0; i < n; i++) { int a = i * 2; t.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2, a, a + 3, a + 1, a, a + 2, a + 3 }); }
            var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        public static Mesh RingMesh(float r0, float r1, float start) { var m = SectorMesh(r1, 360f, r0); return m; }
        public static Mesh DiscMesh(float r, int n)
        {
            var v = new List<Vector3> { Vector3.zero }; var t = new List<int>();
            for (int i = 0; i <= n; i++) { float a = i / (float)n * Mathf.PI * 2f; v.Add(new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * r * (0.85f + 0.15f * Mathf.PerlinNoise(i * 0.7f, r))); }
            for (int i = 1; i <= n; i++) t.AddRange(new[] { 0, i, i + 1 });
            var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        static Mesh MultiDisc(List<Vector3> pts, float r, Vector3 origin)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            foreach (var p in pts)
            {
                var c = Flat(p - origin);
                int b = v.Count;
                for (int i = 0; i <= 20; i++) { float a = i / 20f * Mathf.PI * 2f; var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)); v.Add(c + d * (r - 0.25f)); v.Add(c + d * r); }
                for (int i = 0; i < 20; i++) { int a = b + i * 2; t.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2, a, a + 3, a + 1, a, a + 2, a + 3 }); }
                int cb = v.Count; v.Add(c); for (int i = 0; i <= 12; i++) { float a = i / 12f * Mathf.PI * 2f; v.Add(c + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 0.18f); }
                for (int i = 1; i <= 12; i++) t.AddRange(new[] { cb, cb + i, cb + i + 1 });
            }
            var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        static Mesh CageMesh(float r, float h)
        {
            var v = new List<Vector3>(); var t = new List<int>(); int n = 24;
            for (int i = 0; i <= n; i++) { float a = i / (float)n * Mathf.PI * 2f; var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * r; v.Add(d); v.Add(d + Vector3.up * h); }
            for (int i = 0; i < n; i++) { int a = i * 2; t.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2, a, a + 3, a + 1, a, a + 2, a + 3 }); }
            var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        /// <summary>An irregular rock lump (debris for the telekinetic throw) - never a box.</summary>
        public static Mesh Rock(int seed, Vector3 size)
        {
            var rnd = new System.Random(seed);
            var v = new List<Vector3>(); var t = new List<int>(); int la = 6, lo = 9;
            for (int i = 0; i <= la; i++)
                for (int j = 0; j <= lo; j++)
                {
                    float th = i / (float)la * Mathf.PI, ph = j / (float)lo * Mathf.PI * 2f;
                    float k = j == lo ? 1f : 0.78f + 0.3f * (float)rnd.NextDouble();
                    v.Add(new Vector3(Mathf.Sin(th) * Mathf.Cos(ph) * size.x, Mathf.Cos(th) * size.y, Mathf.Sin(th) * Mathf.Sin(ph) * size.z) * k);
                }
            for (int j = 0; j <= la; j++) v[j * (lo + 1) + lo] = v[j * (lo + 1)];
            for (int i = 0; i < la; i++) for (int j = 0; j < lo; j++) { int a = i * (lo + 1) + j, b = a + lo + 1; t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
    }

    public class PuddleLife : MonoBehaviour
    {
        public float Until;
        void Update()
        {
            float left = Until - Time.time;
            if (left <= 0f) { Destroy(gameObject); return; }
            float k = Mathf.Clamp01(left / 0.6f);
            transform.localScale = new Vector3(k, 1f, k);
            var r = GetComponentInChildren<MeshRenderer>();
            foreach (var mr in GetComponentsInChildren<MeshRenderer>()) if (mr.gameObject != gameObject && mr.material.HasProperty("_Intensity")) mr.material.SetFloat("_Intensity", 0.45f + 0.35f * Mathf.Abs(Mathf.Sin(Time.time * 7f + transform.position.x)));
        }
    }
}
