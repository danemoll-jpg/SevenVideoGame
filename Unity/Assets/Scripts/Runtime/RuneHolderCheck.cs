using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF43 (19): a rune never passes through its holder. While watched (a touch, a rise, a lift out), every frame the rune's
    /// convex shape is tested against every mesh of its holder (the cube's cage stems, a gimbal's rings, a cradle, a socket) with
    /// Physics.ComputePenetration on temporary colliders; the deepest overlap is logged as RUNE vs HOLDER (QF43 19) when the watch
    /// stops (must be 0 cm). The colliders are on a layer nothing else uses and are removed afterwards.
    /// </summary>
    public class RuneHolderCheck : MonoBehaviour
    {
        string _what; Transform _rune; MeshCollider _r; readonly List<MeshCollider> _h = new List<MeshCollider>();
        float _deepest; int _frames, _hitFrames; string _worst = "-";
        public static int Watched, Failed;

        public static RuneHolderCheck Watch(Transform rune, Transform holder, string what)
        {
            if (rune == null || holder == null) return null;
            var go = new GameObject("QF43_RuneHolderCheck");
            var c = go.AddComponent<RuneHolderCheck>(); c._what = what; c._rune = rune;
            var mf = rune.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                c._r = mf.gameObject.AddComponent<MeshCollider>(); c._r.sharedMesh = mf.sharedMesh; c._r.convex = true; c._r.isTrigger = true;
            }
            foreach (var h in holder.GetComponentsInChildren<MeshFilter>())
            {
                if (h.sharedMesh == null || h.transform.IsChildOf(rune)) continue;
                var mc = h.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = h.sharedMesh; mc.isTrigger = false;
                c._h.Add(mc);
            }
            Watched++;
            return c;
        }

        void LateUpdate()
        {
            if (_r == null) return;
            _frames++;
            Physics.SyncTransforms();
            bool hit = false;
            foreach (var h in _h)
            {
                if (h == null) continue;
                if (Physics.ComputePenetration(_r, _r.transform.position, _r.transform.rotation, h, h.transform.position, h.transform.rotation, out _, out float d) && d > 0.002f)
                {
                    hit = true;
                    if (d > _deepest) { _deepest = d; _worst = h.name; }
                }
            }
            if (hit) _hitFrames++;
        }

        public void Stop()
        {
            string s = $"RUNE vs HOLDER (QF43 19) {_what}: {_frames} frames, overlapping in {_hitFrames}, deepest {_deepest * 100f:0.0} cm{(_hitFrames > 0 ? " (against " + _worst + ")" : "")} - {(_hitFrames == 0 ? "PASS" : "FAIL")}";
            if (_hitFrames > 0) Failed++;
            Debug.Log("[Seven] " + s);
            if (DemoBoot.CaptureDir != null && ChapterCapture.I != null) ChapterCapture.I.Log(s);
            if (_r != null) Destroy(_r);
            foreach (var h in _h) if (h != null) Destroy(h);
            Destroy(gameObject);
        }
        void OnDestroy() { if (_r != null) Destroy(_r); foreach (var h in _h) if (h != null) Destroy(h); }
    }
}
