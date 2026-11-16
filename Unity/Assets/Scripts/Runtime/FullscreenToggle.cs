using UnityEngine;
using UnityEngine.InputSystem;

namespace Seven
{
    /// <summary>
    /// Fullscreen handling (QF1).
    /// Normal launch: force borderless fullscreen at the monitor's native resolution, even if an earlier (windowed)
    /// test run left Unity's saved setting at windowed. Skipped when the command line sets the window itself
    /// (any -screen-* / -window-mode / -popupwindow flag) and always skipped for -capture check runs.
    /// F11 or the Xbox View button toggles fullscreen ↔ windowed (Alt+Enter is Unity's own and keeps working).
    /// Check runs (-capture) never change the window mode.
    /// </summary>
    public class FullscreenToggle : MonoBehaviour
    {
        const int WindowedW = 1600, WindowedH = 900;

        public static void Install(bool forceFullscreen)
        {
            var go = new GameObject("FullscreenToggle");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;
            go.AddComponent<FullscreenToggle>();
            if (forceFullscreen) GoFullscreen();
        }

        static void GoFullscreen()
        {
            var di = Screen.mainWindowDisplayInfo;
            int w = di.width > 0 ? di.width : Display.main.systemWidth;
            int h = di.height > 0 ? di.height : Display.main.systemHeight;
            Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
            Debug.Log($"[Seven] fullscreen: borderless {w}x{h}");
        }

        static void GoWindowed()
        {
            var di = Screen.mainWindowDisplayInfo;
            int w = WindowedW, h = WindowedH;
            if (di.width > 0 && (w > di.width || h > di.height)) { w = Mathf.RoundToInt(di.width * 0.8f); h = Mathf.RoundToInt(di.height * 0.8f); }
            Screen.SetResolution(w, h, FullScreenMode.Windowed);
            Debug.Log($"[Seven] fullscreen: windowed {w}x{h}");
        }

        public static void Toggle()
        {
            if (Screen.fullScreenMode == FullScreenMode.Windowed) GoFullscreen();
            else GoWindowed();
        }

        void Update()
        {
            if (DemoBoot.CaptureDir != null) return;   // check runs never touch the window
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool f11 = kb != null && kb.f11Key.wasPressedThisFrame;
            bool view = gp != null && gp.selectButton.wasPressedThisFrame;
            if (f11 || view) Toggle();
        }
    }
}
