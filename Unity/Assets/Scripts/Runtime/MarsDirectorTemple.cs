using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using L = Seven.MarsLayout;

namespace Seven
{
    /// <summary>Round 15 — Mars, sections 7-11: Viper's intervention (a positioning-and-timing encounter, not Venus's creature
    /// fight), the temple, the vision, the route that stops, the ride back.</summary>
    public partial class MarsDirector
    {
        // ------------------------------------------------------------------------------------------ 7: Viper's intervention
        /// <summary>The arena's gates / barriers / moving section / walkway (done = everything open, as after the encounter).</summary>
        void SetEncounter(bool done)
        {
            SetZone("GateWC", done); SetZone("GateCE", done); SetZone("CentreBarrier", done); SetZone("SlideGate", done); SetZone("FallenWalkZ", done);
            if (BarrierWC != null) BarrierWC.SetActive(false); if (BarrierCE != null) BarrierCE.SetActive(false); if (BarrierCentre != null) BarrierCentre.SetActive(false);
            if (FallenWalk != null) { if (done) FallenWalk.SetPositionAndRotation(L.FallenWalk, Quaternion.identity); else FallenWalk.SetPositionAndRotation(L.FallenWalkRest, Quaternion.Euler(14f, 8f, -18f)); }
            if (SlideBlock != null) SlideBlock.position = new Vector3(6.7f, done ? -3f : 0f, L.SlideGateZ);
            if (Pylons != null) foreach (var p in Pylons) Glow(p, done ? 0f : 1.4f);
        }

        bool _slideRun, _slideOpen;
        /// <summary>The moving section: a block that slides across the gap in the east wall and back (open 2.4 s, closed 3 s).</summary>
        IEnumerator SlideCycle()
        {
            _slideRun = true;
            while (_slideRun)
            {
                // closing: the block slides in
                AudioKit.Play("mars_section_slide", new Vector3(6.7f, 0f, L.SlideGateZ), 0.7f, 0.03f, AudioKit.Bus.Sfx, 40f);
                for (float t = 0f; t < 1f && _slideRun; t += Time.deltaTime / 0.6f) { if (SlideBlock != null) SlideBlock.position = new Vector3(6.7f, Mathf.Lerp(-3f, 0f, t), L.SlideGateZ); yield return null; }
                _slideOpen = false; SetZone("SlideGate", false);
                for (float t = 0f; t < 3f && _slideRun; t += Time.deltaTime) yield return null;
                AudioKit.Play("mars_section_slide", new Vector3(6.7f, 0f, L.SlideGateZ), 0.7f, 0.03f, AudioKit.Bus.Sfx, 40f);
                for (float t = 0f; t < 1f && _slideRun; t += Time.deltaTime / 0.6f) { if (SlideBlock != null) SlideBlock.position = new Vector3(6.7f, Mathf.Lerp(0f, -3f, t), L.SlideGateZ); yield return null; }
                _slideOpen = true; SetZone("SlideGate", true);
                for (float t = 0f; t < 2.4f && _slideRun; t += Time.deltaTime) yield return null;
            }
        }

        IEnumerator SetBarrier(GameObject b, bool on, string zone, Vector3 at)
        {
            if (b != null) b.SetActive(on);
            SetZone(zone, !on);
            AudioKit.Play(on ? "mars_barrier_on" : "mars_barrier_off", at, 0.9f, 0.04f, AudioKit.Bus.Sfx, 60f);
            yield return null;
        }

        IEnumerator Encounter()
        {
            FreeBike = true;
            Objective("Cross the aligned bridge to the temple");
            if (Worker != null) Worker.Place(L.WestControl + new Vector3(-1.6f, 0f, 0f), Vector3.right);
            if (Caretaker != null) Caretaker.Place(L.EastControl + new Vector3(1.6f, 0f, 0f), Vector3.left);
            if (P7.y < -1f) yield return GuideWalk(new[] { new Vector3(-49f, -3.4f, 160f), new Vector3(-55.5f, 0f, 160f) }, "up from the pier walk");
            ShowGuide(L.ArenaEntry);
            if (Auto) { if (!Riding) { yield return WalkPath(new[] { new Vector3(-56f, 0f, 150f), L.SpanWestFoot, new Vector3(-24f, 0f, 150f), L.ArenaEntry }, 40f); } }
            yield return Until(() => P7.x > L.ArenaEntry.x - 5f && Flat(P7 - L.ArenaEntry).magnitude < 14f, "across the span to the temple approach", 90f, "Walk (or ride) east over the aligned span", L.ArenaEntry, () => { if (Riding) Bike.Dismount(true); Seven.Place(L.ArenaEntry, Vector3.right); });
            HideGuide();
            if (Riding) { yield return StopBike(1f); Bike.DismountTo(P7 + Vector3.right * 1.5f, Vector3.right); FollowCamera(); }
            FreeBike = false;
            yield return Cutscene(true);
            // WIDE - TEMPLE APPROACH: Viper moves to seize the restored crossing; he blocks her path
            if (Viper != null) { Viper.gameObject.SetActive(true); Viper.Place(L.ViperBlocks + Vector3.forward * 4f, Vector3.back); Viper.WalkTo(L.ViperBlocks, 1.2f); }
            Shoot(L.IslandC + new Vector3(-26f, 9f, -18f), L.ViperBlocks + Vector3.up * 2f, 54f, 0.01f);
            MusicSystem.Play("mars_viper", 1.5f);
            yield return Wait(2.6f);
            yield return Shot("m24_wide_temple_approach_viper_blocks_her");
            var vp = L.ViperBlocks; var mid = (P7 + vp) * 0.5f; var side = Vector3.Cross(Vector3.up, Flat(vp - P7).normalized);
            Seven.transform.rotation = Quaternion.LookRotation(Flat(vp - P7)); if (Viper != null) Viper.Face(P7);
            TwoShot(mid, side, 9f, 2.4f, 1.8f, 46f, 0.6f);
            yield return Say("MAR_07", 1, 5);                    // Do you know what you've opened? / A bridge. / ... / It goes both ways. / That is what concerns me.
            yield return Shot("m25_it_goes_both_ways");
            // he activates the crossing's defensive machinery: barriers divide the arena, a section starts moving
            if (Viper != null) Viper.Play("point");
            AudioKit.Play("mars_defense_arm", L.IslandC, 1f, 0.02f, AudioKit.Bus.Sfx, 200f);
            CameraShake.Quake(0.8f, 0.3f);
            yield return SetBarrier(BarrierWC, true, "GateWC", new Vector3(-6.7f, 0f, 138f));
            yield return SetBarrier(BarrierCE, true, "GateCE", new Vector3(6.7f, 0f, 158f));
            yield return SetBarrier(BarrierCentre, true, "CentreBarrier", new Vector3(0f, 0f, 150.5f));
            SetZone("FallenWalkZ", false);
            if (Pylons != null) foreach (var p in Pylons) Glow(p, 1.4f);
            StartCoroutine(SlideCycle());
            Shoot(L.IslandC + new Vector3(0f, 22f, -22f), L.IslandC + Vector3.up * 0.5f, 54f, 0.6f);
            yield return Wait(1.8f);
            yield return Shot("m26_the_arena_divided_barriers_and_moving_section");
            if (Viper != null) Viper.Play("idle");
            yield return Cutscene(false);
            // the encounter: positioning and timing - reconnect routes, disable his control points, close the distance
            Objective("Viper holds the crossing - disable his control points (X) and reach him");
            // control point 1 (west lane)
            yield return Interact(L.Pylons[0], "Disable", "disable control point 1 (west lane)", "The glowing control pylon in the west lane - walk up and press X", new[] { new Vector3(-14f, 0f, 140f), L.Pylons[0] + new Vector3(0f, 0f, 1.4f) });
            yield return PylonDown(0);
            yield return SetBarrier(BarrierWC, false, "GateWC", new Vector3(-6.7f, 0f, 138f));
            yield return SayViperBark("He reroutes the centre");
            // he answers: the centre lane's barrier stays; the east lane only through the moving section
            Objective("Into the centre lane, then through the moving section into the east lane when it opens - control point 2 (X)");
            ShowGuide(new Vector3(-3f, 0f, 138f));                                        // beyond the gate, so the light leads through it
            if (Auto) yield return WalkPath(new[] { new Vector3(-9f, 0f, 138f), new Vector3(-4f, 0f, 138f), new Vector3(4.5f, 0f, 143f) }, 20f);
            yield return Until(() => P7.x > -5.5f || P7.x > 6f, "through the west gate into the centre lane", 60f, "The west gate is open now - walk through into the centre lane", new Vector3(-6.7f, 0f, 138f), () => Seven.Place(new Vector3(-3f, 0f, 138f), Vector3.right));
            // in front of the moving section first, then through it (the light beyond it) once she is there
            if (!Auto && P7.x < 4.6f) { ShowGuide(new Vector3(5.0f, 0f, L.SlideGateZ)); yield return Until(() => P7.x > 4.4f && Mathf.Abs(P7.z - L.SlideGateZ) < 2.2f || P7.x > 7.8f, "to the moving section", 40f, "Walk to the sliding block in the east wall of the centre lane", new Vector3(5f, 0f, L.SlideGateZ), () => Seven.Place(new Vector3(5f, 0f, L.SlideGateZ), Vector3.right)); }
            ShowGuide(new Vector3(10f, 0f, L.SlideGateZ));
            if (Auto) { while (!_slideOpen) yield return null; yield return WalkPath(new[] { new Vector3(6.7f, 0f, L.SlideGateZ), new Vector3(10f, 0f, 140f) }, 12f); }
            yield return Until(() => P7.x > 7.8f && MarsLayout.ZoneAt(P7) == "PlazaE_S", "through the moving section into the east lane (timing)", 60f, "Wait by the sliding block in the east wall - go through while it is down", new Vector3(6.7f, 0f, L.SlideGateZ), () => Seven.Place(new Vector3(10f, 0f, 140f), Vector3.right));
            HideGuide();
            yield return Shot("m27_through_the_moving_section");
            yield return Interact(L.Pylons[1], "Disable", "disable control point 2 (east lane)", "The control pylon in the east lane - press X", new[] { L.Pylons[1] + new Vector3(-1.3f, 0f, 0f) });
            yield return PylonDown(1);
            // reconnect the route: the walkway section lies broken across the gap in the east lane - lift it back (TK)
            Objective("The east lane is broken - lift the fallen walkway section back into place (RT)");
            yield return Telekinesis(L.FallenWalkRest, 14f, "lift the walkway section (telekinesis)", "Look at the fallen walkway section in the gap and use telekinesis (RT)", null, new[] { new Vector3(14f, 0f, 141.5f) });
            yield return LiftWalkway();
            SetZone("FallenWalkZ", true);
            Objective("Across the walkway to control point 3 (X)");
            yield return Interact(L.Pylons[2], "Disable", "disable control point 3 (by the temple)", "The last control pylon, north of the walkway - press X", new[] { new Vector3(14f, 0f, 146.5f), new Vector3(14f, 0f, 152f), L.Pylons[2] + new Vector3(0f, 0f, -1.3f) });
            yield return PylonDown(2);
            _slideRun = false;
            yield return SetBarrier(BarrierCentre, false, "CentreBarrier", new Vector3(0f, 0f, 150.5f));
            yield return SetBarrier(BarrierCE, false, "GateCE", new Vector3(6.7f, 0f, 158f));
            SetZone("SlideGate", true);
            // close the distance and contain him (never killed): a takedown, the restraint field
            Objective("His machinery is down - close the distance and restrain Viper (X)");
            if (Viper != null) Viper.Play("roar");
            if (P7.x > 7f) yield return GuideWalk(new[] { new Vector3(10f, 0f, 158f), new Vector3(4.5f, 0f, 158f) }, "through the east gate");
            yield return Interact(Viper != null ? Viper.transform.position : L.ViperBlocks, "Restrain", "restrain Viper", "Go through the east gate to Viper by the temple steps and press X", new[] { new Vector3(10f, 0f, 158f), new Vector3(4.5f, 0f, 158f), L.ViperBlocks + new Vector3(1.1f, 0f, -1.0f) }, null, 2.6f);
            yield return Subdue();
        }

        IEnumerator SayViperBark(string what) { if (Viper != null) Viper.Play("point"); Log($"encounter: {what}"); yield return Wait(0.2f); if (Viper != null) Viper.Play("idle"); }

        IEnumerator PylonDown(int i)
        {
            if (Pylons != null && i < Pylons.Length) Glow(Pylons[i], 0f);
            AudioKit.Play("mars_barrier_off", L.Pylons[i], 0.9f, 0.04f, AudioKit.Bus.Sfx, 60f);
            Log($"encounter: control point {i + 1} disabled");
            if (i == 0) yield return Shot("m26b_control_point_disabled");
            yield return null;
        }

        IEnumerator LiftWalkway()
        {
            if (FallenWalk == null) yield break;
            AudioKit.Play("slab_lift", FallenWalk.position, 0.9f, 0.05f, AudioKit.Bus.Sfx, 40f);
            var a = FallenWalk.position; var r0 = FallenWalk.rotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.8f) { float u = Mathf.SmoothStep(0f, 1f, t); FallenWalk.SetPositionAndRotation(Vector3.Lerp(a, L.FallenWalk, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 1.2f, Quaternion.Slerp(r0, Quaternion.identity, u)); yield return null; }
            AudioKit.Play("mars_section_slide", L.FallenWalk, 0.8f, 0.05f, AudioKit.Bus.Sfx, 40f);
            yield return Shot("m28_the_walkway_lifted_back");
        }

        IEnumerator Subdue()
        {
            yield return Cutscene(true);
            var vp = Viper != null ? Viper.transform.position : L.ViperBlocks;
            { var back = Flat(P7 - vp); if (back.sqrMagnitude < 0.01f) back = Vector3.back; Seven.Place(vp + back.normalized * 2.8f, -back); }   // she stops a few steps short of him
            Seven.transform.rotation = Quaternion.LookRotation(Flat(vp - P7));
            TwoShot((P7 + vp) * 0.5f, Vector3.Cross(Vector3.up, Flat(vp - P7).normalized), 3.6f, 1.5f, 1.4f, 44f, 0.01f);
            Seven.Gesture(vp + Vector3.up * 1.2f); Seven.SetEnergyPoint(2.2f, 0.4f);
            for (float t = 0f; t < 2.5f && !Seven.GesturePeak; t += Time.deltaTime) yield return null;
            if (Viper != null) { Viper.Play("hit", 0.1f); }
            yield return Wait(0.5f);
            if (Viper != null) Viper.Play("down", 0.3f);                 // knocked down - alive, contained where he lies
            yield return Wait(0.9f);
            if (Restraint != null) { Restraint.SetActive(true); Restraint.transform.position = vp; }
            AudioKit.Play("mars_restraint", vp, 1f, 0.03f, AudioKit.Bus.Sfx, 40f);
            yield return Shot("m29_viper_subdued_and_contained");
            Log("Viper subdued and contained (alive - left secured for local custody)");
            MusicSystem.Play("mars_kingdoms", 4f);
            // LEVEL TWO-SHOT - after the fight; the worker and the caretaker approach from opposite sides
            if (Worker != null) { Worker.Place(new Vector3(-12f, 0f, 150f), Vector3.right); Worker.WalkTo(vp + new Vector3(-4.5f, 0f, -4f), 1.3f); }
            if (Caretaker != null) { Caretaker.Place(new Vector3(18f, 0f, 152f), Vector3.left); Caretaker.WalkTo(vp + new Vector3(4.5f, 0f, -4f), 1.3f); }
            var mid = (P7 + vp) * 0.5f; var side = Vector3.Cross(Vector3.up, Flat(vp - P7).normalized);
            TwoShot(mid, side, 3.4f, 1.1f, 1.1f, 42f, 0.5f);
            yield return Say("MAR_07", 6, 6);                    // They'll turn on each other again.
            // 7 looks toward the worker and the caretaker approaching from opposite sides
            Shoot(P7 - Flat(vp - P7).normalized * 2.4f + Vector3.up * 1.7f + side * 0.6f, vp + Vector3.back * 4f + Vector3.up * 1.2f, 50f, 0.5f);
            yield return Wait(1.2f);
            yield return Shot("m30_the_worker_and_the_caretaker_from_opposite_sides");
            TwoShot(mid, side, 3.4f, 1.1f, 1.1f, 42f, 0.5f);
            yield return Say("MAR_07", 7, 7);                    // That's their choice. You don't get to make it for them.
            Complete("MAR_07");
            SetEncounter(true);
            yield return Cutscene(false);
        }

        // ------------------------------------------------------------------------------------------ 8-10: the temple, the vision, the leads
        IEnumerator Temple()
        {
            SetZone("TempleHall", true);
            if (Viper != null && !Viper.gameObject.activeSelf) { Viper.gameObject.SetActive(true); Viper.Place(L.ViperBlocks + new Vector3(4f, 0f, -1f), Vector3.back); Viper.Play("dazed"); }
            if (Restraint != null && Viper != null) { Restraint.SetActive(true); Restraint.transform.position = Viper.transform.position; }
            yield return Cutscene(true);
            yield return UI.Fade(1f, 0.6f);
            // the worker and the caretaker enter with 7
            Seven.Place(L.TempleDoor + new Vector3(0f, 0f, 2f), Vector3.forward); Seven.Lock(true);
            if (Worker != null) Worker.Place(L.TempleDoor + new Vector3(-2.2f, 0f, 1.4f), Vector3.forward);
            if (Caretaker != null) Caretaker.Place(L.TempleDoor + new Vector3(2.2f, 0f, 1.4f), Vector3.forward);
            Shoot(L.TempleDoor + new Vector3(0f, 2.2f, -3f), L.SymbolWall, 52f, 0.01f);
            yield return UI.Fade(0f, 0.8f);
            if (Worker != null) Worker.WalkTo(L.RuneView + new Vector3(-2.4f, 0f, 1.5f), 1.1f);
            if (Caretaker != null) Caretaker.WalkTo(L.RuneView + new Vector3(2.4f, 0f, 1.5f), 1.1f);
            yield return WalkSeven(L.RuneView + new Vector3(0f, 0f, -1.4f), 12f);
            Seven.Lock(true);
            yield return Shot("m31_inside_the_temple_markings_of_both_kingdoms");
            // INSERT - the symbols together
            Glow(SymbolW, 0.5f); Glow(SymbolE, 0.5f);
            Shoot(L.SymbolWall + new Vector3(0f, -0.2f, -4.2f), L.SymbolWall, 40f, 0.01f);
            yield return Wait(0.6f);
            yield return Shot("m32_insert_the_symbols_together");
            if (Worker != null) Worker.Play("point");
            yield return Say("MAR_08", 1, 1);                    // That's ours.
            if (Worker != null) Worker.Play("idle"); if (Caretaker != null) Caretaker.Play("point");
            yield return Say("MAR_08", 2, 2);                    // And that is ours.
            if (Caretaker != null) Caretaker.Play("idle");
            Glow(SymbolW, 1.2f); Glow(SymbolE, 1.2f);
            yield return Say("MAR_08", 3, 3);                    // They're part of the same design.
            yield return Wait(2.0f);                              // hold on the complete pattern
            yield return Shot("m33_hold_the_complete_pattern");
            Complete("MAR_08");
            yield return Cutscene(false);
            // they approach Mars's rune - the tetrahedron; the vision plays as she reaches it (no button)
            Objective("Go to the rune");
            ShowGuide(L.RuneView);
            if (Auto) StartCoroutine(WalkSeven(L.RuneView, 10f));
            yield return Until(() => Flat(P7 - L.RuneView).magnitude < 1.4f || P7.z > L.RuneView.z, "reach the rune", 60f, "The glowing tetrahedron at the temple's heart", L.RuneView, () => Seven.Place(L.RuneView, Vector3.forward));
            HideGuide();
            yield return Cutscene(true);
            Seven.Place(L.RuneView, Vector3.forward); Seven.Lock(true);
            Shoot(L.RuneView + new Vector3(2.2f, 1.6f, 0.6f), L.RuneAt, 38f, 0.01f);
            Seven.Gesture(L.RuneAt);
            yield return Wait(0.9f);
            yield return Shot("m34_she_touches_the_rune");
            yield return Vision4Play();
            // RETURN - insert of 7's hand leaving the rune
            // the insert: the rune, her hand drawing back from it
            Shoot(L.RuneAt + new Vector3(1.1f, 0.5f, -1.0f), L.RuneAt + new Vector3(0f, 0f, -0.35f), 32f, 0.01f);
            yield return Wait(0.6f);
            yield return Shot("m35_insert_her_hand_leaving_the_rune");
            var mid = L.RuneView + Vector3.up * 1.3f;
            Shoot(mid + new Vector3(0f, 0.6f, 4.2f), mid, 50f, 0.5f);
            yield return Say("MAR_09", 1, 3);                    // They built it together. / The temple? / The bridge. Both sides.
            // the worker looks toward the entrance
            if (Worker != null) Worker.Face(L.TempleDoor);
            yield return Say("MAR_09", 4, 5);                    // We were taught it was taken from us. / So were we.
            yield return Wait(2.2f);                              // let the silence carry the discovery
            yield return Shot("m36_let_the_silence_carry_it");
            yield return Say("MAR_09", 6, 6);                    // Then people should see this.
            // they agree to preserve and share the record; 7 receives a small counterpart while the original remains
            bool first = MarsState.AwardRecord();
            if (RecordTet != null)
            {
                RecordTet.gameObject.SetActive(true);
                var lh = Seven.Bone(HumanBodyBones.LeftHand); var lp = lh != null ? lh.position : P7 + Vector3.up * 1.1f;
                RecordTet.position = lp + Vector3.up * 0.08f;
                Shoot(lp + Vector3.up * 0.5f + Seven.transform.forward * 0.25f, lp, 26f, 0.01f);
                yield return Wait(0.8f);
                yield return Shot("m37_insert_the_small_tetrahedron_beside_the_other_records");
            }
            Log($"Mars's record: awarded {(first ? "now (first time)" : "ALREADY - no second award")} (awards {MarsState.RecordAwards}, must be 1)");
            Complete("MAR_09");
            // section 10 (revised): the temple systems reactivate; a display shows its old connections - OVER 7's SHOULDER, A
            // ROUTE leading out from Mars beyond the edge of the solar system, breaking off partway. No new destinations here.
            if (Display != null) Glow(Display, 1.4f);
            if (DisplayLeads != null) DisplayLeads.SetActive(true);
            AudioKit.Play("mars_control_on", L.DisplayAt, 0.8f, 0.03f, AudioKit.Bus.Sfx, 30f);
            Seven.transform.rotation = Quaternion.LookRotation(Flat(L.DisplayAt - P7));
            var f = Flat(L.DisplayAt - P7).normalized;
            Shoot(P7 + Vector3.up * 1.65f - f * 1.0f + Vector3.Cross(Vector3.up, f) * 0.5f, L.DisplayAt, 42f, 0.6f);
            yield return Wait(0.8f);
            yield return Shot("m38_the_display_a_route_that_stops");
            yield return Say("MAR_10", 1, 4);                    // Our keepers traded records along this route. / It stops. / The rest was never kept here... / What do you expect to find?
            // 7 looks at the record in her hands
            { var lh2 = Seven.Bone(HumanBodyBones.LeftHand); Shoot(P7 + Seven.transform.right * 1.1f + Seven.transform.forward * 0.9f + Vector3.up * 1.5f, (lh2 != null ? lh2.position : P7 + Vector3.up * 1.1f), 34f, 0.4f); }
            yield return Say("MAR_10", 5, 5);                    // I'm trying not to decide before I get there.
            // she sends the route to the saucer
            MarsState.SendRoute();
            Log("section 10: the route (it stops partway) sent to the saucer - no new destinations unlock here");
            yield return Say("MAR_10", 6, 7);                    // Received. Part of a route. / We'll look at it together.
            Complete("MAR_10");
            yield return Cutscene(false);
        }

        UnityEngine.Video.VideoPlayer _vp4; RenderTexture _vp4RT;
        IEnumerator Vision4Play()
        {
            // as the other visions: music suspended, the shimmer in and out; the video is nearly silent - soft stone and wind, no music
            MusicSystem.Suspend(1.2f);
            AudioKit.Play("vision_whoosh", null, 0.9f);
            var shimmer = AudioKit.Loop("vision_shimmer", null, null, 0f, AudioKit.Bus.Sfx);
            if (shimmer != null) StartCoroutine(AudioKit.Fade(shimmer, 0.35f, 1.5f));
            yield return UI.Fade(1f, 0.9f, 1f);
            bool video = false;
            if (Vision4 != null && !DemoBoot.NoVideo)
            {
                var vgo = new GameObject("Vision4Video");
                _vp4 = vgo.AddComponent<UnityEngine.Video.VideoPlayer>();
                _vp4.playOnAwake = false; _vp4.isLooping = false; _vp4.clip = Vision4; _vp4.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
                _vp4RT = new RenderTexture((int)Vision4.width, (int)Vision4.height, 0); _vp4.targetTexture = _vp4RT;
                _vp4.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None; _vp4.timeUpdateMode = UnityEngine.Video.VideoTimeUpdateMode.GameTime;
                _vp4.Prepare();
                for (float t = 0f; t < 3f && !_vp4.isPrepared; t += Time.deltaTime) yield return null;
                video = _vp4.isPrepared;
            }
            float aspect = Vision4 != null ? Vision4.width / (float)Mathf.Max(1u, Vision4.height) : 16f / 9f;
            AudioKit.Play("mars_vision_stone", null, 1f);
            var wind = AudioKit.Loop("mars_amb_wind", null, null, 0.35f, AudioKit.Bus.Sfx);
            if (video)
            {
                Log($"vision 4: video {Vision4.name} ({Vision4.length:0.0}s) full-screen; soft stone and wind under it, no music");
                _vp4.time = 0; _vp4.Play();
                for (float t = 0f; t < 0.7f; t += Time.deltaTime) { UI.SetVideo(_vp4RT, aspect, t / 0.7f); yield return null; }
                UI.SetVideo(_vp4RT, aspect, 1f);
                bool shot = false; float len = (float)Vision4.length;
                for (float vt0 = Time.time; Time.time - vt0 < len - 0.12f && (_vp4.isPlaying || Time.time - vt0 < 1f);)
                {
                    if (!shot && Time.time - vt0 > 6f) { shot = true; yield return Shot("m34b_vision_video_frame"); }
                    if (Time.time - vt0 > 0.8f && SkipButton()) break;
                    yield return null;
                }
                _vp4.Pause();
            }
            else
            {
                var still = Resources.Load<Texture2D>("Mars/vision_04_still");
                Log($"vision 4: VIDEO NOT AVAILABLE - still fallback {(still != null ? "shown" : "missing")}");
                if (still != null) for (float t = 0f; t < 0.7f; t += Time.deltaTime) { UI.SetVideo(still, still.width / (float)still.height, t / 0.7f); yield return null; }
                for (float t = 0f; t < 8f && !(t > 0.8f && SkipButton()); t += Time.deltaTime) yield return null;
                yield return Shot("m34b_vision_still_fallback");
            }
            if (shimmer != null) StartCoroutine(AudioKit.Fade(shimmer, 0f, 1.0f, true));
            if (wind != null) StartCoroutine(AudioKit.Fade(wind, 0f, 1.2f, true));
            UI.SetVideo(null, aspect, 0f);
            if (_vp4 != null) { _vp4.Stop(); Destroy(_vp4.gameObject); }
            yield return UI.Fade(0f, 1.2f, 1f);
            MusicSystem.Resume(3.5f);
        }

        // ------------------------------------------------------------------------------------------ 11: the ride back
        IEnumerator RideBack()
        {
            // out of the temple to the bike at the west span head (a short cut)
            yield return Cutscene(true);
            yield return UI.Fade(1f, 0.7f);
            if (Viper != null) Viper.gameObject.SetActive(false);                           // left secured for local custody
            if (Restraint != null) Restraint.SetActive(false);
            // the crossing is open: the guard and the caretaker by the barrier, working; the transport crosses openly
            SetZone("Bridge", true); if (Barrier != null) Barrier.SetActive(false);
            if (Guard != null) { Guard.Place(L.GuardAt + new Vector3(-3f, 0f, -6f), Vector3.back); Guard.Play("talk"); }
            if (Caretaker != null) { Caretaker.Place(L.GuardAt + new Vector3(-3f, 0f, -8.6f), Vector3.forward); Caretaker.Play("talk"); }
            if (Worker != null) Worker.Place(L.TransportCrossFrom + new Vector3(-3f, 0f, -2.4f), Vector3.right);
            if (Transport != null) { var tp = L.TransportCrossFrom; tp.y = 0f; Transport.SetPositionAndRotation(tp, Quaternion.Euler(0f, 90f, 0f)); Transport.gameObject.SetActive(true); }
            PlaceRiding(new Vector3(-58f, 0f, 146f), 180f);
            yield return UI.Fade(0f, 0.7f);
            yield return Cutscene(false);
            FreeBike = true;
            Objective("Ride back to the saucer");
            yield return RideRoute(new[] { L.Back[1], L.Back[2], new Vector3(-66f, 0f, 14f) }, "back along the west rim", 8f);
            // 7 slows to let the transport pass - it crosses openly; the guard and the caretaker exchange a few practical words
            StartCoroutine(TransportCrosses());
            yield return StopBike(2.5f);
            Shoot(P7 + new Vector3(-4f, 2.4f, 4f), L.TransportCrossFrom + new Vector3(10f, 1f, 0f), 50f, 0.8f);
            _cut = true;
            yield return Wait(1.2f);
            yield return Shot("m39_the_transport_crosses_openly");
            yield return Say("MAR_11", 1, 2);                    // Did you settle their differences? / No.
            // she watches the crossing
            Shoot(L.GuardAt + new Vector3(-8f, 1.8f, -12f), (L.GuardAt + new Vector3(-3f, 0f, -7.3f)) + Vector3.up * 1.3f, 42f, 0.6f);
            yield return Wait(0.8f);
            yield return Shot("m40_the_guard_and_the_caretaker_working");
            yield return Say("MAR_11", 3, 4);                    // Got them talking. / A productive afternoon, then.
            Complete("MAR_11");
            _cut = false; FollowCamera();
            // she gives a quiet chuckle and continues toward the saucer
            yield return RideRoute(new[] { L.Back[3], L.Back[4], L.Back[5], L.HomeStop }, "back to the saucer", 8f);
            yield return StopBike(2f);
            yield return Home();
        }

        IEnumerator TransportCrosses()
        {
            if (Transport == null) yield break;
            var a = L.TransportCrossFrom; var b = L.TransportCrossTo; float len = Vector3.Distance(a, b);
            var hum = AudioKit.Loop("mars_transport_hum", Transport, null, 0.6f, AudioKit.Bus.Sfx, 4f, 60f);
            for (float d = 0f; d < len; d += 5f * Time.deltaTime) { var p = Vector3.Lerp(a, b, d / len); Transport.SetPositionAndRotation(p, Quaternion.Euler(0f, 90f, 0f)); yield return null; }
            if (hum != null) StartCoroutine(AudioKit.Fade(hum, 0f, 1.5f, true));
            Transport.gameObject.SetActive(false);
        }

        IEnumerator Home()
        {
            yield return Cutscene(true);
            Checkpoint(L.CpHome);
            var land = L.SaucerAt;
            Shoot(land + new Vector3(18f, 5f, 16f), land + new Vector3(0f, 2.5f, 0f), 50f, 0.6f);
            if (Riding) Bike.DismountTo(L.SevenDown + new Vector3(2.6f, 0f, 0f), Vector3.left);
            yield return Wait(0.6f);
            yield return Shot("m41_wide_back_at_the_saucer");
            // the bike rises back into the bike hold; 7 boards in the beam (no ramp)
            yield return HoldBikeIn();
            if (SaucerBeam != null) { yield return SaucerBeam.Iris(true); yield return SaucerBeam.BeamOn(true); }
            yield return WalkSeven(L.SevenDown, 6f);
            Seven.Lock(true); Seven.BeginFloat();
            bool s = false;
            if (SaucerBeam != null) yield return SaucerBeam.Rise(Seven.transform, 3.8f, 40f, t => { if (!s && t > 0.45f) { s = true; StartCoroutine(Shot("m42_she_boards_in_the_beam")); } });
            if (SaucerBeam != null) yield return SaucerBeam.Open(false);
            yield return UI.Fade(1f, 1.0f);
            ChapterDone = true;
            Log("Mars (sections 1-11 in the Mars scene) done - aboard for the debrief and the destination choice");
            MarsState.HomeAboard = true; Chapter1State.Aboard = true;
            SceneManager.LoadScene("Saucer");
        }
    }
}
