using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 7 (6b): the small moving things that give the city its scale — lights / carts creeping along Tunneler's
    /// conveyors, freight lifts sliding up and down the walls, excavator arms biting into terraces — plus the staged
    /// moment where a distant section of machinery starts moving and stone breaks off an ancient terrace.
    /// Built by FightDemoBuilderChapter1 (BuildCity); everything here is tiny against the buildings and strictly white.
    /// </summary>
    public class CityMotion : MonoBehaviour
    {
        [System.Serializable] public class Conveyor { public Vector3[] Path; public Transform[] Carts; public float Speed = 9f; public bool Quiet; }
        [System.Serializable] public class Lift { public Transform Car; public Vector3 Bottom, Top; public float Period = 40f, Phase; }
        [System.Serializable] public class Arm { public Transform Pivot; public float Swing = 28f, Period = 14f, Phase; public bool Quiet; }

        public Conveyor[] Conveyors = new Conveyor[0];
        public Lift[] Lifts = new Lift[0];
        public Arm[] Arms = new Arm[0];
        [Header("Staged: machinery starts, stone falls")]
        public Transform FallingStone;
        public Vector3 StoneLandAt;
        public ParticleSystem Dust;
        /// <summary>The "quiet" district wakes up when the reveal asks for it (StartMachinery).</summary>
        public bool QuietDistrictRunning;
        public bool StoneFallen { get; private set; }

        readonly List<float> _convT = new List<float>();
        Quaternion[] _armRest;

        void Start()
        {
            foreach (var c in Conveyors) _convT.Add(Random.value * 100f);
            _armRest = new Quaternion[Arms.Length];
            for (int i = 0; i < Arms.Length; i++) _armRest[i] = Arms[i].Pivot != null ? Arms[i].Pivot.localRotation : Quaternion.identity;
            // Every cart / lift car starts on its track (they are built at the origin - which is inside the cell).
            for (int i = 0; i < Conveyors.Length; i++)
            {
                var c = Conveyors[i]; float len = PathLength(c.Path);
                for (int k = 0; k < c.Carts.Length; k++) if (c.Carts[k] != null) c.Carts[k].position = At(c.Path, Mathf.Repeat(_convT[i] + k * len / c.Carts.Length, len));
            }
            foreach (var l in Lifts) if (l.Car != null) l.Car.position = Vector3.Lerp(l.Bottom, l.Top, 0.5f);
        }

        static float PathLength(Vector3[] p) { float l = 0f; for (int i = 1; i < p.Length; i++) l += Vector3.Distance(p[i - 1], p[i]); return l; }
        static Vector3 At(Vector3[] p, float d)
        {
            for (int i = 1; i < p.Length; i++)
            {
                float seg = Vector3.Distance(p[i - 1], p[i]);
                if (d <= seg) return Vector3.Lerp(p[i - 1], p[i], seg > 0f ? d / seg : 0f);
                d -= seg;
            }
            return p[p.Length - 1];
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < Conveyors.Length; i++)
            {
                var c = Conveyors[i];
                if (c.Quiet && !QuietDistrictRunning) continue;
                _convT[i] += dt * c.Speed;
                float len = PathLength(c.Path);
                for (int k = 0; k < c.Carts.Length; k++)
                    if (c.Carts[k] != null) c.Carts[k].position = At(c.Path, Mathf.Repeat(_convT[i] + k * len / c.Carts.Length, len));
            }
            foreach (var l in Lifts)
            {
                if (l.Car == null) continue;
                float u = 0.5f - 0.5f * Mathf.Cos((Time.time / l.Period + l.Phase) * Mathf.PI * 2f);
                l.Car.position = Vector3.Lerp(l.Bottom, l.Top, Mathf.SmoothStep(0f, 1f, u));
            }
            for (int i = 0; i < Arms.Length; i++)
            {
                var a = Arms[i];
                if (a.Pivot == null || (a.Quiet && !QuietDistrictRunning)) continue;
                float s = Mathf.Sin((Time.time / a.Period + a.Phase) * Mathf.PI * 2f);
                a.Pivot.localRotation = _armRest[i] * Quaternion.Euler(0f, a.Swing * s, -6f * Mathf.Abs(s));
            }
        }

        /// <summary>The distant section of machinery starts moving (the quiet district wakes up).</summary>
        public void StartMachinery() { QuietDistrictRunning = true; AudioKit.Log?.Invoke("city: quiet district machinery starts"); }

        /// <summary>Stone breaks off the ancient terrace and falls (dust where it lands). ~4 s.</summary>
        public IEnumerator DropStone()
        {
            if (FallingStone == null || StoneFallen) yield break;
            StoneFallen = true;
            AudioKit.Play("city_stone_fall", null, 0.8f);
            var from = FallingStone.position;
            // a short shudder first, then the fall (accelerating, slight tumble)
            for (float t = 0f; t < 0.7f; t += Time.deltaTime) { FallingStone.position = from + Random.insideUnitSphere * 0.6f; yield return null; }
            float v = 0f, y = from.y;
            var rot = Random.onUnitSphere;
            while (y > StoneLandAt.y)
            {
                v += 9.81f * Time.deltaTime * 2.2f;          // reads at this distance: a little faster than real
                y -= v * Time.deltaTime;
                var p = Vector3.Lerp(from, StoneLandAt, Mathf.InverseLerp(from.y, StoneLandAt.y, y)); p.y = Mathf.Max(y, StoneLandAt.y);
                FallingStone.position = p;
                FallingStone.Rotate(rot, 25f * Time.deltaTime, Space.World);
                if (Dust != null && Random.value < 0.25f) { Dust.transform.position = p; Dust.Emit(2); }
                yield return null;
            }
            if (Dust != null) { Dust.transform.position = StoneLandAt; var sh = Dust.shape; sh.radius = 18f; Dust.Emit(160); }
        }
    }
}
