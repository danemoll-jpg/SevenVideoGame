using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF17 (4) proof: "-rampcheck" (with -marscp n / -chapter2 ...): she rides up and down every ramp (sloped bike zone)
    /// of the world with the real riding camera; every frame it checks that nothing she rides on - the ramp under the bike,
    /// any walkable piece, anything at or below her - is cut away (forceRenderingOff / disabled), and captures each ramp
    /// half way up from the player's camera. Ends "RAMP CHECK (QF17 4): N ramps, frames F, cut-away under her 0".
    /// </summary>
    public static class RampCheck
    {
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        public static IEnumerator Run(HoverBike bike, ChapterSeven seven, ChapterCamera cam, string world, System.Func<string, bool> bikeZone)
        {
            var cap = ChapterCapture.I;
            var ramps = new List<Ground.Zone>();
            foreach (var z in Ground.Zones) if (z.Field == null && !Mathf.Approximately(z.Y0, z.Y1) && bikeZone(z.Name)) ramps.Add(z);
            cap?.Log($"RAMP CHECK (QF17 4) {world}: {ramps.Count} ramps the bike can use: {string.Join(", ", ramps.ConvertAll(z => z.Name))}");
            int frames = 0, bad = 0, n = 0;
            var all = Object.FindObjectsByType<MeshRenderer>();
            foreach (var z in ramps)
            {
                bool was = z.Enabled; z.Enabled = true;
                // ride it from the low end to the high end (along its slope axis), then back down
                Vector3 lo, hi;
                var c = (z.Min + z.Max) * 0.5f;
                if (z.SlopeAxis == 0) { lo = new Vector3(z.Y0 < z.Y1 ? z.Min.x + 1.5f : z.Max.x - 1.5f, 0f, c.y); hi = new Vector3(z.Y0 < z.Y1 ? z.Max.x - 1.5f : z.Min.x + 1.5f, 0f, c.y); }
                else { lo = new Vector3(c.x, 0f, z.Y0 < z.Y1 ? z.Min.y + 1.5f : z.Max.y - 1.5f); hi = new Vector3(c.x, 0f, z.Y0 < z.Y1 ? z.Max.y - 1.5f : z.Min.y + 1.5f); }
                foreach (var (from, to, dir) in new[] { (lo, hi, "up"), (hi, lo, "down") })
                {
                    var f0 = from; f0.y = Ground.Height(from.x, from.z);
                    var d = Flat(to - from).normalized; float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                    if (!bike.Riding) { seven.Place(f0 - Quaternion.Euler(0f, yaw, 0f) * Vector3.right * 1.3f, d); }
                    bike.Place(f0, yaw); if (!bike.Riding) bike.Mount();
                    cam.Target = bike.transform; cam.Distance = 7.6f; cam.LookHeight = 1.35f; cam.AutoBehind = true; cam.DefaultPitch = 12f; cam.Follow(); cam.SnapBehind();
                    bike.ScriptDriven = true;
                    bool shot = false; float len = Flat(to - from).magnitude;
                    for (float t = 0f; t < 12f; t += Time.deltaTime)
                    {
                        var p = bike.transform.position;
                        float along = Vector3.Dot(Flat(p - from), d);
                        if (along > len - 0.5f) break;
                        float err = Vector3.SignedAngle(Flat(bike.transform.forward), Flat(to - p), Vector3.up);
                        bike.ScriptSteer = Mathf.Clamp(err / 30f, -1f, 1f); bike.ScriptThrottle = 0.55f;
                        frames++;
                        // anything under her / walkable that the camera has hidden?
                        foreach (var r in all)
                        {
                            if (r == null || !r.gameObject.activeInHierarchy || !r.forceRenderingOff) continue;
                            if (!CameraOccluders.MayCutAway(r, p)) { bad++; if (bad <= 12) cap?.Log($"RAMP CHECK: '{r.name}' cut away while she rides {z.Name} {dir} at {p:0.0} (bounds {r.bounds.min:0.0}..{r.bounds.max:0.0})"); }
                        }
                        if (!shot && along > len * 0.5f && cap != null) { shot = true; yield return cap.Shot($"ramp_{world}_{z.Name}_{dir}"); }
                        yield return null;
                    }
                    bike.ScriptThrottle = 0f; bike.ScriptSteer = 0f;
                    for (float t = 0f; t < 0.6f; t += Time.deltaTime) yield return null;
                }
                z.Enabled = was; n++;
            }
            cap?.Log($"RAMP CHECK (QF17 4) {world}: {n} ramps ridden up and down, {frames} riding frames, cut-away under her / walkable {bad} (must be 0)");
            cap?.Finish($"RAMP CHECK (QF17 4) {world}: {n} ramps, frames {frames}, cut-away under her {bad}");
        }
    }
}
