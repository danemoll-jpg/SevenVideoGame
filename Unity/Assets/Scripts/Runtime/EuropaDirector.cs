using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// Round 14 — Chapter "Europa — Beneath the Ice" (Docs/CHAPTER_EUROPA.md, sections 2-16; section 1 and 16-17 aboard are
    /// in Chapter1AboardEuropa). A first playable pass on the Venus systems: the hover bike now swims (HoverBike.Swim), the
    /// dialogue is data (EuropaDialogue, voices in Resources/EuropaVoice), the Cetaceans swim procedurally (Cetacean),
    /// checkpoints save for Continue / Scene Select (EuropaState). Every wait on the player has a hint (45 s) and a skip
    /// (2 min), as on Venus; story triggers fire at or past their place; the way to go is shown by a soft white guide light
    /// (and on the settlement leg by the Cetacean guide). Strictly monochrome; nobody is killed or hurt (the guards stand
    /// aside, the workers walk out, Tunneler escapes; the escape ride's crash = a white flash and a retry from its start).
    /// </summary>
    public partial class EuropaDirector : MonoBehaviour
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
        public Atmosphere Atmo;
        public BubbleHelmet Helmet;
        public Cetacean Keeper, Guide, HugeOne;
        public Cetacean[] Pod;                       // the settlement's Cetaceans (calm loops)
        public Transform[] Vehicles;                 // small vehicles moving between the platforms
        public Transform[] RaceRings, Rivals;
        public Transform RaceRegistration;
        public Transform Tunneler, TunnelerHead, TunnelerCraft;
        public Camera ScreenCam; public Renderer Screen;
        public GameObject[] Barriers;                // close in section 8
        public GameObject DoorHolding, DoorTransport, DoorMachine, WindowShutter;
        public Transform[] Scree;                    // the workers (placeholder figures where the model is missing)
        public Animator[] ScreeAnims;
        public Transform[] Transports;               // the evacuation transports (leave one by one)
        public Transform[] DeviceSlots; public Renderer[] DeviceGlows;
        public Renderer[] BasePanels;                // transport access, lift power, schematic
        public Transform[] EscapeSupports; public Transform EscapeCollapse;
        public ParticleSystem Ruptures, Bubbles;
        public Transform Rune, RecordIco;
        public Light[] BaseLights;
        public UnityEngine.Video.VideoClip Vision3;
        public Transform GuideLight;

        public static EuropaDirector I { get; private set; }
        bool Auto => DemoBoot.CaptureDir != null && !DemoBoot.PlayerSim;
        bool Shots => DemoBoot.CaptureDir != null && Cap != null;
        ChapterCapture Cap => ChapterCapture.I;
        Vector3 P7 => Seven.transform.position;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        string _objective; bool _cut;
        public string ObjectiveNow => _objective;
        /// <summary>The next place to go (the guide light) - what a player sees; the player-style run steers by it.</summary>
        public Vector3? NextMarker { get; private set; }
        public string WaitingFor { get; private set; }
        public bool SkipOffered { get; private set; }
        public bool ChapterDone { get; private set; }
        public int Hints { get; private set; }
        public int Skips { get; private set; }
        public int Crashes { get; private set; }
        public float HintAfter => DemoBoot.VenusHintFast ? 12f : 45f;
        public float SkipAfter => DemoBoot.VenusHintFast ? 30f : 120f;
        AudioSource _amb, _drill;

        // ------------------------------------------------------------------------------------------ setup
        void Start()
        {
            I = this;
            if (DemoBoot.CaptureDir != null && !EuropaState.SceneSelectRun && !DemoBoot.Continue && DemoBoot.EuropaCp >= 0) EuropaState.ResetForTest();
            int startCp = DemoBoot.EuropaCp >= 0 && DemoBoot.CaptureDir != null ? DemoBoot.EuropaCp : EuropaState.Checkpoint;
            if (DemoBoot.EuropaCp >= 0 && DemoBoot.CaptureDir != null && !EuropaState.SceneSelectRun) FakeProgress(startCp);
            // the first frame is black (the arrival's title card was aboard; any other start fades in)
            UI.SetFade(1f);
            Legend.SetLayout(ControlsLegend.Layout.Chapter); Legend.HidePanels = true;
            if (DemoBoot.CaptureDir != null) { ChapterCapture.Ensure(); Cap.Attach(Cam.GetComponent<Camera>()); StartCoroutine(FirstFrame()); }
            if (Auto) { Seven.ScriptDriven = true; Dlg.AutoSkipAfter = DemoBoot.VenusSkip ? 0f : 0.9f; Cap.Log($"Europa check run: {EuropaState.Summary()}"); }
            Bike.Rider = Seven; Bike.Input = Input; Bike.Swim = true; Bike.SavesToVenus = false; Bike.SwimClamp = p => EuropaLayout.SwimClamp(p, 1.2f);
            Bike.Bubbles = Bubbles;
            // characters and the helmet are never walls (the wall push / camera checks only see the level)
            var ex = new List<Transform> { Seven.transform, Bike.transform };
            if (Helmet != null && Helmet.Sphere != null) ex.Add(Helmet.Sphere);
            foreach (var c in new Component[] { Keeper, Guide, HugeOne, Tunneler, TunnelerCraft, GuideLight }) if (c != null) ex.Add(c.transform);
            if (Pod != null) foreach (var c in Pod) if (c != null) ex.Add(c.transform);
            foreach (var arr in new[] { Scree, Rivals, Vehicles, DeviceSlots }) if (arr != null) foreach (var t in arr) if (t != null) ex.Add(t);
            CameraOccluders.Build(ex.ToArray());
            Dlg.LineStarted += OnLine;
            _amb = AudioKit.Loop("europa_amb_underwater", null, null, 0f, AudioKit.Bus.Ambience);
            if (Tunneler != null) Tunneler.gameObject.SetActive(false);
            StartCoroutine(Flow(startCp));
            StartCoroutine(World());
            StartCoroutine(Watchdog());
        }

        void FakeProgress(int cp)
        {
            // check runs started at a checkpoint (-europacp n): the state a real playthrough has there (like Scene Select)
            for (int c = 0; c <= cp; c++) EuropaState.Reach(c);
            string[] doneBy = { "", "EUR_01|EUR_02", "EUR_03", "EUR_04|EUR_05", "EUR_06|EUR_07", "EUR_08", "EUR_09|EUR_10", "EUR_11|EUR_12|EUR_13", "EUR_14|EUR_15" };
            for (int c = 1; c <= cp && c < doneBy.Length; c++) foreach (var id in doneBy[c].Split('|')) if (id.Length > 0) EuropaState.CompleteScene(id);
            if (cp > EuropaLayout.CpWorkers) for (int i = 0; i < 3; i++) EuropaState.PlaceDevice("dev_" + i);
        }

        IEnumerator FirstFrame()
        {
            Cap.Log($"first frame (Europa): fade {UI.FadeAlpha:0.00} (1 = black)");
            yield return Cap.Shot("e00_first_frame");
        }

        void OnDestroy() { if (Dlg != null) Dlg.LineStarted -= OnLine; }

        /// <summary>Under each Keeper line a soft whale call (starting a beat before, trailing after - the spec's convention);
        /// under each Scree line Dan's Scree noise; the keeper's head and fins move with the voice.</summary>
        void OnLine(VoiceBank.Line l)
        {
            if (l.Speaker == "keeper")
            {
                AudioKit.Play(UnityEngine.Random.value < 0.5f ? "europa_call_deep" : "europa_call_answer", Keeper != null ? Keeper.transform.position : (Vector3?)null, 0.32f, 0.08f, AudioKit.Bus.Sfx, 300f);
                if (Keeper != null) Keeper.Voice = FindVoiceSource();
            }
            else if (Keeper != null) Keeper.Voice = null;
            if (l.Speaker == "scree") AudioKit.Play("scree_grunt", P7 + Seven.transform.forward * 2f, 0.45f, 0.12f, AudioKit.Bus.Sfx, 20f);
            if (l.Speaker == "tunneler") StartCoroutine(TunnelerTalks(l));
        }
        AudioSource _voiceSrc;
        AudioSource FindVoiceSource() { if (_voiceSrc == null) { var go = GameObject.Find("Voice_7"); if (go != null) _voiceSrc = go.GetComponent<AudioSource>(); } return _voiceSrc; }

        IEnumerator TunnelerTalks(VoiceBank.Line l)
        {
            // his drill head twists and nods with his voice's loudness (no face, no lip-sync)
            var src = FindVoiceSource(); var buf = new float[256]; float spin = 0f;
            if (TunnelerHead == null) yield break;
            var r0 = TunnelerHead.localRotation;
            for (float t = 0f; t < 8f && Dlg.Current == l; t += Time.deltaTime)
            {
                float lvl = 0f; if (src != null && src.isPlaying) { src.GetOutputData(buf, 0); foreach (var v in buf) lvl = Mathf.Max(lvl, Mathf.Abs(v)); }
                spin += lvl * 900f * Time.deltaTime;
                TunnelerHead.localRotation = r0 * Quaternion.Euler(lvl * 14f, spin, 0f);
                yield return null;
            }
            TunnelerHead.localRotation = r0;
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
            if (!Bike.Riding) Seven.Lock(on);
            if (!on) FollowCamera();
            yield return null;
        }
        void FollowCamera()
        {
            if (Bike.Riding) { Cam.Target = Bike.transform; Cam.Distance = 7.2f; Cam.LookHeight = 1.5f; Cam.AutoBehind = true; }
            else { Cam.Target = Seven.transform; Cam.Distance = 3.8f; Cam.LookHeight = 1.45f; Cam.AutoBehind = Auto; }
            Cam.Follow();
        }
        IEnumerator Say(string scene, int from = 1, int to = 99)
        {
            var ls = EuropaDialogue.Scene(scene, from, to);
            if (ls.Count == 0) { Log($"no lines for {scene} {from}-{to}"); yield break; }
            yield return Dlg.PlayLines(ls, false);
        }
        bool Complete(string id) { bool first = EuropaState.CompleteScene(id); Log($"scene {id} complete{(first ? "" : " (already done)")}"); return first; }
        void Checkpoint(int cp) { if (EuropaState.Reach(cp)) Log($"checkpoint {cp}: {EuropaLayout.CpNames[cp]} (saved for Continue)"); }
        bool SkipButton()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current; var gp = UnityEngine.InputSystem.Gamepad.current;
            return (kb != null && kb.enterKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame);
        }

        /// <summary>Wait for a story condition (Venus's rule): check runs log a soft-lock after autoTimeout; a player without
        /// progress gets the hint after <see cref="HintAfter"/> s (the guide light pulses) and a skip after
        /// <see cref="SkipAfter"/> s (Y / Enter: white fade, the step is done for her).</summary>
        IEnumerator Until(Func<bool> cond, string what, float autoTimeout, string hint, Vector3? at, Action skip)
        {
            float t0 = Time.time, since = Time.time, best = at.HasValue ? Vector3.Distance(P7, at.Value) : 0f; bool hinted = false;
            WaitingFor = what; SkipOffered = false;
            while (!cond())
            {
                if (Auto && Time.time - t0 > autoTimeout) { Cap.Soft++; Cap.Log($"SOFT-LOCK? waiting for '{what}' {autoTimeout:0}s (7 at {P7})"); skip?.Invoke(); break; }
                bool busy = _cut || PauseMenu.IsOpen || Dlg.Busy;
                if (busy) { since += Time.deltaTime; yield return null; continue; }
                if (at.HasValue) { float d = Vector3.Distance(Bike.Riding ? Bike.transform.position : P7, at.Value); if (d < best - 2f) { best = d; since = Time.time; } }
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

        /// <summary>The context prompt on screen right now (X), or null - what a player sees.</summary>
        public string Prompt { get; private set; }
        void SetPrompt(string label) { Prompt = label; Legend.Context(ControlsLegend.Btn.Interact, label); }
        public bool Riding => Bike != null && Bike.Riding;
        public bool InCutscene => _cut;

        void ShowGuide(Vector3 at) { NextMarker = at; if (GuideLight != null) { GuideLight.gameObject.SetActive(true); GuideLight.position = at; } }
        void HideGuide() { NextMarker = null; if (GuideLight != null) GuideLight.gameObject.SetActive(false); }

        /// <summary>Ride a route: the guide light moves ahead along the points as she reaches them (at or past each: the
        /// nearest point ahead counts); the autopilot steers the bike along them.</summary>
        IEnumerator RideRoute(Vector3[] pts, string what, float radius = 7f, float autoTimeout = 120f)
        {
            int k = 0;
            if (Auto) StartCoroutine(AutoSwim(pts, radius * 0.6f));
            while (k < pts.Length)
            {
                // at or past: any later point already within reach skips ahead
                for (int j = pts.Length - 1; j >= k; j--) if (Vector3.Distance(Bike.transform.position, pts[j]) < radius) { k = j + 1; break; }
                if (k >= pts.Length) break;
                ShowGuide(pts[k]);
                int kk = k;
                yield return Until(() => Vector3.Distance(Bike.transform.position, pts[kk]) < radius || ReachedAfter(pts, kk, radius), $"{what} ({kk + 1}/{pts.Length})", autoTimeout,
                    "Follow the soft white light (steer with the left stick, RT forward, A up, B down)", pts[kk], () => PlaceRiding(pts[kk]));
                k = kk + 1;
            }
            HideGuide();
        }
        bool ReachedAfter(Vector3[] pts, int k, float r) { for (int j = k + 1; j < pts.Length; j++) if (Vector3.Distance(Bike.transform.position, pts[j]) < r) return true; return false; }

        void PlaceRiding(Vector3 at)
        {
            Bike.Place(at, Bike.transform.eulerAngles.y);
            if (!Bike.Riding) { Bike.Mount(); FollowCamera(); }
        }

        /// <summary>Check runs: the bike steered at each point in turn (yaw toward it, up / down toward its height).</summary>
        bool _autoSwim;
        IEnumerator AutoSwim(Vector3[] pts, float r)
        {
            _autoSwim = true; Bike.ScriptDriven = true;
            foreach (var p in pts)
            {
                for (float t0 = Time.time; Vector3.Distance(Bike.transform.position, p) > r && Time.time - t0 < 40f && _autoSwim;)
                {
                    var to = p - Bike.transform.position; var f = Flat(to);
                    float err = Vector3.SignedAngle(Flat(Bike.transform.forward), f, Vector3.up);
                    Bike.ScriptSteer = Mathf.Clamp(err / 30f, -1f, 1f);
                    Bike.ScriptThrottle = Mathf.Abs(err) > 70f ? 0.25f : Mathf.Clamp(f.magnitude / 6f, 0.35f, 1f);
                    Bike.ScriptRise = Mathf.Clamp(to.y / 2.5f, -1f, 1f);
                    yield return null;
                }
                if (!_autoSwim) break;
            }
            Bike.ScriptSteer = 0f; Bike.ScriptThrottle = 0f; Bike.ScriptRise = 0f;
            _autoSwim = false;
        }
        void StopAutoSwim() { _autoSwim = false; Bike.ScriptSteer = 0f; Bike.ScriptThrottle = 0f; Bike.ScriptRise = 0f; }

        IEnumerator StopBike(float seconds = 2f)
        {
            bool sd = Bike.ScriptDriven; Bike.ScriptDriven = true; Bike.ScriptThrottle = -1f; Bike.ScriptSteer = 0f; Bike.ScriptRise = 0f;
            for (float t = 0f; t < seconds && Bike.Speed > 0.3f; t += Time.deltaTime) yield return null;
            Bike.ScriptThrottle = 0f; Bike.ScriptDriven = sd || Auto;
        }

        IEnumerator WalkSeven(Vector3 to, float timeout)
        {
            bool sd = Seven.ScriptDriven; Seven.ScriptDriven = true; Seven.Lock(false);
            for (float t0 = Time.time; Flat(to - P7).magnitude > 0.35f && Time.time - t0 < timeout;) { Seven.ScriptMove = Flat(to - P7).normalized * 0.8f; yield return null; }
            Seven.ScriptMove = Vector3.zero; Seven.ScriptDriven = sd;
        }

        // ---- the base's rooms and doors: the guide light (and the check run's walk) go door to door, never through a wall
        static char RoomOf(Vector3 p)
        {
            if (p.x < -209.4f) return 'T';
            if (p.x < -191.4f) return 'H';
            if (p.x > -170.6f) return 'M';
            if (p.z > 493.2f) return 'W';
            if (p.z > 480.6f) return 'C';
            return 'S';
        }
        static readonly Dictionary<string, Vector3> Doors = new Dictionary<string, Vector3>
        {
            { "SC", new Vector3(-181f, EuropaLayout.BaseY, 480.6f) }, { "CH", new Vector3(-191.8f, EuropaLayout.BaseY, 488f) }, { "HT", new Vector3(-209.1f, EuropaLayout.BaseY, 488f) },
            { "CM", new Vector3(-170.3f, EuropaLayout.BaseY, 488f) }, { "CW", new Vector3(-181f, EuropaLayout.BaseY, 493.3f) },
        };
        static Vector3 RoomCentre(char r) => r switch { 'S' => new Vector3(-181f, 0f, 466f), 'C' => new Vector3(-181f, 0f, 487f), 'H' => new Vector3(-200f, 0f, 488f), 'T' => new Vector3(-217f, 0f, 488f), 'M' => new Vector3(-159f, 0f, 490f), _ => new Vector3(-181f, 0f, 499f) };
        /// <summary>The door points between where she is and the target, then the target (a breadth-first walk over the rooms).</summary>
        public static List<Vector3> RouteTo(Vector3 from, Vector3 to)
        {
            var l = new List<Vector3>();
            if (Mathf.Abs(from.y - EuropaLayout.BaseY) > 2f || Mathf.Abs(to.y - EuropaLayout.BaseY) > 2f) { l.Add(to); return l; }
            char a = RoomOf(from), b = RoomOf(to);
            var prev = new Dictionary<char, char>(); var q = new Queue<char>(); q.Enqueue(a); prev[a] = a;
            while (q.Count > 0) { var c = q.Dequeue(); if (c == b) break; foreach (var k in Doors.Keys) { char n = k[0] == c ? k[1] : k[1] == c ? k[0] : '\0'; if (n == '\0' || prev.ContainsKey(n)) continue; prev[n] = c; q.Enqueue(n); } }
            if (!prev.ContainsKey(b)) { l.Add(to); return l; }
            var path = new List<char>(); for (char c = b; c != a; c = prev[c]) path.Add(c); path.Reverse();
            char cur = a;
            foreach (var c in path)
            {
                var key = Doors.ContainsKey("" + cur + c) ? "" + cur + c : "" + c + cur; var d = Doors[key];
                var across = RoomCentre(c) - RoomCentre(cur); across = Mathf.Abs(across.x) > Mathf.Abs(across.z) ? new Vector3(Mathf.Sign(across.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(across.z));
                if ((d - from).sqrMagnitude > 0.5f) l.Add(d - across * 1.1f);     // in front of the door, then through it
                l.Add(d + across * 1.3f);
                cur = c;
            }
            if ((to.z < 456.5f && to.z > 446f) && a != 'S') { l.Insert(l.Count, new Vector3(-181f, EuropaLayout.BaseY, 458f)); }
            l.Add(to);
            return l;
        }
        /// <summary>Show the way (door to door) until she is at the spot - runs alongside a wait.</summary>
        IEnumerator GuideTo(Vector3 spot, Func<bool> done)
        {
            while (!done())
            {
                var r = RouteTo(P7, spot);
                int k = 0; while (k < r.Count - 1 && Flat(r[k] - P7).magnitude < 0.7f) k++;
                ShowGuide(r[k] + Vector3.up * 1.2f);
                yield return Wait(0.25f);
            }
        }
        IEnumerator WalkRoute(Vector3 to, float timeout)
        {
            float t0 = Time.time;
            var r = RouteTo(P7, to);
            foreach (var p in r) { if (Time.time - t0 > timeout) break; yield return WalkSeven(p, Mathf.Max(2f, timeout - (Time.time - t0))); }
        }

        /// <summary>On foot: walk up to a spot and press X (the context prompt shows when she is there). Check runs walk it.</summary>
        IEnumerator Interact(Vector3 spot, string label, string what, string hint, Action skipped = null, float radius = 1.6f)
        {
            bool reached = false;
            StartCoroutine(GuideTo(spot, () => reached));
            bool hit = false;
            IEnumerator Prompt()
            {
                while (!hit)
                {
                    bool near = Flat(spot - P7).magnitude < radius && Mathf.Abs(spot.y - P7.y) < 1.2f && Seven.Current == ChapterSeven.Mode.Free;
                    SetPrompt(near ? label : null);
                    if (near && (Auto || Input.Interact.WasPressedThisFrame())) hit = true;
                    yield return null;
                }
                SetPrompt(null);
            }
            StartCoroutine(Prompt());
            if (Auto) StartCoroutine(WalkRoute(spot, 40f));
            yield return Until(() => hit, what, 60f, hint, spot, () => { Seven.Place(spot, Flat(spot - P7)); hit = true; skipped?.Invoke(); });
            hit = true; reached = true;
            HideGuide();
        }

        static void Glow(Renderer r, float v) { if (r == null) return; var m = r.material; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white * v); }
        void Teach(params (ControlsLegend.Btn, string)[] items) => Legend.Teach(items);

        // ------------------------------------------------------------------------------------------ the world's life
        IEnumerator World()
        {
            if (Pod != null) foreach (var c in Pod) if (c != null && c.Points != null && c.Points.Length > 0) c.Loop(c.Points, c.Speed);
            if (HugeOne != null) HugeOne.gameObject.SetActive(false);
            float vt = 0f;
            var v0 = new Vector3[Vehicles != null ? Vehicles.Length : 0];
            for (int i = 0; i < v0.Length; i++) v0[i] = Vehicles[i].position;
            while (true)
            {
                vt += Time.deltaTime;
                // the small vehicles shuttle between the platforms
                for (int i = 0; i < v0.Length; i++) if (Vehicles[i] != null)
                    {
                        float s = Mathf.Sin(vt * 0.12f + i * 1.7f);
                        var p = v0[i] + new Vector3(s * 14f, Mathf.Sin(vt * 0.3f + i) * 1.2f, Mathf.Cos(vt * 0.1f + i) * 6f);
                        var d = p - Vehicles[i].position; if (d.sqrMagnitude > 1e-4f) Vehicles[i].rotation = Quaternion.Slerp(Vehicles[i].rotation, Quaternion.LookRotation(d), Time.deltaTime * 2f);
                        Vehicles[i].position = p;
                    }
                // the ambience and the light: under water vs at the surface
                bool under = (Bike.Riding ? Bike.transform.position.y : P7.y) < EuropaLayout.WaterY;
                if (_amb != null) AudioKit.SetBaseVolume(_amb, Mathf.MoveTowards(_amb.volume, under ? 0.8f : 0.15f, Time.deltaTime * 0.4f));
                if (Helmet != null) Helmet.On = under || _helmetForced;
                yield return null;
            }
        }
        bool _helmetForced = true;

        IEnumerator Watchdog()
        {
            // the story flow must keep moving; nothing here may teleport her except a skip the player takes
            while (!ChapterDone) { yield return Wait(5f); }
        }

        // ------------------------------------------------------------------------------------------ the flow
        IEnumerator Flow(int cp)
        {
            Restore(cp);
            yield return null; yield return null;
            if (cp <= EuropaLayout.CpArrival) { yield return Arrival(); Checkpoint(EuropaLayout.CpDescent); }
            else yield return UI.Fade(0f, 1.2f);
            if (EuropaState.Checkpoint <= EuropaLayout.CpDescent) { yield return Descent(); Checkpoint(EuropaLayout.CpSettlement); }
            if (EuropaState.Checkpoint <= EuropaLayout.CpSettlement) { yield return Settlement(); Checkpoint(EuropaLayout.CpApproach); }
            if (EuropaState.Checkpoint <= EuropaLayout.CpApproach) { yield return Machinery(); yield return Approach(); Checkpoint(EuropaLayout.CpBase); }
            if (EuropaState.Checkpoint <= EuropaLayout.CpBase) { yield return BaseContact(); Checkpoint(EuropaLayout.CpWorkers); }
            if (EuropaState.Checkpoint <= EuropaLayout.CpWorkers) { yield return Workers(); Checkpoint(EuropaLayout.CpEscape); }
            if (EuropaState.Checkpoint <= EuropaLayout.CpEscape) { yield return EscapeAll(); Checkpoint(EuropaLayout.CpTemple); }
            if (EuropaState.Checkpoint <= EuropaLayout.CpTemple) { yield return Temple(); Checkpoint(EuropaLayout.CpHome); }
            yield return Home();
        }

        /// <summary>Put everything where the checkpoint says (reload, Continue, Scene Select).</summary>
        void Restore(int cp)
        {
            foreach (var z in EuropaLayout.Zones) SetZone(z.Name, z.On);
            if (Saucer != null) Saucer.position = EuropaLayout.SaucerAt + (cp <= EuropaLayout.CpArrival ? Vector3.up * 80f : Vector3.zero);
            if (Barriers != null) foreach (var b in Barriers) if (b != null) b.SetActive(false);
            if (RecordIco != null) RecordIco.gameObject.SetActive(false);
            if (Transports != null) foreach (var t in Transports) if (t != null) t.gameObject.SetActive(cp <= EuropaLayout.CpWorkers);
            if (EscapeCollapse != null) EscapeCollapse.gameObject.SetActive(cp > EuropaLayout.CpEscape);
            for (int i = 0; DeviceGlows != null && i < DeviceGlows.Length; i++) { bool on = EuropaState.HasDevice("dev_" + i); if (DeviceSlots != null && i < DeviceSlots.Length && DeviceSlots[i] != null) DeviceSlots[i].gameObject.SetActive(on); }
            if (cp > EuropaLayout.CpWorkers && Scree != null) foreach (var s in Scree) if (s != null) s.gameObject.SetActive(false);
            Vector3 at; float yaw = 0f; bool riding = true;
            switch (cp)
            {
                case EuropaLayout.CpArrival: at = EuropaLayout.BikeDown; riding = false; break;
                case EuropaLayout.CpDescent: at = EuropaLayout.Dive[EuropaLayout.Dive.Length - 1]; break;
                case EuropaLayout.CpSettlement: at = EuropaLayout.Descent[EuropaLayout.Descent.Length - 1]; yaw = 40f; break;
                case EuropaLayout.CpApproach: at = EuropaLayout.Receiving + new Vector3(-3f, 0f, -3f); yaw = 45f; break;
                case EuropaLayout.CpBase: at = EuropaLayout.BayPark; yaw = 270f; riding = false; break;
                case EuropaLayout.CpWorkers: at = EuropaLayout.BayPark; yaw = 270f; riding = false; break;
                case EuropaLayout.CpEscape: at = EuropaLayout.BayPark; yaw = 200f; riding = false; break;
                case EuropaLayout.CpTemple: at = EuropaLayout.OpenWaterStop; yaw = 160f; break;
                default: at = EuropaLayout.Home[EuropaLayout.Home.Length - 1]; break;
            }
            Bike.Place(at, yaw);
            if (cp == EuropaLayout.CpArrival) { Bike.gameObject.SetActive(false); Seven.gameObject.SetActive(false); }
            else if (riding) { Seven.gameObject.SetActive(true); Bike.Mount(); }
            else
            {
                Seven.gameObject.SetActive(true);
                var stand = cp == EuropaLayout.CpBase ? EuropaLayout.DockStep : cp == EuropaLayout.CpWorkers ? EuropaLayout.HoldingSpot + Vector3.right * 2f : EuropaLayout.WindowSpot - Vector3.forward * 2f;
                if (cp >= EuropaLayout.CpWorkers) { SetZone("HoldingDoor", true); SetZone("WindowHall", cp >= EuropaLayout.CpEscape); if (DoorHolding != null) DoorHolding.SetActive(false); }
                if (cp >= EuropaLayout.CpEscape) { SetZone("TransportDoor", true); SetZone("MachineDoor", true); if (DoorTransport != null) DoorTransport.SetActive(false); if (DoorMachine != null) DoorMachine.SetActive(false); }
                Seven.Place(stand, Vector3.forward);
            }
            if (Keeper != null) { Keeper.transform.position = cp >= EuropaLayout.CpTemple ? EuropaLayout.TempleCentre + new Vector3(10f, 4f, 6f) : EuropaLayout.KeeperAt; Keeper.HoldHere(); }
            if (Guide != null) { Guide.transform.position = cp <= EuropaLayout.CpDescent ? EuropaLayout.Descent[2] + new Vector3(20f, -6f, 30f) : EuropaLayout.Receiving + new Vector3(3f, 1f, 2f); Guide.HoldHere(); }
            bool baseNow = cp >= EuropaLayout.CpBase && cp <= EuropaLayout.CpEscape;
            MusicSystem.Play(baseNow ? "europa_base" : "europa_underwater", 2.5f);
            FollowCamera(); Cam.SnapBehind();
            Log($"restore: {EuropaState.Summary()} -> {(riding ? "riding" : "on foot")} at {at}");
        }
        void SetZone(string name, bool on) { var z = Ground.FindZone(name); if (z != null) z.Enabled = on; }

        // ------------------------------------------------------------------------------------------ 2: arrival + the dive
        IEnumerator Arrival()
        {
            yield return Cutscene(true);
            // extreme wide: the saucer crosses the immense pale ice field (fractures like veins), comes to hover over the shelf
            var land = EuropaLayout.SaucerAt; var from = land + new Vector3(-110f, 34f, -80f);
            Saucer.position = from;
            Shoot(land + new Vector3(34f, 12f, -34f), from, 55f, 0.01f);
            StartCoroutine(UI.Fade(0f, 1.6f));
            AudioKit.Play("saucer_descent", null, 0.7f);
            bool shot = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 7f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Saucer.position = Vector3.Lerp(from, land, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 8f;
                Cam.Shot(land + new Vector3(34f, 12f, -34f), Saucer.position, 2f, false, 55f);
                if (!shot && t > 0.45f) { shot = true; StartCoroutine(Shot("e01_extreme_wide_ice_field")); }
                yield return null;
            }
            Saucer.position = land;
            // the beam lowers the bike into position, then 7 (in her bubble helmet); Panther watches from inside
            Shoot(land + new Vector3(10f, 2.2f, 9f), land + new Vector3(0f, 2.2f, 0f), 46f, 0.01f);
            if (SaucerBeam != null) { yield return SaucerBeam.Iris(true); yield return SaucerBeam.BeamOn(true); }
            Bike.gameObject.SetActive(true);
            if (SaucerBeam != null) yield return SaucerBeam.Lower(Bike.transform, 2.6f, Vector3.forward);
            yield return GlideBike(EuropaLayout.BikeDown, 1.2f);
            Seven.gameObject.SetActive(true); Seven.Lock(true); Seven.BeginFloat();
            _helmetForced = true;
            bool s2 = false;
            if (SaucerBeam != null) yield return SaucerBeam.Lower(Seven.transform, 3.4f, Vector3.forward, t => { if (!s2 && t > 0.5f) { s2 = true; StartCoroutine(Shot("e02_beam_bike_and_7_helmet")); } });
            Seven.EndFloat(); Seven.Place(EuropaLayout.SevenDown, Vector3.forward); Seven.Lock(true);
            if (SaucerBeam != null) StartCoroutine(SaucerBeam.Open(false));
            // insert: the helmet seal check - a soft tone
            var head = Seven.Bone(HumanBodyBones.Head);
            if (head != null) Shoot(head.position + Seven.transform.forward * 0.85f + Seven.transform.right * 0.35f, head.position, 30f, 0.01f);
            yield return Wait(0.6f);
            AudioKit.Play("europa_seal_tone", null, 0.6f);
            if (Helmet != null) Helmet.Pulse();
            yield return Shot("e03_insert_helmet_seal");
            yield return Wait(0.6f);
            // she walks to the bike; insert: her hands settle into the fairing's recessed grips
            yield return WalkSeven(EuropaLayout.BikeDown - Bike.transform.right * 1.05f, 6f);
            Seven.Lock(true);
            Bike.Mount(); FollowCamera();
            { var g = Bike.GripL.position; Shoot(g - Bike.transform.right * 0.9f + Vector3.up * 0.35f + Bike.transform.forward * 0.5f, g, 32f, 0.01f); }
            yield return Wait(0.8f);
            yield return Shot("e04_insert_hands_in_the_grips");
            Shoot(Bike.transform.position + Bike.transform.forward * 4.5f + Bike.transform.right * 2.5f + Vector3.up * 1.2f, Bike.transform.position + Vector3.up * 1f, 46f, 0.6f);
            yield return Say("EUR_02");
            Complete("EUR_02");
            yield return Cutscene(false);
            // gameplay: a gentle passage down through the ice into the water - teach steering, ascent, descent, braking
            Objective("Dive through the passage in the ice");
            StartCoroutine(TeachSwim());
            MusicSystem.Play("europa_underwater", 4f);
            yield return RideRoute(EuropaLayout.Dive, "the ice passage", 6f);
            Teach();
            Log($"dive done: bike at {Bike.transform.position} (under water: {Bike.transform.position.y < EuropaLayout.WaterY})");
        }
        IEnumerator GlideBike(Vector3 to, float seconds)
        {
            var p0 = Bike.transform.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds) { Bike.transform.position = Vector3.Lerp(p0, to, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            Bike.Place(to, 0f);
        }
        IEnumerator TeachSwim()
        {
            Teach((ControlsLegend.Btn.Move, "Steer"), (ControlsLegend.Btn.Tk, "Forward"));
            yield return Wait(6f); if (Auto) yield return Shot("t_swim_steer");
            Teach((ControlsLegend.Btn.Jump, "Ascend"), (ControlsLegend.Btn.Crouch, "Descend"));
            yield return Wait(6f); if (Auto) yield return Shot("t_swim_up_down");
            Teach((ControlsLegend.Btn.Block, "Brake"));
            yield return Wait(5f);
            Teach();
        }

        // ------------------------------------------------------------------------------------------ 3: first descent - wonder
        IEnumerator Descent()
        {
            Objective("Explore beneath the ice");
            if (HugeOne != null) { HugeOne.gameObject.SetActive(true); HugeOne.transform.position = EuropaLayout.HugeOnePath0; HugeOne.Loop(new[] { EuropaLayout.HugeOnePath1, EuropaLayout.HugeOnePath0 }, 5.5f); }
            // uninterrupted riding; the lines come as she goes (at or past their place)
            if (Auto) StartCoroutine(AutoSwim(EuropaLayout.Descent, 5f));
            ShowGuide(EuropaLayout.Descent[0]);
            yield return Until(() => Bike.transform.position.z > EuropaLayout.Descent[0].z - 4f, "into the great chamber", 60f, "Ride on into the great chamber", EuropaLayout.Descent[0], () => PlaceRiding(EuropaLayout.Descent[0]));
            HideGuide();
            Shoot(Bike.transform.position + new Vector3(-6f, 3f, -10f), Bike.transform.position + new Vector3(10f, -6f, 40f), 58f, 1.6f);
            yield return Wait(1.6f);
            yield return Shot("e05_wide_below_the_ice");
            FollowCamera();
            yield return Say("EUR_03", 1, 4);                   // Oh... / What is it? / They're beneath me. / Who?
            // the huge Cetacean passes beneath the bike: its scale reads as it continues beyond the frame
            if (HugeOne != null)
            {
                var b = Bike.transform.position;
                HugeOne.transform.position = b + Bike.transform.right * -60f + Vector3.down * 16f + Bike.transform.forward * 8f;
                HugeOne.transform.rotation = Quaternion.LookRotation(Bike.transform.right);
                HugeOne.GlideTo(b + Bike.transform.right * 70f + Vector3.down * 15f + Bike.transform.forward * 10f, 6.5f);
                Shoot(b + Bike.transform.forward * -6f + Vector3.up * 3.5f + Bike.transform.right * 4f, b + Vector3.down * 14f + Bike.transform.forward * 8f, 60f, 1.0f);
                HugeOne.Call("europa_call_deep", 0.9f, 500f);
                yield return Wait(4.5f);
                yield return Shot("e06_the_huge_one_beneath_the_bike");
                Log($"the huge Cetacean passes beneath the bike: {Vector3.Distance(HugeOne.transform.position, Bike.transform.position):0} m away, length 26 m vs the 3 m bike");
            }
            yield return Say("EUR_03", 5, 5);                   // I think one of the people we came to meet.
            if (HugeOne != null) HugeOne.Call("europa_call_deep", 1f, 600f);
            yield return Wait(1.5f);
            StartCoroutine(StopBike(1.2f));
            yield return Say("EUR_03", 6, 9);                   // Did you hear that? / ... / Tell me.
            if (HugeOne != null) HugeOne.Loop(new[] { EuropaLayout.HugeOnePath1 + Vector3.down * 20f, EuropaLayout.HugeOnePath0 + Vector3.down * 20f }, 4f);
            FollowCamera();
            Complete("EUR_03");
            // let the next stretch remain quiet
            Log("descent: a quiet stretch after 'Tell me.' (no lines)");
            yield return RideRoute(new[] { EuropaLayout.Descent[2], EuropaLayout.Descent[3] }, "the quiet stretch", 8f);
        }

        // ------------------------------------------------------------------------------------------ 4-5: the settlement, the keeper, the race
        IEnumerator Settlement()
        {
            Objective("Follow the guide to the settlement");
            // the guide accompanies the bike (chirps, swims alongside) toward the inhabited structures
            if (Guide != null)
            {
                Guide.transform.position = Bike.transform.position + Bike.transform.forward * 12f + Bike.transform.right * 4f;
                Guide.Follow(Bike.transform, new Vector3(3.2f, 0.6f, 4f), 9f);
                Guide.Call("europa_dolphin_chirp", 0.8f, 60f);
                StartCoroutine(GuideChirps());
            }
            yield return Wait(0.5f);
            if (Shots) { Shoot(Bike.transform.position + Bike.transform.right * 7f + Vector3.up * 1.5f + Bike.transform.forward * 4f, Bike.transform.position + Bike.transform.forward * 2f, 50f, 0.6f); yield return Wait(0.8f); yield return Shot("e07_the_guide_alongside"); FollowCamera(); }
            yield return RideRoute(EuropaLayout.ToSettlement, "to the settlement", 8f);
            yield return RideRoute(new[] { EuropaLayout.Receiving }, "the receiving platform", 5f);
            yield return StopBike();
            // the meeting space: she stops at the receiving platform (helmet on), the guide turns beside her, the keeper
            yield return Cutscene(true);
            if (Guide != null) { Guide.GlideTo(EuropaLayout.Receiving + new Vector3(3f, 1f, 2.5f), 3f); }
            if (Keeper != null) Keeper.GlideTo(EuropaLayout.KeeperAt, 3f);
            var mid = (Bike.transform.position + EuropaLayout.KeeperAt) * 0.5f;
            Shoot(mid + new Vector3(-22f, 6f, -14f), mid + Vector3.up * 2f, 54f, 0.01f);
            yield return Wait(1.2f);
            yield return Shot("e08_wide_meeting_space_keeper");
            yield return Say("EUR_04", 1, 5);
            KeeperOnRecords();
            yield return Say("EUR_04", 6, 6);
            // a distant mechanical pulse interrupts the water; the keeper becomes still
            AudioKit.Play("europa_extraction_pulse", null, 0.9f);
            CameraShake.Quake(0.6f, 0.4f);
            if (Keeper != null) Keeper.HoldHere();
            yield return Wait(1.0f);
            yield return Say("EUR_04", 7, 8);
            Complete("EUR_04");
            yield return Cutscene(false);
            // section 5: before the mission advances - explore the settlement, or the optional race
            yield return ExploreAndRace();
            Complete("EUR_05");
        }
        void KeeperOnRecords()
        {
            // the keeper's attention shifts to the geometric records she carries (a slow turn of the great head toward her)
            if (Keeper != null) Keeper.GlideTo(EuropaLayout.KeeperAt + Flat(Bike.transform.position - EuropaLayout.KeeperAt).normalized * 3f, 1.5f);
            Shoot(Bike.transform.position + Vector3.up * 1.4f - Flat(EuropaLayout.KeeperAt - Bike.transform.position).normalized * 2.2f + Bike.transform.right * 0.8f, EuropaLayout.KeeperAt + Vector3.up * 2f, 50f, 1f);
        }
        IEnumerator GuideChirps() { for (int i = 0; i < 6 && Guide != null; i++) { yield return Wait(UnityEngine.Random.Range(5f, 9f)); Guide.Call("europa_dolphin_chirp", 0.6f, 60f); } }

        bool _raceDone;
        IEnumerator ExploreAndRace()
        {
            Objective("Explore the settlement - the race arch is open - then return to the keeper");
            UI.Hint("Optional: ride through the registration arch to race. Return to the keeper (X) when you are ready.", 7f);
            bool toKeeper = false, raceNow = false;
            float since = Time.time;
            ShowGuide(EuropaLayout.KeeperAt + new Vector3(-6f, -2f, -6f));
            IEnumerator Watch()
            {
                while (!toKeeper)
                {
                    bool nearK = Vector3.Distance(Bike.transform.position, EuropaLayout.KeeperAt) < 12f && Time.time - since > 3f;
                    SetPrompt(nearK ? "Talk to the keeper" : null);
                    if (nearK && (Input.Interact.WasPressedThisFrame())) toKeeper = true;
                    if (!_raceDone && RaceRegistration != null && Vector3.Distance(Bike.transform.position, EuropaLayout.RaceRegistration) < 6f) raceNow = true;
                    yield return null;
                }
                SetPrompt(null);
            }
            StartCoroutine(Watch());
            if (Auto && DemoBoot.EuropaRace) { StartCoroutine(AutoSwim(new[] { EuropaLayout.RaceRegistration }, 3f)); }
            else if (Auto) toKeeper = true;
            while (!toKeeper)
            {
                if (raceNow && !_raceDone) { raceNow = false; yield return Race(); if (Auto) toKeeper = true; }
                if (!Auto && Time.time - since > SkipAfter * 2f) { Log("explore: long enough - the keeper calls her back"); toKeeper = true; }
                yield return null;
            }
            HideGuide();
            Log($"settlement: back to the keeper (race {(EuropaState.RaceEntered ? "entered, best " + EuropaState.RaceBest : "not entered - optional")})");
        }

        // ---- the optional race (section 5): rings through carved arches, under ice shelves, round the pillars; three rivals
        IEnumerator Race()
        {
            _raceDone = true;
            yield return StopBike(1f);
            yield return Cutscene(true);
            Shoot(EuropaLayout.RaceRegistration + new Vector3(8f, 3f, -6f), EuropaLayout.RaceRegistration + Vector3.up * 1f, 48f, 0.01f);
            yield return Say("EUR_05R");
            yield return Shot("e09_race_registration");
            yield return Cutscene(false);
            Objective("Race: through the rings");
            var start = EuropaLayout.RaceRegistration;
            var rp = new Vector3[Rivals != null ? Rivals.Length : 0]; var rk = new int[rp.Length]; var rs = new float[] { 7.4f, 8.1f, 8.8f };
            for (int i = 0; i < rp.Length; i++) { rp[i] = start + new Vector3(2f + i * 2f, 0.5f * i, -2f); if (Rivals[i] != null) Rivals[i].gameObject.SetActive(true); }
            int gate = 0; float t0 = Time.time; bool overtook = false; int behind0 = -1;
            if (Auto) StartCoroutine(AutoSwim(EuropaLayout.RaceGates, 5f));
            UI.Hint("3 . 2 . 1", 1.5f); yield return Wait(1.5f);
            while (gate < EuropaLayout.RaceGates.Length && Time.time - t0 < 150f)
            {
                var g = EuropaLayout.RaceGates[gate];
                ShowGuide(g);
                if (Vector3.Distance(Bike.transform.position, g) < 6f) { gate++; AudioKit.Play("energy_point", g, 0.5f); continue; }
                // the rivals fly the same line at their own pace
                int ahead = 0;
                for (int i = 0; i < rp.Length; i++)
                {
                    if (Rivals[i] == null || rk[i] >= EuropaLayout.RaceGates.Length) { if (rk[i] >= EuropaLayout.RaceGates.Length) ahead++; continue; }
                    var tg = EuropaLayout.RaceGates[rk[i]]; var d = tg - rp[i];
                    if (d.magnitude < 3f) rk[i]++;
                    rp[i] += d.normalized * Mathf.Min(d.magnitude, rs[i % rs.Length] * Time.deltaTime);
                    Rivals[i].position = rp[i]; if (d.sqrMagnitude > 0.01f) Rivals[i].rotation = Quaternion.LookRotation(d);
                    if (rk[i] > gate || (rk[i] == gate && d.magnitude < Vector3.Distance(Bike.transform.position, tg))) ahead++;
                }
                if (behind0 < 0) behind0 = ahead;
                if (!overtook && behind0 > 0 && ahead < behind0 && gate >= 2) { overtook = true; StartCoroutine(Say("EUR_05O")); StartCoroutine(Shot("e10_race_clean_overtake")); }
                if (gate == 3 && Shots && !_raceShot) { _raceShot = true; StartCoroutine(Shot("e10b_race_round_the_pillars")); }
                yield return null;
            }
            HideGuide();
            int place = 1; for (int i = 0; i < rk.Length; i++) if (rk[i] >= EuropaLayout.RaceGates.Length) place++;
            if (gate < EuropaLayout.RaceGates.Length) place = rp.Length + 1;
            EuropaState.RaceResult(place);
            Log($"race (optional): finished place {place} of {rp.Length + 1} in {Time.time - t0:0.0}s, gates {gate}/{EuropaLayout.RaceGates.Length}, overtake {(overtook ? "yes" : "no")} (no history reward - optional)");
            yield return StopBike(1.5f);
            yield return Say(place == 1 ? "EUR_05W" : "EUR_05L");
            AudioKit.Play("europa_extraction_pulse", null, 0.8f);                // a distant extraction pulse again; the mood settles
            yield return Wait(1.2f);
            for (int i = 0; i < rp.Length; i++) if (Rivals[i] != null) Rivals[i].gameObject.SetActive(false);
            Objective("Return to the keeper");
        }
        bool _raceShot;
    }
}
