using System.Collections.Generic;
using UnityEngine;
using L = Seven.FinaleLayout;

namespace Seven
{
    /// <summary>
    /// QF34 (8) - Kyle's request via Dan: "not overdo it, but it needs more than it has". More sparkle in the Moon finale, in Kyle's misty
    /// monochrome with soft glows - focal points glow, the rest stays dark and misty:
    ///  - light motes drifting in the air of the chamber, along the route to the door and at the berth (catching the light);
    ///  - a soft halo in the haze round each working light (the mining lights, the cranes' work lights, the berth's lamps);
    ///  - crystal veins in the chamber's old stone between the screens, low on the wall, breathing slowly;
    ///  - glyphs on the ancient door and the chamber's walls that wake (brighten and stay lit) as she passes them.
    /// All white, additive, small; nothing large or bright enough to wash out a frame (the image gate still applies).
    /// </summary>
    public class FinaleSparkle : MonoBehaviour
    {
        public static int Motes, Haloes, Veins, Glyphs, GlyphsWoken;
        readonly List<(LineRenderer lr, float phase, float baseW)> _veins = new List<(LineRenderer, float, float)>();
        readonly List<(LineRenderer lr, Vector3 at, float lit)> _glyphs = new List<(LineRenderer, Vector3, float)>();
        Transform _her; Material _veinMat, _glyphMat;
        float[] _glyphLit;

        public static FinaleSparkle Make(Transform her)
        {
            var go = new GameObject("FinaleSparkle"); var s = go.AddComponent<FinaleSparkle>(); s._her = her; s.Build(); return s;
        }
        void Build()
        {
            var dot = Markers.AdditiveMatTex(0.5f);
            MoteField(L.ChamberC + Vector3.up * 4.5f, new Vector3(18f, 9f, 18f), 160, dot);
            MoteField((L.DoorAt + L.Overlook) * 0.5f + Vector3.up * 2.5f, new Vector3(8f, 5f, Mathf.Abs(L.DoorAt.z - L.Overlook.z)), 140, dot);
            MoteField(L.Overlook + Vector3.up * 3f, new Vector3(22f, 6f, 14f), 80, dot);
            // haloes round the working lights (a soft dot facing the camera, sized by the light's reach)
            var halo = Markers.AdditiveMatTex(0.16f);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type != LightType.Point || Haloes >= 40) continue;
                var n = l.name; if (!(n.Contains("MiningLight") || n.Contains("DrillWork") || n.Contains("Lamp") || n.Contains("Glow") || n.Contains("Work"))) continue;
                if (n.Contains("Chamber") || n.Contains("Doorway") || n.Contains("Socket") || n.Contains("Oculus") || (new Vector2(l.transform.position.x - L.ChamberC.x, l.transform.position.z - L.ChamberC.z)).magnitude < L.ChamberR + 3f) continue;   // (QF34 3: a halo by the barrier console stood in front of Europa's screen)   // never a halo out in the chamber (in front of the screens)
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = "Halo_" + n; q.transform.SetParent(transform, false); q.transform.position = l.transform.position;
                float sz = Mathf.Clamp(l.range * 0.16f, 0.9f, 3.2f); q.transform.localScale = Vector3.one * sz;
                var r = q.GetComponent<Renderer>(); r.sharedMaterial = halo; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; q.AddComponent<FaceCamera>(); Haloes++;
            }
            // crystal veins in the chamber's stone: low on the wall between the screens (clear of each screen and of the alcove)
            _veinMat = Markers.Additive(0.3f);   // faint: a glint in the stone, not a drawn line
            var rnd = new System.Random(34);
            for (int i = 0; i < 7; i++)
            {
                float yaw = (i + 0.5f) * 360f / 7f; if (yaw > 72f && yaw < 122f) continue;                    // not in the alcove's mouth / by Europa's moved screen
                for (int k = 0; k < 2; k++)
                {
                    var lr = Line("CrystalVein", _veinMat, 0.018f); var pts = new List<Vector3>(); float a0 = yaw + (k == 0 ? -4f : 3f), y = 0.25f + (float)rnd.NextDouble() * 0.5f;
                    for (int s = 0; s < 9; s++) { float a = (a0 + s * 1.1f + ((float)rnd.NextDouble() - 0.5f) * 0.6f) * Mathf.Deg2Rad; y += 0.12f + (float)rnd.NextDouble() * 0.22f; pts.Add(L.ChamberC + new Vector3(Mathf.Sin(a) * (L.ChamberR - 0.06f), y, Mathf.Cos(a) * (L.ChamberR - 0.06f))); }
                    lr.positionCount = pts.Count; lr.SetPositions(pts.ToArray()); _veins.Add((lr, (float)rnd.NextDouble() * 6f, 0.018f)); Veins++;
                }
            }
            // glyphs: carved into the chamber's wall between the screens at eye height (they wake as she comes near and stay lit)
            _glyphMat = Markers.Additive(0.08f);
            for (int i = 0; i < 7; i++)
            {
                float yaw = (i + 0.5f) * 360f / 7f; if (yaw > 72f && yaw < 122f) continue;
                var dir = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad)); var tan = Vector3.Cross(Vector3.up, dir);
                var g = L.ChamberC + dir * (L.ChamberR - 0.07f) + Vector3.up * 1.75f;
                var lr = Line("WakingGlyph", new Material(_glyphMat), 0.018f); int sides = 3 + _glyphs.Count % 4; var pts = new Vector3[sides + 1];
                for (int k = 0; k <= sides; k++) { float a = k / (float)sides * Mathf.PI * 2f + Mathf.PI * 0.5f; pts[k] = g + tan * Mathf.Cos(a) * 0.2f + Vector3.up * Mathf.Sin(a) * 0.2f; }
                lr.positionCount = pts.Length; lr.SetPositions(pts); _glyphs.Add((lr, g, 0f)); Glyphs++;
            }
            _glyphLit = new float[_glyphs.Count];
            ChapterCapture.I?.Log($"FINALE SPARKLE (QF34 8): light motes {Motes} (the chamber, the route to the door, the berth), haloes round {Haloes} working lights, crystal veins {Veins} in the chamber's stone, waking glyphs {Glyphs} on the chamber's wall - all white, additive, small");
        }
        LineRenderer Line(string name, Material m, float w)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false); var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true; lr.sharedMaterial = m; lr.widthMultiplier = w; lr.numCapVertices = 2; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false;
            return lr;
        }
        void MoteField(Vector3 c, Vector3 size, int n, Material m)
        {
            var go = new GameObject("LightMotes"); go.transform.SetParent(transform, false); go.transform.position = c;
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 11f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f); main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
            main.startColor = new Color(1f, 1f, 1f, 0.7f); main.maxParticles = n; main.simulationSpace = ParticleSystemSimulationSpace.World; main.prewarm = true;
            var em = ps.emission; em.rateOverTime = n / 8f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = size;
            var no = ps.noise; no.enabled = true; no.strength = 0.08f; no.frequency = 0.25f;
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) }); col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = m;
            ps.Play(); Motes += n;
        }
        void Update()
        {
            float t = Time.time;
            foreach (var (lr, ph, w) in _veins) if (lr != null) lr.widthMultiplier = w * (0.75f + 0.35f * Mathf.Sin(t * 0.7f + ph));   // breathing slowly
            if (_her == null) return;
            for (int i = 0; i < _glyphs.Count; i++)
            {
                var (lr, at, _) = _glyphs[i]; if (lr == null) continue;
                bool near = (new Vector2(_her.position.x - at.x, _her.position.z - at.z)).magnitude < 8f;
                float want = near || _glyphLit[i] > 0.95f ? 1f : 0f; if (near && _glyphLit[i] < 0.05f) GlyphsWoken++;
                _glyphLit[i] = Mathf.MoveTowards(_glyphLit[i], want, Time.deltaTime * 0.5f);
                var m = lr.sharedMaterial; if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", 0.05f + 0.4f * _glyphLit[i] * (0.9f + 0.1f * Mathf.Sin(t * 2f + i)));
            }
        }
    }
}
