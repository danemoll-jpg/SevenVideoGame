using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF30 (5): Tunneler's drill head - ONE component for every appearance (Europa's screen, the finale's speakers / FIN_08 /
    /// FIN_09 / the barrier). A real drill: a continuous rotation about the cone's own axis (his body's up through the collar
    /// socket, measured once from the mesh: the axis from the base ring's centre to the tip), always the same direction, faster on
    /// louder syllables while he talks, full speed on a drill strike, spinning down to a stop when his machinery powers down. It
    /// keeps its angle (never snaps back, never twists back and forth).
    /// Why it regressed (QF13 11 fixed it on Europa): the finale did not use Europa's code - Round 19 wrote a second drill in
    /// FinaleDirector.LateUpdate that turned the DrillHead with <c>Rotate(Vector3.up, .., Space.Self)</c>: the DrillHead's own
    /// local Y. The FBX comes from Blender (Z up), so the mesh object's local Y is one of the cone's SIDE axes once imported -
    /// the head tumbled end over end round a horizontal axis instead of spinning, and BossFight's Anim set a new speed every frame
    /// (900 / 1400 / 0) so it jerked. Europa's code mapped the body's up into the parent frame. Now both scenes use this one
    /// component and the build check DRILL AXIS (QF30 5) spins it in every scene with Tunneler and fails on a wrong axis or a
    /// reversal.
    /// </summary>
    [DefaultExecutionOrder(250)]
    public class DrillHead : MonoBehaviour
    {
        /// <summary>The body the cone sits on (its up = the cone's axis when no mesh axis is found).</summary>
        public Transform Body;
        /// <summary>deg/s wanted now (set by whoever drives him: talking, fighting, idle, contained).</summary>
        public float Want;
        /// <summary>Talking: the loudness 0..1 adds to the speed (240 + 900 x loudness, as on Europa).</summary>
        public float Talk;
        public float SpinUp = 2400f, SpinDown = 380f;
        public float Speed { get; private set; }
        public float Angle { get; private set; }
        public float MaxSpeed { get; private set; }
        public int Turns => Mathf.FloorToInt(Mathf.Abs(Angle) / 360f);
        public int Reversals { get; private set; }
        /// <summary>The cone's axis in the PARENT's frame (measured once), and the head's rest rotation.</summary>
        public Vector3 AxisLocal { get => _axis; private set => _axis = value; }
        [SerializeField] Vector3 _axis; [SerializeField] bool _baked;
        /// <summary>The builder measures the cone's axis from the imported mesh (editor: always readable) and stores it here.</summary>
        public void Bake() { _axis = MeasureAxis(transform, Body); _baked = true; }
        Quaternion _rest; bool _init; float _lastStep;
        public static readonly List<DrillHead> All = new List<DrillHead>();
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public static DrillHead On(Transform head, Transform body)
        {
            if (head == null) return null;
            var d = head.GetComponent<DrillHead>() ?? head.gameObject.AddComponent<DrillHead>();
            d.Body = body; return d;
        }

        public void Init()
        {
            if (_init) return; _init = true;
            _rest = transform.localRotation;
            if (!_baked || _axis.sqrMagnitude < 0.5f) AxisLocal = MeasureAxis(transform, Body);
        }
        /// <summary>The cone's axis: from the centroid of its wide base to its tip (the vertex farthest from the base centroid), in the
        /// parent's frame. Falls back to the body's up.</summary>
        public static Vector3 MeasureAxis(Transform head, Transform body)
        {
            var parent = head.parent;
            Vector3 world = body != null ? body.up : Vector3.up;
            var mf = head.GetComponent<MeshFilter>(); var mesh = mf != null ? mf.sharedMesh : null;
            if (mesh != null && (mesh.isReadable || Application.isEditor) && mesh.vertexCount > 10)
            {
                var v = mesh.vertices;
                // the base = the vertices nearest the pivot (the collar socket: the Blender cut put the pivot at the base's lowest point)
                Vector3 c = Vector3.zero; int n = 0; float far = 0f; Vector3 tip = Vector3.zero;
                foreach (var p in v) { if (p.sqrMagnitude > far) { far = p.sqrMagnitude; tip = p; } }
                foreach (var p in v) if (p.magnitude < Mathf.Sqrt(far) * 0.35f) { c += p; n++; }
                if (n > 3)
                {
                    c /= n; var axis = head.TransformDirection((tip - c).normalized);
                    // the tip points away from the body: choose the sign along the body's up
                    if (body != null && Vector3.Dot(axis, body.up) < 0f) axis = -axis;
                    if (body == null || Vector3.Angle(axis, body.up) < 35f) world = axis;   // a cone leaning a little is kept; a wild measure falls back
                }
            }
            return parent != null ? parent.InverseTransformDirection(world).normalized : world.normalized;
        }

        void LateUpdate() { Step(Time.deltaTime); }
        /// <summary>One frame of the drill (also called by the build check DRILL AXIS in the editor).</summary>
        public void Step(float dt)
        {
            if (!_init) Init();
            if (dt <= 0f) return;
            float target = Mathf.Max(0f, Want + (Talk > 0f ? 240f + Mathf.Clamp01(Talk) * 900f : 0f));
            Speed = Mathf.MoveTowards(Speed, target, (target > Speed ? SpinUp : SpinDown) * dt);
            float step = Speed * dt;
            if (step < 0f) Reversals++;                       // never: the speed is clamped >= 0 (one direction only)
            _lastStep = step;
            Angle += step;
            MaxSpeed = Mathf.Max(MaxSpeed, Speed);
            transform.localRotation = Quaternion.AngleAxis(Angle % 360f, AxisLocal) * _rest;
        }
        /// <summary>The cone's axis in the world measured from the mesh as it is posed now (the check compares it before / after spinning).</summary>
        public Vector3 ConeAxisWorld()
        {
            var parent = transform.parent; var local = MeasureAxis(transform, Body);
            return parent != null ? parent.TransformDirection(local) : local;
        }
        /// <summary>Back to the rest pose (editor check).</summary>
        public void ResetSpin() { if (_init) transform.localRotation = _rest; Angle = 0f; Speed = 0f; MaxSpeed = 0f; Reversals = 0; _init = false; }

        /// <summary>The proof: the head's axis in the world now vs the measured cone axis (deg) - a spin about its own axis keeps the
        /// cone's axis fixed, a tumble swings it round.</summary>
        public float AxisWobble()
        {
            var parent = transform.parent; var want = parent != null ? parent.TransformDirection(AxisLocal) : AxisLocal;
            var now = transform.rotation * Quaternion.Inverse(parent != null ? parent.rotation * _rest : _rest) * want;
            return Vector3.Angle(want, now);
        }
        public string Report(string where) => $"DRILL HEAD (QF30 5) {where}: {Turns} full turns, top speed {MaxSpeed:0} deg/s, reversals {Reversals} (must be 0), about the cone's own axis (wobble {AxisWobble():0.0} deg)";
    }
}
