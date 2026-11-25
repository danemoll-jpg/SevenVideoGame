using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Seven
{
    /// <summary>
    /// Round 20: the Grand Circuit scene (Docs/RACING_MODE.md, Stage 1). The builder places the two worlds' sets (Cattalus,
    /// the Moon), the vehicle templates (7 on her bike, Duchess in her racer #8, Tunneler on his drill rig), the materials and the
    /// atmospheres; this builds the chosen track's ribbon at load, runs the setup screens (<see cref="GcMenu"/>), the race (grid,
    /// lights, 3 laps, pickups, positions, the finish) and the time trial (best times + a ghost), with 1-2 players in split screen.
    /// </summary>
    public class GcGame : MonoBehaviour
    {
        public const string SceneName = "GrandCircuit";
        // ---- set by the builder
        public GameObject BikeTpl, RacerTpl, RigTpl, SevenTpl, DuchessTpl;
        public Material Road, Under, Wall, Glow, GlowSoft, Pad, Frame, Trail, Shield, Wave, PickupMat, GhostMat; [System.NonSerialized] public Material HoverMat;
        /// <summary>Round 21: one world set + atmosphere + lowest point per track, in <see cref="GcTrack.Ids"/> order (one shown at a time).</summary>
        public GameObject[] Sets = new GameObject[0], Atmos = new GameObject[0]; public float[] Lowest = new float[0];
        public Camera CamTemplate;

        // ---- the race
        public enum Mode { Race, TimeTrial }
        public static Mode NextMode = Mode.Race; public static string NextTrack = "cattalus"; public static int[] NextRacers = { 0 }; public static int[] NextPads = { -1 };
        public static bool SideBySide;
        public static bool StartDirect;
        /// <summary>Round 21 check runs: a shorter race (the players run).</summary>
        public static int LapsOverride;                              // the menu chose a race: start it on load
        public GcTrack Track; public readonly List<GcRacer> Racers = new List<GcRacer>();
        public readonly List<GcRacer> Humans = new List<GcRacer>();
        public int Laps = 3; public bool Running, Over; public float RaceT0;
        public List<GcCam> Cams = new List<GcCam>(); public List<GcHud> Huds = new List<GcHud>();
        public System.Action<string> Moment;
        Transform _trackRoot; GcGhost _ghost; GcGhostRecorder _rec; readonly List<(int i, Vector3 p, Quaternion r, float x)> _pads = new List<(int, Vector3, Quaternion, float)>();
        readonly Dictionary<GcRacer, float> _padCool = new Dictionary<GcRacer, float>();
        public static GcGame I { get; private set; }
        public GcMenu Menu;
        public int Countdown = -1;
        public bool Paused;
        public float FrameMsSum; public int FrameN; public List<float> FrameMs = new List<float>();

        void Awake()
        {
            I = this;
            // the see-through whites (built at run time: the trail, the shield bubble, the push ring, the ghost) - additive, monochrome
            if (Trail == null) Trail = Markers.Additive(0.55f); if (Shield == null) Shield = Markers.Additive(0.14f);
            if (Wave == null) Wave = Markers.Additive(0.85f); if (GhostMat == null) GhostMat = Markers.Additive(0.3f);
            HoverMat = Markers.AdditiveMatTex(0.3f);
        }
        void Start()
        {
            GcSave.Load();
            if (CamTemplate != null) CamTemplate.gameObject.SetActive(false);
            MusicSystem.EndSilence();
            Menu = gameObject.AddComponent<GcMenu>(); Menu.Game = this;
            if (GcBoot.Any) { gameObject.AddComponent<GcCaptureRun>().Game = this; return; }
            if (StartDirect) { StartDirect = false; StartCoroutine(RaceRun()); return; }
            Menu.Open();
        }

        public void ShowSet(string track)
        {
            int k = GcTrack.IndexOf(track);
            for (int i = 0; i < Sets.Length; i++) if (Sets[i] != null) Sets[i].SetActive(i == k);
            // (the ones going away first: an atmosphere switched off after the new one came on could clear the shared fog settings)
            for (int i = 0; i < Atmos.Length; i++) if (i != k && Atmos[i] != null) Atmos[i].SetActive(false);
            if (k < Atmos.Length && Atmos[k] != null) Atmos[k].SetActive(true);
            GcRacer.LowestY = k < Lowest.Length ? Lowest[k] : -60f;
        }

        // ------------------------------------------------------------------------------------------ setup
        public void BuildTrack(string id)
        {
            if (_trackRoot != null) Destroy(_trackRoot.gameObject);
            Track = GcTrack.ById(id); ShowSet(id);
            _trackRoot = new GameObject("Track_" + id).transform; _trackRoot.SetParent(transform, false);
            Track.BuildMeshes(_trackRoot, Road, Under, Wall, Glow, GlowSoft, Pad);
            Track.BuildGates(_trackRoot, Frame, Glow);
            // pickup pads: three across the track at the start of each pickup piece
            _pads.Clear();
            var padRoot = new GameObject("PickupPads").transform; padRoot.SetParent(_trackRoot, false);
            for (int i = 0; i < Track.Count; i++)
            {
                if ((Track.Flag[i] & GcTrack.F.Pickup) == 0) continue;
                if (i > 0 && (Track.Flag[i - 1] & GcTrack.F.Pickup) != 0) continue;
                int k = Mathf.Min(i + 6, Track.Count - 1);
                foreach (var x in new[] { -4f, 0f, 4f })
                {
                    var p = Track.P[k] + Track.R[k] * new Vector3(x, 0.05f, 0f);
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(go.GetComponent<Collider>()); go.name = "PickupPad"; go.transform.SetParent(padRoot, false);
                    go.transform.SetPositionAndRotation(p, Track.R[k] * Quaternion.Euler(0f, 45f, 0f)); go.transform.localScale = new Vector3(1.7f, 0.06f, 1.7f);
                    go.GetComponent<Renderer>().sharedMaterial = PickupMat;
                    var cr = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(cr.GetComponent<Collider>()); cr.name = "PadGem"; cr.transform.SetParent(go.transform, false); cr.transform.localPosition = new Vector3(0f, 12f, 0f); cr.transform.localScale = new Vector3(0.35f, 8f, 0.35f); cr.GetComponent<Renderer>().sharedMaterial = Glow;
                    go.AddComponent<GcSpin>();
                    _pads.Add((k, p, Track.R[k], x));
                }
            }
            BuildPresses();
            Log($"GC TRACK (R20): {Track.Name} - {Track.Length:0} m a lap, closing error {Track.CloseError:0.0} m spread over the flat parts, {_pads.Count} pickup pads, {(Track.Branch != null ? $"the shortcut {Track.Branch.Length:0} m (s {Track.Branch.JoinFrom:0} -> {Track.Branch.JoinTo:0}, saves {Track.Ahead(Track.Branch.JoinFrom, Track.Branch.JoinTo) - Track.Branch.Length:0} m)" : "no shortcut")}");
        }

        /// <summary>Round 21 (Dan: no duplicate racers): the field is ONE of each racer - the humans' choices first (a racer already taken
        /// goes to the next free one), the AI fills the rest; on a staggered grid just behind the line.</summary>
        public void SpawnField(int[] humanRacers, int[] pads, int fieldSize, bool ghosts = false)
        {
            foreach (var r in Racers) if (r != null) Destroy(r.gameObject);
            Racers.Clear(); Humans.Clear();
            int n = GcStats.All.Length; fieldSize = Mathf.Min(fieldSize, n);
            var picks = new List<(int racer, bool human, int pad)>(); var taken = new bool[n];
            for (int i = 0; i < humanRacers.Length && i < n; i++)
            {
                int want = Mathf.Clamp(humanRacers[i], 0, n - 1); for (int k = 0; k < n && taken[want]; k++) want = (want + 1) % n;
                taken[want] = true; picks.Add((want, true, pads != null && i < pads.Length ? pads[i] : -1));
            }
            for (int k = 0; k < n && picks.Count < fieldSize; k++) if (!taken[k]) { taken[k] = true; picks.Add((k, false, -1)); }
            // humans start at the back of the grid (a race to win, not a lead to keep)
            var order = picks.Where(p => !p.human).Concat(picks.Where(p => p.human)).ToList();
            for (int i = 0; i < order.Count; i++)
            {
                var (racer, human, pad) = order[i];
                float s = Track.Length - 12f - i * 7f; float x = (i % 2 == 0 ? -1f : 1f) * 2.6f;
                var r = MakeRacer(racer, i, human);
                r.Init(Track, s, x); r.Human = human; r.PadIndex = pad;
                if (!human) { int ai = order.Take(i).Count(p => !p.human); r.AI = new GcAI(1000 + i * 17, AiSkill(ai, order.Count(p => !p.human))); r.AI.ShortcutChance = racer == 2 ? 0.3f : 0.55f; }
                r.Event = OnEvent; r.Snap();
                Racers.Add(r); if (human) Humans.Add(r);
            }
            // humans in player order
            Humans.Sort((a, b) => a.PadIndex.CompareTo(b.PadIndex));
            var who = Racers.Select(x => x.Label).ToList();
            Log($"GC FIELD (R21): {string.Join(", ", Racers.Select(x => x.Label + (x.Human ? " (player)" : $" (AI, skill {x.AI.Skill:0.00}, pace {x.AI.Pace:0.000})")))}{(who.Distinct().Count() != who.Count ? " - DUPLICATE RACER" : " - one of each")}");
        }
        /// <summary>Round 21 (the AI tuned against a human-like driver, TODO.md): the AI drivers' skill by their order - the first AI is
        /// the quick one, the next a little slower (a human-like driver races them, neither lapping them nor lapped).</summary>
        public static float AiSkill(int ai, int ais) => ais <= 1 ? 0.9f : Mathf.Lerp(0.95f, 0.8f, ai / (float)Mathf.Max(1, ais - 1));

        GcRacer MakeRacer(int racer, int slot, bool human)
        {
            var go = new GameObject($"Racer{slot}_{GcStats.Of(racer).Racer}");
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<GcRacer>(); r.Stats = GcStats.Of(racer); r.Slot = slot; r.Label = GcStats.Of(racer).Racer;
            var vis = GcVehicles.Make(this, racer, go.transform); r.Visual = vis;
            // the light trail + the hover glow (white)
            var tr = go.AddComponent<TrailRenderer>(); tr.time = 0.25f; tr.startWidth = 0.22f; tr.endWidth = 0f; tr.sharedMaterial = Trail; tr.minVertexDistance = 0.5f; tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.startColor = new Color(1f, 1f, 1f, 0.35f); tr.endColor = new Color(1f, 1f, 1f, 0f);
            var sh = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(sh.GetComponent<Collider>()); sh.name = "Shield"; sh.transform.SetParent(go.transform, false); sh.transform.localPosition = new Vector3(0f, 0.8f, 0.2f); sh.transform.localScale = new Vector3(3.2f, 2.6f, 4.6f); sh.GetComponent<Renderer>().sharedMaterial = Shield; sh.SetActive(false);
            return r;
        }

        // ------------------------------------------------------------------------------------------ cameras + HUD
        public void MakeViews(int players)
        {
            foreach (var c in Cams) if (c != null) Destroy(c.gameObject); Cams.Clear();
            foreach (var h in Huds) if (h != null) Destroy(h.gameObject); Huds.Clear();
            for (int i = 0; i < players; i++)
            {
                var cam = Instantiate(CamTemplate.gameObject, transform); cam.name = "PlayerCam" + (i + 1); cam.SetActive(true);
                var c = cam.GetComponent<Camera>();
                if (players == 1) c.rect = new Rect(0f, 0f, 1f, 1f);
                else if (SideBySide) c.rect = new Rect(i * 0.5f, 0f, 0.5f, 1f);
                else c.rect = new Rect(0f, i == 0 ? 0.5f : 0f, 1f, 0.5f);
                if (i > 0) { var al = cam.GetComponent<AudioListener>(); if (al != null) Destroy(al); }
                else if (cam.GetComponent<AudioListener>() == null) cam.AddComponent<AudioListener>();
                var gc = cam.AddComponent<GcCam>(); gc.Target = Humans.Count > i ? Humans[i] : Racers[0]; gc.Split = players > 1 && !SideBySide; gc.Snap();
                Cams.Add(gc);
                var hud = GcHud.Make(c, this, gc.Target, players); Huds.Add(hud);
            }
            if (players > 0) Cams[0].GetComponent<Camera>().tag = "MainCamera";
        }

        // ------------------------------------------------------------------------------------------ the race
        public IEnumerator RaceRun(System.Func<GcRacer, int, GcControls> humanControls = null)
        {
            Over = false; Running = false; Paused = false;
            BuildTrack(NextTrack);
            bool trial = NextMode == Mode.TimeTrial;
            SpawnField(NextRacers, NextPads, trial ? 1 : GcStats.All.Length);
            MakeViews(Humans.Count);
            if (trial) { _ghost = GcGhost.Load(this, NextTrack); _rec = new GcGhostRecorder(); }
            Laps = LapsOverride > 0 ? LapsOverride : trial ? GcSave.TrialLaps : 3;
            MusicSystem.Play(GcTrack.Music[GcTrack.IndexOf(NextTrack)], 1.2f);   // Round 21: each world's own track
            foreach (var r in Racers) r.In = default;
            Moment?.Invoke("grid");
            // the lights: three low tones, one high: GO
            for (int i = 3; i >= 1; i--) { Countdown = i; AudioKit.Play("race_start_lights", null, 0.8f); yield return new WaitForSeconds(0.9f); }
            Countdown = 0; Running = true; RaceT0 = Time.time; foreach (var r in Racers) r.LapStart = Time.time;
            AudioKit.Play("bike_liftoff", null, 0.7f); Moment?.Invoke("go"); GcVoice.Start(this); _lastReal = 0f; FrameMs.Clear();
            Log($"GC RACE (R20): {(trial ? "time trial" : "single race")} on {Track.Name}, {Laps} laps, {Racers.Count} racers ({string.Join(", ", Racers.Select(r => r.Label + (r.Human ? " [P" + (Humans.IndexOf(r) + 1) + "]" : "")))}), {(Humans.Count > 1 ? (SideBySide ? "split side by side" : "split top / bottom") : "full screen")}");
            float goOff = Time.time + 1.2f;
            while (!Over)
            {
                if (Time.time > goOff) Countdown = -1;
                yield return null;
                if (Paused) continue;
                float dt = Time.deltaTime;
                float rt = Time.realtimeSinceStartup; if (_lastReal > 0f) FrameMs.Add((rt - _lastReal) * 1000f); _lastReal = rt;
                for (int i = 0; i < Racers.Count; i++)
                {
                    var r = Racers[i];
                    if (r.Finished && r.AI == null && r.Human) { r.In = new GcControls { Throttle = 0.3f }; }
                    else if (r.AI != null && (!r.Human)) r.In = r.AI.Drive(r, Racers, dt);
                    else r.In = humanControls != null ? humanControls(r, Humans.IndexOf(r)) : GcPads.Read(Humans.IndexOf(r), Humans.Count);
                    if (r.Finished && r.Human) r.In = new GcControls { Throttle = 0.4f, Steer = r.AI != null ? r.AI.Drive(r, Racers, dt).Steer : 0f };
                }
                // rubber band (AI only, mild): relative to the best human
                var bestHuman = Humans.Count > 0 ? Humans.Max(h => h.Distance) : float.NaN;
                foreach (var r in Racers) if (!r.Human) r.TopScale = r.AI.Pace * (float.IsNaN(bestHuman) ? 1f : 1f + Mathf.Clamp((bestHuman - r.Distance) / 400f, -0.05f, 0.05f));
                foreach (var r in Racers) r.Step(dt, Racers);
                foreach (var r in Racers) { if (r.In.Use && r.Item != GcPickup.None) { var hit = r.UseItem(Racers); FxUse(r, hit); } }
                Pads(dt); Presses(dt);
                Positions(); GcVoice.Tick(this);
                foreach (var r in Racers) { var sh = r.transform.Find("Shield"); if (sh != null) sh.gameObject.SetActive(r.ShieldT > 0f); }
                _rec?.Add(Humans.Count > 0 ? Humans[0] : null, Time.time - RaceT0);
                _ghost?.Play(Time.time - RaceT0);
                // the finish
                foreach (var r in Racers)
                {
                    if (r.Finished || r.Lap <= Laps) continue;
                    r.Finished = true; r.FinishTime = Time.time - RaceT0; Moment?.Invoke("finish:" + r.Label);
                    if (r.Human) { AudioKit.Play("cat_crowd_cheer", null, 0.6f); r.AI = new GcAI(5, 0.8f); }
                }
                if (Humans.Count > 0 ? Humans.All(h => h.Finished) : Racers.All(r => r.Finished)) Over = true;
                if (!Humans.Any() && Time.time - RaceT0 > 900f) Over = true;
            }
            Running = false;
            // the AI that has not finished gets an estimated time (its pace so far)
            foreach (var r in Racers.Where(r => !r.Finished)) { float frac = Mathf.Max(0.05f, r.Distance / (Laps * Track.Length)); r.FinishTime = (Time.time - RaceT0) / frac; }
            var standing = Racers.OrderBy(r => r.Finished ? 0 : 1).ThenBy(r => r.FinishTime).ToList();
            for (int i = 0; i < standing.Count; i++) standing[i].Place = i + 1;
            // saving
            foreach (var h in Humans) { GcSave.Record(Track.Id, h.Label, h.FinishTime, h.BestLap, trial); }
            if (trial && Humans.Count > 0 && _rec != null) { if (GcSave.IsBestTrial(Track.Id, Humans[0].FinishTime)) _rec.Save(Track.Id, Humans[0].Stats.Racer); }
            GcSave.Save();
            Log("GC RESULT (R20): " + string.Join(" | ", standing.Select(r => $"{r.Place}. {r.Label}{(r.Human ? " (P" + (Humans.IndexOf(r) + 1) + ")" : "")} {Fmt(r.FinishTime)}{(r.Finished ? "" : " (est.)")} best lap {Fmt(r.BestLap)}")));
            Moment?.Invoke("results");
            Results = standing;
        }
        public List<GcRacer> Results; float _lastReal;

        void Positions()
        {
            var ord = Racers.OrderByDescending(r => r.Finished ? 1e9f - r.FinishTime : r.Distance).ToList();
            for (int i = 0; i < ord.Count; i++) ord[i].Place = i + 1;
        }

        void Pads(float dt)
        {
            foreach (var r in Racers)
            {
                if (r.Air || r.RespawnT > 0f || r.On != Track) continue;
                _padCool.TryGetValue(r, out var cool); if (cool > 0f) { _padCool[r] = cool - dt; continue; }
                foreach (var p in _pads)
                {
                    float ds = Track.Ahead(p.i, r.S); if (ds > Mathf.Max(2.5f, r.V * dt + 0.5f)) continue;
                    if (Mathf.Abs(r.X - p.x) > 1.6f) continue;
                    if (r.Item == GcPickup.None) { r.Give(Roll(r)); AudioKit.Play("energy_point", r.transform.position, 0.5f, 0.05f); }
                    _padCool[r] = 1.5f; break;
                }
            }
        }
        /// <summary>Weighted by position: those behind get more Boost / Push; the leader mostly a Shield.</summary>
        GcPickup Roll(GcRacer r)
        {
            float back = Racers.Count > 1 ? (r.Place - 1f) / (Racers.Count - 1f) : 0.5f;
            float wB = 0.25f + 0.55f * back, wP = 0.3f + 0.15f * back, wS = 0.45f - 0.35f * back;
            float u = Random.value * (wB + wP + wS);
            return u < wB ? GcPickup.Boost : u < wB + wP ? GcPickup.Push : GcPickup.Shield;
        }

        void FxUse(GcRacer r, GcRacer hit)
        {
            // the boost: the trail flares (the push ring and the shield bubble come from the events / the loop)
            var tr = r.GetComponent<TrailRenderer>();
            if (r.BoostT > 2f && tr != null) StartCoroutine(Flare(tr));
        }
        IEnumerator Flare(TrailRenderer tr) { float w = tr.startWidth; tr.startWidth = 0.6f; tr.time = 0.5f; yield return new WaitForSeconds(2f); if (tr != null) { tr.startWidth = w; tr.time = 0.25f; } }
        // ------------------------------------------------------------------------------------------ Round 21: the foundry's presses
        /// <summary>The Asteroid Foundry's presses and crushers: a heavy block over HALF the road (left, then right, in turn along the
        /// line) coming down on a fixed, readable rhythm - a white warning plate lights on the road under it as it starts down (0.6 s), it
        /// stays down 0.9 s, rises. A racer under a block that is down is pushed back out and slowed (never wrecked; a shield takes it).
        /// The other half is always open, so a racer reading it is never caught.</summary>
        public class Press { public float S, Side, Period, Phase, Last; public Transform Block, Warn; public Renderer WarnR; public int Hits; }
        public readonly List<Press> PressList = new List<Press>();
        public const float PressCycle = 2.6f, PressWarn = 0.6f, PressDown = 0.9f;
        /// <summary>0 = up, 0..1 = coming down (the warning), 1 = down.</summary>
        public static float PressState(Press p, float t)
        {
            float u = Mathf.Repeat(t + p.Phase, p.Period);
            if (u < PressWarn) return u / PressWarn;
            if (u < PressWarn + PressDown) return 1f;
            if (u < PressWarn + PressDown + 0.5f) return 1f - (u - PressWarn - PressDown) / 0.5f;
            return 0f;
        }
        public float PressClock => Running ? Time.time - RaceT0 : Time.time;
        void BuildPresses()
        {
            PressList.Clear(); if (Track == null) return;
            var root = new GameObject("Presses").transform; root.SetParent(_trackRoot, false);
            int k = 0;
            GameObject Cube(string n, Material m) { var c = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(c.GetComponent<Collider>()); c.name = n; c.transform.SetParent(root, false); c.GetComponent<Renderer>().sharedMaterial = m; return c; }
            for (int i = 0; i < Track.Count; i++)
            {
                if ((Track.Flag[i] & GcTrack.F.Press) == 0 || i % 30 != 15) continue;
                var p = new Press { S = i * GcTrack.Step, Side = k % 2 == 0 ? -1f : 1f, Period = PressCycle, Phase = (k % 4) * 0.65f };
                Track.Frame(p.S, out var pos, out var rot, out float hw);
                // the press: a dark steel block over its half, guide columns either side, a crosshead, a white edge light on the block
                var blk = Cube("PressBlock" + k, Frame); blk.transform.localScale = new Vector3(hw - 0.6f, 2.4f, 4f);
                var edge = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(edge.GetComponent<Collider>()); edge.name = "Edge"; edge.transform.SetParent(blk.transform, false);
                edge.transform.localPosition = new Vector3(0f, -0.52f, -0.51f); edge.transform.localScale = new Vector3(1.02f, 0.05f, 0.03f); edge.GetComponent<Renderer>().sharedMaterial = Glow;
                foreach (var cx in new[] { 0.3f, hw + 0.5f })
                {
                    var col = Cube("Guide", Wall); col.transform.SetPositionAndRotation(pos + rot * new Vector3(p.Side * cx, 4.5f, 0f), rot); col.transform.localScale = new Vector3(0.45f, 9f, 0.45f);
                }
                var beam = Cube("Crosshead", Frame); beam.transform.SetPositionAndRotation(pos + rot * new Vector3(p.Side * (hw * 0.5f + 0.2f), 9.1f, 0f), rot); beam.transform.localScale = new Vector3(hw + 0.8f, 0.6f, 1f);
                var warn = Cube("PressWarn" + k, PickupMat); warn.transform.SetPositionAndRotation(pos + rot * new Vector3(p.Side * (hw * 0.5f), 0.04f, 0f), rot); warn.transform.localScale = new Vector3(hw - 1.2f, 0.02f, 3.6f);
                p.WarnR = warn.GetComponent<Renderer>(); p.WarnR.enabled = false;
                p.Block = blk.transform; p.Warn = warn.transform; PressList.Add(p); k++;
                PlacePress(p, 0f);
            }
            if (PressList.Count > 0) Log($"GC PRESSES (R21): {PressList.Count} presses / crushers on {Track.Name} - cycle {PressCycle:0.0} s (warning {PressWarn:0.0} s, down {PressDown:0.0} s), half the road each, alternating sides");
        }
        void PlacePress(Press p, float st)
        {
            Track.Frame(p.S, out var pos, out var rot, out float hw);
            float h = Mathf.Lerp(7.2f, 1.25f, st * st);
            p.Block.SetPositionAndRotation(pos + rot * new Vector3(p.Side * (hw * 0.5f), h, 0f), rot);
            p.WarnR.enabled = st > 0.001f;
        }
        void Presses(float dt)
        {
            if (PressList.Count == 0) return;
            float t = PressClock;
            foreach (var p in PressList)
            {
                float st = PressState(p, t); PlacePress(p, st);
                // the slam you hear as it lands (the rhythm is audible as well as visible), near a player
                if (st >= 0.999f && p.Last < 0.999f && Humans.Any(h => Vector3.Distance(h.transform.position, p.Block.position) < 70f)) AudioKit.Play("machinist_step_3", p.Block.position, 0.55f, 0.08f);
                p.Last = st;
                if (st < 0.95f) continue;
                foreach (var r in Racers)
                {
                    if (r.Air || r.RespawnT > 0f || r.On != Track) continue;
                    float ds = Track.Ahead(p.S - 2.2f, r.S); if (ds > 4.4f) continue;
                    if (r.X * p.Side < 0.6f) continue;   // its half of the road only
                    r.S = Track.Wrap(p.S - 2.6f);         // pushed back out from under it (non-lethal)
                    if (r.ShieldT > 0f) { r.ShieldT = 0f; OnEvent(r, "shielded"); continue; }
                    r.V = Mathf.Min(r.V, 14f); r.SlowT = Mathf.Max(r.SlowT, 1.2f); p.Hits++; r.PressHits++;
                    OnEvent(r, "press");
                }
            }
        }
        /// <summary>For the AI: is a press ahead within lookAhead that will be down (or coming down) when this racer gets there?</summary>
        public bool PressAhead(GcRacer me, float lookAhead, out float pressS, out float side)
        {
            pressS = 0f; side = 0f;
            if (me.On != Track) return false;
            foreach (var p in PressList)
            {
                float d = Track.Ahead(me.S, p.S); if (d > lookAhead) continue;
                float v = Mathf.Max(8f, me.V), arrive = PressClock + (d - 2.5f) / v, leave = PressClock + (d + 2.5f) / v;
                if (PressState(p, arrive) > 0.5f || PressState(p, leave) > 0.5f || PressState(p, (arrive + leave) * 0.5f) > 0.5f) { pressS = p.S; side = p.Side; return true; }
            }
            return false;
        }

        public void PushRing(GcRacer from)
        {
            var go = new GameObject("PushWave"); go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>(); lr.loop = true; lr.positionCount = 24; lr.useWorldSpace = true; lr.sharedMaterial = Wave; lr.widthMultiplier = 0.25f; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            StartCoroutine(WaveRun(lr, from));
        }
        IEnumerator WaveRun(LineRenderer lr, GcRacer from)
        {
            float t = 0f; var p0 = from.transform.position; var q = from.transform.rotation;
            while (t < 0.7f && lr != null)
            {
                t += Time.deltaTime; float rad = 1f + t * 5f; var c = p0 + q * Vector3.forward * (4f + t * 50f) + q * Vector3.up * 1f;
                for (int i = 0; i < 24; i++) { float a = i / 24f * Mathf.PI * 2f; lr.SetPosition(i, c + q * new Vector3(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad * 0.6f, 0f)); }
                lr.widthMultiplier = 0.3f * (1f - t / 0.7f);
                yield return null;
            }
            if (lr != null) Destroy(lr.gameObject);
        }

        void OnEvent(GcRacer r, string e)
        {
            bool near = Humans.Contains(r);
            switch (e)
            {
                case "wall": if (near) AudioKit.Play("climb_scuff", r.transform.position, 0.6f, 0.1f); break;
                case "jump": if (near) AudioKit.Play("bike_whoosh", r.transform.position, 0.5f); break;
                case "land": if (near) AudioKit.Play("land_hard", r.transform.position, 0.5f, 0.05f); break;
                case "gate": if (near) AudioKit.Play("cat_gate_chime", r.transform.position, 0.35f); break;
                case "lap": if (near) AudioKit.Play("cat_gate_chime", r.transform.position, 0.7f); break;
                case "boostpad": if (near) AudioKit.Play("liftoff", r.transform.position, 0.35f, 0.05f); break;
                case "use:push": AudioKit.Play("tk_push", r.transform.position, near ? 0.7f : 0.4f); PushRing(r); break;
                case "use:boost": AudioKit.Play("bike_liftoff", r.transform.position, near ? 0.7f : 0.3f); break;
                case "use:shield": AudioKit.Play("energy_point", r.transform.position, 0.6f); break;
                case "pushed": if (near) AudioKit.Play("fight_hit", r.transform.position, 0.5f); break;
                case "shielded": AudioKit.Play("fight_block", r.transform.position, 0.5f); break;
                case "bump": if (near) AudioKit.Play("fight_block", r.transform.position, 0.3f, 0.1f); break;
                case "press": AudioKit.Play(PressSfx, r.transform.position, near ? 0.8f : 0.4f, 0.05f); break;
            }
            if (e.StartsWith("fall:")) Log($"GC FALL (R20): {r.Label} slot {r.Slot} {e.Substring(5)} at s {r.S:0} (lap {r.Lap}) - back on at the last gate");
            Moment?.Invoke(e + "|" + r.Slot);
            GcVoice.On(this, r, e);
        }

        public static string PressSfx = "ast_clamp";
        public static string Fmt(float t) { if (t < 0f || float.IsNaN(t) || float.IsInfinity(t)) return "--:--.--"; int m = (int)(t / 60f); return $"{m}:{t - m * 60f:00.00}"; }
        public static void Log(string s) { Debug.Log("[Seven] " + s); ChapterCapture.I?.Log(s); }

        public void Quit()
        {
            foreach (var r in Racers) if (r != null) Destroy(r.gameObject); Racers.Clear(); Humans.Clear();
            foreach (var c in Cams) if (c != null) Destroy(c.gameObject); Cams.Clear();
            foreach (var h in Huds) if (h != null) Destroy(h.gameObject); Huds.Clear();
            _ghost?.Clear(); _ghost = null; _rec = null; Over = true; Running = false; Paused = false; Time.timeScale = GcBoot.Speed;
        }
    }

}
