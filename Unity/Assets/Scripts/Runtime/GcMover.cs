using UnityEngine;

namespace Seven
{
    /// <summary>Round 21 (Grand Circuit Stage 2): scenery that moves on a fixed rhythm - a spin about a local axis (the foundry's great
    /// flywheel, gears), or a shuttle back and forth along a straight run (the rail carrier's car, conveyor crates). Decoration only:
    /// the racers ride in track coordinates and never touch it.</summary>
    public class GcMover : MonoBehaviour
    {
        public enum Kind { Spin, Shuttle }
        public Kind Mode = Kind.Spin;
        public Vector3 Axis = Vector3.forward; public float DegPerSec = 30f;
        public Vector3 From, To; public float Period = 12f, Phase;
        Quaternion _rest; bool _init;
        void Update()
        {
            if (!_init) { _rest = transform.localRotation; _init = true; }
            if (Mode == Kind.Spin) transform.localRotation = _rest * Quaternion.AngleAxis(Time.time * DegPerSec, Axis);
            else { float u = Mathf.PingPong((Time.time + Phase) * 2f / Mathf.Max(0.1f, Period), 1f); transform.localPosition = Vector3.Lerp(From, To, Mathf.SmoothStep(0f, 1f, u)); }
        }
    }
}
