using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// Chapter 1 "The Moon Prison", scenes 1-8 (the Moon level): cell, first contact, the cat opens the door,
    /// walk-and-talk, the older wall, the Scree guard (telekinesis distraction; being seen only resets to the hall
    /// entrance), the first vision, the impound bay, releasing the saucer, boarding and liftoff. Then the saucer scene
    /// plays scenes 9-11 (<see cref="Chapter1Aboard"/>). Checkpoint at each scene (Backspace / d-pad down restarts from
    /// it). With -capture the same flow runs on autopilot and writes screenshots + a log (<see cref="ChapterCapture"/>).
    /// </summary>
    public class Chapter1Director : MonoBehaviour
    {
        public ChapterSeven Seven;
        public ChapterCat Cat;
        public ChapterCamera Cam;
        public ChapterInput Input;
        public DialoguePlayer Dlg;
        public ChapterUI UI;
        public ControlsLegend Legend;
        public Material Additive;

        [Header("Props")]
        [Tooltip("Round 7: laser door (beam, halo pairs) set flush in the cell opening.")]
        public Transform LaserDoor;
        public Renderer[] LaserBeams;
        public Light LaserLight;
        public Renderer ReleaseLight;
        public Transform OldWallSlab;
        public ScreeGuard Guard;
        public ScreeGuard[] GateGuards;
        [Tooltip("QF4: Scree workers (work loop) in the bay and on the terrace seen in the city reveal.")]
        public ScreeGuard[] Workers;
        public Transform Stone;
        public Renderer InscriptionGlow;
        public GameObject VisionWhole, VisionRuins;
        public Transform SealedDoorSlab;
        public Transform Saucer, Lever;
        [Tooltip("Round 7: tractor beam boarding (no ramp): belly iris hatch, cone of light, its spot light and floor glow.")]
        public Transform Hatch, Beam;
        public Light BeamLight;
        public Renderer BeamFloor, HatchGlow;
        [Tooltip("Round 7 (6b): the city inside the Moon and the scene's Atmosphere (blended to the city look).")]
        public CityMotion City;
        public Atmosphere Atmo;
        [Tooltip("6b dialogue set: primary | alternate | off (-cityreveal switch overrides).")]
        public string CityRevealDialogue = "primary";
        public Transform[] Clamps;
        public Renderer[] RimGlow;
        public Light SaucerUnderLight;
        [Header("Round 9")]
        [Tooltip("The fallen slab on ruin terrace C (telekinesis clears the stair).")]
        public Transform Slab;
        [Tooltip("Kyle's floating shape in the vision alcove (InscriptionGlow = its edges) and its light; its outline on the sealed door.")]
        public Transform Shape;
        public Light ShapeLight;
        [Tooltip("QF7: the collapse that buries the lock in the present (cleared by telekinesis after the vision).")]
        public Transform LockRubble;
        /// <summary>QF7: the pedestal glow - thin faint edge lines, a dim light (was 1.4, a white bloom halo).</summary>
        const float ShapeGlowIdle = 0.45f;
        public Renderer DoorSymbol;
        [Tooltip("8b addition: the door's right leaf (SealedDoorSlab = left leaf) and its engravings (0 = the S-seam line).")]
        public Transform DoorRight;
        public Renderer[] DoorEngrave;
        [Tooltip("The first vision (Art/Video/vision_01_door.mp4, copied to Assets/Video): full-screen, sides cropped.")]
        public UnityEngine.Video.VideoClip VisionVideo;
        UnityEngine.Video.VideoPlayer _vp;
        RenderTexture _vpRT;

        bool Auto => DemoBoot.CaptureDir != null && !DemoBoot.PlayerSim;     // QF5: the cat-follow test plays like a person

        // ---- QF6: what a player can see / do right now (read by the -playtest driver, which plays like a person)
        public static Chapter1Director I { get; private set; }
        /// <summary>-noqf6fix: 0.10.1's behaviour (the take step blocks, no story watchdog) - reproduces Dan's soft-lock.</summary>
        public static bool QF6Fix = true;
        public bool PromptInRange => PromptNear;
        public string PromptLabel => _promptLabel;
        public bool PromptIsTk => _promptTk;
        public bool InCutscene => _cut;
        public bool Resetting => _resetting || _resettingFall;
        public bool SlabMoved => _slabMoved;
        public bool StoneDown => _stoneDown;
        public bool ShapeTaken => _shapeTaken;
        public ScreeGuard HallGuard => Guard;
        public int TakeFallbacks { get; private set; }
        public int StoryWatchdogs { get; private set; }
        public int PlayerArea => AreaOf(Seven.transform.position);
        /// <summary>QF6: the step the story waits for now (shown in logs / the audit) and the area its trigger lives in.</summary>
        public string StepName => _step;
        string _step = "-"; int _stepArea; Vector3 _stepSpot; bool _stepRearm;
        string _objective;
        ChapterCapture Cap => ChapterCapture.I;
        ParticleSystem _motes;
        string _promptLabel; bool _promptTk; Vector3 _promptAt; float _promptR; bool _promptHit;
        bool _stoneDown, _inHall, _resetting;
        int _spotted;
        readonly Dictionary<Renderer, Material> _mats = new Dictionary<Renderer, Material>();

        // ------------------------------------------------------------------------------------------ setup
        void Start()
        {
            I = this;
            MoonTraversal.UseMoon();                        // Round 11: Venus may have swapped in its own traversal set
            if (Chapter1State.StartedAt < 0f) Chapter1State.StartedAt = Time.time;
            _motes = MakeMotes();
            Legend.SetLayout(ControlsLegend.Layout.Chapter);
            Legend.HidePanels = true;                      // Round 9: no permanent legend in the chapter
            Seven.Did += OnMove;
            Seven.FellOff = at => StartCoroutine(FallReset(at));
            Cat.StopReached += OnCatStop;
            Dlg.LineStarted += l => { if (!Chapter1State.Played.Contains(l.Id)) Chapter1State.Played.Add(l.Id); };
            if (Auto)
            {
                ChapterCapture.Ensure();
                Cap.Attach(Cam.GetComponent<Camera>());
                Seven.ScriptDriven = true;
                Cam.AutoBehind = true;
                Dlg.AutoSkipAfter = 0.9f;
                Legend.AutoSwitch = false;
                Dlg.LineStarted += OnLineForShots;
                StartCoroutine(TrackCat());
            }
            if (DemoBoot.PlayerSim)
            {
                ChapterCapture.Ensure();
                Cap.Attach(Cam.GetComponent<Camera>());
                StartCoroutine(TrackCat());
            }
            // QF5: the cat's leash (catch up when left behind) works whenever the player has 7 - never in a cutscene, a
            // reset fade, or while she is still in her cell (he waits outside it).
            Cat.LeashAllowed = () => !_cut && !_resetting && !_resettingFall && !PauseMenu.IsOpen && DemoBoot.OpeningTest <= 0
                && Seven.Current != ChapterSeven.Mode.Locked && Seven.Current != ChapterSeven.Mode.Touch && Seven.Current != ChapterSeven.Mode.Float
                && !ChapterCat.InNoCat(Seven.transform.position, 0.3f);
            Cat.SevenGrounded = () => Seven.Current == ChapterSeven.Mode.Free || Seven.Current == ChapterSeven.Mode.Land;
            // QF4: the cat may never be inside 7's cell (laser door on or off).
            ChapterCat.NoCat.Clear();
            ChapterCat.NoCat.Add(Rect.MinMaxRect(-1.75f, -1.75f, 1.75f, MoonLayout.CellWallZ1));
            SetupCheckpoint(Chapter1State.Checkpoint);
            SetupAudio();
            ChapterCamera.AreaName = p => AreaNames[Mathf.Clamp(AreaOf(p), 0, AreaNames.Length - 1)];
            ChapterCamera.CatRoot = Cat != null ? Cat.transform : null;
            CameraOccluders.Build(Seven.transform, Cat != null ? Cat.transform : null, Guard != null ? Guard.transform : null, Shape);
            if (Auto && DemoBoot.OpeningTest <= 0) { AudioKit.Log = s => Cap.Log("audio: " + s); LineOfSightCheck(); }
            if (Auto && DemoBoot.OpeningTest <= 0 && !DemoBoot.CamSweep && !DemoBoot.FlickerTest) StartCoroutine(SpeakerViewSampler());
            StartCoroutine(DemoBoot.FlickerTest ? FlickerTestRun() : DemoBoot.CamSweep ? CamSweepRun() : DemoBoot.OpeningTest > 0 ? OpeningTestRun() : DemoBoot.CatFollowTest > 0 ? CatFollowTestRun() : Flow());
            if (DemoBoot.OpeningTest <= 0 && !DemoBoot.CamSweep && !DemoBoot.FlickerTest) StartCoroutine(TeachWatch());
            if (DemoBoot.OpeningTest <= 0 && DemoBoot.CatFollowTest <= 0 && !DemoBoot.CamSweep && !DemoBoot.FlickerTest && QF6Fix) StartCoroutine(StoryWatchdog());
            StartCoroutine(ShapeIdle());
            if (VisionVideo != null)
            {
                var vgo = new GameObject("VisionVideo");
                _vp = vgo.AddComponent<UnityEngine.Video.VideoPlayer>();
                _vp.playOnAwake = false; _vp.isLooping = false; _vp.clip = VisionVideo;
                _vp.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
                _vpRT = new RenderTexture((int)VisionVideo.width, (int)VisionVideo.height, 0);
                _vp.targetTexture = _vpRT;
                _vp.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None;
                _vp.timeUpdateMode = UnityEngine.Video.VideoTimeUpdateMode.GameTime;     // pauses with the pause menu
                _vp.Prepare();
            }
        }

        // ------------------------------------------------------------------------------------------ Round 7 audio
        AudioSource _hum, _amb, _saucerHum;
        void SetupAudio()
        {
            int cp = Chapter1State.Checkpoint;
            if (cp < Chapter1State.CpCity || !_cityLook) { _amb = AudioKit.Loop("amb_cavern", null, null, 1f, AudioKit.Bus.Ambience); MusicSystem.Play("moon_prison", 2f); }
            else { _amb = AudioKit.Loop("amb_city", null, null, 1f, AudioKit.Bus.Ambience); MusicSystem.Play("moon_prison", 2f); }
            if (cp == 0 && LaserDoor != null) _hum = AudioKit.Loop("laser_hum", LaserDoor, null, 0.7f, AudioKit.Bus.Sfx, 1.2f, 14f);
            var f7 = Seven.gameObject.AddComponent<Footsteps>();
            f7.Stride = 0.75f; f7.Volume = 0.5f;
            f7.Surface = () => { var p = Seven.transform.position; var z = Ground.ZoneName(p.x, p.z); return z == "Cell" || z == "CellDoor" || z == "Corridor" || z == "Alcove" || z == "RampTop" || z == "RampA" || z == "CatwalkB" || z == "PipeRun" || z == "CatwalkC" || z == "Bulkhead" || z == "Corridor2" || z == "Scaffold1" || z == "Scaffold2" ? "step_metal_" : "step_rock_"; };
            f7.Enabled = () => Seven.Current == ChapterSeven.Mode.Free && Seven.Speed > 0.1f;
            var fc = Cat.gameObject.AddComponent<Footsteps>();
            fc.Stride = 0.3f; fc.Volume = 0.22f; fc.MaxDistance = 8f; fc.Surface = () => "paw_";
            var fg = Guard.gameObject.AddComponent<Footsteps>();
            fg.Stride = 0.8f; fg.Volume = 0.5f; fg.Surface = () => "scree_step_";
        }

        IEnumerator SwapAmbience(string to, float seconds)
        {
            var old = _amb;
            _amb = AudioKit.Loop(to, null, null, 0f, AudioKit.Bus.Ambience);
            if (_amb != null) StartCoroutine(AudioKit.Fade(_amb, 1f, seconds));
            if (old != null) yield return AudioKit.Fade(old, 0f, seconds, true);
        }

        /// <summary>Round 7 check: no straight line from anywhere inside the cell reaches the hidden release panel,
        /// and the panel is not inside the frame of any camera shot taken from inside the cell.</summary>
        void LineOfSightCheck()
        {
            var target = MoonLayout.ReleasePanel + Vector3.forward * 0.06f;      // the panel's visible face
            int rays = 0, clear = 0;
            for (float x = -1.35f; x <= 1.36f; x += 0.27f)
                for (float z = -1.35f; z <= 1.06f; z += 0.24f)
                    foreach (var y in new[] { 0.35f, 1.0f, 1.55f, 1.75f })
                        foreach (var off in new[] { Vector3.zero, new Vector3(0.18f, 0.22f, 0f), new Vector3(-0.18f, -0.22f, 0f) })
                        {
                            rays++;
                            if (!Physics.Linecast(new Vector3(x, y, z), target + off, out _, ~0, QueryTriggerInteraction.Ignore)) clear++;
                        }
            Cap.Log($"line of sight cell -> hidden release panel: {rays} rays from a grid inside the cell, {clear} unobstructed (must be 0)");
            if (DemoBoot.Quick)
                foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    if (r.enabled && r.bounds.Contains(new Vector3(0.2f, 0.45f, 0.2f))) Cap.Log($"renderer covering the cell's lower middle: {r.name} ({r.GetType().Name}) layer {r.gameObject.layer} bounds {r.bounds.min}..{r.bounds.max} mat {(r.sharedMaterial != null ? r.sharedMaterial.name + "/" + r.sharedMaterial.shader.name : "-")}");
        }
        void ShotFrameCheck(string shot)
        {
            var cam = Cam.GetComponent<Camera>();
            var p = cam.transform.position;
            bool inCell = p.x > -1.6f && p.x < 1.6f && p.z < MoonLayout.CellWallZ0 && p.z > -1.6f;
            var vp = cam.WorldToViewportPoint(MoonLayout.ReleasePanel);
            bool inFrame = vp.z > 0f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f;
            bool seen = inFrame && !Physics.Linecast(p, MoonLayout.ReleasePanel + Vector3.forward * 0.06f, out _, ~0, QueryTriggerInteraction.Ignore);
            if (inCell) Cap.Log($"release panel in shot '{shot}' (camera inside the cell): in frame={inFrame}, visible={seen} (must be false)");
        }

        Material Mat(Renderer r) { if (!_mats.TryGetValue(r, out var m)) { m = r.material; _mats[r] = m; } return m; }
        void Glow(Renderer r, float v) { if (r == null) return; var m = Mat(r); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white * v); }

        void SetZone(string name, bool on) { var z = Ground.FindZone(name); if (z != null) z.Enabled = on; }

        /// <summary>World state for a checkpoint (doors, walls, guards, positions).</summary>
        void SetupCheckpoint(int cp)
        {
            // QF6: 8 checkpoints (Chapter1State.Cp*), each a Scene Select entry; the state is what a real playthrough
            // has there (doors / wall / slab / sealed door, the shape on the pedestal or at her hip, the city look).
            bool doorOpen = cp >= Chapter1State.CpBlock, wallOpen = cp >= Chapter1State.CpPassage, sealedOpen = cp >= Chapter1State.CpCity;
            _cityLook = cp >= Chapter1State.CpCity;
            SetZone("CellDoor", doorOpen);
            SetZone("Alcove", doorOpen);
            SetZone("Passage0", wallOpen); SetZone("Passage1", wallOpen);
            SetZone("VisionExit", sealedOpen);
            // Round 9: the fallen slab blocks the ruins stair until telekinesis moves it (checkpoint 3 = past the hall)
            _slabMoved = cp >= Chapter1State.CpHall;
            SetZone("Stair", _slabMoved);
            if (Slab != null) { Slab.gameObject.SetActive(!_slabMoved); Slab.position = MoonTraversal.SlabRest; Slab.rotation = Quaternion.Euler(4f, 12f, -6f); }
            _shapeGlow = ShapeGlowIdle;
            _shapeTaken = cp >= Chapter1State.CpDoor;       // QF7: she has it from the end of the vision on (checkpoint 5)
            if (Shape != null) Show(Shape.gameObject, true);
            if (Shape != null && _shapeTaken)
            {
                // QF7: in a pocket of her suit - out of sight, no light, no glow on her
                _carry = CarriedShape.Attach(Seven.gameObject, Shape);
                _carry.Set(CarriedShape.Where.Pocket, 0f);
            }
            if (ShapeLight != null) ShapeLight.enabled = !_shapeTaken;
            ResetRubble(!sealedOpen);                        // QF7: the lock stays buried until she clears it
            Glow(DoorSymbol, sealedOpen ? 0.7f : cp == Chapter1State.CpDoor ? 0.9f : 0f);
            Ground.IgnoreObstacle = -1;
            Show(Seven.gameObject, true); Show(Cat.gameObject, true);
            foreach (var b in LaserBeams) if (b != null) b.enabled = !doorOpen;
            if (LaserLight != null) LaserLight.enabled = !doorOpen;
            Seven.SetEnergyPoint(doorOpen ? 1f : 0f);       // the laser field dampens her powers: dark inside the cell
            if (_cityLook) StartCoroutine(CityLook(0f));
            if (cp >= Chapter1State.CpBay && City != null) City.StartMachinery();     // QF6: after the reveal it keeps working
            OldWallSlab.localPosition = wallOpen ? new Vector3(0f, -2.7f, 0f) : Vector3.zero;
            SealedDoorSlab.localPosition = sealedOpen ? new Vector3(-LeafOpen, 0f, 0f) : Vector3.zero;
            if (DoorRight != null) DoorRight.localPosition = sealedOpen ? new Vector3(LeafOpen, 0f, 0f) : Vector3.zero;
            if (DoorEngrave != null) foreach (var r in DoorEngrave) Glow(r, sealedOpen ? 0.6f : 0f);
            VisionWhole.SetActive(false); VisionRuins.SetActive(true);
            Glow(InscriptionGlow, sealedOpen ? 0.6f : 1.4f);
            Glow(ReleaseLight, 0.8f);
            foreach (var g in GateGuards) g.Stand();
            if (Workers != null) foreach (var w in Workers) w.ResetToPost();
            Guard.ResetToPost();
            Stone.position = MoonLayout.StoneRest; _stoneDown = false;
            foreach (var r in RimGlow) Glow(r, 0.9f);
            Vector3 p7, f7, pc;
            switch (cp)
            {
                case 0: p7 = MoonLayout.SevenStart; f7 = Vector3.forward; pc = MoonLayout.VentMouth; break;
                case 1: p7 = new Vector3(0f, 0f, 2.9f); f7 = Vector3.right; pc = new Vector3(1f, 0f, 3.5f); break;
                case 2: p7 = new Vector3(16.5f, 0f, 1.2f); f7 = Vector3.back; pc = new Vector3(16.9f, 0f, 2.3f); break;
                case 3: p7 = MoonLayout.HallEntry + Vector3.forward * 0.4f; f7 = Vector3.back; pc = MoonLayout.HallEntry + new Vector3(0.8f, 0f, 0.9f); break;
                case 4: p7 = new Vector3(16.5f, MoonLayout.Lower, -26.5f); f7 = Vector3.back; pc = new Vector3(16.9f, MoonLayout.Lower, -25.4f); break;
                case 5: p7 = MoonLayout.ShapeSpot + new Vector3(-0.6f, 0f, 0.5f); f7 = Vector3.right; pc = MoonLayout.ShapeSpot + new Vector3(-0.9f, 0f, 1.0f); break;
                case 6: p7 = new Vector3(16.5f, MoonLayout.Lower, -38.85f); f7 = Vector3.back; pc = new Vector3(17.05f, MoonLayout.Lower, -38.3f); break;
                default: p7 = MoonLayout.RevealSpot + new Vector3(0.6f, 0f, -0.4f); f7 = new Vector3(0.6f, 0f, -1f); pc = MoonLayout.RevealSpot + new Vector3(1.2f, 0f, 0.1f); break;
            }
            Seven.Place(p7, f7);
            Cat.Place(pc, f7);
            if (cp > 0) Cat.Follow();
            else Cat.Hold(p7);                  // QF4: sits by his opening until the script sends him (never follows into the cell)
            Cam.SnapBehind();
        }

        // ------------------------------------------------------------------------------------------ helpers
        IEnumerator Wait(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }

        /// <summary>Wait for a condition; on a check run, give up after a timeout and log it (soft-lock suspicion).</summary>
        IEnumerator Until(Func<bool> cond, string what, float autoTimeout = 60f)
        {
            float t0 = Time.time;
            while (!cond())
            {
                if (Auto && Time.time - t0 > autoTimeout) { Cap.Soft++; Cap.Log($"SOFT-LOCK? timed out waiting for {what} (7 at {Seven.transform.position}, {Seven.Current}/{Seven.State}; cat at {Cat.transform.position} {Cat.Current})"); yield break; }
                yield return null;
            }
            if (Auto || DemoBoot.PlayerSim) Cap?.Log($"ok: {what} ({Time.time - t0:0.0}s)");
        }

        /// <summary>QF4 watchdog: wait for a condition; after <paramref name="timeout"/> seconds (in play too, not only on
        /// check runs) run the fallback (e.g. place the cat on his mark) and go on - never a soft-lock.</summary>
        IEnumerator UntilOr(Func<bool> cond, string what, float timeout, Action fallback)
        {
            float t0 = Time.time;
            while (!cond())
            {
                if (Time.time - t0 > timeout)
                {
                    Chapter1State.Watchdogs++;
                    Debug.Log($"[Seven] watchdog after {timeout:0}s: {what}");
                    if (Auto || DemoBoot.PlayerSim) Cap?.Log($"WATCHDOG fallback after {timeout:0.0}s: {what} (7 at {Seven.transform.position}; cat at {Cat.transform.position} {Cat.Current})");
                    fallback?.Invoke();
                    yield break;
                }
                yield return null;
            }
            if (Auto || DemoBoot.PlayerSim) Cap?.Log($"ok: {what} ({Time.time - t0:0.0}s)");
        }

        /// <summary>Scripted walk for 7 (watchdog fallbacks): steer, then place her if she could not get there.</summary>
        IEnumerator WalkSeven(Vector3 to, float timeout)
        {
            bool sd = Seven.ScriptDriven;
            Seven.ScriptDriven = true; Seven.Lock(false);
            for (float t0 = Time.time; Dist7(to) > 0.25f && Time.time - t0 < timeout;)
            { Seven.ScriptMove = Flat(to - Seven.transform.position).normalized * 0.8f; yield return null; }
            Seven.ScriptMove = Vector3.zero;
            if (Dist7(to) > 0.25f) Seven.Place(to, Vector3.forward);
            Seven.ScriptDriven = sd;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        float Dist7(Vector3 p) => Flat(Seven.transform.position - p).magnitude;

        /// <summary>Check runs: walk 7 along points (world direction steering).</summary>
        int _walkToken;
        IEnumerator AutoWalk(params Vector3[] pts)
        {
            int token = ++_walkToken;
            foreach (var p in pts)
            {
                float t0 = Time.time;
                while (Dist7(p) > 0.3f && Time.time - t0 < 25f)
                {
                    if (token != _walkToken) yield break;
                    if (Seven.Current == ChapterSeven.Mode.Free) Seven.ScriptMove = Flat(p - Seven.transform.position).normalized;
                    yield return null;
                }
                if (token != _walkToken) { Cap.Log($"auto walk to {p} replaced by a newer one"); yield break; }
                if (Dist7(p) > 0.3f) { Cap.Soft++; Cap.Log($"SOFT-LOCK? could not reach {p} (at {Seven.transform.position})"); }
            }
            Seven.ScriptMove = Vector3.zero;
            Cap.Log($"auto walk done at {Seven.transform.position}");
        }

        void Objective(string s) { _objective = s; UI.SetObjective(s); if (Auto || DemoBoot.PlayerSim) Cap?.Log("objective: " + s); }

        /// <summary>QF8: <paramref name="where"/> (optional) replaces the radius test - a generous area + look angle.</summary>
        void Prompt(string label, Vector3 at, float radius, bool tk = false, Func<bool> where = null)
        {
            _promptLabel = label; _promptAt = at; _promptR = radius; _promptTk = tk; _promptHit = false; _promptWhere = where;
        }
        Func<bool> _promptWhere;
        void ClearPrompt() { _promptLabel = null; _promptHit = false; _promptWhere = null; Legend.Context(ControlsLegend.Btn.Interact, null); }
        bool PromptNear => _promptLabel != null && (_promptWhere != null ? _promptWhere() : Dist7(_promptAt) < _promptR) && Seven.Current == ChapterSeven.Mode.Free && !Dlg.Busy;
        IEnumerator WaitPrompt(string what)
        {
            if (Auto)
            {
                yield return Until(() => PromptNear, what + " (in range)", 30f);
                _promptHit = true;
            }
            yield return Until(() => _promptHit, what, 30f);
            ClearPrompt();
        }

        bool _cut;
        IEnumerator Cutscene(bool on)
        {
            _cut = on;
            Seven.Lock(on);
            if (!on) { Cam.Follow(); if (Auto) Seven.ScriptMove = Vector3.zero; }
            yield return null;
        }

        IEnumerator ShotWhileTalking(string name, float after) { yield return Wait(after); yield return Shot(name); }

        IEnumerator Shot(string name)
        {
            if (!Auto && !(DemoBoot.PlayerSim && (name.StartsWith("qf7_") || name.StartsWith("qf12_")) && Cap != null)) yield break;
            ShotFrameCheck(name);
            if (Dlg.Busy && Cat != null && Cat.gameObject.activeInHierarchy && Cam.InShot)
            {
                ShotCheck.Prepare(Seven.transform, Cat.transform);
                float cv = ShotCheck.Visible(Cam.GetComponent<Camera>());
                Cap.Log($"shot '{name}': cat {cv * 100f:0} % unobstructed in a dialogue shot (QF8: must be >= 80 where he is framed)");
            }
            if (Legend.VisiblePanels > 0) Cap.Log($"LEGEND VISIBLE in shot '{name}': {Legend.VisiblePanels} panel(s) (must be 0 in the chapter)");
            if (Legend.TeachText != null || Legend.ContextText != null) Cap.Log($"prompts in shot '{name}': teach [{Legend.TeachText}] context [{Legend.ContextText}]");
            yield return Cap.Shot(name);
        }
        bool _cityLook;

        readonly Dictionary<string, string> _shotOnLine = new Dictionary<string, string>
        {
            { "s01_03_cat", "s01_first_contact" }, { "s02_03_cat", "s02_cat_at_release" }, { "s02_06_7", "s02_door_open_which_way" },
            { "s03_05_7", "s03_walk_and_talk" }, { "s04_02_cat", "s04_older_wall" }, { "s05_01_cat", "s05_scree_guard" },
            { "s05_04_7", "s05_slipped_past" }, { "s06_02_cat", "s06_vision_during_far_voice" }, { "s06_07_7", "s06_after_vision" },
            { "s08_02_cat", "s08_at_the_edge_of_the_light" },
        };
        void OnLineForShots(VoiceBank.Line l)
        {
            if (_shotOnLine.TryGetValue(l.Id, out var name)) StartCoroutine(ShotLater(name, 0.35f));
        }
        IEnumerator ShotLater(string name, float delay) { yield return Wait(delay); yield return Shot(name); }

        IEnumerator TrackCat()
        {
            while (true)
            {
                if (Cat.Current == ChapterCat.Mode.Follow || Cat.Current == ChapterCat.Mode.Lead || Cat.Current == ChapterCat.Mode.Route)
                    Cap.CatMaxBehind = Mathf.Max(Cap.CatMaxBehind, Cat.DistanceTo7);
                yield return null;
            }
        }

        // ------------------------------------------------------------------------------------------ input
        void Update()
        {
            if (PauseMenu.IsOpen) return;
            if (!Auto)
            {
                // Round 7: Esc / Start open the pause menu (PauseMenu) instead of leaving directly.
                if (Input.Restart.WasPressedThisFrame() && !Dlg.Busy) { SceneManager.LoadScene(SceneManager.GetActiveScene().name); return; }
            }
            bool near = PromptNear;
            // Round 9: one quiet context prompt - the scripted action here, else what a traversal button would do
            if (near) Legend.Context(_promptTk ? ControlsLegend.Btn.Tk : ControlsLegend.Btn.Interact, _promptLabel);
            else if (Seven.Context != null && !Dlg.Busy && !_resettingFall)
            {
                int c = Seven.Context.IndexOf(':');
                Legend.Context(BtnOf(Seven.Context.Substring(0, c)), Seven.Context.Substring(c + 1));
            }
            else Legend.Context(ControlsLegend.Btn.Interact, null);
            if (near && !Auto)
            {
                if (!_promptTk && Input.Interact.WasPressedThisFrame()) _promptHit = true;
                if (_promptTk && Input.Telekinesis.WasPressedThisFrame()) _promptHit = true;
            }
        }

        // ------------------------------------------------------------------------------------------ flow
        IEnumerator Flow()
        {
            int cp = Chapter1State.Checkpoint;
            if (Auto || DemoBoot.PlayerSim) Cap?.Log($"flow starts at checkpoint {cp} ({Chapter1State.CpName(cp)})");
            if (cp <= Chapter1State.CpCell) yield return Scene1And2();
            if (cp <= Chapter1State.CpBlock) yield return Scene3And4();
            if (cp <= Chapter1State.CpPassage) yield return Scene5Ruins();
            if (cp <= Chapter1State.CpHall) yield return Scene5Hall();
            if (cp <= Chapter1State.CpVision) yield return Scene6Vision();
            if (cp <= Chapter1State.CpDoor) yield return Scene6Door();
            if (cp <= Chapter1State.CpCity) yield return Scene6bReveal();
            yield return SceneBay();
        }

        /// <summary>QF6: a checkpoint is also the Continue save (quit and Continue resumes here) and a Scene Select entry.</summary>
        void Checkpoint(int n)
        {
            Chapter1State.Checkpoint = n;
            if (DemoBoot.CatFollowTest <= 0 && DemoBoot.OpeningTest <= 0) SaveGame.SaveProgress(n);
            Debug.Log($"[Seven] checkpoint {n} ({Chapter1State.CpName(n)})");
            if (Auto || DemoBoot.PlayerSim) Cap?.Log($"checkpoint {n} ({Chapter1State.CpName(n)})");
        }

        // ------------------------------------------------------------------------------------------ QF6 story watchdog
        /// <summary>Which stretch of the chapter a point is in (0 cell · 1 cell block · 2 old passage + ruins · 3 Scree hall ·
        /// 4 vision chamber · 5 mining cut · 6 terrace + ramp · 7 bay floor). Only the traversal links join them, in order.</summary>
        public static int AreaOf(Vector3 p)
        {
            if (p.y < MoonLayout.Bay + 0.6f) return 7;
            if (p.z < MoonLayout.ViewpointZ) return 6;
            if (p.z < -38.0f) return 5;
            if (p.z < MoonLayout.HallExitZ) return 4;
            if (p.z < MoonLayout.HallEnterZ) return 3;
            if (Mathf.Abs(p.x) < 1.65f && p.z < MoonLayout.CellWallZ0 + 0.05f && Mathf.Abs(p.y) < 0.5f) return 0;   // the cell first (it reaches z -1.6)
            if (p.z < -0.2f || p.y > 2.6f && p.x > 15.5f) return 2;
            return 1;
        }
        static readonly string[] AreaNames = { "cell", "cell block", "old passage + ruins", "Scree hall", "vision chamber", "mining cut", "terrace + ramp", "bay floor" };

        /// <summary>QF6: declare the story step the flow waits for: its trigger fires in <paramref name="area"/> (or later);
        /// the watchdog re-arms the objective if the player is ever further along than that (see <see cref="StoryWatchdog"/>).</summary>
        void Step(string name, int area, Vector3 spot, bool rearm = true)
        {
            _step = name; _stepArea = area; _stepSpot = spot; _stepRearm = rearm;
            if (Auto || DemoBoot.PlayerSim) Cap?.Log($"story step: {name} (fires in area {area} '{AreaNames[Mathf.Clamp(area, 0, 7)]}' or later)");
        }

        /// <summary>QF6 rule: no trigger may depend on one exact moment or spot. Every trigger in the flow already fires
        /// when 7 is at OR PAST it; this watchdog is the net under that: (1) the current objective is re-shown whenever the
        /// player has control and it has gone missing; (2) if 7 is ever further along the chapter than the step the story is
        /// waiting for (for 4 s, outside cutscenes and resets), it logs WATCHDOG and returns her - white fade, no harm - to that
        /// step's spot, so its trigger can fire. In a correct flow (2) never happens.</summary>
        IEnumerator StoryWatchdog()
        {
            float ahead = 0f;
            while (true)
            {
                yield return null;
                bool control = !_cut && !_resetting && !_resettingFall && !PauseMenu.IsOpen && !Dlg.Busy && UI.FadeAlpha < 0.05f;
                if (!control) { ahead = 0f; continue; }
                if (_objective != null && UI.Objective != _objective) { UI.SetObjective(_objective); Debug.Log($"[Seven] objective re-shown: {_objective}"); }
                if (!_stepRearm || Seven.Current != ChapterSeven.Mode.Free) { ahead = 0f; continue; }
                int a = PlayerArea;
                ahead = a > _stepArea ? ahead + Time.deltaTime : 0f;
                if (ahead < 4f) continue;
                ahead = 0f;
                StoryWatchdogs++; Chapter1State.Watchdogs++; Chapter1State.StoryWatchdogs++;
                var msg = $"WATCHDOG: 7 is in area {a} '{AreaNames[a]}' while the story waits for '{_step}' (area {_stepArea}) - back to {_stepSpot}";
                Debug.Log("[Seven] " + msg);
                Cap?.Log(msg);
                _resettingFall = true;
                yield return UI.Fade(1f, 0.45f, 1f);
                Seven.Place(_stepSpot, Vector3.back);
                Cat.Place(_stepSpot + new Vector3(0.7f, 0f, 0.6f), Vector3.back);
                Cat.Follow();
                Cam.SnapBehind();
                yield return UI.Fade(0f, 0.6f, 1f);
                _resettingFall = false;
            }
        }

        /// <summary>The cell up to the first-contact shot. QF4: the cat walks a fixed route to his mark outside the door
        /// (he can never enter the cell: <see cref="ChapterCat.NoCat"/>); scene 1 starts when 7 reaches the door, or by
        /// the watchdog 20 s after the cat arrived (45 s after he was sent at most) - then 7 is walked to the door. If the
        /// cat is not on his mark by then he is placed there.</summary>
        IEnumerator Opening()
        {
            bool test = DemoBoot.OpeningTest > 0;
            UI.SetFade(1f);
            Seven.Lock(true);
            // Round 12 (2d): the opening shot - the Moon in space with the title, the dive through the crust, then her cell
            if (!test && (Chapter1State.Intro || DemoBoot.OpeningShot))
            {
                Chapter1State.Intro = false;
                float amb0 = _amb != null ? _amb.volume : 1f; if (_amb != null) _amb.volume = 0f;
                yield return MoonOpening.Play(this, Cam.GetComponent<Camera>(), n => Shot(n), s => { if (Auto || DemoBoot.PlayerSim) Cap?.Log(s); });
                if (_amb != null) StartCoroutine(AudioKit.Fade(_amb, amb0, 2.5f));
                MusicSystem.Play("moon_prison", 3f);                       // fades in as she appears
            }
            StartCoroutine(UI.Title("CHAPTER 1  —  THE MOON PRISON", 1.0f, 2.0f, 1.0f));
            yield return Wait(0.8f);
            yield return UI.Fade(0f, 1.8f);
            Seven.Lock(false);
            if (!test) yield return Shot("s01_cell_start");
            if (Auto && DemoBoot.OpeningShot) { Cap.Finish("opening shot check (2d): " + (MoonOpening.Skipped ? "skipped" : "watched") + ", then the cell"); yield break; }
            if (Auto && !test) yield return LaserCollisionCheck();
            // The cat slips out of the small opening and sits outside the cell, watching her.
            yield return Wait(1.5f);
            Cat.GoTo(MoonLayout.CatWaitOutside, Cat.StrollSpeed, MoonLayout.DoorApproach);
            Objective("Look around");
            if (Auto && !test) yield return Until(() => Cat.Arrived, "cat at the cell door", 20f);
            Objective("Go to the cell door");
            if (Auto && !test) yield return AutoWalk(MoonLayout.DoorApproach);
            float tStart = Time.time, catAt = -1f;
            string trigger = "7 at the door";
            // Round 9: X / E starts the first contact. QF8 (Dan: "only if 7 stands in exactly the right place"): the prompt
            // shows anywhere in the cell while she is turned roughly toward the door / the cat (110 deg either side of
            // straight at him), and the scene starts by itself 8 s after the cat is on his mark if the player never presses it.
            Prompt("Look", MoonLayout.DoorApproach, 0.8f, false, () =>
            {
                var p = Seven.transform.position;
                if (Mathf.Abs(p.x) > 1.9f || p.z > MoonLayout.CellWallZ0 + 0.3f || p.z < -2.0f || Mathf.Abs(p.y) > 0.6f) return false;
                if (Dist7(MoonLayout.DoorApproach) < 0.9f) return true;
                var to = Flat((Cat.Arrived ? Cat.transform.position : MoonLayout.CatWaitOutside) - p);
                return to.sqrMagnitude < 1e-4f || Vector3.Angle(Flat(Seven.transform.forward), to) < 110f;
            });
            while (true)
            {
                if (Cat.Arrived && catAt < 0f) catAt = Time.time;
                if (_promptHit && !PauseMenu.IsOpen) break;
                if (Auto && Dist7(MoonLayout.DoorApproach) < 0.75f && !PauseMenu.IsOpen)
                {
                    if (!test) yield return AwaitTeach("interact");
                    _promptHit = true; yield return null; break;
                }
                if (!PauseMenu.IsOpen && catAt > 0f && Time.time - catAt > (test ? 20f : 8f)) { trigger = "auto-start: 8 s after the cat arrived (QF8)"; break; }
                if (!PauseMenu.IsOpen && Time.time - tStart > 45f) { trigger = "watchdog: 45 s after the cat was sent"; break; }
                yield return null;
            }
            OpeningTrigger = trigger;
            ClearPrompt();
            if (Auto || DemoBoot.PlayerSim) Cap?.Log($"opening trigger: {trigger} ({Time.time - tStart:0.0}s after the cat was sent; cat arrived {(catAt > 0f ? (catAt - tStart).ToString("0.0") + "s" : "no")})");
            if (trigger.StartsWith("watchdog")) Chapter1State.Watchdogs++;
            // the cat must be on his mark (outside the door) before the scene: wait a moment, else place him
            yield return UntilOr(() => Cat.Arrived, "cat on his mark outside the door", 4f, () => Cat.ForceArrive());

            // Scene 1: first contact (through the laser beams).
            yield return Cutscene(true);
            Objective(null);
            if (Dist7(MoonLayout.DoorApproach) > 0.75f) yield return WalkSeven(MoonLayout.DoorApproach, 5f);
            Seven.Lock(true);
            Talk();
            Cam.Shot(new Vector3(0.45f, 1.75f, 4.3f), new Vector3(-0.05f, 1.3f, 0.9f), 1.4f);
            yield return Wait(1.0f);
            yield return ReadyToTalk();
            if (!test) yield return Shot("s01_laser_door_on");
        }
        string OpeningTrigger;

        IEnumerator Scene1And2()
        {
            Step("first contact + the cat opens the door (cell)", 1, MoonLayout.DoorApproach, false);   // the laser door holds her in the cell
            yield return Opening();
            if (Auto && DemoBoot.Quick) { Cap.Finish("quick chapter check (first shots only)"); yield break; }
            yield return Dlg.PlayScene(1);
            Checkpoint(0);

            // Scene 2: the cat goes out of sight to a hidden release; she only hears it.
            yield return Dlg.PlayScene(2, 1, 1);            // "Stand back from the door."
            Cam.Follow();
            Seven.Lock(false);
            Objective("Stand back from the door");
            StartCoroutine(Dlg.PlayScene(2, 2, 3));        // "You know how to open it?" / "I wouldn't have asked..."
            SetZone("Alcove", true);
            Cat.Lead(new List<Vector3> { MoonLayout.AlcoveMouth, MoonLayout.CrateFoot }, 99f);
            if (Auto) yield return AutoWalk(new Vector3(0.1f, 0f, -0.35f));
            yield return Until(() => Seven.transform.position.z < 0.15f, "7 stands back", 40f);
            yield return UntilOr(() => Cat.Arrived, "cat in the guard alcove (out of sight)", 25f, () => Cat.ForceArrive());
            yield return Until(() => !Dlg.Busy, "lines finished", 25f);
            Seven.Lock(true);
            // From inside the cell: 7 and the beams; the cat is somewhere off to the left, out of sight.
            Cam.Shot(new Vector3(0.85f, 1.55f, -1.25f), new Vector3(0.05f, 1.15f, 1.6f), 1.0f);
            Objective(null);
            Cat.Hop(MoonLayout.CrateTop, 0.8f, 0.5f);
            yield return UntilOr(() => Cat.Arrived, "cat on the crate in the alcove", 6f, () => Cat.ForceArrive());
            if (Auto)
            {
                // Check-run only view (the player never sees this): the cat at the hidden release.
                Cam.Shot(MoonLayout.AlcoveMouth + new Vector3(0.6f, 1.5f, 1.4f), MoonLayout.ReleasePanel + Vector3.down * 0.3f, 0.01f, true, 60f);
                yield return Wait(0.05f);
                yield return Shot("s02_checkonly_cat_at_hidden_release");
                Cam.Shot(new Vector3(0.85f, 1.55f, -1.25f), new Vector3(0.05f, 1.15f, 1.6f), 0.01f, true);
            }
            yield return Wait(0.4f);
            Cat.Paw();
            yield return Wait(0.7f);
            AudioKit.Play("release_click", MoonLayout.ReleasePanel, 1f, 0f, AudioKit.Bus.Sfx, 18f);
            Glow(ReleaseLight, 4f);
            yield return Wait(0.35f);
            yield return Shot("s02_heard_click_beams_on");
            yield return LasersOff();
            yield return Shot("s02_beams_off");
            Glow(ReleaseLight, 0.8f);
            Cat.Hop(MoonLayout.CrateFoot, 0.6f, 0.3f);
            yield return UntilOr(() => Cat.Arrived, "cat down from the crate", 4f, () => Cat.ForceArrive());
            Cat.Lead(new List<Vector3> { MoonLayout.AlcoveMouth, new Vector3(-0.4f, 0f, 2.9f) }, 99f);
            Cam.Follow();
            Seven.Lock(false);
            Objective("Step out");
            if (Auto) yield return FaceShot("s02_energy_point_dark_closeup");
            if (Auto) yield return AutoWalk(new Vector3(0f, 0f, 1.6f), new Vector3(0.1f, 0f, 2.8f));
            yield return Until(() => Seven.transform.position.z > 2.2f, "7 steps out", 40f);
            yield return Cutscene(true);
            yield return UntilOr(() => Cat.Arrived, "cat back from the alcove", 8f, () => Cat.ForceArrive());
            Cat.Hold(Seven.transform.position);
            var face = Flat(Cat.transform.position - Seven.transform.position);
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(face), 360f * Time.deltaTime); yield return null; }
            Talk();
            Cam.Shot(new Vector3(1.6f, 1.75f, 4.4f), new Vector3(-0.2f, 1.0f, 2.7f), 1.1f);
            yield return ReadyToTalk();
            yield return Dlg.PlayScene(2, 4, 6);           // Thank you / You're welcome / Which way?
            Cat.Hold(MoonLayout.VentMouth);                // looks toward the small opening he used
            yield return Dlg.PlayScene(2, 7, 8);
            yield return Cutscene(false);
            Checkpoint(1);
        }

        /// <summary>The energy point relights as she steps out past the dead beams (watched every frame).</summary>
        IEnumerator EnergyPointWatch()
        {
            while (Seven.EnergyPoint < 0.5f)
            {
                if (Seven.transform.position.z > MoonLayout.CellWallZ1 + 0.15f)
                {
                    Seven.SetEnergyPoint(1f, 1.6f);
                    AudioKit.Play("energy_point", null, 0.8f);
                    if (Auto) { Cap.Log($"energy point relights at z {Seven.transform.position.z:0.00} (beams at {MoonLayout.LaserZ:0.00})"); yield return Wait(1.9f); yield return FaceShot("s02_energy_point_relit_closeup"); }
                    yield break;
                }
                yield return null;
            }
        }

        /// <summary>Check runs only: a short face close-up (the energy point), then back to the normal camera.</summary>
        IEnumerator FaceShot(string name)
        {
            var head = Seven.Bone(HumanBodyBones.Head);
            if (head == null) yield break;
            var c = head.position + Vector3.up * 0.06f;
            bool wasShot = Cam.InShot;
            var sp = Cam.ShotPos; var sl = Cam.ShotLook; float sf = Cam.Fov;
            Cam.Shot(c + Seven.transform.forward * 0.75f + Vector3.up * 0.05f, c, 0.01f, true, 40f);
            yield return null;
            yield return Shot(name);
            if (!wasShot) Cam.Follow();
            else Cam.Shot(sp, sl, 0.01f, true, sf);       // back to the conversation shot
        }

        /// <summary>Beams switch off one by one (top to bottom) after the click; the hum dies; the doorway opens.</summary>
        IEnumerator LasersOff()
        {
            AudioKit.Play("laser_power_down", LaserDoor.position + Vector3.up * 1.2f, 1f, 0f, AudioKit.Bus.Sfx, 16f);
            if (_hum != null) StartCoroutine(AudioKit.Fade(_hum, 0f, 1.6f, true));
            for (int i = MoonLayout.LaserCount - 1; i >= 0; i--)
            {
                for (int k = 0; k < 2; k++)
                {
                    int idx = i * 2 + k;
                    if (idx < LaserBeams.Length && LaserBeams[idx] != null) LaserBeams[idx].enabled = false;
                }
                if (LaserLight != null) LaserLight.intensity *= 0.82f;
                yield return Wait(0.17f);
            }
            if (LaserLight != null) LaserLight.enabled = false;
            SetZone("CellDoor", true);
            StartCoroutine(EnergyPointWatch());
            if (Auto) Cap.Log("laser door: all beams off, doorway open");
        }

        /// <summary>Check: drive 7 straight and diagonally into the beams from several spots; she must stay inside.</summary>
        IEnumerator LaserCollisionCheck()
        {
            float maxZ = float.MinValue; var start = Seven.transform.position;
            var tries = new[] { (new Vector3(0f, 0f, 0.2f), new Vector3(0f, 0f, 1f)), (new Vector3(-0.9f, 0f, 0.2f), new Vector3(0.6f, 0f, 1f)),
                                (new Vector3(0.9f, 0f, 0.2f), new Vector3(-0.6f, 0f, 1f)), (new Vector3(0.3f, 0f, 0.6f), new Vector3(0.15f, 0f, 1f)),
                                (new Vector3(-0.3f, 0f, 0.6f), new Vector3(-1f, 0f, 0.4f)) };
            foreach (var (from, dir) in tries)
            {
                Seven.Place(from, dir);
                for (float t = 0f; t < 1.4f; t += Time.deltaTime)
                {
                    Seven.ScriptMove = dir.normalized; Seven.ScriptRun = true;
                    maxZ = Mathf.Max(maxZ, Seven.transform.position.z);
                    yield return null;
                }
                Seven.ScriptMove = Vector3.zero; Seven.ScriptRun = false;
            }
            Seven.Place(start, Vector3.forward);
            Cap.Log($"laser door collision: 5 runs into the beams (straight, diagonal, running) - 7's furthest z {maxZ:0.000} (root; beams at z {MoonLayout.LaserZ:0.000}, body radius 0.25) -> {(maxZ + 0.25f <= MoonLayout.LaserZ + 0.001f ? "stays inside" : "REACHED THE BEAMS")}");
        }

        IEnumerator Scene3And4()
        {
            Step("look closer at the older wall", 1, MoonLayout.OldWallSpot, false);                // the wall itself is the way on
            Objective("Follow the cat");
            // Round 9: the cat leads over the broken catwalk, under the pipes and over the bulkhead, waiting at each one.
            float px() => Seven.transform.position.x;
            CatRoute("prison", new List<ChapterCat.Stop>
            {
                Walk(new Vector3(7.9f, 0f, 2.45f)),
                HopTo(new Vector3(10.45f, 0f, 2.4f), () => px() > 6.0f && !_scene3, 0.55f),
                Walk(new Vector3(13.55f, 0f, 2.3f), () => px() > 9.9f && Seven.transform.position.y > -0.2f),
                HopTo(new Vector3(14.6f, MoonTraversal.BulkheadTop, 2.35f), () => px() > 12.9f, 0.7f),
                HopTo(new Vector3(15.85f, 0f, 2.35f), () => Seven.transform.position.y > 2.0f, 0.4f),
                Walk(new Vector3(15.4f, 0f, 3.4f), () => px() > 15.3f),
            });
            if (Auto) StartCoroutine(AutoPrison());
            yield return Until(() => Seven.transform.position.x > MoonLayout.Scene3TriggerX, "corridor: scene 3 trigger", 40f);
            _scene3 = true;
            // Scene 3 as a walk-and-talk (the first safe pause after the cell).
            yield return Dlg.PlayScene(3);
            _scene3 = false;
            yield return Until(() => Seven.transform.position.x > 14.0f && Seven.transform.position.y > 2.0f || Seven.transform.position.x > 15.3f, "on top of the bulkhead (the gate in view)", 120f);
            UI.Hint("Guards at the gate ahead.", 3.5f);
            yield return Shot("s04_guarded_route");
            Objective("Find another way out");
            Prompt("Look closer", MoonLayout.OldWallSpot, 1.5f);
            yield return WaitPrompt("look closer at the older wall");

            // Scene 4.
            yield return Cutscene(true);
            Objective(null);
            Talk();
            Cam.Shot(new Vector3(18.9f, 1.8f, 3.9f), new Vector3(16.3f, 1.3f, 2.1f), 1.2f);
            Seven.Touch(MoonLayout.OldWall);
            yield return ReadyToTalk();
            yield return Dlg.PlayScene(4, 1, 4);
            Seven.Release();
            // The old stones give: the slab sinks and opens a gap into the structures beneath the facility.
            Burst(MoonLayout.OldWall + Vector3.forward * 0.2f, 60, 1.2f);
            AudioKit.Play("stone_door", MoonLayout.OldWall, 1f, 0f, AudioKit.Bus.Sfx, 25f);
            var from = OldWallSlab.localPosition; var to = new Vector3(0f, -2.7f, 0f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.2f) { OldWallSlab.localPosition = Vector3.Lerp(from, to, t * t); yield return null; }
            OldWallSlab.localPosition = to;
            SetZone("Passage0", true); SetZone("Passage1", true);
            yield return Dlg.PlayScene(4, 5, 5);
            yield return Cutscene(false);
            Checkpoint(2);
        }

        /// <summary>QF6: scene 5 is two checkpoints now - the old passage + ruins (up to the hall door), then the hall.</summary>
        IEnumerator Scene5Ruins()
        {
            Step("ancient hall entrance", 3, MoonLayout.HallEntry);
            Objective("Follow the old passage");
            // Round 9: up the cracked wall, along the shaft lip over the dark drop, down into the ruins, across the broken
            // terraces, telekinesis on the fallen slab, down the stair to the hall door. The cat shows the way.
            Vector3 P7() => Seven.transform.position;
            CatRoute("passage + ruins", new List<ChapterCat.Stop>
            {
                Walk(new Vector3(17.0f, 0f, 0.6f)),
                HopTo(MoonTraversal.StubCrackWall, () => Seven.State == "wall_climb" || P7().y > 0.4f, 0.45f),
                HopTo(new Vector3(17.0f, 2.8f, -0.75f), null, 0.5f),
                HopTo(MoonTraversal.StubShaft, () => Hanging(MoonTraversal.ShaftLip), 0.6f),
                HopTo(new Vector3(11.3f, -1.6f, -3.15f), () => Hanging(MoonTraversal.ShaftLip) && P7().x < 14.2f || OnZone("RuinsLedge"), 0.5f),
                HopTo(new Vector3(7.5f, -1.6f, -3.3f), () => OnZone("RuinsLedge"), 0.6f),
                HopTo(new Vector3(7.2f, -0.7f, -6.25f), () => OnZone("TerraceA"), 0.55f),
                HopTo(new Vector3(13.0f, 0.8f, -6.6f), () => OnZone("TerraceB"), 0.9f),
                Walk(new Vector3(18.8f, MoonLayout.Lower, -5.4f), () => _slabMoved),
                Walk(new Vector3(16.0f, MoonLayout.Lower, -7.6f), () => OnZone("StairFoot") || OnZone("RuinsFloor")),
            });
            StartCoroutine(SlabSequence());
            if (Auto) StartCoroutine(AutoPassageRuins());
            yield return Until(() => Seven.transform.position.z < MoonLayout.HallEnterZ, "ancient hall entrance", 600f);
            _slabMoved = true;                                   // (the stair is the only way down here)
            Checkpoint(Chapter1State.CpHall);
        }

        IEnumerator Scene5Hall()
        {
            Step("slip past the Scree guard", 4, MoonLayout.HallEntry);
            yield return Until(() => Seven.transform.position.z < MoonLayout.HallEnterZ, "in the ancient hall", 60f);
            yield return Cutscene(true);
            // QF4: the old camera (17.6, -7.6) sat behind the passage wall. They step a little into the hall so one
            // framing from inside it holds 7, the cat and the guard beyond them (and the clear-view check guards it).
            ++_walkToken;
            Cat.GoTo(new Vector3(15.9f, MoonLayout.Lower, -11.4f), Cat.TrotSpeed, MoonLayout.GuardPost);
            Talk(Guard.transform);
            // high in the hall's north-west corner, over the pair toward the guard (clear of every pillar line)
            Cam.Shot(new Vector3(14.2f, MoonLayout.Lower + 3.2f, -9.2f), new Vector3(16.5f, MoonLayout.Lower + 0.7f, -15.2f), 1.2f, false, 58f);
            yield return WalkSeven(new Vector3(16.6f, MoonLayout.Lower, -10.7f), 3f);
            Seven.Lock(true);
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(MoonLayout.GuardPost - Seven.transform.position)), 360f * Time.deltaTime); yield return null; }
            yield return ReadyToTalk();
            yield return Dlg.PlayScene(5, 1, 3);
            yield return Cutscene(false);
            Objective("Get past the Scree guard without hurting anyone");
            UI.Hint("Knock the loose stone off the broken pillar to draw the guard away.", 6f);
            _inHall = true;
            StartCoroutine(Watch());
            Prompt("Telekinesis: loose stone", MoonLayout.StoneRest, MoonLayout.TkRange, true);
            StartCoroutine(StoneLoop());
            if (Auto)
            {
                yield return GuardView("s05_checkonly_scree_guard_idle", Guard, 3.2f);
                ScreeFeet(Guard, "guard idle at the post");
                yield return AbdomenClearance(Guard, "guard idle", 2.5f);
                // Check: walk straight at the guard first and get spotted (must reset to the entrance, no harm).
                int tok = ++_walkToken;
                for (float t = 0f; t < 15f && _spotted == 0; t += Time.deltaTime)
                {
                    if (Seven.Current == ChapterSeven.Mode.Free) Seven.ScriptMove = Flat(new Vector3(16.5f, 0f, -16.5f) - Seven.transform.position).normalized;
                    yield return null;
                }
                Seven.ScriptMove = Vector3.zero;
                yield return Until(() => _spotted > 0, "spotted -> reset", 1f);
                yield return Wait(1.2f);
                yield return Until(() => _stoneDown, "stone knocked down", 30f);
                yield return Until(() => Guard.Current != ScreeGuard.Mode.Post, "guard hears the stone", 4f);
                if (Guard.Current == ScreeGuard.Mode.Alert)
                {
                    yield return Wait(0.35f);
                    yield return GuardView("s05_checkonly_scree_guard_alert", Guard, 3.0f);
                    ScreeFeet(Guard, "guard alert");
                }
                Cap.Log($"guard alert reactions played: {Guard.Alerts} (state {Guard.Current})");
                yield return Until(() => Guard.Current == ScreeGuard.Mode.ToNoise, "guard walks to the noise", 10f);
                yield return Wait(0.8f);
                yield return GuardView("s05_checkonly_scree_guard_walking", Guard, 3.4f);
                ScreeFeet(Guard, "guard walking");
                yield return AbdomenClearance(Guard, "guard walking", 2.5f);
                yield return Until(() => Guard.Current == ScreeGuard.Mode.Studying, "guard studies the noise", 30f);
                yield return Shot("s05_guard_distracted");
                yield return AutoWalk(MoonLayout.SneakPath);
            }
            yield return Until(() => Seven.transform.position.z < MoonLayout.HallExitZ, "slipped past the guard", 400f);
            _inHall = false;
            ClearPrompt();
            Seven.Sneak = false;
            // QF4: "Come on." as a short beat with both in view (it used to play over the follow camera, the cat behind it)
            yield return Cutscene(true);
            var fwd5 = Flat(Seven.transform.forward).sqrMagnitude > 0.01f ? Flat(Seven.transform.forward).normalized : Vector3.back;
            var side5 = Vector3.Cross(Vector3.up, fwd5);
            Cat.GoTo(Seven.transform.position + side5 * 0.7f + fwd5 * 0.2f, Cat.TrotSpeed, Seven.transform.position + fwd5 * 3f);
            Talk();
            Cam.Shot(Seven.transform.position + fwd5 * 3.0f + side5 * 1.2f + Vector3.up * 1.6f, Seven.transform.position + Vector3.up * 0.9f, 0.9f);
            yield return ReadyToTalk();
            yield return Dlg.PlayScene(5, 4, 4);
            yield return Cutscene(false);
            Checkpoint(Chapter1State.CpVision);
            if (Auto) Cap.Log($"Scree room: spotted {_spotted}x (each reset to the entrance), guard never attacks");
        }

        IEnumerator StoneLoop()
        {
            while (_inHall)
            {
                if (_promptLabel == null && !_stoneDown) Prompt("Telekinesis: loose stone", MoonLayout.StoneRest, MoonLayout.TkRange, true);
                if (Auto && !_stoneDown && !_resetting && _spotted > 0 && Seven.Current == ChapterSeven.Mode.Free && PromptNear) _promptHit = true;
                if (_promptHit && !_stoneDown && !_resetting)
                {
                    ClearPrompt();
                    Seven.Gesture(MoonLayout.StoneRest);
                    Seven.SetEnergyPoint(1.9f, 0.25f);         // Round 9: brightens while she uses it
                    while (Seven.Current == ChapterSeven.Mode.Gesture && !Seven.GesturePeak) yield return null;
                    if (Seven.GesturePeak)
                    {
                        TkStream(MoonLayout.StoneRest);
                        yield return FallStone();
                    }
                    Seven.SetEnergyPoint(1f, 1.2f);
                }
                yield return null;
            }
        }

        IEnumerator FallStone()
        {
            _stoneDown = true;
            AudioKit.Play("stone_fall", MoonLayout.StoneRest, 1f, 0.05f, AudioKit.Bus.Sfx, 30f);
            var a = MoonLayout.StoneRest; var b = MoonLayout.StoneLand;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.75f)
            {
                var p = Vector3.Lerp(a, b, t); p.y = Mathf.Lerp(a.y, b.y, t * t) + Mathf.Sin(t * Mathf.PI) * 0.4f;
                Stone.position = p; Stone.Rotate(Vector3.forward, -420f * Time.deltaTime, Space.World);
                yield return null;
            }
            Stone.position = b;
            Burst(b, 70, 1.0f);
            Guard.Hear(b);
            if (Auto) Cap.Log("stone falls: noise at " + b);
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { Stone.position += new Vector3(-0.6f, 0f, 0.15f) * Time.deltaTime * (1f - t / 0.6f); Stone.Rotate(Vector3.forward, -200f * Time.deltaTime, Space.World); yield return null; }
        }

        IEnumerator Watch()
        {
            while (_inHall)
            {
                Seven.Sneak = Seven.transform.position.z < MoonLayout.HallEnterZ + 0.3f;
                if (Seven.Current == ChapterSeven.Mode.Free && Guard.Sees(Seven.transform.position)) { yield return Spotted(); }
                yield return null;
            }
        }

        IEnumerator Spotted()
        {
            _spotted++;
            _resetting = true;
            Seven.Release(); Seven.Lock(true);
            if (Auto) { Cap.Log($"spotted by the guard at {Seven.transform.position} -> reset to the hall entrance"); yield return Shot("s05_spotted"); }
            Guard.Stand();
            UI.Hint("Spotted! Back to the entrance — try a distraction.", 3.5f);
            yield return UI.Fade(1f, 0.6f);
            Seven.Place(MoonLayout.HallEntry, Vector3.back);
            Seven.Lock(true);
            Cat.Place(MoonLayout.HallEntry + new Vector3(0.8f, 0f, 0.6f), Vector3.back);
            Cat.Follow();
            Guard.ResetToPost();
            Stone.position = MoonLayout.StoneRest; Stone.rotation = Quaternion.identity; _stoneDown = false;
            Prompt("Telekinesis: loose stone", MoonLayout.StoneRest, MoonLayout.TkRange, true);
            Cam.SnapBehind();
            yield return UI.Fade(0f, 0.6f);
            Seven.Lock(false);
            _resetting = false;
            if (Auto) Seven.ScriptMove = Vector3.zero;
        }

        IEnumerator Scene6Vision()
        {
            // QF7 (Dan: touching the shape, walking to the lock and walking back to take it was silly): one step from the
            // start. The objective is "Take the shape" as soon as she is in the chamber; she walks up to it and reaches
            // for it (no prompt, no button: walking up is enough, at or past the spot), the vision plays as she touches
            // it, and when it ends she is holding it. Then the pocket, then the hidden lock (Scene6Door).
            Step("take the glowing shape (the vision plays as she touches it)", 4, MoonLayout.ShapeSpot);
            Objective("Take the shape");
            Cat.Follow();
            _catWatch = true; StartCoroutine(CatWatchesInChamber());
            if (Auto) yield return AutoWalk(new Vector3(16.5f, MoonLayout.Lower, -30.6f));
            yield return Until(() => Seven.transform.position.z < -30f, "vision chamber", 40f);
            if (Auto)
            {
                // the chamber before the vision: the lock is NOT visible (the collapse covers it)
                Cam.Shot(new Vector3(16.0f, MoonLayout.Lower + 1.7f, -33.4f), new Vector3(13.8f, MoonLayout.Lower + 1.2f, -37.7f), 0.01f, true, 50f);
                yield return null;
                yield return Shot("s06_chamber_before_lock_buried");
                Cam.Shot(new Vector3(19.55f, MoonLayout.Lower + 1.5f, -34.75f), MoonLayout.ShapeAt + Vector3.down * 0.15f, 0.01f, true, 40f);
                yield return null;
                yield return Shot("s06_shape_in_alcove");
                Cam.Follow();
                yield return AutoWalk(new Vector3(18.6f, MoonLayout.Lower, -33.6f), MoonLayout.ShapeSpot);
            }
            float t0 = Time.time;
            while (true)
            {
                var p = Seven.transform.position;
                bool free = Seven.Current == ChapterSeven.Mode.Free && !PauseMenu.IsOpen && !_resettingFall && !Dlg.Busy;
                // at or past the spot: within reach of the pedestal, or anywhere in the alcove mouth
                if (free && (Flat(p - MoonLayout.ShapeSpot).magnitude < 1.5f || p.x > MoonLayout.ShapeSpot.x - 0.2f && p.z < MoonLayout.AlcoveZ.y + 0.6f && p.z > MoonLayout.AlcoveZ.x - 0.6f)) break;
                yield return null;
            }
            _catWatch = false;
            if (Auto || DemoBoot.PlayerSim) Cap?.Log($"ok: walked up to the shape ({Time.time - t0:0.0}s) - no prompt, no button; cat {Cat.DistanceTo7:0.0} m from her (furthest in the chamber {_catChamberMax:0.0} m)");
            Objective(null);

            yield return Cutscene(true);
            Cam.Subjects.Clear();
            Cat.GoTo(MoonLayout.ShapeSpot + new Vector3(-0.9f, 0f, 1.0f), Cat.StrollSpeed, Seven.transform.position);
            if (Dist7(MoonLayout.ShapeSpot) > 0.15f) yield return WalkSeven(MoonLayout.ShapeSpot, 2.5f);
            Seven.Lock(true);
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(MoonLayout.ShapeAt - Seven.transform.position)), 400f * Time.deltaTime); yield return null; }
            // a medium side shot: her upper body reaching into the alcove, the shape in view (no face close-up)
            FrameHer("reach for the shape", MoonLayout.ShapeAt, 1.9f, 50f, 0.8f);
            // QF8: arm's reach (her shoulder ~0.55 m from the shape) - she used to reach from a metre away
            {
                var to = Flat(MoonLayout.ShapeAt - Seven.transform.position); float dd = to.magnitude;
                if (dd > 0.62f) yield return WalkSeven(Seven.transform.position + to.normalized * (dd - 0.55f), 1.5f);
                Seven.Lock(true);
                for (float t = 0f; t < 0.3f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(MoonLayout.ShapeAt - Seven.transform.position)), 500f * Time.deltaTime); yield return null; }
                if (Auto) Cap.Log($"touch: she reaches from {Dist7(MoonLayout.ShapeAt):0.00} m (horizontal) to the shape");
            }
            Seven.Touch(MoonLayout.ShapeAt);
            // QF8: her palm on the shape (it floated beside her hand): the wrist placed so the palm meets its near face
            var tik = Seven.gameObject.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
            var toShape = Flat(MoonLayout.ShapeAt - Seven.transform.position).normalized;
            tik.RightPos = MoonLayout.ShapeAt - toShape * (MoonLayout.ShapeRadius + 0.075f) + Vector3.up * 0.015f;
            tik.RightW = 1f; tik.LookPos = MoonLayout.ShapeAt; tik.LookW = 0.8f; tik.Ease = 2.5f;
            yield return Wait(1.1f);
            if (Auto) { var rh = Seven.Bone(HumanBodyBones.RightMiddleProximal); Cap.Log($"touch: right hand knuckles {(rh != null ? Vector3.Distance(rh.position, MoonLayout.ShapeAt) - MoonLayout.ShapeRadius : -1f):0.000} m from the shape's surface (QF8: must be < 0.04)"); }
            yield return Shot("qf7_01_reaches_for_the_shape");
            // her fingers on it: the edges flare, the light swells to white -> the vision
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { _shapeGlow = Mathf.Lerp(ShapeGlowIdle, 4f, t / 0.5f); yield return null; }
            yield return Shot("s06_shape_touch_flare");
            // Wash to white; the vision (Dan, 2026-11-17): the video of this doorway long ago - the lock beside it intact,
            // being used - full-screen; the cat is heard faintly, far away. The music fades out (Round 13 0g: suspended, it
            // comes back where it left off when the vision ends).
            MusicSystem.Suspend(1.2f);
            AudioKit.Play("vision_whoosh", null, 0.9f);
            var shimmer = AudioKit.Loop("vision_shimmer", null, null, 0f, AudioKit.Bus.Sfx);
            if (shimmer != null) StartCoroutine(AudioKit.Fade(shimmer, 0.7f, 1.5f));
            if (_amb != null) StartCoroutine(AudioKit.Fade(_amb, 0.25f, 1.0f));
            _shapeGlow = 5f;
            StartCoroutine(UI.Fade(1f, 0.9f, 1f));
            yield return Wait(0.5f);
            yield return Shot("s06_whitewash_in");
            yield return Wait(0.45f);
            Show(Cat.gameObject, false);
            float visionStart = Time.time;
            bool video = _vp != null && _vp.isPrepared;
            if (Auto) Cap.Log(video ? $"vision: video {VisionVideo.name} ({VisionVideo.length:0.0}s, {VisionVideo.width}x{VisionVideo.height}) full-screen, sides cropped to the screen aspect" : "vision: VIDEO NOT READY - in-engine fallback");
            StartCoroutine(Dlg.PlayLines(Dlg.Bank.Scene(6, 1, 2), true));   // the cat's two lines, faint
            Coroutine ghostRoutine = null;
            if (video)
            {
                _vp.time = 0; _vp.Play();
                float aspect = VisionVideo.width / (float)Mathf.Max(1u, VisionVideo.height);
                for (float t = 0f; t < 0.7f; t += Time.deltaTime) { UI.SetVideo(_vpRT, aspect, t / 0.7f); yield return null; }
                UI.SetVideo(_vpRT, aspect, 1f);
                bool frameShot = false;
                float len = (float)VisionVideo.length;
                for (float vt0 = Time.time; Time.time - vt0 < len - 0.75f && (_vp.isPlaying || Time.time - vt0 < 1f);)
                {
                    if (!frameShot && Time.time - vt0 > 5.5f) { frameShot = true; yield return Shot("s06_vision_video_frame"); }
                    yield return null;
                }
                for (float t = 0f; t < 0.7f; t += Time.deltaTime) { UI.SetVideo(_vpRT, aspect, 1f - t / 0.7f); yield return null; }
                UI.SetVideo(null, aspect, 0f);
                _vp.Stop();
            }
            else
            {
                VisionRuins.SetActive(false); VisionWhole.SetActive(true); ShowRubble(false);
                Cam.Shot(new Vector3(16.5f, MoonLayout.Lower + 2.3f, -30.2f), new Vector3(16.5f, MoonLayout.Lower + 1.6f, -37.5f), 4f, true);
                ghostRoutine = StartCoroutine(Ghosts());
                yield return UI.Fade(0.12f, 1.1f, 1f);
                yield return Wait(3.2f);
                yield return Shot("s06_vision_during");
                yield return Until(() => !Dlg.Busy && Time.time - visionStart > 7.6f, "vision length", 20f);
                yield return UI.Fade(1f, 0.8f, 1f);
            }
            yield return Until(() => !Dlg.Busy, "vision lines", 10f);
            AudioKit.Play("vision_whoosh", null, 0.6f);
            if (shimmer != null) StartCoroutine(AudioKit.Fade(shimmer, 0f, 1.6f, true));
            if (_amb != null) StartCoroutine(AudioKit.Fade(_amb, 1f, 2.5f));
            MusicSystem.Resume(3.5f);                                  // Round 13 (0g): the level's track returns
            if (Auto) Cap.Log($"music after the vision (0g): '{MusicSystem.Current ?? "(none)"}' playing again");
            if (ghostRoutine != null) StopCoroutine(ghostRoutine);
            VisionWhole.SetActive(false); VisionRuins.SetActive(true); ShowRubble(true);
            Show(Cat.gameObject, true);
            // ... and when it ends she is holding it: she took it in that moment (still under the white)
            _shapeTaken = true;
            if (ShapeLight != null) ShapeLight.enabled = false;
            Seven.Release(); Seven.Lock(true);
            _carry = CarriedShape.Attach(Seven.gameObject, Shape);
            var ik = Seven.gameObject.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
            var head = Seven.Bone(HumanBodyBones.Head);
            var fwd = Flat(Seven.transform.forward).normalized; var side = Vector3.Cross(Vector3.up, fwd);
            var hold = (head != null ? head.position : Seven.transform.position + Vector3.up * 1.6f) + Vector3.down * 0.42f + fwd * 0.3f + side * 0.05f;
            ik.RightPos = hold; ik.RightW = 1f; ik.LookPos = hold; ik.LookW = 0.85f; ik.Ease = 6f;
            yield return null; yield return null;
            _carry.Set(CarriedShape.Where.Hand, 0f);
            Talk();
            // medium two-shot: 7 at the alcove with it in her hand, the cat beside her
            FrameHer("after the vision, holding it", Shape.position, 1.6f, 50f, 0f);
            if (Auto) Cap.Log($"vision lasted {Time.time - visionStart:0.0}s (white wash in/out included); after it she holds the dodecahedron: {_carry.At}, {Vector3.Distance(Shape.position, _carry.Palm):0.000} m from her palm");
            if (Auto) StartCoroutine(LogGrip("holding it after the vision", 1.2f));
            StartCoroutine(UI.Fade(0f, 1.2f, 1f));
            yield return Wait(0.75f);
            yield return Shot("s06_whitewash_out");
            yield return Wait(0.5f);
            yield return Shot("qf7_02_after_the_vision_holding_it");
            yield return ReadyToTalk(2f);
            var probe = DemoBoot.CaptureDir != null && Cap != null ? BodyProbe.Begin(Seven.gameObject, "Chapter 1, after the first vision (lines 6.3-6.7)") : null;
            if (probe != null) StartCoroutine(ShotWhileTalking("qf12_01_after_vision_talking", 2.5f));
            yield return Dlg.PlayScene(6, 3, 7);             // lines 3-7 (DIALOGUE.md scene 6)
            if (probe != null) Cap.Log(probe.Report());
            // she looks at it once more, then puts it away in a pocket of her suit: out of sight, no glow on her at all
            ik.LookPos = Shape.position; ik.LookW = 1f;
            FrameHer("into her pocket", null, 2.2f, 46f, 0.9f);
            yield return Wait(1.0f);
            var pocket = _carry.HipPoint + Seven.transform.forward * 0.04f;
            ik.RightPos = pocket; ik.LookPos = pocket; ik.LookW = 0.5f; ik.Ease = 3f;
            yield return Wait(0.55f);
            yield return LogGrip("hand at the pocket", 0.05f);
            _carry.Set(CarriedShape.Where.Pocket, 0.35f);
            yield return Wait(0.5f);
            yield return Shot("qf7_03_into_her_pocket");
            ik.RightW = 0f; ik.LookW = 0f;
            yield return Wait(0.5f);
            Destroy(ik);
            if (Auto) Cap.Log($"dodecahedron into her pocket: in pocket {_carry.InPocket}, renderers shown {Shape.GetComponentInChildren<Renderer>().enabled}, lights on her {CountLightsOn(Seven.transform)}");
            Checkpoint(Chapter1State.CpDoor);
        }

        /// <summary>QF9 item 1 (Dan: door frames flicker black / grey as 7 / the camera move): measure it in motion. 7 walks
        /// through the lunar-door chamber (door shut, as Dan saw it) and past the gate in the cell block while the follow
        /// camera orbits and pulls in as in play. Every frame is rendered to a texture and read back; each door-frame mesh's
        /// brightness is sampled at its own visible surface points (depth-tested by raycast). Per sample point visible in
        /// two consecutive frames the brightness change is taken; a frame where >= 30 % of a frame's points jump by > 0.2
        /// (0..1) is a flicker event (the orbit moves the view < 1 deg per frame, so real lighting cannot do that).
        /// Logs per mesh: frames, mean / max change, flicker events (must be 0), and whether it was ever faded.</summary>
        Vector3 _flPrevPos; Quaternion _flPrevRot = Quaternion.identity;
        readonly Dictionary<Renderer, Vector2> _noise = new Dictionary<Renderer, Vector2>();
        byte[] _prevFrame;
        IEnumerator FlickerTestRun()
        {
            {
                var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
                if (urp != null && !CameraOccluders.QF9Fix) { urp.shadowDepthBias = 0.1f; urp.shadowNormalBias = 0.5f; urp.mainLightShadowmapResolution = 2048; }
                if (urp != null) Cap.Log($"flicker test: shadows depth bias {urp.shadowDepthBias}, normal bias {urp.shadowNormalBias}, main light shadow map {urp.mainLightShadowmapResolution} ({(CameraOccluders.QF9Fix ? "QF9" : "0.10.5 values")})");
            }
            Cam.AutoBehind = false; Seven.ScriptDriven = true; Seven.Lock(false);
            Cat.gameObject.SetActive(false);
            UI.SetFade(1f); UI.SetObjective(null);
            var cam = Cam.GetComponent<Camera>();
            const int W = 640, H = 360;
            var rt = new RenderTexture(W, H, 24); var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            var targets = new List<(Renderer r, Vector3[] pts)>();
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var n = r.name;
                if (n.Contains("Glow") || n.Contains("Engrave") || n.Contains("Seam")) continue;
                if (!(n.Contains("DoorFrame") || n.Contains("DoorRim") || n.Contains("Leaf") || n.Contains("Gate_") || n.Contains("Frame") || n.Contains("Jamb")
                      || n.Contains("Rib") || n.Contains("Column") || n.Contains("Arch") || n.Contains("Pillar")
                      || n.Contains("Exit_") || n.Contains("Wall") || n.Contains("Passage") || n.Contains("Ceiling"))) continue;
                if (!r.gameObject.activeInHierarchy) continue;
                var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable) continue;
                // points INSIDE the surface (random spots on triangles, area-weighted) - never corners / silhouette edges
                var v = mf.sharedMesh.vertices; var tr = mf.sharedMesh.triangles; var m = r.transform.localToWorldMatrix;
                var rnd = new System.Random(r.name.GetHashCode()); var pts = new List<Vector3>();
                float total = 0f; var areas = new float[tr.Length / 3];
                for (int i = 0; i < areas.Length; i++) { var a0 = m.MultiplyPoint3x4(v[tr[i * 3]]); var a1 = m.MultiplyPoint3x4(v[tr[i * 3 + 1]]); var a2 = m.MultiplyPoint3x4(v[tr[i * 3 + 2]]); areas[i] = Vector3.Cross(a1 - a0, a2 - a0).magnitude * 0.5f; total += areas[i]; }
                for (int k = 0; k < 80 && total > 0f; k++)
                {
                    float pick = (float)rnd.NextDouble() * total; int ti = 0;
                    for (; ti < areas.Length - 1 && pick > areas[ti]; ti++) pick -= areas[ti];
                    var a0 = m.MultiplyPoint3x4(v[tr[ti * 3]]); var a1 = m.MultiplyPoint3x4(v[tr[ti * 3 + 1]]); var a2 = m.MultiplyPoint3x4(v[tr[ti * 3 + 2]]);
                    float u = 0.2f + 0.6f * (float)rnd.NextDouble(), w = (1f - u) * (0.25f + 0.5f * (float)rnd.NextDouble());
                    var nrm = Vector3.Cross(a1 - a0, a2 - a0).normalized;
                    pts.Add(a0 * (1f - u - w) + a1 * u + a2 * w + nrm * 0.01f);
                }
                targets.Add((r, pts.ToArray()));
            }
            // z-fighting check: two blocks whose faces lie in the same plane, face the same way and overlap
            {
                var boxes = new List<(string n, Bounds b)>();
                foreach (var mf in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                {
                    var mr = mf.GetComponent<MeshRenderer>();
                    if (mr == null || !mr.enabled || !mf.gameObject.activeInHierarchy || mf.sharedMesh == null || !mf.sharedMesh.name.StartsWith("Cube")) continue;
                    if (Quaternion.Angle(mf.transform.rotation, Quaternion.identity) > 0.01f) continue;
                    if (mr.sharedMaterial != null && mr.sharedMaterial.renderQueue > 2500) continue;
                    boxes.Add((mf.name, mr.bounds));
                }
                int found = 0;
                for (int i = 0; i < boxes.Count; i++)
                    for (int j = i + 1; j < boxes.Count; j++)
                        for (int ax = 0; ax < 3; ax++)
                            foreach (var side in new[] { -1, 1 })
                            {
                                var A = boxes[i].b; var B = boxes[j].b;
                                float fa = side < 0 ? A.min[ax] : A.max[ax], fb = side < 0 ? B.min[ax] : B.max[ax];
                                if (Mathf.Abs(fa - fb) > 0.002f) continue;
                                int u = (ax + 1) % 3, v = (ax + 2) % 3;
                                float ou = Mathf.Min(A.max[u], B.max[u]) - Mathf.Max(A.min[u], B.min[u]);
                                float ov = Mathf.Min(A.max[v], B.max[v]) - Mathf.Max(A.min[v], B.min[v]);
                                if (ou <= 0.01f || ov <= 0.01f) continue;
                                found++;
                                if (found <= 60) Cap.Log($"z-fight: {boxes[i].n} & {boxes[j].n}: {(side < 0 ? "-" : "+")}{"xyz"[ax]} faces both at {fa:0.000}, overlap {ou:0.00} x {ov:0.00} m");
                            }
                Cap.Log($"Z-FIGHT CHECK: {found} coplanar same-facing overlapping block faces (must be 0)");
            }
            Cap.Log($"flicker test (QF9): {targets.Count} door-frame meshes: {string.Join(", ", targets.ConvertAll(x => x.r.name))}");
            var stats = new Dictionary<Renderer, (int frames, float sum, float max, int events, int faded)>();
            foreach (var t in targets) stats[t.r] = (0, 0f, 0f, 0, 0);
            var prev = new Dictionary<Renderer, float[]>();
            float worstJump = 0f; int worstFrame = -1; string worstName = null;
            int frameNo = 0;
            float Lum(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

            IEnumerator Sample(string leg)
            {
                yield return new WaitForEndOfFrame();
                var old = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = old;
                var a = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0, false); tex.Apply(false); RenderTexture.active = a;
                frameNo++;
                var cp = cam.transform.position; var cr = cam.transform.rotation;
                // a jump only counts as flicker while the view is (nearly) still: < 2 deg and < 6 cm this frame
                bool still = (cp - _flPrevPos).magnitude < 0.06f && Quaternion.Angle(cr, _flPrevRot) < 2f;
                _flPrevPos = cp; _flPrevRot = cr;
                foreach (var (r, pts) in targets)
                {
                    if (r == null || !r.isVisible) { prev.Remove(r); continue; }
                    var cur = new float[pts.Length];
                    for (int i = 0; i < pts.Length; i++)
                    {
                        cur[i] = -1f;
                        var vp = cam.WorldToViewportPoint(pts[i]);
                        if (vp.z < cam.nearClipPlane || vp.x < 0.01f || vp.x > 0.99f || vp.y < 0.01f || vp.y > 0.99f) continue;
                        var d = pts[i] - cp; float len = d.magnitude;
                        if (Physics.Raycast(cp, d / len, len - 0.03f, ~((1 << 20) | (1 << 5) | (1 << 22)), QueryTriggerInteraction.Ignore)) continue;
                        if (CameraOccluders.Raycast(cp, d, len - 0.03f, out var oh, CameraOccluders.AllMask) && CameraOccluders.RendererOf(oh.collider) != r) continue;
                        // not on a silhouette edge: rays 3 pixels left / right / up / down of it must be clear too
                        {
                            float pxw = 2f * len * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / H * 3f;
                            bool edge = false;
                            foreach (var off in new[] { cam.transform.right, -cam.transform.right, cam.transform.up, -cam.transform.up })
                            {
                                var q = pts[i] + off * pxw; var dq = q - cp; float lq = dq.magnitude;
                                if (Physics.Raycast(cp, dq / lq, lq - 0.05f, ~((1 << 20) | (1 << 5) | (1 << 22)), QueryTriggerInteraction.Ignore)
                                    || CameraOccluders.Raycast(cp, dq, lq - 0.05f, out var oq, CameraOccluders.AllMask) && CameraOccluders.RendererOf(oq.collider) != r) { edge = true; break; }
                            }
                            if (edge) continue;
                        }
                        int px = (int)(vp.x * W), py = (int)(vp.y * H);
                        cur[i] = Lum(tex.GetPixel(px, py));
                        // surface noise: neighbour-pixel contrast on the surface (shadow-acne stripes make a flat wall "busy")
                        _noise.TryGetValue(r, out var nz);
                        nz.x += (Mathf.Abs(cur[i] - Lum(tex.GetPixel(px + 1, py))) + Mathf.Abs(cur[i] - Lum(tex.GetPixel(px, py + 1)))) * 0.5f; nz.y += 1f;
                        _noise[r] = nz;
                    }
                    var st = stats[r];
                    bool faded = r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly || r.HasPropertyBlock();
                    if (faded) st.faded++;
                    if (prev.TryGetValue(r, out var p))
                    {
                        int both = 0, jumps = 0; float sum = 0f, mx = 0f;
                        for (int i = 0; i < pts.Length; i++)
                        {
                            if (cur[i] < 0f || p[i] < 0f) continue;
                            float dl = Mathf.Abs(cur[i] - p[i]); both++; sum += dl; mx = Mathf.Max(mx, dl);
                            if (dl > 0.2f) jumps++;
                        }
                        if (both >= 5)
                        {
                            st.frames++; st.sum += sum / both; st.max = Mathf.Max(st.max, mx);
                            if (still && jumps >= both * 0.3f && st.events < 2 && _prevFrame != null)
                            {
                                System.IO.File.WriteAllBytes(System.IO.Path.Combine(DemoBoot.CaptureDir, $"event_{r.name}_{frameNo:0000}_a.jpg"), _prevFrame);
                                System.IO.File.WriteAllBytes(System.IO.Path.Combine(DemoBoot.CaptureDir, $"event_{r.name}_{frameNo:0000}_b.png"), tex.EncodeToPNG());
                            }
                            if (still && jumps >= both * 0.3f) { st.events++; Cap.Log($"flicker test: EVENT {r.name} frame {frameNo} ({leg}): {jumps}/{both} points jumped > 0.2, camera {cp}, faded {faded}"); }
                            if (sum / both > worstJump) { worstJump = sum / both; worstFrame = frameNo; worstName = r.name; }
                        }
                    }
                    stats[r] = st;
                    prev[r] = cur;
                }
                _prevFrame = tex.EncodeToJPG(85);
                if (frameNo % (leg == "vision_exit_doorway" ? 20 : 60) == 1 || frameNo <= 3)
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(DemoBoot.CaptureDir, $"flicker_{leg}_{frameNo:000}.png"), tex.EncodeToPNG());
            }

            IEnumerator Leg(string leg, Vector3 start, Vector3[] path, float yawSpeed, float seconds)
            {
                Seven.Place(start, Flat(path[0] - start)); Cam.Yaw = Seven.transform.eulerAngles.y; Cam.Pitch = 15f; Cam.SnapTo();
                prev.Clear();
                UI.SetFade(0f);
                int k = 0;
                for (float t = 0f; t < seconds; t += Time.deltaTime)
                {
                    var to = path[k % path.Length];
                    if (Dist7(to) < 0.4f) k++;
                    Seven.ScriptMove = Flat(to - Seven.transform.position).normalized * 0.6f;
                    Cam.Yaw += yawSpeed * Time.deltaTime;
                    Cam.Pitch = 15f + 12f * Mathf.Sin(t * 0.7f);
                    yield return Sample(leg);
                }
                Seven.ScriptMove = Vector3.zero;
            }

            float L0 = MoonLayout.Lower;
            yield return Leg("lunar_door", new Vector3(16.5f, L0, -31.0f),
                new[] { new Vector3(16.5f, L0, -36.6f), new Vector3(14.6f, L0, -36.0f), new Vector3(18.4f, L0, -36.0f), new Vector3(16.5f, L0, -33.0f) }, 28f, 14f);
            // Dan's doorway (2026-11-18 screenshots): the box doorway out of the vision chamber, hatched shadow stripes
            yield return Leg("vision_exit_doorway", new Vector3(16.5f, L0, -35.8f),
                new[] { new Vector3(16.5f, L0, -38.9f), new Vector3(16.0f, L0, -37.4f), new Vector3(17.0f, L0, -38.9f), new Vector3(16.6f, L0, -36.6f) }, 22f, 16f);
            yield return Leg("chamber_corners", new Vector3(18.6f, L0, -33.6f),
                new[] { new Vector3(19.4f, L0, -35.2f), new Vector3(18.6f, L0, -37.3f), new Vector3(14.6f, L0, -37.3f), new Vector3(13.2f, L0, -34.0f), new Vector3(13.4f, L0, -30.6f) }, 40f, 16f);
            yield return Leg("cell_block_gate", new Vector3(21.5f, 0f, 3.4f),
                new[] { new Vector3(25.6f, 0f, 3.4f), new Vector3(24.0f, 0f, 2.6f), new Vector3(24.0f, 0f, 4.2f) }, -30f, 8f);
            int events = 0, fadedFrames = 0; float noiseSum = 0f, noiseN = 0f;
            foreach (var kv in stats)
            {
                var st = kv.Value; events += st.events; fadedFrames += st.faded;
                _noise.TryGetValue(kv.Key, out var nz);
                if (st.frames > 0) Cap.Log($"flicker test: {kv.Key.name}: {st.frames} frame pairs, mean change {st.sum / st.frames:0.000}, largest point change {st.max:0.00}, flicker events {st.events}, frames faded {st.faded}, surface noise {(nz.y > 0 ? nz.x / nz.y : 0f):0.0000}");
                if (nz.y > 0) { noiseSum += nz.x; noiseN += nz.y; }
            }
            Cap.Log($"FLICKER TEST: {frameNo} consecutive frames, door-frame flicker events {events} (must be 0), frames with a door frame faded {fadedFrames} (must be 0); mean surface noise {(noiseN > 0 ? noiseSum / noiseN : 0f):0.0000}; largest mean frame-to-frame change {worstJump:0.000} ({worstName}, frame {worstFrame})");
            Cap.Finish("flicker test");
        }

        /// <summary>QF8 item 1 check: the follow camera over the whole traversal route. 7 is put at points along every
        /// walkable zone of the climbing stretch (prison catwalk to the scaffolds, 2 m apart), at every climb-down edge and
        /// into a hang on every ledge (every 0.7 m along it); at each point the camera starts from four directions (behind
        /// her, both sides, in front), is left alone for 0.9 s (so it can pull in / swing as it would after the player lets
        /// go of the stick) and her visible share is measured (17 body points, in frame and unobstructed). Worst points
        /// and Dan's spot (the first "Climb down") are captured.</summary>
        IEnumerator CamSweepRun()
        {
            Cam.AutoBehind = false; Seven.ScriptDriven = true; Seven.Lock(false);
            Cat.gameObject.SetActive(false);
            UI.SetFade(0f);
            var pts = new List<(Vector3 p, Vector3 face, MoonTraversal.Ledge hang, float u, string what)>();
            foreach (var z in Ground.Zones)
            {
                if (!z.Enabled) continue;
                float cx = (z.Min.x + z.Max.x) * 0.5f, cz = (z.Min.y + z.Max.y) * 0.5f;
                if (cx < 4f || cx > 24f || cz > 5.5f || cz < -66f) continue;                  // the traversal stretch
                for (float x = z.Min.x + 0.45f; x <= z.Max.x - 0.45f + 1e-3f; x += 2.5f)
                    for (float zz = z.Min.y + 0.45f; zz <= z.Max.y - 0.45f + 1e-3f; zz += 2.5f)
                        pts.Add((new Vector3(x, z.HeightAt(x, zz), zz), Vector3.back, null, 0f, z.Name));
            }
            foreach (var h in MoonTraversal.HangDowns) pts.Add((h.Spot, h.Ledge.Fwd * -1f, null, 0f, "edge " + h.Name));
            foreach (var L in MoonTraversal.Ledges)
                for (float u = 0.3f; u <= L.Length - 0.2f; u += 0.7f) pts.Add((Vector3.zero, L.Fwd, L, u, "hang " + L.Name));
            Cap.Log($"camera sweep (QF8): {pts.Count} points x 4 camera directions");
            var worst = new List<(float v, int i, float yaw)>();
            int under = 0, n = 0; float sum = 0f, min = 1f;
            for (int i = 0; i < pts.Count; i++)
            {
                var pt = pts[i];
                if (pt.hang != null) Seven.DebugHang(pt.hang, pt.u); else { Seven.Place(pt.p, pt.face); Seven.transform.position = pt.p; }
                yield return null;
                float yaw0 = Seven.transform.eulerAngles.y;
                foreach (var dy in new[] { 0f, 90f, -90f, 180f })
                {
                    Cam.Yaw = yaw0 + dy; Cam.Pitch = Cam.DefaultPitch;
                    Cam.SnapTo();
                    for (float t = 0f; t < 0.9f; t += Time.deltaTime) yield return null;
                    int mask = Seven.Current == ChapterSeven.Mode.Hang ? ChapterCamera.CamMaskPublic & ~(1 << 21) : ChapterCamera.CamMaskPublic;
                    float v = Cam.VisibleFraction(Cam.transform.position, mask);
                    n++; sum += v; min = Mathf.Min(min, v);
                    if (v < 0.6f) { under++; Cap.Log($"camera sweep: UNDER 60 % - {v * 100f:0} % at {pt.what} {(pt.hang != null ? "u " + pt.u.ToString("0.0") : pt.p.ToString())}, camera start {dy:0} deg, camera {Cam.transform.position}"); }
                    worst.Add((v, i, dy));
                }
            }
            worst.Sort((a, b) => a.v.CompareTo(b.v));
            for (int k = 0; k < Mathf.Min(4, worst.Count); k++)
            {
                var (v, i, dy) = worst[k]; var pt = pts[i];
                if (pt.hang != null) Seven.DebugHang(pt.hang, pt.u); else { Seven.Place(pt.p, pt.face); Seven.transform.position = pt.p; }
                yield return null;
                Cam.Yaw = Seven.transform.eulerAngles.y + dy; Cam.Pitch = Cam.DefaultPitch; Cam.SnapTo();
                for (float t = 0f; t < 0.9f; t += Time.deltaTime) yield return null;
                yield return Cap.Shot($"camsweep_worst{k + 1}");
                Cap.Log($"camsweep_worst{k + 1}: {v * 100f:0} % at {pt.what} {(pt.hang != null ? "u " + pt.u.ToString("0.0") : pt.p.ToString())}, camera start {dy:0} deg");
            }
            // Dan's spot: the first "Climb down" (the shelf edge), camera from behind and from each side
            var se = MoonTraversal.HangDowns[0];
            foreach (var dy in new[] { 0f, 90f, -90f })
            {
                Seven.Place(se.Spot, se.Ledge.Fwd); Seven.transform.position = se.Spot; yield return null;
                Cam.Yaw = Seven.transform.eulerAngles.y + dy; Cam.Pitch = Cam.DefaultPitch; Cam.SnapTo();
                for (float t = 0f; t < 0.9f; t += Time.deltaTime) yield return null;
                yield return Cap.Shot($"camsweep_first_climb_down_{(dy == 0f ? "behind" : dy > 0f ? "right" : "left")}");
                Cap.Log($"camsweep first climb-down spot, camera start {dy:0} deg: {Cam.VisibleFraction(Cam.transform.position) * 100f:0} % of 7 visible");
            }
            Cap.Log($"CAMERA SWEEP: {n} views, mean {sum / Mathf.Max(1, n) * 100f:0} %, worst {min * 100f:0} %, under 60 %: {under} (must be 0)");
            Cap.Log(ChapterCamera.Summary());
            Cap.Finish("camera sweep");
        }

        /// <summary>QF8 (Dan: the cat wandered off the other way while she went for the shape): in the vision chamber he
        /// stays near her - when she is more than 2.6 m away he trots to a spot 1.1 m beside her (on the chamber side, a
        /// little behind), then sits there watching her, or the shape when she is close to it. Never away from her.</summary>
        bool _catWatch; float _catChamberMax;
        IEnumerator CatWatchesInChamber()
        {
            bool going = false; float next = 0f;
            while (_catWatch)
            {
                yield return null;
                if (_cut || _resettingFall || PauseMenu.IsOpen) { going = false; continue; }
                var p7 = Seven.transform.position;
                if (PlayerArea != 4) continue;                        // until she is in the chamber: plain follow
                float d = Cat.DistanceTo7;
                if (Seven.transform.position.z < -30.5f) _catChamberMax = Mathf.Max(_catChamberMax, d);
                bool nearShape = Dist7(MoonLayout.ShapeSpot) < 3.2f;
                if (d > 2.6f && Time.time >= next)
                {
                    next = Time.time + 0.6f;
                    var fwd = Flat(Seven.transform.forward).sqrMagnitude > 0.01f ? Flat(Seven.transform.forward).normalized : Vector3.back;
                    var side = Vector3.Cross(Vector3.up, fwd);
                    if (side.x > 0f) side = -side;                    // the open chamber side (west), away from the alcove wall
                    var spot = p7 + side * 1.1f - fwd * 0.4f;
                    if (Flat(Ground.ClampToArena(spot, 0.2f) - spot).magnitude > 0.05f) spot = Ground.ClampToArena(p7 - fwd * 1.1f, 0.2f);
                    Cat.GoTo(spot, d > 5f ? Cat.SprintSpeed : Cat.TrotSpeed, nearShape ? MoonLayout.ShapeAt : p7);
                    going = true;
                }
                else if (going && Cat.Arrived)
                {
                    going = false;
                    Cat.Hold(nearShape ? MoonLayout.ShapeAt : p7);
                }
                else if (!going && Cat.Current == ChapterCat.Mode.Hold && d < 2.6f)
                    Cat.Hold(nearShape ? MoonLayout.ShapeAt : p7);    // keeps watching (her, or the shape once she is at it)
            }
            if (Cat.Current == ChapterCat.Mode.Hold || Cat.Current == ChapterCat.Mode.GoTo) Cat.Follow();
        }

        /// <summary>QF8: how well the shape sits in her palm (surface-to-palm error, m) - logged for the check run.</summary>
        IEnumerator LogGrip(string beat, float after)
        {
            yield return Wait(after);
            if (_carry != null && (Auto || DemoBoot.PlayerSim)) Cap?.Log($"grip '{beat}': shape {_carry.At}, palm contact error {_carry.GripError:0.000} m (QF8: must be < 0.02)");
        }

        /// <summary>QF7: a cutscene framing found around her instead of a fixed spot (the ruin columns blocked the fixed ones,
        /// and some framed her from behind): her head + chest and <paramref name="also"/> in clear view, the camera at
        /// about head height (never looking down her body), from her side or three-quarter front - never from behind - and
        /// never inside geometry. Medium distance, no face close-up.</summary>
        void FrameHer(string what, Vector3? also, float dist, float fov, float ease)
        {
            var head = Seven.Bone(HumanBodyBones.Head); var chest = Seven.Bone(HumanBodyBones.Chest);
            var hp = head != null ? head.position : Seven.transform.position + Vector3.up * 1.6f;
            var cp = chest != null ? chest.position : Seven.transform.position + Vector3.up * 1.25f;
            var fwd = Flat(Seven.transform.forward).normalized;
            var look = also.HasValue ? Vector3.Lerp(Vector3.Lerp(hp, cp, 0.3f), also.Value, 0.4f) : Vector3.Lerp(hp, cp, 0.3f);
            float best = float.MaxValue; Vector3 bestPos = Vector3.zero; float bestAng = 0f;
            int nSolid = 0, nLine = 0, nAlso = 0, nProp = 0, nBehind = 0;
            foreach (var d in new[] { dist, dist * 1.25f, dist * 1.6f, dist * 0.85f })
                for (int a = -150; a <= 150; a += 15)
                    foreach (var h in new[] { 0.05f, 0.2f, -0.05f })
                    {
                        var dir = Quaternion.AngleAxis(a, Vector3.up) * fwd;
                        var pos = Seven.transform.position + dir * d + Vector3.up * (hp.y - Seven.transform.position.y + h);
                        float ang = Mathf.Abs(a);
                        if (ang > 100f) { nBehind++; continue; }                  // never from behind her
                        if (Physics.CheckSphere(pos, 0.25f, ~((1 << 20) | (1 << 5)), QueryTriggerInteraction.Ignore)) { nSolid++; continue; }
                        if (!ChapterCamera.ClearLine(pos, hp) || !ChapterCamera.ClearLine(pos, cp)) { nLine++; continue; }
                        if (also.HasValue && !ChapterCamera.ClearLine(pos, also.Value)) { nAlso++; continue; }
                        if (Cam.PropBetween(pos, hp) || Cam.PropBetween(pos, cp) || also.HasValue && Cam.PropBetween(pos, also.Value)) { nProp++; continue; }
                        float score = Mathf.Abs(ang - 60f) * 0.02f + Mathf.Abs(d - dist) * 0.8f + Mathf.Abs(h - 0.05f) * 2f;
                        if (score < best) { best = score; bestPos = pos; bestAng = a; }
                    }
            if (best == float.MaxValue)
            {
                var why = $"framing '{what}': NO clear framing found around her - kept the follow view (rejected: inside geometry {nSolid}, line to her blocked {nLine}, line to the subject blocked {nAlso}, props/ribs {nProp}, behind her {nBehind})";
                Cap?.Log(why); Debug.Log("[Seven] " + why);
                return;
            }
            Cam.Shot(bestPos, look, ease, ease <= 0.05f, fov);
            Debug.Log($"[Seven] framing '{what}': camera {bestPos} at {bestAng:0} deg");
            if (Auto || DemoBoot.PlayerSim) Cap?.Log($"framing '{what}': camera {bestPos} at {bestAng:0} deg from her facing (0 = straight in front, > 100 never used), {Vector3.Distance(bestPos, hp):0.00} m from her head");
        }

        static int CountLightsOn(Transform t) { int n = 0; foreach (var l in t.GetComponentsInChildren<Light>()) if (l.enabled) n++; return n; }

        // ------------------------------------------------------------------------------------------ QF7: the buried lock
        readonly List<(Transform t, Vector3 p, Quaternion r)> _rubbleHome = new List<(Transform, Vector3, Quaternion)>();
        static readonly Vector2 RubbleObstacle = new Vector2(13.7f, -37.45f);
        Vector3 RubbleCentre => new Vector3(MoonLayout.DoorSymbol.x, MoonLayout.Lower + 1.0f, -37.6f);
        bool _lockUncovered;
        public bool LockUncovered => _lockUncovered;

        /// <summary>The collapse in front of the lock: back in place (present day, before she clears it) or gone.</summary>
        void ResetRubble(bool present)
        {
            if (LockRubble == null) return;
            if (_rubbleHome.Count == 0) foreach (Transform c in LockRubble) _rubbleHome.Add((c, c.position, c.rotation));
            foreach (var (t, p, r) in _rubbleHome) { t.position = p; t.rotation = r; }
            LockRubble.gameObject.SetActive(present);
            _lockUncovered = !present;
            Ground.Obstacles.RemoveAll(c => (c.C - RubbleObstacle).sqrMagnitude < 0.01f);
            if (present) Ground.Obstacles.Add(new Ground.Circle(RubbleObstacle, 0.75f));
        }
        void ShowRubble(bool on) { if (LockRubble != null && !_lockUncovered) LockRubble.gameObject.SetActive(on); }

        /// <summary>QF7: checkpoint 5. She knows from the vision where the lock is: behind the collapse. She clears it with
        /// telekinesis (RT), the lock is exposed, she touches it, the door opens; only then is the way on open.</summary>
        IEnumerator Scene6Door()
        {
            Step("uncover the lock (telekinesis), then open the door", 4, MoonLayout.MechanismSpot);
            yield return Cutscene(true);
            Seven.Release(); Seven.Lock(true);
            var toPile = Flat(RubbleCentre - Seven.transform.position);
            for (float t = 0f; t < 0.7f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(toPile), 260f * Time.deltaTime); yield return null; }
            Talk();
            // she looks across at the collapse where the lock was in the vision (a medium shot over her shoulder)
            FrameHer("she looks at the collapse", RubbleCentre, 2.2f, 54f, 1.0f);
            yield return ReadyToTalk(1.5f);
            yield return Shot("qf7_04_she_looks_at_the_collapse");
            yield return Dlg.PlayScene(6, 8, 8);             // "But I think I know how this opens."
            yield return Cutscene(false);
            Objective("Uncover the lock");
            Prompt("Telekinesis: the collapse", RubbleCentre, MoonLayout.TkRange, true);
            var rends = LockRubble != null ? LockRubble.GetComponentsInChildren<Renderer>() : new Renderer[0];
            bool pulsing = true;
            IEnumerator Pulse() { for (float t = 0f; pulsing; t += Time.deltaTime) { foreach (var r in rends) Glow(r, 0.06f + 0.06f * Mathf.Sin(t * 3f)); yield return null; } foreach (var r in rends) Glow(r, 0f); }
            StartCoroutine(Pulse());
            if (Auto) { yield return AutoWalk(new Vector3(15.6f, MoonLayout.Lower, -35.2f)); yield return Until(() => PromptNear, "the collapse in telekinesis range", 20f); }
            yield return WaitPrompt("clear the collapse from the lock (telekinesis)");
            pulsing = false;
            yield return Cutscene(true);
            Seven.Gesture(RubbleCentre);
            Seven.SetEnergyPoint(1.9f, 0.25f);
            for (float t = 0f; t < 2f && Seven.Current == ChapterSeven.Mode.Gesture && !Seven.GesturePeak; t += Time.deltaTime) yield return null;
            TkStream(RubbleCentre);
            AudioKit.Play("slab_lift", RubbleCentre, 0.9f, 0.05f, AudioKit.Bus.Sfx, 25f);
            Cam.Shot(new Vector3(16.6f, MoonLayout.Lower + 1.9f, -33.0f), RubbleCentre + Vector3.up * 0.3f, 0.7f, false, 52f);
            // each piece lifts off the wall and drifts to the west wall of the chamber, settling on the floor there
            var froms = new List<(Transform t, Vector3 p, Quaternion r, Vector3 to, float delay)>();
            int k = 0;
            foreach (var (t, p, r) in _rubbleHome)
            {
                var to = new Vector3(12.95f + 0.25f * (k % 3), MoonLayout.Lower + 0.15f + 0.1f * (k % 2), -33.2f - 0.55f * (k / 3));
                froms.Add((t, t.position, t.rotation, to, 0.12f * k)); k++;
            }
            bool shotMid = false;
            for (float t = 0f; t < 2.6f; t += Time.deltaTime)
            {
                foreach (var f in froms)
                {
                    float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - f.delay) / 1.4f));
                    f.t.position = Vector3.Lerp(f.p, f.to, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 1.1f;
                    f.t.rotation = f.r * Quaternion.Euler(u * 70f, u * 40f, u * -30f);
                }
                if (!shotMid && t > 0.9f) { shotMid = true; StartCoroutine(Shot("qf7_05_telekinesis_clears_the_collapse")); }
                yield return null;
            }
            AudioKit.Play("stone_fall", new Vector3(13.2f, MoonLayout.Lower, -34f), 0.7f, 0.05f, AudioKit.Bus.Sfx, 25f);
            Seven.SetEnergyPoint(1f, 1.2f);
            Seven.Release(); Seven.Lock(true);
            _lockUncovered = true;
            Ground.Obstacles.RemoveAll(c => (c.C - RubbleObstacle).sqrMagnitude < 0.01f);
            ClearPrompt();
            Glow(DoorSymbol, 0.9f);                          // the lock, intact - as in the vision
            Cam.Shot(new Vector3(15.4f, MoonLayout.Lower + 1.75f, -34.6f), MoonLayout.DoorSymbol + Vector3.down * 0.15f, 0.8f, false, 48f);
            yield return Wait(1.0f);
            yield return Shot("qf7_06_the_lock_uncovered");
            if (Auto) Cap.Log("the collapse is cleared by telekinesis: the lock is exposed (on the right of the door, as in the vision)");
            yield return Cutscene(false);
            Objective("Open the door");
            Prompt("Touch", MoonLayout.MechanismSpot, 1.0f);
            if (Auto) yield return AutoWalk(MoonLayout.MechanismSpot);
            yield return WaitPrompt("touch the lock");
            Objective(null);
            yield return Cutscene(true);
            Cat.GoTo(MoonLayout.MechanismSpot + new Vector3(1.3f, 0f, 1.2f), Cat.StrollSpeed, MoonLayout.SealedDoor);
            Cam.Subjects.Clear();          // no lines here: a fixed over-the-shoulder view of her hand on the mechanism
            Cam.Shot(new Vector3(15.0f, MoonLayout.Lower + 1.7f, -35.4f), new Vector3(13.7f, MoonLayout.Lower + 1.35f, -37.9f), 0.8f, false, 50f);   // QF5: mirrored with the mechanism
            if (Dist7(MoonLayout.MechanismSpot) > 0.1f) yield return WalkSeven(MoonLayout.MechanismSpot, 2.5f);
            Seven.Lock(true);
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Vector3.back), 400f * Time.deltaTime); yield return null; }
            Seven.Touch(MoonLayout.DoorSymbol);
            yield return Wait(1.0f);
            // she touches it the way she saw them do it: it lights, the engraved lines light along the S-seam, the leaves part
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { Glow(DoorSymbol, Mathf.Lerp(0.9f, 3.0f, t / 0.5f)); yield return null; }
            AudioKit.Play("energy_point", MoonLayout.DoorSymbol, 0.9f, 0f, AudioKit.Bus.Sfx, 20f);
            yield return Shot("s06_touching_mechanism");
            Cam.Shot(new Vector3(15.6f, MoonLayout.Lower + 1.9f, -33.4f), new Vector3(15.9f, MoonLayout.Lower + 1.8f, -38.1f), 0.8f, false, 54f);   // QF5: mirrored
            for (float t = 0f; t < 1.6f; t += Time.deltaTime)
            {
                float u = t / 1.6f;
                if (DoorEngrave != null && DoorEngrave.Length > 0) Glow(DoorEngrave[0], Mathf.Lerp(0f, 3.5f, Mathf.Clamp01(u * 1.6f)));             // the S-seam first
                if (DoorEngrave != null) for (int i = 1; i < DoorEngrave.Length; i++) Glow(DoorEngrave[i], Mathf.Lerp(0f, 1.8f, Mathf.Clamp01(u * 1.4f - 0.4f)));
                yield return null;
            }
            yield return Shot("s06_emblem_and_engravings_lit");
            if (Auto)
            {
                // the present day from where the vision looks at the door (the lock on the right, her hand on it)
                Cam.Shot(new Vector3(16.6f, MoonLayout.Lower + 1.6f, -31.0f), new Vector3(16.2f, MoonLayout.Lower + 2.0f, -38.0f), 0.01f, true, 62f);
                yield return null; yield return null;
                yield return Shot("s06_like_the_vision");
                Cam.Shot(new Vector3(15.6f, MoonLayout.Lower + 1.9f, -33.4f), new Vector3(15.9f, MoonLayout.Lower + 1.8f, -38.1f), 0.01f, true, 54f);
            }
            Seven.Release(); Seven.Lock(true);
            Burst(MoonLayout.SealedDoor + Vector3.up * 1.6f, 80, 1.3f);
            AudioKit.Play("stone_door", MoonLayout.SealedDoor + Vector3.up, 1f, 0f, AudioKit.Bus.Sfx, 25f);
            bool partShot = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 3.0f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                SealedDoorSlab.localPosition = new Vector3(-LeafOpen * u, 0f, 0f);
                if (DoorRight != null) DoorRight.localPosition = new Vector3(LeafOpen * u, 0f, 0f);
                if (!partShot && t > 0.35f) { partShot = true; StartCoroutine(Shot("s06_door_parting")); }
                yield return null;
            }
            if (DoorEngrave != null) foreach (var r in DoorEngrave) Glow(r, 0.6f);
            Glow(DoorSymbol, 0.7f);
            yield return Shot("s06_passage_opens");
            var faceCat = Flat(Cat.transform.position - Seven.transform.position);
            for (float t = 0f; t < 0.6f && faceCat.sqrMagnitude > 0.01f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(faceCat), 300f * Time.deltaTime); yield return null; }
            Talk();
            Cam.Shot(new Vector3(17.0f, MoonLayout.Lower + 1.8f, -34.0f), (Seven.transform.position + Cat.transform.position) * 0.5f + Vector3.up * 0.9f, 0.8f, false, 54f);
            yield return ReadyToTalk(1.5f);
            yield return Dlg.PlayScene(6, 9, 11);            // closing lines 9-11
            MusicSystem.Play("moon_prison", 4f);
            Objective("Keep going");
            yield return Cutscene(false);
            // QF6 (no points of no return): the way on (and its one-way drop) opens only now - she has the shape in her
            // pocket since the vision, and the lock is open.
            SetZone("VisionExit", true);
            Checkpoint(Chapter1State.CpCity);
        }
        CarriedShape _carry;

        /// <summary>QF7 check-run captures: the dodecahedron is out of sight in her pocket in play (terrace, bay) - no
        /// shape, no pouch, no light on her; logs it each time.</summary>
        IEnumerator HipShots()
        {
            bool Free() => Seven.Current == ChapterSeven.Mode.Free && !Cam.InShot && !Dlg.Busy;
            void Where(string what) { if (_carry != null && Shape != null) Cap.Log($"dodecahedron {what}: {_carry.At}, shape renderers shown {Shape.GetComponentInChildren<Renderer>().enabled}, lights on her {CountLightsOn(Seven.transform)} (pocket: must be False / 0)"); }
            while (!(Free() && Seven.transform.position.z < -45.0f && Seven.Speed > 0.3f)) yield return null;
            yield return Wait(0.6f);
            Where("on the terrace"); yield return Shot("qf7_07_pocket_terrace_no_glow");
            while (!(Free() && Seven.transform.position.y < MoonLayout.Bay + 0.5f)) yield return null;
            yield return Wait(0.6f);
            Where("on the bay floor");
            var f = Flat(Seven.transform.forward).normalized; var r = Vector3.Cross(Vector3.up, f);
            Cam.Shot(Seven.transform.position + r * 2.6f + f * 1.6f + Vector3.up * 1.5f, Seven.transform.position + Vector3.up * 0.95f, 0.01f, true, 52f);
            yield return null; yield return null;
            yield return Shot("qf7_08_full_figure_no_glow");
            Cam.Follow();
        }
        bool _shapeTaken;

        IEnumerator Ghosts()
        {
            var ghosts = VisionWhole.transform.Find("Ghosts");
            if (ghosts == null) yield break;
            var start = new List<Vector3>();
            foreach (Transform g in ghosts) start.Add(g.localPosition);
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime;
                int i = 0;
                foreach (Transform g in ghosts) { g.localPosition = start[i] + g.forward * Mathf.Sin(t * 0.35f + i) * 0.45f; i++; }
                yield return null;
            }
        }

        // ------------------------------------------------------------------------------------------ 6b: the city reveal
        /// <summary>Blend the Atmosphere to the city look: distant layers visible, haze bright in every direction
        /// (also up the curve), a broad unexplained glow high above, the basin buried in soft valley haze.</summary>
        IEnumerator CityLook(float seconds)
        {
            _cityLook = true;
            if (Atmo == null) yield break;
            var a = Atmo;
            float[] from = { a.FogStart, a.GroundDensity, a.GroundBase, a.GroundFalloff, a.GroundRange, a.ValleyDensity, a.ValleyBase, a.ValleyFalloff, a.DistanceDensity, a.HazeHeight, a.SkyDistance, a.NearBrightness, a.FarBrightness, a.BrightnessDistance, a.ZenithBrightness, a.GlowIntensity, a.GlowRadius, a.MaxFog };
            float[] to = { 8f, 0.010f, MoonLayout.Bay, 2.2f, 45f, 0.0026f, -200f, 120f, 0.00021f, 6000f, 7000f, 0.12f, 1.2f, 2200f, 1.0f, 0.38f, 34f, 0.97f };
            // the scene's shadowed key light swings overhead: one broad source above the city (pale tops, dark streets)
            Light key = null;
            foreach (Transform c in a.transform) if (c.name == "BackLight") key = c.GetComponent<Light>();
            var keyR0 = key != null ? key.transform.rotation : Quaternion.identity; float keyI0 = key != null ? key.intensity : 1f;
            var keyR1 = Quaternion.Euler(68f, 205f, 0f);
            // glow sources: one broad source far above the visible city + soft lights deep in the haze
            var glowPos = new[] { new Vector3(-200f, 1900f, -1600f), new Vector3(16f, -120f, -900f), new Vector3(-700f, 200f, -1700f), new Vector3(800f, 300f, -1500f),
                                  new Vector3(16f, 900f, -3600f), new Vector3(-1500f, 600f, -400f), new Vector3(1500f, 500f, -300f), new Vector3(16f, 2600f, 300f) };
            var glowScale = new[] { new Vector3(36f, 2.2f, 1f), new Vector3(6f, 0.7f, 1f), new Vector3(9f, 0.8f, 1f), new Vector3(9f, 0.8f, 1f),
                                    new Vector3(18f, 1.2f, 1f), new Vector3(12f, 0.8f, 1f), new Vector3(12f, 0.8f, 1f), new Vector3(24f, 1.0f, 1f) };
            var glows = new List<Transform>();
            foreach (Transform c in a.transform) if (c.name.StartsWith("Glow")) glows.Add(c);
            var gp0 = glows.ConvertAll(g => g.position); var gs0 = glows.ConvertAll(g => g.localScale);
            for (float t = seconds > 0f ? 0f : 1f; t <= 1f + 1e-4f; t += seconds > 0f ? Time.deltaTime / seconds : 2f)
            {
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                float L(int i) => Mathf.Lerp(from[i], to[i], u);
                a.FogStart = L(0); a.GroundDensity = L(1); a.GroundBase = L(2); a.GroundFalloff = L(3); a.GroundRange = L(4);
                a.ValleyDensity = L(5); a.ValleyBase = L(6); a.ValleyFalloff = L(7); a.DistanceDensity = Mathf.Exp(Mathf.Lerp(Mathf.Log(from[8]), Mathf.Log(to[8]), u));
                a.HazeHeight = Mathf.Exp(Mathf.Lerp(Mathf.Log(from[9]), Mathf.Log(to[9]), u)); a.SkyDistance = L(10);
                a.NearBrightness = L(11); a.FarBrightness = L(12); a.BrightnessDistance = L(13); a.ZenithBrightness = L(14);
                a.GlowIntensity = L(15); a.GlowRadius = L(16); a.MaxFog = L(17);
                for (int i = 0; i < glows.Count && i < glowPos.Length; i++) { glows[i].position = Vector3.Lerp(gp0[i], glowPos[i], u); glows[i].localScale = Vector3.Lerp(gs0[i], glowScale[i], u); }
                if (key != null) { key.transform.rotation = Quaternion.Slerp(keyR0, keyR1, u); key.intensity = Mathf.Lerp(keyI0, 1.35f, u); }
                if (seconds <= 0f) break;
                yield return null;
            }
            if (Auto) Cap.Log("atmosphere: city look applied");
        }

        IEnumerator LookAlong(Vector3 pos, Vector3 look0, Vector3 look1, float seconds, float fov)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Cam.Shot(pos, Vector3.Lerp(look0, look1, u), 0.02f, true, fov);
                yield return null;
            }
        }

        string RevealSet => DemoBoot.CityRevealDialogue != "primary" ? DemoBoot.CityRevealDialogue : CityRevealDialogue;
        IEnumerator RevealLines(int from, int to, int altFrom, int altTo)
        {
            if (RevealSet == "off") { yield return Wait(2.4f * (to - from + 1)); yield break; }
            if (RevealSet == "alternate") { if (altFrom > 0) yield return Dlg.PlayScene(66, altFrom, altTo); else yield return Wait(1.5f); yield break; }
            yield return Dlg.PlayScene(65, from, to);
        }

        // ------------------------------------------------------------------------------------------ QF9: the exit fight
        // Dan (2026-11-18): the two Scree on the terrace right before the city reveal become the fighting tutorial. They
        // block the way out; the controls are taught as they are needed (attack, block, dodge, takedown - first use only);
        // nonlethal always (a takedown leaves a Scree on the ground, then sitting up dazed, alive); if 7 is knocked down
        // the fight restarts (white fade, no game over); when both are down the city reveal plays. An invisible arena
        // (the terrace between the mining cut and the viewpoint) keeps the fight from being walked around or fallen out
        // of; a watchdog ends it (both sit down) if nothing has happened for 90 s, so it can never soft-lock.
        static readonly Vector3 FightStart = new Vector3(16.5f, MoonLayout.Lower, -42.0f);
        static readonly Vector3[] FightSpots = { new Vector3(15.6f, MoonLayout.Lower, -44.4f), new Vector3(17.5f, MoonLayout.Lower, -44.7f) };
        const float ArenaZMin = -45.3f, ArenaZMax = -41.45f, ArenaXMin = 13.6f, ArenaXMax = 19.6f;
        bool _fightWon, _fighting;
        ScreeGuard[] _fighters;
        int _fightResets; float _fightProgress;
        public bool FightWon => _fightWon;

        ScreeGuard[] FindExitPair()
        {
            if (Workers == null) return null;
            var want = new[] { new Vector3(12.6f, MoonLayout.Lower, -42.9f), new Vector3(11.8f, MoonLayout.Lower, -47.4f) };
            var pair = new ScreeGuard[2];
            for (int k = 0; k < 2; k++)
            {
                float best = 3f;
                foreach (var w in Workers) { if (w == null || w == pair[0]) continue; float d = Flat(w.Post - want[k]).magnitude; if (d < best) { best = d; pair[k] = w; } }
                if (pair[k] == null) return null;
            }
            return pair;
        }

        ScreeGuard EngagedScree()
        {
            ScreeGuard best = null; float bd = float.MaxValue;
            foreach (var s in _fighters) { if (s == null || s.IsOut) continue; float d = Flat(s.transform.position - Seven.transform.position).magnitude; if (d < bd) { bd = d; best = s; } }
            return best;
        }

        IEnumerator ArenaClamp()
        {
            while (_fighting)
            {
                var p = Seven.transform.position;
                var q = new Vector3(Mathf.Clamp(p.x, ArenaXMin, ArenaXMax), p.y, Mathf.Clamp(p.z, ArenaZMin, ArenaZMax));
                if ((q - p).sqrMagnitude > 1e-8f) { q.y = Ground.Height(q.x, q.z); Seven.transform.position = q; }
                yield return null;
            }
        }

        void OnScreeFight(ScreeGuard s, string what)
        {
            _fightProgress = Time.time;
            if (Auto || DemoBoot.PlayerSim) Cap?.Log($"fight: {s.name} {what}");
            if (what == "windup")
            {
                float tt = Time.time;
                if (!Chapter1State.Taught.Contains("fight_block")) { s.WindupSeconds = 1.3f; StartTeach("fight_block", () => _defendedBlock || Time.time - tt > 9f, (ControlsLegend.Btn.Block, "Block")); }
                else if (!Chapter1State.Taught.Contains("fight_dodge")) { s.WindupSeconds = 1.1f; StartTeach("fight_dodge", () => _defendedDodge || Time.time - tt > 9f, (ControlsLegend.Btn.Dodge, "Dodge")); }
                else s.WindupSeconds = 0.75f;
            }
            if (what == "defended") { if (Seven.Act == ChapterSeven.FightAct.Block) _defendedBlock = true; else _defendedDodge = true; }
            if (what == "dazed") StartTeach("fight_takedown", () => s.IsOut || !s.Dazed, (ControlsLegend.Btn.Takedown, "Takedown"));   // later dazes: the context prompt (ChapterSevenFight)
            if (what == "taken_down" && Auto) StartCoroutine(ShotLater("qf9_takedown", 0.45f));
            var sp = s.transform.position + Vector3.up;
            if (what == "windup" || what == "dazed") AudioKit.Play("scree_grunt", sp, 0.7f, 0.12f, AudioKit.Bus.Sfx, 16f);
            if (what == "defended") AudioKit.Play("fight_block", sp, 0.9f, 0.08f, AudioKit.Bus.Sfx, 16f);
            if (what == "taken_down") StartCoroutine(PlayLater("body_fall", sp, 0.55f));
        }
        bool _defendedBlock, _defendedDodge;
        IEnumerator PlayLater(string id, Vector3 at, float delay) { yield return Wait(delay); AudioKit.Play(id, at, 0.8f, 0.05f, AudioKit.Bus.Sfx, 16f); }

        IEnumerator ExitFight()
        {
            Step("get past the two Scree at the terrace exit (fighting tutorial)", 6, FightStart);
            _fighters = FindExitPair();
            if (_fighters == null) { Cap?.Log("fight: the two terrace Scree were not found - no fight, the reveal goes on"); _fightWon = true; yield break; }
            yield return Until(() => Seven.transform.position.z < -41.3f && Seven.transform.position.y > MoonLayout.Lower - 0.4f && Seven.Current == ChapterSeven.Mode.Free, "out of the mining cut onto the terrace", 400f);
            // they see her and come to block the way out (a short beat, both in view)
            yield return Cutscene(true);
            Seven.Lock(true);
            for (int i = 0; i < 2; i++) { _fighters[i].FightEvent -= OnScreeFight; _fighters[i].FightEvent += OnScreeFight; _fighters[i].BeginFight(Seven, FightSpots[i]); }
            Cat.GoTo(new Vector3(16.9f, MoonLayout.Lower, -41.6f), Cat.TrotSpeed, Seven.transform.position + Vector3.back * 3f);   // keeps his distance
            // from the open terrace, looking back at her (out of the cut) and the two Scree coming between her and the way out
            Cam.Shot(new Vector3(17.6f, MoonLayout.Lower + 1.9f, -47.6f), new Vector3(16.4f, MoonLayout.Lower + 1.0f, -42.8f), 1.0f, false, 52f);
            for (float t = 0f; t < 2.6f && (_fighters[0].FightState == ScreeGuard.Fight.Approach || _fighters[1].FightState == ScreeGuard.Fight.Approach); t += Time.deltaTime) yield return null;
            yield return Shot("qf9_scree_block_the_way");
            yield return Cutscene(false);
            Objective("Get past the Scree");
            Seven.FightTarget = EngagedScree;
            Seven.FightTargets = () => _fighters;            // QF10: soft lock on the nearest in front of her
            Seven.BeginFight();
            if (Auto || DemoBoot.PlayerSim) Cap?.Log(Seven.FightFeelTable());
            Seven.FightEvent -= OnSevenFight; Seven.FightEvent += OnSevenFight;
            _fighting = true; _fightProgress = Time.time; StartCoroutine(ArenaClamp());
            float t0 = Time.time;
            float ta = Time.time;
            StartTeach("fight_attack", () => Seven.HitsLanded >= 2 || Time.time - ta > 10f, (ControlsLegend.Btn.Attack, "Attack"));
            if (Auto) yield return FeelDemo();
            if (Auto) StartCoroutine(AutoFight());
            while (true)
            {
                var eng = EngagedScree();
                // the dodge lesson needs its own punch: hold the next one while another prompt is still on screen
                bool holdForDodge = Chapter1State.Taught.Contains("fight_block") && !Chapter1State.Taught.Contains("fight_dodge") && _teachCo != null;
                foreach (var s in _fighters) { s.Engaged = s == eng; s.HoldPunch = holdForDodge; }
                if (_fighters[0].IsOut && _fighters[1].IsOut) break;
                if (Seven.FightDown)
                {
                    _fightResets++;
                    if (Auto || DemoBoot.PlayerSim) Cap?.Log($"fight: 7 knocked down ({Seven.HitsTaken} blows) - reset #{_fightResets} to the start of the fight");
                    if (Auto) yield return Shot("qf9_seven_knocked_down");
                    yield return Wait(0.8f);
                    yield return UI.Fade(1f, 0.4f, 1f);
                    Seven.Place(FightStart, Vector3.back); Seven.BeginFight();
                    foreach (var s in _fighters) s.ResetFight();
                    Cam.SnapBehind();
                    yield return UI.Fade(0f, 0.5f, 1f);
                    _fightProgress = Time.time;
                }
                if (Time.time - _fightProgress > 90f && !PauseMenu.IsOpen)
                {
                    StoryWatchdogs++; Chapter1State.Watchdogs++;
                    Cap?.Log("WATCHDOG: the terrace fight made no progress for 90 s - both Scree sit down, the reveal goes on");
                    foreach (var s in _fighters) s.ForceOut();
                    break;
                }
                yield return null;
            }
            if (Auto || DemoBoot.PlayerSim) Cap?.Log($"fight won in {Time.time - t0:0.0}s: blows landed {Seven.HitsLanded}, blocks {Seven.Blocks}, dodges {Seven.Dodges}, blows taken {Seven.HitsTaken}, resets {_fightResets}; both Scree down and alive (nonlethal)");
            if (Auto || DemoBoot.PlayerSim) Cap?.Log("fight timings measured in play (QF10): " + string.Join("; ", Seven.FightTimings) + $" | Scree voice: idle calls {ScreeVoice.Calls}, alert calls {ScreeVoice.Alerts}, fight chirps {ScreeVoice.FightChirps}");
            yield return Wait(1.8f);
            if (Auto) yield return Shot("qf9_both_scree_down");
            _fighting = false;
            Seven.EndFight();
            Legend.Teach();
            Objective(null);
            Cat.Follow();
            _fightWon = true;
        }

        /// <summary>QF10 check run: a deliberate combo (three chained blows), a dodge-cancel out of the last blow's recovery
        /// and a block, each captured as a short burst of consecutive frames.</summary>
        IEnumerator FeelDemo()
        {
            var s = EngagedScree(); if (s == null) yield break;
            for (float t = 0f; t < 4f && Flat(s.transform.position - Seven.transform.position).magnitude > 1.7f; t += Time.deltaTime) { Seven.ScriptMove = Flat(s.transform.position - Seven.transform.position).normalized; yield return null; }
            Seven.ScriptMove = Vector3.zero;
            Cam.Shot(Seven.transform.position + Vector3.Cross(Vector3.up, Flat(s.transform.position - Seven.transform.position).normalized) * 3.6f + Vector3.up * 1.6f, (Seven.transform.position + s.transform.position) * 0.5f + Vector3.up * 1.1f, 0.01f, true, 50f);
            Seven.AutoAttack = true; StartCoroutine(Burst("qf10_combo", 8, 0.07f));
            yield return Wait(0.17f); Seven.AutoAttack = true;          // pressed early: buffered, chains at the first moment it may
            yield return Wait(0.2f); Seven.AutoAttack = true;
            yield return Wait(0.24f); Seven.AutoDodge = true; StartCoroutine(Burst("qf10_dodge_cancel", 5, 0.06f));
            yield return Wait(0.5f); Seven.AutoBlockHeld = true; StartCoroutine(Burst("qf10_block", 4, 0.08f));
            yield return Wait(0.7f); Seven.AutoBlockHeld = false;
            Cam.Follow();
            Cap?.Log("fight feel demo (QF10): " + string.Join("; ", Seven.FightTimings));
        }
        IEnumerator Burst(string name, int n, float every)
        {
            for (int i = 0; i < n; i++) { yield return Shot($"{name}_{i}"); yield return new WaitForSecondsRealtime(every); }
        }

        void OnSevenFight(string what)
        {
            if (what == "hit" || what == "hit_taken" || what == "takedown_landed") _fightProgress = Time.time;
            var at = Seven.transform.position + Vector3.up;
            // ambient effort sounds only (no new dialogue): swings, padded hits, the fall
            if (what == "jab" || what == "cross" || what == "hook" || what == "finisher") AudioKit.Play("fight_swing", at, 0.6f, 0.08f, AudioKit.Bus.Sfx, 14f);
            if (what == "hit" || what == "takedown_landed") AudioKit.Play("fight_hit", at, 0.8f, 0.08f, AudioKit.Bus.Sfx, 16f);
            if (what == "hit_taken") AudioKit.Play("fight_hit", at, 0.7f, 0.08f, AudioKit.Bus.Sfx, 16f);
            if (what == "down") AudioKit.Play("body_fall", at, 0.8f, 0.05f, AudioKit.Bus.Sfx, 16f);
        }

        /// <summary>Check run: lose once on purpose (no defence, no attacks), then fight it like a player - attack in range,
        /// block the first punch, dodge the second, take a dazed Scree down.</summary>
        IEnumerator AutoFight()
        {
            Cap.Log("fight (check run): first attempt - standing still on purpose to be knocked down");
            while (_fighting && _fightResets == 0) yield return null;
            Cap.Log("fight (check run): second attempt - fighting");
            bool blocked = false;
            float nextAtk = 0f;
            while (_fighting)
            {
                var s = EngagedScree();
                Seven.ScriptMove = Vector3.zero;
                if (s != null && Seven.InFight && !Seven.FightDown)
                {
                    var to = Flat(s.transform.position - Seven.transform.position);
                    bool threat = s.FightState == ScreeGuard.Fight.Windup || s.FightState == ScreeGuard.Fight.Punch;
                    if (threat)
                    {
                        if (!blocked) { Seven.AutoBlockHeld = true; if (s.FightState == ScreeGuard.Fight.Punch && s.Tn > 0.6f) blocked = true; }
                        else if (s.FightState == ScreeGuard.Fight.Punch && s.Tn > 0.25f && Seven.Act != ChapterSeven.FightAct.Dodge) Seven.AutoDodge = true;
                    }
                    else
                    {
                        Seven.AutoBlockHeld = false;
                        if (s.Dazed && to.magnitude < 1.8f) Seven.AutoTakedown = true;
                        else if (to.magnitude > 1.45f) Seven.ScriptMove = to.normalized * 0.8f;
                        else if (Time.time >= nextAtk) { Seven.AutoAttack = true; nextAtk = Time.time + 0.3f; }
                    }
                }
                yield return null;
            }
            Seven.AutoBlockHeld = false; Seven.ScriptMove = Vector3.zero;
        }

        IEnumerator Scene6bReveal()
        {
            Step("the city reveal (out onto the terrace)", 6, MoonLayout.RevealSpot);
            if (Auto) StartCoroutine(HipShots());
            Objective("Find the way out");
            StartCoroutine(WatchCityBlend());
            // Round 9: past the sealed door the floor drops into an old mining cut; she climbs the far wall out onto the terrace.
            CatRoute("mining cut", new List<ChapterCat.Stop>
            {
                Walk(new Vector3(17.05f, MoonLayout.Lower, -38.95f)),
                HopTo(new Vector3(17.0f, -6.4f, -40.1f), () => Hanging(MoonTraversal.VisionExitLip) || Seven.transform.position.y < -4f, 0.3f),
                HopTo(MoonTraversal.StubMine, () => Seven.State == "wall_climb", 0.45f),
                HopTo(new Vector3(17.0f, MoonLayout.Lower, -41.85f), null, 0.45f),
                Walk(new Vector3(17.4f, MoonLayout.Lower, -43.2f), () => Seven.transform.position.z < -41.3f && Seven.transform.position.y > -2.6f),
            });
            if (Auto) StartCoroutine(AutoPit());
            yield return ExitFight();                          // QF9: the two Scree on the terrace - the fighting tutorial
            Step("the city reveal (out onto the terrace)", 6, MoonLayout.RevealSpot);
            yield return Until(() => Seven.transform.position.z < MoonLayout.ViewpointZ || _fightWon, "out onto the terrace", 300f);
            if (DemoBoot.FightOnly && Auto) { yield return Wait(2.5f); yield return Shot("qf9_reveal_start"); Cap.Log(ChapterCamera.Summary()); Cap.Finish("fight check (QF9): stopped at the start of the city reveal"); yield break; }
            // Emergence: no objective, no joke. She walks a few steps, then turns back to see where they escaped.
            yield return Cutscene(true);
            Cam.Subjects.Clear();               // 6b: scenic shots of the city (the lines play over them on purpose)
            Objective(null);
            // QF6: "at or past" - if she is already well beyond the viewpoint (down the ramp, on a scaffold, in the bay) the
            // reveal still plays from the terrace (a short fade there and back), never from wherever she happens to be.
            Vector3? resume = null;
            if (Dist7(MoonLayout.RevealSpot) > 5f || Seven.transform.position.y < MoonLayout.Lower - 0.5f)
            {
                resume = Seven.transform.position;
                Cap?.Log($"6b: 7 already past the viewpoint at {resume.Value} - the reveal plays from the terrace, then back there");
                yield return UI.Fade(1f, 0.5f);
                Seven.Place(MoonLayout.RevealSpot + new Vector3(0.2f, 0f, 2.0f), Vector3.back);
                Cat.Place(MoonLayout.RevealSpot + new Vector3(1.1f, 0f, 2.4f), Vector3.back);
                Cam.SnapBehind();
                StartCoroutine(UI.Fade(0f, 0.6f));
            }
            // Round 12 (2e): Dan's rewritten 6b - they step out and see the city for the first time. The city is alive
            // (FightDemoBuilderCity2: lights, traffic, clouds); its distant hum replaces the prison sounds.
            MusicSystem.Play("main_theme", 4f);
            StartCoroutine(SwapAmbience("amb_city_life", 3f));
            Seven.ScriptDriven = true;
            Cat.GoTo(MoonLayout.RevealSpot + new Vector3(0.95f, 0f, -0.2f), Cat.StrollSpeed, MoonLayout.RevealSpot + Vector3.back * 20f);
            Seven.Lock(false);
            for (float t0 = Time.time; Dist7(MoonLayout.RevealSpot) > 0.2f && Time.time - t0 < 6f;)
            { Seven.ScriptMove = Flat(MoonLayout.RevealSpot - Seven.transform.position).normalized * 0.7f; yield return null; }
            Seven.ScriptMove = Vector3.zero;
            Seven.Lock(true);
            Seven.ScriptDriven = Auto;
            var cityDir = new Vector3(-0.05f, 0f, -1f).normalized;
            for (float t = 0f; t < 0.8f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(cityDir), 200f * Time.deltaTime); yield return null; }
            var p7 = Seven.transform.position; var side = Vector3.Cross(Vector3.up, cityDir);
            var ik = Seven.gameObject.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
            ik.enabled = true; ik.Alive = true; ik.Ease = 1.6f; ik.LeftW = 0f; ik.RightW = 0f; ik.LookW = 0f;
            // OVER 7'S SHOULDER - THE REVEAL (hold before anyone speaks)
            Cam.Shot(p7 - cityDir * 1.7f - side * 0.75f + Vector3.up * 1.75f, p7 + cityDir * 420f + Vector3.up * 70f, 0.01f, true, 56f);
            Coroutine pan = StartCoroutine(LookAlong(p7 - cityDir * 1.7f - side * 0.75f + Vector3.up * 1.75f, p7 + cityDir * 420f + Vector3.up * 70f, p7 + cityDir * 420f + Vector3.up * 260f, 9f, 56f));
            void Cut6b() { if (pan != null) StopCoroutine(pan); pan = null; }
            yield return Wait(2.6f);
            yield return Shot("c01_over_her_shoulder_the_reveal");
            yield return Wait(2.2f);
            // LOW TWO-SHOT - 7 AND CAT LOOKING UP: she slowly tilts her head back, he follows her gaze
            Cut6b();
            if (Flat(Cat.transform.position - (p7 + side * 0.95f)).magnitude > 0.6f) Cat.Place(p7 + side * 0.95f, cityDir);   // beside her for the two-shot
            Cat.Hold(p7 + cityDir * 20f, "sit");
            var mid = (p7 + Cat.transform.position) * 0.5f;
            Cam.Shot(mid + cityDir * 2.9f + side * 0.9f + Vector3.up * 0.35f, mid + Vector3.up * 0.95f, 0.01f, true, 52f);
            var sky = p7 + cityDir * 60f + Vector3.up * 140f;
            ik.LookPos = p7 + cityDir * 8f + Vector3.up * 1.8f; ik.LookW = 1f;
            yield return Wait(0.5f);
            ik.LookPos = sky;
            StartCoroutine(CatLooksAt(() => sky, 9f));
            yield return Wait(1.2f);
            yield return Shot("c02_low_two_shot_looking_up");
            LogCatInFrame("6b low two-shot");
            // Round 13 (0a, Dan): only briefly on them looking up - then cut to WHAT they are looking at: their view up the
            // curve, over her shoulder, the traffic streams overhead and the towers rising into the clouds
            Cut6b();
            var povEye = p7 + Vector3.up * 1.6f - cityDir * 0.45f + side * 0.3f;
            var upCurve = p7 + cityDir * 140f + Vector3.up * 150f;
            Cam.Shot(povEye, upCurve, 0.01f, true, 64f);
            pan = StartCoroutine(LookAlong(povEye, upCurve, sky + side * 30f, 7f, 64f));
            yield return Wait(1.2f);
            yield return Shot("c02b_pov_up_the_curve_traffic_towers");
            yield return Dlg.PlayScene(65, 1, 2);            // I've never seen the inside of a moon before. / Most people insist on looking at the outside.
            // she barely hears him: her attention (and the shot) follows a stream of traffic overhead
            Cut6b();
            pan = StartCoroutine(LookAlong(povEye, sky + side * -40f, sky + side * 60f, 2.6f, 60f));
            for (float t = 0f; t < 1.4f; t += Time.deltaTime) { ik.LookPos = sky + side * Mathf.Lerp(-40f, 60f, t / 1.4f); yield return null; }
            yield return Shot("c02c_pov_following_the_traffic");
            yield return Dlg.PlayScene(65, 3, 4);            // Are those cars? / Apparently we escaped just in time for traffic.
            ik.LookPos = p7 + cityDir * 30f + Vector3.up * 6f; ik.LookW = 0.6f;
            // EXTREME WIDE - THEIR SMALL FIGURES AT THE EXIT (hold; the city's distant hum)
            Cut6b();
            var wPos = new Vector3(26f, 42f, -74f);                          // high out over the city edge, looking back at the terrace
            Cam.Shot(wPos, p7 + Vector3.up * 1.5f, 0.01f, true, 40f);
            pan = StartCoroutine(LookAlong(wPos, p7 + Vector3.up * 1.5f, p7 + Vector3.up * 12f, 7f, 40f));
            yield return Wait(2.4f);
            yield return Shot("c03_extreme_wide_small_figures");
            yield return Wait(1.0f);
            yield return Dlg.PlayScene(65, 5, 5);            // There are people living all the way up there.
            // the cat watches her rather than the skyline
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { var d = Flat(Seven.transform.position - Cat.transform.position); if (d.sqrMagnitude > 1e-4f) Cat.transform.rotation = Quaternion.RotateTowards(Cat.transform.rotation, Quaternion.LookRotation(d), 300f * Time.deltaTime); yield return null; }
            StartCoroutine(CatLooksAt(() => SevenHead, 4f));
            Cut6b();
            var cp = Cat.transform.position;
            // low in front of him, the city side: he fills the lower frame, looking up at her
            Cam.Shot(cp + cityDir * 1.9f - side * 0.35f + Vector3.up * 0.45f, cp + Vector3.up * 0.25f + (SevenHead - cp) * 0.18f, 0.01f, true, 52f);
            yield return Wait(0.6f);
            yield return Shot("c04_the_cat_watches_her");
            LogCatInFrame("6b the cat watches her");
            yield return Dlg.PlayScene(65, 6, 6);            // Come on. Let's find your ship.
            // she starts after him, then looks back once more
            Cat.GoTo(p7 + side * 2.6f + Vector3.forward * -0.5f, Cat.StrollSpeed, MoonLayout.SaucerCentre);
            ik.LookW = 0f;
            Seven.Lock(false); Seven.ScriptDriven = true;
            var after = p7 + side * 1.4f;
            for (float t0 = Time.time; Dist7(after) > 0.25f && Time.time - t0 < 3f;) { Seven.ScriptMove = Flat(after - Seven.transform.position).normalized * 0.6f; yield return null; }
            Seven.ScriptMove = Vector3.zero; Seven.Lock(true); Seven.ScriptDriven = Auto;
            Cam.Shot(Seven.transform.position - cityDir * 1.9f - side * 0.6f + Vector3.up * 1.65f, Seven.transform.position + cityDir * 40f + Vector3.up * 10f, 0.01f, true, 50f);
            for (float t = 0f; t < 1.0f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(cityDir + side * -0.3f), 180f * Time.deltaTime); yield return null; }
            yield return Shot("c05_she_looks_back_once_more");
            // HER VIEW - THE CITY'S LOWER DISTRICTS: mining structures cutting into the ancient city, excavation lights on
            // broken foundations, vast machinery beneath inhabited terraces - the beauty and the damage in one frame
            if (City != null) City.StartMachinery();
            Cut6b();
            var eye = new Vector3(16f, 30f, -62f);                            // her line of sight, from just above the terrace edge
            Cam.Shot(eye, MoonLayout.BasinCenter + new Vector3(-60f, -10f, 0f), 0.01f, true, 34f);
            pan = StartCoroutine(LookAlong(eye, MoonLayout.BasinCenter + new Vector3(-90f, -10f, 0f), MoonLayout.BasinCenter + new Vector3(40f, 10f, 60f), 5f, 34f));
            yield return Wait(2.6f);
            yield return Shot("c06_her_view_the_lower_districts");
            yield return Wait(1.0f);
            Cut6b();
            if (Auto && DemoBoot.Quick)
            {
                // 2f check: the saucer from the same offset as on the Venus platform (side by side)
                Cam.Shot(MoonLayout.SaucerCentre + SaucerCheckOffset, MoonLayout.SaucerCentre + Vector3.up * 1.6f, 0.01f, true, 45f);
                yield return Wait(0.5f);
                yield return Shot("saucer_side_by_side_moon");
                Cap.Finish("6b check (2e): the city reveal, then the saucer side-by-side shot"); yield break;
            }
            ik.LookW = 0f;
            // Scene 7: she spots the impound bay holding her saucer, small but recognisable by its rim glow.
            for (float t = 0f; t < 0.8f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(MoonLayout.SaucerCentre - Seven.transform.position)), 200f * Time.deltaTime); yield return null; }
            // QF4: over her shoulder with the cat beside her and the saucer far below (was a tight 34 deg shot that lost them)
            var toS = Flat(MoonLayout.SaucerCentre - Seven.transform.position).normalized;
            var sideS = Vector3.Cross(Vector3.up, toS);
            Cat.GoTo(Seven.transform.position + sideS * 0.8f + toS * 0.3f, Cat.StrollSpeed, MoonLayout.SaucerCentre);
            Talk();
            Cam.Shot(Seven.transform.position - toS * 2.7f - sideS * 0.6f + Vector3.up * 2.1f, Seven.transform.position + toS * 7f + Vector3.down * 1.2f, 1.2f, false, 50f);
            yield return Wait(1.4f);
            yield return ReadyToTalk();
            yield return Shot("c06_saucer_spotted");
            yield return Dlg.PlayScene(7);
            Seven.Release();
            if (resume.HasValue && resume.Value.z < -48.6f)
            {
                yield return UI.Fade(1f, 0.5f);
                Seven.Place(resume.Value, Vector3.back);
                Cat.Place(resume.Value + new Vector3(0.8f, 0f, 0.6f), Vector3.back);
                Cat.Follow();
                Cam.SnapBehind();
                StartCoroutine(UI.Fade(0f, 0.6f));
            }
            yield return Cutscene(false);
            Checkpoint(Chapter1State.CpBay);
        }

        /// <summary>2e: the cat's head follows a point (after the animation each frame).</summary>
        IEnumerator CatLooksAt(Func<Vector3> at, float seconds)
        {
            Transform head = null;
            foreach (var t in Cat.GetComponentsInChildren<Transform>()) if (t.name == "head") { head = t; break; }
            if (head == null) yield break;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                var d = (at() - head.position).normalized;
                head.rotation = Quaternion.Slerp(head.rotation, Quaternion.LookRotation(d), 0.4f);
                yield return new WaitForEndOfFrame();
            }
        }
        Vector3 SevenHead { get { var a = Seven.GetComponent<Animator>(); var h = a != null && a.isHuman ? a.GetBoneTransform(HumanBodyBones.Head) : null; return h != null ? h.position : Seven.transform.position + Vector3.up * 1.55f; } }
        /// <summary>2e check: the cat fully in frame (his renderers' screen rectangle inside the frame).</summary>
        void LogCatInFrame(string what)
        {
            if (!(Auto || DemoBoot.PlayerSim) || Cap == null) return;
            var cam = Cam.GetComponent<Camera>(); Bounds b = default; bool any = false;
            foreach (var r in Cat.GetComponentsInChildren<Renderer>()) { if (!r.enabled || r is ParticleSystemRenderer) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            if (!any) return;
            float x0 = 9f, y0 = 9f, x1 = -9f, y1 = -9f;
            for (int i = 0; i < 8; i++) { var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)); var v = cam.WorldToViewportPoint(c); x0 = Mathf.Min(x0, v.x); y0 = Mathf.Min(y0, v.y); x1 = Mathf.Max(x1, v.x); y1 = Mathf.Max(y1, v.y); }
            float area = Mathf.Max(1e-6f, (x1 - x0) * (y1 - y0)), ix = Mathf.Max(0f, Mathf.Min(1f, x1) - Mathf.Max(0f, x0)), iy = Mathf.Max(0f, Mathf.Min(1f, y1) - Mathf.Max(0f, y0));
            Cap.Log($"{what}: the cat {(1f - ix * iy / area) * 100f:0.0} % off screen (must be 0)");
        }

        public static readonly Vector3 SaucerCheckOffset = new Vector3(9f, 3.2f, 9f);
        IEnumerator WatchCityBlend()
        {
            while (!_cityLook)
            {
                if (Seven.transform.position.z < MoonLayout.CityBlendZ) { yield return CityLook(3.5f); yield break; }
                yield return null;
            }
        }

        // ------------------------------------------------------------------------------------------ 7-8: bay, beam
        /// <summary>Scenes 7-8 after the reveal (checkpoint 7): down to the bay, release the saucer, the beam, liftoff.</summary>
        IEnumerator SceneBay()
        {
            Step("release the saucer, then board in the light", 7, MoonLayout.LeverSpot);
            MusicSystem.Play("moon_prison", 5f);
            Objective("Get down to the bay and release the saucer");
            // Round 9: down the broken ramp, two scaffolds and drops to the bay floor; the cat hops ahead.
            CatRoute("bay descent", new List<ChapterCat.Stop>
            {
                Walk(new Vector3(21.0f, MoonLayout.Lower, -48.2f)),
                Walk(new Vector3(20.95f, -4.3f, -54.55f), () => Seven.transform.position.z < -48.5f),
                HopTo(new Vector3(20.3f, -7.6f, -56.2f), () => Hanging(MoonTraversal.RampEnd) || Seven.transform.position.y < -5f, 0.3f),
                HopTo(new Vector3(20.3f, -7.6f, -60.3f), () => OnZone("Scaffold1"), 0.5f),
                HopTo(new Vector3(20.1f, MoonLayout.Bay, -63.4f), () => OnZone("Scaffold2"), 0.3f),
                Walk(new Vector3(21f, MoonLayout.Bay, -72.4f), () => Seven.transform.position.y < MoonLayout.Bay + 0.5f),
            });
            Prompt("Telekinesis: clamp release lever", MoonLayout.Lever, 8.5f, true);
            if (Auto) yield return AutoBay();
            yield return Until(() => Seven.transform.position.y < MoonLayout.Bay + 0.5f, "down in the bay", 300f);
            if (Auto && Workers != null)
            {
                foreach (var w in Workers) ScreeFeet(w, "worker " + w.name);
                if (Workers.Length > 0) yield return AbdomenClearance(Workers[0], "worker", 2.4f);
                if (Workers.Length > 0) yield return GuardView("s07_checkonly_scree_worker_bay", Workers[0], 3.0f);
                if (Workers.Length > 4) yield return GuardView("s07_checkonly_scree_worker_terrace", Workers[4], 3.0f);
            }
            if (Auto)
            {
                // Round 10 capture: the silver saucer in the impound bay (reflections from its own probe)
                bool w = Cam.InShot;
                Cam.Shot(MoonLayout.SaucerCentre + new Vector3(8.5f, 2.6f, 9.5f), MoonLayout.SaucerCentre + Vector3.up * 1.0f, 0.01f, true, 48f);
                yield return null; yield return null;
                yield return Shot("s07_silver_saucer_in_bay");
                Cam.Shot(MoonLayout.SaucerCentre + new Vector3(-8.5f, 0.9f, 8.0f), MoonLayout.SaucerCentre + Vector3.up * 0.9f, 0.01f, true, 46f);
                yield return null; yield return null;
                yield return Shot("s07_silver_saucer_low_angle");
                var sp = Saucer != null ? Saucer.GetComponentInChildren<SaucerProbe>() : null;
                Cap.Log($"silver saucer: reflection probe riding with it {(sp != null ? $"renders so far {sp.Renders}" : "MISSING")}");
                if (!w) Cam.Follow();
            }
            yield return WaitPrompt("pull the release lever");
            Seven.Gesture(MoonLayout.Lever + Vector3.up * 1f);
            while (!Seven.GesturePeak) yield return null;
            TkStream(MoonLayout.Lever + Vector3.up * 1.1f);
            Seven.Lock(true);
            Cam.Shot(MoonLayout.Lever + new Vector3(1.2f, 3.2f, 4.5f), MoonLayout.SaucerCentre + new Vector3(2f, 1.2f, 0f), 1.0f, false, 40f);
            yield return ReleaseSaucer();
            yield return Cutscene(false);
            Cat.Follow();
            Objective("Board the saucer");
            if (Auto) yield return AutoWalk(MoonLayout.BeamApproach, MoonLayout.BeamEdge);

            yield return Until(() => Dist7(MoonLayout.BeamCentre) < 3.9f, "at the edge of the light", 60f);

            // Scene 8, at the edge of the light.
            yield return Cutscene(true);
            Objective(null);
            MusicSystem.Play("main_theme", 3f);
            Cat.GoTo(MoonLayout.BeamEdge + new Vector3(0.7f, 0f, 1.0f), Cat.TrotSpeed, MoonLayout.BeamCentre);
            Talk();
            Cam.Shot(MoonLayout.BeamCentre + new Vector3(6.6f, 1.7f, 4.6f), MoonLayout.BeamCentre + new Vector3(1.4f, 1.6f, 0f), 1.2f, false, 50f);
            yield return ReadyToTalk();
            yield return Dlg.PlayScene(8);
            yield return Board();
        }

        IEnumerator ReleaseSaucer()
        {
            var l0 = Lever.localRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f) { Lever.localRotation = l0 * Quaternion.Euler(Mathf.Lerp(0f, 70f, t), 0f, 0f); yield return null; }
            yield return Wait(0.3f);
            AudioKit.Play("clamps_release", MoonLayout.SaucerCentre + Vector3.up, 1f, 0f, AudioKit.Bus.Sfx, 45f);
            var starts = new List<Vector3>();
            foreach (var c in Clamps) starts.Add(c.position);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.2f)
            {
                for (int i = 0; i < Clamps.Length; i++)
                {
                    var outDir = Flat(starts[i] - MoonLayout.SaucerCentre).normalized;
                    Clamps[i].position = starts[i] + outDir * Mathf.SmoothStep(0f, 1.6f, t) + Vector3.down * Mathf.SmoothStep(0f, 0.9f, Mathf.InverseLerp(0.4f, 1f, t));
                }
                foreach (var r in RimGlow) Glow(r, Mathf.Lerp(0.9f, 3.2f, t));
                yield return null;
            }
            if (SaucerUnderLight != null) SaucerUnderLight.intensity = 3f;
            _saucerHum = AudioKit.Loop("saucer_hum", Saucer, null, 0.9f, AudioKit.Bus.Sfx, 3f, 45f);
            // The saucer lifts a little and hovers; the iris hatch opens and a soft cone of light shines down.
            var s0 = Saucer.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.4f) { Saucer.position = s0 + Vector3.up * MoonLayout.HoverLift * Mathf.SmoothStep(0f, 1f, t); yield return null; }
            StartCoroutine(Hover(s0 + Vector3.up * MoonLayout.HoverLift));
            Ground.IgnoreObstacle = 0;             // obstacle 0 = the saucer: she can walk under it now
            yield return Iris(true);
            yield return BeamOn(true);
            yield return Shot("s07_saucer_released_beam_on");
            // QF12 (2): the beam's iris hatch sits flush on the hull's underside (measured), also in a normal play
            if (DemoBoot.CaptureDir != null && Cap != null)
            {
                float hullY = Saucer.position.y + MoonLayout.SaucerBaseY - 0.1f; float gap = Hatch.position.y - hullY;
                Cap.Log($"beam hatch (QF12 2): iris at y {Hatch.position.y:0.00}, hull underside at y {hullY:0.00} - gap {gap * 100f:0.0} cm (flush = within 2 cm)");
                if (!Auto) yield return Shot("qf12_02_beam_on_player_view");
            }
            if (Auto) Cap.Log("saucer released: clamps open, rim glow up, hovering, iris open, tractor beam on (no ramp)");
        }

        bool _hovering;
        IEnumerator Hover(Vector3 at)
        {
            _hovering = true;
            for (float t = 0f; _hovering; t += Time.deltaTime) { Saucer.position = at + Vector3.up * Mathf.Sin(t * 1.1f) * 0.05f; yield return null; }
        }

        IEnumerator Iris(bool open)
        {
            AudioKit.Play("hatch_iris", Hatch.position, 1f, 0f, AudioKit.Bus.Sfx, 25f);
            if (HatchGlow != null) Glow(HatchGlow, open ? 3f : 0.4f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.1f)
            {
                float u = Mathf.SmoothStep(0f, 1f, open ? t : 1f - t);
                for (int i = 0; i < Hatch.childCount; i++)
                {
                    var b = Hatch.GetChild(i);
                    b.localRotation = Quaternion.Euler(0f, i * 45f + u * 50f, 0f);
                    b.localPosition = b.localRotation * new Vector3(MoonLayout.HatchRadius * 0.95f * u, 0.01f * u, 0f);
                }
                yield return null;
            }
        }

        IEnumerator BeamOn(bool on)
        {
            if (on) { Beam.gameObject.SetActive(true); AudioKit.Play("tractor_beam", MoonLayout.BeamCentre + Vector3.up * 2f, 0.8f, 0f, AudioKit.Bus.Sfx, 35f); }
            for (float t = 0f; t < 1f; t += Time.deltaTime / (on ? 1.3f : 1.6f))
            {
                float u = Mathf.SmoothStep(0f, 1f, on ? t : 1f - t);
                Beam.localScale = new Vector3(Mathf.Lerp(0.6f, 1f, u), Mathf.Max(0.001f, u), Mathf.Lerp(0.6f, 1f, u));
                if (BeamLight != null) BeamLight.intensity = 3.2f * u;
                yield return null;
            }
            if (!on) Beam.gameObject.SetActive(false);
        }

        /// <summary>Float one of them up the beam into the hatch (upright, slow, a gentle turn), then hide them.</summary>
        IEnumerator Rise(Transform who, float seconds, float turn, System.Action<float> each = null)
        {
            var p0 = who.position;
            float top = Saucer.position.y + MoonLayout.SaucerBaseY - 0.1f;
            var r0 = who.rotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                var c = new Vector3(MoonLayout.BeamCentre.x, 0f, MoonLayout.BeamCentre.z);
                var p = Vector3.Lerp(new Vector3(p0.x, 0f, p0.z), c, Mathf.Clamp01(t * 2.5f));
                p.y = Mathf.Lerp(p0.y, top + 0.3f, u);
                who.position = p;
                who.rotation = r0 * Quaternion.Euler(0f, turn * u, 0f);
                each?.Invoke(t);
                if (UnityEngine.Random.value < 0.5f) { _motes.transform.position = c + Vector3.up * UnityEngine.Random.Range(0f, top - MoonLayout.Bay) + UnityEngine.Random.insideUnitSphere * 0.8f; _motes.Emit(1); }
                yield return null;
            }
            Show(who.gameObject, false);
        }

        IEnumerator Board()
        {
            // 7 steps into the cone first and floats gently up, drawn in through the hatch; then the cat.
            Cam.Subjects.Clear();
            Seven.Lock(false); Seven.ScriptDriven = true;
            Cam.Shot(MoonLayout.BeamCentre + new Vector3(9.5f, 2.0f, 6.5f), MoonLayout.BeamCentre + new Vector3(0f, 2.3f, 0f), 1.6f, false, 46f);
            for (float t0 = Time.time; Dist7(MoonLayout.BeamCentre) > 0.35f && Time.time - t0 < 8f;)
            { Seven.ScriptMove = Flat(MoonLayout.BeamCentre - Seven.transform.position).normalized * 0.7f; yield return null; }
            Seven.ScriptMove = Vector3.zero;
            Seven.Lock(true);
            Seven.BeginFloat();
            bool s7a = false, s7b = false;
            yield return Rise(Seven.transform, 4.2f, 40f, t =>
            {
                if (!s7a && t > 0.35f) { s7a = true; StartCoroutine(Shot("s08_7_rising")); }
                if (!s7b && t > 0.75f) { s7b = true; StartCoroutine(Shot("s08_7_drawn_into_hatch")); }
            });
            yield return Wait(0.5f);
            Cat.Scripted("walk");
            for (float t0 = Time.time; Flat(Cat.transform.position - MoonLayout.BeamCentre).magnitude > 0.2f && Time.time - t0 < 5f;)
            {
                var d = Flat(MoonLayout.BeamCentre - Cat.transform.position);
                Cat.transform.position += d.normalized * Mathf.Min(d.magnitude, 1.4f * Time.deltaTime);
                Cat.transform.rotation = Quaternion.RotateTowards(Cat.transform.rotation, Quaternion.LookRotation(d), 360f * Time.deltaTime);
                yield return null;
            }
            Cat.Floating();                              // paws dangling (held jump frame), tail free
            bool sc = false;
            yield return Rise(Cat.transform, 3.4f, -60f, t => { if (!sc && t > 0.45f) { sc = true; StartCoroutine(Shot("s08_cat_rising")); } });
            // The hatch irises shut, the light fades, liftoff as before.
            yield return Wait(0.4f);
            StartCoroutine(BeamOn(false));
            yield return Wait(0.5f);
            StartCoroutine(Iris(false));
            yield return Wait(0.5f);
            yield return Shot("s08_hatch_closing");
            yield return Wait(0.8f);
            _hovering = false;
            // Liftoff: a wide shot from the bay corner.
            Cam.Shot(new Vector3(29f, MoonLayout.Bay + 3f, -61f), MoonLayout.SaucerCentre + Vector3.up * 2f, 0.4f, true);
            Burst(MoonLayout.SaucerCentre + Vector3.up * 0.3f, 160, 4f);
            AudioKit.Play("liftoff", null, 0.9f);
            if (_saucerHum != null) StartCoroutine(AudioKit.Fade(_saucerHum, 0f, 3f, true));
            var s0 = Saucer.position;
            float dur = 7.5f;
            bool shot1 = false, shot2 = false;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float u = t / dur;
                float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.55f)) * 22f;
                float away = Mathf.Pow(Mathf.Clamp01((u - 0.4f) / 0.6f), 2.2f) * 260f;
                Saucer.position = s0 + new Vector3(-away * 0.35f, rise + away * 0.35f, -away);
                Saucer.rotation = Quaternion.Euler(-Mathf.Clamp01((u - 0.35f) * 3f) * 9f, 0f, Mathf.Sin(u * 6f) * 1.2f);
                foreach (var r in RimGlow) Glow(r, 3.2f + Mathf.Clamp01(u * 2f) * 2f);
                Cam.Shot(new Vector3(29f, MoonLayout.Bay + 3f, -61f), Saucer.position + Vector3.up * 0.5f, 0.35f);
                if (!shot1 && u > 0.3f) { shot1 = true; yield return Shot("s08_liftoff_rising"); }
                if (!shot2 && u > 0.72f) { shot2 = true; yield return Shot("s08_liftoff_leaving"); }
                yield return null;
            }
            yield return UI.Fade(1f, 1.2f);
            if (Auto)
            {
                var mv = new System.Text.StringBuilder(); foreach (var kv in _moves) mv.Append(kv.Key).Append(' ').Append(kv.Value).Append(", ");
                Cap.Log($"traversal summary: {_traversalOk} scripted traversal steps ok, deliberate falls reset {_falls}, falls total {Seven.Falls}, worst hand-to-lip gap {_handGapMax:0.000} m, moves: {mv}taught: [{string.Join(", ", Chapter1State.Taught)}], permanent legend panels visible now: {Legend.VisiblePanels}");
            }
            if (Auto || DemoBoot.PlayerSim) { Cap?.Log(ChapterCamera.Summary()); Cap?.Log(ChapterCamera.VisSummary()); Cap?.Log(ChapterSeven.WallSummary()); }
            if (Auto) Cap.Log($"moon level done in {Time.time - Chapter1State.StartedAt:0.0}s game time; lines so far {Chapter1State.Played.Count}; cat max distance while following/leading {Cap.CatMaxBehind:0.00} m; cat catch-ups {Cat.CatchUps}; 7 footsteps {Seven.GetComponent<Footsteps>()?.Count}");
            Chapter1State.Aboard = true;
            SaveGame.SaveProgress(Chapter1State.CpAboard);   // QF6: Continue resumes aboard (scene 9)
            MusicSystem.Play("aboard_saucer", 3f);
            SceneManager.LoadScene("Saucer");
        }

        // ------------------------------------------------------------------------------------------ QF4 Scree checks
        /// <summary>Check runs only: a short close view of a Scree (front three-quarter), then back to the camera as it was.</summary>
        IEnumerator GuardView(string name, ScreeGuard g, float dist)
        {
            if (g == null) yield break;
            bool was = Cam.InShot;
            var at = g.transform.position + Vector3.up * 1.05f;
            var pos = at + g.transform.forward * dist;
            foreach (var yaw in new[] { 25f, -25f, 60f, -60f, 0f, 100f, -100f, 140f, -140f, 180f })
            {
                var c = at + Quaternion.AngleAxis(yaw, Vector3.up) * g.transform.forward * dist + Vector3.up * 0.35f;
                if (!Physics.CheckSphere(c, 0.3f, ~0, QueryTriggerInteraction.Ignore) && ChapterCamera.ClearLine(c, at)) { pos = c; break; }
            }
            Cam.Shot(pos, at, 0.01f, true, 45f);
            yield return null;
            yield return Shot(name);
            if (!was) Cam.Follow(); else Cam.Shot(Cam.transform.position, at, 0.01f, true);
        }

        /// <summary>Check runs: abdomen-to-leg clearance of a Scree sampled over a few seconds of its current clip.</summary>
        IEnumerator AbdomenClearance(ScreeGuard g, string label, float seconds)
        {
            var rig = g != null ? g.GetComponent<ScreeRig>() : null;
            if (rig == null) yield break;
            float min = float.MaxValue;
            for (float t = 0f; t < seconds; t += Time.deltaTime) { min = Mathf.Min(min, rig.LegClearance()); yield return null; }
            Cap.Log($"scree tucked abdomen vs legs ({label}, {seconds:0.0}s of '{g.State}'): smallest centre-line distance {min:0.000} m (abdomen + covers ~0.15 m thick, leg ~0.05 m: needs > 0.20) -> {(min > 0.20f ? "clears" : "TOUCHES")}");
        }

        /// <summary>Check runs: the lowest foot / toe bone of a Scree vs the walkable ground under it.</summary>
        void ScreeFeet(ScreeGuard g, string label)
        {
            if (g == null) return;
            var anim = g.GetComponent<Animator>();
            float low = float.MaxValue;
            foreach (var b in new[] { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.LeftToes, HumanBodyBones.RightToes })
            {
                var t = anim != null && anim.isHuman ? anim.GetBoneTransform(b) : null;
                if (t != null) low = Mathf.Min(low, t.position.y);
            }
            var p = g.transform.position;
            Cap.Log(low < float.MaxValue
                ? $"scree feet ({label}): lowest foot/toe bone {low - p.y:0.000} m above its root, root {p.y - Ground.Height(p.x, p.z):0.000} m above the ground (state {g.State}, model {(anim != null && anim.isHuman ? "Scree" : "placeholder")})"
                : $"scree feet ({label}): no humanoid bones");
        }

        // ------------------------------------------------------------------------------------------ QF4 opening test
        /// <summary>-openingtest n: the real opening (<see cref="Opening"/>) n times, each with its own seed: 7 starts at
        /// a random spot in the cell and either stays idle (the 20 s watchdog must start scene 1), walks to the door at
        /// once, wanders first, waits for the cat then goes, or runs back and forth along the beams while the cat arrives;
        /// every second run also injects frame hitches (150-450 ms frames, like a loading stall) while the cat walks.
        /// Pass = scene 1 starts and the cat is never inside the cell (checked every frame).</summary>
        IEnumerator OpeningTestRun()
        {
            int i = Chapter1State.OpeningRun, n = DemoBoot.OpeningTest;
            var rng = new System.Random(4000 + i * 7919);
            int variant = i % 5; bool hitches = i % 2 == 1;
            string[] names = { "idle (watchdog)", "to the door at once", "wanders, then the door", "waits for the cat, then the door", "runs along the beams" };
            var start = new Vector3((float)(rng.NextDouble() * 2.4 - 1.2), 0f, (float)(rng.NextDouble() * 2.0 - 1.2));
            Seven.Place(start, Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f) * Vector3.forward);
            int inside = 0, frames = 0, hitchCount = 0; float maxDt = 0f, deepest = 99f;
            bool done = false;
            IEnumerator Watch()
            {
                while (!done)
                {
                    frames++;
                    maxDt = Mathf.Max(maxDt, Time.deltaTime);
                    var c = Cat.transform.position;
                    if (c.x > -1.6f && c.x < 1.6f && c.z < MoonLayout.CellWallZ1) { inside++; deepest = Mathf.Min(deepest, c.z); }
                    yield return null;
                }
            }
            IEnumerator Hitches()
            {
                for (float t0 = Time.realtimeSinceStartup; !done && Time.realtimeSinceStartup - t0 < 14f;)
                {
                    if (rng.NextDouble() < 0.06) { System.Threading.Thread.Sleep(150 + rng.Next(300)); hitchCount++; }
                    yield return null;
                }
            }
            IEnumerator Drive()
            {
                Seven.ScriptDriven = true;
                yield return Wait(2.6f);            // the title fade
                switch (variant)
                {
                    case 0: break;
                    case 1: yield return AutoWalk(MoonLayout.DoorApproach); break;
                    case 2:
                        for (float t = 0f, tEnd = 3f + (float)rng.NextDouble() * 7f; t < tEnd && !done;)
                        {
                            var p = new Vector3((float)(rng.NextDouble() * 2.6 - 1.3), 0f, (float)(rng.NextDouble() * 2.4 - 1.3));
                            for (float u = 0f; u < 1.6f && Dist7(p) > 0.3f && !done; u += Time.deltaTime, t += Time.deltaTime) { Seven.ScriptMove = Flat(p - Seven.transform.position).normalized; yield return null; }
                        }
                        Seven.ScriptMove = Vector3.zero;
                        if (!done) yield return AutoWalk(MoonLayout.DoorApproach);
                        break;
                    case 3:
                        while (!Cat.Arrived && !done) yield return null;
                        yield return Wait((float)rng.NextDouble() * 6f);
                        if (!done) yield return AutoWalk(MoonLayout.DoorApproach);
                        break;
                    case 4:
                        float extra = -1f; int dir = 1;
                        while (!done)
                        {
                            var p = new Vector3(1.2f * dir, 0f, 0.95f);
                            Seven.ScriptRun = true;
                            while (Dist7(p) > 0.3f && !done) { Seven.ScriptMove = Flat(p - Seven.transform.position).normalized; yield return null; }
                            dir = -dir;
                            if (Cat.Arrived && extra < 0f) extra = Time.time + 3f;
                            if (extra > 0f && Time.time > extra) break;
                        }
                        Seven.ScriptRun = false; Seven.ScriptMove = Vector3.zero;
                        if (!done) yield return AutoWalk(MoonLayout.DoorApproach);
                        break;
                }
            }
            StartCoroutine(Watch());
            if (hitches) StartCoroutine(Hitches());
            StartCoroutine(Drive());
            float t0r = Time.time;
            yield return Opening();
            done = true;
            bool pass = inside == 0;
            var line = $"opening run {i + 1,2}/{n}: {(pass ? "PASS" : "FAIL")} variant '{names[variant]}', start {start.x:0.00},{start.z:0.00}, hitches {(hitches ? hitchCount + " (longest frame " + maxDt * 1000f + " ms)" : "off")}, " +
                       $"scene 1 reached after {Time.time - t0r:0.0}s via '{OpeningTrigger}', cat inside the cell {inside} of {frames} frames{(inside > 0 ? " (deepest z " + deepest.ToString("0.00") + ")" : "")}, cat pushes out of the cell box {Cat.NoCatPushes}, cat at {Cat.transform.position}";
            Cap.Log(line);
            Chapter1State.OpeningResults.Add(line);
            if (i < 5) yield return Shot($"op{i + 1:00}_scene1_{variant}");
            Chapter1State.OpeningRun++;
            if (Chapter1State.OpeningRun < n) { SceneManager.LoadScene(SceneManager.GetActiveScene().name); yield break; }
            int passed = Chapter1State.OpeningResults.FindAll(x => x.Contains(": PASS")).Count;
            Cap.Finish($"OPENING TEST: {passed}/{n} runs reached scene 1 with the cat outside the cell; watchdog fallbacks taken {Chapter1State.Watchdogs}");
        }

        // ------------------------------------------------------------------------------------------ QF5 cat-follow test
        /// <summary>-catfollowtest n: from the end of the cell rescue (scene 2 over, the cat on his spot) out of the cell
        /// block - corridor + walk-and-talk, the catwalk gap, the pipes, the bulkhead, the older wall (scene 4) - and up the
        /// cracked wall into the old passage, n times. 7 is played like a person: a virtual gamepad on the real input
        /// actions, the free player camera (no autopilot, no check-run shortcuts in the story code), each run with its
        /// own seed and path: walks away fast (runs, skips lines) · waits, then walks · backtracks to the far end ·
        /// circles · hurries over the bulkhead while the walk-and-talk still plays (Dan's path). Every 2nd run adds frame
        /// hitches. Pass = the run gets through (no soft-lock), the cat ends within 6 m of her, is never left waiting
        /// behind her at a route stop, is never left more than 6 m behind for long, and never enters the cell.</summary>
        IEnumerator CatFollowTestRun()
        {
            int i = Chapter1State.FollowRun, n = DemoBoot.CatFollowTest;
            var rng = new System.Random(5000 + i * 7919);
            int variant = i % 5; bool hitches = i % 2 == 1;
            string[] names = { "walks away fast", "waits, then walks", "backtracks to the far end", "circles", "hurries over the bulkhead during the walk-and-talk" };
            // the state at the end of scene 2 ("Which way?"): 7 just outside the door, the cat on his spot looking at his opening
            var start = new Vector3((float)(rng.NextDouble() - 0.5), 0f, 2.55f + (float)rng.NextDouble() * 0.7f);
            Seven.Place(start, Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f) * Vector3.forward);
            Cat.Place(new Vector3(-0.4f, 0f, 2.9f), Vector3.right);
            Cat.Hold(MoonLayout.VentMouth);
            Cam.SnapBehind();
            var pad = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>("FollowTestPad");
            Vector2 stick = Vector2.zero, look = Vector2.zero; bool run = false; int pulse = 0; uint pulseBit = 0;
            bool done = false, stuckFlag = false; string phase = "start";
            int inside = 0, frames = 0, hitchCount = 0; float maxDt = 0f, maxD = 0f, behindRun = 0f, maxBehindRun = 0f, waitingBehind = 0f, maxWaitingBehind = 0f;
            float dAtWall = -1f, dAtEnd = -1f;
            IEnumerator Feed()
            {
                while (!done)
                {
                    uint b = run ? (1u << (int)UnityEngine.InputSystem.LowLevel.GamepadButton.LeftShoulder) : 0u;
                    if (pulse > 0) { b |= pulseBit; pulse--; }
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad, new UnityEngine.InputSystem.LowLevel.GamepadState { leftStick = stick, rightStick = look, buttons = b });
                    yield return null;
                }
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad, new UnityEngine.InputSystem.LowLevel.GamepadState());
            }
            void Press(UnityEngine.InputSystem.LowLevel.GamepadButton btn) { pulseBit = 1u << (int)btn; pulse = 3; }
            const UnityEngine.InputSystem.LowLevel.GamepadButton A = UnityEngine.InputSystem.LowLevel.GamepadButton.South,
                B = UnityEngine.InputSystem.LowLevel.GamepadButton.East, X = UnityEngine.InputSystem.LowLevel.GamepadButton.West,
                Y = UnityEngine.InputSystem.LowLevel.GamepadButton.North;
            // world direction -> left stick, relative to the camera (as a person steers)
            Vector2 StickFor(Vector3 dir)
            {
                var f = Flat(Cam.transform.forward).normalized; var r = new Vector3(f.z, 0f, -f.x);
                dir = Flat(dir); if (dir.sqrMagnitude > 1f) dir.Normalize();
                return new Vector2(Vector3.Dot(dir, r), Vector3.Dot(dir, f));
            }
            IEnumerator Go(Vector3 to, float tol = 0.3f, float timeout = 25f, float speed = 1f)
            {
                for (float t0 = Time.time; Dist7(to) > tol && Time.time - t0 < timeout && !done;)
                {
                    if (Seven.Current == ChapterSeven.Mode.Free) stick = StickFor((to - Seven.transform.position).normalized * speed); else stick = Vector2.zero;
                    yield return null;
                }
                stick = Vector2.zero;
            }
            IEnumerator Idle(float s) { stick = Vector2.zero; for (float t = 0f; t < s && !done; t += Time.deltaTime) yield return null; }
            IEnumerator LookAround(float s, float x) { for (float t = 0f; t < s && !done; t += Time.deltaTime) { look = new Vector2(x, 0f); yield return null; } look = Vector2.zero; }
            IEnumerator Circle(Vector3 c, float r, int loops)
            {
                for (float a = 0f; a < loops * 360f && !done; a += 150f * Time.deltaTime)
                {
                    var p = c + new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0f, Mathf.Sin(a * Mathf.Deg2Rad)) * r;
                    if (Seven.Current == ChapterSeven.Mode.Free) stick = StickFor((p - Seven.transform.position).normalized);
                    yield return null;
                }
                stick = Vector2.zero;
            }
            // one traversal move: steer into it, press, until it is done; retry after a fall reset
            IEnumerator Move(string what, Vector3 from, Vector3 dir, UnityEngine.InputSystem.LowLevel.GamepadButton btn, Func<bool> ok, float hold = 0.5f)
            {
                for (int tries = 0; tries < 4 && !done; tries++)
                {
                    phase = what;
                    yield return Go(from, 0.2f, 20f, 0.6f);
                    for (float t = 0f; t < 0.25f; t += Time.deltaTime) { stick = StickFor(dir) * 0.35f; yield return null; }
                    Press(btn);
                    for (float t = 0f; t < hold; t += Time.deltaTime) { stick = StickFor(dir) * 0.35f; yield return null; }
                    stick = Vector2.zero;
                    // wait for the move to finish: 4 s after she is back in free control (climbs take longer), 25 s at most
                    for (float t0 = Time.time, tMax = Time.time + 25f; Time.time - t0 < 4f && Time.time < tMax && !ok() && !_resettingFall;)
                    { if (Seven.Current != ChapterSeven.Mode.Free) t0 = Time.time; yield return null; }
                    while (_resettingFall) yield return null;
                    if (ok()) yield break;
                }
                stuckFlag = true; Cap.Log($"follow run {i + 1}: could not do '{what}' (7 at {Seven.transform.position} {Seven.Current}/{Seven.State})");
            }
            IEnumerator Watch()
            {
                while (!done)
                {
                    frames++; maxDt = Mathf.Max(maxDt, Time.deltaTime);
                    var c = Cat.transform.position;
                    if (c.x > -1.6f && c.x < 1.6f && c.z < MoonLayout.CellWallZ1) inside++;
                    float d = Cat.DistanceTo7;
                    bool play = Cat.LeashAllowed();
                    if (play) maxD = Mathf.Max(maxD, d);
                    // "left behind": more than 6 m away and she is further along (or he is not leading)
                    bool behindFar = play && d > 6f && !(Cat.Current == ChapterCat.Mode.Route && !Cat.WaitingBehind && Cat.ArcOf(c) >= Cat.ArcOf(Seven.transform.position) - 0.75f);
                    behindRun = behindFar ? behindRun + Time.deltaTime : 0f; maxBehindRun = Mathf.Max(maxBehindRun, behindRun);
                    waitingBehind = play && Cat.WaitingBehind ? waitingBehind + Time.deltaTime : 0f; maxWaitingBehind = Mathf.Max(maxWaitingBehind, waitingBehind);
                    yield return null;
                }
            }
            IEnumerator Hitches()
            {
                while (!done) { if (rng.NextDouble() < 0.03) { System.Threading.Thread.Sleep(150 + rng.Next(300)); hitchCount++; } yield return null; }
            }
            IEnumerator SkipLines(bool on) { while (!done) { if (on && Dlg.Busy && rng.NextDouble() < 0.02) Press(Y); yield return null; } }
            bool fast = variant == 0 || variant == 4;
            IEnumerator Drive()
            {
                yield return Idle(0.6f);
                run = fast;
                switch (variant)
                {
                    case 1: yield return Idle(10f + (float)rng.NextDouble() * 10f); yield return LookAround(0.8f, 0.7f); break;
                    case 2:
                        yield return Go(new Vector3(6.8f, 0f, 3.3f));           // toward the gap (the walk-and-talk starts)...
                        run = true; yield return Go(new Vector3(-6.6f, 0f, 3.4f));   // ...then all the way back past the cell
                        yield return Idle(1.5f + (float)rng.NextDouble() * 2f); run = rng.NextDouble() < 0.5; break;
                    case 3:
                        yield return Go(new Vector3(2.8f, 0f, 3.3f));
                        yield return Circle(new Vector3(2.8f, 0f, 3.3f), 0.9f, 2);
                        yield return LookAround(1.0f, -0.8f); break;
                }
                phase = "corridor";
                yield return Go(new Vector3(7.6f, 0f, 3.3f), 0.3f, 40f);
                if (!fast && variant != 3) while (_scene3 && !done) yield return null;   // a calm player listens to the walk-and-talk first
                yield return Move("jump the catwalk gap", new Vector3(7.95f, 0f, 3.3f), Vector3.right, A, () => FreeAt(0f) && Seven.transform.position.x > 10f);
                if (variant == 3) yield return Circle(new Vector3(10.5f, 0f, 3.3f), 0.5f, 1);
                if (variant == 2) { yield return Go(new Vector3(10.3f, 0f, 2.6f)); yield return Idle(1f); }
                phase = "pipes";
                yield return Move("crouch at the pipes", new Vector3(10.95f, 0f, 3.3f), Vector3.right, B, () => Seven.Crouched, 0.2f);
                yield return Go(new Vector3(13.75f, 0f, 3.3f), 0.25f, 25f, 0.8f);
                if (Seven.Crouched) { Press(B); yield return Idle(0.6f); }
                yield return Move("jump to grab the bulkhead", new Vector3(13.6f, 0f, 3.3f), Vector3.right, A, () => Hanging(MoonTraversal.Bulkhead));
                phase = "climb the bulkhead";
                for (float t0 = Time.time; Time.time - t0 < 6f && !FreeAt(MoonTraversal.BulkheadTop) && !done;) { stick = StickFor(Vector3.right); yield return null; }
                stick = Vector2.zero;
                if (!FreeAt(MoonTraversal.BulkheadTop)) { stuckFlag = true; Cap.Log($"follow run {i + 1}: could not climb the bulkhead ({Seven.Current}/{Seven.State})"); }
                if (variant == 1) yield return Idle(2f);
                yield return Move("jump down from the bulkhead", new Vector3(14.95f, MoonTraversal.BulkheadTop, 3.3f), Vector3.right, A, () => FreeAt(0f) && Seven.transform.position.x > 15.3f);
                if (variant == 3) yield return Circle(new Vector3(17.2f, 0f, 3.4f), 0.8f, 1);
                phase = "older wall";
                yield return Go(MoonLayout.OldWallSpot, 0.4f, 30f);
                for (float t0 = Time.time; !PromptNear && Time.time - t0 < 90f && !done;) yield return null;   // the prompt (after the gate hint / the walk-and-talk)
                dAtWall = Cat.DistanceTo7;
                Press(X);
                for (float t0 = Time.time; Chapter1State.Checkpoint < 2 && Time.time - t0 < 90f && !done;) yield return null;
                if (Chapter1State.Checkpoint < 2) yield break;
                phase = "old passage";
                yield return Idle(0.4f);
                yield return Move("climb the cracked wall", new Vector3(16.5f, 0f, 0.15f), Vector3.back, A, () => FreeAt(2.8f), 0.3f);
                for (float t0 = Time.time; Time.time - t0 < 12f && !FreeAt(2.8f) && !done;) yield return null;
                yield return Go(new Vector3(16.4f, 2.8f, -1.2f), 0.3f, 6f, 0.5f);
                yield return Idle(5f);
                dAtEnd = Cat.DistanceTo7;
                phase = "end";
            }
            StartCoroutine(Feed());
            StartCoroutine(Watch());
            StartCoroutine(SkipLines(variant == 0));
            if (hitches) StartCoroutine(Hitches());
            var flow = StartCoroutine(Scene3And4());
            Coroutine s5 = null;
            var drive = StartCoroutine(Drive());
            float t0r = Time.time;
            bool finished = false;
            while (Time.time - t0r < 300f)
            {
                if (s5 == null && Chapter1State.Checkpoint >= 2) s5 = StartCoroutine(Scene5Ruins());
                if (phase == "end") { finished = true; break; }
                yield return null;
            }
            done = true;
            StopCoroutine(drive); StopCoroutine(flow); if (s5 != null) StopCoroutine(s5);
            yield return null;
            UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);
            bool pass = finished && !stuckFlag && inside == 0 && dAtEnd >= 0f && dAtEnd <= 6f && maxWaitingBehind < 1f && maxBehindRun < 6f;
            var line = $"follow run {i + 1,2}/{n}: {(pass ? "PASS" : "FAIL")} '{names[variant]}', start {start.x:0.00},{start.z:0.00}, hitches {(hitches ? hitchCount + " (longest frame " + (maxDt * 1000f).ToString("0") + " ms)" : "off")}, " +
                       $"{(finished ? "out of the block in " + (Time.time - t0r).ToString("0.0") + "s" : "SOFT-LOCK at '" + phase + "' after " + (Time.time - t0r).ToString("0") + "s")}, cat at the older wall {dAtWall:0.0} m, at the end {dAtEnd:0.0} m, " +
                       $"longest wait at a stop she had passed {maxWaitingBehind:0.0}s, longest > 6 m behind {maxBehindRun:0.0}s, max distance in play {maxD:0.0} m, leash {Cat.Leashes} (trotted {Cat.LeashTrots}, reappeared {Cat.LeashBlinks}), " +
                       $"cat inside the cell {inside}/{frames} frames, cat {Cat.Current} at {Cat.transform.position} (route stop {Cat.RouteStop + 1}), 7 at {Seven.transform.position}";
            Cap.Log(line);
            Chapter1State.FollowResults.Add(line);
            if (i < 5) yield return Cap.Shot($"cf{i + 1:00}_end_{variant}");
            Chapter1State.FollowRun++;
            if (Chapter1State.FollowRun < n) { Chapter1State.Checkpoint = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().name); yield break; }
            int passed = Chapter1State.FollowResults.FindAll(x => x.Contains(": PASS")).Count;
            int soft = Chapter1State.FollowResults.FindAll(x => x.Contains("SOFT-LOCK")).Count;
            Cap.Finish($"CAT FOLLOW TEST: {passed}/{n}, soft-locks {soft} (QF5 cat fix {(ChapterCat.QF5Fix ? "on" : "OFF - repro")})");
        }

        // ------------------------------------------------------------------------------------------ QF4 dialogue view
        /// <summary>Sets who must stay in view during the next conversation (the camera reframes if a wall gets in
        /// the way: <see cref="ChapterCamera.Subjects"/>). 7 and the cat always; extra = e.g. the guard.</summary>
        void Talk(params Transform[] extra)
        {
            Cam.Subjects.Clear();
            Cam.TalkUntil = Time.time + 4f;
            var head = Seven.Bone(HumanBodyBones.Head);
            Cam.Subjects.Add((head != null ? head : Seven.transform, head != null ? 0.05f : 1.5f));
            Cam.Subjects.Add((Cat.transform, 0.18f));
            foreach (var t in extra) if (t != null) Cam.Subjects.Add((t, 1.55f));
        }

        /// <summary>QF4: a conversation starts once the cat has reached his spot and the camera has settled on its
        /// framing (at most <paramref name="maxWait"/> s - never a soft-lock).</summary>
        IEnumerator ReadyToTalk(float maxWait = 3f)
        {
            for (float t0 = Time.time; Time.time - t0 < maxWait; )
            {
                bool catOk = Cat.Current != ChapterCat.Mode.GoTo && Cat.Current != ChapterCat.Mode.Lead && Cat.Current != ChapterCat.Mode.Hop;
                if (catOk && Cam.Settled) yield break;
                yield return null;
            }
        }

        /// <summary>Check runs: while any line plays, every 0.1 s test 7, the cat (and the guard in scene 5) from the
        /// camera - unobstructed (raycast) and inside the frame - and log the totals per dialogue scene at the end.</summary>
        IEnumerator SpeakerViewSampler()
        {
            var cam = Cam.GetComponent<Camera>();
            while (true)
            {
                yield return Wait(0.1f);
                var line = Dlg.Current;
                if (line == null) continue;
                if (UI.FadeAlpha > 0.5f || UI.VideoAlpha > 0.5f) continue;     // the vision (white wash / video) covers the screen
                var who = new List<(string, Vector3, bool)>();
                var head = Seven.Bone(HumanBodyBones.Head);
                who.Add(("7", head != null ? head.position + Vector3.up * 0.05f : Seven.transform.position + Vector3.up * 1.5f, Shown(Seven.gameObject)));
                who.Add(("cat", Cat.transform.position + Vector3.up * 0.18f, Shown(Cat.gameObject)));
                if (line.Scene == 5 && line.Id != "s05_04_7" && Guard != null) who.Add(("guard", Guard.transform.position + Vector3.up * 1.55f, true));
                foreach (var (name, p, shown) in who)
                {
                    if (!shown) continue;
                    // story beats where a speaker is meant to be unseen: the cat walking off to the hidden release
                    if (name == "cat" && (line.Id == "s02_02_7" || line.Id == "s02_03_cat")) continue;
                    var key = $"scene {line.Scene,2} {name}";
                    if (!_view.TryGetValue(key, out var v)) v = new int[3];
                    v[0]++;
                    if (!ChapterCamera.ClearLine(cam.transform.position, p)) { v[1]++; if (v[1] <= 3) Cap.Log($"speaker view: {name} OCCLUDED during {line.Id} (camera {cam.transform.position}, {name} {p})"); }
                    var vp = cam.WorldToViewportPoint(p);
                    // 6b (65/66): the lines play over scenic city shots on purpose - only occlusion counts there
                    if (line.Scene != 65 && line.Scene != 66 && !(vp.z > 0f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f)) v[2]++;
                    _view[key] = v;
                }
            }
        }
        readonly SortedDictionary<string, int[]> _view = new SortedDictionary<string, int[]>();
        static bool Shown(GameObject go) { foreach (var r in go.GetComponentsInChildren<Renderer>()) if (r.enabled) return true; return false; }
        void OnDestroy()
        {
            if (!Auto || Cap == null || _view.Count == 0) return;
            foreach (var kv in _view)
                Cap.Log($"speaker view {kv.Key}: {kv.Value[0]} samples during lines, occluded {kv.Value[1]} ({100f * kv.Value[1] / kv.Value[0]:0.0}%), outside the frame {kv.Value[2]} ({100f * kv.Value[2] / kv.Value[0]:0.0}%), camera reframes so far {ChapterCamera.Reframes}");
        }

        // ================================================================================== Round 9: traversal
        bool OnZone(string name)
        {
            var z = Ground.FindZone(name); var p = Seven.transform.position;
            return z != null && z.Contains(p.x, p.z) && Mathf.Abs(z.HeightAt(p.x, p.z) - p.y) < 0.15f && (Seven.Current == ChapterSeven.Mode.Free || Seven.Current == ChapterSeven.Mode.Locked);
        }
        bool Hanging(MoonTraversal.Ledge l) => Seven.Current == ChapterSeven.Mode.Hang && Seven.HangLedge == l;
        bool _slabMoved, _resettingFall;
        readonly Dictionary<string, int> _moves = new Dictionary<string, int>();

        void OnMove(string what)
        {
            _moves[what] = _moves.TryGetValue(what, out var n) ? n + 1 : 1;
            var p = Seven.transform.position;
            // Round 9 SFX (Docs/SFX-LIST.md): grab, landing, climbing scuffs
            if (what == "grab") AudioKit.Play("ledge_grab", p + Vector3.up * 2.2f, 0.8f, 0.06f, AudioKit.Bus.Sfx, 14f);
            else if (what == "land") AudioKit.Play(Seven.LastFallHeight > 2.6f ? "land_hard" : "land_soft", p, Seven.LastFallHeight > 1f ? 0.8f : 0.55f, 0.06f, AudioKit.Bus.Sfx, 14f);
            else if (what == "wallclimb" || what == "climb" || what == "mantle" || what == "climbdown") AudioKit.Play("climb_scuff", p + Vector3.up * 1.2f, 0.6f, 0.08f, AudioKit.Bus.Sfx, 12f);
            if (!Auto) return;
            string extra = what == "land" ? $", fall {Seven.LastFallHeight:0.00} m" : what == "grab" || what == "climbdown" ? $", ledge '{Seven.HangLedge?.Name}'" : "";
            Cap.Log($"move: {what} at ({p.x:0.00}, {p.y:0.00}, {p.z:0.00}){extra}");
            if (what == "grab" || what == "climbdown") StartCoroutine(LogHandGap());
        }

        IEnumerator LogHandGap()
        {
            yield return Wait(0.6f);
            if (Seven.Current != ChapterSeven.Mode.Hang) yield break;
            var g = Seven.HandGap();
            Cap.Log($"hands on the ledge '{Seven.HangLedge.Name}': wrist height vs lip {g.x:0.000} m, along the wall normal {g.y:0.000} m (state {Seven.State})");
            _handGapMax = Mathf.Max(_handGapMax, Mathf.Max(g.x, g.y));
        }
        float _handGapMax;

        /// <summary>A fall into a chasm: fade to white, back on the last safe ledge, fade in. No injury, no death.</summary>
        IEnumerator FallReset(Vector3 at)
        {
            if (_resettingFall) yield break;
            _resettingFall = true;
            Chapter1State.FallResets++;
            if (Auto || DemoBoot.PlayerSim) Cap?.Log($"fall into the dark at ({at.x:0.00}, {at.y:0.00}, {at.z:0.00}) -> white fade, back to the last safe ledge {Seven.LastSafe}");
            AudioKit.Play("energy_point", null, 0.5f);
            yield return UI.Fade(1f, 0.45f, 1f);
            if (Auto) yield return Shot("tr_fall_white");
            Seven.ReturnToSafe();
            Seven.Lock(true);
            Cam.SnapBehind();
            yield return Wait(0.3f);
            yield return UI.Fade(0f, 0.6f, 1f);
            Seven.Lock(false);
            _resettingFall = false;
            if (Auto || DemoBoot.PlayerSim) Cap?.Log($"fall reset done: 7 at {Seven.transform.position} ({Seven.Current})");
        }

        // ------------------------------------------------------------------------------------------ teaching prompts
        Coroutine _teachCo;
        string _teachId;
        IEnumerator TeachRoutine(string id, Func<bool> done, (ControlsLegend.Btn, string)[] items)
        {
            _teachId = id;
            Chapter1State.Taught.Add(id);
            Legend.Teach(items);
            float t0 = Time.time, doneAt = -1f;
            if (Auto) Cap.Log($"teach: '{id}' shown ({string.Join(" · ", Array.ConvertAll(items, x => x.Item1 + " " + x.Item2))})");
            if (Auto)
            {
                for (float w = 0f; !Legend.TeachFull && w < 3f; w += Time.deltaTime) yield return null;
                yield return Shot($"t_{id}_xbox");
                Legend.SetMode(false); Legend.Teach(items);
                yield return null;
                for (float w = 0f; !Legend.TeachFull && w < 3f; w += Time.deltaTime) yield return null;
                yield return Shot($"t_{id}_keys");
                Legend.SetMode(true); Legend.Teach(items);
            }
            while (true)
            {
                if (doneAt < 0f && done()) doneAt = Time.time;
                if (doneAt > 0f && Time.time - doneAt > 1.0f && Time.time - t0 > 2.2f) break;
                // a cutscene takes over: the lesson is over
                if (Seven.Current == ChapterSeven.Mode.Locked && Dlg.Busy && Time.time - t0 > 1f) break;
                yield return null;
            }
            Legend.Teach();
            if (Auto) Cap.Log($"teach: '{id}' cleared after {Time.time - t0:0.0}s ({(doneAt > 0f ? "done" : "cutscene")})");
            _teachId = null; _teachCo = null;
        }

        bool StartTeach(string id, Func<bool> done, params (ControlsLegend.Btn, string)[] items)
        {
            if (Chapter1State.Taught.Contains(id) || _teachCo != null) return false;
            _teachCo = StartCoroutine(TeachRoutine(id, done, items));
            return true;
        }

        /// <summary>Each control is taught the first time it is needed, in the order the level asks for them.</summary>
        IEnumerator TeachWatch()
        {
            while (UI.FadeAlpha > 0.05f) yield return null;
            yield return Wait(1.4f);
            var p0 = Seven.transform.position; float yaw0 = Cam.Yaw, movedAt = -1f;
            StartTeach("move", () =>
            {
                if (movedAt < 0f && Flat(Seven.transform.position - p0).magnitude > 1.2f) movedAt = Time.time;
                return movedAt > 0f && (Mathf.Abs(Mathf.DeltaAngle(Cam.Yaw, yaw0)) > 25f || Time.time - movedAt > 6f);
            }, (ControlsLegend.Btn.Move, "Move"), (ControlsLegend.Btn.Camera, "Camera"));
            while (true)
            {
                if (_teachCo == null)
                {
                    var p = Seven.transform.position;
                    if (_promptLabel == "Look" && PromptNear) StartTeach("interact", () => _promptHit || Dlg.Busy, (ControlsLegend.Btn.Interact, "Look"));
                    else if (OnZone("Corridor") && MoonTraversal.Near(p, MoonTraversal.Jumps[0].From, 1.7f)) StartTeach("jump", () => Seven.LastMove == "jump" || p.x > 10f, (ControlsLegend.Btn.Jump, "Jump"));
                    else if (OnZone("CatwalkB") || (OnZone("PipeRun") && Seven.Crouched)) StartTeach("crouch", () => Seven.Crouched, (ControlsLegend.Btn.Crouch, "Crouch"));
                    else if (OnZone("CatwalkC") && !Seven.Crouched && Seven.transform.position.x > 13.1f) StartTeach("ledge", () => Seven.Current == ChapterSeven.Mode.Hang, (ControlsLegend.Btn.Jump, "Grab the ledge"));
                    else if (Seven.Current == ChapterSeven.Mode.Hang)
                        StartTeach("hang", () => Seven.LastMove == "climb" || (Seven.Current != ChapterSeven.Mode.Hang && Seven.Current != ChapterSeven.Mode.Climb),
                            (ControlsLegend.Btn.ClimbUp, "Climb up"), (ControlsLegend.Btn.Shimmy, "Shimmy"), (ControlsLegend.Btn.LetGo, "Let go"));
                    else if (OnZone("TerraceC") && !_slabMoved) StartTeach("tk", () => _slabMoved, (ControlsLegend.Btn.Tk, "Telekinesis"));
                }
                yield return null;
            }
        }

        static ControlsLegend.Btn BtnOf(string key) => key switch
        {
            "jump" => ControlsLegend.Btn.Jump, "crouch" => ControlsLegend.Btn.Crouch, "up" => ControlsLegend.Btn.ClimbUp,
            "letgo" => ControlsLegend.Btn.LetGo, "lr" => ControlsLegend.Btn.Shimmy, "takedown" => ControlsLegend.Btn.Takedown,
            _ => ControlsLegend.Btn.Interact,
        };

        // ------------------------------------------------------------------------------------------ cat routes
        static ChapterCat.Stop Walk(Vector3 p, Func<bool> go = null) => new ChapterCat.Stop { P = p, Go = go };
        static ChapterCat.Stop HopTo(Vector3 p, Func<bool> go = null, float h = 0.5f) => new ChapterCat.Stop { P = p, Hop = true, H = h, Go = go };

        void CatRoute(string name, List<ChapterCat.Stop> stops)
        {
            Cat.Route(stops);
            _catRoute = name;
            if (Auto) Cap.Log($"cat leads the {name} route ({stops.Count} stops)");
        }
        string _catRoute;

        void OnCatStop(int i, Vector3 at)
        {
            if (!Auto) return;
            var p = Seven.transform.position;
            Cap.Log($"cat waits at {_catRoute} stop {i + 1} ({at.x:0.0}, {at.y:0.0}, {at.z:0.0}); 7 at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}), {Flat(p - at).magnitude:0.0} m away");
        }

        // ------------------------------------------------------------------------------------------ autopilot helpers
        IEnumerator AutoDo(string what, Action press, Func<bool> until, float timeout = 8f)
        {
            Seven.ScriptMove = Vector3.zero;
            press?.Invoke();
            float t0 = Time.time;
            while (!until())
            {
                if (Time.time - t0 > timeout) { Cap.Soft++; Cap.Log($"SOFT-LOCK? traversal '{what}' did not finish in {timeout:0}s (7 at {Seven.transform.position}, {Seven.Current}/{Seven.State}, hang {Seven.HangLedge?.Name} u {Seven.HangU:0.00})"); yield break; }
                yield return null;
            }
            Cap.Log($"traversal ok: {what} ({Time.time - t0:0.0}s)");
            _traversalOk++;
        }
        int _traversalOk;

        /// <summary>Check runs: shimmy (world direction) until a condition, then stop.</summary>
        IEnumerator AutoShimmy(Vector3 dir, Func<bool> until, string what, float timeout = 10f)
        {
            float t0 = Time.time, u0 = Seven.HangU;
            while (!until() && Time.time - t0 < timeout) { Seven.ScriptMove = dir; yield return null; }
            Seven.ScriptMove = Vector3.zero;
            if (!until()) { Cap.Soft++; Cap.Log($"SOFT-LOCK? shimmy '{what}' stuck at u {Seven.HangU:0.00}"); }
            else { Cap.Log($"traversal ok: shimmy {what} ({Mathf.Abs(Seven.HangU - u0):0.00} m in {Time.time - t0:0.0}s)"); _traversalOk++; }
        }

        bool FreeAt(float y) => Seven.Current == ChapterSeven.Mode.Free && Mathf.Abs(Seven.transform.position.y - y) < 0.12f;

        IEnumerator WaitReset(string what)
        {
            int f0 = Seven.Falls;
            float t0 = Time.time;
            while (Seven.Falls == f0 && Time.time - t0 < 6f) yield return null;
            while ((_resettingFall || Seven.Current != ChapterSeven.Mode.Free) && Time.time - t0 < 10f) yield return null;
            bool ok = Seven.Falls > f0 && Seven.Current == ChapterSeven.Mode.Free;
            if (ok) { _falls++; Cap.Log($"deliberate fall ({what}): reset to the last safe ledge {Seven.transform.position} - ok"); }
            else { Cap.Soft++; Cap.Log($"SOFT-LOCK? deliberate fall ({what}) did not reset (falls {Seven.Falls}, {Seven.Current})"); }
        }
        int _falls;

        /// <summary>Check runs: let the teaching prompt for a control appear (and its Xbox / keys shots) before acting.</summary>
        IEnumerator AwaitTeach(string id)
        {
            for (float t0 = Time.time; !Chapter1State.Taught.Contains(id) && Time.time - t0 < 6f;) yield return null;
            if (!Chapter1State.Taught.Contains(id)) { Cap.Log($"TEACH MISSING: '{id}' did not appear"); yield break; }
            yield return Wait(1.35f);
        }

        // ------------------------------------------------------------------------------------------ the routes (check run)
        bool _scene3;
        IEnumerator AutoPrison()
        {
            // the walk-and-talk (scene 3) plays along the corridor first, as when walking at a normal pace
            yield return AutoWalk(new Vector3(6.2f, 0f, 3.3f));
            yield return Until(() => _scene3, "scene 3 starts", 20f);
            yield return Until(() => !_scene3, "scene 3 walk-and-talk done", 90f);
            var gap = MoonTraversal.Jumps[0];
            yield return AutoWalk(new Vector3(8.1f, 0f, 3.3f));
            yield return AwaitTeach("jump");
            // deliberate fall: a plain short hop into the gap (assist off)
            Seven.NoAssist = true; Seven.transform.rotation = Quaternion.LookRotation(Vector3.right);
            Seven.ScriptMove = Vector3.right * 0.3f; yield return null; Seven.ScriptJump = true;
            yield return Wait(0.1f); Seven.ScriptMove = Vector3.zero;
            yield return WaitReset("short hop into the catwalk gap");
            Seven.NoAssist = false;
            yield return AutoWalk(new Vector3(7.85f, 0f, 3.3f));
            yield return AutoDo("jump the catwalk gap", () => Seven.ScriptJump = true, () => FreeAt(0f) && Seven.transform.position.x > 10f);
            yield return Shot("tr01_across_the_gap");
            yield return AutoWalk(new Vector3(10.95f, 0f, 3.3f));
            yield return AwaitTeach("crouch");
            yield return AutoDo("crouch at the pipes", () => Seven.ScriptCrouch = true, () => Seven.Crouched, 3f);
            yield return AutoWalk(new Vector3(12.2f, 0f, 3.3f));
            yield return Shot("tr02_crouched_under_pipes");
            yield return AutoWalk(new Vector3(13.8f, 0f, 3.3f));
            yield return AutoDo("stand up after the pipes", () => Seven.ScriptCrouch = true, () => !Seven.Crouched, 3f);
            yield return AutoWalk(new Vector3(13.55f, 0f, 3.3f));
            yield return AwaitTeach("ledge");
            yield return AutoDo("jump to grab the bulkhead", () => Seven.ScriptJump = true, () => Hanging(MoonTraversal.Bulkhead));
            yield return AwaitTeach("hang");
            yield return Wait(0.7f);
            yield return Shot("tr03_hanging_bulkhead");
            yield return AutoShimmy(Vector3.forward, () => Seven.HangU > 1.45f, "along the bulkhead");
            yield return Shot("tr04_shimmy_bulkhead");
            yield return AutoDo("climb up the bulkhead", () => Seven.ScriptMove = Vector3.right, () => FreeAt(MoonTraversal.BulkheadTop));
            Seven.ScriptMove = Vector3.zero;
            yield return Shot("tr05_on_the_bulkhead");
            yield return AutoWalk(new Vector3(14.95f, MoonTraversal.BulkheadTop, 3.3f));
            yield return AutoDo("jump down from the bulkhead", () => Seven.ScriptJump = true, () => FreeAt(0f) && Seven.transform.position.x > 15.3f);
            yield return AutoWalk(MoonLayout.OldWallSpot);
        }

        IEnumerator AutoPassageRuins()
        {
            yield return AutoWalk(new Vector3(16.5f, 0f, 0.4f));
            StartCoroutine(ShotWhen(() => Seven.State == "wall_climb" && Seven.Tn > 0.55f, "tr06_wall_climb"));
            yield return AutoDo("climb the cracked wall", () => Seven.ScriptJump = true, () => FreeAt(2.8f), 12f);
            yield return AutoWalk(new Vector3(16.5f, 2.8f, -1.95f));
            yield return AutoDo("climb down onto the shaft lip", () => Seven.ScriptCrouch = true, () => Hanging(MoonTraversal.ShaftLip));
            yield return Wait(0.6f);
            yield return Shot("tr07_hanging_shaft_lip");
            // deliberate fall: let go over the dark drop
            yield return AutoShimmy(Vector3.left, () => Seven.transform.position.x < 15.0f, "out over the drop");
            Seven.ScriptLetGo = true;
            yield return WaitReset("let go over the shaft");
            yield return AutoWalk(new Vector3(16.5f, 2.8f, -1.95f));
            yield return AutoDo("climb down onto the shaft lip again", () => Seven.ScriptCrouch = true, () => Hanging(MoonTraversal.ShaftLip));
            StartCoroutine(ShotWhen(() => Seven.Current == ChapterSeven.Mode.Hang && Seven.transform.position.x < 14.2f, "tr08_shimmy_over_the_drop"));
            yield return AutoShimmy(Vector3.left, () => Seven.transform.position.x < 12.5f, "along the shaft lip", 14f);
            yield return AutoDo("let go onto the ruins ledge", () => Seven.ScriptLetGo = true, () => FreeAt(-1.6f));
            yield return Shot("tr09_dropped_into_the_ruins");
            yield return AutoWalk(new Vector3(10.85f, -1.6f, -3.6f));
            yield return AutoDo("jump the ruins gap", () => Seven.ScriptJump = true, () => FreeAt(-1.6f) && Seven.transform.position.x < 9.3f);
            yield return Shot("tr10_ruins_terrace_a");
            yield return AutoWalk(new Vector3(8.2f, -1.6f, -4.35f));
            yield return AutoDo("mantle onto terrace B", () => Seven.ScriptJump = true, () => FreeAt(-0.7f));
            yield return AutoWalk(new Vector3(9.3f, -0.7f, -6.3f));
            yield return Shot("tr11_terrace_b_cat_ahead");
            yield return AutoDo("jump to grab terrace C", () => Seven.ScriptJump = true, () => Hanging(MoonTraversal.TerraceCLip));
            yield return Wait(0.6f);
            yield return Shot("tr12_hanging_terrace_c");
            Seven.ScriptLetGo = true;
            yield return WaitReset("let go over the terrace gap");
            yield return AutoWalk(new Vector3(9.3f, -0.7f, -6.3f));
            yield return AutoDo("jump to grab terrace C again", () => Seven.ScriptJump = true, () => Hanging(MoonTraversal.TerraceCLip));
            yield return AutoShimmy(Vector3.forward, () => Seven.HangU < 0.55f, "north to the clear top");
            yield return AutoDo("climb up terrace C", () => Seven.ScriptMove = Vector3.right, () => FreeAt(0.8f));
            Seven.ScriptMove = Vector3.zero;
            yield return AutoWalk(new Vector3(12.9f, 0.8f, -5.45f));
            yield return Until(() => _slabMoved, "fallen slab moved (telekinesis)", 20f);
            yield return AutoWalk(new Vector3(14.2f, 0.45f, -5.2f), new Vector3(17.9f, MoonLayout.Lower, -5.2f), new Vector3(18.6f, MoonLayout.Lower, -5.9f),
                new Vector3(17.6f, MoonLayout.Lower, -7.3f), new Vector3(16.5f, MoonLayout.Lower, -8.1f), MoonLayout.HallEntry);
        }

        IEnumerator AutoPit()
        {
            yield return AutoWalk(new Vector3(16.5f, MoonLayout.Lower, -38.85f), new Vector3(16.5f, MoonLayout.Lower, -39.12f));
            yield return AutoDo("climb down into the mining cut", () => Seven.ScriptCrouch = true, () => Hanging(MoonTraversal.VisionExitLip));
            yield return Wait(0.5f);
            yield return Shot("tr13_hanging_into_the_cut");
            yield return AutoDo("let go into the cut", () => Seven.ScriptLetGo = true, () => FreeAt(-6.4f));
            yield return AutoWalk(new Vector3(16.5f, -6.4f, -40.75f));
            StartCoroutine(ShotWhen(() => Seven.State == "wall_climb" && Seven.transform.position.y > -5.5f, "tr14_climbing_out"));
            yield return AutoDo("climb out of the cut (two climbs)", () => Seven.ScriptJump = true, () => FreeAt(MoonLayout.Lower) && Seven.transform.position.z < -41.2f, 14f);
            yield return Shot("tr15_out_onto_the_terrace_passage");
            // QF9: the fight starts as soon as she is out of the cut (AutoFight takes over from here)
        }

        IEnumerator AutoBay()
        {
            yield return AutoWalk(new Vector3(21.5f, MoonLayout.Lower, -47f), new Vector3(21.5f, MoonLayout.Lower, -50f), new Vector3(21.5f, -4.3f, -54.62f));
            yield return AutoDo("climb down at the broken ramp", () => Seven.ScriptCrouch = true, () => Hanging(MoonTraversal.RampEnd));
            yield return Wait(0.5f);
            yield return Shot("tr16_hanging_broken_ramp");
            yield return AutoDo("drop onto scaffold 1", () => Seven.ScriptLetGo = true, () => FreeAt(-7.6f));
            yield return AutoWalk(new Vector3(21.5f, -7.6f, -57.8f));
            yield return AutoDo("jump to scaffold 2", () => Seven.ScriptJump = true, () => FreeAt(-7.6f) && Seven.transform.position.z < -59.4f);
            yield return Shot("tr17_scaffold_2");
            yield return AutoWalk(new Vector3(21.5f, -7.6f, -62.05f));
            yield return AutoDo("climb down the scaffold", () => Seven.ScriptCrouch = true, () => Hanging(MoonTraversal.ScaffoldLip));
            yield return AutoDo("drop to the bay floor", () => Seven.ScriptLetGo = true, () => FreeAt(MoonLayout.Bay));
            yield return Shot("tr18_down_in_the_bay");
            yield return AutoWalk(new Vector3(21.5f, MoonLayout.Bay, -66f), new Vector3(21.5f, MoonLayout.Bay, -72f), MoonLayout.LeverSpot);
        }

        IEnumerator ShotWhen(Func<bool> cond, string name, float timeout = 20f)
        {
            for (float t0 = Time.time; !cond() && Time.time - t0 < timeout;) yield return null;
            if (cond()) yield return Shot(name);
        }

        // ------------------------------------------------------------------------------------------ the fallen slab (telekinesis)
        IEnumerator SlabSequence()
        {
            yield return Until(() => OnZone("TerraceC"), "ruins: up on terrace C", 400f);
            Objective("Clear the fallen slab");
            Prompt("Telekinesis", MoonTraversal.SlabRest, MoonTraversal.SlabTkRange, true);
            var slabR = Slab != null ? Slab.GetComponent<Renderer>() : null;
            bool pulsing = true;
            IEnumerator Pulse() { for (float t = 0f; pulsing; t += Time.deltaTime) { Glow(slabR, 0.12f + 0.1f * Mathf.Sin(t * 3f)); yield return null; } Glow(slabR, 0f); }
            StartCoroutine(Pulse());
            if (Auto) { yield return Until(() => PromptNear, "slab in telekinesis range", 20f); yield return AwaitTeach("tk"); yield return Shot("tr_slab_highlighted"); }
            yield return WaitPrompt("lift the fallen slab");
            pulsing = false;
            Seven.Gesture(MoonTraversal.SlabRest);
            Seven.SetEnergyPoint(1.9f, 0.25f);                  // the energy point brightens while she uses it
            while (Seven.Current == ChapterSeven.Mode.Gesture && !Seven.GesturePeak) yield return null;
            TkStream(MoonTraversal.SlabRest);
            AudioKit.Play("slab_lift", MoonTraversal.SlabRest, 0.9f, 0.05f, AudioKit.Bus.Sfx, 25f);
            var a = Slab.position; var r0 = Slab.rotation; var b = MoonTraversal.SlabAway;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.3f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Slab.position = Vector3.Lerp(a, b, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.5f;
                Slab.rotation = r0 * Quaternion.Euler(u * 25f, u * 40f, u * -15f);
                if (Auto && t > 0.55f && t - Time.deltaTime / 1.3f <= 0.55f) StartCoroutine(Shot("tr_slab_lifted_energy_point_bright"));
                yield return null;
            }
            Seven.SetEnergyPoint(1f, 1.2f);
            AudioKit.Play("stone_fall", b + Vector3.down * 6f, 0.8f, 0.05f, AudioKit.Bus.Sfx, 30f);
            for (float t = 0f, v = 0f; t < 1.4f; t += Time.deltaTime) { v += 12f * Time.deltaTime; Slab.position += Vector3.down * v * Time.deltaTime; Slab.Rotate(Vector3.right, 140f * Time.deltaTime, Space.World); yield return null; }
            Slab.gameObject.SetActive(false);
            SetZone("Stair", true);
            _slabMoved = true;
            ClearPrompt();
            Objective("Follow the old passage");
            if (Auto) Cap.Log($"fallen slab moved by telekinesis; energy point during the lift up to 1.9 (now {Seven.EnergyPoint:0.00}); stair open");
        }

        // ------------------------------------------------------------------------------------------ the vision shape
        float _shapeGlow = ShapeGlowIdle;
        /// <summary>8b addition: how far each door leaf slides into the wall along the S-seam's sides.</summary>
        const float LeafOpen = 2.0f;
        IEnumerator ShapeIdle()
        {
            if (Shape == null) yield break;
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime;
                if (_shapeTaken) { yield return null; continue; }
                Shape.rotation = Quaternion.Euler(12f * Mathf.Sin(t * 0.21f), t * 9f, 8f * Mathf.Sin(t * 0.17f + 1f));
                Shape.position = MoonLayout.ShapeAt + Vector3.up * Mathf.Sin(t * 0.8f) * 0.012f;
                Glow(InscriptionGlow, _shapeGlow * (1f + 0.14f * Mathf.Sin(t * 1.6f)));
                if (ShapeLight != null) ShapeLight.intensity = 0.5f * _shapeGlow * (1f + 0.1f * Mathf.Sin(t * 1.6f));
                yield return null;
            }
        }

        // ------------------------------------------------------------------------------------------ fx
        ParticleSystem MakeMotes()
        {
            var go = new GameObject("TkMotes");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
            main.startColor = Color.white;
            main.gravityModifier = -0.03f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1500;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = new Material(Additive);
            m.SetTexture("_MainTex", FxUtil.Dot(64));
            m.SetFloat("_Intensity", 1.8f);
            r.sharedMaterial = m;
            ps.Play();
            return ps;
        }

        static void Show(GameObject go, bool on) { foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.enabled = on; }

        void Burst(Vector3 at, int n, float radius)
        {
            var sh = _motes.shape; sh.radius = radius;
            _motes.transform.position = at;
            _motes.Emit(n);
        }

        void TkStream(Vector3 target) => StartCoroutine(TkStreamRoutine(target));
        IEnumerator TkStreamRoutine(Vector3 target)
        {
            AudioKit.Play("tk_gather", Seven.transform.position + Vector3.up * 1.2f, 0.7f, 0.04f, AudioKit.Bus.Sfx, 20f);
            AudioKit.Play("tk_push", target, 0.9f, 0.05f, AudioKit.Bus.Sfx, 25f);
            var hand = Seven.Bone(HumanBodyBones.RightHand);
            var sh = _motes.shape; sh.radius = 0.08f;
            for (float t = 0f; t < 0.7f; t += Time.deltaTime)
            {
                var from = hand != null ? hand.position : Seven.transform.position + Vector3.up * 1.3f;
                for (int i = 0; i < 4; i++)
                {
                    _motes.transform.position = Vector3.Lerp(from, target, UnityEngine.Random.value);
                    _motes.Emit(1);
                }
                yield return null;
            }
            Burst(target, 40, 0.4f);
        }
    }
}
