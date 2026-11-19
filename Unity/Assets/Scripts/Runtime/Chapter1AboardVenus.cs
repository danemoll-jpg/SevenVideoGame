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
            StartCoroutine(VenusState.SceneDone("VEN_C11") ? ExploreVenus(true) : VenusDebrief());
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
            if (c == "venus") { yield return UI.Fade(1f, 1f); UnityEngine.SceneManagement.SceneManager.LoadScene("Venus"); yield break; }
            if (c == "moon") { yield return UI.Fade(1f, 1f); VenusState.UseOwnSave(); Chapter1State.StartAt(Chapter1State.CpCell); yield break; }
            Director.Cam.Hold = false; Seven.Frozen = false; _settingCourse = false;
        }
    }
}
