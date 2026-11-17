using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Chapter 1 check run helper (-chapter1 -capture dir): survives the scene change to the saucer, renders the active
    /// camera off-screen into a RenderTexture, writes numbered PNGs + capture_log.txt (audio output level every frame,
    /// voice lines played vs. the bank, cat distance, timings), then quits. Never needs the window or focus.
    /// </summary>
    public class ChapterCapture : MonoBehaviour
    {
        public static ChapterCapture I { get; private set; }
        public static bool Active => I != null;
        RenderTexture _rt;
        Camera _cam;
        readonly StringBuilder _log = new StringBuilder();
        float _t0, _peak;
        readonly float[] _buf = new float[512];
        public float CatMaxBehind;      // max distance of the cat from 7 while following / leading (not cutscenes)
        public int Soft;                // stage timeouts (soft-lock suspicion)

        public static ChapterCapture Ensure()
        {
            if (I != null || DemoBoot.CaptureDir == null) return I;
            var go = new GameObject("ChapterCapture");
            DontDestroyOnLoad(go);
            I = go.AddComponent<ChapterCapture>();
            I._t0 = Time.realtimeSinceStartup;
            Directory.CreateDirectory(DemoBoot.CaptureDir);
            I._rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            I.Log($"chapter 1 capture start, muted={DemoBoot.Muted}, listenerVolume={AudioListener.volume}, paused={AudioListener.pause}");
            return I;
        }

        public void Attach(Camera cam)
        {
            if (_cam != null && _cam != cam) _cam.targetTexture = null;
            _cam = cam;
            _cam.targetTexture = _rt;
        }

        public void Log(string s)
        {
            var line = $"[{Time.realtimeSinceStartup - _t0,7:0.00}s] {s}";
            _log.AppendLine(line);
            Debug.Log("[Seven] " + line);
        }

        void Update()
        {
            AudioListener.GetOutputData(_buf, 0);
            foreach (var v in _buf) _peak = Mathf.Max(_peak, Mathf.Abs(v));
        }

        public float AudioPeak => _peak;

        public IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            if (_cam == null) yield break;
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            var tex = new Texture2D(_rt.width, _rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            Chapter1State.ShotCount++;
            var file = $"{Chapter1State.ShotCount:00}_{name}.png";
            File.WriteAllBytes(Path.Combine(DemoBoot.CaptureDir, file), tex.EncodeToPNG());
            Destroy(tex);
            Log($"shot {file}  audioPeak={_peak:0.000000}");
        }

        public void Finish(string summary)
        {
            Log(summary);
            Log($"audio output peak over the whole run = {_peak:0.000000} (muted={DemoBoot.Muted}, listenerVolume={AudioListener.volume})");
            File.WriteAllText(Path.Combine(DemoBoot.CaptureDir, "capture_log.txt"), _log.ToString());
            Application.Quit();
        }

        void OnApplicationQuit()
        {
            if (DemoBoot.CaptureDir != null && !File.Exists(Path.Combine(DemoBoot.CaptureDir, "capture_log.txt")))
                File.WriteAllText(Path.Combine(DemoBoot.CaptureDir, "capture_log.txt"), _log + "\n(quit before finish)\n");
        }
    }
}
