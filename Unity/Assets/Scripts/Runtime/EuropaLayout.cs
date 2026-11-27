using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 14 — Chapter "Europa — Beneath the Ice" (Docs/CHAPTER_EUROPA.md): every number of the level in one place, for
    /// the builder (FightDemoBuilderEuropa) and the game (EuropaDirector).
    /// The surface: a sheltered ice shelf (dry, walkable) where the saucer hovers, and the opening into a sloping ice
    /// passage. Water begins at <see cref="WaterY"/>. Under the ice: the great chamber (light through the ceiling, curtains of
    /// bubbles, ancient structures with small white lights, vast shapes far below), the Cetacean settlement on its east side
    /// (platforms around open swimming passages, the receiving platform, the race course with arches, ice shelves and pillars),
    /// the damaged migration route, the industrial approach to Tunneler's base (pipes, intakes), the base itself (on foot:
    /// dock, pressure chamber, corridor, control room with the screen, worker holding, transport dock, machinery section,
    /// observation window over his private dock), the escape tunnel and the open water beyond, and the Cetacean temple.
    /// Riding underwater (<see cref="HoverBike.Swim"/>) is kept inside the swim volumes (<see cref="SwimClamp"/>); the dry
    /// floors are ordinary ground zones (<see cref="Zones"/>), joined at the dock.
    /// </summary>
    public static class EuropaLayout
    {
        public const float WaterY = -6f;                     // the water's surface inside the passage
        public const float IceTop = 0f;                      // the ice field / shelf
        public const float CeilingY = -24f;                  // the underside of the ice over the great chamber

        // ---- the surface
        public static readonly Vector3 SaucerAt = new Vector3(0f, 0f, -6f);    // its root on the shelf; the underside SaucerHover above
        public const float SaucerHover = 3.35f;                                 // the shared design (CLAUDE.md "Saucer — fixed design")
        public static readonly Vector3 BeamFoot = new Vector3(0f, 0f, -6f);
        public static readonly Vector3 BikeDown = new Vector3(7.5f, 0f, 1.5f);   // QF13 (5): where the bike settles after gliding out of the hold (clear of the hull)
        public static readonly Vector3 SevenDown = new Vector3(0f, 0f, -6f);
        public static readonly Vector3 OpeningAt = new Vector3(0f, 0f, 12f);    // the hole in the ice

        // ---- the ride: waypoints (the guiding lights follow these; check runs and the player-style run steer by them)
        // QF29 (1): the second point was (0, -2, 22) - inside the ice field's 2 m slab (y -2..0; its hole only spans z 7..17): the guide
        // light led the bike into the slab's lip just past the hole (the check run's jam since QF28, 0.18.2's soft-lock). Now the route
        // drops through the hole (z 9 -> 16) and runs under the slab with the bike's body clear of it (core below y -3).
        public static readonly Vector3[] Dive = { new Vector3(0f, 0.6f, 9f), new Vector3(0f, -4.2f, 15.5f), new Vector3(0f, -6.5f, 28f), new Vector3(0f, -9f, 42f), new Vector3(0f, -16f, 64f), new Vector3(0f, -26f, 92f), new Vector3(0f, -38f, 120f) };
        public static readonly Vector3[] Descent = { new Vector3(0f, -44f, 140f), new Vector3(14f, -52f, 170f), new Vector3(34f, -58f, 200f), new Vector3(52f, -60f, 228f) };
        public static readonly Vector3 HugeOnePath0 = new Vector3(-90f, -78f, 150f), HugeOnePath1 = new Vector3(140f, -74f, 200f);
        public static readonly Vector3[] ToSettlement = { new Vector3(64f, -60f, 246f), new Vector3(78f, -61f, 262f), new Vector3(88f, -62f, 274f) };
        public static readonly Vector3 Receiving = new Vector3(92f, -62f, 280f);          // the receiving platform (she stops here)
        public static readonly Vector3 KeeperAt = new Vector3(104f, -60f, 292f);           // the keeper beside the platform
        public static readonly Vector3 RaceRegistration = new Vector3(70f, -62f, 290f);    // the registration arch
        public static readonly Vector3[] RaceGates =
        {
            new Vector3(50f, -64f, 300f), new Vector3(20f, -70f, 312f), new Vector3(-6f, -78f, 300f), new Vector3(-14f, -84f, 270f),
            new Vector3(4f, -80f, 244f), new Vector3(34f, -72f, 236f), new Vector3(58f, -66f, 252f), new Vector3(68f, -62f, 278f),
        };
        public static readonly Vector3[] RacePillars = { new Vector3(-2f, -80f, 286f), new Vector3(10f, -78f, 258f), new Vector3(40f, -68f, 304f) };
        public static readonly Vector3 DamagedPassage = new Vector3(118f, -66f, 330f);    // section 6: the keeper shows the migration route
        public static readonly Vector3[] Approach = { new Vector3(100f, -70f, 320f), new Vector3(60f, -80f, 350f), new Vector3(10f, -88f, 380f), new Vector3(-40f, -92f, 410f), new Vector3(-90f, -94f, 430f), new Vector3(-140f, -95f, 444f), new Vector3(-166f, -95f, 450f) };
        public static readonly Vector3 ObservePoint = new Vector3(-120f, -92f, 440f);     // section 7: watch the workers before going in
        public static readonly Vector3 BayPark = new Vector3(-172f, -95.6f, 450f);         // the bike in the access bay (water), beside the dock

        // ---- the base (dry, on foot, helmet on)
        public const float BaseY = -94f;
        public static readonly Vector3 DockStep = new Vector3(-178.5f, BaseY, 450f);       // she steps off onto the dock here
        public static readonly Vector3 PressureIn = new Vector3(-181f, BaseY, 455.5f), PressureOut = new Vector3(-181f, BaseY, 462.5f);
        public static readonly Vector3 ScreenAt = new Vector3(-181f, BaseY + 2.4f, 493.6f); // Tunneler's screen on the control room's north wall
        public static readonly Vector3 ControlSpot = new Vector3(-181f, BaseY, 487f);
        public static readonly Vector3 HoldingDoor = new Vector3(-192f, BaseY, 488f);
        public static readonly Vector3 HoldingSpot = new Vector3(-200f, BaseY, 489f);
        public static readonly Vector3 TransportPanel = new Vector3(-209.4f, BaseY, 485f);   // "open transport access"
        public static readonly Vector3 LiftPanel = new Vector3(-218f, BaseY, 494.4f);        // "restore power to the evacuation lift"
        public static readonly Vector3 TransportDock = new Vector3(-221f, BaseY, 489f);
        public static readonly Vector3 SchematicAt = new Vector3(-203f, BaseY, 495.4f);
        public static readonly Vector3 MachineDoor = new Vector3(-170f, BaseY, 488f);
        public static readonly Vector3[] DeviceSpots = { new Vector3(-163f, BaseY, 482.6f), new Vector3(-155f, BaseY, 497.4f), new Vector3(-151.6f, BaseY, 486f) };
        public static readonly Vector3 WindowSpot = new Vector3(-181f, BaseY, 503.5f);       // section 11: the sealed observation window
        public static readonly Vector3 TunnelerDock = new Vector3(-181f, BaseY - 1f, 514f);  // his craft, beyond the glass
        public static readonly Vector3 HoldingScreeAt = new Vector3(-196.5f, BaseY, 490.5f);   // the worker who steps to the threshold

        // ---- the escape (section 12) and after
        public static readonly Vector3[] Escape =
        {
            new Vector3(-168f, -95.5f, 446f), new Vector3(-176f, -96f, 420f), new Vector3(-196f, -96f, 396f), new Vector3(-212f, -94f, 366f),
            new Vector3(-222f, -92f, 336f), new Vector3(-226f, -90f, 306f), new Vector3(-228f, -88f, 280f), new Vector3(-230f, -86f, 250f),
        };
        public static readonly Vector3 EscapeJunction = new Vector3(-196f, -96f, 396f);   // "Left at the junction"
        public static readonly Vector3[] EscapeSupports = { new Vector3(-209f, -94.5f, 372f), new Vector3(-220f, -92f, 342f), new Vector3(-227.5f, -89f, 298f) };
        public static readonly Vector3 EscapeNarrow = new Vector3(-224f, -90f, 320f);
        public static readonly Vector3 EscapeMouth = new Vector3(-229f, -87f, 262f);      // she bursts out here
        public static readonly Vector3 OpenWaterStop = new Vector3(-232f, -84f, 226f);
        public static readonly Vector3[] ToTemple = { new Vector3(-226f, -88f, 206f), new Vector3(-214f, -94f, 186f), new Vector3(-204f, -98f, 170f) };
        public static readonly Vector3 TempleCentre = new Vector3(-196f, -104f, 150f);
        /// <summary>QF38 (3): the temple's entrance - an open gap in the hall's wall (Lathe angle, deg: x = cos, z = sin) facing the
        /// route in from the open water. Round 14 built the hall as a closed dome; since QF15 the bike collides with every visual
        /// mesh under water, so the dome wall shut the route (the bots got through only by the 2-minute skip).</summary>
        public const float TempleDoorDeg = 118.125f, TempleDoorHalf = 16.875f, TempleDoorTop = 30f;   // the wall faces with a mid angle within the half-width and a mid height below the top (rel. TempleCentre) are left out
        public static Vector3 TempleDoor => TempleCentre + new Vector3(Mathf.Cos(TempleDoorDeg * Mathf.Deg2Rad) * 48.5f, 12f, Mathf.Sin(TempleDoorDeg * Mathf.Deg2Rad) * 48.5f);
        /// <summary>QF38 (3): the swim path the build check and the runtime check follow: open water -> the route -> the rune.</summary>
        public static Vector3[] TemplePath => new[] { OpenWaterStop, ToTemple[0], TempleDoor, ToTemple[1], ToTemple[2], RuneView };
        public static readonly Vector3 RuneAt = new Vector3(-196f, -100.6f, 142f);        // Europa's record: the icosahedron
        public static readonly Vector3 RuneView = new Vector3(-196f, -100.4f, 149f);       // she stops here (the vision as she reaches it)
        public static readonly Vector3[] Home = { new Vector3(-150f, -80f, 170f), new Vector3(-60f, -50f, 140f), new Vector3(0f, -38f, 120f) };

        // ---- swim volumes (riding stays inside their union)
        public struct Capsule { public Vector3 A, B; public float R; }
        public struct Box { public Vector3 Min, Max; }
        public static readonly Capsule[] Capsules =
        {
            new Capsule { A = new Vector3(0f, 2.5f, 4f), B = new Vector3(0f, -9f, 42f), R = 5.5f },                 // the ice passage
            new Capsule { A = new Vector3(0f, -9f, 42f), B = new Vector3(0f, -36f, 116f), R = 7f },
            new Capsule { A = new Vector3(100f, -70f, 320f), B = new Vector3(-40f, -92f, 410f), R = 11f },           // the industrial approach
            new Capsule { A = new Vector3(-40f, -92f, 410f), B = new Vector3(-166f, -95f, 450f), R = 9f },
            new Capsule { A = new Vector3(-168f, -95.5f, 446f), B = new Vector3(-196f, -96f, 396f), R = 6f },        // the escape tunnel
            new Capsule { A = new Vector3(-196f, -96f, 396f), B = new Vector3(-222f, -92f, 336f), R = 6f },
            new Capsule { A = new Vector3(-222f, -92f, 336f), B = new Vector3(-229f, -87f, 262f), R = 6.5f },
            new Capsule { A = new Vector3(-150f, -80f, 170f), B = new Vector3(0f, -38f, 120f), R = 16f },           // the way home
        };
        public static readonly Box[] Boxes =
        {
            new Box { Min = new Vector3(-150f, -120f, 112f), Max = new Vector3(150f, -28f, 340f) },                 // the great chamber
            new Box { Min = new Vector3(-175.2f, -101f, 444f), Max = new Vector3(-164f, -94.3f, 456f) },            // the access bay pool (QF15: stops at the dock's edge - the bike slipped under the dock floor and the camera had to jump in)
            new Box { Min = new Vector3(-300f, -125f, 120f), Max = new Vector3(-150f, -40f, 265f) },                // the open water + the temple
            new Box { Min = new Vector3(100f, -80f, 310f), Max = new Vector3(135f, -55f, 345f) },                   // the damaged passage mouth
        };

        static Vector3 ClosestOn(Capsule c, Vector3 p, out float d)
        {
            var ab = c.B - c.A; float t = Mathf.Clamp01(Vector3.Dot(p - c.A, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
            var q = c.A + ab * t; var v = p - q; d = v.magnitude;
            return d <= c.R ? p : q + v / Mathf.Max(1e-4f, d) * c.R;
        }
        static Vector3 ClosestIn(Box b, Vector3 p) => new Vector3(Mathf.Clamp(p.x, b.Min.x, b.Max.x), Mathf.Clamp(p.y, b.Min.y, b.Max.y), Mathf.Clamp(p.z, b.Min.z, b.Max.z));

        /// <summary>Is p inside the swim space (with a margin r from its walls)?</summary>
        public static bool InSwim(Vector3 p, float r = 0f)
        {
            foreach (var c in Capsules) { var ab = c.B - c.A; float t = Mathf.Clamp01(Vector3.Dot(p - c.A, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude)); if ((p - (c.A + ab * t)).magnitude <= c.R - r) return true; }
            foreach (var b in Boxes) if (p.x >= b.Min.x + r && p.x <= b.Max.x - r && p.y >= b.Min.y + r && p.y <= b.Max.y - r && p.z >= b.Min.z + r && p.z <= b.Max.z - r) return true;
            return false;
        }

        /// <summary>The nearest point of the swim space to p (p itself when inside): riding slides along the walls.</summary>
        public static Vector3 SwimClamp(Vector3 p, float r = 1.0f)
        {
            if (InSwim(p, r)) return p;
            Vector3 best = p; float bd = float.MaxValue;
            foreach (var c in Capsules)
            {
                var cc = c; cc.R = Mathf.Max(0.1f, c.R - r);
                var q = ClosestOn(cc, p, out _); float d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; }
            }
            foreach (var b in Boxes)
            {
                var bb = new Box { Min = b.Min + Vector3.one * r, Max = b.Max - Vector3.one * r };
                var q = ClosestIn(bb, p); float d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; }
            }
            return best;
        }

        public static bool Underwater(Vector3 p) => p.y < WaterY;

        // ---- the dry ground (on foot): the shelf on the surface and the base's rooms (overlapping >= 0.6 m at doors)
        public struct ZoneDef { public string Name; public float X0, Z0, X1, Z1, Y; public bool On; }
        static ZoneDef R(string n, float x0, float z0, float x1, float z1, float y, bool on = true) => new ZoneDef { Name = n, X0 = x0, Z0 = z0, X1 = x1, Z1 = z1, Y = y, On = on };
        public static readonly ZoneDef[] Zones =
        {
            R("Shelf", -16f, -18f, 16f, 6f, IceTop),
            R("Dock", -183f, 446.5f, -176f, 453.5f, BaseY),
            R("Pressure", -183f, 453f, -179f, 463f, BaseY),
            R("Corridor", -183f, 462.4f, -179f, 481f, BaseY),
            R("Control", -191f, 480.4f, -171f, 493f, BaseY),
            R("HoldingDoor", -193.2f, 486.4f, -190.4f, 489.6f, BaseY, false),     // opens in section 8 (barrier) -> 9
            R("Holding", -208.6f, 481.6f, -192.6f, 495f, BaseY),
            R("TransportDoor", -210.2f, 486.4f, -208f, 489.6f, BaseY, false),     // the worker opens it (section 9)
            R("Transport", -224f, 481.6f, -209.6f, 495f, BaseY),
            R("MachineDoor", -171.6f, 486.4f, -168.2f, 489.6f, BaseY, false),     // overlaps both rooms >= 0.6 m (was 0.4: she could not pass)     // after the evacuation is under way
            R("Machinery", -169f, 480.6f, -149f, 499f, BaseY),
            R("WindowHall", -183f, 492.4f, -179f, 504.5f, BaseY, false),          // section 11 (on her way back)
        };
        public static readonly string[] ZoneNames = System.Array.ConvertAll(Zones, z => z.Name);
        /// <summary>QF14 (6): the name of the (first) dry-ground zone containing p, or "-".</summary>
        public static string ZoneAt(Vector3 p) { foreach (var z in Zones) if (p.x >= z.X0 && p.x <= z.X1 && p.z >= z.Z0 && p.z <= z.Z1 && Mathf.Abs(p.y - z.Y) < 3f) return z.Name; return "-"; }

        // ---- the checkpoints (Continue / Scene Select)
        public const int CpArrival = 0, CpDescent = 1, CpSettlement = 2, CpApproach = 3, CpBase = 4, CpWorkers = 5, CpEscape = 6, CpTemple = 7, CpHome = 8, CpDone = 9;
        public static readonly string[] CpNames =
        {
            "Europa: arrival + the dive", "Europa: first descent", "Europa: the settlement + the keeper (race optional)", "Europa: the machinery + the way in",
            "Europa: Tunneler's base", "Europa: freeing the Scree + the devices", "Europa: Tunneler escapes + the escape ride", "Europa: the temple + the vision",
            "Europa: home - the reunion + the debrief", "Europa: done",
        };
    }
}
