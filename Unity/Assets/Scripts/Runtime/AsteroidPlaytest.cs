using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Seven
{
    /// <summary>
    /// Round 16: the player-style run of the asteroid chapter (-astplay -capture dir; ONE run, testing rule 4). Starts free
    /// aboard with Mars + Europa complete and the combined records seen (the asteroid not found yet): a virtual gamepad on
    /// the real input actions walks to the console, X, the star map (d-pad to the Biaveh gateway, A; Biaveh, A, A) - the
    /// first journey finds the asteroid (TRV_AST) - "Investigate the asteroid" (d-pad, A, A) - AST_01 - the whole chapter -
    /// AST_10 / AST_11 aboard. It reads only what a player sees: the subtitles (Y skips a line now and then), the objective,
    /// the guide light + marker, the context prompt and its button (X / RT / RB / A to climb), the security machines
    /// coming at her, the boss's tells (BossPilot). Like run 5's habits it skips the optional bird. No teleports.
    /// Ends "ASTEROID PLAYTEST: 1/1, soft-locks N, skips S".
    /// </summary>
    public class AsteroidPlaytest : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (DemoBoot.CaptureDir == null || !DemoBoot.AstPlay || FindAnyObjectByType<AsteroidPlaytest>() != null) return;
            var go = new GameObject("AsteroidPlaytest"); DontDestroyOnLoad(go); go.AddComponent<AsteroidPlaytest>();
        }

        Gamepad _pad; Vector2 _stick; float _rt, _lt; uint _held;
        readonly Queue<GamepadButton> _presses = new Queue<GamepadButton>(); GamepadButton _cur; int _phase;
        int _rtTap;
        const GamepadButton X = GamepadButton.West, Y = GamepadButton.North, A = GamepadButton.South;
        ChapterCapture Cap => ChapterCapture.I;
        void Log(string s) => Cap?.Log("asteroid playtest: " + s);
        static AsteroidDirector D => AsteroidDirector.I != null && AsteroidDirector.I.isActiveAndEnabled ? AsteroidDirector.I : null;
        static Chapter1Aboard S => Chapter1Aboard.I != null && Chapter1Aboard.I.isActiveAndEnabled ? Chapter1Aboard.I : null;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        int _soft, _skips, _xs, _tks, _rbs, _climbs, _secAnswers; float _t0;
        Vector3 _lastPos; float _lastMoveT, _sideUntil, _side = 1f;

        void Update()
        {
            if (_pad == null) return;
            if (Gamepad.current != _pad) _pad.MakeCurrent();
            if (_phase == 0 && _presses.Count > 0) { _cur = _presses.Dequeue(); _phase = 1; }
            uint bits = _held;
            if (_phase >= 1 && _phase <= 3) bits |= 1u << (int)_cur;
            if (_phase > 0) _phase = _phase >= 6 ? 0 : _phase + 1;
            float rt = _rt; if (_rtTap > 0) { rt = _rtTap > 2 ? 1f : 0f; _rtTap--; }
            InputSystem.QueueStateEvent(_pad, new GamepadState { leftStick = _stick, rightTrigger = rt, leftTrigger = _lt, buttons = bits });
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

        // ---------------------------------------------------------------- the boss fight (as the Mars / Venus runs)
        float _fightT0 = -1f; bool _drew;
        void FightPad(BossFight f, ChapterSeven s)
        {
            if (_fightT0 < 0f) _fightT0 = Time.time;
            if (!_drew && Time.time - _fightT0 > 1.5f && _phase == 0) { _drew = true; if (!s.BladesDrawn) { _cur = GamepadButton.RightShoulder; _phase = 1; _rbs++; Log("RB - she draws the arc blades for the fight"); return; } Log("the arc blades are already drawn for the fight"); }
            var I = BossPilot.Think(f, s);
            _stick = BossPilot.Stick(I.Move);
            _lt = I.Block ? 1f : 0f; _rt = 0f;
            _held = I.Run ? 1u << (int)GamepadButton.LeftShoulder : 0u;
            if (I.Tk && _rtTap == 0) _rtTap = 5;
            GamepadButton? b = I.Dodge ? GamepadButton.East : I.Hop ? GamepadButton.South : (I.Attack || I.Use) ? GamepadButton.West : (I.Takedown || I.Kick) ? GamepadButton.North : I.Skip ? GamepadButton.Select : (GamepadButton?)null;
            if (b.HasValue && (_phase == 0 || I.Dodge || I.Hop || I.Use)) { _presses.Clear(); _cur = b.Value; _phase = 1; }
        }

        IEnumerator Start()
        {
            ChapterCapture.Ensure();
            while (S == null && D == null) yield return null;
            _pad = InputSystem.AddDevice<Gamepad>("AsteroidPlayPad"); _pad.MakeCurrent();
            _t0 = Time.time;
            Log($"virtual gamepad on the real input actions; muted={DemoBoot.Muted} listenerVolume={AudioListener.volume}; {WorldFlags.Summary()}");
            float stuckSince = Time.time; string sig = null; bool consoleDone = false; string want = "biaveh";
            while (true)
            {
                yield return null;
                var ui = FindAnyObjectByType<ChapterUI>();
                if (ui != null && ui.SubtitleVisible && _phase == 0 && _presses.Count == 0 && Random.value < 0.05f) Press(Y);
                var con = ShipConsole.I;
                // the star map: a choice (TRV_AST) or the free console
                if (con != null && con.Map != null && (con.Choosing || con.Current == ShipConsole.State.Using && con.CurrentPage == ShipConsole.Page.Map))
                {
                    _stick = Vector2.zero; _rt = 0f; _held = 0u;
                    if (con.Choosing) want = "asteroid";
                    if (con.Map.Selected != want) { if (_phase == 0 && _presses.Count == 0) { Press(con.Map.IsGateway(con.Map.Selected) && !con.Choosing && want == "biaveh" && con.Map.View == StarMap.Sol ? A : GamepadButton.DpadRight); yield return WaitR(0.6f); } }
                    else { Log($"star map: {want} highlighted - A, A"); Press(A); yield return WaitR(0.6f); Press(A); yield return WaitR(1.6f); consoleDone = true; }
                    continue;
                }
                var s = S;
                if (s != null && s.AsteroidMode && s.AsteroidHomeDone) break;
                // aboard, free: walk to the console, X, then the main menu down to "Star map", A
                if (s != null && D == null && s.Explored && !s.InGalleyScene && !consoleDone && con != null)
                {
                    if (con.Current == ShipConsole.State.Using && con.CurrentPage == ShipConsole.Page.Main)
                    {
                        int idx = ShipConsole.MainLabels().IndexOf("Star map");
                        Log($"console open - d-pad down {idx}x to the star map, A");
                        for (int i = 0; i < idx; i++) { Press(GamepadButton.DpadDown); yield return WaitR(0.3f); }
                        Press(A); yield return WaitR(1.0f); continue;
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
                if (d.SkipOffered) { _skips++; Log($"skip offered on '{d.WaitingFor}' - taken"); Press(Y); yield return WaitR(1.5f); continue; }
                if (BossFight.Active != null && !BossFight.Active.Over && d.Seven.InFight) { FightPad(BossFight.Active, d.Seven); continue; }
                _held = 0u;
                if (d.InCutscene) { _stick = Vector2.zero; _rt = 0f; continue; }
                var seven = d.Seven.transform.position;
                // a security machine rolling at her: the blades if drawn and close, else telekinesis
                if (d.Security != null && !d.Riding)
                {
                    AstSecurity near = null; float bd = 99f;
                    foreach (var sm in d.Security) if (sm != null && sm.gameObject.activeInHierarchy && sm.State == AstSecurity.St.Chase) { float dd = Flat(sm.transform.position - seven).magnitude; if (dd < bd) { bd = dd; near = sm; } }
                    if (near != null && bd < 8f)
                    {
                        _secAnswers++;
                        if (d.Seven.BladesDrawn && bd < 2.4f) { _stick = Vector2.zero; if (_phase == 0) { Press(X); Log($"a security machine at {bd:0.0} m - X: the blades"); yield return WaitR(0.7f); } continue; }
                        if (!d.Seven.BladesDrawn && d.Seven.BladesOwned && _phase == 0) { Press(GamepadButton.RightShoulder); _rbs++; yield return WaitR(0.3f); continue; }
                        if (_rtTap == 0) { StickToward(near.transform.position, seven, 0.2f); _rtTap = 5; _tks++; Log($"a security machine at {bd:0.0} m - RT: telekinesis"); yield return WaitR(0.6f); continue; }
                    }
                }
                // the prompt: X (interact), RT (telekinesis), RB (draw the blades); never "Look" (run 5 skips the optional bird)
                if (!string.IsNullOrEmpty(d.Prompt) && d.Prompt != "Look")
                {
                    if (d.PromptBtn == ControlsLegend.Btn.Tk) { _stick = Vector2.zero; if (_rtTap == 0) { _rtTap = 5; _tks++; } yield return WaitR(0.5f); continue; }
                    if (d.PromptBtn == ControlsLegend.Btn.Blades) { _stick = Vector2.zero; if (_phase == 0) { Press(GamepadButton.RightShoulder); _rbs++; Log("RB - draw the arc blades"); } yield return WaitR(0.6f); continue; }
                    if (_phase == 0 && _presses.Count == 0 && !(d.Prompt == "Ride" && !WantsBike(d)) && !(d.Prompt == "Get off" && !WantsOff(d))) { _stick = Vector2.zero; _rt = 0f; Press(X); _xs++; Log($"X at '{d.Prompt}'"); yield return WaitR(0.6f); continue; }
                }
                // climbing (AST_06): A at the wall when 7's own context says so
                if (!d.Riding && d.Seven.Context != null && d.Seven.Context.StartsWith("jump:")) { _stick = Vector2.zero; if (_phase == 0) { Press(A); _climbs++; Log($"A - '{d.Seven.Context}'"); } yield return WaitR(0.5f); continue; }
                if (!d.Riding && (d.Seven.Current == ChapterSeven.Mode.Hang || d.Seven.Current == ChapterSeven.Mode.Climb)) { _stick = new Vector2(0f, 1f); continue; }
                var m = d.NextMarker;
                if (d.Riding)
                {
                    var bike = d.Bike.transform;
                    if (!m.HasValue) { _stick = Vector2.zero; _rt = 0f; _lt = 0.4f; continue; }
                    var f = Flat(m.Value - bike.position);
                    float err = Vector3.SignedAngle(Flat(bike.forward), f, Vector3.up);
                    if (f.magnitude < 5f) { _stick = Vector2.zero; _rt = 0f; _lt = 1f; continue; }
                    _stick = new Vector2(Mathf.Clamp(err / 30f, -1f, 1f), 0f);
                    _rt = Mathf.Abs(err) > 75f ? 0.2f : Mathf.Clamp(f.magnitude / 14f, 0.35f, 1f);
                    _lt = 0f;
                }
                else
                {
                    _rt = 0f; _lt = 0f;
                    if (!m.HasValue) { _stick = Vector2.zero; continue; }
                    if (Flat(m.Value - seven).magnitude < 0.5f) { _stick = Vector2.zero; continue; }
                    StickToward(m.Value, seven);
                    // stuck against something (no headway for 1.5 s): step sideways for a moment, alternating sides
                    if (Time.time > _sideUntil)
                    {
                        if (Flat(seven - _lastPos).magnitude > 0.4f) { _lastPos = seven; _lastMoveT = Time.time; }
                        else if (Time.time - _lastMoveT > 1.5f) { _side = -_side; _sideUntil = Time.time + 1.1f; _lastMoveT = Time.time; Log("blocked - steps round it"); }
                    }
                    if (Time.time < _sideUntil) { var cam = Camera.main.transform; var cf = Flat(cam.forward).normalized; var cr = new Vector3(cf.z, 0f, -cf.x); var dir = Flat(m.Value - seven).normalized; var side = Vector3.Cross(Vector3.up, dir) * _side; var v = (side * 0.9f + dir * 0.3f).normalized; _stick = new Vector2(Vector3.Dot(v, cr), Vector3.Dot(v, cf)); }
                }
                string now = d.ObjectiveNow + "|" + d.WaitingFor;
                if (now != sig) { sig = now; stuckSince = Time.time; }
                else if (Time.time - stuckSince > 240f) { _soft++; Log($"SOFT-LOCK: no story progress for 240 s ('{now}') at {seven}"); stuckSince = Time.time; }
            }
            yield return WaitR(2f);
            Cap.Finish($"ASTEROID PLAYTEST: 1/1, soft-locks {_soft}, skips taken {_skips}, X presses {_xs}, telekinesis {_tks}, RB {_rbs}, climb presses {_climbs}, security answers {_secAnswers} ({(Time.time - _t0) / 60f:0.0} game-min) - {AsteroidState.Summary()}; {WorldFlags.Summary()}");
        }
        static bool BikeStep(AsteroidDirector d) => (d.ObjectiveNow != null && d.ObjectiveNow.Contains("bike")) || (d.WaitingFor != null && d.WaitingFor.Contains("on the bike"));
        static bool WantsBike(AsteroidDirector d) => d.NextMarker.HasValue && (BikeStep(d) || Flat(d.NextMarker.Value - d.Seven.transform.position).magnitude > 14f);
        static bool WantsOff(AsteroidDirector d) => d.NextMarker.HasValue && !BikeStep(d) && Flat(d.NextMarker.Value - d.Bike.transform.position).magnitude < 12f;
        void OnDestroy() { if (_pad != null && _pad.added) InputSystem.RemoveDevice(_pad); }
    }
}
