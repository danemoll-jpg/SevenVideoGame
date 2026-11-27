using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 14 — the Saucer scene's Europa parts (Docs/CHAPTER_EUROPA.md):
    /// section 1 (<see cref="EuropaState.ArrivalAboard"/>): the title card over black from the first frame, then 7 in the
    /// pilot's chair with Panther already curled in her lap (her hand on top of his back, QF12), the navigation display
    /// insert, the lines - then the Europa scene (the dive);
    /// sections 16-17 (<see cref="EuropaState.HomeAboard"/>): QF15 (3) - the reunion is cut: a short fade from the beam-up
    /// straight into the debrief in the pilot's chair (already seated, helmet off, Panther in her lap; Europa's record with
    /// the others), the purr, the two-shot held; then free
    /// exploration with the star map (Mars available, Europa done).
    /// </summary>
    public partial class Chapter1Aboard
    {
        public bool EuropaMode { get; private set; }
        public bool EuropaDebriefDone { get; private set; }

        void StartEuropa()
        {
            bool arrival = EuropaState.ArrivalAboard; EuropaState.ArrivalAboard = false; EuropaState.HomeAboard = false;
            I = this; Active = true; VenusMode = true; EuropaMode = true;
            Director.ChapterMode = true;
            Cat.RoamScale = 1f; Cat.LapScale = 1f; Cat.StrollSpeed = 1.0f; Cat.TrotSpeed = 1.6f; Cat.transform.localScale = Vector3.one;
            UI.SetFade(1f);                                                  // black from the very first frame
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
                Cap.Log($"aboard (Europa mode, {(arrival ? "arrival" : "home")}): {EuropaState.Summary()}");
                StartCoroutine(FirstFrameE());
            }
            StartCoroutine(arrival ? EuropaArrival() : EuropaHome());
        }
        IEnumerator FirstFrameE() { Cap.Log($"first frame aboard (Europa): fade {UI.FadeAlpha:0.00} (1 = black)"); yield return Cap.Shot("ea00_first_frame_aboard"); }

        IEnumerator SayE(string scene, int from, int to = -1, bool holdPause = true)
        {
            foreach (var l in EuropaDialogue.Scene(scene, from, to < 0 ? from : to))
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
        IEnumerator EuropaArrival()
        {
            ShipConsole.StoryLock = true;
            InGalleyScene = true;
            Seven.Frozen = true; Seven.Cutscene = true; Director.Cam.Hold = true;
            Cat.ForceTarget = null; Cat.enabled = false;
            // the chapter's title card, over black from the first frame
            MusicSystem.Play("europa_underwater", 3f);
            yield return UI.Title("EUROPA  —  BENEATH THE ICE", 1.0f, 2.4f, 1.0f);
            if (DemoBoot.CaptureDir != null) Cap?.Log("title card: 'EUROPA — BENEATH THE ICE' over black");
            // 7 sits with Panther in her lap (already settled - no hop); ahead, the Elarim's coordinates beneath the surface
            yield return EnterStaging("EUR_01");                                // QF23 (3): the pilot's chair at the front window, Panther in her lap
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 0.6f;
            CutPilot("two");
            yield return UI.Fade(0f, 1.4f);
            yield return ShotV("ea01_pilot_chair_panther_in_her_lap");
            // QF23 (3): as they talk about the world below, the hull fades away round them (the vanish-ship effect)
            StartCoroutine(HullAway(true, "EUR_01", "ea01h"));
            yield return SayE("EUR_01", 1, 4);                   // There's an ocean under all of this. / Apparently. / ... / I'm curious from here.
            // she laughs softly and strokes his back
            _stroke = 1f;
            CutPilot("lap");
            yield return Wait(1.2f);
            yield return ShotV("ea02_she_strokes_his_back");
            // insert: the navigation display - a passage through the ice near the temple coordinates
            NavHand(true);
            CutPilot("ots");
            // QF18 (2): the display SHOWS it - a hologram of Europa's actual ice field, the passage through the ice marked, the
            // temple's coordinates marked below it
            var holo = HoloDisplay.Get();
            if (holo != null)
            {
                holo.Show("europa", 1.15f, 560f, new Vector3(-60f, 0f, 90f));
                holo.Mark(EuropaLayout.OpeningAt, 12f, "the passage through the ice");
                holo.Mark(EuropaLayout.TempleCentre, 14f, "the temple coordinates", false);
                Cut(holo.transform.position + PilotFwd * -0.95f + PilotRight * 0.3f + Up * 0.32f, holo.transform.position, 38f);
            }
            yield return Wait(1.0f);
            yield return ShotV("ea03_insert_navigation_display");
            yield return DialogueFraming.Over("the passage through the ice on the display (QF24 7)", SayE("EUR_01", 5, 5));   // There. A way down.
            Cat.LapNudge(-14f);                                  // Panther looks up at her
            CutPilot("lap");
            yield return SayE("EUR_01", 6, 7);                   // You said that rather enthusiastically. / Mm-hmm.
            NavHand(false);
            holo?.Hide();
            CutPilot("wide");
            yield return HullAway(false, "EUR_01");                             // the hull comes back as the talk ends
            if (_strokeRoutine != null) StopCoroutine(_strokeRoutine);
            _ik.ReleaseAll("EUR_01 ends");
            EuropaState.CompleteScene("EUR_01");
            yield return UI.Fade(1f, 1.2f);
            if (DemoBoot.CaptureDir != null) Cap?.Log("Europa arrival aboard done -> the Europa scene (the saucer over the ice field, the beam, the dive)");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Europa");
        }

        // ------------------------------------------------------------------------------------------ sections 16-17
        IEnumerator EuropaHome()
        {
            ShipConsole.StoryLock = true;
            InGalleyScene = true;
            Seven.Frozen = true; Seven.Cutscene = true; Director.Cam.Hold = true;
            Cat.ForceTarget = null; Cat.enabled = false;
            MusicSystem.Play("aboard_saucer", 2.5f);
            // QF15 (3) - Dan: the reunion at the boarding area (helmet off, Panther waiting, the kneel, his head against her hand,
            // "Good.") felt like a lock-up and is cut entirely (EUR_16 removed). The beam-up fades straight into the debrief: 7
            // already seated in the pilot's chair, helmet off, Panther in her lap.
            if (UI.FadeAlpha < 0.99f) yield return UI.Fade(1f, 0.6f);
            // later: the debrief in the pilot's chair, Panther in her lap (already seated), Europa's record with the others
            yield return EnterStaging("EUR_17");                                // QF16 (3): on the floor, back against the sofa
            yield return PlaceRecordsOnArm();
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 1f;
            CutPilot("two");
            yield return UI.Fade(0f, 1.4f);
            yield return ShotV("ea12_debrief_in_the_chair");
            CutPilot("objects");
            yield return Wait(1.0f);
            yield return ShotV("ea13_the_three_records_on_the_arm");
            CutPilot("side");
            yield return SayE("EUR_17", 1, 1);                   // They teach their history through sound.
            Cat.LapNudge(-6f);                                    // a thoughtful hum
            yield return SayE("EUR_17", 2, 2);
            yield return Wait(0.6f);                              // she chuckles
            yield return SayE("EUR_17", 3, 3);
            _stroke = 0.4f;                                       // her hand slows against his fur
            yield return SayE("EUR_17", 4, 5);                   // How could I know that? / I don't know.
            // he begins to purr; she looks down at him, listening
            var purr = Purr.Loop(Cat.transform, 1f)   /* QF23 (11): loud, on the voice bus */;
            CutPilot("lap");
            _ik.LookPos = Seven.LapPoint(0.1f); _ik.LookW = 0.8f;
            yield return Wait(2.0f);
            yield return ShotV("ea14_he_purrs");
            yield return SayE("EUR_17", 6, 6);                   // That one I know.
            _stroke = 1f;                                         // her hand resumes
            CutPilot("two");
            yield return Wait(3.2f);                              // hold the two-shot before returning to navigation
            yield return ShotV("ea15_hold_the_two_shot");
            if (purr != null) StartCoroutine(AudioKit.Fade(purr, 0f, 2.5f, true));
            EuropaState.CompleteScene("EUR_17");
            EuropaState.Reach(EuropaLayout.CpDone);
            EuropaDebriefDone = true;
            if (DemoBoot.CaptureDir != null) Cap?.Log($"EUROPA CHAPTER DONE: {EuropaState.Summary()}; {WorldFlags.Summary()}");
            // Round 15 (Dan's revised MAR_13): Europa finished second - the combined-record discovery plays here, once
            var eurTalk = WorldFlags.EuropaEndTalk;                               // QF38 (8): the one selector (BranchAudit)
            if (DemoBoot.CaptureDir != null) Cap?.Log($"BRANCH (QF38 8) the end of Europa: {eurTalk} ({WorldFlags.Summary()})");
            if (eurTalk == "MAR_13C") { yield return CombinedRecords("the end of Europa (Europa finished second)"); yield break; }
            // QF13 (15): the petting moment ends here - its stroke loop owned her left hand for as long as it ran (it was never
            // stopped, so after standing up her arm kept reaching for the cat wherever she walked)
            _stroke = 0f; yield return Wait(0.4f);
            if (_strokeRoutine != null) { StopCoroutine(_strokeRoutine); _strokeRoutine = null; }
            _ik.ReleaseAll("the Europa debrief ends");
            yield return ExitStaging("the Europa debrief ends");
            if (DemoBoot.CaptureDir != null)
            {
                // QF13 (15) check: after standing up she walks off - her arms hang naturally (no hand still reaching for the cat)
                Seven.Frozen = false; bool sd = Seven.ScriptDriven; Seven.ScriptDriven = true;
                var to = Seven.transform.position + Seven.transform.forward * 3.5f;
                CamTo(Seven.transform.position + Seven.transform.right * 3.2f + Up * 1.4f + Seven.transform.forward * 1.5f, Seven.transform.position + Up * 1.0f + Seven.transform.forward * 1.5f, 0.3f, 48f);
                var walk = StartCoroutine(AutoWalk(to));
                yield return Wait(1.6f);
                Cap?.Log($"arms after the debrief (QF13 15): hand IK weights left {_ik.LeftW:0.00} right {_ik.RightW:0.00} look {_ik.LookW:0.00}; safety-net releases so far {SevenTableIK.NetReleases}");
                yield return ShotV("qf13_15b_after_the_debrief_walking_arms_free");
                yield return walk;
                Seven.ScriptMove = Vector2.zero; Seven.ScriptDriven = sd;
            }
            ShipConsole.StoryLock = false;
            yield return ExploreVenus(false);
        }

        IEnumerator PlaceRecordsOnArm()
        {
            // QF23 (23): every collected record rests together on the chair's arm (the one saved list - Mars's too if it is done)
            PlaceRecords("the chair arm");
            yield return null;
        }

        /// <summary>Europa's record: the small icosahedron (generated like the dodecahedron and the cube - pale stone).</summary>
        Transform MakeIcosahedronCopy()
        {
            var go = new GameObject("EuropaRecordCopy");
            var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = Icosahedron.Mesh(0.06f);
            var mr = go.AddComponent<MeshRenderer>();
            var src = Shape != null ? Shape.GetComponentInChildren<Renderer>() : null;
            if (src != null) mr.sharedMaterial = src.sharedMaterial;
            return go.transform;
        }
    }

    /// <summary>Round 14: Europa's rune - the icosahedron (Dan: Venus = cube, the Moon = dodecahedron), built in code.</summary>
    public static class Icosahedron
    {
        public static readonly Vector3[] V;
        public static readonly int[] F =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
        };
        static Icosahedron()
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            V = new[] { new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0), new Vector3(0, -1, t), new Vector3(0, 1, t),
                        new Vector3(0, -1, -t), new Vector3(0, 1, -t), new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1) };
            for (int i = 0; i < V.Length; i++) V[i] = V[i].normalized;
        }
        /// <summary>A flat-shaded icosahedron of the given circumradius (each face its own vertices: crisp edges).</summary>
        public static Mesh Mesh(float radius)
        {
            var v = new System.Collections.Generic.List<Vector3>(); var tr = new System.Collections.Generic.List<int>();
            for (int i = 0; i < F.Length; i += 3) { int b = v.Count; v.Add(V[F[i]] * radius); v.Add(V[F[i + 1]] * radius); v.Add(V[F[i + 2]] * radius); tr.Add(b); tr.Add(b + 1); tr.Add(b + 2); }
            var m = new Mesh { name = "Icosahedron" }; m.SetVertices(v); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
        /// <summary>Its 30 edges (index pairs into <see cref="V"/>).</summary>
        public static System.Collections.Generic.List<(int, int)> Edges()
        {
            var l = new System.Collections.Generic.List<(int, int)>();
            for (int i = 0; i < F.Length; i += 3) for (int k = 0; k < 3; k++) { int a = F[i + k], b = F[i + (k + 1) % 3]; var e = a < b ? (a, b) : (b, a); if (!l.Contains(e)) l.Add(e); }
            return l;
        }
    }
}
