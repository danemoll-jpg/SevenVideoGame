using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using L = Seven.BiavehLayout;

namespace Seven
{
    /// <summary>Round 17 — the mothership: BIA_09 (cornering Ratwing: the fight, then stage three - the compartment and
    /// Panther's timing), BIA_10 (restoring the archive), BIA_11 (the octahedron, the vision) and the way home.</summary>
    public partial class BiavehDirector
    {
        RatwingFight _fight;
        public int Mistimed { get; private set; }
        public float SealedAt { get; private set; } = -1f;

        // ------------------------------------------------------------------------------------------ BIA_09
        IEnumerator Cornering()
        {
            FreeBike = true;
            if (!BiavehState.SceneDone("BIA_09_lines"))
            {
                Objective("To the temple's transfer chamber - Ratwing is at the archive");
                if (!Riding) yield return GetBackOn();
                // QF23: if the bike stands inside the passage entry (it rolled through the doorway when parked), the way out runs
                // back through the middle of the doorway first - a diagonal from in there catches the doorway's jamb
                var route = L.RideToChamber;
                if (Riding && Bike.transform.position.x < 209f && Mathf.Abs(Bike.transform.position.z - 66f) < 5f) route = new[] { new Vector3(214f, 0f, 66f) }.Concat(L.RideToChamber).ToArray();
                yield return RideRoute(route, "to the transfer chamber", 6f);
                // ARRIVAL: she brakes the bike behind cover; Ratwing turns from the damaged entrance
                yield return StopBike(1.6f);
                if (Riding) { if (!Bike.Dismount()) Bike.Dismount(true); }
                yield return Cutscene(true);
                if (Ratwing != null) { Ratwing.gameObject.SetActive(true); SetRat(new Vector3(L.ArchiveEntrance.x, 0f, L.ArchiveEntrance.z - 3f), 0f, RatwingRig.Mode.Perched, Vector3.forward); }
                Seven.Place(L.ChamberCover, Vector3.left); Seven.Lock(true);
                Shoot(L.ChamberCover + new Vector3(2.2f, 1.5f, -2f), L.ArchiveEntrance + Vector3.up * 2f, 46f, 0.01f);
                yield return Wait(0.6f);
                if (Ratwing != null) StartCoroutine(TurnRat(P7, 0.8f));
                yield return Wait(0.8f);
                yield return Shot("b34_the_transfer_chamber_ratwing_at_the_damaged_entrance");
                yield return Say("BIA_09", 1, 1);                // Do you have any idea what's in there?
                Seven.Place(L.HerFightStart, Flat(L.ArchiveEntrance - L.HerFightStart));
                TwoShot((L.HerFightStart + Ratwing.position) * 0.5f, Vector3.right, 8f, 1.8f, 2.2f, 48f, 0.4f);
                yield return Say("BIA_09", 2, 2);                // Enough to know it isn't yours.
                if (RRig != null) RRig.Now = RatwingRig.Mode.Fly;     // he spreads his wings
                yield return Say("BIA_09", 3, 4);                // They leave it sitting here. I could make it worth something. / It already is.
                // QF23 (19): the partitions explained before the fight
                Shoot(L.Partitions[0] + new Vector3(-10f, 6f, -10f), L.Partitions[0] + Vector3.up * 6f, 52f, 0.4f);
                yield return SayIds("BIA_09_N1");                // Our partitions can wall off sections of this shaft...
                yield return Shot("b35a_the_partitions_explained");
                yield return Cutaway("b35b_aboard_a_smaller_room", AboardCut.Mood.Hunt, "him", 0.3f, "BIA_09_N2");   // A smaller room. My favourite kind of hunt.
                BiavehState.CompleteScene("BIA_09_lines");
                yield return Cutscene(false);
            }
            // the fight (the Decisions): his passes, the throw that brings him down, her strikes, the partitions
            MusicSystem.Play("biaveh_ratwing", 1.5f);
            StartCoroutine(FogThin(0.8f, 1.5f));                 // the shaft: clear enough to read him in the air
            _fight = Ratwing.GetComponent<RatwingFight>() ?? Ratwing.gameObject.AddComponent<RatwingFight>();
            _fight.HomeOf = Home; _fight.Setup(RRig, ChamberPartitions, StationLamps, DebrisMeshes, DebrisMat);
            _fight.PlaceStart();
            if (Flat(P7 - L.HerFightStart).magnitude > 2f) Seven.Place(L.HerFightStart, Vector3.forward);
            _fight.UI = UI; _fight.AutoPilot = Auto;
            _fight.Beat = PhaseBeatShot;
            _fight.PhaseObjective = ph => ph == 0 ? "He swoops and claws - dodge (B), then knock him down with telekinesis (RT) and fight him on the floor (X / Y). Close the first partition (X at its station)"
                                       : ph == 1 ? "His space is smaller - keep him coming down. The second partition (X at its station)"
                                       : "Wear him out - dodge, bring him down, fight";
            // QF23 (17): the controls box at the start of the fight is gone - small prompts at the moment instead (BossFight.Tip)
            _fight.Begin(BossFight.Load("ratwing"), Seven, Cam);
            if (Auto && Seven.BladesOwned) Seven.DrawBlades(true);
            StartCoroutine(FightPrompts(_fight));
            _fight.Moment += FightMoment; StartCoroutine(StationWatch(_fight));
            var run = StartCoroutine(_fight.Run(s => { if (s != null && !s.StartsWith("He is dazed")) Objective(s); }));
            // exhausted (his stamina spent) he FLEES - stage three instead of a takedown
            while (!_fight.Over && !_fight.Dazed) yield return null;
            if (DemoBoot.BossIdle) yield break;
            StopCoroutine(run);
            _fight.Moment -= FightMoment; _fight.StationBeam(false);
            Log(_fight.Summary());
            Log($"RATWING FIGHT (QF23 19, 21): Panther's stun calls {_stunCalls}, partition calls {_partCalls}, station beams {_beams}; arc-blade shocks {_fight.BladeShocks}, swoop counters (shocked out of the air) {_fight.SwoopCounters}; fight tips shown {_fight.TipsShown}");
            _fight.End(); _fight.enabled = false; UI.HideBossBar();
            yield return Shot("b35_his_stamina_spent_he_flees");
            yield return StageThree();
            if (DemoBoot.BossCheck && DemoBoot.CaptureDir != null) { Cap.Finish("boss check (Round 17): Ratwing contained - " + _fight.Summary() + $"; CONTAINMENT: mistimed tries {Mistimed}, sealed at {SealedAt:0.0} s after stage three began"); yield break; }
        }
        IEnumerator TurnRat(Vector3 at, float secs) { var a = Ratwing.rotation; var b = Quaternion.LookRotation(Flat(at - Ratwing.position)); for (float t = 0f; t < 1f; t += Time.deltaTime / secs) { Ratwing.rotation = Quaternion.Slerp(a, b, t); yield return null; } }
        IEnumerator FightPrompts(BossFight f)
        {
            while (f != null && !f.Over && !f.Dazed && f.isActiveAndEnabled)
            {
                if (!_cut) { var l = f.PromptLabel; SetPrompt(l, f.PromptIsTk ? ControlsLegend.Btn.Tk : ControlsLegend.Btn.Interact); }
                yield return null;
            }
            SetPrompt(null);
        }
        IEnumerator PhaseBeatShot(int ended)
        {
            // the partition she just closed, from the side: his space is smaller now
            var p = L.Partitions[Mathf.Clamp(ended, 0, 1)];
            Shoot(p + new Vector3(-12f, 6f, ended == 0 ? 12f : -12f), p + Vector3.up * 4f, 54f, 0.3f);
            yield return Wait(1.6f);
            yield return Shot($"b36_phase{ended + 1}_ends_a_partition_shrinks_his_space");
            FollowCamera();
        }

        /// <summary>Stage three (the script): he appears to retreat - Panther reads him circling toward the last service
        /// opening. Seal the outer barrier only once he is INSIDE the empty maintenance compartment (the barrier never closes
        /// on his body or wings: in the doorway it stops and he slips out - the chance resets; outside, he sees it and circles
        /// again). A second barrier then seals behind him: contained, alive, in a spacious enclosure.</summary>
        IEnumerator StageThree()
        {
            yield return Cutscene(true);
            SetZone("Compartment", false);
            Seven.Place(L.CoverRight + new Vector3(1.2f, 0f, -2f), Vector3.left); Seven.Lock(true); Seven.PlayAnim("crouch_idle", 0.3f);
            if (RRig != null) { RRig.Now = RatwingRig.Mode.Fly; }
            StartCoroutine(FlyRat(new Vector3(175f, 0f, 135f), 9f, 1.8f, RatwingRig.Mode.Fly));
            Shoot(L.CoverRight + new Vector3(2.4f, 1.2f, -2.6f), L.OuterBarrier + Vector3.up * 2.5f, 46f, 0.01f);
            yield return Say("BIA_09", 5, 7);                    // Don't follow him. Watch the opening on your right. / I thought following him was the plan. / It was an early draft.
            yield return Shot("b37_the_opening_on_her_right_the_empty_compartment");
            Seven.Release();
            yield return Cutscene(false);
            Objective("Wait for him to go into the compartment - then seal it (X at the barrier control)");
            ShowGuide(L.BarrierControlView);
            if (Auto) yield return WalkSeven(L.BarrierControlView, 10f);
            float t0 = Time.time; bool sealedOk = false; int cycle = 0; bool spokeBack = false, demoMistime = Auto;
            while (!sealedOk)
            {
                cycle++;
                // circle the shaft (high), then come back down toward the opening, in, a moment inside, out again
                // QF23 (19): the closed partitions leave him very little room - his only way runs past the compartment, so he
                // goes there within a few seconds (one short arc, not laps of the shaft)
                yield return FlyRat(cycle == 1 ? new Vector3(172f, 0f, 132f) : new Vector3(166f, 0f, 128f), 6f, 1.3f, RatwingRig.Mode.Fly);
                if (!spokeBack) { spokeBack = true; StartCoroutine(SayIds("BIA_09_08", "BIA_09_09")); }      // There. He's coming back. / I see him.
                // the doorway (the barrier must not close now)
                _ratPhase = 1; StartCoroutine(SayIds("BIA_09_10"));                                             // Wait...
                yield return FlyRat(L.OuterBarrier + new Vector3(0.4f, 0f, 0f), 2.2f, 1.3f, RatwingRig.Mode.Fly);
                yield return WaitPress(0.7f, 1);
                if (_pressed == 1) { yield return Mistime("in the doorway - the barrier stops for him"); continue; }
                // inside the compartment: NOW
                _ratPhase = 2; StartCoroutine(SayIds("BIA_09_11"));                                             // Now.
                yield return FlyRat(L.CompartmentIn, 1.4f, 0.9f, RatwingRig.Mode.Fly);
                yield return WaitPress(Auto && demoMistime ? 0.1f : 2.6f, 2);
                if (Auto && demoMistime) { demoMistime = false; yield return FlyRat(L.OuterBarrier + new Vector3(1.5f, 0f, 0f), 3f, 0.8f, RatwingRig.Mode.Fly); yield return Mistime("(check run: pressed while he flew back out through the doorway)"); continue; }
                if (_pressed == 2) { sealedOk = true; break; }
                // he leaves again (no press in time) - the chance comes round again
                _ratPhase = 0;
                Log("stage three: he came out of the compartment again (not sealed in time) - he circles round once more");
                if (cycle > 6 && !Auto) { UI.Hint("Seal it while he is INSIDE the compartment - after Panther's 'Now.'", 5f); }
                if (cycle > 8) { Log("stage three: SKIP (fairness) - sealed for her"); sealedOk = true; }
            }
            _ratPhase = 0; HideGuide(); SetPrompt(null);
            SealedAt = Time.time - t0;
            Log($"STAGE THREE (QF23 19): he fled -> sealed in {SealedAt:0.0} s (target well under 15 s), {cycle} pass(es), mistimed {Mistimed}");
            // the compartment seals safely; a second barrier behind him; he strikes the barrier, tests the other exit - both hold
            yield return Cutscene(true);
            Shoot(L.OuterBarrier + new Vector3(6f, 2.4f, -6f), L.CompartmentIn + Vector3.up * 1.5f, 48f, 0.01f);
            AudioKit.Play("bia_barrier", L.OuterBarrier, 1f, 0f, AudioKit.Bus.Sfx, 60f);
            if (OuterBarrier != null) StartCoroutine(Drop(OuterBarrier, 6f, 0f, 0.6f));
            yield return Wait(0.5f);
            AudioKit.Play("bia_barrier", L.InnerBarrier, 0.8f, 0f, AudioKit.Bus.Sfx, 60f);
            if (InnerBarrier != null) StartCoroutine(Drop(InnerBarrier, 6f, 0f, 0.6f));
            Glow(BarrierLamp, 1.8f);
            yield return Wait(0.6f);
            yield return Shot("b38_sealed_the_compartment_holds_him");
            if (Ratwing != null) { yield return FlyRat(L.OuterBarrier + new Vector3(-1.2f, 0f, 0f), 1.6f, 0.5f, RatwingRig.Mode.Fly); AudioKit.Play("ratwing_strike", L.OuterBarrier + Vector3.up * 1.6f, 1f, 0f, AudioKit.Bus.Sfx, 40f); CameraShake.Quake(0.2f, 0.2f); }
            if (Ratwing != null) { yield return FlyRat(L.InnerBarrier + new Vector3(1.2f, 0f, 0f), 1.6f, 0.8f, RatwingRig.Mode.Fly); AudioKit.Play("ratwing_strike", L.InnerBarrier + Vector3.up * 1.6f, 0.8f, 0f, AudioKit.Bus.Sfx, 40f); }
            if (Ratwing != null) { yield return FlyRat(L.CompartmentIn, 0f, 0.9f, RatwingRig.Mode.Perched); }
            yield return Shot("b39_he_tests_both_barriers_both_hold");
            Log($"BIA_09: RATWING CONTAINED alive in the maintenance compartment (outer + inner barrier) - stage three took {SealedAt:0.0} s, mistimed tries {Mistimed} (each reset the chance; the barrier never closed on him)");
            Seven.Release(); Seven.Lock(true);
            TwoShot(P7 + Vector3.left * 2f, Vector3.forward, 3.2f, 1.2f, 1.3f, 44f, 0.4f);
            yield return Say("BIA_09", 12, 15);                  // Ha! Got him. / Mm. / That was a very pleased noise. / A successful joint effort.
            // QF23 (11): the purr is playing - loud - BEFORE she mentions it, and carries over the link through both lines
            var purr = Purr.Loop(null, 1f); if (purr != null) purr.spatialBlend = 0f;
            yield return Cutaway("b40_aboard_purring", AboardCut.Mood.Purr, "him", 1.4f);
            Log("PURR (QF23 11): BIA_09_16 - " + Purr.Level());
            yield return Say("BIA_09", 16, 17);                  // You're purring. / The ship is vibrating.
            if (purr != null) StartCoroutine(AudioKit.Fade(purr, 0f, 1.5f, true));
            // Biavian security takes the enclosure (its lamp, the light along the barrier) - he remains alive, contained
            Shoot(L.OuterBarrier + new Vector3(5f, 3f, 5f), L.CompartmentIn + Vector3.up * 1.2f, 46f, 0.01f);
            Glow(BarrierLamp, 2.2f);
            // QF23: he is contained - the shaft's partitions rise again (the way out of the chamber is open)
            if (ChamberPartitions != null) foreach (var p in ChamberPartitions) if (p != null) StartCoroutine(Drop(p, 0f, 14f, 2.4f));
            AudioKit.Play("bia_partition", L.Partitions[0] + Vector3.up * 6f, 0.8f, 0f, AudioKit.Bus.Sfx, 120f);
            yield return Wait(1.0f);
            yield return Shot("b41_biavian_security_holds_the_enclosure");
            MusicSystem.Play("biaveh_mothership", 3f);
            StartCoroutine(FogThin(0f, 2f));
            Complete("BIA_09");
            yield return Cutscene(false);
        }
        int _ratPhase, _pressed;
        /// <summary>Panther has just said "Now." - he is inside the compartment (what a player hears).</summary>
        public bool SaidNow => _ratPhase == 2;
        /// <summary>Wait up to secs for X at the barrier control (in reach); _pressed = the phase it was pressed in (0 = none).</summary>
        IEnumerator WaitPress(float secs, int phase)
        {
            _pressed = 0;
            for (float t = 0f; t < secs; t += Time.deltaTime)
            {
                bool near = Flat(L.BarrierControlView - P7).magnitude < 2.2f && Seven.Current == ChapterSeven.Mode.Free;
                SetPrompt(near ? "Seal the compartment" : null);
                if (near && (Auto ? (phase == 2 && t > 0.6f) : Input.Interact.WasPressedThisFrame())) { _pressed = phase; yield break; }
                yield return null;
            }
        }
        IEnumerator Mistime(string why)
        {
            Mistimed++;
            Log($"stage three: MISTIMED ({Mistimed}x) - {why}; the barrier stops (never on his body or wings) and he slips out - the chance resets");
            AudioKit.Play("bia_barrier", L.OuterBarrier, 0.5f, 0f, AudioKit.Bus.Sfx, 40f);
            if (OuterBarrier != null) { StartCoroutine(Drop(OuterBarrier, 6f, 3.2f, 0.3f)); }
            yield return FlyRat(new Vector3(168f, 0f, 126f), 6f, 0.9f, RatwingRig.Mode.Fly);
            if (OuterBarrier != null) yield return Drop(OuterBarrier, 3.2f, 6f, 0.5f);
            UI.Hint("Too soon - wait until he is inside", 3f);
            if (Mistimed == 1) yield return Shot("b37b_mistimed_the_barrier_stops_for_him");
        }

        // ------------------------------------------------------------------------------------------ BIA_10 + BIA_11
        IEnumerator Temple()
        {
            if (!BiavehState.SceneDone("BIA_10"))
            {
                // the final routing control, with the symbols learned at the three relays (their three marks, in order)
                yield return SayIds("BIA_10_N1");                // QF23 (12): the archive opens only to all three districts together...
                Objective("Repair the final routing control at the archive entrance - the three relay symbols (X)");
                yield return Interact(L.RoutingView, "Set the three symbols", "the final routing control", "At the archive entrance, north end of the chamber", new[] { L.RoutingView });
                yield return Cutscene(true);
                Seven.Place(L.RoutingView, Vector3.forward); Seven.Touch(L.RoutingControl + Vector3.up * 1.2f);
                Shoot(L.RoutingControl + new Vector3(1.8f, 1.7f, -2f), L.RoutingControl + Vector3.up * 1.2f, 38f, 0.01f);
                // the three relays' marks light in turn on the control (the order she restored them)
                for (int i = 0; i < 3; i++) { RoutingMark(i); AudioKit.Play("console_select", L.RoutingControl, 0.6f); yield return Wait(0.45f); }
                Glow(RoutingLamp, 1.8f); AudioKit.Play("bia_relay_on", L.RoutingControl, 1f, 0f, AudioKit.Bus.Sfx, 80f);
                yield return Shot("b42_the_routing_control_the_three_symbols_set");
                // WIDE: stable lights ripple through the distant city; residents move through reopened passages
                Shoot(new Vector3(200f, 40f, 60f), new Vector3(120f, 120f, 400f), 62f, 0.01f);
                StartCoroutine(FogThin(0.7f, 1.5f));
                if (CityLights != null) StartCoroutine(Ripple());
                yield return Wait(2.6f);
                yield return Shot("b43_lights_ripple_through_the_city");
                StartCoroutine(FogThin(0f, 1.5f));
                Seven.Release(); Seven.Lock(true);
                if (Nela != null) { Nela.gameObject.SetActive(true); Nela.Place(L.TempleDoor + new Vector3(-2.4f, 0f, -4f), Vector3.back); }
                TwoShot(P7 + Vector3.left * 1.2f, Vector3.right, 3.2f, 1.3f, 1.3f, 44f, 0.01f);
                yield return Say("BIA_10", 1, 5);                // We can hear one another clearly again. / Everyone...? / Yes. Thank you. Both of you. / You're welcome. / He was particularly invested.
                yield return Wait(1.4f);                         // let the humour settle
                SetZone("ArchiveDoorZ", true); SetZone("Temple", true);
                if (ArchiveDoor != null) StartCoroutine(Drop(ArchiveDoor, 0f, 7f, 2f));
                Complete("BIA_10");
                yield return Cutscene(false);
            }
            // BIA_11: Nela accompanies her into the quiet chamber
            Objective("Into the archive with Nela");
            if (Nela != null) { Nela.gameObject.SetActive(true); Nela.WalkTo(L.OctaView + new Vector3(-2.4f, 0f, -1.4f), 1.1f); }
            yield return GuideWalk(new[] { L.TempleDoor + new Vector3(0f, 0f, 3f), L.OctaView + new Vector3(0f, 0f, -3.6f) }, "into the archive");
            yield return Cutscene(true);
            // ESTABLISHING: suspended memory structures round the octahedron - its eight faces readable from here
            Shoot(L.TempleDoor + new Vector3(6f, 3.2f, 6f), L.OctaAt, 50f, 0.01f);
            yield return Wait(1.2f);
            yield return Shot("b44_the_memory_chamber_the_octahedron");
            if (Nela != null) { if (Nela.Busy) { Nela.Stop(); Nela.Place(L.OctaView + new Vector3(-2.4f, 0f, -1.4f), P7 - Nela.transform.position); Log("WALK CUT (QF23 9): BIA_11 - Nela is already in the chamber"); } Nela.Face(P7, true); }
            Seven.Place(L.OctaView + new Vector3(0f, 0f, -2.2f), Vector3.forward); Seven.Lock(true);
            TwoShot((P7 + (Nela != null ? Nela.transform.position : P7)) * 0.5f, Vector3.right, 3.4f, 1.3f, 1.3f, 44f, 0.4f);
            yield return Say("BIA_11", 1, 5);                    // These are memories people chose to leave... / All their memories? / No... / And some were lost. / Yes.
            yield return Cutscene(false);
            Objective("Touch the octahedron");
            ShowGuide(L.OctaView);
            if (Auto) StartCoroutine(WalkSeven(L.OctaView, 20f));
            yield return Until(() => Flat(P7 - L.OctaView).magnitude < 1.2f, "reach the octahedron", 60f, "The octahedron at the centre of the chamber", L.OctaView, () => Seven.Place(L.OctaView, Vector3.forward));
            HideGuide();
            yield return Cutscene(true);
            Seven.Place(L.OctaView, Vector3.forward); Seven.Lock(true);
            Shoot(L.OctaView + new Vector3(1.6f, 1.6f, -1.2f), L.OctaAt, 38f, 0.01f);
            yield return Wait(0.4f);
            yield return Shot("b45_she_touches_the_octahedron");
            MusicSystem.Stop(1.5f);
            System.Action<float> glow = k => { if (Octa != null) foreach (var rr in Octa.GetComponentsInChildren<Renderer>()) if (rr.name == "Edges") Glow(rr, 1.1f * k); };
            yield return VisionTouch.Into("biaveh", Seven, Cam, UI, Octa != null ? Octa.position : L.OctaAt, OctaFace, glow);   // QF23 (22): the face distance of the 0.45 m octahedron
            yield return Vision6Play();
            yield return VisionTouch.Back("biaveh", Seven, Cam, Octa != null ? Octa.position : L.OctaAt, OctaFace, glow);
            yield return Shot("b46_her_fingers_lift_slowly_from_the_octahedron");
            Shoot(P7 + new Vector3(-0.5f, 1.65f, 2.2f), HerHead() + Vector3.down * 0.2f, 40f, 0.5f);
            yield return Say("BIA_11", 6, 9);                    // They weren't sure they wanted to leave it. / What did you see? / Someone recording a memory... / I remember waiting with them.
            yield return Wait(1.8f);                             // BIA_11_10: hold on her still posture
            yield return Shot("b47_hold_on_her_still_posture");
            TwoShot((P7 + (Nela != null ? Nela.transform.position : P7)) * 0.5f, Vector3.right, 3.2f, 1.3f, 1.3f, 42f, 0.5f);
            yield return Say("BIA_11", 11, 13);                  // Would you like to share what you saw? / Not yet. / Then not yet.
            // the octahedron key and the preserved Biavian history (once) - the same collection as the other temples
            bool first = BiavehState.AwardRecord();
            WorldFlags.MarkMothershipKnown();
            if (RecordOcta != null)
            {
                RecordOcta.gameObject.SetActive(true);
                var fw = Flat(Seven.transform.forward).normalized; var lp = P7 + Vector3.up * 1.22f + fw * 0.4f - Seven.transform.right * 0.08f;
                var tik = Seven.gameObject.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
                tik.enabled = true; tik.Ease = 3f; tik.LeftPos = lp; tik.LeftW = 1f; tik.LookPos = lp; tik.LookW = 0.8f;
                yield return Wait(0.7f);
                RecordOcta.position = lp + Vector3.up * 0.07f;
                Shoot(lp + fw * 0.75f + Vector3.up * 0.18f + Seven.transform.right * 0.2f, lp + Vector3.up * 0.04f, 28f, 0.01f);
                yield return Wait(0.6f);
                yield return Shot("b48_insert_the_small_octahedron_and_the_record");
                tik.ReleaseAll("the mothership record insert");
                RecordOcta.gameObject.SetActive(false);
            }
            Log($"the mothership's record + its octahedron: awarded {(first ? "now (first time)" : "ALREADY - no second award")} (awards {BiavehState.RecordAwards}, must be 1); the destination is now named '{WorldFlags.ContactName}'");
            Complete("BIA_11");
            yield return Cutscene(false);
        }
        const float OctaFace = 0.45f / 1.7320508f;   // an octahedron of vertex radius r has its faces at r / sqrt(3)
        Vector3 HerHead() => Seven.Bone(HumanBodyBones.Head) != null ? Seven.Bone(HumanBodyBones.Head).position : P7 + Vector3.up * 1.6f;
        IEnumerator Ripple()
        {
            for (int i = 0; i < CityLights.Length; i++) { Glow(CityLights[i], 1.6f); if (i % 4 == 3) yield return Wait(0.08f); }
        }
        UnityEngine.Video.VideoPlayer _vp6; RenderTexture _vp6RT;
        /// <summary>BIA_11's vision: Art/Video/vision_06_mothership.mp4 (12 s; the still fallback Resources/Biaveh/vision_06_still).
        /// Silent but for a soft hum as the light travels into the archive and a faint chime - no music.</summary>
        IEnumerator Vision6Play()
        {
            MusicSystem.Suspend(1.2f);
            AudioKit.Play("vision_whoosh", null, 0.9f);
            var shimmer = AudioKit.Loop("vision_shimmer", null, null, 0f, AudioKit.Bus.Sfx);
            if (shimmer != null) StartCoroutine(AudioKit.Fade(shimmer, 0.3f, 1.5f));
            yield return UI.Fade(1f, 0.9f, 1f);
            bool video = false;
            if (Vision6 != null && !DemoBoot.NoVideo)
            {
                var vgo = new GameObject("Vision6Video"); _vp6 = vgo.AddComponent<UnityEngine.Video.VideoPlayer>();
                _vp6.playOnAwake = false; _vp6.isLooping = false; _vp6.clip = Vision6; _vp6.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
                _vp6RT = new RenderTexture((int)Vision6.width, (int)Vision6.height, 0); _vp6.targetTexture = _vp6RT;
                _vp6.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None; _vp6.timeUpdateMode = UnityEngine.Video.VideoTimeUpdateMode.GameTime;
                _vp6.Prepare(); for (float t = 0f; t < 3f && !_vp6.isPrepared; t += Time.deltaTime) yield return null;
                video = _vp6.isPrepared;
            }
            float aspect = Vision6 != null ? Vision6.width / (float)Mathf.Max(1u, Vision6.height) : 16f / 9f;
            float len = video ? (float)Vision6.length : 10f;
            if (video)
            {
                Log($"vision 6: video {Vision6.name} ({Vision6.length:0.0}s) full-screen; a soft hum as the light travels into the archive, a faint chime - no music");
                _vp6.time = 0; _vp6.Play();
                for (float t = 0f; t < 0.7f; t += Time.deltaTime) { UI.SetVideo(_vp6RT, aspect, t / 0.7f); yield return null; }
                UI.SetVideo(_vp6RT, aspect, 1f);
            }
            else
            {
                var still = Resources.Load<Texture2D>("Biaveh/vision_06_still");
                Log($"vision 6: VIDEO NOT AVAILABLE - still fallback {(still != null ? "shown" : "missing")}");
                if (still != null) for (float t = 0f; t < 0.7f; t += Time.deltaTime) { UI.SetVideo(still, still.width / (float)still.height, t / 0.7f); yield return null; }
            }
            bool shot = false, hum = false, chime = false; AudioSource humSrc = null;
            for (float vt0 = Time.time; Time.time - vt0 < len - 0.12f && (!video || _vp6.isPlaying || Time.time - vt0 < 1f);)
            {
                float e = Time.time - vt0;
                if (!hum && e > len * 0.45f) { hum = true; humSrc = AudioKit.Loop("bia_route_share", null, null, 0.35f, AudioKit.Bus.Sfx); }   // the light travels into the archive
                if (!chime && e > len * 0.72f) { chime = true; AudioKit.Play("console_select", null, 0.35f); }                             // a faint chime
                if (!shot && e > 4f) { shot = true; yield return Shot(video ? "b45b_vision_video_frame" : "b45b_vision_still_fallback"); }
                if (e > 0.8f && SkipButton()) break;
                yield return null;
            }
            if (_vp6 != null) _vp6.Pause();
            if (shimmer != null) StartCoroutine(AudioKit.Fade(shimmer, 0f, 1.0f, true));
            if (humSrc != null) StartCoroutine(AudioKit.Fade(humSrc, 0f, 1.0f, true));
            UI.SetVideo(null, aspect, 0f);
            if (_vp6 != null) { _vp6.Stop(); Destroy(_vp6.gameObject); }
            yield return UI.Fade(0f, 1.2f, 1f);
        }

        // ------------------------------------------------------------------------------------------ BIA_12: the ride back, the beam
        IEnumerator Home()
        {
            FreeBike = true;
            Objective("Ride back to the saucer on the bike - through the restored districts");
            if (Nela != null) Nela.gameObject.SetActive(false);
            yield return GuideWalk(new[] { L.TempleDoor + new Vector3(0f, 0f, -4f), L.ChamberEntry + new Vector3(-3f, 0f, 0f) }, "back to the bike");
            if (!Riding) yield return GetBackOn();
            // the player sees the bridge open, the garden clear, residents moving freely
            if (Crossers != null) for (int i = 0; i < Crossers.Length; i++) if (Crossers[i] != null) Crossers[i].Loop(new[] { new Vector3(196f - i * 3f, 0f, -2.2f), new Vector3(120f - i * 3f, 0f, 3.2f) }, 2f, 1.2f);
            Lights(DistrictLights1, true); Lights(DistrictLights2, true); Lights(DistrictLights3, true);
            // QF23: out through the middle of the chamber's doorway first (tight radius) - from inside the chamber the route's
            // next point lies diagonally through the wall south of the door (the bike pressed against it: a skip in the playtest)
            if (Riding && Bike.transform.position.x < 206.5f) yield return RideRoute(new[] { new Vector3(208.5f, 0f, 112f) }, "out of the transfer chamber", 2.5f);
            yield return RideRoute(L.RideHome.Skip(1).ToArray(), "back through the restored districts", 8f);
            yield return StopBike(1.6f);
            yield return Cutscene(true);
            Checkpoint(L.CpHome);
            Shoot(new Vector3(16f, 6f, -14f), L.SaucerAt + Vector3.up * 2f, 50f, 0.01f);
            yield return Shot("b49_back_at_the_berth");
            if (Riding) { if (!Bike.Dismount()) Bike.Dismount(true); }
            Seven.Place(L.SaucerHome + new Vector3(-2.5f, 0f, 0f), Vector3.right);
            yield return HoldBikeIn();
            if (SaucerBeam != null) { yield return SaucerBeam.Iris(true); yield return SaucerBeam.BeamOn(true); }
            yield return WalkSeven(L.SaucerHome, 6f);
            Seven.Lock(true); Seven.BeginFloat();
            bool s = false;
            Shoot(L.SaucerHome + new Vector3(14f, 4f, 12f), L.SaucerHome + new Vector3(0f, 2.5f, 0f), 50f, 0.01f);
            if (SaucerBeam != null) yield return SaucerBeam.Rise(Seven.transform, 3.8f, 40f, t => { if (!s && t > 0.45f) { s = true; StartCoroutine(Shot("b50_she_boards_in_the_beam")); } });
            if (SaucerBeam != null) yield return SaucerBeam.Open(false);
            yield return UI.Fade(1f, 0.6f);
            ChapterDone = true;
            Log("the Mothership (BIA_03-BIA_11 in the Biaveh scene) done - aboard: the reunion straight from the beam-up, BIA_13");
            BiavehState.HomeAboard = true; Chapter1State.Aboard = true;
            SceneManager.LoadScene("Saucer");
        }

        // ------------------------------------------------------------------------------------------ QF23 (19, 21): the fight's voice
        int _stunCalls, _partCalls, _beams, _stunsSeen; bool _bladeLine; float _lastCall = -9f;
        void FightMoment(string m)
        {
            if (m == "stagger")
            {
                _stunsSeen++;
                // the first times every stun, then every third (the player has it by then)
                if ((_stunsSeen <= 3 || _stunsSeen % 3 == 0) && Time.time - _lastCall > 3f && !Dlg.Busy)
                { _lastCall = Time.time; _stunCalls++; StartCoroutine(SayIds(new[] { "BIA_09_S1", "BIA_09_S2", "BIA_09_S3" }[(_stunCalls - 1) % 3])); }
            }
            else if (m == "blade_shock" && !_bladeLine && !Dlg.Busy) { _bladeLine = true; StartCoroutine(SayIds("BIA_09_B1", "BIA_09_B2")); }   // He doesn't like those. / Good.
        }
        /// <summary>While his energy waits at a phase mark: the station lit from above (a beam, the partition's edge glowing),
        /// Panther calls it (again after 12 s if she hasn't gone), and the bar's mark pulses (BossFight).</summary>
        IEnumerator StationWatch(RatwingFight f)
        {
            float since = -1f; int called = 0;
            while (f != null && !f.Over && !f.Dazed)
            {
                bool w = f.WaitingOnObjective && !f.Paused;
                if (w) { if (since < 0f) { since = Time.time; called = 0; _beams++; } f.StationBeam(true); if ((called == 0 || (called == 1 && Time.time - since > 12f)) && !Dlg.Busy) { _partCalls++; StartCoroutine(SayIds(called == 0 ? "BIA_09_C1" : "BIA_09_C2")); if (called == 0) StartCoroutine(Shot("b36a_the_beam_over_the_station_close_the_partition")); called++; } }
                else if (since >= 0f) { since = -1f; f.StationBeam(false); }
                yield return null;
            }
            f?.StationBeam(false);
        }
        /// <summary>BIA_10: one of the three relays' marks lights on the routing control.</summary>
        void RoutingMark(int i)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(g.GetComponent<Collider>()); g.name = "RoutingMark" + i;
            g.transform.position = L.RoutingControl + Vector3.up * 1.45f + Vector3.right * (i - 1) * 0.28f; g.transform.localScale = Vector3.one * 0.09f;
            g.GetComponent<Renderer>().sharedMaterial = Markers.Additive(2f);
        }
    }
}
