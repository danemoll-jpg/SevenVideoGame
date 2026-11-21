using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 16 — Chapter "The Asteroid — the Great Machine Age" (Docs/CHAPTER_ASTEROID.md): every number of the level in one
    /// place, for the builder (FightDemoBuilderAsteroid) and the game (AsteroidDirector).
    /// Inside an irregular asteroid. East: the sealed RECEIVING CHAMBER (the saucer hovers in it; the bike pad and 7's pad, the
    /// receiving scanner on the north wall, the cargo conveyor to sorting, the service entrance in the west wall). A service
    /// tunnel opens onto the enormous CAVERN - the city that still works: factory terraces climbing the walls, rail carriers
    /// between them, repetitive machinery, empty workstations; the raised SERVICE CAUSEWAY crosses it with three rail
    /// junctions (turntables, misaligned for the carriers) and bends north then west to the CENTRAL MAINTENANCE HALL (the
    /// Machinist's repair cradle, the archive conveyor into the cutting mechanism, the override terminal, three service
    /// stations, the temple archive behind the west wall). South of the hall, through the maintenance opening: the
    /// observation alcove over the archive feed line and the compact FACTORY AREA with the three bypass controls (transport /
    /// cooling / archive diverter), the final transport assembly (AST_06's drop + the climb) and the west passage back.
    /// Zones: axis-aligned, overlapping >= 0.6 m where they join; walkable on foot = every enabled zone, the bike = the
    /// <see cref="BikeZone"/> ones.
    /// </summary>
    public static class AsteroidLayout
    {
        public const float SaucerHover = 3.35f;

        // ---- the receiving chamber (AST_02)
        public static readonly Vector3 SaucerAt = new Vector3(0f, 0f, 0f);
        public static readonly Vector3 SevenDown = new Vector3(0f, 0f, 0f);         // 7's pad, under the beam
        public static readonly Vector3 BikePad = new Vector3(9f, 0f, 6f);           // the bike's pad (out of the hold)
        public static readonly Vector3 ScannerAt = new Vector3(2f, 7.2f, 17.2f);    // the receiving scanner's head (north wall)
        public static readonly Vector3 ContainerAt = new Vector3(-10f, 0f, 13f);    // the marked cargo container (on the conveyor's west end)
        public static readonly Vector3 SortingAt = new Vector3(18f, 0f, 13f);       // the sorting slot (east wall)
        public const float ConveyorZ = 13f, ConveyorX0 = -14f, ConveyorX1 = 18f;
        public static readonly Vector3 MaintBotHome = new Vector3(4.5f, 0f, 8.5f);
        public static readonly Vector3 DoorAt = new Vector3(-18f, 0f, -4f);          // the service entrance (west wall, 6 m wide)
        public static readonly Vector3 DoorControl = new Vector3(-15.2f, 0f, -10.4f);// the manual door control (a wheel on a post)
        public static readonly Vector3 DoorControlView = new Vector3(-14.2f, 0f, -9.2f);
        public const float ScannerTkRange = 21f;

        // ---- the cavern + the causeway (AST_03) - y 0 on the causeway; the cavern floor far below
        public const float CavernFloor = -46f;
        public static readonly Vector3 TunnelEnd = new Vector3(-60f, 0f, -4f);
        public static readonly Vector3[] Junctions = { new Vector3(-110f, 0f, -4f), new Vector3(-176f, 0f, 52f), new Vector3(-236f, 0f, 104f) };
        public static readonly float[] JunctionAxis = { 0f, 1f, 0f };               // 0 = the road runs along x when aligned, 1 = along z
        public static readonly Vector3[] JunctionStops = { new Vector3(-100f, 0f, -4f), new Vector3(-176f, 0f, 42f), new Vector3(-226f, 0f, 104f) };
        public static readonly Vector3[] JunctionControls = { new Vector3(-101f, 0f, -1.2f), new Vector3(-173.2f, 0f, 42f), new Vector3(-226f, 0f, 106.8f) };
        public const float JunctionR = 6.6f;
        public static readonly Vector3 BirdBench = new Vector3(-186.2f, 0f, 76f);   // a workstation off the causeway (the bird)
        public static readonly Vector3 BirdView = new Vector3(-184.6f, 0f, 76f);
        public static readonly Vector3 BirdAt = new Vector3(-186.6f, 0.98f, 76f);
        public static readonly Vector3[] Ride1 = { new Vector3(-70f, 0f, -4f), new Vector3(-94f, 0f, -4f) };
        public static readonly Vector3[] Ride2 = { new Vector3(-130f, 0f, -4f), new Vector3(-162f, 0f, -4f), new Vector3(-175.5f, 0f, -4f), new Vector3(-176f, 0f, 4f), new Vector3(-176f, 0f, 30f) };
        public static readonly Vector3[] Ride3 = { new Vector3(-176f, 0f, 66f), new Vector3(-176f, 0f, 96f), new Vector3(-176f, 0f, 103.6f), new Vector3(-184f, 0f, 104f), new Vector3(-212f, 0f, 104f) };
        public static readonly Vector3[] Ride4 = { new Vector3(-250f, 0f, 104f), new Vector3(-290f, 0f, 104f), new Vector3(-306f, 0f, 104f) };
        public static readonly Vector3 HallPlazaStop = new Vector3(-308f, 0f, 104f);
        public static readonly Vector3 TempleSignal = new Vector3(-319.1f, 13.5f, 104f); // the glyph beacon over the hall's east entrance

        // ---- the central maintenance hall (AST_04 / 07 / 09) - floor y 0, x -400..-320, z 70..140
        public static readonly Vector3 HallDoor = new Vector3(-320f, 0f, 104f);
        public static readonly Vector3 HallMeet = new Vector3(-340f, 0f, 112f);       // she stops here; he unfolds in front of her
        public static readonly Vector3 CradleAt = new Vector3(-344f, 0f, 128f);       // his repair cradle (he rises from it; the end)
        public static readonly Vector3 CutterAt = new Vector3(-374f, 0f, 122f);       // the cutting mechanism
        public const float ArchiveConveyorZ = 122f;
        public static readonly Vector3 TerminalAt = new Vector3(-360f, 0f, 100f);     // the override terminal
        public static readonly Vector3 TerminalView = new Vector3(-360f, 0f, 97.6f);
        public static readonly Vector3 MaintHatch = new Vector3(-328f, 0f, 70f);       // the maintenance opening (south wall)
        public static readonly Vector3[] Stations = { new Vector3(-390f, 0f, 80f), new Vector3(-372f, 0f, 84f), new Vector3(-394f, 0f, 112f) };
        public static readonly Vector3 ArchiveDoor = new Vector3(-400f, 0f, 106f);
        public static readonly Vector3 RuneAt = new Vector3(-426f, 1.25f, 106f);       // the cuboctahedron in the temple archive's mechanism
        public static readonly Vector3 RuneView = new Vector3(-423.4f, 0f, 106f);
        public static readonly Vector3 MemorySurface = new Vector3(-352f, 2.4f, 124.6f); // the archive surface the memory plays on (AST_09)
        public static readonly Vector3 StorageAt = new Vector3(-388f, 0f, 136f);      // protected storage (AST_09)
        public static readonly Vector3 MachinistStart = new Vector3(-360f, 0f, 92f);  // the fight (he starts here)
        public static readonly Vector3 HerFightStart = new Vector3(-360f, 0f, 80f);
        public const float ArenaX0 = -398f, ArenaX1 = -322f, ArenaZ0 = 72f, ArenaZ1 = 116f;
        /// <summary>QF21 (10): obstacle circles for the hall's foundry dressing (FightDemoBuilderAsteroidFoundry) - flywheel
        /// casings, boiler drums, crate stacks, the cooling tower, the lane end drums. Outside the fight's open floor except the
        /// two corner crate stacks (cover) and the lane drums at the arena's ends.</summary>
        public static readonly (Vector3 c, float r)[] HallProps =
        {
            (new Vector3(-335.2f, 0f, 134f), 1.3f), (new Vector3(-332f, 0f, 134f), 1.3f), (new Vector3(-328.8f, 0f, 134f), 1.3f),
            (new Vector3(-374.6f, 0f, 137.2f), 1.2f), (new Vector3(-372f, 0f, 137.2f), 1.2f), (new Vector3(-369.4f, 0f, 137.2f), 1.2f),
            (new Vector3(-362.2f, 0f, 137.6f), 1.3f), (new Vector3(-360f, 0f, 137.6f), 1.3f), (new Vector3(-357.8f, 0f, 137.6f), 1.3f),
            (new Vector3(-351f, 0f, 137.6f), 1.5f), (new Vector3(-337f, 0f, 120.5f), 1.5f), (new Vector3(-324f, 0f, 128f), 2.0f),
            (new Vector3(-395.6f, 0f, 74.6f), 1.5f), (new Vector3(-324.4f, 0f, 74.6f), 1.5f),
            (new Vector3(-322.4f, 0f, 116f), 1.1f), (new Vector3(-322.4f, 0f, 118f), 1.1f), (new Vector3(-322.4f, 0f, 120f), 1.1f),
        };
        public static readonly Vector3 SaucerHome = new Vector3(-300f, 0f, 106f);      // the saucer comes in over the plaza (AST_09)

        // ---- the factory area (AST_05 / 06) - y 0; the pit with the feed line between east and middle
        public static readonly Vector3 AlcoveWindow = new Vector3(-336f, 1.6f, 44f);   // the glass over the archive feed line
        public static readonly Vector3 AlcoveTerminal = new Vector3(-330f, 0f, 40.5f);
        public static readonly Vector3 LockerAt = new Vector3(-323.4f, 0f, 44.5f);    // the sealed maintenance locker (the arc blades)
        public static readonly Vector3 LockerView = new Vector3(-324.8f, 0f, 44.5f);
        public static readonly Vector3 TransportControl = new Vector3(-333f, 0f, 9.5f); // control 1
        public static readonly Vector3 CoolingValve = new Vector3(-369.5f, 0f, 30.6f);   // control 2 (the valve wheel)
        public static readonly Vector3 ClampLine = new Vector3(-366f, 1.2f, 33.4f);     // the powered clamp's feed (cut with the blades)
        public static readonly Vector3 DiverterAt = new Vector3(-407f, 0f, 27f);         // control 3
        public static readonly Vector3 FinalPlatC = new Vector3(-391.8f, 0f, 14f);       // the final transport assembly (retracts, AST_06)
        public const float LedgeY = -4.4f;
        public static readonly Vector3 LedgeLand = new Vector3(-392f, LedgeY, 14f);
        public static readonly Vector3 ClimbBase = new Vector3(-397.2f, LedgeY, 14f);    // the wall up to the west works
        public static readonly Vector3[] SecurityHomes = { new Vector3(-326f, 0f, 20f), new Vector3(-362f, 0f, 12f), new Vector3(-404f, 0f, 14f) };
        public static readonly Vector3 EscortedTo = new Vector3(-328f, 0f, 34f);         // where a security machine takes her (the works' entry)
        public static readonly Vector3[] SteamVents = { new Vector3(-375f, 0f, 12f), new Vector3(-379f, 0f, 16f), new Vector3(-383f, 0f, 12f) };

        /// <summary>AST_06: the wall she climbs from the maintenance ledge back up to the west works (a real wall climb, A).</summary>
        public static readonly MoonTraversal.Ledge WestLip = new MoonTraversal.Ledge { Name = "west works lip", A = new Vector3(-398f, 0f, 11.5f), B = new Vector3(-398f, 0f, 16.5f), Fwd = Vector3.left };
        public static readonly MoonTraversal.WallClimb WestClimb = new MoonTraversal.WallClimb { Name = "maintenance wall", Base = ClimbBase, Radius = 1.1f, Ledge = WestLip, Cycles = 2 };
        public static void UseTraversal() => MoonTraversal.Use(new[] { WestLip }, null, null, new[] { WestClimb }, null, null);

        // ---- zones (order matters: the first enabled zone containing a point gives its height)
        public struct ZoneDef { public string Name; public float X0, Z0, X1, Z1, Y0, Y1; public int Axis; public bool On, Bike; }
        static ZoneDef R(string n, float x0, float z0, float x1, float z1, float y, bool bike = true, bool on = true) => new ZoneDef { Name = n, X0 = x0, Z0 = z0, X1 = x1, Z1 = z1, Y0 = y, Y1 = y, Axis = 0, On = on, Bike = bike };
        static ZoneDef S(string n, float x0, float z0, float x1, float z1, float y0, float y1, int axis, bool bike = true, bool on = true) => new ZoneDef { Name = n, X0 = x0, Z0 = z0, X1 = x1, Z1 = z1, Y0 = y0, Y1 = y1, Axis = axis, On = on, Bike = bike };
        public static readonly ZoneDef[] Zones =
        {
            // the receiving chamber + the service tunnel
            R("Receiving", -18f, -18f, 18f, 18f, 0f),
            R("ServiceDoor", -18.6f, -7f, -17.4f, -1f, 0f, true, false),          // the service entrance (opens in AST_02)
            R("Tunnel", -60.6f, -8f, -18f, 0f, 0f),
            // the causeway (the junction turntables are off until she aligns them)
            R("CausewayA", -104.6f, -7.5f, -59.4f, -0.5f, 0f),
            R("Junction1", -116.6f, -7.5f, -104f, -0.5f, 0f, true, false),
            R("CausewayB", -170f, -7.5f, -116f, -0.5f, 0f),
            R("Bend1", -179.5f, -7.5f, -169.4f, 3.5f, 0f),
            R("CausewayC", -179.5f, 2.9f, -172.5f, 46.6f, 0f),
            R("Junction2", -179.5f, 46f, -172.5f, 58f, 0f, true, false),
            R("CausewayD", -179.5f, 57.4f, -172.5f, 100.6f, 0f),
            R("Workstations", -188f, 70f, -179f, 82f, 0f, false),              // the empty workstations beside the causeway (the bird)
            R("Bend2", -179.5f, 100f, -172.5f, 107.5f, 0f),
            R("CausewayE", -230f, 100.5f, -178.9f, 107.5f, 0f),
            R("Junction3", -242.6f, 100.5f, -229.4f, 107.5f, 0f, true, false),
            R("CausewayF", -300.6f, 100.5f, -242f, 107.5f, 0f),
            R("HallPlaza", -319.4f, 88f, -300f, 124f, 0f),
            R("HallDoorZ", -321f, 100f, -318.8f, 108f, 0f, true),                 // the hall's east entrance (shut by the shutters in AST_04)
            // the central maintenance hall + the temple archive
            R("Hall", -400f, 70f, -320f, 140f, 0f, false),
            R("Archive", -440f, 96f, -399.4f, 116f, 0f, false, false),
            // the maintenance opening -> the observation alcove -> the factory area
            R("MaintHatchZ", -330f, 65.4f, -326f, 70.6f, 0f, false),               // the maintenance opening itself (shut in AST_04 until AST_07)
            R("MaintPass", -330f, 50f, -326f, 66f, 0f, false),
            R("Alcove", -336f, 38f, -320f, 50.6f, 0f, false),
            R("WorksE", -336f, 4f, -320f, 38.6f, 0f, false),
            R("Platforms1", -352.6f, 14f, -335.4f, 20f, 0f, false, false),        // control 1: the cargo platforms line up across the pit
            R("WorksM", -372f, 4f, -352f, 34f, 0f, false),
            R("CoolPass", -386f, 10f, -371.4f, 18f, 0f, false, false),            // control 2: the steam dies down
            R("FinalPlat", -398.6f, 10f, -385.4f, 18f, 0f, false),                 // the final transport assembly (retracts, AST_06)
            R("LowLedge", -397.6f, 6f, -386f, 22f, LedgeY, false, false),         // the maintenance ledge below it
            R("WorksW", -412f, 4f, -398f, 30.6f, 0f, false),
            R("WorksWN", -412f, 22f, -392f, 30.6f, 0f, false),
            R("WestPass", -399f, 30f, -393f, 70.6f, 0f, false),                    // back to the hall (AST_07)
        };
        public static bool IsBikeZone(string name) { foreach (var z in Zones) if (z.Name == name) return z.Bike; return false; }
        public static bool BikeZone(Vector3 p, float pad = 0.6f)
        {
            foreach (var z in Ground.Zones) if (z.Enabled && z.Contains(p.x, p.z, pad) && IsBikeZone(z.Name)) return true;
            return false;
        }
        public static string ZoneAt(Vector3 p) => Ground.ZoneName(p.x, p.z) ?? "-";

        // ---- checkpoints (Continue / Scene Select)
        public const int CpArrival = 0, CpReceiving = 1, CpCity = 2, CpMachinist = 3, CpControls = 4, CpDrop = 5, CpFight = 6, CpArchive = 7, CpInstruction = 8, CpHome = 9, CpDone = 10;
        public static readonly string[] CpNames =
        {
            "The Asteroid: arrival", "The Asteroid: receiving", "The Asteroid: the city that still works", "The Asteroid: the Machinist",
            "The Asteroid: the three controls", "The Asteroid: a missed answer", "The Asteroid: the caretaker's objection",
            "The Asteroid: the archive + the vision", "The Asteroid: a different instruction", "The Asteroid: back aboard", "The Asteroid: done",
        };
    }
}
