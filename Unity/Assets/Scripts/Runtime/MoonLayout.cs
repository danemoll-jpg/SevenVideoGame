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
        public static readonly Vector3 CrateFoot = new Vector3(1.35f, 0f, 3.15f);
        public static readonly Vector3 CrateTop = new Vector3(1.35f, 0.56f, 2.45f);
        public static readonly Vector3 CrateSize = new Vector3(0.75f, 0.56f, 0.7f);
        public static readonly Vector3 ReleasePanel = new Vector3(1.35f, 1.3f, 1.97f);
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

        // ---- Impound (scene 7-8)
        public static readonly Vector2 LedgeMin = new Vector2(11f, -48.2f), LedgeMax = new Vector2(22.5f, -42f);
        public const float ViewpointZ = -44.6f;
        public static readonly Vector3 Viewpoint = new Vector3(16.5f, Lower, -46.8f);
        public static readonly Vector2 RampX = new Vector2(20.6f, 22.4f);
        public const float RampTopZ = -49.5f, RampBottomZ = -70f;
        public static readonly Vector2 BayMin = new Vector2(-2f, -94f), BayMax = new Vector2(32f, -69.5f);
        public static readonly Vector3 SaucerCentre = new Vector3(11.5f, Bay, -80f);
        public const float SaucerRadius = 6f, SaucerBaseY = 1.15f;     // hull underside above the floor while clamped
        public static readonly Vector3 Lever = new Vector3(25.2f, Bay, -74.2f);
        public static readonly Vector3 LeverSpot = new Vector3(22.6f, Bay, -73.4f);
        public static readonly Vector3 RampFoot = new Vector3(SaucerCentre.x + SaucerRadius + 2.3f, Bay, SaucerCentre.z);
        public static readonly Vector3[] BayPath = { new Vector3(21.5f, Bay, -72f), new Vector3(22.6f, Bay, -73.4f) };
    }

    /// <summary>Carries Chapter 1 progress between its two scenes (Moon level, saucer) and across checkpoint restarts.</summary>
    public static class Chapter1State
    {
        public static int Checkpoint;          // 0 cell, 1 corridor, 2 ancient hall, 3 vision, 4 impound
        public static bool Aboard;             // saucer scene runs scenes 9-11
        public static float StartedAt = -1f;
        public static readonly System.Collections.Generic.List<string> Played = new System.Collections.Generic.List<string>();
        public static readonly System.Text.StringBuilder CaptureLog = new System.Text.StringBuilder();
        public static int ShotCount;

        public static void Reset() { Checkpoint = 0; Aboard = false; StartedAt = -1f; Played.Clear(); }

        public static void StartAboard()
        {
            Aboard = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Saucer");
        }
    }
}
