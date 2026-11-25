using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Seven
{
    /// <summary>
    /// QF25 (2) — screen UI never moves with the camera, game-wide. Dan saw the subtitles shake while the saucer entered the
    /// mothership. Cause: every screen canvas was a Screen-Space-Camera canvas 0.11 m in front of the GAMEPLAY camera (or of a
    /// UI camera parented to it). During the iris approach that camera is ~4 km from the world origin (the passage stage),
    /// where a float's step is ~0.5 mm - and 0.5 mm at 0.11 m is ~4 pixels: the canvas quad's corners rounded to different
    /// steps every frame as the camera moved, so the text jittered (any far-out shot and any camera shake did the same, less).
    /// Fix at the root: ONE overlay camera, never parented to the gameplay camera, fixed at the world origin (full float
    /// precision), rendering only the UI layer through the plain renderer (no fog pass), stacked on whatever camera is the main
    /// camera; every Screen-Space-Camera canvas that pointed at the main camera is moved onto it (subtitles, objectives,
    /// prompts, hints, the pause menu, the menus). The gameplay camera can move, shake, change field of view or fly kilometres
    /// out - the UI stays exactly where it is on screen. Check runs log <c>SUBTITLE STILL (QF25 2)</c>; the build check
    /// <c>SUBTITLE STILL (QF25 2)</c> shakes a camera far out and measures the subtitle rect.
    /// </summary>
    [DefaultExecutionOrder(2000)]
    public class UiOverlay : MonoBehaviour
    {
        static UiOverlay _i;
        Camera _ui, _base; float _next;
        /// <summary>Every element of a moved canvas on the UI layer (re-done when elements are added).</summary>
        void Layer(Canvas c)
        {
            var all = c.GetComponentsInChildren<Transform>(true);
            foreach (var t in all) t.gameObject.layer = 5;
        }
        /// <summary>The plain renderer's index (no SevenHeightFog pass), written by the builder to Resources/UI/ui_renderer.</summary>
        public static int RendererIndex
        {
            get
            {
                if (_ri == -2) { var ta = Resources.Load<TextAsset>("UI/ui_renderer"); _ri = ta != null && int.TryParse(ta.text.Trim(), out int k) ? k : -1; }
                return _ri;
            }
        }
        static int _ri = -2;
        /// <summary>The UI camera (null before the first main camera).</summary>
        public static Camera Cam => _i != null ? _i._ui : null;
        public static bool Off;   // -noqf25fix: the 0.17.3 behaviour (canvases on the gameplay camera)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-noqf25fix") Off = true;
            if (Off || _i != null) return;
            var go = new GameObject("UiOverlay"); DontDestroyOnLoad(go); _i = go.AddComponent<UiOverlay>();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => { if (_i != null) _i._next = 0f; };
        }
        /// <summary>Attach now (a scene that builds its UI late calls this so no frame shows the old placement).</summary>
        public static void EnsureNow() { if (_i == null) Boot(); if (_i != null) _i.Ensure(); }

        void LateUpdate() { if (Time.unscaledTime >= _next) { _next = Time.unscaledTime + 0.25f; Ensure(); } }

        // ---- QF25 (2) check runs: where the subtitle panel lands on screen, every frame it is shown (after rendering)
        int _frames, _moving, _shaking; float _jump; Vector2 _last = new Vector2(float.NaN, 0f); string _lastText; Vector3 _camPrev;
        void Start() { if (DemoBoot.CaptureDir != null) StartCoroutine(Measure()); UnityEngine.SceneManagement.SceneManager.sceneUnloaded += s => Report(s.name); }
        System.Collections.IEnumerator Measure()
        {
            var eof = new WaitForEndOfFrame();
            while (true)
            {
                yield return eof;
                ChapterUI ui = ChapterUI.Live;
                var rt = ui != null && ui.SubtitleVisible ? ui.SubtitleRect : null;
                var canvas = rt != null ? rt.GetComponentInParent<Canvas>() : null;
                var cam = canvas != null ? (canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera) : null;
                if (rt == null || canvas == null) { _last.x = float.NaN; continue; }
                var c = new Vector3[4]; rt.GetWorldCorners(c);
                Vector2 p = cam != null ? (Vector2)cam.WorldToScreenPoint((c[0] + c[3]) * 0.5f) : (Vector2)((c[0] + c[3]) * 0.5f);   // the panel's bottom centre
                var main = Camera.main;
                bool moving = main != null && (main.transform.position - _camPrev).sqrMagnitude > 1e-6f; if (main != null) _camPrev = main.transform.position;
                if (!float.IsNaN(_last.x) && ui.SubtitleText == _lastText)
                {
                    _frames++; if (moving) _moving++; if (CameraShake.Active) _shaking++;
                    _jump = Mathf.Max(_jump, (p - _last).magnitude);
                }
                _last = p; _lastText = ui.SubtitleText;
            }
        }
        void Report(string scene)
        {
            if (DemoBoot.CaptureDir == null || _frames == 0) return;
            ChapterCapture.I?.Log($"SUBTITLE STILL (QF25 2) {scene}: {_frames} frames with a subtitle on screen (the camera moving in {_moving}, shaking in {_shaking}); largest move of the subtitle on screen {_jump:0.00} px (must be under 0.5){(Off ? " [-noqf25fix: the 0.17.3 canvases]" : "")}");
            _frames = _moving = _shaking = 0; _jump = 0f; _last.x = float.NaN;
        }
        void OnApplicationQuit() => Report("(quit)");

        void Ensure()
        {
            // Round 20: the Grand Circuit keeps each player's HUD on that player's own camera (split screen: one HUD per viewport); its
            // tracks lie within ~500 m of the origin, so the float-precision jitter this overlay exists for cannot happen there
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == GcGame.SceneName) return;
            var main = Camera.main;
            if (main == null || main == _ui) return;
            var bd = main.GetComponent<UniversalAdditionalCameraData>();
            if (bd == null || bd.renderType != CameraRenderType.Base) return;
            if (RendererIndex < 0) return;                                        // no plain renderer: leave the UI where it was
            if (_ui == null)
            {
                var go = new GameObject("UiOverlayCamera"); go.transform.SetParent(transform, false);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);   // the world origin: full float precision
                _ui = go.AddComponent<Camera>();
                _ui.cullingMask = 1 << 5; _ui.clearFlags = CameraClearFlags.Nothing; _ui.nearClipPlane = 0.05f; _ui.farClipPlane = 10f; _ui.fieldOfView = 50f;
                var ud = go.AddComponent<UniversalAdditionalCameraData>();
                ud.renderType = CameraRenderType.Overlay; ud.renderPostProcessing = false; ud.SetRenderer(RendererIndex);
            }
            if (_base != main)
            {
                if (_base != null) { var ob = _base.GetComponent<UniversalAdditionalCameraData>(); if (ob != null) ob.cameraStack.Remove(_ui); }
                _base = main;
                Debug.Log($"[Seven] UI overlay (QF25 2): screen UI on its own camera at the world origin, stacked on '{main.name}' (renderer {RendererIndex})");
            }
            // any old per-scene subtitle camera (QF10's, parented to the gameplay camera) gives way to this one
            bd.cameraStack.RemoveAll(c => c == null || (c != _ui && c.transform.IsChildOf(main.transform) && (c.cullingMask & ~(1 << 5)) == 0));
            if (!bd.cameraStack.Contains(_ui)) bd.cameraStack.Add(_ui);
            main.cullingMask &= ~(1 << 5);
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c == null || !c.isRootCanvas || c.renderMode != RenderMode.ScreenSpaceCamera) continue;
                if (c.worldCamera == _ui) { Layer(c); continue; }   // elements added since (markers, bars) join the UI layer too
                if (c.worldCamera != null && c.worldCamera != main && !c.worldCamera.transform.IsChildOf(main.transform) && c.worldCamera.targetTexture != null) continue;   // a render-texture screen keeps its camera
                c.worldCamera = _ui;
                Layer(c);
            }
        }
    }
}
