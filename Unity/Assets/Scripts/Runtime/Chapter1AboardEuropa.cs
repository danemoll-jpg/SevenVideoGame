using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 14 — the Saucer scene's Europa parts (Docs/CHAPTER_EUROPA.md):
    /// section 1 (<see cref="EuropaState.ArrivalAboard"/>): the title card over black from the first frame, then 7 in the
    /// pilot's chair with Panther already curled in her lap (her hand on top of his back, QF12), the navigation display
    /// insert, the lines - then the Europa scene (the dive);
    /// sections 16-17 (<see cref="EuropaState.HomeAboard"/>): helmet off, Panther waiting near the boarding area instead of
    /// his usual spot, she kneels beside him, his head against her hand, a quiet pause; later the debrief in the pilot's chair
    /// (already seated, Panther in her lap; Europa's record with the others), the purr, the two-shot held; then free
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
            yield return SitInPilotChair();
            Cat.LapFrame = ShipConsole.I.PilotSeat; Cat.enabled = true; yield return null; yield return null;
            Cat.PlaceOnLap();
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 0f;
            CutPilot("two");
            yield return UI.Fade(0f, 1.4f);
            yield return ShotV("ea01_pilot_chair_panther_in_her_lap");
            yield return SayE("EUR_01", 1, 4);                   // There's an ocean under all of this. / Apparently. / ... / I'm curious from here.
            // she laughs softly and strokes his back
            _stroke = 1f;
            CutPilot("lap");
            yield return Wait(1.2f);
            yield return ShotV("ea02_she_strokes_his_back");
            // insert: the navigation display - a passage through the ice near the temple coordinates
            NavHand(true);
            CutPilot("ots");
            UI.Hint("EUROPA  ·  ice field  ·  a passage through the ice near the temple coordinates", 4f);
            yield return Wait(1.0f);
            yield return ShotV("ea03_insert_navigation_display");
            yield return SayE("EUR_01", 5, 5);                   // There. A way down.
            Cat.LapNudge(-14f);                                  // Panther looks up at her
            CutPilot("lap");
            yield return SayE("EUR_01", 6, 7);                   // You said that rather enthusiastically. / Mm-hmm.
            NavHand(false);
            CutPilot("wide");
            yield return Wait(1.6f);
            if (_strokeRoutine != null) StopCoroutine(_strokeRoutine);
            _ik.LeftW = 0f;
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
            if (!EuropaState.SceneDone("EUR_16"))
            {
                // inside: she has come up the beam; the helmet comes off. Panther is waiting near the boarding area.
                var at = new Vector3(-1.2f, 0f, -4.6f); var catAt = new Vector3(0.4f, 0f, -3.5f);
                Seven.transform.SetPositionAndRotation(at, Quaternion.LookRotation(new Vector3(0.2f, 0f, 1f)));
                Cat.transform.SetPositionAndRotation(catAt, Quaternion.LookRotation(Flat(at - catAt)));
                CatClip("sit", 0.05f);
                var mid = (at + catAt) * 0.5f;
                CamTo(mid + new Vector3(2.6f, 1.35f, 2.6f), mid + Up * 0.7f, 0f, 44f);
                yield return UI.Fade(0f, 1.2f);
                // she removes her bubble helmet (a quick lift - the glass dome gone, a breath)
                AudioKit.Play("europa_bubbles", at + Up * 1.6f, 0.3f);
                yield return ShotV("ea10_helmet_off_panther_waiting");
                // she notices where he is
                yield return Turn(Seven.transform, catAt, 0.6f);
                yield return SayE("EUR_16", 1, 2);               // Were you waiting for the bike? / Someone had to inspect the damage.
                // she kneels beside him
                Seven.Frozen = false; bool sd = Seven.ScriptDriven; Seven.ScriptDriven = true;
                yield return AutoWalk(catAt + Flat(at - catAt).normalized * 0.75f);
                Seven.ScriptMove = Vector2.zero; Seven.ScriptDriven = sd; Seven.Frozen = true;
                if (Seven.HasState("crouch_idle")) Seven.PlayClip("crouch_idle", 0.5f);
                yield return Turn(Seven.transform, catAt, 0.4f);
                CamTo(catAt + new Vector3(1.6f, 0.8f, 1.2f), (catAt + Seven.transform.position) * 0.5f + Up * 0.5f, 0.8f, 42f);
                yield return SayE("EUR_16", 3, 3);               // No damage.
                // he presses his head against her hand
                var head = catAt + Up * 0.32f + Cat.transform.forward * 0.18f;
                _ik.RightPos = head + Up * 0.04f; _ik.RightW = 1f; _ik.LookPos = head; _ik.LookW = 0.8f;
                Cat.LapNudge(10f);
                yield return Wait(1.4f);
                yield return ShotV("ea11_his_head_against_her_hand");
                yield return Wait(1.0f);                          // a quiet pause
                yield return SayE("EUR_16", 4, 4);               // Good.
                yield return Wait(2.2f);                          // let her remain with him a moment before either moves
                _ik.RightW = 0f; _ik.LookW = 0f;
                EuropaState.CompleteScene("EUR_16");
                yield return UI.Fade(1f, 1.4f);
            }
            // later: the debrief in the pilot's chair, Panther in her lap (already seated), Europa's record with the others
            yield return SitInPilotChair();
            Cat.LapFrame = ShipConsole.I.PilotSeat; Cat.enabled = true; yield return null; yield return null;
            Cat.PlaceOnLap();
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
            var purr = AudioKit.Loop("cat_purr", null, null, 0.55f, AudioKit.Bus.Sfx);
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
            if (DemoBoot.CaptureDir != null) Cap?.Log($"EUROPA CHAPTER DONE: {EuropaState.Summary()}");
            yield return StandFromPilotChair();
            ShipConsole.StoryLock = false;
            yield return ExploreVenus(false);
        }

        IEnumerator PlaceRecordsOnArm()
        {
            // the dodecahedron, the small cube and now the small icosahedron rest together on the chair's arm
            if (Shape != null) { _carry?.Set(CarriedShape.Where.Away, 0f); PlaceShape(ArmRest, MarkingUp()); }
            if (_cubeCopy != null) { _cubeCopy.gameObject.SetActive(true); _cubeCopy.SetPositionAndRotation(ArmRest + PilotFwd * 0.14f, Quaternion.LookRotation(PilotFwd)); }
            var ico = MakeIcosahedronCopy();
            ico.SetPositionAndRotation(ArmRest - PilotFwd * 0.14f, Quaternion.LookRotation(PilotFwd));
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
