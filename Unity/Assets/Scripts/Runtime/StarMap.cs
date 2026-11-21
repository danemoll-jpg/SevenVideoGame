using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Seven
{
    /// <summary>
    /// Round 13 (item 2) + QF18 (22): the star map on the ship's navigation display (a world-space uGUI panel built by
    /// <see cref="ShipConsole"/>), grayscale with Kyle's glow, in TWO levels:
    ///  - OUR SOLAR SYSTEM: the Sun's glow at the centre and only the places she can reach, each on its orbit (tilted
    ///    ellipses): the Moon (on Earth's orbit), Venus, Mars and Europa (on Jupiter's orbit).
    ///  - THE BIAVEH SYSTEM: Biaveh's star, Biaveh on its orbit, the asteroid with its structures.
    /// Between them, a zoomed-out view (our Sun on one side, Biaveh's star on the other, the route line joining them once the
    /// combined records unlocked it) plays whenever the map switches system. Each system has a gateway at its edge to the
    /// other (only once Biaveh is unlocked - before MAR_13C the Biaveh system does not exist on the map). The map opens on the
    /// system the saucer is in. The saucer's position is marked with a ring and "THE SHIP". Plain class (not a MonoBehaviour).
    /// </summary>
    public class StarMap
    {
        public class Body
        {
            public string Id, Name, Sys;
            public float OrbitX, Angle, Size;
            public bool Shown, Selectable, Gateway;
            public string Note;
            public RectTransform Dot, Halo, Orbit;
            public Text Label;
            public Vector2 Pos;
        }

        /// <summary>The reachable places per system, inner to outer (orbit = the horizontal semi-axis in canvas units).</summary>
        public static readonly (string id, string name, float orbit, float angle, float size, string sys)[] Defs =
        {
            ("venus", "Venus", 0.165f, 206f, 18f, "sol"),
            ("moon", "The Moon", 0.25f, 328f, 11f, "sol"),
            ("mars", "Mars", 0.34f, 52f, 14f, "sol"),
            ("europa", "Europa", 0.45f, 134f, 9f, "sol"),
            ("biaveh", "Biavian contact — Veyra", 0.24f, 214f, 17f, "biaveh"),   // QF22: the mothership among Veyra's rings (id kept for saves)
            ("asteroid", "The asteroid", 0.40f, 318f, 7f, "biaveh"),    // found after the warp into Veyra's system (TRV_AST)
        };
        public const string Sol = "sol", Bia = "biaveh";
        public static string SystemOf(string id) { foreach (var d in Defs) if (d.id == id) return d.sys; return id == "biaveh_space" ? Bia : Sol; }
        const float Tilt = 0.46f;                 // ellipse squash (the map is seen at an angle)

        public readonly List<Body> Bodies = new List<Body>();
        public RectTransform Root { get; private set; }
        public string Selected { get; private set; }
        public string Here { get; private set; }
        public bool Confirming { get; private set; }
        /// <summary>QF18: the system on screen ("sol" / "biaveh").</summary>
        public string View { get; private set; } = Sol;
        public bool Switching => _switchT >= 0f;
        public bool BiavehOnMap { get; private set; }
        /// <summary>Optional heading over the info line (C13's "Choose the next world").</summary>
        public string Heading;
        Text _name, _note, _action, _heading, _shipLabel, _title;
        RectTransform _cursor, _ship, _pointer;
        Image _cursorImg;
        Vector2 _size;
        Font _font;
        float _t;
        Vector2 _pointerPos; bool _pointerOn;
        readonly Dictionary<string, RectTransform> _sys = new Dictionary<string, RectTransform>();
        readonly Dictionary<string, CanvasGroup> _sysG = new Dictionary<string, CanvasGroup>();
        RectTransform _over; CanvasGroup _overG; readonly List<Image> _route = new List<Image>();
        float _switchT = -1f; string _switchTo;

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
            Img(Root, null, Vector2.zero, size, new Color(0.02f, 0.02f, 0.02f, 0.94f));
            // the two systems, each in its own group (one is shown; the switch zooms out through the overview)
            foreach (var (sys, star) in new[] { (Sol, "THE SUN"), (Bia, "VEYRA'S STAR") })
            {
                var g = new GameObject("System_" + sys, typeof(RectTransform)); g.transform.SetParent(Root, false);
                var rt = (RectTransform)g.transform; rt.sizeDelta = size; _sys[sys] = rt; _sysG[sys] = g.AddComponent<CanvasGroup>();
                Img(rt, _soft, MapCentre, new Vector2(size.x * 0.9f, size.x * 0.9f * Tilt * 1.4f), new Color(1f, 1f, 1f, sys == Sol ? 0.10f : 0.08f));
                Img(rt, _soft, MapCentre, new Vector2(170f, 170f), new Color(1f, 1f, 1f, 0.55f));
                Img(rt, _disc, MapCentre, new Vector2(sys == Sol ? 34f : 28f, sys == Sol ? 34f : 28f), Color.white);
                var lbl = Txt(rt, star, MapCentre + new Vector2(0f, -38f), 18, new Color(0.62f, 0.62f, 0.62f), TextAnchor.MiddleCenter, new Vector2(240f, 24f));
                lbl.name = sys == Sol ? "SunLabel" : "BiavehStarLabel";
            }
            // the overview between the systems: our Sun left, Biaveh's star right, the route joining them
            {
                var g = new GameObject("Overview", typeof(RectTransform)); g.transform.SetParent(Root, false); _over = (RectTransform)g.transform; _over.sizeDelta = size;
                _overG = g.AddComponent<CanvasGroup>(); _overG.alpha = 0f;
                var a = new Vector2(-size.x * 0.34f, 20f); var b = new Vector2(size.x * 0.34f, 20f);
                foreach (var (p, n) in new[] { (a, "OUR SUN"), (b, "VEYRA'S STAR") })
                {
                    Img(_over, _soft, p, new Vector2(150f, 150f), new Color(1f, 1f, 1f, 0.5f)); Img(_over, _disc, p, new Vector2(24f, 24f), Color.white);
                    Txt(_over, n, p + new Vector2(0f, -40f), 18, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleCenter, new Vector2(240f, 24f));
                }
                for (int i = 1; i < 40; i++) { var im = Img(_over, _disc, Vector2.Lerp(a, b, i / 40f) + new Vector2(0f, Mathf.Sin(i / 40f * Mathf.PI) * 30f), new Vector2(5f, 5f), new Color(1f, 1f, 1f, 0f)); _route.Add(im); }
                Txt(_over, "THE ROUTE", new Vector2(0f, 74f), 17, new Color(0.62f, 0.62f, 0.62f), TextAnchor.MiddleCenter, new Vector2(240f, 22f));
            }
            _title = Txt(Root, "STAR MAP  ·  OUR SOLAR SYSTEM", new Vector2(-size.x * 0.5f + 26f, size.y * 0.5f - 30f), 26, new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleLeft, new Vector2(700f, 30f), new Vector2(0f, 0.5f));
            _heading = Txt(Root, "", new Vector2(size.x * 0.5f - 26f, size.y * 0.5f - 64f), 23, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleRight, new Vector2(620f, 30f), new Vector2(1f, 0.5f));
            foreach (var d in Defs)                    // orbits first (under every body and label)
            {
                var rt = _sys[d.sys];
                var b = new Body { Id = d.id, Name = d.name, Sys = d.sys, OrbitX = d.orbit * size.x, Angle = d.angle, Size = d.size };
                float a = d.angle * Mathf.Deg2Rad;
                b.Pos = MapCentre + new Vector2(Mathf.Cos(a) * b.OrbitX, Mathf.Sin(a) * b.OrbitX * Tilt);
                b.Orbit = Img(rt, OrbitSprite(b.OrbitX, b.OrbitX * Tilt), MapCentre, new Vector2(b.OrbitX * 2f + 8f, b.OrbitX * Tilt * 2f + 8f), new Color(1f, 1f, 1f, 0.32f)).rectTransform;
                Bodies.Add(b);
            }
            for (int i = 0; i < Defs.Length; i++)
            {
                var d = Defs[i]; var b = Bodies[i]; var rt = _sys[d.sys];
                b.Halo = Img(rt, _soft, b.Pos, Vector2.one * (d.size * 4.2f), new Color(1f, 1f, 1f, 0.32f)).rectTransform;
                b.Dot = Img(rt, _disc, b.Pos, Vector2.one * d.size, new Color(0.92f, 0.92f, 0.92f, 1f)).rectTransform;
                if (d.id == "asteroid") { Img(rt, _ringThin, b.Pos, Vector2.one * (d.size * 2.2f), new Color(1f, 1f, 1f, 0.7f)); }   // its structures: a faint ring of light
                if (d.id == "biaveh")
                {
                    // QF22: Veyra with its rings (a wide tilted ellipse round the planet), the mothership a small bright mark beside it
                    Img(rt, OrbitSprite(d.size * 1.6f, d.size * 0.45f), b.Pos, new Vector2(d.size * 3.4f, d.size * 1.1f), new Color(1f, 1f, 1f, 0.75f));
                    Img(rt, OrbitSprite(d.size * 1.25f, d.size * 0.34f), b.Pos, new Vector2(d.size * 2.7f, d.size * 0.85f), new Color(1f, 1f, 1f, 0.4f));
                    Img(rt, _disc, b.Pos + new Vector2(d.size * 1.45f, d.size * 0.55f), new Vector2(5f, 7f), Color.white);
                    Txt(rt, "VEYRA", b.Pos + new Vector2(d.size * 2.2f, -d.size * 0.2f), 15, new Color(0.6f, 0.6f, 0.6f), TextAnchor.MiddleCenter, new Vector2(160f, 20f));
                }
                b.Label = Txt(rt, d.name, b.Pos + new Vector2(0f, d.size * 0.5f + 18f), 24, new Color(0.88f, 0.88f, 0.88f), TextAnchor.MiddleCenter, new Vector2(d.id == "biaveh" ? 360f : 220f, 26f));   // QF22: the contact's name on one line
            }
            // the gateways at the edges (to the other system)
            foreach (var (id, name, sys, pos) in new[] { ("to_biaveh", "The Veyra system  →", Sol, new Vector2(size.x * 0.40f, -size.y * 0.04f)), ("to_sol", "←  Our solar system", Bia, new Vector2(-size.x * 0.40f, -size.y * 0.04f)) })
            {
                var rt = _sys[sys];
                var b = new Body { Id = id, Name = name, Sys = sys, Gateway = true, Pos = pos, Size = 10f };
                b.Halo = Img(rt, _soft, pos, Vector2.one * 46f, new Color(1f, 1f, 1f, 0.25f)).rectTransform;
                b.Dot = Img(rt, _ringThin, pos, Vector2.one * 20f, new Color(1f, 1f, 1f, 0.9f)).rectTransform;
                b.Orbit = b.Dot;
                b.Label = Txt(rt, name, pos + new Vector2(0f, 26f), 21, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleCenter, new Vector2(300f, 26f));
                Bodies.Add(b);
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
            ShowSystem(Sol);
        }

        Vector2 MapCentre => new Vector2(0f, 38f);

        void ShowSystem(string sys)
        {
            View = sys;
            foreach (var kv in _sys) { kv.Value.gameObject.SetActive(kv.Key == sys); _sysG[kv.Key].alpha = 1f; kv.Value.localScale = Vector3.one; }
            if (_overG != null) _overG.alpha = 0f;
            _title.text = sys == Sol ? "STAR MAP  ·  OUR SOLAR SYSTEM" : "STAR MAP  ·  THE VEYRA SYSTEM";
            PlaceShip();
        }
        void PlaceShip()
        {
            var h = Find(Here); bool hereInView = h != null && h.Shown && h.Sys == View;
            bool spaceHere = Here == "biaveh_space" && View == Bia;
            _ship.gameObject.SetActive(hereInView || spaceHere); _shipLabel.gameObject.SetActive(hereInView || spaceHere);
            var p = hereInView ? h.Pos : new Vector2(-_size.x * 0.30f, MapCentre.y - 60f);
            float r = hereInView ? h.Size : 10f;
            _ship.anchoredPosition = p; _shipLabel.rectTransform.anchoredPosition = p + new Vector2(0f, -r * 0.5f - 22f);
        }

        /// <summary>Which bodies are drawn / selectable, their info lines (null = "Not visited yet."), and where the ship is
        /// ("biaveh_space" = in the Biaveh system, between its worlds). The map shows the system of the selection (else the
        /// ship's).</summary>
        public void Configure(ICollection<string> shown, ICollection<string> selectable, IDictionary<string, string> notes, string here, string select = null)
        {
            Here = here;
            BiavehOnMap = false;
            foreach (var b in Bodies)
            {
                if (b.Gateway) continue;
                b.Shown = shown.Contains(b.Id);
                b.Selectable = b.Shown && selectable.Contains(b.Id);
                b.Note = notes != null && notes.TryGetValue(b.Id, out var n) ? n : null;
                if (b.Id == "biaveh" && b.Label != null) b.Label.text = WorldFlags.ContactName;   // QF22
                b.Orbit.gameObject.SetActive(b.Shown); b.Halo.gameObject.SetActive(b.Shown); b.Dot.gameObject.SetActive(b.Shown); b.Label.gameObject.SetActive(b.Shown);
                b.Label.color = b.Selectable ? new Color(0.88f, 0.88f, 0.88f) : new Color(0.5f, 0.5f, 0.5f);
                if (b.Shown && b.Sys == Bia) BiavehOnMap = true;
            }
            // the gateways exist only once the Biaveh system is known (MAR_13C)
            foreach (var b in Bodies)
            {
                if (!b.Gateway) continue;
                bool other = false; foreach (var o in Bodies) if (!o.Gateway && o.Shown && o.Sys != b.Sys) other = true;
                b.Shown = BiavehOnMap && other; b.Selectable = b.Shown;
                b.Dot.gameObject.SetActive(b.Shown); b.Halo.gameObject.SetActive(b.Shown); b.Label.gameObject.SetActive(b.Shown);
            }
            Confirming = false;
            var first = select != null && Find(select) != null && Find(select).Selectable ? select : null;
            string sys = first != null ? SystemOf(first) : SystemOf(here);
            if (sys == Bia && !BiavehOnMap) sys = Sol;
            ShowSystem(sys);
            if (first == null) foreach (var b in Bodies) if (b.Selectable && !b.Gateway && b.Sys == View && b.Id != here) { first = b.Id; break; }
            if (first == null) foreach (var b in Bodies) if (b.Selectable && b.Sys == View) { first = b.Id; break; }
            Select(first);
        }

        public Body Find(string id) { foreach (var b in Bodies) if (b.Id == id) return b; return null; }
        public bool IsGateway(string id) { var b = Find(id); return b != null && b.Gateway; }
        public List<string> SelectableIds() { var l = new List<string>(); foreach (var b in Bodies) if (b.Selectable && !b.Gateway) l.Add(b.Id); return l; }
        public List<string> ShownIds() { var l = new List<string>(); foreach (var b in Bodies) if (b.Shown && !b.Gateway) l.Add(b.Id); return l; }
        List<string> ViewIds() { var l = new List<string>(); foreach (var b in Bodies) if (b.Selectable && b.Sys == View) l.Add(b.Id); return l; }

        public void Select(string id)
        {
            var b = Find(id);
            if (b == null || !b.Selectable) { Selected = null; _cursor.gameObject.SetActive(false); Refresh(); return; }
            if (b.Sys != View && !Switching) ShowSystem(b.Sys);
            Selected = id; Confirming = false;
            _cursor.gameObject.SetActive(true); _cursor.anchoredPosition = b.Pos;
            Refresh();
        }

        /// <summary>Stick / d-pad / keys: the next selectable body in this system (the gateway included), wrapping.</summary>
        public void Step(int d)
        {
            if (Switching) return;
            var ids = ViewIds();
            if (ids.Count == 0 || d == 0) return;
            int i = Mathf.Max(0, ids.IndexOf(Selected));
            Select(ids[(i + d + ids.Count) % ids.Count]);
        }

        public void SetConfirm(bool on) { Confirming = on && Selected != null && !IsGateway(Selected); Refresh(); }

        /// <summary>QF18 (22): through the gateway - zoom out of this system, the overview (both stars, the route drawn
        /// between them), into the other system; the first world there is selected.</summary>
        public void SwitchSystem()
        {
            if (Switching) return;
            _switchTo = View == Sol ? Bia : Sol; _switchT = 0f;
            _cursor.gameObject.SetActive(false); Confirming = false;
            foreach (var im in _route) { var c = im.color; c.a = 0f; im.color = c; }
            AudioKit.Play("console_select", null, 0.6f);
        }

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
            foreach (var b in Bodies) { if (!b.Selectable || b.Sys != View) continue; float dd = Vector2.Distance(b.Pos, _pointerPos); if (dd < bd) { bd = dd; best = b; } }
            return best?.Id;
        }
        public void HidePointer() { _pointerOn = false; _pointer.gameObject.SetActive(false); }

        public void Tick(float dt)
        {
            _t += dt;
            if (_switchT >= 0f)
            {
                _switchT += dt / 3.0f;
                float u = _switchT;
                var from = _sys[View]; var to = _sys[_switchTo];
                if (u < 0.3f) { float k = u / 0.3f; from.localScale = Vector3.one * Mathf.Lerp(1f, 0.25f, k); _sysG[View].alpha = 1f - k; _overG.alpha = k; _ship.gameObject.SetActive(false); _shipLabel.gameObject.SetActive(false); }
                else if (u < 0.7f)
                {
                    from.gameObject.SetActive(false); _overG.alpha = 1f;
                    float k = (u - 0.3f) / 0.4f;
                    for (int i = 0; i < _route.Count; i++) { var c = _route[i].color; c.a = BiavehOnMap && i < k * _route.Count * 1.2f ? 0.85f : 0f; _route[i].color = c; }
                    _title.text = "STAR MAP  ·  BETWEEN THE SYSTEMS";
                }
                else if (u < 1f) { float k = (u - 0.7f) / 0.3f; to.gameObject.SetActive(true); to.localScale = Vector3.one * Mathf.Lerp(0.25f, 1f, k); _sysG[_switchTo].alpha = k; _overG.alpha = 1f - k; }
                else
                {
                    string sys = _switchTo; _switchT = -1f; ShowSystem(sys);
                    string first = null; foreach (var b in Bodies) if (b.Selectable && !b.Gateway && b.Sys == sys) { first = b.Id; break; }
                    Select(first ?? (sys == Sol ? "to_biaveh" : "to_sol"));
                }
            }
            if (_cursor != null && _cursor.gameObject.activeSelf)
            {
                float s = 1f + 0.08f * Mathf.Sin(_t * 4.2f);
                _cursor.localScale = Vector3.one * (Confirming ? 1.18f : s);
                _cursorImg.color = new Color(1f, 1f, 1f, Confirming ? 1f : 0.75f + 0.25f * Mathf.Sin(_t * 4.2f));
            }
            foreach (var b in Bodies)
                if (b.Shown && b.Halo != null) b.Halo.localScale = Vector3.one * (b.Id == Selected ? 1.25f + 0.1f * Mathf.Sin(_t * 3f) : 0.9f);
        }

        void Refresh()
        {
            var b = Find(Selected);
            _heading.text = Heading ?? "";
            if (b == null) { _name.text = ""; _note.text = ""; _action.text = ""; return; }
            if (b.Gateway)
            {
                _name.text = b.Sys == Sol ? "The Veyra system" : "Our solar system";
                _note.text = b.Sys == Sol ? "Beyond this solar system - the old Biavian contact route from the combined records leads there." : "Back across to the Sun and its worlds.";
                _action.text = "A  Show it   ·   B  Back";
                return;
            }
            _name.text = b.Id == "biaveh" ? WorldFlags.ContactName : b.Name;
            _note.text = (b.Id == Here ? "The ship is here.  " : "") + (string.IsNullOrEmpty(b.Note) ? "Not visited yet." : b.Note);
            _action.text = Confirming ? $"Set course for {(b.Id == "biaveh" ? WorldFlags.ContactName : b.Name)}?   A  Yes   ·   B  No" : "A  Set course   ·   B  Back";
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
