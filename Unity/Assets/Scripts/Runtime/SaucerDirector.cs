using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// Saucer scene glue: reveal / stand input while seated (standing while revealed brings the ship back first),
    /// camera mode, context legend + "Sit" prompt, Esc / Start back to the demo picker, -capture check run.
    /// </summary>
    public class SaucerDirector : MonoBehaviour
    {
        public SaucerSeven Seven;
        public SaucerCamera Cam;
        public CatCompanion Cat;
        public ShipReveal Reveal;
        public ControlsLegend Legend;
        public SaucerInput Input;
        public GalleryTV Gallery;          // Round 5 art TV (optional)

        bool _standPending;
        /// <summary>Chapter 1 aboard (scenes 9-11): no reveal, the chapter sets the prompt.</summary>
        [System.NonSerialized] public bool ChapterMode;
        [System.NonSerialized] public string PromptOverride;
        [System.NonSerialized] public bool LiveInputInCapture;   // gallery check run: real input path with virtual devices

        void Start()
        {
            if (Chapter1State.Aboard) ChapterMode = true;
            PauseMenu.EscapeOwned = () => Gallery != null && Gallery.OwnsEscape;
            if (DemoBoot.CaptureDir != null && !ChapterMode)
            {
                if (DemoBoot.Gallery && Gallery != null) gameObject.AddComponent<GalleryCaptureRun>().Init(this);
                else gameObject.AddComponent<SaucerCaptureRun>().Init(this);
            }
        }

        public void PressReveal()
        {
            if (Seven.IsSeated && !_standPending) Reveal.Toggle();
        }

        public void PressStand()
        {
            if (!Seven.IsSeated) return;
            if (Reveal.Progress > 0f || Reveal.Target > 0f) { Reveal.Set(false); _standPending = true; }
            else Seven.RequestStand();
        }

        void Update()
        {
            bool live = (DemoBoot.CaptureDir == null || LiveInputInCapture) && Input != null;
            if (live)
            {
                // Esc leaves the gallery view first; Start still opens the demo menu from anywhere.
                // Round 7: Esc / Start open the pause menu (PauseMenu); Esc still leaves the gallery view first.
                if (PauseMenu.IsOpen) return;
                if (Input.Reveal.WasPressedThisFrame() && !ChapterMode) PressReveal();
                if (Input.Stand.WasPressedThisFrame()) PressStand();
            }
            if (_standPending && Reveal.Progress <= 0f) { _standPending = false; Seven.RequestStand(); }

            var m = Seven.Current;
            bool seated = m == SaucerSeven.Mode.SittingDown || m == SaucerSeven.Mode.Seated;
            var want = !seated ? SaucerCamera.Mode.Walk
                : (Reveal.Target > 0.5f && Reveal.Progress > 0.12f) ? SaucerCamera.Mode.Revealed : SaucerCamera.Mode.Seated;
            if (Cam.Current != want) Cam.SetMode(want);
            Cam.InsideShip = Reveal.Progress < 0.3f;

            if (Legend != null)
            {
                var layout = Gallery != null && Gallery.InView ? ControlsLegend.Layout.GalleryView
                    : m == SaucerSeven.Mode.Seated ? ControlsLegend.Layout.SaucerSeated : ControlsLegend.Layout.SaucerWalk;
                if (Legend.Set != layout) Legend.SetLayout(layout);
                string prompt = ChapterMode && PromptOverride != null ? PromptOverride : Seven.NearChair ? "Sit" : Gallery != null && Gallery.NearTV ? "View art" : null;
                // Round 9: in Chapter 1 there is no permanent legend; "X Sit" is taught once, then a quiet context prompt.
                Legend.HidePanels = Chapter1State.Aboard;
                if (Chapter1State.Aboard)
                {
                    if (Seven.IsSeated && !Chapter1State.Taught.Contains("sit")) { Chapter1State.Taught.Add("sit"); ChapterCapture.I?.Log("teach: sit done (seated)"); }
                    bool teachSit = prompt == "Sit" && !Chapter1State.Taught.Contains("sit");
                    if (teachSit != _teachSit) { _teachSit = teachSit; ChapterCapture.I?.Log("teach: sit " + (teachSit ? "shown" : "cleared")); }
                    if (teachSit) { Legend.Teach((ControlsLegend.Btn.Sit, "Sit")); Legend.Context(ControlsLegend.Btn.Sit, null); }
                    else { Legend.Teach(); Legend.Context(ControlsLegend.Btn.Interact, prompt); }
                    if (_prompt != null) { _prompt = null; Legend.Prompt(null); }
                }
                else if (prompt != _prompt) { _prompt = prompt; Legend.Prompt(prompt); }
            }
        }
        string _prompt;
        bool _teachSit;
    }
}
