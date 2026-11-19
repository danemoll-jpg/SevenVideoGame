using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 13 (0k, Dan: "every tremor must be obvious - a big camera shake, don't hold back"). The old tremor nudged the
    /// camera by ~4 cm per frame, which the camera rig overwrote the same frame, so nothing showed. This shakes after every
    /// camera script has placed the camera (late execution order): position and rotation, smooth noise, with a fast
    /// attack and a slow decay; removed again before the next frame's camera update. Check runs read <see cref="Peak"/>.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class CameraShake : MonoBehaviour
    {
        static float _amp, _until, _len, _start;
        static float _peakPos, _peakRot;
        Vector3 _appliedPos; Quaternion _appliedRot = Quaternion.identity;
        /// <summary>Largest offset of the last quake: metres, degrees.</summary>
        public static Vector2 Peak => new Vector2(_peakPos, _peakRot);

        /// <summary>Shake the main camera for 'seconds'; amplitude 1 = a strong quake (about 0.22 m and 3 degrees at peak).</summary>
        public static void Quake(float seconds, float amplitude = 1f)
        {
            var cam = Camera.main; if (cam == null) return;
            if (cam.GetComponent<CameraShake>() == null) cam.gameObject.AddComponent<CameraShake>();
            _start = Time.time; _len = Mathf.Max(0.1f, seconds); _until = _start + _len; _amp = amplitude;
            _peakPos = 0f; _peakRot = 0f;
        }
        public static bool Active => Time.time < _until;

        void Update()
        {
            // undo last frame's shake before any camera script runs this frame
            transform.position -= _appliedPos; transform.rotation = transform.rotation * Quaternion.Inverse(_appliedRot);
            _appliedPos = Vector3.zero; _appliedRot = Quaternion.identity;
        }

        void LateUpdate()
        {
            if (Time.time >= _until) return;
            float t = (Time.time - _start) / _len;
            float env = Mathf.Clamp01(t / 0.08f) * Mathf.Pow(1f - t, 1.4f) * _amp;      // fast attack, slow decay
            float s = Time.time * 23f;
            var p = new Vector3(Mathf.PerlinNoise(s, 0.1f) - 0.5f, Mathf.PerlinNoise(0.3f, s) - 0.5f, Mathf.PerlinNoise(s, 0.7f) - 0.5f) * 2f * 0.22f * env;
            var r = Quaternion.Euler((Mathf.PerlinNoise(s * 0.8f, 2.1f) - 0.5f) * 2f * 3f * env, (Mathf.PerlinNoise(3.3f, s * 0.8f) - 0.5f) * 2f * 1.5f * env, (Mathf.PerlinNoise(s * 0.8f, 4.4f) - 0.5f) * 2f * 2.5f * env);
            _appliedPos = transform.rotation * p; _appliedRot = r;
            transform.position += _appliedPos; transform.rotation = transform.rotation * r;
            _peakPos = Mathf.Max(_peakPos, p.magnitude);
            _peakRot = Mathf.Max(_peakRot, Quaternion.Angle(Quaternion.identity, r));
        }
    }
}
