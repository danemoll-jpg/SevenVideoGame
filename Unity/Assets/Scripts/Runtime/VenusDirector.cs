using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// Round 11 — Chapter 2 "Venus", part 1 (Docs/CHAPTER2_VENUS.md): the descent under the clouds, C01 arrival,
    /// B01 the bike + the cat's name (Panther) and first size change, B02 the first ride, B03 / C02 the damaged route and
    /// the tremor (dismount, on-foot detour round the collapsed walkway), B04 parking at the temple, C03 the guardian, C04
    /// the cube and the interruption, B05 the ride to the lower gardens, C05 the signal - to VEN_S04.
    /// Reuses Chapter 1's systems: 7 (walk, traversal, prompts), the follow camera, the dialogue player + subtitles +
    /// telepathy treatment (lines and labels from Resources/Venus/venus_dialogue.json), the pause menu, the capture.
    /// Progression + persistence: <see cref="VenusState"/>. Every scene applies its result when it completes, watched or
    /// skipped (skipping = the same result, once); an interrupted scene restarts from its beginning; reloading restores
    /// the place for the stage reached. Triggers fire at or past their place; a watchdog re-shows the objective and a
    /// soft-lock guard (check runs) logs any step that takes too long.
    /// </summary>
    public class VenusDirector : MonoBehaviour
    {
        public ChapterSeven Seven;
        public ChapterCat Cat;
        public ChapterCamera Cam;
        public ChapterInput Input;
        public DialoguePlayer Dlg;
        public ChapterUI UI;
        public ControlsLegend Legend;
        public HoverBike Bike;
        public Transform Saucer, Hatch, Ramp, Vessel, VesselWater, Cube, CubeCage;
        public ElarimNpc Guardian, Attendant;
        public ElarimNpc[] Workers;
        public WormCreature[] Worms;
        public ParticleSystem CeilingDust;
        public Transform[] Flyers;
        public ElarimShape Shape;
        public GameObject MaintGate;          // blocks the maintenance path until C04
        public Atmosphere Atmo;

        public static VenusDirector I { get; private set; }
        bool Auto => DemoBoot.CaptureDir != null && !DemoBoot.PlayerSim;
        ChapterCapture Cap => ChapterCapture.I;
        string _objective;
        bool _cut;
        CarriedShape _carry;
        static readonly HashSet<string> _taught = new HashSet<string>();
        public static int Reloads;        // check run: reloads done (each committed scene once)

        // ------------------------------------------------------------------------------------------ setup
        void Start()
        {
            I = this;
            VenusLayout.UseTraversal();
            Legend.SetLayout(ControlsLegend.Layout.Chapter);
            Legend.HidePanels = true;
            if (Auto && Reloads == 0 && !DemoBoot.Continue) VenusState.ResetForTest();
            if (Auto)
            {
                ChapterCapture.Ensure(); Cap.Attach(Cam.GetComponent<Camera>());
                Seven.ScriptDriven = true; Dlg.AutoSkipAfter = DemoBoot.VenusSkip ? 0f : 0.9f;
                if (Reloads == 0) Cap.Log($"Venus check run{(DemoBoot.VenusSkip ? " (every line skipped)" : "")}: {VenusState.Summary()}");
            }
            ChapterCamera.CatRoot = Cat != null ? Cat.transform : null;
            CameraOccluders.Build(Seven.transform, Cat != null ? Cat.transform : null, Bike != null ? Bike.transform : null, Shape != null ? Shape.transform : null);
            if (Shape != null) { _carry = CarriedShape.Attach(Seven.gameObject, Shape.transform); _carry.Set(CarriedShape.Where.Pocket, 0f); }
            if (Cat != null) Cat.LeashAllowed = () => false;            // the cat stays at the ship on Venus
            Bike.Rider = Seven; Bike.Input = Input;
            MusicSystem.Play("venus_gardens", 3f);
            AudioKit.Loop("amb_venus_gardens", null, null, 1f, AudioKit.Bus.Ambience);
            StartCoroutine(Flow());
            StartCoroutine(Watchdog());
        }

        // ------------------------------------------------------------------------------------------ helpers
        IEnumerator Wait(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        float Dist7(Vector3 p) => Flat(Seven.transform.position - p).magnitude;
        Vector3 P7 => Seven.transform.position;
        void Objective(string s) { _objective = s; UI.SetObjective(s); if (Auto) Cap.Log("objective: " + s); }
        void Log(string s) { Debug.Log("[Seven] " + s); if (Auto || DemoBoot.PlayerSim) Cap?.Log(s); }
        IEnumerator Shot(string name) { if (!Auto) yield break; yield return Cap.Shot(name); }

        IEnumerator Until(Func<bool> cond, string what, float autoTimeout = 90f)
        {
            float t0 = Time.time;
            while (!cond())
            {
                if (Auto && Time.time - t0 > autoTimeout) { Cap.Soft++; Cap.Log($"SOFT-LOCK? waiting for '{what}' {autoTimeout:0}s (7 at {P7}, {(Bike.Riding ? "riding" : "on foot")})"); yield break; }
                yield return null;
            }
            if (Auto) Cap.Log($"ok: {what} ({Time.time - t0:0.0}s)");
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
            if (Bike.Riding) { Cam.Target = Bike.transform; Cam.Distance = 5.6f; Cam.LookHeight = 1.5f; Cam.AutoBehind = true; }
            else { Cam.Target = Seven.transform; Cam.Distance = 3.8f; Cam.LookHeight = 1.45f; Cam.AutoBehind = Auto; }
            Cam.Follow();
        }

        /// <summary>Lines of one scene as data (labels, telepathy, optional voice files; subtitles only without them).</summary>
        List<VoiceBank.Line> Lines(string scene, int from = 1, int to = 99)
        {
            var l = new List<VoiceBank.Line>();
            foreach (var x in VenusDialogue.SceneLines(scene))
            {
                int n = int.Parse(x.id.Substring(x.id.Length - 2));
                if (n < from || n > to) continue;
                l.Add(new VoiceBank.Line
                {
                    Id = x.id, Speaker = x.speaker == "cat" ? "cat" : x.speaker, Text = x.text, Scene = 200, PauseAfter = 0.45f,
                    Clip = Resources.Load<AudioClip>("VenusVoice/" + x.id), Label = x.label,
                });
            }
            return l;
        }

        /// <summary>Play lines; the camera cue on a line (if any) runs first - presentation only, never waited on.</summary>
        IEnumerator Say(string scene, int from = 1, int to = 99)
        {
            var ls = Lines(scene, from, to);
            foreach (var line in ls)
            {
                var cue = VenusDialogue.Get(line.Id)?.cue;
                if (!string.IsNullOrEmpty(cue)) try { Cue(cue); } catch (Exception e) { Log($"camera cue '{cue}' skipped ({e.Message})"); }
                yield return Dlg.PlayLines(new List<VoiceBank.Line> { line }, false);
            }
        }

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
                case "wide two-shot at temple entrance": Shoot(VenusLayout.MeetSpot + new Vector3(7f, 2.4f, -5f), VenusLayout.MeetSpot + new Vector3(0f, 1.8f, 2f), 50f); break;
                case "insert of dodecahedron": if (Shape != null) Shoot(Shape.transform.position + r * 0.6f + Vector3.up * 0.15f - f * 0.2f, Shape.transform.position, 34f); break;
                case "over 7's shoulder toward guardian": if (Guardian != null) Shoot(h - f * 1.4f + r * 0.9f - Vector3.up * 0.3f, Guardian.transform.position + Vector3.up * 2.4f, 52f); break;   // low: he towers over her
                case "close view of object between them": if (Shape != null) Shoot(Shape.transform.position + r * 1.0f + Vector3.up * 0.25f, Shape.transform.position, 36f); break;
                case "over 7's shoulder toward cube": Shoot(h - f * 1.5f + r * 0.8f, VenusLayout.CubeAt, 44f); break;
                case "matching inserts of related markings": Shoot(VenusLayout.CubeAt + new Vector3(0.9f, 0.3f, -1.2f), VenusLayout.CubeAt, 30f); break;
                case "brief eye close-up": Shoot(h + f * 0.9f + r * 0.25f + Vector3.up * 0.05f, h + Vector3.up * 0.1f, 30f, 0.6f); break;
                case "falling dust insert": Shoot(VenusLayout.CubeAt + new Vector3(-3f, 2.5f, -4f), VenusLayout.CubeAt + Vector3.up * 3f, 50f); break;
                case "wide - saucer and landscape": Shoot(VenusLayout.SaucerAt + new Vector3(9f, 4f, 12f), VenusLayout.SaucerAt + new Vector3(0f, 1.5f, 3f), 55f); break;
                case "low three-quarter on the bike": Shoot(Bike.transform.position + Bike.transform.forward * 2.6f + Bike.transform.right * 1.6f + Vector3.up * 0.45f, Bike.transform.position + Vector3.up * 0.6f, 44f); break;
                case "side medium - 7 mounting": Shoot(Bike.transform.position + Bike.transform.right * 3.2f + Vector3.up * 1.3f, Bike.transform.position + Vector3.up * 0.9f, 46f); break;
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
            if (!VenusState.SceneDone("VEN_C01")) { yield return Arrival(); if (ReloadCheck("VEN_C01")) yield break; }
            if (!VenusState.SceneDone("VEN_B01")) { yield return B01(); if (ReloadCheck("VEN_B01")) yield break; }
            if (!VenusState.SceneDone("VEN_C03")) { yield return RideToTemple(); yield return C03(); if (ReloadCheck("VEN_C03")) yield break; }
            if (!VenusState.SceneDone("VEN_C04")) { yield return C04(); if (ReloadCheck("VEN_C04")) yield break; }
            if (!VenusState.SceneDone("VEN_C05")) { yield return RideToObservation(); if (ReloadCheck("VEN_C05")) yield break; }
            yield return EndOfPart1();
        }

        /// <summary>Put everything where the save says (a reload / Continue restores the place for the stage reached).</summary>
        void Restore()
        {
            var st = VenusState.Current;
            bool c01 = VenusState.SceneDone("VEN_C01"), b01 = VenusState.SceneDone("VEN_B01");
            SetZone("MaintA", VenusState.AtLeast(VenusState.Stage.HelpAccepted)); SetZone("MaintB", VenusState.AtLeast(VenusState.Stage.HelpAccepted)); SetZone("ObsArea", VenusState.AtLeast(VenusState.Stage.HelpAccepted));
            if (MaintGate != null) MaintGate.SetActive(!VenusState.AtLeast(VenusState.Stage.HelpAccepted));
            if (Saucer != null) Saucer.position = VenusLayout.SaucerAt + Vector3.up * (c01 ? VenusLayout.SaucerHover : 60f);
            // the bike: in the hatch until B01, then where it was left
            if (!b01) Bike.Place(VenusLayout.HatchAt + Vector3.back * 0.4f, 0f);
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
            if (!c01) at = VenusLayout.RampFoot;
            else if (!b01) at = VenusLayout.RampFoot + Vector3.forward * 1f;
            else if (!VenusState.SceneDone("VEN_C03")) at = VenusState.BikeKnown ? VenusState.BikeAt - Vector3.right * 1.2f : VenusLayout.BikeUnpacked - Vector3.right * 1.2f;
            else if (!VenusState.SceneDone("VEN_C04")) { at = VenusLayout.MeetSpot + Vector3.forward * 4f; }
            else if (!VenusState.SceneDone("VEN_C05")) { at = VenusLayout.ParkSpot - Vector3.right * 1.6f; }
            else { at = VenusLayout.ObsSpot; face = Vector3.right; }
            Seven.Place(at, face);
            if (b01 && VenusState.Riding && !VenusState.SceneDone("VEN_C03")) { Bike.Mount(); }
            FollowCamera(); Cam.SnapBehind();
            if (VenusState.Objective != null && c01) Objective(VenusState.Objective);
            if (VenusState.SceneDone("VEN_C03") && Guardian != null) { Guardian.transform.position = VenusLayout.CubeView + new Vector3(1.8f, 0f, 2f); }
            Log($"restore: {VenusState.Summary()} -> 7 at {at}");
        }

        void SetZone(string name, bool on) { var z = Ground.FindZone(name); if (z != null) z.Enabled = on; }

        // ------------------------------------------------------------------------------------------ the descent + C01
        IEnumerator Arrival()
        {
            yield return Cutscene(true);
            Seven.gameObject.SetActive(false); if (Cat != null) Cat.gameObject.SetActive(false);
            UI.SetFade(1f);
            StartCoroutine(UI.Title("CHAPTER 2  —  VENUS", 1.0f, 2.2f, 1.0f));
            var top = VenusLayout.SaucerAt + Vector3.up * 70f; var land = VenusLayout.SaucerAt + Vector3.up * VenusLayout.SaucerHover;
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
            Seven.gameObject.SetActive(true); if (Cat != null) Cat.gameObject.SetActive(true);
            Seven.Place(VenusLayout.RampFoot + Vector3.back * 2.2f, Vector3.forward);
            Seven.Lock(true);
            if (Cat != null) { Cat.Place(VenusLayout.RampFoot + new Vector3(-0.8f, 0f, -1.8f), Vector3.forward); Cat.GoTo(VenusLayout.RampFoot + new Vector3(-1.0f, 0f, 0.4f), Cat.StrollSpeed, VenusLayout.VistaSpot + Vector3.forward * 20f); }
            Shoot(VenusLayout.RampFoot + new Vector3(3.5f, 1.6f, 4.5f), VenusLayout.RampFoot + new Vector3(0f, 1.2f, -1f), 50f, 0.01f);
            yield return WalkSeven(VenusLayout.RampFoot + Vector3.forward * 1.2f, 4f);
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
            Complete("VEN_C01", VenusState.Stage.Landed);
            yield return Cutscene(false);
        }

        IEnumerator CatTracksFlyers(float seconds)
        {
            if (Cat == null || Flyers == null || Flyers.Length == 0) yield break;
            var anim = Cat.GetComponentInChildren<Animator>(); Transform head = null;
            foreach (var t in Cat.GetComponentsInChildren<Transform>()) if (t.name == "head") { head = t; break; }
            if (head == null) yield break;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                var f = Flyers[0]; var d = (f.position - head.position).normalized;
                head.rotation = Quaternion.Slerp(head.rotation, Quaternion.LookRotation(d) * Quaternion.Euler(0f, 0f, 0f), 0.35f);
                yield return new WaitForEndOfFrame();
            }
        }

        IEnumerator WalkSeven(Vector3 to, float timeout)
        {
            bool sd = Seven.ScriptDriven; Seven.ScriptDriven = true; Seven.Lock(false);
            for (float t0 = Time.time; Dist7(to) > 0.3f && Time.time - t0 < timeout;) { Seven.ScriptMove = Flat(to - P7).normalized * 0.8f; yield return null; }
            Seven.ScriptMove = Vector3.zero;
            if (Dist7(to) > 0.3f) Seven.Place(to, Flat(to - P7));
            Seven.ScriptDriven = sd;
        }

        // ------------------------------------------------------------------------------------------ B01: the bike, Panther
        public const float PantherScale = 2.7f;
        IEnumerator B01()
        {
            yield return Cutscene(true);
            Seven.Lock(true);
            yield return Say("VEN_B01", 1, 2);                             // "That's a walk." / "You have a vehicle."
            // she looks back toward the ship
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(VenusLayout.SaucerAt - P7)), 300f * Time.deltaTime); yield return null; }
            yield return Say("VEN_B01", 3, 4);
            // the storage hatch opens: the bike, hovering out of the bay
            Bike.gameObject.SetActive(true); Bike.Place(VenusLayout.HatchAt, 0f);
            AudioKit.Play("hatch_iris", VenusLayout.HatchAt, 0.9f);
            if (Hatch != null) StartCoroutine(OpenHatch());
            Shoot(VenusLayout.HatchAt + new Vector3(4.5f, 1.4f, 4f), VenusLayout.HatchAt + Vector3.up * 0.7f, 48f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.2f) { Bike.Place(Vector3.Lerp(VenusLayout.HatchAt, VenusLayout.BikeUnpacked, Mathf.SmoothStep(0f, 1f, t)), 0f); yield return null; }
            yield return Shot("v06_b01_bike_out_of_the_hatch");
            // she walks over and slides her hands into the grips; the vents hum, lifting it
            if (Cat != null) Cat.GoTo(VenusLayout.RampFoot + new Vector3(-1.4f, 0f, -0.2f), Cat.StrollSpeed, VenusLayout.BikeUnpacked);
            yield return WalkSeven(VenusLayout.BikeUnpacked - Vector3.right * 1.0f, 5f);
            Seven.Lock(true);
            Cue("low three-quarter on the bike");
            yield return Wait(0.8f);
            yield return Shot("v07_b01_low_three_quarter_bike");
            yield return Say("VEN_B01", 5, 5);                             // "I'll watch the ship."
            // she starts to mount, pauses, looks back at him
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { if (Cat != null) Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(Cat.transform.position - P7)), 300f * Time.deltaTime); yield return null; }
            if (Cat != null) Shoot((P7 + Cat.transform.position) * 0.5f + Vector3.Cross(Vector3.up, Flat(Cat.transform.position - P7).normalized) * 4.2f + Vector3.up * 1.4f, (P7 + Cat.transform.position) * 0.5f + Vector3.up * 0.7f, 50f);
            yield return Say("VEN_B01", 6, 6);                             // "What should I call you?"
            // Panther: the name, and the stretch - he grows to a large, powerful cat, paws on the ground
            if (Cat != null) StartCoroutine(PantherStretch());
            yield return Say("VEN_B01", 7, 7);
            yield return Wait(0.9f);                                         // a beat: her gaze travels over the (now not small) cat
            yield return Shot("v08_b01_panther_stretch");
            yield return Say("VEN_B01", 8, 9);
            yield return Wait(1.2f);
            yield return Shot("v09_b01_panther_lies_beside_the_ramp");
            yield return Say("VEN_B01", 10, 11);
            // she settles into the low saddle, leans toward the fairing; a glance back; then riding
            Cue("side medium - 7 mounting");
            Bike.Mount();
            yield return Wait(1.0f);
            yield return Shot("v10_b01_mounted_hands_in_the_grips");
            Log($"bike grips: left hand {(Seven.Bone(HumanBodyBones.LeftHand) != null ? Vector3.Distance(Seven.Bone(HumanBodyBones.LeftHand).position, Bike.GripL.position) : -1f):0.00} m, right hand {(Seven.Bone(HumanBodyBones.RightHand) != null ? Vector3.Distance(Seven.Bone(HumanBodyBones.RightHand).position, Bike.GripR.position) : -1f):0.00} m from the grips");
            Complete("VEN_B01", VenusState.Stage.Landed);
            Objective(VenusState.Objectives[(int)VenusState.Stage.Landed]);  // "Ride to the Elarim temple"
            yield return Cutscene(false);
            StartCoroutine(TeachRiding());
        }

        IEnumerator OpenHatch()
        {
            var a = Hatch.localRotation; var b = a * Quaternion.Euler(-80f, 0f, 0f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f) { Hatch.localRotation = Quaternion.Slerp(a, b, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
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
            yield return Wait(0.6f);
            Cat.GoTo(VenusLayout.PantherLie, Cat.StrollSpeed * 1.3f, VenusLayout.BikeUnpacked, "lie_down");
            Log($"Panther: stretched and grew to x{PantherScale} (uniform), lies beside the ramp; label 'Panther' from VEN_B01_07");
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

        // ------------------------------------------------------------------------------------------ riding prompts
        void Update()
        {
            if (PauseMenu.IsOpen || _cut || Dlg.Busy && !_gameplayLines) { if (_cut) Legend.Context(ControlsLegend.Btn.Interact, null); return; }
            if (!VenusState.SceneDone("VEN_B01")) { Legend.Context(ControlsLegend.Btn.Interact, null); return; }
            bool canRide = !_noRide;
            if (Bike.Riding)
            {
                bool stopped = Bike.Speed < 1.2f;
                Legend.Context(ControlsLegend.Btn.Interact, stopped ? "Get off" : null);
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
                Legend.Context(Seven.Context.StartsWith("jump") ? ControlsLegend.Btn.Jump : ControlsLegend.Btn.Interact, Seven.Context.Substring(c + 1));
            }
            else Legend.Context(ControlsLegend.Btn.Interact, null);
        }
        bool _gameplayLines, _noRide;

        IEnumerator SayInGameplay(string scene, int from = 1, int to = 99)
        {
            _gameplayLines = true;
            yield return Dlg.PlayLines(Lines(scene, from, to), false);
            _gameplayLines = false;
        }

        // ------------------------------------------------------------------------------------------ B02, B03 + C02, B04
        IEnumerator RideToTemple()
        {
            if (Auto) StartCoroutine(AutoRide(new[] { new Vector3(0f, 0f, 20f), new Vector3(0f, 0f, 46f), new Vector3(14f, 0f, 50f), new Vector3(29f, 0f, 52f), new Vector3(30f, 0f, 80f), new Vector3(30f, 0f, 108f) }, () => VenusState.SceneDone("VEN_B03") || P7.z > 104f));
            // B02 at the long open bend (in gameplay, the normal riding camera)
            if (!VenusState.SceneDone("VEN_B02"))
            {
                yield return Until(() => P7.z > VenusLayout.BendZ - 6f && P7.x > 6f || P7.z > 60f, "the long open bend (B02)", 120f);
                yield return Shot("v11_b02_the_long_open_bend");
                yield return SayInGameplay("VEN_B02", 1, 3);
                yield return Wait(1.2f);                                    // a short pause
                yield return SayInGameplay("VEN_B02", 4, 4);
                Complete("VEN_B02");
            }
            // B03 + C02: workers signal to slow down, water across the route, the tremor (movement never locked)
            if (!VenusState.SceneDone("VEN_B03"))
            {
                yield return Until(() => P7.z > VenusLayout.TremorZ, "the damaged route (B03)", 120f);
                StartCoroutine(Tremor(3.2f));
                yield return Shot("v12_c02_tremor_vessel");
                yield return SayInGameplay("VEN_C02", 1, 2);
                Complete("VEN_C02"); Complete("VEN_B03");
            }
            // the collapsed walkway: brake in the stopping area, get off, find a way round on foot
            if (P7.z < VenusLayout.CollapseZ1)
            {
                Objective("Find a way round the collapse");
                yield return Until(() => !Bike.Riding || P7.z > VenusLayout.CollapseZ1, "off the bike at the collapse", 90f);
                if (Auto) { yield return AutoGetOff(); StartCoroutine(AutoDetour()); }
                yield return Shot("v13_b03_collapsed_walkway");
                yield return Until(() => P7.z > VenusLayout.CollapseZ1 + 1.5f && P7.x > 25.5f && P7.y < 0.4f, "round the collapse on foot (mantle, jump, down the ramp)", 120f);
                // the bike was brought round on the service path (summoned) - waiting a step ahead
                if (Flat(Bike.transform.position - P7).magnitude > 10f) { Bike.Place(VenusLayout.BikeBeyond + Vector3.forward * 2f, 0f); Log("bike summoned past the collapse"); }
                Objective(VenusState.Objectives[(int)VenusState.Stage.Landed]);
            }
            // on to the temple court, riding (or walking)
            if (Auto) { yield return AutoMount(); StartCoroutine(AutoRide(new[] { new Vector3(30f, 0f, 160f), new Vector3(30f, 0f, 186f), new Vector3(34f, 0f, 196f) }, () => P7.z > VenusLayout.CourtZ + 3f)); }
            yield return Until(() => P7.z > VenusLayout.CourtZ, "the temple court (B04)", 150f);
            // B04: she parks beside the court, gets off; the bike settles into a low idle hover as the guardian approaches
            yield return Cutscene(true);
            if (Bike.Riding) { Bike.Dismount(true); }
            Bike.Place(VenusLayout.ParkSpot, 180f);
            Seven.Place(VenusLayout.ParkSpot - Vector3.right * 1.6f + Vector3.forward * 1f, Vector3.forward);
            Seven.Lock(true);
            VenusState.SaveBike(Bike.transform.position, Bike.transform.eulerAngles.y, false);
            Shoot(VenusLayout.ParkSpot + new Vector3(10f, 4f, -9f), VenusLayout.MeetSpot + Vector3.up * 2f, 55f);
            if (Guardian != null) Guardian.WalkTo(VenusLayout.MeetSpot + Vector3.forward * 1.6f);
            yield return WalkSeven(VenusLayout.MeetSpot - Vector3.forward * 1.4f, 10f);
            Seven.Lock(true);
            yield return Shot("v14_b04_temple_arrival_wide");
            Complete("VEN_B04");
        }

        IEnumerator Tremor(float seconds)
        {
            AudioKit.Play("tremor_rumble", P7, 0.9f);
            var w = VesselWater != null ? VesselWater.localPosition : Vector3.zero;
            // a worker steadies the vessel
            ElarimNpc near = null; float best = 99f;
            if (Workers != null) foreach (var x in Workers) { if (x == null) continue; float d = Vector3.Distance(x.transform.position, VenusLayout.VesselAt); if (d < best) { best = d; near = x; } }
            if (near != null) { near.Face(VenusLayout.VesselAt); near.Play("lift"); }
            AudioKit.Play("vessel_slosh", VenusLayout.VesselAt, 0.8f);
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / seconds * Mathf.PI);
                Cam.transform.position += UnityEngine.Random.insideUnitSphere * 0.04f * k;
                if (VesselWater != null) VesselWater.localPosition = w + new Vector3(Mathf.Sin(t * 31f), 0f, Mathf.Cos(t * 27f)) * 0.012f * k + Vector3.up * 0.006f * Mathf.Sin(t * 40f) * k;
                yield return null;
            }
            if (VesselWater != null) VesselWater.localPosition = w;
            if (near != null) near.Play(near.PostClip);
        }

        // ------------------------------------------------------------------------------------------ C03, C04 (+ B05 lines)
        IEnumerator C03()
        {
            yield return Cutscene(true);
            Seven.Lock(true);
            if (Guardian != null) { Guardian.Stop(); Guardian.transform.position = VenusLayout.MeetSpot + Vector3.forward * 1.6f; Guardian.Face(P7); }
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { if (Guardian != null) Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(Guardian.transform.position - P7)), 400f * Time.deltaTime); yield return null; }
            yield return Say("VEN_C03", 1, 2);
            yield return Shot("v15_c03_wide_two_shot");
            // she takes the dodecahedron out and holds it out; the guardian kneels to her level and looks - without taking it
            var ik = Seven.gameObject.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
            var f = Flat(Seven.transform.forward).normalized;
            ik.RightPos = _carry != null ? _carry.HipPoint : P7 + Vector3.up; ik.RightW = 1f; ik.Ease = 3f; ik.Alive = false;
            yield return Wait(0.6f);
            if (_carry != null) _carry.Set(CarriedShape.Where.Hand, 0.25f);
            ik.RightPos = P7 + Vector3.up * 1.15f + f * 0.42f; ik.LookPos = ik.RightPos; ik.LookW = 0.7f;
            if (Guardian != null) Guardian.Play("kneel");
            yield return Say("VEN_C03", 3, 3);
            yield return Wait(0.8f);
            yield return Shot("v16_c03_guardian_kneels_examines_it");
            yield return Say("VEN_C03", 4, 8);
            yield return Wait(0.9f);
            yield return Say("VEN_C03", 9, 10);
            yield return Shot("v17_c03_most_of_a_life");
            // she keeps it (never taken); the guardian rises and leads toward the cube chamber
            if (_carry != null) { ik.RightPos = _carry.HipPoint; yield return Wait(0.5f); _carry.Set(CarriedShape.Where.Pocket, 0.3f); }
            ik.RightW = 0f; ik.LookW = 0f;
            if (Guardian != null) { Guardian.Play("idle"); Guardian.WalkTo(VenusLayout.CubeView + new Vector3(1.8f, 0f, 2f)); }
            Log($"C03: the guardian examined the dodecahedron without taking it (still hers: {(_carry != null ? _carry.At.ToString() : "-")})");
            Complete("VEN_C03", VenusState.Stage.GuardianMet);
            Objective(VenusState.Objectives[(int)VenusState.Stage.GuardianMet]);
            yield return Cutscene(false);
        }

        IEnumerator C04()
        {
            if (Auto) StartCoroutine(AutoWalkPath(new[] { new Vector3(30f, 0f, 230f), new Vector3(30f, 0f, 248f), VenusLayout.CubeView }));
            yield return Until(() => P7.z > VenusLayout.CubeRoomZ, "the cube chamber (C04)", 120f);
            yield return Cutscene(true);
            Seven.ScriptMove = Vector3.zero;
            yield return WalkSeven(VenusLayout.CubeView, 4f);
            Seven.Lock(true);
            if (Guardian != null) { Guardian.Stop(); Guardian.transform.position = VenusLayout.CubeView + new Vector3(1.8f, 0f, 2f); Guardian.Face(VenusLayout.CubeAt); }
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(VenusLayout.CubeAt - P7)), 400f * Time.deltaTime); yield return null; }
            Shoot(VenusLayout.CubeAt + new Vector3(-6f, 3.5f, -6f), VenusLayout.CubeAt + Vector3.down * 0.3f, 55f);   // wide chamber
            yield return Wait(1.0f);
            yield return Shot("v18_c04_wide_chamber");
            yield return Say("VEN_C04", 1, 6);
            yield return Shot("v19_c04_the_same_marking");
            // the tremor interrupts before she touches the cube; dust falls; the attendant arrives
            StartCoroutine(Tremor(3.0f));
            if (CeilingDust != null) CeilingDust.Play();
            AudioKit.Play("dust_fall", VenusLayout.CubeAt + Vector3.up * 5f, 0.8f);
            Cue("falling dust insert");
            yield return Wait(1.4f);
            yield return Shot("v20_c04_falling_dust");
            if (Attendant != null) { Attendant.gameObject.SetActive(true); Attendant.transform.position = new Vector3(30f, 0f, 250f); Attendant.WalkTo(VenusLayout.CubeView + new Vector3(-2f, 0f, -1.5f)); }
            yield return Wait(2.2f);
            yield return Say("VEN_C04", 7, 12);
            // she puts the dodecahedron away
            yield return Say("VEN_C04", 13, 13);
            yield return Say("VEN_C04", 14, 16);
            yield return Shot("v21_c04_ill_find_him");
            Complete("VEN_C04", VenusState.Stage.HelpAccepted);
            // B05's opening lines with the guardian, then the route to the lower gardens opens
            yield return Say("VEN_B05", 1, 3);
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
            if (Flat(Bike.transform.position - P7).magnitude > 45f && !Bike.Riding) { Bike.Place(Ground.ClampToArena(P7 + Vector3.right * 2f, 0.8f), 90f); Log("bike summoned to her (she was far from it)"); }
            if (Auto) { yield return AutoWalkPath(new[] { new Vector3(30f, 0f, 244f), new Vector3(30f, 0f, 216f), VenusLayout.ParkSpot - Vector3.right * 1.4f }); yield return AutoMount();
                StartCoroutine(AutoRide(new[] { new Vector3(46f, 0f, 199f), new Vector3(76f, 0f, 199f), new Vector3(76.5f, -3f, 175f), new Vector3(76.5f, -6f, 152f), VenusLayout.ObsPark }, () => P7.z < 151f && P7.y < -5f)); }
            yield return Until(() => P7.x > 64f && P7.z < 152f && P7.y < -5f, "the creature observation area (B05 -> C05)", 200f);
            // she parks and watches; C05 plays in gameplay (movement not locked)
            if (Bike.Riding) { yield return Until(() => Bike.Speed < 1.2f || Auto, "stopped", 10f); Bike.Dismount(true); FollowCamera(); }
            if (Auto) yield return WalkSeven(VenusLayout.ObsSpot, 6f);
            Shoot(VenusLayout.ObsSpot + new Vector3(-2.5f, 2.2f, 3.5f), VenusLayout.BasinCentre + Vector3.up * 2f, 55f, 1.2f);
            yield return Wait(1.0f);
            yield return Shot("v22_c05_observation_area");
            Cam.Follow();
            yield return SayInGameplay("VEN_C05", 1, 3);
            // the signal pulse: the creatures visibly change direction (toward the temple foundations)
            AudioKit.Play("signal_pulse", VenusLayout.BasinCentre, 1f, 0f, AudioKit.Bus.Sfx, 200f);
            var before = new List<Vector3>();
            if (Worms != null) { foreach (var w in Worms) { before.Add(w.Heading); w.Drive(VenusLayout.Foundations, 14f); } }
            Shoot(VenusLayout.ObsSpot + new Vector3(4f, 7f, 2f), VenusLayout.BasinCentre, 60f, 1.5f);
            yield return Wait(3.0f);
            float turn = 0f; if (Worms != null) for (int i = 0; i < Worms.Length; i++) turn = Mathf.Max(turn, Vector3.Angle(before[i], Worms[i].Heading));
            Log($"C05 signal pulse: {Worms?.Length ?? 0} creatures turned toward the temple foundations (largest turn {turn:0} deg in 3 s)");
            yield return Shot("v23_c05_creatures_change_direction");
            Cam.Follow();
            yield return SayInGameplay("VEN_C05", 4, 5);
            Complete("VEN_C05", VenusState.Stage.ControlDiscovered);
            Objective(VenusState.Objectives[(int)VenusState.Stage.ControlDiscovered]);
        }

        IEnumerator EndOfPart1()
        {
            yield return Wait(2.0f);
            if (Auto) yield return Shot("v24_root_gate_shut_round12");
            yield return UI.Title("Venus continues…\n(the control chamber comes next)", 0.8f, 2.6f, 0.8f);
            if (Auto)
            {
                Cap.Log(VenusState.Summary());
                Cap.Log($"VENUS PART 1 DONE: {VenusState.CurrentId}, reloads {Reloads}, record awards {VenusState.RecordAwards}, bike recoveries {Bike.Recoveries}, gave way {Bike.GiveWays}x, odometer {Bike.Odometer:0} m");
                Cap.Log(ChapterCamera.Summary());
                Cap.Finish("Venus part 1 check run");
            }
        }

        // ------------------------------------------------------------------------------------------ watchdog
        IEnumerator Watchdog()
        {
            while (true)
            {
                yield return null;
                if (_cut || PauseMenu.IsOpen || Dlg.Busy) continue;
                if (_objective != null && UI.Objective != _objective) UI.SetObjective(_objective);
                if (Bike.Riding && Bike.Stuck && !Auto) { Bike.Recover(); Log("WATCHDOG: the bike was stuck - recovered to the last safe spot"); }
                // fell somewhere with no ground (never expected): back to the last safe spot
                if (P7.y < -30f) { Seven.Place(Seven.LastSafe, Vector3.forward); Log("WATCHDOG: 7 below the world - back to safety"); }
            }
        }

        // ------------------------------------------------------------------------------------------ check-run autopilot
        IEnumerator AutoRide(Vector3[] pts, Func<bool> until)
        {
            Bike.ScriptDriven = true;
            int i = 0;
            while (i < pts.Length && !until())
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
            yield return AutoWalkPath(new[] { Bike.transform.position - Bike.transform.right * 1.3f });
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

        IEnumerator AutoDetour()
        {
            yield return AutoWalkPath(new[] { new Vector3(24.6f, 0f, 112f) });
            yield return Shot("v13b_detour_mantle_wall");
            for (float t = 0f; t < 6f && P7.y < 0.9f; t += Time.deltaTime) { Seven.ScriptMove = Vector3.left * 0.6f; if (Seven.Current == ChapterSeven.Mode.Free) Seven.ScriptJump = true; yield return null; }
            Cap.Log($"detour: {(P7.y > 0.9f ? "ok" : "FAILED")} mantle onto the garden wall");
            yield return AutoWalkPath(new[] { new Vector3(19.2f, 1f, 118f), new Vector3(19.2f, 1f, 125.2f) });
            for (float t = 0f; t < 6f && P7.z < 128.8f; t += Time.deltaTime) { Seven.ScriptMove = Vector3.forward * 0.8f; if (Seven.Current == ChapterSeven.Mode.Free) Seven.ScriptJump = true; yield return null; }
            Cap.Log($"detour: {(P7.z > 128.6f ? "ok" : "FAILED")} jump across the broken channel");
            yield return Shot("v13c_detour_across_the_channel");
            yield return AutoWalkPath(new[] { new Vector3(21f, 1f, 133f), new Vector3(28f, 0f, 133f) });
        }
    }
}
