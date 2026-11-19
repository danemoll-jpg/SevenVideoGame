using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Seven
{
    /// <summary>
    /// Round 12 (B11): the player-style run of Venus part 2 (-venusplay2, with -capture; add -venushintfast and -testspeed 2)
    /// on a virtual gamepad driving the real input actions - no autopilot, no teleports. It starts like the Scene Select
    /// entry "the way down + the control chamber" (the observation area after C05) and plays on to the saucer's destination
    /// screen: the way down on foot, the encounter (it watches the creature troughs - the dust and the creatures are what a
    /// player sees - and crosses when one has passed; X at the north device, the crystal with RT, climbs to the ledge with
    /// A), the console, the ride back, the guardian, the record (it skips the vision with Y, so the skip path is tested),
    /// the ride to the ship, the debrief aboard, Set course. Lines are skipped with Y like a fast reader.
    /// Ends "VENUS PLAYTEST 2: 1/1, soft-locks N, knockdowns K, hints H, skips S".
    /// </summary>
    public class VenusPlaytest2 : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (DemoBoot.CaptureDir == null || !DemoBoot.VenusPlay2 || FindAnyObjectByType<VenusPlaytest2>() != null) return;
            var go = new GameObject("VenusPlaytest2"); DontDestroyOnLoad(go); go.AddComponent<VenusPlaytest2>();
        }

        Gamepad _pad; Vector2 _stick; float _rt;
        readonly Queue<GamepadButton> _presses = new Queue<GamepadButton>(); GamepadButton _cur; int _phase;
        const GamepadButton X = GamepadButton.West, Y = GamepadButton.North, A = GamepadButton.South, B = GamepadButton.East;
        void Press(GamepadButton b) => _presses.Enqueue(b);
        bool Idle => _phase == 0 && _presses.Count == 0;

        void Update()
        {
            if (_pad == null) return;
            if (Gamepad.current != _pad) _pad.MakeCurrent();
            if (_phase == 0 && _presses.Count > 0) { _cur = _presses.Dequeue(); _phase = 1; }
            uint bits = 0u; float rt = _rt;
            if (_phase >= 1 && _phase <= 3) { if (_cur == GamepadButton.RightTrigger) rt = 1f; else bits |= 1u << (int)_cur; }
            if (_phase > 0) _phase = _phase >= 6 ? 0 : _phase + 1;
            InputSystem.QueueStateEvent(_pad, new GamepadState { leftStick = _stick, rightTrigger = rt, buttons = bits });
        }

        static VenusDirector D => VenusDirector.I != null && VenusDirector.I.isActiveAndEnabled ? VenusDirector.I : null;
        static Chapter1Aboard S => Chapter1Aboard.I != null && Chapter1Aboard.I.isActiveAndEnabled && Chapter1Aboard.I.VenusMode ? Chapter1Aboard.I : null;
        static Vector3 P => D != null && D.Seven != null ? D.Seven.transform.position : Vector3.zero;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        string Obj => D != null ? D.ObjectiveNow ?? "" : "";
        ChapterCapture Cap => ChapterCapture.I;
        void Log(string s) => Cap?.Log("venus playtest 2: " + s);
        int _soft; string _lastObj; float _objSince;

        Vector2 StickFor(Vector3 dir)
        {
            var cam = Camera.main != null ? Camera.main.transform : null; if (cam == null) return Vector2.zero;
            var f = Flat(cam.forward).normalized; var r = new Vector3(f.z, 0f, -f.x);
            dir = Flat(dir); if (dir.sqrMagnitude > 1f) dir.Normalize();
            return new Vector2(Vector3.Dot(dir, r), Vector3.Dot(dir, f));
        }

        void OnEnable() => UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnLoaded;
        void OnDisable() => UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnLoaded;
        bool _aboard;
        void OnLoaded(UnityEngine.SceneManagement.Scene sc, UnityEngine.SceneManagement.LoadSceneMode m)
        {
            if (sc.name != "Saucer" || _aboard) return;
            _aboard = true; StopAllCoroutines(); _stick = Vector2.zero; _presses.Clear();
            StartCoroutine(SaucerPhase());
        }
        float _t0;
        IEnumerator SaucerPhase()
        {
            Log("the Saucer scene loaded - the debrief (C11), then Set course");
            for (float t = 0f; t < 10f && S == null; t += Time.deltaTime) yield return null;
            var s = S;
            if (s == null) { Log("no Venus-mode galley found"); Cap.Finish("VENUS PLAYTEST 2: 0/1 (saucer phase did not start)"); yield break; }
            while (!s.Explored) { if (s.Dlg != null && s.Dlg.Busy && Idle && s.UI.SubtitleVisible) Press(Y); yield return WaitR(0.3f); }
            Log("aboard: free exploration - walking to her galley seat for Set course");
            var seven = s.Director.Seven.transform;
            for (float t = 0f; t < 20f && Flat(s.SeatStandPos - seven.position).magnitude > 0.5f; t += Time.deltaTime) { _stick = StickFor((s.SeatStandPos - seven.position).normalized); yield return null; }
            _stick = Vector2.zero; yield return WaitR(0.6f);
            Log($"at the seat: prompt '{s.Director.ExtraPrompt}' - pressing X");
            for (int k = 0; k < 3 && !s.DestinationsShown; k++) { Press(X); yield return WaitR(3f); }
            Log($"route done in {Time.time - _t0:0} s game time; destination screen shown: {s.DestinationsShown}");
            if (s.Dest != null && s.Dest.Open) { yield return WaitR(1f); Press(B); yield return WaitR(2f); }
            Cap.Finish($"VENUS PLAYTEST 2: {(_soft == 0 && s.DestinationsShown ? 1 : 0)}/1, soft-locks {_soft}, knockdowns {VenusDirector.LastKnockdowns}, hints {VenusDirector.LastHints}, skips {VenusDirector.LastSkips}, {VenusState.Summary()}");
        }

        IEnumerator Start()
        {
            ChapterCapture.Ensure();
            _pad = InputSystem.AddDevice<Gamepad>("VenusPlayPad2"); _pad.MakeCurrent();
            Log($"virtual gamepad on the real input actions; muted={DemoBoot.Muted} listenerVolume={AudioListener.volume}; starts at the Scene Select entry 'the way down + the control chamber'");
            while (D == null) yield return null;
            yield return null;
            float t0 = Time.time; _t0 = t0;
            bool skippedVision = false;
            while (S == null || !S.DestinationsShown)
            {
                if (_aboard) yield break;
                if (D == null || D.Seven == null || D.Dlg == null || D.Bike == null) { _stick = Vector2.zero; yield return null; continue; }
                if (Obj != _lastObj) { _lastObj = Obj; _objSince = Time.time; Log($"objective: '{Obj}' at {P:0.0}"); }
                if (Time.time - _objSince > 240f) { _soft++; Log($"SOFT-LOCK: '{Obj}' unchanged for 240 s at {P:0.0}"); break; }
                if (D.UI.VideoAlpha > 0.5f && !skippedVision) { skippedVision = true; _stick = Vector2.zero; yield return WaitR(7f); Log("the vision plays - skipping it (Y)"); Press(Y); yield return WaitR(1.5f); continue; }
                if (D.Dlg.Busy) { _stick = Vector2.zero; if (Idle && D.UI.SubtitleVisible) Press(Y); yield return WaitR(0.35f); continue; }
                var s7 = D.Seven; var bike = D.Bike;
                switch (true)
                {
                    case true when Obj.StartsWith("Find the source of the signal"):
                        foreach (var p in new[] { new Vector3(69f, -6f, 137.5f), new Vector3(69f, -6f, 132f), new Vector3(69f, -14f, 117f), new Vector3(69f, -18f, 106f), new Vector3(69f, -22f, 95f), new Vector3(71f, -22f, 89f), new Vector3(81f, -22f, 89f), new Vector3(97f, -27f, 89f), new Vector3(106f, -30f, 89f) })
                            { yield return WalkTo(p, 0.9f); if (!Obj.StartsWith("Find")) break; }
                        break;
                    case true when Obj.StartsWith("Disable the control devices"):
                        yield return Devices(); break;
                    case true when Obj.StartsWith("Close the containment barrier"):
                        yield return WalkTo(VenusLayout.ConsoleAt - Vector3.right * 1.3f, 0.5f);
                        if (D.Legend.ContextText != null && D.Legend.ContextText.Contains("barrier")) { Press(X); yield return WaitR(1.0f); }
                        break;
                    case true when Obj.StartsWith("Return to the guardian"):
                        if (!bike.Riding) { yield return WalkTo(bike.transform.position - bike.transform.right * 1.35f, 1.2f); if (D.Legend.ContextText != null && D.Legend.ContextText.Contains("Ride")) { Press(X); yield return WaitR(0.8f); } }
                        else yield return RidePath(new[] { new Vector3(76.5f, -6f, 152f), new Vector3(76.5f, -3f, 175f), new Vector3(76f, 0f, 199f), new Vector3(52f, 0f, 199f), new Vector3(38f, 0f, 199f) });
                        break;
                    case true when Obj.StartsWith("Speak with the guardian"):
                        if (bike.Riding) { _stick = Vector2.zero; if (bike.Speed < 1f) { Press(X); yield return WaitR(0.8f); } break; }
                        yield return WalkTo(VenusLayout.MeetSpot - Vector3.forward * 1.2f, 1.0f);
                        if (D.Legend.ContextText != null && D.Legend.ContextText.Contains("Speak")) { Press(X); yield return WaitR(1.0f); }
                        break;
                    case true when Obj.StartsWith("Follow the guardian to the cube"):
                        if (P.z < 205f) yield return WalkTo(new Vector3(30f, 0f, 214f), 1f);
                        yield return WalkTo(new Vector3(30f, 0f, 240f), 1f); yield return WalkTo(new Vector3(30f, 0f, 262f), 1f);
                        yield return WalkTo(VenusLayout.CubeAt + new Vector3(0f, -VenusLayout.CubeAt.y, -2.3f), 0.4f);
                        if (D.Legend.ContextText != null && D.Legend.ContextText.Contains("record")) { Press(X); yield return WaitR(1.0f); }
                        break;
                    case true when Obj.StartsWith("Return to the saucer"):
                        if (!bike.Riding)
                        {
                            if (P.z > 236f) { yield return WalkTo(new Vector3(30f, 0f, 244f), 1f); yield return WalkTo(new Vector3(30f, 0f, 214f), 1f); }
                            yield return WalkTo(bike.transform.position - bike.transform.right * 1.35f, 1.2f);
                            if (D.Legend.ContextText != null && D.Legend.ContextText.Contains("Ride")) { Press(X); yield return WaitR(0.8f); }
                        }
                        else RideDownRoute();
                        break;
                    default: _stick = Vector2.zero; break;
                }
                yield return null;
            }
            _stick = Vector2.zero;
            yield return WaitR(1f);
            Log($"route done in {Time.time - t0:0} s game time; destination screen shown: {(S != null && S.DestinationsShown)}");
            if (S != null && S.Dest != null && S.Dest.Open) { Press(B); yield return WaitR(1.5f); }
            Cap.Finish($"VENUS PLAYTEST 2: {(_soft == 0 && S != null && S.DestinationsShown ? 1 : 0)}/1, soft-locks {_soft}, knockdowns {VenusDirector.LastKnockdowns}, hints {VenusDirector.LastHints}, skips {VenusDirector.LastSkips}, {VenusState.Summary()}");
        }

        IEnumerator WaitR(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }

        IEnumerator WalkTo(Vector3 to, float tol)
        {
            string o0 = Obj;
            for (float t = 0f; t < 25f && D != null && Flat(to - P).magnitude > tol && !D.Dlg.Busy && Obj == o0; t += Time.deltaTime)
            {
                if (D.Seven.Current != ChapterSeven.Mode.Free) { _stick = Vector2.zero; yield return null; continue; }
                _stick = StickFor((to - P).normalized); yield return null;
            }
            _stick = Vector2.zero;
        }

        /// <summary>Wait at a trough's edge until the creatures in it have passed the crossing (dust / the creature itself
        /// is what a player watches), then cross.</summary>
        IEnumerator CrossTrough(Vector3 before, Vector3 after, string group)
        {
            yield return WalkTo(before, 0.4f);
            var cross = (before + after) * 0.5f;
            for (float t = 0f; t < 25f; t += Time.deltaTime)
            {
                bool clear = true;
                foreach (var w in D.ChamberWorms)
                    if (w != null && w.gameObject.activeSelf && w.Group == group)
                    {
                        float d = Flat(w.transform.position - cross).magnitude; bool coming = Vector3.Dot(Flat(cross - w.transform.position), w.Heading) > 0f;
                        if (d < 4.5f || coming && d < 12f) clear = false;
                    }
                if (clear) break;
                _stick = Vector2.zero; yield return null;
            }
            yield return WalkTo(after, 0.5f);
        }

        IEnumerator Devices()
        {
            float y = VenusLayout.ControlY;
            var dev = D.Devices;
            if (dev[0].On)
            {
                yield return CrossTrough(new Vector3(112f, y, 94f), new Vector3(112f, y, 100.5f), "dev_north");
                yield return WalkTo(VenusLayout.DeviceUse[0], 0.4f);
                if (D.Legend.ContextText != null && D.Legend.ContextText.Contains("Shut")) { Press(X); yield return WaitR(2.5f); }
                yield return CrossTrough(new Vector3(112f, y, 100.5f), new Vector3(112f, y, 92f), "dev_north");
                yield break;
            }
            if (dev[2].On)
            {
                if (P.z > 82f) yield return CrossTrough(new Vector3(114f, y, 83f), new Vector3(114.5f, y, 75.5f), "dev_crystal");
                yield return WalkTo(VenusLayout.DeviceUse[2], 0.4f);
                for (float t = 0f; t < 0.5f; t += Time.deltaTime) { _stick = StickFor((VenusLayout.DeviceAt[2] - P).normalized) * 0.35f; yield return null; }
                _stick = Vector2.zero; yield return WaitR(0.3f);
                if (D.Legend.ContextText != null && D.Legend.ContextText.Contains("crystal")) { Press(GamepadButton.RightTrigger); yield return WaitR(4f); }
                else { Log($"no crystal prompt at {P:0.0} ('{D.Legend.ContextText}') - stepping closer"); yield return WalkTo(VenusLayout.DeviceUse[2] + new Vector3(2.5f, 0f, 0.5f), 0.4f); }
                yield break;
            }
            if (dev[1].On)
            {
                if (P.z > 82f) yield return CrossTrough(new Vector3(110f, y, 83f), new Vector3(110f, y, 75.5f), "dev_crystal");
                yield return WalkTo(new Vector3(109.5f, y, 72.2f), 0.25f);
                _stick = StickFor(Vector3.back) * 0.2f; yield return WaitR(0.35f); _stick = Vector2.zero;
                string ctx = D.Legend.ContextText; Press(A); yield return WaitR(2.2f);
                Log($"climb to the ledge: prompt '{ctx}', up: {(P.y > VenusLayout.LedgeY - 0.2f ? "ok" : "FAILED")} ({P:0.00})");
                yield return WalkTo(VenusLayout.DeviceUse[1], 0.4f);
                if (D.Legend.ContextText != null && D.Legend.ContextText.Contains("Shut")) { Press(X); yield return WaitR(2.5f); }
                yield return WalkTo(new Vector3(109.5f, VenusLayout.LedgeY, 70.9f), 0.3f);
                _stick = StickFor(Vector3.forward) * 0.2f; yield return WaitR(0.3f); _stick = Vector2.zero;
                ctx = D.Legend.ContextText; Press(A); yield return WaitR(1.8f);
                Log($"jump down from the ledge: prompt '{ctx}', down: {(P.y < VenusLayout.ControlY + 0.2f ? "ok" : "FAILED")}");
                if (P.y < VenusLayout.ControlY + 0.2f) yield return CrossTrough(new Vector3(116f, y, 76f), new Vector3(116f, y, 84f), "dev_crystal");
            }
        }

        IEnumerator RidePath(Vector3[] pts)
        {
            int i = 0; string o0 = Obj;
            while (i < pts.Length && D != null && D.Bike.Riding && Obj == o0)
            {
                if (D.Dlg.Busy && Idle && D.UI.SubtitleVisible) Press(Y);
                var b = D.Bike.transform; var to = Flat(pts[i] - b.position);
                if (to.magnitude < 3.5f) { i++; continue; }
                float ang = Vector3.SignedAngle(Flat(b.forward), to, Vector3.up);
                _stick = new Vector2(Mathf.Clamp(ang / 35f, -1f, 1f), Mathf.Abs(ang) > 50f ? 0.3f : 0.8f);
                yield return null;
            }
            _stick = Vector2.zero;
        }

        void RideDownRoute()
        {
            var b = D.Bike.transform;
            VenusTerrain.OnRoute(b.position.x, b.position.z, out _, out float s, out _, out _);
            var target = b.position.z > 196f ? new Vector3(33f, 0f, 188f) : s < 12f ? VenusLayout.BikeUnpacked : VenusTerrain.AtS(s - 9f);
            float ang = Vector3.SignedAngle(Flat(b.forward), Flat(target - b.position), Vector3.up);
            bool near = P.z < 14f;
            _stick = new Vector2(Mathf.Clamp(ang / 35f, -1f, 1f), near ? (D.Bike.Speed > 3f ? -1f : 0.2f) : Mathf.Abs(ang) > 50f ? 0.3f : 0.85f);
        }

        IEnumerator Aboard()
        {
            var s = S; _stick = Vector2.zero;
            while (s != null && !s.Explored) { if (s.Dlg != null && s.Dlg.Busy && Idle && s.UI.SubtitleVisible) Press(Y); yield return WaitR(0.3f); }
            if (s == null || s.Director == null || s.Director.Seven == null) yield break;
            Log("aboard: free exploration - walking to her galley seat for Set course");
            var seven = s.Director.Seven.transform;
            for (float t = 0f; t < 20f && Flat(s.SeatStandPos - seven.position).magnitude > 0.5f; t += Time.deltaTime) { _stick = StickFor((s.SeatStandPos - seven.position).normalized); yield return null; }
            _stick = Vector2.zero; yield return WaitR(0.6f);
            Log($"at the seat: prompt '{s.Director.ExtraPrompt}' - pressing X");
            Press(X); yield return WaitR(3f);
        }
    }
}
