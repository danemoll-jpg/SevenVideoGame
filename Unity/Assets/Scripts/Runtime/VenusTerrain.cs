using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF10 item 6f (Docs/VENUS-TERRAIN.md): Venus's ground is land, not an elevated path. One height function, shared by
    /// the scene builder (the sculpted ground mesh, grass, trees) and the game (the "Valley" ground zone: 7, the bike, the
    /// Elarim walk on exactly this surface):
    ///  - the route is a path worn along a curved centreline (<see cref="Route"/>), following the contours - it dips off the
    ///    landing rise into the valley and rolls gently further on - inside a valley floor <see cref="Floor"/> m either side;
    ///  - beyond the floor the land rises into rolling hills with soft natural shelves (the garden terraces), fading into the
    ///    fog; around the landing platform it falls away (the platform sits on a rise); to the east it sinks into the basin;
    ///  - a stream runs beside the path and has undercut it at the collapse: a gully (z 118-128) cuts across the whole
    ///    valley - the fallen slab bridges it;
    ///  - the architecture zones (court, temple, platform) keep their own floors; the natural ones (the maintenance path,
    ///    the observation ledge, the garden-wall shelf) are this ground at their exact heights.
    /// Walkable (<see cref="Walkable"/>): the valley floor minus the gully, the slab's bed and the stream. Natural
    /// boundaries: the hills, the gully, the water, the fog. Heights are precomputed on a 0.5 m grid (bilinear).
    /// </summary>
    public static class VenusTerrain
    {
        // ---- the route (centreline control points, y ignored), platform -> court
        public static readonly Vector3[] RouteCtrl =
        {
            new Vector3(0f, 0f, 6f), new Vector3(0.5f, 0f, 20f), new Vector3(4f, 0f, 34f), new Vector3(11f, 0f, 46f), new Vector3(21f, 0f, 57f),
            new Vector3(28.5f, 0f, 71f), new Vector3(30f, 0f, 88f), new Vector3(30f, 0f, 100f), new Vector3(30f, 0f, 112f), new Vector3(30f, 0f, 130f),
            new Vector3(30f, 0f, 140f), new Vector3(28f, 0f, 154f), new Vector3(28.5f, 0f, 168f), new Vector3(31.5f, 0f, 181f), new Vector3(33f, 0f, 194f),
        };
        public const float Floor = 9f;            // walkable valley floor either side of the centreline
        public const float PathHalf = 3.0f;       // the worn path itself
        public const float GullyZ0 = 118f, GullyZ1 = 128f, GullyDepth = -6.5f;
        public const float StreamLat = 5.0f;      // the stream: east of the path, until it turns into the gully
        public static readonly Rect GridArea = new Rect(-45f, -25f, 140f, 245f);
        const float Cell = 0.5f;

        static List<Vector3> _route; static float[] _s;
        public static IList<Vector3> Route { get { Ensure(); return _route; } }
        public static float RouteLength { get { Ensure(); return _s[_s.Length - 1]; } }

        static void Ensure()
        {
            if (_route != null) return;
            _route = new List<Vector3>();
            for (int i = 0; i < RouteCtrl.Length - 1; i++)
            {
                var p0 = RouteCtrl[Mathf.Max(0, i - 1)]; var p1 = RouteCtrl[i]; var p2 = RouteCtrl[i + 1]; var p3 = RouteCtrl[Mathf.Min(RouteCtrl.Length - 1, i + 2)];
                for (int k = 0; k < 10; k++)
                {
                    float t = k / 10f, t2 = t * t, t3 = t2 * t;
                    _route.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            _route.Add(RouteCtrl[RouteCtrl.Length - 1]);
            _s = new float[_route.Count];
            for (int i = 1; i < _route.Count; i++) _s[i] = _s[i - 1] + Vector2.Distance(new Vector2(_route[i].x, _route[i].z), new Vector2(_route[i - 1].x, _route[i - 1].z));
        }

        /// <summary>Nearest point on the route: distance (flat), arc length, signed lateral offset (+ = east / right of travel), z of that point.</summary>
        public static void OnRoute(float x, float z, out float dist, out float s, out float lat, out float rz)
        {
            Ensure();
            dist = float.MaxValue; s = 0f; lat = 0f; rz = z;
            var p = new Vector2(x, z);
            for (int i = 0; i < _route.Count - 1; i++)
            {
                var a = new Vector2(_route[i].x, _route[i].z); var b = new Vector2(_route[i + 1].x, _route[i + 1].z);
                if (Mathf.Min(a.y, b.y) - 60f > z || Mathf.Max(a.y, b.y) + 60f < z) continue;
                var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                var c = a + ab * t; float d = (p - c).magnitude;
                if (d < dist)
                {
                    dist = d; s = Mathf.Lerp(_s[i], _s[i + 1], t); rz = c.y;
                    var dir = ab.normalized; lat = dir.y * (p.x - c.x) - dir.x * (p.y - c.y);   // right of travel positive
                }
            }
        }

        /// <summary>The path's own height (it dips off the landing rise into the valley, then rolls gently).</summary>
        public static float PathHeight(float rz, float s)
        {
            float dip = -2.6f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(16f, 30f, rz)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(52f, 74f, rz)));
            float roll = 0.7f * Mathf.Sin(s / 11f) * Win(rz, 64f, 70f, 84f, 90f) + 0.6f * Mathf.Sin(s / 9f + 1f) * Win(rz, 140f, 146f, 172f, 178f);
            return dip + roll;
        }
        static float Win(float v, float a, float b, float c, float d) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, v)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(c, d, v)));

        // ---- features: flat ground at a known height (exact inside)
        struct Feat { public float X0, Z0, X1, Z1, Y0, Y1; public int Axis; public bool Built; }
        static Feat F(float x0, float z0, float x1, float z1, float y0, bool built, float y1 = float.NaN, int axis = 1) => new Feat { X0 = x0, Z0 = z0, X1 = x1, Z1 = z1, Y0 = y0, Y1 = float.IsNaN(y1) ? y0 : y1, Axis = axis, Built = built };
        static readonly Feat[] Feats =
        {
            F(14f, 190f, 46f, 214f, 0f, true),            // the court (paving)
            F(25f, 212f, 35f, 237f, 0f, true),            // the temple walk
            F(21f, 235f, 39f, 263f, 0f, true),            // the hall
            F(24f, 262f, 36f, 275f, 0f, true),            // the cube chamber
            F(44f, 195f, 80f, 203f, 0f, false),           // the maintenance path (worn ground)
            F(73f, 150f, 80f, 197f, -6f, false, 0f),      // down the hillside
            F(64f, 134f, 88f, 152f, -6f, false),          // the observation ledge
            F(15f, 104f, 23.4f, 116f, 1.0f, false),       // the garden-wall shelf (a rock shelf beside the stopping area)
        };
        static float FeatH(in Feat f, float x, float z)
        {
            float u = f.Axis == 0 ? Mathf.InverseLerp(f.X0, f.X1, x) : Mathf.InverseLerp(f.Z0, f.Z1, z);
            return Mathf.Lerp(f.Y0, f.Y1, u);
        }
        static float FeatD(in Feat f, float x, float z) { float dx = Mathf.Max(f.X0 - x, 0f, x - f.X1), dz = Mathf.Max(f.Z0 - z, 0f, z - f.Z1); return Mathf.Sqrt(dx * dx + dz * dz); }

        // ---- noise
        static float Hash(int x, int y) { unchecked { int h = x * 374761393 + y * 668265263; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffffff) / 16777215f; } }
        public static float Noise(float x, float y)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y); float fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            return Mathf.Lerp(Mathf.Lerp(Hash(ix, iy), Hash(ix + 1, iy), fx), Mathf.Lerp(Hash(ix, iy + 1), Hash(ix + 1, iy + 1), fx), fy);
        }
        public static float Fbm(float x, float y) => Noise(x, y) * 0.55f + Noise(x * 2.1f + 7f, y * 2.1f + 3f) * 0.3f + Noise(x * 4.3f + 1f, y * 4.3f + 9f) * 0.15f;

        /// <summary>Soft natural shelves: the hillside rises in steps (garden terraces grown into the slope).</summary>
        static float Shelves(float h, float step)
        {
            if (h <= 0f) return h;
            float q = h / step, f = q - Mathf.Floor(q);
            return (Mathf.Floor(q) + Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, f))) * step;
        }

        /// <summary>The ground height (analytic). Inside a built feature it lies 5 cm under that floor (the floor mesh shows).</summary>
        public static float RawHeight(float x, float z)
        {
            OnRoute(x, z, out float dP, out float s, out float lat, out float rz);
            float hp = PathHeight(rz, s);
            // exact inside a feature (built floors: just under them)
            foreach (var f in Feats) if (x >= f.X0 && x <= f.X1 && z >= f.Z0 && z <= f.Z1) return FeatH(f, x, z) - (f.Built ? 0.05f : 0f);
            float r0 = Mathf.Sqrt(x * x + z * z);
            if (r0 < 14.6f) return 0.01f;                                          // the landing platform's disc (walk height; the mesh stays under it)
            float h; float dmin;
            {
                // blend of the nearby flat grounds (inverse distance), then the hills rising from them
                float dC = Mathf.Max(0f, dP - Floor), dPl = Mathf.Max(0f, r0 - 14.6f);
                float w = 1f / Mathf.Pow(dC + 1f, 3f), sum = hp * w, ws = w; dmin = dC;
                float wp = 1f / Mathf.Pow(dPl + 1f, 3f); sum += 0f * wp; ws += wp; dmin = Mathf.Min(dmin, dPl);
                foreach (var f in Feats) { float d = FeatD(f, x, z); float wf = 1f / Mathf.Pow(d + 1f, 3f); sum += FeatH(f, Mathf.Clamp(x, f.X0, f.X1), Mathf.Clamp(z, f.Z0, f.Z1)) * wf; ws += wf; dmin = Mathf.Min(dmin, d); }
                float L = sum / ws;
                if (dmin <= 0f) h = dP <= Floor ? hp : L;
                else
                {
                    float hills = 9f + 10f * Fbm(x * 0.018f, z * 0.018f);
                    float rise = Shelves(Mathf.Pow(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(dmin / 18f)), 1.2f) * hills, 2.3f);
                    rise += 0.35f * (Noise(x * 0.25f, z * 0.25f) - 0.5f) * Mathf.SmoothStep(0f, 1f, dmin / 3f);
                    // the landing platform sits on a rise: away from the path the land falls away around it
                    float pf = (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(32f, 58f, r0))) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5f, 16f, dP));
                    float fall = -Mathf.Min(13f, dPl * 0.34f) + 0.6f * (Fbm(x * 0.05f, z * 0.05f) - 0.5f);
                    h = L + Mathf.Lerp(rise, fall, pf);
                }
            }
            // the basin to the east: the land sinks to the creatures' lowland (the observation ledge overlooks it)
            float b = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(86f, 99f, x)) * Win(z, 88f, 100f, 188f, 205f);
            if (b > 0f) h = Mathf.Lerp(h, -12f + 1.4f * (Fbm(x * 0.06f, z * 0.06f) - 0.5f), b);
            // the stream beside the path, east of it, flowing on into the gully
            float sf = Win(rz, 22f, 30f, 108f, 117.5f);
            if (sf > 0f) h -= 0.55f * sf * Mathf.Exp(-Mathf.Pow((lat - StreamLat) / 0.9f, 2f));
            // the gully: the stream has undercut the path - a break in the land across the whole valley
            float edge = Mathf.Abs(x - 30f) < 6f ? 0f : 0.6f * (Noise(x * 0.3f, 3.1f) - 0.5f);
            float g = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(GullyZ0 - 0.6f + edge, GullyZ0 + 0.7f + edge, z)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(GullyZ1 - 0.7f - edge, GullyZ1 + 0.6f - edge, z)));
            if (g > 0f) h = Mathf.Lerp(h, Mathf.Min(h, GullyDepth + 0.8f * Noise(x * 0.2f, z * 0.2f)), g);
            return h;
        }

        /// <summary>Is the point on the walkable valley floor (not the gully, the slab's bed, the stream, the shelf)?</summary>
        public static bool RawWalkable(float x, float z)
        {
            OnRoute(x, z, out float dP, out _, out float lat, out float rz);
            if (dP > Floor + 0.3f) return false;
            if (z > 113.9f && z < GullyZ1 + 0.25f) return false;                       // the slab's bed + the gully
            if (Win(rz, 22f, 30f, 108f, 117.5f) > 0.3f && Mathf.Abs(lat - StreamLat) < 1.0f) return false;   // the stream
            if (x >= 15f && x <= 23.4f && z >= 104f && z <= 116f) return false;      // the shelf (its own zone, a step up)
            return true;
        }

        // ---- the precomputed grid
        static float[] _h; static bool[] _w; static int _nx, _nz;
        public static void Build()
        {
            if (_h != null) return;
            _nx = Mathf.CeilToInt(GridArea.width / Cell) + 1; _nz = Mathf.CeilToInt(GridArea.height / Cell) + 1;
            _h = new float[_nx * _nz]; _w = new bool[_nx * _nz];
            for (int j = 0; j < _nz; j++)
                for (int i = 0; i < _nx; i++)
                {
                    float x = GridArea.xMin + i * Cell, z = GridArea.yMin + j * Cell;
                    _h[j * _nx + i] = RawHeight(x, z); _w[j * _nx + i] = RawWalkable(x, z);
                }
        }

        public static float Height(float x, float z)
        {
            Build();
            float fx = (x - GridArea.xMin) / Cell, fz = (z - GridArea.yMin) / Cell;
            if (fx < 0f || fz < 0f || fx > _nx - 1.001f || fz > _nz - 1.001f) return RawHeight(x, z);
            int i = (int)fx, j = (int)fz; float u = fx - i, v = fz - j;
            float a = _h[j * _nx + i], b = _h[j * _nx + i + 1], c = _h[(j + 1) * _nx + i], d = _h[(j + 1) * _nx + i + 1];
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        public static bool Walkable(float x, float z)
        {
            Build();
            int i = Mathf.RoundToInt((x - GridArea.xMin) / Cell), j = Mathf.RoundToInt((z - GridArea.yMin) / Cell);
            if (i < 0 || j < 0 || i >= _nx || j >= _nz) return false;
            return _w[j * _nx + i];
        }

        /// <summary>Points along the route every <paramref name="step"/> m (the autopilot, lamps, arches).</summary>
        public static List<Vector3> Along(float from, float to, float step)
        {
            Ensure(); var l = new List<Vector3>();
            for (float s = from; s <= Mathf.Min(to, RouteLength); s += step) l.Add(AtS(s));
            return l;
        }
        public static Vector3 AtS(float s)
        {
            Ensure();
            for (int i = 0; i < _s.Length - 1; i++)
                if (s <= _s[i + 1]) { float t = Mathf.InverseLerp(_s[i], _s[i + 1], s); var p = Vector3.Lerp(_route[i], _route[i + 1], t); p.y = PathHeight(p.z, s); return p; }
            var e = _route[_route.Count - 1]; e.y = 0f; return e;
        }
        public static Vector3 DirAtS(float s) { var a = AtS(s - 0.5f); var b = AtS(s + 0.5f); var d = b - a; d.y = 0f; return d.normalized; }
    }
}
