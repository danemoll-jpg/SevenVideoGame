using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using L = Seven.CattalusLayout;

namespace Seven
{
    /// <summary>
    /// Round 18 — Chapter "Cattalus — Panther's homeworld" (Docs/CHAPTER_CATTALUS.md; CAT_02-CAT_11 here, CAT_01 and CAT_12 aboard
    /// in Chapter1AboardCattalus). Built on the shared systems: the hover bike (out of / back into the bike hold, never the
    /// beam), 7 on foot over the ground zones, interactions (X, wrong-state prompts), the one saucer prefab, dialogue as data
    /// (CattalusDialogue; every cat is telepathic, the announcer is the square's PA), checkpoints for Continue / Scene Select
    /// (CattalusState). Panther is with her the whole chapter, on foot (ChapterCat). The race is CattalusRace. Every wait has a
    /// hint and a skip; story triggers fire at or past their place; the guide light + marker show the way. Strictly monochrome.
    /// </summary>
    public partial class CattalusDirector : MonoBehaviour
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
        public ChapterCat Cat;
        [Header("Cats")]
        public CatFolk Juniper, Steward, Duchess, Attendant, GateCat, Announcer; public CatFolk[] Citizens, Spectators, VignetteCats;
        [Header("CAT_02 - CAT_06")]
        public Transform GateLeaf, SmallBox, TransitBall; public CatatronScreen Screen;
        public Transform Trophy, Sphere, Retreat, RetreatPaper, Podium, DuchessRacer; public Renderer SphereLines; public Transform[] Prongs;
        public Transform[] VignetteProps;
        [Header("The race")]
        public CattalusRace Race;
        [Header("CAT_09 / CAT_10")]
        public UnityEngine.Video.VideoClip Vision7; public UnityEngine.Video.VideoClip[] MeadowClips; public Transform Tree;
        public Transform VisionInsert;

        public static CattalusDirector I { get; private set; }
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
            if (DemoBoot.CaptureDir != null && !CattalusState.SceneSelectRun && !DemoBoot.Continue && DemoBoot.CatCp >= 0) CattalusState.ResetForTest();
            int startCp = DemoBoot.CatCp >= 0 && DemoBoot.CaptureDir != null ? DemoBoot.CatCp : CattalusState.Checkpoint;
            if (DemoBoot.CatCp >= 0 && DemoBoot.CaptureDir != null && !CattalusState.SceneSelectRun) CattalusState.Fake(startCp, true, DemoBoot.CatLose ? CattalusState.Result.RunnerUp : CattalusState.Result.Won);
            if (startCp < L.CpCity) startCp = L.CpCity;
            UI.SetFade(1f);
            Legend.SetLayout(ControlsLegend.Layout.Chapter); Legend.HidePanels = true;
            if (DemoBoot.CaptureDir != null) { ChapterCapture.Ensure(); Cap.Attach(Cam.GetComponent<Camera>()); StartCoroutine(FirstFrame()); }
            if (Auto) { Seven.ScriptDriven = true; Dlg.AutoSkipAfter = DemoBoot.VenusSkip ? 0f : 0.9f; Cap.Log($"Cattalus check run: {CattalusState.Summary()}"); }
            Bike.Rider = Seven; Bike.Input = Input; Bike.Swim = false; Bike.SavesToVenus = false;
            HoverBike.ZoneTest = L.BikeZone;
            MoonTraversal.Use(new MoonTraversal.Ledge[0], null, null, null, null, null);
            CattalusCourse.AddZones();
            { var mz = Ground.FindZone("Meadow"); if (mz != null) mz.Field = L.MeadowHeight; }   // the meadow's rolling ground (the builder's terrain)
            var ex = new List<Transform> { Seven.transform, Bike.transform };
            if (Cat != null) ex.Add(Cat.transform);
            foreach (var f in new[] { Juniper, Steward, Duchess, Attendant, GateCat }) if (f != null) ex.Add(f.transform);
            foreach (var arr in new[] { Citizens, Spectators, VignetteCats }) if (arr != null) foreach (var f in arr) if (f != null) ex.Add(f.transform);
            foreach (var t in new[] { GuideLight, GateLeaf, SmallBox, TransitBall, Trophy, Sphere, Retreat, DuchessRacer }) if (t != null) ex.Add(t);
            if (VignetteProps != null) foreach (var t in VignetteProps) if (t != null) ex.Add(t);
            if (Race != null && Race.Toys != null) foreach (var t in Race.Toys) if (t != null) ex.Add(t);
            CameraOccluders.IncludeHuge = true;
            CameraOccluders.Build(ex.ToArray());
            HoverBike.IgnoreRoots = ex.ToArray(); ChapterCamera.RideIgnore = ex.ToArray();
            ChapterCamera.RideArea = () => RideAreaName(Riding ? Bike.transform.position : P7);
            Dlg.LineStarted += OnLine;
            DialogueFraming.Speaker = id => id == "seven" ? Seven.transform : id == "cat" ? (Cat != null ? Cat.transform : null) : id == "juniper" ? Juniper?.transform
                : id == "steward" ? Steward?.transform : id == "duchess" ? Duchess?.transform : id == "announcer" ? (Screen != null ? Screen.transform : null) : null;
            _amb = AudioKit.Loop("cat_crowd_ambience", null, null, 0.35f, AudioKit.Bus.Ambience);
            if (Cat != null) { Cat.Seven = Seven.transform; Cat.transform.localScale = Vector3.one; }
            if (AboardCut.I != null) AboardCut.I.AutoFirstLook = false;   // Panther is here with her (no cuts aboard)
            StartCoroutine(Flow(startCp));
        }
        AudioSource _amb;
        void OnDestroy() { if (Dlg != null) Dlg.LineStarted -= OnLine; if (I == this) I = null; HoverBike.ZoneTest = VenusLayout.BikeZone; DialogueFraming.Speaker = null; MoonTraversal.UseMoon(); foreach (var z in CattalusCourse.Pieces) Ground.Zones.Remove(z); }
        IEnumerator FirstFrame() { Cap.Log($"first frame (Cattalus): fade {UI.FadeAlpha:0.00} (1 = black)"); yield return Cap.Shot("c00_first_frame"); }
        string RideAreaName(Vector3 p) { var z = L.ZoneAt(p); return z.StartsWith("Course") ? "the Grand Circuit" : z; }

        // the speaker's own body moves with the voice (cats: head lift, ears, a slight tilt; telepathic - no mouth)
        AudioSource _voiceSrc, _telSrc;
        AudioSource VoiceSource() { if (_voiceSrc == null) { var go = GameObject.Find("Voice_7"); if (go != null) _voiceSrc = go.GetComponent<AudioSource>(); } return _voiceSrc; }
        AudioSource TelepathySource() { if (_telSrc == null) { var go = GameObject.Find("Voice_Cat_Telepathy"); if (go != null) _telSrc = go.GetComponent<AudioSource>(); } return _telSrc ?? VoiceSource(); }
        void OnLine(VoiceBank.Line l)
        {
            foreach (var c in new[] { Juniper, Steward, Duchess, Announcer }) if (c != null) c.Voice = null;
            var who = l.Speaker == "juniper" ? Juniper : l.Speaker == "steward" ? Steward : l.Speaker == "duchess" ? Duchess : l.Speaker == "announcer" ? Announcer : null;
            if (who != null) { who.Voice = TelepathySource(); who.LookTarget = Seven.transform; }
            if (Screen != null) Screen.Talking(l.Speaker == "announcer");
        }

        void LateUpdate()
        {
            if (DemoBoot.CaptureDir != null && !Riding && Seven != null && Seven.gameObject.activeInHierarchy) FeetProbe.Sample(Seven, "Cattalus " + L.ZoneAt(P7));
            // QF40 (6): the one shared guide light (GuideOrb) - eased lead ahead of her, never through a wall, fades near / in cutscenes
            if (GuideLight != null && Cam != null) GuideOrb.Drive(GuideLight, NextMarker, _cut, Cam.GetComponent<Camera>() ?? Camera.main, Seven.transform, UI, 1.2f, 0.6f, 0.03f);
            Ground.Obstacles.Clear();
            foreach (var f in new[] { Juniper, Steward, Duchess, Attendant, GateCat }) if (f != null && f.gameObject.activeInHierarchy) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(f.transform.position.x, f.transform.position.z), R = 0.35f * f.transform.localScale.x });
            foreach (var (c, r) in PropCircles) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(c.x, c.z), R = r });
        }
        static readonly (Vector3 c, float r)[] PropCircles =
        {
            (L.FountainAt, 4.0f), (L.TrophyAt, 1.2f), (L.RetreatAt, 1.3f), (L.DeskAt, 1.4f), (L.PodiumAt, 1.6f), (L.SmallBoxAt, 0.5f),
            (L.QuietCorner + new Vector3(0.9f, 0f, 1.2f), 1.35f), (L.QuietCorner + new Vector3(-2.4f, 0f, 1.6f), 1.35f), (L.QuietCorner + new Vector3(2.8f, 0f, -1.4f), 1.35f),   // the planters
        };

        // ------------------------------------------------------------------------------------------ helpers (as Mars)
        IEnumerator Wait(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }
        void Objective(string s) { _objective = s; UI.SetObjective(s); Log("objective: " + s); }
        void Log(string s) { Debug.Log("[Seven] " + s); if (DemoBoot.CaptureDir != null) Cap?.Log(s); }
        IEnumerator Shot(string name) { if (!Shots) yield break; yield return Cap.Shot(name); }
        (Vector3 p, Vector3 l, float f) _lastShot;
        void Shoot(Vector3 pos, Vector3 look, float fov = 50f, float ease = 1f) { Cam.Subjects.Clear(); Cam.Shot(pos, look, ease, ease <= 0.05f, fov); _lastShot = (pos, look, fov); }
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
            var ls = CattalusDialogue.Scene(scene, from, to);
            if (ls.Count == 0) { Log($"no lines for {scene} {from}-{to}"); yield break; }
            yield return Dlg.PlayLines(ls, false);
        }
        IEnumerator SayIds(params string[] ids) { var ls = CattalusDialogue.Ids(ids); if (ls.Count > 0) yield return Dlg.PlayLines(ls, false); }
        bool Complete(string id) { bool first = CattalusState.CompleteScene(id); Log($"scene {id} complete{(first ? "" : " (already done)")}"); return first; }
        void Checkpoint(int cp) { if (CattalusState.Reach(cp)) Log($"checkpoint {cp}: {L.CpNames[cp]} (saved for Continue)"); }
        bool SkipButton()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current; var gp = UnityEngine.InputSystem.Gamepad.current;
            return (kb != null && kb.enterKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame);
        }
        void SetPrompt(string label, ControlsLegend.Btn btn = ControlsLegend.Btn.Interact) { Prompt = label; PromptBtn = btn; Legend.Context(btn, label); }
        void SetZone(string name, bool on) { var z = Ground.FindZone(name); if (z != null) z.Enabled = on; }
        static void Glow(Renderer r, float v) => Glowing.Set(r, v);
        void ShowGuide(Vector3 at) { NextMarker = at; if (GuideLight != null) { GuideLight.gameObject.SetActive(true); } }
        void HideGuide() { NextMarker = null; UI.SetMarker(null); }   // QF40 (6): GuideOrb fades it out

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
                            StartCoroutine(Shot("c13w_wrong_state_" + what.Replace(' ', '_')));
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
            yield return Shot("c13x_wrong_state_on_foot_player_camera");
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
