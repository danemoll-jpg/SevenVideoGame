using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 8 check run (-fightcombo -capture dir, muted, off-screen): presses a combo the way a player mashes it
    /// (each next button pressed early, while the previous move is still playing) and logs, for every press, when the
    /// move actually started (input → move) and the gap between consecutive moves (move → next move), plus what the
    /// pre-Round-8 rules would have done with the same presses (early presses were dropped; the next move could only
    /// start at the old "End" point of the previous one, from a fresh press). Screenshots at each connect.
    /// </summary>
    public class FightComboRun : MonoBehaviour
    {
        FightDirector _d;
        readonly List<(string s, float t)> _presses = new List<(string, float)>();
        readonly List<(string s, float t)> _starts = new List<(string, float)>();
        readonly List<(string s, float t)> _hits = new List<(string, float)>();
        ChapterCapture Cap => ChapterCapture.I;

        // pre-Round-8 "End" (normalized) per move = earliest moment a NEW press could start the next move
        static readonly Dictionary<string, float> OldEnd = new Dictionary<string, float>
        { { "jab", 0.62f }, { "cross", 0.6f }, { "hook", 0.66f }, { "roundhouse", 0.75f }, { "mma_kick", 0.72f }, { "finisher", 0.93f } };

        public void Init(FightDirector d) { _d = d; }

        IEnumerator Start()
        {
            ChapterCapture.Ensure();
            Cap.Attach(_d.Cam.GetComponent<Camera>());
            var s7 = _d.Seven; var bot = _d.Bot;
            s7.ScriptDriven = true;
            bot.AutoPunch = false;
            s7.Pressed += (s, t) => _presses.Add((s, t));
            s7.MoveStarted += (s, t) => { _starts.Add((s, t)); Cap.Log($"move start {s} at {t:0.000}"); };
            s7.HitLanded += s => { _hits.Add((s, Time.time)); Cap.Log($"connect {s} at {Time.time:0.000}"); StartCoroutine(Shot("connect_" + s + "_" + _hits.Count)); };
            yield return new WaitForSeconds(1.5f);
            // stand in range, facing the bot (jab reach 1.45 m)
            var toBot = bot.transform.position - s7.transform.position; toBot.y = 0f;
            s7.transform.position = bot.transform.position - toBot.normalized * 1.2f;
            s7.transform.rotation = Quaternion.LookRotation(toBot);
            yield return new WaitForSeconds(0.5f);
            foreach (var st in OldEnd.Keys) Cap.Log($"clip {st}: {s7.Length(st):0.000}s (old End {OldEnd[st]:0.00} -> {OldEnd[st] * s7.Length(st):0.000}s)");

            // A player's combo: each next button ~0.12 s after the previous move's blow lands (during its recovery,
            // before its old End) - the moment people actually press when chaining.
            var combo = new[] { "jab", "cross", "hook", "mma_kick", "roundhouse" };
            float t0 = Time.time;
            for (int i = 0; i < combo.Length; i++)
            {
                int hitsBefore = _hits.Count;
                s7.DoAttack(combo[i]);
                float waitStart = Time.time;
                // wait for this move to start, then for its blow, then press the next one shortly after
                while (_starts.Count(x => x.s == combo[i]) == 0 && Time.time - waitStart < 1.5f) yield return null;
                float started = Time.time;
                while (_hits.Count == hitsBefore && Time.time - started < 1.5f) yield return null;
                yield return new WaitForSeconds(0.12f);
            }
            yield return new WaitForSeconds(1.2f);
            Cap.Log($"combo of {combo.Length} took {_starts.Last().t - _starts.First().t:0.000}s from first to last move start; connects {_hits.Count}");

            // Report: input -> move, move -> next move (new), and the old rules for the same presses.
            var report = new System.Text.StringBuilder("timings (s):\n");
            float oldClock = 0f; float newPrev = -1f; float oldPrev = -1f; string prevState = null;
            for (int i = 0; i < combo.Length; i++)
            {
                var press = _presses.FirstOrDefault(p => p.s == combo[i]).t;
                var start = _starts.FirstOrDefault(p => p.s == combo[i]).t;
                // old rules: the move starts at the press if the previous move had reached its old End, else the press
                // is dropped and the player must press again after End (best case: right at End).
                float oldStart = i == 0 ? press : Mathf.Max(press, oldPrev + OldEnd[prevState] * s7.Length(prevState));
                bool oldDropped = i > 0 && press < oldPrev + OldEnd[prevState] * s7.Length(prevState);
                report.AppendLine($"  {combo[i],-10} input->move new {start - press:0.000}  |  move->move new {(newPrev < 0 ? 0f : start - newPrev):0.000}  old {(oldPrev < 0 ? 0f : oldStart - oldPrev):0.000}{(oldDropped ? " (old: this press was DROPPED; needs a 2nd press after End)" : "")}");
                newPrev = start; oldPrev = oldStart; prevState = combo[i];
                oldClock = oldStart;
            }
            Cap.Log(report.ToString());

            // Dodge out of a recovery and a buffered press during a dodge.
            int n = _starts.Count;
            s7.DoAttack("jab");
            yield return new WaitUntil(() => _hits.Count > 0 && s7.Current == SevenFighter.Mode.Attack);
            yield return new WaitForSeconds(0.1f);
            float dp = Time.time; s7.DoDodge();
            yield return null;
            Cap.Log($"dodge pressed during the jab's recovery: started {(s7.Current == SevenFighter.Mode.Dodge ? "yes" : "no")} after {Time.time - dp:0.000}s");
            yield return new WaitUntil(() => s7.State != "dodge" || s7.Tn >= 0.5f);
            float bp = Time.time; s7.DoAttack("cross");   // pressed late in the dodge: buffered until it may start
            yield return new WaitUntil(() => s7.State == "cross" || Time.time - bp > 1.5f);
            Cap.Log($"cross pressed during the dodge (buffered): started {(s7.State == "cross" ? "yes" : "no")} {Time.time - bp:0.000}s after the press");
            yield return new WaitForSeconds(1.5f);
            Cap.Finish($"fight combo check finished: {_starts.Count} move starts, {_hits.Count} connects");
        }

        IEnumerator Shot(string name) { yield return null; yield return Cap.Shot(name); }
    }
}
