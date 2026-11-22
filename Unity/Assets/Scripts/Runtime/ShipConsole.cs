using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Seven
{
    /// <summary>
    /// Round 13 (item 2) — the ship's console on the saucer's main deck (geometry: Editor/FightDemoBuilderConsole.cs).
    /// 7 walks up, X / E "Use console" (A works too), steps in and rests her hands on the desk (SevenTableIK), the camera
    /// moves over her shoulder to the screens; B / Esc leaves. The menu on the screens (world-space uGUI, Unity's built-in
    /// LegacyRuntime font, chrome-and-glow grays): Log (Resources/Console/adventure_log.json), Field guide
    /// (Resources/Console/field_guide.json + Console/Portraits/&lt;key&gt;.png), Star map (on the navigation display; the
    /// only place to choose a destination), Settings (the shared <see cref="GameSettings"/> — the pause menu's rows) and a
    /// hidden Upgrades slot. Never opens during a story beat (only in free exploration; <see cref="StoryLock"/> for cutscenes).
    /// The pilot's chair in front of the navigation display is for the lead's cutscenes (VEN_C11 / VEN_C13):
    /// <see cref="PilotSeat"/>, <see cref="PilotLap"/>, <see cref="Display"/>, <see cref="ChooseOnStarMap"/>,
    /// <see cref="ComingSoon"/>.
    /// </summary>
    public class ShipConsole : MonoBehaviour
    {
        public static ShipConsole I { get; private set; }
        /// <summary>Mars and Europa appear on the star map once VEN_C12 ("Two locations") has sent them to the ship.</summary>
        public static bool MarsAvailable => VenusState.SceneDone("VEN_C12");
        public static bool EuropaAvailable => VenusState.SceneDone("VEN_C12");
        /// <summary>Cutscenes on the deck set this while they run: the console never opens during a story beat.</summary>
        public static bool StoryLock;

        [Header("Scene (built by FightDemoBuilderConsole)")]
        public SaucerDirector Director;
        public ChapterUI UI;
        [Tooltip("Where 7 stands to use the console (her feet); forward = toward the screens.")]
        public Transform StandSpot;
        [Tooltip("The pilot's chair root (swivel pedestal); forward = toward the navigation display.")]
        public Transform PilotChair;
        [Tooltip("Where her hips sit in the pilot's chair; forward = facing the navigation display.")]
        public Transform PilotSeat;
        [Tooltip("Where the cat lies on her lap in the pilot's chair.")]
        public Transform PilotLap;
        [Tooltip("Centre of the navigation display (star map); forward = out of the glass toward the viewer.")]
        public Transform Display;
        [Tooltip("Centre of the menu screen; forward = out of the glass toward the viewer.")]
        public Transform MenuScreen;
        public Transform HandLeft, HandRight;
        public Vector2 MenuSize = new Vector2(0.84f, 0.61f), DisplaySize = new Vector2(0.93f, 0.74f);
        public Light ScreenLight;
        public float PromptRange = 1.35f, MenuFov = 40f, MapFov = 46f, EaseSeconds = 0.8f;

        public enum Page { Main, Log, Guide, Map, Settings, Upgrades }
        public enum State { Off, Entering, Using, Leaving }
        public State Current { get; private set; } = State.Off;
        public Page CurrentPage { get; private set; } = Page.Main;
        /// <summary>The player is at the console (stepping in, using it or stepping back).</summary>
        public bool Open => Current != State.Off;
        /// <summary>Anything running: in use, a set-course / coming-soon card, or a <see cref="ChooseOnStarMap"/> choice.</summary>
        public bool Busy => Current != State.Off || _busy > 0 || _choosing;
        public bool NearConsole { get; private set; }
        public bool OwnsEscape => Current != State.Off || _choosing || Time.unscaledTime < _escUntil;
        /// <summary>Check runs: no live input (the run calls <see cref="Nav"/>).</summary>
        [System.NonSerialized] public bool ScriptInput;
        /// <summary>Check runs (Auto mode): the id <see cref="ChooseOnStarMap"/> picks by script ("mars" / "europa").</summary>
        [System.NonSerialized] public string ScriptPick;
        /// <summary>Check runs: the console does not drive the camera (a capture places it).</summary>
        [System.NonSerialized] public bool CameraOverride;
        public StarMap Map => _map;
        public bool Choosing => _choosing;      // Round 14: the player-style Europa run reads it (the star map is up for a choice)
        public int Selection => _sel;
        public string LastStarMapLog { get; private set; } = "";

        SaucerSeven Seven => Director != null ? Director.Seven : null;
        Camera Cam => Director != null && Director.Cam != null ? Director.Cam.GetComponent<Camera>() : null;
        static bool Live(SaucerDirector d) => DemoBoot.CaptureDir == null || DemoBoot.PlayerSim || (d != null && d.LiveInputInCapture);

        // ---- menu
        struct Item { public string Label; public Page Page; public bool Visible; public Item(string l, Page p, bool v) { Label = l; Page = p; Visible = v; } }
        /// <summary>The main menu. Upgrades has its slot here but stays hidden until it is built (Dan: "not built yet").</summary>
        static readonly Item[] MainItems =
        {
            new Item("Log", Page.Log, true), new Item("Field guide", Page.Guide, true), new Item("Star map", Page.Map, true),
            new Item("Settings", Page.Settings, true), new Item("Upgrades", Page.Upgrades, false),
        };
        public static List<string> MainLabels() { var l = new List<string>(); foreach (var it in MainItems) if (it.Visible) l.Add(it.Label); return l; }
        static List<Item> Visible() { var l = new List<Item>(); foreach (var it in MainItems) if (it.Visible) l.Add(it); return l; }

        // ---- data
        [System.Serializable] public class LogEntry { public string id, chapter, title, text, unlock; }
        [System.Serializable] class LogFile { public List<LogEntry> entries = new List<LogEntry>(); }
        [System.Serializable] public class GuideEntry { public string id, title, titleBefore, textBefore, text, nameUnlock, unlock, portrait; public GuideMore[] more; }
        /// <summary>QF13 (4): an entry grows with the story - a paragraph shown after the main text once its unlock is met.</summary>
        [System.Serializable] public class GuideMore { public string text, unlock; }
        [System.Serializable] class GuideFile { public List<GuideEntry> entries = new List<GuideEntry>(); }

        /// <summary>Unlock forms (the JSON files): "ch1>=N" (SaveGame.Furthest), "venus>=Stage" (VenusState.AtLeast),
        /// "scene:VEN_xxx" (VenusState.SceneDone), "never"; empty = always.</summary>
        public static bool Unlocked(string u)
        {
            if (string.IsNullOrEmpty(u)) return true;
            u = u.Trim();
            if (u == "never") return false;
            if (u.StartsWith("ch1>=")) return int.TryParse(u.Substring(5), out var n) && SaveGame.Furthest >= n;
            if (u.StartsWith("venus>=")) return System.Enum.TryParse<VenusState.Stage>(u.Substring(7), out var st) && VenusState.AtLeast(st);
            if (u.StartsWith("scene:")) return u.Substring(6).StartsWith("EUR_") ? EuropaState.SceneDone(u.Substring(6)) : u.Substring(6).StartsWith("MAR_") ? MarsState.SceneDone(u.Substring(6)) : u.Substring(6).StartsWith("AST_") ? AsteroidState.SceneDone(u.Substring(6)) : VenusState.SceneDone(u.Substring(6));
            if (u.StartsWith("mars>=")) return int.TryParse(u.Substring(6), out var mc) && MarsState.Reached && MarsState.Checkpoint >= mc;   // Round 15: MarsLayout.Cp*
            if (u == "biaveh") return WorldFlags.BiavehUnlocked;
            if (u == "asteroid") return WorldFlags.AsteroidFound;
            if (u.StartsWith("asteroid>=")) return int.TryParse(u.Substring(10), out var ac) && AsteroidState.Reached && AsteroidState.Checkpoint >= ac;   // Round 16: AsteroidLayout.Cp*
            if (u.StartsWith("biaveh>=")) return int.TryParse(u.Substring(8), out var bc) && BiavehState.Reached && BiavehState.Checkpoint >= bc;   // Round 17: BiavehLayout.Cp*
            if (u.StartsWith("europa>=")) return int.TryParse(u.Substring(8), out var ec) && EuropaState.Reached && EuropaState.Checkpoint >= ec;   // Round 14: EuropaLayout.Cp*
            return false;
        }

        public static List<LogEntry> LogEntries()
        {
            var ta = Resources.Load<TextAsset>("Console/adventure_log");
            var f = ta != null ? JsonUtility.FromJson<LogFile>(ta.text) : null;
            var l = new List<LogEntry>();
            if (f?.entries != null) foreach (var e in f.entries) if (Unlocked(e.unlock)) l.Add(e);
            return l;
        }

        public static List<GuideEntry> GuideEntries()
        {
            var ta = Resources.Load<TextAsset>("Console/field_guide");
            var f = ta != null ? JsonUtility.FromJson<GuideFile>(ta.text) : null;
            var l = new List<GuideEntry>();
            if (f?.entries != null) foreach (var e in f.entries) if (Unlocked(e.unlock)) l.Add(e);
            return l;
        }
        public static string GuideTitle(GuideEntry e) => !string.IsNullOrEmpty(e.nameUnlock) && !Unlocked(e.nameUnlock) && !string.IsNullOrEmpty(e.titleBefore) ? e.titleBefore : e.title;
        public static string GuideText(GuideEntry e)
        {
            bool before = !string.IsNullOrEmpty(e.nameUnlock) && !Unlocked(e.nameUnlock) && !string.IsNullOrEmpty(e.textBefore);
            var s = before ? e.textBefore : e.text;
            if (!before && e.more != null) foreach (var m in e.more) if (m != null && !string.IsNullOrEmpty(m.text) && Unlocked(m.unlock)) s += "\n\n" + m.text;
            return s;
        }

        // ---- star map rules
        /// <summary>Only the places she can reach: the Moon and Venus always; Mars and Europa once VEN_C12 has happened.</summary>
        public static List<string> Reachable()
        {
            var l = new List<string> { "venus", "moon" };
            if (MarsAvailable) l.Add("mars");
            if (EuropaAvailable) l.Add("europa");
            if (WorldFlags.BiavehUnlocked) l.Add("biaveh");          // Round 15: only after Mars + Europa (the combined records)
            if (WorldFlags.AsteroidFound) l.Add("asteroid");         // only after the travel event on the way to Biaveh
            return l;
        }
        /// <summary>Where the saucer is now: at Venus in the Venus aboard mode, otherwise at the Moon (Chapter 1).</summary>
        public static string Here()
        {
            if (SaucerLocation.System == StarMap.Bia) return SaucerLocation.Saved == "biaveh" ? "biaveh" : SaucerLocation.Saved == "asteroid" ? "asteroid" : "biaveh_space";   // QF18 (22)
            var ab = Chapter1Aboard.I;
            return ab != null && ab.Active && ab.MarsMode ? "mars" : ab != null && ab.Active && ab.EuropaMode ? "europa" : ab != null && ab.Active && ab.VenusMode ? "venus" : "moon";
        }
        public static string WorldName(string id) => id == "moon" ? "The Moon" : id == "venus" ? "Venus" : id == "mars" ? "Mars" : id == "europa" ? "Europa" : id == "biaveh" ? WorldFlags.ContactName : id == "asteroid" ? "The asteroid" : id;
        /// <summary>One line from the Log for a visited world (its latest unlocked entry, first sentence); null = not visited.</summary>
        public static Dictionary<string, string> VisitedNotes()
        {
            var d = new Dictionary<string, string>();
            foreach (var e in LogEntries())
            {
                string w = e.id != null && e.id.StartsWith("ch1_") ? "moon" : e.id != null && e.id.StartsWith("ven_") ? "venus" : e.id != null && e.id.StartsWith("eur_") ? "europa" : e.id != null && e.id.StartsWith("mar_") ? "mars" : null;
                if (w != null) d[w] = e.title + ": " + FirstSentence(e.text);
            }
            if (!d.ContainsKey("venus") && VenusState.AtLeast(VenusState.Stage.Landed)) d["venus"] = "Visited.";
            return d;
        }
        static string FirstSentence(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            int k = s.IndexOf(". ");
            return k > 0 ? s.Substring(0, k + 1) : s;
        }

        // ---- runtime state
        Font _font;
        GameObject _menuCanvasGo, _mapCanvasGo;
        RectTransform _menuRoot, _mapRoot, _pageRoot;
        CanvasGroup _menuGroup, _mapGroup;
        StarMap _map;
        Page _page = Page.Main;
        int _sel, _listTop, _mainSel;
        int _busy; bool _choosing;
        float _escUntil = -1f, _stickReady;
        SevenTableIK _ik;
        Vector3 _camFromPos; Quaternion _camFromRot; float _camEase = 1f, _fov0 = 50f, _fovFrom = 50f;
        bool _promptMine, _escHooked;
        List<LogEntry> _log = new List<LogEntry>();
        List<GuideEntry> _guide = new List<GuideEntry>();
        readonly Dictionary<string, Texture2D> _portraits = new Dictionary<string, Texture2D>();

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; }

        IEnumerator Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildCanvases();
            IdleScreens();
            yield return null;                   // Chapter1Aboard has started by now (Venus mode or not): where the ship is
            if (Current == State.Off && !_choosing) IdleScreens();
        }

        // ================================================================================== canvases
        void BuildCanvases()
        {
            if (MenuScreen == null || Display == null) return;
            _menuCanvasGo = MakeCanvas("ConsoleMenuCanvas", MenuScreen, MenuSize, out _menuRoot, out _menuGroup);
            _mapCanvasGo = MakeCanvas("ConsoleMapCanvas", Display, DisplaySize, out _mapRoot, out _mapGroup);
            // menu screen: dark glass, a soft glow in the middle (Kyle's light), header + footer rules
            Img(_menuRoot, null, Vector2.zero, _menuRoot.sizeDelta, new Color(0.02f, 0.02f, 0.02f, 0.95f));
            Img(_menuRoot, StarMap.SoftSprite, new Vector2(0f, 20f), _menuRoot.sizeDelta * 1.15f, new Color(1f, 1f, 1f, 0.07f));
            var pg = new GameObject("Page", typeof(RectTransform)); pg.transform.SetParent(_menuRoot, false);
            _pageRoot = (RectTransform)pg.transform; _pageRoot.sizeDelta = _menuRoot.sizeDelta;
            _map = new StarMap(_mapRoot, _font, _mapRoot.sizeDelta);
        }

        GameObject MakeCanvas(string name, Transform at, Vector2 sizeM, out RectTransform root, out CanvasGroup group)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 0;                                   // not the UI layer: the galley's subtitle camera takes layer 5 off the main camera
            go.transform.SetParent(at, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // readable from the marker's forward side (the viewer)
            go.transform.localScale = Vector3.one * 0.001f;                 // 1 canvas unit = 1 mm
            var c = go.AddComponent<Canvas>(); c.renderMode = RenderMode.WorldSpace;
            var sc = go.AddComponent<CanvasScaler>(); sc.dynamicPixelsPerUnit = 2.5f;
            group = go.AddComponent<CanvasGroup>(); group.interactable = false; group.blocksRaycasts = false;
            root = (RectTransform)go.transform; root.sizeDelta = sizeM * 1000f;
            return go;
        }

        Image Img(RectTransform parent, Sprite sp, Vector2 pos, Vector2 size, Color c, Vector2? pivot = null)
        {
            var go = new GameObject("Img", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.pivot = pivot ?? new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = size;
            var im = go.AddComponent<Image>(); im.sprite = sp; im.color = c; im.raycastTarget = false;
            return im;
        }

        Text Txt(RectTransform parent, string s, Vector2 pos, int size, Color c, TextAnchor anchor, Vector2 box, Vector2? pivot = null)
        {
            var go = new GameObject("Text", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.pivot = pivot ?? new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = box;
            var t = go.AddComponent<Text>(); t.font = _font; t.fontSize = size; t.text = s; t.color = c; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
            t.lineSpacing = 1.05f;
            return t;
        }

        void ClearPage() { if (_pageRoot == null) return; for (int i = _pageRoot.childCount - 1; i >= 0; i--) Destroy(_pageRoot.GetChild(i).gameObject); }

        /// <summary>Not in use: the menu screen shows the "7" emblem, the navigation display the star map, dimmed.</summary>
        void IdleScreens()
        {
            if (_pageRoot == null) return;
            ClearPage();
            var W = _menuRoot.sizeDelta.x; var H = _menuRoot.sizeDelta.y;
            Img(_pageRoot, StarMap.SoftSprite, new Vector2(0f, 40f), new Vector2(360f, 360f), new Color(1f, 1f, 1f, 0.16f));
            Txt(_pageRoot, "7", new Vector2(0f, 46f), 190, new Color(0.82f, 0.82f, 0.82f), TextAnchor.MiddleCenter, new Vector2(400f, 240f));
            Txt(_pageRoot, "SHIP'S CONSOLE", new Vector2(0f, -110f), 26, new Color(0.6f, 0.6f, 0.6f), TextAnchor.MiddleCenter, new Vector2(W, 36f));
            Txt(_pageRoot, "X / E   Use console", new Vector2(0f, -H * 0.5f + 40f), 20, new Color(0.45f, 0.45f, 0.45f), TextAnchor.MiddleCenter, new Vector2(W, 30f));
            if (_menuGroup != null) _menuGroup.alpha = 0.8f;
            ConfigureMap(null, null, null);
            if (_mapGroup != null) _mapGroup.alpha = 0.6f;
            _map?.HidePointer();
        }

        void ConfigureMap(ICollection<string> selectable, IDictionary<string, string> extraNotes, string select)
        {
            if (_map == null) return;
            var shown = Reachable();
            var notes = VisitedNotes();
            if (extraNotes != null) foreach (var kv in extraNotes) notes[kv.Key] = kv.Value;
            if (selectable != null) foreach (var id in selectable) if (!shown.Contains(id)) shown.Add(id);
            _map.Configure(shown, selectable ?? shown, notes, Here(), select);
            LastStarMapLog = $"star map: shown [{string.Join(", ", _map.ShownIds())}], selectable [{string.Join(", ", _map.SelectableIds())}], ship at {Here()}, selected {_map.Selected}";
        }

        // ================================================================================== update
        void Update()
        {
            HookEscape();
            bool vis = Director == null || Director.Reveal == null || Director.Reveal.Progress < 0.02f;      // the canvases don't dissolve: hide them for the reveal
            if (_menuCanvasGo != null && _menuCanvasGo.activeSelf != vis) _menuCanvasGo.SetActive(vis);
            if (_mapCanvasGo != null && _mapCanvasGo.activeSelf != vis) _mapCanvasGo.SetActive(vis);
            _map?.Tick(Time.unscaledDeltaTime);
            NearConsole = ComputeNear();
            if (Director == null || Seven == null) return;
            if (Current == State.Off)
            {
                if (NearConsole && !Busy && Live(Director) && !PauseMenu.IsOpen && Director.Input != null && Director.Input.Sit.WasPressedThisFrame() && !Seven.NearChair) Use();
            }
            else if (Current == State.Using && !ScriptInput && _busy == 0 && Live(Director) && !PauseMenu.IsOpen) ReadInput();
            if (Current == State.Using && _ik != null)
            {
                _ik.LeftPos = HandLeft != null ? HandLeft.position : _ik.LeftPos; _ik.RightPos = HandRight != null ? HandRight.position : _ik.RightPos;
                _ik.LookPos = _page == Page.Map ? Display.position : MenuScreen.position;
            }
        }

        void HookEscape()
        {
            if (_escHooked) return;
            _escHooked = true;
            var prev = PauseMenu.EscapeOwned;          // SaucerDirector's (the gallery) - keep it, add the console
            PauseMenu.EscapeOwned = () => (prev != null && prev()) || (I != null && I.OwnsEscape);
        }

        /// <summary>Free exploration only: no chapter scene / galley cutscene / set course / story lock running.</summary>
        public static bool StoryAllows
        {
            get
            {
                if (StoryLock) return false;
                var ab = Chapter1Aboard.I;
                if (ab != null && ab.Active) return ab.Explored && !ab.InGalleyScene && !ab.SettingCourse && (I == null || I.Director == null || !I.Director.ChapterMode);
                return true;
            }
        }

        bool ComputeNear()
        {
            var s = Seven;
            if (s == null || StandSpot == null) return false;
            if (s.Current != SaucerSeven.Mode.Free || s.Frozen || s.Cutscene || s.OnSofa) return false;
            if (Director.Gallery != null && Director.Gallery.Current != GalleryTV.State.Off) return false;
            if (Director.Reveal != null && (Director.Reveal.Progress > 0f || Director.Reveal.Target > 0f)) return false;
            if (!StoryAllows) return false;
            return Flat(s.transform.position - StandSpot.position).magnitude < PromptRange;
        }

        void LateUpdate()
        {
            if (Director == null) return;
            // the context prompt (X / E "Use console"); other writers (the galley seat's hint) set it every frame
            bool want = NearConsole && Current == State.Off && !Busy;
            if (want) { Director.ExtraPrompt = "Use console"; _promptMine = true; }
            else if (_promptMine) { if (Director.ExtraPrompt == "Use console") Director.ExtraPrompt = null; _promptMine = false; }

            var cam = Cam;
            if (cam == null || CameraOverride) return;
            if (Current == State.Entering || Current == State.Using)
            {
                _camEase = Mathf.Min(1f, _camEase + Time.deltaTime / Mathf.Max(0.05f, EaseSeconds));
                View(_page == Page.Map, out var pos, out var look);
                float e = Mathf.SmoothStep(0f, 1f, _camEase);
                cam.transform.position = Vector3.Lerp(_camFromPos, pos, e);
                cam.transform.rotation = Quaternion.Slerp(_camFromRot, Quaternion.LookRotation(look - pos), e);
                cam.fieldOfView = Mathf.Lerp(_fovFrom, _page == Page.Map ? MapFov : MenuFov, e);
            }
            else if (Current == State.Leaving) cam.fieldOfView = Mathf.MoveTowards(cam.fieldOfView, _fov0, Time.deltaTime * 20f);
        }

        /// <summary>Over her shoulder to the menu screen, or (star map) to the navigation display from her side.</summary>
        public void View(bool map, out Vector3 pos, out Vector3 look)
        {
            var f = Flat(StandSpot.forward).normalized; var r = new Vector3(f.z, 0f, -f.x);
            if (!map)
            {
                pos = StandSpot.position + Vector3.up * 1.98f - f * 0.42f + r * 0.58f;   // Round 13 check: higher and further right, her hair clear of the screen
                look = MenuScreen.position;
                return;
            }
            var n = Display.forward;
            float side = Vector3.Dot(StandSpot.position - Display.position, Display.right) >= 0f ? 1f : -1f;
            pos = Display.position + n * 1.0f + Vector3.up * 0.26f + Display.right * side * 0.22f;
            look = Display.position;
        }

        void Retarget()
        {
            var cam = Cam; if (cam == null) return;
            _camFromPos = cam.transform.position; _camFromRot = cam.transform.rotation; _fovFrom = cam.fieldOfView; _camEase = 0f;
        }

        // ================================================================================== use / leave
        /// <summary>X / E at the console: she steps in, rests her hands on it, the camera moves over her shoulder.</summary>
        public void Use()
        {
            if (Current != State.Off || Seven == null) return;
            // QF20 (1): the combined-records discovery can never be missed - due = she goes to the pilot's chair instead
            var ab = Chapter1Aboard.I;
            if (WorldFlags.CombinedDue && ab != null && ab.Active && ab.Explored && !ab.InGalleyScene)
            {
                CapLog("used while the combined-records discovery is due -> MAR_13C in the pilot's chair");
                _busy++; StartCoroutine(DueDiscovery(ab)); return;
            }
            if (WorldFlags.CattalusDue && ab != null && ab.Active && ab.Explored && !ab.InGalleyScene)
            {
                CapLog("used while the Cattalus lead is due -> AST_11B in the pilot's chair");
                _busy++; StartCoroutine(DueCattalus(ab)); return;
            }
            Current = State.Entering;
            _ik = Seven.GetComponent<SevenTableIK>();
            if (_ik == null) _ik = Seven.gameObject.AddComponent<SevenTableIK>();
            var cam = Cam;
            _fov0 = cam != null ? cam.fieldOfView : 50f;
            Director.Cam.Hold = true;
            Retarget();
            _page = Page.Main; _sel = 0; _mainSel = 0;
            AudioKit.Play("console_open", null, 0.7f);
            CapLog($"used: 7 at {Seven.transform.position}, stand spot {StandSpot.position}");
            StartCoroutine(StepIn());
        }

        IEnumerator DueDiscovery(Chapter1Aboard ab) { yield return ab.CombinedAtConsole(); _busy--; }
        IEnumerator DueCattalus(Chapter1Aboard ab) { yield return ab.CattalusAtConsole(); _busy--; }

        IEnumerator StepIn()
        {
            var s = Seven;
            s.Frozen = true; s.Cutscene = true;
            var p0 = s.transform.position; var r0 = s.transform.rotation;
            var p1 = new Vector3(StandSpot.position.x, p0.y, StandSpot.position.z);
            var r1 = Quaternion.LookRotation(Flat(StandSpot.forward));
            float dist = Flat(p1 - p0).magnitude;
            float dur = Mathf.Clamp(dist / 1.1f, 0.35f, 1.2f);
            if (dist > 0.12f) s.PlayClip("walk", 0.15f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / dur)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                s.transform.position = Vector3.Lerp(p0, p1, u);
                s.transform.rotation = Quaternion.Slerp(r0, r1, Mathf.Clamp01(t * 1.6f));
                yield return null;
            }
            s.transform.SetPositionAndRotation(p1, r1);
            s.PlayClip("idle", 0.25f);
            s.Cutscene = false;                      // Frozen keeps her standing (idle) while she uses it
            // hands resting on the desk (left / right by her side), eyes on the screens
            if (HandLeft != null && HandRight != null && Vector3.Dot(HandLeft.position - p1, s.transform.right) > Vector3.Dot(HandRight.position - p1, s.transform.right))
            { var tmp = HandLeft; HandLeft = HandRight; HandRight = tmp; }
            if (_ik != null)
            {
                if (HandLeft != null) _ik.LeftPos = HandLeft.position;
                if (HandRight != null) _ik.RightPos = HandRight.position;
                _ik.LeftW = 0.9f; _ik.RightW = 0.9f;
                _ik.LookPos = MenuScreen.position; _ik.LookW = 0.7f;
            }
            if (_menuGroup != null) _menuGroup.alpha = 1f;
            if (_mapGroup != null) _mapGroup.alpha = 0.75f;
            ShowPage(Page.Main, 0);
            while (_camEase < 1f) yield return null;
            Current = State.Using;
        }

        /// <summary>B / Esc on the main menu: she steps back, the camera returns behind her.</summary>
        public void Leave()
        {
            if (Current != State.Using) return;
            Current = State.Leaving;
            _escUntil = Time.unscaledTime + 0.4f;
            AudioKit.Play("console_select", null, 0.45f);
            if (_ik != null) { _ik.LeftW = 0f; _ik.RightW = 0f; _ik.LookW = 0f; }
            var s = Seven;
            var cam = Director.Cam;
            var d = MenuScreen.position - s.transform.position; d.y = 0f;
            cam.Yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            cam.Pitch = cam.WalkPitch;
            cam.BlendFrom(cam.transform.position, cam.transform.rotation, EaseSeconds);
            cam.Hold = false;
            IdleScreens();
            CapLog("left the console");
            StartCoroutine(StepBack());
        }

        IEnumerator StepBack()
        {
            for (float t = 0f; t < EaseSeconds; t += Time.deltaTime) yield return null;
            var cam = Cam; if (cam != null) cam.fieldOfView = _fov0;
            if (Seven != null) Seven.Frozen = false;
            Current = State.Off;
            _escUntil = Time.unscaledTime + 0.3f;
        }

        /// <summary>Travel takes over: the console closes at once (her hands come off, the travel moves the camera).</summary>
        void ReleaseForTravel()
        {
            if (_ik != null) { _ik.LeftW = 0f; _ik.RightW = 0f; _ik.LookW = 0f; }
            Current = State.Off;
            _escUntil = Time.unscaledTime + 0.4f;
            IdleScreens();
        }

        // ================================================================================== pages
        void ShowPage(Page p, int sel)
        {
            bool mapChange = (p == Page.Map) != (_page == Page.Map);
            _page = p; CurrentPage = p; _sel = sel; _listTop = 0;
            if (p == Page.Log) _log = LogEntries();
            if (p == Page.Guide) _guide = GuideEntries();
            if (p == Page.Map)
            {
                ConfigureMap(Reachable(), null, Here() == "moon" ? "venus" : "moon");
                if (_mapGroup != null) _mapGroup.alpha = 1f;
                CapLog(LastStarMapLog);
            }
            else if (_mapGroup != null && Current != State.Off) { _mapGroup.alpha = 0.75f; _map?.HidePointer(); }
            if (mapChange) Retarget();
            Redraw();
        }

        void Redraw()
        {
            if (_pageRoot == null) return;
            ClearPage();
            float W = _menuRoot.sizeDelta.x, H = _menuRoot.sizeDelta.y;
            var ink = new Color(0.92f, 0.92f, 0.92f); var dim = new Color(0.6f, 0.6f, 0.6f);
            string title = _page == Page.Main ? "" : _page == Page.Log ? "LOG" : _page == Page.Guide ? "FIELD GUIDE" : _page == Page.Map ? "STAR MAP" : _page == Page.Settings ? "SETTINGS" : "UPGRADES";
            Txt(_pageRoot, "7  ·  SHIP'S CONSOLE", new Vector2(-W * 0.5f + 24f, H * 0.5f - 28f), 20, dim, TextAnchor.MiddleLeft, new Vector2(W * 0.5f, 30f), new Vector2(0f, 0.5f));
            Txt(_pageRoot, title, new Vector2(W * 0.5f - 24f, H * 0.5f - 28f), 22, ink, TextAnchor.MiddleRight, new Vector2(W * 0.5f, 30f), new Vector2(1f, 0.5f));
            Img(_pageRoot, null, new Vector2(0f, H * 0.5f - 50f), new Vector2(W - 40f, 2f), new Color(0.75f, 0.75f, 0.75f, 0.5f));
            Img(_pageRoot, null, new Vector2(0f, -H * 0.5f + 44f), new Vector2(W - 40f, 2f), new Color(0.75f, 0.75f, 0.75f, 0.35f));
            string hint = _page == Page.Main ? "↑ ↓  choose    ·    A  open    ·    B  leave"
                        : _page == Page.Settings ? "↑ ↓  choose    ·    ← →  adjust    ·    A  switch    ·    B  back"
                        : _page == Page.Map ? "← →  choose a world    ·    A  set course    ·    B  back"
                        : "↑ ↓  choose    ·    B  back";
            Txt(_pageRoot, hint, new Vector2(0f, -H * 0.5f + 22f), 18, new Color(0.58f, 0.58f, 0.58f), TextAnchor.MiddleCenter, new Vector2(W, 26f));
            switch (_page)
            {
                case Page.Main: DrawMain(W, H); break;
                case Page.Log: DrawLog(W, H); break;
                case Page.Guide: DrawGuide(W, H); break;
                case Page.Map: DrawMapPage(W, H); break;
                case Page.Settings: DrawSettings(W, H); break;
                default: Txt(_pageRoot, "Not available yet.", Vector2.zero, 26, ink, TextAnchor.MiddleCenter, new Vector2(W, 40f)); break;
            }
        }

        void Bar(Vector2 pos, Vector2 size, string label, int font, bool on)
        {
            if (on) Img(_pageRoot, StarMap.SoftSprite, pos, size * new Vector2(1.25f, 2.2f), new Color(1f, 1f, 1f, 0.16f));     // glow behind the highlight
            var bar = Img(_pageRoot, null, pos, size, on ? new Color(0.9f, 0.9f, 0.9f, 1f) : new Color(1f, 1f, 1f, 0.06f));
            var t = Txt((RectTransform)bar.transform, label, Vector2.zero, font, on ? new Color(0.05f, 0.05f, 0.05f) : new Color(0.86f, 0.86f, 0.86f), TextAnchor.MiddleCenter, size);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        void DrawMain(float W, float H)
        {
            var items = Visible();
            float top = 112f;
            for (int i = 0; i < items.Count; i++) Bar(new Vector2(0f, top - i * 74f), new Vector2(420f, 58f), items[i].Label.ToUpperInvariant(), 32, i == _sel);
            string sub = items[_sel].Page == Page.Log ? $"{LogEntries().Count} entries" : items[_sel].Page == Page.Guide ? $"{GuideEntries().Count} entries"
                       : items[_sel].Page == Page.Map ? "Choose the next destination" : items[_sel].Page == Page.Settings ? "Sound, subtitles and hints" : "";
            if (items[_sel].Page == Page.Settings) sub = "Music, sound effects, voice, button hints";
            Txt(_pageRoot, sub, new Vector2(0f, top - items.Count * 74f + 6f), 20, new Color(0.62f, 0.62f, 0.62f), TextAnchor.MiddleCenter, new Vector2(W, 28f));
        }

        const int ListRows = 9;
        void ListColumn(List<string> labels, float W, float H)
        {
            if (_sel < _listTop) _listTop = _sel;
            if (_sel >= _listTop + ListRows) _listTop = _sel - ListRows + 1;
            float x0 = -W * 0.5f + 24f, y0 = H * 0.5f - 84f;
            for (int k = 0; k < ListRows && _listTop + k < labels.Count; k++)
            {
                int i = _listTop + k; bool on = i == _sel;
                var pos = new Vector2(x0, y0 - k * 44f);
                if (on) Img(_pageRoot, null, pos + new Vector2(0f, 0f), new Vector2(300f, 38f), new Color(0.9f, 0.9f, 0.9f, 1f), new Vector2(0f, 0.5f));
                var t = Txt(_pageRoot, labels[i], pos + new Vector2(12f, 0f), 22, on ? new Color(0.05f, 0.05f, 0.05f) : new Color(0.84f, 0.84f, 0.84f), TextAnchor.MiddleLeft, new Vector2(282f, 36f), new Vector2(0f, 0.5f));
                t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; t.resizeTextForBestFit = true; t.resizeTextMinSize = 14; t.resizeTextMaxSize = 22;
            }
            if (_listTop > 0) Txt(_pageRoot, "▲", new Vector2(x0 + 150f, y0 + 26f), 14, new Color(0.6f, 0.6f, 0.6f), TextAnchor.MiddleCenter, new Vector2(40f, 18f));
            if (_listTop + ListRows < labels.Count) Txt(_pageRoot, "▼", new Vector2(x0 + 150f, y0 - ListRows * 44f + 22f), 14, new Color(0.6f, 0.6f, 0.6f), TextAnchor.MiddleCenter, new Vector2(40f, 18f));
            Img(_pageRoot, null, new Vector2(-W * 0.5f + 338f, -6f), new Vector2(2f, H - 120f), new Color(0.75f, 0.75f, 0.75f, 0.3f));
        }

        void DrawLog(float W, float H)
        {
            if (_log.Count == 0) { Txt(_pageRoot, "Nothing logged yet.", Vector2.zero, 26, Color.white, TextAnchor.MiddleCenter, new Vector2(W, 40f)); return; }
            var labels = new List<string>(); foreach (var e in _log) labels.Add(e.title);
            ListColumn(labels, W, H);
            var cur = _log[Mathf.Clamp(_sel, 0, _log.Count - 1)];
            float x = -W * 0.5f + 356f, w = W * 0.5f - 24f - x, y = H * 0.5f - 78f;
            Txt(_pageRoot, cur.chapter ?? "", new Vector2(x, y), 18, new Color(0.6f, 0.6f, 0.6f), TextAnchor.UpperLeft, new Vector2(w, 24f), new Vector2(0f, 1f));
            Txt(_pageRoot, cur.title ?? "", new Vector2(x, y - 28f), 30, Color.white, TextAnchor.UpperLeft, new Vector2(w, 40f), new Vector2(0f, 1f));
            Txt(_pageRoot, cur.text ?? "", new Vector2(x, y - 78f), 23, new Color(0.86f, 0.86f, 0.86f), TextAnchor.UpperLeft, new Vector2(w, H - 200f), new Vector2(0f, 1f));
        }

        void DrawGuide(float W, float H)
        {
            if (_guide.Count == 0) { Txt(_pageRoot, "No one met yet.", Vector2.zero, 26, Color.white, TextAnchor.MiddleCenter, new Vector2(W, 40f)); return; }
            var labels = new List<string>(); foreach (var e in _guide) labels.Add(GuideTitle(e));
            ListColumn(labels, W, H);
            var cur = _guide[Mathf.Clamp(_sel, 0, _guide.Count - 1)];
            float x = -W * 0.5f + 356f, w = W * 0.5f - 24f - x, y = H * 0.5f - 74f;
            // portrait (a clean grayscale render, Tools/Blender/render_portraits.py) in a chrome-gray frame
            var tex = Portrait(cur.portrait);
            Img(_pageRoot, null, new Vector2(x - 3f, y + 3f), new Vector2(166f, 206f), new Color(0.7f, 0.7f, 0.7f, 1f), new Vector2(0f, 1f));
            var go = new GameObject("Portrait", typeof(RectTransform)); go.transform.SetParent(_pageRoot, false);
            var prt = (RectTransform)go.transform; prt.pivot = new Vector2(0f, 1f); prt.anchoredPosition = new Vector2(x, y); prt.sizeDelta = new Vector2(160f, 200f);
            var raw = go.AddComponent<RawImage>(); raw.raycastTarget = false;
            if (tex != null) raw.texture = tex; else raw.color = new Color(0.12f, 0.12f, 0.12f);
            if (tex == null) Txt(prt, "no picture", new Vector2(80f, -100f), 16, new Color(0.5f, 0.5f, 0.5f), TextAnchor.MiddleCenter, new Vector2(150f, 24f));
            Txt(_pageRoot, GuideTitle(cur), new Vector2(x + 180f, y), 28, Color.white, TextAnchor.UpperLeft, new Vector2(w - 180f, 100f), new Vector2(0f, 1f));
            Txt(_pageRoot, GuideText(cur) ?? "", new Vector2(x, y - 216f), 20, new Color(0.86f, 0.86f, 0.86f), TextAnchor.UpperLeft, new Vector2(w, H - 330f), new Vector2(0f, 1f));
        }

        Texture2D Portrait(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_portraits.TryGetValue(key, out var t)) return t;
            t = Resources.Load<Texture2D>("Console/Portraits/" + key);       // missing = a plain card (no error)
            _portraits[key] = t;
            return t;
        }

        void DrawMapPage(float W, float H)
        {
            Txt(_pageRoot, "The star map is on the navigation display.", new Vector2(0f, 120f), 24, Color.white, TextAnchor.MiddleCenter, new Vector2(W - 60f, 40f));
            var b = _map != null ? _map.Find(_map.Selected) : null;
            Txt(_pageRoot, b != null ? b.Name : "", new Vector2(0f, 40f), 40, Color.white, TextAnchor.MiddleCenter, new Vector2(W - 60f, 56f));
            Txt(_pageRoot, b != null ? (string.IsNullOrEmpty(b.Note) ? "Not visited yet." : b.Note) : "", new Vector2(0f, -50f), 22, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperCenter, new Vector2(W - 100f, 120f));
        }

        void DrawSettings(float W, float H)
        {
            for (int i = 0; i <= GameSettings.Count; i++)
            {
                string label = i < GameSettings.Count ? GameSettings.Label(i) : "Back";
                Bar(new Vector2(0f, 128f - i * 66f), new Vector2(600f, 52f), label, 28, i == _sel);
            }
        }

        int ItemCount()
        {
            switch (_page)
            {
                case Page.Main: return Visible().Count;
                case Page.Log: return Mathf.Max(1, _log.Count);
                case Page.Guide: return Mathf.Max(1, _guide.Count);
                case Page.Settings: return GameSettings.Count + 1;
                default: return 1;
            }
        }

        // ================================================================================== input
        void ReadInput()
        {
            int d = 0, lr = 0; bool ok = false, back = false;
            var kb = Keyboard.current; var gp = Gamepad.current;
            if (kb != null)
            {
                if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) d = -1;
                if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) d = 1;
                if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) lr = -1;
                if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) lr = 1;
                ok |= kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame;
                back |= kb.escapeKey.wasPressedThisFrame;
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
                ok |= gp.buttonSouth.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame;
                back |= gp.buttonEast.wasPressedThisFrame;
            }
            // the star map's pointer: the mouse moves an in-world pointer on the display, a click sets course
            var ms = Mouse.current;
            if (_page == Page.Map && ms != null && _map != null)
            {
                var delta = ms.delta.ReadValue();
                if (delta.sqrMagnitude > 1f)
                {
                    var hit = _map.MovePointer(delta * 1.1f);
                    if (hit != null && hit != _map.Selected) { _map.Select(hit); AudioKit.Play("console_move", null, 0.5f); Redraw(); }
                }
                if (ms.leftButton.wasPressedThisFrame && _map.Selected != null) ok = true;
            }
            Nav(d, lr, ok, back);
        }

        /// <summary>One step of menu input (keys / pad / check runs): d = up (-1) / down (+1), lr = left / right.</summary>
        public void Nav(int d, int lr, bool ok, bool back)
        {
            if (Current != State.Using || _busy > 0) return;
            if (_page == Page.Map)
            {
                if (d != 0 || lr != 0) { _map.Step(lr != 0 ? lr : d); AudioKit.Play("console_move", null, 0.5f); Redraw(); }
                if (back) { if (_map.Confirming) { _map.SetConfirm(false); AudioKit.Play("console_move", null, 0.5f); } else ShowPage(Page.Main, _mainSel); return; }
                if (ok && _map.Selected != null && _map.IsGateway(_map.Selected)) { _map.SwitchSystem(); CapLog($"star map (QF18 22): through the gateway to the {(_map.View == StarMap.Sol ? "Veyra system" : "solar system")}"); return; }   // QF18 (22)
                if (ok && _map.Selected != null)
                {
                    AudioKit.Play("console_select", null, 0.7f);
                    if (!_map.Confirming) _map.SetConfirm(true);
                    else StartCoroutine(SetCourseTo(_map.Selected));
                }
                return;
            }
            int n = ItemCount();
            if (d != 0) { _sel = (_sel + d + n) % n; AudioKit.Play("console_move", null, 0.5f); Redraw(); }
            if (_page == Page.Settings && lr != 0 && _sel < GameSettings.Count) { GameSettings.Adjust(_sel, lr); AudioKit.Play("console_move", null, 0.5f); Redraw(); }
            if (back)
            {
                if (_page == Page.Main) Leave();
                else { AudioKit.Play("console_move", null, 0.5f); ShowPage(Page.Main, _mainSel); }
                return;
            }
            if (!ok) return;
            AudioKit.Play("console_select", null, 0.7f);
            switch (_page)
            {
                case Page.Main: { _mainSel = _sel; ShowPage(Visible()[_sel].Page, 0); break; }
                case Page.Settings:
                    if (_sel < GameSettings.Count) { GameSettings.Activate(_sel); Redraw(); }
                    else ShowPage(Page.Main, _mainSel);
                    break;
            }
        }

        /// <summary>Check runs: open a page straight from the main menu (same path as choosing it).</summary>
        public void OpenPage(Page p)
        {
            if (Current != State.Using) return;
            var items = Visible();
            for (int i = 0; i < items.Count; i++) if (items[i].Page == p) { _sel = i; Redraw(); Nav(0, 0, true, false); return; }
            ShowPage(p, 0);          // the hidden Upgrades slot
        }

        /// <summary>Check runs: move the highlight to row i of the current page.</summary>
        public void SelectRow(int i) { if (_page == Page.Map) return; _sel = Mathf.Clamp(i, 0, ItemCount() - 1); Redraw(); }

        // ================================================================================== travel
        /// <summary>"Set course" on the star map: the Moon / Venus = the existing travel (Chapter1Aboard.TravelFromConsole);
        /// Mars / Europa = the "Chapter 3 — … (coming soon)" card, then back to the map.</summary>
        /// <summary>QF23 (24) check run: the star map's own travel path for a world (as A on it).</summary>
        public IEnumerator TravelForCheck(string id) { yield return SetCourseTo(id); }
        IEnumerator SetCourseTo(string id)
        {
            _busy++;
            CapLog($"star map: set course for {WorldName(id)} ({id})");
            if (id == "europa")
            {
                // Round 14: Europa is playable. Not started / in progress = the chapter (Continue where it left off);
                // done = a short card (the record is aboard) and back to the map
                if (EuropaState.Complete) { yield return Revisit("europa", "Europa"); _busy--; yield break; }
                ReleaseForTravel();
                yield return TravelPassage.Run("europa");                    // QF16 (2): the sacred-geometry passage
                if (UI != null) yield return UI.Fade(1f, 0.8f);
                if (EuropaState.HasProgress) EuropaState.ContinueOwn(); else EuropaState.StartFresh();
                _busy--;
                yield break;
            }
            if (id == "mars")
            {
                // Round 15: Mars is playable (as Europa: done = a short card, else the chapter where it left off)
                if (MarsState.OwnComplete) { yield return Revisit("mars", "Mars"); _busy--; yield break; }
                ReleaseForTravel();
                yield return TravelPassage.Run("mars");                      // QF16 (2): the sacred-geometry passage
                if (UI != null) yield return UI.Fade(1f, 0.8f);
                if (MarsState.HasProgress) MarsState.ContinueOwn(); else MarsState.StartFresh();
                _busy--;
                yield break;
            }
            if (id == "asteroid" && WorldFlags.AsteroidFound)
            {
                // Round 16: the asteroid is playable (as Mars: done = a short card, else the chapter where it left off)
                if (AsteroidState.OwnComplete) { yield return Revisit("asteroid", "The asteroid"); _busy--; yield break; }
                ReleaseForTravel();
                yield return TravelPassage.Run("asteroid");
                if (UI != null) yield return UI.Fade(1f, 0.8f);
                if (AsteroidState.HasProgress) AsteroidState.ContinueOwn(); else AsteroidState.StartFresh();
                _busy--;
                yield break;
            }
            if (id == "biaveh" || id == "asteroid")
            {
                // the asteroid is found on the first journey toward Biaveh (TRV_AST); Biaveh's chapter does not exist yet
                var ab0 = Chapter1Aboard.I;
                // QF23 (24): the first journey toward the mothership (TRV_AST) only while it is still ahead - never for a finished mothership
                if (id == "biaveh" && !WorldFlags.AsteroidFound && !BiavehState.OwnComplete && ab0 != null && ab0.Active) { ReleaseForTravel(); yield return ab0.TravelToBiaveh(); _busy--; yield break; }
                if (id == "biaveh")
                {
                    // Round 17: the Biavian mothership at Veyra is playable (done = a short card, else the chapter where it left off)
                    if (BiavehState.OwnComplete) { yield return Revisit("biaveh", "The Biavian Mothership"); _busy--; yield break; }
                    ReleaseForTravel();
                    yield return TravelPassage.Run("biaveh");
                    if (UI != null) yield return UI.Fade(1f, 0.8f);
                    if (BiavehState.HasProgress) BiavehState.ContinueOwn(); else BiavehState.StartFresh();
                    _busy--;
                    yield break;
                }
                yield return ComingSoonCard($"{WorldName(id)} (coming soon)");
                _map.SetConfirm(false); Redraw();
                _busy--;
                yield break;
            }
            var ab = Chapter1Aboard.I;
            if (ab == null || !ab.Active)
            {
                UI?.Hint("Set course from the chapters (main menu)", 3f);
                _map.SetConfirm(false);
                _busy--;
                yield break;
            }
            ReleaseForTravel();
            yield return ab.TravelFromConsole(id);
            var cam = Cam; if (cam != null && this != null) cam.fieldOfView = _fov0;
            _busy--;
        }

        /// <summary>QF23 (24): a FINISHED world revisited - the short arrival only: the sacred-geometry passage, the world in the
        /// window, a "visited" card, then free exploration aboard. Never its story (no arrival talk, no first-visit or
        /// mid-chapter scenes); replaying a chapter is only through the main menu's chapter list / Scene Select.</summary>
        public IEnumerator Revisit(string id, string name)
        {
            var ab = Chapter1Aboard.I;
            CapLog($"REVISIT (QF23 24): {name} is finished - the passage, the world in the window, a visited card, free exploration (no story scenes)");
            if (ab == null || !ab.Active) { yield return ComingSoonCard($"{name} — visited (its record is aboard)"); _map.SetConfirm(false); Redraw(); yield break; }
            ReleaseForTravel();
            yield return ab.RevisitWorld(id, $"{name} — visited (its record is aboard)");
            var cam = Cam; if (cam != null && this != null) cam.fieldOfView = _fov0;
        }

        /// <summary>The "Chapter 3 — &lt;world&gt; (coming soon)" card (fade out, the card, fade back in).</summary>
        public IEnumerator ComingSoonCard(string card)
        {
            _busy++;
            CapLog($"card '{card}'");
            if (UI != null) { yield return UI.Fade(1f, 0.8f); yield return UI.Title(card, 0.6f, 1.8f, 0.6f); yield return UI.Fade(0f, 0.8f); }
            _busy--;
        }
        public IEnumerator ComingSoon(string worldLabel)
        {
            _busy++;
            AudioKit.Play("course_set", null, 0.6f);
            string card = $"Chapter 3 — {worldLabel} (coming soon)";
            CapLog($"card '{card}'");
            if (UI != null)
            {
                yield return UI.Fade(1f, 0.8f);
                yield return UI.Title(card, 0.6f, 1.8f, 0.6f);
                yield return UI.Fade(0f, 0.8f);
            }
            else for (float t = 0f; t < 3f; t += Time.deltaTime) yield return null;
            _busy--;
        }

        /// <summary>For the lead's cutscenes (VEN_C13): the star map on the navigation display with only these ids selectable,
        /// each with its label as the info line; the player picks one (stick / d-pad / keys / mouse, A twice = confirm), or
        /// <see cref="ScriptPick"/> picks it in check runs. The map stays shown (choice highlighted) until
        /// <see cref="HideMap"/> or the console is next used. The console's camera is not touched.</summary>
        public IEnumerator ChooseOnStarMap(string[] ids, string[] labels, System.Action<string> picked)
        {
            if (ids == null || ids.Length == 0) { picked?.Invoke(null); yield break; }
            _choosing = true;
            var notes = new Dictionary<string, string>();
            for (int i = 0; i < ids.Length; i++) if (labels != null && i < labels.Length && !string.IsNullOrEmpty(labels[i])) notes[ids[i]] = labels[i];
            if (_map != null)
            {
                _map.Heading = "Choose the next world";
                ConfigureMap(new List<string>(ids), notes, ids[0]);
                CapLog("choose: " + LastStarMapLog);
            }
            if (_mapGroup != null) _mapGroup.alpha = 1f;
            string choice = null;
            while (choice == null)
            {
                if (!string.IsNullOrEmpty(ScriptPick) || (DemoBoot.CaptureDir != null && !DemoBoot.PlayerSim && !Live(Director)))
                {
                    string want = System.Array.IndexOf(ids, ScriptPick) >= 0 ? ScriptPick : ids[0];
                    for (float t = 0f; t < 1.0f; t += Time.deltaTime) yield return null;
                    _map?.Select(want); AudioKit.Play("console_move", null, 0.5f);
                    for (float t = 0f; t < 0.8f; t += Time.deltaTime) yield return null;
                    _map?.SetConfirm(true); AudioKit.Play("console_select", null, 0.7f);
                    for (float t = 0f; t < 0.6f; t += Time.deltaTime) yield return null;
                    choice = want; ScriptPick = null;
                    break;
                }
                if (!PauseMenu.IsOpen && _map != null) choice = ChooseInput();
                yield return null;
            }
            AudioKit.Play("console_select", null, 0.7f);
            _map?.SetConfirm(false);
            if (_map != null) _map.Heading = null;
            _map?.Select(choice);
            _map?.HidePointer();
            _choosing = false;
            CapLog($"choose: picked {choice}");
            picked?.Invoke(choice);
        }

        string ChooseInput()
        {
            int step = 0; bool ok = false, back = false;
            var kb = Keyboard.current; var gp = Gamepad.current;
            if (kb != null)
            {
                if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) step = -1;
                if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) step = 1;
                ok |= kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame;
                back |= kb.escapeKey.wasPressedThisFrame;
            }
            // QF15: every connected pad, not only Gamepad.current (a second pad - the check runs' menu pilot - could hold
            // "current" and the star map never saw the player's d-pad)
            foreach (var g in Gamepad.all)
            {
                if (g == gp) continue;
                if (g.dpad.left.wasPressedThisFrame || g.dpad.up.wasPressedThisFrame) step = -1;
                if (g.dpad.right.wasPressedThisFrame || g.dpad.down.wasPressedThisFrame) step = 1;
                ok |= g.buttonSouth.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame;
                back |= g.buttonEast.wasPressedThisFrame;
            }
            if (gp != null)
            {
                if (gp.dpad.left.wasPressedThisFrame || gp.dpad.up.wasPressedThisFrame) step = -1;
                if (gp.dpad.right.wasPressedThisFrame || gp.dpad.down.wasPressedThisFrame) step = 1;
                var st = gp.leftStick.ReadValue();
                if (st.magnitude < 0.3f) _stickReady = 0f;
                else if (Time.unscaledTime > _stickReady) { step = (Mathf.Abs(st.x) > Mathf.Abs(st.y) ? st.x : -st.y) > 0 ? 1 : -1; _stickReady = Time.unscaledTime + 0.3f; }
                ok |= gp.buttonSouth.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame;
                back |= gp.buttonEast.wasPressedThisFrame;
            }
            var ms = Mouse.current;
            if (ms != null)
            {
                var delta = ms.delta.ReadValue();
                if (delta.sqrMagnitude > 1f) { var hit = _map.MovePointer(delta * 1.1f); if (hit != null && hit != _map.Selected) { _map.Select(hit); AudioKit.Play("console_move", null, 0.5f); } }
                if (ms.leftButton.wasPressedThisFrame && _map.Selected != null) ok = true;
            }
            if (step != 0) { _map.Step(step); AudioKit.Play("console_move", null, 0.5f); }
            if (back && _map.Confirming) { _map.SetConfirm(false); return null; }
            if (ok && _map.Selected != null)
            {
                if (!_map.Confirming) { _map.SetConfirm(true); AudioKit.Play("console_select", null, 0.7f); return null; }
                return _map.Selected;
            }
            return null;
        }

        /// <summary>Show the star map on the navigation display for a cutscene (no choice), optionally highlighting one world.</summary>
        public void ShowMap(string highlight = null, string heading = null)
        {
            if (_map == null) return;
            _map.Heading = heading;
            ConfigureMap(null, null, highlight);
            if (_mapGroup != null) _mapGroup.alpha = 1f;
        }
        /// <summary>Back to the dimmed idle map.</summary>
        public void HideMap() { if (_map != null) { _map.Heading = null; ConfigureMap(null, null, null); } if (_mapGroup != null) _mapGroup.alpha = 0.6f; }

        /// <summary>Check runs (Auto): walk 7 to the console like a person, use it, open the star map, and either set course
        /// for <paramref name="target"/> (the travel runs) or (null) look and leave. Captures the console + star map.</summary>
        public IEnumerator ScriptedSetCourse(string target, string shotPrefix = "x02")
        {
            bool si = ScriptInput; ScriptInput = true;
            yield return WalkTo(ApproachPos, 12f);
            yield return WalkTo(StandSpot.position, 6f);
            CapLog($"scripted: at the console, {Flat(Seven.transform.position - StandSpot.position).magnitude:0.00} m from the stand spot; prompt '{Director.ExtraPrompt}', near {NearConsole}");
            if (Current == State.Off) Use();
            while (Current == State.Entering) yield return null;
            yield return Shot(shotPrefix + "c_console_menu");
            OpenPage(Page.Map);
            for (float t = 0f; t < EaseSeconds + 0.4f; t += Time.deltaTime) yield return null;
            if (target != null && _map != null) { _map.Select(target); Redraw(); }
            yield return Shot(shotPrefix + "d_console_star_map");
            if (target == null || _map == null || _map.Find(target) == null || !_map.Find(target).Selectable)
            {
                CapLog($"scripted: star map shown, no course set ({target ?? "look only"})");
                Nav(0, 0, false, true); Nav(0, 0, false, true);          // back to the menu, leave
                while (Current != State.Off) yield return null;
                ScriptInput = si;
                yield break;
            }
            Nav(0, 0, true, false);
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) yield return null;
            Nav(0, 0, true, false);                                       // confirm -> SetCourseTo (the travel)
            yield return null;
            while (_busy > 0) yield return null;
            ScriptInput = si;
        }

        /// <summary>A point in front of the console (the way in), clear of the desk.</summary>
        public Vector3 ApproachPos => StandSpot.position - Flat(StandSpot.forward).normalized * 0.95f;

        /// <summary>Check runs: walk 7 like a player would (camera-relative stick), giving up after the timeout.</summary>
        public IEnumerator WalkTo(Vector3 p, float timeout = 10f)
        {
            var s = Seven; if (s == null) yield break;
            bool sd = s.ScriptDriven; s.ScriptDriven = true;
            float t0 = Time.time;
            while (Flat(p - s.transform.position).magnitude > 0.25f && Time.time - t0 < timeout)
            {
                var d = Flat(p - s.transform.position).normalized;
                var f = Flat(Director.Cam.transform.forward).normalized; var r = new Vector3(f.z, 0f, -f.x);
                s.ScriptMove = new Vector2(Vector3.Dot(d, r), Vector3.Dot(d, f));
                yield return null;
            }
            s.ScriptMove = Vector2.zero; s.ScriptDriven = sd;
            if (Flat(p - s.transform.position).magnitude > 0.25f) CapLog($"walk: did not reach {p} in {timeout:0} s (at {s.transform.position})");
        }

        IEnumerator Shot(string name) { if (ChapterCapture.I != null) yield return ChapterCapture.I.Shot(name); }
        static void CapLog(string s) { ChapterCapture.I?.Log("console: " + s); }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        /// <summary>For check-run logs: the current page and what it shows.</summary>
        public string Describe()
        {
            switch (_page)
            {
                case Page.Log: return $"Log: {_log.Count} entries [{string.Join(" | ", _log.ConvertAll(e => e.title))}], selected '{(_log.Count > 0 ? _log[Mathf.Clamp(_sel, 0, _log.Count - 1)].title : "-")}'";
                case Page.Guide: return $"Field guide: {_guide.Count} entries [{string.Join(" | ", _guide.ConvertAll(GuideTitle))}], selected '{(_guide.Count > 0 ? GuideTitle(_guide[Mathf.Clamp(_sel, 0, _guide.Count - 1)]) : "-")}' (portrait {(_guide.Count > 0 && Portrait(_guide[Mathf.Clamp(_sel, 0, _guide.Count - 1)].portrait) != null ? "loaded" : "missing")})";
                case Page.Map: return LastStarMapLog + (_map != null ? $", cursor on {_map.Selected}" : "");
                case Page.Settings: return "Settings: " + GameSettings.Describe() + $", row {_sel}";
                default: return $"Main menu: [{string.Join(" | ", MainLabels())}] (Upgrades slot hidden), row {_sel}";
            }
        }
    }
}
