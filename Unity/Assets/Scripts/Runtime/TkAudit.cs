using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF24 (14) - Dan: "telekinesis on the floor coupling does nothing visible" (the mothership's airlock: she did the pose,
    /// the step completed, the coupling never moved - it was baked into the passage's static mesh). Every telekinesis
    /// interaction in every chapter now reports whether the VISIBLE object actually moved: at the moment her telekinesis lands,
    /// the object it acts on (given, or the small renderers right at the target) is recorded; a few seconds later its position /
    /// rotation are compared. Check runs log <c>TK MOVED (QF24 14)</c> per interaction and a summary
    /// <c>TK AUDIT (QF24 14)</c>: completed without moving must be 0.
    /// </summary>
    public static class TkAudit
    {
        public static int Done, Moved, Still;
        static readonly List<string> _still = new List<string>(), _all = new List<string>();
        static void Log(string s) { Debug.Log("[Seven] " + s); if (DemoBoot.CaptureDir != null) ChapterCapture.I?.Log(s); }

        /// <summary>Watch what her telekinesis acts on (<paramref name="obj"/>, or the small renderers within 2.5 m of the target).</summary>
        public static void Watch(MonoBehaviour host, string what, Vector3 target, Transform obj = null, float window = 6f)
        {
            if (host == null) return;
            host.StartCoroutine(Run(what, target, obj, window));
        }
        static IEnumerator Run(string what, Vector3 target, Transform obj, float window)
        {
            var list = new List<Transform>();
            if (obj != null) list.Add(obj);
            else
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if (r == null || !r.enabled || r is ParticleSystemRenderer) continue;
                    var b = r.bounds; if (b.size.magnitude > 9f) continue;
                    if ((b.ClosestPoint(target) - target).magnitude > 2.5f) continue;
                    if (r.GetComponentInParent<ChapterSeven>() != null || r.name.Contains("Guide") || r.name.Contains("Marker") || r.name.Contains("Beacon")) continue;   // never the guide light / markers
                    list.Add(r.transform); if (list.Count > 24) break;
                }
            var p0 = new List<Vector3>(); var q0 = new List<Quaternion>();
            foreach (var t in list) { p0.Add(t.position); q0.Add(t.rotation); }
            float best = 0f, bestAng = 0f; string who = list.Count > 0 && list[0] != null ? list[0].name : "nothing";
            for (float t = 0f; t < window; t += Time.deltaTime)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == null) continue;
                    float d = (list[i].position - p0[i]).magnitude, a = Quaternion.Angle(list[i].rotation, q0[i]);
                    if (d > best || a > bestAng) { if (d > best) best = d; if (a > bestAng) bestAng = a; who = list[i].name; }
                }
                yield return null;
            }
            Done++;
            bool moved = best > 0.05f || bestAng > 3f;
            if (moved) Moved++; else { Still++; _still.Add(what); }
            var line = $"TK MOVED (QF24 14): '{what}' - {(moved ? $"'{who}' moved {best:0.00} m / turned {bestAng:0} deg" : $"NOTHING VISIBLE MOVED ({list.Count} objects watched at the target)")}";
            _all.Add(line); Log(line);
        }
        /// <summary>The summary (end of a check run / a scene).</summary>
        public static string Summary() => $"TK AUDIT (QF24 14): {Done} telekinesis interactions, the visible object moved in {Moved}, completed without moving {Still} (must be 0){(_still.Count > 0 ? " - " + string.Join(", ", _still) : "")}";
    }
}
