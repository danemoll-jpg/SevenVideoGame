using System;
using System.Collections;
using UnityEngine;
using T = Seven.MoonTraversal;

namespace Seven
{
    /// <summary>
    /// Round 9: 7's traversal in Chapter 1 - assisted, forgiving, Tomb-Raider-style moves on top of the walkable zones:
    /// standing / running jump (assisted across known gaps), jump-to-grab, ledge grab while jumping at a wall, hang,
    /// shimmy, climb up, let go (short drops play Freehang Drop), climb down to a hang at an edge, wall climb, waist-high
    /// mantle, crouch-sneak (toggle; the pipe run only takes her crouched). A fall into a chasm never hurts her: the
    /// director fades to white and returns her to the last safe ledge (<see cref="FellOff"/>).
    /// Hand IK holds both hands on the lip while hanging. Check runs press the buttons through ScriptJump / ScriptCrouch /
    /// ScriptLetGo and steer with ScriptMove (world direction; on a ledge its sideways part shimmies, toward the wall climbs).
    /// </summary>
    public partial class ChapterSeven
    {
        const float G = 14f;

        [NonSerialized] public bool Crouched;
        [NonSerialized] public bool ScriptJump, ScriptCrouch, ScriptLetGo;
        /// <summary>Check runs: a plain jump without the gap assist (deliberate falls).</summary>
        [NonSerialized] public bool NoAssist;
        /// <summary>Traversal events: jump, land, grab, shimmy, climb, wallclimb, mantle, climbdown, letgo, crouch, stand, fall.</summary>
        public event Action<string> Did;
        /// <summary>A fall into a chasm (no ground below): the director fades to white and calls <see cref="ReturnToSafe"/>.</summary>
        public Action<Vector3> FellOff;
        public Vector3 LastSafe { get; private set; }
        public Vector3 LastSafeFacing { get; private set; } = Vector3.forward;
        public T.Ledge HangLedge { get; private set; }
        public float HangU { get; private set; }
        /// <summary>What a button would do right here (for the context prompt): "jump:Climb", "crouch:Climb down", ...</summary>
        public string Context { get; private set; }
        public float CameraLift => _camLift;
        public float LastFallHeight { get; private set; }
        public string LastMove { get; private set; }
        public int Falls { get; private set; }

        Coroutine _tr;
        Vector3 _vel; float _airT, _peakY, _noGrabUntil, _landT, _towardFor, _camLift;
        bool _inPlace, _falling, _hardLanding;
        string _airClip;
        float _ikW; T.Ledge _ikLedge;

        bool Consume(ref bool b) { bool v = b; b = false; return v; }
        bool PressedJump() => ScriptDriven ? Consume(ref ScriptJump) : Input != null && Input.Jump.WasPressedThisFrame();
        bool PressedCrouch() => ScriptDriven ? Consume(ref ScriptCrouch) : Input != null && Input.Crouch.WasPressedThisFrame();
        bool PressedLetGo() => ScriptDriven ? Consume(ref ScriptLetGo) : Input != null && Input.LetGo.WasPressedThisFrame();

        void Emit(string what) { LastMove = what; Did?.Invoke(what); }

        void BeginTraversal(IEnumerator r)
        {
            if (_tr != null) StopCoroutine(_tr);
            Current = Mode.Climb; Speed = 0f;
            _tr = StartCoroutine(r);
        }

        void StopTraversal()
        {
            if (_tr != null) StopCoroutine(_tr);
            _tr = null; _ikW = 0f; _ikLedge = null; HangLedge = null; _falling = false; Crouched = false;
            if (Current == Mode.Air || Current == Mode.Hang || Current == Mode.Climb || Current == Mode.Land) Current = Mode.Free;
        }

        /// <summary>Back on the last safe ledge (after the white fade). No injury, no animation.</summary>
        public void ReturnToSafe()
        {
            Place(LastSafe, LastSafeFacing);
        }

        // ------------------------------------------------------------------------------------------ queries
        bool LowOnly()
        {
            var p = transform.position; bool low = false, normal = false;
            foreach (var z in Ground.Zones)
            {
                if (!z.Enabled || !z.Contains(p.x, p.z, 0.2f)) continue;
                if (z.Low) low = true; else normal = true;
            }
            return low && !normal;
        }

        void UpdateSafe()
        {
            var p = transform.position;
            foreach (var z in Ground.Zones)
            {
                if (!z.Enabled || z.Low || !z.Contains(p.x, p.z)) continue;
                if (Mathf.Abs(z.HeightAt(p.x, p.z) - p.y) > 0.1f) continue;
                float px = Mathf.Min(0.45f, (z.Max.x - z.Min.x) * 0.45f), pz = Mathf.Min(0.45f, (z.Max.y - z.Min.y) * 0.45f);
                var q = new Vector3(Mathf.Clamp(p.x, z.Min.x + px, z.Max.x - px), 0f, Mathf.Clamp(p.z, z.Min.y + pz, z.Max.y - pz));
                q.y = z.HeightAt(q.x, q.z);
                LastSafe = q; LastSafeFacing = transform.forward;
                return;
            }
        }

        T.HangDown FindHangDown()
        {
            foreach (var h in T.HangDowns) if (h.Ledge != null && T.Near(transform.position, h.Spot, h.Radius)) return h;
            return null;
        }

        string FreeContext()
        {
            var p = transform.position;
            if (FindHangDown() != null) return "crouch:Climb down";
            foreach (var w in T.Climbs) if (T.Near(p, w.Base, w.Radius)) return "jump:Climb";
            foreach (var m in T.Mantles) if (T.Near(p, m.From, m.Radius)) return "jump:Climb up";
            foreach (var g in T.Grabs) if (T.Near(p, g.From, g.Radius)) return "jump:Grab the ledge";
            foreach (var j in T.Jumps) if (T.Near(p, j.From, j.Radius)) return "jump:Jump";
            return null;
        }

        /// <summary>The usable zone whose surface the point would cross between two heights (landing), or null.</summary>
        static Ground.Zone Crossing(float y0, float y1, float x, float z, out float h)
        {
            h = float.MinValue; Ground.Zone best = null;
            foreach (var zn in Ground.Zones)
            {
                if (!zn.Usable || !zn.Contains(x, z)) continue;
                float zh = zn.HeightAt(x, z);
                if (y0 >= zh - 0.03f && y1 <= zh + 0.001f && zh > h) { h = zh; best = zn; }
            }
            return best;
        }

        static bool AnyGroundBelow(Vector3 p)
        {
            foreach (var zn in Ground.Zones) if (zn.Usable && zn.Contains(p.x, p.z) && zn.HeightAt(p.x, p.z) <= p.y + 0.05f) return true;
            return false;
        }

        readonly Collider[] _hits = new Collider[8];
        bool Blocked(Vector3 p)
        {
            int n = Physics.OverlapCapsuleNonAlloc(p + Vector3.up * 0.4f, p + Vector3.up * 1.45f, 0.2f, _hits, ~(1 << 20), QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (!_hits[i].transform.IsChildOf(transform)) return true;
            return false;
        }

        // ------------------------------------------------------------------------------------------ jumps
        bool TryJump(Vector3 move, bool run)
        {
            var p = transform.position;
            foreach (var w in T.Climbs) if (T.Near(p, w.Base, w.Radius)) { BeginTraversal(WallClimbRoutine(w)); return true; }
            foreach (var m in T.Mantles) if (T.Near(p, m.From, m.Radius)) { BeginTraversal(MantleRoutine(m)); return true; }
            foreach (var g in T.Grabs) if (T.Near(p, g.From, g.Radius)) { BeginTraversal(JumpGrabRoutine(g)); return true; }
            if (!NoAssist)
                foreach (var j in T.Jumps) if (T.Near(p, j.From, j.Radius)) { LinkJump(j); return true; }
            if (move.sqrMagnitude > 0.04f)
            {
                float speed = (run ? RunSpeed : WalkSpeed) * move.magnitude;
                var dir = move.normalized;
                float dist = Mathf.Clamp(speed * 0.62f, 0.9f, 2.6f);
                ArcJump(p + dir * dist, 0.62f, dir);
            }
            else StandJump();
            Emit("jump");
            return true;
        }

        void LinkJump(T.JumpLink j)
        {
            var to = j.To;
            var d = j.To - j.From; d.y = 0f;
            if (j.KeepLateral)
            {
                var p = transform.position;
                if (Mathf.Abs(d.x) > Mathf.Abs(d.z)) to.z = Mathf.Clamp(p.z, j.LateralClamp.x, j.LateralClamp.y);
                else to.x = Mathf.Clamp(p.x, j.LateralClamp.x, j.LateralClamp.y);
            }
            var flat = to - transform.position; flat.y = 0f;
            float T0 = 0.62f + 0.08f * Mathf.Max(0f, flat.magnitude - 2.5f);
            ArcJump(to, T0, flat.normalized);
            Emit("jump");
        }

        /// <summary>Ballistic jump that lands on <paramref name="to"/> after <paramref name="T0"/> s (running jump clip).</summary>
        void ArcJump(Vector3 to, float T0, Vector3 dir)
        {
            var from = transform.position;
            // the arc always rises at least 0.55 m above the higher end (a flat arc would land back on the take-off edge)
            float apex = Mathf.Max(from.y, to.y) + 0.55f - from.y;
            float vy = Mathf.Sqrt(2f * G * apex);
            T0 = (vy + Mathf.Sqrt(Mathf.Max(0f, vy * vy + 2f * G * (from.y - to.y)))) / G;
            var flat = to - from; flat.y = 0f;
            _vel = flat / T0 + Vector3.up * vy;
            if (dir.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
            _inPlace = false; _falling = false; _airT = 0f; _peakY = from.y;
            _noGrabUntil = Time.time + 0.12f;
            Current = Mode.Air; Speed = 0f;
            _airClip = HasState("running_jump") ? "running_jump" : "jump";
            // flight f4..f21 of Running Jump (0.57 s) stretched over the arc
            Play(_airClip, 0.08f, _airClip == "running_jump" ? 0.57f / T0 : 1f, true, _airClip == "running_jump" ? 0.11f : 0.25f);
        }

        void StandJump()
        {
            _vel = Vector3.zero; _inPlace = true; _falling = false; _airT = 0f; _peakY = transform.position.y;
            Current = Mode.Air; Speed = 0f;
            _airClip = HasState("jumping_up") ? "jumping_up" : "jump";
            Play(_airClip, 0.1f);
        }

        void UpdateAir()
        {
            Speed = 0f;
            _airT += Time.deltaTime;
            if (_inPlace)
            {
                // Jumping Up (body rises in the pose) chains into Falling To Landing at its first frame.
                if (Tn >= 0.98f)
                {
                    if (_airClip == "jumping_up" && HasState("landing")) { Play("landing", 0f); _landT = 0f; _hardLanding = false; Current = Mode.Land; Emit("land"); }
                    else { Current = Mode.Free; Play("idle", 0.2f); }
                }
                return;
            }
            float dt = Time.deltaTime;
            int steps = Mathf.Max(1, Mathf.CeilToInt((_vel.magnitude * dt) / 0.12f));
            for (int s = 0; s < steps; s++)
            {
                float h = dt / steps;
                var p = transform.position;
                var np = p + _vel * h;
                _vel.y -= G * h;
                var hp = new Vector3(np.x, p.y, np.z);
                if ((new Vector3(_vel.x, 0f, _vel.z)).sqrMagnitude > 0f && Blocked(hp)) { np.x = p.x; np.z = p.z; _vel.x = 0f; _vel.z = 0f; }
                _peakY = Mathf.Max(_peakY, np.y);
                if (_vel.y < 0f)
                {
                    var zn = Crossing(p.y, np.y, np.x, np.z, out float zh);
                    if (zn != null) { transform.position = new Vector3(np.x, zh, np.z); Land(zh); return; }
                }
                transform.position = np;
                if (!_falling && Time.time >= _noGrabUntil && TryAirGrab()) return;
            }
            if (_airClip != "fall_idle" && (_airT > 0.75f || (_vel.y < -4f)) && HasState("fall_idle")) { _airClip = "fall_idle"; Play("fall_idle", 0.3f); }
            if (!_falling && !AnyGroundBelow(transform.position) && (transform.position.y < LastSafe.y - 1.4f || _airT > 1.8f))
            {
                _falling = true; Falls++;
                Emit("fall");
                if (FellOff != null) FellOff(transform.position); else ReturnToSafe();
            }
            if (_falling) _vel.y = Mathf.Max(_vel.y, -9f);
        }

        void Land(float h)
        {
            LastFallHeight = _peakY - h;
            var move = MoveInput(out _);
            _landT = 0f;
            _hardLanding = LastFallHeight > 2.6f && HasState("landing_hard");
            if (_hardLanding)
            {
                // Hard Landing carries the body 0.88 m forward by touchdown (baked): start the root that far back.
                var f = transform.forward; f.y = 0f; f.Normalize();
                transform.position -= f * 0.88f;
                Play("landing_hard", 0f, 1f, true, 0.26f);
                Current = Mode.Land;
            }
            else if (move.sqrMagnitude > 0.1f) { Current = Mode.Free; Play(move.magnitude > 0.7f ? "run" : "walk", 0.18f); }
            else { Play(HasState("landing") ? "landing" : "idle", 0.08f, 1f, true, HasState("landing") ? 0.27f : 0f); Current = Mode.Land; }
            Emit("land");
        }

        void UpdateLand()
        {
            Speed = 0f;
            _landT += Time.deltaTime;
            if (_hardLanding)
            {
                if (Tn >= 0.93f)
                {
                    var f = transform.forward; f.y = 0f; f.Normalize();
                    transform.position += f * 0.97f;
                    Play("idle", 0f); Current = Mode.Free; ClampToArena();
                }
                return;
            }
            var move = MoveInput(out _);
            if (_landT > 0.42f || (_landT > 0.2f && move.sqrMagnitude > 0.1f)) { Current = Mode.Free; if (move.sqrMagnitude <= 0.1f) Play("idle", 0.3f); }
        }

        bool TryAirGrab()
        {
            var p = transform.position;
            var fv = new Vector3(_vel.x, 0f, _vel.z);
            foreach (var L in T.Ledges)
            {
                float raw = Vector3.Dot(p - L.A, L.Dir);
                if (raw < 0.2f || raw > L.Length - 0.2f) continue;
                var q = L.At(raw);
                var d3 = q - p; float dh = d3.y; d3.y = 0f;
                float d = Vector3.Dot(d3, L.Fwd);
                if (d < 0.05f || d > 0.75f) continue;
                if ((d3 - L.Fwd * d).magnitude > 0.3f) continue;
                if (dh < 1.55f || dh > 2.65f) continue;
                if (Vector3.Dot(fv, L.Fwd) < 0.2f && d > 0.4f) continue;
                StartHang(L, raw, 0.15f);
                Emit("grab");
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------------------------------------ hang
        /// <summary>QF8 camera sweep only: put her straight into a hang on a ledge.</summary>
        public void DebugHang(T.Ledge L, float u) { StopTraversal(); Release(); StartHang(L, u, 0f); }
        void StartHang(T.Ledge L, float u, float fade)
        {
            HangLedge = L; HangU = Mathf.Clamp(u, 0.2f, L.Length - 0.2f);
            transform.position = L.HangRoot(HangU);
            transform.rotation = Quaternion.LookRotation(L.Fwd);
            Play("hang_idle", fade);
            _ikLedge = L; _ikW = 1f; _towardFor = 0f;
            Current = Mode.Hang;
        }

        void UpdateHang()
        {
            Speed = 0f;
            var L = HangLedge;
            if (L == null) { Current = Mode.Free; return; }
            var v = MoveInput(out _);
            float lat = Vector3.Dot(v, L.Dir), toward = Vector3.Dot(v, L.Fwd);
            bool canClimb = L.CanClimb(HangU);
            _towardFor = toward > 0.6f && Mathf.Abs(lat) < 0.5f ? _towardFor + Time.deltaTime : 0f;
            bool climb = PressedJump() || _towardFor > 0.18f;
            var below = T.ZoneUnder(transform.position);
            Context = canClimb ? "up:Climb up" : below != null ? "letgo:Let go" : "lr:Shimmy";
            if (PressedLetGo()) { LetGo(below); return; }
            if (climb && canClimb) { BeginTraversal(ClimbUpRoutine(L, HangU)); return; }
            if (Mathf.Abs(lat) > 0.35f)
            {
                float before = HangU;
                HangU = Mathf.Clamp(HangU + Mathf.Sign(lat) * T.ShimmySpeed * Mathf.Min(1f, Mathf.Abs(lat) * 1.3f) * Time.deltaTime, 0.2f, L.Length - 0.2f);
                if (Mathf.Abs(HangU - before) > 1e-5f)
                {
                    var right = Vector3.Cross(Vector3.up, L.Fwd);
                    bool toRight = Vector3.Dot(L.Dir * Mathf.Sign(lat), right) > 0f;
                    string clip = toRight && HasState("shimmy_mirror") ? "shimmy_mirror" : "shimmy";
                    if (State != clip) { Play(clip, 0.2f); Emit("shimmy"); }
                }
                else if (State != "hang_idle") Play("hang_idle", 0.25f);
            }
            else if (State != "hang_idle") Play("hang_idle", 0.3f);
            transform.position = Vector3.MoveTowards(transform.position, L.HangRoot(HangU), 2f * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(L.Fwd), 360f * Time.deltaTime);
        }

        void LetGo(Ground.Zone below)
        {
            var L = HangLedge;
            float lip = L.At(HangU).y;
            float h = below != null ? below.HeightAt(transform.position.x, transform.position.z) : float.NaN;
            Emit("letgo");
            if (below != null && lip - h >= 2.0f && lip - h <= 2.9f && HasState("hang_drop")) { BeginTraversal(HangDropRoutine(L, HangU, h)); return; }
            HangLedge = null; _ikW = 0f; _ikLedge = null;
            _vel = -L.Fwd * 0.5f; _inPlace = false; _falling = false; _airT = 0f; _peakY = transform.position.y;
            _noGrabUntil = Time.time + 0.5f;
            _airClip = HasState("fall_idle") ? "fall_idle" : "idle";
            Play(_airClip, 0.2f);
            Current = Mode.Air;
        }

        // ------------------------------------------------------------------------------------------ scripted moves
        IEnumerator WaitTn(float tn, float maxSeconds = 6f)
        {
            for (float t = 0f; Tn < tn && t < maxSeconds; t += Time.deltaTime) yield return null;
        }

        IEnumerator MoveTurn(Vector3 to, Vector3 face, float seconds)
        {
            var p0 = transform.position; var r0 = transform.rotation; var r1 = Quaternion.LookRotation(new Vector3(face.x, 0f, face.z));
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / seconds);
                transform.position = Vector3.Lerp(p0, to, u); transform.rotation = Quaternion.Slerp(r0, r1, u);
                yield return null;
            }
            transform.position = to; transform.rotation = r1;
        }

        /// <summary>Braced Hang To Crouch from a hang on the lip (or as the end of a wall climb / a mantle).</summary>
        IEnumerator ClimbUpRoutine(T.Ledge L, float u, float startTn = 0f, float ikFrom = 1f)
        {
            Current = Mode.Climb; Crouched = false;
            var P = L.At(u); var F = L.Fwd;
            var o = P - F * T.HangOut - Vector3.up * T.ClimbUpRise;
            transform.position = o; transform.rotation = Quaternion.LookRotation(F);
            HangLedge = null; _ikLedge = L; _ikW = ikFrom;
            Play("climb_up", 0f, 1f, true, startTn);
            for (float t = 0f; Tn < 0.97f && t < 4f; t += Time.deltaTime)
            {
                _ikW = ikFrom * Mathf.Clamp01(1f - (Tn - startTn) / 0.4f);
                yield return null;
            }
            _ikW = 0f; _ikLedge = null;
            transform.position = o + F * T.ClimbUpAdvance + Vector3.up * T.ClimbUpRise;
            Play(HasState("crouch_idle") ? "crouch_idle" : "idle", 0f);
            yield return null; yield return null;
            Play("idle", 0.35f);
            Current = Mode.Free; ClampToArena(); _tr = null;
            Emit("climb");
        }

        IEnumerator WallClimbRoutine(T.WallClimb w)
        {
            var L = w.Ledge; float u = L.U(w.Base); var P = L.At(u); var F = L.Fwd;
            var b = P - F * 0.25f; b.y = w.Base.y;
            Play("walk", 0.15f);
            yield return MoveTurn(b, F, 0.3f);
            Emit("wallclimb");
            Play("wall_climb", 0.2f);
            for (int k = 0; k < w.Cycles; k++)
            {
                yield return WaitTn(0.98f);
                if (k < w.Cycles - 1) { transform.position += Vector3.up * T.WallClimbStep; Play("wall_climb", 0f); }
            }
            // Hands are on the lip now: pull up (climb_up started where its hips match the end of the climb).
            yield return ClimbUpRoutine(L, u, 0.17f, 0.6f);
        }

        IEnumerator MantleRoutine(T.Mantle m)
        {
            var F = m.Fwd;
            var p = transform.position;
            // put her 0.32 m in front of the face, keeping her sideways position
            var start = p;
            bool alongX = Mathf.Abs(F.x) > 0.5f;
            float want = m.FaceAt - Mathf.Sign(alongX ? F.x : F.z) * 0.32f;
            if (alongX) start.x = want; else start.z = want;
            Play("walk", 0.15f);
            yield return MoveTurn(start, F, 0.25f);
            var o = start; o.y = m.TopY - T.ClimbUpRise;
            transform.position = o;
            Play("climb_up", 0f, 1.15f, true, T.MantleStart);
            Emit("mantle");
            yield return WaitTn(0.97f);
            transform.position = o + F * T.ClimbUpAdvance + Vector3.up * T.ClimbUpRise;
            Play(HasState("crouch_idle") ? "crouch_idle" : "idle", 0f);
            yield return null; yield return null;
            Play("idle", 0.35f);
            Current = Mode.Free; ClampToArena(); _tr = null;
        }

        IEnumerator JumpGrabRoutine(T.JumpGrab g)
        {
            var L = g.Ledge;
            float u = Mathf.Clamp(L.U(transform.position), 0.3f, L.Length - 0.3f);
            var P = L.At(u); var F = L.Fwd;
            var S = transform.position;
            var E = P - F * (T.JumpHangHips + 0.22f); E.y = P.y - T.JumpHangUp;
            transform.rotation = Quaternion.LookRotation(F);
            Emit("jump");
            Play("jump_to_hang", 0.1f);
            _ikLedge = L; _ikW = 0f;
            for (float t = 0f; Tn < 0.72f && t < 4f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Tn / 0.27f));
                transform.position = Vector3.Lerp(S, E, k);
                _ikW = Mathf.Clamp01((Tn - 0.18f) / 0.12f);
                yield return null;
            }
            StartHang(L, u, 0f);
            _tr = null;
            Emit("grab");
        }

        /// <summary>Stand-in (no Mixamo clip for lowering herself): turn to face the wall side, crouch at the edge,
        /// then slide down into the hang while the hands take the lip (IK).</summary>
        IEnumerator HangDownRoutine(T.HangDown h)
        {
            Crouched = false;
            var L = h.Ledge;
            float u = Mathf.Clamp(L.U(transform.position), 0.3f, L.Length - 0.3f);
            var P = L.At(u); var F = L.Fwd;
            Play(HasState("crouch_idle") ? "crouch_idle" : "idle", 0.25f);
            yield return MoveTurn(P + F * 0.32f, F, 0.4f);
            Emit("climbdown");
            Play("hang_idle", 0.45f);
            _ikLedge = L;
            var a = transform.position; var b = L.HangRoot(u);
            for (float t = 0f; t < 0.55f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 0.55f);
                transform.position = Vector3.Lerp(a, b, k);
                _ikW = Mathf.Clamp01((k - 0.4f) / 0.5f);
                yield return null;
            }
            StartHang(L, u, 0.1f);
            _tr = null;
        }

        /// <summary>Freehang Drop: from the hang to the floor below (lip 2.0-2.9 m above it); the clip steps 0.44 m back.</summary>
        IEnumerator HangDropRoutine(T.Ledge L, float u, float floor)
        {
            float lip = L.At(u).y;
            var root = L.HangRoot(u); root.y = lip - 2.45f;
            transform.position = root;
            HangLedge = null; _ikLedge = L; _ikW = 1f;
            Play("hang_drop", 0.05f);
            float y0 = root.y;
            for (float t = 0f; Tn < 0.97f && t < 3f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.5f, Tn));
                var p = transform.position; p.y = Mathf.Lerp(y0, floor, k); transform.position = p;
                _ikW = Mathf.Clamp01(1f - Tn / 0.25f);
                yield return null;
            }
            _ikW = 0f; _ikLedge = null;
            var q = transform.position; q.y = floor; q -= L.Fwd * 0.44f;
            transform.position = q;
            Play("idle", 0f);
            Current = Mode.Free; ClampToArena(); _tr = null;
            LastFallHeight = lip - floor;
            Emit("land");
        }

        // ------------------------------------------------------------------------------------------ IK + camera
        void OnAnimatorIK(int layer)
        {
            if (Anim == null) return;
            RideLeanIK();                                  // Round 11: the bike lean, before the hand IK
            if (_ikW <= 0.001f || _ikLedge == null)
            {
                // QF8: leave the hands to SevenTableIK when it is on her (the touch / hold / pocket beats) - this pass used
                // to zero its weights, so her hand never reached the shape
                if (_tableIK == null) _tableIK = GetComponent<SevenTableIK>();
                if (_tableIK != null && _tableIK.enabled) return;
                Anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f); Anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
                return;
            }
            float center = HangLedge != null ? HangU : _ikLedge.U(transform.position);
            foreach (var (goal, bone) in new[] { (AvatarIKGoal.LeftHand, HumanBodyBones.LeftHand), (AvatarIKGoal.RightHand, HumanBodyBones.RightHand) })
            {
                var b = Bone(bone); if (b == null) continue;
                float uu = Mathf.Clamp(_ikLedge.U(b.position), center - 0.42f, center + 0.42f);
                var q = _ikLedge.At(uu) + _ikLedge.Fwd * 0.04f + Vector3.up * 0.035f - WallPush;   // QF8: the body is pushed later

                Anim.SetIKPositionWeight(goal, _ikW);
                Anim.SetIKPosition(goal, q);
            }
        }

        SevenTableIK _tableIK;
        /// <summary>Check runs: hand bones (wrists) vs the lip while hanging - (worst height gap, worst distance along the wall normal).</summary>
        public Vector2 HandGap()
        {
            var L = HangLedge; if (L == null) return new Vector2(-1f, -1f);
            float up = 0f, fw = 0f;
            foreach (var bb in new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand })
            {
                var b = Bone(bb); if (b == null) continue;
                var bp = b.position + (_pushApplied.sqrMagnitude > 0f ? Vector3.zero : WallPush);   // QF8: as rendered
                var q = L.At(L.U(bp));
                up = Mathf.Max(up, Mathf.Abs(bp.y - q.y));
                var d = bp - q; d.y = 0f;
                fw = Mathf.Max(fw, Mathf.Abs(Vector3.Dot(d, L.Fwd)));
            }
            return new Vector2(up, fw);
        }

        void TraversalLateUpdate()
        {
            // hang calibration: wrists (after IK) vs the IK target height on the lip
            if (Current == Mode.Hang && HangLedge != null && _ikW > 0.9f)
            {
                float lip = HangLedge.At(HangU).y + 0.035f, sum = 0f; int n = 0;
                foreach (var bb in new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand }) { var b = Bone(bb); if (b != null) { sum += b.position.y; n++; } }
                if (n > 0)
                {
                    float err = lip - sum / n;
                    if (Mathf.Abs(err) > 0.01f) T.HangLift = Mathf.Clamp(T.HangLift + err * Mathf.Min(1f, 4f * Time.deltaTime), -0.2f, 0.6f);
                }
            }
            float want = Current == Mode.Hang ? 0.3f : Current == Mode.Climb ? 0.35f : 0f;
            _camLift = Mathf.MoveTowards(_camLift, want, 2.2f * Time.deltaTime);
        }
    }
}
