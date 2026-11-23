using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF28 pass (the image gate): the follow camera avoided walls but not bodies - in the Moon's terrace fight a Scree stepped into
    /// the camera and the whole frame was the inside of its mesh (black, tonal range 0.02). After every camera script has placed
    /// the camera (late), while the player has the camera (no scripted shot), the camera is moved along the line to its target until
    /// it is outside every other character's body. 7 herself is never pushed against (the camera may sit close to her).
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class CameraBodyGuard : MonoBehaviour
    {
        ChapterCamera _cc; readonly List<Renderer> _bodies = new List<Renderer>(); float _next, _nextBikes; HoverBike[] _bikes;
        public static int Pushes;
        void LateUpdate()
        {
            if (_cc == null) _cc = GetComponent<ChapterCamera>();
            if (_cc == null || _cc.Target == null || _cc.InShot) return;
            if (_cc.Target.GetComponentInParent<HoverBike>() != null) return;   // riding: the riding camera has its own handling (and steering is camera-relative)
            if (Time.unscaledTime > _nextBikes) { _nextBikes = Time.unscaledTime + 0.5f; _bikes = FindObjectsByType<HoverBike>(FindObjectsSortMode.None); }
            if (_bikes != null) foreach (var hb in _bikes) if (hb != null && hb.Riding) return;
            if (Time.unscaledTime > _next)
            {
                _next = Time.unscaledTime + 1.5f; _bodies.Clear();
                foreach (var sm in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
                    if (sm.enabled && sm.gameObject.activeInHierarchy && !sm.transform.IsChildOf(_cc.Target.root) && sm.GetComponentInParent<ChapterSeven>() == null && sm.GetComponentInParent<HoverBike>() == null) _bodies.Add(sm);
            }
            var cam = transform.position; var tgt = _cc.Target.position + Vector3.up * 1.2f;
            for (int pass = 0; pass < 3; pass++)
            {
                bool moved = false;
                foreach (var r in _bodies)
                {
                    if (r == null || !r.enabled) continue;
                    var b = r.bounds; b.Expand(-Vector3.Min(b.size * 0.25f, Vector3.one * 0.3f));   // the core of the body, not its outstretched limbs
                    b.Expand(0.25f);                                                               // + the near plane's reach
                    if (!b.Contains(cam)) continue;
                    var d = tgt - cam; float len = d.magnitude; if (len < 0.3f) break;
                    d /= len;
                    for (float s = 0.1f; s < Mathf.Min(len - 0.5f, 4f); s += 0.1f) { if (!b.Contains(cam + d * s)) { cam += d * (s + 0.05f); moved = true; break; } }
                }
                if (!moved) break;
                transform.position = cam; Pushes++;
            }
        }
    }
}
