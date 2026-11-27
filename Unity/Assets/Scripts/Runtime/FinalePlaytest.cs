using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Seven
{
    /// <summary>
    /// Round 19: the player-style run of the Moon finale (-finplay; a copy of CattalusPlaytest). Starts free aboard at Cattalus with
    /// every world done: a virtual gamepad on the real input actions walks to the console, X, the star map (through the gateway back to
    /// our system, d-pad to the Moon, A, A), FIN_01 (the stick steers the saucer down to the guide ring in the bay), the whole chapter on
    /// foot with Panther (the prompts and their buttons, the guide light + marker), the confrontation (played by BossPilot like a
    /// player; with -finretry she goes down once and the retry keeps what was done), the barrier (RT), the archive (X), the ending and
    /// the credits. It reads only what a player sees. Ends "FINALE PLAYTEST: 1/1, soft-locks N, ...".
    /// </summary>
    public class FinalePlaytest : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (DemoBoot.CaptureDir == null || !DemoBoot.FinPlay || FindAnyObjectByType<FinalePlaytest>() != null) return;
            var go = new GameObject("FinalePlaytest"); DontDestroyOnLoad(go); go.AddComponent<FinalePlaytest>();
        }

        Gamepad _pad; Vector2 _stick; float _rt, _lt; uint _held;
        readonly Queue<GamepadButton> _presses = new Queue<GamepadButton>(); GamepadButton _cur; int _phase;
        const GamepadButton X = GamepadButton.West, Y = GamepadButton.North, A = GamepadButton.South;
        ChapterCapture Cap => ChapterCapture.I;
        void Log(string s) => Cap?.Log("Finale playtest: " + s);
        static FinaleDirector D => FinaleDirector.I != null && FinaleDirector.I.isActiveAndEnabled ? FinaleDirector.I : null;
        static Chapter1Aboard S => Chapter1Aboard.I != null && Chapter1Aboard.I.isActiveAndEnabled ? Chapter1Aboard.I : null;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        int _soft, _skips, _xs, _tks; float _t0;
        Vector3 _lastPos; float _lastMoveT, _sideUntil, _side = 1f;

        void Update()
        {
            if (_pad == null) return;
            if (Gamepad.current != _pad) _pad.MakeCurrent();
            if (_phase == 0 && _presses.Count > 0) { _cur = _presses.Dequeue(); _phase = 1; }
            uint bits = _held;
            if (_phase >= 1 && _phase <= 3) bits |= 1u << (int)_cur;
            if (_phase > 0) _phase = _phase >= 6 ? 0 : _phase + 1;
            InputSystem.QueueStateEvent(_pad, new GamepadState { leftStick = _stick, rightTrigger = _rt, leftTrigger = _lt, buttons = bits });
        }
        void Press(GamepadButton b) { if (_presses.Count < 3) _presses.Enqueue(b); }
        IEnumerator WaitR(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }
        void StickToward(Vector3 at, Vector3 from, float k = 1f)
        {
            var dir = Flat(at - from); var cam = Camera.main != null ? Camera.main.transform : null;
            if (cam == null || dir.magnitude < 0.3f) { _stick = Vector2.zero; return; }
            var cf = Flat(cam.forward).normalized; var cr = new Vector3(cf.z, 0f, -cf.x); dir.Normalize();
            _stick = new Vector2(Vector3.Dot(dir, cr), Vector3.Dot(dir, cf)) * k;
        }

        IEnumerator Start()
        {
            ChapterCapture.Ensure();
            while (S == null && D == null) yield return null;
            _pad = InputSystem.AddDevice<Gamepad>("FinalePlayPad"); _pad.MakeCurrent();
            _t0 = Time.time;
            Log($"virtual gamepad on the real input actions; muted={DemoBoot.Muted} listenerVolume={AudioListener.volume}; {WorldFlags.Summary()}; retry wanted: {DemoBoot.FinRetry}");
            float stuckSince = Time.time; string sig = null; bool consoleDone = DemoBoot.FinCp >= 0;
            while (true)
            {
                yield return null;
                var ui = FindAnyObjectByType<ChapterUI>();
                if (ui != null && ui.SubtitleVisible && _phase == 0 && _presses.Count == 0 && Random.value < 0.03f && (D == null || !D.InCredits)) Press(Y);
                var con = ShipConsole.I;
                if (con != null && con.Map != null && (con.Choosing || con.Current == ShipConsole.State.Using && con.CurrentPage == ShipConsole.Page.Map))
                {
                    _stick = Vector2.zero; _rt = 0f; _held = 0u;
                    if (con.Map.Selected != WholeGameRun.MapWant(con, "moon"))
                    {
                        if (_phase == 0 && _presses.Count == 0)
                        {
                            bool gate = con.Map.IsGateway(con.Map.Selected);
                            Press(con.Map.View != StarMap.Sol && gate ? A : GamepadButton.DpadRight); yield return WaitR(0.6f);
                        }
                    }
                    else { Log("star map: the Moon highlighted - A, A"); Press(A); yield return WaitR(0.6f); Press(A); yield return WaitR(1.6f); consoleDone = true; }
                    continue;
                }
                var s = S;
                // FIN_01: the stick steers the saucer down to the guide ring in the bay
                if (s != null && s.FinaleMode && s.GuidingTo.HasValue && s.GuidingSaucer != null)
                {
                    var sp = s.GuidingSaucer; var f = Flat(s.GuidingTo.Value - sp.position);
                    float err = Vector3.SignedAngle(Flat(sp.forward), f, Vector3.up); _stick = new Vector2(Mathf.Clamp(err / 25f, -1f, 1f), 0f); continue;
                }
                if (s != null && D == null && s.Explored && !s.InGalleyScene && !consoleDone && con != null)
                {
                    if (con.Current == ShipConsole.State.Using && con.CurrentPage == ShipConsole.Page.Main)
                    {
                        int idx = ShipConsole.MainLabels().IndexOf("Star map");
                        for (int i = 0; i < idx; i++) { Press(GamepadButton.DpadDown); yield return WaitR(0.3f); }
                        Press(A); yield return WaitR(1.0f);
                        for (float w = 0f; w < 4f && con.Current == ShipConsole.State.Using && con.CurrentPage == ShipConsole.Page.Main; w += Time.unscaledDeltaTime) yield return null;
                        continue;
                    }
                    if (con.Current == ShipConsole.State.Off && !con.Busy)
                    {
                        var p7 = con.Director.Seven.transform.position;
                        if (con.NearConsole) { _stick = Vector2.zero; Press(X); _xs++; Log("X at the console"); yield return WaitR(1.5f); }
                        else StickToward(con.StandSpot.position, p7, 0.8f);
                    }
                    continue;
                }
                var d = D;
                if (d == null) { _stick = Vector2.zero; _rt = 0f; _lt = 0f; _held = 0u; continue; }
                if (d.ChapterDone) break;
                if (d.SkipOffered) { _skips++; Log($"skip offered on '{d.WaitingFor}' - taken"); Press(Y); yield return WaitR(1.5f); continue; }
                _held = 0u; _lt = 0f; _rt = 0f;
                if (d.AwaitsChoice && _phase == 0 && _presses.Count == 0) { _stick = Vector2.zero; Press(X); _xs++; Log($"X at '{d.Prompt}' (a choice inside the scene)"); yield return WaitR(0.8f); continue; }
                if (d.InCutscene || d.InCredits) { _stick = Vector2.zero; continue; }
                if (BossFight.Active != null && !BossFight.Active.Over) { continue; }   // the confrontation: BossPilot plays it as a player would
                var seven = d.Seven.transform.position;
                if (!string.IsNullOrEmpty(d.Prompt) && _phase == 0 && _presses.Count == 0)
                {
                    _stick = Vector2.zero;
                    if (d.PromptBtn == ControlsLegend.Btn.Tk) { _rt = 1f; _tks++; Log($"RT at '{d.Prompt}'"); yield return WaitR(0.25f); _rt = 0f; yield return WaitR(0.6f); continue; }
                    Press(X); _xs++; Log($"X at '{d.Prompt}'"); yield return WaitR(0.6f); continue;
                }
                // the archive (FIN_11: no prompt, no text - she walks up to it and presses once)
                if (d.WaitingFor != null && d.WaitingFor.StartsWith("resume the sharing")) { _stick = Vector2.zero; Press(X); _xs++; Log("X at the archive (no prompt in FIN_11)"); yield return WaitR(1.0f); continue; }
                var m = d.NextMarker;
                if (!m.HasValue) { _stick = Vector2.zero; continue; }
                if (Flat(m.Value - seven).magnitude < 0.5f) { _stick = Vector2.zero; continue; }
                StickToward(m.Value, seven);
                if (Time.time > _sideUntil)
                {
                    if (Flat(seven - _lastPos).magnitude > 0.4f) { _lastPos = seven; _lastMoveT = Time.time; }
                    else if (Time.time - _lastMoveT > 1.5f) { _side = -_side; _sideUntil = Time.time + 1.1f; _lastMoveT = Time.time; Log("blocked - steps round it"); }
                }
                if (Time.time < _sideUntil) { var cam = Camera.main.transform; var cf = Flat(cam.forward).normalized; var cr = new Vector3(cf.z, 0f, -cf.x); var dir = Flat(m.Value - seven).normalized; var side = Vector3.Cross(Vector3.up, dir) * _side; var v = (side * 0.9f + dir * 0.3f).normalized; _stick = new Vector2(Vector3.Dot(v, cr), Vector3.Dot(v, cf)); }
                string now = d.ObjectiveNow + "|" + d.WaitingFor;
                if (now != sig) { sig = now; stuckSince = Time.time; }
                else if (Time.time - stuckSince > 240f) { _soft++; Log($"SOFT-LOCK: no story progress for 240 s ('{now}') at {seven}"); stuckSince = Time.time; }
            }
            yield return WaitR(1f);
            Cap.Finish($"FINALE PLAYTEST: 1/1, soft-locks {_soft}, skips taken {_skips}, X presses {_xs}, telekinesis {_tks}, ({(Time.time - _t0) / 60f:0.0} game-min) - {FinaleState.Summary()}; {WorldFlags.Summary()}");
        }
        void OnDestroy() { if (_pad != null && _pad.added) InputSystem.RemoveDevice(_pad); }
    }
}
