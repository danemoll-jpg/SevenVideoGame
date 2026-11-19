using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Seven
{
    /// <summary>
    /// Demo picker (first scene): "Chapter 1 — The Moon Prison" (-chapter1) / "Fight demo" / "Saucer demo" / "Ratwing". Pad: stick or d-pad + A. Keys: W/S or arrows + Enter
    /// or Space. Strictly monochrome uGUI drawn by the menu camera (so it can be captured off-screen).
    /// -fight / -room / -capture skip to the fight demo, -saucer to the saucer, -ratwing to the Ratwing cave; -capture -menu screenshots the menu.
    /// </summary>
    public class DemoMenu : MonoBehaviour
    {
        public const string SceneName = "Menu";
        public Camera Cam;
        public VoiceBank Bank;          // for the -soundcheck run

        // Round 7: "Continue" on top when a Chapter 1 save exists; "Quit" at the bottom (with a confirm).
        // QF6: Continue resumes at the saved checkpoint; "Scene select" jumps to any part of Chapter 1.
        const string ContinueVenus = "@continuevenus", NewVenus = "@newvenus", ContinueScene = "@continue", QuitScene = "@quit", SelectScene = "@select", BackScene = "@back", LaterScene = "@later";
        (string label, string scene)[] Items;
        bool _selectPage;
        static (string, string)[] BuildItems()
        {
            var l = new System.Collections.Generic.List<(string, string)>();
            VenusState.UseOwnSave();
            if (VenusState.Reached)
                l.Add(("Continue  —  Venus: " + (VenusState.Objective ?? "the saucer"), ContinueVenus));
            else if (SaveGame.HasChapter1Save)
                l.Add((SaveGame.Progress >= Chapter1State.CpExplore ? "Continue  —  explore the saucer" : "Continue  —  " + Chapter1State.CpName(SaveGame.Progress), ContinueScene));
            l.Add(("Chapter 1 — The Moon Prison", "Chapter1")); l.Add(("Chapter 2 — Venus", NewVenus)); l.Add(("Scene select", SelectScene)); l.Add(("Fight demo", "FightDemo")); l.Add(("Saucer demo", "Saucer"));
            l.Add(("Ratwing", "Ratwing")); l.Add(("Quit", QuitScene));
            return l.ToArray();
        }
        /// <summary>QF6: the Scene Select page (also in the pause menu): every checkpoint of Chapter 1, then Venus (not built).</summary>
        public static (string, string)[] SelectItems()
        {
            var l = new System.Collections.Generic.List<(string, string)>();
            for (int cp = Chapter1State.CpCell; cp <= Chapter1State.CpGalley; cp++) l.Add((Chapter1State.CpName(cp), "@cp" + cp));
            l.Add(("Chapter 2 — Venus  >", "@venuspage")); l.Add(("Back", BackScene));
            return l.ToArray();
        }
        /// <summary>QF11: Chapter 2's Scene Select page - each entry starts with the state a real playthrough has there,
        /// on its own save slot (the player's Venus save is never touched).</summary>
        public static (string, string)[] VenusSelectItems()
        {
            var l = new System.Collections.Generic.List<(string, string)>();
            foreach (var (label, id) in VenusState.SelectEntries) l.Add((label, "@ven:" + id));
            l.Add(("Back", BackScene));
            return l.ToArray();
        }
        public static DemoMenu I { get; private set; }
        /// <summary>QF6 playtest: what the highlighted entry says (the driver reads the menu like a person).</summary>
        public string SelectedLabel => Items != null && _sel >= 0 && _sel < Items.Length ? Items[_sel].label : null;
        public bool OnSelectPage => _selectPage;
        GameObject _canvasGo;
        bool _confirmQuit, _confirmVenus, _venusPage;
        Text _hint;
        int _sel;
        Text[] _labels;
        Image[] _marks;
        Font _font;
        float _stickReady;

        void Start()
        {
            I = this;
            if (DemoBoot.PlayerSim && !DemoBoot.Chapter1 && !DemoBoot.VenusPlay && !DemoBoot.VenusPlay2) { Build(); return; }     // QF6 playtest: the driver uses the menu (QF10: the Venus run goes straight in)
            if (DemoBoot.CaptureDir != null && DemoBoot.MenuShot && DemoBoot.MenuVenusConfirm) { _confirmVenus = true; Build(); _sel = System.Array.FindIndex(Items, x => x.scene == NewVenus); Refresh(); StartCoroutine(Shot()); return; }
            if (DemoBoot.CaptureDir != null && DemoBoot.MenuShot) { Build(); StartCoroutine(Shot()); return; }
            if (DemoBoot.Gallery) GalleryTV.Preload();      // the gallery check run exercises the preload path
            if (DemoBoot.SoundCheck) { gameObject.AddComponent<TelepathySoundCheck>(); return; }
            if (DemoBoot.Continue) { SaveGame.Continue(); return; }
            if (DemoBoot.Chapter1Aboard) { Chapter1State.StartAboard(); return; }
            if (DemoBoot.VenusSelect != null) { VenusState.StartAt(DemoBoot.VenusSelect); return; }
            if (DemoBoot.Chapter2) { SceneManager.LoadScene("Venus"); return; }
            if (DemoBoot.Chapter1) { SceneManager.LoadScene("Chapter1"); return; }
            if (DemoBoot.Saucer) { SceneManager.LoadScene("Saucer"); return; }
            if (DemoBoot.Ratwing) { SceneManager.LoadScene("Ratwing"); return; }
            if (DemoBoot.Fight || DemoBoot.Room || DemoBoot.CaptureDir != null) { SceneManager.LoadScene("FightDemo"); return; }
            Build();
        }

        void Build()
        {
            Items = _selectPage ? (_venusPage ? VenusSelectItems() : SelectItems()) : BuildItems();
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

            // Round 10 (Dan): the logo image ("7" + THE OBSERVER, Arial Bold typeset by Tools/make_logo.py; no font shipped)
            if (_selectPage)
                Text(canvasGo.transform, _venusPage ? "SCENE SELECT  —  CHAPTER 2" : "SCENE SELECT  —  CHAPTER 1", new Vector2(0, 300), 30, new Color(0.95f, 0.95f, 0.95f));
            else if (Logo.Add(canvasGo.transform, new Vector2(0, 215), 360f, new Color(0.95f, 0.95f, 0.95f)) == null)
            {
                Text(canvasGo.transform, "7", new Vector2(0, 240), 110, new Color(0.95f, 0.95f, 0.95f));
                Text(canvasGo.transform, "THE OBSERVER", new Vector2(0, 160), 26, new Color(0.7f, 0.7f, 0.7f));
            }
            _labels = new Text[Items.Length];
            _marks = new Image[Items.Length];
            for (int i = 0; i < Items.Length; i++)
            {
                float y = _selectPage ? 245 - i * 43 : 30 - i * 56 + (Items.Length > 5 ? 40 : 0) + (Items.Length > 6 ? 16 : 0);
                var bar = new GameObject("Item" + i, typeof(RectTransform));
                bar.transform.SetParent(canvasGo.transform, false);
                var rt = (RectTransform)bar.transform;
                rt.anchoredPosition = new Vector2(0, y); rt.sizeDelta = new Vector2(_selectPage ? 560 : Items[i].scene.StartsWith("@") || Items[i].scene == "Chapter1" ? 520 : 360, _selectPage ? 38 : 50);
                _marks[i] = bar.AddComponent<Image>();
                _labels[i] = Text(bar.transform, Items[i].label, Vector2.zero, _selectPage ? 22 : 28, Color.white);
            }
            _hint = Text(canvasGo.transform, "stick / d-pad + A    ·    W S / arrows + Enter", new Vector2(0, -305), 16, new Color(0.55f, 0.55f, 0.55f));
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
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        void Refresh()
        {
            if (_hint != null) _hint.text = _confirmQuit ? "Quit the game?   A / Enter = yes   ·   B / Esc = no"
                : _confirmVenus ? "Start Chapter 2 over? Your Venus progress will be reset.   A / Enter = yes   ·   B / Esc = no" : "stick / d-pad + A    ·    W S / arrows + Enter";
            for (int i = 0; i < Items.Length; i++)
            {
                bool on = i == _sel;
                if (Items[i].scene == QuitScene) _labels[i].text = _confirmQuit ? "Quit?  Yes (A / Enter)" : "Quit";
                if (Items[i].scene == NewVenus) _labels[i].text = _confirmVenus ? "Start Chapter 2 over?  Yes (A / Enter)" : Items[i].label;
                _marks[i].color = on ? new Color(0.92f, 0.92f, 0.92f, 1f) : new Color(1f, 1f, 1f, 0.06f);
                _labels[i].color = on ? new Color(0.05f, 0.05f, 0.05f) : Items[i].scene == LaterScene ? new Color(0.45f, 0.45f, 0.45f) : new Color(0.85f, 0.85f, 0.85f);
            }
        }

        void Update()
        {
            if (_labels == null || (DemoBoot.CaptureDir != null && !DemoBoot.PlayerSim)) return;
            int d = 0;
            var kb = Keyboard.current; var gp = Gamepad.current;
            if (kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)) d = -1;
            if (kb != null && (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)) d = 1;
            if (gp != null)
            {
                if (gp.dpad.up.wasPressedThisFrame) d = -1;
                if (gp.dpad.down.wasPressedThisFrame) d = 1;
                float y = gp.leftStick.ReadValue().y;
                if (Mathf.Abs(y) < 0.3f) _stickReady = 0f;
                else if (Time.time > _stickReady) { d = y > 0 ? -1 : 1; _stickReady = Time.time + 0.35f; }
            }
            if (d != 0) { _sel = (_sel + d + Items.Length) % Items.Length; _confirmQuit = false; _confirmVenus = false; AudioKit.Play("menu_move", null, 0.6f); Refresh(); }
            bool go = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                      || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            bool back = (kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.buttonEast.wasPressedThisFrame);
            if (back && (_confirmQuit || _confirmVenus)) { _confirmQuit = false; _confirmVenus = false; Refresh(); return; }
            if (back && _selectPage && _venusPage) { _venusPage = false; _sel = System.Array.FindIndex(SelectItems(), x => x.Item2 == "@venuspage"); Build(); return; }
            if (back && _selectPage) { _selectPage = false; _sel = 0; Build(); return; }
            if (!go) return;
            AudioKit.Play("menu_confirm", null, 0.7f);
            var scene = Items[_sel].scene;
            if (scene == SelectScene) { _selectPage = true; _sel = 0; Build(); return; }
            if (scene == BackScene && _venusPage) { _venusPage = false; _sel = System.Array.FindIndex(SelectItems(), x => x.Item2 == "@venuspage"); Build(); return; }
            if (scene == BackScene) { _selectPage = false; _sel = 0; Build(); return; }
            if (scene == "@venuspage") { _venusPage = true; _sel = 0; Build(); return; }
            if (scene.StartsWith("@ven:")) { Debug.Log($"[Seven] scene select: {Items[_sel].label}"); Chapter1State.Reset(); VenusState.StartAt(scene.Substring(5)); return; }
            // QF11: "Chapter 2 — Venus" always starts the chapter fresh; with Venus progress saved, confirm first
            if (scene == NewVenus)
            {
                if (VenusState.HasProgress && !_confirmVenus) { _confirmVenus = true; Refresh(); return; }
                _confirmVenus = false; Chapter1State.Reset(); VenusState.StartFresh(); return;
            }
            if (scene == ContinueVenus) { Chapter1State.Reset(); VenusState.ContinueOwn(); return; }
            if (scene == LaterScene) return;
            if (scene.StartsWith("@cp")) { Debug.Log($"[Seven] scene select: {Items[_sel].label}"); int cp = int.Parse(scene.Substring(3)); Chapter1State.StartAt(cp); Chapter1State.Intro = cp == Chapter1State.CpCell; return; }
            if (scene == QuitScene)
            {
                if (!_confirmQuit) { _confirmQuit = true; Refresh(); return; }
                PauseMenu.QuitGame();
                return;
            }
            if (scene == ContinueScene) { SaveGame.Continue(); return; }
            Chapter1State.Reset();
            Chapter1State.Intro = scene == "Chapter1";                   // Round 12 (2d): the opening shot plays from the menu
            SceneManager.LoadScene(scene);
        }

        IEnumerator Shot()
        {
            var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 };
            Cam.targetTexture = rt;
            for (int i = 0; i < 10; i++) yield return null;
            yield return new WaitForEndOfFrame();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            Directory.CreateDirectory(DemoBoot.CaptureDir);
            File.WriteAllBytes(Path.Combine(DemoBoot.CaptureDir, "00_menu.png"), tex.EncodeToPNG());
            File.WriteAllText(Path.Combine(DemoBoot.CaptureDir, "menu_log.txt"),
                $"menu shot, muted={DemoBoot.Muted}, listenerVolume={AudioListener.volume}\n");
            Application.Quit();
        }
    }
}
