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
        Transform _ext, _iris, _egg; Transform[] _orbs, _blades; Vector3 _irisAt, _irisN;
        /// <summary>Veyra (a ringed planet, a pale debris band), the egg-shaped mothership (hull lights, traffic, recessed
        /// entrances), the three guardian orbs, the iris - round the exterior saucer of the passage stage (far above the deck).</summary>
        void BuildExterior(Vector3 p0)
        {
            _ext = new GameObject("MothershipExterior").transform;
            var dark = Markers.DarkGloss(); var glow = Markers.Additive(1.4f); var soft = Markers.Additive(0.35f);
            var hull = TravelStage.I != null && TravelStage.I.Saucer != null && TravelStage.I.Saucer.GetComponentInChildren<Renderer>() != null ? TravelStage.I.Saucer.GetComponentInChildren<Renderer>().sharedMaterial : dark;
            GameObject Prim(PrimitiveType t, string n, Vector3 at, Vector3 scale, Material m, Transform parent = null) { var g = GameObject.CreatePrimitive(t); g.name = n; Destroy(g.GetComponent<Collider>()); g.transform.SetParent(parent != null ? parent : _ext, false); g.transform.position = at; g.transform.localScale = scale; g.GetComponent<Renderer>().sharedMaterial = m; return g; }
            var fwd = Vector3.forward;
            // Veyra far beyond, its rings diagonal across the frame
            var veyra = Prim(PrimitiveType.Sphere, "Veyra", p0 + new Vector3(-900f, -260f, 3400f), Vector3.one * 1700f, dark);
            foreach (var (r0, r1, k) in new[] { (1050f, 1550f, 0.5f), (1600f, 1800f, 0.22f) })
            {
                var ring = new GameObject("VeyraRing"); ring.transform.SetParent(_ext, false); ring.transform.position = veyra.transform.position; ring.transform.rotation = Quaternion.Euler(12f, 0f, -24f);
                ring.AddComponent<MeshFilter>().sharedMesh = Markers.RingMesh(r0, r1, 0f); ring.AddComponent<MeshRenderer>().sharedMaterial = Markers.Additive(k);
            }
            // the broad band of pale debris under the saucer
            var rnd = new System.Random(17);
            for (int i = 0; i < 260; i++)
            {
                float x = (float)rnd.NextDouble() * 900f - 450f, z = (float)rnd.NextDouble() * 1400f - 200f, s = 1.5f + (float)rnd.NextDouble() * 9f;
                var g = Prim(PrimitiveType.Sphere, "Debris", p0 + new Vector3(x, -60f - (float)rnd.NextDouble() * 30f, z), new Vector3(s, s * 0.5f, s * 0.8f), i % 3 == 0 ? Markers.Additive(0.25f) : dark);
                g.transform.rotation = Quaternion.Euler((float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 360f, 0f);
            }
            // the mothership: an immense egg (26 miles - shown at a distance that reads; hull lights, traffic, recessed entrances)
            _egg = Prim(PrimitiveType.Sphere, "Mothership", p0 + new Vector3(260f, 40f, 1500f), new Vector3(900f, 1200f, 900f), hull).transform;
            _egg.rotation = Quaternion.Euler(0f, 0f, -12f);
            for (int i = 0; i < 420; i++)
            {
                var d = Random.onUnitSphere; if (d.z > 0.35f) continue;                 // the near side
                var p = _egg.position + Vector3.Scale(d, new Vector3(450f, 600f, 450f)) * 1.002f;
                Prim(PrimitiveType.Sphere, "HullLight", p, Vector3.one * (i % 9 == 0 ? 9f : 3.5f), i % 9 == 0 ? glow : soft);
            }
            for (int i = 0; i < 6; i++) { var d = Quaternion.Euler(Random.Range(-40f, 40f), Random.Range(140f, 220f), 0f) * Vector3.forward; Prim(PrimitiveType.Sphere, "RecessedEntrance", _egg.position + Vector3.Scale(d, new Vector3(452f, 602f, 452f)), new Vector3(60f, 26f, 8f), Markers.DarkGloss()).transform.rotation = Quaternion.LookRotation(d); }
            for (int i = 0; i < 14; i++) { var t = Prim(PrimitiveType.Sphere, "Traffic", _egg.position, Vector3.one * 4f, glow).transform; _traffic.Add((t, Random.Range(0f, 6.28f), Random.Range(520f, 760f), Random.Range(-0.4f, 0.4f))); }
            // the three guardian orbs on separate orbital paths
            _orbs = new Transform[3];
            for (int i = 0; i < 3; i++) { _orbs[i] = Prim(PrimitiveType.Sphere, "GuardianOrb" + i, _egg.position, Vector3.one * (60f - i * 8f), dark).transform; Prim(PrimitiveType.Sphere, "OrbGlow", _orbs[i].position, Vector3.one * 0.35f, glow, _orbs[i]).transform.localPosition = new Vector3(0f, 0f, -0.48f); }
            // the iris: on the hull, facing the saucer's approach; 12 overlapping segments
            _irisN = -fwd; _irisAt = p0 + fwd * 160f;
            _iris = new GameObject("Iris").transform; _iris.SetParent(_ext, false); _iris.position = _irisAt; _iris.rotation = Quaternion.LookRotation(_irisN);
            Prim(PrimitiveType.Cylinder, "IrisWall", _irisAt + fwd * 2f, Vector3.zero, dark).transform.localScale = Vector3.zero;
            var wall = new GameObject("HullSection"); wall.transform.SetParent(_ext, false); wall.transform.position = _irisAt + fwd * 0.6f; wall.transform.rotation = Quaternion.LookRotation(Vector3.up, -fwd); wall.AddComponent<MeshFilter>().sharedMesh = Markers.RingMesh(15.5f, 700f, 0f); wall.AddComponent<MeshRenderer>().sharedMaterial = hull; _hullSection = wall;
            _blades = new Transform[12];
            for (int i = 0; i < 12; i++)
            {
                var b = new GameObject("IrisBlade" + i).transform; b.SetParent(_iris, false); b.localRotation = Quaternion.Euler(0f, 0f, i * 30f);
                var q = Prim(PrimitiveType.Cube, "Segment", Vector3.zero, new Vector3(9f, 18f, 0.6f), i % 2 == 0 ? hull : dark, b);
                q.transform.localPosition = new Vector3(5f, 7f, -0.4f - (i % 2) * 0.3f); q.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
                _blades[i] = b;
            }
            var seam = new GameObject("IrisSeam"); seam.transform.SetParent(_iris, false); seam.AddComponent<MeshFilter>().sharedMesh = Markers.RingMesh(15.5f, 16.2f, 0f); _seamR = seam.AddComponent<MeshRenderer>(); _seamR.sharedMaterial = Markers.Additive(0f);
            seam.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            // inside: the guide lights toward the receiving berth
            for (int i = 0; i < 16; i++) Prim(PrimitiveType.Sphere, "GuideLights", _irisAt + fwd * (12f + i * 8f) + Vector3.down * 10f, Vector3.one * 1.2f, glow);
            Irise(0f);
        }
        Renderer _seamR; GameObject _hullSection; float _far0 = -1f;
        void Approach(bool on) { if (_hullSection != null) _hullSection.SetActive(on); if (_iris != null) _iris.gameObject.SetActive(on); }
        readonly List<(Transform t, float a, float r, float y)> _traffic = new List<(Transform, float, float, float)>();
        /// <summary>0 = shut (the segments meet at the centre), 1 = open just wider than the saucer (~14 m).</summary>
        void Irise(float k)
        {
            if (_blades == null) return;
            for (int i = 0; i < _blades.Length; i++) { var b = _blades[i]; if (b == null) continue; b.localRotation = Quaternion.Euler(0f, 0f, i * 30f + k * 38f); b.localScale = Vector3.one; b.GetChild(0).localPosition = new Vector3(5f + k * 9.5f, 7f + k * 2f, b.GetChild(0).localPosition.z); }
        }
        void ExteriorTick()
        {
            if (_egg == null) return;
            float t = Time.time;
            if (_orbs != null) for (int i = 0; i < _orbs.Length; i++) if (_orbs[i] != null && !(_orbHold && i == 0))
                    _orbs[i].position = _egg.position + Quaternion.Euler(20f * i - 15f, t * (6f + i * 3f) + i * 120f, 0f) * new Vector3(0f, 0f, 820f + i * 160f);
            foreach (var (tr, a, r, y) in _traffic) if (tr != null) tr.position = _egg.position + Quaternion.Euler(y * 40f, a * 57.3f + t * 4f, 0f) * new Vector3(0f, 0f, r);
        }
        bool _orbHold;

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
            BuildExterior(p0); Approach(false);
            var mainCam = Director.Cam != null ? Director.Cam.GetComponent<Camera>() : Camera.main; if (mainCam != null) { _far0 = mainCam.farClipPlane; mainCam.farClipPlane = 9000f; }
            MusicSystem.Play("biaveh_mothership", 3f);
            // EXTREME WIDE: Veyra fills the distance, its rings diagonal; the saucer above the pale debris band; the egg beyond
            var camA = p0 + new Vector3(-120f, 30f, -260f);
            for (float t = 0f; t < 0.2f; t += Time.deltaTime) { ExteriorTick(); Cut(camA, p0 + new Vector3(60f, -20f, 900f), 50f); yield return null; }
            yield return UI.Fade(0f, 1.4f);
            for (float t = 0f; t < 3.2f; t += Time.deltaTime) { ExteriorTick(); if (saucer != null) saucer.position = p0 + Vector3.forward * t * 6f; Cut(camA + Vector3.forward * t * 5f, p0 + new Vector3(60f, -20f, 900f), 50f); yield return null; }
            yield return ShotV("ba01_extreme_wide_veyra_its_rings_the_debris_band");
            // one orb crosses the foreground: the mothership is still far away
            if (_orbs != null && _orbs[1] != null) { var o = _orbs[1]; for (float t = 0f; t < 2.4f; t += Time.deltaTime) { ExteriorTick(); o.position = camA + Vector3.forward * 220f + Vector3.right * Mathf.Lerp(-160f, 160f, t / 2.4f) + Vector3.up * 10f; yield return null; } }
            yield return ShotV("ba02_an_orb_crosses_the_foreground");
            if (saucer != null) saucer.position = p0;
            // INTERIOR: 7 at the controls with Panther in her lap (the window shows Veyra and the mothership)
            yield return EnterStaging("AST_01");
            PlaceRecordsOnArmAsteroid();
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 1f;
            CutPilot("two");
            yield return SayB("BIA_01", 1, 2);                   // Is that Biaveh? / Veyra. The route leads to a Biavian station here.
            WindowOnTheAsteroid();
            yield return ShotV("ba03_the_window_veyra_the_mothership");
            yield return SayB("BIA_01", 3, 4);                   // That's the station? / Their mothership. Twenty-six miles across.
            CutPilot("lap");
            yield return SayB("BIA_01", 5, 6);                   // Across? / Mm-hmm.
            // the three things moving round it
            for (float t = 0f; t < 2.6f; t += Time.deltaTime) { ExteriorTick(); Cut(_egg.position + new Vector3(-700f, 200f, -1600f), _egg.position, 40f); yield return null; }
            yield return ShotV("ba04_three_orbs_round_the_mothership");
            CutPilot("two");
            yield return SayB("BIA_01", 7, 8);                   // And those three things moving around it? / Let's wait to be invited...
            // BIA_02: one orb adjusts its course and holds near their approach; Nela's voice
            _orbHold = true;
            if (_orbs != null && _orbs[0] != null) for (float t = 0f; t < 2f; t += Time.deltaTime) { ExteriorTick(); _orbs[0].position = Vector3.Lerp(_orbs[0].position, p0 + new Vector3(70f, 10f, 120f), Time.deltaTime * 2f); Cut(p0 + new Vector3(-30f, 14f, -40f), p0 + new Vector3(60f, 10f, 110f), 46f); yield return null; }
            yield return ShotV("ba05_an_orb_holds_position_near_their_approach");
            CutPilot("two");
            yield return SayB("BIA_02", 1, 5);                   // Welcome to Veyra. I am Nela... / Oh-hello. / Forgive me... / A little... / A considerable improvement...
            // EXTERIOR TRACKING: the orb moves aside; an unbroken stretch of hull; a circular seam; the iris opens
            Approach(true);
            var cTrack = new Vector3(-26f, 9f, -30f);
            float k0 = 0f;
            bool spoken = false; Coroutine say = null;
            AudioKit.Play("bia_iris_open", null, 0.8f);
            for (float t = 0f; t < 6.5f; t += Time.deltaTime)
            {
                ExteriorTick();
                if (_orbs != null && _orbs[0] != null) _orbs[0].position += Vector3.right * 30f * Time.deltaTime;
                if (saucer != null) saucer.position = p0 + Vector3.forward * Mathf.Lerp(0f, 110f, Mathf.SmoothStep(0f, 1f, t / 6.5f));
                var sp = saucer != null ? saucer.position : p0;
                Cut(sp + cTrack, sp + Vector3.forward * 40f, 48f);
                if (_seamR != null) Glowing.Set(_seamR, Mathf.Clamp01(t - 1f) * 1.4f);
                if (t > 2f) { k0 = Mathf.SmoothStep(0f, 1f, (t - 2f) / 3.5f); Irise(k0); }
                if (!spoken && t > 1.2f) { spoken = true; say = StartCoroutine(SayB("BIA_02", 6, 10)); }   // That's the door? / Apparently. / All that ship... / You have sufficient clearance... / Slowly sounds excellent.
                if (t > 3.6f && t < 3.7f) yield return ShotV("ba06_the_seam_the_iris_opens");
                yield return null;
            }
            while (say != null && Dlg.Busy) { ExteriorTick(); yield return null; }
            // FRONT-FACING: the saucer slips through, its edges close to the aperture without touching
            for (float t = 0f; t < 3.2f; t += Time.deltaTime)
            {
                ExteriorTick();
                if (saucer != null) saucer.position = p0 + Vector3.forward * Mathf.Lerp(110f, 190f, t / 3.2f);
                Cut(_irisAt + Vector3.forward * 40f + Vector3.up * 6f, saucer != null ? saucer.position : _irisAt, 44f);
                if (t > 1.4f && t < 1.5f) yield return ShotV("ba07_front_the_saucer_slips_through_the_iris");
                yield return null;
            }
            // REVERSE from inside: the iris spirals shut behind them; the ringed planet disappears; the guide lights
            AudioKit.Play("bia_iris_close", null, 0.8f);
            for (float t = 0f; t < 3f; t += Time.deltaTime) { ExteriorTick(); Irise(1f - Mathf.SmoothStep(0f, 1f, t / 3f)); Cut(_irisAt + Vector3.forward * 60f + Vector3.up * -4f, _irisAt, 46f); if (t > 2.2f && t < 2.3f) yield return ShotV("ba08_reverse_the_iris_closes_the_planet_disappears"); yield return null; }
            if (_seamR != null) Glowing.Set(_seamR, 0f);
            if (mainCam != null && _far0 > 0f) mainCam.farClipPlane = _far0;
            CutPilot("lap");
            yield return SayB("BIA_02", 11, 13);                 // There. Plenty of room. / Then you can stop holding yours. / Heh. You noticed.
            CutPilot("two");
            yield return SayB("BIA_02", 14, 14);                 // Follow the lights to the receiving berth... We have an intruder in the inner districts.
            yield return ShotV("ba09_follow_the_lights");
            BiavehState.CompleteScene("BIA_01"); BiavehState.CompleteScene("BIA_02");
            BiavehState.Reach(BiavehLayout.CpBerth);
            _ik.ReleaseAll("BIA_02 ends");
            if (_strokeRoutine != null) { StopCoroutine(_strokeRoutine); _strokeRoutine = null; }
            yield return UI.Fade(1f, 1.2f);
            if (DemoBoot.CaptureDir != null) Cap?.Log("mothership arrival aboard done -> the Biaveh scene (the receiving berth)");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Biaveh");
        }

        // ------------------------------------------------------------------------------------------ BIA_12 + BIA_13
        IEnumerator BiavehHome()
        {
            ShipConsole.StoryLock = true; InGalleyScene = true;
            Seven.Frozen = true; Seven.Cutscene = true; Director.Cam.Hold = true;
            Cat.ForceTarget = null; Cat.enabled = false;
            MusicSystem.Stop(0.2f);
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
                var purr = AudioKit.Loop("cat_purr", Cat.transform, null, 0.5f, AudioKit.Bus.Sfx, 1f, 8f);
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
            // LATER: the navigation chair, Panther in her lap
            MusicSystem.Play("aboard_saucer", 3f);
            yield return EnterStaging("AST_10");
            PlaceRecordsOnArmAsteroid();
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 1f;
            CutPilot("two");
            yield return UI.Fade(0f, 1.2f);
            BiavehState.Reach(BiavehLayout.CpDone);
            WorldFlags.MarkBiavehDone();
            var con = ShipConsole.I;
            if (WorldFlags.AsteroidDone && WorldFlags.CattalusDue)
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
        void LateUpdateBiaveh() { if (BiavehMode && _egg != null) ExteriorTick(); }
    }
}
