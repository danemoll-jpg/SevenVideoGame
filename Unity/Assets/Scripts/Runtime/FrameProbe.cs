using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Polish pass P1 (4): real frame time per scene in check runs - every rendered frame's real duration (unscaled), the frames that
    /// carried a capture (PNG / JPG writes, their own stall) left out. Logged on every scene change and at the end:
    /// `FRAME TIME (P1 4) &lt;scene&gt;: frames N, median x ms, p95 y ms, worst z ms, hitches over 50 ms H (where: ...)`.
    /// Check runs are minimized at Below Normal priority with captures in between, so the numbers are an upper bound of a real player's.
    /// </summary>
    public class FrameProbe : MonoBehaviour
    {
        static FrameProbe _i;
        readonly List<float> _ms = new List<float>(); readonly List<string> _hitches = new List<string>();
        string _scene; float _last; int _skipUntil;
        public static int CaptureFrame;     // ChapterCapture sets it when it writes a file (that frame and the next are left out)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (DemoBoot.CaptureDir == null || _i != null) return;
            var go = new GameObject("FrameProbe"); DontDestroyOnLoad(go); _i = go.AddComponent<FrameProbe>();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => { if (_i != null) { _i.Report(); _i._scene = s.name; _i._skipUntil = Time.frameCount + 30; } };
            _i._scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name; _i._skipUntil = Time.frameCount + 30;
        }
        void Update()
        {
            float now = Time.realtimeSinceStartup, dt = (now - _last) * 1000f; _last = now;
            if (Time.frameCount < _skipUntil || Time.frameCount <= CaptureFrame + 1 || ClipRecorder.Recording || dt <= 0f || dt > 5000f) return;
            _ms.Add(dt);
            if (dt > 50f && _hitches.Count < 12) { var s7 = FindFirstObjectByType<ChapterSeven>(); _hitches.Add($"{dt:0} ms at {Time.time:0}s{(s7 != null ? $" near ({s7.transform.position.x:0},{s7.transform.position.z:0})" : "")}"); }
        }
        void Report()
        {
            if (_ms.Count < 30) { _ms.Clear(); _hitches.Clear(); return; }
            var s = _ms.OrderBy(x => x).ToList(); float med = s[s.Count / 2], p95 = s[(int)(s.Count * 0.95f)], max = s[s.Count - 1];
            int over = s.Count(x => x > 50f);
            ChapterCapture.I?.Log($"FRAME TIME (P1 4) {_scene}: frames {s.Count}, median {med:0.0} ms, p95 {p95:0.0} ms, worst {max:0} ms, hitches over 50 ms {over}{(_hitches.Count > 0 ? " (" + string.Join("; ", _hitches) + ")" : "")}");
            _ms.Clear(); _hitches.Clear();
        }
        public static void Final() { if (_i != null) _i.Report(); }
    }
}
