using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 12 — one of Wormmaster's three control devices in VEN_05 (spec section 7). While on it emits a repeated
    /// signal: a white ring expands from its head, a pulse travels along its cable to the floor, the signal sounds, and
    /// the creatures of its trough are driven along their route. Disabled (interaction, a climb + interaction, or
    /// telekinesis on its crystal - existing abilities only) it winds down and goes dark: that source of control is gone.
    /// Strictly white / gray light.
    /// </summary>
    public class ControlDevice : MonoBehaviour
    {
        public string Id;
        public Renderer Glow;          // the head's glowing core (white emission)
        public Transform Ring;         // the expanding pulse ring
        public Light Lamp;
        public float Period = 5.5f, Phase;
        public bool On { get; private set; } = true;
        public int Pulses { get; private set; }
        public event System.Action<ControlDevice> Pulsed;
        float _t, _off = -1f;
        Material _gm, _rm;

        void Start()
        {
            _t = Phase;
            if (Glow != null) { _gm = Glow.material; _gm.EnableKeyword("_EMISSION"); }
            if (Ring != null) { var r = Ring.GetComponent<Renderer>(); if (r != null) { _rm = r.material; _rm.EnableKeyword("_EMISSION"); } }
        }

        /// <summary>Set the device's state without the wind-down (a reload restores it).</summary>
        public void SetOff() { On = false; _off = 1f; Apply(0f, 0f); }

        /// <summary>Disable it: the pulse winds down and it goes dark.</summary>
        public void TurnOff()
        {
            if (!On) return;
            On = false; _off = 0f;
            AudioKit.Play("device_off", transform.position + Vector3.up * 2f, 0.9f, 0.03f, AudioKit.Bus.Sfx, 40f);
        }

        void Apply(float glow, float ring)
        {
            if (_gm != null) _gm.SetColor("_EmissionColor", Color.white * glow);
            if (Ring != null) { Ring.gameObject.SetActive(ring > 0.01f); if (_rm != null) _rm.SetColor("_EmissionColor", Color.white * ring); }
            if (Lamp != null) Lamp.intensity = glow * 0.6f;
        }

        void Update()
        {
            if (!On)
            {
                if (_off >= 0f && _off < 1f) { _off += Time.deltaTime / 2.2f; Apply(Mathf.Lerp(1.6f, 0f, _off) * (0.6f + 0.4f * Mathf.Sin(_off * 40f)), 0f); }
                return;
            }
            _t += Time.deltaTime;
            float u = (_t % Period) / Period;
            if (_t - Time.deltaTime >= 0f && Mathf.Floor((_t - Time.deltaTime) / Period) != Mathf.Floor(_t / Period))
            {
                Pulses++;
                AudioKit.Play("signal_pulse", transform.position + Vector3.up * 2f, 0.55f, 0.04f, AudioKit.Bus.Sfx, 45f);
                Pulsed?.Invoke(this);
            }
            float ring = u < 0.45f ? 1f - u / 0.45f : 0f;
            if (Ring != null) Ring.localScale = Vector3.one * Mathf.Lerp(0.4f, 5.5f, Mathf.Clamp01(u / 0.45f));
            Apply(0.45f + 0.55f * Mathf.Exp(-u * 8f), ring * 0.9f);
        }
    }
}
