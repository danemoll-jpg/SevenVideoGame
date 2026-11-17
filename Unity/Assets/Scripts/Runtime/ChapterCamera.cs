using UnityEngine;
using UnityEngine.InputSystem;

namespace Seven
{
    /// <summary>
    /// Chapter 1 camera: orbit around 7 (right stick / right mouse drag, R3 / C / middle mouse re-centres behind her)
    /// with a sphere-cast against the level's colliders so it never ends up inside rock. Cutscenes take over with
    /// <see cref="Shot"/> (a fixed position looking at a point, eased in) and hand back with <see cref="Follow"/>.
    /// </summary>
    public class ChapterCamera : MonoBehaviour
    {
        public Transform Target;
        public ChapterInput Input;
        public float Distance = 3.8f, LookHeight = 1.45f, DefaultPitch = 15f, MinPitch = -10f, MaxPitch = 60f;
        public float StickYawSpeed = 150f, StickPitchSpeed = 85f, MouseDegPerPixel = 0.18f;
        public float Yaw, Pitch;
        [System.NonSerialized] public bool AutoBehind;      // check runs: keep the camera behind her walk direction

        bool _shot;
        Vector3 _shotPos, _shotLook, _shotPosVel, _shotLookVel, _look;
        float _shotEase = 1f, _dist, _distVel, _yawVel, _pitchVel, _recYawVel, _recPitchVel;
        bool _recentering;
        Vector3 _focus, _focusVel;

        public bool InShot => _shot;

        void Start()
        {
            Pitch = DefaultPitch;
            if (Target != null) { Yaw = Target.eulerAngles.y; _focus = Target.position + Vector3.up * LookHeight; }
            _dist = Distance;
            _look = _focus;
        }

        /// <summary>Cut or ease to a fixed framing.</summary>
        public void Shot(Vector3 pos, Vector3 look, float ease = 1.2f, bool cut = false, float fov = 0f)
        {
            var c = GetComponent<Camera>();
            if (_baseFov <= 0f) _baseFov = c.fieldOfView;
            c.fieldOfView = fov > 0f ? fov : _baseFov;
            _shot = true; _shotPos = pos; _shotLook = look; _shotEase = Mathf.Max(0.01f, ease);
            if (cut) { transform.position = pos; _look = look; transform.rotation = Quaternion.LookRotation(look - pos); }
        }

        /// <summary>Back to following 7 (from wherever the camera is now).</summary>
        float _baseFov;
        public void Follow(bool behind = true)
        {
            if (_baseFov > 0f) GetComponent<Camera>().fieldOfView = _baseFov;
            _shot = false;
            if (behind && Target != null)
            {
                var d = Target.position + Vector3.up * LookHeight - transform.position; d.y = 0f;
                if (d.sqrMagnitude > 0.01f) Yaw = Quaternion.LookRotation(d).eulerAngles.y;
            }
            _focus = Target != null ? Target.position + Vector3.up * LookHeight : _focus;
            _dist = Mathf.Max(0.5f, (transform.position - _focus).magnitude);
        }

        public void SnapBehind()
        {
            if (Target == null) return;
            Yaw = Target.eulerAngles.y; Pitch = DefaultPitch;
            _focus = Target.position + Vector3.up * LookHeight; _dist = Distance;
            Place(1f);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (_shot)
            {
                transform.position = Vector3.SmoothDamp(transform.position, _shotPos, ref _shotPosVel, _shotEase * 0.5f);
                _look = Vector3.SmoothDamp(_look, _shotLook, ref _shotLookVel, _shotEase * 0.5f);
                transform.rotation = Quaternion.LookRotation(_look - transform.position);
                return;
            }
            if (Target == null) return;
            Vector2 stick = Input != null ? Input.Look.ReadValue<Vector2>() : Vector2.zero;
            _yawVel = Mathf.MoveTowards(_yawVel, stick.x * StickYawSpeed, 500f * dt);
            _pitchVel = Mathf.MoveTowards(_pitchVel, -stick.y * StickPitchSpeed, 500f * dt);
            float dYaw = _yawVel * dt, dPitch = _pitchVel * dt;
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed && DemoBoot.CaptureDir == null)
            {
                var d = mouse.delta.ReadValue();
                dYaw += d.x * MouseDegPerPixel; dPitch -= d.y * MouseDegPerPixel;
            }
            if (Mathf.Abs(dYaw) + Mathf.Abs(dPitch) > 0.01f) _recentering = false;
            if (Input != null && Input.Recenter.WasPressedThisFrame()) _recentering = true;
            Yaw += dYaw; Pitch = Mathf.Clamp(Pitch + dPitch, MinPitch, MaxPitch);
            if (_recentering || AutoBehind)
            {
                float target = Target.eulerAngles.y;
                Yaw = Mathf.SmoothDampAngle(Yaw, target, ref _recYawVel, AutoBehind ? 0.6f : 0.25f);
                Pitch = Mathf.SmoothDamp(Pitch, DefaultPitch, ref _recPitchVel, 0.4f);
                if (_recentering && Mathf.Abs(Mathf.DeltaAngle(Yaw, target)) < 0.5f) _recentering = false;
            }
            _focus = Vector3.SmoothDamp(_focus, Target.position + Vector3.up * LookHeight, ref _focusVel, 0.15f);
            Place(dt);
        }

        void Place(float dt)
        {
            var dir = Quaternion.Euler(Pitch, Yaw, 0f) * Vector3.back;
            float want = Distance;
            if (Physics.SphereCast(_focus, 0.22f, dir, out var hit, Distance, ~0, QueryTriggerInteraction.Ignore))
                want = Mathf.Max(0.45f, hit.distance - 0.05f);
            // Pull in fast, ease back out slowly.
            _dist = want < _dist ? want : Mathf.SmoothDamp(_dist, want, ref _distVel, 0.35f, Mathf.Infinity, Mathf.Max(dt, 1e-4f));
            transform.position = _focus + dir * _dist;
            transform.rotation = Quaternion.LookRotation(_focus - transform.position);
            _look = _focus;
        }
    }
}
