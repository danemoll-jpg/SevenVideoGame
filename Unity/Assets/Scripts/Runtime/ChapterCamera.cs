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
        public Vector3 ShotPos => _shotPos;
        public Vector3 ShotLook => _shotLook;
        public float Fov => GetComponent<Camera>().fieldOfView;
        /// <summary>The camera has eased (almost) onto its shot position.</summary>
        public bool Settled => !_shot || (transform.position - _shotPos).magnitude < 0.25f;

        /// <summary>QF4: who must stay in clear view during a conversation shot (transform + height offset). While set,
        /// a shot whose line of sight to any of them is blocked (or that leaves one out of the frame) is moved to the
        /// nearest clear framing around them - eased when the way there is open, cut when a wall is in between.</summary>
        public readonly System.Collections.Generic.List<(Transform t, float h)> Subjects = new System.Collections.Generic.List<(Transform, float)>();
        public static int Reframes;
        /// <summary>The clear-view rule applies while a line plays and until this time (set just before a conversation).</summary>
        [System.NonSerialized] public float TalkUntil;
        float _nextCheck; int _fails;
        bool _userShot;          // a cut shot set by the script (check-only views) is never reframed

        /// <summary>True when nothing solid lies between a and b (the last 0.3 m at b is ignored: the subject itself).</summary>
        public static bool ClearLine(Vector3 a, Vector3 b)
        {
            var d = b - a; float len = d.magnitude;
            if (len < 0.35f) return true;
            return !Physics.Raycast(a, d / len, len - 0.3f, ~0, QueryTriggerInteraction.Ignore);
        }

        bool SeesAll(Vector3 pos, Vector3 look, float fov, float margin = 0.97f)
        {
            var rot = Quaternion.LookRotation(look - pos);
            float tanV = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad), aspect = GetComponent<Camera>().aspect;
            foreach (var (t, h) in Subjects)
            {
                if (t == null) continue;
                var p = t.position + Vector3.up * h;
                if (!ClearLine(pos, p)) return false;
                var lp = Quaternion.Inverse(rot) * (p - pos);
                if (lp.z < 0.3f) return false;
                if (Mathf.Abs(lp.y / lp.z) > tanV * margin || Mathf.Abs(lp.x / lp.z) > tanV * aspect * margin) return false;
            }
            return true;
        }

        void Reframe()
        {
            var cam = GetComponent<Camera>();
            if (SeesAll(_shotPos, _shotLook, cam.fieldOfView)) { _fails = 0; return; }
            if (++_fails < 2) return;           // blocked for two checks in a row (0.4 s) before moving
            var c = Vector3.zero; int n = 0;
            foreach (var (t, h) in Subjects) if (t != null) { c += t.position + Vector3.up * h; n++; }
            if (n == 0) return;
            c /= n;
            var look = c + Vector3.down * 0.1f;
            var away = _shotPos - c; away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            float d0 = Mathf.Clamp((_shotPos - c).magnitude, 2.4f, 6f);
            float fov0 = cam.fieldOfView;
            foreach (var fov in new[] { fov0, Mathf.Min(fov0 + 10f, 64f) })
            {
            cam.fieldOfView = fov;
            foreach (var dist in new[] { d0, d0 * 0.8f, d0 * 1.25f, d0 * 1.6f, d0 * 0.6f, 2.0f })
                foreach (var yaw in new[] { 0f, 25f, -25f, 50f, -50f, 75f, -75f, 105f, -105f, 140f, -140f, 180f })
                    foreach (var pitch in new[] { 16f, 28f, 6f, 40f })
                    {
                        var dir = Quaternion.AngleAxis(yaw, Vector3.up) * away.normalized;
                        dir = Quaternion.AngleAxis(-pitch, Vector3.Cross(Vector3.up, dir)) * dir;
                        var cand = c + dir.normalized * dist;
                        if (Physics.CheckSphere(cand, 0.25f, ~0, QueryTriggerInteraction.Ignore)) continue;
                        if (Physics.SphereCast(c, 0.2f, dir, out _, dist, ~0, QueryTriggerInteraction.Ignore)) continue;
                        // keep the authored aim if it still frames everyone, else aim between them
                        var aim = SeesAll(cand, _shotLook, cam.fieldOfView, 0.85f) ? _shotLook : Vector3.Lerp(look, _shotLook, 0.25f);
                        if (!SeesAll(cand, aim, cam.fieldOfView, 0.85f)) { aim = look; if (!SeesAll(cand, aim, cam.fieldOfView, 0.85f)) continue; }
                        bool cut = Physics.SphereCast(transform.position, 0.15f, cand - transform.position, out _, (cand - transform.position).magnitude, ~0, QueryTriggerInteraction.Ignore);
                        Reframes++;
                        Debug.Log($"[Seven] dialogue camera reframed: {_shotPos} -> {cand} ({(cut ? "cut" : "eased")})");
                        _shotPos = cand; _shotLook = aim; _fails = 0;
                        if (cut) { transform.position = cand; _look = aim; transform.rotation = Quaternion.LookRotation(aim - cand); _shotPosVel = _shotLookVel = Vector3.zero; }
                        return;
                    }
            }
            cam.fieldOfView = fov0;
        }

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
            _userShot = cut && ease <= 0.05f; _nextCheck = 0f;
            if (cut) { transform.position = pos; _look = look; transform.rotation = Quaternion.LookRotation(look - pos); }
        }

        /// <summary>Back to following 7 (from wherever the camera is now).</summary>
        float _baseFov;
        public void Follow(bool behind = true)
        {
            Subjects.Clear();
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
                if (Subjects.Count > 0 && !_userShot && (DialoguePlayer.AnyBusy || Time.time < TalkUntil) && Time.time >= _nextCheck) { _nextCheck = Time.time + 0.2f; Reframe(); }
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
