using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF18 (10): the land wall test (QF15's under-water test, on land) - -landwalltest. The bike is launched at full speed
    /// straight into and at shallow angles along the walls and props of the Mars level (the ramp cut walls, the ramp foot,
    /// the parapets, the gateway piers, the drawbridge tower, the arcade columns, the checkpoint towers, houses), and 7 walks
    /// into the same walls on foot; every frame is measured (BIKE vs GEOMETRY ON LAND, FEET vs GROUND). Then the whole route
    /// is ridden end to end at full speed by the autopilot (arrival -> ravine -> causeway -> passage -> east ramp -> gate ->
    /// far bank -> rims -> spans -> plaza -> back). Ends "LAND WALL TEST (QF18 10): N attempts ...".
    /// </summary>
    public static class LandWallTest
    {
        static void Log(string s) { Debug.Log("[Seven] " + s); ChapterCapture.I?.Log(s); }
        public static IEnumerator Run(HoverBike bike, ChapterSeven seven, ChapterCamera cam, System.Action<string, bool> zone)
        {
            foreach (var z in new[] { "Causeway", "ServiceGateZ", "SpanW", "SpanE", "Bridge", "TempleHall" }) zone(z, true);
            var md = MarsDirector.I;
            if (md != null && md.Drawbridge != null) { md.Drawbridge.Set(1f); if (md.Drawbridge.Lock != null) md.Drawbridge.Lock.gameObject.SetActive(false); }
            if (md != null && md.ServiceGate != null) md.ServiceGate.gameObject.SetActive(false);
            CameraOccluders.SyncAll();
            int attempts = 0, frames0 = HoverBike.LandFrames, inside0 = HoverBike.LandInsideFrames, cross0 = HoverBike.LandCrossFrames;
            bike.gameObject.SetActive(true); seven.gameObject.SetActive(true);
            if (!bike.Riding) bike.Mount();
            bike.ScriptDriven = true;
            // (start, heading) pairs: head-on and glancing runs at the walls
            var runs = new List<(Vector3 at, float yaw, string what)>
            {
                (new Vector3(-55f, MarsLayout.FloorY, -45f), 180f, "down the west ramp into the landing's end wall"),
                (new Vector3(-55f, MarsLayout.FloorY, -60f), 190f, "the west ramp foot, glancing off the bank wall"),
                (new Vector3(-55f, -6f, -20f), 270f, "across the west ramp into its inner wall"),
                (new Vector3(-55f, -6f, -24f), 95f, "across the west ramp into its parapet"),
                (new Vector3(55f, MarsLayout.FloorY, -60f), 0f, "up the east ramp"),
                (new Vector3(55f, -8f, -28f), 90f, "across the east ramp into the cable wall"),
                (new Vector3(55.2f, -1f, -16f), 0f, "up through the service gateway"),
                (new Vector3(54f, -1f, -18f), 35f, "into the service gateway's pier"),
                (new Vector3(-20f, MarsLayout.FloorY, -73f), 70f, "into the drawbridge tower"),
                (new Vector3(-20f, MarsLayout.FloorY, -67f), 90f, "over the lowered drawbridge"),
                (new Vector3(28f, MarsLayout.FloorY, -83.5f), 90f, "into an arcade column"),
                (new Vector3(-66f, 0f, -6f), 0f, "into the checkpoint tower"),
                (new Vector3(-100f, 0f, 3f), 0f, "into a house on the main street"),
                (new Vector3(-56f, 0f, 150f), 90f, "over the west span"),
            };
            foreach (var r in runs)
            {
                attempts++;
                var at = r.at; at.y = Ground.Height(at.x, at.z);
                bike.Place(at, r.yaw);                                              // full throttle from an open spot in the zone
                cam.Target = bike.transform; cam.Follow(); cam.SnapBehind();
                int f0 = HoverBike.LandInsideFrames, c0 = HoverBike.LandCrossFrames, s0 = bike.LandSlides;
                for (float t = 0f; t < 3.4f; t += Time.deltaTime) { bike.ScriptThrottle = 1f; bike.ScriptSteer = Mathf.Sin(t * 1.3f) * 0.35f; yield return null; }
                Log($"land wall test (QF18 10) {attempts}: {r.what} - slid along walls {bike.LandSlides - s0} times, body inside geometry {HoverBike.LandInsideFrames - f0} frames, crossed a surface {HoverBike.LandCrossFrames - c0}, ended at {bike.transform.position}");
                if (attempts == 1 || attempts == 7) yield return ChapterCapture.I?.Shot($"landwall_{attempts:00}");
            }
            bike.ScriptThrottle = 0f;
            // on foot into the same kind of walls
            bike.Dismount(true);
            int footIn0 = FeetProbe.Inside;
            foreach (var (at, dir) in new[] { (new Vector3(-55f, MarsLayout.FloorY, -66f), Vector3.back), (new Vector3(-58f, -6f, -24f), Vector3.left), (new Vector3(55.2f, -1f, -15f), new Vector3(0.6f, 0f, 1f)), (new Vector3(-48.8f, -4f, 150f), Vector3.left), (new Vector3(-48.8f, -2f, 162f), Vector3.right) })
            {
                attempts++;
                seven.Place(new Vector3(at.x, Ground.Height(at.x, at.z), at.z), dir); seven.ScriptDriven = true;
                for (float t = 0f; t < 2.2f; t += Time.deltaTime) { seven.ScriptMove = dir.normalized; FeetProbe.Sample(seven, "wall test"); yield return null; }
                seven.ScriptMove = Vector3.zero;
            }
            int frames = HoverBike.LandFrames - frames0, inside = HoverBike.LandInsideFrames - inside0, cross = HoverBike.LandCrossFrames - cross0;
            Log($"LAND WALL TEST (QF18 10): {attempts} attempts (14 riding at full speed, 5 on foot), {frames} riding frames - bike inside geometry {inside}, crossed a surface {cross}; on foot inside geometry {FeetProbe.Inside - footIn0}; slides on foot {ChapterSeven.FootSlides}; {(inside == 0 && cross == 0 && FeetProbe.Inside - footIn0 == 0 ? "failed 0" : "FAILED")}");
        }
    }
}
