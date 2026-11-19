using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Seven
{
    /// <summary>
    /// Round 13 (item 2): the star map drawn on the ship's navigation display (a world-space uGUI panel built by
    /// <see cref="ShipConsole"/>). The solar system in grayscale with Kyle's glow: the Sun's glow at the centre and ONLY the
    /// places she can reach, each on its orbit (tilted ellipses): the Moon (on Earth's orbit, no Earth drawn) and Venus always;
    /// Mars and Europa (on Jupiter's orbit) once VEN_C12 has sent them to the ship. The saucer's current position is marked
    /// with a small ring and "THE SHIP". A cursor ring moves between the selectable bodies; the info line shows the name and,
    /// once visited, one line from the Log. Plain class (not a MonoBehaviour); every colour is a gray.
    /// </summary>
    public class StarMap
    {
        public class Body
        {
            public string Id, Name;
            public float OrbitX, Angle, Size;
            public bool Shown, Selectable;
            public string Note;
            public RectTransform Dot, Halo, Orbit;
            public Text Label;
            public Vector2 Pos;
        }

        /// <summary>The reachable places, inner to outer (orbit = the horizontal semi-axis in canvas units).</summary>
        public static readonly (string id, string name, float orbit, float angle, float size)[] Defs =
        {
            ("venus", "Venus", 0.165f, 206f, 18f),
            ("moon", "The Moon", 0.25f, 328f, 11f),
            ("mars", "Mars", 0.34f, 52f, 14f),
            ("europa", "Europa", 0.45f, 134f, 9f),
        };
        const float Tilt = 0.46f;                 // ellipse squash (the map is seen at an angle)

        public readonly List<Body> Bodies = new List<Body>();
        public RectTransform Root { get; private set; }
        public string Selected { get; private set; }
        public string Here { get; private set; }
        public bool Confirming { get; private set; }
        /// <summary>Optional heading over the info line (C13's "Choose the next world").</summary>
        public string Heading;
        Text _name, _note, _action, _heading, _shipLabel;
        RectTransform _cursor, _ship, _pointer;
        Image _cursorImg;
        Vector2 _size;
        Font _font;
        float _t;
        Vector2 _pointerPos; bool _pointerOn;

        static Sprite _disc, _soft, _ringThin;
        /// <summary>The soft white glow sprite (also used by the console's menu screen).</summary>
        public static Sprite SoftSprite { get { MakeSprites(); return _soft; } }
        static readonly Dictionary<string, Sprite> _orbits = new Dictionary<string, Sprite>();

        public StarMap(RectTransform parent, Font font, Vector2 size)
        {
            _font = font; _size = size;
            var go = new GameObject("StarMap", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Root = (RectTransform)go.transform; Root.sizeDelta = size; Root.anchoredPosition = Vector2.zero;
            MakeSprites();
            // backdrop: deep dark glass, a broad soft glow around the Sun (Kyle's bright misty centre)
            Img(Root, null, Vector2.zero, size, new Color(0.02f, 0.02f, 0.02f, 0.94f));
            Img(Root, _soft, MapCentre, new Vector2(size.x * 0.9f, size.x * 0.9f * Tilt * 1.4f), new Color(1f, 1f, 1f, 0.10f));
            Img(Root, _soft, MapCentre, new Vector2(170f, 170f), new Color(1f, 1f, 1f, 0.55f));
            Img(Root, _disc, MapCentre, new Vector2(34f, 34f), new Color(1f, 1f, 1f, 1f));
            var sun = Txt(Root, "THE SUN", MapCentre + new Vector2(0f, -38f), 18, new Color(0.62f, 0.62f, 0.62f), TextAnchor.MiddleCenter, new Vector2(200f, 24f));
            sun.name = "SunLabel";
            Txt(Root, "STAR MAP", new Vector2(-size.x * 0.5f + 26f, size.y * 0.5f - 30f), 26, new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleLeft, new Vector2(400f, 30f), new Vector2(0f, 0.5f));
            _heading = Txt(Root, "", new Vector2(size.x * 0.5f - 26f, size.y * 0.5f - 30f), 23, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleRight, new Vector2(620f, 30f), new Vector2(1f, 0.5f));
            foreach (var d in Defs)                    // orbits first (under every body and label)
            {
                var b = new Body { Id = d.id, Name = d.name, OrbitX = d.orbit * size.x, Angle = d.angle, Size = d.size };
                float a = d.angle * Mathf.Deg2Rad;
                b.Pos = MapCentre + new Vector2(Mathf.Cos(a) * b.OrbitX, Mathf.Sin(a) * b.OrbitX * Tilt);
                b.Orbit = Img(Root, OrbitSprite(b.OrbitX, b.OrbitX * Tilt), MapCentre, new Vector2(b.OrbitX * 2f + 8f, b.OrbitX * Tilt * 2f + 8f), new Color(1f, 1f, 1f, 0.32f)).rectTransform;
                Bodies.Add(b);
            }
            for (int i = 0; i < Defs.Length; i++)
            {
                var d = Defs[i]; var b = Bodies[i];
                b.Halo = Img(Root, _soft, b.Pos, Vector2.one * (d.size * 4.2f), new Color(1f, 1f, 1f, 0.32f)).rectTransform;
                b.Dot = Img(Root, _disc, b.Pos, Vector2.one * d.size, new Color(0.92f, 0.92f, 0.92f, 1f)).rectTransform;
                b.Label = Txt(Root, d.name, b.Pos + new Vector2(0f, d.size * 0.5f + 18f), 24, new Color(0.88f, 0.88f, 0.88f), TextAnchor.MiddleCenter, new Vector2(220f, 26f));
            }
            _ship = Img(Root, _ringThin, Vector2.zero, Vector2.one * 36f, new Color(0.8f, 0.8f, 0.8f, 0.9f)).rectTransform;
            _shipLabel = Txt(Root, "THE SHIP", Vector2.zero, 16, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleCenter, new Vector2(120f, 20f));
            _cursorImg = Img(Root, _ringThin, Vector2.zero, Vector2.one * 58f, Color.white);
            _cursor = _cursorImg.rectTransform;
            _pointer = Img(Root, _ringThin, Vector2.zero, Vector2.one * 16f, new Color(1f, 1f, 1f, 0.8f)).rectTransform;
            _pointer.gameObject.SetActive(false);
            // info panel: name + one Log line + the action
            float by = -size.y * 0.5f;
            Img(Root, null, new Vector2(0f, by + 62f), new Vector2(size.x - 40f, 104f), new Color(1f, 1f, 1f, 0.05f));
            _name = Txt(Root, "", new Vector2(-size.x * 0.5f + 36f, by + 92f), 34, Color.white, TextAnchor.MiddleLeft, new Vector2(size.x - 600f, 40f), new Vector2(0f, 0.5f));
            _note = Txt(Root, "", new Vector2(-size.x * 0.5f + 36f, by + 46f), 23, new Color(0.78f, 0.78f, 0.78f), TextAnchor.UpperLeft, new Vector2(size.x - 72f, 52f), new Vector2(0f, 0.5f));
            _note.verticalOverflow = VerticalWrapMode.Truncate;
            _action = Txt(Root, "", new Vector2(size.x * 0.5f - 36f, by + 92f), 23, new Color(0.9f, 0.9f, 0.9f), TextAnchor.MiddleRight, new Vector2(520f, 32f), new Vector2(1f, 0.5f));
        }

        Vector2 MapCentre => new Vector2(0f, 38f);

        /// <summary>Which bodies are drawn / selectable, their info lines (null = "Not visited yet."), and where the ship is.</summary>
        public void Configure(ICollection<string> shown, ICollection<string> selectable, IDictionary<string, string> notes, string here, string select = null)
        {
            Here = here;
            foreach (var b in Bodies)
            {
                b.Shown = shown.Contains(b.Id);
                b.Selectable = b.Shown && selectable.Contains(b.Id);
                b.Note = notes != null && notes.TryGetValue(b.Id, out var n) ? n : null;
                b.Orbit.gameObject.SetActive(b.Shown); b.Halo.gameObject.SetActive(b.Shown); b.Dot.gameObject.SetActive(b.Shown); b.Label.gameObject.SetActive(b.Shown);
                b.Label.color = b.Selectable ? new Color(0.88f, 0.88f, 0.88f) : new Color(0.5f, 0.5f, 0.5f);
            }
            var h = Find(here);
            _ship.gameObject.SetActive(h != null && h.Shown); _shipLabel.gameObject.SetActive(h != null && h.Shown);
            if (h != null) { _ship.anchoredPosition = h.Pos; _shipLabel.rectTransform.anchoredPosition = h.Pos + new Vector2(0f, -h.Size * 0.5f - 22f); }
            Confirming = false;
            var first = select != null && Find(select) != null && Find(select).Selectable ? select : null;
            if (first == null) foreach (var b in Bodies) if (b.Selectable && b.Id != here) { first = b.Id; break; }
            if (first == null) foreach (var b in Bodies) if (b.Selectable) { first = b.Id; break; }
            Select(first);
        }

        public Body Find(string id) { foreach (var b in Bodies) if (b.Id == id) return b; return null; }
        public List<string> SelectableIds() { var l = new List<string>(); foreach (var b in Bodies) if (b.Selectable) l.Add(b.Id); return l; }
        public List<string> ShownIds() { var l = new List<string>(); foreach (var b in Bodies) if (b.Shown) l.Add(b.Id); return l; }

        public void Select(string id)
        {
            var b = Find(id);
            if (b == null || !b.Selectable) { Selected = null; _cursor.gameObject.SetActive(false); Refresh(); return; }
            Selected = id; Confirming = false;
            _cursor.gameObject.SetActive(true); _cursor.anchoredPosition = b.Pos;
            Refresh();
        }

        /// <summary>Stick / d-pad / keys: the next selectable body (inner to outer, wrapping).</summary>
        public void Step(int d)
        {
            var ids = SelectableIds();
            if (ids.Count == 0 || d == 0) return;
            int i = Mathf.Max(0, ids.IndexOf(Selected));
            Select(ids[(i + d + ids.Count) % ids.Count]);
        }

        public void SetConfirm(bool on) { Confirming = on && Selected != null; Refresh(); }

        /// <summary>The in-world mouse pointer (moved by mouse delta, so it also works with a locked cursor). Returns the body
        /// under it (within reach), or null.</summary>
        public string MovePointer(Vector2 delta)
        {
            if (!_pointerOn) { _pointerOn = true; _pointerPos = Selected != null ? Find(Selected).Pos : MapCentre; }
            _pointerPos += delta;
            _pointerPos.x = Mathf.Clamp(_pointerPos.x, -_size.x * 0.5f + 10f, _size.x * 0.5f - 10f);
            _pointerPos.y = Mathf.Clamp(_pointerPos.y, -_size.y * 0.5f + 10f, _size.y * 0.5f - 10f);
            _pointer.gameObject.SetActive(true); _pointer.anchoredPosition = _pointerPos;
            Body best = null; float bd = 70f;
            foreach (var b in Bodies) { if (!b.Selectable) continue; float dd = Vector2.Distance(b.Pos, _pointerPos); if (dd < bd) { bd = dd; best = b; } }
            return best?.Id;
        }
        public void HidePointer() { _pointerOn = false; _pointer.gameObject.SetActive(false); }

        public void Tick(float dt)
        {
            _t += dt;
            if (_cursor != null && _cursor.gameObject.activeSelf)
            {
                float s = 1f + 0.08f * Mathf.Sin(_t * 4.2f);
                _cursor.localScale = Vector3.one * (Confirming ? 1.18f : s);
                _cursorImg.color = new Color(1f, 1f, 1f, Confirming ? 1f : 0.75f + 0.25f * Mathf.Sin(_t * 4.2f));
            }
            foreach (var b in Bodies)
                if (b.Shown) b.Halo.localScale = Vector3.one * (b.Id == Selected ? 1.25f + 0.1f * Mathf.Sin(_t * 3f) : 0.9f);
        }

        void Refresh()
        {
            var b = Find(Selected);
            _heading.text = Heading ?? "";
            if (b == null) { _name.text = ""; _note.text = ""; _action.text = ""; return; }
            _name.text = b.Name;
            _note.text = (b.Id == Here ? "The ship is here.  " : "") + (string.IsNullOrEmpty(b.Note) ? "Not visited yet." : b.Note);
            _action.text = Confirming ? $"Set course for {b.Name}?   A  Yes   ·   B  No" : "A  Set course   ·   B  Back";
        }

        // ------------------------------------------------------------------------------ drawing helpers
        Image Img(RectTransform parent, Sprite sp, Vector2 pos, Vector2 size, Color c)
        {
            var go = new GameObject("Img", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchoredPosition = pos; rt.sizeDelta = size;
            var im = go.AddComponent<Image>(); im.sprite = sp; im.color = c; im.raycastTarget = false;
            return im;
        }

        Text Txt(RectTransform parent, string s, Vector2 pos, int size, Color c, TextAnchor anchor, Vector2 box, Vector2? pivot = null)
        {
            var go = new GameObject("Text", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.pivot = pivot ?? new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = box;
            var t = go.AddComponent<Text>(); t.font = _font; t.fontSize = size; t.text = s; t.color = c; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
            return t;
        }

        static void MakeSprites()
        {
            if (_disc != null) return;
            _disc = Radial(64, (r) => Mathf.Clamp01((1f - r) * 18f));                         // crisp disc, soft 1-px edge
            _soft = Radial(128, (r) => { float k = Mathf.Clamp01(1f - r); return k * k * k; });   // glow
            _ringThin = Radial(96, (r) => Mathf.Clamp01(1f - Mathf.Abs(r - 0.9f) * 28f));         // thin ring
        }

        static Sprite Radial(int n, System.Func<float, float> alpha)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(Mathf.Sqrt(dx * dx + dy * dy))) * 255f);
                    px[y * n + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>A thin ellipse (orbit) at 1 texel per canvas unit, anti-aliased (white, alpha = the line).</summary>
        static Sprite OrbitSprite(float a, float b)
        {
            string key = $"{a:0}x{b:0}";
            if (_orbits.TryGetValue(key, out var s) && s != null) return s;
            int w = Mathf.CeilToInt(a * 2f + 8f), h = Mathf.CeilToInt(b * 2f + 8f);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            float cx = w * 0.5f, cy = h * 0.5f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f - cx) / a, v = (y + 0.5f - cy) / b;
                    float f = Mathf.Sqrt(u * u + v * v);
                    // distance to the ellipse ~ |f - 1| / |grad f|
                    float gx = u / (a * Mathf.Max(f, 1e-4f)), gy = v / (b * Mathf.Max(f, 1e-4f));
                    float dist = Mathf.Abs(f - 1f) / Mathf.Max(1e-4f, Mathf.Sqrt(gx * gx + gy * gy));
                    byte al = (byte)Mathf.RoundToInt(Mathf.Clamp01(1.4f - dist) * 255f);
                    px[y * w + x] = new Color32(255, 255, 255, al);
                }
            tex.SetPixels32(px); tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            _orbits[key] = s;
            return s;
        }
    }
}
