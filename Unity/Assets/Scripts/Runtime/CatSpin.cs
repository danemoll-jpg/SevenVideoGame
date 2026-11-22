using UnityEngine;

namespace Seven
{
    /// <summary>Round 18: the prize display turns the trophy slowly (only while it stands on its display plinth).</summary>
    public class CatSpin : MonoBehaviour
    {
        public float DegPerSec = 14f;
        public Transform Also;
        Vector3 _home; bool _set;
        void Start() { _home = transform.position; _set = true; }
        void Update()
        {
            if (!_set || (transform.position - _home).sqrMagnitude > 0.05f) return;   // moved off the display: it stops
            transform.Rotate(0f, DegPerSec * Time.deltaTime, 0f, Space.World);
        }
    }
}
