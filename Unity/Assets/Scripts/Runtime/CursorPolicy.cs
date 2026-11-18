using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// QF3: the OS mouse cursor is hidden during gameplay. It only appears on a menu (main menu or pause menu) and only
    /// after the mouse was actually moved there; keyboard / pad input on the menu hides it again. Not locked, so the
    /// right-drag camera orbit and alt-tab keep working.
    /// </summary>
    public class CursorPolicy : MonoBehaviour
    {
        static CursorPolicy _i;
        /// <summary>Round 9: Chapter 1's camera orbits with the mouse - lock the cursor while playing (never on a menu,
        /// never in a check run, released when the window loses focus).</summary>
        public static bool WantLock;
        bool _mouseActive;

        /// <summary>True when a menu is on screen (main menu scene or the pause menu).</summary>
        public static bool MenuOpen => SceneManager.GetActiveScene().name == DemoMenu.SceneName || PauseMenu.IsOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (_i != null) return;
            var go = new GameObject("CursorPolicy");
            DontDestroyOnLoad(go);
            _i = go.AddComponent<CursorPolicy>();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
        }

        void LateUpdate()
        {
            var ms = Mouse.current; var kb = Keyboard.current; var gp = Gamepad.current;
            if (ms != null && (ms.delta.ReadValue().sqrMagnitude > 4f || ms.leftButton.wasPressedThisFrame)) _mouseActive = true;
            if ((kb != null && kb.anyKey.wasPressedThisFrame) || (gp != null && gp.wasUpdatedThisFrame && AnyPad(gp))) _mouseActive = false;
            bool show = MenuOpen && _mouseActive && DemoBoot.CaptureDir == null;
            if (Cursor.visible != show) Cursor.visible = show;
            var lockMode = WantLock && !MenuOpen && DemoBoot.CaptureDir == null && Application.isFocused ? CursorLockMode.Locked : CursorLockMode.None;
            if (Cursor.lockState != lockMode) Cursor.lockState = lockMode;
        }

        static bool AnyPad(Gamepad gp) =>
            gp.leftStick.ReadValue().sqrMagnitude > 0.1f || gp.buttonSouth.isPressed || gp.buttonEast.isPressed || gp.dpad.ReadValue() != Vector2.zero;
    }
}
