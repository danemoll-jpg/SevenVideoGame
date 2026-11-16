using UnityEngine;

namespace Seven
{
    /// <summary>
    /// A small rock or grass clump on the ground. While 7 holds telekinesis nearby it floats up and orbits her
    /// slowly; when she lets go it drifts back down and settles on the ground wherever it is.
    /// </summary>
    public class Debris : MonoBehaviour
    {
        enum State { Resting, Floating, Falling }
        State _state;
        Transform _center;
        float _radius, _angle, _angSpeed, _height, _bobPhase, _riseDelay, _t;
        Vector3 _spin, _vel;
        float _restOffset;   // pivot height above the ground when resting
        Quaternion _restRot;

        public bool IsFloating => _state == State.Floating;

        void Awake()
        {
            _restOffset = transform.position.y - Ground.Height(transform.position);
            _restRot = transform.rotation;
        }

        public void Float(Transform center)
        {
            _center = center;
            var d = transform.position - center.position; d.y = 0f;
            _radius = Mathf.Clamp(d.magnitude, 0.8f, 4.5f);
            _angle = Mathf.Atan2(d.z, d.x);
            _angSpeed = Random.Range(14f, 32f) * Mathf.Deg2Rad;   // all orbit the same way, slowly
            _height = Random.Range(0.35f, 1.7f);
            _bobPhase = Random.Range(0f, 6.28f);
            _riseDelay = Random.Range(0f, 0.6f) + _radius * 0.08f;
            _spin = Random.onUnitSphere * Random.Range(25f, 70f);
            _t = 0f;
            _state = State.Floating;
        }

        public void Drop()
        {
            if (_state != State.Floating) return;
            _state = State.Falling;
            _vel = Vector3.zero;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            switch (_state)
            {
                case State.Floating:
                {
                    _t += dt;
                    float k = Mathf.Clamp01((_t - _riseDelay) / 1.1f);
                    k = k * k * (3f - 2f * k);
                    if (k <= 0f) break;
                    _angle += _angSpeed * dt * k;
                    var c = _center.position;
                    var want = new Vector3(c.x + Mathf.Cos(_angle) * _radius, 0f, c.z + Mathf.Sin(_angle) * _radius);
                    float ground = Ground.Height(want.x, want.z) + _restOffset;
                    want.y = ground + (_height + 0.08f * Mathf.Sin(_t * 1.7f + _bobPhase)) * k;
                    transform.position = Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-6f * dt));
                    transform.Rotate(_spin * dt * k, Space.World);
                    break;
                }
                case State.Falling:
                {
                    // Gentle fall (slowed, like the power fading), then rest on the ground.
                    _vel += Vector3.down * 6f * dt;
                    var p = transform.position + _vel * dt;
                    float ground = Ground.Height(p.x, p.z) + _restOffset;
                    if (p.y <= ground) { p.y = ground; _state = State.Resting; }
                    transform.position = p;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, _restRot, 240f * dt);   // lands upright
                    break;
                }
            }
        }
    }
}
