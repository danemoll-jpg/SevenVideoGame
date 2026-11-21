using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Seven
{
    /// <summary>
    /// QF20 (1) proof (-qf20check -capture dir): Dan's real 0.15.4 save, rebuilt on the test keys - Mars complete, Europa
    /// complete, the old route flag "combined" (Biaveh unlocked) and "asteroid found" already set, but the discovery never
    /// seen to its end. Free exploration aboard; a virtual gamepad on the real input actions walks 7 to the console until
    /// the "Use console" prompt shows and presses X ONCE. Must follow: MAR_13C + MAR_13B in the pilot's chair, the journey
    /// toward Biaveh, TRV_AST, the star map choice (d-pad to the asteroid, A, A), free exploration again with the discovery
    /// marked seen. Then X at the console a second time must open the console normally (no replay).
    /// Ends "QF20 DISCOVERY CHECK: PASS / FAIL ...".
    /// </summary>
    public class QF20Check : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (DemoBoot.CaptureDir == null || !DemoBoot.QF20Check || FindAnyObjectByType<QF20Check>() != null) return;
            var go = new GameObject("QF20Check"); DontDestroyOnLoad(go); go.AddComponent<QF20Check>();
        }

        Gamepad _pad; Vector2 _stick; GamepadButton _cur; int _phase;
        ChapterCapture Cap => ChapterCapture.I;
        void Log(string s) => Cap?.Log("QF20 check: " + s);
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        static Chapter1Aboard Ab => Chapter1Aboard.I != null && Chapter1Aboard.I.isActiveAndEnabled ? Chapter1Aboard.I : null;

        void Update()
        {
            if (_pad == null) return;
            if (Gamepad.current != _pad) _pad.MakeCurrent();
            uint bits = 0u;
            if (_phase >= 1 && _phase <= 3) bits |= 1u << (int)_cur;
            if (_phase > 0) _phase = _phase >= 6 ? 0 : _phase + 1;
            InputSystem.QueueStateEvent(_pad, new GamepadState { leftStick = _stick, buttons = bits });
        }
        void Press(GamepadButton b) { _cur = b; _phase = 1; }
        IEnumerator WaitR(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }

        IEnumerator Start()
        {
            ChapterCapture.Ensure();
            while (Ab == null || !Ab.Explored || Ab.InGalleyScene || ShipConsole.I == null) yield return null;
            yield return WaitR(7f);                                         // the "Set course" objective fades
            _pad = InputSystem.AddDevice<Gamepad>("QF20Pad"); _pad.MakeCurrent();
            if (Camera.main != null) Cap.Attach(Camera.main);      // player-sim aboard runs do not attach the capture themselves
            Log($"free exploration aboard; muted={DemoBoot.Muted} listenerVolume={AudioListener.volume}; {WorldFlags.Summary()}; due {WorldFlags.CombinedDue}");
            bool dueAtStart = WorldFlags.CombinedDue;
            yield return Cap.Shot("qf20_00_free_exploration");

            // 1) walk to the console like a player until the prompt shows, press X once
            int presses = 0;
            yield return WalkToConsole();
            Log($"at the console: in reach (NearConsole {ShipConsole.I.NearConsole}, 7 {Flat(ShipConsole.I.Director.Seven.transform.position - ShipConsole.I.StandSpot.position).magnitude:0.00} m from its stand spot) - pressing X once");
            Press(GamepadButton.West); presses++;
            yield return WaitR(2.5f);
            bool chair = ShipConsole.StoryLock && Ab.InGalleyScene;
            Log($"after the one press: console state {ShipConsole.I.Current}, story scene {(chair ? "RUNNING (the pilot's chair)" : "not running")}");
            yield return Cap.Shot("qf20_01_after_the_press_the_pilots_chair");

            // 2) let the scene play; at the star map choice: d-pad to the asteroid, A, A
            float t0 = Time.time; bool shotMap = false, sawSeen = false;
            while (Time.time - t0 < 400f)
            {
                yield return null;
                if (WorldFlags.CombinedSeen) sawSeen = true;
                var con = ShipConsole.I;
                if (con != null && con.Choosing && con.Map != null)
                {
                    if (!shotMap) { shotMap = true; yield return Cap.Shot("qf20_02_trv_ast_the_choice"); }
                    if (con.Map.Selected != "asteroid") { if (_phase == 0) { Press(GamepadButton.DpadRight); yield return WaitR(0.4f); } }
                    else { Log("star map: the asteroid highlighted - A, A"); Press(GamepadButton.South); yield return WaitR(0.6f); Press(GamepadButton.South); yield return WaitR(1.5f); }
                    continue;
                }
                if (sawSeen && !ShipConsole.StoryLock && Ab != null && Ab.Explored && !Ab.InGalleyScene && (con == null || !con.Busy)) break;
            }
            yield return WaitR(2f);
            yield return Cap.Shot("qf20_03_free_exploration_again");
            Log($"after the discovery: {WorldFlags.Summary()}; Biaveh unlocked {WorldFlags.BiavehUnlocked}; due {WorldFlags.CombinedDue}; ship system {SaucerLocation.System}");

            // 3) the console again: it must open normally now
            yield return WalkToConsole();
            Press(GamepadButton.West); presses++;
            yield return WaitR(2.5f);
            var st = ShipConsole.I.Current;
            Log($"second X at the console: console state {st} (Using = it opened normally), story scene {(ShipConsole.StoryLock ? "RUNNING - REPLAYED" : "none")}");
            yield return Cap.Shot("qf20_04_console_opens_normally");
            Press(GamepadButton.East); yield return WaitR(1.5f);

            bool pass = dueAtStart && chair && WorldFlags.CombinedSeen && WorldFlags.AsteroidFound && !WorldFlags.CombinedDue && st == ShipConsole.State.Using;
            Cap.Finish($"QF20 DISCOVERY CHECK: {(pass ? "PASS" : "FAIL")} - due at start {dueAtStart}, one X press at the console -> the chair scene {chair}, seen {WorldFlags.CombinedSeen}, asteroid found {WorldFlags.AsteroidFound}, due after {WorldFlags.CombinedDue}, second press opened the console {st == ShipConsole.State.Using}; pad presses at the console {presses}");
        }

        IEnumerator WalkToConsole()
        {
            var con = ShipConsole.I;
            float t0 = Time.time;
            while (Time.time - t0 < 40f)
            {
                // the console's own "near" (the X / E "Use console" prompt a player sees; in player-sim runs the galley seat's
                // legacy watcher rewrites the prompt text every frame, so the text is not read here)
                if (con.NearConsole && !con.Busy) { _stick = Vector2.zero; yield return WaitR(0.3f); yield break; }
                var s = con.Director.Seven.transform.position;
                var to = Flat(con.StandSpot.position - s);
                var cam = Camera.main != null ? Camera.main.transform : null;
                if (cam != null && to.magnitude > 0.2f) { var cf = Flat(cam.forward).normalized; var cr = new Vector3(cf.z, 0f, -cf.x); to.Normalize(); _stick = new Vector2(Vector3.Dot(to, cr), Vector3.Dot(to, cf)) * 0.8f; }
                else _stick = Vector2.zero;
                yield return null;
            }
            _stick = Vector2.zero;
            Log("WALK TIMEOUT: the console prompt never showed");
        }

        void OnDestroy() { if (_pad != null && _pad.added) InputSystem.RemoveDevice(_pad); }
    }
}
