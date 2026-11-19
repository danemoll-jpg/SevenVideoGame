using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF9: a Scree in the fighting tutorial on the terrace. The Scree are oppressed workers, not monsters: they block the
    /// way out and spar - a slow, clearly telegraphed punch (wind-up held a moment), stance otherwise. Hits stagger them;
    /// after <see cref="Poise"/> blows a Scree is <see cref="Dazed"/> (swaying, open for the takedown). The takedown (7's
    /// finisher) knocks it down; it stays on the ground stunned, then sits up groggily and stays there - alive, out of the
    /// fight. A dazed Scree that is not taken down recovers after a few seconds (forgiving). Only the
    /// <see cref="Engaged"/> one attacks; the other keeps its distance. Uses the sparring bot's clips (fight_idle, punch,
    /// hit_head / hit_body, knocked_down, get_up, dazed) retargeted onto the Scree.
    /// </summary>
    public partial class ScreeGuard
    {
        public enum Fight { None, Approach, Guard, Windup, Punch, Hit, Dazed, Down, Sitting }
        public Fight FightState { get; private set; } = Fight.None;
        public bool InFight => FightState != Fight.None;
        public bool Dazed => FightState == Fight.Dazed;
        public bool IsOut => FightState == Fight.Down || FightState == Fight.Sitting;
        /// <summary>Only the engaged Scree attacks (one at a time - a tutorial).</summary>
        [System.NonSerialized] public bool Engaged;
        /// <summary>The director holds the next punch until the matching prompt is on screen.</summary>
        [System.NonSerialized] public bool HoldPunch;
        public int Poise = 3;
        public float PunchEvery = 3.6f, WindupSeconds = 0.75f, PunchSpeed = 0.55f, DazedSeconds = 5f;
        public ChapterSeven Opponent;
        public Vector3 FightSpot;
        public event System.Action<ScreeGuard, string> FightEvent;
        public int Punches { get; private set; }

        int _blows; float _next, _until; bool _resolved;
        const float PunchHitAt = 0.40f;

        public void BeginFight(ChapterSeven opponent, Vector3 spot)
        {
            Opponent = opponent; FightSpot = spot; _blows = 0; _next = Time.time + 2.5f;
            UseGround = true;
            FightState = Fight.Approach;
            var rig = GetComponent<ScreeRig>(); if (rig != null) rig.Brace = 0f;
        }
        /// <summary>Back to a fresh start (7 was knocked down): on the spot, full poise.</summary>
        public void ResetFight()
        {
            Freeze(false);
            transform.position = new Vector3(FightSpot.x, Ground.Height(FightSpot.x, FightSpot.z), FightSpot.z);
            if (Opponent != null) FaceAt(Opponent.transform.position);
            _blows = 0; _next = Time.time + 3f; FightState = Fight.Guard;
            Play(HasState("fight_idle") ? "fight_idle" : "idle", 0.1f);
        }
        /// <summary>Safety net (watchdog): end its part at once - down and sitting, alive.</summary>
        public void ForceOut() { if (IsOut) return; FightState = Fight.Down; Play(HasState("knocked_down") ? "knocked_down" : "idle", 0.2f); _until = Time.time + 2f; }

        void FaceAt(Vector3 p) { var d = p - transform.position; d.y = 0f; if (d.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(d); }
        float DistTo7 { get { var d = Opponent.transform.position - transform.position; d.y = 0f; return d.magnitude; } }

        /// <summary>7's blow lands. Returns false when it cannot be hit (down).</summary>
        public bool TakeBlow(Vector3 dir, bool takedown)
        {
            if (IsOut || FightState == Fight.None) return false;
            if (takedown)
            {
                if (FightState != Fight.Dazed) return false;
                FightState = Fight.Down; Freeze(false);
                transform.rotation = Quaternion.LookRotation(-new Vector3(dir.x, 0f, dir.z));   // falls away from her
                Play(HasState("knocked_down") ? "knocked_down" : "idle", 0.08f);
                _until = Time.time + 2.2f;
                FightEvent?.Invoke(this, "taken_down");
                return true;
            }
            if (FightState == Fight.Dazed) { _until = Mathf.Max(_until, Time.time + 1.5f); Play("hit_body", 0.03f); Slide(dir, 0.15f, 0.2f); FightEvent?.Invoke(this, "hit"); return true; }
            _blows++;
            FightEvent?.Invoke(this, "hit");
            Freeze(false);
            Play(_blows % 2 == 1 ? "hit_head" : "hit_body", 0.03f);
            Slide(dir, 0.35f, 0.25f);
            if (_blows >= Poise) { FightState = Fight.Dazed; _until = Time.time + DazedSeconds; FightEvent?.Invoke(this, "dazed"); }
            else { FightState = Fight.Hit; _until = Time.time + 0.55f; _next = Mathf.Max(_next, Time.time + 1.6f); }
            return true;
        }

        /// <summary>Called from Update while in the fight; returns true when it handled the frame.</summary>
        bool UpdateFight()
        {
            if (FightState == Fight.None || Opponent == null) return false;
            switch (FightState)
            {
                case Fight.Approach:
                    if (Walk(FightSpot, 0.12f)) { FightState = Fight.Guard; Play("fight_idle", 0.25f); }
                    break;
                case Fight.Guard:
                {
                    FaceTowards(Opponent.transform.position, 220f);
                    float want = Engaged ? 1.25f : 2.6f;                   // the engaged one closes in, the other waits back
                    float d = DistTo7;
                    var to = Opponent.transform.position - transform.position; to.y = 0f;
                    if (d > want + 0.35f) { transform.position += to.normalized * Mathf.Min(d - want, 0.9f * Time.deltaTime); if (State != "walk") Play("walk", 0.2f, 0.7f, false); }
                    else if (d < want - 0.6f && !Engaged) { transform.position -= to.normalized * 0.7f * Time.deltaTime; if (State != "walk") Play("walk", 0.2f, 0.7f, false); }
                    else if (State != "fight_idle") Play("fight_idle", 0.25f, 1f, false);
                    if (Engaged && !HoldPunch && Time.time >= _next && d < 1.8f && Opponent.InFight && !Opponent.FightDown)
                    {
                        // wind-up: the punch clip starts and holds at its draw-back for a clear tell, then lets fly slowly
                        FightState = Fight.Windup; _until = Time.time + WindupSeconds; _resolved = false; Punches++;
                        Play("punch", 0.15f, PunchSpeed); FightEvent?.Invoke(this, "windup");
                    }
                    break;
                }
                case Fight.Windup:
                    FaceTowards(Opponent.transform.position, 200f);
                    if (Tn >= 0.22f) Freeze(true);
                    if (Time.time >= _until) { Freeze(false); FightState = Fight.Punch; }
                    break;
                case Fight.Punch:
                    if (!_resolved && Tn >= PunchHitAt)
                    {
                        _resolved = true;
                        var to = Opponent.transform.position - transform.position; to.y = 0f;
                        bool reach = to.magnitude < 1.6f && Vector3.Dot(transform.forward, to.normalized) > 0.45f;
                        if (!reach) FightEvent?.Invoke(this, "missed");
                        else if (Opponent.IsDefending) { FightEvent?.Invoke(this, "defended"); Play("hit_body", 0.1f); Slide(-transform.forward, 0.45f, 0.35f); FightState = Fight.Hit; _until = Time.time + 0.6f; }
                        else { Opponent.TakeFightHit(to.normalized); FightEvent?.Invoke(this, "landed"); }
                    }
                    if (FightState == Fight.Punch && Tn >= 0.9f) { FightState = Fight.Guard; _next = Time.time + PunchEvery; Play("fight_idle", 0.2f); }
                    break;
                case Fight.Hit:
                    if (Time.time >= _until) { FightState = Fight.Guard; Play("fight_idle", 0.2f); }
                    break;
                case Fight.Dazed:
                    if (State != "dazed" && State != "hit_body" || State == "hit_body" && Tn > 0.8f) Play(HasState("dazed") ? "dazed" : "idle", 0.2f, 1f, false);
                    if (Time.time >= _until) { _blows = Poise - 1; FightState = Fight.Guard; _next = Time.time + 2f; Play("fight_idle", 0.3f); FightEvent?.Invoke(this, "recovered"); }
                    break;
                case Fight.Down:
                    if (Tn >= 0.98f) Freeze(true);                                  // stunned on the ground
                    if (Time.time >= _until)
                    {
                        // sits up groggily (the first part of getting up, held) and stays there, out of the way
                        FightState = Fight.Sitting; Play(HasState("get_up") ? "get_up" : "idle", 0.3f, 0.6f); _until = 0f;
                        FightEvent?.Invoke(this, "sitting");
                    }
                    break;
                case Fight.Sitting:
                    if (State == "get_up" && Tn >= 0.3f) Freeze(true);
                    break;
            }
            return true;
        }
    }
}
