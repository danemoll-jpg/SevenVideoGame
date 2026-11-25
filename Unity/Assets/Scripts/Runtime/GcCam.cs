using UnityEngine;

namespace Seven
{
    /// <summary>Round 20: a player's chase camera. Behind and above the vehicle in the TRACK's frame (it rolls with loops, banks and
    /// the corkscrew so "down" is always the road), lagging a little, looking ahead; the field of view opens with speed and kicks
    /// on a boost, a small shake on landings and wall hits. In the air it eases toward the world's up.</summary>
    public class GcCam : MonoBehaviour
    {
        public GcRacer Target; public bool Split;
        public float Back = 7.2f, Up = 2.4f, LookAhead = 7f;
        Camera _cam; Vector3 _pos; Quaternion _rot; Vector3 _upS = Vector3.up; float _fov, _shake; float _kick;
        public float LastFov => _fov;
        void Awake() { _cam = GetComponent<Camera>(); }
        public void Snap()
        {
            if (Target == null) return; if (_cam == null) _cam = GetComponent<Camera>();
            Desired(out _pos, out _rot); _upS = Target.transform.up; transform.SetPositionAndRotation(_pos, _rot); _fov = 64f; if (_cam != null) _cam.fieldOfView = _fov;
            Target.Event += (r, e) => { if (e == "land") _shake = Mathf.Max(_shake, 0.35f); if (e == "wall") _shake = Mathf.Max(_shake, 0.2f); if (e == "use:boost" || e == "boostpad") _kick = 1f; if (e == "pushed") _shake = 0.4f; };
        }
        void Desired(out Vector3 p, out Quaternion r)
        {
            var t = Target.transform; var up = Target.Air ? Vector3.Slerp(_upS, Vector3.up, 0.5f) : t.up;
            var fwd = Vector3.ProjectOnPlane(Target.Air ? Target.WorldVel : t.forward, up); if (fwd.sqrMagnitude < 0.01f) fwd = t.forward; fwd.Normalize();
            // look along the track a little more than along the vehicle (the heading swings while steering)
            if (!Target.Air && Target.On != null) { var tf = Target.On.Rot(Target.S + 6f) * Vector3.forward; fwd = Vector3.Slerp(fwd, Vector3.ProjectOnPlane(tf, up).normalized, 0.45f); }
            float back = Back + Mathf.Clamp01(Target.V / 60f) * 1.4f;
            p = t.position - fwd * back + up * Up;
            r = Quaternion.LookRotation((t.position + fwd * LookAhead + up * 1.1f) - p, up);
        }
        void LateUpdate()
        {
            if (Target == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            _upS = Vector3.Slerp(_upS, Target.Air ? Vector3.up : Target.transform.up, 1f - Mathf.Exp(-dt * 8f));
            Desired(out var p, out var r);
            // follow tightly along the direction of travel (no lag at 50 m/s), softly across
            var vel = Target.WorldVel; _pos += vel * dt;
            _pos = Vector3.Lerp(_pos, p, 1f - Mathf.Exp(-dt * 9f));
            _rot = Quaternion.Slerp(_rot, r, 1f - Mathf.Exp(-dt * 10f));
            _shake = Mathf.MoveTowards(_shake, 0f, dt * 1.2f); _kick = Mathf.MoveTowards(_kick, 0f, dt * 0.8f);
            var sh = _shake > 0f ? new Vector3(Mathf.PerlinNoise(Time.time * 30f, 0f) - 0.5f, Mathf.PerlinNoise(0f, Time.time * 30f) - 0.5f, 0f) * _shake * 0.6f : Vector3.zero;
            transform.SetPositionAndRotation(_pos + _rot * sh, _rot);
            float baseFov = Split ? 58f : 64f;
            float want = baseFov + Mathf.Clamp01(Target.V / 60f) * 14f + (Target.BoostT > 0f ? 6f : 0f) + _kick * 4f;
            _fov = Mathf.Lerp(_fov, want, 1f - Mathf.Exp(-dt * 4f));
            if (_cam != null) _cam.fieldOfView = _fov;
        }
    }
}
