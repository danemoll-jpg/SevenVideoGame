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
        public Transform DoorBars;
        public Renderer DoorField;
        public Renderer ReleaseLight;
        public Transform OldWallSlab;
        public ScreeGuard Guard;
        public ScreeGuard[] GateGuards;
        public Transform Stone;
        public Renderer InscriptionGlow;
        public GameObject VisionWhole, VisionRuins;
        public Transform SealedDoorSlab;
        public Transform Saucer, SaucerRamp, Lever;
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
            StartCoroutine(Flow());
        }

        Material Mat(Renderer r) { if (!_mats.TryGetValue(r, out var m)) { m = r.material; _mats[r] = m; } return m; }
        void Glow(Renderer r, float v) { if (r == null) return; var m = Mat(r); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white * v); }

        void SetZone(string name, bool on) { var z = Ground.FindZone(name); if (z != null) z.Enabled = on; }

        /// <summary>World state for a checkpoint (doors, walls, guards, positions).</summary>
        void SetupCheckpoint(int cp)
        {
            bool doorOpen = cp >= 1, wallOpen = cp >= 2, sealedOpen = cp >= 4;
            SetZone("CellDoor", doorOpen);
            SetZone("Passage0", wallOpen); SetZone("Passage1", wallOpen);
            SetZone("VisionExit", sealedOpen);
            SetZone("SaucerRamp", false);
            Ground.IgnoreObstacle = -1;
            Show(Seven.gameObject, true); Show(Cat.gameObject, true);
            DoorBars.localPosition = doorOpen ? new Vector3(0f, 2.6f, 0f) : Vector3.zero;
            DoorField.gameObject.SetActive(!doorOpen);
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

        IEnumerator Shot(string name) { if (Auto) yield return Cap.Shot(name); }

        readonly Dictionary<string, string> _shotOnLine = new Dictionary<string, string>
        {
            { "s01_03_cat", "s01_first_contact" }, { "s02_03_cat", "s02_cat_at_release" }, { "s02_06_7", "s02_door_open_which_way" },
            { "s03_05_7", "s03_walk_and_talk" }, { "s04_02_cat", "s04_older_wall" }, { "s05_01_cat", "s05_scree_guard" },
            { "s05_04_7", "s05_slipped_past" }, { "s06_02_cat", "s06_vision_during_far_voice" }, { "s06_07_7", "s06_after_vision" },
            { "s07_01_7", "s07_saucer_from_ledge" }, { "s08_02_cat", "s08_at_the_ramp" },
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
            if (!Auto)
            {
                if (Input.Menu.WasPressedThisFrame()) { Chapter1State.Reset(); SceneManager.LoadScene(DemoMenu.SceneName); return; }
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
            // The cat slips out of the small opening and sits outside the cell, watching her.
            yield return Wait(1.5f);
            Cat.GoTo(MoonLayout.CatWaitOutside, Cat.StrollSpeed, MoonLayout.DoorApproach);
            Objective("Look around");
            if (Auto) yield return Until(() => Cat.Arrived, "cat at the cell door", 20f);
            Objective("Go to the cell door");
            if (Auto) yield return AutoWalk(MoonLayout.DoorApproach);
            yield return Until(() => Dist7(MoonLayout.DoorApproach) < 0.75f && Cat.Arrived, "7 at the door", 40f);

            // Scene 1: first contact.
            yield return Cutscene(true);
            Objective(null);
            Cam.Shot(new Vector3(0.45f, 1.75f, 4.3f), new Vector3(-0.05f, 1.3f, 0.9f), 1.4f);
            yield return Dlg.PlayScene(1);
            Checkpoint(0);

            // Scene 2: the cat opens the cell.
            yield return Dlg.PlayScene(2, 1, 1);            // "Stand back from the door."
            Cam.Follow();
            Seven.Lock(false);
            Objective("Stand back from the door");
            StartCoroutine(Dlg.PlayScene(2, 2, 3));        // "You know how to open it?" / "I wouldn't have asked..."
            Cat.GoTo(MoonLayout.CrateFoot, Cat.TrotSpeed, MoonLayout.CrateTop);
            if (Auto) yield return AutoWalk(new Vector3(0.1f, 0f, -0.35f));
            yield return Until(() => Seven.transform.position.z < 0.15f, "7 stands back", 40f);
            yield return Until(() => Cat.Arrived && !Dlg.Busy, "cat at the crate", 20f);
            Seven.Lock(true);
            Cam.Shot(new Vector3(-0.9f, 1.7f, 4.6f), new Vector3(0.9f, 0.9f, 2.0f), 1.0f);
            Cat.Hop(MoonLayout.CrateTop, 0.8f, 0.55f);
            yield return Until(() => Cat.Arrived, "cat on the crate", 6f);
            yield return Wait(0.4f);
            Cat.Paw();
            yield return Wait(0.7f);
            Glow(ReleaseLight, 4f);
            yield return Shot("s02_cat_paws_release");
            yield return OpenCellDoor();
            Glow(ReleaseLight, 0.8f);
            Cat.Hop(new Vector3(0.9f, 0f, 3.2f), 0.7f, 0.35f);
            yield return Wait(0.8f);
            Cat.Hold(MoonLayout.CellDoor + Vector3.forward * 0.6f);
            Cam.Follow();
            Seven.Lock(false);
            Objective("Step out");
            if (Auto) yield return AutoWalk(new Vector3(0f, 0f, 1.6f), new Vector3(0.1f, 0f, 2.8f));
            yield return Until(() => Seven.transform.position.z > 2.2f, "7 steps out", 40f);
            yield return Cutscene(true);
            var face = Flat(Cat.transform.position - Seven.transform.position);
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(face), 360f * Time.deltaTime); yield return null; }
            Cam.Shot(new Vector3(2.9f, 1.8f, 4.3f), new Vector3(0.3f, 1.0f, 2.8f), 1.1f);
            yield return Dlg.PlayScene(2, 4, 6);           // Thank you / You're welcome / Which way?
            Cat.Hold(MoonLayout.VentMouth);                // looks toward the small opening he used
            yield return Dlg.PlayScene(2, 7, 8);
            yield return Cutscene(false);
            Checkpoint(1);
        }

        IEnumerator OpenCellDoor()
        {
            var from = DoorBars.localPosition; var to = new Vector3(0f, 2.6f, 0f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.6f)
            {
                DoorBars.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                DoorField.gameObject.SetActive(t < 0.3f && Mathf.Repeat(t * 30f, 1f) > 0.4f);
                yield return null;
            }
            DoorBars.localPosition = to;
            DoorField.gameObject.SetActive(false);
            SetZone("CellDoor", true);
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
            var from = SealedDoorSlab.localPosition; var to = new Vector3(0f, -3.3f, 0f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.6f) { SealedDoorSlab.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            SetZone("VisionExit", true);
            Glow(InscriptionGlow, 0.6f);
            yield return Shot("s06_passage_opens");
            Seven.Release(); Seven.Lock(true);
            yield return Dlg.PlayScene(6, 13, 15);
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

        IEnumerator Scene7And8()
        {
            Objective("Find the way out");
            if (Auto) StartCoroutine(AutoWalk(new Vector3(16.5f, MoonLayout.Lower, -41f), MoonLayout.Viewpoint));
            yield return Until(() => Seven.transform.position.z < MoonLayout.ViewpointZ, "impound viewpoint", 40f);
            yield return Cutscene(true);
            Cam.Shot(new Vector3(14.6f, MoonLayout.Lower + 2.4f, -43.0f), MoonLayout.SaucerCentre + Vector3.up * 1.2f, 1.4f, false, 30f);
            yield return Dlg.PlayScene(7);
            yield return Cutscene(false);
            Checkpoint(4);
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
            if (Auto) yield return AutoWalk(new Vector3(MoonLayout.RampFoot.x + 1.5f, MoonLayout.Bay, -75f), MoonLayout.RampFoot + Vector3.right * 0.6f);
            yield return Until(() => Dist7(MoonLayout.RampFoot) < 2.2f, "at the saucer ramp", 60f);

            // Scene 8.
            yield return Cutscene(true);
            Objective(null);
            Cat.GoTo(MoonLayout.RampFoot + new Vector3(1.0f, 0f, 1.2f), Cat.TrotSpeed, Seven.transform.position);
            Cam.Shot(MoonLayout.RampFoot + new Vector3(3.6f, 1.85f, 3.8f), MoonLayout.RampFoot + new Vector3(-1.2f, 1.3f, 0f), 1.2f);
            yield return Dlg.PlayScene(8);
            Checkpoint(4);
            yield return Board();
        }

        IEnumerator ReleaseSaucer()
        {
            var l0 = Lever.localRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f) { Lever.localRotation = l0 * Quaternion.Euler(Mathf.Lerp(0f, 70f, t), 0f, 0f); yield return null; }
            yield return Wait(0.3f);
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
            // Ramp lowers.
            var r0 = SaucerRamp.localRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.6f) { SaucerRamp.localRotation = Quaternion.Slerp(r0, Quaternion.Euler(0f, 0f, 0f), Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            yield return Shot("s07_saucer_released");
            if (Auto) Cap.Log("saucer released: clamps open, rim glow up, ramp down");
        }

        IEnumerator Board()
        {
            // 7 goes first, the cat after her; both vanish into the hull; the ramp closes; liftoff.
            var top = MoonLayout.SaucerCentre + new Vector3(5.25f, 0f, 0f);
            var path = new[] { MoonLayout.RampFoot, top };
            Ground.IgnoreObstacle = 0;             // obstacle 0 = the saucer
            SetZone("SaucerRamp", true);
            Seven.Lock(false); Seven.ScriptDriven = true;
            Cam.Shot(MoonLayout.SaucerCentre + new Vector3(13f, 3.2f, 8f), MoonLayout.SaucerCentre + new Vector3(3f, 1.2f, 0f), 1.6f);
            float rampLen = 2.3f, rampTopY = MoonLayout.SaucerBaseY + 0.25f;
            foreach (var p in path)
            {
                float t0 = Time.time;
                while (Dist7(p) > 0.25f && Time.time - t0 < 10f)
                {
                    Seven.ScriptMove = Flat(p - Seven.transform.position).normalized * 0.8f;
                    yield return null;
                }
            }
            Seven.ScriptMove = Vector3.zero;
            Show(Seven.gameObject, false);
            Cat.Scripted("walk");
            var c0 = Cat.transform.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.8f)
            {
                var p = Vector3.Lerp(c0, top, t);
                float along = Mathf.InverseLerp(MoonLayout.RampFoot.x, MoonLayout.RampFoot.x - rampLen, p.x);
                p.y = MoonLayout.Bay + rampTopY * Mathf.Clamp01(along);
                Cat.transform.position = p;
                var d = Flat(top - c0); if (d.sqrMagnitude > 1e-4f) Cat.transform.rotation = Quaternion.LookRotation(d);
                yield return null;
            }
            Show(Cat.gameObject, false);
            var r0 = SaucerRamp.localRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f) { SaucerRamp.localRotation = Quaternion.Slerp(r0, Quaternion.Euler(0f, 0f, 80f), t); yield return null; }
            // Liftoff: a wide shot from the bay corner.
            Cam.Shot(new Vector3(29f, MoonLayout.Bay + 3f, -61f), MoonLayout.SaucerCentre + Vector3.up * 2f, 0.4f, true);
            Burst(MoonLayout.SaucerCentre + Vector3.up * 0.3f, 160, 4f);
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
            if (Auto) Cap.Log($"moon level done in {Time.time - Chapter1State.StartedAt:0.0}s game time; lines so far {Chapter1State.Played.Count}; cat max distance while following/leading {Cap.CatMaxBehind:0.00} m; cat catch-ups {Cat.CatchUps}");
            Chapter1State.Aboard = true;
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
