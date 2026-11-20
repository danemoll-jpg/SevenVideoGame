using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using L = Seven.MarsLayout;

namespace Seven
{
    /// <summary>Round 15 — Mars: the flow, sections 1-7 (the temple, the leads and the ride back: MarsDirectorTemple).</summary>
    public partial class MarsDirector
    {
        IEnumerator Flow(int cp)
        {
            Restore(cp);
            yield return null; yield return null;
            if (DemoBoot.RampCheck && DemoBoot.CaptureDir != null) { yield return UI.Fade(0f, 0.3f); SetZone("Bridge", true); yield return global::Seven.RampCheck.Run(Bike, Seven, Cam, "mars", MarsLayout.IsBikeZone); yield break; }
            if (cp <= L.CpArrival) BikeHoldWatch.Begin("mars", Hold, Bike, Saucer);   // QF17 (5)
            if (cp <= L.CpArrival) { yield return Arrival(); Checkpoint(L.CpCrossing); }
            else if (cp == L.CpCrossing) { BikeHoldWatch.Begin("mars_resumed", Hold, Bike, Saucer); yield return Relanding(); }   // QF17 (5)
            else yield return UI.Fade(0f, 1.2f);
            if (MarsState.Checkpoint <= L.CpCrossing) { yield return Crossing(); Checkpoint(L.CpMaint); }
            if (MarsState.Checkpoint <= L.CpMaint) { yield return Maintenance(); Checkpoint(L.CpSharedDoor); }
            if (MarsState.Checkpoint <= L.CpSharedDoor) { yield return SharedDoor(); Checkpoint(L.CpControls); }
            if (MarsState.Checkpoint <= L.CpControls) { yield return Controls(); Checkpoint(L.CpViper); }
            if (MarsState.Checkpoint <= L.CpViper) { yield return Encounter(); Checkpoint(L.CpTemple); }
            if (MarsState.Checkpoint <= L.CpTemple) { yield return Temple(); Checkpoint(L.CpReturn); }
            yield return RideBack();
        }

        /// <summary>Put everything where the checkpoint says (reload, Continue, Scene Select).</summary>
        void Restore(int cp)
        {
            foreach (var z in L.Zones) SetZone(z.Name, z.On);
            bool routeOpen = cp >= L.CpSharedDoor, spans = cp >= L.CpViper, viperDone = cp >= L.CpTemple, back = cp >= L.CpReturn;
            SetZone("Causeway", routeOpen); SetZone("ServiceGateZ", routeOpen);
            if (CausewaySlab != null && routeOpen) CausewaySlab.SetPositionAndRotation(L.CausewayGap, Quaternion.identity);
            if (ServiceGate != null) ServiceGate.gameObject.SetActive(!routeOpen);
            SetZone("SpanW", spans); SetZone("SpanE", spans);
            if (spans) { AlignSpan(SpanW, 1f); AlignSpan(SpanE, 1f); } else { AlignSpan(SpanW, 0f); AlignSpan(SpanE, 0f); }
            if (Conduit != null && cp >= L.CpViper) Conduit.position = L.PierConduit;
            SetEncounter(viperDone);
            SetZone("TempleHall", viperDone);
            SetZone("Bridge", back);
            if (Barrier != null) Barrier.SetActive(!back);
            if (Restraint != null) Restraint.SetActive(viperDone);
            if (DisplayLeads != null) DisplayLeads.SetActive(false);
            if (RecordTet != null) RecordTet.gameObject.SetActive(false);
            if (ViperPlatform != null) ViperPlatform.position = L.PlatformAt + Vector3.down * 3f;
            if (PlatformLight != null) PlatformLight.enabled = false;
            // the people
            if (Guard != null) Guard.Place(back ? L.GuardAt + new Vector3(-3f, 0f, -6f) : L.GuardAt, Vector3.left);
            if (Worker != null) Worker.Place(cp > L.CpControls && cp < L.CpTemple ? L.WestControl + new Vector3(-1.6f, 0f, 0f) : cp >= L.CpTemple && cp < L.CpReturn ? L.TempleDoor + new Vector3(-2.5f, 0f, 2f) : L.WorkerAt, Vector3.right);
            if (Caretaker != null) Caretaker.Place(cp < L.CpSharedDoor ? L.CaretakerAt : cp < L.CpTemple ? L.EastControl + new Vector3(1.6f, 0f, 0f) : cp < L.CpReturn ? L.TempleDoor + new Vector3(2.5f, 0f, 2f) : L.GuardAt + new Vector3(-3f, 0f, -4.4f), Vector3.left);
            if (Viper != null) { Viper.gameObject.SetActive(cp >= L.CpViper && cp < L.CpReturn); Viper.Place(viperDone ? L.ViperBlocks + new Vector3(4f, 0f, -1f) : L.ViperBlocks, Vector3.back); if (viperDone) Viper.Play("dazed"); }
            if (Transport != null) { var tp = routeOpen && !back ? L.CaretakerAt + new Vector3(4f, 0f, -6f) : L.TransportAt; tp.y = Ground.Height(tp.x, tp.z); Transport.SetPositionAndRotation(tp, Quaternion.Euler(0f, 90f, 0f)); }
            if (Drivers != null) for (int k = 0; k < Drivers.Length; k++) if (Drivers[k] != null) Drivers[k].Place((Transport != null ? Transport.position : L.TransportAt) + new Vector3(-3.4f, 0f, k == 0 ? 1.6f : -1.6f), Vector3.right);
            if (Saucer != null) Saucer.position = L.SaucerAt + (cp <= L.CpArrival ? Vector3.up * 80f : Vector3.zero);
            Vector3 at; float yaw = 90f; bool riding = true;
            switch (cp)
            {
                case L.CpArrival: at = L.BikeDown; riding = false; break;
                case L.CpCrossing: at = L.BikeDown; break;
                case L.CpMaint: at = L.CheckStop + new Vector3(0f, 0f, -6f); yaw = 180f; break;
                case L.CpSharedDoor: at = L.EastStop; yaw = 0f; riding = false; break;
                case L.CpControls: at = L.MechanismView + new Vector3(4f, 0f, -6f); yaw = 180f; break;
                case L.CpViper: at = new Vector3(-58f, 0f, 150f); yaw = 90f; riding = false; break;
                case L.CpTemple: at = L.ViperBlocks + new Vector3(0f, 0f, -4f); yaw = 0f; riding = false; break;
                default: at = new Vector3(-58f, 0f, 146f); yaw = 180f; break;
            }
            if (cp == L.CpTemple || cp == L.CpViper) Bike.Place(new Vector3(-62f, 0f, 146f), 90f); else Bike.Place(at, yaw);
            if (cp == L.CpArrival || cp == L.CpCrossing && !DemoBoot.RampCheck) { Bike.gameObject.SetActive(false); Seven.gameObject.SetActive(false); }   // QF17 (5): both start in the hold / aboard
            else if (riding) { Seven.gameObject.SetActive(true); Bike.Mount(); }
            else { Seven.gameObject.SetActive(true); Seven.Place(at + (cp == L.CpSharedDoor ? new Vector3(1.5f, 0f, 1.5f) : Vector3.zero), Quaternion.Euler(0f, yaw, 0f) * Vector3.forward); if (cp == L.CpSharedDoor) Bike.Place(L.EastStop + new Vector3(-2f, 0f, -1f), 0f); }
            MusicSystem.Play(cp == L.CpViper ? "mars_viper" : "mars_kingdoms", 2.5f);
            FollowCamera(); Cam.SnapBehind();
            Log($"restore: {MarsState.Summary()} -> {(riding ? "riding" : "on foot")} at {at}");
        }

        // ------------------------------------------------------------------------------------------ 1: arrival
        IEnumerator Arrival()
        {
            yield return Cutscene(true);
            // EXTREME WIDE: the saucer crosses an immense plain cut by walls, trenches and elevated roads; two fortress cities
            // on either side of a deep ravine; the temple on its island of stone, older than everything round it
            var land = L.SaucerAt; var from = land + new Vector3(260f, 60f, 180f);
            Saucer.position = from;
            var camAt = new Vector3(-150f, 46f, -120f);
            Shoot(camAt, new Vector3(0f, 0f, 120f), 58f, 0.01f);
            StartCoroutine(UI.Fade(0f, 1.8f));
            AudioKit.Play("saucer_descent", null, 0.7f);
            bool s1 = false, s2 = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 9f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Saucer.position = Vector3.Lerp(from, land, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 24f;
                var look = Vector3.Lerp(new Vector3(0f, 0f, 120f), Saucer.position, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.4f - 0.2f)));
                Cam.Shot(Vector3.Lerp(camAt, land + new Vector3(30f, 10f, -32f), u * u), look, 1.6f, false, 58f);
                if (!s1 && t > 0.18f) { s1 = true; StartCoroutine(Shot("m01_extreme_wide_the_divided_plain")); }
                if (!s2 && t > 0.7f) { s2 = true; StartCoroutine(Shot("m02_the_saucer_lands_outside_the_approaches")); }
                yield return null;
            }
            Saucer.position = land;
            yield return LandingTail();
            Objective("Find a route to the temple - ride east through the settlement to the crossing");
            Complete("MAR_01");
        }

        /// <summary>QF17 (5): Mars entered from a save at the landing site (Continue, the star map continuing a save, Scene
        /// Select "the crossing") - QF13's rule lived only inside the arrival, so these starts restored the bike straight onto
        /// the plain. Now they play the landing's tail too: the saucer hovering, the hatch opens, the bike lowers out, 7 comes
        /// down the beam to it.</summary>
        IEnumerator Relanding()
        {
            yield return Cutscene(true);
            Seven.gameObject.SetActive(false); Bike.gameObject.SetActive(false);
            Saucer.position = L.SaucerAt;
            Shoot(L.SaucerAt + new Vector3(30f, 10f, -32f), L.SaucerAt + Vector3.up * 2f, 58f, 0.01f);
            StartCoroutine(UI.Fade(0f, 1.2f));
            yield return Wait(1.2f);
            yield return LandingTail();
            Objective("Find a route to the temple - ride east through the settlement to the crossing");
        }
        IEnumerator LandingTail()
        {
            var land = L.SaucerAt;
            // the bike lowers out of the bike hold; 7 descends in the beam (Panther stays aboard)
            yield return HoldBikeOut(L.BikeDown, 90f);
            Shoot(land + new Vector3(10f, 2.2f, 9f), land + new Vector3(0f, 2.2f, 0f), 46f, 0.01f);
            if (SaucerBeam != null) { yield return SaucerBeam.Iris(true); yield return SaucerBeam.BeamOn(true); }
            Seven.gameObject.SetActive(true); Seven.Lock(true); Seven.BeginFloat();
            bool s3 = false;
            if (SaucerBeam != null) yield return SaucerBeam.Lower(Seven.transform, 3.4f, Vector3.right, t => { if (!s3 && t > 0.5f) { s3 = true; StartCoroutine(Shot("m03_7_down_the_beam_bike_waiting")); } });
            Seven.EndFloat(); Seven.Place(L.SevenDown, Vector3.right); Seven.Lock(true);
            if (SaucerBeam != null) StartCoroutine(SaucerBeam.Open(false));
            yield return WalkSeven(L.BikeDown - Bike.transform.right * 1.1f, 8f);
            Seven.Lock(true);
            Bike.Mount(); FollowCamera();
            yield return Cutscene(false);
        }

        // ---- the bike hold (the one saucer prefab): the same opening, glide and close as Venus / Europa
        void ShowBike(bool on)
        {
            foreach (var r in Bike.GetComponentsInChildren<Renderer>(true)) r.enabled = on;
            foreach (var l in Bike.GetComponentsInChildren<Light>(true)) l.enabled = on;
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
            yield return Shot("m02b_the_bike_hold_open");
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
            Log("the bike came out of the bike hold and settled on the plain; the hold closed seamlessly (7 comes down in the beam)");
        }
        IEnumerator CloseHoldLater(float s) { yield return Wait(s); yield return Hold.Close(); }
        IEnumerator HoldBikeIn()
        {
            if (Hold == null) { Bike.gameObject.SetActive(false); yield break; }
            var dir = Hold.Dir; var tan = Vector3.Cross(Vector3.up, dir); var mouth = Hold.Mouth;
            float yawOut = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, bottomIn = Hold.transform.position.y + Hold.BottomIn;
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

        // ------------------------------------------------------------------------------------------ 2-3: the crossing, Viper
        IEnumerator Crossing()
        {
            FreeBike = true;
            if (!Riding) PlaceRiding(L.BikeDown, 90f);
            Objective("Find a route to the temple - ride east through the settlement to the crossing");
            // a living settlement first: workers, families, traders and guards going about their day
            bool marketShot = false;
            IEnumerator MarketShot() { while (!marketShot) { if (Flat(P7 - L.MarketAt).magnitude < 14f) { marketShot = true; yield return Shot("m04_the_settlement_ordinary_life"); } yield return null; } }
            StartCoroutine(MarketShot());
            yield return RideRoute(L.ToCheckpoint, "through the settlement to the checkpoint", 8f);
            marketShot = true;
            ShowGuide(L.CheckStop);
            if (Auto) StartCoroutine(AutoRide(new[] { L.CheckStop }, 2.5f));
            yield return Until(() => Flat(P7 - L.CheckStop).magnitude < 7f || P7.x > L.CheckStop.x - 2f, "the checkpoint", 60f, "Ride up to the checkpoint at the bridge", L.CheckStop, () => PlaceRiding(L.CheckStop, 90f));
            HideGuide(); StopAutoRide();
            yield return StopBike(2f);
            FreeBike = false;
            yield return Cutscene(true);
            if (Riding) { Bike.DismountTo(L.CheckStop + new Vector3(2.5f, 0f, -1.5f), Vector3.right); }
            Seven.Place(L.GuardAt + new Vector3(-3.4f, 0f, -0.8f), Vector3.right); Seven.Lock(true);
            // the checkpoint: a supply transport stranded behind a closed barrier
            Shoot(L.CheckStop + new Vector3(2f, 4.5f, -12f), L.BarrierAt + Vector3.up * 1.5f, 50f, 0.01f);
            yield return Wait(0.8f);
            yield return Shot("m05_checkpoint_the_stranded_transport");
            // MEDIUM - 7 and a guard
            var sevenAt = P7; var g = Guard != null ? Guard.transform.position : L.GuardAt;
            if (Guard != null) Guard.Face(sevenAt);
            Seven.transform.rotation = Quaternion.LookRotation(Flat(g - sevenAt));
            var mid = (sevenAt + g) * 0.5f; var side = Vector3.Cross(Vector3.up, Flat(g - sevenAt).normalized);
            TwoShot(mid, side, 3.6f, 1.55f, 1.4f, 44f, 0.6f);
            yield return Say("MAR_02", 1, 4);                    // I'm looking for the temple. / Crossing is closed. / For how long? / Until the other side withdraws.
            yield return Shot("m06_medium_7_and_the_guard");
            // across the checkpoint, a worker gestures toward the supplies
            if (Worker != null) { Worker.Face(Transport != null ? Transport.position : L.TransportAt); Worker.Play("point"); }
            var w = Worker != null ? Worker.transform.position : L.WorkerAt;
            Shoot(w + new Vector3(4f, 1.6f, -3.5f), (w + (Transport != null ? Transport.position : L.TransportAt)) * 0.5f + Vector3.up * 1.2f, 46f, 0.5f);
            yield return Say("MAR_02", 5, 5);                    // Those are water filters. They're waiting for them.
            if (Worker != null) Worker.Play("idle");
            TwoShot(mid, side, 3.6f, 1.55f, 1.4f, 44f, 0.5f);
            yield return Say("MAR_02", 6, 6);                    // I have my orders.
            // 7 looks from the supplies to the bridge
            Seven.transform.rotation = Quaternion.LookRotation(Flat(L.BarrierAt + Vector3.forward * 20f - P7));
            yield return Wait(0.6f);
            yield return Say("MAR_02", 7, 7);                    // Who gives them?
            Complete("MAR_02");
            // a low mechanical sound interrupts - LOW-ANGLE REVEAL: Viper on an elevated platform, harsh white light from beneath
            AudioKit.Play("mars_platform_rise", L.PlatformAt, 1f, 0.03f, AudioKit.Bus.Sfx, 80f);
            if (Viper != null) { Viper.gameObject.SetActive(true); Viper.transform.SetParent(ViperPlatform, true); }
            if (ViperPlatform != null)
            {
                var p0 = L.PlatformAt + Vector3.down * 3f; var p1 = L.PlatformAt + Vector3.up * L.PlatformTop;
                if (Viper != null) Viper.transform.SetPositionAndRotation(p0 + Vector3.up * 0.4f, Quaternion.LookRotation(Flat(P7 - L.PlatformAt)));
                if (PlatformLight != null) PlatformLight.enabled = true;
                var lowCam = L.PlatformAt - Flat(L.PlatformAt - P7).normalized * 13f + Vector3.up * 1.2f;       // low angle, far enough to see him above the deck
                for (float t = 0f; t < 1f; t += Time.deltaTime / 3.6f)
                {
                    ViperPlatform.position = Vector3.Lerp(p0, p1, Mathf.SmoothStep(0f, 1f, t));
                    Cam.Shot(lowCam, ViperPlatform.position + Vector3.up * 1.9f, 0.5f, false, 46f);
                    yield return null;
                }
                if (Viper != null) Viper.Play("idle");
            }
            yield return Wait(0.4f);
            yield return Shot("m07_low_angle_reveal_viper_lit_from_beneath");
            var vp = Viper != null ? Viper.transform.position : L.PlatformAt + Vector3.up * L.PlatformTop;
            yield return Say("MAR_03", 1, 1);                    // Someone who understands what happens when we become careless.
            Shoot(P7 + Flat(vp - P7).normalized * -2.2f + Vector3.up * 1.7f + Vector3.Cross(Vector3.up, Flat(vp - P7).normalized) * 0.7f, vp + Vector3.up * 1.6f, 40f, 0.5f);
            yield return Say("MAR_03", 2, 4);                    // They need those supplies. / Then they can accept our terms. / And the temple?
            // Viper notices the geometric objects she carries; she shifts slightly, keeping them close
            ShotClear(P7 + Vector3.up * 1.05f, new[] { P7 + Seven.transform.right * 2.1f - Seven.transform.forward * 0.4f + Vector3.up * 1.3f, P7 - Seven.transform.right * 2.1f - Seven.transform.forward * 0.4f + Vector3.up * 1.3f, P7 - Seven.transform.forward * 2.4f + Seven.transform.right * 0.8f + Vector3.up * 1.6f }, 36f, 0.4f);
            yield return Say("MAR_03", 5, 5);                    // What business do you have there?
            Seven.transform.rotation *= Quaternion.Euler(0f, -12f, 0f);
            yield return Wait(0.4f);
            yield return Shot("m08_she_keeps_the_records_close");
            yield return Say("MAR_03", 6, 6);                    // Historical.
            Shoot(P7 + Flat(vp - P7).normalized * 3f + Vector3.up * 0.6f, vp + Vector3.up * 2.0f, 42f, 0.4f);
            if (Viper != null) Viper.Play("talk");
            yield return Say("MAR_03", 7, 7);                    // History is precisely the problem. (dry laugh)
            // he orders the crossing secured and leaves
            if (Viper != null) Viper.Play("point");
            if (Guard != null) { Guard.Face(L.BarrierAt); }
            { var dv = Flat(vp - P7).normalized; Shoot(P7 - dv * 2.2f + Vector3.up * 1.85f + Vector3.Cross(Vector3.up, dv) * 0.8f, vp + Vector3.up * 2.3f, 48f, 0.4f); }   // QF17 (6): over her shoulder up to him, never from below
            yield return Wait(1.4f);
            yield return Shot("m09_he_orders_the_crossing_secured");
            if (ViperPlatform != null)
            {
                var p1 = ViperPlatform.position; var p2 = L.PlatformAt + Vector3.down * 3f;
                AudioKit.Play("mars_platform_rise", L.PlatformAt, 0.8f, 0.03f, AudioKit.Bus.Sfx, 80f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 2.6f) { ViperPlatform.position = Vector3.Lerp(p1, p2, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
                if (PlatformLight != null) PlatformLight.enabled = false;
            }
            if (Viper != null) { Viper.transform.SetParent(null, true); Viper.gameObject.SetActive(false); }
            yield return Cutscene(false);
            Complete("MAR_03");
            // privately, in gameplay
            yield return SayInPlay("MAR_03", 8, 10);              // Charming. / He noticed the records. / I noticed him noticing.
            Objective("Find another way across - the old maintenance route goes down into the ravine south of the checkpoint");
        }

        // ------------------------------------------------------------------------------------------ 4: both sides of the ravine
        IEnumerator Maintenance()
        {
            FreeBike = true;
            if (!Riding) { Objective("Get back on the bike (X)"); ShowGuide(Bike.transform.position); if (Auto) { yield return WalkSeven(Bike.transform.position - Bike.transform.right * 1.1f, 10f); PlaceRiding(Bike.transform.position); } yield return Until(() => Riding, "on the bike", 40f, "Walk to the bike and press X", Bike.transform.position, () => PlaceRiding(Bike.transform.position)); HideGuide(); }
            Objective("Take the old maintenance route down into the ravine");
            yield return RideRoute(new[] { L.Maint[0], L.Maint[1], L.Maint[2], L.Maint[3], L.CausewayStop }, "down the maintenance route", 7f);
            yield return Shot("m10_the_ravine_floor_broken_causeway");
            // the broken causeway: the fallen slab lies beside the gap
            yield return StopBike(2f);
            Objective("The causeway is broken - lay the fallen slab back across the gap (get off, RT)");
            yield return Telekinesis(L.CausewaySlabRest + Vector3.up * 0.4f, L.SlabTkRange, "lay the causeway slab (telekinesis)", "Get off the bike (X), look at the long fallen slab beside the gap and use telekinesis (RT)", CausewayEdges, new[] { L.CausewayStop + new Vector3(-1f, 0f, -4f) });
            yield return LaySlab();
            SetZone("Causeway", true);
            Objective("Ride across and on along the ravine floor");
            if (!Riding) { ShowGuide(Bike.transform.position); if (Auto) { yield return WalkSeven(Bike.transform.position - Bike.transform.right * 1.1f, 10f); PlaceRiding(Bike.transform.position); } yield return Until(() => Riding, "back on the bike", 40f, "Walk to the bike and press X", Bike.transform.position, () => PlaceRiding(Bike.transform.position)); HideGuide(); }
            // broad riding sections, a sheltered passage, a lookout to stop at (optional)
            yield return RideRoute(new[] { L.Maint[5], L.Maint[6], L.Maint[7], L.Maint[8], L.Maint[9] }, "across the floor and up the east wall", 8f);
            yield return Shot("m11_the_sheltered_passage_and_the_east_ramp");
            // the east top: the service gate is sealed and unpowered - its panel is on this side
            Objective("The service gate is sealed - restore its power at the panel (X)");
            yield return Interact(L.ServiceGatePanel, "Restore power", "restore the service gate's power", "Get off beside the gate at the top of the ramp and press X at its panel", new[] { L.ServiceGatePanel + new Vector3(-0.6f, 0f, -0.7f) });
            Glow(ServicePanel, 1.6f);
            AudioKit.Play("mars_control_on", L.ServiceGatePanel, 0.9f, 0.03f, AudioKit.Bus.Sfx, 40f);
            yield return OpenServiceGate();
            // the transport crosses through the restored service passage
            yield return TransportThrough();
            // on the opposite side: the caretaker who keeps the temple approach
            Objective("Talk to the Reptilian at the far checkpoint (X)");
            if (Caretaker != null) Caretaker.Face(P7);
            yield return Interact(Caretaker != null ? Caretaker.transform.position + Vector3.left * 1.4f : L.CaretakerAt, "Talk", "talk to the caretaker", "The Reptilian by the far checkpoint - walk up and press X", new[] { L.EastStop, L.CaretakerAt + Vector3.left * 1.5f });
            yield return Cutscene(true);
            var c = Caretaker != null ? Caretaker.transform.position : L.CaretakerAt;
            Seven.transform.rotation = Quaternion.LookRotation(Flat(c - P7)); if (Caretaker != null) Caretaker.Face(P7);
            var mid = (P7 + c) * 0.5f; var side = Vector3.Cross(Vector3.up, Flat(c - P7).normalized);
            TwoShot(mid, side, 3.4f, 1.5f, 1.4f, 44f, 0.6f);
            yield return Say("MAR_04", 1, 4);                    // You brought the filters through? / Helped them through. / Hm. ... / The people carrying them ...
            yield return Shot("m12_the_caretaker");
            // the caretaker looks toward the opposite settlement
            if (Caretaker != null) Caretaker.Face(new Vector3(-150f, 0f, 0f));
            Shoot(c + new Vector3(1.6f, 1.7f, 2.4f), new Vector3(-90f, 4f, 0f), 44f, 0.5f);
            yield return Wait(0.6f);
            yield return Say("MAR_04", 5, 5);                    // That is not what we've been hearing.
            yield return Shot("m13_he_looks_toward_the_other_bank");
            Complete("MAR_04");
            yield return Cutscene(false);
            FreeBike = false;
        }

        IEnumerator LaySlab()
        {
            yield return Cutscene(true);
            Shoot(L.CausewayStop + new Vector3(-3f, 4.2f, -9f), L.CausewayGap + Vector3.up * 0.5f, 50f, 0.6f);
            AudioKit.Play("slab_lift", L.CausewaySlabRest, 1f, 0.05f, AudioKit.Bus.Sfx, 40f);
            var from = CausewaySlab != null ? CausewaySlab.position : L.CausewaySlabRest; var r0 = CausewaySlab != null ? CausewaySlab.rotation : Quaternion.identity;
            bool s = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.6f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                if (CausewaySlab != null) CausewaySlab.SetPositionAndRotation(Vector3.Lerp(from, L.CausewayGap, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 3f, Quaternion.Slerp(r0, Quaternion.identity, u));
                if (!s && t > 0.5f) { s = true; StartCoroutine(Shot("m10b_the_slab_lifted_into_the_gap")); }
                yield return null;
            }
            AudioKit.Play("mars_section_slide", L.CausewayGap, 1f, 0.05f, AudioKit.Bus.Sfx, 40f);
            CameraShake.Quake(0.4f, 0.25f);
            Log("the causeway slab is back in its gap (Causeway zone open)");
            yield return Cutscene(false);
        }

        IEnumerator OpenServiceGate()
        {
            if (ServiceGate == null) { SetZone("ServiceGateZ", true); yield break; }
            yield return Cutscene(true);
            Shoot(L.ServiceGateAt + new Vector3(6f, 3f, -8f), L.ServiceGateAt + Vector3.up * 1.5f, 48f, 0.5f);
            AudioKit.Play("mars_machinery", L.ServiceGateAt, 0.9f, 0.03f, AudioKit.Bus.Sfx, 40f);
            var p0 = ServiceGate.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.4f) { ServiceGate.position = p0 + Vector3.down * 4.6f * Mathf.SmoothStep(0f, 1f, t); yield return null; }
            ServiceGate.gameObject.SetActive(false);
            SetZone("ServiceGateZ", true);
            yield return Shot("m11b_the_service_gate_open");
            yield return Cutscene(false);
        }

        /// <summary>The supply transport (its two drivers walking beside it) comes through the restored service passage.</summary>
        IEnumerator TransportThrough()
        {
            if (Transport == null) yield break;
            yield return Cutscene(true);
            yield return UI.Fade(1f, 0.6f);
            // a short time later: it has come down the west ramp and across the floor - the last stretch up and through the gate
            var path = new[] { new Vector3(56f, L.FloorY, -64f), new Vector3(56f, -10f, -34f), new Vector3(58f, 0f, -14f), L.CaretakerAt + new Vector3(4f, 0f, -6f) };
            Transport.position = path[0]; Transport.gameObject.SetActive(true);
            var hum = AudioKit.Loop("mars_transport_hum", Transport, null, 0.6f, AudioKit.Bus.Sfx, 4f, 60f);
            Shoot(L.ServiceGateAt + new Vector3(10f, 4f, 6f), L.ServiceGateAt + new Vector3(0f, -2f, -10f), 52f, 0.01f);
            yield return UI.Fade(0f, 0.6f);
            bool s = false;
            for (int i = 0; i < path.Length - 1; i++)
            {
                var a = path[i]; var b = path[i + 1]; float len = Vector3.Distance(a, b);
                for (float d = 0f; d < len; d += 6f * Time.deltaTime)
                {
                    var p = Vector3.Lerp(a, b, d / len); p.y = Ground.Height(p.x, p.z);
                    Transport.SetPositionAndRotation(p, Quaternion.Slerp(Transport.rotation, Quaternion.LookRotation(Flat(b - a)), Time.deltaTime * 3f));
                    if (Drivers != null) for (int k = 0; k < Drivers.Length; k++) if (Drivers[k] != null) { Drivers[k].gameObject.SetActive(true); var dp = p - Transport.forward * (2.6f + k * 1.2f) + Transport.right * (k == 0 ? 1.8f : -1.8f); dp.y = Ground.Height(dp.x, dp.z); Drivers[k].transform.SetPositionAndRotation(dp, Transport.rotation); Drivers[k].Play("walk"); }
                    Cam.Shot(L.ServiceGateAt + new Vector3(10f, 4f, 6f), p + Vector3.up * 1.5f, 1.0f, false, 52f);
                    if (!s && i == 2) { s = true; StartCoroutine(Shot("m11c_the_transport_comes_through")); }
                    yield return null;
                }
            }
            if (Drivers != null) foreach (var d in Drivers) if (d != null) d.Play("idle");
            if (hum != null) StartCoroutine(AudioKit.Fade(hum, 0f, 1.5f, true));
            Log("the supply transport came through the restored service passage");
            yield return Cutscene(false);
        }

        // ------------------------------------------------------------------------------------------ 5: the shared door
        IEnumerator SharedDoor()
        {
            // the caretaker takes 7 to an ancient mechanism overlooking the temple (a short cut: both arrive there)
            yield return Cutscene(true);
            yield return UI.Fade(1f, 0.8f);
            if (Riding) Bike.DismountTo(L.MechanismView + new Vector3(-2f, 0f, -3f), Vector3.forward);
            Bike.Place(L.MechanismView + new Vector3(4f, 0f, -6f), 0f);
            Seven.Place(L.MechanismView, Flat(L.MechanismAt - L.MechanismView)); Seven.Lock(true);
            if (Caretaker != null) Caretaker.Place(L.MechanismAt + new Vector3(2.2f, 0f, -1.4f), L.MechanismAt - (L.MechanismAt + new Vector3(2.2f, 0f, -1.4f)));
            // OVER 7's SHOULDER - the mechanism; two channels lead into it from opposite sides of the ravine
            var head = P7 + Vector3.up * 1.6f; var f = Flat(L.MechanismAt - P7).normalized;
            Shoot(head - f * 1.1f + Vector3.Cross(Vector3.up, f) * 0.55f + Vector3.up * 0.2f, L.MechanismAt + Vector3.up * 1.0f, 42f, 0.01f);
            Glow(ChannelW, 0.25f); Glow(ChannelE, 0.25f);
            yield return UI.Fade(0f, 0.8f);
            yield return Shot("m14_over_her_shoulder_the_mechanism");
            yield return Say("MAR_05", 1, 4);                    // Two controls? / One on each bank. / So both sides have to open it. / That was the intention.
            yield return Say("MAR_05", 5, 5);                    // Rather optimistic architecture.
            Shoot(P7 + f * 1.4f + Vector3.Cross(Vector3.up, f) * 1.2f + Vector3.up * 1.5f, P7 + Vector3.up * 1.5f, 34f, 0.5f);
            yield return Say("MAR_05", 6, 6);                    // Or they used to trust each other.
            yield return Shot("m15_or_they_used_to_trust_each_other");
            // the caretaker explains: Viper has disabled the shared approach and uses its closure to control passage
            var c = Caretaker != null ? Caretaker.transform.position : L.MechanismAt;
            var mid = (P7 + c) * 0.5f; var side = Vector3.Cross(Vector3.up, Flat(c - P7).normalized);
            TwoShot(mid, side, 3.2f, 1.6f, 1.4f, 44f, 0.5f);
            yield return Say("MAR_05", 7, 9);                    // He tells each bank ... / Does either one? / Some would. Others just want the bridge open.
            Complete("MAR_05");
            yield return Cutscene(false);
            Objective("Restore both temple controls - first back to the west bank: the worker at the checkpoint");
        }

        // ------------------------------------------------------------------------------------------ 6: two controls
        IEnumerator Controls()
        {
            FreeBike = true;
            if (!Riding) { ShowGuide(Bike.transform.position); if (Auto) { yield return WalkSeven(Bike.transform.position - Bike.transform.right * 1.1f, 12f); PlaceRiding(Bike.transform.position); } yield return Until(() => Riding, "on the bike", 40f, "Walk to the bike and press X", Bike.transform.position, () => PlaceRiding(Bike.transform.position)); HideGuide(); }
            Objective("Back to the west bank with the news - the service route is safe; the caretaker will help");
            if (Caretaker != null) Caretaker.Place(L.EastControl + new Vector3(1.6f, 0f, 0f), Vector3.left);
            var back = new[] { new Vector3(64f, 0f, 60f), new Vector3(60f, 0f, -6f), L.Maint[9], L.Maint[8], L.Maint[6], L.Maint[5], L.Maint[4], L.Maint[3], L.Maint[2], L.Maint[1], L.Maint[0], new Vector3(-62f, 0f, -6f) };
            yield return RideRoute(back, "back to the west checkpoint", 8f);
            // the worker from the supply transport volunteers to operate one control
            Objective("Talk to the worker by the checkpoint (X)");
            yield return Interact(Worker != null ? Worker.transform.position + Vector3.right * 1.4f : L.WorkerAt, "Talk", "talk to the worker", "The worker who was with the transport - walk up and press X", new[] { L.WorkerAt + Vector3.right * 1.6f });
            yield return Cutscene(true);
            var w = Worker != null ? Worker.transform.position : L.WorkerAt;
            if (Worker != null) Worker.Face(P7);
            Seven.transform.rotation = Quaternion.LookRotation(Flat(w - P7));
            var mid = (P7 + w) * 0.5f; var side = Vector3.Cross(Vector3.up, Flat(w - P7).normalized);
            TwoShot(mid, side, 3.2f, 1.5f, 1.4f, 44f, 0.6f);
            yield return Wait(1.0f);                              // she tells them the route is safe and the caretaker will help
            if (Worker != null) { Worker.Play("wave"); }          // the worker volunteers: a nod and a raised hand
            yield return Wait(1.6f);
            yield return Shot("m16_the_worker_volunteers");
            if (Worker != null) { Worker.Play("point"); Worker.Face(L.WestControl); }
            yield return Wait(1.0f);
            yield return Cutscene(false);
            // the worker goes up to the west control (a short cut later shows them there)
            if (Worker != null) Worker.Place(L.WestControl + new Vector3(-1.6f, 0f, 0f), Vector3.right);
            Glow(ControlW, 0.3f); Glow(ControlE, 0.3f);
            // 1. ride north to the relay on the west rim and reconnect it
            Objective("Ride north along the west rim to the relay and reconnect it (X)");
            yield return RideRoute(new[] { new Vector3(-62f, 0f, 20f), new Vector3(-66f, 0f, 70f), L.RelayWest + new Vector3(0f, 0f, -4f) }, "to the relay on the west rim", 7f);
            yield return Interact(L.RelayWest, "Reconnect", "reconnect the relay", "Stop by the relay pylon on the rim and press X", new[] { L.RelayWest + new Vector3(-1.2f, 0f, -1.2f) });
            Glow(RelayW, 1.6f); Glow(ChannelW, 0.7f);
            AudioKit.Play("mars_control_on", L.RelayWest, 0.9f, 0.03f, AudioKit.Bus.Sfx, 40f);
            yield return SayInPlay("MAR_06", 1, 1);               // The far control is ready.
            // 2. on foot through the ancient bridge structure: down the pier walk, the conduit pushed back into place
            Objective("Go down the pier walk under the old bridge and push the displaced conduit back into place (RT)");
            if (Riding) yield return GetOff();
            yield return GuideWalk(new[] { new Vector3(-58f, 0f, 158f), new Vector3(-55.5f, 0f, 160f), new Vector3(-49f, -3.4f, 160f), new Vector3(-46.3f, -4f, 163.5f) }, "down the pier steps");
            yield return Telekinesis(L.PierConduitRest, 7f, "the pier conduit (telekinesis)", "On foot: from the west bluff take the steps down the side of the old bridge; the displaced conduit is at the end of the walk", null, new[] { new Vector3(-62f, 0f, 150f), L.PierWalkTop, new Vector3(-50f, -3f, 160f), new Vector3(-46.4f, -4f, 163.5f) });
            yield return PushConduit();
            Glow(ChannelW, 1.2f); Glow(ControlW, 0.9f);
            // back up the steps to the worker's control (the light leads the way)
            yield return GuideWalk(new[] { new Vector3(-49f, -3.4f, 160f), new Vector3(-55.5f, 0f, 160f), new Vector3(-60f, 0f, 156f) }, "back up the pier steps");
            // 3. protect the repair: Viper's small machines go for the worker's control - knock them aside (nonlethal)
            yield return Jammers3();
            // 4. "Tell them to hold." - a mechanism moves unexpectedly
            yield return SayInPlay("MAR_06", 2, 3);               // Tell them to hold. / They are holding.
            yield return SwingAndSecure();
            // the two inhabitants activate their controls together - WIDE: the ancient structures align into a passage
            yield return Align();
            Complete("MAR_06");
            FreeBike = false;
        }

        IEnumerator PushConduit()
        {
            if (Conduit == null) yield break;
            AudioKit.Play("slab_lift", Conduit.position, 0.8f, 0.05f, AudioKit.Bus.Sfx, 30f);
            var a = Conduit.position; var r0 = Conduit.rotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.6f) { float u = Mathf.SmoothStep(0f, 1f, t); Conduit.SetPositionAndRotation(Vector3.Lerp(a, L.PierConduit, u), Quaternion.Slerp(r0, Quaternion.identity, u)); yield return null; }
            AudioKit.Play("mars_control_on", L.PierConduit, 0.8f, 0.03f, AudioKit.Bus.Sfx, 40f);
            yield return Shot("m17_the_conduit_back_in_place");
            Log("the pier conduit is back in place (power restored to the west control)");
        }

        IEnumerator Jammers3()
        {
            Objective("Viper's machines are going for the worker's control - knock them aside with telekinesis (RT)");
            if (Jammers == null || Jammers.Length == 0) yield break;
            if (Auto) StartCoroutine(WalkPath(new[] { L.PierWalkTop, L.WestControl + new Vector3(-6f, 0f, -6f) }, 30f));
            var target = L.WestControl + Vector3.up * 1.4f;
            for (int i = 0; i < Jammers.Length; i++)
            {
                var j = Jammers[i]; if (j == null) continue;
                j.gameObject.SetActive(true); j.position = L.JammerFrom[i % L.JammerFrom.Length];
                bool down = false;
                IEnumerator Fly() { var hum = AudioKit.Loop("mars_barrier_hum", j, null, 0.25f, AudioKit.Bus.Sfx, 3f, 30f); for (float t = 0f; !down; t += Time.deltaTime) { var to = target + new Vector3(Mathf.Sin(t * 2f) * 1.2f, 0.6f + Mathf.Sin(t * 3f) * 0.3f, 0f) - j.position; float sp = Flat(to).magnitude > 6f ? 2.2f : 0.4f; j.position += to.normalized * Mathf.Min(to.magnitude, sp * Time.deltaTime); j.Rotate(0f, 120f * Time.deltaTime, 0f); yield return null; } if (hum != null) StartCoroutine(AudioKit.Fade(hum, 0f, 0.4f, true)); }
                StartCoroutine(Fly());
                yield return Telekinesis(j.position, 22f, $"knock Viper's machine aside ({i + 1}/{Jammers.Length})", "Look at the small hovering machine and use telekinesis (RT)", null, null, true, j);
                down = true;
                // it tumbles away and drops, harmlessly, onto the bluff
                var p0 = j.position; var land = p0 + Flat(p0 - P7).normalized * 6f; land.y = Ground.Height(land.x, land.z) + 0.25f;
                AudioKit.Play("mars_barrier_off", p0, 0.8f, 0.05f, AudioKit.Bus.Sfx, 30f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.9f) { j.position = Vector3.Lerp(p0, land, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 2.5f; j.Rotate(300f * Time.deltaTime, 0f, 200f * Time.deltaTime); yield return null; }
                if (i == 0) yield return Shot("m18_a_machine_knocked_aside");
            }
            Log($"protect the repair: {Jammers.Length} of Viper's machines knocked aside (nonlethal - machines, nobody hurt)");
        }

        IEnumerator SwingAndSecure()
        {
            // a mechanism moves unexpectedly - a section of the west span starts to swing out of line
            var seg = SwingSeg; float ang = 0f; bool held = false;
            AudioKit.Play("mars_section_slide", L.SwingSegment, 1f, 0.03f, AudioKit.Bus.Sfx, 60f);
            IEnumerator Swing() { while (!held) { ang = Mathf.MoveTowards(ang, 28f, 14f * Time.deltaTime); if (seg != null) seg.localRotation = Quaternion.Euler(0f, ang, 0f); yield return null; } }
            StartCoroutine(Swing());
            yield return SayInPlay("MAR_06", 4, 4);               // Not that one! (a startled breath)
            yield return SayInPlay("MAR_06", 5, 5);               // Specificity. Always helpful.
            Objective("Hold the swinging section steady with telekinesis (RT)");
            yield return Telekinesis(L.SwingSegment, 30f, "secure the swinging span section (telekinesis)", "Look at the section swinging out from the old bridge and use telekinesis (RT)", null, null);
            held = true;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f) { ang = Mathf.Lerp(ang, 0f, t); if (seg != null) seg.localRotation = Quaternion.Euler(0f, ang, 0f); yield return null; }
            if (seg != null) seg.localRotation = Quaternion.identity;
            yield return Shot("m19_she_secures_it");
            yield return SayInPlay("MAR_06", 6, 6);               // Now. (a short, relieved laugh)
        }

        void AlignSpan(Transform span, float u)
        {
            if (span == null) return;
            // each span's segments: a child "Seg*" per section, misaligned (dropped, turned) until u = 1
            int i = 0;
            foreach (Transform s in span)
            {
                if (!s.name.StartsWith("Seg")) continue;
                float k = (i % 3) - 1; i++;
                s.localPosition = new Vector3(s.localPosition.x, Mathf.Lerp(-2.2f - Mathf.Abs(k) * 1.4f, 0f, u), Mathf.Lerp(k * 2.2f, 0f, u));
                s.localRotation = Quaternion.Euler(Mathf.Lerp(k * 9f, 0f, u), Mathf.Lerp(k * 14f, 0f, u), Mathf.Lerp(-k * 6f, 0f, u));
            }
        }

        IEnumerator Align()
        {
            yield return Cutscene(true);
            // the worker and the caretaker on their controls, together
            if (Worker != null) { Worker.Place(L.WestControl + new Vector3(-1.6f, 0f, 0f), Vector3.right); Worker.Play("point"); }
            if (Caretaker != null) { Caretaker.Place(L.EastControl + new Vector3(1.6f, 0f, 0f), Vector3.left); Caretaker.Play("point"); }
            Shoot(L.WestControl + new Vector3(-4.5f, 2.2f, -3.5f), L.WestControl + Vector3.up * 1.2f, 44f, 0.01f);
            yield return Wait(0.8f);
            Glow(ControlW, 2.2f);
            AudioKit.Play("mars_control_on", L.WestControl, 1f, 0.03f, AudioKit.Bus.Sfx, 80f);
            yield return Shot("m20_the_worker_on_the_west_control");
            Shoot(L.EastControl + new Vector3(4.5f, 2.2f, -3.5f), L.EastControl + Vector3.up * 1.2f, 44f, 0.01f);
            Glow(ControlE, 2.2f); Glow(ChannelE, 1.2f);
            yield return Wait(0.8f);
            yield return Shot("m21_the_caretaker_on_the_east_control");
            // WIDE - THE BRIDGE: the ancient structures align, forming a passage toward the temple
            Shoot(new Vector3(0f, 26f, 92f), L.IslandC + Vector3.up * 2f, 56f, 0.01f);
            AudioKit.Play("mars_bridge_align", L.IslandC, 1f, 0.02f, AudioKit.Bus.Sfx, 300f);
            CameraShake.Quake(2.5f, 0.35f);
            bool s = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 4.5f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                AlignSpan(SpanW, u); AlignSpan(SpanE, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.15f - 0.1f)));
                if (!s && t > 0.55f) { s = true; StartCoroutine(Shot("m22_wide_the_bridge_aligns")); }
                yield return null;
            }
            AlignSpan(SpanW, 1f); AlignSpan(SpanE, 1f);
            SetZone("SpanW", true); SetZone("SpanE", true);
            yield return Wait(1.2f);
            yield return Shot("m23_a_passage_toward_the_temple");
            Log("section 6: both controls together - the ancient spans aligned (SpanW / SpanE open)");
            yield return Cutscene(false);
        }
    }
}
