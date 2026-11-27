using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Seven
{
    /// <summary>
    /// Round 18: the player-style run of Cattalus (-catplay; a copy of BiavehPlaytest). Starts free aboard in Veyra's system with
    /// every world before it done: a virtual gamepad on the real input actions walks to the console, X, the star map (d-pad to
    /// Cattalus, A, A), CAT_01 (the stick steers the saucer down to the guide ring over the public berth), the whole chapter on
    /// foot with Panther (the prompts and their buttons, the guide light + marker), the Grand Circuit ridden by the road ahead
    /// (the left stick toward the course a little way on, RT; a clean rider times the toys - with -catlose a sloppy one: lower
    /// throttle, no timing, so Duchess wins and CAT_08L plays), CAT_09-11, CAT_12 aboard. It reads only what a player sees.
    /// With -catcp n it starts at that checkpoint (the second outcome run). Ends "CATTALUS PLAYTEST: 1/1, soft-locks N, ...".
    /// </summary>
    public class CattalusPlaytest : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (DemoBoot.CaptureDir == null || !DemoBoot.CatPlay || FindAnyObjectByType<CattalusPlaytest>() != null) return;
            var go = new GameObject("CattalusPlaytest"); DontDestroyOnLoad(go); go.AddComponent<CattalusPlaytest>();
        }

        Gamepad _pad; Vector2 _stick; float _rt, _lt; uint _held;
        readonly Queue<GamepadButton> _presses = new Queue<GamepadButton>(); GamepadButton _cur; int _phase;
        const GamepadButton X = GamepadButton.West, Y = GamepadButton.North, A = GamepadButton.South;
        ChapterCapture Cap => ChapterCapture.I;
        void Log(string s) => Cap?.Log("Cattalus playtest: " + s);
        static CattalusDirector D => CattalusDirector.I != null && CattalusDirector.I.isActiveAndEnabled ? CattalusDirector.I : null;
        static Chapter1Aboard S => Chapter1Aboard.I != null && Chapter1Aboard.I.isActiveAndEnabled ? Chapter1Aboard.I : null;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        int _soft, _skips, _xs, _races; float _t0;
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
        /// <summary>The race: steer at the road a little way on (what a player follows), full throttle unless the corner ahead is
        /// tighter than her speed; a clean rider waits for a toy's swing to pass, a sloppy one (-catlose) doesn't and rides softer.</summary>
        void RacePad(CattalusRace r, HoverBike bike)
        {
            bool slow = DemoBoot.CatLose;
            var aimP = r.PilotAim; if (!slow && r.ToyAhead(45f)) aimP += Vector3.Cross(Vector3.up, CattalusCourse.Dir(CattalusCourse.IndexAtDist(r.HerDist + 12f))) * 5.8f;   // a player keeps to the side the swinging toys don't reach
            var f = Flat(aimP - bike.transform.position);
            float err = Vector3.SignedAngle(Flat(bike.transform.forward), f, Vector3.up);
            _stick = new Vector2(Mathf.Clamp(err / 22f, -1f, 1f), 0f);
            float corner = CattalusCourse.CornerSpeed(r.HerIndex, bike.MaxSpeed) * (slow ? 0.78f : 1.04f);
            _rt = bike.Speed > corner ? 0f : slow ? 0.72f : 1f; _lt = bike.Speed > corner + 2f ? 0.6f : 0f;
            if (!slow && r.Toys != null) for (int i = 0; i < r.Toys.Length; i++)
            {
                float ahead = CattalusCourse.Dist[r.ToyIdx[i]] - CattalusCourse.Dist[r.HerIndex]; if (ahead < 0f) ahead += CattalusCourse.Length;
                if (ahead > 4f && ahead < 26f) { float eta = ahead / Mathf.Max(4f, bike.Speed); float sw = CattalusCourse.ToySwing(Time.time + eta, r.ToyPeriod[i], r.ToyPhase[i]); if (Mathf.Abs(sw) < 0.45f) { _rt = 0f; _lt = bike.Speed > 9f ? 0.8f : 0f; } }
            }
        }

        IEnumerator Start()
        {
            ChapterCapture.Ensure();
            while (S == null && D == null) yield return null;
            _pad = InputSystem.AddDevice<Gamepad>("CattalusPlayPad"); _pad.MakeCurrent();
            _t0 = Time.time;
            Log($"virtual gamepad on the real input actions; muted={DemoBoot.Muted} listenerVolume={AudioListener.volume}; {WorldFlags.Summary()}; outcome wanted: {(DemoBoot.CatLose ? "runner-up (a sloppy rider)" : "a clean race")}");
            float stuckSince = Time.time; string sig = null; bool consoleDone = DemoBoot.CatCp >= 0;
            while (true)
            {
                yield return null;
                var ui = FindAnyObjectByType<ChapterUI>();
                if (ui != null && ui.SubtitleVisible && _phase == 0 && _presses.Count == 0 && Random.value < 0.04f) Press(Y);
                var con = ShipConsole.I;
                if (con != null && con.Map != null && (con.Choosing || con.Current == ShipConsole.State.Using && con.CurrentPage == ShipConsole.Page.Map))
                {
                    _stick = Vector2.zero; _rt = 0f; _held = 0u;
                    if (con.Map.Selected != WholeGameRun.MapWant(con, "cattalus")) { if (_phase == 0 && _presses.Count == 0) { Press(con.Map.View == StarMap.Sol && con.Map.IsGateway(con.Map.Selected) ? A : GamepadButton.DpadRight); yield return WaitR(0.6f); } }
                    else { Log("star map: Cattalus highlighted - A, A"); Press(A); yield return WaitR(0.6f); Press(A); yield return WaitR(1.6f); consoleDone = true; }
                    continue;
                }
                var s = S;
                if (s != null && s.CattalusMode && s.CattalusHomeDone) break;
                // CAT_01: the stick steers the saucer down to the guide ring over the berth
                if (s != null && s.CattalusMode && s.GuidingTo.HasValue && s.GuidingSaucer != null)
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
                if (d.SkipOffered) { _skips++; Log($"skip offered on '{d.WaitingFor}' - taken"); Press(Y); yield return WaitR(1.5f); continue; }
                if (d.Race != null && d.Race.Running) { RacePad(d.Race, d.Bike); continue; }
                _held = 0u; _lt = 0f;
                if (d.InCutscene) { _stick = Vector2.zero; _rt = 0f; continue; }
                var seven = d.Seven.transform.position;
                if (!string.IsNullOrEmpty(d.Prompt))
                {
                    if (_phase == 0 && _presses.Count == 0 && !(d.Prompt == "Ride" && !d.WantsBike) && d.Prompt != "Get off") { _stick = Vector2.zero; _rt = 0f; Press(X); _xs++; if (d.Prompt == "Start the race") _races++; Log($"X at '{d.Prompt}'"); yield return WaitR(0.6f); continue; }
                }
                var m = d.NextMarker;
                if (d.Riding)
                {
                    var bike = d.Bike.transform;
                    if (!m.HasValue) { _stick = Vector2.zero; _rt = 0f; _lt = 0.4f; continue; }
                    var f = Flat(m.Value - bike.position); float err = Vector3.SignedAngle(Flat(bike.forward), f, Vector3.up);
                    if (f.magnitude < 5f) { _stick = Vector2.zero; _rt = 0f; _lt = 1f; continue; }
                    _stick = new Vector2(Mathf.Clamp(err / 30f, -1f, 1f), 0f); _rt = Mathf.Abs(err) > 75f ? 0.2f : Mathf.Clamp(f.magnitude / 14f, 0.35f, 1f);
                }
                else
                {
                    _rt = 0f;
                    if (!m.HasValue) { _stick = Vector2.zero; continue; }
                    if (Flat(m.Value - seven).magnitude < 0.5f) { _stick = Vector2.zero; continue; }
                    StickToward(m.Value, seven);
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
            Cap.Finish($"CATTALUS PLAYTEST: 1/1, soft-locks {_soft}, skips taken {_skips}, X presses {_xs}, races started {_races}, ({(Time.time - _t0) / 60f:0.0} game-min) - {CattalusState.Summary()}; {WorldFlags.Summary()}");
        }
        void OnDestroy() { if (_pad != null && _pad.added) InputSystem.RemoveDevice(_pad); }
    }
}
