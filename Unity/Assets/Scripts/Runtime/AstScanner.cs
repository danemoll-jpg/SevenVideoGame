using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>The receiving scanner: an arm on the north wall with a lamp head; its cone of light follows what it is
    /// processing (7 at first; the marked container once she turns it with telekinesis).</summary>
    public class AstScanner : MonoBehaviour
    {
        public Transform Head; public Renderer Cone; public Light Spot;
        public Transform Target; public Vector3 TargetPoint; public bool Tracking = true;
        AudioSource _hum;
        void Start() { _hum = AudioKit.Loop("ast_scanner", transform, null, 0.4f, AudioKit.Bus.Sfx, 4f, 40f); }
        void LateUpdate()
        {
            if (Head == null) return;
            var at = Target != null ? Target.position + Vector3.up * 1.0f : TargetPoint;
            var want = Quaternion.LookRotation(at - Head.position);
            Head.rotation = Quaternion.Slerp(Head.rotation, want, Time.deltaTime * (Tracking ? 3f : 1.2f));
            if (Cone != null)
            {
                float d = Vector3.Distance(Head.position, at);
                Cone.transform.localScale = new Vector3(1f, 1f, d);
                var m = Cone.material; m.SetColor("_BaseColor", new Color(0.35f, 0.35f, 0.35f, 0.12f + 0.04f * Mathf.Sin(Time.time * 6f)));
            }
        }
        public IEnumerator SwingTo(Transform t, float secs = 1.2f)
        {
            AudioKit.Play("ast_scanner_turn", Head != null ? Head.position : transform.position, 1f, 0f, AudioKit.Bus.Sfx, 50f);
            Tracking = false; Target = t;
            for (float k = 0f; k < secs; k += Time.deltaTime) yield return null;
            Tracking = true;
        }
    }
}
