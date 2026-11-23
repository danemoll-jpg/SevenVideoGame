using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Seven
{
    /// <summary>
    /// The main menu (first scene). QF14 (Dan): Continue (when a save exists) · Start new game · one entry per UNLOCKED
    /// chapter (from <see cref="ChapterList"/> - a new chapter appears by itself) · Settings · Controls · Quit. A chapter
    /// opens its page: Play from the start, its sections (the old Scene Select entries), Back. The early test demos are no
    /// longer in the menu; their switches still work (-fight / -room / -saucer / -ratwing). Lists that do not fit scroll
    /// (<see cref="MenuScroll"/>). Pad: stick or d-pad + A, B = back. Keys: W/S or arrows + Enter or Space, Esc = back,
    /// ← → adjust a setting; the mouse wheel moves the selection. Strictly monochrome uGUI drawn by the menu camera (so it
    /// can be captured off-screen): -capture -menu = 00_menu.png; -menupages = every page (see <see cref="Pages"/>).
    /// </summary>
    public class DemoMenu : MonoBehaviour
    {
        public const string SceneName = "Menu";
        public Camera Cam;
        public VoiceBank Bank;          // for the -soundcheck run

        public const string NewGameLabel = "Start new game";
        enum Page { Main, Chapter, Settings, Controls }
        const string ContinueAst = "@continueasteroid";
        const string ContinueBia = "@continuebiaveh";
        const string ContinueCat = "@continuecattalus";
        const string ContinueFin = "@continuefinale";
        static bool _finPlayStarted;   // Round 19: the player-style run starts once (the credits come back to this menu)
        const string ContinueCh1 = "@continue", ContinueVenus = "@continuevenus", ContinueEuropa = "@continueeuropa", ContinueMars = "@continuemars", NewKey = "@new",
                     ChapterKey = "@ch:", SettingsKey = "@settings", ControlsKey = "@controls", QuitKey = "@quit", BackKey = "@back",
                     StartKey = "@start", SectionKey = "@sec:", SettingKey = "@set:";
        (string label, string key)[] Items;
        Page _page;
        int _chapter = -1;              // ChapterList.All index of the open chapter page
        string _confirm;                // the row (key) that is asking "…?  Yes (A)"

        public static DemoMenu I { get; private set; }
        /// <summary>QF12 (MenuPilot): the rows as the player sees them (a confirm shows its question) and the highlighted row.</summary>
        public string[] Labels { get { if (_labels == null) return new string[0]; var l = new string[_labels.Length]; for (int i = 0; i < l.Length; i++) l[i] = _labels[i] != null ? _labels[i].text : ""; return l; } }
        public int Selected => _sel;
        /// <summary>QF6 playtest: what the highlighted entry says (the driver reads the menu like a person).</summary>
        public string SelectedLabel => Items != null && _sel >= 0 && _sel < Items.Length ? Items[_sel].label : null;
        /// <summary>Round 13 (0c): the "Start a new game?" confirm is showing.</summary>
        public bool ConfirmingNewGame => _confirm == NewKey || (_confirm == StartKey && _chapter == 0);
        /// <summary>A chapter page (its sections = the old Scene Select entries) is showing.</summary>
        public bool OnSelectPage => _page == Page.Chapter;
        public string PageTitle => _page == Page.Main ? "MAIN MENU" : _page == Page.Settings ? "SETTINGS" : _page == Page.Controls ? "CONTROLS" : Ch != null ? Ch.Name.ToUpperInvariant() : "";

        GameObject _canvasGo;
        Text _hint;
        int _sel;
        Text[] _labels;
        Image[] _marks;
        Font _font;
        MenuScroll _scroll;
        float _stickReady;
        bool _capturing;
        ChapterDef Ch => _chapter >= 0 && _chapter < ChapterList.All.Length ? ChapterList.All[_chapter] : null;

        // ---- layout (the 1280x720 reference canvas, matched by height: 720 units high at 16:9 AND at the Steam Deck's
        // 16:10, where it is 1152 wide instead of 1280 - every row / bar / text stays inside x ±360, so both fit)
        // main page: logo 300 wide (175 high) centred at y 255 = 167..343; rows centred 125 .. 125 - 7 x 50 = -225 (8 rows
        // show; bottom edge -247); the hint at -305.
        const float MainTop = 125f, MainPitch = 50f, MainBar = 44f, MainWidth = 520f; const int MainRows = 8;
        // chapter / settings pages: title at y 295; rows centred 240 .. 240 - 11 x 42 = -222 (12 rows show; bottom edge -240).
        const float PageTop = 240f, PagePitch = 42f, PageBar = 36f, PageWidth = 640f; const int PageRows = 12;

        static (string, string)[] MainItems()
        {
            var l = new List<(string, string)>();
            VenusState.UseOwnSave(); EuropaState.UseOwnSave(); MarsState.UseOwnSave(); AsteroidState.UseOwnSave(); BiavehState.UseOwnSave(); CattalusState.UseOwnSave(); FinaleState.UseOwnSave();
            if (FinaleState.Released && FinaleState.Reached && !FinaleState.Complete)               // Round 19: the Moon finale in progress
                l.Add(("Continue  —  the Moon: " + FinaleLayout.CpNames[FinaleState.Checkpoint], ContinueFin));
            else if (CattalusState.Reached && !CattalusState.Complete)          // Round 18: a Cattalus game in progress
                l.Add(("Continue  —  " + CattalusLayout.CpNames[CattalusState.Checkpoint], ContinueCat));
            else if (BiavehState.Reached && !BiavehState.Complete)            // Round 17: a mothership game in progress
                l.Add(("Continue  —  " + BiavehLayout.CpNames[BiavehState.Checkpoint], ContinueBia));
            else if (AsteroidState.Reached && !AsteroidState.Complete)        // Round 16: an asteroid game in progress
                l.Add(("Continue  —  " + AsteroidLayout.CpNames[AsteroidState.Checkpoint], ContinueAst));
            else if (MarsState.Reached && !MarsState.Complete)           // Round 15: a Mars game in progress
                l.Add(("Continue  —  " + MarsLayout.CpNames[MarsState.Checkpoint], ContinueMars));
            else if (EuropaState.Reached && !EuropaState.Complete)       // Round 14: the furthest chapter first
                l.Add(("Continue  —  " + EuropaLayout.CpNames[EuropaState.Checkpoint], ContinueEuropa));
            else if (VenusState.Reached)
                l.Add(("Continue  —  Venus: " + (VenusState.Objective ?? "the saucer"), ContinueVenus));
            else if (SaveGame.HasChapter1Save)
                l.Add((SaveGame.Progress >= Chapter1State.CpExplore ? "Continue  —  explore the saucer" : "Continue  —  " + Chapter1State.CpName(SaveGame.Progress), ContinueCh1));
            l.Add((NewGameLabel, NewKey));
            for (int i = 0; i < ChapterList.All.Length; i++)                 // QF14: every unlocked chapter, from the one list
                if (ChapterList.IsUnlocked(ChapterList.All[i])) l.Add((ChapterList.All[i].Name + (ChapterList.All[i].Id == "finale" && FinaleState.GameFinished ? "   -   the journey is complete" : ""), ChapterKey + i));   // Round 19: the finished mark
            l.Add(("Settings", SettingsKey)); l.Add(("Controls", ControlsKey)); l.Add(("Quit", QuitKey));
            return l.ToArray();
        }

        (string, string)[] PageItems()
        {
            var l = new List<(string, string)>();
            switch (_page)
            {
                case Page.Chapter:
                {
                    l.Add((ChapterList.PlayFromStartLabel, StartKey));
                    var s = Ch != null ? Ch.Sections() : new (string label, System.Action start)[0];
                    for (int i = 0; i < s.Length; i++) l.Add((s[i].label, SectionKey + i));
                    break;
                }
                case Page.Settings:
                    for (int i = 0; i < GameSettings.Count; i++) l.Add((GameSettings.Label(i), SettingKey + i));
                    break;
                case Page.Controls: break;
                default: return MainItems();
            }
            l.Add(("Back", BackKey));
            return l.ToArray();
        }

        static bool _routesDone;
        void Start()
        {
            I = this;
            if (DemoBoot.CaptureDir != null && DemoBoot.Routes != null && !_routesDone) { _routesDone = true; WorldFlags.FakeForTest(DemoBoot.Routes); }   // Round 15 routing checks
            if (DemoBoot.MenuPick != null && DemoBoot.CaptureDir != null) { ChapterList.UnlockAllForTest(); Build(); gameObject.AddComponent<MenuPilot>(); return; }   // QF12: a run that starts in the real menu (QF14: every chapter open, test keys)
            if (DemoBoot.PlayerSim && !DemoBoot.Chapter1 && !DemoBoot.VenusPlay && !DemoBoot.VenusPlay2 && !DemoBoot.EuropaPlay && !(DemoBoot.MarsPlay && DemoBoot.MenuPick == null) && !DemoBoot.AstPlay && !DemoBoot.BiaPlay && !DemoBoot.CatPlay && !DemoBoot.FinPlay) { Build(); return; }     // QF6 playtest: the driver uses the menu (QF10: the Venus run goes straight in)
            if (DemoBoot.CaptureDir != null && DemoBoot.MenuPages) { StartCoroutine(Pages()); return; }
            if (DemoBoot.CaptureDir != null && DemoBoot.MenuShot && DemoBoot.MenuVenusConfirm)
            {
                // the Venus page with "Play from the start" asking to confirm (QF14: was the main menu's "Chapter 2 — Venus")
                ChapterList.UnlockAllForTest();
                OpenChapter(ChapterList.IndexOf(ChapterList.ById("venus")), 0); _confirm = StartKey; Refresh();
                StartCoroutine(Shot()); return;
            }
            if (DemoBoot.CaptureDir != null && DemoBoot.MenuShot) { Build(); StartCoroutine(Shot()); return; }
            if (DemoBoot.Gallery) GalleryTV.Preload();      // the gallery check run exercises the preload path
            if (DemoBoot.SoundCheck) { gameObject.AddComponent<TelepathySoundCheck>(); return; }
            if (DemoBoot.Continue) { SaveGame.Continue(); return; }
            if (DemoBoot.QF16Check) { Chapter1State.StartAt(Chapter1State.CpExplore); return; }
            if (DemoBoot.QF20Check) { WorldFlags.FakeForTest("dansave"); Chapter1State.StartAt(Chapter1State.CpExplore); return; }
            if (DemoBoot.QF23Revisit) { QF23Check.FakeOldSave(); Chapter1State.StartAt(Chapter1State.CpExplore); return; }   // QF23 (24)   // QF20 (1)   // QF16 check run: free exploration aboard
            if (DemoBoot.Chapter1Aboard) { Chapter1State.StartAboard(); return; }
            if (DemoBoot.EuropaCp >= 0 && DemoBoot.CaptureDir != null) { SceneManager.LoadScene("Europa"); return; }     // Round 14 check runs
            if (DemoBoot.MarsCp >= 0 && DemoBoot.CaptureDir != null) { if (DemoBoot.MarsCp >= MarsLayout.CpHome) { MarsState.ResetForTest(); MarsState.Fake(DemoBoot.MarsCp, true); MarsState.HomeAboard = true; Chapter1State.Aboard = true; SceneManager.LoadScene("Saucer"); } else SceneManager.LoadScene("Mars"); return; }
            if (DemoBoot.Mars && DemoBoot.CaptureDir != null && DemoBoot.MenuPick == null) { MarsState.ResetForTest(); MarsState.StartFresh(); return; }
            // Round 19: the Moon finale
            if (DemoBoot.FinPlay && DemoBoot.CaptureDir != null && DemoBoot.FinCp < 0 && !_finPlayStarted) { _finPlayStarted = true; WorldFlags.FakeForTest("finale"); Chapter1State.StartAt(Chapter1State.CpExplore); SaucerLocation.System = StarMap.Bia; SaucerLocation.Set("cattalus"); return; }   // free aboard -> the console -> the Moon
            if (DemoBoot.FinCp >= 0 && DemoBoot.CaptureDir != null) { WorldFlags.FakeForTest("finale"); FinaleState.ResetForTest(); FinaleState.Fake(DemoBoot.FinCp, true); if (DemoBoot.FinCp <= FinaleLayout.CpArrival) { FinaleState.ArrivalAboard = true; Chapter1State.Aboard = true; SceneManager.LoadScene("Saucer"); } else SceneManager.LoadScene("Finale"); return; }
            if (DemoBoot.Finale && DemoBoot.CaptureDir != null) { WorldFlags.FakeForTest("finale"); FinaleState.StartFresh(); return; }
            // Round 18: Cattalus (Panther's homeworld)
            if (DemoBoot.CatPlay && DemoBoot.CaptureDir != null && DemoBoot.CatCp < 0) { WorldFlags.FakeForTest("cattalus"); Chapter1State.StartAt(Chapter1State.CpExplore); SaucerLocation.System = StarMap.Bia; SaucerLocation.Set("biaveh"); return; }   // free aboard in Veyra's system: console -> Cattalus
            if (DemoBoot.CatCp >= 0 && DemoBoot.CaptureDir != null) { WorldFlags.FakeForTest("cattalus"); CattalusState.ResetForTest(); CattalusState.Fake(DemoBoot.CatCp, true, DemoBoot.CatLose ? CattalusState.Result.RunnerUp : CattalusState.Result.Won); if (DemoBoot.CatCp >= CattalusLayout.CpHome) { CattalusState.HomeAboard = true; Chapter1State.Aboard = true; SceneManager.LoadScene("Saucer"); } else if (DemoBoot.CatCp <= CattalusLayout.CpArrival) { CattalusState.ArrivalAboard = true; Chapter1State.Aboard = true; SceneManager.LoadScene("Saucer"); } else SceneManager.LoadScene("Cattalus"); return; }
            if (DemoBoot.Cattalus && DemoBoot.CaptureDir != null) { WorldFlags.FakeForTest("cattalus"); CattalusState.ResetForTest(); CattalusState.StartFresh(); return; }
            // Round 17: the Biavian Mothership
            if (DemoBoot.BiaPlay && DemoBoot.CaptureDir != null && DemoBoot.BiaCp < 0) { WorldFlags.FakeForTest(DemoBoot.Routes ?? "biaveh"); Chapter1State.StartAt(Chapter1State.CpExplore); SaucerLocation.System = StarMap.Bia; SaucerLocation.Set("asteroid"); return; }   // free aboard in Veyra's system: console -> the mothership
            if (DemoBoot.BiaCp >= 0 && DemoBoot.CaptureDir != null) { WorldFlags.FakeForTest(DemoBoot.Routes ?? "biaveh"); BiavehState.ResetForTest(); BiavehState.Fake(DemoBoot.BiaCp, true); if (DemoBoot.BiaCp >= BiavehLayout.CpHome) { BiavehState.HomeAboard = true; Chapter1State.Aboard = true; SceneManager.LoadScene("Saucer"); } else if (DemoBoot.BiaCp <= BiavehLayout.CpArrival) { BiavehState.ArrivalAboard = true; Chapter1State.Aboard = true; SceneManager.LoadScene("Saucer"); } else SceneManager.LoadScene("Biaveh"); return; }
            if (DemoBoot.Bia && DemoBoot.CaptureDir != null) { WorldFlags.FakeForTest(DemoBoot.Routes ?? "biaveh"); BiavehState.ResetForTest(); BiavehState.StartFresh(); return; }
            // Round 16: the Asteroid
            if (DemoBoot.AstPlay && DemoBoot.CaptureDir != null && DemoBoot.AstCp < 0) { WorldFlags.FakeForTest("trvast"); Chapter1State.StartAt(Chapter1State.CpExplore); return; }   // free aboard: console -> Biaveh -> TRV_AST
            if (DemoBoot.AstCp >= 0 && DemoBoot.CaptureDir != null) { AsteroidState.ResetForTest(); AsteroidState.Fake(DemoBoot.AstCp, true); if (DemoBoot.AstCp >= AsteroidLayout.CpHome) { AsteroidState.HomeAboard = true; Chapter1State.Aboard = true; SceneManager.LoadScene("Saucer"); } else if (DemoBoot.AstCp <= AsteroidLayout.CpArrival) { AsteroidState.ArrivalAboard = true; Chapter1State.Aboard = true; SceneManager.LoadScene("Saucer"); } else SceneManager.LoadScene("Asteroid"); return; }
            if (DemoBoot.Asteroid && DemoBoot.CaptureDir != null) { AsteroidState.ResetForTest(); AsteroidState.StartFresh(); return; }
            if (DemoBoot.Europa && DemoBoot.CaptureDir != null && !DemoBoot.EuropaPlay) { EuropaState.ResetForTest(); EuropaState.StartFresh(); return; }
            if (DemoBoot.VenusSelect != null) { VenusState.StartAt(DemoBoot.VenusSelect); return; }
            if (DemoBoot.Chapter2) { SceneManager.LoadScene("Venus"); return; }
            if (DemoBoot.Chapter1) { SceneManager.LoadScene("Chapter1"); return; }
            if (DemoBoot.Saucer) { SceneManager.LoadScene("Saucer"); return; }
            if (DemoBoot.Ratwing) { SceneManager.LoadScene("Ratwing"); return; }
            if (DemoBoot.Fight || DemoBoot.Room || DemoBoot.CaptureDir != null) { SceneManager.LoadScene("FightDemo"); return; }
            Build();
        }

        void OpenMain(string selectKey = null)
        {
            _page = Page.Main; _chapter = -1; _confirm = null; _sel = 0; Build();
            _sel = selectKey == null ? 0 : Mathf.Max(0, System.Array.FindIndex(Items, x => x.key == selectKey)); Refresh();
        }
        void OpenChapter(int chapter, int sel) { _page = Page.Chapter; _chapter = chapter; _confirm = null; _sel = 0; Build(); _sel = Mathf.Clamp(sel, 0, Items.Length - 1); Refresh(); }
        void OpenPage(Page p) { _page = p; _confirm = null; _sel = 0; Build(); Refresh(); }

        void Build()
        {
            Items = PageItems();
            _sel = Mathf.Clamp(_sel, 0, Mathf.Max(0, Items.Length - 1));
            if (_canvasGo == null) { MusicSystem.Play("main_theme", 1.5f); GalleryTV.Preload(); }   // Round 8: decode the gallery while the menu is idle
            else Destroy(_canvasGo);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("MenuCanvas");
            _canvasGo = canvasGo;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Cam;
            canvas.planeDistance = 1f;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1f;

            var white = new Color(0.95f, 0.95f, 0.95f);
            bool main = _page == Page.Main;
            // Round 10 (Dan): the logo image ("7" + THE OBSERVER, Arial Bold typeset by Tools/make_logo.py; no font shipped)
            if (!main) Text(canvasGo.transform, PageTitle, new Vector2(0, 295), 30, white);
            else if (Logo.Add(canvasGo.transform, new Vector2(0, 255), 300f, white) == null)
            {
                Text(canvasGo.transform, "7", new Vector2(0, 275), 90, white);
                Text(canvasGo.transform, "THE OBSERVER", new Vector2(0, 205), 24, new Color(0.7f, 0.7f, 0.7f));
            }
            if (_page == Page.Controls) PauseMenu.ControlsTable(canvasGo.transform, _font);     // the pause menu's table (one control map)

            _labels = new Text[Items.Length];
            _marks = new Image[Items.Length];
            float width = main ? MainWidth : PageWidth, barH = main ? MainBar : PageBar;
            for (int i = 0; i < Items.Length; i++)
            {
                var bar = new GameObject("Item" + i, typeof(RectTransform));
                bar.transform.SetParent(canvasGo.transform, false);
                var rt = (RectTransform)bar.transform;
                rt.sizeDelta = new Vector2(width, barH);
                _marks[i] = bar.AddComponent<Image>();
                _labels[i] = Text(bar.transform, Items[i].label, Vector2.zero, main ? 28 : 22, Color.white);
                // QF14: a long row (the Continue row names the checkpoint) shrinks to fit its bar instead of running past it
                { var lt = _labels[i]; ((RectTransform)lt.transform).sizeDelta = new Vector2(width - 16f, barH); lt.horizontalOverflow = HorizontalWrapMode.Wrap; lt.resizeTextForBestFit = true; lt.resizeTextMinSize = 12; lt.resizeTextMaxSize = main ? 28 : 22; }
            }
            // QF14: the scroll bar right of the rows, from the first row's top edge to the last visible row's bottom edge
            int rows = main ? MainRows : PageRows; float top = main ? MainTop : PageTop, pitch = main ? MainPitch : PagePitch;
            _scroll = new MenuScroll(rows);
            _scroll.Make(canvasGo.transform, width * 0.5f + 20f, top + barH * 0.5f, (rows - 1) * pitch + barH);
            _hint = Text(canvasGo.transform, "", new Vector2(0, -305), 16, new Color(0.55f, 0.55f, 0.55f));
            Refresh();
        }

        Text Text(Transform parent, string s, Vector2 pos, int size, Color c)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(900, size * 1.5f);
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = size; t.text = s; t.color = c; t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.raycastTarget = false;
            return t;
        }

        static string Question(string confirm) { int q = confirm.IndexOf('?'); return q >= 0 ? confirm.Substring(0, q + 1) : confirm; }
        string ConfirmText => _confirm == QuitKey ? "Quit the game?" : _confirm == NewKey ? ChapterList.All[0].Confirm : _confirm == StartKey && Ch != null ? Ch.Confirm : null;

        void Refresh()
        {
            if (_labels == null || Items == null) return;
            if (_page == Page.Settings) for (int i = 0; i < GameSettings.Count && i < Items.Length; i++) Items[i].label = GameSettings.Label(i);
            var ct = ConfirmText;
            if (_hint != null)
                _hint.text = ct != null ? ct + "   A = yes   ·   B = no"
                    : _page == Page.Main ? "stick / d-pad + A"
                    : _page == Page.Settings ? "stick / d-pad + A · ← → adjust · B back"
                    : "stick / d-pad + A · B back";
            bool main = _page == Page.Main;
            float top0 = _page == Page.Controls ? -240f : main ? MainTop : PageTop, pitch = main ? MainPitch : PagePitch;
            int top = _scroll != null ? _scroll.Follow(_sel, Items.Length) : 0;
            int rows = _scroll != null ? _scroll.Visible : Items.Length;
            for (int i = 0; i < Items.Length; i++)
            {
                bool vis = i >= top && i < top + rows;
                _marks[i].gameObject.SetActive(vis);
                if (!vis) continue;
                ((RectTransform)_marks[i].transform).anchoredPosition = new Vector2(0, top0 - (i - top) * pitch);
                bool on = i == _sel;
                var key = Items[i].key;
                _labels[i].text = _confirm != null && key == _confirm && ct != null ? Question(ct) + "  Yes (A)" : Items[i].label;
                _marks[i].color = on ? new Color(0.92f, 0.92f, 0.92f, 1f) : new Color(1f, 1f, 1f, 0.06f);
                _labels[i].color = on ? new Color(0.05f, 0.05f, 0.05f) : new Color(0.85f, 0.85f, 0.85f);
            }
        }

        void Update()
        {
            if (_labels == null || _capturing || (DemoBoot.CaptureDir != null && !DemoBoot.PlayerSim && DemoBoot.MenuPick == null)) return;
            int d = 0, lr = 0;
            var kb = Keyboard.current; var gp = Gamepad.current;
            if (kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)) d = -1;
            if (kb != null && (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)) d = 1;
            if (kb != null && (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)) lr = -1;
            if (kb != null && (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)) lr = 1;
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
                    if (Mathf.Abs(st.y) >= Mathf.Abs(st.x)) d = st.y > 0 ? -1 : 1; else lr = st.x > 0 ? 1 : -1;
                    _stickReady = Time.unscaledTime + 0.35f;
                }
            }
            if (d == 0) d = MenuScroll.Wheel();          // QF14: the wheel moves the selection; long lists scroll with it
            if (d != 0) { _sel = (_sel + d + Items.Length) % Items.Length; _confirm = null; AudioKit.Play("menu_move", null, 0.6f); Refresh(); }
            if (_page == Page.Settings && lr != 0 && _sel < GameSettings.Count) { GameSettings.Adjust(_sel, lr); Refresh(); }
            bool go = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                      || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            bool back = (kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.buttonEast.wasPressedThisFrame);
            if (back && _confirm != null) { _confirm = null; Refresh(); return; }
            if (back && _page != Page.Main) { Back(); return; }
            if (!go) return;
            AudioKit.Play("menu_confirm", null, 0.7f);
            Choose();
        }

        /// <summary>Back from a page: to the main menu, on the row that opened it.</summary>
        void Back() => OpenMain(_page == Page.Chapter ? ChapterKey + _chapter : _page == Page.Settings ? SettingsKey : ControlsKey);

        void Choose()
        {
            var key = Items[_sel].key;
            if (key == BackKey) { Back(); return; }
            if (key.StartsWith(ChapterKey)) { OpenChapter(int.Parse(key.Substring(ChapterKey.Length)), 0); return; }
            if (key == SettingsKey) { OpenPage(Page.Settings); return; }
            if (key == ControlsKey) { OpenPage(Page.Controls); return; }
            if (key.StartsWith(SettingKey)) { GameSettings.Activate(int.Parse(key.Substring(SettingKey.Length))); Refresh(); return; }
            if (key == QuitKey)
            {
                if (_confirm != QuitKey) { _confirm = QuitKey; Refresh(); return; }
                PauseMenu.QuitGame();
                return;
            }
            if (key == ContinueCh1) { SaveGame.Continue(); return; }
            if (key == ContinueVenus) { Chapter1State.Reset(); VenusState.ContinueOwn(); return; }
            if (key == ContinueEuropa) { Chapter1State.Reset(); EuropaState.ContinueOwn(); return; }
            if (key == ContinueMars) { Chapter1State.Reset(); MarsState.ContinueOwn(); return; }
            if (key == ContinueAst) { Chapter1State.Reset(); AsteroidState.ContinueOwn(); return; }
            if (key == ContinueBia) { Chapter1State.Reset(); BiavehState.ContinueOwn(); return; }
            if (key == ContinueCat) { Chapter1State.Reset(); CattalusState.ContinueOwn(); return; }
            if (key == ContinueFin) { Chapter1State.Reset(); FinaleState.ContinueOwn(); return; }
            // Round 13 (0c) / QF14: "Start new game" = Chapter 1 from the start, every chapter's progress reset (ask once if
            // there is any); a chapter page's "Play from the start" = that chapter fresh (asks when its progress would go)
            if (key == NewKey || key == StartKey)
            {
                var ch = key == NewKey ? ChapterList.All[0] : Ch;
                if (ch == null) return;
                if (ch.HasProgress != null && ch.HasProgress() && _confirm != key) { _confirm = key; Refresh(); return; }
                _confirm = null;
                ChapterList.StartFromBeginning(ch, key == NewKey ? " (start new game)" : "");
                return;
            }
            if (key.StartsWith(SectionKey) && Ch != null) { ChapterList.StartSection(Ch, int.Parse(key.Substring(SectionKey.Length)), ""); return; }
        }

        // ------------------------------------------------------------------------------------------ captures
        RenderTexture _rt;
        void EnsureTarget()
        {
            if (_rt != null) return;
            // the screen's own size, so the canvas lays out exactly as on screen (-screen-width/-height 1280x720, 1920x1080,
            // 1280x800 for the Steam Deck)
            int w = Screen.width >= 64 ? Screen.width : 1280, h = Screen.height >= 64 ? Screen.height : 720;
            _rt = new RenderTexture(w, h, 24) { antiAliasing = 4 };
            Cam.targetTexture = _rt;
        }

        /// <summary>Render the current frame into file; returns the largest |R-G| / |G-B| over every pixel (monochrome check).</summary>
        IEnumerator ShotTo(string file, System.Action<int> colour = null)
        {
            EnsureTarget();
            for (int i = 0; i < 4; i++) yield return null;
            yield return new WaitForEndOfFrame();
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            var tex = new Texture2D(_rt.width, _rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            Directory.CreateDirectory(DemoBoot.CaptureDir);
            File.WriteAllBytes(Path.Combine(DemoBoot.CaptureDir, file), tex.EncodeToPNG());
            if (colour != null)
            {
                int worst = 0;
                foreach (var p in tex.GetPixels32()) worst = Mathf.Max(worst, Mathf.Abs(p.r - p.g), Mathf.Abs(p.g - p.b));
                colour(worst);
            }
            Destroy(tex);
        }

        IEnumerator Shot()
        {
            yield return ShotTo("00_menu.png");
            File.WriteAllText(Path.Combine(DemoBoot.CaptureDir, "menu_log.txt"),
                $"menu shot, muted={DemoBoot.Muted}, listenerVolume={AudioListener.volume}\n");
            Application.Quit();
        }

        /// <summary>
        /// QF14 (-capture dir -menupages): every menu page, captured off-screen at the window's size - 00_menu_main.png,
        /// 00_menu_chapter_&lt;n&gt;.png (n = 1.. in ChapterList order), 00_menu_chapter_&lt;n&gt;_scrolled.png (the longest page,
        /// selection on its last row), 00_menu_settings.png, 00_menu_controls.png, 00_menu_new_game_confirm.png, the pause
        /// menu's 00_pause_scene_select.png + 00_pause_chapter_&lt;n&gt;.png (+ _scrolled). Every chapter is unlocked on the test
        /// keys for this run only (removed again at the end; the player's saves are never read or written). capture_log.txt:
        /// "menu page '&lt;title&gt;': N items, visible M, scroll bar shown &lt;bool&gt;" per page + the monochrome check per shot.
        /// Run it once per resolution (16:9: 1280x720 / 1920x1080; Steam Deck 16:10: 1280x800).
        /// </summary>
        IEnumerator Pages()
        {
            _capturing = true;
            var cap = ChapterCapture.Ensure();
            SaveGame.ClearTestUnlocks();
            ChapterList.UnlockAllForTest();
            EnsureTarget();
            cap.Log($"menu pages (QF14): {_rt.width}x{_rt.height} (aspect {_rt.width / (float)_rt.height:0.000}; reference canvas 1280x720 matched by height = {720f * _rt.width / _rt.height:0} x 720 units), muted={DemoBoot.Muted}, listenerVolume={AudioListener.volume}; every chapter unlocked on the test keys for this run only");
            int shots = 0, worstColour = 0;
            void Colour(int c) { worstColour = Mathf.Max(worstColour, c); }
            IEnumerator Snap(string file, string what)
            {
                int c0 = -1;
                yield return ShotTo(file, c => { c0 = c; Colour(c); });
                shots++;
                cap.Log($"shot {file} ({what}) - max |R-G|,|G-B| = {c0}");
            }
            void LogPage(string extra = "")
            {
                cap.Log($"menu page '{PageTitle}': {Items.Length} items, visible {Mathf.Min(Items.Length, _scroll.Visible)}, scroll bar shown {_scroll.Shown}, first row shown {_scroll.Top}, selected {_sel} '{Items[_sel].label}'{extra}");
            }

            OpenMain();
            yield return Snap("00_menu.png", "the main menu");
            yield return Snap("00_menu_main.png", "the main menu");
            LogPage($" - rows: {string.Join(" | ", System.Array.ConvertAll(Items, x => x.label))}");
            int longest = 0, longestCount = -1;
            for (int n = 0; n < ChapterList.All.Length; n++)
            {
                OpenChapter(n, 0);
                yield return Snap($"00_menu_chapter_{n + 1}.png", ChapterList.All[n].Name + " page");
                LogPage();
                if (Items.Length > longestCount) { longestCount = Items.Length; longest = n; }
            }
            OpenChapter(longest, longestCount - 1);
            yield return Snap($"00_menu_chapter_{longest + 1}_scrolled.png", ChapterList.All[longest].Name + " page, selection on the last row");
            LogPage(" (scrolled to the bottom)");
            OpenPage(Page.Settings);
            yield return Snap("00_menu_settings.png", "settings");
            LogPage();
            OpenPage(Page.Controls);
            yield return Snap("00_menu_controls.png", "controls");
            LogPage();
            OpenMain(NewKey); _confirm = NewKey; Refresh();
            yield return Snap("00_menu_new_game_confirm.png", "start new game, confirm showing");
            LogPage($" - confirm row '{_labels[_sel].text}'");

            // the pause menu's Scene select pages, drawn on this camera (the menu scene itself never opens the pause menu)
            var pm = PauseMenu.I;
            if (pm != null && Cam != null)
            {
                _canvasGo.SetActive(false);
                pm.OpenForCapture(Cam);
                pm.CaptureShow(-1, 0);
                yield return Snap("00_pause_scene_select.png", "pause menu: scene select");
                cap.Log($"menu page 'pause: {pm.PageTitle}': {pm.ItemCount} items, visible {pm.VisibleRows}, scroll bar shown {pm.ScrollBarShown}");
                int pl = 0, plCount = -1;
                for (int n = 0; n < ChapterList.All.Length; n++)
                {
                    pm.CaptureShow(n, 0);
                    yield return Snap($"00_pause_chapter_{n + 1}.png", "pause menu: " + ChapterList.All[n].Name);
                    cap.Log($"menu page 'pause: {pm.PageTitle}': {pm.ItemCount} items, visible {pm.VisibleRows}, scroll bar shown {pm.ScrollBarShown}");
                    if (pm.ItemCount > plCount) { plCount = pm.ItemCount; pl = n; }
                }
                pm.CaptureShow(pl, plCount - 1);
                yield return Snap($"00_pause_chapter_{pl + 1}_scrolled.png", "pause menu: " + ChapterList.All[pl].Name + ", selection on the last row");
                cap.Log($"menu page 'pause: {pm.PageTitle}': {pm.ItemCount} items, visible {pm.VisibleRows}, scroll bar shown {pm.ScrollBarShown}, first row shown {pm.ScrollTop} (scrolled to the bottom)");
                pm.Close();
                _canvasGo.SetActive(true);
            }
            else cap.Log("pause menu pages: not captured (no pause menu / camera)");

            SaveGame.ClearTestUnlocks();
            cap.Finish($"MENU PAGES (QF14): {shots} shots at {_rt.width}x{_rt.height}, monochrome max |R-G|,|G-B| over every pixel = {worstColour} (must be 0)");
        }
    }
}
