using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 14 — a Cetacean of Europa (Tools/Blender/cleanup_cetacean.py: Spine0 (head) … Spine7 (flukes), FinL, FinR; no
    /// clips). The swim is procedural: an up-and-down wave runs from the head to the flukes (stronger toward the tail, faster
    /// with speed), the body bends into its turns, the pectoral fins sway slowly. It moves along a loop of points, follows a
    /// target (the guide escorting the bike), glides to a spot and holds there (the keeper), or is placed by a scene.
    /// <see cref="Voice"/>: while that source plays, the head nods and the fins lift with its loudness (the keeper speaks -
    /// no mouth). <see cref="Call"/> plays one of its calls from where it is.
    /// </summary>
    public class Cetacean : MonoBehaviour
    {
        public enum Mode { Hold, Loop, Follow, GlideTo }
        public Mode Current = Mode.Hold;
        public Vector3[] Points;
        public float Speed = 4f, TurnRate = 40f, Wave = 7f, WaveRate = 0.9f;
        public Transform FollowTarget; public Vector3 FollowOffset;
        public AudioSource Voice;
        public float Talk { get; private set; }
        Transform[] _spine; Transform _finL, _finR;
        Quaternion[] _rest; Quaternion _restL, _restR;
        Vector3 _holdAt, _glideTo; float _glideSpeed; int _i; float _phase, _turn, _speedNow;
        readonly float[] _buf = new float[256];

        void Awake()
        {
            var all = GetComponentsInChildren<Transform>(true);
            _spine = new Transform[8];
            foreach (var t in all)
            {
                for (int i = 0; i < 8; i++) if (t.name == "Spine" + i) _spine[i] = t;
                if (t.name == "FinL") _finL = t; if (t.name == "FinR") _finR = t;
            }
            _rest = new Quaternion[8];
            for (int i = 0; i < 8; i++) if (_spine[i] != null) _rest[i] = _spine[i].localRotation;
            if (_finL != null) _restL = _finL.localRotation; if (_finR != null) _restR = _finR.localRotation;
            _holdAt = transform.position; _phase = Random.value * 10f;
        }

        public void Hold(Vector3 at) { Current = Mode.Hold; _holdAt = at; }
        public void HoldHere() => Hold(transform.position);
        public void Loop(Vector3[] pts, float speed) { Points = pts; Speed = speed; Current = Mode.Loop; _i = 0; }
        public void Follow(Transform t, Vector3 offset, float speed) { FollowTarget = t; FollowOffset = offset; Speed = speed; Current = Mode.Follow; }
        public void GlideTo(Vector3 p, float speed) { _glideTo = p; _glideSpeed = speed; Current = Mode.GlideTo; }
        public bool Arrived => Current != Mode.GlideTo || (transform.position - _glideTo).magnitude < 0.6f;

        public void Call(string sfx, float volume = 0.8f, float range = 400f) => AudioKit.Play(sfx, transform.position, volume, 0.05f, AudioKit.Bus.Sfx, range);

        void Update()
        {
            float dt = Time.deltaTime; if (dt <= 0f) return;
            Vector3 want; float sp;
            switch (Current)
            {
                case Mode.Loop:
                    if (Points == null || Points.Length == 0) { want = transform.position; sp = 0f; break; }
                    want = Points[_i]; sp = Speed;
                    if ((want - transform.position).magnitude < Mathf.Max(2f, Speed * 0.8f)) _i = (_i + 1) % Points.Length;
                    break;
                case Mode.Follow:
                    if (FollowTarget == null) { want = transform.position; sp = 0f; break; }
                    var off = FollowTarget.right * FollowOffset.x + Vector3.up * FollowOffset.y + FollowTarget.forward * FollowOffset.z;
                    want = FollowTarget.position + off;
                    float dist = (want - transform.position).magnitude;
                    sp = Mathf.Clamp(dist * 0.9f, 0f, Speed);
                    break;
                case Mode.GlideTo:
                    want = _glideTo; sp = Mathf.Min(_glideSpeed, (want - transform.position).magnitude * 1.2f);
                    break;
                default:
                    want = _holdAt + new Vector3(Mathf.Sin(_phase * 0.21f), Mathf.Sin(_phase * 0.17f) * 0.5f, Mathf.Cos(_phase * 0.19f)) * 0.6f;
                    sp = Mathf.Min(1.2f, (want - transform.position).magnitude);
                    break;
            }
            _speedNow = Mathf.MoveTowards(_speedNow, sp, dt * Mathf.Max(1f, Speed * 0.5f));
            var to = want - transform.position;
            if (to.sqrMagnitude > 0.04f && _speedNow > 0.05f)
            {
                var q = Quaternion.LookRotation(to.normalized, Vector3.up);
                float before = transform.eulerAngles.y;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, q, TurnRate * dt);
                _turn = Mathf.Lerp(_turn, Mathf.DeltaAngle(before, transform.eulerAngles.y) / dt, dt * 3f);
                transform.position += transform.forward * _speedNow * dt;
            }
            else { _turn = Mathf.Lerp(_turn, 0f, dt * 2f); if (Current == Mode.Hold) transform.position = Vector3.Lerp(transform.position, want, dt * 0.5f); }
            _phase += dt * WaveRate * (1f + _speedNow / Mathf.Max(1f, Speed)) * Mathf.PI * 2f * 0.5f;
            // the voice's loudness (the keeper speaks)
            float lvl = 0f;
            if (Voice != null && Voice.isPlaying) { Voice.GetOutputData(_buf, 0); foreach (var v in _buf) lvl = Mathf.Max(lvl, Mathf.Abs(v)); }
            Talk = Mathf.Lerp(Talk, Mathf.Clamp01(lvl * 3f), dt * 8f);
        }

        void LateUpdate()
        {
            if (_spine == null) return;
            var right = transform.right; var up = transform.up;
            for (int i = 0; i < 8; i++)
            {
                var b = _spine[i]; if (b == null) continue;
                b.localRotation = _rest[i];
                float k = (i + 1) / 8f;
                float pitch = Wave * k * k * Mathf.Sin(_phase - i * 0.75f);                         // the up-and-down beat, strongest at the flukes
                if (i == 0) pitch += -Talk * 9f + Mathf.Sin(_phase * 0.5f) * 1.5f;                   // the head nods with its voice
                float yaw = Mathf.Clamp(_turn * 0.06f, -6f, 6f) * (k - 0.3f);                         // the body bends into the turn
                b.rotation = Quaternion.AngleAxis(pitch, right) * Quaternion.AngleAxis(yaw, up) * b.rotation;
            }
            float fin = 8f * Mathf.Sin(_phase * 0.6f) + Talk * 14f;
            if (_finL != null) { _finL.localRotation = _restL; _finL.rotation = Quaternion.AngleAxis(fin, transform.forward) * _finL.rotation; }
            if (_finR != null) { _finR.localRotation = _restR; _finR.rotation = Quaternion.AngleAxis(-fin, transform.forward) * _finR.rotation; }
        }
    }
}
