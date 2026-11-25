using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Seven
{
    /// <summary>
    /// Round 20 check runs (with -capture): -gcproof = the AI on every vehicle (two of each) completes 3 laps on both tracks
    /// (falls, fall-throughs, walls, the loop / corkscrew minimum speed, the jump, the shortcut); -gcsplit = the setup pages played
    /// with two virtual pads (player 2 presses A to join), then a 2-player split-screen race against 4 AI, the two human slots
    /// driven by the bot THROUGH those pads, frame time measured; -gcmenu = every page captured; -gctrial = a time trial, then a
    /// second with its ghost. Captures go through the image gate; every on-screen string is checked for keyboard keys.
    /// </summary>
    public class GcCaptureRun : MonoBehaviour
    {
        public GcGame Game; ChapterCapture C; int _n;
        readonly Gamepad[] _pads = new Gamepad[2]; readonly GcAI[] _bots = new GcAI[2];
        readonly Dictionary<GcRacer, float> _minAssist = new Dictionary<GcRacer, float>(); readonly Dictionary<GcRacer, int> _loops = new Dictionary<GcRacer, int>(), _corks = new Dictionary<GcRacer, int>(), _jumps = new Dictionary<GcRacer, int>(), _cuts = new Dictionary<GcRacer, int>();
        Camera _obs; int _rideN, _rideOff; float _hipsWorst, _palmWorst;

        IEnumerator Start()
        {
            C = ChapterCapture.Ensure(); GcSave.ResetTest();
            Log($"GC TEST (R20): proof {GcBoot.Proof}, split {GcBoot.Split}, menu {GcBoot.MenuShots}, trial {GcBoot.Trial}; test speed {DemoBoot.TestSpeed}");
            yield return null;
            if (GcBoot.MenuShots) yield return MenuRun();
            if (GcBoot.Proof) yield return ProofRun();
            if (GcBoot.Split) yield return SplitRun();
            if (GcBoot.Trial) yield return TrialRun();
            Log(ImageGate.Summary(DemoBoot.CaptureDir));
            Log(GlyphSummary());
            C.Finish("GC TEST (R20) done");
        }
        void Log(string s) => GcGame.Log(s);

        // ------------------------------------------------------------------------------------------ captures
        IEnumerator Shot(string name, Transform subject, string what, params Camera[] cams)
        {
            cams = cams.Where(c => c != null && c.isActiveAndEnabled).ToArray(); if (cams.Length == 0) yield break;
            yield return new WaitForEndOfFrame();
            var rt = ImageGate.NewRT();
            var prev = cams.Select(c => c.targetTexture).ToArray();
            foreach (var c in cams) c.targetTexture = rt;
            if (subject != null) ImageGate.Expect(subject, what, 1f);
            yield return new WaitForEndOfFrame();
            _n++; var file = $"{_n:00}_{name}.png";
            CheckGlyphs(name);
            Log(ImageGate.Save(rt, cams[0], DemoBoot.CaptureDir, file));
            for (int i = 0; i < cams.Length; i++) cams[i].targetTexture = prev[i];
            rt.Release(); Destroy(rt);
        }
        Camera Observer()
        {
            if (_obs != null) return _obs;
            var go = Instantiate(Game.CamTemplate.gameObject, transform); go.name = "ObserverCam"; go.SetActive(true); _obs = go.GetComponent<Camera>(); _obs.rect = new Rect(0, 0, 1, 1);
            if (go.GetComponent<AudioListener>() == null) go.AddComponent<AudioListener>();
            go.AddComponent<GcCam>(); return _obs;
        }
        IEnumerator SideShot(string name, GcRacer r, Vector3 offsetLocal, float fov = 55f)
        {
            var cam = Observer(); var gc = cam.GetComponent<GcCam>(); gc.enabled = false;
            var p = r.transform.position + r.transform.rotation * offsetLocal;
            cam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(r.transform.position - p, r.transform.up)); cam.fieldOfView = fov;
            yield return Shot(name, Subj(r), r.Label + " racing", cam);
            gc.enabled = true;
        }

        // ------------------------------------------------------------------------------------------ the AI proof
        IEnumerator ProofRun()
        {
            int ok = 0, total = 0, falls = 0, through = 0; float worstMin = 999f;
            foreach (var id in GcTrack.Ids)
            {
                GcGame.NextMode = GcGame.Mode.Race; GcGame.NextTrack = id; GcGame.NextRacers = new int[0]; GcGame.NextPads = new int[0];
                _minAssist.Clear(); _loops.Clear(); _corks.Clear(); _jumps.Clear(); _cuts.Clear();
                var run = StartCoroutine(Game.RaceRun());
                while (Game.Racers.Count == 0 || !Game.Running) yield return null;
                var cam = Observer(); var gc = cam.GetComponent<GcCam>(); gc.Target = Game.Racers[0]; gc.Snap();
                foreach (var r in Game.Racers) { var rr = r; r.Event += (x, e) => Count(x, e); }
                yield return Shot($"gc_{id}_grid_go", Subj(Game.Racers[0]), "the grid at the lights", cam);
                bool loopShot = false, corkShot = false, jumpShot = false, cutShot = false, landShot = false, packShot = false, sideLoop = false;
                float t0 = Time.time;
                while (!Game.Over)
                {
                    foreach (var r in Game.Racers)
                    {
                        var f = r.On.Flags(r.S);
                        if ((f & GcTrack.F.Assist) != 0 && !r.Air && r.RespawnT <= 0f) { _minAssist.TryGetValue(r, out var m); _minAssist[r] = m == 0f ? r.V : Mathf.Min(m, r.V); }
                    }
                    // captures from the observer following a racer through the features
                    var focus = Game.Racers.OrderBy(r => r.Slot).FirstOrDefault(r => !r.Finished) ?? Game.Racers[0];
                    if (gc.Target != focus) { gc.Target = focus; gc.Snap(); }
                    var ff = focus.On.Flags(focus.S); float into = 0f;
                    if (!loopShot && (ff & GcTrack.F.Loop) != 0 && Upside(focus)) { loopShot = true; yield return Shot($"gc_{id}_loop_upside_down", Subj(focus), "racing upside down in the loop", cam); }
                    if (!sideLoop && loopShot && (ff & GcTrack.F.Loop) != 0 && !Upside(focus)) { sideLoop = true; yield return SideShot($"gc_{id}_loop_side", focus, new Vector3(-9f, 2.5f, 1f), 60f); }
                    if (!corkShot && (ff & GcTrack.F.Cork) != 0 && Upside(focus)) { corkShot = true; yield return Shot($"gc_{id}_corkscrew", Subj(focus), "rolling through the corkscrew", cam); }
                    if (!jumpShot && focus.Air && focus.AirTime > 0.5f) { jumpShot = true; yield return Shot($"gc_{id}_the_jump", Subj(focus), "in the air over the gap", cam); yield return SideShot($"gc_{id}_the_jump_side", focus, new Vector3(-9f, -1f, 2f), 60f); }
                    if (jumpShot && !landShot && !focus.Air && focus.Landings > 0) { landShot = true; yield return Shot($"gc_{id}_landing", Subj(focus), "landed after the jump", cam); }
                    if (!packShot && Time.time - t0 > 6f) { packShot = true; yield return Shot($"gc_{id}_the_pack", Subj(focus), "the pack", cam); }
                    var cutter = Game.Racers.FirstOrDefault(r => r.OnShortcut && r.S > 20f && r.S < r.On.Length - 30f);
                    if (!cutShot && cutter != null) { cutShot = true; gc.Target = cutter; gc.Snap(); yield return null; yield return null; yield return Shot($"gc_{id}_shortcut", Subj(cutter), "through the shortcut", cam); }
                    if (Time.time - t0 > 3f) foreach (var rd in FindObjectsByType<GcSevenRider>(FindObjectsSortMode.None)) { _rideN++; _hipsWorst = Mathf.Max(_hipsWorst, rd.HipsGap); _palmWorst = Mathf.Max(_palmWorst, Mathf.Max(rd.PalmGaps.x, rd.PalmGaps.y)); if (rd.HipsGap > 0.05f || Mathf.Max(rd.PalmGaps.x, rd.PalmGaps.y) > 0.05f) _rideOff++; }
                    if (Time.time - t0 > 1200f) break;
                    yield return null;
                }
                while (Game.Results == null) yield return null;
                foreach (var r in Game.Racers)
                {
                    total++; bool done = r.Finished && r.Lap > Game.Laps; if (done) ok++;
                    falls += r.Falls; through += r.FallThroughs; _minAssist.TryGetValue(r, out var mn); if (mn > 0f) worstMin = Mathf.Min(worstMin, mn);
                    Log($"GC PROOF (R20) {id}: {r.Label} (slot {r.Slot}, {r.Stats.Vehicle}) {(done ? "3 laps in " + GcGame.Fmt(r.FinishTime) : "DID NOT FINISH (lap " + r.Lap + ")")}, place {r.Place}; laps {string.Join(" / ", r.LapTimes.Select(GcGame.Fmt))}; falls {r.Falls}, fall-throughs {r.FallThroughs}, wall hits {r.WallHits}, bumps {r.Bumps}, loops {Get(_loops, r)}, corkscrews {Get(_corks, r)}, jumps {Get(_jumps, r)} (landed {r.Landings}), shortcuts {Get(_cuts, r)}, pickups {r.PadsTaken} (boosts {r.Boosts}, pushed {r.PushesHit}, shielded {r.Shielded}), slowest on a loop / the corkscrew {mn:0.0} m/s");
                }
                Game.Results = null; Game.Quit(); yield return null;
            }
            Log($"GC RIDE POSE (R20): 7 on her bike, {_rideN} samples - hips off the saddle point / a palm off its grip by more than 5 cm in {_rideOff} (worst hips {_hipsWorst * 100f:0.0} cm, palm {_palmWorst * 100f:0.0} cm)");
            Log($"GC PROOF (R20): {ok} / {total} AI runs completed 3 laps (2 tracks x 6 racers, two of each vehicle); falls-through {through} (must be 0); falls off {falls}; slowest racer on a loop / the corkscrew {worstMin:0.0} m/s (the assist holds {GcRacer.MinAssistSpeed:0} m/s)");
        }
        static Transform Subj(GcRacer r) => r.Visual != null ? r.Visual : r.transform;
        static bool Upside(GcRacer r) => Vector3.Dot(r.transform.up, Vector3.up) < -0.3f;
        void Count(GcRacer r, string e)
        {
            if (e == "jump") Inc(_jumps, r); if (e == "shortcut") Inc(_cuts, r);
            var f = r.On.Flags(r.S);
        }
        void LateUpdate()
        {
            // loops / corkscrews passed: counted on the way out (a racer that left an Assist section upright)
            foreach (var r in Game.Racers)
            {
                if (r == null || r.On == null) continue;
                var f = r.On.Flags(r.S); _inLoop.TryGetValue(r, out var was);
                var now = (f & GcTrack.F.Loop) != 0 ? 1 : (f & GcTrack.F.Cork) != 0 ? 2 : 0;
                if (was != 0 && now == 0) { if (was == 1) Inc(_loops, r); else Inc(_corks, r); }
                _inLoop[r] = now;
            }
            Tick();
        }
        readonly Dictionary<GcRacer, int> _inLoop = new Dictionary<GcRacer, int>();
        static void Inc(Dictionary<GcRacer, int> d, GcRacer r) { d.TryGetValue(r, out var n); d[r] = n + 1; }
        static int Get(Dictionary<GcRacer, int> d, GcRacer r) { d.TryGetValue(r, out var n); return n; }

        // ------------------------------------------------------------------------------------------ virtual pads
        uint _held0, _held1; readonly GcControls[] _want = new GcControls[2]; readonly bool[] _pressA = new bool[2], _pressX = new bool[2]; readonly int[] _dpad = new int[2];
        void MakePads()
        {
            for (int i = 0; i < 2; i++) if (_pads[i] == null) _pads[i] = InputSystem.AddDevice<Gamepad>("GcTestPad" + (i + 1));
        }
        void Tick()
        {
            for (int i = 0; i < 2; i++)
            {
                var p = _pads[i]; if (p == null || !p.added) continue;
                var w = _want[i]; uint bits = 0;
                // (the throttle / brake on the triggers only: A / B held as throttle pressed the menus behind the race)
                if (w.AirL) bits |= 1u << (int)GamepadButton.LeftShoulder; if (w.AirR) bits |= 1u << (int)GamepadButton.RightShoulder;
                if (_pressA[i]) { bits |= 1u << (int)GamepadButton.South; _pressA[i] = false; }
                if (_pressX[i] || w.Use) { bits |= 1u << (int)GamepadButton.West; _pressX[i] = false; }
                if (_dpad[i] != 0) { bits |= 1u << (int)(_dpad[i] < 0 ? GamepadButton.DpadLeft : GamepadButton.DpadRight); _dpad[i] = 0; }
                InputSystem.QueueStateEvent(p, new GamepadState { leftStick = new Vector2(w.Steer, 0f), rightTrigger = w.Throttle, leftTrigger = w.Brake, buttons = bits });
            }
        }
        IEnumerator Press(int pad, bool a = true, int dpad = 0) { if (a) _pressA[pad] = true; _dpad[pad] = dpad; yield return null; yield return null; _want[pad] = default; yield return null; yield return null; }

        // ------------------------------------------------------------------------------------------ the split-screen race
        IEnumerator SplitRun()
        {
            MakePads(); GcGame.SideBySide = GcBoot.Side;
            var m = Game.Menu; m.Open(); yield return Wait(1f);
            // the real pages: player 1 presses A on "Single race"; player 2 presses A to join; each picks a racer; the track
            m.Pads[0] = _pads[0];
            yield return Press(0); yield return Wait(0.5f);
            Log($"GC SPLIT (R20): page {m.PageName}");
            yield return Press(1); yield return Wait(0.5f);
            Log($"GC SPLIT (R20): after player 2 pressed A - players {m.Players} (page {m.PageName})");
            if (m.Players < 2) m.Join(_pads[1]);
            yield return MenuShot("gc_menu_join_two_players");
            yield return Press(0); yield return Wait(0.5f);   // "Race"
            // racer select: player 1 keeps 7, player 2 moves right once to Duchess; each presses A
            yield return Press(1, false, 1); yield return Wait(0.3f);
            yield return MenuShot("gc_menu_racer_select_two_players");
            yield return Press(0); yield return Wait(0.3f); yield return Press(1); yield return Wait(0.6f);
            Log($"GC SPLIT (R20): racers chosen P1 {GcStats.Of(m.Pick[0]).Racer}, P2 {GcStats.Of(m.Pick[1]).Racer} (page {m.PageName})");
            if (m.PageName != "Track") { m.Choose(0, 0); m.Choose(1, 1); m.Show(GcMenu.Page.Track); }
            yield return MenuShot("gc_menu_track_select");
            // the bot drives both human slots through the pads
            _bots[0] = new GcAI(77, 0.92f); _bots[1] = new GcAI(78, 0.88f);
            var drive = StartCoroutine(DriveHumans());
            yield return Press(0); yield return Wait(1f);
            while (!Game.Running) yield return null;
            var cams = Game.Cams.Select(c => c.GetComponent<Camera>()).ToArray();
            Log($"GC SPLIT (R20): {cams.Length} player views, {(GcGame.SideBySide ? "side by side" : "top / bottom")} - viewports {string.Join(", ", cams.Select(c => c.rect.ToString()))}; the humans {string.Join(", ", Game.Humans.Select(h => h.Label + " pad " + h.PadIndex))}");
            yield return Wait(4f); yield return Shot("gc_split_race_start", Subj(Game.Humans[0]), "player 1 in the split screen", cams);
            float t0 = Time.time; bool loop = false, jump = false, item = false, mid = false;
            while (!Game.Over && Time.time - t0 < 400f)
            {
                var h0 = Game.Humans[0]; var h1 = Game.Humans[1];
                // (the gate measures the subject in the first view - player 1's - so these wait for player 1's own moment)
                if (!loop && (h0.On.Flags(h0.S) & GcTrack.F.Loop) != 0 && Upside(h0)) { loop = true; yield return Shot("gc_split_loop", Subj(h0), "player 1 in the loop", cams); }
                if (!jump && h0.Air && h0.AirTime > 0.4f) { jump = true; yield return Shot("gc_split_jump", Subj(h0), "player 1 over the jump", cams); }
                if (!item && (h0.Item != GcPickup.None || h1.Item != GcPickup.None)) { item = true; yield return Shot("gc_split_pickup_held", Subj(h0), "player 1", cams); }
                if (!mid && Time.time - t0 > 70f) { mid = true; yield return Shot("gc_split_mid_race", Subj(h0), "player 1", cams); }
                yield return null;
            }
            while (Game.Results == null) yield return null;
            var ms = Game.FrameMs.OrderBy(x => x).ToList();
            if (ms.Count > 0) Log($"GC SPLIT FRAME TIME (R20): {ms.Count} race frames, mean {ms.Average():0.0} ms ({1000f / ms.Average():0} fps), p95 {ms[(int)(ms.Count * 0.95f)]:0.0} ms, frames over 16.7 ms {ms.Count(x => x > 16.7f) * 100f / ms.Count:0.0} % (2 player views + 4 AI, the check run's window minimized at 1280x720)");
            foreach (var h in Game.Humans) Log($"GC SPLIT (R20): player {Game.Humans.IndexOf(h) + 1} ({h.Label}) place {h.Place}, {GcGame.Fmt(h.FinishTime)}, falls {h.Falls}, fall-throughs {h.FallThroughs}, pickups used {h.PadsTaken}, shortcut taken {(h.OnShortcut ? "yes" : "-")}");
            yield return Wait(1.5f);
            yield return MenuShot("gc_menu_results");
            StopCoroutine(drive); _want[0] = _want[1] = default;
            m.Close(); Game.Quit(); yield return null;
        }
        IEnumerator DriveHumans()
        {
            while (true)
            {
                if (Game.Running)
                    for (int i = 0; i < Game.Humans.Count && i < 2; i++) { var h = Game.Humans[i]; _want[i] = _bots[i].Drive(h, Game.Racers, Time.deltaTime); }
                yield return null;
            }
        }
        IEnumerator Wait(float s) { for (float t = 0f; t < s; t += Time.unscaledDeltaTime) yield return null; }
        IEnumerator MenuShot(string name)
        {
            var cam = Game.Menu.GetComponentsInChildren<Camera>(true).FirstOrDefault(c => c.name == "MenuCam");
            if (cam == null) yield break;
            yield return Shot(name, null, null, cam);
        }

        // ------------------------------------------------------------------------------------------ menus
        IEnumerator MenuRun()
        {
            var m = Game.Menu; m.Open(); yield return Wait(1.5f);
            yield return MenuShot("gc_menu_title");
            GcGame.NextMode = GcGame.Mode.Race; m.Show(GcMenu.Page.Join); yield return Wait(0.5f); yield return MenuShot("gc_menu_join");
            m.Show(GcMenu.Page.Racer); yield return Wait(0.5f); yield return MenuShot("gc_menu_racer_7");
            m.Choose(0, 1); m.Show(GcMenu.Page.Racer); m.Pick[0] = 1; yield return Wait(0.3f); yield return MenuShot("gc_menu_racer_duchess");
            m.Pick[0] = 2; m.Show(GcMenu.Page.Racer); yield return Wait(0.3f); yield return MenuShot("gc_menu_racer_tunneler");
            m.Show(GcMenu.Page.Track); yield return Wait(1f); yield return MenuShot("gc_menu_track_cattalus");
            m.Show(GcMenu.Page.Records); yield return Wait(0.5f); yield return MenuShot("gc_menu_best_times");
            m.Show(GcMenu.Page.Title);
        }

        // ------------------------------------------------------------------------------------------ the time trial + its ghost
        IEnumerator TrialRun()
        {
            MakePads(); GcPads.Device[0] = _pads[0]; _bots[0] = new GcAI(91, 0.9f);
            var drive = StartCoroutine(DriveHumans());
            for (int k = 0; k < 2; k++)
            {
                GcGame.NextMode = GcGame.Mode.TimeTrial; GcGame.NextTrack = "cattalus"; GcGame.NextRacers = new[] { 0 }; GcGame.NextPads = new[] { 0 };
                Game.Results = null;
                var run = StartCoroutine(Game.RaceRun());
                while (!Game.Running) yield return null;
                var cams = Game.Cams.Select(c => c.GetComponent<Camera>()).ToArray();
                yield return Wait(k == 0 ? 8f : 9f);
                yield return Shot(k == 0 ? "gc_trial_first_run" : "gc_trial_with_ghost", Subj(Game.Humans[0]), "7 in the time trial", cams);
                while (Game.Results == null) yield return null;
                Log($"GC TRIAL (R20): run {k + 1}: {GcGame.Fmt(Game.Humans[0].FinishTime)}, laps {string.Join(" / ", Game.Humans[0].LapTimes.Select(GcGame.Fmt))}; best saved {GcGame.Fmt(GcSave.Best("cattalus", true))}; ghost file {(File.Exists(GcSave.GhostPath("cattalus")) ? "saved" : "MISSING")}");
                Game.Quit(); yield return null;
            }
            StopCoroutine(drive); _want[0] = default;
        }

        // ------------------------------------------------------------------------------------------ no keyboard glyphs on screen
        int _glyphChecks, _glyphBad; readonly List<string> _glyphWhat = new List<string>();
        static readonly string[] KeyWords = { "Esc", "Enter", "Space", "Shift", "Ctrl", "Tab", "Backspace", "WASD", "Arrow", "Keyboard", "Mouse", "LMB", "RMB" };
        void CheckGlyphs(string shot)
        {
            foreach (var t in FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))
            {
                if (!t.isActiveAndEnabled || string.IsNullOrEmpty(t.text)) continue; _glyphChecks++;
                foreach (var k in KeyWords) if (System.Text.RegularExpressions.Regex.IsMatch(t.text, $@"\b{k}\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) { _glyphBad++; if (_glyphWhat.Count < 6) _glyphWhat.Add($"{shot}: '{t.text}'"); }
                // single keyboard letters shown as a button (A / B / X / Y are the pad's own buttons, never flagged)
                if (System.Text.RegularExpressions.Regex.IsMatch(t.text, @"\b[CDEFGHQRSTW]\s*(/|=)\s", System.Text.RegularExpressions.RegexOptions.None)) { _glyphBad++; if (_glyphWhat.Count < 6) _glyphWhat.Add($"{shot}: '{t.text}'"); }
            }
        }
        string GlyphSummary() => $"GC PROMPTS (R20): {_glyphChecks} on-screen texts checked at the captures, keyboard keys shown {_glyphBad} (must be 0){(_glyphWhat.Count > 0 ? " - " + string.Join(" | ", _glyphWhat) : "")}";
        void OnDestroy() { foreach (var p in _pads) if (p != null && p.added) InputSystem.RemoveDevice(p); }
    }
}
