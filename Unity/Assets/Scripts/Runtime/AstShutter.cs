using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>A heavy security shutter / door: built in its closed place (in the doorway); open = raised Drop m into the
    /// lintel; closing = it comes down (accelerating, a thud) - riveted plates.</summary>
    public class AstShutter : MonoBehaviour
    {
        public float Drop = 6f; public bool Closed;
        Vector3 _up; bool _init;
        void Init() { if (_init) return; _init = true; _up = transform.localPosition; }
        public void Set(bool closed) { Init(); Closed = closed; transform.localPosition = _up + Vector3.up * (closed ? 0f : Drop); }
        public IEnumerator Move(bool close, float secs)
        {
            Init(); Closed = close;
            var a = transform.localPosition; var b = _up + Vector3.up * (close ? 0f : Drop);
            for (float t = 0f; t < 1f; t += Time.deltaTime / secs) { transform.localPosition = Vector3.Lerp(a, b, close ? t * t : Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            transform.localPosition = b;
            if (close) { CameraShake.Quake(0.25f, 0.2f); AudioKit.Play("land_hard", transform.position, 0.8f, 0.05f, AudioKit.Bus.Sfx, 60f); }
        }
    }
}
