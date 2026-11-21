using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>Suspended rail carriers moving along a closed path between the factory terraces.</summary>
    public class AstRailCarrier : MonoBehaviour
    {
        public Vector3[] Path; public Transform[] Cars; public float Speed = 7f; public bool Essential;
        float _s; float[] _cum;
        void Start()
        {
            if (Path == null || Path.Length < 2) { enabled = false; return; }
            _cum = new float[Path.Length + 1];
            for (int i = 0; i < Path.Length; i++) _cum[i + 1] = _cum[i] + Vector3.Distance(Path[i], Path[(i + 1) % Path.Length]);
        }
        public Vector3 At(float s, out Vector3 dir)
        {
            float L = _cum[_cum.Length - 1]; s = ((s % L) + L) % L;
            for (int i = 0; i < Path.Length; i++)
                if (s <= _cum[i + 1]) { var a = Path[i]; var b = Path[(i + 1) % Path.Length]; dir = (b - a).normalized; return Vector3.Lerp(a, b, (s - _cum[i]) / Mathf.Max(0.01f, _cum[i + 1] - _cum[i])); }
            dir = Vector3.forward; return Path[0];
        }
        void Update()
        {
            if (_cum == null) return;
            _s += Speed * AstProduction.For(Essential) * Time.deltaTime;
            float L = _cum[_cum.Length - 1];
            for (int i = 0; i < Cars.Length; i++)
            {
                if (Cars[i] == null) continue;
                var p = At(_s + L * i / Cars.Length, out var d);
                Cars[i].position = p; if (d.sqrMagnitude > 0.01f) Cars[i].rotation = Quaternion.LookRotation(new Vector3(d.x, 0f, d.z).sqrMagnitude > 0.001f ? new Vector3(d.x, 0f, d.z) : Vector3.forward);
            }
        }
    }
}
