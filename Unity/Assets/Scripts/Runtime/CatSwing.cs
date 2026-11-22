using UnityEngine;

namespace Seven
{
    /// <summary>Round 18: a slow pendulum (the old transit tower's hanging ball sways a little in the wind).</summary>
    public class CatSwing : MonoBehaviour
    {
        public float Amp = 6f, Period = 7f;
        Quaternion _r0; void Start() { _r0 = transform.localRotation; }
        void Update() { transform.localRotation = _r0 * Quaternion.Euler(Mathf.Sin(Time.time * Mathf.PI * 2f / Period) * Amp, 0f, Mathf.Sin(Time.time * Mathf.PI * 2f / (Period * 1.3f)) * Amp * 0.4f); }
    }
}
