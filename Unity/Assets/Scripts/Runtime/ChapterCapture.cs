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
            I._rt = ImageGate.NewRT();   // QF26 (2): HDR like the screen (an ARGB32 target made URP render the whole frame in LDR)
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
            if (!_stopped && DemoBoot.StopAfter > 0f && Time.time > DemoBoot.StopAfter && !ClipRecorder.Recording) { _stopped = true; Finish($"stopped after {DemoBoot.StopAfter:0} s of game time (-stopafter)"); }
            if (_cam != null) WatchControl();
            ClipRecorder.Tick();                                                   // QF34 (2): memories at a game time
            AudioListener.GetOutputData(_buf, 0);
            foreach (var v in _buf) _peak = Mathf.Max(_peak, Mathf.Abs(v));
        }

        public float AudioPeak => _peak;

        public IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            if (_cam == null) yield break;
            Chapter1State.ShotCount++;
            var file = $"{Chapter1State.ShotCount:00}_{name}.png";
            QF30Watch.BeforeShot(file);                                           // QF30 (4): limbs through props
            FrameProbe.CaptureFrame = Time.frameCount;                            // P1 (4): a capture frame is not a hitch
            var gate = ImageGate.Save(_rt, _cam, DemoBoot.CaptureDir, file);   // QF26 (2): every capture is gated
            Log($"shot {file}  audioPeak={_peak:0.000000}  cursorVisible={Cursor.visible}");
            Log(gate);
            ClipRecorder.OnShot(this, _cam, name);                                // QF31: raw memory clips / the "Received" stills
        }

        // ---- QF26 (1): control came back -> one gated frame a moment later (the white-out stayed on after control returned and
        // QF24's POST FX check only looked at the depth-of-field switch, never at the picture)
        bool _hadControl; float _noControlFor, _ctlAt = -1f; int _ctlShots;
        static float _holdCtlUntil;
        /// <summary>QF30: a check-run test stages a view (the bike turned away from the light) - the control-back frame waits until it is over.</summary>
        public static void HoldControlShot(float seconds) { _holdCtlUntil = Time.unscaledTime + seconds; }
        float _ctlWait; HoverBike _fastBike;
        bool FastRide() { foreach (var b in FindObjectsByType<HoverBike>(FindObjectsSortMode.None)) if (b.Riding && Mathf.Abs(b.Speed) > 15f) return true; return false; }
        Vector3 _ctlCamP; Quaternion _ctlCamR = Quaternion.identity; int _ctlStill; float _ctlSettle;
        void WatchControl()
        {
            bool control = false; try { control = MusicSystem.HasControl != null && MusicSystem.HasControl(); } catch { }
            if (!control) { _noControlFor += Time.unscaledDeltaTime; _hadControl = false; return; }
            if (!_hadControl && _noControlFor > 1.0f && _ctlShots < 40) { _ctlAt = Time.unscaledTime + 0.8f; _ctlWait = 0f; }
            _hadControl = true; _noControlFor = 0f;
            if (_ctlAt > 0f && Time.unscaledTime >= _ctlAt && ChapterUI.Live != null && ChapterUI.Live.FadeAlpha > 0.05f) _ctlAt = Time.unscaledTime + 0.3f;   // still fading in: the frame the player first sees clearly
            // QF28 pass: never a capture (a long frame) while she rides fast - the check run's own hitch drove the Europa bike into the
            // ice passage's bend; the frame waits (up to 6 s) until she is under 15 m/s
            if (_ctlAt > 0f && Time.unscaledTime >= _ctlAt && _ctlWait < 6f && FastRide()) { _ctlAt = Time.unscaledTime + 0.3f; _ctlWait += 0.3f; }
            if (_ctlAt > 0f && Time.unscaledTime >= _ctlAt && Time.unscaledTime < _holdCtlUntil) _ctlAt = _holdCtlUntil + 0.05f;   // a staged check-run test owns the view
            // QF32 (3): repeatability - the frame is taken once the follow camera has SETTLED (still for 6 frames: < 2 cm, < 0.3 deg a frame),
            // at most 1.5 s more; before, it could catch the camera mid-ease out of the cutscene (Biaveh's control-back frame: range 0.30
            // in one run, 0.48 in the next)
            var cm = Camera.main;
            if (cm != null)
            {
                bool still = (cm.transform.position - _ctlCamP).sqrMagnitude < 0.0004f && Quaternion.Angle(cm.transform.rotation, _ctlCamR) < 0.3f;
                _ctlStill = still ? _ctlStill + 1 : 0; _ctlCamP = cm.transform.position; _ctlCamR = cm.transform.rotation;
            }
            if (_ctlAt > 0f && Time.unscaledTime >= _ctlAt && _ctlStill < 6 && _ctlSettle < 1.5f) { _ctlSettle += Time.unscaledDeltaTime; return; }
            if (_ctlAt > 0f && Time.unscaledTime >= _ctlAt) {
                if (_ctlSettle > 0f) Log($"control back: frame taken after the camera settled ({_ctlSettle:0.00} s, still {_ctlStill} frames)");
                _ctlSettle = 0f; _ctlAt = -1f; _ctlShots++; var rb = HoverBike.RiddenNow; if (rb != null) ImageGate.Expect(rb.transform, "7 on her bike (the player's view while riding)", 0f, "ctl_control_back"); StartCoroutine(Shot($"ctl_control_back_{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}")); }
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
            FrameProbe.CaptureFrame = Time.frameCount;
            var rt = ImageGate.NewRT(_rt.width, _rt.height);
            var req = new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = rt };
            if (UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(c, req)) UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(c, req);
            else { c.targetTexture = rt; c.Render(); c.targetTexture = null; }
            Chapter1State.ShotCount++;
            var file = $"{Chapter1State.ShotCount:00}_{name}.png";
            var gate = ImageGate.Save(rt, c, DemoBoot.CaptureDir, file);       // QF26 (2)
            rt.Release(); Destroy(rt); Destroy(go);
            Log($"shot {file} (second angle)");
            Log(gate);
        }

        public void Finish(string summary)
        {
            if (WholeGameRun.Intercept(summary)) return;      // QF38: a chained whole-game run goes on to the next piece
            FrameProbe.Final();                         // P1 (4): real frame time in the last scene
            Log(ChapterCat.SpeedReport());              // QF11: never the walk cycle on the spot
            if (ChapterCamera.RideFrames > 0) Log(ChapterCamera.RideSummary());   // QF15 (1)
            if (HoverBike.SwimFrames > 0) Log(HoverBike.InsideSummary());         // QF15 (2)
            if (HoverBike.LandFrames > 0) Log(HoverBike.LandSummary());           // QF18 (10)
            if (FeetProbe.Frames > 0) Log(FeetProbe.Summary());                   // QF18 (16)
            Log($"collision on foot (QF18 10): she slid along walls / props {ChapterSeven.FootSlides} times");
            Log(DialogueFraming.Summary());                                        // QF18 (7)
            if (SaucerOutside.Audits > 0) Log(SaucerOutside.AuditSummary());       // QF38 (7)
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
            if (HandHold.Frames > 0) Log(HandHold.Summary());                       // QF28 (5)
            if (FurContact.Any) Log(FurContact.Summary());                         // P2 (8)
            if (FurPalm.MotionFrames > 0) Log(FurPalm.MotionSummary());            // QF34 (1)
            Log(HandAudit.Summary());                                              // QF38 (1)
            if (TunnelerBody.Frames > 0 || TunnelerBody.Lines > 0) Log(TunnelerBody.Summary());   // P2 (9)
            if (ScreeRig.CalmFrames > 0) Log(ScreeRig.Summary());                   // P2 (10)
            Log($"RIDE ROOT (P2 1): body placed on the saddle in the IK pass {ChapterSeven.BodyPlaced} frames; frames the hips were still > 0.3 m off the saddle after it {ChapterSeven.RideBigErr} (never folded into her root); root offset clamps {ChapterSeven.RideOffsetClamps}");
            Log(QF30Watch.Summary());                                              // QF30 (3/4/9/14/17/22)
            Log(ImageGate.Summary(DemoBoot.CaptureDir));                          // QF26 (2)
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
