using UnityEngine;

namespace Seven
{
    /// <summary>A slow spin (pickup pads, the trophy...).</summary>
    public class GcSpin : MonoBehaviour { public float Deg = 90f; void Update() { var g = transform.Find("PadGem"); if (g != null) g.localRotation = Quaternion.Euler(0f, Time.time * Deg, 0f); } }
}
