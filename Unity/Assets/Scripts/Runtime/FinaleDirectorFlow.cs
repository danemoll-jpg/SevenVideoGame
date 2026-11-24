using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using L = Seven.FinaleLayout;

namespace Seven
{
    /// <summary>Round 19 — the finale's flow, FIN_02 (beneath the lights) to FIN_07 (the network opens). FIN_08+ in FinaleDirectorEnd.</summary>
    public partial class FinaleDirector
    {
        /// <summary>The music the guard brings back while she has control (MusicSystem.Theme("Finale")).</summary>
        public string ThemeNow { get; private set; } = "moon_descent";

        IEnumerator Flow(int cp)
        {
            Restore(cp);
            yield return null;
            if (cp > L.CpBerth) { StartCoroutine(UI.Fade(0f, 1.2f)); if (ThemeNow == null) MusicSystem.Silence("FIN_10 - restored after the leap", 240f); else { MusicSystem.EndSilence(); MusicSystem.Play(ThemeNow, 2f); } }   // Continue / Scene Select: out of black at once
            if (cp <= L.CpBerth) { yield return Arrive(); Checkpoint(L.CpJunction); }
            if (cp <= L.CpJunction) { yield return Junction(); Checkpoint(L.CpDoor); }
            if (cp <= L.CpDoor) { yield return TheDoor(); Checkpoint(L.CpChamber); }
            if (cp <= L.CpChamber) { yield return Runes(); yield return Witness(); yield return Network(); Checkpoint(L.CpConfront); }
            if (cp <= L.CpConfront) { yield return Confrontation(); yield return TheLeap(); Checkpoint(L.CpAfter); }
            // QF31 (A): FIN_10 -> her grief + the memories of Panther + the archive taking them -> FIN_12 "Received" -> the light lifts them
            // both -> heaven (the carry, "You're staying with me", the walk back to the archive and the separate memory step are gone)
            if (cp <= L.CpAfter) { yield return EightMore(); yield return Grieve(); Checkpoint(L.CpEnding); }
            yield return RisenTogether();
            yield return HeavenQF31();
            yield return Credits();
        }

        /// <summary>The state a real playthrough has at checkpoint cp (Continue / Scene Select / check runs).</summary>
        void Restore(int cp)
        {
            ThemeNow = cp >= L.CpEnding ? "moon_transfer" : cp == L.CpAfter ? null : cp >= L.CpConfront ? "moon_witness" : "moon_descent";
            bool gate = cp > L.CpJunction || FinaleState.StepDone("gate");
            SetZone("GateZ", gate); SetZone("DoorZ", cp > L.CpDoor);
            if (GateLeaf != null) GateLeaf.position = L.GateAt + (gate ? Vector3.up * 3.6f : Vector3.zero);
            if (GateLockT != null) GateLockT.gameObject.SetActive(!gate);
            if (PowerLever != null && (cp > L.CpJunction || FinaleState.StepDone("power"))) PowerLever.localRotation = Quaternion.Euler(40f, 0f, 0f);
            Glow(PowerPanelLamp, cp > L.CpJunction || FinaleState.StepDone("power") ? 1.6f : 0.1f);
            if (cp > L.CpDoor) OpenDoorNow();
            int placed = cp > L.CpChamber ? 7 : 0;
            _runes = new List<Transform>();
            for (int i = 0; i < 7; i++)
            {
                var r = MakeRune(i); r.gameObject.SetActive(i < placed);
                if (i < placed) r.position = L.SocketTop(i) + Vector3.up * 0.16f;
                Glow(i < SocketLights.Length ? SocketLights[i] : null, i < placed ? 1.4f : 0f);
            }
            Glow(PlatformSurface, cp > L.CpChamber ? 0.3f : 0.05f);
            Glow(ConduitGlow, cp > L.CpChamber ? 0.55f : 0f); Glow(ConduitBreak, cp > L.CpChamber ? 0.55f : 0f);
            if (CityTerminals != null) foreach (var t in CityTerminals) Glow(t, cp > L.CpChamber ? 1.0f : 0.15f);
            if (ProjectionWalls != null) for (int i = 0; i < ProjectionWalls.Length; i++) ShowWall(i, cp > L.CpChamber ? 1f : 0f);
            if (PaleLight != null) PaleLight.enabled = false;
            if (BarrierField != null) BarrierField.gameObject.SetActive(cp > L.CpConfront);
            if (TunnelerCraft != null)
            {
                TunnelerCraft.gameObject.SetActive(cp > L.CpChamber && cp <= L.CpAfter);
                if (cp > L.CpConfront) { TunnelerCraft.position = L.BarrierAt + new Vector3(0.6f, 0f, 0f); TunnelerCraft.rotation = Quaternion.LookRotation(Vector3.left); }
            }
            DrillSpeed = cp > L.CpConfront ? 0f : 160f;
            if (MiningDrills != null) _drillsOn = cp <= L.CpChamber;
            _drillsOn = cp <= L.CpChamber; if (!_drillsOn) StopWorldDrillsNow();                                  // QF30 (6): stopped in FIN_07, stay stopped
            if (cp <= L.CpConfront && !FinaleState.SceneDone("FIN_08_entrance")) BuildPlug();                    // QF30 (6): the mining access is blocked until he breaks through
            Glow(PowerCable, cp > L.CpJunction || FinaleState.StepDone("power") ? 1.0f : 0f);
            Glow(BarrierCable, cp > L.CpConfront || FinaleState.StepDone("barrier") ? 1.0f : 0f);
            Glow(BarrierFrameLines, cp > L.CpConfront ? 1.4f : 0.08f); Glow(BarrierLines, cp > L.CpConfront ? 1.0f : 0.12f); Glow(BarrierLight, cp > L.CpConfront ? 1.6f : 0.15f);
            Glow(ConduitLines, cp > L.CpChamber ? 0.7f : 0.1f); Glow(ConduitNode, cp > L.CpChamber ? 0.9f : 0.1f);
            if (cp > L.CpConfront) { _rigUp = false; _sag = 7f; if (Rig != null) Rig.PowerDown(); Restrained(cp >= L.CpEnding); if (_barrierHum == null) _barrierHum = AudioKit.Loop("bia_barrier", null, L.BarrierAt + Vector3.up * 2f, 0.22f, AudioKit.Bus.Sfx, 2f, 20f); }   // QF30 (16): contained in the barrier from FIN_09 on
            if (Crew != null) for (int i = 0; i < Crew.Length; i++) if (Crew[i] != null) { bool on = cp >= L.CpEnding; Crew[i].gameObject.SetActive(on); if (on) { Crew[i].transform.position = L.CrewAt(i); Crew[i].transform.rotation = Quaternion.LookRotation(Flat(L.BarrierAt - L.CrewAt(i))); } }
            if (Dodecahedron != null) Dodecahedron.gameObject.SetActive(false);
            if (cp >= L.CpChamber) ChamberLook();
            // where she starts
            Vector3 at; Vector3 face;
            switch (cp)
            {
                case L.CpJunction: at = new Vector3(11.5f, L.Y, -101f); face = Vector3.back; break;
                case L.CpDoor: at = new Vector3(11.5f, L.Y, -134f); face = Vector3.back; break;
                case L.CpChamber: at = new Vector3(11.5f, L.Y, -150.8f); face = Vector3.back; break;
                case L.CpConfront: at = L.HerFightStart; face = Vector3.back; break;
                case L.CpAfter: at = L.KneelSpot + new Vector3(0.9f, 0f, 0.6f); face = Flat(L.PantherHead - L.KneelSpot); break;
                case L.CpEnding: at = L.KneelSpot; face = Flat(L.PantherHead - L.KneelSpot); break;          // QF31: she is still kneeling over him
                default: at = L.BeamDown + new Vector3(0f, 0f, -2f); face = Vector3.back; break;
            }
            Seven.Place(at, face);
            if (Cat != null)
            {
                Cat.gameObject.SetActive(true);
                if (cp < L.CpAfter) { Cat.Place(at + new Vector3(0.9f, 0f, 0.4f), face); Cat.Follow(); }
                else if (cp == L.CpAfter) PantherFallen(true);                                                       // QF30 (15): large, lying on his side where he fell
                else { PantherFallen(false); var o = CatOv; if (o != null) { o.Breath = 0f; o.Alive = false; o.Slack = 1f; } }   // QF31: small, still, where he fell - she takes him with her
            }
            if (Worker != null) { Worker.gameObject.SetActive(cp <= L.CpChamber || cp >= L.CpEnding); }
            QF31Restore(cp);
            FollowCamera(); Cam.SnapBehind();
            Log($"restore: {FinaleState.Summary()} -> at {at}");
        }
        List<Transform> _runes = new List<Transform>();
        bool _drillsOn = true;
        Transform MakeRune(int i)
        {
            var rec = RecordSet.All[i];
            var go = new GameObject("Rune_" + rec.Name);
            float r = RuneR(i);
            go.AddComponent<MeshFilter>().sharedMesh = rec.Mesh(r);
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = RuneMat();
            go.transform.position = L.SocketTop(i) + Vector3.up * 0.16f;
            _runes.Add(go.transform);
            return go.transform;
        }
        static float RuneR(int i) => L.SocketRunes[i] == "sphere" ? RuneSizes.SphereR * 1.2f : L.SocketRunes[i] == "octahedron" ? RuneSizes.OctaR * 0.7f : 0.13f;
        Material _runeMat;
        Material RuneMat()
        {
            if (_runeMat != null) return _runeMat;
            var sh = Shader.Find("Seven/DissolveLit") ?? Shader.Find("Universal Render Pipeline/Lit");
            _runeMat = new Material(sh) { name = "Fin_rune" };
            if (_runeMat.HasProperty("_BaseColor")) _runeMat.SetColor("_BaseColor", new Color(0.82f, 0.82f, 0.82f));
            _runeMat.EnableKeyword("_EMISSION"); if (_runeMat.HasProperty("_EmissionColor")) _runeMat.SetColor("_EmissionColor", Color.white * 0.25f);
            return _runeMat;
        }
        void ShowWall(int i, float a)
        {
            if (ProjectionWalls == null || i >= ProjectionWalls.Length || ProjectionWalls[i] == null) return;
            var r = ProjectionWalls[i]; r.enabled = a > 0.01f;
            var tex = WallTex(i);                                                    // QF31 (9): its vision playing (looping, muted), else the still
            var m = r.material;
            if (m.HasProperty("_TexA")) { m.SetTexture("_TexA", tex); float wa = 3.2f / 4.4f, ia = tex != null ? tex.width / (float)tex.height : 1.6f; m.SetVector("_RectA", new Vector4(1f, Mathf.Clamp01(wa / ia), 0f, 0f)); m.SetFloat("_Bright", 1.6f * a); m.SetFloat("_Bg", 0.02f * a); }
            else m.mainTexture = tex;
        }
        void OpenDoorNow()
        {
            if (DoorLeafL != null) DoorLeafL.position = new Vector3(11.5f - 1.15f - 2.2f, L.Y, -142f);
            if (DoorLeafR != null) DoorLeafR.position = new Vector3(11.5f + 1.15f + 2.2f, L.Y, -142f);
        }

        void Update()
        {
            // the mining machinery outside the chamber: drilling until the Scree stop it (FIN_07)
            if (MiningDrills != null) foreach (var d in MiningDrills) if (d != null) { _drillRate = Mathf.MoveTowards(_drillRate, _drillsOn ? 180f : 0f, 40f * Time.deltaTime); d.Rotate(Vector3.up, _drillRate * Time.deltaTime, Space.Self); }
            TickWallDrills(); TickBeacons(); TickWalls();                                                        // QF31 (9): the screens' loops                                                                       // QF30 (6 / 9)
        }
        float _drillRate = 180f;

        // ------------------------------------------------------------------------------------------ the city look (as Chapter 1's 6b)
        /// <summary>Inside the archive chamber: the city's ground fog thinned (it flattened every chamber shot to one grey), the far haze kept.</summary>
        void ChamberLook()
        {
            var a = Atmo; if (a == null || _chamberLook) return; _chamberLook = true;
            a.FogStart = 30f; a.GroundDensity = 0.0015f; a.GroundRange = 12f; a.NearBrightness = 0.05f;
            Log("the chamber's look: ground fog thinned (density 0.0015, fog from 30 m)");
        }
        bool _chamberLook;
        void CityLookNow()
        {
            var a = Atmo; if (a == null) return;
            a.FogStart = 8f; a.GroundDensity = 0.010f; a.GroundBase = L.Y; a.GroundFalloff = 2.2f; a.GroundRange = 45f; a.ValleyDensity = 0.0026f; a.ValleyBase = -200f; a.ValleyFalloff = 120f;
            a.DistanceDensity = 0.00021f; a.HazeHeight = 6000f; a.SkyDistance = 7000f; a.NearBrightness = 0.12f; a.FarBrightness = 1.2f; a.BrightnessDistance = 2200f; a.ZenithBrightness = 1.0f; a.GlowIntensity = 0.38f; a.GlowRadius = 34f; a.MaxFog = 0.97f;
            var glowPos = new[] { new Vector3(-200f, 1900f, -1600f), new Vector3(16f, -120f, -900f), new Vector3(-700f, 200f, -1700f), new Vector3(800f, 300f, -1500f), new Vector3(16f, 900f, -3600f), new Vector3(-1500f, 600f, -400f), new Vector3(1500f, 500f, -300f), new Vector3(16f, 2600f, 300f) };
            var glowScale = new[] { new Vector3(36f, 2.2f, 1f), new Vector3(6f, 0.7f, 1f), new Vector3(9f, 0.8f, 1f), new Vector3(9f, 0.8f, 1f), new Vector3(18f, 1.2f, 1f), new Vector3(12f, 0.8f, 1f), new Vector3(12f, 0.8f, 1f), new Vector3(24f, 1.0f, 1f) };
            int k = 0; foreach (Transform c in a.transform) if (c.name.StartsWith("Glow") && k < glowPos.Length) { c.position = glowPos[k]; c.localScale = glowScale[k]; k++; }
            foreach (Transform c in a.transform) if (c.name == "BackLight") { c.rotation = Quaternion.Euler(68f, 205f, 0f); }
            var rep = a.FitFogBudget(0.98f, 0.95f);
            Log("FOG BUDGET (QF26 rule) the finale's city look: " + rep);
        }

        // ------------------------------------------------------------------------------------------ FIN_02: beneath the lights
        IEnumerator Arrive()
        {
            yield return Cutscene(true);
            CityLookNow();
            Seven.gameObject.SetActive(false); if (Cat != null) Cat.gameObject.SetActive(false);
            // the saucer berthed in the sheltered service berth; the reveal first, from the berth's edge (Chapter 1's 6b shot, reversed)
            MusicSystem.EndSilence(); MusicSystem.Play("main_theme", 2.5f);
            var dir = new Vector3(-0.62f, 0f, -0.78f).normalized; var side = Vector3.Cross(Vector3.up, dir);
            var sky = L.Overlook + dir * 420f + Vector3.up * 105f;
            Shoot(L.Overlook - dir * 2.6f - side * 0.7f + Vector3.up * 1.5f, L.Overlook + dir * 20f + Vector3.up * 5f, 58f, 0.01f);
            Seven.gameObject.SetActive(true); Seven.Place(L.Overlook, dir); Seven.Lock(true);
            if (Cat != null) { Cat.gameObject.SetActive(true); Cat.Place(L.Overlook + side * 0.95f, dir); Cat.Hold(L.Overlook + dir * 20f, "sit"); }
            // (the beam comes first in time: shown after the reveal is established - she has just come down it)
            yield return UI.Fade(0f, 1.6f);
            ImageGate.Expect(Seven.transform, "7 looking out at the hollow Moon's city");
            yield return Wait(1.6f);
            yield return Shot("f01_over_her_shoulder_the_city_again");
            // what has changed: the mining towers cutting into the city
            var drill = MiningDrills != null && MiningDrills.Length > 0 ? MiningDrills[0] : null;
            if (drill != null)
            {
                Shoot(drill.position + new Vector3(16f, 92f, 22f), drill.position + Vector3.up * 70f, 52f, 0.01f);
                ImageGate.Expect(drill.parent, "a mining tower cutting into the city");
                yield return Wait(0.6f);
                yield return Shot("f02_the_mining_cutting_into_the_city");
            }
            var mid = (P7 + Cat.transform.position) * 0.5f;
            Shoot(mid + dir * 2.9f + side * 0.9f + Vector3.up * 0.4f, mid + Vector3.up * 0.95f, 52f, 0.01f);
            yield return Wait(0.4f);
            yield return Shot("f03_two_shot_looking_out_together");
            yield return Say("FIN_02", 1, 2);                       // It's still beautiful. / Yes.
            if (drill != null) yield return DialogueFraming.Over("the mining lights she looks toward (QF24 7)", Say("FIN_02", 3, 3));   // Even knowing what's underneath it.
            else yield return Say("FIN_02", 3, 3);
            yield return Say("FIN_02", 4, 4);                       // Especially worth looking after, then.
            MusicSystem.Play("moon_descent", 4f); ThemeNow = "moon_descent";
            // the beam had lowered them both: the saucer hovers over the berth (the bike stays in its hold)
            Shoot(L.SaucerAt + new Vector3(9f, 2.2f, -9f), L.SaucerAt + new Vector3(0f, 2.6f, 0f), 50f, 0.01f);
            if (Saucer != null) ImageGate.Expect(Saucer, "the saucer over the service berth");
            yield return Wait(0.5f);
            yield return Shot("f04_the_saucer_over_the_service_berth");
            Seven.Place(L.Overlook + new Vector3(0f, 0f, -1.6f), Vector3.back);
            if (Cat != null) { Cat.Place(L.Overlook + new Vector3(0.9f, 0f, -1.2f), Vector3.back); }
            TwoShot((P7 + Cat.transform.position) * 0.5f, Vector3.right, 2.4f, 0.8f, 0.7f, 42f, 0.01f);
            yield return Say("FIN_02", 5, 7);                       // Coming with me this time? / ...within walking distance. / Heh. Come on.
            if (Cat != null) Cat.Follow();
            Complete("FIN_02");
            yield return Cutscene(false);
        }

        // ------------------------------------------------------------------------------------------ FIN_03: the people keeping it running
        IEnumerator Junction()
        {
            // she can see the worker at the damaged junction from the walkway
            Objective("Go down the service walkway");
            yield return GuideWalk(new[] { new Vector3(11.5f, L.Y, -99f), new Vector3(12.6f, L.Y, -106.5f) }, "down the service walkway");
            yield return Cutscene(true);
            if (Worker != null) { Worker.transform.position = L.WorkerAt; Worker.transform.rotation = Quaternion.LookRotation(Vector3.right); }
            Seven.Place(L.WorkerAt + new Vector3(-2.2f, 0f, 1.4f), Flat(L.WorkerAt - (L.WorkerAt + new Vector3(-2.2f, 0f, 1.4f))));
            if (Cat != null) Cat.Place(L.WorkerAt + new Vector3(-2.6f, 0f, 2.6f), Vector3.back);
            Shoot(L.WorkerAt + new Vector3(2.8f, 1.6f, 3.2f), L.WorkerAt + new Vector3(0.6f, 1.2f, -0.4f), 46f, 0.01f);
            yield return Wait(0.6f);
            yield return Shot("f05_a_scree_worker_repairing_the_junction");
            if (Worker != null) Worker.transform.rotation = Quaternion.LookRotation(Flat(P7 - Worker.transform.position));
            yield return Say("FIN_03", 1, 2);                       // You came back. / We're looking for a chamber beneath this level.
            // the worker looks toward the terminal with Tunneler's restricted markings
            Shoot(L.TerminalAt + new Vector3(-1.1f, 1.6f, 1.5f), L.TerminalAt + Vector3.up * 1.4f, 44f, 0.01f);
            if (TerminalScreen != null) ImageGate.Expect(TerminalScreen.transform, "the terminal with Tunneler's restricted markings");
            yield return DialogueFraming.Over("the terminal the worker looks at (QF24 7)", Say("FIN_03", 3, 3));   // That section is sealed.
            yield return Shot("f06_the_terminal_tunnelers_restricted_markings");
            TwoShot((P7 + L.WorkerAt) * 0.5f, Vector3.forward, 3.2f, 1.0f, 1.3f, 44f, 0.3f);
            yield return Say("FIN_03", 4, 12);                      // By Tunneler? ... We'll bring it back.
            // the junction objective comes from the worker: power through the panel, the rusted lock, the crossing
            Shoot(L.PowerPanel + new Vector3(4f, 2f, 2.4f), L.PowerPanel + Vector3.up * 1.2f, 46f, 0.3f);
            yield return DialogueFraming.Over("the panel she must switch (QF24 7)", SayIds("FIN_03_P1"));   // The power for the old route runs through that panel.
            Complete("FIN_03_lines");
            yield return Cutscene(false);
            // 1. reroute the power: the lever, the panel lamp, the light runs along the route to the gate
            if (!FinaleState.StepDone("power"))
            {
                Objective("Switch the old route's power back on at the panel");
                var beacon = Highlight("the power panel", L.PowerPanel + new Vector3(0.3f, 0f, 0f), PowerCable, PowerPanelLamp);
                yield return Interact(L.PowerPanel + new Vector3(1.0f, 0f, 0f), "Switch the power", "the power panel", "The panel on the left wall of the junction", new[] { L.PowerPanel + new Vector3(1.2f, 0f, 0.2f) });
                BeaconDone(beacon, 1.6f);
                yield return Cutscene(true);
                Seven.Touch(L.PowerPanel + Vector3.up * 1.2f);
                AudioKit.Play("ast_door_manual", L.PowerPanel + Vector3.up, 0.9f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f) { if (PowerLever != null) PowerLever.localRotation = Quaternion.Euler(Mathf.Lerp(-40f, 40f, t), 0f, 0f); yield return null; }
                Glow(PowerPanelLamp, 1.6f);
                Shoot(L.PowerPanel + new Vector3(3.6f, 1.8f, 1.6f), L.PowerPanel + Vector3.up * 1.2f, 44f, 0.01f);
                yield return Wait(0.5f);
                yield return Shot("f07_the_power_back_the_panel_lit");
                FinaleState.DoStep("power"); Log("FIN_03: the old route's power rerouted (the lever down, the panel lamp lit)");
                yield return Cutscene(false);
            }
            // 2. the rusted gate lock: they were never strong enough to lift it - telekinesis lifts it away
            if (!FinaleState.StepDone("gate"))
            {
                yield return SayIds("FIN_03_P2");                    // The gate lock rusted shut years ago. We were never strong enough to lift it.
                Objective("Lift the rusted lock off the gate");
                var lockBeacon = Highlight("the rusted gate lock", L.GateAt + new Vector3(0f, 0f, 0.6f), null, GateLockT != null ? GateLockT.GetComponentInChildren<Renderer>() : null);
                yield return Telekinesis(L.GateLock, 9f, "the rusted gate lock", "The lock above the gate - telekinesis", null, new[] { new Vector3(11.5f, L.Y, -115f) }, GateLockT);
                yield return Cutscene(true);
                Shoot(L.GateAt + new Vector3(4.6f, 2.4f, 5.4f), L.GateAt + Vector3.up * 1.8f, 50f, 0.01f);
                ImageGate.Expect(GateLeaf, "the gate");
                var a = GateLockT.position;
                AudioKit.Play("tk_gather", a, 0.8f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 1.4f) { GateLockT.position = a + new Vector3(0f, Mathf.Sin(t * Mathf.PI * 0.5f) * 1.4f, 2.0f * t * t); GateLockT.rotation = Quaternion.Euler(t * 30f, t * 40f, 0f); yield return null; }
                yield return Shot("f08_the_lock_rises_in_her_telekinesis");
                var b = GateLockT.position; var land = new Vector3(14.2f, L.Y + 0.35f, -116.8f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f) { GateLockT.position = Vector3.Lerp(b, land, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.6f; yield return null; }
                AudioKit.Play("land_hard", land, 0.7f);
                Seven.SetEnergyPoint(0.6f, 0.6f);
                AudioKit.Play("ast_door_manual", L.GateAt + Vector3.up * 2f, 1f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 1.4f) { if (GateLeaf != null) GateLeaf.position = L.GateAt + Vector3.up * Mathf.SmoothStep(0f, 3.6f, t); yield return null; }
                SetZone("GateZ", true); FinaleState.DoStep("gate"); BeaconDone(lockBeacon, 0f);
                ImageGate.ExpectPoint(L.GateAt + Vector3.up * 1.6f, "the open gate");
                yield return Shot("f09_the_gate_rises");
                Log("FIN_03: the rusted lock lifted away by telekinesis, the gate rises");
                yield return Cutscene(false);
            }
            SetZone("GateZ", true);
            // 3. the short maintenance crossing (narrow, a drop either side)
            Objective("Cross the maintenance walkway");
            yield return GuideWalk(new[] { new Vector3(11.5f, L.Y, -122.4f) }, "to the crossing");
            if (Cat != null) StartCoroutine(SayIds("FIN_03_P3"));      // Narrow walkway. Watch the edge.
            StartCoroutine(CrossingShot());
            yield return GuideWalk(new[] { new Vector3(11.5f, L.Y, -127f), new Vector3(11.5f, L.Y, -134f) }, "across the crossing", 1.4f);
            // the worker's payoff: the old route is open
            yield return DialogueFraming.Over("the old route, open ahead (QF24 7)", SayIds("FIN_03_P4"));   // There. The old route is open. Go carefully.
            Complete("FIN_03");
        }
        IEnumerator CrossingShot()
        {
            yield return Wait(2.2f);
            yield return Shot("f10_the_narrow_maintenance_crossing");
        }

        // ------------------------------------------------------------------------------------------ FIN_04: the door from the memory
        IEnumerator TheDoor()
        {
            Objective("Go on to the old door");
            yield return GuideWalk(new[] { L.DoorView }, "to the ancient door", 1.8f);
            yield return Cutscene(true);
            Seven.Place(L.DoorView, Vector3.back); if (Cat != null) Cat.Place(L.DoorView + new Vector3(0.9f, 0f, 0.6f), Vector3.back);
            // vision_01's framing: low, straight on, the door filling the middle, its lintel marks catching the light
            Shoot(L.DoorView + new Vector3(0.6f, 0.9f, 3.4f), L.DoorAt + Vector3.up * 3.1f, 48f, 0.01f);
            ImageGate.Expect(DoorLeafL, "the ancient door (vision_01's framing)");
            yield return Wait(0.8f);
            yield return Shot("f11_the_door_from_the_memory_vision_framing");
            HerFaceCloseUp();
            yield return Say("FIN_04", 1, 3);                       // I know this door. / From the first vision? / Yes.
            // FIN_04_04: the door vision's own shimmer (no line)
            var sh = AudioKit.Loop("vision_shimmer", null, null, 0f, AudioKit.Bus.Sfx); if (sh != null) { StartCoroutine(AudioKit.Fade(sh, 0.35f, 0.8f)); }
            Glow(DoorMarkings, 1.2f);
            yield return Wait(1.6f);
            if (sh != null) StartCoroutine(AudioKit.Fade(sh, 0f, 1.2f, true));
            yield return Say("FIN_04", 5, 6);                       // They weren't surprised to see me. / No.
            yield return Cutscene(false);
            // the player touches the door (never opens by itself)
            Objective("Touch the door");
            yield return Interact(L.DoorAt + new Vector3(0f, 0f, 1.0f), "Touch the door", "the ancient door", "The door ahead - touch it", new[] { L.DoorAt + new Vector3(0f, 0f, 1.3f) }, null, 2.4f);
            yield return Cutscene(true);
            Seven.Place(L.DoorAt + new Vector3(0.4f, 0f, 0.95f), Vector3.back);
            Seven.Touch(L.DoorMark);
            Shoot(L.DoorAt + new Vector3(2.6f, 1.6f, 2.2f), L.DoorMark, 40f, 0.01f);
            yield return Say("FIN_04", 7, 7);                       // I think I belonged here.
            yield return Wait(0.5f);
            yield return Shot("f12_her_hand_on_the_door");
            // the dodecahedron answers from her pocket: it rises in her glow, the marks light, the leaves part
            if (Dodecahedron != null) { Dodecahedron.gameObject.SetActive(true); Dodecahedron.position = P7 + Vector3.up * 1.05f + Seven.transform.forward * 0.25f; }
            AudioKit.Play("tk_gather", P7 + Vector3.up, 0.8f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f) { if (Dodecahedron != null) { Dodecahedron.position += Vector3.up * Time.deltaTime * 0.35f; Dodecahedron.Rotate(0f, 60f * Time.deltaTime, 0f); } Glow(DoorMarkings, 1.2f + t * 1.6f); yield return null; }
            Shoot(L.DoorView + new Vector3(0.5f, 1.2f, 1.6f), L.DoorAt + Vector3.up * 2.6f, 50f, 0.01f);
            AudioKit.Play("ast_door_manual", L.DoorAt + Vector3.up * 2f, 1f);
            var la = DoorLeafL.position; var ra = DoorLeafR.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.4f) { float u = Mathf.SmoothStep(0f, 1f, t); DoorLeafL.position = la + Vector3.left * 2.2f * u; DoorLeafR.position = ra + Vector3.right * 2.2f * u; if (t > 0.5f && Dodecahedron != null) Dodecahedron.gameObject.SetActive(false); yield return null; }
            if (Dodecahedron != null) Dodecahedron.gameObject.SetActive(false);
            ImageGate.ExpectPoint(L.DoorAt + Vector3.up * 2.4f, "the open doorway");
            yield return Shot("f13_the_door_opens");
            SetZone("DoorZ", true);
            Log("FIN_04: she touched the door; the dodecahedron answered, the leaves parted (the door was closed until she touched it)");
            Complete("FIN_04");
            yield return Cutscene(false);
        }

        // ------------------------------------------------------------------------------------------ FIN_05: seven places, one history
        IEnumerator Runes()
        {
            Objective("Go into the chamber");
            yield return GuideWalk(new[] { new Vector3(11.5f, L.Y, -151.4f) }, "into the chamber", 1.8f);
            ChamberLook();
            yield return Cutscene(true);
            Seven.Place(new Vector3(11.5f, L.Y, -151.6f), Vector3.back); if (Cat != null) Cat.Place(new Vector3(12.4f, L.Y, -151.2f), Vector3.back);
            Shoot(L.ChamberC + new Vector3(-9.5f, 6.2f, 9.8f), L.ChamberC + Vector3.up * 1.6f, 56f, 0.01f);
            ImageGate.ExpectPoint(L.ChamberC + Vector3.up * 1f, "the archive chamber: the platform and the seven sockets");
            yield return Wait(1.2f);
            yield return Shot("f14_the_archive_chamber_seven_sockets");
            yield return DialogueFraming.Over("the sockets round the chamber (QF24 7)", SayIds("FIN_05_P1"));   // One for every place we've been.
            yield return Cutscene(false);
            // one quick placement each: the Moon's at the entrance first, then round the circle
            var t0 = Time.time;
            for (int i = 0; i < 7; i++)
            {
                if (FinaleState.RunesPlaced > i) continue;
                Objective($"Place the {L.SocketRunes[i]}  -  {i} / 7");
                var spot = L.Socket(i) + Flat(L.ChamberC - L.Socket(i)).normalized * 1.05f;
                var sb = Highlight($"socket {i + 1}", L.Socket(i), null, i < SocketLights.Length ? SocketLights[i] : null);
                yield return Interact(L.Socket(i), $"Place the {L.SocketRunes[i]}", $"socket {i + 1} ({L.SocketWorlds[i]})", "A socket round the chamber's edge", new[] { spot }, null, 1.9f);
                BeaconDone(sb, 0.4f);
                yield return PlaceRune(i);
            }
            Objective(null);
            Log($"RUNE PLACEMENT (Round 19 5): 7 of 7 placed in {Time.time - t0:0.0} s (must be under a minute of play)");
            yield return Cutscene(true);
            Shoot(L.ChamberC + new Vector3(8.6f, 4.2f, 8.2f), L.ChamberC + Vector3.up * 2.2f, 58f, 0.01f);
            ScreenViewCheck("all seven lit (the chamber, wide)", L.ChamberC + new Vector3(8.6f, 4.2f, 8.2f));
            ScreenViewCheck("all seven lit (from the entrance)", new Vector3(11.5f, L.Y + 1.6f, -151.6f));
            ScreenViewCheck("all seven lit (from the archive)", L.Stand(0f) + Vector3.up * 1.6f);
            Seven.Place(L.PlatformAt + new Vector3(0f, 0f, L.PlatformR + 1.6f), Vector3.back);
            if (Cat != null) Cat.Place(L.PlatformAt + new Vector3(1.0f, 0f, L.PlatformR + 1.9f), Vector3.back);
            ImageGate.ExpectPoint(L.ChamberC + Vector3.up * 2f, "the chamber assembling its display");
            yield return Wait(0.8f);
            yield return Shot("f22_the_seven_records_align_the_walls_lit");
            TwoShot((P7 + Cat.transform.position) * 0.5f, Vector3.right, 2.6f, 0.9f, 0.9f, 44f, 0.3f);
            yield return Say("FIN_05", 1, 4);                       // We carried all of this... / We knew some of it. / Enough to keep going. / Mm-hmm.
            yield return BarrierSetUp();                            // QF30 (10): FIN_05_04a-c, the barrier shown as it is mentioned
            // the platform responds to her: the Archive (a recorded system voice, no figure)
            Glow(PlatformSurface, 0.4f);
            AudioKit.Play("europa_seal_tone", L.PlatformAt + Vector3.up, 0.7f);
            Shoot(L.PlatformAt + new Vector3(2.4f, 2.4f, 4.8f), L.PlatformAt + Vector3.up * 1.0f, 44f, 0.3f);
            ImageGate.Expect(PlatformSurface.transform, "the archive surface lighting");
            yield return DialogueFraming.Over("the archive surface (the Archive's voice: no figure)", Say("FIN_05", 5, 6));   // Records received. / Witness recognized.
            yield return Shot("f23_the_archive_surface_lights");
            HerFaceCloseUp();
            yield return Say("FIN_05", 7, 7);                       // Witness?
            Complete("FIN_05");
            yield return Cutscene(false);
        }
        /// <summary>One placement: she takes the piece in both hands (real contact), sets it into the socket's cup, the socket lights,
        /// its world shows on the wall behind it for a moment with a phrase of that chapter's sound.</summary>
        IEnumerator PlaceRune(int i)
        {
            yield return Cutscene(true);
            var s = L.Socket(i); var toS = Flat(s - L.ChamberC).normalized; var stand = s - toS * 0.95f;
            Seven.Place(stand, toS);
            var rune = _runes[i]; rune.gameObject.SetActive(true);
            var hold = HoldPose.On(Seven.gameObject);
            rune.position = P7 + Vector3.up * 1.05f + toS * 0.34f;
            hold.Hold(rune, true, true, $"the {L.SocketRunes[i]}", RuneR(i));
            // QF30 (2): Panther waits on the far side of her from the camera, and the shot is the first framing with her hands, the rune
            // and the socket all in clear view (rc1's fixed angle had a column / pedestal between the camera and the third socket)
            var cam = RuneShot(i, stand, toS);
            if (Cat != null) Cat.Place(stand + Flat(stand - cam).normalized * 0.9f - toS * 0.4f, toS);
            ImageGate.Expect(Sockets != null && i < Sockets.Length ? Sockets[i] : Seven.transform, $"socket {i + 1} and the {L.SocketRunes[i]} in her hands");
            yield return Wait(0.45f);
            yield return Shot($"f15_rune_{i + 1}_{L.SocketRunes[i]}_in_her_hands");
            var a = rune.position; var b = L.SocketTop(i) + Vector3.up * 0.16f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.55f) { rune.position = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            hold.Release("placed in its socket");
            AudioKit.Play("europa_device_place", b, 0.8f);
            Glow(i < SocketLights.Length ? SocketLights[i] : null, 1.6f);
            PlayWall(i);                                                             // QF31 (9): its vision plays on its own screen and keeps looping there
            var wall = ProjectionWalls != null && i < ProjectionWalls.Length ? ProjectionWalls[i] : null;
            yield return Wait(0.35f);
            ImageGate.Expect(Sockets != null && i < Sockets.Length ? Sockets[i] : Seven.transform, $"the {L.SocketRunes[i]} set in socket {i + 1}");
            yield return Shot($"f15b_rune_{i + 1}_{L.SocketRunes[i]}_goes_in");
            if (wall != null) { Shoot(stand - toS * 1.0f + Vector3.Cross(Vector3.up, toS) * 1.4f + Vector3.up * 1.6f, Vector3.Lerp(b, wall.transform.position, 0.72f), 46f, 0.01f); ImageGate.Expect(wall.transform, $"{L.SocketWorlds[i]} on the wall"); }
            // a phrase of that world's sound
            string[] cue = { "vision_shimmer", "grass_rustle", "europa_call_deep", "mars_vision_stone", "ast_steam_whistle", "bia_barrier", "cat_gate_chime" };
            AudioKit.Play(cue[i], b, 0.6f);
            FinaleState.RunesPlaced = i + 1;
            Log($"rune {i + 1} / 7: the {L.SocketRunes[i]} ({L.SocketWorlds[i]}) set into its socket - the socket lights, {L.SocketWorlds[i]} on the wall behind it");
            yield return Wait(1.1f);
            yield return Shot($"f16_rune_{i + 1}_{L.SocketRunes[i]}_placed_{L.SocketWorlds[i].Replace(" ", "_")}");
            ScreenViewCheck($"rune {i + 1} placed (the shot)", Cam.transform.position);
            yield return Cutscene(false);
            yield return null; yield return null;
            ScreenViewCheck($"rune {i + 1} placed (control back)", Cam.transform.position);
        }

        // ------------------------------------------------------------------------------------------ FIN_06: she was there
        IEnumerator Witness()
        {
            Objective("Put your hand on the archive");
            var ab = Highlight("the archive surface", L.PlatformAt + new Vector3(0f, 0f, L.PlatformR * 0.6f), null, PlatformSurface);
            yield return Interact(L.Stand(0f), "Touch the archive", "the archive surface", "The central platform", new[] { L.Stand(0f) + new Vector3(0f, 0f, 0.1f) }, null, 2.2f);
            BeaconDone(ab, 0.3f);
            yield return Cutscene(true);
            MusicSystem.Play("moon_witness", 3f); ThemeNow = "moon_witness";
            Seven.Place(L.Stand(0f), Vector3.back);
            if (Cat != null) Cat.Place(L.Stand(-24f, L.StandR + 0.15f), Vector3.back);
            yield return HandOnSurface(true);
            Shoot(P7 + new Vector3(1.4f, 1.1f, 0.9f), P7 + new Vector3(0f, 0.95f, -0.5f), 38f, 0.01f);
            yield return Wait(0.6f);
            yield return Shot("f24_her_hand_on_the_archive");
            // the revelation montage: short fragments of the seven visions, back to her hand and breathing between them
            for (int i = 0; i < 7; i++)
            {
                yield return Fragment(i, 2.1f, $"f25_witness_video_frame_{i + 1}");
                if (i == 1) yield return Say("FIN_06", 1, 3);       // They weren't showing me someone else's life. / Seven? / I was there.
                else if (i == 4) yield return Say("FIN_06", 4, 5);  // The gardens. The bridge. That little bird... / I knew them.
                else { HandShot(i); yield return Wait(0.9f); }
            }
            // Panther moves close enough to touch her leg (the hold)
            if (Cat != null) { Cat.GoTo(P7 + new Vector3(0.35f, 0f, 0.3f), 0.6f, P7 + Vector3.up); }
            Shoot(P7 + new Vector3(1.8f, 0.6f, 1.6f), P7 + new Vector3(0.2f, 0.5f, 0f), 42f, 0.01f);
            yield return Wait(2.2f);
            yield return Shot("f26_panther_against_her_leg");
            yield return Say("FIN_06", 7, 8);                       // They're all gone. And I'm still here. / You're here.
            Shoot(L.ChamberC + new Vector3(-6f, 3f, 6f), L.ChamberC + new Vector3(4f, 3.2f, -6f), 54f, 0.6f);
            ImageGate.ExpectPoint(L.ChamberC + Vector3.up * 2.5f, "the display: records and the gaps between them");
            yield return DialogueFraming.Over("the display (the Archive's voice)", Say("FIN_06", 9, 10));   // Living testimony available. / Permission to preserve and share?
            yield return Shot("f27_the_display_the_gaps");
            HerFaceCloseUp();
            yield return Say("FIN_06", 11, 14);                     // That's why it's incomplete. ... Someone should.
            // the player confirms the release (one press) - inside the scene: her hand stays on the archive until her answer (QF30 3)
            Objective("Share it");
            bool yes = false;
            SetPrompt("Share it");
            float sh0 = Time.time;
            AwaitsChoice = true;
            while (!yes) { if (Input.Interact.WasPressedThisFrame() || (Auto && Time.time - sh0 > 1.2f)) yes = true; yield return null; }
            AwaitsChoice = false;
            SetPrompt(null); Objective(null);
            HerFaceCloseUp();
            yield return Say("FIN_06", 15, 15);                     // Yes. Share it.
            yield return HandOnSurface(false);                      // QF30 (3): the touch ends with her answer
            Log("FIN_06: the release confirmed by one press");
            Complete("FIN_06");
        }
        IEnumerator HandOnSurface(bool on)
        {
            var ik = Seven.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
            ik.enabled = true; ik.Alive = true; ik.Ease = 2.2f;
            if (on)
            {
                // QF30 (4): she stands clear of the platform's base (StandR) and leans in a little; the palm lies flat on the surface
                ik.RightPos = L.HandOnArchive; ik.RightPalmN = Vector3.up; ik.RightPalmW = 1f; ik.RightFingerDir = Vector3.back; ik.RightW = 1f;
                ik.Lean = 12f; ik.LeanDir = Flat(L.PlatformAt - P7);
                ik.LookPos = L.PlatformAt + Vector3.up * 1.2f; ik.LookW = 0.6f;
            }
            else ik.ReleaseAll("her hand off the archive");
            yield return Wait(0.6f);
        }
        void HandShot(int i)
        {
            var side = i % 2 == 0 ? 1f : -1f;
            Shoot(P7 + new Vector3(1.2f * side, 1.0f, 0.8f), P7 + new Vector3(0.1f, 0.95f, -0.5f), 36f, 0.01f);
        }
        /// <summary>A few seconds of one vision, full screen, then back.</summary>
        IEnumerator Fragment(int i, float secs, string shot, float from = 3f)
        {
            var clip = Visions != null && i < Visions.Length ? Visions[i] : null;
            if (clip == null || DemoBoot.NoVideo)
            {
                var still = Resources.Load<Texture2D>($"Finale/VisionStills/vis_0{i + 1}");
                if (still != null) { UI.SetVideo(still, still.width / (float)still.height, 1f); yield return Wait(0.3f); yield return Shot(shot); yield return Wait(secs - 0.3f); UI.SetVideo(null, 1f, 0f); }
                yield break;
            }
            var go = new GameObject("Fragment" + i); var vp = go.AddComponent<UnityEngine.Video.VideoPlayer>();
            vp.playOnAwake = false; vp.clip = clip; vp.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture; vp.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None; vp.timeUpdateMode = UnityEngine.Video.VideoTimeUpdateMode.GameTime;
            var rt = new RenderTexture((int)clip.width, (int)clip.height, 0); vp.targetTexture = rt;
            vp.Prepare(); for (float w = 0f; w < 3f && !vp.isPrepared; w += Time.deltaTime) yield return null;
            vp.time = Mathf.Min(from, (float)clip.length - secs - 0.2f); vp.Play();
            float asp = clip.width / (float)clip.height;
            for (float t = 0f; t < 0.35f; t += Time.deltaTime) { UI.SetVideo(rt, asp, t / 0.35f); yield return null; }
            UI.SetVideo(rt, asp, 1f);
            yield return Wait(0.5f);
            yield return Shot(shot);
            yield return Wait(Mathf.Max(0f, secs - 1.2f));
            for (float t = 0f; t < 0.35f; t += Time.deltaTime) { UI.SetVideo(rt, asp, 1f - t / 0.35f); yield return null; }
            UI.SetVideo(null, 1f, 0f); vp.Stop(); Destroy(go); rt.Release();
        }

        // ------------------------------------------------------------------------------------------ FIN_07: the network opens
        IEnumerator Network()
        {
            // QF31 (8): the release begins - the pedestals sink flush, the table lowers into a shallow light well: the room opens up
            Shoot(L.ChamberC + new Vector3(-9.6f, 5.6f, 8.8f), L.ChamberC + Vector3.up * 0.4f, 58f, 0.01f);
            ImageGate.ExpectPoint(L.ChamberC + Vector3.up * 0.5f, "the pedestals sinking, the table lowering into the floor");
            StartCoroutine(ShotAfter(2.0f, "f27b_the_pedestals_sink_the_table_lowers"));
            yield return SinkArchive(Auto ? 3f : 4.2f);
            Shoot(L.ChamberC + new Vector3(-9.6f, 5.6f, 8.8f), L.ChamberC + Vector3.up * 0.4f, 58f, 0.01f);
            ImageGate.ExpectPoint(L.ChamberC, "the open chamber: the light well, the flush sockets");
            yield return Wait(0.4f);
            yield return Shot("f27c_the_open_chamber_the_light_well");
            // light moves out through the old conduits; terminals across the city begin receiving; the Scree stations can talk
            Shoot(L.ChamberC + new Vector3(-8f, 2.2f, -6f), L.ChamberC + new Vector3(2f, 0.4f, 2f), 54f, 0.01f);
            AudioKit.Play("europa_extraction_pulse", L.PlatformAt, 0.8f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.6f) { Glow(ConduitGlow, t * 0.6f); Glow(ConduitBreak, t * 0.6f); yield return null; }
            ImageGate.Expect(ConduitGlow.transform, "light running out through the old conduits");
            yield return Shot("f28_light_runs_through_the_conduits");
            // the junction: the worker's terminal comes alive (the network shown on the terminals, the workers' lines over it)
            var term = CityTerminals != null && CityTerminals.Length > 0 ? CityTerminals[0] : null;
            if (CityTerminals != null) foreach (var t in CityTerminals) Glow(t, 1.3f);
            if (term != null)
            {
                if (Worker != null) { Worker.transform.position = term.transform.position + term.transform.forward * -1.3f + Vector3.right * 0.4f; Worker.transform.rotation = Quaternion.LookRotation(Flat(term.transform.position - Worker.transform.position)); }
                Shoot(term.transform.position + new Vector3(2.2f, 1.2f, 2.4f), term.transform.position + Vector3.down * 0.1f, 46f, 0.01f);
                ImageGate.Expect(term.transform, "a city terminal receiving the archive");
                yield return Wait(0.5f);
                yield return Shot("f29_a_city_terminal_receives_the_archive");
                yield return Say("FIN_07", 1, 1);                   // Seven? We can hear the lower districts. All of them.
            }
            else yield return Say("FIN_07", 1, 1);
            HerFaceCloseUp();
            yield return Say("FIN_07", 2, 2);                       // The chamber is reconnecting the old network.
            if (term != null) Shoot(term.transform.position + new Vector3(2.2f, 1.2f, 2.4f), term.transform.position, 46f, 0.01f);
            yield return Say("FIN_07", 3, 4);                       // There are records here... / These controls belong to the city. He locked us out of them.
            // QF30 (6): the drilling she means is on screen as she says it - a drill boring through the chamber wall, dust and sparks
            var wd = WallDrills != null && WallDrills.Length > 0 ? WallDrills[0] : null;
            if (wd != null) { DrillShot(wd, true); ImageGate.Expect(wd, "a mining drill boring through the chamber wall"); yield return Wait(0.4f); yield return Shot("f29b_a_drill_boring_into_the_chamber"); }
            yield return DialogueFraming.Over("the mining drill boring into the chamber wall (QF24 7)", Say("FIN_07", 5, 5));   // Can you stop the drilling around this chamber?
            yield return Say("FIN_07", 6, 6);                       // We can reach the controls now. Give us a moment.
            // the drilling slows and stops (the bits spin down, the dust settles, the rumble fades) - and stays stopped
            _drillsOn = false;
            if (wd != null) { DrillShot(wd, true); ImageGate.Expect(wd, "the drill spinning down"); }
            yield return Wait(Auto ? 3.5f : 5f);
            yield return Shot("f30_the_drilling_slows_and_stops");
            var dr = MiningDrills != null && MiningDrills.Length > 0 ? MiningDrills[0] : null;
            if (dr != null) { Shoot(dr.position + new Vector3(12f, 76f, 16f), dr.position + Vector3.up * 64f, 50f, 0.01f); ImageGate.Expect(dr.parent, "the drill towers stopped"); yield return Wait(0.6f); yield return Shot("f30b_the_drill_towers_stopped"); }
            Log($"QF30 (6): the drilling stopped - the wall drills spin {_wallRate:0} deg/s, the towers {_drillRate:0} deg/s (both spinning down to 0)");
            // Tunneler over the industrial speakers (his voice heard over the chamber; no worker answers)
            Shoot(L.ChamberC + new Vector3(0f, 3.4f, 9.6f), L.MiningAccess + Vector3.up * 3f, 52f, 0.01f);
            ImageGate.ExpectPoint(L.MiningAccess + Vector3.up * 2f, "the mining access passage his voice comes from");
            yield return DialogueFraming.Over("the industrial speakers (Tunneler's voice)", Say("FIN_07", 7, 7));   // Who authorized that shutdown?
            yield return Wait(1.4f);                                // (no worker answers him)
            yield return DialogueFraming.Over("the industrial speakers (Tunneler's voice)", Say("FIN_07", 9, 9));   // Return to your stations.
            if (term != null) Shoot(term.transform.position + new Vector3(2.2f, 1.2f, 2.4f), term.transform.position, 46f, 0.01f);
            yield return Say("FIN_07", 10, 10);                     // We are at our stations.
            // a heavy impact beyond the chamber
            AudioKit.Play("europa_rupture", L.MiningAccess, 1f); CameraShake.Quake(0.5f, 0.6f);
            Shoot(L.PlatformAt + new Vector3(-3f, 1.7f, 4.2f), L.MiningAccess + Vector3.up * 2f, 50f, 0.01f);
            yield return Wait(1.2f);
            yield return Shot("f31_a_heavy_impact_beyond_the_chamber");
            if (Worker != null) Worker.gameObject.SetActive(false);   // he is at his station (heard over the network from now on)
            Complete("FIN_07");
            yield return Cutscene(false);
        }
    }
}
