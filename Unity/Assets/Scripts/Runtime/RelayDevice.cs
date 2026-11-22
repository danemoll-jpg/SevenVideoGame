using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF24 (10) - the mothership's communication relays, redesigned as Biavian technology for a telepathic people: a slender
    /// pearly pylon rising from the floor, a glowing crystal NODE at the top inside a ring of smooth petals, a small resonance
    /// PLATE on its front where she lays her palm. Damaged (Ratwing's work) = the node dark and cracked, the petals jammed
    /// half-open at odd angles, faint white sparks. Restored = the node lights, the cracks fade, the petals open fully and a wave
    /// of light runs out along the floor channel toward the district it reconnects (the payoff).
    /// Built by FightDemoBuilderBiavehQF24.RelayPylon; driven by the director (BiavehDirector.RelayTouch).
    /// </summary>
    public class RelayDevice : MonoBehaviour
    {
        public Renderer Node, PlateGlow; public GameObject Cracks; public Transform Plate;
        public Transform[] Petals; public float[] JamDeg; public float OpenDeg = 62f;
        public Renderer[] Channel;
        public bool Restored { get; private set; }
        float _spark;
        public Vector3 PlateNormal => Plate != null ? Plate.forward : transform.forward;
        public Vector3 PlateUp => Plate != null ? Plate.up : Vector3.up;

        void Start() { if (!Restored) Set(false); }

        static void Glow(Renderer r, float v) { if (r != null) Glowing.Set(r, v); }
        void PetalsTo(float k)
        {
            if (Petals == null) return;
            for (int i = 0; i < Petals.Length; i++)
                if (Petals[i] != null)
                {
                    Petals[i].localRotation = Quaternion.Euler(0f, i * 360f / Petals.Length, 0f);
                    if (Petals[i].childCount > 0) Petals[i].GetChild(0).localRotation = Quaternion.Euler(Mathf.Lerp(JamDeg != null && i < JamDeg.Length ? JamDeg[i] : 20f, OpenDeg, k), 0f, 0f);
                }
        }
        /// <summary>At once: broken (false) or restored (true) - for checkpoints / revisits.</summary>
        public void Set(bool restored)
        {
            Restored = restored;
            Glow(Node, restored ? 1.8f : 0.12f); Glow(PlateGlow, restored ? 0.6f : 0.25f);
            if (Cracks != null) Cracks.SetActive(!restored);
            PetalsTo(restored ? 1f : 0f);
            if (Channel != null) foreach (var c in Channel) Glow(c, restored ? 0.7f : 0.05f);
        }
        /// <summary>The payoff: the node lights (the cracks fade), the petals open, a wave of light runs along the floor channel.</summary>
        public IEnumerator Restore()
        {
            if (Restored) yield break;
            Restored = true;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.9f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Glow(Node, Mathf.Lerp(0.12f, 2.6f, u)); Glow(PlateGlow, Mathf.Lerp(0.25f, 1.2f, u));
                PetalsTo(u);
                if (Cracks != null && t > 0.45f) Cracks.SetActive(false);
                yield return null;
            }
            if (Channel != null)
                for (float t = 0f; t < 1.6f; t += Time.deltaTime)
                {
                    float front = t / 1.6f * (Channel.Length + 3);
                    for (int i = 0; i < Channel.Length; i++) { float d = front - i; Glow(Channel[i], d < 0f ? 0.05f : d < 1.5f ? Mathf.Lerp(2.4f, 0.7f, d / 1.5f) : 0.7f); }
                    yield return null;
                }
            Glow(Node, 1.8f); Glow(PlateGlow, 0.6f);
            if (Channel != null) foreach (var c in Channel) Glow(c, 0.7f);
        }
        /// <summary>Broken: faint white sparks round the jammed petals every so often, a flicker in the dead node.</summary>
        void Update()
        {
            if (Restored || Node == null) return;
            _spark -= Time.deltaTime;
            if (_spark <= 0f)
            {
                _spark = Random.Range(1.4f, 3.2f);
                StartCoroutine(Spark());
            }
        }
        IEnumerator Spark()
        {
            var at = Node.transform.position + Random.insideUnitSphere * 0.14f;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(go.GetComponent<Collider>());
            go.name = "RelaySpark"; go.transform.position = at; go.transform.localScale = Vector3.one * 0.03f;
            var r = go.GetComponent<Renderer>(); r.sharedMaterial = Markers.Additive(2.5f); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Glow(Node, 0.5f);
            if (Random.value < 0.5f) AudioKit.Play("spark", at, 0.25f, 0.1f, AudioKit.Bus.Sfx, 8f);
            var v = Random.onUnitSphere * 0.6f + Vector3.up * 0.3f;
            for (float t = 0f; t < 0.25f; t += Time.deltaTime) { go.transform.position += v * Time.deltaTime; go.transform.localScale = Vector3.one * Mathf.Lerp(0.03f, 0.005f, t / 0.25f); yield return null; }
            Destroy(go); if (!Restored) Glow(Node, 0.12f);
        }
    }
}
