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

        public enum Mode { Follow, GoTo, Hold, Lead, Hop, Perched, Scripted }
        public Mode Current { get; private set; } = Mode.Follow;
        public bool Arrived { get; private set; }
        public string Clip => _clip;
        public float DistanceTo7 => Flat(Seven.position - transform.position).magnitude;
        public int CatchUps { get; private set; }

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
        public void Hop(Vector3 to, float duration = 0.8f, float height = 0.45f)
        {
            _hopFrom = transform.position; _hopTo = to; _hopT = 0f; _hopDur = duration; _hopH = height;
            Current = Mode.Hop; Arrived = false;
            PlayClip("jump", 0.1f, 0.8f / duration);
        }
        public void Paw() { PlayClip("wash", 0.2f, 1.6f); }
        public void Place(Vector3 p, Vector3 facing)
        {
            transform.position = new Vector3(p.x, Ground.Height(p.x, p.z), p.z);
            facing.y = 0f; if (facing.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(facing);
            _perchY = float.NaN;
        }
        /// <summary>Scripted: an outside script moves him (liftoff boarding etc.).</summary>
        public void Scripted(string clip) { Current = Mode.Scripted; PlayClip(clip); }

        // ------------------------------------------------------------------------------------------ motion
        bool MoveTo(Vector3 target, float speed, float arrive = 0.12f)
        {
            var to = Flat(target - transform.position);
            if (to.magnitude < arrive) return true;
            float len = Mathf.Min(to.magnitude, speed * Time.deltaTime);
            Vector3 p = transform.position;
            foreach (var turn in new[] { 0f, 40f, -40f, 80f, -80f, 120f, -120f })
            {
                var dir = Quaternion.Euler(0f, turn + _detour, 0f) * to.normalized;
                var q = Ground.ClampToArena(transform.position + dir * len, BodyRadius);
                if (Vector3.Dot(Flat(q - transform.position), dir) > len * 0.6f)
                {
                    p = q;
                    if (turn != 0f) _detour = Mathf.Clamp(_detour + turn * 0.02f, -90f, 90f); else _detour *= 0.9f;
                    break;
                }
            }
            var moved = Flat(p - transform.position);
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
            }
            if (Current != Mode.Hop && Current != Mode.Perched && Current != Mode.Scripted)
            {
                var p = transform.position; p.y = Ground.Height(p.x, p.z); transform.position = p;
            }
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
