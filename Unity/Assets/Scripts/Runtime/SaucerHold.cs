using System;
using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF13 (1, 5, 16) - the saucer's bike hold, the same on every world (part of the one saucer prefab,
    /// Assets/Saucer/Saucer.prefab). The bike always leaves and returns through it; the beam is only for 7 (and the cat).
    /// Opening: the hair-thin seam lights, the panel sinks into the hull (scaled about the hull's mid-plane, so it never
    /// pokes out under the neighbouring hull) and slides round inside it; the seam goes dark again as the panel clears the
    /// opening - what is left is an empty opening (the hold's inside is one flat unlit tone, Seven/HoldInterior) with the bike.
    /// Closing is the reverse; closed, the hull is seamless.
    /// </summary>
    public class SaucerHold : MonoBehaviour
    {
        public Transform Panel;
        public Renderer Seam;
        public Light BayLight;
        public const float Angle = 41.25f, Span = 30f, Tuck = 0.97f;
        public bool IsOpen { get; private set; }
        public float Slide { get; private set; }
        Material _seamM;

        public Vector3 Dir => transform.TransformDirection(new Vector3(Mathf.Cos(Angle * Mathf.Deg2Rad), 0f, Mathf.Sin(Angle * Mathf.Deg2Rad)));
        /// <summary>The middle of the opening at the rim (world).</summary>
        public Vector3 Mouth => transform.position + Dir * 6f + transform.up * (SaucerBeam.ShipBelly + 0.68f);
        /// <summary>Where the bike rests inside (ground point under it) and where it is clear of the hull outside.</summary>
        public Vector3 Inside => transform.position + Dir * 3.4f;
        public Vector3 Outside => transform.position + Dir * 8.9f;
        /// <summary>The bike's underside height above the saucer's root while it is in the hold.</summary>
        public float BottomIn => SaucerBeam.ShipBelly + 0.1f;

        Material SeamM() { if (_seamM == null && Seam != null) _seamM = Seam.material; return _seamM; }
        void SeamGlow(float v) { var m = SeamM(); if (m != null) m.SetColor("_EmissionColor", Color.white * v); }
        void Pose(float sink, float slide)
        {
            if (Panel == null) return;
            Panel.localScale = Vector3.one * Mathf.Lerp(1f, Tuck, sink);
            Panel.localRotation = Quaternion.Euler(0f, slide * (Span + 3f), 0f);
            Slide = slide;
        }

        /// <summary>Open the hold. onSeam: the seam is fully lit (before the panel moves); onSlide(0..1) every frame of the slide.</summary>
        public IEnumerator Open(Action onSeam = null, Action<float> onSlide = null)
        {
            if (Seam != null) Seam.enabled = true;
            AudioKit.Play("hatch_iris", Mouth, 0.7f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f) { SeamGlow(Mathf.SmoothStep(0f, 3.2f, t)); yield return null; }
            onSeam?.Invoke();
            // the panel sinks into the hull while the seam goes dark - no lit outline round the opening
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f) { Pose(Mathf.SmoothStep(0f, 1f, t), 0f); SeamGlow(Mathf.Lerp(3.2f, 0f, t)); yield return null; }
            SeamGlow(0f); if (Seam != null) Seam.enabled = false;
            // then it slides round inside the hull, quickly
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.9f)
            {
                float slide = Mathf.SmoothStep(0f, 1f, t);
                Pose(1f, slide);
                if (BayLight != null) BayLight.intensity = 2.4f * slide;
                onSlide?.Invoke(slide);
                yield return null;
            }
            Pose(1f, 1f); SeamGlow(0f);
            if (Seam != null) Seam.enabled = false;
            onSlide?.Invoke(1f);
            IsOpen = true;
        }

        public IEnumerator Close()
        {
            AudioKit.Play("hatch_iris", Mouth, 0.6f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.6f)
            {
                float slide = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.75f)), sink = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.7f) / 0.3f));
                Pose(sink, slide);
                if (BayLight != null) BayLight.intensity = 2.4f * slide;
                yield return null;
            }
            Pose(0f, 0f);
            if (Panel != null) { Panel.localScale = Vector3.one; Panel.localRotation = Quaternion.identity; }
            if (BayLight != null) BayLight.intensity = 0f;
            SeamGlow(0f); if (Seam != null) Seam.enabled = false;
            IsOpen = false;
        }

        /// <summary>Closed at once (scene start / reload).</summary>
        public void Shut()
        {
            Pose(0f, 0f); if (Panel != null) { Panel.localScale = Vector3.one; Panel.localRotation = Quaternion.identity; }
            if (BayLight != null) BayLight.intensity = 0f;
            SeamGlow(0f); if (Seam != null) Seam.enabled = false;
            IsOpen = false;
        }
    }
}
