using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Seven
{
    /// <summary>
    /// Round 7 (3d): Start / Esc opens a small pause menu in every playable scene (never on the main menu):
    /// Resume · Settings (music / sound effects / voice volume, Round 9: Button hints On / Off) · Controls (Round 9: the
    /// full Chapter 1 map, Xbox + keyboard) · Main menu · Quit game, with a confirm step on
    /// Main menu and Quit. Time stops and audio pauses while it is open. Esc no longer leaves a scene directly.
    /// Pad: stick / d-pad, A = choose, B = back, Start = close. Keys: W/S or arrows, ←/→ to adjust, Enter / Space,
    /// Esc = back. Strictly monochrome uGUI drawn on the active camera (so check runs can capture it).
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu I { get; private set; }
        public static bool IsOpen => I != null && I._open;
        /// <summary>Another screen owns Esc right now (the gallery full view): Esc does not open the menu.</summary>
        public static System.Func<bool> EscapeOwned;

        enum Page { Main, Settings, ConfirmMenu, ConfirmQuit, Controls }
        const int MainSettings = 1, MainControls = 2, MainMenu = 3, MainQuit = 4;
        readonly List<GameObject> _extra = new List<GameObject>();
        Page _page;
        int _sel;
        bool _open;
        float _stickReady;
        GameObject _canvasGo;
        Canvas _canvas;
        Text _title;
        readonly List<Text> _labels = new List<Text>();
        readonly List<Image> _bars = new List<Image>();
        Font _font;
        float _prevTimeScale = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (I != null) return;
            var go = new GameObject("PauseMenu");
            DontDestroyOnLoad(go);
            I = go.AddComponent<PauseMenu>();
            SceneManager.sceneLoaded += (s, m) => { if (I != null && I._open) I.Close(); };
        }

        static bool Allowed => SceneManager.GetActiveScene().name != DemoMenu.SceneName && !TelepathySoundCheck.Running;

        void Update()
        {
            var kb = Keyboard.current; var gp = Gamepad.current;
            bool esc = kb != null && kb.escapeKey.wasPressedThisFrame;
            bool start = gp != null && gp.startButton.wasPressedThisFrame;
            if (!_open)
            {
                if (!Allowed || (DemoBoot.CaptureDir != null && !ScriptInput)) return;
                if (start || (esc && !(EscapeOwned?.Invoke() ?? false))) Open();
                return;
            }
            if (start) { Close(); return; }
            int d = 0, lr = 0;
            if (kb != null)
            {
                if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) d = -1;
                if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) d = 1;
                if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) lr = -1;
                if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) lr = 1;
            }
            if (gp != null)
            {
                if (gp.dpad.up.wasPressedThisFrame) d = -1;
                if (gp.dpad.down.wasPressedThisFrame) d = 1;
                if (gp.dpad.left.wasPressedThisFrame) lr = -1;
                if (gp.dpad.right.wasPressedThisFrame) lr = 1;
                var st = gp.leftStick.ReadValue();
                if (st.magnitude < 0.3f) _stickReady = 0f;
                else if (Time.unscaledTime > _stickReady)
                {
                    if (Mathf.Abs(st.y) > Mathf.Abs(st.x)) d = st.y > 0 ? -1 : 1; else lr = st.x > 0 ? 1 : -1;
                    _stickReady = Time.unscaledTime + 0.3f;
                }
            }
            bool ok = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            bool back = esc || (gp != null && gp.buttonEast.wasPressedThisFrame);
            Handle(d, lr, ok, back);
        }

        /// <summary>Check runs drive the menu through this (same path as the keys).</summary>
        public static bool ScriptInput;
        public void Handle(int d, int lr, bool ok, bool back)
        {
            var items = Items();
            if (d != 0) { _sel = (_sel + d + items.Count) % items.Count; AudioKit.Play("menu_move", null, 0.6f); Refresh(); }
            if (_page == Page.Settings && lr != 0 && _sel == 3) { ControlsLegend.HintsOn = !ControlsLegend.HintsOn; AudioKit.Play("menu_move", null, 0.6f); Refresh(); }
            if (_page == Page.Settings && lr != 0 && _sel < 3)
            {
                float step = 0.1f * lr;
                if (_sel == 0) AudioKit.MusicVolume = Mathf.Round((AudioKit.MusicVolume + step) * 10f) / 10f;
                if (_sel == 1) { AudioKit.SfxVolume = Mathf.Round((AudioKit.SfxVolume + step) * 10f) / 10f; AudioKit.Play("menu_move", null, 1f); }
                if (_sel == 2) AudioKit.VoiceVolume = Mathf.Round((AudioKit.VoiceVolume + step) * 10f) / 10f;
                Refresh();
            }
            if (back)
            {
                if (_page == Page.Main) Close(); else Show(Page.Main, _page == Page.Settings ? MainSettings : _page == Page.Controls ? MainControls : _page == Page.ConfirmMenu ? MainMenu : MainQuit);
                return;
            }
            if (!ok) return;
            AudioKit.Play("menu_confirm", null, 0.7f);
            switch (_page)
            {
                case Page.Main:
                    if (_sel == 0) Close();
                    else if (_sel == MainSettings) Show(Page.Settings, 0);
                    else if (_sel == MainControls) Show(Page.Controls, 0);
                    else if (_sel == MainMenu) Show(Page.ConfirmMenu, 1);
                    else Show(Page.ConfirmQuit, 1);
                    break;
                case Page.Settings:
                    if (_sel == 3) { ControlsLegend.HintsOn = !ControlsLegend.HintsOn; Refresh(); }
                    if (_sel == 4) Show(Page.Main, MainSettings);
                    break;
                case Page.Controls:
                    Show(Page.Main, MainControls);
                    break;
                case Page.ConfirmMenu:
                    if (_sel == 0) { Close(); Chapter1State.Reset(); SceneManager.LoadScene(DemoMenu.SceneName); }
                    else Show(Page.Main, MainMenu);
                    break;
                case Page.ConfirmQuit:
                    if (_sel == 0) { Close(); QuitGame(); }
                    else Show(Page.Main, MainQuit);
                    break;
            }
        }

        public static void QuitGame()
        {
            Debug.Log("[Seven] quit requested");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        List<string> Items()
        {
            switch (_page)
            {
                case Page.Settings:
                    return new List<string> { "Music   " + Bar(AudioKit.MusicVolume), "Sound effects   " + Bar(AudioKit.SfxVolume), "Voice   " + Bar(AudioKit.VoiceVolume),
                                              "Button hints   " + (ControlsLegend.HintsOn ? "On" : "Off"), "Back" };
                case Page.Controls: return new List<string> { "Back" };
                case Page.ConfirmMenu: return new List<string> { "Yes, main menu", "No" };
                case Page.ConfirmQuit: return new List<string> { "Yes, quit", "No" };
                default: return new List<string> { "Resume", "Settings", "Controls", "Main menu", "Quit game" };
            }
        }
        static string Bar(float v) { int n = Mathf.RoundToInt(v * 10f); return new string('■', n) + new string('□', 10 - n); }
        string Title() => _page == Page.Settings ? "SETTINGS" : _page == Page.Controls ? "CONTROLS" : _page == Page.ConfirmMenu ? "Return to the main menu?" : _page == Page.ConfirmQuit ? "Quit the game?" : "PAUSED";

        public void Open()
        {
            if (_open) return;
            _open = true;
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Build();
            Show(Page.Main, 0);
            AudioKit.Log?.Invoke("pause menu opened");
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            Time.timeScale = _prevTimeScale <= 0f ? 1f : _prevTimeScale;
            AudioListener.pause = DemoBoot.Muted;
            if (_canvasGo != null) Destroy(_canvasGo);
            _labels.Clear(); _bars.Clear(); _extra.Clear();
        }

        void Show(Page p, int sel) { _page = p; _sel = sel; Refresh(); }

        void Build()
        {
            if (_canvasGo != null) Destroy(_canvasGo);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _canvasGo = new GameObject("PauseCanvas");
            _canvasGo.transform.SetParent(transform, false);
            _canvas = _canvasGo.AddComponent<Canvas>();
            var cam = Camera.main;
            if (cam != null) { _canvas.renderMode = RenderMode.ScreenSpaceCamera; _canvas.worldCamera = cam; _canvas.planeDistance = cam.nearClipPlane + 0.05f; }
            else _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 500;
            var sc = _canvasGo.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1280, 720); sc.matchWidthOrHeight = 1f;
            var dim = new GameObject("Dim", typeof(RectTransform)); dim.transform.SetParent(_canvasGo.transform, false);
            var drt = (RectTransform)dim.transform; drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one; drt.offsetMin = drt.offsetMax = Vector2.zero;
            dim.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);
            Logo.Add(_canvasGo.transform, new Vector2(0, 262), 210f, new Color(0.9f, 0.9f, 0.9f));     // Round 10: logo header
            _title = MakeText(_canvasGo.transform, "", new Vector2(0, 175), 34, new Color(0.95f, 0.95f, 0.95f));
            MakeText(_canvasGo.transform, "stick / d-pad + A · B back    ·    W S / arrows, ← → adjust, Enter · Esc back", new Vector2(0, -290), 15, new Color(0.55f, 0.55f, 0.55f));
        }

        Text MakeText(Transform parent, string s, Vector2 pos, int size, Color c)
        {
            var go = new GameObject("Text", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(900, size * 1.6f);
            var t = go.AddComponent<Text>(); t.font = _font; t.fontSize = size; t.text = s; t.color = c; t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        /// <summary>Round 9: the Chapter 1 control map (the chapter teaches these one by one; this is the full list).</summary>
        public static readonly (string what, string pad, string keys)[] ControlMap =
        {
            ("Move", "Left stick", "W A S D / arrows"),
            ("Camera", "Right stick", "Mouse"),
            ("Re-center camera", "R3", "Middle mouse / R"),
            ("Run (hold)", "LB", "Shift"),
            ("Jump · grab a ledge", "A", "Space"),
            ("Interact (doors, touch, sit)", "X", "E"),
            ("Crouch-sneak (toggle) · climb down at an edge", "B", "C"),
            ("Hanging: climb up · shimmy · let go", "Up · left/right · B", "W · A/D · Space"),
            ("Telekinesis (highlighted object)", "RT", "Right mouse"),
            ("Skip a line", "Y", "Enter"),
            ("Restart from the checkpoint", "D-pad down", "Backspace"),
            ("Pause menu", "Start", "Esc"),
        };

        void BuildControlsTable()
        {
            foreach (var g in _extra) if (g != null) Destroy(g);
            _extra.Clear();
            if (_page != Page.Controls) return;
            var head = new Color(0.62f, 0.62f, 0.62f); var ink = new Color(0.92f, 0.92f, 0.92f);
            Cell("", -150, 132, 16, head, 420, TextAnchor.MiddleLeft); Cell("XBOX", 170, 132, 16, head, 230, TextAnchor.MiddleLeft); Cell("KEYBOARD / MOUSE", 400, 132, 16, head, 260, TextAnchor.MiddleLeft);
            for (int i = 0; i < ControlMap.Length; i++)
            {
                float y = 104 - i * 25;
                Cell(ControlMap[i].what, -150, y, 17, ink, 420, TextAnchor.MiddleLeft);
                Cell(ControlMap[i].pad, 170, y, 17, ink, 230, TextAnchor.MiddleLeft);
                Cell(ControlMap[i].keys, 400, y, 17, ink, 260, TextAnchor.MiddleLeft);
            }
            void Cell(string s, float x, float y, int size, Color c, float w, TextAnchor a)
            {
                var t = MakeText(_canvasGo.transform, s, new Vector2(x - 120f, y), size, c);
                t.alignment = a; ((RectTransform)t.transform).sizeDelta = new Vector2(w, size * 1.6f);
                ((RectTransform)t.transform).pivot = new Vector2(0f, 0.5f);
                _extra.Add(t.gameObject);
            }
        }

        void Refresh()
        {
            if (_canvasGo == null) return;
            BuildControlsTable();
            var items = Items();
            while (_labels.Count < items.Count)
            {
                var bar = new GameObject("Item", typeof(RectTransform)); bar.transform.SetParent(_canvasGo.transform, false);
                var img = bar.AddComponent<Image>();
                _bars.Add(img);
                _labels.Add(MakeText(bar.transform, "", Vector2.zero, 26, Color.white));
            }
            for (int i = 0; i < _labels.Count; i++)
            {
                bool vis = i < items.Count;
                _bars[i].gameObject.SetActive(vis);
                if (!vis) continue;
                var rt = (RectTransform)_bars[i].transform; rt.anchoredPosition = _page == Page.Controls ? new Vector2(0, -230) : new Vector2(0, 80 - i * 54); rt.sizeDelta = new Vector2(440, 46);
                bool on = i == _sel;
                _bars[i].color = on ? new Color(0.92f, 0.92f, 0.92f, 1f) : new Color(1f, 1f, 1f, 0.07f);
                _labels[i].text = items[i];
                _labels[i].color = on ? new Color(0.05f, 0.05f, 0.05f) : new Color(0.85f, 0.85f, 0.85f);
            }
            _title.text = Title();
        }
    }

    /// <summary>Round 7 (3d): "Continue" save — set when 7 and the cat reach the saucer in Chapter 1.</summary>
    public static class SaveGame
    {
        // Check runs use their own key and remove it again, so a test never leaves a save on Dan's PC.
        static string Key => DemoBoot.CaptureDir != null ? "seven_test_ch1_aboard" : "seven_ch1_aboard";
        public static bool HasChapter1Save => PlayerPrefs.GetInt(Key, 0) == 1;
        public static void SaveChapter1Aboard() { PlayerPrefs.SetInt(Key, 1); PlayerPrefs.Save(); Debug.Log("[Seven] saved: chapter 1 aboard (" + Key + ")"); }
        public static void ClearTestSave() { if (DemoBoot.CaptureDir != null) { PlayerPrefs.DeleteKey("seven_test_ch1_aboard"); PlayerPrefs.Save(); } }

        /// <summary>Continue: straight into exploring the saucer after the chapter.</summary>
        public static void Continue()
        {
            Chapter1State.Reset();
            Chapter1State.Explore = true;
            Chapter1State.StartAboard();
        }
    }
}
