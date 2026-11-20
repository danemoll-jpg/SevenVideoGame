using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 11 — 7's hover bike (Kyle's design; the forked end is the front; no handlebars - her hands rest in two
    /// recessed grips in the fairing). One bike for the whole chapter.
    /// Riding: left stick / A-D steer, RT / W accelerate, LT / S brake (also slows to a stop by itself), X / E mount and
    /// dismount (dismount only where she can stand), d-pad down / Backspace recovery (back to the last safe spot on the
    /// riding route). It floats ~0.5 m over the road with a soft bob, leans into turns, glows softly from the round
    /// vents underneath (white emission + a faint white light) and hums. It stays on the riding zones (never in the
    /// temple or the underground) and gives way: it slows to walking pace near workers and creatures and can never run
    /// into them (they are hard circles). Its parked spot and whether 7 rides it are saved (<see cref="VenusState"/>).
    /// The rider: 7 in <see cref="ChapterSeven.Mode.Ride"/> - seated low, leaning toward the forked front, hands placed
    /// in the grips by IK.
    /// </summary>
    public class HoverBike : MonoBehaviour
    {
        public ChapterSeven Rider;
        public ChapterInput Input;
        public Transform Seat, GripL, GripR, Visual;
        /// <summary>QF10: foot points on the side panels (her feet, IK), the fairing point her hand catches in B01.</summary>
        public Transform FootL, FootR, Fairing;
        public float Length = 3f;
        /// <summary>QF10: a scripted glide (B01: out of the hull bay) - the bike floats at this height above the ground
        /// instead of its riding hover (NaN = normal).</summary>
        [System.NonSerialized] public float GlideBottom = float.NaN;
        public Light VentLight;
        public Renderer VentGlow;
        // QF13 (13): Dan - "way too slow, make it fast everywhere": on land about twice the top speed and acceleration (the
        // paths, the bridge and the turns were checked at speed; it still slows to walking pace near people)
        public float MaxSpeed = 24f, Accel = 13f, Brake = 20f, Drag = 3f, TurnRate = 115f, HoverHeight = 0.5f;
        public bool Riding { get; private set; }
        public float Speed { get; private set; }
        public bool Stuck => _stuckFor > 2.5f;
        public Vector3 LastSafe { get; private set; }
        public float Odometer { get; private set; }
        /// <summary>Check runs: script inputs (steer -1..1, throttle -1..1).</summary>
        [System.NonSerialized] public bool ScriptDriven; [System.NonSerialized] public float ScriptSteer, ScriptThrottle;
        public int GiveWays { get; private set; }
        public int Recoveries { get; private set; }
        AudioSource _hum, _whoosh;
        /// <summary>Round 13 (0h): the hum's / whoosh's current base volume (check runs log it).</summary>
        public float HumLevel { get; private set; }
        public float WhooshLevel { get; private set; }
        float _lean, _bob, _stuckFor, _saveT, _safeT, _pitch;
        SevenTableIK _ik;

        /// <summary>QF10: measured from the model itself at start (raycasts on its mesh): the saddle top under her hips, and
        /// the body's half-width where her knees and feet go (so they sit outside it, never through it).</summary>
        public float SaddleTop { get; private set; } = float.NaN;
        public float KneeWidth { get; private set; }

        void MeasureBody()
        {
            if (Visual == null || Seat == null) return;
            var cols = new System.Collections.Generic.List<MeshCollider>();
            foreach (var mf in Visual.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null || !mf.sharedMesh.isReadable || mf.name == "VentGlow") continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; cols.Add(mc);
            }
            if (cols.Count == 0) return;
            Physics.SyncTransforms();
            bool Ray(Vector3 fromLocal, Vector3 dirLocal, out Vector3 hitLocal)
            {
                var o = Visual.TransformPoint(fromLocal); var d = Visual.TransformDirection(dirLocal);
                float best = float.MaxValue; hitLocal = Vector3.zero; bool any = false;
                foreach (var c in cols) if (c.Raycast(new UnityEngine.Ray(o, d), out var h, 5f) && h.distance < best) { best = h.distance; hitLocal = Visual.InverseTransformPoint(h.point); any = true; }
                return any;
            }
            // the saddle top along the centre line where her hips go (a little forward of the measured saddle)
            var sp = Seat.localPosition; sp.z += SeatForward;
            if (Ray(new Vector3(0f, 3f, sp.z), Vector3.down, out var top)) { SaddleTop = top.y; Seat.localPosition = new Vector3(0f, top.y, sp.z); }
            // the body's half-width at the knees (ahead of the hips, just under the saddle) and at the feet (the side panels)
            float W(float z, float y) { float w = 0f; foreach (var sx in new[] { -1f, 1f }) if (Ray(new Vector3(sx * 2f, y, z), new Vector3(-sx, 0f, 0f), out var h)) w = Mathf.Max(w, Mathf.Abs(h.x)); return w; }
            // the grips: the two recessed hollows in the cowl's sides - on each side, the deepest point of the side surface
            // (smallest half-width) between the cowl's lower and upper edge, a little ahead of the saddle
            // (measured on the right side, outside the forks' gap, and mirrored: the design is symmetric)
            if (GripR != null && GripL != null)
            {
                float bestX = 9f; Vector3 best = GripR.localPosition;
                for (float z = 0.42f; z <= 0.72f; z += 0.05f)
                    for (float y = 0.45f; y <= 0.90f; y += 0.03f)
                        if (Ray(new Vector3(2f, y, z), Vector3.left, out var h) && h.x > 0.25f && h.x < bestX) { bestX = h.x; best = h; }
                if (bestX < 9f) { GripR.localPosition = best; GripL.localPosition = new Vector3(-best.x, best.y, best.z); }
            }
            float st = Seat.localPosition.y;
            KneeWidth = W(Seat.localPosition.z + 0.32f, st - 0.15f);
            float fz = Seat.localPosition.z - 0.22f, fy = st - 0.28f, fw = W(fz, fy);     // feet back on the rear sides
            if (FootL != null) FootL.localPosition = new Vector3(-(fw + 0.06f), fy, fz);
            if (FootR != null) FootR.localPosition = new Vector3(fw + 0.06f, fy, fz);
            foreach (var c in cols) Destroy(c);
            Debug.Log($"[Seven] bike body measured: grips {(GripL != null ? GripL.localPosition.ToString("0.00") : "-")}, saddle top {SaddleTop:0.00} at z {Seat.localPosition.z:0.00}, knees half-width {KneeWidth:0.00}, side panels half-width {fw:0.00} at y {fy:0.00}");
        }
        public const float SeatForward = -0.32f;          // QF10 (Dan): hips at the back of the saddle (racing tuck), 4 cm in from the very back for reach

        void Start()
        {
            MeasureBody();
            LastSafe = transform.position;
            EnsureAudio();
        }

        /// <summary>Round 13 (0h): the Round 11 hum was never heard - the bike's Start can run before the director has loaded
        /// the sound bank (AudioKit.Sfx), so the loop was never created. The loops are made as soon as the bank is there, and
        /// their levels go through AudioKit (bus volume, mute).</summary>
        void EnsureAudio()
        {
            string humId = Swim && AudioKit.Clip("europa_bike_hum") != null ? "europa_bike_hum" : "bike_hum";       // Round 14: muffled under water
            if (_hum == null && AudioKit.Clip(humId) != null) _hum = AudioKit.Loop(humId, transform, null, 0.3f, AudioKit.Bus.Sfx, 3f, 40f);
            if (_whoosh == null && AudioKit.Clip("bike_whoosh") != null) _whoosh = AudioKit.Loop("bike_whoosh", transform, null, 0f, AudioKit.Bus.Sfx, 3f, 40f);
        }
        public bool HasHum => _hum != null && _hum.isPlaying;
        /// <summary>Round 13 (0d): frames the bike could not move at all (the bridge-angle test logs it).</summary>
        public int Snags;
        /// <summary>Round 13 (0k): tremor wobble, 0..1 (decays by itself).</summary>
        [System.NonSerialized] public float Wobble;

        // ---- Round 14 (Europa): the same bike swims. Steer (left stick), throttle (RT / stick up), brake (LT / stick down),
        // ascend (A held) / descend (B held); slower and floaty (it drifts on, it settles), bubbles trail behind it, the hum is
        // muffled. Kept inside the chapter's swim space by SwimClamp. No ground, no obstacle circles.
        [System.NonSerialized] public bool Swim;
        [System.NonSerialized] public bool SavesToVenus = true;
        [System.NonSerialized] public System.Func<Vector3, Vector3> SwimClamp;
        [System.NonSerialized] public float ScriptRise;
        public ParticleSystem Bubbles;
        /// <summary>QF13 (13): the sense of speed under water - fine streaking particles rushing past (white, stretched).</summary>
        public ParticleSystem Streaks;
        // QF13 (13): under water ~3x today's (8.5 -> 25 m/s), quick to accelerate, responsive turning
        public float SwimMax = 25f, SwimAccel = 11f, SwimDrag = 2.2f, SwimBrake = 16f, SwimTurn = 95f, SwimRise = 3.4f;
        float _vy;
        public float VerticalSpeed => _vy;
        // QF13 (12): flying controls under water - left stick X turns, left stick Y pitches the nose (push forward = dive,
        // pull back = climb; Settings: "Invert up / down"), RT throttle, LT brake. The bike travels along its nose.
        public static bool InvertPitch { get { try { return PlayerPrefs.GetInt("seven_swim_invert", 0) == 1; } catch { return false; } } set { try { PlayerPrefs.SetInt("seven_swim_invert", value ? 1 : 0); PlayerPrefs.Save(); } catch { } } }
        public const float MaxPitchDeg = 40f;
        float _swimPitch;
        /// <summary>The nose's pitch under water (degrees, + = climbing).</summary>
        public float SwimPitch => _swimPitch;
        /// <summary>QF13 (10): frames the bike slid along a wall / structure under water instead of passing through it.</summary>
        public int WallSlides { get; private set; }
        public const float BodyRadius = 1.0f;
        /// <summary>Round 14: a scripted push (the pressure wave): adds to its motion for a moment, spun off line.</summary>
        public void Shove(Vector3 v, float spinDeg) { _shove = v; _spin = spinDeg; }
        Vector3 _shove; float _spin;

        void SwimUpdate(float dt)
        {
            float steer, throttle, pitchIn;
            if (ScriptDriven) { steer = ScriptSteer; throttle = ScriptThrottle; pitchIn = ScriptRise; }
            else
            {
                var mv = Input != null ? Input.Move.ReadValue<Vector2>() : Vector2.zero;
                float rt = Input != null ? Input.Telekinesis.ReadValue<float>() : 0f, lt = Input != null ? Input.Block.ReadValue<float>() : 0f;
                bool fwdKey = UnityEngine.InputSystem.Keyboard.current != null && (UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed || UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed);
                steer = mv.x;
                throttle = Mathf.Clamp(Mathf.Max(rt, fwdKey ? 1f : 0f) - lt, -1f, 1f);
                pitchIn = InvertPitch ? mv.y : -mv.y;                        // push forward = dive (like flying)
            }
            if (throttle > 0.05f) Speed = Mathf.MoveTowards(Speed, SwimMax * throttle, SwimAccel * dt);
            else if (throttle < -0.05f) Speed = Mathf.MoveTowards(Speed, 0f, SwimBrake * -throttle * dt);
            else Speed = Mathf.MoveTowards(Speed, 0f, SwimDrag * dt);
            // the nose pitches toward the stick; it travels where the nose points (and drifts up / down a little when slow)
            _swimPitch = Mathf.MoveTowards(_swimPitch, Mathf.Clamp(pitchIn, -1f, 1f) * MaxPitchDeg, 110f * dt);
            float sp = Mathf.Sin(_swimPitch * Mathf.Deg2Rad), cp = Mathf.Cos(_swimPitch * Mathf.Deg2Rad);
            _vy = Mathf.MoveTowards(_vy, Speed * sp + Mathf.Clamp(pitchIn, -1f, 1f) * SwimRise * (1f - Mathf.Clamp01(Speed / 6f)), 30f * dt);
            float turn = steer * SwimTurn * Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(Speed / 3f)) * dt;
            _spin = Mathf.MoveTowards(_spin, 0f, 120f * dt);
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y + turn + _spin * dt * 2f, 0f);
            var from = transform.position;
            var p = from + transform.forward * Speed * cp * dt + Vector3.up * _vy * dt + _shove * dt;
            _shove = Vector3.MoveTowards(_shove, Vector3.zero, 9f * dt);
            // QF13 (10): real collision under water - the bike's body (a 1 m sphere) sweeps against the level's meshes (the
            // camera-occluder copy: structures, pillars, arches, platforms, pipes) and slides along a surface, never through it
            p = SlideSwim(from, p);
            if (SwimClamp != null) { var q = SwimClamp(p); if ((q - p).sqrMagnitude > 1e-8f) p = SlideSwim(p, q); }   // QF15 (2): the clamp is swept too
            float moved = (p - from).magnitude;
            if (Speed > 1f && moved < Speed * dt * 0.25f) { _stuckFor += dt; Speed *= 0.6f; } else _stuckFor = Mathf.Max(0f, _stuckFor - dt);
            transform.position = p;
            Odometer += moved;
            if (DemoBoot.CaptureDir != null) MeasureInside(from, p);
            _lean = Mathf.Lerp(_lean, -steer * Mathf.Clamp01(Speed / 5f) * 18f, dt * 3f);
            if (Time.time > _safeT) { _safeT = Time.time + 1.5f; LastSafe = p; }
            if (Bubbles != null) { var e = Bubbles.emission; e.rateOverTime = transform.position.y < EuropaLayout.WaterY ? 8f + 70f * Mathf.Clamp01(Speed / SwimMax) : 0f; }
            if (Streaks != null) { var e = Streaks.emission; e.rateOverTime = transform.position.y < EuropaLayout.WaterY ? 160f * Mathf.Clamp01((Speed - 6f) / (SwimMax - 6f)) : 0f; Streaks.transform.position = transform.position + transform.forward * 9f + Vector3.up * 0.8f; Streaks.transform.rotation = transform.rotation; }
        }

        // QF15 (2) - Dan: "the bike can still go through walls under water" (a repeat of QF13 10). Why QF13's fix did not
        // hold: the sweep asked only the visual-mesh copy's solid layer, which skipped every mesh over 60 m (the chamber's ice
        // ceiling and floor, the ice passage, the approach / escape tubes, the temple hall, the merged pillars, structures,
        // spires and platforms), every surface with a real collider (the base's outer walls and floors) and the small props;
        // and the swim-volume clamp that ran AFTER the sweep moved the bike back inside its capsules / boxes with no sweep at
        // all - where a volume reaches past a wall, straight through it. Now the whole frame's move (at 25 m/s too) is swept
        // against everything (RideProbe: real colliders + every visual mesh, both faces, no size cut in Europa), slides along
        // up to four surfaces, and the clamp's correction is swept the same way (a clamp into a wall is refused).
        public static Transform[] IgnoreRoots = new Transform[0];
        public int Depenetrations { get; private set; }
        public const float Skin = 0.06f;
        Vector3 SlideSwim(Vector3 from, Vector3 to)
        {
            if (!CameraOccluders.Ready) return to;
            var c0 = from + Vector3.up * 0.6f; var mv = to - from;
            // already touching a surface (a scripted push, a placement): out of it first, or the sweep would not see it
            float push = RideProbe.Depenetrate(ref c0, BodyRadius - 0.02f, IgnoreRoots);
            if (push > 0f) Depenetrations++;
            for (int it = 0; it < 4; it++)
            {
                float len = mv.magnitude; if (len < 1e-4f) break;
                if (!RideProbe.Sweep(c0, BodyRadius, mv, len + Skin, IgnoreRoots, out var hit)) { c0 += mv; mv = Vector3.zero; break; }
                float go = Mathf.Max(0f, hit.distance - Skin);
                var md = mv / len;
                c0 += md * go;
                var rest = md * (len - go);
                var nrm = hit.normal; if (Vector3.Dot(nrm, md) > 0f) nrm = -nrm;            // a back face: the normal toward the bike
                mv = Vector3.ProjectOnPlane(rest, nrm);
                if (Vector3.Dot(nrm, md) < -0.6f) Speed *= Mathf.Pow(0.4f, Time.deltaTime);   // head-on: scrapes speed off
                WallSlides++;
            }
            return c0 - Vector3.up * 0.6f;
        }

        // QF15 (2) check runs: every swimming frame - is the bike's core (0.55 m of its 1 m body) inside any surface, and did
        // its centre cross a surface this frame (tunnelling)?
        public static int SwimFrames, InsideFrames, TunnelFrames; public static string InsideWorst;
        void MeasureInside(Vector3 from, Vector3 to)
        {
            if (!CameraOccluders.Ready) return;
            SwimFrames++;
            var c = to + Vector3.up * 0.6f;
            if (RideProbe.Overlap(c, 0.55f, IgnoreRoots, null, false, out var w)) { InsideFrames++; InsideWorst = $"bike core inside '{w}' at {to}"; Debug.Log("[Seven] BIKE INSIDE GEOMETRY (QF15 2): " + InsideWorst); }
            if (RideProbe.Crosses(from + Vector3.up * 0.6f, c, IgnoreRoots, null, false, out var w2)) { TunnelFrames++; InsideWorst = $"bike crossed '{w2}' from {from} to {to}"; Debug.Log("[Seven] BIKE THROUGH A SURFACE (QF15 2): " + InsideWorst); }
        }
        public static string InsideSummary() => $"BIKE vs GEOMETRY (QF15 2): {SwimFrames} swimming frames, bike core inside geometry {InsideFrames}, centre crossed a surface {TunnelFrames} (both must be 0){(InsideWorst != null ? "; last: " + InsideWorst : "")}";

        /// <summary>QF15 (2) wall test: put the bike somewhere under water already moving (full speed, nose pitch).</summary>
        public void TestLaunch(Vector3 at, float yaw, float pitch, float speed)
        {
            transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            _swimPitch = pitch; Speed = speed; _vy = speed * Mathf.Sin(pitch * Mathf.Deg2Rad); _shove = Vector3.zero; _spin = 0f; _stuckFor = 0f;
            LastSafe = at;
        }

        /// <summary>Round 14: get off onto a given spot (the base's dock), standing beside the bike.</summary>
        public void DismountTo(Vector3 stand, Vector3 facing)
        {
            if (!Riding) return;
            Riding = false; Speed = 0f; _vy = 0f;
            if (_ik != null) { _ik.RightW = 0f; _ik.LeftW = 0f; _ik.LookW = 0f; _ik.enabled = true; }
            Rider.EndRide(stand, facing);
            AudioKit.Play("bike_dismount", stand, 0.8f, 0.05f);
        }

        public bool CanMount(Vector3 p) => !Riding && Flat(p - transform.position).magnitude < 2.4f;

        public void Mount()
        {
            if (Riding || Rider == null) return;
            Riding = true;
            // QF10: the riding pose owns her hands (grips), feet (side panels) and gaze; the table IK rests meanwhile
            _ik = Rider.GetComponent<SevenTableIK>();
            if (_ik != null) { _ik.RightW = 0f; _ik.LeftW = 0f; _ik.LookW = 0f; _ik.enabled = false; }
            Rider.BeginRide(this);
            AudioKit.Play("bike_mount", transform.position, 0.8f, 0.05f);
            AudioKit.Play("bike_liftoff", transform.position, 0.75f, 0.03f);
            if (SavesToVenus) VenusState.SaveBike(transform.position, transform.eulerAngles.y, true);
        }

        /// <summary>Get off on the left side - only where she can stand (a walkable zone there).</summary>
        public bool Dismount(bool force = false)
        {
            if (!Riding) return false;
            var side = transform.position - transform.right * 1.1f;
            if (!force && Flat(Ground.ClampToArena(side, 0.25f) - side).magnitude > 0.05f)
            {
                side = transform.position + transform.right * 1.1f;
                if (Flat(Ground.ClampToArena(side, 0.25f) - side).magnitude > 0.05f) return false;
            }
            Riding = false; Speed = 0f;
            if (_ik != null) { _ik.RightW = 0f; _ik.LeftW = 0f; _ik.LookW = 0f; _ik.enabled = true; }
            Rider.EndRide(new Vector3(side.x, Ground.Height(side.x, side.z), side.z), transform.forward);
            AudioKit.Play("bike_settle", transform.position, 0.7f, 0.03f);
            AudioKit.Play("bike_dismount", side, 0.8f, 0.05f);
            if (SavesToVenus) VenusState.SaveBike(transform.position, transform.eulerAngles.y, false);
            return true;
        }

        /// <summary>Put the bike somewhere (parking, summon, reload), hovering over the ground there.</summary>
        public void Place(Vector3 at, float yaw)
        {
            if (!Swim) at.y = Ground.Height(at.x, at.z);
            _vy = 0f;
            transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            LastSafe = at; Speed = 0f; _stuckFor = 0f;
        }

        public void Recover()
        {
            Recoveries++;
            Place(LastSafe, transform.eulerAngles.y);
            Debug.Log($"[Seven] bike recovered to {LastSafe}");
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        /// <summary>Round 15: which zones the bike may use on land (Venus by default; Mars sets its own).</summary>
        public static System.Func<Vector3, float, bool> ZoneTest = VenusLayout.BikeZone;
        /// <summary>Nearest point the bike may be at (inside a riding zone, outside every obstacle circle).</summary>
        Vector3 ClampBike(Vector3 p, Vector3 from)
        {
            if (!ZoneTest(p, 0.25f))
            {
                // Round 13 (0d): glide along the edge instead of snagging - try the move turned a little either way (it keeps
                // most of its length), then the two axis moves, else stay. At a ramp or bridge end met at an angle this
                // steers it onto the surface rather than stopping it dead.
                var mv = p - from; mv.y = 0f; bool ok = false;
                foreach (var ang in new[] { 12f, -12f, 25f, -25f, 40f, -40f, 55f, -55f, 70f, -70f })
                {
                    var q = from + Quaternion.Euler(0f, ang, 0f) * mv * Mathf.Cos(ang * Mathf.Deg2Rad); q.y = p.y;
                    if (ZoneTest(q, 0.25f)) { p = q; ok = true; Snags += 0; break; }
                }
                if (!ok)
                {
                    var a = new Vector3(p.x, p.y, from.z); var b = new Vector3(from.x, p.y, p.z);
                    if (ZoneTest(a, 0.25f)) p = a; else if (ZoneTest(b, 0.25f)) p = b; else { p = from; Snags++; }
                }
            }
            foreach (var c in Ground.Obstacles)
            {
                var d = new Vector2(p.x - c.C.x, p.z - c.C.y); float r = c.R + Length * 0.42f;   // QF10: 3 m bike
                if (d.sqrMagnitude < r * r && d.sqrMagnitude > 1e-6f) { d = d.normalized * r; p.x = c.C.x + d.x; p.z = c.C.y + d.y; }
            }
            return p;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _bob += dt;
            if (Riding && Swim) SwimUpdate(dt);
            else if (Riding)
            {
                float steer, throttle;
                if (ScriptDriven) { steer = ScriptSteer; throttle = ScriptThrottle; }
                else
                {
                    var mv = Input != null ? Input.Move.ReadValue<Vector2>() : Vector2.zero;
                    float rt = Input != null ? Input.Telekinesis.ReadValue<float>() : 0f, lt = Input != null ? Input.Block.ReadValue<float>() : 0f;
                    steer = mv.x; throttle = Mathf.Clamp(Mathf.Max(rt, mv.y) - Mathf.Max(lt, -mv.y), -1f, 1f);
                }
                // give way: anyone (worker, creature) ahead within 6 m -> walking pace
                float cap = MaxSpeed;
                float look = 6f + Speed * 0.9f;                                     // QF13 (13): look further ahead at speed
                foreach (var c in Ground.Obstacles)
                {
                    var to = new Vector3(c.C.x - transform.position.x, 0f, c.C.y - transform.position.z);
                    float d = to.magnitude - c.R;
                    if (d < look && Vector3.Dot(to.normalized, transform.forward) > 0.5f) { cap = Mathf.Min(cap, Mathf.Lerp(1.6f, MaxSpeed, (d - 1.5f) / (look - 1.5f))); }
                }
                if (cap < MaxSpeed * 0.5f && Speed > cap + 0.5f) GiveWays++;
                if (throttle > 0.05f) Speed = Mathf.MoveTowards(Speed, MaxSpeed * throttle, Accel * dt);
                else if (throttle < -0.05f) Speed = Mathf.MoveTowards(Speed, 0f, Brake * -throttle * dt);
                else Speed = Mathf.MoveTowards(Speed, 0f, Drag * dt);
                Speed = Mathf.Min(Speed, cap);
                float turn = steer * TurnRate * Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(Speed / 4f)) * dt;
                transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y + turn, 0f);
                var from = transform.position;
                var to2 = from + transform.forward * Speed * dt;
                var p = ClampBike(to2, from);
                float moved = Flat(p - from).magnitude;
                if (Speed > 1f && moved < Speed * dt * 0.3f) { _stuckFor += dt; Speed *= 0.5f; } else _stuckFor = Mathf.Max(0f, _stuckFor - dt);
                p.y = Ground.Height(p.x, p.z);
                transform.position = p;
                Odometer += moved;
                _lean = Mathf.Lerp(_lean, -steer * Mathf.Clamp01(Speed / 6f) * 14f, dt * 4f);
                if (ZoneTest(p, 1.6f) && Time.time > _safeT) { _safeT = Time.time + 1.5f; LastSafe = p; }
                if (Time.time > _saveT && SavesToVenus) { _saveT = Time.time + 3f; VenusState.SaveBike(transform.position, transform.eulerAngles.y, true); }
                if (Input != null && !ScriptDriven && Input.Restart.WasPressedThisFrame()) Recover();
            }
            else { Speed = 0f; _lean = Mathf.Lerp(_lean, 0f, dt * 3f); }
            if (Visual != null)
            {
                float h = HoverHeight + 0.05f * Mathf.Sin(_bob * 1.7f) + (Riding ? 0f : -0.12f);     // parked: a lower idle hover
                if (!float.IsNaN(GlideBottom)) h = GlideBottom - transform.position.y + 0.03f * Mathf.Sin(_bob * 1.7f);
                Visual.localPosition = new Vector3(0f, h, 0f);
                // QF10 (6f): it follows the land - pitched to the slope under it (smoothed), so it never snags on a rise
                var fw = transform.forward; var p0 = transform.position;
                float ahead = Ground.Height(p0.x + fw.x * 1.3f, p0.z + fw.z * 1.3f), behind = Ground.Height(p0.x - fw.x * 1.3f, p0.z - fw.z * 1.3f);
                float slope = Swim ? (Riding ? _swimPitch : Mathf.Clamp(_vy * 6f, -22f, 22f)) : float.IsNaN(GlideBottom) ? Mathf.Clamp(Mathf.Atan2(ahead - behind, 2.6f) * Mathf.Rad2Deg, -18f, 18f) : 0f;
                _pitch = Mathf.Lerp(_pitch, slope, dt * 6f);
                // Round 13 (0k): a tremor makes the bike wobble on its hover
                Wobble = Mathf.MoveTowards(Wobble, 0f, dt * 0.35f);
                float wob = Wobble * 9f;
                Visual.localRotation = Quaternion.Euler(-Mathf.Min(Speed, 11f) * 0.25f - _pitch + (Mathf.PerlinNoise(_bob * 7f, 1f) - 0.5f) * wob, 0f, _lean + (Mathf.PerlinNoise(2f, _bob * 6f) - 0.5f) * wob * 1.6f);
                Visual.localPosition += Vector3.up * (Mathf.PerlinNoise(_bob * 9f, 3f) - 0.5f) * Wobble * 0.12f;
            }
            float glow = Riding ? 0.7f + 0.3f * Mathf.Clamp01(Speed / MaxSpeed) : 0.45f;
            if (VentLight != null) VentLight.intensity = 0.6f * glow;
            if (VentGlow != null) VentGlow.material.SetColor("_EmissionColor", Color.white * (0.8f + 1.2f * glow));
            EnsureAudio();
            // Round 13 (0h): idle hum, rising in pitch and level with speed, plus a soft whoosh that comes in with speed
            float sp = Mathf.Clamp01(Mathf.Abs(Speed) / MaxSpeed);
            HumLevel = Riding ? 0.55f + 0.35f * sp : (gameObject.activeInHierarchy ? 0.32f : 0f);
            WhooshLevel = Riding ? 0.75f * sp * sp : 0f;
            if (_hum != null) { AudioKit.SetBaseVolume(_hum, HumLevel); _hum.pitch = 0.85f + 0.55f * sp; }
            if (_whoosh != null) { AudioKit.SetBaseVolume(_whoosh, WhooshLevel); _whoosh.pitch = 0.9f + 0.3f * sp; }
            if (Riding && Rider != null) Rider.RideSeat(this);     // QF10: seated before the animator runs (hands / feet IK on the current grips)
        }

        /// <summary>QF10: B01 - put the bike anywhere (in the bay, gliding out) without the ground clamp.</summary>
        public void Glide(Vector3 at, float yaw, float bottom)
        {
            transform.SetPositionAndRotation(new Vector3(at.x, Ground.Height(at.x, at.z), at.z), Quaternion.Euler(0f, yaw, 0f));
            GlideBottom = bottom; Speed = 0f;
        }
        public void EndGlide() { GlideBottom = float.NaN; LastSafe = transform.position; }
        public float Turn => _lean;

        void LateUpdate()
        {
            if (!Riding || Rider == null) return;
            Rider.RideSeat(this);
        }
    }
}
