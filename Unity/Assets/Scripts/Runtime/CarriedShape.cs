using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF5 (Dan: "we don't see her taking the dodecahedron with her, which she should"): the hand-held dodecahedron
    /// stays visibly with 7 after she takes it - held in her right hand, or riding in a small open pouch at the right
    /// side of her belt (the glowing edges show above the rim, plus a faint white glow), through the rest of Chapter 1 and
    /// into the galley, where she takes it out again. Lives on 7's root; positions are set after the animation
    /// (LateUpdate), so it follows every clip. Strictly monochrome (white emission, grey pouch).
    /// </summary>
    [DefaultExecutionOrder(200)]   // QF8: after 7's wall push and the IK, so it sits in the hand as rendered
    public class CarriedShape : MonoBehaviour
    {
        /// <summary>QF7: Pocket = out of sight in a pocket of her suit (no shape, no pouch, no light, no glow at all).</summary>
        public enum Where { Hand, Hip, Away, Pocket, HandLeft }
        public Transform Shape;
        public Where At { get; private set; } = Where.Away;
        [Tooltip("Pouch on the belt, in 7's root space from the hips bone: right, up, forward (m).")]
        public Vector3 HipOffset = new Vector3(0.215f, 0.06f, -0.03f);
        public float GlowAtHip = 0.35f, GlowInHand = 0.4f;     // QF7: faint edge lines only (no bloom halo)

        Animator _anim;
        Transform _hand, _mid, _hips;
        // QF8: palm frames of both hands (wrist, index / middle / little knuckles, thumb) + the finger chains for the grip
        readonly Transform[][] _h = new Transform[2][];
        readonly Transform[][][] _fingers = new Transform[2][][];
        float[] _grip = new float[2];
        /// <summary>Shape centre to palm surface (m): the shape rests against the palm, fingers curled round it.</summary>
        public float PalmGap = 0.012f;
        public float GripError { get; private set; }
        Transform _pouch;
        Light _glow;
        ElarimShape _es;
        float _blend = 1f, _blendDur = 0.4f;
        Vector3 _fromP; Quaternion _fromR;

        public static CarriedShape Attach(GameObject seven, Transform shape)
        {
            var c = seven.GetComponent<CarriedShape>();
            if (c == null) c = seven.AddComponent<CarriedShape>();
            c.Shape = shape;
            c.Setup();
            return c;
        }

        void Setup()
        {
            _anim = GetComponentInChildren<Animator>();
            if (_anim != null && _anim.isHuman)
            {
                _hand = _anim.GetBoneTransform(HumanBodyBones.RightHand);
                _mid = _anim.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
                _hips = _anim.GetBoneTransform(HumanBodyBones.Hips);
                Transform B(HumanBodyBones b) => _anim.GetBoneTransform(b);
                _h[0] = new[] { B(HumanBodyBones.RightHand), B(HumanBodyBones.RightIndexProximal), B(HumanBodyBones.RightMiddleProximal), B(HumanBodyBones.RightLittleProximal), B(HumanBodyBones.RightThumbProximal) };
                _h[1] = new[] { B(HumanBodyBones.LeftHand), B(HumanBodyBones.LeftIndexProximal), B(HumanBodyBones.LeftMiddleProximal), B(HumanBodyBones.LeftLittleProximal), B(HumanBodyBones.LeftThumbProximal) };
                for (int s = 0; s < 2; s++)
                {
                    bool r = s == 0;
                    _fingers[s] = new[]
                    {
                        new[] { B(r ? HumanBodyBones.RightIndexProximal : HumanBodyBones.LeftIndexProximal), B(r ? HumanBodyBones.RightIndexIntermediate : HumanBodyBones.LeftIndexIntermediate), B(r ? HumanBodyBones.RightIndexDistal : HumanBodyBones.LeftIndexDistal) },
                        new[] { B(r ? HumanBodyBones.RightMiddleProximal : HumanBodyBones.LeftMiddleProximal), B(r ? HumanBodyBones.RightMiddleIntermediate : HumanBodyBones.LeftMiddleIntermediate), B(r ? HumanBodyBones.RightMiddleDistal : HumanBodyBones.LeftMiddleDistal) },
                        new[] { B(r ? HumanBodyBones.RightRingProximal : HumanBodyBones.LeftRingProximal), B(r ? HumanBodyBones.RightRingIntermediate : HumanBodyBones.LeftRingIntermediate), B(r ? HumanBodyBones.RightRingDistal : HumanBodyBones.LeftRingDistal) },
                        new[] { B(r ? HumanBodyBones.RightLittleProximal : HumanBodyBones.LeftLittleProximal), B(r ? HumanBodyBones.RightLittleIntermediate : HumanBodyBones.LeftLittleIntermediate), B(r ? HumanBodyBones.RightLittleDistal : HumanBodyBones.LeftLittleDistal) },
                        new[] { B(r ? HumanBodyBones.RightThumbProximal : HumanBodyBones.LeftThumbProximal), B(r ? HumanBodyBones.RightThumbIntermediate : HumanBodyBones.LeftThumbIntermediate), B(r ? HumanBodyBones.RightThumbDistal : HumanBodyBones.LeftThumbDistal) },
                    };
                }
            }
            _es = Shape != null ? Shape.GetComponent<ElarimShape>() : null;
            if (_es != null) _es.Hover = false;
            if (Shape != null) Shape.SetParent(null, true);
            if (_pouch == null) BuildPouch();
        }

        /// <summary>Move it (smoothly over <paramref name="seconds"/>, 0 = at once) to her hand or her hip; Away = the
        /// script places it itself (on the table...).</summary>
        public void Set(Where w, float seconds = 0.4f)
        {
            if (Shape == null) return;
            if (w == At && seconds > 0f) return;
            _fromP = At == Where.Pocket && w != Where.Pocket ? HipPoint : Shape.position;   // QF8: out of the pocket, not out of the air
            _fromR = Shape.rotation;
            _blendDur = Mathf.Max(0.001f, seconds); _blend = seconds <= 0f ? 1f : 0f;
            At = w;
            if (_es != null) _es.Glow = w == Where.Hand || w == Where.HandLeft ? GlowInHand : w == Where.Hip ? GlowAtHip : _es.Glow;
            Debug.Log($"[Seven] dodecahedron -> {w}");
        }

        public Vector3 Palm => _hand == null ? transform.position + Vector3.up : _mid == null ? _hand.position : Vector3.Lerp(_hand.position, _mid.position, 0.55f);
        /// <summary>Where the shape rides at her hip (world).</summary>
        public Vector3 HipPoint
        {
            get
            {
                var basis = transform;
                var h = _hips != null ? _hips.position : transform.position + Vector3.up * 0.95f;
                return h + basis.right * HipOffset.x + basis.up * HipOffset.y + basis.forward * HipOffset.z;
            }
        }

        /// <summary>QF8: the palm of hand <paramref name="side"/> (0 right, 1 left): centre, normal (out of the palm) and
        /// the finger direction - from the wrist and the knuckle bones, so it follows every clip and the IK.</summary>
        public bool PalmFrame(int side, out Vector3 centre, out Vector3 normal, out Vector3 along)
        {
            centre = normal = along = Vector3.zero;
            var h = _h[side]; if (h == null || h[0] == null || h[2] == null) return false;
            var idx = h[1] != null ? h[1].position : h[2].position; var lit = h[3] != null ? h[3].position : h[2].position;
            along = (h[2].position - h[0].position).normalized;
            var across = (lit - idx).normalized;
            normal = Vector3.Cross(along, across).normalized;
            centre = Vector3.Lerp(h[0].position, h[2].position, 0.62f);
            // the thumb sits on the palm side: use it to pick the sign
            if (h[4] != null && Vector3.Dot(h[4].position - centre, normal) < 0f) normal = -normal;
            return true;
        }

        void TargetPose(out Vector3 p, out Quaternion r)
        {
            int side = At == Where.HandLeft ? 1 : 0;
            if ((At == Where.Hand || At == Where.HandLeft || At == Where.Pocket) && PalmFrame(side, out var c, out var n, out var al))
            {
                // QF8: against the palm, fingers round it (Pocket: it stays in her hand until the hand is at the pocket)
                p = c + n * (MoonLayout.ShapeRadius * 0.8f + PalmGap);
                r = Quaternion.LookRotation(al, n) * Quaternion.Euler(18f, 25f, 0f);
            }
            else if (At == Where.Hand || At == Where.HandLeft)
            {
                p = Palm + Vector3.up * (MoonLayout.ShapeRadius * 0.55f) + transform.forward * 0.015f;
                r = Quaternion.LookRotation(transform.forward, Vector3.up) * Quaternion.Euler(18f, 25f, 0f);
            }
            else
            {
                p = HipPoint + transform.up * 0.012f;
                r = transform.rotation * Quaternion.Euler(12f, 30f, 6f);
            }
        }

        void LateUpdate()
        {
            bool pocket = At == Where.Pocket && _blend >= 1f;
            ShowShape(!pocket);
            bool show = Shape != null && At != Where.Away && !pocket && Shape.gameObject.activeInHierarchy;
            if (_pouch != null)
            {
                // the pouch always hangs at the belt once she carries it (empty while it is in her hand)
                var hp = HipPoint;
                _pouch.SetPositionAndRotation(hp + transform.up * -0.025f, transform.rotation * Quaternion.Euler(6f, 0f, 4f));
                _pouch.gameObject.SetActive(_carrying && At == Where.Hip && Shape != null && Shape.gameObject.activeInHierarchy);
            }
            if (_glow != null) _glow.enabled = false;                   // QF7: no light on her at all (Dan's white blob)
            // QF8: the fingers close round it while it is in a hand
            for (int sd = 0; sd < 2; sd++)
            {
                bool want = Shape != null && !pocket && (sd == 0 ? At == Where.Hand || At == Where.Pocket : At == Where.HandLeft);
                _grip[sd] = Mathf.MoveTowards(_grip[sd], want ? 1f : 0f, Time.deltaTime * 4f);
                if (_grip[sd] > 0.001f) Curl(sd, _grip[sd]);
            }
            if (!show) return;
            TargetPose(out var p, out var r);
            if (_blend < 1f)
            {
                _blend = Mathf.Min(1f, _blend + Time.deltaTime / _blendDur);
                float u = Mathf.SmoothStep(0f, 1f, _blend);
                p = Vector3.Lerp(_fromP, p, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.05f;
                r = Quaternion.Slerp(_fromR, r, u);
            }
            Shape.SetPositionAndRotation(p, r);
            // how far the shape's surface is from the palm (check runs: must stay ~0 while it is held)
            if ((At == Where.Hand || At == Where.HandLeft) && _blend >= 1f && PalmFrame(At == Where.HandLeft ? 1 : 0, out var pc, out var pn, out _))
                GripError = Mathf.Abs(Vector3.Dot(p - pc, pn) - (MoonLayout.ShapeRadius * 0.8f + PalmGap)) + (p - pc - pn * Vector3.Dot(p - pc, pn)).magnitude;
        }

        static readonly float[] CurlDeg = { 32f, 42f, 28f }, ThumbDeg = { 14f, 18f, 12f };
        /// <summary>Close the fingers of hand <paramref name="side"/> round the shape (rotations after the animation; each
        /// joint turns about the palm's across-axis, in whichever direction brings its fingertip toward the palm).</summary>
        void Curl(int side, float w)
        {
            if (_fingers[side] == null || !PalmFrame(side, out var c, out var n, out var al)) return;
            for (int f = 0; f < 5; f++)
            {
                var ch = _fingers[side][f]; if (ch == null || ch[0] == null) continue;
                var tip = ch[2] != null ? ch[2] : ch[1] != null ? ch[1] : ch[0];
                bool thumb = f == 4;
                var axis = thumb ? al : Vector3.Cross(n, al).normalized;
                for (int j = 0; j < 3; j++)
                {
                    var b = ch[j]; if (b == null) continue;
                    float deg = (thumb ? ThumbDeg[j] : CurlDeg[j]) * w;
                    var tipP = tip.position + (tip.position - b.position).normalized * 0.02f;
                    var test = b.position + Quaternion.AngleAxis(5f, axis) * (tipP - b.position);
                    float sgn = Vector3.Dot(test - tipP, n) > 0f ? 1f : -1f;
                    b.rotation = Quaternion.AngleAxis(sgn * deg, axis) * b.rotation;
                }
            }
        }

        Renderer[] _rends;
        bool _shown = true;
        /// <summary>Shape renderers on / off (never SetActive: the galley toggles the object itself during the reveal).</summary>
        void ShowShape(bool on)
        {
            if (Shape == null || on == _shown) return;
            if (_rends == null) _rends = Shape.GetComponentsInChildren<Renderer>(true);
            foreach (var r in _rends) if (r != null) r.enabled = on;
            _shown = on;
        }
        /// <summary>True while it is out of sight in her pocket.</summary>
        public bool InPocket => At == Where.Pocket;

        bool _carrying;
        /// <summary>The pouch shows from the moment she first stows the shape (it is part of her outfit from then on).</summary>
        public bool Carrying { get => _carrying; set => _carrying = value; }

        void BuildPouch()
        {
            // a small open cup (radius 7.5 cm, 7 cm deep) of dark grey cloth with a strap tab up to the belt
            var go = new GameObject("ShapePouch");
            go.transform.SetParent(transform, false);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            var m = new Mesh { name = "ShapePouch" };
            var v = new System.Collections.Generic.List<Vector3>(); var t = new System.Collections.Generic.List<int>();
            const int N = 20; const float R = 0.075f, Rb = 0.06f, H = 0.07f;
            for (int i = 0; i <= N; i++)
            {
                float a = i / (float)N * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v.Add(d * R + Vector3.up * (H * 0.5f)); v.Add(d * Rb - Vector3.up * (H * 0.5f));
            }
            for (int i = 0; i < N; i++)
            {
                int a = i * 2, b = a + 2;
                t.AddRange(new[] { a, b, a + 1, b, b + 1, a + 1 });           // outside
                t.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 });           // inside (open top)
            }
            int c = v.Count; v.Add(-Vector3.up * (H * 0.5f));
            for (int i = 0; i < N; i++) { t.AddRange(new[] { c, i * 2 + 1, i * 2 + 3 }); t.AddRange(new[] { c, i * 2 + 3, i * 2 + 1 }); }
            // strap tab on the outer side, up to the belt line
            int s0 = v.Count; float w = 0.018f;
            v.Add(new Vector3(R + 0.002f, H * 0.4f, -w)); v.Add(new Vector3(R + 0.002f, H * 0.4f, w));
            v.Add(new Vector3(R * 0.55f, H * 1.6f, -w)); v.Add(new Vector3(R * 0.55f, H * 1.6f, w));
            t.AddRange(new[] { s0, s0 + 2, s0 + 1, s0 + 1, s0 + 2, s0 + 3, s0, s0 + 1, s0 + 2, s0 + 1, s0 + 3, s0 + 2 });
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            mf.sharedMesh = m;
            var es = Shape != null ? Shape.GetComponent<ElarimShape>() : null;
            var src = Shape != null ? Shape.GetComponentInChildren<Renderer>() : null;
            var mat = es != null && es.PouchMaterial != null ? new Material(es.PouchMaterial)
                    : src != null ? new Material(src.sharedMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat.HasProperty("_Rim")) mat.SetFloat("_Rim", 0.08f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.16f, 0.16f, 0.16f, 1f));
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", null);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            mat.DisableKeyword("_EMISSION");
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.25f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _pouch = go.transform;
            go.SetActive(false);
            // a faint white glow around the shape so it reads from the normal camera in the fog
            var lg = new GameObject("ShapeGlow");
            lg.transform.SetParent(transform, false);
            _glow = lg.AddComponent<Light>();
            _glow.type = LightType.Point; _glow.color = Color.white; _glow.range = 0.55f; _glow.intensity = 0.35f;
            _glow.shadows = LightShadows.None; _glow.enabled = false;
        }
    }
}
