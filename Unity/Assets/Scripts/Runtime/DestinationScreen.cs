using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Seven
{
    /// <summary>
    /// Round 12 (B9, spec "open the existing navigation flow") — the saucer's destination screen after Venus: only the
    /// worlds that exist (the Moon, Venus) plus "Stay aboard". Nothing is invented for worlds not built yet, and no world
    /// is forced next. Monochrome uGUI on the given camera (captured off-screen in check runs). Pad: stick / d-pad + A,
    /// B = back; keys: W S / arrows + Enter, Esc.
    /// </summary>
    public class DestinationScreen : MonoBehaviour
    {
        public static readonly (string label, string id)[] Items =
        {
            ("The Moon", "moon"), ("Venus", "venus"), ("Stay aboard", "stay"),
        };
        public static readonly string[] Notes =
        {
            "The Moon: the prison city beneath its surface (Chapter 1, played from its start)",
            "Venus: the Elarim gardens - as you left them",
            "Back to the ship",
        };
        public string Choice { get; private set; }
        public int Selected => _sel;
        public bool Open => _go != null;
        /// <summary>Check runs: the choice is made by the script.</summary>
        [System.NonSerialized] public bool ScriptInput;
        GameObject _go; Text[] _labels; Image[] _marks; Text _note; int _sel = 1; float _stickReady;

        public IEnumerator Show(Camera cam)
        {
            Choice = null; _sel = 1;
            Build(cam);
            AudioKit.Play("menu_confirm", null, 0.6f);
            while (Choice == null) { if (!ScriptInput) HandleInput(); yield return null; }
            Destroy(_go); _go = null;
        }

        public void Pick(string id) { for (int i = 0; i < Items.Length; i++) if (Items[i].id == id) { _sel = i; Refresh(); } Choice = id; }

        void Build(Camera cam)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _go = new GameObject("DestinationScreen");
            var canvas = _go.AddComponent<Canvas>();
            canvas.renderMode = cam != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay; canvas.worldCamera = cam; canvas.planeDistance = 0.5f; canvas.sortingOrder = 50;
            var sc = _go.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1280, 720); sc.matchWidthOrHeight = 1f;
            var bg = new GameObject("Shade", typeof(RectTransform)); bg.transform.SetParent(_go.transform, false);
            var bgr = (RectTransform)bg.transform; bgr.anchorMin = Vector2.zero; bgr.anchorMax = Vector2.one; bgr.offsetMin = bgr.offsetMax = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
            Text T(string s, Vector2 pos, int size, Color c, Transform parent = null)
            {
                var g = new GameObject("Text", typeof(RectTransform)); g.transform.SetParent(parent ?? _go.transform, false);
                var rt = (RectTransform)g.transform; rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(1000, size * 1.6f);
                var t = g.AddComponent<Text>(); t.font = font; t.fontSize = size; t.text = s; t.color = c; t.alignment = TextAnchor.MiddleCenter; t.horizontalOverflow = HorizontalWrapMode.Overflow; return t;
            }
            T("SET COURSE", new Vector2(0, 200), 34, new Color(0.95f, 0.95f, 0.95f));
            _labels = new Text[Items.Length]; _marks = new Image[Items.Length];
            for (int i = 0; i < Items.Length; i++)
            {
                var bar = new GameObject("Item" + i, typeof(RectTransform)); bar.transform.SetParent(_go.transform, false);
                var rt = (RectTransform)bar.transform; rt.anchoredPosition = new Vector2(0, 90 - i * 64); rt.sizeDelta = new Vector2(420, 52);
                _marks[i] = bar.AddComponent<Image>();
                _labels[i] = T(Items[i].label, Vector2.zero, 28, Color.white, bar.transform);
            }
            _note = T("", new Vector2(0, -130), 18, new Color(0.7f, 0.7f, 0.7f));
            T("stick / d-pad + A    ·    W S / arrows + Enter    ·    B / Esc = stay aboard", new Vector2(0, -300), 16, new Color(0.55f, 0.55f, 0.55f));
            Refresh();
        }

        void Refresh()
        {
            if (_labels == null) return;
            for (int i = 0; i < Items.Length; i++)
            {
                bool on = i == _sel;
                _marks[i].color = on ? new Color(0.92f, 0.92f, 0.92f, 1f) : new Color(1f, 1f, 1f, 0.07f);
                _labels[i].color = on ? new Color(0.05f, 0.05f, 0.05f) : new Color(0.85f, 0.85f, 0.85f);
            }
            if (_note != null) _note.text = Notes[_sel];
        }

        void HandleInput()
        {
            int d = 0; var kb = Keyboard.current; var gp = Gamepad.current;
            if (kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)) d = -1;
            if (kb != null && (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)) d = 1;
            if (gp != null)
            {
                if (gp.dpad.up.wasPressedThisFrame) d = -1;
                if (gp.dpad.down.wasPressedThisFrame) d = 1;
                float y = gp.leftStick.ReadValue().y;
                if (Mathf.Abs(y) < 0.3f) _stickReady = 0f; else if (Time.unscaledTime > _stickReady) { d = y > 0 ? -1 : 1; _stickReady = Time.unscaledTime + 0.35f; }
            }
            if (d != 0) { _sel = (_sel + d + Items.Length) % Items.Length; AudioKit.Play("menu_move", null, 0.6f); Refresh(); }
            bool go = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            bool back = (kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.buttonEast.wasPressedThisFrame);
            if (back) { Choice = "stay"; return; }
            if (go) { AudioKit.Play("menu_confirm", null, 0.7f); Choice = Items[_sel].id; }
        }
    }
}
