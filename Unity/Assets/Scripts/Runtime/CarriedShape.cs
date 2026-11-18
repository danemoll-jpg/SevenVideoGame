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
    public class CarriedShape : MonoBehaviour
    {
        public enum Where { Hand, Hip, Away }
        public Transform Shape;
        public Where At { get; private set; } = Where.Away;
        [Tooltip("Pouch on the belt, in 7's root space from the hips bone: right, up, forward (m).")]
        public Vector3 HipOffset = new Vector3(0.215f, 0.06f, -0.03f);
        public float GlowAtHip = 1.1f, GlowInHand = 1.6f;

        Animator _anim;
        Transform _hand, _mid, _hips;
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
            _fromP = Shape.position; _fromR = Shape.rotation;
            _blendDur = Mathf.Max(0.001f, seconds); _blend = seconds <= 0f ? 1f : 0f;
            At = w;
            if (_es != null) _es.Glow = w == Where.Hand ? GlowInHand : w == Where.Hip ? GlowAtHip : _es.Glow;
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

        void TargetPose(out Vector3 p, out Quaternion r)
        {
            if (At == Where.Hand)
            {
                // resting in the cupped right hand
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
            bool show = Shape != null && At != Where.Away && Shape.gameObject.activeInHierarchy;
            if (_pouch != null)
            {
                // the pouch always hangs at the belt once she carries it (empty while it is in her hand)
                var hp = HipPoint;
                _pouch.SetPositionAndRotation(hp + transform.up * -0.025f, transform.rotation * Quaternion.Euler(6f, 0f, 4f));
                _pouch.gameObject.SetActive(_carrying && Shape != null && Shape.gameObject.activeInHierarchy);
            }
            if (_glow != null) { _glow.enabled = show; if (show) _glow.transform.position = Shape.position; }
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
        }

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
