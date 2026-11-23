using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Seven
{
    // -------------------------------------------------------------------------------------------------------------
    // QF15 (1) - Dan: "the camera keeps shifting while riding the bike - it must be smooth, always" (a repeat of QF14 5).
    // Why QF14's fix did not hold: it only switched the follow camera's wall logic off in the race (RaceMode). Everywhere
    // else while riding, the ON-FOOT follow camera was still driving the bike: it snapped in at once for any mesh on the
    // boom ("pull in fast"), its body-visibility check looked for 7's bones on the BIKE's transform (it has none, so it used
    // fixed points 0.95-1.6 m above the bike's root, often inside the hull or a ceiling) and pulled in / swung / CUT round
    // whenever those points were "hidden", the AutoBehind recentre stopped whenever that check said "occluded" and then
    // resumed (a re-aim), the camera's pitch never followed the bike's nose, and the speed FOV pushed in and out on its own.
    // Now, while she rides (any section, any world, outside cutscenes) there is ONE bike camera, this one:
    //  - behind and a little above the bike, following its heading and its nose pitch through a two-stage first-order lag
    //    (critically damped or slower: it can never overshoot);
    //  - no swings, cuts, resets or "tight spot" snaps - none of the on-foot wall logic runs;
    //  - anything between the camera and the bike that is not a shell around her (pillars, arches, rings, structures,
    //    platforms, rocks, vehicles) is CUT AWAY (not drawn, held 0.8 s) instead of moving the camera;
    //  - only shells (ice ceiling, chamber floor, tunnel / tube walls, the temple hall) bring the camera closer: a wide
    //    probe sees them early, the distance eases in (0.5 s) and only eases back out after the way has been clear for 1 s,
    //    with a 0.5 m dead band, so it never pumps; a last guard keeps the camera itself out of geometry;
    //  - look-toward-the-next-ring stays in the race only, as a slow ease; the player's own look offset eases back;
    //  - the speed FOV is a slow, small push (+6 deg, 1.2 s).
    // Check runs log every riding frame (ride_camera_frames.csv) and count "shifts" (RideSummary).
    public partial class ChapterCamera
    {
        float _raceW;
        public float RideMinDist = 2.6f, RideTightDist = 1.5f, RideMaxYawRate = 140f, RideMaxPitchRate = 80f;
        bool _rideOn, _rideHandover;
        float _ry1, _ry2, _rp1, _rp2, _rideTarget, _rideHoldUntil, _rideUserYaw, _rideUserPitch, _rideLastInput = -99f, _rideFy;
        bool _rideRecenter;
        Vector3 _ridePrevCam;
        HashSet<Renderer> _rideCutable; bool _rideCutReady;
        readonly Dictionary<Renderer, float> _rideCut = new Dictionary<Renderer, float>();
        readonly List<Renderer> _rideTmp = new List<Renderer>();

        /// <summary>Area name for the riding log (set by the chapter director).</summary>
        public static System.Func<string> RideArea;
        /// <summary>Roots never treated as geometry (7, the bike, characters) - set by the chapter director.</summary>
        public static Transform[] RideIgnore = new Transform[0];

        public static int RideFrames, RideAimShifts, RidePosShifts, RideReversals, RideForced, RideHandovers, RideCuts, RideCutAways, RideCamInside, RideCamBehind;
        public static float RideDistMin = 99f, RideDistMax;
        public static string RideWorst;
        static readonly Dictionary<string, int> _rideAreaFrames = new Dictionary<string, int>();
        static readonly Dictionary<string, int> _rideAreaShifts = new Dictionary<string, int>();
        System.IO.StreamWriter _rideCsv;
        bool _rlHave; Vector3 _rlAim, _rlRel, _rlFocus; float _rlHead, _rlBikePitch, _rlDist, _rlExtreme, _rlRevAt = -99f; int _rlSign, _rideCuts0;

        static readonly string[] Shells = { "Ceiling", "Floor", "Passage", "Tunnel", "Wall", "Hall", "Tube", "Terrain", "Ground", "Shell", "Cliff", "Valley", "Gully" };
        static bool IsShell(string n) { foreach (var s in Shells) if (n.Contains(s)) return true; return false; }

        void CollectRideCutable()
        {
            _rideCutReady = true; _rideCutable = new HashSet<Renderer>();
            foreach (var r in CameraOccluders.Renderers)
            {
                if (r == null || IsShell(r.name) || CameraOccluders.IsWalkableName(r.name)) continue;   // QF17 (4): never anything she rides on
                _rideCutable.Add(r);
            }
            Debug.Log($"[Seven] ride camera (QF15 1): {_rideCutable.Count} meshes cut away when they stand between the camera and the bike (shells ease the camera in)");
        }

        bool RidingNow => _bike != null && _bike.Riding && Cutaway == null;
        float _rideLift;

        void RideBegin(Vector3 f)
        {
            _rideOn = true; _rideHandover = true;
            var off = transform.position - f;
            float d = off.magnitude;
            if (d < 0.5f || float.IsNaN(d)) { _ry1 = _ry2 = Target.eulerAngles.y; _rp1 = _rp2 = DefaultPitch; d = Distance; }
            else
            {
                var dir = -off / d;   // camera looks along dir
                _ry1 = _ry2 = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z).sqrMagnitude > 1e-4f ? new Vector3(dir.x, 0f, dir.z) : Target.forward).eulerAngles.y;
                _rp1 = _rp2 = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(-dir.y, -1f, 1f)) * Mathf.Rad2Deg, -25f, 55f);
            }
            _dist = Mathf.Clamp(d, RideMinDist, Distance + 2f); _rideTarget = _dist; _rideHoldUntil = 0f;
            _rideUserYaw = _rideUserPitch = 0f; _rideFy = f.y; _ridePrevCam = transform.position;
            _rlHave = false; _rlSign = 0; _rideBikeLast = Target.position;
            // a cutscene that ended with its camera behind / inside a shell (the escape's burst-out framed from outside the
            // tube): easing from there would drag the camera in through the wall - the hand-over cuts straight behind her
            if (!CameraOccluders.Ready) return;
            if (!_rideCutReady) CollectRideCutable();
            // ... and a cutscene framing far round from her heading is cut, not swung, back behind her (a fast catch-up turn)
            float yawOff = Mathf.Abs(Mathf.DeltaAngle(_ry2, Target.eulerAngles.y)), pitchOff = Mathf.Abs(_rp2 - RidePitchTarget());
            if (yawOff > 35f || pitchOff > 25f || RideProbe.Crosses(f, transform.position, RideIgnore, _rideCutable, false, out _) || RideProbe.Overlap(transform.position, 0.3f, RideIgnore, _rideCutable, false))
            { RideHandoverCuts++; RideSnapBehind(); }
        }
        Vector3 _rideBikeLast, _rideVel;
        public static int RideHandoverCuts, RideTeleports;

        /// <summary>Explicit cut while riding (a teleport under a fade, a check-run turn): straight behind the bike.</summary>
        void RideSnapBehind()
        {
            _rideOn = true; _rideHandover = true; RideCuts++;
            _ry1 = _ry2 = Target.eulerAngles.y; _rp1 = _rp2 = RidePitchTarget(); _dist = _rideTarget = Distance;
            _rideUserYaw = _rideUserPitch = 0f; _rideFy = Target.position.y + LookHeight; _rlHave = false; _rlSign = 0; _rideBikeLast = Target.position;
            LateUpdateRide(0.0001f, true);
        }

        // climbing: the camera comes down toward her level but never below it (below, it met the floor of every chamber)
        float RidePitchTarget() => Mathf.Clamp(DefaultPitch - (_bike != null && _bike.Swim ? _bike.SwimPitch * 0.55f : 0f), 2f, 45f);

        void RideUncutAll()
        {
            foreach (var kv in _rideCut) if (kv.Key != null) kv.Key.forceRenderingOff = false;
            _rideCut.Clear();
        }

        void LateUpdateRide(float dt, bool snap = false)
        {
            dt = Mathf.Max(dt, 1e-4f);
            if (!CameraOccluders.Ready) CameraOccluders.Build(RideIgnore);
            if (!_rideCutReady) CollectRideCutable();
            var bt = Target; var now = Time.time;
            var fRaw = bt.position + Vector3.up * LookHeight;
            if (!_rideOn) { int c0 = RideCuts; RideBegin(fRaw); if (RideCuts != c0) return; }
            // the bike was put somewhere (a skip / recovery under a fade, a check-run placement): a cut, not a camera move
            if (!snap && (bt.position - _rideBikeLast).magnitude > Mathf.Max(3f, _bike.Speed * dt * 4f + 1f)) { RideTeleports++; _rideBikeLast = bt.position; RideSnapBehind(); return; }
            _rideVel = Vector3.Lerp(_rideVel, (bt.position - _rideBikeLast) / dt, 1f - Mathf.Exp(-dt / 0.2f));
            _rideBikeLast = bt.position;
            // the speed push: slow and small
            {
                var cm = GetComponent<Camera>(); if (_baseFov <= 0f) _baseFov = cm.fieldOfView;
                float k = Mathf.Clamp01((_bike.Speed - 8f) / 16f);
                if (RaceMode) k += 0.6f * Mathf.Clamp01((_bike.Speed - 24f) / 12f);       // QF28 (1): the race widens the view further with speed
                SpeedKick = Mathf.MoveTowards(SpeedKick, k, dt * (SpeedKick > k ? 1.6f : 0.8f));
                cm.fieldOfView += (_baseFov + 6f * SpeedKick - cm.fieldOfView) * (1f - Mathf.Exp(-dt / 1.2f));
            }
            // the player may look around; the offset eases back once the stick is let go (R3 / R: at once, smoothly)
            if (!snap)
            {
                Vector2 stick = Input != null ? Input.Look.ReadValue<Vector2>() : Vector2.zero;
                float dYaw = stick.x * StickYawSpeed * dt, dPitch = -stick.y * StickPitchSpeed * dt;
                var mouse = Mouse.current;
                if (mouse != null && DemoBoot.CaptureDir == null && !PauseMenu.IsOpen && Cursor.lockState == CursorLockMode.Locked)
                { var md = mouse.delta.ReadValue(); dYaw += md.x * MouseDegPerPixel; dPitch -= md.y * MouseDegPerPixel; }
                if (Mathf.Abs(dYaw) + Mathf.Abs(dPitch) > 0.01f) { _rideLastInput = now; _rideRecenter = false; }
                _rideUserYaw = Mathf.Clamp(_rideUserYaw + dYaw, -170f, 170f); _rideUserPitch = Mathf.Clamp(_rideUserPitch + dPitch, -30f, 40f);
                if (Input != null && Input.Recenter.WasPressedThisFrame()) _rideRecenter = true;
                if (_rideRecenter || now - _rideLastInput > 1.5f)
                {
                    float a = 1f - Mathf.Exp(-dt / (_rideRecenter ? 0.3f : 1.0f));
                    _rideUserYaw -= _rideUserYaw * a; _rideUserPitch -= _rideUserPitch * a;
                    if (Mathf.Abs(_rideUserYaw) < 0.5f) _rideRecenter = false;
                }
            }
            // where it wants to be: behind her heading (in the race, eased toward the next ring for a moment)
            float heading = bt.eulerAngles.y, pitchT = RidePitchTarget();
            if (RaceMode && RaceAim.HasValue && now < RaceAimUntil)
            {
                var to = RaceAim.Value - bt.position; var fl = new Vector3(to.x, 0f, to.z);
                if (fl.sqrMagnitude > 4f)
                {
                    // a slow ease toward the next ring and back (its weight itself moves at most 0.5 per second)
                    _raceW = Mathf.MoveTowards(_raceW, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((RaceAimUntil - now) / 0.8f)) * 0.6f, dt * 0.5f);
                    float w = _raceW;
                    heading = Mathf.LerpAngle(heading, Quaternion.LookRotation(fl).eulerAngles.y, w);
                    pitchT = Mathf.Lerp(pitchT, Mathf.Clamp(DefaultPitch + Mathf.Atan2(-to.y, fl.magnitude) * Mathf.Rad2Deg * 0.6f, -10f, 40f), w);
                }
            }
            float yT = heading + _rideUserYaw, pT = Mathf.Clamp(pitchT + _rideUserPitch, -25f, 55f);
            if (snap) { _ry1 = _ry2 = yT; _rp1 = _rp2 = pT; }
            else
            {
                float ay = 1f - Mathf.Exp(-dt / 0.17f), ap = 1f - Mathf.Exp(-dt / 0.22f);
                _ry1 += Mathf.DeltaAngle(_ry1, yT) * ay; _rp1 += (pT - _rp1) * ap;
                // ... and never faster than a calm turn (a bike spun on the spot must not whip the view round)
                _ry2 += Mathf.Clamp(Mathf.DeltaAngle(_ry2, _ry1) * ay, -RideMaxYawRate * dt, RideMaxYawRate * dt);
                _rp2 += Mathf.Clamp((_rp1 - _rp2) * ap, -RideMaxPitchRate * dt, RideMaxPitchRate * dt);
            }
            Yaw = _ry2; Pitch = _rp2;
            // the focus is on the bike; only its height is smoothed on land (ground steps)
            _rideFy = snap || _bike.Swim ? fRaw.y : _rideFy + (fRaw.y - _rideFy) * (1f - Mathf.Exp(-dt / 0.12f));
            var f = new Vector3(fRaw.x, _rideFy, fRaw.z);
            _focus = f;
            var dir = Quaternion.Euler(Pitch, Yaw, 0f) * Vector3.back;
            // distance: only shells bring it in - a wide probe (eases in early), a dead band, a hold before easing out
            float want = Distance;
            float wr = 1.1f;
            while (wr > 0.3f && RideProbe.Overlap(f, wr, RideIgnore, _rideCutable, false)) wr -= 0.3f;
            if (RideProbe.Cast(f, Mathf.Max(0.25f, wr), dir, Distance + 1.5f, RideIgnore, _rideCutable, out var hd)) want = Mathf.Clamp(hd - 1.2f, RideTightDist, Distance);
            // ... and the camera's own spot keeps ~1 m from any shell (a bike hugging a curving tube wall: the boom runs
            // along the wall, the narrow probe sees nothing, and the bend would come through the camera)
            while (want > RideTightDist && RideProbe.Overlap(f + dir * want, 0.95f, RideIgnore, _rideCutable, false)) want -= 0.4f;
            // look ahead: where the bike will be in ~0.6 s (a tunnel mouth, a ceiling ahead) - the camera comes in BEFORE the
            // rim of a tube cuts the line to her, never after
            if (!snap && _bike.Speed > 2f)
            {
                var fa = f + _rideVel * 0.6f;
                if (!RideProbe.Overlap(fa, 0.3f, RideIgnore, _rideCutable, false) && !RideProbe.Crosses(f, fa, RideIgnore, _rideCutable, false, out _))
                {
                    if (RideProbe.Cast(fa, 0.6f, dir, Distance + 1f, RideIgnore, _rideCutable, out var ha)) want = Mathf.Min(want, Mathf.Max(RideTightDist, ha - 1.2f));
                    while (want > RideTightDist && RideProbe.Overlap(fa + dir * want, 0.95f, RideIgnore, _rideCutable, false)) want -= 0.4f;
                }
            }
            want = Mathf.Max(want, RideTightDist);
            if (snap) { _rideTarget = want; _dist = want; }
            else
            {
                // easing back out and a new, slightly closer limit appears: stop where it is (no in / out pump); ease in only
                // when it is really needed (the limit is well inside where the camera is now)
                if (want < _rideTarget - 0.5f && _rideTarget > _dist + 0.01f && want > _dist - 0.4f) { _rideTarget = _dist; _rideHoldUntil = now + 1.0f; }
                else if (want < _rideTarget - 0.5f) { _rideTarget = want; _rideHoldUntil = now + 1.0f; }
                else if (want < _rideTarget + 0.5f) { if (want < Distance - 0.01f) _rideHoldUntil = Mathf.Max(_rideHoldUntil, now + 1.2f); }
                else if (want < Distance - 0.01f) _rideHoldUntil = Mathf.Max(_rideHoldUntil, now + 1.2f);   // out only after 1.2 s fully clear
                else if (now > _rideHoldUntil) _rideTarget = want;
                float tau = _rideTarget < _dist ? (RideProbe.Overlap(f + dir * _dist, 0.6f, RideIgnore, _rideCutable, false) ? 0.25f : 0.5f) : 1.4f;
                _dist += (_rideTarget - _dist) * (1f - Mathf.Exp(-dt / tau));
            }
            // the last guard: the camera itself never inside geometry or through a wall (should not be needed - counted)
            var cp = f + dir * _dist;
            // QF17 (4): on a slope the ground behind her is higher than she is (riding down a ramp): the camera rises to stay
            // above that surface (eased, never a jump) instead of sitting under it - the old fix cut the ramp away
            {
                // the line from the camera to her must clear the ground (ramp surfaces, crests) at every point along it:
                // the lowest camera height that does, eased (never a step, so no shift)
                string zb = Ground.ZoneName(bt.position.x, bt.position.z);
                float need = 0f; var fb = f;
                for (int k = 0; k < 4; k++)
                {
                    float u = k * 0.25f; var q = Vector3.Lerp(cp, fb, u);
                    string zq = Ground.ZoneName(q.x, q.z);
                    if (zq == null || zq != zb && Mathf.Abs(Ground.Height(q.x, q.z) - bt.position.y) > 6f) continue;
                    float clear = Ground.Height(q.x, q.z) + (k == 0 ? 1.6f : 0.7f);
                    // camera y so that the line's height at fraction u is >= clear
                    float camY = (clear - u * fb.y) / Mathf.Max(0.25f, 1f - u);
                    need = Mathf.Max(need, camY - cp.y);
                }
                _rideLift = Mathf.MoveTowards(_rideLift, need, dt * (need > _rideLift ? 5f : 2f));
                cp.y += _rideLift;
            }
            if (RideProbe.Overlap(cp, 0.3f, RideIgnore, _rideCutable, false) || (!snap && RideProbe.Crosses(_ridePrevCam, cp, RideIgnore, _rideCutable, false, out _)))
            {
                if (RideProbe.Cast(f, 0.3f, dir, _dist, RideIgnore, _rideCutable, out var hh)) { _dist = Mathf.Max(0.8f, hh - 0.1f); _rideTarget = Mathf.Min(_rideTarget, _dist); _rideHoldUntil = now + 1f; RideForced++; cp = f + dir * _dist; }
            }
            transform.position = cp;
            transform.rotation = Quaternion.LookRotation(f - cp);
            _look = f; _ridePrevCam = cp;
            // cut away whatever (not a shell) stands between the camera and the bike / her, or around the camera
            _rideTmp.Clear();
            var bc = bt.position + Vector3.up * 0.7f;
            foreach (var q in new[] { bc, f, bc + bt.forward * 1.3f, bc - bt.forward * 1.3f })
                CameraOccluders.RenderersBetween(q, cp, _rideTmp, CameraOccluders.AllMask);
            CameraOccluders.OverlapRenderers(cp, 0.6f, _rideTmp);
            var bikeAt = bt.position;
            foreach (var r in _rideTmp) if (r != null && _rideCutable.Contains(r) && CameraOccluders.MayCutAway(r, bikeAt)) { if (!_rideCut.ContainsKey(r)) RideCutAways++; _rideCut[r] = now + 0.8f; }
            _done.Clear();
            foreach (var kv in _rideCut) { bool hide = now < kv.Value; if (kv.Key != null && kv.Key.forceRenderingOff != hide) kv.Key.forceRenderingOff = hide; if (!hide) _done.Add(kv.Key); }
            foreach (var r in _done) _rideCut.Remove(r);
            if (DemoBoot.CaptureDir != null || DemoBoot.PlayerSim) RideMeasure(f, cp, dt);
            _rideHandover = false;
        }

        // ---- the measure (check runs): per frame, the camera's aim / position change vs the bike's own
        void RideMeasure(Vector3 f, Vector3 cp, float dt)
        {
            string area = RideArea != null ? RideArea() : "?";
            if (RaceMode) area += " (race)";
            RideFrames++;
            _rideAreaFrames[area] = _rideAreaFrames.TryGetValue(area, out var af) ? af + 1 : 1;
            float camDist = (cp - f).magnitude;
            RideDistMin = Mathf.Min(RideDistMin, camDist); RideDistMax = Mathf.Max(RideDistMax, camDist);
            var aim = transform.forward; var rel = cp - f; float head = Target.eulerAngles.y, bp = _bike.Swim ? _bike.SwimPitch : 0f;
            float aimCh = 0f, headCh = 0f, relCh = 0f; string flag = "";
            bool inside = RideProbe.Overlap(cp, 0.15f, RideIgnore, null, true);
            bool behind = RideProbe.Crosses(f, cp, RideIgnore, null, true, out var what);
            if (inside) { RideCamInside++; flag += "CAM-INSIDE "; RideWorst = $"{area}: camera inside geometry at {cp} (bike {Target.position})"; }
            if (behind) { RideCamBehind++; flag += "behind:" + what + " "; }
            if (_rlHave && !_rideHandover)
            {
                aimCh = Vector3.Angle(aim, _rlAim);
                headCh = Mathf.Abs(Mathf.DeltaAngle(head, _rlHead)) + Mathf.Abs(bp - _rlBikePitch);
                relCh = (rel - _rlRel).magnitude;
                // Dan's thresholds are per frame at 60 fps: scaled to this frame's length (check runs at -testspeed 2 take 1/30 s of game time per frame)
                float fs = Mathf.Max(1f, dt * 60f);
                // the aim change the bike's own motion explains: its turn / pitch plus its sideways travel seen from the camera
                var df = f - _rlFocus; float side = Vector3.ProjectOnPlane(df, aim).magnitude;
                float moveDeg = Mathf.Atan2(side, Mathf.Max(0.5f, camDist)) * Mathf.Rad2Deg;
                bool shiftA = aimCh > headCh + moveDeg + 3f * fs;
                bool shiftP = relCh > 0.3f * fs + _dist * headCh * Mathf.Deg2Rad;
                if (shiftA) { RideAimShifts++; flag += "AIM-SHIFT "; }
                if (shiftP) { RidePosShifts++; flag += "POS-SHIFT "; }
                // in / out reversals of the distance (>= 5 cm) within 0.5 s
                int sg = camDist > _rlDist + 1e-4f ? 1 : camDist < _rlDist - 1e-4f ? -1 : 0;
                if (sg != 0)
                {
                    if (_rlSign == 0) { _rlSign = sg; _rlExtreme = _rlDist; }
                    else if (sg != _rlSign)
                    {
                        if (Mathf.Abs(_rlDist - _rlExtreme) >= 0.05f)
                        {
                            if (Time.time - _rlRevAt < 0.5f) { RideReversals++; flag += "REVERSAL "; }
                            _rlRevAt = Time.time;
                        }
                        _rlSign = sg; _rlExtreme = _rlDist;
                    }
                }
                if (shiftA || shiftP || flag.Contains("REVERSAL"))
                {
                    _rideAreaShifts[area] = _rideAreaShifts.TryGetValue(area, out var sh) ? sh + 1 : 1;
                    Debug.Log($"[Seven] ride camera SHIFT (QF15 1) in {area}: aim {aimCh:0.0} deg vs bike {headCh:0.0}, offset {relCh:0.00} m, dist {camDist:0.00} ({flag.Trim()}) at {Target.position}");
                }
            }
            else if (_rideHandover) RideHandovers++;
            _rlHave = true; _rlAim = aim; _rlRel = rel; _rlHead = head; _rlBikePitch = bp; _rlDist = camDist; _rlFocus = f;
            if (DemoBoot.CaptureDir != null)
            {
                if (_rideCsv == null)
                {
                    _rideCsv = new System.IO.StreamWriter(System.IO.Path.Combine(DemoBoot.CaptureDir, "ride_camera_frames.csv"), true);
                    _rideCsv.WriteLine("frame,time,area,bike_x,bike_y,bike_z,speed,bike_pitch,cam_dist,aim_change_deg,bike_turn_deg,offset_change_m,cut_away,flags");
                }
                var p = Target.position;
                _rideCsv.WriteLine($"{Time.frameCount},{Time.time:0.000},{area},{p.x:0.00},{p.y:0.00},{p.z:0.00},{_bike.Speed:0.0},{bp:0.0},{camDist:0.000},{aimCh:0.00},{headCh:0.00},{relCh:0.000},{_rideCut.Count},{flag.Trim()}");
                if (RideFrames % 600 == 0) _rideCsv.Flush();
            }
        }

        void OnDestroy() { if (_rideCsv != null) { _rideCsv.Flush(); _rideCsv.Close(); _rideCsv = null; } RideUncutAll(); }

        public static string RideSummary()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kv in _rideAreaFrames) sb.Append($"{kv.Key} {kv.Value} frames / {(_rideAreaShifts.TryGetValue(kv.Key, out var s) ? s : 0)} shifts; ");
            return $"RIDE CAMERA (QF15 1): {RideFrames} riding frames, aim shifts {RideAimShifts}, position shifts {RidePosShifts}, distance reversals within 0.5 s {RideReversals} (all must be 0); " +
                   $"camera inside geometry {RideCamInside} frames (must be 0), behind a drawn surface {RideCamBehind} frames; distance {RideDistMin:0.0}-{RideDistMax:0.0} m; last-guard pull-ins {RideForced}; " +
                   $"explicit cuts {RideCuts} (of them: hand-overs from a cutscene camera behind a wall {RideHandoverCuts}, the bike put elsewhere by a skip / placement {RideTeleports}), cutscene handovers {RideHandovers}; meshes cut away {RideCutAways}; by area: {sb}{(RideWorst != null ? " worst: " + RideWorst : "")}";
        }
    }
}
