using UnityEngine;

namespace Seven
{
    /// <summary>Sets <see cref="Ground"/> for this scene before anything else runs.</summary>
    [DefaultExecutionOrder(-500)]
    public class WorldGround : MonoBehaviour
    {
        public Ground.Shape Shape = Ground.Shape.Flat;

        [Header("Deck (saucer)")]
        public float DeckRadius = 7.6f;
        [Tooltip("Wider sectors (nooks): x = start angle, y = end angle (deg, atan2(z, x)), z = walkable radius.")]
        public Vector3[] DeckSectors = new Vector3[0];
        [Tooltip("Furniture footprints: x, z = centre, z = radius. Element 0 is 7's chair.")]
        public Vector3[] DeckObstacles = new Vector3[0];

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

        [Header("Zones (Chapter 1)")]
        [Tooltip("Walkable rectangles: name, min (x,z), max (x,z), height at min / max, slope axis 0 = X, 1 = Z.")]
        public ZoneDef[] Zones = new ZoneDef[0];
        public Vector3[] ZoneObstacles = new Vector3[0];
        [System.Serializable] public struct ZoneDef { public string Name; public Vector2 Min, Max; public float Y0, Y1; public int Axis; public bool Enabled; public bool Low; }

        void Awake()
        {
            Ground.Current = Shape;
            if (Shape == Ground.Shape.Hill)
            {
                Ground.ArenaCenter = ArenaCenter;
                Ground.ArenaRadius = ArenaRadius;
            }
            Ground.Zones.Clear();
            Ground.DeckSectors.Clear();
            Ground.Obstacles.Clear();
            Ground.IgnoreObstacle = -1;
            if (Shape == Ground.Shape.Zones)
            {
                foreach (var z in Zones) Ground.Zones.Add(new Ground.Zone { Name = z.Name, Min = z.Min, Max = z.Max, Y0 = z.Y0, Y1 = z.Y1, SlopeAxis = z.Axis, Enabled = z.Enabled, Low = z.Low });
                foreach (var o in ZoneObstacles) Ground.Obstacles.Add(new Ground.Circle(new Vector2(o.x, o.y), o.z));
            }
            if (Shape == Ground.Shape.Deck)
            {
                Ground.DeckRadius = DeckRadius;
                foreach (var s in DeckSectors) Ground.DeckSectors.Add(new Ground.Sector(s.x, s.y, s.z));
                foreach (var o in DeckObstacles) Ground.Obstacles.Add(new Ground.Circle(new Vector2(o.x, o.y), o.z));
            }
        }
    }
}
