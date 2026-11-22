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

        bool _stopped;
        void Update()
        {
            if (!_stopped && DemoBoot.StopAfter > 0f && Time.time > DemoBoot.StopAfter) { _stopped = true; Finish($"stopped after {DemoBoot.StopAfter:0} s of game time (-stopafter)"); }
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
            Log($"shot {file}  audioPeak={_peak:0.000000}  cursorVisible={Cursor.visible}");
        }

        /// <summary>QF13: the same frame from a second, fixed angle (a side view) without moving the player's camera - a
        /// temporary copy of the camera rendered into its own texture.</summary>
        public IEnumerator ShotFrom(string name, Vector3 pos, Vector3 look, float fov = 40f)
        {
            yield return new WaitForEndOfFrame();
            if (_cam == null) yield break;
            var go = new GameObject("QF13_SideCam"); var c = go.AddComponent<Camera>(); c.CopyFrom(_cam); c.enabled = false;
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos)); c.fieldOfView = fov;
            var src = _cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            var dst = c.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() ?? go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (src != null) { dst.renderPostProcessing = src.renderPostProcessing; dst.antialiasing = src.antialiasing; }
            var rt = new RenderTexture(_rt.width, _rt.height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var req = new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = rt };
            if (UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(c, req)) UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(c, req);
            else { c.targetTexture = rt; c.Render(); c.targetTexture = null; }
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            Chapter1State.ShotCount++;
            var file = $"{Chapter1State.ShotCount:00}_{name}.png";
            File.WriteAllBytes(Path.Combine(DemoBoot.CaptureDir, file), tex.EncodeToPNG());
            Destroy(tex); rt.Release(); Destroy(rt); Destroy(go);
            Log($"shot {file} (second angle)");
        }

        public void Finish(string summary)
        {
            Log(ChapterCat.SpeedReport());              // QF11: never the walk cycle on the spot
            if (ChapterCamera.RideFrames > 0) Log(ChapterCamera.RideSummary());   // QF15 (1)
            if (HoverBike.SwimFrames > 0) Log(HoverBike.InsideSummary());         // QF15 (2)
            if (HoverBike.LandFrames > 0) Log(HoverBike.LandSummary());           // QF18 (10)
            if (FeetProbe.Frames > 0) Log(FeetProbe.Summary());                   // QF18 (16)
            Log($"collision on foot (QF18 10): she slid along walls / props {ChapterSeven.FootSlides} times");
            Log(DialogueFraming.Summary());                                        // QF18 (7)
            Log(ChapterCamera.ShotSummary());                                      // QF21 (6)
            if (MachinistRig.Steps > 0 || MachinistRig.Servos > 0) Log($"MACHINIST STEPS (QF21 5): {MachinistRig.Steps} heavy footfalls (four takes, never twice in a row), servo whirs {MachinistRig.Servos}");
            if (AsteroidDirector.FramedLines > 0) Log($"MACHINIST FRAMING (QF21 7): {AsteroidDirector.FramedLines} lines framed - wide two-shots {AsteroidDirector.FramedWide}, low angles on him {AsteroidDirector.FramedHim}, her from his side {AsteroidDirector.FramedHer}; tracking shots {AsteroidDirector.Tracks}; worst armour share with her hidden {AsteroidDirector.ArmourWorst * 100f:0} % (must be 0)");
            if (ChapterSeven.BladeFrames > 0) Log(ChapterSeven.BladeSummary());   // QF21 (9)
            if (TkAudit.Done > 0) Log(TkAudit.Summary());                          // QF24 (14)
            DialoguePlayer.ReportOverlaps("the whole run");                        // QF24 (3)
            MusicSystem.Report();                                                  // QF24 (21)
            if (RidePose.Checks > 0) Log(RidePose.Summary());                      // QF24 (15)
            Log($"POST FX (QF24 24): close-up depth of field found still on when control returned {CloseUpFocus.Releases}x - each released within a frame (longest {CloseUpFocus.LongestOnWithControl * 1000f:0} ms)");
            if (MothershipInterior.Shown > 0 || RecordDisplay.Built > 0) Log($"QF24 (2/4): record displays built {RecordDisplay.Built}, the mothership's interior shown {MothershipInterior.Shown}x");
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
