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

        void LateUpdate()
        {
            float dt = Time.deltaTime;
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
