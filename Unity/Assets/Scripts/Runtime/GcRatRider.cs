using UnityEngine;

namespace Seven
{
    /// <summary>QF37: Ratwing seated in the Jetstream's open cockpit (Dan: "Ratwing in the Jetstream cockpit"). His rig is generic (not
    /// Humanoid), so the seat is made on his bones after the animator, in the jet's frame: his hips on the cockpit seat, the spine
    /// upright with a slight forward hunch, the thighs forward and the shins down into the footwell, the arms reaching forward so his
    /// claws rest on the controls (two points ahead of the seat), the head up behind the windscreen. RatwingRig (order 70, before this)
    /// folds the wings along his sides (perched) and keeps his twitchy head / shoulder life and the jaw with his voice.
    /// Check runs read <see cref="HipsGap"/> / <see cref="ClawGaps"/> (GC RIDE POSE lines, as for the riders astride).</summary>
    [DefaultExecutionOrder(80)]   // after RatwingRig (70): its shoulder roll / head life first, then the seat and the reach on top
    public class GcRatRider : MonoBehaviour
    {
        public Transform Model, Seat, Jet;
        public float HipsGap { get; private set; } = 1f;
        public Vector2 ClawGaps { get; private set; }
        Transform _hips, _spine, _chest, _neck, _head; Transform[] _thigh = new Transform[2], _shin = new Transform[2], _foot = new Transform[2], _toes = new Transform[2], _upper = new Transform[2], _fore = new Transform[2], _hand = new Transform[2];
        RatwingRig _rig;
        void Awake()
        {
            Transform F(string n) { foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == n) return t; return null; }
            _hips = F("hips"); _spine = F("spine"); _chest = F("chest"); _neck = F("neck"); _head = F("head");
            string[] sd = { "L", "R" };
            for (int i = 0; i < 2; i++)
            {
                _thigh[i] = F("thigh." + sd[i]); _shin[i] = F("shin." + sd[i]); _foot[i] = F("foot." + sd[i]); _toes[i] = F("toes." + sd[i]);
                _upper[i] = F("upper_arm." + sd[i]); _fore[i] = F("forearm." + sd[i]); _hand[i] = F("hand." + sd[i]);
            }
            _rig = GetComponent<RatwingRig>();
            if (_rig != null) { _rig.Now = RatwingRig.Mode.Perched; _rig.LookUp = 0.18f; }
        }
        static void Aim(Transform bone, Transform child, Vector3 dir)
        {
            if (bone == null || child == null) return;
            var cur = child.position - bone.position; if (cur.sqrMagnitude < 1e-8f) return;
            bone.rotation = Quaternion.FromToRotation(cur, dir) * bone.rotation;
        }
        /// <summary>The control points (left, right) in world space: ahead of the seat, at his chest's height.</summary>
        public Vector3 Control(int side)
        {
            float s = transform.lossyScale.x; var f = Seat.forward; var u = Seat.up; var r = Seat.right;
            return Seat.position + (f * 0.62f + u * 0.62f + r * (side == 0 ? -0.2f : 0.2f)) * s;
        }
        void LateUpdate()
        {
            if (Seat == null || _hips == null) return;
            if (_rig != null) _rig.Now = RatwingRig.Mode.Perched;
            float s = transform.lossyScale.x; var f = Seat.forward; var u = Seat.up; var r = Seat.right;
            // the body onto the seat: a translation of the model (the bones keep the animator's pose), the pelvis a little above the cushion
            var hipsWant = Seat.position + u * 0.1f * s;
            if (Model != null) Model.position += hipsWant - _hips.position;
            // the spine upright with a slight forward hunch, the neck up, the head level and ahead
            Aim(_hips, _spine, u * 0.96f + f * 0.12f);
            Aim(_spine, _chest, u * 0.93f + f * 0.25f);
            Aim(_chest, _neck, u * 0.9f + f * 0.32f);
            Aim(_neck, _head, u * 0.85f + f * 0.45f);
            for (int i = 0; i < 2; i++)
            {
                float sg = i == 0 ? -1f : 1f;
                // the legs: thighs forward along the seat, the shins down into the footwell, the feet flat ahead
                Aim(_thigh[i], _shin[i], f * 0.97f - u * 0.1f + r * sg * 0.12f);
                Aim(_shin[i], _foot[i], -u * 0.9f + f * 0.3f);
                Aim(_foot[i], _toes[i], f * 0.95f - u * 0.2f);
                // the arms: the upper arm forward and down, the forearm and the claws to the controls
                // two-bone reach: the claw tip (wrist + the hand along the forearm) onto the control point, the elbow bent down and out
                var c = Control(i);
                if (_upper[i] != null && _fore[i] != null && _hand[i] != null)
                {
                    float handLen = _hand[i].childCount > 0 ? Vector3.Distance(_hand[i].position, _hand[i].GetChild(0).position) : 0.3f * s;
                    float l1 = Vector3.Distance(_upper[i].position, _fore[i].position), l2 = Vector3.Distance(_fore[i].position, _hand[i].position) + handLen;
                    var d = c - _upper[i].position; float dl = Mathf.Clamp(d.magnitude, Mathf.Abs(l1 - l2) + 1e-3f, l1 + l2 - 1e-3f); var dn = d.normalized;
                    float cosA = Mathf.Clamp((l1 * l1 + dl * dl - l2 * l2) / (2f * l1 * dl), -1f, 1f), sinA = Mathf.Sqrt(1f - cosA * cosA);
                    var hint = Vector3.ProjectOnPlane(-u * 0.7f + r * sg * 0.55f - f * 0.15f, dn).normalized;
                    Aim(_upper[i], _fore[i], dn * cosA + hint * sinA);
                    Aim(_fore[i], _hand[i], c - _fore[i].position);
                    if (_hand[i].childCount > 0) Aim(_hand[i], _hand[i].GetChild(0), c - _hand[i].position);
                }
            }
            HipsGap = Vector3.Distance(_hips.position, hipsWant);
            // the claws: the forearm points through the control point; the gap is how far the claw tip (the wrist + the hand's length
            // along the forearm) ends from it - short of it or past it
            var gaps = new float[2];
            for (int i = 0; i < 2; i++)
            {
                if (_fore[i] == null || _hand[i] == null) { gaps[i] = 1f; continue; }
                float handLen = _hand[i].childCount > 0 ? Vector3.Distance(_hand[i].position, _hand[i].GetChild(0).position) : 0.3f * s;
                var tip = _hand[i].childCount > 0 ? _hand[i].GetChild(0).position : _hand[i].position + (_hand[i].position - _fore[i].position).normalized * handLen;
                gaps[i] = Vector3.Distance(tip, Control(i));
            }
            ClawGaps = new Vector2(gaps[0], gaps[1]);
        }
    }
}
