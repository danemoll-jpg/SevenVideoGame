using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 10 galley: small arm / head moves layered on 7's seated clip with the Animator's IK pass (the Seven
    /// controller has it on): the right hand brings the fork to her mouth, either hand reaches the dodecahedron, her eyes /
    /// head follow a point. Targets are set by <see cref="Chapter1Aboard"/>; weights ease so nothing snaps.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class SevenTableIK : MonoBehaviour
    {
        public Vector3 RightPos, LeftPos, LookPos;
        public Quaternion RightRot = Quaternion.identity, LeftRot = Quaternion.identity;
        // QF23 (5): the seated lap arms. Elbow hints (a comfortable bend resting near her side) and, after the IK pass, the
        // palm laid flat on what it touches (PalmDown = the surface normal it rests on, FingerDir = where the fingers point,
        // Spread = fingers a little apart) - before QF23 only the wrist POSITION was set: the clip's wrist angle stayed and the
        // elbow went wherever the solver put it.
        public Vector3 LeftHint, RightHint; public float LeftHintW, RightHintW;
        public Vector3 LeftPalmN, RightPalmN, LeftFingerDir, RightFingerDir; public float LeftPalmW, RightPalmW, Spread = 1f;
        /// <summary>QF23 (22): lean in from the waist (degrees) toward LeanDir - applied in the IK pass before the hands are
        /// solved, so a hand on a target stays on it (a reach across a plinth).</summary>
        public float Lean; public Vector3 LeanDir; float _lean;
        // QF13 (15): every hand moment (bike grip in B01, petting, holding a shape, the table, the console) sets these
        // weights; a moment that ended without setting them back (a skipped line, a stopped coroutine, a never-ending stroke
        // loop) left her arm reaching for an old target. Each weight now remembers when it was last set, and a safety net
        // (OnAnimatorIK) blends a hand to zero within ~0.25 s when its target is out of natural reach, or when she walks and
        // the target does not move with her. ReleaseAll() is called at every stand-up, dismount, scene start and checkpoint.
        [Range(0f, 1f), SerializeField] float _rightW, _leftW, _lookW;
        public float RightW { get => _rightW; set { _rightW = value; } }
        public float LeftW { get => _leftW; set { _leftW = value; } }
        public float LookW { get => _lookW; set { _lookW = value; } }
        /// <summary>Check runs: how often the safety net let a hand go (stale / unreachable / walking away), and the last reason.</summary>
        public static int NetReleases; public static string LastNet;
        /// <summary>QF38 (1): releases made by the control-back guarantee (a scene that ended without releasing), and the last one.</summary>
        public static int AutoReleases; public static string LastAuto, LastRelease;
        public void ReleaseAll(string why)
        {
            OwnsArms = false;
            bool any = _rightW > 0.01f || _leftW > 0.01f || _lookW > 0.01f || _r > 0.01f || _l > 0.01f;
            any |= _lean > 0.5f || Lean > 0.5f || !Alive;
            _rightW = _leftW = _lookW = 0f; LeftHintW = RightHintW = LeftPalmW = RightPalmW = 0f; Lean = 0f; _leanAnchored = false;
            Alive = true;                               // QF38 (2): Venus's touch switched her head life off and never back on
            _freeFor = 0f;
            if (any) Debug.Log($"[Seven] hand IK released ({why})");
            LastRelease = why;
        }
        [Tooltip("How fast the weights ease (per second).")]
        public float Ease = 3f;
        float _r, _l, _look;
        Animator _anim;
        /// <summary>QF7 (emotion without faces): small life on her head and eyes - the gaze shifts a little now and then
        /// around what she looks at, a slow head / neck drift, and while she speaks (<see cref="Talk"/>) the head moves with
        /// the speech (small nods and turns). The Tripo head has no eye bones or eyelids, so gaze = head + look-at weights.</summary>
        public bool Alive = true;
        [Range(0f, 1f)] public float Talk;
        public int GazeShifts { get; private set; }
        Vector3 _gaze, _gazeTo; float _nextGaze, _talk, _t;
        Transform _neckB, _headB;

        void Awake() { _anim = GetComponent<Animator>(); _rightW = _leftW = _lookW = 0f; }
        void OnDisable() { _r = _l = 0f; }
        /// <summary>QF38 (2): a scene that ends its touch by removing the IK (the Moon's pocket beat) has ended the touch too.</summary>
        void OnDestroy() { if (VisionTouch.Open != null) { VisionTouch.Open = null; VisionTouch.Finished++; } }

        float _armLen = -1f; Vector3 _lastMe, _lastL, _lastR; float _walkT; float _netL = 1f, _netR = 1f;
        /// <summary>QF13 (15) safety net: 1 = this hand may follow its target, 0 = let go (blends over ~0.25 s).</summary>
        float Net(ref float k, HumanBodyBones upper, Vector3 target, Vector3 last, float want, string hand)
        {
            bool ok = true; string why = null;
            // held arms (OwnsArms) have their own reach rule (SolveHeldArms / InReach, QF34b): the restored out-of-reach net half-let-go
            // of FIN_10's hand on his shoulder (palm 4.7 cm off his fur vs rc1's 0.2 cm, same -fincp 6 run)
            if (OwnsArms) { k = 1f; return k; }
            if (want > 0.01f)
            {
                var sh = _anim.GetBoneTransform(upper);
                // QF38 (1): this check's body had been swallowed into a comment since QF23 (22) - the out-of-reach net never let go
                if (sh != null && _armLen > 0f && (target - sh.position).magnitude > _armLen * (Lean > 0.1f ? 1.5f : 1.15f))   // QF23 (22): leaning in brings the shoulder closer
                { ok = false; why = $"target {(target - sh.position).magnitude:0.00} m from the shoulder (reach {_armLen:0.00})"; }
                if (ok && _walkT > 0.3f)
                {
                    float dt = Mathf.Max(1e-4f, Time.deltaTime);
                    var vMe = (transform.position - _lastMe) / dt; var vT = (target - last) / dt; vMe.y = 0f; vT.y = 0f;
                    if ((vT - vMe).magnitude > 0.45f) { ok = false; why = "she walks and the target stays behind"; }
                }
            }
            float before = k;
            k = Mathf.MoveTowards(k, ok ? 1f : 0f, Time.deltaTime * (ok ? 2f : 4f));
            if (before > 0.5f && k <= 0.5f) { NetReleases++; LastNet = $"{hand} hand: {why}"; Debug.Log($"[Seven] hand IK safety net let go of the {LastNet}"); }
            return k;
        }

        void OnAnimatorIK(int layer)
        {
            float dt = Time.deltaTime;
            if (_armLen < 0f)
            {
                var u = _anim.GetBoneTransform(HumanBodyBones.LeftUpperArm); var lo = _anim.GetBoneTransform(HumanBodyBones.LeftLowerArm); var h = _anim.GetBoneTransform(HumanBodyBones.LeftHand);
                _armLen = u != null && lo != null && h != null ? Vector3.Distance(u.position, lo.position) + Vector3.Distance(lo.position, h.position) + 0.08f : 0.7f;
                _lastMe = transform.position; _lastL = LeftPos; _lastR = RightPos;
            }
            { var mv = transform.position - _lastMe; mv.y = 0f; _walkT = mv.magnitude / Mathf.Max(1e-4f, dt) > 0.6f ? _walkT + dt : 0f; }
            float nl = Net(ref _netL, HumanBodyBones.LeftUpperArm, LeftPos, _lastL, _leftW, "left");
            float nr = Net(ref _netR, HumanBodyBones.RightUpperArm, RightPos, _lastR, _rightW, "right");
            _lastMe = transform.position; _lastL = LeftPos; _lastR = RightPos;
            _r = Mathf.MoveTowards(_r, RightW * nr, dt * Mathf.Max(Ease, nr < 1f ? 4f : 0f));
            _l = Mathf.MoveTowards(_l, LeftW * nl, dt * Mathf.Max(Ease, nl < 1f ? 4f : 0f));
            _look = Mathf.MoveTowards(_look, LookW, dt * Ease);
            // QF38 (1): the hands follow their targets with a speed / acceleration limit (a scene that moved a target in one frame
            // snapped the hand there: the jitter Dan saw); smoothed in her own frame so a cut that re-places her never drags them
            SmoothTarget(ref _rpL, ref _rpV, RightPos, _r, dt); SmoothTarget(ref _lpL, ref _lpV, LeftPos, _l, dt);
            _anim.SetIKPositionWeight(AvatarIKGoal.RightHand, _r);
            _anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            _anim.SetIKPosition(AvatarIKGoal.RightHand, RightNowPos);
            _anim.SetIKRotation(AvatarIKGoal.RightHand, RightRot);
            _anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, _l);
            _anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            _anim.SetIKPosition(AvatarIKGoal.LeftHand, LeftNowPos);
            _anim.SetIKRotation(AvatarIKGoal.LeftHand, LeftRot);
            _anim.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, LeftHintW * _l); _anim.SetIKHintPosition(AvatarIKHint.LeftElbow, LeftHint);
            _anim.SetIKHintPositionWeight(AvatarIKHint.RightElbow, RightHintW * _r); _anim.SetIKHintPosition(AvatarIKHint.RightElbow, RightHint);
            _anim.SetLookAtWeight(_look, 0.15f, 0.7f, 1f, 0.6f);
            _anim.SetLookAtPosition(LookPos + (Alive ? _gaze : Vector3.zero));
        }

        // ---- QF38 (1): target smoothing (speed + acceleration limited), in her root's frame
        Vector3 _rpL, _rpV, _lpL, _lpV;
        public const float MaxHandSpeed = 2.2f, HandSmooth = 0.07f;
        /// <summary>Times a target jumped further than a hand can move in one frame (it was eased there instead).</summary>
        public static int TargetJumps;
        void SmoothTarget(ref Vector3 cur, ref Vector3 vel, Vector3 world, float w, float dt)
        {
            var want = transform.InverseTransformPoint(world);
            if (w < 0.01f || dt <= 0f) { cur = want; vel = Vector3.zero; return; }   // weight ~0: nothing to see - follow exactly
            // FurPalm / HandsOnHim (OwnsArms: FIN_10, petting) already ease the palms onto the fur and correct the gap from the drawn
            // hand - a second lag in series made that correction overshoot (the whole-game run's FIN_10 hand reversing 3-8x in 0.6 s)
            if (OwnsArms) { cur = want; vel = Vector3.zero; return; }
            if ((want - cur).magnitude > MaxHandSpeed * Mathf.Max(dt, 0.02f) * 1.5f) TargetJumps++;
            cur = Vector3.SmoothDamp(cur, want, ref vel, HandSmooth, MaxHandSpeed, dt);
        }
        /// <summary>The (smoothed) IK targets applied now.</summary>
        public Vector3 RightNowPos => transform.TransformPoint(_rpL);
        public Vector3 LeftNowPos => transform.TransformPoint(_lpL);
        // ---- QF38 (2): the lean belongs to the reach it was made for
        bool _leanAnchored; Vector3 _leanAnchor;
        /// <summary>The lean applied now (degrees).</summary>
        public float LeanNow => _lean;
        float LeanTarget()
        {
            if (Lean <= 0.01f) { _leanAnchored = false; return 0f; }
            // arms held on a body (OwnsArms: FIN_10's hands on Panther as he is gathered / shrinks) - her targets move with him, so the
            // 'the hand moved on' rule below straightened her and the palms fell 3-5 cm short of his fur (rc2's first whole-game runs)
            if (OwnsArms) return Lean;
            if (RightW < 0.01f && LeftW < 0.01f) return 0f;                             // no hand on a target: no reason to lean
            var local = transform.InverseTransformPoint(RightPos);
            if (!_leanAnchored) { if (_r > 0.5f) { _leanAnchored = true; _leanAnchor = local; } return Lean; }
            return (local - _leanAnchor).magnitude > 0.3f ? 0f : Lean;                  // the hand moved on (held up, palm up, to a pocket): she straightens
        }
        // ---- QF38 (1): the guarantee - nothing of a scene's hands survives control coming back
        float _freeFor;
        ChapterSeven _cs; SaucerSeven _ss;
        /// <summary>The player has her (control, on foot / in a fight, not locked, not frozen aboard).</summary>
        public bool PlayerFree()
        {
            bool control = false; try { control = MusicSystem.HasControl != null && MusicSystem.HasControl(); } catch { }
            if (!control || QF30Watch.HoldAllowed) return false;
            if (_cs == null) _cs = GetComponent<ChapterSeven>();
            if (_cs != null) return _cs.Current == ChapterSeven.Mode.Free || _cs.Current == ChapterSeven.Mode.Fight;
            if (_ss == null) _ss = GetComponent<SaucerSeven>();
            if (_ss != null) return _ss.Current == SaucerSeven.Mode.Free && !_ss.Frozen && !_ss.Cutscene;
            return false;
        }
        bool AnythingHeld => _rightW > 0.01f || _leftW > 0.01f || _lookW > 0.01f || Lean > 0.01f || _lean > 0.5f || !Alive || OwnsArms;
        void ControlGuard()
        {
            if (PlayerFree() && AnythingHeld) _freeFor += Time.deltaTime; else _freeFor = 0f;
            if (_freeFor > 1.0f)
            {
                string what = $"weights R {_rightW:0.00} L {_leftW:0.00} look {_lookW:0.00}, lean {Mathf.Max(Lean, _lean):0} deg{(!Alive ? ", head life off" : "")}";
                AutoReleases++; LastAuto = $"{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}: {what} (last release before it: {LastRelease ?? "-"})";
                ReleaseAll($"control came back with the hands still held (QF38 1): {what}");
                if (DemoBoot.CaptureDir != null && ChapterCapture.I != null) ChapterCapture.I.Log($"HAND AUDIT (QF38 1): control-back release - {LastAuto}");
            }
        }

        float _pl, _pr;
        /// <summary>The last palm-side direction measured (the proof: palm vs the surface it lies on).</summary>
        public Vector3 PalmSide { get; private set; }
        /// <summary>QF23 (5): after the animator (IK applied): lay the palm on the surface - the hand turns so its palm faces
        /// along -n (onto the surface) and the fingers point along fingerDir; the fingers spread a little.</summary>
        void PalmTo(bool right, float w, Vector3 n, Vector3 fingerDir)
        {
            if (w <= 0.001f || n.sqrMagnitude < 0.01f) return;
            var hand = _anim.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            var mid = _anim.GetBoneTransform(right ? HumanBodyBones.RightMiddleProximal : HumanBodyBones.LeftMiddleProximal);
            var idx = _anim.GetBoneTransform(right ? HumanBodyBones.RightIndexProximal : HumanBodyBones.LeftIndexProximal);
            var lit = _anim.GetBoneTransform(right ? HumanBodyBones.RightLittleProximal : HumanBodyBones.LeftLittleProximal);
            if (hand == null || mid == null || idx == null || lit == null) return;
            var f = (mid.position - hand.position).normalized; var across = (idx.position - lit.position).normalized;
            var palm = Vector3.Cross(f, across).normalized * (right ? -1f : 1f);      // the palm side (toward what it holds)
            // self-calibrating: a relaxed hand's fingers curl toward the palm - use the middle finger's curl when it is clear
            var midI = _anim.GetBoneTransform(right ? HumanBodyBones.RightMiddleIntermediate : HumanBodyBones.LeftMiddleIntermediate);
            if (midI != null) { var curl = Vector3.ProjectOnPlane(midI.position - mid.position, f); if (curl.magnitude > 0.004f && Vector3.Dot(curl, palm) < 0f) palm = -palm; }
            PalmSide = palm;
            var wantF = Vector3.ProjectOnPlane(fingerDir.sqrMagnitude > 0.01f ? fingerDir : f, n).normalized;
            if (wantF.sqrMagnitude < 0.01f) return;
            var from = Quaternion.LookRotation(f, -palm); var to = Quaternion.LookRotation(wantF, n);
            hand.rotation = Quaternion.Slerp(Quaternion.identity, to * Quaternion.Inverse(from), w) * hand.rotation;
            // the fingers a little apart (relaxed, not a paddle)
            var spread = new (HumanBodyBones b, float deg)[] { (right ? HumanBodyBones.RightIndexProximal : HumanBodyBones.LeftIndexProximal, -7f), (right ? HumanBodyBones.RightRingProximal : HumanBodyBones.LeftRingProximal, 5f), (right ? HumanBodyBones.RightLittleProximal : HumanBodyBones.LeftLittleProximal, 10f) };
            foreach (var (b, deg) in spread) { var t = _anim.GetBoneTransform(b); if (t != null) t.rotation = Quaternion.AngleAxis(deg * Spread * w * (right ? -1f : 1f), n) * t.rotation; }
        }
        void LateUpdate()
        {
            _own = Mathf.MoveTowards(_own, OwnsArms ? 1f : 0f, Time.deltaTime / 0.8f);
            ControlGuard();
            _lean = Mathf.MoveTowards(_lean, LeanTarget(), Time.deltaTime * 30f);   // QF38 (2): eased here (it ran only in the IK pass), and only while its reach holds
            if (_anim != null && _lean > 0.1f && LeanDir.sqrMagnitude > 0.01f)
            {
                // QF23 (22): the lean, after the animator - the spine and chest bend toward the shape, then the right arm is
                // re-solved onto its target (two-bone) so the hand still lands where it was aimed
                var axis = Vector3.Cross(Vector3.up, new Vector3(LeanDir.x, 0f, LeanDir.z).normalized);
                foreach (var (b, k) in new[] { (HumanBodyBones.Spine, 0.55f), (HumanBodyBones.Chest, 0.45f) })
                { var t = _anim.GetBoneTransform(b); if (t != null) t.rotation = Quaternion.AngleAxis(_lean * k, axis) * t.rotation; }
                var u = _anim.GetBoneTransform(HumanBodyBones.RightUpperArm); var lo = _anim.GetBoneTransform(HumanBodyBones.RightLowerArm); var h = _anim.GetBoneTransform(HumanBodyBones.RightHand);
                if (u != null && lo != null && h != null && _r > 0.01f)
                {
                    float la = Vector3.Distance(u.position, lo.position), lb = Vector3.Distance(lo.position, h.position);
                    var toT = RightNowPos - u.position; float d = Mathf.Clamp(toT.magnitude, 0.01f, (la + lb) * 0.999f); var dir = toT.normalized;
                    var pole = RightHintW > 0.01f ? RightHint : u.position - Vector3.up * 0.3f;
                    var pn = Vector3.ProjectOnPlane(pole - u.position, dir).normalized; if (pn.sqrMagnitude < 1e-4f) pn = Vector3.down;
                    float cosA = Mathf.Clamp((la * la + d * d - lb * lb) / (2f * la * d), -1f, 1f), A = Mathf.Acos(cosA);
                    var elbow = u.position + dir * Mathf.Cos(A) * la + pn * Mathf.Sin(A) * la;
                    var r1 = Quaternion.FromToRotation(lo.position - u.position, elbow - u.position) * u.rotation; u.rotation = Quaternion.Slerp(u.rotation, r1, _r);
                    var r2 = Quaternion.FromToRotation(h.position - lo.position, u.position + dir * d - lo.position) * lo.rotation; lo.rotation = Quaternion.Slerp(lo.rotation, r2, _r);
                }
            }
            if (_anim != null)
            {
                _pl = Mathf.MoveTowards(_pl, LeftPalmW * _l, Time.deltaTime * 3f); _pr = Mathf.MoveTowards(_pr, RightPalmW * _r, Time.deltaTime * 3f);
                PalmTo(false, _pl, LeftPalmN, LeftFingerDir); PalmTo(true, _pr, RightPalmN, RightFingerDir);
            }
            if (!Alive || _anim == null) return;
            float dt = Time.deltaTime; _t += dt;
            if (_headB == null) { _headB = _anim.GetBoneTransform(HumanBodyBones.Head); _neckB = _anim.GetBoneTransform(HumanBodyBones.Neck); }
            if (_headB == null) return;
            // gaze shifts: a new small offset around the look target every 1.2 - 3.5 s, reached in ~0.15 s (a glance)
            if (_t > _nextGaze)
            {
                _gazeTo = new Vector3(Random.Range(-0.07f, 0.07f), Random.Range(-0.04f, 0.05f), Random.Range(-0.07f, 0.07f));
                _nextGaze = _t + Random.Range(1.2f, 3.5f); GazeShifts++;
            }
            _gaze = Vector3.MoveTowards(_gaze, _gazeTo, dt * 0.6f);
            _talk = Mathf.MoveTowards(_talk, Talk, dt * 3f);
            var up = transform.up; var right = transform.right;
            float yaw = 2.0f * Mathf.Sin(_t * 0.37f) + 1.1f * Mathf.Sin(_t * 0.83f + 1f);
            float pitch = 1.2f * Mathf.Sin(_t * 0.29f + 2f);
            float env = 0.55f + 0.45f * Mathf.Sin(_t * 2f * Mathf.PI * 0.31f);
            pitch += _talk * (2.6f * Mathf.Sin(_t * 2f * Mathf.PI * 1.7f) * env + 1.2f * Mathf.Sin(_t * 2f * Mathf.PI * 0.7f));
            yaw += _talk * 1.8f * Mathf.Sin(_t * 2f * Mathf.PI * 0.45f + 0.5f);
            if (_neckB != null) _neckB.rotation = Quaternion.AngleAxis(yaw * 0.4f, up) * Quaternion.AngleAxis(pitch * 0.4f, right) * _neckB.rotation;
            _headB.rotation = Quaternion.AngleAxis(yaw * 0.6f, up) * Quaternion.AngleAxis(pitch * 0.6f, right) * _headB.rotation;
        }

        /// <summary>QF34 (1): a posed body (SevenRestPose: the kneel, the floor sit) bends her spine AFTER this component's pass, which moved
        /// the shoulders and - worse - re-aimed both arms to the pose, so a held hand never reached its target (FIN_10: her open hand ~10 cm
        /// over Panther's head). The pose now leaves a held arm alone and calls this after its spine: each held arm is re-solved
        /// (two-bone, elbow toward its hint) onto its target and the palm laid on again - one solver in charge of the arm.</summary>
        public void SolveHeldArms()
        {
            if (_anim == null) return;
            // held arms (FurPalm's points on his fur, written this frame) aim at the CURRENT target, as in rc1 - the smoothed one is from
            // the last IK pass (a frame behind the fur): FIN_10's palms sat 3-5 cm off him with it
            if (ArmOwn(true) > 0.001f) TwoBone(true, OwnsArms ? RightPos : RightNowPos, RightHintW > 0.01f ? RightHint : (Vector3?)null, ArmOwn(true));
            if (ArmOwn(false) > 0.001f) TwoBone(false, OwnsArms ? LeftPos : LeftNowPos, LeftHintW > 0.01f ? LeftHint : (Vector3?)null, ArmOwn(false));
            PalmTo(false, _pl, LeftPalmN, LeftFingerDir); PalmTo(true, _pr, RightPalmN, RightFingerDir);
            HeldSolves++;
        }
        public static int HeldSolves;
        /// <summary>True while this hand is held by the IK (the rest pose then leaves that arm alone).</summary>
        public bool Holds(bool right) => ArmOwn(right) > 0.001f;
        /// <summary>How much this arm is the IK's (0..1): OwnsArms eased over 0.8 s, times the hand's weight - the hand-over from a pose's
        /// arms is never a one-frame snap.</summary>
        public float ArmOwn(bool right) => _own * Mathf.Clamp01(right ? _r : _l);
        float _own;
        /// <summary>QF34b: set by a scene whose hands must really land (FIN_10 once he is in her lap); off by default - the posed scenes
        /// aboard keep their pose's arms (FurPalm lays the palms), and ReleaseAll clears it.</summary>
        public bool OwnsArms;
        void TwoBone(bool right, Vector3 target, Vector3? hint, float w)
        {
            var u = _anim.GetBoneTransform(right ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm);
            var lo = _anim.GetBoneTransform(right ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm);
            var h = _anim.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            if (u == null || lo == null || h == null) return;
            float la = Vector3.Distance(u.position, lo.position), lb = Vector3.Distance(lo.position, h.position);
            var toT = target - u.position; float d = Mathf.Clamp(toT.magnitude, 0.01f, (la + lb) * 0.999f); var dir = toT.normalized;
            var pole = hint ?? lo.position;                                   // no hint: keep the elbow on the side it is on
            var pn = Vector3.ProjectOnPlane(pole - u.position, dir).normalized; if (pn.sqrMagnitude < 1e-4f) pn = Vector3.down;
            float cosA = Mathf.Clamp((la * la + d * d - lb * lb) / (2f * la * d), -1f, 1f), A = Mathf.Acos(cosA);
            var elbow = u.position + dir * Mathf.Cos(A) * la + pn * Mathf.Sin(A) * la;
            var r1 = Quaternion.FromToRotation(lo.position - u.position, elbow - u.position) * u.rotation; u.rotation = Quaternion.Slerp(u.rotation, r1, w);
            var r2 = Quaternion.FromToRotation(h.position - lo.position, u.position + dir * d - lo.position) * lo.rotation; lo.rotation = Quaternion.Slerp(lo.rotation, r2, w);
        }

        /// <summary>QF30 (3): the weights actually applied now (eased).</summary>
        public float RightNow => _r; public float LeftNow => _l;
        public bool Settled => Mathf.Abs(_r - RightW) < 0.01f && Mathf.Abs(_l - LeftW) < 0.01f;
    }
}
