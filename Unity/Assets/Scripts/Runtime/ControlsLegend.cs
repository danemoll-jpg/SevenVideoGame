using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Seven
{
    /// <summary>
    /// On-screen controls legend in uGUI (rendered by the fight camera, so it shows up in off-screen captures).
    /// Shows Xbox controller glyphs by default and switches to the keyboard/mouse legend automatically when the last
    /// input came from keyboard or mouse. Monochrome: white glyphs on a dark translucent panel, no button colours.
    /// Layouts: the fight demo, and the saucer's two contexts (walking / seated). Optional centre-bottom prompt
    /// ("A Sit") for context actions.
    /// </summary>
    public class ControlsLegend : MonoBehaviour
    {
        public Camera Cam;
        public bool ShowXbox = true;
        public enum Layout { Fight, SaucerWalk, SaucerSeated, GalleryView, Chapter }
        public Layout Set = Layout.Fight;
        bool _saucer;
        [System.NonSerialized] public bool AutoSwitch = true;   // check runs force a mode

        enum G { Face, Bumper, Trigger, DPadUp, DPadDown, Stick, StickClick, Menu, View, Key, WideKey, Mouse }
        struct Row { public (G g, string t)[] Glyphs; public string Label; public Row(string label, params (G, string)[] glyphs) { Label = label; Glyphs = glyphs; } }

        static readonly Row[] XboxRows =
        {
            new Row("jab", (G.Face, "X")),
            new Row("jab-cross", (G.Face, "Y")),
            new Row("hook", (G.Bumper, "RB")),
            new Row("roundhouse", (G.Face, "B")),
            new Row("mma kick", (G.Face, "A")),
            new Row("block (hold)", (G.Trigger, "LT")),
            new Row("dodge", (G.Bumper, "LB")),
            new Row("finisher", (G.Trigger, "RT")),
            new Row("telekinesis: tap push · hold lift", (G.DPadUp, "")),
            new Row("move", (G.Stick, "L")),
            new Row("orbit camera", (G.Stick, "R")),
            new Row("re-center camera", (G.StickClick, "R3")),
            new Row("reset", (G.DPadDown, "")),
            new Row("demo menu", (G.Menu, "")),
            new Row("fullscreen on/off", (G.View, "")),
        };

        static readonly Row[] KeyRows =
        {
            new Row("jab", (G.Key, "1")),
            new Row("jab-cross", (G.Key, "2")),
            new Row("hook", (G.Key, "3")),
            new Row("roundhouse", (G.Key, "4")),
            new Row("mma kick", (G.Key, "5")),
            new Row("block (hold)", (G.WideKey, "Space")),
            new Row("dodge", (G.WideKey, "Shift")),
            new Row("finisher", (G.Key, "F")),
            new Row("telekinesis: tap push · hold lift", (G.Key, "T")),
            new Row("move", (G.Key, "W"), (G.Key, "A"), (G.Key, "S"), (G.Key, "D")),
            new Row("orbit camera (hold + drag)", (G.Mouse, "R")),
            new Row("re-center camera", (G.Key, "C"), (G.Mouse, "M")),
            new Row("reset", (G.WideKey, "Bksp")),
            new Row("demo menu", (G.WideKey, "Esc")),
            new Row("fullscreen on/off", (G.WideKey, "F11")),
        };

        // Saucer: walking around the ship.
        static readonly Row[] SaucerWalkXbox =
        {
            new Row("move", (G.Stick, "L")),
            new Row("orbit camera", (G.Stick, "R")),
            new Row("re-center camera", (G.StickClick, "R3")),
            new Row("sit (near the chair)", (G.Face, "A")),
            new Row("view art (near the TV)", (G.Face, "A")),
            new Row("demo menu", (G.Menu, "")),
            new Row("fullscreen on/off", (G.View, "")),
        };
        static readonly Row[] SaucerWalkKeys =
        {
            new Row("move", (G.Key, "W"), (G.Key, "A"), (G.Key, "S"), (G.Key, "D")),
            new Row("orbit camera (hold + drag)", (G.Mouse, "R")),
            new Row("re-center camera", (G.Key, "C"), (G.Mouse, "M")),
            new Row("sit (near the chair)", (G.Key, "E")),
            new Row("view art (near the TV)", (G.Key, "E")),
            new Row("demo menu", (G.WideKey, "Esc")),
            new Row("fullscreen on/off", (G.WideKey, "F11")),
        };
        // Saucer: seated in the chair.
        static readonly Row[] SaucerSeatedXbox =
        {
            new Row("stand up", (G.Face, "B")),
            new Row("reveal / return the ship", (G.Face, "Y")),
            new Row("orbit camera", (G.Stick, "R")),
            new Row("re-center camera", (G.StickClick, "R3")),
            new Row("demo menu", (G.Menu, "")),
            new Row("fullscreen on/off", (G.View, "")),
        };
        static readonly Row[] SaucerSeatedKeys =
        {
            new Row("stand up", (G.Key, "Q")),
            new Row("reveal / return the ship", (G.Key, "R")),
            new Row("orbit camera (hold + drag)", (G.Mouse, "R")),
            new Row("re-center camera", (G.Key, "C"), (G.Mouse, "M")),
            new Row("demo menu", (G.WideKey, "Esc")),
            new Row("fullscreen on/off", (G.WideKey, "F11")),
        };

        // Round 5: viewing Kyle's art on the TV.
        static readonly Row[] GalleryXbox =
        {
            new Row("previous / next (or d-pad, left stick)", (G.Bumper, "LB"), (G.Bumper, "RB")),
            new Row("full view on/off", (G.Face, "Y")),
            new Row("leave the TV", (G.Face, "B")),
            new Row("demo menu", (G.Menu, "")),
            new Row("fullscreen on/off", (G.View, "")),
        };
        static readonly Row[] GalleryKeys =
        {
            new Row("previous / next", (G.Key, "\u2190"), (G.Key, "\u2192")),
            new Row("full view on/off", (G.Key, "F")),
            new Row("leave the TV", (G.WideKey, "Esc")),
            new Row("fullscreen on/off", (G.WideKey, "F11")),
        };

        // Chapter 1 (Moon level).
        static readonly Row[] ChapterXbox =
        {
            new Row("move", (G.Stick, "L")),
            new Row("run (hold)", (G.Bumper, "LB")),
            new Row("orbit camera", (G.Stick, "R")),
            new Row("re-center camera", (G.StickClick, "R3")),
            new Row("interact · skip line", (G.Face, "A")),
            new Row("telekinesis (when shown)", (G.Face, "X")),
            new Row("restart from checkpoint", (G.DPadDown, "")),
            new Row("menu", (G.Menu, "")),
        };
        static readonly Row[] ChapterKeys =
        {
            new Row("move", (G.Key, "W"), (G.Key, "A"), (G.Key, "S"), (G.Key, "D")),
            new Row("run (hold)", (G.WideKey, "Shift")),
            new Row("orbit camera (hold + drag)", (G.Mouse, "R")),
            new Row("re-center camera", (G.Key, "C"), (G.Mouse, "M")),
            new Row("interact · skip line (Enter)", (G.Key, "E")),
            new Row("telekinesis (when shown)", (G.Key, "T")),
            new Row("restart from checkpoint", (G.WideKey, "Bksp")),
            new Row("menu", (G.WideKey, "Esc")),
        };

        readonly Dictionary<(Layout, bool), GameObject> _panels = new Dictionary<(Layout, bool), GameObject>();
        GameObject _promptX, _promptK, _tkX, _tkK;
        Text _promptTextX, _promptTextK, _tkTextX, _tkTextK;
        bool _promptOn, _tkOn;

        GameObject _xboxPanel, _keyPanel;
        Font _font;
        readonly Dictionary<string, UnityEngine.Sprite> _sprites = new Dictionary<string, UnityEngine.Sprite>();
        static readonly Color Ink = new Color(0.93f, 0.93f, 0.93f, 1f);

        void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("LegendCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Cam;
            canvas.planeDistance = Cam != null ? Mathf.Max(Cam.nearClipPlane * 2f, 0.12f) : 1f;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1f;

            _saucer = Set != Layout.Fight;
            if (!_saucer)
            {
                _panels[(Layout.Fight, true)] = _xboxPanel = BuildPanel(canvasGo.transform, "XboxLegend", XboxRows);
                _panels[(Layout.Fight, false)] = _keyPanel = BuildPanel(canvasGo.transform, "KeyLegend", KeyRows);
            }
            else
            {
                _panels[(Layout.SaucerWalk, true)] = BuildPanel(canvasGo.transform, "WalkXbox", SaucerWalkXbox);
                _panels[(Layout.SaucerWalk, false)] = BuildPanel(canvasGo.transform, "WalkKeys", SaucerWalkKeys);
                _panels[(Layout.SaucerSeated, true)] = BuildPanel(canvasGo.transform, "SeatedXbox", SaucerSeatedXbox);
                _panels[(Layout.SaucerSeated, false)] = BuildPanel(canvasGo.transform, "SeatedKeys", SaucerSeatedKeys);
                _panels[(Layout.GalleryView, true)] = BuildPanel(canvasGo.transform, "GalleryXbox", GalleryXbox);
                _panels[(Layout.GalleryView, false)] = BuildPanel(canvasGo.transform, "GalleryKeys", GalleryKeys);
                _panels[(Layout.Chapter, true)] = BuildPanel(canvasGo.transform, "ChapterXbox", ChapterXbox);
                _panels[(Layout.Chapter, false)] = BuildPanel(canvasGo.transform, "ChapterKeys", ChapterKeys);
                _promptX = BuildPrompt(canvasGo.transform, "PromptXbox", G.Face, "A", out _promptTextX);
                _promptK = BuildPrompt(canvasGo.transform, "PromptKeys", G.Key, "E", out _promptTextK);
                _tkX = BuildPrompt(canvasGo.transform, "PromptTkXbox", G.Face, "X", out _tkTextX);
                _tkK = BuildPrompt(canvasGo.transform, "PromptTkKeys", G.Key, "T", out _tkTextK);
            }
            Apply();
        }

        public void SetMode(bool xbox) { ShowXbox = xbox; Apply(); }
        public void SetLayout(Layout l) { Set = l; Apply(); }

        /// <summary>Centre-bottom context prompt (saucer: "Sit" near the chair). Glyph: A / E.</summary>
        public void Prompt(string label)
        {
            bool on = !string.IsNullOrEmpty(label);
            if (on == _promptOn && (!on || _promptTextX == null || _promptTextX.text == label)) return;
            _promptOn = on;
            if (_promptTextX != null && _promptOn) { _promptTextX.text = label; _promptTextK.text = label; Fit(_promptX, label); Fit(_promptK, label); }
            Apply();
        }

        static void Fit(GameObject panel, string label)
        {
            if (panel != null) ((RectTransform)panel.transform).sizeDelta = new Vector2(Mathf.Max(170f, 70f + label.Length * 10.5f), 40f);
        }

        /// <summary>Centre-bottom telekinesis prompt (Chapter 1). Glyph: X / T.</summary>
        public void PromptTk(string label)
        {
            bool on = !string.IsNullOrEmpty(label);
            if (on == _tkOn && (!on || _tkTextX == null || _tkTextX.text == label)) return;
            _tkOn = on;
            if (_tkTextX != null && on) { _tkTextX.text = label; _tkTextK.text = label; Fit(_tkX, label); Fit(_tkK, label); }
            Apply();
        }

        void Apply()
        {
            if (_panels.Count == 0) return;
            foreach (var kv in _panels) kv.Value.SetActive(kv.Key.Item1 == Set && kv.Key.Item2 == ShowXbox);
            if (_promptX != null) { _promptX.SetActive(_promptOn && ShowXbox); _promptK.SetActive(_promptOn && !ShowXbox); }
            if (_tkX != null) { _tkX.SetActive(_tkOn && ShowXbox); _tkK.SetActive(_tkOn && !ShowXbox); }
        }

        GameObject BuildPrompt(Transform parent, string name, G g, string glyph, out Text label)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 90f);
            rt.sizeDelta = new Vector2(170f, 40f);
            panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            AddGlyph(panel.transform, g, glyph, new Vector2(14f, 6f), new Vector2(28f, 28f));
            label = AddText(panel.transform, "", new Vector2(52f, 4f), new Vector2(110f, 32f), 20, TextAnchor.MiddleLeft);
            return panel;
        }

        void Update()
        {
            if (!AutoSwitch) return;
            bool pad = false, keys = false;
            var gp = Gamepad.current;
            if (gp != null)
            {
                foreach (var c in gp.allControls)
                    if (c is UnityEngine.InputSystem.Controls.ButtonControl b && b.wasPressedThisFrame) { pad = true; break; }
                if (gp.leftStick.ReadValue().sqrMagnitude > 0.1f || gp.rightStick.ReadValue().sqrMagnitude > 0.1f) pad = true;
            }
            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame) keys = true;
            var ms = Mouse.current;
            if (ms != null && (ms.leftButton.wasPressedThisFrame || ms.rightButton.wasPressedThisFrame || ms.middleButton.wasPressedThisFrame
                               || ms.delta.ReadValue().sqrMagnitude > 9f)) keys = true;
            if (pad && !ShowXbox) SetMode(true);
            else if (keys && !pad && ShowXbox) SetMode(false);
        }

        // ------------------------------------------------------------------------------------ layout
        const float RowH = 26f, GlyphH = 22f, Pad = 10f;

        GameObject BuildPanel(Transform parent, string name, Row[] rows)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(16f, 16f);
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.8f);

            float glyphCol = 0f;
            foreach (var r in rows)
            {
                float w = 0f;
                foreach (var g in r.Glyphs) w += GlyphWidth(g.g) + 4f;
                glyphCol = Mathf.Max(glyphCol, w);
            }
            float labelW = 250f;
            rt.sizeDelta = new Vector2(Pad * 2 + glyphCol + 8f + labelW, Pad * 2 + rows.Length * RowH);

            for (int i = 0; i < rows.Length; i++)
            {
                float y = rt.sizeDelta.y - Pad - (i + 1) * RowH + (RowH - GlyphH) * 0.5f;
                float x = Pad;
                foreach (var g in rows[i].Glyphs)
                {
                    float gw = GlyphWidth(g.g);
                    AddGlyph(panel.transform, g.g, g.t, new Vector2(x, y), new Vector2(gw, GlyphH));
                    x += gw + 4f;
                }
                AddText(panel.transform, rows[i].Label, new Vector2(Pad + glyphCol + 8f, y - 2f), new Vector2(labelW, GlyphH + 4f), 15, TextAnchor.MiddleLeft);
            }
            return panel;
        }

        static float GlyphWidth(G g) => g switch
        {
            G.Bumper => 34f, G.Trigger => 30f, G.WideKey => 50f, G.Menu => 30f, G.View => 30f, _ => 22f,
        };

        Text AddText(Transform parent, string s, Vector2 pos, Vector2 size, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = fontSize; t.text = s; t.color = Ink; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.9f); sh.effectDistance = new Vector2(1f, -1f);
            return t;
        }

        void AddGlyph(Transform parent, G g, string label, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Glyph_" + g + label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = GlyphSprite(g, Mathf.RoundToInt(size.x * 3), Mathf.RoundToInt(size.y * 3));
            img.color = Ink;
            if (!string.IsNullOrEmpty(label))
            {
                int fs = label.Length >= 3 ? 10 : label.Length == 2 ? 11 : 13;
                if (g == G.Mouse) return;   // mouse glyph draws the pressed button itself
                var t = AddText(go.transform, label, Vector2.zero, size, fs, TextAnchor.MiddleCenter);
                t.fontStyle = FontStyle.Bold;
                if (g == G.Trigger) ((RectTransform)t.transform).anchoredPosition = new Vector2(0f, -1.5f);
            }
            if (g == G.Mouse)
            {
                // Mark which button: R (right), M (middle).
                var mark = new GameObject("Btn", typeof(RectTransform));
                mark.transform.SetParent(go.transform, false);
                var mrt = (RectTransform)mark.transform;
                mrt.anchorMin = mrt.anchorMax = mrt.pivot = Vector2.zero;
                mrt.sizeDelta = label == "M" ? new Vector2(3f, 6f) : new Vector2(7f, 8f);
                mrt.anchoredPosition = label == "M" ? new Vector2(size.x * 0.5f - 1.5f, size.y - 10f) : new Vector2(size.x * 0.5f + 1f, size.y - 11f);
                mark.AddComponent<Image>().color = Ink;
            }
        }

        // ------------------------------------------------------------------------------- glyph art
        UnityEngine.Sprite GlyphSprite(G g, int w, int h)
        {
            string key = g + ":" + w + "x" + h;
            if (_sprites.TryGetValue(key, out var s)) return s;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            float line = 2.2f;   // stroke width in texels (sprites are drawn at 3x)
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float a = Shape(g, fx, fy, w, h, line);
                    byte v = (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f);
                    px[y * w + x] = new Color32(255, 255, 255, v);
                }
            tex.SetPixels32(px);
            tex.Apply();
            s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            _sprites[key] = s;
            return s;
        }

        // Coverage helpers (signed distance -> antialiased alpha).
        static float Fill(float d) => Mathf.Clamp01(0.5f - d);
        static float Stroke(float d, float w) => Mathf.Clamp01(w * 0.5f + 0.5f - Mathf.Abs(d));
        static float RoundRect(float x, float y, float cx, float cy, float hw, float hh, float r)
        {
            float qx = Mathf.Abs(x - cx) - hw + r, qy = Mathf.Abs(y - cy) - hh + r;
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }
        static float Circle(float x, float y, float cx, float cy, float r) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

        static float Shape(G g, float x, float y, int w, int h, float line)
        {
            float cx = w * 0.5f, cy = h * 0.5f, m = line + 1f;
            switch (g)
            {
                case G.Face:
                    return Stroke(Circle(x, y, cx, cy, Mathf.Min(cx, cy) - m), line);
                case G.Stick:
                {
                    float R = Mathf.Min(cx, cy) - m;
                    return Mathf.Max(Stroke(Circle(x, y, cx, cy, R), line), 0.28f * Fill(Circle(x, y, cx, cy, R * 0.62f)));
                }
                case G.StickClick:
                {
                    float R = Mathf.Min(cx, cy) - m;
                    return Mathf.Max(Stroke(Circle(x, y, cx, cy, R), line), Stroke(Circle(x, y, cx, cy, R * 0.72f), line * 0.7f));
                }
                case G.Bumper:
                    return Stroke(RoundRect(x, y, cx, cy, cx - m, cy - m - h * 0.08f, h * 0.22f), line);
                case G.Trigger:
                {
                    // Rounded top, flat bottom.
                    float d = RoundRect(x, y, cx, cy - h * 0.06f, cx - m - w * 0.06f, cy - m, w * 0.32f);
                    return Stroke(d, line);
                }
                case G.DPadDown:
                {
                    float arm = w * 0.17f, len = cx - m;
                    float v = RoundRect(x, y, cx, cy, arm, len, 2f);
                    float hz = RoundRect(x, y, cx, cy, len, arm, 2f);
                    float down = RoundRect(x, y, cx, cy - len * 0.55f, arm - line, len * 0.42f, 1.5f);   // filled "down" arm
                    return Mathf.Max(Stroke(Mathf.Min(v, hz), line), Fill(down));
                }
                case G.DPadUp:
                {
                    float arm = w * 0.17f, len = cx - m;
                    float v = RoundRect(x, y, cx, cy, arm, len, 2f);
                    float hz = RoundRect(x, y, cx, cy, len, arm, 2f);
                    float cross = Mathf.Min(v, hz);
                    float up = RoundRect(x, y, cx, cy + len * 0.55f, arm - line, len * 0.42f, 1.5f);   // filled "up" arm
                    return Mathf.Max(Stroke(cross, line), Fill(up));
                }
                case G.Menu:
                {
                    float frame = Stroke(RoundRect(x, y, cx, cy, cx - m, cy - m - h * 0.08f, h * 0.2f), line);
                    float bars = 0f;
                    for (int i = -1; i <= 1; i++) bars = Mathf.Max(bars, Fill(RoundRect(x, y, cx, cy + i * h * 0.17f, w * 0.2f, line * 0.45f, 0.5f)));
                    return Mathf.Max(frame, bars);
                }
                case G.View:
                {
                    // Xbox View button: two overlapping windows inside the button frame.
                    float frame = Stroke(RoundRect(x, y, cx, cy, cx - m, cy - m - h * 0.08f, h * 0.2f), line);
                    float bw = w * 0.16f, bh = h * 0.14f, off = w * 0.06f;
                    float back = Stroke(RoundRect(x, y, cx + off, cy + off, bw, bh, 1f), line * 0.7f);
                    float front = Fill(RoundRect(x, y, cx - off, cy - off, bw, bh, 1f));
                    return Mathf.Max(frame, Mathf.Max(back, front));
                }
                case G.Key:
                case G.WideKey:
                    return Stroke(RoundRect(x, y, cx, cy, cx - m, cy - m, h * 0.18f), line);
                case G.Mouse:
                {
                    float body = Stroke(RoundRect(x, y, cx, cy, w * 0.3f, cy - m, w * 0.28f), line);
                    float split = (y > h * 0.55f && Mathf.Abs(x - cx) < line * 0.5f) ? 1f : 0f;
                    float mid = Fill(RoundRect(x, y, cx - w * 0.2f, h * 0.55f, w * 0.2f, line * 0.4f, 0.3f)) * (x < cx ? 1f : 0f);
                    float mid2 = Fill(RoundRect(x, y, cx + w * 0.2f, h * 0.55f, w * 0.2f, line * 0.4f, 0.3f)) * (x > cx ? 1f : 0f);
                    return Mathf.Max(body, Mathf.Max(split, Mathf.Max(mid, mid2)));
                }
            }
            return 0f;
        }
    }
}
