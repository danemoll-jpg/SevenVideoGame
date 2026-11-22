using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 17 — Ratwing alive (Kyle's design; the Round 4 model, a generic rig with wing chains: wing_root / wing_upper /
    /// three finger spars per wing, jaw, eyes). His clips are only "idle" and the wake "sequence", so flight is procedural,
    /// after the animator: the body hangs under a <see cref="Height"/> offset (the BossFight / director move his root on the
    /// ground plane). QF23 (20) - Dan: "he moves very stiffly". Now a fast, wiry flier and a sly talker:
    ///  - WINGS: a real flap cycle (a quick power stroke down, a slower fold-and-lift up: the spars fold in on the upstroke),
    ///    overlapping motion along each chain (root -> upper -> spars lag and settle: second-order springs), glides with the
    ///    wings spread and small corrections whenever he moves fast and level, a snap-open before a dive (<see cref="SnapOpen"/>),
    ///    folded neatly when perched / clinging, drooping when stunned.
    ///  - FLIGHT: he banks into turns (the body rolls with his turn rate), pitches with the climb / dive, eases into landings
    ///    (a crouch and a wing-brace for a moment).
    ///  - BODY: a hunched, twitchy idle - head cocks, shoulder rolls; talking moves the head and shoulders with the voice's
    ///    loudness and a sneering head tilt; the jaw opens with it.
    ///  - SHOCK (QF23 21): <see cref="Seize"/> - the arc blades make him seize (rapid jitter through the chest, head, wings).
    ///  - BIA_07 (QF23 15): <see cref="LookUp"/> (he glances up at the gap) and <see cref="Gather"/> (gathers his feet under him).
    /// The rig is generic (not Humanoid), so the humanoid clip set can't be retargeted onto it without re-rigging - the life
    /// is procedural on the existing bones.
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
        [System.NonSerialized] public float LookUp, Gather;   // 0..1 (BIA_07)
        [System.NonSerialized] public bool Sneer;       // taunts: a sneering tilt while he speaks
        Transform _wrL, _wrR, _wuL, _wuR, _jaw, _hips, _chest, _head, _neck; Transform[] _spL, _spR;
        float _phase, _flapCd, _talk, _fold, _bank, _seize, _snap, _land, _glide, _cling, _pitchS; readonly float[] _buf = new float[256];
        // the lagged chain angles (overlapping motion): per side, root / upper / spars - value + velocity
        readonly float[] _a = new float[6], _v = new float[6];
        Vector3 _lastPos; float _lastYaw; Mode _lastMode;
        float _cock, _cockTo, _nextCock, _roll, _rollTo;
        public int Flaps { get; private set; }
        public int Seizes { get; private set; }

        void Awake()
        {
            if (Anim == null) Anim = GetComponentInChildren<Animator>();
            Transform F(string n) { foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == n) return t; return null; }
            _wrL = F("wing_root.L"); _wrR = F("wing_root.R"); _wuL = F("wing_upper.L"); _wuR = F("wing_upper.R");
            _jaw = F("jaw"); _hips = F("hips"); _chest = F("chest"); _head = F("head"); _neck = F("neck");
            _spL = new[] { F("wing_f1a.L"), F("wing_f2a.L"), F("wing_f3a.L") }; _spR = new[] { F("wing_f1a.R"), F("wing_f2a.R"), F("wing_f3a.R") };
            _lastPos = transform.position; _lastYaw = transform.eulerAngles.y;
            PlayIdle();
        }
        // QF23 (20): THE UPSIDE-DOWN RATWING - his animator's default state is the Round 4 wake "sequence", which opens with him
        // hanging asleep upside down; nothing on the mothership ever switched him to "idle", so he played those hanging frames
        // (slowed to 0.3x in flight). He stays in his standing "idle" here; the rig does the rest (and the cling's roll).
        void PlayIdle() { if (Anim != null && Anim.runtimeAnimatorController != null && Anim.HasState(0, Animator.StringToHash("idle"))) Anim.Play("idle", 0, Random.value); }
        void OnEnable() { PlayIdle(); }
        /// <summary>The wings snap wide open (before a dive).</summary>
        public void SnapOpen() { _snap = 1f; }
        /// <summary>A shock: he seizes for <paramref name="secs"/>.</summary>
        public void Seize(float secs) { _seize = Mathf.Max(_seize, secs); Seizes++; }

        static float Spring(ref float x, ref float v, float target, float k, float d, float dt) { v += (k * (target - x) - d * v) * dt; x += v * dt; return x; }

        void LateUpdate()
        {
            float dt = Mathf.Max(1e-4f, Time.deltaTime), t = Time.time;
            // how he is moving (for glides and banking)
            var vel = (transform.position - _lastPos) / dt; vel.y = 0f; _lastPos = transform.position;
            float yaw = transform.eulerAngles.y, turn = Mathf.DeltaAngle(_lastYaw, yaw) / dt; _lastYaw = yaw;
            bool flying = Now == Mode.Fly || Now == Mode.Swoop;
            if (_lastMode != Now) { if ((_lastMode == Mode.Fly || _lastMode == Mode.Swoop) && (Now == Mode.Perched || Now == Mode.Cling)) _land = 1f; _lastMode = Now; }
            _land = Mathf.MoveTowards(_land, 0f, dt / 0.6f); _snap = Mathf.MoveTowards(_snap, 0f, dt / 0.45f); _seize = Mathf.Max(0f, _seize - dt);
            _glide = Mathf.MoveTowards(_glide, flying && Now == Mode.Fly && vel.magnitude > 5.5f ? 1f : 0f, dt * 1.5f);
            _bank = Mathf.Lerp(_bank, flying ? Mathf.Clamp(-turn * 0.22f, -38f, 38f) : 0f, dt * 4f);
            // the body: lifted, pitched with the climb / dive, rolled into turns; landing = a crouch for a moment
            if (Body != null)
            {
                float crouch = (_land * 0.25f + Gather * 0.22f) * (Now == Mode.Cling ? 0f : 1f);
                Body.localPosition = Vector3.Lerp(Body.localPosition, new Vector3(0f, Height - crouch, 0f), dt * 8f);
                // QF23 (20): clinging = rolled 180 deg (hanging under the bridge). The old code blended between the two with a
                // quaternion slerp - from an exactly opposite rotation that blend is unstable and he stayed UPSIDE DOWN after
                // letting go. Now an explicit roll that turns over in ~0.5 s, everything else on top of it.
                _cling = Mathf.MoveTowards(_cling, Now == Mode.Cling ? 1f : 0f, dt * 2f);
                _pitchS = Mathf.Lerp(_pitchS, Now == Mode.Cling ? 0f : Pitch + _land * 12f + Gather * 10f, dt * 6f);
                float roll = 180f * Mathf.SmoothStep(0f, 1f, _cling) + (Now == Mode.Stunned ? Mathf.Sin(t * 3.1f) * 8f : 0f) + _bank * (1f - _cling);
                Body.localRotation = Quaternion.Euler(_pitchS, 0f, roll);
            }
            if (Anim != null) Anim.speed = flying ? 0.3f : 1f;
            var fwd = transform.forward; var up = transform.up; var right = transform.right;
            // the flap cycle: power stroke down fast (0..0.4), fold-and-lift up (0.4..1)
            float beat = 0f, foldCycle = 0f;
            if (Now == Mode.Fly)
            {
                float rate = Mathf.Lerp(2.7f, 0.6f, _glide);
                _phase += dt * rate; float u = _phase % 1f;
                beat = u < 0.4f ? Mathf.Lerp(42f, -36f, Mathf.SmoothStep(0f, 1f, u / 0.4f)) : Mathf.Lerp(-36f, 42f, Mathf.SmoothStep(0f, 1f, (u - 0.4f) / 0.6f));
                foldCycle = u < 0.4f ? 0f : Mathf.Sin((u - 0.4f) / 0.6f * Mathf.PI) * 0.6f;
                beat = Mathf.Lerp(beat, 8f + Mathf.Sin(t * 1.3f) * 4f, _glide);                  // gliding: spread, small corrections
                foldCycle *= 1f - _glide;
                _flapCd -= dt;
                if (u < 0.08f && _flapCd <= 0f && _glide < 0.5f) { _flapCd = 0.5f; Flaps++; AudioKit.Play("wing_flutter", transform.position + Vector3.up * Height, 0.5f, 0.12f, AudioKit.Bus.Sfx, 40f); }
            }
            float foldWant = Now == Mode.Stunned ? 1f : Now == Mode.Perched || Now == Mode.Cling ? 0.85f : Now == Mode.Swoop ? 0.5f : foldCycle;
            foldWant = Mathf.Lerp(foldWant, 0.35f, _land) * (1f - _snap) + (_snap > 0f ? -0.3f * _snap : 0f) + Gather * 0.3f;   // the brace on landing; wide on the snap
            _fold = Mathf.MoveTowards(_fold, foldWant, dt * 3.5f);
            // overlapping motion: root follows the beat, upper lags the root, spars lag the upper (and settle)
            for (int s = 0; s < 2; s++)
            {
                int o = s * 3;
                Spring(ref _a[o], ref _v[o], beat + _snap * 30f, 260f, 22f, dt);
                Spring(ref _a[o + 1], ref _v[o + 1], _a[o] * 0.7f, 140f, 12f, dt);
                Spring(ref _a[o + 2], ref _v[o + 2], _a[o + 1] * 0.6f, 90f, 8f, dt);
            }
            float sj = _seize > 0f ? 1f : 0f;
            for (int s = 0; s < 2; s++)
            {
                var root = s == 0 ? _wrL : _wrR; var upper = s == 0 ? _wuL : _wuR; var sp = s == 0 ? _spL : _spR; float sg = s == 0 ? 1f : -1f; int o = s * 3;
                float jit = sj * Random.Range(-14f, 14f);
                if (root != null) root.rotation = Quaternion.AngleAxis((_a[o] + jit) * sg, fwd) * Quaternion.AngleAxis(-_fold * 45f * sg, up) * root.rotation;
                if (upper != null) upper.rotation = Quaternion.AngleAxis((_a[o + 1] * 0.55f + jit * 0.5f) * sg, fwd) * Quaternion.AngleAxis(-_fold * 35f * sg, up) * upper.rotation;
                if (sp != null) for (int k = 0; k < sp.Length; k++) if (sp[k] != null) sp[k].rotation = Quaternion.AngleAxis((_a[o + 2] * (0.3f + 0.1f * k)) * sg, fwd) * Quaternion.AngleAxis(-_fold * (25f + 6f * k) * sg, up) * sp[k].rotation;
            }
            // the head and shoulders: a twitchy idle (cocks, rolls), the voice, the glance up (BIA_07), stunned lolling
            if (t > _nextCock) { _cockTo = Random.Range(-22f, 22f); _rollTo = Random.Range(-9f, 9f); _nextCock = t + Random.Range(0.8f, 2.6f); }
            _cock = Mathf.Lerp(_cock, _cockTo, dt * 14f); _roll = Mathf.Lerp(_roll, _rollTo, dt * 5f);   // quick, bird-like
            float lvl = 0f;
            if (Voice != null && Voice.isPlaying) { Voice.GetOutputData(_buf, 0); foreach (var x in _buf) lvl = Mathf.Max(lvl, Mathf.Abs(x)); if (lvl <= 0.0001f && DemoBoot.Muted) lvl = 0.3f * Mathf.Abs(Mathf.Sin(t * 9f)); }
            _talk = Mathf.Lerp(_talk, Mathf.Clamp01(lvl * 3f), dt * 18f);
            if (_head != null)
            {
                if (Now == Mode.Stunned) _head.rotation = Quaternion.AngleAxis(Mathf.Sin(t * 2.3f) * 12f, fwd) * Quaternion.AngleAxis(18f, right) * _head.rotation;
                else
                {
                    float idleK = flying ? 0.25f : 1f;
                    _head.rotation = Quaternion.AngleAxis(_cock * idleK + (Sneer || _talk > 0.05f ? 14f * Mathf.Max(_talk, Sneer ? 0.6f : 0f) : 0f), fwd) * Quaternion.AngleAxis(-LookUp * 38f - _talk * 6f, right) * Quaternion.AngleAxis(_cock * 0.5f * idleK, up) * _head.rotation;
                }
                if (sj > 0f) _head.rotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f)) * _head.rotation;
            }
            if (_chest != null)
            {
                if (Now == Mode.Stunned) _chest.rotation = Quaternion.AngleAxis(10f, right) * _chest.rotation;
                else _chest.rotation = Quaternion.AngleAxis(_roll * (flying ? 0.3f : 1f) + _talk * 5f * Mathf.Sin(t * 6f), fwd) * Quaternion.AngleAxis(Gather * 14f + _talk * 4f, right) * _chest.rotation;   // shoulder rolls; talking
                if (sj > 0f) _chest.rotation = Quaternion.Euler(Random.Range(-8f, 8f), 0f, Random.Range(-8f, 8f)) * _chest.rotation;
            }
            if (_jaw != null && _talk > 0.01f) _jaw.rotation = Quaternion.AngleAxis(_talk * 16f, right) * _jaw.rotation;
        }
    }
}
