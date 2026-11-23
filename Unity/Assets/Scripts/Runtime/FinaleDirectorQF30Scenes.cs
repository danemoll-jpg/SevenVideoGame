using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using L = Seven.FinaleLayout;

namespace Seven
{
    /// <summary>QF30 — Dan's review of 1.0.0-rc1, the ending restaged: FIN_09 (the danger is real: the drill at her back; "Seven!"; he grows
    /// mid-stride and leaps between them; a hard cut to black on the impact, the music dead; back on him already falling, still large),
    /// FIN_10 (he dies clearly and gently in her arms, shrinking as the life leaves him; "Panther, no!"), FIN_11 (she carries him and lays
    /// him on the archive; the Scree take Tunneler; the Archive asks her to continue), FIN_13 (she walks into the light and rises).</summary>
    public partial class FinaleDirector
    {
        Beacon _conduitBeacon, _barrierBeacon;
        AudioSource _purr, _barrierHum;
        bool _handsOn; bool _endingAudio;

        void ChapterSevenOff() { if (Seven.InFight) Seven.EndFight(); }
        void NoHud(bool on)
        {
            Objective(null); SetPrompt(null); HideGuide(); UI.HideBossBar();
            if (Legend != null) Legend.HidePanels = true;
        }
        /// <summary>An animated turn (never a snap): her facing eases to <paramref name="dir"/> over <paramref name="secs"/>.</summary>
        IEnumerator TurnTo(Vector3 dir, float secs = 0.6f)
        {
            dir = Flat(dir); if (dir.sqrMagnitude < 1e-4f) yield break;
            var q0 = Seven.transform.rotation; var q1 = Quaternion.LookRotation(dir);
            float ang = Quaternion.Angle(q0, q1); if (ang < 2f) yield break;
            for (float t = 0f; t < 1f; t += Time.deltaTime / secs) { Seven.transform.rotation = Quaternion.Slerp(q0, q1, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            Seven.transform.rotation = q1;
            Log($"TURN (QF30 14): she turns {ang:0} deg over {secs:0.0} s (animated, never a snap)");
        }
        SevenTableIK IK { get { var ik = Seven.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>(); ik.enabled = true; return ik; } }

        // ------------------------------------------------------------------------------------------ FIN_09: the leap (restaged)
        IEnumerator TheLeap()
        {
            yield return Cutscene(true);
            UI.HideBossBar();
            if (_fight != null) { _fight.End(); _fight.enabled = false; }
            ChapterSevenOff();                                       // she walks / uses telekinesis normally from here (the trap is not a fight move)
            if (Rig != null) { Rig.UpdateDeck(false); Rig.Drop(false); }
            _rigUp = true; _sag = 0f;
            if (!FinaleState.SceneDone("FIN_09_leap"))
            {
                // the connection restored: the console's light-lines run again
                BeaconDone(_conduitBeacon, 1.2f); Glow(ConduitBreak, 0.6f); Glow(ConduitLines, 1.2f); Glow(ConduitNode, 1.4f);
                if (Shots) { ImageGate.ExpectPoint(L.ConduitAt + Vector3.up * 0.9f, "the conduit's control, restored"); yield return Cap.ShotFrom("f38c_the_conduit_control_restored", L.ConduitAt + Flat(L.ChamberC - L.ConduitAt).normalized * 3.4f + Vector3.up * 1.9f + Vector3.forward * 1.2f, L.ConduitAt + Vector3.up * 0.5f, 50f); }
                StartCoroutine(DialogueFraming.Over("the restored conduit (the Archive's voice)", SayIds("FIN_08_P6")));   // Connection restored.
                // 1. the danger is real and clear: she turns back toward the platform, unaware; the rig roars up behind her, the drill at her back
                var A = new Vector3(5.6f, L.Y, -159.6f); var toP = Flat(L.PlatformAt - A).normalized; var side = Vector3.Cross(Vector3.up, toP);
                Seven.Place(A, toP);
                var B0 = A - toP * 5.4f; var stopAt = A - toP * 3.0f;
                TunnelerCraft.position = B0; TunnelerCraft.rotation = Quaternion.LookRotation(toP);
                DrillSpeed = 1400f; if (Rig != null) Rig.ArmDrillWant = 1700f;
                var roar = AudioKit.Play("europa_drill", TunnelerCraft.position, 1f, 0f, AudioKit.Bus.Sfx, 40f);
                Shoot(A + toP * 1.0f + side * 0.6f + Vector3.up * 1.2f, TunnelerCraft.position + Vector3.up * 1.5f, 52f, 0.01f);   // low, past her shoulder: the drill coming
                ImageGate.Expect(TunnelerCraft, "his drill driving at her back");
                for (float t = 0f; t < 0.9f; t += Time.deltaTime) { TunnelerCraft.position = Vector3.MoveTowards(TunnelerCraft.position, stopAt, 2.6f * Time.deltaTime); yield return null; }
                yield return Shot("f39_the_drill_drives_at_her_back");
                HerFaceCloseUp();                                                                                // then her, unaware
                for (float t = 0f; t < 0.6f; t += Time.deltaTime) { TunnelerCraft.position = Vector3.MoveTowards(TunnelerCraft.position, stopAt, 2.6f * Time.deltaTime); yield return null; }
                yield return Shot("f39b_her_unaware");
                // 2. "Seven!" - Panther at the chamber's edge, already moving
                var G = Vector3.Lerp(A, TunnelerCraft.position, 0.5f);
                var P0 = new Vector3(3.0f, L.Y, -166.8f); var run = Flat(G - P0).normalized;
                if (Cat != null)
                {
                    Cat.enabled = true; Cat.gameObject.SetActive(true); Cat.transform.localScale = Vector3.one; Cat.Place(P0, run);
                    Cat.GoTo(G - run * 1.4f, 5.5f, G);
                    _trackCat = true; StartCoroutine(TrackCat(run));
                    ImageGate.Expect(Cat.transform, "Panther already running");
                    StartCoroutine(ShotAfter(0.25f, "f40a_panther_already_moving"));
                }
                yield return Say("FIN_09", 1, 1);                       // Seven!
                _trackCat = false;
                // 3. he grows mid-stride into his large combat form and leaps between them: wide, side-on, all three in frame
                var axis = Flat(TunnelerCraft.position - A).normalized; var sd = Vector3.Cross(Vector3.up, axis);
                Shoot(G + sd * 9.0f + Vector3.up * 1.8f, G + Vector3.up * 1.1f, 58f, 0.01f);
                StartCoroutine(TurnTo(TunnelerCraft.position - P7, 0.5f));
                if (Cat != null)
                {
                    for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f) { Cat.transform.localScale = Vector3.one * Mathf.Lerp(1f, LargeScale, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
                    Cat.Hop(G, 0.6f, 1.5f);
                    for (float t = 0f; t < 0.3f; t += Time.deltaTime) yield return null;
                    ImageGate.Expect(Cat.transform, "Panther in the air between her and the drill");
                    yield return Shot("f40_panther_leaps_between_them");
                }
                // 4. cut on the moment the drill meets him: black, the drill's sound stops, the music cut dead - no impact shown
                UI.SetFade(1f, 0f);
                if (roar != null) roar.Stop();
                MusicSystem.Silence("FIN_09 impact - Panther's death (FIN_10) - the start of FIN_11", 240f); MusicSystem.Stop(0.05f); ThemeNow = null;
                if (Rig != null) Rig.ArmDrillWant = 0f;
                DrillSpeed = 0f;
                yield return Shot("f41_black_on_the_impact");
                for (float t = 0f; t < 0.9f; t += Time.deltaTime) yield return null;
                // 5. back on him already falling: he hits the floor, slides, collapses - still large
                TunnelerCraft.position = A - toP * 4.8f; TunnelerCraft.rotation = Quaternion.LookRotation(toP);
                var land = L.PantherFalls - Flat(L.PantherFalls - G).normalized * 1.4f;
                if (Cat != null) { Cat.enabled = false; Cat.Scripted("jump"); Cat.enabled = false; }
                Shoot(L.PantherFalls + new Vector3(1.2f, 0.9f, 4.2f), L.PantherFalls + Vector3.up * 0.5f, 52f, 0.01f);
                UI.SetFade(0f, 0f);
                if (Cat != null)
                {
                    var from = G + Vector3.up * 1.0f; var q0 = Cat.transform.rotation;
                    for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f) { Cat.transform.position = Vector3.Lerp(from, land, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.3f; Cat.transform.rotation = Quaternion.Slerp(q0, Quaternion.LookRotation(Flat(L.PantherHead - L.PantherFalls)) * Quaternion.Euler(0f, 0f, 78f), t); yield return null; }
                    AudioKit.Play("land_hard", land, 1f);
                    LayCat(land, L.PantherHead - L.PantherFalls, LargeScale, L.Y);
                    var s0 = Cat.transform.position; var s1 = s0 + (L.PantherFalls - land);
                    for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f) { Cat.transform.position = Vector3.Lerp(s0, s1, 1f - (1f - t) * (1f - t)); yield return null; }
                    var ov = CatOv; if (ov != null) { ov.Alive = true; ov.Breath = 0.9f; ov.BreathRate = 0.7f; }
                    ImageGate.Expect(Cat.transform, "Panther fallen, still in his large form");
                    yield return Shot("f41b_he_falls_still_large");
                }
                Log("FIN_09 (QF30 12): the drill at her back, 'Seven!', he grows mid-stride and leaps between them, a hard cut to black on the impact (no impact shown, the drill's sound and the music cut dead), back on him already falling - large");
                // her reaction: she starts toward him
                HerFaceCloseUp(); yield return Wait(0.5f);
                yield return Shot("f41c_her_reaction");
                Seven.Lock(false); yield return WalkSeven(Vector3.Lerp(P7, L.KneelSpot, 0.45f), 1.4f); Seven.Lock(true);
                // Tunneler moves again toward the archive
                _rigUp = true; DrillSpeed = 600f; if (Rig != null) Rig.ArmDrillWant = 600f;
                for (float t = 0f; t < 1.0f; t += Time.deltaTime) { TunnelerCraft.position = Vector3.MoveTowards(TunnelerCraft.position, A - toP * 2.6f + side * 1.6f, 1.6f * Time.deltaTime); yield return null; }
                Shoot(L.PantherFalls + new Vector3(2.6f, 1.3f, 3.6f), Vector3.Lerp(L.PantherHead, P7, 0.4f) + Vector3.up * 0.4f, 46f, 0.01f);
                if (Cat != null) ImageGate.Expect(Cat.transform, "Panther, still conscious, and her");
                yield return Say("FIN_09", 2, 2);                       // The barrier.
                FinaleState.CompleteScene("FIN_09_leap");
            }
            else
            {
                PantherFallen(true);
                TunnelerCraft.position = L.ChamberC + new Vector3(-4.6f, 0f, 2.6f);
                MusicSystem.Silence("FIN_09 - the barrier (retry from the save)", 240f);
            }
            // 6. the final player action: she drives him into the prepared barrier (telekinesis), the worker locks it
            Objective("Drive him into the barrier");
            var bb = Highlight("the barrier", L.BarrierAt + new Vector3(-0.6f, 0f, 0f), null, BarrierFrameLines);
            // she faces him, the camera behind her: he is in view the moment control comes back (the marker shows the barrier too)
            if (Seven.InFight) Seven.EndFight(); if (Seven.Current != ChapterSeven.Mode.Free && Seven.Current != ChapterSeven.Mode.Locked) Seven.Release();
            yield return TurnTo(TunnelerCraft.position - P7, 0.5f);
            yield return Cutscene(false);
            Cam.SnapBehind();
            Log($"FIN_09: control back for the trap - 7 {Seven.Current}, {Flat(TunnelerCraft.position - P7).magnitude:0.0} m from him, facing him");
            yield return Telekinesis(TunnelerCraft.position + Vector3.up * 1.2f, 18f, "Tunneler into the barrier", "Drive him into the barrier's alcove (telekinesis)", null, null, TunnelerCraft);
            Objective(null); BeaconDone(bb, 1.4f);
            yield return Cutscene(true);
            var a2 = TunnelerCraft.position; var into = L.BarrierAt + new Vector3(0.6f, 0f, 0f);
            var mid = L.ChamberC + new Vector3(2f, 0f, 5.6f);                                 // round the platform's north side (never through it)
            Shoot(L.ChamberC + new Vector3(2f, 3.2f, 9.6f), L.BarrierAt + Vector3.up * 2f, 56f, 0.01f);
            AudioKit.Play("tk_push", a2, 1f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.6f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                var p = (1f - u) * (1f - u) * a2 + 2f * (1f - u) * u * mid + u * u * into;
                TunnelerCraft.position = p + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.6f;
                var d = Flat(into - p); if (d.sqrMagnitude > 0.01f) TunnelerCraft.rotation = Quaternion.Slerp(TunnelerCraft.rotation, Quaternion.LookRotation(-d), Time.deltaTime * 4f);
                yield return null;
            }
            TunnelerCraft.rotation = Quaternion.LookRotation(Vector3.left);
            if (BarrierField != null) BarrierField.gameObject.SetActive(true);
            AudioKit.Play("barrier_close", L.BarrierAt + Vector3.up * 2f, 1f);
            _barrierHum = AudioKit.Loop("bia_barrier", null, L.BarrierAt + Vector3.up * 2f, 0.22f, AudioKit.Bus.Sfx, 2f, 20f);
            ImageGate.Expect(TunnelerCraft, "Tunneler inside the barrier");
            yield return Shot("f42_the_barrier_closes_on_him");
            Shoot(Lock.position + new Vector3(-3f, -0.6f, -2.4f), Lock.position, 40f, 0.01f);
            Glow(BarrierLight, 2.0f); if (Lock != null) foreach (var r in Lock.GetComponentsInChildren<Renderer>()) Glow(r, 1.6f);   // the lock light
            yield return DialogueFraming.Over("the external lock (the worker over the network)", Say("FIN_09", 3, 3));   // We have the lock.
            // he presses against the barrier once, then gives up; his machinery powers down, the drills spin down, the rig sags to the floor
            Shoot(L.BarrierAt + new Vector3(-6.5f, 2.2f, 3.4f), TunnelerCraft.position + Vector3.up * 1.6f, 50f, 0.01f);
            var c0 = TunnelerCraft.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.7f) { TunnelerCraft.position = c0 + Vector3.left * Mathf.Sin(t * Mathf.PI) * 0.45f; yield return null; }
            AudioKit.Play("bia_barrier", L.BarrierAt + Vector3.up * 2f, 0.9f); if (BarrierField != null) foreach (var r in BarrierField.GetComponentsInChildren<Renderer>()) StartCoroutine(FieldFlash(r));
            DrillSpeed = 0f; _rigUp = false; _sag = 7f;
            if (Rig != null) Rig.PowerDown();
            foreach (var l in TunnelerCraft.GetComponentsInChildren<Light>()) l.enabled = false;
            AudioKit.Play("mars_barrier_off", TunnelerCraft.position, 0.6f);
            ImageGate.Expect(TunnelerCraft, "Tunneler contained");
            yield return Wait(1.6f);
            yield return Shot("f43_contained_the_rig_sagged_the_drills_spinning_down");
            Log("FIN_09: Tunneler contained in the old maintenance barrier, alive - the field closed and humming, the lock light on, his thrusters dead, the drills spinning down, the rig sagged on the floor; he pressed against it once (QF30 16)");
            Complete("FIN_09");
            ChapterSevenOff();
        }
        IEnumerator FieldFlash(Renderer r)
        {
            var m = r.material; float k0 = m.HasProperty("_Intensity") ? m.GetFloat("_Intensity") : 0.18f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f) { if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", Mathf.Lerp(0.6f, k0, t)); yield return null; }
        }

        // ------------------------------------------------------------------------------------------ FIN_10: eight more (no HUD, no music)
        IEnumerator EightMore()
        {
            yield return Cutscene(true);
            NoHud(true);
            QF30Watch.Free("FIN_10 - Panther's death and the long silence (scripted)");
            MusicSystem.Silence("FIN_10 - Panther's death, the long hold", 240f);
            if (Cat == null) yield break;
            if (Cat.enabled || Cat.transform.localScale.x < 2f) PantherFallen(true);
            var ov = CatOv; if (ov != null) { ov.Alive = true; ov.Slack = 0f; ov.SettleTo = null; ov.Breath = 0.85f; ov.BreathRate = 0.55f; }
            // she reaches him and kneels at his head, gathering his head against her (he is still large)
            // QF30 (15): she kneels right at his head - measured from his head bone as he lies (rc2 draft: a fixed spot a metre short)
            var hb = CatBone("head"); var headP = hb != null ? hb.position : L.PantherHead; var body = CatBounds().center;
            var away = Flat(headP - body); if (away.sqrMagnitude < 0.01f) away = Vector3.right; away.Normalize();
            var kneel = Flat(headP) + away * 0.5f + Vector3.Cross(Vector3.up, away) * 0.12f; kneel.y = L.Y;
            var faceHim = Flat(headP - kneel);
            Shoot(L.PantherFalls + new Vector3(2.4f, 1.4f, 4.2f), L.PantherFalls + Vector3.up * 0.5f + Vector3.right * 0.6f, 48f, 0.01f);
            Seven.Lock(false); yield return WalkSeven(kneel, 3f); Seven.Lock(true);
            yield return TurnTo(faceHim, 0.5f);
            Seven.Place(kneel, faceHim);
            var rest = SevenRestPose.On(Seven.gameObject, SevenRestPose.Kind.Kneel); rest.EaseIn = true; rest.PinHere(); rest.Bow = 0f; rest.Sob = 0f;
            _purr = Purr.Loop(Cat.transform, 0.5f);
            _handsOn = true; StartCoroutine(HandsOnHim());
            StartCoroutine(BreathSounds());
            var fwd = Seven.transform.forward; var side = Vector3.Cross(Vector3.up, fwd);
            Shoot(P7 + fwd * 0.55f + side * 2.5f + Vector3.up * 0.9f, P7 + fwd * 0.5f + Vector3.up * 0.4f, 40f, 0.01f);
            ImageGate.Expect(Seven.transform, "7 kneeling at his head, his head against her");
            yield return Wait(1.4f);
            yield return Shot("f44_she_gathers_his_head_against_her");
            yield return Say("FIN_10", 1, 3);                           // Hey. Hey, I'm here. / I know. / We'll get you back to the ship.
            // FIN_10_04: he makes a small effort to press his head into her hand
            if (ov != null) ov.LookAt = HandR();
            Shoot(HandR() + side * 0.75f + fwd * 0.15f + Vector3.up * 0.3f, HandR(), 34f, 0.01f);
            ImageGate.Expect(Cat.transform, "his head pressed into her hand (insert)");
            yield return Wait(1.8f);
            yield return Shot("f45_insert_his_head_into_her_hand");
            Shoot(P7 + fwd * 1.0f + side * 1.3f + Vector3.up * 0.95f, HerHead + Vector3.down * 0.3f, 38f, 0.01f);
            yield return Say("FIN_10", 5, 5);                           // Just stay with me.
            Shoot(HandR() + side * 0.85f + Vector3.up * 0.35f, HandR(), 32f, 0.01f);
            yield return Wait(2.0f);
            yield return Shot("f45b_insert_her_hands_on_his_fur");
            Shoot(P7 + fwd * 0.55f + side * 2.5f + Vector3.up * 0.9f, P7 + fwd * 0.5f + Vector3.up * 0.4f, 40f, 0.01f);
            yield return Say("FIN_10", 6, 6);                           // Don't worry... I have eight more.
            PlayReaction("seven", "laugh_soft");                      // a tiny, involuntary laugh through tears
            yield return Wait(1.0f);
            yield return Say("FIN_10", 8, 8);                           // You'd better.
            if (ov != null) ov.LookAt = null;
            // his breathing slows; as the life leaves him he shrinks, slowly, back to his ordinary size - her arms follow him in
            Shoot(P7 + fwd * 0.75f + side * 1.9f + Vector3.up * 0.75f, P7 + fwd * 0.5f + Vector3.up * 0.3f, 40f, 0.01f);
            var head0 = CatBone("head"); var anchor = head0 != null ? head0.position : Cat.transform.position;
            float sc0 = Cat.transform.localScale.x; float shrink = Auto ? 5f : 8f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / shrink)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                if (ov != null) { ov.BreathRate = Mathf.Lerp(0.55f, 0.2f, u); ov.Breath = Mathf.Lerp(0.85f, 0.35f, u); }
                float s = Mathf.Lerp(sc0, 1f, u), k = s / Cat.transform.localScale.x;
                Cat.transform.position = anchor + (Cat.transform.position - anchor) * k; Cat.transform.localScale = Vector3.one * s;
                var b = CatBounds(); if (b.min.y < L.Y + 0.005f || b.min.y > L.Y + 0.03f) Cat.transform.position += Vector3.up * (L.Y + 0.012f - b.min.y);
                if (_purr != null) _purr.volume = Mathf.Lerp(0.5f, 0.15f, u) * AudioKit.Gain(AudioKit.Bus.Voice);
                if (t > 0.5f && !_shrinkShot) { _shrinkShot = true; StartCoroutine(Shot("f45c_he_shrinks_as_the_life_leaves_him")); }
                yield return null;
            }
            Cat.transform.localScale = Vector3.one;
            // one last breath, then stillness: the breathing stops, his head settles into her hand, ears slack, the tail limp; the purr stops
            if (ov != null) { ov.BreathRate = 0.18f; for (float t = 0f; t < 2.4f; t += Time.deltaTime) yield return null; ov.Breath = 0f; ov.Alive = false; ov.Slack = 1f; ov.SettleTo = HandR(); }
            _breathing = false;
            if (_purr != null) { _purr.Stop(); Destroy(_purr.gameObject); _purr = null; }
            Log("FIN_10 (QF30 14/15): his breathing slowed and stopped after one last breath - he shrank to his ordinary size as the life left him; stillness (head settled in her hand, ears slack, tail limp), the purr stopped; total silence");
            Shoot(P7 + fwd * 0.6f + side * 1.6f + Vector3.up * 0.55f, P7 + fwd * 0.45f + Vector3.up * 0.25f, 36f, 0.01f);
            yield return Wait(2.6f);                                    // she waits for another breath. None comes.
            yield return Shot("f46a_stillness_none_comes");
            // "Panther, no!" - broken, disbelieving; she cries a little, her head bowed over him
            rest.Bow = 22f; rest.Sob = 1f;
            Shoot(P7 + fwd * 1.1f + side * 1.0f + Vector3.up * 0.85f, HerHead + Vector3.down * 0.35f, 40f, 0.01f);
            yield return SayIds("FIN_10_08b");                          // Panther, no!
            yield return Wait(Auto ? 1.5f : 3f);
            rest.Sob = 0.3f;
            // her hand strokes him once, then is still
            _stroke = true; for (float t = 0f; t < 1.6f; t += Time.deltaTime) { _strokeU = Mathf.SmoothStep(0f, 1f, t / 1.6f); yield return null; } _stroke = false;
            Shoot(HandR() + side * 0.85f + Vector3.up * 0.35f, HandR(), 32f, 0.01f);
            yield return Wait(0.6f);
            yield return Shot("f46_her_hand_still_on_his_fur");
            // a substantial silence
            rest.Sob = 0f;
            Shoot(P7 + fwd * 1.6f + side * 3.4f + Vector3.up * 2.0f, P7 + fwd * 0.3f + Vector3.up * 0.4f, 42f, 2.5f);
            yield return Wait(Auto ? 4f : 8f);
            Log("FIN_10: no HUD, no music; 'Panther, no!' and her tears; the long hold (silence)");
            Complete("FIN_10");
        }
        bool _shrinkShot, _breathing, _stroke; float _strokeU;
        Vector3 HandR() { var a = Seven.AnimNow; var h = a != null ? a.GetBoneTransform(HumanBodyBones.RightHand) : null; return h != null ? h.position : P7 + Vector3.up * 0.5f; }
        /// <summary>FIN_10: her hands rest ON his fur (targets on his head and neck surface every frame - they follow his breathing and the
        /// shrink; palms toward him, fingers relaxed); a stroke runs the right hand from his head to his neck once.</summary>
        IEnumerator HandsOnHim()
        {
            var ik = IK; ik.Ease = 1.4f; ik.Alive = true; ik.Spread = 0.6f;
            while (_handsOn && Cat != null)
            {
                var head = CatBone("head"); var neck = CatBone("neck") ?? head; if (head == null) { yield return null; continue; }
                var her = P7 + Vector3.up * 0.6f; float s = Cat.transform.lossyScale.x;
                var outH = (her - head.position); outH.y = Mathf.Max(outH.y, 0.15f); outH.Normalize(); var up = (outH + Vector3.up).normalized;
                var hp = _stroke ? Vector3.Lerp(head.position, neck.position, _strokeU) : head.position;
                ik.RightPos = hp + up * 0.055f * s + Vector3.up * 0.015f; ik.RightPalmN = up; ik.RightPalmW = 1f; ik.RightFingerDir = Flat(Cat.transform.forward); ik.RightW = 1f;
                var under = (her - neck.position); under.y = 0f; under.Normalize();
                ik.LeftPos = neck.position + under * 0.06f * s + Vector3.up * 0.01f; ik.LeftPalmN = under; ik.LeftPalmW = 0.8f; ik.LeftFingerDir = Vector3.down; ik.LeftW = 0.9f;
                ik.LeftHint = P7 + Vector3.up * 0.7f - Seven.transform.right * 0.5f; ik.LeftHintW = 0.6f; ik.RightHint = P7 + Vector3.up * 0.8f + Seven.transform.right * 0.5f; ik.RightHintW = 0.6f;
                ik.LookPos = head.position; ik.LookW = 1f;
                yield return null;
            }
        }
        IEnumerator BreathSounds()
        {
            _breathing = true; var ov = CatOv; float next = 0f;
            while (_breathing && Cat != null && ov != null)
            {
                next -= Time.deltaTime;
                if (next <= 0f && ov.Breath > 0.05f) { var head = CatBone("head"); AudioKit.Play("cat_breath_slow", head != null ? head.position : Cat.transform.position, Mathf.Lerp(0.25f, 0.6f, ov.Breath), 0.03f, AudioKit.Bus.Sfx, 8f); next = 1f / Mathf.Max(0.1f, ov.BreathRate); }
                yield return null;
            }
        }

        // ------------------------------------------------------------------------------------------ FIN_11: what she carries
        IEnumerator WhatSheCarries()
        {
            yield return Cutscene(true);
            NoHud(true);
            QF30Watch.Free("FIN_11 - before the Archive asks");
            if (Cat != null && (Cat.enabled || Cat.transform.localScale.x > 1.2f)) { PantherFallen(false); var o = CatOv; if (o != null) { o.Breath = 0f; o.Alive = false; o.Slack = 1f; } }
            var rest = Seven.GetComponent<SevenRestPose>();
            if (rest == null || rest.Pose != SevenRestPose.Kind.Kneel) { Seven.Place(L.KneelSpot, Flat(L.PantherHead - L.KneelSpot)); rest = SevenRestPose.On(Seven.gameObject, SevenRestPose.Kind.Kneel); rest.EaseIn = false; rest.PinHere(); _handsOn = true; StartCoroutine(HandsOnHim()); }
            var fwd = Seven.transform.forward; var side = Vector3.Cross(Vector3.up, fwd);
            // the archive waits for her: a distant light pulses gently, no timer
            Shoot(P7 + fwd * 2.4f + Vector3.up * 1.4f, L.PlatformAt + Vector3.up * 1f, 48f, 0.01f);
            for (float t = 0f; t < (Auto ? 2.5f : 5f); t += Time.deltaTime) { Glow(PlatformSurface, 0.3f + 0.25f * Mathf.Sin(t * 1.6f)); yield return null; }
            yield return Shot("f47_the_archive_pulses_waiting");
            if (Worker != null) { Worker.gameObject.SetActive(true); Worker.transform.position = new Vector3(11.5f, L.Y, -144f); }
            yield return DialogueFraming.Over("the chamber doorway (the worker's voice over the network)", Say("FIN_11", 1, 1));   // Seven?
            Shoot(P7 + fwd * 1.1f + Vector3.up * 1.0f, HerHead + Vector3.down * 0.15f, 36f, 0.01f);
            yield return Say("FIN_11", 2, 2);                           // I'm here.
            // "The drilling has stopped" - and it has: the drill in the wall, still, the dust settled
            var wd = WallDrills != null && WallDrills.Length > 0 ? WallDrills[0] : null;
            if (wd != null) { DrillShot(wd); ImageGate.Expect(wd, "the drill stopped in the wall"); }
            yield return DialogueFraming.Over("the stopped drill (the worker's voice)", Say("FIN_11", 3, 3));
            yield return Shot("f47b_the_drilling_has_stopped");
            Shoot(P7 + fwd * 1.1f + Vector3.up * 1.0f, HerHead + Vector3.down * 0.15f, 36f, 0.01f);
            yield return Say("FIN_11", 4, 4);                           // Good.
            yield return DialogueFraming.Over("the chamber doorway (the worker's voice)", Say("FIN_11", 5, 5));   // We're coming down.
            // the Scree are already down (a cut, never a walk to wait for): at the barrier, pulling his cables, clamping the restraints
            if (Crew != null) for (int i = 0; i < Crew.Length; i++) if (Crew[i] != null) { Crew[i].gameObject.SetActive(true); Crew[i].transform.position = L.CrewAt(i); Crew[i].transform.rotation = Quaternion.LookRotation(Flat(L.BarrierAt - L.CrewAt(i))); }
            Restrained(true);
            yield return Wait(Auto ? 1.0f : 2.0f);
            // she rises facing him (the way she was facing), then lifts him gently in both arms against her chest
            _handsOn = false; IK.ReleaseAll("she lifts him (FIN_11)");
            if (rest != null) { rest.RiseTime = 1.4f; rest.Rise(); for (float t = 0f; t < 1.6f; t += Time.deltaTime) yield return null; }
            yield return CarryLift();
            Shoot(P7 + fwd * 1.25f + side * 0.35f + Vector3.up * 1.3f, P7 + Vector3.up * 1.2f, 38f, 0.01f);
            ImageGate.Expect(Seven.transform, "7 holding him against her chest");
            yield return SayIds("FIN_11_05a");                          // You're staying with me.
            yield return Shot("f47c_youre_staying_with_me");
            // she turns (animated) and carries him to the platform; the Scree at the barrier behind
            yield return TurnTo(L.Stand(0f) - P7, 0.8f);
            _trackHer = true; StartCoroutine(TrackHer());
            StartCoroutine(CarryShot());
            Seven.Lock(false); yield return WalkSeven(L.Stand(0f), 10f); Seven.Lock(true);
            _trackHer = false;
            yield return TurnTo(Vector3.back, 0.5f);
            // she lays him on the archive beside the surface; her hand stays near him, relaxed
            yield return LayHimDown();
            SideOfArchive(1.0f);
            ImageGate.Expect(Cat.transform, "Panther laid on the archive, her hand near him");
            yield return Wait(0.8f);
            yield return Shot("f49_she_lays_him_on_the_archive_her_hand_near_him");
            // one short cut: Tunneler chained, the Scree round him - his last quip and their answer (never during FIN_10's silence)
            if (Elder != null) { Elder.gameObject.SetActive(true); Elder.transform.position = L.CrewAt(1) + new Vector3(0.4f, 0f, 1.2f); Elder.transform.rotation = Quaternion.LookRotation(Flat(L.BarrierAt - Elder.transform.position)); }
            var tb = Tunneler != null ? Tunneler.position : TunnelerCraft.position;
            Shoot(L.BarrierAt + new Vector3(-6.4f, 1.0f, 2.6f), tb + Vector3.up * 1.4f, 44f, 0.01f);
            if (_faceKey == null) { _faceKey = new GameObject("TunnelerFaceKey").AddComponent<Light>(); _faceKey.type = LightType.Point; _faceKey.range = 6f; _faceKey.intensity = 3.5f; _faceKey.shadows = LightShadows.None; }
            _faceKey.enabled = true; _faceKey.transform.position = tb + Vector3.up * 1.8f + Vector3.left * 1.6f;
            ImageGate.Expect(Tunneler != null ? Tunneler : TunnelerCraft, "Tunneler chained, the Scree round him");
            yield return Shot("f49b_tunneler_chained_the_scree_round_him");
            yield return SayIds("FIN_11_05b");                          // Fine. Keep your history. You'll still need someone to run the drills.
            if (Elder != null) Shoot(Elder.transform.position - Elder.transform.forward * 1.8f + Elder.transform.right * 0.9f + Vector3.up * 1.7f, tb + Vector3.up * 1.2f, 42f, 0.01f);
            yield return SayIds("FIN_11_05c");                          // We always have.
            DrillSpeed = 0f;                                            // his drill head spins down to a stop on it
            yield return Wait(1.2f);
            KeyOff();
            Log("FIN_11 (QF30 16): the Scree took him - restraints on him and the rig, his cables pulled, his last quip and their answer; his drill spun down to a stop");
            // she straightens up; her hand leaves him
            IK.ReleaseAll("she straightens up (FIN_11)");
            Seven.Place(L.Stand(0f), Vector3.back);
            // the Archive asks her (a few seconds after she straightens up, wherever she is; once more, softly, if she lingers); no timer
            QF30Watch.EndFree();
            Objective("Return to the archive");
            var ab = Highlight("the archive", L.PlatformAt + new Vector3(0.3f, 0f, L.PlatformR * 0.5f), null, PlatformSurface);
            yield return Cutscene(false);
            bool touched = false; StartCoroutine(ArchiveAsks(() => touched));
            yield return Interact(L.Stand(0f), "Touch the archive", "the archive (FIN_11)", "The archive in the middle of the chamber", new[] { L.Stand(0f) + new Vector3(0f, 0f, 0.1f) }, null, 2.2f);
            touched = true; BeaconDone(ab, 0.35f); Objective(null);
            yield return Cutscene(true);
            MusicSystem.EndSilence(); MusicSystem.Play("moon_transfer", 3f); ThemeNow = "moon_transfer";
            Seven.Place(L.Stand(0f), Vector3.back);
            HerFaceCloseUp();
            yield return Say("FIN_11", 7, 7);                           // Yes.
            yield return HandsOnArchiveAndHim();
            yield return Say("FIN_11", 8, 8);                           // All of it.
            SideOfArchive(0.9f);
            yield return Wait(0.8f);
            yield return Shot("f49c_one_hand_on_the_archive_one_on_him");
            // the memories: the ancient visions briefly, then their own moments
            for (int i = 0; i < 7; i++) yield return Fragment(i, 1.3f, $"f50_memory_video_frame_vision_{i + 1}", 5f);
            if (Memories != null)
                for (int i = 0; i < Memories.Length; i++)
                {
                    var m = Memories[i];
                    for (float t = 0f; t < 0.5f; t += Time.deltaTime) { UI.SetVideo(m, m.width / (float)m.height, t / 0.5f); yield return null; }
                    UI.SetVideo(m, m.width / (float)m.height, 1f);
                    yield return Shot($"f51_memory_video_frame_{i + 1}_{m.name}");
                    yield return Wait(Auto ? 1.2f : 2.4f);
                    for (float t = 0f; t < 0.5f; t += Time.deltaTime) { UI.SetVideo(m, m.width / (float)m.height, 1f - t / 0.5f); yield return null; }
                }
            UI.SetVideo(null, 1f, 0f);
            Log($"FIN_11: the memories - the seven visions, then {(Memories != null ? Memories.Length : 0)} of their own moments ({string.Join(", ", (Memories ?? new Texture[0]).Select(x => x.name))})");
            Complete("FIN_11");
        }
        IEnumerator ArchiveAsks(System.Func<bool> touched)
        {
            for (float t = 0f; t < 3f; t += Time.deltaTime) { if (touched()) yield break; yield return null; }
            if (!touched()) yield return DialogueFraming.Over("the archive (its voice)", Say("FIN_11", 6, 6));   // Continue?
            for (float t = 0f; t < 20f; t += Time.deltaTime) { if (touched()) yield break; yield return null; }
            if (!touched()) { Log("FIN_11 (QF30 16): she lingered - the Archive asks once more, softly"); yield return DialogueFraming.Over("the archive (its voice)", SayIds("FIN_11_06b")); }
        }
        bool _trackCat, _trackHer;
        IEnumerator ShotAfter(float s, string n) { yield return Wait(s); yield return Shot(n); }
        IEnumerator TrackCat(Vector3 run)
        {
            var side = Vector3.Cross(Vector3.up, run);
            while (_trackCat && Cat != null) { var c = Cat.transform.position; var wallImg = L.ChamberC + Flat(c - L.ChamberC).normalized * (L.ChamberR - 0.2f) + Vector3.up * 3.2f; var back = Flat(c - wallImg).normalized; Shoot(c + back * 2.4f + Vector3.Cross(Vector3.up, back) * 0.6f + Vector3.up * 0.8f, Vector3.Lerp(c + Vector3.up * 0.3f, wallImg, 0.3f), 56f, 0.01f); /* side-on and low: him running, the chamber's wall behind */ yield return null; }
        }
        IEnumerator TrackHer()
        {
            while (_trackHer) { var f = Flat(Seven.transform.forward).normalized; var sd = Vector3.Cross(Vector3.up, f); Shoot(P7 + f * 2.0f + sd * 1.2f + Vector3.up * 1.35f, P7 + Vector3.up * 1.05f, 44f, 0.01f); yield return null; }
        }
        /// <summary>From beside the archive: her face and hands, Panther on the archive, the lit surface.</summary>
        void SideOfArchive(float h)
        {
            var mid = Vector3.Lerp(P7 + Vector3.up * 1.0f, L.PantherOnArchive + Vector3.up * 0.15f, 0.5f);
            var f = Flat(L.PlatformAt - P7).normalized; var sd = Vector3.Cross(Vector3.up, f);
            Shoot(mid - sd * 1.9f + f * 0.25f + Vector3.up * h * 0.4f, mid, 44f, 0.01f);   // on his side (her left)
        }
        IEnumerator CarryShot() { yield return Wait(1.2f); if (Cat != null) ImageGate.Expect(Cat.transform, "she carries him in both arms"); yield return Shot("f48_she_carries_him_in_both_arms"); }
        /// <summary>A proper two-armed carry: he lies across her forearms against her chest; both palms under him.</summary>
        IEnumerator CarryLift()
        {
            if (Cat == null) yield break;
            var from = Cat.transform.position; var q0 = Cat.transform.rotation;
            Cat.transform.SetParent(Seven.transform, true);
            var lp = new Vector3(0.02f, 1.0f, 0.3f); var lq = Quaternion.Euler(0f, -90f, 0f) * Quaternion.Euler(0f, 0f, 70f);
            var ik = IK; ik.Ease = 2f; ik.LeftPalmW = ik.RightPalmW = 1f; ik.LeftPalmN = ik.RightPalmN = Vector3.up;
            _carry = true; StartCoroutine(CarryHands());
            var lp0 = Cat.transform.localPosition; var lq0 = Cat.transform.localRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.1f) { float u = Mathf.SmoothStep(0f, 1f, t); Cat.transform.localPosition = Vector3.Lerp(lp0, lp, u); Cat.transform.localRotation = Quaternion.Slerp(lq0, lq, u); yield return null; }
            Log("FIN_11 (QF30 13/15): she lifts him gently in both arms against her chest (a two-armed carry, palms under him)");
        }
        bool _carry;
        IEnumerator CarryHands()
        {
            var ik = IK;
            while (_carry && Cat != null)
            {
                var c = Cat.transform.position; var r = Seven.transform.right; var f = Seven.transform.forward;
                ik.RightPos = c + r * 0.13f - Vector3.up * 0.07f - f * 0.02f; ik.LeftPos = c - r * 0.15f - Vector3.up * 0.07f - f * 0.02f;
                ik.RightPalmN = Vector3.up; ik.LeftPalmN = Vector3.up; ik.RightFingerDir = -r; ik.LeftFingerDir = r;
                ik.RightHint = P7 + Vector3.up * 1.0f + r * 0.4f - f * 0.1f; ik.LeftHint = P7 + Vector3.up * 1.0f - r * 0.4f - f * 0.1f; ik.RightHintW = ik.LeftHintW = 0.7f;
                ik.RightW = 1f; ik.LeftW = 1f; ik.LookPos = c; ik.LookW = 0.8f;
                yield return null;
            }
        }
        IEnumerator LayHimDown()
        {
            if (Cat == null) yield break;
            var from = Cat.transform.position; var q0 = Cat.transform.rotation;
            Cat.transform.SetParent(null, true);
            var ik = IK; ik.Lean = 14f; ik.LeanDir = Flat(L.PlatformAt - P7);
            var to = L.PantherOnArchive + Vector3.up * 0.12f; var q1 = Quaternion.LookRotation(Vector3.right) * Quaternion.Euler(0f, 0f, 78f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.3f) { float u = Mathf.SmoothStep(0f, 1f, t); Cat.transform.SetPositionAndRotation(Vector3.Lerp(from, to, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.06f, Quaternion.Slerp(q0, q1, u)); yield return null; }
            _carry = false;
            PantherOnTheArchive();
            // her left hand stays near him, relaxed (on his flank); the right hand lets go
            var chest = CatBone("chest") ?? Cat.transform;
            ik.RightW = 0f; ik.RightPalmW = 0f;
            ik.LeftPos = chest.position + Vector3.up * 0.075f; ik.LeftPalmN = Vector3.up; ik.LeftPalmW = 0.8f; ik.LeftFingerDir = Vector3.right; ik.LeftW = 0.7f; ik.LeftHintW = 0f;
            Log("FIN_11 (QF30 15): she lays him on the archive beside its surface (he stays there, visible, to the end); her hand rests near him");
            yield return Wait(0.4f);
        }
        /// <summary>FIN_11_08: one hand on the archive's surface, the other resting on him (on the archive beside it).</summary>
        IEnumerator HandsOnArchiveAndHim()
        {
            var ik = IK; ik.Ease = 2f;
            ik.RightPos = L.HandOnArchive; ik.RightPalmN = Vector3.up; ik.RightPalmW = 1f; ik.RightFingerDir = Vector3.back; ik.RightW = 1f;
            var chest = Cat != null ? (CatBone("chest") ?? Cat.transform) : null;
            if (chest != null) { ik.LeftPos = chest.position + Vector3.up * 0.075f; ik.LeftPalmN = Vector3.up; ik.LeftPalmW = 0.9f; ik.LeftFingerDir = Vector3.back; ik.LeftW = 0.9f; }
            ik.Lean = 12f; ik.LeanDir = Flat(L.PlatformAt - P7);
            ik.LookPos = L.PlatformAt + Vector3.up * 1.1f; ik.LookW = 0.6f;
            yield return Wait(0.6f);
        }

        // ------------------------------------------------------------------------------------------ FIN_13: she can rest - and rises into the light
        IEnumerator SheCanRest()
        {
            yield return Cutscene(true);
            NoHud(true);
            QF30Watch.Free("FIN_13 - the ending (scripted)");
            if (Cat != null && Cat.transform.parent != null) Cat.transform.SetParent(null, true);
            if (Cat != null && (Cat.enabled || Flat(Cat.transform.position - L.PantherOnArchive).magnitude > 0.4f)) PantherOnTheArchive();
            if (Flat(P7 - L.Stand(0f)).magnitude > 0.3f) Seven.Place(L.Stand(0f), Vector3.back);
            if (MusicSystem.Current != "moon_transfer") MusicSystem.Play("moon_transfer", 2f);
            if (Crew != null) for (int i = 0; i < Crew.Length; i++) if (Crew[i] != null && !Crew[i].gameObject.activeSelf) { Crew[i].gameObject.SetActive(true); Crew[i].transform.position = L.CrewAt(i); Crew[i].transform.rotation = Quaternion.LookRotation(Flat(L.BarrierAt - L.CrewAt(i))); }
            Restrained(true);
            // 7 removes her hand from the archive (the other stays on him)
            var ik = IK; ik.RightW = 0f; ik.RightPalmW = 0f; ik.Lean = 6f;
            if (Cat != null && ik.LeftW < 0.1f) { var chest = CatBone("chest") ?? Cat.transform; ik.LeftPos = chest.position + Vector3.up * 0.075f; ik.LeftPalmN = Vector3.up; ik.LeftPalmW = 0.8f; ik.LeftW = 0.7f; }
            // the worker reaches the doorway and stops
            if (Worker != null) { Worker.gameObject.SetActive(true); Worker.transform.position = new Vector3(11.5f, L.Y, -150.9f); Worker.transform.rotation = Quaternion.LookRotation(Vector3.back); }
            Shoot(new Vector3(11.5f, L.Y, -150.9f) + new Vector3(2.4f, 1.6f, -3.6f), new Vector3(11.5f, L.Y + 1.4f, -150.9f), 42f, 0.01f);
            yield return Wait(0.5f);
            yield return Shot("f54_the_worker_stops_at_the_doorway");
            yield return Say("FIN_13", 1, 1);                           // We'll look after him.
            HerFaceCloseUp();
            yield return Wait(1.6f);                                    // she nods, unable to answer at once
            yield return Say("FIN_13", 3, 3);                           // Thank you.
            // the pale light: plain, calm, no symbols, no figure - and a column of it on the floor beside the archive
            var colAt = L.ChamberC + new Vector3(-3.4f, 0f, 4.6f);
            var beam = AscentColumn(colAt);
            if (PaleLight != null)
            {
                PaleLight.enabled = true; var m = PaleLight.material;
                Shoot(L.ChamberC + new Vector3(-6.4f, 1.8f, 7.4f), L.ChamberC + Vector3.up * 4.6f, 54f, 0.01f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 3f) { if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", t * 0.22f); if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white * t * 0.22f); beam.SetFloat("_Intensity", t * 0.16f); yield return null; }
                ImageGate.ExpectPoint(L.ChamberC + Vector3.up * 3f, "the pale light falling into the chamber");
                yield return Shot("f55_a_pale_light_falls_across_the_chamber");
            }
            // she looks up; the voice (not shown, not named)
            ik.LookPos = colAt + Vector3.up * 12f; ik.LookW = 1f;
            { var f = Flat(colAt - P7).normalized; var sd2 = Vector3.Cross(Vector3.up, f); Shoot(HerHead - f * 0.4f + sd2 * 1.5f + Vector3.down * 0.25f, HerHead + f * 0.6f + Vector3.up * 0.35f, 48f, 0.01f); }   // beside her: her face turned up, the column of light beyond   // low, in front of her: her face turned up to the light
            ImageGate.Expect(Seven.transform, "7 looking up into the light");
            yield return DialogueFraming.Over("the pale light (the voice: not shown, not named)", Say("FIN_13", 4, 4));   // You can rest now.
            yield return Shot("f56_she_looks_up_into_the_light");
            // she looks down at Panther and touches his head - her last touch
            if (Cat != null)
            {
                var head = CatBone("head") ?? Cat.transform;
                ik.LookPos = head.position; ik.LookW = 1f;
                ik.LeftPos = head.position + Vector3.up * 0.06f; ik.LeftPalmN = Vector3.up; ik.LeftPalmW = 1f; ik.LeftFingerDir = Vector3.right; ik.LeftW = 1f; ik.Ease = 1.6f; ik.Lean = 10f;
                SideOfArchive(0.8f);
                ImageGate.Expect(Cat.transform, "her last touch of his head");
                yield return Wait(2.2f);
                yield return Shot("f57_she_touches_his_head");
            }
            ik.ReleaseAll("into the light");
            yield return Wait(0.5f);
            // she walks forward into the column of light (a few deliberate steps, facing it)
            yield return TurnTo(colAt - P7, 0.9f);
            Shoot(colAt + new Vector3(-2.6f, 1.2f, 4.4f), Vector3.Lerp(P7, colAt, 0.5f) + Vector3.up * 1.1f, 50f, 0.01f);
            Seven.Lock(false); yield return WalkSeven(colAt, 6f); Seven.Lock(true);
            yield return Shot("f57b_she_walks_into_the_light");
            // standing in it, she lifts gently off the floor and rises, the way the saucer's beam raises her; the light brightens round her
            if (Worker != null) Worker.transform.rotation = Quaternion.LookRotation(Flat(colAt - Worker.transform.position));
            Seven.BeginFloat();
            var rends = Seven.GetComponentsInChildren<Renderer>(); var mpb = new MaterialPropertyBlock();
            Shoot(L.ChamberC + new Vector3(-9.6f, 1.2f, 9.2f), colAt + Vector3.up * 3.2f, 58f, 0.01f);   // wide: the chamber below her, Panther by the archive, the Scree in the doorway
            var y0 = Seven.transform.position.y; bool fadeStarted = false;
            StartCoroutine(StopWorldAudio(5.5f));
            for (float t = 0f; t < 1f; t += Time.deltaTime / 7f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                var p = Seven.transform.position; p.y = y0 + u * 4.2f; Seven.transform.position = p;
                beam.SetFloat("_Intensity", 0.16f + u * 0.6f);
                if (PaleLight != null) { var m = PaleLight.material; if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", 0.32f + u * 0.4f); }
                if (t > 0.45f) foreach (var r in rends) { r.GetPropertyBlock(mpb); mpb.SetFloat("_Fade", Mathf.SmoothStep(0f, 1f, (t - 0.45f) / 0.55f)); r.SetPropertyBlock(mpb); }
                if (!_softShot && t > 0.3f) { _softShot = true; ImageGate.Expect(Seven.transform, "7 rising in the light, the chamber below"); StartCoroutine(Shot("f58_she_rises_into_the_light")); }
                if (!fadeStarted && t > 0.55f) { fadeStarted = true; StartCoroutine(UI.Fade(1f, 3.6f, 1f)); }
                yield return null;
            }
            yield return Wait(0.8f);
            yield return Shot("f59_fade_through_white");
            MusicSystem.Stop(2f);
            Log("FIN_13 (QF30 20): the pale light (plain, no symbols, no figure), 'You can rest now.' (the voice, unnamed), her last touch of his head, she walked into the column of light and rose in it like the saucer's beam lifts her, her silhouette softening, fade through white");
            Complete("FIN_13");
            Checkpoint(L.CpEnding);
        }
        Material AscentColumn(Vector3 at)
        {
            var top = L.ChamberC + Vector3.up * (L.ChamberH + 2f); var dir = (top - at).normalized; float len = (top - at).magnitude;
            var col = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(col.GetComponent<Collider>()); col.name = "AscentLight_Beam";
            col.transform.position = at + dir * len * 0.5f; col.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir); col.transform.localScale = new Vector3(2.4f, len * 0.5f, 2.4f);
            var m = Markers.Additive(0f); col.GetComponent<Renderer>().sharedMaterial = m; col.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var disc = new GameObject("AscentLight_Glow"); disc.transform.position = at + Vector3.up * 0.02f; disc.AddComponent<MeshFilter>().sharedMesh = Markers.DiscMesh(1.3f, 32); disc.AddComponent<MeshRenderer>().sharedMaterial = m;
            return m;
        }
        /// <summary>QF30 (22): on the fade through white every sound of the Moon stops - the Scree's buzz, the chamber hum, the archive, the
        /// machinery, the drills, Tunneler, ambience loops, any 3D sound - except the music (it fades on its own) and the voice line playing.</summary>
        IEnumerator StopWorldAudio(float secs)
        {
            _endingAudio = true;
            var keep = new HashSet<AudioSource>();
            foreach (var m in FindObjectsByType<MusicSystem>(FindObjectsSortMode.None)) foreach (var a in m.GetComponentsInChildren<AudioSource>(true)) keep.Add(a);
            var voice = VoiceSource(); if (voice != null) keep.Add(voice);
            var all = FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Where(a => a != null && !keep.Contains(a) && !a.name.StartsWith("Voice") && !a.name.StartsWith("Reaction")).ToList();   // the dialogue's own voices stay (FIN_14's lines)
            var v0 = all.Select(a => a.volume).ToList();
            for (float t = 0f; t < 1f; t += Time.deltaTime / secs) { for (int i = 0; i < all.Count; i++) if (all[i] != null) all[i].volume = v0[i] * (1f - t); yield return null; }
            int n = 0; foreach (var a in all) if (a != null) { if (a.isPlaying) n++; a.Stop(); a.enabled = false; }
            // and the things that make them go quiet for good
            if (Rig != null) Rig.Silence();
            foreach (var w in new[] { Worker, Elder }) if (w != null) w.gameObject.SetActive(false);
            if (Workers != null) foreach (var w in Workers) if (w != null) w.gameObject.SetActive(false);
            if (Crew != null) foreach (var w in Crew) if (w != null) w.gameObject.SetActive(false);
            if (TunnelerCraft != null) TunnelerCraft.gameObject.SetActive(false);
            StopWorldDrillsNow();
            if (_barrierHum != null) _barrierHum.Stop();
            Log($"FIN_13 -> FIN_14 (QF30 22): every sound of the Moon faded out over the white and stopped ({all.Count} sources, {n} were playing); the Scree, Tunneler and the machinery switched off");
        }
    }
}
