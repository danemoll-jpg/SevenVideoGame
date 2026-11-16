using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>Scene glue: Esc / Start resets the scene, -room loads the old training room, -capture starts the check run.</summary>
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
            if (DemoBoot.CaptureDir != null)
            {
                var run = gameObject.AddComponent<CaptureRun>();
                run.Init(this);
            }
        }

        void Update()
        {
            if (DemoBoot.CaptureDir == null && Input != null && Input.Reset.WasPressedThisFrame())
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
