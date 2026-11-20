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
        /// <summary>QF17 (1): the plaza is one open arena; the old lanes' barriers, moving section and fallen walkway are gone
        /// (done = after the fight: the pylons dark, his platform sunk).</summary>
        void SetEncounter(bool done)
        {
            SetZone("Arena", true);
            foreach (var z in new[] { "GateWC", "GateCE", "CentreBarrier", "SlideGate", "FallenWalkZ" }) SetZone(z, true);
            foreach (var g in new[] { BarrierWC, BarrierCE, BarrierCentre }) if (g != null) g.SetActive(false);
            if (FallenWalk != null) FallenWalk.gameObject.SetActive(false);
            if (SlideBlock != null) SlideBlock.gameObject.SetActive(false);
            if (Dais != null) Dais.position = ViperFight.DaisAt + Vector3.down * ViperFight.DaisTop;
            if (Pylons != null) foreach (var p in Pylons) Glow(p, done ? 0f : 1.4f);
        }
        public Transform Dais;
        ViperFight _viperFight;
        public BossFight BossNow => _viperFight != null && _viperFight.isActiveAndEnabled && !_viperFight.Over ? _viperFight : null;

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
            if (Viper != null) { Viper.gameObject.SetActive(true); Viper.Place(L.ViperBlocks + Vector3.forward * 4f, Vector3.back); Viper.WalkTo(L.ViperBlocks + Vector3.back * 7f, 1.6f); }
            Shoot(L.IslandC + new Vector3(-26f, 9f, -18f), L.ViperBlocks + Vector3.up * 2f, 54f, 0.01f);
            MusicSystem.Play("mars_viper", 1.5f);
            yield return Wait(2.6f);
            yield return Shot("m24_wide_temple_approach_viper_blocks_her");
            // QF17 (6): she walks up to him first (from 19 m apart the two-shot found no clear framing and showed a wall)
            { var vq = Viper != null ? Viper.transform.position : L.ViperBlocks; var near = vq + Flat(P7 - vq).normalized * 6.5f; Seven.Place(near, Flat(vq - near)); }
            var vp = Viper != null ? Viper.transform.position : L.ViperBlocks; var mid = (P7 + vp) * 0.5f; var side = Vector3.Cross(Vector3.up, Flat(vp - P7).normalized);
            Seven.transform.rotation = Quaternion.LookRotation(Flat(vp - P7)); if (Viper != null) Viper.Face(P7);
            TwoShot(mid, side, 9f, 2.4f, 1.8f, 46f, 0.6f);
            yield return Say("MAR_07", 1, 5);                    // Do you know what you've opened? / A bridge. / ... / It goes both ways. / That is what concerns me.
            yield return Shot("m25_it_goes_both_ways");
            // QF17 (1): he raises his platform out of the plaza and springs onto it; his machinery arms (the pylons light)
            if (Viper != null) { Viper.Stop(); Viper.Play("roar"); Viper.Flare = 1f; }
            AudioKit.Play("mars_defense_arm", L.IslandC, 1f, 0.02f, AudioKit.Bus.Sfx, 200f);
            AudioKit.Play("mars_platform_rise", ViperFight.DaisAt, 1f, 0f, AudioKit.Bus.Sfx, 120f);
            CameraShake.Quake(0.8f, 0.3f);
            Shoot(ViperFight.DaisAt + new Vector3(9f, 4.5f, -14f), ViperFight.DaisAt + Vector3.up * 2.4f, 50f, 0.5f);
            var from = Viper != null ? Viper.transform.position : ViperFight.DaisAt;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.8f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                if (Dais != null) Dais.position = ViperFight.DaisAt + Vector3.down * ViperFight.DaisTop * (1f - u);
                if (Viper != null && t > 0.35f) { float k = Mathf.Clamp01((t - 0.35f) / 0.4f); Viper.transform.position = Vector3.Lerp(from, ViperFight.DaisAt + Vector3.up * ViperFight.DaisTop * u, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.2f; }
                yield return null;
            }
            if (Pylons != null) foreach (var p in Pylons) Glow(p, 1.4f);
            foreach (var g in new[] { BarrierWC, BarrierCE, BarrierCentre }) if (g != null) g.SetActive(false);
            if (FallenWalk != null) FallenWalk.gameObject.SetActive(false);
            if (SlideBlock != null) SlideBlock.gameObject.SetActive(false);
            SetZone("Arena", true);
            yield return Wait(0.6f);
            yield return Shot("m26_viper_on_his_platform_the_machinery_arms");
            // the fight
            _viperFight = Viper.GetComponent<ViperFight>() ?? Viper.gameObject.AddComponent<ViperFight>();
            var deck = GameObject.Find("PlazaDeckW");
            var rockMat = deck != null && deck.GetComponent<Renderer>() != null ? deck.GetComponent<Renderer>().sharedMaterial : Markers.DarkGloss();
            _viperFight.Setup(Viper, Dais, Pylons, rockMat);
            _viperFight.PlaceStart();
            if (Flat(P7 - ViperFight.HerStart).magnitude > 8f) Seven.Place(new Vector3(-9f, 0f, 132f), Flat(ViperFight.DaisAt - new Vector3(-9f, 0f, 132f)));
            _viperFight.UI = UI; _viperFight.AutoPilot = Auto;
            _viperFight.Beat = ViperComesDown;
            _viperFight.PhaseObjective = ph => ph == 0 ? "Viper attacks from his platform - shut his two pylons down (X) under fire"
                                            : ph == 1 ? "He hunts you - make him waste an attack (dodge B, jump A, block LT), then throw debris at him (RT)"
                                            : "Enraged - read him, dodge, counter: three more staggers";
            yield return Cutscene(false);
            UI.Hint("Dodge B  ·  Jump A  ·  Block LT  ·  Strike X  ·  Telekinesis RT: throw debris when he is open", 9f);
            _viperFight.Begin(BossFight.Load("viper"), Seven, Cam);
            StartCoroutine(FightPrompts(_viperFight));
            yield return _viperFight.Run(Objective);
            if (DemoBoot.BossIdle) yield break;
            _viperFight.End();
            yield return Shot("m28b_viper_pinned");
            yield return Subdue();
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
        IEnumerator ViperComesDown()
        {
            // a short beat from the side: the platform sinks, he drops onto the plaza
            Shoot(ViperFight.DaisAt + new Vector3(-12f, 3.5f, -9f), ViperFight.DaisAt + Vector3.up * 1.6f, 48f, 0.4f);
            yield return Wait(0.8f);
            yield return Shot("m27_his_platform_sinks_he_comes_down");
            yield return Wait(1.6f);
            FollowCamera();
        }

        IEnumerator Subdue()
        {
            yield return Cutscene(true);
            var vp = Viper != null ? Viper.transform.position : L.ViperBlocks;
            { var back = Flat(P7 - vp); if (back.sqrMagnitude < 0.01f) back = Vector3.back; if (back.magnitude < 2f || back.magnitude > 4f) Seven.Place(vp + back.normalized * 2.8f, -back); }
            Seven.transform.rotation = Quaternion.LookRotation(Flat(vp - P7));
            TwoShot((P7 + vp) * 0.5f, Vector3.Cross(Vector3.up, Flat(vp - P7).normalized), 3.6f, 1.5f, 1.4f, 44f, 0.01f);
            if (Viper != null) Viper.Play("down", 0.3f);                 // pinned - alive, contained where he lies
            Seven.SetEnergyPoint(2.2f, 0.4f);
            yield return Wait(0.9f);
            if (Restraint != null) { Restraint.SetActive(true); Restraint.transform.position = vp; }
            AudioKit.Play("mars_restraint", vp, 1f, 0.03f, AudioKit.Bus.Sfx, 40f);
            Seven.SetEnergyPoint(1f, 1f);
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
            if (Viper != null) Viper.Play("dazed");
            yield return Cutscene(false);
            if (DemoBoot.BossCheck && DemoBoot.CaptureDir != null) Cap.Finish("boss check (QF17): Viper's fight done - " + (_viperFight != null ? _viperFight.Summary() : ""));
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
                // QF17 (6): in her hand at chest height, framed on the hand from in front (never at her hip)
                var fw = Flat(Seven.transform.forward).normalized;
                var lp = P7 + Vector3.up * 1.22f + fw * 0.4f - Seven.transform.right * 0.08f;
                var tik = Seven.gameObject.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
                tik.enabled = true; tik.Ease = 3f; tik.LeftPos = lp; tik.LeftW = 1f; tik.LookPos = lp; tik.LookW = 0.8f;
                yield return Wait(0.7f);
                RecordTet.position = lp + Vector3.up * 0.07f;
                Shoot(lp + fw * 0.75f + Vector3.up * 0.18f + Seven.transform.right * 0.2f, lp + Vector3.up * 0.04f, 28f, 0.01f);
                yield return Wait(0.6f);
                yield return Shot("m37_insert_the_small_tetrahedron_beside_the_other_records");
                tik.ReleaseAll("the Mars record insert");
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
