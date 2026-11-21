using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>A wheel / flywheel / gear turning about a local axis.</summary>
    public class AstSpin : MonoBehaviour
    {
        public Vector3 Axis = Vector3.forward; public float Speed = 30f; public bool Essential;
        void Update() { transform.Rotate(Axis, Speed * AstProduction.For(Essential) * Time.deltaTime, Space.Self); }
    }
}
