using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF15 (2): the automated wall test (-europawalltest -capture dir, starts at checkpoint 2). The story flow is stopped;
    /// the bike is put at full speed (25 m/s) in open water all over the chapter (the ice passage, the great chamber under the
    /// ice ceiling and over its floor, the ancient structures and spires, the settlement's platforms, the race course's rings,
    /// pillars and ice shelves, the damaged passage, the industrial approach tunnel and the base's outside, the access bay,
    /// the escape tube, the open water, the temple hall, the way home) and ridden straight into a wall - head-on, at 35 deg
    /// and at 60 deg (sliding along it) - from many angles, up and down too. Every frame the bike's core and its centre's path
    /// (HoverBike.MeasureInside) and the riding camera (ChapterCamera.RideMeasure: inside geometry / behind a drawn surface)
    /// are checked. Ends "WALL TEST (QF15 2): N attempts ... failed attempts F" with every failed spot listed.
    /// </summary>
    public class EuropaWallTest : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (DemoBoot.CaptureDir == null || !DemoBoot.EuropaWallTest || FindAnyObjectByType<EuropaWallTest>() != null) return;
            var go = new GameObject("EuropaWallTest"); DontDestroyOnLoad(go); go.AddComponent<EuropaWallTest>();
        }

        ChapterCapture Cap => ChapterCapture.I;
        void Log(string s) => Cap?.Log("wall test: " + s);
        public const int Attempts = 165;

        static List<(string area, Vector3 p)> Starts()
        {
            var s = new List<(string, Vector3)>();
            void Add(string a, IEnumerable<Vector3> ps) { foreach (var p in ps) s.Add((a, p)); }
            Add("ice passage", new[] { EuropaLayout.Dive[2], EuropaLayout.Dive[3], EuropaLayout.Dive[4], EuropaLayout.Dive[5] });
            Add("great chamber", EuropaLayout.Descent);
            Add("great chamber", new[] { new Vector3(-70f, -60f, 172f), new Vector3(-30f, -70f, 236f), new Vector3(60f, -50f, 174f), new Vector3(110f, -60f, 210f), new Vector3(-96f, -60f, 300f), new Vector3(20f, -60f, 306f),
                                          new Vector3(-40f, -32f, 200f), new Vector3(40f, -32f, 280f), new Vector3(-100f, -110f, 180f), new Vector3(60f, -112f, 230f) });
            Add("settlement", EuropaLayout.ToSettlement);
            Add("settlement", new[] { EuropaLayout.Receiving + new Vector3(0f, 4f, -10f), new Vector3(100f, -60f, 284f), new Vector3(120f, -52f, 256f), new Vector3(84f, -66f, 304f), new Vector3(130f, -62f, 300f) });
            Add("race course", EuropaLayout.RaceGates);
            Add("race course", new[] { EuropaLayout.RacePillars[0] + new Vector3(9f, 0f, 0f), EuropaLayout.RacePillars[1] + new Vector3(-9f, 0f, 2f), EuropaLayout.RacePillars[2] + new Vector3(0f, 2f, -9f), new Vector3(14f, -70f, 300f), new Vector3(-8f, -78f, 262f) });
            Add("damaged passage", new[] { EuropaLayout.DamagedPassage + new Vector3(-8f, 0f, -10f), new Vector3(112f, -68f, 322f) });
            Add("approach tunnel", EuropaLayout.Approach);
            Add("base outside / bay", new[] { EuropaLayout.ObservePoint, EuropaLayout.BayPark + new Vector3(4f, -1f, 0f), new Vector3(-150f, -95f, 446f) });
            Add("escape tube", EuropaLayout.Escape);
            Add("open water + temple", EuropaLayout.ToTemple);
            Add("open water + temple", new[] { EuropaLayout.OpenWaterStop, EuropaLayout.TempleCentre + new Vector3(0f, 6f, 14f), EuropaLayout.TempleCentre + new Vector3(-12f, 4f, 0f), EuropaLayout.TempleCentre + new Vector3(12f, 8f, 6f) });
            Add("way home", EuropaLayout.Home);
            return s;
        }

        IEnumerator Start()
        {
            ChapterCapture.Ensure();
            float t0 = Time.time;
            while (EuropaDirector.I == null || !EuropaDirector.I.Riding || EuropaDirector.I.InCutscene) { if (Time.time - t0 > 90f) { Cap.Finish("WALL TEST (QF15 2): could not get her onto the bike"); yield break; } yield return null; }
            var d = EuropaDirector.I; var bike = d.Bike; var cam = d.Cam;
            yield return new WaitForSeconds(0.5f);
            d.StopAllCoroutines();                                  // the story stands still: nothing fires while the bike is moved about
            bike.ScriptDriven = true; bike.ScriptSteer = 0f; bike.ScriptThrottle = 1f;
            Log($"story flow stopped at '{d.ObjectiveNow}'; {Attempts} attempts at 25 m/s (test speed {DemoBoot.TestSpeed}); occluders {CameraOccluders.Count}");
            var starts = Starts(); var rnd = new System.Random(15);
            int fails = 0, done = 0, slides0 = bike.WallSlides, shots = 0;
            var failList = new List<string>(); var perArea = new Dictionary<string, Vector2Int>();
            var ign = HoverBike.IgnoreRoots;
            for (int i = 0; i < Attempts; i++)
            {
                var (area, s0) = starts[i % starts.Count];
                // a start in open water near the spot
                Vector3 s = s0; bool ok = false;
                for (int k = 0; k < 20 && !ok; k++)
                {
                    s = s0 + new Vector3((float)rnd.NextDouble() * 6f - 3f, (float)rnd.NextDouble() * 4f - 2f, (float)rnd.NextDouble() * 6f - 3f) * (k == 0 ? 0f : 1f + k * 0.2f);
                    ok = EuropaLayout.InSwim(s, 1.4f) && !RideProbe.Overlap(s + Vector3.up * 0.6f, 1.3f, ign, null, false);
                }
                if (!ok) { Log($"#{i} {area}: no open water at {s0} - skipped"); continue; }
                // a wall within reach: random directions (more up / down in the chamber: the ice ceiling and the floor)
                Vector3 dir = Vector3.forward; RaycastHit hit = default; bool found = false;
                int kind = i % 3;   // 0 head-on, 1 at 35 deg, 2 at 60 deg (sliding along it)
                for (int k = 0; k < 60 && !found; k++)
                {
                    float yaw = (float)rnd.NextDouble() * 360f;
                    float pitch = area == "great chamber" && i % 4 == 0 ? (i % 8 == 0 ? 38f : -38f) : (float)rnd.NextDouble() * 60f - 30f;
                    var dd = Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward;
                    if (!RideProbe.Sweep(s + Vector3.up * 0.6f, 1.0f, dd, 55f, ign, out var h) || h.distance < 5f) continue;
                    var n = h.normal; if (Vector3.Dot(n, dd) > 0f) n = -n;
                    if (kind == 0) { dir = dd; hit = h; found = true; break; }
                    var tan = Vector3.ProjectOnPlane(dd, n); if (tan.sqrMagnitude < 1e-3f) tan = Vector3.ProjectOnPlane(Vector3.Cross(n, Vector3.up), n);
                    float a = (kind == 1 ? 35f : 60f) * Mathf.Deg2Rad;
                    var gd = (-n * Mathf.Cos(a) + tan.normalized * Mathf.Sin(a)).normalized;
                    float gp = Mathf.Asin(Mathf.Clamp(gd.y, -1f, 1f)) * Mathf.Rad2Deg;
                    if (Mathf.Abs(gp) > HoverBike.MaxPitchDeg) continue;
                    if (RideProbe.Sweep(s + Vector3.up * 0.6f, 1.0f, gd, 55f, ign, out var h2) && h2.distance > 3f) { dir = gd; hit = h2; found = true; }
                }
                if (!found) { Log($"#{i} {area}: no wall within 55 m of {s} - skipped"); continue; }
                float pitchDeg = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg, -HoverBike.MaxPitchDeg, HoverBike.MaxPitchDeg);
                float yawDeg = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z).sqrMagnitude > 1e-4f ? new Vector3(dir.x, 0f, dir.z) : Vector3.forward).eulerAngles.y;
                bike.TestLaunch(s, yawDeg, pitchDeg, bike.SwimMax);
                bike.ScriptRise = pitchDeg / HoverBike.MaxPitchDeg; bike.ScriptThrottle = 1f;
                cam.SnapBehind();
                int in0 = HoverBike.InsideFrames, tu0 = HoverBike.TunnelFrames, ci0 = ChapterCamera.RideCamInside;
                float run = Mathf.Min(3.6f, hit.distance / bike.SwimMax + 1.8f);
                bool shoot = kind == 2 && shots < 12 && i % 6 == 2;
                for (float t = 0f; t < run; t += Time.deltaTime)
                {
                    if (shoot && t > hit.distance / bike.SwimMax + 0.35f) { shoot = false; yield return Cap.Shot($"qf15_02_wall_slide_{shots++:00}_{area.Replace(' ', '_').Replace("/", "")}"); }
                    yield return null;
                }
                done++;
                int bIn = HoverBike.InsideFrames - in0, bTu = HoverBike.TunnelFrames - tu0, cIn = ChapterCamera.RideCamInside - ci0;
                var pa = perArea.TryGetValue(area, out var v) ? v : Vector2Int.zero;
                bool fail = bIn + bTu + cIn > 0;
                perArea[area] = new Vector2Int(pa.x + 1, pa.y + (fail ? 1 : 0));
                string what = $"#{i} {area} {(kind == 0 ? "head-on" : kind == 1 ? "35 deg" : "60 deg")} from {s:0} toward '{(CameraOccluders.RendererOf(hit.collider) != null ? CameraOccluders.RendererOf(hit.collider).name : hit.collider != null ? hit.collider.name : "?")}' {hit.distance:0} m (pitch {pitchDeg:0})";
                if (fail) { fails++; failList.Add($"{what}: bike core inside {bIn}, through {bTu}, camera inside {cIn}{(HoverBike.InsideWorst != null ? " - " + HoverBike.InsideWorst : "")}"); Log("FAILED " + failList[failList.Count - 1]); }
                else if (i % 15 == 0) Log($"ok {what}, ended at {bike.transform.position:0}");
            }
            var sb = new System.Text.StringBuilder();
            foreach (var kv in perArea) sb.Append($"{kv.Key} {kv.Value.x} ({kv.Value.y} failed); ");
            Log($"wall slides during the test {bike.WallSlides - slides0}, depenetrations {bike.Depenetrations}");
            foreach (var f in failList) Log("failed spot: " + f);
            Cap.Finish($"WALL TEST (QF15 2): {done} attempts at full speed, failed attempts {fails} (must be 0); by area: {sb}({(Time.time - t0) / 60f:0.0} game-min)");
        }
    }
}
