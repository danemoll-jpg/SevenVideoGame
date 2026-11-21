using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF18 (15, 19b): one of Viper's pylons - angular dark metal, a hard white emitter head, three hinged armour panels.
    /// Its done state is unmistakable: the light dies (head and lamp), sparks and a power-down sound, the panels fall open,
    /// the head slumps; the prompt is gone and it can never be used again.
    /// QF19 (2): it shows what it does - a white power beam runs from its head to Viper's platform while it is on - and its
    /// shutdown is a bang at once: a white flash, a burst of sparks, the beam snaps off, the panels blow open, the power-down
    /// sound (the camera nudge comes from ViperFight).
    /// </summary>
    public class MarsPylon : MonoBehaviour
    {
        public Renderer Head; public Light Lamp; public Transform[] Panels; public Transform HeadT;
        public bool Down { get; private set; }
        /// <summary>QF19: where its power beam goes (the platform); null = no beam.</summary>
        public Vector3? FeedTo;
        Transform _beam; Material _beamMat; ParticleSystem _sparks;
        Vector3 HeadPos => (HeadT != null ? HeadT.position : transform.position + Vector3.up * 2.6f) + Vector3.up * 0.3f;
        void MakeBeam()
        {
            if (_beam != null || !FeedTo.HasValue) return;
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = "PylonPowerBeam";
            Destroy(g.GetComponent<Collider>());
            _beamMat = Markers.Additive(1.1f); var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = _beamMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _beam = g.transform; _beam.SetParent(transform, true);
            var a = HeadPos; var b = FeedTo.Value; var d = b - a;
            _beam.position = (a + b) * 0.5f; _beam.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized); _beam.localScale = new Vector3(0.07f, d.magnitude * 0.5f, 0.07f);
        }
        void Update()
        {
            if (_beam != null && !Down && _beamMat != null && _beamMat.HasProperty("_Intensity")) _beamMat.SetFloat("_Intensity", 0.9f + 0.35f * Mathf.PerlinNoise(Time.time * 7f, transform.position.x));
        }
        void Burst()
        {
            if (_sparks == null)
            {
                var go = new GameObject("PylonSparks"); go.transform.SetParent(transform, false);
                var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main; main.loop = false; main.playOnAwake = false;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 1.1f); main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 7.5f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.22f); main.startColor = Color.white; main.gravityModifier = 1.2f;
                main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 300;
                var em = ps.emission; em.enabled = false;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.25f;
                var col = ps.colorOverLifetime; col.enabled = true; var gr = new Gradient();
                gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
                col.color = gr;
                var m = Markers.Additive(3.2f); if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", FxUtil.Dot(64));
                go.GetComponent<ParticleSystemRenderer>().sharedMaterial = m;
                _sparks = ps;
            }
            _sparks.transform.position = HeadPos; _sparks.Play(); _sparks.Emit(260);
            StartCoroutine(Ring());
        }
        public void Arm(bool on)
        {
            if (Down) return;
            if (Head != null) { var m = Head.material; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white * (on ? 2.6f : 0.15f)); }
            if (Lamp != null) Lamp.enabled = on;
            if (on) MakeBeam();
            if (_beam != null) _beam.gameObject.SetActive(on);
        }
        public void Collapse(bool instant = false)
        {
            if (Down) return;
            Down = true;
            if (instant) { Set(1f); return; }
            StartCoroutine(Fall());
        }
        void Set(float u)
        {
            if (Head != null) { var m = Head.material; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white * Mathf.Lerp(2.6f, 0f, Mathf.Clamp01(u * 3f))); }
            if (Lamp != null) { Lamp.intensity = Mathf.Lerp(Lamp.intensity, 0f, u); if (u >= 1f) Lamp.enabled = false; }
            if (Panels != null) for (int i = 0; i < Panels.Length; i++) if (Panels[i] != null) Panels[i].localRotation = Quaternion.Euler(Mathf.SmoothStep(0f, 1f, u) * 68f, 0f, 0f);
            if (HeadT != null) HeadT.localRotation = Quaternion.Euler(Mathf.SmoothStep(0f, 1f, u) * 24f, 0f, Mathf.SmoothStep(0f, 1f, u) * 12f);
        }
        /// <summary>QF19: a white shock ring bursts out from the head (0.5 s) - the bang you can see from across the plaza.</summary>
        IEnumerator Ring()
        {
            var g = new GameObject("PylonShockRing");
            g.AddComponent<MeshFilter>().sharedMesh = Markers.RingMesh(0.82f, 1f, 0f);
            var mat = Markers.Additive(2.4f); var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere); flash.name = "PylonFlash"; Destroy(flash.GetComponent<Collider>());
            var fm = Markers.Additive(3f); flash.GetComponent<MeshRenderer>().sharedMaterial = fm;
            var at = HeadPos;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f)
            {
                float e = 1f - (1f - t) * (1f - t);
                g.transform.position = at; g.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 4.5f, e);
                if (Camera.main != null) g.transform.rotation = Quaternion.LookRotation(Camera.main.transform.position - at) * Quaternion.Euler(90f, 0f, 0f);
                if (mat.HasProperty("_Intensity")) mat.SetFloat("_Intensity", 2.4f * (1f - t));
                flash.transform.position = at; flash.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.8f, e);
                if (fm.HasProperty("_Intensity")) fm.SetFloat("_Intensity", 3f * Mathf.Clamp01(1f - t * 2.5f));
                yield return null;
            }
            Destroy(g); Destroy(flash);
        }
        IEnumerator Fall()
        {
            // QF19: the bang at once - flash, sparks, the beam snaps off, then the power-down
            AudioKit.Play("land_hard", HeadPos, 1f, 0.05f, AudioKit.Bus.Sfx, 70f);
            AudioKit.Play("fight_block", HeadPos, 1f, 0.05f, AudioKit.Bus.Sfx, 60f);
            AudioKit.Play("mars_barrier_off", transform.position, 1f, 0.04f, AudioKit.Bus.Sfx, 60f);
            Burst();
            if (Lamp != null) { Lamp.enabled = true; Lamp.intensity = 14f; Lamp.range = Mathf.Max(Lamp.range, 12f); }
            if (Head != null) { var m0 = Head.material; m0.SetColor("_EmissionColor", Color.white * 6f); }
            for (float t = 0f; t < 0.08f; t += Time.deltaTime) yield return null;
            if (_beam != null) _beam.gameObject.SetActive(false);
            // sparks: the head stutters bright / dark, then dies
            for (int k = 0; k < 4; k++)
            {
                if (Head != null) { var m = Head.material; m.SetColor("_EmissionColor", Color.white * (k % 2 == 0 ? 4f : 0.2f)); }
                if (Lamp != null) Lamp.intensity = k % 2 == 0 ? 9f : 1f;
                if (k == 2) AudioKit.Play("fight_block", transform.position + Vector3.up * 2.4f, 0.6f, 0.1f, AudioKit.Bus.Sfx, 40f);
                for (float t = 0f; t < 0.06f + k * 0.012f; t += Time.deltaTime) yield return null;
            }
            AudioKit.Play("stone_fall", transform.position, 0.7f, 0.05f, AudioKit.Bus.Sfx, 40f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f) { Set(t); yield return null; }
            Set(1f);
        }
    }
}
