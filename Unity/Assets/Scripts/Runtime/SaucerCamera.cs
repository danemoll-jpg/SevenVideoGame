using UnityEngine;
using UnityEngine.InputSystem;

namespace Seven
{
    /// <summary>
    /// Saucer camera. Walking: orbit around 7 (right stick / right mouse drag, R3 / C / middle mouse re-centres behind
    /// her). Seated: over-the-shoulder from behind and a little above, looking out of the window and down at the city
    /// (pitch never goes below SeatedMinPitch, so no low angles on her legs). Revealed: eases out to a wide side view
    /// with the city below. Inside the ship the camera is kept under the dome and inside the walls (analytic shape,
    /// no physics); the boom shortens instead of clipping through.
    /// </summary>
    public class SaucerCamera : MonoBehaviour
    {
        public SaucerSeven Seven;
        public SaucerInput Input;
        public Transform Chair;           // ChairPivot (seat point + forward = window)

        public enum Mode { Walk, Seated, Revealed }
        public Mode Current = Mode.Walk;

        [Header("Walk")]
        public float WalkDistance = 3.3f, WalkLookHeight = 1.15f, WalkPitch = 12f;
        public float MinPitch = -5f, MaxPitch = 55f;
        [Header("Seated / revealed (relative to the chair's facing)")]
        public float SeatedYawOffset = 74f, SeatedPitch = 24f, SeatedDistance = 2.7f;    // Round 8: from the side, clear of the chair shell   // yaw 0 = behind her, looking where the chair faces
        public float RevealYawOffset = 72f, RevealPitch = 20f, RevealDistance = 6.8f;
        public float SeatedMinPitch = 12f;
        [Header("Ship shell (camera stays inside while the ship is there)")]
        public float ShellRadius = 7.3f, CeilingCenter = 3.75f, CeilingEdge = 2.75f, CeilingRadius = 7.2f;
        [Tooltip("Wall nooks the camera may enter: x = centre angle (deg, atan2(z, x)), y = half width (deg), z = max radius.")]
        public Vector3[] Nooks;
        public float NookCeiling = 2.2f;
        [System.NonSerialized] public bool InsideShip = true;
        [System.NonSerialized] public bool Hold;

        public float StickYawSpeed = 150f, StickPitchSpeed = 85f, MouseDegPerPixel = 0.18f;
        public float Yaw, Pitch;
        float _dist, _distVel, _yawVel, _pitchVel, _recYawVel, _recPitchVel;
        Vector3 _focus, _focusVel, _posVel;
        bool _recentering;
        float _userYaw, _userPitch;   // seated / revealed: player's offset from the default framing

        float ChairYaw => Chair != null ? Chair.eulerAngles.y : 0f;

        public void SetMode(Mode m)
        {
            Current = m;
            _userYaw = 0f; _userPitch = 0f;
        }

        float _blendT = 1f, _blendDur = 1f;
        Vector3 _blendPos; Quaternion _blendRot;
        /// <summary>Ease from a given pose into the normal camera over the next seconds (gallery TV hand-back).</summary>
        public void BlendFrom(Vector3 pos, Quaternion rot, float seconds)
        {
            _blendPos = pos; _blendRot = rot; _blendDur = Mathf.Max(0.01f, seconds); _blendT = 0f;
            _recentering = false; _yawVel = _pitchVel = 0f;
        }

        public void StartRecenter() { _recentering = true; _userYaw = 0f; _userPitch = 0f; }

        void Start()
        {
            Yaw = Seven.transform.eulerAngles.y; Pitch = WalkPitch;
            _focus = WantedFocus(); _dist = WalkDistance;
            transform.position = Clamp(_focus - Quaternion.Euler(Pitch, Yaw, 0f) * Vector3.forward * _dist, _focus);
            transform.rotation = Quaternion.LookRotation(_focus - transform.position);
        }

        Vector3 WantedFocus()
        {
            switch (Current)
            {
                case Mode.Seated: return Chair.position + Chair.forward * 1.2f + Vector3.up * 0.75f;
                case Mode.Revealed: return Chair.position + Chair.forward * 0.3f + Vector3.up * 0.2f;
                default: return Seven.transform.position + Vector3.up * WalkLookHeight;
            }
        }

        void ReadInput(float dt, out float dYaw, out float dPitch)
        {
            Vector2 stick = Input != null ? Input.Look.ReadValue<Vector2>() : Vector2.zero;
            _yawVel = Mathf.MoveTowards(_yawVel, stick.x * StickYawSpeed, 500f * dt);
            _pitchVel = Mathf.MoveTowards(_pitchVel, -stick.y * StickPitchSpeed, 500f * dt);
            dYaw = _yawVel * dt; dPitch = _pitchVel * dt;
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                var d = mouse.delta.ReadValue();
                dYaw += d.x * MouseDegPerPixel; dPitch -= d.y * MouseDegPerPixel;
            }
            if (Mathf.Abs(dYaw) + Mathf.Abs(dPitch) > 0.01f) _recentering = false;
            if (Input != null && Input.Recenter.WasPressedThisFrame()) StartRecenter();
        }

        void LateUpdate()
        {
            if (Hold || Seven == null) return;
            float dt = Time.deltaTime;
            ReadInput(dt, out float dYaw, out float dPitch);
            float wantDist;
            if (Current == Mode.Walk)
            {
                Yaw += dYaw; Pitch = Mathf.Clamp(Pitch + dPitch, MinPitch, MaxPitch);
                if (_recentering)
                {
                    float target = Seven.transform.eulerAngles.y;
                    Yaw = Mathf.SmoothDampAngle(Yaw, target, ref _recYawVel, 0.25f);
                    Pitch = Mathf.SmoothDamp(Pitch, WalkPitch, ref _recPitchVel, 0.25f);
                    if (Mathf.Abs(Mathf.DeltaAngle(Yaw, target)) < 0.5f) _recentering = false;
                }
                wantDist = WalkDistance;
            }
            else
            {
                bool rev = Current == Mode.Revealed;
                _userYaw += dYaw;
                _userPitch += dPitch;
                float baseYaw = ChairYaw + (rev ? RevealYawOffset : SeatedYawOffset);
                float basePitch = rev ? RevealPitch : SeatedPitch;
                _userPitch = Mathf.Clamp(basePitch + _userPitch, SeatedMinPitch, MaxPitch) - basePitch;
                // Ease toward the framing (slow for the big move out to the wide view and back).
                float ease = rev ? 1.4f : 0.6f;
                Yaw = Mathf.SmoothDampAngle(Yaw, baseYaw + _userYaw, ref _recYawVel, ease);
                Pitch = Mathf.SmoothDamp(Pitch, basePitch + _userPitch, ref _recPitchVel, ease);
                wantDist = rev ? RevealDistance : SeatedDistance;
            }
            _focus = Vector3.SmoothDamp(_focus, WantedFocus(), ref _focusVel, Current == Mode.Walk ? 0.2f : 0.8f);
            _dist = Mathf.SmoothDamp(_dist, wantDist, ref _distVel, Current == Mode.Revealed ? 1.4f : 0.6f);
            var want = Clamp(_focus - Quaternion.Euler(Pitch, Yaw, 0f) * Vector3.forward * _dist, _focus);
            transform.position = Current == Mode.Walk ? want : Vector3.SmoothDamp(transform.position, want, ref _posVel, 0.12f);
            transform.rotation = Quaternion.LookRotation(_focus - transform.position);
            if (_blendT < 1f)
            {
                _blendT = Mathf.Min(1f, _blendT + dt / _blendDur);
                float w = 1f - Mathf.SmoothStep(0f, 1f, _blendT);
                transform.position = Vector3.Lerp(transform.position, _blendPos, w);
                transform.rotation = Quaternion.Slerp(transform.rotation, _blendRot, w);
            }
        }

        /// <summary>Highest the camera may go at this radius (under the dome).</summary>
        public float CeilingAt(float r)
        {
            float k = Mathf.Clamp01(r / CeilingRadius);
            return Mathf.Lerp(CeilingEdge, CeilingCenter, 1f - k * k);
        }

        bool Inside(Vector3 p)
        {
            float r = new Vector2(p.x, p.z).magnitude;
            if (r < ShellRadius) return p.y > 0.25f && p.y < CeilingAt(r) - 0.25f;
            if (Nooks != null && p.y > 0.25f && p.y < NookCeiling)
            {
                float a = Mathf.Atan2(p.z, p.x) * Mathf.Rad2Deg;
                foreach (var n in Nooks)
                    if (Mathf.Abs(Mathf.DeltaAngle(n.x, a)) < n.y - 2f && r < n.z) return true;
            }
            return false;
        }

        /// <summary>Shorten the boom so the camera stays inside the ship (while the ship is there).</summary>
        Vector3 Clamp(Vector3 pos, Vector3 focus)
        {
            if (!InsideShip) return pos;
            const int steps = 16;
            var prev = focus;
            for (int i = 1; i <= steps; i++)
            {
                var p = Vector3.Lerp(focus, pos, (float)i / steps);
                if (!Inside(p)) return prev;
                prev = p;
            }
            return pos;
        }
    }
}
