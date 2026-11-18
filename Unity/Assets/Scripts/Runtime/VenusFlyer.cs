using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>Round 11 — small pale gliding shapes high in the haze over the landing platform (C01: the cat's head
    /// follows them).</summary>
    public class VenusFlyer : MonoBehaviour
    {
        public Vector3 Centre; public float Radius = 30f, Height = 40f, Speed = 0.12f, Phase;
        void Update()
        {
            float a = Phase + Time.time * Speed;
            var p = Centre + new Vector3(Mathf.Cos(a) * Radius, Height + 3f * Mathf.Sin(a * 3.1f), Mathf.Sin(a) * Radius * 0.6f);
            var fwd = (p - transform.position); if (fwd.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(fwd) * Quaternion.Euler(0f, 0f, 12f * Mathf.Sin(a * 2f));
            transform.position = p;
        }
    }
}
