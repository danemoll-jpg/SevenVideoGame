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

        /// <summary>Like <see cref="Sweep"/> but floors / ramps (normals within ~45° of up) never count - the land probe
        /// slides along walls and props, it does not catch on the ground it stands on.</summary>
        public static bool SweepWalls(Vector3 o, float r, Vector3 dir, float len, Transform[] ign, out RaycastHit best)
        {
            best = default; best.distance = float.MaxValue; bool any = false;
            if (len <= 0f || dir.sqrMagnitude < 1e-10f) return false;
            var d = dir.normalized;
            bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            int n = Physics.SphereCastNonAlloc(o, r, d, _h, len, Mask, QueryTriggerInteraction.Ignore);
            Physics.queriesHitBackfaces = bf;
            for (int i = 0; i < n; i++)
            {
                var h = _h[i]; if (h.collider == null || Started(h) || Ignored(h.collider.transform, ign) || Mathf.Abs(h.normal.y) > 0.7f) continue;
                if (h.distance < best.distance) { best = h; any = true; }
            }
            int m = CameraOccluders.SphereCastAll(o, r, d, len, _h);
            for (int i = 0; i < m; i++)
            {
                var h = _h[i]; if (h.collider == null || Started(h) || Mathf.Abs(h.normal.y) > 0.7f) continue;
                var rr = CameraOccluders.RendererOf(h.collider);
                if (rr != null && (Ignored(rr.transform, ign) || Soft(rr))) continue;
                if (h.distance < best.distance) { best = h; any = true; }
            }
            return any;
        }
        /// <summary>QF18 (10): what never stops her or the bike - grass and other soft growth (blades she walks through).</summary>
        static bool Soft(Renderer r) { if (r.name.StartsWith("Grass")) return true; var m = r.sharedMaterial; return m != null && m.shader != null && m.shader.name.Contains("Grass"); }

        /// <summary>QF18 (10): collision on land (the bike and 7 on foot, every world). Why QF15's fix did not cover it: QF15
        /// swept only the swimming bike; on land the bike and 7 were kept on the walkable zones alone, so wherever two zones
        /// overlapped past a wall (the west ramp's foot: a 0.6 m corner of overlap behind the bank wall) they went straight
        /// through it. Now the horizontal move is swept with a body sphere (centre h above the higher of the two ground
        /// heights, radius r) against the real colliders and every visual mesh, sliding along up to three surfaces.</summary>
        public static Vector3 SlideLand(Vector3 from, Vector3 to, float h, float r, Transform[] ign, out bool hit)
        {
            hit = false;
            if (!CameraOccluders.Ready) return to;
            float y = Mathf.Max(from.y, to.y) + h;
            var c0 = new Vector3(from.x, y, from.z);
            var mv = new Vector3(to.x - from.x, 0f, to.z - from.z);
            for (int it = 0; it < 3; it++)
            {
                float len = mv.magnitude; if (len < 1e-4f) break;
                if (!SweepWalls(c0, r, mv, len + LandSkin, ign, out var hh)) { c0 += mv; mv = Vector3.zero; break; }
                hit = true; LastLandHit = hh.collider != null ? (CameraOccluders.RendererOf(hh.collider)?.name ?? hh.collider.name) + $" n{hh.normal:0.00} d{hh.distance:0.00}" : "?";
                float go = Mathf.Max(0f, hh.distance - LandSkin);
                var md = mv / len;
                c0 += md * go;
                var nrm = hh.normal; nrm.y = 0f; if (nrm.sqrMagnitude < 1e-6f) break;
                nrm.Normalize(); if (Vector3.Dot(nrm, md) > 0f) nrm = -nrm;
                mv = Vector3.ProjectOnPlane(md * (len - go), nrm); mv.y = 0f;
            }
            return new Vector3(c0.x, to.y, c0.z);
        }
        public const float LandSkin = 0.04f;
        public static string LastLandHit;
        /// <summary>QF18 (10) measure: is a body sphere at p (h up, radius r) inside any wall / prop? (floors never count)</summary>
        public static bool LandInside(Vector3 p, float h, float r, Transform[] ign, out string what)
        {
            what = null;
            var c = p + Vector3.up * h;
            foreach (var dir in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, (Vector3.forward + Vector3.left).normalized, (Vector3.forward + Vector3.right).normalized, (Vector3.back + Vector3.left).normalized, (Vector3.back + Vector3.right).normalized })
            {
                // a short outward ray from the centre: hitting a wall face (from either side) inside r = the body is in it
                if (SweepWalls(c - dir * 0.01f, 0.02f, dir, r * 0.85f, ign, out var hh)) { what = hh.collider != null ? (CameraOccluders.RendererOf(hh.collider)?.name ?? hh.collider.name) : "?"; return true; }
            }
            return false;
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
