using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 7: distance-based footsteps placed at the feet (3D). One step every <see cref="Stride"/> metres of
    /// horizontal travel; the surface picks the variant set (step_rock_*, step_metal_*, paw_*, scree_step_*).
    /// Added at runtime by the directors; a missing clip set simply stays silent.
    /// </summary>
    public class Footsteps : MonoBehaviour
    {
        public float Stride = 0.72f, Volume = 0.55f, MaxDistance = 14f;
        public System.Func<string> Surface = () => "step_rock_";
        public System.Func<bool> Enabled = () => true;
        Vector3 _last;
        float _acc;
        public int Count { get; private set; }

        void Start() { _last = transform.position; }

        void Update()
        {
            var p = transform.position;
            var d = p - _last; d.y = 0f;
            _last = p;
            float m = d.magnitude;
            if (m > 1.5f || Time.deltaTime <= 0f) { _acc = 0f; return; }       // teleports / checkpoint resets
            if (!Enabled()) { _acc = Mathf.Min(_acc, Stride * 0.5f); return; }
            _acc += m;
            if (_acc < Stride) return;
            _acc -= Stride;
            AudioKit.PlayVariant(Surface(), p + Vector3.up * 0.05f, Volume, 0.06f, MaxDistance);
            Count++;
        }
    }
}
