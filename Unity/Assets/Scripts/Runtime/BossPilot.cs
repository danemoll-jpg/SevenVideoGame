using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF17: how a player plays a boss fight - used by the check runs (Auto: 7's script flags) and by the player-style runs
    /// (a virtual gamepad on the real input actions). It reads only what a player sees: his wind-up and the white marker on
    /// the ground (where it will land, when it locks), the burning puddles, whether he is open (the debris glows), whether
    /// he is staggered or dazed, and the objective's light. Like a real player it does not answer every attack: a fixed
    /// share of reads are late or wrong (<see cref="Skill"/>), so the log shows her taking hits too. With -bossidle nobody
    /// presses anything (the idle test: the boss must put her down on his own within ~60 s).
    /// </summary>
    public static class BossPilot
    {
        public struct Intent { public Vector3 Move; public bool Run, Dodge, Hop, Tk, Attack, Kick, Use, Takedown, Block, Skip; public string Why; }
        /// <summary>QF20 (2): a player mixes the buttons - X, X, Y, X, Y, Y ... (punch, punch, kick ...): one combo.</summary>
        static int _seq;
        static readonly bool[] KickAt = { false, false, true, false, true, true };
        static void Strike(ref Intent I) { if (KickAt[_seq++ % KickAt.Length]) I.Kick = true; else I.Attack = true; }
        public static float Skill = 0.82f;
        /// <summary>-bossnovice: a first-time player - reads fewer attacks in time, misses many of his openings, keeps
        /// pressing on an objective while being shot at (the fight length a new player gets).</summary>
        static bool Novice => DemoBoot.BossNovice;
        static int _openSeen = -1; static bool _openTake;
        static int _decidedFor = -1; static string _answer; static bool _pressed;
        static float _nextStrike, _tkAt, _useAt;
        static Vector3 _useFor = new Vector3(1e6f, 0f, 0f);
        /// <summary>QF19 proof: X presses the pilot made at objectives, and how many were second presses (must be 0).</summary>
        public static int UsePresses, UseRepeats;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        static Vector3 boss0(BossFight f) => f.transform.position;

        public static Intent Think(BossFight f, ChapterSeven s)
        {
            var I = new Intent();
            if (f == null || s == null || f.Over || f.Paused || s.FightDown || DemoBoot.BossIdle) return I;
            // stuck: after five knockdowns in one part a player takes the offered skip (Y) - counted in the log
            if (f.DownsThisPhase >= 5 && f.State != BossFight.St.Dazed) { I.Skip = Time.frameCount % 20 < 3; I.Why = "takes the skip"; return I; }
            // QF20 (3) proof (-bossdownonce): once, mid-fight (phase 2+, some damage done), the pilot stops answering him
            // until she goes down - then plays on (his energy before / after the retry is logged)
            if (DemoBoot.BossDownOnce && f.Knockdowns == 0 && f.Phase >= 1 && f.Energy < 0.85f && f.State != BossFight.St.Dazed) { I.Move = Flat(boss0(f) - s.transform.position).normalized * 0.3f; I.Why = "lets him hit her (the retry test)"; return I; }
            var her = s.transform.position; var boss = f.transform.position;
            var toB = Flat(boss - her); float d = toB.magnitude; var toBn = d > 0.01f ? toB / d : Vector3.forward;

            // 1) an attack coming: read it once per attack (a player's read), then answer at the right moment
            if ((f.State == BossFight.St.Telegraph || f.State == BossFight.St.Active) && f.Cur != null)
            {
                var a = f.Cur;
                if (_decidedFor != f.Attacks)
                {
                    _decidedFor = f.Attacks; _pressed = false;
                    if (Random.value > (Novice ? 0.66f : Skill)) _answer = "late";
                    else if (a.kind == "spit") _answer = "step";
                    else if (a.jumpable && (a.kind == "wave" || a.kind == "ring" || a.kind == "beam" || Random.value < 0.4f)) _answer = "hop";
                    else if (a.dodgeable) _answer = "dodge";
                    else if (a.blockable) _answer = "block";
                    else _answer = "away";
                }
                bool danger = f.Danger(her);
                float tth = f.TimeToHit;
                if (danger && _answer != "late")
                {
                    I.Why = $"{a.id}: {_answer} ({tth:0.00}s)";
                    switch (_answer)
                    {
                        case "hop": if (!_pressed && tth <= 0.2f && tth > -0.3f) { I.Hop = true; _pressed = true; } break;
                        case "dodge":
                        {
                            var side = Vector3.Cross(Vector3.up, f.AimDir);
                            if (Vector3.Dot(side, Flat(her - f.From)) < 0f) side = -side;
                            I.Move = a.kind == "sweep" ? -toBn : side;
                            if (!_pressed && tth <= 0.2f && tth > -0.3f) { I.Dodge = true; _pressed = true; }
                            break;
                        }
                        case "block": I.Block = tth < 0.6f; I.Move = Vector3.zero; break;
                        case "step":
                        {
                            // walk out from between the marked spots (sideways to his aim), dodge if still in one at the end
                            var side = Vector3.Cross(Vector3.up, f.AimDir); if (Mathf.Sin(f.Attacks * 1.7f) < 0f) side = -side;
                            I.Move = side; I.Run = true;
                            if (!_pressed && tth <= 0.22f) { I.Dodge = true; _pressed = true; }
                            break;
                        }
                        default: I.Move = -toBn; I.Run = true; break;
                    }
                    return I;
                }
            }
            // 2) the burning puddles: never stand in one
            foreach (var pd in f.Puddles)
                if (Flat(her - pd.p).magnitude < pd.r + 0.4f) { I.Move = Flat(her - pd.p).normalized; I.Run = true; I.Why = "out of a puddle"; return I; }
            foreach (var hz in f.Hazards())
                if (Flat(her - hz).magnitude < 2.4f) { I.Move = Flat(her - hz).normalized; I.Run = true; I.Why = "round a creature"; return I; }
            // 3) caged: wait for it (the next attack inside it is answered above)
            // 4) the finish: dazed -> takedown
            if (f.State == BossFight.St.Dazed)
            {
                I.Why = "takedown";
                if (d > 1.6f) { I.Move = toBn; I.Run = true; }
                else I.Takedown = true;
                return I;
            }
            // 4b) QF29: his bar waits at its mark for an objective (it pulses, the place is lit, a companion calls it) - a player goes
            // there now instead of stunning him again and again (the Moon finale's run stalled here: 1400 stuns, never the barrier)
            if (f.WaitingOnObjective && f.PilotObjective().HasValue) goto objective;
            // 5) staggered: press the advantage (strikes)
            if (f.State == BossFight.St.Staggered && !f.IsOut)
            {
                I.Why = "strikes while he reels";
                if (d > 1.5f) { I.Move = toBn; I.Run = true; }
                else if (Time.time >= _nextStrike) { Strike(ref I); _nextStrike = Time.time + 0.28f; }
                return I;
            }
            // 6) open: the telekinetic throw (debris in reach), else close in for three strikes
            if (f.Open && !f.IsOut && Novice)
            {
                // a new player notices only some of his openings
                if (_openSeen != f.Attacks) { _openSeen = f.Attacks; _openTake = Random.value < 0.6f; }
            }
            if (f.Open && !f.IsOut && (!Novice || _openTake))
            {
                if (f.DebrisNear && Time.time - _tkAt > 1.2f) { I.Tk = true; _tkAt = Time.time; I.Why = "he is open - throw"; return I; }
                var deb = f.NearestDebrisAt;
                if (deb.HasValue && Flat(deb.Value - her).magnitude < 9f && Flat(deb.Value - her).magnitude > f.Def.tkRange - 1f) { I.Move = Flat(deb.Value - her).normalized; I.Run = true; I.Why = "toward debris"; return I; }
                if (d < 1.6f && Time.time >= _nextStrike) { Strike(ref I); _nextStrike = Time.time + 0.28f; I.Why = "strike while open"; return I; }
                if (d < 6f) { I.Move = toBn; I.Run = true; I.Why = "close in while open"; return I; }
            }
            // 7) an objective of this phase (a pylon, a device)
            objective:
            var obj = f.PilotObjective();
            if (obj.HasValue)
            {
                var (at, how) = obj.Value; var to = Flat(at - her); float od = to.magnitude;
                I.Why = "objective " + how;
                if (how == "tk" && od < 9f) { if (Time.time - _tkAt > 1.0f) { I.Tk = true; _tkAt = Time.time; } return I; }
                // QF19: like a player - walk up until the X prompt shows, press X ONCE; it must go down on that press.
                // (A second press at the same objective is counted as a failure in the log: UseRepeats must stay 0.)
                if (how == "use" && f.PromptLabel != null && !f.PromptIsTk && f.PromptLabel != "Takedown")
                {
                    if (_useFor != at) { _useFor = at; _useAt = Time.time; I.Use = true; UsePresses++; I.Why = $"X once at '{f.PromptLabel}' ({od:0.0} m)"; return I; }
                    if (Time.time - _useAt > 1.0f) { _useAt = Time.time; I.Use = true; UsePresses++; UseRepeats++; I.Why = $"X AGAIN at '{f.PromptLabel}' - the first press did nothing"; return I; }
                    I.Why = "waits after the press"; return I;
                }
                I.Move = to.normalized; I.Run = od > 4f; return I;
            }
            // 8) otherwise: keep his distance, circle him, stay near something to throw
            {
                float keep = Mathf.Max(3.5f, f.P.keep);
                var deb = f.NearestDebrisAt;
                Vector3 m;
                if (d > keep + 2.5f) m = toBn;
                else if (d < keep - 1f) m = -toBn;
                else m = Vector3.Cross(Vector3.up, toBn) * (Mathf.Sin(Time.time * 0.3f) > 0f ? 0.6f : -0.6f);
                if (deb.HasValue && Flat(deb.Value - her).magnitude > f.Def.tkRange * 0.7f) m = (m + Flat(deb.Value - her).normalized * 0.8f).normalized * 0.8f;
                I.Move = m; I.Why = "spacing";
            }
            return I;
        }

        /// <summary>Check runs: the intent through 7's script flags.</summary>
        public static void ApplyAuto(Intent I, ChapterSeven s)
        {
            s.ScriptDriven = true;
            s.ScriptMove = I.Move; s.ScriptRun = I.Run;
            if (I.Dodge) s.AutoDodge = true; if (I.Hop) s.AutoHop = true; if (I.Tk) s.AutoTk = true;
            if (I.Attack) s.AutoAttack = true; if (I.Kick) s.AutoKick = true; if (I.Takedown) s.AutoTakedown = true; if (I.Use) s.AutoUse = true;
            s.AutoBlockHeld = I.Block;
        }

        /// <summary>Player-style runs: the intent as a pad state (stick relative to the camera, the real buttons).</summary>
        public static Vector2 Stick(Vector3 world)
        {
            var cam = Camera.main != null ? Camera.main.transform : null;
            if (cam == null || world.sqrMagnitude < 0.0001f) return Vector2.zero;
            var cf = Flat(cam.forward).normalized; var cr = new Vector3(cf.z, 0f, -cf.x);
            var v = new Vector2(Vector3.Dot(world, cr), Vector3.Dot(world, cf));
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }
    }
}
