using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using L = Seven.AsteroidLayout;

namespace Seven
{
    /// <summary>Round 16 â€” the Asteroid: the flow, AST_02-AST_04 (AST_05-AST_09 + the way home: AsteroidDirectorHall).</summary>
    public partial class AsteroidDirector
    {
        IEnumerator Flow(int cp)
        {
            Restore(cp);
            yield return null; yield return null;
            if (cp <= L.CpReceiving) { BikeHoldWatch.Begin("asteroid", Hold, Bike, Saucer); yield return Receiving(); Checkpoint(L.CpCity); }
            else yield return UI.Fade(0f, 1.2f);
            if (AsteroidState.Checkpoint <= L.CpCity) { yield return City(); Checkpoint(L.CpMachinist); }
            if (AsteroidState.Checkpoint <= L.CpMachinist) { yield return MeetMachinist(); Checkpoint(L.CpControls); }
            if (AsteroidState.Checkpoint <= L.CpControls) { yield return Controls(); Checkpoint(L.CpDrop); }
            if (AsteroidState.Checkpoint <= L.CpDrop) { yield return Drop(); Checkpoint(L.CpFight); }
            if (AsteroidState.Checkpoint <= L.CpFight) { yield return Objection(); Checkpoint(L.CpArchive); }
            if (DemoBoot.BossCheck || DemoBoot.BossIdle) yield break;
            if (AsteroidState.Checkpoint <= L.CpArchive) { yield return ArchiveVision(); Checkpoint(L.CpInstruction); }
            yield return Instruction();
        }

        /// <summary>Put everything where the checkpoint says (reload, Continue, Scene Select).</summary>
        void Restore(int cp)
        {
            foreach (var z in L.Zones) SetZone(z.Name, z.On);
            bool doorOpen = cp >= L.CpCity, hallSealed = cp >= L.CpControls && cp < L.CpArchive, afterFight = cp >= L.CpArchive;
            SetZone("ServiceDoor", doorOpen); if (ServiceDoor != null) ServiceDoor.Set(!doorOpen); if (DoorLine != null) DoorLine.All(doorOpen);
            if (OuterGate != null) OuterGate.Set(true);
            Glow(DoorLamp, doorOpen ? 1.6f : 0.1f);
            for (int i = 0; i < 3; i++)
            {
                bool al = AsteroidState.StepDone("junction" + (i + 1)) || cp > L.CpCity;
                SetZone("Junction" + (i + 1), al); if (Junctions != null && i < Junctions.Length && Junctions[i] != null) Junctions[i].Set(al);
                if (JunctionLamps != null && i < JunctionLamps.Length) Glow(JunctionLamps[i], al ? 1.6f : 0.35f);
            }
            SetZone("HallDoorZ", !hallSealed); if (HallDoorShutter != null) HallDoorShutter.Set(hallSealed);
            SetZone("MaintHatchZ", !hallSealed); if (HatchShutter != null) HatchShutter.Set(hallSealed);
            bool westOpen = cp >= L.CpFight; SetZone("WestPass", westOpen); if (WestShutter != null) WestShutter.Set(!westOpen);
            SetZone("Archive", afterFight); if (ArchiveShutter != null) ArchiveShutter.Set(!afterFight);
            bool c1 = AsteroidState.StepDone("control_transport") || cp > L.CpDrop, c2 = AsteroidState.StepDone("control_cooling") || cp > L.CpDrop, c3 = AsteroidState.StepDone("control_diverter") || cp > L.CpDrop;
            SetZone("Platforms1", c1); PlacePlatforms(c1 ? 1f : 0f);
            SetZone("CoolPass", c2); SteamOn(!c2); if (ClampPower != null) ClampPower.All(!c2); if (Coolant != null) Coolant.All(c2);
            SetZone("FinalPlat", true); SetZone("LowLedge", false); if (FinalPlat != null) FinalPlat.localPosition = Vector3.zero;
            if (ControlLamps != null) for (int i = 0; i < ControlLamps.Length; i++) Glow(ControlLamps[i], (i == 0 ? c1 : i == 1 ? c2 : c3) ? 1.8f : 0.3f);
            if (ArchiveLine != null) { ArchiveLine.Stopped = c1 && !c3; ArchiveLine.Diverted = c3; }
            if (CutterPanel != null) CutterPanel.gameObject.SetActive(cp < L.CpControls);
            if (LockerDoor != null) LockerDoor.localRotation = Quaternion.Euler(0f, cp > L.CpControls || AsteroidState.StepDone("blades") ? -100f : 0f, 0f);
            if (LockerBlades != null) LockerBlades.gameObject.SetActive(!(cp > L.CpControls || AsteroidState.StepDone("blades")));
            Seven.SetBladesOwned(AsteroidState.BladesOwned);
            if (RecordCubo != null) RecordCubo.gameObject.SetActive(false);
            if (Cradle != null) Cradle.Set(cp >= L.CpArchive && cp < L.CpHome);
            if (Stations != null) for (int i = 0; i < Stations.Length; i++) if (Stations[i] != null) Stations[i].Set(cp >= L.CpArchive && i < 2);
            if (MRig != null && cp >= L.CpArchive) { if (MRig.CutterTool != null) MRig.CutterTool.gameObject.SetActive(false); if (MRig.ClampTool != null) MRig.ClampTool.gameObject.SetActive(false); }
            if (Security != null) foreach (var s in Security) if (s != null) { s.gameObject.SetActive(cp >= L.CpControls && cp <= L.CpDrop); s.transform.position = s.Home; }
            // the people: the Machinist in his repair cradle (folded) until AST_04; at the terminal / in the cradle later
            if (Machinist != null)
            {
                Machinist.gameObject.SetActive(true);
                if (cp <= L.CpMachinist) { Machinist.Place(L.CradleAt, Vector3.back); Machinist.Play(Machinist.HasState("dazed") ? "dazed" : "idle"); }
                else if (cp < L.CpArchive) { Machinist.Place(L.CutterAt + new Vector3(4f, 0f, -3f), Vector3.left); Machinist.Play("idle"); }
                else { Machinist.Place(L.CradleAt, Vector3.back); Machinist.Play("dazed", 0f); if (cp < L.CpHome) StartCoroutine(RestrainAfter(0.6f)); }   // QF21 (11b)
                if (MRig != null) { MRig.Restrained = false; MRig.Calm = false; }
            }
            if (Saucer != null) Saucer.position = L.SaucerAt;
            if (MaintBot != null) { MaintBot.transform.position = L.MaintBotHome; MaintBot.Goal = L.MaintBotHome; MaintBot.ShowArrow(false); }
            if (Container != null) Container.position = cp <= L.CpReceiving ? L.ContainerAt + Vector3.up * 0.55f : L.SortingAt + new Vector3(4f, -20f, 0f);
            if (Scanner != null) { Scanner.Target = null; Scanner.TargetPoint = L.SevenDown; }
            if (Helmet != null) Helmet.On = cp <= L.CpReceiving;
            Vector3 at; float yaw = 270f; bool riding = true;
            switch (cp)
            {
                case L.CpReceiving: at = L.BikePad; riding = false; break;
                case L.CpCity: at = L.TunnelEnd + new Vector3(4f, 0f, 0f); break;
                case L.CpMachinist: at = L.HallPlazaStop; break;
                case L.CpControls: at = new Vector3(-328f, 0f, 47f); riding = false; yaw = 180f; break;
                case L.CpDrop: at = new Vector3(-362f, 0f, 22f); riding = false; yaw = 270f; break;
                case L.CpFight: at = new Vector3(-396f, 0f, 64f); riding = false; yaw = 0f; break;
                case L.CpArchive: at = L.CradleAt + new Vector3(0f, 0f, -5f); riding = false; yaw = 0f; break;
                default: at = L.ArchiveDoor + new Vector3(4f, 0f, 0f); riding = false; yaw = 90f; break;
            }
            if (cp >= L.CpMachinist + 1) Bike.Place(L.HallPlazaStop, 270f); else Bike.Place(at, yaw);
            if (cp <= L.CpReceiving) { Bike.gameObject.SetActive(false); Seven.gameObject.SetActive(false); }   // both start in the hold / aboard
            else if (riding) { Seven.gameObject.SetActive(true); Bike.Mount(); }
            else { Seven.gameObject.SetActive(true); Seven.Place(at, Quaternion.Euler(0f, yaw, 0f) * Vector3.forward); }
            MusicSystem.Play(cp <= L.CpReceiving ? null : "asteroid_city", 2.5f);   // QF24 (18): the Machinist's track starts with his fight
            FollowCamera(); Cam.SnapBehind();
            Log($"restore: {AsteroidState.Summary()} -> {(riding ? "riding" : "on foot")} at {at}");
        }
        void PlacePlatforms(float k)
        {
            if (CargoPlatforms == null || PlatformParked == null || PlatformAligned == null) return;
            for (int i = 0; i < CargoPlatforms.Length; i++) if (CargoPlatforms[i] != null) CargoPlatforms[i].position = Vector3.Lerp(PlatformParked[i], PlatformAligned[i], k);
        }
        void SteamOn(bool on) { if (Steam == null) return; foreach (var p in Steam) if (p != null) { if (on) p.Play(); else p.Stop(true, ParticleSystemStopBehavior.StopEmitting); } }

        // ------------------------------------------------------------------------------------------ AST_02: receiving
        IEnumerator Receiving()
        {
            yield return Cutscene(true);
            // ESTABLISHING: the sealed arrival chamber - the outer gate closes behind the saucer, which holds position
            if (OuterGate != null) OuterGate.Set(false);
            Shoot(new Vector3(15f, 14f, 16f), new Vector3(0f, 3f, -6f), 56f, 0.01f);
            StartCoroutine(UI.Fade(0f, 1.6f));
            yield return Wait(0.8f);
            if (OuterGate != null) yield return OuterGate.Move(true, 3.2f);
            if (OuterGate != null) ImageGate.Expect(OuterGate.transform, "the outer gate, shut", 0f, "a01_");   // QF26 (2)
            yield return Shot("a01_the_receiving_chamber_the_outer_gate_shut");
            // the bike lowers out of the bike hold onto its pad; 7 comes down the beam onto hers (no ramp)
            yield return HoldBikeOut(L.BikePad, 270f);
            Shoot(L.SaucerAt + new Vector3(9f, 2.4f, -9f), L.SaucerAt + new Vector3(0f, 2.2f, 0f), 46f, 0.01f);
            if (SaucerBeam != null) { yield return SaucerBeam.Iris(true); yield return SaucerBeam.BeamOn(true); }
            Seven.gameObject.SetActive(true); Seven.Lock(true); Seven.BeginFloat();
            if (Helmet != null) Helmet.On = true;
            bool s3 = false;
            if (SaucerBeam != null) yield return SaucerBeam.Lower(Seven.transform, 3.4f, Vector3.forward, t => { if (!s3 && t > 0.5f) { s3 = true; StartCoroutine(Shot("a02_7_down_the_beam_onto_her_pad_bike_on_its_pad")); } });
            Seven.EndFloat(); Seven.Place(L.SevenDown, Vector3.forward); Seven.Lock(true);
            if (SaucerBeam != null) StartCoroutine(SaucerBeam.Open(false));
            // the chamber pressurizes before she takes the helmet off
            AudioKit.Play("ast_pressurize", L.SevenDown, 1f, 0f, AudioKit.Bus.Sfx, 60f);
            if (PressureVents != null) foreach (var v in PressureVents) Glow(v, 1.6f);
            Shoot(P7 + new Vector3(2.4f, 1.7f, 2.6f), P7 + Vector3.up * 1.5f, 40f, 0.01f);
            yield return Wait(2.2f);
            if (PressureVents != null) foreach (var v in PressureVents) Glow(v, 0.2f);
            if (Helmet != null) { Helmet.Pulse(); yield return Wait(0.4f); Helmet.On = false; }
            Log("AST_02: the chamber pressurized, then her helmet off");
            yield return Shot("a03_pressurized_helmet_off");
            // the maintenance machine approaches and scans her; the scanner on the wall finds her too
            if (MaintBot != null) { MaintBot.Goal = P7 + new Vector3(0.6f, 0f, 2.3f); MaintBot.ScanTarget = Seven.transform; }   // QF21 (2): the lens narrows, the fan sweeps her
            if (Scanner != null) { Scanner.Target = Seven.transform; }
            AudioKit.Play("ast_servo_small", L.MaintBotHome, 1f, 0f, AudioKit.Bus.Sfx, 30f);
            Shoot(P7 + new Vector3(-2.6f, 1.2f, -1.2f), P7 + new Vector3(0.5f, 0.9f, 2f), 44f, 0.5f);
            yield return Wait(2.4f);
            if (MaintBot != null) MaintBot.Face(P7);
            yield return Shot("a04_the_maintenance_machine_scans_her");
            var side = Vector3.right;
            TwoShot((P7 + (MaintBot != null ? MaintBot.transform.position : P7 + Vector3.forward * 2f)) * 0.5f, side, 3.2f, 1.2f, 1.0f, 44f, 0.4f);
            if (MaintBot != null) MaintBot.ScanTarget = null;
            yield return Say("AST_02", 1, 1);                     // Hello.
            if (MaintBot != null) { MaintBot.Tilt(26f, 1.0f); yield return Wait(0.5f); yield return Shot("a04b_system_tilts_its_head_at_hello"); }   // QF21 (2): a beat of mechanical curiosity
            yield return Say("AST_02", 2, 2);                     // Unregistered material. Proceed to sorting.
            if (MaintBot != null) MaintBot.ShowArrow(true, P7 + new Vector3(1.2f, 0f, 3.2f), new Vector3(0.4f, 0f, 1f));   // the projected symbol: toward the conveyor
            yield return Say("AST_02", 3, 3);                     // Material?
            if (MaintBot != null) { MaintBot.Tilt(-22f, 0.9f); yield return Wait(0.35f); }
            yield return Say("AST_02", 4, 5);                     // You do contain quite a lot of it. / Thank you. Very helpful.
            // the conveyor begins moving
            var belt = AudioKit.Loop("ast_conveyor", null, null, 0.5f, AudioKit.Bus.Sfx);
            yield return Say("AST_02", 6, 6);                     // Please remain intact during transport.
            Shoot(new Vector3(-4f, 2.2f, 6f), new Vector3(6f, 0.6f, L.ConveyorZ), 52f, 0.5f);
            yield return Shot("a05_the_conveyor_to_sorting");
            TwoShot(P7 + Vector3.forward * 1.2f, Vector3.right, 3.0f, 1.3f, 1.3f, 44f, 0.5f);
            yield return Say("AST_02", 7, 8);                     // Mm. Don't like that bit. / There's a service entrance to your left.
            // the puzzle's reason: the scanner keeps its eye on her (the cone of light on her) - give it the marked container
            Shoot(P7 + new Vector3(1.6f, 1.6f, -2.4f), L.ScannerAt, 50f, 0.5f);
            yield return Wait(0.6f);
            yield return Shot("a06_the_scanner_holds_her");
            yield return SayIds("AST_02_P1", "AST_02_P2");        // ... Give it something more interesting. The marked container. / More interesting than me. Rude.
            yield return Cutscene(false);
            Complete("AST_02_lines");
            // QF21 (3): no walking back and forth for the bike - Panther tells her to take it first; the intro puzzle is then
            // solved from the saddle (telekinesis on the scanner, X at the door control) and she rides straight through.
            yield return SayIds("AST_02_P5");                     // Take the bike. Whatever's through there, it's a long way.
            Objective("Get on your bike (X) - then open the service entrance");
            FreeBike = true;
            ShowGuide(Bike.transform.position);
            if (Auto) { yield return WalkSeven(Bike.transform.position - Bike.transform.right * 1.1f, 15f); PlaceRiding(Bike.transform.position, 270f); }
            yield return Until(() => Riding, "on the bike", 60f, "Walk to the bike on its pad and press X", Bike.transform.position, () => PlaceRiding(Bike.transform.position, 270f));
            HideGuide();
            Log("AST_02 (QF21 3): she mounted the bike first - the scanner and the door control are worked from the saddle");
            yield return Shot("a06b_on_the_bike_first");
            // THE INTRO PUZZLE: while the scanner holds her, the door control stays locked (its lamp dark) and the maintenance
            // machine keeps between her and it. Telekinesis turns the scanner to the marked container -> it processes the
            // container (the conveyor takes it to sorting, the machine escorts it) -> the control's lamp lights -> X.
            Objective("Open the service entrance - first turn the scanner onto the marked container (RT, from the bike)");
            var bot = StartCoroutine(BotGuards());
            var scannerHead = Scanner != null && Scanner.Head != null ? Scanner.Head : null;
            yield return Telekinesis(L.ScannerAt, L.ScannerTkRange, "turn the scanner (telekinesis)", "Look at the scanner arm on the far wall and use telekinesis (RT) - from the bike", Scanner != null ? Scanner.Cone : null, null, false, scannerHead);
            if (Scanner != null) yield return Scanner.SwingTo(Container, 1.0f);
            StopCoroutine(bot);
            if (MaintBot != null) MaintBot.ScanTarget = Container;
            AudioKit.Play("ast_scanner", L.ContainerAt, 0.8f, 0f, AudioKit.Bus.Sfx, 30f);
            if (ContainerMarks != null) Glow(ContainerMarks, 1.8f);
            Shoot(L.ContainerAt + new Vector3(5f, 3f, -6f), L.ContainerAt + Vector3.up * 0.8f, 46f, 0.5f);
            _cut = true;
            yield return Wait(0.5f);
            if (Container != null) ImageGate.Expect(Container, "the container under the scanner", 0f, "a07_");   // QF26 (2)
            yield return Shot("a07_the_scanner_on_the_container");
            yield return SayIds("AST_02_P3");                     // Material registered. Proceed to sorting.
            StartCoroutine(ContainerToSorting());
            Glow(DoorLamp, 1.6f);                                 // the door control is free: its lamp lights
            yield return Wait(1.2f);
            _cut = false; FollowCamera();
            yield return SayIds("AST_02_P4");                     // The door is free. Go, before it remembers you.
            Objective("Open the service entrance (ride up to the door control, X from the bike)");
            yield return Interact(L.DoorControlView, "Open the door", "open the service entrance", "Ride up to the wheel on the post beside the door (west wall) and press X", new[] { new Vector3(-8f, 0f, -6f), L.DoorControlView }, null, 1.8f, true, 5.5f);
            // the payoff: the wheel turns, the door rolls up, light runs along its frame, the tunnel beyond is lit
            yield return Cutscene(true);
            if (Riding) Bike.ScriptThrottle = 0f;
            Shoot(L.DoorAt + new Vector3(7f, 2.4f, -7f), L.DoorAt + Vector3.up * 2.4f, 50f, 0.01f);
            AudioKit.Play("ast_door_manual", L.DoorAt, 1f, 0f, AudioKit.Bus.Sfx, 50f);
            if (DoorWheel != null) StartCoroutine(Spin(DoorWheel, Vector3.forward, 720f, 1.6f));
            if (ServiceDoor != null) StartCoroutine(ServiceDoor.Move(false, 2.6f));
            if (DoorLine != null) StartCoroutine(DoorLine.Run(0, DoorLine.Count - 1, 2.2f));
            yield return Wait(1.6f);
            if (ServiceDoor != null) ImageGate.Expect(ServiceDoor.transform, "the service entrance", 0f, "a08_");   // QF26 (2)
            yield return Shot("a08_the_service_entrance_opens");
            yield return Wait(1.2f);
            SetZone("ServiceDoor", true);
            if (belt != null) StartCoroutine(AudioKit.Fade(belt, 0f, 2f, true));
            yield return Cutscene(false);
            Complete("AST_02");
            Objective("Ride through the service entrance");
            if (!Riding) yield return GetBackOn();
            yield return RideRoute(new[] { new Vector3(-12f, 0f, -4f), new Vector3(-21f, 0f, -4f), new Vector3(-40f, 0f, -4f), L.TunnelEnd }, "through the service tunnel", 3.5f);   // QF21: through the door's middle (the bike now starts at the door control)
        }
        IEnumerator Spin(Transform t, Vector3 axis, float deg, float secs) { for (float k = 0f; k < secs; k += Time.deltaTime) { t.Rotate(axis, deg / secs * Time.deltaTime, Space.Self); yield return null; } }
        /// <summary>While the scanner holds her, the maintenance machine keeps itself between her and the door control.</summary>
        IEnumerator BotGuards()
        {
            while (MaintBot != null)
            {
                var c = L.DoorControl; var toHer = Flat(P7 - c);
                if (toHer.magnitude < 9f) MaintBot.Goal = c + toHer.normalized * 1.6f;
                else MaintBot.Goal = Vector3.Lerp(MaintBot.Goal, L.MaintBotHome, Time.deltaTime);
                MaintBot.Face(P7);
                yield return null;
            }
        }
        IEnumerator ContainerToSorting()
        {
            if (Container == null) yield break;
            var a = Container.position; var b = new Vector3(L.SortingAt.x - 1f, a.y, L.ConveyorZ);
            if (MaintBot != null) MaintBot.ShowArrow(false);
            for (float d = 0f; d < 1f; d += Time.deltaTime / 9f)
            {
                Container.position = Vector3.Lerp(a, b, d);
                if (MaintBot != null) { MaintBot.Goal = Container.position + new Vector3(-2f, 0f, -2.4f); MaintBot.Face(Container.position); }
                yield return null;
            }
            Container.position = L.SortingAt + new Vector3(4f, -20f, 0f);           // through the sorting slot
            if (MaintBot != null) MaintBot.Goal = L.MaintBotHome;
            Log("AST_02: the container went into sorting; the maintenance machine escorted it");
        }
        IEnumerator HoldBikeOut(Vector3 settle, float settleYaw)
        {
            if (Hold == null) { Bike.gameObject.SetActive(true); Bike.Place(settle, settleYaw); yield break; }
            var dir = Hold.Dir; var tan = Vector3.Cross(Vector3.up, dir); var mouth = Hold.Mouth;
            float yawOut = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, bottomIn = Hold.transform.position.y + Hold.BottomIn;
            Bike.gameObject.SetActive(true); ShowBike(false);
            Bike.Glide(Hold.Inside, yawOut, bottomIn);
            var camAt = mouth + dir * 6.2f + tan * 2.6f - Vector3.up * 0.3f;
            Shoot(camAt, mouth - Vector3.up * 0.1f, 42f, 0.01f);
            yield return Wait(0.8f);
            yield return Hold.Open(null, s => { if (s > 0.02f) ShowBike(true); });
            ShowBike(true);
            if (Hold != null) ImageGate.Expect(Hold.transform, "the open bike hold", 0f, "a01b_");   // QF26 (2)
            yield return Shot("a01b_the_bike_hold_open");
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.8f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Bike.Glide(Vector3.Lerp(Hold.Inside, Hold.Outside, u), yawOut, bottomIn);
                Cam.Shot(camAt + dir * 1.5f * u, Bike.transform.position + Vector3.up * (bottomIn - Bike.transform.position.y + 0.4f), 0.8f, false, 42f);
                yield return null;
            }
            StartCoroutine(CloseHoldLater(1.2f));
            float settleBottom = settle.y + Bike.HoverHeight - 0.12f; var outAt = Hold.Outside;
            Shoot((outAt + settle) * 0.5f + Vector3.Cross(Vector3.up, Flat(settle - outAt).normalized) * -7.5f + Vector3.up * 1.6f, (outAt + settle) * 0.5f + Vector3.up * 0.6f, 48f, 1.6f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 3.0f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Bike.Glide(Vector3.Lerp(outAt, settle, u), Mathf.LerpAngle(yawOut, settleYaw, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.2f))), Mathf.Lerp(bottomIn, settleBottom, u * u));
                yield return null;
            }
            Bike.EndGlide(); Bike.Place(settle, settleYaw);
            while (Hold.IsOpen) yield return null;
            Log("the bike came out of the bike hold onto its pad; the hold closed seamlessly (7 comes down in the beam)");
        }
        IEnumerator CloseHoldLater(float s) { yield return Wait(s); yield return Hold.Close(); }
        void ShowBike(bool on)
        {
            foreach (var r in Bike.GetComponentsInChildren<Renderer>(true)) r.enabled = on;
            foreach (var l in Bike.GetComponentsInChildren<Light>(true)) l.enabled = on;
        }

        // ------------------------------------------------------------------------------------------ AST_03: the city that still works
        IEnumerator City()
        {
            MusicSystem.Play("asteroid_city", 3f);
            if (!Riding) { PlaceRiding(L.TunnelEnd + new Vector3(4f, 0f, 0f), 270f); }
            // WIDE REVEAL: she slows the bike at the tunnel mouth; the camera rises past her shoulder onto the cavern
            if (!AsteroidState.SceneDone("AST_03_lines"))
            {
                yield return StopBike(1.6f);
                yield return Cutscene(true);
                var bp = Bike.transform.position;
                var c0 = bp + new Vector3(3.2f, 1.9f, 1.2f); var c1 = bp + new Vector3(4.5f, 9f, -4f);
                var look = new Vector3(-170f, 4f, 70f);
                Shoot(c0, bp + new Vector3(-6f, 1.4f, 0f), 50f, 0.01f);
                StartCoroutine(FogThin(0.85f, 2f));
                for (float t = 0f; t < 1f; t += Time.deltaTime / 4.5f) { float u = Mathf.SmoothStep(0f, 1f, t); Cam.Shot(Vector3.Lerp(c0, c1, u), Vector3.Lerp(bp + new Vector3(-6f, 1.4f, 0f), look, u), 1.2f, false, Mathf.Lerp(50f, 62f, u)); yield return null; }
                ImageGate.ExpectPoint(look, "the city that still works", 0f, "a10_");   // QF26 (2)
                yield return Shot("a10_the_city_that_still_works_wide");
                yield return Say("AST_03", 1, 2);                 // It's all still going. / Yes.
                // P3 (1): the camera by the bike looked at the workstations THROUGH the causeway deck and its piers (a11: "the empty
                // workstations" blocked) - the first spot near her side with a clear line both ways to them
                var ws = new Vector3(-183f, 1f, 76f); var wsCam = bp + new Vector3(-3f, 2.2f, 6f);
                if (!(ChapterCamera.ClearLine(wsCam, ws) && ChapterCamera.ClearLine(ws, wsCam)))
                    for (int ci = 0; ci < 36; ci++)
                    {
                        var dir0 = Flat(bp - ws).normalized; if (dir0.sqrMagnitude < 0.01f) dir0 = Vector3.back;
                        float ang = ((ci % 9) - 4) * 22f, dist = 9f + (ci / 9) * 4f, hgt = 2.5f + (ci / 9) * 1.5f;
                        var c = ws + Quaternion.Euler(0f, ang, 0f) * dir0 * dist + Vector3.up * hgt;
                        if (ChapterCamera.ClearLine(c, ws) && ChapterCamera.ClearLine(ws, c)) { wsCam = c; break; }
                    }
                Log($"a11 (P3 1): the workstations' camera {wsCam}");
                Shoot(wsCam, ws, 40f, 0.6f);
                yield return Wait(0.5f);
                ImageGate.ExpectPoint(new Vector3(-183f, 1f, 76f), "the empty workstations", 0f, "a11_");   // QF26 (2)
                yield return Shot("a11_the_empty_workstations");
                yield return Say("AST_03", 3, 4);                 // But there's nobody at the stations. / I haven't found anyone yet.
                // the objective's reason: the junctions are set for the carriers, not for her
                Shoot(bp + new Vector3(2f, 3f, 4f), L.Junctions[0] + Vector3.up * 1f, 44f, 0.6f);
                yield return Wait(0.4f);
                ImageGate.ExpectPoint(L.Junctions[0] + Vector3.up * 1f, "the first junction", 0f, "a12_");   // QF26 (2)
                yield return Shot("a12_the_first_junction_set_for_the_carriers");
                yield return SayIds("AST_03_P1", "AST_03_P2");     // ... set for the carriers, not for you. / Then I'll borrow them.
                StartCoroutine(FogThin(0f, 2f));
                Complete("AST_03_lines");
                yield return Cutscene(false);
            }
            FreeBike = true;
            var bird = StartCoroutine(BirdWatch());
            for (int i = 0; i < 3; i++)
            {
                if (AsteroidState.StepDone("junction" + (i + 1))) continue;
                int n = AsteroidState.StepsDone("junction");
                Objective($"Align the three rail junctions on the causeway (from the bike)\nJunctions {n} / 3");
                var route = i == 0 ? L.Ride1 : i == 1 ? L.Ride2 : L.Ride3;
                yield return RideRoute(route, $"to junction {i + 1}", 7f);
                yield return StopBike(1.0f);
                yield return Junction(i);
            }
            Objective("Follow the causeway to the temple signal\nJunctions 3 / 3");
            yield return RideRoute(L.Ride4, "to the central hall", 7f);
            yield return StopBike(1.4f);
            StopCoroutine(bird); SetPrompt(null);
            Complete("AST_03");
        }
        bool _junctionLine;
        IEnumerator Junction(int i)
        {
            var ctl = L.JunctionControls[i];
            // QF21 (4): worked from the bike; on foot the prompt says so, Panther once, the marker back to the bike
            yield return Interact(ctl, "Align the rails", $"align junction {i + 1}", "Ride up to the lever at the edge of the causeway, just before the turntable, and press X from the bike", null, null, 1.8f, true, 5.5f, "Use the junction from your bike", _junctionLine ? null : "AST_03_P3");
            _junctionLine = true;
            // the payoff: the lever throws, the turntable turns into line, the route lights run ahead, its lamp turns white
            yield return Cutscene(true);
            if (Riding) { Bike.ScriptThrottle = 0f; }
            var c = L.Junctions[i]; var axis = L.JunctionAxis[i] == 0f ? Vector3.right : Vector3.forward;
            Shoot(c + Vector3.Cross(Vector3.up, axis) * 13f + Vector3.up * 7f - axis * 6f, c + Vector3.up * 0.5f, 52f, 0.01f);
            if (JunctionLevers != null && i < JunctionLevers.Length && JunctionLevers[i] != null) StartCoroutine(Spin(JunctionLevers[i], Vector3.right, -70f, 0.5f));
            AudioKit.Play("mars_control_on", ctl, 0.8f, 0f, AudioKit.Bus.Sfx, 40f);
            if (Junctions != null && i < Junctions.Length && Junctions[i] != null) yield return Junctions[i].Turn(2.6f);
            if (JunctionLamps != null && i < JunctionLamps.Length) Glow(JunctionLamps[i], 1.6f);
            SetZone("Junction" + (i + 1), true);
            AsteroidState.DoStep("junction" + (i + 1));
            int n = AsteroidState.StepsDone("junction");
            Log($"junction {i + 1} aligned (the turntable turned into line, the route lights ran ahead) - Junctions {n} / 3");
            ImageGate.ExpectPoint(c + Vector3.up * 0.5f, "the junction turned into line", 0f, "a13_");   // QF26 (2)
            yield return Shot($"a13_junction{i + 1}_aligned_route_lights_ahead");
            UI.Hint($"Junctions {n} / 3", 2.5f);
            yield return Cutscene(false);
        }
        /// <summary>The optional inspection: the little mechanical bird at a workstation beside the causeway (X "Look").</summary>
        IEnumerator BirdWatch()
        {
            if (AsteroidState.BirdSeen) yield break;
            while (true)
            {
                if (BirdLamp != null) Glow(BirdLamp, 0.5f + 0.25f * Mathf.Sin(Time.time * 2f));
                bool near = !Riding && !_cut && Seven.Current == ChapterSeven.Mode.Free && Flat(P7 - L.BirdView).magnitude < 1.8f;
                if (near) { if (Prompt == null || Prompt == "Look") SetPrompt("Look"); }
                else if (Prompt == "Look") SetPrompt(null);
                if (near && (DemoBoot.AstBird && Auto || !Auto && Input.Interact.WasPressedThisFrame())) { SetPrompt(null); yield return BirdScene(); yield break; }
                if (DemoBoot.AstBird && Auto && !Riding && Flat(P7 - L.BirdView).magnitude > 1.8f && Flat(P7 - L.BirdView).magnitude < 14f) Seven.ScriptMove = Flat(L.BirdView - P7).normalized * 0.8f;
                yield return null;
            }
        }
        IEnumerator BirdScene()
        {
            yield return Cutscene(true);
            Seven.Place(L.BirdView, Vector3.left);
            var tik = Seven.gameObject.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
            tik.enabled = true; tik.Ease = 3f;
            var hold = P7 + Vector3.up * 1.2f + Seven.transform.forward * 0.36f;
            tik.LookPos = L.BirdAt; tik.LookW = 0.9f;
            Shoot(L.BirdView + new Vector3(-0.2f, 1.55f, 1.6f), L.BirdAt + Vector3.up * 0.1f, 34f, 0.01f);
            yield return Wait(0.5f);
            yield return Shot("a14_the_little_mechanical_bird");
            // she picks it up and turns it carefully in her hands
            tik.RightPos = L.BirdAt; tik.RightW = 1f; tik.LeftPos = L.BirdAt + Vector3.back * 0.06f; tik.LeftW = 0.8f;
            yield return Wait(0.6f);
            Vector3 b0 = Bird != null ? Bird.transform.position : L.BirdAt;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f) { var p = Vector3.Lerp(b0, hold, Mathf.SmoothStep(0f, 1f, t)); if (Bird != null) Bird.transform.position = p; tik.RightPos = p + Vector3.right * 0.03f; tik.LeftPos = p - Vector3.right * 0.05f + Vector3.back * 0.02f; tik.LookPos = p; yield return null; }
            Shoot(hold + Seven.transform.forward * 0.8f + Vector3.up * 0.15f + Seven.transform.right * 0.3f, hold, 30f, 0.01f);
            yield return DialogueFraming.Over("the bird in her hands (QF24 7)", Say("AST_03", 5, 5));   // Look at this. Someone kept fixing it. (over the bird she holds)
            yield return Say("AST_03", 6, 7);                     // Does it still work? / Hang on...
            if (Bird != null) { Bird.transform.rotation = Quaternion.LookRotation(-Seven.transform.forward); yield return Bird.WindAndChirp(); }    // AST_03_08: one uneven chirp, a wing flap
            yield return Shot("a15_the_bird_chirps_and_flaps");
            yield return Say("AST_03", 9, 13);                    // Ha! There you are. / Hm. Very convincing. / You're doing that voice. / What voice? / The one you do when...
            // she places the bird safely back
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f) { var p = Vector3.Lerp(hold, b0, Mathf.SmoothStep(0f, 1f, t)); if (Bird != null) Bird.transform.position = p; tik.RightPos = p + Vector3.right * 0.03f; tik.LeftPos = p - Vector3.right * 0.05f; yield return null; }
            if (Bird != null) Bird.transform.position = b0;
            tik.ReleaseAll("she placed the bird back");
            AsteroidState.SeeBird(); Complete("AST_03_bird");
            if (BirdLamp != null) Glow(BirdLamp, 0.15f);
            yield return Cutscene(false);
        }

        // ------------------------------------------------------------------------------------------ AST_04: the Machinist
        IEnumerator MeetMachinist()
        {
            if (Riding) yield return GetOff();
            FreeBike = false; SetPrompt(null);
            Objective("Follow the temple signal into the central hall");
            yield return GuideWalk(new[] { L.HallDoor + new Vector3(3f, 0f, 0f), L.HallDoor + new Vector3(-6f, 0f, 0f), L.HallMeet + new Vector3(8f, 0f, -4f) }, "into the central hall");
            yield return Until(() => P7.x < -330f || Flat(P7 - L.HallMeet).magnitude < 6f, "inside the hall", 40f, "Walk into the hall", L.HallMeet, () => Seven.Place(L.HallMeet, Vector3.left));
            yield return Cutscene(true);
            Seven.Place(L.HallMeet, Flat(L.CradleAt - L.HallMeet)); Seven.Lock(true);
            // a huge articulated machine unfolds from its repair cradle - LOW ANGLE as he rises
            var m = Machinist;
            AudioKit.Play("ast_servo_heavy", L.CradleAt, 1f, 0f, AudioKit.Bus.Sfx, 80f);
            Shoot(L.CradleAt + new Vector3(-5f, 0.8f, -11.5f), L.CradleAt + Vector3.up * 3.0f, 58f, 0.01f);
            if (m != null) m.Play(m.HasState("roar") ? "roar" : "idle", 0.6f);                 // he unfolds: from slumped in the cradle to his full height
            yield return Wait(1.8f);
            yield return Shot("a16_low_angle_he_rises_from_his_cradle");
            if (m != null) m.Play("idle", 0.4f);
            yield return Wait(0.8f);
            // he stops precisely in front of her; over-the-shoulder behind 7
            var stop = L.HallMeet + Flat(L.CutterAt - L.HallMeet).normalized * 4.2f;
            // QF21 (6): the camera moves with him as he leaves the cradle and walks to her (low, his scale) - never an empty shot
            // QF32 (3) / QF24 (9): a soft white key light on him from her side, riding with him through the walk-up and the talk (his
            // chrome read as one mid-grey against the hall - the a16b / a17 frames' tonal range was 0.21-0.29)
            Light mKey = null;
            if (m != null)
            {
                mKey = new GameObject("Machinist_Key").AddComponent<Light>(); mKey.type = LightType.Point; mKey.shadows = LightShadows.None;
                mKey.range = 10f; mKey.intensity = 3.2f; mKey.color = Color.white;
                StartCoroutine(KeyRides(mKey, m.transform));
            }
            if (m != null) { yield return TrackWalk(m, stop, 1.0f, "a16b_he_walks_to_her", P7); m.Face(P7); }
            OverHerShoulderUp();
            yield return Wait(0.4f);
            yield return Shot("a17_over_her_shoulder_his_scale");
            yield return Line("AST_04_01");                    // You have left the receiving route.
            yield return Line("AST_04_02");                    // Yes. It was taking me somewhere unpleasant.
            yield return Line("AST_04_03");                    // Sorting.
            yield return Line("AST_04_04");                    // Mm-hmm. That was the unpleasant part.
            yield return Line("AST_04_05");                    // State your function.
            yield return Line("AST_04_06");                    // I'm looking for your temple.
            if (m != null) m.Face(L.CutterAt);                 // he turns toward an active assembly
            yield return Line("AST_04_07");                    // Archive access is suspended.
            yield return Line("AST_04_08");                    // Until when?
            yield return Line("AST_04_09");                    // Until restoration is complete.
            yield return Line("AST_04_10");                    // And... how long has that been?
            yield return Wait(0.6f);                           // a brief mechanical hesitation before the answer
            yield return Line("AST_04_11");                    // Restoration is ongoing.
            if (mKey != null) Destroy(mKey.gameObject, 2f);
            // behind him an engraved wall panel moves into the cutting mechanism
            if (CutterPanel != null) StartCoroutine(PanelIntoCutter());
            Shoot(new Vector3(-379f, 3.2f, 113.5f), new Vector3(-379f, 1.2f, L.ArchiveConveyorZ), 48f, 0.5f);
            yield return Wait(1.0f);
            ImageGate.ExpectPoint(new Vector3(-379f, 1.2f, L.ArchiveConveyorZ), "the panel moving into the cutter");          // P2 (gate): what the shot is of
            yield return Shot("a18_a_panel_moves_into_the_cutter");
            // INSERT: the markings match those near the temple signal
            if (CutterPanel != null) { var cpp = new Vector3(-381f, 1.0f, L.ArchiveConveyorZ); Shoot(cpp + new Vector3(2.6f, 0.4f, -0.3f), cpp, 30f, 0.01f); Glow(PanelGlyph, 1.4f); yield return Wait(1.1f); ImageGate.Expect(CutterPanel, "the panel's markings (insert)"); yield return Shot("a19_insert_the_panel_markings"); }
            Shoot(L.TempleSignal + new Vector3(11f, -3.5f, 0f), L.TempleSignal, 30f, 0.01f); Glow(SignalGlyph, 1.6f);
            yield return Wait(1.1f);
            ImageGate.ExpectPoint(L.TempleSignal, "the same markings at the temple signal (insert)");
            yield return Shot("a20_insert_the_same_markings_at_the_temple_signal");
            yield return Line("AST_04_12");                    // Wait. That came from the archive.
            yield return Line("AST_04_13");                    // Unused material is being reassigned.
            yield return Line("AST_04_14");                    // There's writing on it.
            if (m != null) { _talkCalm = false; yield return TrackWalk(m, P7 + Flat(L.CutterAt - P7).normalized * 2.6f, 1.2f, "a20b_he_blocks_her_path", P7, 5f, 6f); m.Face(P7); }   // he blocks her path (QF21 6: followed)
            yield return Line("AST_04_15");                    // There are no outstanding requests to read it.
            yield return Line("AST_04_16");                    // I'm requesting it.
            yield return Line("AST_04_17");                    // Request denied.
            // the security shutters begin closing - not yet the encounter: she gets out through the maintenance opening
            AudioKit.Play("ast_alarm", L.HallMeet, 1f, 0f, AudioKit.Bus.Sfx, 120f);
            AudioKit.Play("ast_shutters", L.HallDoor, 1f, 0f, AudioKit.Bus.Sfx, 120f);
            if (HallDoorShutter != null) StartCoroutine(HallDoorShutter.Move(true, 3.5f));
            if (ArchiveShutter != null) ArchiveShutter.Set(true);
            SetZone("HallDoorZ", false);
            Shoot(L.HallMeet + new Vector3(10f, 5f, -12f), L.HallDoor + Vector3.up * 2f, 56f, 0.3f);
            yield return Wait(1.0f);
            yield return Shot("a21_the_security_shutters_close");
            yield return SayIds("AST_04_P1");                  // Seven. The maintenance opening, on your right. Now.
            Complete("AST_04_lines");
            _talkCalm = false;
            yield return Cutscene(false);
            yield return Escape();
            Complete("AST_04");
        }
        IEnumerator Line(string id) { yield return Frame(id); yield return SayIds(id); }
        Vector3 HeadOfM() { var an = Machinist != null ? Machinist.Anim : null; var h = an != null && an.isHuman ? an.GetBoneTransform(HumanBodyBones.Head) : null; return h != null ? h.position : (Machinist != null ? Machinist.transform.position : L.CradleAt) + Vector3.up * 3.7f; }
        Vector3 HerHead => Seven.Bone(HumanBodyBones.Head) != null ? Seven.Bone(HumanBodyBones.Head).position : P7 + Vector3.up * 1.6f;
        void MachinistUp()
        {
            if (Machinist == null) return;
            var h = HeadOfM(); var toHer = Flat(P7 - Machinist.transform.position).normalized; if (toHer.sqrMagnitude < 0.01f) toHer = Machinist.transform.forward;
            foreach (var a in new[] { 20f, -20f, 40f, -40f, 0f })
            {
                var d = Quaternion.Euler(0f, a, 0f) * toHer;
                var cam = Machinist.transform.position + d * 5.2f + Vector3.up * 1.0f;      // low: his scale carries it
                var look = h + Vector3.down * 0.6f;
                if (RideProbe.Overlap(cam, 0.25f, null, null, false) || !ChapterCamera.ClearLine(cam, look)) continue;
                Shoot(cam, look, 46f, 0.01f); return;
            }
            Shoot(Machinist.transform.position + toHer * 6f + Vector3.up * 1.2f, h, 46f, 0.01f);
        }
        void OverHerShoulderUp()
        {
            var at = HeadOfM();
            var hh = HerHead; var d = Flat(at - hh).normalized; var side = Vector3.Cross(Vector3.up, d);
            foreach (var s in new[] { 1f, -1f })
            {
                var cam = hh - d * 1.4f + side * 0.6f * s + Vector3.down * 0.1f;
                if (RideProbe.Overlap(cam, 0.2f, null, null, false) || !ChapterCamera.ClearLine(cam, at)) continue;
                Shoot(cam, (at + hh) * 0.5f + Vector3.up * 0.4f, 50f, 0.01f); return;
            }
            Shoot(hh - d * 2f + Vector3.up * 0.3f, at, 50f, 0.01f);
        }
        /// <summary>QF32 (3): a key light that stays on a big character's front, from her side and above his chest, while it lives.</summary>
        IEnumerator KeyRides(Light key, Transform who)
        {
            while (key != null && who != null)
            {
                var toHer = Flat(P7 - who.position); if (toHer.sqrMagnitude < 0.01f) toHer = who.forward; toHer.Normalize();
                key.transform.position = who.position + Vector3.up * 3.4f + toHer * 2.6f + Vector3.Cross(Vector3.up, toHer) * 1.2f;
                yield return null;
            }
        }
        IEnumerator PanelIntoCutter()
        {
            var a = CutterPanel.position; var b = L.CutterAt + new Vector3(0f, 1.0f, 0f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 5f) { CutterPanel.position = Vector3.Lerp(a, b, t); yield return null; }
            AudioKit.Play("ast_cutter", L.CutterAt, 1f, 0f, AudioKit.Bus.Sfx, 60f);
            Sparks(L.CutterAt + Vector3.up * 1.4f, 120);
            CutterPanel.gameObject.SetActive(false);
        }
        /// <summary>AST_04's escape: a short tense run - the maintenance opening's shutter comes down; out before it shuts.
        /// Too slow: a white fade, back to where it began (a fair retry, no fail screen).</summary>
        IEnumerator Escape()
        {
            Objective("Get out through the maintenance opening before the hall seals");
            float secs = 22f; Coroutine hc = null;
            for (int attempt = 1; ; attempt++)
            {
                Seven.Lock(false);
                if (HatchShutter != null) { HatchShutter.Set(false); hc = StartCoroutine(HatchShutter.Move(true, secs)); }
                if (Machinist != null && attempt == 1) { Machinist.WalkTo(L.CutterAt + new Vector3(4.5f, 0f, -3f), 1.0f); }      // back to his work - the shutters do the rest
                ShowGuide(new Vector3(-328f, 0f, 63.5f));                // the light is through the opening, in the passage
                if (Auto) { Seven.ScriptRun = true; StartCoroutine(WalkPath(new[] { new Vector3(-328f, 0f, 76f), new Vector3(-328f, 0f, 64f) }, secs)); }
                float t0 = Time.time; bool out_ = false;
                while (Time.time - t0 < secs + 0.2f) { if (P7.z < 69.0f && P7.x > -331f && P7.x < -325f) { out_ = true; break; } if (P7.z < 62f) { out_ = true; break; } yield return null; }
                HideGuide();
                if (out_) { Seven.ScriptRun = false; Log($"AST_04: out through the maintenance opening in {Time.time - t0:0.0} s (try {attempt}; the shutter takes {secs:0} s)"); break; }
                Log($"AST_04: the opening shut before she was through (try {attempt}) - back to the meeting spot, a little more time");
                yield return UI.Fade(1f, 0.4f, 1f);
                Seven.Place(L.HallMeet + new Vector3(4f, 0f, -12f), Vector3.back); secs += 4f;
                yield return UI.Fade(0f, 0.5f, 1f);
            }
            // she is through: the shutter comes down behind her once she is clear of the opening itself (its zone closes with it)
            for (float t = 0f; t < 3f && P7.z > 65f; t += Time.deltaTime) yield return null;
            if (P7.z > 65f) Seven.Place(new Vector3(-328f, 0f, 64.2f), Vector3.back);
            HideGuide();
            if (hc != null) StopCoroutine(hc);
            if (HatchShutter != null) yield return HatchShutter.Move(true, 0.6f);
            SetZone("MaintHatchZ", false);
            yield return Shot("a22_the_opening_shuts_behind_her");
        }
    }
}
