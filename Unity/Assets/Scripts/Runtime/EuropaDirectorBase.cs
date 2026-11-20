using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    public partial class EuropaDirector
    {
        // ------------------------------------------------------------------------------------------ 6: what the machinery is doing
        IEnumerator Machinery()
        {
            if (EuropaState.SceneDone("EUR_06")) yield break;
            Objective("Follow the keeper");
            if (Keeper != null) Keeper.GlideTo(EuropaLayout.DamagedPassage + new Vector3(-6f, 4f, -8f), 4.5f);
            ShowGuide(EuropaLayout.DamagedPassage + new Vector3(-12f, 0f, -14f));
            if (Auto) StartCoroutine(AutoSwim(new[] { EuropaLayout.DamagedPassage + new Vector3(-12f, 0f, -14f) }, 4f));
            yield return Until(() => Vector3.Distance(Bike.transform.position, EuropaLayout.DamagedPassage) < 22f, "the damaged migration route", 90f,
                "Follow the keeper toward the damaged passage", EuropaLayout.DamagedPassage, () => PlaceRiding(EuropaLayout.DamagedPassage + new Vector3(-12f, 0f, -14f)));
            HideGuide();
            yield return StopBike();
            yield return Cutscene(true);
            // over 7's shoulder: shattered supports, suspended debris, the sealed passage, creatures turning from the noise
            var to = Flat(EuropaLayout.DamagedPassage - Bike.transform.position).normalized; var r = Vector3.Cross(Vector3.up, to);
            Shoot(Bike.transform.position - to * 2.6f + r * 1.1f + Vector3.up * 2.0f, EuropaLayout.DamagedPassage, 46f, 0.01f);
            if (Pod != null && Pod.Length > 0 && Pod[0] != null) { Pod[0].transform.position = EuropaLayout.DamagedPassage + new Vector3(4f, 6f, 4f); Pod[0].GlideTo(EuropaLayout.DamagedPassage + new Vector3(30f, 10f, -30f), 5f); }
            yield return Wait(0.8f);
            yield return Shot("e11_damaged_passage_over_her_shoulder");
            yield return Say("EUR_06", 1, 1);                   // Our young travel through here. Our calls guide them.
            // another pulse overwhelms a distant call
            AudioKit.Play("europa_call_answer", EuropaLayout.DamagedPassage + Vector3.forward * 60f, 0.5f, 0f, AudioKit.Bus.Sfx, 500f);
            yield return Wait(0.8f);
            AudioKit.Play("europa_extraction_pulse", null, 1f); CameraShake.Quake(0.7f, 0.5f);
            yield return Wait(1.2f);
            yield return Say("EUR_06", 2, 8);                   // Now they cannot hear us. ... Oh. Him. ... We know the name ...
            // the keeper turns toward a distant industrial structure embedded beneath the ice
            if (Keeper != null) Keeper.GlideTo(Keeper.transform.position + (EuropaLayout.Approach[3] - Keeper.transform.position).normalized * 4f, 1.5f);
            Shoot(Bike.transform.position + Vector3.up * 2.5f - to * 3f, EuropaLayout.Approach[4], 40f, 1.2f);
            yield return Wait(1.0f);
            yield return Shot("e12_the_distant_industrial_structure");
            yield return Say("EUR_06", 9, 12);                  // Below the old shelf ... The Scree? / Yes. / Show me the way in.
            Complete("EUR_06");
            yield return Cutscene(false);
            Objective("Investigate Tunneler's underwater base");
        }

        // ------------------------------------------------------------------------------------------ 7: approach - quiet observation
        IEnumerator Approach()
        {
            Objective("Investigate Tunneler's underwater base");
            if (!EuropaState.SceneDone("EUR_07"))
            {
                // the playful course geometry gives way to pipes, intake channels, industrial tunnels
                var leg1 = new[] { EuropaLayout.Approach[0], EuropaLayout.Approach[1], EuropaLayout.Approach[2], EuropaLayout.Approach[3], EuropaLayout.ObservePoint };
                yield return RideRoute(leg1, "the industrial approach", 9f);
                MusicSystem.Play("europa_base", 3f);                         // the base's cover of the underwater theme
                if (_drill == null) _drill = AudioKit.Loop("europa_drill", null, EuropaLayout.Approach[5], 0.6f, AudioKit.Bus.Sfx, 6f, 120f);
                yield return StopBike();
                yield return Cutscene(true);
                // she observes: Scree workers labouring near the equipment - fatigue, coercion
                Shoot(EuropaLayout.ObservePoint + new Vector3(-3f, 2.5f, -6f), EuropaLayout.Approach[5] + Vector3.up * 1f, 44f, 0.01f);
                yield return Wait(1.0f);
                yield return Shot("e13_scree_workers_labouring");
                yield return Say("EUR_07");
                Complete("EUR_07");
                yield return Cutscene(false);
            }
            Objective("Park in the access bay");
            yield return RideRoute(new[] { EuropaLayout.Approach[4], EuropaLayout.Approach[5], EuropaLayout.BayPark }, "the access bay", 4f);
            yield return StopBike(1.5f);
            // she parks in the sheltered access bay and goes in on foot (X)
            bool off = false;
            IEnumerator Prompt() { while (!off) { SetPrompt("Get off"); if (Auto || Input.Interact.WasPressedThisFrame()) off = true; yield return null; } SetPrompt(null); }
            StartCoroutine(Prompt());
            yield return Until(() => off, "get off at the dock", 30f, "X: get off onto the dock", EuropaLayout.DockStep, () => off = true);
            off = true;
            StopAutoSwim(); Bike.ScriptDriven = false;
            Bike.Place(EuropaLayout.BayPark, 270f);
            Bike.DismountTo(EuropaLayout.DockStep, Vector3.forward);
            FollowCamera(); Cam.SnapBehind();
            yield return Shot("e14_parked_in_the_access_bay");
            // the pressure chamber: the door cycles (hiss, locks, water drains); the music turns fully to the base
            Objective("Go in through the pressure chamber");
            if (Auto) StartCoroutine(WalkSeven(EuropaLayout.PressureIn, 12f)); ShowGuide(EuropaLayout.PressureIn + Vector3.up);
            yield return Until(() => P7.z > EuropaLayout.PressureIn.z - 0.5f, "into the pressure chamber", 40f, "Walk into the pressure chamber", EuropaLayout.PressureIn, () => Seven.Place(EuropaLayout.PressureIn, Vector3.forward));
            yield return Cutscene(true);
            AudioKit.Play("europa_pressure_door", P7, 0.9f);
            Shoot(P7 + new Vector3(1.8f, 1.6f, 2.6f), P7 + Vector3.up * 1.1f, 48f, 0.01f);
            yield return Wait(2.2f);
            yield return Shot("e15_pressure_chamber");
            yield return Cutscene(false);
            Log("pressure chamber cycled: inside the base on foot (helmet kept on - the air here isn't hers)");
        }

        // ------------------------------------------------------------------------------------------ 8: first contact with Tunneler
        IEnumerator BaseContact()
        {
            Objective("Find the workers");
            if (Shots) StartCoroutine(WalkInShots());
            if (Auto) StartCoroutine(WalkRoute(EuropaLayout.ControlSpot, 30f)); StartCoroutine(GuideTo(EuropaLayout.ControlSpot, () => P7.z > 481.5f));
            yield return Until(() => P7.z > 481.5f, "into the control area", 60f, "Follow the corridor to the control room", EuropaLayout.ControlSpot, () => Seven.Place(EuropaLayout.ControlSpot, Vector3.forward));
            yield return Cutscene(true);
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(EuropaLayout.ScreenAt - P7)), 300f * Time.deltaTime); yield return null; }
            // low-angle: Tunneler appears on the industrial display, machinery lighting his shape from below
            if (Tunneler != null) { Tunneler.gameObject.SetActive(true); }
            if (ScreenCam != null) ScreenCam.enabled = true;
            if (Screen != null) Screen.enabled = true;
            Shoot(P7 + Vector3.up * 0.6f - Flat(EuropaLayout.ScreenAt - P7).normalized * 1.4f + Seven.transform.right * 0.7f, EuropaLayout.ScreenAt, 52f, 0.01f);
            yield return Wait(0.6f);
            yield return Say("EUR_08", 1, 1);                   // You.
            yield return Shot("e16_tunneler_on_the_screen_low_angle");
            // medium on 7: she stops, then looks directly at the display
            var hd = Seven.Bone(HumanBodyBones.Head); var hp = hd != null ? hd.position : P7 + Vector3.up * 1.6f;
            Shoot(hp + Flat(EuropaLayout.ScreenAt - P7).normalized * 1.6f + Seven.transform.right * 0.5f, hp - Vector3.up * 0.15f, 40f, 0.01f);
            yield return Say("EUR_08", 2, 9);
            // he leans closer to the camera
            Shoot(EuropaLayout.ScreenAt - Vector3.forward * 2.2f + Vector3.down * 0.4f, EuropaLayout.ScreenAt, 34f, 0.6f);
            yield return Say("EUR_08", 10, 11);
            // the screen goes dark; security barriers close
            if (Screen != null) Screen.enabled = false;
            if (ScreenCam != null) ScreenCam.enabled = false;
            if (Tunneler != null) Tunneler.gameObject.SetActive(false);
            var alarm = AudioKit.Loop("europa_alarm", null, P7, 0.5f, AudioKit.Bus.Sfx, 4f, 60f);
            if (Barriers != null) foreach (var b in Barriers) if (b != null) StartCoroutine(Drop(b));
            SetZone("Corridor", false);                         // the way back is barred for now (it reopens when the workers are out)
            Shoot(EuropaLayout.ControlSpot + new Vector3(3.5f, 1.9f, 4.5f), new Vector3(-181f, EuropaLayout.BaseY + 1.2f, 479.6f), 56f, 0.01f);
            yield return Wait(1.6f);
            yield return Shot("e17_security_barriers_close");
            if (alarm != null) StartCoroutine(AudioKit.Fade(alarm, 0f, 3f, true));
            Complete("EUR_08");
            Log($"Tunneler's drill (QF13 11): spun continuously while he spoke - {DrillTurns} full turns, top speed {DrillMaxSpeed:0} deg/s, spinning down when he stopped (never twisting back)");
            yield return Cutscene(false);
            // reach the worker holding area: the override panel beside the barred door (no guard is ever harmed)
            Objective("Reach the worker holding area");
            yield return Interact(EuropaLayout.HoldingDoor + new Vector3(1.2f, 0f, -1.6f), "Override the lock", "the holding-area door", "The barred door on the west wall: X at its panel");
            if (DoorHolding != null) StartCoroutine(Drop(DoorHolding, true));
            SetZone("HoldingDoor", true);
            AudioKit.Play("europa_pressure_door", EuropaLayout.HoldingDoor, 0.7f);
            yield return Wait(0.6f);
        }
        /// <summary>QF13 (7): the walk in, from the gameplay camera - the dock, the pressure chamber, the corridor, the
        /// control room door - one capture every 1.2 s with the cut-away / blocked state.</summary>
        IEnumerator WalkInShots()
        {
            for (int i = 0; i < 14 && P7.z < 482f; i++)
            {
                yield return Wait(1.2f);
                if (_cut) continue;
                Log($"walk in (QF13 7) #{i}: 7 at {P7:0.0}, camera {Cam.transform.position:0.0} ({Vector3.Distance(Cam.transform.position, P7 + Vector3.up * 1.45f):0.0} m from her head), walls cut away now {(_baseWalls != null ? System.Linq.Enumerable.Count(_baseWalls, r => r != null && r.forceRenderingOff) : 0)}");
                yield return Shot($"qf13_07_walk_in_{i:00}");
            }
        }

        IEnumerator Drop(GameObject g, bool open = false)
        {
            if (!open) { g.SetActive(true); var p1 = g.transform.position; var p0 = p1 + Vector3.up * 3f; for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f) { g.transform.position = Vector3.Lerp(p0, p1, t * t); yield return null; } g.transform.position = p1; }
            else { var p0 = g.transform.position; for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f) { g.transform.position = p0 + Vector3.up * 3.2f * Mathf.SmoothStep(0f, 1f, t); yield return null; } g.SetActive(false); g.transform.position = p0; }
        }

        // ------------------------------------------------------------------------------------------ 9-10: freeing the Scree, the devices
        IEnumerator Workers()
        {
            if (!EuropaState.SceneDone("EUR_09"))
            {
                Objective("Free the workers");
                if (Auto) StartCoroutine(WalkRoute(EuropaLayout.HoldingSpot + Vector3.right * 2.5f, 20f)); StartCoroutine(GuideTo(EuropaLayout.HoldingSpot, () => P7.x < -193.5f));
                yield return Until(() => P7.x < -193.5f, "into the holding area", 60f, "Through the opened door, west", EuropaLayout.HoldingSpot, () => Seven.Place(EuropaLayout.HoldingSpot + Vector3.right * 2.5f, Vector3.left));
                yield return Cutscene(true);
                // she opens the secured passage; a worker hesitates at the threshold; she steps aside, leaving the doorway clear
                var w0 = Scree != null && Scree.Length > 0 ? Scree[0] : null;
                if (w0 != null) { w0.position = EuropaLayout.HoldingScreeAt; w0.rotation = Quaternion.LookRotation(Flat(P7 - w0.position)); }
                Shoot(P7 + new Vector3(-1.5f, 1.6f, 3.4f), (P7 + (w0 != null ? w0.position : P7)) * 0.5f + Vector3.up * 1.2f, 50f, 0.01f);
                yield return Wait(0.7f);
                yield return Shot("e18_worker_at_the_threshold");
                yield return Say("EUR_09", 1, 1);                // Where are we being taken?
                yield return WalkSeven(P7 + Vector3.forward * 1.3f, 2f);   // she steps aside
                Seven.Lock(true);
                yield return Say("EUR_09", 2, 4);                // Out. / All of us? / All of you.
                yield return Cutscene(false);
                // gameplay with agency: one worker opens the transport access, she restores power to the evacuation lift
                Objective("Restore power to the evacuation lift");
                StartCoroutine(WorkerOpensTransport());
                yield return Interact(EuropaLayout.LiftPanel + new Vector3(0f, 0f, -1.1f), "Restore power", "the evacuation lift's power", "The lift's power panel is in the transport dock (north wall)");
                if (BasePanels != null && BasePanels.Length > 1) Glow(BasePanels[1], 1.6f);
                AudioKit.Play("energy_point", EuropaLayout.LiftPanel, 0.7f);
                Log("evacuation lift powered; the workers walk to the transports (away from the extraction machinery)");
                StartCoroutine(WorkersToTransports());
                // a worker shows her the schematic
                Objective("Talk to the worker at the schematic");
                if (Scree != null && Scree.Length > 1 && Scree[1] != null) { Scree[1].position = EuropaLayout.SchematicAt + new Vector3(1.2f, 0f, -1f); Scree[1].rotation = Quaternion.LookRotation(Vector3.forward); }
                yield return Interact(EuropaLayout.SchematicAt + new Vector3(-0.8f, 0f, -1.2f), "Look", "the schematic", "A worker waits at the wall schematic in the holding area");
                yield return Cutscene(true);
                if (BasePanels != null && BasePanels.Length > 2) Glow(BasePanels[2], 1.4f);
                Shoot(EuropaLayout.SchematicAt + new Vector3(-1.4f, 0.4f, -2.4f), EuropaLayout.SchematicAt + Vector3.up * 1.4f, 44f, 0.01f);
                yield return Wait(0.6f);
                yield return Shot("e19_the_schematic");
                yield return Say("EUR_09", 5, 8);
                Complete("EUR_09");
                yield return Cutscene(false);
            }
            // section 10: the demolition devices (abstract), detonation only once everyone is clear - no timer
            Objective($"Place the demolition devices ({EuropaState.Devices.Count}/3)");
            if (DoorMachine != null && DoorMachine.activeSelf) { if (Scree != null && Scree.Length > 2 && Scree[2] != null) Scree[2].position = EuropaLayout.MachineDoor + new Vector3(-1.4f, 0f, -1.5f); yield return Drop(DoorMachine, true); Log("a worker opens the machinery section for her (agency)"); }
            SetZone("MachineDoor", true);
            var panther = StartCoroutine(PantherTracksTransports());
            for (int i = 0; i < 3; i++)
            {
                if (EuropaState.HasDevice("dev_" + i)) continue;
                var spot = EuropaLayout.DeviceSpots[i];
                yield return Interact(spot + Flat(EuropaLayout.MachineDoor - spot).normalized * 0.9f, "Place the device", $"demolition device {i + 1}", "The machinery connections glow faintly: X at each one");
                Seven.Touch(spot + Vector3.up * 1.0f);
                yield return Wait(0.5f);
                if (DeviceSlots != null && i < DeviceSlots.Length && DeviceSlots[i] != null) DeviceSlots[i].gameObject.SetActive(true);
                AudioKit.Play("europa_device_place", spot, 0.8f);
                EuropaState.PlaceDevice("dev_" + i);
                Seven.Release();
                Objective($"Place the demolition devices ({EuropaState.Devices.Count}/3)");
                Log($"demolition device {i + 1} placed (saved)");
                if (i == 2)
                {
                    // insert: her hand on the last device - "Ready here."
                    yield return Cutscene(true);
                    var hand = Seven.Bone(HumanBodyBones.RightHand);
                    var hp = hand != null ? hand.position : spot + Vector3.up;
                    Shoot(hp + Vector3.up * 0.4f + Flat(P7 - spot).normalized * 0.5f + Seven.transform.right * 0.4f, hp, 30f, 0.01f);
                    yield return Wait(0.5f);
                    yield return Shot("e20_insert_hand_on_the_last_device");
                    yield return Cutscene(false);
                }
            }
            Seven.Release();
            yield return WaitForTransports();
            if (panther != null) StopCoroutine(panther);
            Complete("EUR_10");
            // the way back opens (the barriers lift): the evacuation is done
            if (Barriers != null) foreach (var b in Barriers) if (b != null && b.activeSelf) StartCoroutine(Drop(b, true));
            SetZone("Corridor", true); SetZone("WindowHall", true);
        }

        IEnumerator WorkerOpensTransport()
        {
            var w = Scree != null && Scree.Length > 2 ? Scree[2] : null;
            if (w != null)
            {
                Anim(w, "walk");
                var to = EuropaLayout.TransportPanel + Vector3.right * 0.9f;
                for (float t0 = Time.time; Flat(to - w.position).magnitude > 0.3f && Time.time - t0 < 12f;) { var d = Flat(to - w.position); w.position += d.normalized * Mathf.Min(d.magnitude, 1.1f * Time.deltaTime); w.rotation = Quaternion.RotateTowards(w.rotation, Quaternion.LookRotation(d), 300f * Time.deltaTime); yield return null; }
                Anim(w, "work");
                yield return Wait(1.2f);
            }
            if (BasePanels != null && BasePanels.Length > 0) Glow(BasePanels[0], 1.6f);
            if (DoorTransport != null && DoorTransport.activeSelf) StartCoroutine(Drop(DoorTransport, true));
            SetZone("TransportDoor", true);
            AudioKit.Play("europa_pressure_door", EuropaLayout.TransportPanel, 0.6f);
            Log("a worker opens the transport access himself (agency, section 9)");
        }

        IEnumerator WorkersToTransports()
        {
            if (Scree == null) yield break;
            for (int i = 0; i < Scree.Length; i++)
            {
                var w = Scree[i]; if (w == null || i == 1) continue;   // the schematic worker comes later
                StartCoroutine(WalkWorker(w, EuropaLayout.TransportDock + new Vector3(-1.5f * (i % 3), 0f, -2f + 1.5f * (i / 3)), 1.0f));
                yield return Wait(0.4f);
            }
        }
        IEnumerator WalkWorker(Transform w, Vector3 to, float speed)
        {
            Anim(w, "walk");
            for (float t0 = Time.time; Flat(to - w.position).magnitude > 0.3f && Time.time - t0 < 30f;)
            {
                var d = Flat(to - w.position);
                w.position += d.normalized * Mathf.Min(d.magnitude, speed * Time.deltaTime);
                if (d.sqrMagnitude > 1e-3f) w.rotation = Quaternion.RotateTowards(w.rotation, Quaternion.LookRotation(d), 300f * Time.deltaTime);
                yield return null;
            }
            Anim(w, "idle");
        }
        void Anim(Transform w, string state)
        {
            var a = w != null ? w.GetComponentInChildren<Animator>() : null;
            if (a != null && a.HasState(0, Animator.StringToHash(state))) a.CrossFadeInFixedTime(state, 0.25f);
        }

        int _transportsAway;
        IEnumerator PantherTracksTransports()
        {
            // Panther follows the departing transports through the saucer's instruments
            yield return Wait(6f);
            DepartTransport(0);
            yield return Say("EUR_10", 1, 3);                   // First transport is away. / And the others? / Still boarding.
        }
        void DepartTransport(int i)
        {
            if (Transports == null || i >= Transports.Length || Transports[i] == null || !Transports[i].gameObject.activeSelf) return;
            _transportsAway = Mathf.Max(_transportsAway, i + 1);
            StartCoroutine(MoveAway(Transports[i], Vector3.left * 60f, 8f));
            if (Scree != null) foreach (var s in Scree) if (s != null && s.gameObject.activeSelf && Flat(s.position - EuropaLayout.TransportDock).magnitude < 6f && UnityEngine.Random.value < 0.6f) s.gameObject.SetActive(false);
            Log($"transport {i + 1} away");
        }
        IEnumerator MoveAway(Transform t, Vector3 by, float seconds)
        {
            var p0 = t.position;
            for (float u = 0f; u < 1f; u += Time.deltaTime / seconds) { t.position = p0 + by * (u * u); yield return null; }
            t.gameObject.SetActive(false);
        }
        IEnumerator WaitForTransports()
        {
            // she has reached the final objective: "Ready here." / "Then wait." - the alarm; tension, no timer; the last ones go
            yield return Say("EUR_10", 4, 5);
            var alarm = AudioKit.Loop("europa_alarm", null, P7, 0.45f, AudioKit.Bus.Sfx, 4f, 60f);
            Objective("Wait until everyone is clear");
            Shoot(P7 + new Vector3(3f, 2.2f, -3f), EuropaLayout.MachineDoor + Vector3.up * 1.2f, 50f, 1.0f);   // she looks toward the access tunnel
            yield return Wait(2.5f);
            for (int i = 1; Transports != null && i < Transports.Length; i++) { DepartTransport(i); yield return Wait(2.2f); }
            if (Scree != null) foreach (var s in Scree) if (s != null) s.gameObject.SetActive(false);
            yield return Say("EUR_10", 6, 6);                   // Last transport moving.
            yield return Wait(1.4f);                            // a pause
            yield return Say("EUR_10", 7, 7);                   // They're clear. (a relieved breath)
            if (alarm != null) StartCoroutine(AudioKit.Fade(alarm, 0.15f, 2f));
            _alarm = alarm;
            Log($"evacuation: every transport away ({_transportsAway}); workers in the base 0; detonation now available");
            FollowCamera();
        }
        AudioSource _alarm;

        // ------------------------------------------------------------------------------------------ 11-13: Tunneler escapes, the escape, after the noise
        IEnumerator EscapeAll()
        {
            if (!EuropaState.SceneDone("EUR_11"))
            {
                Objective("Return to the bike");
                if (Auto) StartCoroutine(WalkRoute(EuropaLayout.ControlSpot, 40f)); StartCoroutine(GuideTo(EuropaLayout.ControlSpot, () => _cut || (P7.x > -191f && P7.x < -171f && P7.z > 481f && P7.z < 493f)));
                yield return Until(() => P7.x > -191f && P7.x < -171f && P7.z > 481f && P7.z < 493f, "back through the control room", 90f, "Back east through the holding area to the control room", EuropaLayout.ControlSpot, () => Seven.Place(EuropaLayout.ControlSpot, Vector3.forward));
                yield return Cutscene(true);
                // wide, separated docking chambers: across the sealed observation window he enters his escape craft
                if (WindowShutter != null) StartCoroutine(Drop(WindowShutter, true));
                if (Tunneler != null) { Tunneler.gameObject.SetActive(true); Tunneler.position = EuropaLayout.TunnelerDock + new Vector3(-3f, 0f, 0f); Tunneler.rotation = Quaternion.LookRotation(Vector3.back); }
                Seven.Place(EuropaLayout.WindowSpot - Vector3.forward * 1.2f, Vector3.forward); Seven.Lock(true);
                Shoot(new Vector3(-182.2f, EuropaLayout.BaseY + 1.7f, 499.5f), EuropaLayout.TunnelerDock + Vector3.up * 1.3f, 48f, 0.01f);   // over her shoulder through the glass
                yield return Wait(0.8f);
                yield return Shot("e21_tunneler_across_the_window");
                yield return Say("EUR_11", 1, 4);
                // his craft powers up and leaves through its own passage
                if (TunnelerCraft != null)
                {
                    if (Tunneler != null) Tunneler.gameObject.SetActive(false);
                    AudioKit.Play("bike_liftoff", EuropaLayout.TunnelerDock, 0.8f, 0f, AudioKit.Bus.Sfx, 60f);
                    StartCoroutine(MoveAway(TunnelerCraft, Vector3.forward * 45f + Vector3.up * 4f, 5f));
                }
                yield return Say("EUR_11", 5, 5);                // Let him go. Your exit is the other way.
                yield return Shot("e22_the_craft_departs");
                Complete("EUR_11");
                yield return Cutscene(false);
                Log("Tunneler escapes (not a boss fight): he survives and will remember this");
            }
            // back to the bike (the dock)
            if (!EuropaState.SceneDone("EUR_12"))
            {
                Objective("Return to the bike");
                if (!Bike.Riding)
                {
                    if (Auto) StartCoroutine(WalkRoute(EuropaLayout.DockStep, 60f)); StartCoroutine(GuideTo(EuropaLayout.DockStep, () => Bike.Riding || Flat(EuropaLayout.DockStep - P7).magnitude < 2.2f));
                    bool on = false;
                    IEnumerator Prompt() { while (!on) { bool near = Flat(EuropaLayout.DockStep - P7).magnitude < 2.2f; SetPrompt(near ? "Ride" : null); if (near && (Auto || Input.Interact.WasPressedThisFrame())) on = true; yield return null; } SetPrompt(null); }
                    StartCoroutine(Prompt());
                    yield return Until(() => on, "back on the bike", 90f, "Back south down the corridor and through the pressure chamber to the dock: X to ride", EuropaLayout.DockStep, () => { Seven.Place(EuropaLayout.DockStep, Vector3.back); on = true; });
                    on = true;
                    Bike.Place(EuropaLayout.BayPark, 200f); Bike.Mount(); FollowCamera();
                }
                yield return Cutscene(true);
                { var g = Bike.GripR.position; Shoot(g + Bike.transform.right * 0.9f + Vector3.up * 0.35f + Bike.transform.forward * 0.5f, g, 32f, 0.01f); }
                yield return Wait(0.6f);
                yield return Shot("e23_insert_hands_settle_into_the_grips");
                yield return Say("EUR_12", 1, 4);                // Panther? / I'm here. / Everyone clear? / Everyone we brought out...
                // QF13 (14): the detonation is SHOWN - she triggers it; a wide shot of the base: the first blast erupts (a huge
                // white flash, a rolling boil of bubbles and debris, the structure buckling), the shockwave hits the camera
                if (_alarm != null) StartCoroutine(AudioKit.Fade(_alarm, 0f, 1f, true));
                if (_drill != null) { StartCoroutine(AudioKit.Fade(_drill, 0f, 1.5f, true)); _drill = null; }
                yield return Detonation();
                yield return Cutscene(false);
                yield return EscapeRide();
                Complete("EUR_12");
            }
            if (!EuropaState.SceneDone("EUR_13")) yield return AfterTheNoise();
        }
        IEnumerator Flash(Light l, float delay)
        {
            if (l == null) yield break;
            yield return Wait(delay);
            float i0 = l.intensity;
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { l.intensity = i0 + 6f * (1f - t / 0.5f); yield return null; }
            l.intensity = 0f;                                   // power fails in that section
        }

        public Transform EscapeBlast; public Transform[] EscapeDebris;
        static Vector3 BaseCore => EuropaLayout.Approach[6] + Vector3.up * 2f;          // where the first blast erupts (the extraction structure at the bay)
        /// <summary>QF13 (14): the first blast, seen from outside the base.</summary>
        IEnumerator Detonation()
        {
            // the wide down the industrial approach tube (on its axis, so its walls never block the view), ~40 m from the bay
            var camAt = Vector3.Lerp(EuropaLayout.Approach[4], EuropaLayout.Approach[6], 0.7f) + Vector3.up * 3.5f;
            Shoot(camAt, BaseCore + Vector3.up * 2f, 54f, 0.01f);
            yield return Wait(0.6f);
            yield return Shot("qf13_14a_the_base_before_the_blast");
            AudioKit.Play("europa_demolition", null, 1f);
            for (int i = 0; BaseLights != null && i < BaseLights.Length; i++) StartCoroutine(Flash(BaseLights[i], i * 0.12f));
            var bl = EscapeBlast; Light bLight = bl != null ? bl.GetComponentInChildren<Light>(true) : null;
            if (bl != null) { bl.gameObject.SetActive(true); bl.position = BaseCore; bl.localScale = Vector3.one * 0.2f; }
            var buckle = new List<(Transform t, Vector3 p, Quaternion r)>();
            foreach (var n in new[] { "WorkPlatform", "PipesAndIntakes" }) { var g = GameObject.Find(n); if (g != null) buckle.Add((g.transform, g.transform.position, g.transform.rotation)); }
            StartCoroutine(UI.Fade(0.85f, 0.08f, 1f));
            if (Ruptures != null) { Ruptures.transform.position = BaseCore; Ruptures.Emit(500); }
            ThrowDebris(BaseCore, 22f, 16);
            bool shot = false, hit = false;
            for (float t = 0f; t < 2.6f; t += Time.deltaTime)
            {
                float u = Mathf.Clamp01(t / 1.6f);
                if (bl != null) { bl.localScale = Vector3.one * Mathf.Lerp(0.2f, 2.8f, Mathf.SmoothStep(0f, 1f, u)); bl.rotation = Quaternion.LookRotation(bl.position - Cam.transform.position); }
                if (bLight != null) bLight.intensity = Mathf.Lerp(30f, 6f, u);
                if (t > 0.12f && t - Time.deltaTime <= 0.12f) StartCoroutine(UI.Fade(0f, 0.6f, 1f));
                foreach (var b in buckle) b.t.SetPositionAndRotation(b.p + Vector3.down * 2.5f * u * u, b.r * Quaternion.Euler(6f * u, 0f, -9f * u));
                if (Ruptures != null && Mathf.Repeat(t, 0.15f) < Time.deltaTime) { Ruptures.transform.position = BaseCore + UnityEngine.Random.insideUnitSphere * 10f; Ruptures.Emit(60); }
                if (!shot && t > 0.5f) { shot = true; StartCoroutine(Shot("qf13_14b_the_blast_erupts")); }
                if (!hit && t > 1.2f) { hit = true; AudioKit.Play("europa_pressure_wave", null, 0.9f); CameraShake.Quake(1.6f, 1.1f); Cam.Shot(camAt + (camAt - BaseCore).normalized * 3f, BaseCore + Vector3.up * 2f, 0.25f, false, 58f); StartCoroutine(Shot("qf13_14c_the_shockwave_hits")); }
                yield return null;
            }
            if (bl != null) bl.gameObject.SetActive(false);
            Log("escape (QF13 14): the detonation shown - wide on the base, the blast (white flash, boil of bubbles + debris), the structure buckling, the shockwave on the camera");
        }
        void ThrowDebris(Vector3 at, float speed, int n)
        {
            if (EscapeDebris == null) return;
            for (int i = 0; i < Mathf.Min(n, EscapeDebris.Length); i++) { var d = EscapeDebris[(i + _debrisNext) % EscapeDebris.Length]; if (d != null) StartCoroutine(Fly(d, at + UnityEngine.Random.insideUnitSphere * 3f, UnityEngine.Random.onUnitSphere * speed * UnityEngine.Random.Range(0.4f, 1f))); }
            _debrisNext += n;
        }
        int _debrisNext;
        IEnumerator Fly(Transform d, Vector3 from, Vector3 v)
        {
            d.gameObject.SetActive(true); d.position = from; var spin = UnityEngine.Random.onUnitSphere * 200f;
            // QF15 (2): a flying piece never passes into the bike - it glances off her bubble of space (the wall test found
            // debris moving through the bike: the bike's sweep only stops HER moving into things)
            var rd = d.GetComponentInChildren<Renderer>(); float rad = rd != null ? Mathf.Max(rd.bounds.extents.x, Mathf.Max(rd.bounds.extents.y, rd.bounds.extents.z)) : 1f;
            for (float t = 0f; t < 2.4f; t += Time.deltaTime)
            {
                v *= Mathf.Pow(0.45f, Time.deltaTime); v += Vector3.down * 3f * Time.deltaTime; d.position += v * Time.deltaTime; d.Rotate(spin * Time.deltaTime, Space.World);
                if (Bike != null)
                {
                    var bc = Bike.transform.position + Vector3.up * 0.6f; var off = d.position - bc; float keep = HoverBike.BodyRadius + rad + 0.35f;
                    if (off.sqrMagnitude < keep * keep) { var n = off.sqrMagnitude > 1e-4f ? off.normalized : Vector3.up; d.position = bc + n * keep; if (Vector3.Dot(v, n) < 0f) v = Vector3.Reflect(v, n) * 0.5f; }
                }
                yield return null;
            }
            d.gameObject.SetActive(false);
        }

        /// <summary>Section 12: the chase out. The destruction advances behind her along the tunnel (a little slower than the
        /// bike at speed): readable steering past three broken supports, Panther's warnings, a collapsing narrow with a low
        /// tracking shot, the burst into open water, the pressure wave (she recovers - no cut). Fair and retryable: caught by
        /// the destruction or a crash into a support = a white flash and the ride restarts from its beginning.</summary>
        IEnumerator EscapeRide()
        {
            Objective("Get out!");
            int tries = 0;
            while (true)
            {
                tries++;
                Bike.Place(EuropaLayout.Escape[0], 200f);
                if (!Bike.Riding) Bike.Mount();
                FollowCamera(); Cam.SnapBehind();
                bool crashed = false; string why = null;
                float front = -34f;                             // the blast front's distance along the path (behind her start)
                var pts = EuropaLayout.Escape;
                float total = 0f; var seg = new float[pts.Length]; for (int i = 1; i < pts.Length; i++) { seg[i] = Vector3.Distance(pts[i - 1], pts[i]); total += seg[i]; }
                Vector3 OnPath(float s) { if (s <= 0f) return pts[0] + (pts[0] - pts[1]).normalized * -s; for (int i = 1; i < pts.Length; i++) { if (s <= seg[i]) return Vector3.Lerp(pts[i - 1], pts[i], s / seg[i]); s -= seg[i]; } return pts[pts.Length - 1]; }
                float Along(Vector3 p) { float best = float.MaxValue, acc = 0f, at = 0f; for (int i = 1; i < pts.Length; i++) { var ab = pts[i] - pts[i - 1]; float u = Mathf.Clamp01(Vector3.Dot(p - pts[i - 1], ab) / ab.sqrMagnitude); float d = (pts[i - 1] + ab * u - p).sqrMagnitude; if (d < best) { best = d; at = acc + u * seg[i]; } acc += seg[i]; } return at; }
                var safe = SafeLine();
                if (Auto) StartCoroutine(AutoSwim(safe, 3.5f));
                bool saidJunction = false, saidNext = false, saidNarrow = false, saidKeep = false, lowShot = false;
                float lastRupture = 0f, lastDebris = 0f; float t0 = Time.time;
                // QF13 (14): the blast front - a white-hot wall filling the tunnel, always racing up behind her (it starts 34 m
                // back and runs a little under the bike's top speed, faster if she dawdles); the supports ahead fall in as she
                // nears them, lit by its glow; real chase framing she can steer in: a low camera in front looking back at her
                // with the blast behind (the start), the chase camera, a side tracking shot (mid-tunnel), the chase camera
                var bl = EscapeBlast; Light bLight = bl != null ? bl.GetComponentInChildren<Light>(true) : null;
                if (bl != null) { bl.gameObject.SetActive(true); bl.localScale = Vector3.one; }
                var fell = new bool[EscapeSupports != null ? EscapeSupports.Length : 0];
                if (EscapeSupports != null) foreach (var s in EscapeSupports) if (s != null) s.gameObject.SetActive(false);   // they fall in during the ride
                string frame = "";
                float minGap = 999f, maxGap = 0f; int inFrame = 0, frames = 0;
                while (Vector3.Distance(Bike.transform.position, EuropaLayout.EscapeMouth) > 6f && Time.time - t0 < 120f)
                {
                    float me = Along(Bike.transform.position);
                    foreach (var q in safe) if (q.z < Bike.transform.position.z - 3f) { ShowGuide(q); break; }
                    float gap0 = me - front;
                    float fs = Mathf.Lerp(17f, 22.5f, Mathf.Clamp01(me / total)) + Mathf.Clamp(gap0 - 22f, 0f, 24f) * 0.5f;   // closes in if she is far ahead; never faster than she can ride
                    front += fs * Time.deltaTime;
                    var fp = OnPath(front); var fdir = (OnPath(front + 2f) - fp).normalized;
                    if (bl != null) { bl.position = fp; bl.rotation = Quaternion.LookRotation(fp - Cam.transform.position); }
                    if (bLight != null) bLight.intensity = 9f + 3f * Mathf.PerlinNoise(Time.time * 6f, 0f);
                    if (Time.time - lastRupture > 0.18f && Ruptures != null) { lastRupture = Time.time; Ruptures.transform.position = fp + fdir * 2f; Ruptures.Emit(45); }
                    if (Time.time - lastDebris > 0.5f) { lastDebris = Time.time; ThrowDebris(fp + fdir * 3f + Vector3.up * 3f, 9f, 2); AudioKit.Play("europa_rupture", fp, 0.85f, 0.1f, AudioKit.Bus.Sfx, 80f); }
                    float gap = me - front; minGap = Mathf.Min(minGap, gap); maxGap = Mathf.Max(maxGap, gap);
                    if (front > me - 2.5f) { crashed = true; why = "caught by the blast"; break; }
                    // the supports fall in ahead of her, lit by the blast (readable obstacles)
                    for (int i = 0; EscapeSupports != null && i < EscapeSupports.Length; i++)
                        if (!fell[i] && EscapeSupports[i] != null && Vector3.Distance(Bike.transform.position, EuropaLayout.EscapeSupports[i]) < 48f) { fell[i] = true; StartCoroutine(SupportFalls(EscapeSupports[i])); }
                    foreach (var s in EuropaLayout.EscapeSupports) if (Vector3.Distance(Bike.transform.position, s) < 1.7f) { crashed = true; why = "hit a falling support"; }
                    if (crashed) break;
                    // the chase framing by where she is along the tunnel (short cuts, never confusing about where she goes)
                    string want = me < 30f || (me > total * 0.72f && me < total * 0.8f) ? "front" : me > total * 0.42f && me < total * 0.56f ? "side" : "chase";
                    {
                        // every framing is kept inside the tunnel (the swim volume) - a follow camera behind her slid
                        // through the tunnel's walls at the bends and showed nothing (Dan: "you couldn't even tell what happened")
                        var b = Bike.transform; Vector3 cp, lk; float fov;
                        if (want == "front") { cp = b.position + b.forward * 6f + Vector3.up * 0.8f + b.right * 1.0f; lk = b.position + Vector3.up * 0.8f - b.forward * 8f; fov = 60f; }
                        else if (want == "side")
                        {
                            // the side with room (the tunnel wall must not push the camera into her); too tight = the chase framing
                            var r1 = EuropaLayout.SwimClamp(b.position + b.right * 2.8f + Vector3.up * 1.0f - b.forward * 1.5f, 1.3f);
                            var r2 = EuropaLayout.SwimClamp(b.position - b.right * 2.8f + Vector3.up * 1.0f - b.forward * 1.5f, 1.3f);
                            cp = (r1 - b.position).magnitude >= (r2 - b.position).magnitude ? r1 : r2; lk = b.position + b.forward * 3f + Vector3.up * 0.5f; fov = 62f;
                            if ((cp - b.position).magnitude < 2.2f) { cp = b.position - b.forward * 6.5f + Vector3.up * 1.8f; lk = b.position + b.forward * 6f + Vector3.up * 0.5f; fov = 60f; }
                        }
                        else { cp = b.position - b.forward * 6.5f + Vector3.up * 1.8f; lk = b.position + b.forward * 6f + Vector3.up * 0.5f; fov = 60f; }
                        cp = EuropaLayout.SwimClamp(cp, 1.3f);
                        Cam.Shot(cp, lk, frame != want ? 0.01f : 0.12f, frame != want, fov);
                    }
                    if (frame != want) { Log($"escape (QF13 14): cut to the {want} framing at {me:0} m (blast {gap:0} m behind her)"); StartCoroutine(Shot($"qf13_14d_chase_{want}_{me:000}m")); }
                    frame = want;
                    if (Shots && Time.time > _seqAt) { _seqAt = Time.time + 0.4f; StartCoroutine(Shot($"qf13_14s_{_seqN++:00}_chase_{want}")); }
                    frames++; if (bl != null) { var vp = Cam.GetComponent<Camera>().WorldToViewportPoint(bl.position); if (vp.z > 0f && vp.x > -0.2f && vp.x < 1.2f && vp.y > -0.2f && vp.y < 1.2f) inFrame++; }
                    if (!saidJunction && Vector3.Distance(Bike.transform.position, EuropaLayout.EscapeJunction) < 22f) { saidJunction = true; StartCoroutine(Say("EUR_12", 5, 6)); }   // Left at the junction / I see it.
                    if (!saidNext && me > total * 0.45f) { saidNext = true; StartCoroutine(Say("EUR_12", 7, 8)); }                                                         // The next opening— / I see it!
                    if (!saidNarrow && Vector3.Distance(Bike.transform.position, EuropaLayout.EscapeNarrow) < 14f) { saidNarrow = true; if (EscapeCollapse != null) StartCoroutine(Narrowing()); }
                    if (!lowShot && Vector3.Distance(Bike.transform.position, EuropaLayout.EscapeNarrow) < 4f)
                    {
                        // low tracking shot - the bike forks clear the obstruction; straight back to the riding camera
                        lowShot = true;
                        Shoot(Bike.transform.position + Bike.transform.forward * 2.2f + Vector3.down * 0.4f + Bike.transform.right * 0.6f, Bike.transform.position + Bike.transform.forward * 0.8f, 44f, 0.01f);
                        yield return Wait(0.35f); StartCoroutine(Shot("e25_low_tracking_bike_forks")); yield return Wait(0.35f);
                        FollowCamera();
                    }
                    if (!saidKeep && me > total * 0.8f) { saidKeep = true; StartCoroutine(Say("EUR_12", 9, 9)); }                                                         // Keep going.
                    if (Shots && !_escShot && me > total * 0.25f) { _escShot = true; StartCoroutine(Shot("e25b_escape_ruptures_behind_her")); }
                    yield return null;
                }
                StopAutoSwim(); HideGuide();
                Log($"escape (QF13 14): try {tries}: blast {minGap:0}..{maxGap:0} m behind her; the blast (or its light) in the frame in {inFrame} of {frames} frames; {(crashed ? why : "out")}");
                if (!crashed) break;
                if (bl != null) bl.gameObject.SetActive(false);
                Crashes++;
                Log($"escape ride: {why} at {Bike.transform.position} - white flash, retry from the start (try {tries + 1})");
                yield return UI.Fade(1f, 0.25f, 1f);
                yield return Wait(0.4f);
                yield return UI.Fade(0f, 0.6f, 1f);
                if (tries >= 6 && Auto) { Log("escape ride: autopilot could not finish - placed at the mouth (check run only)"); Bike.Place(EuropaLayout.EscapeMouth, 200f); break; }
            }
            // QF13 (14): the money shot - she bursts out of the tunnel mouth a split second ahead of the blast; the fireball
            // erupts out of the opening behind her (slow motion for a moment), the pressure wave catches and spins the bike,
            // she wrestles it back under control - the player sees her recover; then the entrance collapses and it goes quiet
            {
                var bl = EscapeBlast; Light bLight = bl != null ? bl.GetComponentInChildren<Light>(true) : null;
                var mouth = EuropaLayout.EscapeMouth; var outDir = (EuropaLayout.OpenWaterStop - mouth).normalized;
                var wide = mouth + outDir * 28f + Vector3.Cross(Vector3.up, outDir) * 3f + Vector3.up * 1.5f;     // in the open water, looking straight back into the mouth: she bursts out toward the camera, the fireball behind her
                Shoot(wide, mouth, 50f, 0.01f);
                float ts0 = Time.timeScale;
                if (bl != null) { bl.gameObject.SetActive(true); bl.position = mouth - outDir * 6f; bl.localScale = Vector3.one; }
                AudioKit.Play("europa_demolition", mouth, 1f, 0f, AudioKit.Bus.Sfx, 300f);
                bool s1 = false, s2 = false, shoved = false;
                for (float t = 0f; t < 2.4f; t += Time.unscaledDeltaTime)
                {
                    Time.timeScale = ts0 * (t < 0.3f ? 1f : t < 1.5f ? 0.3f : Mathf.Lerp(0.3f, 1f, (t - 1.5f) / 0.9f));
                    float u = Mathf.Clamp01((t - 0.15f) / 1.6f);
                    if (bl != null) { bl.position = mouth + outDir * Mathf.Lerp(-6f, 1f, u); bl.localScale = Vector3.one * Mathf.Lerp(1f, 1.6f, u); bl.rotation = Quaternion.LookRotation(bl.position - Cam.transform.position); }
                    if (bLight != null) bLight.intensity = Mathf.Lerp(14f, 4f, u);
                    if (Ruptures != null && Mathf.Repeat(t, 0.1f) < Time.unscaledDeltaTime) { Ruptures.transform.position = mouth + outDir * 4f * u; Ruptures.Emit(70); }
                    if (!shoved && t > 0.45f) { shoved = true; AudioKit.Play("europa_pressure_wave", null, 1f); Bike.Shove(outDir * 9f + Vector3.up * 2f, 260f); CameraShake.Quake(1.4f, 1f); ThrowDebris(mouth, 14f, 10); }
                    if (!s1 && t > 0.7f) { s1 = true; StartCoroutine(Shot("qf13_14e_burst_out_fireball_behind_her")); }
                    if (Shots && Time.unscaledTime > _seqAt) { _seqAt = Time.unscaledTime + 0.3f; StartCoroutine(Shot($"qf13_14s_{_seqN++:00}_burst_out")); }
                    if (!s2 && t > 1.4f) { s2 = true; StartCoroutine(Shot("e26_the_pressure_wave")); }
                    yield return null;
                }
                Time.timeScale = ts0;
                if (bl != null) bl.gameObject.SetActive(false);
                if (EscapeCollapse != null) EscapeCollapse.gameObject.SetActive(true);
                // she wrestles it back under control - stay on her until she has
                var b = Bike.transform; Shoot(EuropaLayout.SwimClamp(b.position + outDir * 7f + Vector3.Cross(Vector3.up, outDir) * 3f + Vector3.up * 1.2f, 1.5f), b.position + Vector3.up * 0.8f, 50f, 0.6f);
                yield return Wait(1.4f);
                yield return Shot("e27_she_recovers");
                FollowCamera();
            }
            Log($"escape ride: out in open water after {tries} try/tries ({Crashes} crash(es) in total) - the pressure wave, she recovers");
        }
        bool _escShot; float _seqAt; int _seqN;
        Vector3[] SafeLine()
        {
            // the autopilot line: the tunnel's centre, with a side-step round each support
            var l = new List<Vector3>();
            foreach (var p in EuropaLayout.Escape)
            {
                foreach (var s in EuropaLayout.EscapeSupports) if (Vector3.Distance(p, s) < 14f && (s - l.LastOrDefault(p)).sqrMagnitude > 1f) { var side = Vector3.Cross(Vector3.up, (p - s).normalized); l.Add(s + side * 3.2f); }
                l.Add(p);
            }
            l.Sort((a, b) => b.z.CompareTo(a.z));
            return l.ToArray();
        }
        /// <summary>QF13 (14): a broken support drops into the tunnel ahead of her (from the ceiling, tumbling), dust.</summary>
        IEnumerator SupportFalls(Transform s)
        {
            var p1 = s.position; var r1 = s.rotation; var p0 = p1 + Vector3.up * 7f; var r0 = r1 * Quaternion.Euler(0f, 0f, 35f);
            s.gameObject.SetActive(true);
            AudioKit.Play("europa_rupture", p1, 0.9f, 0.1f, AudioKit.Bus.Sfx, 80f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.75f) { float u = t * t; s.SetPositionAndRotation(Vector3.Lerp(p0, p1, u), Quaternion.Slerp(r0, r1, u)); yield return null; }
            s.SetPositionAndRotation(p1, r1);
            if (Ruptures != null) { Ruptures.transform.position = p1; Ruptures.Emit(40); }
        }

        IEnumerator Narrowing()
        {
            // a collapsing section narrows the route (a slab tilting in from the wall)
            var t = EscapeCollapse; if (t == null) yield break;
            yield break;
        }

        IEnumerator AfterTheNoise()
        {
            // she brings the bike to a stop in clear water; her breathing inside the helmet
            Objective(null);
            if (Auto) yield return AutoSwimOnce(EuropaLayout.OpenWaterStop);
            yield return StopBike(2.5f);
            yield return Cutscene(true);
            Shoot(Bike.transform.position + Bike.transform.forward * 4f + Bike.transform.right * 2f + Vector3.up * 1.2f, Bike.transform.position + Vector3.up * 1.2f, 46f, 0.01f);
            AudioKit.Play("europa_bubbles", P7 + Vector3.up * 1.6f, 0.5f);
            yield return Say("EUR_13", 1, 7);
            // close-up: her eyes through the helmet; her gaze softens
            var hd = Seven.Bone(HumanBodyBones.Head);
            if (hd != null) Shoot(hd.position + Bike.transform.forward * 0.8f + Bike.transform.right * 0.2f, hd.position + Vector3.up * 0.03f, 30f, 0.01f);
            yield return Wait(0.6f);
            yield return Shot("e28_her_eyes_through_the_helmet");
            yield return Say("EUR_13", 8, 8);
            // before she answers: a Cetacean call travels through the water; another answers
            AudioKit.Play("europa_call_deep", EuropaLayout.OpenWaterStop + new Vector3(-80f, -10f, -60f), 0.9f, 0f, AudioKit.Bus.Sfx, 500f);
            yield return Wait(2.8f);
            AudioKit.Play("europa_call_answer", EuropaLayout.OpenWaterStop + new Vector3(90f, 0f, 40f), 0.8f, 0f, AudioKit.Bus.Sfx, 500f);
            Shoot(Bike.transform.position + new Vector3(-30f, 10f, 30f), Bike.transform.position + new Vector3(10f, -4f, -30f), 60f, 2.5f);
            yield return Wait(2.2f);
            yield return Shot("e29_wide_the_ocean_calls");
            yield return Say("EUR_13", 9, 9);                    // Listen.
            yield return Wait(3.0f);                             // hold the moment
            MusicSystem.Play("europa_underwater", 4f);
            Complete("EUR_13");
            yield return Cutscene(false);
        }
        IEnumerator AutoSwimOnce(Vector3 p) { yield return AutoSwim(new[] { p }, 4f); }

        // ------------------------------------------------------------------------------------------ 14-15: the temple, the vision
        IEnumerator Temple()
        {
            Objective("Follow the keeper");
            if (Keeper != null) { Keeper.transform.position = EuropaLayout.OpenWaterStop + new Vector3(8f, 3f, -14f); Keeper.GlideTo(EuropaLayout.ToTemple[2] + new Vector3(6f, 6f, 0f), 5f); }
            yield return RideRoute(EuropaLayout.ToTemple, "the reopened route to the temple", 8f);
            yield return StopBike();
            yield return Cutscene(true);
            // the temple is built for its inhabitants: broad passages, enormous spaces
            Shoot(EuropaLayout.TempleCentre + new Vector3(-30f, 18f, 30f), EuropaLayout.TempleCentre + Vector3.up * 4f, 58f, 0.01f);
            if (Keeper != null) Keeper.Loop(new[] { EuropaLayout.TempleCentre + new Vector3(12f, 8f, 0f), EuropaLayout.TempleCentre + new Vector3(0f, 10f, 14f), EuropaLayout.TempleCentre + new Vector3(-12f, 8f, 0f) }, 3f);
            yield return Wait(1.0f);
            yield return Shot("e30_wide_the_cetacean_temple");
            yield return Say("EUR_14", 1, 4);
            Complete("EUR_14");
            yield return Cutscene(false);
            // the keeper brings her to Europa's rune; the vision plays as she reaches it (no button)
            Objective("Go to the rune");
            if (Keeper != null) Keeper.GlideTo(EuropaLayout.RuneAt + new Vector3(8f, 4f, 2f), 3f);
            ShowGuide(EuropaLayout.RuneView);
            if (Auto) StartCoroutine(AutoSwim(new[] { EuropaLayout.RuneView }, 1.5f));
            yield return Until(() => Vector3.Distance(Bike.transform.position, EuropaLayout.RuneView) < 3f || Bike.transform.position.z < EuropaLayout.RuneView.z, "reach the rune", 60f, "The glowing icosahedron at the temple's heart", EuropaLayout.RuneView, () => PlaceRiding(EuropaLayout.RuneView));
            HideGuide(); StopAutoSwim();
            yield return StopBike(1f);
            yield return Cutscene(true);
            Shoot(EuropaLayout.RuneView + new Vector3(2.5f, 1.4f, 1.6f), EuropaLayout.RuneAt, 40f, 0.01f);
            yield return Wait(0.8f);
            yield return Shot("e31_the_rune_icosahedron");
            yield return Vision3Play();
            // back in the temple: 7 remains still
            Shoot(EuropaLayout.RuneView + Bike.transform.right * 3.5f + Vector3.up * 1.6f, P7 + Vector3.up * 1.3f, 44f, 0.01f);
            yield return Say("EUR_15", 1, 7);
            // the record, once - insert: she secures the small icosahedron beside the cube and the dodecahedron
            bool first = EuropaState.AwardRecord();
            if (RecordIco != null)
            {
                RecordIco.gameObject.SetActive(true);
                var hand = Seven.Bone(HumanBodyBones.LeftHand); var hp = hand != null ? hand.position : P7 + Vector3.up * 1.2f;
                RecordIco.position = hp + Vector3.up * 0.08f;
                Shoot(hp + Vector3.up * 0.55f + Bike.transform.forward * 0.2f, hp, 26f, 0.01f);
                yield return Wait(0.8f);
                yield return Shot("e32_insert_icosahedron_beside_cube_and_dodecahedron");
            }
            Log($"Europa's record: awarded {(first ? "now (first time)" : "ALREADY - no second award")} (awards {EuropaState.RecordAwards}, must be 1)");
            yield return Say("EUR_15", 8, 8);                    // Carry it somewhere it can be heard.
            Complete("EUR_15");
            yield return Cutscene(false);
        }

        UnityEngine.Video.VideoPlayer _vp3; RenderTexture _vp3RT;
        IEnumerator Vision3Play()
        {
            // as the Venus vision: music suspended, the shimmer in and out; the video is nearly silent - the learning call carries it
            MusicSystem.Suspend(1.2f);
            AudioKit.Play("vision_whoosh", null, 0.9f);
            var shimmer = AudioKit.Loop("vision_shimmer", null, null, 0f, AudioKit.Bus.Sfx);
            if (shimmer != null) StartCoroutine(AudioKit.Fade(shimmer, 0.5f, 1.5f));
            yield return UI.Fade(1f, 0.9f, 1f);
            bool video = false;
            if (Vision3 != null && !DemoBoot.NoVideo)
            {
                var vgo = new GameObject("Vision3Video");
                _vp3 = vgo.AddComponent<UnityEngine.Video.VideoPlayer>();
                _vp3.playOnAwake = false; _vp3.isLooping = false; _vp3.clip = Vision3; _vp3.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
                _vp3RT = new RenderTexture((int)Vision3.width, (int)Vision3.height, 0); _vp3.targetTexture = _vp3RT;
                _vp3.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None; _vp3.timeUpdateMode = UnityEngine.Video.VideoTimeUpdateMode.GameTime;
                _vp3.Prepare();
                for (float t = 0f; t < 3f && !_vp3.isPrepared; t += Time.deltaTime) yield return null;
                video = _vp3.isPrepared;
            }
            float aspect = Vision3 != null ? Vision3.width / (float)Mathf.Max(1u, Vision3.height) : 16f / 9f;
            AudioKit.Play("europa_call_learning", null, 0.9f);
            if (video)
            {
                Log($"vision 3: video {Vision3.name} ({Vision3.length:0.0}s) full-screen; the learning call carries it");
                _vp3.time = 0; _vp3.Play();
                for (float t = 0f; t < 0.7f; t += Time.deltaTime) { UI.SetVideo(_vp3RT, aspect, t / 0.7f); yield return null; }
                UI.SetVideo(_vp3RT, aspect, 1f);
                bool shot = false; float len = (float)Vision3.length;
                for (float vt0 = Time.time; Time.time - vt0 < len - 0.12f && (_vp3.isPlaying || Time.time - vt0 < 1f);)
                {
                    if (!shot && Time.time - vt0 > 5f) { shot = true; yield return Shot("e31b_vision_video_frame"); }
                    if (Time.time - vt0 > 0.8f && SkipButton()) break;
                    yield return null;
                }
                _vp3.Pause();
            }
            else
            {
                var still = Resources.Load<Texture2D>("Europa/vision_03_still");
                Log($"vision 3: VIDEO NOT AVAILABLE - still fallback {(still != null ? "shown" : "missing")}");
                if (still != null) for (float t = 0f; t < 0.7f; t += Time.deltaTime) { UI.SetVideo(still, still.width / (float)still.height, t / 0.7f); yield return null; }
                for (float t = 0f; t < 8f && !(t > 0.8f && SkipButton()); t += Time.deltaTime) yield return null;
                yield return Shot("e31b_vision_still_fallback");
            }
            if (shimmer != null) StartCoroutine(AudioKit.Fade(shimmer, 0f, 1.0f, true));
            UI.SetVideo(null, aspect, 0f);
            if (_vp3 != null) { _vp3.Stop(); Destroy(_vp3.gameObject); }
            yield return UI.Fade(0f, 1.2f, 1f);
            MusicSystem.Resume(3.5f);
        }

        // ------------------------------------------------------------------------------------------ 16: home - the bike recovered in the beam
        IEnumerator Home()
        {
            Objective("Return to the saucer");
            if (!Bike.Riding) { Bike.Place(EuropaLayout.Home[0], 40f); Bike.Mount(); FollowCamera(); }
            // a short ride toward the way home, then the cut to the shelf (the keeper's people show her the short way up)
            ShowGuide(EuropaLayout.Home[0]);
            if (Auto) StartCoroutine(AutoSwim(new[] { EuropaLayout.Home[0] }, 8f));
            yield return Until(() => Vector3.Distance(Bike.transform.position, EuropaLayout.Home[0]) < 14f || Bike.transform.position.x > EuropaLayout.Home[0].x, "the way home", 90f, "Follow the light: the keeper's people show you the way up", EuropaLayout.Home[0], () => PlaceRiding(EuropaLayout.Home[0]));
            HideGuide(); StopAutoSwim();
            yield return Cutscene(true);
            yield return UI.Fade(1f, 1.0f);
            // the shelf: the saucer above the ice; the bike returns to the boarding position and is recovered in the beam
            Bike.Place(EuropaLayout.BikeDown, 0f);
            if (Bike.Riding) Bike.DismountTo(EuropaLayout.SevenDown + Vector3.forward * 2.6f, Vector3.back);
            Seven.Place(EuropaLayout.SevenDown + Vector3.forward * 2.6f, Vector3.back); Seven.Lock(true);
            var land = EuropaLayout.SaucerAt;
            Shoot(land + new Vector3(16f, 4f, 14f), land + new Vector3(0f, 2.5f, 0f), 50f, 0.01f);
            yield return UI.Fade(0f, 1.0f);
            yield return Shot("e33_wide_saucer_above_the_ice");
            // QF13 (5): the bike rises back into the bike hold, which closes seamlessly; the beam is only for her
            yield return HoldBikeIn();
            if (SaucerBeam != null) { yield return SaucerBeam.Iris(true); yield return SaucerBeam.BeamOn(true); }
            // she boards through the beam - no ramp
            yield return WalkSeven(EuropaLayout.SevenDown, 6f);
            Seven.Lock(true); Seven.BeginFloat();
            bool s = false;
            if (SaucerBeam != null) yield return SaucerBeam.Rise(Seven.transform, 3.8f, 40f, t => { if (!s && t > 0.45f) { s = true; StartCoroutine(Shot("e34_she_boards_in_the_beam")); } });
            if (SaucerBeam != null) yield return SaucerBeam.Open(false);
            yield return UI.Fade(1f, 1.0f);
            ChapterDone = true;
            Log("Europa (sections 2-16 in the Europa scene) done - aboard for the reunion and the debrief");
            EuropaState.HomeAboard = true; Chapter1State.Aboard = true;
            SceneManager.LoadScene("Saucer");
        }
    }

    static class ListExt { public static T LastOrDefault<T>(this List<T> l, T d) => l.Count > 0 ? l[l.Count - 1] : d; }
}
