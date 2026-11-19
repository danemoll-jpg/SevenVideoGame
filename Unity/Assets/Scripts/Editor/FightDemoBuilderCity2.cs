using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using L = Seven.MoonLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 12 (2e, Dan's rewritten scene 6b) — the city is alive: nearby towers with layered balconies, recessed lit
    /// windows and bridges, built around the older structures; thousands of tiny white lights in rooms and streets that
    /// thin into clusters along the inner curve; cloud banks drifting between districts (hiding tower bases); streams of
    /// vehicle lights on elevated roads and airborne lanes, and headlights far overhead across what looks like the sky;
    /// excavation lights on broken foundations in the lower districts. Grayscale only: the lights are white points /
    /// streaks. All on the city layer (20): never seen from inside the prison. Cheap: one light mesh, a few particle lanes.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        static readonly Vector3 CityEye = new Vector3(16.3f, -0.8f, -45.9f);   // the terrace viewpoint (the lights face it)

        static void BuildLivingCity(Transform root)
        {
            var rnd = new System.Random(62);
            float R() => (float)rnd.NextDouble();
            var lightMat = CityMat("City_lights", 1f, 0.2f, true);
            lightMat.EnableKeyword("_EMISSION"); lightMat.SetColor("_EmissionColor", Color.white * 11f); EditorUtility.SetDirty(lightMat);
            var towerMat = CityMat("City_towers_new", 0.3f, 0.45f);       // darker faces so the lit windows read against them
            var balcMat = CityMat("City_balconies", 0.5f, 0.3f);
            var lv = new List<Vector3>(); var lt = new List<int>();
            int nLights = 0;
            void Dot(Vector3 p, float scale = 1f)
            {
                var to = (CityEye - p); float d = to.magnitude; if (d < 1f) return;
                var f = to / d; var r = Vector3.Cross(Vector3.up, f).normalized; var u = Vector3.Cross(f, r);
                float s = Mathf.Clamp(d * 0.0034f, 0.25f, 12f) * scale;
                int b = lv.Count;
                lv.Add(p - r * s - u * s); lv.Add(p - r * s + u * s); lv.Add(p + r * s + u * s); lv.Add(p + r * s - u * s);
                lt.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 }); nLights++;
            }
            // ---- nearby towers: layered balconies, recessed lit windows (a grid, some rooms lit), bridges between pairs
            var tv = new List<Vector3>(); var tt = new List<int>(); var bv = new List<Vector3>(); var bt = new List<int>();
            var tops = new List<Vector3>();
            for (int i = 0; i < 30; i++)
            {
                float z = -190f - R() * 560f, x = 16f + (R() - 0.5f) * (260f + (-z) * 0.55f);
                if (Mathf.Abs(x - 16f) < 70f && z > -320f) x += x < 16f ? -90f : 90f;      // keep the centre view (the old city) open
                float gy = CityGround(x, z), h = 50f + R() * 170f, w = 16f + R() * 18f;
                var c = new Vector3(x, gy + h * 0.5f, z);
                var q = Quaternion.LookRotation(new Vector3(CityEye.x - x, 0f, CityEye.z - z));
                OBox(tv, tt, c, new Vector3(w, h, w * 0.8f), q);
                OBox(tv, tt, c + Vector3.up * (h * 0.5f + 6f), new Vector3(w * 0.6f, 12f, w * 0.5f), q);      // a smooth crown
                for (float y = gy + 8f; y < gy + h - 4f; y += 7f)                                                  // balconies
                    OBox(bv, bt, new Vector3(x, y, z) + q * new Vector3(0f, 0f, w * 0.4f + 1.4f), new Vector3(w * 0.92f, 0.5f, 2.8f), q);
                // windows on the face toward the terrace: recessed rows, roughly half the rooms lit
                for (float y = gy + 5f; y < gy + h - 3f; y += 3.5f)
                    for (float a = -w * 0.4f; a <= w * 0.4f; a += 2.6f)
                        if (R() < 0.42f) Dot(new Vector3(x, y, z) + q * new Vector3(a, 0f, w * 0.4f + 0.15f), 0.55f);
                tops.Add(new Vector3(x, gy + h * 0.6f, z));
            }
            for (int i = 0; i + 1 < tops.Count; i += 3)
            {
                var a = tops[i]; var b = tops[i + 1]; if (Vector3.Distance(a, b) > 260f) continue;
                var mid = (a + b) * 0.5f; var dir = b - a;
                OBox(bv, bt, mid, new Vector3(4f, 2.5f, dir.magnitude), Quaternion.LookRotation(dir));
                for (float u = 0.1f; u < 0.9f; u += 0.08f) Dot(Vector3.Lerp(a, b, u) + Vector3.up * 1.8f, 0.4f);   // lamps along the bridge
            }
            var tw = CityObject("LiveTowers", tv, tt, towerMat, root);
            var bl = CityObject("LiveBalconies", bv, bt, balcMat, root);
            // ---- thousands of tiny lights along the inner curve, in districts (clusters), thinning with distance
            for (int k = 0; k < 170; k++)
            {
                // a district centre on the shell, in front of the terrace and up the curve (never behind / under the host)
                float az = (R() - 0.5f) * 2.4f, el = -1.35f + R() * 2.1f;
                var dir = new Vector3(Mathf.Sin(az) * Mathf.Cos(el), Mathf.Sin(el), -Mathf.Cos(az) * Mathf.Cos(el)).normalized;
                var cpos = L.ShellCenter + dir * (L.ShellRadius - 8f);
                if (cpos.z > -250f || InHostFootprint(cpos)) continue;
                var up = (L.ShellCenter - cpos).normalized; var t1 = Vector3.Cross(up, Vector3.right).normalized; var t2 = Vector3.Cross(up, t1);
                float spread = 40f + R() * 120f; int n = 18 + rnd.Next(40);
                for (int j = 0; j < n; j++)
                {
                    float g1 = (R() + R() + R() - 1.5f) * spread, g2 = (R() + R() + R() - 1.5f) * spread;
                    var p = cpos + t1 * g1 + t2 * g2; p = L.ShellCenter + (p - L.ShellCenter).normalized * (L.ShellRadius - 6f - R() * 20f);
                    if (p.z > -220f) continue;
                    Dot(p, 0.8f + R() * 0.8f);
                }
            }
            // street lights in the near districts (rows along the terraces)
            for (int k = 0; k < 900; k++) { float x = 16f + (R() - 0.5f) * 1400f, z = -260f - R() * 900f; if (InHostFootprint(new Vector3(x, 0f, z))) continue; Dot(new Vector3(x, CityGround(x, z) + 1.5f, z), 0.6f); }
            // ---- excavation lights in the lower districts: bright clusters on the broken foundations round the mining
            var bc = L.BasinCenter;
            for (int k = 0; k < 9; k++)
            {
                float a = -0.9f + k * 0.22f, r = 260f + R() * 380f;
                var c = new Vector3(bc.x + Mathf.Sin(a) * r, 0f, bc.z + Mathf.Cos(a) * r * 0.6f + 300f); c.y = CityGround(c.x, c.z) + 3f;
                if (c.z > -220f) continue;
                for (int j = 0; j < 14; j++) Dot(c + new Vector3((R() - 0.5f) * 30f, R() * 10f, (R() - 0.5f) * 30f), 1.8f);
            }
            var lights = CityObject("LiveLights", lv, lt, lightMat, root);
            lights.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // ---- traffic: streams of vehicle lights (particles moving along lanes), cheap
            var dotMat = AdditiveMatTex("City_traffic", 2.2f);
            int lanes = 0;
            void Lane(Vector3 a, Vector3 b, float speed, float rate, float size)
            {
                var go = new GameObject($"TrafficLane_{lanes++}"); go.transform.SetParent(root, false); go.transform.position = a; go.transform.rotation = Quaternion.LookRotation(b - a);
                var ps = go.AddComponent<ParticleSystem>(); var mn = ps.main;
                float len = Vector3.Distance(a, b);
                mn.loop = true; mn.playOnAwake = true; mn.startSpeed = speed; mn.startLifetime = len / speed; mn.startSize = size; mn.maxParticles = 600;
                mn.simulationSpace = ParticleSystemSimulationSpace.World; mn.startColor = Color.white; mn.prewarm = true;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(size * 3f, size, 0.1f); sh.alignToDirection = false;
                var vel = ps.velocityOverLifetime; vel.enabled = false;
                var em = ps.emission; em.rateOverTime = rate;
                var pr = ps.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = dotMat; pr.renderMode = ParticleSystemRenderMode.Stretch; pr.lengthScale = 4f; pr.velocityScale = 0.02f;
            }
            // elevated roads through the near districts
            for (int k = 0; k < 5; k++)
            {
                float z = -240f - k * 140f, y = CityGround(16f, z) + 30f + k * 12f;
                Lane(new Vector3(-700f, y, z - 60f), new Vector3(760f, y + 20f, z + 40f), 60f, 7f, 1.2f + k * 0.4f);
                Lane(new Vector3(760f, y + 6f, z + 46f), new Vector3(-700f, y + 6f, z - 54f), 55f, 6f, 1.2f + k * 0.4f);
            }
            // airborne lanes over the districts, and headlights far overhead (across what first looks like the sky)
            for (int k = 0; k < 6; k++)
            {
                float y = 260f + k * 380f, z = -700f - k * 260f;
                Lane(new Vector3(-1500f, y, z), new Vector3(1500f, y + 200f, z - 300f), 140f, 4f, 4f + k * 2f);
            }
            foreach (var (a, b) in new[] { (new Vector3(-2000f, 4200f, -1400f), new Vector3(2200f, 4600f, -600f)), (new Vector3(1800f, 5000f, -2200f), new Vector3(-1900f, 4800f, -300f)), (new Vector3(-600f, 5400f, -2600f), new Vector3(900f, 5200f, 400f)) })
                Lane(a, b, 260f, 3f, 14f);
            // ---- cloud banks drifting between districts, hiding the bases of distant towers
            var cloudGo = new GameObject("CloudBanks"); cloudGo.transform.SetParent(root, false); cloudGo.transform.position = new Vector3(16f, bc.y + 160f, -900f);
            {
                var ps = cloudGo.AddComponent<ParticleSystem>(); var mn = ps.main;
                mn.loop = true; mn.prewarm = true; mn.startLifetime = 140f; mn.startSpeed = 0f; mn.startSize = new ParticleSystem.MinMaxCurve(260f, 520f); mn.maxParticles = 60;
                mn.startColor = new Color(0.85f, 0.85f, 0.85f, 0.16f); mn.simulationSpace = ParticleSystemSimulationSpace.World;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(2200f, 500f, 1500f);
                var em = ps.emission; em.rateOverTime = 0.35f;
                var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World; vel.x = new ParticleSystem.MinMaxCurve(3f, 7f); vel.y = 0f; vel.z = 0f;
                var col = ps.colorOverLifetime; col.enabled = true; var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) }); col.color = gr;
                ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = AdditiveMatTex("City_cloud", 0.55f);
            }
            Log($"living city (2e): {nLights} tiny lights, {tops.Count} towers with balconies + windows, {lanes} traffic lanes, cloud banks, excavation light clusters");
        }

        /// <summary>A soft white dot material for particles (additive, textured).</summary>
        static Material AdditiveMatTex(string name, float intensity)
        {
            var m = AdditiveMat(name, intensity);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(GenDir + "/City_dot.asset") ?? FxDotTexture();   // never re-create it (the dust uses it)
            if (tex != null) m.SetTexture("_MainTex", tex);
            EditorUtility.SetDirty(m); return m;
        }
    }
}
