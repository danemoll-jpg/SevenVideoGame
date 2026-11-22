using UnityEngine;

namespace Seven
{
    /// <summary>Round 18: the transit pod rides the elevated loop (CattalusLayout.TransitLoop) at a steady speed.</summary>
    public class CatTransit : MonoBehaviour
    {
        public float Speed = 7f; float _s;
        void Update()
        {
            var L = CattalusLayout.TransitLoop; float total = 0f; for (int i = 0; i < L.Length; i++) total += Vector3.Distance(L[i], L[(i + 1) % L.Length]);
            _s = Mathf.Repeat(_s + Speed * Time.deltaTime, total); float s = _s;
            for (int i = 0; i < L.Length; i++) { var a = L[i]; var b = L[(i + 1) % L.Length]; float d = Vector3.Distance(a, b); if (s <= d) { transform.position = Vector3.Lerp(a, b, s / d); var f = b - a; f.y = 0f; if (f.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(f), Time.deltaTime * 3f); return; } s -= d; }
        }
    }
}
