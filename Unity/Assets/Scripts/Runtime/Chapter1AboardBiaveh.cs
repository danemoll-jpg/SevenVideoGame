using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 17 — the Saucer scene's mothership parts (Docs/CHAPTER_BIAVEH.md):
    /// BIA_01 + BIA_02 (<see cref="BiavehState.ArrivalAboard"/>): the title card over black from the first frame; among Veyra's
    /// rings - the EXTREME WIDE (Veyra filling the distance, its rings diagonal, the saucer above a broad band of pale debris,
    /// the egg-shaped mothership beyond the ring plane, three orbs on their own orbits, one crossing the foreground), 7 at the
    /// controls with Panther in her lap; Nela's telepathic welcome; the orb holds position, the others revolve; the tracking
    /// shot to an unbroken stretch of hull, the circular seam, the iris (overlapping segments rotating open just wider than
    /// the saucer), the front shot as it slips through, the reverse shot from inside as it spirals shut and the ringed planet
    /// disappears, the guide lights - then the Biaveh scene (the receiving berth). The exterior is built here at runtime round
    /// the passage's exterior saucer (TravelStage: the one saucer prefab, unchanged).
    /// BIA_12 + BIA_13 (<see cref="BiavehState.HomeAboard"/>): straight from the beam-up, no gap: she sits on the floor at the
    /// boarding point, he walks into her lap - the full conversation, ending there; later, the navigation chair: Branch A
    /// (the asteroid unfinished) or Branch B (the asteroid complete: the Cattalus lead, once). Then free exploration.
    /// </summary>
    public partial class Chapter1Aboard
    {
        public bool BiavehMode { get; private set; }
        public bool BiavehHomeDone { get; private set; }
        void StartBiaveh()
        {
            bool arrival = BiavehState.ArrivalAboard; BiavehState.ArrivalAboard = false; BiavehState.HomeAboard = false;
            I = this; Active = true; VenusMode = true; MarsMode = true; AsteroidMode = true; BiavehMode = true;
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
            SaucerLocation.Set("biaveh");
            if (DemoBoot.CaptureDir != null)
            {
                ChapterCapture.Ensure(); Cap.Attach(Director.Cam.GetComponent<Camera>());
                if (Auto) { Seven.ScriptDriven = true; Dlg.AutoSkipAfter = DemoBoot.VenusSkip ? 0f : 0.9f; Director.Legend.AutoSwitch = false; }
                Cap.Log($"aboard (mothership mode, {(arrival ? "arrival" : "home")}): {BiavehState.Summary()}; {WorldFlags.Summary()}");
                StartCoroutine(FirstFrameB());
            }
            StartCoroutine(arrival ? BiavehArrival() : BiavehHome());
        }
        IEnumerator FirstFrameB() { Cap.Log($"first frame aboard (the Mothership): fade {UI.FadeAlpha:0.00} (1 = black)"); yield return Cap.Shot("ba00_first_frame_aboard"); }
        IEnumerator SayB(string scene, int from, int to = -1) { yield return SayLines(BiavehDialogue.Scene(scene, from, to < 0 ? from : to), true); }

        // ------------------------------------------------------------------------------------------ the exterior (runtime)
        Transform _ext, _iris; Vector3 _irisAt, _irisN; VeyraScene _veyra; IrisAperture _ap; MothershipInterior _inside;
        static IEnumerator Then(IEnumerator e, System.Action done) { yield return e; done(); }
        Transform _egg => _veyra != null ? _veyra.Egg : null;
        Transform[] _orbs => _veyra != null ? _veyra.Orbs : null;
        /// <summary>QF23 (1): Veyra, its rings and the WHITE mothership stationed among them (the shared VeyraScene - the same
        /// composition as the window view), round the exterior saucer of the passage stage (far above the deck); plus, for the
        /// approach, an unbroken stretch of the white hull with the iris (QF23 4: a real aperture of overlapping leaves).</summary>
        void BuildExterior(Vector3 p0)
        {
            _ext = new GameObject("MothershipExterior").transform;
            _veyra = VeyraScene.Build(_ext, p0, 1f);
            var fwd = Vector3.forward;
            // the door: an unbroken stretch of white hull (a gently curved wall seen up close), a fine spiral seam, the aperture
            _irisN = -fwd; _irisAt = p0 + fwd * 160f;
            _iris = new GameObject("Iris").transform; _iris.SetParent(_ext, false); _iris.position = _irisAt; _iris.rotation = Quaternion.LookRotation(_irisN);
            var wall = new GameObject("HullSection"); wall.transform.SetParent(_iris, false); wall.transform.localPosition = Vector3.zero; wall.transform.localRotation = Quaternion.identity;
            wall.AddComponent<MeshFilter>().sharedMesh = IrisAperture.Annulus(HoleR, 700f, 128);
            var wr = wall.AddComponent<MeshRenderer>(); wr.sharedMaterial = VeyraScene.Pearl(); _hullSection = wall;
            // the seam round the door: a hairline, and the spiral of the leaves' edges reads through it as it opens
            var seam = new GameObject("IrisSeam"); seam.transform.SetParent(_iris, false); seam.transform.localPosition = new Vector3(0f, 0f, 0.03f); seam.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            seam.AddComponent<MeshFilter>().sharedMesh = Markers.RingMesh(HoleR - 0.05f, HoleR + 0.12f, 0f); _seamR = seam.AddComponent<MeshRenderer>(); _seamR.sharedMaterial = Markers.Additive(0f);
            // the leaves: two soft metal greys, darker than the white hull (in the hull's own pearl, half of them vanished against it)
            Material Leaf(string n2, float g, float e) { var m = new Material(VeyraScene.Pearl()) { name = n2 }; if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(g, g, g)); if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", new Color(e, e, e)); if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.85f); return m; }
            var leafA = Leaf("Veyra_iris_leaf_a", 0.62f, 0.10f); var leafB = Leaf("Veyra_iris_leaf_b", 0.50f, 0.07f);
            _ap = IrisAperture.Build(_iris, HoleR, 7.6f, 10, leafA, leafB, Markers.DarkGloss());
            // QF24 (4): inside - the mothership's interior behind the iris (the hall, the guide lights, the receiving berth);
            // hidden until the iris has shut behind the saucer
            _inside = MothershipInterior.Build(_ext, _irisAt);
            if (DemoBoot.CaptureDir != null) Cap?.Log($"BIA_01 exterior (QF23 1): the mothership WHITE (pearl hull, emission) in Veyra's ring plane; iris (QF23 4): {IrisInfo()}");
        }
        const float HoleR = 8.2f;
        string IrisInfo() => _ap != null ? $"10 leaves, open swing {_ap.OpenDeg:0} deg, clear radius {_ap.ClearRadius:0.00} m vs the saucer's 6 m, shut coverage {_ap.Coverage * 100f:0.0}%" : "none";
        Renderer _seamR; GameObject _hullSection; float _far0 = -1f;
        void Approach(bool on) { if (_iris != null) _iris.gameObject.SetActive(on); }
        void ExteriorTick() { if (_veyra != null) _veyra.Tick(_orbHold ? 0 : -1); }
        bool _orbHold;
        /// <summary>QF23 (1): the ship's own window view (a small Veyra round the deck) is hidden while the camera is out at the
        /// exterior stage (from up there it showed as a second, small Veyra far below).</summary>
        void Win(bool on) { if (_win == null) _win = GameObject.Find("Outside_SpaceBiaveh"); if (_win != null) _win.SetActive(on); }
        GameObject _win;

        // ------------------------------------------------------------------------------------------ BIA_01 + BIA_02
        IEnumerator BiavehArrival()
        {
            ShipConsole.StoryLock = true; InGalleyScene = true;
            Seven.Frozen = true; Seven.Cutscene = true; Director.Cam.Hold = true;
            Cat.ForceTarget = null; Cat.enabled = false;
            MusicSystem.Stop(0.5f);
            yield return UI.Title("THE BIAVIAN MOTHERSHIP  —  THE CONNECTED MIND AGE", 1.0f, 2.4f, 1.0f);
            if (DemoBoot.CaptureDir != null) Cap?.Log("title card: 'THE BIAVIAN MOTHERSHIP — THE CONNECTED MIND AGE' over black");
            var stage = TravelStage.I; Transform saucer = null; Vector3 p0 = new Vector3(0f, 400f, 0f);
            if (stage != null && stage.Saucer != null) { saucer = stage.Saucer; var holder = saucer.parent != null ? saucer.parent.gameObject : saucer.gameObject; holder.SetActive(true); p0 = saucer.position; }
            BuildExterior(p0); Approach(false); Win(false);
            var mainCam = Director.Cam != null ? Director.Cam.GetComponent<Camera>() : Camera.main; if (mainCam != null) { _far0 = mainCam.farClipPlane; mainCam.farClipPlane = 12000f; }
            MusicSystem.Play("biaveh_mothership", 3f);
            // EXTREME WIDE: Veyra huge beyond, its rings crossing the frame; the saucer above the band of ice; the white egg
            // rising out of the ring plane among the drifting ice
            var n = _veyra.RingNormal; var toEgg = (_veyra.EggPos - p0).normalized; var side = Vector3.Cross(n, toEgg).normalized;
            var camA = p0 - toEgg * 70f + side * 26f + n * 34f;
            var lookA = Vector3.Lerp(_veyra.EggPos, _veyra.Planet.position, 0.3f);
            for (float t = 0f; t < 0.2f; t += Time.deltaTime) { ExteriorTick(); Cut(camA, lookA, 58f); yield return null; }
            yield return UI.Fade(0f, 1.4f);
            for (float t = 0f; t < 3.2f; t += Time.deltaTime) { ExteriorTick(); if (saucer != null) saucer.position = p0 + toEgg * t * 6f; Cut(camA + toEgg * t * 5f, lookA, 58f); yield return null; }
            yield return ShotV("ba01_extreme_wide_the_white_mothership_in_veyras_rings");
            // one orb crosses the foreground: the mothership is still far away
            if (_orbs != null && _orbs[1] != null) { var o = _orbs[1]; for (float t = 0f; t < 2.4f; t += Time.deltaTime) { ExteriorTick(); o.position = camA + toEgg * 220f + side * Mathf.Lerp(-160f, 160f, t / 2.4f) + n * 10f; Cut(camA + toEgg * 16f, _veyra.EggPos - n * 120f, 50f); yield return null; } }
            yield return ShotV("ba02_an_orb_crosses_the_foreground");
            if (saucer != null) saucer.position = p0;
            // INTERIOR (QF23 3): the pilot's chair at the front window, Panther in her lap; as they talk the hull fades away
            Win(true);
            yield return EnterStaging("BIA_01");
            PlaceRecordsOnArmAsteroid();
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 1f;
            CutPilot("ots");                                     // QF24 (7): "Is that Biaveh?" over her shoulder at the world she asks about
            yield return DialogueFraming.Over("Veyra through the window, past her shoulder (QF24 7)", SayB("BIA_01", 1, 1));
            CutPilot("two");
            yield return SayB("BIA_01", 2, 2);                   // Veyra. The route leads to a Biavian station here.
            StartCoroutine(HullAway(true, "BIA_01", "ba02h"));
            WindowOnTheAsteroid();
            yield return ShotV("ba03_the_window_veyra_the_white_mothership");
            yield return SayB("BIA_01", 3, 4);                   // That's the station? / Their mothership. Twenty-six miles across.
            OpenSpaceShot();
            for (float t = 0f; t < 8f && Director.Reveal != null && Director.Reveal.Progress < 0.99f; t += Time.deltaTime) yield return null;   // the dissolve has finished
            yield return ShotV("ba03b_open_space_round_the_chair_veyras_rings");
            CutPilot("lap");
            yield return SayB("BIA_01", 5, 6);                   // Across? / Mm-hmm. / (QF23 2) I thought their mothership was in the rings of Saturn. / That one is. This is a sister ship. / A sister ship. So… the aunt ship? / That was a terrible joke.
            // the three things moving round it
            Win(false);
            for (float t = 0f; t < 2.6f; t += Time.deltaTime) { ExteriorTick(); Cut(_veyra.EggPos - toEgg * 1900f + side * 500f + n * 160f, _veyra.EggPos, 40f); yield return null; }
            yield return ShotV("ba04_three_orbs_round_the_white_mothership");
            Win(true); CutPilot("two");
            yield return SayB("BIA_01", 7, 8);                   // And those three things moving around it? / Let's wait to be invited...
            // BIA_02: one orb adjusts its course and holds near their approach; Nela's voice
            _orbHold = true; Win(false);
            if (_orbs != null && _orbs[0] != null) for (float t = 0f; t < 2f; t += Time.deltaTime) { ExteriorTick(); _orbs[0].position = Vector3.Lerp(_orbs[0].position, p0 + toEgg * 120f + side * 70f + n * 10f, Time.deltaTime * 2f); Cut(p0 - toEgg * 40f - side * 30f + n * 14f, p0 + toEgg * 110f + side * 60f, 46f); yield return null; }
            yield return ShotV("ba05_an_orb_holds_position_near_their_approach");
            Win(true); CutPilot("two");
            yield return SayB("BIA_02", 1, 5);                   // Welcome to Veyra. I am Nela... / Oh-hello. / Forgive me... / A little... / A considerable improvement...
            // QF24 (4): the vanish effect ends here, before the door - the hull comes back round them while they are still
            // in the cabin; from the door on, everything is shown from outside the saucer
            CutPilot("wide");
            yield return HullAway(false, "BIA_01");
            yield return ShotV("ba05b_the_hull_back_before_the_door");
            // QF23 (4): EXTERIOR - the orb moves aside; the saucer flies right up to the door (a closed, seamless circle of white
            // hull, the fine spiral seam) and HOVERS in front of it while they talk about it
            Approach(true); _ap.Set(0f); Win(false);
            var hover = _irisAt - Vector3.forward * 30f;
            float s0 = 0f;
            for (float t = 0f; t < 4.2f; t += Time.deltaTime)
            {
                ExteriorTick();
                if (_orbs != null && _orbs[0] != null) _orbs[0].position += side * 30f * Time.deltaTime;
                s0 = Mathf.SmoothStep(0f, 1f, t / 4.2f);
                if (saucer != null) saucer.position = Vector3.Lerp(p0, hover, s0);
                var sp = saucer != null ? saucer.position : p0;
                Cut(sp + new Vector3(-24f, 7f, -26f), sp + Vector3.forward * 40f, 46f);
                if (_seamR != null) Glowing.Set(_seamR, Mathf.Clamp01(t - 1.5f) * 0.9f);
                yield return null;
            }
            yield return ShotV("ba06_the_saucer_hovers_at_the_closed_door");
            // the conversation about the door, hovering in front of it (a slow drift round the saucer, the door behind it)
            // QF24 (3): the door talk runs beside the hover shot; the scene waits for the TALK to finish (before, it waited on
            // Dlg.Busy, which is false in the pause between two lines - the scene moved on and lines 11-13 started over 7-10)
            bool doorTalkDone = false;
            StartCoroutine(Then(SayB("BIA_02", 6, 10), () => doorTalkDone = true));   // That's the door? / Apparently. / All that ship... / You have sufficient clearance... / Slowly sounds excellent.
            for (float t = 0f; !doorTalkDone || t < 0.5f; t += Time.deltaTime)
            {
                ExteriorTick();
                if (saucer != null) saucer.position = hover + Vector3.up * Mathf.Sin(t * 1.3f) * 0.25f;
                float a = Mathf.Lerp(-0.5f, 0.5f, Mathf.Clamp01(t / 14f));
                var cp = hover + Quaternion.Euler(0f, a * 40f, 0f) * new Vector3(-18f, 5f, -16f);
                Cut(cp, hover + Vector3.forward * 22f, 44f);
                if (t > 2f && t < 2.1f) yield return ShotV("ba06b_hovering_the_door_behind_the_saucer");
                yield return null;
            }
            // FRONT, then the snap: the leaves swing and spiral back just wider than the saucer, a sharp whirr-click
            var front = _irisAt - Vector3.forward * 58f + Vector3.up * 3f + Vector3.right * 9f;
            Cut(front, _irisAt, 40f);
            yield return Wait(0.35f);
            AudioKit.Play("bia_iris_snap_open", null, 1f);
            if (DemoBoot.CaptureDir != null) Cap?.Log($"BIA_02 (QF23 4): the iris snaps open - {IrisInfo()}");
            for (float t = 0f; t < 0.45f; t += Time.deltaTime) { ExteriorTick(); _ap.Set(Mathf.SmoothStep(0f, 1f, t / 0.45f)); Cut(front, _irisAt, 40f); if (t > 0.2f && t < 0.24f) yield return ShotV("ba07_front_the_leaves_spiral_open"); yield return null; }
            _ap.Set(1f);
            // the saucer slips through in that instant
            for (float t = 0f; t < 0.9f; t += Time.deltaTime)
            {
                ExteriorTick();
                if (saucer != null) saucer.position = Vector3.Lerp(hover, _irisAt + Vector3.forward * 24f, Mathf.SmoothStep(0f, 1f, t / 0.9f));
                Cut(front + Vector3.forward * 6f, saucer != null ? saucer.position : _irisAt, 42f);
                if (t > 0.42f && t < 0.46f) yield return ShotV("ba07b_the_saucer_slips_through");
                yield return null;
            }
            // REVERSE from inside: the leaves spiral shut very quickly behind them - a solid clack; the ringed planet disappears
            if (_inside != null) _inside.gameObject.SetActive(true);      // QF24 (4): the interior round the reverse shot
            var inside = _irisAt + Vector3.forward * 46f + Vector3.up * -3f + Vector3.right * 4f;
            AudioKit.Play("bia_iris_snap_shut", null, 1f);
            for (float t = 0f; t < 0.35f; t += Time.deltaTime) { ExteriorTick(); _ap.Set(1f - Mathf.SmoothStep(0f, 1f, t / 0.35f)); Cut(inside, _irisAt, 44f); if (t > 0.15f && t < 0.19f) yield return ShotV("ba08_reverse_the_leaves_spiral_shut"); yield return null; }
            _ap.Set(0f); CameraShake.Quake(0.12f, 0.15f);
            yield return Wait(0.5f);
            yield return ShotV("ba08b_shut_the_planet_gone");
            if (_seamR != null) Glowing.Set(_seamR, 0f);
            yield return InsideTheMothership(saucer);
            if (mainCam != null && _far0 > 0f) mainCam.farClipPlane = _far0;
            BiavehState.CompleteScene("BIA_01"); BiavehState.CompleteScene("BIA_02");
            BiavehState.Reach(BiavehLayout.CpBerth);
            _ik.ReleaseAll("BIA_02 ends");
            if (_strokeRoutine != null) { StopCoroutine(_strokeRoutine); _strokeRoutine = null; }
            yield return UI.Fade(1f, 1.2f);
            if (DemoBoot.CaptureDir != null) Cap?.Log("mothership arrival aboard done -> the Biaveh scene (the receiving berth)");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Biaveh");
        }

        /// <summary>QF24 (4) - after the iris: they are INSIDE the mothership, shown from outside the saucer only (no cabin
        /// shots). Veyra, its rings and the orbs are behind the shut iris, so they are gone from every view; the saucer glides
        /// along the interior approach over the rippling guide lights, the vast interior fading into the bright haze, and
        /// settles into the receiving berth. BIA_02_11-14 play over these shots (declared voice-overs).</summary>
        IEnumerator InsideTheMothership(Transform saucer)
        {
            if (_inside == null || saucer == null) { yield return SayB("BIA_02", 11, 14); yield break; }
            if (_veyra != null && _veyra.Root != null) _veyra.Root.gameObject.SetActive(false);   // the outside matches where they are
            if (_hullSection != null) _hullSection.SetActive(false);
            Approach(false); Win(false);
            _inside.gameObject.SetActive(true);
            if (DemoBoot.CaptureDir != null) Cap?.Log($"BIA_02 inside (QF24 4): the iris shut behind them - Veyra / rings / orbs hidden {(_veyra != null && _veyra.Root != null && !_veyra.Root.gameObject.activeSelf)}, the interior shown ({_inside.LightCount} guide lights rippling toward the berth), window view off, no cabin shots from here");
            DialogueFraming.VoiceOver = "the saucer flying inside the mothership (QF24 4: exterior only)";
            bool done = false;
            StartCoroutine(Then(SayB("BIA_02", 11, 14), () => done = true));   // There. Plenty of room. / ... / Follow the lights to the receiving berth...
            var berth = _inside.Berth.position;
            float z0 = 24f, z1 = MothershipInterior.BerthZ - 30f;
            // 1) tracking beside and below: the saucer glides in, the hall opening up ahead, the lights rippling forward
            float glide = 0f; const float Speed = 16f;
            System.Func<Vector3> P = () => saucer.position;
            for (float t = 0f; t < 7f; t += Time.deltaTime)
            {
                glide += Speed * Time.deltaTime; float z = Mathf.Min(z0 + glide, z1);
                saucer.position = _inside.Route(z) + Vector3.up * Mathf.Sin(t * 1.1f) * 0.2f;
                Cut(P() + new Vector3(-26f, -7f, -14f), P() + Vector3.forward * 26f + Vector3.down * 4f, 50f);
                if (t > 2.5f && t < 2.55f) yield return ShotV("ba09_inside_the_saucer_glides_in_the_guide_lights_ripple");
                yield return null;
            }
            // 2) ahead and low over the route: the lights run toward the camera, the saucer following them
            for (float t = 0f; t < 6f; t += Time.deltaTime)
            {
                glide += Speed * Time.deltaTime; float z = Mathf.Min(z0 + glide, z1);
                saucer.position = _inside.Route(z);
                var cam = _inside.Route(Mathf.Min(z + 70f, z1 + 20f)) + new Vector3(9f, -30f, 0f);
                Cut(cam, P() + Vector3.down * 2f, 44f);
                if (t > 2f && t < 2.05f) yield return ShotV("ba09b_inside_ahead_the_lights_lead_to_the_berth");
                yield return null;
            }
            // 3) high and wide: the saucer settles into the receiving berth, the docking arms round it
            var from = saucer.position;
            for (float t = 0f; t < 5f || !done; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 5f));
                saucer.position = Vector3.Lerp(from, berth, u);
                Cut(berth + new Vector3(-48f, 26f, -40f), Vector3.Lerp(from, berth, 0.7f), 46f);
                if (t > 4.6f && t < 4.65f) yield return ShotV("ba09c_inside_the_saucer_settles_into_the_receiving_berth");
                if (t > 25f) break;                                   // watchdog
                yield return null;
            }
            DialogueFraming.VoiceOver = null;
            if (DemoBoot.CaptureDir != null) Cap?.Log($"BIA_02 inside (QF24 4): the saucer settled in the receiving berth ({Vector3.Distance(saucer.position, berth):0.00} m from the pad); BIA_02_11-14 played over exterior shots; cabin shots in this stretch 0");
        }

        // ------------------------------------------------------------------------------------------ BIA_12 + BIA_13
        IEnumerator BiavehHome()
        {
            ShipConsole.StoryLock = true; InGalleyScene = true;
            Seven.Frozen = true; Seven.Cutscene = true; Director.Cam.Hold = true;
            Cat.ForceTarget = null; Cat.enabled = false;
            MusicSystem.Board();                                 // QF24 (21): back aboard = the saucer theme, every time (it was stopped here until the chair talk)
            if (!BiavehState.SceneDone("BIA_12"))
            {
                // straight from the beam-up: she sits on the floor at the arrival point; he walks into her lap
                var at = new Vector3(-1.2f, 0f, -4.6f); var fwd = new Vector3(0.2f, 0f, 1f).normalized; var side = Vector3.Cross(Up, fwd);
                Seven.transform.SetPositionAndRotation(at, Quaternion.LookRotation(fwd));
                Seven.PlayClip("idle", 0f); var rest = SevenRestPose.On(Seven.gameObject, SevenRestPose.Kind.SitFloor);
                yield return null; yield return null;
                var lap = at + fwd * 0.42f + Up * 0.22f;
                var catFrom = at + fwd * 2.0f + side * 0.6f;
                Cat.transform.SetPositionAndRotation(catFrom, Quaternion.LookRotation(Flat(at - catFrom)));
                CatClip("walk", 0.05f);
                var mid = (at + catFrom) * 0.5f;
                CamTo(mid + side * 2.4f + Up * 0.9f + fwd * 0.2f, mid + Up * 0.4f, 0f, 44f);
                StartCoroutine(UI.Fade(0f, 0.7f));
                for (float t = 0f; t < 1f; t += Time.deltaTime / 1.8f) { var p = Vector3.Lerp(catFrom, lap, Mathf.SmoothStep(0f, 1f, t)); p.y = Mathf.Lerp(catFrom.y, lap.y, Mathf.Clamp01((t - 0.75f) / 0.25f)); Cat.transform.position = p; yield return null; }
                Cat.transform.SetPositionAndRotation(lap, Quaternion.LookRotation(-side, Up));
                CatState("sit");
                yield return ShotV("ba10_she_sits_on_the_floor_he_walks_into_her_lap");
                var head = SevenHead;
                CamTo(head + fwd * 1.6f + side * 0.7f + Up * 0.1f, (head + lap) * 0.5f, 0.4f, 38f);
                if (_catPose != null) _catPose.LookAt = head;
                yield return SayB("BIA_12", 1, 3);               // There are chairs. / Mm. This was closer. / Fair enough.
                var purr = Purr.Loop(Cat.transform, 1f)   /* QF23 (11): loud, on the voice bus */;
                CatState("sleep");
                yield return Wait(2.4f);                         // BIA_12_04: a quiet pause, audible purring
                yield return ShotV("ba11_the_purr_carries_the_pause");
                _ik.RightW = 1f; _ik.RightPos = lap + Up * 0.18f;
                yield return SayB("BIA_12", 5, 7);               // Is the ship still vibrating? / Apparently. / Heh.
                yield return Wait(1.4f);                         // a comfortable silence
                CamTo(lap + fwd * 0.9f + side * 0.4f + Up * 0.4f, lap + Up * 0.1f, 0.5f, 34f);
                yield return SayB("BIA_12", 8, 13);              // They asked if I wanted to share the vision... / I think that's why I want to.
                CamTo(head + fwd * 1.4f - side * 0.6f + Up * 0.15f, head + Down() * 0.2f, 0.5f, 36f);
                yield return SayB("BIA_12", 14, 16);             // Someone was leaving a memory behind... / But you remember being there. / Mm-hmm. Waiting...
                if (_catPose != null) _catPose.LookAt = head;    // BIA_12_17: he presses his head into her hand
                _ik.RightPos = lap + Up * 0.26f + fwd * 0.05f;
                yield return Wait(1.6f);
                yield return ShotV("ba12_he_presses_his_head_into_her_hand");
                CamTo(mid + side * 2.0f + Up * 0.7f, (head + lap) * 0.5f, 0.6f, 42f);
                yield return SayB("BIA_12", 18, 22);             // You're very good at that. / Waiting? / Staying. / Well. You are quite warm. / Heh. There it is.
                yield return ShotV("ba13_there_it_is");
                if (purr != null) StartCoroutine(AudioKit.Fade(purr, 0f, 1.5f, true));
                _ik.ReleaseAll("the reunion ends");
                if (rest != null) Destroy(rest);
                if (_catPose != null) _catPose.LookAt = null;
                BiavehState.CompleteScene("BIA_12");
                yield return UI.Fade(1f, 1.2f);                  // end the scene here - no extra joke
            }
            // LATER: the navigation chair, Panther in her lap (the saucer theme already playing since boarding)
            MusicSystem.Play("aboard_saucer", 3f);
            yield return EnterStaging("AST_10");
            PlaceRecordsOnArmAsteroid();
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 1f;
            CutPilot("two");
            yield return UI.Fade(0f, 1.2f);
            BiavehState.Reach(BiavehLayout.CpDone);
            WorldFlags.MarkBiavehDone();
            var con = ShipConsole.I;
            var nextTalk = WorldFlags.NextTalk("biaveh");                          // QF24 (22): both done = the Cattalus talk, whatever was seen before
            if (DemoBoot.CaptureDir != null) Cap?.Log($"NEXT TALK (QF24 22): the mothership finished, asteroid done {WorldFlags.AsteroidDone}, mothership done {WorldFlags.BiavehDone} -> {nextTalk}");
            if (nextTalk == "BIA_13B")
            {
                if (DemoBoot.CaptureDir != null) Cap?.Log($"BIA_13B (the Cattalus lead) plays: {WorldFlags.Summary()}");
                NavHand(true); CutPilot("ots");
                if (con != null) con.ShowMap(null, "The Biavian record  ·  the asteroid record");
                yield return SayB("BIA_13B", 1, 2);
                Cat.LapNudge(-12f); CutPilot("lap");
                yield return SayB("BIA_13B", 3, 7);
                yield return SayB("BIA_13B", 8, 11);
                WorldFlags.MarkCattalusSeen();
                if (con != null) yield return con.ComingSoonCard("Cattalus (coming soon)");
            }
            else
            {
                if (DemoBoot.CaptureDir != null) Cap?.Log($"BIA_13A (the asteroid unfinished): {WorldFlags.Summary()}");
                NavHand(true); CutPilot("ots");
                if (con != null) con.ShowMap("asteroid", "The asteroid  ·  the next destination");
                yield return SayB("BIA_13A", 1, 2);              // We still have our little detour. / The one covered in machinery.
                CutPilot("lap");
                yield return SayB("BIA_13A", 3, 5);              // You make it sound inviting. / I'm reserving judgment... / Somewhere memorable.
                yield return ShotV("ba14_the_asteroid_selected");
            }
            if (DemoBoot.CaptureDir != null) Cap?.Log($"MOTHERSHIP CHAPTER DONE: {BiavehState.Summary()}; {WorldFlags.Summary()}");
            _stroke = 0f; if (_strokeRoutine != null) { StopCoroutine(_strokeRoutine); _strokeRoutine = null; }
            _ik.ReleaseAll("the mothership's departure routing ends");
            yield return ExitStaging("the mothership's departure routing ends");
            ShipConsole.StoryLock = false;
            BiavehHomeDone = true;
            if (Auto) { Cap.Finish($"MOTHERSHIP CHECK RUN DONE: {BiavehState.Summary()}; {WorldFlags.Summary()}"); yield break; }
            yield return ExploreVenus(false);
        }
        static Vector3 Down() => Vector3.down;
        void LateUpdateBiaveh() { if (BiavehMode && _veyra != null) ExteriorTick(); }
    }
}
