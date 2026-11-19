using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 14 — 7's bubble helmet (Dan, 2026-11-19: a simple transparent sphere / glass shader, not a generated model):
    /// a clear glass dome round her head (FightDemoBuilderEuropa builds the sphere + the "Europa_helmet_glass" material: almost
    /// clear, a pale rim and a bright highlight - monochrome). It rides on her head bone; <see cref="On"/> shows it whenever
    /// she is under water or somewhere unsafe; <see cref="Pulse"/> = the seal check (a soft white ring of light).
    /// </summary>
    public class BubbleHelmet : MonoBehaviour
    {
        public Transform Sphere;
        /// <summary>QF13 (6): the solid collar ring where the dome seals at her neck.</summary>
        public Transform Collar;
        public Animator Anim;
        public float Radius = 0.2f;
        public bool On { get => _on; set { _on = value; if (Sphere != null && Sphere.gameObject.activeSelf != value) Sphere.gameObject.SetActive(value); if (Collar != null && Collar.gameObject.activeSelf != value) Collar.gameObject.SetActive(value); } }
        bool _on = true; float _pulse;
        Transform _head; Renderer _r;

        void Start() { if (Anim != null && Anim.isHuman) _head = Anim.GetBoneTransform(HumanBodyBones.Head); if (Sphere != null) _r = Sphere.GetComponent<Renderer>(); }
        public void Pulse() { _pulse = 1f; }

        void LateUpdate()
        {
            if (Sphere == null || _head == null) return;
            Sphere.position = _head.position + _head.up * 0.06f + Anim.transform.forward * 0.02f;
            Sphere.rotation = Anim.transform.rotation;
            Sphere.localScale = Vector3.one * Radius * 2f;
            if (Collar != null)
            {
                // the ring sits where the dome meets her neck (the dome's circle of radius 0.148 m, below its centre)
                float r = Radius, rc = 0.148f * Radius / 0.2f, below = Mathf.Sqrt(Mathf.Max(0f, r * r - rc * rc));
                Collar.SetPositionAndRotation(Sphere.position - Anim.transform.up * below, Anim.transform.rotation);
                Collar.localScale = Vector3.one * (Radius / 0.2f);
            }
            if (_pulse > 0f && _r != null)
            {
                _pulse = Mathf.MoveTowards(_pulse, 0f, Time.deltaTime * 1.2f);
                _r.material.SetFloat("_Pulse", _pulse);
            }
        }
    }
}
