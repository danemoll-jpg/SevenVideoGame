using UnityEngine;
using UnityEngine.InputSystem;

namespace Seven
{
    /// <summary>
    /// Orbit camera around the fight. Right stick (or right mouse button + drag) orbits: full 360° yaw, pitch
    /// clamped to MinPitch..MaxPitch, with a little acceleration on the stick. R3 / middle mouse / C re-centres to
    /// the 3/4 side framing of the two fighters. Distance still grows with their separation so both stay in view,
    /// and the camera never dips below the ground or looks through the terrain.
    /// </summary>
    public class FightCamera : MonoBehaviour
    {
        public Transform A, B;
        public FightInput Input;

        [Header("Orbit")]
        public float Yaw = 18f, Pitch = 7f;
        public float DefaultPitch = 7f, ThreeQuarterYaw = 18f;   // re-centre: this many degrees off side-on
        public float MinPitch = -5f, MaxPitch = 60f;
        public float StickYawSpeed = 170f, StickPitchSpeed = 95f; // deg/s at full deflection
        public float StickAccel = 520f;                            // deg/s² ramp-up (release stops 2x faster)
        public float MouseDegPerPixel = 0.18f;
        public float RecenterTime = 0.25f;

        [Header("Framing")]
        public float MinDistance = 3.6f, PerMetre = 0.85f, MaxDistance = 9f;
        public float LookHeight = 0.95f, Smooth = 0.35f;
        public float GroundClearance = 0.35f;

        [System.NonSerialized] public bool Hold;               // check runs park the camera for close-ups

        Vector3 _focus, _focusVel;
        float _dist, _distVel;
        float _yawVel, _pitchVel;
        bool _recentering;
        float _recYawVel, _recPitchVel;
        float _shake;

        public void Shake(float amount) => _shake = Mathf.Max(_shake, amount);

        Vector3 Focus()
        {
            var mid = (A.position + B.position) * 0.5f;
            return mid + Vector3.up * LookHeight;
        }

        float WantedDistance()
        {
            var sep = Vector2.Distance(new Vector2(A.position.x, A.position.z), new Vector2(B.position.x, B.position.z));
            return Mathf.Clamp(MinDistance + sep * PerMetre, MinDistance, MaxDistance);
        }

        /// <summary>Yaw of the 3/4 framing (side-on to the A→B line, plus ThreeQuarterYaw), on the side nearest now.</summary>
        public float RecenterYaw()
        {
            var line = B.position - A.position; line.y = 0f;
            if (line.sqrMagnitude < 1e-4f) return Yaw;
            float lineYaw = Mathf.Atan2(line.x, line.z) * Mathf.Rad2Deg;
            float y1 = lineYaw - 90f + ThreeQuarterYaw, y2 = lineYaw + 90f - ThreeQuarterYaw;
            return Mathf.Abs(Mathf.DeltaAngle(Yaw, y1)) <= Mathf.Abs(Mathf.DeltaAngle(Yaw, y2)) ? y1 : y2;
        }

        public void StartRecenter() => _recentering = true;

        /// <summary>Jump straight to an orbit angle (check runs).</summary>
        public void SetOrbit(float yaw, float pitch)
        {
            Yaw = yaw; Pitch = Mathf.Clamp(pitch, MinPitch, MaxPitch);
            _yawVel = _pitchVel = 0f; _recentering = false;
            if (A != null && B != null) { _focus = Focus(); _dist = WantedDistance(); Place(); }
        }

        void Start()
        {
            if (A == null || B == null) return;
            Yaw = RecenterYaw(); Pitch = DefaultPitch;
            _focus = Focus(); _dist = WantedDistance();
            Place();
        }

        void ReadInput(float dt)
        {
            Vector2 stick = Input != null ? Input.Look.ReadValue<Vector2>() : Vector2.zero;
            float wantYaw = stick.x * StickYawSpeed, wantPitch = -stick.y * StickPitchSpeed;
            _yawVel = Mathf.MoveTowards(_yawVel, wantYaw, StickAccel * (Mathf.Abs(wantYaw) < Mathf.Abs(_yawVel) ? 2f : 1f) * dt);
            _pitchVel = Mathf.MoveTowards(_pitchVel, wantPitch, StickAccel * (Mathf.Abs(wantPitch) < Mathf.Abs(_pitchVel) ? 2f : 1f) * dt);
            float dYaw = _yawVel * dt, dPitch = _pitchVel * dt;

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                var d = mouse.delta.ReadValue();
                dYaw += d.x * MouseDegPerPixel;
                dPitch -= d.y * MouseDegPerPixel;
            }
            if (Mathf.Abs(dYaw) + Mathf.Abs(dPitch) > 0.01f) _recentering = false;
            Yaw += dYaw;
            Pitch = Mathf.Clamp(Pitch + dPitch, MinPitch, MaxPitch);

            if (Input != null && Input.Recenter.WasPressedThisFrame()) StartRecenter();
        }

        void LateUpdate()
        {
            if (Hold || A == null || B == null) return;
            float dt = Time.deltaTime;
            ReadInput(dt);
            if (_recentering)
            {
                float target = RecenterYaw();
                Yaw = Mathf.SmoothDampAngle(Yaw, target, ref _recYawVel, RecenterTime);
                Pitch = Mathf.SmoothDamp(Pitch, DefaultPitch, ref _recPitchVel, RecenterTime);
                if (Mathf.Abs(Mathf.DeltaAngle(Yaw, target)) < 0.3f && Mathf.Abs(Pitch - DefaultPitch) < 0.3f) _recentering = false;
            }
            _focus = Vector3.SmoothDamp(_focus, Focus(), ref _focusVel, Smooth);
            _dist = Mathf.SmoothDamp(_dist, WantedDistance(), ref _distVel, Smooth);
            Place();
            if (_shake > 0f)
            {
                transform.position += Random.insideUnitSphere * _shake;
                _shake = Mathf.MoveTowards(_shake, 0f, dt * 0.4f);
            }
        }

        void Place()
        {
            var rot = Quaternion.Euler(Pitch, Yaw, 0f);
            var back = rot * Vector3.back;
            float dist = _dist;
            // Pull in if the line from the focus to the camera passes into the terrain.
            const int steps = 12;
            for (int i = 1; i <= steps; i++)
            {
                float d = _dist * i / steps;
                var p = _focus + back * d;
                if (p.y < Ground.Height(p.x, p.z) + GroundClearance * 0.5f) { dist = Mathf.Max(1.2f, _dist * (i - 1) / steps); break; }
            }
            var pos = _focus + back * dist;
            float floor = Ground.Height(pos.x, pos.z) + GroundClearance;
            if (pos.y < floor) pos.y = floor;
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(_focus - pos);
        }
    }
}
