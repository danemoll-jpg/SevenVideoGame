using UnityEngine;
using UnityEngine.InputSystem;

namespace Seven
{
    /// <summary>
    /// Input System actions for Chapter 1 (keyboard + gamepad), built in code. Round 9 chapter scheme:
    /// left stick / WASD move · right stick / mouse camera · A / Space jump (grabs ledges when jumping at them) ·
    /// X / E interact · B / C crouch-sneak (toggle; at an edge: climb down) · B / Space while hanging = let go ·
    /// RT / right mouse telekinesis · R3 / middle mouse re-centre · LB / Shift run · Y / Enter skip a line ·
    /// Start / Esc pause · d-pad down / Backspace restart from the checkpoint.
    /// </summary>
    public class ChapterInput : MonoBehaviour
    {
        public InputAction Move, Look, Recenter, Run, Interact, Telekinesis, Menu, Restart, Jump, Crouch, LetGo;

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
            // Round 9: C is crouch now; keyboard re-centre moved to R (middle mouse still works).
            Recenter = Button("recenter", "<Gamepad>/rightStickPress", "<Mouse>/middleButton", "<Keyboard>/r");
            Run = Button("run", "<Gamepad>/leftShoulder", "<Gamepad>/leftStickPress", "<Keyboard>/leftShift");
            Interact = Button("interact", "<Gamepad>/buttonWest", "<Keyboard>/e");
            Telekinesis = Button("telekinesis", "<Gamepad>/rightTrigger", "<Mouse>/rightButton", "<Keyboard>/t");
            Jump = Button("jump", "<Gamepad>/buttonSouth", "<Keyboard>/space");
            Crouch = Button("crouch", "<Gamepad>/buttonEast", "<Keyboard>/c");
            LetGo = Button("letgo", "<Gamepad>/buttonEast", "<Keyboard>/space", "<Keyboard>/c");
            Menu = Button("menu", "<Gamepad>/start", "<Keyboard>/escape");
            Restart = Button("restart", "<Gamepad>/dpad/down", "<Keyboard>/backspace");
            foreach (var a in All()) a.Enable();
        }

        InputAction[] All() => new[] { Move, Look, Recenter, Run, Interact, Telekinesis, Menu, Restart, Jump, Crouch, LetGo };

        void OnDestroy()
        {
            foreach (var a in All()) { a?.Disable(); a?.Dispose(); }
        }
    }
}
