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

        /// <summary>Feet-on-ground check on uneven terrain: each foot / toe bone and the lowest mesh point are measured
        /// against the ground height directly under them (not against y = 0).</summary>
        void LogFeet(FighterBase f, string who, string when)
        {
            float lowest = float.MaxValue;
            foreach (var smr in f.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var m = new Mesh();
                smr.BakeMesh(m, true);
                var tr = smr.transform;
                foreach (var v in m.vertices)
                {
                    var w = tr.TransformPoint(v);
                    lowest = Mathf.Min(lowest, w.y - Ground.Height(w.x, w.z));
                }
                Destroy(m);
            }
            var sb = new StringBuilder();
            foreach (var b in new[] { HumanBodyBones.LeftToes, HumanBodyBones.RightToes, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
            {
                var t = f.Bone(b);
                if (t != null) sb.Append($" {b}={t.position.y - Ground.Height(t.position.x, t.position.z):0.000}");
            }
            var p = f.transform.position;
            Log($"feet {who} ({when}): root above ground {p.y - Ground.Height(p.x, p.z):0.000} m, lowest mesh point above ground {lowest:0.000} m, bones above ground:{sb}");
        }

        void LogCamera(string what)
        {
            var c = _d.Cam.transform.position;
            Log($"camera {what}: yaw={_d.Cam.Yaw:0.0} pitch={_d.Cam.Pitch:0.0} height above ground {c.y - Ground.Height(c.x, c.z):0.00} m");
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

        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>Walk 7 away from the bot until the gap is >= sep.</summary>
        IEnumerator BackOff(float sep, float timeout)
        {
            var seven = _d.Seven;
            float end = Time.time + timeout;
            while (Sep() < sep && Time.time < end)
            {
                var to = seven.transform.position - _d.Bot.transform.position; to.y = 0f; to.Normalize();
                var f = _d.Cam.transform.forward; f.y = 0f; f.Normalize();
                var r = new Vector3(f.z, 0f, -f.x);
                seven.ScriptMove = new Vector2(Vector3.Dot(to, r), Vector3.Dot(to, f));
                yield return null;
            }
            seven.ScriptMove = Vector2.zero;
        }

        IEnumerator Run()
        {
            var seven = _d.Seven; var bot = _d.Bot; var cam = _d.Cam; var legend = _d.Legend;
            string lastHit = null;
            seven.HitLanded += s => lastHit = s;
            bool landed = false;
            bot.Landed += () => landed = true;
            if (legend != null) { legend.AutoSwitch = false; legend.gameObject.SetActive(false); }

            yield return new WaitForSeconds(2.5f);
            yield return Shot("01_wide");
            LogCamera("default");
            LogFeet(seven, "7", "fight idle");
            LogFeet(bot, "bot", "fight idle");
            LogAudio();

            // Orbit: four yaw angles around the 3/4 framing, plus the pitch limits.
            float baseYaw = cam.RecenterYaw();
            int[] yaws = { 0, 90, 180, 270 };
            for (int i = 0; i < yaws.Length; i++)
            {
                cam.SetOrbit(baseYaw + yaws[i], 14f);
                yield return Frames(3);
                yield return Shot($"{2 + i:00}_orbit_{yaws[i]:000}");
                LogCamera($"orbit {yaws[i]}");
            }
            cam.SetOrbit(baseYaw + 30f, 60f);
            yield return Frames(3);
            yield return Shot("06_orbit_top_pitch60");
            LogCamera("pitch max");
            cam.SetOrbit(baseYaw + 200f, -5f);
            yield return Frames(3);
            yield return Shot("07_orbit_low_pitch-5");
            LogCamera("pitch min");
            cam.SetOrbit(cam.Yaw, 80f);
            Log($"pitch clamp: asked 80 -> {cam.Pitch:0.0}");
            cam.SetOrbit(baseYaw, 2f);
            yield return Frames(3);
            yield return Shot("01b_wide_level");
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-quick") >= 0) { LogAudio(); Finish("done (quick: look shots only)"); yield break; }

            // Camera-relative movement: stick up must walk away from the camera at any orbit angle.
            cam.SetOrbit(baseYaw + 120f, 14f);
            yield return Frames(2);
            var p0 = seven.transform.position;
            var camFwd = cam.transform.forward; camFwd.y = 0f; camFwd.Normalize();
            seven.ScriptMove = new Vector2(0f, 1f);
            yield return new WaitForSeconds(0.5f);
            seven.ScriptMove = Vector2.zero;
            var moved = seven.transform.position - p0; moved.y = 0f;
            Log($"camera-relative move: stick up at yaw {cam.Yaw:0} moved {moved.magnitude:0.00} m, dot with camera forward = {Vector3.Dot(moved.normalized, camFwd):0.000}");

            // Re-centre.
            cam.StartRecenter();
            yield return new WaitForSeconds(0.9f);
            Log($"recenter: yaw {cam.Yaw:0.0} vs 3/4 target {cam.RecenterYaw():0.0}, pitch {cam.Pitch:0.0}");

            // Legend: Xbox then keyboard.
            if (legend != null)
            {
                legend.gameObject.SetActive(true);
                legend.SetMode(true);
                yield return Frames(3);
                yield return Shot("08_legend_xbox");
                legend.SetMode(false);
                yield return Frames(3);
                yield return Shot("09_legend_keys");
                legend.SetMode(true);
                legend.gameObject.SetActive(false);
            }

            // Jab connects.
            yield return Approach(1.25f, 4f);
            yield return new WaitForSeconds(0.6f);
            lastHit = null;
            seven.DoAttack("jab");
            yield return Until(() => lastHit != null, 3f, "jab hit");
            yield return new WaitForSeconds(0.1f);
            yield return Shot("10_punch_connect");
            yield return Until(() => bot.Current == TrainingBot.Mode.Idle && seven.Current == SevenFighter.Mode.Free, 4f, "settle");
            yield return new WaitForSeconds(0.4f);

            // Telekinesis lift: hold, release, landing, recovery.
            yield return BackOff(2.8f, 4f);
            yield return new WaitForSeconds(0.5f);
            cam.StartRecenter();
            yield return new WaitForSeconds(0.5f);
            float before = Sep();
            landed = false;
            seven.ScriptTkHeld = true;
            seven.DoAttack("telekinesis");
            yield return Until(() => seven.Tk == SevenFighter.TkPhase.Charging && seven.TkPhaseTime > 0.45f, 3f, "tk charging");
            yield return Shot("11_tk_start");
            Log($"tk start: charge={_d.Fx.Charge:0.00}");
            yield return Until(() => seven.Current == SevenFighter.Mode.Lift, 3f, "lift start");
            yield return Until(() => seven.LiftSeconds > 1.8f, 4f, "lift held");
            yield return Shot("12_tk_held");
            Log($"tk held: bot height {bot.Height:0.00} m (max {bot.MaxHeightThisLift:0.00}), bot mode {bot.Current}, debris floating {_d.Fx.FloatingDebris}, aura={_d.Fx.Aura:0.00}");
            LogFeet(bot, "bot", "lifted (expected ~0.8 above ground)");
            cam.SetOrbit(cam.Yaw + 70f, 10f);
            yield return Frames(3);
            yield return Shot("12b_tk_held_side");
            cam.StartRecenter();
            yield return new WaitForSeconds(0.6f);
            seven.ScriptTkHeld = false;
            float releasedAt = Time.time;
            yield return new WaitForSeconds(0.3f);
            yield return Shot("13_tk_release");
            yield return Until(() => landed, 3f, "bot landing");
            Log($"tk landing {Time.time - releasedAt:0.00}s after release, separation {before:0.00} -> {Sep():0.00} m, bot height {bot.Height:0.000}");
            yield return new WaitForSeconds(0.15f);
            yield return Shot("14_tk_landing");
            yield return Until(() => bot.Current == TrainingBot.Mode.Dazed, 8f, "dazed after lift");
            yield return new WaitForSeconds(0.6f);
            yield return Shot("15_tk_dazed");
            yield return Until(() => bot.Current == TrainingBot.Mode.Idle, 5f, "recovered after lift");
            Log($"bot recovered after lift: mode {bot.Current}");
            LogFeet(bot, "bot", "after lift landing");

            // Max hold: keep holding, it must let go on its own at 4 s.
            yield return Approach(2.6f, 4f);
            yield return BackOff(2.6f, 3f);
            yield return Until(() => seven.Current == SevenFighter.Mode.Free, 3f, "free");
            seven.ScriptTkHeld = true;
            seven.DoAttack("telekinesis");
            yield return Until(() => seven.Current == SevenFighter.Mode.Lift, 3f, "lift 2 start");
            yield return Until(() => seven.Current != SevenFighter.Mode.Lift, 6f, "auto release");
            Log($"auto release after {seven.LiftSeconds:0.00} s of holding (limit {seven.MaxLiftSeconds:0.0})");
            seven.ScriptTkHeld = false;
            yield return Until(() => bot.Current == TrainingBot.Mode.Idle, 9f, "recovered after lift 2");
            yield return Until(() => seven.Current == SevenFighter.Mode.Free, 3f, "free");

            // Finisher -> knockdown -> recover.
            yield return Approach(1.4f, 5f);
            yield return new WaitForSeconds(0.5f);
            lastHit = null;
            seven.DoAttack("finisher");
            yield return Until(() => lastHit != null, 3f, "finisher hit");
            yield return Until(() => bot.Current == TrainingBot.Mode.Down && bot.Tn > 0.85f, 5f, "knocked down");
            yield return Shot("16_knockdown");
            LogFeet(seven, "7", "after finisher");
            yield return Until(() => bot.Current == TrainingBot.Mode.Idle, 9f, "recovered");
            Log("bot recovered to fight idle");
            LogFeet(seven, "7", "end");
            LogFeet(bot, "bot", "end");

            // Close-up of 7's head: forehead dot.
            cam.Hold = true;
            var head = seven.Bone(HumanBodyBones.Head);
            var camT = cam.transform;
            var face = seven.transform.forward;
            camT.position = head.position + Vector3.up * 0.05f + face * 0.75f + seven.transform.right * 0.25f;
            camT.LookAt(head.position + Vector3.up * 0.06f);
            yield return null;
            yield return Shot("17_head_front");

            LogAudio();
            Finish("done");
        }

        IEnumerator Watchdog()
        {
            yield return new WaitForSecondsRealtime(240f);
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
