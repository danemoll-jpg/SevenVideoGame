using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF18 (15, 19b): one of Viper's pylons - angular dark metal, a hard white emitter head, three hinged armour panels.
    /// Its done state is unmistakable: the light dies (head and lamp), sparks and a power-down sound, the panels fall open,
    /// the head slumps; the prompt is gone and it can never be used again.
    /// </summary>
    public class MarsPylon : MonoBehaviour
    {
        public Renderer Head; public Light Lamp; public Transform[] Panels; public Transform HeadT;
        public bool Down { get; private set; }
        public void Arm(bool on)
        {
            if (Down) return;
            if (Head != null) { var m = Head.material; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white * (on ? 2.6f : 0.15f)); }
            if (Lamp != null) Lamp.enabled = on;
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
        IEnumerator Fall()
        {
            AudioKit.Play("mars_barrier_off", transform.position, 1f, 0.04f, AudioKit.Bus.Sfx, 60f);
            // sparks: the head stutters bright / dark, then dies
            for (int k = 0; k < 7; k++)
            {
                if (Head != null) { var m = Head.material; m.SetColor("_EmissionColor", Color.white * (k % 2 == 0 ? 4f : 0.2f)); }
                if (Lamp != null) Lamp.intensity = k % 2 == 0 ? 9f : 1f;
                if (k == 2) AudioKit.Play("fight_block", transform.position + Vector3.up * 2.4f, 0.6f, 0.1f, AudioKit.Bus.Sfx, 40f);
                for (float t = 0f; t < 0.06f + k * 0.012f; t += Time.deltaTime) yield return null;
            }
            AudioKit.Play("stone_fall", transform.position, 0.7f, 0.05f, AudioKit.Bus.Sfx, 40f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.9f) { Set(t); yield return null; }
            Set(1f);
        }
    }
}
