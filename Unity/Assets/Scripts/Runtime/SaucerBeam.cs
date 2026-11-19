using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 13 (0b) - the saucer's fixed design: there is no ramp. 7 (and the cat) get in and out through the tractor
    /// beam from the iris hatch in the belly, on every world. Built by FightDemoBuilder.BuildSaucerBeam (shared by Chapter 1
    /// and Venus); this drives it: the iris, the beam's light cone, and floating someone up into the hatch or down from it.
    /// </summary>
    public class SaucerBeam : MonoBehaviour
    {
        public Transform Hatch, Beam;
        public Light BeamLight;
        public Renderer BeamFloor, HatchGlow;
        public float HatchRadius = 0.9f, BellyY = 1.15f;
        /// <summary>QF13 (16): the one saucer prefab's hull underside above its root (its hover height on every world; on the
        /// Moon the clamped ship sits HoverLift lower until it lifts off).</summary>
        public const float ShipBelly = 3.35f;
        public bool IsOn { get; private set; }

        /// <summary>World point on the ground under the hatch.</summary>
        public Vector3 Centre => new Vector3(transform.position.x, Ground.Height(transform.position.x, transform.position.z), transform.position.z);
        public Vector3 HatchPoint => transform.TransformPoint(new Vector3(0f, BellyY - 0.1f, 0f));

        void Glow(Renderer r, float v) { if (r == null) return; var m = r.material; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white * v); }

        public IEnumerator Iris(bool open)
        {
            if (Hatch == null) yield break;
            AudioKit.Play("hatch_iris", Hatch.position, 1f, 0f, AudioKit.Bus.Sfx, 25f);
            Glow(HatchGlow, open ? 3f : 0.4f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.1f)
            {
                float u = Mathf.SmoothStep(0f, 1f, open ? t : 1f - t);
                for (int i = 0; i < Hatch.childCount; i++)
                {
                    var b = Hatch.GetChild(i);
                    b.localRotation = Quaternion.Euler(0f, i * 45f + u * 50f, 0f);
                    b.localPosition = b.localRotation * new Vector3(HatchRadius * 0.95f * u, 0.01f * u, 0f);
                }
                yield return null;
            }
        }

        public IEnumerator BeamOn(bool on)
        {
            if (Beam == null) yield break;
            IsOn = on;
            if (on) { Beam.gameObject.SetActive(true); AudioKit.Play("tractor_beam", Centre + Vector3.up * 2f, 0.8f, 0f, AudioKit.Bus.Sfx, 35f); }
            for (float t = 0f; t < 1f; t += Time.deltaTime / (on ? 1.3f : 1.6f))
            {
                float u = Mathf.SmoothStep(0f, 1f, on ? t : 1f - t);
                Beam.localScale = new Vector3(Mathf.Lerp(0.6f, 1f, u), Mathf.Max(0.001f, u), Mathf.Lerp(0.6f, 1f, u));
                if (BeamLight != null) BeamLight.intensity = 3.2f * u;
                yield return null;
            }
            if (!on) Beam.gameObject.SetActive(false);
        }

        /// <summary>Open the iris and switch the beam on (or the reverse).</summary>
        public IEnumerator Open(bool open)
        {
            if (open) { yield return Iris(true); yield return BeamOn(true); }
            else { yield return BeamOn(false); yield return Iris(false); }
        }

        /// <summary>Float someone up the beam into the hatch (upright, slow, a gentle turn); hidden at the top.</summary>
        public IEnumerator Rise(Transform who, float seconds, float turn, System.Action<float> each = null)
        {
            var p0 = who.position; var c = Centre; float top = HatchPoint.y + 0.2f; var r0 = who.rotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                var p = Vector3.Lerp(new Vector3(p0.x, 0f, p0.z), c, Mathf.Clamp01(t * 2.5f));
                p.y = Mathf.Lerp(p0.y, top, u);
                who.position = p; who.rotation = r0 * Quaternion.Euler(0f, turn * u, 0f);
                each?.Invoke(t);
                yield return null;
            }
            who.gameObject.SetActive(false);
        }

        /// <summary>Lower someone from the hatch down the beam to the ground at its centre, then a step out to 'stepTo'.</summary>
        public IEnumerator Lower(Transform who, float seconds, Vector3 facing, System.Action<float> each = null)
        {
            var c = Centre; float top = HatchPoint.y - 0.2f;
            who.gameObject.SetActive(true);
            facing.y = 0f; var rot = facing.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(facing) : who.rotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                who.position = new Vector3(c.x, Mathf.Lerp(top, c.y, u), c.z);
                who.rotation = rot * Quaternion.Euler(0f, -30f * (1f - u), 0f);
                each?.Invoke(t);
                yield return null;
            }
            who.position = c; who.rotation = rot;
        }
    }
}
