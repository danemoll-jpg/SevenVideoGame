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

        bool Auto => DemoBoot.CaptureDir != null;
        ChapterCapture Cap => ChapterCapture.I;
        ParticleSystem _motes;
        string _promptLabel; bool _promptTk; Vector3 _promptAt; float _promptR; bool _promptHit;
        bool _stoneDown, _inHall, _resetting;
        int _spotted;
        readonly Dictionary<Renderer, Material> _mats = new Dictionary<Renderer, Material>();

        // ------------------------------------------------------------------------------------------ setup
        void Start()
        {
            if (Chapter1State.StartedAt < 0f) Chapter1State.StartedAt = Time.time;
            _motes = MakeMotes();
            Legend.SetLayout(ControlsLegend.Layout.Chapter);
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
            SetupCheckpoint(Chapter1State.Checkpoint);
            SetupAudio();
            if (Auto) { AudioKit.Log = s => Cap.Log("audio: " + s); LineOfSightCheck(); }
            StartCoroutine(Flow());
        }

        // ------------------------------------------------------------------------------------------ Round 7 audio
        AudioSource _hum, _amb, _saucerHum;
        void SetupAudio()
        {
            int cp = Chapter1State.Checkpoint;
            if (cp < 4 || !_cityLook) { _amb = AudioKit.Loop("amb_cavern", null, null, 1f, AudioKit.Bus.Ambience); MusicSystem.Play("moon_prison", 2f); }
            else { _amb = AudioKit.Loop("amb_city", null, null, 1f, AudioKit.Bus.Ambience); MusicSystem.Play("moon_prison", 2f); }
            if (cp == 0 && LaserDoor != null) _hum = AudioKit.Loop("laser_hum", LaserDoor, null, 0.7f, AudioKit.Bus.Sfx, 1.2f, 14f);
            var f7 = Seven.gameObject.AddComponent<Footsteps>();
            f7.Stride = 0.75f; f7.Volume = 0.5f;
            f7.Surface = () => { var p = Seven.transform.position; var z = Ground.ZoneName(p.x, p.z); return z == "Cell" || z == "CellDoor" || z == "Corridor" || z == "Alcove" || z == "RampTop" || z == "Ramp" ? "step_metal_" : "step_rock_"; };
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
            bool doorOpen = cp >= 1, wallOpen = cp >= 2, sealedOpen = cp >= 4;
            _cityLook = cp >= 4;
            SetZone("CellDoor", doorOpen);
            SetZone("Alcove", doorOpen);
            SetZone("Passage0", wallOpen); SetZone("Passage1", wallOpen);
            SetZone("VisionExit", sealedOpen);
            Ground.IgnoreObstacle = -1;
            Show(Seven.gameObject, true); Show(Cat.gameObject, true);
            foreach (var b in LaserBeams) if (b != null) b.enabled = !doorOpen;
            if (LaserLight != null) LaserLight.enabled = !doorOpen;
            Seven.SetEnergyPoint(doorOpen ? 1f : 0f);       // the laser field dampens her powers: dark inside the cell
            if (_cityLook) StartCoroutine(CityLook(0f));
            OldWallSlab.localPosition = wallOpen ? new Vector3(0f, -2.7f, 0f) : Vector3.zero;
            SealedDoorSlab.localPosition = sealedOpen ? new Vector3(0f, -3.3f, 0f) : Vector3.zero;
            VisionWhole.SetActive(false); VisionRuins.SetActive(true);
            Glow(InscriptionGlow, sealedOpen ? 0.6f : 1.4f);
            Glow(ReleaseLight, 0.8f);
            foreach (var g in GateGuards) g.Stand();
            Guard.ResetToPost();
            Stone.position = MoonLayout.StoneRest; _stoneDown = false;
            foreach (var r in RimGlow) Glow(r, 0.9f);
            Vector3 p7, f7, pc;
            switch (cp)
            {
                case 0: p7 = MoonLayout.SevenStart; f7 = Vector3.forward; pc = MoonLayout.VentMouth; break;
                case 1: p7 = new Vector3(0f, 0f, 2.9f); f7 = Vector3.right; pc = new Vector3(1f, 0f, 3.5f); break;
                case 2: p7 = new Vector3(16.5f, 0f, 1.2f); f7 = Vector3.back; pc = new Vector3(16.9f, 0f, 2.3f); break;
                case 3: p7 = new Vector3(16.5f, MoonLayout.Lower, -26.5f); f7 = Vector3.back; pc = new Vector3(16.9f, MoonLayout.Lower, -25.4f); break;
                default: p7 = new Vector3(16.5f, MoonLayout.Lower, -40.5f); f7 = Vector3.back; pc = new Vector3(17f, MoonLayout.Lower, -39.5f); break;
            }
            Seven.Place(p7, f7);
            Cat.Place(pc, f7);
            if (cp > 0) Cat.Follow();
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
            if (Auto) Cap.Log($"ok: {what} ({Time.time - t0:0.0}s)");
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

        void Objective(string s) { UI.SetObjective(s); if (Auto) Cap.Log("objective: " + s); }

        void Prompt(string label, Vector3 at, float radius, bool tk = false)
        {
            _promptLabel = label; _promptAt = at; _promptR = radius; _promptTk = tk; _promptHit = false;
        }
        void ClearPrompt() { _promptLabel = null; _promptHit = false; Legend.Prompt(null); Legend.PromptTk(null); }
        bool PromptNear => _promptLabel != null && Dist7(_promptAt) < _promptR && Seven.Current == ChapterSeven.Mode.Free && !Dlg.Busy;
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

        IEnumerator Cutscene(bool on)
        {
            Seven.Lock(on);
            if (!on) { Cam.Follow(); if (Auto) Seven.ScriptMove = Vector3.zero; }
            yield return null;
        }

        IEnumerator Shot(string name) { if (Auto) { ShotFrameCheck(name); yield return Cap.Shot(name); } }
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
                if (Cat.Current == ChapterCat.Mode.Follow || Cat.Current == ChapterCat.Mode.Lead)
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
            Legend.Prompt(near && !_promptTk ? _promptLabel : null);
            Legend.PromptTk(near && _promptTk ? _promptLabel : null);
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
            if (cp <= 0) yield return Scene1And2();
            if (cp <= 1) yield return Scene3And4();
            if (cp <= 2) yield return Scene5();
            if (cp <= 3) yield return Scene6();
            yield return Scene7And8();
        }

        void Checkpoint(int n) { Chapter1State.Checkpoint = n; if (Auto) Cap.Log($"checkpoint {n}"); }

        IEnumerator Scene1And2()
        {
            UI.SetFade(1f);
            Seven.Lock(true);
            StartCoroutine(UI.Title("CHAPTER 1  —  THE MOON PRISON", 1.0f, 2.0f, 1.0f));
            yield return Wait(0.8f);
            yield return UI.Fade(0f, 1.8f);
            Seven.Lock(false);
            yield return Shot("s01_cell_start");
            if (Auto) yield return LaserCollisionCheck();
            // The cat slips out of the small opening and sits outside the cell, watching her.
            yield return Wait(1.5f);
            Cat.GoTo(MoonLayout.CatWaitOutside, Cat.StrollSpeed, MoonLayout.DoorApproach);
            Objective("Look around");
            if (Auto) yield return Until(() => Cat.Arrived, "cat at the cell door", 20f);
            Objective("Go to the cell door");
            if (Auto) yield return AutoWalk(MoonLayout.DoorApproach);
            yield return Until(() => Dist7(MoonLayout.DoorApproach) < 0.75f && Cat.Arrived, "7 at the door", 40f);

            // Scene 1: first contact (through the laser beams).
            yield return Cutscene(true);
            Objective(null);
            Cam.Shot(new Vector3(0.45f, 1.75f, 4.3f), new Vector3(-0.05f, 1.3f, 0.9f), 1.4f);
            yield return Wait(1.0f);
            yield return Shot("s01_laser_door_on");
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
            yield return Until(() => Cat.Arrived && !Dlg.Busy, "cat in the guard alcove (out of sight)", 25f);
            Seven.Lock(true);
            // From inside the cell: 7 and the beams; the cat is somewhere off to the left, out of sight.
            Cam.Shot(new Vector3(0.85f, 1.55f, -1.25f), new Vector3(0.05f, 1.15f, 1.6f), 1.0f);
            Objective(null);
            Cat.Hop(MoonLayout.CrateTop, 0.8f, 0.5f);
            yield return Until(() => Cat.Arrived, "cat on the crate in the alcove", 6f);
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
            yield return Until(() => Cat.Arrived, "cat down from the crate", 4f);
            Cat.Lead(new List<Vector3> { MoonLayout.AlcoveMouth, new Vector3(-0.4f, 0f, 2.9f) }, 99f);
            Cam.Follow();
            Seven.Lock(false);
            Objective("Step out");
            if (Auto) yield return FaceShot("s02_energy_point_dark_closeup");
            if (Auto) yield return AutoWalk(new Vector3(0f, 0f, 1.6f), new Vector3(0.1f, 0f, 2.8f));
            yield return Until(() => Seven.transform.position.z > 2.2f, "7 steps out", 40f);
            yield return Cutscene(true);
            yield return Until(() => Cat.Arrived, "cat back from the alcove", 8f);
            Cat.Hold(Seven.transform.position);
            var face = Flat(Cat.transform.position - Seven.transform.position);
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(face), 360f * Time.deltaTime); yield return null; }
            Cam.Shot(new Vector3(1.6f, 1.75f, 4.4f), new Vector3(-0.2f, 1.0f, 2.7f), 1.1f);
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
            Cam.Shot(c + Seven.transform.forward * 0.75f + Vector3.up * 0.05f, c, 0.01f, true, 40f);
            yield return null;
            yield return Shot(name);
            if (!wasShot) Cam.Follow();
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
            Objective("Follow the cat");
            Cat.Lead(new List<Vector3>(MoonLayout.CorridorLead), 5f);
            if (Auto) StartCoroutine(AutoWalk(new Vector3(3f, 0f, 3.2f), MoonLayout.SafeSpot, new Vector3(14.2f, 0f, 3.2f)));
            yield return Until(() => Seven.transform.position.x > MoonLayout.Scene3TriggerX, "corridor: scene 3 trigger", 40f);
            // Scene 3 as a walk-and-talk (the first safe pause after the cell).
            yield return Dlg.PlayScene(3);
            yield return Until(() => Seven.transform.position.x > 13.2f, "corridor: near the guard post", 40f);
            UI.Hint("Guards at the gate ahead.", 3.5f);
            yield return Shot("s04_guarded_route");
            Objective("Find another way out");
            Cat.GoTo(new Vector3(15.4f, 0f, 3.4f), Cat.TrotSpeed, MoonLayout.OldWall);
            Prompt("Look closer", MoonLayout.OldWallSpot, 1.5f);
            if (Auto) yield return AutoWalk(MoonLayout.OldWallSpot);
            yield return WaitPrompt("look closer at the older wall");

            // Scene 4.
            yield return Cutscene(true);
            Objective(null);
            Cam.Shot(new Vector3(18.9f, 1.8f, 3.9f), new Vector3(16.3f, 1.3f, 2.1f), 1.2f);
            Seven.Touch(MoonLayout.OldWall);
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
            Cat.Follow();
        }

        IEnumerator Scene5()
        {
            Objective("Follow the old passage");
            if (Auto) StartCoroutine(AutoWalk(new Vector3(16.5f, 0f, 1.0f), new Vector3(16.5f, MoonLayout.Lower, -8.4f), MoonLayout.HallEntry));
            yield return Until(() => Seven.transform.position.z < MoonLayout.HallEnterZ, "ancient hall entrance", 40f);
            yield return Cutscene(true);
            Cam.Shot(new Vector3(17.6f, MoonLayout.Lower + 2.6f, -7.6f), new Vector3(16.4f, MoonLayout.Lower + 1.3f, -19f), 1.2f);
            yield return Dlg.PlayScene(5, 1, 3);
            yield return Cutscene(false);
            Objective("Get past the Scree guard without hurting anyone");
            UI.Hint("Telekinesis (X / T): knock the loose stone off the broken pillar.", 6f);
            _inHall = true;
            StartCoroutine(Watch());
            Prompt("Telekinesis: loose stone", MoonLayout.StoneRest, MoonLayout.TkRange, true);
            StartCoroutine(StoneLoop());
            if (Auto)
            {
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
                yield return Until(() => Guard.Current == ScreeGuard.Mode.Studying, "guard studies the noise", 30f);
                yield return Shot("s05_guard_distracted");
                yield return AutoWalk(MoonLayout.SneakPath);
            }
            yield return Until(() => Seven.transform.position.z < MoonLayout.HallExitZ, "slipped past the guard", 400f);
            _inHall = false;
            ClearPrompt();
            Seven.Sneak = false;
            yield return Dlg.PlayScene(5, 4, 4);
            Checkpoint(3);
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
                    while (Seven.Current == ChapterSeven.Mode.Gesture && !Seven.GesturePeak) yield return null;
                    if (Seven.GesturePeak)
                    {
                        TkStream(MoonLayout.StoneRest);
                        yield return FallStone();
                    }
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

        IEnumerator Scene6()
        {
            Objective("Keep going");
            Cat.Follow();
            Prompt("Touch", MoonLayout.InscriptionSpot, 1.4f);
            if (Auto) yield return AutoWalk(new Vector3(16.5f, MoonLayout.Lower, -30f), MoonLayout.InscriptionSpot);
            yield return Until(() => Seven.transform.position.z < -30f, "vision chamber", 40f);
            Objective("Look at the glowing inscription");
            yield return WaitPrompt("touch the inscription");
            Objective(null);

            yield return Cutscene(true);
            Cat.GoTo(MoonLayout.InscriptionSpot + new Vector3(1.1f, 0f, 0.9f), Cat.StrollSpeed, Seven.transform.position);
            Cam.Shot(new Vector3(19.7f, MoonLayout.Lower + 2.5f, -34.2f), new Vector3(16.5f, MoonLayout.Lower + 1.5f, -37.4f), 1.0f);
            Seven.Touch(MoonLayout.Inscription);
            yield return Wait(1.3f);
            yield return Shot("s06_vision_before");
            // Wash to white; the ruins appear whole, with pale glowing figures; the cat is heard faintly, far away.
            // The music fades out: the vision plays no track, only its own shimmer.
            MusicSystem.Stop(1.2f);
            AudioKit.Play("vision_whoosh", null, 0.9f);
            var shimmer = AudioKit.Loop("vision_shimmer", null, null, 0f, AudioKit.Bus.Sfx);
            if (shimmer != null) StartCoroutine(AudioKit.Fade(shimmer, 0.7f, 1.5f));
            if (_amb != null) StartCoroutine(AudioKit.Fade(_amb, 0.25f, 1.0f));
            Glow(InscriptionGlow, 6f);
            yield return UI.Fade(1f, 0.9f, 1f);
            VisionRuins.SetActive(false); VisionWhole.SetActive(true);
            Show(Cat.gameObject, false);
            Cam.Shot(new Vector3(16.5f, MoonLayout.Lower + 2.3f, -30.2f), new Vector3(16.5f, MoonLayout.Lower + 1.6f, -37.5f), 4f, true);
            var ghostRoutine = StartCoroutine(Ghosts());
            yield return UI.Fade(0.12f, 1.1f, 1f);
            float visionStart = Time.time;
            StartCoroutine(Dlg.PlayLines(Dlg.Bank.Scene(6, 1, 2), true));   // "What is it?" ... "Can you hear me?"
            yield return Wait(3.2f);
            yield return Shot("s06_vision_during");
            yield return Until(() => !Dlg.Busy && Time.time - visionStart > 7.6f, "vision length", 20f);
            yield return UI.Fade(1f, 0.8f, 1f);
            AudioKit.Play("vision_whoosh", null, 0.6f);
            if (shimmer != null) StartCoroutine(AudioKit.Fade(shimmer, 0f, 1.6f, true));
            if (_amb != null) StartCoroutine(AudioKit.Fade(_amb, 1f, 2.5f));
            StopCoroutine(ghostRoutine);
            VisionWhole.SetActive(false); VisionRuins.SetActive(true);
            Show(Cat.gameObject, true);
            Glow(InscriptionGlow, 1.4f);
            Seven.Release(); Seven.Lock(true);
            Cam.Shot(new Vector3(19.7f, MoonLayout.Lower + 2.5f, -34.0f), new Vector3(16.4f, MoonLayout.Lower + 1.5f, -37.0f), 0.6f, true);
            if (Auto) Cap.Log($"vision lasted {Time.time - visionStart:0.0}s (white wash in/out included)");
            yield return UI.Fade(0f, 1.2f, 1f);
            yield return Shot("s06_vision_after");
            yield return Dlg.PlayScene(6, 3, 12);
            // The gesture she saw in the vision opens the passage.
            Seven.Release();
            Seven.Gesture(MoonLayout.SealedDoor + Vector3.up * 1.2f);
            while (!Seven.GesturePeak) yield return null;
            Glow(InscriptionGlow, 7f);
            TkStream(MoonLayout.Inscription);
            Burst(MoonLayout.SealedDoor + Vector3.up * 1.0f, 80, 1.3f);
            AudioKit.Play("stone_door", MoonLayout.SealedDoor + Vector3.up, 1f, 0f, AudioKit.Bus.Sfx, 25f);
            var from = SealedDoorSlab.localPosition; var to = new Vector3(0f, -3.3f, 0f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.6f) { SealedDoorSlab.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            SetZone("VisionExit", true);
            Glow(InscriptionGlow, 0.6f);
            yield return Shot("s06_passage_opens");
            Seven.Release(); Seven.Lock(true);
            yield return Dlg.PlayScene(6, 13, 15);
            MusicSystem.Play("moon_prison", 4f);
            yield return Cutscene(false);
            Cat.Follow();
            Checkpoint(4);
        }

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

        IEnumerator Scene6bReveal()
        {
            Objective("Find the way out");
            StartCoroutine(WatchCityBlend());
            if (Auto) StartCoroutine(AutoWalk(new Vector3(16.5f, MoonLayout.Lower, -41f), new Vector3(16.5f, MoonLayout.Lower, -43.8f)));
            yield return Until(() => Seven.transform.position.z < MoonLayout.ViewpointZ, "out onto the terrace", 40f);
            // Emergence: no objective, no joke. She walks a few steps, then turns back to see where they escaped.
            yield return Cutscene(true);
            Objective(null);
            MusicSystem.Play("main_theme", 4f);
            StartCoroutine(SwapAmbience("amb_city", 3f));
            Seven.ScriptDriven = true;
            Cat.GoTo(MoonLayout.RevealSpot + new Vector3(1.1f, 0f, 0.6f), Cat.StrollSpeed, MoonLayout.RevealSpot);
            Cam.Shot(new Vector3(18.2f, MoonLayout.Lower + 1.5f, -48.6f), new Vector3(16.4f, MoonLayout.Lower + 1.2f, -43f), 1.6f);
            Seven.Lock(false);
            for (float t0 = Time.time; Dist7(MoonLayout.RevealSpot) > 0.2f && Time.time - t0 < 6f;)
            { Seven.ScriptMove = Flat(MoonLayout.RevealSpot - Seven.transform.position).normalized * 0.7f; yield return null; }
            Seven.ScriptMove = Vector3.zero;
            Seven.Lock(true);
            Seven.ScriptDriven = Auto;
            // turns back toward the colossal structure the prison is bolted into
            var back = Vector3.forward;
            for (float t = 0f; t < 1.4f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(back), 160f * Time.deltaTime); yield return null; }
            Seven.LookAround(0.8f);
            // Shot A: wide from out over the bay - 7 tiny on the terrace, the host rising above, the city around.
            var aPos = new Vector3(-46f, -4f, -150f);
            Cam.Shot(aPos, new Vector3(14f, 40f, -40f), 0.01f, true, 54f);
            StartCoroutine(LookAlong(aPos, new Vector3(14f, 40f, -40f), new Vector3(10f, 110f, -20f), 8f, 54f));
            yield return Wait(3.0f);
            yield return Shot("c01_terrace_wide_before_dialogue");
            yield return Wait(2.8f);                                 // several seconds of only sound and the theme
            // "All this was above us?" / "Most of it." - looking up the host: only one of many.
            var bPos = new Vector3(14.2f, MoonLayout.Lower + 0.8f, -47.3f);
            StartCoroutine(LookAlong(bPos, Seven.transform.position + new Vector3(0f, 1.6f, 0f), new Vector3(-340f, 520f, -40f), 6.5f, 62f));
            yield return RevealLines(1, 2, 1, 2);
            yield return Shot("c02_looking_up_one_of_many");
            // She follows the rising curve of the city: she turns to the open side and the camera tilts up along it.
            for (float t = 0f; t < 1.4f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(new Vector3(-0.15f, 0f, -1f)), 150f * Time.deltaTime); yield return null; }
            var cPos = new Vector3(17.6f, 5.5f, -42.5f);           // above and behind her, clear of the railing
            yield return LookAlong(cPos, new Vector3(16f, -170f, -800f), new Vector3(-200f, 1900f, -4300f), 7.5f, 62f);
            yield return Shot("c03_up_the_curve");
            yield return RevealLines(3, 4, 3, 4);                     // "Someone built this." / "Yes."
            // The quiet centre: pyramids around the dark depression.
            Cam.Shot(new Vector3(16f, 30f, -62f), MoonLayout.BasinCenter + new Vector3(-60f, -10f, 0f), 0.01f, true, 30f);
            yield return Wait(2.2f);
            yield return Shot("c04_pyramids_and_depression");
            // A distant section of machinery starts moving; stone falls from an ancient terrace.
            if (City != null) City.StartMachinery();
            Cam.Shot(new Vector3(16f, 30f, -62f), MoonLayout.FallingStoneAt + new Vector3(0f, -70f, 12f), 0.01f, true, 30f);
            yield return Wait(1.6f);
            if (City != null) StartCoroutine(City.DropStone());
            yield return Wait(2.6f);
            yield return Shot("c05_stone_falls");
            yield return Wait(1.0f);
            if (RevealSet == "primary") yield return Dlg.PlayScene(65, 5, 5);      // "And they're taking it apart."
            else yield return Wait(1.5f);
            // Scene 7: she spots the impound bay holding her saucer, small but recognisable by its rim glow.
            for (float t = 0f; t < 0.8f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(MoonLayout.SaucerCentre - Seven.transform.position)), 200f * Time.deltaTime); yield return null; }
            Cam.Shot(Seven.transform.position + new Vector3(-1.3f, 1.9f, 1.6f), MoonLayout.SaucerCentre + Vector3.up * 1.2f, 1.2f, false, 34f);
            yield return Wait(1.4f);
            yield return Shot("c06_saucer_spotted");
            yield return Dlg.PlayScene(7);
            Seven.Release();
            yield return Cutscene(false);
            Checkpoint(4);
        }

        IEnumerator WatchCityBlend()
        {
            while (!_cityLook)
            {
                if (Seven.transform.position.z < MoonLayout.CityBlendZ) { yield return CityLook(3.5f); yield break; }
                yield return null;
            }
        }

        // ------------------------------------------------------------------------------------------ 7-8: bay, beam
        IEnumerator Scene7And8()
        {
            yield return Scene6bReveal();
            MusicSystem.Play("moon_prison", 5f);
            Objective("Get down to the bay and release the saucer");
            Cat.Lead(new List<Vector3> { new Vector3(21.5f, MoonLayout.Lower, -47f), new Vector3(21.5f, MoonLayout.Lower, -50f), new Vector3(21.5f, MoonLayout.Bay, -70.6f), new Vector3(21f, MoonLayout.Bay, -72.4f) }, 6f);
            Prompt("Telekinesis: clamp release lever", MoonLayout.Lever, 8.5f, true);
            if (Auto) yield return AutoWalk(new Vector3(21.5f, MoonLayout.Lower, -47f), new Vector3(21.5f, MoonLayout.Lower, -50f), new Vector3(21.5f, MoonLayout.Bay, -70.4f), MoonLayout.LeverSpot);
            yield return Until(() => Seven.transform.position.y < MoonLayout.Bay + 0.5f, "down in the bay", 60f);
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
            Cam.Shot(MoonLayout.BeamCentre + new Vector3(6.6f, 1.7f, 4.6f), MoonLayout.BeamCentre + new Vector3(1.4f, 1.6f, 0f), 1.2f, false, 50f);
            yield return Dlg.PlayScene(8);
            Checkpoint(4);
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
            if (Auto) Cap.Log($"moon level done in {Time.time - Chapter1State.StartedAt:0.0}s game time; lines so far {Chapter1State.Played.Count}; cat max distance while following/leading {Cap.CatMaxBehind:0.00} m; cat catch-ups {Cat.CatchUps}; 7 footsteps {Seven.GetComponent<Footsteps>()?.Count}");
            Chapter1State.Aboard = true;
            SaveGame.SaveChapter1Aboard();             // Round 7: Continue = explore the saucer after the chapter
            MusicSystem.Play("aboard_saucer", 3f);
            SceneManager.LoadScene("Saucer");
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
