using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 16 — the Saucer scene's asteroid parts (Docs/CHAPTER_ASTEROID.md):
    /// AST_01 (<see cref="AsteroidState.ArrivalAboard"/>): the title card "THE ASTEROID — THE GREAT MACHINE AGE" over black
    /// from the first frame; 7 at the controls with Panther in her lap; the window shows the asteroid - silver structures
    /// through dark rock, rings turning round a recessed opening, small white route lights, one section intact and one
    /// stripped to its frame; she lifts him onto the adjoining seat ("I was working.") - then the Asteroid scene (the docking
    /// and the receiving chamber).
    /// AST_10 (<see cref="AsteroidState.HomeAboard"/>): Dan - keep the reunion but no gap or lock-up: it opens straight from
    /// the beam-up - she is already kneeling at the boarding area, Panther walks the last steps into her hands, forehead to
    /// forehead, the purr carries the pause; a fade; then the navigation chair, Panther in her lap (AST_10_07-12) and AST_11:
    /// Branch A (Biaveh unfinished - Biaveh is still a "coming soon" card) or Branch B (Biaveh complete: the Cattalus lead,
    /// once - also at the console whenever it is due and hasn't played: the QF20 lesson). Then free exploration.
    /// </summary>
    public partial class Chapter1Aboard
    {
        public bool AsteroidMode { get; private set; }
        public bool AsteroidHomeDone { get; private set; }

        void StartAsteroid()
        {
            bool arrival = AsteroidState.ArrivalAboard; AsteroidState.ArrivalAboard = false; AsteroidState.HomeAboard = false;
            I = this; Active = true; VenusMode = true; MarsMode = true; AsteroidMode = true;
            Director.ChapterMode = true;
            Cat.RoamScale = 1f; Cat.LapScale = 1f; Cat.StrollSpeed = 1.0f; Cat.TrotSpeed = 1.6f; Cat.transform.localScale = Vector3.one;
            UI.SetFade(1f);
            if (FridgeLight != null) FridgeLight.enabled = false;
            _ik = Seven.gameObject.AddComponent<SevenTableIK>();
            _focus = Focus != null ? Focus : new GameObject("GalleyFocus").AddComponent<CloseUpFocus>();
            StackSubtitleCamera();
            _catPose = Cat.gameObject.AddComponent<CatPoseOverlay>();
            if (Moon != null) Moon.gameObject.SetActive(false);
            if (Shape != null) { _carry = CarriedShape.Attach(Seven.gameObject, Shape.transform); _carry.Set(CarriedShape.Where.Pocket, 0f); }
            _cubeCopy = MakeCubeCopy();
            AudioKit.Loop("amb_saucer", null, null, 1f, AudioKit.Bus.Ambience);
            if (DemoBoot.CaptureDir != null)
            {
                ChapterCapture.Ensure(); Cap.Attach(Director.Cam.GetComponent<Camera>());
                if (Auto) { Seven.ScriptDriven = true; Dlg.AutoSkipAfter = DemoBoot.VenusSkip ? 0f : 0.9f; Director.Legend.AutoSwitch = false; }
                Cap.Log($"aboard (asteroid mode, {(arrival ? "arrival" : "home")}): {AsteroidState.Summary()}; {WorldFlags.Summary()}");
                StartCoroutine(FirstFrameA());
            }
            StartCoroutine(arrival ? AsteroidArrival() : AsteroidHome());
        }
        IEnumerator FirstFrameA() { Cap.Log($"first frame aboard (the Asteroid): fade {UI.FadeAlpha:0.00} (1 = black)"); yield return Cap.Shot("aa00_first_frame_aboard"); }

        IEnumerator SayA(string scene, int from, int to = -1, bool holdPause = true) { yield return SayLines(AsteroidDialogue.Scene(scene, from, to < 0 ? from : to), holdPause); }
        IEnumerator SayAIds(params string[] ids) { yield return SayLines(AsteroidDialogue.Ids(ids), true); }
        IEnumerator SayLines(System.Collections.Generic.List<VoiceBank.Line> ls, bool holdPause)
        {
            foreach (var l in ls)
            {
                bool talkClip = false;
                if (!l.IsCat) { _ik.Talk = 1f; if (_seated && _ik.RightW < 0.05f && Seven.HasState("sit_talking")) { talkClip = true; Seven.PlayClip("sit_talking", 0.5f); } }
                else if (_catPose != null) _catPose.LookAt = SevenEyes;
                float t0 = Time.time;
                yield return Dlg.PlayOne(l, false);
                _ik.Talk = 0f; if (talkClip) Seven.PlayClip(_seated ? _seatClip : "idle", 0.6f);
                if (l.IsCat && _catPose != null) _catPose.LookAt = null;
                if (DemoBoot.CaptureDir != null) Cap?.Log($"line {l.Id} ({l.Label ?? l.Speaker}) played {Time.time - t0:0.00}s of {(l.Clip != null ? l.Clip.length : 0f):0.00}s: '{l.Text}'");
                if (holdPause) yield return Wait(0.45f);
            }
        }

        /// <summary>The records on the chair arm, the asteroid's small cuboctahedron among them once awarded.</summary>
        void PlaceRecordsOnArmAsteroid()
        {
            PlaceRecordsOnArmMars();
            if (AsteroidState.RecordAwarded) { var c = MakeCuboctahedronCopy(); c.SetPositionAndRotation(ArmRest - PilotFwd * 0.40f + Up * 0.02f, Quaternion.LookRotation(PilotFwd)); }
        }
        Transform MakeCuboctahedronCopy()
        {
            var go = new GameObject("AsteroidRecordCopy");
            go.AddComponent<MeshFilter>().sharedMesh = Cuboctahedron.Mesh(0.06f);
            var mr = go.AddComponent<MeshRenderer>();
            var src = Shape != null ? Shape.GetComponentInChildren<Renderer>() : null;
            if (src != null) mr.sharedMaterial = src.sharedMaterial;
            return go.transform;
        }
        /// <summary>The window: the asteroid outside (the backdrop in SaucerOutside), framed past her shoulder.</summary>
        void WindowOnTheAsteroid() { var wf = Flat(Polar(9f, WindowA, 0f)).normalized; Cut(PilotHead - PilotFwd * 0.6f + PilotRight * 0.35f + Up * 0.25f, PilotHead + wf * 30f + Up * -4f, 50f); }

        // ------------------------------------------------------------------------------------------ AST_01
        IEnumerator AsteroidArrival()
        {
            ShipConsole.StoryLock = true; InGalleyScene = true;
            Seven.Frozen = true; Seven.Cutscene = true; Director.Cam.Hold = true;
            Cat.ForceTarget = null; Cat.enabled = false;
            SaucerLocation.Set("asteroid");
            MusicSystem.Stop(0.5f);
            yield return UI.Title("THE ASTEROID  —  THE GREAT MACHINE AGE", 1.0f, 2.4f, 1.0f);
            if (DemoBoot.CaptureDir != null) Cap?.Log("title card: 'THE ASTEROID — THE GREAT MACHINE AGE' over black");
            yield return EnterStaging("AST_01");                             // 7 at the controls, Panther in her lap
            PlaceRecordsOnArmAsteroid();
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 1f;
            // EXTERIOR WIDE through the window: the asteroid, its silver structures, the turning rings, the white route lights
            var wf = Flat(Polar(9f, WindowA, 0f)).normalized;
            Cut(new Vector3(0f, 1.7f, 0f) + wf * 4.5f, new Vector3(0f, -6f, 0f) + wf * 120f, 52f);
            yield return UI.Fade(0f, 1.4f);
            yield return Wait(1.0f);
            yield return ShotV("aa01_the_asteroid_through_the_window");
            CutPilot("two");
            // QF21 (1): as they talk about the asteroid, the ship dissolves round them - the pilot's-chair effect (ShipReveal,
            // the button that makes the ship see-through), started from her seat; the chair and his seat beside it stay solid
            StartCoroutine(HullAway(true));
            yield return SayA("AST_01", 1, 1);                               // That's built into it? (she leans forward)
            Cat.LapNudge(-8f);                                               // his ears turn toward a faint mechanical sound
            yield return SayA("AST_01", 2, 2);                               // Through it, from what I can see.
            OpenSpaceShot();
            yield return ShotV("aa01b_the_hull_dissolves_round_them");
            yield return SayA("AST_01", 3, 4);                               // Someone really wanted a workshop. / ...every workshop.
            yield return ShotV("aa02_one_section_intact_one_stripped");
            _stroke = 0f;                                                    // her hand stops stroking his back
            CutPilot("lap");
            yield return SayA("AST_01", 5, 6);                               // There are lights. / Mm-hmm. Something is still working.
            CutPilot("two");
            yield return SayA("AST_01", 7, 7);                               // Let's find out who. (she lifts him onto the adjoining seat)
            yield return LiftCatToSeat();
            yield return ShotV("aa03_she_lifts_him_onto_the_adjoining_seat");
            yield return SayA("AST_01", 8, 10);                              // I was working. / Oh? At what? / Keeping you seated.
            yield return ShotV("aa04_keeping_you_seated");
            // the scene ends: the hull rebuilds round them before she goes down to disembark
            OpenSpaceShot();
            yield return HullAway(false);
            yield return ShotV("aa04b_the_hull_back");
            AsteroidState.CompleteScene("AST_01");
            AsteroidState.Reach(AsteroidLayout.CpReceiving);
            _ik.ReleaseAll("AST_01 ends");
            if (_strokeRoutine != null) { StopCoroutine(_strokeRoutine); _strokeRoutine = null; }
            yield return UI.Fade(1f, 1.2f);
            if (DemoBoot.CaptureDir != null) Cap?.Log("asteroid arrival aboard done -> the Asteroid scene (the docking recess, the receiving chamber)");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Asteroid");
        }
        /// <summary>QF21 (1): the ship "invisible" while they talk - the existing reveal (ShipReveal, the pilot's-chair button)
        /// run from her seat at the front window; her chair, the seat beside it (his) and the records on the arm stay solid;
        /// slow, with the shimmer at the dissolving edge. away = false: the hull comes back (from the outside in).</summary>
        IEnumerator HullAway(bool away)
        {
            var rev = Director != null ? Director.Reveal : null;
            if (rev == null) { if (DemoBoot.CaptureDir != null) Cap?.Log("AST_01 (QF21 1): no ShipReveal in the scene - the hull stays"); yield break; }
            if (away)
            {
                rev.CenterOverride = ShipConsole.I != null && ShipConsole.I.PilotSeat != null ? ShipConsole.I.PilotSeat : null;
                var keep = new System.Collections.Generic.List<Renderer>();
                if (ShipConsole.I != null && ShipConsole.I.PilotChair != null) keep.AddRange(ShipConsole.I.PilotChair.GetComponentsInChildren<Renderer>(true));
                var catSeat = PilotHips + PilotFwd * 0.62f - PilotRight * 0.42f;
                var ship = GameObject.Find("Ship");
                if (ship != null)
                    foreach (var r in ship.GetComponentsInChildren<Renderer>(true))
                    {
                        var b = r.bounds; if (b.size.magnitude > 2.6f) continue;               // never the floor, walls or the hull
                        if ((b.ClosestPoint(catSeat) - catSeat).magnitude < 0.55f || (b.ClosestPoint(ArmRest) - ArmRest).magnitude < 0.3f) keep.Add(r);
                    }
                foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if (r.name.StartsWith("AsteroidRecordCopy") || r.name.EndsWith("RecordCopy")) keep.Add(r);
                rev.KeepSolid(keep);
                float d0 = rev.Duration; rev.Duration = 6.5f;                                  // slow and elegant
                rev.Set(true);
                if (DemoBoot.CaptureDir != null) Cap?.Log($"AST_01 (QF21 1): the hull dissolves round them - the ship-reveal effect from the pilot's seat, {rev.KeptCount} renderers kept solid (her chair, his seat, the records)");
                for (float t = 0f; t < 10f && rev.Progress < 0.22f; t += Time.deltaTime) yield return null;
                yield return ShotV("aa01a_mid_dissolve_the_shimmering_edge");
                for (float t = 0f; t < 10f && rev.Progress < 0.999f; t += Time.deltaTime) yield return null;
                rev.Duration = d0;
                if (DemoBoot.CaptureDir != null) Cap?.Log($"AST_01 (QF21 1): open space round them (reveal {rev.Progress:0.00})");
            }
            else
            {
                float d0 = rev.Duration; rev.Duration = 4.5f;
                rev.Set(false);
                for (float t = 0f; t < 10f && rev.Progress > 0.001f; t += Time.deltaTime) yield return null;
                rev.Duration = d0; rev.ClearKeep();
                if (DemoBoot.CaptureDir != null) Cap?.Log($"AST_01 (QF21 1): the hull is back (reveal {rev.Progress:0.00}) before she goes to disembark");
            }
        }
        /// <summary>The two of them in open space with the asteroid filling the view (wide, from behind and beside the chair).</summary>
        void OpenSpaceShot()
        {
            // beside and a little ahead of the chair, looking back past her profile toward the asteroid (never the chair's back)
            var wf = Flat(Polar(9f, WindowA, 0f)).normalized; var side = Vector3.Cross(Up, wf);
            Cut(PilotHead + side * 2.6f + wf * 1.2f + Up * 0.15f, PilotHead + wf * 7f - side * 1.2f - Up * 0.9f, 60f);
        }
        /// <summary>AST_01_07: she gently lifts him off her lap onto the adjoining seat (the co-pilot pod beside the chair).</summary>
        IEnumerator LiftCatToSeat()
        {
            var from = Cat.transform.position;
            var disp = ShipConsole.I != null && ShipConsole.I.Display != null ? ShipConsole.I.Display.position : PilotHips + PilotFwd * 1.1f + Up * 0.5f;
            var seat = PilotHips + PilotFwd * 0.62f - PilotRight * 0.42f; seat.y = disp.y - 0.34f;      // the console desk beside the chair (where he sits in the route scenes)
            Cat.enabled = false;
            _ik.LeftW = 1f; _ik.RightW = 1f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.3f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                var p = Vector3.Lerp(from, seat, u) + Up * Mathf.Sin(u * Mathf.PI) * 0.25f;
                Cat.transform.position = p;
                _ik.LeftPos = p + Up * 0.12f - PilotRight * 0.1f; _ik.RightPos = p + Up * 0.12f + PilotRight * 0.1f;
                yield return null;
            }
            Cat.transform.SetPositionAndRotation(seat, Quaternion.LookRotation(Flat(PilotHips - seat)));
            CatState("sit");
            _ik.LeftW = 0f; _ik.RightW = 0f;
            if (_catPose != null) _catPose.LookAt = SevenEyes;                // he glances at her now-empty lap
        }

        // ------------------------------------------------------------------------------------------ AST_10 + AST_11
        IEnumerator AsteroidHome()
        {
            ShipConsole.StoryLock = true; InGalleyScene = true;
            Seven.Frozen = true; Seven.Cutscene = true; Director.Cam.Hold = true;
            Cat.ForceTarget = null; Cat.enabled = false;
            SaucerLocation.Set("asteroid");
            MusicSystem.Stop(0.2f);
            if (!AsteroidState.SceneDone("AST_10_reunion"))
            {
                // straight from the beam-up: she is already kneeling at the boarding area; he walks directly into her hands
                var at = new Vector3(-1.2f, 0f, -4.6f); var fwd = new Vector3(0.2f, 0f, 1f).normalized;
                Seven.transform.SetPositionAndRotation(at, Quaternion.LookRotation(fwd));
                if (Seven.HasState("crouch_idle")) Seven.PlayClip("crouch_idle", 0f);
                var catFrom = at + fwd * 1.7f + Vector3.Cross(Up, fwd) * 0.3f; var catTo = at + fwd * 0.55f;
                Cat.transform.SetPositionAndRotation(catFrom, Quaternion.LookRotation(Flat(at - catFrom)));
                CatClip("walk", 0.05f);
                var mid = (at + catTo) * 0.5f; var side = Vector3.Cross(Up, fwd);
                CamTo(mid + side * 2.2f + Up * 0.9f + fwd * 0.3f, mid + Up * 0.55f, 0f, 44f);
                StartCoroutine(UI.Fade(0f, 0.8f));
                for (float t = 0f; t < 1f; t += Time.deltaTime / 1.6f) { Cat.transform.position = Vector3.Lerp(catFrom, catTo, t); yield return null; }
                CatClip("sit", 0.2f);
                yield return ShotV("aa10_he_walks_straight_into_her_hands");
                // she lifts him to her; he presses his forehead against hers
                var head = SevenHead; var hold = head + fwd * 0.2f - Up * 0.26f;
                _ik.LeftW = 1f; _ik.RightW = 1f;
                var c0 = Cat.transform.position;
                for (float t = 0f; t < 1f; t += Time.deltaTime / 1.1f)
                {
                    float u = Mathf.SmoothStep(0f, 1f, t); var p = Vector3.Lerp(c0, hold, u);
                    Cat.transform.SetPositionAndRotation(p, Quaternion.LookRotation(Flat(at - p).sqrMagnitude > 0.001f ? Flat(at - p) : -fwd));
                    _ik.LeftPos = p + Up * 0.1f - side * 0.1f; _ik.RightPos = p + Up * 0.1f + side * 0.1f; _ik.LookPos = p + Up * 0.32f; _ik.LookW = 0.9f;
                    yield return null;
                }
                CamTo(hold + side * 1.1f + Up * 0.35f + fwd * 0.1f, hold + Up * 0.3f - fwd * 0.08f, 0.6f, 36f);
                yield return Wait(2.0f);                                      // hold without dialogue for a moment
                yield return ShotV("aa11_forehead_to_forehead");
                yield return SayA("AST_10", 1, 4);                            // Hello, you. / You're late. / We didn't agree on a time. / I decided on one...
                if (_catPose != null) _catPose.TailToward = SevenHead - Up * 0.5f;   // his tail curls round her wrist
                yield return SayA("AST_10", 5, 5);                            // Mm. Sorry. (she rubs behind his ear)
                var purr = AudioKit.Loop("cat_purr", Cat.transform, null, 0.5f, AudioKit.Bus.Sfx, 1f, 8f);
                yield return Wait(2.6f);                                      // AST_10_06: he purrs; let it carry the pause
                yield return ShotV("aa12_the_purr_carries_the_pause");
                if (purr != null) StartCoroutine(AudioKit.Fade(purr, 0f, 1.5f, true));
                _ik.ReleaseAll("the reunion ends");
                if (_catPose != null) _catPose.TailToward = null;
                AsteroidState.CompleteScene("AST_10_reunion");
                yield return UI.Fade(1f, 1.0f);
            }
            // LATER: the navigation chair, Panther in her lap; the asteroid through the window, an archive light switching on
            MusicSystem.Play("aboard_saucer", 3f);
            yield return EnterStaging("AST_10");
            PlaceRecordsOnArmAsteroid();
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 1f;
            WindowOnTheAsteroid();
            yield return UI.Fade(0f, 1.2f);
            yield return ShotV("aa13_the_chair_the_asteroid_outside");
            yield return SayA("AST_10", 7, 8);                                // He kept everything ready. All that time. / He didn't know what else to do.
            CutPilot("lap");
            yield return SayA("AST_10", 9, 10);                               // Do you think anyone will come back? / I don't know.
            WindowOnTheAsteroid();
            SaucerOutside.I?.ArchiveLightOn();                                // an archive light switches on out there
            yield return SayA("AST_10", 11, 12);                              // At least there'll be something left for them. / Mm-hmm.
            yield return ShotV("aa14_an_archive_light_switches_on");
            AsteroidState.CompleteScene("AST_10");
            AsteroidState.Reach(AsteroidLayout.CpDone);
            WorldFlags.MarkAsteroidDone();
            yield return NextDestination("the end of the asteroid");
        }
        /// <summary>AST_11: Branch B (the Cattalus lead) once, when Biaveh is complete and it has not played; else Branch A.</summary>
        IEnumerator NextDestination(string where)
        {
            var con = ShipConsole.I;
            if (WorldFlags.CattalusDue)
            {
                if (DemoBoot.CaptureDir != null) Cap?.Log($"AST_11B (the Cattalus lead) plays at {where}: {WorldFlags.Summary()}");
                NavHand(true); CutPilot("ots");
                if (con != null) con.ShowMap(null, "The Biavian record  ·  the asteroid record");
                yield return SayA("AST_11B", 1, 2);                           // There. The part missing from the Biaveh record. / Hmm. Enlarge that mark.
                Cat.LapNudge(-12f); CutPilot("lap");
                yield return SayA("AST_11B", 3, 7);                           // You recognize it? / Yes. / Panther? / Cattalus. / Your home?
                yield return SayA("AST_11B", 8, 13);                          // ...entrusted to us. / ... show me around. / There will be rules. / ... / I'll knock.
                WorldFlags.MarkCattalusSeen();
                if (con != null) yield return con.ComingSoonCard("Cattalus (coming soon)");
            }
            else
            {
                if (DemoBoot.CaptureDir != null) Cap?.Log($"AST_11A (Biaveh unfinished) at {where}: {WorldFlags.Summary()}");
                CutPilot("lap");
                yield return SayA("AST_11A", 1, 1);                           // Biaveh is still ahead of us.
                CutPilot("two");
                yield return SayA("AST_11A", 2, 5);                           // Right. Our actual destination. / You wanted a closer look. / I got one. / An exceptionally close look.
                NavHand(true); CutPilot("ots");
                if (con != null) con.ShowMap("biaveh", WorldFlags.ContactName + "  ·  beyond this solar system");
                yield return SayA("AST_11A", 6, 7);                           // Biaveh. And something to eat on the way. / Now that is a plan.
                yield return ShotV("aa15_biaveh_selected");
                if (con != null) yield return con.ComingSoonCard("The Biavian Mothership (coming soon)");
            }
            AsteroidHomeDone = true;
            if (DemoBoot.CaptureDir != null) Cap?.Log($"ASTEROID CHAPTER DONE: {AsteroidState.Summary()}; {WorldFlags.Summary()}");
            _stroke = 0f; if (_strokeRoutine != null) { StopCoroutine(_strokeRoutine); _strokeRoutine = null; }
            _ik.ReleaseAll("the asteroid's departure routing ends");
            yield return ExitStaging("the asteroid's departure routing ends");
            ShipConsole.StoryLock = false;
            if (Auto) { Cap.Finish($"ASTEROID CHECK RUN DONE: {AsteroidState.Summary()}; {WorldFlags.Summary()}"); yield break; }
            yield return ExploreVenus(false);
        }
        /// <summary>The Cattalus lead is due at the console (the QF20 lesson): she takes the pilot's chair, AST_11B.</summary>
        public IEnumerator CattalusAtConsole()
        {
            ShipConsole.StoryLock = true; InGalleyScene = true;
            Seven.Frozen = true; Seven.Cutscene = true; Director.Cam.Hold = true;
            yield return UI.Fade(1f, 0.8f);
            yield return EnterStaging("AST_10");
            PlaceRecordsOnArmAsteroid();
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 1f;
            CutPilot("two");
            yield return UI.Fade(0f, 1.0f);
            yield return NextDestination("the console (due: the asteroid + Biaveh complete, not seen yet)");
        }
    }
}
