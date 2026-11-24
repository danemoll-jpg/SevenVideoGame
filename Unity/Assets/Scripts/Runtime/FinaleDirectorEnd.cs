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
                DrillSpeed = 1100f; if (Rig != null) Rig.ArmDrillWant = 1800f;
                // QF30 (6): the drilling grinds closer behind the rubble that blocks the mining access, then his rig bursts through it
                var grind = AudioKit.Play("drill_grind_rock", L.MiningAccess, 1f, 0f, AudioKit.Bus.Sfx, 40f);
                for (float t = 0f; t < 0.9f; t += Time.deltaTime) { if (Random.value < 0.25f) Sparks(L.MiningAccess + new Vector3(Random.Range(-2f, 2f), Random.Range(0.5f, 4f), -0.2f), 20); yield return null; }
                yield return TearOpen();                                                   // QF31 (8): the wall round the access torn open onto the cavern
                StartCoroutine(BurstPlug());
                for (float t = 0f; t < 1f; t += Time.deltaTime / 1.6f) { TunnelerCraft.position = Vector3.Lerp(a, L.TunnelerStart, Mathf.SmoothStep(0f, 1f, t)); if (t > 0.1f && Random.value < 0.2f) Sparks(L.MiningAccess + new Vector3(Random.Range(-3f, 3f), Random.Range(1f, 5f), 0f), 30); yield return null; }
                if (grind != null) grind.Stop();
                DrillSpeed = 220f; if (Rig != null) Rig.ArmDrillWant = 300f;
                ImageGate.Expect(TunnelerCraft, "Tunneler forcing his way in");
                yield return Shot("f32_tunneler_forces_his_way_in");
                if (Shots) { ImageGate.ExpectPoint(L.CavernC + Vector3.up * 2f, "the torn wall and the mining cavern beyond"); yield return Cap.ShotFrom("f32b_the_wall_torn_open_the_cavern_beyond", L.ChamberC + new Vector3(-3.5f, 3.4f, 6.5f), L.CavernC + Vector3.up * 1.5f, 62f); }
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
            else { TunnelerCraft.gameObject.SetActive(true); TunnelerCraft.position = L.TunnelerStart; Seven.Place(L.HerFightStart, Vector3.back); if (!_breached) BreachNow(); }
            // the confrontation: the three objectives while he attacks; Panther calls from the chamber's edge
            if (Cat != null) { Cat.Place(L.PantherEdge, Vector3.right); Cat.Hold(L.ChamberC, "sit"); }
            EnsureHover();
            _fight = TunnelerCraft.GetComponent<TunnelerFight>() ?? TunnelerCraft.gameObject.AddComponent<TunnelerFight>();
            _fight.Body = _hover; _fight.Dir = this; _fight.Rig = Rig;
            CameraBodyGuard.Extra.Clear(); CameraBodyGuard.Extra.AddRange(TunnelerCraft.GetComponentsInChildren<Renderer>());   // the fight camera never sits inside him
            var rockMat = new Material(Shader.Find("Seven/DissolveLit") ?? Shader.Find("Universal Render Pipeline/Lit")) { name = "Fin_debris" };
            if (rockMat.HasProperty("_BaseColor")) rockMat.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.3f));
            _fight.Setup(null, rockMat);
            foreach (var p in _plug) if (p != null) _fight.AddThrowable(p);                  // QF30 (6): the rubble he broke through lies where it fell - loose
            foreach (var p in _breachRubble) if (p != null) _fight.AddThrowable(p);           // QF31 (8): the torn wall's stone, along the breach
            _fight.PlaceStart();
            if (FinaleState.StepDone("barrier")) _fight.ForceBarrierOpen();
            _fight.UI = UI; _fight.AutoPilot = DemoBoot.CaptureDir != null;
            _fight.Beat = PhaseBeatShot;
            _fight.StunBeat = b => { if (_stunShots.Add(b) && Shots) StartCoroutine(Shot(b == "drop" ? "f37b_stunned_the_rig_settles_flush" : b == "deck" ? "f37c_she_steps_onto_its_edge" : b == "strike" ? "f37d_her_blows_land_on_him" : "f37e_he_recovers_the_rig_rises")); };
            _fight.OnObjective = what =>
            {
                if (what == "barrier") { FinaleState.DoStep("barrier"); StartCoroutine(BarrierOpens()); }
                if (what == "conduit") { FinaleState.DoStep("conduit"); }
            };
            if (Shots) StartCoroutine(ControlsBefore());
            _fight.PhaseObjective = ph => ph == 0 ? "Keep him away from the archive"
                                       : ph == 1 ? "Open the old maintenance barrier - its control by the east alcove"
                                       : "Restore the connection he cut - the junction box on the west wall";
            yield return Cutscene(false);
            FollowCamera();
            SetZone("DoorZ", false);   // the doorway closes behind the fight (she can't be driven out of the chamber). QF31 (10): ChamberN stays ON - rc2 switched the whole north band off for her while his arena still reached into it: stunned up there, he was out of her reach
            _fight.Begin(BossFight.Load("tunneler"), Seven, Cam);
            ThemeNow = "moon_tunneler";
            if (Shots) StartCoroutine(ArenaShots());
            StartCoroutine(FightPrompts(_fight));
            _fight.Moment += FightMoment;
            StartCoroutine(FightCalls());
            int downs = 0;
            var run = StartCoroutine(_fight.Run(s => { if (s != null && !s.StartsWith("He is dazed")) Objective(s); }));
            if (DemoBoot.StunTest > 0 && DemoBoot.CaptureDir != null) { yield return StunTest(DemoBoot.StunTest); yield break; }   // QF31 (10): the proof run ends here
            while (!_fight.Over && !_fight.Dazed)
            {
                if (_fight.Knockdowns > downs) { downs = _fight.Knockdowns; FinaleState.CountRetry(); Log($"CONFRONTATION RETRY (Round 19 8): she went down ({downs}x) - the retry keeps the objectives done ({(_fight.BarrierOpen ? "barrier open" : "barrier closed")}, conduit {(_fight.ConduitRestored ? "restored" : _fight.ConduitCut ? "cut" : "whole")}) and his energy {_fight.Energy * 100f:0}%"); }
                yield return null;
            }
            if (DemoBoot.BossIdle) yield break;
            StopCoroutine(run);
            _fight.Moment -= FightMoment;
            SetZone("DoorZ", true);
            Log(_fight.Summary());
            Log($"STUN REACH (QF31 10): stuns {_fight.StunTries}, reached and hit in {_fight.StunReached} (first blow on average {(_fight.StunReached > 0 ? _fight.StunFirstHitSum / _fight.StunReached : 0f):0.0} s after the stun); she stepped onto its edge {_fight.DeckLandings}x (no jump), {_fight.DeckStrikes} blows there; whiffed blows {_fight.Whiffs}; {string.Join("; ", _fight.StunLog)}; beats seen from the player's camera: {string.Join(", ", _stunShots)}");
            Log($"TUNNELER FIGHT (Round 19 8): objectives - kept off the platform (phase 1 by her blows), barrier {(_fight.BarrierOpen ? "open" : "CLOSED")}, conduit {(_fight.ConduitRestored ? "restored" : "NOT restored")}; Panther's calls {_calls}, his distraction {(_distracted ? "used once" : "not used")}; retries {FinaleState.Retries}");
            Objective(null);
            if (DemoBoot.BossCheck && DemoBoot.CaptureDir != null && !DemoBoot.FinPlay) { Cap.Finish("boss check (Round 19): " + _fight.Summary()); yield break; }
        }
        int _calls; bool _distracted; readonly HashSet<string> _stunShots = new HashSet<string>();
        IEnumerator FightPrompts(BossFight f)
        {
            while (f != null && !f.Over && !f.Dazed && f.isActiveAndEnabled) { if (!_cut) { var l = f.PromptLabel; SetPrompt(l, f.PromptIsTk ? ControlsLegend.Btn.Tk : ControlsLegend.Btn.Interact); } yield return null; }
            SetPrompt(null);
        }
        void FightMoment(string m) { if (m == "waiting" && _fight.Phase == 1 && _barrierBeacon == null && !_fight.BarrierOpen) { _barrierBeacon = Highlight("the barrier control", L.BarrierControl, BarrierCable, BarrierLines, BarrierLight, BarrierFrameLines); } if (m == "waiting") Log($"boss tunneler: his bar waits at the mark - the place to go is lit ({(_fight.Phase == 1 ? "the barrier control" : "the conduit's control")})"); }
        /// <summary>Close on a control's carved face: the glyph panel, the node, the light-lines round its foot.</summary>
        static Vector3 ConsoleCam(Vector3 at) { var f = Flat(L.ChamberC - at).normalized; var sd = Vector3.Cross(Vector3.up, f); return at + f * 1.7f + sd * 0.7f + Vector3.up * 1.45f; }
        /// <summary>QF30 (11): the controls before the fight changes them (the barrier's dormant, the conduit's live).</summary>
        IEnumerator ControlsBefore()
        {
            yield return Wait(0.5f);
            ImageGate.ExpectPoint(L.BarrierControl + Vector3.up * 1f, "the barrier's control, dormant");
            yield return Cap.ShotFrom("f36a_the_barrier_control_before", ConsoleCam(L.BarrierControl), L.BarrierControl + Vector3.up * 0.95f, 42f);
            ImageGate.ExpectPoint(L.ConduitAt + Vector3.up * 1f, "the conduit's control, live");
            yield return Cap.ShotFrom("f36a2_the_conduit_control_before", ConsoleCam(L.ConduitAt), L.ConduitAt + Vector3.up * 0.95f, 42f);
        }
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
                // QF30 (9/10): the worker over the network - the old barrier; its control and its frame lit, the place clearly marked
                Shoot(L.ChamberC + new Vector3(1.5f, 3.2f, 5.0f), L.BarrierAt + Vector3.up * 1.8f, 58f, 0.01f);
                _barrierBeacon = Highlight("the barrier control", L.BarrierControl, BarrierCable, BarrierLines, BarrierLight, BarrierFrameLines);
                yield return Wait(0.4f);
                ImageGate.ExpectPoint(L.BarrierAt + Vector3.up * 2.2f, "the old barrier's alcove");
                yield return Shot("f36_the_old_barrier_its_control_lit");     // (before the line: the framing may change for it)
                yield return DialogueFraming.Over("the old maintenance barrier's alcove (the worker over the network)", SayIds("FIN_08_P7"));   // The old maintenance barrier! Open it and get him inside. We can lock it from here.
                FollowCamera();
                // QF31 (9): with the barrier's control lit (its light column on), every screen stays clear - from the fight's whole floor
                foreach (var (w, at) in new[] { ("the breach mouth", L.TunnelerStart), ("the cavern", L.CavernC), ("the chamber's south", L.ChamberC + new Vector3(0f, 0f, -9f)), ("the chamber's west", L.ChamberC + new Vector3(-9f, 0f, 0f)), ("the entrance", L.ChamberC + new Vector3(0f, 0f, 9.5f)) })
                    ScreenViewCheck($"FIN_08 the barrier's control lit - from {w}", at + Vector3.up * 1.7f);
            }
            else if (ended == 1)
            {
                // he swings at the west conduit and cuts it: the light on the left goes out
                var inw = Flat(L.ChamberC - L.ConduitAt).normalized; var cs = Vector3.Cross(Vector3.up, inw);
                var a = _fight.transform.position; var b = L.ConduitAt + inw * 3.6f - cs * 1.6f;
                Shoot(L.ConduitAt + inw * 4.6f + cs * 5.2f + Vector3.up * 2.4f, L.ConduitAt + inw * 1.0f + Vector3.up * 1.2f, 54f, 0.01f);   /* a cut, not an ease: the frame never depends on when the capture lands */
                for (float t = 0f; t < 1f; t += Time.deltaTime / 1.0f) { _fight.transform.position = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t)); _fight.transform.rotation = Quaternion.Slerp(_fight.transform.rotation, Quaternion.LookRotation(Flat(L.ConduitAt - _fight.transform.position)), t); yield return null; }
                DrillSpeed = 1400f;
                AudioKit.Play("europa_rupture", L.ConduitAt + Vector3.up * 2f, 1f); CameraShake.Quake(0.4f, 0.4f);
                Sparks(L.ConduitAt + Vector3.up * 2.4f, 140);
                Glow(ConduitBreak, 0f); Glow(ConduitLines, 0f); Glow(ConduitNode, 0.05f);          // QF30 (11): the light-lines dead, the node dark
                _fight.ConduitCut = true;
                ImageGate.ExpectPoint(L.ConduitAt + Vector3.up * 2f, "the cut conduit");
                yield return Shot("f38_he_cuts_the_conduit");
                if (Shots) { ImageGate.ExpectPoint(L.ConduitAt + Vector3.up * 0.9f, "the conduit's control, damaged"); yield return Cap.ShotFrom("f38b_the_conduit_control_damaged", L.ConduitAt + Flat(L.ChamberC - L.ConduitAt).normalized * 3.4f + Vector3.up * 1.9f + Vector3.forward * 1.2f, L.ConduitAt + Vector3.up * 0.5f, 50f); }
                yield return SayIds("FIN_08_P4");                       // He's cut the conduit. The light's gone out on the left.
                _conduitBeacon = Highlight("the conduit's control", L.ConduitAt, null, ConduitNode);
                FollowCamera();
            }
        }
        IEnumerator BarrierOpens()
        {
            BeaconDone(_barrierBeacon, 1.4f); Glow(BarrierLight, 2.2f); Glow(BarrierFrameLines, 1.4f); Glow(BarrierLines, 1.1f);   // QF30 (9/10): the done state - the frame lit, the lines steady
            AudioKit.Play("bia_barrier", L.BarrierAt + Vector3.up * 2f, 0.9f);
            if (Shots) { ImageGate.ExpectPoint(L.BarrierControl + Vector3.up * 1.0f, "the barrier control, opened"); yield return Cap.ShotFrom("f36b_the_barrier_control_opened", L.BarrierControl + new Vector3(-6.0f, 2.6f, 4.6f), Vector3.Lerp(L.BarrierControl, L.BarrierAt, 0.5f) + Vector3.up * 1.6f, 54f); }
            Log("FIN_08: the old maintenance barrier opened - the alcove is ready (its field will close behind him)");
        }

        // QF30: FIN_10 in FinaleDirectorQF30Scenes.cs; QF31: FIN_09, the grief + memories, FIN_12 and the rise in FinaleDirectorQF31.cs
        /// <summary>QF31 (8): the open room and the arena from the fight camera (the chamber, the breach, the cavern).</summary>
        IEnumerator ArenaShots()
        {
            yield return Wait(1.5f);
            ImageGate.ExpectPoint(L.ChamberC + new Vector3(0f, 1f, -8f), "the arena: the chamber's open floor, the breach, the cavern");
            yield return Shot("f36c_the_arena_from_the_fight_camera");
            ImageGate.ExpectPoint(L.CavernC + Vector3.up * 1f, "the mining cavern (the arena's far side)");
            yield return Cap.ShotFrom("f36d_the_arena_wide_chamber_and_cavern", L.TunnelerStart + new Vector3(2.5f, 6.5f, 6.5f), L.CavernC + new Vector3(0f, 0.5f, 2f), 66f);   // from above the breach, down into the cavern
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
            float start = 17.6f, len = (float)clip.length, asp = clip.width / (float)clip.height;   // QF30 (21): she stands alone, her back to us - checked frame by frame in the game copy: a speck of the small cat's tail is still at the right edge through 17.5 s, 17.6 s is the first clean frame; the giant's head enters at ~18.0 s
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
                (17.7f, "shot:f60_colour_video_frame_her_from_behind"), (17.9f, "sfx:grass_rustle:0.4"), (18.4f, "audit"), (18.6f, "line:FIN_14_01"), (19.2f, "sfx:land_hard:0.28"),
                (20.6f, "shot:f61_colour_video_frame_giant_panther_arrives"), (22.8f, "line:FIN_14_02"), (24.4f, "sfx:giant_purr:0.45"), (27.0f, "shot:f62_colour_video_frame_the_touch"),
                (30.5f, "react:laugh_soft"), (38.0f, "sfx:grass_run:0.7"), (42.0f, "shot:f63_colour_video_frame_running_together"), (52.0f, "sfx:grass_run:0.5"),
                (60.5f, "shot:f64_colour_video_frame_beneath_the_tree"), (62.0f, "sfx:land_soft:0.4"), (63.0f, "purr"), (72.0f, "shot:f65_colour_video_frame_her_head_against_him"),
                // QF31 (A7): the Voice moves into heaven - in the stand-in, timed to end a few seconds before the master's last held image
                (Mathf.Max(start + 30f, (float)clip.length - RestVoiceLen() - 3.5f), "line:FIN_13_04"),
            };
            System.Array.Sort(cues, (a, b) => a.t.CompareTo(b.t));
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
                    else if (p[0] == "audit") QF30Watch.AudioAudit("FIN_14 (the colour meadow)", EndingSounds);
                }
                if (!fading && t >= len - 6f) { fading = true; MusicSystem.Stop(8f); }
                if (t >= len - 0.06f || (!vp.isPlaying && Time.time - t0 > len - start + 1f)) break;
                yield return null;
            }
            // the final image held: 7 resting her head against him beneath the tree, in colour - long enough to breathe
            vp.Pause();
            QF30Watch.AudioAudit("FIN_14 (the final image)", EndingSounds);
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
            // QF30 (23): an opaque black layer under the whole sequence, and the game camera renders nothing - Kyle's art cross-fades into
            // black and into each other, never into the lunar chamber
            var blackGo = new GameObject("CreditsBlack", typeof(RectTransform)); blackGo.transform.SetParent(root, false);
            { var r = (RectTransform)blackGo.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; var im = blackGo.AddComponent<Image>(); im.color = Color.black; im.raycastTarget = false; }
            var gameCam = Cam != null ? Cam.GetComponent<Camera>() : null; int mask0 = 0;
            if (gameCam != null) { mask0 = gameCam.cullingMask; gameCam.cullingMask = 0; gameCam.clearFlags = CameraClearFlags.SolidColor; gameCam.backgroundColor = Color.black; }
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if (r.gameObject.layer != 5) r.enabled = false;
            Log("CREDITS (QF30 23): on an opaque black layer; the game camera renders nothing (culling mask 0) and every world renderer is off - the game world can never show between Kyle's pieces");
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
            float skipHold = 0f; int sec = -1; StartCoroutine(CreditsAudit());
            while (Time.time - t0 < roll)
            {
                if (Shots && (int)(Time.time - t0) != sec) { sec = (int)(Time.time - t0); StartCoroutine(CreditsFrame(sec, blackGo)); }
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
            // QF30 (24): after 7's sign-off, a beat, then Kyle's credit alone, centred, softly (a surprise for him: only here, never in public text)
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { spi.color = new Color(0f, 0f, 0f, 0.62f * (1f - t / 0.6f)); yield return null; }
            spi.color = new Color(0f, 0f, 0f, 0f); stx.text = ""; signPanel.SetActive(false);
            for (float t = 0f; t < 1.4f; t += Time.deltaTime) { float k = 1f - t / 1.4f; art2.color = new Color(1f, 1f, 1f, k); yield return null; }
            art2.color = new Color(1f, 1f, 1f, 0f);
            yield return Wait(Auto ? 0.6f : 1.2f);
            var kyleGo = new GameObject("KyleCredit", typeof(RectTransform)); kyleGo.transform.SetParent(root, false);
            var krt = (RectTransform)kyleGo.transform; krt.anchorMin = krt.anchorMax = krt.pivot = new Vector2(0.5f, 0.5f); krt.anchoredPosition = Vector2.zero; krt.sizeDelta = new Vector2(1100f, 80f);
            var ktx = kyleGo.AddComponent<Text>(); ktx.font = txt.font; ktx.fontSize = 28; ktx.alignment = TextAnchor.MiddleCenter; ktx.supportRichText = true; ktx.raycastTarget = false;
            ktx.text = "Closing lines from <i>7</i> (2014) by Kyle G Anderson"; ktx.color = new Color(1f, 1f, 1f, 0f);
            for (float t = 0f; t < 1.4f; t += Time.deltaTime) { ktx.color = new Color(1f, 1f, 1f, t / 1.4f * 0.92f); yield return null; }
            yield return Shot("f71b_kyle_credit_card");
            yield return Wait(Auto ? 2.0f : 4.0f);
            for (float t = 0f; t < 1.6f; t += Time.deltaTime) { ktx.color = new Color(1f, 1f, 1f, 0.92f * (1f - t / 1.6f)); yield return null; }
            Destroy(kyleGo);
            Log("CREDITS (QF30 24): after the sign-off, Kyle's credit alone, centred, held ~4 s, then the slow fade to black");
            // a slow fade to black, a beat of silence, the menu (the credits' layers sit above the fade: they fade themselves)
            StartCoroutine(UI.Fade(1f, 3f, 0f));
            for (float t = 0f; t < 3f; t += Time.deltaTime) { float k = 1f - t / 3f; art2.color = new Color(1f, 1f, 1f, k); art.color = new Color(1f, 1f, 1f, Mathf.Min(art.color.a, k)); yield return null; }
            Destroy(artGo); Destroy(art2Go); Destroy(shade); Destroy(colGo); Destroy(signPanel); Destroy(blackGo);
            MusicSystem.Stop(2.5f);
            yield return Wait(2.5f);
            yield return Shot("f72_black_after_the_credits");
            Log($"CREDITS (QF30 23): {_creditsFrames} frames checked (one a second) - frames where the game world could show {_creditsDirty} (must be 0)");
            FinaleState.MarkFinished();
            ChapterDone = true; InCredits = false;
            Log("THE GAME IS FINISHED: the credits played to the end (FinaleState.MarkFinished); back to the main menu");
            if (DemoBoot.CaptureDir != null && Cap != null && !DemoBoot.FinPlay) { Cap.Finish($"FINALE DONE: {FinaleState.Summary()}"); yield break; }
            yield return Wait(1.2f);
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
        }
        /// <summary>QF30 (22): the only sounds allowed in FIN_14 and the credits (clip-name prefixes).</summary>
        static readonly string[] EndingSounds = { "cattalus_meadow", "credits_theme", "cat_meadow_breeze", "grass_", "land_", "giant_purr", "cat_purr", "FIN_13_04", "FIN_14", "FIN_15", "seven_", "Seven_", "laugh_" };
        static float RestVoiceLen() { var l = FinaleDialogue.Ids("FIN_13_04"); return l.Count > 0 && l[0].Clip != null ? l[0].Clip.length : 14f; }
        IEnumerator CreditsAudit() { yield return Wait(4f); QF30Watch.AudioAudit("the credits", EndingSounds); }
        /// <summary>QF30 (23): a capture every second of the credits (exempt "card" frames) + a check that the black layer is whole.</summary>
        IEnumerator CreditsFrame(int sec, GameObject black)
        {
            var im = black != null ? black.GetComponent<Image>() : null; var gc = Cam != null ? Cam.GetComponent<Camera>() : null;
            bool clean = im != null && im.enabled && im.color.a >= 0.999f && (gc == null || gc.cullingMask == 0);
            if (!clean) { _creditsDirty++; Log($"CREDITS (QF30 23) {sec} s: THE GAME WORLD COULD SHOW (black layer {(im != null ? im.color.a.ToString("0.00") : "missing")}, camera mask {(gc != null ? gc.cullingMask : -1)})"); }
            _creditsFrames++;
            yield return Shot($"f70c_credits_card_t{sec:00}");
        }
        int _creditsFrames, _creditsDirty;
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
