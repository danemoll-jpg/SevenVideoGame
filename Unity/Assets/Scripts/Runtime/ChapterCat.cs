using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// The cat in Chapter 1 (house-cat size throughout, scale 1). Follows 7 at a short distance, waits at interaction
    /// points, leads the way where the script says so (waits for her when she falls behind), hops onto ledges and
    /// paws things. Never gets stuck: if he is more than <see cref="CatchUpDistance"/> behind and out of the camera's
    /// view, he reappears near her where the camera cannot see the jump. Never ridden, never hurt.
    /// </summary>
    public class ChapterCat : MonoBehaviour
    {
        public Transform Seven;
        public Animator Anim;
        public Camera Cam;
        public float StrideSpeed = 0.16f;            // ground speed of the walk clip at speed 1 (scale 1)
        public float StrollSpeed = 0.9f, TrotSpeed = 2.4f, SprintSpeed = 4.4f;
        public float FollowDistance = 1.7f, CatchUpDistance = 8f, BodyRadius = 0.16f;

        public enum Mode { Follow, GoTo, Hold, Lead, Hop, Perched, Scripted, Route }

        /// <summary>Round 9: one stop of a traversal route - walk or hop there once <see cref="Go"/> says so (7 has caught up),
        /// then wait (sitting, watching her). Hops ignore the ground (gaps, ledges, stone stubs).</summary>
        public struct Stop { public Vector3 P; public bool Hop; public float H; public System.Func<bool> Go; }
        public event System.Action<int, Vector3> StopReached;
        List<Stop> _route; int _ri; bool _rMoving, _rHop;
        readonly List<Vector3> _rPts = new List<Vector3>();
        readonly List<float> _rArc = new List<float>();

        // ---- QF5 leash: the cat can never be left behind (Dan's 0.10.0 playtest: he stayed in the cell block).
        /// <summary>QF5: off only for the before/after repro (-noqf5catfix). On = route stops 7 has already passed go at
        /// once, and the leash below is active.</summary>
        public static bool QF5Fix = true;
        /// <summary>Set by the director: true while the player controls 7 (no cutscene, 7 not in her cell).</summary>
        public System.Func<bool> LeashAllowed;
        /// <summary>Set by the director: 7 stands on walkable ground (not hanging / in the air) - only then can he
        /// reappear a step behind her.</summary>
        public System.Func<bool> SevenGrounded;
        public float LeashDistance = 6f, AheadDistance = 14f, StallSeconds = 3f, BlockedSeconds = 1.2f;
        /// <summary>QF5 counters for the checks: catch-ups started, of them finished by trotting / by reappearing.</summary>
        public int Leashes { get; private set; }
        public int LeashTrots { get; private set; }
        public int LeashBlinks { get; private set; }
        public string LastLeash { get; private set; }
        /// <summary>QF5 check: he waits at a route stop for a condition while 7 is already past the next stop (the 0.10.0
        /// bug - he waits there for ever).</summary>
        public bool WaitingBehind => Current == Mode.Route && _route != null && _ri < _route.Count && !_rMoving && ArcOf(Seven.position) > _rArc[_ri + 1] + 1f;
        public int RouteStop => _ri;
        /// <summary>1 = fully shown; below 1 while he fades out / in (QF5 reappear).</summary>
        public float Fade { get; private set; } = 1f;
        bool _chase, _blinking;
        float _chaseBest, _chaseProgT, _chaseT0;
        Mode _chaseFrom;
        readonly Queue<(float t, float d, Vector3 p7)> _samples = new Queue<(float, float, Vector3)>();
        float _sampleT, _moved7;
        Vector3 _last7;
        Renderer[] _rends;
        UnityEngine.Rendering.ShadowCastingMode[] _shadow;
        MaterialPropertyBlock _mpb;
        bool _routePaused;        // left the route to follow her (she wandered off it); resumes when she is back on it
        public Mode Current { get; private set; } = Mode.Follow;
        public bool Arrived { get; private set; }
        public string Clip => _clip;
        public float DistanceTo7 => Flat(Seven.position - transform.position).magnitude;
        public int CatchUps { get; private set; }
        /// <summary>QF4: areas the cat may never be in (7's cell), as x0, z0, x1, z1 rectangles. Every move, hop end,
        /// placement and catch-up is pushed out of them.</summary>
        public static readonly List<Rect> NoCat = new List<Rect>();
        /// <summary>QF4: frames in which a step would have ended inside a no-cat area (pushed out; for the checks).</summary>
        public int NoCatPushes { get; private set; }
        /// <summary>Where the current command ends (GoTo target, last lead point, hop target).</summary>
        public Vector3 Destination => Current == Mode.Lead && _lead != null && _lead.Count > 0 ? _lead[_lead.Count - 1]
            : Current == Mode.Hop || Current == Mode.Perched ? _hopTo : _target;

        Vector3 _target, _look;
        float _speed;
        List<Vector3> _lead;
        int _leadIdx;
        float _waitFor7 = 4.5f;
        string _clip;
        string _holdClip = "sit";
        float _detour;
        // hop
        Vector3 _hopFrom, _hopTo; float _hopT, _hopDur, _hopH;
        float _perchY = float.NaN;

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        void Start() { PlayClip("sit"); }

        void PlayClip(string clip, float fade = 0.25f, float speed = 1f)
        {
            Anim.speed = speed;
            if (_clip == clip) return;
            _clip = clip;
            Anim.CrossFadeInFixedTime(clip, fade, 0);
        }

        // ------------------------------------------------------------------------------------------ commands
        /// <summary>QF4 watchdog fallback: finish the current walk / lead / hop at once (placed on the mark).</summary>
        public void ForceArrive()
        {
            var d = Destination;
            if (Current == Mode.Hop) { transform.position = _hopTo; _perchY = _hopTo.y; Current = Mode.Perched; PlayClip("idle", 0.2f); }
            else
            {
                Place(d, Flat(_look - d).sqrMagnitude > 1e-4f ? Flat(_look - d) : transform.forward);
                if (Current == Mode.Lead) _leadIdx = _lead.Count;
                else if (Current == Mode.GoTo) Current = Mode.Hold;
            }
            Arrived = true;
            Debug.Log($"[Seven] cat watchdog: placed on his mark {transform.position}");
        }

        public void Follow() { Current = Mode.Follow; _perchY = float.NaN; Arrived = false; _routePaused = false; CancelLeash(); }
        public void GoTo(Vector3 p, float speed = -1f, Vector3? lookAt = null, string holdClip = "sit")
        {
            _target = p; _speed = speed > 0f ? speed : TrotSpeed; _look = lookAt ?? Seven.position; _holdClip = holdClip; _stuckT = 0f;
            Current = Mode.GoTo; Arrived = false; _perchY = float.NaN; _routePaused = false; CancelLeash();
        }
        public void Lead(List<Vector3> path, float waitFor7 = 4.5f)
        {
            _lead = path; _leadIdx = 0; _waitFor7 = waitFor7; Current = Mode.Lead; Arrived = false; _perchY = float.NaN; _routePaused = false; CancelLeash();
        }
        public void Hold(Vector3 lookAt, string clip = "sit") { _look = lookAt; _holdClip = clip; Current = Mode.Hold; _routePaused = false; CancelLeash(); }
        /// <summary>Round 9: lead a traversal route: go ahead to each stop, wait there until 7 catches up (never blocks her).</summary>
        public void Route(List<Stop> stops)
        {
            _route = stops; _ri = 0; _rMoving = false; _rHop = false; Current = Mode.Route; Arrived = false; _perchY = float.NaN; _routePaused = false; CancelLeash();
            // QF5: the route as a polyline (his start + every stop) with the arc length at each stop, so "has 7 already
            // gone past this stop?" can be answered wherever she is (see ArcOf).
            _rPts.Clear(); _rArc.Clear();
            _rPts.Add(transform.position); _rArc.Add(0f);
            foreach (var s in stops) { _rArc.Add(_rArc[_rArc.Count - 1] + Vector3.Distance(_rPts[_rPts.Count - 1], s.P)); _rPts.Add(s.P); }
        }
        public void Hop(Vector3 to, float duration = 0.8f, float height = 0.45f)
        {
            CancelLeash();
            _hopFrom = transform.position; _hopTo = to; _hopT = 0f; _hopDur = duration; _hopH = height;
            Current = Mode.Hop; Arrived = false;
            PlayClip("jump", 0.1f, 0.8f / duration);
        }
        public void Paw() { PlayClip("wash", 0.2f, 1.6f); }
        public void Place(Vector3 p, Vector3 facing)
        {
            p = OutOfNoCat(p);
            transform.position = new Vector3(p.x, Ground.Height(p.x, p.z), p.z);
            facing.y = 0f; if (facing.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(facing);
            _perchY = float.NaN;
        }
        /// <summary>Scripted: an outside script moves him (liftoff boarding etc.).</summary>
        public void Scripted(string clip) { Current = Mode.Scripted; PlayClip(clip); _routePaused = false; CancelLeash(); }
        /// <summary>Round 7 tractor beam: held mid-air frame of the jump clip (legs hanging), drifting very slowly.</summary>
        public void Floating()
        {
            CancelLeash();
            Current = Mode.Scripted; _clip = "float";
            Anim.CrossFadeInFixedTime("jump", 0.25f, 0, 0.42f * 0.8f);
            Anim.speed = 0.04f;
        }

        // ------------------------------------------------------------------------------------------ motion
        /// <summary>Push a point out of the no-cat areas (to the nearest edge plus the body radius).</summary>
        public Vector3 OutOfNoCat(Vector3 p)
        {
            foreach (var r in NoCat)
            {
                float x0 = r.xMin - BodyRadius, x1 = r.xMax + BodyRadius, z0 = r.yMin - BodyRadius, z1 = r.yMax + BodyRadius;
                if (p.x <= x0 || p.x >= x1 || p.z <= z0 || p.z >= z1) continue;
                NoCatPushes++;
                float dx0 = p.x - x0, dx1 = x1 - p.x, dz0 = p.z - z0, dz1 = z1 - p.z;
                float m = Mathf.Min(Mathf.Min(dx0, dx1), Mathf.Min(dz0, dz1));
                if (m == dz1) p.z = z1 + 0.01f; else if (m == dz0) p.z = z0 - 0.01f; else if (m == dx0) p.x = x0 - 0.01f; else p.x = x1 + 0.01f;
            }
            return p;
        }

        public static bool InNoCat(Vector3 p, float pad = 0f)
        {
            foreach (var r in NoCat) if (p.x > r.xMin - pad && p.x < r.xMax + pad && p.z > r.yMin - pad && p.z < r.yMax + pad) return true;
            return false;
        }

        /// <summary>QF4: the move is split into short sub-steps (&lt;= 6 cm), so a long frame (a load hitch) can never
        /// carry him across a wall into another walkable zone (the bug that let him into 7's cell).</summary>
        bool MoveTo(Vector3 target, float speed, float arrive = 0.12f)
        {
            var to0 = Flat(target - transform.position);
            if (to0.magnitude < arrive) return true;
            float total = Mathf.Min(to0.magnitude, speed * Time.deltaTime);
            int steps = Mathf.Max(1, Mathf.CeilToInt(total / 0.06f));
            float len = total / steps;
            var start = transform.position;
            Vector3 p = transform.position;
            // QF11 (the real cause of Dan's "walks in place"): standing inside a ground obstacle (the landed saucer's pad),
            // every step was clamped out of it and rejected as a jump - he trod on the spot. Steps that lead him out are fine.
            bool inside = Flat(Ground.ClampToArena(p, BodyRadius) - p).magnitude > 0.02f;
            for (int s = 0; s < steps; s++)
            {
                var to = Flat(target - p);
                if (to.magnitude < 1e-4f) break;
                bool moved1 = false;
                foreach (var turn in new[] { 0f, 40f, -40f, 80f, -80f, 120f, -120f })
                {
                    var dir = Quaternion.Euler(0f, turn + _detour, 0f) * to.normalized;
                    var q = Ground.ClampToArena(p + dir * Mathf.Min(len, to.magnitude), BodyRadius);
                    // never accept a step the clamp turned into a jump (snapping across a wall into another walkable zone)
                    if (Flat(q - p).magnitude > len + (inside ? 0.6f : 0.05f)) continue;
                    if (InNoCat(q, BodyRadius)) continue;
                    if (Vector3.Dot(Flat(q - p), dir) > Mathf.Min(len, to.magnitude) * 0.6f)
                    {
                        p = q; moved1 = true;
                        if (turn != 0f) _detour = Mathf.Clamp(_detour + turn * 0.02f, -90f, 90f); else _detour *= 0.9f;
                        break;
                    }
                }
                if (!moved1) break;
            }
            p = OutOfNoCat(p);
            var moved = Flat(p - start);
            p.y = Ground.Height(p.x, p.z);
            transform.position = p;
            if (moved.sqrMagnitude > 1e-7f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(moved), 420f * Time.deltaTime);
            // QF11 (Dan: he walked in place in C01): the walk clip follows his REAL ground speed - blocked or barely moving
            // = idle (short blend), never the walk cycle on the spot; the clip's rate matches the speed he actually covers
            float dt = Mathf.Max(1e-4f, Time.deltaTime), sc = Mathf.Max(0.1f, transform.lossyScale.x);
            _gs = Mathf.Lerp(_gs, moved.magnitude / dt, 1f - Mathf.Exp(-12f * dt));
            if (_gs > WalkMin * sc) { PlayClip("walk", 0.2f, Mathf.Clamp(_gs / sc / StrideSpeed * 0.25f, 0.6f, 4f)); _stuckT = 0f; }
            else { PlayClip("idle", 0.2f); _stuckT += dt; }
            return false;
        }
        /// <summary>QF11: below this ground speed (m/s at scale 1) he is standing, not walking.</summary>
        public const float WalkMin = 0.06f;
        float _gs, _stuckT, _measured; Vector3 _lastPos; bool _havePos; float _stillT;
        /// <summary>QF11 check: his measured ground speed (m/s, smoothed), frames with the walk clip, and of those frames
        /// the ones with him (nearly) not moving - must stay 0.</summary>
        public float MeasuredSpeed => _measured;
        public static int WalkFrames, WalkInPlaceFrames;
        public static float WorstInPlace;
        public static string SpeedReport() => $"cat speed vs clip (QF11): walk clip {WalkFrames} frames, walking in place (speed < {WalkMin:0.00} m/s x scale for > 0.25 s, then switched to idle) {WalkInPlaceFrames}x";

        void LateUpdate()
        {
            float dt = Mathf.Max(1e-4f, Time.deltaTime), sc = Mathf.Max(0.1f, transform.lossyScale.x);
            var p = transform.position;
            if (_havePos) _measured = Mathf.Lerp(_measured, Flat(p - _lastPos).magnitude / dt, 1f - Mathf.Exp(-10f * dt));
            _lastPos = p; _havePos = true;
            if (_clip == "walk" && Time.timeScale > 0f)
            {
                WalkFrames++;
                // a safety net for every caller (scripted moves included): the walk clip with no ground speed -> idle
                if (_measured < WalkMin * sc) { _stillT += dt; if (_stillT > 0.25f) { WalkInPlaceFrames++; PlayClip("idle", 0.2f); } }
                else _stillT = 0f;
            }
            else _stillT = 0f;
        }

        void Face(Vector3 p)
        {
            var d = Flat(p - transform.position);
            if (d.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 300f * Time.deltaTime);
        }

        void Update()
        {
            if (QF5Fix && Leash()) return;
            switch (Current)
            {
                case Mode.Follow:
                {
                    if (_routePaused && _route != null && ArcOf(Seven.position) >= 0f) { ResumeRoute("7 is back on the route"); break; }
                    float d = DistanceTo7;
                    if (d > CatchUpDistance && TryCatchUp()) break;
                    if (d > FollowDistance + 0.6f)
                    {
                        var side = Flat(transform.position - Seven.position).normalized;
                        var t = Seven.position + side * FollowDistance;
                        MoveTo(t, d > 4f ? SprintSpeed : d > 2.6f ? TrotSpeed : StrollSpeed);
                    }
                    else { PlayClip(_clip == "walk" ? "idle" : _clip == "idle" && Time.time % 9f > 6f ? "sit" : _clip ?? "idle", 0.3f); Face(Seven.position); }
                    break;
                }
                case Mode.GoTo:
                    // QF11: blocked short of the mark (a hull, a rail) for 1.5 s -> he stops there and holds, never treads on the spot
                    if (MoveTo(_target, _speed) || _stuckT > 1.5f) { if (_stuckT > 1.5f) Debug.Log($"[Seven] cat: blocked {Flat(_target - transform.position).magnitude:0.00} m short of his mark - holds there"); _stuckT = 0f; Arrived = true; Current = Mode.Hold; }
                    break;
                case Mode.Hold:
                    PlayClip(_holdClip, 0.3f); Face(_look);
                    break;
                case Mode.Lead:
                {
                    if (_leadIdx >= _lead.Count) { Arrived = true; PlayClip("sit", 0.3f); Face(Seven.position); break; }
                    float d7 = DistanceTo7;
                    if (d7 > CatchUpDistance + 3f && TryCatchUp()) break;
                    if (d7 > _waitFor7) { PlayClip("idle", 0.3f); Face(Seven.position); break; }   // wait for her
                    if (MoveTo(_lead[_leadIdx], d7 < 2.5f ? TrotSpeed : StrollSpeed, 0.25f)) _leadIdx++;
                    break;
                }
                case Mode.Hop:
                {
                    _hopT += Time.deltaTime / _hopDur;
                    float u = Mathf.InverseLerp(0.3f, 0.85f, _hopT);
                    var p = Vector3.Lerp(_hopFrom, _hopTo, Mathf.SmoothStep(0f, 1f, u));
                    p.y += Mathf.Sin(u * Mathf.PI) * _hopH;
                    transform.position = p;
                    var face = Flat(_hopTo - _hopFrom);
                    if (face.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face), 400f * Time.deltaTime);
                    if (_hopT >= 1f) { transform.position = _hopTo; _perchY = _hopTo.y; Current = Mode.Perched; Arrived = true; PlayClip("idle", 0.2f); }
                    break;
                }
                case Mode.Perched:
                    if (_clip != "wash") PlayClip("idle", 0.3f);
                    break;
                case Mode.Route:
                    UpdateRoute();
                    break;
            }
            if (Current != Mode.Hop && Current != Mode.Perched && Current != Mode.Scripted && Current != Mode.Route)
            {
                var p = OutOfNoCat(transform.position); p.y = Ground.Height(p.x, p.z); transform.position = p;
            }
        }

        void UpdateRoute()
        {
            if (_route == null || _ri >= _route.Count)
            {
                Arrived = true;
                PlayClip(Time.time % 9f > 5f ? "sit" : "idle", 0.3f); Face(Seven.position);
                return;
            }
            var s = _route[_ri];
            if (!_rMoving)
            {
                // QF5: a stop also goes once 7 is already level with it or past it along the route (Dan hurried over the
                // bulkhead and jumped down before the cat got up there: "7 is on the bulkhead" never came true again)
                bool passed = QF5Fix && ArcOf(Seven.position) >= _rArc[_ri + 1] - 0.6f;
                if (s.Go != null && !s.Go() && !passed) { PlayClip(_clip == "walk" || _clip == "jump" ? "idle" : Time.time % 9f > 5f ? "sit" : _clip ?? "idle", 0.3f); Face(Seven.position); return; }
                _rMoving = true; _rHop = s.Hop;
                if (s.Hop)
                {
                    _hopFrom = transform.position; _hopTo = s.P; _hopT = 0f;
                    float d = Flat(s.P - transform.position).magnitude + Mathf.Abs(s.P.y - transform.position.y) * 0.5f;
                    _hopDur = Mathf.Clamp(0.45f + d * 0.16f, 0.6f, 1.25f);
                    _hopH = Mathf.Max(s.H, s.P.y - transform.position.y + 0.35f);
                    PlayClip("jump", 0.1f, 0.8f / _hopDur);
                }
            }
            if (_rHop)
            {
                _hopT += Time.deltaTime / _hopDur;
                float u = Mathf.InverseLerp(0.3f, 0.85f, _hopT);
                var p = Vector3.Lerp(_hopFrom, _hopTo, Mathf.SmoothStep(0f, 1f, u));
                // arc over the higher end so he never clips a lip
                float baseY = Mathf.Lerp(_hopFrom.y, _hopTo.y, u);
                p.y = baseY + Mathf.Sin(u * Mathf.PI) * _hopH;
                transform.position = p;
                var face = Flat(_hopTo - _hopFrom);
                if (face.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face), 400f * Time.deltaTime);
                if (_hopT >= 1f) { transform.position = _hopTo; PlayClip("idle", 0.2f); ReachStop(); }
            }
            else if (MoveTo(s.P, DistanceTo7 < 2.5f ? TrotSpeed : StrollSpeed, 0.2f)) ReachStop();
        }

        void ReachStop()
        {
            var at = _route[_ri].P;
            _ri++; _rMoving = false; _rHop = false;
            StopReached?.Invoke(_ri - 1, at);
        }

        // ------------------------------------------------------------------------------------------ QF5 leash
        /// <summary>Arc length along the current route of the point on it nearest to <paramref name="p"/>, looking only
        /// at the segments around the stop he is on (so a route that doubles back cannot make 7 "jump ahead");
        /// -1 if p is more than 4 m from that part of the route.</summary>
        public float ArcOf(Vector3 p)
        {
            if (_route == null || _rPts.Count < 2) return -1f;
            int k0 = Mathf.Max(0, _ri - 1), k1 = Mathf.Min(_rPts.Count - 2, _ri + 3);
            float best = float.MaxValue, arc = -1f;
            for (int k = k0; k <= k1; k++)
            {
                var a = _rPts[k]; var b = _rPts[k + 1]; var ab = b - a;
                float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
                float d = (a + ab * t - p).sqrMagnitude;
                if (d < best) { best = d; arc = _rArc[k] + ab.magnitude * t; }
            }
            return best < 16f ? arc : -1f;
        }

        /// <summary>Is he behind her (she has gone on and left him)? On a route / lead he is meant to be ahead and wait
        /// for her, so there only "she is further along than he is" counts; everywhere else being far away is enough.</summary>
        bool Behind()
        {
            if (Current == Mode.Route && _route != null && _ri < _route.Count)
            {
                float s7 = ArcOf(Seven.position), sc = ArcOf(transform.position);
                if (s7 < 0f) return true;                 // she has left the route altogether
                return sc < 0f || s7 > sc + 0.75f;
            }
            if (Current == Mode.Lead && _lead != null && _leadIdx < _lead.Count) return false;
            return true;
        }

        /// <summary>QF5: returns true while the leash has taken over this frame (chasing her / reappearing).
        /// Rule (Dan, QF5): more than ~6 m from 7, or not moving toward her for ~3 s while she moves on -> he trots after
        /// her; if that is blocked he fades out and reappears a step behind her, out of the camera's view. On a route he
        /// is allowed to wait up to 14 m ahead of her (that is the route working, not a soft-lock).</summary>
        bool Leash()
        {
            if (_blinking) return true;
            bool allowed = LeashAllowed != null && LeashAllowed() && Current != Mode.Scripted && Current != Mode.Hop && !(Current == Mode.Route && _rHop);
            var p7 = Seven.position;
            float d = Flat(p7 - transform.position).magnitude + Mathf.Max(0f, Mathf.Abs(p7.y - transform.position.y) - 0.6f);
            // samples every 0.25 s for the stall rule
            _moved7 += Flat(p7 - _last7).magnitude; _last7 = p7;
            if (Time.time >= _sampleT)
            {
                _sampleT = Time.time + 0.25f;
                _samples.Enqueue((Time.time, d, p7));
                while (_samples.Count > 0 && Time.time - _samples.Peek().t > StallSeconds + 0.3f) _samples.Dequeue();
            }
            if (!allowed) { if (_chase) EndChase("cutscene"); _samples.Clear(); _moved7 = 0f; return false; }
            if (!_chase)
            {
                bool behind = Behind();
                bool far = behind ? d > LeashDistance : d > AheadDistance;
                bool stall = false;
                if (behind && _samples.Count > 0 && Time.time - _samples.Peek().t >= StallSeconds - 0.05f)
                {
                    var old = _samples.Peek();
                    float sevenMoved = Flat(p7 - old.p7).magnitude;
                    // she moved on (> 1.2 m) and he is no closer than 3 s ago, while not already beside her
                    // QF8: in plain Follow mode he must also come when she stands still (he was left standing far off,
                    // stuck behind a column, while she waited at the shape)
                    stall = (sevenMoved > 1.2f || Current == Mode.Follow) && d > FollowDistance + 1.2f && d >= old.d - 0.2f
                            && (Current != Mode.Follow || d > FollowDistance + 1.5f);
                }
                if (!far && !stall) return false;
                _chase = true; _chaseFrom = Current; _chaseBest = d; _chaseProgT = Time.time; _chaseT0 = Time.time;
                Leashes++;
                LastLeash = $"{(far ? (behind ? "more than " + LeashDistance + " m behind" : "more than " + AheadDistance + " m ahead") : "not closing in for " + StallSeconds + " s while she moves on")} ({d:0.0} m, mode {Current})";
                Debug.Log($"[Seven] cat leash #{Leashes}: {LastLeash} at {transform.position}, 7 at {p7}");
                // nobody sees him: no need to trot the whole way - reappear behind her at once
                if (Cam != null && !Seen(transform.position) && SevenGrounded != null && SevenGrounded()) { StartCoroutine(Blink(false)); return true; }
            }
            // chase: trot / sprint after her
            if (d < FollowDistance + 0.5f) { LeashTrots++; EndChase("trotted up to her"); return false; }
            if (d < _chaseBest - 0.3f) { _chaseBest = d; _chaseProgT = Time.time; }
            bool blocked = Time.time - _chaseProgT > BlockedSeconds;
            if (blocked && SevenGrounded != null && SevenGrounded()) { StartCoroutine(Blink(true)); return true; }
            var behindHer = p7 - Flat(Seven.forward).normalized * 0.9f;
            var y0 = transform.position.y; var before = transform.position;
            MoveTo(behindHer, d > 4f ? SprintSpeed : TrotSpeed, 0.2f);
            // never walk off a ledge or up a wall while chasing (that counts as blocked: he reappears instead)
            if (Mathf.Abs(transform.position.y - y0) > 0.4f) { transform.position = before; _chaseProgT = Mathf.Min(_chaseProgT, Time.time - BlockedSeconds); }
            return true;
        }

        /// <summary>A new command from the script always wins over a catch-up in progress.</summary>
        void CancelLeash()
        {
            if (_blinking) { StopAllCoroutines(); _blinking = false; SetFade(1f); }
            _chase = false; _samples.Clear();
        }

        void ResumeRoute(string why)
        {
            _routePaused = false;
            var p7 = Seven.position;
            while (_ri < _route.Count) { float s7 = ArcOf(p7); if (s7 < 0f || s7 < _rArc[_ri + 1] - 0.3f) break; _ri++; }
            _rMoving = false; _rHop = false;
            Current = _ri < _route.Count ? Mode.Route : Mode.Follow;
            Debug.Log($"[Seven] cat resumes the route ({why}) at stop {_ri + 1}");
        }

        void EndChase(string why)
        {
            _chase = false; _samples.Clear();
            var p7 = Seven.position;
            if (_chaseFrom == Mode.Route && _route != null)
            {
                // go on with the route from the first stop still ahead of her
                _ri = Mathf.Clamp(_ri, 0, _route.Count);
                while (_ri < _route.Count)
                {
                    float s7 = ArcOf(p7);
                    if (s7 < 0f || s7 < _rArc[_ri + 1] - 0.3f) break;
                    _ri++;
                }
                _rMoving = false; _rHop = false;
                bool offRoute = ArcOf(p7) < 0f;
                Current = _ri < _route.Count && !offRoute ? Mode.Route : Mode.Follow;
                _routePaused = _ri < _route.Count && offRoute;      // she wandered off the route: follow her, lead again later
            }
            else if (_chaseFrom == Mode.Lead && _lead != null && _leadIdx < _lead.Count) Current = Mode.Lead;
            else Current = Mode.Follow;
            _perchY = float.NaN;
            if (Current == Mode.Follow) Arrived = false;
            Debug.Log($"[Seven] cat leash done ({why}) after {Time.time - _chaseT0:0.0}s: now {Current}{(Current == Mode.Route ? " stop " + (_ri + 1) : "")}, {DistanceTo7:0.0} m from 7");
        }

        /// <summary>Fade out (only if the camera can see him), reappear a step behind her where the camera cannot see,
        /// fade in only if the new spot is in view anyway (no hidden spot found).</summary>
        IEnumerator Blink(bool fadeOut)
        {
            _blinking = true;
            bool seen = Cam != null && Seen(transform.position);
            if (fadeOut && seen) for (float t = 0f; t < 0.35f; t += Time.deltaTime) { SetFade(1f - t / 0.35f); yield return null; }
            SetFade(seen ? 0f : 1f);
            var spot = BehindHerSpot(out bool hidden);
            var face = Flat(Seven.position - spot);
            transform.position = spot;
            if (face.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(face);
            PlayClip("walk", 0.1f, 1.6f);
            LeashBlinks++; CatchUps++;
            Debug.Log($"[Seven] cat reappeared a step behind 7 at {spot} ({(hidden ? "out of the camera's view" : "in view - faded in")})");
            if (!hidden || seen) { SetFade(0f); for (float t = 0f; t < 0.35f; t += Time.deltaTime) { SetFade(t / 0.35f); yield return null; } }
            SetFade(1f);
            _blinking = false;
            EndChase("reappeared behind her");
        }

        Vector3 BehindHerSpot(out bool hidden)
        {
            var p7 = Seven.position;
            var back = -Flat(Seven.forward).normalized; if (back.sqrMagnitude < 0.5f) back = Vector3.back;
            Vector3 bestVisible = Vector3.zero; bool haveVisible = false;
            foreach (var r in new[] { 1.0f, 1.5f, 2.2f })
                for (int i = 0; i < 12; i++)
                {
                    float a = (i % 2 == 0 ? 1 : -1) * ((i + 1) / 2) * 30f;          // 0, +30, -30, +60 ... behind her first
                    var c = p7 + Quaternion.Euler(0f, a, 0f) * back * r;
                    var q = Ground.ClampToArena(c, BodyRadius);
                    if (Flat(q - c).magnitude > 0.25f) continue;                      // not on walkable ground
                    q.y = Ground.Height(q.x, q.z);
                    if (Mathf.Abs(q.y - p7.y) > 0.3f) continue;                       // a different level (below a ledge...)
                    if (InNoCat(q, BodyRadius)) continue;
                    if (Physics.Linecast(p7 + Vector3.up * 0.3f, q + Vector3.up * 0.3f, out _, ~0, QueryTriggerInteraction.Ignore)) continue;   // through a wall
                    if (Cam == null || !Seen(q)) { hidden = true; return q; }
                    if (!haveVisible) { bestVisible = q; haveVisible = true; }
                }
            hidden = false;
            if (haveVisible) return bestVisible;
            var f = Ground.ClampToArena(p7 + back * 0.8f, BodyRadius); f.y = Ground.Height(f.x, f.z);
            return OutOfNoCat(f);
        }

        /// <summary>In the camera's frame and not hidden behind a wall.</summary>
        bool Seen(Vector3 p)
        {
            if (!Visible(p)) return false;
            var c = Cam.transform.position; var t = p + Vector3.up * 0.15f;
            return !Physics.Linecast(c, t, out _, ~0, QueryTriggerInteraction.Ignore);
        }

        void SetFade(float a)
        {
            if (_rends == null)
            {
                _rends = GetComponentsInChildren<Renderer>(true); _mpb = new MaterialPropertyBlock();
                _shadow = System.Array.ConvertAll(_rends, r => r.shadowCastingMode);
            }
            for (int i = 0; i < _rends.Length; i++)
            {
                var r = _rends[i];
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat("_Fade", a);
                r.SetPropertyBlock(_mpb);
                r.shadowCastingMode = a > 0.5f ? _shadow[i] : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Fade = a;
        }

        /// <summary>Reappear next to 7 at a spot the camera does not see (never in view, never pops on screen).</summary>
        bool TryCatchUp()
        {
            if (Cam != null && Visible(transform.position)) return false;
            var back = -Seven.forward;
            for (int i = 0; i < 12; i++)
            {
                var dir = Quaternion.Euler(0f, i * 30f, 0f) * back;
                var c = Ground.ClampToArena(Seven.position + dir * 2.2f, BodyRadius);
                if ((Flat(c - Seven.position)).magnitude < 1.2f) continue;
                if (InNoCat(c, BodyRadius)) continue;
                c.y = Ground.Height(c.x, c.z);
                if (Cam != null && Visible(c)) continue;
                transform.position = c;
                transform.rotation = Quaternion.LookRotation(Flat(Seven.position - c).sqrMagnitude > 1e-4f ? Flat(Seven.position - c) : Vector3.forward);
                CatchUps++;
                Debug.Log($"[Seven] cat caught up out of sight (#{CatchUps}) at {c}");
                return true;
            }
            return false;
        }

        bool Visible(Vector3 p)
        {
            var v = Cam.WorldToViewportPoint(p + Vector3.up * 0.15f);
            return v.z > 0f && v.x > -0.05f && v.x < 1.05f && v.y > -0.05f && v.y < 1.05f;
        }
    }
}
