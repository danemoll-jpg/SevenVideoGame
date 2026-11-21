using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 17 — a memory echo (BIA_03, BIA_06, BIA_07): archived memories leaking into the living network through the
    /// damaged relays. Translucent shimmering white light shapes (additive, never solid, no shadows, no collision): they fade in
    /// with a soft rising tone, waver (a fine flicker and a slow drift of horizontal scan bands), and fade out. Clearly
    /// distinct from solid structures (no shading, see-through) and from her visions (those are full-screen films).
    /// Walking into one does nothing - you can't stand on a remembered bridge or walk through a remembered door.
    /// </summary>
    public class MemoryEcho : MonoBehaviour
    {
        public Renderer[] Parts;
        public float Strength = 0.55f;
        public float Shown { get; private set; }
        float _target; MaterialPropertyBlock _mpb; static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        public bool Visible => Shown > 0.05f;

        void Awake()
        {
            if (Parts == null || Parts.Length == 0) Parts = GetComponentsInChildren<Renderer>(true);
            _mpb = new MaterialPropertyBlock();
            foreach (var r in Parts) if (r != null) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; }
            Apply(0f);
        }
        public void Set(bool on, bool sound = true)
        {
            _target = on ? 1f : 0f;
            if (on && sound) AudioKit.Play("bia_echo", transform.position + Vector3.up, 0.8f, 0.05f, AudioKit.Bus.Sfx, 40f);
        }
        public void SetNow(bool on) { _target = on ? 1f : 0f; Shown = _target; Apply(Shown); }
        public IEnumerator Run(float hold)
        {
            Set(true); for (float t = 0f; t < hold + 1.2f; t += Time.deltaTime) yield return null;
            Set(false); for (float t = 0f; t < 1.4f; t += Time.deltaTime) yield return null;
        }
        void Update()
        {
            Shown = Mathf.MoveTowards(Shown, _target, Time.deltaTime / 1.2f);
            Apply(Shown);
        }
        void Apply(float k)
        {
            if (Parts == null) return;
            for (int i = 0; i < Parts.Length; i++)
            {
                var r = Parts[i]; if (r == null) continue;
                bool on = k > 0.01f; if (r.enabled != on) r.enabled = on; if (!on) continue;
                // the shimmer: a fine flicker per part + a slow band of brightness rising through the echo
                float band = 0.5f + 0.5f * Mathf.Sin(Time.time * 1.7f - r.bounds.center.y * 1.3f + i);
                float flick = 0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 9f, i * 1.37f);
                float a = Strength * k * flick * Mathf.Lerp(0.6f, 1f, band);
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat(IntensityId, a * 1.6f);
                r.SetPropertyBlock(_mpb);
            }
            transform.localScale = Vector3.one * (1f + 0.004f * Mathf.Sin(Time.time * 2.2f));
        }
    }
}
