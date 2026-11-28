using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Seven
{
    /// <summary>
    /// QF10 item 8: a player-style Venus run (-venusplay, with -capture; add -venushintfast and -testspeed 2) on a virtual
    /// gamepad driving the real input actions - no autopilot, no teleports. From the arrival through B01 (the bike),
    /// the ride, the collapse (it waits until the hint shows, then lays the slab across with telekinesis (RT) and rides
    /// over it), the temple (C03 / C04 on foot), and at "Ride to the lower gardens" it waits until the skip is offered
    /// and takes it (Y), then C05. The driver reads only what a player sees: the objective, the context prompt, where 7
    /// and her bike are, the camera. Ends "VENUS PLAYTEST: 1/1, soft-locks N, hints H, skips S".
    /// </summary>
    public class VenusPlaytest : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (DemoBoot.CaptureDir == null || !DemoBoot.VenusPlay || FindAnyObjectByType<VenusPlaytest>() != null) return;
            var go = new GameObject("VenusPlaytest"); DontDestroyOnLoad(go); go.AddComponent<VenusPlaytest>();
        }

        Gamepad _pad; Vector2 _stick; float _rt;
        readonly Queue<GamepadButton> _presses = new Queue<GamepadButton>(); GamepadButton _cur; int _phase;
        const GamepadButton X = GamepadButton.West, Y = GamepadButton.North;
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

        static VenusDirector D => VenusDirector.I;
        static Vector3 P => D.Seven.transform.position;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        string Obj => D.ObjectiveNow ?? "";
        ChapterCapture Cap => ChapterCapture.I;
        void Log(string s) => Cap?.Log("venus playtest: " + s);
        int _soft; string _lastObj; float _objSince; bool _wides, _climbed; float _nextRouteShot = 20f; int _routeShots;

        Vector2 StickFor(Vector3 dir)
        {
            var ct = D.Cam.transform; var f = Flat(ct.forward).normalized; var r = new Vector3(f.z, 0f, -f.x);
            dir = Flat(dir); if (dir.sqrMagnitude > 1f) dir.Normalize();
            return new Vector2(Vector3.Dot(dir, r), Vector3.Dot(dir, f));
        }

        IEnumerator Start()
        {
            ChapterCapture.Ensure();
            _pad = InputSystem.AddDevice<Gamepad>("VenusPlayPad"); _pad.MakeCurrent();
            Log($"virtual gamepad on the real input actions; muted={DemoBoot.Muted} listenerVolume={AudioListener.volume}; hint after {(DemoBoot.VenusHintFast ? 12 : 45)} s, skip after {(DemoBoot.VenusHintFast ? 30 : 120)} s");
            while (D == null) yield return null;
            yield return null;
            float t0 = Time.time;
            bool waitedHint = false, tookSkip = false;
            while (!D.PartOneDone)
            {
                if (Obj != _lastObj) { _lastObj = Obj; _objSince = Time.time; Log($"objective: '{Obj}' at {P:0.0}"); }
                if (Time.time - _objSince > 240f) { _soft++; Log($"SOFT-LOCK: '{Obj}' unchanged for 240 s at {P:0.0}"); break; }
                // lines: a player who reads fast skips with Y
                // (in the covered talks - C03 / C04 / C05 - it listens, so every line's framing is captured)
                if (D.Dlg.Busy) { _stick = Vector2.zero; if (Idle && D.UI.SubtitleVisible && !D.Covering) Press(Y); yield return WaitR(0.35f); continue; }
                var s7 = D.Seven; var bike = D.Bike;
                if (D.SkipOffered && Obj.StartsWith("Ride to the lower gardens") && !tookSkip) { tookSkip = true; Log($"the skip is offered for '{D.WaitingFor}' - taking it (Y)"); _stick = Vector2.zero; Press(Y); yield return WaitR(2.5f); continue; }
                switch (true)
                {
                    case true when Obj.StartsWith("Ride to the Elarim temple") && !_wides:
                        _wides = true; _stick = Vector2.zero; yield return D.HighWideShots(); break;
                    case true when Obj.StartsWith("Ride to the Elarim temple") || Obj.StartsWith("Ride across"):
                        if (!bike.Riding && P.z > 185f) { _stick = Vector2.zero; break; }      // at the temple court: the story takes over (B04 / C03)
                        if (!bike.Riding) { yield return WalkTo(bike.transform.position - bike.transform.right * 1.35f, 1.2f); if (D.Legend.ContextText != null && D.Legend.ContextText.Contains("Ride")) { Press(X); yield return WaitR(0.6f); } }
                        else RideToward(196f);
                        break;
                    case true when Obj.StartsWith("The walkway is gone"):
                        if (bike.Riding && P.z < 104f) RideToward(110f);
                        else if (bike.Riding) { _stick = new Vector2(0f, -1f); if (bike.Speed < 1.0f && Idle) { _stick = Vector2.zero; Press(X); yield return WaitR(0.8f); } }
                        break;
                    case true when Obj.StartsWith("Lift the fallen slab"):
                        if (!waitedHint) { _stick = Vector2.zero; if (D.Hints == 0) { yield return null; continue; } waitedHint = true; Log("the hint showed - now doing it"); }
                        if (!_climbed)
                        {
                            // item 1: the garden wall climbs anywhere along it - try a spot 2.5 m from the old mantle point, then jump down
                            _climbed = true;
                            yield return WalkTo(new Vector3(24.15f, 0f, 105.5f), 0.2f);
                            _stick = StickFor(Vector3.left) * 0.2f; yield return WaitR(0.35f); _stick = Vector2.zero;
                            string ctx = D.Legend.ContextText; Press(GamepadButton.South); yield return WaitR(2.2f);
                            bool up = P.y > 0.9f; Log($"climb anywhere (item 1): prompt '{ctx}', up on the wall at z 105.5: {(up ? "ok" : "FAILED")} ({P:0.00})");
                            if (Cap != null) yield return Cap.Shot("item1_climbed_the_wall_anywhere");
                            if (up) { _stick = StickFor(Vector3.right) * 0.3f; yield return WaitR(0.5f); _stick = StickFor(Vector3.right) * 0.15f; yield return WaitR(0.3f); _stick = Vector2.zero; ctx = D.Legend.ContextText; Press(GamepadButton.South); yield return WaitR(1.6f); Log($"jump down (item 1): prompt '{ctx}', back on the road: {(P.y < 0.2f ? "ok" : "FAILED")} ({P:0.00})"); }
                        }
                        yield return WalkTo(new Vector3(30.4f, 0f, 110.4f), 0.6f);
                        _stick = StickFor(Vector3.forward) * 0.25f; yield return WaitR(0.3f); _stick = Vector2.zero;
                        if (D.Legend.ContextText != null && D.Legend.ContextText.Contains("Telekinesis")) { Press(GamepadButton.RightTrigger); yield return WaitR(6f); }
                        break;
                    case true when Obj.StartsWith("Ride to the lower gardens"):
                        _stick = Vector2.zero;                         // waits (to see the hint, then the skip)
                        break;
                    case true when s7.Current == ChapterSeven.Mode.Free && P.z > 205f && P.z < VenusLayout.CubeRoomZ + 1f:
                        yield return WalkTo(new Vector3(30f, 0f, Mathf.Min(P.z + 12f, VenusLayout.CubeView.z)), 1f);
                        break;
                    default:
                        if (s7.Current == ChapterSeven.Mode.Free && P.z > 205f && P.z < 262f) yield return WalkTo(VenusLayout.CubeView, 1f);
                        else _stick = Vector2.zero;
                        break;
                }
                yield return null;
            }
            _stick = Vector2.zero;
            Log($"route done in {Time.time - t0:0} s game time");
            Cap.Finish($"VENUS PLAYTEST: {(D.PartOneDone && _soft == 0 ? 1 : 0)}/1, soft-locks {_soft}, hints {D.Hints}, skips {D.Skips}, bridge {(VenusState.SceneDone("VEN_B03_bridge") ? "laid" : "NOT laid")}, coverage {D.Coverage}, {VenusState.CurrentId}");
        }

        IEnumerator WaitR(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }

        IEnumerator WalkTo(Vector3 to, float tol)
        {
            string o0 = Obj;
            for (float t = 0f; t < 20f && Flat(to - P).magnitude > tol && !D.Dlg.Busy && Obj == o0; t += Time.deltaTime)
            {
                if (D.Seven.Current != ChapterSeven.Mode.Free) { _stick = Vector2.zero; yield return null; continue; }
                _stick = StickFor((to - P).normalized); yield return null;
            }
            _stick = Vector2.zero;
        }

        /// <summary>Ride along the worn path: steer toward a point ~9 m ahead on it, ease off in tight turns.</summary>
        void RideToward(float maxZ)
        {
            var b = D.Bike.transform;
            // (6f check) the riding camera along the whole route, every ~22 m
            if (D.Bike.Odometer > _nextRouteShot && Cap != null) { _nextRouteShot += 22f; StartCoroutine(Cap.Shot($"route_ride_{_routeShots++:00}_z{b.position.z:0}")); }
            VenusTerrain.OnRoute(b.position.x, b.position.z, out _, out float s, out _, out _);
            var target = VenusTerrain.AtS(s + 9f);
            if (target.z > maxZ) target.z = maxZ;
            float ang = Vector3.SignedAngle(Flat(b.forward), Flat(target - b.position), Vector3.up);
            _stick = new Vector2(Mathf.Clamp(ang / 35f, -1f, 1f), Mathf.Abs(ang) > 50f ? 0.3f : 0.85f);
        }
    }
}
