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

        bool _standPending;

        void Start()
        {
            if (DemoBoot.CaptureDir != null) gameObject.AddComponent<SaucerCaptureRun>().Init(this);
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
            bool live = DemoBoot.CaptureDir == null && Input != null;
            if (live)
            {
                if (Input.Menu.WasPressedThisFrame()) { SceneManager.LoadScene(DemoMenu.SceneName); return; }
                if (Input.Reveal.WasPressedThisFrame()) PressReveal();
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
                var layout = m == SaucerSeven.Mode.Seated ? ControlsLegend.Layout.SaucerSeated : ControlsLegend.Layout.SaucerWalk;
                if (Legend.Set != layout) Legend.SetLayout(layout);
                bool prompt = Seven.NearChair;
                if (prompt != _prompt) { _prompt = prompt; Legend.Prompt(prompt ? "Sit" : null); }
            }
        }
        bool _prompt;
    }
}
