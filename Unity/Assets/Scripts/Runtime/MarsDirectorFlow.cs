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
            if (DemoBoot.LandWallTest && DemoBoot.CaptureDir != null) { yield return UI.Fade(0f, 0.3f); yield return global::Seven.LandWallTest.Run(Bike, Seven, Cam, SetZone); Cap.Finish("land wall test (QF18 10) done"); yield break; }
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
            if (Drawbridge != null) { Drawbridge.Set(routeOpen ? 1f : 0f); if (routeOpen && Drawbridge.Lock != null) Drawbridge.Lock.gameObject.SetActive(false); }   // QF18 (11)
            if (ServiceGate != null) ServiceGate.gameObject.SetActive(!routeOpen);
            if (GateLine != null) GateLine.All(routeOpen); if (GateFeed != null) GateFeed.All(routeOpen);
            bool powered = cp >= L.CpViper;
            if (ChannelWA != null) ChannelWA.All(powered); if (ChannelWB != null) ChannelWB.All(powered); if (ChannelE != null) ChannelE.All(powered);
            if (RelayCrystal != null && powered) RelayCrystal.rotation = Quaternion.identity;
            Glow(ControlW, powered ? 1.4f : 0.12f); Glow(ControlE, powered ? 1.4f : 0.12f); Glow(RelayW, powered ? 2.4f : 0.2f);
            SetZone("SpanW", spans); SetZone("SpanE", spans);
            if (spans) { AlignSpan(SpanW, 1f); AlignSpan(SpanE, 1f); } else { AlignSpan(SpanW, 0f); AlignSpan(SpanE, 0f); }
            if (Conduit != null && cp >= L.CpViper) Conduit.SetPositionAndRotation(L.PierConduit, Quaternion.identity);
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
            MusicSystem.Play("mars_kingdoms", 2.5f);                                // QF24 (18): Viper's track starts with his fight
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
            // QF18 (21): the extreme wide from high over the plain - the dune sea, the ravine, both fortress cities, the temple
            // island and (an easter egg) the Cydonia mesa below, a face only from up here; the haze thinned for it
            StartCoroutine(FogThin(1f, 0.2f, 0.6f));
            if (Atmo != null) { Atmo.DistanceDensity *= 0.25f; Atmo.MaxFog *= 0.6f; }
            Shoot(new Vector3(-150f, 190f, 230f), L.FaceAt + new Vector3(20f, 0f, -20f), 52f, 0.01f);
            StartCoroutine(UI.Fade(0f, 1.8f));
            for (float t = 0f; t < 3.2f; t += Time.deltaTime) { if (t > 1.6f && t < 1.62f) { ImageGate.ExpectPoint(L.FaceAt + new Vector3(20f, 27f, -20f), "the plain and the Cydonia mesa (its top, 26 m up)", 0f, "m00b_"); StartCoroutine(Shot("m00b_from_high_the_plain_and_the_cydonia_mesa")); } yield return null; }
            var camAt = new Vector3(-150f, 46f, -120f);
            Shoot(camAt, new Vector3(0f, 0f, 120f), 58f, 1.2f);
            AudioKit.Play("saucer_descent", null, 0.7f);
            bool s1 = false, s2 = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 9f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Saucer.position = Vector3.Lerp(from, land, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 24f;
                var look = Vector3.Lerp(new Vector3(0f, 0f, 120f), Saucer.position, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.4f - 0.2f)));
                Cam.Shot(Vector3.Lerp(camAt, land + new Vector3(30f, 10f, -32f), u * u), look, 1.6f, false, 58f);
                if (!s1 && t > 0.18f) { s1 = true; ImageGate.ExpectPoint(new Vector3(0f, 2f, 120f), "the divided plain (the saucer a speck crossing it)", 0f, "m01_"); StartCoroutine(Shot("m01_extreme_wide_the_divided_plain")); }
                if (!s2 && t > 0.7f) { s2 = true; ImageGate.ExpectPoint(Saucer.position + Vector3.down * 1f, "the saucer landing outside the approaches", 0f, "m02_"); StartCoroutine(Shot("m02_the_saucer_lands_outside_the_approaches")); }
                yield return null;
            }
            Saucer.position = land;
            StartCoroutine(FogThin(0f, 2.5f));
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
            if (Hold != null) ImageGate.Expect(Hold.transform, "the open bike hold", 0f, "m02b_");   // QF26 (2)
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

        // ------------------------------------------------------------------------------------------ QF18 helpers
        /// <summary>QF18 (8, 17): thin Kyle's fog round the action (her, the people, the machines read clearly, dark against
        /// the haze) - eased in / out; the far haze, glow and silhouettes stay.</summary>
        float _fog0Dist = -1f, _fog0Max, _fog0Ground, _fog0Far, _fog0Zen;
        IEnumerator FogThin(float k, float secs = 1.2f, float dim = -1f)
        {
            if (dim < 0f) dim = k;
            if (Atmo == null) yield break;
            if (_fog0Dist < 0f) { _fog0Dist = Atmo.DistanceDensity; _fog0Max = Atmo.MaxFog; _fog0Ground = Atmo.GroundDensity; _fog0Far = Atmo.FarBrightness; _fog0Zen = Atmo.ZenithBrightness; }
            float d0 = Atmo.DistanceDensity, m0 = Atmo.MaxFog, g0 = Atmo.GroundDensity, f0 = Atmo.FarBrightness, z0 = Atmo.ZenithBrightness;
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, secs))
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Atmo.DistanceDensity = Mathf.Lerp(d0, _fog0Dist * (1f - 0.6f * k), u); Atmo.MaxFog = Mathf.Lerp(m0, _fog0Max * (1f - 0.3f * k), u);
                Atmo.GroundDensity = Mathf.Lerp(g0, _fog0Ground * (1f - 0.5f * k), u);
                Atmo.FarBrightness = Mathf.Lerp(f0, _fog0Far * (1f - 0.55f * dim), u); Atmo.ZenithBrightness = Mathf.Lerp(z0, _fog0Zen * (1f - 0.6f * dim), u);
                yield return null;
            }
            Log($"fog (QF18): thinned round the action x{k:0.00} (distance density {Atmo.DistanceDensity:0.0000}, max {Atmo.MaxFog:0.00}, far brightness {Atmo.FarBrightness:0.00})");
        }
        Vector3 HeadOf(MarsFolk f) { if (f == null) return Vector3.zero; var an = f.Anim; var h = an != null && an.isHuman ? an.GetBoneTransform(HumanBodyBones.Head) : null; return h != null ? h.position : f.transform.position + Vector3.up * 1.7f * f.transform.lossyScale.y; }
        Vector3 HerHead => Seven.Bone(HumanBodyBones.Head) != null ? Seven.Bone(HumanBodyBones.Head).position : P7 + Vector3.up * 1.6f;
        /// <summary>QF18 (8): a close-up / medium close-up of Viper himself - low angle (from below his eyes), the harsh white
        /// light from beneath, his hood and eyes readable, the haze behind him; the first clear angle round his front.</summary>
        void ViperCloseUp(bool medium = false)
        {
            if (Viper == null) return;
            var h = HeadOf(Viper); var toHer = Flat(P7 - Viper.transform.position).normalized; if (toHer.sqrMagnitude < 0.01f) toHer = Viper.transform.forward;
            foreach (var a in new[] { 0f, 25f, -25f, 50f, -50f })
            {
                var d = Quaternion.Euler(0f, a, 0f) * toHer;
                var cam = h + d * (medium ? 3.4f : 2.3f) + Vector3.down * (medium ? 1.15f : 0.75f);
                var look = h + Vector3.down * (medium ? 0.35f : 0.12f);
                if (RideProbe.Overlap(cam, 0.25f, null, null, false)) continue;
                if (!DialogueFraming.Sees(Cam.GetComponent<Camera>(), cam, look, medium ? 36f : 28f, Viper.transform, Others(Viper.transform))) continue;
                Shoot(cam, look, medium ? 36f : 28f, 0.01f); return;
            }
            Shoot(h + toHer * 3f + Vector3.down * 0.9f, h, 34f, 0.01f);
        }
        /// <summary>QF18 (8): 7's replies - over her shoulder, looking up at him.</summary>
        void OverHerShoulderUp(Vector3 at)
        {
            var hh = HerHead; var d = Flat(at - hh).normalized; var side = Vector3.Cross(Vector3.up, d);
            foreach (var s in new[] { 1f, -1f })
            {
                var cam = hh - d * 1.15f + side * 0.55f * s + Vector3.down * 0.05f;
                if (RideProbe.Overlap(cam, 0.2f, null, null, false) || !ChapterCamera.ClearLine(cam, at)) continue;
                Shoot(cam, at, 42f, 0.01f); return;
            }
            Shoot(hh - d * 1.3f + Vector3.up * 0.2f, at, 44f, 0.01f);
        }
        Transform[] Others(Transform not) { var l = new System.Collections.Generic.List<Transform>(); foreach (var f in AllFolk()) if (f.transform != not && f.gameObject.activeInHierarchy) l.Add(f.transform); l.Add(Seven.transform); return l.ToArray(); }
        /// <summary>QF18 (12, 16): follow a power line's light with the camera (a side view travelling with the head).</summary>
        IEnumerator FollowPower(PowerLine line, int from, int to, float secs, Vector3 side, float back = 6f, float up = 2.5f)
        {
            if (line == null) yield break;
            var hum = AudioKit.Loop("mars_transport_hum", null, null, 0.25f, AudioKit.Bus.Sfx);
            Vector3 head = line.Point(from);
            Shoot(head + side * back + Vector3.up * up, head, 46f, 0.01f);
            StartCoroutine(line.Run(from, to, secs, p => head = p));
            for (float t = 0f; t < secs + 0.2f; t += Time.deltaTime) { Cam.Shot(head + side * back + Vector3.up * up, head, 0.35f, false, 46f); yield return null; }
            if (hum != null) StartCoroutine(AudioKit.Fade(hum, 0f, 0.5f, true));
        }
        static void Spin(Transform t, float deg) { if (t != null) t.localRotation = t.localRotation * Quaternion.Euler(0f, 0f, deg); }

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
            // QF18 (7): the worker gestures toward the supplies - framed on the worker WITH the transport beside them (never
            // the containers alone); the transport is a real vehicle now, nobody stands in it
            var w = Worker != null ? Worker.transform.position : L.WorkerAt; var tr = Transport != null ? Transport.position : L.TransportAt;
            if (Worker != null) { Worker.Face(tr); Worker.Play("point"); }
            { var wt = Flat(tr - w).normalized; var sd = Vector3.Cross(Vector3.up, wt); ShotClear(HeadOf(Worker) + Vector3.down * 0.25f, new[] { w - wt * 2.6f + sd * 2.4f + Vector3.up * 1.5f, w - wt * 2.6f - sd * 2.4f + Vector3.up * 1.5f, w + sd * 3.4f + Vector3.up * 1.6f, w - sd * 3.4f + Vector3.up * 1.6f }, 44f, 0.01f); }
            yield return Say("MAR_02", 5, 5);                    // Those are water filters. They're waiting for them.
            yield return Shot("m06b_the_worker_and_the_filters");
            if (Worker != null) Worker.Play("idle");
            TwoShot(mid, side, 3.6f, 1.55f, 1.4f, 44f, 0.5f);
            yield return Say("MAR_02", 6, 6);                    // I have my orders.
            // 7 looks from the supplies to the bridge
            Seven.transform.rotation = Quaternion.LookRotation(Flat(L.BarrierAt + Vector3.forward * 20f - P7));
            yield return Wait(0.6f);
            yield return Say("MAR_02", 7, 7);                    // Who gives them?
            Complete("MAR_02");
            // a low mechanical sound interrupts - LOW-ANGLE REVEAL: Viper on an elevated platform, harsh white light from beneath
            // QF18 (8): the guard steps aside first (he stood between her and the platform, so the close-ups showed a guard)
            if (Guard != null) { Guard.WalkTo(L.GuardAt + new Vector3(-1.5f, 0f, -6.5f), 1.4f); }
            AudioKit.Play("mars_platform_rise", L.PlatformAt, 1f, 0.03f, AudioKit.Bus.Sfx, 80f);
            StartCoroutine(FogThin(0.7f, 2.5f, 1f));
            if (Viper != null) { Viper.gameObject.SetActive(true); Viper.transform.SetParent(ViperPlatform, true); Viper.EyeGlow = true; }
            if (ViperPlatform != null)
            {
                var p0 = L.PlatformAt + Vector3.down * 3f; var p1 = L.PlatformAt + Vector3.up * L.PlatformTop;
                if (Viper != null) Viper.transform.SetPositionAndRotation(p0 + Vector3.up * 0.4f, Quaternion.LookRotation(Flat(P7 - L.PlatformAt)));
                if (PlatformLight != null) PlatformLight.enabled = true;
                var lowCam = L.PlatformAt - Flat(L.PlatformAt - P7).normalized * 13f + Vector3.up * 2.6f;   // QF28 pass (the gate): at 1.2 m the glowing barrier filled the lower third (23 % white)       // low angle, far enough to see him above the deck
                for (float t = 0f; t < 1f; t += Time.deltaTime / 3.6f)
                {
                    ViperPlatform.position = Vector3.Lerp(p0, p1, Mathf.SmoothStep(0f, 1f, t));
                    Cam.Shot(lowCam, ViperPlatform.position + Vector3.up * 1.9f, 0.5f, false, 46f);
                    yield return null;
                }
                if (Viper != null) Viper.Play("idle");
            }
            yield return Wait(0.4f);
            if (Viper != null) { Cam.Shot(L.PlatformAt - Flat(L.PlatformAt - P7).normalized * 7.5f + Vector3.up * 3.4f, Viper.transform.position + Vector3.up * 1.3f, 0.01f, true, 38f); /* QF30 (gate): closer and lower - he was 3.5 % of the frame at 11 m */ ImageGate.Expect(Viper.transform, "Viper on his platform, lit from beneath", 0f, "m07_"); yield return null; }   // QF29: the capture framed on him (a cut, not the eased hold)
            yield return Shot("m07_low_angle_reveal_viper_lit_from_beneath");
            var vp = Viper != null ? HeadOf(Viper) : L.PlatformAt + Vector3.up * (L.PlatformTop + 2f);
            ViperCloseUp();
            yield return Wait(0.3f);
            yield return Shot("m07b_close_up_viper_speaks");
            yield return Say("MAR_03", 1, 1);                    // Someone who understands what happens when we become careless.
            OverHerShoulderUp(vp);
            yield return Say("MAR_03", 2, 2);                    // They need those supplies.
            ViperCloseUp(true);
            yield return Say("MAR_03", 3, 3);                    // Then they can accept our terms.
            OverHerShoulderUp(vp);
            yield return Say("MAR_03", 4, 4);                    // And the temple?
            // Viper notices the geometric objects she carries; she shifts slightly, keeping them close
            ViperCloseUp();
            yield return Say("MAR_03", 5, 5);                    // What business do you have there?
            ShotClear(P7 + Vector3.up * 1.05f, new[] { P7 + Seven.transform.right * 2.1f - Seven.transform.forward * 0.4f + Vector3.up * 1.3f, P7 - Seven.transform.right * 2.1f - Seven.transform.forward * 0.4f + Vector3.up * 1.3f, P7 - Seven.transform.forward * 2.4f + Seven.transform.right * 0.8f + Vector3.up * 1.6f }, 36f, 0.4f);
            Seven.transform.rotation *= Quaternion.Euler(0f, -12f, 0f);
            yield return Wait(0.4f);
            yield return Shot("m08_she_keeps_the_records_close");
            OverHerShoulderUp(vp);
            yield return Say("MAR_03", 6, 6);                    // Historical.
            ViperCloseUp(true);
            if (Viper != null) Viper.Play("talk");
            yield return Say("MAR_03", 7, 7);                    // History is precisely the problem. (dry laugh)
            yield return Shot("m08b_close_up_history_is_the_problem");
            // he orders the crossing secured and leaves
            if (Viper != null) Viper.Play("point");
            if (Guard != null) { Guard.Face(L.BarrierAt); }
            OverHerShoulderUp(vp + Vector3.up * 0.3f);
            yield return Wait(1.4f);
            yield return Shot("m09_he_orders_the_crossing_secured");
            if (ViperPlatform != null)
            {
                var p1 = ViperPlatform.position; var p2 = L.PlatformAt + Vector3.down * 3f;
                AudioKit.Play("mars_platform_rise", L.PlatformAt, 0.8f, 0.03f, AudioKit.Bus.Sfx, 80f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 2.6f) { ViperPlatform.position = Vector3.Lerp(p1, p2, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
                if (PlatformLight != null) PlatformLight.enabled = false;
            }
            if (Viper != null) { Viper.transform.SetParent(null, true); Viper.gameObject.SetActive(false); Viper.EyeGlow = false; }
            StartCoroutine(FogThin(0f, 2f));
            Complete("MAR_03");
            // privately (Panther) - then QF18 (9): the worker tells her about the old route; Panther sees its entrance; the shot
            // of the entrance; only then the objective names it
            yield return Say("MAR_03", 8, 10);                    // Charming. / He noticed the records. / I noticed him noticing.
            Objective("Find another way across");
            if (Guard != null) Guard.WalkTo(L.GuardAt + new Vector3(-1.5f, 0f, -6.5f), 1.2f);
            if (Worker != null) { Worker.WalkTo(P7 + Flat(Seven.transform.forward).normalized * 1.6f + Seven.transform.right * 0.6f, 1.3f); }
            yield return Wait(1.6f);
            if (Worker != null) { Worker.Face(P7); Seven.transform.rotation = Quaternion.LookRotation(Flat(Worker.transform.position - P7)); }
            { var wq = Worker != null ? Worker.transform.position : P7 + Vector3.right; var md = (P7 + wq) * 0.5f; var sd = Vector3.Cross(Vector3.up, Flat(wq - P7).normalized); TwoShot(md, sd, 3.0f, 1.5f, 1.4f, 42f, 0.5f); }
            yield return Say("MAR_03", 11, 14);                  // There's an old maintenance route ... / Can the transport get through? / Not anymore... / Then I'll take a look.
            { // QF29: the capture's two-shot from a side clear BOTH ways (0.18.3's was inside the bank wall: a flat grey frame)
              var wq = Worker != null ? Worker.transform.position : P7 + Vector3.right; var md = (P7 + wq) * 0.5f + Vector3.up * 1.3f; var sd = Vector3.Cross(Vector3.up, Flat(wq - P7).normalized);
              foreach (var c in new[] { md + sd * 3.2f + Vector3.up * 0.3f, md - sd * 3.2f + Vector3.up * 0.3f, md + sd * 2.2f + Vector3.up * 1.2f, md - sd * 2.2f + Vector3.up * 1.2f })
                  if (ChapterCamera.ClearLine(c, md) && ChapterCamera.ClearLine(md, c) && !Physics.CheckSphere(c, 0.3f, ~0, QueryTriggerInteraction.Ignore)) { Shoot(c, md, 44f, 0.01f); break; }
              yield return Wait(0.2f); }
            yield return Shot("m09b_the_worker_tells_her_about_the_old_route");
            // Panther sees it from the saucer: the entrance - the old ramp cut into the bank, the half-buried gateway
            Shoot(new Vector3(-46.5f, 6.5f, -36f), new Vector3(-55.5f, -2.5f, -14f), 48f, 0.01f);
            yield return Say("MAR_03", 15, 15);                  // I see it from here. A ramp cut into the bank, east of you.
            ImageGate.ExpectPoint(new Vector3(-55.5f, -0.5f, -14f), "the route entrance in the bank", 0f, "m09c_");   // QF26 (2)
            yield return Shot("m09c_the_route_entrance_shown");
            yield return Cutscene(false);
            if (Worker != null) Worker.WalkTo(L.WorkerAt, 1.2f);            // back to the transport
            Objective("Take the old maintenance route down into the ravine");
        }

        // ------------------------------------------------------------------------------------------ 4: both sides of the ravine
        IEnumerator Maintenance()
        {
            FreeBike = true;
            if (!Riding) { Objective("Get back on the bike (X)"); ShowGuide(Bike.transform.position); if (Auto) { yield return WalkSeven(Bike.transform.position - Bike.transform.right * 1.1f, 10f); PlaceRiding(Bike.transform.position); } yield return Until(() => Riding, "on the bike", 40f, "Walk to the bike and press X", Bike.transform.position, () => PlaceRiding(Bike.transform.position)); HideGuide(); }
            Objective("Take the old maintenance route down into the ravine");
            yield return RideRoute(new[] { L.RampEntryW, L.Maint[0], L.Maint[1], L.Maint[2], L.Maint[3], L.CausewayStop }, "down the maintenance route", 7f);
            yield return Shot("m10_the_ravine_floor_the_raised_span");
            // QF18 (11): the drawbridge - raised and chained up during the division (show it: the span, the chain, the lock,
            // the kingdom's barred order plaque), then she breaks the lock with telekinesis and the span comes down
            yield return StopBike(2f);
            yield return Cutscene(true);
            Shoot(L.CausewayStop + new Vector3(-6f, 3.2f, 9f), L.DrawHinge + new Vector3(2.5f, 7f, 0f), 50f, 0.01f);
            yield return Wait(1.6f);
            yield return Shot("m10a_the_span_chained_up");
            Shoot(L.DrawTower + new Vector3(5.5f, 2.4f, 4.2f), L.DrawTower + new Vector3(2.0f, 2.2f, 1.6f), 40f, 0.6f);
            yield return Wait(1.8f);
            ImageGate.ExpectPoint(L.DrawTower + new Vector3(2.0f, 2.2f, 1.6f), "the lock and the order plaque", 0f, "m10b_");   // QF26 (2)
            yield return Shot("m10b_the_lock_and_the_order_plaque");
            yield return Cutscene(false);
            Objective("One kingdom raised and chained the span - break the lock on its winding drum (get off, RT)");
            yield return Telekinesis(L.DrawLock, L.SlabTkRange, "break the drawbridge lock (telekinesis)", "Get off the bike (X), look at the heavy lock on the drum at the tower's foot and use telekinesis (RT)", CausewayEdges, new[] { L.CausewayStop + new Vector3(-1f, 0f, -3f) });
            yield return LowerBridge();
            SetZone("Causeway", true);
            Objective("Ride across and on along the ravine floor");
            if (!Riding) { ShowGuide(Bike.transform.position); if (Auto) { yield return WalkSeven(Bike.transform.position - Bike.transform.right * 1.1f, 10f); PlaceRiding(Bike.transform.position); } yield return Until(() => Riding, "back on the bike", 40f, "Walk to the bike and press X", Bike.transform.position, () => PlaceRiding(Bike.transform.position)); HideGuide(); }
            // broad riding sections, a sheltered passage, a lookout to stop at (optional), up the east ramp to the gate
            yield return RideRoute(new[] { L.Maint[5], L.Maint[6], L.Maint[7], new Vector3(53f, L.FloorY, -66f), L.Maint[8], L.Maint[9], new Vector3(55.2f, -1f, -16f) }, "across the floor and up the east wall", 8f);
            yield return StopBike(1.5f);
            yield return Shot("m11_the_sheltered_passage_and_the_east_ramp");
            // QF18 (12): the gate is dead - show why: its seam lights dark, its power line runs down the ramp's wall to a cut
            yield return Cutscene(true);
            Shoot(L.ServiceGateAt + new Vector3(-3.5f, 2.2f, -9f), L.ServiceGateAt + Vector3.up * 2.4f, 48f, 0.01f);
            AudioKit.Play("mars_machinery", L.ServiceGateAt, 0.4f, 0.1f, AudioKit.Bus.Sfx, 40f);
            yield return Wait(1.3f);
            yield return Shot("m11a_the_gate_dead");
            if (GateLine != null)
            {
                // the camera travels down the dead line to the break: the two severed ends hang apart, the feed below sparks
                var a = GateLine.Point(GateLine.Count - 1); var b = GateLine.Point(0);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 2.6f) { var p = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t)); Cam.Shot(p + new Vector3(-4.2f, 1.2f, -1.5f), p, 0.3f, false, 46f); yield return null; }
                if (GateFeed != null) StartCoroutine(GateFeed.Flicker(GateFeed.Count - 1, 2.4f));
                // P2 (gate): pulled back and up so the ramp under the break and the wall it runs along frame the two ends (the close
                // framing showed two cable pieces floating in flat haze - nothing to place them)
                StartCoroutine(FogThin(0.5f, 0.6f, 0f));                       // thinner near the cut, the bright haze beyond kept
                // along the wall, not at it (facing it, its face filled the frame - range 0.07), looking up the ramp: the dressed wall and the
                // cable at one side, the break ahead, the ramp's bright top and the sky over the gate beyond it (range from the light above)
                Shoot(L.GateBreak + new Vector3(-2.2f, 0.4f, -9.5f), L.GateBreak + new Vector3(-0.8f, 1.6f, 5f), 58f, 0.01f);
                yield return Wait(1.6f);
                ImageGate.ExpectPoint(L.GateBreak, "the cut power line");
                yield return Shot("m11b_the_cut_power_line");
                StartCoroutine(FogThin(0f, 1.5f));
            }
            yield return Cutscene(false);
            Objective("The gate's power line was cut - follow it down the ramp to the break");
            if (GateFeed != null) GateFeed.Set(GateFeed.Count - 1, 0.7f);
            ShowGuide(L.GateBreakView);
            if (Auto) { yield return GetOff(); StartCoroutine(WalkSeven(L.GateBreakView, 30f)); }
            yield return Until(() => Flat(P7 - L.GateBreakView).magnitude < 5f, "to the break in the gate's power line", 60f, "Down the ramp along the wall - where the cable hangs apart", L.GateBreakView, () => { if (Riding) Bike.Dismount(true); Seven.Place(L.GateBreakView, Vector3.right); });
            HideGuide();
            Objective("Pull the two cut ends together with telekinesis and hold them (hold RT)");
            yield return HoldTogether();
            // the payoff: the power runs up the wall, the gate's seam lights, it grinds open - and the transport comes through
            yield return OpenServiceGate();
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

        /// <summary>QF18 (11): the lock shatters, the drum runs free, the chain pays out and the span swings down across the
        /// channel - heavy, rumbling, dust. Its collision copy follows (SyncAll) and the causeway zone opens.</summary>
        IEnumerator LowerBridge()
        {
            yield return Cutscene(true);
            var db = Drawbridge;
            Shoot(L.CausewayStop + new Vector3(-4f, 2.8f, 10f), L.DrawHinge + new Vector3(4f, 3f, 0f), 54f, 0.6f);
            // the lock breaks: three pieces fly off the drum
            if (db != null && db.Lock != null)
            {
                AudioKit.Play("stone_fall", db.Lock.position, 1f, 0.04f); AudioKit.Play("fight_block", db.Lock.position, 1f, 0.04f);
                var pieces = new System.Collections.Generic.List<(Transform t, Vector3 v)>();
                foreach (Transform p in db.Lock) pieces.Add((p, new Vector3(Random.Range(-2f, 2f), Random.Range(2.5f, 4f), Random.Range(0.5f, 3f))));
                for (float t = 0f; t < 0.9f; t += Time.deltaTime)
                {
                    foreach (var (p, v) in pieces) { p.position += (v + Vector3.down * 9.8f * t) * Time.deltaTime; p.Rotate(500f * Time.deltaTime, 300f * Time.deltaTime, 0f); }
                    yield return null;
                }
                foreach (var (p, _) in pieces) p.gameObject.SetActive(false);
            }
            AudioKit.Play("mars_machinery", L.DrawTower, 1f, 0.03f, AudioKit.Bus.Sfx, 80f);
            var rattle = AudioKit.Loop("mars_section_slide", null, null, 0.8f, AudioKit.Bus.Sfx);
            bool s = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 3.4f)
            {
                float u = t * t * (1.6f - 0.6f * t);                                       // slow to start, heavy at the end
                db?.Set(Mathf.Clamp01(u));
                if (!s && t > 0.55f) { s = true; StartCoroutine(Shot("m10c_the_span_comes_down")); }
                yield return null;
            }
            if (rattle != null) StartCoroutine(AudioKit.Fade(rattle, 0f, 0.3f, true));
            db?.Set(1f);
            // it lands: a bounce, a heavy boom, dust along the channel
            AudioKit.Play("land_hard", L.CausewayGap, 1f, 0f, AudioKit.Bus.Sfx, 120f); AudioKit.Play("mars_section_slide", L.CausewayGap, 1f, 0.05f, AudioKit.Bus.Sfx, 60f);
            CameraShake.Quake(0.7f, 0.35f);
            for (int k = 0; k < 9; k++)
            {
                var g = new GameObject("BridgeDust"); g.AddComponent<MeshFilter>().sharedMesh = Markers.Rock(500 + k, Vector3.one * 0.5f);
                var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = Markers.Additive(0.22f); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                g.transform.position = L.CausewayGap + new Vector3(Random.Range(-7f, 7f), 0.3f, Random.Range(-3.5f, 3.5f));
                g.AddComponent<SmokePuff>().Life = 2.4f + k * 0.15f;
            }
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { db?.Set(1f - Mathf.Sin(t / 0.5f * Mathf.PI) * 0.02f); yield return null; }
            db?.Set(1f);
            CameraOccluders.SyncAll();
            yield return Wait(0.8f);
            yield return Shot("m10d_the_span_across_the_channel");
            Log("the drawbridge is down across the channel (Causeway zone open) - the lock broken, nobody hurt");
            ZFightProbe.Report("the lowered drawbridge", db != null ? db.Span.gameObject : null);
            yield return Cutscene(false);
        }

        /// <summary>QF18 (12): hold RT - the two cut ends are pulled together while sparks arc; letting go lets them drift
        /// apart again. A little real play with a visible payoff.</summary>
        IEnumerator HoldTogether()
        {
            var joint = L.GateBreak;
            var up0 = CableEndUpper != null ? CableEndUpper.rotation : Quaternion.identity; var lo0 = CableEndLower != null ? CableEndLower.rotation : Quaternion.identity;
            float k = 0f, need = 1.8f; bool done = false; float spark = 0f;
            var hum = AudioKit.Loop("laser_hum", null, null, 0f, AudioKit.Bus.Sfx);
            ShowGuide(joint);
            while (!done)
            {
                bool inReach = Flat(joint - P7).magnitude < 9f && !Riding;
                bool held = inReach && (Auto || Input.Telekinesis.IsPressed());
                if (!_cut) SetPrompt(inReach ? "Hold: pull the ends together" : Riding && Bike.Speed < 1.2f ? "Get off" : null, inReach ? ControlsLegend.Btn.Tk : ControlsLegend.Btn.Interact);
                if (!inReach && Riding && Bike.Speed < 1.2f && !Auto && Input.Interact.WasPressedThisFrame()) { if (Bike.Dismount()) FollowCamera(); }
                k = Mathf.MoveTowards(k, held ? 1f : 0f, Time.deltaTime / (held ? need : 1.2f));
                if (held) { Seven.Gesture(joint); Seven.SetEnergyPoint(1.6f + k, 0.1f); }
                // the ends swing toward the joint as k rises
                void Aim(Transform e, Quaternion r0) { if (e == null) return; var tipLocal = e.childCount > 1 ? e.GetChild(1).localPosition : Vector3.down; var want = Quaternion.FromToRotation(r0 * tipLocal.normalized, (joint - e.position).normalized) * r0; e.rotation = Quaternion.Slerp(r0, want, Mathf.SmoothStep(0f, 1f, k)); }
                Aim(CableEndUpper, up0); Aim(CableEndLower, lo0);
                // sparks arc across the gap
                spark -= Time.deltaTime;
                if (k > 0.25f && spark <= 0f) { spark = Random.Range(0.06f, 0.2f); AudioKit.Play("fight_block", joint, 0.25f + 0.4f * k, 0.2f, AudioKit.Bus.Sfx, 25f); if (GateFeed != null) StartCoroutine(GateFeed.Flicker(GateFeed.Count - 1, 0.12f)); if (GateLine != null) StartCoroutine(GateLine.Flicker(0, 0.12f)); }
                if (hum != null) AudioKit.SetBaseVolume(hum, 0.5f * k);
                if (k >= 1f) done = true;
                if (Auto && Time.time > 0f) { /* the check run holds until it joins */ }
                yield return null;
            }
            SetPrompt(null); HideGuide();
            if (hum != null) StartCoroutine(AudioKit.Fade(hum, 0f, 0.4f, true));
            AudioKit.Play("mars_control_on", joint, 1f, 0.03f, AudioKit.Bus.Sfx, 40f);
            Seven.SetEnergyPoint(1f, 0.8f);
            Log("the gate's two cut cable ends joined with telekinesis (held)");
        }

        IEnumerator OpenServiceGate()
        {
            if (ServiceGate == null) { SetZone("ServiceGateZ", true); yield break; }
            yield return Cutscene(true);
            // the power runs back up the wall in a visible glow, all the way to the gate
            if (GateFeed != null) GateFeed.All(true);
            yield return FollowPower(GateLine, 0, GateLine != null ? GateLine.Count - 1 : 0, 2.4f, new Vector3(-1f, 0f, 0f), 5.5f, 1.6f);
            Glow(ServicePanel, 1.6f);
            var seam = ServiceGate.Find("GateSeam"); if (seam != null) Glow(seam.GetComponent<Renderer>(), 2.4f);
            Shoot(L.ServiceGateAt + new Vector3(-3.5f, 2.2f, -10f), L.ServiceGateAt + Vector3.up * 2f, 48f, 0.01f);
            AudioKit.Play("mars_machinery", L.ServiceGateAt, 1f, 0.03f, AudioKit.Bus.Sfx, 60f);
            yield return Wait(0.5f);
            var p0 = ServiceGate.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.6f) { ServiceGate.position = p0 + Vector3.down * 4.8f * Mathf.SmoothStep(0f, 1f, t) + Vector3.right * Mathf.Sin(t * 60f) * 0.01f; yield return null; }
            ServiceGate.gameObject.SetActive(false);
            SetZone("ServiceGateZ", true);
            CameraOccluders.SyncAll();
            yield return Shot("m11c_the_service_gate_open");
            yield return Cutscene(false);
        }

        /// <summary>The supply transport (its two drivers walking beside it) comes up the ramp and through the gate she opened;
        /// a driver raises a hand to her (the payoff).</summary>
        IEnumerator TransportThrough()
        {
            if (Transport == null) yield break;
            yield return Cutscene(true);
            yield return UI.Fade(1f, 0.5f);
            // a short time later: it has come down the west ramp and across the floor - the last stretch up and through the gate
            Seven.Place(new Vector3(52.6f, 0f, -24f), Vector3.right); Seven.Lock(true);
            if (Riding) Bike.DismountTo(new Vector3(52.6f, 0f, -24f), Vector3.right);
            Bike.Place(new Vector3(52.6f, 0f, -20f), 0f);
            var path = new[] { new Vector3(57f, L.FloorY, -64f), new Vector3(57f, -10f, -34f), new Vector3(56f, 0f, -14f), L.CaretakerAt + new Vector3(4f, 0f, -6f) };
            Transport.position = path[0]; Transport.gameObject.SetActive(true);
            var hum = AudioKit.Loop("mars_transport_hum", Transport, null, 0.6f, AudioKit.Bus.Sfx, 4f, 60f);
            var camAt = new Vector3(51.8f, Ground.Height(51.8f, -18f) + 2.2f, -18f);
            Shoot(camAt, path[1] + Vector3.up, 50f, 0.01f);
            yield return UI.Fade(0f, 0.5f);
            bool s = false, waved = false;
            for (int i = 0; i < path.Length - 1; i++)
            {
                var a = path[i]; var b = path[i + 1]; float len = Vector3.Distance(a, b);
                for (float d = 0f; d < len; d += 6f * Time.deltaTime)
                {
                    var p = Vector3.Lerp(a, b, d / len); p.y = Ground.Height(p.x, p.z);
                    Transport.SetPositionAndRotation(p, Quaternion.Slerp(Transport.rotation, Quaternion.LookRotation(Flat(b - a)), Time.deltaTime * 3f));
                    if (Drivers != null) for (int k = 0; k < Drivers.Length; k++) if (Drivers[k] != null) { Drivers[k].gameObject.SetActive(true); var dp = p - Transport.forward * (2.6f + k * 1.2f) + Transport.right * (k == 0 ? -1.9f : 1.9f); dp.y = Ground.Height(dp.x, dp.z); Drivers[k].transform.SetPositionAndRotation(dp, Transport.rotation); if (!(waved && k == 0)) Drivers[k].Play("walk"); }
                    // as it passes her, the driver raises a hand to her
                    if (!waved && Drivers != null && Drivers.Length > 0 && Drivers[0] != null && Flat(Drivers[0].transform.position - P7).magnitude < 6f) { waved = true; Drivers[0].Face(P7); Drivers[0].Play("wave", 0.2f); StartCoroutine(Shot("m11d_the_driver_raises_a_hand")); }
                    Cam.Shot(camAt, p + Vector3.up * 1.5f, 1.0f, false, 50f);
                    if (!s && i == 2) { s = true; StartCoroutine(Shot("m11e_the_transport_comes_through_the_gate")); }
                    yield return null;
                }
            }
            if (Drivers != null) foreach (var d in Drivers) if (d != null) d.Play("idle");
            if (hum != null) StartCoroutine(AudioKit.Fade(hum, 0f, 1.5f, true));
            Log($"the supply transport came up the ramp and through the gate she opened (a driver waved: {waved})");
            Seven.Place(L.EastStop + new Vector3(-1.5f, 0f, -2f), Vector3.right);
            Bike.Place(L.EastStop + new Vector3(-3f, 0f, -3f), 0f);
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
            if (Caretaker != null) Caretaker.Place(L.MechanismAt + new Vector3(2.8f, 0f, -2.2f), L.MechanismAt - (L.MechanismAt + new Vector3(2.8f, 0f, -2.2f)));
            // OVER 7's SHOULDER - the mechanism; the channel leads into it
            var head = P7 + Vector3.up * 1.6f; var f = Flat(L.MechanismAt - P7).normalized;
            Shoot(head - f * 1.1f + Vector3.Cross(Vector3.up, f) * 0.55f + Vector3.up * 0.2f, L.MechanismAt + Vector3.up * 2.0f, 42f, 0.01f);
            if (ChannelE != null) ChannelE.Set(-1); if (ChannelWA != null) ChannelWA.Set(-1); if (ChannelWB != null) ChannelWB.Set(-1);
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
            var back = new[] { new Vector3(64f, 0f, 60f), L.RampEntryE, new Vector3(55.2f, -1f, -16f), L.Maint[9], L.Maint[8], L.Maint[6], L.Maint[5], L.Maint[4], L.Maint[3], L.Maint[2], L.Maint[1], L.Maint[0], L.RampEntryW, new Vector3(-62f, 0f, -6f) };   // QF18: through the gateways, never across their piers
            yield return RideRoute(back, "back to the west checkpoint", 8f);
            // QF18 (14): the worker volunteers - spoken, not mimed
            Objective("Talk to the worker by the checkpoint (X)");
            if (Worker != null) Worker.Place(L.WorkerAt, Vector3.right);
            yield return Interact(L.WorkerAt + Vector3.right * 1.4f, "Talk", "talk to the worker", "The worker who was with the transport - walk up and press X", null);
            yield return Cutscene(true);
            var w = Worker != null ? Worker.transform.position : L.WorkerAt;
            if (Worker != null) Worker.Face(P7);
            Seven.transform.rotation = Quaternion.LookRotation(Flat(w - P7));
            var mid = (P7 + w) * 0.5f; var side = Vector3.Cross(Vector3.up, Flat(w - P7).normalized);
            TwoShot(mid, side, 3.2f, 1.5f, 1.4f, 44f, 0.6f);
            yield return SayIds("MAR_06_V1", "MAR_06_V2", "MAR_06_V3");   // The service route's open ... / The caretaker? ... / They're waiting ...
            if (Worker != null && Guard != null) { Worker.Face(Guard.transform.position); yield return Wait(0.7f); Worker.Face(P7); }   // a glance at the guard first
            yield return SayIds("MAR_06_V4");                    // Then I'll go. Someone should.
            if (Worker != null) Worker.Play("wave");
            yield return SayIds("MAR_06_V5", "MAR_06_V6");       // Thank you. / Two volunteers. Practically a treaty.
            yield return Shot("m16_the_worker_volunteers");
            yield return Cutscene(false);
            // QF18 (16): the power chain, shown before she acts - the worker's control is dark; its dark channel runs back
            // along the rim to the relay
            if (Worker != null) Worker.Place(L.WestControl + new Vector3(-1.6f, 0f, 0f), Vector3.right);
            Glow(ControlW, 0.12f); Glow(ControlE, 0.12f);
            if (ChannelWA != null) ChannelWA.Set(-1); if (ChannelWB != null) ChannelWB.Set(-1);
            yield return Cutscene(true);
            yield return UI.Fade(1f, 0.4f); yield return UI.Fade(0f, 0.4f);
            Shoot(L.WestControl + new Vector3(-5.5f, 2.4f, -3.5f), L.WestControl + Vector3.up * 1.2f, 44f, 0.01f);
            yield return Wait(1.4f);
            yield return Shot("m16b_the_workers_control_is_dark");
            if (ChannelWA != null)
            {
                for (float t = 0f; t < 1f; t += Time.deltaTime / 3.2f) { var p = ChannelWA.Point(Mathf.RoundToInt(Mathf.Lerp(ChannelWA.Count - 1, 0, Mathf.SmoothStep(0f, 1f, t)))); Cam.Shot(p + new Vector3(-6f, 4f, 2f), p, 0.6f, false, 48f); yield return null; }
                yield return Shot("m16c_its_channel_leads_to_the_relay");
            }
            yield return Cutscene(false);
            // 1. ride north along the west rim to the relay; its crystal is knocked askew - seat it (X)
            Objective("The control has no power - ride along the rim to the relay its channel comes from and reset it (X)");
            yield return RideRoute(new[] { new Vector3(-62f, 0f, 20f), new Vector3(-66f, 0f, 70f), L.RelayWest + new Vector3(0f, 0f, -4f) }, "to the relay on the west rim", 7f);
            yield return Interact(L.RelayWest + new Vector3(-1.5f, 0f, -0.9f), "Reset the crystal", "reset the relay crystal", "Stop by the relay pillar on the rim and press X", new[] { L.RelayWest + new Vector3(-1.6f, 0f, -1.3f) });
            yield return Cutscene(true);
            Seven.Gesture(L.RelayWest + Vector3.up * 2.8f);
            if (RelayCrystal != null) { var r0 = RelayCrystal.rotation; for (float t = 0f; t < 1f; t += Time.deltaTime / 0.7f) { RelayCrystal.rotation = Quaternion.Slerp(r0, Quaternion.identity, Mathf.SmoothStep(0f, 1f, t)); yield return null; } }
            Glow(RelayW, 2.4f);
            AudioKit.Play("mars_control_on", L.RelayWest, 0.9f, 0.03f, AudioKit.Bus.Sfx, 40f);
            yield return Shot("m17a_the_relay_crystal_seated");
            // the light runs along the channel - and stops under the old bridge, where the conduit is knocked loose, sparking
            yield return FollowPower(ChannelWA, 0, ChannelWA != null ? ChannelWA.Count - 1 : 0, 4.2f, new Vector3(-0.6f, 0f, -0.8f).normalized, 7f, 3f);
            Shoot(new Vector3(-43.2f, -2.6f, 155.5f), (L.PierConduitRest + L.PierConduit) * 0.5f, 44f, 0.01f);   // from over the ravine, the walk and the knocked-out section in view
            if (ChannelWA != null) StartCoroutine(ChannelWA.Flicker(ChannelWA.Count - 1, 3.5f));
            yield return Wait(0.6f);
            yield return Shot("m17b_the_power_stops_under_the_bridge");
            yield return SayIds("MAR_06_P1");                    // The power stops under the bridge. Something down there's knocked loose.
            if (ChannelWA != null) ChannelWA.Set(ChannelWA.Count - 1);
            yield return Cutscene(false);
            // 2. on foot down the carved steps, the ledge walk under the span; the section lifted back with telekinesis
            Objective("Take the steps down beside the old bridge and lift the loose conduit back into place (RT)");
            if (Riding) yield return GetOff();
            yield return GuideWalk(new[] { new Vector3(-60f, 0f, 150f), new Vector3(-57f, 0f, 165f), L.PierWalkTop, new Vector3(-48.8f, 0f, L.PierStairZ1 + 1f), new Vector3(-48.8f, -4f, L.PierStairZ0 - 0.6f), L.PierConduitView }, "down the pier steps");
            yield return Telekinesis(L.PierConduitRest, 7f, "the pier conduit (telekinesis)", "On the ledge walk under the bridge: look at the loose conduit section and use telekinesis (RT)", null, new[] { L.PierConduitView });
            yield return PushConduit();
            // back up the steps to the worker's control (the light leads the way)
            yield return GuideWalk(new[] { new Vector3(-48.8f, -4f, L.PierStairZ0 - 0.4f), new Vector3(-48.8f, 0f, L.PierStairZ1 + 1f), L.PierWalkTop, new Vector3(-57f, 0f, 165f), L.WestControl + new Vector3(-5f, 0f, 2f) }, "back up the pier steps");
            // 3. Viper's sentries go for the worker's control - knock them aside (nonlethal: machines)
            yield return Jammers3();
            // 4. the hold - shown: the caretaker raises a lamp at the far control, the worker holds, a section swings out
            yield return HoldAndSwing();
            // the two inhabitants activate their controls together - WIDE: the ancient structures align into a passage
            yield return Align();
            Complete("MAR_06");
            FreeBike = false;
        }
        IEnumerator SayIds(params string[] ids) { var ls = MarsDialogue.Ids(ids); if (ls.Count == 0) yield break; yield return Dlg.PlayLines(ls, false); }

        IEnumerator PushConduit()
        {
            if (Conduit == null) yield break;
            yield return Cutscene(true);
            Shoot(L.PierConduitView + new Vector3(1.6f, 1.4f, 3.6f), L.PierConduit, 46f, 0.01f);
            AudioKit.Play("slab_lift", Conduit.position, 0.8f, 0.05f, AudioKit.Bus.Sfx, 30f);
            var a = Conduit.position; var r0 = Conduit.rotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.6f) { float u = Mathf.SmoothStep(0f, 1f, t); Conduit.SetPositionAndRotation(Vector3.Lerp(a, L.PierConduit, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.5f, Quaternion.Slerp(r0, Quaternion.identity, u)); yield return null; }
            Conduit.SetPositionAndRotation(L.PierConduit, Quaternion.identity);
            AudioKit.Play("mars_control_on", L.PierConduit, 0.8f, 0.03f, AudioKit.Bus.Sfx, 40f);
            Glow(Conduit.Find("ConduitCore") != null ? Conduit.Find("ConduitCore").GetComponent<Renderer>() : null, 1.6f);
            yield return Shot("m17_the_conduit_back_in_place");
            // the light runs through it, along the wall and up to the worker's control - which lights up
            yield return FollowPower(ChannelWB, 0, ChannelWB != null ? ChannelWB.Count - 1 : 0, 3.0f, new Vector3(1f, 0.2f, -0.6f).normalized, 7f, 2.5f);
            Glow(ControlW, 1.4f);
            Shoot(L.WestControl + new Vector3(-4.5f, 2.2f, -3.5f), L.WestControl + Vector3.up * 1.2f, 44f, 0.01f);
            if (Worker != null) { Worker.Face(L.PierConduit); Worker.Play("wave"); }
            AudioKit.Play("mars_control_on", L.WestControl, 0.9f, 0.03f, AudioKit.Bus.Sfx, 60f);
            yield return Wait(1.2f);
            yield return Shot("m17c_the_workers_control_lights_up");
            Log("the pier conduit is back in place - the light ran through it to the west control, which lit up");
            yield return Cutscene(false);
        }

        IEnumerator Jammers3()
        {
            if (Jammers == null || Jammers.Length == 0) yield break;
            var target = L.WestControl + Vector3.up * 1.4f;
            // QF18 (17): introduced - Viper's voice from his loudspeaker mast, then his sentries drop in
            yield return Cutscene(true);
            StartCoroutine(FogThin(0.75f, 1.5f));
            var hornLight = ViperHorn != null ? ViperHorn.GetComponentInChildren<Light>() : null;
            Shoot(L.ViperHornAt + new Vector3(4.5f, 2.2f, -4.5f), L.ViperHornAt + Vector3.up * 4.2f, 40f, 0.01f);
            IEnumerator HornPulse(float s) { for (float t = 0f; t < s; t += Time.deltaTime) { if (hornLight != null) hornLight.intensity = 2.5f + 2f * Mathf.Abs(Mathf.Sin(t * 14f)); yield return null; } if (hornLight != null) hornLight.intensity = 0f; }
            AudioKit.Play("signal_pulse", L.ViperHornAt + Vector3.up * 4f, 0.8f, 0.05f, AudioKit.Bus.Sfx, 60f);
            StartCoroutine(HornPulse(4.2f));
            yield return SayIds("MAR_06_D1");                    // You've been busy. Let's see how you manage several things at once.
            yield return Shot("m18a_viper_over_the_loudspeaker");
            var sentries = new System.Collections.Generic.List<MarsSentry>();
            for (int i = 0; i < Jammers.Length; i++) { var s = Jammers[i] != null ? Jammers[i].GetComponent<MarsSentry>() : null; if (s == null) continue; s.Launch(L.JammerFrom[i % L.JammerFrom.Length], target + new Vector3((i - 1) * 0.9f, 0f, 0f)); sentries.Add(s); }
            Shoot(L.WestControl + new Vector3(-8f, 3.5f, -9f), (L.JammerFrom[0] + L.WestControl) * 0.5f + Vector3.up * 3f, 54f, 0.01f);
            yield return Wait(1.2f);
            yield return Shot("m18b_his_sentries_drop_in");
            if (Worker != null) { Worker.Face(L.JammerFrom[0]); Worker.Play("point"); }
            { var wq = Worker != null ? Worker.transform.position : L.WestControl; var md = (P7 + wq) * 0.5f; TwoShot(md, Vector3.Cross(Vector3.up, Flat(wq - P7).normalized), 4f, 1.4f, 1.5f, 48f, 0.01f); }
            yield return SayIds("MAR_06_D2", "MAR_06_D3", "MAR_06_D4");   // Those are his sentries ... / Three of them ... / Not if I can help it.
            yield return Cutscene(false);
            Objective("Viper's sentries are attacking the worker's control - knock them aside with telekinesis (RT)  ·  0 / 3");
            if (Auto) StartCoroutine(WalkPath(new[] { L.WestControl + new Vector3(-6f, 0f, -4f) }, 30f));
            float wear = 0f; bool failing = false; int down = 0;
            while (down < sentries.Count)
            {
                // the nearest one still flying
                MarsSentry next = null; float bd = 1e9f;
                foreach (var s in sentries) if (!s.Out) { float d = Flat(s.transform.position - P7).magnitude; if (d < bd) { bd = d; next = s; } }
                if (next == null) break;
                var nt = next;
                bool hit = false;
                IEnumerator Pressure() { while (!hit) { float w2 = 0f; foreach (var s in sentries) if (!s.Out && s.Attacking) w2 += Time.deltaTime; wear += w2; if (!failing && wear > 9f) { failing = true; UI.Hint("The control is failing - knock the sentries off it!", 4f); Log("the sentries' attack is wearing the control down (pressure)"); } Glow(ControlW, failing ? (Random.value < 0.5f ? 0.2f : 1.2f) : 1.4f); yield return null; } }
                StartCoroutine(Pressure());
                yield return Telekinesis(nt.transform.position, 24f, $"knock a sentry aside ({down + 1}/{sentries.Count})", "Look at a hovering sentry (the white searchlight) and use telekinesis (RT)", null, null, true, nt.transform);
                hit = true;
                StartCoroutine(nt.KnockAside(P7, (x, z) => Ground.Height(x, z)));
                down++;
                Objective($"Viper's sentries are attacking the worker's control - knock them aside with telekinesis (RT)  ·  {down} / {sentries.Count}");
                if (down == 1) { yield return Wait(0.6f); yield return Shot("m18_a_sentry_knocked_aside"); StartCoroutine(HornPulse(2.4f)); yield return SayIds("MAR_06_D5"); }   // Every bridge has a weak point.
            }
            Glow(ControlW, 1.4f);
            yield return Wait(0.8f);
            StartCoroutine(HornPulse(2.4f));
            yield return SayIds("MAR_06_D6");                    // Enjoy it. Bridges fall.
            StartCoroutine(FogThin(0f, 2.5f));
            Log($"protect the repair: {sentries.Count} of Viper's sentries knocked aside (machines - nobody hurt); the control took {wear:0.0} s of attack");
        }

        /// <summary>QF18 (18): the hold - every beat shown: the caretaker raises a lamp at the far control; she tells the
        /// worker beside her; the power surges, a span section swings out; she catches it; "Now!".</summary>
        IEnumerator HoldAndSwing()
        {
            yield return Cutscene(true);
            if (Worker != null) { Worker.Place(L.WestControl + new Vector3(-1.6f, 0f, 0f), Vector3.right); Worker.Play("idle"); }
            Seven.Place(L.WestControl + new Vector3(-3.2f, 0f, -2.4f), Flat(L.WestControl - (L.WestControl + new Vector3(-3.2f, 0f, -2.4f)))); Seven.Lock(true);
            // the far bank: the caretaker at their control raises a lamp
            Light lamp = null;
            if (Caretaker != null)
            {
                Caretaker.Place(L.EastControl + new Vector3(1.6f, 0f, 0f), Vector3.left); Caretaker.Play("wave", 0.2f);
                lamp = new GameObject("CaretakerLamp").AddComponent<Light>(); lamp.type = LightType.Point; lamp.color = Color.white; lamp.range = 10f; lamp.intensity = 0f; lamp.shadows = LightShadows.None;
            }
            Shoot(L.EastControl + new Vector3(-6.5f, 2.6f, -4.5f), L.EastControl + new Vector3(1.6f, 1.9f, 0f), 40f, 0.01f);
            var say = StartCoroutine(SayIds("MAR_06_H1"));       // The caretaker's at the far control. They've raised a lamp - they're ready.
            for (float t = 0f; t < 2.2f; t += Time.deltaTime)
            {
                if (lamp != null && Caretaker != null) { var an = Caretaker.Anim; var hand = an != null && an.isHuman ? an.GetBoneTransform(HumanBodyBones.RightHand) : null; lamp.transform.position = (hand != null ? hand.position : Caretaker.transform.position + Vector3.up * 2.2f) + Vector3.up * 0.15f; lamp.intensity = Mathf.MoveTowards(lamp.intensity, 4f, Time.deltaTime * 4f); }
                if (t > 1.0f && t < 1.05f) StartCoroutine(Shot("m19a_the_caretaker_raises_a_lamp"));
                yield return null;
            }
            yield return say;
            // 7 to the worker beside her
            var w = Worker != null ? Worker.transform.position : L.WestControl;
            Seven.transform.rotation = Quaternion.LookRotation(Flat(w - P7)); if (Worker != null) Worker.Face(P7);
            TwoShot((P7 + w) * 0.5f, Vector3.Cross(Vector3.up, Flat(w - P7).normalized), 3.2f, 1.5f, 1.4f, 44f, 0.01f);
            yield return SayIds("MAR_06_H2");                    // The other side is ready. Hold until I say.
            if (Worker != null) { Worker.Face(L.WestControl); }
            ShotClear(L.WestControl + Vector3.up * 1.1f, new[] { L.WestControl + new Vector3(-3f, 1.7f, 2.6f), L.WestControl + new Vector3(-3f, 1.7f, -2.6f) }, 40f, 0.01f);
            yield return SayIds("MAR_06_H3");                    // Holding.
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { Spin(WheelW, -40f * Time.deltaTime); yield return null; }
            // the power surges - a loose span section swings out of line
            var seg = SwingSeg; float ang = 0f; bool held = false;
            AudioKit.Play("mars_section_slide", L.SwingSegment, 1f, 0.03f, AudioKit.Bus.Sfx, 60f);
            CameraShake.Quake(0.4f, 0.2f);
            IEnumerator Swing() { while (!held) { ang = Mathf.MoveTowards(ang, 28f, 14f * Time.deltaTime); if (seg != null) seg.localRotation = Quaternion.Euler(0f, ang, 0f); yield return null; } }
            StartCoroutine(Swing());
            Shoot(L.SwingSegment + new Vector3(-12f, 5f, -9f), L.SwingSegment, 46f, 0.01f);
            yield return Wait(0.7f);
            yield return Shot("m19b_a_section_swings_out");
            OverHerShoulderUp(L.SwingSegment);
            yield return SayIds("MAR_06_H4", "MAR_06_H5");       // Not that one! / Specificity. Always helpful.
            yield return Cutscene(false);
            Objective("Catch the swinging section with telekinesis and hold it (RT)");
            yield return Telekinesis(L.SwingSegment, 30f, "secure the swinging span section (telekinesis)", "Look at the section swinging out from the old bridge and use telekinesis (RT)", null, null);
            held = true;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f) { ang = Mathf.Lerp(ang, 0f, t); if (seg != null) seg.localRotation = Quaternion.Euler(0f, ang, 0f); yield return null; }
            if (seg != null) seg.localRotation = Quaternion.identity;
            yield return Shot("m19_she_secures_it");
            yield return SayIds("MAR_06_H6");                    // Now! (a short, relieved laugh)
            if (lamp != null) Destroy(lamp.gameObject, 6f);
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
            // the worker and the caretaker turn their wheels together
            if (Worker != null) { Worker.Place(L.WestControl + new Vector3(-1.6f, 0f, 0f), Vector3.right); Worker.Play("carry"); }
            if (Caretaker != null) { Caretaker.Place(L.EastControl + new Vector3(1.6f, 0f, 0f), Vector3.left); Caretaker.Play("carry"); }
            Shoot(L.WestControl + new Vector3(-4.5f, 2.2f, -3.5f), L.WestControl + Vector3.up * 1.2f, 44f, 0.01f);
            for (float t = 0f; t < 1.0f; t += Time.deltaTime) { Spin(WheelW, -160f * Time.deltaTime); yield return null; }
            Glow(ControlW, 2.4f);
            AudioKit.Play("mars_control_on", L.WestControl, 1f, 0.03f, AudioKit.Bus.Sfx, 80f);
            yield return Shot("m20_the_worker_on_the_west_control");
            Shoot(L.EastControl + new Vector3(4.5f, 2.2f, -3.5f), L.EastControl + Vector3.up * 1.2f, 44f, 0.01f);
            for (float t = 0f; t < 1.0f; t += Time.deltaTime) { Spin(WheelE, 160f * Time.deltaTime); yield return null; }
            Glow(ControlE, 2.4f);
            if (ChannelE != null) StartCoroutine(ChannelE.Run(0, ChannelE.Count - 1, 1.6f));
            yield return Shot("m21_the_caretaker_on_the_east_control");
            // the halves of the symbol on the two controls read as one design - the mechanism's gears turn
            // WIDE - THE BRIDGE: the ancient structures align, forming a passage toward the temple
            Shoot(new Vector3(0f, 26f, 92f), L.IslandC + Vector3.up * 2f, 56f, 0.01f);
            AudioKit.Play("mars_bridge_align", L.IslandC, 1f, 0.02f, AudioKit.Bus.Sfx, 300f);
            CameraShake.Quake(2.5f, 0.35f);
            bool s = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 4.5f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                AlignSpan(SpanW, u); AlignSpan(SpanE, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.15f - 0.1f)));
                if (MechGears != null) foreach (var gg in MechGears) Spin(gg, 90f * Time.deltaTime);
                if (!s && t > 0.55f) { s = true; StartCoroutine(Shot("m22_wide_the_bridge_aligns")); }
                yield return null;
            }
            AlignSpan(SpanW, 1f); AlignSpan(SpanE, 1f);
            SetZone("SpanW", true); SetZone("SpanE", true);
            CameraOccluders.SyncAll();
            if (Worker != null) Worker.Play("idle"); if (Caretaker != null) Caretaker.Play("idle");
            yield return Wait(1.2f);
            yield return Shot("m23_a_passage_toward_the_temple");
            Log("section 6: both controls together - the ancient spans aligned (SpanW / SpanE open)");
            yield return Cutscene(false);
        }
    }
}
