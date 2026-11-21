using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>A service-station clamp (AST_07): two heavy jaws that slam shut on a tool assembly; the maintenance cradle
    /// uses the same piece with padded arms.</summary>
    public class AstClamp : MonoBehaviour
    {
        public Transform JawA, JawB; public Renderer Lamp; public float Open = 50f; public bool Closed;
        public string Sound = "ast_clamp";
        public void Set(bool closed) { Closed = closed; float a = closed ? 0f : Open; if (JawA != null) JawA.localRotation = Quaternion.Euler(0f, 0f, a); if (JawB != null) JawB.localRotation = Quaternion.Euler(0f, 0f, -a); Glowing.Set(Lamp, closed ? 1.8f : 0.5f); }
        public IEnumerator Close(float secs = 0.35f)
        {
            AudioKit.Play(Sound, transform.position, 1f, 0f, AudioKit.Bus.Sfx, 70f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / secs) { float a = Open * (1f - t * t); if (JawA != null) JawA.localRotation = Quaternion.Euler(0f, 0f, a); if (JawB != null) JawB.localRotation = Quaternion.Euler(0f, 0f, -a); yield return null; }
            Set(true); CameraShake.Quake(0.4f, 0.25f);
        }
        /// <summary>QF21 (11): the jaws / padded arms swing open visibly (the release), the lamp dims.</summary>
        public IEnumerator OpenUp(float secs = 1.2f)
        {
            AudioKit.Play(Sound, transform.position, 0.8f, 0f, AudioKit.Bus.Sfx, 70f);
            AudioKit.Play("machinist_hiss", transform.position + Vector3.up * 3f, 0.7f, 0f, AudioKit.Bus.Sfx, 40f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / secs) { float a = Open * Mathf.SmoothStep(0f, 1f, t); if (JawA != null) JawA.localRotation = Quaternion.Euler(0f, 0f, a); if (JawB != null) JawB.localRotation = Quaternion.Euler(0f, 0f, -a); yield return null; }
            Set(false);
        }
        /// <summary>The pad centres (the cradle's padded arms; the jaws' working ends) - QF21 (11) clamp check.</summary>
        public Vector3 PadA => JawA != null ? JawA.TransformPoint(new Vector3(1.9f, 0f, 0f)) : transform.position;
        public Vector3 PadB => JawB != null ? JawB.TransformPoint(new Vector3(-1.9f, 0f, 0f)) : transform.position;
    }
}
