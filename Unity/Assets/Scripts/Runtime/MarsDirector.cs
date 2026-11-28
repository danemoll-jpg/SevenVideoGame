using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// Round 15 — Chapter "Mars — The Divided Kingdoms" (Docs/CHAPTER_MARS.md, sections 1-11 here; the chair scenes and the
    /// destination choice aboard are in Chapter1AboardMars). A first playable pass on the shared systems: the hover bike on
    /// land (one smooth riding camera, QF15), 7 on foot over the ground zones, telekinesis (RT) and interactions (X), the one
    /// saucer prefab (the bike via the bike hold, 7 by the beam), dialogue as data (MarsDialogue, voices in
    /// Resources/MarsVoice), checkpoints for Continue / Scene Select (MarsState). Every wait on the player has a hint (45 s,
    /// 25 s riding) and a skip (2 min, 60 s riding); story triggers fire at or past their place; the way to go is the soft
    /// white guide light + the on-screen marker. Strictly monochrome; nobody is killed or hurt: the guards are never attacked,
    /// Viper's machines are knocked aside, Viper himself is subdued and contained, left for local custody.
    /// </summary>
    public partial class MarsDirector : MonoBehaviour
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
        [Header("People")]
        public MarsFolk Guard, Worker, Caretaker, Viper;
        public MarsFolk[] Townsfolk;                   // the settlement's life (both banks)
        public MarsFolk[] Drivers;                     // the two who drive the supply transport
        [Header("The crossing")]
        public GameObject Barrier;                     // the checkpoint's barrier across the bridge head
        public Transform Transport, ViperPlatform; public Light PlatformLight;
        [Header("The maintenance route")]
        public Transform CausewaySlab; public Renderer CausewayEdges;
        public Transform ServiceGate; public Renderer ServicePanel;
        [Header("The controls")]
        public Transform Mechanism; public Renderer ControlW, ControlE, RelayW;
        // QF18 (12, 15, 16): power lines whose light runs, the control wheels, the mechanism's gears, the relay crystal
        public PowerLine ChannelWA, ChannelWB, ChannelE, GateLine, GateFeed;
        public Transform WheelW, WheelE, RelayCrystal, CableEndUpper, CableEndLower, ViperHorn; public Transform[] MechGears;
        public MarsDrawbridge Drawbridge; public MarsPylon[] PylonObjs;
        public Transform Conduit, SwingSeg, SpanW, SpanE; public Transform[] Jammers;
        [Header("The encounter")]
        public Renderer[] Pylons; public GameObject BarrierWC, BarrierCE, BarrierCentre; public Transform SlideBlock, FallenWalk; public GameObject Restraint;
        [Header("The temple")]
        public Renderer SymbolW, SymbolE, Display; public Transform Rune, RecordTet; public GameObject DisplayLeads;
        public UnityEngine.Video.VideoClip Vision4;

        public static MarsDirector I { get; private set; }
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
        /// <summary>The context prompt on screen right now and its button (X = interact, RT = telekinesis) - what a player sees.</summary>
        public string Prompt { get; private set; }
        public ControlsLegend.Btn PromptBtn { get; private set; }
        public bool Riding => Bike != null && Bike.Riding;
        public bool InCutscene => _cut;
        public float HintAfter => DemoBoot.VenusHintFast ? 12f : Riding ? 25f : 45f;
        public float SkipAfter => DemoBoot.VenusHintFast ? 30f : Riding ? 60f : 120f;
        AudioSource _wind, _market;

        // ------------------------------------------------------------------------------------------ setup
        void Start()
        {
            I = this;
            if (DemoBoot.CaptureDir != null && !MarsState.SceneSelectRun && !DemoBoot.Continue && DemoBoot.MarsCp >= 0) MarsState.ResetForTest();
            int startCp = DemoBoot.MarsCp >= 0 && DemoBoot.CaptureDir != null ? DemoBoot.MarsCp : MarsState.Checkpoint;
            if (DemoBoot.MarsCp >= 0 && DemoBoot.CaptureDir != null && !MarsState.SceneSelectRun) MarsState.Fake(startCp, true);
            UI.SetFade(1f);
            Legend.SetLayout(ControlsLegend.Layout.Chapter); Legend.HidePanels = true;
            if (DemoBoot.CaptureDir != null) { ChapterCapture.Ensure(); Cap.Attach(Cam.GetComponent<Camera>()); StartCoroutine(FirstFrame()); }
            if (Auto) { Seven.ScriptDriven = true; Dlg.AutoSkipAfter = DemoBoot.VenusSkip ? 0f : 0.9f; Cap.Log($"Mars check run: {MarsState.Summary()}"); }
            Bike.Rider = Seven; Bike.Input = Input; Bike.Swim = false; Bike.SavesToVenus = false;
            HoverBike.ZoneTest = MarsLayout.BikeZone;
            var ex = new List<Transform> { Seven.transform, Bike.transform };
            foreach (var f in AllFolk()) ex.Add(f.transform);
            if (GuideLight != null) ex.Add(GuideLight);
            if (Jammers != null) foreach (var j in Jammers) if (j != null) ex.Add(j);
            if (Transport != null) ex.Add(Transport);
            CameraOccluders.IncludeHuge = true;
            CameraOccluders.Build(ex.ToArray());
            HoverBike.IgnoreRoots = ex.ToArray(); ChapterCamera.RideIgnore = ex.ToArray();
            ChapterCamera.RideArea = () => RideAreaName(Riding ? Bike.transform.position : P7);
            Dlg.LineStarted += OnLine;
            DialogueFraming.Speaker = id => id == "seven" ? Seven.transform : id == "guard" ? Guard?.transform : id == "worker" ? Worker?.transform : id == "caretaker" ? Caretaker?.transform : id == "viper" ? Viper?.transform : null;
            _wind = AudioKit.Loop("mars_amb_wind", null, null, 0.55f, AudioKit.Bus.Ambience);
            _market = AudioKit.Loop("mars_amb_market", null, null, 0f, AudioKit.Bus.Ambience);
            if (Viper != null) Viper.Role = "viper";
            StartCoroutine(Flow(startCp));
            StartCoroutine(World());
        }
        void OnDestroy() { if (Dlg != null) Dlg.LineStarted -= OnLine; if (I == this) I = null; HoverBike.ZoneTest = VenusLayout.BikeZone; DialogueFraming.Speaker = null; }
        IEnumerator FirstFrame() { Cap.Log($"first frame (Mars): fade {UI.FadeAlpha:0.00} (1 = black)"); yield return Cap.Shot("m00_first_frame"); }

        IEnumerable<MarsFolk> AllFolk()
        {
            foreach (var f in new[] { Guard, Worker, Caretaker, Viper }) if (f != null) yield return f;
            if (Townsfolk != null) foreach (var f in Townsfolk) if (f != null) yield return f;
            if (Drivers != null) foreach (var f in Drivers) if (f != null) yield return f;
        }

        string RideAreaName(Vector3 p)
        {
            var z = MarsLayout.ZoneAt(p);
            if (z == "Landing") return "landing site";
            if (z == "WestRoad" || z == "Market" || z.StartsWith("Street")) return "settlement";
            if (z.StartsWith("Maint") || z.StartsWith("Floor") || z == "Causeway") return "maintenance route";
            if (z.StartsWith("Check") || z == "Bridge" || z == "EastRoad" || z == "ServiceGateZ") return "the crossing";
            if (z.Contains("Rim") || z == "Overlook") return "the rims";
            if (z.Contains("Span") || z.StartsWith("Plaza") || z.Contains("Bluff")) return "temple approach";
            return z;
        }

        // the speaking character's head and body move with the voice; Viper's hood flares on his sharper lines
        AudioSource _voiceSrc;
        AudioSource VoiceSource() { if (_voiceSrc == null) { var go = GameObject.Find("Voice_7"); if (go != null) _voiceSrc = go.GetComponent<AudioSource>(); } return _voiceSrc; }
        void OnLine(VoiceBank.Line l)
        {
            foreach (var f in AllFolk()) f.Voice = null;
            MarsFolk who = l.Speaker == "guard" ? Guard : l.Speaker == "worker" ? Worker : l.Speaker == "caretaker" ? Caretaker : l.Speaker == "viper" ? Viper : null;
            if (who != null) { who.Voice = VoiceSource(); if (who.HasState("talk") && !who.Busy && who.State != "walk") who.Play("talk", 0.3f); }
            if (Viper != null) Viper.Flare = l.Speaker == "viper" && (l.Text.Contains("concerns") || l.Text.Contains("History") || l.Text.Contains("turn on") || l.Text.Contains("careless")) ? 1f : 0f;
        }

        // ------------------------------------------------------------------------------------------ helpers
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
        /// <summary>A two-shot / medium shot from the side of the line between two people, the first framing (either side,
        /// a few distances and heights) with a clear view of both - never from inside a wall or behind one.</summary>
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
        /// <summary>An insert / single from the first camera with a clear view - no wall and no other person in the way.</summary>
        void ShotClear(Vector3 look, Vector3[] cams, float fov, float ease)
        {
            foreach (var c in cams)
            {
                if (!ChapterCamera.ClearLine(c, look)) continue;
                bool person = false;
                foreach (var f in AllFolk()) { if (!f.gameObject.activeInHierarchy) continue; var q = f.transform.position + Vector3.up * 1.0f; var seg = look - c; float t = Mathf.Clamp01(Vector3.Dot(q - c, seg) / seg.sqrMagnitude); if ((c + seg * t - q).magnitude < 0.7f && t < 0.92f) { person = true; break; } }
                if (!person) { Shoot(c, look, fov, ease); return; }
            }
            Shoot(cams[0], look, fov, ease);
        }
        IEnumerator Say(string scene, int from = 1, int to = 99)
        {
            var ls = MarsDialogue.Scene(scene, from, to);
            if (ls.Count == 0) { Log($"no lines for {scene} {from}-{to}"); yield break; }
            yield return Dlg.PlayLines(ls, false);
        }
        /// <summary>Lines during gameplay (riding / walking on): subtitles + voice, no camera change.</summary>
        IEnumerator SayInPlay(string scene, int from, int to) { yield return Say(scene, from, to); }
        bool Complete(string id) { bool first = MarsState.CompleteScene(id); Log($"scene {id} complete{(first ? "" : " (already done)")}"); return first; }
        void Checkpoint(int cp) { if (MarsState.Reach(cp)) Log($"checkpoint {cp}: {MarsLayout.CpNames[cp]} (saved for Continue)"); }
        bool SkipButton()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current; var gp = UnityEngine.InputSystem.Gamepad.current;
            return (kb != null && kb.enterKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame);
        }
        void SetPrompt(string label, ControlsLegend.Btn btn = ControlsLegend.Btn.Interact)
        {
            Prompt = label; PromptBtn = btn;
            Legend.Context(btn, label);
        }
        void SetZone(string name, bool on) { var z = Ground.FindZone(name); if (z != null) z.Enabled = on; }
        static void Glow(Renderer r, float v) { if (r == null) return; var m = r.material; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white * v); }

        void ShowGuide(Vector3 at) { NextMarker = at; if (GuideLight != null) { GuideLight.gameObject.SetActive(true); } }
        void HideGuide() { NextMarker = null; UI.SetMarker(null); }   // QF40 (6): GuideOrb fades it out

        /// <summary>Wait for a story condition (Venus's rule): check runs log a soft-lock after autoTimeout; a player without
        /// progress gets the hint after HintAfter s (the guide light pulses) and a skip after SkipAfter s (Y / Enter).</summary>
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

        /// <summary>Ride a route: the guide light moves ahead along the points as she reaches them (at or past each).</summary>
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
            if (prev != null && prev != _objective) Objective(prev);       // QF18: the step's own objective comes back
        }

        /// <summary>Check runs: the bike steered along the points (yaw toward each, throttle by distance).</summary>
        bool _autoRide;
        IEnumerator AutoRide(Vector3[] pts, float r)
        {
            _autoRide = true; Bike.ScriptDriven = true;
            foreach (var p in pts)
            {
                for (float t0 = Time.time; Flat(Bike.transform.position - p).magnitude > r && Time.time - t0 < 40f && _autoRide;)
                {
                    var f = Flat(p - Bike.transform.position);
                    float err = Vector3.SignedAngle(Flat(Bike.transform.forward), f, Vector3.up);
                    Bike.ScriptSteer = Mathf.Clamp(err / 30f, -1f, 1f);
                    Bike.ScriptThrottle = Mathf.Abs(err) > 70f ? 0.25f : Mathf.Clamp(f.magnitude / 10f, 0.35f, 1f);
                    yield return null;
                }
                if (!_autoRide) break;
            }
            Bike.ScriptSteer = 0f; Bike.ScriptThrottle = 0f;
            _autoRide = false;
        }
        void StopAutoRide() { _autoRide = false; Bike.ScriptSteer = 0f; Bike.ScriptThrottle = 0f; }
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
        // check runs: one scripted walk at a time - a newer walk (or a finished step) ends the older one (two walks left
        // running pulled her both ways and she stood still: the first Mars run's stalls)
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

        /// <summary>On foot: lead her along guide points (the light moves on as she reaches each one, or any later one) -
        /// round walls, through gates, down steps - before a step whose spot is not in a straight line from her.</summary>
        IEnumerator GuideWalk(Vector3[] pts, string what, float r = 1.6f)
        {
            int k = 0;
            if (Auto) StartCoroutine(WalkPath(pts, 40f * pts.Length));   // QF18: the walk's budget per point, not one 40 s for all
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

        /// <summary>On foot (or stopped on the bike: she gets off first): walk up to a spot and press X.</summary>
        IEnumerator Interact(Vector3 spot, string label, string what, string hint, Vector3[] autoPath = null, Action skipped = null, float radius = 1.8f)
        {
            bool hit = false;
            IEnumerator P()
            {
                while (!hit)
                {
                    bool near = Flat(spot - P7).magnitude < radius && Mathf.Abs(Ground.Height(spot.x, spot.z) - P7.y) < 1.6f && (Seven.Current == ChapterSeven.Mode.Free || (Riding && Bike.Speed < 1.5f) || (Auto && !Riding));
                    if (!_cut) SetPrompt(near ? label : (Riding && Bike.Speed < 1.2f ? "Get off" : null));
                    if (near && (Auto || Input.Interact.WasPressedThisFrame())) { if (Riding) { Bike.Dismount(true); FollowCamera(); } hit = true; }
                    yield return null;
                }
                SetPrompt(null);
            }
            StartCoroutine(P());
            ShowGuide(spot);
            if (Auto) { if (Riding) yield return GetOff(); StartCoroutine(WalkPath(autoPath ?? new[] { spot }, 40f)); }
            yield return Until(() => hit, what, 60f, hint, spot, () => { if (Riding) Bike.Dismount(true); Seven.Place(spot, Flat(spot - P7)); hit = true; skipped?.Invoke(); });
            hit = true; if (Auto) StopWalks();
            HideGuide();
        }

        /// <summary>Telekinesis (RT) at something within range she (or the camera) looks at; the edges glow while it can be taken.</summary>
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

        /// <summary>The mount / dismount prompts in free riding stretches.</summary>
        [NonSerialized] public bool FreeBike;
        void Update()
        {
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

        void LateUpdate()
        {
            if (DemoBoot.CaptureDir != null && !Riding && Seven != null && Seven.gameObject.activeInHierarchy) FeetProbe.Sample(Seven, "mars " + MarsLayout.ZoneAt(P7));   // QF18 (16)
            // QF40 (6): the one shared guide light (GuideOrb) - eased lead ahead of her, never through a wall, fades near / in cutscenes
            if (GuideLight != null && Cam != null) GuideOrb.Drive(GuideLight, NextMarker, _cut, Cam.GetComponent<Camera>() ?? Camera.main, Seven.transform, UI, 1.2f, 0.6f, 0.03f);
            // the bike gives way to the people in the streets (hard circles round them)
            Ground.Obstacles.Clear();
            foreach (var f in AllFolk()) if (f.gameObject.activeInHierarchy) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(f.transform.position.x, f.transform.position.z), R = 0.45f });
            if (Transport != null && Transport.gameObject.activeInHierarchy) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(Transport.position.x, Transport.position.z), R = 2.4f });
            foreach (var sa in MarketStalls) { var sp = MarsLayout.MarketAt + new Vector3(Mathf.Cos(sa * Mathf.Deg2Rad), 0f, Mathf.Sin(sa * Mathf.Deg2Rad)) * 12.5f; Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(sp.x, sp.z), R = 2.0f }); }   // QF17 (6): the stalls are solid
            // QF18 (16): the props with low plinths she must walk round (the body sweep sits above a 0.3 m flange)
            foreach (var (c, r) in PropCircles) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(c.x, c.z), R = r });
            if (Dais != null && Dais.position.y > -2.5f) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(Dais.position.x, Dais.position.z), R = 3.0f });   // QF17: his raised platform
        }

        static readonly (Vector3 c, float r)[] PropCircles = { (MarsLayout.WestControl, 1.15f), (MarsLayout.EastControl, 1.15f), (MarsLayout.RelayWest, 0.8f), (MarsLayout.MechanismAt, 2.7f), (MarsLayout.Pylons[0], 0.85f), (MarsLayout.Pylons[1], 0.85f), (MarsLayout.Pylons[2], 0.85f), (MarsLayout.DrawTower, 2.1f), (MarsLayout.ViperHornAt, 0.4f) };
        static readonly float[] MarketStalls = { 40f, 70f, 110f, 140f, 220f, 250f, 290f, 320f };   // = the builder's stall angles
        // ------------------------------------------------------------------------------------------ the world's life
        IEnumerator World()
        {
            yield return null;
            // the settlement: workers, families, traders, guards going about their day (loops between stalls and doors)
            if (Townsfolk != null)
                for (int i = 0; i < Townsfolk.Length; i++)
                {
                    var f = Townsfolk[i]; if (f == null) continue;
                    var c = f.transform.position;
                    if (f.Role == "guard") { f.Play("idle"); continue; }
                    var r = 4f + (i % 4) * 2.5f;
                    var pts = new[] { c, c + new Vector3(r, 0f, 1.5f * ((i % 3) - 1)), c + new Vector3(r * 0.4f, 0f, ((i % 2) * 2 - 1) * 3f) };
                    for (int k = 0; k < pts.Length; k++) { var z = Ground.ZoneName(pts[k].x, pts[k].z); if (z == null) pts[k] = c; }
                    f.Loop(pts, f.Role == "trader" ? 5f : 2.5f, f.Role == "family" ? 0.9f : 1.15f);
                }
            while (true)
            {
                // the market murmur rises in the streets, the wind out on the plain and in the ravine
                var z = MarsLayout.ZoneAt(Riding ? Bike.transform.position : P7);
                bool town = z == "Market" || z == "WestRoad" || z.StartsWith("Street") || z.StartsWith("Check") || z == "EastRoad";
                if (_market != null) AudioKit.SetBaseVolume(_market, Mathf.MoveTowards(_market.volume, town ? 0.5f : 0f, Time.deltaTime * 0.3f));
                yield return null;
            }
        }
    }
}
