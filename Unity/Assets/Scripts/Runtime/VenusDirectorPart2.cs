using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// Round 12 — Chapter 2 "Venus", part 2 (Docs/CHAPTER2_VENUS.md): the way down on foot from the observation area
    /// (the root gate, the root pit, the tunnels, the root chamber) into VEN_05; C06 Wormmaster above his machinery; the
    /// nonlethal encounter (three control devices disabled with existing abilities - interaction, a climb + interaction,
    /// telekinesis; creatures on visible troughs, telegraphed by dust and rumble; a creature that reaches her knocks her
    /// back - white fade, back on the walkway, nothing hurt; the creatures withdraw when all three are off; the console
    /// closes the containment barrier - only containment completes it); C07; B06 the return ride through the changed
    /// gardens; C08 - C10 at the temple (the vision video, the match cut to her empty palm, the new C09 lines, the cube
    /// copy awarded exactly once); B07 back to the saucer (the bike glides in through the hatch, the hull seamless again),
    /// then the debrief (C11) aboard. Return visits keep the changed state. Every wait has the QF10 hint / skip.
    /// </summary>
    public partial class VenusDirector
    {
        // ---- Round 12 scene objects (FightDemoBuilderVenus2)
        public GameObject RootGate;
        public ControlDevice[] Devices;
        public Transform Crystal, Barrier, Console, RecordCopy, LocationsDisplay;
        public Renderer BarrierGlow, ConsoleGlow;
        public WormmasterRig Wormmaster;
        public WormCreature[] ChamberWorms;
        public Light[] UnderLights;
        public GameObject RepairedChannel, DamagedChannel;
        public ElarimNpc[] ReturnWorkers;
        public UnityEngine.Video.VideoClip Vision2;

        public bool ChapterDone { get; private set; }
        bool _reloading;
        /// <summary>For the player-style run's last line (the Venus scene is gone by then): knockdowns, hints, skips.</summary>
        public static int LastKnockdowns, LastHints, LastSkips;
        void LateUpdate() { LastKnockdowns = Knockdowns; LastHints = Hints; LastSkips = Skips; }
        public int Knockdowns { get; private set; }
        public bool DestinationReached;
        AudioSource _machineHum, _ambUnder;
        float _under;                                   // 0 above ground .. 1 underground (music, light, fog)

        // ------------------------------------------------------------------------------------------ flow (part 2)
        IEnumerator Part2()
        {
            if (VenusState.SceneDone("VEN_C11")) { yield return ReturnVisit(); yield break; }
            if (!VenusState.SceneDone("VEN_C06")) { yield return WayDown(); yield return C06(); if (ReloadCheck("VEN_C06")) yield break; }
            if (!VenusState.SceneDone("VEN_C07")) { yield return Encounter(); if (_reloading) yield break; yield return C07(); if (ReloadCheck("VEN_C07")) yield break; }
            if (!VenusState.SceneDone("VEN_B06")) { yield return B06(); if (ReloadCheck("VEN_B06")) yield break; }
            if (!VenusState.SceneDone("VEN_C08")) yield return C08();
            if (!VenusState.SceneDone("VEN_C10")) { yield return C09C10(); if (ReloadCheck("VEN_C10")) yield break; }
            if (!VenusState.SceneDone("VEN_C12")) { yield return C12(); if (ReloadCheck("VEN_C12")) yield break; }     // Round 13 (0m)
            if (!VenusState.SceneDone("VEN_B07")) yield return B07();
            yield return Aboard();
        }

        /// <summary>The state of part 2's world for the stage reached (reload, Continue, Scene Select, return visits).</summary>
        void RestorePart2(ref Vector3 at, ref Vector3 face)
        {
            // QF12 (8): the chamber creatures' ground is the control chamber's floor. BaseY is not serialized, so the builder's
            // scene never carried it: they used the basin's height (-12) and swam 18 m above the chamber, inside the rock -
            // invisible to the player while they still "lunged" and knocked her back.
            if (ChamberWorms != null) foreach (var w in ChamberWorms) if (w != null) { w.BaseY = VenusLayout.ControlY; var p = w.transform.position; p.y = VenusLayout.ControlY - w.Depth; w.transform.position = p; }
            bool c05 = VenusState.SceneDone("VEN_C05");
            SetZone("RootLanding", c05); SetZone("RootPit", c05);
            if (RootGate != null) RootGate.SetActive(!c05);
            if (Devices != null) foreach (var d in Devices) if (d != null && VenusState.IsDisabled(d.Id)) { d.SetOff(); if (d.Id == "dev_crystal" && Crystal != null) Crystal.position = CrystalRest; }
            if (ChamberWorms != null) foreach (var w in ChamberWorms) if (w != null && (VenusState.IsDisabled(w.Group) || VenusState.WormmasterContained)) w.gameObject.SetActive(false);
            if (Barrier != null) Barrier.position = BarrierPos(VenusState.WormmasterContained ? 1f : 0f);
            if (BarrierGlow != null) Glow(BarrierGlow, VenusState.WormmasterContained ? 1.4f : 0f);
            if (Wormmaster != null) Wormmaster.Current = VenusState.WormmasterContained ? WormmasterRig.Pose.Contained : WormmasterRig.Pose.Dominant;
            if (VenusState.AtLeast(VenusState.Stage.ThreatStopped)) ApplyThreatStopped(false);
            if (Cat != null && VenusState.SceneDone("VEN_B02")) Cat.gameObject.SetActive(false);   // spec: by B02 Panther is back inside, normal size
            if (RecordCopy != null) RecordCopy.gameObject.SetActive(false);
            // where she is
            if (VenusState.SceneDone("VEN_C11")) { at = VenusLayout.StepOut + Vector3.forward * 2.5f; face = Vector3.forward; Bike.Place(VenusLayout.BikeUnpacked, 0f); Bike.gameObject.SetActive(true); }
            else if (VenusState.SceneDone("VEN_C10")) { at = VenusLayout.ParkSpot - Vector3.right * 1.7f; face = Vector3.forward; }
            else if (VenusState.SceneDone("VEN_B06")) { at = VenusLayout.ParkSpot - Vector3.right * 1.7f + Vector3.forward * 2f; face = Vector3.forward; }
            else if (VenusState.SceneDone("VEN_C07")) { at = (VenusState.BikeKnown ? VenusState.BikeAt : VenusLayout.ObsPark) - Vector3.right * 1.4f; face = Vector3.forward; }
            else if (VenusState.SceneDone("VEN_C06")) { at = VenusLayout.ControlEntry; face = Vector3.right; }
            if (VenusState.SceneDone("VEN_B06") && Guardian != null && !VenusState.SceneDone("VEN_C08")) { Guardian.transform.position = VenusLayout.MeetSpot + Vector3.forward * 1.6f; }
            if (VenusState.SceneDone("VEN_C08") && Guardian != null) Guardian.transform.position = VenusLayout.CubeView + new Vector3(1.8f, 0f, 2f);
        }

        static Vector3 CrystalRest => VenusLayout.DeviceUse[2] + new Vector3(1.2f, 0.12f, 0.6f);
        static Vector3 BarrierPos(float up) => new Vector3(VenusLayout.DaisAt.x, Mathf.Lerp(VenusLayout.ControlY - 7.6f, VenusLayout.ControlY, up), VenusLayout.DaisAt.z);

        /// <summary>Section 9: after the threat is stopped the pulses cease, the creatures are free (calm, away from the
        /// foundations), workers are back on the paths, water runs in the repaired channel; the damage stays visible.</summary>
        void ApplyThreatStopped(bool log)
        {
            _pulsesOn = false;
            if (Worms != null) for (int i = 0; i < Worms.Length; i++) if (Worms[i] != null && !Worms[i].Calm)
                {
                    var home = VenusLayout.BasinCentre + new Vector3(16f, 0f, -6f);
                    var r = new Vector3[5]; for (int k = 0; k < 5; k++) r[k] = home + new Vector3(Mathf.Cos(i * 1.4f + k * 1.25f) * 12f, 0f, Mathf.Sin(i * 1.4f + k * 1.25f) * 14f);
                    Worms[i].SetCalm(r);
                }
            if (RepairedChannel != null) RepairedChannel.SetActive(true);
            if (ReturnWorkers != null) foreach (var w in ReturnWorkers) if (w != null) { w.gameObject.SetActive(true); w.Play(w.PostClip); }
            if (_machineHum != null) { _machineHum.Stop(); _machineHum = null; }
            if (UnderLights != null) foreach (var l in UnderLights) if (l != null) l.intensity = 0.6f;
            if (log) Log($"the world after the threat (section 9): signal pulses stopped, {Worms?.Length ?? 0} basin creatures calm (routes away from the foundations), repaired channel with water {(RepairedChannel != null && RepairedChannel.activeSelf ? "on" : "-")}, {ReturnWorkers?.Length ?? 0} workers back on the paths, damage still there (gully, rubble, broken channel pieces)");
        }

        // ------------------------------------------------------------------------------------------ underground look + sound
        float[] _atmo0; float _amb0; Light[] _sunLights; float[] _sun0;
        IEnumerator UndergroundLook()
        {
            if (Atmo == null) yield break;
            var a = Atmo;
            _atmo0 = new[] { a.GroundDensity, a.GroundBase, a.ValleyDensity, a.DistanceDensity, a.NearBrightness, a.FarBrightness, a.ZenithBrightness, a.GlowIntensity, a.FogStart };
            float[] cave = { 0.007f, -31f, 0f, 0.0035f, 0.12f, 0.36f, 0.08f, 0.12f, 6f };
            _amb0 = RenderSettings.ambientLight.r;
            var ls = new List<Light>(); foreach (var l in Atmo.GetComponentsInChildren<Light>(true)) if (l.type == LightType.Directional) ls.Add(l);
            _sunLights = ls.ToArray(); _sun0 = new float[_sunLights.Length]; for (int i = 0; i < _sunLights.Length; i++) _sun0[i] = _sunLights[i].intensity;
            bool wasUnder = false;
            while (true)
            {
                bool under = VenusLayout.Underground(P7);
                _under = Mathf.MoveTowards(_under, under ? 1f : 0f, Time.deltaTime / 2.5f); if (_wild != null) _wild.Under = _under;
                float k = Mathf.SmoothStep(0f, 1f, _under);
                a.GroundDensity = Mathf.Lerp(_atmo0[0], cave[0], k); a.GroundBase = Mathf.Lerp(_atmo0[1], cave[1], k); a.ValleyDensity = Mathf.Lerp(_atmo0[2], cave[2], k);
                a.DistanceDensity = Mathf.Lerp(_atmo0[3], cave[3], k); a.NearBrightness = Mathf.Lerp(_atmo0[4], cave[4], k); a.FarBrightness = Mathf.Lerp(_atmo0[5], cave[5], k);
                a.ZenithBrightness = Mathf.Lerp(_atmo0[6], cave[6], k); a.GlowIntensity = Mathf.Lerp(_atmo0[7], cave[7], k); a.FogStart = Mathf.Lerp(_atmo0[8], cave[8], k);
                float amb = Mathf.Lerp(_amb0, 0.2f, k); RenderSettings.ambientLight = new Color(amb, amb, amb);
                for (int i = 0; i < _sunLights.Length; i++) _sunLights[i].intensity = _sun0[i] * (1f - 0.9f * k);
                if (under != wasUnder)
                {
                    wasUnder = under;
                    // the threshold: the underground music (its whispers ducked under dialogue by the music system) and the cave ambience
                    MusicSystem.Play(under ? "venus_underground" : "venus_gardens", 3f);
                    if (_ambUnder == null) _ambUnder = AudioKit.Loop("amb_underground", null, null, 0f, AudioKit.Bus.Ambience);
                    if (_ambUnder != null) StartCoroutine(AudioKit.Fade(_ambUnder, under ? 0.8f : 0f, 3f));
                    if (_ambGardens != null) StartCoroutine(AudioKit.Fade(_ambGardens, under ? 0f : 1f, 3f));
                    Log($"music: {(under ? "below ground -> venus_underground" : "above ground -> venus_gardens")} (crossfade 3 s at {P7:0.0})");
                }
                yield return null;
            }
        }

        bool _pulsesOn;
        /// <summary>Before the threat is stopped the gardens stay under pressure: the signal sounds from below every ~25 s
        /// and the basin creatures turn toward the foundations.</summary>
        IEnumerator AmbientPulses()
        {
            _pulsesOn = true;
            while (_pulsesOn)
            {
                for (float t = 0f; t < 25f && _pulsesOn; t += Time.deltaTime) yield return null;
                if (!_pulsesOn || VenusState.AtLeast(VenusState.Stage.ThreatStopped)) break;
                if (_under > 0.5f) continue;
                AudioKit.Play("signal_pulse", VenusLayout.BasinCentre, 0.5f, 0.05f, AudioKit.Bus.Sfx, 160f);
                if (Worms != null) foreach (var w in Worms) if (w != null) w.Drive(VenusLayout.Foundations, 8f);
            }
        }

        // ------------------------------------------------------------------------------------------ the way down (B2)
        IEnumerator OpenRootGate(bool animate)
        {
            SetZone("RootLanding", true); SetZone("RootPit", true);
            if (RootGate == null || !RootGate.activeSelf) yield break;
            if (animate)
            {
                AudioKit.Play("stone_door", VenusLayout.RootGateIn, 0.9f, 0f, AudioKit.Bus.Sfx, 50f);
                var p0 = RootGate.transform.position;
                for (float t = 0f; t < 1f; t += Time.deltaTime / 3f) { RootGate.transform.position = p0 + Vector3.down * 5.2f * Mathf.SmoothStep(0f, 1f, t); yield return null; }
            }
            RootGate.SetActive(false);
            Log("the root gate opened (after C05): the way down - the root pit, the tunnels, the root chamber");
        }

        IEnumerator WayDown()
        {
            Objective(VenusState.Objectives[(int)VenusState.Stage.ControlDiscovered]);
            yield return OpenRootGate(true);
            if (Shots) { var wb = CameraOccluders.WalkBlockers(out int nb); Log($"walk blockers (QF12 7) after the root gate opened: {nb} blocked samples in the walkable ground - {wb}"); }
            if (Shots) { Shoot(VenusLayout.RootGateIn + new Vector3(-4.5f, 4.2f, 9f), VenusLayout.RootGateIn + new Vector3(0f, -3f, -5f), 50f, 0.01f); yield return Wait(0.3f); yield return Shot("w01_root_gate_open"); FollowCamera(); }
            if (Auto) StartCoroutine(AutoWalkPath(new[] { new Vector3(69f, -6f, 137f), new Vector3(69f, -6f, 132.5f), new Vector3(69f, -14f, 117.4f), new Vector3(69f, -18f, 107f), new Vector3(69f, -22f, 95f), new Vector3(71f, -22f, 89f), new Vector3(80f, -22f, 89f), new Vector3(96f, -27.4f, 89f), VenusLayout.ControlEntry }));
            if (Shots) StartCoroutine(WayDownShots());
            StartCoroutine(WayDownHarass());                             // Round 13 (0f)
            yield return Until(() => P7.x > 104.5f && P7.y < -28f, "the control chamber (C06)", 120f,
                "Through the root gate below the lookout, down the root pit and along the tunnels to the east", VenusLayout.ControlEntry,
                () => Seven.Place(VenusLayout.ControlEntry, Vector3.right));
        }
        IEnumerator WayDownShots()
        {
            var marks = new (Func<bool>, string)[] { (() => P7.z < 126f && P7.y < -9f, "w02_root_pit_steps"), (() => P7.z < 108f && P7.z > 98f, "w03_root_tunnel"), (() => P7.z < 92f && P7.x < 78f, "w04_root_chamber"), (() => P7.x > 88f && P7.x < 100f, "w05_deep_tunnel") };
            foreach (var (when, name) in marks) { while (!when() && !VenusState.SceneDone("VEN_C06")) yield return null; if (VenusState.SceneDone("VEN_C06")) yield break; yield return Shot(name); }
        }

        // ------------------------------------------------------------------------------------------ C06
        Vector3 WmHead => Wormmaster != null ? Wormmaster.transform.position + Vector3.up * 3.4f : VenusLayout.WormmasterAt + Vector3.up * 3.4f;
        void LowOnWormmaster(float ease = 0.01f, float push = 0f)
        {
            var toHer = Flat(P7 - VenusLayout.DaisAt).normalized; var r = Vector3.Cross(Vector3.up, toHer);
            var cam = VenusLayout.DaisAt + toHer * (11f - push) + r * 2.2f + Vector3.up * 1.5f;   // low, looking up over the machinery (he stands above its rim)
            Shoot(cam, WmHead - Vector3.up * 1.4f, 40f, ease);
        }
        void OverHisShoulder(float ease = 0.01f)
        {
            var toHer = Flat(P7 - VenusLayout.DaisAt).normalized; var r = Vector3.Cross(Vector3.up, toHer);
            Shoot(WmHead - toHer * 1.6f + r * 1.3f + Vector3.up * 0.7f, Head7, 38f, ease);
        }
        void LevelOn7(float ease = 0.01f)
        {
            var toHim = Flat(VenusLayout.DaisAt - P7).normalized; var r = Vector3.Cross(Vector3.up, toHim);
            Shoot(Head7 + toHim * 1.9f + r * 0.9f, Head7 - Vector3.up * 0.1f, 40f, ease);
        }
        void LevelOnHim(float ease = 0.01f)
        {
            // C07: level angles - the camera at his eye height from her side of the barrier, no looking up
            var toHim = Flat(VenusLayout.DaisAt - P7).normalized; var r = Vector3.Cross(Vector3.up, toHim);
            var eye = WmHead - Vector3.up * 0.3f;
            var cam = new Vector3(P7.x, eye.y, P7.z) + toHim * 2.5f + r * 1.4f; cam.y = eye.y;
            Shoot(cam, eye, 44f, ease);
        }

        IEnumerator C06()
        {
            yield return Cutscene(true);
            Seven.Lock(true);
            if (Dist7(VenusLayout.ControlEntry) > 6f) Seven.Place(VenusLayout.ControlEntry, Vector3.right);
            yield return WalkSeven(VenusLayout.ControlEntry + Vector3.right * 4.5f, 4f, 0.9f);
            Seven.Lock(true);
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(VenusLayout.DaisAt - P7)), 400f * Time.deltaTime); yield return null; }
            if (_machineHum == null) _machineHum = AudioKit.Loop("machinery_hum", null, VenusLayout.DaisAt + Vector3.up * 3f, 0.6f, AudioKit.Bus.Sfx, 4f, 60f);
            // the wide reveal from behind her, then low on him above his machinery
            Shoot(P7 + Vector3.up * 2.2f - Flat(VenusLayout.DaisAt - P7).normalized * 3.5f, WmHead - Vector3.up * 1.5f, 55f, 0.01f);
            yield return Wait(1.2f);
            yield return Shot("c06_01_wide_the_chamber");
            LowOnWormmaster();
            yield return Wait(0.6f);
            yield return Shot("c06_02_low_angle_above_his_machinery");
            yield return SayC6(1, "low");          // They sent someone down at last.
            yield return SayC6(2, "level");        // You're bringing their gardens down.
            yield return SayC6(3, "ots");          // They can leave.
            yield return SayC6(4, "level");        // It's their home.
            yield return SayC6(5, "low");          // Then they should have been more willing to negotiate.
            // he activates a signal pulse: the devices flare, the creatures surge along their troughs
            if (Wormmaster != null) Wormmaster.PulseGesture();
            AudioKit.Play("signal_pulse", VenusLayout.DaisAt, 1f, 0f, AudioKit.Bus.Sfx, 80f);
            Shoot(VenusLayout.ControlCentre + new Vector3(-12f, 7.5f, 18f), VenusLayout.ControlCentre + Vector3.up * 0.5f, 58f, 0.01f);
            yield return Wait(1.4f);
            yield return Shot("c06_03_he_activates_a_signal_pulse");
            yield return SayC6(6, "level");        // Turn it off.
            yield return SayC6(7, "lowpush");      // Come and do it yourself.
            yield return Shot("c06_04_come_and_do_it_yourself");
            Complete("VEN_C06", VenusState.Stage.EncounterActive);
            Objective(VenusState.Objectives[(int)VenusState.Stage.EncounterActive]);
            yield return Cutscene(false);
        }
        IEnumerator SayC6(int n, string shot)
        {
            if (shot == "low") LowOnWormmaster(); else if (shot == "lowpush") { LowOnWormmaster(); Cam.Shot(Cam.ShotPos + (WmHead - Cam.ShotPos) * 0.18f, WmHead - Vector3.up * 0.9f, 3f, false, 42f); }
            else if (shot == "ots") OverHisShoulder(); else if (shot == "level") LevelOn7(); else if (shot == "levelhim") LevelOnHim();
            _lineSerial++;
            var l = Lines(_c6Scene, n, n);
            if (Shots && l.Count > 0) StartCoroutine(SpeakerShot(l[0], _lineSerial));
            yield return Dlg.PlayLines(l, false);
        }
        string _c6Scene = "VEN_C06";
        IEnumerator SpeakerShot(VoiceBank.Line line, int serial)
        {
            yield return Wait(Auto ? 0.7f : 1.6f);
            if (serial != _lineSerial || !Dlg.Busy) yield break;           // measured only while the line is still playing
            var cam = Cam.GetComponent<Camera>();
            var head = line.Speaker == "wormmaster" ? WmHead : line.Speaker == "guardian" && Guardian != null ? HeadOf(Guardian) : Head7;
            var v = cam.WorldToViewportPoint(head);
            bool inFrame = v.z > 0f && v.x > 0.04f && v.x < 0.96f && v.y > 0.04f && v.y < 0.98f;
            float camVsHead = cam.transform.position.y - head.y;
            if (!inFrame) _coverageMisses++;
            _coverageShots++;
            Log($"coverage {line.Id} ({line.Speaker}): speaker head at ({v.x:0.00}, {v.y:0.00}) {(inFrame ? "in frame" : "OUT OF FRAME")}, camera {camVsHead:+0.0;-0.0} m vs his / her head ({(camVsHead < -0.4f ? "low angle" : Mathf.Abs(camVsHead) <= 0.4f ? "level" : "high")}), wormmaster tip x{(Wormmaster != null ? Wormmaster.MaxTipScale : 1f):0.00}");
            yield return Cap.Shot($"cov_{line.Id.Substring(4)}_{line.Speaker}");
        }

        // ------------------------------------------------------------------------------------------ the encounter (section 7)
        Vector3 _encSafe; bool _knocking, _tkCrystal, _consoleHit; int _deviceHit = -1;
        bool AllOff { get { foreach (var d in Devices) if (d != null && d.On) return false; return true; } }
        int OffCount { get { int n = 0; foreach (var d in Devices) if (d != null && !d.On) n++; return n; } }
        static float DistToSeg(Vector3 p, Vector3 a, Vector3 b) { var ab = Flat(b - a); float t = Mathf.Clamp01(Vector3.Dot(Flat(p - a), ab) / Mathf.Max(1e-4f, ab.sqrMagnitude)); return Flat(p - (a + ab * t)).magnitude; }
        float TroughDist(Vector3 p) { float d = 99f; for (int i = 0; i < 3; i++) d = Mathf.Min(d, DistToSeg(p, VenusLayout.Trough[i, 0], VenusLayout.Trough[i, 1])); return d; }

        IEnumerator Encounter()
        {
            Objective(VenusState.Objectives[(int)VenusState.Stage.EncounterActive]);
            if (_machineHum == null) _machineHum = AudioKit.Loop("machinery_hum", null, VenusLayout.DaisAt + Vector3.up * 3f, 0.6f, AudioKit.Bus.Sfx, 4f, 60f);
            _encSafe = P7.x > 103.4f ? P7 : VenusLayout.ControlEntry;
            foreach (var d in Devices) if (d != null) d.Pulsed += OnDevicePulse;
            yield return EncounterIntro();                               // QF12 (8): the boss is on screen first
            _encounterOn = true;
            StartCoroutine(EncounterWatch());
            StartCoroutine(Harass());                                    // Round 13 (0f): they try to stop her
            if (Auto) StartCoroutine(AutoEncounter());
            else StartCoroutine(TeachEncounter());
            while (!AllOff)
            {
                int i = NextDevice(); int n0 = OffCount;
                string[] hint = {
                    "The north device: wait for the dust and rumble in the trough to pass, cross, then X at the device",
                    "The device on the raised ledge (south-west): climb up the low wall (A), then X",
                    "The crystal device stands in the south trough: from the walkway beside it, look at its crystal and use telekinesis (RT)" };
                yield return Until(() => OffCount > n0, $"disable the control devices ({n0}/3)", 150f, hint[i], VenusLayout.DeviceUse[i], () => DisableDevice(i, true));
                if (n0 == 0 && ReloadCheck("first_device")) { _reloading = true; yield break; }      // check run: the save after a device
            }
            yield return Shot("enc_all_three_devices_off");
            // the creatures withdraw (their control is gone): alive, back into the burrows
            AudioKit.Play("creatures_withdraw", VenusLayout.ControlCentre, 0.9f, 0f, AudioKit.Bus.Sfx, 80f);
            foreach (var w in ChamberWorms) if (w != null && w.gameObject.activeSelf) w.Withdraw(NearestBurrow(w.transform.position));
            Objective("Close the containment barrier");
            if (ConsoleGlow != null) Glow(ConsoleGlow, 1.6f);
            _consoleHit = false;
            if (Auto) yield return AutoConsole();
            yield return Until(() => _consoleHit, "close the containment barrier (the console)", 90f, "The console in front of his dais: X closes the barrier", VenusLayout.ConsoleAt, () => _consoleHit = true);
            _encounterOn = false;
            foreach (var d in Devices) if (d != null) d.Pulsed -= OnDevicePulse;
            yield return CloseBarrier();
        }
        bool _encounterOn;
        /// <summary>The device the hint / skip is about: the nearest one still on.</summary>
        int NextDevice() { int best = 0; float bd = 1e9f; for (int i = 0; i < Devices.Length; i++) if (Devices[i] != null && Devices[i].On) { float d = Flat(VenusLayout.DeviceUse[i] - P7).magnitude; if (d < bd) { bd = d; best = i; } } return best; }
        Vector3 NearestBurrow(Vector3 p)
        {
            Vector3 best = p; float bd = 1e9f;
            for (int i = 0; i < 3; i++) foreach (var e in new[] { VenusLayout.Trough[i, 0], VenusLayout.Trough[i, 1] }) { float d = Flat(e - p).magnitude; if (d < bd) { bd = d; best = e; } }
            return best;
        }
        void OnDevicePulse(ControlDevice d)
        {
            // the signal drives its creatures: they surge along their trough (the link between signal and movement is seen)
            foreach (var w in ChamberWorms) if (w != null && w.gameObject.activeSelf && w.Group == d.Id) w.Drive(w.transform.position + w.transform.forward * 6f, 1.2f);
        }

        /// <summary>Disable device i (the step it took, or the player fallback's skip). Saves at once.</summary>
        void DisableDevice(int i, bool skipped = false)
        {
            var d = Devices[i]; if (d == null || !d.On) return;
            d.TurnOff();
            if (i == 2 && Crystal != null && skipped) Crystal.position = CrystalRest;
            VenusState.DisableDevice(d.Id);
            int gone = 0;
            foreach (var w in ChamberWorms) if (w != null && w.Group == d.Id && w.gameObject.activeSelf) { w.Withdraw(NearestBurrow(w.transform.position)); gone++; }
            Log($"device {d.Id} disabled{(skipped ? " (skipped: done for her)" : "")}: {gone} creatures of its trough withdraw (alive); saved - devices [{string.Join(", ", VenusState.DevicesDisabled)}] ({OffCount}/3)");
            StartCoroutine(Shot($"enc_{d.Id}_off"));
        }

        /// <summary>Prompts + actions in the chamber, and the creatures' contact: knocked back (white fade, back on the
        /// walkway). Nobody is hurt; the creatures are never harmed.</summary>
        IEnumerator EncounterWatch()
        {
            while (_encounterOn)
            {
                yield return null;
                if (_cut || PauseMenu.IsOpen || _knocking) continue;
                bool free = Seven.Current == ChapterSeven.Mode.Free;
                if (free && TroughDist(P7) > 2.6f && P7.y > VenusLayout.ControlY - 0.2f) _encSafe = P7;
                // contact with a surfaced creature
                foreach (var w in ChamberWorms)
                    if (w != null && w.gameObject.activeSelf && w.Surfaced && Flat(w.transform.position - P7).magnitude < 1.7f && Mathf.Abs(P7.y - VenusLayout.ControlY) < 0.6f)
                    { StartCoroutine(KnockBack(w)); break; }
                // the devices
                _deviceHit = -1; string prompt = null; ControlsLegend.Btn btn = ControlsLegend.Btn.Interact;
                for (int i = 0; i < 3 && free; i++)
                {
                    var d = Devices[i]; if (d == null || !d.On) continue;
                    if (i < 2 && Flat(VenusLayout.DeviceUse[i] - P7).magnitude < 1.7f && Mathf.Abs(P7.y - VenusLayout.DeviceUse[i].y) < 0.4f) { prompt = "Shut it down"; _deviceHit = i; }
                    if (i == 2 && Flat(VenusLayout.DeviceAt[2] - P7).magnitude < 10f && TroughDist(P7) > 1.2f && CrystalInView()) { prompt = "Telekinesis: the signal crystal"; btn = ControlsLegend.Btn.Tk; _deviceHit = 2; }
                }
                if (AllOff && free && !_consoleHit && Flat(VenusLayout.ConsoleAt - P7).magnitude < 2.0f) { prompt = "Close the barrier"; btn = ControlsLegend.Btn.Interact; _deviceHit = 9; }
                // Round 13 (0f): a shielded device waits; a creature coming at her can be pushed aside with telekinesis
                if (_deviceHit >= 0 && _deviceHit < 3 && Shielded(_deviceHit)) { prompt = "Shielded by his signal - wait"; _deviceHit = -1; }
                var stunT = free && _deviceHit != 2 ? StunTarget() : null;
                if (stunT != null && prompt == null) { prompt = "Telekinesis: push it aside"; btn = ControlsLegend.Btn.Tk; }
                if (stunT != null && free && (Auto ? stunT.Lunging && Flat(stunT.transform.position - P7).magnitude < 6f : Input.Telekinesis.WasPressedThisFrame())) { _encPrompt = null; yield return StunWorm(stunT); continue; }
                _encPrompt = prompt; _encBtn = btn;
                if (Auto || _deviceHit < 0) continue;
                if (_deviceHit == 9 && Input.Interact.WasPressedThisFrame()) _consoleHit = true;
                else if (_deviceHit < 2 && _deviceHit >= 0 && Input.Interact.WasPressedThisFrame()) yield return UseDevice(_deviceHit);
                else if (_deviceHit == 2 && Input.Telekinesis.WasPressedThisFrame()) yield return PullCrystal();
            }
            _encPrompt = null;
        }
        string _encPrompt; ControlsLegend.Btn _encBtn;
        // ------------------------------------------------------------------------------------------ Round 13 (0f): they try to stop her
        // Dan: "the encounter is too easy - they should be trying to stop her". A fair first boss, still nonlethal:
        //  - the Wormmaster fires a signal pulse every few seconds (faster as devices fall) that sends the nearest creature
        //    lunging at her (surfaced, fast, telegraphed by dust + rumble); contact knocks her back (the existing retry);
        //  - the same pulse shields the device she is nearest for a few seconds ("shielded - wait") and pulls that device's
        //    creatures across her path (he shifts their routes);
        //  - he taunts (four short barks);
        //  - her telekinesis (RT) at a surfaced creature pushes it aside and stuns it: it sinks and lies still, then recovers
        //    and goes back to its trough (never harmed). Only when all three devices are off do they withdraw.
        //  - fairness: after three knockbacks the pulses pause for 20 s; every wait keeps its hint (45 s) and skip (2 min).
        static readonly (string id, string text)[] Taunts =
        {
            ("VEN_ENC_T1", "They answer to me. Not to you."), ("VEN_ENC_T2", "You cannot reach them all."),
            ("VEN_ENC_T3", "Turn back, little traveller."), ("VEN_ENC_T4", "Another one? I can wait."),
        };
        readonly float[] _shieldUntil = new float[3];
        float _harassPause; int _knocksSincePause, _pulses, _lunges, _stuns, _taunts, _shields;
        bool Shielded(int i) => Time.time < _shieldUntil[i];

        IEnumerator Harass()
        {
            int ti = 1; float nextTaunt = Time.time + 9f;
            yield return Wait(1.5f);
            while (_encounterOn && !AllOff)
            {
                float gap = Mathf.Lerp(6f, 4f, OffCount / 2f) * (Auto ? 1.5f : 1f);
                yield return Wait(gap);
                if (!_encounterOn || AllOff) break;
                if (_cut || PauseMenu.IsOpen || _knocking || Time.time < _harassPause) continue;
                if (_knocksSincePause >= 3) { _knocksSincePause = 0; _harassPause = Time.time + 20f; Log("encounter (0f): three knockbacks - the pulses pause for 20 s (fairness)"); continue; }
                // the signal pulse from his head tip
                _pulses++;
                if (Wormmaster != null) Wormmaster.PulseGesture();
                AudioKit.Play("signal_pulse", WmHead, 1f, 0.05f, AudioKit.Bus.Sfx, 90f);
                // the nearest creature still under his control lunges at her
                WormCreature best = null; float bd = 34f;
                foreach (var w in ChamberWorms)
                {
                    if (w == null || !w.gameObject.activeSelf || w.Withdrawn || w.Stunned || w.Calm) continue;
                    var dv = Devices != null ? System.Array.Find(Devices, x => x != null && x.Id == w.Group) : null;
                    if (dv != null && !dv.On) continue;
                    float d = Flat(w.transform.position - P7).magnitude; if (d < bd) { bd = d; best = w; }
                }
                if (best != null && Mathf.Abs(P7.y - VenusLayout.ControlY) < 1.5f)
                {
                    if (bd > 10f) { BringNear(best); bd = Flat(best.transform.position - P7).magnitude; }
                    var lead = P7 + Flat(Seven.transform.forward) * 1.2f;
                    best.Lunge(lead, 3.2f); _lunges++;
                    if (Shots && Auto && _lunges <= 2) StartCoroutine(LungeShot(best, _lunges));
                    if (Shots && !Auto) StartCoroutine(PlayerViewLunge(best, _lunges));
                    // QF12 (8): the player must SEE it come: the camera frames her and the creature for the lunge (a short
                    // assist, then the follow camera again), and a white light rides with the creature (the chamber is dark)
                    if (!_cut && !_knocking) StartCoroutine(TurnToThreat(best));
                    best.Highlight(3.2f);
                    Log($"encounter (0f): pulse {_pulses} - {best.name} sent lunging at her ({bd:0.0} m away)");
                }
                // he shields the device she is nearest and pulls its creatures across her path
                int nd = NextDevice();
                if (Devices[nd] != null && Devices[nd].On && Flat(VenusLayout.DeviceUse[nd] - P7).magnitude < 14f)
                {
                    _shieldUntil[nd] = Time.time + 3.5f; _shields++;
                    AudioKit.Play("barrier_close", VenusLayout.DeviceAt[nd] + Vector3.up, 0.35f, 0.1f, AudioKit.Bus.Sfx, 30f);
                    var mid = Vector3.Lerp(P7, VenusLayout.DeviceUse[nd], 0.5f);
                    foreach (var w in ChamberWorms) if (w != null && w.gameObject.activeSelf && w.Group == Devices[nd].Id && w != best) w.Drive(mid, 2.2f);
                }
                // a taunt now and then
                if (Time.time > nextTaunt && !Dlg.Busy)
                {
                    var (id, text) = Taunts[ti++ % Taunts.Length]; _taunts++;
                    nextTaunt = Time.time + 15f;
                    StartCoroutine(Dlg.PlayLines(new List<VoiceBank.Line> { new VoiceBank.Line { Id = id, Speaker = "wormmaster", Text = text, Scene = 200, PauseAfter = 0.2f, Clip = Resources.Load<AudioClip>("VenusVoice/" + id), Label = "Wormmaster" } }, false));
                }
            }
            Log($"ENCOUNTER (0f): pulses {_pulses}, lunges {_lunges}, device shields {_shields}, taunts {_taunts}, stuns by telekinesis {_stuns}, knockbacks {Knockdowns}");
        }

        /// <summary>QF12 (8): Dan saw no boss and no creatures in a normal play. The encounter now opens on him: the camera
        /// finds the Wormmaster on his dais, his head tip flares, the signal pulse rolls out and every creature still under
        /// his control breaks the surface of its trough at once; he taunts her. Then the follow camera, and the fight.</summary>
        IEnumerator EncounterIntro()
        {
            yield return Cutscene(true);
            Seven.Lock(true);
            var toWm = Flat(VenusLayout.DaisAt - P7).normalized; var r = Vector3.Cross(Vector3.up, toWm);
            Shoot(P7 - toWm * 3.2f + r * 1.6f + Vector3.up * 2.4f, WmHead - Vector3.up * 1.2f, 50f, 0.01f);
            yield return Wait(0.6f);
            if (Wormmaster != null) Wormmaster.PulseGesture();
            AudioKit.Play("signal_pulse", WmHead, 1f, 0f, AudioKit.Bus.Sfx, 120f);
            int up = 0;
            foreach (var w in ChamberWorms)
                if (w != null && w.gameObject.activeSelf && !w.Withdrawn && !VenusState.IsDisabled(w.Group)) { w.Lunge(w.transform.position + w.transform.forward * 6f, 2.4f); up++; }
            StartCoroutine(Dlg.PlayLines(new List<VoiceBank.Line> { new VoiceBank.Line { Id = "VEN_ENC_T1", Speaker = "wormmaster", Text = "They answer to me. Not to you.", Scene = 200, PauseAfter = 0.2f, Clip = Resources.Load<AudioClip>("VenusVoice/VEN_ENC_T1"), Label = "Wormmaster" } }, false));
            // the wide view: the dais, him, and the troughs with the creatures breaking the surface
            yield return Wait(0.9f);
            Shoot(P7 + toWm * 2.5f + r * 3f + Vector3.up * 5.5f, Vector3.Lerp(P7, VenusLayout.DaisAt, 0.6f), 58f, 1.2f);
            yield return Wait(1.3f);
            yield return Shot("enc_00_intro_he_sends_them");
            int seen = 0; var camC = Cam.GetComponent<Camera>();
            foreach (var w in ChamberWorms) if (w != null && w.gameObject.activeSelf) { var v = camC.WorldToViewportPoint(w.transform.position + Vector3.up * 0.3f); if (v.z > 0f && v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f) seen++; }
            Log($"encounter intro (QF12 8): {seen} creatures in the frame; the Wormmaster on screen ({(Wormmaster != null && Wormmaster.gameObject.activeInHierarchy ? "visible" : "MISSING")}), his pulse, {up} creatures surfaced at once, taunt");
            yield return Wait(1.0f);
            FollowCamera(); Cam.SnapBehind();
            Seven.Release();
            yield return Cutscene(false);
        }

        /// <summary>QF12 (8): a creature far from her travels under the floor along its trough to the point nearest her
        /// (unseen, below the surface) before it lunges - every lunge comes at her from close by, in front of her.</summary>
        void BringNear(WormCreature w)
        {
            if (w == null || w.Surfaced || w.Route == null || w.Route.Length < 2) return;
            Vector3 a = w.Route[0], b = w.Route[w.Route.Length - 1];
            var ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(P7 - a, ab) / Mathf.Max(1e-3f, ab.sqrMagnitude));
            var near = a + ab * t; var dir = ab.normalized;
            // start 7 m along the trough, on the side she is facing away from less (so it comes into view)
            var s1 = near - dir * 7f; var s2 = near + dir * 7f;
            var f = Flat(Cam.transform.forward);                      // QF12: from where the player is looking
            var start = Vector3.Dot(Flat(s1 - P7), f) > Vector3.Dot(Flat(s2 - P7), f) ? s1 : s2;
            float gy = float.IsNaN(w.BaseY) ? VenusLayout.ControlY : w.BaseY;
            w.transform.position = new Vector3(start.x, gy - w.Depth, start.z);
            w.transform.rotation = Quaternion.LookRotation(Flat(near - start).normalized);
        }

        /// <summary>QF12 (8): the follow camera swings (0.6 s) to look past her at the creature coming at her - the player
        /// sees the threat and has time to push it aside (RT) or step out of its way.</summary>
        IEnumerator TurnToThreat(WormCreature w)
        {
            float y0 = Cam.Yaw;
            for (float t = 0f; t < 1f && w != null && !_cut && !_knocking; t += Time.deltaTime / 0.6f)
            {
                var d = Flat(Vector3.Lerp(w.transform.position, P7, 0.25f) - P7); if (d.sqrMagnitude < 0.25f) yield break;
                Cam.Yaw = Mathf.LerpAngle(y0, Quaternion.LookRotation(d).eulerAngles.y, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
        }

        /// <summary>QF12 (8): what the player sees - the follow camera untouched: is the lunging creature in the frame, and
        /// the Wormmaster? Logged per lunge; the first three are captured.</summary>
        IEnumerator PlayerViewLunge(WormCreature w, int k)
        {
            yield return Wait(1.0f);
            if (w == null) yield break;
            var cam = Cam.GetComponent<Camera>();
            bool In(Vector3 p) { var v = cam.WorldToViewportPoint(p); return v.z > 0f && v.x > 0.02f && v.x < 0.98f && v.y > 0.02f && v.y < 0.98f; }
            bool wIn = In(w.transform.position + Vector3.up * 0.3f), mIn = In(Wormmaster != null ? Wormmaster.transform.position + Vector3.up * 1.6f : WmHead);
            Log($"lunge {k} (player view, QF12 8): {w.name} surfaced {w.Surfaced}, {Flat(w.transform.position - P7).magnitude:0.0} m from her, in the frame {(wIn ? "yes" : "NO")}; the Wormmaster in the frame {(mIn ? "yes" : "no")}");
            if (k <= 3 && Cap != null) yield return Cap.Shot($"enc_player_view_lunge_{k}");
        }

        IEnumerator LungeShot(WormCreature w, int k)
        {
            yield return Wait(1.1f);
            if (w == null || _knocking) yield break;
            var mid = (w.transform.position + P7) * 0.5f; var side = Vector3.Cross(Vector3.up, Flat(w.transform.position - P7).normalized);
            Shoot(mid + side * 7f + Vector3.up * 3.5f, mid + Vector3.up * 0.6f, 52f, 0.01f);
            yield return Wait(0.15f);
            yield return Shot($"enc_lunge_{k}_creature_comes_at_her");
            Log($"lunge {k}: {w.name} surfaced {w.Surfaced}, {Flat(w.transform.position - P7).magnitude:0.0} m from her");
            FollowCamera();
        }

        /// <summary>RT at a surfaced creature within 10 m (roughly ahead or close): push it aside and stun it.</summary>
        WormCreature StunTarget()
        {
            WormCreature best = null; float bd = 10f;
            foreach (var w in ChamberWorms)
            {
                if (w == null || !w.gameObject.activeSelf || w.Withdrawn || w.Stunned || !(w.Surfaced || w.Lunging)) continue;
                var to = Flat(w.transform.position - P7); float d = to.magnitude;
                if (d > bd) continue;
                if (d > 4f && Vector3.Angle(Flat(Cam.transform.forward), to) > 70f && Vector3.Angle(Flat(Seven.transform.forward), to) > 70f) continue;
                bd = d; best = w;
            }
            return best;
        }
        IEnumerator StunWorm(WormCreature w)
        {
            Seven.Gesture(w.transform.position + Vector3.up * 0.6f);
            Seven.SetEnergyPoint(1.6f, 0.2f);
            for (float t = 0f; t < 1.2f && !Seven.GesturePeak; t += Time.deltaTime) yield return null;
            AudioKit.Play("tk_push", w.transform.position, 0.9f);
            w.Stun(6f, w.transform.position - P7); _stuns++;
            Log($"encounter (0f): telekinesis pushed {w.name} aside - stunned for 6 s (it recovers; never harmed)");
            Seven.SetEnergyPoint(1f, 0.8f);
            yield return Wait(0.3f);
            Seven.Release();
        }

        // ---- Round 13 (0f): on the way down, one creature in the root chamber is driven at her too
        WormCreature _tunnelWorm;
        IEnumerator WayDownHarass()
        {
            if (ChamberWorms == null || ChamberWorms.Length == 0 || ChamberWorms[0] == null) yield break;
            var go = Instantiate(ChamberWorms[0].gameObject, new Vector3(74f, -24f, 89f), Quaternion.LookRotation(Vector3.right));
            go.name = "TunnelWorm"; go.SetActive(true);
            _tunnelWorm = go.GetComponent<WormCreature>();
            _tunnelWorm.Group = null; _tunnelWorm.BaseY = -22f; _tunnelWorm.PingPong = true; _tunnelWorm.Telegraph = true;
            _tunnelWorm.Route = new[] { new Vector3(71.5f, -22f, 89f), new Vector3(80.5f, -22f, 89f) };
            _tunnelWorm.transform.localScale = Vector3.one * 0.7f;
            float nextLunge = 0f; int lunges = 0, knocks = 0;
            while (!VenusState.SceneDone("VEN_C06") && !(P7.x > 104.5f && P7.y < -28f))
            {
                yield return null;
                if (_cut || PauseMenu.IsOpen || _knocking) continue;
                float d = Flat(_tunnelWorm.transform.position - P7).magnitude;
                bool inChamber = Mathf.Abs(P7.y + 22f) < 1.5f && P7.x > 68f && P7.x < 84f;
                if (Seven.Current == ChapterSeven.Mode.Free && d > 4f) _encSafe = P7;
                if (inChamber && d < 14f && Time.time > nextLunge) { _tunnelWorm.Lunge(P7, 2.4f); lunges++; nextLunge = Time.time + (Auto ? 12f : 7f); if (Shots && lunges == 1) StartCoroutine(LungeShot(_tunnelWorm, 0)); AudioKit.Play("signal_pulse", P7 + Vector3.up * 6f, 0.5f); }
                if (_tunnelWorm.Surfaced && !_tunnelWorm.Stunned && d < 1.2f && inChamber && !Auto) { knocks++; yield return KnockBack(_tunnelWorm); }
                if (!Auto && Input.Telekinesis.WasPressedThisFrame() && (_tunnelWorm.Surfaced || _tunnelWorm.Lunging) && d < 10f && Seven.Current == ChapterSeven.Mode.Free) yield return StunTunnelWorm();
                if (Auto && _tunnelWorm.Lunging && d < 6f && Seven.Current == ChapterSeven.Mode.Free) yield return StunTunnelWorm();
            }
            Log($"way down (0f): the root-chamber creature lunged at her {lunges}x, knocked her back {knocks}x, stunned {_tunnelWorm.Stuns}x; it stays behind (alive)");
        }
        IEnumerator StunTunnelWorm()
        {
            Seven.Gesture(_tunnelWorm.transform.position + Vector3.up * 0.5f);
            for (float t = 0f; t < 1.2f && !Seven.GesturePeak; t += Time.deltaTime) yield return null;
            AudioKit.Play("tk_push", _tunnelWorm.transform.position, 0.9f);
            _tunnelWorm.Stun(6f, _tunnelWorm.transform.position - P7);
            yield return Wait(0.3f);
            Seven.Release();
        }

        bool CrystalInView()
        {
            var to = Flat(VenusLayout.DeviceAt[2] - P7);
            // generous: anywhere on the walkway beside it, roughly facing it (she turns to it as she gestures)
            return to.magnitude < 4.5f || Vector3.Angle(Flat(Cam.transform.forward), to) < 75f || Vector3.Angle(Flat(Seven.transform.forward), to) < 75f;
        }

        IEnumerator UseDevice(int i)
        {
            Seven.Touch(VenusLayout.DeviceAt[i] + Vector3.up * 1.5f);
            for (float t = 0f; t < 1.5f && Seven.Current == ChapterSeven.Mode.Touch && Seven.Tn < 0.5f; t += Time.deltaTime) yield return null;
            DisableDevice(i);
            for (float t = 0f; t < 1.2f && Seven.Current == ChapterSeven.Mode.Touch; t += Time.deltaTime) yield return null;
            Seven.Release();
        }

        IEnumerator PullCrystal()
        {
            if (Crystal == null) { DisableDevice(2); yield break; }
            Seven.Gesture(Crystal.position);
            Seven.SetEnergyPoint(1.8f, 0.25f);
            for (float t = 0f; t < 2.5f && !Seven.GesturePeak; t += Time.deltaTime) yield return null;
            AudioKit.Play("tk_push", Crystal.position, 0.8f);
            var p0 = Crystal.position; var p1 = CrystalRest;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.3f) { float u = Mathf.SmoothStep(0f, 1f, t); Crystal.position = Vector3.Lerp(p0, p1, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 1.4f; Crystal.Rotate(0f, 220f * Time.deltaTime, 0f); yield return null; }
            Crystal.position = p1;
            AudioKit.Play("stone_fall", p1, 0.4f);
            DisableDevice(2);
            Seven.SetEnergyPoint(1f, 1f);
            yield return Wait(0.5f);
            Seven.Release();
        }

        IEnumerator KnockBack(WormCreature w)
        {
            _knocking = true; Knockdowns++; _knocksSincePause++;
            Seven.Lock(true);
            var away = Flat(P7 - w.transform.position).normalized; if (away.sqrMagnitude < 0.5f) away = -Seven.transform.forward;
            AudioKit.Play("body_fall", P7, 0.7f);
            var a = P7; var b = Ground.ClampToArena(P7 + away * 1.6f, 0.3f); b.y = Ground.Height(b.x, b.z);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.3f) { Seven.transform.position = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            Log($"KNOCKED BACK by a creature ({w.name}) at {a:0.0} -> white fade, back on the walkway at {_encSafe:0.0} (nobody hurt; knockdowns {Knockdowns})");
            yield return UI.Fade(1f, 0.4f, 1f);
            Seven.Place(_encSafe, Flat(VenusLayout.ControlCentre - _encSafe));
            Cam.SnapBehind();
            yield return Wait(0.3f);
            yield return UI.Fade(0f, 0.6f, 1f);
            Seven.Lock(false);
            _knocking = false;
        }

        IEnumerator TeachEncounter()
        {
            if (_taught.Contains("enc")) yield break;
            _taught.Add("enc");
            UI.Hint("The creatures follow the troughs - dust and a rumble come before one surfaces. Cross when it has passed.", 6f);
            yield break;
        }

        IEnumerator CloseBarrier()
        {
            yield return Cutscene(true);
            Seven.Lock(true);
            if (Flat(VenusLayout.ConsoleAt - P7).magnitude > 3f) Seven.Place(VenusLayout.ConsoleAt - Vector3.right * 1.2f, Vector3.right);
            Seven.Touch(VenusLayout.ConsoleAt + Vector3.up * 1.1f);
            if (ConsoleGlow != null) Glow(ConsoleGlow, 3f);
            yield return Wait(0.5f);
            AudioKit.Play("barrier_close", VenusLayout.DaisAt, 1f, 0f, AudioKit.Bus.Sfx, 80f);
            Shoot(VenusLayout.ConsoleAt + new Vector3(-6f, 3.2f, 7.5f), VenusLayout.DaisAt + Vector3.up * 2.5f, 54f, 0.01f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 3.6f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                if (Barrier != null) Barrier.position = BarrierPos(u);
                if (BarrierGlow != null) Glow(BarrierGlow, 1.4f * u);
                if (t > 0.6f && t - Time.deltaTime / 3.6f <= 0.6f) StartCoroutine(Shot("enc_barrier_closing"));
                yield return null;
            }
            if (Wormmaster != null) Wormmaster.Current = WormmasterRig.Pose.Contained;
            VenusState.WormmasterContained = true;                 // saved: containment
            Log($"containment: the barrier closed round the dais - Wormmaster contained (saved); devices [{string.Join(", ", VenusState.DevicesDisabled)}]; knockdowns during the encounter {Knockdowns}; creatures harmed 0");
            Seven.Release();
        }

        // ------------------------------------------------------------------------------------------ C07
        IEnumerator C07()
        {
            yield return Cutscene(true);
            Seven.Lock(true);
            if (Wormmaster != null) Wormmaster.Current = WormmasterRig.Pose.Contained;
            if (Barrier != null) Barrier.position = BarrierPos(1f);
            // level angles: his dominance gone (no low angles any more)
            var stand = VenusLayout.DaisAt - Vector3.right * (VenusLayout.BarrierRadius + 2.6f) + Vector3.forward * 2.8f;
            if (Flat(stand - P7).magnitude > 1f) yield return WalkSeven(stand, 5f);
            Seven.Lock(true);
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(VenusLayout.DaisAt - P7)), 400f * Time.deltaTime); yield return null; }
            _c6Scene = "VEN_C07";
            yield return SayC6(1, "levelhim");     // You don't know what you're interfering with.
            yield return SayC6(2, "level");        // I know what was standing above it.
            _c6Scene = "VEN_C06";
            // the creatures withdraw safely; the machinery falls silent
            foreach (var w in ChamberWorms) if (w != null && w.gameObject.activeSelf) w.Withdraw(NearestBurrow(w.transform.position));
            if (_machineHum != null) StartCoroutine(AudioKit.Fade(_machineHum, 0f, 2.5f, true));
            _machineHum = null;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2f) { if (UnderLights != null) foreach (var l in UnderLights) if (l != null) l.intensity = Mathf.Lerp(2.2f, 0.6f, t); yield return null; }
            Shoot(VenusLayout.ControlCentre + new Vector3(-10f, 1.7f, 9f), VenusLayout.DaisAt + Vector3.up * 2.2f, 50f, 0.01f);
            yield return Wait(1.2f);
            yield return Shot("c07_level_wide_machinery_silent");
            Complete("VEN_C07", VenusState.Stage.ThreatStopped);
            ApplyThreatStopped(true);
            Objective(VenusState.Objectives[(int)VenusState.Stage.ThreatStopped]);
            // she makes her way back up to the bike (a cut: the tunnels are the same way back, and still open)
            yield return UI.Fade(1f, 1.0f);
            var bikeAt = VenusState.BikeKnown ? VenusState.BikeAt : VenusLayout.ObsPark;
            Seven.Place(bikeAt - Vector3.right * 1.4f + Vector3.forward * 1.5f, Vector3.forward);
            FollowCamera(); Cam.SnapBehind();
            yield return Wait(0.4f);
            yield return UI.Fade(0f, 1.2f);
            yield return Cutscene(false);
        }

        // ------------------------------------------------------------------------------------------ B06 (the return ride)
        IEnumerator B06()
        {
            Objective(VenusState.Objectives[(int)VenusState.Stage.ThreatStopped]);
            if (Auto) yield return AutoMount();
            yield return Until(() => Bike.Riding, "back on the bike (B06)", 60f, "Your bike is parked at the lookout: X to ride", Bike.transform.position, () => { if (!Bike.Riding) { Seven.Place(Bike.transform.position - Bike.transform.right * 1.3f, Bike.transform.forward); Bike.Mount(); FollowCamera(); } });
            if (Auto) StartCoroutine(AutoRide(new[] { new Vector3(76.5f, -6f, 152f), new Vector3(76.5f, -3f, 175f), new Vector3(76f, 0f, 199f), new Vector3(46f, 0f, 199f), new Vector3(40f, 0f, 199f) }, () => P7.x < 44f));
            yield return Wait(2.5f);
            yield return SayInGameplay("VEN_B06", 1, 4);      // How's the garden? / Still standing. / And its unwelcome guest? / Staying put.
            yield return Shot("b06_return_ride_calm_basin");
            // she passes a creature moving calmly beneath the roots
            yield return Until(() => NearCalmCreature() < 30f || P7.z > 180f || P7.x < 60f, "a calm creature beside the path", 40f);
            yield return SayInGameplay("VEN_B06", 5, 5);      // The others are going home.
            yield return Until(() => P7.x < 46f && P7.z > 190f, "the temple court (B06)", 120f, "Ride back up the maintenance path to the temple court", VenusLayout.ParkSpot, () => PlaceRiding(VenusLayout.ParkSpot + Vector3.right * 4f, 270f));
            _autoRideStop = true; yield return null; _autoRideStop = false;
            if (Bike.Riding) { Bike.ScriptThrottle = -1f; for (float t = 0f; t < 2f && Bike.Speed > 0.5f; t += Time.deltaTime) yield return null; Bike.ScriptThrottle = 0f; Bike.ScriptDriven = false; Bike.Dismount(true); FollowCamera(); }
            if (Bike.Riding) Bike.Dismount(true);
            Bike.Place(VenusLayout.ParkSpot, 180f); VenusState.SaveBike(Bike.transform.position, Bike.transform.eulerAngles.y, false);
            Log($"B06: parked at the temple court, on foot: {!Bike.Riding}");
            Seven.Place(VenusLayout.ParkSpot - Vector3.right * 1.7f + Vector3.forward * 1f, Vector3.forward);
            if (Guardian != null) { Guardian.Stop(); Guardian.transform.position = VenusLayout.MeetSpot + Vector3.forward * 1.6f; Guardian.Face(P7); }
            yield return Shot("b06_parked_at_the_temple_workers_back");
            Complete("VEN_B06");
        }
        float NearCalmCreature() { float d = 1e9f; if (Worms != null) foreach (var w in Worms) if (w != null) d = Mathf.Min(d, Flat(w.transform.position - P7).magnitude); return d; }

        // ------------------------------------------------------------------------------------------ C08
        bool _talkHit;
        IEnumerator C08()
        {
            Objective("Speak with the guardian");
            _talkHit = false;
            if (Auto) { yield return AutoWalkPath(new[] { VenusLayout.MeetSpot - Vector3.forward * 1.4f }); _talkHit = true; }
            _talkPrompt = "Speak with the guardian";
            yield return Until(() => _talkHit, "speak with the guardian (C08)", 90f, "The guardian waits in the temple garden: walk up and X", VenusLayout.MeetSpot, () => _talkHit = true);
            _talkPrompt = null;
            yield return Cutscene(true);
            Seven.Lock(true);
            _coverage = true; _lastSpeaker = null;
            if (Guardian != null) { Guardian.Stop(); Guardian.Face(P7); }
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { if (Guardian != null) Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(Guardian.transform.position - P7)), 400f * Time.deltaTime); yield return null; }
            yield return Say("VEN_C08", 1, 4);
            yield return Shot("c08_you_came_with_a_question");
            _coverage = false;
            if (Guardian != null) Guardian.WalkTo(VenusLayout.CubeView + new Vector3(1.8f, 0f, 2f));
            Complete("VEN_C08");
            Objective("Follow the guardian to the cube");
            yield return Cutscene(false);
        }
        string _talkPrompt;

        // ------------------------------------------------------------------------------------------ C09 + C10
        bool _cubeHit;
        IEnumerator C09C10()
        {
            Objective("Follow the guardian to the cube");
            if (Auto) yield return AutoWalkPath(new[] { new Vector3(30f, 0f, 230f), new Vector3(30f, 0f, 248f), VenusLayout.CubeView });
            yield return Until(() => P7.z > VenusLayout.CubeRoomZ, "the cube chamber (C09)", 90f, "Into the temple, to the cube chamber", new Vector3(30f, 0f, VenusLayout.CubeRoomZ), () => Seven.Place(VenusLayout.CubeView, Vector3.forward));
            if (Guardian != null) { Guardian.Stop(); Guardian.transform.position = VenusLayout.CubeView + new Vector3(1.8f, 0f, 2f); Guardian.Face(P7); }
            _cubeHit = false;
            if (Auto) { yield return AutoWalkPath(new[] { CubeTouchSpot }); _cubeHit = true; }
            _talkPrompt = "Touch the record";
            yield return Until(() => _cubeHit, "touch the record (C09)", 90f, "Walk up to the cube in its protected structure and X", CubeTouchSpot, () => _cubeHit = true);
            _talkPrompt = null;
            yield return Cutscene(true);
            Seven.Lock(true);
            if (Flat(CubeTouchSpot - P7).magnitude > 0.6f) yield return WalkSeven(CubeTouchSpot, 3f);
            Seven.Lock(true);
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(VenusLayout.CubeAt - P7)), 400f * Time.deltaTime); yield return null; }
            // over her shoulder toward the cube; her hand approaches and touches the protected structure
            Cue("over 7's shoulder toward cube");
            var ik = Seven.gameObject.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
            ik.enabled = true; ik.Alive = false; ik.Ease = 2.2f;
            var touch = VenusLayout.CubeAt + Flat(P7 - VenusLayout.CubeAt).normalized * 1.3f + Vector3.down * 0.05f;
            ik.RightPos = touch; ik.RightW = 1f; ik.LookPos = VenusLayout.CubeAt; ik.LookW = 0.9f;
            yield return Wait(1.0f);
            yield return Shot("c09_01_over_her_shoulder_toward_cube"); LogCube("C09");
            { var hb = Seven.Bone(HumanBodyBones.RightHand); var h = hb != null ? Vector3.Lerp(hb.position, touch, 0.4f) : touch; var side = Vector3.Cross(Vector3.up, Flat(VenusLayout.CubeAt - P7).normalized); Shoot(h + side * 1.0f + Vector3.up * 0.3f + Flat(VenusLayout.CubeAt - P7).normalized * 0.2f, h, 34f, 0.01f); }
            yield return Wait(0.8f);
            yield return Shot("c09_02_hand_touches");
            AudioKit.Play("energy_point", VenusLayout.CubeAt, 0.7f);
            // the vision: full screen (skippable; a still if the video can't play). Under it her hand comes back and opens,
            // palm up in front of her at chest height (ready for the match cut)
            var fw = Flat(Seven.transform.forward).normalized;
            StartCoroutine(PalmUpLater(ik, P7 + Vector3.up * 1.22f + fw * 0.42f + Seven.transform.right * 0.06f, 1.2f));
            yield return Vision2Play();
            // match cut: the vision's offered palm -> her empty palm (framed on the hand alone, from above and in front -
            // the floor beyond it, never her body)
            var palm = Seven.Bone(HumanBodyBones.RightHand) != null ? Seven.Bone(HumanBodyBones.RightHand).position : ik.RightPos;
            // from straight above the open hand, tight: only her hand and the floor below it (never her body)
            Shoot(palm + Vector3.up * 0.62f + fw * 0.12f, palm + fw * 0.02f, 24f, 0.01f);
            UI.SetFade(0f);
            yield return null;
            yield return Shot("c09_03_match_cut_her_empty_palm");
            Log("C09 match cut: the vision's offered palm (its last frame) -> a close shot of 7's empty palm, cut straight (no fade)");
            yield return Wait(1.2f);
            // eye close-up, gaze lowered
            { var hd = Head7; Shoot(hd + fw * 0.85f + Seven.transform.right * 0.22f - Vector3.up * 0.06f, hd + Vector3.up * 0.02f, 30f, 0.01f); }
            ik.LookPos = P7 + fw * 0.9f + Vector3.up * 0.5f; ik.RightW = 0f;          // eye close-up, gaze lowered
            yield return Wait(1.0f);
            yield return Shot("c09_04_eye_close_up_gaze_lowered");
            ik.RightW = 0f;
            _coverage = true; _lastSpeaker = null;
            var probe = Shots ? BodyProbe.Begin(Seven.gameObject, "Venus, after the second vision (C09 1-5)") : null;
            yield return Say("VEN_C09", 1, 4);
            // the unanswered question: a medium two-shot; hold briefly (no frightened reaction)
            if (Guardian != null) TwoShot(Guardian, _side, 5.6f);
            yield return Say("VEN_C09", 5, 5, false);
            yield return Wait(1.6f);
            yield return Shot("c09_05_two_shot_i_think_i_knew_them");
            if (probe != null) Log(probe.Report());
            Complete("VEN_C09");
            // C10: the guardian offers the small cube; the original stays in its protected structure
            if (Guardian != null && RecordCopy != null)
            {
                Guardian.Face(P7); Guardian.Play(Guardian.HasState("point") ? "point" : "lift");
                RecordCopy.gameObject.SetActive(true);
                var hand = Guardian.Anim != null && Guardian.Anim.isHuman ? Guardian.Anim.GetBoneTransform(HumanBodyBones.RightHand) : null;
                StartCoroutine(HoldRecordIn(() => hand != null ? hand.position + Vector3.up * 0.05f : Guardian.transform.position + Vector3.up * 2.2f + Guardian.transform.forward * 0.6f, 3.0f));
            }
            yield return Say("VEN_C10", 1, 4);
            yield return Shot("c10_01_take_this_record");
            if (Guardian != null) Guardian.Face(new Vector3(60f, 0f, 200f));       // he looks toward the gardens
            _coverage = false;
            if (Guardian != null) LowOn(Guardian, P7, _side);
            yield return Say("VEN_C10", 5, 6, false);
            yield return Shot("c10_02_he_looks_toward_the_gardens");
            // she takes it; insert: she secures the small cube beside the dodecahedron
            ik.RightPos = RecordCopy != null ? RecordCopy.position : P7 + Vector3.up; ik.RightW = 1f; ik.LookPos = ik.RightPos; ik.LookW = 0.8f;
            yield return Wait(0.8f);
            bool first = VenusState.AwardRecord();
            if (RecordCopy != null && _carry != null)
            {
                var from = RecordCopy.position;
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f) { RecordCopy.position = Vector3.Lerp(from, _carry.Palm, t); yield return null; }
                // insert: in front of her at chest height, the dodecahedron out of her pocket in her hand, the small cube set
                // beside it - then both go into her pocket together (the camera on her hands, from above and in front)
                var fw2 = Flat(Seven.transform.forward).normalized;
                var held = P7 + Vector3.up * 1.18f + fw2 * 0.36f;
                ik.RightPos = held; ik.LookPos = held; ik.LookW = 0.9f;
                _carry.Set(CarriedShape.Where.Hand, 0.3f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f) { RecordCopy.position = Vector3.Lerp(RecordCopy.position, _carry.Palm + Seven.transform.right * -0.13f + Vector3.up * 0.04f, t); yield return null; }
                // from straight above her hands, tight: the dodecahedron and the cube side by side (never her body)
                Shoot(_carry.Palm + Vector3.up * 0.7f + fw2 * 0.14f + Seven.transform.right * -0.06f, _carry.Palm + Seven.transform.right * -0.06f, 26f, 0.01f);
                for (float t = 0f; t < 0.6f; t += Time.deltaTime) { RecordCopy.position = _carry.Palm + Seven.transform.right * -0.13f + Vector3.up * 0.04f; yield return null; }
                yield return Shot("c10_03_insert_secures_it_beside_the_dodecahedron");
                _carry.Set(CarriedShape.Where.Pocket, 0.3f);
                RecordCopy.gameObject.SetActive(false);
            }
            ik.RightW = 0f; ik.LookW = 0f;
            Log($"C10: the record copy awarded {(first ? "now (first time)" : "ALREADY (no second award)")} - awards {VenusState.RecordAwards} (must be 1); the original cube still in its protected structure: {(Cube != null && Cube.gameObject.activeInHierarchy ? "yes" : "NO")}");
            Complete("VEN_C10", VenusState.Stage.RecordReceived);
            Objective(VenusState.Objectives[(int)VenusState.Stage.RecordReceived]);
            yield return Cutscene(false);
        }
        // ------------------------------------------------------------------------------------------ C12: two locations (Round 13, 0m)
        /// <summary>VEN_C12 (Dan, 2026-11-19): right after C10, before she leaves the temple. The guardian wakes an ancient
        /// display - Mars and Europa - and sends both to her ship. Result: Mars and Europa on the saucer's star map.</summary>
        IEnumerator C12()
        {
            yield return Cutscene(true);
            _coverage = true; _lastSpeaker = null;
            Seven.Lock(true);
            var ik = Seven.gameObject.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
            ik.enabled = true; ik.Alive = true;
            if (Vector3.Distance(P7, VenusLayout.CubeView) > 3f) Seven.Place(VenusLayout.CubeView, Vector3.forward);
            if (Guardian != null) { Guardian.Stop(); Guardian.Face(P7); }
            // she secures the cube beside the dodecahedron, then pauses
            yield return Wait(0.8f);
            yield return Say("VEN_C12", 1, 3);
            // the guardian approaches the ancient display; two locations appear
            var dispAt = LocationsDisplay != null ? LocationsDisplay.position : VenusLayout.CubeAt + new Vector3(4.3f, 0f, -2.5f);
            if (Guardian != null) { Guardian.WalkTo(dispAt + new Vector3(-1.1f, 0f, 2.0f)); /* beside the display, never between it and her */ for (float t = 0f; t < 6f && Guardian.Walking; t += Time.deltaTime) yield return null; Guardian.Face(dispAt + Vector3.up * 1.8f); }
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(dispAt - P7)), 300f * Time.deltaTime); yield return null; }
            AudioKit.Play("energy_point", dispAt + Vector3.up * 1.8f, 0.8f);
            Renderer mars = null, europa = null;
            var locs = new List<Renderer>();
            if (LocationsDisplay != null) foreach (var r in LocationsDisplay.GetComponentsInChildren<Renderer>(true)) { if (r.name == "Loc_Mars") mars = r; if (r.name == "Loc_Europa") europa = r; if (r.name.StartsWith("Loc_")) locs.Add(r); }
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.4f)
            {
                foreach (var r in locs) Glow(r, Mathf.Lerp(0f, r.name.EndsWith("Ring") ? 0.8f : 1.6f, t));
                yield return null;
            }
            Shoot(dispAt + new Vector3(-5.2f, 2.4f, -3.0f), dispAt + Vector3.up * 1.7f, 46f, 0.8f);
            yield return Wait(0.9f);
            yield return Shot("c12_01_the_display_two_locations");
            yield return Say("VEN_C12", 4, 4);
            // over her shoulder - the display; she studies Mars first
            {
                var h = P7 + Vector3.up * 1.55f; var f = Flat(dispAt - P7).normalized; var rr = Vector3.Cross(Vector3.up, f);
                Shoot(h - f * 1.3f + rr * 0.55f, dispAt + Vector3.up * 1.9f, 40f, 0.6f);
            }
            if (mars != null) { ik.LookPos = mars.transform.position; ik.LookW = 0.9f; Glow(mars, 2.6f); }
            yield return Wait(0.6f);
            yield return Shot("c12_02_over_her_shoulder_mars");
            yield return Say("VEN_C12", 5, 8, false);
            // her attention shifts to Europa
            if (mars != null) Glow(mars, 1.4f);
            if (europa != null) { ik.LookPos = europa.transform.position; Glow(europa, 2.6f); }
            yield return Say("VEN_C12", 9, 11, false);
            yield return Shot("c12_03_europa");
            if (Guardian != null) TwoShot(Guardian, _side, 6.0f, 0.8f);
            yield return Wait(0.8f);                                       // the guardian pauses
            yield return Say("VEN_C12", 12, 14, false);
            yield return Wait(0.6f);                                       // 7 considers the two locations
            yield return Say("VEN_C12", 15, 16, false);
            // a soft tone confirms the transfer; Panther from the ship
            AudioKit.Play("console_select", dispAt + Vector3.up * 1.8f, 0.9f);
            for (float t = 0f; t < 0.8f; t += Time.deltaTime) { float k = 1f + Mathf.Sin(t * 14f) * 0.5f; if (mars != null) Glow(mars, 1.6f * k); if (europa != null) Glow(europa, 1.6f * k); yield return null; }
            yield return Say("VEN_C12", 17, 18, false);
            // she turns to the guardian
            if (Guardian != null)
            {
                for (float t = 0f; t < 0.5f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(Guardian.transform.position - P7)), 300f * Time.deltaTime); yield return null; }
                ik.LookPos = HeadOf(Guardian);
            }
            yield return Say("VEN_C12", 19, 19);
            // her hand rests briefly over the secured cube (in her pocket)
            if (_carry != null) { ik.RightPos = _carry.HipPoint; ik.RightW = 0.8f; }
            yield return Say("VEN_C12", 20, 20, false);
            yield return Shot("c12_04_thank_you");
            ik.RightW = 0f;
            // 7 inclines her head and leaves
            if (Guardian != null) { ik.LookPos = HeadOf(Guardian) + Vector3.down * 0.5f; yield return Wait(0.7f); }
            ik.LookW = 0f;
            _coverage = false;
            Complete("VEN_C12");
            Log("C12 (0m): Mars and Europa sent to the ship - available on the star map from now on");
            yield return Cutscene(false);
        }

        static Vector3 CubeTouchSpot => VenusLayout.CubeAt + new Vector3(0f, -VenusLayout.CubeAt.y, -1.98f);
        bool _autoRideStop;
        IEnumerator PalmUpLater(SevenTableIK ik, Vector3 at, float delay) { yield return Wait(delay); ik.RightPos = at; ik.RightW = 1f; ik.LookW = 0f; }
        IEnumerator HoldRecordIn(Func<Vector3> at, float seconds) { for (float t = 0f; t < seconds && RecordCopy != null && RecordCopy.gameObject.activeSelf; t += Time.deltaTime) { RecordCopy.position = at(); yield return null; } }

        UnityEngine.Video.VideoPlayer _vp2; RenderTexture _vp2RT;
        IEnumerator Vision2Play()
        {
            MusicSystem.Suspend(1.2f);                       // Round 13 (0g): resumed when the vision ends
            AudioKit.Play("vision_whoosh", null, 0.9f);
            var shimmer = AudioKit.Loop("vision_shimmer", null, null, 0f, AudioKit.Bus.Sfx);
            if (shimmer != null) StartCoroutine(AudioKit.Fade(shimmer, 0.6f, 1.5f));
            yield return UI.Fade(1f, 0.9f, 1f);
            bool video = false;
            if (Vision2 != null && !DemoBoot.NoVideo)
            {
                var vgo = new GameObject("Vision2Video");
                _vp2 = vgo.AddComponent<UnityEngine.Video.VideoPlayer>();
                _vp2.playOnAwake = false; _vp2.isLooping = false; _vp2.clip = Vision2; _vp2.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
                _vp2RT = new RenderTexture((int)Vision2.width, (int)Vision2.height, 0); _vp2.targetTexture = _vp2RT;
                _vp2.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None; _vp2.timeUpdateMode = UnityEngine.Video.VideoTimeUpdateMode.GameTime;
                _vp2.Prepare();
                for (float t = 0f; t < 3f && !_vp2.isPrepared; t += Time.deltaTime) yield return null;
                video = _vp2.isPrepared;
            }
            float aspect = Vision2 != null ? Vision2.width / (float)Mathf.Max(1u, Vision2.height) : 16f / 9f;
            bool skipped = false;
            if (video)
            {
                Log($"C09 vision: video {Vision2.name} ({Vision2.length:0.0}s, {Vision2.width}x{Vision2.height}) full-screen, skippable (Y / Enter)");
                _vp2.time = 0; _vp2.Play();
                for (float t = 0f; t < 0.7f; t += Time.deltaTime) { UI.SetVideo(_vp2RT, aspect, t / 0.7f); yield return null; }
                UI.SetVideo(_vp2RT, aspect, 1f);
                bool shot = false; float len = (float)Vision2.length;
                for (float vt0 = Time.time; Time.time - vt0 < len - 0.12f && (_vp2.isPlaying || Time.time - vt0 < 1f);)
                {
                    if (!shot && Time.time - vt0 > 6f) { shot = true; yield return Shot("c09_vision_video_frame"); }
                    if (Time.time - vt0 > 0.8f && SkipButton()) { skipped = true; break; }
                    yield return null;
                }
                if (!skipped) yield return Shot("c09_vision_last_frame_offered_palm");
                _vp2.Pause();
            }
            else
            {
                // fallback: the still of the offered palm (Resources/Venus/vision_02_still.jpg) - the scene goes on without the video
                var still = Resources.Load<Texture2D>("Venus/vision_02_still");
                Log($"C09 vision: VIDEO NOT AVAILABLE - still fallback {(still != null ? "shown" : "missing - white only")} (completion never depends on the video)");
                if (still != null) { for (float t = 0f; t < 0.7f; t += Time.deltaTime) { UI.SetVideo(still, still.width / (float)still.height, t / 0.7f); yield return null; } }
                for (float t = 0f; t < 6f && !(t > 0.8f && SkipButton()); t += Time.deltaTime) yield return null;
                yield return Shot("c09_vision_still_fallback");
            }
            if (shimmer != null) StartCoroutine(AudioKit.Fade(shimmer, 0f, 1.0f, true));
            UI.SetVideo(null, aspect, 0f);
            if (_vp2 != null) { _vp2.Stop(); Destroy(_vp2.gameObject); }
            if (skipped) Log("C09 vision skipped (Y) - the scene goes on to the match cut");
            MusicSystem.Resume(3.5f);                                  // Round 13 (0g): the level's track returns
            Log($"music after the vision (0g): '{MusicSystem.Current ?? "(none)"}' playing again");
        }

        // ------------------------------------------------------------------------------------------ B07: back to the saucer
        IEnumerator B07()
        {
            Objective(VenusState.Objectives[(int)VenusState.Stage.RecordReceived]);
            if (Auto) { if (P7.z > 236f) yield return AutoWalkPath(new[] { new Vector3(30f, 0f, 244f), new Vector3(30f, 0f, 216f) }); yield return AutoMount(); }
            yield return Until(() => Bike.Riding || P7.z < 40f, "back on the bike (B07)", 90f, "Your bike is parked in the temple court: X to ride", Bike.transform.position, () => { Bike.Place(Ground.ClampToArena(P7 + Vector3.right * 1.5f, 1f), 180f); if (!Bike.Riding) { Bike.Mount(); FollowCamera(); } });
            if (Auto) { var pts = new List<Vector3>(RoutePts(-30f, 200f)); pts.Reverse(); pts.Add(VenusLayout.BikeUnpacked + Vector3.forward * 4f); StartCoroutine(AutoRide(pts.ToArray(), () => P7.z < 12f)); }
            yield return Until(() => P7.z < 40f, "near the ship (B07)", 180f, "Ride back along the garden road to the saucer", new Vector3(10f, 0f, 30f), () => PlaceRiding(new Vector3(6f, 0f, 36f), 190f));
            yield return SayInGameplay("VEN_B07", 1, 2);      // Did they have answers? / Some.
            yield return Wait(1.4f);                          // a pause
            yield return SayInGameplay("VEN_B07", 3, 3);      // I'll tell you inside.
            yield return Until(() => P7.z < 12f && Flat(P7 - VenusLayout.SaucerAt).magnitude < 14f, "park at the ship (B07)", 60f, "Bring the bike to the ship", VenusLayout.BikeUnpacked, () => PlaceRiding(VenusLayout.BikeUnpacked + Vector3.forward * 3f, 180f));
            // she parks the bike in the saucer's storage area: the hatch opens, it glides in, its hum fades, seamless again
            yield return Cutscene(true);
            if (Bike.Riding) { Bike.ScriptThrottle = -1f; for (float t = 0f; t < 2f && Bike.Speed > 0.4f; t += Time.deltaTime) yield return null; Bike.ScriptThrottle = 0f; }
            Bike.Place(VenusLayout.BikeUnpacked, 0f);
            if (Bike.Riding) Bike.Dismount(true);
            Seven.Place(VenusLayout.BikeUnpacked - Vector3.right * 1.6f - Vector3.forward * 1.2f, Vector3.right);
            Seven.Lock(true);
            yield return BikeIntoTheHatch();
            VenusState.SaveBike(VenusLayout.BikeUnpacked, 0f, false);
            Complete("VEN_B07");
            // Round 13 (0b): aboard by the tractor beam (no ramp); Panther (back to normal size) goes up after her
            if (Cat != null && Cat.gameObject.activeInHierarchy) { Cat.transform.localScale = Vector3.one; Cat.Place(VenusLayout.BeamFoot + new Vector3(-2.2f, 0f, 1.2f), Vector3.right); }
            yield return BeamUp(true);
        }

        IEnumerator BikeIntoTheHatch()
        {
            var dir = VenusLayout.HatchDir; var tan = Vector3.Cross(Vector3.up, dir);
            float bottomIn = VenusLayout.SaucerHover + 0.1f;
            var inBay = VenusLayout.BayInside; var outAt = VenusLayout.BayOutside; float yawOut = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            var mouth = VenusLayout.HatchMouth;
            var settle = Bike.transform.position; float settleBottom = Ground.Height(settle.x, settle.z) + Bike.HoverHeight - 0.12f;
            Shoot(mouth + dir * 7.5f + tan * 4.2f + Vector3.up * 0.4f, (mouth + settle) * 0.5f, 46f, 1.0f);
            yield return Wait(1.0f);
            yield return Shot("b07_01_parked_hull_seamless");
            // the seam glows, the panel sinks and slides round
            if (HatchSeam != null) HatchSeam.enabled = true;
            var seamM = HatchSeam != null ? HatchSeam.material : null; var backM = BayBack != null ? BayBack.sharedMaterial : null;
            AudioKit.Play("hatch_iris", mouth, 0.7f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.9f) { if (seamM != null) seamM.SetColor("_EmissionColor", Color.white * Mathf.SmoothStep(0f, 3.2f, t)); yield return null; }
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.6f)
            {
                float sink = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.3f)), slide = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.25f) / 0.75f));
                if (Hatch != null) { Hatch.localScale = Vector3.one * Mathf.Lerp(1f, 0.984f, sink); Hatch.localRotation = Quaternion.Euler(0f, slide * (VenusLayout.HatchSpan + 3f), 0f); }
                if (BayLight != null) BayLight.intensity = 2.4f * slide;
                if (backM != null) backM.SetColor("_EmissionColor", Color.white * Mathf.Lerp(0.05f, 0.55f, slide));
                yield return null;
            }
            yield return Shot("b07_02_hatch_open");
            // the bike rises off its spot, turns and glides in, unhurried; the hum fades
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.6f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Bike.Glide(Vector3.Lerp(settle, outAt, u), Mathf.LerpAngle(Bike.transform.eulerAngles.y, yawOut + 180f, Mathf.Clamp01(t * 1.5f)), Mathf.Lerp(settleBottom, bottomIn, u));
                yield return null;
            }
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.6f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Bike.Glide(Vector3.Lerp(outAt, inBay, u), yawOut + 180f, bottomIn);
                if (t > 0.5f && t - Time.deltaTime / 2.6f <= 0.5f) StartCoroutine(Shot("b07_03_bike_glides_in"));
                yield return null;
            }
            BikeShown(false);                                 // inside the bay: nothing of it may show through the hull
            Bike.EndGlide();
            StartCoroutine(CloseHatch(seamM, backM));
            while (_hatchOpen) yield return null;
            Bike.gameObject.SetActive(false);
            Shoot(mouth + dir * 5.5f - tan * 2.2f + Vector3.up * 0.2f, mouth, 40f, 0.01f);
            yield return Wait(0.2f);
            LogBikeThroughHull("after B07 (hatch closed again)");
            yield return Shot("b07_04_hull_seamless_again");
            Log($"B07: the bike glided in through the hatch; closed again - panel offset {(Hatch != null ? Hatch.localRotation.eulerAngles.y : 0f):0.000} deg, bay light {(BayLight != null ? BayLight.intensity : 0f):0.00}");
        }

        // ------------------------------------------------------------------------------------------ aboard (C11) / return visits
        IEnumerator Aboard()
        {
            yield return UI.Fade(1f, 1.2f);
            Log($"aboard: the Saucer scene plays the debrief (C11) - {VenusState.Summary()}");
            if (Auto && DemoBoot.VenusStopAtShip) { ChapterDone = true; Cap.Log($"VENUS PART 2 (to the ship): {VenusState.CurrentId}, knockdowns {Knockdowns}, coverage {_coverageShots}/{_coverageMisses}, hints {Hints}, skips {Skips}"); Cap.Finish("Venus check run (stopped at the ship)"); yield break; }
            VenusState.VenusAboardStart = true; Chapter1State.Aboard = true;
            SceneManager.LoadScene("Saucer");
        }

        IEnumerator ReturnVisit()
        {
            ChapterDone = true;
            Objective(null);
            Log("return visit (section 9): the gardens as she left them - pulses stopped, creatures free, workers on the paths, water in the repaired channel, the damage still there; the reward is not repeated");
            // Round 13 (0b): she comes down in the beam (no ramp)
            yield return Cutscene(true);
            Seven.gameObject.SetActive(false);
            yield return BeamDown(false);
            yield return Cutscene(false);
            FollowCamera(); Cam.SnapBehind();
            if (Shots) { yield return Wait(1f); yield return Shot("rv_01_return_visit"); }
            if (Auto) yield return WalkSeven(VenusLayout.BeamFoot + Vector3.forward * 1.2f, 8f);
            _talkPrompt = null;
            while (true)
            {
                // Round 13 (0b): under the saucer, in the beam's spot (no ramp)
                bool atBeam = Dist7(VenusLayout.BeamFoot) < 3.0f && !Bike.Riding && Seven.Current == ChapterSeven.Mode.Free;
                _boardPrompt = atBeam;
                if (atBeam && (Input.Interact.WasPressedThisFrame() || Auto))
                {
                    _boardPrompt = false;
                    yield return Cutscene(true);
                    yield return BeamUp(false);
                    yield return UI.Fade(1f, 1f);
                    VenusState.VenusAboardStart = true; Chapter1State.Aboard = true;
                    if (Auto) { Cap.Log("return visit: boarded the saucer (Saucer scene: free exploration + destinations)"); Cap.Finish("Venus return visit check"); yield break; }
                    SceneManager.LoadScene("Saucer");
                    yield break;
                }
                yield return null;
            }
        }
        bool _boardPrompt;

        /// <summary>Part 2's context prompts (called from Update): devices, the console, the guardian, the record, boarding.</summary>
        bool Part2Prompt()
        {
            if (_encPrompt != null) { Legend.Context(_encBtn, _encPrompt); return true; }
            if (_boardPrompt) { Legend.Context(ControlsLegend.Btn.Interact, "Board the saucer"); return true; }
            if (_talkPrompt != null && Seven.Current == ChapterSeven.Mode.Free)
            {
                bool near = _talkPrompt == "Touch the record" ? Flat(CubeTouchSpot - P7).magnitude < 1.4f : Guardian != null && Flat(Guardian.transform.position - P7).magnitude < 4f;
                if (near) { Legend.Context(ControlsLegend.Btn.Interact, _talkPrompt); if (!Auto && Input.Interact.WasPressedThisFrame()) { if (_talkPrompt == "Touch the record") _cubeHit = true; else _talkHit = true; } return true; }
            }
            return false;
        }

        // ------------------------------------------------------------------------------------------ check-run autopilot (the encounter)
        IEnumerator AutoWaitClear(int trough, Vector3 crossing)
        {
            for (float t0 = Time.time; Time.time - t0 < 20f;)
            {
                bool clear = true;
                foreach (var w in ChamberWorms)
                    if (w != null && w.gameObject.activeSelf && w.Group == TroughGroup(trough))
                    {
                        float d = Flat(w.transform.position - crossing).magnitude;
                        bool coming = Vector3.Dot(Flat(crossing - w.transform.position), w.Heading) > 0f;
                        if (d < 4.5f || coming && d < 12f) clear = false;
                    }
                if (clear) yield break;
                yield return null;
            }
        }
        static string TroughGroup(int t) => t == 0 ? "dev_north" : t == 1 ? "dev_ledge" : "dev_crystal";
        IEnumerator AutoEncounter()
        {
            Seven.ScriptDriven = true;
            yield return Wait(0.6f);
            // north device: cross the north trough
            yield return AutoWalkPath(new[] { new Vector3(112f, VenusLayout.ControlY, 94f) });
            yield return AutoWaitClear(0, new Vector3(112f, VenusLayout.ControlY, 97f));
            yield return AutoWalkPath(new[] { new Vector3(112f, VenusLayout.ControlY, 100.5f), VenusLayout.DeviceUse[0] });
            yield return Shot("enc_at_the_north_device");
            yield return UseDevice(0);
            // back over the north trough, then over the south trough to the crystal's walkway
            yield return AutoWalkPath(new[] { new Vector3(112f, VenusLayout.ControlY, 100.5f) });
            yield return AutoWaitClear(0, new Vector3(112f, VenusLayout.ControlY, 97f));
            yield return AutoWalkPath(new[] { new Vector3(112f, VenusLayout.ControlY, 89f), new Vector3(114f, VenusLayout.ControlY, 83f) });
            yield return AutoWaitClear(2, new Vector3(114f, VenusLayout.ControlY, 79.5f));
            yield return AutoWalkPath(new[] { new Vector3(114.5f, VenusLayout.ControlY, 75.5f), VenusLayout.DeviceUse[2] });
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(VenusLayout.DeviceAt[2] - P7)), 400f * Time.deltaTime); yield return null; }
            yield return Shot("enc_crystal_in_view");
            yield return PullCrystal();
            // the ledge: climb the low wall (A), X at the device, jump down
            yield return AutoWalkPath(new[] { new Vector3(109.5f, VenusLayout.ControlY, 72.4f) });
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Vector3.back), 400f * Time.deltaTime); yield return null; }
            string ctx = Seven.Context; Seven.ScriptJump = true; yield return Wait(2.0f);
            Log($"ledge climb: prompt '{ctx}', up on the ledge: {(P7.y > VenusLayout.LedgeY - 0.2f ? "ok" : "FAILED")} ({P7:0.00})");
            if (P7.y < VenusLayout.LedgeY - 0.2f) Seven.Place(VenusLayout.DeviceUse[1], Vector3.back);
            yield return AutoWalkPath(new[] { VenusLayout.DeviceUse[1] });
            yield return Shot("enc_on_the_ledge");
            yield return UseDevice(1);
            yield return AutoWalkPath(new[] { new Vector3(109.5f, VenusLayout.LedgeY, 70.8f) });
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Vector3.forward), 400f * Time.deltaTime); yield return null; }
            ctx = Seven.Context; Seven.ScriptJump = true; yield return Wait(1.6f);
            Log($"ledge jump down: prompt '{ctx}', back on the floor: {(P7.y < VenusLayout.ControlY + 0.2f ? "ok" : "FAILED")}");
            if (P7.y > VenusLayout.ControlY + 0.3f) Seven.Place(new Vector3(109.5f, VenusLayout.ControlY, 73f), Vector3.forward);
        }
        IEnumerator AutoConsole()
        {
            yield return Wait(3.5f);                              // the creatures withdraw
            yield return AutoWalkPath(new[] { new Vector3(114f, VenusLayout.ControlY, 84f), new Vector3(122f, VenusLayout.ControlY, 86f), VenusLayout.ConsoleAt - Vector3.right * 1.3f });
            yield return Shot("enc_at_the_console");
            _consoleHit = true;
        }
    }
}
