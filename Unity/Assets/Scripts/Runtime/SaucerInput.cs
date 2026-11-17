using UnityEngine;
using UnityEngine.InputSystem;

namespace Seven
{
    /// <summary>Input System actions for the saucer demo (keyboard + gamepad), built in code.</summary>
    public class SaucerInput : MonoBehaviour
    {
        public InputAction Move, Look, Recenter, Sit, Stand, Reveal, Menu;

        static InputAction Button(string name, params string[] paths)
        {
            var a = new InputAction(name, InputActionType.Button);
            foreach (var p in paths) a.AddBinding(p);
            return a;
        }

        void Awake()
        {
            Move = new InputAction("move", InputActionType.Value, expectedControlType: "Vector2");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddBinding("<Gamepad>/leftStick");
            Look = new InputAction("look", InputActionType.Value, expectedControlType: "Vector2");
            Look.AddBinding("<Gamepad>/rightStick");
            Recenter = Button("recenter", "<Gamepad>/rightStickPress", "<Mouse>/middleButton", "<Keyboard>/c");
            Sit = Button("sit", "<Gamepad>/buttonSouth", "<Keyboard>/e");
            Stand = Button("stand", "<Gamepad>/buttonEast", "<Keyboard>/q");
            Reveal = Button("reveal", "<Gamepad>/buttonNorth", "<Keyboard>/r");
            Menu = Button("menu", "<Gamepad>/start", "<Keyboard>/escape");
            foreach (var a in All()) a.Enable();
        }

        InputAction[] All() => new[] { Move, Look, Recenter, Sit, Stand, Reveal, Menu };

        void OnDestroy()
        {
            foreach (var a in All()) { a?.Disable(); a?.Dispose(); }
        }
    }
}
