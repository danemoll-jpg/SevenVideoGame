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
        public float MaxSpeed = 11f, Accel = 5.5f, Brake = 9f, Drag = 1.6f, TurnRate = 95f, HoverHeight = 0.5f;
        public bool Riding { get; private set; }
        public float Speed { get; private set; }
        public bool Stuck => _stuckFor > 2.5f;
        public Vector3 LastSafe { get; private set; }
        public float Odometer { get; private set; }
        /// <summary>Check runs: script inputs (steer -1..1, throttle -1..1).</summary>
        [System.NonSerialized] public bool ScriptDriven; [System.NonSerialized] public float ScriptSteer, ScriptThrottle;
        public int GiveWays { get; private set; }
        public int Recoveries { get; private set; }
        AudioSource _hum;
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
        public const float SeatForward = -0.36f;          // QF10 (Dan): hips at the BACK of the saddle (racing tuck)

        void Start()
        {
            MeasureBody();
            LastSafe = transform.position;
            _hum = AudioKit.Loop("bike_hum", transform, null, 0.0f, AudioKit.Bus.Sfx);
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
            VenusState.SaveBike(transform.position, transform.eulerAngles.y, true);
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
            VenusState.SaveBike(transform.position, transform.eulerAngles.y, false);
            return true;
        }

        /// <summary>Put the bike somewhere (parking, summon, reload), hovering over the ground there.</summary>
        public void Place(Vector3 at, float yaw)
        {
            at.y = Ground.Height(at.x, at.z);
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

        /// <summary>Nearest point the bike may be at (inside a riding zone, outside every obstacle circle).</summary>
        Vector3 ClampBike(Vector3 p, Vector3 from)
        {
            if (!VenusLayout.BikeZone(p, 0.25f))
            {
                // slide along the edge: try the two axis moves, else stay
                var a = new Vector3(p.x, p.y, from.z); var b = new Vector3(from.x, p.y, p.z);
                if (VenusLayout.BikeZone(a, 0.25f)) p = a; else if (VenusLayout.BikeZone(b, 0.25f)) p = b; else p = from;
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
            if (Riding)
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
                foreach (var c in Ground.Obstacles)
                {
                    var to = new Vector3(c.C.x - transform.position.x, 0f, c.C.y - transform.position.z);
                    float d = to.magnitude - c.R;
                    if (d < 6f && Vector3.Dot(to.normalized, transform.forward) > 0.5f) { cap = Mathf.Min(cap, Mathf.Lerp(1.6f, MaxSpeed, (d - 1.5f) / 4.5f)); }
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
                if (VenusLayout.BikeZone(p, 1.6f) && Time.time > _safeT) { _safeT = Time.time + 1.5f; LastSafe = p; }
                if (Time.time > _saveT) { _saveT = Time.time + 3f; VenusState.SaveBike(transform.position, transform.eulerAngles.y, true); }
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
                float slope = float.IsNaN(GlideBottom) ? Mathf.Clamp(Mathf.Atan2(ahead - behind, 2.6f) * Mathf.Rad2Deg, -18f, 18f) : 0f;
                _pitch = Mathf.Lerp(_pitch, slope, dt * 6f);
                Visual.localRotation = Quaternion.Euler(-Speed * 0.25f - _pitch, 0f, _lean);
            }
            float glow = Riding ? 0.7f + 0.3f * Mathf.Clamp01(Speed / MaxSpeed) : 0.45f;
            if (VentLight != null) VentLight.intensity = 0.6f * glow;
            if (VentGlow != null) VentGlow.material.SetColor("_EmissionColor", Color.white * (0.8f + 1.2f * glow));
            if (_hum != null) { _hum.volume = (Riding ? 0.35f + 0.4f * Mathf.Clamp01(Speed / MaxSpeed) : 0.18f); _hum.pitch = 0.9f + 0.35f * Mathf.Clamp01(Speed / MaxSpeed); }
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
