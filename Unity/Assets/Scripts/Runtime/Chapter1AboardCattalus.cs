using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Seven
{
    /// <summary>
    /// Round 18 — the Saucer scene's Cattalus parts (Docs/CHAPTER_CATTALUS.md):
    /// CAT_01 (<see cref="CattalusState.ArrivalAboard"/>): the title card over black from the first frame; the exterior approach
    /// over Cattalus (the real level, baked: the cardboard quarter, the cat-tree towers, the old transit tower with its hanging
    /// ball); 7 in her EGG lounge chair facing the window, Panther in her lap, the hull vanishing round them as they talk (the
    /// arrival rule); the city reveal; then the player guides the saucer down to the public berth (the stick steers, the guide
    /// ring glows over the berth) - then the Cattalus scene.
    /// CAT_12 (<see cref="CattalusState.HomeAboard"/>): straight from the beam-up, the PILOT'S chair at the console, Panther in her
    /// lap; the sphere joins the six records on the display ring; the marks align (the hologram); the lunar interior resolves
    /// with a chamber below the city; the Moon route is selected; the exterior: the saucer rises above Cattalus with the meadow
    /// beyond; "To be continued". Then free exploration (the star map offers only the Moon).
    /// </summary>
    public partial class Chapter1Aboard
    {
        public bool CattalusMode { get; private set; }
        public bool CattalusHomeDone { get; private set; }
        /// <summary>CAT_01 gameplay: the berth the player steers the saucer to (the playtest reads it like the guide ring on screen).</summary>
        [System.NonSerialized] public Vector3? GuidingTo; [System.NonSerialized] public Transform GuidingSaucer;
        IEnumerator SayC(string scene, int from, int to = -1) { yield return SayLines(CattalusDialogue.Scene(scene, from, to < 0 ? from : to), true); }
        IEnumerator SayCIds(params string[] ids) { yield return SayLines(CattalusDialogue.Ids(ids), true); }

        void StartCattalus()
        {
            bool arrival = CattalusState.ArrivalAboard; CattalusState.ArrivalAboard = false; CattalusState.HomeAboard = false;
            I = this; Active = true; VenusMode = true; MarsMode = true; AsteroidMode = true; CattalusMode = true;
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
            SaucerLocation.System = StarMap.Bia;
            SaucerLocation.Set("cattalus");
            if (DemoBoot.CaptureDir != null)
            {
                ChapterCapture.Ensure(); Cap.Attach(Director.Cam.GetComponent<Camera>());
                if (Auto) { Seven.ScriptDriven = true; Dlg.AutoSkipAfter = DemoBoot.VenusSkip ? 0f : 0.9f; Director.Legend.AutoSwitch = false; }
                Cap.Log($"aboard (Cattalus mode, {(arrival ? "arrival" : "home")}): {CattalusState.Summary()}; {WorldFlags.Summary()}");
                StartCoroutine(FirstFrameC());
            }
            StartCoroutine(arrival ? CattalusArrival() : CattalusHome());
        }
        IEnumerator FirstFrameC() { Cap.Log($"first frame aboard (Cattalus): fade {UI.FadeAlpha:0.00} (1 = black)"); yield return Cap.Shot("ca00_first_frame_aboard"); }

        // ------------------------------------------------------------------------------------------ the exterior (the baked level)
        GameObject _catWorld; Transform _berthRing; float _farC = -1f;
        /// <summary>The real Cattalus level (Resources/Outside/Outside_cattalus) placed under the passage stage's saucer: its berth
        /// at <paramref name="berth"/>. The same copy the window shows - the exterior always matches the level.</summary>
        void BuildCattalusExterior(Vector3 berth)
        {
            var pf = Resources.Load<GameObject>("Outside/Outside_cattalus");
            if (pf != null) { _catWorld = Instantiate(pf); _catWorld.name = "CattalusExterior"; _catWorld.transform.position = berth; }
            var rg = new GameObject("BerthGuideRing"); _berthRing = rg.transform; _berthRing.position = berth + Vector3.up * 0.6f;
            rg.AddComponent<MeshFilter>().sharedMesh = Markers.RingMesh(9f, 10.5f, 0f); var mr = rg.AddComponent<MeshRenderer>(); mr.sharedMaterial = Markers.Additive(1.4f); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(beam.GetComponent<Collider>()); beam.transform.SetParent(_berthRing, false); beam.transform.localPosition = Vector3.up * 40f; beam.transform.localScale = new Vector3(1.2f, 40f, 1.2f); beam.GetComponent<Renderer>().sharedMaterial = Markers.Additive(0.35f);
            if (DemoBoot.CaptureDir != null) Cap?.Log($"CAT_01 exterior: the real Cattalus level {(pf != null ? $"({_catWorld.transform.childCount} pieces)" : "MISSING Resources/Outside/Outside_cattalus")} under the saucer; the public berth's guide ring");
        }
        void CatWin(bool on)
        {
            if (_win == null) _win = GameObject.Find("Outside_cattalus_Level") ?? GameObject.Find("Outside_cattalus_Level(Clone)");
            if (_catWorld != null) _catWorld.SetActive(!on);
            // QF26 (3): the exterior copy is staged ~2.9 km up; its haze is the window view's (ground 95 m below the ship) shifted up
            // to it - with no offset it had no haze at all (flat gray), the window view the Moon city's white fog
            var a = SaucerOutside.I != null ? SaucerOutside.I.Atmo : null;
            if (a != null) a.HeightOffset = !on && _catWorld != null ? _catWorld.transform.position.y + 95f : 0f;
        }

        // ------------------------------------------------------------------------------------------ CAT_01
        IEnumerator CattalusArrival()
        {
            ShipConsole.StoryLock = true; InGalleyScene = true;
            Seven.Frozen = true; Seven.Cutscene = true; Director.Cam.Hold = true;
            Cat.ForceTarget = null; Cat.enabled = false;
            MusicSystem.Stop(0.5f);
            yield return UI.Title("CATTALUS  —  PANTHER'S HOMEWORLD", 1.0f, 2.4f, 1.0f);
            if (DemoBoot.CaptureDir != null) Cap?.Log("title card: 'CATTALUS — PANTHER'S HOMEWORLD' over black");
            var stage = TravelStage.I; Transform saucer = null; Vector3 p0 = new Vector3(0f, 400f, 0f);
            if (stage != null && stage.Saucer != null) { saucer = stage.Saucer; var holder = saucer.parent != null ? saucer.parent.gameObject : saucer.gameObject; holder.SetActive(true); p0 = saucer.position; }
            // the level 140 m below and 260 m ahead of the saucer; the saucer comes in from the south over the berth's approach
            var berth = p0 + new Vector3(0f, -140f, 260f);
            BuildCattalusExterior(berth);
            var mainCam = Director.Cam != null ? Director.Cam.GetComponent<Camera>() : Camera.main; if (mainCam != null) { _farC = mainCam.farClipPlane; mainCam.farClipPlane = 3000f; }
            MusicSystem.Play("cattalus_city", 3f);
            // EXTERIOR: the approach over the city in the mist - cat-tree towers, the cardboard quarter, the transit tower's ball
            var tower = berth + CattalusLayout.TransitTower + Vector3.up * CattalusLayout.TransitTowerH * 0.7f;
            CatWin(false);
            for (float t = 0f; t < 0.2f; t += Time.deltaTime) { Cut(p0 + new Vector3(-30f, 14f, -40f), berth + new Vector3(0f, 10f, 120f), 56f); yield return null; }
            yield return UI.Fade(0f, 1.4f);
            for (float t = 0f; t < 3.4f; t += Time.deltaTime) { if (saucer != null) saucer.position = p0 + Vector3.forward * t * 7f + Vector3.down * t * 1.5f; Cut(p0 + new Vector3(-30f, 14f, -40f) + Vector3.forward * t * 5f, berth + new Vector3(0f, 6f, 90f), 40f); yield return null; }   // QF26 (3): on the city
            ImageGate.ExpectPoint(berth + Vector3.up * 4f, "the city round the public berth");   // QF26 (2)
            yield return ShotV("ca01_the_approach_over_cattalus");
            if (saucer != null) saucer.position = p0;
            // INTERIOR: 7's EGG chair facing the window, Panther in her lap (he has been watching the window for some time)
            CatWin(true);
            yield return EnterStaging("CAT_01");
            PlaceRecordsOnArmAsteroid();
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 1f;
            if (_catPose != null) _catPose.LookAt = PilotHead + PilotFwd * 20f;              // sitting up, watching outside
            CutPilot("lap");
            StartCoroutine(HullAway(true, "CAT_01", "ca02h"));
            yield return SayC("CAT_01", 1, 3);                   // You've been sitting up for the last ten minutes. / Have I? / Mm-hmm.
            var tw = SaucerOutside.I != null ? SaucerOutside.I.LevelPoint(CattalusLayout.TransitTower + Vector3.up * (CattalusLayout.TransitTowerH - 6f)) : null;
            if (tw.HasValue) { var hd = PilotHead; Cut(hd - PilotFwd * 0.55f + PilotRight * 0.3f + Up * 0.25f, tw.Value, 34f); }
            else WindowOnTheAsteroid();
            if (_catPose != null) _catPose.EarToward = tw ?? (PilotHead + PilotFwd * 20f);
            yield return DialogueFraming.Over("the old transit tower and its hanging ball through the window (QF24 7)", SayC("CAT_01", 4, 6));   // That's the old transit tower. / The one with the ball hanging from it? / Yes.
            if (tw.HasValue) ImageGate.ExpectPoint(tw.Value, "the old transit tower");
            yield return ShotV("ca03_the_old_transit_tower_its_hanging_ball");
            CutPilot("two");
            yield return SayC("CAT_01", 7, 7);                   // Of course.
            // the city reveal: tiered cat-tree residences over decorated cardboard neighbourhoods, transit routes between the columns
            OpenSpaceShot();
            for (float t = 0f; t < 8f && Director.Reveal != null && Director.Reveal.Progress < 0.99f; t += Time.deltaTime) yield return null;
            // QF26 (3): the reveal is OF THE CITY - from behind her shoulder, down over the avenue, the cardboard quarter and the square
            // (the window's own direction looked over the meadow's bare terrain: 0.18.0's "fog with a giant blank slab")
            { var cityP = SaucerOutside.I != null ? SaucerOutside.I.LevelPoint(new Vector3(0f, 6f, 100f)) : null;
              if (cityP.HasValue) { var dc = Flat(cityP.Value - PilotHead).normalized; var sd = Vector3.Cross(Up, dc).normalized; Cut(PilotHead - dc * 1.5f + sd * 1.3f + Up * 0.55f, cityP.Value, 50f); } }
            { var cityP = SaucerOutside.I != null ? SaucerOutside.I.LevelPoint(new Vector3(0f, 4f, 90f)) : null; if (cityP.HasValue) ImageGate.ExpectPoint(cityP.Value, "the city below (the avenue)"); }
            yield return ShotV("ca04_the_city_reveal_the_hull_gone");
            var cardboard = SaucerOutside.I != null ? SaucerOutside.I.LevelPoint(new Vector3(-20f, 6f, 60f)) : null;
            if (cardboard.HasValue) Cut(PilotHead - PilotFwd * 0.5f - PilotRight * 0.3f + Up * 0.2f, cardboard.Value, 36f);
            yield return DialogueFraming.Over("the cardboard quarter through the window (QF24 7)", SayC("CAT_01", 8, 9));   // Is that building cardboard? / Several buildings, actually.
            var tree = SaucerOutside.I != null ? SaucerOutside.I.LevelPoint(new Vector3(26f, 20f, 70f)) : null;
            if (tree.HasValue) Cut(PilotHead - PilotFwd * 0.5f + PilotRight * 0.3f + Up * 0.1f, tree.Value, 40f);
            yield return DialogueFraming.Over("the enormous cat tree (QF24 7)", SayC("CAT_01", 10, 11));   // And the enormous tree? / Apartments...
            CutPilot("lap");
            yield return SayC("CAT_01", 12, 14);                 // Better view? / Better sun. / Ah. Obviously.
            if (_catPose != null) { _catPose.LookAt = null; _catPose.EarToward = null; }
            CutPilot("two");
            yield return SayCIds("CAT_01_P1", "CAT_01_P2");     // The public berths are below the transit tower. Bring us in slowly. / Slowly. Got it.
            yield return HullAway(false, "CAT_01");
            // GAMEPLAY: guide the saucer down to the public berth
            yield return GuideToBerth(saucer, p0, berth);
            if (mainCam != null && _farC > 0f) mainCam.farClipPlane = _farC;
            CattalusState.CompleteScene("CAT_01");
            CattalusState.Reach(CattalusLayout.CpCity);
            _ik.ReleaseAll("CAT_01 ends");
            if (_strokeRoutine != null) { StopCoroutine(_strokeRoutine); _strokeRoutine = null; }
            yield return UI.Fade(1f, 1.2f);
            if (DemoBoot.CaptureDir != null) Cap?.Log("Cattalus arrival aboard done -> the Cattalus scene (the public berth)");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Cattalus");
        }

        /// <summary>The player guides the saucer down to the public berth: the stick steers (left / right turn, up / down climb or
        /// dive), the saucer flies on slowly, the guide ring and its beam glow over the berth; within the ring it settles itself
        /// (the docking arms are clear of the bike hold). Check runs steer like a player at the ring.</summary>
        IEnumerator GuideToBerth(Transform saucer, Vector3 p0, Vector3 berth)
        {
            if (saucer == null) yield break;
            CatWin(false);
            UI.SetObjective("Guide the saucer down to the public berth");
            UI.Hint("Left stick: steer", 4f);
            var pos = p0; float yaw = 0f, speed = 16f; float t0 = Time.time; var pad = Gamepad.current; var kb = Keyboard.current;
            var target = berth + Vector3.up * CattalusLayout.SaucerHover;
            GuidingTo = target; GuidingSaucer = saucer;
            while (true)
            {
                pad = Gamepad.current; kb = Keyboard.current;
                Vector2 st = pad != null ? pad.leftStick.ReadValue() : Vector2.zero;
                if (kb != null) { if (kb.aKey.isPressed) st.x -= 1f; if (kb.dKey.isPressed) st.x += 1f; if (kb.wKey.isPressed) st.y += 1f; if (kb.sKey.isPressed) st.y -= 1f; }
                var to = target - pos; var flat = new Vector3(to.x, 0f, to.z);
                if (Auto) { float err = Vector3.SignedAngle(Quaternion.Euler(0f, yaw, 0f) * Vector3.forward, flat, Vector3.up); st = new Vector2(Mathf.Clamp(err / 25f, -1f, 1f), 0f); }
                yaw += st.x * 55f * Time.deltaTime;
                float d = flat.magnitude;
                speed = Mathf.Lerp(4f, 16f, Mathf.Clamp01(d / 90f));
                var fw = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                pos += fw * speed * Time.deltaTime;
                // height: the stick's up / down nudges; otherwise a glide path down toward the berth as she closes in
                float wantY = Mathf.Lerp(target.y, p0.y, Mathf.Clamp01((d - 20f) / 240f));
                pos.y = Mathf.MoveTowards(pos.y, wantY + st.y * 6f, 9f * Time.deltaTime);
                saucer.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, -st.x * 8f));
                if (_berthRing != null) foreach (var r in _berthRing.GetComponentsInChildren<Renderer>()) Glowing.Set(r, 1f + 0.5f * Mathf.Sin(Time.time * 4f));
                var cam = pos - fw * 26f + Vector3.up * 10f;   // QF26 (3): closer - the saucer over the city, less haze between
                Cut(cam, pos + fw * 20f - Vector3.up * 6f, 52f);
                if (Time.time - t0 > 2.0f && Time.time - t0 < 2.05f) { ImageGate.Expect(saucer, "the saucer"); yield return ShotV("ca05_guiding_the_saucer_down_to_the_berth"); }
                if (d < 10f) break;
                if (d > 900f || Time.time - t0 > (Auto ? 90f : 240f)) { if (Auto && Cap != null) { Cap.Soft++; Cap.Log("SOFT-LOCK? guiding the saucer to the berth"); } pos = target + Vector3.back * 9f + Vector3.up * 8f; }
                yield return null;
            }
            GuidingTo = null; GuidingSaucer = null;
            // settle onto the berth: the docking arms swing in (clear of the hold and the beam)
            var from = pos; var q0 = saucer.rotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.2f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                saucer.SetPositionAndRotation(Vector3.Lerp(from, target, u), Quaternion.Slerp(q0, Quaternion.identity, u));
                Cut(target + new Vector3(-22f, 9f, -26f), saucer.position, 46f);
                yield return null;
            }
            ImageGate.Expect(saucer, "the saucer at the berth");
            yield return ShotV("ca06_docked_at_the_public_berth");
            UI.SetObjective(null);
            if (DemoBoot.CaptureDir != null) Cap?.Log($"CAT_01 gameplay: the saucer guided down to the public berth in {Time.time - t0:0.0} s ({(Auto ? "check-run steering" : "the player's stick")})");
        }

        /// <summary>QF26 (2): the records on their display, from the first spot round it with a clear line to the ring and room for
        /// the camera (the "objects" framing stood behind the console's rim band at the pilot's chair).</summary>
        void RecordsShotClear()
        {
            var d = RecordDisplayTop() + Up * 0.06f; var buf = new Collider[4];
            for (int i = 0; i < 12; i++)
            {
                var dir = Quaternion.AngleAxis(i * 30f, Up) * (PilotRight * 0.7f + PilotFwd * 0.4f).normalized;
                foreach (var hgt in new[] { 0.38f, 0.6f })
                {
                    var c = d + dir * 0.75f + Up * hgt; var to = d - c;
                    if (CameraOccluders.Ready && (CameraOccluders.AnyRay(c, to.normalized, to.magnitude - 0.12f, CameraOccluders.SolidMask, out _) || CameraOccluders.OverlapSphere(c, 0.15f, buf, CameraOccluders.SolidMask) > 0)) continue;
                    Cut(c, d, 36f); if (DemoBoot.CaptureDir != null) Cap?.Log($"records shot (QF26 2): clear view of the display from {i * 30}deg, {hgt:0.00} m above it"); return;
                }
            }
        }

        // ------------------------------------------------------------------------------------------ CAT_12
        IEnumerator CattalusHome()
        {
            ShipConsole.StoryLock = true; InGalleyScene = true;
            Seven.Frozen = true; Seven.Cutscene = true; Director.Cam.Hold = true;
            Cat.ForceTarget = null; Cat.enabled = false;
            MusicSystem.Board();
            // the pilot's chair at the console, Panther in her lap (the destination choice)
            yield return EnterStaging("CAT_12");
            PlaceRecordsOnArmAsteroid();                        // the seven records on the display ring (the sphere among them)
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 1f;
            CutPilot("two");
            yield return UI.Fade(0f, 1.2f);
            if (DemoBoot.CaptureDir != null) Cap?.Log($"CAT_12: the records on the display: {RecordSet.Names()} ({RecordSet.Collected().Count} of 7)");
            CutPilot("objects");
            RecordsShotClear();                                  // QF26 (2): 0.18.0's framing was filled by the console's rim band
            if (_recordDisplay != null) ImageGate.Expect(_recordDisplay.transform, "the seven records on their display");
            yield return ShotV("ca10_the_seven_records_the_sphere_among_them");
            var con = ShipConsole.I;
            NavHand(true); CutPilot("ots");
            var holo = HoloDisplay.Get();
            yield return holo.MarksAlign(RecordSet.Collected().Count);
            yield return DialogueFraming.Over("the records' marks on the hologram (QF24 7)", SayC("CAT_12", 1, 3));   // There. That mark was on the container. / In the memory? / Mm-hmm. And on the first piece...
            yield return holo.MarksJoin();                      // the markings align; the fragments join into one structure
            yield return ShotV("ca11_the_marks_align_into_one_structure");
            CutPilot("two");
            yield return SayC("CAT_12", 4, 5);                   // They belong together. / The records do, too. Look.
            // the lunar interior: the city and the escape routes; below them a chamber resolves
            holo.Show("moon", 1.15f, 900f);
            holo.Mark(new Vector3(0f, -180f, 0f), 40f, "a chamber below the lunar city", true);
            CutPilot("ots");
            yield return Wait(1.0f);
            yield return ShotV("ca12_the_lunar_interior_a_chamber_resolves_below_the_city");
            yield return DialogueFraming.Over("the lunar city on the hologram (QF24 7)", SayC("CAT_12", 6, 7));   // That's where we were. / Above it. This is farther down.
            yield return Wait(1.4f);                              // CAT_12_08: a quiet pause
            CutPilot("lap");
            yield return SayC("CAT_12", 9, 14);                   // All those different places... / ... / You do.
            HerFaceCloseUpPilot();
            yield return SayC("CAT_12", 15, 16);                  // Then we need to find out why. / The Moon, then.
            // she selects the route
            if (con != null) con.ShowMap("moon", "The way back to the Moon");
            CutPilot("ots");
            yield return SayC("CAT_12", 17, 17);                  // The Moon.
            AudioKit.Play("course_set", null, 0.6f);
            yield return ShotV("ca13_the_moon_route_selected");
            holo.Hide();
            // EXTERIOR: the saucer rises above Cattalus, the meadow beyond the city
            yield return RiseAboveCattalus();
            CattalusState.Reach(CattalusLayout.CpDone);
            if (DemoBoot.CaptureDir != null) Cap?.Log($"CATTALUS CHAPTER DONE: {CattalusState.Summary()}; {WorldFlags.Summary()} - the star map now offers only the Moon");
            // Round 19: no "to be continued" - the route to the Moon is set; the console takes them there (the finale)
            if (!FinaleState.Released) yield return UI.Title("TO BE CONTINUED", 0.8f, 2.4f, 0.8f);
            _stroke = 0f; if (_strokeRoutine != null) { StopCoroutine(_strokeRoutine); _strokeRoutine = null; }
            _ik.ReleaseAll("CAT_12 ends");
            yield return ExitStaging("CAT_12 ends");
            ShipConsole.StoryLock = false;
            CattalusHomeDone = true;
            if (DemoBoot.CatPlay) yield break;                  // the player-style run writes its own summary
            if (Auto) { Cap?.Finish($"CATTALUS CHECK RUN DONE: {CattalusState.Summary()}; {WorldFlags.Summary()}"); yield break; }
            yield return ExploreVenus(false);
        }
        void HerFaceCloseUpPilot() { var h = SevenHead; Cut(h + PilotFwd * 1.2f + PilotRight * 0.45f + Up * 0.05f, h - Up * 0.03f, 32f); }
        IEnumerator RiseAboveCattalus()
        {
            var stage = TravelStage.I; Transform saucer = null; Vector3 p0 = new Vector3(0f, 400f, 0f);
            if (stage != null && stage.Saucer != null) { saucer = stage.Saucer; var holder = saucer.parent != null ? saucer.parent.gameObject : saucer.gameObject; holder.SetActive(true); p0 = saucer.position; }
            var berth = p0 + Vector3.down * CattalusLayout.SaucerHover;
            if (_catWorld == null) BuildCattalusExterior(berth); else _catWorld.transform.position = berth;
            if (_berthRing != null) _berthRing.gameObject.SetActive(false);
            CatWin(false);
            var mainCam = Director.Cam.GetComponent<Camera>(); float far0 = mainCam.farClipPlane; mainCam.farClipPlane = 3000f;
            yield return UI.Fade(1f, 0.6f);
            var meadow = berth + CattalusLayout.TreeAt;
            for (float t = 0f; t < 5.5f; t += Time.deltaTime)
            {
                if (t < 0.6f && UI.FadeAlpha > 0.5f) StartCoroutine(UI.Fade(0f, 0.8f));
                if (saucer != null) saucer.position = p0 + Vector3.up * t * t * 3.2f;
                var sp = saucer != null ? saucer.position : p0;
                Cut(berth + new Vector3(60f, 18f + t * 4f, -40f), Vector3.Lerp(sp, meadow + Vector3.up * 8f, 0.35f), 52f);
                if (t > 3.4f && t - Time.deltaTime <= 3.4f) { if (saucer != null) ImageGate.Expect(saucer, "the saucer rising over the city"); yield return ShotV("ca14_the_saucer_rises_above_cattalus_the_meadow_beyond"); }
                yield return null;
            }
            yield return UI.Fade(1f, 1.0f);
            mainCam.farClipPlane = far0;
            if (_catWorld != null) Destroy(_catWorld);
            if (saucer != null) saucer.position = p0;
            CatWin(true);
        }
    }
}
