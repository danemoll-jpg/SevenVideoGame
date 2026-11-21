using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using L = Seven.AsteroidLayout;

namespace Seven
{
    /// <summary>Round 16 — the Asteroid: AST_05 (the alcove, the arc blades, the three controls), AST_06 (a missed answer),
    /// AST_07 (the Machinist's fight), AST_08 (the archive + the vision), AST_09 (a different instruction) and the way home.</summary>
    public partial class AsteroidDirector
    {
        // ------------------------------------------------------------------------------------------ AST_05: keeping the wrong thing alive
        IEnumerator Controls()
        {
            if (!AsteroidState.SceneDone("AST_05_lines"))
            {
                Objective("Follow the maintenance passage");
                yield return GuideWalk(new[] { new Vector3(-328f, 0f, 58f), new Vector3(-331f, 0f, 46f) }, "to the observation alcove");
                yield return Cutscene(true);
                // CLOSE on her hand resting against the glass, then the panels moving below
                Seven.Place(new Vector3(L.AlcoveWindow.x + 0.85f, 0f, L.AlcoveWindow.z), Vector3.left); Seven.Lock(true);
                Seven.Touch(L.AlcoveWindow + Vector3.left * 0.3f);
                Shoot(new Vector3(-333.2f, 1.8f, 47.6f), L.AlcoveWindow + new Vector3(-0.6f, -0.2f, -0.6f), 42f, 0.01f);
                yield return Wait(1.2f);
                yield return Shot("a23_her_hand_against_the_glass");
                Shoot(L.AlcoveWindow + new Vector3(0.6f, 1.2f, 0.8f), new Vector3(-344f, -5f, 36f), 44f, 0.6f);
                yield return Wait(1.0f);
                yield return Shot("a23b_the_archive_panels_moving_below");
                yield return Say("AST_05", 1, 2);                 // He's taking the place apart to keep it running. / The archive feed goes directly to the foundry.
                Seven.Release(); Seven.Lock(true);
                Seven.transform.rotation = Quaternion.LookRotation(Flat(L.AlcoveTerminal - P7));
                TwoShot((P7 + L.AlcoveTerminal) * 0.5f, Vector3.Cross(Vector3.up, Flat(L.AlcoveTerminal - P7).normalized), 2.8f, 1.3f, 1.2f, 46f, 0.4f);
                yield return Say("AST_05", 3, 5);                 // Can we stop it? / The whole foundry? Not safely... / All right. Just the archive line.
                // a route appears on her display: the bypass, three local controls
                if (AlcoveScreen != null) Glow(AlcoveScreen, 1.5f);
                Shoot(L.AlcoveTerminal + new Vector3(0.9f, 1.75f, 1.9f), L.AlcoveTerminal + new Vector3(0f, 1.45f, -0.3f), 36f, 0.4f);
                yield return Say("AST_05", 6, 6);                 // There's a maintenance bypass. Three local controls.
                yield return Shot("a25_the_bypass_route_on_the_display");
                TwoShot((P7 + L.AlcoveTerminal) * 0.5f, Vector3.Cross(Vector3.up, Flat(L.AlcoveTerminal - P7).normalized), 2.8f, 1.3f, 1.2f, 46f, 0.4f);
                yield return Say("AST_05", 7, 9);                 // Of course there are. / Would you prefer four? / Don't encourage it.
                yield return SayIds("AST_05_P1");                 // The bypass wants a maintenance tool. The locker beside the terminal.
                Complete("AST_05_lines");
                yield return Cutscene(false);
            }
            if (!AsteroidState.StepDone("blades"))
            {
                Objective("Open the maintenance locker beside the terminal (X)");
                if (LockerLamp != null) Glow(LockerLamp, 1.2f);
                yield return Interact(L.LockerView, "Open the locker", "open the locker", "The sealed locker on the wall beside the terminal", new[] { L.LockerView });
                yield return Cutscene(true);
                Seven.Place(L.LockerView, Vector3.right); Seven.Lock(true);
                AudioKit.Play("ast_vault_open", L.LockerAt, 0.7f, 0f, AudioKit.Bus.Sfx, 20f);
                if (LockerDoor != null) StartCoroutine(SwingDoor(LockerDoor, -100f, 1.0f));
                Shoot(L.LockerView + new Vector3(-1.4f, 1.6f, -1.6f), L.LockerAt + Vector3.up * 1.2f, 40f, 0.01f);
                yield return Wait(1.1f);
                yield return Shot("a26_the_locker_the_arc_blades");
                // she takes them as if she knew them: the hilts into her hands, the light grows out of them (first draw)
                if (LockerBlades != null) LockerBlades.gameObject.SetActive(false);
                AsteroidState.GiveBlades(); AsteroidState.DoStep("blades");
                Seven.SetBladesOwned(true); Seven.DrawBlades(true);
                Seven.transform.rotation = Quaternion.LookRotation(Vector3.left);                  // she turns back into the alcove with them
                Shoot(P7 + Vector3.left * 2.3f + Vector3.up * 1.4f + Vector3.forward * 0.5f, P7 + Vector3.up * 1.15f, 40f, 0.01f);
                yield return Wait(1.0f);
                yield return Shot("a27_she_holds_the_arc_blades");
                yield return SayIds("AST_05_10");                 // You've done that before.
                Shoot(HerHead + Vector3.left * 1.1f + Vector3.down * 0.15f, HerHead + Vector3.down * 0.35f, 36f, 0.01f);
                yield return SayIds("AST_05_11");                 // I... don't know.
                yield return Wait(0.8f);
                Seven.DrawBlades(false);
                Log("AST_05: the arc blades are hers (she keeps them: RB / G draws, X cuts while drawn)");
                yield return Cutscene(false);
                UI.Hint("RB / G: draw or sheathe the arc blades  ·  X while drawn: cut", 7f);
            }
            Complete("AST_05_blades");
            // the three controls in the compact factory area; the security machines are about
            SecurityActive(true);
            StartCoroutine(SecurityLines());
            yield return ControlTransport();
            yield return ControlCooling();
            Objective("Cross the last transport assembly to the archive diverter");
        }
        IEnumerator SwingDoor(Transform t, float deg, float secs) { var a = t.localRotation; var b = Quaternion.Euler(0f, deg, 0f); for (float k = 0f; k < 1f; k += Time.deltaTime / secs) { t.localRotation = Quaternion.Slerp(a, b, Mathf.SmoothStep(0f, 1f, k)); yield return null; } }
        IEnumerator SecurityLines()
        {
            // the first time one comes for her: Panther's warning (spoken while she plays)
            while (Security != null)
            {
                foreach (var s in Security) if (s != null && s.State == AstSecurity.St.Chase) { yield return SayIds("AST_05_P2", "AST_05_P3"); yield break; }
                yield return null;
            }
        }
        int ControlsDone => (AsteroidState.StepDone("control_transport") ? 1 : 0) + (AsteroidState.StepDone("control_cooling") ? 1 : 0) + (AsteroidState.StepDone("control_diverter") ? 1 : 0);
        string ControlsLine => $"\nControls {ControlsDone} / 3";
        IEnumerator ControlTransport()
        {
            if (AsteroidState.StepDone("control_transport")) yield break;
            Objective("Control 1: reposition the cargo platforms across the pit" + ControlsLine);
            yield return Interact(L.TransportControl, "Reposition the platforms", "the transport control", "The lever by the pit's edge, east side", new[] { new Vector3(-330f, 0f, 30f), L.TransportControl + new Vector3(1.2f, 0f, 0f) });
            yield return Cutscene(true);
            if (TransportLever != null) StartCoroutine(Spin(TransportLever, Vector3.right, -70f, 0.5f));
            AudioKit.Play("mars_section_slide", new Vector3(-344f, 0f, 17f), 1f, 0f, AudioKit.Bus.Sfx, 60f);
            Shoot(new Vector3(-330f, 6f, 26f), new Vector3(-344f, -1f, 16f), 52f, 0.01f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.6f) { PlacePlatforms(Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            PlacePlatforms(1f); SetZone("Platforms1", true);
            CameraShake.Quake(0.2f, 0.2f);
            yield return Shot("a28_control1_the_platforms_line_up");
            AsteroidState.DoStep("control_transport"); if (ControlLamps != null && ControlLamps.Length > 0) Glow(ControlLamps[0], 1.8f);
            // the central hall changes: the feed to the cutter halts (the panels wait short of the saw)
            if (ArchiveLine != null) ArchiveLine.Stopped = true;
            yield return HallInsert("a29_hall_the_feed_halts");
            Log($"control 1 (transport): the cargo platforms lined up across the pit; in the hall the feed to the cutter stopped - Controls {ControlsDone} / 3");
            yield return Cutscene(false);
        }
        IEnumerator ControlCooling()
        {
            if (AsteroidState.StepDone("control_cooling")) yield break;
            Objective("Control 2: redirect the coolant - the valve beyond the platforms" + ControlsLine);
            yield return GuideWalk(new[] { new Vector3(-337f, 0f, 17f), new Vector3(-354f, 0f, 17f), new Vector3(-364f, 0f, 26f) }, "across the platforms");
            // the valve is held by a powered clamp; the new tool cuts power lines
            yield return SayIds("AST_05_P4");
            Objective("Cut the clamp's power line with the arc blades (RB, then X)" + ControlsLine);
            yield return BladeCut(L.ClampLine, "Cut the power line", "cut the clamp's power line", "Draw the arc blades (RB / G) and cut the glowing line feeding the clamp on the valve (X)", new[] { new Vector3(-365.5f, 0f, 31.2f) }, 3.0f);
            if (ClampPower != null) ClampPower.All(false);
            if (ClampJaw != null) StartCoroutine(SwingDoor(ClampJaw, 60f, 0.4f));
            AudioKit.Play("ast_power_down", L.ClampLine, 0.8f, 0f, AudioKit.Bus.Sfx, 30f);
            yield return Shot("a30_the_clamp_power_line_cut");
            Objective("Control 2: turn the coolant valve (X)" + ControlsLine);
            yield return Interact(L.CoolingValve + new Vector3(1.0f, 0f, -0.6f), "Turn the valve", "the cooling valve", "The valve wheel the clamp was holding", new[] { L.CoolingValve + new Vector3(1.0f, 0f, -0.6f) });
            yield return Cutscene(true);
            Shoot(new Vector3(-362f, 4f, 24f), new Vector3(-378f, 1f, 14f), 50f, 0.01f);
            if (ValveWheel != null) StartCoroutine(Spin(ValveWheel, Vector3.forward, 540f, 1.6f));
            AudioKit.Play("ast_coolant", L.CoolingValve, 1f, 0f, AudioKit.Bus.Sfx, 50f);
            if (Coolant != null) StartCoroutine(Coolant.Run(0, Coolant.Count - 1, 2.0f));
            yield return Wait(1.4f);
            SteamOn(false); SetZone("CoolPass", true);
            yield return Wait(1.0f);
            yield return Shot("a31_control2_coolant_runs_the_steam_dies");
            AsteroidState.DoStep("control_cooling"); if (ControlLamps != null && ControlLamps.Length > 1) Glow(ControlLamps[1], 1.8f);
            // the central hall changes: coolant floods the cutting mechanism - its saw slows and goes dark
            if (ArchiveLine != null && ArchiveLine.SawLight != null) ArchiveLine.SawLight.enabled = false;
            yield return HallInsert("a32_hall_the_cutter_cools_and_stops");
            Log($"control 2 (cooling): the clamp's power cut with the blades, the coolant valve turned, the passage clear; the cutter cooled - Controls {ControlsDone} / 3");
            yield return Cutscene(false);
        }
        /// <summary>Each control visibly changes the central hall: a short look at it (the archive getting closer to safety).</summary>
        IEnumerator HallInsert(string shot)
        {
            if (shot.Contains("storage")) Shoot(new Vector3(-386f, 6f, 117f), new Vector3(-389f, 0.6f, 132f), 50f, 0.01f);              // the panels turn onto the storage loop
            else if (shot.Contains("cutter")) Shoot(new Vector3(-381f, 2.4f, 116.5f), L.CutterAt + new Vector3(-2.3f, 1.4f, 0f), 40f, 0.01f);   // the saw cools and goes dark
            else Shoot(new Vector3(-386f, 2.6f, 114f), new Vector3(-380f, 1f, 122.5f), 46f, 0.01f);                                           // the feed waits short of the saw
            yield return Wait(1.6f);
            yield return Shot(shot);
            yield return Wait(0.6f);
        }

        // ------------------------------------------------------------------------------------------ AST_06: a missed answer
        IEnumerator Drop()
        {
            SecurityActive(true);
            if (!AsteroidState.SceneDone("AST_06"))
            {
                Objective("Cross the last transport assembly to the archive diverter" + ControlsLine);
                yield return GuideWalk(new[] { new Vector3(-374f, 0f, 14f), new Vector3(-386f, 0f, 14f), new Vector3(-390f, 0f, 14f) }, "onto the final transport assembly", 1.4f);
                // the platform unexpectedly retracts - she drops onto the maintenance ledge
                yield return Cutscene(true);
                SecurityActive(false);
                AudioKit.Play("ast_platform_retract", L.FinalPlatC, 1f, 0f, AudioKit.Bus.Sfx, 40f);
                var p0 = P7;
                StartCoroutine(SayIds("AST_06_01"));               // Oh-!
                if (FinalPlat != null) StartCoroutine(SlidePlat(true));
                SetZone("FinalPlat", false); SetZone("LowLedge", true);
                Shoot(new Vector3(-383.5f, 4f, 14f), new Vector3(-392f, -3f, 14f), 56f, 0.01f);
                Seven.PlayAnim("fall_idle", 0.1f);
                var land = new Vector3(Mathf.Clamp(p0.x, -396f, -387f), L.LedgeY, Mathf.Clamp(p0.z, 8f, 20f));
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.55f) { Seven.transform.position = Vector3.Lerp(p0, land, t * t); yield return null; }
                Seven.Place(land, Vector3.left); Seven.Lock(true);
                Seven.PlayAnim("landing_hard", 0.05f);
                CameraShake.Quake(0.45f, 0.3f); AudioKit.Play("land_hard", land, 1f, 0.03f);
                yield return Shot("a33_she_drops_to_the_maintenance_ledge");
                // the connection breaks: static, then silence
                var stat = AudioKit.Play("ast_static", null, 0.8f);
                yield return SayIds("AST_06_02");                  // Seven?
                MusicSystem.Stop(1.5f);
                yield return Wait(2.6f);                           // AST_06_03: the machinery drowns out the connection
                yield return SayIds("AST_06_04");                  // Seven. Answer me.
                yield return Cutscene(false);
                // the player keeps control while she climbs to safety (the wall up to the west works: A at its foot)
                Objective("Climb back up (A at the wall)");
                ShowGuide(L.ClimbBase);
                if (Auto) { yield return WalkSeven(L.ClimbBase, 10f); Seven.Place(L.ClimbBase, Vector3.left); }
                bool spoke = false;
                float tClimb = Time.time;
                while (!(P7.y > -0.6f && P7.x < -398.2f))
                {
                    if (!spoke && (Seven.Current == ChapterSeven.Mode.Climb || Seven.Current == ChapterSeven.Mode.Hang)) { spoke = true; StartCoroutine(SayIds("AST_06_05")); }    // Here. I'm here. (hauling herself up)
                    if (Auto && Time.time - tClimb > 1f && Seven.Current == ChapterSeven.Mode.Free && Time.frameCount % 30 == 0) { Seven.Place(L.ClimbBase, Vector3.left); Seven.ScriptJump = true; }   // the check run presses A at the wall
                    if (Time.time - tClimb > (Auto ? 30f : 150f)) { Log("AST_06: the climb took too long - she is helped up (skip)"); Seven.Place(new Vector3(-400f, 0f, 14f), Vector3.left); break; }
                    yield return null;
                }
                HideGuide();
                if (!spoke) yield return SayIds("AST_06_05");
                Log($"AST_06: she climbed back up the maintenance wall ({Time.time - tClimb:0.0} s; climb mode seen {spoke})");
                yield return Shot("a34_she_hauls_herself_up");
                // the full exchange, unhurried - no combat: she sits back briefly, steadies her breathing
                yield return Cutscene(true);
                Seven.PlayAnim(Seven.HasState("crouch_idle") ? "crouch_idle" : "idle", 0.4f);
                TwoShot(P7 + Vector3.up * 0.2f, Vector3.forward, 2.6f, 0.9f, 0.9f, 42f, 0.5f);
                yield return Say("AST_06", 6, 8);                  // Are you hurt? / Just... give me a second. / Take it.
                yield return Wait(1.2f);
                Seven.Release(); Seven.Lock(true);
                yield return Say("AST_06", 9, 11);                 // I'm all right. / Good. / You went very quiet.
                Shoot(P7 + new Vector3(1.2f, 1.6f, 2.2f), P7 + new Vector3(8f, 3f, -6f), 44f, 0.8f);   // she looks toward the distant saucer marker
                yield return Say("AST_06", 12, 12);                // So did you.
                yield return Wait(1.2f);                           // a beat of silence
                TwoShot(P7 + Vector3.up * 0.2f, Vector3.forward, 2.6f, 1.1f, 1.2f, 42f, 0.5f);
                yield return Say("AST_06", 13, 15);                // I'll keep talking. / An unusual thing... / Heh. There you are.
                yield return Shot("a35_there_you_are");
                MusicSystem.Play("asteroid_city", 4f);
                Complete("AST_06");
                // the platform slides back out (no point of no return)
                if (FinalPlat != null) StartCoroutine(SlidePlat(false));
                SetZone("FinalPlat", true); SetZone("LowLedge", false);
                yield return Cutscene(false);
                SecurityActive(true);
            }
            yield return ControlDiverter();
        }
        IEnumerator SlidePlat(bool retract) { var a = FinalPlat.localPosition; var b = retract ? new Vector3(0f, 0f, -13f) : Vector3.zero; for (float t = 0f; t < 1f; t += Time.deltaTime / 0.7f) { FinalPlat.localPosition = Vector3.Lerp(a, b, t); yield return null; } FinalPlat.localPosition = b; }
        IEnumerator ControlDiverter()
        {
            if (!AsteroidState.StepDone("control_diverter"))
            {
                Objective("Control 3: set the archive diverter (X)" + ControlsLine);
                yield return Interact(L.DiverterAt + new Vector3(1.2f, 0f, 0f), "Set the diverter", "the archive diverter", "The diverter lever in the west works", new[] { new Vector3(-404f, 0f, 22f), L.DiverterAt + new Vector3(1.2f, 0f, 0f) });
                yield return Cutscene(true);
                if (DiverterLever != null) StartCoroutine(Spin(DiverterLever, Vector3.right, -80f, 0.5f));
                AudioKit.Play("mars_control_on", L.DiverterAt, 0.9f, 0f, AudioKit.Bus.Sfx, 40f);
                AsteroidState.DoStep("control_diverter"); if (ControlLamps != null && ControlLamps.Length > 2) Glow(ControlLamps[2], 1.8f);
                // the archive conveyor diverts successfully: the intact panels turn onto the storage loop
                if (ArchiveLine != null) { ArchiveLine.Stopped = false; ArchiveLine.Diverted = true; }
                yield return HallInsert("a36_hall_the_panels_turn_onto_the_storage_loop");
                Log($"control 3 (diverter): the intact archive panels turned onto the storage loop - Controls {ControlsDone} / 3");
                // the Machinist enters the central hall and begins overriding the changes
                if (Machinist != null) { Machinist.Place(L.HallDoor + new Vector3(-8f, 0f, -6f), Vector3.left); Machinist.WalkTo(L.TerminalAt + new Vector3(2.5f, 0f, 2.5f), 1.3f); }
                Shoot(L.TerminalAt + new Vector3(-9f, 4f, -10f), L.TerminalAt + new Vector3(8f, 2f, 2f), 50f, 0.01f);
                yield return Wait(1.8f);
                yield return Shot("a37_the_machinist_comes_to_override");
                yield return Cutscene(false);
            }
            SecurityActive(false);
            if (Security != null) foreach (var s in Security) if (s != null) s.gameObject.SetActive(false);
            // the west passage opens back into the hall
            SetZone("WestPass", true); if (WestShutter != null) StartCoroutine(WestShutter.Move(false, 1.6f));
            Objective("Stop him at the terminal - the west passage back to the hall");
        }

        // ------------------------------------------------------------------------------------------ AST_07: the caretaker's objection
        MachinistFight _fight;
        IEnumerator Objection()
        {
            SetZone("WestPass", true); if (WestShutter != null) WestShutter.Set(false);
            yield return GuideWalk(new[] { new Vector3(-396f, 0f, 27f), new Vector3(-396f, 0f, 46f), new Vector3(-396f, 0f, 72f), L.TerminalView + new Vector3(-6f, 0f, -4f) }, "back into the hall");
            yield return Cutscene(true);
            // he is at the override; she stands between him and the terminal
            if (Machinist != null) { Machinist.Stop(); Machinist.Place(L.TerminalAt + new Vector3(0f, 0f, 4.4f), Vector3.back); }
            Seven.Place(L.TerminalAt + new Vector3(0f, 0f, 1.6f), Vector3.forward); Seven.Lock(true);
            MusicSystem.Play("asteroid_machinist", 2f);
            yield return Line("AST_07_01");                       // You are interfering with restoration.
            yield return Line("AST_07_02");                       // I'm saving the archive.
            Gesture("point", 2.6f);                               // QF21 (8): his one restrained gesture - he reaches past her toward the override
            yield return Line("AST_07_03");                       // That material is required.
            yield return Line("AST_07_04");                       // For what? There's no one here using any of this.
            yield return Line("AST_07_05");                       // It must be ready when they return.
            if (Machinist != null) Machinist.Play("idle", 0.4f);  // his hand stops over the control
            // AST_07_06: hold on 7 - the answer changes how she sees him
            Shoot(HerHead + Seven.transform.forward * 1.2f + Vector3.down * 0.15f + Seven.transform.right * 0.2f, HerHead, 34f, 0.01f);
            yield return Wait(2.2f);
            yield return Shot("a38_hold_on_her_the_answer");
            yield return Line("AST_07_07");                       // You're waiting for them.
            yield return Line("AST_07_08");                       // Stand aside.
            yield return Line("AST_07_09");                       // I can't let you destroy what they left.
            Complete("AST_07_lines");
            _talkCalm = false; if (MRig != null) MRig.Calm = false;
            // the fight
            _fight = Machinist.GetComponent<MachinistFight>() ?? Machinist.gameObject.AddComponent<MachinistFight>();
            _fight.DebrisKit = DebrisMeshes;
            _fight.Setup(Machinist, MRig, Stations, Terminal, ScrapMat != null ? ScrapMat : Markers.DarkGloss(), CanisterMat != null ? CanisterMat : Markers.DarkGloss(), CanisterMesh);
            _fight.PlaceStart();
            if (Flat(P7 - L.HerFightStart).magnitude > 2f) Seven.Place(L.HerFightStart, Vector3.forward);
            _fight.UI = UI; _fight.AutoPilot = Auto;
            _fight.Beat = FightBeat;
            _fight.PhaseObjective = ph => ph == 0 ? "He wants to restrain you - make him waste an attack, stun him (RT), then the blades (X) on his glowing couplings, kicks (Y)"
                                       : ph == 1 ? "The other arm - stun him (RT), then cut (X) and kick (Y); jump the lane surges (A)"
                                       : "The override - read him, dodge, stun him, then cut and kick until he stops";
            yield return Cutscene(false);
            UI.Hint("Dodge B  ·  Jump A  ·  Block LT  ·  Arc blades RB, then X: cut  ·  Kick Y  ·  Telekinesis RT: stun him when he is open  ·  R3 / Tab: lock on", 10f);
            _fight.Begin(BossFight.Load("machinist"), Seven, Cam);
            if (Auto) Seven.DrawBlades(true);
            StartCoroutine(FightPrompts(_fight));
            StartCoroutine(FightBanter(_fight));
            yield return _fight.Run(Objective);
            if (DemoBoot.BossIdle) yield break;
            _fight.End();
            yield return Shot("a42_he_is_dazed_her_takedown");
            yield return CradleEnd();
            if (DemoBoot.BossCheck && DemoBoot.CaptureDir != null) Cap.Finish("boss check (Round 16): the Machinist's fight done - " + _fight.Summary());
        }
        IEnumerator FightPrompts(BossFight f)
        {
            while (f != null && !f.Over && f.isActiveAndEnabled)
            {
                if (!_cut) { var l = f.PromptLabel; SetPrompt(l, f.PromptIsTk ? ControlsLegend.Btn.Tk : l == "Takedown" ? ControlsLegend.Btn.Takedown : ControlsLegend.Btn.Interact); }
                yield return null;
            }
            SetPrompt(null);
        }
        /// <summary>Panther during the fight: the "enormous arm" exchange; the hint only if she has not stunned him for a while.</summary>
        IEnumerator FightBanter(MachinistFight f)
        {
            for (float t = 0f; t < 5f; t += Time.deltaTime) yield return null;
            yield return SayIds("AST_07_10", "AST_07_11");     // The clamp beside you. It's made for his arm. / Yes, I noticed the enormous arm!
            float since = Time.time; int st = f.Staggers;
            while (!f.Over)
            {
                if (f.Staggers != st) { st = f.Staggers; since = Time.time; }
                if (Time.time - since > 35f && !f.Paused) { yield return SayIds("AST_07_P1"); since = Time.time + 60f; }
                yield return null;
            }
        }
        IEnumerator FightBeat(int ended)
        {
            // a short beat from the side: he reels into a service station; its clamp locks his tool arm
            var st = Stations != null && ended < Stations.Length ? Stations[ended] : null;
            var at = st != null ? st.transform.position : L.MachinistStart;
            Shoot(at + new Vector3(8f, 4f, at.z < 95f ? 7f : -7f), at + Vector3.up * 2.2f, 50f, 0.3f);
            yield return Wait(0.4f);
            if (ended == 0) yield return SayIds("AST_07_12", "AST_07_13");   // I meant the other enormous arm. / A little more specific next time!
            else yield return Wait(1.6f);
            yield return Shot($"a40_phase{ended + 1}_ends_the_clamp_locks_his_arm");
            FollowCamera();
        }
        /// <summary>The final action: her takedown engages the maintenance cradle - he is immobilized, not destroyed.</summary>
        IEnumerator CradleEnd()
        {
            yield return Cutscene(true);
            var mp = Machinist.transform.position;
            Shoot(mp + new Vector3(7f, 3.5f, -8f), mp + Vector3.up * 2.4f, 52f, 0.01f);
            yield return UI.Fade(1f, 0.4f, 1f);
            // the cradle: he is guided back into it and its padded arms close round him
            Machinist.Place(L.CradleAt, Vector3.back); Machinist.Play("dazed", 0.2f);
            if (Cradle != null) Cradle.Set(false);
            Seven.Place(L.CradleAt + new Vector3(-1.5f, 0f, -5f), Vector3.forward); Seven.Lock(true); Seven.DrawBlades(false);
            Shoot(L.CradleAt + new Vector3(6f, 2.6f, -9f), L.CradleAt + Vector3.up * 2.6f, 48f, 0.01f);
            yield return UI.Fade(0f, 0.5f, 1f);
            if (Cradle != null) yield return Cradle.Close(0.8f);
            yield return Shot("a43_the_maintenance_cradle_holds_him");
            if (MRig != null) MRig.LookAt = HerHead;
            MachinistUp();
            Machinist.Play("idle", 0.2f); yield return Wait(0.3f); Machinist.Play("dazed", 0.3f);      // he tries once to move, then stops
            StartCoroutine(RestrainAfter(0.6f));                  // QF21 (11): from here he is held still in the cradle
            yield return SayIds("AST_07_14");                     // Maintenance interruption.
            HerFromHisSide();                                     // QF21 (7): her line from his side, pulled back - not his armour
            yield return SayIds("AST_07_15");                     // Yes. You need one.
            MusicSystem.Play("asteroid_city", 4f);
            Complete("AST_07");
            Log("the Machinist is immobilized in his maintenance cradle (not destroyed); his two tool assemblies are locked in the service stations");
            // the hall's security lets go: the shutters lift, the temple archive is open
            SetZone("HallDoorZ", true); SetZone("MaintHatchZ", true); SetZone("Archive", true);
            if (HallDoorShutter != null) StartCoroutine(HallDoorShutter.Move(false, 2f));
            if (HatchShutter != null) StartCoroutine(HatchShutter.Move(false, 2f));
            if (ArchiveShutter != null) StartCoroutine(ArchiveShutter.Move(false, 2.4f));
            yield return Cutscene(false);
        }

        // ------------------------------------------------------------------------------------------ AST_08: what the machines were for
        IEnumerator ArchiveVision()
        {
            SetZone("Archive", true); if (ArchiveShutter != null && ArchiveShutter.Closed) StartCoroutine(ArchiveShutter.Move(false, 2f));
            Objective("Open the temple archive - through the west door");
            yield return GuideWalk(new[] { L.ArchiveDoor + new Vector3(4f, 0f, 0f), L.ArchiveDoor + new Vector3(-6f, 0f, 0f) }, "into the temple archive");
            yield return Cutscene(true);
            // the archive's central mechanism round the rune; the empty mounting points show what has been lost
            Shoot(L.ArchiveDoor + new Vector3(-5f, 2.6f, 5f), L.RuneAt + new Vector3(0f, 1f, -2f), 54f, 0.01f);
            yield return Wait(1.2f);
            yield return Shot("a44_the_archive_mechanism_the_empty_mountings");
            yield return Cutscene(false);
            Objective("Go to the rune");
            ShowGuide(L.RuneView);
            if (Auto) StartCoroutine(WalkSeven(L.RuneView, 30f));
            yield return Until(() => Flat(P7 - L.RuneView).magnitude < 1.4f || P7.x < L.RuneView.x, "reach the rune", 60f, "The glowing cuboctahedron at the heart of the archive", L.RuneView, () => Seven.Place(L.RuneView, Vector3.left));
            HideGuide();
            yield return Cutscene(true);
            { var rf = new Vector3(L.RuneAt.x, 0f, L.RuneAt.z); Seven.Place(rf + Vector3.right * 0.95f, Vector3.left); Seven.Lock(true); }
            Shoot(L.RuneView + new Vector3(0.6f, 1.6f, 2.2f), L.RuneAt, 38f, 0.01f);
            yield return Wait(0.5f);
            yield return Shot("a45_she_touches_the_rune");
            MusicSystem.Stop(1.5f);
            System.Action<float> runeGlow = k => { if (Rune != null) foreach (var rr in Rune.GetComponentsInChildren<Renderer>()) if (rr.name == "Edges") Glow(rr, 1.1f * k); };
            var runeAt = Rune != null ? Rune.position : L.RuneAt;
            yield return VisionTouch.Into("asteroid", Seven, Cam, UI, runeAt, 0.32f, runeGlow);
            yield return Vision5Play();
            yield return VisionTouch.Back("asteroid", Seven, Cam, runeAt, 0.32f, runeGlow);
            yield return Shot("a46_insert_her_fingers_lift_from_the_rune");
            Shoot(P7 + new Vector3(-0.4f, 1.7f, 1.9f), L.RuneAt + Vector3.up * 0.2f, 44f, 0.5f);
            yield return Say("AST_08", 1, 4);                    // Seven? / They stopped the line. / What line? / All of this. Just to mend a little bird.
            if (AsteroidState.BirdSeen) yield return Say("AST_08", 5, 6);                       // The one you found? / I think so.
            else yield return SayIds("AST_08_05S", "AST_08_05T");                               // A bird? / A toy. Someone loved it.
            Seven.transform.rotation = Quaternion.LookRotation(Vector3.right);                 // she looks back toward the workshop passage
            Shoot(HerHead + Vector3.right * 2.4f + Vector3.forward * 0.7f + Vector3.down * 0.1f, HerHead - Vector3.up * 0.2f, 36f, 0.6f);   // QF21: a clear medium shot
            yield return Say("AST_08", 7, 7);                    // I knew what it would sound like. Before it moved.
            yield return Wait(1.6f);                              // hold the shot
            yield return Shot("a47_i_knew_what_it_would_sound_like");
            Complete("AST_08");
            yield return Cutscene(false);
        }
        UnityEngine.Video.VideoPlayer _vp5; RenderTexture _vp5RT;
        IEnumerator Vision5Play()
        {
            // silent: the machine winding down, tool sounds, the bird's uneven chirp and wing flap (the same sound as AST_03_08)
            MusicSystem.Suspend(1.2f);
            AudioKit.Play("vision_whoosh", null, 0.9f);
            var shimmer = AudioKit.Loop("vision_shimmer", null, null, 0f, AudioKit.Bus.Sfx);
            if (shimmer != null) StartCoroutine(AudioKit.Fade(shimmer, 0.35f, 1.5f));
            yield return UI.Fade(1f, 0.9f, 1f);
            bool video = false;
            if (Vision5 != null && !DemoBoot.NoVideo)
            {
                var vgo = new GameObject("Vision5Video");
                _vp5 = vgo.AddComponent<UnityEngine.Video.VideoPlayer>();
                _vp5.playOnAwake = false; _vp5.isLooping = false; _vp5.clip = Vision5; _vp5.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
                _vp5RT = new RenderTexture((int)Vision5.width, (int)Vision5.height, 0); _vp5.targetTexture = _vp5RT;
                _vp5.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None; _vp5.timeUpdateMode = UnityEngine.Video.VideoTimeUpdateMode.GameTime;
                _vp5.Prepare();
                for (float t = 0f; t < 3f && !_vp5.isPrepared; t += Time.deltaTime) yield return null;
                video = _vp5.isPrepared;
            }
            float aspect = Vision5 != null ? Vision5.width / (float)Mathf.Max(1u, Vision5.height) : 16f / 9f;
            var hum = AudioKit.Loop("ast_amb_factory", null, null, 0.25f, AudioKit.Bus.Sfx);
            float len = video ? (float)Vision5.length : 9f;
            if (video)
            {
                Log($"vision 5: video {Vision5.name} ({Vision5.length:0.0}s) full-screen; the machine winding down, tool sounds, the bird's chirp - no music");
                _vp5.time = 0; _vp5.Play();
                for (float t = 0f; t < 0.7f; t += Time.deltaTime) { UI.SetVideo(_vp5RT, aspect, t / 0.7f); yield return null; }
                UI.SetVideo(_vp5RT, aspect, 1f);
            }
            else
            {
                var still = Resources.Load<Texture2D>("Asteroid/vision_05_still");
                Log($"vision 5: VIDEO NOT AVAILABLE - still fallback {(still != null ? "shown" : "missing")}");
                if (still != null) for (float t = 0f; t < 0.7f; t += Time.deltaTime) { UI.SetVideo(still, still.width / (float)still.height, t / 0.7f); yield return null; }
            }
            bool shot = false, chirp = false, wound = false;
            for (float vt0 = Time.time; Time.time - vt0 < len - 0.12f && (!video || _vp5.isPlaying || Time.time - vt0 < 1f);)
            {
                float e = Time.time - vt0;
                if (!wound && e > len * 0.3f) { wound = true; if (hum != null) StartCoroutine(AudioKit.Fade(hum, 0.05f, 2.5f)); AudioKit.Play("ast_power_down", null, 0.5f); }    // the great machine stops
                if (!chirp && e > len * 0.62f) { chirp = true; AudioKit.Play("ast_bird_chirp", null, 0.9f); }                                                              // the bird flutters unevenly
                if (!shot && e > 4f) { shot = true; yield return Shot(video ? "a45b_vision_video_frame" : "a45b_vision_still_fallback"); }
                if (e > 0.8f && SkipButton()) break;
                yield return null;
            }
            if (_vp5 != null) _vp5.Pause();
            if (shimmer != null) StartCoroutine(AudioKit.Fade(shimmer, 0f, 1.0f, true));
            if (hum != null) StartCoroutine(AudioKit.Fade(hum, 0f, 1.0f, true));
            UI.SetVideo(null, aspect, 0f);
            if (_vp5 != null) { _vp5.Stop(); Destroy(_vp5.gameObject); }
            yield return UI.Fade(0f, 1.2f, 1f);
        }

        // ------------------------------------------------------------------------------------------ AST_09: a different instruction
        IEnumerator Instruction()
        {
            MusicSystem.Stop(2f);
            if (Machinist != null) { Machinist.Place(L.CradleAt, Vector3.back); Machinist.Play("dazed", 0f); }
            if (Cradle != null) Cradle.Set(true);
            if (MRig != null && !MRig.Restrained) StartCoroutine(RestrainAfter(0.8f));   // QF21 (11b): locked in the cradle
            Objective("Back to the Machinist");
            yield return GuideWalk(new[] { L.ArchiveDoor + new Vector3(5f, 0f, 0f), L.CradleAt + new Vector3(-4.5f, 0f, -4f) }, "back to the restrained Machinist");
            yield return Cutscene(true);
            { var at = new Vector3(L.MemorySurface.x + 2.4f, 0f, L.MemorySurface.z - 3.6f); Seven.Place(at, Flat(L.MemorySurface - at)); Seven.Lock(true); }
            if (MRig != null) MRig.LookAt = L.MemorySurface;     // QF21 (11b): no idle clip - only his head turns to the image
            // she shows the recovered workshop memory on the archive surface beside him
            yield return MemoryOn();
            // CAMERA: the enormous caretaker beside the tiny recorded bird
            Shoot(new Vector3(-343.5f, 3.2f, 111.5f), new Vector3(-348.5f, 2.6f, 125.6f), 50f, 0.01f);
            yield return Wait(1.2f);
            yield return Shot("a48_the_caretaker_beside_the_recorded_bird");
            yield return Line("AST_09_01");                      // That assembly was discontinued.
            yield return Line("AST_09_02");                      // Someone kept repairing it.
            yield return Line("AST_09_03");                      // Replacement would have been more efficient.
            yield return Line("AST_09_04");                      // Mm. But they wanted that one.
            Shoot(L.MemorySurface + new Vector3(-1f, -0.4f, -4.5f), L.MemorySurface, 36f, 0.4f);
            MemoryPhase(1);                                      // AST_09_05: the recorded production line stops (on the surface)
            yield return Wait(2.4f);
            yield return Shot("a49_the_recorded_line_stops");
            yield return Wait(0.8f);                             // a longer pause than before
            MemoryPhase(2);                                      // the bird flutters on the surface while they talk
            if (MRig != null) MRig.Twitch();                     // QF21 (11b): the claw twitches once
            yield return Line("AST_09_06");                      // They interrupted production.
            yield return Line("AST_09_07");                      // They were allowed to. It belonged to them.
            if (MRig != null) MRig.LookAt = HerHead;             // he turns from the image to her
            yield return Line("AST_09_08");                      // What is required of me?
            yield return Line("AST_09_09");                      // Keep what's left safe. The records. The things they made.
            yield return Line("AST_09_10");                      // And the restoration?
            yield return Line("AST_09_11");                      // Start there.
            Complete("AST_09_lines");
            _talkCalm = false;
            Log(ClampSummary());
            MemoryOff();
            yield return Cutscene(false);
            // GAMEPLAY: she sets the facility to preservation mode (essential power and the stabilizers stay; production stops)
            Objective("Set the facility to preservation mode (X at the terminal)");
            yield return Interact(L.TerminalView, "Preservation mode", "preservation mode", "The override terminal in the middle of the hall", new[] { L.TerminalView });
            yield return Cutscene(true);
            Shoot(L.TerminalAt + new Vector3(2.2f, 1.9f, -2.4f), L.TerminalAt + Vector3.up * 1.2f, 40f, 0.01f);
            if (TerminalScreen != null) Glow(TerminalScreen, 0.6f);
            AstProduction.Target = 0.04f;
            AudioKit.Play("ast_power_down", L.TerminalAt, 1f, 0f, AudioKit.Bus.Sfx, 200f);
            if (_factory != null) StartCoroutine(AudioKit.Fade(_factory, 0f, 6f));
            if (_quiet != null) StartCoroutine(AudioKit.Fade(_quiet, 0.5f, 6f));
            StartCoroutine(DimProduction(6f));
            yield return Wait(0.8f);
            MachinistUp();
            yield return SayIds("AST_09_P1");                    // Preservation mode. Essential systems only.
            // released only now, after he accepts the task: the cradle opens; he opens protected storage for the surviving panels
            // QF21 (11): the padded arms open visibly first, then he moves; the camera goes with him to protected storage
            // (keeping 7 in the shot where it can) - never a hold on the empty cradle
            Shoot(L.CradleAt + new Vector3(7.5f, 2.8f, -8.5f), L.CradleAt + Vector3.up * 3f, 50f, 0.01f);
            if (Cradle != null) yield return Cradle.OpenUp(1.4f);
            yield return Shot("a49b_the_cradle_opens_first");
            yield return Wait(0.5f);
            if (MRig != null) MRig.Restrained = false;
            if (Machinist != null) { yield return TrackWalk(Machinist, L.StorageAt + new Vector3(4f, 0f, -3f), 1.1f, "a49c_he_walks_to_protected_storage", P7, 6f, 7f); Machinist.Face(L.StorageAt); Gesture("point", 2.4f); }
            AudioKit.Play("ast_vault_open", L.StorageAt, 1f, 0f, AudioKit.Bus.Sfx, 80f);
            if (StorageDoor != null) StartCoroutine(SwingDoor(StorageDoor, -95f, 2.6f));
            yield return SayIds("AST_09_P2");                    // Protected storage is open.
            StartCoroutine(PanelsIntoStorage());
            yield return Wait(1.6f);
            yield return Shot("a50_protected_storage_open_the_panels_go_in");
            // the record and the cuboctahedron awarded (once)
            bool first = AsteroidState.AwardRecord();
            Seven.Place(L.CradleAt + new Vector3(-6f, 0f, -2f), Vector3.left); Seven.Lock(true);
            if (RecordCubo != null)
            {
                RecordCubo.gameObject.SetActive(true);
                var fw = Flat(Seven.transform.forward).normalized;
                var lp = P7 + Vector3.up * 1.22f + fw * 0.4f - Seven.transform.right * 0.08f;
                var tik = Seven.gameObject.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
                tik.enabled = true; tik.Ease = 3f; tik.LeftPos = lp; tik.LeftW = 1f; tik.LookPos = lp; tik.LookW = 0.8f;
                yield return Wait(0.7f);
                RecordCubo.position = lp + Vector3.up * 0.07f;
                Shoot(lp + fw * 0.75f + Vector3.up * 0.18f + Seven.transform.right * 0.2f, lp + Vector3.up * 0.04f, 28f, 0.01f);
                yield return Wait(0.6f);
                yield return Shot("a51_insert_the_small_cuboctahedron_and_the_record");
                tik.ReleaseAll("the asteroid record insert");
                RecordCubo.gameObject.SetActive(false);
            }
            Log($"the asteroid's record + its cuboctahedron: awarded {(first ? "now (first time)" : "ALREADY - no second award")} (awards {AsteroidState.RecordAwards}, must be 1)");
            // WIDE: most factory lines slow to a stop; essential lights remain; for the first time, the cavern is quiet
            Shoot(new Vector3(-296f, 9f, 128f), new Vector3(-180f, -10f, 40f), 62f, 0.01f);
            yield return Wait(3.2f);
            yield return Shot("a52_wide_the_cavern_falls_quiet");
            Log($"preservation mode: production rate {AstProduction.Rate:0.00} -> {AstProduction.Target:0.00} (essential machinery + lights kept)");
            Complete("AST_09");
            yield return Home();
        }
        IEnumerator DimProduction(float secs)
        {
            var i0 = new float[ProductionLights != null ? ProductionLights.Length : 0];
            for (int i = 0; i < i0.Length; i++) if (ProductionLights[i] != null) i0[i] = ProductionLights[i].intensity;
            for (float t = 0f; t < 1f; t += Time.deltaTime / secs)
            {
                for (int i = 0; i < i0.Length; i++) if (ProductionLights[i] != null) ProductionLights[i].intensity = Mathf.Lerp(i0[i], i0[i] * 0.12f, t);
                if (ProductionLamps != null) foreach (var r in ProductionLamps) Glow(r, Mathf.Lerp(1.4f, 0.12f, t));
                yield return null;
            }
        }
        IEnumerator PanelsIntoStorage()
        {
            if (StoredPanels == null) yield break;
            foreach (var p in StoredPanels)
            {
                if (p == null) continue;
                var a = p.position; var b = L.StorageAt + new Vector3(-1f, 1f, 0f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f) { p.position = Vector3.Lerp(a, b, t); yield return null; }
                p.gameObject.SetActive(false);
            }
            if (ArchiveLine != null) foreach (var p in ArchiveLine.Panels) if (p != null) p.gameObject.SetActive(false);
        }
        // ------------------------------------------------------------------------------------------ home: the saucer comes in, the beam
        IEnumerator Home()
        {
            yield return Cutscene(true);
            yield return SayIds("AST_09_P3");                    // I'm bringing the ship in.
            Checkpoint(L.CpHome);
            // the saucer glides down through the quiet cavern and holds over the plaza by the hall
            var land = L.SaucerHome; var from = land + new Vector3(140f, 60f, -60f);
            if (Saucer != null) Saucer.position = from;
            Shoot(new Vector3(-292f, 4f, 132f), from, 56f, 0.01f);
            AudioKit.Play("saucer_descent", null, 0.6f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 6f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                if (Saucer != null) Saucer.position = Vector3.Lerp(from, land, u);
                Cam.Shot(new Vector3(-292f, 4f, 132f), Saucer != null ? Saucer.position + Vector3.up * 2f : land, 1.4f, false, 56f);
                yield return null;
            }
            yield return Shot("a53_the_saucer_comes_in_over_the_plaza");
            // 7 at the plaza; the bike rises back into the bike hold; she boards in the beam (no ramp)
            Seven.Place(land + new Vector3(-2.5f, 0f, 0f), Vector3.right);
            Bike.Place(L.HallPlazaStop + new Vector3(0f, 0f, -8f), 90f);
            yield return HoldBikeIn();
            if (SaucerBeam != null) { yield return SaucerBeam.Iris(true); yield return SaucerBeam.BeamOn(true); }
            yield return WalkSeven(land, 6f);
            Seven.Lock(true); Seven.BeginFloat();
            bool s = false;
            Shoot(land + new Vector3(14f, 4f, 12f), land + new Vector3(0f, 2.5f, 0f), 50f, 0.01f);
            if (SaucerBeam != null) yield return SaucerBeam.Rise(Seven.transform, 3.8f, 40f, t => { if (!s && t > 0.45f) { s = true; StartCoroutine(Shot("a54_she_boards_in_the_beam")); } });
            if (SaucerBeam != null) yield return SaucerBeam.Open(false);
            yield return UI.Fade(1f, 0.6f);
            ChapterDone = true;
            Log("the Asteroid (AST_02-AST_09 in the Asteroid scene) done - aboard: the reunion straight from the beam-up, the chair talk, the next destination");
            AsteroidState.HomeAboard = true; Chapter1State.Aboard = true;
            SceneManager.LoadScene("Saucer");
        }
        IEnumerator HoldBikeIn()
        {
            if (Hold == null) { Bike.gameObject.SetActive(false); yield break; }
            var dir = Hold.Dir; var tan = Vector3.Cross(Vector3.up, dir); var mouth = Hold.Mouth;
            float yawOut = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, bottomIn = Hold.transform.position.y + Hold.BottomIn;
            if (Riding) Bike.DismountTo(P7 + Vector3.left * 2f, Vector3.right);
            var settle = Bike.transform.position; float settleBottom = settle.y + Bike.HoverHeight - 0.12f;
            Shoot(mouth + dir * 7.5f + tan * 4.2f + Vector3.up * 0.4f, (mouth + settle) * 0.5f, 46f, 0.6f);
            yield return Wait(0.6f);
            yield return Hold.Open();
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.4f)
            { float u = Mathf.SmoothStep(0f, 1f, t); Bike.Glide(Vector3.Lerp(settle, Hold.Outside, u), Mathf.LerpAngle(Bike.transform.eulerAngles.y, yawOut + 180f, Mathf.Clamp01(t * 1.5f)), Mathf.Lerp(settleBottom, bottomIn, u)); yield return null; }
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.4f)
            { float u = Mathf.SmoothStep(0f, 1f, t); Bike.Glide(Vector3.Lerp(Hold.Outside, Hold.Inside, u), yawOut + 180f, bottomIn); yield return null; }
            yield return Hold.Close();
            ShowBike(false); Bike.EndGlide();
            Bike.gameObject.SetActive(false);
            Log("going home the bike rose back into the bike hold; it closed seamlessly (7 boards in the beam)");
        }

        // ------------------------------------------------------------------------------------------ the fog round the action
        float _fog0Dist = -1f, _fog0Max, _fog0Ground, _fog0Far, _fog0Zen;
        IEnumerator FogThin(float k, float secs = 1.2f)
        {
            if (Atmo == null) yield break;
            if (_fog0Dist < 0f) { _fog0Dist = Atmo.DistanceDensity; _fog0Max = Atmo.MaxFog; _fog0Ground = Atmo.GroundDensity; _fog0Far = Atmo.FarBrightness; _fog0Zen = Atmo.ZenithBrightness; }
            float d0 = Atmo.DistanceDensity, m0 = Atmo.MaxFog, g0 = Atmo.GroundDensity;
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, secs))
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Atmo.DistanceDensity = Mathf.Lerp(d0, _fog0Dist * (1f - 0.55f * k), u); Atmo.MaxFog = Mathf.Lerp(m0, _fog0Max * (1f - 0.25f * k), u);
                Atmo.GroundDensity = Mathf.Lerp(g0, _fog0Ground * (1f - 0.5f * k), u);
                yield return null;
            }
        }
    }
}
