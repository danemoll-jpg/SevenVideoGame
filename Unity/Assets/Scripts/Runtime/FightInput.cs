using UnityEngine;
using UnityEngine.InputSystem;

namespace Seven
{
    /// <summary>Input System actions for the fight demo (keyboard + gamepad), built in code.</summary>
    public class FightInput : MonoBehaviour
    {
        public InputAction Move, Jab, Cross, Hook, Roundhouse, MmaKick, Block, Dodge, Finisher, Telekinesis, Reset;

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

            Jab = Button("jab", "<Keyboard>/1", "<Gamepad>/buttonWest");
            Cross = Button("cross", "<Keyboard>/2", "<Gamepad>/buttonNorth");
            Hook = Button("hook", "<Keyboard>/3", "<Gamepad>/rightShoulder");
            Roundhouse = Button("roundhouse", "<Keyboard>/4", "<Gamepad>/buttonEast");
            MmaKick = Button("mma_kick", "<Keyboard>/5", "<Gamepad>/buttonSouth");
            Block = Button("block", "<Keyboard>/space", "<Gamepad>/leftTrigger");
            Dodge = Button("dodge", "<Keyboard>/shift", "<Gamepad>/leftShoulder");
            Finisher = Button("finisher", "<Keyboard>/f", "<Gamepad>/rightTrigger");
            Telekinesis = Button("telekinesis", "<Keyboard>/t", "<Gamepad>/dpad/up");
            Reset = Button("reset", "<Keyboard>/escape", "<Gamepad>/start");

            foreach (var a in All()) a.Enable();
        }

        InputAction[] All() => new[] { Move, Jab, Cross, Hook, Roundhouse, MmaKick, Block, Dodge, Finisher, Telekinesis, Reset };

        void OnDestroy()
        {
            foreach (var a in All()) { a?.Disable(); a?.Dispose(); }
        }
    }
}
