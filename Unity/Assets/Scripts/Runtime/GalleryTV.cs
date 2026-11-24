using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Seven
{
    [Serializable] public class GalleryEntry { public string file; public string title; public bool include = true; }
    [Serializable] public class GalleryFile { public string credit = "Art by Kyle G. Anderson"; public GalleryEntry[] images; }

    /// <summary>
    /// Round 5: Kyle's art gallery TV on the saucer. At start it reads StreamingAssets/Gallery/gallery.json and loads
    /// every included image from that folder at runtime (converted to grayscale, longest side at most MaxSide).
    /// Idle: the screen cycles through the art (cross-fade every IdleSeconds). Near the screen the director shows
    /// "View art" (A / E): the camera eases to face the screen, the image fills it at its own aspect ratio; left / right
    /// (d-pad, left stick, LB / RB, arrow keys) = previous / next with a quick cross-fade (wraps), Y / F = full view
    /// (letterboxed over the whole display) and back, B / Esc = leave. Caption: title, credit, n / N. While viewing,
    /// a high-priority Volume turns bloom off so the art is not washed out.
    /// </summary>
    public class GalleryTV : MonoBehaviour
    {
        public const string Folder = "Gallery", JsonName = "gallery.json";

        [Header("Scene refs (set by the builder)")]
        public Renderer Screen;                 // quad, material Seven/GalleryScreen
        public Material UiMaterial;             // Seven/GalleryUI (full view)
        public Light ScreenLight;               // soft light thrown into the nook by the picture
        public SaucerSeven Seven;
        public SaucerCamera Cam;
        public SaucerInput Input;
        public ShipReveal Reveal;

        [Header("Screen (this transform = screen centre; forward points into the wall)")]
        public Vector2 ScreenSize = new Vector2(2.8f, 1.4f);
        public float ViewDistance = 2.4f;
        public float PromptRange = 3.4f;
        public int MaxSide = 2048;

        [Header("Timing")]
        public float IdleSeconds = 8f, IdleFade = 1.6f, StepFade = 0.3f, EaseSeconds = 0.9f;
        public float IdleBright = 1.0f, ViewBright = 0.92f, LightIntensity = 1.4f;

        public class Item { public string File, Title; public Texture2D Tex; public float Aspect, AvgLum; public Vector2Int Size; }
        public readonly List<Item> Items = new List<Item>();
        public string Credit { get; private set; } = "Art by Kyle G. Anderson";
        public bool Loaded { get; private set; }
        public int IncludedInJson { get; private set; }
        public float LoadSeconds { get; private set; }
        public string LoadLog { get; private set; } = "";

        public enum State { Off, Entering, Viewing, Leaving }
        public State Current { get; private set; } = State.Off;
        public bool Full { get; private set; }
        public int Index { get; private set; }
        public float FadeProgress => _fade;          // 1 = settled on Index
        public bool InView => Current == State.Entering || Current == State.Viewing;
        /// <summary>The gallery owns Esc while viewing or easing out (so Esc does not also open the demo menu).</summary>
        public bool OwnsEscape => Current != State.Off;
        public bool NearTV => Current == State.Off && Loaded && Items.Count > 0 && Seven != null && Seven.Current == SaucerSeven.Mode.Free && InZone(Seven.transform.position);
        [NonSerialized] public bool ScriptDriven;     // check run: no player input
        [NonSerialized] public bool CaptionVisible = true;
        [NonSerialized] public bool Probe;            // check run: image area drawn solid white (aspect measurement)

        int _prev = -1;
        float _fade = 1f, _fadeDur = 1f, _idleTimer, _ease;
        Material _mat;
        MaterialPropertyBlock _mpb;
        Vector3 _easeFromPos, _easeFromFocus; Quaternion _easeFromRot;
        bool _sevenHidden; float _showSevenAt = -1f;
        Volume _noBloom;
        float _stickReady;

        // Full view + caption UI.
        GameObject _canvas, _fullRoot, _caption;
        RawImage _fullA, _fullB;
        AspectRatioFitter _fitA, _fitB;
        Image _fullBg;
        Text _capTitle, _capLine;

        static readonly int TexA = Shader.PropertyToID("_TexA"), TexB = Shader.PropertyToID("_TexB");
        static readonly int RectA = Shader.PropertyToID("_RectA"), RectB = Shader.PropertyToID("_RectB");
        static readonly int BlendId = Shader.PropertyToID("_Blend"), BrightId = Shader.PropertyToID("_Bright"), BgId = Shader.PropertyToID("_Bg"), ProbeId = Shader.PropertyToID("_Probe");

        public static string GalleryDir => Path.Combine(Application.streamingAssetsPath, Folder);

        void Start()
        {
            _mat = Screen.material;     // instance
            _mat.SetTexture(TexA, Texture2D.blackTexture);
            _mat.SetTexture(TexB, Texture2D.blackTexture);
            BuildUI();
            // Bloom off while viewing: a global override volume blended in with the view.
            var vgo = new GameObject("GalleryNoBloom");
            vgo.transform.SetParent(transform, false);
            _noBloom = vgo.AddComponent<Volume>();
            _noBloom.isGlobal = true; _noBloom.priority = 50f; _noBloom.weight = 0f;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = prof.Add<Bloom>(false);
            bloom.intensity.Override(0f);
            _noBloom.profile = prof;
            StartCoroutine(Load());
        }

        // ------------------------------------------------------------------------------- loading
        // ------------------------------------------------------------------ Round 8: preload (no hitch on the saucer)
        static List<Item> _cache;
        static bool _caching, _cacheDone;
        static int _cacheIncluded; static float _cacheSeconds; static string _cacheLog = "", _cacheCredit;
        class PreloadHost : MonoBehaviour { }
        /// <summary>Decode + grayscale + downscale every included image once, while the main menu is idle, and keep
        /// them for the whole session. The saucer then shows the gallery the moment it loads.</summary>
        public static void Preload()
        {
            if (_caching || _cacheDone) return;
            _caching = true;
            var go = new GameObject("GalleryPreload"); DontDestroyOnLoad(go);
            go.AddComponent<PreloadHost>().StartCoroutine(PreloadRoutine());
        }
        static IEnumerator PreloadRoutine()
        {
            float t0 = Time.realtimeSinceStartup;
            var items = new List<Item>(); var log = new System.Text.StringBuilder();
            GalleryFile g = null;
            var json = Path.Combine(GalleryDir, JsonName);
            try { if (File.Exists(json)) g = JsonUtility.FromJson<GalleryFile>(File.ReadAllText(json)); } catch (Exception e) { log.AppendLine("gallery.json unreadable: " + e.Message); }
            if (g?.images != null)
            {
                _cacheCredit = g.credit;
                foreach (var e in g.images)
                {
                    if (e == null || !e.include || string.IsNullOrEmpty(e.file)) continue;
                    _cacheIncluded++;
                    var path = Path.Combine(GalleryDir, e.file);
                    if (!File.Exists(path)) { log.AppendLine("missing file: " + e.file); continue; }
                    Item it = null; var ld = LoadImageAsync(path, e, 2048, x => it = x);
                    while (true) { bool more; try { more = ld.MoveNext(); } catch (Exception ex) { log.AppendLine($"{e.file}: {ex.Message}"); break; } if (!more) break; yield return ld.Current; }
                    if (it != null) items.Add(it); else log.AppendLine("could not decode: " + e.file);
                    yield return null;
                }
            }
            _cache = items; _cacheSeconds = Time.realtimeSinceStartup - t0; _cacheLog = log.ToString();
            _cacheDone = true; _caching = false;
            Debug.Log($"[Seven] gallery preloaded: {items.Count} of {_cacheIncluded} images in {_cacheSeconds:0.00}s");
        }
        /// <summary>True when this saucer visit used the preloaded images (no decode on scene start).</summary>
        public bool FromPreload { get; private set; }

        IEnumerator Load()
        {
            while (_caching) yield return null;
            if (_cacheDone && _cache != null)
            {
                Items.AddRange(_cache);
                if (!string.IsNullOrEmpty(_cacheCredit)) Credit = _cacheCredit;
                IncludedInJson = _cacheIncluded; LoadSeconds = 0f; FromPreload = true;
                if (Items.Count > 0) { Index = 0; _prev = -1; _fade = 1f; Apply(); }
                Loaded = true;
                LoadLog = $"gallery: {Items.Count} of {IncludedInJson} included images from the preload cache (decoded in the menu in {_cacheSeconds:0.00}s; 0 s here)\n" + _cacheLog;
                Debug.Log("[Seven] " + LoadLog);
                yield break;
            }
            float t0 = Time.realtimeSinceStartup;
            var log = new System.Text.StringBuilder();
            var json = Path.Combine(GalleryDir, JsonName);
            GalleryFile g = null;
            try
            {
                if (File.Exists(json)) g = JsonUtility.FromJson<GalleryFile>(File.ReadAllText(json));
                else log.AppendLine("no " + json);
            }
            catch (Exception e) { log.AppendLine("gallery.json unreadable: " + e.Message); }
            if (g?.images != null)
            {
                if (!string.IsNullOrEmpty(g.credit)) Credit = g.credit;
                foreach (var e in g.images)
                {
                    if (e == null || !e.include || string.IsNullOrEmpty(e.file)) continue;
                    IncludedInJson++;
                    var path = Path.Combine(GalleryDir, e.file);
                    if (!File.Exists(path)) { log.AppendLine("missing file: " + e.file); continue; }
                    Item it = null; var ld = LoadImageAsync(path, e, MaxSide, x => it = x);
                    while (true) { bool more; try { more = ld.MoveNext(); } catch (Exception ex) { log.AppendLine($"{e.file}: {ex.Message}"); break; } if (!more) break; yield return ld.Current; }
                    if (it == null) { log.AppendLine("could not decode: " + e.file); continue; }
                    Items.Add(it);
                    if (Items.Count == 1) { Index = 0; _prev = -1; _fade = 1f; Apply(); }
                    yield return null;     // one image per frame
                }
            }
            LoadSeconds = Time.realtimeSinceStartup - t0;
            Loaded = true;
            log.Insert(0, $"gallery: {Items.Count} of {IncludedInJson} included images loaded in {LoadSeconds:0.00}s from {GalleryDir}\n");
            LoadLog = log.ToString();
            Debug.Log("[Seven] " + LoadLog);
        }

        /// <summary>P2 (5): one image, loaded without stalling the game - the file is read and the grayscale + downscale (the C# pixel
        /// loops: ~300 ms on a large piece, the saucer's load hitches whenever the menu's preload had not finished) run on a worker
        /// thread; only the JPG / PNG decode and the final texture upload stay on the main thread.</summary>
        static IEnumerator LoadImageAsync(string path, GalleryEntry e, int MaxSide, Action<Item> done)
        {
            var read = System.Threading.Tasks.Task.Run(() => File.ReadAllBytes(path));
            while (!read.IsCompleted) yield return null;
            if (read.IsFaulted) throw read.Exception.InnerException ?? read.Exception;
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!src.LoadImage(read.Result, false)) { Destroy(src); done(null); yield break; }
            int sw = src.width, sh = src.height;
            var px = src.GetPixels32();
            Destroy(src);
            int w = 0, h = 0; byte[] dst = null; double sum = 0;
            var work = System.Threading.Tasks.Task.Run(() =>
            {
                // Grayscale (Rec. 709 weights on the stored sRGB values) ...
                var lum = new float[sw * sh];
                for (int i = 0; i < lum.Length; i++) { var c = px[i]; lum[i] = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b; }
                // ... then shrink so the longest side is at most MaxSide (bilinear).
                float s = Mathf.Min(1f, (float)MaxSide / Mathf.Max(sw, sh));
                w = Mathf.Max(1, Mathf.RoundToInt(sw * s)); h = Mathf.Max(1, Mathf.RoundToInt(sh * s));
                dst = new byte[w * h];
                float fx = (float)sw / w, fy = (float)sh / h;
                for (int y = 0; y < h; y++)
                {
                    float sy = Mathf.Clamp((y + 0.5f) * fy - 0.5f, 0f, sh - 1);
                    int y0 = (int)sy, y1 = Mathf.Min(y0 + 1, sh - 1); float ty = sy - y0;
                    for (int x = 0; x < w; x++)
                    {
                        float sx = Mathf.Clamp((x + 0.5f) * fx - 0.5f, 0f, sw - 1);
                        int x0 = (int)sx, x1 = Mathf.Min(x0 + 1, sw - 1); float tx = sx - x0;
                        float a = lum[y0 * sw + x0] + (lum[y0 * sw + x1] - lum[y0 * sw + x0]) * tx;
                        float b = lum[y1 * sw + x0] + (lum[y1 * sw + x1] - lum[y1 * sw + x0]) * tx;
                        float v = a + (b - a) * ty;
                        byte bv = (byte)Mathf.Clamp(Mathf.RoundToInt(v), 0, 255);
                        dst[y * w + x] = bv;
                        float l = bv / 255f;
                        sum += l <= 0.04045f ? l / 12.92f : Mathf.Pow((l + 0.055f) / 1.055f, 2.4f);
                    }
                }
            });
            while (!work.IsCompleted) yield return null;
            if (work.IsFaulted) throw work.Exception.InnerException ?? work.Exception;
            var tex = new Texture2D(w, h, TextureFormat.R8, true, true)
            {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4, name = e.file,
            };
            tex.SetPixelData(dst, 0);
            tex.Apply(true, true);
            done(new Item
            {
                File = e.file, Title = string.IsNullOrEmpty(e.title) ? Path.GetFileNameWithoutExtension(e.file) : e.title,
                Tex = tex, Aspect = (float)sw / sh, AvgLum = (float)(sum / (w * h)), Size = new Vector2Int(sw, sh),
            });
        }

        // ------------------------------------------------------------------------------ controls
        public void Next() => Step(1);
        public void Prev() => Step(-1);

        void Step(int d)
        {
            if (Items.Count == 0) return;
            Show((Index + d + Items.Count) % Items.Count, InView ? StepFade : IdleFade);
        }

        void Show(int i, float fade)
        {
            if (Items.Count == 0 || i == Index && _fade >= 1f) return;
            // A step during a fade starts the new fade from what is on screen now (the nearer image).
            _prev = _fade < 0.5f && _prev >= 0 ? _prev : Index;
            Index = i;
            _fade = 0f; _fadeDur = Mathf.Max(0.01f, fade);
            _idleTimer = 0f;
        }

        // ------------------------------------------------------------------ Round 8: 7 sits on the sofa while browsing
        [Header("Sofa (Round 8)")]
        public bool SitOnSofa = true;
        public Vector3 SofaRoot, SofaFacing;
        Vector3 _standPos; Quaternion _standRot; bool _onSofa;
        bool UseSofa => SitOnSofa && SofaFacing.sqrMagnitude > 0.5f;
        IEnumerator MoveSeven(Vector3 to, Quaternion rot, float seconds, string clip, string after)
        {
            var p0 = Seven.transform.position; var r0 = Seven.transform.rotation;
            Seven.PlayClip(clip, 0.2f);
            float len = Mathf.Max(0.3f, Seven.Length(clip));
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Seven.transform.SetPositionAndRotation(Vector3.Lerp(p0, to, u), Quaternion.Slerp(r0, rot, u));
                yield return null;
            }
            Seven.transform.SetPositionAndRotation(to, rot);
            float rest = len - seconds;
            for (float t = 0f; t < rest; t += Time.deltaTime) yield return null;
            if (after != null) Seven.PlayClip(after, 0.3f);
        }

        public void Enter()
        {
            if (Current != State.Off || Items.Count == 0) return;
            Current = State.Entering;
            _ease = 0f;
            if (UseSofa)
            {
                _standPos = Seven.transform.position; _standRot = Seven.transform.rotation;
                Seven.OnSofa = true; _onSofa = true;
                StartCoroutine(MoveSeven(SofaRoot, Quaternion.LookRotation(SofaFacing), 0.9f, "stand_to_sit", "sitting_idle"));
            }
            _easeFromPos = Cam.transform.position; _easeFromRot = Cam.transform.rotation;
            _easeFromFocus = _easeFromPos + Cam.transform.forward * Vector3.Distance(_easeFromPos, transform.position);
            Cam.Hold = true;
            Seven.Frozen = true;
            Full = false;
        }

        public void ToggleFull() { if (Current == State.Viewing || Current == State.Entering) Full = !Full; }

        public void Leave()
        {
            if (!InView) return;
            Full = false;
            Current = State.Leaving;
            // Hand the camera back behind 7, looking at the screen, blended from the view pose.
            var d = transform.position - Seven.transform.position; d.y = 0f;
            Cam.Yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            Cam.Pitch = Cam.WalkPitch;
            Cam.BlendFrom(Cam.transform.position, Cam.transform.rotation, EaseSeconds);
            Cam.Hold = false;
            _ease = 0f;
            _showSevenAt = Time.time + EaseSeconds * 0.55f;
            if (_onSofa) StartCoroutine(StandUpFromSofa());
        }
        IEnumerator StandUpFromSofa()
        {
            var front = SofaRoot + SofaFacing * 0.55f;
            yield return MoveSeven(front, Quaternion.LookRotation(SofaFacing), 0.9f, "sit_to_stand", "idle");
            Seven.OnSofa = false; _onSofa = false;
        }

        /// <summary>Seated: over her right shoulder to the screen. Standing (old): square in front of the screen.</summary>
        public Vector3 ViewPos => UseSofa
            ? SofaRoot + Vector3.up * 1.3f - SofaFacing * 1.0f + Vector3.Cross(Vector3.up, SofaFacing) * 0.42f
            : transform.position - transform.forward * ViewDistance;
        public Quaternion ViewRot => UseSofa ? Quaternion.LookRotation(transform.position + Vector3.up * 0.05f - ViewPos) : Quaternion.LookRotation(transform.forward);

        bool InZone(Vector3 p)
        {
            var c = transform.position; c.y = 0f; p.y = 0f;
            var to = p - c;
            var front = -transform.forward; front.y = 0f; front.Normalize();
            return to.magnitude < PromptRange && Vector3.Dot(to, front) > 0.3f;
        }

        void ReadInput()
        {
            if (ScriptDriven || Input == null) return;
            if (Current == State.Off)
            {
                if (Input.Sit.WasPressedThisFrame() && NearTV && !Seven.NearChair) Enter();
                return;
            }
            if (!InView) return;
            if (Input.GalleryLeave.WasPressedThisFrame()) { Leave(); return; }
            if (Input.GalleryFull.WasPressedThisFrame()) ToggleFull();
            if (Input.GalleryNext.WasPressedThisFrame()) Next();
            if (Input.GalleryPrev.WasPressedThisFrame()) Prev();
            float x = Input.Move.ReadValue<Vector2>().x;
            if (Mathf.Abs(x) < 0.35f) _stickReady = 0f;
            else if (Time.unscaledTime > _stickReady)
            {
                if (x > 0f) Next(); else Prev();
                _stickReady = Time.unscaledTime + (_stickReady == 0f ? 0.45f : 0.3f);
            }
        }

        // -------------------------------------------------------------------------------- update
        void Update()
        {
            if (!PauseMenu.IsOpen) ReadInput();
            float dt = Time.deltaTime;
            if (_fade < 1f) _fade = Mathf.Min(1f, _fade + dt / _fadeDur);
            if (Current == State.Off && Items.Count > 1 && _fade >= 1f)
            {
                _idleTimer += dt;
                if (_idleTimer >= IdleSeconds) Show((Index + 1) % Items.Count, IdleFade);
            }
            if (Current == State.Leaving)
            {
                _ease += dt / EaseSeconds;
                if (_ease >= 1f) { Current = State.Off; Seven.Frozen = false; }
            }
            if (_sevenHidden && _showSevenAt > 0f && Time.time >= _showSevenAt) { SetSevenVisible(true); _showSevenAt = -1f; }
            if (InView && Seven != null && !_onSofa)
            {

                // 7 turns to the screen.
                var d = transform.position - Seven.transform.position; d.y = 0f;
                if (d.sqrMagnitude > 0.01f) Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(d), 240f * dt);
            }
            float wantBloomOff = InView ? 1f : 0f;
            _noBloom.weight = Mathf.MoveTowards(_noBloom.weight, wantBloomOff, dt / 0.5f);
            Apply();
        }

        void LateUpdate()
        {
            if (Current != State.Entering && Current != State.Viewing) return;
            if (Current == State.Entering)
            {
                _ease = Mathf.Min(1f, _ease + Time.deltaTime / EaseSeconds);
                if (_ease > 0.35f && !_sevenHidden && !_onSofa && SevenBlocksView()) SetSevenVisible(false);
                if (_ease >= 1f) Current = State.Viewing;
            }
            float e = Mathf.SmoothStep(0f, 1f, _ease);
            var pos = Vector3.Lerp(_easeFromPos, ViewPos, e);
            var focus = Vector3.Lerp(_easeFromFocus, transform.position, e);
            Cam.transform.position = pos;
            Cam.transform.rotation = e >= 1f ? ViewRot : Quaternion.LookRotation(focus - pos);
        }

        /// <summary>Is 7 between the view camera and the screen?</summary>
        bool SevenBlocksView()
        {
            var p = Seven.transform.position + Vector3.up * 1.0f;
            var local = transform.InverseTransformPoint(p);      // z < 0 in front of the screen
            return local.z > -ViewDistance - 0.35f && Mathf.Abs(local.x) < ScreenSize.x * 0.5f + 1.2f;
        }

        void SetSevenVisible(bool on)
        {
            foreach (var r in Seven.GetComponentsInChildren<Renderer>()) r.enabled = on;
            _sevenHidden = !on;
        }

        Vector4 FitRect(int i)
        {
            if (i < 0 || i >= Items.Count) return new Vector4(1, 1, 0, 0);
            float sa = ScreenSize.x / ScreenSize.y, a = Items[i].Aspect;
            return a > sa ? new Vector4(1f, sa / a, 0, 0) : new Vector4(a / sa, 1f, 0, 0);
        }

        /// <summary>Screen material, light and full-view UI from the current state.</summary>
        void Apply()
        {
            if (_mat == null) return;
            bool has = Items.Count > 0;
            int a = _fade < 1f && _prev >= 0 ? _prev : Index;
            var ta = has ? Items[a].Tex : null; var tb = has ? Items[Index].Tex : null;
            _mat.SetTexture(TexA, ta != null ? ta : (Texture)Texture2D.blackTexture);
            _mat.SetTexture(TexB, tb != null ? tb : (Texture)Texture2D.blackTexture);
            _mat.SetVector(RectA, FitRect(a));
            _mat.SetVector(RectB, FitRect(Index));
            float blend = Mathf.SmoothStep(0f, 1f, _fade);
            _mat.SetFloat(BlendId, has ? blend : 0f);
            float reveal = Reveal != null ? Reveal.Progress : 0f;
            float bright = Mathf.Lerp(IdleBright, ViewBright, _noBloom != null ? _noBloom.weight : 0f);
            _mat.SetFloat(BrightId, Probe ? 1f : bright);
            _mat.SetFloat(BgId, Probe ? 0f : 0.02f);
            _mat.SetFloat(ProbeId, Probe ? 1f : 0f);
            if (ScreenLight != null)
            {
                float lum = has ? Mathf.Lerp(Items[a].AvgLum, Items[Index].AvgLum, blend) : 0f;
                ScreenLight.intensity = LightIntensity * Mathf.Clamp01(0.25f + lum * 2.2f) * (1f - reveal);
            }

            // Full view + caption.
            bool showFull = Full && InView;
            _fullRoot.SetActive(showFull);
            if (showFull)
            {
                SetFull(_fullA, _fitA, a, 1f);
                SetFull(_fullB, _fitB, Index, blend);
                _fullA.gameObject.SetActive(a != Index);
                if (a == Index) _fullB.color = Color.white;
            }
            bool cap = InView && CaptionVisible && has && !Probe;
            _caption.SetActive(cap);
            if (cap)
            {
                _capTitle.text = Items[Index].Title;
                _capLine.text = $"{Credit}     {Index + 1} / {Items.Count}";
            }
        }

        void SetFull(RawImage img, AspectRatioFitter fit, int i, float alpha)
        {
            img.texture = Probe ? (Texture)Texture2D.whiteTexture : Items[i].Tex;
            fit.aspectRatio = Items[i].Aspect;
            img.color = new Color(1f, 1f, 1f, alpha);
        }

        // -------------------------------------------------------------------------------------- UI
        void BuildUI()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var cam = Cam.GetComponent<Camera>();
            _canvas = new GameObject("GalleryCanvas");
            _canvas.transform.SetParent(transform, false);
            var canvas = _canvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = Mathf.Max(cam.nearClipPlane * 2.6f, 0.13f);   // just behind the legend (0.12)
            canvas.sortingOrder = 90;
            var scaler = _canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1f;

            _fullRoot = Rect("FullView", _canvas.transform, Vector2.zero, Vector2.one);
            _fullBg = _fullRoot.AddComponent<Image>();
            _fullBg.color = Color.black;
            (_fullA, _fitA) = FullImage("ImageA");
            (_fullB, _fitB) = FullImage("ImageB");
            _fullRoot.SetActive(false);

            _caption = Rect("Caption", _canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            var crt = (RectTransform)_caption.transform;
            crt.pivot = new Vector2(0.5f, 0f);
            crt.anchoredPosition = new Vector2(0f, 18f);
            crt.sizeDelta = new Vector2(440f, 58f);
            _caption.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
            _capTitle = Label(_caption.transform, font, 22, new Vector2(0f, 30f), new Vector2(440f, 26f));
            _capLine = Label(_caption.transform, font, 15, new Vector2(0f, 6f), new Vector2(440f, 22f));
            _capLine.color = new Color(0.78f, 0.78f, 0.78f, 1f);
            _caption.SetActive(false);
        }

        (RawImage, AspectRatioFitter) FullImage(string name)
        {
            var go = Rect(name, _fullRoot.transform, Vector2.zero, Vector2.one);
            var img = go.AddComponent<RawImage>();
            img.material = UiMaterial;
            img.raycastTarget = false;
            var fit = go.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            return (img, fit);
        }

        static GameObject Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return go;
        }

        static Text Label(Transform parent, Font font, int size, Vector2 pos, Vector2 box)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos; rt.sizeDelta = box;
            var t = go.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        void OnDestroy()
        {
            if (!FromPreload) foreach (var it in Items) if (it.Tex != null) Destroy(it.Tex);     // preloaded ones live for the session
            if (_noBloom != null && _noBloom.profile != null) Destroy(_noBloom.profile);
        }
    }
}
