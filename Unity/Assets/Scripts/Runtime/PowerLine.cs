using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF18 (12, 15, 16): a power line / conduit / channel whose light visibly runs along it - the "no filler" payoff and
    /// the "show the problem" beat: power runs from one end and stops where the line is broken; fixed, it runs on to what it
    /// feeds. Built by FightDemoBuilder.PowerLineObj (a glowing core in segments, metal bands). Strictly white emission.
    /// </summary>
    public class PowerLine : MonoBehaviour
    {
        public Renderer[] Segs;
        public Vector3[] Points;
        public float Dim = 0.08f, Lit = 1.5f, Head = 3.2f;
        float[] _lvl;
        public int Count => Segs != null ? Segs.Length : 0;
        public int LitUpTo { get; private set; } = -1;
        public Vector3 Point(int i) => Points != null && Points.Length > 0 ? Points[Mathf.Clamp(i, 0, Points.Length - 1)] : transform.position;

        void Awake() { if (Segs != null && _lvl == null) { _lvl = new float[Segs.Length]; for (int i = 0; i < Segs.Length; i++) Put(i, Dim); } }
        void Put(int i, float v)
        {
            if (Segs == null || i < 0 || i >= Segs.Length || Segs[i] == null) return;
            if (_lvl == null) _lvl = new float[Segs.Length];
            _lvl[i] = v;
            var m = Segs[i].material; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white * v);
        }
        /// <summary>Segments 0..upTo lit at level, the rest dim (instant).</summary>
        public void Set(int upTo, float level = -1f)
        {
            if (level < 0f) level = Lit;
            for (int i = 0; i < Count; i++) Put(i, i <= upTo ? level : Dim);
            LitUpTo = upTo;
        }
        public void All(bool on) => Set(on ? Count - 1 : -1);
        /// <summary>The light runs from segment `from` to `to` (inclusive) in `secs`, a bright head travelling ahead of the lit
        /// part. Returns when it arrives.</summary>
        public IEnumerator Run(int from, int to, float secs, System.Action<Vector3> onHead = null)
        {
            int dir = to >= from ? 1 : -1; int n = Mathf.Abs(to - from) + 1;
            float per = Mathf.Max(0.02f, secs / n);
            for (int k = 0; k < n; k++)
            {
                int i = from + k * dir;
                onHead?.Invoke(Point(i));
                for (float t = 0f; t < per; t += Time.deltaTime)
                {
                    Put(i, Mathf.Lerp(Dim, Head, t / per));
                    if (k > 0) Put(i - dir, Mathf.Lerp(Head, Lit, t / per));
                    yield return null;
                }
            }
            Put(to, Lit);
            LitUpTo = Mathf.Max(LitUpTo, Mathf.Max(from, to));
        }
        /// <summary>A failing flicker at one segment (the break, sparking).</summary>
        public IEnumerator Flicker(int i, float secs)
        {
            for (float t = 0f; t < secs; t += Time.deltaTime) { Put(i, Random.value < 0.3f ? Head : Dim * 2f); yield return null; }
            Put(i, Dim);
        }
    }
}
