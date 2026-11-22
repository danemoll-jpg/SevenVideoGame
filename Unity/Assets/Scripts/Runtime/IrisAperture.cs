using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF23 (4) - the mothership's door: a real camera-shutter aperture, not a fading hole. N overlapping curved leaves, each on
    /// its own pivot round a ring behind the hull, swing together (the ring turns with them) and retract in a spiral; shut,
    /// they meet in the middle. Built in the iris's local XY plane (local +z = out of the hull, toward the saucer). The open
    /// angle is solved numerically so the clear opening is <c>clearR</c> ("only just wider than the saucer"), and the leaf
    /// width so the shut leaves cover the whole hole (both measured and logged).
    /// </summary>
    public class IrisAperture
    {
        public Transform Root; readonly List<Transform> _pivots = new List<Transform>(); readonly List<Vector2[]> _outline = new List<Vector2[]>();
        Transform _ring; float _openDeg = 60f, _sign = 1f, _holeR, _pivotR; int _n;
        public float Open { get; private set; }
        public float OpenDeg => _openDeg * _sign;
        public float ClearRadius { get; private set; }
        public float Coverage { get; private set; }

        public static IrisAperture Build(Transform parent, float holeR, float clearR, int n, Material leaf, Material leafAlt, Material ring)
        {
            var ia = new IrisAperture { _holeR = holeR, _pivotR = holeR * 1.28f, _n = n };
            ia.Root = new GameObject("IrisAperture").transform; ia.Root.SetParent(parent, false);
            // solve the leaf width so the shut leaves cover the hole
            float wk = 1f;
            for (; wk < 2.6f; wk += 0.1f) { ia.MakeOutlines(wk); if (ia.CoverageAt(0f) >= 0.998f) break; }
            ia.Coverage = ia.CoverageAt(0f);
            // solve the open angle: the smallest swing (either way) that clears clearR
            float best = 999f, bestS = 1f;
            foreach (var s in new[] { 1f, -1f })
                for (float a = 5f; a <= 150f; a += 1f) if (ia.MinDist(a * s) >= clearR) { if (a < best) { best = a; bestS = s; } break; }
            ia._openDeg = best < 999f ? best : 90f; ia._sign = bestS; ia.ClearRadius = ia.MinDist(ia._openDeg * ia._sign);
            // meshes
            for (int i = 0; i < n; i++)
            {
                float th = i * 360f / n * Mathf.Deg2Rad;
                var piv = new GameObject("Leaf" + i).transform; piv.SetParent(ia.Root, false);
                piv.localPosition = new Vector3(Mathf.Cos(th) * ia._pivotR, Mathf.Sin(th) * ia._pivotR, -0.25f - 0.06f * i);   // stacked: they overlap, never z-fight
                var g = new GameObject("LeafMesh"); g.transform.SetParent(piv, false);
                var o = ia._outline[i]; var mesh = new Mesh { name = "IrisLeaf" };
                var v = new List<Vector3>(); var t = new List<int>();
                int half = o.Length / 2;
                var pivot2 = new Vector2(Mathf.Cos(th), Mathf.Sin(th)) * ia._pivotR;
                // two faces, each with its own vertices and an explicit normal (out of the hull / into it)
                var nm = new List<Vector3>();
                for (int side = 0; side < 2; side++)
                {
                    int b = v.Count;
                    for (int k = 0; k < o.Length; k++) { v.Add((Vector3)(o[k] - pivot2) + new Vector3(0f, 0f, side == 0 ? 0.03f : -0.03f)); nm.Add(side == 0 ? Vector3.forward : Vector3.back); }
                    for (int k = 0; k + 1 < half; k++)
                    {
                        int a0 = b + k, a1 = b + k + 1, b0 = b + o.Length - 1 - k, b1 = b + o.Length - 2 - k;
                        if (side == 0) t.AddRange(new[] { a0, b0, a1, a1, b0, b1 }); else t.AddRange(new[] { a0, a1, b0, a1, b1, b0 });
                    }
                }
                mesh.SetVertices(v); mesh.SetNormals(nm); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
                g.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = i % 2 == 0 ? leaf : leafAlt; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // a fine edge line along each leaf's leading edge (the spiral reads in motion)
                var e = new GameObject("LeafEdge"); e.transform.SetParent(piv, false); e.transform.localPosition = new Vector3(0f, 0f, 0.02f);
                var em = new Mesh(); var ev = new List<Vector3>(); var et = new List<int>();
                for (int k = 0; k < half; k++) { var p = (Vector3)(o[k] - pivot2); var q = k + 1 < half ? (Vector3)(o[k + 1] - pivot2) : p; var d = (q - p).normalized; var nn = new Vector3(-d.y, d.x, 0f) * 0.07f; ev.Add(p - nn); ev.Add(p + nn); if (k > 0) { int b = k * 2; et.AddRange(new[] { b - 2, b, b - 1, b - 1, b, b + 1, b - 2, b - 1, b, b - 1, b + 1, b }); } }
                em.SetVertices(ev); em.SetTriangles(et, 0); em.RecalculateBounds();
                e.AddComponent<MeshFilter>().sharedMesh = em; var er = e.AddComponent<MeshRenderer>(); er.sharedMaterial = Markers.Additive(0.45f); er.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ia._pivots.Add(piv);
            }
            // the rotating drive ring behind the hull edge (it turns with the leaves)
            ia._ring = new GameObject("IrisDriveRing").transform; ia._ring.SetParent(ia.Root, false); ia._ring.localPosition = new Vector3(0f, 0f, -0.15f);
            ia._ring.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ia._ring.gameObject.AddComponent<MeshFilter>().sharedMesh = Markers.RingMesh(holeR * 1.02f, holeR * 1.55f, 0f);
            var rr = ia._ring.gameObject.AddComponent<MeshRenderer>(); rr.sharedMaterial = ring; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ia.Set(0f);
            Debug.Log($"[Seven] IRIS (QF23 4): {n} overlapping curved leaves on pivots at r {ia._pivotR:0.0} m round a {holeR:0.0} m hole; shut they cover {ia.Coverage * 100f:0.0}% of it (leaf width x{wk:0.0}); open = a {ia._openDeg:0} deg swing -> clear radius {ia.ClearRadius:0.00} m (wanted {clearR:0.00} m: just wider than the saucer)");
            return ia;
        }

        void MakeOutlines(float wk)
        {
            _outline.Clear();
            for (int i = 0; i < _n; i++)
            {
                float th = i * 360f / _n * Mathf.Deg2Rad;
                var P = new Vector2(Mathf.Cos(th), Mathf.Sin(th)) * _pivotR;
                // the centreline: an arc of radius rho through the pivot and the centre, continuing 30 deg past it
                float rho = _pivotR * 0.78f, h = Mathf.Sqrt(Mathf.Max(0.01f, rho * rho - (_pivotR * 0.5f) * (_pivotR * 0.5f)));
                var mid = P * 0.5f; var perp = new Vector2(-P.y, P.x).normalized;
                var C = mid + perp * h;
                float a0 = Mathf.Atan2(P.y - C.y, P.x - C.x), aO = Mathf.Atan2(-C.y, -C.x);
                float sweep = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, aO * Mathf.Rad2Deg) * Mathf.Deg2Rad; float a1 = a0 + sweep * 1.45f;
                int seg = 18; var outer = new Vector2[seg + 1]; var inner = new Vector2[seg + 1];
                for (int k = 0; k <= seg; k++)
                {
                    float u = k / (float)seg, a = Mathf.Lerp(a0, a1, u);
                    var c = C + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rho; var nr = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    float w = Mathf.Lerp(0.34f, 0.16f, u) * _holeR * wk * (u < 0.06f ? 0.6f + u / 0.06f * 0.4f : 1f);
                    outer[k] = c + nr * w * 0.5f; inner[k] = c - nr * w * 0.5f;
                }
                var o = new Vector2[(seg + 1) * 2];
                for (int k = 0; k <= seg; k++) { o[k] = outer[k]; o[(seg + 1) * 2 - 1 - k] = inner[k]; }
                _outline.Add(o);
            }
        }
        Vector2 Rot(Vector2 p, Vector2 piv, float deg) { float a = deg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a); var d = p - piv; return piv + new Vector2(d.x * c - d.y * s, d.x * s + d.y * c); }
        float MinDist(float deg)
        {
            float m = 999f;
            for (int i = 0; i < _n; i++)
            {
                float th = i * 360f / _n * Mathf.Deg2Rad; var piv = new Vector2(Mathf.Cos(th), Mathf.Sin(th)) * _pivotR;
                var o = _outline[i];
                for (int k = 0; k < o.Length; k++)
                {
                    var a = Rot(o[k], piv, deg); var b = Rot(o[(k + 1) % o.Length], piv, deg);
                    // distance from the centre to the edge segment
                    var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(-a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                    m = Mathf.Min(m, (a + ab * t).magnitude);
                }
                if (PointIn(Vector2.zero, o, piv, deg)) m = 0f;
            }
            return m;
        }
        bool PointIn(Vector2 p, Vector2[] o, Vector2 piv, float deg)
        {
            bool c = false;
            for (int i = 0, j = o.Length - 1; i < o.Length; j = i++)
            {
                var a = Rot(o[i], piv, deg); var b = Rot(o[j], piv, deg);
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) c = !c;
            }
            return c;
        }
        float CoverageAt(float deg)
        {
            int all = 0, cov = 0;
            for (float x = -_holeR; x <= _holeR; x += _holeR / 14f)
                for (float y = -_holeR; y <= _holeR; y += _holeR / 14f)
                {
                    var p = new Vector2(x, y); if (p.magnitude > _holeR * 0.98f) continue; all++;
                    for (int i = 0; i < _n; i++) { float th = i * 360f / _n * Mathf.Deg2Rad; if (PointIn(p, _outline[i], new Vector2(Mathf.Cos(th), Mathf.Sin(th)) * _pivotR, deg)) { cov++; break; } }
                }
            return all > 0 ? cov / (float)all : 0f;
        }

        /// <summary>A flat annulus in the local XY plane, facing +z (out of the hull), with its own back face - correct normals
        /// both ways (the shared-vertex ring shaded in stripes on a lit material).</summary>
        public static Mesh Annulus(float r0, float r1, int seg = 96)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            for (int side = 0; side < 2; side++)
            {
                int b = v.Count;
                for (int i = 0; i <= seg; i++) { float a = i * Mathf.PI * 2f / seg; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f); v.Add(d * r0); v.Add(d * r1); n.Add(side == 0 ? Vector3.forward : Vector3.back); n.Add(side == 0 ? Vector3.forward : Vector3.back); }
                for (int i = 0; i < seg; i++) { int a = b + i * 2; if (side == 0) t.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2 }); else t.AddRange(new[] { a, a + 3, a + 1, a, a + 2, a + 3 }); }
            }
            var m = new Mesh { name = "Annulus" }; m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds(); return m;
        }

        /// <summary>0 = shut, 1 = open (the swing solved at build).</summary>
        public void Set(float k)
        {
            Open = Mathf.Clamp01(k);
            float deg = _openDeg * _sign * Open;
            foreach (var p in _pivots) p.localRotation = Quaternion.Euler(0f, 0f, deg);
            if (_ring != null) _ring.localRotation = Quaternion.Euler(0f, 0f, deg * 0.5f) * Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
