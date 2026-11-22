using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// Round 11 / QF10 — Chapter 2 "Venus", part 1 (Docs/CHAPTER2_VENUS.md): the descent under the clouds, C01 arrival,
    /// B01 the bike (out of a seamless hull hatch) + the cat's name (Panther) and first size change, B02 the first ride,
    /// B03 / C02 the damaged route and the tremor, then the collapse crossed on the bike over a fallen slab she lays across
    /// with telekinesis (QF10), B04 parking at the temple, C03 the guardian, C04 the cube and the interruption, B05 the
    /// ride to the lower gardens, C05 the signal - to VEN_S04.
    /// Reuses Chapter 1's systems: 7 (walk, traversal, prompts), the follow camera, the dialogue player + subtitles +
    /// telepathy treatment (lines and labels from Resources/Venus/venus_dialogue.json), the pause menu, the capture.
    /// Progression + persistence: <see cref="VenusState"/>. Every scene applies its result when it completes, watched or
    /// skipped (skipping = the same result, once); an interrupted scene restarts from its beginning; reloading restores
    /// the place for the stage reached. Triggers fire at or past their place; a watchdog re-shows the objective.
    /// QF10 player fallback (as Chapter 1's QF6): every wait on the player shows a hint and briefly lights the next step
    /// after ~45 s without progress, and after ~2 min offers to skip the step (a white fade; the step is done).
    /// QF10 camera coverage in C03-C05: wide, over-the-shoulder both ways (low on the guardian, level on her), medium
    /// two-shots, inserts, the attendant's arrival, cut on speaker change - always framed on who speaks or reacts.
    /// </summary>
    public partial class VenusDirector : MonoBehaviour
    {
        public ChapterSeven Seven;
        public ChapterCat Cat;
        public ChapterCamera Cam;
        public ChapterInput Input;
        public DialoguePlayer Dlg;
        public ChapterUI UI;
        public ControlsLegend Legend;
        public HoverBike Bike;
        public SaucerBeam SaucerBeam;
        public GameObject BridgeRamps;
        public Transform WalkwayIntact;
        public ParticleSystem TremorStones;
        public Transform Saucer, Hatch, Vessel, VesselWater, Cube, CubeCage;
        public ElarimNpc Guardian, Attendant;
        public ElarimNpc[] Workers;
        public WormCreature[] Worms;
        public ParticleSystem CeilingDust;
        public Transform[] Flyers;
        public ElarimShape Shape;
        public GameObject MaintGate;          // blocks the maintenance path until C04
        public Atmosphere Atmo;
        // QF10
        public Transform Slab;                // the fallen slab (telekinesis bridge)
        public ParticleSystem SlabDust, TkMotes;
        public Renderer HatchSeam, BayBack;
        public Light BayLight;
        /// <summary>QF13 (1): the saucer prefab's bike hold (open / close; the same on every world).</summary>
        public SaucerHold Hold;

        public static VenusDirector I { get; private set; }
        bool Auto => DemoBoot.CaptureDir != null && !DemoBoot.PlayerSim;
        bool Shots => DemoBoot.CaptureDir != null && Cap != null;
        ChapterCapture Cap => ChapterCapture.I;
        string _objective, _hintLine;
        bool _cut; public bool InCutscene => _cut;
        CarriedShape _carry;
        bool _firstFrameCovered;
        static readonly HashSet<string> _taught = new HashSet<string>();
        public static int Reloads;        // check run: reloads done (each committed scene once)
        static bool _playReset;
        public string ObjectiveNow => _objective;

        // ------------------------------------------------------------------------------------------ setup
        void Start()
        {
            I = this;
            // QF12 (9): arriving on Venus, the very first frame is already black (the title card fades in over it) and 7
            // is not there yet - she appears only when the arrival scene brings her out of the saucer
            if (!VenusState.SceneDone("VEN_C01") && !DemoBoot.VenusSelectTest)
            {
                UI.SetFade(1f);
                if (Seven != null) Seven.gameObject.SetActive(false);
                if (Cat != null) Cat.gameObject.SetActive(false);
                _firstFrameCovered = true;
            }
            VenusLayout.UseTraversal();
            Legend.SetLayout(ControlsLegend.Layout.Chapter);
            Legend.HidePanels = true;
            if (Auto && Reloads == 0 && !DemoBoot.Continue && !VenusState.SceneSelectRun) VenusState.ResetForTest();
            if (DemoBoot.VenusPlay && !_playReset && !DemoBoot.Continue && !VenusState.SceneSelectRun) { _playReset = true; VenusState.ResetForTest(); }
            if (DemoBoot.CaptureDir != null) { ChapterCapture.Ensure(); Cap.Attach(Cam.GetComponent<Camera>()); }
            if (DemoBoot.CaptureDir != null && _firstFrameCovered) StartCoroutine(FirstFrames());
            if (Auto)
            {
                Seven.ScriptDriven = true; Dlg.AutoSkipAfter = DemoBoot.VenusSkip ? 0f : 0.9f;
                if (Reloads == 0) Cap.Log($"Venus check run{(DemoBoot.VenusSkip ? " (every line skipped)" : "")}: {VenusState.Summary()}");
            }
            ChapterCamera.CatRoot = Cat != null ? Cat.transform : null;
            CameraOccluders.Build(Seven.transform, Cat != null ? Cat.transform : null, Bike != null ? Bike.transform : null, Shape != null ? Shape.transform : null);
            ChapterCamera.RideIgnore = new[] { Seven.transform, Cat != null ? Cat.transform : null, Bike != null ? Bike.transform : null, Shape != null ? Shape.transform : null };   // QF15 (1)
            ChapterCamera.RideArea = () => "Venus";
            if (Shape != null) { _carry = CarriedShape.Attach(Seven.gameObject, Shape.transform); _carry.Set(CarriedShape.Where.Pocket, 0f); }
            if (Cat != null) Cat.LeashAllowed = () => false;            // the cat stays at the ship on Venus
            Bike.Rider = Seven; Bike.Input = Input;
            if (Slab != null) _slabEdges = Slab.Find("Edges")?.GetComponent<Renderer>();
            if (HatchSeam != null) HatchSeam.enabled = false;            // QF10: closed, the hull is seamless (no line at all)
            MusicSystem.Play("venus_gardens", 3f);
            _ambGardens = AudioKit.Loop("amb_venus_gardens", null, null, 1f, AudioKit.Bus.Ambience);
            _wild = gameObject.AddComponent<VenusWildlife>();          // Round 13 (0i): birds, insects, animals
            if (DemoBoot.VenusSelectTest) { StartCoroutine(SelectTestStep()); StartCoroutine(UndergroundLook()); return; }
            StartCoroutine(Flow());
            StartCoroutine(Watchdog());
            StartCoroutine(UndergroundLook());                         // Round 12: below ground - the underground music, cave light
            if (VenusState.AtLeast(VenusState.Stage.ControlDiscovered) && !VenusState.AtLeast(VenusState.Stage.ThreatStopped)) StartCoroutine(AmbientPulses());
        }

        /// <summary>QF12 (9): the first frames of the chapter - black, 7 not there (captured, logged).</summary>
        IEnumerator FirstFrames()
        {
            for (int f = 1; f <= 3; f++)
            {
                bool seen = Seven != null && Seven.gameObject.activeInHierarchy;
                Cap.Log($"first frame {f} (QF12 9): fade {UI.FadeAlpha:0.00} (1 = black), 7 {(seen ? "IN THE SCENE" : "not there")}");
                if (f == 1) yield return Cap.Shot("v00_first_frame"); else yield return null;
            }
        }

        // ------------------------------------------------------------------------------------------ helpers
        IEnumerator Wait(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }
        AudioSource _ambGardens;
        VenusWildlife _wild;
        /// <summary>QF11 check (-venusselecttest): each Scene Select entry in turn - where 7, the bike and Panther are, a capture.</summary>
        IEnumerator SelectTestStep()
        {
            Restore();
            yield return Wait(2.5f);
            var id = DemoBoot.VenusSelect;
            Log($"SCENE SELECT '{id}': {VenusState.Summary()}; 7 at {P7:0.0} ({(Bike.Riding ? "riding" : "on foot")}), bike {(Bike.gameObject.activeSelf ? Bike.transform.position.ToString("0.0") : "in the bay")}, Panther {(Cat != null && Cat.gameObject.activeSelf ? $"x{Cat.transform.localScale.x:0.0} at {Cat.transform.position:0.0}" : "aboard (not shown)")}, bridge {(VenusState.SceneDone("VEN_B03_bridge") ? "laid" : "not laid")}, objective '{_objective}'");
            yield return Shot("sel_" + id);
            int k = System.Array.FindIndex(VenusState.SelectEntries, e => e.id == id);
            if (k < 0 || k + 1 >= VenusState.SelectEntries.Length) { Cap.Finish("SCENE SELECT TEST (Venus) done"); yield break; }
            DemoBoot.VenusSelect = VenusState.SelectEntries[k + 1].id;
            VenusState.StartAt(DemoBoot.VenusSelect);
        }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        float Dist7(Vector3 p) => Flat(Seven.transform.position - p).magnitude;
        Vector3 P7 => Seven.transform.position;
        void Objective(string s) { _objective = s; _hintLine = null; UI.SetObjective(s); if (DemoBoot.CaptureDir != null) Cap?.Log("objective: " + s); }
        void Log(string s) { Debug.Log("[Seven] " + s); if (DemoBoot.CaptureDir != null) Cap?.Log(s); }
        IEnumerator Shot(string name) { if (!Shots) yield break; ShotChecks(name); yield return Cap.Shot(name); }

        // ------------------------------------------------------------------------------------------ QF10: the player fallback
        public float HintAfter => DemoBoot.VenusHintFast ? 12f : 45f;
        public float SkipAfter => DemoBoot.VenusHintFast ? 30f : 120f;
        /// <summary>The step a player is on right now (for the hint / skip / the virtual-pad check run).</summary>
        public string WaitingFor { get; private set; }
        public bool SkipOffered { get; private set; }
        public int Hints { get; private set; }
        public int Skips { get; private set; }
        bool _skipPressed;
        Light _beacon;

        /// <summary>Wait for a story condition. Check runs (autopilot): a soft-lock log after autoTimeout. A player: no progress
        /// (not closer to the step) for <see cref="HintAfter"/> s -> the objective comes back with a hint and the step is lit
        /// briefly; after <see cref="SkipAfter"/> s a skip is offered (Y / Enter): white fade, the step's skip action, done.
        /// No wait can only be escaped by the autopilot.</summary>
        IEnumerator Until(Func<bool> cond, string what, float autoTimeout = 90f, string hint = null, Vector3? at = null, Action skip = null)
        {
            float t0 = Time.time, since = Time.time, best = at.HasValue ? Dist7(at.Value) : 0f;
            bool hinted = false;
            WaitingFor = what; SkipOffered = false; _skipPressed = false;
            while (!cond())
            {
                if (Auto && Time.time - t0 > autoTimeout) { Cap.Soft++; Cap.Log($"SOFT-LOCK? waiting for '{what}' {autoTimeout:0}s (7 at {P7}, {(Bike.Riding ? "riding" : "on foot")})"); break; }
                bool busy = _cut || PauseMenu.IsOpen || Dlg.Busy;
                if (busy) { since += Time.deltaTime; yield return null; continue; }
                if (at.HasValue) { float d = Dist7(at.Value); if (d < best - 2f) { best = d; since = Time.time; } }
                if (!Auto)
                {
                    if (!hinted && Time.time - since > HintAfter && hint != null)
                    {
                        hinted = true; Hints++;
                        _hintLine = hint; UI.SetObjective(ObjectiveText); UI.Hint(hint, 8f);
                        if (at.HasValue) StartCoroutine(Beacon(at.Value, 7f));
                        Log($"HINT ({Time.time - since:0}s without progress): '{what}' -> \"{hint}\"");
                        StartCoroutine(Shot("hint_" + Slug(what)));
                    }
                    if (!SkipOffered && skip != null && Time.time - since > SkipAfter)
                    {
                        SkipOffered = true;
                        Log($"SKIP OFFERED ({Time.time - since:0}s without progress): '{what}'");
                        StartCoroutine(Shot("skip_offered_" + Slug(what)));
                    }
                    if (SkipOffered && (_skipPressed || SkipButton()))
                    {
                        _skipPressed = false; Skips++;
                        Log($"SKIP taken: '{what}'");
                        yield return UI.Fade(1f, 0.5f, 1f);
                        try { skip(); } catch (Exception e) { Log("skip action failed: " + e.Message); }
                        yield return null; yield return null;
                        StartCoroutine(Shot("skip_done_" + Slug(what)));
                        yield return UI.Fade(0f, 0.7f, 1f);
                        break;
                    }
                }
                yield return null;
            }
            WaitingFor = null; SkipOffered = false; _hintLine = null;
            if (_objective != null) UI.SetObjective(_objective);
            if (Auto || DemoBoot.CaptureDir != null) Cap?.Log($"ok: {what} ({Time.time - t0:0.0}s)");
        }
        string ObjectiveText => _hintLine != null ? _objective + "\n" + _hintLine : _objective;
        static string Slug(string s) { var b = new System.Text.StringBuilder(); foreach (var c in s.ToLowerInvariant()) b.Append(char.IsLetterOrDigit(c) ? c : '_'); var r = b.ToString(); while (r.Contains("__")) r = r.Replace("__", "_"); return r.Length > 28 ? r.Substring(0, 28) : r.Trim('_'); }
        bool SkipButton()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current; var gp = UnityEngine.InputSystem.Gamepad.current;
            return (kb != null && kb.enterKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame);
        }

        /// <summary>Briefly light the next step: a soft white light pulsing over the spot.</summary>
        IEnumerator Beacon(Vector3 at, float seconds)
        {
            if (_beacon == null) { var go = new GameObject("StepBeacon"); _beacon = go.AddComponent<Light>(); _beacon.type = LightType.Point; _beacon.color = Color.white; _beacon.shadows = LightShadows.None; _beacon.range = 7f; }
            _beacon.transform.position = at + Vector3.up * 1.6f; _beacon.enabled = true;
            for (float t = 0f; t < seconds; t += Time.deltaTime) { _beacon.intensity = 1.6f + 1.4f * Mathf.Sin(t * 4f); if (_slabEdges != null && Flat(at - Slab.position).magnitude < 3f) Glow(_slabEdges, 1.5f + Mathf.Sin(t * 4f)); yield return null; }
            _beacon.enabled = false;
        }

        IEnumerator Cutscene(bool on)
        {
            _cut = on;
            if (on) { if (!Bike.Riding) Seven.Lock(true); }
            else { if (!Bike.Riding) Seven.Lock(false); FollowCamera(); }
            yield return null;
        }

        void FollowCamera()
        {
            if (Bike.Riding) { Cam.Target = Bike.transform; Cam.Distance = 7.4f; Cam.LookHeight = 1.35f; Cam.AutoBehind = true; }   // QF13 (13): a bit further back and lower at the new speeds
            else { Cam.Target = Seven.transform; Cam.Distance = 3.8f; Cam.LookHeight = 1.45f; Cam.AutoBehind = Auto; }
            Cam.Follow();
        }

        /// <summary>Lines of one scene as data (labels, telepathy, optional voice files; subtitles only without them).</summary>
        List<VoiceBank.Line> Lines(string scene, int from = 1, int to = 99)
        {
            var l = new List<VoiceBank.Line>();
            foreach (var x in VenusDialogue.SceneLines(scene))
            {
                int n = VenusDialogue.LineNumber(x.id);
                if (n < from || n > to) continue;
                l.Add(new VoiceBank.Line
                {
                    Id = x.id, Speaker = x.speaker == "cat" ? "cat" : x.speaker, Text = x.text, Scene = 200, PauseAfter = 0.45f,
                    Clip = Resources.Load<AudioClip>("VenusVoice/" + x.id), Label = x.label,
                });
            }
            return l;
        }

        /// <summary>Play lines; the camera cue on a line (if any) runs first - presentation only, never waited on. In the
        /// covered scenes (C03 / C04 / B05 lines) every line is framed on its speaker (QF10).</summary>
        IEnumerator Say(string scene, int from = 1, int to = 99, bool cues = true)
        {
            var ls = Lines(scene, from, to);
            foreach (var line in ls)
            {
                _lineSerial++;
                var cue = cues ? VenusDialogue.Get(line.Id)?.cue : null;
                bool cued = false;
                if (!string.IsNullOrEmpty(cue)) try { Cue(cue); cued = true; } catch (Exception e) { Log($"camera cue '{cue}' skipped ({e.Message})"); }
                if (_coverage)
                {
                    if (cued && CueShowsSpeaker(cue, line.Speaker)) _lastSpeaker = line.Speaker;      // the cue framed the speaker: the next speaker cuts
                    if (cued && !CueShowsSpeaker(cue, line.Speaker)) StartCoroutine(CoverAfter(line, Auto ? 0.4f : 1.5f, _lineSerial));
                    else if (!cued) Cover(line);
                    StartCoroutine(LineShot(line, _lineSerial));
                }
                yield return Dlg.PlayLines(new List<VoiceBank.Line> { line }, false);
            }
        }
        int _lineSerial;

        void Shoot(Vector3 pos, Vector3 look, float fov = 50f, float ease = 1.0f) { Cam.Subjects.Clear(); Cam.Shot(pos, look, ease, ease <= 0.05f, fov); }

        void Cue(string cue)
        {
            var h = P7 + Vector3.up * 1.5f; var f = Flat(Seven.transform.forward).normalized; var r = Vector3.Cross(Vector3.up, f);
            switch (cue)
            {
                case "over 7's shoulder toward the gardens": Shoot(h - f * 1.6f + r * 0.75f + Vector3.up * 0.1f, h + f * 30f + Vector3.down * 4f, 48f); break;
                case "medium two-shot of 7 and Cat": if (Cat != null) Shoot((P7 + Cat.transform.position) * 0.5f + f * 3.4f + r * 1.2f + Vector3.up * 1.3f, (P7 + Cat.transform.position) * 0.5f + Vector3.up * 0.8f, 46f); break;
                case "landscape insert": Shoot(P7 + f * 2f + Vector3.up * 3.5f, VenusLayout.GuardianSpot + Vector3.up * 30f, 55f, 2f); break;
                case "cat head tracking flying creatures": if (Cat != null) Shoot(Cat.transform.position + r * 1.4f + f * 1.2f + Vector3.up * 0.5f, Cat.transform.position + Vector3.up * 0.35f, 40f); break;
                case "wide two-shot at temple entrance": if (Guardian != null) TwoShot(Guardian, 1f, 7.5f, 0.01f); else Shoot(VenusLayout.MeetSpot + new Vector3(7f, 2.4f, -5f), VenusLayout.MeetSpot + new Vector3(0f, 1.8f, 2f), 50f); break;
                case "insert of dodecahedron": { var at = _carry != null ? _carry.Palm : P7 + Vector3.up * 1.1f + f * 0.4f; Shoot(at + r * 0.7f + Vector3.up * 0.2f - f * 0.1f, at, 34f, 0.01f); break; }
                case "over 7's shoulder toward guardian": if (Guardian != null) OtsOnto(Guardian, 1f); break;
                case "close view of object between them": { var at = _carry != null ? _carry.Palm : P7 + Vector3.up * 1.1f + f * 0.4f; Shoot(at + r * 1.1f + Vector3.up * 0.3f, at + f * 0.4f, 38f, 0.01f); break; }
                case "over 7's shoulder toward cube": Shoot(h - Flat(VenusLayout.CubeAt - P7).normalized * 1.3f + Vector3.Cross(Vector3.up, Flat(VenusLayout.CubeAt - P7).normalized) * 0.6f - Vector3.up * 0.1f, VenusLayout.CubeAt, 44f, 0.01f); break;
                case "matching inserts of related markings": Shoot(VenusLayout.CubeAt + new Vector3(0.9f, 0.3f, -1.2f), VenusLayout.CubeAt, 30f, 0.01f); break;
                case "brief eye close-up": { var hd = Head(SevenAnim) ?? h; Shoot(hd + f * 0.9f + r * 0.25f + Vector3.up * 0.02f, hd + Vector3.up * 0.04f, 30f, 0.01f); break; }
                case "falling dust insert": Shoot(VenusLayout.CubeAt + new Vector3(-3f, 2.5f, -4f), VenusLayout.CubeAt + Vector3.up * 3f, 50f, 0.01f); break;
                case "wide - saucer and landscape": Shoot(VenusLayout.SaucerAt + new Vector3(9f, 4f, 12f), VenusLayout.SaucerAt + new Vector3(0f, 1.5f, 3f), 55f); break;
                case "low three-quarter on the bike": Shoot(Bike.transform.position + Bike.transform.forward * 3.4f + Bike.transform.right * 2.0f + Vector3.up * 0.5f, Bike.transform.position + Vector3.up * 0.7f, 44f); break;
                case "side medium - 7 mounting": Shoot(Bike.transform.position + Bike.transform.right * 4.0f + Vector3.up * 1.3f, Bike.transform.position + Vector3.up * 0.9f, 46f); break;
                default: throw new Exception("unknown cue");
            }
        }

        void Complete(string scene, VenusState.Stage? result = null)
        {
            bool first = VenusState.CompleteScene(scene, result);
            Log($"scene {scene} complete{(first ? "" : " (already done - no second result)")}; {VenusState.CurrentId}");
        }

        /// <summary>Check run (-venusreload): reload the scene once after each committed scene and continue from the
        /// save (proves the restore for every stage).</summary>
        bool ReloadCheck(string after)
        {
            if (!Auto || !DemoBoot.VenusReload || VenusState.SceneDone("reload_" + after)) return false;
            VenusState.CompleteScene("reload_" + after);
            Reloads++;
            Cap.Log($"RELOAD after {after}: {VenusState.Summary()}");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return true;
        }

        // ------------------------------------------------------------------------------------------ flow
        IEnumerator Flow()
        {
            if (!VenusState.Reached) VenusState.Advance(VenusState.Stage.Available);
            Restore();
            yield return null;
            if (Auto && DemoBoot.R13 == "bridge") { yield return BridgeTest(); yield break; }        // Round 13 (0d) check
            if (DemoBoot.RampCheck && DemoBoot.CaptureDir != null) { UI.SetFade(0f); Bike.gameObject.SetActive(true); foreach (var r in Bike.GetComponentsInChildren<Renderer>(true)) r.enabled = true; yield return global::Seven.RampCheck.Run(Bike, Seven, Cam, "venus", VenusLayout.IsBikeZone); yield break; }   // QF17 (4)
            if (!VenusState.SceneDone("VEN_B01")) BikeHoldWatch.Begin("venus", Hold, Bike, Saucer);   // QF17 (5)
            if (!VenusState.SceneDone("VEN_C01")) { yield return Arrival(); if (ReloadCheck("VEN_C01")) yield break; }
            if (!VenusState.SceneDone("VEN_B01")) { yield return B01(); if (ReloadCheck("VEN_B01")) yield break; }
            if (!VenusState.SceneDone("VEN_C03")) { yield return RideToTemple(); yield return C03(); if (ReloadCheck("VEN_C03")) yield break; }
            if (!VenusState.SceneDone("VEN_C04")) { yield return C04(); if (ReloadCheck("VEN_C04")) yield break; }
            if (!VenusState.SceneDone("VEN_C05")) { yield return RideToObservation(); if (ReloadCheck("VEN_C05")) yield break; }
            PartOneDone = true;
            if (Auto && DemoBoot.Quick2) { yield return EndOfPart1(); yield break; }
            if (!VenusState.AtLeast(VenusState.Stage.ThreatStopped)) StartCoroutine(AmbientPulses());
            yield return Part2();                                       // Round 12
        }

        /// <summary>Put everything where the save says (a reload / Continue restores the place for the stage reached).</summary>
        void Restore()
        {
            bool c01 = VenusState.SceneDone("VEN_C01"), b01 = VenusState.SceneDone("VEN_B01");
            SetZone("MaintA", VenusState.AtLeast(VenusState.Stage.HelpAccepted)); SetZone("MaintB", VenusState.AtLeast(VenusState.Stage.HelpAccepted)); SetZone("ObsArea", VenusState.AtLeast(VenusState.Stage.HelpAccepted));
            if (MaintGate != null) MaintGate.SetActive(!VenusState.AtLeast(VenusState.Stage.HelpAccepted));
            if (Saucer != null) Saucer.position = VenusLayout.SaucerAt + Vector3.up * (c01 ? 0f : 60f);
            // QF10: the bridge stays for return trips
            bool bridged = VenusState.SceneDone("VEN_B03_bridge");
            SetBridge(bridged);
            if (WalkwayIntact != null) WalkwayIntact.gameObject.SetActive(!VenusState.SceneDone("VEN_B03"));
            // QF12 (5): the slab is a piece of the walkway - it exists only once the tremor has broken the walkway (B03)
            if (Slab != null)
            {
                bool fallen = VenusState.SceneDone("VEN_B03");
                Slab.gameObject.SetActive(bridged || fallen);
                if (bridged) Slab.SetPositionAndRotation(BridgePos, Quaternion.identity); else Slab.SetPositionAndRotation(SlabRest, SlabRestRot);
            }
            if (WalkwayIntact != null) foreach (Transform c in WalkwayIntact) c.gameObject.SetActive(true);
            // the bike: in the bay until B01, then where it was left
            if (!b01) Bike.Place(VenusLayout.BikeUnpacked, 0f);
            else if (VenusState.BikeKnown) Bike.Place(VenusState.BikeAt, VenusState.BikeYaw);
            else Bike.Place(VenusLayout.BikeUnpacked, 0f);
            Bike.gameObject.SetActive(b01);
            // Panther: large and lying beside the ramp after B01; aboard (hidden) before C01
            if (Cat != null)
            {
                Cat.gameObject.SetActive(c01);
                if (b01) { Cat.transform.localScale = Vector3.one * PantherScale; Cat.Place(VenusLayout.PantherLie, Vector3.forward); Cat.Scripted("lie_down"); }
            }
            Vector3 at; Vector3 face = Vector3.forward;
            if (!c01) at = VenusLayout.StepOut;
            else if (!b01) at = VenusLayout.StepOut + Vector3.forward * 1f;
            else if (!VenusState.SceneDone("VEN_C03")) at = VenusState.BikeKnown ? VenusState.BikeAt - Vector3.right * 1.3f : VenusLayout.BikeUnpacked - Vector3.right * 1.3f;
            else if (!VenusState.SceneDone("VEN_C04")) { at = VenusLayout.MeetSpot + Vector3.forward * 4f; }
            else if (!VenusState.SceneDone("VEN_C05")) { at = VenusLayout.ParkSpot - Vector3.right * 1.7f; }
            else { at = VenusLayout.ObsSpot; face = Vector3.right; }
            RestorePart2(ref at, ref face);                              // Round 12
            Seven.Place(at, face);
            if (b01 && VenusState.Riding && !VenusState.SceneDone("VEN_C03")) { Bike.Place(VenusState.BikeAt, VenusState.BikeYaw); Bike.Mount(); }
            FollowCamera(); Cam.SnapBehind();
            if (VenusState.Objective != null && c01) Objective(VenusState.Objective);
            if (VenusState.SceneDone("VEN_C03") && Guardian != null) { Guardian.transform.position = VenusLayout.CubeView + new Vector3(1.8f, 0f, 2f); }
            Log($"restore: {VenusState.Summary()} -> 7 at {at}{(bridged ? ", the slab bridge is down" : "")}");
        }

        void SetZone(string name, bool on) { var z = Ground.FindZone(name); if (z != null) z.Enabled = on; }
        /// <summary>Round 13 (0d): the slab bridge, its two ramped ends (zones + the stone aprons) together.</summary>
        void SetBridge(bool on) { SetZone("Bridge", on); SetZone("BridgeRampS", on); SetZone("BridgeRampN", on); if (BridgeRamps != null) BridgeRamps.SetActive(on); }

        // ------------------------------------------------------------------------------------------ the descent + C01
        IEnumerator Arrival()
        {
            yield return Cutscene(true);
            Seven.gameObject.SetActive(false); if (Cat != null) Cat.gameObject.SetActive(false);
            UI.SetFade(1f);
            StartCoroutine(UI.Title("CHAPTER 2  —  VENUS", 1.0f, 2.2f, 1.0f));
            var top = VenusLayout.SaucerAt + Vector3.up * 70f; var land = VenusLayout.SaucerAt;
            Saucer.position = top;
            // extreme wide: the saucer emerging beneath the clouds, the gardens below
            Shoot(VenusLayout.SaucerAt + new Vector3(-55f, 22f, -60f), top + Vector3.down * 10f, 52f, 0.01f);
            AudioKit.Play("saucer_descent", null, 0.8f);
            yield return Wait(0.6f);
            yield return UI.Fade(0f, 1.6f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 7f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Saucer.position = Vector3.Lerp(top, land, u);
                Cam.Shot(VenusLayout.SaucerAt + new Vector3(-55f + 25f * u, 22f - 12f * u, -60f + 30f * u), Saucer.position, 0.6f, false, 52f);
                if (t > 0.45f && t - Time.deltaTime / 7f <= 0.45f) StartCoroutine(Shot("v01_descent_under_the_clouds"));
                yield return null;
            }
            Saucer.position = land;
            yield return Wait(0.6f);
            // Round 13 (0b): no ramp - the iris opens, the beam comes down, she floats down in it, then the cat
            yield return BeamDown();
            Shoot(VenusLayout.StepOut + new Vector3(3.5f, 1.6f, 4.5f), VenusLayout.StepOut + new Vector3(0f, 1.2f, -1f), 50f, 0.01f);
            if (Cat != null) Cat.GoTo(VenusLayout.StepOut + new Vector3(-1.0f, 0f, 0.4f), Cat.StrollSpeed, VenusLayout.VistaSpot + Vector3.forward * 20f);
            yield return WalkSeven(VenusLayout.StepOut + Vector3.forward * 1.2f, 7f);
            Seven.Lock(true);
            yield return Shot("v02_down_the_ramp");
            yield return Say("VEN_C01", 1, 2);
            yield return Shot("v03_c01_two_shot");
            yield return Wait(0.8f);                                          // "After the Moon..."
            yield return Say("VEN_C01", 3, 4);
            yield return Shot("v04_c01_landscape_insert");
            StartCoroutine(CatTracksFlyers(8f));
            yield return Say("VEN_C01", 5, 7);
            yield return Shot("v05_c01_cat_tracks_flyers");
            Log("after C01: " + ChapterCat.SpeedReport());
            Complete("VEN_C01", VenusState.Stage.Landed);
            yield return Cutscene(false);
        }

        /// <summary>Round 13 (0b): 7 and the cat come down from the saucer in the tractor beam (Chapter 1's beam, reversed).</summary>
        IEnumerator BeamDown(bool withCat = true)
        {
            var sb = SaucerBeam;
            var foot = VenusLayout.BeamFoot;
            Shoot(foot + new Vector3(8.5f, 1.6f, 7.5f), foot + new Vector3(0f, 2.0f, 0f), 46f, 0.01f);
            if (sb == null)
            {
                Seven.gameObject.SetActive(true); Seven.Place(VenusLayout.StepOut + Vector3.back * 2.2f, Vector3.forward); Seven.Lock(true);
                if (withCat && Cat != null) { Cat.gameObject.SetActive(true); Cat.Place(VenusLayout.StepOut + new Vector3(-1.1f, 0f, -0.9f), Vector3.forward); }
                yield break;
            }
            yield return sb.Iris(true);
            yield return sb.BeamOn(true);
            Seven.gameObject.SetActive(true); Seven.Lock(true); Seven.BeginFloat();
            bool shot = false;
            yield return sb.Lower(Seven.transform, 3.6f, Vector3.forward, t => { if (!shot && t > 0.5f) { shot = true; StartCoroutine(Shot("v02a_beam_down")); } });
            Seven.EndFloat(); Seven.Place(foot, Vector3.forward); Seven.Lock(true);
            if (withCat && Cat != null)
            {
                Cat.gameObject.SetActive(true); Cat.Floating();
                yield return sb.Lower(Cat.transform, 2.8f, Vector3.forward);
                Cat.Place(foot + new Vector3(-0.9f, 0f, -0.3f), Vector3.forward);
            }
            // she steps out of the light; it goes out, the iris closes
            yield return WalkSeven(foot + Vector3.forward * 2.4f, 4f);
            Seven.Lock(true);
            StartCoroutine(sb.Open(false));
            Log("beam (Round 13, no ramp): 7 and the cat came down in the tractor beam");
        }

        /// <summary>Round 13 (0b): back aboard by the beam - she walks into the light under the hatch and floats up.</summary>
        public IEnumerator BeamUp(bool withCat)
        {
            var sb = SaucerBeam; var foot = VenusLayout.BeamFoot;
            if (sb == null) yield break;
            Shoot(foot + new Vector3(9.5f, 2.0f, 6.5f), foot + new Vector3(0f, 2.3f, 0f), 46f, 0.01f);
            yield return sb.Iris(true);
            yield return sb.BeamOn(true);
            yield return WalkSeven(foot, 8f, 0.7f);
            Seven.Lock(true); Seven.BeginFloat();
            yield return sb.Rise(Seven.transform, 4.2f, 40f);
            if (withCat && Cat != null && Cat.gameObject.activeInHierarchy)
            {
                Cat.Scripted("walk");
                for (float t0 = Time.time; Flat(Cat.transform.position - foot).magnitude > 0.2f && Time.time - t0 < 5f;)
                { var d = Flat(foot - Cat.transform.position); Cat.transform.position += d.normalized * Mathf.Min(d.magnitude, 1.4f * Time.deltaTime); Cat.transform.rotation = Quaternion.RotateTowards(Cat.transform.rotation, Quaternion.LookRotation(d), 360f * Time.deltaTime); yield return null; }
                Cat.Floating();
                yield return sb.Rise(Cat.transform, 3.4f, -60f);
            }
            yield return sb.Open(false);
            Log("beam (Round 13, no ramp): back aboard by the tractor beam");
        }

        IEnumerator CatTracksFlyers(float seconds)
        {
            if (Cat == null || Flyers == null || Flyers.Length == 0) yield break;
            Transform head = null;
            foreach (var t in Cat.GetComponentsInChildren<Transform>()) if (t.name == "head") { head = t; break; }
            if (head == null) yield break;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                var f = Flyers[0]; var d = (f.position - head.position).normalized;
                head.rotation = Quaternion.Slerp(head.rotation, Quaternion.LookRotation(d), 0.35f);
                yield return new WaitForEndOfFrame();
            }
        }

        IEnumerator WalkSeven(Vector3 to, float timeout, float speed = 0.8f)
        {
            bool sd = Seven.ScriptDriven; Seven.ScriptDriven = true; Seven.Lock(false);
            for (float t0 = Time.time; Dist7(to) > 0.3f && Time.time - t0 < timeout;) { Seven.ScriptMove = Flat(to - P7).normalized * speed; yield return null; }
            Seven.ScriptMove = Vector3.zero;
            if (Dist7(to) > 0.3f) Seven.Place(to, Flat(to - P7));
            Seven.ScriptDriven = sd;
        }

        static Vector3? Head(Animator a) { if (a == null || !a.isHuman) return null; var h = a.GetBoneTransform(HumanBodyBones.Head); return h != null ? h.position : (Vector3?)null; }
        Animator _sevenAnim; Animator SevenAnim => _sevenAnim != null ? _sevenAnim : (_sevenAnim = Seven.GetComponent<Animator>());
        Vector3 Head7 => Head(SevenAnim) ?? P7 + Vector3.up * 1.55f;
        static Vector3 HeadOf(ElarimNpc n) => Head(n.Anim) ?? n.transform.position + Vector3.up * 3.2f;

        // ------------------------------------------------------------------------------------------ B01: the bike, Panther
        public const float PantherScale = 2.7f;
        static readonly Vector3 PantherStand = new Vector3(3.2f, 0f, 6.6f);        // Round 13: 3.3 m from the bike (was 9 m: the two-shots were far too wide), room to grow
        bool _measurePanther;

        IEnumerator B01()
        {
            yield return Cutscene(true);
            Seven.Lock(true);
            var ik = Seven.gameObject.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
            ik.enabled = true; ik.Alive = true; ik.Ease = 2.5f; ik.LeftW = 0f; ik.RightW = 0f; ik.LookW = 0f;
            yield return Say("VEN_B01", 1, 2);                             // "That's a walk." / "You have a vehicle."
            // she looks back toward the ship
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(VenusLayout.SaucerAt - P7)), 300f * Time.deltaTime); yield return null; }
            yield return Say("VEN_B01", 3, 4);
            // QF10: the hull opens along a seam, the bike glides out on its hover glow, the hull closes seamless again
            if (Cat != null) Cat.GoTo(PantherStand, Cat.StrollSpeed, VenusLayout.BikeUnpacked);
            yield return HatchAndBike();
            // she walks over; her hand settles into a recessed grip; the vents hum, lifting it gently
            yield return WalkSeven(VenusLayout.BikeUnpacked - Vector3.right * 1.05f - Vector3.forward * 0.2f, 5f);
            Seven.Lock(true);
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Vector3.forward), 300f * Time.deltaTime); yield return null; }
            Cue("low three-quarter on the bike");
            ik.LeftPos = Bike.GripL.position + Vector3.up * 0.02f; ik.LeftW = 1f; ik.LookPos = Bike.GripL.position; ik.LookW = 0.6f;
            yield return Wait(1.0f);
            yield return Shot("v07_b01_low_three_quarter_bike");
            { var g = Bike.GripL.position; Shoot(g + Bike.transform.right * -0.9f + Vector3.up * 0.35f + Bike.transform.forward * 0.5f, g, 32f, 0.6f); }   // insert: her hand in the grip
            yield return Wait(0.9f);
            yield return Shot("v07b_b01_insert_hand_in_the_grip");
            yield return Say("VEN_B01", 5, 5, false);                      // "I'll watch the ship."
            // 7 starts to mount the hover bike, then pauses - and turns to him
            ik.LeftW = 0f; ik.LookW = 0f;
            for (float t = 0f; t < 0.7f; t += Time.deltaTime) { if (Cat != null) Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(Cat.transform.position - P7)), 260f * Time.deltaTime); yield return null; }
            FrameAll(1.0f, 12f, 1.0f, Cat != null ? Cat.transform : null);
            // Round 13 (0l): Dan's rewritten name reveal (VEN_B01_06-20, delivery directions voiced into the takes)
            Transform catT = Cat != null ? Cat.transform : null;
            Vector3 CatAt(float up) => catT != null ? catT.position + Vector3.up * up * catT.localScale.y : P7 + Vector3.forward;
            yield return Say("VEN_B01", 6, 6, false);                      // "What should I call you?" - casually
            yield return Say("VEN_B01", 7, 7, false);                      // "Panther."
            // she looks him up and down
            if (catT != null) { ik.LookPos = CatAt(0.3f); ik.LookW = 0.8f; yield return Wait(0.5f); ik.LookPos = CatAt(0f); yield return Wait(0.5f); ik.LookPos = CatAt(0.3f); }
            // her surprised look down at him
            ik.LookPos = CatAt(0.15f); ik.LookW = 1f;
            yield return Say("VEN_B01", 8, 8, false);                      // "Panther?" - a small, surprised laugh
            yield return Shot("v08a_b01_looks_down_at_him");
            yield return Say("VEN_B01", 9, 9, false);                      // "Ahem. You could say it without looking down at me."
            yield return Say("VEN_B01", 10, 10, false);                    // teasing
            // wide two-shot - framed for his full grown size from the start: he holds her gaze, then stretches and grows
            _measurePanther = true;
            FrameAll(1.0f, -18f, 1.4f, catT, PantherScale);
            yield return Wait(1.0f);                                         // he holds her gaze
            if (Cat != null) StartCoroutine(PantherStretch());
            // she takes a sharp breath and steps back, catching the fairing; her gaze rises with him
            {
                yield return Wait(0.9f);
                var back = P7 + Flat(P7 - (catT != null ? catT.position : P7 + Vector3.forward)).normalized * 0.45f;
                back = Ground.ClampToArena(back, 0.3f);
                var a0 = P7;
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f) { Seven.transform.position = Vector3.Lerp(a0, back, Mathf.SmoothStep(0f, 1f, t)); ik.LookPos = CatAt(0.4f); yield return null; }
                ik.LeftPos = Bike.Fairing.position; ik.LeftW = 1f;
                yield return Shot("v08_b01_panther_stretch_grows");
                for (float t = 0f; t < 1.5f; t += Time.deltaTime) { ik.LookPos = CatAt(0.6f); yield return null; }
                yield return Shot("v08b_b01_panther_grown");
            }
            FrameAll(1.0f, 40f, 0.01f, catT);
            yield return Wait(0.6f);
            yield return Shot("v09_b01_steps_back_hand_on_the_fairing");
            // the exchange - every shot holds all of him (and her), from changing sides
            FrameAll(1.0f, -40f, 0.01f, catT);
            yield return Say("VEN_B01", 11, 11, false);                    // "There. Saved you the trouble." - perfectly composed
            FrameAll(0.9f, 70f, 0.01f, catT);
            yield return Say("VEN_B01", 12, 12, false);                    // "You can do that?" - breathless, a disbelieving laugh
            yield return Shot("v10_b01_you_can_do_that");
            FrameAll(1.0f, -40f, 0.01f, catT);
            yield return Say("VEN_B01", 13, 13, false);                    // "Apparently."
            // she looks him over, taking in his enormous paws
            ik.LeftW = 0f;
            FrameAll(1.0f, 25f, 0.8f, catT);
            if (catT != null) { ik.LookPos = CatAt(0.05f); ik.LookW = 0.9f; yield return Wait(0.9f); }
            yield return Shot("v12_b01_looks_at_his_paws");
            ik.LookPos = CatAt(0.6f);
            FrameAll(0.9f, 15f, 0.01f, catT);
            yield return Say("VEN_B01", 14, 14, false);                    // "Hang on. We crawled through a prison drain."
            FrameAll(1.0f, -60f, 0.01f, catT);
            yield return Say("VEN_B01", 15, 15, false);                    // "An excellent time to be small." - mildly
            yield return Shot("v11_b01_an_excellent_time_to_be_small");
            // she opens her mouth to argue, then a reluctant little chuckle (in the take)
            yield return Wait(0.5f);
            yield return Say("VEN_B01", 16, 16, false);                    // "Mm. Fair point."
            // he settles onto his haunches, thoroughly pleased with himself; she glances toward the saucer
            if (Cat != null) Cat.Scripted("sit");
            FrameAll(1.0f, 25f, 0.8f, catT);
            yield return Wait(0.9f);
            yield return Shot("v12c_b01_on_his_haunches");
            ik.LookPos = VenusLayout.SaucerAt + Vector3.up * 3.5f; yield return Wait(0.9f);
            yield return Shot("v12b_b01_looks_to_the_saucer");
            // she points at him, still amused
            ik.LookPos = CatAt(0.6f);
            {
                var sh = P7 + Vector3.up * 1.4f; var dir = (CatAt(0.5f) - sh).normalized;
                ik.RightPos = sh + dir * 0.62f + Seven.transform.right * 0.12f; ik.RightW = 0.85f;
            }
            FrameAll(1.0f, -30f, 0.01f, catT);
            yield return Say("VEN_B01", 17, 17, false);                    // "You're not getting a bigger bowl." - pointing
            yield return Shot("v13a_b01_points_at_him");
            ik.RightW = 0f;
            yield return Say("VEN_B01", 18, 18, false);                    // "Mmm. We can discuss that when you return."
            // she laughs softly and shakes her head as she settles into the saddle
            FrameAll(1.1f, 55f, 0.01f, catT);
            {
                var c = ik.LookPos; var rgt = Vector3.Cross(Vector3.up, Flat(c - P7).normalized);
                for (float t = 0f; t < 1.0f; t += Time.deltaTime) { ik.LookPos = c + rgt * Mathf.Sin(t * Mathf.PI * 4f) * 0.9f * (1f - t); yield return null; }
                yield return Shot("v13_b01_shakes_her_head");
            }
            ik.LookW = 0f;
            Bike.Mount();
            yield return Wait(1.0f);
            yield return Shot("v14_b01_settles_into_the_saddle");
            yield return Say("VEN_B01", 19, 19, false);                    // "Look after the ship, Panther." - warmly
            // he lies down beside the ship, under the rim (no ramp - 0b), still large
            if (Cat != null) Cat.GoTo(VenusLayout.PantherLie, Cat.StrollSpeed * 1.2f, VenusLayout.BikeUnpacked, "lie_down");
            FrameAll(1.0f, 10f, 1.2f, catT);
            yield return Wait(2.6f);
            yield return Shot("v15_b01_he_lies_down_large");
            yield return Say("VEN_B01", 20, 20, false);                    // "I intend to." - quietly assured
            yield return Shot("v16_b01_i_intend_to_mounted");
            // the bike rises with a low hum; one last look back at him before she rides toward the gardens
            AudioKit.Play("bike_liftoff", Bike.transform.position, 0.8f);
            if (catT != null) { ik.enabled = true; ik.LookPos = CatAt(1.0f); ik.LookW = 0.9f; }
            FrameAll(1.0f, -35f, 0.6f, catT);
            yield return Wait(1.3f);
            yield return Shot("v17_b01_one_last_look");
            ik.LookW = 0f;
            _measurePanther = false;
            var rg = Seven.RideGaps;
            Log($"bike grips (QF10, palm centre to grip): left {rg.x:0.000} m, right {rg.y:0.000} m; feet to the side panels: left {rg.z:0.000} m, right {rg.w:0.000} m; hips to the saddle {Seven.RideHipsGap:0.000} m; ride state '{(Seven.State ?? "-")}'");
            Log($"bike sound (0h): hum loop {(Bike.HasHum ? "playing" : "MISSING")}, hum level {Bike.HumLevel:0.00}, whoosh level {Bike.WhooshLevel:0.00} (riding, standing)");
            Log("after B01: " + ChapterCat.SpeedReport());
            Complete("VEN_B01", VenusState.Stage.Landed);
            if (Shots) yield return RidePoseShots();
            if (Auto && DemoBoot.Quick) { Cap.Finish("Venus quick check: up to B01 (mounted)"); yield break; }
            Objective(VenusState.Objectives[(int)VenusState.Stage.Landed]);  // "Ride to the Elarim temple"
            yield return Cutscene(false);
            StartCoroutine(TeachRiding());
        }

        /// <summary>Check runs: the riding pose from the side, the front and the riding camera (hands, feet, astride).</summary>
        IEnumerator RidePoseShots()
        {
            var b = Bike.transform;
            Shoot(b.position + b.right * -3.2f + Vector3.up * 1.1f, b.position + Vector3.up * 1.0f, 40f, 0.01f); yield return Wait(0.3f);
            yield return Shot("ride_pose_side_left");
            Shoot(b.position + b.right * 3.2f + Vector3.up * 1.1f, b.position + Vector3.up * 1.0f, 40f, 0.01f); yield return Wait(0.3f);
            yield return Shot("ride_pose_side_right");
            Shoot(b.position + b.forward * 3.6f + Vector3.up * 1.4f, b.position + Vector3.up * 1.0f, 40f, 0.01f); yield return Wait(0.3f);
            yield return Shot("ride_pose_front");
            FollowCamera(); Cam.SnapBehind(); yield return Wait(0.5f);
            yield return Shot("ride_pose_riding_camera");
            var rg = Seven.RideGaps;
            Log($"RIDE POSE (QF10): extra reach lean {Seven.RideReachLean:0} deg, bike saddle top {Bike.SaddleTop:0.00} m, knee half-width {Bike.KneeWidth:0.00} m; palm-to-grip left {rg.x * 100f:0.0} cm, right {rg.y * 100f:0.0} cm (must be <= 2); feet-to-panel left {rg.z * 100f:0.0} cm, right {rg.w * 100f:0.0} cm; hips-to-saddle {Seven.RideHipsGap * 100f:0.0} cm; legs-through-bike {LegsThroughBike()}");
        }

        /// <summary>QF10: does any leg bone segment pass through the bike's body (its renderers' mesh colliders)?</summary>
        string LegsThroughBike()
        {
            int inside = 0, n = 0;
            var cols = new List<Collider>();
            foreach (var mf in Bike.GetComponentsInChildren<MeshFilter>()) { if (mf.name != "BikeModel" && mf.transform.parent?.name != "BikeModel" && !mf.name.StartsWith("hoverbike")) continue; var mc = mf.gameObject.GetComponent<MeshCollider>() ?? mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; cols.Add(mc); }
            if (cols.Count == 0) return "n/a (no bike mesh)";
            Physics.SyncTransforms();
            foreach (var (a, b) in new[] { (HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg), (HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot), (HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg), (HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot) })
            {
                var A = Seven.Bone(a); var B = Seven.Bone(b); if (A == null || B == null) continue;
                for (int k = 1; k < 8; k++)
                {
                    var p = Vector3.Lerp(A.position, B.position, k / 8f); n++;
                    // a point is inside the closed bike mesh when a ray from far above hits the mesh an odd number of times
                    var o = p + Vector3.up * 5f; int hits = 0;
                    foreach (var c in cols) { var q = o; for (int g = 0; g < 8 && c.Raycast(new Ray(q, Vector3.down), out var hit, (q.y - p.y)); g++) { hits++; q = hit.point + Vector3.down * 0.002f; } }
                    if (hits % 2 == 1) inside++;
                }
            }
            foreach (var c in cols) Destroy(c);
            return $"{inside} of {n} leg sample points inside the bike body";
        }

        /// <summary>QF10: frame 7 and Panther (and the bike) so all of them are on screen: the camera sits on the bounding
        /// sphere's tangent distance for the vertical field of view, from a direction (degrees round from the line between
        /// them), slightly above. grownScale > 0: frame for the size he is about to grow to.</summary>
        void FrameAll(float pad, float aroundDeg, float ease, Transform cat, float grownScale = 0f)
        {
            var b = BoundsOf(Seven.transform);
            if (cat != null)
            {
                var cb = BoundsOf(cat);
                if (grownScale > 0f && cat.localScale.x > 0.01f)
                {
                    float k = grownScale / cat.localScale.x; var g = cat.position;
                    cb = new Bounds(g + (cb.center - g) * k, cb.size * k);
                }
                b.Encapsulate(cb);
            }
            var line = cat != null ? Flat(cat.position - P7) : Seven.transform.forward; if (line.sqrMagnitude < 1e-3f) line = Vector3.forward;
            var side = Vector3.Cross(Vector3.up, line.normalized);
            var dir = Quaternion.AngleAxis(aroundDeg, Vector3.up) * side; dir = (dir + Vector3.up * 0.28f).normalized;
            // keep the camera off the saucer side when possible (it would look into the hull)
            if (Vector3.Dot(Flat(VenusLayout.SaucerAt - b.center).normalized, Flat(dir).normalized) > 0.6f) dir = Vector3.Reflect(dir, side);
            float fov = 46f, r = b.extents.magnitude * pad;
            float dist = r / Mathf.Sin(fov * 0.5f * Mathf.Deg2Rad);
            Shoot(b.center + dir * dist, b.center, fov, ease);
        }

        static Bounds BoundsOf(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            Bounds b = new Bounds(t.position + Vector3.up * 0.8f, Vector3.one * 0.4f); bool any = false;
            foreach (var r in rs) { if (!r.enabled || r is ParticleSystemRenderer) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            return b;
        }

        /// <summary>Fraction (0..1) of a bounding box's screen rectangle outside the frame.</summary>
        float OffScreen(Bounds b)
        {
            var cam = Cam.GetComponent<Camera>();
            float x0 = 1e9f, y0 = 1e9f, x1 = -1e9f, y1 = -1e9f;
            for (int i = 0; i < 8; i++)
            {
                var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var v = cam.WorldToViewportPoint(c);
                if (v.z < 0f) return 1f;
                x0 = Mathf.Min(x0, v.x); y0 = Mathf.Min(y0, v.y); x1 = Mathf.Max(x1, v.x); y1 = Mathf.Max(y1, v.y);
            }
            float area = Mathf.Max(1e-6f, (x1 - x0) * (y1 - y0));
            float ix = Mathf.Max(0f, Mathf.Min(1f, x1) - Mathf.Max(0f, x0)), iy = Mathf.Max(0f, Mathf.Min(1f, y1) - Mathf.Max(0f, y0));
            return 1f - ix * iy / area;
        }

        /// <summary>Measurements logged with every capture: Panther on screen (B01 after the growth), the speaker in frame.</summary>
        void ShotChecks(string name)
        {
            if (_measurePanther && Cat != null && Cat.gameObject.activeInHierarchy)
                Log($"shot '{name}': Panther {OffScreen(BoundsOf(Cat.transform)) * 100f:0.0} % off screen (QF10: must be 0), 7 {OffScreen(BoundsOf(Seven.transform)) * 100f:0.0} %");
        }

        // ---- QF10: the hull hatch
        IEnumerator HatchAndBike()
        {
            Bike.gameObject.SetActive(true);
            var dir = VenusLayout.HatchDir; var tan = Vector3.Cross(Vector3.up, dir);
            float bottomIn = VenusLayout.SaucerHover + SaucerHold.BottomUp;
            var inBay = VenusLayout.BayInside; float yawOut = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            Bike.Glide(inBay, yawOut, bottomIn);
            // QF11 (Dan's screenshot: the 3 m bike poked out through the closed hull): nothing of the bike shows until
            // the panel has opened - it is hidden in the bay and appears only once the opening is clear
            BikeShown(false);
            var mouth = VenusLayout.HatchMouth;
            // the camera: outside the hatch, a little to the side, near hull height
            Shoot(mouth + dir * 6.2f + tan * 2.6f - Vector3.up * 0.3f, mouth - Vector3.up * 0.1f, 42f, 1.2f);
            yield return Wait(1.3f);
            LogBikeThroughHull("hull closed, before the hatch opens");
            yield return Shot("v06a_b01_seamless_hull");
            // QF13 (1): the shared hold (SaucerHold): the seam lights, the panel sinks into the hull and slides round inside
            // it, the seam goes dark again - an empty opening (flat unlit inside) with only the bike in it
            Material seamM = null, backM = null;
            yield return Hold.Open(() => StartCoroutine(Shot("v06b_b01_hatch_seam_glows")), slide =>
            {
                if (slide > 0.02f && !_bikeShown) BikeShown(true);     // QF14 (3): the room behind the panel - the bike sits in its cradles
                if (Shots && !_bikeShown && !_hullCheckLogged) { _hullCheckLogged = true; LogBikeThroughHull("hatch opening (panel " + (slide * 100f).ToString("0") + " % open)"); }
                if (Shots && slide > 0.5f && !_holdMidShot) { _holdMidShot = true; StartCoroutine(Shot("qf13_01a_b01_hold_half_open_player_view")); }
            });
            BikeShown(true);
            yield return Shot("v06c_b01_hatch_open_light_inside");
            // QF14 (3): the bay straight on (the reference's view) and close from the player's side, same instant
            if (Shots && Cap != null)
            {
                var axis = VenusLayout.SaucerAt + Vector3.up * (VenusLayout.SaucerHover + 0.72f);
                yield return Cap.ShotFrom("qf14_03_bay_straight_on", axis + dir * 7.2f, axis + dir * 2.0f, 30f);
                yield return Cap.ShotFrom("qf14_03_bay_three_quarter", axis + dir * 6.8f + tan * 2.2f + Vector3.up * 0.15f, axis + dir * 1.8f, 34f);
            }
            // the bike glides out on its hover glow, unhurried
            var outAt = VenusLayout.BayOutside;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.8f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Bike.Glide(Vector3.Lerp(inBay, outAt, u), yawOut, bottomIn);
                Cam.Shot(mouth + dir * 6.2f + tan * 2.6f - Vector3.up * 0.3f + dir * 1.5f * u, Bike.transform.position + Vector3.up * (bottomIn - Bike.transform.position.y + 0.4f), 0.8f, false, 42f);
                if (t > 0.6f && t - Time.deltaTime / 2.8f <= 0.6f) StartCoroutine(Shot("v06d_b01_bike_glides_out"));
                yield return null;
            }
            // it turns slowly and settles beside her at riding height with a low hum; the hatch closes behind it
            StartCoroutine(CloseHatch(seamM, backM));
            var settle = VenusLayout.BikeUnpacked; float settleBottom = Ground.Height(settle.x, settle.z) + Bike.HoverHeight - 0.12f;
            Shoot((outAt + settle) * 0.5f + Vector3.Cross(Vector3.up, Flat(settle - outAt).normalized) * -7.5f + Vector3.up * 1.6f + Flat(P7 - settle) * 0.2f, (outAt + settle) * 0.5f + Vector3.up * 0.6f, 48f, 1.6f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 3.0f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Bike.Glide(Vector3.Lerp(outAt, settle, u), Mathf.LerpAngle(yawOut, 0f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.2f))), Mathf.Lerp(bottomIn, settleBottom, u * u));
                if (t > 0.55f && t - Time.deltaTime / 3.0f <= 0.55f) StartCoroutine(Shot("v06e_b01_bike_turns_and_settles"));
                yield return null;
            }
            Bike.EndGlide(); Bike.Place(settle, 0f);
            yield return Wait(0.6f);
            yield return Shot("v06f_b01_bike_beside_her");
            // the hull is seamless again
            while (_hatchOpen) yield return null;
            Shoot(mouth + dir * 5.5f - tan * 2.2f + Vector3.up * 0.2f, mouth, 40f, 0.01f);
            yield return Wait(0.2f);
            yield return Shot("v06g_b01_hull_seamless_again");
            if (Shots) { Shoot(VenusLayout.SaucerAt + Chapter1Director.SaucerCheckOffset, VenusLayout.SaucerAt + Vector3.up * 1.6f, 45f, 0.01f); yield return Wait(0.3f); yield return Shot("saucer_side_by_side_venus"); }
            Log($"hatch (QF10): opened along the seam, the bike glided out and settled at riding height; closed again - panel offset {(Hatch != null ? Hatch.localRotation.eulerAngles.y : 0f):0.000} deg, scale {(Hatch != null ? Hatch.localScale.x : 1f):0.000}, seam emission {(seamM != null ? seamM.GetColor("_EmissionColor").r : 0f):0.000}, bay light {(BayLight != null ? BayLight.intensity : 0f):0.00}");
        }
        bool _hatchOpen = true, _bikeShown = true, _hullCheckLogged, _holdMidShot;
        void BikeShown(bool on)
        {
            _bikeShown = on;
            foreach (var r in Bike.GetComponentsInChildren<Renderer>(true)) r.enabled = on;
            foreach (var l in Bike.GetComponentsInChildren<Light>(true)) l.enabled = on;
        }
        /// <summary>QF11 check: bike renderers that are on (visible) while the hatch is not open - must be 0.</summary>
        void LogBikeThroughHull(string when)
        {
            int on = 0, n = 0; foreach (var r in Bike.GetComponentsInChildren<Renderer>(true)) { n++; if (r.enabled && r.gameObject.activeInHierarchy) on++; }
            Log($"bike vs the hull (QF11) {when}: {on} of {n} bike renderers shown (must be 0 while the hull is closed)");
        }
        IEnumerator CloseHatch(Material seamM, Material backM)
        {
            _hatchOpen = true;
            yield return Wait(1.2f);
            yield return Hold.Close();
            _hatchOpen = false;
        }

        IEnumerator PantherStretch()
        {
            Cat.Scripted("stretch");
            var s0 = Cat.transform.localScale.x;
            yield return Wait(0.8f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.6f)       // uniform, smooth, grounded (the clip grounds every frame)
            {
                float s = Mathf.Lerp(s0, PantherScale, Mathf.SmoothStep(0f, 1f, t));
                Cat.transform.localScale = Vector3.one * s;
                var p = Cat.transform.position; p.y = Ground.Height(p.x, p.z); Cat.transform.position = p;
                yield return null;
            }
            Log($"Panther: stretched and grew to x{PantherScale} (uniform), stays large; label 'Panther' from VEN_B01_07");
        }

        IEnumerator TeachRiding()
        {
            if (_taught.Contains("ride")) yield break;
            _taught.Add("ride");
            Legend.Teach((ControlsLegend.Btn.Move, "Steer"), (ControlsLegend.Btn.Tk, "Accelerate"), (ControlsLegend.Btn.Block, "Brake"));
            if (Auto) { yield return Wait(1.2f); yield return Shot("t_ride_xbox"); }
            float t0 = Time.time;
            while (Bike.Odometer < 25f && Time.time - t0 < 14f) yield return null;
            Legend.Teach();
        }

        // ------------------------------------------------------------------------------------------ prompts (riding, the slab, skip)
        void Update()
        {
            if (PauseMenu.IsOpen || _cut || Dlg.Busy && !_gameplayLines) { if (_cut) Legend.Context(ControlsLegend.Btn.Interact, null); return; }
            // the skip offer is a teaching line, so the step's own action (e.g. telekinesis) still works meanwhile
            if (SkipOffered != _skipShown) { _skipShown = SkipOffered; if (SkipOffered) Legend.Teach((ControlsLegend.Btn.Skip, "Skip this step")); else Legend.Teach(); }
            if (!VenusState.SceneDone("VEN_B01")) { Legend.Context(ControlsLegend.Btn.Interact, null); return; }
            if (Part2Prompt()) return;                                     // Round 12: devices, console, guardian, record, boarding
            bool canRide = !_noRide;
            bool atWall = Seven.Context != null && (Seven.Context.StartsWith("jump:Climb") || Seven.Context.StartsWith("jump:Jump down"));   // the climb prompt wins at the wall
            if (_slabReady && !Bike.Riding && Seven.Current == ChapterSeven.Mode.Free && SlabInView() && !atWall)
            {
                Legend.Context(ControlsLegend.Btn.Tk, "Telekinesis: the fallen slab");
                if (!Auto && Input.Telekinesis.WasPressedThisFrame()) _slabHit = true;
                return;
            }
            if (Bike.Riding)
            {
                bool stopped = Bike.Speed < 1.2f;
                // QF21 (4) wrong state: the slab needs her on foot - say so instead of nothing
                bool slab = _slabReady && SlabInView();
                Legend.Context(ControlsLegend.Btn.Interact, stopped ? (slab ? "Get off - the slab needs telekinesis on foot" : "Get off") : (slab ? "Stop and get off for the slab" : null));
                if (stopped && !Auto && Input.Interact.WasPressedThisFrame()) { if (Bike.Dismount()) FollowCamera(); }
            }
            else if (canRide && Seven.Current == ChapterSeven.Mode.Free && Bike.CanMount(P7))
            {
                Legend.Context(ControlsLegend.Btn.Interact, "Ride");
                if (!Auto && Input.Interact.WasPressedThisFrame()) { Bike.Mount(); FollowCamera(); }
            }
            else if (Seven.Context != null)
            {
                int c = Seven.Context.IndexOf(':');
                Legend.Context(Seven.Context.StartsWith("jump") ? ControlsLegend.Btn.Jump : Seven.Context.StartsWith("crouch") ? ControlsLegend.Btn.Crouch : ControlsLegend.Btn.Interact, Seven.Context.Substring(c + 1));
            }
            else Legend.Context(ControlsLegend.Btn.Interact, null);
        }
        bool _gameplayLines, _noRide, _skipShown;

        IEnumerator SayInGameplay(string scene, int from = 1, int to = 99)
        {
            _gameplayLines = true;
            if (_coverage) { foreach (var l in Lines(scene, from, to)) { _lineSerial++; StartCoroutine(LineShot(l, _lineSerial)); yield return Dlg.PlayLines(new List<VoiceBank.Line> { l }, false); } }
            else yield return Dlg.PlayLines(Lines(scene, from, to), false);
            _gameplayLines = false;
        }

        // ------------------------------------------------------------------------------------------ B02, B03 + C02, the bridge, B04
        /// <summary>Round 13 (0d) check (-r13 bridge): the slab bridge from several approach angles, both ways, ridden like
        /// a player (aiming at the far side, no help): each must get across within 12 s.</summary>
        IEnumerator BridgeTest()
        {
            SetBridge(true);
            if (Slab != null) Slab.SetPositionAndRotation(BridgePos, Quaternion.identity);
            if (WalkwayIntact != null) WalkwayIntact.gameObject.SetActive(false);
            yield return Wait(0.5f);
            Shoot(new Vector3(38.5f, 4.2f, 110f), new Vector3(30f, 0f, 124f), 52f, 0.01f); yield return Wait(0.4f);
            yield return Shot("br_01_bridge_and_ramps");
            Shoot(new Vector3(22.5f, 1.4f, 114f), new Vector3(30f, 0f, 116f), 46f, 0.01f); yield return Wait(0.4f);
            yield return Shot("br_02_south_ramp_close");
            int ok = 0, n = 0; Bike.Snags = 0;
            foreach (int dirSign in new[] { 1, -1 })
                foreach (float ang in new[] { -35f, -20f, 0f, 20f, 35f })
                {
                    n++;
                    var entry = new Vector3(30f, 0f, dirSign > 0 ? 114.5f : 131f);
                    var heading = Quaternion.Euler(0f, ang, 0f) * (dirSign > 0 ? Vector3.forward : Vector3.back);
                    var start = entry - heading * 6f;
                    var goal = new Vector3(30f, 0f, dirSign > 0 ? 136f : 109f);
                    PlaceRiding(start, Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg);
                    FollowCamera(); Cam.SnapBehind();
                    yield return Wait(0.3f);
                    Bike.ScriptDriven = true; int snag0 = Bike.Snags;
                    bool across = false; float t0 = Time.time;
                    while (Time.time - t0 < 12f)
                    {
                        var to = Flat(goal - Bike.transform.position);
                        float a = Vector3.SignedAngle(Flat(Bike.transform.forward), to, Vector3.up);
                        Bike.ScriptSteer = Mathf.Clamp(a / 35f, -1f, 1f); Bike.ScriptThrottle = 0.65f;
                        if (dirSign > 0 ? P7.z > 131.5f : P7.z < 113.5f) { across = true; break; }
                        if (Time.time - t0 > 1.2f && Time.time - t0 < 1.3f && n == 1) StartCoroutine(Shot("br_03_riding_onto_it_at_an_angle"));
                        yield return null;
                    }
                    Bike.ScriptThrottle = 0f; Bike.ScriptSteer = 0f; Bike.ScriptDriven = false;
                    if (across) ok++;
                    Log($"bridge test (0d): {(dirSign > 0 ? "north-bound" : "south-bound")} at {ang:+0;-0;0} deg: {(across ? "ACROSS" : "STUCK")} in {Time.time - t0:0.0} s, bike at {P7:0.0}, frames it could not move {Bike.Snags - snag0}");
                }
            Cap.Finish($"BRIDGE TEST (0d): {ok}/{n} approaches got across; frames the bike could not move {Bike.Snags}");
        }

        void PlaceRiding(Vector3 at, float yaw)
        {
            if (!Bike.Riding) { Seven.Place(at - Quaternion.Euler(0f, yaw, 0f) * Vector3.right * 1.3f, Quaternion.Euler(0f, yaw, 0f) * Vector3.forward); }
            Bike.Place(at, yaw);
            if (!Bike.Riding) Bike.Mount();
            FollowCamera(); Cam.SnapBehind();
        }

        IEnumerator RideToTemple()
        {
            if (Auto) StartCoroutine(AutoRide(RoutePts(P7.z, 104f), () => VenusState.SceneDone("VEN_B03") && P7.z > 100f || P7.z > 104f));
            // QF13 (2): the walkway breaks AHEAD of her - its own watcher from the start of the ride (not after B02's lines)
            _collapseFired = _collapseDone = VenusState.SceneDone("VEN_B03");
            if (!_collapseDone) StartCoroutine(CollapseWatch());
            // B02 at the long open bend (in gameplay, the normal riding camera)
            if (!VenusState.SceneDone("VEN_B02"))
            {
                yield return Until(() => P7.z > VenusLayout.BendZ - 6f && P7.x > 6f || P7.z > 60f, "the long open bend (B02)", 120f,
                    "Ride north along the garden road (RT to accelerate)", VenusLayout.BendSpot, () => PlaceRiding(VenusLayout.BendSpot + Vector3.right * 4f, 90f));
                yield return Shot("v11_b02_the_long_open_bend");
                if (!_collapseFired) yield return SayInGameplay("VEN_B02", 1, 3);
                if (!_collapseFired) yield return Wait(1.2f);              // a short pause
                if (!_collapseFired) yield return SayInGameplay("VEN_B02", 4, 4);
                Complete("VEN_B02");
            }
            // B03 + C02: workers signal to slow down, water across the route, the tremor (movement never locked)
            if (!VenusState.SceneDone("VEN_B03"))
            {
                // QF13 (2): the watcher fires the tremor + collapse while she is still well short of the gully (on any
                // approach line); here the flow only waits for it to be over
                yield return Until(() => _collapseDone, "the damaged route (B03)", 120f,
                    "Follow the road on round the bend", new Vector3(30f, 0f, VenusLayout.TremorZ), () => PlaceRiding(new Vector3(30f, 0f, VenusLayout.TremorZ + 1f), 0f));
                yield return SayInGameplay("VEN_C02", 1, 2);
                Complete("VEN_C02"); Complete("VEN_B03");
            }
            // QF10 (VEN_B03 changed by Dan): the collapse - stop, get off, lay the fallen slab across with telekinesis, ride over
            if (!VenusState.SceneDone("VEN_B03_bridge") && P7.z < VenusLayout.CollapseZ1) yield return Bridge();
            else Objective(VenusState.Objectives[(int)VenusState.Stage.Landed]);
            // on to the temple court, riding (or walking)
            if (Auto) { yield return AutoMount(); StartCoroutine(AutoRide(RoutePts(P7.z, 196f), () => P7.z > VenusLayout.CourtZ + 3f)); }
            yield return Until(() => P7.z > VenusLayout.CourtZ, "the temple court (B04)", 150f,
                "Ride on north to the temple court", new Vector3(30f, 0f, VenusLayout.CourtZ), () => PlaceRiding(new Vector3(30f, 0f, VenusLayout.CourtZ + 1f), 0f));
            // B04: she parks beside the court, gets off; the bike settles into a low idle hover as the guardian approaches
            yield return Cutscene(true);
            if (Bike.Riding) { Bike.Dismount(true); }
            Bike.Place(VenusLayout.ParkSpot, 180f);
            Seven.Place(VenusLayout.ParkSpot - Vector3.right * 1.7f + Vector3.forward * 1f, Vector3.forward);
            Seven.Lock(true);
            VenusState.SaveBike(Bike.transform.position, Bike.transform.eulerAngles.y, false);
            Shoot(VenusLayout.ParkSpot + new Vector3(10f, 4f, -9f), VenusLayout.MeetSpot + Vector3.up * 2f, 55f);
            // QF23 (9): they walk TOGETHER and briefly - both start a couple of metres from the meeting spot (a cut)
            if (Guardian != null) { var gm = VenusLayout.MeetSpot + Vector3.forward * 1.6f; var gd = Flat(gm - Guardian.transform.position); if (gd.magnitude > 3f) Guardian.transform.position = gm - gd.normalized * 2.6f; Guardian.WalkTo(gm); }
            { var sm = VenusLayout.MeetSpot - Vector3.forward * 1.4f; var sd = Flat(sm - P7); if (sd.magnitude > 3f) Seven.Place(sm - sd.normalized * 2.6f, sd); }
            yield return Wait(1.2f);
            yield return Shot("v14a_b04_guardian_walking_cape");
            yield return WalkSeven(VenusLayout.MeetSpot - Vector3.forward * 1.4f, 4f);
            Seven.Lock(true);
            yield return Shot("v14_b04_temple_arrival_wide");
            Complete("VEN_B04");
        }

        // ---- QF10: the telekinesis bridge
        Renderer _slabEdges;
        bool _slabReady, _slabHit;
        /// <summary>QF12 (5): where the broken walkway piece lies after the collapse - at the bottom of the gully, tilted on
        /// the rubble, dusty (the same piece she lifts and lays across).</summary>
        static Vector3 SlabRest => new Vector3(30.4f, VenusTerrain.Height(30.4f, 122.8f) + 0.55f, 122.8f);
        static Quaternion SlabRestRot => Quaternion.Euler(4f, 97f, 9f);
        static Vector3 SlabLookAt => SlabRest + Vector3.up * 0.3f;
        static Vector3 BridgePos => new Vector3(VenusLayout.SlabBridge.x, VenusLayout.BridgeTop - VenusLayout.SlabSize.y * 0.5f, VenusLayout.SlabBridge.z);
        bool SlabInView()
        {
            if (Slab == null) return false;
            var to = Flat(SlabLookAt - P7);
            if (to.magnitude > VenusLayout.SlabTkRange) return false;
            // she (or the camera) looks at it: the slab is long, so aim at its nearest point along its length
            var a = SlabRest - Vector3.right * VenusLayout.SlabSize.x * 0.5f; var b = SlabRest + Vector3.right * VenusLayout.SlabSize.x * 0.5f;
            var near = a + Vector3.right * Mathf.Clamp(P7.x - a.x, 0f, VenusLayout.SlabSize.x);
            var fwdCam = Flat(Cam.transform.forward).normalized;
            return Vector3.Angle(fwdCam, Flat(near - P7)) < 55f || Vector3.Angle(Flat(Seven.transform.forward), Flat(near - P7)) < 55f || Flat(near - P7).magnitude < 3.2f;
        }
        static void Glow(Renderer r, float v) { if (r == null) return; var m = r.material; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white * v); }

        IEnumerator Bridge()
        {
            Objective("Stop before the collapse");
            if (Auto) { yield return Until(() => P7.z > 102f || !Bike.Riding, "near the collapse", 30f); yield return AutoGetOff(); }
            yield return Until(() => !Bike.Riding, "off the bike at the collapse", 90f,
                "Brake (LT) in the open area before the hole, then get off (X)", VenusLayout.StopSpot, () => { if (Bike.Riding) { Bike.Place(VenusLayout.StopSpot, 0f); Bike.Dismount(true); FollowCamera(); } });
            yield return Shot("v13_b03_collapsed_walkway_slab_beside_it");
            Objective("Lay something across the gap");
            _slabReady = true; _slabHit = false;
            // the telekinesis highlight: the slab's edges glow softly when she looks at it
            bool pulsing = true;
            IEnumerator Pulse() { for (float t = 0f; pulsing; t += Time.deltaTime) { Glow(_slabEdges, SlabInView() ? 1.1f + 0.6f * Mathf.Sin(t * 3.5f) : 0.25f); yield return null; } Glow(_slabEdges, 0f); }
            StartCoroutine(Pulse());
            if (Auto)
            {
                yield return AutoWalkPath(new[] { new Vector3(30.4f, 0f, 110.6f) });
                Seven.ScriptMove = Vector3.zero;
                for (float t = 0f; t < 0.5f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Vector3.forward), 360f * Time.deltaTime); yield return null; }
                yield return AwaitTeach("tk");
                yield return Shot("v13a_b03_slab_highlighted");
                _slabHit = true;
            }
            else StartCoroutine(TeachWhen(() => SlabInView() && !Bike.Riding, "tk", (ControlsLegend.Btn.Tk, "Telekinesis")));
            yield return Until(() => _slabHit, "lay the fallen slab across the gap (telekinesis)", 60f,
                "Look at the long fallen slab beside the hole and use telekinesis (RT)", SlabRest, () => { _slabHit = true; _skipBridge = true; });
            _slabReady = false; pulsing = false;
            yield return LayTheSlab(_skipBridge);
            Objective("Ride across");
            if (Auto) { yield return AutoMount(); StartCoroutine(AutoRide(new[] { new Vector3(30f, 0f, 112f), new Vector3(30f, 0.12f, 122f), new Vector3(30f, 0f, 134f) }, () => P7.z > VenusLayout.CollapseZ1 + 3f)); }
            yield return Until(() => P7.z > VenusLayout.CollapseZ1 + 2f, "ride across the slab bridge", 60f,
                "Get back on the bike (X) and ride over the slab", BridgePos, () => PlaceRiding(new Vector3(30f, 0f, VenusLayout.CollapseZ1 + 3f), 0f));
            Log($"bridge: crossed {(Bike.Riding ? "riding" : "on foot")} (7 at {P7})");
            yield return Shot("v13d_b03_across_the_bridge");
            Objective(VenusState.Objectives[(int)VenusState.Stage.Landed]);
        }
        bool _skipBridge;

        IEnumerator TeachWhen(Func<bool> when, string key, (ControlsLegend.Btn, string) item)
        {
            if (_taught.Contains(key)) yield break;
            while (!when()) { if (!_slabReady) yield break; yield return null; }
            _taught.Add(key); _teachShown = key;
            Legend.Teach(item);
            while (_slabReady && !_slabHit) yield return null;
            Legend.Teach(); _teachShown = null;
        }
        string _teachShown;
        IEnumerator AwaitTeach(string key)
        {
            if (!_taught.Contains(key)) { _taught.Add(key); Legend.Teach((ControlsLegend.Btn.Tk, "Telekinesis")); yield return Wait(0.6f); yield return Shot("t_tk_xbox"); Legend.Teach(); }
        }

        IEnumerator LayTheSlab(bool skipped)
        {
            var from = Slab.position; var r0 = Slab.rotation; var to = BridgePos; var r1 = Quaternion.identity;
            if (!skipped)
            {
                yield return Cutscene(true);
                Seven.Gesture(SlabLookAt); TkAudit.Watch(this, "Venus: the slab bridge", SlabLookAt, null);   // QF24 (14)
                Seven.SetEnergyPoint(1.9f, 0.25f);
                // the beat from the side: her, the slab and the hole in one frame
                Shoot(new Vector3(25.2f, 3.4f, 104.2f), new Vector3(31f, 0.6f, 119f), 52f, 0.8f);
                for (float t = 0f; t < 3f && !Seven.GesturePeak; t += Time.deltaTime) yield return null;
                if (TkMotes != null) StartCoroutine(TkStream());
                AudioKit.Play("slab_lift", from, 1f, 0.05f, AudioKit.Bus.Sfx, 40f);
                // it rises, swings round over the hole (unhurried, heavy), and comes down across it
                var up = from + Vector3.up * 2.2f; var over = to + Vector3.up * 2.4f;
                for (float t = 0f; t < 1f; t += Time.deltaTime / 1.4f) { float u = Mathf.SmoothStep(0f, 1f, t); Slab.position = Vector3.Lerp(from, up, u) + new Vector3(0f, Mathf.Sin(t * 19f) * 0.03f, 0f); Slab.rotation = Quaternion.Slerp(r0, Quaternion.Euler(0f, VenusLayout.SlabLieYaw, 0f), u); yield return null; }
                yield return Shot("v13b_b03_slab_lifted");
                for (float t = 0f; t < 1f; t += Time.deltaTime / 2.4f) { float u = Mathf.SmoothStep(0f, 1f, t); Slab.position = Vector3.Lerp(up, over, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.6f; Slab.rotation = Quaternion.Slerp(Quaternion.Euler(0f, VenusLayout.SlabLieYaw, 0f), r1, u); if (t > 0.5f && t - Time.deltaTime / 2.4f <= 0.5f) StartCoroutine(Shot("v13c_b03_slab_swings_over_the_gap")); yield return null; }
                for (float t = 0f, v = 0f; Slab.position.y > to.y + 0.001f && t < 2f; t += Time.deltaTime) { v += 9f * Time.deltaTime; Slab.position = Vector3.MoveTowards(Slab.position, to, v * Time.deltaTime); yield return null; }
                Seven.SetEnergyPoint(1f, 1.2f);
            }
            Slab.SetPositionAndRotation(to, r1);
            // it settles with dust and a heavy thud
            if (SlabDust != null) { SlabDust.transform.position = VenusLayout.SlabBridge + Vector3.up * 0.2f; SlabDust.Play(); }
            AudioKit.Play("stone_fall", to, 1f, 0.03f, AudioKit.Bus.Sfx, 60f);
            AudioKit.Play("tremor_rumble", to, 0.35f, 0.05f, AudioKit.Bus.Sfx, 40f);
            if (!skipped) for (float t = 0f; t < 0.5f; t += Time.deltaTime) { Cam.transform.position += UnityEngine.Random.insideUnitSphere * 0.06f * (1f - t / 0.5f); yield return null; }
            SetBridge(true);
            VenusState.CompleteScene("VEN_B03_bridge");
            Log($"bridge (QF10): the fallen slab laid across the collapse by telekinesis{(skipped ? " (skipped: placed)" : "")}; 'Bridge' zone open (top {VenusLayout.BridgeTop:0.00} m), saved for return trips");
            if (!skipped) { yield return Wait(0.9f); yield return Shot("v13c2_b03_slab_down_dust"); yield return Cutscene(false); Seven.Release(); }
        }

        IEnumerator TkStream()
        {
            var em = TkMotes.emission; TkMotes.Play();
            for (float t = 0f; t < 3.8f; t += Time.deltaTime)
            {
                var hand = Seven.Bone(HumanBodyBones.RightHand); var from = hand != null ? hand.position : P7 + Vector3.up * 1.3f;
                for (int i = 0; i < 3; i++) { TkMotes.transform.position = Vector3.Lerp(from, Slab.position, UnityEngine.Random.value); TkMotes.Emit(1); }
                yield return null;
            }
            TkMotes.Stop();
        }

        /// <summary>Round 13 (0k): every tremor is obvious - a deep rumble, a big camera shake (CameraShake), the bike wobbles,
        /// workers stumble and grab hold, water slops out of the vessel and the channel, the grass and trees shiver; indoors
        /// the cube's cage rattles. Logs the measured shake.</summary>
        IEnumerator Tremor(float seconds, float amp = 1f, bool indoors = false)
        {
            AudioKit.Play("tremor_rumble", P7 + Vector3.down * 2f, 1f, 0f, AudioKit.Bus.Sfx, 80f);
            CameraShake.Quake(seconds, amp);
            if (Bike != null) Bike.Wobble = 1f;
            var w = VesselWater != null ? VesselWater.localPosition : Vector3.zero;
            var cage0 = CubeCage != null ? CubeCage.localPosition : Vector3.zero;
            // workers stumble and grab hold (the nearest steadies the vessel)
            var stumbling = new List<(ElarimNpc n, Vector3 p)>();
            if (Workers != null) foreach (var x in Workers) { if (x == null || !x.gameObject.activeInHierarchy || Vector3.Distance(x.transform.position, P7) > 60f) continue; stumbling.Add((x, x.transform.position)); x.Play("lift"); }
            ElarimNpc near = null; float best = 99f;
            foreach (var (x, _) in stumbling) { float d = Vector3.Distance(x.transform.position, VenusLayout.VesselAt); if (d < best) { best = d; near = x; } }
            if (near != null) near.Face(VenusLayout.VesselAt);
            if (!indoors) { AudioKit.Play("vessel_slosh", VenusLayout.VesselAt, 0.9f); AudioKit.Play("water_slosh", new Vector3(32f, 0f, 104f), 1f); }
            // the grass shivers
            var grass = new List<(Material m, float s, float sp)>();
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                var m = r.sharedMaterial; if (m == null || m.shader == null || !m.HasProperty("_WindStrength") || grass.Exists(g => g.m == m)) continue;
                grass.Add((m, m.GetFloat("_WindStrength"), m.GetFloat("_WindSpeed")));
            }
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.Sin(Mathf.Clamp01(t / seconds) * Mathf.PI) * amp;
                if (VesselWater != null) VesselWater.localPosition = w + new Vector3(Mathf.Sin(t * 31f), 0f, Mathf.Cos(t * 27f)) * 0.05f * k + Vector3.up * 0.03f * Mathf.Sin(t * 40f) * k;
                if (CubeCage != null && indoors) CubeCage.localPosition = cage0 + new Vector3(Mathf.Sin(t * 47f), Mathf.Sin(t * 53f) * 0.4f, Mathf.Cos(t * 43f)) * 0.012f * k;
                foreach (var (x, p0) in stumbling) x.transform.position = p0 + new Vector3(Mathf.Sin(t * 9f + p0.x), 0f, Mathf.Cos(t * 7f + p0.z)) * 0.12f * k;
                foreach (var g in grass) { g.m.SetFloat("_WindStrength", Mathf.Lerp(g.s, 0.55f, k)); g.m.SetFloat("_WindSpeed", Mathf.Lerp(g.sp, 3.8f, k)); }
                yield return null;
            }
            if (VesselWater != null) VesselWater.localPosition = w;
            if (CubeCage != null) CubeCage.localPosition = cage0;
            foreach (var (x, p0) in stumbling) { x.transform.position = p0; x.Play(x.PostClip); }
            foreach (var g in grass) { g.m.SetFloat("_WindStrength", g.s); g.m.SetFloat("_WindSpeed", g.sp); }
            Log($"TREMOR (0k): {seconds:0.0} s, camera shake peak {CameraShake.Peak.x * 100f:0} cm / {CameraShake.Peak.y:0.0} deg, bike wobble, {stumbling.Count} Elarim stumbled, {grass.Count} grass materials shivered{(indoors ? ", the cube's cage rattled, stones + dust from the ceiling" : ", water slopped")}");
        }

        /// <summary>Round 13 (0k): 7 and the guardian brace while the temple shakes (her arms out, gaze up; he widens his stance).</summary>
        IEnumerator Brace(float seconds)
        {
            var ik = Seven.gameObject.GetComponent<SevenTableIK>();
            if (Guardian != null) Guardian.Play("lift");
            if (ik != null)
            {
                ik.enabled = true; var f = Flat(Seven.transform.forward).normalized; var r = Vector3.Cross(Vector3.up, f);
                ik.LeftPos = P7 + Vector3.up * 1.05f - r * 0.55f + f * 0.15f; ik.RightPos = P7 + Vector3.up * 1.05f + r * 0.55f + f * 0.15f;
                ik.LeftW = 0.8f; ik.RightW = 0.8f; ik.LookPos = P7 + Vector3.up * 6f + f * 2f; ik.LookW = 0.7f;
            }
            yield return Wait(seconds);
            if (ik != null) { ik.LeftW = 0f; ik.RightW = 0f; ik.LookW = 0f; }
            if (Guardian != null) Guardian.Play(Guardian.PostClip);
            Log("brace (0k): 7 and the guardian braced through the tremor");
        }

        /// <summary>Round 13 (0k, VEN_B03): as she rides toward the damaged stretch a strong tremor hits; a short in-engine cut:
        /// she brakes hard and in front of her the walkway cracks and collapses into the gully (dust, falling stone).</summary>
        /// <summary>QF12 (6): the original cube on its pedestal - active, rendered, where it belongs.</summary>
        void LogCube(string when)
        {
            if (!Shots) return;
            if (Cube == null) { Log($"cube (QF12 6) at {when}: NO CUBE OBJECT"); return; }
            int on = 0, all = 0; foreach (var r in Cube.GetComponentsInChildren<Renderer>(true)) { all++; if (r.enabled && r.gameObject.activeInHierarchy) on++; }
            Log($"cube (QF12 6) at {when}: active {Cube.gameObject.activeInHierarchy}, renderers on {on}/{all}, at {Cube.position} ({Vector3.Distance(Cube.position, VenusLayout.CubeAt):0.00} m from its pedestal), scale {Cube.lossyScale.x:0.00}");
        }

        // ---- QF13 (2): Dan - "it waits until she's on top of it and can't move forward". Cause: the trigger (z > 96, "at or
        // past") was only checked after B02's dialogue had finished, and she rode on meanwhile - by then she was on the
        // walkway, so it fell under her. Now a watcher runs from the start of the ride and fires on distance short of the
        // gully (which crosses the whole valley, so any approach line), early enough for her speed: <= 30 m, or < 3.2 s
        // away + 8 m. She brakes at once, watches it fall, and is never left on or at the broken edge.
        bool _collapseFired, _collapseDone;
        IEnumerator CollapseWatch()
        {
            while (true)
            {
                if (VenusState.SceneDone("VEN_B03")) { _collapseFired = _collapseDone = true; yield break; }
                var p = Bike.Riding ? Bike.transform.position : P7;
                float dz = VenusLayout.CollapseZ0 - p.z, v = Bike.Riding ? Mathf.Max(0f, Bike.Speed) : 0f;
                bool busy = PauseMenu.IsOpen;
                if (!busy && p.z > 40f && (dz < 30f || dz < v * 3.2f + 8f)) break;
                yield return null;
            }
            _collapseFired = true;
            var at = Bike.Riding ? Bike.transform.position : P7;
            Log($"collapse (QF13 2): fired with her {VenusLayout.CollapseZ0 - at.z:0.0} m short of the gully at {(Bike.Riding ? Bike.Speed : 0f):0.0} m/s ({(Bike.Riding ? "riding" : "on foot")}, x {at.x:0.0}) - never with her on it");
            yield return TremorAndCollapse();
            _collapseDone = true;
        }

        IEnumerator TremorAndCollapse()
        {
            // QF13 (2): she brakes at once (the bike stops well short of the edge), then watches it fall
            bool sd = Bike.ScriptDriven; float thr = Bike.ScriptThrottle;
            if (Bike.Riding) { Bike.ScriptDriven = true; Bike.ScriptThrottle = -1f; Bike.ScriptSteer = 0f; }
            StartCoroutine(Tremor(5.0f, 1.1f));
            yield return Wait(0.9f);
            yield return Shot("v12_c02_tremor_vessel");
            yield return Cutscene(true);
            // the cut: from beside the road, the walkway ahead and her braking toward it
            Shoot(new Vector3(37.5f, 3.4f, 106f), new Vector3(30f, -0.4f, 121f), 52f, 0.01f);
            yield return Collapse();
            if (Bike.Riding) { for (float t = 0f; t < 2f && Bike.Speed > 0.3f; t += Time.deltaTime) yield return null; }
            // never left on or at the broken edge: if she ended within 6 m of it (or on it), she glides back to a safe stop
            {
                var p = Bike.Riding ? Bike.transform.position : P7;
                float gap = VenusLayout.CollapseZ0 - p.z;
                if (gap < 6f)
                {
                    var safe = new Vector3(p.x, 0f, VenusLayout.CollapseZ0 - 9f);
                    if (Bike.Riding) { var b0 = Bike.transform.position; for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f) { var q = Vector3.Lerp(b0, safe, Mathf.SmoothStep(0f, 1f, t)); Bike.Place(q, Bike.transform.eulerAngles.y); yield return null; } }
                    else Seven.Place(new Vector3(safe.x, Ground.Height(safe.x, safe.z), safe.z), Vector3.forward);
                    Log($"collapse (QF13 2): she had stopped {gap:0.0} m from the edge - moved back to {VenusLayout.CollapseZ0 - safe.z:0.0} m");
                }
                var e = Bike.Riding ? Bike.transform.position : P7;
                Log($"collapse (QF13 2): she stopped {VenusLayout.CollapseZ0 - e.z:0.0} m short of the broken edge (must be > 5)");
            }
            Bike.ScriptThrottle = thr; Bike.ScriptDriven = sd;
            FollowCamera();
            yield return Cutscene(false);
        }

        /// <summary>QF12 (5): the walkway's middle strip drops into the gully, tips onto the rubble, a dust burst rises.</summary>
        IEnumerator SlabFalls(Vector3 sp0)
        {
            var to = SlabRest; var r1 = SlabRestRot; float v = 0f;
            yield return Wait(0.25f);
            for (float t = 0f; t < 3f && Slab.position.y > to.y + 0.01f; t += Time.deltaTime)
            {
                v += 9.8f * Time.deltaTime;
                var p = Slab.position; p.y = Mathf.Max(to.y, p.y - v * Time.deltaTime);
                float k = Mathf.InverseLerp(sp0.y, to.y, p.y);
                p.x = Mathf.Lerp(sp0.x, to.x, k); p.z = Mathf.Lerp(sp0.z, to.z, k);
                Slab.SetPositionAndRotation(p, Quaternion.Slerp(Quaternion.identity, r1, Mathf.SmoothStep(0f, 1f, k)));
                yield return null;
            }
            Slab.SetPositionAndRotation(to, r1);
            if (SlabDust != null) { SlabDust.transform.position = to + Vector3.up * 0.4f; SlabDust.Play(); }
            AudioKit.Play("stone_rain", to, 0.8f, 0.05f, AudioKit.Bus.Sfx, 60f);
            Log($"collapse (QF12 5): the walkway's middle strip fell into the gully as one broken slab - it lies at {to} (no slab existed before the collapse)");
        }

        IEnumerator Collapse()
        {
            if (WalkwayIntact == null || !WalkwayIntact.gameObject.activeInHierarchy) { Log("collapse: walkway already gone"); yield break; }
            AudioKit.Play("path_collapse", new Vector3(30f, 0f, 122f), 1f, 0f, AudioKit.Bus.Sfx, 90f);
            var pieces = new List<(Transform t, Vector3 p, Quaternion r, float delay, Vector3 spin)>();
            var rnd = new System.Random(7);
            // QF12 (5): the long middle strip of the walkway breaks away as ONE piece - the slab she will lift. Before the
            // collapse it is not anywhere in the world; it shows here in the walkway (the small paving pieces it replaces are
            // hidden) and falls with the rest, then lies broken and dusty at the bottom of the gully.
            bool slabFalls = Slab != null && !VenusState.SceneDone("VEN_B03_bridge");
            Vector3 sp0 = new Vector3(VenusLayout.SlabBridge.x, -VenusLayout.SlabSize.y * 0.5f, VenusLayout.SlabBridge.z);
            if (slabFalls)
            {
                foreach (Transform c in WalkwayIntact) if (Mathf.Abs(c.position.x - VenusLayout.SlabBridge.x) < VenusLayout.SlabSize.z * 0.5f + 0.4f) c.gameObject.SetActive(false);
                Slab.gameObject.SetActive(true); Slab.SetPositionAndRotation(sp0, Quaternion.identity);
            }
            foreach (Transform c in WalkwayIntact)
            {
                if (!c.gameObject.activeSelf) continue;
                float zN = Mathf.InverseLerp(VenusLayout.CollapseZ0, VenusLayout.CollapseZ1, c.position.z);
                float centre = 1f - Mathf.Abs(zN - 0.5f) * 2f;                 // the middle goes first
                pieces.Add((c, c.position, c.rotation, (1f - centre) * 0.9f + (float)rnd.NextDouble() * 0.3f, new Vector3((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f) * 140f));
            }
            // it cracks: a shudder first
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { foreach (var x in pieces) x.t.position = x.p + new Vector3(0f, Mathf.Sin(t * 60f + x.p.x) * 0.03f, 0f); if (slabFalls) Slab.position = sp0 + new Vector3(0f, Mathf.Sin(t * 55f) * 0.03f, 0f); yield return null; }
            if (slabFalls) StartCoroutine(SlabFalls(sp0));
            if (SlabDust != null) { SlabDust.transform.position = VenusLayout.SlabBridge + Vector3.up * 0.2f; SlabDust.Play(); }
            bool shot = false;
            for (float t = 0f; t < 3.2f; t += Time.deltaTime)
            {
                foreach (var x in pieces)
                {
                    float u = Mathf.Max(0f, t - x.delay);
                    if (u <= 0f) continue;
                    x.t.position = x.p + Vector3.down * (4.9f * u * u) + new Vector3(0f, 0f, (x.p.z - 123f) * 0.03f * u);
                    x.t.rotation = x.r * Quaternion.Euler(x.spin * Mathf.Min(u, 1.2f));
                }
                if (!shot && t > 0.45f) { shot = true; StartCoroutine(Shot("v12b_b03_the_walkway_collapses")); }
                yield return null;
            }
            WalkwayIntact.gameObject.SetActive(false);
            foreach (var x in pieces) x.t.SetPositionAndRotation(x.p, x.r);       // a reload shows the gully (it stays hidden)
            yield return Wait(0.6f);
            yield return Shot("v12c_b03_the_gap");
            Log("collapse (0k): the walkway cracked and fell into the gully in front of her");
        }

        // ------------------------------------------------------------------------------------------ QF10: coverage (C03-C05)
        bool _coverage;
        string _lastSpeaker; int _side = 1;
        Vector3 Dir7To(ElarimNpc n) => Flat(n.transform.position - P7).normalized;

        /// <summary>Over 7's shoulder onto an Elarim: low (her shoulder height), looking up at him - he towers.</summary>
        void OtsOnto(ElarimNpc n, float side, float ease = 0.01f)
        {
            var d = Dir7To(n); var r = Vector3.Cross(Vector3.up, d);
            var h = Head7; var cam = h - d * 1.25f + r * 0.6f * side - Vector3.up * 0.32f;
            if (!ChapterCamera.ClearLine(cam, HeadOf(n))) cam = h - d * 1.25f - r * 0.6f * side - Vector3.up * 0.32f;
            Shoot(cam, HeadOf(n) - Vector3.up * 0.25f, 50f, ease);
        }
        /// <summary>Medium on 7, level with her eyes, from his side of the line (the reverse of the over-the-shoulder).</summary>
        void SingleOn7(ElarimNpc n, float side, float ease = 0.01f)
        {
            var d = Dir7To(n); var r = Vector3.Cross(Vector3.up, d); var h = Head7;
            var cam = h + d * 1.7f + r * 0.95f * side;
            if (!ChapterCamera.ClearLine(cam, h)) cam = h + d * 1.7f - r * 0.95f * side;
            Shoot(cam, h - Vector3.up * 0.12f, 40f, ease);
        }
        /// <summary>Low angle on an Elarim (he towers), from in front of him toward 7's side.</summary>
        void LowOn(ElarimNpc n, Vector3 toward, float side, float ease = 0.01f)
        {
            var d = Flat(toward - n.transform.position).normalized; var r = Vector3.Cross(Vector3.up, d);
            var cam = n.transform.position + d * 2.9f + r * 0.9f * side + Vector3.up * 1.05f;
            Shoot(cam, HeadOf(n) - Vector3.up * 0.15f, 46f, ease);
        }
        /// <summary>Medium two-shot across the line between 7 and an Elarim (both whole, him towering).</summary>
        void TwoShot(ElarimNpc n, float side, float dist = 5.4f, float ease = 0.01f)
        {
            var mid = (P7 + n.transform.position) * 0.5f; var d = Dir7To(n); var r = Vector3.Cross(Vector3.up, d);
            var cam = mid + r * side * dist + Vector3.up * 1.5f - d * 0.6f;
            if (!ChapterCamera.ClearLine(cam, mid + Vector3.up * 1.6f)) cam = mid - r * side * dist + Vector3.up * 1.5f - d * 0.6f;
            Shoot(cam, mid + Vector3.up * 1.75f, 50f, ease);
        }

        ElarimNpc NpcOf(string speaker) => speaker == "guardian" ? Guardian : speaker == "attendant" ? Attendant : null;
        ElarimNpc OtherOf() => Guardian;

        /// <summary>Frame the line on its speaker; cut when the speaker changes; the same speaker again -> a slow push-in.</summary>
        void Cover(VoiceBank.Line line)
        {
            string sp = line.Speaker;
            bool change = sp != _lastSpeaker;
            if (change) _side = -_side;
            var g = Guardian;
            if (sp == "seven" || sp == "7")
            {
                if (g == null) return;
                if (change) { if (UnityEngine.Random.value < 0.5f || _lastSpeaker == null) SingleOn7(g, _side); else TwoShot(g, _side); }
                else Cam.Shot(Cam.ShotPos + (Head7 - Cam.ShotPos) * 0.12f, Cam.ShotLook, 3f, false, Cam.Fov * 0.95f);
            }
            else if (sp == "guardian" && g != null)
            {
                if (change) { if (_attendantIn && Attendant != null && line.Id == "VEN_C04_08") GroupShot(); else if (_otsNext) OtsOnto(g, _side); else LowOn(g, P7, _side); _otsNext = !_otsNext; }
                else Cam.Shot(Cam.ShotPos + (HeadOf(g) - Cam.ShotPos) * 0.1f, Cam.ShotLook, 3f, false, Cam.Fov * 0.95f);
            }
            else if (sp == "attendant" && Attendant != null)
            {
                LowOn(Attendant, P7, _side);
            }
            else if (sp == "cat") { /* telepathic from the ship: hold on her (reacting) */ if (change && g != null && Vector3.Distance(P7, g.transform.position) < 12f) SingleOn7(g, _side); }
            _lastSpeaker = sp;
        }
        bool _otsNext = true, _attendantIn;
        void GroupShot()
        {
            var a = Attendant.transform.position; var g = Guardian.transform.position;
            var mid = (a + g + P7) / 3f; var d = Flat(mid - Cam.transform.position).normalized;
            var r = Vector3.Cross(Vector3.up, Flat(g - a).normalized);
            Shoot(mid + r * 7.5f + Vector3.up * 2.0f, mid + Vector3.up * 1.9f, 52f, 0.01f);
        }

        bool CueShowsSpeaker(string cue, string speaker)
        {
            if (cue.Contains("two-shot")) return true;
            if (cue.Contains("toward guardian")) return speaker == "guardian";
            if (cue.Contains("eye close-up")) return speaker == "seven" || speaker == "7";
            return false;   // inserts (the object, the markings, the dust, the cube) hold briefly, then cut to the speaker
        }
        IEnumerator CoverAfter(VoiceBank.Line line, float delay, int serial)
        {
            yield return Wait(delay);
            if (serial == _lineSerial) Cover(line);
        }
        /// <summary>Check runs: a capture of every line's shot, with the speaker's head checked in frame and unblocked.</summary>
        IEnumerator LineShot(VoiceBank.Line line, int serial)
        {
            if (!Shots) yield break;
            yield return Wait(Auto ? 0.75f : 1.8f);
            if (serial != _lineSerial || !Dlg.Busy) yield break;           // Round 12: measured only while the line is still playing
            var cam = Cam.GetComponent<Camera>();
            Vector3? head = line.Speaker == "seven" || line.Speaker == "7" ? Head7 : NpcOf(line.Speaker) != null ? HeadOf(NpcOf(line.Speaker)) : (Vector3?)null;
            string frame = "telepathic (from the ship): framed on her";
            if (head.HasValue)
            {
                var v = cam.WorldToViewportPoint(head.Value);
                bool inFrame = v.z > 0f && v.x > 0.04f && v.x < 0.96f && v.y > 0.04f && v.y < 0.98f;
                bool clear = ChapterCamera.ClearLine(cam.transform.position, head.Value);
                frame = $"speaker head at ({v.x:0.00}, {v.y:0.00}) {(inFrame ? "in frame" : "OUT OF FRAME")}, {(clear ? "clear" : "BLOCKED")}";
                if (!inFrame || !clear) _coverageMisses++;
            }
            _coverageShots++;
            Log($"coverage {line.Id} ({line.Speaker}): camera {cam.transform.position:0.0} fov {cam.fieldOfView:0}; {frame}");
            yield return Cap.Shot($"cov_{line.Id.Substring(4)}_{line.Speaker}");
        }
        int _coverageShots, _coverageMisses;

        // ------------------------------------------------------------------------------------------ C03, C04 (+ B05 lines)
        IEnumerator C03()
        {
            yield return Cutscene(true);
            Seven.Lock(true);
            _coverage = true; _lastSpeaker = null;
            if (Guardian != null) { Guardian.Stop(); Guardian.transform.position = VenusLayout.MeetSpot + Vector3.forward * 1.6f; Guardian.Face(P7); }
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { if (Guardian != null) Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(Guardian.transform.position - P7)), 400f * Time.deltaTime); yield return null; }
            yield return Shot("v14b_c03_guardian_standing_cape");
            if (Shots) StartCoroutine(GuardSeq("1_standing_talking", 4, 0.7f));
            yield return Say("VEN_C03", 1, 2);
            yield return Shot("v15_c03_wide_two_shot");
            // she takes the dodecahedron out and holds it out; the guardian kneels to her level and looks - without taking it
            var ik = Seven.gameObject.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
            ik.enabled = true;
            var f = Flat(Seven.transform.forward).normalized;
            ik.RightPos = _carry != null ? _carry.HipPoint : P7 + Vector3.up; ik.RightW = 1f; ik.Ease = 3f; ik.Alive = false;
            yield return Wait(0.6f);
            if (_carry != null) _carry.Set(CarriedShape.Where.Hand, 0.25f);
            ik.RightPos = P7 + Vector3.up * 1.15f + f * 0.42f; ik.LookPos = ik.RightPos; ik.LookW = 0.7f;
            if (Guardian != null) Guardian.Play("kneel");
            yield return Say("VEN_C03", 3, 3);
            yield return Wait(0.8f);
            if (Guardian != null) TwoShot(Guardian, -_side, 5.2f);
            yield return Wait(0.3f);
            yield return Shot("v16_c03_guardian_kneels_examines_it");
            if (Shots) StartCoroutine(GuardSeq("2_kneeling", 2, 0.6f));
            if (Shots) { var wb = CameraOccluders.WalkBlockers(out int nb); Log($"walk blockers (QF12 7) at C03: {nb} blocked samples in the walkable ground - {wb}"); }
            if (Guardian != null) Log($"guardian kneel (QF12): hips at {Guardian.KneelHipsRatio:0.00} of standing height ({(Guardian.KneelProcedural ? "procedural kneel - the clip did not lower him" : "the kneel clip")}; must be < 0.7)");
            if (Guardian != null) Log($"cape (QF10) kneeling: worst leg contact {Guardian.GetComponent<ElarimCape>()?.WorstDepth * 100f:0.0} cm, swing {Guardian.GetComponent<ElarimCape>()?.MaxAngle:0} deg");
            yield return Say("VEN_C03", 4, 8);
            yield return Wait(0.9f);
            yield return Say("VEN_C03", 9, 10);
            yield return Shot("v17_c03_most_of_a_life");
            // she keeps it (never taken); the guardian rises and leads toward the cube chamber
            if (_carry != null) { ik.RightPos = _carry.HipPoint; yield return Wait(0.5f); _carry.Set(CarriedShape.Where.Pocket, 0.3f); }
            ik.RightW = 0f; ik.LookW = 0f;
            // Round 13 (0e): he turns toward the temple doors and tells her to follow, then walks ahead to the cube chamber
            // (waiting for her if she falls behind)
            if (Guardian != null) { Guardian.Play("idle"); Guardian.Face(new Vector3(30f, 0f, 240f)); }
            if (Shots) StartCoroutine(GuardSeq("3_getting_up", 4, 0.22f));
            yield return Wait(0.6f);
            yield return Say("VEN_C03", 11, 11);                          // "Follow me."
            if (Guardian != null) { StartCoroutine(GuardianLeads()); }
            if (Shots) StartCoroutine(GuardSeq("4_walking_follow_me", 10, 0.18f, 0.5f));
            if (Guardian != null) { TwoShot(Guardian, _side, 6.5f, 1.0f); yield return Wait(1.6f); yield return Shot("v17b_c03_guardian_walks_cape"); Log($"cape (QF10) walking: worst leg contact {Guardian.GetComponent<ElarimCape>()?.WorstDepth * 100f:0.0} cm"); }
            _coverage = false;
            Log($"C03: the guardian examined the dodecahedron without taking it (still hers: {(_carry != null ? _carry.At.ToString() : "-")})");
            if (_wild != null) Log($"wildlife (0i): {_wild.Count} bird / animal / grass sounds so far above ground");
            Complete("VEN_C03", VenusState.Stage.GuardianMet);
            Objective(VenusState.Objectives[(int)VenusState.Stage.GuardianMet]);
            yield return Cutscene(false);
        }

        /// <summary>QF13 (17): the guardian frame by frame - each frame from the player's camera (what is on screen) and the
        /// same instant from his side, with the walk / cape measurements: the clip's ground speed and the playback that
        /// matches it, the planted foot's slide, the cape's top below the shoulders and hem above the ground (now vs the
        /// model), the re-seat shift, the deepest leg contact and the swing.</summary>
        IEnumerator GuardSeq(string beat, int n, float every, float delay = 0f)
        {
            if (!Shots || Guardian == null) yield break;
            if (delay > 0f) yield return Wait(delay);
            for (int i = 0; i < n; i++)
            {
                var g = Guardian.transform; var cape = Guardian.GetComponent<ElarimCape>();
                Log($"guardian (QF13 17) {beat} #{i}: state {(Guardian.Walking ? "walking" : "standing")}, clip ground speed {Guardian.ClipGroundSpeed:0.00} m/s, playback {Guardian.WalkAnimSpeed:0.00} for {Guardian.WalkSpeed:0.0} m/s, planted foot slide {Guardian.FootSlide:0.00} m/s, kneel hips {Guardian.KneelHipsRatio:0.00}, hip sway {Guardian.SwayBefore:0.000} m -> {Guardian.SwayAfter:0.000} m (QF14 7)"
                    + (cape != null ? $"; cape top {cape.TopBelowShoulder:0.00} m below the shoulders (model {cape.RefTopBelowShoulder:0.00}), hem {cape.HemAboveGround:0.00} m above the ground (model {cape.RefHemAboveGround:0.00}), re-seat {cape.SeatShift * 100f:+0;-0} cm, leg contact {cape.WorstDepth * 100f:0.0} cm, swing {cape.MaxAngle:0} deg" : ""));
                yield return Cap.Shot($"qf14_07_{beat}_{i}_player_camera");
                yield return Cap.ShotFrom($"qf14_07_{beat}_{i}_side", g.position + g.right * 8f + g.forward * 1.0f + Vector3.up * 1.9f, g.position + Vector3.up * 1.8f, 38f);
                if (every > 0f) yield return Wait(every);
            }
        }

        /// <summary>Round 13 (0e): after "Follow me." the guardian walks ahead through the temple to the cube chamber; if 7
        /// falls more than 9 m behind he stops, turns and waits until she is within 5 m. Ends at his cube-chamber spot.</summary>
        IEnumerator GuardianLeads()
        {
            var pts = new[] { new Vector3(30f, 0f, 228f), new Vector3(30f, 0f, 248f), VenusLayout.CubeView + new Vector3(1.8f, 0f, 2f) };
            int waits = 0;
            foreach (var p in pts)
            {
                Guardian.WalkTo(p);
                while (Guardian != null && Guardian.Walking)
                {
                    if (VenusState.SceneDone("VEN_C04")) yield break;
                    if (Flat(Guardian.transform.position - P7).magnitude > 9f && Guardian.transform.position.z > P7.z)
                    {
                        Guardian.Stop(); Guardian.Face(P7); waits++;
                        while (Flat(Guardian.transform.position - P7).magnitude > 5f && !VenusState.SceneDone("VEN_C04")) yield return null;
                        Guardian.WalkTo(p);
                    }
                    yield return null;
                }
            }
            if (Guardian != null) Guardian.Face(VenusLayout.CubeAt);
            Log($"guardian leads (0e): reached the cube chamber ahead of her (waited for her {waits}x)");
        }

        IEnumerator C04()
        {
            if (Auto) StartCoroutine(AutoWalkPath(new[] { new Vector3(30f, 0f, 230f), new Vector3(30f, 0f, 248f), VenusLayout.CubeView }));
            yield return Until(() => P7.z > VenusLayout.CubeRoomZ, "the cube chamber (C04)", 120f,
                "Follow the guardian through the great doorway into the temple", new Vector3(30f, 0f, VenusLayout.CubeRoomZ), () => Seven.Place(VenusLayout.CubeView, Vector3.forward));
            yield return Cutscene(true);
            _coverage = true; _lastSpeaker = null;
            Seven.ScriptMove = Vector3.zero;
            yield return WalkSeven(VenusLayout.CubeView, 4f);
            Seven.Lock(true);
            if (Guardian != null) { Guardian.Stop(); Guardian.transform.position = VenusLayout.CubeView + new Vector3(1.8f, 0f, 2f); Guardian.Face(VenusLayout.CubeAt); }
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(VenusLayout.CubeAt - P7)), 400f * Time.deltaTime); yield return null; }
            Shoot(VenusLayout.CubeAt + new Vector3(-4.2f, 3.2f, -8.2f), (VenusLayout.CubeAt + P7) * 0.5f + Vector3.up * 1.0f, 55f);   // wide chamber: both of them and the cube
            LogCube("C04");
            yield return Wait(1.0f);
            yield return Shot("v18_c04_wide_chamber");
            yield return Say("VEN_C04", 1, 6);
            yield return Shot("v19_c04_the_same_marking");
            // the tremor interrupts before she touches the cube; dust falls; the attendant arrives
            StartCoroutine(Tremor(4.2f, 1.25f, true));                     // Round 13 (0k): a big one - stones and dust from the ceiling
            if (CeilingDust != null) { var em = CeilingDust.emission; em.rateOverTime = 420f; CeilingDust.Play(); }
            if (TremorStones != null) TremorStones.Play();
            AudioKit.Play("stone_rain", VenusLayout.CubeAt + Vector3.up * 5f, 1f);
            StartCoroutine(Brace(3.6f));
            AudioKit.Play("dust_fall", VenusLayout.CubeAt + Vector3.up * 5f, 0.8f);
            Cue("falling dust insert");
            yield return Wait(1.1f);
            yield return Shot("v20_c04_falling_dust");
            if (Auto && DemoBoot.R13 == "tremor") { yield return Wait(2.5f); yield return Shot("v20c_c04_after_the_tremor"); Cap.Finish($"TREMOR CHECK (Round 13): B03 walkway collapse + bridge + temple + C04 tremor done; knockdowns 0"); yield break; }
            // they both turn to the doorway; over 7's shoulder as the attendant comes in
            if (Attendant != null) { Attendant.gameObject.SetActive(true); var aTo = VenusLayout.CubeView + new Vector3(-2f, 0f, -1.5f); var aFrom = new Vector3(30f, 0f, 252f); var ad = Flat(aTo - aFrom); Attendant.transform.position = ad.magnitude > 2.8f ? aTo - ad.normalized * 2.6f : aFrom; Attendant.WalkTo(aTo); Log("WALK CUT (QF23 9): C04 - the attendant comes in already near (no long walk while 7 waits)"); }
            if (Guardian != null) Guardian.Face(new Vector3(30f, 0f, 255f));
            {
                var door = new Vector3(30f, 2.4f, 258f); var d = Flat(door - P7).normalized; var r = Vector3.Cross(Vector3.up, d);
                for (float t = 0f; t < 0.5f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(d), 400f * Time.deltaTime); yield return null; }
                Shoot(Head7 - d * 1.3f + r * 0.6f - Vector3.up * 0.2f, door, 48f, 0.01f);
            }
            for (float t = 0f; t < 2.2f && Attendant != null && Attendant.Walking; t += Time.deltaTime)
            {
                if (t > 1.2f && t - Time.deltaTime <= 1.2f) StartCoroutine(Shot("v20b_c04_the_attendant_arrives"));
                Cam.Shot(Cam.ShotPos, HeadOf(Attendant) - Vector3.up * 0.4f, 0.5f, false, 48f);
                yield return null;
            }
            _attendantIn = true;
            if (Attendant != null) { Attendant.Face(P7); }
            yield return Say("VEN_C04", 7, 12);
            // she puts the dodecahedron away (it is in her pocket since C03)
            yield return Say("VEN_C04", 13, 13);
            yield return Say("VEN_C04", 14, 16);
            yield return Shot("v21_c04_ill_find_him");
            Complete("VEN_C04", VenusState.Stage.HelpAccepted);
            // B05's opening lines with the guardian, then the route to the lower gardens opens
            yield return Say("VEN_B05", 1, 3);
            _coverage = false;
            if (Guardian != null) Log($"cape (QF10) standing: worst leg contact {Guardian.GetComponent<ElarimCape>()?.WorstDepth * 100f:0.0} cm; coverage C03-C05 so far: {_coverageShots} line shots, {_coverageMisses} with the speaker out of frame / blocked");
            SetZone("MaintA", true); SetZone("MaintB", true); SetZone("ObsArea", true);
            if (MaintGate != null) MaintGate.SetActive(false);
            Complete("VEN_B05_lines");
            Objective("Ride to the lower gardens");
            yield return Cutscene(false);
        }

        // ------------------------------------------------------------------------------------------ B05 ride + C05
        IEnumerator RideToObservation()
        {
            // back to the parked bike (summoned if she is far from it)
            // (QF10: only where the bike can go - never into the temple; from the cube chamber she walks back to it in the court)
            if (Flat(Bike.transform.position - P7).magnitude > 45f && !Bike.Riding)
            {
                var to = Ground.ClampToArena(P7 + Vector3.right * 2f, 0.8f);
                if (VenusLayout.BikeZone(to)) { Bike.Place(to, 90f); Log("bike summoned to her (she was far from it)"); }
                else Log($"bike stays parked at {Bike.transform.position:0.0} (she is where it cannot go)");
            }
            if (Auto) { if (P7.z > 236f) yield return AutoWalkPath(new[] { new Vector3(30f, 0f, 244f), new Vector3(30f, 0f, 216f) }); yield return AutoWalkPath(new[] { VenusLayout.ParkSpot - Vector3.right * 1.5f }); yield return AutoMount();
                StartCoroutine(AutoRide(new[] { new Vector3(46f, 0f, 199f), new Vector3(76f, 0f, 199f), new Vector3(76.5f, -3f, 175f), new Vector3(76.5f, -6f, 152f), VenusLayout.ObsPark }, () => P7.z < 151f && P7.y < -5f)); }
            yield return Until(() => P7.x > 64f && P7.z < 152f && P7.y < -5f, "the creature observation area (B05 -> C05)", 200f,
                "Back to the bike, then east through the opened gate and down the maintenance path", VenusLayout.ObsPark, () => PlaceRiding(VenusLayout.ObsPark, 180f));
            // she parks and watches; C05 plays in gameplay (movement not locked)
            if (Bike.Riding) { yield return Until(() => Bike.Speed < 1.2f || Auto, "stopped", 10f, "Brake (LT) to stop", VenusLayout.ObsPark, () => { }); Bike.Dismount(true); FollowCamera(); }
            if (Auto) yield return WalkSeven(VenusLayout.ObsSpot, 6f);
            Shoot(VenusLayout.ObsSpot + new Vector3(-2.5f, 2.2f, 3.5f), VenusLayout.BasinCentre + Vector3.up * 2f, 55f, 1.2f);
            yield return Wait(1.0f);
            yield return Shot("v22_c05_observation_area");
            // C05 in gameplay: the camera keeps her in frame with the basin beyond (she speaks; Panther is at the ship)
            _coverage = true; _lastSpeaker = null;
            Cam.Follow();
            yield return SayInGameplay("VEN_C05", 1, 3);
            // the signal pulse: the creatures visibly change direction (toward the temple foundations)
            AudioKit.Play("signal_pulse", VenusLayout.BasinCentre, 1f, 0f, AudioKit.Bus.Sfx, 200f);
            var before = new List<Vector3>();
            if (Worms != null) { foreach (var w in Worms) { before.Add(w.Heading); w.Drive(VenusLayout.Foundations, 14f); } }
            // aim at the nearest creature (the turn must be seen), from just above her at the rail - her in the frame
            var nearW = Worms != null && Worms.Length > 0 ? Worms[0].transform.position : VenusLayout.BasinCentre;
            if (Worms != null) foreach (var w in Worms) if (Flat(w.transform.position - P7).magnitude < Flat(nearW - P7).magnitude) nearW = w.transform.position;
            Shoot(P7 + Vector3.up * 2.6f + Flat(nearW - P7).normalized * -2.4f + Vector3.Cross(Vector3.up, Flat(nearW - P7).normalized) * 0.8f, (nearW + P7 + Vector3.up * 1.4f) * 0.5f, 56f, 1.0f);
            yield return Wait(3.0f);
            float turn = 0f; if (Worms != null) for (int i = 0; i < Worms.Length; i++) turn = Mathf.Max(turn, Vector3.Angle(before[i], Worms[i].Heading));
            Log($"C05 signal pulse: {Worms?.Length ?? 0} creatures turned toward the temple foundations (largest turn {turn:0} deg in 3 s)");
            yield return Shot("v23_c05_creatures_change_direction");
            Cam.Follow();
            yield return SayInGameplay("VEN_C05", 4, 5);
            _coverage = false;
            Complete("VEN_C05", VenusState.Stage.ControlDiscovered);
            Objective(VenusState.Objectives[(int)VenusState.Stage.ControlDiscovered]);
        }

        IEnumerator EndOfPart1()
        {
            yield return Wait(2.0f);
            if (Shots) yield return Shot("v24_root_gate_shut_round12");
            yield return UI.Title("Venus continues…\n(the control chamber comes next)", 0.8f, 2.6f, 0.8f);
            PartOneDone = true;
            if (Auto)
            {
                Cap.Log(VenusState.Summary());
                Cap.Log($"VENUS PART 1 DONE: {VenusState.CurrentId}, reloads {Reloads}, record awards {VenusState.RecordAwards}, bike recoveries {Bike.Recoveries}, gave way {Bike.GiveWays}x, odometer {Bike.Odometer:0} m, coverage {_coverageShots} line shots / {_coverageMisses} misses, hints {Hints}, skips {Skips}");
                Cap.Log(ChapterCamera.Summary());
                Cap.Finish("Venus part 1 check run");
            }
        }
        public bool PartOneDone { get; private set; }
        public bool Covering => _coverage;
        /// <summary>QF10 (6f) check: high wide shots over the valley (no platform edges, undersides or void), then back.</summary>
        public IEnumerator HighWideShots()
        {
            if (!Shots) yield break;
            var keep = _cut; _cut = true;
            var views = new[] { (new Vector3(-30f, 38f, -30f), new Vector3(15f, -2f, 60f)), (new Vector3(70f, 45f, 90f), new Vector3(25f, -2f, 130f)), (new Vector3(10f, 55f, 240f), new Vector3(30f, -2f, 150f)), (new Vector3(110f, 30f, 200f), new Vector3(85f, -8f, 150f)) };
            int i = 0;
            foreach (var (pos, look) in views) { Shoot(pos, look, 55f, 0.01f); yield return null; yield return Shot($"high_wide_{i++}"); }
            _cut = keep; FollowCamera(); Cam.SnapBehind();
        }
        public string Coverage => $"{_coverageShots} line shots, {_coverageMisses} misses";

        // ------------------------------------------------------------------------------------------ watchdog
        IEnumerator Watchdog()
        {
            while (true)
            {
                yield return null;
                if (_cut || PauseMenu.IsOpen || Dlg.Busy) continue;
                if (_objective != null && UI.Objective != ObjectiveText) UI.SetObjective(ObjectiveText);
                if (Bike.Riding && Bike.Stuck && !Auto) { Bike.Recover(); Log("WATCHDOG: the bike was stuck - recovered to the last safe spot"); }
                // fell somewhere with no ground (never expected): back to the last safe spot
                if (P7.y < -45f) { Seven.Place(Seven.LastSafe, Vector3.forward); Log("WATCHDOG: 7 below the world - back to safety"); }
            }
        }

        // ------------------------------------------------------------------------------------------ check-run autopilot
        /// <summary>QF10 (6f): the worn path's centreline between two z values (the autopilot follows the curved route).</summary>
        static Vector3[] RoutePts(float fromZ, float toZ)
        {
            var l = new List<Vector3>();
            foreach (var p in VenusTerrain.Along(0f, VenusTerrain.RouteLength, 9f)) if (p.z > fromZ + 2f && p.z <= toZ + 0.5f) l.Add(p);
            if (l.Count == 0 || l[l.Count - 1].z < toZ - 4f) { VenusTerrain.OnRoute(30f, toZ, out _, out float s, out _, out _); l.Add(VenusTerrain.AtS(s)); }
            return l.ToArray();
        }
        IEnumerator AutoRide(Vector3[] pts, Func<bool> until)
        {
            Bike.ScriptDriven = true;
            int i = 0;
            while (i < pts.Length && !until() && !_autoRideStop)
            {
                if (!Bike.Riding) { yield return null; continue; }
                var to = Flat(pts[i] - Bike.transform.position);
                if (to.magnitude < 3.5f) { i++; continue; }
                float ang = Vector3.SignedAngle(Flat(Bike.transform.forward), to, Vector3.up);
                Bike.ScriptSteer = Mathf.Clamp(ang / 35f, -1f, 1f);
                Bike.ScriptThrottle = Mathf.Abs(ang) > 50f ? 0.25f : 0.85f;
                yield return null;
            }
            Bike.ScriptThrottle = -1f; Bike.ScriptSteer = 0f;
            for (float t = 0f; t < 2f && Bike.Speed > 0.3f; t += Time.deltaTime) yield return null;
            Bike.ScriptThrottle = 0f;
        }

        IEnumerator AutoGetOff()
        {
            Bike.ScriptThrottle = -1f;
            for (float t = 0f; t < 3f && Bike.Speed > 0.5f; t += Time.deltaTime) yield return null;
            if (Bike.Riding) { Bike.Dismount(true); FollowCamera(); }
        }

        IEnumerator AutoMount()
        {
            if (Bike.Riding) yield break;
            yield return AutoWalkPath(new[] { Bike.transform.position - Bike.transform.right * 1.35f });
            Bike.Mount(); FollowCamera();
            yield return null;
        }

        IEnumerator AutoWalkPath(Vector3[] pts)
        {
            Seven.ScriptDriven = true;
            foreach (var p in pts)
            {
                for (float t0 = Time.time; Dist7(p) > 0.4f && Time.time - t0 < 25f;)
                {
                    if (Seven.Current == ChapterSeven.Mode.Free) Seven.ScriptMove = Flat(p - P7).normalized; else Seven.ScriptMove = Vector3.zero;
                    yield return null;
                }
                if (Dist7(p) > 0.4f) { Cap.Soft++; Cap.Log($"SOFT-LOCK? could not walk to {p} (at {P7})"); }
            }
            Seven.ScriptMove = Vector3.zero;
        }
    }
}
