using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using K = Seven.CattalusCourse;

namespace Seven
{
    /// <summary>
    /// Round 18 — the Grand Circuit (CAT_07; Docs/CHAPTER_CATTALUS.md "The race"): two racers, 7 on her bike and Duchess on her
    /// hover racer, one lap (~2½ minutes) through the six sections. Gates are Cattalus things (hanging-ornament arches); the next
    /// one glows. The toy boulevard's toys swing on a fixed, readable rhythm (a warning stripe on each beam). A toy that hits her
    /// = a crash: a white flash, back on the course at the last gate, +3 s (a small time note at the bottom). Duchess rides a
    /// good racing line at a pace a clean rider can beat and a sloppy one can't (her pace is set from the course itself: a clean
    /// lap of the bike at full speed through every corner, plus 5 %), with only a mild invisible catch-up (±3 %), never cheating
    /// speeds. She is confined to the course while racing (the course zones round her). Panther's lines follow her position
    /// (the market bend, the heights, leading / behind) - telepathic, the race camera stays on her. The practice lap: the same
    /// gates, no opponent, CAT_06_01 as a tip on the first one. Results: her time + penalties vs Duchess's.
    /// </summary>
    public class CattalusRace : MonoBehaviour
    {
        public HoverBike Bike;
        public ChapterSeven Seven;
        public ChapterCamera Cam;
        public ChapterUI UI;
        public Transform DuchessRacer; public CatFolk Duchess;
        public Renderer[] Gates; public int[] GateIdx;
        public Transform[] Toys; public int[] ToyIdx; public float[] ToyPeriod, ToyPhase;
        public Renderer[] StartLights;
        public Transform[] Spectators;
        public CatatronScreen Screen;

        public bool Running { get; private set; }
        public bool Practice { get; private set; }
        public bool Finished { get; private set; }
        public float HerTime { get; private set; }
        public float DuchessTime { get; private set; } = -1f;
        public float Penalty { get; private set; }
        public int Crashes { get; private set; }
        public int GatesPassed { get; private set; }
        public float HerDist { get; private set; }
        public float DuchessDist { get; private set; }
        public bool Leading => HerDist >= DuchessDist;
        public int HerIndex => _idx;
        /// <summary>The road ahead of her (what a player steers by): the course a little way on.</summary>
        public Vector3 PilotAim => K.AtDist(HerDist + 9f + (Bike != null ? Bike.Speed * 0.55f : 0f));
        public int Section => K.Section[K.IndexAtDist(HerDist)];
        public float ClockNow => Running ? Time.time - _t0 + Penalty : HerTime;
        public string Log => $"lap {K.Length:0} m, her time {Fmt(HerTime)} (penalties {Penalty:0} s, crashes {Crashes}), Duchess {Fmt(DuchessTime)}, gates {GatesPassed}/{(GateIdx != null ? GateIdx.Length : 0)}; QF28 (1) top speed: hers {TopSpeedSeen:0.0} m/s ({TopSpeedSeen * 3.6f:0} km/h, ordinary riding 24), Duchess {DuchessTop:0.0} m/s, clean-gate boosts {Boosts}";
        public static string Fmt(float s) => CattalusState.Fmt(s);
        public System.Action<string> Moment;
        /// <summary>QF28 (1): the race's top speed (m/s) - well above ordinary riding (24 m/s; 0.18.2 capped the race at 20, the
        /// slowest riding in the game), a quick launch (accel 22 vs 13) and a boost out of every clean gate.</summary>
        public const float RaceTop = 34f, RaceAccel = 22f, BoostAdd = 5f, BoostTime = 1.6f; float _max0 = 24f, _acc0 = 13f;
        public float TopSpeedSeen { get; private set; }
        public int Boosts { get; private set; }
        float _boostUntil; bool _cleanSince = true;      // "start", "gate", "crash", "section:<n>", "lead", "behind", "duchess_finished", "finished"

        int _idx, _dIdx, _lastGate = -1, _nextGate = 1; float _grace; float _t0, _dSpeed, _dPace = 1f; float _sPrev; bool _dFin;
        float _startD = -16f, _dLane;
        System.Func<Vector3, float, bool> _zoneWas;

        void Update()
        {
            if (Toys == null) return;
            // the toys swing all the time (a fixed rhythm the player can read from a distance)
            for (int i = 0; i < Toys.Length; i++)
            {
                if (Toys[i] == null) continue;
                float sw = K.ToySwing(Time.time, ToyPeriod[i], ToyPhase[i]);
                Toys[i].rotation = Quaternion.LookRotation(K.Dir(ToyIdx[i])) * Quaternion.Euler(0f, 0f, sw * 52f);
            }
        }

        /// <summary>The whole race (or the practice lap): the grid, the lights, the lap, the finish.</summary>
        public IEnumerator Run(bool practice, System.Func<string, IEnumerator> say)
        {
            QF30Watch.Free(practice ? "practice laps on the Grand Circuit" : "the Grand Circuit race (the race HUD is the objective)");   // P3 (3)
            if (Duchess != null) NpcGlideProbe.Riding.Add(Duchess.transform);   // P3 (3): seated in her racer - carried, not walking
            Practice = practice; Finished = false; HerTime = 0f; DuchessTime = -1f; Penalty = 0f; Crashes = 0; GatesPassed = 0; _lastGate = -1; _nextGate = 1; _dFin = false;
            _max0 = Bike.MaxSpeed; _acc0 = Bike.Accel; Bike.MaxSpeed = RaceTop; Bike.Accel = RaceAccel; Bike.PitchBoost = 0.35f;   // QF28 (1)
            TopSpeedSeen = 0f; Boosts = 0; _boostUntil = 0f; _cleanSince = true; SpeedCues(true);
            HerDist = _startD; DuchessDist = practice ? -9999f : _startD;
            // the grid: her on the left lane, Duchess on the right, just before the line
            var g = K.AtDist(_startD); var d = K.Dir(K.IndexAtDist(_startD)); var side = Vector3.Cross(Vector3.up, d);
            if (!Bike.Riding) Bike.Mount();
            Bike.Place(g - side * 3f, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg);
            _idx = K.IndexAtDist(_startD); _dIdx = _idx; _sPrev = K.Dist[_idx];
            K.Around(_idx);
            _zoneWas = HoverBike.ZoneTest; HoverBike.ZoneTest = OnCourse;
            if (DuchessRacer != null) { DuchessRacer.gameObject.SetActive(!practice); PlaceDuchess(_startD, 3f); if (!practice && !DuchessSeated) SeatDuchess(); }
            Cam.RaceMode = true; Cam.Target = Bike.transform; Cam.Distance = 7.6f; Cam.LookHeight = 1.35f; Cam.AutoBehind = true; Cam.DefaultPitch = 12f; Cam.Follow(); Cam.SnapBehind();
            Bike.ScriptDriven = true; Bike.ScriptThrottle = 0f; Bike.ScriptSteer = 0f;            // held at the line until green
            SetDuchessPace();
            Glow(-1);
            // the lights: three low, one long high; the announcer; the race track starts with the lights (QF24 18)
            if (!practice) yield return say("CAT_07_01");                                      // Racers ready!
            AudioKit.Play("race_start_lights", g + Vector3.up * 2f, 1f);
            if (!practice) MusicSystem.Play("cattalus_race", 0.6f);
            for (int i = 0; i < 4; i++) { if (StartLights != null && i < StartLights.Length) Glowing.Set(StartLights[i], 2f); yield return Wait(i < 3 ? 0.62f : 0.1f); }
            Bike.ScriptDriven = DemoBoot.CaptureDir != null && !DemoBoot.PlayerSim;
            Running = true; _t0 = Time.time; Moment?.Invoke("start");
            if (Screen != null && !practice) Screen.FollowRace(Bike.transform);
            if (!practice) StartCoroutine(say("CAT_07_CALL2"));                                // And they're off!
            Glow(0);
            float noProgress = 0f, bestDist = HerDist; int lastSec = -1; bool leadSaid = false, behindSaid = false;
            while (!Finished)
            {
                Track();
                if (!practice) DriveDuchess();
                // gates: passed in order as her distance crosses them
                while (GateIdx != null && _nextGate < GateIdx.Length && HerDist >= GateDist(_nextGate)) { _lastGate = _nextGate; _nextGate++; GatesPassed++; AudioKit.Play("cat_gate_chime", Bike.transform.position, 0.6f); Moment?.Invoke("gate"); Glow(_nextGate < GateIdx.Length ? _nextGate : 0); GateBoost(_lastGate); }
                Boosting();
                int sec = Section; if (sec != lastSec) { lastSec = sec; Moment?.Invoke("section:" + sec); }
                if (!practice && sec == 3 && !leadSaid && !behindSaid) { if (Leading) { leadSaid = true; Moment?.Invoke("lead"); } else { behindSaid = true; Moment?.Invoke("behind"); } }
                // the toys: a hit = a crash
                if (HitByToy()) yield return Crash(say);
                // no progress (stopped / turned round) for long: the restart is offered (the pause menu has it too)
                if (HerDist > bestDist + 1f) { bestDist = HerDist; noProgress = 0f; } else noProgress += Time.deltaTime;
                if (noProgress > 30f) { noProgress = 0f; UI.Hint("Restart the race from the pause menu", 6f); }
                if (HerDist >= K.Length) Finish();
                yield return null;
            }
            Running = false;
            Bike.ScriptDriven = true; Bike.ScriptThrottle = -1f; Bike.ScriptSteer = 0f;
            for (float t = 0f; t < 3f && Bike.Speed > 0.5f; t += Time.deltaTime) { Track(); if (!practice && !_dFin) DriveDuchess(); yield return null; }
            Bike.ScriptThrottle = 0f; Bike.ScriptDriven = DemoBoot.CaptureDir != null && !DemoBoot.PlayerSim;
            // Duchess finishes her lap (she is never frozen on the course)
            for (float t = 0f; !practice && !_dFin && t < 40f; t += Time.deltaTime) { DriveDuchess(); yield return null; }
            if (!practice && DuchessTime < 0f) DuchessTime = HerTime + 8f;
            Glow(-2);
            HoverBike.ZoneTest = _zoneWas ?? CattalusLayout.BikeZone; K.Around(-1); Bike.MaxSpeed = _max0; Bike.Accel = _acc0; Bike.PitchBoost = 0f; SpeedCues(false);
            QF30Watch.EndFree(); if (Duchess != null) NpcGlideProbe.Riding.Remove(Duchess.transform);
            if (!practice) yield return DuchessClimbsOut();
            Cam.RaceMode = false;
            if (StartLights != null) foreach (var r in StartLights) Glowing.Set(r, 0.15f);
            if (Screen != null) Screen.Show(CatatronScreen.Mode.Ads);           // QF41: back to the commercials after the race
        }
        void Finish()
        {
            if (Finished) return;
            Finished = true; HerTime = Time.time - _t0 + Penalty; Moment?.Invoke("finished");
            AudioKit.Play("cat_crowd_cheer", Bike.transform.position, 1f);
        }
        bool OnCourse(Vector3 p, float pad) { foreach (var z in K.Pieces) if (z.Enabled && z.Contains(p.x, p.z, pad)) return true; return false; }
        void Track()
        {
            if (Bike == null) return;
            _idx = K.Nearest(Bike.transform.position, _idx, 30);
            float s = K.Dist[_idx], ds = s - _sPrev;
            if (ds < -K.Length * 0.5f) ds += K.Length; if (ds > K.Length * 0.5f) ds -= K.Length;
            HerDist += ds; _sPrev = s;
            K.Around(_idx);
            Cam.RaceAim = K.AtDist(HerDist + 40f); Cam.RaceAimUntil = Time.time + 0.5f;
        }
        float GateDist(int g) { var s = K.Dist[GateIdx[g]]; if (g == 0) s += K.Length; return s; }   // gate 0 is the line (the finish)
        int NextGate() { if (GateIdx == null) return -1; for (int g = 1; g < GateIdx.Length; g++) if (GateDist(g) > HerDist) return g; return HerDist < K.Length ? 0 : -1; }
        void Glow(int next)
        {
            if (Gates == null) return;
            for (int g = 0; g < Gates.Length; g++) if (Gates[g] != null) Glowing.Set(Gates[g], next == -2 ? 0.2f : g == next ? 1.6f + 0.4f * Mathf.Sin(Time.time * 4f) : 0.25f);
        }

        // ---- Duchess
        /// <summary>Her pace from the course: the time of a clean lap at full speed through every corner (the bike's own
        /// numbers), plus 5 % - a clean rider beats her; crashes or slow corners don't.</summary>
        void SetDuchessPace()
        {
            float clean = 0f, hers = 0f;
            for (int i = 0; i < K.Count; i++) { clean += K.Step / Mathf.Max(4f, K.CornerSpeed(i, RaceTop) * 1.04f); hers += K.Step / Mathf.Max(4f, K.CornerSpeed(i, RaceTop * 0.95f)); }
            float target = clean * 1.0f;          // QF28 (1): a clean lap (with its boosts) beats her by a few seconds; a sloppy one can't
            _dPace = hers / target;                                     // scale her profile so her clean lap takes 'target'
            _dSpeed = 0f;
            Debug.Log($"[Seven] GRAND CIRCUIT: lap {K.Length:0} m, a clean full-speed lap {clean:0.0} s, Duchess's lap {target:0.0} s (pace x{_dPace:0.00})");
            ChapterCapture.I?.Log($"GRAND CIRCUIT: lap {K.Length:0} m, {K.Gates().Count} gates, {K.Toys().Count} swinging toys; race top speed {RaceTop:0} m/s (+{BoostAdd:0} boosting), a clean lap at full speed {clean:0.0} s (the corner model, no boosts), Duchess rides {target:0.0} s");
        }
        void DriveDuchess()
        {
            if (DuchessRacer == null || _dFin) return;
            int i = K.IndexAtDist(DuchessDist);
            float want = K.CornerSpeed(i, RaceTop * 0.95f) * _dPace;
            float gap = DuchessDist - HerDist;                                       // mild, invisible catch-up (±3 %)
            if (Running) want *= gap > 60f ? 0.97f : gap < -60f ? 1.03f : 1f;
            _dSpeed = Mathf.MoveTowards(_dSpeed, want, (want > _dSpeed ? 15f : 22f) * Time.deltaTime);
            DuchessDist += _dSpeed * Time.deltaTime;
            float lane = 2.6f * Mathf.Sin(DuchessDist * 0.004f) + 1.2f;               // a smooth racing line across the road
            if (Bike != null && Mathf.Abs(DuchessDist - HerDist) < 10f)
            {
                var c = K.AtDist(HerDist); var sd = Vector3.Cross(Vector3.up, K.Dir(K.IndexAtDist(HerDist))); float herLane = Vector3.Dot(Bike.transform.position - c, sd);
                _dLane = Mathf.MoveTowards(_dLane, herLane > 0f ? -4.2f : 4.2f, Time.deltaTime * 3f); lane = _dLane;
            }
            else _dLane = lane;
            PlaceDuchess(DuchessDist, lane); DuchessTop = Mathf.Max(DuchessTop, _dSpeed);
            if (DuchessDist >= K.Length && DuchessTime < 0f) { DuchessTime = Time.time - _t0; _dFin = true; Moment?.Invoke("duchess_finished"); }
            if (Running && Bike != null) TopSpeedSeen = Mathf.Max(TopSpeedSeen, Bike.Speed);
        }
        void PlaceDuchess(float dist, float lane)
        {
            var p = K.AtDist(dist); var q = K.AtDist(dist + 3f); var f = q - p; f.y = 0f; if (f.sqrMagnitude < 1e-4f) f = Vector3.forward;
            var side = Vector3.Cross(Vector3.up, f.normalized);
            DuchessRacer.SetPositionAndRotation(p + side * lane + Vector3.up * 0.65f, Quaternion.LookRotation(f.normalized) * Quaternion.Euler(0f, 0f, -Mathf.Clamp(Vector3.SignedAngle(f, K.AtDist(dist + 12f) - q, Vector3.up), -20f, 20f) * 0.5f));
        }

        // ---- QF28 (1): the boost, the speed cues
        /// <summary>A clean gate (through its middle half, no crash since the last one, at speed) gives a short boost.</summary>
        void GateBoost(int g)
        {
            if (g < 1 || Bike == null) { _cleanSince = true; return; }
            var c = K.At(GateIdx[g]); var sd = Vector3.Cross(Vector3.up, K.Dir(GateIdx[g])); float lat = Mathf.Abs(Vector3.Dot(Bike.transform.position - c, sd));
            bool clean = _cleanSince && lat < K.HalfWidth * 0.55f && Bike.Speed > RaceTop * 0.6f;
            _cleanSince = true;
            if (!clean) return;
            Boosts++; _boostUntil = Time.time + BoostTime;
            Bike.Kick(4f, RaceTop + BoostAdd);
            AudioKit.Play("bike_liftoff", Bike.transform.position, 0.8f, 0.03f);
            if (Cam != null) Cam.SpeedKick = Mathf.Max(Cam.SpeedKick, 1.6f);
            if (_streaks != null) _streaks.Emit(40);
            Moment?.Invoke("boost");
        }
        void Boosting() { if (Bike != null) Bike.MaxSpeed = Time.time < _boostUntil ? RaceTop + BoostAdd : RaceTop; }

        ParticleSystem _streaks;
        /// <summary>Wind streaks in the haze round her (white, stretched with speed) while she races.</summary>
        void SpeedCues(bool on)
        {
            if (!on) { if (_streaks != null) Destroy(_streaks.gameObject); _streaks = null; return; }
            if (Bike == null || _streaks != null) return;
            var go = new GameObject("RaceWindStreaks"); go.transform.SetParent(Bike.transform, false); go.transform.localPosition = new Vector3(0f, 1.4f, 14f);
            _streaks = go.AddComponent<ParticleSystem>(); _streaks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var mn = _streaks.main; mn.loop = true; mn.startLifetime = 0.5f; mn.startSpeed = 0f; mn.startSize = 0.05f; mn.maxParticles = 400; mn.simulationSpace = ParticleSystemSimulationSpace.World; mn.startColor = new Color(1f, 1f, 1f, 0.5f);
            var sh = _streaks.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(16f, 7f, 10f);
            var col = _streaks.colorOverLifetime; col.enabled = true; var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.45f, 0.3f), new GradientAlphaKey(0f, 1f) }); col.color = gr;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.05f; r.lengthScale = 2f;
            r.sharedMaterial = Markers.Additive(0.9f);
            _streaks.Play();
        }
        void LateUpdate()
        {
            if (_streaks == null || Bike == null) return;
            // streaks only at speed: they stay in the world, so the bike rushes through them (the faster, the longer)
            var em = _streaks.emission; float k = Mathf.Clamp01((Bike.Speed - 18f) / (RaceTop - 18f)); em.rateOverTime = 260f * k * k;
        }

        // ---- QF28 (4): Duchess rides her racer (grid, race, finish, the highlights), and climbs out after
        Transform _dHome, _seat; public float DuchessTop { get; private set; }
        /// <summary>At the grid before the start: her racer on the right of the line, Duchess in it.</summary>
        public void DuchessToGrid() { if (DuchessRacer == null) return; DuchessRacer.gameObject.SetActive(true); PlaceDuchess(_startD, 3f); SeatDuchess(); }
        public void SeatDuchess()
        {
            if (Duchess == null || DuchessRacer == null) return;
            _seat = DuchessRacer.Find("DuchessSeat"); if (_seat == null) return;
            if (_dHome == null) _dHome = Duchess.transform.parent;
            Duchess.gameObject.SetActive(true); Duchess.StopWalk(); Duchess.Play("idle", 0.1f);
            var pose = Duchess.Pose != null ? Duchess.Pose : Duchess.GetComponent<CatPoseOverlay>();
            Duchess.transform.SetParent(_seat, false); Duchess.transform.localRotation = Quaternion.identity;
            if (pose != null) { pose.Ride = 1f; pose.RideNow(); Duchess.transform.localPosition = Vector3.down * pose.RideDrop; } else Duchess.transform.localPosition = Vector3.zero;
            ChapterCapture.I?.Log($"DUCHESS RIDES (QF28 4): seated in her racer's padded bed (crouched {(pose != null ? pose.RideDrop : 0f):0.00} m down, head up behind the windscreen, ears back, tail tucked), parented to the racer");
            Moment?.Invoke("duchess_grid");
        }
        public bool DuchessSeated => Duchess != null && _seat != null && Duchess.transform.parent == _seat;
        IEnumerator DuchessClimbsOut()
        {
            if (!DuchessSeated) yield break;
            // the racer coasts to a stop beyond the line, then she climbs out over the side
            float v = Mathf.Max(_dSpeed, 6f), d = DuchessDist;
            while (v > 0.2f) { v = Mathf.MoveTowards(v, 0f, 9f * Time.deltaTime); d += v * Time.deltaTime; PlaceDuchess(d, _dLane); yield return null; }
            Moment?.Invoke("duchess_finish");
            yield return Wait(0.6f);
            var pose = Duchess.Pose != null ? Duchess.Pose : Duchess.GetComponent<CatPoseOverlay>();
            var side = DuchessRacer.right * (_dLane >= 0f ? 1f : -1f); var to = DuchessRacer.position + side * 1.4f; to.y = K.AtDist(d).y + 0.02f;
            Duchess.transform.SetParent(_dHome, true);
            if (pose != null) pose.Ride = 0f;
            yield return Duchess.Hop(to, 0.7f, 0.45f);
            Duchess.Place(to, DuchessRacer.forward); Duchess.Play("idle");
            Moment?.Invoke("duchess_out");
            ChapterCapture.I?.Log($"DUCHESS RIDES (QF28 4): she climbs out over the side after the finish (racer stopped {d - K.Length:0} m past the line)");
        }

        // ---- the toys and crashes
        bool HitByToy()
        {
            if (Toys == null || Bike == null || Time.time < _grace) return false;
            var rider = Bike.transform.position + Vector3.up * 1.3f;
            foreach (var t in Toys) { if (t == null) continue; var body = t.position + t.rotation * (Vector3.down * 8.4f); if ((body - rider).sqrMagnitude < 2.0f * 2.0f) return true; }
            return false;
        }
        IEnumerator Crash(System.Func<string, IEnumerator> say)
        {
            Crashes++; Penalty += 3f; Moment?.Invoke("crash"); _cleanSince = false; _boostUntil = 0f;
            AudioKit.Play("cat_toy_swing", Bike.transform.position, 1f); CameraShake.Quake(0.4f, 0.6f);
            bool sd = Bike.ScriptDriven; Bike.ScriptDriven = true; Bike.ScriptThrottle = -1f;
            yield return UI.Fade(0.85f, 0.25f, 1f);
            // back on the course at the last gate, facing on
            float at = _lastGate >= 1 ? GateDist(_lastGate) + 4f : Mathf.Max(_startD, HerDist - 30f);
            if (ToyIdx != null) foreach (var ti in ToyIdx) { float ts = K.Dist[ti]; if (at > ts - 18f && at < ts + 3f) at = ts - 18f; }   // never set down just in front of a toy
            _grace = Time.time + 2.5f;
            var p = K.AtDist(at); var d = K.Dir(K.IndexAtDist(at));
            _idx = K.IndexAtDist(at); _sPrev = K.Dist[_idx]; HerDist = at; K.Around(_idx);
            Bike.Place(p, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg); Cam.SnapBehind();
            UI.Hint("+3 s", 2.5f);
            yield return UI.Fade(0f, 0.35f, 1f);
            Bike.ScriptThrottle = 0f; Bike.ScriptDriven = sd;
            if (Crashes <= 2) StartCoroutine(say("CAT_07_CALL1"));                     // Up again. Three seconds, not the race.
        }
        static IEnumerator Wait(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }

        /// <summary>A swinging toy within this distance ahead (a rider who knows the boulevard moves to the side its swing
        /// passes over - 5.8 m out from the middle the toy is overhead even at the end of its swing).</summary>
        public bool ToyAhead(float within) { if (ToyIdx == null) return false; foreach (var ti in ToyIdx) { float a = K.Dist[ti] - Mathf.Repeat(HerDist, K.Length); if (a < 0f) a += K.Length; if (a > -3f && a < within) return true; } return false; }
        /// <summary>Check runs' autopilot (-catrace): a racing line by pure pursuit, full throttle where the corner allows;
        /// 'slow' = a sloppy rider (a lower top throttle and early braking) for the runner-up path.</summary>
        public void Autopilot(bool slow)
        {
            if (!Running || Bike == null) return;
            var aim = PilotAim; if (!slow && ToyAhead(45f)) aim += Vector3.Cross(Vector3.up, K.Dir(K.IndexAtDist(HerDist + 12f))) * 5.8f;
            var f = aim - Bike.transform.position; f.y = 0f;
            float err = Vector3.SignedAngle(Vector3.ProjectOnPlane(Bike.transform.forward, Vector3.up), f, Vector3.up);
            Bike.ScriptSteer = Mathf.Clamp(err / 22f, -1f, 1f);
            float corner = K.CornerSpeed(_idx, Bike.MaxSpeed) * (slow ? 0.78f : 1.04f);
            Bike.ScriptThrottle = Bike.Speed > corner ? -0.4f : slow ? 0.72f : 1f;
            // the toys: a clean rider times them (waits for the swing to pass), a sloppy one doesn't
            if (!slow && Toys != null) for (int i = 0; i < Toys.Length; i++)
            {
                float ahead = K.Dist[ToyIdx[i]] - K.Dist[_idx]; if (ahead < 0f) ahead += K.Length;
                if (ahead > 4f && ahead < 26f) { float eta = ahead / Mathf.Max(4f, Bike.Speed); float sw = K.ToySwing(Time.time + eta, ToyPeriod[i], ToyPhase[i]); if (Mathf.Abs(sw) < 0.45f) Bike.ScriptThrottle = Mathf.Min(Bike.ScriptThrottle, Bike.Speed > 9f ? -0.6f : 0.2f); }
            }
        }
    }
}
