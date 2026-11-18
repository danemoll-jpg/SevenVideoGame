using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 10 (silver saucer): a realtime reflection probe that rides with 7's saucer, so the polished silver hull
    /// reflects the cavern / fog / sky around it wherever it is seen (impound bay, the sighting from the terrace, the
    /// tractor beam, the liftoff). It never sees the saucer itself (layer <see cref="Layer"/>) nor the city (layer 20).
    /// Re-rendered when the ship has moved, and at least every few seconds; cheap (128 px, all faces at once).
    /// </summary>
    [RequireComponent(typeof(ReflectionProbe))]
    public class SaucerProbe : MonoBehaviour
    {
        public const int Layer = 22;
        public float MoveThreshold = 0.6f, MaxInterval = 4f;
        ReflectionProbe _p;
        Vector3 _last;
        float _lastTime = -99f;
        public int Renders { get; private set; }

        void Start() { _p = GetComponent<ReflectionProbe>(); Render(); }

        void LateUpdate()
        {
            if ((transform.position - _last).sqrMagnitude > MoveThreshold * MoveThreshold || Time.time - _lastTime > MaxInterval) Render();
        }

        public void Render()
        {
            if (_p == null) return;
            _p.RenderProbe();
            _last = transform.position; _lastTime = Time.time; Renders++;
        }
    }
}
