using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Seven
{
    /// <summary>
    /// Round 19 — the Saucer scene's part of the Moon finale (Docs/CHAPTER_FINALE.md, FIN_01; Decisions 1):
    /// the title card over black from the first frame; the exterior: the saucer approaching the Moon - Chapter 1's opening
    /// composition reversed (they choose to come back); 7 in her EGG lounge chair facing the window, Panther in her lap, the hull
    /// vanishing round them as they talk (the arrival rule); then the player guides the saucer in through the familiar lunar
    /// entrance down to the service berth (the real Moon level, baked: Resources/Outside/Outside_moon, the bay under the guide ring)
    /// - then the Finale scene. Music: aboard_saucer (FIN_01), the guided approach carries it.
    /// </summary>
    public partial class Chapter1Aboard
    {
        public bool FinaleMode { get; private set; }
        IEnumerator SayF(string scene, int from, int to = -1) { yield return SayLines(FinaleDialogue.Scene(scene, from, to < 0 ? from : to), true); }

        void StartFinale()
        {
            FinaleState.ArrivalAboard = false;
            I = this; Active = true; VenusMode = true; MarsMode = true; AsteroidMode = true; CattalusMode = true; FinaleMode = true;
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
            SaucerLocation.System = "sol";
            SaucerLocation.Set("moon");
            if (DemoBoot.CaptureDir != null)
            {
                ChapterCapture.Ensure(); Cap.Attach(Director.Cam.GetComponent<Camera>());
                if (Auto) { Seven.ScriptDriven = true; Dlg.AutoSkipAfter = DemoBoot.VenusSkip ? 0f : 0.9f; Director.Legend.AutoSwitch = false; }
                Cap.Log($"aboard (the Moon finale, arrival): {FinaleState.Summary()}; {WorldFlags.Summary()}");
                StartCoroutine(FirstFrameF());
            }
            StartCoroutine(FinaleArrival());
        }
        IEnumerator FirstFrameF() { Cap.Log($"first frame aboard (the finale): fade {UI.FadeAlpha:0.00} (1 = black)"); yield return Cap.Shot("fa00_first_frame_aboard"); }

        // ------------------------------------------------------------------------------------------ the exterior (the baked Moon level)
        GameObject _moonWorld; Transform _bayRing; float _farF = -1f;
        /// <summary>The real Moon level (Resources/Outside/Outside_moon, baked from Chapter 1 - the city, the host, the bay) placed so
        /// the bay (MoonLayout.SaucerCentre) sits at <paramref name="bay"/>; a guide ring + its beam over the berth.</summary>
        void BuildMoonExterior(Vector3 bay)
        {
            var pf = Resources.Load<GameObject>("Outside/Outside_moon");
            if (pf != null) { _moonWorld = Instantiate(pf); _moonWorld.name = "MoonExterior"; _moonWorld.transform.position = bay - MoonLayout.SaucerCentre; MoonExteriorDress.Dress(_moonWorld); }
            if (DemoBoot.CaptureDir != null && pf != null) Cap?.Log($"FIN_01 EXTERIOR DRESSING (QF32 4): {MoonExteriorDress.Dressed} plain box pieces of the host dressed as worked stone ({MoonExteriorDress.Parts} parts: plinths, string courses, pilasters, cornices, fallen blocks)");
            var rg = new GameObject("BayGuideRing"); _bayRing = rg.transform; _bayRing.position = bay + Vector3.up * 0.6f;
            rg.AddComponent<MeshFilter>().sharedMesh = Markers.RingMesh(9f, 10.5f, 0f); var mr = rg.AddComponent<MeshRenderer>(); mr.sharedMaterial = Markers.Additive(1.4f); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(beam.GetComponent<Collider>()); beam.transform.SetParent(_bayRing, false); beam.transform.localPosition = Vector3.up * 40f; beam.transform.localScale = new Vector3(1.2f, 40f, 1.2f); beam.GetComponent<Renderer>().sharedMaterial = Markers.Additive(0.35f);
            if (DemoBoot.CaptureDir != null) Cap?.Log($"FIN_01 exterior: the real Moon level {(pf != null ? $"({_moonWorld.transform.childCount} pieces)" : "MISSING Resources/Outside/Outside_moon")} under the saucer; the berth's guide ring in the bay");
        }
        void MoonWin(bool on)
        {
            if (_moonWorld != null) _moonWorld.SetActive(!on);
            var a = SaucerOutside.I != null ? SaucerOutside.I.Atmo : null;
            if (a != null) a.HeightOffset = !on && _moonWorld != null ? _moonWorld.transform.position.y + MoonLayout.SaucerCentre.y + 95f : 0f;
        }

        // ------------------------------------------------------------------------------------------ FIN_01
        IEnumerator FinaleArrival()
        {
            ShipConsole.StoryLock = true; InGalleyScene = true;
            Seven.Frozen = true; Seven.Cutscene = true; Director.Cam.Hold = true;
            Cat.ForceTarget = null; Cat.enabled = false;
            MusicSystem.Stop(0.5f);
            yield return UI.Title("THE MOON  —  SOMEWHERE TO COME BACK TO", 1.0f, 2.6f, 1.0f);
            if (DemoBoot.CaptureDir != null) Cap?.Log("title card: 'THE MOON — SOMEWHERE TO COME BACK TO' over black");
            var stage = TravelStage.I; Transform saucer = null; Vector3 p0 = new Vector3(0f, 400f, 0f);
            if (stage != null && stage.Saucer != null) { saucer = stage.Saucer; var holder = saucer.parent != null ? saucer.parent.gameObject : saucer.gameObject; holder.SetActive(true); p0 = saucer.position; }
            // the bay 120 m below and 230 m ahead: the saucer comes back the way it left in Chapter 1 - in through the entrance, down
            var bay = p0 + new Vector3(0f, -120f, 230f);
            BuildMoonExterior(bay);
            var mainCam = Director.Cam != null ? Director.Cam.GetComponent<Camera>() : Camera.main; if (mainCam != null) { _farF = mainCam.farClipPlane; mainCam.farClipPlane = 4000f; }
            MusicSystem.Play("aboard_saucer", 3f);
            // EXTERIOR - the opening composition reversed: in Chapter 1 the camera pushed in on the Moon and dived through its crust;
            // now the saucer itself flies in toward the hollow interior's lights, the camera following from behind
            // INTERIOR: 7's EGG chair facing the window, Panther in her lap; the hull fades away round them as they talk
            MoonWin(true);
            yield return EnterStaging("CAT_01");
            PlaceRecordsOnArmAsteroid();
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 1f;
            if (_catPose != null) _catPose.LookAt = PilotHead + PilotFwd * 20f;
            CutPilot("two");
            // QF30 (1): outside the vanishing hull, the Moon itself - large ahead, the lunar entrance on its face, Earth beyond, the stars
            if (SaucerOutside.I != null) SaucerOutside.I.Show("space");
            var wfw = Flat(Polar(9f, WindowA, 0f)).normalized;
            var ma = MoonApproach.Build(PilotHead, wfw, ApproachFrom, 220f);
            StartCoroutine(UI.Fade(0f, 1.4f));                                                // out of the title: the Moon whole, far ahead
            _approach = true; _approachRun = StartCoroutine(Approach(ma, PilotHead));
            StartCoroutine(HullAway(true, "FIN_01", "fa02h"));
            var moonP = ma.Moon.position;
            { var hd = PilotHead; Cut(hd - wfw * 0.6f + Vector3.Cross(Up, wfw) * 0.35f + Up * 0.22f, moonP, 40f); }    // over her shoulder: she looks at it
            ImageGate.Expect(ma.Moon, "the Moon ahead, the hull dissolving", 0f, "fa02h");     // the mid-dissolve frame (taken during the line) is of the Moon too
            for (float t = 0f; t < 6f && Director.Reveal != null && Director.Reveal.Progress < 0.3f; t += Time.deltaTime) yield return null;
            ImageGate.Expect(ma.Moon, "the Moon ahead of the window", 0f, "fa03_");
            yield return DialogueFraming.Over("the Moon ahead, through the open hull (QF24 7)", SayF("FIN_01", 1, 1));   // Looks smaller from out here.
            yield return ShotV("fa03_she_watches_the_moon");
            // cut to the Moon: its face, the entrance they will fly into
            Cut(PilotHead + wfw * 0.4f + Up * 0.1f, Vector3.Lerp(ma.Moon.position, ma.Entrance.position, 0.35f), 24f);   // QF34 (6): a long lens on the far Moon
            ImageGate.Expect(ma.Moon, "the Moon and the lunar entrance", 0f, "fa03b_");
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) yield return null;
            yield return ShotV("fa03b_the_moon_and_the_entrance");
            CutPilot("lap");
            yield return SayF("FIN_01", 2, 2);                   // You've had a busy few weeks.
            CutPilot("two");
            yield return SayF("FIN_01", 3, 4);                   // Last time, all I wanted was to get away from it. / An excellent plan at the time.
            HerFaceCloseUpPilot();
            yield return SayF("FIN_01", 5, 5);                   // Still could be.
            CutPilot("lap");
            yield return SayF("FIN_01", 6, 7);                   // Let's park somewhere else. / Mm. Somewhere without a prison attached.
            { var sd = Vector3.Cross(Up, wfw); Cut(PilotHead - wfw * 2.4f + sd * 1.3f + Up * 0.7f, PilotHead + wfw * 8f - Up * 1.2f, 52f); }   // behind and beside the chair: her, and the Moon ahead
            for (float t = 0f; t < 1.5f; t += Time.deltaTime) yield return null;
            ImageGate.Expect(Seven.transform, "7 in the egg chair, the Moon round her", 0f, "fa04_");
            yield return ShotV("fa04_the_hull_gone_the_moon_round_them");
            if (_catPose != null) { _catPose.LookAt = null; _catPose.EarToward = null; }
            yield return HullAway(false, "FIN_01");
            _approach = false; if (_approachRun != null) yield return _approachRun;               // the approach ends close, at the entrance
            if (MoonApproach.Current != null) Destroy(MoonApproach.Current.gameObject);      // the guided approach shows the real level
            // QF34 (6): the close exterior - the saucer flying in over the Moon level - now ENDS the approach (rc6 opened on it, then cut to the
            // whole Moon far away, then back close: the "flip")
            MoonWin(false);
            var city = bay + (MoonLayout.BasinCenter - MoonLayout.SaucerCentre) * 0.35f;
            // (QF26 2) the camera high behind the saucer, above the host's broken pieces, looking down its path to the bay's lights
            var camA = p0 + new Vector3(8f, 46f, -84f);
            for (float t = 0f; t < 0.2f; t += Time.deltaTime) { Cut(camA, Vector3.Lerp(p0, bay, 0.45f), 54f); yield return null; }
            for (float t = 0f; t < 3.6f; t += Time.deltaTime) { if (saucer != null) saucer.position = p0 + Vector3.forward * t * 6f + Vector3.down * t * 1.2f; Cut(camA + Vector3.forward * t * 5f, saucer != null ? Vector3.Lerp(saucer.position, bay, 0.3f) : bay, 50f); yield return null; }
            if (saucer != null) ImageGate.Expect(saucer, "the saucer coming back to the Moon", 0f, "fa01_");
            yield return ShotV("fa01_the_familiar_approach_reversed");
            if (saucer != null) saucer.position = p0;
            Cap?.Log($"FIN_01 APPROACH (QF34 6): one continuous approach - the Moon from {ApproachFrom:0} m to {ApproachTo:0} m (centre) through the whole talk, never pulled back; then the flight in over the level at the entrance");
            // GAMEPLAY: the completed map shows a maintenance approach below the city - guide the saucer in to the service berth
            yield return GuideToBay(saucer, p0, bay);
            if (mainCam != null && _farF > 0f) mainCam.farClipPlane = _farF;
            FinaleState.CompleteScene("FIN_01");
            FinaleState.Reach(FinaleLayout.CpBerth);
            _ik.ReleaseAll("FIN_01 ends");
            if (_strokeRoutine != null) { StopCoroutine(_strokeRoutine); _strokeRoutine = null; }
            yield return UI.Fade(1f, 1.2f);
            if (DemoBoot.CaptureDir != null) Cap?.Log("the finale's arrival aboard done -> the Finale scene (the service berth)");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Finale");
        }

        // QF34 (6): one steady approach - the Moon (and its entrance) close in along the view at a constant rate through the whole talk;
        // framings per line may change angle, never distance; when the talk ends the rest of the approach completes in a couple of seconds
        const float ApproachFrom = 900f, ApproachTo = 330f, ApproachTalk = 42f;
        bool _approach; Coroutine _approachRun;
        IEnumerator Approach(MoonApproach ma, Vector3 eye)
        {
            if (ma == null || ma.Moon == null) yield break;
            var dir = (ma.Moon.position - eye).normalized; float d0 = (ma.Moon.position - eye).magnitude;
            var entOff = ma.Entrance != null ? ma.Entrance.position - ma.Moon.position : Vector3.zero;
            float d = d0, minD = d0;
            while (ma != null && ma.Moon != null)
            {
                float rate = (ApproachFrom - ApproachTo) / ApproachTalk * (_approach ? 1f : 8f);
                d = Mathf.Max(ApproachTo, d - rate * Time.deltaTime); minD = Mathf.Min(minD, d);
                var c = eye + dir * d; ma.Moon.position = c; if (ma.Entrance != null) ma.Entrance.position = c + entOff;
                if (!_approach && d <= ApproachTo + 0.01f) break;
                yield return null;
            }
            if (DemoBoot.CaptureDir != null) Cap?.Log($"FIN_01 APPROACH (QF34 6): distance only ever went down ({d0:0} m -> {minD:0} m)");
        }

        /// <summary>The player guides the saucer in to the berth in the bay (as CAT_01's GuideToBerth: the stick steers, the guide ring
        /// glows; within it the saucer settles itself).</summary>
        IEnumerator GuideToBay(Transform saucer, Vector3 p0, Vector3 bay)
        {
            if (saucer == null) yield break;
            MoonWin(false);
            UI.SetObjective("Guide the saucer in to the service berth below the city");
            var pos = p0; float yaw = 0f, speed = 16f; float t0 = Time.time;
            var target = bay + Vector3.up * FinaleLayout.SaucerHover;
            GuidingTo = target; GuidingSaucer = saucer;
            while (true)
            {
                var pad = Gamepad.current; var kb = Keyboard.current;
                Vector2 st = pad != null ? pad.leftStick.ReadValue() : Vector2.zero;
                if (kb != null) { if (kb.aKey.isPressed) st.x -= 1f; if (kb.dKey.isPressed) st.x += 1f; if (kb.wKey.isPressed) st.y += 1f; if (kb.sKey.isPressed) st.y -= 1f; }
                var to = target - pos; var flat = new Vector3(to.x, 0f, to.z);
                if (Auto) { float err = Vector3.SignedAngle(Quaternion.Euler(0f, yaw, 0f) * Vector3.forward, flat, Vector3.up); st = new Vector2(Mathf.Clamp(err / 25f, -1f, 1f), 0f); }
                yaw += st.x * 55f * Time.deltaTime;
                float d = flat.magnitude;
                speed = Mathf.Lerp(4f, 16f, Mathf.Clamp01(d / 90f));
                var fw = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                pos += fw * speed * Time.deltaTime;
                float wantY = Mathf.Lerp(target.y, p0.y, Mathf.Clamp01((d - 20f) / 220f));
                pos.y = Mathf.MoveTowards(pos.y, wantY + st.y * 6f, 9f * Time.deltaTime);
                saucer.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, -st.x * 8f));
                if (_bayRing != null) foreach (var r in _bayRing.GetComponentsInChildren<Renderer>()) Glowing.Set(r, 1f + 0.5f * Mathf.Sin(Time.time * 4f));
                var cam = pos - fw * 26f + Vector3.up * 10f;
                Cut(cam, pos + fw * 20f - Vector3.up * 6f, 52f);
                if (Time.time - t0 > 2.0f && Time.time - t0 < 2.05f) { ImageGate.Expect(saucer, "the saucer"); yield return ShotV("fa05_guiding_the_saucer_in_to_the_berth"); }
                if (d < 10f) break;
                if (d > 900f || Time.time - t0 > (Auto ? 90f : 240f)) { if (Auto && Cap != null) { Cap.Soft++; Cap.Log("SOFT-LOCK? guiding the saucer to the berth"); } pos = target + Vector3.back * 9f + Vector3.up * 8f; }
                yield return null;
            }
            GuidingTo = null; GuidingSaucer = null;
            var from = pos; var q0 = saucer.rotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.2f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                saucer.SetPositionAndRotation(Vector3.Lerp(from, target, u), Quaternion.Slerp(q0, Quaternion.identity, u));
                Cut(target + new Vector3(-22f, 9f, -26f), saucer.position, 46f);
                yield return null;
            }
            ImageGate.Expect(saucer, "the saucer at the service berth");
            yield return ShotV("fa06_berthed_below_the_city");
            UI.SetObjective(null);
            if (DemoBoot.CaptureDir != null) Cap?.Log($"FIN_01 gameplay: the saucer guided in to the service berth in {Time.time - t0:0.0} s ({(Auto ? "check-run steering" : "the player's stick")})");
        }
    }
}
