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
        /// <summary>Total time the chewing nods ran (checks: mouth movement only while eating).</summary>
        public float ChewSeconds { get; private set; }

        Transform _chest, _neck, _head, _earL, _earR;
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
                    case "neck": _neck = t; break;
                    case "head": _head = t; break;
                    case "earL": _earL = t; break;
                    case "earR": _earR = t; break;
                }
            }
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
            var right = transform.right; var fwd = transform.forward;
            bool chew = Chewing && _eat > 0.6f;
            if (chew) ChewSeconds += dt;
            float nod = chew ? Mathf.Sin(_t * 2f * Mathf.PI * 2.6f) * 3.5f + Mathf.Sin(_t * 2f * Mathf.PI * 0.4f) * 2f : 0f;
            // + angle about the cat's right axis = nose down
            Rot(_chest, right, 16f * _eat - 6f * _lean);
            Rot(_neck, right, 52f * _eat - 10f * _lean);
            Rot(_head, right, 34f * _eat + nod + 8f * _lean);
            if (_head != null && _tilt > 0.001f) _head.rotation = Quaternion.AngleAxis(TiltDeg * _tilt, fwd) * _head.rotation;
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

        static void Rot(Transform t, Vector3 axis, float deg) { if (t != null && Mathf.Abs(deg) > 0.01f) t.rotation = Quaternion.AngleAxis(deg, axis) * t.rotation; }

        /// <summary>Head tilt side (+1 / -1) - studying one face, then another.</summary>
        public void TiltSide(float side) { TiltDeg = Mathf.Abs(TiltDeg) * Mathf.Sign(side); }
    }
}
