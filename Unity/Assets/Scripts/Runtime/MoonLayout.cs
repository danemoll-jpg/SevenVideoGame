using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Chapter 1 level layout (metres; +X along the cell-block catwalk, -Z into the rock, down to the impound bay).
    /// Shared by the scene builder (geometry) and the runtime director (triggers, paths, check-run autopilot).
    /// </summary>
    public static class MoonLayout
    {
        public const float Upper = 0f, Lower = -2.4f, Bay = -10f;

        // ---- Cell (scene 1-2) and the catwalk outside it
        public static readonly Vector3 SevenStart = new Vector3(0.3f, 0f, -0.7f);
        public static readonly Vector3 CellDoor = new Vector3(0f, 0f, 1.65f);         // door centre (bars at z 1.65)
        public const float DoorHalf = 0.6f, CellWallZ0 = 1.3f, CellWallZ1 = 1.95f;
        public static readonly Vector3 VentMouth = new Vector3(-4.2f, 0f, 2.25f);     // the cat's way in (small opening)
        public static readonly Vector3 CatWaitOutside = new Vector3(-0.75f, 0f, 2.75f);
        // Round 7: horizontal laser beams flush with the cell's inner wall face (7's walkable cell ends at CellWallZ0).
        public const float LaserZ = CellWallZ0 + 0.015f;
        public const int LaserCount = 9;
        public static float LaserY(int i) => 0.2f + i * 0.27f;
        // Round 7: the release is inside a guard alcove recessed into the rock face west of the cell: out of reach and
        // out of sight from anywhere inside the cell (the cell's west wall and the face block every line of sight).
        public static readonly Vector2 AlcoveX = new Vector2(-3.6f, -2.4f);
        public const float AlcoveBackZ = 0.45f;
        public static readonly Vector3 AlcoveMouth = new Vector3(-3.0f, 0f, 2.45f);       // the cat turns in here
        public static readonly Vector3 CrateFoot = new Vector3(-3.0f, 0f, 1.5f);          // in the alcove, in front of the crate
        public static readonly Vector3 CrateTop = new Vector3(-3.0f, 0.45f, 0.86f);
        public static readonly Vector3 CrateSize = new Vector3(0.55f, 0.45f, 0.42f);
        public static readonly Vector3 ReleasePanel = new Vector3(-3.0f, 0.95f, AlcoveBackZ + 0.02f);
        public static readonly Vector3 DoorApproach = new Vector3(0f, 0f, 0.85f);
        public static readonly Vector3[] CorridorLead = { new Vector3(1.8f, 0f, 3.3f), new Vector3(8f, 0f, 3.4f), new Vector3(14.6f, 0f, 3.3f) };
        public const float Scene3TriggerX = 4.5f;
        public static readonly Vector3 SafeSpot = new Vector3(10.5f, 0f, 3.2f);
        public const float BarricadeX = 19f;
        public static readonly Vector3[] GatePosts = { new Vector3(24.6f, 0f, 3.5f), new Vector3(25.9f, 0f, 2.7f) };
        public static readonly Vector3 OldWall = new Vector3(16.5f, 1.2f, 1.95f);    // centre of the older wall section
        public static readonly Vector3 OldWallSpot = new Vector3(16.5f, 0f, 2.55f);   // where 7 stands to look closer

        // ---- Older passage down to the ancient hall (scene 4-5)
        public static readonly Vector2 PassageX = new Vector2(15.6f, 17.4f);
        public const float PassageTopZ = 0.2f, PassageBottomZ = -9f;
        public static readonly Vector3 HallEntry = new Vector3(16.5f, Lower, -9.7f);
        public const float HallEnterZ = -9.2f;
        public static readonly Vector2 HallMin = new Vector2(8.5f, -24f), HallMax = new Vector2(24.5f, -8.6f);
        public static readonly Vector3 GuardPost = new Vector3(16.5f, Lower, -21.7f);
        public static readonly Vector3 StoneRest = new Vector3(23.3f, Lower + 1.95f, -13.2f);
        public static readonly Vector3 StoneLand = new Vector3(22.2f, Lower + 0.32f, -12.2f);
        public const float StoneRadius = 0.32f, TkRange = 12f;
        public static readonly Vector3 StonePillar = new Vector3(23.4f, Lower, -13.2f);
        public static readonly Vector3[] Pillars =
        {
            new Vector3(12.4f, Lower, -12.3f), new Vector3(12.4f, Lower, -16.4f), new Vector3(12.4f, Lower, -20.4f),
            new Vector3(20.6f, Lower, -12.3f), new Vector3(20.6f, Lower, -16.4f), new Vector3(20.6f, Lower, -20.4f),
        };
        public const float PillarR = 0.62f;
        public static readonly Vector3[] SneakPath =
        {
            new Vector3(14.6f, Lower, -10.4f), new Vector3(10.4f, Lower, -12.4f), new Vector3(10.4f, Lower, -21.8f),
            new Vector3(14.2f, Lower, -23.4f), new Vector3(16.5f, Lower, -24.6f), new Vector3(16.5f, Lower, -27f),
        };
        public const float HallExitZ = -24.4f;

        // ---- Vision chamber (scene 6)
        public static readonly Vector2 VisionMin = new Vector2(12.5f, -38f), VisionMax = new Vector2(20.5f, -29.5f);
        public static readonly Vector3 Inscription = new Vector3(16.5f, Lower + 1.35f, -37.95f);
        public static readonly Vector3 InscriptionSpot = new Vector3(16.5f, Lower, -37.15f);
        public static readonly Vector3 SealedDoor = new Vector3(16.5f, Lower, -38.1f);
        // Round 9 (8b): the vision object - one of Kyle's floating wireframe shapes over a pale pedestal in an alcove in
        // the chamber's east wall. Change VisionShape to swap the shape (tetrahedron / cube / octahedron) everywhere.
        public const PlatonicShape.Kind VisionShape = PlatonicShape.Kind.Dodecahedron;
        // Round 10: a hand-held piece now - ~15 cm across, solid pale stone with thin glowing edges, hovering over the
        // pedestal; 7 takes it after the door scene. One face carries the (placeholder) Elarim marking: swap the image
        // Art/Marks/<ElarimMarkFile> (white on transparent) - OPEN, Kyle hasn't designed it yet.
        public const float ShapeRadius = 0.075f;                                             // circumradius: ~15 cm across
        public const string ElarimMarkFile = "elarim_mark_placeholder.png";
        public static readonly Vector3 ShapeAt = new Vector3(21.05f, Lower + 1.2f, -34.8f);
        public static readonly Vector3 Pedestal = new Vector3(21.05f, Lower, -34.8f);
        public static readonly Vector3 ShapeSpot = new Vector3(19.95f, Lower, -34.8f);      // where 7 stands to touch it
        public static readonly Vector2 AlcoveZ = new Vector2(-35.6f, -34.0f);
        public const float AlcoveBackX = 21.9f;
        // Round 9 (8b addition): the round three-spoke emblem beside the door - the shape's outline glows in it
        public static readonly Vector3 DoorSymbol = new Vector3(19.35f, Lower + 1.55f, -37.97f);
        public static readonly Vector3 MechanismSpot = new Vector3(19.35f, Lower, -37.42f);   // where 7 stands to touch it

        // ---- Impound (scene 7-8)
        public static readonly Vector2 LedgeMin = new Vector2(11f, -48.2f), LedgeMax = new Vector2(22.5f, -42f);
        public const float ViewpointZ = -43.4f;                                              // 6b: the reveal starts here
        public static readonly Vector3 Viewpoint = new Vector3(16.5f, Lower, -46.8f);
        public static readonly Vector3 RevealSpot = new Vector3(16.3f, Lower, -45.9f);       // she walks a few steps, turns back
        public const float CityBlendZ = -38.6f;                                              // fog blends to the city look past here

        // ---- The city inside the Moon (6b): a hollow sphere; the basin bottom is far below the terrace.
        public static readonly Vector3 ShellCenter = new Vector3(16f, 2800f, -900f);
        public const float ShellRadius = 3000f;
        public static Vector3 BasinCenter => ShellCenter + Vector3.down * ShellRadius;       // (16, -200, -900)
        /// <summary>Height of the inner shell surface at a horizontal point (valid within ~2900 m of the axis).</summary>
        public static float ShellY(float x, float z)
        {
            float d2 = (x - ShellCenter.x) * (x - ShellCenter.x) + (z - ShellCenter.z) * (z - ShellCenter.z);
            return ShellCenter.y - Mathf.Sqrt(Mathf.Max(0f, ShellRadius * ShellRadius - d2));
        }
        public static readonly Vector3 FallingStoneAt = new Vector3(-150f, 23f, -347f);     // the ancient terrace edge that breaks
        public const float HostFrontZ = -41.6f;                                              // the colossal host structure's face
        public static readonly Vector2 RampX = new Vector2(20.6f, 22.4f);
        public const float RampTopZ = -49.5f, RampBottomZ = -70f;
        public static readonly Vector2 BayMin = new Vector2(-2f, -94f), BayMax = new Vector2(32f, -69.5f);
        public static readonly Vector3 SaucerCentre = new Vector3(11.5f, Bay, -80f);
        public const float SaucerRadius = 6f, SaucerBaseY = 1.15f;     // hull underside above the floor while clamped
        public static readonly Vector3 Lever = new Vector3(25.2f, Bay, -74.2f);
        public static readonly Vector3 LeverSpot = new Vector3(22.6f, Bay, -73.4f);
        public static readonly Vector3 RampFoot = new Vector3(SaucerCentre.x + SaucerRadius + 2.3f, Bay, SaucerCentre.z);   // (pre-Round 7 ramp)
        // Round 7: tractor beam instead of a ramp. The saucer lifts HoverLift and shines a cone down from its belly hatch.
        public const float HoverLift = 2.2f, BeamFloorRadius = 1.45f, HatchRadius = 0.9f;
        public static readonly Vector3 BeamCentre = new Vector3(SaucerCentre.x, Bay, SaucerCentre.z);
        public static readonly Vector3 BeamEdge = new Vector3(SaucerCentre.x + 2.6f, Bay, SaucerCentre.z + 0.4f);   // scene 8 plays here
        public static readonly Vector3 BeamApproach = new Vector3(SaucerCentre.x + 7.6f, Bay, SaucerCentre.z + 0.6f);
        public static readonly Vector3[] BayPath = { new Vector3(21.5f, Bay, -72f), new Vector3(22.6f, Bay, -73.4f) };
    }

    /// <summary>Carries Chapter 1 progress between its two scenes (Moon level, saucer) and across checkpoint restarts.</summary>
    public static class Chapter1State
    {
        public static int Checkpoint;          // 0 cell, 1 corridor, 2 ancient hall, 3 vision, 4 impound
        public static bool Aboard;             // saucer scene runs scenes 9-11
        public static bool Explore;            // Continue: skip scenes 9-11, straight into free exploration
        public static float StartedAt = -1f;
        public static readonly System.Collections.Generic.List<string> Played = new System.Collections.Generic.List<string>();
        public static readonly System.Text.StringBuilder CaptureLog = new System.Text.StringBuilder();
        public static int ShotCount;
        /// <summary>QF4: watchdog fallbacks taken (story triggers that did not happen on their own in time).</summary>
        public static int Watchdogs;
        /// <summary>QF4 opening test (-openingtest n): run index and per-run results across scene reloads.</summary>
        public static int OpeningRun;
        public static readonly System.Collections.Generic.List<string> OpeningResults = new System.Collections.Generic.List<string>();

        /// <summary>Round 9: teaching prompts already shown this playthrough (each appears once; kept across checkpoint restarts).</summary>
        public static readonly System.Collections.Generic.HashSet<string> Taught = new System.Collections.Generic.HashSet<string>();

        public static void Reset() { Checkpoint = 0; Aboard = false; Explore = false; StartedAt = -1f; Played.Clear(); Taught.Clear(); }

        public static void StartAboard()
        {
            Aboard = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer");
        }
    }
}
