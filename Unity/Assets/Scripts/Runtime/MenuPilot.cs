using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Seven
{
    /// <summary>
    /// QF12: a check run that begins in the real main menu, like a player (-menupick "Label|Label|..."). A virtual
    /// gamepad presses d-pad down until the highlighted row's label starts with the next part, then A; when the menu asks
    /// to confirm (the row's label changes to a question), A again. Logs each step and the menu shot. Nothing is called
    /// directly - the menu's own input handling starts the scene.
    /// </summary>
    public class MenuPilot : MonoBehaviour
    {
        Gamepad _pad;

        IEnumerator Start()
        {
            ChapterCapture.Ensure();
            var cap = ChapterCapture.I;
            _pad = InputSystem.AddDevice<Gamepad>("MenuPilotPad"); _pad.MakeCurrent();
            yield return new WaitForSecondsRealtime(1.0f);
            var menu = DemoMenu.I;
            var parts = DemoBoot.MenuPick.Split('|');
            cap?.Log($"menu pilot (QF12): from the main menu -> {string.Join(" > ", parts)}");
            foreach (var raw in parts)
            {
                var part = raw.Trim();
                if (part.Length == 0) continue;
                int guard = 0;
                while (menu != null && guard++ < 40)
                {
                    var labels = menu.Labels; int s = menu.Selected;
                    if (s >= 0 && s < labels.Length && labels[s].StartsWith(part)) break;
                    yield return Tap(GamepadButton.DpadDown);
                }
                var lab = menu.Labels; cap?.Log($"menu pilot: '{(menu.Selected < lab.Length ? lab[menu.Selected] : "?")}' highlighted - A");
                string before = menu.Selected < lab.Length ? lab[menu.Selected] : null;
                yield return Tap(GamepadButton.South);
                yield return new WaitForSecondsRealtime(0.4f);
                if (menu == null) yield break;                     // the scene is loading
                // a confirm (the same row now asks a question): answer yes
                var after = menu.Labels;
                if (menu.Selected < after.Length && before != null && after[menu.Selected] != before && after[menu.Selected].Contains("?"))
                {
                    cap?.Log($"menu pilot: confirm '{after[menu.Selected]}' - A");
                    yield return Tap(GamepadButton.South);
                    yield return new WaitForSecondsRealtime(0.4f);
                }
            }
        }

        IEnumerator Tap(GamepadButton b)
        {
            InputSystem.QueueStateEvent(_pad, new GamepadState().WithButton(b));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(_pad, new GamepadState());
            yield return null; yield return null;
            yield return new WaitForSecondsRealtime(0.12f);
        }

        void OnDestroy() { if (_pad != null && _pad.added) InputSystem.RemoveDevice(_pad); }
    }
}
