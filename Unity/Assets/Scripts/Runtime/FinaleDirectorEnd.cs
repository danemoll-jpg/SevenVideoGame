using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using L = Seven.FinaleLayout;

namespace Seven
{
    /// <summary>Round 19 — the finale's end: FIN_08 (Tunneler arrives + the short confrontation), FIN_09 (the leap), FIN_10 / FIN_11
    /// (eight more; what she carries), FIN_12 (received), FIN_13 (she can rest), FIN_14 (the colour meadow) and the credits with 7's
    /// sign-off (FIN_15).</summary>
    public partial class FinaleDirector
    {
        TunnelerFight _fight;
        Transform _hover;
        Light _faceKey;

        Bounds TunBounds()
        {
            var b = new Bounds(TunnelerCraft.position + Vector3.up * 2f, Vector3.one);
            if (Tunneler != null) { bool f = true; foreach (var r in Tunneler.GetComponentsInChildren<Renderer>()) { if (f) { b = r.bounds; f = false; } else b.Encapsulate(r.bounds); } }
            return b;
        }
        Vector3 TunFace { get { var b = TunBounds(); return b.center + Vector3.up * b.extents.y * 0.25f; } }
        /// <summary>QF24 (9): a boss's lines get a close-up of HIS face from low, a soft white key light so it reads against the fog.</summary>
        void TunnelerCloseUp()
        {
            var f = TunFace; var fwd = Flat(TunnelerCraft.forward).normalized;
            if (_faceKey == null) { _faceKey = new GameObject("TunnelerFaceKey").AddComponent<Light>(); _faceKey.type = LightType.Point; _faceKey.range = 6f; _faceKey.intensity = 3.5f; _faceKey.color = Color.white; _faceKey.shadows = LightShadows.None; }
            _faceKey.enabled = true; _faceKey.transform.position = f + fwd * 1.6f + Vector3.up * 0.4f;
            var b = TunBounds();
            Shoot(f + fwd * (2.2f + b.extents.y * 1.2f) + Vector3.Cross(Vector3.up, fwd) * 0.8f + Vector3.down * (b.extents.y * 0.25f), f, 42f, 0.01f);
            ImageGate.Expect(Tunneler != null ? Tunneler : TunnelerCraft, "Tunneler speaking (close-up)");
        }
        void KeyOff() { if (_faceKey != null) _faceKey.enabled = false; }

        // ------------------------------------------------------------------------------------------ FIN_08: Tunneler arrives
        IEnumerator Confrontation()
        {
            yield return Cutscene(true);
            if (Cat != null) { Cat.gameObject.SetActive(true); Cat.transform.localScale = Vector3.one; }
            if (!FinaleState.SceneDone("FIN_08_entrance"))
            {
                // he forces his way through the mining access passage: debris, the shake, the craft lit from beneath
                TunnelerCraft.gameObject.SetActive(true);
                var a = L.MiningAccess + new Vector3(0f, 0f, -6f); TunnelerCraft.position = a; TunnelerCraft.rotation = Quaternion.LookRotation(Vector3.forward);
                Seven.Place(L.HerFightStart, Vector3.back); if (Cat != null) Cat.Place(L.HerFightStart + new Vector3(1.0f, 0f, 0.6f), Vector3.back);
                Shoot(L.ChamberC + new Vector3(3.6f, 0.5f, -3.0f), L.MiningAccess + Vector3.up * 2.4f, 52f, 0.01f);
                AudioKit.Play("europa_demolition", L.MiningAccess, 1f); CameraShake.Quake(0.7f, 1.0f);
                DrillSpeed = 1100f;
                for (float t = 0f; t < 1f; t += Time.deltaTime / 1.6f) { TunnelerCraft.position = Vector3.Lerp(a, L.TunnelerStart, Mathf.SmoothStep(0f, 1f, t)); if (t > 0.3f && Random.value < 0.15f) Sparks(L.MiningAccess + new Vector3(Random.Range(-3f, 3f), Random.Range(1f, 5f), 0f), 30); yield return null; }
                DrillSpeed = 220f;
                ImageGate.Expect(TunnelerCraft, "Tunneler forcing his way in");
                yield return Shot("f32_tunneler_forces_his_way_in");
                // low angle, machinery lighting him from beneath
                var b = TunBounds();
                Shoot(L.TunnelerStart + new Vector3(2.6f, 0.4f, 4.2f), TunFace, 46f, 0.01f);
                ImageGate.Expect(Tunneler != null ? Tunneler : TunnelerCraft, "Tunneler from below, lit by his machinery");
                yield return Wait(0.5f);
                yield return Shot("f33_low_angle_tunneler_lit_from_beneath");
                // the wider view: 7, Panther, the archive controls between them
                Shoot(L.ChamberC + new Vector3(-9.6f, 4.4f, 2.4f), L.ChamberC + new Vector3(0f, 1.2f, -4f), 56f, 0.01f);
                yield return Wait(0.4f);
                yield return Shot("f34_wide_her_panther_the_platform_and_him");
                TunnelerCloseUp(); yield return Say("FIN_08", 1, 1);                 // Shut it down.
                HerFaceCloseUp(); yield return Say("FIN_08", 2, 2);                  // No.
                TunnelerCloseUp(); yield return Say("FIN_08", 3, 3);                 // Those workers answer to me.
                HerFaceCloseUp(); yield return Say("FIN_08", 4, 4);                  // They can answer for themselves.
                TunnelerCloseUp(); yield return Say("FIN_08", 5, 5);                 // You don't understand what you're interfering with.
                yield return Shot("f35_closeup_tunneler_speaks");
                HerFaceCloseUp(); yield return Say("FIN_08", 6, 6);                  // I've seen it. Here. On Europa.
                KeyOff();
                FinaleState.CompleteScene("FIN_08_entrance");
            }
            else { TunnelerCraft.gameObject.SetActive(true); TunnelerCraft.position = L.TunnelerStart; Seven.Place(L.HerFightStart, Vector3.back); }
            // the confrontation: the three objectives while he attacks; Panther calls from the chamber's edge
            if (Cat != null) { Cat.Place(L.PantherEdge, Vector3.right); Cat.Hold(L.ChamberC, "sit"); }
            if (_hover == null)
            {
                _hover = new GameObject("Hover").transform; _hover.SetParent(TunnelerCraft, false);
                var kids = new List<Transform>(); foreach (Transform c in TunnelerCraft) if (c != _hover) kids.Add(c);
                foreach (var c in kids) c.SetParent(_hover, true);
            }
            _fight = TunnelerCraft.GetComponent<TunnelerFight>() ?? TunnelerCraft.gameObject.AddComponent<TunnelerFight>();
            _fight.Body = _hover; _fight.Dir = this;
            CameraBodyGuard.Extra.Clear(); CameraBodyGuard.Extra.AddRange(TunnelerCraft.GetComponentsInChildren<Renderer>());   // the fight camera never sits inside him
            var rockMat = new Material(Shader.Find("Seven/DissolveLit") ?? Shader.Find("Universal Render Pipeline/Lit")) { name = "Fin_debris" };
            if (rockMat.HasProperty("_BaseColor")) rockMat.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.3f));
            _fight.Setup(null, rockMat);
            _fight.PlaceStart();
            if (FinaleState.StepDone("barrier")) _fight.ForceBarrierOpen();
            _fight.UI = UI; _fight.AutoPilot = DemoBoot.CaptureDir != null;
            _fight.Beat = PhaseBeatShot;
            _fight.OnObjective = what =>
            {
                if (what == "barrier") { FinaleState.DoStep("barrier"); StartCoroutine(BarrierOpens()); }
                if (what == "conduit") { FinaleState.DoStep("conduit"); }
            };
            _fight.PhaseObjective = ph => ph == 0 ? "Keep him away from the platform"
                                       : ph == 1 ? "Open the old maintenance barrier - its control by the east alcove"
                                       : "Restore the connection he cut - the junction box on the west wall";
            yield return Cutscene(false);
            FollowCamera();
            SetZone("DoorZ", false); SetZone("ChamberN", false);   // the doorway closes behind the fight (she can't be driven out of the chamber)
            _fight.Begin(BossFight.Load("tunneler"), Seven, Cam);
            ThemeNow = "moon_tunneler";
            StartCoroutine(FightPrompts(_fight));
            _fight.Moment += FightMoment;
            StartCoroutine(FightCalls());
            int downs = 0;
            var run = StartCoroutine(_fight.Run(s => { if (s != null && !s.StartsWith("He is dazed")) Objective(s); }));
            while (!_fight.Over && !_fight.Dazed)
            {
                if (_fight.Knockdowns > downs) { downs = _fight.Knockdowns; FinaleState.CountRetry(); Log($"CONFRONTATION RETRY (Round 19 8): she went down ({downs}x) - the retry keeps the objectives done ({(_fight.BarrierOpen ? "barrier open" : "barrier closed")}, conduit {(_fight.ConduitRestored ? "restored" : _fight.ConduitCut ? "cut" : "whole")}) and his energy {_fight.Energy * 100f:0}%"); }
                yield return null;
            }
            if (DemoBoot.BossIdle) yield break;
            StopCoroutine(run);
            _fight.Moment -= FightMoment;
            SetZone("DoorZ", true); SetZone("ChamberN", true);
            Log(_fight.Summary());
            Log($"TUNNELER FIGHT (Round 19 8): objectives - kept off the platform (phase 1 by her blows), barrier {(_fight.BarrierOpen ? "open" : "CLOSED")}, conduit {(_fight.ConduitRestored ? "restored" : "NOT restored")}; Panther's calls {_calls}, his distraction {(_distracted ? "used once" : "not used")}; retries {FinaleState.Retries}");
            Objective(null);
            if (DemoBoot.BossCheck && DemoBoot.CaptureDir != null && !DemoBoot.FinPlay) { Cap.Finish("boss check (Round 19): " + _fight.Summary()); yield break; }
        }
        int _calls; bool _distracted;
        IEnumerator FightPrompts(BossFight f)
        {
            while (f != null && !f.Over && !f.Dazed && f.isActiveAndEnabled) { if (!_cut) { var l = f.PromptLabel; SetPrompt(l, f.PromptIsTk ? ControlsLegend.Btn.Tk : ControlsLegend.Btn.Interact); } yield return null; }
            SetPrompt(null);
        }
        void FightMoment(string m) { if (m == "waiting" && _fight.Phase == 1) { _fight.StationLit(true); Log("boss tunneler: his bar waits at the barrier mark - the barrier control is lit (the elder said it at the phase change)"); } }
        /// <summary>Panther from the chamber's edge: the first drill (move!), the platform, the cut conduit; once he draws Tunneler away.</summary>
        IEnumerator FightCalls()
        {
            bool first = false, plat = false;
            while (_fight != null && !_fight.Over)
            {
                if (!first && _fight.State == BossFight.St.Telegraph) { first = true; _calls++; StartCoroutine(SayIds("FIN_08_P1")); }          // The drill - move!
                if (!plat && _fight.Phase == 0 && Flat(_fight.transform.position - L.PlatformAt).magnitude < L.PlatformR + 3.2f && _fight.State == BossFight.St.Idle) { plat = true; _calls++; StartCoroutine(SayIds("FIN_08_P2")); }   // He's going for the platform!
                if (_fight.Phase == 2 && _fight.ConduitCut && !_distracted && !_fight.ConduitRestored && Flat(P7 - L.ConduitAt).magnitude < 9f && !_fight.Paused && _fight.State == BossFight.St.Idle)
                {
                    _distracted = true; _calls++;
                    if (Cat != null) { Cat.GoTo(L.PantherEdge + new Vector3(2.2f, 0f, -1.4f), 2.4f, _fight.transform.position); }
                    StartCoroutine(SayIds("FIN_08_P3"));                // Over here, you lump of scrap!
                    _fight.Distract(Cat != null ? Cat.transform.position : L.PantherEdge, 4.5f);
                    StartCoroutine(Shot("f37_panther_draws_him_away"));
                }
                yield return null;
            }
        }
        IEnumerator PhaseBeatShot(int ended)
        {
            if (ended == 0)
            {
                // the elder over the network: the old barrier still has power (the alcove lit)
                Shoot(L.ChamberC + new Vector3(1.5f, 3.2f, 5.0f), L.BarrierAt + Vector3.up * 1.8f, 58f, 0.01f);
                Glow(BarrierLight, 1.6f);
                yield return Wait(0.4f);
                ImageGate.ExpectPoint(L.BarrierAt + Vector3.up * 2.2f, "the old barrier's alcove");
                yield return Shot("f36_the_old_barrier_its_control_lit");     // (before the line: the framing may change for it)
                yield return DialogueFraming.Over("the old maintenance barrier's alcove (the elder over the network)", SayIds("FIN_08_P5"));   // The old maintenance barrier still has power. Open it, and we will hold the lock.
                FollowCamera();
            }
            else if (ended == 1)
            {
                // he swings at the west conduit and cuts it: the light on the left goes out
                var a = _fight.transform.position; var b = L.ConduitAt + new Vector3(3.6f, 0f, -0.4f);
                Shoot(L.ConduitAt + new Vector3(7.5f, 2.6f, 0.4f), L.ConduitAt + Vector3.up * 1.8f, 52f, 0.2f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 1.0f) { _fight.transform.position = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t)); _fight.transform.rotation = Quaternion.Slerp(_fight.transform.rotation, Quaternion.LookRotation(Flat(L.ConduitAt - _fight.transform.position)), t); yield return null; }
                DrillSpeed = 1400f;
                AudioKit.Play("europa_rupture", L.ConduitAt + Vector3.up * 2f, 1f); CameraShake.Quake(0.4f, 0.4f);
                Sparks(L.ConduitAt + Vector3.up * 2.4f, 140);
                Glow(ConduitBreak, 0f);
                _fight.ConduitCut = true;
                ImageGate.ExpectPoint(L.ConduitAt + Vector3.up * 2f, "the cut conduit");
                yield return Shot("f38_he_cuts_the_conduit");
                yield return SayIds("FIN_08_P4");                       // He's cut the conduit. The light's gone out on the left.
                FollowCamera();
            }
        }
        IEnumerator BarrierOpens()
        {
            Glow(BarrierLight, 2.2f);
            AudioKit.Play("bia_barrier", L.BarrierAt + Vector3.up * 2f, 0.9f);
            if (Shots) { ImageGate.ExpectPoint(L.BarrierControl + Vector3.up * 1.0f, "the barrier control, opened"); yield return Cap.ShotFrom("f36b_the_barrier_control_opened", L.BarrierControl + new Vector3(-6.0f, 2.6f, 4.6f), Vector3.Lerp(L.BarrierControl, L.BarrierAt, 0.5f) + Vector3.up * 1.6f, 54f); }
            Log("FIN_08: the old maintenance barrier opened - the alcove is ready (its field will close behind him)");
        }

        // ------------------------------------------------------------------------------------------ FIN_09: the leap
        IEnumerator TheLeap()
        {
            yield return Cutscene(true);
            UI.HideBossBar();
            if (_fight != null) { _fight.End(); _fight.enabled = false; }
            if (!FinaleState.SceneDone("FIN_09_leap"))
            {
                Glow(ConduitBreak, 0.6f);
                StartCoroutine(DialogueFraming.Over("the restored conduit (the Archive's voice)", SayIds("FIN_08_P6")));   // Connection restored.
                // she turns back toward the platform; he breaks free and drives at her - Panther sees it first
                Seven.Place(L.ConduitAt + new Vector3(1.4f, 0f, 0f), Vector3.right);
                var from = L.ChamberC + new Vector3(4.6f, 0f, 1.6f);
                TunnelerCraft.position = from; TunnelerCraft.rotation = Quaternion.LookRotation(Flat(P7 - from));
                if (Cat != null) { Cat.transform.localScale = Vector3.one; Cat.Place(L.PantherEdge + new Vector3(1.6f, 0f, 2.6f), Vector3.forward); Cat.Hold(TunnelerCraft.position, "sit"); }
                Shoot(P7 + new Vector3(-1.6f, 1.5f, 2.6f), TunnelerCraft.position + Vector3.up * 1.6f, 50f, 0.01f);
                DrillSpeed = 1400f; AudioKit.Play("europa_drill", TunnelerCraft.position, 1f);
                yield return Wait(0.6f);
                yield return Shot("f39_he_breaks_free_and_drives_at_her");
                Shoot(Cat != null ? Cat.transform.position + new Vector3(0.9f, 0.4f, 1.4f) : P7, Cat != null ? Cat.transform.position + Vector3.up * 0.3f : P7, 40f, 0.01f);
                yield return Say("FIN_09", 1, 1);                       // Seven!
                // he grows to his combat size (never the meadow's greatest form) and leaps into the strike's path
                if (Cat != null)
                {
                    Cat.Scripted("stretch");
                    var s0 = Cat.transform.localScale.x;
                    for (float t = 0f; t < 1f; t += Time.deltaTime / 0.7f) { Cat.transform.localScale = Vector3.one * Mathf.Lerp(s0, 2.7f, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
                    var mid = Vector3.Lerp(P7, TunnelerCraft.position, 0.45f);
                    Shoot(mid + new Vector3(0f, 2.2f, 6.4f), mid + Vector3.up * 1.2f, 54f, 0.01f);
                    Cat.Hop(mid, 0.55f, 1.3f);
                    for (float t = 0f; t < 0.55f; t += Time.deltaTime) { TunnelerCraft.position = Vector3.MoveTowards(TunnelerCraft.position, mid, 9f * Time.deltaTime); yield return null; }
                    yield return Shot("f40_panther_leaps_into_the_strike");
                }
                // CUT at the impact: the sound, the music gone, her face
                AudioKit.Play("fight_hit", P7, 1f); AudioKit.Play("land_hard", P7, 1f);
                MusicSystem.Silence("FIN_09 impact - Panther's death (FIN_10) - the start of FIN_11", 240f); MusicSystem.Stop(0.05f);
                ThemeNow = null;
                CameraShake.Quake(0.5f, 0.3f);
                HerFaceCloseUp();
                Seven.Gesture(P7 + Seven.transform.forward);
                yield return Wait(0.3f);
                yield return Shot("f41_her_reaction_at_the_impact");
                Log("FIN_09: the impact - cut away from it (no injury shown), the music cut dead, her reaction");
                // Panther fallen clear of the machinery, small again; Tunneler knocked back toward the east
                if (Cat != null) { Cat.transform.localScale = Vector3.one; Cat.Place(L.PantherFalls, Vector3.left); Cat.Scripted("lie_down"); }
                TunnelerCraft.position = L.ChamberC + new Vector3(6.2f, 0f, -0.4f); DrillSpeed = 0f;
                yield return Wait(1.2f);
                // for a moment she starts toward him
                Seven.Lock(false); yield return WalkSeven(L.PantherFalls + new Vector3(1.6f, 0f, 0.8f), 1.4f); Seven.Lock(true);
                // Tunneler moves again toward the archive
                DrillSpeed = 600f;
                Shoot(L.ChamberC + new Vector3(-2f, 2.2f, 8f), TunnelerCraft.position + Vector3.up * 1.4f, 50f, 0.01f);
                for (float t = 0f; t < 1.0f; t += Time.deltaTime) { TunnelerCraft.position = Vector3.MoveTowards(TunnelerCraft.position, L.ChamberC + new Vector3(4.2f, 0f, -0.4f), 1.4f * Time.deltaTime); yield return null; }
                Shoot(L.PantherFalls + new Vector3(0.9f, 0.5f, 1.4f), L.PantherFalls + Vector3.up * 0.2f, 38f, 0.01f);
                if (Cat != null) ImageGate.Expect(Cat.transform, "Panther, still conscious");
                yield return Say("FIN_09", 2, 2);                       // The barrier.
                FinaleState.CompleteScene("FIN_09_leap");
            }
            else
            {
                if (Cat != null) { Cat.transform.localScale = Vector3.one; Cat.Place(L.PantherFalls, Vector3.left); Cat.Scripted("lie_down"); }
                TunnelerCraft.position = L.ChamberC + new Vector3(4.2f, 0f, -0.4f);
                MusicSystem.Silence("FIN_09 - the barrier (retry from the save)", 240f);
            }
            // the final player action: she drives him into the prepared barrier (telekinesis), the worker locks it
            Seven.Place(L.ChamberC + new Vector3(-2.6f, 0f, 2.2f), Vector3.right);
            yield return Cutscene(false);
            UI.SetObjective(null);
            yield return Telekinesis(TunnelerCraft.position + Vector3.up * 1.2f, 16f, "Tunneler into the barrier", "Drive him into the barrier's alcove (telekinesis)", null, null, TunnelerCraft);
            yield return Cutscene(true);
            var a2 = TunnelerCraft.position; var into = L.BarrierAt + new Vector3(0.6f, 0f, 0f);
            Shoot(L.ChamberC + new Vector3(2f, 2.8f, 8.6f), L.BarrierAt + Vector3.up * 2f, 54f, 0.01f);
            AudioKit.Play("tk_push", a2, 1f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.3f) { TunnelerCraft.position = Vector3.Lerp(a2, into, Mathf.SmoothStep(0f, 1f, t)) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.6f; yield return null; }
            TunnelerCraft.rotation = Quaternion.LookRotation(Vector3.left);
            if (BarrierField != null) BarrierField.gameObject.SetActive(true);
            AudioKit.Play("barrier_close", L.BarrierAt + Vector3.up * 2f, 1f);
            ImageGate.Expect(TunnelerCraft, "Tunneler inside the barrier");
            yield return Shot("f42_the_barrier_closes_on_him");
            Shoot(Lock.position + new Vector3(-3f, -0.6f, -2.4f), Lock.position, 40f, 0.01f);
            Glow(BarrierLight, 0.4f);
            yield return DialogueFraming.Over("the external lock (the worker over the network)", Say("FIN_09", 3, 3));   // We have the lock.
            // he is alive and contained; his machinery powers down
            DrillSpeed = 0f;
            foreach (var l in TunnelerCraft.GetComponentsInChildren<Light>()) l.enabled = false;
            AudioKit.Play("mars_barrier_off", TunnelerCraft.position, 0.6f);
            Shoot(L.BarrierAt + new Vector3(-6.5f, 2.2f, 3.4f), TunnelerCraft.position + Vector3.up * 1.6f, 50f, 0.01f);
            ImageGate.Expect(TunnelerCraft, "Tunneler contained");
            yield return Wait(1.2f);
            yield return Shot("f43_contained_alive_his_machinery_powering_down");
            Log("FIN_09: Tunneler contained in the old maintenance barrier, alive; the worker holds the lock; no victory banner");
            Complete("FIN_09");
            ChapterSevenOff();
        }
        void ChapterSevenOff() { if (Seven.InFight) Seven.EndFight(); }

        // ------------------------------------------------------------------------------------------ FIN_10: eight more (no HUD, no music)
        IEnumerator EightMore()
        {
            yield return Cutscene(true);
            NoHud(true);
            MusicSystem.Silence("FIN_10 - Panther's death, the long hold", 240f);
            if (Cat != null) { Cat.gameObject.SetActive(true); Cat.transform.localScale = Vector3.one; }
            // she reaches him and drops beside him; gathers him against her lap
            var spot = L.PantherFalls + new Vector3(0.8f, 0f, 0.5f);
            Seven.Place(spot + new Vector3(1.5f, 0f, 0.4f), Flat(L.PantherFalls - spot));
            Shoot(L.PantherFalls + new Vector3(3.6f, 1.4f, 3.2f), L.PantherFalls + Vector3.up * 0.4f, 46f, 0.01f);
            Seven.Lock(false); yield return WalkSeven(spot, 2.5f); Seven.Lock(true);
            Seven.Place(spot, Vector3.left);
            var rest = SevenRestPose.On(Seven.gameObject, SevenRestPose.Kind.Kneel);
            var fwd = Seven.transform.forward; var lap = P7 + fwd * 0.66f;
            if (Cat != null) { Cat.Place(lap, Vector3.Cross(Vector3.up, fwd)); Cat.Scripted("lie_down"); }
            var ik = Seven.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
            ik.enabled = true; ik.Alive = true; ik.Ease = 1.4f;
            Vector3 Fur(float along) => (Cat != null ? Cat.transform.position : lap) + Vector3.up * 0.24f + Cat.transform.forward * along;
            ik.RightPos = Fur(0.02f); ik.RightPalmN = Vector3.down; ik.RightPalmW = 1f; ik.RightFingerDir = Cat != null ? Cat.transform.forward : fwd; ik.RightW = 1f;
            ik.LeftPos = Fur(-0.12f) + Vector3.Cross(Vector3.up, Cat.transform.forward) * 0.08f; ik.LeftPalmN = Vector3.down; ik.LeftPalmW = 1f; ik.LeftW = 0.8f;
            ik.LookPos = Fur(0.1f); ik.LookW = 1f;
            var side = Vector3.Cross(Vector3.up, fwd);
            Shoot(P7 + fwd * 0.5f + side * 2.3f + Vector3.up * 0.9f, P7 + fwd * 0.35f + Vector3.up * 0.45f, 40f, 0.01f);
            ImageGate.Expect(Seven.transform, "7 kneeling with Panther against her knees");
            var breath = StartCoroutine(Breathing(true));
            yield return Wait(1.2f);
            yield return Shot("f44_she_gathers_him_into_her_lap");
            yield return Say("FIN_10", 1, 3);                           // Hey. Hey, I'm here. / I know. / We'll get you back to the ship.
            // he presses his head into her hand
            Shoot(Fur(0.2f) + side * 0.9f + Vector3.up * 0.35f, Fur(0.18f), 34f, 0.01f);
            ImageGate.Expect(Cat != null ? Cat.transform : Seven.transform, "his head pressed into her hand (insert)");
            ik.RightPos = Fur(0.24f);
            yield return Wait(1.8f);
            yield return Shot("f45_insert_his_head_into_her_hand");
            Shoot(P7 + fwd * 0.9f + side * 1.3f + Vector3.up * 0.9f, HerHead + Vector3.down * 0.3f, 38f, 0.01f);
            yield return Say("FIN_10", 5, 5);                           // Just stay with me.
            // hold on her hand against his fur; no archive announcements
            Shoot(Fur(0.05f) + side * 0.85f + Vector3.up * 0.35f, Fur(0.04f), 32f, 0.01f);
            yield return Wait(2.4f);
            Shoot(P7 + fwd * 0.5f + side * 2.3f + Vector3.up * 0.9f, P7 + fwd * 0.35f + Vector3.up * 0.45f, 40f, 0.01f);
            yield return Say("FIN_10", 6, 6);                           // Don't worry... I have eight more.
            PlayReaction("seven", "laugh_soft");                      // a tiny, involuntary laugh through tears
            yield return Wait(1.0f);
            yield return Say("FIN_10", 8, 8);                           // You'd better.
            // his breathing settles, then stops; she waits for another breath; none comes
            yield return Wait(2.0f);
            StopCoroutine(breath); StartCoroutine(Breathing(false));
            yield return Wait(3.0f);
            // her hand strokes him once, then is still
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.6f) { ik.RightPos = Fur(Mathf.Lerp(0.2f, -0.15f, Mathf.SmoothStep(0f, 1f, t))); yield return null; }
            yield return Shot("f46_her_hand_still_on_his_fur");
            // a substantial silence
            Shoot(P7 + fwd * 1.6f + side * 3.4f + Vector3.up * 2.0f, P7 + fwd * 0.3f + Vector3.up * 0.4f, 42f, 2.5f);
            yield return Wait(Auto ? 4f : 8f);
            Log("FIN_10: no HUD, no music; his breathing slowed and stopped; the long hold (silence)");
            Complete("FIN_10");
        }
        IEnumerator Breathing(bool on)
        {
            if (Cat == null) yield break;
            float amp = 0.02f;
            for (float t = 0f; ; t += Time.deltaTime)
            {
                if (!on) amp = Mathf.MoveTowards(amp, 0f, Time.deltaTime * 0.01f);
                float rate = on ? 1.6f : 0.9f;
                Cat.transform.localScale = new Vector3(1f + Mathf.Sin(t * rate * Mathf.PI) * amp * 0.5f, 1f + Mathf.Sin(t * rate * Mathf.PI) * amp, 1f);
                if (!on && amp <= 0f) { Cat.transform.localScale = Vector3.one; yield break; }
                yield return null;
            }
        }
        void NoHud(bool on)
        {
            _noHud = on;
            Objective(null); SetPrompt(null); HideGuide(); UI.HideBossBar();
            if (Legend != null) Legend.HidePanels = true;
        }
        bool _noHud;

        // ------------------------------------------------------------------------------------------ FIN_11: what she carries
        IEnumerator WhatSheCarries()
        {
            yield return Cutscene(true);
            NoHud(true);
            // the archive waits for her: a distant light pulses gently, no timer
            var fwd = Seven.transform.forward;
            Shoot(P7 + fwd * 2.4f + Vector3.up * 1.4f, L.PlatformAt + Vector3.up * 1f, 48f, 0.01f);
            for (float t = 0f; t < (Auto ? 3f : 6f); t += Time.deltaTime) { Glow(PlatformSurface, 0.3f + 0.25f * Mathf.Sin(t * 1.6f)); yield return null; }
            yield return Shot("f47_the_archive_pulses_waiting");
            if (Worker != null) { Worker.gameObject.SetActive(true); Worker.transform.position = new Vector3(11.5f, L.Y, -144f); }
            yield return DialogueFraming.Over("the chamber doorway (the worker's voice over the network)", Say("FIN_11", 1, 1));   // Seven?
            Shoot(P7 + fwd * 1.1f + Vector3.up * 1.0f, HerHead + Vector3.down * 0.15f, 36f, 0.01f);
            yield return Say("FIN_11", 2, 2);                           // I'm here.
            yield return DialogueFraming.Over("the chamber doorway (the worker's voice)", Say("FIN_11", 3, 3));
            Shoot(P7 + fwd * 1.1f + Vector3.up * 1.0f, HerHead + Vector3.down * 0.15f, 36f, 0.01f);
            yield return Say("FIN_11", 4, 4);                           // Good.
            yield return DialogueFraming.Over("the chamber doorway (the worker's voice)", Say("FIN_11", 5, 5));   // We're coming down.
            yield return Wait(Auto ? 2f : 4f);
            // she carries him to the central platform and lays him gently beside her place
            var rest = Seven.GetComponent<SevenRestPose>(); if (rest != null) rest.Pose = SevenRestPose.Kind.None;
            var ik = Seven.GetComponent<SevenTableIK>();
            if (Cat != null) { Cat.transform.SetParent(Seven.transform, true); Cat.transform.localPosition = new Vector3(0f, 1.02f, 0.3f); Cat.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); }
            if (ik != null)
            {
                ik.RightPos = Cat.transform.position + Vector3.down * 0.1f + Seven.transform.right * 0.1f; ik.LeftPos = Cat.transform.position + Vector3.down * 0.1f - Seven.transform.right * 0.16f;
                ik.RightPalmN = Vector3.up; ik.LeftPalmN = Vector3.up; ik.RightW = 1f; ik.LeftW = 1f;
            }
            var to = L.PlatformAt + new Vector3(-0.4f, 0f, L.PlatformR + 0.75f);
            Shoot(L.ChamberC + new Vector3(-7.4f, 2.4f, 5.4f), L.ChamberC + new Vector3(-2f, 1.0f, 2f), 50f, 0.01f);
            StartCoroutine(CarryHands(ik));
            Seven.Lock(false); yield return WalkSeven(to, 10f); Seven.Lock(true);
            _carrying = false;
            yield return Shot("f48_she_carries_him_to_the_platform");
            if (Cat != null) { Cat.transform.SetParent(null, true); Cat.Place(L.PlatformAt + new Vector3(-0.9f, 0f, L.PlatformR + 0.45f), Vector3.right); Cat.Scripted("sleep"); }
            if (ik != null) { ik.LeftW = 0f; ik.RightPos = Cat.transform.position + Vector3.up * 0.24f; ik.RightPalmN = Vector3.down; ik.RightW = 0.6f; }
            Shoot(P7 + new Vector3(2.0f, 1.2f, 1.8f), Cat.transform.position + Vector3.up * 0.3f, 40f, 0.01f);
            if (Cat != null) ImageGate.Expect(Cat.transform, "Panther laid beside the archive");
            yield return Wait(1.0f);
            yield return Shot("f49_she_lays_him_beside_the_archive_one_hand_near_him");
            if (ik != null) ik.ReleaseAll("she stands at the archive");
            yield return Cutscene(false);
            // the player approaches the archive and resumes the sharing: one deliberate interaction, no timer (still no HUD text)
            bool go = false; float t0 = Time.time;
            yield return Until(() => { if (Input.Interact.WasPressedThisFrame() || (Auto && Time.time - t0 > 1.5f)) go = true; return go; }, "resume the sharing at the archive", 60f, null, null, () => go = true);
            yield return Cutscene(true);
            MusicSystem.EndSilence(); MusicSystem.Play("moon_transfer", 3f); ThemeNow = "moon_transfer";
            Shoot(L.PlatformAt + new Vector3(2.4f, 2.4f, 4.8f), L.PlatformAt + Vector3.up * 1.0f, 44f, 0.01f);
            yield return DialogueFraming.Over("the archive surface (the Archive's voice)", Say("FIN_11", 6, 6));   // Continue?
            HerFaceCloseUp();
            yield return Say("FIN_11", 7, 8);                           // Yes. / All of it.
            yield return HandOnSurface(true);
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
            Log($"FIN_11: the memories - the seven visions, then {(Memories != null ? Memories.Length : 0)} of their own moments (first meeting, his bowl, his head in her lap, relief, the race, the meadow silhouette, her head against him)");
            Complete("FIN_11");
        }
        bool _carrying = true;
        IEnumerator CarryHands(SevenTableIK ik)
        {
            _carrying = true;
            while (_carrying && ik != null && Cat != null) { ik.RightPos = Cat.transform.position + Vector3.down * 0.12f + Seven.transform.right * 0.12f; ik.LeftPos = Cat.transform.position + Vector3.down * 0.12f - Seven.transform.right * 0.18f; yield return null; }
        }

        // ------------------------------------------------------------------------------------------ FIN_12: received
        IEnumerator Received()
        {
            // the lunar city: the Scree stations connected; the workers shut the mining down and secure the city's services
            var term = CityTerminals != null && CityTerminals.Length > 0 ? CityTerminals[0] : null;
            if (Worker != null && term != null) { Worker.gameObject.SetActive(true); Worker.transform.position = term.transform.position - term.transform.forward * 1.3f; Worker.transform.rotation = Quaternion.LookRotation(Flat(term.transform.position - Worker.transform.position)); }
            if (Elder != null && CityTerminals != null && CityTerminals.Length > 1) { Elder.gameObject.SetActive(true); var t2 = CityTerminals[1]; Elder.transform.position = t2.transform.position - t2.transform.forward * 1.3f; Elder.transform.rotation = Quaternion.LookRotation(Flat(t2.transform.position - Elder.transform.position)); }
            if (term != null) { Shoot(term.transform.position + new Vector3(2.6f, 1.3f, 2.8f), term.transform.position + Vector3.down * 0.2f, 46f, 0.01f); ImageGate.Expect(Worker != null ? Worker.transform : term.transform, "the Scree at their own stations"); }
            yield return Wait(0.4f);
            yield return Shot("f52_received_the_scree_at_their_stations");
            yield return Say("FIN_12", 1, 1);                           // We have it. Our records... they're here.
            if (Elder != null) Shoot(Elder.transform.position + Elder.transform.forward * 2.6f + Vector3.up * 1.5f + Elder.transform.right * 0.8f, Elder.transform.position + Vector3.up * 1.4f, 42f, 0.01f);
            yield return Say("FIN_12", 2, 2);                           // Send them to the other districts.
            // the worlds they visited (each from the game's own views of it): the record arrives
            yield return Received("rec_1_venus", "Venus: the temple receives it", () => Say("FIN_12", 3, 3), null);
            yield return Received("rec_2_mars", "Mars: the temple of both kingdoms", null, "mars_vision_stone");
            yield return Received("rec_3_europa", "Europa: the temple, the familiar call", null, "europa_call_deep");
            yield return Received("rec_4_asteroid", "the asteroid: the Machinist beside the archive", () => Say("FIN_12", 4, 5), null);
            yield return Received("rec_5_mothership", "the mothership: the memory structures", () => Say("FIN_12", 6, 6), "bia_barrier");
            yield return Received("rec_6_cattalus", "Cattalus: the Cat-a-tron", null, null, 1.4f);
            yield return Received("rec_7_cattalus_meadow", "Cattalus: the old meadow on the Cat-a-tron, the square still", null, "cat_meadow_breeze", 2.6f);
            UI.SetVideo(null, 1f, 0f);
            // back in the chamber: the gaps are filled
            for (int i = 0; i < 7; i++) { Glow(i < SocketLights.Length ? SocketLights[i] : null, 2.0f); ShowWall(i, 1.2f); }
            Glow(PlatformSurface, 0.5f);
            Shoot(L.ChamberC + new Vector3(-8.4f, 4.6f, 8f), L.ChamberC + Vector3.up * 2.4f, 58f, 0.01f);
            ImageGate.ExpectPoint(L.ChamberC + Vector3.up * 2f, "the archive complete");
            yield return Wait(0.6f);
            yield return Shot("f53_the_archive_complete");
            yield return DialogueFraming.Over("the archive (its voice)", Say("FIN_12", 7, 8));   // Record complete. / All connected archives have received it.
            Complete("FIN_12");
        }
        IEnumerator Received(string still, string what, System.Func<IEnumerator> lines, string sfx, float hold = 2.4f)
        {
            var tex = Resources.Load<Texture2D>("Finale/Received/" + still);
            if (tex == null) { Log("FIN_12 montage: MISSING " + still); yield break; }
            float asp = tex.width / (float)tex.height;
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { UI.SetVideo(tex, asp, t / 0.5f); yield return null; }
            UI.SetVideo(tex, asp, 1f);
            if (sfx != null) AudioKit.Play(sfx, null, 0.6f);
            yield return Shot($"f52_received_video_frame_{still}");
            Log($"FIN_12 montage: {what}");
            DialogueFraming.VoiceOver = "FIN_12's montage (the received record, " + what + ")";
            if (lines != null) yield return lines(); else yield return Wait(Auto ? 1.0f : hold);
            DialogueFraming.VoiceOver = null;
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { UI.SetVideo(tex, asp, 1f - t / 0.4f); yield return null; }
        }

        // ------------------------------------------------------------------------------------------ FIN_13: she can rest
        IEnumerator SheCanRest()
        {
            yield return Cutscene(true);
            NoHud(true);
            if (Cat != null && Cat.transform.parent != null) Cat.transform.SetParent(null, true);
            if (Cat != null && !FinaleState.SceneDone("FIN_11")) { }
            if (Cat != null) { Cat.gameObject.SetActive(true); Cat.Place(L.PlatformAt + new Vector3(-0.9f, 0f, L.PlatformR + 0.45f), Vector3.right); Cat.Scripted("sleep"); }
            Seven.Place(L.PlatformAt + new Vector3(-0.1f, 0f, L.PlatformR + 0.75f), Vector3.back);
            if (MusicSystem.Current != "moon_transfer") MusicSystem.Play("moon_transfer", 2f);
            yield return HandOnSurface(false);
            // she takes her hand from the archive and looks at him; the worker reaches the doorway and stops
            if (Worker != null) { Worker.gameObject.SetActive(true); Worker.transform.position = new Vector3(11.5f, L.Y, -150.9f); Worker.transform.rotation = Quaternion.LookRotation(Vector3.back); }
            Shoot(new Vector3(11.5f, L.Y, -150.9f) + new Vector3(2.4f, 1.6f, -3.6f), new Vector3(11.5f, L.Y + 1.4f, -150.9f), 42f, 0.01f);
            yield return Wait(0.5f);
            yield return Shot("f54_the_worker_stops_at_the_doorway");
            yield return Say("FIN_13", 1, 1);                           // We'll look after him.
            HerFaceCloseUp();
            yield return Wait(1.6f);                                    // she nods, unable to answer at once
            yield return Say("FIN_13", 3, 3);                           // Thank you.
            // the pale light: plain, calm, no symbols, no figure
            if (PaleLight != null)
            {
                PaleLight.enabled = true; var m = PaleLight.material;
                Shoot(L.ChamberC + new Vector3(-6.4f, 1.8f, 7.4f), L.ChamberC + Vector3.up * 4.6f, 54f, 0.01f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 3f) { if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", t * 0.32f); if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white * t * 0.32f); yield return null; }
                ImageGate.ExpectPoint(L.ChamberC + Vector3.up * 3f, "the pale light falling into the chamber");
                yield return Shot("f55_a_pale_light_falls_across_the_chamber");
            }
            // she looks up; the voice (not shown, not named)
            var ik = Seven.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
            ik.enabled = true; ik.LookPos = L.ChamberC + Vector3.up * 14f; ik.LookW = 1f;
            Shoot(P7 + new Vector3(0.9f, 0.8f, 1.5f), HerHead + Vector3.up * 0.4f, 44f, 0.01f);
            ImageGate.Expect(Seven.transform, "7 looking up into the light");
            yield return DialogueFraming.Over("the pale light (the voice: not shown, not named)", Say("FIN_13", 4, 4));   // You can rest now.
            yield return Shot("f56_she_looks_up_into_the_light");
            // she looks down at Panther once more and gently touches his head
            ik.LookPos = Cat != null ? Cat.transform.position : P7; ik.LookW = 1f;
            Seven.Place(P7, Flat((Cat != null ? Cat.transform.position : P7 + Vector3.left) - P7));
            ik.RightPos = (Cat != null ? Cat.transform.position + Cat.transform.forward * 0.2f : P7) + Vector3.up * 0.24f; ik.RightPalmN = Vector3.down; ik.RightPalmW = 1f; ik.RightW = 1f; ik.Ease = 1.6f;
            Shoot(P7 + new Vector3(1.6f, 1.0f, 1.4f), (Cat != null ? Cat.transform.position : P7) + Vector3.up * 0.3f, 40f, 0.01f);
            yield return Wait(2.2f);
            yield return Shot("f57_she_touches_his_head");
            ik.ReleaseAll("into the light");
            // the light surrounds her; her silhouette softens and disappears into it; fade through white
            Shoot(P7 + new Vector3(0f, 1.4f, 5.6f), P7 + Vector3.up * 1.2f, 46f, 0.01f);
            var rends = Seven.GetComponentsInChildren<Renderer>();
            var mpb = new MaterialPropertyBlock();
            StartCoroutine(UI.Fade(1f, 4.2f, 1f));
            for (float t = 0f; t < 1f; t += Time.deltaTime / 4f)
            {
                foreach (var r in rends) { r.GetPropertyBlock(mpb); mpb.SetFloat("_Fade", Mathf.SmoothStep(0f, 1f, t)); r.SetPropertyBlock(mpb); }
                if (PaleLight != null) { var m = PaleLight.material; if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", 0.32f + t * 0.5f); }
                if (!_softShot && t > 0.12f) { _softShot = true; StartCoroutine(Shot("f58_her_silhouette_softens_into_the_light")); }
                yield return null;
            }
            yield return Wait(0.6f);
            yield return Shot("f59_fade_through_white");
            MusicSystem.Stop(3f);
            Log("FIN_13: the pale light (plain, no symbols, no figure), 'You can rest now.' (the voice, unnamed), her silhouette softened into it, fade through white");
            Complete("FIN_13");
            Checkpoint(L.CpEnding);
        }

        bool _softShot;
        // ------------------------------------------------------------------------------------------ FIN_14: somewhere to come back to (COLOUR)
        IEnumerator ComeBackTo()
        {
            var clip = MeadowColour != null && !DemoBoot.NoVideo ? MeadowColour : MeadowGray;
            bool colour = clip == MeadowColour && clip != null;
            // the only colour in the game: every volume's grade off while it plays (the video is drawn on the UI layer anyway)
            var vols = FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None); var was = new float[vols.Length];
            for (int i = 0; i < vols.Length; i++) { was[i] = vols[i].weight; vols[i].weight = 0f; }
            UI.SetFade(1f, 1f);
            if (clip == null) { Log("FIN_14: NO MEADOW CLIP"); yield break; }
            var go = new GameObject("MeadowColour"); var vp = go.AddComponent<UnityEngine.Video.VideoPlayer>();
            vp.playOnAwake = false; vp.clip = clip; vp.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture; vp.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None; vp.timeUpdateMode = UnityEngine.Video.VideoTimeUpdateMode.GameTime;
            var rt = new RenderTexture((int)clip.width, (int)clip.height, 0); vp.targetTexture = rt;
            vp.Prepare(); for (float w = 0f; w < 5f && !vp.isPrepared; w += Time.deltaTime) yield return null;
            float start = 12f, len = (float)clip.length, asp = clip.width / (float)clip.height;
            vp.time = start; vp.Play();
            yield return Wait(0.15f);
            UI.SetVideo(rt, asp, 1f);
            MusicSystem.EndSilence(); MusicSystem.Play("cattalus_meadow", 2.5f);
            var breeze = AudioKit.Loop("cat_meadow_breeze", null, null, 0f, AudioKit.Bus.Ambience); if (breeze != null) StartCoroutine(AudioKit.Fade(breeze, 0.45f, 2f));
            StartCoroutine(UI.Fade(0f, 2.2f, 1f));                 // out of the white into colour
            DialogueFraming.VoiceOver = "FIN_14 (the colour meadow, video)";
            Reactions.Suppress = true;
            Log($"FIN_14: {(colour ? "Dan's COLOUR master (game copy)" : "the grey meadow clip (no colour copy)")} from {start:0.0} s of {len:0.0} s - the only colour in the game");
            (float t, string what)[] cues =
            {
                (12.6f, "shot:f60_colour_video_frame_her_from_behind"), (15.0f, "sfx:grass_rustle:0.4"), (19.2f, "sfx:land_hard:0.28"), (19.8f, "line:FIN_14_01"),
                (20.6f, "shot:f61_colour_video_frame_giant_panther_arrives"), (22.8f, "line:FIN_14_02"), (24.4f, "sfx:giant_purr:0.45"), (27.0f, "shot:f62_colour_video_frame_the_touch"),
                (30.5f, "react:laugh_soft"), (38.0f, "sfx:grass_run:0.7"), (42.0f, "shot:f63_colour_video_frame_running_together"), (52.0f, "sfx:grass_run:0.5"),
                (60.5f, "shot:f64_colour_video_frame_beneath_the_tree"), (62.0f, "sfx:land_soft:0.4"), (63.0f, "purr"), (72.0f, "shot:f65_colour_video_frame_her_head_against_him"),
            };
            int ci = 0; AudioSource purr = null; bool fading = false; float t0 = Time.time;
            while (true)
            {
                float t = vp.isPlaying ? (float)vp.time : start + Time.time - t0;
                while (ci < cues.Length && t >= cues[ci].t)
                {
                    var p = cues[ci++].what.Split(':');
                    if (p[0] == "line") StartCoroutine(SayIds(p[1]));
                    else if (p[0] == "shot") StartCoroutine(Shot(p[1]));
                    else if (p[0] == "sfx") AudioKit.Play(p[1], null, float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture), 0.03f, AudioKit.Bus.Sfx);
                    else if (p[0] == "react") PlayReaction("seven", p[1]);
                    else if (p[0] == "purr") purr = Purr.Loop(Seven.transform, 0.8f);
                }
                if (!fading && t >= len - 6f) { fading = true; MusicSystem.Stop(8f); }
                if (t >= len - 0.06f || (!vp.isPlaying && Time.time - t0 > len - start + 1f)) break;
                yield return null;
            }
            // the final image held: 7 resting her head against him beneath the tree, in colour - long enough to breathe
            vp.Pause();
            yield return Shot("f66_colour_video_frame_the_final_image_held");
            yield return Wait(Auto ? 3f : 6f);
            // fade to credits; the quiet purr and the meadow continue briefly
            yield return UI.Fade(1f, 3f, 0f);
            UI.SetVideo(null, 1f, 0f); vp.Stop(); Destroy(go); rt.Release();
            yield return Wait(2f);
            if (purr != null) StartCoroutine(AudioKit.Fade(purr, 0f, 2.5f, true));
            if (breeze != null) StartCoroutine(AudioKit.Fade(breeze, 0f, 2.5f, true));
            Reactions.Suppress = false; DialogueFraming.VoiceOver = null;
            for (int i = 0; i < vols.Length; i++) if (vols[i] != null) vols[i].weight = was[i];
            Complete("FIN_14");
        }

        // ------------------------------------------------------------------------------------------ the credits + FIN_15 (7's sign-off)
        IEnumerator Credits()
        {
            InCredits = true;
            NoHud(true);
            UI.SetFade(1f, 0f);
            MusicSystem.EndSilence();
            MusicSystem.Play("credits_theme", 0.5f);
            var track = 180f;
            var mc = Resources.Load<AudioBank>("Audio/MusicBank");
            var tc = mc != null ? mc.Get("credits_theme") : null; if (tc != null) track = tc.length;
            float t0 = Time.time;
            // the layer: Kyle's art full screen (slow fades, gentle drift) under a rolling text column
            var root = UI.CanvasRoot;
            var artGo = new GameObject("CreditsArt", typeof(RectTransform)); artGo.transform.SetParent(root, false);
            var art = artGo.AddComponent<RawImage>(); art.raycastTarget = false; var art2Go = new GameObject("CreditsArt2", typeof(RectTransform)); art2Go.transform.SetParent(root, false); var art2 = art2Go.AddComponent<RawImage>(); art2.raycastTarget = false;
            foreach (var g in new[] { artGo, art2Go }) { var r = (RectTransform)g.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
            var shade = new GameObject("CreditsShade", typeof(RectTransform)); shade.transform.SetParent(root, false); { var r = (RectTransform)shade.transform; r.anchorMin = new Vector2(0.62f, 0f); r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; var im = shade.AddComponent<Image>(); im.color = new Color(0f, 0f, 0f, 0.55f); im.raycastTarget = false; }
            var colGo = new GameObject("CreditsText", typeof(RectTransform)); colGo.transform.SetParent(root, false);
            var col = (RectTransform)colGo.transform; col.anchorMin = col.anchorMax = new Vector2(0.81f, 0f); col.pivot = new Vector2(0.5f, 1f); col.sizeDelta = new Vector2(440f, 2000f);
            var txt = colGo.AddComponent<Text>(); txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); txt.fontSize = 22; txt.alignment = TextAnchor.UpperCenter; txt.color = Color.white; txt.raycastTarget = false; txt.lineSpacing = 1.15f;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap; txt.verticalOverflow = VerticalWrapMode.Overflow; txt.supportRichText = true;
            colGo.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.9f); colGo.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.55f);
            var ct = Resources.Load<TextAsset>("Finale/credits");
            txt.text = ct != null ? ct.text : "7: THE OBSERVER";
            // the sign-off's line sits on a soft dark panel low on the screen (Kyle's page is white: white text alone was unreadable)
            var signPanel = new GameObject("SignOffPanel", typeof(RectTransform)); signPanel.transform.SetParent(root, false);
            var spr = (RectTransform)signPanel.transform; spr.anchorMin = spr.anchorMax = spr.pivot = new Vector2(0.5f, 0f); spr.anchoredPosition = new Vector2(0f, 70f); spr.sizeDelta = new Vector2(760f, 84f);
            var spi = signPanel.AddComponent<Image>(); spi.sprite = ChapterUI.SoftSprite(); spi.type = Image.Type.Sliced; spi.color = new Color(0f, 0f, 0f, 0f); spi.raycastTarget = false;
            var sign = new GameObject("SignOff", typeof(RectTransform)); sign.transform.SetParent(signPanel.transform, false);
            var srt = (RectTransform)sign.transform; srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0.5f, 0.5f); srt.anchoredPosition = Vector2.zero; srt.sizeDelta = new Vector2(740f, 80f);
            var stx = sign.AddComponent<Text>(); stx.font = txt.font; stx.fontSize = 34; stx.alignment = TextAnchor.MiddleCenter; stx.color = new Color(1f, 1f, 1f, 0f); stx.raycastTarget = false; stx.fontStyle = FontStyle.Italic;
            sign.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.9f);
            var arts = KyleArt != null && KyleArt.Length > 0 ? KyleArt : new Texture[0];
            float signAt = Mathf.Max(60f, track - 24f);                 // the sign-off on the final held chord
            float roll = signAt - 6f;
            float per = arts.Length > 0 ? roll / arts.Length : roll;
            var shots = new HashSet<int>();
            StartCoroutine(UI.Fade(0f, 2.5f, 0f));
            Log($"CREDITS: credits_theme {track:0.0} s, {arts.Length} of Kyle's pieces at {per:0.0} s each, the text rolls for {roll:0} s, the sign-off at {signAt:0} s on kyle_7_thank_you");
            float skipHold = 0f;
            while (Time.time - t0 < roll)
            {
                float e = Time.time - t0;
                int i = arts.Length > 0 ? Mathf.Min(arts.Length - 1, (int)(e / per)) : 0;
                float k = arts.Length > 0 ? (e - i * per) / per : 0f;
                if (arts.Length > 0)
                {
                    var a = arts[i]; art.texture = a; Fit(art, a, 1.04f + 0.05f * k, k);
                    float fade = Mathf.Clamp01(k * per / 1.6f) * Mathf.Clamp01((1f - k) * per / 1.6f);
                    art.color = new Color(1f, 1f, 1f, Mathf.Max(0.0f, fade)); art2.color = new Color(1f, 1f, 1f, 0f);
                    if (!shots.Contains(i) && k > 0.4f) { shots.Add(i); if (i % 3 == 0) StartCoroutine(Shot($"f70_credits_card_{i + 1}_{a.name}")); }
                }
                col.anchoredPosition = new Vector2(0f, Mathf.Lerp(-40f, txt.preferredHeight + 760f, e / roll));
                // hold Start / Esc 2 s: skip to the sign-off (the credits play to the end otherwise)
                var gp = UnityEngine.InputSystem.Gamepad.current; var kb = UnityEngine.InputSystem.Keyboard.current;
                bool held = (gp != null && gp.startButton.isPressed) || (kb != null && kb.escapeKey.isPressed);
                skipHold = held ? skipHold + Time.deltaTime : 0f;
                if (skipHold > 2f || (Auto && e > 20f)) { Log($"CREDITS: {(Auto ? "the check run moves on" : "skipped by holding Start")} at {e:0} s"); break; }
                yield return null;
            }
            // the last image settles on kyle_7_thank_you; 7 speaks six lines (FIN_15), each fading in, a pause between
            art.color = new Color(1f, 1f, 1f, 0f); colGo.SetActive(false); shade.SetActive(false);
            if (KyleThanks != null) { art2.texture = KyleThanks; Contain(art2, KyleThanks); for (float t = 0f; t < 2.5f; t += Time.deltaTime) { art2.color = new Color(1f, 1f, 1f, t / 2.5f); yield return null; } }
            if (Auto && MusicSystem.Current == "credits_theme") { }   // (the check run moves faster than the music)
            DialogueFraming.VoiceOver = "FIN_15 (7's sign-off over Kyle's art)";
            string[] ids = { "FIN_15_01", "FIN_15_02", "FIN_15_03", "FIN_15_04", "FIN_15_05", "FIN_15_06" };
            foreach (var id in ids)
            {
                var l = FinaleDialogue.Ids(id); if (l.Count == 0) continue;
                for (float t = 0f; t < 0.7f; t += Time.deltaTime) { stx.text = l[0].Text; stx.color = new Color(1f, 1f, 1f, t / 0.7f); spi.color = new Color(0f, 0f, 0f, 0.62f * Mathf.Max(spi.color.a / 0.62f, t / 0.7f)); yield return null; }
                yield return Dlg.PlayLines(l, false);
                if (id == "FIN_15_01" || id == "FIN_15_06") yield return Shot($"f71_signoff_card_{id}");
                yield return Wait(Auto ? 0.6f : 1.6f);
                for (float t = 0f; t < 0.5f; t += Time.deltaTime) { stx.color = new Color(1f, 1f, 1f, 1f - t / 0.5f); yield return null; }
                yield return Wait(Auto ? 0.3f : 0.8f);
            }
            DialogueFraming.VoiceOver = null;
            spi.color = new Color(0f, 0f, 0f, 0f);
            // a slow fade to black, a beat of silence, the menu (the credits' layers sit above the fade: they fade themselves)
            StartCoroutine(UI.Fade(1f, 3f, 0f));
            for (float t = 0f; t < 3f; t += Time.deltaTime) { float k = 1f - t / 3f; art2.color = new Color(1f, 1f, 1f, k); art.color = new Color(1f, 1f, 1f, Mathf.Min(art.color.a, k)); yield return null; }
            Destroy(artGo); Destroy(art2Go); Destroy(shade); Destroy(colGo); Destroy(signPanel);
            MusicSystem.Stop(2.5f);
            yield return Wait(2.5f);
            yield return Shot("f72_black_after_the_credits");
            FinaleState.MarkFinished();
            ChapterDone = true; InCredits = false;
            Log("THE GAME IS FINISHED: the credits played to the end (FinaleState.MarkFinished); back to the main menu");
            if (DemoBoot.CaptureDir != null && Cap != null && !DemoBoot.FinPlay) { Cap.Finish($"FINALE DONE: {FinaleState.Summary()}"); yield break; }
            yield return Wait(1.2f);
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
        }
        void PlayReaction(string who, string kind)
        {
            var c = Reactions.Pick(who, kind, out _); if (c == null) return;
            var go = new GameObject("Reaction_" + who + "_" + kind); var a = go.AddComponent<AudioSource>(); a.spatialBlend = 0f; a.clip = c; a.volume = 0.8f * AudioKit.Gain(AudioKit.Bus.Voice); a.Play();
            Destroy(go, c.length + 0.2f); Log($"reaction: {who} {kind}");
        }
        /// <summary>The whole picture, letterboxed on black (Kyle's thank-you page must not be cropped).</summary>
        static void Contain(RawImage img, Texture tex)
        {
            var rt = (RectTransform)img.transform; rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero;
            float screen = Screen.width / (float)Mathf.Max(1, Screen.height), asp = tex.width / (float)Mathf.Max(1, tex.height), h = 720f, w = h * screen;
            rt.sizeDelta = asp > screen ? new Vector2(w, w / asp) : new Vector2(h * asp, h);
            img.uvRect = new Rect(0f, 0f, 1f, 1f);
        }
        static void Fit(RawImage img, Texture tex, float zoom, float drift)
        {
            float screen = Screen.width / (float)Mathf.Max(1, Screen.height), asp = tex.width / (float)Mathf.Max(1, tex.height);
            float w = Mathf.Min(1f, screen / asp) / zoom, h = Mathf.Min(1f, asp / screen) / zoom;
            float x = (1f - w) * Mathf.Lerp(0.42f, 0.58f, drift), y = (1f - h) * 0.5f;
            img.uvRect = new Rect(x, y, w, h);
        }
    }
}
