using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF16 (1): where the saucer is - ONE saved state ("seven_ship_at"; check runs "seven_test_ship_at") read by every view
    /// outside the ship: "moon", "venus", "europa", "mars", or "space" while travelling (not saved). Set when she arrives
    /// (the Saucer scene's aboard modes set it from where they are; a journey sets it at the end) and used on Continue /
    /// Scene Select / the chair scenes.
    /// </summary>
    public static class SaucerLocation
    {
        static string Key => DemoBoot.CaptureDir != null ? "seven_test_ship_at" : "seven_ship_at";
        static bool _travelling;
        public static event System.Action Changed;
        public static string Saved { get { try { return PlayerPrefs.GetString(Key, "moon"); } catch { return "moon"; } } }
        /// <summary>What the windows show now.</summary>
        public static string Current => _travelling ? "space" : Saved;
        public static void Set(string world)
        {
            if (string.IsNullOrEmpty(world) || world == "space") return;
            try { PlayerPrefs.SetString(Key, world); PlayerPrefs.Save(); } catch { }
            Debug.Log("[Seven] the saucer is at: " + world);
            Changed?.Invoke();
        }
        public static bool Travelling { get => _travelling; set { if (_travelling == value) return; _travelling = value; Changed?.Invoke(); } }
    }

    /// <summary>
    /// QF16 (1): the view outside the saucer (the window band, the glass floor in front of it, the galley porthole) shows the
    /// world the saucer is at - cheap backdrops in Kyle's misty style, built by Editor/FightDemoBuilderOutside.cs: the Moon's
    /// grey surface with Earth in the black sky; Venus's garden hills under a bright cloud ceiling; Europa's ice field with
    /// Jupiter huge in the sky; Mars's plain, walls, the ravine and the two fortress cities; open space (stars) while
    /// travelling. One group per world is active; the Atmosphere's haze is set to that world's look; the porthole's disc
    /// shows that world from space. (The Round 3 hollow-Moon city stays only for the saucer demo and Chapter 1's scene 9.)
    /// </summary>
    public class SaucerOutside : MonoBehaviour
    {
        public GameObject Moon, Venus, Europa, Mars, Space, City;
        public GameObject DiscMoon, DiscVenus, DiscEuropa, DiscMars;     // the galley porthole's view
        public Atmosphere Atmo;
        public static SaucerOutside I { get; private set; }
        public string Showing { get; private set; }

        struct Haze { public float valleyD, noise; public float fogStart, groundD, groundBase, groundFall, groundRange, distD, hazeH, maxFog, near, far, brightDist, zenith, glow; }
        static readonly Dictionary<string, Haze> Looks = new Dictionary<string, Haze>
        {
            // ground mist hugs the backdrops' ground (y -200); the distance haze stays light enough that the ground reads
            { "moon",   new Haze { valleyD = 0.0f, noise = 0.1f, fogStart = 40f, groundD = 0.003f, groundBase = -200f, groundFall = 6f, groundRange = 300f, distD = 0.0003f, hazeH = 80f, maxFog = 0.4f, near = 0.12f, far = 0.35f, brightDist = 500f, zenith = 0.05f, glow = 0.25f } },
            { "venus",  new Haze { valleyD = 0.0012f, noise = 0.35f, fogStart = 30f, groundD = 0.006f, groundBase = -200f, groundFall = 25f, groundRange = 500f, distD = 0.0009f, hazeH = 150f, maxFog = 0.55f, near = 0.45f, far = 0.75f, brightDist = 600f, zenith = 0.85f, glow = 0.35f } },
            { "europa", new Haze { valleyD = 0.0007f, noise = 0.25f, fogStart = 40f, groundD = 0.004f, groundBase = -200f, groundFall = 12f, groundRange = 400f, distD = 0.0006f, hazeH = 120f, maxFog = 0.5f, near = 0.3f, far = 0.6f, brightDist = 700f, zenith = 0.2f, glow = 0.3f } },
            { "mars",   new Haze { valleyD = 0.001f, noise = 0.3f, fogStart = 30f, groundD = 0.005f, groundBase = -200f, groundFall = 20f, groundRange = 500f, distD = 0.0008f, hazeH = 150f, maxFog = 0.55f, near = 0.35f, far = 0.65f, brightDist = 600f, zenith = 0.6f, glow = 0.35f } },
            { "space",  new Haze { valleyD = 0.0f, noise = 0.0f, fogStart = 200f, groundD = 0f, groundBase = -500f, groundFall = 10f, groundRange = 10f, distD = 0.0002f, hazeH = 10f, maxFog = 0.15f, near = 0.02f, far = 0.04f, brightDist = 900f, zenith = 0.02f, glow = 0.2f } },
        };
        Haze _saved; bool _savedSet;

        void Awake() { I = this; }
        void OnEnable() { SaucerLocation.Changed += Apply; }
        void OnDisable() { SaucerLocation.Changed -= Apply; if (I == this) I = null; }
        IEnumerator Start()
        {
            yield return null;                    // the aboard modes set the location in their Start
            Apply();
            if (DemoBoot.CaptureDir != null && ChapterCapture.I != null && Chapter1Aboard.I != null && Chapter1Aboard.I.Active)
            {
                yield return new WaitForSeconds(1.5f);
                // QF16 (1) check: the view from inside, through the window band, at this world
                yield return ChapterCapture.I.ShotFrom($"qf16_01_window_view_{Showing}", new Vector3(0f, 1.6f, 4.2f), new Vector3(0f, -14f, 60f), 58f);
                ChapterCapture.I.Log($"window view (QF16 1): the saucer is at '{SaucerLocation.Saved}', showing '{Showing}'");
            }
        }

        /// <summary>Show the world (or space) now.</summary>
        public void Apply() => Show(SaucerLocation.Current);
        public void Show(string w)
        {
            Showing = w;
            bool demoCity = !Chapter1State.Aboard && !DemoBoot.Chapter1Aboard;
            bool ch1Scene9 = Chapter1Aboard.I != null && Chapter1Aboard.I.Active && !Chapter1Aboard.I.VenusMode && !Chapter1State.Explore && !Chapter1State.GalleyStart;   // still inside the hollow Moon
            if (City != null) City.SetActive(w == "moon" && (demoCity || ch1Scene9));
            if (Moon != null) Moon.SetActive(w == "moon" && City != null && !City.activeSelf);
            if (Venus != null) Venus.SetActive(w == "venus");
            if (Europa != null) Europa.SetActive(w == "europa");
            if (Mars != null) Mars.SetActive(w == "mars");
            if (Space != null) Space.SetActive(w == "space");             // open space (stars) only while travelling
            if (DiscMoon != null) DiscMoon.SetActive(w == "moon");
            if (DiscVenus != null) DiscVenus.SetActive(w == "venus");
            if (DiscEuropa != null) DiscEuropa.SetActive(w == "europa");
            if (DiscMars != null) DiscMars.SetActive(w == "mars");
            if (Atmo != null)
            {
                if (!_savedSet) { _saved = Read(Atmo); _savedSet = true; }
                var h = City != null && City.activeSelf ? _saved : Looks.TryGetValue(w, out var hh) ? hh : _saved;
                Write(Atmo, h);
            }
        }
        static Haze Read(Atmosphere a) => new Haze { valleyD = a.ValleyDensity, noise = a.NoiseStrength, fogStart = a.FogStart, groundD = a.GroundDensity, groundBase = a.GroundBase, groundFall = a.GroundFalloff, groundRange = a.GroundRange, distD = a.DistanceDensity, hazeH = a.HazeHeight, maxFog = a.MaxFog, near = a.NearBrightness, far = a.FarBrightness, brightDist = a.BrightnessDistance, zenith = a.ZenithBrightness, glow = a.GlowIntensity };
        static void Write(Atmosphere a, Haze h)
        {
            a.ValleyDensity = h.valleyD; a.NoiseStrength = h.noise; a.FogStart = h.fogStart; a.GroundDensity = h.groundD; a.GroundBase = h.groundBase; a.GroundFalloff = h.groundFall; a.GroundRange = h.groundRange;
            a.DistanceDensity = h.distD; a.HazeHeight = h.hazeH; a.MaxFog = h.maxFog; a.NearBrightness = h.near; a.FarBrightness = h.far;
            a.BrightnessDistance = h.brightDist; a.ZenithBrightness = h.zenith; a.GlowIntensity = h.glow;
        }
    }
}
