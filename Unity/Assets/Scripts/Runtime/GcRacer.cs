using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>Round 20: one racer's controls for a frame (a pad, the keyboard, the AI or the test bot fill it).</summary>
    public struct GcControls { public float Steer, Throttle, Brake; public bool AirL, AirR, Use; }

    /// <summary>Per-vehicle numbers (Docs/RACING_MODE.md "Racers and vehicles").</summary>
    [System.Serializable]
    public class GcStats
    {
        public string Racer, Vehicle; public float Top, Accel, Handling, Weight, Grip;
        public static readonly GcStats Seven = new GcStats { Racer = "7", Vehicle = "her hover bike", Top = 50f, Accel = 15f, Handling = 1.0f, Weight = 1.0f, Grip = 1.0f };
        public static readonly GcStats Duchess = new GcStats { Racer = "Duchess", Vehicle = "her racer #8", Top = 53f, Accel = 17f, Handling = 0.9f, Weight = 0.7f, Grip = 0.82f };
        public static readonly GcStats Tunneler = new GcStats { Racer = "Tunneler", Vehicle = "his drill rig", Top = 52f, Accel = 11f, Handling = 0.85f, Weight = 1.7f, Grip = 1.1f };
        public static readonly GcStats[] All = { Seven, Duchess, Tunneler };
        public static GcStats Of(int i) => All[Mathf.Clamp(i, 0, All.Length - 1)];
    }

    /// <summary>
    /// Round 20: an anti-grav racer, simulated in track coordinates: s (metres along the lap), x (across, 0 = the centre line),
    /// psi (heading relative to the track, degrees), v (speed). It hovers on the surface and sticks to it whatever its "down" is
    /// (loops, the corkscrew, banks); the track's curvature turns the track under it, so the player steers through every bend.
    /// Walls scrape speed; an edge without a wall (the jump gaps) = airborne with world gravity until it lands on the track again
    /// (or misses it: back on at the last gate, a short penalty). Loops / the corkscrew hold a minimum speed (the assist) so
    /// nobody falls off upside down. Bumps push by weight, never wreck. Pickups: Push / Shield / Boost (non-lethal).
    /// </summary>
    public class GcRacer : MonoBehaviour
    {
        public GcStats Stats; public int Slot; public bool Human; public int PadIndex = -1; public string Label;
        public GcTrack Main; public GcTrack On;                 // the track it is on (the main line or the shortcut)
        public float S, X, Psi, V, Hover = 0.55f;
        public bool Air; Vector3 _airPos, _airVel; Quaternion _airRot; float _airT;
        public GcControls In;
        public Transform Visual;                              // the vehicle + rider (its local up = the track's up)
        public int Lap, NextGate = 1, Gates; public float Progress; public bool Finished; public float FinishTime; public int Place;
        public bool Started; public float LapStart, BestLap = -1f, LastLap = -1f; public List<float> LapTimes = new List<float>();
        public float LastGateS; public int Falls, WallHits, Bumps, PushesHit, Shielded, Boosts, PadsTaken, Landings, FallThroughs, Respawns;
        public GcPickup Item = GcPickup.None; public float ShieldT, SlowT, BoostT, StunLat, RespawnT;
        public const float MinAssistSpeed = 26f;
        public float AirTime; public bool OnShortcut => On != null && On != Main;
        public float LeanVis; Vector3 _lastWorld; public Vector3 WorldVel { get; private set; }
        public System.Action<GcRacer, string> Event;
        public GcAI AI;
        public float TopScale = 1f;                           // the AI's mild rubber band
        public float Distance => (Lap - 1) * Main.Length + Progress + (Lap == 0 && Progress < Main.Length * 0.5f ? Main.Length : 0f); // total race progress (for positions)
        public Vector3 WorldPos { get; private set; }
        public Quaternion WorldRot { get; private set; }

        public const float G = 16f;                            // gravity in the air (floaty arcade jumps)
        public const int GatesPerLap = 8;

        public void Init(GcTrack t, float s, float x)
        {
            // the grid is just behind the line: every gate counts as passed, so crossing the line starts lap 1 (Lap 0 -> 1 = "on lap 1")
            Main = t; On = t; S = s; X = x; Psi = 0f; V = 0f; Air = false; Lap = 0; NextGate = GatesPerLap; Progress = s; LastGateS = s; Started = false;
            Place3D(); _lastWorld = WorldPos;
        }

        // ------------------------------------------------------------------------------------------ the step
        public void Step(float dt, List<GcRacer> all)
        {
            if (Finished && AI == null && !Human) return;
            if (RespawnT > 0f) { RespawnT -= dt; if (RespawnT <= 0f) Respawn(); Place3D(); return; }
            ShieldT = Mathf.Max(0f, ShieldT - dt); SlowT = Mathf.Max(0f, SlowT - dt); BoostT = Mathf.Max(0f, BoostT - dt);
            if (Air) { StepAir(dt); Place3D(); return; }
            On.Frame(S, out _, out _, out float hw);
            var fl = On.Flags(S);
            // speed: throttle toward the top speed (boost lifts it), brake, drag; air-brakes bleed a little
            float top = Stats.Top * TopScale * (SlowT > 0f ? 0.72f : 1f) + (BoostT > 0f ? 16f : 0f);
            float acc = Stats.Accel * (V < top * 0.5f ? 1.25f : 1f);
            if (In.Throttle > 0.05f && V < top) V = Mathf.Min(top, V + acc * In.Throttle * dt);
            else if (V > top) V = Mathf.MoveTowards(V, top, 9f * dt);
            if (In.Brake > 0.05f) V = Mathf.Max(0f, V - 30f * In.Brake * dt);
            V -= (0.012f * V + (In.Throttle < 0.05f ? 3f : 0f)) * dt;
            if (In.AirL || In.AirR) V -= 3.5f * dt;
            if ((fl & GcTrack.F.Assist) != 0 && V < MinAssistSpeed) V = Mathf.MoveTowards(V, MinAssistSpeed, 40f * dt);   // nobody falls off a loop
            if ((fl & GcTrack.F.Boost) != 0 && Mathf.Abs(X) < 2.6f) { if (BoostT < 0.6f) { Event?.Invoke(this, "boostpad"); } BoostT = Mathf.Max(BoostT, 1.1f); }
            V = Mathf.Max(0f, V);
            // steering: the heading relative to the track; the track's own bend turns it the other way
            float steer = Mathf.Clamp(In.Steer, -1f, 1f);
            float rate = 62f * Stats.Handling * (1f - 0.35f * Mathf.Clamp01(V / 60f));
            float turn = steer * rate;
            if (In.AirL) turn -= 48f * Stats.Handling; if (In.AirR) turn += 48f * Stats.Handling;
            float curv = On.Curv(S);
            float ds = V * Mathf.Cos(Psi * Mathf.Deg2Rad) * dt;
            Psi += turn * dt - curv * ds;
            // grip: the heading relaxes back toward the travel direction a little when not steering (anti-grav slide feel)
            if (Mathf.Abs(steer) < 0.1f && !In.AirL && !In.AirR) Psi = Mathf.MoveTowards(Psi, 0f, 6f * Stats.Grip * dt);
            Psi = Mathf.Clamp(Psi, -80f, 80f);
            X += V * Mathf.Sin(Psi * Mathf.Deg2Rad) * dt + StunLat * dt; StunLat = Mathf.MoveTowards(StunLat, 0f, 14f * dt);
            Advance(ds);
            On.Frame(S, out _, out _, out hw);
            fl = On.Flags(S);
            // the jump: the ribbon stops - airborne from here
            if ((fl & GcTrack.F.Gap) != 0) { TakeOff(); Place3D(); return; }
            // walls (or the edge)
            float lim = hw - 0.9f;
            if (Mathf.Abs(X) > lim)
            {
                if ((fl & GcTrack.F.Walls) != 0 || (fl & (GcTrack.F.Loop | GcTrack.F.Cork)) != 0)
                {
                    float sg = Mathf.Sign(X); X = sg * lim;
                    if (Psi * sg > 0f) { float hit = Mathf.Sin(Mathf.Abs(Psi) * Mathf.Deg2Rad); V *= 1f - Mathf.Clamp01(hit * 0.9f) * 0.5f; Psi = -Psi * 0.25f; WallHits++; Event?.Invoke(this, "wall"); }
                    else V -= 2f * dt;
                }
                else if (Mathf.Abs(X) > hw + 0.4f) { FallOff("off the edge"); Place3D(); return; }
            }
            Bump(all);
            Place3D();
        }

        void Advance(float ds)
        {
            float before = S;
            S += ds;
            if (On == Main)
            {
                // the shortcut: at the fork, on the fork side of the track -> onto the branch
                var b = Main.Branch;
                if (b != null && ds > 0f)
                {
                    float a0 = Main.Ahead(b.JoinFrom, before), a1 = Main.Ahead(b.JoinFrom, S);
                    if (a1 < a0 && X * b.ForkSide > 3.5f) { On = b; S = a1; X = X - b.ForkSide * (Main.HalfW[Main.Idx(b.JoinFrom)] - 5f); X = Mathf.Clamp(X, -3f, 3f); Event?.Invoke(this, "shortcut"); }
                }
                if (On == Main && S >= Main.Length) S -= Main.Length;
                if (On == Main && S < 0f) S += Main.Length;
            }
            else
            {
                if (S >= On.Length - 0.5f) { var b = On; S = b.JoinTo + (S - b.Length); On = Main; X = Mathf.Clamp(X + b.ForkSide * 1.5f, -5f, 5f); Event?.Invoke(this, "rejoin"); }
                if (S < 0f) { S = Main.Branch.JoinFrom; On = Main; }
            }
            // progress along the MAIN line (the shortcut counts linearly from its fork to its join)
            float prog = On == Main ? S : Main.Wrap(Main.Branch.JoinFrom + (S / On.Length) * Main.Ahead(Main.Branch.JoinFrom, Main.Branch.JoinTo));
            // gates, in order; the lap counts at the line with every gate passed
            float gateLen = Main.Length / GatesPerLap;
            int gNow = Mathf.FloorToInt(prog / gateLen);
            while (NextGate < GatesPerLap && gNow >= NextGate && gNow - NextGate < 2) { NextGate++; Gates++; LastGateS = Main.Wrap((NextGate - 1) * gateLen + 2f); Event?.Invoke(this, "gate"); }
            if (NextGate >= GatesPerLap && prog < gateLen * 0.5f && Progress > Main.Length - gateLen)
            {
                Lap++; NextGate = 1; LastGateS = 2f;
                float now = Time.time;
                if (Started) { LastLap = now - LapStart; LapTimes.Add(LastLap); if (BestLap < 0f || LastLap < BestLap) BestLap = LastLap; }
                Started = true; LapStart = now;
                Event?.Invoke(this, "lap");
            }
            Progress = prog;
        }

        void TakeOff()
        {
            Air = true; AirTime = 0f; _airT = 0f; _wasAbove = true;
            On.Frame(S, out var p, out var r, out _);
            // launch along the kicker's lip (the track at the gap's start is pitched up); the jump assist: at least the speed that carries
            // the racer to the far side's surface (a slow racer still makes it - a fast one lands further down the landing ramp)
            var launchDir = (r * Quaternion.Euler(0f, Psi * 0.5f, 0f) * Vector3.forward).normalized;
            int i0 = On.Idx(S), i1 = i0; while ((On.Flag[i1 % On.Count] & GcTrack.F.Gap) != 0 && i1 - i0 < 400) i1++;
            var land = On.P[i1 % On.Count] + On.R[i1 % On.Count] * new Vector3(X, Hover, 0f);
            var start = p + r * new Vector3(X, Hover, 0f);
            var flat = land - start; float dh = flat.y; flat.y = 0f; float D = flat.magnitude;
            float th = Mathf.Asin(Mathf.Clamp(launchDir.y, -0.9f, 0.9f)); float cos = Mathf.Cos(th), tan = Mathf.Tan(th);
            float need = (D * tan - dh) > 0.5f ? Mathf.Sqrt(G * D * D / (2f * cos * cos * (D * tan - dh))) : 40f;
            float v = Mathf.Max(V, need * 1.04f); if (V < need) JumpAssists++;
            _airPos = start; _airVel = launchDir * v; _airRot = Quaternion.LookRotation(launchDir, r * Vector3.up);
            _gapEnd = i1 % On.Count;
            Event?.Invoke(this, "jump");
        }
        bool _wasAbove; int _gapEnd; public int JumpAssists;

        void StepAir(float dt)
        {
            AirTime += dt; _airT += dt;
            _airVel += Vector3.down * G * dt;
            if (In.AirL) _airVel = Quaternion.AngleAxis(-25f * dt, Vector3.up) * _airVel; if (In.AirR) _airVel = Quaternion.AngleAxis(25f * dt, Vector3.up) * _airVel;
            _airVel += Vector3.ProjectOnPlane(_airRot * Vector3.right, Vector3.up) * (In.Steer * 6f * dt);
            var next = _airPos + _airVel * dt;
            // landing: the nearest ribbon sample past the gap; land when the racer comes down THROUGH its surface level from above
            int ni = On.Nearest(next, _gapEnd + 30, 60);
            On.Frame(ni * GcTrack.Step, out var tp, out var tr, out var hw);
            var loc = Quaternion.Inverse(tr) * (next - tp);
            bool overRoad = (On.Flag[ni] & GcTrack.F.Gap) == 0 && Mathf.Abs(loc.x) < hw + 0.3f && Mathf.Abs(loc.z) < 1.5f;
            if (overRoad && loc.y <= Hover)
            {
                if (!_wasAbove || loc.y < -2.5f) FallThroughs++;   // came up from under the surface (or deep below it): counted, must stay 0
                // landed: back into track coordinates; the speed into the surface is lost (a landing thump)
                S = ni * GcTrack.Step; X = Mathf.Clamp(loc.x, -(hw - 1f), hw - 1f);
                var lv = Quaternion.Inverse(tr) * _airVel; V = Mathf.Max(0f, new Vector2(lv.x, lv.z).magnitude * 0.96f);
                Psi = Mathf.Clamp(Mathf.Atan2(lv.x, lv.z) * Mathf.Rad2Deg, -40f, 40f);
                Air = false; Landings++; Event?.Invoke(this, "land");
                return;
            }
            if (overRoad) _wasAbove = loc.y > Hover;
            _airPos = next; _airRot = Quaternion.Slerp(_airRot, Quaternion.LookRotation(_airVel.sqrMagnitude > 1f ? _airVel.normalized : _airRot * Vector3.forward, Vector3.up), dt * 2f);
            if (_airT > 6f || _airPos.y < LowestY - 40f) FallOff("missed the landing");
        }
        public static float LowestY = -60f;

        void FallOff(string why)
        {
            Falls++; Air = false; RespawnT = 1.4f; V = 0f;
            Event?.Invoke(this, "fall:" + why);
        }
        void Respawn()
        {
            On = Main; S = LastGateS; X = 0f; Psi = 0f; V = 18f; Air = false; Respawns++; ShieldT = Mathf.Max(ShieldT, 1.5f);
            Event?.Invoke(this, "respawn");
        }

        void Bump(List<GcRacer> all)
        {
            foreach (var o in all)
            {
                if (o == this || o.Air || Air || o.RespawnT > 0f || o.On != On) continue;
                float ds = On.Closed ? Mathf.DeltaAngle(0f, 0f) + (o.S - S) : o.S - S;
                if (On.Closed) { float L = On.Length; if (ds > L * 0.5f) ds -= L; if (ds < -L * 0.5f) ds += L; }
                float dx = o.X - X;
                if (Mathf.Abs(ds) < 3.2f && Mathf.Abs(dx) < 1.9f)
                {
                    // push apart across the track by weight (the heavier one moves less), trade a little speed
                    float wSum = Stats.Weight + o.Stats.Weight; float mine = o.Stats.Weight / wSum;
                    float sep = 1.9f - Mathf.Abs(dx); float sg = dx >= 0f ? -1f : 1f;
                    if (ShieldT <= 0f) { X += sg * sep * mine; StunLat += sg * 4f * mine; }
                    if (o.ShieldT <= 0f) { o.X -= sg * sep * (1f - mine); o.StunLat -= sg * 4f * (1f - mine); }
                    if (ds > 0f && ds < 3.2f && V > o.V) { float dv = (V - o.V) * 0.5f; V -= dv * (o.Stats.Weight / wSum); o.V += dv * (Stats.Weight / wSum); }
                    if (Slot < o.Slot) { Bumps++; Event?.Invoke(this, "bump"); }
                }
            }
        }

        // ------------------------------------------------------------------------------------------ pickups
        public void Give(GcPickup p) { if (Item != GcPickup.None) return; Item = p; PadsTaken++; Event?.Invoke(this, "pickup:" + p); }
        public GcRacer UseItem(List<GcRacer> all)
        {
            var it = Item; if (it == GcPickup.None) return null; Item = GcPickup.None;
            switch (it)
            {
                case GcPickup.Boost: BoostT = 2.2f; V += 8f; Boosts++; Event?.Invoke(this, "use:boost"); return null;
                case GcPickup.Shield: ShieldT = 5f; Event?.Invoke(this, "use:shield"); return null;
                case GcPickup.Push:
                {
                    // the telekinesis wave: the nearest racer ahead within 45 m (same line) is nudged off its line and slowed
                    GcRacer best = null; float bd = 45f;
                    foreach (var o in all) { if (o == this || o.RespawnT > 0f) continue; float d = o.Distance - Distance; if (d > 0f && d < bd) { bd = d; best = o; } }
                    Event?.Invoke(this, "use:push");
                    if (best != null) best.TakePush(this);
                    return best;
                }
            }
            return null;
        }
        public void TakePush(GcRacer from)
        {
            if (ShieldT > 0f) { Shielded++; Event?.Invoke(this, "shielded"); return; }
            PushesHit++; SlowT = 1.3f; V *= 0.88f; StunLat += (X > 0f ? -1f : 1f) * 9f; if (!Air) Psi += (X > 0f ? -1f : 1f) * 8f;
            Event?.Invoke(this, "pushed");
        }

        // ------------------------------------------------------------------------------------------ the 3D placement
        void Place3D()
        {
            if (Air) { WorldPos = _airPos; WorldRot = _airRot; }
            else if (RespawnT > 0f) { /* hold where it fell (dropping) */ WorldPos += Vector3.down * 9f * Time.deltaTime; }
            else
            {
                On.Frame(S, out var p, out var r, out _);
                float bob = Mathf.Sin(Time.time * 3.1f + Slot) * 0.05f;
                WorldPos = p + r * new Vector3(X, Hover + bob, 0f);
                LeanVis = Mathf.Lerp(LeanVis, -Mathf.Clamp(In.Steer + (In.AirR ? 0.8f : 0f) - (In.AirL ? 0.8f : 0f), -1.2f, 1.2f) * 16f, Time.deltaTime * 6f);
                WorldRot = r * Quaternion.Euler(0f, Psi, 0f) * Quaternion.Euler(0f, 0f, LeanVis);
            }
            float dt = Mathf.Max(1e-4f, Time.deltaTime); WorldVel = (WorldPos - _lastWorld) / dt; _lastWorld = WorldPos;
            transform.SetPositionAndRotation(WorldPos, WorldRot);
        }
        public void Snap() => Place3D();
    }

    public enum GcPickup { None, Push, Shield, Boost }
}
