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

        public void Follow() { Current = Mode.Follow; _perchY = float.NaN; Arrived = false; }
        public void GoTo(Vector3 p, float speed = -1f, Vector3? lookAt = null, string holdClip = "sit")
        {
            _target = p; _speed = speed > 0f ? speed : TrotSpeed; _look = lookAt ?? Seven.position; _holdClip = holdClip;
            Current = Mode.GoTo; Arrived = false; _perchY = float.NaN;
        }
        public void Lead(List<Vector3> path, float waitFor7 = 4.5f)
        {
            _lead = path; _leadIdx = 0; _waitFor7 = waitFor7; Current = Mode.Lead; Arrived = false; _perchY = float.NaN;
        }
        public void Hold(Vector3 lookAt, string clip = "sit") { _look = lookAt; _holdClip = clip; Current = Mode.Hold; }
        /// <summary>Round 9: lead a traversal route: go ahead to each stop, wait there until 7 catches up (never blocks her).</summary>
        public void Route(List<Stop> stops) { _route = stops; _ri = 0; _rMoving = false; _rHop = false; Current = Mode.Route; Arrived = false; _perchY = float.NaN; }
        public void Hop(Vector3 to, float duration = 0.8f, float height = 0.45f)
        {
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
        public void Scripted(string clip) { Current = Mode.Scripted; PlayClip(clip); }
        /// <summary>Round 7 tractor beam: held mid-air frame of the jump clip (legs hanging), drifting very slowly.</summary>
        public void Floating()
        {
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
                    if (Flat(q - p).magnitude > len + 0.05f) continue;
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
            PlayClip("walk", 0.2f, Mathf.Clamp(speed / StrideSpeed * 0.25f, 0.8f, 4f));
            return false;
        }

        void Face(Vector3 p)
        {
            var d = Flat(p - transform.position);
            if (d.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 300f * Time.deltaTime);
        }

        void Update()
        {
            switch (Current)
            {
                case Mode.Follow:
                {
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
                    if (MoveTo(_target, _speed)) { Arrived = true; Current = Mode.Hold; }
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
                if (s.Go != null && !s.Go()) { PlayClip(_clip == "walk" || _clip == "jump" ? "idle" : Time.time % 9f > 5f ? "sit" : _clip ?? "idle", 0.3f); Face(Seven.position); return; }
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
