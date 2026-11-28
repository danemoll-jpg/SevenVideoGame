using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF43 (8): the Elarim's cape - attached at the shoulders and flowing. Dan: "they hang below the shoulders and are very rigid".
    /// QF10-QF13's cape hung on two (then four) bones of its own and was swung like a board to clear the legs. Now, at load, the
    /// cape part of the skinned mesh (the vertices weighted to the CapeA / CapeB / CapeA2 / CapeB2 bones of cleanup_r11.py) is
    /// re-skinned onto a cloth grid of its own - Rows x Cols points spread over the cape (top edge to hem, left to right):
    ///  - the attachment: the whole cape is moved up so its top edge sits ON the shoulder line (a collar across the upper back:
    ///    no gap, no sag below the shoulders), and the top row is pinned to the upper chest bone - it moves with the body;
    ///  - the flow: the rows below are a verlet cloth (gravity, damping, distance links down / across / two rows down for a soft
    ///    stiffness), with the body's own motion, a little breeze, swaying with walking, turning and gestures and settling when
    ///    they stand;
    ///  - never through the body or the legs: every point is pushed out of capsules round the torso (hips -> chest), the thighs,
    ///    shins and feet, and kept above the ground (the hem may rest on it when kneeling).
    /// Each grid point is a bone (bilinear weights, up to four per vertex); its rotation follows the body (the chest's turn) and
    /// the cloth's bend, so the cape's thickness turns with him. Check runs read <see cref="WorstDepth"/> (deepest body / leg
    /// contact after the solve), <see cref="TopBelowShoulder"/> (the top edge's height under the shoulder line) and
    /// <see cref="MaxAngle"/> (the hem's swing from hanging straight down, degrees).
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class ElarimCape : MonoBehaviour
    {
        public const int Rows = 5, Cols = 3;
        public float Margin = 0.03f;
        public float Damping = 0.06f;           // per-frame velocity loss (60 fps reference)
        public float Breeze = 0.35f;            // m/s^2 of slow gusting wind
        class P { public Transform Bone; public Vector3 Rest, Pos, Prev; public Vector3 BindPos; public int R, C; }
        readonly P[] _p = new P[Rows * Cols];
        Animator _anim; SkinnedMeshRenderer _smr; Transform _chest, _hips, _neck;
        Vector3[] _pinLocal = new Vector3[Cols];         // top row in the chest bone's space
        Quaternion _chestBindRot; float _s = 1f;
        float[,] _restDown, _restAcross;                 // link rest lengths
        bool _ready;
        public bool Ready => _ready;
        public float WorstDepth { get; private set; }
        public float MaxAngle { get; private set; }
        public float TopBelowShoulder { get; private set; }
        public float RefTopBelowShoulder => 0f;
        public float HemAboveGround { get; private set; }
        public float RefHemAboveGround { get; private set; }
        public float SeatShift { get; private set; }

        void Start()
        {
            _anim = GetComponentInChildren<Animator>();
            _smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (_smr == null || _smr.sharedMesh == null || _anim == null || !_anim.isHuman) { Debug.Log("[Seven] ElarimCape: no skinned humanoid on " + name); return; }
            var src = _smr.sharedMesh;
            if (!src.isReadable) { Debug.LogWarning($"[Seven] {name}: cape mesh NOT readable (Read/Write must be on) - the cape stays as modelled"); return; }
            var bones = _smr.bones; var bp = src.bindposes;
            _chest = _anim.GetBoneTransform(HumanBodyBones.UpperChest) ?? _anim.GetBoneTransform(HumanBodyBones.Chest);
            _hips = _anim.GetBoneTransform(HumanBodyBones.Hips); _neck = _anim.GetBoneTransform(HumanBodyBones.Neck);
            int chestI = System.Array.IndexOf(bones, _chest);
            if (_chest == null || chestI < 0) { Debug.Log("[Seven] ElarimCape: no chest bone in the skin on " + name); return; }
            var capeIdx = new HashSet<int>();
            for (int b = 0; b < bones.Length; b++) if (bones[b] != null && (bones[b].name.EndsWith("CapeA") || bones[b].name.EndsWith("CapeB") || bones[b].name.EndsWith("CapeA2") || bones[b].name.EndsWith("CapeB2"))) capeIdx.Add(b);
            if (capeIdx.Count == 0) { Debug.Log("[Seven] ElarimCape: no cape bones on " + name + " (old export?)"); return; }
            var verts = src.vertices; var w = src.boneWeights;
            // bind space -> world at bind: the renderer's transform (the skin's mesh space)
            var M0 = _smr.transform.localToWorldMatrix;
            var chestBind = M0 * bp[chestI].inverse;
            _chestBindRot = chestBind.rotation;
            Vector3 BoneBindPos(HumanBodyBones hb) { var t = _anim.GetBoneTransform(hb); int i = t != null ? System.Array.IndexOf(bones, t) : -1; return i >= 0 ? (M0 * bp[i].inverse).MultiplyPoint3x4(Vector3.zero) : Vector3.zero; }
            var lArm = BoneBindPos(HumanBodyBones.LeftUpperArm); var rArm = BoneBindPos(HumanBodyBones.RightUpperArm);
            float shoulderY = (lArm.y + rArm.y) * 0.5f;
            var side = (rArm - lArm); side.y = 0f; side = side.sqrMagnitude > 1e-6f ? side.normalized : Vector3.right;
            // the cape's vertices (mostly weighted to a cape bone) in bind world
            var cape = new List<int>(); var cw = new List<Vector3>();
            for (int i = 0; i < verts.Length; i++)
            {
                var bw = w[i]; float k = 0f;
                if (capeIdx.Contains(bw.boneIndex0)) k += bw.weight0; if (capeIdx.Contains(bw.boneIndex1)) k += bw.weight1;
                if (capeIdx.Contains(bw.boneIndex2)) k += bw.weight2; if (capeIdx.Contains(bw.boneIndex3)) k += bw.weight3;
                if (k < 0.5f) continue;
                cape.Add(i); cw.Add(M0.MultiplyPoint3x4(verts[i]));
            }
            if (cape.Count < 20) { Debug.Log($"[Seven] ElarimCape: only {cape.Count} cape vertices on {name}"); return; }
            float top = -1e9f, hem = 1e9f, lat0 = 1e9f, lat1 = -1e9f; var mid = Vector3.zero;
            foreach (var p in cw) { top = Mathf.Max(top, p.y); hem = Mathf.Min(hem, p.y); mid += p; }
            mid /= cw.Count;
            foreach (var p in cw) { float l = Vector3.Dot(p - mid, side); lat0 = Mathf.Min(lat0, l); lat1 = Mathf.Max(lat1, l); }
            _s = Mathf.Max(1e-3f, transform.lossyScale.y);
            // the attachment: up so the top edge is ON the shoulder line (a hair under it: the collar lies across the upper back)
            float lift = shoulderY - 0.02f * _s - top;
            SeatShift = lift / _s;
            for (int i = 0; i < cw.Count; i++) cw[i] += Vector3.up * lift;
            top += lift; hem += lift;
            // the grid: row = height fraction from the top, col = across; each point's rest = the mean of the vertices round it
            float H = Mathf.Max(0.1f, top - hem), W = Mathf.Max(0.1f, lat1 - lat0);
            var sum = new Vector3[Rows * Cols]; var cnt = new int[Rows * Cols];
            float RowOf(Vector3 p) => Mathf.Clamp01((top - p.y) / H) * (Rows - 1);
            float ColOf(Vector3 p) => Mathf.Clamp01((Vector3.Dot(p - mid, side) - lat0) / W) * (Cols - 1);
            foreach (var p in cw) { int r = Mathf.RoundToInt(RowOf(p)), c = Mathf.RoundToInt(ColOf(p)); sum[r * Cols + c] += p; cnt[r * Cols + c]++; }
            var newBones = new List<Transform>(bones); var newBind = new List<Matrix4x4>(bp);
            var gridRoot = new GameObject("CapeCloth").transform; gridRoot.SetParent(transform, false);
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    int k = r * Cols + c;
                    Vector3 rest = cnt[k] > 0 ? sum[k] / cnt[k] : new Vector3(0f, top - H * r / (Rows - 1), 0f) + side * (lat0 + W * c / (Cols - 1)) + Vector3.Scale(mid, new Vector3(1f, 0f, 1f));
                    if (cnt[k] == 0) rest = mid + side * (lat0 + W * c / (Cols - 1) - Vector3.Dot(Vector3.zero, side)) + Vector3.up * (top - H * r / (Rows - 1) - mid.y);
                    var go = new GameObject($"Cloth_{r}_{c}").transform; go.SetParent(gridRoot, false);
                    go.SetPositionAndRotation(rest, _chestBindRot);
                    _p[k] = new P { Bone = go, Rest = rest, BindPos = rest, Pos = rest, Prev = rest, R = r, C = c };
                    newBones.Add(go);
                    newBind.Add(go.worldToLocalMatrix * M0);          // bind: mesh space -> the point's space at rest
                }
            // the top row pinned to the chest (in its space, at bind)
            var chestInv = chestBind.inverse;
            for (int c = 0; c < Cols; c++) _pinLocal[c] = chestInv.MultiplyPoint3x4(_p[c].Rest);
            // re-skin: the cape's vertices to the grid (bilinear: up to 4 points), lifted to the shoulder line
            var mesh = Instantiate(src); mesh.name = src.name + "_cloth";
            var nv = mesh.vertices; var nw = mesh.boneWeights; var invM0 = M0.inverse;
            int baseIdx = bones.Length;
            for (int j = 0; j < cape.Count; j++)
            {
                int i = cape[j]; var p = cw[j];
                nv[i] = invM0.MultiplyPoint3x4(p);
                float rf = RowOf(p), cf = ColOf(p);
                int r0 = Mathf.Min(Rows - 2, Mathf.FloorToInt(rf)), c0 = Mathf.Min(Cols - 2, Mathf.FloorToInt(cf));
                float ur = Mathf.Clamp01(rf - r0), uc = Mathf.Clamp01(cf - c0);
                var bw = new BoneWeight
                {
                    boneIndex0 = baseIdx + r0 * Cols + c0, weight0 = (1f - ur) * (1f - uc),
                    boneIndex1 = baseIdx + r0 * Cols + c0 + 1, weight1 = (1f - ur) * uc,
                    boneIndex2 = baseIdx + (r0 + 1) * Cols + c0, weight2 = ur * (1f - uc),
                    boneIndex3 = baseIdx + (r0 + 1) * Cols + c0 + 1, weight3 = ur * uc,
                };
                nw[i] = bw;
            }
            mesh.vertices = nv; mesh.boneWeights = nw; mesh.bindposes = newBind.ToArray(); mesh.RecalculateBounds();
            _smr.sharedMesh = mesh; _smr.bones = newBones.ToArray(); _smr.updateWhenOffscreen = true;
            // link rest lengths
            _restDown = new float[Rows, Cols]; _restAcross = new float[Rows, Cols];
            for (int r = 0; r < Rows; r++) for (int c = 0; c < Cols; c++)
                {
                    if (r > 0) _restDown[r, c] = Vector3.Distance(At(r, c).Rest, At(r - 1, c).Rest);
                    if (c > 0) _restAcross[r, c] = Vector3.Distance(At(r, c).Rest, At(r, c - 1).Rest);
                }
            RefHemAboveGround = (hem - transform.position.y) / _s;
            // start the cloth where the body is now (not at the bind pose's place)
            Settle();
            _ready = true;
            Debug.Log($"[Seven] {name} cape (QF43 8): {cape.Count} cape vertices re-skinned onto a {Rows}x{Cols} cloth; top edge lifted {SeatShift:+0.00;-0.00} m onto the shoulder line, pinned to {_chest.name}");
        }
        P At(int r, int c) => _p[r * Cols + c];

        void Settle()
        {
            var chestRot = _chest.rotation * Quaternion.Inverse(_chestBindRot);
            var anchor = _chest.localToWorldMatrix;
            var pin0 = anchor.MultiplyPoint3x4(_pinLocal[Cols / 2]);
            var restPin = _p[Cols / 2].Rest;
            foreach (var p in _p) { var q = pin0 + chestRot * (p.Rest - restPin); p.Pos = p.Prev = q; }
        }

        struct Cap { public Vector3 A, B; public float R; }
        readonly List<Cap> _caps = new List<Cap>();
        void Body()
        {
            _caps.Clear();
            float s = _s;
            if (_hips != null && _chest != null) _caps.Add(new Cap { A = _hips.position, B = (_neck != null ? _neck.position : _chest.position), R = 0.24f * s });
            foreach (var side in new[] { (HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes),
                                         (HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, HumanBodyBones.RightToes) })
            {
                var up = _anim.GetBoneTransform(side.Item1); var kn = _anim.GetBoneTransform(side.Item2);
                var ft = _anim.GetBoneTransform(side.Item3); var to = _anim.GetBoneTransform(side.Item4);
                if (up == null || kn == null || ft == null) continue;
                _caps.Add(new Cap { A = up.position, B = kn.position, R = 0.19f * s });
                _caps.Add(new Cap { A = kn.position, B = ft.position, R = 0.15f * s });
                var toe = to != null ? to.position + (to.position - ft.position) * 0.6f : ft.position + transform.forward * 0.25f * s;
                _caps.Add(new Cap { A = ft.position, B = toe, R = 0.11f * s });
            }
        }
        static Vector3 Closest(Vector3 p, Vector3 a, Vector3 b) { var ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); return a + ab * t; }

        void LateUpdate()
        {
            if (!_ready) return;
            float dt = Mathf.Clamp(Time.deltaTime, 1e-4f, 0.05f);
            if (_smr != null && !_smr.isVisible && DemoBoot.CaptureDir == null) { Settle(); ApplyBones(); return; }
            Body();
            var chestRot = _chest.rotation * Quaternion.Inverse(_chestBindRot);
            var anchor = _chest.localToWorldMatrix;
            // a pose jump (a placement / a cut): start the cloth again where the body is
            if ((anchor.MultiplyPoint3x4(_pinLocal[Cols / 2]) - At(0, Cols / 2).Pos).magnitude > 1.5f * _s) Settle();
            float ground = Ground.Height(transform.position.x, transform.position.z);
            if (float.IsNaN(ground)) ground = transform.position.y;
            var wind = (transform.right * Mathf.Sin(Time.time * 0.7f) * 0.6f - transform.forward * (0.6f + 0.4f * Mathf.Sin(Time.time * 0.43f))) * Breeze;
            float damp = Mathf.Pow(1f - Damping, dt * 60f);
            // integrate (the top row is pinned)
            for (int i = 0; i < _p.Length; i++)
            {
                var p = _p[i];
                if (p.R == 0) { p.Prev = p.Pos; p.Pos = anchor.MultiplyPoint3x4(_pinLocal[p.C]); continue; }
                var v = (p.Pos - p.Prev) * damp; p.Prev = p.Pos;
                p.Pos += v + (Physics.gravity + wind) * dt * dt;
            }
            // constraints: links (down, across, two-down for soft stiffness), then the body / legs / ground
            for (int it = 0; it < 6; it++)
            {
                for (int r = 1; r < Rows; r++)
                    for (int c = 0; c < Cols; c++)
                    {
                        Link(At(r, c), At(r - 1, c), _restDown[r, c], r - 1 == 0 ? 1f : 0.5f);
                        if (c > 0) Link(At(r, c), At(r, c - 1), _restAcross[r, c], 0.5f);
                        if (r > 1) Link(At(r, c), At(r - 2, c), _restDown[r, c] + _restDown[r - 1, c], r - 2 == 0 ? 0.6f : 0.25f, true);
                    }
                foreach (var p in _p)
                {
                    if (p.R == 0) continue;
                    foreach (var cp in _caps)
                    {
                        var q = Closest(p.Pos, cp.A, cp.B); var d = p.Pos - q; float m = d.magnitude, need = cp.R + Margin * _s;
                        if (m < need) p.Pos = q + (m > 1e-5f ? d / m : -transform.forward) * need;
                    }
                    if (p.Pos.y < ground + 0.02f) p.Pos.y = ground + 0.02f;
                }
            }
            // measures
            WorstDepth = 0f;
            foreach (var p in _p) foreach (var cp in _caps) { float d = cp.R - (p.Pos - Closest(p.Pos, cp.A, cp.B)).magnitude; if (d > WorstDepth) WorstDepth = d; }
            var la = _anim.GetBoneTransform(HumanBodyBones.LeftUpperArm); var ra = _anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
            float topY = -1e9f, hemY = 1e9f; for (int c = 0; c < Cols; c++) { topY = Mathf.Max(topY, At(0, c).Pos.y); hemY = Mathf.Min(hemY, At(Rows - 1, c).Pos.y); }
            if (la != null && ra != null) TopBelowShoulder = ((la.position.y + ra.position.y) * 0.5f - topY) / _s;
            HemAboveGround = (hemY - ground) / _s;
            var hang = At(Rows - 1, Cols / 2).Pos - At(0, Cols / 2).Pos;
            MaxAngle = Vector3.Angle(hang, Vector3.down);
            ApplyBones(chestRot);
        }
        void Link(P a, P b, float rest, float wb, bool onlyStretch = false)
        {
            var d = a.Pos - b.Pos; float m = d.magnitude; if (m < 1e-6f) return;
            if (onlyStretch && m < rest * 0.92f) { /* a soft bend limit: only stops folding up */ rest *= 0.92f; }
            else if (onlyStretch) return;
            float diff = (m - rest) / m;
            bool pinned = b.R == 0;
            if (pinned) a.Pos -= d * diff;
            else { a.Pos -= d * diff * (1f - wb); b.Pos += d * diff * wb; }
        }
        void ApplyBones(Quaternion? chestRotIn = null)
        {
            var chestRot = chestRotIn ?? _chest.rotation * Quaternion.Inverse(_chestBindRot);
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    var p = At(r, c);
                    // the body's turn, then the cloth's bend (the rest direction toward the row below -> the current one)
                    var rot = chestRot * _chestBindRot;
                    int rn = Mathf.Min(Rows - 1, r + 1), rp = Mathf.Max(0, r - 1);
                    if (rn != rp)
                    {
                        var restDir = chestRot * (At(rn, c).Rest - At(rp, c).Rest); var nowDir = At(rn, c).Pos - At(rp, c).Pos;
                        if (restDir.sqrMagnitude > 1e-6f && nowDir.sqrMagnitude > 1e-6f) rot = Quaternion.FromToRotation(restDir, nowDir) * rot;
                    }
                    p.Bone.SetPositionAndRotation(p.Pos, rot);
                }
        }
    }
}
