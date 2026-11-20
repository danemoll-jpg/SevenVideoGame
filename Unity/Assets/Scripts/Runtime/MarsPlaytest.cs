using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Seven
{
    /// <summary>
    /// Round 15: the player-style run of the Mars chapter (-marsplay -capture dir; add -menupick "Venus|Venus: the debrief"
    /// to start in the real main menu and choose Mars on the star map at VEN_C13, or -marscp n to start at a checkpoint).
    /// A virtual gamepad on the real input actions - no autopilot, no teleports. It reads only what a player sees: the
    /// subtitles (Y skips a line like a fast reader), the star map (d-pad to the wanted world, A, A), the objective, the
    /// guide light (MarsDirector.NextMarker) and the context prompt (X to interact, RT for telekinesis - the prompt says
    /// which). Riding: left stick toward the light, RT forward, LT to brake near it. On foot: the stick toward the light;
    /// at the moving section it waits until it is open (as a player would watch it). A skip offered after the wait is taken
    /// (counted). Ends "MARS PLAYTEST: 1/1, soft-locks N, skips S" once the destination choice aboard is done.
    /// </summary>
    public class MarsPlaytest : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (DemoBoot.CaptureDir == null || !DemoBoot.MarsPlay || FindAnyObjectByType<MarsPlaytest>() != null) return;
            var go = new GameObject("MarsPlaytest"); DontDestroyOnLoad(go); go.AddComponent<MarsPlaytest>();
        }

        Gamepad _pad; Vector2 _stick; float _rt, _lt;
        readonly Queue<GamepadButton> _presses = new Queue<GamepadButton>(); GamepadButton _cur; int _phase;
        bool _holdRT; int _rtPhase;
        const GamepadButton X = GamepadButton.West, Y = GamepadButton.North, A = GamepadButton.South;
        void Press(GamepadButton b) { if (_presses.Count < 3) _presses.Enqueue(b); }
        ChapterCapture Cap => ChapterCapture.I;
        void Log(string s) => Cap?.Log("mars playtest: " + s);
        static MarsDirector D => MarsDirector.I != null && MarsDirector.I.isActiveAndEnabled ? MarsDirector.I : null;
        static Chapter1Aboard S => Chapter1Aboard.I != null && Chapter1Aboard.I.isActiveAndEnabled ? Chapter1Aboard.I : null;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        int _soft, _skipsTaken, _tks, _xs; float _t0;

        void Update()
        {
            if (_pad == null) return;
            if (Gamepad.current != _pad) _pad.MakeCurrent();
            if (_phase == 0 && _presses.Count > 0) { _cur = _presses.Dequeue(); _phase = 1; }
            uint bits = 0u;
            if (_phase >= 1 && _phase <= 3) bits |= 1u << (int)_cur;
            if (_phase > 0) _phase = _phase >= 6 ? 0 : _phase + 1;
            // RT taps (telekinesis): pressed for 3 frames, released for 3
            float rt = _rt;
            if (_holdRT) { rt = _rtPhase < 3 ? 1f : 0f; _rtPhase = (_rtPhase + 1) % 8; }
            InputSystem.QueueStateEvent(_pad, new GamepadState { leftStick = _stick, rightTrigger = rt, leftTrigger = _lt, buttons = bits });
        }

        IEnumerator Start()
        {
            ChapterCapture.Ensure();
            while (S == null && D == null) yield return null;
            _pad = InputSystem.AddDevice<Gamepad>("MarsPlayPad"); _pad.MakeCurrent();
            _t0 = Time.time;
            Log($"virtual gamepad on the real input actions; muted={DemoBoot.Muted} listenerVolume={AudioListener.volume}; started {(DemoBoot.MenuPick != null ? "from the main menu: " + DemoBoot.MenuPick : "directly")}");
            float stuckSince = Time.time; string sig = null;
            string want = DemoBoot.MarsPick ?? "asteroid";
            while (true)
            {
                yield return null;
                var ui = FindAnyObjectByType<ChapterUI>();
                if (ui != null && ui.SubtitleVisible && _phase == 0 && _presses.Count == 0 && Random.value < 0.08f) Press(Y);
                var con = ShipConsole.I;
                if (con != null && con.Choosing && con.Map != null)
                {
                    _stick = Vector2.zero; _rt = 0f; _holdRT = false;
                    string target = S != null && S.MarsMode ? want : "mars";
                    if (con.Map.Selected != target) { if (_phase == 0 && _presses.Count == 0) { Press(GamepadButton.DpadRight); yield return WaitR(0.4f); } }
                    else { Log($"star map: {target} highlighted - A, A"); Press(A); yield return WaitR(0.6f); Press(A); yield return WaitR(1.5f); }
                    continue;
                }
                var s = S;
                if (s != null && s.MarsMode && s.MarsDebriefDone) break;
                var d = D;
                if (d == null) { _stick = Vector2.zero; _rt = 0f; _lt = 0f; _holdRT = false; continue; }
                if (d.SkipOffered) { _skipsTaken++; Log($"skip offered on '{d.WaitingFor}' - taken"); Press(Y); yield return WaitR(1.5f); continue; }
                if (d.InCutscene) { _stick = Vector2.zero; _rt = 0f; _holdRT = false; continue; }
                // the prompt: X (interact) or RT (telekinesis)
                if (!string.IsNullOrEmpty(d.Prompt))
                {
                    if (d.PromptBtn == ControlsLegend.Btn.Tk) { _stick = Vector2.zero; _rt = 0f; _holdRT = true; _tks++; yield return WaitR(0.5f); _holdRT = false; continue; }
                    if (_phase == 0 && _presses.Count == 0 && !(d.Prompt == "Ride" && !WantsBike(d)) && !(d.Prompt == "Get off" && !WantsOff(d))) { _stick = Vector2.zero; _rt = 0f; Press(X); _xs++; Log($"X at '{d.Prompt}'"); yield return WaitR(0.6f); continue; }
                }
                _holdRT = false;
                var m = d.NextMarker;
                var seven = d.Seven.transform.position;
                if (d.Riding)
                {
                    var bike = d.Bike.transform;
                    if (!m.HasValue) { _stick = Vector2.zero; _rt = 0f; _lt = 0.4f; continue; }
                    var f = Flat(m.Value - bike.position);
                    float err = Vector3.SignedAngle(Flat(bike.forward), f, Vector3.up);
                    // close to the light: brake and stop (a player does not circle the spot)
                    if (f.magnitude < 6f) { _stick = Vector2.zero; _rt = 0f; _lt = 1f; continue; }
                    _stick = new Vector2(Mathf.Clamp(err / 30f, -1f, 1f), 0f);
                    _rt = Mathf.Abs(err) > 75f ? 0.2f : Mathf.Clamp(f.magnitude / 14f, 0.35f, 1f);
                    _lt = 0f;
                }
                else
                {
                    _rt = 0f; _lt = 0f;
                    if (!m.HasValue) { _stick = Vector2.zero; continue; }
                    var dir = Flat(m.Value - seven);
                    // the moving section: wait beside it until it opens
                    if (Ground.FindZone("SlideGate") is Ground.Zone sg && !sg.Enabled && Flat(seven - new Vector3(6.7f, 0f, MarsLayout.SlideGateZ)).magnitude < 2.6f && m.Value.x > seven.x + 1f) { _stick = Vector2.zero; continue; }
                    if (dir.magnitude < 0.5f) { _stick = Vector2.zero; continue; }
                    var cam = Camera.main != null ? Camera.main.transform : null;
                    if (cam != null) { var cf = Flat(cam.forward).normalized; var cr = new Vector3(cf.z, 0f, -cf.x); dir.Normalize(); _stick = new Vector2(Vector3.Dot(dir, cr), Vector3.Dot(dir, cf)); }
                }
                string now = d.ObjectiveNow + "|" + d.WaitingFor;
                if (now != sig) { sig = now; stuckSince = Time.time; }
                else if (Time.time - stuckSince > 240f) { _soft++; Log($"SOFT-LOCK: no story progress for 240 s ('{now}') at {seven}"); stuckSince = Time.time; }
            }
            Cap.Finish($"MARS PLAYTEST: 1/1, soft-locks {_soft}, skips taken {_skipsTaken}, X presses {_xs}, telekinesis {_tks} ({(Time.time - _t0) / 60f:0.0} game-min) - {MarsState.Summary()}; {WorldFlags.Summary()}; choice {(S != null ? S.MarsChoice : "?")}");
        }
        // a player gets on the bike when the light is far, and off when the light is close by (an X / RT spot ahead)
        static bool BikeStep(MarsDirector d) => (d.ObjectiveNow != null && d.ObjectiveNow.Contains("bike")) || (d.WaitingFor != null && d.WaitingFor.Contains("on the bike"));
        static bool WantsBike(MarsDirector d) => d.NextMarker.HasValue && (BikeStep(d) || Flat(d.NextMarker.Value - d.Seven.transform.position).magnitude > 14f);
        static bool WantsOff(MarsDirector d) => d.NextMarker.HasValue && !BikeStep(d) && Flat(d.NextMarker.Value - d.Bike.transform.position).magnitude < 12f;
        IEnumerator WaitR(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }
        void OnDestroy() { if (_pad != null && _pad.added) InputSystem.RemoveDevice(_pad); }
    }
}
