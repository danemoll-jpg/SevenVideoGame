using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 16 — the Machinist on Dan's Mixamo rig (Art/Mixamo/machinist/machinist_body_mixamo.fbx, ~4 m), after Kyle's
    /// sketch (adapted: no cape; the arm cannon is a heavy maintenance tool arm). Runs after the animator and MarsFolk:
    ///  - the front plate hangs from its own hinge bone at the belt (FightDemoBuilderAsteroid re-skins the plate onto
    ///    "PlateHinge"): it swings forward with whichever thigh comes forward, so it never cuts through his legs;
    ///  - no face: the visor slit glows white with his voice (and dims to a thin line when he is quiet / stunned);
    ///  - small, precise head adjustments (a tilt, a stop, a re-centre - never a sway); deliberate, economical movement;
    ///  - his power couplings (hips, chest, shoulders) glow; an arc-blade hit makes the nearest one flare, and they dim
    ///    as his energy drains;
    ///  - the two tool assemblies on his forearms (the cutter, the clamp tool) can be taken off into a service-station
    ///    clamp (AST_07: each phase ends with one locked away).
    /// </summary>
    [DefaultExecutionOrder(70)]
    public class MachinistRig : MonoBehaviour
    {
        public Animator Anim;
        public Transform PlateHinge;
        public Renderer Visor;
        public Renderer[] Couplings;
        public Transform CutterTool, ClampTool;
        public AudioSource Voice;
        [System.NonSerialized] public float Energy = 1f;
        [System.NonSerialized] public bool Stunned;
        [System.NonSerialized] public Vector3? LookAt;
        Transform _hips, _thighL, _thighR, _head, _neck;
        float _talk, _adjT, _flare; Quaternion _adj = Quaternion.identity, _adjTo = Quaternion.identity; int _flareI = -1;
        readonly float[] _buf = new float[256];
        Quaternion _plateRest; bool _plateSet;

        void Awake()
        {
            if (Anim == null) Anim = GetComponentInChildren<Animator>();
            if (Anim != null && Anim.isHuman)
            {
                _hips = Anim.GetBoneTransform(HumanBodyBones.Hips); _head = Anim.GetBoneTransform(HumanBodyBones.Head); _neck = Anim.GetBoneTransform(HumanBodyBones.Neck);
                _thighL = Anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg); _thighR = Anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            }
        }

        /// <summary>The nearest coupling to a point flares (a blade strike landed).</summary>
        public void Flare(Vector3 near)
        {
            if (Couplings == null) return;
            float bd = 1e9f; for (int i = 0; i < Couplings.Length; i++) { if (Couplings[i] == null) continue; float d = (Couplings[i].transform.position - near).sqrMagnitude; if (d < bd) { bd = d; _flareI = i; } }
            _flare = 1f;
        }

        // ---------------------------------------------------------------- QF21 (8) calm in conversation, (11) restrained
        /// <summary>QF21 (8): while he talks he stands still and upright - the borrowed idle is held to a slow, damped pose
        /// (arms mostly at rest); his acting is the head, the visor and at most one restrained gesture. Set by the director
        /// for conversations; big arm movement stays for work and the fight.</summary>
        [System.NonSerialized] public bool Calm;
        /// <summary>QF21 (11): in the clamps / cradle his body is locked where it is (nothing passes through the pads); only the
        /// head turns (LookAt), the visor glows with his voice, the claw twitches once (<see cref="Twitch"/>).</summary>
        [System.NonSerialized] public bool Restrained;
        float _holdW; Transform[] _holdBones; Quaternion[] _holdRot; Vector3 _hipsHold; bool _holdSnap;
        float _twitch; Transform _handR;
        public float HoldDrift { get; private set; }
        /// <summary>Called at the end of his LateUpdate (his final pose this frame) - the QF21 clamp check reads it there.</summary>
        public event System.Action AfterPose;
        public void Twitch() { _twitch = 1f; AudioKit.Play("machinist_servo", transform.position + Vector3.up * 2.5f, 0.5f, 0.05f, AudioKit.Bus.Sfx, 30f); }
        void HoldPose(float dt)
        {
            float want = Restrained ? 1f : Calm ? 0.85f : 0f;
            _holdW = Restrained ? 1f : Mathf.MoveTowards(_holdW, want, dt * (want > _holdW ? 2.5f : 1.2f));
            if (Anim == null || !Anim.isHuman) return;
            if (_holdBones == null)
            {
                var l = new System.Collections.Generic.List<Transform>();
                foreach (HumanBodyBones b in System.Enum.GetValues(typeof(HumanBodyBones)))
                {
                    if (b == HumanBodyBones.LastBone || b == HumanBodyBones.Head || b == HumanBodyBones.Neck || b == HumanBodyBones.Jaw || b == HumanBodyBones.LeftEye || b == HumanBodyBones.RightEye) continue;
                    var t = Anim.GetBoneTransform(b); if (t != null) l.Add(t);
                }
                _holdBones = l.ToArray(); _holdRot = new Quaternion[_holdBones.Length];
                _handR = Anim.GetBoneTransform(HumanBodyBones.RightHand);
            }
            if (_holdW <= 0.001f) { _holdSnap = false; return; }
            if (!_holdSnap) { for (int i = 0; i < _holdBones.Length; i++) _holdRot[i] = _holdBones[i].localRotation; if (_hips != null) _hipsHold = _hips.localPosition; _holdSnap = true; }
            // calm: the held pose follows the clip very slowly (a breath of life); restrained: locked
            float follow = Restrained ? 0f : dt * 0.25f;
            float drift = 0f;
            for (int i = 0; i < _holdBones.Length; i++)
            {
                var b = _holdBones[i];
                if (follow > 0f) _holdRot[i] = Quaternion.Slerp(_holdRot[i], b.localRotation, follow);
                drift = Mathf.Max(drift, Quaternion.Angle(b.localRotation, _holdRot[i]));
                b.localRotation = Quaternion.Slerp(b.localRotation, _holdRot[i], _holdW);
            }
            if (_hips != null) _hips.localPosition = Vector3.Lerp(_hips.localPosition, _hipsHold, _holdW);
            HoldDrift = drift * (1f - _holdW);
            // the one claw twitch (a small close-and-open of the tool hand)
            if (_twitch > 0f && _handR != null) { _twitch = Mathf.MoveTowards(_twitch, 0f, dt * 2.2f); _handR.localRotation *= Quaternion.Euler(Mathf.Sin(_twitch * Mathf.PI) * 18f, 0f, 0f); }
        }

        // ---------------------------------------------------------------- QF21 (5) heavy steps, servos
        /// <summary>QF21 (5): every planted foot lands with a heavy metallic clunk (four takes, never the same twice in a
        /// row), positional and loud; a piston hiss now and then; a light camera rumble when the camera is close; servo whir
        /// on turns. The plant is read from the clip's own foot motion (lifted -> back down), like 7's footsteps.</summary>
        public static int Steps, Servos;
        Transform _footL, _footR; float _footMinL = 99f, _footMinR = 99f; bool _upL, _upR; int _lastTake = -1; float _yawLast, _servoCd, _stepCd;
        void StepSounds(float dt)
        {
            if (Anim == null || !Anim.isHuman) return;
            if (_footL == null) { _footL = Anim.GetBoneTransform(HumanBodyBones.LeftFoot); _footR = Anim.GetBoneTransform(HumanBodyBones.RightFoot); _yawLast = transform.eulerAngles.y; }
            if (_footL == null || _footR == null) return;
            _stepCd -= dt;
            float s = Mathf.Max(0.5f, transform.lossyScale.y);
            Foot(_footL, ref _footMinL, ref _upL, s, dt);
            Foot(_footR, ref _footMinR, ref _upR, s, dt);
            // servos: on turns (yaw rate) and quick arm moves
            _servoCd -= dt;
            float yaw = transform.eulerAngles.y, rate = Mathf.Abs(Mathf.DeltaAngle(_yawLast, yaw)) / Mathf.Max(1e-4f, dt); _yawLast = yaw;
            if (rate > 45f && _servoCd <= 0f && !Restrained) { _servoCd = 1.4f; Servos++; AudioKit.Play("machinist_servo", transform.position + Vector3.up * 2.4f, 0.7f, 0.08f, AudioKit.Bus.Sfx, 45f); }
        }
        void Foot(Transform f, ref float min, ref bool up, float s, float dt)
        {
            float h = f.position.y - transform.position.y;
            min = Mathf.Min(min + dt * 0.02f, h);                   // the rest height (slowly forgets)
            if (!up && h > min + 0.14f * s) up = true;
            else if (up && h < min + 0.05f * s)
            {
                up = false;
                if (_stepCd > 0f || Restrained) return;
                _stepCd = 0.18f; Steps++;
                int take; do take = Random.Range(1, 5); while (take == _lastTake); _lastTake = take;
                AudioKit.Play("machinist_step_" + take, f.position, 1f, 0.07f, AudioKit.Bus.Sfx, 70f);
                if (Random.value < 0.3f) AudioKit.Play("machinist_hiss", f.position + Vector3.up * 1.2f, 0.35f, 0.1f, AudioKit.Bus.Sfx, 30f);
                var cam = Camera.main;
                if (cam != null) { float d = Vector3.Distance(cam.transform.position, f.position); if (d < 14f) CameraShake.Quake(0.18f, 0.12f * (1f - d / 14f)); }
            }
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            HoldPose(dt);
            StepSounds(dt);
            // the plate: swings forward with the thigh that comes forward (the thighs' pitch against the hips' down)
            if (PlateHinge != null && _hips != null)
            {
                if (!_plateSet) { _plateRest = PlateHinge.localRotation; _plateSet = true; }
                float Swing(Transform th)
                {
                    if (th == null) return 0f;
                    var down = th.TransformDirection(Vector3.down); var knee = th.childCount > 0 ? (th.GetChild(0).position - th.position).normalized : down;
                    return Vector3.SignedAngle(-_hips.up, knee, _hips.right);
                }
                float a = Mathf.Max(0f, Mathf.Max(-Swing(_thighL), -Swing(_thighR)));
                a = Mathf.Clamp(a * 0.95f + 2f, 0f, 75f);
                PlateHinge.localRotation = _plateRest;
                PlateHinge.rotation = Quaternion.AngleAxis(-a, _hips.right) * PlateHinge.rotation;
            }
            // the voice: the visor slit glows with it
            float lvl = 0f;
            if (Voice != null && Voice.isPlaying)
            {
                Voice.GetOutputData(_buf, 0); foreach (var s in _buf) lvl = Mathf.Max(lvl, Mathf.Abs(s));
                if (lvl <= 0.0001f && DemoBoot.Muted) lvl = 0.25f + 0.25f * Mathf.Abs(Mathf.Sin(Time.time * 6.3f)) * Mathf.Abs(Mathf.Sin(Time.time * 1.9f));
            }
            _talk = Mathf.Lerp(_talk, Mathf.Clamp01(lvl * 2.5f), dt * 14f);
            if (Visor != null) Glowing.Set(Visor, Stunned ? (Mathf.PerlinNoise(Time.time * 18f, 0f) > 0.55f ? 0.9f : 0.05f) : 0.55f + 2.4f * _talk);
            // small, precise head adjustments: hold, then a short exact move to a new small angle (never a sway)
            if (_head != null)
            {
                _adjT -= dt;
                if (_adjT <= 0f) { _adjT = Random.Range(1.6f, 3.4f); _adjTo = Quaternion.Euler(Random.Range(-4f, 4f), Random.Range(-9f, 9f), Random.Range(-3f, 3f)); }
                _adj = Quaternion.RotateTowards(_adj, _adjTo, 40f * dt);
                if (LookAt.HasValue)
                {
                    var to = LookAt.Value - _head.position; var f = transform.forward;
                    float yaw = Mathf.Clamp(Vector3.SignedAngle(new Vector3(f.x, 0f, f.z), new Vector3(to.x, 0f, to.z), Vector3.up), -40f, 40f);
                    float pitch = Mathf.Clamp(-Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg, -30f, 25f);
                    _head.rotation = Quaternion.AngleAxis(yaw * 0.7f, Vector3.up) * Quaternion.AngleAxis(pitch * 0.6f, transform.right) * _head.rotation;
                }
                _head.localRotation *= _adj;
            }
            // the couplings: glow with his energy; a blade hit flares the nearest one
            _flare = Mathf.MoveTowards(_flare, 0f, dt * 2.2f);
            if (Couplings != null)
                for (int i = 0; i < Couplings.Length; i++)
                    if (Couplings[i] != null) Glowing.Set(Couplings[i], Mathf.Lerp(0.15f, 1.6f, Energy) * (Stunned ? 0.45f + 0.4f * Mathf.PerlinNoise(Time.time * 11f, i) : 1f) + (i == _flareI ? _flare * 4f : 0f));
            AfterPose?.Invoke();
        }

        /// <summary>AST_07: a tool assembly is pulled off his forearm into a service-station clamp.</summary>
        public System.Collections.IEnumerator ToolInto(Transform tool, Transform clampJaws, float secs = 0.6f)
        {
            if (tool == null || clampJaws == null) yield break;
            var a = tool.position; var ra = tool.rotation;
            tool.SetParent(clampJaws, true);
            for (float t = 0f; t < 1f; t += Time.deltaTime / secs) { tool.position = Vector3.Lerp(a, clampJaws.position, t * t); tool.rotation = Quaternion.Slerp(ra, clampJaws.rotation, t); yield return null; }
            tool.position = clampJaws.position; tool.rotation = clampJaws.rotation;
        }
    }
}
