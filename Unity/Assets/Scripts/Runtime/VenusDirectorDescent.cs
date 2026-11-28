using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using L = Seven.VenusLayout;

namespace Seven
{
    /// <summary>
    /// QF43 (14): the way down to the Wormmaster is a traversal route, not a gate that lowers by itself. The root gate stays shut
    /// (jammed from below); Panther: "That's not opening from this side."; she goes round it down the ROOT WALL beside it
    /// (VenusLayout: the slot east of the pit), in Chapter 1's moves and clips:
    ///   1. at the gap in the lookout's railing, climb down into a hang on the edge (B / crouch) and shimmy left above the shelf;
    ///      let go - onto the shelf (letting go over the deep part = a fall: white fade, back at the top);
    ///   2. the root chasm: jump it (A) - only between pulses: on every pulse the hanging roots over it thrash and grit pours down,
    ///      and the jump is not there ("Wait for the next one... now.");
    ///   3. the drop into the burrows: climb down into a hang off the lower ledge and let go;
    ///   4. the sill into the pit is too high: lift the fallen slab into place with telekinesis (RT) - a step - and climb onto it;
    ///   5. in the pit: pull the gate's inner release (X) - the gate up at the top sinks open (the short way back up from now on).
    /// Then on into the tunnels as before. A fall anywhere lands her at the last safe spot (Chapter 1's rule). The guide light
    /// leads every step. Check runs (Auto) and player-style runs drive the route with <see cref="DescentBot"/> (a precise bot in
    /// check runs, human timing - late / early presses, overshooting the shimmy - in player-style runs); both log
    /// DESCENT (QF43 14) per step.
    /// </summary>
    public partial class VenusDirector
    {
        Transform _cutSlab, _lever, _hangRoots; bool _slabPlaced, _released, _leverHit, _slabTk;
        float _lastPulseT = -99f;
        public bool DescentBotActive { get; private set; }
        bool PulseActive => Time.time - _lastPulseT < 2.6f;

        void DescentMoves()
        {
            if (!VenusState.SceneDone("VEN_C05")) { VenusLayout.UseTraversal(); return; }
            MoonTraversal.Use(new[] { L.CutLip, L.CutLedgeLip }, PulseActive && !_released ? null : new[] { L.ChasmJump }, null, null,
                _slabPlaced ? new[] { L.DetourMantle, L.LedgeMantle, L.CutSlabStep } : new[] { L.DetourMantle, L.LedgeMantle },
                new[] { L.CutLipDown, L.CutLedgeDown });
        }

        void BuildDescentProps()
        {
            if (_cutSlab != null) return;
            var rock = Markers.DarkGloss(); var chalk = new Material(Shader.Find("Universal Render Pipeline/Unlit")); chalk.SetColor("_BaseColor", new Color(0.92f, 0.92f, 0.92f));
            var stone = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.4f, 0.4f, 0.4f) };
            // chalk-glow strips just under every lip she can use (Chapter 1's handhold language)
            var cv = new List<Vector3>(); var ct = new List<int>();
            void Strip(Vector3 a, Vector3 b, Vector3 outward) { var d = (b - a); var up = Vector3.up * 0.035f; var o = outward * 0.02f; Quad(cv, ct, a + o - up * 3.4f, b + o - up * 3.4f, b + o - up * 2.4f, a + o - up * 2.4f); Quad(cv, ct, a + o - up * 2.4f, b + o - up * 2.4f, b + o - up * 3.4f, a + o - up * 3.4f); }
            Strip(L.CutLip.A, L.CutLip.B, Vector3.back);
            Strip(L.CutLedgeLip.A, L.CutLedgeLip.B, Vector3.back);
            Strip(new Vector3(L.CutX0, L.ShelfY, L.ChasmZ0 + 2.2f), new Vector3(L.ShelfX1, L.ShelfY, L.ChasmZ0 + 2.2f), Vector3.back);   // the shelf's jumping edge
            MakeMesh("QF43_DescentChalk", cv, ct, chalk, null);
            // the fallen slab (a broken paving slab from the old stairs), lying on the burrow floor
            var sv = new List<Vector3>(); var st = new List<int>(); var h = new Vector3(0.75f, 0.5f, 0.7f);
            Vector3 C(int i) => new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
            int[] f = { 0, 2, 3, 1, 4, 5, 7, 6, 0, 1, 5, 4, 2, 6, 7, 3, 0, 4, 6, 2, 1, 3, 7, 5 };
            for (int k = 0; k < 6; k++) Quad(sv, st, C(f[k * 4]), C(f[k * 4 + 1]), C(f[k * 4 + 2]), C(f[k * 4 + 3]));
            var slab = MakeMesh("QF43_FallenStairSlab", sv, st, stone, null); _cutSlab = slab.transform;
            _cutSlab.SetPositionAndRotation(L.CutSlabRest + Vector3.up * 0.15f, Quaternion.Euler(8f, 24f, 14f));
            // the gate's inner release: a grown lever on the pit's west wall, its root running up toward the gate
            var lv = new List<Vector3>(); var lt = new List<int>();
            void Bar(Vector3 a, Vector3 b, float r) { var d = (b - a).normalized; var s1 = Vector3.Cross(d, Vector3.right).sqrMagnitude > 0.01f ? Vector3.Cross(d, Vector3.right).normalized * r : Vector3.forward * r; var s2 = Vector3.Cross(d, s1).normalized * r; for (int i = 0; i < 6; i++) { float a0 = i / 6f * Mathf.PI * 2f, a1 = (i + 1) / 6f * Mathf.PI * 2f; var o0 = s1 * Mathf.Cos(a0) + s2 * Mathf.Sin(a0); var o1 = s1 * Mathf.Cos(a1) + s2 * Mathf.Sin(a1); Quad(lv, lt, a + o0, b + o0, b + o1, a + o1); } }
            var baseY = L.PitY(L.GateRelease.z);
            var lp = new Vector3(L.GateRelease.x, baseY, L.GateRelease.z);
            Bar(lp, lp + new Vector3(-0.1f, 1.0f, 0f), 0.09f);
            var post = MakeMesh("QF43_GateReleasePost", lv, lt, rock, null);
            lv.Clear(); lt.Clear(); Bar(Vector3.zero, new Vector3(0.45f, 0.45f, 0f), 0.04f);
            var handle = MakeMesh("QF43_GateReleaseHandle", lv, lt, stone, null); handle.transform.position = lp + new Vector3(-0.1f, 0.95f, 0f);
            _lever = handle.transform;
            var lamp = new GameObject("QF43_ReleaseGlow").AddComponent<Light>(); lamp.transform.position = lp + Vector3.up * 1.4f + Vector3.right * 0.4f;
            lamp.type = LightType.Point; lamp.color = Color.white; lamp.range = 3f; lamp.intensity = 0.9f; lamp.shadows = LightShadows.None;
            Ground.Obstacles.Add(new Ground.Circle(new Vector2(lp.x, lp.z), 0.3f));
            var hr = GameObject.Find("Cut_HangingRoots"); _hangRoots = hr != null ? hr.transform : null;
        }

        /// <summary>On every pulse during the descent: the hanging roots over the chasm thrash, grit pours down, the jump is off.</summary>
        IEnumerator DescentPulseFx()
        {
            int seen = _pulsesFired; var r0 = _hangRoots != null ? _hangRoots.rotation : Quaternion.identity;
            while (!_released)
            {
                if (_pulsesFired != seen)
                {
                    seen = _pulsesFired; _lastPulseT = Time.time;
                    DescentMoves();
                    if (SlabDust != null) for (int i = 0; i < 4; i++) { SlabDust.transform.position = new Vector3(L.CutX0 + 0.5f + i * 1.1f, -6.2f, L.ChasmZ0 + 1.1f); SlabDust.Emit(10); }
                    AudioKit.Play("stone_rain", new Vector3(75f, -9f, 128.3f), 0.6f, 0.05f, AudioKit.Bus.Sfx, 40f);
                }
                bool on = PulseActive;
                if (_hangRoots != null) _hangRoots.rotation = on ? r0 * Quaternion.Euler(Mathf.Sin(Time.time * 17f) * 2.2f, 0f, Mathf.Cos(Time.time * 13f) * 2.2f) : Quaternion.Slerp(_hangRoots.rotation, r0, Time.deltaTime * 4f);
                if (!on && _lastPulseT > 0f && Time.time - _lastPulseT < 2.7f) DescentMoves();     // the jump is back as soon as it passes
                yield return null;
            }
            if (_hangRoots != null) _hangRoots.rotation = r0;
        }

        bool OnZone(string n, float tol = 0.3f) { var z = Ground.FindZone(n); return z != null && z.Enabled && z.Contains(P7.x, P7.z) && Mathf.Abs(P7.y - z.HeightAt(P7.x, P7.z)) < tol && Seven.Current == ChapterSeven.Mode.Free; }
        void DescentZones(bool on) { foreach (var n in new[] { "CutShelf", "CutLedge", "CutBottom", "RootSill", "RootPitLow" }) SetZone(n, on); SetZone("CutSlab", on && _slabPlaced); }

        /// <summary>The state of the descent for the save: the slot and its floors open after C05; the slab and the release persist.</summary>
        void RestoreDescent()
        {
            BuildDescentProps();
            bool c05 = VenusState.SceneDone("VEN_C05");
            _released = VenusState.SceneDone("VEN_root_release"); _slabPlaced = _released || VenusState.SceneDone("VEN_cut_slab");
            if (_slabPlaced) _cutSlab.SetPositionAndRotation(L.CutSlabAt, Quaternion.identity);
            DescentZones(c05);
            SetZone("RootLanding", _released); SetZone("RootPit", _released);
            if (RootGate != null) RootGate.SetActive(!_released);
            if (_released && _lever != null) _lever.rotation = Quaternion.Euler(0f, 0f, -70f);
            DescentMoves();
        }

        /// <summary>QF43 (14): the way down - the route round the jammed gate (replaces the gate that lowered by itself).</summary>
        IEnumerator WayDown()
        {
            RestoreDescent();
            DescentZones(true);
            StartCoroutine(DescentPulseFx());
            if (!_released)
            {
                // the gate won't move: Panther says so; she sees the root wall beside it
                yield return Cutscene(true);
                var g = L.RootGateIn + Vector3.up * 1.6f;
                for (float t = 0f; t < 0.4f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(g - P7)), 300f * Time.deltaTime); yield return null; }
                Shoot(Head7 - Flat(g - P7).normalized * 2.2f + Vector3.Cross(Vector3.up, Flat(g - P7).normalized) * 0.9f + Vector3.up * 0.4f, g, 48f, 0.01f);
                AudioKit.Play("stone_door", L.RootGateIn, 0.6f, 0.1f, AudioKit.Bus.Sfx, 40f);
                yield return SayDescent(1);                                      // That's not opening from this side.
                var wall = new Vector3(75f, -9f, 131f);
                Shoot(new Vector3(75.4f, -3.0f, 138.2f), new Vector3(75.0f, -11.5f, 127.5f), 56f, 0.01f);      // the root wall going down beside it (from the lookout, down into the slot)
                ImageGate.ExpectPoint(wall, "the root wall beside the jammed gate", 0f, "qf43_14a");
                yield return Wait(0.4f);
                yield return Shot("qf43_14a_the_root_wall_beside_the_gate");
                yield return SayDescent(2);                                      // The root wall beside it goes down. I'll go around.
                FollowCamera(); Cam.SnapBehind();
                yield return Cutscene(false);
                Objective("Find a way down to the source - climb down the root wall beside the gate");
                if (Auto || DemoBoot.PlayerSim) StartCoroutine(DescentBot(DemoBoot.PlayerSim));
                // 1. down onto the shelf
                yield return Until(() => OnZone("CutShelf") || OnZone("CutLedge") || OnZone("CutBottom") || _released, "descent: the shelf below the edge", 120f,
                    "At the gap in the railing beside the gate: crouch (B) at the edge to climb down into a hang, shimmy left and let go over the shelf",
                    L.CutLipDown.Spot, () => Seven.Place(new Vector3(74.2f, L.ShelfY, 131.5f), Vector3.back));
                Log($"DESCENT (QF43 14): on the shelf below the edge ({P7:0.0})");
                yield return Shot("qf43_14b_on_the_shelf");
                // 2. the chasm, between pulses
                if (!OnZone("CutLedge") && !OnZone("CutBottom") && !_released) yield return SayDescent(3);    // Wait for the next one... now.
                yield return Until(() => OnZone("CutLedge") || OnZone("CutBottom") || _released, "descent: across the root chasm", 120f,
                    "Jump the chasm (A) between the pulses - when the roots over it are still", L.ChasmJump.From, () => Seven.Place(new Vector3(75f, L.CutLedgeY, 124f), Vector3.back));
                Log($"DESCENT (QF43 14): across the root chasm between pulses (last pulse {Time.time - _lastPulseT:0.0} s before)");
                yield return Shot("qf43_14c_across_the_chasm");
                // 3. the drop into the burrows
                yield return Until(() => OnZone("CutBottom") || OnZone("CutSlab") || OnZone("RootSill") || OnZone("RootPitLow") || _released, "descent: down into the burrows", 120f,
                    "At the end of the ledge crouch (B) to climb down into a hang, then let go", L.CutLedgeDown.Spot, () => Seven.Place(new Vector3(75.5f, L.CutBotY, 119.5f), Vector3.left));
                Log("DESCENT (QF43 14): dropped into the burrows");
                // 4. the slab step
                if (!_slabPlaced)
                {
                    Objective("The sill is too high - lift the fallen slab into place (telekinesis)");
                    _slabTk = false;
                    StartCoroutine(SlabPrompt());
                    yield return Until(() => _slabTk, "descent: the slab step (telekinesis)", 90f, "Look at the fallen slab on the floor and use telekinesis (RT)", L.CutSlabRest, () => _slabTk = true);
                    yield return LiftCutSlab();
                }
                Objective("Climb up onto the slab and into the pit");
                yield return Until(() => OnZone("RootSill") && P7.x < 72.9f || OnZone("RootPitLow") || _released, "descent: up the slab into the pit", 90f,
                    "Walk up to the slab and press A to climb onto it, then on into the pit", L.CutSlabStep.From, () => Seven.Place(new Vector3(70f, L.PitY(119f), 119f), Vector3.left));
                // 5. the gate's inner release
                Objective("Pull the gate's release");
                _leverHit = false;
                StartCoroutine(LeverPrompt());
                yield return Until(() => _leverHit, "descent: the gate's inner release", 90f, "The release on the pit's west wall: X", L.GateRelease, () => _leverHit = true);
                yield return PullRelease();
            }
            Objective(VenusState.Objectives[(int)VenusState.Stage.ControlDiscovered]);
            if (Auto && !DemoBoot.PlayerSim) StartCoroutine(AutoWalkPath(new[] { new Vector3(69f, -14f, 117.4f), new Vector3(69f, -18f, 107f), new Vector3(69f, -22f, 95f), new Vector3(71f, -22f, 89f), new Vector3(80f, -22f, 89f), new Vector3(96f, -27.4f, 89f), L.ControlEntry }));
            if (Shots) StartCoroutine(WayDownShots());
            StartCoroutine(WayDownHarass());                             // Round 13 (0f)
            yield return Until(() => P7.x > 104.5f && P7.y < -28f, "the control chamber (C06)", 120f,
                "Along the tunnels to the east, down to the source", L.ControlEntry,
                () => Seven.Place(L.ControlEntry, Vector3.right));
        }

        IEnumerator SayDescent(int n)
        {
            var ls = Lines("VEN_C05", 900 + n, 900 + n);
            if (ls.Count == 0) yield break;
            yield return Dlg.PlayLines(ls, false);
        }

        IEnumerator SlabPrompt()
        {
            while (!_slabTk)
            {
                var to = Flat(L.CutSlabRest - P7);
                bool near = Seven.Current == ChapterSeven.Mode.Free && to.magnitude < 6f && P7.y < L.CutBotY + 0.4f && (to.magnitude < 2.5f || Vector3.Angle(Flat(Cam.transform.forward), to) < 60f || Vector3.Angle(Flat(Seven.transform.forward), to) < 60f);
                _encPrompt = near ? "Telekinesis: the fallen slab" : null; _encBtn = ControlsLegend.Btn.Tk;
                if (near && !Auto && Input.Telekinesis.WasPressedThisFrame()) _slabTk = true;
                yield return null;
            }
            _encPrompt = null;
        }
        IEnumerator LiftCutSlab()
        {
            yield return Cutscene(true);
            Seven.Gesture(_cutSlab.position); TkAudit.Watch(this, "Venus: the slab step in the burrows", _cutSlab.position, _cutSlab);   // QF24 (14)
            Seven.SetEnergyPoint(1.8f, 0.25f);
            Shoot(new Vector3(77.0f, -12.3f, 117.7f), new Vector3(74.4f, -13.4f, 119.6f), 56f, 0.01f);
            for (float t = 0f; t < 2.5f && !Seven.GesturePeak; t += Time.deltaTime) yield return null;
            AudioKit.Play("slab_lift", _cutSlab.position, 0.8f, 0.05f, AudioKit.Bus.Sfx, 30f);
            var p0 = _cutSlab.position; var r0 = _cutSlab.rotation; var up = (p0 + L.CutSlabAt) * 0.5f + Vector3.up * 1.4f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.8f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                _cutSlab.position = u < 0.5f ? Vector3.Lerp(p0, up, u * 2f) : Vector3.Lerp(up, L.CutSlabAt, (u - 0.5f) * 2f);
                _cutSlab.rotation = Quaternion.Slerp(r0, Quaternion.identity, u);
                if (t > 0.45f && t - Time.deltaTime / 1.8f <= 0.45f) StartCoroutine(Shot("qf43_14d_the_slab_lifted"));
                yield return null;
            }
            _cutSlab.SetPositionAndRotation(L.CutSlabAt, Quaternion.identity);
            AudioKit.Play("stone_fall", L.CutSlabAt, 0.8f, 0.03f, AudioKit.Bus.Sfx, 40f);
            if (SlabDust != null) { SlabDust.transform.position = L.CutSlabAt; SlabDust.Emit(20); }
            Seven.SetEnergyPoint(1f, 1f);
            _slabPlaced = true; VenusState.CompleteScene("VEN_cut_slab");
            SetZone("CutSlab", true); DescentMoves();
            Log("DESCENT (QF43 14): the fallen slab lifted by telekinesis against the sill - a step (TK moved it)");
            yield return Wait(0.5f);
            yield return Shot("qf43_14e_the_slab_step");
            Seven.Release();
            FollowCamera();
            yield return Cutscene(false);
        }
        IEnumerator LeverPrompt()
        {
            while (!_leverHit)
            {
                bool near = Seven.Current == ChapterSeven.Mode.Free && Flat(L.GateRelease - P7).magnitude < 1.7f && Mathf.Abs(P7.y - L.PitY(L.GateRelease.z)) < 0.6f;
                _encPrompt = near ? "Pull the release" : null; _encBtn = ControlsLegend.Btn.Interact;
                if (near && !Auto && Input.Interact.WasPressedThisFrame()) _leverHit = true;
                yield return null;
            }
            _encPrompt = null;
        }
        IEnumerator PullRelease()
        {
            yield return Cutscene(true);
            Seven.Touch(_lever.position);
            for (float t = 0f; t < 1.2f && Seven.Current == ChapterSeven.Mode.Touch && Seven.Tn < 0.5f; t += Time.deltaTime) yield return null;
            AudioKit.Play("lever_pull", _lever.position, 0.9f, 0.05f, AudioKit.Bus.Sfx, 30f);
            var q0 = _lever.rotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f) { _lever.rotation = Quaternion.Slerp(q0, Quaternion.Euler(0f, 0f, -70f), t); yield return null; }
            Seven.Release();
            // up the pit: the gate at the top sinks open (the payoff - and the short way back up from now on)
            Shoot(new Vector3(69.2f, L.PitY(119.5f) + 1.6f, 119.5f), L.RootGateIn + Vector3.up * 1.2f, 46f, 0.01f);
            ImageGate.ExpectPoint(L.RootGateIn + Vector3.up * 1.2f, "the root gate sinking open at the top of the pit", 0f, "qf43_14f");
            _released = true;
            yield return OpenRootGate(true);
            SetZone("RootLanding", true); SetZone("RootPit", true);
            VenusState.CompleteScene("VEN_root_release");
            yield return Shot("qf43_14f_the_gate_opens_behind_her");
            yield return SayDescent(4);                                          // Good. Now you have a way back.
            Log($"DESCENT (QF43 14): the gate's inner release pulled - the gate at the top is open (the pit's stairs: the short way back up); the route took {Time.time - _descentT0:0} s");
            FollowCamera(); Cam.SnapBehind();
            yield return Cutscene(false);
        }
        float _descentT0;

        /// <summary>Check runs (precise) / player-style runs (human timing): the descent, step by step.</summary>
        IEnumerator DescentBot(bool human)
        {
            DescentBotActive = true; _descentT0 = Time.time;
            var s7 = Seven; s7.ScriptDriven = true; var rnd = new System.Random(human ? 43 : 7);
            float J() => human ? (float)(rnd.NextDouble() * 0.5 - 0.15) : 0f;
            IEnumerator Walk(Vector3 to, float tol = 0.3f, float max = 12f) { for (float t = 0f; t < max; t += Time.deltaTime) { var d = Flat(to - P7); if (d.magnitude < tol) break; s7.ScriptMove = s7.Current == ChapterSeven.Mode.Free ? d.normalized * Mathf.Clamp01(d.magnitude * 1.5f + 0.25f) : Vector3.zero; yield return null; } s7.ScriptMove = Vector3.zero; }
            IEnumerator Face(Vector3 dir) { for (float t = 0f; t < 0.4f; t += Time.deltaTime) { s7.transform.rotation = Quaternion.RotateTowards(s7.transform.rotation, Quaternion.LookRotation(dir), 400f * Time.deltaTime); yield return null; } }
            IEnumerator Late() { float l = J(); if (l > 0f) yield return new WaitForSeconds(l); }
            int retries = 0;
            while (_cut) yield return null;
            // 1. the edge -> the hang -> shimmy -> let go over the shelf
            for (int tries = 0; tries < 4 && !OnZone("CutShelf"); tries++)
            {
                yield return Walk(L.CutLipDown.Spot, 0.25f);
                yield return Face(Vector3.back);
                yield return Late(); s7.ScriptCrouch = true;
                for (float t = 0f; t < 3f && s7.Current != ChapterSeven.Mode.Hang; t += Time.deltaTime) yield return null;
                if (s7.Current != ChapterSeven.Mode.Hang) { retries++; continue; }
                float goal = L.CutLip.U(new Vector3(74.3f, 0f, 133.95f)) - (human ? 0.5f : 0f);
                for (float t = 0f; t < 10f && Mathf.Abs(s7.HangU - goal) > 0.12f && s7.Current == ChapterSeven.Mode.Hang; t += Time.deltaTime) { s7.ScriptMove = L.CutLip.Dir * Mathf.Sign(goal - s7.HangU); yield return null; }
                s7.ScriptMove = Vector3.zero;
                yield return Late(); s7.ScriptLetGo = true;
                for (float t = 0f; t < 3f && !OnZone("CutShelf") && !(s7.Current == ChapterSeven.Mode.Free && P7.y > -7f); t += Time.deltaTime) yield return null;
                if (!OnZone("CutShelf")) retries++;
            }
            Log($"DESCENT BOT (QF43 14) [{(human ? "human-timed" : "precise")}]: shelf {(OnZone("CutShelf") ? "reached" : "NOT reached")}");
            // 2. the chasm, between pulses
            for (int tries = 0; tries < 5 && !OnZone("CutLedge"); tries++)
            {
                yield return Walk(L.ChasmJump.From, 0.3f);
                yield return Face(Vector3.back);
                while (PulseActive) yield return null;                           // wait for the roots to still
                if (Time.time - _lastPulseT > PulsePeriod - 2.2f) { while (!PulseActive) yield return null; while (PulseActive) yield return null; }   // not just before the next one
                yield return Late(); s7.ScriptJump = true;
                for (float t = 0f; t < 3f && !OnZone("CutLedge"); t += Time.deltaTime) yield return null;
                if (!OnZone("CutLedge")) retries++;
            }
            Log($"DESCENT BOT (QF43 14): the chasm {(OnZone("CutLedge") ? "jumped between pulses" : "NOT crossed")}");
            // 3. the drop
            for (int tries = 0; tries < 4 && !OnZone("CutBottom"); tries++)
            {
                yield return Walk(L.CutLedgeDown.Spot, 0.25f);
                yield return Face(Vector3.back);
                yield return Late(); s7.ScriptCrouch = true;
                for (float t = 0f; t < 3f && s7.Current != ChapterSeven.Mode.Hang; t += Time.deltaTime) yield return null;
                yield return new WaitForSeconds(0.5f);
                yield return Late(); s7.ScriptLetGo = true;
                for (float t = 0f; t < 3f && !OnZone("CutBottom"); t += Time.deltaTime) yield return null;
                if (!OnZone("CutBottom")) retries++;
            }
            // 4. the slab
            yield return Walk(new Vector3(75.6f, 0f, 119f), 0.3f);
            yield return Face(Vector3.left);
            for (float t = 0f; t < 6f && !_slabPlaced; t += Time.deltaTime) { if (!_slabTk && t > 0.6f + Mathf.Max(0f, J())) _slabTk = true; yield return null; }
            while (_cut) yield return null;
            for (int tries = 0; tries < 4 && !OnZone("CutSlab") && !OnZone("RootSill"); tries++)
            {
                yield return Walk(L.CutSlabStep.From, 0.25f);
                yield return Face(Vector3.left);
                yield return Late(); s7.ScriptJump = true;
                for (float t = 0f; t < 3f && !OnZone("CutSlab"); t += Time.deltaTime) yield return null;
                if (!OnZone("CutSlab")) retries++;
            }
            // 5. into the pit, to the release
            yield return Walk(new Vector3(72.3f, 0f, 119f), 0.3f);
            yield return Walk(L.GateRelease + Vector3.right * 1.0f, 0.3f);
            yield return Face(Vector3.left);
            yield return Late(); _leverHit = true;
            while (!_released) yield return null;
            while (_cut) yield return null;
            s7.ScriptMove = Vector3.zero; s7.ScriptJump = s7.ScriptCrouch = s7.ScriptLetGo = false;
            if (!Auto) s7.ScriptDriven = false;
            DescentBotActive = false;
            Log($"DESCENT (QF43 14) [{(human ? "human-timed bot" : "precise bot")}]: done in {Time.time - _descentT0:0} s, retries {retries}, pulses during it {_pulsesFired}");
        }
    }
}
