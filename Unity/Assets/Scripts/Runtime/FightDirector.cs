using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>Scene glue: Backspace / d-pad down resets the scene, Esc / Start goes back to the demo picker,
    /// -room loads the old training room, -capture starts the check run.</summary>
    public class FightDirector : MonoBehaviour
    {
        public SevenFighter Seven;
        public TrainingBot Bot;
        public FightInput Input;
        public FightCamera Cam;
        public TelekinesisFX Fx;
        public ControlsLegend Legend;

        void Start()
        {
            if (DemoBoot.Room && SceneManager.GetActiveScene().name != "FightDemo_Room")
            {
                SceneManager.LoadScene("FightDemo_Room");
                return;
            }
            if (DemoBoot.CaptureDir != null && DemoBoot.FightCombo) { gameObject.AddComponent<FightComboRun>().Init(this); return; }
            if (DemoBoot.CaptureDir != null)
            {
                var run = gameObject.AddComponent<CaptureRun>();
                run.Init(this);
            }
        }

        void Update()
        {
            if (DemoBoot.CaptureDir != null || Input == null || PauseMenu.IsOpen) return;
            if (Input.Reset.WasPressedThisFrame()) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            // Round 7: Esc / Start open the pause menu (PauseMenu) instead of leaving directly.
        }
    }
}
