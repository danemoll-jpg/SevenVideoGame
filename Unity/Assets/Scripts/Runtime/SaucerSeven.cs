using System;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// 7 on the saucer: walks the deck (camera-relative, same feel as the fight demo, no combat), and sits in her chair:
    /// walk to the spot in front of it -> turn to the window -> Stand To Sit -> Sitting Idle. Stand up: Sit To Stand -> idle.
    /// The Mixamo sit clips do not line up with each other (Stand To Sit ends with the hips 0.47 m behind the root and
    /// 9 cm lower than Sitting Idle), so while seated her hips are pinned to the seat point (in the chair's space, so
    /// she tilts with the chair), and every switch between clips keeps the hips where they are.
    /// </summary>
    public class SaucerSeven : FighterBase
    {
        public SaucerInput Input;
        public Transform CameraTransform;
        public Transform ChairPivot;        // seated 7 is parented here (the chair tilts during the reveal)
        public Transform SeatPoint;         // where her hips go when seated (child of ChairPivot)
        public float WalkSpeed = 1.6f;
        public float SitReach = 1.6f;       // prompt / A works within this distance of the spot in front of the chair
        /// <summary>Stand To Sit moves the hips this far back (m, measured from the clip: 0.473).</summary>
        public float SitBackOffset = 0.47f;

        public enum Mode { Free, ToChair, SittingDown, Seated, StandingUp }
        public Mode Current { get; private set; } = Mode.Free;
        public event Action SatDown, StoodUp, StartedSitting, StartedStanding;

        [NonSerialized] public bool ScriptDriven;
        [NonSerialized] public Vector2 ScriptMove;
        /// <summary>Round 5: stands still (idle) while the gallery TV is in use.</summary>
        [NonSerialized] public bool Frozen;

        Transform _hips;
        bool _anchor;                       // pin hips to a point (in anchor space) every LateUpdate
        Vector3 _anchorLocal;               // target in the parent's space
        float _anchorBlend, _anchorBlendSpeed;
        Vector3 _anchorFrom;
        float _seatedHipsHeight = 0.646f;   // Sitting Idle hips above the root (clip), refined at runtime

        protected override bool Grounded => Current != Mode.Seated && Current != Mode.SittingDown && transform.parent == null && !OnSofa;
        /// <summary>Round 8: sitting on the gallery sofa while browsing (GalleryTV moves her; no ground clamp, no walking).</summary>
        [NonSerialized] public bool OnSofa;
        public void PlayClip(string state, float fade, float speed = 1f) => Play(state, fade, speed);

        public Vector3 StandSpot => ChairPivot.position + ChairPivot.forward * SitBackOffset + Vector3.ProjectOnPlane(SeatPoint.position - ChairPivot.position, Vector3.up);
        public bool NearChair => Current == Mode.Free && Flat(transform.position - StandSpot).magnitude < SitReach;
        public bool IsSeated => Current == Mode.Seated;
        public Transform Hips => _hips;

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        protected override void Awake()
        {
            base.Awake();
            _hips = Bone(HumanBodyBones.Hips);
            Play("idle", 0f);
        }

        Vector2 MoveInput => ScriptDriven ? ScriptMove : (Input != null ? Input.Move.ReadValue<Vector2>() : Vector2.zero);

        public void RequestSit()
        {
            if (Current != Mode.Free) return;
            Current = Mode.ToChair;
            Ground.IgnoreObstacle = 0;     // obstacle 0 is the chair
            StartedSitting?.Invoke();
        }

        public void RequestStand()
        {
            if (Current != Mode.Seated) return;
            Current = Mode.StandingUp;
            StartedStanding?.Invoke();
            // Leave the chair's space (world pose kept), then play Sit To Stand from its seated start.
            transform.SetParent(null, true);
            var e = transform.eulerAngles; transform.rotation = Quaternion.Euler(0f, e.y, 0f);
            _anchor = false;
            HoldHipsDuring(0.35f);
            Play("sit_to_stand", 0.35f);
        }

        protected override void Update()
        {
            base.Update();
            switch (Current)
            {
                case Mode.Free: UpdateFree(); break;
                case Mode.ToChair: UpdateToChair(); break;
                case Mode.SittingDown:
                    // Hand over to Sitting Idle once the hips have come down to its seat height.
                    if (_hips.position.y - transform.position.y <= _seatedHipsHeight + 0.005f || Tn >= 0.97f)
                    {
                        transform.SetParent(ChairPivot, true);
                        Current = Mode.Seated;
                        Play("sitting_idle", 0.4f);
                        PinHips(SeatPoint.position, 0.4f);
                        SatDown?.Invoke();
                    }
                    break;
                case Mode.StandingUp:
                    if (Tn >= 0.95f)
                    {
                        HoldHipsDuring(0.25f);
                        Play("idle", 0.25f);
                        Current = Mode.Free;
                        StoodUp?.Invoke();
                    }
                    break;
            }
        }

        void UpdateFree()
        {
            if (OnSofa) return;
            if (Frozen) { if (State != "idle") Play("idle", 0.2f, 1f, false); return; }
            var m = MoveInput;
            if (m.sqrMagnitude > 0.04f)
            {
                var fwd = CameraTransform != null ? CameraTransform.forward : Vector3.forward;
                fwd.y = 0f; fwd.Normalize();
                var right = new Vector3(fwd.z, 0f, -fwd.x);
                var v = fwd * m.y + right * m.x;
                if (v.sqrMagnitude > 1f) v.Normalize();
                transform.position += v * WalkSpeed * Time.deltaTime;
                if (State != "walk") Play("walk", 0.15f, 1f, false);
                FaceTowards(transform.position + v, 540f);
            }
            else if (State != "idle") Play("idle", 0.2f, 1f, false);
            // After standing up she starts inside the chair's footprint: re-enable it once she has walked clear.
            if (Ground.IgnoreObstacle == 0 && Ground.Obstacles.Count > 0
                && (new Vector2(transform.position.x, transform.position.z) - Ground.Obstacles[0].C).magnitude > Ground.Obstacles[0].R + 0.3f)
                Ground.IgnoreObstacle = -1;
            if (!ScriptDriven && Input != null && !PauseMenu.IsOpen && Input.Sit.WasPressedThisFrame() && NearChair) RequestSit();
        }

        void UpdateToChair()
        {
            var to = Flat(StandSpot - transform.position);
            if (to.magnitude > 0.04f)
            {
                if (State != "walk") Play("walk", 0.15f, 1f, false);
                var step = Mathf.Min(to.magnitude, WalkSpeed * 0.75f * Time.deltaTime);
                // Straight to the chair; sidestep around furniture in the way.
                foreach (var turn in new[] { 0f, 45f, -45f, 80f, -80f, 120f, -120f })
                {
                    var dir = Quaternion.Euler(0f, turn + _detour, 0f) * to.normalized;
                    var q = Ground.ClampToArena(transform.position + dir * step);
                    if (Vector3.Dot(q - transform.position, dir) > step * 0.6f)
                    {
                        if (turn != 0f) _detour = Mathf.Clamp(_detour + turn * 0.02f, -90f, 90f); else _detour *= 0.95f;
                        transform.position = new Vector3(q.x, transform.position.y, q.z);
                        FaceTowards(transform.position + dir, 540f);
                        break;
                    }
                }
                return;
            }
            transform.position = new Vector3(StandSpot.x, transform.position.y, StandSpot.z);
            if (State != "idle") Play("idle", 0.2f, 1f, false);
            // Turn her back to the chair (face the window), then sit.
            var want = Quaternion.LookRotation(Flat(ChairPivot.forward));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 300f * Time.deltaTime);
            if (Quaternion.Angle(transform.rotation, want) < 1f)
            {
                transform.rotation = want;
                Current = Mode.SittingDown;
                Play("stand_to_sit", 0.2f);
            }
        }

        /// <summary>Pin the hips (horizontally, in the parent's space) to a world point, easing there over blend s.</summary>
        void PinHips(Vector3 world, float blend)
        {
            var space = transform.parent;
            _anchorLocal = space != null ? space.InverseTransformPoint(world) : world;
            _anchorFrom = space != null ? space.InverseTransformPoint(_hips.position) : _hips.position;
            _anchorBlend = 0f;
            _anchorBlendSpeed = blend > 0f ? 1f / blend : 1000f;
            _anchor = true;
            _holdUntil = -1f;
        }

        float _holdUntil = -1f, _detour;
        /// <summary>Keep the hips where they are now for a moment (hides the clip-to-clip root mismatch).</summary>
        void HoldHipsDuring(float seconds)
        {
            PinHips(_hips.position, 0f);
            _holdUntil = Time.time + seconds;
        }

        void LateUpdate()
        {
            if (!_anchor) return;
            if (_holdUntil > 0f && Time.time > _holdUntil) { _anchor = false; _holdUntil = -1f; return; }
            _anchorBlend = Mathf.Min(1f, _anchorBlend + Time.deltaTime * _anchorBlendSpeed);
            var space = transform.parent;
            var target = Vector3.Lerp(_anchorFrom, _anchorLocal, Mathf.SmoothStep(0f, 1f, _anchorBlend));
            var hips = space != null ? space.InverseTransformPoint(_hips.position) : _hips.position;
            var d = target - hips;
            d.y = 0f;                                                   // height comes from the clip (+ chair)
            if (space != null) transform.localPosition += d;
            else transform.position += d;
        }

        /// <summary>Top of her lap: between the middles of both thighs, lifted by about a thigh radius.</summary>
        public Vector3 LapPoint(float lift = 0.07f)
        {
            // 60 % of the way from hip to knee: on top of her thighs, in front of her belly.
            Vector3 Mid(HumanBodyBones a, HumanBodyBones b) => Vector3.Lerp(Bone(a).position, Bone(b).position, 0.6f);
            var l = Mid(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg);
            var r = Mid(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg);
            var up = ChairPivot != null ? ChairPivot.up : Vector3.up;
            return (l + r) * 0.5f + up * lift;
        }

        /// <summary>Seat geometry check: hips height above the root in the current pose.</summary>
        public float HipsAboveRoot => _hips.position.y - transform.position.y;
    }
}
