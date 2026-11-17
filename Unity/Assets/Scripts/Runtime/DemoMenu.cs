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
        const string ContinueScene = "@continue", QuitScene = "@quit";
        (string label, string scene)[] Items;
        static (string, string)[] BuildItems()
        {
            var l = new System.Collections.Generic.List<(string, string)>();
            if (SaveGame.HasChapter1Save) l.Add(("Continue  —  explore the saucer", ContinueScene));
            l.Add(("Chapter 1 — The Moon Prison", "Chapter1")); l.Add(("Fight demo", "FightDemo")); l.Add(("Saucer demo", "Saucer"));
            l.Add(("Ratwing", "Ratwing")); l.Add(("Quit", QuitScene));
            return l.ToArray();
        }
        bool _confirmQuit;
        Text _hint;
        int _sel;
        Text[] _labels;
        Image[] _marks;
        Font _font;
        float _stickReady;

        void Start()
        {
            if (DemoBoot.CaptureDir != null && DemoBoot.MenuShot) { Build(); StartCoroutine(Shot()); return; }
            if (DemoBoot.Gallery) GalleryTV.Preload();      // the gallery check run exercises the preload path
            if (DemoBoot.SoundCheck) { gameObject.AddComponent<TelepathySoundCheck>(); return; }
            if (DemoBoot.Continue) { SaveGame.Continue(); return; }
            if (DemoBoot.Chapter1Aboard) { Chapter1State.StartAboard(); return; }
            if (DemoBoot.Chapter1) { SceneManager.LoadScene("Chapter1"); return; }
            if (DemoBoot.Saucer) { SceneManager.LoadScene("Saucer"); return; }
            if (DemoBoot.Ratwing) { SceneManager.LoadScene("Ratwing"); return; }
            if (DemoBoot.Fight || DemoBoot.Room || DemoBoot.CaptureDir != null) { SceneManager.LoadScene("FightDemo"); return; }
            Build();
        }

        void Build()
        {
            Items = BuildItems();
            MusicSystem.Play("main_theme", 1.5f);
            GalleryTV.Preload();            // Round 8: decode the gallery while the menu is idle
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("MenuCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Cam;
            canvas.planeDistance = 1f;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1f;

            Text(canvasGo.transform, "7", new Vector2(0, 240), 110, new Color(0.95f, 0.95f, 0.95f));
            Text(canvasGo.transform, "THE OBSERVER", new Vector2(0, 160), 26, new Color(0.7f, 0.7f, 0.7f));
            _labels = new Text[Items.Length];
            _marks = new Image[Items.Length];
            for (int i = 0; i < Items.Length; i++)
            {
                var y = 30 - i * 56;
                var bar = new GameObject("Item" + i, typeof(RectTransform));
                bar.transform.SetParent(canvasGo.transform, false);
                var rt = (RectTransform)bar.transform;
                rt.anchoredPosition = new Vector2(0, y + (Items.Length > 5 ? 40 : 0)); rt.sizeDelta = new Vector2(Items[i].scene.StartsWith("@") || Items[i].scene == "Chapter1" ? 520 : 360, 50);
                _marks[i] = bar.AddComponent<Image>();
                _labels[i] = Text(bar.transform, Items[i].label, Vector2.zero, 28, Color.white);
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
            if (_hint != null) _hint.text = _confirmQuit ? "Quit the game?   A / Enter = yes   ·   B / Esc = no" : "stick / d-pad + A    ·    W S / arrows + Enter";
            for (int i = 0; i < Items.Length; i++)
            {
                bool on = i == _sel;
                if (Items[i].scene == QuitScene) _labels[i].text = _confirmQuit ? "Quit?  Yes (A / Enter)" : "Quit";
                _marks[i].color = on ? new Color(0.92f, 0.92f, 0.92f, 1f) : new Color(1f, 1f, 1f, 0.06f);
                _labels[i].color = on ? new Color(0.05f, 0.05f, 0.05f) : new Color(0.85f, 0.85f, 0.85f);
            }
        }

        void Update()
        {
            if (_labels == null || DemoBoot.CaptureDir != null) return;
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
            if (d != 0) { _sel = (_sel + d + Items.Length) % Items.Length; _confirmQuit = false; AudioKit.Play("menu_move", null, 0.6f); Refresh(); }
            bool go = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                      || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            bool back = (kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.buttonEast.wasPressedThisFrame);
            if (back && _confirmQuit) { _confirmQuit = false; Refresh(); return; }
            if (!go) return;
            AudioKit.Play("menu_confirm", null, 0.7f);
            var scene = Items[_sel].scene;
            if (scene == QuitScene)
            {
                if (!_confirmQuit) { _confirmQuit = true; Refresh(); return; }
                PauseMenu.QuitGame();
                return;
            }
            if (scene == ContinueScene) { SaveGame.Continue(); return; }
            Chapter1State.Reset();
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
