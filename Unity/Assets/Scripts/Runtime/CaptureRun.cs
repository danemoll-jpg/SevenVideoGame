using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Check run (-capture <dir>): drives a fixed fight sequence, renders the main camera off-screen into a
    /// RenderTexture and writes PNGs plus capture_log.txt, then quits. Never needs the window or focus.
    /// </summary>
    public class CaptureRun : MonoBehaviour
    {
        FightDirector _d;
        string _dir;
        RenderTexture _rt;
        readonly StringBuilder _log = new StringBuilder();
        float _t0;

        public void Init(FightDirector d)
        {
            _d = d;
            _dir = DemoBoot.CaptureDir;
            Directory.CreateDirectory(_dir);
            _rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var cam = _d.Cam.GetComponent<Camera>();
            cam.targetTexture = _rt;
            _d.Bot.AutoPunch = false;
            _d.Seven.ScriptDriven = true;
            _t0 = Time.realtimeSinceStartup;
            Log($"capture start, muted={DemoBoot.Muted}, listenerVolume={AudioListener.volume}");
            StartCoroutine(Run());
            StartCoroutine(Watchdog());
        }

        void Log(string s)
        {
            var line = $"[{Time.realtimeSinceStartup - _t0,6:0.00}s] {s}";
            _log.AppendLine(line);
            Debug.Log("[Seven] " + line);
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            var tex = new Texture2D(_rt.width, _rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(_dir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
            Log($"shot {name}: seven={_d.Seven.State}/{_d.Seven.Current} bot={_d.Bot.State}/{_d.Bot.Current} " +
                $"sep={Sep():0.00}m");
        }

        float Sep()
        {
            var a = _d.Seven.transform.position; var b = _d.Bot.transform.position;
            a.y = b.y = 0; return Vector3.Distance(a, b);
        }

        IEnumerator Until(Func<bool> cond, float timeout, string what)
        {
            float end = Time.time + timeout;
            while (!cond() && Time.time < end) yield return null;
            if (!cond()) Log($"TIMEOUT waiting for {what}");
        }

        /// <summary>Walk 7 straight at the bot (input converted to camera space) until the gap is <= sep.</summary>
        IEnumerator Approach(float sep, float timeout)
        {
            var seven = _d.Seven;
            float end = Time.time + timeout;
            while (Sep() > sep && Time.time < end)
            {
                var to = _d.Bot.transform.position - seven.transform.position; to.y = 0f; to.Normalize();
                var f = _d.Cam.transform.forward; f.y = 0f; f.Normalize();
                var r = new Vector3(f.z, 0f, -f.x);
                seven.ScriptMove = new Vector2(Vector3.Dot(to, r), Vector3.Dot(to, f));
                yield return null;
            }
            seven.ScriptMove = Vector2.zero;
            if (Sep() > sep) Log($"TIMEOUT approaching to {sep:0.00} m (at {Sep():0.00})");
        }

        /// <summary>Lowest point of 7's skinned mesh (world y) — feet-on-floor check.</summary>
        void LogLowest(string when)
        {
            float lowest = float.MaxValue, lowestToe = float.MaxValue;
            foreach (var smr in _d.Seven.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var m = new Mesh();
                smr.BakeMesh(m, true);
                var tr = smr.transform;
                foreach (var v in m.vertices) lowest = Mathf.Min(lowest, tr.TransformPoint(v).y);
                Destroy(m);
            }
            foreach (var b in new[] { HumanBodyBones.LeftToes, HumanBodyBones.RightToes })
            {
                var t = _d.Seven.Bone(b);
                if (t != null) lowestToe = Mathf.Min(lowestToe, t.position.y);
            }
            Log($"feet ({when}): lowest mesh point y={lowest:0.000} m, lowest toe bone y={lowestToe:0.000} m");
        }

        void LogAudio()
        {
            var buf = new float[512];
            double sum = 0; int n = 0;
            for (int ch = 0; ch < 2; ch++)
            {
                AudioListener.GetOutputData(buf, ch);
                foreach (var s in buf) { sum += s * s; n++; }
            }
            Log($"audio: listenerVolume={AudioListener.volume} paused={AudioListener.pause} outputRMS={Math.Sqrt(sum / n):0.000000}");
        }

        IEnumerator Run()
        {
            var seven = _d.Seven; var bot = _d.Bot;
            string lastHit = null;
            seven.HitLanded += s => lastHit = s;
            bool? lastPunch = null;
            bot.PunchResolved += b => lastPunch = b;

            yield return new WaitForSeconds(2.0f);
            yield return Shot("01_idle");
            LogLowest("fight idle");
            LogAudio();

            // Step in to sparring distance.
            StartCoroutine(Approach(1.25f, 4f));
            yield return new WaitForSeconds(0.6f);
            yield return Shot("02_walk");
            yield return Until(() => Sep() <= 1.26f, 4f, "walk in");
            yield return new WaitForSeconds(0.8f);

            // Jab connects.
            lastHit = null;
            seven.DoAttack("jab");
            yield return Until(() => lastHit != null, 3f, "jab hit");
            yield return new WaitForSeconds(0.1f);
            yield return Shot("03_punch_connect");
            yield return Until(() => bot.Current == TrainingBot.Mode.Idle && seven.Current == SevenFighter.Mode.Free, 4f, "settle");
            yield return new WaitForSeconds(0.6f);

            // Bot throws its slow punch; 7 blocks.
            seven.ScriptBlockHeld = true;
            yield return new WaitForSeconds(0.5f);
            if (Sep() > 1.5f) { Log($"bot too far for punch ({Sep():0.00})"); }
            lastPunch = null;
            bot.StartPunch();
            yield return Until(() => bot.Current == TrainingBot.Mode.Punch && bot.Tn >= 0.34f, 4f, "punch incoming");
            yield return Shot("04_block");
            yield return Until(() => lastPunch != null, 4f, "bot punch");
            Log($"bot punch defended={lastPunch}");
            yield return new WaitForSeconds(0.35f);
            yield return Shot("05_bot_staggers");
            seven.ScriptBlockHeld = false;
            yield return Until(() => bot.Current == TrainingBot.Mode.Idle, 4f, "bot idle");
            yield return new WaitForSeconds(0.6f);

            // Roundhouse kick.
            yield return Approach(1.3f, 4f);
            yield return new WaitForSeconds(0.5f);
            lastHit = null;
            seven.DoAttack("roundhouse");
            yield return Until(() => lastHit != null, 3f, "kick hit");
            yield return new WaitForSeconds(0.08f);
            yield return Shot("06_kick_connect");
            yield return Until(() => bot.Current == TrainingBot.Mode.Idle && seven.Current == SevenFighter.Mode.Free, 4f, "settle");
            yield return new WaitForSeconds(0.6f);

            // Finisher -> knocked down -> getting up -> dazed -> fighting idle.
            yield return Approach(1.4f, 4f);
            yield return new WaitForSeconds(0.5f);
            lastHit = null;
            seven.DoAttack("finisher");
            yield return Until(() => lastHit != null, 3f, "finisher hit");
            yield return Shot("07_finisher_connect");
            yield return Until(() => bot.Current == TrainingBot.Mode.Down && bot.Tn > 0.85f, 5f, "knocked down");
            yield return Shot("08_knockdown");
            LogLowest("after finisher");
            yield return Until(() => bot.Current == TrainingBot.Mode.GetUp && bot.Tn > 0.45f, 6f, "getting up");
            yield return Shot("09_getup");
            yield return Until(() => bot.Current == TrainingBot.Mode.Dazed, 6f, "dazed");
            yield return new WaitForSeconds(1.0f);
            yield return Shot("10_dazed");
            yield return Until(() => bot.Current == TrainingBot.Mode.Idle, 4f, "recovered");
            Log("bot recovered to fight idle");
            yield return new WaitForSeconds(0.8f);
            yield return Shot("11_recovered");

            // Telekinesis push.
            yield return Approach(1.6f, 4f);
            yield return new WaitForSeconds(0.4f);
            float before = Sep();
            lastHit = null;
            seven.DoAttack("telekinesis");
            yield return Until(() => lastHit != null, 4f, "telekinesis");
            yield return new WaitForSeconds(0.45f);
            yield return Shot("12_telekinesis");
            yield return new WaitForSeconds(0.6f);
            Log($"telekinesis: separation {before:0.00} -> {Sep():0.00} m");
            yield return Until(() => seven.Current == SevenFighter.Mode.Free, 4f, "settle");
            yield return new WaitForSeconds(0.5f);
            LogLowest("end");

            // Close-up of 7's head: hair-tie cuffs and forehead dot.
            _d.Cam.Hold = true;
            var head = seven.Bone(HumanBodyBones.Head);
            var camT = _d.Cam.transform;
            var face = seven.transform.forward;
            camT.position = head.position + Vector3.up * 0.05f + face * 0.75f + seven.transform.right * 0.25f;
            camT.LookAt(head.position + Vector3.up * 0.06f);
            yield return null;
            yield return Shot("13_head_front");
            camT.position = head.position + Vector3.up * 0.25f - face * 0.7f - seven.transform.right * 0.2f;
            camT.LookAt(head.position + Vector3.up * 0.08f);
            yield return null;
            yield return Shot("14_head_back");

            LogAudio();
            Finish("done");
        }

        IEnumerator Watchdog()
        {
            yield return new WaitForSecondsRealtime(150f);
            Finish("WATCHDOG timeout");
        }

        bool _finished;

        void Finish(string why)
        {
            if (_finished) return;
            _finished = true;
            Log($"capture {why}");
            File.WriteAllText(Path.Combine(_dir, "capture_log.txt"), _log.ToString());
            Application.Quit();
        }
    }
}
