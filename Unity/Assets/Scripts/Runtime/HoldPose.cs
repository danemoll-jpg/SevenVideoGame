using UnityEngine;

namespace Seven
{
    /// <summary>QF28 (5): 7 holds an object with both hands (or one) - the cupped grip of <see cref="HandHold.Cup"/>, eased in and out,
    /// after the animator and after SevenTableIK (which must not hold the same hands meanwhile).</summary>
    [DefaultExecutionOrder(260)]
    public class HoldPose : MonoBehaviour
    {
        public Transform Target; public bool Left, Right;
        HandHold.Surface _s; Animator _an; float _w, _want; string _what;
        public float Weight => _w;
        public static HoldPose On(GameObject seven) => seven.GetComponent<HoldPose>() ?? seven.AddComponent<HoldPose>();
        /// <summary>Hold <paramref name="t"/> (its mesh = the surface; radius &gt; 0 = a sphere of that radius).</summary>
        public void Hold(Transform t, bool left, bool right, string what, float radius = 0f)
        {
            Target = t; Left = left; Right = right; _what = what; _want = 1f;
            var mf = t != null ? t.GetComponentInChildren<MeshFilter>() : null;
            _s = radius > 0f ? new HandHold.Surface(t, radius) : mf != null ? new HandHold.Surface(mf.transform, mf.sharedMesh) : null;
        }
        public void Release(string why) { if (_want > 0f) Debug.Log($"[Seven] hold released ({why})"); _want = 0f; }
        void LateUpdate()
        {
            if (_an == null) _an = GetComponentInChildren<Animator>();
            _w = Mathf.MoveTowards(_w, Target != null && Target.gameObject.activeInHierarchy ? _want : 0f, Time.deltaTime * 2.5f);
            if (_an == null || _s == null || _w <= 0.001f) return;
            HandHold.Cup(_an, transform, _s, Left, Right, Mathf.SmoothStep(0f, 1f, _w));
            if (_w > 0.99f && DemoBoot.CaptureDir != null)
            {
                if (Left) HandHold.Note(_what + " (left)", HandHold.Measure(_an, false, _s));
                if (Right) HandHold.Note(_what + " (right)", HandHold.Measure(_an, true, _s));
            }
        }
    }
}
