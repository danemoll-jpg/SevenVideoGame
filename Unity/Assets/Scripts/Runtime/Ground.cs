using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Where the ground is and where fighters may go. Each scene's <see cref="WorldGround"/> sets this up in Awake
    /// (the training room is flat; the outdoor world is an analytic hill, so height queries need no physics).
    /// </summary>
    public static class Ground
    {
        public enum Shape { Flat, Hill }
        public static Shape Current = Shape.Flat;
        public static Vector2 ArenaHalf = new Vector2(8.5f, 5.5f);   // flat room: rectangle
        public static Vector3 ArenaCenter = Vector3.zero;              // hill: circle
        public static float ArenaRadius = 6.5f;

        public static float Height(float x, float z) => Current == Shape.Hill ? WorldGround.HillHeight(x, z) : 0f;
        public static float Height(Vector3 p) => Height(p.x, p.z);

        public static Vector3 ClampToArena(Vector3 p)
        {
            if (Current == Shape.Hill)
            {
                var d = new Vector2(p.x - ArenaCenter.x, p.z - ArenaCenter.z);
                if (d.magnitude > ArenaRadius) d = d.normalized * ArenaRadius;
                p.x = ArenaCenter.x + d.x; p.z = ArenaCenter.z + d.y;
            }
            else
            {
                p.x = Mathf.Clamp(p.x, -ArenaHalf.x, ArenaHalf.x);
                p.z = Mathf.Clamp(p.z, -ArenaHalf.y, ArenaHalf.y);
            }
            return p;
        }
    }
}
