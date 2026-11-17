using UnityEngine;
using UnityEngine.InputSystem;

namespace Seven
{
    /// <summary>Input System actions for Chapter 1 (keyboard + gamepad), built in code.</summary>
    public class ChapterInput : MonoBehaviour
    {
        public InputAction Move, Look, Recenter, Run, Interact, Telekinesis, Menu, Restart;

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
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            Move.AddBinding("<Gamepad>/leftStick");
            Look = new InputAction("look", InputActionType.Value, expectedControlType: "Vector2");
            Look.AddBinding("<Gamepad>/rightStick");
            Recenter = Button("recenter", "<Gamepad>/rightStickPress", "<Mouse>/middleButton", "<Keyboard>/c");
            Run = Button("run", "<Gamepad>/leftShoulder", "<Gamepad>/leftStickPress", "<Keyboard>/leftShift");
            Interact = Button("interact", "<Gamepad>/buttonSouth", "<Keyboard>/e");
            Telekinesis = Button("telekinesis", "<Gamepad>/buttonWest", "<Gamepad>/dpad/up", "<Keyboard>/t");
            Menu = Button("menu", "<Gamepad>/start", "<Keyboard>/escape");
            Restart = Button("restart", "<Gamepad>/dpad/down", "<Keyboard>/backspace");
            foreach (var a in All()) a.Enable();
        }

        InputAction[] All() => new[] { Move, Look, Recenter, Run, Interact, Telekinesis, Menu, Restart };

        void OnDestroy()
        {
            foreach (var a in All()) { a?.Disable(); a?.Dispose(); }
        }
    }
}
