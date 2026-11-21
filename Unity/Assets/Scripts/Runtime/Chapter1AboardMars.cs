using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 15 — the Saucer scene's Mars parts (Docs/CHAPTER_MARS.md):
    /// section 1 (<see cref="MarsState.ArrivalAboard"/>): the title card "MARS — THE DIVIDED KINGDOMS" over black from the first
    /// frame, 7 in the pilot's chair with Panther already in her lap, the navigation display, MAR_01 - then the Mars scene
    /// (it opens on the extreme wide of the saucer crossing the plain; decision: the wide comes after the chair scene, so the
    /// scene change happens once);
    /// sections 12-13 (<see cref="MarsState.HomeAboard"/>): she sits in the pilot's chair, Panther climbs into her lap, MAR_12;
    /// the navigation display; the destination choice on the star map in its two versions (Europa unfinished / complete,
    /// read from the completed-world flags), MAR_13B; she selects the course. The asteroid world and Biaveh have no chapter
    /// yet: their "coming soon" card, then free exploration aboard (the star map has them from now on).
    /// </summary>
    public partial class Chapter1Aboard
    {
        public bool MarsMode { get; private set; }
        public bool MarsDebriefDone { get; private set; }
        public string MarsChoice { get; private set; }

        void StartMars()
        {
            bool arrival = MarsState.ArrivalAboard; MarsState.ArrivalAboard = false; MarsState.HomeAboard = false;
            I = this; Active = true; VenusMode = true; MarsMode = true;
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
                Cap.Log($"aboard (Mars mode, {(arrival ? "arrival" : "home")}): {MarsState.Summary()}; {WorldFlags.Summary()}");
                StartCoroutine(FirstFrameM());
            }
            StartCoroutine(arrival ? MarsArrival() : MarsHome());
        }
        IEnumerator FirstFrameM() { Cap.Log($"first frame aboard (Mars): fade {UI.FadeAlpha:0.00} (1 = black)"); yield return Cap.Shot("ma00_first_frame_aboard"); }

        IEnumerator SayM(string scene, int from, int to = -1, bool holdPause = true)
        {
            foreach (var l in MarsDialogue.Scene(scene, from, to < 0 ? from : to))
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

        // ------------------------------------------------------------------------------------------ section 1
        IEnumerator MarsArrival()
        {
            ShipConsole.StoryLock = true;
            InGalleyScene = true;
            Seven.Frozen = true; Seven.Cutscene = true; Director.Cam.Hold = true;
            Cat.ForceTarget = null; Cat.enabled = false;
            MusicSystem.Play("mars_kingdoms", 3f);
            yield return UI.Title("MARS  —  THE DIVIDED KINGDOMS", 1.0f, 2.4f, 1.0f);
            if (DemoBoot.CaptureDir != null) Cap?.Log("title card: 'MARS — THE DIVIDED KINGDOMS' over black");
            yield return EnterStaging("MAR_01");                                // QF16 (3): at the window, Panther on her shoulder
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 0f;
            PlaceRecordsOnArmMars();
            // the navigation display: the plain, the two fortress cities, the ravine, the temple on its island
            NavHand(true);
            // QF18 (2): the display SHOWS it - a glowing hologram of the actual Mars level (the plain, the ravine, both fortress
            // cities, the temple island, the bridges), the temple highlighted - projected in front of her at the window
            var holo = HoloDisplay.Get(Seven.transform.position + Seven.transform.forward * 1.25f + Vector3.up * 1.12f, -Seven.transform.forward);
            holo?.Show("mars", 1.0f, 1150f, new Vector3(-40f, 0f, 60f));
            holo?.Mark(new Vector3(0f, 0f, 178f), 28f, "the temple");
            if (holo != null) Cut(holo.transform.position - Seven.transform.forward * 1.9f + Seven.transform.right * 0.55f + Up * 0.45f, holo.transform.position + Up * 0.05f, 44f);
            yield return UI.Fade(0f, 1.4f);
            yield return Wait(0.8f);
            yield return ShotV("ma01_navigation_display_the_temple");
            yield return SayM("MAR_01", 1, 1);                                   // There's the temple.
            CutPilot("lap");
            StartCoroutine(CatMomentTailFlick());                                // QF16 (4): his tail flicks across her face
            yield return SayM("MAR_01", 2, 2);                                   // And several people determined to make reaching it inconvenient.
            yield return ShotV("ma02_panther_in_her_lap");
            // she studies the bridges - QF18 (2): the display zooms to the two bridges, the barricades marked
            if (holo != null)
            {
                holo.Bar(new Vector3(-51f, 0f, -6f), new Vector3(-51f, 0f, 6f), 4f, "the west checkpoint barrier");
                holo.Bar(new Vector3(51f, 0f, -6f), new Vector3(51f, 0f, 6f), 4f, "the east checkpoint barrier");
                holo.Bar(new Vector3(-34f, 0f, 143f), new Vector3(-34f, 0f, 157f), 4f, "the west span, out of line");
                holo.Bar(new Vector3(34f, 0f, 143f), new Vector3(34f, 0f, 157f), 4f, "the east span, out of line");
                Cut(holo.transform.position + Seven.transform.forward * -0.8f + Seven.transform.right * 0.25f + Up * 0.42f, holo.transform.position, 36f);
                yield return holo.Focus(new Vector3(0f, 0f, 75f), 2.4f, 1.8f);
                yield return ShotV("ma02b_hologram_the_barricaded_bridges");
            }
            yield return Wait(0.6f);
            CutPilot("two");
            yield return SayM("MAR_01", 3, 4);                                   // The guardian did mention borders. / Mm. They appear to have been thorough.
            yield return ShotV("ma03_two_shot_in_the_chair");
            NavHand(false);
            holo?.Hide();
            _stroke = 0f;
            yield return Wait(0.8f);
            if (_strokeRoutine != null) StopCoroutine(_strokeRoutine);
            _ik.ReleaseAll("the Mars arrival chair scene ends");
            MarsState.CompleteScene("MAR_01");
            yield return UI.Fade(1f, 1.2f);
            if (DemoBoot.CaptureDir != null) Cap?.Log("Mars arrival aboard done -> the Mars scene (the extreme wide over the plain, the landing)");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Mars");
        }

        // ------------------------------------------------------------------------------------------ sections 12-13
        IEnumerator MarsHome()
        {
            ShipConsole.StoryLock = true;
            InGalleyScene = true;
            Seven.Frozen = true; Seven.Cutscene = true; Director.Cam.Hold = true;
            Cat.ForceTarget = null;
            MusicSystem.Play("aboard_saucer", 2.5f);
            // 7 sits in the pilot's chair; Panther climbs into her lap
            yield return EnterStaging("MAR_12");                                // QF16 (3): both lying on the sofa (a quiet, personal talk)
            PlaceRecordsOnArmMars();
            CutPilot("two");
            yield return UI.Fade(0f, 1.2f);
            yield return ShotV("ma10_panther_climbs_into_her_lap");
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 1f;
            CutPilot("two");
            yield return SayM("MAR_12", 1, 2);                                   // They were both so certain. / About the bridge?
            yield return Wait(0.5f);                                             // she nods, stroking him
            yield return SayM("MAR_12", 3, 4);                                   // What if I'm doing that? ... / Then keep asking.
            CutPilot("lap");
            yield return SayM("MAR_12", 5, 5);                                   // And if I get it wrong?
            Cat.LapNudge(-14f);                                                  // he looks up at her
            yield return SayM("MAR_12", 6, 6);                                   // I'll mention it.
            yield return ShotV("ma11_he_looks_up_at_her");
            CutPilot("two");
            yield return SayM("MAR_12", 7, 7);                                   // Oh, I know you will. (she laughs)
            Cat.LapNudge(10f);                                                   // he settles as she brings up the navigation display
            MarsState.CompleteScene("MAR_12");
            // section 13 (Dan's revised departure routing): the saucer stays put; the completed-world flags decide
            MarsState.Reach(MarsLayout.CpDone);
            MarsDebriefDone = false;
            yield return SwitchStaging("MAR_13");                               // QF16 (3): the route display - the chair, Panther on the console
            if (DemoBoot.CaptureDir != null) Cap?.Log($"MAR_13: {WorldFlags.Summary()} -> {(WorldFlags.CombinedDue ? "MAR_13C (Europa complete: the combined records)" : !WorldFlags.EuropaDone ? "MAR_13U (Europa unfinished)" : "MAR_13B only (the combined records already played)")}");
            if (WorldFlags.CombinedDue) { yield return CombinedRecords("the Mars debrief"); yield break; }
            var con = ShipConsole.I;
            if (!WorldFlags.EuropaDone)
            {
                // MAR_13U - Europa unfinished: Europa is the next destination; Biaveh and the asteroid stay unavailable
                NavHand(true);
                CutPilot("ots");
                if (con != null) con.ShowMap("europa", "Part of a route");
                yield return Wait(0.6f);
                yield return ShotV("ma12_display_part_of_a_route");
                yield return SayM("MAR_13U", 1, 3);                              // Does the record tell us where they went? / Part of the route... / So we're still missing something.
                _stroke = 0.6f;
                yield return SayM("MAR_13U", 4, 4);                              // Mm-hmm. We still have the Cetaceans on Europa...
                CutPilot("lap");
                _ik.LookPos = Seven.LapPoint(0.1f); _ik.LookW = 0.8f;
                yield return SayM("MAR_13U", 5, 6);                              // Beneath all that ice. / And water. An unreasonable amount of water.
                _stroke = 1f;
                yield return SayM("MAR_13U", 7, 7);                              // You haven't forgotten. (she strokes his back)
                Cat.LapNudge(10f);                                               // he settles deeper into her lap
                yield return SayM("MAR_13U", 8, 8);                              // I have tried.
                _ik.LookW = 0f;
                CutPilot("ots");
                yield return SayM("MAR_13U", 9, 9);                              // Europa, then. (selects Europa)
                yield return ShotV("ma13_europa_then");
                yield return Closing();
                MarsChoice = "europa";
                MarsDebriefDone = true;
                if (DemoBoot.CaptureDir != null) Cap?.Log($"MARS CHAPTER DONE: {MarsState.Summary()}; {WorldFlags.Summary()}; next: Europa");
                EndChairMoment("the Mars debrief ends");
                yield return TravelPassage.Run("europa");                    // QF16 (2)
                yield return UI.Fade(1f, 1.0f);
                if (EuropaState.HasProgress) EuropaState.ContinueOwn(); else EuropaState.StartFresh();
                yield break;
            }
            // (Europa done and the combined discovery already played: only the shared closing, then free exploration)
            yield return Closing();
            MarsDebriefDone = true;
            EndChairMoment("the Mars debrief ends");
            yield return ExitStaging("the Mars departure routing ends");
            ShipConsole.StoryLock = false;
            yield return ExploreVenus(false);
        }

        /// <summary>MAR_13B - the shared closing (she returns her hand to the controls).</summary>
        IEnumerator Closing()
        {
            NavHand(false);
            CutPilot("lap");
            _ik.LookPos = Seven.LapPoint(0.1f); _ik.LookW = 0.8f;
            yield return SayM("MAR_13B", 1, 1);                                  // Comfortable?
            Cat.LapNudge(8f);                                                    // his eyes half-close
            yield return SayM("MAR_13B", 2, 2);                                  // You may proceed.
            yield return SayM("MAR_13B", 3, 3, false);                           // Wouldn't dream of leaving without your permission. (soft laugh)
            _ik.LookW = 0f;
            NavHand(true);
            CutPilot("wide");
            yield return Wait(1.2f);
            yield return ShotV("ma14_she_returns_her_hand_to_the_controls");
        }

        void EndChairMoment(string why)
        {
            _stroke = 0f;
            if (_strokeRoutine != null) { StopCoroutine(_strokeRoutine); _strokeRoutine = null; }
            _ik.ReleaseAll(why);
        }

        /// <summary>MAR_13C - the combined-record discovery (the Mars route beside the Cetaceans' sequence): Biaveh unlocks.
        /// Played ONCE, whichever of Mars / Europa is finished second (the Mars debrief, or after EUR_17); then MAR_13B, the
        /// departure toward Biaveh and, on that first journey after the warp, the asteroid discovery (TRV_AST) and its choice.</summary>
        public IEnumerator CombinedRecords(string where)
        {
            if (!_staged || CurrentStage != Stage.ChairCatConsole) yield return SwitchStaging("MAR_13");
            if (DemoBoot.CaptureDir != null) Cap?.Log($"MAR_13C (the combined records) plays at {where}: {WorldFlags.Summary()}");
            var con = ShipConsole.I;
            NavHand(true);
            CutPilot("ots");
            if (con != null) con.ShowMap(null, "The Mars route  ·  the Cetaceans' sequence");
            _stroke = 0.3f;                                                      // her hand pauses on his back
            yield return SayM("MAR_13C", 1, 1);                                  // Wait. Put the route beside the sequence we brought back from Europa.
            // QF18 (2): the display SHOWS the two records side by side, their intervals lining up (no caption)
            var holo = HoloDisplay.Get();
            Coroutine recs = holo != null ? StartCoroutine(holo.Records()) : null;
            Cat.LapNudge(-10f);                                                  // he turns toward the display
            StartCoroutine(CatMomentRouteBat(false));                            // QF16 (4): he bats at the route line
            yield return SayM("MAR_13C", 2, 2);                                  // Hmm. One moment.
            StartCoroutine(CatMomentRouteBat(true));                             // ... and sits with his paw still raised
            yield return SayM("MAR_13C", 3, 3);                                  // There. Those intervals match.
            CatPawDown();
            if (recs != null) yield return recs;
            if (holo != null) Cut(holo.transform.position + PilotFwd * -0.9f + PilotRight * 0.3f + Up * 0.3f, holo.transform.position, 38f);
            yield return ShotV("ma15_two_records_side_by_side");
            yield return SayM("MAR_13C", 4, 5);                                  // They do... / They each kept a piece.
            if (holo != null) yield return holo.RouteCompletes();
            if (con != null) con.ShowMap("biaveh", "Biaveh  ·  beyond this solar system");
            WorldFlags.MarkCombined();
            Log13("Biaveh unlocked (Mars + Europa records combined)");
            CutPilot("lap");
            yield return SayM("MAR_13C", 6, 6);                                  // And you thought to put them together. (he looks up at her)
            CutPilot("ots");
            yield return SayM("MAR_13C", 7, 8);                                  // Biaveh. / Home of the Biaviian. Beyond this solar system.
            yield return ShotV("ma16_the_route_resolves_biaveh");
            holo?.Hide();
            CutPilot("two");
            _stroke = 1f;
            yield return SayM("MAR_13C", 9, 12);                                 // That's... a little farther than Europa. / I suggest eating... / You suggest that... / And yet...
            CutPilot("lap");
            _ik.LookPos = Seven.LapPoint(0.1f); _ik.LookW = 0.8f;
            yield return SayM("MAR_13C", 13, 13);                                // All right. Biaveh. Together?
            Cat.LapNudge(12f);                                                   // he gently presses his head against her hand
            yield return SayM("MAR_13C", 14, 14);                                // Mm-hmm.
            _ik.LookW = 0f;
            yield return Closing();
            WorldFlags.MarkCombinedSeen();                                       // QF20 (1): seen to its end - only now
            Log13($"MAR_13C seen to its end ({where}) - {WorldFlags.Summary()}");
            // the first journey toward Biaveh: the warp into its system, then a quiet navigation chime
            yield return TowardBiaveh();
            CutPilot("ots");
            yield return UI.Fade(0f, 0.9f);
            yield return TravelAsteroid();
        }
        /// <summary>QF20 (1): the discovery is due (Mars + Europa complete, MAR_13C never seen to its end - finished in an
        /// older build, via Scene Select, a quit mid-scene ...) and she goes to the console: she takes the pilot's chair with
        /// Panther instead, MAR_13C + MAR_13B play, then the journey toward Biaveh and TRV_AST after the warp.</summary>
        public IEnumerator CombinedAtConsole()
        {
            ShipConsole.StoryLock = true; InGalleyScene = true;
            Seven.Frozen = true; Seven.Cutscene = true; Director.Cam.Hold = true;
            Director.ExtraPrompt = null; Objective(null);
            Log13($"QF20: the combined-records discovery is due at the console - {WorldFlags.Summary()}");
            yield return UI.Fade(1f, 0.8f);
            yield return EnterStaging("MAR_13");
            PlaceRecordsOnArmMars();
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 1f;
            CutPilot("two");
            yield return UI.Fade(0f, 1.0f);
            yield return CombinedRecords("the console (due: both worlds complete, not seen yet)");
        }
        /// <summary>Biaveh chosen on the star map before the asteroid was found (e.g. after a Continue): the first journey -
        /// she takes the pilot's chair, Panther in her lap, the warp, then the asteroid discovery (TRV_AST).</summary>
        public IEnumerator TravelToBiaveh()
        {
            ShipConsole.StoryLock = true; InGalleyScene = true;
            Seven.Frozen = true; Seven.Cutscene = true; Director.Cam.Hold = true;
            yield return UI.Fade(1f, 0.8f);
            yield return EnterStaging("MAR_13");
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 0f;
            yield return TowardBiaveh();
            CutPilot("ots");
            yield return UI.Fade(0f, 0.9f);
            yield return TravelAsteroid();
        }
        /// <summary>QF16 (2): the first journey toward Biaveh = the sacred-geometry passage; the windows then show open space
        /// (the asteroid is found on the way, TRV_AST; Biaveh is not reached yet, so nothing is saved).</summary>
        IEnumerator TowardBiaveh()
        {
            yield return TravelPassage.Run("biaveh");
            SaucerLocation.System = StarMap.Bia;                                 // QF18 (22): the long jump arrives in Biaveh's system
            SaucerLocation.Travelling = true;
            yield return UI.Fade(1f, 0.5f);
        }
        void Log13(string s) { Debug.Log("[Seven] " + s); if (DemoBoot.CaptureDir != null) Cap?.Log(s); }

        /// <summary>TRV_AST - after the warp into Biaveh's system: an asteroid with artificial structures. The choice
        /// (Investigate the asteroid / Continue to Biaveh) - both unlock; neither chapter exists yet ("coming soon").</summary>
        IEnumerator TravelAsteroid()
        {
            var con = ShipConsole.I;
            AudioKit.Play("console_select", null, 0.6f);                        // the navigation chime
            if (con != null) con.ShowMap("asteroid", "An asteroid  ·  artificial structures");
            WorldFlags.MarkAsteroid();
            Log13("the asteroid discovered on the first journey toward Biaveh (TRV_AST)");
            Cat.LapNudge(-14f);                                                  // he lifts his head
            yield return SayM("TRV_AST", 1, 2);                                  // Hmm. There's something on that asteroid. / Something?
            yield return ShotV("ma17_an_asteroid_with_structures");
            yield return SayM("TRV_AST", 3, 6);                                  // Structures. Machinery... / Out here? / Apparently... / Can you mark it?
            CutPilot("lap");
            yield return SayM("TRV_AST", 7, 7);                                  // Already done. We can take a closer look, or continue to Biaveh and return.
            string pick = null;
            if (con != null)
            {
                CutPilot("ots");
                if (Auto) con.ScriptPick = DemoBoot.MarsPick ?? "asteroid";
                yield return con.ChooseOnStarMap(new[] { "asteroid", "biaveh" }, new[] { "Investigate the asteroid", "Continue to Biaveh" }, p => pick = p);
            }
            if (pick == null) pick = "asteroid";
            MarsChoice = pick;
            MarsDebriefDone = true;
            if (DemoBoot.CaptureDir != null) Cap?.Log($"TRV_AST choice: {pick}; MARS / ROUTES DONE: {MarsState.Summary()}; {WorldFlags.Summary()}");
            EndChairMoment("the departure routing ends");
            if (con != null) yield return con.ComingSoonCard($"{ShipConsole.WorldName(pick)} (coming soon)");
            yield return ExitStaging("the Mars departure routing ends");
            ShipConsole.StoryLock = false;
            yield return ExploreVenus(false);
        }
        /// <summary>The records together on the chair arm: the dodecahedron, the cube, Europa's icosahedron (once it was
        /// awarded) and Mars's small tetrahedron (once it was awarded).</summary>
        void PlaceRecordsOnArmMars()
        {
            if (Shape != null) { _carry?.Set(CarriedShape.Where.Away, 0f); PlaceShape(ArmRest, MarkingUp()); }
            if (_cubeCopy != null) { _cubeCopy.gameObject.SetActive(true); _cubeCopy.SetPositionAndRotation(ArmRest + PilotFwd * 0.14f, Quaternion.LookRotation(PilotFwd)); }
            if (EuropaState.RecordAwarded) { var ico = MakeIcosahedronCopy(); ico.SetPositionAndRotation(ArmRest - PilotFwd * 0.14f, Quaternion.LookRotation(PilotFwd)); }
            if (MarsState.RecordAwarded) { var tet = MakeTetrahedronCopy(); tet.SetPositionAndRotation(ArmRest - PilotFwd * 0.27f + Up * 0.02f, Quaternion.LookRotation(PilotFwd)); }
        }

        /// <summary>Mars's record: the small tetrahedron (generated like the others - pale stone).</summary>
        Transform MakeTetrahedronCopy()
        {
            var go = new GameObject("MarsRecordCopy");
            var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = Tetrahedron.Mesh(0.065f);
            var mr = go.AddComponent<MeshRenderer>();
            var src = Shape != null ? Shape.GetComponentInChildren<Renderer>() : null;
            if (src != null) mr.sharedMaterial = src.sharedMaterial;
            return go.transform;
        }
    }

    /// <summary>Round 15: Mars's rune - the tetrahedron (Dan), built in code like the cube, the dodecahedron and the icosahedron.</summary>
    public static class Tetrahedron
    {
        public static readonly Vector3[] V =
        {
            new Vector3(1f, 1f, 1f).normalized, new Vector3(1f, -1f, -1f).normalized, new Vector3(-1f, 1f, -1f).normalized, new Vector3(-1f, -1f, 1f).normalized,
        };
        public static readonly int[] F = { 0, 1, 2, 0, 3, 1, 0, 2, 3, 1, 3, 2 };
        /// <summary>A flat-shaded tetrahedron of the given circumradius, one vertex up (resting on a face).</summary>
        public static Mesh Mesh(float radius)
        {
            var up = Quaternion.FromToRotation(V[0], Vector3.up);     // one vertex up, resting on the opposite face
            var v = new System.Collections.Generic.List<Vector3>(); var tr = new System.Collections.Generic.List<int>();
            for (int i = 0; i < F.Length; i += 3)
            {
                int b = v.Count;
                v.Add(up * V[F[i]] * radius); v.Add(up * V[F[i + 1]] * radius); v.Add(up * V[F[i + 2]] * radius);
                tr.Add(b); tr.Add(b + 2); tr.Add(b + 1);
            }
            var m = new Mesh { name = "Tetrahedron" }; m.SetVertices(v); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.RecalculateBounds();
            // outward normals: flip the winding if the first face points inward
            if (Vector3.Dot(m.normals[0], (v[0] + v[1] + v[2]) / 3f) < 0f) { for (int i = 0; i < tr.Count; i += 3) { int x = tr[i + 1]; tr[i + 1] = tr[i + 2]; tr[i + 2] = x; } m.SetTriangles(tr, 0); m.RecalculateNormals(); }
            return m;
        }
        public static Vector3 Vertex(int i, float radius) => Quaternion.FromToRotation(V[0], Vector3.up) * V[i] * radius;
        public static readonly (int, int)[] Edges = { (0, 1), (0, 2), (0, 3), (1, 2), (1, 3), (2, 3) };
    }
}
