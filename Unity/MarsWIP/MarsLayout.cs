using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 15 — Chapter "Mars — The Divided Kingdoms" (Docs/CHAPTER_MARS.md): every number of the level in one place, for
    /// the builder (FightDemoBuilderMars) and the game (MarsDirector).
    /// An immense pale plain cut by walls, trenches and elevated roads; a deep ravine runs north-south through it (x -50..50,
    /// floor <see cref="FloorY"/>). The west bank: the landing site, the Reptilian settlement (main street, market, side
    /// streets), the checkpoint at the main crossing (the barrier, the stranded supply transport, the guard, the worker) and
    /// Viper's elevated platform. Under the main crossing: the old maintenance route down the west wall, across the ravine
    /// floor (a broken causeway, a sheltered passage, broad riding sections, lookouts) and up the east wall to its service
    /// gate. The east bank: the caretaker's post, the rim road north to the shared mechanism overlooking the temple and the
    /// east control. North in the ravine the temple stands on a central island of stone, older than everything round it;
    /// the ancient bridge's two spans (misaligned until section 6) join it to the west bluff (the west control, the pier
    /// walk) and the east bluff. Two fortress cities stand far off on either side (silhouettes in the haze).
    /// Riding = the bike zones (<see cref="BikeZone"/>); on foot = every enabled zone. Zones overlap >= 0.6 m where they join.
    /// </summary>
    public static class MarsLayout
    {
        public const float FloorY = -22f;                 // the ravine floor
        public const float RavineX = 50f;                 // the ravine's walls at x = +-50
        public const float SaucerHover = 3.35f;           // the shared design (CLAUDE.md "Saucer — fixed design")

        // ---- arrival
        public static readonly Vector3 SaucerAt = new Vector3(-205f, 0f, 0f);
        public static readonly Vector3 SevenDown = new Vector3(-205f, 0f, 0f);
        public static readonly Vector3 BikeDown = new Vector3(-196f, 0f, 6f);
        public static readonly Vector3[] ToCheckpoint = { new Vector3(-176f, 0f, 0f), new Vector3(-146f, 0f, 0f), new Vector3(-120f, 0f, 0f), new Vector3(-94f, 0f, 0f), new Vector3(-72f, 0f, 0f) };

        // ---- the settlement (west bank) and the checkpoint
        public static readonly Vector3 MarketAt = new Vector3(-120f, 0f, 0f);
        public static readonly Vector3 CheckStop = new Vector3(-70f, 0f, 2f);          // she stops the bike here (section 2)
        public static readonly Vector3 GuardAt = new Vector3(-54f, 0f, 3.5f);
        public static readonly Vector3 WorkerAt = new Vector3(-60f, 0f, -7.5f);
        public static readonly Vector3 TransportAt = new Vector3(-62f, 0f, -3.2f);    // the stranded supply transport behind the barrier
        public static readonly Vector3 BarrierAt = new Vector3(-51f, 0f, 0f);         // the closed barrier across the bridge head
        public static readonly Vector3 PlatformAt = new Vector3(-44f, 0f, 9f);        // Viper's elevated platform (rises to PlatformTop)
        public const float PlatformTop = 7.5f;

        // ---- the maintenance route (section 4) - down the west wall, across the floor, up the east wall
        public static readonly Vector3[] Maint =
        {
            new Vector3(-58f, 0f, -16f), new Vector3(-56f, -10f, -34f), new Vector3(-56f, FloorY, -62f), new Vector3(-34f, FloorY, -72f),
            new Vector3(-12f, FloorY, -67f), new Vector3(12f, FloorY, -67f), new Vector3(30f, FloorY, -80f), new Vector3(46f, FloorY, -74f),
            new Vector3(56f, FloorY, -62f), new Vector3(56f, -10f, -34f), new Vector3(58f, 0f, -16f),
        };
        public static readonly Vector3 CausewaySlabRest = new Vector3(-13f, FloorY, -76.5f);   // the fallen slab beside the gap
        public static readonly Vector3 CausewayGap = new Vector3(0f, FloorY, -67f);            // the broken causeway over the channel
        public static readonly Vector3 CausewayStop = new Vector3(-14f, FloorY, -67f);
        public const float SlabTkRange = 15f;
        public static readonly Vector3[] Lookouts = { new Vector3(-40f, FloorY, -85f), new Vector3(40f, FloorY, -86f) };   // clear stopping points
        public static readonly Vector3 ServiceGatePanel = new Vector3(60.5f, 0f, -14f);        // the east top: the service gate (X)
        public static readonly Vector3 ServiceGateAt = new Vector3(58f, 0f, -12.5f);
        public static readonly Vector3 CaretakerAt = new Vector3(63f, 0f, -2f);
        public static readonly Vector3 EastStop = new Vector3(60f, 0f, -8f);

        // ---- the shared door (section 5) and the two controls (section 6)
        public static readonly Vector3 MechanismAt = new Vector3(60f, 0f, 122f);               // overlooking the temple
        public static readonly Vector3 MechanismView = new Vector3(64f, 0f, 118f);
        public static readonly Vector3 WestControl = new Vector3(-60f, 0f, 152f);
        public static readonly Vector3 EastControl = new Vector3(60f, 0f, 152f);
        public static readonly Vector3 RelayWest = new Vector3(-64f, 0f, 104f);                // ride to it, X: reconnect
        public static readonly Vector3 PierConduit = new Vector3(-46.2f, -4f, 166f);           // on foot down the pier walk; telekinesis
        public static readonly Vector3 PierConduitRest = new Vector3(-45.6f, -5.2f, 170.4f);
        public static readonly Vector3 PierWalkTop = new Vector3(-56f, 0f, 160f);
        public static readonly Vector3 SwingSegment = new Vector3(-36f, -1f, 150f);           // "Not that one!" - the span section that moves
        public static readonly Vector3[] JammerFrom = { new Vector3(-80f, 6f, 176f), new Vector3(-44f, 7f, 182f), new Vector3(-86f, 5f, 136f) };

        // ---- the temple approach + the encounter (section 7) - the island plaza
        public static readonly Vector3 IslandC = new Vector3(0f, 0f, 150f);
        public const float SpanZ0 = 146f, SpanZ1 = 154f;                                         // both spans: z 146..154
        public static readonly Vector3 SpanWestFoot = new Vector3(-46f, 0f, 150f), SpanEastFoot = new Vector3(46f, 0f, 150f);
        public static readonly Vector3 ViperBlocks = new Vector3(0f, 0f, 165f);                // in front of the temple entrance
        public static readonly Vector3 ArenaEntry = new Vector3(-18f, 0f, 150f);
        public static readonly Vector3[] Pylons = { new Vector3(-12f, 0f, 129f), new Vector3(14f, 0f, 132f), new Vector3(12f, 0f, 160f) };
        public static readonly Vector3 FallenWalk = new Vector3(14f, 0f, 146.5f);              // a walkway section to lift into place (TK)
        public static readonly Vector3 FallenWalkRest = new Vector3(19.5f, -1.4f, 141f);
        public const float SlideGateZ = 143f;                                                    // the moving section (timing)

        // ---- the temple (sections 8-10)
        public static readonly Vector3 TempleDoor = new Vector3(0f, 0f, 170f);
        public static readonly Vector3 SymbolWall = new Vector3(0f, 2.6f, 189.6f);
        public static readonly Vector3 RuneAt = new Vector3(0f, 1.25f, 183f);
        public static readonly Vector3 RuneView = new Vector3(0f, 0f, 180.4f);
        public static readonly Vector3 DisplayAt = new Vector3(-6.5f, 1.8f, 184f);

        // ---- the ride back (section 11)
        public static readonly Vector3[] Back = { new Vector3(-30f, 0f, 150f), new Vector3(-64f, 0f, 120f), new Vector3(-64f, 0f, 30f), new Vector3(-80f, 0f, 0f), new Vector3(-130f, 0f, 0f), new Vector3(-190f, 0f, 2f) };
        public static readonly Vector3 TransportCrossFrom = new Vector3(-62f, 0f, -1f), TransportCrossTo = new Vector3(66f, 0f, -1f);
        public static readonly Vector3 HomeStop = new Vector3(-196f, 0f, 6f);

        // ---- zones (order matters: the first zone containing a point gives its height)
        public struct ZoneDef { public string Name; public float X0, Z0, X1, Z1, Y0, Y1; public int Axis; public bool On, Bike; }
        static ZoneDef R(string n, float x0, float z0, float x1, float z1, float y, bool bike = true, bool on = true) => new ZoneDef { Name = n, X0 = x0, Z0 = z0, X1 = x1, Z1 = z1, Y0 = y, Y1 = y, Axis = 0, On = on, Bike = bike };
        /// <summary>A ramp: y0 at the low end of its axis (x or z), y1 at the high end.</summary>
        static ZoneDef S(string n, float x0, float z0, float x1, float z1, float y0, float y1, int axis, bool bike = true, bool on = true) => new ZoneDef { Name = n, X0 = x0, Z0 = z0, X1 = x1, Z1 = z1, Y0 = y0, Y1 = y1, Axis = axis, On = on, Bike = bike };
        public static readonly ZoneDef[] Zones =
        {
            // the temple, the island plaza and the spans (raised over the ravine)
            R("TempleHall", -9f, 169f, 9f, 191f, 0f, false, false),
            R("TempleSteps", -6f, 162f, 6f, 169.8f, 0f, false),
            // the encounter's three lanes (low ancient walls between them, gaps = gates): west, centre (split by Viper's
            // barrier), east (split by a broken strip the fallen walkway section bridges once she lifts it back)
            R("PlazaW", -21f, 124f, -7f, 168f, 0f, false),
            R("PlazaC_S", -6.4f, 124f, 6.4f, 150f, 0f, false),
            R("CentreBarrier", -6.4f, 149.4f, 6.4f, 151.6f, 0f, false),  // Viper's barrier across the centre lane (open = on)
            R("PlazaC_N", -6.4f, 151f, 6.4f, 168f, 0f, false),
            R("PlazaE_S", 7f, 124f, 21f, 144.5f, 0f, false),
            R("PlazaE_N", 7f, 148.5f, 21f, 168f, 0f, false),
            R("FallenWalkZ", 9f, 143.8f, 19f, 149.2f, 0f, false, false),     // the walkway section she lifts back (TK)
            R("GateWC", -7.6f, 136f, -5.8f, 140f, 0f, false),             // gaps in the low walls (Viper's barriers close them)
            R("GateCE", 5.8f, 156f, 7.6f, 160f, 0f, false),
            R("SlideGate", 5.8f, 141f, 7.6f, 145f, 0f, false),           // the moving section opens / closes it (timing)
            R("SpanW", -47f, SpanZ0, -20.4f, SpanZ1, 0f, true, false),    // the west span (aligned in section 6)
            R("SpanE", 20.4f, SpanZ0, 47f, SpanZ1, 0f, true, false),
            // the west bluff + the pier walk (on foot)
            S("PierStair", -54.6f, 156f, -48f, 164f, 0f, -4f, 0, false),
            R("PierWalk", -48.6f, 155f, -44f, 172f, -4f, false),
            R("SpanHeadW", -55f, 145f, -46f, 155f, 0f),
            R("SpanHeadE", 46f, 145f, 55f, 155f, 0f),
            R("WestBluff", -78f, 136f, -54f, 168f, 0f),
            R("WestRim", -76f, 4f, -56f, 137f, 0f),
            R("EastBluff", 54f, 136f, 78f, 168f, 0f),
            R("EastRim", 56f, 4f, 76f, 137f, 0f),
            R("Overlook", 50f, 108f, 76f, 132f, 0f),
            // the maintenance route (down the west wall, the ravine floor, up the east wall)
            S("MaintRampW", -60f, -60f, -52f, -11f, FloorY, 0f, 1),
            R("FloorW", -54f, -92f, -7f, -59.4f, FloorY),
            R("Causeway", -8f, -70f, 8f, -64f, FloorY, true, false),      // the broken causeway (the slab, section 4)
            R("FloorE", 7f, -92f, 54f, -59.4f, FloorY),
            S("MaintRampE", 52f, -60f, 60f, -11f, FloorY, 0f, 1),
            R("ServiceGateZ", 52f, -12f, 62f, -10f, 0f, true, false),     // the east service gate (opened from its panel)
            // the banks: the checkpoint plazas, the roads, the settlement, the landing site
            R("CheckW", -70f, -12f, -51.6f, 12f, 0f),
            R("Bridge", -52f, -4.5f, 52f, 4.5f, 0f, true, false),        // the main crossing (barred until the ride back)
            R("CheckE", 51.6f, -12f, 70f, 12f, 0f),
            R("EastRoad", 68f, -6f, 150f, 6f, 0f),
            R("Market", -136f, -16f, -104f, 16f, 0f),
            R("StreetN", -126f, 14f, -114f, 40f, 0f),
            R("StreetS", -146f, -40f, -134f, -4.5f, 0f),
            R("WestRoad", -184f, -6f, -68f, 6f, 0f),
            R("Landing", -232f, -26f, -182f, 26f, 0f),
        };
        public static bool IsBikeZone(string name) { foreach (var z in Zones) if (z.Name == name) return z.Bike; return false; }
        /// <summary>Is a point on one of the zones the bike may use?</summary>
        public static bool BikeZone(Vector3 p, float pad = 0.6f)
        {
            foreach (var z in Ground.Zones) if (z.Enabled && z.Contains(p.x, p.z, pad) && IsBikeZone(z.Name)) return true;
            return false;
        }
        public static string ZoneAt(Vector3 p) => Ground.ZoneName(p.x, p.z) ?? "-";

        // ---- the checkpoints (Continue / Scene Select)
        public const int CpArrival = 0, CpCrossing = 1, CpMaint = 2, CpSharedDoor = 3, CpControls = 4, CpViper = 5, CpTemple = 6, CpReturn = 7, CpHome = 8, CpDone = 9;
        public static readonly string[] CpNames =
        {
            "Mars: arrival", "Mars: the crossing + Viper", "Mars: the maintenance route", "Mars: the shared door", "Mars: the two controls",
            "Mars: Viper's intervention", "Mars: the temple + the vision", "Mars: the ride back", "Mars: aboard - the debrief + the choice", "Mars: done",
        };
    }
}
