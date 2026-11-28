using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF43 (Dan's playthrough list, part 3) - Venus:
    ///  (13) the pulse: one clock for the whole level - every ~9 s the control devices throb (a deep booming pulse felt more than
    ///       heard, ElevenLabs "venus_pulse"), each with a small tremor, a ripple of light from the devices, the burrowing creatures
    ///       surging the same way, the music ducked under it. From C04 (when the guardian names the trouble) to C07; each device
    ///       shut down makes it weaker. Check runs log PULSE (QF43 13).
    ///  (17) the old Elarim holding cell in the control chamber: a sunken ring with six old pylons, shown and named before the fight
    ///       (VEN_C06_00a-c); when he is beaten she lifts him into it with telekinesis (VEN_C06_P1, RT "Lift him into the cell");
    ///       the pylons raise a wall of light that rises and arcs over into a closed dome (no opening anywhere: logged as
    ///       CONTAINMENT (QF43 17) with the dome's open fraction = 0).
    /// </summary>
    public partial class VenusDirector
    {
        // ------------------------------------------------------------------------------------------ (13) the pulse
        public const float PulsePeriod = 9f;
        float _nextPulseAt = -1f; int _pulsesFired; bool _pulseLoop;
        Light _pulseGlow;
        float PulseStrength { get { int off = 0; if (Devices != null) foreach (var d in Devices) if (d != null && !d.On) off++; return Mathf.Clamp01(1f - 0.27f * off); } }

        /// <summary>The level's pulse clock (replaces the old 25 s ambient pulse). Runs from C04 to C07.</summary>
        IEnumerator AmbientPulses()
        {
            if (_pulseLoop) yield break;
            _pulseLoop = true; _pulsesOn = true;
            if (Devices != null) foreach (var d in Devices) if (d != null) { d.Driven = true; d.Period = 2.6f; }
            if (_nextPulseAt < 0f) _nextPulseAt = Time.time + 3f;
            while (_pulsesOn && !VenusState.AtLeast(VenusState.Stage.ThreatStopped))
            {
                if (Time.time >= _nextPulseAt && !PauseMenu.IsOpen) PulseNow();
                yield return null;
            }
            _pulseLoop = false;
            Log($"PULSE (QF43 13): stopped after {_pulsesFired} pulses (the threat is stopped)");
        }

        /// <summary>One pulse, now (the clock restarts from here): the sound, a small tremor, the devices' ring of light, the
        /// creatures surging, the music ducked.</summary>
        void PulseNow()
        {
            _nextPulseAt = Time.time + PulsePeriod;
            if (VenusState.AtLeast(VenusState.Stage.ThreatStopped)) return;
            float s = PulseStrength; _pulsesFired++;
            bool down = VenusLayout.Underground(P7) || Flat(P7 - VenusLayout.ObsSpot).magnitude < 40f || P7.y < -4f;
            float vol = (down ? 1.0f : 0.55f) * Mathf.Lerp(0.55f, 1f, s);
            AudioKit.Play("venus_pulse", null, vol, 0.02f, AudioKit.Bus.Sfx);
            MusicSystem.DuckFor(3.2f);
            if (!_cut || _pulseInCut) CameraShake.Quake(1.3f, (down ? 0.2f : 0.1f) * s);
            if (VesselWater != null) StartCoroutine(Ripple(VesselWater, 1.4f, 0.03f * s));
            if (Devices != null) foreach (var d in Devices) if (d != null && d.On) d.PulseNow(s);
            // the creatures surge the same way on every pulse (toward the temple's foundations), then mill until the next
            if (Worms != null) foreach (var w in Worms) if (w != null && !w.Calm) w.Drive(VenusLayout.Foundations, 3.2f);
            if (ChamberWorms != null) foreach (var w in ChamberWorms) if (w != null && w.gameObject.activeSelf && !w.Withdrawn && !w.Calm) w.Drive(w.transform.position + w.transform.forward * 6f, 1.6f);
            // a ripple of light from the source: at the root gate below the lookout (where it comes up from) - and in the chamber the devices
            StartCoroutine(PulseLight(down ? (VenusLayout.Underground(P7) ? VenusLayout.ControlCentre + Vector3.up * 3f : VenusLayout.RootGateIn + new Vector3(0f, 1.5f, -1.5f)) : P7 + Vector3.down * 3f, s));
            if (_pulsesFired <= 3 || _pulsesFired % 10 == 0) Log($"PULSE (QF43 13) #{_pulsesFired}: strength {s:0.00} ({(Devices != null ? System.Array.FindAll(Devices, x => x != null && !x.On).Length : 0)} devices off), heard {(down ? "near the source" : "from the gardens")} at {vol:0.00}, music ducked, tremor, creatures surge");
        }
        bool _pulseInCut;
        IEnumerator PulseLight(Vector3 at, float s)
        {
            if (_pulseGlow == null) { var go = new GameObject("QF43_PulseGlow"); _pulseGlow = go.AddComponent<Light>(); _pulseGlow.type = LightType.Point; _pulseGlow.color = Color.white; _pulseGlow.shadows = LightShadows.None; }
            _pulseGlow.transform.position = at; _pulseGlow.enabled = true;
            for (float t = 0f; t < 2.2f; t += Time.deltaTime) { float k = Mathf.Sin(Mathf.Clamp01(t / 2.2f) * Mathf.PI); _pulseGlow.intensity = 3.2f * s * k; _pulseGlow.range = 6f + 14f * (t / 2.2f); yield return null; }
            _pulseGlow.enabled = false;
        }
        static IEnumerator Ripple(Transform water, float seconds, float amp)
        {
            var w0 = water.localPosition;
            for (float t = 0f; t < seconds; t += Time.deltaTime) { float k = 1f - t / seconds; water.localPosition = w0 + new Vector3(Mathf.Sin(t * 29f), Mathf.Sin(t * 41f) * 0.4f, Mathf.Cos(t * 23f)) * amp * k; yield return null; }
            water.localPosition = w0;
        }
        /// <summary>Wait until the next pulse has fired (the clock is shared), up to max seconds.</summary>
        IEnumerator UntilPulse(float max = 12f)
        {
            int n = _pulsesFired;
            for (float t = 0f; t < max && _pulsesFired == n; t += Time.deltaTime) yield return null;
        }

        // ------------------------------------------------------------------------------------------ (17) the holding cell
        Transform _cell; Transform[] _cellPylons; Light[] _cellLights; MeshFilter _domeMf; MeshRenderer _domeMr; float _domeK = -1f;

        /// <summary>The old holding cell: built once (a sunken dark ring with a worn rim, six weathered pylons, their dead lamps,
        /// the dome mesh for the field of light). Its pylons are obstacles for her (she never walks through one).</summary>
        void EnsureCell()
        {
            if (_cell != null) return;
            _cell = new GameObject("QF43_HoldingCell").transform;
            var c = VenusLayout.CellAt;
            var stone = Markers.DarkGloss(); var rim = new Material(Shader.Find("Universal Render Pipeline/Lit")); rim.color = new Color(0.34f, 0.34f, 0.34f);
            // the sunken ring: a dark disc a little below the floor, a raised worn rim round it (a lip, not a wall)
            var v = new List<Vector3>(); var t = new List<int>();
            int N = 40; float R = VenusLayout.CellRadius;
            for (int i = 0; i < N; i++)
            {
                float a0 = i / (float)N * Mathf.PI * 2f, a1 = (i + 1) / (float)N * Mathf.PI * 2f;
                Vector3 D(float a, float r, float y) => c + new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                // floor of the ring (dark, 6 cm under the chamber floor) and its sloped inner face up to the rim
                Tri(v, t, c + Vector3.up * -0.06f, D(a1, R - 0.25f, -0.06f), D(a0, R - 0.25f, -0.06f));
                Quad(v, t, D(a0, R - 0.25f, -0.06f), D(a1, R - 0.25f, -0.06f), D(a1, R, 0.07f), D(a0, R, 0.07f));
                // the rim's top + outer bevel down into the floor
                Quad(v, t, D(a0, R, 0.07f), D(a1, R, 0.07f), D(a1, R + 0.35f, 0.07f), D(a0, R + 0.35f, 0.07f));
                Quad(v, t, D(a0, R + 0.35f, 0.07f), D(a1, R + 0.35f, 0.07f), D(a1, R + 0.5f, -0.02f), D(a0, R + 0.5f, -0.02f));
            }
            MakeMesh("CellRing", v, t, rim, _cell);
            // six old pylons: tapering grown-stone posts, each with a dead lamp crystal near the top
            _cellPylons = new Transform[6]; _cellLights = new Light[6];
            for (int k = 0; k < 6; k++)
            {
                float a = k / 6f * Mathf.PI * 2f + 0.26f;
                var p = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * VenusLayout.CellPylonR;
                var pv = new List<Vector3>(); var pt = new List<int>();
                int S = 6; float h = 2.6f;
                for (int i = 0; i < S; i++)
                {
                    float b0 = i / (float)S * Mathf.PI * 2f, b1 = (i + 1) / (float)S * Mathf.PI * 2f;
                    Vector3 Q(float b, float r, float y) => new Vector3(Mathf.Cos(b) * r, y, Mathf.Sin(b) * r);
                    Quad(pv, pt, Q(b0, 0.26f, -0.1f), Q(b0, 0.16f, h), Q(b1, 0.16f, h), Q(b1, 0.26f, -0.1f));
                    Tri(pv, pt, Q(0f, 0f, h + 0.18f), Q(b1, 0.16f, h), Q(b0, 0.16f, h));
                }
                var pg = MakeMesh("CellPylon" + k, pv, pt, stone, _cell); pg.transform.position = p; pg.transform.rotation = Quaternion.Euler(0f, k * 37f, (k % 2 == 0 ? 2.5f : -2f));
                var lamp = new GameObject("CellLamp" + k).AddComponent<Light>(); lamp.transform.SetParent(pg.transform, false); lamp.transform.localPosition = new Vector3(0f, h - 0.3f, 0f);
                lamp.type = LightType.Point; lamp.color = Color.white; lamp.range = 4f; lamp.intensity = 0f; lamp.shadows = LightShadows.None;
                _cellPylons[k] = pg.transform; _cellLights[k] = lamp;
                Ground.Obstacles.Add(new Ground.Circle(new Vector2(p.x, p.z), 0.45f));
            }
            // the field of light: a wall that rises from the ring, then arcs over into a dome (rebuilt for each k, see Dome)
            var dg = new GameObject("CellField"); dg.transform.SetParent(_cell, false);
            _domeMf = dg.AddComponent<MeshFilter>(); _domeMr = dg.AddComponent<MeshRenderer>(); _domeMr.sharedMaterial = Markers.Additive(0.16f);   // a faint field: he stays visible inside it
            _domeMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Dome(0f);
            Log($"holding cell (QF43 17): the old Elarim ring at {c} (radius {R:0.0} m), six pylons at {VenusLayout.CellPylonR:0.0} m - obstacles for her");
        }
        static GameObject MakeMesh(string name, List<Vector3> v, List<int> t, Material m, Transform parent)
        {
            var vv = new List<Vector3>(); var tt = new List<int>();
            for (int i = 0; i < t.Count; i++) { vv.Add(v[t[i]]); tt.Add(i); }            // flat-shaded, every triangle its own vertices
            var mesh = new Mesh { name = "QF43_" + name }; mesh.SetVertices(vv); mesh.SetTriangles(tt, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }
        static void Tri(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c) { int i = v.Count; v.Add(a); v.Add(b); v.Add(c); t.Add(i); t.Add(i + 1); t.Add(i + 2); }
        static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d) { Tri(v, t, a, b, c); Tri(v, t, a, c, d); }

        /// <summary>The field at k (0..1): 0..0.5 the wall rises to its full height; 0.5..1 the cap arcs over from the rim to the
        /// apex. At 1 it is closed on every side and over the top. Two-sided (seen from inside and out).</summary>
        void Dome(float k)
        {
            _domeK = k;
            var c = VenusLayout.CellAt; float R = VenusLayout.CellRadius + 0.15f, H = VenusLayout.CellWallH;
            float wall = Mathf.Clamp01(k / 0.5f) * H, cap = Mathf.Clamp01((k - 0.5f) / 0.5f);
            var v = new List<Vector3>(); var t = new List<int>();
            int N = 36, M = 10;
            for (int i = 0; i < N; i++)
            {
                float a0 = i / (float)N * Mathf.PI * 2f, a1 = (i + 1) / (float)N * Mathf.PI * 2f;
                Vector3 W(float a, float y) => c + new Vector3(Mathf.Cos(a) * R, y, Mathf.Sin(a) * R);
                if (wall > 0.01f) { Quad(v, t, W(a0, 0f), W(a0, wall), W(a1, wall), W(a1, 0f)); Quad(v, t, W(a1, 0f), W(a1, wall), W(a0, wall), W(a0, 0f)); }
                // the cap: latitude 0 (at the top of the wall) up to cap * 90 deg; at cap = 1 the last ring closes at the apex
                for (int j = 0; j < M; j++)
                {
                    float l0 = j / (float)M * Mathf.PI * 0.5f, l1 = (j + 1) / (float)M * Mathf.PI * 0.5f;
                    if (l0 >= cap * Mathf.PI * 0.5f - 1e-4f) break;
                    l1 = Mathf.Min(l1, cap * Mathf.PI * 0.5f);
                    Vector3 C(float a, float l) => c + new Vector3(Mathf.Cos(a) * R * Mathf.Cos(l), H + R * 0.9f * Mathf.Sin(l), Mathf.Sin(a) * R * Mathf.Cos(l));
                    Quad(v, t, C(a0, l0), C(a0, l1), C(a1, l1), C(a1, l0)); Quad(v, t, C(a1, l0), C(a1, l1), C(a0, l1), C(a0, l0));
                }
            }
            var mesh = _domeMf.sharedMesh; if (mesh == null) { mesh = new Mesh { name = "QF43_CellField" }; _domeMf.sharedMesh = mesh; }
            mesh.Clear(); mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            _domeMr.enabled = v.Count > 0;
        }
        /// <summary>The share of the field still open (0 = closed on every side and over the top).</summary>
        public float CellOpen => _domeK < 0f ? 1f : 1f - Mathf.Clamp01(_domeK);

        void CellLamps(float k) { if (_cellLights != null) foreach (var l in _cellLights) if (l != null) l.intensity = 2.2f * k; }

        /// <summary>The cell as the save says (reload / Continue / revisit): him inside, the dome closed - or empty, dark.</summary>
        void RestoreCell()
        {
            EnsureCell();
            bool shut = VenusState.WormmasterContained;
            Dome(shut ? 1f : 0f); CellLamps(shut ? 1f : 0f);
            if (shut && Wormmaster != null) { Wormmaster.transform.position = VenusLayout.CellAt; Wormmaster.transform.rotation = Quaternion.LookRotation(Vector3.left); Wormmaster.Current = WormmasterRig.Pose.Contained; }
            if (Barrier != null) Barrier.gameObject.SetActive(false);                // QF43 (17): the old barrier round his dais is gone
        }

        /// <summary>(17) C06's opening: as she enters, before he speaks, the camera finds the ring and its pylons; Panther names it.</summary>
        IEnumerator CellIntro()
        {
            EnsureCell();
            var c = VenusLayout.CellAt;
            var toCell = Flat(c - P7).normalized; var r = Vector3.Cross(Vector3.up, toCell);
            Shoot(P7 + Vector3.up * 2.4f - toCell * 2.2f + r * 1.6f, c + Vector3.up * 0.4f, 50f, 0.01f);
            ImageGate.ExpectPoint(c, "the old holding cell's ring and pylons", 0f, "qf43_17a");
            yield return Wait(0.8f);
            yield return Shot("qf43_17a_c06_the_holding_cell");
            yield return Say("VEN_C06", 0, 0, false);              // That ring in the floor... / Big enough for him. / Keep that in mind.
            Log("C06 (QF43 17): the holding cell shown and named before Wormmaster speaks (VEN_C06_00a-c)");
        }

        /// <summary>(17) He is beaten (pinned): Panther - "The holding cell. Get him into it."; RT lifts him (her telekinesis round
        /// him, his body hanging heavy, struggling feebly), carries him through the air into the ring and sets him down; the pylons
        /// light, the wall rises and arcs over into a closed dome; his claws scrape against it.</summary>
        IEnumerator LiftIntoCell()
        {
            EnsureCell();
            if (Wormmaster != null) Wormmaster.Current = WormmasterRig.Pose.Contained;
            yield return Cutscene(true);
            { var a = Wormmaster.transform.position; var side = Vector3.Cross(Vector3.up, Flat(a - P7).normalized);
              Shoot((a + P7) * 0.5f + side * 7f + Vector3.up * 3f, a + Vector3.up * 1.4f, 50f, 0.01f); }
            yield return Wait(0.6f);
            yield return Shot("enc_he_is_subdued");
            yield return Say("VEN_C06", 901, 901, false);          // VEN_C06_P1 "The holding cell. Get him into it."
            Seven.Release();
            yield return Cutscene(false);
            Objective("Lift him into the holding cell (telekinesis)");
            _liftHit = false;
            if (Auto) { yield return Wait(0.8f); _liftHit = true; }
            _encPrompt = null;
            StartCoroutine(LiftPrompt());
            yield return Until(() => _liftHit, "lift him into the holding cell (telekinesis)", 60f, "Look at the Wormmaster and use telekinesis (RT): the ring with the old pylons is the cell", Wormmaster.transform.position, () => _liftHit = true);
            _encPrompt = null;
            yield return Cutscene(true);
            Seven.Lock(true);
            var wm = Wormmaster.transform; var from = wm.position; var to = VenusLayout.CellAt;
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(from - P7)), 400f * Time.deltaTime); yield return null; }
            Seven.Gesture(from + Vector3.up * 1.6f); TkAudit.Watch(this, "Venus: Wormmaster lifted into the holding cell", from, wm);   // QF24 (14)
            Seven.SetEnergyPoint(2.0f, 0.25f);
            for (float t = 0f; t < 2.5f && !Seven.GesturePeak; t += Time.deltaTime) yield return null;
            AudioKit.Play("tk_push", from, 0.9f);
            // the shot: from the side, both of them and the ring
            { var mid = (from + to) * 0.5f; var side = Vector3.Cross(Vector3.up, Flat(to - from).normalized); if (Vector3.Dot(side, Flat(P7 - mid)) < 0f) side = -side;
              Shoot(mid + side * 11f + Vector3.up * 4.5f, mid + Vector3.up * 2.2f, 52f, 0.01f); }
            ImageGate.Expect(wm, "Wormmaster held in her telekinesis", 0f, "qf43_17b");
            var up = from + Vector3.up * 3.2f; var over = to + Vector3.up * 3.4f;
            bool shot = false; var tk = TkMotes;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 4.2f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                var p = u < 0.3f ? Vector3.Lerp(from, up, u / 0.3f) : Vector3.Lerp(up, over, (u - 0.3f) / 0.7f);
                wm.position = p + new Vector3(Mathf.Sin(t * 13f), Mathf.Sin(t * 17f) * 0.4f, Mathf.Cos(t * 11f)) * 0.08f;     // he hangs heavy, struggling feebly
                wm.rotation = Quaternion.LookRotation(Vector3.left) * Quaternion.Euler(Mathf.Sin(t * 9f) * 6f, 0f, Mathf.Sin(t * 7f) * 5f);
                if (tk != null && Time.frameCount % 2 == 0) { var hand = Seven.Bone(HumanBodyBones.RightHand); tk.transform.position = Vector3.Lerp(hand != null ? hand.position : P7 + Vector3.up * 1.3f, wm.position + Vector3.up * 1.5f, Random.value); tk.Emit(2); }
                if (!shot && t > 0.5f) { shot = true; StartCoroutine(Shot("qf43_17b_enc_lifted_through_the_air")); }
                yield return null;
            }
            for (float t = 0f, v = 0f; wm.position.y > to.y + 0.001f && t < 2f; t += Time.deltaTime) { v += 7f * Time.deltaTime; wm.position = Vector3.MoveTowards(wm.position, to, v * Time.deltaTime); yield return null; }
            wm.position = to; wm.rotation = Quaternion.LookRotation(Vector3.left);
            AudioKit.Play("land_hard", to, 0.8f); CameraShake.Quake(0.4f, 0.15f);
            Seven.SetEnergyPoint(1f, 1f);
            Log($"CONTAINMENT (QF43 17): she lifted him by telekinesis from {from:0.0} through the air into the ring at {to:0.0} - he never rose by himself");
            yield return CloseCell();
            Seven.Release();
        }
        bool _liftHit;
        IEnumerator LiftPrompt()
        {
            while (!_liftHit)
            {
                bool free = Seven.Current == ChapterSeven.Mode.Free;
                var to = Wormmaster != null ? Flat(Wormmaster.transform.position - P7) : Vector3.forward * 99f;
                bool near = free && to.magnitude < 12f;          // anywhere near him (the lift turns her to him first)
                _encPrompt = near ? "Telekinesis: lift him into the cell" : null; _encBtn = ControlsLegend.Btn.Tk;
                if (near && !Auto && Input.Telekinesis.WasPressedThisFrame()) _liftHit = true;
                yield return null;
            }
            _encPrompt = null;
        }

        /// <summary>(17) The pylons light one by one; the wall of light rises from the ring and arcs over into a closed dome.</summary>
        IEnumerator CloseCell()
        {
            var c = VenusLayout.CellAt;
            AudioKit.Play("barrier_close", c, 1f, 0f, AudioKit.Bus.Sfx, 80f);
            Shoot(c + new Vector3(-8.5f, 3.6f, 7.5f), c + Vector3.up * 2.0f, 52f, 0.01f);
            ImageGate.ExpectPoint(c + Vector3.up * 1.5f, "the holding cell's dome closing over him", 0f, "qf43_17c");
            for (int k = 0; k < 6; k++) { if (_cellLights[k] != null) _cellLights[k].intensity = 2.2f; AudioKit.Play("energy_point", _cellPylons[k].position + Vector3.up * 2.3f, 0.35f, 0.08f); yield return Wait(0.18f); }
            bool mid = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 3.4f)
            {
                Dome(Mathf.SmoothStep(0f, 1f, t));
                if (!mid && t > 0.55f) { mid = true; StartCoroutine(Shot("qf43_17c_enc_the_field_arcs_over")); }
                yield return null;
            }
            Dome(1f);
            VenusState.WormmasterContained = true;                 // saved: containment
            if (_humAt == null) _humAt = AudioKit.Loop("barrier_hum", null, c + Vector3.up * 2f, 0.5f, AudioKit.Bus.Sfx, 3f, 30f);
            // his claws scrape against it
            AudioKit.Play("stone_scrape", c + Vector3.up * 1.5f, 0.6f, 0.05f, AudioKit.Bus.Sfx, 40f);
            yield return Wait(0.8f);
            yield return Shot("qf43_17d_enc_the_cell_closed");
            Log($"CONTAINMENT (QF43 17): the field rose from the ring and arced over - closed on every side and over the top (open share {CellOpen:0.00}, must be 0); saved; devices [{string.Join(", ", VenusState.DevicesDisabled)}]; knockdowns during the encounter {Knockdowns}; creatures harmed 0");
        }
        AudioSource _humAt;

        // ------------------------------------------------------------------------------------------ (21/22) VEN_C12, re-staged
        // QF43 (21): Dan saw a blank wall, mostly on 7's lines. Why the check missed it: the old staging sent the guardian to the
        // ancient display panel on the east wall, she turned to it, and the coverage framing for her lines put the camera 1.7 m
        // from her TOWARD him - right at / behind that panel. The panel was built without a collider (Block(..., false)), so
        // CameraOccluders and the coverage ray never saw it: "speaker head in frame, clear" passed while the frame showed the
        // panel's back. Now the talk is staged at the cage (no wall in any framing), every framing is tested against the chamber's
        // walls (the room's bounds) AND physics, and the two places are projected over the cage.
        Transform _proj; Renderer _projMars, _projEuropa;
        static readonly Vector2 RoomMin = new Vector2(25.5f, 256f), RoomMax = new Vector2(34.5f, 273.5f);
        static bool InRoom(Vector3 p) => p.x > RoomMin.x && p.x < RoomMax.x && p.z > RoomMin.y && p.z < RoomMax.y && p.y > 0.3f && p.y < 9.5f;
        int _c12Frames, _c12Misses;

        void BuildProjection()
        {
            if (_proj != null) return;
            _proj = new GameObject("QF43_RecordProjection").transform;
            var glow = Markers.Additive(0.9f); var faint = Markers.Additive(0.35f); var dark = Markers.DarkGloss();
            var c = VenusLayout.CubeAt + Vector3.up * 1.75f;
            GameObject Ball(string n, Vector3 at, float r, Material m)
            {
                var v = new List<Vector3>(); var t = new List<int>(); int U = 24, V = 14;
                for (int j = 0; j < V; j++) for (int i = 0; i < U; i++)
                {
                    Vector3 P(int a, int b) { float th = a / (float)U * Mathf.PI * 2f, ph = b / (float)V * Mathf.PI - Mathf.PI * 0.5f; return new Vector3(Mathf.Cos(ph) * Mathf.Cos(th), Mathf.Sin(ph), Mathf.Cos(ph) * Mathf.Sin(th)) * r; }
                    Quad(v, t, P(i, j), P(i, j + 1), P(i + 1, j + 1), P(i + 1, j));
                }
                var g = MakeMesh(n, v, t, m, _proj); g.transform.position = at; return g;
            }
            void Band(Transform parent, float r, float tilt, float width, Material m, float a0 = 0f, float a1 = 360f)
            {
                var v = new List<Vector3>(); var t = new List<int>(); int N = 40; var q = Quaternion.Euler(tilt, 0f, 12f);
                for (int i = 0; i < N; i++)
                {
                    float x0 = Mathf.Lerp(a0, a1, i / (float)N) * Mathf.Deg2Rad, x1 = Mathf.Lerp(a0, a1, (i + 1) / (float)N) * Mathf.Deg2Rad; float rr = r * 1.012f;
                    Vector3 Pt(float a, float y) => q * new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr);
                    Quad(v, t, Pt(x0, -width), Pt(x0, width), Pt(x1, width), Pt(x1, -width)); Quad(v, t, Pt(x1, -width), Pt(x1, width), Pt(x0, width), Pt(x0, -width));
                }
                var g = MakeMesh("Band", v, t, m, parent); g.transform.localPosition = Vector3.zero;
            }
            var mars = Ball("Proj_Mars", c + new Vector3(-0.62f, 0.12f, 0f), 0.34f, glow);
            Band(mars.transform, 0.34f, 18f, 0.022f, dark, 20f, 200f);        // the great canyon across it
            var eur = Ball("Proj_Europa", c + new Vector3(0.62f, -0.05f, 0f), 0.24f, faint);
            Band(eur.transform, 0.24f, -35f, 0.006f, dark); Band(eur.transform, 0.24f, 52f, 0.006f, dark, 0f, 220f); Band(eur.transform, 0.24f, 8f, 0.006f, dark, 90f, 300f);   // the ice's cracks
            _projMars = mars.GetComponent<Renderer>(); _projEuropa = eur.GetComponent<Renderer>();
            ProjLevel(0f, 0f);
        }
        void ProjLevel(float mars, float europa)
        {
            if (_projMars != null) { _projMars.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, mars); foreach (var r in _projMars.GetComponentsInChildren<Renderer>()) r.enabled = mars > 0.01f; }
            if (_projEuropa != null) { _projEuropa.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, europa); foreach (var r in _projEuropa.GetComponentsInChildren<Renderer>()) r.enabled = europa > 0.01f; }
        }

        /// <summary>Frame the line on its speaker - never a wall: her face from in front (his side), his lines over her shoulder,
        /// Panther's (telepathic) on her; each framing must sit inside the chamber with a clear line to the speaker's head.</summary>
        void FrameC12(string speaker, Vector3? look = null)
        {
            var g = Guardian; if (g == null) return;
            var d = Dir7To(g); var r = Vector3.Cross(Vector3.up, d);
            var candidates = new List<(Vector3 cam, Vector3 at)>();
            if (speaker == "guardian")
            {
                var hg = HeadOf(g);
                foreach (float side in new[] { 1f, -1f }) candidates.Add((Head7 - d * 1.3f + r * 0.6f * side - Vector3.up * 0.3f, hg - Vector3.up * 0.25f));
                candidates.Add(((P7 + g.transform.position) * 0.5f + r * 5.2f + Vector3.up * 1.6f, (P7 + g.transform.position) * 0.5f + Vector3.up * 1.8f));
            }
            else
            {
                var h = Head7; var lk = look ?? h;
                foreach (float ang in new[] { 85f, 105f, 70f, 125f }) foreach (float side in new[] { 1f, -1f }) { var dir = Quaternion.AngleAxis(ang * side, Vector3.up) * d; candidates.Add((h + dir * 1.9f - Vector3.up * 0.05f, h - Vector3.up * 0.08f)); }
                candidates.Add(((P7 + g.transform.position) * 0.5f - r * 5.2f + Vector3.up * 1.6f, (P7 + g.transform.position) * 0.5f + Vector3.up * 1.6f));
            }
            var head = speaker == "guardian" ? HeadOf(g) : Head7;
            var other = speaker == "guardian" ? (Vector3?)null : g.transform.position + Vector3.up * 1.8f;
            foreach (var (cam, at) in candidates)
            {
                if (!InRoom(cam) || !ChapterCamera.ClearLine(cam, head) || Physics.Linecast(cam, head - (head - cam).normalized * 0.4f, ~0, QueryTriggerInteraction.Ignore)) continue;
                // QF43 (21): never a big body between the camera and her (the guardian is 3.5 m): he must be off her line of view
                if (other.HasValue && Vector3.Angle(head - cam, other.Value - cam) < 22f && (other.Value - cam).magnitude < (head - cam).magnitude + 1f) continue;
                Shoot(cam, at, 44f, 0.01f); return;
            }
            Shoot(candidates[candidates.Count - 1].cam, candidates[candidates.Count - 1].at, 46f, 0.01f);
        }
        IEnumerator SayC12(int from, int to, Vector3? look = null)
        {
            foreach (var line in Lines("VEN_C12", from, to))
            {
                FrameC12(line.Speaker == "guardian" ? "guardian" : "seven", look);
                yield return null;
                var cam = Cam.GetComponent<Camera>();
                var head = line.Speaker == "guardian" && Guardian != null ? HeadOf(Guardian) : Head7;
                var v = cam.WorldToViewportPoint(head);
                bool inFrame = v.z > 0f && v.x > 0.04f && v.x < 0.96f && v.y > 0.04f && v.y < 0.98f;
                bool clear = ChapterCamera.ClearLine(cam.transform.position, head) && InRoom(cam.transform.position);
                _c12Frames++; if (!inFrame || !clear) _c12Misses++;
                Log($"C12 FRAMING (QF43 21) {line.Id} ({line.Speaker}): {(line.Speaker == "cat" ? "telepathic - framed on her" : "the speaker")} head at ({v.x:0.00}, {v.y:0.00}) {(inFrame ? "in frame" : "OUT OF FRAME")}, {(clear ? "clear, camera inside the chamber" : "BLOCKED / outside the chamber")}");
                if (Shots) StartCoroutine(ShotLater($"qf43_21_{line.Id.Substring(4)}_{line.Speaker}", 0.6f));
                yield return Dlg.PlayLines(new List<VoiceBank.Line> { line }, false);
            }
        }
        IEnumerator ShotLater(string n, float s) { yield return Wait(s); yield return Shot(n); }

        IEnumerator C12()
        {
            yield return Cutscene(true);
            _coverage = false;
            Seven.Lock(true);
            var ik = Seven.gameObject.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>();
            ik.enabled = true; ik.Alive = true;
            if (Vector3.Distance(P7, VenusLayout.CubeView) > 3f) Seven.Place(VenusLayout.CubeView, Vector3.forward);
            if (Guardian != null) { Guardian.Stop(); Guardian.transform.position = VenusLayout.CubeView + new Vector3(1.8f, 0f, 2f); Guardian.Face(P7); }
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { if (Guardian != null) Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(Flat(Guardian.transform.position - P7)), 300f * Time.deltaTime); yield return null; }
            BuildProjection();
            yield return Wait(0.5f);
            yield return SayC12(1, 3);
            // the guardian raises a hand: the temple's record projects two glowing places over the open cage
            if (Guardian != null) Guardian.Play(Guardian.HasState("point") ? "point" : "lift");
            AudioKit.Play("energy_point", VenusLayout.CubeAt + Vector3.up * 1.8f, 0.8f);
            var projAt = VenusLayout.CubeAt + Vector3.up * 1.75f;
            { var dd = Flat(projAt - P7).normalized; var rr = Vector3.Cross(Vector3.up, dd); Shoot(Head7 - dd * 1.5f + rr * 0.7f + Vector3.up * 0.15f, projAt, 46f, 0.01f); }
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f) { ProjLevel(Mathf.SmoothStep(0f, 0.7f, t), Mathf.SmoothStep(0f, 0.7f, t)); yield return null; }
            ImageGate.ExpectPoint(projAt, "the two places projected over the cage", 0f, "qf43_21a");
            yield return Shot("qf43_21a_c12_the_projection_appears");
            // "Two locations survive in our records. Mars - and Europa." - over her shoulder on the projection; each lights as named
            var l4 = Lines("VEN_C12", 4, 4);
            StartCoroutine(LightAsNamed());
            yield return Dlg.PlayLines(l4, false);
            yield return Shot("qf43_21b_c12_mars_and_europa_named");
            ik.LookPos = _projMars != null ? _projMars.transform.position : projAt; ik.LookW = 0.85f;
            ProjLevel(1f, 0.6f);
            yield return SayC12(5, 8, _projMars != null ? _projMars.transform.position : projAt);
            ProjLevel(0.6f, 1f);
            ik.LookPos = _projEuropa != null ? _projEuropa.transform.position : projAt;
            yield return SayC12(9, 11, _projEuropa != null ? _projEuropa.transform.position : projAt);
            yield return Shot("c12_03_europa");
            ProjLevel(0.8f, 0.8f);
            yield return Wait(0.6f);
            ik.LookPos = Guardian != null ? HeadOf(Guardian) : projAt;
            yield return SayC12(12, 16);
            AudioKit.Play("console_select", projAt, 0.9f);
            for (float t = 0f; t < 0.8f; t += Time.deltaTime) { float k = 0.75f + Mathf.Sin(t * 14f) * 0.25f; ProjLevel(k, k); yield return null; }
            yield return SayC12(17, 20);
            yield return Shot("c12_04_thank_you");
            ProjLevel(0f, 0f);
            if (Guardian != null) Guardian.Play(Guardian.PostClip);
            ik.LookW = 0f;
            Log($"C12 (QF43 21): staged at the cage, the two places projected over it and lit as named; {_c12Frames} lines framed, {_c12Misses} with the speaker out of frame / blocked / the camera outside the chamber (must be 0)");
            Complete("VEN_C12");
            Log("C12 (0m): Mars and Europa sent to the ship - available on the star map from now on");
            // QF43 (22): no walk out of the temple - a fade, and she is on her bike in the temple court, ready to ride to the saucer
            yield return UI.Fade(1f, 0.9f);
            PlaceRiding(VenusLayout.ParkSpot, 180f);
            VenusState.SaveBike(VenusLayout.ParkSpot, 180f, true);
            FollowCamera(); Cam.SnapBehind();
            yield return Wait(0.3f);
            yield return UI.Fade(0f, 0.8f);
            Log($"BACK AT THE BIKE (QF43 22): after the talk she is on her bike in the temple court ({(Bike.Riding ? "mounted" : "NOT mounted")}), no walk out of the temple");
            yield return Shot("qf43_22_back_on_her_bike_in_the_court");
            yield return Cutscene(false);
        }
        IEnumerator LightAsNamed()
        {
            // "Two locations survive in our records. Mars-" ~1.9 s, "and Europa." ~2.8 s
            yield return Wait(1.9f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f) { ProjLevel(Mathf.Lerp(0.7f, 1f, t), 0.7f); yield return null; }
            yield return Wait(0.6f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f) { ProjLevel(1f, Mathf.Lerp(0.7f, 1f, t)); yield return null; }
        }
    }
}
