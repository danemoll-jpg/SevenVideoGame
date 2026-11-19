using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 9: Chapter 1 traversal data (metres, same frame as <see cref="MoonLayout"/>), shared by the scene builder
    /// (ledge lips, walls, stubs) and the runtime (7's moves, the cat's routes, the check-run autopilot).
    /// Every walkable surface is a <see cref="Ground.Zone"/>; these objects only connect zones that are not walk-connected:
    /// ledges (hang / shimmy / climb up / let go), assisted jumps, jump-to-grab, wall climbs, mantles and climb-down spots.
    /// Numbers come from the Mixamo clips measured in Blender (Round 9, Docs/Previews/r9/clip_measurements.txt).
    /// </summary>
    public static class MoonTraversal
    {
        // ---- clip geometry (Blender measurements of Dan's clips, metres above / ahead of the root)
        /// <summary>Hang pose: root this far below the lip (hang_idle hands 2.24, shimmy hands 2.38 above the root).</summary>
        public const float HangDrop = 2.32f;
        /// <summary>Hang pose: root this far out from the wall face (hips clear of the wall; hand IK reaches the lip).</summary>
        public const float HangOut = 0.28f;
        /// <summary>7_climb_up (Braced Hang To Crouch): the top surface is 1.64 above its root, she ends 0.56 ahead.</summary>
        public const float ClimbUpRise = 1.64f, ClimbUpAdvance = 0.56f;
        /// <summary>7_wall_climb: one play climbs 1.21 m; at its end the hands are 2.80 above its root.</summary>
        public const float WallClimbStep = 1.21f, WallClimbReach = 2.80f;
        /// <summary>7_jump_to_hang ends with the hands 2.36 up / 0.97 ahead and the hips 1.04 ahead of its root.</summary>
        public const float JumpHangUp = 2.36f, JumpHangHips = 1.04f;
        /// <summary>Mantle = 7_climb_up started at this normalized time (hips at standing height, hands on the edge).</summary>
        public const float MantleStart = 0.36f;
        public const float ShimmySpeed = 0.55f;
        /// <summary>Runtime calibration (7 measures her wrists against the lip while hanging and lifts the hang pose
        /// until they sit on it - the Humanoid retarget puts the hands a little lower than the raw clip).</summary>
        public static float HangLift;

        public class Ledge
        {
            public string Name;
            public Vector3 A, B;          // the lip (top edge of the wall face) from A to B
            public Vector3 Fwd;           // horizontal, from her body toward the wall (she faces this way while hanging)
            public float ClimbFrom = 0f, ClimbTo = 99f;   // climb-up allowed for u (metres from A) in this range
            public float Length => Vector3.Distance(A, B);
            public Vector3 Dir => (B - A).normalized;
            public Vector3 At(float u) => A + Dir * Mathf.Clamp(u, 0f, Length);
            public float U(Vector3 p) => Mathf.Clamp(Vector3.Dot(p - A, Dir), 0f, Length);
            public bool CanClimb(float u) => u >= ClimbFrom - 1e-3f && u <= ClimbTo + 1e-3f;
            public Vector3 HangRoot(float u) => At(u) - Fwd * HangOut - Vector3.up * HangDrop + Vector3.up * HangLift;
        }

        public class JumpLink { public string Name; public Vector3 From, To; public float Radius = 0.9f; public bool KeepLateral = true; public Vector2 LateralClamp; }
        public class JumpGrab { public string Name; public Vector3 From; public float Radius = 0.9f; public Ledge Ledge; }
        public class WallClimb { public string Name; public Vector3 Base; public float Radius = 0.8f; public Ledge Ledge; public int Cycles = 1; }
        /// <summary>QF10: a waist-high wall she can climb up ANYWHERE along it (not one point): the face is the line
        /// FaceAt (x if Fwd is along x, else z), Fwd points from the base toward the top. It works wherever a walkable zone
        /// at the base height (From.y) lies in front of the face and one at TopY behind it, optionally limited to the
        /// range Along0..Along1 (the other axis). Radius = how far from the face she may stand. From = a reference point
        /// on the base (its height is the base height; check runs and the autopilot use it). From the top she can also
        /// jump back down anywhere along it (no point of no return).</summary>
        public class Mantle
        {
            public string Name; public Vector3 From; public float Radius = 0.9f; public float FaceAt; public Vector3 Fwd; public float TopY;
            public float Along0 = float.NegativeInfinity, Along1 = float.PositiveInfinity;
            public bool AlongX => Mathf.Abs(Fwd.x) > 0.5f;
            public float Sign => AlongX ? Mathf.Sign(Fwd.x) : Mathf.Sign(Fwd.z);
            /// <summary>Distance in front of the face (base side positive).</summary>
            public float Front(Vector3 p) => (FaceAt - (AlongX ? p.x : p.z)) * Sign;
            public float Along(Vector3 p) => AlongX ? p.z : p.x;
            public Vector3 OnFace(float along, float y, float outward) { var q = AlongX ? new Vector3(FaceAt, y, along) : new Vector3(along, y, FaceAt); return q - Fwd * outward; }
        }
        /// <summary>QF10: climb down into a hang anywhere along the ledge's climbable range (not one spot). Spot = a reference
        /// point on the top; its distance behind the lip is where she stands; Radius = the tolerance around that.</summary>
        public class HangDown { public string Name; public Vector3 Spot; public float Radius = 0.75f; public Ledge Ledge; }

        /// <summary>QF10: is there a walkable zone at this height (+-0.12) at the point (enabled zones only)?</summary>
        public static bool ZoneAt(float x, float z, float y)
        {
            foreach (var zn in Ground.Zones)
                if (zn.Enabled && !zn.Low && zn.Contains(x, z) && Mathf.Abs(zn.HeightAt(x, z) - y) < 0.12f) return true;
            return false;
        }

        /// <summary>QF10: can she climb this wall from where she stands (anywhere along it)? Out: the along coordinate.</summary>
        public static bool MantleHere(Mantle m, Vector3 p, out float along)
        {
            along = m.Along(p);
            if (Mathf.Abs(p.y - m.From.y) > 0.6f) return false;
            float f = m.Front(p);
            if (f < 0.05f || f > m.Radius + 0.25f) return false;
            if (along < m.Along0 + 0.25f || along > m.Along1 - 0.25f) return false;
            var baseP = m.OnFace(along, m.From.y, 0.35f); var topP = m.OnFace(along, m.TopY, -(ClimbUpAdvance + 0.05f));
            return ZoneAt(baseP.x, baseP.z, m.From.y) && ZoneAt(topP.x, topP.z, m.TopY);
        }

        /// <summary>QF10: standing on top of a mantle wall near its edge: where a jump down would land (or false).</summary>
        public static bool MantleDownHere(Mantle m, Vector3 p, out Vector3 land)
        {
            land = Vector3.zero;
            float along = m.Along(p);
            if (Mathf.Abs(p.y - m.TopY) > 0.3f) return false;
            float f = m.Front(p);                     // negative on the top side
            if (f > -0.05f || f < -1.0f) return false;
            if (along < m.Along0 + 0.25f || along > m.Along1 - 0.25f) return false;
            land = m.OnFace(along, m.From.y, 0.75f);
            return ZoneAt(land.x, land.z, m.From.y);
        }

        /// <summary>QF10: can she climb down into a hang here (anywhere along the ledge's climbable range)? Out: u on the ledge.</summary>
        public static bool HangDownHere(HangDown h, Vector3 p, out float u)
        {
            var L = h.Ledge; u = 0f;
            if (L == null) return false;
            var lip = L.At(L.U(h.Spot));
            float off = Vector3.Dot(h.Spot - lip, L.Fwd); off = Mathf.Max(0.15f, off);
            if (Mathf.Abs(p.y - lip.y) > 0.6f) return false;
            float raw = Vector3.Dot(p - L.A, L.Dir);
            float lo = Mathf.Max(0.3f, L.ClimbFrom), hi = Mathf.Min(L.Length - 0.3f, L.ClimbTo);
            if (raw < lo - 0.1f || raw > hi + 0.1f) return false;
            u = Mathf.Clamp(raw, lo, hi);
            var d = p - L.At(u); d.y = 0f;
            float perp = Vector3.Dot(d, L.Fwd);
            if (perp < 0.05f || Mathf.Abs(perp - off) > h.Radius) return false;
            return true;
        }

        // ------------------------------------------------------------------------------------------ ledges
        public static readonly Ledge Bulkhead = new Ledge { Name = "bulkhead", A = new Vector3(14.0f, 2.4f, 2.25f), B = new Vector3(14.0f, 2.4f, 4.35f), Fwd = Vector3.right };
        public static readonly Ledge CrackWall = new Ledge { Name = "cracked wall", A = new Vector3(15.8f, 2.8f, -0.2f), B = new Vector3(17.2f, 2.8f, -0.2f), Fwd = Vector3.back };
        /// <summary>The shaft lip below the cracked wall's top shelf: shimmy west over the dark drop, let go onto the ruins ledge.</summary>
        public static readonly Ledge ShaftLip = new Ledge { Name = "shaft lip", A = new Vector3(17.1f, 2.8f, -2.4f), B = new Vector3(11.9f, 2.8f, -2.4f), Fwd = Vector3.forward, ClimbFrom = 0f, ClimbTo = 1.25f };
        /// <summary>Broken terrace C's west face: grabbed from terrace B by a jump, shimmy north (a fallen column blocks the top further south).</summary>
        public static readonly Ledge TerraceCLip = new Ledge { Name = "terrace C lip", A = new Vector3(10.8f, 0.8f, -5.25f), B = new Vector3(10.8f, 0.8f, -6.85f), Fwd = Vector3.right, ClimbFrom = 0f, ClimbTo = 0.7f };
        public static readonly Ledge VisionExitLip = new Ledge { Name = "vision exit lip", A = new Vector3(17.1f, MoonLayout.Lower, -39.4f), B = new Vector3(15.9f, MoonLayout.Lower, -39.4f), Fwd = Vector3.forward };
        public static readonly Ledge MineLip = new Ledge { Name = "mining cut lip", A = new Vector3(15.9f, MoonLayout.Lower, -41.2f), B = new Vector3(17.1f, MoonLayout.Lower, -41.2f), Fwd = Vector3.back };
        public static readonly Ledge RampEnd = new Ledge { Name = "broken ramp end", A = new Vector3(20.85f, -4.4f, -55.0f), B = new Vector3(22.15f, -4.4f, -55.0f), Fwd = Vector3.forward };
        public static readonly Ledge ScaffoldLip = new Ledge { Name = "scaffold lip", A = new Vector3(20.1f, -7.6f, -62.4f), B = new Vector3(22.9f, -7.6f, -62.4f), Fwd = Vector3.forward };

        public static Ledge[] Ledges = { Bulkhead, CrackWall, ShaftLip, TerraceCLip, VisionExitLip, MineLip, RampEnd, ScaffoldLip };

        // ------------------------------------------------------------------------------------------ links
        public static JumpLink[] Jumps =
        {
            new JumpLink { Name = "catwalk gap", From = new Vector3(8.1f, 0f, 3.3f), To = new Vector3(10.65f, 0f, 3.3f), Radius = 1.0f, LateralClamp = new Vector2(2.35f, 4.25f) },
            new JumpLink { Name = "bulkhead down", From = new Vector3(15.0f, 2.4f, 3.3f), To = new Vector3(16.2f, 0f, 3.3f), Radius = 0.9f, LateralClamp = new Vector2(2.35f, 4.25f) },
            new JumpLink { Name = "ruins gap", From = new Vector3(10.7f, -1.6f, -3.6f), To = new Vector3(8.45f, -1.6f, -3.7f), Radius = 0.9f, LateralClamp = new Vector2(-4.4f, -2.95f) },
            new JumpLink { Name = "scaffold gap", From = new Vector3(21.5f, -7.6f, -57.9f), To = new Vector3(21.5f, -7.6f, -60.1f), Radius = 0.9f, LateralClamp = new Vector2(20.2f, 22.8f) },
        };
        public static JumpGrab[] Grabs =
        {
            new JumpGrab { Name = "bulkhead grab", From = new Vector3(13.6f, 0f, 3.3f), Radius = 1.0f, Ledge = Bulkhead },
            new JumpGrab { Name = "terrace C grab", From = new Vector3(9.3f, -0.7f, -6.3f), Radius = 0.85f, Ledge = TerraceCLip },
        };
        public static WallClimb[] Climbs =
        {
            new WallClimb { Name = "cracked wall", Base = new Vector3(16.5f, 0f, 0.05f), Ledge = CrackWall, Cycles = 1 },
            new WallClimb { Name = "mining cut wall", Base = new Vector3(16.5f, -6.4f, -40.95f), Ledge = MineLip, Cycles = 2 },
        };
        public static Mantle[] Mantles =
        {
            new Mantle { Name = "terrace B step", From = new Vector3(8.2f, -1.6f, -4.45f), FaceAt = -4.8f, Fwd = Vector3.back, TopY = -0.7f },
        };
        public static HangDown[] HangDowns =
        {
            new HangDown { Name = "shelf edge", Spot = new Vector3(16.5f, 2.8f, -2.05f), Ledge = ShaftLip },
            new HangDown { Name = "vision exit edge", Spot = new Vector3(16.5f, MoonLayout.Lower, -39.1f), Ledge = VisionExitLip, Radius = 0.85f },
            new HangDown { Name = "broken ramp end", Spot = new Vector3(21.5f, -4.35f, -54.7f), Ledge = RampEnd, Radius = 0.85f },
            new HangDown { Name = "scaffold edge", Spot = new Vector3(21.5f, -7.6f, -62.1f), Ledge = ScaffoldLip, Radius = 0.95f },
        };

        // ------------------------------------------------------------------------------------------ props
        public static readonly Vector2 GapX = new Vector2(8.4f, 10.0f);              // broken catwalk section
        public static readonly Vector2 PipeX = new Vector2(11.4f, 13.0f);            // low pipe run (crouch under)
        public const float PipeBottom = 1.15f;
        public static readonly Vector2 BulkheadX = new Vector2(14.0f, 15.3f);
        public const float BulkheadTop = 2.4f;
        /// <summary>The fallen slab on terrace C that blocks the stair down (telekinesis).</summary>
        public static readonly Vector3 SlabRest = new Vector3(14.05f, 0.8f + 0.24f, -5.2f);
        public static readonly Vector3 SlabSize = new Vector3(1.0f, 0.48f, 1.3f);
        public static readonly Vector3 SlabAway = new Vector3(14.6f, 1.6f, -3.4f);  // lifted over the shaft, then dropped into it
        public const float SlabTkRange = 7f;
        // Cat perches that are not walkable for 7 (stone stubs he hops across).
        public static readonly Vector3 StubCrackWall = new Vector3(17.22f, 1.45f, 0.02f);
        public static readonly Vector3 StubShaft = new Vector3(14.4f, 1.25f, -3.45f);
        public static readonly Vector3 StubMine = new Vector3(17.2f, -4.45f, -40.98f);

        // ------------------------------------------------------------------------------------------ Round 11: per-world sets
        /// <summary>The traversal moves in use (7 reads these arrays). The Moon's set is the default; Venus installs its
        /// own with <see cref="Use"/> and the Moon restores its own with <see cref="UseMoon"/>.</summary>
        static Ledge[] _mL; static JumpLink[] _mJ; static JumpGrab[] _mG; static WallClimb[] _mC; static Mantle[] _mM; static HangDown[] _mH;
        public static void UseMoon()
        {
            if (_mL == null) return;
            Ledges = _mL; Jumps = _mJ; Grabs = _mG; Climbs = _mC; Mantles = _mM; HangDowns = _mH;
        }
        public static void Use(Ledge[] l, JumpLink[] j, JumpGrab[] g, WallClimb[] c, Mantle[] m, HangDown[] h)
        {
            if (_mL == null) { _mL = Ledges; _mJ = Jumps; _mG = Grabs; _mC = Climbs; _mM = Mantles; _mH = HangDowns; }
            Ledges = l ?? new Ledge[0]; Jumps = j ?? new JumpLink[0]; Grabs = g ?? new JumpGrab[0];
            Climbs = c ?? new WallClimb[0]; Mantles = m ?? new Mantle[0]; HangDowns = h ?? new HangDown[0];
        }

        // ------------------------------------------------------------------------------------------ helpers
        public static bool Facing(Transform t, Vector3 dir, float maxDeg)
        {
            dir.y = 0f; var f = t.forward; f.y = 0f;
            return dir.sqrMagnitude > 1e-4f && Vector3.Angle(f, dir) <= maxDeg;
        }
        static float FlatDist(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return (a - b).magnitude; }
        public static bool Near(Vector3 p, Vector3 spot, float r) => FlatDist(p, spot) < r && Mathf.Abs(p.y - spot.y) < 0.6f;

        /// <summary>A walkable zone directly under a point (no fallback), or null.</summary>
        public static Ground.Zone ZoneUnder(Vector3 p, float pad = 0.02f)
        {
            Ground.Zone best = null; float bestY = float.MinValue;
            foreach (var z in Ground.Zones)
            {
                if (!z.Enabled || !z.Contains(p.x, p.z, pad)) continue;
                float h = z.HeightAt(p.x, p.z);
                if (h <= p.y + 0.05f && h > bestY) { bestY = h; best = z; }
            }
            return best;
        }
    }

    /// <summary>Round 9 (8b): Kyle's floating shapes (from kyle_7_telekinesis.jpg). The vision object uses
    /// <see cref="MoonLayout.VisionShape"/>; change that one constant to swap the shape everywhere.</summary>
    public static class PlatonicShape
    {
        public enum Kind { Tetrahedron, Cube, Octahedron, Dodecahedron }

        /// <summary>Vertices on the unit sphere.</summary>
        public static List<Vector3> Vertices(Kind k)
        {
            var v = new List<Vector3>();
            float p = (1f + Mathf.Sqrt(5f)) * 0.5f, ip = 1f / p;
            switch (k)
            {
                case Kind.Tetrahedron: v.AddRange(new[] { new Vector3(1, 1, 1), new Vector3(1, -1, -1), new Vector3(-1, 1, -1), new Vector3(-1, -1, 1) }); break;
                case Kind.Cube: for (int i = 0; i < 8; i++) v.Add(new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)); break;
                case Kind.Octahedron: v.AddRange(new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back }); break;
                default:
                    for (int i = 0; i < 8; i++) v.Add(new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f })
                    {
                        v.Add(new Vector3(0f, a * ip, b * p));
                        v.Add(new Vector3(a * ip, b * p, 0f));
                        v.Add(new Vector3(a * p, 0f, b * ip));
                    }
                    break;
            }
            for (int i = 0; i < v.Count; i++) v[i] = v[i].normalized;
            return v;
        }

        /// <summary>Edges = vertex pairs at the shortest distance.</summary>
        public static List<(int, int)> Edges(List<Vector3> v)
        {
            float min = float.MaxValue;
            for (int i = 0; i < v.Count; i++) for (int j = i + 1; j < v.Count; j++) min = Mathf.Min(min, (v[i] - v[j]).magnitude);
            var e = new List<(int, int)>();
            for (int i = 0; i < v.Count; i++) for (int j = i + 1; j < v.Count; j++) if ((v[i] - v[j]).magnitude < min * 1.01f) e.Add((i, j));
            return e;
        }
    }
}
