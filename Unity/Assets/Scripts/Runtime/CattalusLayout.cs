using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 18 — Chapter "Cattalus — Panther's homeworld" (Docs/CHAPTER_CATTALUS.md): the numbers the builder and the game share.
    /// The play floor is flat (y 0):
    ///  - the public berth (the saucer hovers at the origin; the docking arms stand clear of the bike hold and the beam);
    ///  - the public gate north of the berth (CAT_02's door joke), then the avenue through the cardboard quarter (CAT_03:
    ///    Juniper; the seven vignettes along it and round the square), the transit tower with its hanging ball to the east;
    ///  - the Cat-a-tron square (CAT_04: the screen on its north side facing south; the steward's desk, the trophy display, the
    ///    Deluxe Retreat; the quiet corner by the planters for CAT_09); the race's start / finish straight along its east side;
    ///  - west of the square, the meadow path → the meadow (CAT_10: the leaning tree with its long low branch, low hills);
    ///  - the Grand Circuit (CAT_07): one closed loop east of the city through six sections - the cardboard quarter, the
    ///    scratching-post ascent (a 450° spiral round a monumental column), the cat-tree heights (platforms high over the city),
    ///    the toy boulevard (raised, swinging toys on a fixed rhythm), the garden descent, the finish straight past the square.
    /// Walkable / ridable areas are rectangles (Ground zones, overlapping ≥ 0.6 m); the course is its own chain of zones (a
    /// mask + a height field per 60 m piece) switched on only round the racer, so the spiral can pass over itself.
    /// </summary>
    public static class CattalusLayout
    {
        public const float SaucerHover = 3.35f;
        public static readonly Vector3 SaucerAt = Vector3.zero;
        public static readonly Vector3 BikePad = new Vector3(9f, 0f, 6f);
        // CAT_02: the public gate (a cat waits for the attendant), the box Panther inspects
        public static readonly Vector3 GateAt = new Vector3(0f, 0f, 26f);
        public static readonly Vector3 GateCatAt = new Vector3(0.6f, 0f, 23.6f);
        public static readonly Vector3 AttendantAt = new Vector3(-2.4f, 0f, 27.2f);
        public static readonly Vector3 SmallBoxAt = new Vector3(-9.5f, 0f, 46f);
        // CAT_03: Juniper meets them on the avenue
        public static readonly Vector3 JuniperMeet = new Vector3(3f, 0f, 74f);
        public static readonly Vector3 QuietStep = new Vector3(-7.4f, 0f, 84f);       // the low step where they talk after
        // CAT_04 / 05 / 06: the square
        public static readonly Vector3 SquareC = new Vector3(0f, 0f, 145f);
        public const float SquareHalf = 34f;
        public static readonly Vector3 ScreenAt = new Vector3(0f, 13f, 178f);       // the Cat-a-tron's centre (faces south)
        public const float ScreenW = 30f, ScreenH = 16.875f;
        public static readonly Vector3 ScreenView = new Vector3(0f, 0f, 150f);
        public static readonly Vector3 TrophyAt = new Vector3(16f, 0f, 162f);        // the prize display (trophy + retreat)
        public static readonly Vector3 RetreatAt = new Vector3(20.5f, 0f, 162.5f);
        public static readonly Vector3 DeskAt = new Vector3(-16f, 0f, 160f);         // the steward's registration desk
        public static readonly Vector3 DeskView = new Vector3(-16f, 0f, 156.4f);
        public static readonly Vector3 StewardAt = new Vector3(-16f, 0f, 162.2f);
        public static readonly Vector3 DuchessWait = new Vector3(19f, 0f, 158.4f);   // Duchess at the prize display
        public static readonly Vector3 BikeCheck = new Vector3(43.4f, 0f, 154f);        // CAT_06: she checks the bike here
        public static readonly Vector3 LowWall = new Vector3(41.5f, 0f, 164f);        // Panther's low wall beside the start
        public const float LowWallH = 0.62f;
        public static readonly Vector3 QuietCorner = new Vector3(-26f, 0f, 128f);     // CAT_09 by the planters
        /// <summary>CAT_09: the trophy stands on the near rim of the first planter (0.95 m in from its edge toward her side).</summary>
        public static readonly Vector3 TrophyLedge = QuietCorner + new Vector3(0.9f, 0.62f, 1.2f - 0.95f);
        public static readonly Vector3 PodiumAt = new Vector3(6f, 0f, 168f);          // CAT_08: the presentation
        // CAT_10: the meadow (west)
        public static readonly Vector3 MeadowGate = new Vector3(-48f, 0f, 150f);
        public static readonly Vector3 MeadowEdge = new Vector3(-118f, 0f, 150f);     // the city path opens onto the meadow
        public static readonly Vector3 TreeAt = new Vector3(-212f, 0f, 168f);         // the leaning tree (leans toward -x, the low branch toward +z)
        public static readonly Vector3 TreeView = new Vector3(-158f, 0f, 152f);       // CAT_10_01 "That's it."
        public const float MeadowX0 = -330f, MeadowX1 = -112f, MeadowZ0 = 40f, MeadowZ1 = 280f;
        // the old transit tower with its hanging ball (CAT_01_04)
        public static readonly Vector3 TransitTower = new Vector3(-70f, 0f, 64f);
        public const float TransitTowerH = 62f;

        // ---- the walkable / ridable floor (rectangles; Bike = may be ridden)
        public struct ZoneDef { public string Name; public float X0, Z0, X1, Z1; public bool On, Bike; }
        static ZoneDef R(string n, float x0, float z0, float x1, float z1, bool bike = true, bool on = true) => new ZoneDef { Name = n, X0 = x0, Z0 = z0, X1 = x1, Z1 = z1, On = on, Bike = bike };
        public static readonly ZoneDef[] Zones =
        {
            R("Berth", -24f, -24f, 24f, 24.6f),
            R("GateZ", -4f, 24f, 4f, 28.6f, true, false),                   // the public gate (opens for the cat in CAT_02)
            R("Avenue", -14f, 28f, 14f, 118.6f),
            R("Lanes", -21f, 40f, 21f, 110f, false),                        // the cardboard quarter's lanes beside the avenue (on foot)
            R("Square", -34f, 118f, 34f, 174f),
            R("StartApron", 33.4f, 120f, 46f, 176f),                         // between the square and the start straight (the low wall, the grid)
            R("MeadowPath", -118.6f, 144f, -33.4f, 156f),
            R("Meadow", MeadowX0 + 40f, MeadowZ0 + 40f, MeadowX1, MeadowZ1 - 40f, false),   // walkable: the rolling interior (the low hills rise beyond it)
        };
        public static bool IsBikeZone(string name) { foreach (var z in Zones) if (z.Name == name) return z.Bike; return name != null && name.StartsWith("Course"); }
        public static bool BikeZone(Vector3 p, float pad = 0.6f)
        {
            foreach (var z in Ground.Zones) if (z.Enabled && z.Contains(p.x, p.z, pad) && IsBikeZone(z.Name)) return true;
            return false;
        }
        public static string ZoneAt(Vector3 p) => Ground.ZoneName(p.x, p.z) ?? "-";
        /// <summary>The seven vignettes (CAT_02's ambient city jokes): where each loops.</summary>
        public static readonly (string id, Vector3 at, float yaw)[] Vignettes =
        {
            ("mansion_box", new Vector3(-21f, 0f, 58f), 90f),         // a cardboard mansion; its owner sleeps in the tiny ordinary box
            ("fountain_trickle", new Vector3(-20f, 0f, 132f), 0f),    // drinks from the maintenance trickle, not the pristine basin
            ("delivery_carton", new Vector3(18f, 0f, 96f), -90f),     // an empty carton set down, occupied at once
            ("sun_bench", new Vector3(-14f, 0f, 104f), 90f),          // three cats crowd one patch of sun on an empty bench
            ("narrow_ledge", new Vector3(13.5f, 0f, 40f), 0f),        // pedestrians choose the narrow ledge beside the walkway
            ("meeting_ornament", new Vector3(24f, 0f, 128f), -60f),   // a formal meeting pauses for a dangling ornament
            ("counter_push", new Vector3(-17f, 0f, 92f), 90f),        // a paw pushes an object toward a counter's edge
        };
        public static readonly Vector3 FountainAt = new Vector3(-20f, 0f, 136f);
        /// <summary>Carefully occupied patches of sunlight (bright soft pools on the paving; a citizen lies in most of them).</summary>
        public static readonly Vector3[] SunPatches = { new Vector3(8f, 0f, 52f), new Vector3(-9f, 0f, 88f), new Vector3(10f, 0f, 120f), new Vector3(-12f, 0f, 156f), new Vector3(22f, 0f, 150f), new Vector3(-4f, 0f, 30f) };
        /// <summary>The elevated transit route: a loop over the avenue and round the square between the scratching-post columns.</summary>
        public static readonly Vector3[] TransitLoop = { new Vector3(-20f, 8f, 30f), new Vector3(-20f, 8f, 118f), new Vector3(-44f, 8.5f, 150f), new Vector3(-20f, 9f, 186f), new Vector3(20f, 9f, 186f), new Vector3(30f, 8.5f, 150f), new Vector3(20f, 8f, 118f), new Vector3(20f, 8f, 30f), new Vector3(0f, 7.5f, 18f) };

        // ---- routes (the guide light)
        public static readonly Vector3[] WalkToSquare = { new Vector3(0f, 0f, 40f), new Vector3(0f, 0f, 70f), new Vector3(0f, 0f, 110f), ScreenView };
        public static readonly Vector3[] WalkToMeadow = { new Vector3(-30f, 0f, 150f), MeadowGate, new Vector3(-90f, 0f, 150f), TreeView };
        public static readonly Vector3[] WalkHome = { new Vector3(-90f, 0f, 150f), MeadowGate, new Vector3(-10f, 0f, 140f), new Vector3(0f, 0f, 110f), new Vector3(0f, 0f, 40f), new Vector3(0f, 0f, 8f) };

        /// <summary>The meadow's ground (the builder's terrain and the Meadow zone's height field): gently rolling, with low hills
        /// rising round its far edges - the hill line of vision 7.</summary>
        public static float MeadowHeight(float x, float z)
        {
            float h = 0.25f * Mathf.Sin(x * 0.05f) * Mathf.Cos(z * 0.04f) + 0.15f * Mathf.Sin((x + z) * 0.03f);
            float near = Mathf.Clamp01((x - (MeadowX1 - 14f)) / 14f);              // flat where the path comes in
            h *= 1f - near;
            float dx = Mathf.Max(0f, MeadowX0 + 40f - x), dz0 = Mathf.Max(0f, MeadowZ0 + 40f - z), dz1 = Mathf.Max(0f, z - (MeadowZ1 - 40f));
            return h + dx * dx * 0.0012f + dz0 * dz0 * 0.001f + dz1 * dz1 * 0.001f;   // a long gentle rise beyond the walkable meadow
        }
        public const int CpArrival = 0, CpCity = 1, CpScreen = 2, CpRace = 3, CpSphere = 4, CpMeadow = 5, CpHome = 6, CpDone = 7;
        public static readonly string[] CpNames =
        {
            "home, apparently", "a door problem", "the Cat-a-tron", "the Grand Circuit", "the final memory", "the field", "the way back to the Moon", "done",
        };
    }

    /// <summary>
    /// Round 18 — the Grand Circuit: one closed loop (~2.3 km, one lap ≈ 2½ minutes at race pace), shared by the builder
    /// (the road ribbon, rails, gates, the spiral's column, the swinging toys) and the race (Duchess's line, gates, sections,
    /// the course zones). Control points in metres; the scratching-post ascent is an explicit 450° helix round its column.
    /// </summary>
    public static class CattalusCourse
    {
        public const float HalfWidth = 7f, Step = 2f;
        public static readonly Vector3 StartAt = new Vector3(52f, 0f, 150f);          // the start / finish line (heading -z)
        public static readonly Vector3 ColumnC = new Vector3(52f + (440f - 52f) * 1.5f, 0f, 150f);
        public const float ColumnR = 40f, SpiralTurns = 450f, SpiralTop = 28f;
        public static readonly string[] SectionNames = { "the cardboard quarter", "the scratching-post ascent", "the cat-tree heights", "the toy boulevard", "the garden descent", "the finish straight" };

        static Vector3 S(float x, float y, float z) => new Vector3(52f + (x - 52f) * 1.5f, y, 150f + (z - 150f) * 1.5f);
        static List<(Vector3 p, int sec)> Controls()
        {
            var c = new List<(Vector3, int)>
            {
                (S(52f, 0f, 150f), 5), (S(52f, 0f, 112f), 0), (S(62f, 0f, 80f), 0), (S(100f, 0f, 62f), 0), (S(160f, 0f, 82f), 0), (S(220f, 0f, 52f), 0),
                (S(280f, 0f, 86f), 0), (S(340f, 0f, 62f), 0), (S(385f, 0f, 92f), 0),
            };
            // the scratching-post ascent: enter at the column's west point heading north, clockwise 450° up to SpiralTop
            var C = ColumnC;
            c.Add((C + new Vector3(-ColumnR, 0f, -30f), 1));
            for (float a = 0f; a <= SpiralTurns + 0.1f; a += 15f)
            {
                float ang = (180f - a) * Mathf.Deg2Rad, y = SpiralTop * Mathf.SmoothStep(0f, 1f, a / SpiralTurns);
                c.Add((C + new Vector3(Mathf.Cos(ang) * ColumnR, y, Mathf.Sin(ang) * ColumnR), 1));
            }
            var top = C + new Vector3(0f, SpiralTop, ColumnR);
            c.AddRange(new (Vector3, int)[]
            {
                (top + new Vector3(34f, 0f, 6f), 2), (S(480f, SpiralTop, 236f), 2), (S(470f, SpiralTop - 1f, 300f), 2), (S(420f, SpiralTop - 2f, 360f), 2),
                (S(360f, SpiralTop - 4f, 400f), 2), (S(300f, SpiralTop - 6f, 440f), 2),
                (S(262f, 19f, 462f), 3), (S(230f, 16f, 470f), 3), (S(190f, 16f, 470f), 3), (S(150f, 16f, 470f), 3), (S(112f, 16f, 470f), 3),
                (S(72f, 14f, 452f), 4), (S(100f, 10f, 392f), 4), (S(62f, 7f, 332f), 4), (S(96f, 4f, 272f), 4), (S(62f, 1f, 216f), 4), (S(52f, 0f, 186f), 5),
            });
            return c;
        }

        static List<Vector3> _pts; static List<float> _s; static List<int> _sec; static float _len;
        /// <summary>The loop sampled every ~2 m (closed: the last sample joins the first).</summary>
        public static List<Vector3> Points { get { Build(); return _pts; } }
        public static List<float> Dist { get { Build(); return _s; } }
        public static List<int> Section { get { Build(); return _sec; } }
        public static float Length { get { Build(); return _len; } }
        static void Build()
        {
            if (_pts != null) return;
            var c = Controls(); int n = c.Count;
            var dense = new List<(Vector3, int)>();
            for (int i = 0; i < n; i++)
            {
                Vector3 p0 = c[(i - 1 + n) % n].Item1, p1 = c[i].Item1, p2 = c[(i + 1) % n].Item1, p3 = c[(i + 2) % n].Item1;
                int k = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(p1, p2) / 0.5f));
                for (int j = 0; j < k; j++) dense.Add((CatmullRom(p0, p1, p2, p3, j / (float)k), c[i].Item2));
            }
            // resample at Step
            _pts = new List<Vector3>(); _s = new List<float>(); _sec = new List<int>();
            float acc = 0f, next = 0f; _pts.Add(dense[0].Item1); _s.Add(0f); _sec.Add(dense[0].Item2);
            for (int i = 1; i <= dense.Count; i++)
            {
                var a = dense[i - 1].Item1; var b = dense[i % dense.Count].Item1; float d = Vector3.Distance(a, b);
                while (next + Step <= acc + d)
                {
                    next += Step; float u = (next - acc) / Mathf.Max(d, 1e-4f);
                    if (next < 0.5f) continue;
                    _pts.Add(Vector3.Lerp(a, b, u)); _s.Add(next); _sec.Add(dense[i - 1].Item2);
                }
                acc += d;
            }
            _len = acc;
            if (_len - _s[_s.Count - 1] < Step * 0.5f) { _pts.RemoveAt(_pts.Count - 1); _s.RemoveAt(_s.Count - 1); _sec.RemoveAt(_sec.Count - 1); }
        }
        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
        public static int Count => Points.Count;
        public static Vector3 At(int i) => Points[((i % Count) + Count) % Count];
        public static Vector3 Dir(int i) { var d = At(i + 1) - At(i - 1); d.y = 0f; return d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.forward; }
        public static Vector3 AtDist(float s) { s = Mathf.Repeat(s, Length); int i = Mathf.Clamp(Mathf.FloorToInt(s / Step), 0, Count - 1); float u = (s - Dist[i]) / Step; return Vector3.Lerp(At(i), At(i + 1), Mathf.Clamp01(u)); }
        public static int IndexAtDist(float s) { s = Mathf.Repeat(s, Length); return Mathf.Clamp(Mathf.FloorToInt(s / Step), 0, Count - 1); }
        /// <summary>The nearest sample to p within ±window samples of a hint index (progress never jumps across the spiral's turns).</summary>
        public static int Nearest(Vector3 p, int hint, int window = 40)
        {
            int best = hint; float bd = float.MaxValue;
            for (int k = -window; k <= window; k++) { int i = ((hint + k) % Count + Count) % Count; float d = (At(i) - p).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
            return best;
        }
        /// <summary>Curvature-limited speed at sample i (m/s): the racing line's pace there (Duchess's AI, the pilot).</summary>
        public static float CornerSpeed(int i, float vmax)
        {
            float turn = 0f;
            for (int k = 0; k < 12; k++) turn = Mathf.Max(turn, Vector3.Angle(Dir(i + k), Dir(i + k + 4)));   // degrees over 8 m, looking 24 m ahead
            float lat = 9.5f;                                                   // lateral grip (m/s²)
            float r = turn < 0.5f ? 9999f : 8f / (turn * Mathf.Deg2Rad);
            return Mathf.Min(vmax, Mathf.Sqrt(lat * r));
        }
        /// <summary>The gates: every ~140 m (the start line first), each on a sample index.</summary>
        public static List<int> Gates()
        {
            var g = new List<int>(); int every = Mathf.RoundToInt(140f / Step);
            for (int i = 0; i < Count - every / 2; i += every) g.Add(i);
            return g;
        }
        /// <summary>The swinging toys of the toy boulevard (section 3): sample indices, period, phase.</summary>
        public static List<(int i, float period, float phase)> Toys()
        {
            var l = new List<(int, float, float)>(); int first = -1, last = -1;
            for (int i = 0; i < Count; i++) if (Section[i] == 3) { if (first < 0) first = i; last = i; }
            if (first < 0) return l;
            int n = 5; for (int k = 0; k < n; k++) { int i = first + (int)((last - first) * (k + 0.8f) / (n + 0.6f)); l.Add((i, 3.2f, k * 0.37f)); }
            return l;
        }
        /// <summary>A toy's swing (−1..1 across the road) at time t: a fixed, readable pendulum rhythm.</summary>
        public static float ToySwing(float t, float period, float phase) => Mathf.Sin((t / period + phase) * Mathf.PI * 2f);

        // ---- the course zones (one per ~60 m piece; only the pieces round the racer are on, so the spiral can pass over itself)
        public static readonly List<Ground.Zone> Pieces = new List<Ground.Zone>();
        static readonly List<(int a, int b)> _pieceRange = new List<(int, int)>();
        public static void AddZones()
        {
            Pieces.Clear(); _pieceRange.Clear();
            int per = Mathf.RoundToInt(60f / Step);
            for (int a = 0; a < Count; a += per)
            {
                int b = Mathf.Min(Count, a + per + 2);
                Vector2 mn = new Vector2(float.MaxValue, float.MaxValue), mx = new Vector2(float.MinValue, float.MinValue);
                for (int i = a - 1; i <= b; i++) { var p = At(i); mn = Vector2.Min(mn, new Vector2(p.x, p.z)); mx = Vector2.Max(mx, new Vector2(p.x, p.z)); }
                mn -= Vector2.one * (HalfWidth + 0.5f); mx += Vector2.one * (HalfWidth + 0.5f);
                int ia = a, ib = b;
                var z = new Ground.Zone { Name = "Course" + Pieces.Count, Min = mn, Max = mx, Enabled = false };
                z.Mask = (x, zz) => DistIn(ia, ib, x, zz, out _) <= HalfWidth;
                z.Field = (x, zz) => { DistIn(ia, ib, x, zz, out float y); return y; };
                Pieces.Add(z); _pieceRange.Add((a, b));
                Ground.Zones.Insert(0, z);                                       // first: the course wins over the flat city where they meet
            }
        }
        static float DistIn(int a, int b, float x, float z, out float y)
        {
            float best = float.MaxValue; y = 0f; var q = new Vector2(x, z);
            for (int i = a - 1; i < b; i++)
            {
                var p0 = At(i); var p1 = At(i + 1); var A = new Vector2(p0.x, p0.z); var B = new Vector2(p1.x, p1.z);
                var ab = B - A; float u = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(q - A, ab) / ab.sqrMagnitude) : 0f;
                float d = (A + ab * u - q).sqrMagnitude;
                if (d < best) { best = d; y = Mathf.Lerp(p0.y, p1.y, u); }
            }
            return Mathf.Sqrt(best);
        }
        /// <summary>Switch on the pieces round sample index i (−1 = all off).</summary>
        public static void Around(int i)
        {
            for (int k = 0; k < Pieces.Count; k++)
            {
                bool on = false;
                if (i >= 0) { var (a, b) = _pieceRange[k]; int span = Count; int d0 = Mod(a - i, span), d1 = Mod(i - b, span); on = (i >= a - 1 && i <= b + 1) || d0 < 40 || d1 < 40; }
                Pieces[k].Enabled = on;
            }
        }
        static int Mod(int x, int m) => ((x % m) + m) % m;
    }
}
