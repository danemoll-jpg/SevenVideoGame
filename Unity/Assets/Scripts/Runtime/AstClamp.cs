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
    }
}
