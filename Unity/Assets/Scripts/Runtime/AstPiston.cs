using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>A piston / hammer / arm moving to and fro along a local direction (a repetitive, precise movement).</summary>
    public class AstPiston : MonoBehaviour
    {
        public Vector3 Travel = new Vector3(0f, 1f, 0f); public float Period = 2.4f, Phase; public bool Essential; public bool Snap;
        Vector3 _home; float _t;
        void Start() { _home = transform.localPosition; _t = Phase * Period; }
        void Update()
        {
            _t += Time.deltaTime * AstProduction.For(Essential);
            float u = (_t / Period) % 1f;
            float k = Snap ? (u < 0.18f ? u / 0.18f : u < 0.3f ? 1f : Mathf.Clamp01(1f - (u - 0.3f) / 0.7f)) : 0.5f - 0.5f * Mathf.Cos(u * Mathf.PI * 2f);
            transform.localPosition = _home + Travel * k;
        }
    }
}
