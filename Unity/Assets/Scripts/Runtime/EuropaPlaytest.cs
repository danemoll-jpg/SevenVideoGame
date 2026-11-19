using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Seven
{
    /// <summary>
    /// Round 14: the player-style run of the Europa chapter (-europaplay, with -capture; add -menupick
    /// "Scene select|Chapter 2 — Venus|Venus: the debrief" to start in the real main menu, and -testspeed 2). A virtual
    /// gamepad on the real input actions - no autopilot, no teleports. It reads only what a player sees: the subtitles (Y
    /// skips a line like a fast reader), the star map (VEN_C13: d-pad to Europa, A to choose, A to confirm), the objective,
    /// the soft white guide light (EuropaDirector.NextMarker) and the context prompt (X). Riding: left stick steers toward
    /// the light, RT forward, A held to rise / B held to sink toward its height, LT to brake near it. On foot: the stick
    /// toward the light. A skip offered after 2 minutes without progress is taken (counted). Ends
    /// "EUROPA PLAYTEST: 1/1, soft-locks N, crashes C, hints H, skips S" once the debrief aboard is done.
    /// </summary>
    public class EuropaPlaytest : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (DemoBoot.CaptureDir == null || !DemoBoot.EuropaPlay || FindAnyObjectByType<EuropaPlaytest>() != null) return;
            var go = new GameObject("EuropaPlaytest"); DontDestroyOnLoad(go); go.AddComponent<EuropaPlaytest>();
        }

        Gamepad _pad; Vector2 _stick; float _rt, _lt; bool _holdA, _holdB;
        readonly Queue<GamepadButton> _presses = new Queue<GamepadButton>(); GamepadButton _cur; int _phase;
        const GamepadButton X = GamepadButton.West, Y = GamepadButton.North, A = GamepadButton.South, B = GamepadButton.East;
        void Press(GamepadButton b) { if (_presses.Count < 3) _presses.Enqueue(b); }
        ChapterCapture Cap => ChapterCapture.I;
        void Log(string s) => Cap?.Log("europa playtest: " + s);
        static EuropaDirector D => EuropaDirector.I != null && EuropaDirector.I.isActiveAndEnabled ? EuropaDirector.I : null;
        static Chapter1Aboard S => Chapter1Aboard.I != null && Chapter1Aboard.I.isActiveAndEnabled ? Chapter1Aboard.I : null;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        int _soft; float _t0; string _lastObj; float _objSince; int _skipsTaken;

        void Update()
        {
            if (_pad == null) return;
            if (Gamepad.current != _pad) _pad.MakeCurrent();
            if (_phase == 0 && _presses.Count > 0) { _cur = _presses.Dequeue(); _phase = 1; }
            uint bits = 0u;
            if (_phase >= 1 && _phase <= 3) bits |= 1u << (int)_cur;
            if (_phase > 0) _phase = _phase >= 6 ? 0 : _phase + 1;
            if (_holdA) bits |= 1u << (int)A; if (_holdB) bits |= 1u << (int)B;
            InputSystem.QueueStateEvent(_pad, new GamepadState { leftStick = _stick, rightTrigger = _rt, leftTrigger = _lt, buttons = bits });
        }

        IEnumerator Start()
        {
            ChapterCapture.Ensure();
            // with -menupick the menu reads the pilot's pad first: this pad is made once the Saucer scene is up
            while (S == null && D == null) yield return null;
            _pad = InputSystem.AddDevice<Gamepad>("EuropaPlayPad"); _pad.MakeCurrent();
            _t0 = Time.time;
            Log($"virtual gamepad on the real input actions; muted={DemoBoot.Muted} listenerVolume={AudioListener.volume}; started {(DemoBoot.MenuPick != null ? "from the main menu: " + DemoBoot.MenuPick : "directly")}");
            float stuckSince = Time.time; string sig = null;
            while (true)
            {
                yield return null;
                // what a player sees: subtitles -> Y (a fast reader); a skip offer -> Y
                var ui = FindAnyObjectByType<ChapterUI>();
                if (ui != null && ui.SubtitleVisible && _phase == 0 && _presses.Count == 0 && Random.value < 0.08f) Press(Y);
                var con = ShipConsole.I;
                if (con != null && con.Choosing && con.Map != null)
                {
                    _stick = Vector2.zero; _rt = 0f;
                    if (con.Map.Selected != "europa") { if (_phase == 0 && _presses.Count == 0) { Press(GamepadButton.DpadRight); yield return WaitR(0.4f); } }
                    else { Log("VEN_C13: the star map - Europa highlighted, A, A"); Press(A); yield return WaitR(0.6f); Press(A); yield return WaitR(1.5f); }
                    continue;
                }
                var s = S;
                if (s != null && s.EuropaMode && s.EuropaDebriefDone) break;
                var d = D;
                if (d == null) { _stick = Vector2.zero; _rt = 0f; _lt = 0f; _holdA = _holdB = false; continue; }
                if (d.SkipOffered) { _skipsTaken++; Log($"skip offered on '{d.WaitingFor}' - taken"); Press(Y); yield return WaitR(1.5f); continue; }
                if (d.InCutscene) { _stick = Vector2.zero; _rt = 0f; _holdA = _holdB = false; continue; }
                if (!string.IsNullOrEmpty(d.Prompt) && _phase == 0 && _presses.Count == 0) { Press(X); Log($"X at '{d.Prompt}'"); yield return WaitR(0.6f); continue; }
                var m = d.NextMarker;
                var seven = d.Seven.transform.position;
                if (d.Riding)
                {
                    var bike = d.Bike.transform;
                    if (!m.HasValue) { _stick = Vector2.zero; _rt = 0f; _lt = 0.4f; _holdA = _holdB = false; continue; }
                    var to = m.Value - bike.position; var f = Flat(to);
                    float err = Vector3.SignedAngle(Flat(bike.forward), f, Vector3.up);
                    _stick = new Vector2(Mathf.Clamp(err / 30f, -1f, 1f), 0f);
                    _rt = Mathf.Abs(err) > 75f ? 0.2f : Mathf.Clamp(f.magnitude / 8f, 0.35f, 1f);
                    _lt = 0f;
                    _holdA = to.y > 1.2f; _holdB = to.y < -1.2f;
                }
                else
                {
                    _rt = 0f; _lt = 0f; _holdA = _holdB = false;
                    if (!m.HasValue) { _stick = Vector2.zero; continue; }
                    var cam = Camera.main != null ? Camera.main.transform : null;
                    var dir = Flat(m.Value - seven); if (dir.magnitude < 0.5f) { _stick = Vector2.zero; continue; }
                    if (cam != null) { var cf = Flat(cam.forward).normalized; var cr = new Vector3(cf.z, 0f, -cf.x); dir.Normalize(); _stick = new Vector2(Vector3.Dot(dir, cr), Vector3.Dot(dir, cf)); }
                }
                // soft-lock watch: the objective / waiting step unchanged for 240 s of game time
                string now = d.ObjectiveNow + "|" + d.WaitingFor;
                if (now != sig) { sig = now; stuckSince = Time.time; }
                else if (Time.time - stuckSince > 240f) { _soft++; Log($"SOFT-LOCK: no story progress for 240 s ('{now}') at {seven}"); stuckSince = Time.time; }
            }
            var dd = EuropaDirector.I;
            Cap.Finish($"EUROPA PLAYTEST: 1/1, soft-locks {_soft}, crashes {(dd != null ? dd.Crashes : -1)}, hints {(dd != null ? dd.Hints : -1)}, skips taken {_skipsTaken} ({(Time.time - _t0) / 60f:0.0} game-min) - {EuropaState.Summary()}");
        }
        IEnumerator WaitR(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }
        void OnDestroy() { if (_pad != null && _pad.added) InputSystem.RemoveDevice(_pad); }
    }
}
