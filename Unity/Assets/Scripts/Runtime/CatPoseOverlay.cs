using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 10 galley: small procedural moves layered on the cat's clips (after the Animator, in LateUpdate):
    /// <list type="bullet">
    /// <item>Eat: chest, neck and head dip toward the bowl, with small chewing nods of the head - the only time his mouth
    /// "moves" (the bowl rim hides the muzzle). He never moves his mouth when he talks (telepathy).</item>
    /// <item>Lean: chest forward, neck stretched toward a point (studying the object).</item>
    /// <item>Tilt: head roll (studying one face, then another).</item>
    /// <item>Ear: one ear bone (earL / earR from cleanup_cat.py) turns toward a point and twitches.</item>
    /// </list>
    /// Every weight eases, so switching is smooth. Rotations are about the cat's own axes (bone axes don't matter).
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class CatPoseOverlay : MonoBehaviour
    {
        [Range(0f, 1f)] public float Eat, Lean, Tilt;
        public float TiltDeg = 16f;
        [Tooltip("Ear turn target (world); null = no ear turn.")]
        public Vector3? EarToward;
        public bool Chewing;
        /// <summary>QF7: the bowl is a plain low bowl on the table - how much deeper the eating dip goes (found by
        /// Chapter1Aboard so the muzzle ends just over the food, inside the rim).</summary>
        public float EatGain = 1f;
        /// <summary>QF7: front crouch while eating from the low bowl (deg, spine pitched nose-down); the front paws stay
        /// planted where the clip put them (two-bone leg IK), so he crouches at the front like a cat at a floor bowl.</summary>
        public float Crouch;
        /// <summary>QF7: the planted front paws move this far back (m) while he eats, so they stay outside the bowl.</summary>
        public float PawBack;
        /// <summary>QF16 (4): his right front paw reaches for a point (batting at the display, pawing at the glass), weight 0..1.</summary>
        public Vector3? PawTo; public float PawW;
        /// <summary>QF16 (4): tail flick toward a point (across her face on her shoulder), 0..1; and a twitching tail tip (0..1).</summary>
        public Vector3? TailToward; public float TailFlick, TailTwitch;
        readonly System.Collections.Generic.List<Transform> _tail = new System.Collections.Generic.List<Transform>();
        float _paw, _tailF;
        public Vector3 PawL => _legF[0] != null ? _legF[0].position : transform.position;
        public Vector3 PawR => _legF[1] != null ? _legF[1].position : transform.position;
        /// <summary>QF7: head (and a little neck) turned toward this point - he lifts his head and looks at her when he
        /// speaks. Null = none.</summary>
        public Vector3? LookAt;
        /// <summary>QF7: small life - an ear flick now and then, a slow head drift.</summary>
        public bool Alive = true;
        public int EarFlicks { get; private set; }
        float _look, _flickT = -1f, _nextFlick = 3f; Transform _flickEar;
        /// <summary>Total time the chewing nods ran (checks: mouth movement only while eating).</summary>
        public float ChewSeconds { get; private set; }

        Transform _chest, _neck, _head, _earL, _earR, _spine;
        readonly Transform[] _legU = new Transform[4], _legL = new Transform[4], _legF = new Transform[4];   // FL, FR, HL, HR
        /// <summary>QF28 (4): riding a racer - crouched low in its padded bed (all four legs folded under her), head up, ears laid
        /// back, tail wrapped round her side. 0..1 (eases); <see cref="RideDrop"/> = how far her root sits below the seat point.</summary>
        public float Ride; float _ride;
        /// <summary>QF30 (14): the stillness after his last breath - ears slack (laid out and down), the tail limp along the floor, the
        /// head settled down into her hand (toward <see cref="SettleTo"/>), and a breathing rise of the flank (0..1 amplitude) while alive.</summary>
        [Range(0f, 1f)] public float Slack, Breath; public float BreathRate = 1.6f; public Vector3? SettleTo;
        float _slack, _breathT;
        public float RideDrop { get; private set; }
        public void RideNow() { _ride = Ride; }
        float _eat, _lean, _tilt, _ear, _t;
        public bool HasEars => _earL != null && _earR != null;
        /// <summary>The muzzle (approx.): the head bone pushed a little forward along the cat.</summary>
        public Vector3 Muzzle => _head != null ? _head.TransformPoint(_muzzleLocal) : transform.position;
        Vector3 _muzzleLocal;
        /// <summary>Eyes (approx.): between the muzzle and the head bone, a little up.</summary>
        public Vector3 Eyes => _head != null ? _head.TransformPoint(_eyesLocal) : transform.position + Vector3.up * 0.25f;
        Vector3 _eyesLocal;

        void Start()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                switch (t.name)
                {
                    case "chest": _chest = t; break;
                    case "spine": _spine = t; break;
                    case "FL_upper": _legU[0] = t; break;
                    case "FL_lower": _legL[0] = t; break;
                    case "FL_foot": _legF[0] = t; break;
                    case "FR_upper": _legU[1] = t; break;
                    case "FR_lower": _legL[1] = t; break;
                    case "FR_foot": _legF[1] = t; break;
                    case "HL_upper": _legU[2] = t; break;
                    case "HL_lower": _legL[2] = t; break;
                    case "HL_foot": _legF[2] = t; break;
                    case "HR_upper": _legU[3] = t; break;
                    case "HR_lower": _legL[3] = t; break;
                    case "HR_foot": _legF[3] = t; break;
                    case "neck": _neck = t; break;
                    case "head": _head = t; break;
                    case "earL": _earL = t; break;
                    case "earR": _earR = t; break;
                }
                if (t.name.StartsWith("tail")) _tail.Add(t);
            }
            // QF28 (4): the crouch drop - the lower of the shoulder / hip joints above her root, folded to ~40 %
            { float hj = float.MaxValue; for (int i = 0; i < 4; i++) if (_legU[i] != null) hj = Mathf.Min(hj, _legU[i].position.y - transform.position.y); RideDrop = hj < float.MaxValue ? Mathf.Max(0f, hj * 0.8f) : 0f; }
            // rest offsets (rig: muzzle 3.3 cm ahead / 2.7 cm below the head bone at 0.25 m shoulder height)
            if (_head != null)
            {
                float k = transform.localScale.x;
                _muzzleLocal = _head.InverseTransformPoint(_head.position + (transform.forward * 0.036f + Vector3.down * 0.027f) * k);
                _eyesLocal = _head.InverseTransformPoint(_head.position + (transform.forward * 0.022f + Vector3.up * 0.004f) * k);
            }
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime; _t += dt;
            _eat = Mathf.MoveTowards(_eat, Eat, dt * 1.6f);
            _lean = Mathf.MoveTowards(_lean, Lean, dt * 1.4f);
            _tilt = Mathf.MoveTowards(_tilt, Tilt, dt * 2.2f);
            _ear = Mathf.MoveTowards(_ear, EarToward.HasValue ? 1f : 0f, dt * 4f);
            _look = Mathf.MoveTowards(_look, LookAt.HasValue ? 1f : 0f, dt * 2.2f);
            var right = transform.right; var fwd = transform.forward;
            bool chew = Chewing && _eat > 0.6f;
            if (chew) ChewSeconds += dt;
            float nod = chew ? Mathf.Sin(_t * 2f * Mathf.PI * 2.6f) * 3.5f + Mathf.Sin(_t * 2f * Mathf.PI * 0.4f) * 2f : 0f;
            // + angle about the cat's right axis = nose down
            // QF7 front crouch: remember where the clip put the front paws, pitch the spine, then plant the paws again
            float crouch = Crouch * _eat;
            Vector3 a0 = Vector3.zero, a1 = Vector3.zero; Quaternion f0 = Quaternion.identity, f1 = Quaternion.identity;
            bool legs = crouch > 0.01f && _spine != null && _legU[0] != null && _legL[0] != null && _legF[0] != null && _legU[1] != null && _legL[1] != null && _legF[1] != null;
            if (legs) { a0 = _legF[0].position; a1 = _legF[1].position; f0 = _legF[0].rotation; f1 = _legF[1].rotation; }
            if (crouch > 0.01f) Rot(_spine, right, crouch);
            var back = -Vector3.ProjectOnPlane(fwd, Vector3.up).normalized * PawBack * _eat;
            if (legs) { PlantLeg(0, a0 + back, f0); PlantLeg(1, a1 + back, f1); }
            RidePose(dt, right, fwd);
            float g = Mathf.Lerp(1f, EatGain, _eat);
            Rot(_chest, right, 16f * _eat * g - 6f * _lean);
            Rot(_neck, right, 52f * _eat * g - 10f * _lean);
            Rot(_head, right, 34f * _eat * g + nod + 8f * _lean);
            if (Alive && _head != null && _eat < 0.5f)
            {
                // a slow drift of the head while he sits or listens
                Rot(_head, Vector3.up, (3f * Mathf.Sin(_t * 0.43f) + 1.5f * Mathf.Sin(_t * 1.1f + 2f)) * (1f - _eat));
            }
            if (_look > 0.001f && LookAt.HasValue && _head != null)
            {
                // turn the head (most) and the neck (some) toward the point: yaw about up, then pitch about his right axis
                var to = LookAt.Value - _head.position;
                var flat = new Vector3(to.x, 0f, to.z);
                float yaw = Mathf.Clamp(Vector3.SignedAngle(Vector3.ProjectOnPlane(fwd, Vector3.up), flat, Vector3.up), -70f, 70f);
                float pitch = Mathf.Clamp(Mathf.Atan2(to.y, flat.magnitude) * Mathf.Rad2Deg, -20f, 35f);
                Rot(_neck, Vector3.up, yaw * 0.35f * _look);
                Rot(_head, Vector3.up, yaw * 0.6f * _look);
                Rot(_head, right, -pitch * 0.6f * _look);
            }
            if (_head != null && _tilt > 0.001f) _head.rotation = Quaternion.AngleAxis(TiltDeg * _tilt, fwd) * _head.rotation;
            if (Alive && !EarToward.HasValue) FlickEar();
            // QF16 (4): the paw reaching out (a soft bat: it bobs a little) and the tail
            _paw = Mathf.MoveTowards(_paw, PawTo.HasValue ? PawW : 0f, dt * 3f);
            if (_paw > 0.01f && PawTo.HasValue && _legU[1] != null && _legL[1] != null && _legF[1] != null)
            {
                var foot = _legF[1].position; var tgt = PawTo.Value + Vector3.up * Mathf.Abs(Mathf.Sin(_t * 5f)) * 0.03f * transform.localScale.x;
                PlantLeg(1, Vector3.Lerp(foot, tgt, _paw), _legF[1].rotation);
            }
            _tailF = Mathf.MoveTowards(_tailF, TailToward.HasValue ? TailFlick : 0f, dt * 4f);
            for (int i = 0; i < _tail.Count; i++)
            {
                var tb = _tail[i]; if (tb == null) continue;
                float k = (i + 1f) / _tail.Count;
                if (_tailF > 0.01f && TailToward.HasValue)
                {
                    var want = TailToward.Value - tb.position; var cur = tb.up;
                    var q = Quaternion.FromToRotation(cur, want.normalized);
                    tb.rotation = Quaternion.Slerp(Quaternion.identity, q, _tailF * 0.35f * k) * tb.rotation;
                }
                if (TailTwitch > 0.01f) tb.rotation = Quaternion.AngleAxis(Mathf.Sin(_t * 7f + i) * 14f * TailTwitch * k, transform.up) * tb.rotation;
            }
            Stillness(dt, right, fwd);
            CradlePose(dt);
            if (_ear > 0.001f && EarToward.HasValue && _head != null)
            {
                // the ear nearer to her turns toward her (yaw about up) and gives a small flick
                var to = EarToward.Value - _head.position; to.y = 0f;
                float side = Vector3.Dot(to, right);
                var ear = side >= 0f ? (_earL != null && Vector3.Dot(_earL.position - _head.position, right) >= 0f ? _earL : _earR)
                                     : (_earL != null && Vector3.Dot(_earL.position - _head.position, right) < 0f ? _earL : _earR);
                if (ear != null)
                {
                    float yaw = Vector3.SignedAngle(fwd, to, Vector3.up);
                    float flick = Mathf.Max(0f, Mathf.Sin(_t * 9f)) * Mathf.Max(0f, Mathf.Sin(_t * 0.9f)) * 10f;
                    ear.rotation = Quaternion.AngleAxis(Mathf.Clamp(yaw, -70f, 70f) * 0.55f * _ear, Vector3.up) * Quaternion.AngleAxis(-flick * _ear, right) * ear.rotation;
                }
            }
        }

        /// <summary>QF30 (14): breathing (the chest / flank rises and falls) and, after the last breath, slack ears, a limp tail, the head
        /// settled. Eases; never pops.</summary>
        void Stillness(float dt, Vector3 right, Vector3 fwd)
        {
            _slack = Mathf.MoveTowards(_slack, Slack, dt * 0.35f);
            if (Breath > 0.001f && _chest != null)
            {
                _breathT += dt * BreathRate * Mathf.PI * 2f * 0.5f;
                float b = (0.5f - 0.5f * Mathf.Cos(_breathT)) * Breath;
                _chest.localScale = Vector3.one * (1f + 0.06f * b);
                Rot(_chest, right, -3f * b);
            }
            else if (_chest != null && _chest.localScale != Vector3.one) _chest.localScale = Vector3.MoveTowards(_chest.localScale, Vector3.one, dt * 0.2f);
            if (_slack < 0.001f) return;
            var up = transform.up;
            if (_earL != null) _earL.rotation = Quaternion.AngleAxis(-55f * _slack, right) * Quaternion.AngleAxis(25f * _slack, fwd) * _earL.rotation;
            if (_earR != null) _earR.rotation = Quaternion.AngleAxis(-55f * _slack, right) * Quaternion.AngleAxis(-25f * _slack, fwd) * _earR.rotation;
            if (_head != null && SettleTo.HasValue)
            {
                var want = SettleTo.Value - _head.position; var cur = fwd;
                if (want.sqrMagnitude > 1e-6f) _head.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(cur, want.normalized), 0.45f * _slack) * _head.rotation;
            }
            // the tail lies limp: each joint drops toward the floor along the body's back direction
            for (int i = 0; i < _tail.Count; i++)
            {
                var tb = _tail[i]; if (tb == null) continue; float k = (i + 1f) / _tail.Count;
                var want = (-fwd - Vector3.up * 0.6f).normalized;
                tb.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(tb.up, want), _slack * 0.5f * k) * tb.rotation;
            }
        }

        /// <summary>QF32 (1): his head drawn into her lap (FIN_10) - the neck (most) and the head (the rest) turn so his head moves toward
        /// this point (her thighs, just above her knees), eased in, bounded (neck 35 deg, head 35 deg) so it never folds. Null = none.</summary>
        public Vector3? Cradle; float _cradle;
        public float CradleLift { get; private set; }
        void CradlePose(float dt)
        {
            _cradle = Mathf.MoveTowards(_cradle, Cradle.HasValue ? 1f : 0f, dt * 0.8f);
            if (_cradle < 0.001f || !Cradle.HasValue || _neck == null || _head == null) return;
            float y0 = _head.position.y;
            var to = Cradle.Value;
            var cur = _head.position - _neck.position; var want = to - _neck.position;
            if (cur.sqrMagnitude > 1e-6f && want.sqrMagnitude > 1e-6f)
            {
                var q = Quaternion.FromToRotation(cur.normalized, want.normalized);
                q.ToAngleAxis(out float ang, out var ax); if (ang > 180f) ang -= 360f;
                _neck.rotation = Quaternion.AngleAxis(Mathf.Clamp(ang, -35f, 35f) * Mathf.SmoothStep(0f, 1f, _cradle), ax) * _neck.rotation;
            }
            // the head follows the rest of the way (its own forward toward the point, a little)
            var hf = _head.position - _neck.position; var hw = to - _head.position;
            if (hw.sqrMagnitude > 1e-6f && hf.sqrMagnitude > 1e-6f)
            {
                var q = Quaternion.FromToRotation(hf.normalized, (hf.normalized + hw.normalized * 0.6f).normalized);
                q.ToAngleAxis(out float ang, out var ax); if (ang > 180f) ang -= 360f;
                _head.rotation = Quaternion.AngleAxis(Mathf.Clamp(ang, -35f, 35f) * _cradle, ax) * _head.rotation;
            }
            CradleLift = _head.position.y - y0;
        }

        /// <summary>QF28 (4): the riding crouch, after the clip: legs folded under (two-bone IK), head up, ears back, tail wrapped.</summary>
        void RidePose(float dt, Vector3 right, Vector3 fwd)
        {
            _ride = Mathf.MoveTowards(_ride, Ride, dt * 2.5f);
            if (_ride < 0.01f) return;
            var up = transform.up;
            for (int i = 0; i < 4; i++)
            {
                if (_legU[i] == null || _legL[i] == null || _legF[i] == null) continue;
                var a = _legU[i].position; float len = (_legL[i].position - a).magnitude + (_legF[i].position - _legL[i].position).magnitude;
                bool front = i < 2;
                var tgt = a - up * len * 0.22f + fwd * len * (front ? 0.36f : 0.50f);          // folded flat: she lies low in the bed          // tucked under: front paws under the chest, hind feet folded forward
                PlantLeg(i, Vector3.Lerp(_legF[i].position, tgt, _ride), _legF[i].rotation);
            }
            Rot(_neck, right, -14f * _ride); Rot(_head, right, -10f * _ride);                 // head up behind the windscreen
            if (_earL != null) _earL.rotation = Quaternion.AngleAxis(-50f * _ride, right) * _earL.rotation;   // ears laid back
            if (_earR != null) _earR.rotation = Quaternion.AngleAxis(-50f * _ride, right) * _earR.rotation;
            // the tail wrapped round her right side, toward her front paws
            var to = transform.position + right * 0.22f * transform.lossyScale.x + fwd * 0.18f * transform.lossyScale.x + up * 0.04f * transform.lossyScale.x;
            for (int i = 0; i < _tail.Count; i++)
            {
                var tb = _tail[i]; if (tb == null) continue; float k = (i + 1f) / _tail.Count;
                var want = to - tb.position; if (want.sqrMagnitude < 1e-6f) continue;
                tb.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(tb.up, want.normalized), _ride * 0.55f * k) * tb.rotation;
            }
        }

        void Update()
        {
            if (!Alive || !HasEars) return;
            if (_t > _nextFlick)
            {
                _flickEar = Random.value < 0.5f ? _earL : _earR; _flickT = 0f; EarFlicks++;
                _nextFlick = _t + Random.Range(2.5f, 6.5f);
            }
        }

        void FlickEar()
        {
            if (_flickT < 0f || _flickEar == null) return;
            _flickT += Time.deltaTime;
            float u = _flickT / 0.28f;
            if (u > 1f) { _flickT = -1f; return; }
            _flickEar.rotation = Quaternion.AngleAxis(-Mathf.Sin(u * Mathf.PI) * 22f, transform.right) * Quaternion.AngleAxis(Mathf.Sin(u * Mathf.PI) * 12f, Vector3.up) * _flickEar.rotation;
        }

        /// <summary>Two-bone IK: upper + lower leg so the ankle (foot bone head) is back at <paramref name="target"/>,
        /// the knee bending on the side it already bends to; the paw keeps its world rotation.</summary>
        void PlantLeg(int i, Vector3 target, Quaternion footRot)
        {
            Transform u = _legU[i], l = _legL[i], f = _legF[i];
            Vector3 a = u.position, b = l.position, c = f.position;
            float l1 = (b - a).magnitude, l2 = (c - b).magnitude;
            var at = target - a; float d = Mathf.Clamp(at.magnitude, Mathf.Abs(l1 - l2) + 1e-4f, l1 + l2 - 1e-4f);
            var dir = at.normalized;
            var n = Vector3.Cross(c - a, b - a);
            if (n.sqrMagnitude < 1e-10f) n = transform.right;
            n.Normalize();
            float A = Mathf.Acos(Mathf.Clamp((l1 * l1 + d * d - l2 * l2) / (2f * l1 * d), -1f, 1f)) * Mathf.Rad2Deg;
            var k1 = a + Quaternion.AngleAxis(A, n) * dir * l1;
            var k2 = a + Quaternion.AngleAxis(-A, n) * dir * l1;
            var knee = (k1 - b).sqrMagnitude < (k2 - b).sqrMagnitude ? k1 : k2;
            u.rotation = Quaternion.FromToRotation(b - a, knee - a) * u.rotation;
            var c2 = f.position; var b2 = l.position;
            l.rotation = Quaternion.FromToRotation(c2 - b2, target - b2) * l.rotation;
            f.rotation = footRot;
        }

        static void Rot(Transform t, Vector3 axis, float deg) { if (t != null && Mathf.Abs(deg) > 0.01f) t.rotation = Quaternion.AngleAxis(deg, axis) * t.rotation; }

        /// <summary>Head tilt side (+1 / -1) - studying one face, then another.</summary>
        public void TiltSide(float side) { TiltDeg = Mathf.Abs(TiltDeg) * Mathf.Sign(side); }
    }
}
