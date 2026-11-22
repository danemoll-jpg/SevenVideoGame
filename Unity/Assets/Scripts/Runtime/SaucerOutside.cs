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
            if (world == "moon" || world == "venus" || world == "europa" || world == "mars") { try { PlayerPrefs.SetString(SysKey, "sol"); } catch { } }
            try { PlayerPrefs.SetString(Key, world); PlayerPrefs.Save(); } catch { }
            Debug.Log("[Seven] the saucer is at: " + world);
            Changed?.Invoke();
        }
        public static bool Travelling { get => _travelling; set { if (_travelling == value) return; _travelling = value; Changed?.Invoke(); } }
        /// <summary>QF18 (22): which star system the saucer is in ("sol" / "biaveh") - the star map opens on it, the windows
        /// show its open space. Biaveh's system after the long jump (TRV_AST happens there).</summary>
        static string SysKey => DemoBoot.CaptureDir != null ? "seven_test_ship_sys" : "seven_ship_sys";
        public static string System { get { try { return PlayerPrefs.GetString(SysKey, "sol"); } catch { return "sol"; } } set { try { PlayerPrefs.SetString(SysKey, value); PlayerPrefs.Save(); } catch { } Debug.Log("[Seven] the saucer is in the star system: " + value); Changed?.Invoke(); } }
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
        /// <summary>Round 16: the asteroid outside (AST_01 / AST_10): dark rock with silver structures through it, rings turning
        /// round a recessed opening, white route lights; one section intact, one stripped to its frame. ArchiveLight switches on
        /// in AST_10 ("an archive light switches on").</summary>
        public GameObject Asteroid; public Light ArchiveLight; public Renderer ArchiveLamp;
        public void ArchiveLightOn() { if (ArchiveLight != null) ArchiveLight.enabled = true; if (ArchiveLamp != null) Glowing.Set(ArchiveLamp, 2.4f); Debug.Log("[Seven] the asteroid outside: an archive light switches on"); }
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
            { "mars",   new Haze { valleyD = 0.0005f, noise = 0.3f, fogStart = 60f, groundD = 0.0025f, groundBase = -230f, groundFall = 20f, groundRange = 500f, distD = 0.00045f, hazeH = 150f, maxFog = 0.45f, near = 0.35f, far = 0.65f, brightDist = 600f, zenith = 0.6f, glow = 0.35f } },
            { "asteroid", new Haze { valleyD = 0.0f, noise = 0.1f, fogStart = 140f, groundD = 0f, groundBase = -500f, groundFall = 10f, groundRange = 10f, distD = 0.0004f, hazeH = 60f, maxFog = 0.25f, near = 0.03f, far = 0.12f, brightDist = 500f, zenith = 0.03f, glow = 0.4f } },
            { "biaveh", new Haze { valleyD = 0.0f, noise = 0.1f, fogStart = 140f, groundD = 0f, groundBase = -500f, groundFall = 10f, groundRange = 10f, distD = 0.0004f, hazeH = 60f, maxFog = 0.25f, near = 0.03f, far = 0.12f, brightDist = 500f, zenith = 0.03f, glow = 0.4f } },
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
            if (Space != null) Space.SetActive(w == "space" || w == "asteroid" || w == "biaveh");   // open space (stars) while travelling, and round the asteroid
            if (Asteroid != null) Asteroid.SetActive(w == "asteroid");
            if (DiscMoon != null) DiscMoon.SetActive(w == "moon");
            if (DiscVenus != null) DiscVenus.SetActive(w == "venus");
            if (DiscEuropa != null) DiscEuropa.SetActive(w == "europa");
            if (DiscMars != null) DiscMars.SetActive(w == "mars");
            ShowLevel(w);                                                   // QF18 (3): the real level under the ship
            ShowBiavehSpace(w == "biaveh" || w == "space" && SaucerLocation.System == StarMap.Bia);   // Round 17: at the mothership too   // QF18 (22) (round the asteroid: the asteroid fills the window)
            if (Atmo != null)
            {
                if (!_savedSet) { _saved = Read(Atmo); _savedSet = true; }
                var h = City != null && City.activeSelf ? _saved : Looks.TryGetValue(w, out var hh) ? hh : _saved;
                Write(Atmo, h);
            }
        }
        // ---- QF18 (3): the window looks out over the ACTUAL level (Resources/Outside/Outside_<world>, the level's own meshes and
        // materials copied at build time) - QF16's procedural ground showed Mars as one ravine outline in fog. Per world: the
        // point of the level under the ship (S), what the window faces (aim) and how high the ship hovers (H).
        static readonly Dictionary<string, (Vector3 S, Vector3 aim, float H)> LevelView = new Dictionary<string, (Vector3, Vector3, float)>
        {
            { "mars", (new Vector3(-170f, 0f, -330f), new Vector3(-70f, 0f, 260f), 230f) },     // over the plain south of the settlement: the ravine, the crossing, the temple, both cities, the Face
            { "venus", (new Vector3(0f, 0f, -70f), new Vector3(0f, 0f, 200f), 150f) },
            { "europa", (new Vector3(0f, 0f, -90f), new Vector3(-30f, 0f, 160f), 140f) },
            { "cattalus", (new Vector3(-10f, 0f, -150f), new Vector3(0f, 0f, 160f), 95f) },   // Round 18: over the berth's approach: the cardboard quarter, the cat trees, the transit tower, the square, the course, the meadow beyond
        };
        GameObject _levelGo; string _levelW;
        /// <summary>Round 18: where a point of the real level appears in the window view (cabin shots frame it: "That's the old
        /// transit tower"); null while no level is shown.</summary>
        public Vector3? LevelPoint(Vector3 levelPos) => _levelGo != null && _levelGo.activeInHierarchy ? _levelGo.transform.TransformPoint(levelPos) : (Vector3?)null;
        public bool LevelShown => _levelGo != null && _levelGo.activeSelf;
        void ShowLevel(string w)
        {
            if (_levelGo != null && _levelW != w) { Destroy(_levelGo); _levelGo = null; }
            bool has = LevelView.ContainsKey(w);
            if (has && _levelGo == null)
            {
                var pf = Resources.Load<GameObject>("Outside/Outside_" + w);
                if (pf != null)
                {
                    _levelGo = Instantiate(pf); _levelW = w;
                    var v = LevelView[w]; var d = v.aim - v.S; d.y = 0f;
                    var R = Quaternion.Euler(0f, -Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 0f);
                    _levelGo.transform.SetPositionAndRotation(new Vector3(0f, -v.H, 0f) - R * v.S, R);
                    Debug.Log($"[Seven] window view (QF18 3): the real {w} level under the ship ({_levelGo.transform.childCount} pieces), {v.H:0} m up over {v.S}, facing {v.aim}");
                }
            }
            if (_levelGo != null) _levelGo.SetActive(has);
            // the world group keeps its sky (Earth, Jupiter, the cloud ceiling); its procedural ground gives way to the level
            var grp = w == "mars" ? Mars : w == "venus" ? Venus : w == "europa" ? Europa : null;
            if (grp != null && _levelGo != null)
                foreach (Transform c in grp.transform)
                {
                    string n = c.name;
                    bool ground = n.Contains("Plain") || n.Contains("Hills") || n.EndsWith("Ice") || n.Contains("Ground") || n.Contains("City") || n.Contains("Wall") || n.Contains("Tree") || n.Contains("Ravine") || n.Contains("Ridge") || n.Contains("Crack");
                    if (ground) c.gameObject.SetActive(false);
                }
        }
        // ---- QF18 (22): open space in Biaveh's system - its star large and bright, Biaveh itself a dark disc
        GameObject _bia; VeyraScene _veyraView;
        /// <summary>QF25 (1): the window view's mothership (the cabin shots frame it).</summary>
        public Transform VeyraEgg => _veyraView != null ? _veyraView.Egg : null;
        void LateUpdate() { if (_bia != null && _bia.activeSelf && _veyraView != null) _veyraView.Tick(); }
        void ShowBiavehSpace(bool on)
        {
            if (on && _bia == null)
            {
                _bia = new GameObject("Outside_SpaceBiaveh");
                var star = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(star.GetComponent<Collider>()); star.transform.SetParent(_bia.transform, false);
                star.transform.position = new Vector3(700f, 260f, 2300f); star.transform.localScale = Vector3.one * 140f; star.GetComponent<Renderer>().sharedMaterial = Markers.Additive(3f);
                // QF23 (1): no halo quad (an untextured additive quad drew as a square)
                // QF23 (1): the SAME composition as BIA_01 (VeyraScene, scaled to the Saucer scene's 2.5 km camera): the saucer
                // among Veyra's rings, the white mothership in the ring plane, ice drifting in front of it and behind it
                _veyraView = VeyraScene.Build(_bia.transform, new Vector3(0f, 1.5f, 0f), 0.24f, 23, true, new Vector3(0f, -0.06f, 1f), 2500f);   // QF25 (1): the egg straight out of the front window (WindowA = +z), the planet inside the 2.5 km camera
                Debug.Log("[Seven] window view (QF22): open space at Veyra - its star, Veyra and its rings");
            }
            if (_bia != null) _bia.SetActive(on);
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
