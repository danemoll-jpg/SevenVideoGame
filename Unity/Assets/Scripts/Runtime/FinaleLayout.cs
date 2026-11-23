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
        public static readonly Vector3 BarrierControl = new Vector3(21.6f, Y, -167.6f);  // its side control (X)
        public static readonly Vector3 ConduitAt = new Vector3(2.4f, Y, -158f);          // the conduit Tunneler damages (west)
        public static readonly Vector3 MiningAccess = new Vector3(11.5f, Y, -178.5f);    // he forces his way in here (south)
        public static readonly Vector3 TunnelerStart = new Vector3(11.5f, Y, -173.5f);
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
