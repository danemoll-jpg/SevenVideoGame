using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// QF6: the whole of Chapter 1 played like a person (-playtest n -capture dir), from the main menu to "Set course" in
    /// the galley, n times. Nothing is scripted inside the story code: a virtual gamepad drives the real input actions
    /// (left stick camera-relative, A / B / X / Y / RT / LB / Start / d-pad), the free player camera, the real menus
    /// (main menu, pause menu, Continue). No autopilot, no teleports. The driver only looks at what a player sees: the
    /// objective line, the context prompt, where 7 is and what she is doing (standing, hanging), the Scree guard.
    /// It picks its next move from 7's position, so a fall reset, a checkpoint restart or a Continue never confuse it.
    /// Each run has its own seed and habits: hurries (runs, skips lines) · waits · backtracks · wanders off the route ·
    /// skips optional bits (never presses "Take" - walks out; doesn't wait for the cat) · falls on purpose (and gets reset)
    /// · quits to the main menu and Continues at checkpoints (each checkpoint is covered by at least two runs) · restarts
    /// from a checkpoint · frame hitches on every second run. Pass = the run reaches the galley and "Set course" plays
    /// with no soft-lock (the story has not moved for 240 s) and the dodecahedron with her. Ends "PLAYTEST: k/n".
    /// -sceneselecttest: every Scene Select entry started from the menu (one from the pause menu) and played on until the
    /// next entry's checkpoint; the state at each start is logged (shape, slab, doors, cat).
    /// </summary>
    public class PlaytestRun : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (DemoBoot.CaptureDir == null || !(DemoBoot.Playtest > 0 || DemoBoot.SceneSelectTest)) return;
            var go = new GameObject("PlaytestRun");
            DontDestroyOnLoad(go);
            go.AddComponent<PlaytestRun>();
        }

        // ------------------------------------------------------------------------------------------ the virtual pad
        Gamepad _pad;
        Vector2 _stick, _look;
        bool _run;
        readonly Queue<GamepadButton> _presses = new Queue<GamepadButton>();
        GamepadButton _cur; int _phase;            // 0 idle, 1..3 held, 4..6 released
        const GamepadButton A = GamepadButton.South, B = GamepadButton.East, X = GamepadButton.West, Y = GamepadButton.North,
            RT = GamepadButton.RightTrigger, StartBtn = GamepadButton.Start, Down = GamepadButton.DpadDown, Up = GamepadButton.DpadUp;
        void Press(GamepadButton b) => _presses.Enqueue(b);
        int _fightBlows, _fightTakedowns;
        bool Idle => _phase == 0 && _presses.Count == 0;
        System.Random _rng = new System.Random(1);
        bool _hitches;

        void Update()
        {
            if (_pad == null) return;
            if (Gamepad.current != _pad) _pad.MakeCurrent();     // the menus read Gamepad.current (a real idle pad must not win)
            if (_phase == 0 && _presses.Count > 0) { _cur = _presses.Dequeue(); _phase = 1; }
            uint bits = _run ? 1u << (int)GamepadButton.LeftShoulder : 0u;
            float rt = 0f;
            if (_phase >= 1 && _phase <= 3) { if (_cur == RT) rt = 1f; else bits |= 1u << (int)_cur; }
            if (_phase > 0) _phase = _phase >= 6 ? 0 : _phase + 1;
            InputSystem.QueueStateEvent(_pad, new GamepadState { leftStick = _stick, rightStick = _look, rightTrigger = rt, buttons = bits });
            if (_hitches && Time.realtimeSinceStartup > _nextHitch) { _nextHitch = Time.realtimeSinceStartup + 4f + (float)_rng.NextDouble() * 4f; System.Threading.Thread.Sleep(150 + _rng.Next(300)); _hitchCount++; }
        }
        int _hitchCount; float _nextHitch;

        // ------------------------------------------------------------------------------------------ what a player sees
        static Chapter1Director D => Chapter1Director.I;
        static Chapter1Aboard Ab => Chapter1Aboard.I;
        static string SceneNow => SceneManager.GetActiveScene().name;
        static bool OnMoon => SceneNow == "Chapter1" && D != null && D.Seven != null;
        static bool Aboard => SceneNow == "Saucer" && Ab != null && Ab.Director != null;
        static ChapterSeven S7 => D.Seven;
        static Vector3 P => OnMoon ? D.Seven.transform.position : Aboard ? Ab.Director.Seven.transform.position : Vector3.zero;
        static string Obj => OnMoon ? D.UI.Objective : Aboard ? Ab.UI.Objective : null;
        static bool Free7 => OnMoon && S7.Current == ChapterSeven.Mode.Free;
        static bool Hanging(MoonTraversal.Ledge l) => OnMoon && S7.Current == ChapterSeven.Mode.Hang && S7.HangLedge == l;
        static bool FreeAt(float y) => Free7 && Mathf.Abs(P.y - y) < 0.12f;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        static float DistF(Vector3 a, Vector3 b) => Flat(a - b).magnitude;
        // the player has 7 when she can move (scene 2 lets her walk inside a "cutscene" - so go by her own mode)
        static bool Controlling => OnMoon ? !D.Resetting && !PauseMenu.IsOpen && D.UI.FadeAlpha < 0.3f
                                            && S7.Current != ChapterSeven.Mode.Locked && S7.Current != ChapterSeven.Mode.Touch && S7.Current != ChapterSeven.Mode.Float && S7.Current != ChapterSeven.Mode.Gesture
                                          : Aboard && !PauseMenu.IsOpen && Ab.Director.Seven.Current == SaucerSeven.Mode.Free && !Ab.Director.Seven.Frozen;
        Transform CamT => OnMoon ? D.Cam.transform : Aboard ? Ab.Director.Cam.transform : null;

        Vector2 StickFor(Vector3 dir)
        {
            var ct = CamT; if (ct == null) return Vector2.zero;
            var f = Flat(ct.forward).normalized; var r = new Vector3(f.z, 0f, -f.x);
            dir = Flat(dir); if (dir.sqrMagnitude > 1f) dir.Normalize();
            return new Vector2(Vector3.Dot(dir, r), Vector3.Dot(dir, f));
        }

        // ------------------------------------------------------------------------------------------ log
        ChapterCapture Cap => ChapterCapture.I;
        readonly List<string> _results = new List<string>();
        void Log(string s) => Cap?.Log("playtest: " + s);
        void Flush()
        {
            try { File.WriteAllText(System.IO.Path.Combine(DemoBoot.CaptureDir, "playtest_progress.txt"), string.Join("\n", _results) + "\n"); } catch { }
        }
        Camera _attached;
        void AttachCamera()
        {
            Camera cam = OnMoon ? D.Cam.GetComponent<Camera>() : Aboard ? Ab.Director.Cam.GetComponent<Camera>() : DemoMenu.I != null ? DemoMenu.I.Cam : null;
            if (cam != null && cam != _attached && Cap != null) { Cap.Attach(cam); _attached = cam; }
        }
        IEnumerator ShotNow(string name) { AttachCamera(); if (Cap != null) yield return Cap.Shot(name); }

        // ------------------------------------------------------------------------------------------ run state
        int _run_i;
        string _phaseName = "start";
        int _quits, _restarts, _fallsWanted, _wanders, _backtracks, _waits;
        readonly HashSet<int> _quitAt = new HashSet<int>(), _quitDone = new HashSet<int>();
        int _restartAt = -1; bool _restartDone;
        bool _hurry, _patient, _backtracker, _wanderer, _skipper, _faller, _skipLines;
        bool _fellShaft, _fellTerrace, _fellGap, _backtrackedHere, _spottedOnPurpose;
        string _sig; float _sigAt;
        int _lastCp = -1;
        bool _shots;

        IEnumerator Start()
        {
            ChapterCapture.Ensure();
            PauseMenu.ScriptInput = true;            // the pause menu takes the (virtual) pad as for a player
            _pad = InputSystem.AddDevice<Gamepad>("PlaytestPad");
            _pad.MakeCurrent();
            Log($"virtual gamepad '{_pad.name}' on the real input actions; muted={DemoBoot.Muted} listenerVolume={AudioListener.volume}; QF6 fix {(Chapter1Director.QF6Fix ? "on" : "OFF - repro")}");
            yield return null;
            if (DemoBoot.SceneSelectTest) { yield return SceneSelectAll(); yield break; }
            int n = DemoBoot.Playtest;
            for (int i = Mathf.Clamp(DemoBoot.PlaytestFrom, 0, n - 1); i < n; i++)
            {
                yield return OneRun(i, n);
                Flush();
            }
            int pass = _results.FindAll(r => r.Contains(": PASS")).Count;
            int soft = _results.FindAll(r => r.Contains("SOFT-LOCK")).Count;
            SaveGame.ClearTestSave();
            Cap.Finish($"PLAYTEST: {pass}/{n - Mathf.Clamp(DemoBoot.PlaytestFrom, 0, n - 1)}, soft-locks {soft}, story watchdog fallbacks {Chapter1State.StoryWatchdogs} (QF6 fix {(Chapter1Director.QF6Fix ? "on" : "OFF - repro")})");
        }

        // ------------------------------------------------------------------------------------------ one full run
        IEnumerator OneRun(int i, int n)
        {
            _run_i = i;
            _rng = new System.Random(9000 + (i + DemoBoot.PlaytestSeed) * 7919);
            int v = i % 5;
            _hurry = v == 0 || _rng.NextDouble() < 0.2; _patient = v == 1; _backtracker = v == 2; _wanderer = v == 3; _skipper = v == 4;
            _faller = i % 2 == 0; _skipLines = _hurry || _skipper || _rng.NextDouble() < 0.3; _hitches = i % 2 == 1;
            _quitAt.Clear(); _quitDone.Clear();
            _quitAt.Add(1 + i % 10); _quitAt.Add(1 + (i + 5) % 10);         // every checkpoint 1-10 is quit at by >= 2 runs (in 10 runs)
            if (_rng.NextDouble() < 0.3) _quitAt.Add(1 + _rng.Next(10));
            _restartAt = i % 3 == 0 ? 1 + _rng.Next(7) : -1; _restartDone = false;
            _quits = _restarts = _fallsWanted = _wanders = _backtracks = _waits = 0; _hitchCount = 0;
            _fellShaft = _fellTerrace = _fellGap = false; _spottedOnPurpose = false;
            _shots = i < 2;
            _lastCp = -1;
            string habits = $"{(_hurry ? "hurry " : "")}{(_patient ? "patient " : "")}{(_backtracker ? "backtracker " : "")}{(_wanderer ? "wanderer " : "")}{(_skipper ? "skips-optional " : "")}{(_faller ? "falls " : "")}{(_skipLines ? "skips-lines " : "")}{(_hitches ? "hitches " : "")}";
            Log($"run {i + 1}/{n} starts: habits [{habits.Trim()}], quit+Continue at checkpoints [{string.Join(",", _quitAt)}], restart at {(_restartAt >= 0 ? _restartAt.ToString() : "-")}");
            int falls0 = Chapter1State.FallResets, wd0 = Chapter1State.Watchdogs, swd0 = Chapter1State.StoryWatchdogs, takeFb = 0, leash = 0;
            float t0 = Time.realtimeSinceStartup;
            // back to the main menu (between runs only - the run itself starts there, as a player's does)
            SaveGame.ClearTestSave(); Chapter1State.Reset();
            if (SceneNow != DemoMenu.SceneName) { SceneManager.LoadScene(DemoMenu.SceneName); yield return null; yield return null; }
            yield return MenuPick(l => l.StartsWith("Chapter 1"));
            _sig = null; _sigAt = Time.realtimeSinceStartup;
            string fail = null; bool shapeAtGalley = false; bool inGalleySeen = false;
            while (true)
            {
                yield return null;
                AttachCamera();
                // ---- bookkeeping: soft-lock watch (the story must move), stats, checkpoints
                string sig = $"{SceneNow}|{Chapter1State.Checkpoint}|{Obj}|{Chapter1State.Played.Count}|{(OnMoon ? D.StepName : "")}|{(Aboard ? Ab.Explored + "/" + Ab.InGalleyScene : "")}";
                if (sig != _sig) { _sig = sig; _sigAt = Time.realtimeSinceStartup; }
                if (OnMoon) { takeFb = Mathf.Max(takeFb, D.TakeFallbacks); leash = Mathf.Max(leash, D.Cat.Leashes); }
                if (Aboard && Ab.InGalleyScene && !inGalleySeen)
                {
                    inGalleySeen = true;
                    var cs = Ab.Director.Seven.GetComponent<CarriedShape>();
                    shapeAtGalley = cs != null && (cs.At == CarriedShape.Where.Pocket || cs.At == CarriedShape.Where.Hip) || Chapter1State.Explore;
                    Log($"run {i + 1}: in the galley - dodecahedron with her: {(cs != null ? cs.At.ToString() : "none")}");
                    if (_shots) yield return ShotNow($"pt{i + 1:00}_galley");
                }
                if (OnMoon && D.TakeFallbacks > 0 && !_fallbackShot) { _fallbackShot = true; yield return Wait(0.9f); yield return ShotNow("pt_take_fallback_telekinesis"); yield return Wait(0.8f); yield return ShotNow("pt_take_fallback_in_hand"); }
                if ((Aboard && Ab.CourseSetCount > 0) || SceneNow == "Venus") { if (_shots) yield return ShotNow($"pt{i + 1:00}_set_course_done"); if (SceneNow == "Venus") Log($"run {i + 1}: set course - the saucer flew on to Venus (Chapter 2 starts: the chapter is done)"); break; }   // Round 11+: Set course really travels to Venus
                if (Time.realtimeSinceStartup - _sigAt > 240f) { fail = $"SOFT-LOCK at '{_phaseName}' (objective '{Obj}', step '{(OnMoon ? D.StepName : "-")}', 7 at {P}, {(OnMoon ? S7.Current + "/" + S7.State : "")})"; break; }
                if (Time.realtimeSinceStartup - t0 > 1800f) { fail = $"TIMEOUT after 30 min at '{_phaseName}'"; break; }
                // ---- the menu (after a quit): Continue
                if (SceneNow == DemoMenu.SceneName) { yield return MenuPick(l => l.StartsWith("Continue")); continue; }
                int cp = OnMoon ? Chapter1State.Checkpoint : Aboard ? (Ab.Explored ? Chapter1State.CpExplore : Ab.InGalleyScene ? Chapter1State.CpGalley : Chapter1State.CpAboard) : -1;
                if (cp > _lastCp)
                {
                    if (_lastCp >= 0) Log($"run {i + 1}: checkpoint {cp} ({Chapter1State.CpName(cp)}) after {Time.realtimeSinceStartup - t0:0}s");
                    _lastCp = cp;
                    if (_shots && cp > 0) yield return ShotNow($"pt{i + 1:00}_cp{cp:00}");
                }
                // ---- quit to the main menu and Continue / restart from the checkpoint
                if (_quitAt.Contains(cp) && !_quitDone.Contains(cp) && (Controlling || cp == Chapter1State.CpGalley))
                {
                    _quitDone.Add(cp);
                    yield return Wait(0.5f + (float)_rng.NextDouble() * 2f);
                    yield return QuitAndContinue(cp);
                    continue;
                }
                if (OnMoon && cp == _restartAt && !_restartDone && Controlling && !D.Dlg.Busy && Free7)
                {
                    _restartDone = true; _restarts++;
                    Log($"run {i + 1}: restart from the checkpoint (d-pad down) at checkpoint {cp}, 7 at {P}");
                    Press(Down);
                    yield return WaitReal(2f);
                    continue;
                }
                // ---- skip lines (Y) like an impatient player
                if (_skipLines && (OnMoon ? D.Dlg.Busy : Aboard && Ab.Dlg.Busy) && _rng.NextDouble() < 0.03 && Idle) Press(Y);
                // ---- play
                if (OnMoon && Controlling) yield return MoonStep();
                else if (Aboard && Controlling) yield return AboardStep();
                else { _stick = Vector2.zero; }
            }
            _stick = Vector2.zero; _run = false;
            float secs = Time.realtimeSinceStartup - t0;
            bool pass = fail == null && shapeAtGalley;
            if (fail == null && !shapeAtGalley) fail = "reached the galley WITHOUT the dodecahedron";
            var line = $"playtest run {i + 1,2}/{n}: {(pass ? "PASS" : "FAIL")} in {secs / 60f:0.0} min{(fail != null ? " - " + fail : "")}; quit+Continue {_quits}x, restarts {_restarts}, deliberate falls {_fallsWanted} (fall resets {Chapter1State.FallResets - falls0}), wanders {_wanders}, backtracks {_backtracks}, waits {_waits}, take fallback {takeFb}, story watchdog (QF6, must be 0) {Chapter1State.StoryWatchdogs - swd0}, QF4 watchdogs (cell / cat marks) {Chapter1State.Watchdogs - wd0}, cat leash {leash}, hitches {_hitchCount}";
            Log(line);
            _results.Add(line);
            if (!pass) yield return ShotNow($"pt{i + 1:00}_FAIL");
        }

        IEnumerator Tap(GamepadButton b) { Press(b); for (float t0 = Time.realtimeSinceStartup; !Idle && Time.realtimeSinceStartup - t0 < 2f;) yield return null; yield return null; yield return null; }
        IEnumerator Wait(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }
        IEnumerator WaitReal(float s) { for (float t0 = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t0 < s;) yield return null; }
        IEnumerator Idle0(float s) { _stick = Vector2.zero; yield return Wait(s); }

        // ------------------------------------------------------------------------------------------ menus
        IEnumerator MenuPick(Func<string, bool> want)
        {
            for (float t0 = Time.realtimeSinceStartup; (DemoMenu.I == null || DemoMenu.I.SelectedLabel == null) && Time.realtimeSinceStartup - t0 < 10f;) yield return null;
            yield return WaitReal(0.6f);
            if (DemoMenu.I == null) yield break;
            for (int k = 0; k < 16 && DemoMenu.I != null && !want(DemoMenu.I.SelectedLabel ?? ""); k++) { yield return Tap(Down); }
            if (DemoMenu.I == null) yield break;
            if (!want(DemoMenu.I.SelectedLabel ?? ""))
            {
                Log($"main menu: the wanted entry is not there (no save?) - Chapter 1 instead");
                for (int k = 0; k < 16 && DemoMenu.I != null && !(DemoMenu.I.SelectedLabel ?? "").StartsWith("Chapter 1"); k++) { yield return Tap(Down); }
            }
            if (!_menuShot && (DemoMenu.I.SelectedLabel ?? "").StartsWith("Continue")) { _menuShot = true; yield return ShotNow("menu_continue_from_checkpoint"); }
            Log($"main menu: '{DemoMenu.I.SelectedLabel}' (A)");
            Press(A);
            // Round 13 (0c): "Chapter 1" with a save asks "Start a new game?" - a player confirms with A
            yield return WaitReal(0.5f);
            if (DemoMenu.I != null && SceneNow == DemoMenu.SceneName && DemoMenu.I.ConfirmingNewGame) { Log("main menu: 'Start a new game? Your saved progress will be reset.' (A) - confirms the new game"); Press(A); }
            for (float t0 = Time.realtimeSinceStartup; SceneNow == DemoMenu.SceneName && Time.realtimeSinceStartup - t0 < 8f;) yield return null;
            yield return WaitReal(1.0f);
        }

        IEnumerator PausePick(Func<string, bool> want)
        {
            for (int k = 0; k < 16 && PauseMenu.IsOpen && !want(PauseMenu.I.SelectedLabel ?? ""); k++) { yield return Tap(Down); }
            Log($"pause menu: '{PauseMenu.I.SelectedLabel}' (A)");
            Press(A);
            yield return WaitReal(0.3f);
        }

        IEnumerator QuitAndContinue(int cp)
        {
            _quits++;
            Log($"run {_run_i + 1}: quit to the main menu at checkpoint {cp} ({Chapter1State.CpName(cp)}), 7 at {P} -> Continue");
            _stick = Vector2.zero;
            // Round 13: presses still queued (line skips) used to delay Start past the wait - it then opened the pause
            // menu later, unseen, and the run stood still. Clear them first; wait a little longer.
            if (_presses.Count > 0) Log($"pause: {_presses.Count} queued presses dropped before Start");
            _presses.Clear();
            for (float t0 = Time.realtimeSinceStartup; _phase != 0 && Time.realtimeSinceStartup - t0 < 1f;) yield return null;
            Press(StartBtn);
            for (float t0 = Time.realtimeSinceStartup; !PauseMenu.IsOpen && Time.realtimeSinceStartup - t0 < 6f;) yield return null;
            if (!PauseMenu.IsOpen) { Log("pause menu did not open"); yield break; }
            yield return PausePick(l => l == "Main menu");
            yield return PausePick(l => l.StartsWith("Yes"));
            for (float t0 = Time.realtimeSinceStartup; SceneNow != DemoMenu.SceneName && Time.realtimeSinceStartup - t0 < 8f;) yield return null;
            yield return MenuPick(l => l.StartsWith("Continue"));
            Log($"continued: scene {SceneNow}, checkpoint {Chapter1State.Checkpoint}, explore {Chapter1State.Explore}, galley start {Chapter1State.GalleyStart}");
        }

        // ------------------------------------------------------------------------------------------ moving like a player
        IEnumerator Go(Vector3 to, float tol = 0.3f, float timeout = 25f, float speed = 1f)
        {
            for (float t0 = Time.time; DistF(P, to) > tol && Time.time - t0 < timeout;)
            {
                if (!Controlling) break;
                bool canMove = OnMoon ? S7.Current == ChapterSeven.Mode.Free || S7.Current == ChapterSeven.Mode.Look || S7.Current == ChapterSeven.Mode.Fight : Aboard;
                _stick = canMove ? StickFor((to - P).normalized * speed) : Vector2.zero;
                yield return null;
            }
            _stick = Vector2.zero;
        }

        IEnumerator Path(float tol, params Vector3[] pts)
        {
            // continue from the waypoint nearest to her (after a reset she may be part-way along)
            int start = 0; float best = float.MaxValue;
            for (int k = 0; k < pts.Length; k++) { float d = (pts[k] - P).magnitude; if (d < best) { best = d; start = k; } }
            for (int k = start; k < pts.Length; k++) { yield return Go(pts[k], tol); if (!Controlling) yield break; }
        }

        /// <summary>One traversal move: walk to the spot, lean into it, press, then wait until it is done (or retry).</summary>
        IEnumerator Move(string what, Vector3 from, Vector3 dir, GamepadButton btn, Func<bool> ok, float lean = 0.35f)
        {
            _phaseName = what;
            yield return Go(from, 0.2f, 20f, 0.6f);
            if (!Controlling || ok()) yield break;
            for (float t = 0f; t < 0.25f; t += Time.deltaTime) { _stick = StickFor(dir) * lean; yield return null; }
            Press(btn);
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { _stick = StickFor(dir) * lean; yield return null; }
            _stick = Vector2.zero;
            for (float t0 = Time.time; Time.time - t0 < 14f && !ok() && !(Free7 && Time.time - t0 > 2.5f);) yield return null;
        }

        IEnumerator Wander(float seconds, float radius)
        {
            _wanders++;
            var c = P;
            Log($"run {_run_i + 1}: wanders off the route around {c} ({seconds:0}s)");
            for (float t0 = Time.time; Time.time - t0 < seconds && Controlling;)
            {
                var to = c + new Vector3((float)(_rng.NextDouble() * 2 - 1) * radius, 0f, (float)(_rng.NextDouble() * 2 - 1) * radius);
                yield return Go(to, 0.4f, 2.5f);
            }
        }

        IEnumerator Waits(float max)
        {
            _waits++;
            float s = 2f + (float)_rng.NextDouble() * max;
            _stick = Vector2.zero;
            for (float t = 0f; t < s && Controlling; t += Time.deltaTime) { _look = t < 1.2f ? new Vector2(0.6f, 0f) : Vector2.zero; yield return null; }
            _look = Vector2.zero;
        }

        bool Chance(double p) => _rng.NextDouble() < p;
        bool _fallbackShot, _menuShot;
        float _nextHabit;
        readonly HashSet<string> _once = new HashSet<string>();
        bool Once(string key) { if (_once.Contains(_run_i + key)) return false; _once.Add(_run_i + key); return true; }

        // ------------------------------------------------------------------------------------------ the Moon level
        IEnumerator MoonStep()
        {
            var p = P; string obj = Obj ?? ""; var L = MoonLayout.Lower;
            _run = _hurry && !(S7.Crouched);
            // habits that do not depend on the spot
            bool habitDue = Time.time > _nextHabit;
            if (habitDue && ((_patient && Chance(0.01)) || Chance(0.002))) { _nextHabit = Time.time + 35f; yield return Waits(_patient ? 18f : 6f); yield break; }
            if (habitDue && _wanderer && Chance(0.01) && Free7 && !D.Dlg.Busy) { _nextHabit = Time.time + 35f; yield return Wander(4f + (float)_rng.NextDouble() * 5f, 2.0f); yield break; }

            // ---- hanging: what to do on each ledge
            if (S7.Current == ChapterSeven.Mode.Hang)
            {
                var l = S7.HangLedge;
                if (l == MoonTraversal.Bulkhead) { _phaseName = "climb the bulkhead"; for (float t = 0f; t < 3f && Hanging(l); t += Time.deltaTime) { _stick = StickFor(Vector3.right); yield return null; } _stick = Vector2.zero; yield break; }
                if (l == MoonTraversal.ShaftLip)
                {
                    _phaseName = "shimmy along the shaft lip";
                    bool fall = _faller && !_fellShaft;
                    float targetX = fall ? 14.6f : 12.4f;
                    for (float t = 0f; t < 14f && Hanging(l) && P.x > targetX; t += Time.deltaTime) { _stick = StickFor(Vector3.left); yield return null; }
                    _stick = Vector2.zero;
                    if (fall) { _fellShaft = true; _fallsWanted++; Log($"run {_run_i + 1}: lets go over the dark drop on purpose at x {P.x:0.0}"); }
                    if (Hanging(l)) { Press(B); yield return Wait(1.5f); }
                    yield break;
                }
                if (l == MoonTraversal.TerraceCLip)
                {
                    _phaseName = "terrace C lip";
                    if (_faller && !_fellTerrace) { _fellTerrace = true; _fallsWanted++; Log($"run {_run_i + 1}: lets go over the terrace gap on purpose"); Press(B); yield return Wait(1.5f); yield break; }
                    for (float t = 0f; t < 6f && Hanging(l) && S7.HangU > 0.5f; t += Time.deltaTime) { _stick = StickFor(Vector3.forward); yield return null; }
                    for (float t = 0f; t < 4f && Hanging(l); t += Time.deltaTime) { _stick = StickFor(Vector3.right); yield return null; }
                    _stick = Vector2.zero; yield break;
                }
                // the others are climb-downs: let go to the floor below
                _phaseName = "let go from " + l?.Name;
                yield return Wait(0.3f);
                if (Hanging(l)) { Press(B); yield return Wait(1.2f); }
                yield break;
            }
            if (!Free7 && S7.Current != ChapterSeven.Mode.Fight) { _stick = Vector2.zero; yield break; }

            int area = Chapter1Director.AreaOf(p);
            // Round 13: the QF9 terrace fight (fighting tutorial) - the driver never learned it, so the release run stopped
            // there. Like a player: walk up to the nearest Scree still standing, attack (X), dodge (B) his telegraphed
            // punch, take him down (Y) when he is dazed.
            if ((area == 5 || area == 6) && (obj.StartsWith("Get past the Scree") || S7.Current == ChapterSeven.Mode.Fight))
            {
                _phaseName = "the terrace fight";
                ScreeGuard foe = null; float fd = 1e9f;
                foreach (var f in FindObjectsByType<ScreeGuard>(FindObjectsInactive.Exclude))
                    if (f != null && f.InFight && !f.IsOut) { float dd = DistF(f.transform.position, P); if (dd < fd) { fd = dd; foe = f; } }
                if (foe == null) { yield return Idle0(0.3f); yield break; }
                var dv = foe.transform.position - P; dv.y = 0f;
                if (dv.magnitude > 1.6f) { yield return Go(foe.transform.position - dv.normalized * 1.25f, 0.4f, 2.5f); yield break; }
                _stick = StickFor(dv.normalized * 0.2f);
                if (foe.Dazed) { Press(Y); _fightTakedowns++; yield return Wait(1.4f); }
                else if (foe.FightState == ScreeGuard.Fight.Windup) { Press(B); yield return Wait(0.6f); }
                else { Press(X); _fightBlows++; yield return Wait(0.38f); }
                _stick = Vector2.zero;
                yield break;
            }
            // ---- 0: the cell
            if (area == 0 || (area == 1 && p.z < 2.4f && Mathf.Abs(p.x) < 1f && (obj.StartsWith("Look") || obj.StartsWith("Go to") || obj.StartsWith("Stand") || obj.StartsWith("Step"))))
            {
                _phaseName = "cell: " + obj;
                if (obj.StartsWith("Look around") || obj.StartsWith("Go to the cell door"))
                {
                    if (_patient && Once("cellwait")) { yield return Waits(10f); yield break; }
                    yield return Go(MoonLayout.DoorApproach, 0.25f);
                    if (D.PromptInRange && D.PromptLabel == "Look") { Press(X); yield return Wait(1f); }
                }
                else if (obj.StartsWith("Stand back")) yield return Go(new Vector3(0.1f, 0f, -0.35f), 0.25f);
                else if (obj.StartsWith("Step out")) yield return Path(0.3f, new Vector3(0f, 0f, 1.6f), new Vector3(0.1f, 0f, 2.8f));
                else yield return Idle0(0.2f);
                yield break;
            }
            // ---- 1: the cell block
            if (area == 1)
            {
                bool passageOpen = obj.StartsWith("Follow the old passage");
                if (p.y > 2.2f && p.x > 13.9f && p.x < 15.4f)
                { yield return Move("jump down from the bulkhead", new Vector3(14.95f, MoonTraversal.BulkheadTop, 3.3f), Vector3.right, A, () => FreeAt(0f) && P.x > 15.3f); yield break; }
                if (p.x < 8.4f)
                {
                    _phaseName = "corridor";
                    if (_backtracker && Once("corridor_back")) { _backtracks++; Log($"run {_run_i + 1}: backtracks along the cell block"); yield return Go(new Vector3(-6.4f, 0f, 3.4f), 0.4f, 20f); yield break; }
                    if (false && _faller && !_fellGap && p.x < 7.4f)       // (the gap's broken ends stop a short jump: no fall here)
                    {
                        _fellGap = true; _fallsWanted++;
                        Log($"run {_run_i + 1}: running jump short of the take-off spot (into the catwalk gap) on purpose");
                        yield return Go(new Vector3(6.9f, 0f, 3.3f), 0.2f);
                        _run = true; for (float t = 0f; t < 0.2f; t += Time.deltaTime) { _stick = StickFor(Vector3.right); yield return null; }
                        Press(A); for (float t = 0f; t < 0.4f; t += Time.deltaTime) { _stick = StickFor(Vector3.right); yield return null; }
                        _stick = Vector2.zero; _run = false; yield return Wait(2.5f); yield break;
                    }
                    yield return Go(new Vector3(7.6f, 0f, 3.3f), 0.3f);
                    if (_patient && D.Dlg.Busy && Once("listen3")) { while (D != null && D.Dlg.Busy && Controlling) { _stick = Vector2.zero; yield return null; } }
                    yield return Move("jump the catwalk gap", new Vector3(7.95f, 0f, 3.3f), Vector3.right, A, () => FreeAt(0f) && P.x > 10f);
                    yield break;
                }
                if (p.x < 13.2f && p.y < 0.5f)
                {
                    _phaseName = "pipes";
                    if (!S7.Crouched)
                    {
                        if (p.x > 11.3f) { Press(B); yield return Wait(0.5f); yield break; }
                        yield return Move("crouch at the pipes", new Vector3(10.95f, 0f, 3.3f), Vector3.right, B, () => S7.Crouched, 0.2f);
                        yield break;
                    }
                    yield return Go(new Vector3(13.7f, 0f, 3.3f), 0.12f, 25f, 0.8f);
                    yield break;
                }
                if (p.x < 14.05f && p.y < 0.5f)
                {
                    if (S7.Crouched && p.x < 13.55f) { yield return Go(new Vector3(13.7f, 0f, 3.3f), 0.1f, 10f, 0.8f); yield break; }
                    if (S7.Crouched) { Press(B); yield return Wait(0.6f); yield break; }
                    yield return Move("jump to grab the bulkhead", new Vector3(13.6f, 0f, 3.3f), Vector3.right, A, () => Hanging(MoonTraversal.Bulkhead));
                    yield break;
                }
                // past the bulkhead: the older wall, then the cracked wall
                if (passageOpen && p.x > 15.3f)
                { yield return Move("climb the cracked wall", new Vector3(16.5f, 0f, 0.15f), Vector3.back, A, () => FreeAt(2.8f), 0.3f); yield break; }
                _phaseName = "older wall";
                if (_wanderer && Once("gatewander")) { yield return Wander(5f, 1.5f); yield break; }
                yield return Go(MoonLayout.OldWallSpot, 0.35f);
                if (D.PromptInRange && D.PromptLabel == "Look closer") { Press(X); yield return Wait(1f); }
                else yield return Idle0(0.3f);
                yield break;
            }
            // ---- 2: the old passage + ruins
            if (area == 2)
            {
                if (p.y > 2.6f) { _phaseName = "shelf"; yield return Go(new Vector3(16.5f, 2.8f, -1.95f), 0.15f); if (Free7) { Press(B); yield return Wait(1.4f); } yield break; }
                if (Mathf.Abs(p.y - -1.6f) < 0.2f && p.x > 10.0f) { yield return Move("jump the ruins gap", new Vector3(10.7f, -1.6f, -3.6f), Vector3.left, A, () => FreeAt(-1.6f) && P.x < 9.3f); yield break; }
                if (Mathf.Abs(p.y - -1.6f) < 0.2f) { yield return Move("mantle onto terrace B", new Vector3(8.2f, -1.6f, -4.45f), Vector3.back, A, () => FreeAt(-0.7f)); yield break; }
                if (Mathf.Abs(p.y - -0.7f) < 0.2f) { yield return Move("jump to grab terrace C", new Vector3(9.3f, -0.7f, -6.3f), Vector3.right, A, () => Hanging(MoonTraversal.TerraceCLip)); yield break; }
                if (Mathf.Abs(p.y - 0.8f) < 0.2f && p.x < 13.85f && !D.SlabMoved)
                {
                    _phaseName = "the fallen slab";
                    yield return Go(new Vector3(12.9f, 0.8f, -5.45f), 0.3f);
                    if (D.PromptInRange && D.PromptIsTk) { Press(RT); yield return Wait(3.5f); }
                    else yield return Idle0(0.3f);
                    yield break;
                }
                _phaseName = "stair down to the hall";
                yield return Path(0.35f, new Vector3(13.2f, 0.8f, -5.25f), new Vector3(14.2f, 0.45f, -5.2f), new Vector3(17.9f, L, -5.2f), new Vector3(18.6f, L, -5.9f),
                    new Vector3(17.6f, L, -7.3f), new Vector3(16.5f, L, -8.1f), MoonLayout.HallEntry);
                yield break;
            }
            // ---- 3: the Scree hall
            if (area == 3)
            {
                _phaseName = "Scree hall";
                var g = D.HallGuard;
                if (_patient && !_spottedOnPurpose && g.Current == ScreeGuard.Mode.Post && !D.StoneDown)
                {
                    _spottedOnPurpose = true; Log($"run {_run_i + 1}: walks straight at the guard first (gets spotted -> reset)");
                    for (float t0 = Time.time; Time.time - t0 < 12f && Controlling;) { _stick = StickFor(new Vector3(16.5f, 0f, -16.5f) - P); yield return null; }
                    _stick = Vector2.zero; yield break;
                }
                if (!D.StoneDown && !g.Distracted)
                {
                    if (D.PromptInRange && D.PromptIsTk) { Press(RT); yield return Wait(2.5f); }
                    else yield return Go(MoonLayout.HallEntry, 0.4f, 6f);
                    yield break;
                }
                if (g.Distracted && (g.Current == ScreeGuard.Mode.Studying || g.Current == ScreeGuard.Mode.ToNoise && _hurry))
                {
                    yield return Path(0.35f, MoonLayout.SneakPath);
                    yield break;
                }
                if (D.StoneDown && !g.Distracted && p.z > -12f)
                {
                    // the stone is spent and the guard is back: let him see her (reset puts the stone back)
                    for (float t0 = Time.time; Time.time - t0 < 10f && Controlling;) { _stick = StickFor(new Vector3(16.5f, 0f, -16.5f) - P); yield return null; }
                    _stick = Vector2.zero; yield break;
                }
                if (p.z < -12f) { yield return Path(0.35f, MoonLayout.SneakPath); yield break; }
                yield return Idle0(0.2f);
                yield break;
            }
            // ---- 4: the vision chamber (and the corridor into it)
            if (area == 4)
            {
                if (p.z > -29.6f) { _phaseName = "into the chamber"; yield return Go(new Vector3(16.5f, L, -30.6f), 0.4f); yield break; }
                // QF7: "Take the shape" from the start - walking up to it is enough (the vision plays as she touches it)
                if (obj.StartsWith("Take the shape"))
                {
                    _phaseName = "take the shape (walk up to it)";
                    if (_wanderer && Once("chamberwander")) { yield return Wander(5f, 2.5f); yield break; }
                    yield return Path(0.25f, new Vector3(18.6f, L, -33.6f), MoonLayout.ShapeSpot);
                    yield return Wait(1.5f);
                    yield break;
                }
                // QF7: the lock is buried under the collapse: telekinesis (RT) once in range
                if (obj.StartsWith("Uncover the lock"))
                {
                    _phaseName = "uncover the lock (RT)";
                    if (D.PromptInRange && D.PromptIsTk) { Press(RT); yield return Wait(3.5f); yield break; }
                    yield return Go(new Vector3(15.6f, L, -35.2f), 0.4f);
                    yield break;
                }
                if (obj.StartsWith("Open the door")) { _phaseName = "touch the lock"; yield return Go(MoonLayout.MechanismSpot, 0.25f); if (D.PromptInRange) { Press(X); yield return Wait(1f); } yield break; }
                if (obj.StartsWith("Find the way out") || obj.StartsWith("Keep going"))
                {
                    _phaseName = "out of the chamber";
                    yield return Path(0.2f, new Vector3(16.5f, L, -36.9f), new Vector3(16.5f, L, -38.85f), new Vector3(16.5f, L, -39.12f));
                    if (Free7 && P.z < -38.9f) { Press(B); yield return Wait(1.4f); }
                    yield break;
                }
                yield return Idle0(0.3f);
                yield break;
            }
            // ---- 5: the mining cut (and the doorway in front of it)
            if (area == 5)
            {
                if (p.y > -3f && p.z > -39.45f) { _phaseName = "climb down into the cut"; yield return Go(new Vector3(16.5f, L, -39.12f), 0.15f); if (Free7) { Press(B); yield return Wait(1.4f); } yield break; }
                if (p.y < -5.5f) { yield return Move("climb out of the cut", new Vector3(16.5f, -6.4f, -40.75f), Vector3.back, A, () => FreeAt(L) && P.z < -41.2f, 0.3f); yield return Wait(1f); yield break; }
                _phaseName = "out onto the terrace";
                yield return Path(0.35f, new Vector3(16.5f, L, -42.2f), new Vector3(16.5f, L, -43.8f));
                yield break;
            }
            // ---- 6: terrace, ramp, scaffolds
            if (area == 6)
            {
                if (_wanderer && obj.StartsWith("Get down") && Once("terracewander")) { yield return Wander(5f, 2.5f); yield break; }
                if (p.y > -2.6f && p.z > -50.4f) { _phaseName = "to the broken ramp"; yield return Path(0.3f, new Vector3(21.5f, L, -47f), new Vector3(21.5f, L, -50f), new Vector3(21.5f, -4.3f, -54.62f)); yield break; }
                if (p.y > -4.6f && p.z > -55.05f) { _phaseName = "ramp end"; yield return Go(new Vector3(21.5f, -4.3f, -54.62f), 0.15f); if (Free7) { Press(B); yield return Wait(1.4f); } yield break; }
                if (Mathf.Abs(p.y - -7.6f) < 0.25f && p.z > -58.8f) { yield return Move("jump to scaffold 2", new Vector3(21.5f, -7.6f, -57.8f), Vector3.back, A, () => FreeAt(-7.6f) && P.z < -59.4f); yield break; }
                if (Mathf.Abs(p.y - -7.6f) < 0.25f) { _phaseName = "scaffold edge"; yield return Go(new Vector3(21.5f, -7.6f, -62.05f), 0.15f); if (Free7) { Press(B); yield return Wait(1.4f); } yield break; }
                yield return Idle0(0.3f);
                yield break;
            }
            // ---- 7: the bay floor
            if (area == 7)
            {
                if (obj.StartsWith("Board the saucer"))
                {
                    _phaseName = "into the light";
                    if (_backtracker && Once("baywalk")) { _backtracks++; Log($"run {_run_i + 1}: walks off across the bay first"); yield return Go(new Vector3(4f, MoonLayout.Bay, -88f), 0.6f, 20f); yield break; }
                    yield return Path(0.3f, MoonLayout.BeamApproach, MoonLayout.BeamEdge);
                    yield break;
                }
                _phaseName = "the release lever";
                yield return Path(0.3f, new Vector3(21.5f, MoonLayout.Bay, -66f), new Vector3(21.5f, MoonLayout.Bay, -72f), MoonLayout.LeverSpot);
                if (D.PromptInRange && D.PromptIsTk) { Press(RT); yield return Wait(3f); }
                yield break;
            }
            yield return Idle0(0.2f);
        }

        // ------------------------------------------------------------------------------------------ aboard
        IEnumerator AboardStep()
        {
            var dir = Ab.Director; string obj = Obj ?? "";
            _run = false;
            if (Ab.InGalleyScene) { _stick = Vector2.zero; yield break; }
            if (!Ab.Explored)
            {
                _phaseName = "find the kitchen";
                if (_wanderer && Once("deckwander")) { yield return Wander(5f, 2f); yield break; }
                yield return Path(0.3f, new Vector3(-4.5f, 0f, -3.6f), Ab.FridgeSpot);
                if (dir.PromptOverride == "Open the fridge") { Press(X); yield return Wait(1.5f); }
                yield break;
            }
            _phaseName = "set course";
            yield return Go(Ab.SeatStandPos, 0.3f, 25f, 0.8f);
            if (dir.ExtraPrompt == "Set course") { Press(X); yield return Wait(2f); }
            else yield return Idle0(0.3f);
        }

        // ------------------------------------------------------------------------------------------ Scene Select test
        IEnumerator SceneSelectAll()
        {
            _rng = new System.Random(77); _hurry = true; _skipLines = true;
            int pass = 0, total = 0;
            for (int cp = Chapter1State.CpCell; cp <= Chapter1State.CpGalley; cp++)
            {
                total++;
                _run_i = cp; _quitAt.Clear(); _restartAt = -1;
                SaveGame.ClearTestSave(); Chapter1State.Reset();
                bool viaPause = cp == Chapter1State.CpCity;          // one entry from the pause menu, in the middle of the chapter
                if (viaPause)
                {
                    // start the chapter from the main menu, then open the pause menu in the cell and pick the entry there
                    if (SceneNow != DemoMenu.SceneName) { SceneManager.LoadScene(DemoMenu.SceneName); yield return null; yield return null; }
                    yield return MenuPick(l => l.StartsWith("Chapter 1"));
                    for (float t0 = Time.realtimeSinceStartup; !(OnMoon && Controlling) && Time.realtimeSinceStartup - t0 < 20f;) yield return null;
                    Press(StartBtn);
                    for (float t0 = Time.realtimeSinceStartup; !PauseMenu.IsOpen && Time.realtimeSinceStartup - t0 < 3f;) yield return null;
                    yield return PausePick(l => l == "Scene select");
                    if (cp == Chapter1State.CpCity) yield return ShotNow("ss_pause_menu_scene_select");
                    string want = Chapter1State.CpName(cp);
                    yield return PausePick(l => l == want);
                }
                else
                {
                    if (SceneNow != DemoMenu.SceneName) { SceneManager.LoadScene(DemoMenu.SceneName); yield return null; yield return null; }
                    yield return MenuPick(l => l == "Scene select");
                    if (cp == 0) yield return ShotNow("ss_main_menu_scene_select_page");
                    string want = Chapter1State.CpName(cp);
                    for (int k = 0; k < 16 && DemoMenu.I != null && DemoMenu.I.SelectedLabel != want; k++) { yield return Tap(Down); }
                    Log($"scene select: '{DemoMenu.I?.SelectedLabel}' (A)");
                    Press(A);
                }
                for (float t0 = Time.realtimeSinceStartup; !(OnMoon || Aboard) && Time.realtimeSinceStartup - t0 < 15f;) yield return null;
                yield return Wait(1.5f);
                AttachCamera();
                string state = OnMoon
                    ? $"Moon checkpoint {Chapter1State.Checkpoint}, 7 at {P}, shape {(D.ShapeTaken ? "with her (" + (S7.GetComponent<CarriedShape>()?.At.ToString() ?? "?") + ")" : "on the pedestal")}, slab {(D.SlabMoved ? "moved" : "in place")}, cat {D.Cat.DistanceTo7:0.0} m away ({D.Cat.Current}), objective '{Obj}', step '{D.StepName}'"
                    : Aboard ? $"aboard, galley start {Chapter1State.GalleyStart}, shape {(Ab.Director.Seven.GetComponent<CarriedShape>()?.At.ToString() ?? "none")}, objective '{Obj}'" : $"scene {SceneNow}";
                Log($"scene select entry {cp} '{Chapter1State.CpName(cp)}'{(viaPause ? " (from the pause menu)" : "")} starts: {state}");
                yield return ShotNow($"ss{cp:00}_start");
                // play on until the next entry is reached
                float t1 = Time.realtimeSinceStartup; bool ok = false; _sig = null; _sigAt = t1; _lastCp = cp;
                while (Time.realtimeSinceStartup - t1 < 900f)
                {
                    yield return null;
                    AttachCamera();
                    int now = OnMoon ? Chapter1State.Checkpoint : Aboard ? (Ab.Explored ? Chapter1State.CpExplore : Ab.InGalleyScene ? Chapter1State.CpGalley : Chapter1State.CpAboard) : -1;
                    if (cp == Chapter1State.CpGalley ? (Aboard && Ab.CourseSetCount > 0) || SceneNow == "Venus" : now > cp) { ok = true; break; }
                    string sig = $"{SceneNow}|{Chapter1State.Checkpoint}|{Obj}|{Chapter1State.Played.Count}";
                    if (sig != _sig) { _sig = sig; _sigAt = Time.realtimeSinceStartup; }
                    if (Time.realtimeSinceStartup - _sigAt > 240f) break;
                    if (_skipLines && (OnMoon ? D.Dlg.Busy : Aboard && Ab.Dlg.Busy) && _rng.NextDouble() < 0.03 && Idle) Press(Y);
                    if (OnMoon && Controlling) yield return MoonStep();
                    else if (Aboard && Controlling) yield return AboardStep();
                    else _stick = Vector2.zero;
                }
                _stick = Vector2.zero;
                if (ok) pass++;
                var line = $"scene select {cp,2} '{Chapter1State.CpName(cp)}': {(ok ? "PASS" : "FAIL")} - started, played through to {(cp == Chapter1State.CpGalley ? "'Set course'" : "'" + Chapter1State.CpName(cp + 1) + "'")} in {(Time.realtimeSinceStartup - t1) / 60f:0.0} min{(ok ? "" : " (stuck at '" + _phaseName + "', objective '" + Obj + "', 7 at " + P + ")")}";
                Log(line); _results.Add(line); Flush();
            }
            SaveGame.ClearTestSave();
            Cap.Finish($"SCENE SELECT TEST: {pass}/{total} entries start and play through to the next (Venus: 'later', not selectable)");
        }
    }
}
