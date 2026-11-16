using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>Scene glue: key legend, Esc / Start resets the scene, starts the check run under -capture.</summary>
    public class FightDirector : MonoBehaviour
    {
        public SevenFighter Seven;
        public TrainingBot Bot;
        public FightInput Input;
        public FightCamera Cam;

        GUIStyle _style, _box;

        void Start()
        {
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

        const string Legend =
            "1 / 2 / 3   jab / jab-cross / hook\n" +
            "4 / 5         roundhouse / mma kick\n" +
            "Space        block (hold)\n" +
            "Shift         dodge\n" +
            "F              finisher\n" +
            "T              telekinesis push\n" +
            "WASD        move\n" +
            "Esc           reset";

        void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(13, Screen.height / 54) };
                _style.normal.textColor = Color.white;
                var bg = new Texture2D(1, 1);
                bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.45f));
                bg.Apply();
                _box = new GUIStyle { normal = { background = bg } };
            }
            var size = _style.CalcSize(new GUIContent(Legend));
            var r = new Rect(16, Screen.height - size.y - 28, size.x + 24, size.y + 12);
            GUI.Box(r, GUIContent.none, _box);
            GUI.Label(new Rect(r.x + 12, r.y + 6, size.x, size.y), Legend, _style);
        }
    }
}
