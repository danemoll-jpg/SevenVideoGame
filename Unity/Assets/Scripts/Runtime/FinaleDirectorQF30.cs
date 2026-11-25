using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using L = Seven.FinaleLayout;

namespace Seven
{
    /// <summary>QF30 — Dan's review of the ending: the shared pieces of the finale's fixes (the highlighted objectives, the mining drills
    /// boring into the old structure, Tunneler's rig at rest / in the barrier, his talking drill, the entrance breakthrough).</summary>
    public partial class FinaleDirector
    {
        [Header("QF30")]
        public TunnelerRig Rig; public Transform[] WallDrills; public ScreeGuard[] Crew;
        public Renderer BarrierFrameLines, BarrierLines, ConduitLines, ConduitNode, PowerCable, BarrierCable; public Transform ConduitConsoleT;

        // ------------------------------------------------------------------------------------------ item 9: where to go, clearly marked
        class Beacon { public string What; public Vector3 At; public Renderer[] Glow; public GameObject Column; public Renderer Cable; public bool Done; public float NextSpark; }
        readonly List<Beacon> _beacons = new List<Beacon>();
        /// <summary>The place to go: its control glows / pulses, a soft light column over it visible across the area, the dead cable leading
        /// to it sparks, the objective marker on it. <see cref="BeaconDone"/> turns it to the "done" state.</summary>
        Beacon Highlight(string what, Vector3 at, Renderer cable, params Renderer[] glow)
        {
            var b = new Beacon { What = what, At = at, Cable = cable, Glow = glow };
            var col = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(col.GetComponent<Collider>()); col.name = "BeaconBeam_" + what.Replace(" ", "_");
            col.transform.position = at + Vector3.up * 7f; col.transform.localScale = new Vector3(0.9f, 7f, 0.9f);
            var r = col.GetComponent<Renderer>(); r.sharedMaterial = Markers.Additive(0.32f); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            b.Column = col; _beacons.Add(b);
            if (cable != null) Glow(cable, 0f);
            ShowGuide(at);
            Log($"OBJECTIVE PLACE (QF30 9): '{what}' highlighted - its control pulses, a light column over it, {(cable != null ? "the dead cable leading to it sparks, " : "")}the marker on it");
            return b;
        }
        // ---- QF34 (3): a light column never stands between the player's camera and a lit vision screen - while one would, it steps back
        // to almost nothing (the place stays marked by its pulsing control, the guide light and the marker); the moment it is clear, back.
        public static int ColumnsStepped;
        void BeaconsClearScreens(Vector3 eye)
        {
            if (ProjectionWalls == null) return;
            foreach (var b in _beacons)
            {
                if (b.Column == null) continue; var r = b.Column.GetComponent<Renderer>(); if (r == null) continue;
                bool inWay = false; var axis = new Vector2(b.Column.transform.position.x, b.Column.transform.position.z); float rad = b.Column.transform.localScale.x * 0.5f + 0.35f;
                foreach (var w in ProjectionWalls)
                {
                    if (w == null || !w.enabled) continue;
                    foreach (var c in new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), Vector3.zero })
                    {
                        var p = w.transform.TransformPoint(c); var a2 = new Vector2(eye.x, eye.z); var b2 = new Vector2(p.x, p.z); var ab = b2 - a2; float L2 = ab.sqrMagnitude; if (L2 < 1e-4f) continue;
                        float t = Mathf.Clamp01(Vector2.Dot(axis - a2, ab) / L2); if (t <= 0.02f || t >= 0.98f) continue;
                        if ((a2 + ab * t - axis).magnitude < rad) { inWay = true; break; }
                    }
                    if (inWay) break;
                }
                var m = r.material; float want = inWay ? 0.02f : 0.32f; float cur = m.HasProperty("_Intensity") ? m.GetFloat("_Intensity") : want;
                if (inWay && cur > 0.1f) ColumnsStepped++;
                if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", inWay ? want : Mathf.MoveTowards(cur, want, Time.deltaTime * 0.6f));   // out at once, back softly
            }
        }

        void BeaconDone(Beacon b, float doneGlow = 1.3f)
        {
            if (b == null || b.Done) return;
            b.Done = true; if (b.Column != null) Destroy(b.Column);
            if (b.Glow != null) foreach (var g in b.Glow) Glow(g, doneGlow);
            if (b.Cable != null) StartCoroutine(CableRuns(b.Cable));
            _beacons.Remove(b);
            Log($"OBJECTIVE DONE (QF30 9): '{b.What}' - the column goes out, the control lit steady{(b.Cable != null ? ", light runs along its cable" : "")}");
        }
        IEnumerator CableRuns(Renderer cable) { for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f) { Glow(cable, Mathf.Lerp(0f, 1.2f, t) * (0.8f + 0.2f * Mathf.Sin(t * 30f))); yield return null; } Glow(cable, 1.0f); }
        void TickBeacons()
        {
            foreach (var b in _beacons)
            {
                float p = 0.9f + 0.6f * Mathf.Sin(Time.time * 3.4f);
                if (b.Glow != null) foreach (var g in b.Glow) Glow(g, p);
                if (b.Column != null) b.Column.GetComponent<Renderer>().sharedMaterial.SetFloat("_Intensity", 0.24f + 0.1f * Mathf.Sin(Time.time * 2.2f));
                if (b.Cable != null && Time.time > b.NextSpark) { b.NextSpark = Time.time + Random.Range(0.9f, 1.8f); Sparks(b.At + Vector3.up * 0.25f, 14); }
            }
        }

        // ------------------------------------------------------------------------------------------ item 6: the mining drills boring in
        class WallDrill { public Transform Bit; public ParticleSystem Dust; public AudioSource Rumble; public float Angle; }
        readonly List<WallDrill> _wall = new List<WallDrill>();
        float _wallRate = 220f, _nextDebris;
        void SetupWallDrills()
        {
            if (WallDrills == null) return;
            foreach (var b in WallDrills)
            {
                if (b == null) continue;
                var w = new WallDrill { Bit = b };
                var go = new GameObject("DrillDust"); go.transform.SetParent(b.parent, false); go.transform.position = b.position + b.forward * 0.3f; go.transform.rotation = Quaternion.LookRotation(b.forward);
                var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var m = ps.main; m.loop = true; m.startLifetime = 2.6f; m.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 2.2f); m.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.6f); m.startColor = new Color(0.6f, 0.6f, 0.6f, 0.25f); m.maxParticles = 300; m.gravityModifier = 0.05f; m.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = ps.emission; em.rateOverTime = 30f;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 35f; sh.radius = 0.9f;
                var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.3f, 0.15f), new GradientAlphaKey(0f, 1f) }); col.color = g;
                go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Markers.AdditiveMatTex(0.3f);
                ps.Play(); w.Dust = ps;
                w.Rumble = AudioKit.Loop("drill_grind_rock", b, null, 0f, AudioKit.Bus.Sfx, 3f, 40f);
                _wall.Add(w);
            }
            Log($"QF30 (6): {_wall.Count} mining drills boring into the old structure (round the chamber and along the service route) - spinning, dust, sparks, a grinding rumble; they stop when the Scree reach the controls (FIN_07)");
        }
        void TickWallDrills()
        {
            if (_endingAudio) return;
            float dt = Time.deltaTime;
            _wallRate = Mathf.MoveTowards(_wallRate, _drillsOn ? 220f : 0f, (_drillsOn ? 120f : 30f) * dt);   // a long spin-down (~7 s)
            float k = _wallRate / 220f;
            foreach (var w in _wall)
            {
                if (w.Bit == null) continue;
                w.Angle += _wallRate * dt; if (w.Bit.name != "DrillCrack") w.Bit.localRotation = Quaternion.AngleAxis(w.Angle % 360f, Vector3.forward);   // QF34 (5): a crack never spins
                if (w.Dust != null) { var em = w.Dust.emission; em.rateOverTime = 30f * k; }
                if (w.Rumble != null) { w.Rumble.volume = 0.45f * k * AudioKit.Gain(AudioKit.Bus.Sfx); w.Rumble.pitch = 0.75f + 0.25f * k; }
                if (k > 0.5f && Random.value < dt * 0.8f) Sparks(w.Bit.position + w.Bit.forward * 0.4f + Random.insideUnitSphere * 0.8f, 16);
            }
            // the grinding shakes debris loose near her (and the floor trembles a little)
            if (k > 0.6f && Time.time > _nextDebris && Seven != null)
            {
                _nextDebris = Time.time + Random.Range(5f, 9f);
                WallDrill near = null; float best = 18f;
                foreach (var w in _wall) if (w.Bit != null) { float d = Vector3.Distance(w.Bit.position, P7); if (d < best) { best = d; near = w; } }
                if (near != null) { StartCoroutine(FallingStone(near.Bit.position + near.Bit.forward * 0.6f)); if (best < 12f && !_cut) CameraShake.Quake(0.12f, 0.5f); }
            }
        }
        IEnumerator FallingStone(Vector3 from)
        {
            var go = new GameObject("FallenStoneChip"); go.AddComponent<MeshFilter>().sharedMesh = Markers.Rock(Random.Range(0, 999), new Vector3(0.16f, 0.12f, 0.14f)); go.AddComponent<MeshRenderer>().sharedMaterial = RuneMat();
            go.transform.position = from; var v = Random.insideUnitSphere * 1.2f; v.y = 0.5f;
            float floor = Ground.Height(from.x, from.z);
            for (float t = 0f; t < 4f && go.transform.position.y > floor + 0.06f; t += Time.deltaTime) { v.y -= 9.8f * Time.deltaTime; go.transform.position += v * Time.deltaTime; go.transform.Rotate(200f * Time.deltaTime, 90f * Time.deltaTime, 0f); yield return null; }
            AudioKit.Play("stone_fall", go.transform.position, 0.35f);
            Destroy(go, 6f);
        }
        void StopWorldDrillsNow() { _drillsOn = false; _wallRate = 0f; foreach (var w in _wall) { if (w.Rumble != null) w.Rumble.Stop(); if (w.Dust != null) w.Dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); } }

        // ------------------------------------------------------------------------------------------ Tunneler's rig and drill, outside the fight
        DrillHead _dh; bool _rigUp = true; float _sag;
        /// <summary>The craft's children move into one hover node (the fight's Body) once, so the rig hovers at his entrance, in the fight
        /// and sags onto the floor in the barrier.</summary>
        void EnsureHover()
        {
            if (_hover != null || TunnelerCraft == null) return;
            _hover = new GameObject("Hover").transform; _hover.SetParent(TunnelerCraft, false);
            var kids = new List<Transform>(); foreach (Transform c in TunnelerCraft) if (c != _hover) kids.Add(c);
            foreach (var c in kids) c.SetParent(_hover, false);
            _hover.localPosition = Vector3.up * 0.6f;
        }
        void TickRig()
        {
            if (TunnelerCraft == null || !TunnelerCraft.gameObject.activeInHierarchy || _hover == null) return;
            bool inFight = _fight != null && _fight.enabled && !_fight.Over;
            if (!inFight)
            {
                float h = _rigUp ? 0.6f + Mathf.Sin(Time.time * 1.6f) * 0.06f : 0.16f;
                _hover.localPosition = Vector3.MoveTowards(_hover.localPosition, Vector3.up * h, Time.deltaTime * (_rigUp ? 1.5f : 0.6f));
                _hover.localRotation = Quaternion.RotateTowards(_hover.localRotation, Quaternion.Euler(_sag, 0f, 0f), Time.deltaTime * 10f);
            }
            if (_dh != null) _dh.Want = Mathf.Max(0f, DrillSpeed);
            if (Rig != null && !inFight) Rig.ArmDrillWant = _rigUp ? Mathf.Clamp(DrillSpeed, 0f, 600f) : 0f;
        }
        /// <summary>His voice drives the drill (louder syllables = faster), as on Europa.</summary>
        IEnumerator TunnelerTalks(VoiceBank.Line l)
        {
            if (_dh == null) yield break;
            var src = VoiceSource(); var buf = new float[256]; float t0 = Time.time;
            while (Dlg.Current == l && Time.time - t0 < 12f)
            {
                float lvl = 0f;
                if (src != null && src.isPlaying) { src.GetOutputData(buf, 0); foreach (var v in buf) lvl = Mathf.Max(lvl, Mathf.Abs(v)); }
                if (lvl <= 0.0001f && (src == null || !src.isPlaying || DemoBoot.Muted)) lvl = 0.25f + 0.2f * Mathf.Abs(Mathf.Sin((Time.time - t0) * 7.3f)) * Mathf.Abs(Mathf.Sin((Time.time - t0) * 2.1f));
                _dh.Talk = Mathf.Clamp01(lvl * 2.2f) + 0.001f;
                yield return null;
            }
            _dh.Talk = 0f;
        }

        // ------------------------------------------------------------------------------------------ item 6: the entrance - a drill machine breaking through
        readonly List<Transform> _plug = new List<Transform>();
        void BuildPlug()
        {
            if (_plug.Count > 0) return;
            for (int i = 0; i < 16; i++)
            {
                // QF31 (8): smaller stones (rc2's 0.9 m blocks, flung far in, carpeted the fight's floor)
                var go = new GameObject("AccessRubble" + i); go.AddComponent<MeshFilter>().sharedMesh = Markers.Rock(300 + i, new Vector3(0.6f, 0.46f, 0.52f) * Random.Range(0.75f, 1.1f)); go.AddComponent<MeshRenderer>().sharedMaterial = RockMat();
                go.transform.position = L.MiningAccess + new Vector3(-2.4f + (i % 4) * 1.6f, 0.5f + (i / 4) * 1.15f, -0.6f + Random.Range(-0.2f, 0.2f)); go.transform.rotation = Random.rotation;
                _plug.Add(go.transform);
            }
        }
        Material _rockMat;
        Material RockMat() { if (_rockMat != null) return _rockMat; _rockMat = new Material(Shader.Find("Seven/DissolveLit") ?? Shader.Find("Universal Render Pipeline/Lit")) { name = "Fin_debris" }; if (_rockMat.HasProperty("_BaseColor")) _rockMat.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.3f)); return _rockMat; }
        /// <summary>The rubble bursts into the chamber ahead of the rig and comes to rest on the floor (loose: thrown in the fight).</summary>
        IEnumerator BurstPlug()
        {
            var vel = new List<Vector3>();
            foreach (var p in _plug) vel.Add(new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(1.2f, 3f), Random.Range(1.8f, 4.5f)));   // QF31 (8): they tumble in near the breach, not across the floor
            for (float t = 0f; t < 1.6f; t += Time.deltaTime)
            {
                for (int i = 0; i < _plug.Count; i++)
                {
                    var p = _plug[i]; if (p == null) continue;
                    var v = vel[i]; v.y -= 9.8f * Time.deltaTime; var np = p.position + v * Time.deltaTime;
                    float fl = L.Y + 0.35f; if (np.y < fl) { np.y = fl; v *= 0.35f; v.y = Mathf.Abs(v.y) * 0.3f; }
                    if (L.Walkable(np, 0f) == false && Flat(np - L.ChamberC).magnitude > L.ChamberR - 1.5f) { np = p.position; v = Vector3.zero; }
                    if (Flat(np - L.PlatformAt).magnitude < L.PlatformR + 0.6f) { np = p.position; v = Vector3.zero; }
                    p.position = np; vel[i] = v; p.Rotate(v.magnitude * 40f * Time.deltaTime, v.magnitude * 25f * Time.deltaTime, 0f);
                }
                yield return null;
            }
        }
    }
}
