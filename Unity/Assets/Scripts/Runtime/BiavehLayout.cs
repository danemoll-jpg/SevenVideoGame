using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 17 — Chapter "The Biavian Mothership (Veyra)" (Docs/CHAPTER_BIAVEH.md): the numbers the builder and the game share.
    /// The play floor is flat (y 0) at the bottom of the interior city; the city curves up the shell beyond it (visual).
    ///  - the receiving berth (BIA_03): the saucer hovers at the origin, the overlook on the berth's north edge (Nela waits
    ///    there), the temple-route display beside it (BIA_04), the passage where the memory echo appears (east);
    ///  - the east road → the service junction under a high bridge (BIA_05: Ratwing clings beneath it; the damaged relay
    ///    cabinet) → the chasm with the suspended bridge (BIA_06: relay 1 aligns it; the echo of an old span beside it);
    ///  - the road on → north to the overlapping garden (BIA_07: the remembered doorway in its north wall, the seam, relay 2,
    ///    the glass partition and the upper opening Ratwing eyes);
    ///  - west from the garden: the shielded quiet passage (BIA_08: an airlock pair - a short solo spatial puzzle - relay 3);
    ///  - north: the temple's transfer chamber (BIA_09: the fight - platforms, three partitions, the maintenance compartment
    ///    with its outer + inner barriers; the damaged archive entrance), then the memory chamber with the octahedron (BIA_11).
    /// Walkable / ridable areas are rectangles (Ground zones, overlapping ≥ 0.6 m); the bridge zone opens when it is aligned.
    /// </summary>
    public static class BiavehLayout
    {
        public const float SaucerHover = 3.35f;
        public static readonly Vector3 SaucerAt = Vector3.zero;
        public static readonly Vector3 SevenDown = new Vector3(0f, 0f, 0f);
        public static readonly Vector3 BikePad = new Vector3(9f, 0f, 6f);
        // BIA_03 / BIA_04 at the overlook
        public static readonly Vector3 OverlookAt = new Vector3(-17.5f, 0f, 0f);         // the west edge of the berth: the city curves up beyond it
        public static readonly Vector3 NelaAt = new Vector3(-18.2f, 0f, -2.4f);
        public static readonly Vector3 DisplayAt = new Vector3(-16.8f, 0f, 4.2f);        // the temple route display (a holo table)
        public static readonly Vector3 EchoAt = new Vector3(15f, 0f, 15.5f);            // the memory echo across the passage
        public static readonly Vector3 ResidentAAt = new Vector3(13.2f, 0f, 14f);       // the disoriented resident
        public static readonly Vector3 BenchAt = new Vector3(13.8f, 0f, 17.6f);         // Nela helps them sit here
        public static readonly Vector3 SealedGate = new Vector3(19.5f, 3f, 21f);        // Nela looks toward it (BIA_03_09)
        // BIA_05: the service junction under the high bridge; the damaged relay cabinet
        public static readonly Vector3 JunctionStop = new Vector3(128f, 0f, 0f);
        public static readonly Vector3 PillarAt = new Vector3(131f, 0f, 3.6f);          // she stops behind it
        public static readonly Vector3 CabinetAt = new Vector3(140.5f, 0f, 6.2f);       // relay 1 (damaged)
        public static readonly Vector3 CabinetView = new Vector3(140.5f, 0f, 4.6f);
        public const float HighBridgeY = 15f;
        public static readonly Vector3 RatwingPerch = new Vector3(141f, 13.35f, -2f);   // hanging beneath the high bridge (upside down, among its supports)
        public static readonly Vector3 RatwingDrop = new Vector3(160f, 8.6f, -19f);     // the distant platform
        public static readonly Vector3 ShaftTop = new Vector3(168f, 60f, -34f);         // he launches up the vertical shaft
        // BIA_06: the chasm, the suspended bridge (three segments), the service controls, the stranded residents
        public const float ChasmX0 = 150f, ChasmX1 = 190f;
        public static readonly Vector3 BridgeControl = new Vector3(147.4f, 0f, -6.6f);
        public static readonly Vector3 BridgeControlView = new Vector3(146.2f, 0f, -5.2f);
        public static readonly Vector3 FarSide = new Vector3(196f, 0f, 0f);
        public static readonly Vector3 PartitionA = new Vector3(170f, 22f, -46f);       // closes over one of Ratwing's routes (visible)
        // BIA_07: the garden
        public static readonly Vector3 GardenGate = new Vector3(260f, 0f, 40f);
        public static readonly Vector3 GardenPark = new Vector3(260f, 0f, 46f);
        public static readonly Vector3 EchoDoor = new Vector3(258f, 1.6f, 91.55f);     // the remembered doorway in the north wall
        public static readonly Vector3 SeamAt = new Vector3(262.6f, 0.35f, 91.5f);      // the seam lower down (the release)
        public static readonly Vector3 SeamView = new Vector3(262.6f, 0f, 90.2f);
        public static readonly Vector3 Relay2At = new Vector3(268.5f, 0f, 90.4f);       // revealed when the panel slides back
        public static readonly Vector3 Relay2View = new Vector3(268.5f, 0f, 89.0f);
        public static readonly Vector3 GlassAt = new Vector3(272f, 0f, 92f);             // the glass partition (Ratwing beyond, z > 92)
        public static readonly Vector3 RatwingGarden = new Vector3(274f, 1.2f, 95.5f);
        public static readonly Vector3 UpperOpening = new Vector3(276f, 7.5f, 92f);
        public static readonly Vector3 OpeningControl = new Vector3(270.6f, 0f, 90.6f);
        public static readonly Vector3 OpeningControlView = new Vector3(270.6f, 0f, 89.2f);
        // BIA_08: the quiet passage (entrance east, the relay room west), the airlock pair
        public static readonly Vector3 PassagePark = new Vector3(212f, 0f, 66f);
        public static readonly Vector3 PassageDoor = new Vector3(206f, 0f, 66f);
        public static readonly Vector3 LockOuter = new Vector3(196f, 0f, 66f);          // airlock door A
        public static readonly Vector3 LockInner = new Vector3(186f, 0f, 66f);          // airlock door B
        public static readonly Vector3 LockPanelA = new Vector3(199f, 0f, 63.2f);       // opens A (outside the lock)
        public static readonly Vector3 LockPanelB = new Vector3(191f, 0f, 68.8f);       // inside: closes A, opens B
        public static readonly Vector3 Relay3At = new Vector3(174.5f, 0f, 66f);
        public static readonly Vector3 Relay3View = new Vector3(176f, 0f, 66f);
        // BIA_09: the transfer chamber (the fight) and the archive entrance
        public static readonly Vector3 ChamberEntry = new Vector3(208f, 0f, 112f);
        public static readonly Vector3 ChamberCover = new Vector3(198f, 0f, 116f);     // she brakes behind cover
        public static readonly Vector3 ArchiveEntrance = new Vector3(175f, 0f, 150f);
        public static readonly Vector3 RatwingStart = new Vector3(175f, 5f, 140f);
        public static readonly Vector3 HerFightStart = new Vector3(184f, 0f, 118f);
        public const float ArenaX0 = 152f, ArenaX1 = 198f, ArenaZ0 = 104f, ArenaZ1 = 146f;
        public static readonly Vector3[] PartitionStations = { new Vector3(156f, 0f, 108f), new Vector3(195f, 0f, 142f) };
        public static readonly Vector3[] Partitions = { new Vector3(175f, 0f, 104f), new Vector3(198f, 0f, 125f) };   // close across the shaft (phase 1 / 2)
        public static readonly Vector3 CompartmentIn = new Vector3(144f, 0f, 125f);     // the empty maintenance compartment
        public static readonly Vector3 OuterBarrier = new Vector3(150.5f, 0f, 125f);   // seals it (BIA_09_11 "Now.")
        public static readonly Vector3 InnerBarrier = new Vector3(138.5f, 0f, 125f);
        public static readonly Vector3 BarrierControl = new Vector3(156f, 0f, 131f);
        public static readonly Vector3 BarrierControlView = new Vector3(157.4f, 0f, 131f);
        public static readonly Vector3 CoverRight = new Vector3(160f, 0f, 133f);        // "the opening on your right"
        public static readonly Vector3 RoutingControl = new Vector3(178.5f, 0f, 146.6f); // BIA_10: the final routing control
        public static readonly Vector3 RoutingView = new Vector3(178.5f, 0f, 145f);
        // BIA_11: the memory chamber, the octahedron
        public static readonly Vector3 TempleDoor = new Vector3(175f, 0f, 151f);
        public static readonly Vector3 OctaAt = new Vector3(175f, 1.14f, 172f);      // QF24 (20): the smaller octahedron (0.44 m across) hovers just over its plinth
        public static readonly Vector3 OctaView = new Vector3(175f, 0f, 170.6f);
        public static readonly Vector3 SaucerHome = new Vector3(0f, 0f, 0f);

        // ---- the walkable / ridable floor (rectangles; Bike = may be ridden)
        public struct ZoneDef { public string Name; public float X0, Z0, X1, Z1, Y0, Y1; public int Axis; public bool On, Bike; }
        static ZoneDef R(string n, float x0, float z0, float x1, float z1, bool bike = true, bool on = true) => new ZoneDef { Name = n, X0 = x0, Z0 = z0, X1 = x1, Z1 = z1, Y0 = 0f, Y1 = 0f, On = on, Bike = bike };
        public static readonly ZoneDef[] Zones =
        {
            R("Berth", -22f, -22f, 22f, 20.4f),
            R("EchoPassage", 10f, 10f, 24f, 20.4f, false),
            R("RoadEast", 21.4f, -5f, 151f, 5f),
            R("Junction", 120f, -12f, 151f, 12f),
            R("Bridge", 150.4f, -2.6f, 189.6f, 2.6f, true, false),          // the suspended bridge (aligned in BIA_06)
            R("FarSide", 189f, -8f, 206f, 8f),
            R("RoadOn", 205.4f, -5f, 265f, 5f),
            R("RoadNorth", 255f, 4.4f, 265f, 42f),
            R("Garden", 236f, 41.4f, 284f, 91.4f, false),
            R("GardenAvenue", 255f, 41f, 265f, 71.6f),                      // the avenue through the garden (the bike may use it)
            R("GardenRoad", 205.4f, 61f, 255.6f, 71f),                      // west from the garden to the quiet passage
            R("GardenGateZ", 250f, 61f, 260f, 71f),
            R("PassageEntry", 204f, 62f, 214f, 70f),
            R("PassageA", 196.4f, 63.4f, 205f, 68.6f, false),                // before the airlock
            R("LockDoorA", 195.4f, 63.4f, 197f, 68.6f, false, false),        // airlock door A (opens with panel A)
            R("Lock", 186.4f, 63.4f, 196f, 68.6f, false),
            R("LockDoorB", 185.4f, 63.4f, 187f, 68.6f, false, false),        // airlock door B (opens with panel B, A closed)
            R("Relay3Room", 170f, 58f, 186f, 74f, false),
            R("RoadToChamber", 205.4f, 69.4f, 214f, 113f),
            R("Chamber", 150f, 102f, 214f, 148f),
            R("ArchiveDoorZ", 171f, 147.4f, 179f, 152f, false, false),        // the archive (opens in BIA_10)
            R("Temple", 160f, 151.4f, 190f, 185f, false, false),
            R("Compartment", 136f, 120f, 151f, 130f, false, false),           // the maintenance compartment (Ratwing only)
        };
        public static bool IsBikeZone(string name) { foreach (var z in Zones) if (z.Name == name) return z.Bike; return false; }
        public static bool BikeZone(Vector3 p, float pad = 0.6f)
        {
            foreach (var z in Ground.Zones) if (z.Enabled && z.Contains(p.x, p.z, pad) && IsBikeZone(z.Name)) return true;
            return false;
        }
        public static string ZoneAt(Vector3 p) => Ground.ZoneName(p.x, p.z) ?? "-";

        // ---- routes (the guide light)
        public static readonly Vector3[] RideToJunction = { new Vector3(30f, 0f, 0f), new Vector3(80f, 0f, 0f), JunctionStop };
        public static readonly Vector3[] RideToGarden = { new Vector3(200f, 0f, 0f), new Vector3(240f, 0f, 0f), new Vector3(260f, 0f, 0f), new Vector3(260f, 0f, 30f), GardenPark };
        public static readonly Vector3[] RideToPassage = { new Vector3(260f, 0f, 52f), new Vector3(258f, 0f, 66f), new Vector3(230f, 0f, 66f), PassagePark };
        /// <summary>The garden's planters (trees in round beds) - the builder places them, the director makes them obstacles.</summary>
        public static System.Collections.Generic.List<Vector3> GardenPlanters()
        {
            var l = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i < 26; i++)
            {
                var p = new Vector3(240f + (i * 37) % 42, 0f, 46f + (i * 23) % 42);
                if (Mathf.Abs(p.x - 260f) < 6f || (p.z > 80f && p.x > 252f) || (p.z > 61f && p.z < 71f && p.x < 256f)) continue;   // clear of the avenue, the wall's works, the road west
                l.Add(p);
            }
            return l;
        }
        public static readonly Vector3[] RideToChamber = { new Vector3(210f, 0f, 80f), new Vector3(210f, 0f, 104f), ChamberEntry };
        public static readonly Vector3[] RideHome = { ChamberEntry, new Vector3(210f, 0f, 104f), new Vector3(210f, 0f, 70f), new Vector3(240f, 0f, 66f), new Vector3(260f, 0f, 30f), new Vector3(260f, 0f, 0f), new Vector3(200f, 0f, 0f), new Vector3(170f, 0f, 0f), new Vector3(130f, 0f, 0f), new Vector3(40f, 0f, 0f), new Vector3(10f, 0f, 2f) };

        /// <summary>The interior shell (visual): the city floor bends up on the west, north, east and south beyond the play area.</summary>
        public static float ShellRise(float x, float z)
        {
            const float R = 320f;
            float Up(float d) { d = Mathf.Max(0f, d); return d >= R * 0.98f ? R : R - Mathf.Sqrt(R * R - d * d); }
            return Up(-40f - x) + Up(z - 210f) + Up(x - 300f) + Up(-80f - z);
        }
        public const int CpArrival = 0, CpBerth = 1, CpRelay1 = 2, CpRelay2 = 3, CpRelay3 = 4, CpFight = 5, CpTemple = 6, CpHome = 7, CpDone = 8;
        public static readonly string[] CpNames =
        {
            "among Veyra's rings", "a world inside", "the first relay", "the overlapping garden", "the quiet passage", "cornering Ratwing",
            "the octahedron", "back aboard", "done",
        };
    }
}
