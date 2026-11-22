using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using L = Seven.AsteroidLayout;

namespace Seven
{
    /// <summary>
    /// Round 16 — Chapter "The Asteroid — the Great Machine Age" (Docs/CHAPTER_ASTEROID.md, AST_02-AST_09 here; AST_01,
    /// AST_10 and AST_11 aboard in Chapter1AboardAsteroid). Built on the shared systems as Mars: the hover bike (one smooth
    /// riding camera), 7 on foot over the ground zones, telekinesis (RT), interactions (X), the arc blades (RB / G; X cuts
    /// while drawn), the one saucer prefab (the bike via the bike hold, 7 by the beam), dialogue as data (AsteroidDialogue,
    /// voices in Resources/AsteroidVoice), checkpoints for Continue / Scene Select (AsteroidState). Every wait on the player
    /// has a hint and a skip; story triggers fire at or past their place; the way to go is the soft white guide light + the
    /// on-screen marker. Strictly monochrome; nobody is killed or hurt: the machines are evaded, trapped or switched off,
    /// the Machinist is immobilized in his cradle and released once he accepts the preservation task.
    /// </summary>
    public partial class AsteroidDirector : MonoBehaviour
    {
        public ChapterSeven Seven;
        public ChapterCamera Cam;
        public ChapterInput Input;
        public DialoguePlayer Dlg;
        public ChapterUI UI;
        public ControlsLegend Legend;
        public HoverBike Bike;
        public SaucerBeam SaucerBeam;
        public Transform Saucer;
        public SaucerHold Hold;
        public Atmosphere Atmo;
        public Transform GuideLight;
        public BubbleHelmet Helmet;
        [Header("Receiving (AST_02)")]
        public AstScanner Scanner; public AstMaintBot MaintBot; public Transform Container; public Renderer ContainerMarks;
        public Transform DoorWheel; public Renderer DoorLamp; public AstShutter ServiceDoor, OuterGate; public PowerLine DoorLine; public Renderer[] PressureVents;
        [Header("The city (AST_03)")]
        public AstTurntable[] Junctions; public Transform[] JunctionLevers; public Renderer[] JunctionLamps; public AstBird Bird; public Renderer BirdLamp;
        [Header("The hall (AST_04 / 07 / 09)")]
        public MarsFolk Machinist; public MachinistRig MRig; public AstClamp Cradle; public AstClamp[] Stations; public Transform Terminal; public Renderer TerminalScreen;
        public AstArchiveLine ArchiveLine; public Transform CutterPanel; public Renderer PanelGlyph, SignalGlyph;
        public AstShutter HallDoorShutter, HatchShutter, ArchiveShutter, WestShutter;
        public Transform Rune, RecordCubo; public Renderer MemoryScreen; public Transform StorageDoor; public Transform[] StoredPanels;
        public Light[] ProductionLights; public Renderer[] ProductionLamps;
        public Material ScrapMat, CanisterMat; public Mesh CanisterMesh; public Mesh[] DebrisMeshes;
        [Header("The factory area (AST_05 / 06)")]
        public Renderer AlcoveScreen; public Transform LockerDoor; public Renderer LockerLamp; public Transform LockerBlades;
        public Transform TransportLever; public Transform[] CargoPlatforms; public Vector3[] PlatformParked, PlatformAligned;
        public PowerLine ClampPower; public Transform ClampJaw; public Transform ValveWheel; public PowerLine Coolant; public ParticleSystem[] Steam;
        public Transform FinalPlat; public Transform DiverterLever; public Renderer[] ControlLamps;
        public AstSecurity[] Security;
        public UnityEngine.Video.VideoClip Vision5;

        public static AsteroidDirector I { get; private set; }
        bool Auto => DemoBoot.CaptureDir != null && !DemoBoot.PlayerSim;
        bool Shots => DemoBoot.CaptureDir != null && Cap != null;
        ChapterCapture Cap => ChapterCapture.I;
        Vector3 P7 => Seven.transform.position;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        string _objective; bool _cut;
        public string ObjectiveNow => _objective;
        public Vector3? NextMarker { get; private set; }
        public string WaitingFor { get; private set; }
        public bool SkipOffered { get; private set; }
        public bool ChapterDone { get; private set; }
        public int Hints { get; private set; }
        public int Skips { get; private set; }
        public string Prompt { get; private set; }
        public ControlsLegend.Btn PromptBtn { get; private set; }
        public bool Riding => Bike != null && Bike.Riding;
        public bool InCutscene => _cut;
        public float HintAfter => DemoBoot.VenusHintFast ? 12f : Riding ? 25f : 45f;
        public float SkipAfter => DemoBoot.VenusHintFast ? 30f : Riding ? 60f : 120f;
        AudioSource _factory, _quiet;

        // ------------------------------------------------------------------------------------------ setup
        void Start()
        {
            I = this;
            if (DemoBoot.CaptureDir != null && !AsteroidState.SceneSelectRun && !DemoBoot.Continue && DemoBoot.AstCp >= 0) AsteroidState.ResetForTest();
            int startCp = DemoBoot.AstCp >= 0 && DemoBoot.CaptureDir != null ? DemoBoot.AstCp : AsteroidState.Checkpoint;
            if (DemoBoot.AstCp >= 0 && DemoBoot.CaptureDir != null && !AsteroidState.SceneSelectRun) AsteroidState.Fake(startCp, true);
            if (startCp < L.CpReceiving) startCp = L.CpReceiving;
            UI.SetFade(1f);
            Legend.SetLayout(ControlsLegend.Layout.Chapter); Legend.HidePanels = true;
            if (DemoBoot.CaptureDir != null) { ChapterCapture.Ensure(); Cap.Attach(Cam.GetComponent<Camera>()); StartCoroutine(FirstFrame()); }
            if (Auto) { Seven.ScriptDriven = true; Dlg.AutoSkipAfter = DemoBoot.VenusSkip ? 0f : 0.9f; Cap.Log($"Asteroid check run: {AsteroidState.Summary()}"); }
            Bike.Rider = Seven; Bike.Input = Input; Bike.Swim = false; Bike.SavesToVenus = false;
            HoverBike.ZoneTest = L.BikeZone;
            L.UseTraversal();
            AstProduction.Rate = AstProduction.Target = 1f;
            var ex = new List<Transform> { Seven.transform, Bike.transform };
            if (Machinist != null) ex.Add(Machinist.transform);
            if (GuideLight != null) ex.Add(GuideLight);
            if (MaintBot != null) ex.Add(MaintBot.transform);
            if (Container != null) ex.Add(Container);
            if (Security != null) foreach (var s in Security) if (s != null) ex.Add(s.transform);
            if (CargoPlatforms != null) foreach (var p in CargoPlatforms) if (p != null) ex.Add(p);
            if (Helmet != null && Helmet.Sphere != null) ex.Add(Helmet.Sphere);
            if (Helmet != null && Helmet.Collar != null) ex.Add(Helmet.Collar);
            foreach (var sp in FindObjectsByType<AstSpin>(FindObjectsSortMode.None)) ex.Add(sp.transform);          // QF21: moving parts
            foreach (var rc in FindObjectsByType<AstRailCarrier>(FindObjectsSortMode.None)) if (rc.Cars != null) foreach (var c in rc.Cars) if (c != null) ex.Add(c);
            CameraOccluders.IncludeHuge = true;
            CameraOccluders.Build(ex.ToArray());
            HoverBike.IgnoreRoots = ex.ToArray(); ChapterCamera.RideIgnore = ex.ToArray();
            ChapterCamera.RideArea = () => RideAreaName(Riding ? Bike.transform.position : P7);
            Dlg.LineStarted += OnLine;
            DialogueFraming.Speaker = id => id == "seven" ? Seven.transform : id == "machinist" ? Machinist?.transform : null;
            _factory = AudioKit.Loop("ast_amb_factory", null, null, 0.55f, AudioKit.Bus.Ambience);
            _quiet = AudioKit.Loop("ast_amb_quiet", null, null, 0f, AudioKit.Bus.Ambience);
            if (Machinist != null) { Machinist.Role = "machinist"; Machinist.CalmSway = false; }
            if (Security != null) foreach (var s in Security) if (s != null) { s.Seven = Seven; s.Paused = true; s.Caught = OnCaught; }
            Seven.SetBladesOwned(AsteroidState.BladesOwned);
            if (MRig != null) MRig.AfterPose += ClampCheck;
            if (DemoBoot.Qf21Blades && Auto) StartCoroutine(BladeDemo(startCp)); else StartCoroutine(Flow(startCp));
        }
        void OnDestroy() { if (Dlg != null) Dlg.LineStarted -= OnLine; if (I == this) I = null; HoverBike.ZoneTest = VenusLayout.BikeZone; DialogueFraming.Speaker = null; MoonTraversal.UseMoon(); AstProduction.Rate = AstProduction.Target = 1f; }
        IEnumerator FirstFrame() { Cap.Log($"first frame (the Asteroid): fade {UI.FadeAlpha:0.00} (1 = black)"); yield return Cap.Shot("a00_first_frame"); }

        string RideAreaName(Vector3 p)
        {
            var z = L.ZoneAt(p);
            if (z == "Receiving" || z == "ServiceDoor") return "receiving chamber";
            if (z == "Tunnel") return "service tunnel";
            if (z.StartsWith("Causeway") || z.StartsWith("Junction") || z.StartsWith("Bend") || z == "Workstations") return "the causeway";
            if (z == "HallPlaza" || z == "HallDoorZ" || z == "Hall") return "the central hall";
            return z;
        }

        // the Machinist's visor glows with his voice
        AudioSource _voiceSrc;
        AudioSource VoiceSource() { if (_voiceSrc == null) { var go = GameObject.Find("Voice_7"); if (go != null) _voiceSrc = go.GetComponent<AudioSource>(); } return _voiceSrc; }
        void OnLine(VoiceBank.Line l)
        {
            if (Machinist != null) Machinist.Voice = null;
            if (MRig != null) MRig.Voice = null;
            if (l.Speaker == "machinist" && Machinist != null) { Machinist.Voice = VoiceSource(); if (MRig != null) MRig.Voice = VoiceSource(); }
            if (MaintBot != null) MaintBot.Voice = null;
            if (l.Speaker == "system")
            {
                AudioKit.Play("console_select", null, 0.25f);      // the announcement's chime
                // QF21 (2): System speaks visibly - its iris, grille and the wall horns follow the voice; a whistle of steam
                if (MaintBot != null && MaintBot.gameObject.activeInHierarchy && Flat(MaintBot.transform.position - P7).magnitude < 40f) { MaintBot.Voice = VoiceSource(); MaintBot.Whistle(); }
            }
        }

        // ------------------------------------------------------------------------------------------ helpers (as Mars)
        IEnumerator Wait(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }
        void Objective(string s) { _objective = s; UI.SetObjective(s); Log("objective: " + s); }
        void Log(string s) { Debug.Log("[Seven] " + s); if (DemoBoot.CaptureDir != null) Cap?.Log(s); }
        IEnumerator Shot(string name) { if (!Shots) yield break; yield return Cap.Shot(name); }
        void Shoot(Vector3 pos, Vector3 look, float fov = 50f, float ease = 1f) { Cam.Subjects.Clear(); Cam.Shot(pos, look, ease, ease <= 0.05f, fov); }
        IEnumerator Cutscene(bool on)
        {
            _cut = on;
            if (!Riding) Seven.Lock(on);
            if (on) { SetPrompt(null); }
            if (!on) FollowCamera();
            yield return null;
        }
        void FollowCamera()
        {
            if (Riding) { Cam.Target = Bike.transform; Cam.Distance = 7.6f; Cam.LookHeight = 1.35f; Cam.AutoBehind = true; Cam.DefaultPitch = 12f; }
            else { Cam.Target = Seven.transform; Cam.Distance = 3.8f; Cam.LookHeight = 1.45f; Cam.AutoBehind = Auto; Cam.DefaultPitch = 15f; }
            Cam.Follow();
        }
        /// <summary>A two-shot from the side of the line between two people: the first framing with a clear view of both.</summary>
        void TwoShot(Vector3 mid, Vector3 side, float dist, float h, float lookH, float fov, float ease)
        {
            var axis = Vector3.Cross(side, Vector3.up).normalized; var look = mid + Vector3.up * lookH;
            foreach (var sg in new[] { 1f, -1f })
                foreach (var dk in new[] { 1f, 1.35f, 0.8f, 1.8f })
                    foreach (var hk in new[] { 0f, 1.2f, 2.6f })
                    {
                        var cam = mid + side * sg * dist * dk + Vector3.up * (h + hk);
                        if (RideProbe.Overlap(cam, 0.3f, HoverBike.IgnoreRoots, null, false)) continue;
                        if (!ChapterCamera.ClearLine(cam, look) || !ChapterCamera.ClearLine(cam, look + axis * 1.4f) || !ChapterCamera.ClearLine(cam, look - axis * 1.4f)) continue;
                        Shoot(cam, look, fov, ease); return;
                    }
            Shoot(mid + side * dist + Vector3.up * (h + 3f), look, fov, ease);
        }
        IEnumerator Say(string scene, int from = 1, int to = 99)
        {
            var ls = AsteroidDialogue.Scene(scene, from, to);
            if (ls.Count == 0) { Log($"no lines for {scene} {from}-{to}"); yield break; }
            yield return Dlg.PlayLines(ls, false);
        }
        IEnumerator SayIds(params string[] ids) { var ls = AsteroidDialogue.Ids(ids); if (ls.Count > 0) yield return Dlg.PlayLines(ls, false); }
        bool Complete(string id) { bool first = AsteroidState.CompleteScene(id); Log($"scene {id} complete{(first ? "" : " (already done)")}"); return first; }
        void Checkpoint(int cp) { if (AsteroidState.Reach(cp)) Log($"checkpoint {cp}: {L.CpNames[cp]} (saved for Continue)"); }
        bool SkipButton()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current; var gp = UnityEngine.InputSystem.Gamepad.current;
            return (kb != null && kb.enterKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame);
        }
        void SetPrompt(string label, ControlsLegend.Btn btn = ControlsLegend.Btn.Interact) { Prompt = label; PromptBtn = btn; Legend.Context(btn, label); }
        void SetZone(string name, bool on) { var z = Ground.FindZone(name); if (z != null) z.Enabled = on; }
        static void Glow(Renderer r, float v) => Glowing.Set(r, v);
        void ShowGuide(Vector3 at) { NextMarker = at; if (GuideLight != null) { GuideLight.gameObject.SetActive(true); GuideLight.position = at + Vector3.up * 1.2f; } }
        void HideGuide() { NextMarker = null; UI.SetMarker(null); if (GuideLight != null) GuideLight.gameObject.SetActive(false); }

        /// <summary>Wait for a story condition: check runs log a soft-lock after autoTimeout; a player without progress gets
        /// the hint after HintAfter s (the guide light) and a skip after SkipAfter s (Y / Enter).</summary>
        IEnumerator Until(Func<bool> cond, string what, float autoTimeout, string hint = null, Vector3? at = null, Action skip = null)
        {
            float t0 = Time.time, since = Time.time, best = at.HasValue ? Vector3.Distance(P7, at.Value) : 0f; bool hinted = false;
            WaitingFor = what; SkipOffered = false;
            while (!cond())
            {
                if (Auto && Time.time - t0 > autoTimeout) { Cap.Soft++; Cap.Log($"SOFT-LOCK? waiting for '{what}' {autoTimeout:0}s (7 at {P7}, mode {Seven.Current}, riding {Riding}, prompt '{Prompt}')"); StartCoroutine(Shot("softlock_" + what.Replace(' ', '_').Replace("'", "").Replace("(", "").Replace(")", "").Replace("/", ""))); skip?.Invoke(); break; }
                bool busy = _cut || PauseMenu.IsOpen || Dlg.Busy;
                if (busy) { since += Time.deltaTime; yield return null; continue; }
                if (at.HasValue) { float d = Vector3.Distance(P7, at.Value); if (d < best - 2f) { best = d; since = Time.time; } }
                if (!Auto)
                {
                    if (!hinted && hint != null && Time.time - since > HintAfter) { hinted = true; Hints++; UI.Hint(hint, 8f); Log($"HINT: '{what}' -> \"{hint}\""); if (at.HasValue) ShowGuide(at.Value); }
                    if (!SkipOffered && skip != null && Time.time - since > SkipAfter) { SkipOffered = true; UI.Hint("Y: skip this step", 6f); Log($"SKIP OFFERED: '{what}'"); }
                    if (SkipOffered && SkipButton())
                    {
                        Skips++; Log($"SKIP taken: '{what}'");
                        yield return UI.Fade(1f, 0.5f, 1f);
                        try { skip?.Invoke(); } catch (Exception e) { Log("skip failed: " + e.Message); }
                        yield return null; yield return UI.Fade(0f, 0.7f, 1f);
                        break;
                    }
                }
                yield return null;
            }
            WaitingFor = null; SkipOffered = false;
            if (DemoBoot.CaptureDir != null) Cap?.Log($"ok: {what} ({Time.time - t0:0.0}s)");
        }

        void PlaceRiding(Vector3 at, float yaw = float.NaN)
        {
            if (float.IsNaN(yaw)) yaw = Bike.transform.eulerAngles.y;
            if (!Bike.Riding) Seven.Place(at - Quaternion.Euler(0f, yaw, 0f) * Vector3.right * 1.3f, Quaternion.Euler(0f, yaw, 0f) * Vector3.forward);
            Bike.Place(at, yaw);
            if (!Bike.Riding) Bike.Mount();
            FollowCamera(); Cam.SnapBehind();
        }
        IEnumerator RideRoute(Vector3[] pts, string what, float radius = 7f, float autoTimeout = 120f, string hint = "Follow the white light (left stick: steer, RT forward)")
        {
            int k = 0;
            if (Auto) StartCoroutine(AutoRide(pts, radius * 0.6f));
            while (k < pts.Length)
            {
                for (int j = pts.Length - 1; j >= k; j--) if (Flat(Bike.transform.position - pts[j]).magnitude < radius) { k = j + 1; break; }
                if (k >= pts.Length) break;
                ShowGuide(pts[k]);
                int kk = k;
                yield return Until(() => !Riding || Flat(Bike.transform.position - pts[kk]).magnitude < radius || Reached(pts, kk, radius), $"{what} ({kk + 1}/{pts.Length})", autoTimeout, hint, pts[kk], () => PlaceRiding(pts[kk]));
                if (!Riding) { yield return GetBackOn(); continue; }
                k = kk + 1;
            }
            HideGuide();
        }
        bool Reached(Vector3[] pts, int k, float r) { for (int j = k + 1; j < pts.Length; j++) if (Flat(Bike.transform.position - pts[j]).magnitude < r) return true; return false; }
        IEnumerator GetBackOn()
        {
            var prev = _objective;
            Objective("Get back on the bike (X)");
            ShowGuide(Bike.transform.position);
            if (Auto) { yield return WalkSeven(Bike.transform.position - Bike.transform.right * 1.1f, 12f); PlaceRiding(Bike.transform.position); }
            yield return Until(() => Riding, "back on the bike", 30f, "Walk to the bike and press X", Bike.transform.position, () => PlaceRiding(Bike.transform.position));
            if (prev != null && prev != _objective) Objective(prev);
        }
        bool _autoRide; int _autoRideGen;
        IEnumerator AutoRide(Vector3[] pts, float r)
        {
            _autoRide = true; Bike.ScriptDriven = true; int gen = ++_autoRideGen;   // QF21: a newer AutoRide replaces this one
            foreach (var p in pts)
            {
                for (float t0 = Time.time; Flat(Bike.transform.position - p).magnitude > r && Time.time - t0 < 40f && _autoRide && gen == _autoRideGen;)
                {
                    var f = Flat(p - Bike.transform.position);
                    float err = Vector3.SignedAngle(Flat(Bike.transform.forward), f, Vector3.up);
                    Bike.ScriptSteer = Mathf.Clamp(err / 30f, -1f, 1f);
                    Bike.ScriptThrottle = Mathf.Abs(err) > 70f ? 0.25f : Mathf.Clamp(f.magnitude / 10f, 0.35f, 1f);
                    yield return null;
                }
                if (!_autoRide || gen != _autoRideGen) break;
            }
            if (gen != _autoRideGen) yield break;
            Bike.ScriptSteer = 0f; Bike.ScriptThrottle = 0f;
            _autoRide = false;
        }
        IEnumerator StopBike(float seconds = 2f)
        {
            bool sd = Bike.ScriptDriven; Bike.ScriptDriven = true; Bike.ScriptThrottle = -1f; Bike.ScriptSteer = 0f;
            for (float t = 0f; t < seconds && Bike.Speed > 0.3f; t += Time.deltaTime) yield return null;
            Bike.ScriptThrottle = 0f; Bike.ScriptDriven = sd || Auto;
        }
        IEnumerator GetOff()
        {
            if (!Riding) yield break;
            yield return StopBike(1.5f);
            if (!Bike.Dismount()) Bike.Dismount(true);
            FollowCamera();
        }
        int _walkGen;
        void StopWalks() { _walkGen++; Seven.ScriptMove = Vector3.zero; }
        IEnumerator WalkSeven(Vector3 to, float timeout, int gen = -1)
        {
            if (gen < 0) gen = ++_walkGen;
            bool sd = Seven.ScriptDriven; Seven.ScriptDriven = true; Seven.Lock(false);
            for (float t0 = Time.time; Flat(to - P7).magnitude > 0.35f && Time.time - t0 < timeout && gen == _walkGen;) { Seven.ScriptMove = Flat(to - P7).normalized * 0.8f; yield return null; }
            if (gen == _walkGen) Seven.ScriptMove = Vector3.zero;
            Seven.ScriptDriven = sd || Auto;
        }
        IEnumerator WalkPath(Vector3[] pts, float timeout) { int gen = ++_walkGen; float t0 = Time.time; foreach (var p in pts) { if (Time.time - t0 > timeout || gen != _walkGen) break; yield return WalkSeven(p, Mathf.Max(2f, timeout - (Time.time - t0)), gen); } }
        IEnumerator GuideWalk(Vector3[] pts, string what, float r = 1.6f)
        {
            int k = 0;
            if (Auto) StartCoroutine(WalkPath(pts, 40f * pts.Length));
            while (k < pts.Length)
            {
                for (int j = pts.Length - 1; j >= k; j--) if (Flat(P7 - pts[j]).magnitude < r) { k = j + 1; break; }
                if (k >= pts.Length) break;
                ShowGuide(pts[k]); int kk = k;
                yield return Until(() => Flat(P7 - pts[kk]).magnitude < r || GuideReached(pts, kk, r), $"{what} ({kk + 1}/{pts.Length})", 40f, "Follow the white light", pts[kk], () => Seven.Place(pts[kk], Vector3.forward));
                k = kk + 1;
            }
            HideGuide();
        }
        bool GuideReached(Vector3[] pts, int k, float r) { for (int j = k + 1; j < pts.Length; j++) if (Flat(P7 - pts[j]).magnitude < r) return true; return false; }

        /// <summary>Walk up to a spot and press X (fromBike: X also works stopped on the bike within bikeReach - no dismount).</summary>
        /// QF21 (4) - the wrong-state rule: bikeOnly (its text) = only from the saddle; on foot near it the prompt says so
        /// ("Use the junction from your bike"), Panther says it once (bikeLine) and the marker points back at the bike. An X
        /// pressed in the wrong state answers too - never a silent ignore.
        IEnumerator Interact(Vector3 spot, string label, string what, string hint, Vector3[] autoPath = null, Action skipped = null, float radius = 1.8f, bool fromBike = false, float bikeReach = 5f, string bikeOnly = null, string bikeLine = null)
        {
            bool hit = false;
            IEnumerator P()
            {
                bool told = false, guideBike = false;
                while (!hit)
                {
                    bool onFootClose = !Riding && Seven.Current == ChapterSeven.Mode.Free && Flat(spot - P7).magnitude < Mathf.Max(radius + 1.2f, 3.2f);
                    bool foot = bikeOnly == null && Flat(spot - P7).magnitude < radius && Mathf.Abs(Ground.Height(spot.x, spot.z) - P7.y) < 1.6f && (Seven.Current == ChapterSeven.Mode.Free || (Auto && !Riding));
                    bool bike = fromBike && Riding && Bike.Speed < 1.6f && Flat(spot - Bike.transform.position).magnitude < bikeReach;
                    bool near = foot || bike;
                    bool wrong = bikeOnly != null && onFootClose;
                    string want = near ? label : wrong ? bikeOnly : (!fromBike && Riding && Bike.Speed < 1.2f && Flat(spot - P7).magnitude < 12f ? "Get off" : null);
                    // the free-riding prompts (Ride / Get off) belong to Update when this one has nothing to say
                    if (!_cut && !(want == null && FreeBike && (Prompt == "Ride" || Prompt == "Get off"))) SetPrompt(want);
                    if (wrong && (!told || (!Auto && Input.Interact.WasPressedThisFrame())))
                    {
                        if (!told)
                        {
                            WrongState++;
                            Log($"WRONG STATE (QF21 4): '{what}' needs the bike - prompt '{bikeOnly}'{(bikeLine != null ? ", Panther " + bikeLine : "")}, marker back to the bike");
                            StartCoroutine(Shot("a13w_wrong_state_" + what.Replace(' ', '_')));
                            if (bikeLine != null) StartCoroutine(SayIds(bikeLine));
                        }
                        else UI.Hint(bikeOnly, 2.5f);
                        told = true;
                    }
                    // the marker: back to the bike while she is on foot (bike-only), at the spot again once she rides
                    bool wantBike = bikeOnly != null && !Riding;
                    if (wantBike != guideBike) { guideBike = wantBike; ShowGuide(wantBike ? Bike.transform.position : spot); }
                    if (near && ((Auto && !_inWrongDemo) || Input.Interact.WasPressedThisFrame())) hit = true;
                    else if (!near && !fromBike && Riding && Bike.Speed < 1.2f && !Auto && Input.Interact.WasPressedThisFrame()) { if (Bike.Dismount()) FollowCamera(); }
                    yield return null;
                }
                SetPrompt(null);
            }
            StartCoroutine(P());
            ShowGuide(spot);
            if (Auto && bikeOnly != null && !_wrongDemoDone) { _wrongDemoDone = true; yield return WrongStateDemo(spot); }
            if (Auto) { if (Riding && !fromBike) yield return GetOff(); if (!Riding) StartCoroutine(WalkPath(autoPath ?? new[] { spot }, 40f)); else StartCoroutine(AutoRide(new[] { spot }, bikeReach * 0.5f)); }
            yield return Until(() => hit, what, 60f, hint, spot, () => { if (Riding && !fromBike) Bike.Dismount(true); if (!Riding && bikeOnly != null) PlaceRiding(spot); else if (!Riding) Seven.Place(spot, Flat(spot - P7)); hit = true; skipped?.Invoke(); });
            hit = true; if (Auto) { StopWalks(); _autoRide = false; if (Riding) { Bike.ScriptThrottle = 0f; Bike.ScriptSteer = 0f; } }
            HideGuide();
        }
        public int WrongState { get; private set; }
        bool _wrongDemoDone, _inWrongDemo;
        /// <summary>QF21 (4) proof in check runs (once): she gets off short of a bike-only control and walks up to it on foot -
        /// the prompt, Panther's line and the marker back to the bike show (captured from the player's camera) - then she
        /// walks back and gets on again.</summary>
        IEnumerator WrongStateDemo(Vector3 spot)
        {
            _inWrongDemo = true; _autoRide = false; _autoRideGen++;
            if (Riding && Flat(spot - Bike.transform.position).magnitude > 7f) { yield return AutoRide(new[] { spot }, 6f); _autoRideGen++; }
            if (Riding) { yield return StopBike(1.5f); if (!Bike.Dismount()) Bike.Dismount(true); FollowCamera(); }
            for (float t = 0f; t < 1.5f && (Riding || Seven.Current != ChapterSeven.Mode.Free); t += Time.deltaTime) yield return null;
            Log($"WRONG STATE (QF21 4) check: she got off at {Flat(spot - P7).magnitude:0.0} m from the control (riding {Riding}, mode {Seven.Current})");
            yield return WalkSeven(spot + Flat(P7 - spot).normalized * 1.4f, 10f);
            for (float t = 0f; t < 2.6f; t += Time.deltaTime) yield return null;
            bool onBike = NextMarker.HasValue && Flat(NextMarker.Value - Bike.transform.position).magnitude < 1f;
            Log($"WRONG STATE (QF21 4) check: on foot {Flat(spot - P7).magnitude:0.0} m from the control - prompt '{Prompt}', marker {(onBike ? "on the bike" : "NOT on the bike")}, Panther's line {(WrongState > 0 ? "played" : "not yet")}");
            yield return Shot("a13x_wrong_state_on_foot_at_the_junction_player_camera");
            yield return WalkSeven(Bike.transform.position - Bike.transform.right * 1.1f, 10f);
            PlaceRiding(Bike.transform.position);
            _inWrongDemo = false;
        }

        /// <summary>Telekinesis (RT) at something in range she (or the camera) looks at; its edges glow while it can be taken.</summary>
        IEnumerator Telekinesis(Vector3 target, float range, string what, string hint, Renderer glow = null, Vector3[] autoPath = null, bool onFoot = true, Transform track = null)
        {
            bool hit = false;
            bool InView() { if (track != null) target = track.position; if (Auto) return Flat(target - P7).magnitude < range; var to = Flat(target - P7); if (to.magnitude > range) return false; if (to.magnitude < 3.5f) return true; return Vector3.Angle(Flat(Cam.transform.forward), to) < 50f || Vector3.Angle(Flat(Seven.transform.forward), to) < 50f; }
            IEnumerator P()
            {
                for (float t = 0f; !hit; t += Time.deltaTime)
                {
                    bool can = InView() && (!onFoot || !Riding) && !_cut;
                    if (glow != null) Glow(glow, can ? 1.1f + 0.6f * Mathf.Sin(t * 3.5f) : 0.25f);
                    if (!_cut) SetPrompt(can ? "Telekinesis" : (Riding && onFoot && Bike.Speed < 1.2f && Flat(target - P7).magnitude < range + 6f ? "Get off" : null), can ? ControlsLegend.Btn.Tk : ControlsLegend.Btn.Interact);
                    if (!can && Riding && onFoot && Bike.Speed < 1.2f && !Auto && Input.Interact.WasPressedThisFrame()) { if (Bike.Dismount()) FollowCamera(); }
                    if (can && (Auto || Input.Telekinesis.WasPressedThisFrame())) hit = true;
                    yield return null;
                }
                if (glow != null) Glow(glow, 0f);
                SetPrompt(null);
            }
            StartCoroutine(P());
            ShowGuide(target);
            if (Auto) { if (Riding && onFoot) yield return GetOff(); if (autoPath != null) StartCoroutine(WalkPath(autoPath, 40f)); }
            yield return Until(() => hit, what, 60f, hint, target, () => { if (Riding && onFoot) Bike.Dismount(true); hit = true; });
            hit = true; if (Auto) StopWalks();
            TkAudit.Watch(this, what, track != null ? track.position : target, track);   // QF24 (14): did the visible object move?
            if (track != null) target = track.position;
            HideGuide();
            Seven.Gesture(target); Seven.SetEnergyPoint(1.9f, 0.25f);
            for (float t = 0f; t < 2.5f && !Seven.GesturePeak; t += Time.deltaTime) yield return null;
        }

        /// <summary>AST_05: cut a power line with the arc blades - drawn (RB / G), then X within reach. Not drawn = the prompt
        /// says to draw them first.</summary>
        IEnumerator BladeCut(Vector3 target, string label, string what, string hint, Vector3[] autoPath = null, float reach = 2.3f)
        {
            bool hit = false;
            IEnumerator P()
            {
                while (!hit)
                {
                    bool near = Flat(target - P7).magnitude < reach && Seven.Current == ChapterSeven.Mode.Free;
                    if (!_cut) { if (near && !Seven.BladesDrawn) SetPrompt("Draw the arc blades", ControlsLegend.Btn.Blades); else SetPrompt(near ? label : null); }
                    if (near && Seven.BladesDrawn && (Auto || Input.Interact.WasPressedThisFrame())) hit = true;
                    yield return null;
                }
                SetPrompt(null);
            }
            StartCoroutine(P());
            ShowGuide(target);
            if (Auto) { StartCoroutine(WalkPath(autoPath ?? new[] { new Vector3(target.x, 0f, target.z) - Vector3.forward * 1.4f }, 40f)); Seven.DrawBlades(true); }
            yield return Until(() => hit, what, 60f, hint, target, () => { Seven.DrawBlades(true); hit = true; });
            hit = true; if (Auto) StopWalks();
            HideGuide();
            Seven.BladeSwing(target);
            for (float t = 0f; t < 1.5f && !Seven.GesturePeak; t += Time.deltaTime) yield return null;
            AudioKit.Play("ast_blade_hit", target, 1f, 0.03f, AudioKit.Bus.Sfx, 30f);
            Sparks(target, 70);
        }

        /// <summary>A short burst of white sparks (a power line severed, a coupling flaring).</summary>
        void Sparks(Vector3 at, int n)
        {
            var go = new GameObject("Sparks"); go.transform.position = at;
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main; m.duration = 0.3f; m.loop = false; m.startLifetime = 0.5f; m.startSpeed = 5f; m.startSize = 0.05f; m.gravityModifier = 1.2f; m.startColor = Color.white; m.maxParticles = 400;
            var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)n) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.1f;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Markers.Additive(2f);
            ps.Play(); Destroy(go, 2f);
        }

        /// <summary>The mount / dismount prompts in free riding stretches.</summary>
        [NonSerialized] public bool FreeBike;
        void Update()
        {
            AstProduction.Tick(Time.deltaTime);
            CalmTick();
            FreeActions();
            if (Auto || _cut || PauseMenu.IsOpen || !FreeBike || Prompt != null && Prompt != "Get off" && Prompt != "Ride") return;
            if (Riding)
            {
                bool stopped = Bike.Speed < 1.2f;
                Legend.Context(ControlsLegend.Btn.Interact, stopped ? "Get off" : null); Prompt = stopped ? "Get off" : null; PromptBtn = ControlsLegend.Btn.Interact;
                if (stopped && Input.Interact.WasPressedThisFrame()) { if (Bike.Dismount()) FollowCamera(); }
            }
            else if (Seven.Current == ChapterSeven.Mode.Free && Bike.CanMount(P7))
            {
                Legend.Context(ControlsLegend.Btn.Interact, "Ride"); Prompt = "Ride"; PromptBtn = ControlsLegend.Btn.Interact;
                if (Input.Interact.WasPressedThisFrame()) { Bike.Mount(); FollowCamera(); }
            }
            else if (Prompt == "Ride" || Prompt == "Get off") { Legend.Context(ControlsLegend.Btn.Interact, null); Prompt = null; }
        }

        // ------------------------------------------------------------------------------------------ the security machines (AST_05)
        public int Escorted { get; private set; }
        public int SecurityCut { get; private set; }
        public int SecurityPushed { get; private set; }
        bool _securityOn;
        void SecurityActive(bool on) { _securityOn = on; if (Security != null) foreach (var s in Security) if (s != null) s.Paused = !on || Escorted >= 3; }
        /// <summary>Free play near a security machine: X with the blades drawn cuts its drive's power; RT pushes it away.</summary>
        void FreeActions()
        {
            if (!_securityOn || _cut || Security == null || Seven.Current != ChapterSeven.Mode.Free || PauseMenu.IsOpen) return;
            AstSecurity near = null; float bd = 99f;
            foreach (var s in Security) { if (s == null || !s.Active) continue; float d = Flat(s.transform.position - P7).magnitude; if (d < bd) { bd = d; near = s; } }
            if (near == null) return;
            bool x = Auto ? bd < 2.4f : Input.Interact.WasPressedThisFrame();
            bool rt = !Auto && Input.Telekinesis.WasPressedThisFrame();
            if (Seven.BladesDrawn && bd < 2.6f && x) { StartCoroutine(CutSecurity(near)); return; }
            if (bd < 10f && rt && Prompt == null) { SecurityPushed++; Seven.Gesture(near.transform.position); near.Stun(Flat(near.transform.position - P7).normalized * 6f, 4f); Log($"security machine {near.name}: pushed away by telekinesis (stunned 4 s)"); }
        }
        IEnumerator CutSecurity(AstSecurity s)
        {
            Seven.BladeSwing(s.transform.position);
            for (float t = 0f; t < 1.2f && !Seven.GesturePeak; t += Time.deltaTime) yield return null;
            if (Flat(s.transform.position - P7).magnitude < 3.2f) { SecurityCut++; s.Disable(25f, "the arc blades (its drive's power line severed)"); Sparks(s.transform.position + Vector3.up * 0.8f, 50); AudioKit.Play("ast_blade_hit", s.transform.position, 1f, 0.05f, AudioKit.Bus.Sfx, 25f); if (Shots && SecurityCut == 1) StartCoroutine(Shot("a24_security_machine_disabled_by_the_blades")); }
        }
        void OnCaught(AstSecurity s)
        {
            if (_cut) return;
            Escorted++;
            Log($"security machine {s.name} caught her - escorted back to the works' entry (nonlethal; {Escorted}x)");
            StartCoroutine(Escort(s));
        }
        IEnumerator Escort(AstSecurity s)
        {
            _cut = true; Seven.Lock(true);
            yield return UI.Fade(1f, 0.5f, 0f);
            Seven.Place(L.EscortedTo, Vector3.back);
            if (Escorted >= 3) { foreach (var x in Security) if (x != null) x.Paused = true; UI.Hint("The security machines lose interest in you", 4f); Log("security: three escorts - they stand down (fairness)"); }
            yield return UI.Fade(0f, 0.6f, 0f);
            Seven.Lock(false); _cut = false; FollowCamera();
        }

        void LateUpdate()
        {
            if (DemoBoot.CaptureDir != null && !Riding && Seven != null && Seven.gameObject.activeInHierarchy) FeetProbe.Sample(Seven, "asteroid " + L.ZoneAt(P7));
            if (GuideLight != null && Cam != null)
            {
                bool show = NextMarker.HasValue && !_cut;
                if (GuideLight.gameObject.activeSelf != show) GuideLight.gameObject.SetActive(show);
                UI.SetMarker(show ? NextMarker : null);
                if (show)
                {
                    float d = Vector3.Distance(Cam.transform.position, GuideLight.position);
                    GuideLight.localScale = Vector3.one * Mathf.Max(0.6f, d * 0.03f);
                    GuideLight.rotation = Quaternion.LookRotation(GuideLight.position - Cam.transform.position);
                }
            }
            Ground.Obstacles.Clear();
            if (Machinist != null && Machinist.gameObject.activeInHierarchy && !Seven.InFight) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(Machinist.transform.position.x, Machinist.transform.position.z), R = 1.3f });
            if (MaintBot != null) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(MaintBot.transform.position.x, MaintBot.transform.position.z), R = 0.9f });
            if (Container != null) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(Container.position.x, Container.position.z), R = 1.5f });
            foreach (var (c, r) in PropCircles) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(c.x, c.z), R = r });
            foreach (var (c, r) in L.HallProps) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(c.x, c.z), R = r });   // QF21 (10)
            if (Stations != null) foreach (var st in Stations) if (st != null) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(st.transform.position.x, st.transform.position.z), R = 1.75f });
        }
        static readonly (Vector3 c, float r)[] PropCircles =
        {
            (L.DoorControl, 0.5f), (L.TerminalAt, 1.0f), (L.CutterAt, 2.6f), (L.CradleAt, 2.2f), (L.TransportControl, 0.55f), (L.CoolingValve, 0.6f),
            (L.DiverterAt, 0.6f), (L.BirdBench, 0.9f), (L.AlcoveTerminal, 0.7f), (L.JunctionControls[0], 0.45f), (L.JunctionControls[1], 0.45f), (L.JunctionControls[2], 0.45f),
        };
    }
}
