using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// The reveal: floor, walls, ceiling and furniture (every Seven/DissolveLit material with _Dissolvable = 1) dissolve
    /// in a wave spreading out from 7's chair, with a soft white edge glow and drifting white motes, until only 7, her
    /// chair and the cat remain above the city. Interior lights and the window glass fade as the wave passes them; the
    /// chair tilts forward a little. Toggle again: the ship rebuilds in reverse (from the outside in).
    /// </summary>
    public class ShipReveal : MonoBehaviour
    {
        public Transform Center;                 // wave centre (7's seat)
        public Transform ChairPivot;
        public float TiltDegrees = 9f;
        public float Duration = 4.2f;
        public float MaxRadius = 17.5f;          // QF3: was 14.5 - the far wall panels / nooks (up to 15.5 m from the seat) stayed
        public float EdgeWidth = 0.7f, EdgeGlow = 2.6f, NoiseScale = 1.1f, NoiseAmplitude = 1.0f;
        public float ShipRadius = 9.8f, ShipHeight = 3.9f;
        public Material Additive;
        public List<Light> InteriorLights = new List<Light>();
        public List<Renderer> Glass = new List<Renderer>();
        /// <summary>QF3: ship parts with a non-dissolving material (e.g. the gallery TV screen): hidden as the wave passes.</summary>
        readonly List<Renderer> _solid = new List<Renderer>();

        public float Progress { get; private set; }   // 0 = ship whole, 1 = fully revealed
        public float Target { get; private set; }
        public bool Revealed => Progress >= 0.999f && Target > 0.5f;
        public bool Busy => !Mathf.Approximately(Progress, Target);
        public float Radius { get; private set; } = -1f;

        static readonly int DissolveId = Shader.PropertyToID("_SevenDissolve");
        static readonly int EdgeId = Shader.PropertyToID("_SevenDissolveEdge");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        readonly List<float> _lightBase = new List<float>();
        readonly List<Color> _glassBase = new List<Color>();
        MaterialPropertyBlock _mpb;
        Quaternion _chairRest;
        ParticleSystem _motes;

        public void Toggle() => Target = Target > 0.5f ? 0f : 1f;

        // ---- QF21 (1): the same effect from another seat (AST_01: the pilot's chair at the front window)
        /// <summary>The wave starts here instead of the galley seat (null = the usual seat); the old chair does not tilt.</summary>
        [System.NonSerialized] public Transform CenterOverride;
        static readonly int DissolvableId = Shader.PropertyToID("_Dissolvable");
        readonly List<Renderer> _kept = new List<Renderer>();
        Vector3 CenterPos => CenterOverride != null ? CenterOverride.position : Center.position;
        /// <summary>Keep these solid while the ship dissolves round them (her chair, the seat beside it, the records).</summary>
        public void KeepSolid(IEnumerable<Renderer> rs)
        {
            var mpb = new MaterialPropertyBlock();
            foreach (var r in rs)
            {
                if (r == null || _kept.Contains(r)) continue;
                r.GetPropertyBlock(mpb); mpb.SetFloat(DissolvableId, 0f); r.SetPropertyBlock(mpb); _kept.Add(r);
            }
        }
        public void ClearKeep()
        {
            var mpb = new MaterialPropertyBlock();
            foreach (var r in _kept) { if (r == null) continue; r.GetPropertyBlock(mpb); mpb.SetFloat(DissolvableId, 1f); r.SetPropertyBlock(mpb); r.forceRenderingOff = false; }
            _kept.Clear(); CenterOverride = null;
        }
        public int KeptCount => _kept.Count;
        public void Set(bool revealed) => Target = revealed ? 1f : 0f;

        void Awake()
        {
            Shader.SetGlobalVector(DissolveId, new Vector4(0, 0, 0, -1));
        }

        void Start()
        {
            foreach (var l in InteriorLights) _lightBase.Add(l.intensity);
            _mpb = new MaterialPropertyBlock();
            foreach (var g in Glass) _glassBase.Add(g.sharedMaterial.GetColor(BaseColorId));
            _chairRest = ChairPivot.localRotation;
            var ship = GameObject.Find("Ship");
            if (ship != null)
                foreach (var r in ship.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is ParticleSystemRenderer || Glass.Contains(r) || r.transform.IsChildOf(ChairPivot)) continue;
                    bool dissolves = true;
                    foreach (var m in r.sharedMaterials) if (m == null || !m.HasProperty("_Dissolvable") || m.GetFloat("_Dissolvable") < 0.5f) dissolves = false;
                    if (!dissolves) _solid.Add(r);
                }
            _motes = MakeMotes();
            Apply();
        }

        void OnDisable() => Shader.SetGlobalVector(DissolveId, new Vector4(0, 0, 0, -1));

        void Update()
        {
            float before = Progress;
            Progress = Mathf.MoveTowards(Progress, Target, Time.deltaTime / Duration);
            Apply();
            if (Progress > 0f && Progress < 1f && !Mathf.Approximately(before, Progress)) EmitMotes();
        }

        void Apply()
        {
            float e = Mathf.SmoothStep(0f, 1f, Progress);
            Radius = e <= 0.0005f ? -1f : Mathf.Lerp(0f, MaxRadius + (CenterOverride != null ? 6f : 0f), e);   // QF21: from the front seat the far wall is further
            var c = CenterPos;
            Shader.SetGlobalVector(DissolveId, new Vector4(c.x, c.y, c.z, Radius));
            Shader.SetGlobalVector(EdgeId, new Vector4(EdgeWidth, EdgeGlow, NoiseScale, NoiseAmplitude));

            for (int i = 0; i < InteriorLights.Count; i++)
            {
                float d = Vector3.Distance(InteriorLights[i].transform.position, c);
                InteriorLights[i].intensity = _lightBase[i] * (1f - Smooth(d - 1.2f, d + 0.8f, Radius));
            }
            for (int i = 0; i < Glass.Count; i++)
            {
                var b = Glass[i].bounds;
                float d = Vector3.Distance(b.ClosestPoint(c), c);
                float a = 1f - Smooth(d - 0.3f, d + 2.5f, Radius);
                var col = _glassBase[i]; col.a *= a;
                Glass[i].GetPropertyBlock(_mpb);
                _mpb.SetColor(BaseColorId, col);
                Glass[i].SetPropertyBlock(_mpb);
                Glass[i].enabled = a > 0.01f;
            }
            foreach (var r in _solid)
            {
                if (r == null || _kept.Contains(r)) continue;
                float d = Vector3.Distance(r.bounds.ClosestPoint(c), c);
                r.forceRenderingOff = Radius > d + 0.3f;
            }
            ChairPivot.localRotation = _chairRest * Quaternion.Euler(CenterOverride != null ? 0f : TiltDegrees * e, 0f, 0f);
        }

        static float Smooth(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        /// <summary>Motes drift up from where the wave front cuts through the ship.</summary>
        void EmitMotes()
        {
            if (Radius <= 0f) return;
            var c = CenterPos;
            int n = Mathf.CeilToInt(160f * Time.deltaTime);
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < n; i++)
            {
                float y = Random.Range(0f, ShipHeight);
                float dy = y - c.y;
                float h2 = Radius * Radius - dy * dy;
                if (h2 <= 0f) continue;
                float a = Random.Range(0f, Mathf.PI * 2f);
                var p = new Vector3(c.x + Mathf.Cos(a) * Mathf.Sqrt(h2), y, c.z + Mathf.Sin(a) * Mathf.Sqrt(h2));
                if (new Vector2(p.x, p.z).magnitude > ShipRadius) continue;
                ep.position = p;
                ep.velocity = new Vector3(Random.Range(-0.08f, 0.08f), Random.Range(0.15f, 0.5f), Random.Range(-0.08f, 0.08f));
                ep.startLifetime = Random.Range(1.8f, 3.4f);
                ep.startSize = Random.Range(0.03f, 0.09f);
                ep.startColor = Color.white;
                _motes.Emit(ep, 1);
            }
        }

        ParticleSystem MakeMotes()
        {
            var go = new GameObject("RevealMotes");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2000;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = new Material(Additive);
            m.SetTexture("_MainTex", FxUtil.Dot(64));
            m.SetFloat("_Intensity", 1.4f);
            r.sharedMaterial = m;
            ps.Play();
            return ps;
        }
    }
}
