using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Reusable world look ("Kyle mood"): dark foreground, bright glowing misty distance, strong ground fog.
    /// Lives on the Atmosphere prefab (Assets/Atmosphere/Atmosphere.prefab) together with the post-process Volume.
    /// Feeds the Seven/HeightFog full-screen pass (URP renderer feature "SevenHeightFog") through shader globals.
    /// Knobs are documented in Docs/ATMOSPHERE.md. Grayscale only: every value here is a brightness, never a colour.
    /// </summary>
    [ExecuteAlways]
    public class Atmosphere : MonoBehaviour
    {
        [Header("Foreground")]
        [Tooltip("Metres from the camera before fog starts: keeps the foreground dark like a silhouette.")]
        public float FogStart = 5f;

        [Header("Ground fog (thick layer hugging the ground / fog sea below)")]
        [Tooltip("Fog density per metre at GroundBase height. Higher = legs vanish sooner.")]
        public float GroundDensity = 0.07f;
        [Tooltip("World height where the ground layer has GroundDensity (usually the walkable ground).")]
        public float GroundBase = 0f;
        [Tooltip("Metres over which the ground layer thins out by e. Small = thin sharp layer.")]
        public float GroundFalloff = 0.6f;
        [Tooltip("The ground layer only builds up over this many metres of each view ray (so distant structures stay visible).")]
        public float GroundRange = 22f;

        [Header("Valley haze (tall soft layer that buries the bases of distant structures)")]
        public float ValleyDensity = 0.002f;
        public float ValleyBase = -10f;
        public float ValleyFalloff = 15f;

        [Header("Distance haze (low contrast far away)")]
        public float DistanceDensity = 0.0012f;
        [Tooltip("Distance haze thins out for rays travelling above this height (m): high structures stay darker.")]
        public float HazeHeight = 18f;
        [Range(0f, 1f)] public float MaxFog = 1f;
        [Tooltip("Distance used for empty sky pixels.")]
        public float SkyDistance = 900f;

        [Header("Fog brightness (0 = black, 1 = white; >1 glows through Bloom)")]
        public float NearBrightness = 0.85f;
        public float FarBrightness = 0.95f;
        public float BrightnessDistance = 120f;
        public float ZenithBrightness = 0.32f;

        [Header("Drifting variation in the ground layer")]
        [Range(0f, 1f)] public float NoiseStrength = 0.45f;
        public float NoiseScale = 0.035f;
        public float NoiseSpeed = 0.012f;
        public float NoiseFadeDistance = 40f;
        public Texture2D NoiseTexture;

        [Header("Soft light sources inside the fog (up to 8; uses child transforms named Glow*)")]
        public float GlowIntensity = 0.35f;
        public float GlowRadius = 18f;
        /// <summary>QF26 (3): where "ground level" is for the fog (0 = the world origin); a level staged far above / below the
        /// deck (the CAT_01 exterior 2.9 km up) gets its haze at its own height.</summary>
        [System.NonSerialized] public float HeightOffset;
        static readonly int YOffId = Shader.PropertyToID("_SevenFogYOff");

        static readonly int OnId = Shader.PropertyToID("_SevenFogOn");
        static readonly int StartId = Shader.PropertyToID("_SevenFogStart");
        static readonly int GroundId = Shader.PropertyToID("_SevenFogGround");
        static readonly int ValleyId = Shader.PropertyToID("_SevenFogValley");
        static readonly int DistId = Shader.PropertyToID("_SevenFogDist");
        static readonly int ToneId = Shader.PropertyToID("_SevenFogTone");
        static readonly int NoiseId = Shader.PropertyToID("_SevenFogNoise");
        static readonly int NoiseTexId = Shader.PropertyToID("_SevenNoiseTex");
        static readonly int GlowPosId = Shader.PropertyToID("_SevenGlowPos");
        static readonly int GlowIntId = Shader.PropertyToID("_SevenGlowInt");
        static readonly int GlowCountId = Shader.PropertyToID("_SevenGlowCount");

        readonly Vector4[] _glowPos = new Vector4[8];
        readonly Vector4[] _glowInt = new Vector4[8];

        void OnEnable()
        {
            // Dev knob: -atmofile <json> overrides these values at start-up (tune the look without rebuilding).
            if (Application.isPlaying)
            {
                var args = System.Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length - 1; i++)
                    if (args[i] == "-atmofile" && System.IO.File.Exists(args[i + 1]))
                    {
                        JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(args[i + 1]), this);
                        Debug.Log("[Seven] atmosphere overrides from " + args[i + 1] + ": " + JsonUtility.ToJson(this));
                    }
            }
            Apply();
        }
        void OnDisable() => Shader.SetGlobalFloat(OnId, 0f);
        void Update() => Apply();

        public void Apply()
        {
            Shader.SetGlobalFloat(OnId, 1f);
            Shader.SetGlobalVector(GroundId, new Vector4(GroundDensity, GroundBase, GroundFalloff, GroundRange));
            Shader.SetGlobalVector(ValleyId, new Vector4(ValleyDensity, ValleyBase, ValleyFalloff, 0));
            Shader.SetGlobalFloat(StartId, FogStart);
            Shader.SetGlobalFloat(YOffId, HeightOffset);
            Shader.SetGlobalVector(DistId, new Vector4(DistanceDensity, MaxFog, SkyDistance, HazeHeight));
            Shader.SetGlobalVector(ToneId, new Vector4(NearBrightness, FarBrightness, BrightnessDistance, ZenithBrightness));
            Shader.SetGlobalVector(NoiseId, new Vector4(NoiseTexture != null ? NoiseStrength : 0f, NoiseScale, NoiseSpeed, NoiseFadeDistance));
            if (NoiseTexture != null) Shader.SetGlobalTexture(NoiseTexId, NoiseTexture);

            int n = 0;
            foreach (Transform c in transform)
            {
                if (n >= 8) break;
                if (!c.gameObject.activeInHierarchy || !c.name.StartsWith("Glow")) continue;
                // Child scale.x multiplies the radius, scale.y the intensity.
                _glowPos[n] = new Vector4(c.position.x, c.position.y, c.position.z, GlowRadius * c.localScale.x);
                _glowInt[n] = new Vector4(GlowIntensity * c.localScale.y, 0, 0, 0);
                n++;
            }
            Shader.SetGlobalVectorArray(GlowPosId, _glowPos);
            Shader.SetGlobalVectorArray(GlowIntId, _glowInt);
            Shader.SetGlobalFloat(GlowCountId, n);
        }

        // ---- QF26 (1): what the fog adds along a view - the same sums as SevenHeightFog (tone + in-fog glow). Above ~1.2 the fog
        // itself is HDR-bright and bloom whites the frame out (Cattalus 0.18.0: 8 glows 400-800 m wide summed to ~10).
        public float FogColourAlong(Vector3 cam, Vector3 dir, float dist)
        {
            float tone = Mathf.Lerp(NearBrightness, FarBrightness, Mathf.Clamp01(dist / Mathf.Max(BrightnessDistance, 1f)));
            tone = Mathf.Lerp(tone, ZenithBrightness, Mathf.Clamp01(dir.y * 3f));
            float glow = 0f; int n = 0;
            foreach (Transform c in transform)
            {
                if (n >= 8) break;
                if (!c.gameObject.activeInHierarchy || !c.name.StartsWith("Glow")) continue;
                var L = c.position - cam; float t = Mathf.Clamp(Vector3.Dot(L, dir), 0f, dist);
                float d = (L - dir * t).magnitude / Mathf.Max(GlowRadius * c.localScale.x, 1e-3f);
                glow += GlowIntensity * c.localScale.y * (Mathf.Exp(-d * d) + 0.12f / (1f + d * d)); n++;
            }
            return Mathf.Min(tone + glow, 1.25f);   // the shader clamps the same (QF26)
        }
        /// <summary>QF26 rule (QF28 pass): keep the fog colour - the tone plus the in-fog glows - at or below <paramref name="budget"/>
        /// (~1.0; the shader clamps at 1.25). Measured over 96 view directions from five spots round the level; the far tone is
        /// capped at <paramref name="farCap"/> first, then the glow strength scaled until the brightest direction fits. Returns a
        /// report line. Used by the builders for every world and at runtime by presets that blend to new fog numbers.</summary>
        public string FitFogBudget(float budget = 0.98f, float farCap = 0.8f)
        {
            var cams = new System.Collections.Generic.List<Vector3> { transform.position + new Vector3(0f, 2f, 0f) };
            foreach (Transform c in transform) if (c.name.StartsWith("Glow") && c.gameObject.activeSelf) { var q = c.position; cams.Add(new Vector3(q.x * 0.5f, 2f, q.z * 0.5f)); if (cams.Count >= 5) break; }
            var dirs = new System.Collections.Generic.List<Vector3>(); for (int i = 0; i < 96; i++) { float y = 1f - (i + 0.5f) / 96f * 1.4f, r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y)), ph = i * 2.39996f; dirs.Add(new Vector3(Mathf.Cos(ph) * r, y, Mathf.Sin(ph) * r).normalized); }
            float Max() { float m = 0f; foreach (var c in cams) foreach (var d in dirs) m = Mathf.Max(m, FogColourAlong(c, d, SkyDistance)); return m; }
            float m0 = Max(), far0 = FarBrightness, zen0 = ZenithBrightness, g0 = GlowIntensity;
            if (m0 > budget)
            {
                FarBrightness = Mathf.Min(FarBrightness, farCap); ZenithBrightness = Mathf.Min(ZenithBrightness, Mathf.Max(farCap, 0.85f));
                float lo = 0f, hi = 1f;
                if (Max() > budget) { for (int it = 0; it < 14; it++) { float k = (lo + hi) * 0.5f; GlowIntensity = g0 * k; if (Max() > budget) hi = k; else lo = k; } GlowIntensity = g0 * lo; }
            }
            LastBudget = Max();
            return $"brightest fog colour {m0:0.00} -> {LastBudget:0.00} (far tone {far0:0.00} -> {FarBrightness:0.00}, zenith {zen0:0.00} -> {ZenithBrightness:0.00}, glow intensity {g0:0.00} -> {GlowIntensity:0.00}; must be <= ~1.0)";
        }
        [System.NonSerialized] public float LastBudget;
        /// <summary>The fog colour at the horizon of a camera (its forward, levelled, out to the sky distance).</summary>
        public static string Report(Camera cam)
        {
            var a = Object.FindFirstObjectByType<Atmosphere>();
            if (a == null || cam == null) return "no atmosphere";
            var f = cam.transform.forward; var flat = new Vector3(f.x, 0f, f.z).normalized;
            return $"fog colour ahead {a.FogColourAlong(cam.transform.position, f, a.SkyDistance):0.00}, at the horizon {a.FogColourAlong(cam.transform.position, flat, a.SkyDistance):0.00} (1 = white; glow {a.GlowIntensity:0.00} x radius {a.GlowRadius:0}, max fog {a.MaxFog:0.00})";
        }
    }
}
