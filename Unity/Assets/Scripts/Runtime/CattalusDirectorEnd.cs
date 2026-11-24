using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using L = Seven.CattalusLayout;

namespace Seven
{
    /// <summary>Round 18 — Cattalus: CAT_07 (the race), CAT_08W / CAT_08L, CAT_09 (the sphere, vision 7), CAT_10 (the field and its
    /// cinematic), CAT_11 (travel size, the beam home); the vignettes; the bike hold.</summary>
    public partial class CattalusDirector
    {
        // ------------------------------------------------------------------------------------------ CAT_07: the Grand Circuit
        public bool AutoLose => DemoBoot.CatLose;
        public bool WantsPractice { get; private set; }
        /// <summary>The playtest: the step wants her on the bike (the race grid).</summary>
        public bool WantsBike => _objective != null && (_objective.Contains("Race") || _objective.Contains("bike"));
        IEnumerator TheRace()
        {
            if (Race == null) { Log("RACE: no race object - skipped"); yield break; }
            Race.Moment = RaceMoment;
            bool practiced = false;
            while (true)
            {
                // at the grid, on the bike: X = race; A = a practice lap first (offered by the steward), the first time only
                var grid = CattalusCourse.AtDist(-16f);
                if (!practiced) Race.DuchessToGrid();                          // QF28 (4): she waits at the grid in her racer
                CattalusCourse.Around(CattalusCourse.IndexAtDist(-16f));      // the course round the grid is open to ride onto
                Objective("Race the Grand Circuit - ride to the start line");
                bool go = false, practice = false;
                if (Auto) { StartCoroutine(AutoRide(new[] { grid }, 3f)); }
                ShowGuide(grid);
                float t0 = Time.time;
                while (!go)
                {
                    bool at = Riding && Flat(Bike.transform.position - grid).magnitude < 9f;
                    if (!at && !Riding && Flat(P7 - L.BikeCheck).magnitude < 4f && !_cut) { SetPrompt("Ride"); if (Input.Interact.WasPressedThisFrame() || Auto) { Bike.Mount(); FollowCamera(); } }
                    else SetPrompt(at ? "Start the race" : null);
                    if (at && !practiced && Time.time - t0 > 0.5f) UI.Hint("A: a practice lap first (optional)", 0.2f);
                    if (at && (Input.Interact.WasPressedThisFrame() || (Auto && Time.time - t0 > 1.5f))) go = true;
                    if (at && !practiced && Input.Jump.WasPressedThisFrame()) { go = true; practice = true; }
                    if (Time.time - t0 > 600f && Auto) { Cap.Soft++; Log("SOFT-LOCK? the grid"); go = true; }
                    yield return null;
                }
                SetPrompt(null); HideGuide(); _autoRide = false;
                Objective(practice ? "Practice lap - the same gates, nobody to beat" : null);
                _race = true; _practice = practice;
                if (Auto) StartCoroutine(AutoRace());
                if (practice) { CattalusState.CountPractice(); Log("RACE: practice lap (no opponent)"); }
                else { CattalusState.CountRace(); Log($"RACE: the Grand Circuit starts (race {CattalusState.Races})"); }
                yield return Race.Run(practice, id => SayIds(id));
                _race = false;
                Log($"RACE {(practice ? "practice" : "result")}: {Race.Log}");
                yield return Shot(practice ? "c25_practice_lap_finished" : "c26_over_the_line");
                if (practice) { practiced = true; MusicSystem.Play("cattalus_city", 2f); continue; }
                break;
            }
            // the result (kept as it was: a loss is never rewritten)
            bool won = Race.HerTime <= Race.DuchessTime;
            CattalusState.SetResult(won ? CattalusState.Result.Won : CattalusState.Result.RunnerUp, Race.HerTime, Race.DuchessTime);
            Log($"RACE RESULT: {(won ? "WON" : "RUNNER-UP")} - her {CattalusState.Fmt(Race.HerTime)} vs Duchess {CattalusState.Fmt(Race.DuchessTime)}; saved: {CattalusState.ResultText}");
            MusicSystem.Play("cattalus_city", 3f);
            Complete("CAT_07");
            yield return won ? Champion() : RunnerUp();
            Complete("CAT_08");
        }
        bool _race, _practice; int _raceLines;
        IEnumerator AutoRace() { while (_race) { Race.Autopilot(AutoLose && !_practice); yield return null; } Bike.ScriptSteer = 0f; Bike.ScriptThrottle = 0f; }
        /// <summary>Panther's race lines, tied to where she is and whether she leads (telepathic; the race camera stays on her).</summary>
        void RaceMoment(string m)
        {
            if (Race.Practice)
            {
                if (m == "section:0" && CattalusState.Practices <= 1) StartCoroutine(SayIds("CAT_06_01"));      // the tip: the high turn after the market
                return;
            }
            switch (m)
            {
                case "start": StartCoroutine(SayIds("CAT_07_02", "CAT_07_03")); break;          // All right. Here we go. / Smoothly off the line.
                case "section:1": StartCoroutine(SayIds("CAT_07_04", "CAT_07_05", "CAT_07_06")); break;   // Tight turn ahead... / I'm remembering! / Excellent.
                case "section:2": StartCoroutine(SayIds("CAT_07_07", "CAT_07_08")); break;      // Oh, that's a view! / Admire it afterward.
                case "lead": StartCoroutine(SayIds("CAT_07_09", "CAT_07_10", "CAT_07_11")); break;
                case "behind": StartCoroutine(SayIds("CAT_07_12", "CAT_07_13")); break;
                case "duchess_finished": case "finished": if (_raceLines++ == 0) StartCoroutine(SayIds("CAT_07_CALL3")); break;
            }
            if (m.StartsWith("section:") || m == "lead" || m == "behind" || m == "crash") Log($"race moment: {m} at {Race.HerDist:0} m ({(Race.Leading ? "leading" : "behind")}, {CattalusState.Fmt(Race.ClockNow)})");
            DuchessShots(m);                                                   // QF28 (4)
            if (m == "section:3" || m == "section:2" || m == "section:1") StartCoroutine(Shot($"c2{m.Substring(8)}_race_{CattalusCourse.SectionNames[int.Parse(m.Substring(8))].Replace(' ', '_').Replace("-", "_")}"));
        }

        // ------------------------------------------------------------------------------------------ CAT_08W: championship
        IEnumerator Champion()
        {
            yield return Cutscene(true);
            yield return StopBike(1.5f);
            if (Riding) { if (!Bike.Dismount()) Bike.Dismount(true); }
            // she stops beside Panther (at the low wall by the line)
            var at = L.LowWall + new Vector3(-1.3f, 0f, -0.8f);
            Seven.Place(at, Vector3.right); Seven.Lock(true);
            PerchCat(L.LowWall + Vector3.up * L.LowWallH, Vector3.left);
            TwoShot((P7 + L.LowWall) * 0.5f, Vector3.back, 2.4f, 0.9f, 0.9f, 40f, 0.01f);
            AudioKit.Play("cat_crowd_cheer", P7, 0.8f);
            yield return Wait(0.6f);
            yield return Shot("c30w_she_stops_beside_panther_breathless");
            yield return Say("CAT_08W", 1, 3);                    // Ha! Did you see that? / I was supervising most of it. / Supervising?
            var pose = Cat != null ? Cat.GetComponent<CatPoseOverlay>() : null; if (pose != null) { pose.Lean = 0.6f; pose.LookAt = HerHead(); }
            yield return Say("CAT_08W", 4, 4);                    // Expertly. (leans into her hand)
            if (pose != null) { pose.Lean = 0f; pose.LookAt = null; }
            // the presentation: the steward presents the trophy with its sphere; Duchess receives the box and climbs in
            yield return Presentation(true);
            yield return Say("CAT_08W", 5, 5);                    // Oh, this is excellent.
            TwoShot((P7 + (Cat != null ? Cat.transform.position : P7)) * 0.5f, Vector3.right, 2.4f, 0.9f, 0.8f, 40f, 0.4f);
            yield return Say("CAT_08W", 6, 9);                    // I think everyone's happy. / Some of us are still evaluating... / You have a whole saucer. / It came furnished.
            yield return Shot("c34w_champion_the_trophy_hers");
            yield return Cutscene(false);
        }
        // ------------------------------------------------------------------------------------------ CAT_08L: runner-up
        IEnumerator RunnerUp()
        {
            yield return Cutscene(true);
            yield return StopBike(1.5f);
            Seven.Lock(true);
            // she exhales against the fairing, still in the saddle
            var bp = Bike.transform.position; var bf = Flat(Bike.transform.forward).normalized; var br = Vector3.Cross(Vector3.up, bf);
            Shoot(bp + bf * 2.2f + br * 1.3f + Vector3.up * 1.4f, bp + Vector3.up * 1.2f, 38f, 0.01f);
            yield return Wait(0.5f);
            yield return Shot("c30l_she_exhales_against_the_fairing");
            yield return Say("CAT_08L", 1, 1);                    // Oh… I nearly had it.
            if (Riding) { if (!Bike.Dismount()) Bike.Dismount(true); }
            Seven.Place(L.LowWall + new Vector3(-1.3f, 0f, -0.8f), Vector3.right); Seven.Lock(true);
            PerchCat(L.LowWall + Vector3.up * L.LowWallH, Vector3.left);
            TwoShot((P7 + L.LowWall) * 0.5f, Vector3.back, 2.4f, 0.9f, 0.9f, 40f, 0.01f);
            yield return Say("CAT_08L", 2, 6);                    // You did. / Now we have a box. / A very good box. / Panther. / We'll speak to the winner.
            yield return Presentation(false);
            // the champion barely looks at the trophy; her attention stays on 7's prize
            if (Duchess != null) { Duchess.Pose.LookAt = Retreat != null ? Retreat.position + Vector3.up * 0.5f : L.PodiumAt; }
            var mid = (P7 + (Duchess != null ? Duchess.transform.position : P7)) * 0.5f;
            TwoShot(mid, Vector3.right, 3.0f, 1.0f, 0.8f, 42f, 0.4f);
            yield return Say("CAT_08L", 7, 10);                   // Does yours have the little shelf inside? / Um… yes. / And the paper? / That's included.
            if (Duchess != null && Trophy != null) { Duchess.Pose.PawTo = Trophy.position + Vector3.up * 0.2f; Duchess.Pose.PawW = 1f; }   // she nudges the trophy toward her
            yield return Say("CAT_08L", 11, 13);                  // Would you consider an exchange? / The trophy for the box? / I have trophies.
            if (Duchess != null) Duchess.Pose.PawW = 0f;
            yield return Shot("c33l_would_you_consider_an_exchange");
            // CAT_08L_14: 7 glances at Panther; he gives a small approving nod
            Seven.transform.rotation = Quaternion.LookRotation(Flat((Cat != null ? Cat.transform.position : P7 + Vector3.right) - P7));
            var pose = Cat != null ? Cat.GetComponent<CatPoseOverlay>() : null;
            var pc = Cat != null ? Cat.transform.position : P7;
            Shoot(pc + new Vector3(-0.9f, 0.45f, -0.9f), pc + Vector3.up * 0.3f, 32f, 0.01f);
            for (float t = 0f; t < 1.4f; t += Time.deltaTime) { if (pose != null) pose.Lean = Mathf.Sin(Mathf.Clamp01(t / 0.7f) * Mathf.PI) * 0.5f; yield return null; }
            if (pose != null) pose.Lean = 0f;
            yield return Shot("c34l_panther_gives_a_small_approving_nod");
            TwoShot(mid, Vector3.right, 3.0f, 1.0f, 0.8f, 42f, 0.4f);
            yield return Say("CAT_08L", 15, 17);                  // Yes. I think we can arrange that. / Ahem. Are we certain about the paper? / We're certain.
            yield return DuchessClimbsIn();
            Log("CAT_08L: the runner-up result stays recorded; the trophy (and the sphere) by a voluntary trade");
            yield return Cutscene(false);
        }
        /// <summary>The presentation at the podium: the steward presents (won: the trophy to her, the retreat to Duchess; lost: the
        /// trophy to Duchess, the retreat to her - the trade follows).</summary>
        IEnumerator Presentation(bool won)
        {
            if (Steward != null) { Steward.Place(L.PodiumAt + new Vector3(0f, 0f, 1.4f), Vector3.back); Steward.Play("sit"); }
            if (Trophy != null) Trophy.position = L.PodiumAt + new Vector3(won ? -1.6f : 1.6f, 0.9f, 0.2f);
            if (Retreat != null) Retreat.position = L.PodiumAt + new Vector3(won ? 1.8f : -1.8f, 0f, 0.4f);
            Seven.Place(L.PodiumAt + new Vector3(-1.6f, 0f, -1.8f), Vector3.forward); Seven.Lock(true);
            if (Cat != null) { Cat.Place(L.PodiumAt + new Vector3(-2.5f, 0f, -2.0f), Vector3.forward); Cat.Hold(L.PodiumAt, "sit"); }
            if (Duchess != null) { Duchess.Place(L.PodiumAt + new Vector3(1.6f, 0f, -1.6f), Vector3.forward); Duchess.Play("idle"); }
            Log("WALK CUT (QF23 9): CAT_08 - a cut to the podium (everyone already in place)");
            Shoot(L.PodiumAt + new Vector3(0.2f, 1.6f, -6.4f), L.PodiumAt + Vector3.up * 0.9f, 46f, 0.01f);
            AudioKit.Play("cat_crowd_cheer", L.PodiumAt, 0.7f);
            yield return Wait(0.5f);
            yield return Shot(won ? "c31w_the_presentation" : "c31l_the_presentation");
            if (won)
            {
                yield return SayIds("CAT_08W_P1");                  // The Grand Circuit trophy. Congratulations, champion.
                Shoot(L.PodiumAt + new Vector3(-0.6f, 1.4f, -3.0f), Trophy != null ? Trophy.position + Vector3.up * 0.5f : L.PodiumAt, 36f, 0.4f);
                yield return Shot("c32w_the_trophy_with_its_sphere");
                yield return SayIds("CAT_08W_P2");                  // And for our runner-up, the Deluxe Retreat.
                yield return DuchessClimbsIn();
            }
            else yield return SayIds("CAT_08L_P1");                 // Our champion, Duchess! And our runner-up takes home the Deluxe Retreat.
        }
        IEnumerator DuchessClimbsIn()
        {
            if (Duchess == null || Retreat == null) yield break;
            var door = Retreat.position + Retreat.forward * -0.9f;
            Duchess.Place(door - Retreat.forward * -1.2f, Retreat.position - door); Duchess.Pose.LookAt = null;
            // QF26 (2): from the door's side, so she is seen going in (0.18.0's angle had the box between the camera and her)
            { var outw = (door - Retreat.position); outw.y = 0f; outw = outw.sqrMagnitude > 0.01f ? outw.normalized : -Retreat.forward; var sd = Vector3.Cross(Vector3.up, outw);
              Shoot(door + outw * 2.8f + sd * 0.9f + Vector3.up * 1.25f, door + Vector3.up * 0.35f - outw * 0.3f, 42f, 0.01f); }   // outside the entry: her at the threshold, the box behind her
            Duchess.WalkTo(door, 0.35f);
            for (float t = 0f; t < 2.5f && Duchess.Busy; t += Time.deltaTime) yield return null;
            // QF26 (2): the shot is of her going in, at the threshold (0.18.0 took it after she had vanished inside: an empty box;
            // inside, the covered Retreat hides her from every angle)
            ImageGate.Expect(Duchess.transform, "Duchess at the Deluxe Retreat's entry");
            yield return Shot("c35_duchess_climbs_into_the_box");
            yield return Duchess.Hop(Retreat.position + Vector3.up * 0.15f, 0.7f, 0.35f);
            AudioKit.Play("cardboard_rustle", Retreat.position, 0.9f); AudioKit.Play("paper_crinkle", Retreat.position, 0.7f);
            Duchess.Play("sleep", 0.5f);
            if (RetreatPaper != null) StartCoroutine(Crinkle());
            yield return Wait(1.2f);
            Log("CAT_08: Duchess climbs into the Deluxe Retreat (arranges the paper)");
        }
        IEnumerator Crinkle() { var p0 = RetreatPaper.localPosition; for (float t = 0f; t < 2f; t += Time.deltaTime) { RetreatPaper.localPosition = p0 + new Vector3(Mathf.Sin(t * 13f) * 0.01f, 0f, Mathf.Cos(t * 11f) * 0.01f); yield return null; } RetreatPaper.localPosition = p0; }

        // ------------------------------------------------------------------------------------------ CAT_09: the final memory
        IEnumerator FinalMemory()
        {
            yield return Cutscene(true);
            yield return SayIds("CAT_09_P1");                    // Somewhere quieter. Over there, by the planters.
            yield return Cutscene(false);
            Objective("Take the trophy somewhere quiet - by the planters");
            if (Trophy != null) Trophy.gameObject.SetActive(false);   // she carries it (a cut at the corner sets it down)
            yield return GuideWalk(new[] { new Vector3(-6f, 0f, 122f), new Vector3(-23f, 0f, 124.2f), L.QuietCorner + new Vector3(-0.2f, 0f, -0.6f) }, "to the quiet corner", 2f);
            yield return Cutscene(true);
            var ledge = L.TrophyLedge;
            if (Trophy != null) { Trophy.gameObject.SetActive(true); Trophy.position = ledge; Trophy.rotation = Quaternion.LookRotation(Vector3.back); }
            var sp = Sphere != null ? Sphere.position : ledge + Vector3.up * 1.2f;
            Seven.Place(Flat(sp) + new Vector3(-0.05f, 0f, -0.85f), Vector3.forward); Seven.Lock(true);
            if (Cat != null) { Cat.Place(Flat(sp) + new Vector3(-0.9f, 0f, -0.6f), Vector3.forward); Cat.Hold(sp, "sit"); }
            Shoot(sp + new Vector3(-1.9f, 0.25f, 0.35f), sp + Vector3.down * 0.15f, 40f, 0.01f);   // from the side: the sphere in its setting, her in profile, Panther below
            yield return Wait(0.8f);
            yield return Shot("c40_the_sphere_almost_unmarked");
            // the touch (the shared rune touch -> vision -> return): fine lines emerge across it, the cradle releases
            System.Action<float> glow = k => { if (SphereLines != null) Glowing.Set(SphereLines, k); };
            yield return VisionTouch.Into("cattalus", Seven, Cam, UI, sp, RuneSizes.SphereR, glow, true);
            AudioKit.Play("sphere_lines", sp, 0.9f);
            yield return Vision7Play();
            yield return VisionTouch.Back("cattalus", Seven, Cam, sp, RuneSizes.SphereR, glow);
            // the cradle releases: the prongs open, the sphere rests between her hands (the close-up)
            AudioKit.Play("cradle_release", sp, 0.9f);
            if (Prongs != null) for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f) { foreach (var p in Prongs) if (p != null) p.localRotation = Quaternion.Euler(-Mathf.SmoothStep(0f, 22f, t), 0f, 0f); yield return null; }
            var tik = Seven.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
            var f = Flat(Seven.transform.forward).normalized; var r = Vector3.Cross(Vector3.up, f);
            Seven.Place(P7 - f * 0.45f, f); Seven.Lock(true);                         // a step back from the trophy
            var held = P7 + Vector3.up * 1.18f + f * 0.34f;
            // QF28 (5): her hands cup it - both palms under and round its real surface, fingers curled until they touch it (never
            // through it), the wrists natural (HandHold; was: palms pushed at its sides with the clip's wrists and fixed curls)
            tik.enabled = true; tik.Ease = 2.5f; tik.LeftW = 0f; tik.RightW = 0f; tik.LeftPalmW = 0f; tik.RightPalmW = 0f;
            tik.LookPos = held; tik.LookW = 0.8f;
            if (Sphere != null) Sphere.SetParent(null, true);                         // released from the cradle
            var hold = HoldPose.On(Seven.gameObject);
            if (Sphere != null) hold.Hold(Sphere, true, true, "the sphere (both hands)", RuneSizes.SphereR);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f) { if (Sphere != null) Sphere.position = Vector3.Lerp(sp, held, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            yield return Wait(0.5f);
            Shoot(held + r * 0.8f + f * 0.25f + Vector3.up * 0.06f, held, 30f, 0.01f);
            yield return Wait(0.3f);
            if (Sphere != null) ImageGate.Expect(Sphere, "the sphere between her hands", 0f, "c43_");
            yield return Shot("c43_the_sphere_rests_between_her_hands_lines_across_it");
            // the player's view of the hold, close (her hands from in front and a little above)
            Shoot(held + f * 0.75f + Vector3.up * 0.28f - r * 0.2f, held, 34f, 0.01f);
            yield return Wait(0.3f);
            if (Sphere != null) ImageGate.Expect(Sphere, "the sphere cupped in her hands", 0f, "c43b_");
            yield return Shot("c43b_hold_close_her_hands_cup_the_sphere");
            LogHold("CAT_09 the sphere, both hands", hold);
            Log($"CAT_09: the sphere between her hands - {RuneSizes.SphereR * 200f:0} cm across (hand-sized), the lines lit, the cradle released");
            // the return
            HerFaceCloseUp();
            yield return Say("CAT_09", 1, 5);                     // Seven? / They were given this... / We did keep it. / In a trophy. / A very important trophy.
            yield return Wait(1.2f);                             // CAT_09_06: a small laugh, then she studies the sphere again
            yield return Say("CAT_09", 7, 8);                     // There was a mark on the container... / We can compare it with the records...
            // she traces the tree's shape in the air
            // QF28 (5): the sphere rests in her LEFT palm (cupped under it) while the right hand traces the shape
            if (Sphere != null) { var lp = held - r * 0.04f - Vector3.up * 0.02f; for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f) { Sphere.position = Vector3.Lerp(held, lp, Mathf.SmoothStep(0f, 1f, t)); yield return null; } hold.Hold(Sphere, true, false, "the sphere (left palm)", RuneSizes.SphereR); }
            tik.RightPos = P7 + Vector3.up * 1.5f + f * 0.45f + r * 0.2f; tik.RightW = 1f;
            yield return Say("CAT_09", 9, 9);                     // And a tree. Out in a field. Leaning over, with a low branch like this.
            var pose = Cat != null ? Cat.GetComponent<CatPoseOverlay>() : null; if (pose != null) pose.EarToward = P7;
            TwoShot((P7 + (Cat != null ? Cat.transform.position : P7)) * 0.5f, Vector3.right, 2.2f, 0.8f, 0.8f, 40f, 0.3f);
            yield return Say("CAT_09", 10, 12);                   // I know that place. / It's still here? / Come with me.
            if (pose != null) pose.EarToward = null;
            // she puts the sphere safely away (the pocket - as the dodecahedron on the Moon)
            LogHold("CAT_09 the sphere, left palm", hold);
            hold.Release("CAT_09 the sphere put away"); tik.ReleaseAll("CAT_09 the sphere put away");
            if (Sphere != null) Sphere.gameObject.SetActive(false);
            if (Trophy != null) Trophy.gameObject.SetActive(false);
            bool first = CattalusState.AwardRecord();
            Log($"the Cattalus record + its sphere: awarded {(first ? "now (first time)" : "ALREADY - no second award")} (awards {CattalusState.RecordAwards}, must be 1)");
            Complete("CAT_09");
            yield return Cutscene(false);
        }
        /// <summary>QF28 (5): the hold measured now (each hand: the deepest finger point vs the surface, the wrist bend).</summary>
        void LogHold(string what, HoldPose h)
        {
            var an = Seven.GetComponentInChildren<Animator>(); if (an == null || Sphere == null) return;
            var s = new HandHold.Surface(Sphere, RuneSizes.SphereR);
            foreach (var right in new[] { false, true }) { if (right ? !h.Right : !h.Left) continue; var rep = HandHold.Measure(an, right, s); Log($"HOLD (QF28 5) {what}, {(right ? "right" : "left")} hand: deepest finger point {rep.Deepest * 1000f:0.0} mm ({rep.Where}), fingertips touching {rep.Touching} of 5, wrist bend {rep.Wrist:0} deg - {(rep.Deepest >= HandHold.InsideMax && rep.Wrist <= HandHold.WristMax ? "ok" : "FAIL")}"); }
        }
        UnityEngine.Video.VideoPlayer _vp; RenderTexture _vpRT;
        /// <summary>Vision 7: Art/Video/vision_07_cattalus.mp4 (12 s) with the in-engine insert of the dodecahedron's mark on the box
        /// cut in around 4-5 s (a close-up matching the box), a soft shimmer and breeze, no music.</summary>
        IEnumerator Vision7Play()
        {
            AudioKit.Play("vision_whoosh", null, 0.9f);
            var shimmer = AudioKit.Loop("vision_shimmer", null, null, 0f, AudioKit.Bus.Sfx); if (shimmer != null) StartCoroutine(AudioKit.Fade(shimmer, 0.3f, 1.5f));
            var breeze = AudioKit.Loop("cat_meadow_breeze", null, null, 0f, AudioKit.Bus.Ambience); if (breeze != null) StartCoroutine(AudioKit.Fade(breeze, 0.25f, 2f));
            yield return UI.Fade(1f, 0.9f, 1f);
            bool video = false;
            if (Vision7 != null && !DemoBoot.NoVideo)
            {
                var vgo = new GameObject("Vision7Video"); _vp = vgo.AddComponent<UnityEngine.Video.VideoPlayer>();
                _vp.playOnAwake = false; _vp.isLooping = false; _vp.clip = Vision7; _vp.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
                _vpRT = new RenderTexture((int)Vision7.width, (int)Vision7.height, 0); _vp.targetTexture = _vpRT;
                _vp.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None; _vp.timeUpdateMode = UnityEngine.Video.VideoTimeUpdateMode.GameTime;
                _vp.Prepare(); for (float t = 0f; t < 3f && !_vp.isPrepared; t += Time.deltaTime) yield return null;
                video = _vp.isPrepared;
            }
            float aspect = Vision7 != null ? Vision7.width / (float)Mathf.Max(1u, Vision7.height) : 16f / 9f;
            float len = video ? (float)Vision7.length : 10f;
            Texture still = null;
            if (video) { _vp.time = 0; _vp.Play(); for (float t = 0f; t < 0.7f; t += Time.deltaTime) { UI.SetVideo(_vpRT, aspect, t / 0.7f); yield return null; } UI.SetVideo(_vpRT, aspect, 1f); }
            else { still = Resources.Load<Texture2D>("Cattalus/vision_07_still"); if (still != null) for (float t = 0f; t < 0.7f; t += Time.deltaTime) { UI.SetVideo(still, still.width / (float)still.height, t / 0.7f); yield return null; } }
            Log($"vision 7: {(video ? $"video {Vision7.name} ({Vision7.length:0.0}s)" : "VIDEO NOT AVAILABLE - the still")} full-screen; the insert of the mark on the box at ~4.2 s; shimmer + breeze, no music");
            bool shot = false, insert = false;
            for (float vt0 = Time.time; Time.time - vt0 < len - 0.12f && (!video || _vp.isPlaying || Time.time - vt0 < 1f);)
            {
                float e = Time.time - vt0;
                if (!shot && e > 2.5f) { shot = true; yield return Shot(video ? "c41_vision_7_video_frame" : "c41_vision_7_still"); }
                if (!insert && e > 4.2f && VisionInsert != null)
                {
                    insert = true;
                    // the readable insert: the dodecahedron's mark on the box (in-engine, a close-up matching the box), ~1 s
                    if (_vp != null) _vp.Pause();
                    UI.SetVideo(null, aspect, 0f);
                    var cam = Cam.GetComponent<Camera>();
                    var was = (Cam.transform.position, Cam.transform.rotation, cam.fieldOfView);
                    VisionInsert.gameObject.SetActive(true);
                    var face = VisionInsert.Find("Mark") != null ? VisionInsert.Find("Mark") : VisionInsert;
                    Shoot(face.position - face.forward * 1.1f + Vector3.right * 0.35f + Vector3.up * 0.3f, face.position + Vector3.down * 0.08f, 30f, 0.01f);   // QF26 (2): the mark's side (a Quad faces -z; +z looked through the box), the box and its setting round it
                    // QF26 (2): the vision's white fade is still up under the video - lift it for the insert (0.18.0 showed 1.1 s of
                    // pure white here instead of the mark), put it back for the rest of the clip
                    float fadeWas = UI.FadeAlpha; UI.SetFade(0f, 1f);
                    ImageGate.Expect(face, "the dodecahedron's mark on the box");
                    yield return null; yield return Shot("c42_vision_insert_the_dodecahedrons_mark_on_the_box");
                    for (float t = 0f; t < 1.1f; t += Time.deltaTime) yield return null;
                    UI.SetFade(fadeWas, 1f);
                    VisionInsert.gameObject.SetActive(false);
                    Log("vision 7: the in-engine insert - the Moon dodecahedron's mark on the open box (1.1 s)");
                    if (_vp != null) { _vp.Play(); UI.SetVideo(_vpRT, aspect, 1f); } else if (still != null) UI.SetVideo(still, still.width / (float)still.height, 1f);
                }
                if (e > 0.8f && SkipButton()) break;
                yield return null;
            }
            if (shimmer != null) StartCoroutine(AudioKit.Fade(shimmer, 0f, 1.0f, true));
            if (breeze != null) StartCoroutine(AudioKit.Fade(breeze, 0f, 1.5f, true));
            UI.SetVideo(null, aspect, 0f);
            if (_vp != null) { _vp.Stop(); Destroy(_vp.gameObject); }
            yield return UI.Fade(0f, 1.2f, 1f);
        }

        // ------------------------------------------------------------------------------------------ CAT_10: the field
        IEnumerator TheField()
        {
            // follow Panther to the meadow (on foot): he leads, waits when she falls behind
            Objective("Follow Panther");
            if (Cat != null) { Cat.gameObject.SetActive(true); Cat.transform.localScale = Vector3.one; Cat.Lead(new List<Vector3>(L.WalkToMeadow), 6f); }
            if (Race != null) CattalusCourse.Around(-1);
            yield return GuideWalk(L.WalkToMeadow, "following Panther to the meadow", 3f);
            yield return Cutscene(true);
            bool video = MeadowClips != null && MeadowClips.Length > 0 && MeadowClips[0] != null && !DemoBoot.NoVideo;
            if (!video) MusicSystem.Play("cattalus_meadow", 3f);   // QF27: with the video, the meadow track starts WITH the clip
            if (_amb != null) StartCoroutine(AudioKit.Fade(_amb, 0f, 2f));
            var breeze = AudioKit.Loop("cat_meadow_breeze", null, null, 0f, AudioKit.Bus.Ambience); if (breeze != null) StartCoroutine(AudioKit.Fade(breeze, 0.5f, 2f));
            // the city path opens onto the meadow: the leaning tree, older and larger, the low hills
            Seven.Place(L.TreeView, Flat(L.TreeAt - L.TreeView)); Seven.Lock(true);
            if (Cat != null) { Cat.Place(L.TreeView + new Vector3(0.8f, 0f, -0.9f), Flat(L.TreeAt - L.TreeView)); Cat.Hold(L.TreeAt, "sit"); }
            var toTree = Flat(L.TreeAt - L.TreeView).normalized; var side = Vector3.Cross(Vector3.up, toTree);
            Shoot(L.TreeView - toTree * 3.4f + side * 1.2f + Vector3.up * 1.7f, L.TreeAt + Vector3.up * 6f, 50f, 0.01f);
            yield return Wait(1.6f);
            yield return Shot("c50_the_meadow_the_leaning_tree");
            yield return DialogueFraming.Over("the leaning tree she recognizes (QF24 7)", Say("CAT_10", 1, 1));   // That's it.
            TwoShot((P7 + (Cat != null ? Cat.transform.position : P7)) * 0.5f, side, 2.4f, 0.9f, 0.8f, 42f, 0.4f);
            yield return Say("CAT_10", 2, 3);                     // I used to come here often. / I can see why.
            Complete("CAT_10a");
            // QF27: Dan's edited clip (Art/Video/cattalus_meadow.mp4); the in-engine stand-in only if it is missing / -novideo
            Log($"CAT_10 cinematic: {(video ? "Dan's meadow clip (78.8 s), the lines and sounds timed to it" : "the in-engine stand-in (no clip)")}");
            if (video) yield return MeadowVideo(); else yield return MeadowInEngine();
            if (breeze != null) StartCoroutine(AudioKit.Fade(breeze, 0f, 3f, true));
            if (!video) MusicSystem.Stop(3.5f);                   // (with the video the track has already faded over the clip's last 6 s)
            yield return UI.Fade(1f, 2.2f);
            Complete("CAT_10");
            yield return Cutscene(false);
        }

        // QF27: what is on screen when (Dan's clip, 78.8 s): 0-13 she stands by the leaning tree, small Panther chases a seedhead,
        // 13-17 he bounds off; 18-23 the giant Panther walks in; 24-35 her hand on his face; 36-58 they walk away side by side through
        // the grass; 59-65 he lies down, she sits against him; 66-79 the final hold, her head on his fur, his lowered to hers.
        // Each line on its action; 7's lines where her face is small, turned away or hidden; CAT_10_16-18 over the final hold.
        static readonly (float t, string what)[] MeadowCues =
        {
            (1.5f, "line:CAT_10_04"), (2.2f, "sfx:grass_rustle:0.35"), (4.0f, "line:CAT_10_05"), (5.8f, "line:CAT_10_06"),
            (8.5f, "sfx:paw_1:0.3"), (10.0f, "sfx:grass_rustle:0.4"), (13.4f, "sfx:grass_run:0.55"), (16.5f, "shot:small_panther_bounds_off"),
            (19.2f, "sfx:land_hard:0.28"), (20.0f, "line:CAT_10_07"), (21.3f, "sfx:land_hard:0.3"), (22.6f, "line:CAT_10_08"),
            (24.4f, "sfx:giant_purr:0.45"), (25.4f, "line:CAT_10_09"), (27.4f, "line:CAT_10_10"), (30.0f, "line:CAT_10_11"),
            (32.4f, "line:CAT_10_12"), (34.6f, "react:seven:laugh_soft"), (35.6f, "react:cat:chuckle"),
            (38.0f, "sfx:grass_run:0.7"), (41.0f, "shot:side_by_side_through_the_grass"), (44.0f, "line:CAT_10_13"), (46.0f, "sfx:grass_run:0.6"),
            (52.0f, "sfx:grass_run:0.5"), (57.2f, "react:seven:sigh"), (60.5f, "sfx:grass_rustle:0.5"), (62.0f, "sfx:land_soft:0.4"),
            (63.0f, "purr"), (64.5f, "react:seven:sigh"), (66.0f, "line:CAT_10_14"), (68.5f, "line:CAT_10_15"),
            (71.0f, "line:CAT_10_16"), (73.8f, "line:CAT_10_17"), (75.8f, "line:CAT_10_18"),
        };
        /// <summary>QF27: the meadow as Dan's one clip; the meadow track from its first frame, faded out over its last 6 s.</summary>
        IEnumerator MeadowVideo()
        {
            var clip = MeadowClips[0];
            yield return UI.Fade(1f, 0.8f);
            DialogueFraming.VoiceOver = "the meadow cinematic (video)";
            Reactions.Suppress = true;       // the laughter / breathing are placed on the picture
            var go = new GameObject("MeadowVideo"); var vp = go.AddComponent<UnityEngine.Video.VideoPlayer>();
            vp.playOnAwake = false; vp.clip = clip; vp.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture; vp.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None; vp.timeUpdateMode = UnityEngine.Video.VideoTimeUpdateMode.GameTime;
            var rt = new RenderTexture((int)clip.width, (int)clip.height, 0); vp.targetTexture = rt;
            vp.Prepare(); for (float w = 0f; w < 4f && !vp.isPrepared; w += Time.deltaTime) yield return null;
            float len = (float)clip.length;
            MusicSystem.Play("cattalus_meadow", 0.4f);                         // from the start of the clip
            vp.Play(); UI.SetVideo(rt, clip.width / (float)clip.height, 1f);
            StartCoroutine(UI.Fade(0f, 1.0f));
            AudioSource purr = null; bool fading = false; int ci = 0; float t0 = Time.time, drift = 0f; int lines = 0;
            Log($"MEADOW VIDEO (QF27): {len:0.0} s, {MeadowCues.Length} cues, the meadow track from the first frame");
            while (true)
            {
                float t = vp.isPlaying ? (float)vp.time : Time.time - t0;
                while (ci < MeadowCues.Length && t >= MeadowCues[ci].t)
                {
                    var (ct, what) = MeadowCues[ci++]; var part = what.Split(':');
                    if (part[0] == "line") { lines++; StartCoroutine(SayIds(part[1])); StartCoroutine(MeadowShot($"c5_meadow_video_frame_{part[1]}", 0.6f)); drift = Mathf.Max(drift, t - ct); Log($"meadow cue {ct:0.0} s: {part[1]} at {t:0.00} s of the clip"); }
                    else if (part[0] == "sfx") AudioKit.Play(part[1], null, float.Parse(part[2], System.Globalization.CultureInfo.InvariantCulture), 0.03f, AudioKit.Bus.Sfx);
                    else if (part[0] == "react") MeadowReaction(part[1], part[2]);
                    else if (part[0] == "purr") { purr = Purr.Loop(Seven.transform, 0.8f); }
                    else if (part[0] == "shot") StartCoroutine(MeadowShot($"c5_meadow_video_frame_{part[1]}", 0f));
                }
                if (!fading && t >= len - 6f) { fading = true; MusicSystem.Stop(6f); Log($"meadow track fading out over the clip's last 6 s (from {t:0.0} s)"); }
                if (t >= len - 0.05f || (!vp.isPlaying && Time.time - t0 > len + 1f)) break;
                yield return null;
            }
            Log($"MEADOW VIDEO (QF27): {lines} lines placed on the picture (latest start {drift * 1000f:0} ms after its cue), the music faded over the last 6 s");
            while (Dlg.Busy) yield return null;
            if (purr != null) StartCoroutine(AudioKit.Fade(purr, 0f, 2f, true));
            Reactions.Suppress = false;
            DialogueFraming.VoiceOver = null;
            vp.Stop(); Destroy(go); rt.Release();
            UI.SetVideo(null, 1f, 0f);
        }
        IEnumerator MeadowShot(string name, float after) { for (float w = 0f; w < after; w += Time.deltaTime) yield return null; yield return Shot(name); }
        void MeadowReaction(string who, string kind)
        {
            var c = Reactions.Pick(who, kind, out _); if (c == null) return;
            var go = new GameObject("MeadowReaction_" + who + "_" + kind); var a = go.AddComponent<AudioSource>(); a.spatialBlend = 0f; a.clip = c; a.volume = 0.8f * AudioKit.Gain(AudioKit.Bus.Voice); a.Play();
            Destroy(go, c.length + 0.2f); Log($"meadow reaction: {who} {kind}");
        }

        /// <summary>The TEMPORARY in-engine version of the six segments (until Dan's clips are in): giant Panther = his model scaled
        /// up so his shoulder stands above her head; the transformation hidden behind the grassy rise.</summary>
        IEnumerator MeadowInEngine()
        {
            var cat = Cat; if (cat == null) yield break;
            var pose = cat.GetComponent<CatPoseOverlay>();
            var tree = L.TreeAt; var walkIn = L.TreeView + new Vector3(-6f, 0f, 2f);
            var rise = L.TreeView + new Vector3(-30f, 0f, 14f);                                      // the grassy rise
            float giant = 7.2f;                                                                    // shoulder 0.25 m x 7.2 = 1.8 m (above her head)
            // ---- 1: room to run (wide); a seedhead drifts past his nose, his eyes follow it
            Seven.Place(walkIn, Flat(rise - walkIn)); cat.Place(walkIn + new Vector3(0.7f, 0f, -0.8f), Flat(rise - walkIn)); cat.transform.localScale = Vector3.one;
            var seed = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(seed.GetComponent<Collider>()); seed.transform.localScale = Vector3.one * 0.04f; seed.GetComponent<Renderer>().sharedMaterial = Markers.Additive(1.5f);
            StartCoroutine(WalkSeven(walkIn + Flat(rise - walkIn).normalized * 5f, 8f));
            cat.GoTo(walkIn + Flat(rise - walkIn).normalized * 5f + new Vector3(0.8f, 0f, -0.8f), 0.9f, rise);
            Shoot(walkIn + new Vector3(4f, 2.6f, -9f), walkIn + Flat(rise - walkIn).normalized * 3f + Vector3.up * 0.8f, 46f, 0.01f);
            for (float t = 0f; t < 3f; t += Time.deltaTime) { var np = cat.transform.position + Vector3.up * 0.32f + cat.transform.forward * 0.25f; seed.transform.position = np + new Vector3(Mathf.Sin(t * 1.3f) * 0.4f, 0.15f + Mathf.Sin(t * 2.1f) * 0.08f, 0.2f); if (pose != null) pose.LookAt = seed.transform.position; yield return null; }
            yield return Shot("c51_meadow_seg1_room_to_run_a_seedhead_past_his_nose");
            yield return Say("CAT_10", 4, 6);                     // Go on. Nobody's watching. / You are. / I'll be discreet.
            // he springs after it; she jogs a few steps alongside
            Destroy(seed); if (pose != null) pose.LookAt = null;
            Seven.ScriptRun = true; StartCoroutine(WalkSeven(rise + new Vector3(4f, 0f, -3f), 6f));
            cat.TrotSpeed = 3.2f; cat.GoTo(rise, 3.2f, rise + Vector3.forward);
            Shoot(walkIn + new Vector3(-2f, 2f, -10f), (walkIn + rise) * 0.5f, 48f, 0.8f);
            for (float t = 0f; t < 3.4f; t += Time.deltaTime) yield return null;
            Seven.ScriptRun = false;
            // ---- 2: his greatest form (wide enough to show their scale together)
            cat.gameObject.SetActive(false);                                                       // he is behind the rise
            var herAt = rise + new Vector3(6f, 0f, -5f); Seven.Place(herAt, Flat(rise - herAt)); Seven.Lock(true);
            Shoot(herAt + new Vector3(4f, 2.2f, -12f), (herAt + rise) * 0.5f + Vector3.up * 1.5f, 48f, 0.01f);
            yield return Wait(1.0f);
            cat.transform.localScale = Vector3.one * giant; cat.Place(rise + new Vector3(-3f, 0f, 3f), Flat(herAt - rise)); cat.gameObject.SetActive(true); cat.StrollSpeed = 1.3f;
            AudioKit.Play("giant_purr", rise, 0.5f);
            cat.GoTo(herAt + Flat(rise - herAt).normalized * 3.2f, 1.3f, HerHead());
            for (float t = 0f; t < 2.8f; t += Time.deltaTime) yield return null;
            yield return Shot("c52_meadow_seg2_his_greatest_form_his_shoulder_above_her_head");
            Log($"CAT_10 seg 2: giant Panther x{giant:0.0} - shoulder {0.25f * giant:0.00} m vs her head {HerHead().y - P7.y:0.00} m");
            yield return Say("CAT_10", 7, 8);                     // Oh! / You have been holding out on me.
            TwoShot((P7 + cat.transform.position) * 0.5f, Vector3.Cross(Vector3.up, Flat(cat.transform.position - P7).normalized), 7.5f, 1.4f, 1.6f, 46f, 0.5f);
            yield return Say("CAT_10", 9, 12);                    // There's more room here. / I'm still not getting you a bigger bowl. / We could discuss a trough. / Ha! No.
            // ---- 3: an invitation - he lowers his head, her palm on his cheek, both hands; he steps back into a playful crouch
            if (pose != null) { pose.Eat = 0.6f; pose.EatGain = 0.5f; }
            var tik = Seven.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
            yield return Wait(0.8f);
            var cheek = pose != null ? pose.Eyes + Vector3.down * 0.25f : cat.transform.position + Vector3.up * 1.4f;
            var toCat = Flat(cheek - P7).normalized; var cside = Vector3.Cross(Vector3.up, toCat);
            tik.enabled = true; tik.Ease = 2.4f; tik.RightPos = cheek - toCat * 0.18f + cside * 0.12f; tik.RightW = 1f; tik.RightPalmN = toCat; tik.RightPalmW = 1f; tik.LookPos = cheek; tik.LookW = 0.8f;
            var fpm = FurPalm.On(Seven.gameObject); fpm.Set(true, cat.transform, "CAT_10 her palm on his cheek", 1f);   // P2 (8): on his fur, not 18 cm short of it
            Shoot(P7 + cside * 2.2f + toCat * 0.4f + Vector3.up * 1.5f, (HerHead() + cheek) * 0.5f, 40f, 0.01f);
            yield return Wait(1.2f);
            yield return Shot("c53_meadow_seg3_her_palm_against_his_cheek");
            fpm.Set(false, cat.transform, "CAT_10 both palms on his cheeks", 1f);
            tik.LeftPos = cheek - toCat * 0.18f - cside * 0.12f; tik.LeftW = 1f; tik.LeftPalmN = toCat; tik.LeftPalmW = 1f;
            for (float t = 0f; t < 1.4f; t += Time.deltaTime) { tik.RightPos = cheek - toCat * 0.18f + cside * (0.12f + Mathf.Sin(t * 6f) * 0.05f); yield return null; }
            tik.ReleaseAll("CAT_10 seg 3"); fpm.Off();
            if (pose != null) pose.Eat = 0f;
            cat.Scripted("stretch");                                                                // a playful crouch
            yield return Say("CAT_10", 13, 13);                   // Oh, really?
            // ---- 4: running together (broad side-on), laughter, heavy soft footfalls, grass
            AudioKit.Play("grass_run", P7, 0.9f);
            var runTo = tree + new Vector3(10f, 0f, -12f);
            Seven.Lock(false); Seven.ScriptRun = true; StartCoroutine(WalkSeven(runTo, 9f));
            cat.TrotSpeed = 5.2f; cat.Follow();
            for (float t = 0f; t < 6.5f; t += Time.deltaTime)
            {
                var mid = (P7 + cat.transform.position) * 0.5f; var dir = Flat(runTo - P7).normalized; var sd = Vector3.Cross(Vector3.up, dir);
                Cam.Shot(mid + sd * 13f + Vector3.up * 2.4f, mid + Vector3.up * 1.2f, 0.6f, false, 46f);
                if (t > 3f && t - Time.deltaTime <= 3f) yield return Shot("c54_meadow_seg4_running_together_side_on");
                yield return null;
            }
            Seven.ScriptRun = false; Seven.Lock(true);
            // ---- 5: slowing down at the tree; he lowers himself onto his belly; she sits beside him, a hand on his shoulder
            var lie = tree + new Vector3(2f, 0f, -9f);
            cat.Place(lie, Vector3.left); cat.Scripted("lie_down");
            StartCoroutine(HoldDown(cat));                                             // he stays down (the clip loops)
            var sitAt = lie + new Vector3(0.2f, 0f, -2.4f);
            Seven.Place(sitAt, Vector3.left); SevenRestPose.On(Seven.gameObject, SevenRestPose.Kind.SitFloor); Seven.Lock(true);
            var purr = Purr.Loop(cat.transform, 0.9f);
            Shoot(sitAt + new Vector3(-1.5f, 1.2f, -5.6f), (sitAt + lie) * 0.5f + Vector3.up * 1.1f, 44f, 0.01f);
            yield return Wait(1.2f);
            yield return Shot("c55_meadow_seg5_he_lies_down_she_sits_beside_him");
            yield return Say("CAT_10", 14, 15);                   // Mm… I needed that. / So did I.
            // ---- 6: the image we remember - low three-quarter view; her head against his fur, a hand on his foreleg, eyes closed
            var cf = Flat(cat.transform.forward).normalized; var cr = Vector3.Cross(Vector3.up, cf); float gs = cat.transform.localScale.x;
            var seat = cat.transform.position + cf * 0.12f * gs - cr * (0.13f * gs + 0.35f);             // against his shoulder, on the side toward the camera
            Seven.Place(seat, -cr); SevenRestPose.On(Seven.gameObject, SevenRestPose.Kind.SitFloor);
            var shoulder = seat + Vector3.up * 0.9f;
            if (pose != null) { pose.Eat = 0.35f; pose.EatGain = 0.6f; pose.LookAt = HerHead(); pose.TailToward = P7 + Vector3.left * 2f; }
            var lowCam = P7 - cr * 3.8f + cf * 1.6f + Vector3.up * 0.55f;
            Shoot(lowCam, (P7 + shoulder) * 0.5f + Vector3.up * 0.4f, 46f, 0.01f);
            yield return Wait(2.0f);
            yield return Shot("c56_meadow_seg6_the_image_we_remember");
            yield return Say("CAT_10", 16, 18);                   // When we're finished, can we come back? / I'd like that. / Good.
            yield return Wait(3.0f);                             // hold the image: only breathing, an ear, the breeze
            if (purr != null) StartCoroutine(AudioKit.Fade(purr, 0f, 3f, true));
            SevenRestPose.On(Seven.gameObject, SevenRestPose.Kind.None);
            if (pose != null) { pose.Eat = 0f; pose.LookAt = null; pose.TailToward = null; }
        }

        // ------------------------------------------------------------------------------------------ CAT_11: travel size; the beam home
        IEnumerator TravelSize()
        {
            // back at the berth (a cut at dusk): Panther has returned to his small form; the bike was brought back
            Checkpoint(L.CpHome);
            yield return Cutscene(true);
            if (Cat != null) { Cat.transform.localScale = Vector3.one; Cat.gameObject.SetActive(true); }
            Bike.gameObject.SetActive(true); if (Riding) Bike.Dismount(true); Bike.Place(L.BikePad, 90f);
            Seven.Place(L.SaucerAt + new Vector3(-2.4f, 0f, 2.2f), Vector3.right); Seven.Lock(true);
            if (Cat != null) { Cat.Place(L.SaucerAt + new Vector3(-1.4f, 0f, 2.4f), Vector3.left); Cat.Hold(P7, "sit"); }
            MusicSystem.Play("cattalus_city", 2f);
            yield return UI.Fade(0f, 1.4f);
            TwoShot((P7 + (Cat != null ? Cat.transform.position : P7)) * 0.5f, Vector3.forward, 2.2f, 0.8f, 0.7f, 42f, 0.01f);
            yield return Wait(0.6f);
            yield return Shot("c60_back_at_the_berth_travel_size");
            yield return Say("CAT_11", 1, 3);                     // Back to travel size? / Your lap has limitations. / Heh. Come on.
            yield return HoldBikeIn();
            if (SaucerBeam != null) { yield return SaucerBeam.Iris(true); yield return SaucerBeam.BeamOn(true); }
            yield return WalkSeven(L.SaucerAt, 6f);
            Seven.Lock(true); Seven.BeginFloat();
            if (Cat != null) { Cat.Place(L.SaucerAt + new Vector3(0.7f, 0f, 0.2f), Vector3.forward); Cat.Floating(); }
            bool s = false;
            Shoot(L.SaucerAt + new Vector3(14f, 4f, 12f), L.SaucerAt + new Vector3(0f, 2.5f, 0f), 50f, 0.01f);
            if (SaucerBeam != null) yield return SaucerBeam.Rise(Seven.transform, 3.8f, 40f, t => { if (Cat != null) Cat.transform.position = Seven.transform.position + new Vector3(0.7f, 0.2f, 0.2f); if (!s && t > 0.45f) { s = true; StartCoroutine(Shot("c61_she_and_panther_board_in_the_beam")); } });
            if (SaucerBeam != null) yield return SaucerBeam.Open(false);
            Complete("CAT_11");
            yield return UI.Fade(1f, 0.6f);
            ChapterDone = true;
            Log("Cattalus (CAT_02-CAT_11 in the Cattalus scene) done - aboard: CAT_12 at the pilot's chair");
            CattalusState.HomeAboard = true; Chapter1State.Aboard = true;
            SceneManager.LoadScene("Saucer");
        }

        // ------------------------------------------------------------------------------------------ the vignettes (CAT_02, ambient)
        /// <summary>The seven city jokes, looping where the player walks past them - no prompts, no text (shown, not captioned).</summary>
        IEnumerator Vignettes()
        {
            var cats = VignetteCats; var props = VignetteProps;
            if (cats == null || cats.Length < 9) yield break;
            CatFolk C(int i) => i < cats.Length ? cats[i] : null; Transform P(int i) => props != null && i < props.Length ? props[i] : null;
            StartCoroutine(Loop(() => MansionBox(C(0)))); StartCoroutine(Loop(() => Trickle(C(1)))); StartCoroutine(Loop(() => Carton(C(2), C(3), P(0))));
            StartCoroutine(Loop(() => SunBench(C(4), C(5), C(6)))); StartCoroutine(Loop(() => Ledge(C(7)))); StartCoroutine(Loop(() => Meeting(C(8), P(1))));
            if (cats.Length > 9) StartCoroutine(Loop(() => Counter(C(9), P(2))));
            // the citizens: the meeting sits; the others stroll the avenue or occupy the sunlit patches
            if (Citizens != null) for (int i = 3; i < Citizens.Length; i++)
            {
                var c = Citizens[i]; if (c == null) continue;
                if (i % 2 == 1 && (i / 2) < L.SunPatches.Length) { c.Place(L.SunPatches[i / 2], Quaternion.Euler(0f, i * 53f, 0f) * Vector3.forward); c.Play(i % 4 == 1 ? "lie_down" : "sleep"); }
                else { float x = (i % 4 == 0 ? -1f : 1f) * (5f + i * 0.6f); c.Place(new Vector3(x, 0f, 34f + i * 4f), Vector3.forward); c.Path(new List<Vector3> { new Vector3(x, 0f, 112f), new Vector3(-x * 0.8f, 0f, 112f), new Vector3(-x * 0.8f, 0f, 34f), new Vector3(x, 0f, 34f) }, 0.5f + (i % 3) * 0.1f, "idle", true); }
            }
            Log("VIGNETTES (CAT_02): 7 ambient loops - the mansion's tiny box, the fountain trickle, the carton, the sunny bench, the narrow ledge, the meeting and the ornament, the counter push");
            yield break;
        }
        IEnumerator Loop(System.Func<IEnumerator> f) { while (true) { yield return f(); yield return null; } }
        /// <summary>The meadow: once he has lowered himself onto his belly, the pose holds (the lie-down clip would stand him up again).</summary>
        IEnumerator HoldDown(ChapterCat cat) { for (float t = 0f; t < 3.6f; t += Time.deltaTime) yield return null; if (cat != null && cat.Anim != null && cat.Current == ChapterCat.Mode.Scripted) cat.Anim.speed = 0f; while (cat != null && cat.Current == ChapterCat.Mode.Scripted && cat.transform.localScale.x > 2f) yield return null; if (cat != null && cat.Anim != null) cat.Anim.speed = 1f; }
        IEnumerator MansionBox(CatFolk c) { if (c == null) yield break; c.Play("sleep"); yield return Wait(14f); c.Play("wash"); yield return Wait(4f); }
        IEnumerator Trickle(CatFolk c) { if (c == null) yield break; c.Pose.Eat = 0.9f; c.Pose.Chewing = true; c.Play("idle"); yield return Wait(6f); c.Pose.Eat = 0f; c.Pose.Chewing = false; c.Pose.LookAt = L.FountainAt + Vector3.up * 2.6f; yield return Wait(2.5f); c.Pose.LookAt = null; }
        IEnumerator Carton(CatFolk courier, CatFolk sitter, Transform carton)
        {
            if (courier == null || sitter == null || carton == null) yield break;
            var home = L.Vignettes[2].at; var a = home + new Vector3(-4f, 0f, -3f);
            carton.gameObject.SetActive(false); courier.Place(a, Vector3.right); sitter.Place(home + new Vector3(3.2f, 0f, 1.2f), Vector3.left); sitter.Play("sit");
            yield return courier.WalkTo(home + new Vector3(-0.8f, 0f, 0f), 0.5f);
            carton.gameObject.SetActive(true); carton.position = home; AudioKit.Play("cardboard_rustle", home, 0.4f, 0.05f, AudioKit.Bus.Sfx, 18f);
            courier.WalkTo(a, 0.5f);
            yield return sitter.WalkTo(home + new Vector3(0.6f, 0f, 0f), 0.6f);
            yield return sitter.Hop(home + Vector3.up * 0.12f, 0.5f, 0.3f); sitter.Play("sit");
            yield return Wait(8f);
            yield return sitter.Hop(home + new Vector3(3.2f, 0f, 1.2f), 0.6f, 0.3f); sitter.Play("sit");
            yield return Wait(2f);
        }
        IEnumerator SunBench(CatFolk a, CatFolk b, CatFolk c)
        {
            if (a == null || b == null || c == null) yield break;
            var spot = L.Vignettes[3].at + Vector3.up * 0.5f;
            a.Place(spot + new Vector3(0f, 0f, -0.18f), Vector3.right); a.Play("lie_down"); b.Place(spot + new Vector3(0.1f, 0f, 0.2f), Vector3.left); b.Play("sleep"); c.Place(spot + new Vector3(-0.2f, 0f, 0.05f), Vector3.forward); c.Play("sit");
            yield return Wait(12f); c.Play("wash"); yield return Wait(3f);
        }
        IEnumerator Ledge(CatFolk c)
        {
            if (c == null) yield break;
            var a = L.Vignettes[4].at + new Vector3(0f, 0.9f, -8f); var b = L.Vignettes[4].at + new Vector3(0f, 0.9f, 8f);
            c.Place(a, Vector3.forward); yield return c.Path(new List<Vector3> { b }, 0.45f, "idle"); yield return Wait(1f);
            c.Place(b, Vector3.back); yield return c.Path(new List<Vector3> { a }, 0.45f, "idle"); yield return Wait(1f);
        }
        IEnumerator Meeting(CatFolk chair, Transform ornament)
        {
            if (chair == null) yield break;
            chair.Play("sit"); chair.Pose.LookAt = null; yield return Wait(6f);
            for (float t = 0f; t < 4f; t += Time.deltaTime) { if (ornament != null) { ornament.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 3f) * 25f); chair.Pose.LookAt = ornament.position + ornament.up * -1.2f; } yield return null; }
            chair.Pose.LookAt = null; yield return Wait(2f);
        }
        IEnumerator Counter(CatFolk c, Transform obj)
        {
            if (c == null || obj == null) yield break;
            var home = obj.localPosition; c.Play("sit"); c.Pose.LookAt = obj.position + Vector3.right * 3f;   // pretending not to watch it
            for (float t = 0f; t < 6f; t += Time.deltaTime) { c.Pose.PawTo = obj.position; c.Pose.PawW = 0.8f; obj.localPosition = home + Vector3.right * Mathf.Min(0.35f, t * 0.06f); yield return null; }
            c.Pose.PawW = 0f; yield return Wait(3f); obj.localPosition = home;
        }

        // ------------------------------------------------------------------------------------------ the bike hold (as the mothership)
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
            if (Saucer != null) ImageGate.Expect(Saucer, "the saucer's open bike hold");   // QF26 (2)
            yield return Shot("c01b_the_bike_hold_open");
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
            Log("the bike came out of the bike hold onto its pad; the hold closed seamlessly (7 and Panther come down in the beam)");
        }
        IEnumerator CloseHoldLater(float s) { yield return Wait(s); yield return Hold.Close(); }
        void ShowBike(bool on)
        {
            foreach (var r in Bike.GetComponentsInChildren<Renderer>(true)) r.enabled = on;
            foreach (var l in Bike.GetComponentsInChildren<Light>(true)) l.enabled = on;
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
            Log("going home the bike rose back into the bike hold; it closed seamlessly (7 and Panther board in the beam)");
        }

        /// <summary>Panther perched on something (a wall, a ledge): placed there and held perched - ChapterCat's Hold would put him
        /// back on the ground.</summary>
        void PerchCat(Vector3 at, Vector3 face) { if (Cat == null) return; Cat.Place(at - face * 0.05f, face); Cat.Hop(at, 0.02f, 0f); }
        /// <summary>A warm close-up of 7's face (three-quarter front, eye level).</summary>
        void HerFaceCloseUp(bool closer = false)
        {
            var h = HerHead();
            var f = Flat(Seven.transform.forward).normalized; var r = Vector3.Cross(Vector3.up, f);
            foreach (var a in new[] { 30f, -30f, 50f, -50f, 10f })
            {
                var d = Quaternion.Euler(0f, a, 0f) * f; var pos = h + d * (closer ? 1.05f : 1.4f) + Vector3.up * 0.02f;
                if (RideProbe.Overlap(pos, 0.2f, null, null, false) || !ChapterCamera.ClearLine(pos, h)) continue;
                Shoot(pos, h - Vector3.up * 0.04f, closer ? 30f : 34f, 0.01f); return;
            }
            Shoot(h + f * 1.4f + r * 0.4f, h, 34f, 0.01f);
        }
    }
}
