using UnityEngine;

namespace Seven
{
    /// <summary>Sets <see cref="Ground"/> for this scene before anything else runs.</summary>
    [DefaultExecutionOrder(-500)]
    public class WorldGround : MonoBehaviour
    {
        public Ground.Shape Shape = Ground.Shape.Flat;

        // Hill: a near-flat sparring top (radius PlateauRadius around PlateauCenter) that curves down at the edge
        // into the fog sea. Static so the editor builder generates the mesh from the very same function.
        public static readonly Vector3 PlateauCenter = new Vector3(-3f, 0f, -2.5f);
        public const float PlateauRadius = 8.5f;
        public const float EdgeCurve = 0.055f;            // drop = EdgeCurve * (r - PlateauRadius)^2
        public static readonly Vector3 ArenaCenter = new Vector3(-1.5f, 0f, -1.0f);
        public const float ArenaRadius = 5.6f;

        public static float HillHeight(float x, float z)
        {
            float dx = x - PlateauCenter.x, dz = z - PlateauCenter.z;
            float r = Mathf.Sqrt(dx * dx + dz * dz);
            // Gentle tilt (higher left/back, like the reference) and soft undulation: a few cm across the top.
            float h = -0.03f * x - 0.006f * z + 0.03f * Mathf.Sin(x * 0.61f + 1.3f) * Mathf.Cos(z * 0.47f);
            if (r > PlateauRadius)
            {
                float e = r - PlateauRadius;
                h -= EdgeCurve * e * e + 0.02f * e;
            }
            return h;
        }

        void Awake()
        {
            Ground.Current = Shape;
            if (Shape == Ground.Shape.Hill)
            {
                Ground.ArenaCenter = ArenaCenter;
                Ground.ArenaRadius = ArenaRadius;
            }
        }
    }
}
