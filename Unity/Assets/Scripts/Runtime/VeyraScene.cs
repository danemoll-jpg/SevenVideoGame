using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF23 (1) — Veyra and the Biavian mothership, ONE builder for every view of it (BIA_01's extreme wide + approach, the
    /// departure, the window view aboard while the saucer is there). The setting correction: the mothership is "stationed
    /// among the rings of Veyra" - so the ring plane runs through its position: ring bands (with gaps) all round, drifting ice
    /// and debris in front of it and behind it, the planet huge beyond. The saucer's viewpoint (<c>origin</c>) is itself
    /// inside the ring system, a little above the plane (BIA_01: "the saucer above a broad band of pale debris").
    /// The hull is WHITE (Dan, QF23 1a): smooth, pearly, luminous - the brightest object in the scene - with faint seams and
    /// hull lights. Strictly monochrome. <c>scale</c> shrinks the whole composition (the window view: the Saucer scene's
    /// camera reaches 2.5 km).
    /// </summary>
    public class VeyraScene
    {
        public Transform Root, Planet, Egg; public Transform[] Orbs; public Vector3 RingNormal, Origin; public float Scale;
        readonly List<(Transform t, Vector3 axisPos, float a, float r, float h, float w)> _drift = new List<(Transform, Vector3, float, float, float, float)>();
        /// <summary>Where the egg's centre sits relative to the origin (scaled).</summary>
        public Vector3 EggPos => Egg != null ? Egg.position : Origin;

        static Material _pearl, _ice;
        /// <summary>The mothership's hull: pearly white, a soft self-glow so it is the brightest thing in the frame.</summary>
        public static Material Pearl()
        {
            if (_pearl != null) return _pearl;
            var sh = Shader.Find("Seven/DissolveLit") ?? Shader.Find("Universal Render Pipeline/Lit");
            _pearl = new Material(sh) { name = "Veyra_pearl_hull" };
            if (_pearl.HasProperty("_BaseColor")) _pearl.SetColor("_BaseColor", new Color(0.96f, 0.96f, 0.96f));
            if (_pearl.HasProperty("_Smoothness")) _pearl.SetFloat("_Smoothness", 0.78f);
            if (_pearl.HasProperty("_Metallic")) _pearl.SetFloat("_Metallic", 0.05f);
            if (_pearl.HasProperty("_EmissionColor")) { _pearl.EnableKeyword("_EMISSION"); _pearl.SetColor("_EmissionColor", new Color(0.42f, 0.42f, 0.42f)); }
            return _pearl;
        }
        /// <summary>Ring ice: pale, matte grey-white.</summary>
        public static Material Ice()
        {
            if (_ice != null) return _ice;
            var sh = Shader.Find("Seven/DissolveLit") ?? Shader.Find("Universal Render Pipeline/Lit");
            _ice = new Material(sh) { name = "Veyra_ring_ice" };
            if (_ice.HasProperty("_BaseColor")) _ice.SetColor("_BaseColor", new Color(0.62f, 0.62f, 0.62f));
            if (_ice.HasProperty("_Smoothness")) _ice.SetFloat("_Smoothness", 0.35f);
            if (_ice.HasProperty("_EmissionColor")) { _ice.EnableKeyword("_EMISSION"); _ice.SetColor("_EmissionColor", new Color(0.08f, 0.08f, 0.08f)); }
            return _ice;
        }

        GameObject Prim(PrimitiveType t, string n, Vector3 at, Vector3 size, Material m, Transform parent = null)
        {
            var g = GameObject.CreatePrimitive(t); g.name = n; Object.Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent != null ? parent : Root, false); g.transform.position = at; g.transform.localScale = size;
            var r = g.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }

        /// <summary>Build round <paramref name="origin"/> (the saucer's viewpoint). The planet lies ahead (+z), its rings tilted
        /// so they cross the frame diagonally; the egg sits IN the ring plane ahead and to the right.</summary>
        public static VeyraScene Build(Transform parent, Vector3 origin, float scale, int seed = 17, bool orbs = true)
        {
            var v = new VeyraScene { Origin = origin, Scale = scale };
            v.Root = new GameObject("VeyraScene").transform; if (parent != null) v.Root.SetParent(parent, false); v.Root.position = Vector3.zero;
            float s = scale;
            var n = Quaternion.Euler(9f, 0f, -16f) * Vector3.up; v.RingNormal = n;
            var ahead = Vector3.ProjectOnPlane(new Vector3(-0.22f, 0f, 1f), n).normalized;
            float D = 4200f * s;                                                   // planet centre -> the saucer (inside the rings)
            float above = Mathf.Max(60f * s, 45f);                                 // the saucer floats above the ring plane (>= 45 m: never a floor under the glass)
            var V = origin + ahead * D - n * above;
            var dark = Markers.DarkGloss();
            v.Planet = v.Prim(PrimitiveType.Sphere, "Veyra", V, Vector3.one * 3000f * s, dark).transform;
            // the rings: bands with gaps, pale, all round the planet in the plane (we are inside them)
            var rot = Quaternion.FromToRotation(Vector3.up, n);
            foreach (var (r0, r1, k) in new[] { (2200f, 2900f, 0.07f), (3000f, 3600f, 0.11f), (3680f, 4900f, 0.14f), (5000f, 5450f, 0.06f), (5600f, 5800f, 0.035f) })
            {
                var ring = new GameObject("VeyraRing"); ring.transform.SetParent(v.Root, false);
                ring.transform.position = V; ring.transform.rotation = rot;
                ring.AddComponent<MeshFilter>().sharedMesh = Markers.RingMesh(r0 * s, r1 * s, 0f);
                var mr = ring.AddComponent<MeshRenderer>(); mr.sharedMaterial = Markers.Additive(k); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // the egg: in the ring plane, at the saucer's distance from the planet, ~1.6 km ahead and to the right
            var radial = (origin - V); radial = Vector3.ProjectOnPlane(radial, n);
            var eggAt = V + Quaternion.AngleAxis(-22f, n) * radial;
            var hull = Pearl();
            v.Egg = v.Prim(PrimitiveType.Sphere, "Mothership", eggAt, new Vector3(900f, 1200f, 900f) * s, hull).transform;
            v.Egg.rotation = Quaternion.FromToRotation(Vector3.up, n) * Quaternion.Euler(0f, 0f, -12f);
            var rnd = new System.Random(seed);
            float R() => (float)rnd.NextDouble();
            // faint seams (latitude bands) and hull lights on the white hull
            for (int i = 1; i < 6; i++)
            {
                float lat = -0.75f + i * 0.25f, rr = Mathf.Sqrt(1f - lat * lat);
                var seam = new GameObject("HullSeam"); seam.transform.SetParent(v.Egg, false); seam.transform.localPosition = new Vector3(0f, lat * 0.5f, 0f);
                seam.AddComponent<MeshFilter>().sharedMesh = Markers.RingMesh(rr * 0.5f * 1.003f, rr * 0.5f * 1.012f, 0f);
                var sm = seam.AddComponent<MeshRenderer>(); sm.sharedMaterial = Markers.Additive(0.5f); sm.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            for (int i = 0; i < 260; i++)
            {
                var d = new Vector3(R() * 2f - 1f, R() * 2f - 1f, R() * 2f - 1f).normalized;
                var p = eggAt + v.Egg.rotation * Vector3.Scale(d, new Vector3(450f, 600f, 450f) * s) * 1.004f;
                v.Prim(PrimitiveType.Sphere, "HullLight", p, Vector3.one * (i % 9 == 0 ? 8f : 3f) * s, Markers.Additive(i % 9 == 0 ? 1.6f : 0.7f));
            }
            // drifting ice and debris in the ring plane: a broad band under the saucer, in FRONT of the egg and BEHIND it
            var tan = Vector3.Cross(n, radial.normalized);
            var ice = Ice();
            for (int i = 0; i < 700; i++)
            {
                // around the arc between the saucer and beyond the egg (-10..+40 deg), spread across the band, thin in height
                float a = Mathf.Lerp(-10f, 40f, R()), rr = radial.magnitude + (R() * 2f - 1f) * 900f * s, h = (R() * 2f - 1f) * 18f * s;
                h = h - 0.5f * above + 30f * s;                                    // the band sits a little under the saucer
                var p = V + Quaternion.AngleAxis(-a, n) * radial.normalized * rr + n * h;
                float near = Vector3.Distance(p, origin) / s;
                if (Vector3.Distance(p, origin) < 35f) continue;                   // nothing right at the saucer (35 m clear of the ship at any scale)
                float sz = (1.5f + R() * (near < 600f ? 9f : 28f)) * s;
                var g = v.Prim(PrimitiveType.Sphere, "RingIce", p, new Vector3(sz, sz * (0.45f + R() * 0.4f), sz * (0.6f + R() * 0.5f)), i % 4 == 0 ? Markers.Additive(0.32f) : ice);
                g.transform.rotation = Quaternion.Euler(R() * 360f, R() * 360f, R() * 360f);
                v._drift.Add((g.transform, V, a, rr, h, 0.02f + R() * 0.05f));
            }
            // the three guardian orbs on their own orbits round the egg
            if (orbs)
            {
                v.Orbs = new Transform[3];
                for (int i = 0; i < 3; i++)
                {
                    v.Orbs[i] = v.Prim(PrimitiveType.Sphere, "GuardianOrb" + i, eggAt, Vector3.one * (60f - i * 8f) * s, Markers.DarkGloss()).transform;
                    v.Prim(PrimitiveType.Sphere, "OrbGlow", v.Orbs[i].position, Vector3.one * 0.35f, Markers.Additive(1.4f), v.Orbs[i]).transform.localPosition = new Vector3(0f, 0f, -0.48f);
                }
            }
            Debug.Log($"[Seven] VEYRA (QF23 1): the white mothership in Veyra's ring plane - egg {Vector3.Distance(eggAt, origin):0} m from the viewpoint, " +
                      $"planet {Vector3.Distance(V, origin):0} m, rings {2200f * s:0}-{5800f * s:0} m round it (the viewpoint is inside them, {60f * s:0} m above the plane), {v._drift.Count} pieces of ring ice");
            return v;
        }

        /// <summary>The ice drifts slowly round the planet; the orbs revolve (except a held one).</summary>
        public void Tick(int heldOrb = -1)
        {
            float t = Time.time;
            for (int i = 0; i < _drift.Count; i++)
            {
                var (tr, V, a, rr, h, w) = _drift[i]; if (tr == null) continue;
                var radial = Vector3.ProjectOnPlane(Origin - V, RingNormal).normalized;
                tr.position = V + Quaternion.AngleAxis(-a - t * w * 0.05f, RingNormal) * radial * rr + RingNormal * h;
                tr.Rotate(0f, w * 20f * Time.deltaTime, 0f, Space.Self);
            }
            if (Orbs != null && Egg != null)
                for (int i = 0; i < Orbs.Length; i++)
                    if (Orbs[i] != null && i != heldOrb)
                        Orbs[i].position = Egg.position + Quaternion.AngleAxis(t * (6f + i * 3f) + i * 120f, RingNormal) * Quaternion.AngleAxis(20f * i - 15f, Vector3.right) * new Vector3(0f, 0f, (820f + i * 160f) * Scale);
        }
    }
}
