using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>A causeway rail junction: a round riveted turntable carrying a road section. Misaligned it lies along the
    /// carriers' rails; aligned (AST_03) it turns 90 degrees into line with the causeway - the route lights run ahead.</summary>
    public class AstTurntable : MonoBehaviour
    {
        public float Misaligned = 90f; public bool Aligned;
        public Renderer Lamp; public PowerLine RouteLights;
        public void Set(bool aligned) { Aligned = aligned; transform.localRotation = Quaternion.Euler(0f, aligned ? 0f : Misaligned, 0f); if (Lamp != null) Glowing.Set(Lamp, aligned ? 1.6f : 0.15f); if (RouteLights != null) RouteLights.All(aligned); }
        public IEnumerator Turn(float secs = 2.6f)
        {
            AudioKit.Play("ast_junction_turn", transform.position, 1f, 0f, AudioKit.Bus.Sfx, 90f);
            float a0 = Misaligned;
            for (float t = 0f; t < 1f; t += Time.deltaTime / secs) { transform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(a0, 0f, Mathf.SmoothStep(0f, 1f, t)), 0f); yield return null; }
            Set(true);
            CameraShake.Quake(0.25f, 0.25f);
            if (RouteLights != null) { RouteLights.Set(-1); AudioKit.Play("ast_route_lights", transform.position, 0.9f, 0f, AudioKit.Bus.Sfx, 90f); yield return RouteLights.Run(0, RouteLights.Count - 1, 1.6f); }
        }
    }
}
