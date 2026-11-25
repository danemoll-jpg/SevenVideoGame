using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 20 (Grand Circuit, Docs/RACING_MODE.md): an anti-grav track = a closed ribbon sampled every metre. Each sample is a
    /// frame (position, rotation: forward along the track, up = the surface normal the racers stick to), a half-width, and flags
    /// (walls, a jump gap, boost pad, pickup pad, a loop / corkscrew section where the minimum-speed assist holds).
    /// The track is defined in turtle steps (<see cref="Piece"/>): yaw about the world up, pitch about the track's right (loops,
    /// ramps), roll about its forward (banks, corkscrews), each a pure angle change over a length, so the closing orientation is
    /// exact; the closing position error is spread over the flat pieces. Loops shift sideways by a track width as they turn so
    /// the exit passes beside the entry. A branch (the shortcut) is its own open track that leaves the main one at s0 and meets it
    /// again at s1 (its end error spread the same way).
    /// The SAME definition is used by the builder (scenery placed along it) and the game (the mesh is built at load, the racers
    /// ride it in track coordinates: s along, x across).
    /// </summary>
    public class GcTrack
    {
        [System.Flags] public enum F { None = 0, Walls = 1, Gap = 2, Boost = 4, Pickup = 8, Assist = 16, Loop = 32, Cork = 64, Fork = 128, Start = 256, Tunnel = 512 }

        public struct Piece
        {
            public float Len, Yaw, Pitch, Roll, HalfW0, HalfW1, Shift; public F Flags;
            public Piece(float len, float yaw = 0f, float pitch = 0f, float roll = 0f, float halfW = 7f, F flags = F.Walls, float halfW1 = -1f, float shift = 0f)
            { Len = len; Yaw = yaw; Pitch = pitch; Roll = roll; HalfW0 = halfW; HalfW1 = halfW1 < 0f ? halfW : halfW1; Flags = flags; Shift = shift; }
        }

        public const float Step = 1f, CorkR = 8f;
        public string Name, Id;
        public Vector3[] P; public Quaternion[] R; public float[] HalfW; public F[] Flag; public float[] Bank;
        public int Count => P.Length;
        public float Length => Count * Step;
        public bool Closed = true;
        /// <summary>Main-track s where a branch leaves / rejoins (branches only).</summary>
        public float JoinFrom, JoinTo; public GcTrack Branch; public int ForkSide = 1;
        public float CloseError;

        // ------------------------------------------------------------------------------------------ build
        public static GcTrack Make(string id, string name, Vector3 start, float yaw0, IList<Piece> pieces, bool closed = true, Vector3? endAt = null, Quaternion? endRot = null)
        {
            var t = new GcTrack { Id = id, Name = name, Closed = closed };
            var ps = new List<Vector3>(); var rs = new List<Quaternion>(); var ws = new List<float>(); var fs = new List<F>(); var bank = new List<float>(); var flatW = new List<float>();
            float yaw = yaw0, pitch = 0f, roll = 0f; Vector3 p = start; float shiftAcc = 0f;
            Quaternion Rot(float y, float pi, float r) => Quaternion.Euler(0f, y, 0f) * Quaternion.AngleAxis(-pi, Vector3.right) * Quaternion.AngleAxis(r, Vector3.forward);
            foreach (var pc in pieces)
            {
                int n = Mathf.Max(1, Mathf.RoundToInt(pc.Len / Step));
                float dy = pc.Yaw / n, dp = pc.Pitch / n, dr = pc.Roll / n;
                bool flat = (pc.Flags & (F.Loop | F.Cork | F.Gap)) == 0 && Mathf.Abs(pc.Pitch) < 0.01f;
                bool cork = (pc.Flags & F.Cork) != 0; var up0 = Rot(yaw, pitch, roll) * Vector3.up;
                for (int i = 0; i < n; i++)
                {
                    float k = (i + 0.5f) / n;
                    var q = Rot(yaw + dy * 0.5f, pitch + dp * 0.5f, roll + dr * 0.5f);
                    // the corkscrew rides round an axis CorkR above the track (a heartline roll), not a twist in place
                    var off = cork ? (up0 - Rot(yaw, pitch, roll) * Vector3.up) * CorkR : Vector3.zero;
                    ps.Add(p + off); rs.Add(q); ws.Add(Mathf.Lerp(pc.HalfW0, pc.HalfW1, k)); fs.Add(pc.Flags); bank.Add(roll); flatW.Add(flat ? 1f : 0f);
                    yaw += dy; pitch += dp; roll += dr;
                    p += q * Vector3.forward * Step;
                    if (pc.Shift != 0f) p += Quaternion.Euler(0f, yaw, 0f) * Vector3.right * (pc.Shift / n);
                }
                shiftAcc += pc.Shift;
            }
            // close: spread the position error over the flat samples
            Vector3 target = closed ? start : (endAt ?? p);
            var err = target - p; t.CloseError = err.magnitude;
            float total = 0f; foreach (var w in flatW) total += w;
            if (total > 0f && err.sqrMagnitude > 1e-6f)
            {
                float acc = 0f;
                for (int i = 0; i < ps.Count; i++) { ps[i] += err * (acc / total); acc += flatW[i]; }
            }
            // smooth the rotations a touch so the corrected positions and the frames agree (forward = the real chord direction on flat parts)
            for (int i = 0; i < ps.Count; i++)
            {
                int j = (i + 1) % ps.Count; if (!closed && i == ps.Count - 1) continue;
                var fwd = (ps[j] - ps[i]); if (fwd.sqrMagnitude < 1e-6f) continue;
                var up = rs[i] * Vector3.up;
                rs[i] = Quaternion.LookRotation(fwd.normalized, up);
            }
            t.P = ps.ToArray(); t.R = rs.ToArray(); t.HalfW = ws.ToArray(); t.Flag = fs.ToArray(); t.Bank = bank.ToArray();
            return t;
        }

        // ------------------------------------------------------------------------------------------ queries
        public int Idx(float s) { int n = Count; int i = Mathf.FloorToInt(s / Step); if (Closed) { i %= n; if (i < 0) i += n; } else i = Mathf.Clamp(i, 0, n - 1); return i; }
        public float Wrap(float s) { if (!Closed) return Mathf.Clamp(s, 0f, Length - 0.001f); float L = Length; s %= L; if (s < 0f) s += L; return s; }
        public void Frame(float s, out Vector3 pos, out Quaternion rot, out float halfW)
        {
            s = Wrap(s);
            int i = Idx(s); int j = Closed ? (i + 1) % Count : Mathf.Min(i + 1, Count - 1);
            float k = (s - i * Step) / Step;
            pos = Vector3.Lerp(P[i], P[j], k); rot = Quaternion.Slerp(R[i], R[j], k); halfW = Mathf.Lerp(HalfW[i], HalfW[j], k);
        }
        public Vector3 Pos(float s) { Frame(s, out var p, out _, out _); return p; }
        public Quaternion Rot(float s) { Frame(s, out _, out var r, out _); return r; }
        public F Flags(float s) => Flag[Idx(s)];
        public Vector3 World(float s, float x, float h) { Frame(s, out var p, out var r, out _); return p + r * new Vector3(x, h, 0f); }
        /// <summary>Signed yaw curvature (deg per metre) of the track's heading in its own surface plane at s (+ = turning right).</summary>
        public float Curv(float s)
        {
            var r0 = Rot(s - 2f); var r1 = Rot(s + 2f);
            var f1 = Quaternion.Inverse(r0) * (r1 * Vector3.forward);
            return Mathf.Atan2(f1.x, f1.z) * Mathf.Rad2Deg / 4f;
        }
        /// <summary>The nearest sample to a world point, searched within ±window of a hint index (or everywhere).</summary>
        public int Nearest(Vector3 w, int hint = -1, int window = 0)
        {
            int best = 0; float bd = float.MaxValue;
            if (hint < 0 || window <= 0) { for (int i = 0; i < Count; i += 2) { float d = (P[i] - w).sqrMagnitude; if (d < bd) { bd = d; best = i; } } return best; }
            for (int o = -window; o <= window; o++) { int i = hint + o; if (Closed) { i %= Count; if (i < 0) i += Count; } else if (i < 0 || i >= Count) continue; float d = (P[i] - w).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
            return best;
        }
        /// <summary>Distance of b ahead of a along the closed track (0..L).</summary>
        public float Ahead(float a, float b) { float d = b - a; float L = Length; d %= L; if (d < 0f) d += L; return d; }

        // ------------------------------------------------------------------------------------------ the two Stage 1 tracks
        /// <summary>Cattalus Grand Circuit (Unlimited): out of the square on a long straight, the scratching-post column with a loop
        /// threaded through it, a banked sweep, the corkscrew round the cat-tree towers, the big jump over the toy boulevard, the
        /// shortcut (a narrow cut through the cardboard quarter, on the inside of the long right-hander), home.</summary>
        public static GcTrack Cattalus()
        {
            const float W = 8f; var L = new List<Piece>
            {
                new Piece(60f, 0f, 0f, 0f, W, F.Walls | F.Start),
                new Piece(30f, 0f, 0f, 0f, W, F.Walls | F.Boost),
                new Piece(40f, 0f, 0f, 0f, W, F.Walls | F.Pickup),
                // the scratching-post column: a loop round it (the column stands inside the loop's circle)
                new Piece(20f, 0f, 0f, 0f, W, F.Walls),
                new Piece(150f, 0f, 360f, 0f, 7f, F.Walls | F.Loop | F.Assist, -1f, 24f),
                new Piece(30f, 0f, 0f, 0f, W, F.Walls),
                // a banked sweep right
                new Piece(20f, 0f, 0f, 18f, W, F.Walls),
                new Piece(110f, 90f, 0f, 0f, W, F.Walls),
                new Piece(20f, 0f, 0f, -18f, W, F.Walls | F.Pickup),
                new Piece(40f, 0f, 0f, 0f, W, F.Walls | F.Boost),
                // the corkscrew round the cat-tree towers: a climb, one full roll, down
                new Piece(30f, 0f, 10f, 0f, W, F.Walls),
                new Piece(40f, 0f, -10f, 0f, W, F.Walls),
                new Piece(120f, 0f, 0f, 360f, 7f, F.Walls | F.Cork | F.Assist),
                new Piece(30f, 0f, 0f, 0f, W, F.Walls),
                // the long right-hander: the cardboard quarter is on its inside (the shortcut leaves at the fork)
                new Piece(20f, 0f, 0f, 0f, 11f, F.Walls | F.Fork, 11f),
                new Piece(150f, 90f, 0f, 0f, W, F.Walls),
                new Piece(30f, 0f, 0f, 0f, W, F.Walls | F.Pickup),
                // the big jump over the toy boulevard: a kicker up, the gap, a landing ramp down
                new Piece(40f, 0f, 12f, 0f, W, F.Walls | F.Boost),
                new Piece(50f, 0f, -24f, 0f, W, F.Gap),
                new Piece(40f, 0f, 12f, 0f, 9f, F.Walls),
                new Piece(30f, 0f, 0f, 0f, W, F.Walls),
                // home: a wide right hairpin round the square (banked), the stands, the line
                new Piece(39f, 0f, 0f, 0f, W, F.Walls | F.Pickup),
                new Piece(20f, 0f, 0f, 22f, W, F.Walls),
                new Piece(120f, 90f, 0f, 0f, W, F.Walls),
                new Piece(20f, 0f, 0f, -22f, W, F.Walls),
                new Piece(303f, 0f, 0f, 0f, W, F.Walls | F.Boost),
                new Piece(20f, 0f, 0f, 14f, W, F.Walls),
                new Piece(110f, 90f, 0f, 0f, W, F.Walls),
                new Piece(20f, 0f, 0f, -14f, W, F.Walls),
                new Piece(60f, 0f, 0f, 0f, W, F.Walls),
            };
            var t = Make("cattalus", "Cattalus Grand Circuit (Unlimited)", new Vector3(0f, 1.2f, 0f), 0f, L);
            t.AddShortcut(1, 10f, 0.9f);
            return t;
        }

        /// <summary>Lunar Loop: through the hollow Moon's city - a long tunnel out of the dock, the giant loop in the cavern, a
        /// sweeping descent past the pyramids, the jump across the mining pit, the terrace switchbacks, the tunnel home.</summary>
        public static GcTrack Lunar()
        {
            const float W = 8f; var L = new List<Piece>
            {
                new Piece(60f, 0f, 0f, 0f, W, F.Walls | F.Start | F.Tunnel),
                new Piece(240f, 0f, 0f, 0f, W, F.Walls | F.Tunnel | F.Boost),
                new Piece(20f, 0f, 0f, -16f, W, F.Walls | F.Tunnel),
                new Piece(100f, -90f, 0f, 0f, W, F.Walls | F.Tunnel),
                new Piece(20f, 0f, 0f, 16f, W, F.Walls | F.Tunnel | F.Pickup),
                new Piece(40f, 0f, 0f, 0f, W, F.Walls),
                // the giant cavern loop
                new Piece(200f, 0f, 360f, 0f, 8f, F.Walls | F.Loop | F.Assist, -1f, 22f),
                new Piece(40f, 0f, 0f, 0f, W, F.Walls | F.Boost),
                // the descent past the pyramids: a long banked left, dropping
                new Piece(20f, 0f, 0f, -20f, W, F.Walls),
                new Piece(60f, 0f, -12f, 0f, W, F.Walls),
                new Piece(130f, -90f, 0f, 0f, W, F.Walls | F.Pickup),
                new Piece(60f, 0f, 12f, 0f, W, F.Walls),
                new Piece(20f, 0f, 0f, 20f, W, F.Walls),
                // the jump across the mining pit
                new Piece(40f, 0f, 14f, 0f, W, F.Walls | F.Boost),
                new Piece(60f, 0f, -28f, 0f, W, F.Gap),
                new Piece(40f, 0f, 14f, 0f, 9f, F.Walls),
                new Piece(20f, 0f, 0f, 0f, W, F.Walls | F.Pickup),
                // the terrace switchbacks: left, right, left
                new Piece(70f, -60f, 0f, 0f, W, F.Walls),
                new Piece(70f, 60f, 0f, 0f, W, F.Walls),
                new Piece(20f, 0f, 0f, 0f, W, F.Walls | F.Boost),
                new Piece(20f, 0f, 0f, -18f, W, F.Walls),
                new Piece(130f, -90f, 0f, 0f, W, F.Walls),
                new Piece(20f, 0f, 0f, 18f, W, F.Walls),
                // the tunnel home
                new Piece(72f, 0f, 0f, 0f, W, F.Walls | F.Tunnel | F.Pickup),
                new Piece(20f, 0f, 12f, -16f, W, F.Walls | F.Tunnel),
                new Piece(100f, -90f, 0f, 0f, W, F.Walls | F.Tunnel),
                new Piece(20f, 0f, -12f, 16f, W, F.Walls | F.Tunnel),
                new Piece(70f, 0f, 0f, 0f, W, F.Walls | F.Tunnel),
            };
            return Make("lunar", "Lunar Loop", new Vector3(0f, 1.2f, 0f), 0f, L);
        }

        public static GcTrack ById(string id) => id == "lunar" ? Lunar() : Cattalus();
        public static readonly string[] Ids = { "cattalus", "lunar" };
        public static readonly string[] Names = { "Cattalus Grand Circuit (Unlimited)", "Lunar Loop" };

        /// <summary>A shortcut across the inside of the bend after the fork: a smooth Hermite curve from the fork (the fork side of the
        /// widened road) to just past the bend's end (beside the main line's fork-side lane), sampled every metre; narrow (half-width 5),
        /// walled, with boost chevrons along its straighter middle (it saves ~15 m of distance - the boost makes it worth the tighter line).</summary>
        void AddShortcut(int side, float extra, float tangent)
        {
            int f0 = -1; for (int i = 0; i < Count; i++) if ((Flag[i] & F.Fork) != 0) { f0 = i; break; }
            if (f0 < 0) return;
            int f1 = f0; while (f1 < Count && (Flag[f1] & F.Fork) != 0) f1++;
            // the bend after the fork ends where the heading has turned ~90 degrees
            int e = f1; var fw0 = R[f1] * Vector3.forward; while (e < Count - 1 && Vector3.Angle(R[e] * Vector3.forward, fw0) < 89f) e++;
            float s0 = (f0 + (f1 - f0) * 0.5f) * Step, s1 = Wrap(e * Step + extra);
            Frame(s0, out var p0, out var q0, out var hw0); Frame(s1, out var p1, out var q1, out _);
            var P0 = p0 + q0 * Vector3.right * (side * (hw0 - 5f)); var P1 = p1 + q1 * Vector3.right * (side * 1.5f);
            var T0 = q0 * Vector3.forward; var T1 = q1 * Vector3.forward; float c = (P1 - P0).magnitude;
            Vector3 H(float t) { float t2 = t * t, t3 = t2 * t; return (2f * t3 - 3f * t2 + 1f) * P0 + (t3 - 2f * t2 + t) * c * tangent * T0 + (-2f * t3 + 3f * t2) * P1 + (t3 - t2) * c * tangent * T1; }
            // arc-length resampling every metre
            var fine = new List<Vector3>(); for (int k = 0; k <= 4000; k++) fine.Add(H(k / 4000f));
            var cum = new float[fine.Count]; for (int k = 1; k < fine.Count; k++) cum[k] = cum[k - 1] + (fine[k] - fine[k - 1]).magnitude;
            float L = cum[cum.Length - 1]; int n = Mathf.Max(2, Mathf.FloorToInt(L / Step));
            var b = new GcTrack { Id = Id + "_shortcut", Name = "shortcut", Closed = false };
            b.P = new Vector3[n]; b.R = new Quaternion[n]; b.HalfW = new float[n]; b.Flag = new F[n]; b.Bank = new float[n];
            int at = 0;
            for (int i = 0; i < n; i++)
            {
                float want = i * L / (n - 1); while (at < cum.Length - 2 && cum[at + 1] < want) at++;
                float u = (want - cum[at]) / Mathf.Max(1e-5f, cum[at + 1] - cum[at]); b.P[i] = Vector3.Lerp(fine[at], fine[at + 1], u);
            }
            var up0 = q0 * Vector3.up; var up1 = q1 * Vector3.up;
            for (int i = 0; i < n; i++)
            {
                var fwd = (i < n - 1 ? b.P[i + 1] - b.P[i] : b.P[i] - b.P[i - 1]).normalized; float k = i / (float)(n - 1);
                b.R[i] = Quaternion.LookRotation(fwd, Vector3.Slerp(up0, up1, k)); b.HalfW[i] = 5f;
                b.Flag[i] = F.Walls | F.Tunnel | (k > 0.25f && k < 0.75f ? F.Boost : F.None);
            }
            b.JoinFrom = s0; b.JoinTo = s1; b.ForkSide = side; b.CloseError = 0f; Branch = b;
        }

        // ------------------------------------------------------------------------------------------ the mesh
        /// <summary>The ribbon (top + underside + edge skirts), the side walls (low, with a glowing top strip), the light strips along
        /// the edges and the centre dashes, the boost pads (chevrons), the pickup pads, gates every 1/8 lap (+ the start gate).</summary>
        public void BuildMeshes(Transform parent, Material road, Material under, Material wall, Material glow, Material glowSoft, Material pad, bool isBranch = false)
        {
            var rv = new List<Vector3>(); var rt = new List<int>(); var ruv = new List<Vector2>();
            var uv2 = new List<Vector3>(); var ut = new List<int>();
            var wv = new List<Vector3>(); var wt = new List<int>();
            var gv = new List<Vector3>(); var gt = new List<int>();
            var sv = new List<Vector3>(); var st = new List<int>();
            var pv = new List<Vector3>(); var pt = new List<int>();
            int n = Count; int last = Closed ? n : n - 1;
            void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d) { int k = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); t.Add(k); t.Add(k + 2); t.Add(k + 1); t.Add(k + 1); t.Add(k + 2); t.Add(k + 3); }
            for (int i = 0; i < last; i++)
            {
                int j = (i + 1) % n;
                if ((Flag[i] & F.Gap) != 0) continue;
                Vector3 Pt(int k, float x, float y) => P[k] + R[k] * new Vector3(x, y, 0f);
                float wi = HalfW[i], wj = HalfW[j];
                // the surface (two strips so the UV runs across)
                int k0 = rv.Count;
                rv.Add(Pt(i, -wi, 0f)); rv.Add(Pt(i, wi, 0f)); rv.Add(Pt(j, -wj, 0f)); rv.Add(Pt(j, wj, 0f));
                ruv.Add(new Vector2(0f, i * 0.1f)); ruv.Add(new Vector2(1f, i * 0.1f)); ruv.Add(new Vector2(0f, (i + 1) * 0.1f)); ruv.Add(new Vector2(1f, (i + 1) * 0.1f));
                rt.Add(k0); rt.Add(k0 + 2); rt.Add(k0 + 1); rt.Add(k0 + 1); rt.Add(k0 + 2); rt.Add(k0 + 3);
                // the underside, 0.6 m below, faces the other way + the skirts
                Quad(uv2, ut, Pt(i, wi, -0.6f), Pt(i, -wi, -0.6f), Pt(j, wj, -0.6f), Pt(j, -wj, -0.6f));
                Quad(uv2, ut, Pt(i, -wi, -0.6f), Pt(i, -wi, 0f), Pt(j, -wj, -0.6f), Pt(j, -wj, 0f));
                Quad(uv2, ut, Pt(i, wi, 0f), Pt(i, wi, -0.6f), Pt(j, wj, 0f), Pt(j, wj, -0.6f));
                // walls: 0.9 m high, 0.3 thick, a glowing strip on top
                if ((Flag[i] & F.Walls) != 0)
                {
                    foreach (var sg in new[] { -1f, 1f })
                    {
                        if (!WallShown(i, sg, isBranch)) continue;
                        float xi = sg * wi, xj = sg * wj, o = sg * 0.3f;
                        if (sg < 0) { Quad(wv, wt, Pt(i, xi, 0f), Pt(i, xi, 0.9f), Pt(j, xj, 0f), Pt(j, xj, 0.9f)); Quad(wv, wt, Pt(i, xi + o, 0.9f), Pt(i, xi + o, -0.6f), Pt(j, xj + o, 0.9f), Pt(j, xj + o, -0.6f)); }
                        else { Quad(wv, wt, Pt(i, xi, 0.9f), Pt(i, xi, 0f), Pt(j, xj, 0.9f), Pt(j, xj, 0f)); Quad(wv, wt, Pt(i, xi + o, -0.6f), Pt(i, xi + o, 0.9f), Pt(j, xj + o, -0.6f), Pt(j, xj + o, 0.9f)); }
                        var lo = sg < 0 ? 0f : o; var hi = sg < 0 ? o : 0f;
                        Quad(gv, gt, Pt(i, xi + lo, 0.92f), Pt(i, xi + hi, 0.92f), Pt(j, xj + lo, 0.92f), Pt(j, xj + hi, 0.92f));
                    }
                }
                // the edge light strips on the surface (always) + the centre dashes
                foreach (var sg in new[] { -1f, 1f })
                {
                    float a = sg * (wi - 0.35f), b = sg * (wi - 0.1f), aj = sg * (wj - 0.35f), bj = sg * (wj - 0.1f);
                    if (sg < 0) Quad(gv, gt, Pt(i, a < b ? a : b, 0.02f), Pt(i, a < b ? b : a, 0.02f), Pt(j, aj < bj ? aj : bj, 0.02f), Pt(j, aj < bj ? bj : aj, 0.02f));
                    else Quad(gv, gt, Pt(i, a, 0.02f), Pt(i, b, 0.02f), Pt(j, aj, 0.02f), Pt(j, bj, 0.02f));
                }
                if (i % 8 < 3) Quad(sv, st, Pt(i, -0.12f, 0.015f), Pt(i, 0.12f, 0.015f), Pt(j, -0.12f, 0.015f), Pt(j, 0.12f, 0.015f));
                // boost pads: chevrons across the middle, every 4 m in a boost piece
                if ((Flag[i] & F.Boost) != 0 && i % 4 == 0)
                {
                    int i2 = Mathf.Min(i + 2, last);
                    Quad(pv, pt, Pt(i, -2.4f, 0.03f), Pt(i, -1.1f, 0.03f), Pt(i2, -1.4f, 0.03f), Pt(i2, -0.1f, 0.03f));
                    Quad(pv, pt, Pt(i, 1.1f, 0.03f), Pt(i, 2.4f, 0.03f), Pt(i2, 0.1f, 0.03f), Pt(i2, 1.4f, 0.03f));
                }
            }
            void Save(string name, List<Vector3> v, List<int> t, Material m, bool collide, List<Vector2> uv = null)
            {
                if (v.Count == 0) return;
                var mesh = new Mesh { name = Id + "_" + name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; mesh.SetVertices(v); mesh.SetTriangles(t, 0); if (uv != null) mesh.SetUVs(0, uv); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                var go = new GameObject(name); go.transform.SetParent(parent, false); go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = m;
                if (m == glow || m == glowSoft || m == pad) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            string pre = isBranch ? "Shortcut" : "Track";
            Save(pre + "Road", rv, rt, road, false, ruv); Save(pre + "Under", uv2, ut, under, false); Save(pre + "Walls", wv, wt, wall, false);
            Save(pre + "Lights", gv, gt, glow, false); Save(pre + "Dashes", sv, st, glowSoft, false); Save(pre + "Boost", pv, pt, pad, false);
            if (Branch != null && !isBranch) Branch.BuildMeshes(parent, road, under, wall, glow, glowSoft, pad, true);
        }

        /// <summary>Where the shortcut leaves and rejoins, the main line's wall on that side is left open (the branch's road passes
        /// there) and the branch's own walls start / end clear of the main road. (The racers' physics keeps its walls: only the
        /// meshes open.)</summary>
        bool WallShown(int i, float sg, bool isBranch)
        {
            if (isBranch) return i > 18 && i < Count - 40;
            if (Branch == null || Mathf.Sign(sg) != Mathf.Sign(Branch.ForkSide)) return true;
            int a = Idx(Branch.JoinFrom), b = Idx(Branch.JoinTo);
            int da = i - a; if (da < 0) da += Count; int db = b - i; if (db < 0) db += Count;
            return !(da >= 0 && da < 45) && !(db >= 0 && db < 45);
        }

        /// <summary>Gates: a glowing arch every 1/8 of a lap (the start gate brighter, with the light gantry).</summary>
        public List<Transform> BuildGates(Transform parent, Material frame, Material glow, int every = 8)
        {
            var list = new List<Transform>();
            // the lap gates + an arch over each jump's landing (where to aim while in the air)
            var at = new List<(int i, string name)>();
            for (int g = 0; g < every; g++)
            {
                float s = Length * g / every; int i = Idx(s);
                if ((Flag[i] & (F.Gap | F.Loop | F.Cork)) != 0) { for (int k = 0; k < 60 && (Flag[i] & (F.Gap | F.Loop | F.Cork)) != 0; k++) i = (i + 1) % Count; }
                at.Add((i, g == 0 ? "StartGate" : "Gate" + g));
            }
            for (int i = 1; i < Count; i++) if ((Flag[i - 1] & F.Gap) != 0 && (Flag[i] & F.Gap) == 0) at.Add((Mathf.Min(i + 6, Count - 1), "LandingGate"));
            for (int gi = 0; gi < at.Count; gi++)
            {
                int i = at[gi].i; int g = gi < every ? gi : 1;
                var root = new GameObject(at[gi].name).transform; root.SetParent(parent, false); root.SetPositionAndRotation(P[i], R[i]);
                float w = HalfW[i] + 0.6f, h = g == 0 ? 7.5f : 6f;
                var v = new List<Vector3>(); var t = new List<int>(); var gv = new List<Vector3>(); var gt = new List<int>();
                void Box(List<Vector3> vv, List<int> tt, Vector3 c, Vector3 s2) { GcMesh.Box(vv, tt, c, s2); }
                Box(v, t, new Vector3(-w, h * 0.5f, 0f), new Vector3(0.6f, h, 0.6f)); Box(v, t, new Vector3(w, h * 0.5f, 0f), new Vector3(0.6f, h, 0.6f)); Box(v, t, new Vector3(0f, h, 0f), new Vector3(2f * w + 0.6f, 0.7f, 0.6f));
                Box(gv, gt, new Vector3(0f, h - 0.45f, -0.32f), new Vector3(2f * w - 0.6f, 0.14f, 0.04f));
                Box(gv, gt, new Vector3(-w + 0.32f, h * 0.5f, -0.32f), new Vector3(0.1f, h - 1f, 0.04f)); Box(gv, gt, new Vector3(w - 0.32f, h * 0.5f, -0.32f), new Vector3(0.1f, h - 1f, 0.04f));
                GcMesh.Save("Frame", v, t, frame, root); GcMesh.Save("Glow", gv, gt, glow, root);
                list.Add(root);
            }
            return list;
        }
    }

    /// <summary>Small mesh helpers for the racing mode (runtime-built pieces).</summary>
    public static class GcMesh
    {
        public static void Box(List<Vector3> v, List<int> t, Vector3 c, Vector3 s, Quaternion? r = null)
        {
            var q = r ?? Quaternion.identity; var h = s * 0.5f;
            Vector3 P(float x, float y, float z) => c + q * new Vector3(x * h.x, y * h.y, z * h.z);
            void F(Vector3 a, Vector3 b, Vector3 cc, Vector3 d) { int k = v.Count; v.Add(a); v.Add(b); v.Add(cc); v.Add(d); t.Add(k); t.Add(k + 1); t.Add(k + 2); t.Add(k); t.Add(k + 2); t.Add(k + 3); }
            F(P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1)); F(P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), P(-1, -1, 1));
            F(P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), P(-1, -1, -1)); F(P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1));
            F(P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1)); F(P(-1, -1, 1), P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1));
        }
        public static GameObject Save(string name, List<Vector3> v, List<int> t, Material m, Transform parent, bool shadows = true)
        {
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = m;
            if (!shadows) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }
    }
}
