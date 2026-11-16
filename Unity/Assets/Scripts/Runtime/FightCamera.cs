using UnityEngine;

namespace Seven
{
    /// <summary>Fixed 3/4 side view that frames both fighters full-body and follows gently.</summary>
    public class FightCamera : MonoBehaviour
    {
        public Transform A, B;
        public Vector3 ViewEuler = new Vector3(9f, 18f, 0f);   // fixed orientation (pitch, yaw)
        public float MinDistance = 3.6f, PerMetre = 0.85f, MaxDistance = 9f;
        public float LookHeight = 0.95f, Smooth = 0.45f;
        [System.NonSerialized] public bool Hold;               // check runs park the camera for close-ups

        Vector3 _vel;
        float _shake;

        public void Shake(float amount) => _shake = Mathf.Max(_shake, amount);

        Vector3 Wanted()
        {
            var mid = (A.position + B.position) * 0.5f + Vector3.up * LookHeight;
            var sep = Vector3.Distance(new Vector3(A.position.x, 0, A.position.z), new Vector3(B.position.x, 0, B.position.z));
            float dist = Mathf.Clamp(MinDistance + sep * PerMetre, MinDistance, MaxDistance);
            return mid - Quaternion.Euler(ViewEuler) * Vector3.forward * dist;
        }

        void Start()
        {
            transform.rotation = Quaternion.Euler(ViewEuler);
            if (A != null && B != null) transform.position = Wanted();
        }

        void LateUpdate()
        {
            if (Hold || A == null || B == null) return;
            transform.rotation = Quaternion.Euler(ViewEuler);
            transform.position = Vector3.SmoothDamp(transform.position, Wanted(), ref _vel, Smooth);
            if (_shake > 0f)
            {
                transform.position += Random.insideUnitSphere * _shake;
                _shake = Mathf.MoveTowards(_shake, 0f, Time.deltaTime * 0.4f);
            }
        }
    }
}
