using UnityEngine;

namespace Seven
{
    /// <summary>QF18 (21): wind-blown dust on Mars - soft grey motes drifting across the ground and through the air round the
    /// camera (world-space particles, carried by a steady wind). Monochrome, additive, faint.</summary>
    public class MarsDust : MonoBehaviour
    {
        public Material Mat;
        ParticleSystem _ps;
        void Start()
        {
            _ps = gameObject.AddComponent<ParticleSystem>();
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _ps.main; main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f); main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 2.4f); main.startColor = new Color(1f, 1f, 1f, 0.16f);
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 700; main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            var em = _ps.emission; em.rateOverTime = 70f;
            var sh = _ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(70f, 6f, 70f); sh.position = new Vector3(0f, 2f, 0f);
            var vel = _ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(2.4f, 5.2f); vel.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.35f); vel.z = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            var col = _ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            _ps.Play();
        }
        void LateUpdate()
        {
            var c = Camera.main; if (c == null) return;
            var p = c.transform.position; p.y = Ground.Height(p.x, p.z);
            transform.position = p;
        }
    }
}
