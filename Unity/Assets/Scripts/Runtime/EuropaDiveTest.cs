using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF29 (1): can a PLAYER get stuck at the start of Europa's ice passage? (-europadivetest -capture dir; starts at the dive.)
    /// The story flow is stopped once she is on the bike at "Dive through the passage in the ice"; then many attempts, each from a
    /// different spot on the shelf round the passage's mouth (near / far, straight on / from the side / from behind), at a different
    /// speed, with a different player habit - follows the white light (dives when it is below), dives at once (into the shelf), stays
    /// level until over the hole, half throttle, brakes and restarts - and some at a low frame rate (long frames: 15 and 20 fps, a
    /// slow PC or a Steam Deck under load). The bike is driven by the same inputs a pad gives (steer, throttle, pitch); success = it
    /// passes the passage's fourth point within 40 s. Ends "DIVE TEST (QF29 1): N attempts, stuck S" with every stuck spot listed.
    /// </summary>
    public class EuropaDiveTest : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (DemoBoot.CaptureDir == null || !DemoBoot.EuropaDiveTest || FindAnyObjectByType<EuropaDiveTest>() != null) return;
            var go = new GameObject("EuropaDiveTest"); DontDestroyOnLoad(go); go.AddComponent<EuropaDiveTest>();
        }
        ChapterCapture Cap => ChapterCapture.I;
        void Log(string s) => Cap?.Log("dive test: " + s);
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        IEnumerator Start()
        {
            ChapterCapture.Ensure();
            float t0 = Time.time;
            while (EuropaDirector.I == null || !EuropaDirector.I.Riding || EuropaDirector.I.InCutscene || EuropaDirector.I.ObjectiveNow == null || !(EuropaDirector.I.ObjectiveNow.Contains("passage") || EuropaDirector.I.ObjectiveNow.StartsWith("Dive")))
            { if (Time.time - t0 > 240f) { Cap.Finish("DIVE TEST (QF29 1): never reached the dive"); yield break; } yield return null; }
            var d = EuropaDirector.I; var bike = d.Bike;
            yield return new WaitForSeconds(0.3f);
            d.StopAllCoroutines();
            bike.ScriptDriven = true;
            var pts = EuropaLayout.Dive; var mouth = pts[0]; var axis = Flat(pts[1] - pts[0]).normalized;
            string[] habits = { "follows the light", "dives at once", "level until over the hole", "half throttle", "brakes and restarts" };
            int[] fpsSet = { 0, 0, 20, 15 };
            var rnd = new System.Random(29);
            int attempts = 0, stuck = 0; var list = new List<string>(); float worst = 0f;
            for (int i = 0; i < 60; i++)
            {
                string habit = habits[i % habits.Length];
                int fps = fpsSet[(i / habits.Length) % fpsSet.Length];
                float ang = -150f + 300f * (float)rnd.NextDouble(), dist = 3f + 11f * (float)rnd.NextDouble();
                var from = mouth - Quaternion.Euler(0f, ang, 0f) * axis * dist; from.y = bike.transform.position.y;
                if (!EuropaLayout.InSwim(from, 1.0f) && !EuropaLayout.InSwim(from + Vector3.up * 0.5f, 0.5f)) { from = mouth - axis * dist; }
                float yaw = Mathf.Atan2((mouth - from).x, (mouth - from).z) * Mathf.Rad2Deg + (float)(rnd.NextDouble() * 120.0 - 60.0);
                bike.Place(from, yaw);
                Time.captureDeltaTime = fps > 0 ? 1f / fps : (DemoBoot.NoDeterministic ? 0f : DemoBoot.CaptureStep);
                float start = Time.time, lastProg = Time.time, best = 1e9f; int k = 1; bool ok = false; string why = "";
                while (Time.time - start < 40f)
                {
                    var p = bike.transform.position;
                    for (int j = pts.Length - 1; j >= k; j--) if (Vector3.Distance(p, pts[j]) < 6f) { k = j + 1; break; }
                    if (k >= 4) { ok = true; break; }
                    var target = pts[Mathf.Min(k, pts.Length - 1)];
                    var to = target - p; var f = Flat(to);
                    float err = Vector3.SignedAngle(Flat(bike.transform.forward), f, Vector3.up);
                    float steer = Mathf.Clamp(err / 35f, -1f, 1f), thr = 1f, rise = Mathf.Clamp(to.y / 3f, -1f, 1f);
                    switch (habit)
                    {
                        case "dives at once": rise = -1f; break;
                        case "level until over the hole": rise = Flat(p - mouth).magnitude < 3f || p.y < EuropaLayout.WaterY ? Mathf.Clamp(to.y / 3f, -1f, 1f) : 0f; break;
                        case "half throttle": thr = 0.5f; break;
                        case "brakes and restarts": thr = ((int)((Time.time - start) / 1.5f)) % 2 == 0 ? 1f : -1f; break;
                    }
                    if (Mathf.Abs(err) > 75f) thr = Mathf.Min(thr, 0.3f);
                    bike.ScriptSteer = steer; bike.ScriptThrottle = thr; bike.ScriptRise = rise;
                    float dleft = Vector3.Distance(p, pts[3]);
                    if (dleft < best - 0.5f) { best = dleft; lastProg = Time.time; }
                    if (Time.time - lastProg > 12f) { why = $"no progress for 12 s at {p:0.0} (speed {bike.Speed:0.0}, stuck {bike.Stuck}, sliding along '{bike.LastSlide}')"; break; }
                    yield return null;
                }
                Time.captureDeltaTime = DemoBoot.NoDeterministic ? 0f : DemoBoot.CaptureStep;
                attempts++;
                if (!ok) { stuck++; if (why == "") why = $"40 s, still {best:0.0} m short at {bike.transform.position:0.0}"; list.Add($"#{i} {habit}, {(fps > 0 ? fps + " fps" : "normal fps")}, from {from:0.0} ({ang:0} deg, {dist:0.0} m), yaw {yaw:0}: {why}"); Log("STUCK " + list[list.Count - 1]); }
                else { float tt = Time.time - start; worst = Mathf.Max(worst, tt); Log($"#{i} {habit}, {(fps > 0 ? fps + " fps" : "normal fps")}, from {ang:0} deg / {dist:0.0} m: through in {tt:0.0} s"); }
            }
            bike.ScriptSteer = 0f; bike.ScriptThrottle = 0f; bike.ScriptRise = 0f;
            Cap.Finish($"DIVE TEST (QF29 1): {attempts} attempts (5 habits, normal / 20 / 15 fps, every side of the mouth), stuck {stuck}, slowest {worst:0.0} s" + (stuck > 0 ? " - " + string.Join(" | ", list) : ""));
        }
    }
}
