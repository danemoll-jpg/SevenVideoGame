using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 17 — Ratwing alive (Kyle's design; the Round 4 model, a generic rig with wing chains: wing_root / wing_upper /
    /// three finger spars per wing, jaw, eyes). His clips are only "idle" and the wake "sequence", so flight is procedural,
    /// after the animator: the body hangs under a <see cref="Height"/> offset (the BossFight / director move his root on the
    /// ground plane), the wings beat (<see cref="Mode.Fly"/>), sweep back in a dive (<see cref="Mode.Swoop"/>), fold and droop
    /// when he is stunned on a platform (<see cref="Mode.Stunned"/>); perched / clinging he plays his idle. The eyes keep a
    /// faint white glint; the jaw opens with his voice. Every wing beat is a soft leathery flap (positional).
    /// </summary>
    [DefaultExecutionOrder(70)]
    public class RatwingRig : MonoBehaviour
    {
        public enum Mode { Perched, Fly, Swoop, Stunned, Cling }
        public Animator Anim;
        public Transform Body;                          // the model root (child of this transform): lifted by Height
        [System.NonSerialized] public Mode Now = Mode.Perched;
        [System.NonSerialized] public float Height;     // metres above this transform (the ground under him)
        [System.NonSerialized] public AudioSource Voice;
        [System.NonSerialized] public float Pitch;      // body pitch (deg; + nose down in a dive)
        Transform _wrL, _wrR, _wuL, _wuR, _jaw, _hips, _chest, _head; Transform[] _spL, _spR;
        float _phase, _flapCd, _talk, _fold; readonly float[] _buf = new float[256];
        public int Flaps { get; private set; }

        void Awake()
        {
            if (Anim == null) Anim = GetComponentInChildren<Animator>();
            Transform F(string n) { foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == n) return t; return null; }
            _wrL = F("wing_root.L"); _wrR = F("wing_root.R"); _wuL = F("wing_upper.L"); _wuR = F("wing_upper.R");
            _jaw = F("jaw"); _hips = F("hips"); _chest = F("chest"); _head = F("head");
            _spL = new[] { F("wing_f1a.L"), F("wing_f2a.L"), F("wing_f3a.L") }; _spR = new[] { F("wing_f1a.R"), F("wing_f2a.R"), F("wing_f3a.R") };
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (Body != null) Body.localPosition = Vector3.Lerp(Body.localPosition, new Vector3(0f, Height, 0f), dt * 8f);
            // clinging beneath a bridge he hangs upside down (feet up at Height, body below it)
            if (Body != null) Body.localRotation = Now == Mode.Cling ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.Slerp(Body.localRotation, Quaternion.Euler(Pitch, 0f, Now == Mode.Stunned ? Mathf.Sin(Time.time * 3.1f) * 8f : 0f), dt * 5f);
            bool flying = Now == Mode.Fly || Now == Mode.Swoop;
            if (Anim != null) Anim.speed = flying ? 0.3f : 1f;
            var fwd = transform.forward; var up = transform.up;
            // the wings: a beat (fly), swept back and half closed (swoop), folded and drooping (stunned / perched)
            _fold = Mathf.MoveTowards(_fold, Now == Mode.Stunned ? 1f : Now == Mode.Perched || Now == Mode.Cling ? 0.6f : Now == Mode.Swoop ? 0.45f : 0f, dt * 3f);
            float beat = 0f;
            if (Now == Mode.Fly)
            {
                _phase += dt * 2.6f * Mathf.PI * 2f * 0.5f;
                beat = Mathf.Sin(_phase) * 38f;
                _flapCd -= dt;
                if (Mathf.Cos(_phase) > 0.97f && _flapCd <= 0f) { _flapCd = 0.6f; Flaps++; AudioKit.Play("wing_flutter", transform.position + Vector3.up * Height, 0.5f, 0.12f, AudioKit.Bus.Sfx, 40f); }
            }
            foreach (var (root, upper, sp, s) in new[] { (_wrL, _wuL, _spL, 1f), (_wrR, _wuR, _spR, -1f) })
            {
                if (root != null) root.rotation = Quaternion.AngleAxis(beat * s, fwd) * Quaternion.AngleAxis(-_fold * 45f * s, up) * root.rotation;
                if (upper != null) upper.rotation = Quaternion.AngleAxis(beat * 0.45f * s, fwd) * Quaternion.AngleAxis(-_fold * 35f * s, up) * upper.rotation;
                if (sp != null) foreach (var f in sp) if (f != null) f.rotation = Quaternion.AngleAxis(-_fold * 25f * s, up) * f.rotation;
            }
            // stunned: the head lolls, the chest sags
            if (Now == Mode.Stunned)
            {
                if (_head != null) _head.rotation = Quaternion.AngleAxis(Mathf.Sin(Time.time * 2.3f) * 12f, fwd) * Quaternion.AngleAxis(18f, transform.right) * _head.rotation;
                if (_chest != null) _chest.rotation = Quaternion.AngleAxis(10f, transform.right) * _chest.rotation;
            }
            // the voice: the jaw opens with it
            float lvl = 0f;
            if (Voice != null && Voice.isPlaying) { Voice.GetOutputData(_buf, 0); foreach (var x in _buf) lvl = Mathf.Max(lvl, Mathf.Abs(x)); if (lvl <= 0.0001f && DemoBoot.Muted) lvl = 0.3f * Mathf.Abs(Mathf.Sin(Time.time * 9f)); }
            _talk = Mathf.Lerp(_talk, Mathf.Clamp01(lvl * 3f), dt * 18f);
            if (_jaw != null && _talk > 0.01f) _jaw.rotation = Quaternion.AngleAxis(_talk * 16f, transform.right) * _jaw.rotation;
        }
    }
}
