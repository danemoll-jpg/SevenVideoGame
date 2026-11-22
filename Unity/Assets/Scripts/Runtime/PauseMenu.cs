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
    /// QF14: Scene select = the unlocked chapters (<see cref="ChapterList"/>, the same list as the main menu) → a chapter's
    /// page (Play from the start (asks first when it resets progress), its sections, Back); long pages scroll
    /// (<see cref="MenuScroll"/>, the mouse wheel too).
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu I { get; private set; }
        public static bool IsOpen => I != null && I._open;
        /// <summary>Another screen owns Esc right now (the gallery full view): Esc does not open the menu.</summary>
        public static System.Func<bool> EscapeOwned;

        enum Page { Main, Settings, ConfirmMenu, ConfirmQuit, Controls, Select, Chapter, ConfirmStart }
        const int MainSettings = 1, MainControls = 2, MainSelect = 3, MainMenu = 4, MainQuit = 5;
        /// <summary>QF6 playtest: the highlighted entry (the driver reads the menu like a person).</summary>
        public string SelectedLabel { get { var it = Items(); return _sel >= 0 && _sel < it.Count ? it[_sel] : null; } }
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
            if (d == 0) d = MenuScroll.Wheel();          // QF14: the mouse wheel moves the selection (long lists scroll with it)
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
            // Round 13: the settings rows are the shared GameSettings (the ship's console shows the same ones)
            if (_page == Page.Settings && lr != 0 && _sel < GameSettings.Count) { GameSettings.Adjust(_sel, lr); Refresh(); }
            if (back)
            {
                // QF14: chapter page -> the chapter list -> the pause menu
                if (_page == Page.ConfirmStart) { Show(Page.Chapter, 0); return; }
                if (_page == Page.Chapter) { Show(Page.Select, Mathf.Max(0, _unlocked.IndexOf(Current))); return; }
                if (_page == Page.Main) Close(); else Show(Page.Main, _page == Page.Settings ? MainSettings : _page == Page.Controls ? MainControls : _page == Page.Select ? MainSelect : _page == Page.ConfirmMenu ? MainMenu : MainQuit);
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
                    else if (_sel == MainSelect) Show(Page.Select, 0);
                    else if (_sel == MainMenu) Show(Page.ConfirmMenu, 1);
                    else Show(Page.ConfirmQuit, 1);
                    break;
                case Page.Settings:
                    if (_sel < GameSettings.Count) { GameSettings.Activate(_sel); Refresh(); }
                    if (_sel == GameSettings.Count) Show(Page.Main, MainSettings);
                    break;
                case Page.Controls:
                    Show(Page.Main, MainControls);
                    break;
                case Page.Select:
                    // QF14: the chapter list (ChapterList, unlocked only) -> the chapter's page
                    if (_sel >= _unlocked.Count) Show(Page.Main, MainSelect);
                    else { _chapter = ChapterList.IndexOf(_unlocked[_sel]); Show(Page.Chapter, 0); }
                    break;
                case Page.Chapter:
                {
                    var ch = Current; if (ch == null) { Show(Page.Select, 0); break; }
                    int n = ch.Sections().Length;
                    if (_sel == 0)
                    {
                        if (ch.HasProgress != null && ch.HasProgress()) { Show(Page.ConfirmStart, 1); break; }
                        Close(); ChapterList.StartFromBeginning(ch, " (pause menu)");
                    }
                    else if (_sel <= n) { Close(); ChapterList.StartSection(ch, _sel - 1, " (pause menu)"); }
                    else Show(Page.Select, Mathf.Max(0, _unlocked.IndexOf(ch)));
                    break;
                }
                case Page.ConfirmStart:
                {
                    var ch = Current;
                    if (_sel == 0 && ch != null) { Close(); ChapterList.StartFromBeginning(ch, " (pause menu)"); }
                    else Show(Page.Chapter, 0);
                    break;
                }
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
                {
                    var l = new List<string>();
                    for (int i = 0; i < GameSettings.Count; i++) l.Add(GameSettings.Label(i));
                    l.Add("Back");
                    return l;
                }
                case Page.Controls: return new List<string> { "Back" };
                case Page.Select: { var l = new List<string>(); foreach (var c in _unlocked) l.Add(c.Name); l.Add("Back"); return l; }
                case Page.Chapter:
                {
                    var l = new List<string> { ChapterList.PlayFromStartLabel };
                    if (Current != null) foreach (var (label, _) in Current.Sections()) l.Add(label);
                    l.Add("Back");
                    return l;
                }
                case Page.ConfirmStart: return new List<string> { "Yes, start over", "No" };
                case Page.ConfirmMenu: return new List<string> { "Yes, main menu", "No" };
                case Page.ConfirmQuit: return new List<string> { "Yes, quit", "No" };
                default: return new List<string> { "Resume", "Settings", "Controls", "Scene select", "Main menu", "Quit game" };
            }
        }
        string Title() => _page == Page.Settings ? "SETTINGS" : _page == Page.Controls ? "CONTROLS" : _page == Page.Select ? "SCENE SELECT"
                       : _page == Page.Chapter ? (Current != null ? Current.Name.ToUpperInvariant() : "SCENE SELECT") : _page == Page.ConfirmStart ? (Current != null ? Current.Confirm : "Start over?") : _page == Page.ConfirmMenu ? "Return to the main menu?" : _page == Page.ConfirmQuit ? "Quit the game?" : "PAUSED";

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
            Time.timeScale = _prevTimeScale <= 0f ? DemoBoot.TestSpeed : _prevTimeScale;
            AudioListener.pause = DemoBoot.Muted;
            if (_canvasGo != null) Destroy(_canvasGo);
            _labels.Clear(); _bars.Clear(); _extra.Clear();
            _scroll = null; _capCam = null;
        }

        void Show(Page p, int sel)
        {
            if (p == Page.Select) _unlocked = ChapterList.Unlocked();
            _page = p; _sel = sel; _scroll?.Home(); Refresh();
        }
        // QF14: Scene select = the chapter list (unlocked only) -> a chapter's page (Play from the start, its sections, Back)
        List<ChapterDef> _unlocked = new List<ChapterDef>();
        int _chapter = -1;
        ChapterDef Current => _chapter >= 0 && _chapter < ChapterList.All.Length ? ChapterList.All[_chapter] : null;
        MenuScroll _scroll;
        const int ListRows = 10;            // rows shown on the Scene select pages (more scroll); see Refresh for the arithmetic
        const float ListTop = 135f, ListPitch = 37f, ListBar = 32f;
        Camera _capCam;

        // ---- QF14 (-menupages): the capture run shows these pages on the main menu's camera (the menu scene never opens it)
        public void OpenForCapture(Camera cam) { _capCam = cam; Open(); }
        /// <summary>Capture: the chapter list (chapter &lt; 0) or a chapter's page, with row sel highlighted.</summary>
        public void CaptureShow(int chapter, int sel)
        {
            if (!_open) return;
            if (chapter < 0) { Show(Page.Select, 0); _sel = Mathf.Clamp(sel, 0, Items().Count - 1); Refresh(); return; }
            _unlocked = ChapterList.Unlocked(); _chapter = chapter; Show(Page.Chapter, 0); _sel = Mathf.Clamp(sel, 0, Items().Count - 1); Refresh();
        }
        public string PageTitle => Title();
        public int ItemCount => Items().Count;
        public int VisibleRows => _scroll == null ? ItemCount : Mathf.Min(ItemCount, _scroll.Visible);
        public bool ScrollBarShown => _scroll != null && _scroll.Shown;
        public int ScrollTop => _scroll != null ? _scroll.Top : 0;

        void Build()
        {
            if (_canvasGo != null) Destroy(_canvasGo);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _canvasGo = new GameObject("PauseCanvas");
            _canvasGo.transform.SetParent(transform, false);
            _canvas = _canvasGo.AddComponent<Canvas>();
            var cam = _capCam != null ? _capCam : Camera.main;
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
            MakeText(_canvasGo.transform, "stick / d-pad + A · B back    ·    W S / arrows / wheel, ← → adjust, Enter · Esc back", new Vector2(0, -290), 15, new Color(0.55f, 0.55f, 0.55f));
            // QF14: the Scene select pages scroll; the bar sits right of the 540-wide rows, from the first row's top edge
            // (151) to the last visible row's bottom edge (-214) on the 720-high reference canvas
            _scroll = new MenuScroll(ListRows);
            _scroll.Make(_canvasGo.transform, 292f, ListTop + ListBar * 0.5f, (ListRows - 1) * ListPitch + ListBar);
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
            ("Crouch-sneak · climb down at an edge", "B", "C"),
            ("Hanging: climb up · shimmy · let go", "Up · left/right · B", "W · A/D · Space"),
            ("Telekinesis (highlighted object)", "RT", "Right mouse"),
            ("Skip a line", "Y", "Enter"),
            ("Restart from the checkpoint", "D-pad down", "Backspace"),
            ("Pause menu", "Start", "Esc"),
            // QF13 (12): the hover bike, on land and under water (flying controls: the stick steers AND pitches)
            ("Bike: steer · accelerate · brake", "Left stick · RT · LT", "A/D · W (or right mouse) · S / Q"),
            ("Bike under water: steer · dive / climb", "Left stick (push forward = dive)", "A/D · W dive / S climb"),
            ("Bike under water: forward · brake", "RT · LT", "Shift / right mouse · Q"),
            // QF20 (2): fighting - kicks have their own button (in boss fights); mixing X and Y chains one combo
            ("Fight: punch · kick (mix them: a combo)", "X · Y", "Left mouse / F · E"),
            ("Fight: block · dodge · jump", "LT · B · A", "Q · Ctrl · Space"),
            ("Fight: takedown (he is dazed) · lock-on", "Y · R3", "E · Tab"),
            ("Arc blades: draw / sheathe · cut (drawn)", "RB · X", "G · Left mouse / F"),
        };

        void BuildControlsTable()
        {
            foreach (var g in _extra) if (g != null) Destroy(g);
            _extra.Clear();
            if (_page != Page.Controls) return;
            _extra.AddRange(ControlsTable(_canvasGo.transform, _font));
        }

        /// <summary>Round 9 control map as a table (header at y 132, rows down to y -186; x from -270 to +540 on the
        /// 1280x720 reference, so it also fits 16:10's 1152-wide canvas). QF14: shared by the pause menu and the main menu.</summary>
        public static List<GameObject> ControlsTable(Transform parent, Font font)
        {
            var made = new List<GameObject>();
            var head = new Color(0.62f, 0.62f, 0.62f); var ink = new Color(0.92f, 0.92f, 0.92f);
            Cell("", -150, 132, 16, head, 420); Cell("XBOX", 170, 132, 16, head, 230); Cell("KEYBOARD / MOUSE", 400, 132, 16, head, 260);
            for (int i = 0; i < ControlMap.Length; i++)
            {
                float y = 108 - i * Mathf.Min(21f, 306f / Mathf.Max(1, ControlMap.Length - 1));   // QF20: more rows - closer, same bottom
                Cell(ControlMap[i].what, -150, y, 17, ink, 420);
                Cell(ControlMap[i].pad, 170, y, 17, ink, 230);
                Cell(ControlMap[i].keys, 400, y, 17, ink, 260);
            }
            return made;

            void Cell(string s, float x, float y, int size, Color c, float w)
            {
                var go = new GameObject("Text", typeof(RectTransform)); go.transform.SetParent(parent, false);
                var rt = (RectTransform)go.transform; rt.anchoredPosition = new Vector2(x - 120f, y); rt.sizeDelta = new Vector2(w, size * 1.6f); rt.pivot = new Vector2(0f, 0.5f);
                var t = go.AddComponent<Text>(); t.font = font; t.fontSize = size; t.text = s; t.color = c; t.alignment = TextAnchor.MiddleLeft;
                t.horizontalOverflow = HorizontalWrapMode.Overflow; t.raycastTarget = false;
                made.Add(go);
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
            // QF14: the Scene select pages show ListRows rows of the scroll window (centres 135 .. 135 - 9 x 37 = -198 on the
            // 720-high reference canvas, the hint at -290) and scroll with the selection; the other pages always fit
            bool list = _page == Page.Select || _page == Page.Chapter;
            int top = 0;
            if (_scroll != null) { _scroll.Visible = list ? ListRows : 99; top = _scroll.Follow(_sel, items.Count); }
            for (int i = 0; i < _labels.Count; i++)
            {
                bool vis = i < items.Count && (!list || (i >= top && i < top + ListRows));
                _bars[i].gameObject.SetActive(vis);
                if (!vis) continue;
                var rt = (RectTransform)_bars[i].transform;
                bool sel = list;
                rt.anchoredPosition = _page == Page.Controls ? new Vector2(0, -230) : sel ? new Vector2(0, ListTop - (i - top) * ListPitch) : new Vector2(0, 100 - i * 52);
                rt.sizeDelta = sel ? new Vector2(540, 32) : new Vector2(440, 46);
                _labels[i].fontSize = sel ? 19 : 26;
                bool on = i == _sel;
                _bars[i].color = on ? new Color(0.92f, 0.92f, 0.92f, 1f) : new Color(1f, 1f, 1f, 0.07f);
                _labels[i].text = items[i];
                _labels[i].color = on ? new Color(0.05f, 0.05f, 0.05f) : new Color(0.85f, 0.85f, 0.85f);
            }
            _title.text = Title();
        }
    }

    /// <summary>Round 13: the one shared settings list - the pause menu's Settings page and the ship's console Settings
    /// screen both show and change these rows (same state: AudioKit volumes, ControlsLegend.HintsOn; nothing duplicated).
    /// ← → adjust a row; A / Enter toggles a switch row.</summary>
    public static class GameSettings
    {
        public const int Music = 0, Sfx = 1, Voice = 2, Hints = 3, InvertSwim = 4;
        public const int Count = 5;
        public static string Name(int i) => i == Music ? "Music" : i == Sfx ? "Sound effects" : i == Voice ? "Voice" : i == Hints ? "Button hints" : i == InvertSwim ? "Invert up / down (bike under water)" : "";
        public static string Value(int i) => i == Music ? Bar(AudioKit.MusicVolume) : i == Sfx ? Bar(AudioKit.SfxVolume) : i == Voice ? Bar(AudioKit.VoiceVolume)
                                           : i == Hints ? (ControlsLegend.HintsOn ? "On" : "Off") : i == InvertSwim ? (HoverBike.InvertPitch ? "On" : "Off") : "";
        public static string Label(int i) => Name(i) + "   " + Value(i);
        /// <summary>The raw value of a row (volumes 0-1, switches 0 / 1) - check runs log it before / after a change.</summary>
        public static float Raw(int i) => i == Music ? AudioKit.MusicVolume : i == Sfx ? AudioKit.SfxVolume : i == Voice ? AudioKit.VoiceVolume : i == Hints ? (ControlsLegend.HintsOn ? 1f : 0f) : i == InvertSwim ? (HoverBike.InvertPitch ? 1f : 0f) : 0f;
        public static string Bar(float v) { int n = Mathf.Clamp(Mathf.RoundToInt(v * 10f), 0, 10); return new string('■', n) + new string('□', 10 - n); }

        /// <summary>← → on a row: volumes in steps of 10 %, switches flip.</summary>
        public static void Adjust(int i, int lr)
        {
            if (lr == 0) return;
            float step = 0.1f * lr;
            if (i == Music) AudioKit.MusicVolume = Mathf.Round((AudioKit.MusicVolume + step) * 10f) / 10f;
            else if (i == Sfx) { AudioKit.SfxVolume = Mathf.Round((AudioKit.SfxVolume + step) * 10f) / 10f; AudioKit.Play("menu_move", null, 1f); }
            else if (i == Voice) AudioKit.VoiceVolume = Mathf.Round((AudioKit.VoiceVolume + step) * 10f) / 10f;
            else if (i == Hints) { ControlsLegend.HintsOn = !ControlsLegend.HintsOn; AudioKit.Play("menu_move", null, 0.6f); }
            else if (i == InvertSwim) { HoverBike.InvertPitch = !HoverBike.InvertPitch; AudioKit.Play("menu_move", null, 0.6f); }
        }

        /// <summary>A / Enter on a row: switches flip (volumes do nothing).</summary>
        public static void Activate(int i)
        {
            if (i == Hints) ControlsLegend.HintsOn = !ControlsLegend.HintsOn;
            if (i == InvertSwim) HoverBike.InvertPitch = !HoverBike.InvertPitch;
        }

        public static string Describe() => $"music {AudioKit.MusicVolume:0.0} sfx {AudioKit.SfxVolume:0.0} voice {AudioKit.VoiceVolume:0.0} hints {(ControlsLegend.HintsOn ? "on" : "off")}";
    }

    /// <summary>Round 7 (3d) "Continue" save. QF6: it remembers the furthest checkpoint reached in Chapter 1 (0-7 on the
    /// Moon, 8 aboard, 9 galley, 10 the chapter done = free exploration), so quitting never means replaying the chapter.
    /// Saves from 0.10.1 and earlier (only "aboard") load as 10, as before.</summary>
    public static class SaveGame
    {
        // Check runs use their own keys and remove them again, so a test never leaves a save on Dan's PC.
        static bool Test => DemoBoot.CaptureDir != null;
        static string OldKey => Test ? "seven_test_ch1_aboard" : "seven_ch1_aboard";
        static string Key => Test ? "seven_test_ch1_progress" : "seven_ch1_progress";
        public static int Progress => PlayerPrefs.HasKey(Key) ? PlayerPrefs.GetInt(Key) : PlayerPrefs.GetInt(OldKey, 0) == 1 ? Chapter1State.CpExplore : -1;
        public static bool HasChapter1Save => Progress >= 0;
        /// <summary>Round 13 (console Log / Field Guide): the furthest Chapter 1 checkpoint this playthrough has reached
        /// (Continue's save follows the player back on a Scene Select start; this never goes back until a new game).</summary>
        static string FurthestKey => Test ? "seven_test_ch1_furthest" : "seven_ch1_furthest";
        public static int Furthest => Mathf.Max(PlayerPrefs.GetInt(FurthestKey, -1), Progress);
        /// <summary>Round 13 (0c): a new game - Chapter 1 from the main menu - starts every chapter fresh: the Chapter 1
        /// checkpoint, the Venus save and the console's progress are cleared (Continue then resumes the new playthrough).</summary>
        public static bool AnyProgress => HasChapter1Save || VenusState.OwnHasProgress || EuropaState.OwnHasProgress;     // QF14: read without switching a Scene Select slot; Europa too (NewGame clears it)
        /// <summary>QF14: chapters unlocked on this PC (ChapterList) - remembered, a new game keeps them.</summary>
        static string UnlockKey(string id) => (Test ? "seven_test_unlocked_" : "seven_unlocked_") + id;
        public static bool WasUnlocked(string id) => PlayerPrefs.GetInt(UnlockKey(id), 0) == 1;
        public static void MarkUnlocked(string id) { PlayerPrefs.SetInt(UnlockKey(id), 1); PlayerPrefs.Save(); Debug.Log($"[Seven] chapter unlocked: {id} ({UnlockKey(id)})"); }
        public static void ClearTestUnlocks()
        {
            if (!Test) return;
            foreach (var c in ChapterList.All) PlayerPrefs.DeleteKey("seven_test_unlocked_" + c.Id);
            PlayerPrefs.Save();
        }
        public static void NewGame()
        {
            PlayerPrefs.DeleteKey(Key); PlayerPrefs.DeleteKey(OldKey); PlayerPrefs.DeleteKey(FurthestKey);
            VenusState.ClearOwnSave(); EuropaState.ClearOwnSave(); MarsState.ClearOwnSave(); AsteroidState.ClearOwnSave(); BiavehState.ClearOwnSave(); WorldFlags.Clear();   // QF23 (24): the mothership's save too
            PlayerPrefs.Save();
            Debug.Log("[Seven] new game: Chapter 1 checkpoint, Venus save and console progress reset");
        }
        /// <summary>Save the checkpoint just reached (a later Scene Select start may go back: the save follows the player).</summary>
        public static void SaveProgress(int cp)
        {
            PlayerPrefs.SetInt(Key, cp);
            if (cp > Furthest) PlayerPrefs.SetInt(FurthestKey, cp);
            if (cp >= Chapter1State.CpAboard) PlayerPrefs.SetInt(OldKey, 1);
            PlayerPrefs.Save();
            Debug.Log($"[Seven] saved: chapter 1 checkpoint {cp} ({Chapter1State.CpName(cp)}) ({Key})");
        }
        public static void ClearTestSave()
        {
            if (!Test) return;
            PlayerPrefs.DeleteKey("seven_test_ch1_aboard"); PlayerPrefs.DeleteKey("seven_test_ch1_progress"); PlayerPrefs.DeleteKey("seven_test_ch1_furthest"); PlayerPrefs.Save();
            ClearTestUnlocks();
        }

        /// <summary>The main menu's Continue: back to the saved checkpoint (free exploration once the chapter is done).</summary>
        public static void Continue()
        {
            int p = Progress;
            Debug.Log($"[Seven] continue from checkpoint {p} ({Chapter1State.CpName(p)})");
            Chapter1State.StartAt(Mathf.Max(0, p));
        }
    }
}
