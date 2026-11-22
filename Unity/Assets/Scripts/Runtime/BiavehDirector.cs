using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using L = Seven.BiavehLayout;

namespace Seven
{
    /// <summary>
    /// Round 17 — Chapter "The Biavian Mothership (Veyra)" (Docs/CHAPTER_BIAVEH.md; BIA_03-BIA_11 here, BIA_01 / BIA_02 and
    /// BIA_12 / BIA_13 aboard in Chapter1AboardBiaveh). Built on the shared systems as the asteroid: the hover bike (the
    /// bike hold, never the beam), 7 on foot over the ground zones, telekinesis (RT), interactions (X, wrong-state prompts),
    /// the arc blades if she has them, the one saucer prefab, dialogue as data (BiavehDialogue; Nela's and the residents'
    /// lines are telepathic), checkpoints for Continue / Scene Select (BiavehState). Every wait has a hint and a skip; story
    /// triggers fire at or past their place; the guide light + marker show the way. Nobody is hurt: Ratwing ends contained,
    /// alive. Strictly monochrome; the memory echoes are translucent shimmering white, distinct from solid things and from
    /// her visions.
    /// </summary>
    public partial class BiavehDirector : MonoBehaviour
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
        public MarsFolk Nela; public MarsFolk ResidentA; public MarsFolk[] Crossers; public Transform Seedlings;
        public Transform Ratwing; public RatwingRig RRig;
        [Header("BIA_03 / 04")]
        public MemoryEcho EchoBerth; public Transform RouteDisplay; public Renderer[] RouteMarks; public Renderer RouteLine;
        [Header("BIA_05 / 06")]
        public Renderer Relay1Lamp; public Transform Cabinet1Door; public Transform[] BridgeSegments; public Vector3[] SegmentParked, SegmentAligned; public MemoryEcho EchoBridge;
        public Transform BridgeLever; public Transform PartitionA; public Renderer[] DistrictLights1;
        [Header("BIA_07")]
        public MemoryEcho EchoGarden; public Transform SeamPanel; public Renderer Relay2Lamp; public Transform UpperShutter; public Renderer OpeningLamp; public Renderer[] DistrictLights2;
        [Header("BIA_08")]
        public Transform PassageDoor, LockDoorA, LockDoorB; public Renderer PanelALamp, PanelBLamp, Relay3Lamp; public Renderer[] DistrictLights3;
        [Header("BIA_09 - BIA_11")]
        public Transform[] ChamberPartitions; public Renderer[] StationLamps; public Transform OuterBarrier, InnerBarrier; public Renderer BarrierLamp;
        public Transform ArchiveDoor; public Renderer RoutingLamp; public Transform Octa, RecordOcta; public Renderer[] CityLights; public Mesh[] DebrisMeshes; public Material DebrisMat;
        public UnityEngine.Video.VideoClip Vision6;

        public static BiavehDirector I { get; private set; }
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

        // ------------------------------------------------------------------------------------------ setup
        void Start()
        {
            I = this;
            if (DemoBoot.CaptureDir != null && !BiavehState.SceneSelectRun && !DemoBoot.Continue && DemoBoot.BiaCp >= 0) BiavehState.ResetForTest();
            int startCp = DemoBoot.BiaCp >= 0 && DemoBoot.CaptureDir != null ? DemoBoot.BiaCp : BiavehState.Checkpoint;
            if (DemoBoot.BiaCp >= 0 && DemoBoot.CaptureDir != null && !BiavehState.SceneSelectRun) BiavehState.Fake(startCp, true);
            if (startCp < L.CpBerth) startCp = L.CpBerth;
            UI.SetFade(1f);
            Legend.SetLayout(ControlsLegend.Layout.Chapter); Legend.HidePanels = true;
            if (DemoBoot.CaptureDir != null) { ChapterCapture.Ensure(); Cap.Attach(Cam.GetComponent<Camera>()); StartCoroutine(FirstFrame()); }
            if (Auto) { Seven.ScriptDriven = true; Dlg.AutoSkipAfter = DemoBoot.VenusSkip ? 0f : 0.9f; Cap.Log($"Mothership check run: {BiavehState.Summary()}"); }
            Bike.Rider = Seven; Bike.Input = Input; Bike.Swim = false; Bike.SavesToVenus = false;
            HoverBike.ZoneTest = L.BikeZone;
            MoonTraversal.Use(new MoonTraversal.Ledge[0], null, null, null, null, null);
            var ex = new List<Transform> { Seven.transform, Bike.transform };
            foreach (var f in new[] { Nela, ResidentA }) if (f != null) ex.Add(f.transform);
            if (Crossers != null) foreach (var f in Crossers) if (f != null) ex.Add(f.transform);
            if (Ratwing != null) ex.Add(Ratwing);
            if (GuideLight != null) ex.Add(GuideLight);
            foreach (var e in new[] { EchoBerth, EchoBridge, EchoGarden }) if (e != null) ex.Add(e.transform);
            if (BridgeSegments != null) foreach (var b in BridgeSegments) if (b != null) ex.Add(b);
            foreach (var t in new[] { PassageDoor, LockDoorA, LockDoorB, OuterBarrier, InnerBarrier, ArchiveDoor, SeamPanel, UpperShutter, PartitionA, Seedlings, Octa })
                if (t != null) ex.Add(t);
            if (ChamberPartitions != null) foreach (var t in ChamberPartitions) if (t != null) ex.Add(t);
            CameraOccluders.IncludeHuge = true;
            CameraOccluders.Build(ex.ToArray());
            HoverBike.IgnoreRoots = ex.ToArray(); ChapterCamera.RideIgnore = ex.ToArray();
            ChapterCamera.RideArea = () => RideAreaName(Riding ? Bike.transform.position : P7);
            Dlg.LineStarted += OnLine;
            DialogueFraming.Speaker = id => id == "seven" ? Seven.transform : id == "nela" ? Nela?.transform : id == "ratwing" ? Ratwing : id == "resident1" ? (Crossers != null && Crossers.Length > 0 ? Crossers[0]?.transform : null) : null;
            _amb = AudioKit.Loop("amb_saucer", null, null, 0.25f, AudioKit.Bus.Ambience);
            if (Nela != null) { Nela.Role = "nela"; Nela.CalmSway = true; }
            Seven.SetBladesOwned(BiavehState.BladesOwned);
            RecordHomes();
            if (AboardCut.I != null) AboardCut.I.AutoFirstLook = false;   // QF23: the mothership cuts to Panther at its own beats
            StartCoroutine(Flow(startCp));
        }
        // QF23 (6) - the walls through the parked saucer: every door / partition / barrier pivot is built AT its place (its
        // localPosition = where it stands), but Restore / Door / DoorMove set absolute localPositions ((0, 14, 0) = "raised",
        // zero = "shut"), which put the two chamber partitions, both barriers, the archive door, partition A and the passage
        // doors at the world origin = the receiving berth, through the saucer. Every move is now an offset from its home.
        readonly Dictionary<Transform, Vector3> _home = new Dictionary<Transform, Vector3>();
        void RecordHomes()
        {
            var all = new List<Transform> { PassageDoor, LockDoorA, LockDoorB, OuterBarrier, InnerBarrier, ArchiveDoor, SeamPanel, UpperShutter, PartitionA };
            if (ChamberPartitions != null) all.AddRange(ChamberPartitions);
            foreach (var t in all) if (t != null && !_home.ContainsKey(t)) _home[t] = t.localPosition;
        }
        Vector3 Home(Transform t) { if (t == null) return Vector3.zero; if (!_home.TryGetValue(t, out var h)) { h = t.localPosition; _home[t] = h; } return h; }
        void Off(Transform t, Vector3 offset) { if (t != null) t.localPosition = Home(t) + offset; }
        AudioSource _amb;
        void OnDestroy() { if (Dlg != null) Dlg.LineStarted -= OnLine; if (I == this) I = null; HoverBike.ZoneTest = VenusLayout.BikeZone; DialogueFraming.Speaker = null; MoonTraversal.UseMoon(); }
        IEnumerator FirstFrame() { Cap.Log($"first frame (the Mothership): fade {UI.FadeAlpha:0.00} (1 = black)"); yield return Cap.Shot("b00_first_frame"); StartCoroutine(HiltProof()); }
        /// <summary>QF23 (7) proof: the first time she is free on foot with the blades owned - her back from behind and both
        /// sides (the sheathed hilts: a neat X on her back, nothing trailing), the measured hilt extent.</summary>
        IEnumerator HiltProof()
        {
            while (Seven == null || !Seven.gameObject.activeInHierarchy || _cut || Riding || !Seven.BladesOwned) { if (Time.time > 900f) yield break; yield return null; }
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) yield return null;
            Log("BLADES SHEATHED (QF23 7): " + Seven.HiltReport());
            var f = Flat(Seven.transform.forward).normalized; var r = Vector3.Cross(Vector3.up, f); var h = P7 + Vector3.up * 1.3f;
            foreach (var (n, o) in new[] { ("back", -f * 2.2f), ("left", -r * 2.2f - f * 0.6f), ("right", r * 2.2f - f * 0.6f) })
            { Cam.Shot(h + o + Vector3.up * 0.2f, h, 0.01f, true, 40f); for (float t = 0f; t < 0.15f; t += Time.deltaTime) yield return null; yield return Shot("b03z_qf23_hilts_from_" + n); }
            if (!_cut) FollowCamera();
        }
        string RideAreaName(Vector3 p) { var z = L.ZoneAt(p); return z.StartsWith("Road") ? "the city roads" : z; }

        // the speaker's own body moves with the voice (Nela, the resident: head; Ratwing: the jaw)
        AudioSource _voiceSrc;
        AudioSource VoiceSource() { if (_voiceSrc == null) { var go = GameObject.Find("Voice_7"); if (go != null) _voiceSrc = go.GetComponent<AudioSource>(); } return _voiceSrc; }
        void OnLine(VoiceBank.Line l)
        {
            if (Nela != null) Nela.Voice = null; if (RRig != null) RRig.Voice = null;
            if (Crossers != null) foreach (var c in Crossers) if (c != null) c.Voice = null;
            if (l.Speaker == "nela" && Nela != null && Flat(Nela.transform.position - P7).magnitude < 25f) { var tg = GameObject.Find("Voice_Cat_Telepathy"); Nela.Voice = tg != null ? tg.GetComponent<AudioSource>() : VoiceSource(); }   // her lines play on the telepathy source
            if (l.Speaker == "ratwing" && RRig != null) RRig.Voice = VoiceSource();
            if (l.Speaker == "resident1" && Crossers != null && Crossers.Length > 0 && Crossers[0] != null) Crossers[0].Voice = VoiceSource();
        }

        void LateUpdate()
        {
            if (DemoBoot.CaptureDir != null && !Riding && Seven != null && Seven.gameObject.activeInHierarchy) FeetProbe.Sample(Seven, "mothership " + L.ZoneAt(P7));
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
            foreach (var f in new[] { Nela, ResidentA }) if (f != null && f.gameObject.activeInHierarchy) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(f.transform.position.x, f.transform.position.z), R = 0.45f });
            foreach (var (c, r) in PropCircles) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(c.x, c.z), R = r });
            foreach (var c in _planters) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(c.x, c.z), R = 1.5f });
        }
        static readonly System.Collections.Generic.List<Vector3> _planters = L.GardenPlanters();
        static readonly (Vector3 c, float r)[] PropCircles =
        {
            (L.DisplayAt, 0.8f), (L.PillarAt, 1.1f), (L.CabinetAt, 0.7f), (L.BridgeControl, 0.5f), (L.Relay3At, 0.8f), (L.BarrierControl, 0.5f),
            (L.PartitionStations[0], 0.5f), (L.PartitionStations[1], 0.5f), (L.RoutingControl, 0.6f), (L.OctaAt, 0.45f), (L.BenchAt, 0.7f),
        };

        // ------------------------------------------------------------------------------------------ helpers (as Mars)
        IEnumerator Wait(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }
        void Objective(string s) { _objective = s; UI.SetObjective(s); Log("objective: " + s); }
        void Log(string s) { Debug.Log("[Seven] " + s); if (DemoBoot.CaptureDir != null) Cap?.Log(s); }
        IEnumerator Shot(string name) { if (!Shots) yield break; yield return Cap.Shot(name); }
        (Vector3 p, Vector3 l, float f) _lastShot;
        void Shoot(Vector3 pos, Vector3 look, float fov = 50f, float ease = 1f) { Cam.Subjects.Clear(); Cam.Shot(pos, look, ease, ease <= 0.05f, fov); _lastShot = (pos, look, fov); }
        /// <summary>QF23 (10, 15): a cut to Panther aboard - the REAL saucer interior (AboardCut), his mood for the beat, his lines
        /// played while we see him; then back to the shot we came from. Falls back to the old inset if a scene has no AboardCut.</summary>
        IEnumerator Cutaway(string shot, AboardCut.Mood mood, string frame = "him", float hold = 1.2f, params string[] lines)
        {
            var ac = AboardCut.I;
            if (ac == null) { yield return AboardInset(shot, mood == AboardCut.Mood.Twitch); if (lines.Length > 0) yield return SayIds(lines); yield break; }
            var back = _lastShot;
            ac.Follow = Seven.transform; ac.Rat = _ratLinked && Ratwing != null ? Ratwing : null;
            ac.Begin(mood);
            var (p, l) = ac.Frame(frame); Cam.Subjects.Clear(); Cam.Shot(p, l, 0.01f, true, 40f);
            for (float t = 0f; t < 0.35f; t += Time.deltaTime) yield return null;
            yield return Shot(shot);
            if (lines.Length > 0) yield return SayIds(lines);
            for (float t = 0.35f; t < hold; t += Time.deltaTime) yield return null;
            ac.End();
            if (back.f > 0f) Shoot(back.p, back.l, back.f, 0.01f);
            Log($"cut aboard (QF23 10): Panther in the saucer interior - {mood}{(lines.Length > 0 ? ", lines " + string.Join(" ", lines) : "")}");
        }
        bool _ratLinked;
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
            var ls = BiavehDialogue.Scene(scene, from, to);
            if (ls.Count == 0) { Log($"no lines for {scene} {from}-{to}"); yield break; }
            yield return Dlg.PlayLines(ls, false);
        }
        IEnumerator SayIds(params string[] ids) { var ls = BiavehDialogue.Ids(ids); if (ls.Count > 0) yield return Dlg.PlayLines(ls, false); }
        bool Complete(string id) { bool first = BiavehState.CompleteScene(id); Log($"scene {id} complete{(first ? "" : " (already done)")}"); return first; }
        void Checkpoint(int cp) { if (BiavehState.Reach(cp)) Log($"checkpoint {cp}: {L.CpNames[cp]} (saved for Continue)"); }
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
                    if (!SkipOffered && skip != null && Time.time - since > SkipAfter) { SkipOffered = true; UI.Hint("Y / Enter: skip this step", 6f); Log($"SKIP OFFERED: '{what}'"); }
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
            int k = 0; OnRoute = true;
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
            HideGuide(); OnRoute = false;
        }
        /// <summary>True while a ride route is being followed (the playtest driver stays in the saddle).</summary>
        [System.NonSerialized] public bool OnRoute;
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
            if (track != null) target = track.position;
            HideGuide();
            Seven.Gesture(target); Seven.SetEnergyPoint(1.9f, 0.25f);
            for (float t = 0f; t < 2.5f && !Seven.GesturePeak; t += Time.deltaTime) yield return null;
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

    }
}
