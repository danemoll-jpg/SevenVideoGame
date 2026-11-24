using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 19 — "The Moon Finale" (Docs/CHAPTER_FINALE.md): the scene's numbers, shared by the builder
    /// (Editor/FightDemoBuilderFinale.cs) and the game. The hollow Moon of Chapter 1 (the same city geometry, BuildCity, round the
    /// same level origin) - the finale happens one level down from where Chapter 1 ended: the saucer berths in the bay
    /// (MoonLayout.SaucerCentre), and the lower service district runs south from it at the bay's level (y -10):
    ///   the berth + its overlook (FIN_02: the city reveal) -> a walkway -> the service junction (FIN_03: the Scree worker, the
    ///   restricted terminal, the power panel, the rusted gate) -> a short narrow maintenance crossing -> the landing and the
    ///   ancient door (FIN_04, framed as vision_01) -> the circular archive chamber (FIN_05-FIN_13: seven sockets round a
    ///   central platform, projection walls, the old maintenance barrier in its alcove, the mining access passage opposite).
    /// Walkable floor = rectangles (Ground zones), overlapping >= 0.6 m at every doorway.
    /// </summary>
    public static class FinaleLayout
    {
        public const float Y = -10f;                                                     // = MoonLayout.Bay
        public static readonly Vector3 SaucerAt = new Vector3(11.5f, Y, -80f);           // the berth (the saucer hovers over it)
        public const float SaucerHover = 3.35f;                                          // = SaucerBeam.ShipBelly
        public static readonly Vector3 BeamDown = new Vector3(11.5f, Y, -80f);
        public static readonly Vector3 Overlook = new Vector3(11.5f, Y, -93.2f);         // FIN_02: they look out from the berth's edge
        public static readonly Vector3 CityLook = new Vector3(16f, -150f, -700f);        // the basin's lights and the mining intruding (the reveal's aim)
        // FIN_03 - the junction
        public static readonly Vector3 WorkerAt = new Vector3(15.5f, Y, -111f);
        public static readonly Vector3 TerminalAt = new Vector3(17.6f, Y, -109f);        // Tunneler's restricted markings
        public static readonly Vector3 PowerPanel = new Vector3(5.2f, Y, -113f);         // reroute the power (X)
        public static readonly Vector3 GateLock = new Vector3(11.5f, Y + 1.6f, -119.4f); // the rusted lock (telekinesis lifts it)
        public static readonly Vector3 GateAt = new Vector3(11.5f, Y, -120f);
        // the maintenance crossing (narrow, a drop either side) and the landing before the door
        public const float CrossX0 = 10.5f, CrossX1 = 12.5f, CrossZ0 = -133f, CrossZ1 = -121f;
        public static readonly Vector3 DoorAt = new Vector3(11.5f, Y, -142f);            // the ancient door (faces +z, toward her)
        public static readonly Vector3 DoorView = new Vector3(11.5f, Y, -137.4f);        // where she stops (FIN_04_01)
        public static readonly Vector3 DoorMark = new Vector3(12.6f, Y + 1.5f, -141.6f); // the marking beside which her hand hovers
        // FIN_05+ - the chamber
        public static readonly Vector3 ChamberC = new Vector3(11.5f, Y, -162f);
        public const float ChamberR = 14f, ChamberH = 11f;
        public static readonly Vector3 PlatformAt = ChamberC;                            // the central archive surface
        public const float PlatformR = 2.2f, PlatformH = 0.9f;
        /// <summary>The seven sockets round the chamber (yaw from the entrance, clockwise): the Moon's at the entrance.</summary>
        public static readonly string[] SocketRunes = { "dodecahedron", "cube", "icosahedron", "tetrahedron", "cuboctahedron", "octahedron", "sphere" };
        public static readonly string[] SocketWorlds = { "the Moon", "Venus", "Europa", "Mars", "the asteroid", "the mothership", "Cattalus" };
        public const float SocketR = 9.5f;
        public static Vector3 Socket(int i)
        {
            float a = (i * 360f / 7f) * Mathf.Deg2Rad;               // 0 = toward the entrance (+z)
            return ChamberC + new Vector3(Mathf.Sin(a) * SocketR, 0f, Mathf.Cos(a) * SocketR);
        }
        public static Vector3 SocketTop(int i) => Socket(i) + Vector3.up * 1.15f;
        public static readonly Vector3 BarrierAt = new Vector3(24.2f, Y, -162f);         // the old maintenance barrier's alcove (east)
        // QF30 (11): the controls stand at the room's edge (r 12.4-12.6), out of the open floor where the fight happens
        // QF31 (9): each control (and the light column over it while it is the place to go) stands midway between two screens - the
        // barrier's at rc2's 118.8 deg sat in front of Europa's screen seen from the south (the fight's side now)
        public static readonly Vector3 BarrierControl = new Vector3(21.27f, Y, -169.8f); // its side control (X), 128.6 deg (between Europa's and Mars's screens)
        public static readonly Vector3 ConduitAt = new Vector3(0.2f, Y, -156.9f);        // the conduit Tunneler damages (west), 294 deg (rc2's place: no screen behind its column; the wall drill moved off it)
        /// <summary>QF30 (6): the big mining drills boring into the old structure - round the chamber (yaw from +z, height) and along the
        /// service route (position, boring direction).</summary>
        public static readonly (float yaw, float h, float scale)[] ChamberDrills = { (231.4f, 3.6f, 1f), (334.3f, 5.4f, 0.85f), (25.7f, 2.8f, 0.9f) };   // QF31: each in a gap between two screens, clear of the two controls (128.6 / 282.9 deg)
        public static readonly (Vector3 at, Vector3 dir)[] RouteDrills = { (new Vector3(1.3f, Y + 2.9f, -116.5f), Vector3.right), (new Vector3(18.6f, Y + 3.5f, -135.6f), Vector3.left) };
        public static Vector3 ChamberDrillAt(int i) { var d = ChamberDrills[i]; float a = d.yaw * Mathf.Deg2Rad; return ChamberC + new Vector3(Mathf.Sin(a) * (ChamberR + 0.3f), d.h, Mathf.Cos(a) * (ChamberR + 0.3f)); }
        /// <summary>QF30 (4): where people stand round the platform: the platform's base reaches PlatformR + 0.3, a body needs 0.25 more.</summary>
        public const float StandR = PlatformR + 0.55f;
        public static Vector3 Stand(float yawDeg, float r = StandR) { float a = yawDeg * Mathf.Deg2Rad; return PlatformAt + new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r); }
        /// <summary>QF30 (15): Panther laid on the archive's top, beside where her hand rests (yaw 0 = toward the entrance).</summary>
        public static Vector3 PantherOnArchive => PlatformAt + new Vector3(0.36f, PlatformH, 1.72f);   // on her LEFT (she faces -z: +x is her left) - her left hand rests on him
        /// <summary>QF30 (14/15): the large Panther lies on his side here, his head toward +x; she kneels at his head.</summary>
        public static Vector3 PantherHead => PantherFalls + new Vector3(1.05f, 0f, 0.15f);
        public static Vector3 KneelSpot => PantherFalls + new Vector3(1.6f, 0f, 0.25f);    // QF32 (1): her knees against his head (the scene places her from his drawn head)
        /// <summary>QF30 (16): the Scree crew round the barrier (FIN_11 / FIN_13).</summary>
        public static Vector3 CrewAt(int i) => BarrierAt + new Vector3(-2.3f - (i % 2) * 0.9f, 0f, -2.0f + i * 2.0f);
        /// <summary>QF30 (4) build check STAGING MARKS: every staged spot (what, where, body radius, height, the floor it stands on).</summary>
        public static (string what, Vector3 p, float r, float h, float floor)[] Marks()
        {
            var l = new System.Collections.Generic.List<(string, Vector3, float, float, float)>
            {
                ("7 at the archive (the touch)", Stand(0f), 0.24f, 1.6f, Y), ("Panther beside her at the archive", Stand(-24f, StandR + 0.15f), 0.2f, 0.35f, Y),
                ("7 kneeling at his head (FIN_10 - FIN_13)", KneelSpot, 0.3f, 1.1f, Y),
                ("Panther lying, large (FIN_09/10)", PantherFalls, 0.5f, 0.55f, Y),
                ("7 at the conduit control", ConduitAt + (ChamberC - ConduitAt).normalized * 1.6f, 0.24f, 1.6f, Y), ("7 at the barrier control", BarrierControl + (ChamberC - BarrierControl).normalized * 1.2f, 0.24f, 1.6f, Y),
            };
            for (int i = 0; i < 7; i++) { var s = Socket(i); var toS = (s - ChamberC); toS.y = 0f; l.Add(($"7 placing rune {i + 1}", s - toS.normalized * 0.95f, 0.24f, 1.6f, Y)); }
            for (int i = 0; i < 3; i++) l.Add(($"Scree crew {i + 1} at the barrier", CrewAt(i), 0.3f, 1.8f, Y));
            return l.ToArray();
        }
        public static Vector3 HandOnArchive => PlatformAt + new Vector3(-0.28f, PlatformH + 0.02f, 2.0f);   // on her RIGHT: the right hand
        public static readonly Vector3 MiningAccess = new Vector3(11.5f, Y, -178.5f);    // he forces his way in here (south)
        public static readonly Vector3 TunnelerStart = new Vector3(11.5f, Y, -175.5f);   // QF31 (8): the breach's mouth (he came through the torn wall)
        // ---- QF31 (8): the fight space. The archive's table lowers flush into a shallow light well; the pedestals sink into the floor;
        // his rig tears the south wall open onto the mining cavern beyond - a wide, flat worked-out floor the fight spills into.
        public const float WellR = PlatformR + 0.32f;                                    // the well's rim (the floor's hole)
        public const float WellDepth = 0.12f;                                            // the archive's surface below the floor once lowered
        public const float SocketSink = 1.06f;                                           // the pedestals sink until their cups are flush
        /// <summary>The wall panels he tears out (yaw from +z, degrees): either side of the old access opening, + its lintel.</summary>
        public static readonly float[] BreachPanels = { 163.125f, 196.875f };
        public const float BreachHalfDeg = 18f;
        public static readonly Vector3 CavernC = new Vector3(11.5f, Y, -192.5f);
        public const float CavernX0 = -9f, CavernX1 = 32f, CavernZ0 = -206f, CavernZ1 = -177.4f, CavernH = 16f;
        /// <summary>The cavern's dressing that bodies walk round: spoil heaps, parked drill carts, light masts (x, z, radius).</summary>
        public static readonly Vector3[] CavernProps =
        {
            new Vector3(-6.2f, -186f, 2.2f), new Vector3(-6.4f, -199.5f, 2.6f), new Vector3(29.2f, -185f, 2.4f), new Vector3(29.4f, -200f, 2.3f),
            new Vector3(3.5f, -203.6f, 2.0f), new Vector3(20f, -203.8f, 2.2f),
            new Vector3(-2.8f, -193f, 1.1f), new Vector3(26.4f, -193.4f, 1.1f),
            new Vector3(-4.6f, -180.6f, 0.35f), new Vector3(27.6f, -180.6f, 0.35f), new Vector3(1.0f, -204.5f, 0.35f), new Vector3(22.2f, -204.5f, 0.35f),
        };
        /// <summary>QF31 (8): the fight arena = the chamber's open floor (the well kept clear) + the breach + the cavern.</summary>
        public static Vector3 ClampArena(Vector3 p, float wallMargin, float wellMargin)
        {
            var c = ChamberC; var d = p - c; d.y = 0f; float r = d.magnitude;
            Vector3 inCircle; { float rMin = PlatformR + wellMargin, rMax = ChamberR - wallMargin; var dd = d; if (r < rMin) dd = (r < 0.01f ? Vector3.back : d / r) * rMin; else if (r > rMax) dd = d / r * rMax; inCircle = c + dd; }
            Vector3 inBreach = new Vector3(Mathf.Clamp(p.x, 8.4f, 14.6f), Y, Mathf.Clamp(p.z, -180f, -171.5f));
            Vector3 inCavern = new Vector3(Mathf.Clamp(p.x, CavernX0 + wallMargin + 1f, CavernX1 - wallMargin - 1f), Y, Mathf.Clamp(p.z, CavernZ0 + wallMargin + 1f, CavernZ1 - 1.2f));
            Vector3 best = inCircle; float bd = (Flat(p) - Flat(inCircle)).sqrMagnitude;
            foreach (var q in new[] { inBreach, inCavern }) { float k = (Flat(p) - Flat(q)).sqrMagnitude; if (k < bd) { bd = k; best = q; } }
            foreach (var o in CavernProps) { var od = new Vector2(best.x - o.x, best.z - o.y); float rr = o.z + wallMargin * 0.5f; if (od.sqrMagnitude < rr * rr) { od = od.sqrMagnitude < 1e-4f ? Vector2.up * rr : od.normalized * rr; best = new Vector3(o.x + od.x, Y, o.y + od.y); } }
            best.y = Y; return best;
        }
        public static bool InCavern(Vector3 p) => p.z < -176.6f;
        /// <summary>QF31 (10): the nearest point (with a margin) of the floor she can walk on - the enabled rectangle zones (never a masked
        /// zone like his rig's deck). His arena is clamped to it, so wherever he is stunned she can walk to him.</summary>
        public static Vector3 NearestWalkable(Vector3 p, float pad)
        {
            Vector3 best = p; float bd = float.MaxValue;
            foreach (var z in Ground.Zones)
            {
                if (!z.Enabled || z.Mask != null) continue;
                float px = Mathf.Min(pad, (z.Max.x - z.Min.x) * 0.45f), pz = Mathf.Min(pad, (z.Max.y - z.Min.y) * 0.45f);
                var q = new Vector3(Mathf.Clamp(p.x, z.Min.x + px, z.Max.x - px), p.y, Mathf.Clamp(p.z, z.Min.y + pz, z.Max.y - pz));
                float d = (Flat(q) - Flat(p)).sqrMagnitude; if (d < bd) { bd = d; best = q; }
            }
            return best;
        }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        public static readonly Vector3 HerFightStart = new Vector3(11.5f, Y, -158.2f);
        public static readonly Vector3 PantherEdge = new Vector3(3.4f, Y, -166.6f);      // where Panther calls from (the chamber's edge)
        public static readonly Vector3 PantherFalls = new Vector3(7.2f, Y, -165.6f);     // FIN_09: he falls clear of the machinery
        public const float ArenaX0 = ChamberC_X - 11.5f, ArenaX1 = ChamberC_X + 11.5f, ArenaZ0 = -173.5f, ArenaZ1 = -150.5f;
        const float ChamberC_X = 11.5f;

        // ---- the walkable floor
        public struct ZoneDef { public string Name; public float X0, Z0, X1, Z1; public bool On; }
        static ZoneDef R(string n, float x0, float z0, float x1, float z1, bool on = true) => new ZoneDef { Name = n, X0 = x0, Z0 = z0, X1 = x1, Z1 = z1, On = on };
        public static readonly ZoneDef[] Zones =
        {
            R("Berth", -6f, -94f, 29f, -64f),
            R("Walkway", 8.5f, -105f, 14.5f, -93.4f),
            R("Junction", 1.5f, -119.4f, 21.5f, -104.4f),
            R("GateZ", 9.6f, -121.6f, 13.4f, -118.8f, false),                       // opens when the rusted lock is lifted
            R("Crossing", CrossX0, CrossZ0, CrossX1, CrossZ1),
            R("Landing", 4f, -141f, 19f, -132.4f),
            R("DoorZ", 9.4f, -151.2f, 13.6f, -140.4f, false),                       // opens when she touches the door
            R("Chamber", 1.5f, -168f, 21.8f, -156f),                                 // the round chamber as three bands inside its wall
            R("ChamberN", 5f, -156.6f, 18f, -150.6f),
            R("ChamberS", 5f, -172f, 18f, -167.4f),
            R("Alcove", 21f, -165f, 26f, -159f),                                    // the barrier's alcove (open)
            R("Breach", 6.5f, -180f, 16.5f, -170.5f, false),                        // QF31 (8): the torn wall (opens when his rig breaks through)
            R("Cavern", CavernX0, CavernZ0, CavernX1, -179f, false),                // QF31 (8): the mining cavern beyond
        };
        public static bool Walkable(Vector3 p, float pad = 0.3f) { foreach (var z in Ground.Zones) if (z.Enabled && z.Contains(p.x, p.z, pad)) return true; return false; }
        public static string ZoneAt(Vector3 p) => Ground.ZoneName(p.x, p.z) ?? "-";

        // ---- checkpoints (Continue / Scene Select)
        public const int CpArrival = 0, CpBerth = 1, CpJunction = 2, CpDoor = 3, CpChamber = 4, CpConfront = 5, CpAfter = 6, CpEnding = 7, CpDone = 8;
        public static readonly string[] CpNames =
        {
            "the familiar approach", "beneath the lights", "the people keeping it running", "the door from the memory",
            "seven places, one history", "Tunneler arrives", "what she carries", "somewhere to come back to", "done",
        };
    }
}
