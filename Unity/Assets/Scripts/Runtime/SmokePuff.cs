using System.Collections;
using UnityEngine;

namespace Seven
{
    public class SmokePuff : MonoBehaviour
    {
        public float Life = 2f; float _t;
        void Update()
        {
            _t += Time.deltaTime; if (_t > Life) { Destroy(gameObject); return; }
            transform.position += Vector3.up * Time.deltaTime * 0.6f; transform.localScale = Vector3.one * (1f + _t * 1.4f);
            var r = GetComponent<MeshRenderer>(); if (r != null && r.material.HasProperty("_Intensity")) r.material.SetFloat("_Intensity", 0.25f * (1f - _t / Life));
        }
    }
}
