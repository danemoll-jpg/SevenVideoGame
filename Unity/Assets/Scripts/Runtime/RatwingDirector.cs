using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Seven
{
    /// <summary>
    /// Round 4 cave scene: plays Ratwing's 10 s "sequence" clip (asleep hanging -> wakes -> unfurls -> drops -> lands),
    /// then his "idle" stance loop. Scripted camera: slow push-in while he sleeps, hold on the eyes, pull back for the
    /// wingspan, tilt down with the drop, wide on the landing. Dust/haze puff at touchdown, drifting haze in the cave.
    /// End: "A: replay  /  Menu: back". A skips to the end once the sequence has been seen in full (per session).
    /// Esc / Start: back to the demo picker. -capture: RatwingCaptureRun drives it off-screen.
    /// </summary>
    public class RatwingDirector : MonoBehaviour
    {
        public Animator Anim;
        public Transform Model, Hips, Head;
        public Camera Cam;
        public Material Additive;
        public float CeilingY = 4.2f;

        public const float Length = 10f, TouchDown = 7.9f, EyesOpen = 3.55f;     // Round 8: re-keyed drop (cleanup_ratwing.py T_DOWN)
        public static bool SeenOnce;

        /// <summary>Seconds into the sequence (from the Animator); Length or more once it is over.</summary>
        public float T { get; private set; }
        public bool Finished => T >= Length;
        /// <summary>Check runs park the camera for close-ups; null = scripted path.</summary>
        [System.NonSerialized] public Pose? CamOverride;

        ParticleSystem _dust, _haze;
        bool _puffed;
        GameObject _prompt;
        Text _promptText;
        Vector3 _lookSmooth;
        bool _lookInit;

        void Start()
        {
            _haze = Haze();
            _dust = Dust();
            BuildPrompt();
            Restart();
            if (DemoBoot.CaptureDir != null) gameObject.AddComponent<RatwingCaptureRun>().Init(this);
        }

        public void Restart()
        {
            Anim.Play("sequence", 0, 0f);
            Anim.Update(0f);
            T = 0f;
            _puffed = false;
            _lookInit = false;
            if (_prompt != null) _prompt.SetActive(false);
        }

        public void Skip()
        {
            Anim.Play("idle", 0, 0f);
            Anim.Update(0f);
            T = Length;
            _puffed = true;
            _lookInit = false;
        }

        void Update()
        {
            var st = Anim.GetCurrentAnimatorStateInfo(0);
            if (st.IsName("sequence")) T = Mathf.Min(st.normalizedTime, 1f) * Length;
            else if (T < Length) T = Length;

            if (!_puffed && T >= TouchDown) { _puffed = true; Puff(); }
            if (Finished)
            {
                SeenOnce = true;
                if (_prompt != null && !_prompt.activeSelf) { _prompt.SetActive(true); RefreshPrompt(); }
            }

            if (DemoBoot.CaptureDir != null || PauseMenu.IsOpen) return;
            var kb = Keyboard.current; var gp = Gamepad.current;
            // Round 7: Esc / Start open the pause menu (PauseMenu) instead of leaving directly.
            bool a = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                     || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            if (a)
            {
                if (Finished) Restart();
                else if (SeenOnce) Skip();
            }
            if (_prompt != null && _prompt.activeSelf) RefreshPrompt();
        }

        // --------------------------------------------------------------------------------- camera
        static float S(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        struct Key { public float t; public Vector3 pos, look; public Key(float t, Vector3 p, Vector3 l) { this.t = t; pos = p; look = l; } }

        // Times in seconds; positions relative to the floor point under the ledge (Ratwing faces -Z, the camera).
        static readonly Key[] Keys =
        {
            new Key(0.0f, new Vector3(1.0f, 2.15f, -8.0f), new Vector3(0f, 3.05f, 0f)),
            new Key(3.4f, new Vector3(0.5f, 2.35f, -5.5f), new Vector3(0f, 2.75f, 0f)),     // slow push-in while asleep
            new Key(4.6f, new Vector3(0.42f, 2.35f, -5.25f), new Vector3(0f, 2.45f, 0f)),   // hold on the eyes
            new Key(6.4f, new Vector3(0.3f, 2.5f, -7.6f), new Vector3(0f, 2.85f, 0f)),      // pull back: the wingspan
            new Key(7.7f, new Vector3(0.6f, 1.7f, -7.5f), new Vector3(0f, 1.5f, -0.3f)),    // tilt down with the drop
            new Key(8.8f, new Vector3(1.0f, 1.45f, -7.0f), new Vector3(0f, 1.2f, -0.55f)),  // wide on the landing
            new Key(10.0f, new Vector3(0.75f, 1.5f, -6.4f), new Vector3(0f, 1.4f, -0.55f)),
        };

        public static void CameraAt(float t, out Vector3 pos, out Vector3 look)
        {
            if (t <= Keys[0].t) { pos = Keys[0].pos; look = Keys[0].look; return; }
            for (int i = 0; i < Keys.Length - 1; i++)
            {
                var a = Keys[i]; var b = Keys[i + 1];
                if (t <= b.t)
                {
                    float u = S(a.t, b.t, t);
                    pos = Vector3.Lerp(a.pos, b.pos, u); look = Vector3.Lerp(a.look, b.look, u);
                    return;
                }
            }
            var last = Keys[Keys.Length - 1];
            pos = last.pos; look = last.look;
        }

        void LateUpdate()
        {
            if (CamOverride.HasValue)
            {
                Cam.transform.SetPositionAndRotation(CamOverride.Value.position, CamOverride.Value.rotation);
                return;
            }
            float t = Mathf.Min(T, Length);
            CameraAt(t, out var pos, out var look);
            // During the drop the look target leans toward his hips so the camera follows him down.
            if (Hips != null)
            {
                float follow = S(6.3f, 6.9f, t) * (1f - S(7.9f, 8.8f, t));
                look = Vector3.Lerp(look, Hips.position, follow * 0.7f);
            }
            if (!_lookInit) { _lookSmooth = look; _lookInit = true; }
            _lookSmooth = Vector3.Lerp(_lookSmooth, look, 1f - Mathf.Exp(-Time.deltaTime * 8f));
            var root = Model != null ? Model.parent : null;
            var o = root != null ? root.position : Vector3.zero;
            Cam.transform.position = o + pos;
            Cam.transform.rotation = Quaternion.LookRotation(o + _lookSmooth - Cam.transform.position);
        }

        // ------------------------------------------------------------------------------------- fx
        ParticleSystem MakePs(string name, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = new Material(Additive);
            m.SetTexture("_MainTex", FxUtil.Dot(64));
            r.sharedMaterial = m;
            return ps;
        }

        ParticleSystem Haze()
        {
            var ps = MakePs("Haze", 50);
            var main = ps.main;
            main.loop = true; main.prewarm = true;
            main.duration = 20f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(14f, 22f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(2.2f, 4.5f);
            main.startColor = new Color(1f, 1f, 1f, 0.022f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var em = ps.emission; em.rateOverTime = 2.6f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(11f, 3.5f, 18f);
            ps.transform.position = (Model != null && Model.parent != null ? Model.parent.position : Vector3.zero) + new Vector3(0f, 1.6f, 2f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f); vel.y = new ParticleSystem.MinMaxCurve(-0.01f, 0.03f); vel.z = new ParticleSystem.MinMaxCurve(-0.12f, -0.02f);
            ((ParticleSystemRenderer)ps.GetComponent<Renderer>()).sharedMaterial.SetFloat("_Intensity", 1f);
            ps.Play();
            return ps;
        }

        ParticleSystem Dust()
        {
            var ps = MakePs("LandingDust", 160);
            var main = ps.main;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.8f);
            main.startColor = new Color(1f, 1f, 1f, 0.05f);
            main.gravityModifier = -0.02f;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 0.45f;
            shape.rotation = new Vector3(-90f, 0f, 0f);                     // flat ring on the floor, pushing outward
            var size = ps.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));
            var limit = ps.limitVelocityOverLifetime; limit.enabled = true; limit.drag = 2.2f; limit.dampen = 0.15f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            return ps;
        }

        void Puff()
        {
            var o = Model != null && Model.parent != null ? Model.parent.position : Vector3.zero;
            var p = Hips != null ? new Vector3(Hips.position.x, o.y + 0.05f, Hips.position.z) : o;
            _dust.transform.position = p;
            _dust.Emit(45);
        }

        // ------------------------------------------------------------------------------------ prompt
        void BuildPrompt()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("PromptCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Cam;
            canvas.planeDistance = 0.5f;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1f;
            _prompt = new GameObject("Prompt", typeof(RectTransform));
            _prompt.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)_prompt.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 48f);
            rt.sizeDelta = new Vector2(420f, 44f);
            _prompt.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
            var tgo = new GameObject("Text", typeof(RectTransform));
            tgo.transform.SetParent(_prompt.transform, false);
            var trt = (RectTransform)tgo.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
            _promptText = tgo.AddComponent<Text>();
            _promptText.font = font; _promptText.fontSize = 22; _promptText.alignment = TextAnchor.MiddleCenter;
            _promptText.color = new Color(0.92f, 0.92f, 0.92f);
            _prompt.SetActive(false);
        }

        /// <summary>Pad text when a controller is connected (or forced by the check run), keys otherwise.</summary>
        [System.NonSerialized] public int ForcePromptMode;   // 0 auto, 1 pad, 2 keys

        void RefreshPrompt()
        {
            bool pad = ForcePromptMode == 1 || (ForcePromptMode == 0 && Gamepad.current != null);
            _promptText.text = "A: replay   /   Menu: back";   // P3 (2): controller buttons only, game-wide (QF24 17) - the keyboard still works
        }

        public void ForcePrompt(int mode) { ForcePromptMode = mode; if (_promptText != null) RefreshPrompt(); }
    }
}
