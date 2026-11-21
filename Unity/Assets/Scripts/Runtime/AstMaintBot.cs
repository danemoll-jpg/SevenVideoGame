using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>The small maintenance machine of the receiving chamber: a riveted body on a hover ring, a lens that scans,
    /// a projector that throws a white arrow on the floor toward the conveyor. An obstacle, never an enemy: while the scanner
    /// holds 7 it keeps itself between her and the manual door control.</summary>
    public class AstMaintBot : MonoBehaviour
    {
        public Transform Lens; public Renderer Arrow; public Renderer LensGlow;
        public Vector3 Goal; public float Speed = 2.2f;
        public bool Busy;
        float _bob;
        void Update()
        {
            var p = transform.position; var g = new Vector3(Goal.x, p.y, Goal.z);
            var to = g - p;
            if (to.magnitude > 0.05f)
            {
                transform.position = Vector3.MoveTowards(p, g, Speed * Time.deltaTime);
                var f = new Vector3(to.x, 0f, to.z); if (f.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(f), Time.deltaTime * 4f);
            }
            _bob += Time.deltaTime;
            if (Lens != null) Lens.localRotation = Quaternion.Euler(Mathf.Sin(_bob * 1.7f) * 12f, Mathf.Sin(_bob * 1.1f) * 25f, 0f);
            if (LensGlow != null) Glowing.Set(LensGlow, 0.9f + 0.5f * Mathf.Sin(_bob * 5f));
        }
        public void Face(Vector3 at) { var f = at - transform.position; f.y = 0f; if (f.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(f); }
        public void ShowArrow(bool on, Vector3 at = default, Vector3 dir = default)
        {
            if (Arrow == null) return;
            Arrow.gameObject.SetActive(on);
            if (on) { Arrow.transform.position = at + Vector3.up * 0.03f; Arrow.transform.rotation = Quaternion.LookRotation(dir.sqrMagnitude > 0.01f ? dir : Vector3.forward); Glowing.Set(Arrow, 1.4f); }
        }
    }
}
