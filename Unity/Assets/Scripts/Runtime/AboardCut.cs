using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF23 (10, 15) - Panther's cutaways aboard, in every chapter scene that cuts to him: the REAL saucer interior (the Saucer
    /// scene's ship, saved as Assets/Saucer/SaucerInterior.prefab at build and placed far from the level), lit like the ship,
    /// never an empty white space. He sits on the console desk in front of the navigation display; the display shows what the
    /// saucer's instruments see (QF23 15): a top-down view of the area round 7 with her marker (and Ratwing's, once Nela has
    /// linked the ship to the inner sensors) - rendered live by an orthographic camera over the level.
    /// His cat side (the moods): Watch (glued to the screen: low crouch, ears forward), Hunt (butt wiggle, tail lashing,
    /// chattering, pawing at the moving marker), Bat (batting a dangling cable), Composed (sits up straight, tail curled - when
    /// he speaks), Twitch (the tail tip twitches, then stops), Paws (he looks down at his front paws and lifts one), Purr.
    /// The level's fog is thinned for the cut (the interior is far away inside it) and restored after.
    /// </summary>
    public class AboardCut : MonoBehaviour
    {
        public static AboardCut I { get; private set; }
        public Transform Interior, Cat, Display, PilotSeat, Cable;
        /// <summary>QF24 (8): his two measured places - the pilot's chair (larger form) and the console top (batting the screen).</summary>
        public Transform ChairSpot, DeskSpot; public float ChairScale = 2.1f; public Vector2 ChairHalf, DeskHalf;
        int _placeLogs;
        public Animator CatAnim; public CatPoseOverlay Pose;
        public Camera MapCam; public Renderer Screen; public Transform HerMark, RatMark;
        public enum Mood { Calm, Watch, Hunt, Bat, Composed, Twitch, Paws, Purr }
        [System.NonSerialized] public Mood Now = Mood.Calm;
        [System.NonSerialized] public Transform Follow, Rat;
        public bool Showing { get; private set; }
        public int Cuts { get; private set; }
        Vector3 _catHome; Quaternion _catRot; RenderTexture _rt; Atmosphere _atmo; float _d, _m, _g; float _moodT; AudioSource _purr;
        public const int MapLayer = 23;

        void Awake()
        {
            I = this;
            if (Cat != null) { _catHome = Cat.position; _catRot = Cat.rotation; }
            if (MapCam != null)
            {
                _rt = new RenderTexture(512, 512, 16) { name = "AboardMapRT" };
                MapCam.targetTexture = _rt; MapCam.enabled = false;
                if (Screen != null) { var mpb = new MaterialPropertyBlock(); Screen.GetPropertyBlock(mpb); mpb.SetTexture("_BaseMap", _rt); mpb.SetTexture("_MainTex", _rt); Screen.SetPropertyBlock(mpb); }
            }
            foreach (var c in Camera.allCameras) if (c != MapCam) c.cullingMask &= ~(1 << MapLayer);
            SetVisible(false);
        }
        void OnDestroy() { if (I == this) I = null; if (_rt != null) _rt.Release(); }
        /// <summary>QF23 (15a): the first time Panther "sees" something in a chapter level (his first line there), a quick cut
        /// to the console display with her marker and the area round her - so the player learns where his eyes are. A director
        /// with its own cutaways (the mothership) turns this off.</summary>
        public bool AutoFirstLook = true;
        bool _firstDone;
        void Start()
        {
            foreach (var d in FindObjectsByType<DialoguePlayer>(FindObjectsSortMode.None)) d.LineStarted += l => { if (AutoFirstLook && !_firstDone && l != null && l.Speaker == "cat") { _firstDone = true; StartCoroutine(FirstLook(l.Id)); } };
        }
        IEnumerator FirstLook(string lineId)
        {
            var cam = FindAnyObjectByType<ChapterCamera>(); if (cam == null) yield break;
            var c = cam.GetComponent<Camera>(); bool wasShot = cam.InShot; var p0 = cam.transform.position; var f0 = cam.transform.forward; float fov0 = c != null ? c.fieldOfView : 50f;
            var seven = FindAnyObjectByType<ChapterSeven>(); Follow = seven != null ? seven.transform : null;
            Begin(Mood.Watch);
            var (p, l) = Frame("screen"); cam.Shot(p, l, 0.01f, true, 40f);
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) yield return null;
            if (DemoBoot.CaptureDir != null && ChapterCapture.I != null) { ChapterCapture.I.Log($"FIRST LOOK (QF23 15a) {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}: on his first line here ({lineId}) a cut to the saucer's console - the display shows her marker and the area round her"); yield return ChapterCapture.I.Shot("qf23_first_look_the_console_display_" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name); }
            for (float t = 0.4f; t < 1.9f; t += Time.deltaTime) yield return null;
            End();
            if (wasShot) cam.Shot(p0, p0 + f0 * 10f, 0.01f, true, fov0); else cam.Follow();
        }
        void SetVisible(bool on) { if (Interior != null) Interior.gameObject.SetActive(on); if (Cat != null) Cat.gameObject.SetActive(on); }

        /// <summary>The camera's framing for a cut: "screen" = over his shoulder onto the display (his marker view),
        /// "him" = his face and front paws, past the display edge; "wide" = him at the console, the chair, the window.
        /// QF24 (8): scaled with the form he is in (in the pilot's chair he is about twice his size).</summary>
        public (Vector3 pos, Vector3 look) Frame(string kind)
        {
            PlaceFor(Now);
            var c = Cat != null ? Cat.position : transform.position; var d = Display != null ? Display.position : c + Vector3.forward;
            var toD = d - c; toD.y = 0f; toD.Normalize(); var side = Vector3.Cross(Vector3.up, toD);
            float k = Cat != null ? Mathf.Max(1f, Cat.localScale.x) : 1f;
            switch (kind)
            {
                case "screen": return k > 1.5f ? (c - toD * 0.12f + side * 0.78f + Vector3.up * 1.0f, d - Vector3.up * 0.05f)   // beside and above the chair (its shell is behind him)
                                               : (c - toD * 0.55f + side * 0.35f + Vector3.up * 0.42f, d - Vector3.up * 0.05f);
                case "him": return k > 1.5f ? (c + side * 1.3f + toD * 0.5f + Vector3.up * 1.0f, c + Vector3.up * 0.32f * k)   // in the pilot's chair: from beside it (the console is in front, the shell behind)
                                            : (c + toD * 0.65f + side * 0.55f + Vector3.up * 0.2f, c + Vector3.up * 0.18f);
                default: return (c - toD * 2.3f + side * 1.6f + Vector3.up * 0.9f * k, (c + d) * 0.5f);
            }
        }
        /// <summary>QF24 (8): where he is for a mood - batting at the screen = fully on the console top in front of the display
        /// (his normal size); everything else = sitting in the pilot's chair in his larger form, at eye level with the display.</summary>
        void PlaceFor(Mood m)
        {
            if (Cat == null || ChairSpot == null || DeskSpot == null) return;
            bool desk = m == Mood.Bat || m == Mood.Hunt;
            var spot = desk ? DeskSpot : ChairSpot;
            _catHome = spot.position; _catRot = spot.rotation;
            Cat.SetPositionAndRotation(_catHome, _catRot); Cat.localScale = Vector3.one * (desk ? 1f : m == Mood.Purr ? ChairScale * 0.72f : ChairScale);   // curled asleep he takes the cushion, not more
        }
        /// <summary>QF24 (8): his paws vs the surface he is on (inside its footprint, on its top) - logged for each cut.</summary>
        void LogPlacement()
        {
            if (DemoBoot.CaptureDir == null || Pose == null || ChairSpot == null || _placeLogs > 24) return;
            _placeLogs++;
            bool desk = Now == Mood.Bat || Now == Mood.Hunt; var spot = desk ? DeskSpot : ChairSpot; var half = desk ? DeskHalf : ChairHalf;
            float k = Cat.localScale.x;
            string Paw(Vector3 p)
            {
                var l = spot.InverseTransformPoint(p); l.Scale(spot.lossyScale);
                bool inside = Mathf.Abs(l.z) <= half.x + 0.02f && Mathf.Abs(l.x) <= half.y + 0.02f;
                return $"{(p.y - 0.032f * k - spot.position.y) * 100f:+0.0;-0.0} cm{(inside ? "" : " OFF THE EDGE")}";   // the paw bone sits ~3.2 cm (x his size) above the sole
            }
            var body = Cat.GetComponentInChildren<SkinnedMeshRenderer>(); float under = body != null ? (spot.position.y - body.bounds.min.y) * 100f : 0f;
            ChapterCapture.I?.Log($"PANTHER PLACE (QF24 8): mood {Now} - {(desk ? "on the console top" : $"in the pilot's chair (larger form x{Cat.localScale.x:0.0})")}; front paw soles vs the surface {Paw(Pose.PawL)} / {Paw(Pose.PawR)}; lowest body point {under:0.0} cm below the top ({(desk ? "the desk is hard: must be ~0" : "the cushion is domed and soft: up to ~3 cm at its rim")}); no toy / cable in the set");
        }

        /// <summary>Show the interior (the director then frames with <see cref="Frame"/>).</summary>
        public void Begin(Mood m)
        {
            SetVisible(true); Showing = true; Cuts++; Now = m; _moodT = 0f; PlaceFor(m); _logPending = true;
            if (Cable != null) Cable.gameObject.SetActive(false);                // QF24 (8a): no toy
            _atmo = FindAnyObjectByType<Atmosphere>();
            if (_atmo != null) { _d = _atmo.DistanceDensity; _m = _atmo.MaxFog; _g = _atmo.GroundDensity; _atmo.DistanceDensity = _d * 0.05f; _atmo.MaxFog = _m * 0.2f; _atmo.GroundDensity = 0f; }
            if (DemoBoot.CaptureDir != null) ChapterCapture.I?.Log($"cut aboard (QF23 10): Panther in the REAL saucer interior ({(Interior != null ? Interior.name : "no interior!")}), mood {m}, the display shows {(Follow != null ? "the area round 7 with her marker" : "the route")}{(Rat != null ? " + Ratwing's marker" : "")}");
        }
        public void SetMood(Mood m) { Now = m; _moodT = 0f; PlaceFor(m); _logPending = true; }
        bool _logPending;
        public void End()
        {
            Showing = false; SetVisible(false);
            if (_atmo != null) { _atmo.DistanceDensity = _d; _atmo.MaxFog = _m; _atmo.GroundDensity = _g; }
            if (_purr != null) { StartCoroutine(AudioKit.Fade(_purr, 0f, 0.4f, true)); _purr = null; }
        }

        float _chatterCd, _tapCd;
        void LateUpdate()
        {
            if (!Showing) return;
            float dt = Time.deltaTime; _moodT += dt; float t = Time.time;
            // the map: top-down round her, her marker (and Ratwing's)
            if (MapCam != null && Follow != null)
            {
                MapCam.transform.SetPositionAndRotation(Follow.position + Vector3.up * 80f, Quaternion.Euler(90f, 0f, 0f));
                if (HerMark != null) { HerMark.position = Follow.position + Vector3.up * 40f; HerMark.localScale = Vector3.one * (1.6f + 0.3f * Mathf.Sin(t * 4f)); }
                if (RatMark != null) { RatMark.gameObject.SetActive(Rat != null && Rat.gameObject.activeInHierarchy); if (Rat != null) RatMark.position = Rat.position + Vector3.up * 41f; }
                MapCam.Render();
            }
            if (Cat == null || Pose == null) return;
            // the moods (procedural, after his idle / sit clip)
            Cat.position = _catHome; Cat.rotation = _catRot;
            Pose.Crouch = 0f; Pose.PawTo = null; Pose.PawW = 0f; Pose.TailFlick = 0f; Pose.TailTwitch = 0f; Pose.TailToward = null; Pose.EarToward = null;
            var scr = Display != null ? Display.position : Cat.position + Cat.forward;
            var markOnScreen = scr + (Rat != null && RatMark != null && RatMark.gameObject.activeSelf ? Vector3.right * 0.08f * Mathf.Sin(t * 1.7f) : Vector3.zero);
            switch (Now)
            {
                case Mood.Watch:
                    Pose.Crouch = Cat.localScale.x > 1.5f ? 0f : 0.6f; Pose.LookAt = Cat.localScale.x > 1.5f ? new Vector3(markOnScreen.x, Cat.position.y + 0.75f, markOnScreen.z) : markOnScreen; Pose.EarToward = scr; Play("sit"); break;   // QF24 (8): in the chair he watches sitting upright (the crouch lifted his front paws off the cushion)
                case Mood.Hunt:
                {
                    Play("sit"); Pose.Crouch = 0.9f; Pose.LookAt = markOnScreen; Pose.EarToward = scr; Pose.TailFlick = 1f;
                    // the butt wiggle before the pounce: the rear sways about his front paws
                    float w = Mathf.Sin(t * 15f) * 5f * Mathf.Clamp01(_moodT / 0.4f);
                    var pivot = _catHome + Cat.forward * 0.18f;
                    Cat.RotateAround(pivot, Vector3.up, w);
                    // and the paw at the moving marker every couple of seconds
                    float ph = (_moodT % 2.2f) / 2.2f;
                    if (ph > 0.55f && ph < 0.8f) { Pose.PawTo = markOnScreen; Pose.PawW = Mathf.Sin((ph - 0.55f) / 0.25f * Mathf.PI); if (_tapCd <= 0f) { _tapCd = 1.5f; AudioKit.Play("cat_paw_tap", scr, 0.6f, 0.08f, AudioKit.Bus.Sfx, 6f); } }
                    _chatterCd -= dt; if (_chatterCd <= 0f) { _chatterCd = 2.6f; AudioKit.Play("cat_chatter", Cat.position + Vector3.up * 0.2f, 0.8f, 0.06f, AudioKit.Bus.Voice, 8f); }
                    break;
                }
                case Mood.Bat:
                {
                    // QF24 (8a): he bats at the SCREEN - a quick swipe or two at Ratwing's / her moving marker, tap-tap on the
                    // glass, then the paw comes back and he sits up very properly, as if nothing happened (no props)
                    Play("sit"); Pose.LookAt = markOnScreen; Pose.EarToward = scr;
                    float ph = _moodT % 3.2f;
                    if (ph < 1.3f)
                    {
                        Pose.Crouch = 0.5f; Pose.TailFlick = 0.8f;
                        float sw = ph < 0.55f ? Mathf.Sin(ph / 0.55f * Mathf.PI) : ph > 0.7f && ph < 1.2f ? Mathf.Sin((ph - 0.7f) / 0.5f * Mathf.PI) : 0f;
                        Pose.PawTo = markOnScreen + (Display != null ? -Display.forward * 0.01f : Vector3.zero); Pose.PawW = sw;
                        if (sw > 0.95f && _tapCd <= 0f) { _tapCd = 0.35f; AudioKit.Play("cat_paw_tap", scr, 0.6f, 0.08f, AudioKit.Bus.Sfx, 6f); }
                    }
                    else { Pose.Crouch = 0f; Pose.LookAt = scr + Vector3.up * 0.25f; Pose.TailToward = Cat.position + Cat.right * 0.2f; }   // sits up, very proper
                    break;
                }
                case Mood.Composed:
                    Play("sit"); Pose.LookAt = scr + Vector3.up * 0.2f; Pose.TailToward = Cat.position + Cat.right * 0.2f; break;      // upright, tail curled neatly
                case Mood.Twitch:
                    Play("sit"); Pose.LookAt = markOnScreen; Pose.TailTwitch = _moodT < 1.2f ? 1f : 0f; break;                          // the tip twitches... then stops
                case Mood.Paws:
                {
                    Play("sit"); var paw = Pose.PawL; Pose.LookAt = paw;                                                               // he looks down at his paws...
                    if (_moodT > 0.9f && _moodT < 2.2f) { Pose.PawTo = paw + Vector3.up * 0.08f + Cat.forward * 0.03f; Pose.PawW = Mathf.Sin(Mathf.Clamp01((_moodT - 0.9f) / 1.3f) * Mathf.PI); }   // ...and lifts one
                    break;
                }
                case Mood.Purr:
                    Play("sleep"); Pose.LookAt = null;
                    if (_purr == null) _purr = Purr.Loop(Cat, 1f);
                    break;
                default: Play("sit"); Pose.LookAt = scr; break;
            }
            _tapCd -= dt;
            if (_logPending && _moodT > 0.5f) { _logPending = false; LogPlacement(); }
        }
        string _clip;
        void Play(string s) { if (CatAnim == null || _clip == s) return; if (!CatAnim.HasState(0, Animator.StringToHash(s))) s = "idle"; _clip = s; CatAnim.CrossFadeInFixedTime(s, 0.25f); }

        /// <summary>A whole cut: show the interior, frame, hold, capture, end.</summary>
        public IEnumerator Cut(ChapterCamera cam, Mood mood, string frame, float hold, System.Func<IEnumerator> capture = null)
        {
            Begin(mood);
            var (p, l) = Frame(frame);
            cam.Subjects.Clear(); cam.Shot(p, l, 0.01f, true, 40f);
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) yield return null;
            if (capture != null) yield return capture();
            for (float t = 0.4f; t < hold; t += Time.deltaTime) yield return null;
        }
    }

    /// <summary>
    /// QF23 (11) - Panther's purr, LOUD and clearly heard: three close-miked purr takes (cat_purr_a / b / c, picked so it never
    /// sounds like one loop), on the VOICE bus (music never buries it; MusicSystem ducks under voice), at 1.0; it swells when
    /// she strokes him (<see cref="Swell"/>) and fades when she stops. Every line that mentions his purr starts it first.
    /// </summary>
    public static class Purr
    {
        static int _n;
        public static string Pick() { _n++; var ids = new[] { "cat_purr_a", "cat_purr_b", "cat_purr_c" }; return ids[_n % 3]; }
        public static AudioSource Loop(Transform on, float vol = 1f)
        {
            var id = Pick();
            var src = AudioKit.Loop(id, on, null, vol, AudioKit.Bus.Voice, 1f, 6f) ?? AudioKit.Loop("cat_purr", on, null, vol, AudioKit.Bus.Voice, 1f, 6f);
            if (src != null) { src.spatialBlend = 0.35f; src.priority = 10; Started++; Last = id; _live.Add(src); }
            if (DemoBoot.CaptureDir != null) ChapterCapture.I?.Log($"PURR (QF23 11): {(src != null ? id : "MISSING")} on the voice bus at {vol:0.00} (bus {AudioKit.Bus.Voice})");
            return src;
        }
        public static int Started; public static string Last;
        static readonly System.Collections.Generic.List<AudioSource> _live = new System.Collections.Generic.List<AudioSource>();
        /// <summary>A purr is audible now (the music ducks under it, as under a line).</summary>
        public static bool Playing { get { _live.RemoveAll(x => x == null); foreach (var x in _live) if (x.isPlaying && x.volume > 0.1f) return true; return false; } }
        static readonly float[] _buf = new float[1024];
        /// <summary>QF23 (11) proof: the purr's level now (RMS dBFS of its own output x its volume) next to the music's gain.</summary>
        public static string Level()
        {
            _live.RemoveAll(x => x == null); if (_live.Count == 0) return "no purr playing";
            var s = _live[_live.Count - 1]; float rms = 0f;
            if (s.clip != null) { try { int pos = Mathf.Clamp(s.timeSamples, 0, Mathf.Max(0, s.clip.samples - 1024)); if (s.clip.GetData(_buf, pos)) { foreach (var x in _buf) rms += x * x; rms = Mathf.Sqrt(rms / _buf.Length); } } catch { } }
            float db = 20f * Mathf.Log10(Mathf.Max(1e-5f, rms * s.volume * AudioKit.Gain(AudioKit.Bus.Voice)));
            return $"purr {s.clip?.name} {db:0.0} dBFS (volume {s.volume:0.00}, voice bus {AudioKit.Gain(AudioKit.Bus.Voice):0.00}) vs music gain {AudioKit.Gain(AudioKit.Bus.Music) * MusicSystem.DuckLevel:0.00} while it purrs (ducked)";
        }
        /// <summary>Per frame: the purr's volume follows the stroke (0.55 resting - 1.0 while stroked).</summary>
        public static void Swell(AudioSource src, float stroke) { if (src != null) src.volume = Mathf.MoveTowards(src.volume, Mathf.Lerp(0.7f, 1f, Mathf.Clamp01(stroke)), Time.deltaTime * 0.8f); }
    }
}
