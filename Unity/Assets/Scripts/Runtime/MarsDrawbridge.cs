using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>QF18 (11): the drawbridge - the span on its hinge, the chain over the pulley to the drum, the lock. Set(u):
    /// u = 0 raised (chained up), 1 lowered across the channel. The chain's links are laid along tip -> pulley -> drum.</summary>
    public class MarsDrawbridge : MonoBehaviour
    {
        public Transform Span, Drum, Links, Lock; public Renderer LockGlow;
        public Vector3 Pulley, DrumTop, TipLocal;
        public float RaisedDeg = MarsLayout.DrawRaisedDeg;
        public float U { get; private set; }
        public void Set(float u)
        {
            U = u;
            if (Span == null) return;
            Span.localRotation = Quaternion.Euler(0f, 0f, RaisedDeg * (1f - u));
            if (Drum != null) Drum.localRotation = Quaternion.Euler(-u * 1440f, 0f, 90f);
            if (Links == null) return;
            var tip = Span.TransformPoint(TipLocal);
            int n = Links.childCount; float step = 0.24f; int k = 0;
            void Lay(Vector3 a, Vector3 b)
            {
                float L = Vector3.Distance(a, b); var d = (b - a).normalized;
                for (float s = 0f; s < L && k < n; s += step, k++)
                {
                    var c = Links.GetChild(k); c.gameObject.SetActive(true);
                    c.position = a + d * s + Vector3.down * Mathf.Sin(s / Mathf.Max(0.01f, L) * Mathf.PI) * Mathf.Min(0.6f, L * 0.02f);
                    c.rotation = Quaternion.LookRotation(d) * Quaternion.Euler(0f, 0f, k % 2 == 0 ? 0f : 90f);
                }
            }
            Lay(tip, Pulley); Lay(Pulley, DrumTop);
            for (; k < n; k++) Links.GetChild(k).gameObject.SetActive(false);
        }
    }
}
