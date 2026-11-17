using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Where the ground is and where characters may go. Each scene's <see cref="WorldGround"/> sets this up in Awake
    /// (the training room is flat; the outdoor world is an analytic hill; the saucer deck is a flat round floor with
    /// nooks and furniture), so height and collision queries need no physics.
    /// </summary>
    public static class Ground
    {
        public enum Shape { Flat, Hill, Deck }
        public static Shape Current = Shape.Flat;
        public static Vector2 ArenaHalf = new Vector2(8.5f, 5.5f);   // flat room: rectangle
        public static Vector3 ArenaCenter = Vector3.zero;              // hill: circle
        public static float ArenaRadius = 6.5f;

        // ---- Deck (saucer): round floor, wider in the nooks and narrower where built-in sofas stand.
        public struct Sector { public float A0, A1, MaxR; public Sector(float a0, float a1, float r) { A0 = a0; A1 = a1; MaxR = r; } }
        public struct Circle { public Vector2 C; public float R; public Circle(Vector2 c, float r) { C = c; R = r; } }
        public static float DeckRadius = 7.5f;                         // walkable radius along the curved wall
        public static readonly List<Sector> DeckSectors = new List<Sector>();   // angle range (deg, atan2(z,x)) -> max radius
        public static readonly List<Circle> Obstacles = new List<Circle>();    // furniture footprints
        /// <summary>Temporarily ignored obstacle (7's chair while she walks into it to sit).</summary>
        public static int IgnoreObstacle = -1;

        public static float Height(float x, float z) => Current == Shape.Hill ? WorldGround.HillHeight(x, z) : 0f;
        public static float Height(Vector3 p) => Height(p.x, p.z);

        static bool InSector(float a, Sector s)
        {
            float d0 = Mathf.DeltaAngle(s.A0, a), span = Mathf.DeltaAngle(s.A0, s.A1);
            if (span < 0f) span += 360f;
            if (d0 < 0f) d0 += 360f;
            return d0 <= span;
        }

        /// <summary>Walkable radius at this angle on the deck.</summary>
        public static float DeckMaxRadius(float angleDeg)
        {
            float r = DeckRadius;
            foreach (var s in DeckSectors)
                if (InSector(angleDeg, s)) r = s.MaxR;
            return r;
        }

        public static Vector3 ClampToArena(Vector3 p, float bodyRadius = 0.25f)
        {
            if (Current == Shape.Hill)
            {
                var d = new Vector2(p.x - ArenaCenter.x, p.z - ArenaCenter.z);
                if (d.magnitude > ArenaRadius) d = d.normalized * ArenaRadius;
                p.x = ArenaCenter.x + d.x; p.z = ArenaCenter.z + d.y;
            }
            else if (Current == Shape.Deck)
            {
                var q = new Vector2(p.x, p.z);
                for (int i = 0; i < Obstacles.Count; i++)
                {
                    if (i == IgnoreObstacle) continue;
                    var o = Obstacles[i];
                    var d = q - o.C;
                    float min = o.R + bodyRadius;
                    if (d.sqrMagnitude < min * min) q = o.C + (d.sqrMagnitude > 1e-8f ? d.normalized : Vector2.right) * min;
                }
                float a = Mathf.Atan2(q.y, q.x) * Mathf.Rad2Deg;
                float r = q.magnitude;
                float maxR = DeckMaxRadius(a) - bodyRadius;
                if (r > maxR)
                {
                    // Inside a nook's mouth but past its side wall: slide along the side wall instead of jumping
                    // back to the main wall radius.
                    bool slid = false;
                    if (r > DeckRadius - bodyRadius)
                        foreach (var s in DeckSectors)
                        {
                            if (s.MaxR <= DeckRadius || r > s.MaxR - bodyRadius) continue;
                            float margin = bodyRadius / Mathf.Max(r, 0.1f) * Mathf.Rad2Deg;
                            float e0 = s.A0 + margin, e1 = s.A1 - margin;
                            float d0 = Mathf.Abs(Mathf.DeltaAngle(a, e0)), d1 = Mathf.Abs(Mathf.DeltaAngle(a, e1));
                            float edge = d0 < d1 ? e0 : e1;
                            if (Mathf.Min(d0, d1) * Mathf.Deg2Rad * r < 1.0f)
                            {
                                q = new Vector2(Mathf.Cos(edge * Mathf.Deg2Rad), Mathf.Sin(edge * Mathf.Deg2Rad)) * r;
                                slid = true;
                                break;
                            }
                        }
                    if (!slid) q = q.normalized * maxR;
                }
                p.x = q.x; p.z = q.y;
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
