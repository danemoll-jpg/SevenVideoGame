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
            var g = GameObject.CreatePrimitive(t); g.name = n; if (Application.isPlaying) Object.Destroy(g.GetComponent<Collider>()); else Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent != null ? parent : Root, false); g.transform.position = at; g.transform.localScale = size;
            var r = g.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }

        /// <summary>QF25 (1) — Build round <paramref name="origin"/> (the saucer's viewpoint), framed on the MOTHERSHIP: the egg
        /// lies straight along <paramref name="eggDir"/> (the window's / the shot's direction), ~1.7 km away; Veyra is a planet,
        /// vastly bigger than the 26-mile ship: a huge banded sphere (angular radius ~46 deg) far behind and below the egg, so its
        /// curve fills the lower background and the white egg stands in front of it, its top against space; its rings are broad
        /// pale ellipses round it; the ring ice the mothership is stationed among drifts round the egg in a band at the saucer's
        /// level (in front of the egg and behind it). Angular sizes do not depend on <paramref name="scale"/>; the planet's
        /// distance is capped by <paramref name="farClip"/> (the window view's 2.5 km camera) - same look, nearer.</summary>
        public static VeyraScene Build(Transform parent, Vector3 origin, float scale, int seed = 17, bool orbs = true, Vector3? eggDir = null, float farClip = 60000f)
        {
            var v = new VeyraScene { Origin = origin, Scale = scale };
            All.RemoveAll(x => x == null || x.Root == null); All.Add(v);
            v.Root = new GameObject("VeyraScene").transform; if (parent != null) v.Root.SetParent(parent, false); v.Root.position = Vector3.zero;
            float s = scale;
            var e = (eggDir ?? new Vector3(0f, -0.06f, 1f)).normalized;
            var right = Vector3.Cross(Vector3.up, e).normalized; var up = Vector3.Cross(e, right).normalized;
            // the band the ship sits in: through the egg's equator and a little under the saucer, tilted so it crosses diagonally
            var n = (Quaternion.AngleAxis(-12f, e) * up).normalized; v.RingNormal = n;
            // the planet: far behind and below the egg (centre 32 deg below the egg's direction, a little to the left)
            var pd = (Quaternion.AngleAxis(-9f, up) * Quaternion.AngleAxis(32f, right) * e).normalized;
            float dP = Mathf.Min(30000f * s, farClip * 0.5f), R = dP * Mathf.Sin(46f * Mathf.Deg2Rad);
            var V = origin + pd * dP;
            v.Planet = v.Prim(PrimitiveType.Sphere, "Veyra", V, Vector3.one * 2f * R, PlanetMat()).transform;
            v.Planet.GetComponent<MeshFilter>().sharedMesh = SmoothSphere();
            v.Planet.rotation = Quaternion.FromToRotation(Vector3.up, n);           // its bands follow the ring plane
            // the planet's rings: broad pale ellipses round it, seen from above their plane (gaps between the bands)
            var rot = Quaternion.FromToRotation(Vector3.up, n);
            foreach (var (r0, r1, k) in new[] { (1.22f, 1.48f, 0.26f), (1.52f, 1.78f, 0.40f), (1.82f, 2.02f, 0.22f), (2.08f, 2.16f, 0.14f) })
            {
                var ring = new GameObject("VeyraRing"); ring.transform.SetParent(v.Root, false);
                ring.transform.position = V; ring.transform.rotation = rot;
                ring.AddComponent<MeshFilter>().sharedMesh = FineRing(r0 * R, r1 * R);
                var mr = ring.AddComponent<MeshRenderer>(); mr.sharedMaterial = Markers.Additive(k); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // the egg: straight ahead along eggDir, the brightest thing in the frame, in front of the planet
            float Dg = 1700f * s;
            var eggAt = origin + e * Dg;
            var hull = Pearl();
            v.Egg = v.Prim(PrimitiveType.Sphere, "Mothership", eggAt, new Vector3(900f, 1200f, 900f) * s, hull).transform;
            v.Egg.GetComponent<MeshFilter>().sharedMesh = SmoothSphere();
            v.Egg.rotation = Quaternion.FromToRotation(Vector3.up, n) * Quaternion.Euler(0f, 0f, -6f);
            var rnd = new System.Random(seed);
            float Rn() => (float)rnd.NextDouble();
            for (int i = 1; i < 6; i++)
            {
                float lat = -0.75f + i * 0.25f, rr = Mathf.Sqrt(1f - lat * lat);
                var seam = new GameObject("HullSeam"); seam.transform.SetParent(v.Egg, false); seam.transform.localPosition = new Vector3(0f, lat * 0.5f, 0f);
                seam.AddComponent<MeshFilter>().sharedMesh = Markers.RingMesh(rr * 0.5f * 1.003f, rr * 0.5f * 1.012f, 0f);
                var sm = seam.AddComponent<MeshRenderer>(); sm.sharedMaterial = Markers.Additive(0.5f); sm.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            for (int i = 0; i < 260; i++)
            {
                var d = new Vector3(Rn() * 2f - 1f, Rn() * 2f - 1f, Rn() * 2f - 1f).normalized;
                var p = eggAt + v.Egg.rotation * Vector3.Scale(d, new Vector3(450f, 600f, 450f) * s) * 1.004f;
                v.Prim(PrimitiveType.Sphere, "HullLight", p, Vector3.one * (i % 9 == 0 ? 8f : 3f) * s, Markers.Additive(i % 9 == 0 ? 1.6f : 0.7f));
            }
            // the ring ice the mothership is stationed among: a band round the egg through its equator, the saucer ~40 m above
            // it; pieces in front of the egg and behind it, thin in height, slowly circling the egg
            var ice = Ice();
            float above = Mathf.Max(40f * s, 30f);
            var bandC = eggAt - n * (Vector3.Dot(n, eggAt - origin) + above);       // the band's plane: the saucer 'above' over it
            var radial0 = Vector3.ProjectOnPlane(origin - eggAt, n).normalized;
            v._radial0 = radial0;
            for (int i = 0; i < 700; i++)
            {
                float a = Mathf.Lerp(-80f, 80f, Rn()), rr = Mathf.Lerp(700f, 2600f, Mathf.Pow(Rn(), 0.8f)) * s, h = (Rn() * 2f - 1f) * 22f * s;
                var p = bandC + Quaternion.AngleAxis(a, n) * radial0 * rr + n * h;
                if (Vector3.Distance(p, origin) < 35f) continue;                   // nothing right at the saucer (35 m clear of the ship at any scale)
                float near = Vector3.Distance(p, origin) / s;
                float sz = (1.5f + Rn() * (near < 600f ? 9f : 26f)) * s;
                var g = v.Prim(PrimitiveType.Sphere, "RingIce", p, new Vector3(sz, sz * (0.45f + Rn() * 0.4f), sz * (0.6f + Rn() * 0.5f)), i % 4 == 0 ? Markers.Additive(0.32f) : ice);
                g.transform.rotation = Quaternion.Euler(Rn() * 360f, Rn() * 360f, Rn() * 360f);
                v._drift.Add((g.transform, bandC, a, rr, h, 0.02f + Rn() * 0.05f));
            }
            if (orbs)
            {
                v.Orbs = new Transform[3];
                for (int i = 0; i < 3; i++)
                {
                    v.Orbs[i] = v.Prim(PrimitiveType.Sphere, "GuardianOrb" + i, eggAt, Vector3.one * (60f - i * 8f) * s, Markers.DarkGloss()).transform;
                    v.Prim(PrimitiveType.Sphere, "OrbGlow", v.Orbs[i].position, Vector3.one * 0.35f, Markers.Additive(1.4f), v.Orbs[i]).transform.localPosition = new Vector3(0f, 0f, -0.48f);
                }
            }
            float angP = Mathf.Asin(Mathf.Clamp01(R / dP)) * Mathf.Rad2Deg, angE = Mathf.Atan(600f * s / Dg) * Mathf.Rad2Deg;
            v.PlanetToShipAngle = angP / Mathf.Max(angE, 0.01f);
            Debug.Log($"[Seven] VEYRA (QF25 1): framed on the mothership - egg {Dg:0} m ahead (half-height {angE:0.0} deg), Veyra radius {R:0} m at {dP:0} m " +
                      $"(angular radius {angP:0.0} deg = {v.PlanetToShipAngle:0.0}x the ship), centre {Vector3.Angle(pd, e):0} deg below the egg's direction; " +
                      $"rings {1.22f * R:0}-{2.16f * R:0} m; {v._drift.Count} pieces of ring ice round the ship");
            return v;
        }
        /// <summary>Every Veyra built (the BIA_01 exterior, the window view) - QF25 (1) measures the active ones per shot.</summary>
        public static readonly List<VeyraScene> All = new List<VeyraScene>();
        /// <summary>QF25 (1): what this frame is framed on - rays over a 48x27 grid against the egg and the planet (both
        /// ellipsoids): the share of the frame each covers, whether the ship's centre is in frame. "Mainly the planet" = the
        /// ship out of frame or under 3 % of it while the planet covers more than half.</summary>
        public bool Emphasis(Camera cam, out string info)
        {
            info = "";
            if (cam == null || Root == null || !Root.gameObject.activeInHierarchy || Egg == null || Planet == null) return true;
            int nx = 48, ny = 27, egg = 0, pl = 0;
            for (int y = 0; y < ny; y++) for (int x = 0; x < nx; x++)
            {
                var r = cam.ViewportPointToRay(new Vector3((x + 0.5f) / nx, (y + 0.5f) / ny, 0f));
                float te = Hit(Egg, r), tp = Hit(Planet, r);
                if (te >= 0f && (tp < 0f || te < tp)) egg++; else if (tp >= 0f) pl++;
            }
            var vp = cam.WorldToViewportPoint(Egg.position);
            bool inFrame = vp.z > 0f && vp.x > 0.05f && vp.x < 0.95f && vp.y > 0.05f && vp.y < 0.95f;
            float fe = egg / (float)(nx * ny), fp = pl / (float)(nx * ny);
            bool ok = inFrame || fe > 0.25f || fp < 0.15f;                           // a cabin close-up with neither in frame is fine
            info = $"ship {(inFrame ? "in frame" : "OUT of frame")} (centre {vp.x:0.00},{vp.y:0.00}), ship covers {fe * 100f:0}% of the frame, planet {fp * 100f:0}%";
            return ok;
        }
        static float Hit(Transform t, Ray r)
        {
            var o = t.InverseTransformPoint(r.origin); var d = t.InverseTransformVector(r.direction);   // the scaled direction (an ellipsoid of radius 0.5 in local space)
            float a = Vector3.Dot(d, d), b = 2f * Vector3.Dot(o, d), c = Vector3.Dot(o, o) - 0.25f, disc = b * b - 4f * a * c;
            if (disc < 0f) return -1f; float tt = (-b - Mathf.Sqrt(disc)) / (2f * a); return tt >= 0f ? tt : -1f;
        }
        /// <summary>How many times the planet's angular radius exceeds the ship's (QF25 1: at least 2 in every view).</summary>
        public float PlanetToShipAngle;
        Vector3 _radial0;
        static Material _planet; static Mesh _sphere;
        /// <summary>Veyra's surface: soft grey bands (a gas giant), self-lit enough to read through the haze.</summary>
        static Material PlanetMat()
        {
            if (_planet != null) return _planet;
            var sh = Shader.Find("Seven/DissolveLit") ?? Shader.Find("Universal Render Pipeline/Lit");
            _planet = new Material(sh) { name = "Veyra_planet_bands" };
            var tex = new Texture2D(8, 512, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "Veyra_bands" };
            var px = new Color[8 * 512];
            for (int y = 0; y < 512; y++)
            {
                float u = y / 511f, b = 0.36f + 0.16f * Mathf.Sin(u * 38f) + 0.10f * Mathf.Sin(u * 97f + 1.3f) + 0.08f * (Mathf.PerlinNoise(u * 24f, 0.5f) - 0.5f);
                b *= Mathf.Lerp(0.55f, 1f, Mathf.Sin(u * Mathf.PI));             // darker towards the poles
                for (int x = 0; x < 8; x++) px[y * 8 + x] = new Color(b, b, b, 1f);
            }
            tex.SetPixels(px); tex.Apply(true);
            if (_planet.HasProperty("_BaseMap")) _planet.SetTexture("_BaseMap", tex);
            if (_planet.HasProperty("_MainTex")) _planet.SetTexture("_MainTex", tex);
            // dark bands so the vast curve reads against the bright haze, a bright limb (the atmosphere) on its edge
            if (_planet.HasProperty("_BaseColor")) _planet.SetColor("_BaseColor", new Color(0.30f, 0.30f, 0.30f));
            if (_planet.HasProperty("_Smoothness")) _planet.SetFloat("_Smoothness", 0.1f);
            if (_planet.HasProperty("_EmissionColor")) { _planet.EnableKeyword("_EMISSION"); _planet.SetColor("_EmissionColor", new Color(0.06f, 0.06f, 0.06f)); if (_planet.HasProperty("_EmissionMap")) _planet.SetTexture("_EmissionMap", tex); }
            if (_planet.HasProperty("_Rim")) { _planet.SetFloat("_Rim", 1.4f); if (_planet.HasProperty("_RimPower")) _planet.SetFloat("_RimPower", 2.6f); }
            return _planet;
        }
        /// <summary>A fine UV sphere (diameter 1): the planet's limb and the egg's outline stay round at any size on screen.</summary>
        static Mesh SmoothSphere()
        {
            if (_sphere != null) return _sphere;
            int lon = 160, lat = 80; var vs = new List<Vector3>(); var uv = new List<Vector2>(); var tr = new List<int>();
            for (int j = 0; j <= lat; j++) for (int i = 0; i <= lon; i++)
            {
                float th = j / (float)lat * Mathf.PI, ph = i / (float)lon * Mathf.PI * 2f;
                vs.Add(new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph)) * 0.5f); uv.Add(new Vector2(i / (float)lon, 1f - j / (float)lat));
            }
            for (int j = 0; j < lat; j++) for (int i = 0; i < lon; i++) { int a = j * (lon + 1) + i, b = a + lon + 1; tr.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b }); }
            _sphere = new Mesh { name = "Veyra_sphere", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            _sphere.SetVertices(vs); _sphere.SetUVs(0, uv); _sphere.SetTriangles(tr, 0); _sphere.RecalculateNormals(); _sphere.RecalculateBounds();
            return _sphere;
        }
        /// <summary>A flat ring (both faces) with 360 segments - no visible chords at planet size.</summary>
        static Mesh FineRing(float r0, float r1)
        {
            int n = 360; var vs = new List<Vector3>(); var tr = new List<int>();
            for (int i = 0; i <= n; i++) { float a = i / (float)n * Mathf.PI * 2f; var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)); vs.Add(d * r0); vs.Add(d * r1); }
            for (int i = 0; i < n; i++) { int a = i * 2; tr.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2, a, a + 3, a + 1, a, a + 2, a + 3 }); }
            var m = new Mesh { name = "Veyra_ring" }; m.SetVertices(vs); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        /// <summary>The ice drifts slowly round the ship; the orbs revolve (except a held one).</summary>
        public void Tick(int heldOrb = -1)
        {
            float t = Time.time;
            for (int i = 0; i < _drift.Count; i++)
            {
                var (tr, C, a, rr, h, w) = _drift[i]; if (tr == null) continue;
                tr.position = C + Quaternion.AngleAxis(a + t * w * 0.05f, RingNormal) * _radial0 * rr + RingNormal * h;
                tr.Rotate(0f, w * 20f * Time.deltaTime, 0f, Space.Self);
            }
            if (Orbs != null && Egg != null)
                for (int i = 0; i < Orbs.Length; i++)
                    if (Orbs[i] != null && i != heldOrb)
                        Orbs[i].position = Egg.position + Quaternion.AngleAxis(t * (6f + i * 3f) + i * 120f, RingNormal) * Quaternion.AngleAxis(20f * i - 15f, Vector3.right) * new Vector3(0f, 0f, (820f + i * 160f) * Scale);
        }
    }
}
