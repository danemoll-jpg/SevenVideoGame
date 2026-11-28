using UnityEngine;

namespace Seven
{
    /// <summary>
    /// 7 in Chapter 1: camera-relative walking (hold LB / Shift to run), a slowed "sneak" walk where the level asks for
    /// it (substitute for a Mixamo Sneak Walking clip), and two held poses: touch (telekinesis clip held at the
    /// arm-forward frame, substitute for a touch / button-pushing clip) and the telekinesis gesture. No combat.
    /// Check runs drive her with a world-space direction (<see cref="ScriptMove"/>).
    /// </summary>
    public partial class ChapterSeven : FighterBase
    {
        public ChapterInput Input;
        public Transform CameraTransform;
        // QF43 (23): a faster on-foot pace (Dan) - walk 1.7 -> 2.0 m/s, run 3.9 -> 4.6 m/s (+18 %); the walk / run clips play
        // faster by the same factor (Pace) so the stride still matches the ground speed (no foot sliding)
        public float WalkSpeed = 2.0f, RunSpeed = 4.6f, SneakSpeed = 1.25f;
        public const float OldWalk = 1.7f, OldRun = 3.9f;
        public float Pace(string clip) => clip == "run" ? RunSpeed / OldRun : clip == "walk" ? WalkSpeed / OldWalk : 1f;
        [Tooltip("Telekinesis clip frame held for the 'touch' pose (arm forward, open hand).")]
        public float TouchFrame = 0.45f;

        public enum Mode { Free, Locked, Touch, Gesture, Look, Float, Air, Hang, Climb, Land, Fight, Ride }
        public Mode Current { get; private set; } = Mode.Free;
        /// <summary>QF30: the 7 in the active scene (the check-run watchers read her).</summary>
        public static ChapterSeven Live { get { if (_live == null || !_live.isActiveAndEnabled) _live = FindFirstObjectByType<ChapterSeven>(); return _live; } }
        static ChapterSeven _live;
        public Animator AnimNow => Anim;
        [System.NonSerialized] public bool Sneak;
        [System.NonSerialized] public bool ScriptDriven;
        [System.NonSerialized] public Vector3 ScriptMove;      // world direction * 0..1 (check runs / cutscenes)
        [System.NonSerialized] public bool ScriptRun;
        public float Speed { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Play("idle", 0f);
            FindEnergyPoint();
        }

        // ---------------------------------------------------------------- Round 7: energy point (forehead dot)
        Material _dot; float _dotLevel = 1f, _dotTarget = 1f, _dotRate = 1f;
        void FindEnergyPoint()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.materials;
                foreach (var m in mats) if (m != null && m.name.StartsWith("7_forehead_glow")) { _dot = m; return; }
            }
        }
        /// <summary>0 = dark (dampened by the laser field), 1 = normal glow. Fades over the given time.</summary>
        /// <summary>QF18 (10): collision on foot against walls / props (the zones still decide where the floor is).</summary>
        public static bool FootCollide = true;
        public static int FootSlides; float _footLogT;
        Transform[] _footIgnore; int _footIgnoreN = -1;
        Transform[] FootIgnore()
        {
            var ig = HoverBike.IgnoreRoots;
            if (_footIgnore == null || _footIgnoreN != (ig != null ? ig.Length : 0))
            {
                var l = new System.Collections.Generic.List<Transform> { transform }; if (ig != null) l.AddRange(ig);
                _footIgnore = l.ToArray(); _footIgnoreN = ig != null ? ig.Length : 0;
            }
            return _footIgnore;
        }
        public void SetEnergyPoint(float level, float seconds = 0f)
        {
            _dotTarget = level; _dotRate = seconds <= 0f ? 1e6f : Mathf.Abs(level - _dotLevel) / seconds;
            if (seconds <= 0f) { _dotLevel = level; ApplyDot(); }
        }
        public float EnergyPoint => _dotLevel;
        void ApplyDot()
        {
            if (_dot == null) return;
            // Dark: no emission and a dim base colour; lit: the usual soft white glow (emission 4).
            float shimmer = _dotLevel > 0.02f && _dotLevel < 0.99f ? 1f + 0.35f * Mathf.Sin(Time.time * 23f) * (1f - _dotLevel) : 1f;
            _dot.EnableKeyword("_EMISSION");
            _dot.SetColor("_EmissionColor", Color.white * 4f * _dotLevel * shimmer);
            _dot.SetColor("_BaseColor", Color.white * Mathf.Lerp(0.16f, 1f, _dotLevel));
        }

        // ---------------------------------------------------------------- Round 7 (3c): Dan's optional clips
        string WalkClip => Sneak && HasState("sneak_walk") ? "sneak_walk" : "walk";
        float _idleFor;
        Mode _beforeLook;
        /// <summary>Look around (the city reveal turn / idle variety). Uses 7_look_around when present.</summary>
        public void LookAround(float speed = 1f)
        {
            if (!HasState("look_around")) return;
            _beforeLook = Current == Mode.Look ? _beforeLook : Current;
            Current = Mode.Look;
            Play("look_around", 0.35f, speed);
        }

        /// <summary>Tractor beam: carried upward by a script; upright, arms slightly out (procedural layer).</summary>
        public void BeginFloat() { Current = Mode.Float; if (State != "idle") Play("idle", 0.4f); _floatT = 0f; }
        public void EndFloat() { Current = Mode.Locked; }
        // the beam carries her; Round 9: jumps, hangs, climbs and landings place her themselves
        protected override bool Grounded => Current != Mode.Ride && Current != Mode.Float && Current != Mode.Air && Current != Mode.Hang && Current != Mode.Climb && Current != Mode.Land && !(Current == Mode.Fight && Act == FightAct.Hop);
        protected override bool UseRootMotion => Current == Mode.Free || Current == Mode.Fight || Current == Mode.Locked || Current == Mode.Touch || Current == Mode.Gesture || Current == Mode.Look;
        float _floatT;

        SevenTableIK _bladeIk;
        void LateUpdate()
        {
            // QF38 (1): one owner per arm, the same frame - a ledge / climb / the bike, or a scripted hand (the table IK in a
            // cutscene) takes her hands: the arc blades are sheathed first (quietly for a script)
            if (BladesDrawn)
            {
                if (Current == Mode.Ride || Current == Mode.Hang || Current == Mode.Climb) DrawBlades(false);
                else { if (_bladeIk == null) _bladeIk = GetComponent<SevenTableIK>(); if (_bladeIk != null && _bladeIk.isActiveAndEnabled && Mathf.Max(_bladeIk.RightNow, _bladeIk.LeftNow) > 0.02f) DrawBlades(false, true); }
            }
            TraversalLateUpdate();
            if (_dotLevel != _dotTarget) { _dotLevel = Mathf.MoveTowards(_dotLevel, _dotTarget, _dotRate * Time.deltaTime); }
            ApplyDot();
            RideLateUpdate();                             // Round 11: leaning forward on the bike
            BladesLateUpdate();                           // Round 16: the arc blades (grow / flicker / hum)
            if (Current != Mode.Ride) WallClearance();    // QF8: keep her whole body out of walls
            if (Current != Mode.Float) return;
            _floatT += Time.deltaTime;
            float w = Mathf.Clamp01(_floatT / 1.2f);
            void Rot(HumanBodyBones b, Vector3 axis, float deg) { var t = Bone(b); if (t != null) t.localRotation *= Quaternion.AngleAxis(deg * w, axis); }
            // Arms drift out from the sides (abduct ~22 deg), forearms soften, a slow weightless sway.
            float s = Mathf.Sin(_floatT * 1.3f);
            var la = Bone(HumanBodyBones.LeftUpperArm); var ra = Bone(HumanBodyBones.RightUpperArm);
            if (la != null) la.rotation = Quaternion.AngleAxis(-(22f + 3f * s) * w, transform.forward) * la.rotation;
            if (ra != null) ra.rotation = Quaternion.AngleAxis((22f + 3f * s) * w, transform.forward) * ra.rotation;
            Rot(HumanBodyBones.LeftLowerArm, Vector3.forward, 8f);
            Rot(HumanBodyBones.RightLowerArm, Vector3.forward, -8f);
            var head = Bone(HumanBodyBones.Head); if (head != null) head.rotation = Quaternion.AngleAxis(-8f * w, transform.right) * head.rotation;   // looks up a little
            var hips = Bone(HumanBodyBones.Hips); if (hips != null) hips.rotation = Quaternion.AngleAxis(2f * s * w, transform.forward) * hips.rotation;
        }

        public void Lock(bool on)
        {
            if (on && Current == Mode.Free) { Current = Mode.Locked; Crouched = false; if (State != "idle") Play("idle", 0.25f, 1f, false); }
            if (!on && Current == Mode.Locked) Current = Mode.Free;
        }

        /// <summary>Reach out and hold a hand on a surface in front of her.</summary>
        public void Touch(Vector3 facePoint)
        {
            _face = facePoint; Current = Mode.Touch;
            // Round 7 (3c): Dan's 7_touch_wall clip (hand on the wall at ~frame 45 of 98) replaces the held telekinesis frame.
            if (HasState("touch_wall")) Play("touch_wall", 0.3f, 1f); else Play("telekinesis", 0.25f, 0.8f);
        }
        float TouchHold => State == "touch_wall" ? 0.46f : TouchFrame;

        /// <summary>Telekinesis gesture toward a point (plays the whole clip, then back to idle).</summary>
        public void Gesture(Vector3 facePoint)
        {
            _face = facePoint; Current = Mode.Gesture;
            Play("telekinesis", 0.15f, 1f);
        }

        public void Release()
        {
            if (Current == Mode.Touch || Current == Mode.Gesture || Current == Mode.Look || Current == Mode.Float) { Freeze(false); Play("idle", 0.35f); }
            Current = Mode.Free;
        }

        Vector3 _face;
        public bool GestureDone => Current == Mode.Gesture && Tn >= 0.8f;
        public bool GesturePeak => Current == Mode.Gesture && Tn >= 0.49f;

        /// <summary>Teleport (checkpoint reset).</summary>
        public void Place(Vector3 pos, Vector3 facing)
        {
            StopTraversal();
            Release();
            transform.position = new Vector3(pos.x, Ground.Height(pos.x, pos.z), pos.z);
            facing.y = 0f;
            if (facing.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(facing);
            Play("idle", 0f);
        }

        protected override void Update()
        {
            UndoWallPush();                               // QF8: last frame's rendered wall push is not gameplay
            Ground.BlockLow = !Crouched;
            // QF38 (1): one owner per arm - the arc blades sheathe themselves when her hands are needed elsewhere (a ledge, a climb,
            // the bike's grips); the whole-game run's hand audit caught her climbing and riding with them out (two drivers at once)
            if (BladesDrawn && (Current == Mode.Ride || Current == Mode.Hang || Current == Mode.Climb)) DrawBlades(false);
            base.Update();
            switch (Current)
            {
                case Mode.Free: UpdateFree(); break;
                case Mode.Air: UpdateAir(); break;
                case Mode.Hang: UpdateHang(); break;
                case Mode.Land: UpdateLand(); break;
                case Mode.Climb: Speed = 0f; Context = null; break;
                case Mode.Locked: Speed = 0f; if (State != "idle") Play("idle", 0.25f, 1f, false); break;
                case Mode.Touch:
                    Speed = 0f;
                    FaceTowards(_face, 240f);
                    if (Tn >= TouchHold) Freeze(true);
                    break;
                case Mode.Gesture:
                    Speed = 0f;
                    FaceTowards(_face, 360f);
                    if (Tn >= 0.95f) { Current = Mode.Free; Play("idle", 0.3f); }
                    break;
                case Mode.Look:
                    Speed = 0f;
                    if (Tn >= 0.96f) { Current = _beforeLook == Mode.Look ? Mode.Locked : _beforeLook; Play("idle", 0.4f); }
                    else if (_beforeLook == Mode.Free && !ScriptDriven && Input != null && Input.Move.ReadValue<Vector2>().sqrMagnitude > 0.04f) { Current = Mode.Free; }
                    break;
                case Mode.Float:
                    Speed = 0f;
                    break;
                case Mode.Fight: UpdateFight(); break;                 // QF9: the terrace fight
                case Mode.Ride: Speed = 0f; Context = null; break;     // Round 11: the hover bike carries her
            }
            if (Current != Mode.Free && Current != Mode.Hang) Context = null;
            Ground.BlockLow = false;
        }

        /// <summary>Camera-relative move direction (world, magnitude 0..1) from the stick / keys, or the script's.</summary>
        Vector3 MoveInput(out bool run)
        {
            Vector3 v;
            if (ScriptDriven) { v = ScriptMove; v.y = 0f; run = ScriptRun; }
            else
            {
                var m = Input != null ? Input.Move.ReadValue<Vector2>() : Vector2.zero;
                var fwd = CameraTransform != null ? CameraTransform.forward : Vector3.forward;
                fwd.y = 0f; fwd.Normalize();
                var right = new Vector3(fwd.z, 0f, -fwd.x);
                v = fwd * m.y + right * m.x;
                run = Input != null && Input.Run.IsPressed();
            }
            if (v.sqrMagnitude > 1f) v.Normalize();
            return v;
        }

        void UpdateFree()
        {
            BladeInput();                                 // Round 16: RB / G draws / sheathes the arc blades
            var v = MoveInput(out bool run);
            bool jump = PressedJump(), crouch = PressedCrouch();
            if (crouch)
            {
                var hd = FindHangDown();
                if (hd != null) { BeginTraversal(HangDownRoutine(hd)); return; }
                if (Crouched) { if (!LowOnly()) { Crouched = false; Did?.Invoke("stand"); } }
                else { Crouched = true; Did?.Invoke("crouch"); }
                Ground.BlockLow = !Crouched;
            }
            if (jump && !(Crouched && LowOnly()))
            {
                Crouched = false; Ground.BlockLow = true;
                if (TryJump(v, run)) return;
            }
            bool sneak = Sneak || Crouched;
            if (v.sqrMagnitude > 0.04f)
            {
                float spd = sneak ? SneakSpeed : run ? RunSpeed : WalkSpeed;
                Speed = spd * v.magnitude;
                // Sub-stepped (<= 15 cm per step, clamped each step): a frame hitch can never carry her through a wall
                // or the laser beams into the next walkable zone.
                var step = v * spd * Time.deltaTime;
                int n = Mathf.Max(1, Mathf.CeilToInt(step.magnitude / 0.15f));
                for (int i = 0; i < n; i++)
                {
                    var before = transform.position;
                    transform.position += step / n; ClampToArena();
                    // QF18 (10): every solid surface stops her on foot too (she slides along it) - not only the zone edges
                    if (FootCollide && !Crouched && CameraOccluders.Ready)
                    {
                        var q = RideProbe.SlideLand(before, transform.position, 1.0f, 0.26f, FootIgnore(), out bool wall);
                        if (wall && DemoBoot.CaptureDir != null && Time.time > _footLogT && new Vector2(q.x - before.x, q.z - before.z).magnitude < 0.3f * new Vector2(transform.position.x - before.x, transform.position.z - before.z).magnitude) { _footLogT = Time.time + 2f; Debug.Log($"[Seven] foot collision (QF18 10): held by '{RideProbe.LastLandHit}' at {before}"); }
                        if (wall) { FootSlides++; transform.position = new Vector3(q.x, transform.position.y, q.z); ClampToArena(); }
                    }
                }
                string clip = run && !sneak ? "run" : sneak && HasState("sneak_walk") ? "sneak_walk" : "walk";
                float animSpeed = sneak && clip == "walk" ? 0.62f : Pace(clip);
                _idleFor = 0f;
                if (State != clip) Play(clip, 0.15f, animSpeed, false);
                FaceTowards(transform.position + v, 540f);
            }
            else
            {
                Speed = 0f;
                string idle = Crouched && HasState("crouch_idle") ? "crouch_idle" : "idle";
                if (State != idle) Play(idle, 0.2f, 1f, false);
                // Idle variety: after a while standing still she looks around once (7_look_around).
                _idleFor += Time.deltaTime;
                if (_idleFor > 11f && !ScriptDriven && !Crouched) { _idleFor = -6f; LookAround(); }
            }
            UpdateSafe();
            Context = FreeContext();
        }
    }
}
