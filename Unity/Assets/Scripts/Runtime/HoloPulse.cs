using UnityEngine;

namespace Seven
{
    /// <summary>QF18 (2): a hologram marker breathes (scale + brightness) so the eye finds it.</summary>
    public class HoloPulse : MonoBehaviour
    {
        Vector3 _s0; float _t0;
        void Start() { _s0 = transform.localScale; _t0 = Random.value * 3f; }
        void Update() { float k = 1f + 0.18f * Mathf.Sin((Time.time + _t0) * 4.2f); transform.localScale = _s0 * k; }
    }
}
