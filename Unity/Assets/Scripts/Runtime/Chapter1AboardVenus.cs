using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 12 — the Saucer scene's Venus mode (<see cref="VenusState.VenusAboardStart"/>): VEN_C11, the debrief in the
    /// galley after the ride back (Panther at normal size near the entrance, he follows her to the table, the dodecahedron
    /// and the small cube set together on the table, the table-height two-shot, the open-hand insert, the final wide pause),
    /// then free exploration where "Set course" opens the destination screen (the Moon, Venus; nothing invented, nothing
    /// forced). VEN_S08_COMPLETE. Reuses the galley scene's seat, IK, cat pose and camera helpers (Chapter1Aboard).
    /// </summary>
    public partial class Chapter1Aboard
    {
        public bool VenusMode { get; private set; }
        public bool DestinationsShown { get; private set; }
        public DestinationScreen Dest { get; private set; }
        Transform _cubeCopy;

        void StartVenus()
        {
            I = this; Active = true; VenusMode = true;
            Director.ChapterMode = true;
            Cat.RoamScale = 1f; Cat.LapScale = 1f; Cat.StrollSpeed = 1.0f; Cat.TrotSpeed = 1.6f; Cat.transform.localScale = Vector3.one;   // Panther at normal size aboard
            UI.SetFade(1f);
            if (FridgeLight != null) FridgeLight.enabled = false;
            _ik = Seven.gameObject.AddComponent<SevenTableIK>();
            _focus = Focus != null ? Focus : new GameObject("GalleyFocus").AddComponent<CloseUpFocus>();
            StackSubtitleCamera();
            _catPose = Cat.gameObject.AddComponent<CatPoseOverlay>();
            if (Moon != null) { _moonHome = Moon.position; _moonScale = Moon.localScale; }
            if (Fork != null) { _forkHome = Fork.position; _forkHomeRot = Fork.rotation; }
            VenusOutside();
            if (Shape != null) { _carry = CarriedShape.Attach(Seven.gameObject, Shape.transform); _carry.Set(CarriedShape.Where.Pocket, 0f); }
            _cubeCopy = MakeCubeCopy();
            AudioKit.Loop("amb_saucer", null, null, 1f, AudioKit.Bus.Ambience);
            if (DemoBoot.CaptureDir != null)
            {
                ChapterCapture.Ensure(); Cap.Attach(Director.Cam.GetComponent<Camera>());
                if (Auto) { Seven.ScriptDriven = true; Dlg.AutoSkipAfter = DemoBoot.VenusSkip ? 0f : 0.9f; Director.Legend.AutoSwitch = false; }
                Cap.Log($"aboard (Venus mode): {VenusState.Summary()}");
            }
            FindEnergyPoint();
            StartCoroutine(VenusState.SceneDone("VEN_C11") ? ExploreVenus(true) : ShipConsole.I != null && ShipConsole.I.PilotSeat != null ? VenusDebriefChair() : VenusDebrief());   // Round 13: C11 + C13 in the pilot's chair
        }

        /// <summary>Landed on Venus: no Moon and no stars outside the porthole - the bright mist of the gardens.</summary>
        void VenusOutside()
        {
            if (Moon != null) Moon.gameObject.SetActive(false);
            if (GalleyOutside == null) return;
            foreach (var r in GalleyOutside.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == "Stars") { r.enabled = false; continue; }
                // a soft pale mist (dim: this surface is large and wraps the nook - only the porthole should show it)
                var m = r.material; var c = new Color(0.42f, 0.42f, 0.42f);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white * 0.18f);
            }
        }

        /// <summary>The small cube (Venus's record copy) for the table: pale stone like the dodecahedron, about 15 cm.</summary>
        Transform MakeCubeCopy()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "VenusRecordCopy";
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * 0.1f;
            var src = Shape != null ? Shape.GetComponentInChildren<Renderer>() : null;
            if (src != null) go.GetComponent<Renderer>().sharedMaterial = src.sharedMaterial;
            go.SetActive(false);
            return go.transform;
        }

        IEnumerator SayV(string scene, int from, int to = -1, bool holdPause = true)
        {
            foreach (var l in VenusDialogue.AsVoiceLines(scene, from, to < 0 ? from : to))
            {
                bool talkClip = false;
                if (!l.IsCat) { _ik.Talk = 1f; if (_seated && _ik.RightW < 0.05f && _ik.LeftW < 0.05f && Seven.HasState("sit_talking")) { talkClip = true; Seven.PlayClip("sit_talking", 0.5f); } }
                else if (_catPose != null) _catPose.LookAt = SevenEyes;
                float t0 = Time.time;
                yield return Dlg.PlayOne(l, false);
                _ik.Talk = 0f; if (talkClip) Seven.PlayClip(_seated ? _seatClip : "idle", 0.6f);
                if (l.IsCat && _catPose != null) _catPose.LookAt = null;
                if (Auto || DemoBoot.PlayerSim) Cap?.Log($"line {l.Id} ({l.Label ?? l.Speaker}) played {Time.time - t0:0.00}s of {(l.Clip != null ? l.Clip.length : 0f):0.00}s: '{l.Text}'");
                if (holdPause) yield return Wait(0.45f);
            }
        }

        IEnumerator ShotV(string name) { if (DemoBoot.CaptureDir != null && Cap != null) yield return Cap.Shot(name); }

        IEnumerator VenusDebrief()
        {
            InGalleyScene = true;
            MusicSystem.Play("aboard_saucer", 2.5f);
            Seven.Frozen = true; Seven.Cutscene = true;
            Director.Cam.Hold = true;
            Cat.ForceTarget = null; Cat.enabled = false;
            // she comes in; Panther at normal size near the entrance
            Seven.transform.SetPositionAndRotation(new Vector3(-1.2f, 0f, -4.6f), Quaternion.LookRotation(new Vector3(0.2f, 0f, 1f)));
            Cat.transform.SetPositionAndRotation(new Vector3(0.4f, 0f, -3.5f), Quaternion.LookRotation(Flat(Seven.transform.position - new Vector3(0.4f, 0f, -3.5f))));
            CatClip("sit", 0.05f);
            var mid = (Seven.transform.position + Cat.transform.position) * 0.5f;
            CamTo(mid + new Vector3(2.6f, 1.35f, 2.6f), mid + Up * 0.7f, 0f, 44f);
            yield return Wait(0.3f);
            yield return UI.Fade(0f, 1.4f);
            yield return ShotV("c11_01_panther_near_the_entrance");
            yield return SayV("VEN_C11", 1);                                     // Did they know what it was?
            // follow 7 to the table (the camera tracks her; he walks along beside)
            Seven.Frozen = false; bool sd = Seven.ScriptDriven; Seven.ScriptDriven = true;
            StartCoroutine(CatWalkTo(Flat(TableCentre - TableAlong * 0.75f - TableOut * 0.55f), TableAlong, 4.0f));
            var walk = StartCoroutine(AutoWalk(SeatStand));
            for (float t = 0f; t < 6f && Flat(Seven.transform.position - SeatStand).magnitude > 0.35f; t += Time.deltaTime)
            {
                var p = Seven.transform.position; Director.Cam.transform.position = Vector3.Lerp(Director.Cam.transform.position, p + Flat(Director.Cam.transform.position - p).normalized * 3.2f + Up * 1.5f, Time.deltaTime * 2f);
                Director.Cam.transform.rotation = Quaternion.Slerp(Director.Cam.transform.rotation, Quaternion.LookRotation(p + Up * 1.1f - Director.Cam.transform.position), Time.deltaTime * 4f);
                if (t > 1.5f && t - Time.deltaTime <= 1.5f) StartCoroutine(ShotV("c11_02_follow_her_to_the_table"));
                yield return null;
            }
            StopCoroutine(walk); Seven.ScriptMove = Vector2.zero; Seven.ScriptDriven = sd; Seven.Frozen = true;
            yield return SayV("VEN_C11", 2);                                     // The Moon's record. Venus has its own.
            yield return SitAtTable();
            // he hops onto the table
            var hop0 = Cat.transform.position; CatClip("jump", 0.1f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.75f) { var p = Vector3.Lerp(hop0, CatSitOnTable, t); p.y = Mathf.Lerp(hop0.y, CatSitOnTable.y, Mathf.SmoothStep(0f, 1f, t)) + Mathf.Sin(t * Mathf.PI) * 0.35f; Cat.transform.position = p; yield return null; }
            Cat.transform.position = CatSitOnTable; Cat.transform.rotation = Quaternion.LookRotation(Flat(Seven.transform.position - CatSitOnTable)); CatClip("sit", 0.35f);
            // the dodecahedron and the cube, set together on the table
            yield return SetBothOnTable();
            Cut(TableCentre + TableAlong * 0.05f - TableOut * 0.55f + Up * (TableTopY + 0.16f), (BesidePlate + CubeSpot) * 0.5f, 34f, null, 0.6f, 50f, 3f);
            yield return Wait(1.0f);
            yield return ShotV("c11_03_insert_both_objects");
            // the table-height two-shot
            Cut(TableCentre - TableOut * 2.0f + Up * (TableTopY + 0.32f) + TableAlong * 0.1f, TableCentre + Up * (TableTopY + 0.35f) + TableAlong * 0.1f, 46f);
            yield return SayV("VEN_C11", 3, 5);                                  // And the people you saw? / less certain / something else
            yield return ShotV("c11_04_table_height_two_shot");
            yield return SayV("VEN_C11", 6, 7);                                  // What did you see? / Someone giving me a seed.
            // open-hand insert: her hand open, palm up on the table
            _ik.RightPos = TableCentre + TableAlong * 0.32f + Up * (TableTopY + 0.04f) + TableOut * 0.05f; _ik.RightW = 1f; _ik.LookPos = _ik.RightPos; _ik.LookW = 0.7f;
            yield return Wait(0.7f);
            var palm = Palm(true);
            Cut(palm - TableOut * 0.45f + Up * 0.32f - TableAlong * 0.1f, palm, 34f, null, 0.55f, 50f, 3f);
            yield return Wait(0.6f);
            yield return ShotV("c11_05_open_hand_insert");
            yield return Wait(0.8f);
            TwoShot(0.8f);
            yield return SayV("VEN_C11", 8, 8, false);                           // I wish I could remember their face.
            // Panther settles closer. Hold the silence.
            _ik.RightW = 0f;
            yield return CatWalkTo(CatStudySpot, Flat(Seven.transform.position - CatStudySpot), 1.4f);
            CatClip("sit", 0.4f);
            yield return Silence(2.6f, "VEN_C11_08");
            yield return ShotV("c11_06_he_settles_closer_silence");
            yield return SayV("VEN_C11", 9, 10);                                 // There are other temples. / Then we have somewhere else to ask.
            // the wide two-shot for the final pause
            Cut(TableCentre - TableOut * 3.4f + Up * 1.9f + TableAlong * 0.2f, TableCentre + Up * 0.85f, 52f);
            yield return Wait(3.2f);
            yield return ShotV("c11_07_wide_final_pause");
            bool first = VenusState.CompleteScene("VEN_C11", VenusState.Stage.Complete);
            if (DemoBoot.CaptureDir != null) Cap?.Log($"scene VEN_C11 complete{(first ? "" : " (already)")}: {VenusState.CurrentId}; on the table: the dodecahedron at {Shape?.transform.position}, the cube at {_cubeCopy?.position}");
            yield return UI.Fade(1f, 1.4f);
            yield return StandFromTable();
            Cat.transform.SetPositionAndRotation(Flat(TableCentre - TableAlong * 0.9f - TableOut * 0.6f), Quaternion.LookRotation(-TableOut));
            yield return ExploreVenus(false);
        }

        Vector3 CubeSpot => TableCentre + TableAlong * 0.08f + TableOut * 0.2f + Up * (TableTopY + 0.05f);
        IEnumerator SetBothOnTable()
        {
            if (_carry != null && Shape != null)
            {
                _ik.RightPos = _carry.HipPoint + Up * 0.03f; _ik.RightW = 1f; _ik.LookPos = _carry.HipPoint; _ik.LookW = 0.55f;
                yield return Wait(0.8f);
                _carry.Set(CarriedShape.Where.Hand, 0.25f);
                yield return Wait(0.3f);
                _ik.RightPos = BesidePlate + Up * 0.12f; _ik.LookPos = BesidePlate; yield return Wait(0.7f);
                var held = Shape.transform.position; var heldR = Shape.transform.rotation;
                _carry.Set(CarriedShape.Where.Away, 0f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.25f) { Shape.transform.SetPositionAndRotation(Vector3.Lerp(held, BesidePlate, t), Quaternion.Slerp(heldR, MarkingUp(), t)); yield return null; }
                PlaceShape(BesidePlate, MarkingUp());
            }
            // and the small cube from her pocket, set right beside it
            _ik.RightPos = (_carry != null ? _carry.HipPoint : Seven.transform.position + Up) + Up * 0.03f; yield return Wait(0.7f);
            if (_cubeCopy != null)
            {
                _cubeCopy.gameObject.SetActive(true);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f) { _cubeCopy.position = Palm(true) + Up * 0.06f; _ik.RightPos = Vector3.Lerp(_ik.RightPos, CubeSpot + Up * 0.1f, t); yield return null; }
                _ik.RightPos = CubeSpot + Up * 0.08f; yield return Wait(0.4f);
                _cubeCopy.SetPositionAndRotation(CubeSpot, Quaternion.LookRotation(TableAlong));
                AudioKit.Play("fork_clink", CubeSpot, 0.18f, 0.2f, AudioKit.Bus.Sfx, 6f);
            }
            _ik.RightW = 0f; _ik.LookW = 0f;
        }

        IEnumerator ExploreVenus(bool fromContinue)
        {
            if (Shape != null) PlaceShape(BesidePlate, MarkingUp());
            if (_cubeCopy != null) { _cubeCopy.gameObject.SetActive(true); _cubeCopy.SetPositionAndRotation(CubeSpot, Quaternion.LookRotation(TableAlong)); }
            if (fromContinue) Seven.transform.SetPositionAndRotation(new Vector3(-1.2f, 0f, -4.6f), Quaternion.LookRotation(new Vector3(0.2f, 0f, 1f)));
            Seven.Frozen = false; Seven.Cutscene = false;
            Director.Cam.Hold = false; Director.ChapterMode = false; Director.PromptOverride = null;
            Cat.ForceAction = null; Cat.ForceTarget = null; Cat.enabled = true;
            InGalleyScene = false; Explored = true;
            MusicSystem.Play("aboard_saucer", 3f);
            yield return UI.Fade(0f, 1.4f);
            StartCoroutine(SetCourseWatch());
            Objective("Set course");
            if (DemoBoot.CaptureDir != null) Cap?.Log($"aboard after Venus: free exploration; 'Set course' at her galley seat opens the destination screen ({VenusState.CurrentId})");
            yield return Wait(6f);
            Objective(null);
            if (!Auto) yield break;
            if (DemoBoot.VenusSelectTest) { yield return Cap.Shot("sel_C11_aboard"); Cap.Finish("SCENE SELECT TEST (Venus) done"); yield break; }
            Seven.ScriptDriven = true;
            yield return AutoWalk(SeatStand);
            yield return Wait(0.6f);
            yield return SetCourse();
        }

        /// <summary>B9: the destination screen. Venus = a return visit (its changed state kept); the Moon = Chapter 1 from its
        /// start; Stay aboard = back to exploring.</summary>
        IEnumerator Destinations()
        {
            _settingCourse = true; Director.ExtraPrompt = null; Seven.Frozen = true;
            yield return Turn(Seven.transform, Porthole.position, 0.5f);
            CamTo(SeatStand - TableAlong * 0.2f - TableOut * 0.9f + Up * 1.55f, Porthole.position, 1.0f, 44f);
            yield return Wait(1.0f);
            Dest = gameObject.GetComponent<DestinationScreen>() ?? gameObject.AddComponent<DestinationScreen>();
            Dest.ScriptInput = Auto;
            DestinationsShown = true;
            var show = StartCoroutine(Dest.Show(Director.Cam.GetComponent<Camera>()));
            yield return null; yield return null;
            if (DemoBoot.CaptureDir != null) { Cap?.Log($"DESTINATION SCREEN: {string.Join(" / ", System.Array.ConvertAll(DestinationScreen.Items, x => x.label))} (only the worlds that exist; nothing forced) - {VenusState.CurrentId}"); yield return ShotV("c11_08_destination_screen"); }
            if (Auto) { yield return Wait(0.5f); Dest.Pick("stay"); }
            while (Dest.Open) yield return null;
            var c = Dest.Choice;
            if (DemoBoot.CaptureDir != null) Cap?.Log($"destination chosen: {c}");
            if (DemoBoot.CaptureDir != null && Auto)
            {
                VenusState.ClearTestSave();
                Cap.Finish($"VENUS CHAPTER DONE: {VenusState.CurrentId}, record awards {VenusState.RecordAwards}, devices {VenusState.DevicesDisabled.Count}, contained {VenusState.WormmasterContained}, destination screen shown");
                yield break;
            }
            if (c == "venus") { yield return TravelPassage.Run("venus"); yield return UI.Fade(1f, 1f); UnityEngine.SceneManagement.SceneManager.LoadScene("Venus"); yield break; }
            if (c == "moon") { yield return TravelPassage.Run("moon"); yield return UI.Fade(1f, 1f); VenusState.UseOwnSave(); Chapter1State.StartAt(Chapter1State.CpCell); yield break; }
            Director.Cam.Hold = false; Seven.Frozen = false; _settingCourse = false;
        }

        // ------------------------------------------------------------------------------------------ Round 13 (0j + 0m)
        // VEN_C11 + VEN_C13 in the pilot's chair at the navigation console, Panther curled in her lap (Dan, 2026-11-19):
        // the debrief no longer happens at the galley table. One hand rests on his back (stroking), the other on the
        // navigation controls; the dodecahedron and the small cube lie on the chair's arm for the insert; then the
        // star map brings Mars and Europa close and the player chooses. Mars / Europa chapters do not exist yet: after the
        // journey begins, a "coming soon" card, then free exploration (both stay selectable on the star map).
        bool _inPilotChair;
        Vector3 PilotHips => ShipConsole.I.PilotSeat.position;
        Vector3 PilotFwd => Flat(ShipConsole.I.PilotSeat.forward).normalized;
        Vector3 PilotRight => Vector3.Cross(Vector3.up, PilotFwd);
        Vector3 PilotHead => PilotHips + Up * 0.66f;
        Vector3 ArmRest => PilotHips + PilotRight * 0.42f + Up * 0.12f + PilotFwd * 0.12f;

        IEnumerator SitInPilotChair()
        {
            var seat = ShipConsole.I.PilotSeat;
            Seven.transform.SetPositionAndRotation(Flat(seat.position) + PilotFwd * 0.1f, Quaternion.LookRotation(PilotFwd));
            _seatClip = Seven.HasState("sit_idle") ? "sit_idle" : "sitting_idle";
            Seven.PlayClip(_seatClip, 0.3f);
            Seven.SeatHips = seat.position;
            _seated = true; _inPilotChair = true;
            yield return Wait(0.6f);
            if (DemoBoot.CaptureDir != null) Cap?.Log($"pilot's chair (0j): seated ({_seatClip}), hips {Vector3.Distance(Seven.Hips.position, seat.position):0.000} m from the seat point");
        }

        IEnumerator StandFromPilotChair()
        {
            // QF13 (15): standing up ends every seated hand moment (petting, the console, the armrest) - stop the stroke loop
            _stroke = 0f; if (_strokeRoutine != null) { StopCoroutine(_strokeRoutine); _strokeRoutine = null; }
            if (_ik != null) _ik.ReleaseAll("she stands up from the pilot's chair");
            if (Cat.Current == CatCompanion.Mode.OnLap) { Cat.HopOffLap(Flat(PilotHips + PilotRight * 1.0f - PilotFwd * 0.3f)); yield return Wait(Cat.HopTime + 0.1f); }
            Cat.LapFrame = null;
            _seated = false; _inPilotChair = false;
            Seven.SeatHips = null;
            Seven.transform.SetPositionAndRotation(Flat(PilotHips - PilotFwd * 0.75f), Quaternion.LookRotation(-PilotFwd));
            Seven.PlayClip("idle", 0.2f);
            Seven.Cutscene = false; Seven.Frozen = false;
            yield return null;
        }

        /// <summary>Her left hand on his back, stroking slowly (IK); 0 = hand off.</summary>
        float _stroke; Coroutine _strokeRoutine; int _strokeLogN;
        IEnumerator Stroke()
        {
            for (float t = 0f; ; t += Time.deltaTime)
            {
                // QF12 (10): her palm on the TOP of his back (measured from his drawn body), stroking along it - not her knee
                float along = Mathf.Sin(t * 1.6f) * 0.45f;
                var back = Cat != null ? Cat.BackTop(along, PilotFwd) : Seven.LapPoint(0.13f);
                _ik.LeftPos = back - Up * 0.025f;          // the wrist a little below the top: the palm (measured ~4.5 cm above the wrist here) lies on his fur
                if (DemoBoot.CaptureDir != null && _ik.LeftW > 0.95f && (_strokeLogN++ % 90) == 0)
                {
                    var palm = Palm(false);
                    var top = Cat != null ? Cat.BackTop(0f, PilotFwd) : back;
                    Cap?.Log($"petting (QF12 10): her left palm {palm.y - top.y:+0.00;-0.00} m vs the top of his back, {Flat(palm - top).magnitude:0.00} m from its middle; her lap {Seven.LapPoint(0f).y - top.y:+0.00;-0.00} m (palm {(Mathf.Abs(palm.y - top.y) < 0.07f && Flat(palm - top).magnitude < 0.2f ? "on his back" : "OFF his back")})");
                }
                _ik.LeftW = Mathf.MoveTowards(_ik.LeftW, _staged && CurrentStage != Stage.Chair && CurrentStage != Stage.ChairCatConsole ? 0f : _stroke, Time.deltaTime * 2f);   // QF16: the rest poses keep her hand on him
                yield return null;
            }
        }
        void NavHand(bool on) { if (_staged && CurrentStage != Stage.Chair && CurrentStage != Stage.ChairCatConsole) on = false; if (on && ShipConsole.I != null && ShipConsole.I.Display != null) { _ik.RightPos = PilotHips + PilotFwd * 0.55f + PilotRight * 0.28f + Up * 0.28f; _ik.RightW = 0.9f; } else _ik.RightW = 0f; }

        void CutPilot(string kind)
        {
            if (CutStage(kind)) return;                                            // QF16 (3): the staging's own framings
            var disp = ShipConsole.I.Display != null ? ShipConsole.I.Display.position : PilotHead + PilotFwd * 2f;
            switch (kind)
            {
                case "ots": Cut(PilotHead - PilotFwd * 0.75f + PilotRight * 0.38f + Up * 0.18f, disp, 42f); break;
                case "side": Cut(PilotHips - PilotFwd * 1.0f + PilotRight * 1.8f + Up * 0.75f, PilotHips + Up * 0.35f + PilotFwd * 0.2f, 46f); break;   // from the deck side (the desk pods block the front)
                case "two": Cut(PilotHips + PilotFwd * 0.8f + PilotRight * 1.5f + Up * 1.45f, PilotHips + Up * 0.3f, 48f); break;   // high, over the desk
                case "lap": { var lap = Seven.LapPoint(0.1f); Cut(lap + PilotRight * 0.55f + PilotFwd * 0.45f + Up * 0.25f, lap, 34f, null, 0.6f, 50f, 3f); break; }
                case "objects": Cut(ArmRest + PilotRight * 0.35f + PilotFwd * 0.35f + Up * 0.3f, ArmRest, 32f, null, 0.5f, 50f, 3f); break;
                case "wide": Cut(PilotHips - PilotFwd * 3.2f + Up * 1.3f + PilotRight * 0.3f, disp, 50f); break;
            }
        }

        IEnumerator VenusDebriefChair()
        {
            ShipConsole.StoryLock = true;                                         // the console never opens during the scene
            InGalleyScene = true;
            MusicSystem.Play("aboard_saucer", 2.5f);
            Seven.Frozen = true; Seven.Cutscene = true;
            Director.Cam.Hold = true;
            Cat.ForceTarget = null; Cat.enabled = false;
            // QF12 (11, Dan): back on the ship after Venus they are ALREADY in the pilot's chair - 7 seated, Panther curled
            // in her lap - when the picture comes up (no walk to the chair, no hop). C11's first two lines play there.
            yield return EnterStaging("VEN_C11");                               // QF16 (3): both lying on the sofa
            yield return null;
            CutPilot("two");
            yield return UI.Fade(0f, 1.4f);
            if (DemoBoot.CaptureDir != null) Cap?.Log($"C11 opens in the pilot's chair (QF12 11): seated {_seated}, hips {Vector3.Distance(Seven.Hips.position, ShipConsole.I.PilotSeat.position):0.000} m from the seat point, Panther on her lap {Cat.Current == CatCompanion.Mode.OnLap} (lap mesh {Cat.LapMeshShown}) from the first frame");
            yield return ShotV("c11_01_opens_in_the_chair_panther_on_her_lap");
            yield return SayV("VEN_C11", 1);                                     // Did they know what it was?
            yield return SayV("VEN_C11", 2);                                     // The Moon's record. Venus has its own.
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 1f;
            CutPilot("two");
            yield return Wait(0.8f);
            yield return ShotV("c11_02b_pilot_chair_panther_in_her_lap");
            // the dodecahedron and the small cube, set on the chair's arm
            if (CurrentStage == Stage.Chair) yield return SetBothOnArm();
            CutPilot("objects");
            yield return Wait(1.0f);
            yield return ShotV("c11_03_insert_both_objects");
            CutPilot("side");
            yield return SayV("VEN_C11", 3, 5);                                  // And the people you saw? / ...
            yield return ShotV("c11_04_side_two_shot");
            yield return SayV("VEN_C11", 6, 7);                                  // What did you see? / Someone giving me a seed.
            // open-hand insert: her right hand open, palm up on the chair's arm (in the chair staging; on the sofa: her face)
            if (CurrentStage == Stage.Chair)
            {
                _ik.RightPos = ArmRest - PilotRight * 0.08f + PilotFwd * 0.1f; _ik.RightW = 1f; _ik.LookPos = _ik.RightPos; _ik.LookW = 0.7f;
                yield return Wait(0.7f);
                var palm = Palm(true);
                Cut(palm + PilotRight * 0.4f + Up * 0.3f + PilotFwd * 0.2f, palm, 34f, null, 0.55f, 50f, 3f);
            }
            else CutPilot("side");
            yield return Wait(0.6f);
            yield return ShotV("c11_05_open_hand_insert");
            yield return Wait(0.8f);
            CutPilot("two");
            yield return SayV("VEN_C11", 8, 8, false);                           // I wish I could remember their face.
            // Panther settles closer. Hold the silence.
            _ik.RightW = 0f; _ik.LookW = 0f;
            Cat.LapNudge(8f);
            yield return Silence(2.6f, "VEN_C11_08");
            yield return ShotV("c11_06_he_settles_closer_silence");
            yield return SayV("VEN_C11", 9, 10);                                 // There are other temples. / Then we have somewhere else to ask.
            bool first = VenusState.CompleteScene("VEN_C11", VenusState.Stage.Complete);
            if (DemoBoot.CaptureDir != null) Cap?.Log($"scene VEN_C11 complete{(first ? "" : " (already)")} in the pilot's chair: {VenusState.CurrentId}");
            // ---- VEN_C13: choosing the next world - straight on (QF16 3: at the console, Panther sitting on it)
            yield return SwitchStaging("VEN_C13");
            yield return C13();
            _stroke = 0f; yield return Wait(0.5f);
            if (_strokeRoutine != null) StopCoroutine(_strokeRoutine);
            _ik.LeftW = 0f; _ik.RightW = 0f; _ik.LookW = 0f;
            yield return UI.Fade(1f, 1.2f);
            if (ShipConsole.I != null) ShipConsole.I.HideMap();
            yield return ExitStaging("VEN_C13 ends");
            ShipConsole.StoryLock = false;
            yield return ExploreVenus(false);
        }

        Vector3 ShapeOnArm => ArmRest + PilotFwd * 0.06f;
        Vector3 CubeOnArm => ArmRest - PilotFwd * 0.1f;
        IEnumerator SetBothOnArm()
        {
            if (_carry != null && Shape != null)
            {
                _ik.RightPos = _carry.HipPoint + Up * 0.03f; _ik.RightW = 1f; _ik.LookPos = _carry.HipPoint; _ik.LookW = 0.55f;
                yield return Wait(0.8f);
                _carry.Set(CarriedShape.Where.Hand, 0.25f);
                yield return Wait(0.3f);
                _ik.RightPos = ShapeOnArm + Up * 0.12f; _ik.LookPos = ShapeOnArm; yield return Wait(0.7f);
                var held = Shape.transform.position; var heldR = Shape.transform.rotation;
                _carry.Set(CarriedShape.Where.Away, 0f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.25f) { Shape.transform.SetPositionAndRotation(Vector3.Lerp(held, ShapeOnArm, t), Quaternion.Slerp(heldR, MarkingUp(), t)); yield return null; }
                PlaceShape(ShapeOnArm, MarkingUp());
            }
            _ik.RightPos = (_carry != null ? _carry.HipPoint : Seven.transform.position + Up) + Up * 0.03f; yield return Wait(0.7f);
            if (_cubeCopy != null)
            {
                _cubeCopy.gameObject.SetActive(true);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f) { _cubeCopy.position = Palm(true) + Up * 0.06f; _ik.RightPos = Vector3.Lerp(_ik.RightPos, CubeOnArm + Up * 0.1f, t); yield return null; }
                _ik.RightPos = CubeOnArm + Up * 0.08f; yield return Wait(0.4f);
                _cubeCopy.SetPositionAndRotation(CubeOnArm, Quaternion.LookRotation(PilotFwd));
            }
            _ik.RightW = 0f; _ik.LookW = 0f;
        }

        public string NextWorldChoice { get; private set; }
        IEnumerator C13()
        {
            var con = ShipConsole.I;
            CutPilot("two");
            yield return SayV("VEN_C13", 1);                                     // The guardian's locations. Mars and Europa.
            // over her shoulder - the display; she brings Mars closer, absentmindedly stroking him
            NavHand(true);
            CutPilot("ots");
            yield return Wait(0.6f);
            yield return ShotV("c13_01_over_her_shoulder_display");
            var stroll = StartCoroutine(CatMomentC13());                         // QF16 (4): he strolls across the console in front of the screen
            yield return SayV("VEN_C13", 2, 4);
            if (stroll != null) { float w0 = Time.time; while (_leanHook != null && Time.time - w0 < 8f) yield return null; }                                  // A temple between divided kingdoms. / Convenient... / Perhaps...
            // he lifts his head to look up at her
            Cat.LapNudge(-14f);
            CutPilot("lap");
            yield return SayV("VEN_C13", 5, 6);                                  // Mm. / Such confidence.
            yield return ShotV("c13_02_he_looks_up_at_her");
            // she shifts the display to Europa
            CutPilot("ots");
            yield return SayV("VEN_C13", 7, 11);                                 // ... beneath the ice / Underwater. / Yes. / ... / You'll be perfectly dry in here.
            // he settles deeper into her lap
            Cat.LapNudge(10f);
            yield return SayV("VEN_C13", 12);
            // medium side shot - both in the chair; she grows quiet, her hand comes to rest
            NavHand(false);
            CutPilot("side");
            _stroke = 0.6f;
            yield return SayV("VEN_C13", 13, 15);
            yield return ShotV("c13_03_side_shot_both_in_the_chair");
            // he nudges his head beneath her hand; she resumes stroking him
            Cat.LapNudge(12f); _stroke = 1f;
            yield return SayV("VEN_C13", 16, 16);
            yield return Wait(1.0f);                                             // a quiet pause
            yield return SayV("VEN_C13", 17, 18);
            // a soft breath, she looks down at him
            _ik.LookPos = Seven.LapPoint(0.1f); _ik.LookW = 0.8f;
            yield return SayV("VEN_C13", 19, 22);
            _stroke = 0.7f; yield return Wait(0.4f); _stroke = 1f;               // her hand pauses against his fur, then continues
            yield return SayV("VEN_C13", 23, 23, false);                         // Good.
            _ik.LookW = 0f;
            CutPilot("two");
            yield return Wait(1.6f);                                             // hold the two-shot before the choice
            yield return ShotV("c13_04_hold_before_the_choice");
            // PLAYER CHOICE (on the star map, only the two new worlds)
            string pick = null;
            if (con != null)
            {
                if (Auto) con.ScriptPick = DemoBoot.R13 == "europa" || DemoBoot.Europa && !DemoBoot.Mars ? "europa" : "mars";
                CutPilot("ots");
                NavHand(true);
                yield return con.ChooseOnStarMap(new[] { "mars", "europa" },
                    new[] { "Mars — Seek the temple among the divided kingdoms", "Europa — Find the Cetacean temple beneath the ice" }, p => pick = p);
            }
            if (pick == null) pick = "mars";
            NextWorldChoice = pick;
            if (DemoBoot.CaptureDir != null) Cap?.Log($"VEN_C13 choice: {pick}");
            CutPilot("two");
            if (pick == "mars") yield return SayV("VEN_C13", 901, 904);          // VEN_C13_M1..M4
            else yield return SayV("VEN_C13", 951, 954);                         // VEN_C13_E1..E4 (she chuckles and sets the course)
            // she sets the course; close insert - Panther in her lap, a quiet purr joins the ship's hum
            NavHand(true);
            AudioKit.Play("course_set", null, 0.7f);
            var purr = AudioKit.Loop("cat_purr", null, null, 0.5f, AudioKit.Bus.Sfx);
            CutPilot("lap");
            yield return Wait(1.6f);
            yield return ShotV("c13_05_insert_panther_purring");
            // wide from behind the chair: the chosen world fills the display; she rests back, the journey begins
            NavHand(false);
            CutPilot("wide");
            yield return Wait(2.6f);
            yield return ShotV("c13_06_wide_behind_the_chair_journey_begins");
            if (purr != null) StartCoroutine(AudioKit.Fade(purr, 0f, 2f, true));
            VenusState.CompleteScene("VEN_C13");
            VenusState.CompleteScene("VEN_C13_" + pick);
            // Round 14: Europa is playable - the chapter starts (its title card, the chair scene, the dive); Mars stays "coming soon"
            if (pick == "europa") { yield return TravelPassage.Run("europa"); yield return UI.Fade(1f, 1.0f); if (DemoBoot.CaptureDir != null) Cap?.Log("VEN_C13: Europa chosen -> Chapter 'Europa — Beneath the Ice' starts"); EuropaState.StartFresh(); yield break; }
            // Round 15: Mars is playable - the chapter starts (its title card, the chair scene, the landing)
            if (pick == "mars") { yield return TravelPassage.Run("mars"); yield return UI.Fade(1f, 1.0f); if (DemoBoot.CaptureDir != null) Cap?.Log("VEN_C13: Mars chosen -> Chapter 'Mars — The Divided Kingdoms' starts"); MarsState.StartFresh(); yield break; }
        }
    }
}
