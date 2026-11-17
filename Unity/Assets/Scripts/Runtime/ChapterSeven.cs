using UnityEngine;

namespace Seven
{
    /// <summary>
    /// 7 in Chapter 1: camera-relative walking (hold LB / Shift to run), a slowed "sneak" walk where the level asks for
    /// it (substitute for a Mixamo Sneak Walking clip), and two held poses: touch (telekinesis clip held at the
    /// arm-forward frame, substitute for a touch / button-pushing clip) and the telekinesis gesture. No combat.
    /// Check runs drive her with a world-space direction (<see cref="ScriptMove"/>).
    /// </summary>
    public class ChapterSeven : FighterBase
    {
        public ChapterInput Input;
        public Transform CameraTransform;
        public float WalkSpeed = 1.7f, RunSpeed = 3.9f, SneakSpeed = 1.25f;
        [Tooltip("Telekinesis clip frame held for the 'touch' pose (arm forward, open hand).")]
        public float TouchFrame = 0.45f;

        public enum Mode { Free, Locked, Touch, Gesture }
        public Mode Current { get; private set; } = Mode.Free;
        [System.NonSerialized] public bool Sneak;
        [System.NonSerialized] public bool ScriptDriven;
        [System.NonSerialized] public Vector3 ScriptMove;      // world direction * 0..1 (check runs / cutscenes)
        [System.NonSerialized] public bool ScriptRun;
        public float Speed { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Play("idle", 0f);
        }

        public void Lock(bool on)
        {
            if (on && Current == Mode.Free) { Current = Mode.Locked; if (State != "idle") Play("idle", 0.25f, 1f, false); }
            if (!on && Current == Mode.Locked) Current = Mode.Free;
        }

        /// <summary>Reach out and hold a hand on a surface in front of her.</summary>
        public void Touch(Vector3 facePoint)
        {
            _face = facePoint; Current = Mode.Touch;
            Play("telekinesis", 0.25f, 0.8f);
        }

        /// <summary>Telekinesis gesture toward a point (plays the whole clip, then back to idle).</summary>
        public void Gesture(Vector3 facePoint)
        {
            _face = facePoint; Current = Mode.Gesture;
            Play("telekinesis", 0.15f, 1f);
        }

        public void Release()
        {
            if (Current == Mode.Touch || Current == Mode.Gesture) { Freeze(false); Play("idle", 0.35f); }
            Current = Mode.Free;
        }

        Vector3 _face;
        public bool GestureDone => Current == Mode.Gesture && Tn >= 0.8f;
        public bool GesturePeak => Current == Mode.Gesture && Tn >= 0.49f;

        /// <summary>Teleport (checkpoint reset).</summary>
        public void Place(Vector3 pos, Vector3 facing)
        {
            Release();
            transform.position = new Vector3(pos.x, Ground.Height(pos.x, pos.z), pos.z);
            facing.y = 0f;
            if (facing.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(facing);
            Play("idle", 0f);
        }

        protected override void Update()
        {
            base.Update();
            switch (Current)
            {
                case Mode.Free: UpdateFree(); break;
                case Mode.Locked: Speed = 0f; if (State != "idle") Play("idle", 0.25f, 1f, false); break;
                case Mode.Touch:
                    Speed = 0f;
                    FaceTowards(_face, 240f);
                    if (Tn >= TouchFrame) Freeze(true);
                    break;
                case Mode.Gesture:
                    Speed = 0f;
                    FaceTowards(_face, 360f);
                    if (Tn >= 0.95f) { Current = Mode.Free; Play("idle", 0.3f); }
                    break;
            }
        }

        void UpdateFree()
        {
            Vector3 v;
            bool run;
            if (ScriptDriven) { v = ScriptMove; v.y = 0f; run = ScriptRun; }
            else
            {
                var m = Input != null ? Input.Move.ReadValue<Vector2>() : Vector2.zero;
                var fwd = CameraTransform != null ? CameraTransform.forward : Vector3.forward;
                fwd.y = 0f; fwd.Normalize();
                var right = new Vector3(fwd.z, 0f, -fwd.x);
                v = fwd * m.y + right * m.x;
                run = Input != null && Input.Run.IsPressed();
            }
            if (v.sqrMagnitude > 1f) v.Normalize();
            if (v.sqrMagnitude > 0.04f)
            {
                float spd = Sneak ? SneakSpeed : run ? RunSpeed : WalkSpeed;
                Speed = spd * v.magnitude;
                transform.position += v * spd * Time.deltaTime;
                string clip = run && !Sneak ? "run" : "walk";
                float animSpeed = Sneak ? 0.62f : 1f;
                if (State != clip) Play(clip, 0.15f, animSpeed, false);
                FaceTowards(transform.position + v, 540f);
            }
            else { Speed = 0f; if (State != "idle") Play("idle", 0.2f, 1f, false); }
        }
    }
}
