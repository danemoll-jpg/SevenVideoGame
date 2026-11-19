using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 11 — Venus layout in metres (shared by the scene builder and the runtime), like <see cref="MoonLayout"/>.
    /// Y up, Z = north (the way to the temple). VEN_01 landing platform at the origin; VEN_02 the broad garden road north
    /// with its long open bend (B02) and the collapsed walkway (B03 / C02), crossed on the bike over a slab she lays
    /// across it with telekinesis (QF10); VEN_03 the temple
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
        // Round 12 (2f): the shared saucer exterior (Chapter 1's 12 m disc), radius 6.
        // Round 13 (0b, the saucer's fixed design): no ramp - it hovers on Venus with its underside 3.35 m up (Chapter 1's
        // beam height: 1.15 + HoverLift 2.2) and 7 comes down / goes up in the tractor beam under its centre.
        public const float SaucerRadius = 6f, SaucerHover = 3.35f;
        /// <summary>Where the beam sets her down (the ground under the hatch).</summary>
        public static Vector3 BeamFoot => new Vector3(SaucerAt.x, 0f, SaucerAt.z);
        /// <summary>Where she walks to after coming down (north, clear of the hull) - was the foot of the old ramp.</summary>
        public static readonly Vector3 StepOut = new Vector3(0f, 0f, 4.3f);
        public static readonly Vector3 HatchAt = new Vector3(3.4f, 0f, -1.4f);       // the storage hatch (bike bay), north-east
        /// <summary>QF10: the seamless hull panel of the bike bay: centre angle (degrees from +x toward +z round the saucer's
        /// axis) and angular width; it spans the hull's lower band (local y -0.2 .. 1.0).</summary>
        public const float HatchAngle = 41.25f, HatchSpan = 30f;   // = FightDemoBuilder.SaucerHatch* (edges on the 3.75-degree lathe segments)
        public static Vector3 HatchDir => new Vector3(Mathf.Cos(HatchAngle * Mathf.Deg2Rad), 0f, Mathf.Sin(HatchAngle * Mathf.Deg2Rad));
        /// <summary>The middle of the opening on the hull (world).</summary>
        public static Vector3 HatchMouth => SaucerAt + HatchDir * SaucerRadius + Vector3.up * (SaucerHover + 0.68f);
        /// <summary>The bike inside the bay (radial, its middle here) and where it is clear of the hull outside.</summary>
        public static Vector3 BayInside => SaucerAt + HatchDir * SaucerHold.InsideR;       // QF14 (3): in its cradles in the room
        public static Vector3 BayOutside => SaucerAt + HatchDir * SaucerHold.OutsideR;
        public static readonly Vector3 BikeUnpacked = new Vector3(6.4f, 0f, 4.6f);   // the bike, out of the hatch, facing north
        public static readonly Vector3 PantherLie = new Vector3(-3.4f, 0f, 3.6f);    // he lies beside the ship, under the rim, large
        public static readonly Vector3 VistaSpot = new Vector3(0f, 0f, 11.5f);       // over her shoulder toward the gardens
        // ---- VEN_02 road: (bike + walk)
        public const float BendZ = 50f;
        public static readonly Vector3 BendSpot = new Vector3(16f, -1.5f, 52f);        // B02: the long open bend
        public const float TremorZ = 96f;                                              // B03 / C02 at or past this (on RoadB)
        public static readonly Vector3 StopSpot = new Vector3(30f, 0f, 108f);        // the clear stopping area before the collapse
        public const float CollapseZ0 = 118f, CollapseZ1 = 128f;                     // the road is gone between these
        // ---- QF10 (VEN_B03 changed by Dan): the collapse is crossed with telekinesis, on the bike. A long fallen slab lies
        // along the near edge of the hole (the right length: 12 m for a 10 m gap); she lifts it and lays it across as a
        // bridge (it stays for return trips), then rides over it. The "Bridge" zone opens when it is down.
        public static readonly Vector3 SlabLie = new Vector3(30.4f, 0.32f, 115.6f);    // centre, lying along x beside the gap
        public static readonly Vector3 SlabSize = new Vector3(12.2f, 0.62f, 4.6f);    // length (x when lying), thickness, width (Round 13 0d: 3.0 -> 4.6 m)
        public const float SlabLieYaw = 90f;                                           // its length along x
        public static readonly Vector3 SlabBridge = new Vector3(30f, 0.0f, 123f);      // centre once laid across (length along z)
        public const float BridgeTop = 0.12f;                                          // its top surface (ends sunk into the broken lips)
        public const float SlabTkRange = 14f;
        public static readonly Vector3 SlabLookAt = SlabLie + Vector3.up * 0.3f;
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
        public static readonly Vector3 BasinCentre = new Vector3(106f, -12f, 142f);  // the creatures move down here
        public static readonly Vector3 Foundations = new Vector3(40f, -14f, 228f);   // under the temple: where the signal drives them

        // ---- Round 12: VEN_04 -> VEN_05, the way down (on foot): through the root gate (it opens after C05) down an open
        // root pit into the hillside, a root tunnel, the root chamber, a deep tunnel east under the basin, the control chamber.
        public static readonly Vector3 RootGateIn = new Vector3(69f, -6f, 134.4f);
        public const float RootLandZ = 131f, RootPitTopZ = 134.6f, RootPitBotZ = 117f, RootPitTopY = -6f, RootPitBotY = -14f;
        public const float TunnelBotZ = 96f, ChamberY = -22f, ControlY = -30f;
        /// <summary>Below ground from here on (the music and the cave light blend in past this line).</summary>
        /// <summary>The root pit's floor (a slot cut into the hillside; the terrain takes this height exactly inside it).</summary>
        public static bool InRootPit(float x, float z) => x >= 66.5f && x <= 71.5f && z >= RootPitBotZ && z <= RootPitTopZ;
        public static float RootPitHeight(float z) => z >= RootLandZ + 0.6f ? RootPitTopY : Mathf.Lerp(RootPitBotY, RootPitTopY, Mathf.InverseLerp(RootPitBotZ, RootLandZ + 0.6f, z));
        public static bool Underground(Vector3 p) => p.y < -11f && p.x > 59f && p.x < 142f && p.z < 120f && p.z > 60f;
        public static readonly Vector3 RootChamberAt = new Vector3(70f, ChamberY, 90f);
        // ---- VEN_05: the control chamber (floor y -30). Wormmaster above his machinery on the east dais; three control
        // devices; three creature troughs (each driven by one device); the containment console in front of the dais.
        public static readonly Vector3 ControlEntry = new Vector3(105.5f, ControlY, 89f);
        public static readonly Vector3 ControlCentre = new Vector3(121f, ControlY, 88f);
        public static readonly Vector3 DaisAt = new Vector3(135f, ControlY, 88f);
        public const float DaisRadius = 4.2f, DaisHeight = 4.2f, BarrierRadius = 5.6f;
        public static Vector3 WormmasterAt => DaisAt + Vector3.up * DaisHeight;
        public static readonly Vector3 ConsoleAt = new Vector3(127.2f, ControlY, 88f);     // close the containment barrier (X)
        public static readonly string[] DeviceIds = { "dev_north", "dev_ledge", "dev_crystal" };
        /// <summary>The devices: north (X, at the north walkway), the ledge (climb up to it, X), the crystal (RT: pull its
        /// signal crystal out from the safe walkway - it stands in the south trough).</summary>
        public static readonly Vector3[] DeviceAt = { new Vector3(117f, ControlY, 107.6f), new Vector3(107.4f, LedgeY, 67.2f), new Vector3(122f, ControlY, 74.5f) };
        public static readonly Vector3[] DeviceUse = { new Vector3(117f, ControlY, 105.8f), new Vector3(108.2f, LedgeY, 68.6f), new Vector3(116.2f, ControlY, 70f) };
        public const float LedgeY = -29.0f;          // the raised ledge in the south-west corner (a 1 m wall, climb anywhere along it)
        /// <summary>The creature troughs (start, end): north z 97 (dev_north), south z 79.5 (dev_crystal), east x 128.5 in
        /// front of the dais (dev_ledge). The walkways beside the walls are safe.</summary>
        public static readonly Vector3[,] Trough =
        {
            { new Vector3(106.5f, ControlY, 97f), new Vector3(133f, ControlY, 97f) },
            { new Vector3(128.6f, ControlY, 66f), new Vector3(128.6f, ControlY, 110f) },
            { new Vector3(106.5f, ControlY, 79.5f), new Vector3(133f, ControlY, 79.5f) },
        };
        public static readonly MoonTraversal.Mantle LedgeMantle = new MoonTraversal.Mantle { Name = "control chamber ledge", From = new Vector3(110f, ControlY, 72.2f), FaceAt = 71.4f, Fwd = Vector3.back, TopY = LedgeY, Along0 = 104.2f, Along1 = 111.6f };

        public static readonly Z[] Zones =
        {
            // QF10 (6f): order matters (the first zone containing a point gives its height): the paved / special ones
            // first, then "Valley" - the natural ground (VenusTerrain: the worn path, the valley floor, the gully).
            R("Platform", -10.5f, -10.5f, 10.5f, 10.5f, 0f),
            R("Detour1", 15f, 104f, 23.4f, 116f, 1.0f, false),                        // the garden-wall shelf (climb anywhere along it, explore)
            // Round 13 (0d): the slab bridge is wider (4.6 m) and has ramped ends meeting the ground on both sides (no lip);
            // the wide ramps funnel any reasonable approach angle onto it
            R("Bridge", 27.7f, 116.2f, 32.3f, 129.8f, BridgeTop, true, float.NaN, 1, false),   // QF10: the slab laid across the gully (opens when it is down)
            R("BridgeRampS", 26.2f, 112.4f, 33.8f, 117.6f, 0f, true, BridgeTop, 1, false),          // overlaps the bridge 1.4 m (>= 0.6 m rule)
            R("BridgeRampN", 26.2f, 128.4f, 33.8f, 132.6f, BridgeTop, true, 0f, 1, false),
            R("Court", 14f, 190f, 46f, 214f, 0f),
            R("TempleWalk", 25f, 212f, 35f, 237f, 0f, false),
            R("TempleHall", 22f, 236f, 38f, 262.6f, 0f, false),
            R("CubeRoom", 25f, 262f, 35f, 274f, 0f, false),
            R("MaintA", 44f, 195f, 80f, 203f, 0f, true, float.NaN, 1, false),          // opens after C04 (VEN_S03)
            R("MaintB", 73f, 150f, 80f, 197f, -6f, true, 0f, 1, false),
            R("ObsArea", 64f, 134f, 88f, 152f, -6f, true, float.NaN, 1, false),
            // Round 12: the way down (on foot, opens after C05) and VEN_05 - listed before "Valley", they win where they lie
            R("RootLanding", 66.5f, RootLandZ, 71.5f, RootPitTopZ, RootPitTopY, false, float.NaN, 1, false),
            R("RootPit", 66.5f, RootPitBotZ, 71.5f, RootLandZ + 0.6f, RootPitBotY, false, RootPitTopY, 1, false),
            R("RootTunnel", 66.5f, TunnelBotZ, 71.5f, RootPitBotZ + 0.6f, ChamberY, false, RootPitBotY, 1),
            R("RootChamber", 60f, 84f, 80f, TunnelBotZ + 0.6f, ChamberY, false),
            R("DeepTunnel", 79.4f, 86f, 104f, 92f, ChamberY, false, ControlY, 0),
            R("CtrlLedge", 103.6f, 64.2f, 111.8f, 71.4f, LedgeY, false),
            R("Control", 103.4f, 64.2f, 140.6f, 111.8f, ControlY, false),
            R("Valley", -45f, -25f, 95f, 220f, 0f),                                    // natural ground (height field + walkable mask)
        };
        /// <summary>QF10 (6f): zones whose ground is the sculpted terrain itself (no floor block is built for them).</summary>
        public static bool NaturalZone(string name) => name == "Valley" || name == "MaintA" || name == "MaintB" || name == "ObsArea" || name == "Detour1" || name == "Bridge" || name == "BridgeRampS" || name == "BridgeRampN" || name == "Platform";

        // ---- QF10: the garden wall beside the stopping area: climbable anywhere along it (explore), jump down anywhere.
        // It is no longer the way round the collapse (the far terrace and the ramp beyond are gone: the slab bridge is).
        public static readonly MoonTraversal.Mantle DetourMantle = new MoonTraversal.Mantle { Name = "garden wall", From = new Vector3(24.25f, 0f, 108f), FaceAt = 23.4f, Fwd = Vector3.left, TopY = 1.0f, Along0 = 104f, Along1 = 113.9f };

        public static void UseTraversal() => MoonTraversal.Use(null, null, null, null, new[] { DetourMantle, LedgeMantle }, null);

        /// <summary>Is a point on one of the zones the bike may use?</summary>
        public static bool BikeZone(Vector3 p, float pad = 0.6f)
        {
            foreach (var z in Ground.Zones) if (z.Enabled && z.Contains(p.x, p.z, pad) && IsBikeZone(z.Name)) return true;
            return false;
        }
        public static bool IsBikeZone(string name) { foreach (var z in Zones) if (z.Name == name) return z.Bike; return false; }
    }
}
