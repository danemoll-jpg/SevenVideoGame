using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using L = Seven.CattalusLayout;

namespace Seven
{
    /// <summary>Round 18 — Cattalus: the flow, CAT_02-CAT_06 (the race and CAT_08-CAT_11: CattalusDirectorEnd).</summary>
    public partial class CattalusDirector
    {
        IEnumerator Flow(int cp)
        {
            Restore(cp);
            yield return null; yield return null;
            if (DemoBoot.QF28Holds) { yield return HoldCheck(); yield break; }       // QF28 (5)
            StartCoroutine(Vignettes());
            if (cp <= L.CpCity) { BikeHoldWatch.Begin("cattalus", Hold, Bike, Saucer); yield return Arrive(); yield return City(); Checkpoint(L.CpScreen); }
            else yield return UI.Fade(0f, 1.2f);
            if (CattalusState.Checkpoint <= L.CpScreen) { yield return Catatron(); Checkpoint(L.CpRace); }
            if (CattalusState.Checkpoint <= L.CpRace) { yield return BeforeTheStart(); yield return TheRace(); Checkpoint(L.CpSphere); }
            if (CattalusState.Checkpoint <= L.CpSphere) { yield return FinalMemory(); Checkpoint(L.CpMeadow); }
            if (CattalusState.Checkpoint <= L.CpMeadow) { yield return TheField(); }
            yield return TravelSize();
        }

        /// <summary>Put everything where the checkpoint says (reload, Continue, Scene Select).</summary>
        void Restore(int cp)
        {
            foreach (var z in L.Zones) SetZone(z.Name, z.On);
            SetZone("GateZ", cp > L.CpCity || CattalusState.StepDone("gate"));
            if (GateLeaf != null) GateLeaf.localRotation = Quaternion.Euler(0f, cp > L.CpCity || CattalusState.StepDone("gate") ? 100f : 0f, 0f);
            bool raced = cp > L.CpRace, won = CattalusState.RaceResult == CattalusState.Result.Won;
            // the prizes: on the display before the race; after it the retreat is Duchess's (she is in it) and the trophy hers
            if (Trophy != null) { Trophy.gameObject.SetActive(cp <= L.CpSphere); if (raced) Trophy.position = L.TrophyLedge; }
            if (Sphere != null) Sphere.gameObject.SetActive(cp <= L.CpSphere);
            if (Retreat != null) Retreat.position = raced ? L.PodiumAt + new Vector3(3.6f, 0f, 0.6f) : L.RetreatAt;
            if (Duchess != null) { Duchess.gameObject.SetActive(true); if (raced) { Duchess.Place(Retreat != null ? Retreat.position + Vector3.up * 0.3f : L.PodiumAt, Vector3.back); Duchess.Play("sleep"); } else { Duchess.Place(L.DuchessWait, Vector3.forward); Duchess.Play("sit"); } }
            if (DuchessRacer != null) { DuchessRacer.gameObject.SetActive(true); DuchessRacer.position = CattalusCourse.StartAt + new Vector3(3.5f, 0.65f, 8f); DuchessRacer.rotation = Quaternion.LookRotation(Vector3.back); }
            if (Steward != null) { Steward.Place(L.StewardAt, Vector3.back); Steward.Play("sit"); }
            if (Juniper != null) { Juniper.gameObject.SetActive(cp <= L.CpCity); Juniper.Place(L.JuniperMeet + new Vector3(-6f, 0f, 10f), Vector3.back); Juniper.Play("idle"); }
            if (Screen != null) Screen.Show(CatatronScreen.Mode.Ads);          // QF41: the commercials, before and after CAT_04
            // 7, Panther and the bike
            Vector3 at; Vector3 face = Vector3.forward;
            switch (cp)
            {
                case L.CpCity: at = L.SaucerAt + new Vector3(0f, 0f, 2f); break;
                case L.CpScreen: at = new Vector3(0f, 0f, 108f); break;
                case L.CpRace: at = L.DeskView + new Vector3(2f, 0f, -2f); break;
                case L.CpSphere: at = L.PodiumAt + new Vector3(-2f, 0f, -3f); break;
                default: at = L.QuietCorner + new Vector3(0f, 0f, 6f); face = Vector3.left; break;
            }
            if (cp <= L.CpCity) { Bike.gameObject.SetActive(false); Seven.gameObject.SetActive(false); if (Cat != null) Cat.gameObject.SetActive(false); }
            else
            {
                Seven.gameObject.SetActive(true); Seven.Place(at, face);
                Bike.gameObject.SetActive(true); Bike.Place(cp <= L.CpRace ? L.BikeCheck : L.BikeCheck + new Vector3(0f, 0f, -3f), 180f);
                if (Cat != null) { Cat.gameObject.SetActive(true); Cat.Place(at - face * 1.2f + Vector3.right * 0.6f, face); Cat.Follow(); }
            }
            MusicSystem.Play(cp <= L.CpCity ? null : cp >= L.CpMeadow ? "cattalus_meadow" : "cattalus_city", 2.5f);
            FollowCamera(); Cam.SnapBehind();
            Log($"restore: {CattalusState.Summary()} -> at {at}");
        }

        // ------------------------------------------------------------------------------------------ CAT_02: a door problem
        IEnumerator Arrive()
        {
            yield return Cutscene(true);
            MusicSystem.Play("cattalus_city", 3f);
            // the saucer is secured at the public berth: the bike lowers out of the bike hold, 7 and Panther come down the beam
            Shoot(new Vector3(18f, 8f, -18f), new Vector3(0f, 2.5f, 6f), 54f, 0.01f);
            StartCoroutine(UI.Fade(0f, 1.6f));
            yield return Wait(1.4f);
            if (Saucer != null) ImageGate.Expect(Saucer, "the saucer at the public berth");   // QF26 (2)
            yield return Shot("c01_the_public_berth");
            yield return HoldBikeOut(L.BikePad, 90f);
            Shoot(L.SaucerAt + new Vector3(9f, 2.4f, -9f), L.SaucerAt + new Vector3(0f, 2.2f, 0f), 46f, 0.01f);
            if (SaucerBeam != null) { yield return SaucerBeam.Iris(true); yield return SaucerBeam.BeamOn(true); }
            Seven.gameObject.SetActive(true); Seven.Lock(true); Seven.BeginFloat();
            if (Cat != null) { Cat.gameObject.SetActive(true); Cat.Place(L.SaucerAt + new Vector3(0.8f, 1.4f, 0.3f), Vector3.forward); Cat.Floating(); }
            bool s3 = false;
            if (SaucerBeam != null) yield return SaucerBeam.Lower(Seven.transform, 3.4f, Vector3.forward, t => { if (Cat != null) Cat.transform.position = Seven.transform.position + new Vector3(0.8f, -0.2f + 0.4f * (1f - t), 0.3f); if (!s3 && t > 0.5f) { s3 = true; StartCoroutine(Shot("c02_7_and_panther_down_the_beam_the_bike_on_its_pad")); } });
            Seven.EndFloat(); Seven.Place(L.SaucerAt + new Vector3(0f, 0f, 2f), Vector3.forward); Seven.Lock(true);
            if (Cat != null) { Cat.Place(L.SaucerAt + new Vector3(1.1f, 0f, 2.4f), Vector3.forward); Cat.Follow(); }
            if (SaucerBeam != null) StartCoroutine(SaucerBeam.Open(false));
            // the public gate: a cat waits for the attendant to open it
            yield return Cutscene(false);
            Objective(null);
            yield return GuideWalk(new[] { L.GateAt + new Vector3(-1.6f, 0f, -6f) }, "to the public gate");
            yield return Cutscene(true);
            Seven.Place(L.GateAt + new Vector3(-2.2f, 0f, -5.4f), Vector3.forward); Seven.Lock(true);
            if (Cat != null) { Cat.Place(L.GateAt + new Vector3(-3.0f, 0f, -5.0f), Vector3.forward); Cat.Hold(L.GateCatAt, "sit"); }
            Shoot(L.GateAt + new Vector3(5.2f, 1.5f, -4.6f), L.GateAt + new Vector3(-0.6f, 0.6f, -2.2f), 44f, 0.01f);
            yield return Wait(0.8f);
            yield return Shot("c03_a_cat_waits_at_the_public_gate");
            if (Attendant != null) { Attendant.Play("stretch"); yield return Wait(0.8f); }
            AudioKit.Play("cat_gate_open", L.GateAt + Vector3.up, 0.9f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.1f) { if (GateLeaf != null) GateLeaf.localRotation = Quaternion.Euler(0f, Mathf.SmoothStep(0f, 100f, t), 0f); yield return null; }
            // the cat looks through, considers, and sits down
            if (GateCat != null) { GateCat.LookTarget = null; GateCat.Pose.LookAt = L.GateAt + new Vector3(0f, 0.4f, 6f); yield return Wait(1.0f); GateCat.Play("sit"); }
            Shoot(L.GateAt + new Vector3(2.4f, 0.7f, -3.2f), L.GateCatAt + Vector3.up * 0.3f, 34f, 0.6f);
            yield return Wait(1.2f);
            yield return Shot("c04_the_gate_open_he_sits_down");
            TwoShot((P7 + (Cat != null ? Cat.transform.position : P7)) * 0.5f, Vector3.right, 2.2f, 0.7f, 0.7f, 40f, 0.4f);
            yield return Say("CAT_02", 1, 5);                     // Is he going in? / He wanted it open. / Oh. Not to go through. / That's a separate decision. / Right. I'll try to keep up.
            SetZone("GateZ", true); CattalusState.DoStep("gate");
            yield return SayIds("CAT_02_P1");                    // The square with the big screen is that way. Everyone ends up there eventually.
            if (Cat != null) Cat.Follow();
            Complete("CAT_02");
            yield return Cutscene(false);
            FreeBike = false;
        }

        // ------------------------------------------------------------------------------------------ the city, CAT_03: Juniper
        IEnumerator City()
        {
            // free exploration toward the square (the seven vignettes along the avenue loop on their own; no prompts, no text)
            Objective("Walk into the city with Panther - the big screen's square is ahead");
            bool box = false;
            StartCoroutine(BoxInspection(() => box = true));
            yield return GuideWalk(new[] { new Vector3(0f, 0f, 44f), L.JuniperMeet + new Vector3(0f, 0f, -5f) }, "along the avenue", 3.5f);
            // CAT_03: Juniper recognizes Panther (fires at or past the meeting point)
            yield return Cutscene(true);
            Seven.Place(L.JuniperMeet + new Vector3(0f, 0f, -4.2f), Vector3.forward); Seven.Lock(true);
            if (Cat != null) { Cat.Place(L.JuniperMeet + new Vector3(0.9f, 0f, -3.8f), Vector3.forward); Cat.Hold(L.JuniperMeet + Vector3.forward * 3f, "sit"); }
            // a cut (QF23 9): she is already a few steps away, tail raised, walking the last of it on screen
            if (Juniper != null) { Juniper.Place(L.JuniperMeet + new Vector3(1.2f, 0f, 1.4f), Vector3.back); Juniper.WalkTo(L.JuniperMeet + new Vector3(0.9f, 0f, -2.6f), 0.45f, "sit"); }
            Shoot(L.JuniperMeet + new Vector3(3.4f, 0.8f, -5.2f), L.JuniperMeet + new Vector3(0.6f, 0.35f, -2.4f), 40f, 0.01f);
            Log("WALK CUT (QF23 9): CAT_03 - Juniper already a few steps away (about 1.5 s of walking on screen)");
            yield return Wait(1.6f);
            yield return Shot("c05_juniper_approaches_tail_raised");
            yield return Say("CAT_03", 1, 3);                     // Panther? Well, look who finally came back. / Hello. / And you found her.
            Seven.PlayAnim("crouch_idle", 0.4f);                 // she kneels comfortably
            yield return Wait(0.5f);
            Shoot(L.JuniperMeet + new Vector3(-2.2f, 0.6f, -1.6f), L.JuniperMeet + new Vector3(0.5f, 0.5f, -3.2f), 40f, 0.4f);
            yield return Say("CAT_03", 4, 10);                    // You knew about me? / Only that he wouldn't leave the assignment alone... / Ahem. / What? You did. / You volunteered? / There was an opening. / Mm-hmm.
            yield return Shot("c06_kneeling_with_juniper_and_panther");
            // the acquaintance welcomes them and moves on (she walks out of frame; the camera holds on the two of them)
            if (Juniper != null) Juniper.WalkTo(L.JuniperMeet + new Vector3(18f, 0f, 6f), 0.7f);
            // the low, quiet two-shot: street activity recedes (the ambience drops, a shallow focus)
            if (_amb != null) StartCoroutine(AudioKit.Fade(_amb, 0.12f, 1.5f));
            Seven.Place(L.QuietStep, Vector3.right); SevenRestPose.On(Seven.gameObject, SevenRestPose.Kind.SitFloor); Seven.Lock(true);
            if (Cat != null) { Cat.Place(L.QuietStep + new Vector3(0.7f, 0f, 0.25f), Vector3.left); Cat.Hold(L.QuietStep + Vector3.up * 0.8f, "sit"); }
            var mid = L.QuietStep + new Vector3(0.35f, 0f, 0.1f);
            Shoot(mid + new Vector3(0.6f, 0.55f, -2.1f), mid + Vector3.up * 0.45f, 36f, 0.01f);
            if (Focus != null) Focus.Set(true, 2.1f);
            yield return Wait(0.9f);
            yield return Shot("c07_low_quiet_two_shot");
            yield return Say("CAT_03", 11, 18);                   // You told me you were sent. ... / No.
            var pose = Cat != null ? Cat.GetComponent<CatPoseOverlay>() : null;
            yield return Say("CAT_03", 19, 19);                   // I'm glad you did.
            if (pose != null) { pose.Lean = 0.6f; pose.LookAt = HerHead(); }   // he leans against her hand
            yield return Say("CAT_03", 20, 20);                   // So am I.
            yield return Wait(2.4f);                             // hold the moment
            yield return Shot("c08_he_leans_against_her_hand_hold_the_moment");
            if (pose != null) { pose.Lean = 0f; pose.LookAt = null; }
            if (Focus != null) Focus.Set(false);
            SevenRestPose.On(Seven.gameObject, SevenRestPose.Kind.None);
            if (_amb != null) StartCoroutine(AudioKit.Fade(_amb, 0.35f, 2f));
            if (Juniper != null) Juniper.gameObject.SetActive(false);
            if (Cat != null) Cat.Follow();
            Complete("CAT_03");
            yield return Cutscene(false);
            Objective("Walk on to the square with the big screen");
        }
        public CloseUpFocus Focus;
        Vector3 HerHead() => Seven.Bone(HumanBodyBones.Head) != null ? Seven.Bone(HumanBodyBones.Head).position : P7 + Vector3.up * 1.6f;

        /// <summary>CAT_02 optional: Panther stops beside a box that looks too small (only if she walks past it); he shrinks to fit.</summary>
        IEnumerator BoxInspection(System.Action done)
        {
            if (SmallBox == null || Cat == null || CattalusState.StepDone("box")) yield break;
            while (Flat(P7 - L.SmallBoxAt).magnitude > 4.5f || _cut) { if (CattalusState.Checkpoint > L.CpCity || CattalusState.SceneDone("CAT_03")) yield break; yield return null; }
            yield return Cutscene(true);
            Seven.transform.rotation = Quaternion.LookRotation(Flat(L.SmallBoxAt - P7)); Seven.Lock(true);
            Cat.Place(L.SmallBoxAt + new Vector3(0.9f, 0f, -0.2f), Vector3.left); Cat.Hold(L.SmallBoxAt, "idle");
            Shoot(L.SmallBoxAt + new Vector3(1.8f, 0.7f, -2.2f), L.SmallBoxAt + Vector3.up * 0.2f, 34f, 0.01f);
            yield return Wait(0.6f);
            yield return Shot("c09_panther_beside_a_box_too_small");
            yield return Say("CAT_02", 6, 7);                     // Don't even think about it. / I'm assessing the workmanship.
            var pose = Cat.GetComponent<CatPoseOverlay>();
            if (pose != null) { pose.PawTo = SmallBoxAt(0.05f); pose.PawW = 1f; }   // one paw inside
            yield return Say("CAT_02", 8, 9);                     // With your feet? / Thoroughly. (a second paw)
            // he briefly shrinks to fit and settles in with obvious satisfaction
            var s0 = Cat.transform.localScale.x;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f) { Cat.transform.localScale = Vector3.one * Mathf.Lerp(s0, 0.55f, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            if (pose != null) pose.PawW = 0f;
            Cat.Place(L.SmallBoxAt + Vector3.up * 0.04f, Vector3.left); Cat.Scripted("lie_down");
            AudioKit.Play("cardboard_rustle", L.SmallBoxAt, 0.8f);
            yield return Wait(1.6f);
            yield return Shot("c10_he_shrinks_to_fit_and_settles");
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f) { Cat.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, s0, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            Cat.Place(L.SmallBoxAt + new Vector3(1.0f, 0f, 0f), Vector3.forward); Cat.Follow();
            CattalusState.DoStep("box"); Log("CAT_02 optional: the box inspection (he shrank to fit, settled, resumed the walk)");
            done();
            yield return Cutscene(false);
        }
        Vector3 SmallBoxAt(float up) => L.SmallBoxAt + Vector3.up * up;

        // ------------------------------------------------------------------------------------------ CAT_04 + CAT_05: the Cat-a-tron
        IEnumerator Catatron()
        {
            if (Cat != null) Cat.Follow();
            Objective("Walk on to the square with the big screen");
            yield return GuideWalk(P7.z > 100f ? new[] { L.ScreenView } : P7.z > 66f ? new[] { new Vector3(0f, 0f, 110f), L.ScreenView } : L.WalkToSquare, "to the Cat-a-tron square", 3f);
            yield return Cutscene(true);
            Seven.Place(L.ScreenView, Vector3.forward); Seven.Lock(true);
            if (Cat != null) { Cat.Place(L.ScreenView + new Vector3(1.0f, 0f, 0.3f), Vector3.forward); Cat.Hold(L.ScreenAt, "sit"); }
            // QF41: the screen is running its commercials; as she arrives the ad is cut by a channel change and the RACE AD starts
            // (the announcer, the trophy with the sphere, the mark, the Deluxe Retreat - no race highlights: Dan)
            if (Screen != null) Screen.Show(CatatronScreen.Mode.Ads);
            if (Shots && Screen != null && Screen.Ads != null)
            {
                // check runs: every commercial on the screen, framed close (the screen filling the frame)
                Shoot(L.ScreenView + new Vector3(0.4f, 1.6f, -2f), L.ScreenAt, 17f, 0.01f);
                for (int i = 0; i < Screen.Ads.Length; i++)
                {
                    Screen.PlayAdNow(i); yield return Wait(3.5f);
                    ImageGate.ExpectPoint(L.ScreenAt, "the Cat-a-tron playing a commercial", 0f, "c10_");
                    yield return Shot("c10_ad_" + (Screen.AdNames != null && i < Screen.AdNames.Length ? Screen.AdNames[i] : i.ToString()));
                }
                Log($"CAT-A-TRON ADS (QF41): {Screen.Ads.Length} commercials on the screen, each with its own track ({(Screen.AdAudio != null ? Screen.AdAudio.Length : 0)} mixed tracks); played so far: {string.Join(", ", CatatronScreen.Played)}");
                Screen.Show(CatatronScreen.Mode.Idle); Screen.Show(CatatronScreen.Mode.Ads);   // back to the shuffled rotation
            }
            Shoot(L.ScreenView + new Vector3(-3f, 1.2f, -6f), L.ScreenAt + Vector3.down * 2f, 50f, 0.01f);
            yield return Wait(1.2f);
            ImageGate.ExpectPoint(L.ScreenAt, "a commercial on the Cat-a-tron as she arrives", 0f, "c11a_");
            yield return Shot("c11a_a_commercial_as_she_arrives");
            if (Screen != null)
            {
                var cc = StartCoroutine(Screen.ChannelChange(CatatronScreen.Mode.Announcer));
                yield return Wait(0.18f);
                ImageGate.ExpectPoint(L.ScreenAt, "the channel-change glitch on the Cat-a-tron", 0f, "c11b_");
                yield return Shot("c11b_the_channel_change_glitch");
                yield return cc;
                Log($"CAT-A-TRON (QF41): the running ad was cut by a channel change ({CatatronScreen.Glitches} glitch) -> the race ad");
            }
            AudioKit.Play("cat_screen_jingle", L.ScreenAt, 0.8f);
            yield return Wait(0.8f);
            yield return Shot("c11_the_race_ad_the_announcer");
            yield return DialogueFraming.Over("the Cat-a-tron screen (the announcer)", SayIds("CAT_04_01"));   // The Cattalus Grand Circuit! Registration closes this afternoon!
            if (Screen != null) Screen.Show(CatatronScreen.Mode.Trophy);
            Shoot(L.ScreenView + new Vector3(1.2f, 1.0f, -3f), L.ScreenAt, 34f, 0.4f);
            yield return DialogueFraming.Over("the trophy rotating on the screen", SayIds("CAT_04_02"));      // One champion. One magnificent trophy!
            yield return Shot("c12_the_trophy_rotates_on_the_screen_the_sphere_in_its_cradle");
            // QF28 (2): the advert really shows the sphere in the trophy's ring - measured on the screen's own camera, plus a close look
            if (Shots && Screen != null)
            {
                Log(Screen.SphereReport());
                Shoot(L.ScreenView + new Vector3(0.4f, 1.6f, -2f), L.ScreenAt, 17f, 0.01f); yield return Wait(0.6f);
                ImageGate.ExpectPoint(L.ScreenAt, "the Cat-a-tron's trophy advert", 0f, "c12b_");
                yield return Shot("c12b_the_advert_close_the_sphere_crowns_the_trophy");
                Log(Screen.SphereReport());
            }
            // she stops; Panther steps closer
            HerFaceCloseUp();
            yield return Say("CAT_04", 3, 4);                     // Panther. / I see it.
            if (Cat != null) Cat.GoTo(L.ScreenView + new Vector3(1.2f, 0f, 1.6f), 0.6f, L.ScreenAt);
            if (Screen != null) Screen.Show(CatatronScreen.Mode.Mark);
            Shoot(L.ScreenView + new Vector3(0.6f, 1.3f, -1.6f), L.ScreenAt + Vector3.down * 1.5f, 26f, 0.4f);
            yield return DialogueFraming.Over("the mark on the trophy's setting, on the screen (QF24 7)", Say("CAT_04", 5, 5));   // The setting. That mark…
            if (Screen != null) ImageGate.Expect(Screen.transform, "the Cat-a-tron screen (the mark)");
            yield return Shot("c13_the_mark_on_the_setting");
            TwoShot((P7 + Cat.transform.position) * 0.5f, Vector3.right, 2.4f, 0.9f, 0.8f, 42f, 0.4f);
            yield return Say("CAT_04", 6, 8);                     // The last piece. / In a racing trophy? / Apparently.
            // the runner-up ad: the Deluxe Retreat (the real box on its display, rotating on the screen)
            if (Screen != null) Screen.Show(CatatronScreen.Mode.Retreat);
            Shoot(L.ScreenView + new Vector3(-1.6f, 1.1f, -3.4f), L.ScreenAt, 40f, 0.4f);
            yield return DialogueFraming.Over("the Deluxe Retreat ad on the screen", SayIds("CAT_04_09"));   // And for our runner-up: the Deluxe Retreat!...
            yield return Shot("c14_the_deluxe_retreat_ad");
            // CAT_04_10: Panther's eyes follow the rotating box
            var pose = Cat != null ? Cat.GetComponent<CatPoseOverlay>() : null;
            if (pose != null) pose.LookAt = L.ScreenAt + Vector3.left * 4f;
            var pc = Cat != null ? Cat.transform.position : P7;
            Shoot(pc + new Vector3(0.7f, 0.35f, 0.9f), pc + Vector3.up * 0.25f, 30f, 0.01f);
            for (float t = 0f; t < 2.4f; t += Time.deltaTime) { if (pose != null) pose.LookAt = L.ScreenAt + new Vector3(Mathf.Sin(t * 1.4f) * 6f, -2f, 0f); yield return null; }
            yield return Shot("c15_panthers_eyes_follow_the_rotating_box");
            HerFaceCloseUp();
            yield return Say("CAT_04", 11, 13);                   // Panther. The sphere. / Yes. Of course. / Heh. Come on.
            if (pose != null) pose.LookAt = null;
            yield return SayIds("CAT_04_P1");                    // Registration is at the steward's desk, under the screen.
            if (Screen != null) Screen.Show(CatatronScreen.Mode.Ads);           // QF41: the commercials resume (ducked under every line)
            if (Cat != null) Cat.Follow();
            Complete("CAT_04");
            yield return Cutscene(false);
            // ---- CAT_05: registration (X at the desk)
            Objective("Register for the Grand Circuit at the steward's desk");
            yield return Interact(L.DeskView, "Register", "register at the desk", "The steward's desk is under the big screen, on the left", new[] { L.DeskView });
            yield return Cutscene(true);
            Seven.Place(L.DeskView, Vector3.forward); Seven.Lock(true);
            if (Cat != null) { Cat.Place(L.DeskView + new Vector3(1.1f, 0f, 0.2f), Vector3.forward); Cat.Hold(L.StewardAt, "sit"); }
            if (Steward != null) { Steward.Face(P7); Steward.Play("sit"); }
            // QF26 (2): an explicit two-shot across the desk from the side with a clear line (the 0.18.0 TwoShot ended up facing
            // away from both of them - the gate's "no subject" caught it)
            { var mid = (P7 + L.StewardAt) * 0.5f + Vector3.up * 0.7f; var dd = L.StewardAt - P7; dd.y = 0f; var dn = dd.sqrMagnitude > 0.01f ? dd.normalized : Vector3.forward; var pp = Vector3.Cross(Vector3.up, dn);
              var best = mid + pp * 3.4f + Vector3.up * 0.7f - dn * 0.6f;
              foreach (var sg in new[] { 1f, -1f }) { var c = mid + pp * sg * 3.6f + Vector3.up * 1.1f; /* a side view across the desk: her in front, the steward behind it */ var to = mid - c; if (!CameraOccluders.Ready || !CameraOccluders.AnyRay(c, to.normalized, to.magnitude - 0.6f, CameraOccluders.AllMask, out _)) { best = c; break; } }
              if (Steward != null) { var sp = Steward.transform.position; var back = sp - P7; back.y = 0f; back = back.sqrMagnitude > 0.01f ? back.normalized : Vector3.forward; var sx = Vector3.Cross(Vector3.up, back);
                  best = sp + back * 1.9f + sx * 0.9f + Vector3.up * 1.15f; mid = Vector3.Lerp(sp, P7, 0.55f) + Vector3.up * 0.9f; }   // from behind the steward: him in the foreground, her across the desk
              Shoot(best, mid, 46f, 0.01f); }
            yield return Wait(0.5f);
            if (Steward != null) ImageGate.Expect(Steward.transform, "the steward at his desk");
            yield return Shot("c16_the_stewards_desk");
            yield return Say("CAT_05", 1, 5);                     // The sphere in the trophy... / A very old, very beautiful ball. / It's more than that. / You'll have to speak to its owner after the race. / And until then?
            if (Steward != null) Steward.Play("stretch", 0.3f);   // he turns the registration screen toward her
            yield return Say("CAT_05", 6, 6);                     // We have one place left in the final.
            HerFaceCloseUp(true);
            yield return Say("CAT_05", 7, 7);                     // Well. We did bring transportation.
            TwoShot((P7 + Cat.transform.position) * 0.5f, Vector3.right, 2.2f, 0.8f, 0.7f, 40f, 0.4f);
            yield return Say("CAT_05", 8, 10);                    // Try to keep all of it. / You make one dramatic exit... / I remember it very clearly.
            // the champion inspects the trophy, then the cardboard prize
            if (Duchess != null) { Duchess.Place(L.TrophyAt + new Vector3(-1.4f, 0f, -1.6f), Vector3.right); Duchess.Play("idle"); Duchess.Pose.LookAt = L.TrophyAt + Vector3.up * 1.2f; }
            Shoot(L.TrophyAt + new Vector3(-4.2f, 1.4f, -4.4f), L.TrophyAt + new Vector3(1.6f, 0.8f, 0f), 42f, 0.4f);
            yield return Wait(1.0f);
            yield return Shot("c17_duchess_inspects_the_trophy");
            if (Duchess != null) { Duchess.WalkTo(L.RetreatAt + new Vector3(-0.6f, 0f, -1.6f), 0.45f, "idle"); }
            yield return Wait(1.4f);
            if (Duchess != null) { Duchess.Pose.LookAt = L.RetreatAt + Vector3.up * 0.4f; Duchess.Pose.Lean = 0.7f; }
            Shoot(L.RetreatAt + new Vector3(1.8f, 0.9f, -3.2f), L.RetreatAt + new Vector3(-0.3f, 0.5f, -0.6f), 40f, 0.4f);
            if (Steward != null) Steward.Place(L.RetreatAt + new Vector3(1.6f, 0f, -1.4f), Vector3.left);   // a cut: the steward beside the prizes
            yield return Say("CAT_05", 11, 13);                   // Is the little shelf removable? / Adjustable. / Oh.
            yield return Shot("c18_duchess_peers_into_the_retreat");
            if (Duchess != null) Duchess.Pose.Lean = 0f;
            if (Steward != null) Steward.Place(L.StewardAt, Vector3.back);
            Seven.transform.rotation = Quaternion.LookRotation(Flat(L.StewardAt - P7));
            TwoShot((P7 + L.StewardAt) * 0.5f, Vector3.right, 2.8f, 1.1f, 1.0f, 42f, 0.4f);
            yield return SayIds("CAT_05_P1", "CAT_05_P2");       // The course is open for a practice lap... / Your vehicle will be brought to the start line...
            Complete("CAT_05");
            if (Cat != null) Cat.Follow();
            yield return Cutscene(false);
        }

        // ------------------------------------------------------------------------------------------ CAT_06: before the start
        IEnumerator BeforeTheStart()
        {
            if (Duchess != null) { Duchess.gameObject.SetActive(true); Duchess.Place(L.DuchessWait, Vector3.forward); Duchess.Play("sit"); }
            Bike.gameObject.SetActive(true); if (!Riding) Bike.Place(L.BikeCheck, 180f);
            Objective("Race the Grand Circuit - your bike waits at the start, beside the square");
            yield return GuideWalk(new[] { L.BikeCheck + new Vector3(-1.6f, 0f, 0.4f) }, "to the bike at the start", 2.2f);
            if (!CattalusState.SceneDone("CAT_06"))
            {
                yield return Cutscene(true);
                if (Riding) { if (!Bike.Dismount()) Bike.Dismount(true); }
                Seven.Place(L.BikeCheck + new Vector3(-1.3f, 0f, 0.2f), Vector3.right); Seven.Lock(true);
                // Panther on the low wall beside her
                if (Cat != null) { Cat.Place(L.LowWall + new Vector3(-0.8f, 0f, 0f), Vector3.right); yield return null; Cat.Hop(L.LowWall + Vector3.up * L.LowWallH, 0.6f, 0.5f); yield return Wait(0.7f); }
                Seven.PlayAnim("crouch_idle", 0.4f);                 // she checks the bike
                Shoot(L.BikeCheck + new Vector3(-0.5f, 1.2f, -3.6f), L.BikeCheck + new Vector3(-0.8f, 0.6f, 0.4f), 42f, 0.01f);
                yield return Wait(0.8f);
                yield return Shot("c19_she_checks_the_bike_panther_on_the_low_wall");
                yield return Say("CAT_06", 1, 4);                     // The high turn after the market... / You know the course? / I know the streets. / Going to talk me around it?
                Seven.PlayAnim("idle", 0.4f);
                TwoShot((P7 + L.LowWall) * 0.5f, Vector3.back, 2.4f, 0.9f, 0.8f, 40f, 0.4f);
                yield return Say("CAT_06", 5, 8);                     // If you'll listen. / Mm. No promises. / Comforting. / I'll listen.
                // he touches his nose to her fingers
                var tik = Seven.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
                var nose = Cat != null ? Cat.GetComponent<CatPoseOverlay>()?.Muzzle ?? (L.LowWall + Vector3.up * 0.9f) : L.LowWall + Vector3.up * 0.9f;
                tik.enabled = true; tik.Ease = 3f; tik.RightPos = nose + Vector3.down * 0.02f + Flat(P7 - nose).normalized * 0.05f; tik.RightW = 1f; tik.LookPos = nose; tik.LookW = 0.7f;
                Shoot(nose + new Vector3(0.5f, 0.15f, -0.7f), nose, 30f, 0.01f);
                yield return Wait(1.6f);
                yield return Shot("c20_he_touches_his_nose_to_her_fingers");
                tik.ReleaseAll("CAT_06 nose to fingers");
                Complete("CAT_06");
                // mount before the ride: she gets on and rolls to the grid
                Bike.Mount(); FollowCamera(); Cam.SnapBehind();
                yield return Cutscene(false);
            }
            if (!Riding)
            {
                // mount before the ride: X at the bike (free mounting is off in the city - Panther walks with her)
                Objective("Get on your bike at the start");
                yield return Interact(Bike.transform.position - Bike.transform.right * 1.2f, "Ride", "on the bike at the start", "Your bike waits beside the square, at the start", null, null, 2.6f);
                if (!Riding) Bike.Mount(); FollowCamera(); Cam.SnapBehind();
            }
        }
    }
}
