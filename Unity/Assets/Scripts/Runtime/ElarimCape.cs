using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF10: the Elarim's cape never lets a leg pass through it. The cape hangs from two bones of its own (CapeA / CapeB,
    /// one per half, children of the upper chest; Tools/Blender/cleanup_r11.py). After the animation each frame, each half
    /// is swung backward about its pivot (around the body's left-right axis) by the smallest angle at which none of its
    /// vertices is inside a leg (thigh, shin, foot capsules of both legs, plus a margin) and none is below the ground -
    /// walking, kneeling and standing. It then settles back slowly (a soft, cloth-like follow) and sways a little with the
    /// walk. The cape vertices come from the mesh's bind pose (bone-local), so the test follows the real cape shape.
    /// Check runs read <see cref="WorstDepth"/> (deepest leg contact of any cape vertex after the swing, metres).
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class ElarimCape : MonoBehaviour
    {
        public float Margin = 0.035f;          // keep this far off the leg surface
        public float GroundSink = 1.4f;        // QF13: the hem pools on the ground (kneeling) instead of being swung up into a rod
        public float SettleDegPerSec = 140f;   // how fast a swung cape falls back (QF13: quick - no board left sticking out)
        public float MaxSwing = 80f;
        class Half { public Transform Bone; public Vector3[] Local; public float Angle; public string Name; public Quaternion Rest; public Vector3 RestPos; public bool Lower; }
        // ---- QF13 (3a): the cape's seat. In the model (and Kyle's reference) the cape's top sits just under the shoulder
        // line and its hem clears the ground; in the game the retargeted body carries the cape bones lower on his back, so
        // the hem reached the ground (and the ground / leg check then swung the whole cape back like a board). The bind
        // pose gives the reference: how far the cape's top is below the shoulders. Every frame (before the swing) the cape
        // is re-seated so its top is exactly there again - the height it has on the original model - whatever the clip does.
        float _refTopBelowShoulder = float.NaN, _refHemAboveFeet = float.NaN, _seat;
        /// <summary>Check runs: the cape's top below the shoulder line now vs the model, its hem above the ground now vs the
        /// model (m), and the re-seat shift applied (m, + = raised).</summary>
        public float TopBelowShoulder { get; private set; }
        public float RefTopBelowShoulder => _refTopBelowShoulder;
        public float HemAboveGround { get; private set; }
        public float RefHemAboveGround => _refHemAboveFeet;
        public float SeatShift => _seat;
        readonly List<Half> _halves = new List<Half>();
        Animator _anim; SkinnedMeshRenderer _smr;
        Vector3 _lastPos; float _sway;
        /// <summary>Deepest remaining leg contact of any sampled cape vertex this frame (m; 0 = clean).</summary>
        public float WorstDepth { get; private set; }
        public float MaxAngle { get { float m = 0f; foreach (var h in _halves) m = Mathf.Max(m, h.Angle); return m; } }
        public bool Ready => _halves.Count > 0;

        void Start()
        {
            _anim = GetComponentInChildren<Animator>();
            var smr = _smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr == null || smr.sharedMesh == null) return;
            var mesh = smr.sharedMesh; var bones = smr.bones; var bp = mesh.bindposes;
            if (!mesh.isReadable) Debug.LogWarning($"[Seven] {name}: cape mesh NOT readable - the cape cannot see its vertices (QF13: Read/Write must be on)");
            var verts = mesh.vertices; var w = mesh.boneWeights;
            for (int b = 0; b < bones.Length; b++)
            {
                if (bones[b] == null || !(bones[b].name.EndsWith("CapeA") || bones[b].name.EndsWith("CapeB") || bones[b].name.EndsWith("CapeA2") || bones[b].name.EndsWith("CapeB2"))) continue;   // QF13: + the lower halves (below the hips)
                var pts = new List<Vector3>();
                for (int i = 0; i < verts.Length; i++)
                {
                    var bw = w[i];
                    float k = bw.boneIndex0 == b ? bw.weight0 : bw.boneIndex1 == b ? bw.weight1 : bw.boneIndex2 == b ? bw.weight2 : bw.boneIndex3 == b ? bw.weight3 : 0f;
                    if (k < 0.45f) continue;
                    pts.Add(bp[b].MultiplyPoint3x4(verts[i]));
                }
                // thin out to ~260 points, keeping the lowest ones (the hem) and an even spread
                var sel = new List<Vector3>();
                int step = Mathf.Max(1, pts.Count / 260);
                for (int i = 0; i < pts.Count; i += step) sel.Add(pts[i]);
                _halves.Add(new Half { Bone = bones[b], Local = sel.ToArray(), Name = bones[b].name, Rest = bones[b].localRotation, RestPos = bones[b].localPosition, Lower = bones[b].name.EndsWith("2") });
            }
            _halves.Sort((x, y) => x.Lower.CompareTo(y.Lower));            // the upper halves first: the lower ones hang from them
            // the reference seat from the bind pose (mesh space): shoulders = the upper-arm bones' bind positions
            {
                // mesh space is not Y-up for this FBX: heights are taken in the world, through the renderer's own transform (bind pose)
                var M0 = smr.transform.localToWorldMatrix;
                float sh = 0f; int n = 0; float top = -1e9f, hem = 1e9f, feet = 1e9f;
                for (int b = 0; b < bones.Length; b++)
                    if (bones[b] != null && (bones[b].name.EndsWith("LeftArm") || bones[b].name.EndsWith("RightArm"))) { sh += M0.MultiplyPoint3x4(bp[b].inverse.MultiplyPoint3x4(Vector3.zero)).y; n++; }
                foreach (var hf in _halves)
                {
                    int bi = System.Array.IndexOf(bones, hf.Bone); var inv = bp[bi].inverse;
                    foreach (var l in hf.Local) { float y = M0.MultiplyPoint3x4(inv.MultiplyPoint3x4(l)).y; top = Mathf.Max(top, y); hem = Mathf.Min(hem, y); }
                }
                foreach (var v in verts) feet = Mathf.Min(feet, M0.MultiplyPoint3x4(v).y);
                float sc = Mathf.Max(1e-4f, transform.lossyScale.y);
                if (n > 0 && _halves.Count > 0) { _refTopBelowShoulder = (sh / n - top) / sc; _refHemAboveFeet = (hem - feet) / sc; }
                Debug.Log($"[Seven] {name} cape seat (QF13 3a): model - top {_refTopBelowShoulder:0.00} m below the shoulders, hem {_refHemAboveFeet:0.00} m above the feet (mesh units)");
            }
            _lastPos = transform.position;
            if (_halves.Count == 0) Debug.Log("[Seven] ElarimCape: no cape bones on " + name + " (old export?)");
        }

        struct Cap { public Vector3 A, B; public float R; }
        readonly List<Cap> _caps = new List<Cap>();
        void Legs()
        {
            _caps.Clear();
            if (_anim == null || !_anim.isHuman) return;
            float s = transform.lossyScale.y;
            foreach (var side in new[] { (HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes),
                                         (HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, HumanBodyBones.RightToes) })
            {
                var up = _anim.GetBoneTransform(side.Item1); var kn = _anim.GetBoneTransform(side.Item2);
                var ft = _anim.GetBoneTransform(side.Item3); var to = _anim.GetBoneTransform(side.Item4);
                if (up == null || kn == null || ft == null) continue;
                _caps.Add(new Cap { A = up.position, B = kn.position, R = 0.19f * s });          // thigh (armoured, 3.5 m figure)
                _caps.Add(new Cap { A = kn.position, B = ft.position, R = 0.15f * s });          // shin + greave
                var toe = to != null ? to.position + (to.position - ft.position) * 0.6f : ft.position + transform.forward * 0.25f * s;
                _caps.Add(new Cap { A = ft.position, B = toe, R = 0.11f * s });                  // foot
            }
        }

        static float SegDist(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            return (p - (a + ab * t)).magnitude;
        }

        /// <summary>Deepest penetration of this half if swung by angle (0 = clear).</summary>
        float Depth(Half h, float angle, Vector3 axis, float ground)
        {
            var piv = h.Bone.position; var q = Quaternion.AngleAxis(angle, axis);
            var M = h.Bone.localToWorldMatrix;
            float worst = 0f;
            for (int i = 0; i < h.Local.Length; i++)
            {
                var p = piv + q * (M.MultiplyPoint3x4(h.Local[i]) - piv);
                float g = ground - GroundSink - p.y; if (g > worst) worst = g;      // QF13: the hem may rest on the ground (kneeling), not dig in
                for (int c = 0; c < _caps.Count; c++)
                {
                    float d = _caps[c].R + Margin - SegDist(p, _caps[c].A, _caps[c].B);
                    if (d > worst) worst = d;
                }
            }
            return worst;
        }

        /// <summary>QF13 (17): the cape hangs from the shoulders with gravity - whatever the spine does (the walk's lean, the
        /// kneel), each half is turned about the body's left-right axis so its hem hangs straight down below its pivot
        /// (before, it followed the chest rigidly: a board sticking out behind him when he knelt).</summary>
        void Hang(Half h, Vector3 axis)
        {
            if (h.Local.Length == 0) return;
            var M = h.Bone.localToWorldMatrix; var piv = h.Bone.position;
            Vector3 hem = Vector3.zero; int n = 0; float lo = float.MaxValue;
            foreach (var l in h.Local) { var p = M.MultiplyPoint3x4(l); if (p.y < lo) lo = p.y; }
            foreach (var l in h.Local) { var p = M.MultiplyPoint3x4(l); if (p.y < lo + 0.25f) { hem += p; n++; } }
            if (n == 0) return;
            var d = Vector3.ProjectOnPlane(hem / n - piv, axis); var down = Vector3.ProjectOnPlane(Vector3.down, axis);
            if (d.sqrMagnitude < 1e-4f) return;
            float ang = Vector3.SignedAngle(d, down, axis);
            h.Bone.rotation = Quaternion.AngleAxis(Mathf.Clamp(ang, -60f, 60f), axis) * h.Bone.rotation;
        }

        void Reseat()
        {
            if (float.IsNaN(_refTopBelowShoulder) || _anim == null || !_anim.isHuman) return;
            var la = _anim.GetBoneTransform(HumanBodyBones.LeftUpperArm); var ra = _anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (la == null || ra == null) return;
            float s = transform.lossyScale.y;
            float shoulder = (la.position.y + ra.position.y) * 0.5f, top = -1e9f, hem = 1e9f;
            foreach (var h in _halves) { var M = h.Bone.localToWorldMatrix; foreach (var l in h.Local) { float y = M.MultiplyPoint3x4(l).y; if (y > top) top = y; if (y < hem) hem = y; } }
            float want = shoulder - _refTopBelowShoulder * s;
            float shift = Mathf.Clamp(want - top, -0.4f * s, 1.2f * s);
            _seat = Mathf.Abs(_seat - shift) > 0.3f * s ? shift : Mathf.Lerp(_seat, shift, Mathf.Min(1f, Time.deltaTime * 8f));
            foreach (var h in _halves) if (!h.Lower) h.Bone.position += Vector3.up * _seat;
            float ground = Ground.Height(transform.position.x, transform.position.z);
            TopBelowShoulder = (shoulder - (top + _seat)) / s; HemAboveGround = (hem + _seat - ground) / s;
        }

        void LateUpdate()
        {
            if (_halves.Count == 0) return;
            if (_smr != null && !_smr.isVisible && DemoBoot.CaptureDir == null) return;     // nobody sees it: no work
            Legs();
            var axis = transform.right;
            float ground = Ground.Height(transform.position.x, transform.position.z);
            var v = (transform.position - _lastPos) / Mathf.Max(1e-4f, Time.deltaTime); _lastPos = transform.position;
            float speed = Vector3.Dot(v, transform.forward);
            _sway = Mathf.Lerp(_sway, Mathf.Clamp(speed, 0f, 3f) * 4f + Mathf.Sin(Time.time * 3.1f) * Mathf.Clamp01(speed) * 1.5f, Time.deltaTime * 3f);
            WorstDepth = 0f;
            // the cape bones are not humanoid bones - the animation never resets them: start from their rest pose every frame
            foreach (var h in _halves) { h.Bone.localRotation = h.Rest; h.Bone.localPosition = h.RestPos; }
            Reseat();
            foreach (var h in _halves) Hang(h, axis);
            foreach (var h in _halves)
            {
                // the smallest swing that clears the legs and the ground (coarse 4-degree steps, then 1-degree refine)
                float need = 0f;
                if (Depth(h, 0f, axis, ground) > 0f)
                {
                    float a = 4f;
                    while (a < MaxSwing && Depth(h, a, axis, ground) > 0f) a += 4f;
                    float lo = Mathf.Max(0f, a - 4f);
                    for (float b = lo + 1f; b <= a; b += 1f) if (Depth(h, b, axis, ground) <= 0f) { a = b; break; }
                    need = Mathf.Min(a, MaxSwing);
                }
                need += _sway;
                // never less than needed (no clipping, ever); falls back slowly when the legs move away
                h.Angle = need >= h.Angle ? need : Mathf.Max(need, h.Angle - SettleDegPerSec * Time.deltaTime);
                if (h.Angle > 0.01f) h.Bone.rotation = Quaternion.AngleAxis(h.Angle, axis) * h.Bone.rotation;
                WorstDepth = Mathf.Max(WorstDepth, Depth(h, 0f, axis, ground));
            }
        }
    }
}
