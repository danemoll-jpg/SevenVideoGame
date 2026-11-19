using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>Round 11 — small pale gliding shapes high in the haze over the landing platform (C01: the cat's head
    /// follows them).</summary>
    public class VenusFlyer : MonoBehaviour
    {
        public Vector3 Centre; public float Radius = 30f, Height = 40f, Speed = 0.12f, Phase;
        /// <summary>Round 13 (0i): small birds flap (wing beats, then a glide); 0 = the old gliders.</summary>
        public float Flap;
        Vector3 _scale0; bool _init;
        void Update()
        {
            if (!_init) { _init = true; _scale0 = transform.localScale; }
            if (Flap > 0f)
            {
                float cyc = Mathf.Repeat(Time.time * 0.35f + Phase, 1f);
                float beat = cyc < 0.55f ? Mathf.Sin(Time.time * Flap * 6.283f) : 0.15f;
                transform.localScale = new Vector3(_scale0.x * (0.55f + 0.45f * Mathf.Abs(beat)), _scale0.y + _scale0.x * 0.35f * beat, _scale0.z);
            }
            float a = Phase + Time.time * Speed;
            var p = Centre + new Vector3(Mathf.Cos(a) * Radius, Height + 3f * Mathf.Sin(a * 3.1f), Mathf.Sin(a) * Radius * 0.6f);
            var fwd = (p - transform.position); if (fwd.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(fwd) * Quaternion.Euler(0f, 0f, 12f * Mathf.Sin(a * 2f));
            transform.position = p;
        }
    }
}
