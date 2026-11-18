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
        float _lean, _bob, _stuckFor, _saveT, _safeT;
        SevenTableIK _ik;

        void Start()
        {
            LastSafe = transform.position;
            _hum = AudioKit.Loop("bike_hum", transform, null, 0.0f, AudioKit.Bus.Sfx);
        }

        public bool CanMount(Vector3 p) => !Riding && Flat(p - transform.position).magnitude < 2.4f;

        public void Mount()
        {
            if (Riding || Rider == null) return;
            Riding = true;
            Rider.BeginRide(Seat);
            _ik = Rider.GetComponent<SevenTableIK>() ?? Rider.gameObject.AddComponent<SevenTableIK>();
            _ik.Alive = false; _ik.Ease = 4f; _ik.RightW = 1f; _ik.LeftW = 1f; _ik.LookW = 0.4f;
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
            if (_ik != null) { _ik.RightW = 0f; _ik.LeftW = 0f; _ik.LookW = 0f; }
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
            if (!VenusLayout.BikeZone(p, 0.7f))
            {
                // slide along the edge: try the two axis moves, else stay
                var a = new Vector3(p.x, p.y, from.z); var b = new Vector3(from.x, p.y, p.z);
                if (VenusLayout.BikeZone(a, 0.7f)) p = a; else if (VenusLayout.BikeZone(b, 0.7f)) p = b; else p = from;
            }
            foreach (var c in Ground.Obstacles)
            {
                var d = new Vector2(p.x - c.C.x, p.z - c.C.y); float r = c.R + 0.9f;
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
                Visual.localPosition = new Vector3(0f, h, 0f);
                Visual.localRotation = Quaternion.Euler(-Speed * 0.25f, 0f, _lean);
            }
            float glow = Riding ? 0.7f + 0.3f * Mathf.Clamp01(Speed / MaxSpeed) : 0.45f;
            if (VentLight != null) VentLight.intensity = 0.6f * glow;
            if (VentGlow != null) VentGlow.material.SetColor("_EmissionColor", Color.white * (0.8f + 1.2f * glow));
            if (_hum != null) { _hum.volume = (Riding ? 0.35f + 0.4f * Mathf.Clamp01(Speed / MaxSpeed) : 0.18f); _hum.pitch = 0.9f + 0.35f * Mathf.Clamp01(Speed / MaxSpeed); }
        }

        void LateUpdate()
        {
            if (!Riding || Rider == null) return;
            Rider.RideSeat(Seat);
            if (_ik != null && GripL != null && GripR != null)
            {
                _ik.LeftPos = GripL.position; _ik.RightPos = GripR.position;
                _ik.LookPos = transform.position + transform.forward * 12f + Vector3.up * 1.2f;
            }
        }
    }
}
