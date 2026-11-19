using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Seven
{
    /// <summary>
    /// QF14: a menu list that does not fit scrolls - shared by the main menu and the pause menu. Shows <see cref="Visible"/>
    /// rows from <see cref="Top"/>, always keeps the selected row in view (stick / d-pad / W S / arrows / the mouse wheel
    /// all move the selection) and draws a thin monochrome scroll bar (gray track, white thumb) on the right only when the
    /// list overflows.
    /// </summary>
    public sealed class MenuScroll
    {
        public int Visible;
        public int Top { get; private set; }
        Image _track, _thumb;
        float _height;
        public bool Shown => _track != null && _track.gameObject.activeSelf;

        public MenuScroll(int visible) { Visible = Mathf.Max(1, visible); }

        /// <summary>The scroll bar: a track of the given height whose top edge is at topY, centred on x.</summary>
        public void Make(Transform parent, float x, float topY, float height)
        {
            _height = height;
            _track = Img(parent, "ScrollTrack", new Color(1f, 1f, 1f, 0.14f));
            var rt = _track.rectTransform;
            rt.anchoredPosition = new Vector2(x, topY - height * 0.5f); rt.sizeDelta = new Vector2(6f, height);
            _thumb = Img(_track.transform, "ScrollThumb", new Color(0.9f, 0.9f, 0.9f, 1f));
            var tr = _thumb.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 1f); tr.pivot = new Vector2(0.5f, 1f);
            _track.gameObject.SetActive(false);
        }

        static Image Img(Transform parent, string name, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var im = go.AddComponent<Image>();
            im.color = c; im.raycastTarget = false;
            return im;
        }

        public void Home() { Top = 0; }

        /// <summary>Keep row sel of count in view (the window moves only as far as it must); updates the bar. Returns Top.</summary>
        public int Follow(int sel, int count)
        {
            int top = Top;
            if (count <= Visible) top = 0;
            else
            {
                if (sel < top) top = sel;
                else if (sel >= top + Visible) top = sel - Visible + 1;
                top = Mathf.Clamp(top, 0, count - Visible);
            }
            Top = top;
            if (_track != null)
            {
                bool show = count > Visible;
                _track.gameObject.SetActive(show);
                if (show)
                {
                    float th = Mathf.Max(24f, _height * Visible / count);
                    float y = (_height - th) * top / (count - Visible);
                    _thumb.rectTransform.sizeDelta = new Vector2(6f, th);
                    _thumb.rectTransform.anchoredPosition = new Vector2(0f, -y);
                }
            }
            return top;
        }

        public bool InView(int i) => i >= Top && i < Top + Visible;

        /// <summary>The mouse wheel as a selection step: up = -1, down = +1, still = 0.</summary>
        public static int Wheel()
        {
            var m = Mouse.current;
            if (m == null) return 0;
            float y = m.scroll.ReadValue().y;
            return y > 0.01f ? -1 : y < -0.01f ? 1 : 0;
        }
    }
}
