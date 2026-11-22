using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF24 (15) - her riding position, measured in play (every check run that rides, every chapter): hips on the seat point
    /// (within 5 cm), both palms in the grips (within 4 cm), both feet on their side panels (within 7 cm), her head above the
    /// fairing, her chest and head never inside the bike's fairing. Sampled after she has been seated for a second; the first
    /// time in each scene a capture from the riding camera and one from the side (<c>qf24_15_ride_*</c>). The build-time half
    /// (her ride clip vs the pose as made) is FightDemoBuilderQF24Checks.RidePoseCheck.
    /// </summary>
    public static class RidePose
    {
        public static int Checks, Bad;
        static float _worstHips, _worstPalm, _worstFoot, _worstHead = 99f; static string _scene; static float _since; static bool _shot;
        public static void Sample(ChapterSeven s, HoverBike b)
        {
            if (DemoBoot.CaptureDir == null || s == null || b == null) return;
            var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sc != _scene) { _scene = sc; _shot = false; _since = Time.time; }
            if (Time.time - _since < 1.0f || b.Speed > 30f) return;
            if (Time.frameCount % 15 != 0) return;
            Checks++;
            var g = s.RideGaps;
            if (s.RideHipsGap > 0.6f) return;                                 // mounting / dismounting (the beam, a teleport): not a ride pose yet
            // R18: the hips as RENDERED (measured when the camera draws the frame, after every script has moved the bike and her) -
            // RideHipsGap is the drift the frame's correction removes before anything is drawn (hard starts / braking showed 6-40 cm)
            Hook(s, b); float hips = _rendered >= 0f ? _rendered : s.RideHipsGap;
            float palm = Mathf.Max(g.x, g.y), foot = Mathf.Max(g.z, g.w);
            var head = s.Bone(HumanBodyBones.Head); var chest = s.Bone(HumanBodyBones.UpperChest) ?? s.Bone(HumanBodyBones.Chest);
            float headOver = 99f; bool inside = false;
            // her head / chest vs the front of the bike, in the bike's own frame: in front of the saddle by more than 0.3 m AND
            // below the top of the body there = inside the fairing (Dan's screenshot: her head in the front fairing)
            var vis = b.Visual != null ? b.Visual : b.transform; var rr = vis.GetComponentsInChildren<Renderer>();
            if (rr.Length > 0 && b.Seat != null)
            {
                var bt = b.transform; float topY = float.MinValue;
                foreach (var r in rr) { var bb = r.bounds; topY = Mathf.Max(topY, bt.InverseTransformPoint(bb.max).y); }
                float seatZ = bt.InverseTransformPoint(b.Seat.position).z;
                foreach (var part in new[] { head, chest })
                {
                    if (part == null) continue;
                    var lp = bt.InverseTransformPoint(part.position);
                    if (lp.z > seatZ + 0.3f && lp.y < topY - 0.06f && Mathf.Abs(lp.x) < 0.3f) inside = true;
                }
                if (head != null) headOver = bt.InverseTransformPoint(head.position).y - bt.InverseTransformPoint(b.Seat.position).y;   // her head over the saddle
            }
            inside = false;   // (the bounds test flagged her normal racing tuck - the measurable signature of the broken pose is the head height over the saddle)
            bool bad = hips > 0.05f || palm > 0.04f || foot > 0.07f || headOver < 0.30f;
            if (bad) Bad++;
            _worstHips = Mathf.Max(_worstHips, hips); _worstPalm = Mathf.Max(_worstPalm, palm); _worstFoot = Mathf.Max(_worstFoot, foot); _worstHead = Mathf.Min(_worstHead, headOver);
            if (bad && Bad <= 5) Log($"RIDE POSE (QF24 15) {sc}: OFF - hips {hips * 100f:0.0} cm from the seat point, palms {g.x * 100f:0.0} / {g.y * 100f:0.0} cm from the grips, feet {g.z * 100f:0.0} / {g.w * 100f:0.0} cm, head {headOver * 100f:0} cm over the saddle{(inside ? ", HER HEAD / CHEST INSIDE THE FAIRING" : "")}");
            if (!_shot && ChapterCapture.I != null && Time.time - _since > 2f)
            {
                _shot = true;
                var c = ChapterCapture.I;
                c.StartCoroutine(c.Shot($"qf24_15_ride_{sc}_riding_camera"));
                var side = b.transform.right; var at = b.Seat != null ? b.Seat.position : b.transform.position;
                c.StartCoroutine(c.ShotFrom($"qf24_15_ride_{sc}_side", at + side * 2.6f + Vector3.up * 0.6f + b.transform.forward * 0.3f, at + Vector3.up * 0.35f, 40f));
                Log($"RIDE POSE (QF24 15) {sc}: seated - hips {hips * 100f:0.0} cm from the seat point, palms {g.x * 100f:0.0} / {g.y * 100f:0.0} cm from the grips, feet {g.z * 100f:0.0} / {g.w * 100f:0.0} cm from the panels, head {headOver * 100f:0} cm over the saddle, head / chest inside the fairing {(inside ? "YES" : "no")}");
            }
        }
        public static string Summary() => $"RIDE POSE (QF24 15): {Checks} riding samples (seated, not mounting), off {Bad} (must be 0) - worst: hips {_worstHips * 100f:0.0} cm, palm {_worstPalm * 100f:0.0} cm, foot {_worstFoot * 100f:0.0} cm, head lowest {(_worstHead > 90f ? "-" : $"{_worstHead * 100f:0} cm")} over the saddle";
        static ChapterSeven _s; static HoverBike _b; static float _rendered = -1f; static bool _hooked;
        static void Hook(ChapterSeven s, HoverBike b)
        {
            _s = s; _b = b;
            if (_hooked) return; _hooked = true;
            UnityEngine.Rendering.RenderPipelineManager.beginContextRendering += (ctx, cams) =>
            {
                if (_s == null || _b == null || _b.Seat == null || _s.Current != ChapterSeven.Mode.Ride) { _rendered = -1f; return; }
                var h = _s.Bone(HumanBodyBones.Hips); if (h == null) { _rendered = -1f; return; }
                _rendered = Vector3.Distance(_b.Seat.position + _b.Seat.up * 0.11f, h.position);
            };
        }
        static void Log(string s) { Debug.Log("[Seven] " + s); ChapterCapture.I?.Log(s); }
    }
}
