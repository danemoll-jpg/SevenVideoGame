using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// White-only telekinesis effects: glow building around 7's hands, the forehead dot pulsing brighter, a shimmer of
    /// ripple rings and drifting light motes travelling to the bot, a faint aura around the held bot, nearby rocks and
    /// grass floating up to orbit her, and a soft ring burst on release. Purely visual; it never hurts anyone.
    /// </summary>
    public class TelekinesisFX : MonoBehaviour
    {
        public SevenFighter Seven;
        public TrainingBot Bot;
        public Material Additive;           // Seven/Additive
        public float DebrisRadius = 4.5f;

        Texture2D _dot, _ring;
        Transform _handL, _handR, _cam;
        Renderer _faceRenderer;
        int _faceMatIndex = -1;
        Material _faceMat;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        class Sprite { public Transform T; public Material M; }
        Sprite _glowL, _glowR, _auraCore;
        Sprite[] _aura;
        Light _handLight, _botLight;
        ParticleSystem _motes;
        readonly List<Ring> _rings = new List<Ring>();
        class Ring { public Sprite S; public float Age, Life; public Vector3 From, To; public float Size0, Size1, Peak; public bool Active; }
        float _charge, _aur, _ringTimer;
        readonly List<Debris> _floating = new List<Debris>();

        public float Charge => _charge;
        public float Aura => _aur;
        public int FloatingDebris => _floating.Count;

        void Start()
        {
            _dot = MakeDot(64);
            _ring = MakeRing(128);
            _handL = Seven.Bone(HumanBodyBones.LeftHand);
            _handR = Seven.Bone(HumanBodyBones.RightHand);
            _cam = Camera.main != null ? Camera.main.transform : null;

            foreach (var r in Seven.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name.StartsWith("7_forehead_glow")) { _faceRenderer = r; _faceMatIndex = i; }
            }
            if (_faceRenderer != null) _faceMat = _faceRenderer.materials[_faceMatIndex];

            _glowL = MakeSprite("TkGlowL", _dot);
            _glowR = MakeSprite("TkGlowR", _dot);
            _auraCore = MakeSprite("TkAuraCore", _dot);
            _aura = new[] { MakeSprite("TkAura0", _dot), MakeSprite("TkAura1", _dot), MakeSprite("TkAura2", _dot) };
            for (int i = 0; i < 8; i++) _rings.Add(new Ring { S = MakeSprite("TkRing" + i, _ring) });

            _handLight = MakeLight("TkHandLight", 3f);
            _botLight = MakeLight("TkBotLight", 3.2f);
            _motes = MakeMotes();

            Seven.LiftStarted += OnLiftStarted;
            Seven.LiftReleased += OnLiftReleased;
        }

        // ------------------------------------------------------------------------------- building blocks
        static Texture2D MakeDot(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float r2 = dx * dx + dy * dy;
                    float v = Mathf.Exp(-r2 * 5f) * Mathf.Clamp01(1f - r2);
                    t.SetPixel(x, y, new Color(v, v, v));
                }
            t.Apply();
            return t;
        }

        static Texture2D MakeRing(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float v = Mathf.Exp(-Mathf.Pow((r - 0.78f) / 0.07f, 2f)) + 0.25f * Mathf.Exp(-Mathf.Pow((r - 0.6f) / 0.18f, 2f));
                    v *= Mathf.Clamp01((1f - r) * 8f);
                    t.SetPixel(x, y, new Color(v, v, v));
                }
            t.Apply();
            return t;
        }

        Sprite MakeSprite(string name, Texture2D tex)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var m = new Material(Additive) { mainTexture = tex };
            m.SetFloat(IntensityId, 0f);
            r.sharedMaterial = m;
            go.SetActive(false);
            return new Sprite { T = go.transform, M = m };
        }

        Light MakeLight(string name, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = Color.white;
            l.range = range;
            l.intensity = 0f;
            l.shadows = LightShadows.None;
            return l;
        }

        ParticleSystem MakeMotes()
        {
            var go = new GameObject("TkMotes");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1500;
            main.startSpeed = 0f;
            main.loop = true;
            main.playOnAwake = false;
            main.startColor = Color.white;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = new Material(Additive) { mainTexture = _dot };
            m.SetFloat(IntensityId, 2.5f);
            r.sharedMaterial = m;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        void Billboard(Sprite s, Vector3 pos, float size, float intensity)
        {
            bool on = intensity > 0.003f;
            if (s.T.gameObject.activeSelf != on) s.T.gameObject.SetActive(on);
            if (!on) return;
            s.T.position = pos;
            if (_cam != null) s.T.rotation = _cam.rotation;
            s.T.localScale = Vector3.one * size;
            s.M.SetFloat(IntensityId, intensity);
        }

        void Emit(Vector3 pos, Vector3 vel, float size, float life)
        {
            var p = new ParticleSystem.EmitParams
            {
                position = pos, velocity = vel, startSize = size, startLifetime = life,
                startColor = Color.white, applyShapeToPosition = false,
            };
            _motes.Emit(p, 1);
        }

        Vector3 BotChest() => Bot.Bone(HumanBodyBones.Chest) != null ? Bot.Bone(HumanBodyBones.Chest).position : Bot.transform.position + Vector3.up * 1.1f;

        // ---------------------------------------------------------------------------------- events
        void OnLiftStarted()
        {
            _floating.Clear();
            var c = Seven.transform.position;
            foreach (var d in FindObjectsByType<Debris>())
            {
                var o = d.transform.position - c; o.y = 0f;
                if (o.magnitude <= DebrisRadius) { d.Float(Seven.transform); _floating.Add(d); }
            }
        }

        void OnLiftReleased()
        {
            foreach (var d in _floating) d.Drop();
            _floating.Clear();
            // Soft ring burst around the bot as it is pushed away.
            var at = BotChest();
            for (int i = 0; i < 2; i++)
                StartRing(at, at + (Bot.transform.position - Seven.transform.position).normalized * 0.6f, 0.5f + i * 0.25f, 2.2f + i * 0.8f, 0.55f + i * 0.15f, 1.6f);
        }

        void StartRing(Vector3 from, Vector3 to, float size0, float size1, float life, float peak)
        {
            foreach (var r in _rings)
            {
                if (r.Active) continue;
                r.Active = true; r.Age = 0f; r.Life = life; r.From = from; r.To = to; r.Size0 = size0; r.Size1 = size1; r.Peak = peak;
                return;
            }
        }

        // ---------------------------------------------------------------------------------- per frame
        void LateUpdate()
        {
            if (Seven == null || Bot == null || _handL == null) return;
            float dt = Time.deltaTime;
            var tk = Seven.Tk;
            float t = Time.time;

            float wantCharge = tk == SevenFighter.TkPhase.Charging ? Mathf.Clamp01(Seven.TkPhaseTime / 0.6f)
                             : tk == SevenFighter.TkPhase.Lifting ? 1f : 0f;
            _charge = Mathf.MoveTowards(_charge, wantCharge, dt * (wantCharge > _charge ? 2.2f : 1.4f));
            float wantAura = Bot.IsLifted ? 1f : 0f;
            _aur = Mathf.MoveTowards(_aur, wantAura, dt * (wantAura > _aur ? 2.5f : 1.2f));

            // Hands: glow builds, flickers slightly while holding.
            float flicker = 1f + 0.12f * Mathf.Sin(t * 23f) * (tk == SevenFighter.TkPhase.Lifting ? 1f : 0f);
            Billboard(_glowL, _handL.position, 0.16f + 0.34f * _charge, 1.6f * _charge * flicker);
            Billboard(_glowR, _handR.position, 0.16f + 0.34f * _charge, 1.6f * _charge * flicker);
            var hands = (_handL.position + _handR.position) * 0.5f;
            _handLight.transform.position = hands;
            _handLight.intensity = 2.2f * _charge * flicker;

            // Forehead dot: base emission x4, pulses up to ~x14 while the power is active.
            if (_faceMat != null)
                _faceMat.SetColor(EmissionId, Color.white * (4f + _charge * (7f + 3f * Mathf.Sin(t * 7.5f))));

            // Motes gathering around the hands while charging / holding.
            if (_charge > 0.05f && Random.value < _charge * 0.9f)
            {
                var off = Random.insideUnitSphere * 0.25f;
                Emit(hands + off, -off * 0.8f + Vector3.up * 0.05f, Random.Range(0.02f, 0.05f), Random.Range(0.35f, 0.6f));
            }

            if (tk == SevenFighter.TkPhase.Lifting)
            {
                var target = BotChest();
                // Travelling motes: a drifting stream from her hands to the bot.
                for (int i = 0; i < 2; i++)
                {
                    var from = hands + Random.insideUnitSphere * 0.08f;
                    float life = Random.Range(0.45f, 0.8f);
                    var to = target + Random.insideUnitSphere * 0.35f;
                    Emit(from, (to - from) / life + Random.insideUnitSphere * 0.25f, Random.Range(0.025f, 0.06f), life);
                }
                // Ripple rings travelling along the beam.
                _ringTimer -= dt;
                if (_ringTimer <= 0f)
                {
                    _ringTimer = 0.24f;
                    StartRing(hands, target, 0.22f, 0.9f, 0.6f, 0.9f);
                }
            }

            // Aura on the held bot: soft glow sprites on its body + motes rising off it + a faint light.
            if (_aur > 0.01f)
            {
                var hips = Bot.Bone(HumanBodyBones.Hips);
                var head = Bot.Bone(HumanBodyBones.Head);
                var chest = BotChest();
                float breathe = 1f + 0.15f * Mathf.Sin(t * 3.1f);
                Billboard(_auraCore, chest, 1.9f, 0.32f * _aur * breathe);
                Billboard(_aura[0], hips != null ? hips.position : chest, 1.2f, 0.22f * _aur);
                Billboard(_aura[1], head != null ? head.position : chest, 0.9f, 0.25f * _aur);
                Billboard(_aura[2], Bot.transform.position + Vector3.up * 0.15f, 1.3f, 0.18f * _aur);
                _botLight.transform.position = chest;
                _botLight.intensity = 1.6f * _aur;
                if (Random.value < _aur * 0.9f)
                {
                    var p = Bot.transform.position + Vector3.up * Random.Range(0.1f, 1.7f) + Random.insideUnitSphere * 0.4f;
                    Emit(p, Vector3.up * Random.Range(0.1f, 0.35f) + Random.insideUnitSphere * 0.08f, Random.Range(0.02f, 0.045f), Random.Range(0.6f, 1.1f));
                }
            }
            else
            {
                Billboard(_auraCore, Vector3.zero, 0, 0); foreach (var a in _aura) Billboard(a, Vector3.zero, 0, 0);
                _botLight.intensity = 0f;
            }

            // Rings: face along their travel, expand and fade.
            foreach (var r in _rings)
            {
                if (!r.Active) { if (r.S.T.gameObject.activeSelf) r.S.T.gameObject.SetActive(false); continue; }
                r.Age += dt;
                float k = r.Age / r.Life;
                if (k >= 1f) { r.Active = false; r.S.T.gameObject.SetActive(false); continue; }
                if (!r.S.T.gameObject.activeSelf) r.S.T.gameObject.SetActive(true);
                var dir = r.To - r.From;
                r.S.T.position = Vector3.Lerp(r.From, r.To, k);
                if (dir.sqrMagnitude > 1e-4f) r.S.T.rotation = Quaternion.LookRotation(dir);
                r.S.T.localScale = Vector3.one * Mathf.Lerp(r.Size0, r.Size1, k);
                r.S.M.SetFloat(IntensityId, r.Peak * Mathf.Sin(k * Mathf.PI));
            }
        }
    }
}
