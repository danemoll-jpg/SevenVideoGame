using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// QF38 - the WHOLE game as a player, from a NEW save, in ONE run (`-wholegame mars,asteroid -playtest 5 -playtestfrom 4
    /// -testspeed 2 -capture dir`; the other order `-wholegame europa,biaveh`). P2 / P3's "full run" was ten separate runs, each
    /// starting from a state its switch faked (WorldFlags.FakeForTest, -routes, -catcp) - so a stale flag, a save left from an
    /// earlier playthrough, a Scene Select slot or a talk that reads the real completion order could never show. Here:
    ///  - the branch table check runs first (BranchAudit), then "new game" on the test keys (SaveGame.NewGame);
    ///  - the chapter bots (the Moon's run 5, Venus 1 + 2, Mars, Europa, the asteroid, the mothership, Cattalus, the finale) are
    ///    chained on that one save: each is added when its chapter really starts (its arrival scene loads), and its "done" line
    ///    (ChapterCapture.Finish) hands on to the next instead of quitting;
    ///  - between chapters this pilot plays the aboard part itself on a virtual pad: VEN_C13's choice, the walk to the console,
    ///    the star map (d-pad + A), TRV_AST's choice - always the world the order names, from what the map really offers;
    ///  - nothing is faked or teleported; every piece's summary, the soft-locks and the skips go into one WHOLE GAME line.
    /// </summary>
    public class WholeGameRun : MonoBehaviour
    {
        public static bool Active => DemoBoot.WholeGame != null && DemoBoot.CaptureDir != null;
        static WholeGameRun _i;
        static string[] Order => (DemoBoot.WholeGame ?? "mars,asteroid").Split(',');
        public static string First => Order.Length > 0 && Order[0] == "europa" ? "europa" : "mars";
        public static string Second => First == "mars" ? "europa" : "mars";
        public static string PairFirst => Order.Length > 1 && Order[1] == "biaveh" ? "biaveh" : "asteroid";
        public static string PairSecond => PairFirst == "asteroid" ? "biaveh" : "asteroid";
        static readonly string[] Pieces = { "moon", "venus1", "venus2", "first", "second", "pair1", "pair2", "cattalus", "finale" };
        static int _piece;
        static string PieceWorld(int i)
        {
            if (i < 0 || i >= Pieces.Length) return null;
            switch (Pieces[i]) { case "first": return First; case "second": return Second; case "pair1": return PairFirst; case "pair2": return PairSecond; default: return Pieces[i]; }
        }
        static readonly List<string> _sums = new List<string>();
        static int _softs, _skips;
        static float _t0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (DemoBoot.CaptureDir != null && DemoBoot.BranchAuditOnly)
            {
                var g = new GameObject("BranchAuditOnly"); DontDestroyOnLoad(g); g.AddComponent<WholeGameRun>()._auditOnly = true; return;
            }
            if (!Active || _i != null) return;
            // a NEW save (the test keys): every chapter, the world flags, the saucer's place, the menu's chapter unlocks
            if (DemoBoot.WholeGameFrom <= 0) { SaveGame.NewGame(); SaveGame.ClearTestSave(); }
            var go = new GameObject("WholeGameRun"); DontDestroyOnLoad(go); _i = go.AddComponent<WholeGameRun>();
            SceneManager.sceneLoaded += (sc, m) => { if (_i != null) _i.OnScene(sc.name); };
        }
        bool _auditOnly;

        ChapterCapture Cap => ChapterCapture.I;
        void Log(string s) { Debug.Log("[Seven] whole game: " + s); Cap?.Log("WHOLE GAME (QF38): " + s); }
        static Chapter1Aboard S => Chapter1Aboard.I != null && Chapter1Aboard.I.isActiveAndEnabled ? Chapter1Aboard.I : null;

        Gamepad _pad; Vector2 _stick; readonly Queue<GamepadButton> _presses = new Queue<GamepadButton>(); GamepadButton _cur; int _phase;
        GameObject _driver; bool _waitingForStart;
        const GamepadButton A = GamepadButton.South, X = GamepadButton.West, Y = GamepadButton.North;

        IEnumerator Start()
        {
            ChapterCapture.Ensure();
            if (_auditOnly) { yield return null; var r = BranchAudit.Run(); foreach (var l in r.Split('\n')) Cap.Log(l); Cap.Finish(r.Split('\n')[0]); yield break; }
            _t0 = Time.time;
            if (DemoBoot.WholeGameFrom > 0)
            {
                _piece = Mathf.Clamp(DemoBoot.WholeGameFrom, 3, Pieces.Length - 1);
                Log($"DEBUG start at piece {_piece + 1} ({PieceWorld(_piece)}) on a checkpoint switch's save - NOT a proof run");
                while (true) { yield return null; if (_driver == null) AboardPilot(); }
            }
            var audit = BranchAudit.Run(); foreach (var l in audit.Split('\n')) Cap.Log(l);
            SaveGame.NewGame(); SaveGame.ClearTestSave();         // the audit used the test keys: start the playthrough from nothing again
            Log($"a NEW save, then the whole game in the order {First} -> {Second}, {PairFirst} -> {PairSecond}, Cattalus, the finale ({(DemoBoot.CatLose ? "a sloppy race: Duchess wins" : "the race to win")}); {WorldFlags.Summary()}; the finale save {(FinaleState.OwnComplete ? "COMPLETE (stale!)" : "empty")}; the saucer at {SaucerLocation.Saved} ({SaucerLocation.System})");
            _piece = 0;
            Log("piece 1 / 9: the Moon (Chapter 1's run 5 from the main menu)");
            float lastChange = Time.time; int lastPiece = 0; string lastScene = SceneManager.GetActiveScene().name;
            while (true)
            {
                yield return null;
                if (_piece != lastPiece || SceneManager.GetActiveScene().name != lastScene) { lastPiece = _piece; lastScene = SceneManager.GetActiveScene().name; lastChange = Time.time; }
                if (Time.time - lastChange > 1800f) { _softs++; Log($"SOFT-LOCK: piece {Pieces[Mathf.Min(_piece, Pieces.Length - 1)]} - no piece finished and no scene change for 30 game-min (scene {lastScene})"); lastChange = Time.time; FinishAll("stuck"); yield break; }
                if (_driver != null || _waitingForStart && _driver != null) continue;
                if (_piece <= 2) continue;                          // the Moon / Venus bots run back to back
                AboardPilot();
            }
        }

        // ---------------------------------------------------------------- the aboard part between chapters (the pilot's own pad)
        float _nextAct, _diagT;
        void AboardPilot()
        {
            if (Time.time < _nextAct) return;
            if (_pad == null) { _pad = InputSystem.AddDevice<Gamepad>("WholeGamePad"); }
            _pad.MakeCurrent();
            var con = ShipConsole.I; var s = S;
            var ui = FindAnyObjectByType<ChapterUI>();
            if (ui != null && ui.SubtitleVisible && Random.value < 0.04f) { Press(Y); _nextAct = Time.time + 0.4f; return; }
            if (con == null || s == null) { _stick = Vector2.zero; return; }
            string want = PieceWorld(_piece);
            if (con.Map != null && (con.Choosing || con.Current == ShipConsole.State.Using && con.CurrentPage == ShipConsole.Page.Map))
            {
                _stick = Vector2.zero;
                want = MapWant(con, want);
                if (con.Map.Selected != want) { Press(con.Map.IsGateway(con.Map.Selected) && StarMap.SystemOf(want) != con.Map.View ? A : GamepadButton.DpadRight); _nextAct = Time.time + 0.6f; }
                else { Log($"star map: {want} highlighted ({(con.Choosing ? "the story's choice" : "the console")}) - A, A; {con.Map.SelectableIds().Count} selectable [{string.Join(", ", con.Map.SelectableIds())}]"); Press(A); Press(A); _nextAct = Time.time + 3f; _waitingForStart = true; }
                return;
            }
            if (Time.time > _diagT)
            {
                _diagT = Time.time + 30f;
                var sv = con.Director != null ? con.Director.Seven : null;
                Log($"aboard pilot: explored {s.Explored}, galley scene {s.InGalleyScene}, console {con.Current} busy {con.Busy} near {con.NearConsole}, 7 {(sv != null ? $"{sv.Current} frozen {sv.Frozen} cutscene {sv.Cutscene} scripted {sv.ScriptDriven} sofa {sv.OnSofa} at {sv.transform.position} - {Vector3.Distance(sv.transform.position, con.StandSpot.position):0.0} m from the console" : "none")}, pad {(Gamepad.current != null ? Gamepad.current.name : "none")}, stick {_stick}, story lock {ShipConsole.StoryLock}, pause {PauseMenu.IsOpen}");
            }
            if (s.Explored && !s.InGalleyScene && (con.Current == ShipConsole.State.Using || !con.Busy))
            {
                if (con.Current == ShipConsole.State.Using && con.CurrentPage == ShipConsole.Page.Main)
                {
                    int idx = ShipConsole.MainLabels().IndexOf("Star map");
                    for (int k = 0; k < idx; k++) Press(GamepadButton.DpadDown);
                    Press(A); _nextAct = Time.time + 2.5f; Log($"console open - d-pad down {idx}x to the star map, A (for {want})"); return;
                }
                if (con.Current == ShipConsole.State.Off)
                {
                    var p7 = con.Director.Seven.transform.position;
                    if (con.NearConsole) { _stick = Vector2.zero; Press(X); _nextAct = Time.time + 1.5f; Log("X at the console"); }
                    else StickToward(con.StandSpot.position, p7);
                    return;
                }
            }
            _stick = Vector2.zero;
        }
        void Press(GamepadButton b) { if (_presses.Count < 8) _presses.Enqueue(b); }
        Vector3 _lastPos; float _lastMoveT, _sideUntil, _side = 1f;
        void StickToward(Vector3 at, Vector3 from)
        {
            var dir = at - from; dir.y = 0f; var cam = Camera.main != null ? Camera.main.transform : null;
            if (cam == null || dir.magnitude < 0.3f) { _stick = Vector2.zero; return; }
            var cf = cam.forward; cf.y = 0f; cf.Normalize(); var cr = new Vector3(cf.z, 0f, -cf.x); dir.Normalize();
            // blocked (no headway for 1.2 s - a table, a chair, Panther in the way): step round it for a moment, alternating sides,
            // as the chapter bots do (rc2's first B run stood 3.1 m from the console pushing into the galley table for 15 min)
            var fl = from; fl.y = 0f;
            if (Time.time > _sideUntil)
            {
                if ((fl - _lastPos).magnitude > 0.3f) { _lastPos = fl; _lastMoveT = Time.time; }
                else if (Time.time - _lastMoveT > 1.2f) { _side = -_side; _sideUntil = Time.time + 1.4f; _lastMoveT = Time.time; Log("aboard pilot: blocked - steps round it"); }
            }
            if (Time.time < _sideUntil) dir = (Vector3.Cross(Vector3.up, dir) * _side * 0.85f + dir * 0.35f).normalized;
            _stick = new Vector2(Vector3.Dot(dir, cr), Vector3.Dot(dir, cf)) * 0.8f;
        }
        void Update()
        {
            if (_pad == null || _driver != null || _piece <= 2) return;
            if (Gamepad.current != _pad) _pad.MakeCurrent();
            if (_phase == 0 && _presses.Count > 0) { _cur = _presses.Dequeue(); _phase = 1; }
            uint bits = 0u; if (_phase >= 1 && _phase <= 3) bits |= 1u << (int)_cur;
            if (_phase > 0) _phase = _phase >= 7 ? 0 : _phase + 1;
            InputSystem.QueueStateEvent(_pad, new GamepadState { leftStick = _stick, buttons = bits });
        }

        /// <summary>What a chained run picks on the star map: the world the order names if the map offers it, else the first
        /// world it offers (the story's single choice: AST_11 / BIA_13's next destination, the Moon after CAT_12).</summary>
        public static string MapWant(ShipConsole con, string want)
        {
            if (!Active || con == null || con.Map == null) return want;
            var sel = con.Map.SelectableIds();
            string mine = _i != null ? PieceWorld(_piece) : null;
            if (mine != null && sel.Contains(mine)) return mine;
            if (want != null && sel.Contains(want)) return want;
            return sel.Count > 0 ? sel[0] : want;
        }

        // ---------------------------------------------------------------- pieces
        string _arrivedEarly;     // the next chapter's arrival loaded while the last piece's bot was still finishing
        static bool Arrival(string w, string scene) =>
              w == "mars" ? MarsState.ArrivalAboard || scene == "Mars"
            : w == "europa" ? EuropaState.ArrivalAboard || scene == "Europa"
            : w == "asteroid" ? AsteroidState.ArrivalAboard || scene == "Asteroid"
            : w == "biaveh" ? BiavehState.ArrivalAboard || scene == "Biaveh"
            : w == "cattalus" ? CattalusState.ArrivalAboard || scene == "Cattalus"
            : false;
        void OnScene(string scene)
        {
            if (_piece < Pieces.Length - 1 && _driver != null && Arrival(PieceWorld(_piece + 1), scene))
            {
                // the next chapter's arrival loaded while this piece's bot was still in a wait (Mars's bot picks at TRV_AST, then the
                // scene loads before it sees its own 'done' flag): the piece is over - close it, start the next bot now
                _arrivedEarly = PieceWorld(_piece + 1);
                PieceDone($"{PieceWorld(_piece)} complete - the next world's arrival loaded straight from its departure choice ({WorldFlags.Summary()})");
                return;
            }
            if (_piece <= 2 || _piece >= Pieces.Length || _driver != null) return;
            string w = PieceWorld(_piece);
            bool starts = Arrival(w, scene);
            if (starts) StartDriver($"its arrival ({scene} loaded)");
        }
        void StartDriver(string why)
        {
            string p = Pieces[_piece], w = PieceWorld(_piece);
            _stick = Vector2.zero; _presses.Clear(); _phase = 0; _waitingForStart = false;
            if (_pad != null) InputSystem.QueueStateEvent(_pad, new GamepadState());
            DemoBoot.ChainAs(w);
            var go = new GameObject("WholeGame_" + p); DontDestroyOnLoad(go); _driver = go;
            switch (w)
            {
                case "venus1": go.AddComponent<VenusPlaytest>(); break;
                case "venus2": go.AddComponent<VenusPlaytest2>(); break;
                case "mars": go.AddComponent<MarsPlaytest>(); break;
                case "europa": go.AddComponent<EuropaPlaytest>(); break;
                case "asteroid": go.AddComponent<AsteroidPlaytest>(); break;
                case "biaveh": go.AddComponent<BiavehPlaytest>(); break;
                case "cattalus": go.AddComponent<CattalusPlaytest>(); break;
                case "finale": go.AddComponent<FinalePlaytest>(); break;
            }
            Log($"piece {_piece + 1} / 9: {w} - its bot starts ({why}); {WorldFlags.Summary()}; the saucer at {SaucerLocation.Saved}");
        }

        /// <summary>ChapterCapture.Finish: a piece's done line. True = the run goes on (the next piece); false = really finish.</summary>
        public static bool Intercept(string summary)
        {
            if (!Active || _i == null || _finishing) return false;
            return _i.PieceDone(summary);
        }
        static bool _finishing;
        bool PieceDone(string summary)
        {
            string p = _piece < Pieces.Length ? PieceWorld(_piece) : "?";
            _sums.Add($"{p}: {summary}");
            int soft = CountAfter(summary, "soft-locks "); _softs += Mathf.Max(0, soft);
            int sk = CountAfter(summary, "skips taken "); if (sk < 0) sk = CountAfter(summary, "skips "); _skips += Mathf.Max(0, sk);
            Log($"piece {_piece + 1} / 9 done ({p}): {summary}");
            if (_driver != null) { Destroy(_driver); _driver = null; }
            else { var pr = FindAnyObjectByType<PlaytestRun>(); if (pr != null) Destroy(pr.gameObject); }
            PauseMenu.ScriptInput = false;
            _piece++;
            if (_piece >= Pieces.Length) { FinishAll("the credits rolled"); return true; }
            string next = PieceWorld(_piece);
            if (next == "venus1" || next == "venus2" || next == "finale") StartDriver("straight on");
            else if (_arrivedEarly == next) { _arrivedEarly = null; StartDriver("its arrival had already loaded"); }
            else { _waitingForStart = false; Log($"next: {next} - the pilot plays the aboard part until its arrival loads"); }
            return true;
        }
        static int CountAfter(string s, string key)
        {
            int i = s.IndexOf(key); if (i < 0) return -1; i += key.Length; int j = i; while (j < s.Length && char.IsDigit(s[j])) j++;
            return j > i && int.TryParse(s.Substring(i, j - i), out int n) ? n : -1;
        }
        void FinishAll(string why)
        {
            _finishing = true;
            foreach (var l in _sums) Cap?.Log("WHOLE GAME (QF38) piece " + l);
            bool all = _piece >= Pieces.Length;
            var line = $"WHOLE GAME (QF38): {(all && _softs == 0 ? "PASS" : "FAIL")} - order {First} -> {Second}, {PairFirst} -> {PairSecond}; pieces {Mathf.Min(_piece, Pieces.Length)} / {Pieces.Length}; soft-locks {_softs}; skips taken {_skips}; {(Time.time - _t0) / 60f:0.0} game-min; {why}; {WorldFlags.Summary()}; finale {(FinaleState.OwnComplete ? "done" : "NOT done")}";
            Cap?.Finish(line);
        }
    }
}
