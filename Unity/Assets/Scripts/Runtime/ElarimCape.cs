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
        public float SettleDegPerSec = 40f;    // how fast a swung cape falls back
        public float MaxSwing = 80f;
        class Half { public Transform Bone; public Vector3[] Local; public float Angle; public string Name; public Quaternion Rest; }
        readonly List<Half> _halves = new List<Half>();
        Animator _anim; SkinnedMeshRenderer _smr;
        Vector3 _lastPos; float _sway;
        /// <summary>Deepest remaining leg contact of any sampled cape vertex this frame (m; 0 = clean).</summary>
        public float WorstDepth { get; private set; }
        public float MaxAngle => _halves.Count == 0 ? 0f : Mathf.Max(_halves[0].Angle, _halves.Count > 1 ? _halves[1].Angle : 0f);
        public bool Ready => _halves.Count > 0;

        void Start()
        {
            _anim = GetComponentInChildren<Animator>();
            var smr = _smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr == null || smr.sharedMesh == null) return;
            var mesh = smr.sharedMesh; var bones = smr.bones; var bp = mesh.bindposes;
            var verts = mesh.vertices; var w = mesh.boneWeights;
            for (int b = 0; b < bones.Length; b++)
            {
                if (bones[b] == null || !(bones[b].name.EndsWith("CapeA") || bones[b].name.EndsWith("CapeB"))) continue;
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
                _halves.Add(new Half { Bone = bones[b], Local = sel.ToArray(), Name = bones[b].name, Rest = bones[b].localRotation });
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
                float g = ground + 0.02f - p.y; if (g > worst) worst = g;
                for (int c = 0; c < _caps.Count; c++)
                {
                    float d = _caps[c].R + Margin - SegDist(p, _caps[c].A, _caps[c].B);
                    if (d > worst) worst = d;
                }
            }
            return worst;
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
            foreach (var h in _halves) h.Bone.localRotation = h.Rest;
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
