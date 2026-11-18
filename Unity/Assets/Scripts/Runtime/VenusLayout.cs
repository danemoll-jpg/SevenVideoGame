using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 11 — Venus layout in metres (shared by the scene builder and the runtime), like <see cref="MoonLayout"/>.
    /// Y up, Z = north (the way to the temple). VEN_01 landing platform at the origin; VEN_02 the broad garden road north
    /// with its long open bend (B02) and the collapsed walkway (B03 / C02) plus the on-foot detour; VEN_03 the temple
    /// court (bike parking), pedestrian garden, guardian area, hall and cube chamber; VEN_04 the maintenance path east and
    /// down to the creature observation area above the basin (the root-chamber gate toward VEN_05 stays shut this round).
    /// Built for 3.5 m Elarim: doorways 6 m high, steps and walkways wide - 7 looks small beside them.
    /// </summary>
    public static class VenusLayout
    {
        public struct Z { public string Name; public float X0, Z0, X1, Z1, Y0, Y1; public int Axis; public bool Bike, On; }
        static Z R(string n, float x0, float z0, float x1, float z1, float y0, bool bike = true, float y1 = float.NaN, int axis = 1, bool on = true)
            => new Z { Name = n, X0 = x0, Z0 = z0, X1 = x1, Z1 = z1, Y0 = y0, Y1 = float.IsNaN(y1) ? y0 : y1, Axis = axis, Bike = bike, On = on };

        // ---- VEN_01
        public static readonly Vector3 SaucerAt = new Vector3(0f, 0f, -4.5f);
        public const float SaucerRadius = 4.3f, SaucerHover = 1.15f;
        public static readonly Vector3 RampFoot = new Vector3(0f, 0f, 1.6f);       // the ramp comes down on the north side
        public static readonly Vector3 HatchAt = new Vector3(3.4f, 0f, -1.4f);       // the storage hatch (bike bay), north-east
        public static readonly Vector3 BikeUnpacked = new Vector3(4.6f, 0f, 2.4f);   // the bike, out of the hatch, facing north
        public static readonly Vector3 PantherLie = new Vector3(-2.6f, 0f, 1.2f);    // he lies beside the ramp, large
        public static readonly Vector3 VistaSpot = new Vector3(0f, 0f, 9.5f);       // over her shoulder toward the gardens
        // ---- VEN_02 road: (bike + walk)
        public const float BendZ = 50f;
        public static readonly Vector3 BendSpot = new Vector3(14f, 0f, 50f);        // B02: the long open bend
        public const float TremorZ = 96f;                                              // B03 / C02 at or past this (on RoadB)
        public static readonly Vector3 StopSpot = new Vector3(30f, 0f, 112f);        // the clear stopping area before the collapse
        public const float CollapseZ0 = 118f, CollapseZ1 = 128f;                     // the road is gone between these
        public static readonly Vector3 VesselAt = new Vector3(37.2f, 0.9f, 106f);    // C02: water shakes in a vessel; a worker steadies it
        public static readonly Vector3 BikeBeyond = new Vector3(30f, 0f, 133f);      // the bike is brought round past the collapse
        // ---- VEN_03
        public const float CourtZ = 190f;
        public static readonly Vector3 ParkSpot = new Vector3(37f, 0f, 199f);        // B04: she parks beside the court
        public static readonly Vector3 GuardianSpot = new Vector3(30f, 0f, 222f);    // C03 at the temple entrance garden
        public static readonly Vector3 MeetSpot = new Vector3(30f, 0f, 217.5f);
        public const float DoorZ = 236f, DoorHalf = 3f, DoorH = 6.2f;
        public static readonly Vector3 CubeAt = new Vector3(30f, 1.35f, 270f);       // the original cube, in its protected structure
        public const float CubeRoomZ = 258f;                                          // C04 at or past this
        public static readonly Vector3 CubeView = new Vector3(30f, 0f, 264.5f);
        // ---- VEN_04
        public static readonly Vector3 MaintStart = new Vector3(48f, 0f, 199f);
        public static readonly Vector3 ObsSpot = new Vector3(78f, -6f, 143f);        // C05: the creature observation area
        public static readonly Vector3 ObsPark = new Vector3(74f, -6f, 148f);
        public static readonly Vector3 RootGate = new Vector3(69f, -6f, 137f);       // toward VEN_05 (Round 12) - shut
        public static readonly Vector3 BasinCentre = new Vector3(112f, -14f, 132f);  // the creatures move down here
        public static readonly Vector3 Foundations = new Vector3(40f, -14f, 228f);   // under the temple: where the signal drives them

        public static readonly Z[] Zones =
        {
            R("Platform", -12f, -12f, 12f, 12f, 0f),
            R("RoadA", -4f, 11.4f, 4f, 52f, 0f),
            R("Bend", -4f, 44f, 34f, 56f, 0f),
            R("RoadB", 26f, 50f, 34f, 112f, 0f),
            R("Stop", 23.8f, 100f, 40f, 118f, 0f),
            R("Detour1", 15f, 104f, 23.4f, 126f, 1.0f, false),
            R("Detour2", 15f, 128.6f, 23.4f, 136f, 1.0f, false),
            R("DetourRamp", 22.6f, 130.5f, 27.0f, 135.5f, 1.18f, false, 0f, 0),   // 1.0 at x 23.4 (the terrace edge), 0 at x 27
            R("RoadC", 26f, 128f, 34f, 196f, 0f),
            R("Court", 14f, 190f, 46f, 214f, 0f),
            R("TempleWalk", 25f, 212f, 35f, 237f, 0f, false),
            R("TempleHall", 22f, 236f, 38f, 262.6f, 0f, false),
            R("CubeRoom", 25f, 262f, 35f, 274f, 0f, false),
            R("MaintA", 44f, 195f, 80f, 203f, 0f, true, float.NaN, 1, false),          // opens after C04 (VEN_S03)
            R("MaintB", 73f, 150f, 80f, 197f, -6f, true, 0f, 1, false),
            R("ObsArea", 64f, 134f, 88f, 152f, -6f, true, float.NaN, 1, false),
        };

        // ---- the on-foot detour round the collapsed walkway (existing moves: mantle up, assisted jump, walk down)
        public static readonly MoonTraversal.Mantle DetourMantle = new MoonTraversal.Mantle { Name = "garden wall (detour)", From = new Vector3(24.25f, 0f, 112f), FaceAt = 23.4f, Fwd = Vector3.left, TopY = 1.0f };
        public static readonly MoonTraversal.JumpLink DetourJump = new MoonTraversal.JumpLink { Name = "broken channel (detour)", From = new Vector3(19.2f, 1.0f, 125.4f), To = new Vector3(19.2f, 1.0f, 129.3f), Radius = 1.1f, LateralClamp = new Vector2(15.6f, 22.8f) };

        public static void UseTraversal() => MoonTraversal.Use(null, new[] { DetourJump }, null, null, new[] { DetourMantle }, null);

        /// <summary>Is a point on one of the zones the bike may use?</summary>
        public static bool BikeZone(Vector3 p, float pad = 0.6f)
        {
            foreach (var z in Ground.Zones) if (z.Enabled && z.Contains(p.x, p.z, pad) && IsBikeZone(z.Name)) return true;
            return false;
        }
        public static bool IsBikeZone(string name) { foreach (var z in Zones) if (z.Name == name) return z.Bike; return false; }
    }
}
