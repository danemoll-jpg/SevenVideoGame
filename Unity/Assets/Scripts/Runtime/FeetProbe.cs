using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF18 (16) - "7 clips through the ground badly" at the pier walk. Check runs: every frame 7 is on foot in a zone level,
    /// her feet against the visible surface under each foot (a ray down against the level's meshes): sinking (a foot more
    /// than 6 cm below the surface), floating (both feet over 25 cm above it while standing / walking) and her body inside a
    /// wall or prop. Log: "FEET vs GROUND (QF18 16)" with the worst spots.
    /// </summary>
    public static class FeetProbe
    {
        public static int Frames, Sinking, Floating, Inside; public static float WorstSink, WorstFloat; public static string WorstAt, InsideAt;
        static readonly RaycastHit[] _h = new RaycastHit[16];
        public static void Sample(ChapterSeven s, string where)
        {
            if (s == null || s.Current != ChapterSeven.Mode.Free || !CameraOccluders.Ready) return;
            Frames++;
            float lowest = 99f, highest = -99f; bool any = false;
            foreach (var bb in new[] { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
            {
                var f = s.Bone(bb); if (f == null) continue;
                var o = f.position + Vector3.up * 0.9f;
                float best = float.NegativeInfinity;
                int n = CameraOccluders.RaycastAll(o, Vector3.down, 2.4f, _h);
                for (int i = 0; i < n; i++) if (_h[i].collider != null && _h[i].point.y > best) { var r = CameraOccluders.RendererOf(_h[i].collider); if (r != null && (r.transform.IsChildOf(s.transform) || r.name.Contains("Grass"))) continue; /* her own body; grass blades are not the ground */ best = _h[i].point.y; }
                if (Physics.Raycast(o, Vector3.down, out var ph, 2.4f, ~((1 << 20) | (1 << 22) | (1 << 5) | (1 << 2)), QueryTriggerInteraction.Ignore) && !ph.collider.transform.IsChildOf(s.transform)) best = Mathf.Max(best, ph.point.y);
                if (float.IsNegativeInfinity(best)) continue;
                any = true;
                float d = f.position.y - 0.08f - best;                 // the ankle sits ~8 cm above the sole
                lowest = Mathf.Min(lowest, d); highest = Mathf.Max(highest, d);
            }
            if (any)
            {
                if (lowest < -0.06f) { Sinking++; if (-lowest > WorstSink) { WorstSink = -lowest; WorstAt = $"{where} at {s.transform.position}"; } }
                if (lowest > 0.25f && !s.IsAirborne) { Floating++; if (lowest > WorstFloat) WorstFloat = lowest; }
            }
            if (RideProbe.LandInside(s.transform.position, 1.0f, 0.22f, new[] { s.transform }, out var w)) { Inside++; InsideAt = $"'{w}' {where} at {s.transform.position}"; }
        }
        public static string Summary() => $"FEET vs GROUND (QF18 16): {Frames} on-foot frames, a foot sinking into the surface {Sinking} (worst {WorstSink * 100f:0} cm{(WorstAt != null ? ", " + WorstAt : "")}), floating {Floating} (worst {WorstFloat * 100f:0} cm), her body inside geometry {Inside} (must be 0){(InsideAt != null ? "; last " + InsideAt : "")}";
    }
}
