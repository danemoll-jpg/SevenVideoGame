using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using L = Seven.BiavehLayout;

namespace Seven
{
    /// <summary>Round 17 — the mothership: the flow, BIA_03-BIA_08 (BIA_09-BIA_11 + the way home: BiavehDirectorTemple).</summary>
    public partial class BiavehDirector
    {
        int RelaysDone => (BiavehState.StepDone("relay1") ? 1 : 0) + (BiavehState.StepDone("relay2") ? 1 : 0) + (BiavehState.StepDone("relay3") ? 1 : 0);
        string RelayLine => $"\nRelays {RelaysDone} / 3";

        IEnumerator Flow(int cp)
        {
            Restore(cp);
            yield return null; yield return null;
            if (cp <= L.CpBerth) { BikeHoldWatch.Begin("biaveh", Hold, Bike, Saucer); yield return Berth(); Checkpoint(L.CpRelay1); }
            else yield return UI.Fade(0f, 1.2f);
            if (BiavehState.Checkpoint <= L.CpRelay1) { yield return Relay1(); Checkpoint(L.CpRelay2); }
            if (BiavehState.Checkpoint <= L.CpRelay2) { yield return Garden(); Checkpoint(L.CpRelay3); }
            if (BiavehState.Checkpoint <= L.CpRelay3) { yield return Passage(); Checkpoint(L.CpFight); }
            if (BiavehState.Checkpoint <= L.CpFight) { yield return Cornering(); Checkpoint(L.CpTemple); }
            if (DemoBoot.BossCheck || DemoBoot.BossIdle) yield break;
            if (BiavehState.Checkpoint <= L.CpTemple) { yield return Temple(); Checkpoint(L.CpHome); }
            yield return Home();
        }

        /// <summary>Put everything where the checkpoint says (reload, Continue, Scene Select).</summary>
        void Restore(int cp)
        {
            foreach (var z in L.Zones) SetZone(z.Name, z.On);
            bool r1 = BiavehState.StepDone("relay1") || cp > L.CpRelay1, r2 = BiavehState.StepDone("relay2") || cp > L.CpRelay2, r3 = BiavehState.StepDone("relay3") || cp > L.CpRelay3;
            bool bridge = cp > L.CpRelay1 || BiavehState.StepDone("bridge");
            SetZone("Bridge", bridge); PlaceSegments(bridge ? 1f : 0f);
            if (EchoBridge != null) EchoBridge.SetNow(false); if (EchoBerth != null) EchoBerth.SetNow(false); if (EchoGarden != null) EchoGarden.SetNow(false);
            Glow(Relay1Lamp, r1 ? 1.8f : 0.15f); Glow(Relay2Lamp, r2 ? 1.8f : 0.15f); Glow(Relay3Lamp, r3 ? 1.8f : 0.15f);
            Lights(DistrictLights1, r1); Lights(DistrictLights2, r2); Lights(DistrictLights3, r3);
            Off(PartitionA, r1 ? Vector3.zero : new Vector3(0f, 26f, 0f));
            Off(SeamPanel, r2 || BiavehState.StepDone("seam") ? new Vector3(1.9f, 0f, 0f) : Vector3.zero);
            Off(UpperShutter, r2 ? Vector3.zero : new Vector3(0f, 3.2f, 0f));
            bool passageOpen = r3;
            Door(PassageDoor, passageOpen); Door(LockDoorA, passageOpen); Door(LockDoorB, passageOpen);
            SetZone("LockDoorA", passageOpen); SetZone("LockDoorB", passageOpen);
            bool fightDone = cp > L.CpFight;
            if (ChamberPartitions != null) for (int i = 0; i < ChamberPartitions.Length; i++) Off(ChamberPartitions[i], new Vector3(0f, 14f, 0f));   // raised before the fight and again after it (QF23)
            Off(OuterBarrier, fightDone ? Vector3.zero : new Vector3(0f, 6f, 0f));
            Off(InnerBarrier, fightDone ? Vector3.zero : new Vector3(0f, 6f, 0f));
            bool archiveOpen = cp >= L.CpTemple;
            SetZone("ArchiveDoorZ", archiveOpen); SetZone("Temple", archiveOpen); Off(ArchiveDoor, archiveOpen ? new Vector3(0f, 7f, 0f) : Vector3.zero);
            Glow(RoutingLamp, archiveOpen ? 1.6f : 0.2f);
            if (RecordOcta != null) RecordOcta.gameObject.SetActive(false);
            if (CityLights != null) foreach (var c in CityLights) Glow(c, archiveOpen ? 1.2f : 0.35f);
            // the people
            if (Nela != null) { Nela.gameObject.SetActive(cp <= L.CpBerth || cp >= L.CpTemple); if (cp <= L.CpBerth) Nela.Place(L.NelaAt, Vector3.back); else Nela.Place(L.TempleDoor + new Vector3(-2f, 0f, -2.5f), Vector3.back); Nela.Play("idle"); }
            if (ResidentA != null) { ResidentA.gameObject.SetActive(cp <= L.CpBerth); ResidentA.Place(L.ResidentAAt, Vector3.left); }
            if (Crossers != null) for (int i = 0; i < Crossers.Length; i++) if (Crossers[i] != null)
            {
                var c = Crossers[i];
                if (bridge) { c.Place(new Vector3(120f - i * 2.4f, 0f, 3.2f + (i % 2) * 1.2f), Vector3.left); c.Loop(new[] { new Vector3(60f - i * 3f, 0f, 3.5f), new Vector3(190f + i * 3f, 0f, -3.2f) }, 3f, 1.2f); }
                else c.Place(L.FarSide + new Vector3(2f + i * 1.4f, 0f, -2f + i * 1.2f), Vector3.left);
            }
            if (Seedlings != null) Seedlings.gameObject.SetActive(!bridge);
            // QF23 (13e): a voice matches the look - the seedlings resident keeps the (female) Biavian look and a female voice;
            // another resident gets the masculine variant (broader shoulders, a squarer head, taller)
            if (Crossers != null && Crossers.Length > 1 && Crossers[1] != null) Crossers[1].MakeMasculine();
            if (Crossers != null && Crossers.Length > 0 && Crossers[0] != null && Seedlings != null && !bridge) { Crossers[0].CarryItem = Seedlings; Crossers[0].Carry = true; }
            PlaceRatwing(cp);
            // 7 and the bike
            Vector3 at; float yaw = 90f; bool riding = true;
            switch (cp)
            {
                case L.CpBerth: at = L.BikePad; riding = false; break;
                case L.CpRelay1: at = new Vector3(30f, 0f, 0f); break;
                case L.CpRelay2: at = new Vector3(198f, 0f, 0f); break;
                case L.CpRelay3: at = L.GardenPark + new Vector3(0f, 0f, -2f); yaw = 180f; break;
                case L.CpFight: at = new Vector3(210f, 0f, 76f); yaw = 0f; break;
                default: at = L.RoutingView + new Vector3(0f, 0f, -4f); riding = false; yaw = 0f; break;
            }
            if (cp >= L.CpTemple) Bike.Place(L.ChamberEntry, 0f); else Bike.Place(at, yaw);
            if (cp <= L.CpBerth) { Bike.gameObject.SetActive(false); Seven.gameObject.SetActive(false); }
            else if (riding) { Seven.gameObject.SetActive(true); Bike.Mount(); }
            else { Seven.gameObject.SetActive(true); Seven.Place(at, Quaternion.Euler(0f, yaw, 0f) * Vector3.forward); }
            MusicSystem.Play(cp == L.CpFight ? "biaveh_ratwing" : cp <= L.CpBerth ? null : "biaveh_mothership", 2.5f);
            FollowCamera(); Cam.SnapBehind();
            Log($"restore: {BiavehState.Summary()} -> {(riding ? "riding" : "on foot")} at {at}");
        }
        void Lights(Renderer[] rs, bool on) { if (rs != null) foreach (var r in rs) Glow(r, on ? 1.3f : 0.2f); }
        void Door(Transform d, bool open) => Off(d, open ? new Vector3(0f, 4.2f, 0f) : Vector3.zero);
        IEnumerator DoorMove(Transform d, bool open, float secs = 0.9f)
        {
            if (d == null) yield break;
            AudioKit.Play("bia_airlock", d.position + Vector3.up * 1.5f, 0.9f, 0.04f, AudioKit.Bus.Sfx, 30f);
            var a = d.localPosition; var b = Home(d) + (open ? new Vector3(0f, 4.2f, 0f) : Vector3.zero);
            for (float t = 0f; t < 1f; t += Time.deltaTime / secs) { d.localPosition = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            d.localPosition = b;
        }
        void PlaceSegments(float k)
        {
            if (BridgeSegments == null || SegmentParked == null || SegmentAligned == null) return;
            for (int i = 0; i < BridgeSegments.Length; i++) if (BridgeSegments[i] != null) BridgeSegments[i].localRotation = Quaternion.Slerp(Quaternion.Euler(SegmentParked[i]), Quaternion.Euler(SegmentAligned[i]), k);
        }
        void PlaceRatwing(int cp)
        {
            if (Ratwing == null) return;
            Ratwing.gameObject.SetActive(true);
            if (cp <= L.CpRelay1) { SetRat(new Vector3(L.RatwingPerch.x, 0f, L.RatwingPerch.z), L.RatwingPerch.y, RatwingRig.Mode.Cling, Vector3.left); }
            else if (cp == L.CpRelay2) { Ratwing.gameObject.SetActive(false); }
            else if (cp <= L.CpFight) { SetRat(new Vector3(L.ArchiveEntrance.x, 0f, L.ArchiveEntrance.z - 3f), 0f, RatwingRig.Mode.Perched, Vector3.forward); if (cp < L.CpFight) Ratwing.gameObject.SetActive(false); }
            else { SetRat(L.CompartmentIn, 0f, RatwingRig.Mode.Perched, Vector3.right); }
        }
        void SetRat(Vector3 ground, float h, RatwingRig.Mode m, Vector3 face)
        {
            Ratwing.position = ground; if (face.sqrMagnitude > 0.01f) Ratwing.rotation = Quaternion.LookRotation(Flat(face));
            if (RRig != null) { RRig.Height = h; RRig.Now = m; if (RRig.Body != null) RRig.Body.localPosition = new Vector3(0f, h, 0f); }
        }
        /// <summary>He flies from where he is to a point (ground position + height), wings beating; ends perched / clinging.</summary>
        IEnumerator FlyRat(Vector3 to, float toH, float secs, RatwingRig.Mode end = RatwingRig.Mode.Perched)
        {
            if (Ratwing == null || RRig == null) yield break;
            var a = Ratwing.position; float h0 = RRig.Height; var b = new Vector3(to.x, 0f, to.z);
            RRig.Now = RatwingRig.Mode.Fly; AudioKit.Play("ratwing_flap", Ratwing.position + Vector3.up * h0, 0.9f, 0.05f, AudioKit.Bus.Sfx, 60f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / secs)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Ratwing.position = Vector3.Lerp(a, b, u);
                RRig.Height = Mathf.Lerp(h0, toH, u) + Mathf.Sin(u * Mathf.PI) * 3f;
                var d = Flat(b - a); if (d.sqrMagnitude > 0.01f) Ratwing.rotation = Quaternion.Slerp(Ratwing.rotation, Quaternion.LookRotation(d), Time.deltaTime * 4f);
                yield return null;
            }
            RRig.Height = toH; RRig.Now = end;
        }

        // ------------------------------------------------------------------------------------------ BIA_03: a world inside
        IEnumerator Berth()
        {
            yield return Cutscene(true);
            MusicSystem.Play("biaveh_mothership", 3f);
            // the saucer has docked at the receiving berth: the bike lowers out of the bike hold, 7 comes down the beam
            Shoot(new Vector3(16f, 7f, -16f), new Vector3(0f, 2.5f, 4f), 52f, 0.01f);
            StartCoroutine(UI.Fade(0f, 1.6f));
            yield return Wait(1.4f);
            yield return Shot("b01_the_receiving_berth");
            yield return HoldBikeOut(L.BikePad, 90f);
            Shoot(L.SaucerAt + new Vector3(9f, 2.4f, -9f), L.SaucerAt + new Vector3(0f, 2.2f, 0f), 46f, 0.01f);
            if (SaucerBeam != null) { yield return SaucerBeam.Iris(true); yield return SaucerBeam.BeamOn(true); }
            Seven.gameObject.SetActive(true); Seven.Lock(true); Seven.BeginFloat();
            bool s3 = false;
            if (SaucerBeam != null) yield return SaucerBeam.Lower(Seven.transform, 3.4f, Vector3.forward, t => { if (!s3 && t > 0.5f) { s3 = true; StartCoroutine(Shot("b02_7_down_the_beam_the_bike_on_its_pad")); } });
            Seven.EndFloat(); Seven.Place(L.SevenDown, Vector3.forward); Seven.Lock(true);
            if (SaucerBeam != null) StartCoroutine(SaucerBeam.Open(false));
            // Nela waits beside the overlook (BIA_02_14: "Follow the lights to the receiving berth")
            Shoot(P7 + new Vector3(-2.2f, 1.7f, -2.4f), L.NelaAt + Vector3.up * 1.3f, 44f, 0.5f);
            yield return Wait(1.2f);
            yield return Shot("b03_nela_waits_beside_the_overlook");
            yield return Cutscene(false);
            Objective("Join Nela at the overlook");
            yield return GuideWalk(new[] { L.OverlookAt + new Vector3(0.8f, 0f, -1.6f) }, "to the overlook");
            yield return Cutscene(true);
            Seven.Place(L.OverlookAt + new Vector3(0.6f, 0f, 0.9f), Vector3.left); Seven.Lock(true);
            if (Nela != null) Nela.Face(P7);
            // WIDE REVEAL: the interior city curves up the shell; the camera follows her upward gaze
            var c0 = P7 + new Vector3(2.6f, 1.6f, 1.2f); var c1 = P7 + new Vector3(6f, 2.2f, 2.6f);
            Shoot(c0, P7 + new Vector3(-20f, 2f, 0f), 52f, 0.01f);
            StartCoroutine(RevealAir(true));
            for (float t = 0f; t < 1f; t += Time.deltaTime / 5f) { float u = Mathf.SmoothStep(0f, 1f, t); Cam.Shot(Vector3.Lerp(c0, c1, u), Vector3.Lerp(P7 + new Vector3(-20f, 2f, 0f), new Vector3(-230f, 120f, 10f), u), 1.2f, false, Mathf.Lerp(52f, 66f, u)); yield return null; }
            yield return Shot("b04_the_city_curves_up_the_shell");
            yield return Say("BIA_03", 1, 3);                    // People live all the way up there? / Yes. Most of us never visit every district. / I can believe that.
            TwoShot((P7 + L.NelaAt) * 0.5f, Vector3.Cross(Vector3.up, Flat(L.NelaAt - P7).normalized), 3.0f, 1.3f, 1.3f, 44f, 0.5f);
            yield return Say("BIA_03", 4, 5);                    // Remember where we parked. / Little saucer. Enormous wall. Got it.
            // the memory echo across the passage: seated figures in a room that no longer exists; a resident stops, disoriented
            Shoot(P7 + new Vector3(-1f, 1.7f, -1.5f), L.EchoAt + Vector3.up * 1.2f, 46f, 0.6f);
            if (EchoBerth != null) EchoBerth.Set(true);
            if (ResidentA != null) { ResidentA.Face(L.EchoAt); ResidentA.Play(ResidentA.HasState("kneel") ? "kneel" : "idle", 0.5f); }
            yield return Wait(1.6f);
            yield return Shot("b05_the_memory_echo_a_resident_stops");
            yield return Say("BIA_03", 6, 6);                    // What was that?
            // QF23 (9): never hold 7 still while someone walks - a cut: Nela is already beside the resident
            if (Nela != null) { Nela.Stop(); var nAt = L.ResidentAAt + new Vector3(-0.9f, 0f, 0.6f); Nela.Place(nAt, L.ResidentAAt - nAt); Nela.Play(Nela.HasState("kneel") ? "kneel" : "idle", 0.2f); Log("WALK CUT (QF23 9): BIA_03 - Nela is already beside the resident (no walk on screen)"); }
            Seven.Release(); Seven.Lock(true);
            Seven.transform.rotation = Quaternion.LookRotation(Flat(L.ResidentAAt - P7));
            TwoShot((P7 + L.ResidentAAt) * 0.5f, Vector3.Cross(Vector3.up, Flat(L.ResidentAAt - P7).normalized), 3.4f, 1.1f, 1.0f, 44f, 0.5f);
            yield return Say("BIA_03", 7, 7);                    // A memory from the archive. The damaged relays are carrying it into our shared connection.
            if (EchoBerth != null) EchoBerth.Set(false);
            yield return Say("BIA_03", 8, 9);                    // The intruder did this? / Yes. Ratwing. He cuts one district off...
            Shoot(P7 + new Vector3(-1.2f, 1.6f, -1.6f), L.SealedGate, 40f, 0.5f);
            yield return Wait(0.6f);
            yield return Shot("b06_the_sealed_gate");
            yield return Cutaway("b06b_aboard_ratwing_his_ears_snap_forward", AboardCut.Mood.Watch, "him", 0.6f, "BIA_03_10");   // QF23 (10): Ratwing? - his ears snap forward
            yield return Say("BIA_03", 11, 12);                  // You heard that part, then. / I'm listening to all of it.
            if (ResidentA != null) ResidentA.Play("idle", 0.6f);
            if (Nela != null) { Nela.Play("idle", 0.2f); Nela.Place(L.NelaAt, L.DisplayAt - L.NelaAt); }      // QF23 (9): a cut, not a walk
            Complete("BIA_03");
            // ------------------------------------------------------------------ BIA_04: something worth stealing (at the display)
            if (Nela != null) Nela.Face(L.DisplayAt, true);
            Seven.Place(L.DisplayAt + new Vector3(1.6f, 0f, -0.4f), Vector3.left); Seven.Lock(true);
            Shoot(L.DisplayAt + new Vector3(2.6f, 1.9f, 2.4f), L.DisplayAt + Vector3.up * 1.1f, 44f, 0.01f);
            if (RouteDisplay != null) Glow(RouteDisplay.GetComponentInChildren<Renderer>(), 0.9f);
            yield return Wait(0.6f);
            yield return Shot("b07_the_temple_route_on_the_display");
            yield return Say("BIA_04", 1, 4);                    // He wants the archive... / To sell? / Or to make us pay... / And if we reconnect the districts?
            // three routes illuminate: the three relays round the protected archive (shown, not captioned)
            if (RouteMarks != null) for (int i = 0; i < RouteMarks.Length; i++) { Glow(RouteMarks[i], 1.8f); AudioKit.Play("console_select", L.DisplayAt, 0.4f); yield return Wait(0.35f); }
            Glow(RouteLine, 1.2f);
            Shoot(L.DisplayAt + new Vector3(1.3f, 1.6f, 0.9f), L.DisplayAt + Vector3.up * 1.05f, 34f, 0.01f);
            yield return Say("BIA_04", 5, 5);                    // We can coordinate our barriers and keep him from moving freely.
            yield return Shot("b08_three_relays_light_on_the_display");
            TwoShot((P7 + L.NelaAt) * 0.5f, Vector3.Cross(Vector3.up, Flat(L.NelaAt - P7).normalized), 3.0f, 1.3f, 1.3f, 44f, 0.4f);
            yield return Say("BIA_04", 6, 7);                    // That sounds promising. / You're enjoying this already.
            yield return Cutaway("b09_aboard_a_faint_tail_tip_twitch", AboardCut.Mood.Twitch, "him", 0.8f, "BIA_04_08");   // I appreciate a well-defined problem.
            TwoShot((P7 + L.NelaAt) * 0.5f, Vector3.Cross(Vector3.up, Flat(L.NelaAt - P7).normalized), 2.8f, 1.3f, 1.3f, 42f, 0.4f);
            if (Nela != null) { Nela.Face(P7); Nela.Play(Nela.HasState("point") ? "point" : "idle", 0.4f); }      // an open hand; she does not touch her
            yield return Say("BIA_04", 9, 12);                   // May I share the way with you? / Would you see my thoughts? / Only what you choose... / All right. Just the route.
            yield return RouteShare();
            // QF23 (15): how Panther sees in here - the hull would block the saucer's scanners, so Nela links them
            TwoShot((P7 + L.NelaAt) * 0.5f, Vector3.Cross(Vector3.up, Flat(L.NelaAt - P7).normalized), 3.0f, 1.3f, 1.3f, 44f, 0.01f);
            yield return SayIds("BIA_04_N1");                    // I've linked your ship to our inner sensors. Your companion will be able to follow him.
            _ratLinked = true;
            yield return Cutaway("b10b_aboard_the_display_her_marker_and_ratwings", AboardCut.Mood.Hunt, "screen", 1.6f, "BIA_04_N2");   // Delighted. (far too pleased: glued to the screen)
            Complete("BIA_04");
            StartCoroutine(RevealAir(false));
            yield return Cutscene(false);
            // the objective comes from the story: the three relays she was shown
            if (Nela != null) Nela.Play("idle", 0.4f);
            Objective("Restore the three relays - the first is east, past the service junction" + RelayLine);
            FreeBike = true;
        }
        /// <summary>BIA_04: the one-way route sharing - a navigation impression, distinct from her visions (those are films):
        /// in the world, a soft line of light runs out from her feet along the roads to the three relay districts, a high shot
        /// follows it, then it settles to three faint beacons over the city.</summary>
        IEnumerator RouteShare()
        {
            AudioKit.Play("bia_route_share", P7 + Vector3.up, 0.9f);
            var pts = new[] { P7, new Vector3(40f, 0f, 0f), new Vector3(120f, 0f, 0f), L.CabinetAt, new Vector3(200f, 0f, 0f), new Vector3(260f, 0f, 30f), L.Relay2At, new Vector3(230f, 0f, 66f), L.Relay3At };
            var dots = new System.Collections.Generic.List<GameObject>();
            var mat = Markers.Additive(1.6f);
            Shoot(P7 + new Vector3(-8f, 22f, -20f), new Vector3(120f, 0f, 30f), 58f, 0.01f);
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                int n = Mathf.CeilToInt(Vector3.Distance(pts[i], pts[i + 1]) / 4f);
                for (int k = 0; k < n; k++)
                {
                    var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(g.GetComponent<Collider>()); g.transform.position = Vector3.Lerp(pts[i], pts[i + 1], k / (float)n) + Vector3.up * 0.4f; g.transform.localScale = Vector3.one * 0.55f; g.GetComponent<Renderer>().sharedMaterial = mat; dots.Add(g);
                    if (k % 3 == 0) yield return null;
                }
            }
            var beacons = new System.Collections.Generic.List<GameObject>();
            foreach (var p in new[] { L.CabinetAt, L.Relay2At, L.Relay3At })
            { var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(g.GetComponent<Collider>()); g.transform.position = p + Vector3.up * 30f; g.transform.localScale = new Vector3(0.6f, 30f, 0.6f); g.GetComponent<Renderer>().sharedMaterial = Markers.Additive(0.8f); beacons.Add(g); }
            yield return Wait(1.2f);
            yield return Shot("b10_the_shared_route_a_line_of_light_to_the_three_relays");
            Log("BIA_04: the route shared one way - a line of light along the roads to the three relays (a navigation impression, not a vision)");
            foreach (var g in dots) Destroy(g, 0.6f);
            _beacons = beacons;
            yield return Wait(0.6f);
        }
        System.Collections.Generic.List<GameObject> _beacons;
        void Beacon(int i, bool on) { if (_beacons != null && i < _beacons.Count && _beacons[i] != null) _beacons[i].SetActive(on); }

        // ------------------------------------------------------------------------------------------ BIA_05 + BIA_06: the first relay
        IEnumerator Relay1()
        {
            FreeBike = true;
            if (!BiavehState.SceneDone("BIA_05"))
            {
                Objective("Restore the three relays - the first is east, past the service junction" + RelayLine);
                if (!Riding) yield return GetBackOn();
                yield return RideRoute(L.RideToJunction, "to the service junction", 7f);
                yield return StopBike(1.6f);
                yield return Cutscene(true);
                // something moves high above: LOW ANGLE - Ratwing clings beneath the bridge, partly hidden by its supports
                Shoot(new Vector3(L.RatwingPerch.x - 6f, 0.9f, 7f), L.RatwingPerch + Vector3.down * 1.2f, 44f, 0.01f);
                AudioKit.Play("ratwing_flap", L.RatwingPerch, 0.5f, 0f, AudioKit.Bus.Sfx, 50f);
                yield return Wait(0.8f);
                yield return Shot("b11_low_angle_ratwing_beneath_the_bridge");
                yield return Cutaway("b11a_aboard_the_display_he_has_him", AboardCut.Mood.Watch, "screen", 0.5f, "BIA_05_01");   // There. Above you. (the instruments: where his eyes are)
                Shoot(new Vector3(L.RatwingPerch.x - 6f, 0.9f, 7f), L.RatwingPerch + Vector3.down * 1.2f, 44f, 0.01f);
                yield return Say("BIA_05", 2, 2);                // I see him.
                // he unfolds his wings and drops onto a distant platform
                yield return FlyRat(L.RatwingDrop, L.RatwingDrop.y, 2.2f);
                if (Ratwing != null) Ratwing.rotation = Quaternion.LookRotation(Flat(P7 - Ratwing.position));
                Shoot(P7 + new Vector3(-2.4f, 1.4f, -2f), L.RatwingDrop + Vector3.up * 1.2f, 30f, 0.5f);
                yield return Wait(0.5f);
                yield return Shot("b12_ratwing_on_the_distant_platform");
                yield return Say("BIA_05", 3, 3);                // They've found someone to run their errands.
                if (Riding) { if (!Bike.Dismount()) Bike.Dismount(true); }
                Seven.Lock(true);
                yield return Say("BIA_05", 4, 7);                // You've made quite a mess. / They can tidy up... / For their own archive? / They seem very attached to it.
                // he severs a local control connection and launches into the vertical shaft
                Sparks(L.RatwingDrop + new Vector3(-1.5f, 0.6f, 1f), 90); AudioKit.Play("ast_power_down", L.RatwingDrop, 0.7f, 0f, AudioKit.Bus.Sfx, 60f);
                StartCoroutine(FlyRat(L.ShaftTop, L.ShaftTop.y, 3f));
                Shoot(P7 + new Vector3(-3f, 1.2f, -3f), L.ShaftTop, 46f, 0.6f);
                yield return Say("BIA_05", 8, 11);               // Follow him. / Panther. / We should establish where he's going. / Is your tail doing that little twitch?
                if (Ratwing != null) Ratwing.gameObject.SetActive(false);
                yield return Cutaway("b13_aboard_his_twitching_tail_it_stops", AboardCut.Mood.Twitch, "him", 1.4f);
                yield return Cutaway("b13b_aboard_composed_repair_the_relay", AboardCut.Mood.Composed, "him", 0.4f, "BIA_05_13");   // Repair the relay. (sits up straight)
                yield return Say("BIA_05", 14, 14);              // Thought so.
                yield return SayIds("BIA_05_N1");                // QF23 (12): what's wrong + what she'll do - the relay is in the cabinet, he tore its coupling loose
                Complete("BIA_05");
                yield return Cutscene(false);
            }
            if (!BiavehState.StepDone("relay1"))
            {
                Objective("Repair the relay - the damaged cabinet at the junction (X)" + RelayLine);
                yield return Interact(L.CabinetView, "Repair the relay", "repair relay 1", "The damaged control cabinet at the junction, east side", new[] { L.CabinetView });
                yield return Cutscene(true);
                Shoot(L.CabinetAt + new Vector3(-2.2f, 1.8f, -2.4f), L.CabinetAt + Vector3.up * 1.2f, 40f, 0.01f);
                Seven.Touch(L.CabinetAt + Vector3.up * 1.2f);
                if (Cabinet1Door != null) StartCoroutine(Swing(Cabinet1Door, -95f, 0.8f));
                yield return Wait(0.8f);
                AudioKit.Play("bia_relay_on", L.CabinetAt, 1f, 0f, AudioKit.Bus.Sfx, 60f);
                Glow(Relay1Lamp, 1.8f); Beacon(0, false);
                yield return Wait(0.6f);
                yield return Shot("b14_relay_one_repaired_its_light_returns");
                Seven.Release();
                yield return Cutscene(false);
            }
            // BIA_06: the relay controls a suspended bridge - stranded residents on the far side
            if (!BiavehState.StepDone("bridge"))
            {
                yield return Cutscene(true);
                Shoot(new Vector3(146f, 6f, 10f), new Vector3(178f, -2f, -1f), 50f, 0.01f);
                if (EchoBridge != null) EchoBridge.Set(true);
                yield return Wait(1.4f);
                yield return Shot("b15_the_bridge_out_of_line_the_echo_of_an_old_span");
                yield return SayIds("BIA_06_N1");                // QF23 (12, 13a): that relay held the bridge in line - our people are stranded
                Shoot(new Vector3(150f, 2f, 4f), L.FarSide + Vector3.up * 1.4f, 32f, 0.4f);
                if (Crossers != null && Crossers.Length > 0 && Crossers[0] != null) Crossers[0].Play(Crossers[0].HasState("wave") ? "wave" : "idle", 0.3f);
                yield return Wait(1.2f);
                yield return Shot("b16_residents_stranded_on_the_far_side");
                yield return SayIds("BIA_06_N2");                // the bridge's service controls are at the chasm's edge
                yield return Cutscene(false);
                Objective("Align the bridge - the service controls by the chasm (X)" + RelayLine);
                yield return Interact(L.BridgeControlView, "Align the bridge", "align the bridge", "The service controls at the chasm's edge, north of the road", new[] { L.BridgeControlView });
                yield return Cutscene(true);
                Shoot(new Vector3(152f, 9f, -16f), new Vector3(170f, 0f, 0f), 54f, 0.01f);
                if (BridgeLever != null) StartCoroutine(Swing(BridgeLever, -70f, 0.5f, true));
                AudioKit.Play("bia_bridge", new Vector3(170f, 0f, 0f), 1f, 0f, AudioKit.Bus.Sfx, 90f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 3f) { PlaceSegments(Mathf.SmoothStep(0f, 1f, t)); yield return null; }
                PlaceSegments(1f); SetZone("Bridge", true); BiavehState.DoStep("bridge");
                if (EchoBridge != null) EchoBridge.Set(false);
                CameraShake.Quake(0.25f, 0.25f);
                yield return Shot("b17_the_bridge_settles_into_line");
                // the waiting residents cross; one struggles with a container of seedlings
                // QF23 (13c, 9): no long wait - a cut: the residents are already most of the way across; the one with the
                // seedlings walks the last couple of metres to her (a carrying walk: legs stepping, the tray held in both arms)
                Seven.Place(new Vector3(149f, 0f, -2.6f), Vector3.right); Seven.Lock(true);
                var r1 = Crossers != null && Crossers.Length > 0 ? Crossers[0] : null;
                if (Crossers != null) for (int i = 1; i < Crossers.Length; i++) if (Crossers[i] != null) { Crossers[i].Place(new Vector3(152.5f + i * 1.8f, 0f, 1.6f - i * 0.8f), Vector3.left); Crossers[i].WalkTo(new Vector3(60f - i * 3f, 0f, 3.6f), 1.6f); }
                if (r1 != null) { r1.CarryItem = Seedlings; r1.Carry = true; if (Seedlings != null) Seedlings.gameObject.SetActive(true); r1.Place(new Vector3(153.8f, 0f, -2.0f), Vector3.left); r1.WalkTo(new Vector3(151.0f, 0f, -2.2f), 1.45f); }
                Shoot(new Vector3(146f, 1.6f, -7f), new Vector3(155f, 1.2f, -1f), 46f, 0.01f);
                Log("WALK CUT (QF23 9, 13c): the residents are already most of the way across - the seedlings resident walks the last 2.8 m (a carrying walk)");
                for (float t = 0f; t < 2.5f && r1 != null && r1.Busy; t += Time.deltaTime) yield return null;
                yield return Shot("b17b_she_carries_the_tray_legs_stepping");
                if (r1 != null) { r1.Face(P7); Seven.transform.rotation = Quaternion.LookRotation(Flat(r1.transform.position - P7)); TwoShot((P7 + r1.transform.position) * 0.5f, Vector3.Cross(Vector3.up, Flat(r1.transform.position - P7).normalized), 3.0f, 1.2f, 1.2f, 42f, 0.4f); }
                yield return Shot("b18_the_seedlings_resident");
                yield return Say("BIA_06", 1, 4);                // Thank you... / Are those all yours? / They were smaller when I left home. / Heh. Good luck.
                if (r1 != null) r1.WalkTo(new Vector3(60f, 0f, 3.6f), 1.2f);   // she carries on, the tray in her arms
                // the first district reconnects: distant lights return; a security partition closes across one of his routes
                BiavehState.DoStep("relay1");
                Lights(DistrictLights1, true);
                Shoot(new Vector3(150f, 3f, -8f), L.PartitionA, 50f, 0.01f);
                if (PartitionA != null) StartCoroutine(Drop(PartitionA, 26f, 0f, 2.2f));
                AudioKit.Play("bia_partition", L.PartitionA, 1f, 0f, AudioKit.Bus.Sfx, 200f);
                yield return Wait(2.4f);
                yield return Shot("b19_district_lights_return_a_partition_closes_his_route");
                yield return Say("BIA_06", 5, 5);                // We have the crossing. Everyone is through.
                yield return Cutaway("b19b_aboard_watching_his_marker", AboardCut.Mood.Hunt, "screen", 0.6f, "BIA_06_06");   // And he's lost that passage. (glued to his marker)
                yield return Say("BIA_06", 7, 7);                // Still watching him?
                yield return Cutaway("b19c_aboard_batting_a_cable_then_composed", AboardCut.Mood.Bat, "wide", 1.4f);
                yield return Cutaway("b19d_aboard_someone_should", AboardCut.Mood.Composed, "him", 0.3f, "BIA_06_08");   // Someone should. (snaps back to dignity)
                Complete("BIA_06");
                Log($"relay 1 restored: the bridge aligned, the residents crossed, partition A closed his route - Relays {RelaysDone} / 3");
                yield return Cutscene(false);
            }
        }
        IEnumerator Swing(Transform t, float deg, float secs, bool x = false) { var a = t.localRotation; var b = a * (x ? Quaternion.Euler(deg, 0f, 0f) : Quaternion.Euler(0f, deg, 0f)); for (float k = 0f; k < 1f; k += Time.deltaTime / secs) { t.localRotation = Quaternion.Slerp(a, b, Mathf.SmoothStep(0f, 1f, k)); yield return null; } }
        IEnumerator Drop(Transform t, float fromY, float toY, float secs) { var h = Home(t); for (float k = 0f; k < 1f; k += Time.deltaTime / secs) { t.localPosition = h + new Vector3(0f, Mathf.Lerp(fromY, toY, k * k), 0f); yield return null; } t.localPosition = h + new Vector3(0f, toY, 0f); CameraShake.Quake(0.2f, 0.2f); }

        // ------------------------------------------------------------------------------------------ BIA_07: the overlapping garden
        IEnumerator Garden()
        {
            FreeBike = true;
            Objective("The second relay - the garden district, north" + RelayLine);
            if (!Riding) yield return GetBackOn();
            yield return RideRoute(L.RideToGarden, "to the garden", 7f);
            yield return GetOff();
            Objective("Find the second relay in the garden" + RelayLine);
            if (EchoGarden != null) EchoGarden.Set(true);
            StartCoroutine(SayIds("BIA_07_N1", "BIA_07_N2"));     // QF23 (14): the garden is remembering itself... / Look for what's real. (she keeps control)
            yield return GuideWalk(new[] { new Vector3(260f, 0f, 70f), L.EchoDoor + new Vector3(0f, -1.6f, -3f) }, "into the garden");
            yield return Cutscene(true);
            Seven.Place(L.EchoDoor + new Vector3(0.6f, -1.6f, -2.2f), Vector3.forward); Seven.Lock(true);
            // an old garden overlays the current one: a remembered doorway in the wall
            Shoot(P7 + new Vector3(-2.6f, 1.7f, -2.8f), L.EchoDoor, 46f, 0.01f);
            yield return Wait(0.8f);
            yield return Shot("b20_the_overlapping_garden_a_remembered_doorway");
            while (Dlg.Busy) yield return null;
            yield return SayIds("BIA_07_N3");                    // QF23 (12): the second relay is behind the garden wall - its old door was sealed
            yield return Say("BIA_07", 1, 1);                    // There used to be a door here. (the echo shows where it was)
            Shoot(P7 + new Vector3(1.6f, 1.2f, -1.4f), L.SeamAt, 36f, 0.5f);
            yield return Say("BIA_07", 2, 2);                    // There's a seam. Lower down.
            yield return Shot("b21_the_seam_lower_down");
            yield return Cutscene(false);
            // the remembered door itself can't be walked through - find its physical release
            if (!BiavehState.StepDone("seam"))
            {
                Objective("Open the seam's release (X)" + RelayLine);
                yield return Interact(L.SeamView, "Open the release", "the seam's release", "The seam low in the wall, right of the remembered doorway", new[] { L.SeamView });
                yield return Cutscene(true);
                Seven.Place(L.SeamView, Vector3.forward); Seven.PlayAnim("crouch_idle", 0.3f);
                yield return Say("BIA_07", 3, 3);                // Mm. Found it.
                AudioKit.Play("bia_seam", L.SeamAt, 1f, 0f, AudioKit.Bus.Sfx, 30f);
                if (SeamPanel != null) { var a = SeamPanel.localPosition; for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f) { SeamPanel.localPosition = Vector3.Lerp(a, Home(SeamPanel) + new Vector3(1.9f, 0f, 0f), Mathf.SmoothStep(0f, 1f, t)); yield return null; } }
                BiavehState.DoStep("seam");
                Shoot(L.Relay2At + new Vector3(-2.4f, 1.8f, -3f), L.Relay2At + Vector3.up * 1.2f, 42f, 0.01f);
                yield return Wait(0.5f);
                yield return Shot("b22_the_panel_slides_back_the_relay_behind_it");
                Seven.Release();
                yield return Cutscene(false);
            }
            Objective("Reconnect the second relay (X)" + RelayLine);
            yield return Interact(L.Relay2View, "Reconnect the relay", "reconnect relay 2", "Behind the panel that slid back, right of the seam", new[] { L.Relay2View });
            yield return Cutscene(true);
            Seven.Place(L.Relay2View, Vector3.forward); Seven.Touch(L.Relay2At + Vector3.up * 1.2f);
            // Ratwing returns: he lands beyond the glass partition and tries to reach its external controls
            if (Ratwing != null) { Ratwing.gameObject.SetActive(true); SetRat(L.RatwingGarden + new Vector3(10f, 0f, 14f), 9f, RatwingRig.Mode.Fly, Vector3.back); }
            StartCoroutine(FlyRat(new Vector3(L.RatwingGarden.x, 0f, L.RatwingGarden.z), 0f, 1.8f));
            Shoot(P7 + new Vector3(-1.4f, 1.6f, -2.2f), L.RatwingGarden + Vector3.up * 1.4f, 46f, 0.01f);
            yield return Wait(1.9f);
            AudioKit.Play("ratwing_strike", L.GlassAt + Vector3.up * 1.6f, 1f, 0f, AudioKit.Bus.Sfx, 40f); CameraShake.Quake(0.2f, 0.2f);
            yield return Shot("b23_ratwing_beyond_the_glass");
            yield return Say("BIA_07", 4, 5);                    // Leave it alone. / You first.
            // QF23 (15b): set the threat up - the service opening above the glass (a beat on it), the control beside her (lit)
            Shoot(P7 + new Vector3(-1.6f, 1.3f, -2.8f), L.UpperOpening, 40f, 0.5f);
            Glow(OpeningLamp, 1.4f);
            var line = StartCoroutine(SayIds("BIA_07_N4"));       // There's a service opening above the glass... The control's beside you.
            yield return Wait(1.6f);
            yield return Shot("b24_the_upper_opening");
            Shoot(L.OpeningControl + new Vector3(-1.6f, 1.4f, -1.8f), L.OpeningControl + Vector3.up * 1.0f, 36f, 0.4f);
            while (Dlg.Busy) yield return null;
            yield return Shot("b24b_the_control_beside_her_lit");
            // he glances up at the gap and gathers his feet under him (a clear close shot) - what Panther reads
            if (Ratwing != null) Shoot(Ratwing.position + Ratwing.forward * 3.2f + Ratwing.right * 1.4f + Vector3.up * 1.6f, Ratwing.position + Vector3.up * 1.4f, 34f, 0.01f);
            if (RRig != null) { RRig.Now = RatwingRig.Mode.Perched; for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f) { RRig.LookUp = Mathf.SmoothStep(0f, 1f, t); yield return null; } }
            yield return Wait(0.5f);
            if (RRig != null) for (float t = 0f; t < 1f; t += Time.deltaTime / 0.7f) { RRig.Gather = Mathf.SmoothStep(0f, 1f, t); yield return null; }
            yield return Shot("b24c_he_glances_up_and_gathers_his_feet");
            Shoot(P7 + new Vector3(-1.6f, 1.3f, -2.8f), L.UpperOpening, 40f, 0.3f);
            yield return Say("BIA_07", 6, 8);                    // He's looking at the upper opening. / Can he get through? / Yes. Close it now.
            Seven.Release();
            yield return Cutscene(false);
            Objective("Close the upper opening - now (X at its control)" + RelayLine);
            Glow(OpeningLamp, 1.4f);
            yield return Interact(L.OpeningControlView, "Close the opening", "close the upper opening", "The control beside the relay, right", new[] { L.OpeningControlView });
            if (RRig != null) { RRig.LookUp = 0f; RRig.Gather = 0f; }
            yield return Cutscene(true);
            AudioKit.Play("bia_partition", L.UpperOpening, 0.9f, 0f, AudioKit.Bus.Sfx, 60f);
            Shoot(P7 + new Vector3(-2f, 1.2f, -3.2f), L.UpperOpening + Vector3.down * 2f, 46f, 0.01f);
            if (UpperShutter != null) StartCoroutine(Drop(UpperShutter, 3.2f, 0f, 0.8f));
            // he finds it blocked and launches away along the only remaining route
            if (Ratwing != null) { yield return Wait(0.6f); RRig.Now = RatwingRig.Mode.Fly; StartCoroutine(FlyRat(new Vector3(L.UpperOpening.x, 0f, L.UpperOpening.z + 3f), 7f, 0.8f, RatwingRig.Mode.Fly)); yield return Wait(0.9f); AudioKit.Play("ratwing_strike", L.UpperOpening, 0.8f, 0f, AudioKit.Bus.Sfx, 40f); }
            yield return Shot("b25_the_opening_shut_he_finds_it_blocked");
            if (Ratwing != null) { yield return FlyRat(new Vector3(230f, 0f, 140f), 30f, 2.2f, RatwingRig.Mode.Fly); Ratwing.gameObject.SetActive(false); }
            Seven.Release(); Seven.Lock(true);
            TwoShot(P7 + Vector3.forward * 2f, Vector3.right, 3.2f, 1.2f, 1.3f, 44f, 0.5f);
            yield return Say("BIA_07", 9, 12);                   // How did you know? / He looked at the gap... / You really have been watching. / Mm-hmm.
            // the second district reconnects, the garden echo fades
            BiavehState.DoStep("relay2"); Glow(Relay2Lamp, 1.8f); Lights(DistrictLights2, true); Beacon(1, false);
            if (EchoGarden != null) EchoGarden.Set(false);
            AudioKit.Play("bia_relay_on", L.Relay2At, 1f, 0f, AudioKit.Bus.Sfx, 60f);
            Shoot(new Vector3(252f, 8f, 70f), new Vector3(262f, 2f, 90f), 54f, 0.01f);
            yield return Wait(1.6f);
            yield return Shot("b26_the_garden_clears_district_two_reconnects");
            Complete("BIA_07");
            Log($"relay 2 restored: the seam found, the upper opening shut on him - Relays {RelaysDone} / 3");
            yield return Cutscene(false);
        }

        // ------------------------------------------------------------------------------------------ BIA_08: the quiet passage
        public bool QuietInside { get; private set; }
        IEnumerator Passage()
        {
            FreeBike = true;
            Objective("The third relay - beyond the shielded passage, west of the garden" + RelayLine);
            if (!Riding) yield return GetBackOn();
            yield return RideRoute(L.RideToPassage, "to the quiet passage", 6f);
            yield return StopBike(1.4f);
            // she parks the bike at its entrance (QF23: never rolled through into the passage entry)
            if (Riding && Bike.transform.position.x < 209f) Bike.Place(L.PassagePark, 270f);
            if (Riding) { if (!Bike.Dismount()) Bike.Dismount(true); FollowCamera(); }
            yield return Cutscene(true);
            Seven.Place(L.PassageDoor + new Vector3(2.2f, 0f, 0f), Vector3.left); Seven.Lock(true);
            TwoShot(L.PassageDoor + new Vector3(3f, 0f, 0f), Vector3.forward, 3.4f, 1.3f, 1.3f, 44f, 0.01f);
            yield return Shot("b27_the_shielded_passage_the_bike_parked");
            yield return SayIds("BIA_08_N1");                    // QF23 (16a): the relay sits inside the archive's shielding - nothing reaches through it
            yield return Say("BIA_08", 1, 5);                    // I'll lose you beyond that door. / Only until... / Mm-hmm. / I know. I'll be careful. / I hadn't said anything.
            yield return SayIds("BIA_08_06");                    // You did the pause.
            yield return Cutaway("b28_aboard_my_paws_he_lifts_one", AboardCut.Mood.Paws, "him", 1.2f, "BIA_08_06a");   // My... paws? (looks down at his front paws, lifts one)
            yield return SayIds("BIA_08_06b");                   // Pause. The silence. (a laugh she can't hold in)
            yield return Cutaway("b28b_aboard_an_expressive_pause_paw_down", AboardCut.Mood.Composed, "him", 0.3f, "BIA_08_07");   // Ah. An expressive pause. (the paw set down very neatly)
            yield return SayIds("BIA_08_08");                    // See you on the other side.
            yield return Cutscene(false);
            FreeBike = false;
            Objective("Open the shielded door (X)" + RelayLine);
            yield return Interact(L.PassageDoor + new Vector3(1.4f, 0f, 0f), "Open the door", "open the shielded door", "The door at the end of the road, west", new[] { L.PassageDoor + new Vector3(1.4f, 0f, 0f) });
            yield return DoorMove(PassageDoor, true);
            ShieldThreshold(L.PassageDoor);                      // QF23 (16a): the shielding shows - a shimmering threshold, the link drops
            QuietInside = true; MusicSystem.Stop(2f);
            Log("BIA_08: inside the shielded passage - no Panther (no telepathy lines play in here), music down");
            // the short solo spatial puzzle: an airlock pair. Panel A opens door A; inside the lock panel B is dead - its
            // conduit coupling lies on the floor; telekinesis seats it; then panel B closes A behind her and opens B
            StartCoroutine(SayIds("BIA_08_N2"));                 // QF23 (12): An airlock. One door at a time.
            Objective("Get through the airlock" + RelayLine);
            yield return Interact(L.LockPanelA + new Vector3(0f, 0f, 1.2f), "Open the airlock", "airlock door A", "The panel beside the first airlock door", new[] { L.LockPanelA + new Vector3(0f, 0f, 1.2f) });
            Glow(PanelALamp, 1.4f);
            StartCoroutine(DoorMove(LockDoorA, true)); SetZone("LockDoorA", true);
            yield return Wait(0.6f);
            yield return Shot("b29_airlock_door_a_opens");
            yield return GuideWalk(new[] { new Vector3(191f, 0f, 66f) }, "into the airlock");
            var socket = L.LockPanelB + new Vector3(-1.2f, 0.9f, -0.2f);
            yield return SayIds("BIA_08_N3");                    // QF23 (12): No power. Its coupling's on the floor.
            yield return Telekinesis(socket, 7f, "seat the coupling (telekinesis)", "The panel inside the lock is dark: its coupling lies on the floor - lift it into the socket with telekinesis (RT)", PanelBLamp, null, true, null);
            Glow(PanelBLamp, 1.4f); AudioKit.Play("bia_seam", socket, 0.8f, 0f, AudioKit.Bus.Sfx, 20f);
            Log("BIA_08 puzzle: the coupling seated by telekinesis - panel B has power");
            yield return Shot("b30_the_coupling_seated_panel_b_lights");
            yield return Interact(L.LockPanelB + new Vector3(0f, 0f, -1.2f), "Cycle the airlock", "airlock door B", "Panel B: it closes the door behind you, then opens the next", new[] { L.LockPanelB + new Vector3(0f, 0f, -1.2f) });
            if (Flat(P7 - L.LockOuter).magnitude < 1.4f) Seven.Place(new Vector3(191f, 0f, 66f), Vector3.left);
            SetZone("LockDoorA", false); yield return DoorMove(LockDoorA, false);
            yield return DoorMove(LockDoorB, true); SetZone("LockDoorB", true);
            yield return Shot("b31_door_a_shut_behind_her_door_b_open");
            Objective("Restore the third relay (X)" + RelayLine);
            yield return Interact(L.Relay3View, "Restore the relay", "restore relay 3", "At the end of the passage", new[] { L.Relay3View });
            yield return Cutscene(true);
            Seven.Place(L.Relay3View, Vector3.left); Seven.Touch(L.Relay3At + Vector3.up * 1.2f);
            AudioKit.Play("bia_relay_on", L.Relay3At, 1f, 0f, AudioKit.Bus.Sfx, 60f);
            BiavehState.DoStep("relay3"); Glow(Relay3Lamp, 1.8f); Lights(DistrictLights3, true); Beacon(2, false);
            Shoot(L.Relay3At + new Vector3(3f, 1.7f, 2.4f), L.Relay3At + Vector3.up * 1.2f, 42f, 0.01f);
            yield return Wait(1.2f);
            yield return Shot("b32_relay_three_restored");
            // she opens the exit: the doors stand open back to the road
            StartCoroutine(DoorMove(LockDoorA, true)); SetZone("LockDoorA", true);
            QuietInside = false;
            Seven.Release(); Seven.Lock(true);
            yield return Say("BIA_08", 9, 9);                    // Panther?
            yield return Say("BIA_08", 10, 13);                  // Here. / Good. / How was the silence? / Overrated.
            yield return Wait(1.2f);                             // a pause before Nela interrupts
            yield return Say("BIA_08", 14, 14);                  // All three districts are connected. Ratwing is moving toward the archive.
            yield return Cutaway("b33_aboard_he_turns_to_the_moving_marker", AboardCut.Mood.Watch, "screen", 0.5f, "BIA_08_15");   // I have him.
            yield return Say("BIA_08", 16, 16);                  // Let's keep it that way.
            Complete("BIA_08");
            Log($"relay 3 restored in the quiet passage - Relays {RelaysDone} / 3");
            MusicSystem.Play("biaveh_ratwing", 2f);
            yield return Cutscene(false);
            Objective("Back to the bike - Ratwing is moving toward the archive");
            FreeBike = true;
            Seven.ScriptRun = Auto;
            if (Auto) { yield return WalkPath(new[] { new Vector3(191f, 0f, 66f), new Vector3(204f, 0f, 66f), Bike.transform.position - Bike.transform.right * 1.1f }, 40f); PlaceRiding(Bike.transform.position); }
            yield return Until(() => Riding, "back on the bike", 60f, "Back through the passage to the bike at its entrance", Bike.transform.position, () => PlaceRiding(Bike.transform.position));
            Seven.ScriptRun = false;
        }

        // ------------------------------------------------------------------------------------------ the bike hold (as the asteroid)
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
            yield return Shot("b01b_the_bike_hold_open");
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
            Log("the bike came out of the bike hold onto its pad; the hold closed seamlessly (7 comes down in the beam)");
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
            Log("going home the bike rose back into the bike hold; it closed seamlessly (7 boards in the beam)");
        }

        // ------------------------------------------------------------------------------------------ Panther aboard (a cut)
        public Transform InsetCam, InsetCat; public CatPoseOverlay InsetPose;
        /// <summary>A cut aboard the saucer: Panther at the console (the small set far below the city - his seat, the console
        /// arc, the window glow). twitch = his tail-tip twitches, then stops (BIA_04_08 / BIA_05_12).</summary>
        IEnumerator AboardInset(string shot, bool twitch)
        {
            if (InsetCam == null || InsetCat == null) yield break;
            Shoot(InsetCam.position, InsetCat.position + Vector3.up * 0.25f, 34f, 0.01f);
            if (twitch && InsetPose != null) InsetPose.TailTwitch = 1f;
            yield return Wait(0.5f);
            yield return Shot(shot);
            yield return Wait(twitch ? 1.0f : 0.6f);
            if (InsetPose != null) InsetPose.TailTwitch = 0f;        // it stops
            Log($"cut aboard: Panther at the console{(twitch ? " - his tail-tip twitches, then stops" : "")}");
        }

        // ------------------------------------------------------------------------------------------ the fog round the action
        float _fog0Dist = -1f, _fog0Max, _fog0Ground;
        float _rvD, _rvM, _rvG, _rvF;
        /// <summary>BIA_03's wide reveal: the haze thins a lot for the moment the city is shown curving up the shell.</summary>
        IEnumerator RevealAir(bool on)
        {
            if (Atmo == null) yield break;
            if (on) { _rvD = Atmo.DistanceDensity; _rvM = Atmo.MaxFog; _rvG = Atmo.GroundDensity; _rvF = Atmo.FarBrightness; }
            float d0 = Atmo.DistanceDensity, m0 = Atmo.MaxFog, g0 = Atmo.GroundDensity, f0 = Atmo.FarBrightness;
            float d1 = on ? _rvD * 0.22f : _rvD, m1 = on ? _rvM * 0.5f : _rvM, g1 = on ? _rvG * 0.3f : _rvG, f1 = on ? _rvF * 0.8f : _rvF;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Atmo.DistanceDensity = Mathf.Lerp(d0, d1, u); Atmo.MaxFog = Mathf.Lerp(m0, m1, u); Atmo.GroundDensity = Mathf.Lerp(g0, g1, u); Atmo.FarBrightness = Mathf.Lerp(f0, f1, u);
                yield return null;
            }
        }
        IEnumerator FogThin(float k, float secs = 1.2f)
        {
            if (Atmo == null) yield break;
            if (_fog0Dist < 0f) { _fog0Dist = Atmo.DistanceDensity; _fog0Max = Atmo.MaxFog; _fog0Ground = Atmo.GroundDensity; }
            float d0 = Atmo.DistanceDensity, m0 = Atmo.MaxFog, g0 = Atmo.GroundDensity;
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, secs))
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                Atmo.DistanceDensity = Mathf.Lerp(d0, _fog0Dist * (1f - 0.55f * k), u); Atmo.MaxFog = Mathf.Lerp(m0, _fog0Max * (1f - 0.25f * k), u);
                Atmo.GroundDensity = Mathf.Lerp(g0, _fog0Ground * (1f - 0.5f * k), u);
                yield return null;
            }
        }

        /// <summary>QF23 (16a): the archive's shielding made visible - a shimmering white sheet across the doorway as she passes
        /// through, and the guide marker flickering out (the link drops: Panther can't reach her in here).</summary>
        void ShieldThreshold(Vector3 at)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(g.GetComponent<Collider>()); g.name = "ShieldThreshold";
            g.transform.position = at + Vector3.up * 2.1f; g.transform.rotation = Quaternion.LookRotation(Vector3.right); g.transform.localScale = new Vector3(5.2f, 4.2f, 1f);
            var r = g.GetComponent<Renderer>(); r.sharedMaterial = Markers.Additive(0.25f); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            StartCoroutine(Shimmer(r));
            Log("BIA_08 (QF23 16a): the shielding - a shimmering threshold across the doorway; the marker drops out inside");
        }
        IEnumerator Shimmer(Renderer r)
        {
            var mpb = new MaterialPropertyBlock();
            for (float t = 0f; r != null && (QuietInside || t < 2f); t += Time.deltaTime)
            {
                r.GetPropertyBlock(mpb); mpb.SetFloat("_Intensity", 0.18f + 0.12f * Mathf.Sin(t * 5f) + 0.05f * Mathf.PerlinNoise(t * 7f, 0f)); r.SetPropertyBlock(mpb);
                if (QuietInside && t < 1.2f && NextMarker.HasValue) UI.SetMarker(Mathf.Repeat(t, 0.3f) < 0.15f ? NextMarker : null);   // the marker flickers out
                yield return null;
            }
        }
    }
}
