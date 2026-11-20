using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF15 (2): one set of geometry queries for the bike and the riding camera - the level's real colliders AND every
    /// visual mesh (<see cref="CameraOccluders"/>, both faces, in Europa without the old 60 m size cut), minus the given
    /// roots (7, the bike, characters). Before, the bike's sweep under water saw only the visual-mesh copy's "solid" layer
    /// (no real colliders - the base's outer walls and floors -, no small props, nothing over 60 m), and the swim-volume clamp
    /// that ran after it could push the bike through a wall without any sweep at all.
    /// </summary>
    public static class RideProbe
    {
        const int Mask = ~((1 << 20) | (1 << 22) | (1 << 5) | (1 << 2));
        static readonly RaycastHit[] _h = new RaycastHit[64];
        static readonly Collider[] _c = new Collider[64];

        static bool Ignored(Transform t, Transform[] ign)
        {
            if (ign != null) foreach (var r in ign) if (r != null && t.IsChildOf(r)) return true;
            return false;
        }
        static bool Drawn(Renderer r) => r != null && r.enabled && !r.forceRenderingOff && r.gameObject.activeInHierarchy;
        static Renderer MainRenderer(Collider c) { var r = c.GetComponent<Renderer>(); return r != null ? r : c.GetComponentInParent<Renderer>(); }
        static bool Started(RaycastHit h) => h.distance <= 0f && h.point == Vector3.zero;   // an initial overlap (no contact point)

        /// <summary>Nearest hit of a sphere swept from o along dir (initial overlaps ignored); skip = renderers not counted.</summary>
        public static bool Cast(Vector3 o, float r, Vector3 dir, float len, Transform[] ign, HashSet<Renderer> skip, out float dist)
        {
            bool any = Sweep(o, r, dir, len, ign, out var hit, skip);
            dist = any ? hit.distance : len;
            return any;
        }

        public static bool Sweep(Vector3 o, float r, Vector3 dir, float len, Transform[] ign, out RaycastHit best, HashSet<Renderer> skip = null)
        {
            best = default; best.distance = float.MaxValue; bool any = false;
            if (len <= 0f || dir.sqrMagnitude < 1e-10f) return false;
            var d = dir.normalized;
            bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            int n = Physics.SphereCastNonAlloc(o, r, d, _h, len, Mask, QueryTriggerInteraction.Ignore);
            Physics.queriesHitBackfaces = bf;
            for (int i = 0; i < n; i++)
            {
                var h = _h[i]; if (h.collider == null || Started(h) || Ignored(h.collider.transform, ign)) continue;
                if (skip != null) { var mr = MainRenderer(h.collider); if (mr != null && skip.Contains(mr)) continue; }
                if (h.distance < best.distance) { best = h; any = true; }
            }
            int m = CameraOccluders.SphereCastAll(o, r, d, len, _h);
            for (int i = 0; i < m; i++)
            {
                var h = _h[i]; if (h.collider == null || Started(h)) continue;
                var rr = CameraOccluders.RendererOf(h.collider);
                if (rr != null && (Ignored(rr.transform, ign) || (skip != null && skip.Contains(rr)))) continue;
                if (h.distance < best.distance) { best = h; any = true; }
            }
            return any;
        }

        static SphereCollider _probe;
        /// <summary>Push a sphere at c out of anything it already overlaps (a sweep that starts inside a surface does not see
        /// it). Returns the total push.</summary>
        public static float Depenetrate(ref Vector3 c, float r, Transform[] ign)
        {
            if (_probe == null)
            {
                // an enabled sphere far below everything, in the occluder scene on a layer no query uses (a disabled one
                // made ComputePenetration fail: the first wall test pushed nothing out)
                var go = new GameObject("RideProbeSphere") { layer = CameraOccluders.Probe };
                if (!CameraOccluders.Adopt(go)) Object.DontDestroyOnLoad(go);
                go.transform.position = new Vector3(0f, -20000f, 0f);
                _probe = go.AddComponent<SphereCollider>(); _probe.radius = 1f;
            }
            _probe.radius = r;
            float total = 0f;
            for (int pass = 0; pass < 3; pass++)
            {
                bool moved = false;
                int n = Physics.OverlapSphereNonAlloc(c, r, _c, Mask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n && i < 16; i++)
                {
                    var col = _c[i]; if (col == null || Ignored(col.transform, ign)) continue;
                    if (Physics.ComputePenetration(_probe, c, Quaternion.identity, col, col.transform.position, col.transform.rotation, out var d, out var dist) && dist > 1e-4f)
                    { c += d * (dist + 0.01f); total += dist; moved = true; }
                }
                int m = CameraOccluders.OverlapSphere(c, r, _c);
                for (int i = 0; i < m && i < 16; i++)
                {
                    var col = _c[i]; var rr = CameraOccluders.RendererOf(col); if (rr != null && Ignored(rr.transform, ign)) continue;
                    if (Physics.ComputePenetration(_probe, c, Quaternion.identity, col, col.transform.position, col.transform.rotation, out var d, out var dist) && dist > 1e-4f)
                    { c += d * (dist + 0.01f); total += dist; moved = true; }
                }
                if (!moved) break;
            }
            return total;
        }

        /// <summary>Does a sphere at p touch any geometry? drawnOnly: only what is on screen (not cut away / hidden).</summary>
        public static bool Overlap(Vector3 p, float r, Transform[] ign, HashSet<Renderer> skip, bool drawnOnly) => Overlap(p, r, ign, skip, drawnOnly, out _);
        public static bool Overlap(Vector3 p, float r, Transform[] ign, HashSet<Renderer> skip, bool drawnOnly, out string what)
        {
            what = null;
            int n = Physics.OverlapSphereNonAlloc(p, r, _c, Mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = _c[i]; if (c == null || Ignored(c.transform, ign)) continue;
                var mr = MainRenderer(c);
                if (skip != null && mr != null && skip.Contains(mr)) continue;
                if (drawnOnly && !Drawn(mr)) continue;
                what = c.name; return true;
            }
            int m = CameraOccluders.OverlapSphere(p, r, _c);
            for (int i = 0; i < m; i++)
            {
                var rr = CameraOccluders.RendererOf(_c[i]); if (rr == null || Ignored(rr.transform, ign)) continue;
                if (skip != null && skip.Contains(rr)) continue;
                if (drawnOnly && !Drawn(rr)) continue;
                what = rr.name; return true;
            }
            return false;
        }

        /// <summary>Does the segment a-b cross a surface (either face, either direction)?</summary>
        public static bool Crosses(Vector3 a, Vector3 b, Transform[] ign, HashSet<Renderer> skip, bool drawnOnly, out string what)
        {
            what = null;
            var d = b - a; float len = d.magnitude; if (len < 1e-3f) return false;
            foreach (var (o, dir) in new[] { (a, d / len), (b, -d / len) })
            {
                bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
                int n = Physics.RaycastNonAlloc(o, dir, _h, len, Mask, QueryTriggerInteraction.Ignore);
                Physics.queriesHitBackfaces = bf;
                for (int i = 0; i < n; i++)
                {
                    var c = _h[i].collider; if (c == null || Ignored(c.transform, ign)) continue;
                    var mr = MainRenderer(c);
                    if (skip != null && mr != null && skip.Contains(mr)) continue;
                    if (drawnOnly && !Drawn(mr)) continue;
                    what = c.name; return true;
                }
                int m = CameraOccluders.RaycastAll(o, dir, len, _h);
                for (int i = 0; i < m; i++)
                {
                    var rr = CameraOccluders.RendererOf(_h[i].collider); if (rr == null || Ignored(rr.transform, ign)) continue;
                    if (skip != null && skip.Contains(rr)) continue;
                    if (drawnOnly && !Drawn(rr)) continue;
                    what = rr.name; return true;
                }
            }
            return false;
        }
    }
}
