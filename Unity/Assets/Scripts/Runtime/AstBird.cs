using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>AST_03: the little mechanical bird - one wing repaired several times with mismatched pieces. A small winding
    /// key; wound, it gives one uneven chirp and flaps a wing (the same sound as in the vision).</summary>
    public class AstBird : MonoBehaviour
    {
        public Transform Key, WingL, WingR, Head;
        public IEnumerator WindAndChirp()
        {
            for (float t = 0f; t < 1.1f; t += Time.deltaTime) { if (Key != null) Key.localRotation = Quaternion.Euler(0f, 0f, t * 540f); yield return null; }
            AudioKit.Play("ast_bird_chirp", transform.position, 1f, 0f, AudioKit.Bus.Sfx, 15f);
            for (float t = 0f; t < 1.4f; t += Time.deltaTime)
            {
                // the patched wing flaps unevenly, the good one barely moves; the head tips up on the chirp
                if (WingR != null) WingR.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Abs(Mathf.Sin(t * 17f)) * 55f * (1f - t / 1.4f) * (t % 0.5f < 0.32f ? 1f : 0.4f));
                if (WingL != null) WingL.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 9f) * 6f * (1f - t / 1.4f));
                if (Head != null) Head.localRotation = Quaternion.Euler(-22f * Mathf.Sin(Mathf.Clamp01(t / 0.3f) * Mathf.PI), 0f, 0f);
                yield return null;
            }
            if (WingR != null) WingR.localRotation = Quaternion.identity; if (WingL != null) WingL.localRotation = Quaternion.identity; if (Head != null) Head.localRotation = Quaternion.identity;
        }
    }
}
