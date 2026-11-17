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
        public string SubtitleText => _subText != null ? _subText.text : "";

        void Awake() { if (Cam != null) Build(); }

        public void Build()
        {
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
            _subPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
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
            var lbl = MakeText(objGo.transform, "OBJECTIVE", 12, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(560f, 18f));
            lbl.color = new Color(0.62f, 0.62f, 0.62f);
            ((RectTransform)lbl.transform).anchorMin = ((RectTransform)lbl.transform).anchorMax = ((RectTransform)lbl.transform).pivot = new Vector2(0f, 1f);
            _objective = MakeText(objGo.transform, "", 20, TextAnchor.UpperLeft, new Vector2(0f, -16f), new Vector2(560f, 30f));
            ((RectTransform)_objective.transform).anchorMin = ((RectTransform)_objective.transform).anchorMax = ((RectTransform)_objective.transform).pivot = new Vector2(0f, 1f);
            _objGroup.alpha = 0f;

            // Small hint line under the objective (e.g. "Spotted - back to the entrance").
            _hint = MakeText(root, "", 18, TextAnchor.MiddleCenter, new Vector2(0f, 120f), new Vector2(900f, 30f));
            var hrt = (RectTransform)_hint.transform; hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 0.5f);

            // Title card + fade.
            var fadeGo = new GameObject("Fade", typeof(RectTransform));
            fadeGo.transform.SetParent(root, false);
            var frt = (RectTransform)fadeGo.transform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = frt.offsetMax = Vector2.zero;
            _fade = fadeGo.AddComponent<Image>();
            _fade.color = new Color(0f, 0f, 0f, 0f);
            _fade.raycastTarget = false;
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
            return t;
        }

        public void ShowSubtitle(string speaker, string text, bool telepathic, bool far)
        {
            Build();
            _subPanel.SetActive(true);
            _subName.text = telepathic ? speaker.ToUpperInvariant() + "  (thought)" : speaker.ToUpperInvariant();
            _subText.text = text;
            _subText.fontStyle = telepathic ? FontStyle.Italic : FontStyle.Normal;
            _subText.color = far ? new Color(1f, 1f, 1f, 0.55f) : Color.white;
        }

        public void HideSubtitle() { if (_subPanel != null) _subPanel.SetActive(false); }

        public void SetObjective(string text)
        {
            Build();
            Objective = text;
            _objective.text = text ?? "";
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
            for (float t = 0f; t < seconds; t += Time.deltaTime) yield return null;
            _hint.text = "";
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
    }
}
