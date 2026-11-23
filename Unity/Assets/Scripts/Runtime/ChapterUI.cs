using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Seven
{
    /// <summary>
    /// Chapter overlay in uGUI, drawn by the main camera (so it shows in off-screen captures). Strictly monochrome:
    /// subtitles (speaker name + line, white on a soft dark band; the cat's telepathic lines in italics), the objective
    /// line (top left), a full-screen fade (black, or white for the vision wash) and a centred title card.
    /// </summary>
    public class ChapterUI : MonoBehaviour
    {
        public Camera Cam;
        Font _font;
        GameObject _subPanel;
        Text _subName, _subText, _objective, _title, _hint;
        Image _fade;
        CanvasGroup _objGroup;
        public string Objective { get; private set; }
        public float FadeAlpha => _fade != null ? _fade.color.a : 0f;
        public bool SubtitleVisible => _subPanel != null && _subPanel.activeSelf;
        /// <summary>QF25 (2): the subtitle panel (its position on screen is measured in check runs).</summary>
        public RectTransform SubtitleRect => _subPanel != null ? _subPanel.transform as RectTransform : null;
        public string SubtitleText => _subText != null ? _subText.text : "";

        void Awake() { if (Cam != null) Build(); }

        /// <summary>QF25 (2): the chapter UI of the scene in play (the check runs measure its subtitle).</summary>
        public static ChapterUI Live;
        void OnEnable() { Live = this; }
        void Start() { UiOverlay.EnsureNow(); }                 // QF25 (2): onto the UI camera at the world origin before the first frame
        public void Build()
        {
            Live = this;
            if (_font != null) return;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("ChapterCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Cam;
            canvas.planeDistance = Cam != null ? Mathf.Max(Cam.nearClipPlane * 2f, 0.11f) : 1f;
            canvas.sortingOrder = 200;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1f;
            var root = canvasGo.transform;

            // Subtitles: bottom centre, above the interaction prompt.
            _subPanel = new GameObject("Subtitle", typeof(RectTransform));
            _subPanel.transform.SetParent(root, false);
            var rt = (RectTransform)_subPanel.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 140f); rt.sizeDelta = new Vector2(860f, 64f);
            { var im = _subPanel.AddComponent<Image>(); im.sprite = SoftSprite(); im.type = Image.Type.Sliced; im.color = new Color(0f, 0f, 0f, 0.6f); im.raycastTarget = false; }
            _subName = MakeText(_subPanel.transform, "", 17, TextAnchor.UpperCenter, new Vector2(0f, 20f), new Vector2(840f, 24f));
            _subName.color = new Color(0.72f, 0.72f, 0.72f);
            _subName.fontStyle = FontStyle.Bold;
            _subText = MakeText(_subPanel.transform, "", 24, TextAnchor.MiddleCenter, new Vector2(0f, -8f), new Vector2(840f, 40f));
            _subPanel.SetActive(false);

            // Objective: top left.
            var objGo = new GameObject("Objective", typeof(RectTransform));
            objGo.transform.SetParent(root, false);
            var ort = (RectTransform)objGo.transform;
            ort.anchorMin = ort.anchorMax = ort.pivot = new Vector2(0f, 1f);
            ort.anchoredPosition = new Vector2(20f, -18f); ort.sizeDelta = new Vector2(560f, 50f);
            _objGroup = objGo.AddComponent<CanvasGroup>();
            // QF18 (5): a soft dark translucent panel behind it (white text washed out against the bright fog / sky)
            _objPanel = Panel(objGo.transform, new Vector2(0f, 1f), new Vector2(-12f, 10f), new Vector2(580f, 70f), 0.5f);
            var lbl = MakeText(objGo.transform, "OBJECTIVE", 12, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(560f, 18f));
            lbl.color = new Color(0.62f, 0.62f, 0.62f);
            ((RectTransform)lbl.transform).anchorMin = ((RectTransform)lbl.transform).anchorMax = ((RectTransform)lbl.transform).pivot = new Vector2(0f, 1f);
            _objective = MakeText(objGo.transform, "", 20, TextAnchor.UpperLeft, new Vector2(0f, -16f), new Vector2(560f, 30f));
            ((RectTransform)_objective.transform).anchorMin = ((RectTransform)_objective.transform).anchorMax = ((RectTransform)_objective.transform).pivot = new Vector2(0f, 1f);
            _objGroup.alpha = 0f;

            // Small hint line under the objective (e.g. "Spotted - back to the entrance").
            // QF23 (17): hints are small and OUT of the centre - bottom of the screen, above the subtitles (never a help box
            // over the middle of the view)
            _hintPanel = Panel(root, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(400f, 40f), 0.5f, true);
            { var prt = (RectTransform)_hintPanel.transform; prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f); prt.pivot = new Vector2(0.5f, 0.5f); prt.anchoredPosition = new Vector2(0f, 232f); }
            _hint = MakeText(root, "", 18, TextAnchor.MiddleCenter, new Vector2(0f, 232f), new Vector2(900f, 30f));
            var hrt = (RectTransform)_hint.transform; hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0f); hrt.pivot = new Vector2(0.5f, 0.5f); hrt.anchoredPosition = new Vector2(0f, 232f);
            _hintPanel.gameObject.SetActive(false);

            // Title card + fade.
            var fadeGo = new GameObject("Fade", typeof(RectTransform));
            fadeGo.transform.SetParent(root, false);
            var frt = (RectTransform)fadeGo.transform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = frt.offsetMax = Vector2.zero;
            _fade = fadeGo.AddComponent<Image>();
            _fade.color = new Color(0f, 0f, 0f, 0f);
            _fade.raycastTarget = false;
            // Round 9: full-screen video layer (the first vision) above the white wash; sides cropped to the screen aspect
            var vgo = new GameObject("Video", typeof(RectTransform));
            vgo.transform.SetParent(root, false);
            var vrt = (RectTransform)vgo.transform; vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one; vrt.offsetMin = vrt.offsetMax = Vector2.zero;
            _video = vgo.AddComponent<RawImage>(); _video.raycastTarget = false; _video.color = new Color(1f, 1f, 1f, 0f); _video.enabled = false;
            _title = MakeText(root, "", 40, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1100f, 120f));
            var trt = (RectTransform)_title.transform; trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0.5f, 0.5f);
            _title.color = new Color(1f, 1f, 1f, 0f);
        }

        Text MakeText(Transform parent, string s, int size, TextAnchor anchor, Vector2 pos, Vector2 box)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos; rt.sizeDelta = box;
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = size; t.text = s; t.color = Color.white; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.9f); sh.effectDistance = new Vector2(1.5f, -1.5f);
            var ol = go.AddComponent<Outline>(); ol.effectColor = new Color(0f, 0f, 0f, 0.55f); ol.effectDistance = new Vector2(1f, -1f);   // QF18 (5)
            return t;
        }

        // ---- QF18 (5): the soft panel - a rounded rectangle with feathered edges (generated once), sliced to any size
        static Sprite _soft;
        public static Sprite SoftSprite()
        {
            if (_soft != null) return _soft;
            const int N = 64; const float R = 22f, F = 10f;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, R, N - R), cy = Mathf.Clamp(y + 0.5f, R, N - R);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01((R - d) / F);
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(255f * a * a * (3f - 2f * a)));
                }
            tex.SetPixels32(px); tex.Apply();
            _soft = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
            return _soft;
        }
        Image Panel(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, float alpha, bool centred = false)
        {
            var go = new GameObject("Panel", typeof(RectTransform)); go.transform.SetParent(parent, false); go.transform.SetAsFirstSibling();
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = centred ? new Vector2(0.5f, 0.5f) : anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var im = go.AddComponent<Image>(); im.sprite = SoftSprite(); im.type = Image.Type.Sliced; im.color = new Color(0f, 0f, 0f, alpha); im.raycastTarget = false;
            return im;
        }
        Image _objPanel, _hintPanel, _markPanel;

        public void ShowSubtitle(string speaker, string text, bool telepathic, bool far)
        {
            Build();
            _subPanel.SetActive(true);
            _subName.text = speaker.ToUpperInvariant(); // QF3: no "(thought)" — 7 and the cat both speak telepathically
            _subText.text = text;
            _subText.fontStyle = telepathic ? FontStyle.Italic : FontStyle.Normal;
            _subText.color = far ? new Color(1f, 1f, 1f, 0.55f) : Color.white;
        }

        public void HideSubtitle() { if (_subPanel != null) _subPanel.SetActive(false); }

        // ---- QF13 (9): the next point is always findable - an on-screen marker. On screen: a small diamond on the guide
        // light with its distance and an up / down cue (she swims in 3D); off screen, behind her or lost in the fog: an arrow
        // at the screen's edge pointing the way, with the same distance and cue. Monochrome (white with a dark shadow).
        Text _mark, _markInfo; Vector3? _markAt;
        /// <summary>Check runs: the marker's state this frame ("on screen" / "edge arrow" / "off").</summary>
        public string MarkerState { get; private set; } = "off";
        public Vector2 MarkerScreen { get; private set; }
        public void SetMarker(Vector3? world) { Build(); _markAt = world; if (_mark == null) BuildMarker(); }
        void BuildMarker()
        {
            var root = _hint.transform.parent;
            _mark = MakeText(root, "", 30, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(60f, 60f));
            var rt = (RectTransform)_mark.transform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            _markPanel = Panel(root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 26f), 0.45f, true);
            _markInfo = MakeText(root, "", 16, TextAnchor.UpperCenter, Vector2.zero, new Vector2(140f, 40f));
            var rt2 = (RectTransform)_markInfo.transform; rt2.anchorMin = rt2.anchorMax = new Vector2(0.5f, 0.5f); rt2.pivot = new Vector2(0.5f, 1f);
        }
        void LateUpdate()
        {
            if (_mark == null) return;
            var cam = Cam != null ? Cam : Camera.main;
            bool fading = _fade != null && _fade.color.a > 0.5f;
            if (!_markAt.HasValue || cam == null || fading || (_video != null && _video.enabled && _video.color.a > 0.1f)) { _mark.text = ""; _markInfo.text = ""; MarkerState = "off"; if (_markPanel != null) _markPanel.gameObject.SetActive(false); return; }
            var w = _markAt.Value;
            var vp = cam.WorldToViewportPoint(w);
            var canvasRt = (RectTransform)_mark.transform.parent;
            var size = canvasRt.rect.size;
            float dist = Vector3.Distance(cam.transform.position, w);
            var target = cam.transform.parent != null ? cam.transform.position : cam.transform.position;
            float dy = w.y - (cam.transform.position.y - 1.5f);
            string cue = dy > 4f ? "  ↑ above" : dy < -4f ? "  ↓ below" : "";
            string info = $"{dist:0} m{cue}";
            bool onScreen = vp.z > 0f && vp.x > 0.06f && vp.x < 0.94f && vp.y > 0.08f && vp.y < 0.92f;
            Vector2 pos;
            if (onScreen)
            {
                pos = new Vector2((vp.x - 0.5f) * size.x, (vp.y - 0.5f) * size.y);
                _mark.text = "◊"; _mark.transform.localRotation = Quaternion.identity; _mark.fontSize = 30;
                MarkerState = "on screen";
            }
            else
            {
                var d = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
                if (vp.z < 0f) d = -d;                                                    // behind: mirror
                if (d.sqrMagnitude < 1e-6f) d = Vector2.down;
                d.Normalize();
                float hx = size.x * 0.5f - 46f, hy = size.y * 0.5f - 52f;
                float k = Mathf.Min(hx / Mathf.Max(1e-4f, Mathf.Abs(d.x)), hy / Mathf.Max(1e-4f, Mathf.Abs(d.y)));
                pos = d * k;
                _mark.text = "▲"; _mark.fontSize = 34;
                _mark.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
                MarkerState = "edge arrow";
            }
            ((RectTransform)_mark.transform).anchoredPosition = pos;
            ((RectTransform)_markInfo.transform).anchoredPosition = pos + new Vector2(0f, -22f);
            _markInfo.text = info;
            if (_markPanel != null) { _markPanel.gameObject.SetActive(true); var mrt = (RectTransform)_markPanel.transform; mrt.anchoredPosition = pos + new Vector2(0f, -31f); mrt.sizeDelta = new Vector2(_markInfo.preferredWidth + 22f, 26f); }
            MarkerScreen = pos;
        }

        public void SetObjective(string text)
        {
            Build();
            Objective = text;
            _objective.text = text ?? "";
            if (_objPanel != null) { float h = Mathf.Max(30f, _objective.preferredHeight); float w = Mathf.Min(560f, Mathf.Max(140f, _objective.preferredWidth)); ((RectTransform)_objPanel.transform).sizeDelta = new Vector2(w + 30f, h + 36f); }
            StopCoroutine(nameof(ObjFade));
            StartCoroutine(nameof(ObjFade), string.IsNullOrEmpty(text) ? 0f : 1f);
        }
        IEnumerator ObjFade(float to)
        {
            while (!Mathf.Approximately(_objGroup.alpha, to)) { _objGroup.alpha = Mathf.MoveTowards(_objGroup.alpha, to, Time.unscaledDeltaTime * 2.5f); yield return null; }
        }

        Coroutine _hintCo;
        public void Hint(string text, float seconds = 3f)
        {
            Build();
            if (_hintCo != null) StopCoroutine(_hintCo);
            _hintCo = StartCoroutine(HintRoutine(text, seconds));
        }
        IEnumerator HintRoutine(string text, float seconds)
        {
            _hint.text = text;
            if (_hintPanel != null) { _hintPanel.gameObject.SetActive(!string.IsNullOrEmpty(text)); ((RectTransform)_hintPanel.transform).sizeDelta = new Vector2(Mathf.Min(900f, _hint.preferredWidth) + 44f, 40f); }
            for (float t = 0f; t < seconds; t += Time.deltaTime) yield return null;
            _hint.text = ""; if (_hintPanel != null) _hintPanel.gameObject.SetActive(false);
        }

        /// <summary>Fade the overlay to a shade (0 = black, 1 = white) and opacity.</summary>
        public IEnumerator Fade(float alpha, float seconds, float shade = 0f)
        {
            Build();
            var from = _fade.color;
            var to = new Color(shade, shade, shade, alpha);
            if (from.a < 0.01f) from = new Color(shade, shade, shade, 0f);
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                _fade.color = Color.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
                yield return null;
            }
            _fade.color = to;
        }

        public void SetFade(float alpha, float shade = 0f) { Build(); _fade.color = new Color(shade, shade, shade, alpha); }

        RawImage _video;
        /// <summary>Round 19: the canvas root (the credits build their layers here, above everything else).</summary>
        public Transform CanvasRoot { get { Build(); return _video.transform.parent; } }
        public float VideoAlpha => _video != null && _video.enabled ? _video.color.a : 0f;
        /// <summary>Show a video texture full-screen: fills the height, crops the sides (no stretching).</summary>
        public void SetVideo(Texture tex, float srcAspect, float alpha)
        {
            Build();
            _video.texture = tex; _video.enabled = tex != null && alpha > 0.001f;
            float screen = Cam != null ? Cam.aspect : 16f / 9f;
            float w = Mathf.Min(1f, screen / Mathf.Max(0.1f, srcAspect)), h = Mathf.Min(1f, srcAspect / Mathf.Max(0.1f, screen));
            _video.uvRect = new Rect((1f - w) * 0.5f, (1f - h) * 0.5f, w, h);
            _video.color = new Color(1f, 1f, 1f, alpha);
            // QF27: the lines are read over a video (the meadow): the subtitle panel draws ABOVE the video layer (it was created first,
            // so the video covered every subtitle)
            if (_subPanel != null) _subPanel.transform.SetAsLastSibling();
        }

        public IEnumerator Title(string text, float fadeIn, float hold, float fadeOut)
        {
            Build();
            _title.text = text;
            for (float t = 0f; t < fadeIn; t += Time.deltaTime) { _title.color = new Color(1f, 1f, 1f, t / fadeIn); yield return null; }
            _title.color = Color.white;
            for (float t = 0f; t < hold; t += Time.deltaTime) yield return null;
            for (float t = 0f; t < fadeOut; t += Time.deltaTime) { _title.color = new Color(1f, 1f, 1f, 1f - t / fadeOut); yield return null; }
            _title.color = new Color(1f, 1f, 1f, 0f);
        }

        public void ShowTitle(string text, float alpha) { Build(); _title.text = text; _title.color = new Color(1f, 1f, 1f, alpha); }

        // ---- QF18 (19d): the boss's energy bar (top centre: his name, the bar draining with her hits, phase marks, a notch +
        // shake on each stun), her own state (a small bar of the hits she can still take, bottom left) and a white flash
        // when she is hit. Strictly white / grays.
        RectTransform _bossRoot, _bossFill, _herRoot, _herFill; Text _bossName, _herLabel; Image _flash, _bossNotch;
        readonly System.Collections.Generic.List<RectTransform> _marks = new System.Collections.Generic.List<RectTransform>();
        float _shakeUntil, _notchUntil, _flashT = -9f, _bossShown = -1f, _herShown = -1f;
        public bool BossBarVisible => _bossRoot != null && _bossRoot.gameObject.activeSelf;
        void BuildBars()
        {
            if (_bossRoot != null) return;
            Build();
            var root = _title.transform.parent;
            var b = new GameObject("BossBar", typeof(RectTransform)); b.transform.SetParent(root, false); _bossRoot = (RectTransform)b.transform;
            _bossRoot.anchorMin = _bossRoot.anchorMax = _bossRoot.pivot = new Vector2(0.5f, 1f); _bossRoot.anchoredPosition = new Vector2(0f, -22f); _bossRoot.sizeDelta = new Vector2(520f, 48f);
            Panel(_bossRoot, new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(560f, 64f), 0.5f, true);
            _bossName = MakeText(_bossRoot, "", 15, TextAnchor.UpperCenter, new Vector2(0f, 12f), new Vector2(500f, 20f)); _bossName.fontStyle = FontStyle.Bold;
            var bg = new GameObject("Back", typeof(RectTransform)); bg.transform.SetParent(_bossRoot, false); var bgr = (RectTransform)bg.transform; bgr.anchoredPosition = new Vector2(0f, -10f); bgr.sizeDelta = new Vector2(480f, 12f);
            bg.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.85f);
            var f = new GameObject("Fill", typeof(RectTransform)); f.transform.SetParent(bg.transform, false); _bossFill = (RectTransform)f.transform;
            _bossFill.anchorMin = new Vector2(0f, 0f); _bossFill.anchorMax = new Vector2(1f, 1f); _bossFill.pivot = new Vector2(0f, 0.5f); _bossFill.offsetMin = _bossFill.offsetMax = Vector2.zero;
            f.AddComponent<Image>().color = new Color(0.94f, 0.94f, 0.94f, 1f);
            var n = new GameObject("Notch", typeof(RectTransform)); n.transform.SetParent(bg.transform, false); var nr = (RectTransform)n.transform; nr.sizeDelta = new Vector2(480f, 20f);
            _bossNotch = n.AddComponent<Image>(); _bossNotch.color = new Color(1f, 1f, 1f, 0f); _bossNotch.raycastTarget = false;
            _bossRoot.gameObject.SetActive(false);
            var h = new GameObject("HerBar", typeof(RectTransform)); h.transform.SetParent(root, false); _herRoot = (RectTransform)h.transform;
            _herRoot.anchorMin = _herRoot.anchorMax = _herRoot.pivot = new Vector2(0f, 0f); _herRoot.anchoredPosition = new Vector2(24f, 26f); _herRoot.sizeDelta = new Vector2(220f, 30f);
            Panel(_herRoot, new Vector2(0f, 0f), new Vector2(-10f, -8f), new Vector2(240f, 46f), 0.5f);
            _herLabel = MakeText(_herRoot, "7", 14, TextAnchor.MiddleLeft, new Vector2(-92f, 0f), new Vector2(30f, 20f)); _herLabel.fontStyle = FontStyle.Bold;
            var hb = new GameObject("Back", typeof(RectTransform)); hb.transform.SetParent(_herRoot, false); var hbr = (RectTransform)hb.transform; hbr.anchoredPosition = new Vector2(14f, 0f); hbr.sizeDelta = new Vector2(170f, 9f);
            hb.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.85f);
            var hf = new GameObject("Fill", typeof(RectTransform)); hf.transform.SetParent(hb.transform, false); _herFill = (RectTransform)hf.transform;
            _herFill.anchorMin = Vector2.zero; _herFill.anchorMax = Vector2.one; _herFill.pivot = new Vector2(0f, 0.5f); _herFill.offsetMin = _herFill.offsetMax = Vector2.zero;
            hf.AddComponent<Image>().color = new Color(0.86f, 0.86f, 0.86f, 1f);
            _herRoot.gameObject.SetActive(false);
            var fl = new GameObject("HitFlash", typeof(RectTransform)); fl.transform.SetParent(root, false); var flr = (RectTransform)fl.transform; flr.anchorMin = Vector2.zero; flr.anchorMax = Vector2.one; flr.offsetMin = flr.offsetMax = Vector2.zero;
            _flash = fl.AddComponent<Image>(); _flash.color = new Color(1f, 1f, 1f, 0f); _flash.raycastTarget = false;
        }
        /// <summary>Show / update the boss bar: energy 0..1, the phase marks (energies where a phase ends).</summary>
        /// <summary>QF23 (19): the phase mark the fight is waiting at (an objective to do) - it pulses; -1 = none.</summary>
        [System.NonSerialized] public int PulseMark = -1;
        public void BossBar(string name, float energy, float[] marks, bool stunned)
        {
            BuildBars();
            _bossRoot.gameObject.SetActive(true);
            _bossName.text = (name ?? "").ToUpperInvariant() + (stunned ? "   ·   STUNNED" : "");
            energy = Mathf.Clamp01(energy);
            if (_bossShown >= 0f && energy < _bossShown - 0.001f) _notchUntil = Time.time + 0.25f;
            _bossShown = energy;
            _bossFill.anchorMax = new Vector2(energy, 1f);
            if (marks != null)
            {
                while (_marks.Count < marks.Length)
                {
                    var m = new GameObject("Mark", typeof(RectTransform)); m.transform.SetParent(_bossFill.parent, false);
                    var mr = (RectTransform)m.transform; mr.sizeDelta = new Vector2(2f, 18f); mr.anchorMin = mr.anchorMax = new Vector2(0f, 0.5f);
                    m.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.95f); _marks.Add(mr);
                }
                for (int i = 0; i < _marks.Count; i++)
                {
                    bool on = i < marks.Length && marks[i] > 0.001f && marks[i] < 0.999f; _marks[i].gameObject.SetActive(on);
                    if (on) { _marks[i].anchorMin = _marks[i].anchorMax = new Vector2(marks[i], 0.5f); _marks[i].anchoredPosition = Vector2.zero; }
                    // QF23 (19): the mark the fight is waiting at PULSES (white, wider) - never a silent stall
                    bool pulse = on && i == PulseMark; float k = 0.5f + 0.5f * Mathf.Sin(Time.time * 8f);
                    _marks[i].sizeDelta = pulse ? new Vector2(5f + 3f * k, 24f + 8f * k) : new Vector2(2f, 18f);
                    _marks[i].GetComponent<Image>().color = pulse ? new Color(1f, 1f, 1f, 0.6f + 0.4f * k) : new Color(0.05f, 0.05f, 0.05f, 0.95f);
                }
            }
            float shake = Time.time < _shakeUntil ? 4f : 0f;
            _bossRoot.anchoredPosition = new Vector2(Random.Range(-shake, shake), -22f + Random.Range(-shake, shake) * 0.5f);
            _bossNotch.color = new Color(1f, 1f, 1f, Time.time < _notchUntil ? 0.35f * (_notchUntil - Time.time) / 0.25f : 0f);
        }
        /// <summary>The stun: the bar shakes and flashes.</summary>
        public void BossStunned() { BuildBars(); _shakeUntil = Time.time + 0.45f; _notchUntil = Time.time + 0.4f; }
        public void HideBossBar() { if (_bossRoot != null) { _bossRoot.gameObject.SetActive(false); _herRoot.gameObject.SetActive(false); _flash.color = new Color(1f, 1f, 1f, 0f); _bossShown = -1f; _herShown = -1f; } }
        /// <summary>Her state: hits she can still take of max (a white flash when it drops).</summary>
        public void HerBar(int left, int max)
        {
            BuildBars();
            _herRoot.gameObject.SetActive(true);
            float k = max > 0 ? Mathf.Clamp01(left / (float)max) : 0f;
            if (_herShown >= 0f && k < _herShown - 0.001f) _flashT = Time.time;
            _herShown = k;
            _herFill.anchorMax = new Vector2(k, 1f);
            _flash.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - (Time.time - _flashT) / 0.35f) * 0.38f);
        }

    }
}
