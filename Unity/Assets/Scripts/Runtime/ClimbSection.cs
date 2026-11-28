using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using T = Seven.MoonTraversal;

namespace Seven
{
    /// <summary>
    /// QF40 (3): Chapter 1's climbing moves used beyond the Moon. One shared climbing section, set into Venus, Mars, the asteroid,
    /// the mothership and the finale (not Europa - under water - nor Cattalus): a weathered two-tier ruin / machine block in that
    /// world's own material, on foot, with a story reason (reaching a view the cat points out), real play and a payoff (the view +
    /// his line). The route, in Chapter 1's moves and clips:
    ///   1. a jump up to the carved cornice (a hang - the same lip height as Chapter 1's bulkhead grab);
    ///   2. shimmy along the cornice under the tall block (the top is out of reach there) to the low tier;
    ///   3. climb up onto the low tier; 4. climb up the waist-high step onto the top (mantle) - the view;
    ///   5. down: climb down into a hang over the back edge and let go (or hang down off the low tier's front).
    /// Nothing is one-way: from every tier she can get down, a fall lands her at the foot of the block (the start of the section).
    /// The handholds read on sight: thin white chalk-glow strips along every lip she can use (Kyle's white glow).
    /// Placement is found at load near the chapter's seed (flat walkable ground, clear of every mesh and >= 6 m from every place the
    /// chapter's story, NPCs or bots use: all the points of its Layout class, its routes' segments), so it never blocks the story.
    /// The block's meshes carry colliders and exist before the camera's occluder copies are built (sceneLoaded), so the follow
    /// camera, its pull-in / swing and her foot collision all know it. Check run: <c>-climbtest</c> (with a chapter start switch)
    /// drives the section twice - a precise bot and a bot with human timing errors (late / early presses, heading errors,
    /// shimmy overshoot) - and logs CLIMB TEST (QF40 3) per run: every step, retries, stuck hangs, the frames 7 was hidden from the
    /// player's camera (must be 0), captures from the player's camera.
    /// </summary>
    public class ClimbSection : MonoBehaviour
    {
        public class Spec
        {
            public string Scene, Layout, Theme, LineA, LineB, IdA, IdB, Speaker = "cat";
            public Vector3 Seed, View; public float Radius = 30f;
        }
        public static readonly Spec[] Specs =
        {
            new Spec { Scene = "Venus", Layout = "Seven.VenusLayout", Theme = "the overgrown temple ruin", Seed = new Vector3(46f, 0f, 208f), View = new Vector3(30f, 6f, 236f), Radius = 34f,
                IdA = "CLB_VEN_A", LineA = "That old ruin by the court still has its carvings. Climb it - you'll see the whole temple from the top.", IdB = "CLB_VEN_B", LineB = "Nice reach. The temple looks closer from up there." },
            new Spec { Scene = "Mars", Layout = "Seven.MarsLayout", Theme = "the fallen fortress stones", Seed = new Vector3(-84f, 0f, 16f), View = new Vector3(0f, 6f, -40f), Radius = 34f,
                IdA = "CLB_MAR_A", LineA = "Those fallen fortress stones make a stair, if you don't mind using your hands. You'd see the ravine from the top.", IdB = "CLB_MAR_B", LineB = "Nice reach. You can see both kingdoms from there." },
            new Spec { Scene = "Asteroid", Layout = "Seven.AsteroidLayout", Theme = "the stalled machine housing", Seed = new Vector3(-298f, 0f, 122f), View = new Vector3(-319f, 13.5f, 104f), Radius = 34f,
                IdA = "CLB_AST_A", LineA = "That stalled machine housing has handholds all the way up. The view of the hall should be worth it.", IdB = "CLB_AST_B", LineB = "Nice reach. The whole foundry, laid out like clockwork." },
            new Spec { Scene = "Biaveh", Layout = "Seven.BiavehLayout", Theme = "the curving relay plinth", Seed = new Vector3(-6f, 0f, 18f), View = new Vector3(-60f, 30f, 0f), Radius = 60f,
                IdA = "CLB_BIA_A", LineA = "The architecture by the berth curves up like it wants to be climbed. You'd see the city rise from the top.", IdB = "CLB_BIA_B", LineB = "Nice reach. Even the Biavians don't go up there." },
            new Spec { Scene = "Finale", Layout = "Seven.FinaleLayout", Theme = "the old route's retaining wall", Seed = new Vector3(4f, -10f, -100f), View = new Vector3(16f, -150f, -700f), Radius = 40f,
                IdA = "CLB_FIN_A", LineA = "The old route is above that wall. After you.", IdB = "CLB_FIN_B", LineB = "Nice reach." },
        };

        // local frame: front face at z = 0 facing -z (she approaches from -z); Fwd (toward the wall) = +z
        const float AX0 = -1.6f, AX1 = 2.2f, BX1 = 4.8f, D = 2.6f, HA = 3.3f, HB = 2.4f;   // HB = the bulkhead grab's lip; HA - HB = a waist-high step

        public Spec S; public Vector3 O; public bool Placed; public string Why;
        public T.Ledge Cornice, BackLip; public T.JumpGrab Climb; public T.Mantle Step; public T.HangDown DownBack, DownFront;
        public Ground.Zone TopLow, TopHigh;
        bool _saidA, _saidB; ChapterSeven _seven;
        public static ClimbSection Current;
        public static int Installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded;
            OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
        static void OnLoaded(Scene sc, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            var spec = Specs.FirstOrDefault(x => x.Scene == sc.name);
            if (spec == null || Current != null && Current.gameObject.scene == sc) return;
            var go = new GameObject("QF40_ClimbSection_" + sc.name);
            SceneManager.MoveGameObjectToScene(go, sc);
            var cs = go.AddComponent<ClimbSection>(); cs.S = spec; Current = cs;
            cs.PlaceAndBuild();
        }

        // ------------------------------------------------------------------------------------------ placement
        void PlaceAndBuild()
        {
            var avoid = new List<Vector3>(); var segs = new List<(Vector3, Vector3)>();
            var lt = System.Type.GetType(S.Layout);
            if (lt != null)
                foreach (var f in lt.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                {
                    try
                    {
                        if (f.FieldType == typeof(Vector3)) avoid.Add((Vector3)f.GetValue(null));
                        else if (f.FieldType == typeof(Vector3[])) { var a = (Vector3[])f.GetValue(null); if (a == null) continue; for (int i = 0; i < a.Length; i++) { avoid.Add(a[i]); if (i > 0) segs.Add((a[i - 1], a[i])); } }
                    }
                    catch { }
                }
            var rends = FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.gameObject.scene == gameObject.scene && r.enabled && !(r is ParticleSystemRenderer)).ToArray();
            float best = 1e9f; Vector3? pick = null; int tried = 0, rejGround = 0, rejMesh = 0, rejRoute = 0;
            // candidates: a 1.5 m grid over every usable walkable zone within reach of the seed, nearest first
            var cands = new List<Vector3>();
            foreach (var z in Ground.Zones)
            {
                if (!z.Usable || z.Low) continue;
                for (float x = z.Min.x + 1f; x <= z.Max.x - 1f; x += 1.5f)
                    for (float zz = z.Min.y + 1f; zz <= z.Max.y - 1f; zz += 1.5f)
                    {
                        var c0 = new Vector3(x, z.HeightAt(x, zz), zz);
                        if (Flat(c0 - S.Seed).magnitude <= S.Radius * 3f && Mathf.Abs(c0.y - S.Seed.y) < 4f) cands.Add(c0);
                    }
            }
            cands.Sort((a1, b1) => Flat(a1 - S.Seed).sqrMagnitude.CompareTo(Flat(b1 - S.Seed).sqrMagnitude));
            foreach (var c in cands)
                {
                    if (pick.HasValue) break;
                    tried++;
                    float y0 = Ground.Height(c.x, c.z);
                    if (float.IsNaN(y0) || Mathf.Abs(y0 - S.Seed.y) > 4f) { rejGround++; continue; }
                    var o = new Vector3(c.x, y0, c.z);
                    if (!GroundOk(o)) { rejGround++; continue; }
                    if (!Clear(o, rends)) { rejMesh++; continue; }
                    var mid = o + new Vector3(1.6f, 0f, 1.3f);
                    // QF41: 10 m from the block's centre (its half-length is 3.2 m: the old 6-7 m left a straight bike line from Mars's west rim
                    // to the saucer 2.8 m from its corner - the bot rode into it and needed a skip)
                    bool near = avoid.Any(p => Flat(p - mid).magnitude < 10f && Mathf.Abs(p.y - o.y) < 12f) || segs.Any(sg => SegDist(mid, sg.Item1, sg.Item2) < 10f);
                    if (near) { rejRoute++; continue; }
                    float sc = Flat(o - S.Seed).magnitude;
                    if (sc < best) { best = sc; pick = o; }
                }
            if (!pick.HasValue)
            {
                Why = $"no free spot within {S.Radius * 3f:0} m of the seed ({tried} tried: ground {rejGround}, meshes {rejMesh}, story places {rejRoute}; meshes most in the way: {string.Join(", ", _blockers.OrderByDescending(k => k.Value).Take(4).Select(k => k.Key + " " + k.Value))})";
                L($"CLIMB SECTION (QF40 3) {S.Scene}: NOT PLACED - {Why}");
                return;
            }
            O = pick.Value; Placed = true;
            Build(rends);
            L($"CLIMB SECTION (QF40 3) {S.Scene}: {S.Theme} at {O} ({best:0.0} m from the seed; {tried} spots tried)");
            StartCoroutine(Install());
        }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        static void L(string s) { Debug.Log("[Seven] " + s); if (DemoBoot.CaptureDir == null) return; var c = FindAnyObjectByType<ChapterCapture>(); if (c != null) c.Log(s); }
        static float SegDist(Vector3 p, Vector3 a, Vector3 b) { a.y = b.y = p.y = 0f; var ab = b - a; float t = ab.sqrMagnitude < 1e-4f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude); return Vector3.Distance(p, a + ab * t); }

        /// <summary>The footprint + 3.5 m in front + 2.4 m behind + 1.2 m each side: walkable, flat (within 0.12 m).</summary>
        bool GroundOk(Vector3 o)
        {
            for (float x = AX0 - 1.2f; x <= BX1 + 1.2f; x += 0.6f)
                for (float z = -3.5f; z <= D + 2.4f; z += 0.6f)
                {
                    float h = Ground.Height(o.x + x, o.z + z);
                    if (float.IsNaN(h) || Mathf.Abs(h - o.y) > 0.25f) return false;
                    var cl = Ground.ClampToArena(new Vector3(o.x + x, o.y, o.z + z), 0.05f);
                    if (Flat(cl - new Vector3(o.x + x, o.y, o.z + z)).magnitude > 0.05f) return false;
                }
            return true;
        }
        readonly Dictionary<string, int> _blockers = new Dictionary<string, int>();
        bool Clear(Vector3 o, Renderer[] rends)
        {
            var b = new Bounds(o + new Vector3((AX0 + BX1) * 0.5f, 3f, D * 0.5f - 0.4f), new Vector3(BX1 - AX0 + 2.4f, 5.6f, D + 5.6f));
            foreach (var r in rends) { var rb = r.bounds; if (rb.size.x > 60f || rb.size.z > 60f) continue; if (rb.Intersects(b) && rb.max.y > o.y + 0.25f && rb.min.y < o.y + 5.2f) { _blockers[r.name] = (_blockers.TryGetValue(r.name, out var n) ? n : 0) + 1; return false; } }
            return !Physics.CheckBox(b.center, b.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        }

        // ------------------------------------------------------------------------------------------ geometry
        Vector3 P(float x, float y, float z) => O + new Vector3(x, y, z);
        void Build(Renderer[] rends)
        {
            // the local architecture's material (the most common one round the spot), so it belongs where it stands
            var near = rends.Where(r => r.sharedMaterial != null && (r.bounds.center - O).magnitude < 60f && r.bounds.size.magnitude < 80f && !r.sharedMaterial.name.ToLower().Contains("glow")).ToArray();
            var mat = near.GroupBy(r => r.sharedMaterial).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
            if (mat == null) mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.45f, 0.45f, 0.45f) };
            var chalk = new Material(Shader.Find("Universal Render Pipeline/Unlit")); chalk.SetColor("_BaseColor", new Color(0.92f, 0.92f, 0.92f));
            var v = new List<Vector3>(); var t = new List<int>(); var rnd = new System.Random(S.Scene.GetHashCode());
            // the two tiers built in courses (each course a little offset: weathered masonry, not a plain box)
            void Tier(float x0, float x1, float h, float zBack)
            {
                for (float y = -0.5f; y < h - 0.01f; )      // set 0.5 m into the ground (no gap on uneven ground)
                {
                    float ch = Mathf.Min(h - y, 0.5f + (float)rnd.NextDouble() * 0.35f);
                    // each course laid as separate dressed stones (0.7-1.5 m), chamfered, with joints and small set-backs
                    for (float x = x0 - (float)rnd.NextDouble() * 0.5f; x < x1 - 0.05f; )
                    {
                        float w = 0.7f + (float)rnd.NextDouble() * 0.8f, a = Mathf.Max(x, x0), b = Mathf.Min(x + w, x1);
                        if (b - a > 0.12f)
                        {
                            float dz = (float)rnd.NextDouble() * 0.06f;
                            Stone(v, t, new Vector3((a + b) * 0.5f, y + ch * 0.5f, zBack * 0.5f + dz * 0.5f), new Vector3(b - a - 0.03f, ch - 0.03f, zBack - dz), 0.05f);
                        }
                        x += w;
                    }
                    y += ch;
                }
            }
            Tier(AX0, AX1, HA, D); Tier(AX1, BX1, HB, D);
            // the carved cornice she hangs from (proud of the tall block's face) and fallen chunks at the foot
            Box(v, t, new Vector3((AX0 + 0.4f + AX1) * 0.5f, HB - 0.09f, -0.1f), new Vector3(AX1 - AX0 - 0.4f, 0.18f, 0.22f));
            for (int i = 0; i < 5; i++) { float s = 0.25f + (float)rnd.NextDouble() * 0.3f; Box(v, t, new Vector3(BX1 + 0.4f + (float)rnd.NextDouble() * 0.8f, s * 0.5f, (float)rnd.NextDouble() * D), new Vector3(s * 1.3f, s, s), (float)rnd.NextDouble() * 70f); }
            var body = MeshGo("ClimbBlock", v, t, mat, true);
            // chalk-glow strips on every lip she can use
            var cv = new List<Vector3>(); var ct = new List<int>();
            // on the FACES just under each lip (never on the grip surface: her hands go on top of the lip)
            Box(cv, ct, new Vector3((AX0 + 0.4f + AX1) * 0.5f, HB - 0.12f, -0.225f), new Vector3(AX1 - AX0 - 0.4f, 0.035f, 0.02f));            // the cornice's front
            Box(cv, ct, new Vector3((AX1 + BX1) * 0.5f, HB - 0.12f, -0.012f), new Vector3(BX1 - AX1 - 0.6f, 0.035f, 0.02f));                   // the low tier's front lip
            Box(cv, ct, new Vector3(AX1 + 0.012f, HA - 0.12f, D * 0.5f), new Vector3(0.02f, 0.035f, D - 0.6f));                                 // the step's edge
            Box(cv, ct, new Vector3((AX0 + AX1) * 0.5f, HA - 0.12f, D + 0.012f), new Vector3(AX1 - AX0 - 0.8f, 0.035f, 0.02f));                 // the back lip
            for (int i = 0; i < 4; i++) Box(cv, ct, new Vector3(-0.6f + ((i % 2) - 0.5f) * 0.5f, 0.6f + i * 0.5f, -0.01f), new Vector3(0.22f, 0.03f, 0.02f));   // chalked holds up the face
            MeshGo("ClimbChalk", cv, ct, chalk, false);
            // traversal data (world space)
            Cornice = new T.Ledge { Name = "climb cornice (" + S.Scene + ")", A = P(AX0 + 0.4f, HB, -0.2f), B = P(BX1 - 0.4f, HB, -0.2f), Fwd = Vector3.forward, ClimbFrom = (AX1 + 0.2f) - (AX0 + 0.4f), ClimbTo = (BX1 - 0.6f) - (AX0 + 0.4f) };
            Climb = new T.JumpGrab { Name = "climb cornice grab (" + S.Scene + ")", From = P(-0.6f, 0f, -0.6f), Radius = 0.9f, Ledge = Cornice };
            Step = new T.Mantle { Name = "climb step (" + S.Scene + ")", From = P(AX1 + 0.6f, HB, D * 0.5f), Radius = 0.9f, FaceAt = O.x + AX1, Fwd = Vector3.left, TopY = O.y + HA, Along0 = O.z + 0.3f, Along1 = O.z + D - 0.3f };
            BackLip = new T.Ledge { Name = "climb back lip (" + S.Scene + ")", A = P(AX1 - 0.4f, HA, D), B = P(AX0 + 0.4f, HA, D), Fwd = Vector3.back };
            DownBack = new T.HangDown { Name = "climb back edge (" + S.Scene + ")", Spot = P(0.3f, HA, D - 0.4f), Ledge = BackLip, Radius = 0.8f };
            DownFront = new T.HangDown { Name = "climb low tier front (" + S.Scene + ")", Spot = P((AX1 + BX1) * 0.5f, HB, 0.4f), Ledge = Cornice, Radius = 0.8f };
            // exactly the block's footprint, and FIRST in the list: the ground's rule is one height per spot, the first zone in list
            // order wins (the level's floor runs under the block; behind it the tops were never found and a climb-up snapped her down)
            TopLow = new Ground.Zone { Name = "ClimbLow", Min = new Vector2(O.x + AX1, O.z), Max = new Vector2(O.x + BX1, O.z + D), Y0 = O.y + HB, Y1 = O.y + HB };
            TopHigh = new Ground.Zone { Name = "ClimbTop", Min = new Vector2(O.x + AX0, O.z), Max = new Vector2(O.x + AX1, O.z + D), Y0 = O.y + HA, Y1 = O.y + HA };
            Ground.Zones.Insert(0, TopHigh); Ground.Zones.Insert(0, TopLow);
        }
        /// <summary>A dressed stone: a box whose four vertical edges and top edges are chamfered (never a plain box).</summary>
        static void Stone(List<Vector3> v, List<int> t, Vector3 c, Vector3 s, float ch)
        {
            ch = Mathf.Min(ch, s.x * 0.2f, s.z * 0.2f, s.y * 0.2f);
            float hx = s.x * 0.5f, hy = s.y * 0.5f, hz = s.z * 0.5f;
            Vector3[] Ring(float y, float inset) { float x = hx - inset, z = hz - inset; return new[] { new Vector3(-x + ch, y, -z), new Vector3(x - ch, y, -z), new Vector3(x, y, -z + ch), new Vector3(x, y, z - ch), new Vector3(x - ch, y, z), new Vector3(-x + ch, y, z), new Vector3(-x, y, z - ch), new Vector3(-x, y, -z + ch) }; }
            var rings = new[] { Ring(-hy, 0f), Ring(hy - ch, 0f), Ring(hy, ch) };
            for (int k = 0; k < rings.Length - 1; k++)
                for (int i = 0; i < 8; i++)
                {
                    int b = v.Count; var a0 = rings[k][i]; var a1 = rings[k][(i + 1) % 8]; var b0 = rings[k + 1][i]; var b1 = rings[k + 1][(i + 1) % 8];
                    v.Add(c + a0); v.Add(c + b0); v.Add(c + b1); v.Add(c + a1);
                    t.Add(b); t.Add(b + 1); t.Add(b + 2); t.Add(b); t.Add(b + 2); t.Add(b + 3);
                }
            foreach (var (ring, up) in new[] { (rings[2], true), (rings[0], false) })
            {
                int b = v.Count; foreach (var q in ring) v.Add(c + q);
                for (int i = 1; i < 7; i++) { if (up) { t.Add(b); t.Add(b + i + 1); t.Add(b + i); } else { t.Add(b); t.Add(b + i); t.Add(b + i + 1); } }
            }
        }
        static void Box(List<Vector3> v, List<int> t, Vector3 c, Vector3 s, float yaw = 0f)
        {
            var q = Quaternion.Euler(0f, yaw, 0f); int b = v.Count;
            for (int i = 0; i < 8; i++) v.Add(c + q * Vector3.Scale(new Vector3((i & 1) == 0 ? -0.5f : 0.5f, (i & 2) == 0 ? -0.5f : 0.5f, (i & 4) == 0 ? -0.5f : 0.5f), s));
            int[] f = { 0, 2, 3, 0, 3, 1, 4, 5, 7, 4, 7, 6, 0, 1, 5, 0, 5, 4, 2, 6, 7, 2, 7, 3, 0, 4, 6, 0, 6, 2, 1, 3, 7, 1, 7, 5 };
            foreach (var i in f) t.Add(b + i);
        }
        GameObject MeshGo(string name, List<Vector3> v, List<int> t, Material m, bool collide)
        {
            // flat-shaded: every triangle its own vertices (crisp masonry edges, never a zero normal)
            var vv = new List<Vector3>(); var tt = new List<int>();
            for (int i = 0; i < t.Count; i++) { vv.Add(v[t[i]] + O); tt.Add(i); }
            var mesh = new Mesh { name = "QF40_" + name + "_" + S.Scene, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vv); mesh.SetTriangles(tt, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = m;
            if (collide) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        // ------------------------------------------------------------------------------------------ traversal + story
        IEnumerator Install()
        {
            for (int i = 0; i < 4; i++) yield return null;           // after the chapter's director installed its own set (Start)
            bool moon = T.IsMoonSet;                                  // Mars never installed one: it ran with the Moon's ledges active
            T.Use(Add(moon ? null : T.Ledges, Cornice, BackLip), moon ? null : T.Jumps, Add(moon ? null : T.Grabs, Climb), moon ? null : T.Climbs,
                  Add(moon ? null : T.Mantles, Step), Add(moon ? null : T.HangDowns, DownBack, DownFront));
            Installed++;
            L($"CLIMB SECTION (QF40 3) {S.Scene}: moves installed ({(moon ? "the Moon's set was still active here - replaced" : "added to the chapter's own")})");
            if (System.Environment.GetCommandLineArgs().Contains("-climbtest")) { StartCoroutine(Test()); yield break; }
            StartCoroutine(Story());
        }
        static TT[] Add<TT>(TT[] a, params TT[] more) => (a ?? new TT[0]).Concat(more.Where(x => x != null)).ToArray();
        void OnDestroy() { if (Current == this) Current = null; T.UseMoon(); }

        IEnumerator Story()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.25f);
                if (_seven == null) _seven = FindAnyObjectByType<ChapterSeven>();
                if (_seven == null || !_seven.gameObject.activeInHierarchy) continue;
                var p = _seven.transform.position;
                bool free = _seven.Current == ChapterSeven.Mode.Free && !DialoguePlayer.AnyBusy && !PauseMenu.IsOpen;
                if (!_saidA && free && Flat(p - P(1.6f, 0f, -2f)).magnitude < 16f && Mathf.Abs(p.y - O.y) < 2f) { _saidA = true; yield return Say(S.IdA, S.LineA); L($"CLIMB SECTION (QF40 3) {S.Scene}: the cat points it out"); }
                if (!_saidB && TopHigh.Contains(p.x, p.z) && Mathf.Abs(p.y - TopHigh.Y0) < 0.3f && _seven.Current == ChapterSeven.Mode.Free)
                {
                    _saidB = true; Reached++;
                    L($"CLIMB SECTION (QF40 3) {S.Scene}: she reached the top - the view toward {S.View}");
                    yield return new WaitForSeconds(0.6f);
                    yield return Say(S.IdB, S.LineB);
                }
            }
        }
        public static int Reached;
        IEnumerator Say(string id, string text)
        {
            var dlg = FindAnyObjectByType<DialoguePlayer>();
            var clip = Resources.Load<AudioClip>("ClimbVoice/" + id);
            if (dlg == null) yield break;
            var l = new VoiceBank.Line { Id = id, Speaker = S.Speaker, Text = text, Clip = clip, PauseAfter = 0.3f };
            yield return dlg.PlayLines(new List<VoiceBank.Line> { l }, false);
        }

        // ------------------------------------------------------------------------------------------ -climbtest (the proof)
        IEnumerator Test()
        {
            var cap = FindAnyObjectByType<ChapterCapture>();
            void Log(string s) { Debug.Log("[Seven] " + s); if (cap != null) cap.Log(s); }
            _seven = FindAnyObjectByType<ChapterSeven>();
            for (float t0 = Time.time; Time.time - t0 < 30f && (_seven == null || _seven.Current != ChapterSeven.Mode.Free); ) { yield return null; if (_seven == null) _seven = FindAnyObjectByType<ChapterSeven>(); }
            yield return new WaitForSeconds(2f);
            // the chapter's own flow steps aside (its autopilot would drive her elsewhere)
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)) if (mb.GetType().Name.EndsWith("Director") && mb.GetType().Namespace == "Seven") { mb.StopAllCoroutines(); mb.enabled = false; }
            var bike = FindAnyObjectByType<HoverBike>(); if (bike != null && bike.Riding) bike.DismountTo(P(1.6f, 0f, -3f), Vector3.forward);
            var ui = FindAnyObjectByType<ChapterUI>(); if (ui != null) StartCoroutine(ui.Fade(0f, 0.2f));
            var cam = FindAnyObjectByType<ChapterCamera>();
            _seven.Lock(false);                                       // a stopped director may have left her locked (a cutscene)
            if (!Placed) { Log($"CLIMB TEST (QF40 3) {S.Scene}: NOT PLACED - {Why}"); cap?.Finish($"CLIMB TEST (QF40 3) {S.Scene}: FAIL (not placed)"); yield break; }
            int pass = 0; var lines = new List<string>();
            foreach (var human in new[] { false, true })
            {
                _seven.Place(P(-0.6f, 0f, -3.2f), Vector3.forward);
                if (cam != null) { cam.Follow(true); cam.SnapBehind(); }
                yield return new WaitForSeconds(0.8f);
                var r = new BotRun { Human = human };
                yield return Bot(r, cap);
                lines.Add($"CLIMB TEST (QF40 3) {S.Scene} [{(human ? "human-timed bot" : "precise bot")}]: {(r.Ok ? "PASS" : "FAIL at " + r.FailedAt)} in {r.Seconds:0.0} s - steps {string.Join(", ", r.Steps)}; retries {r.Retries}; stuck hangs {r.Stuck}; 7 hidden from the player's camera {r.Hidden} of {r.Frames} frames (must be 0){(r.HiddenAt.Count > 0 ? " [" + string.Join("; ", r.HiddenAt) + "]" : "")}; falls {r.Falls}");
                Log(lines[lines.Count - 1]);
                if (r.Ok && r.Hidden == 0 && r.Stuck == 0) pass++;
            }
            cap?.Finish($"CLIMB TEST (QF40 3) {S.Scene}: {pass}/2 runs pass");
        }
        class BotRun { public bool Human, Ok; public string FailedAt; public float Seconds; public int Retries, Stuck, Hidden, Frames, Falls; public List<string> Steps = new List<string>(), HiddenAt = new List<string>(); }

        IEnumerator Bot(BotRun r, ChapterCapture cap)
        {
            var s7 = _seven; s7.ScriptDriven = true; float t0 = Time.time; var rnd = new System.Random(r.Human ? 7 : 3);
            float J() => r.Human ? (float)(rnd.NextDouble() * 0.5 - 0.15) : 0f;                         // a press up to 0.35 s late or 0.15 s early
            Vector3 Err(Vector3 d) => r.Human ? Quaternion.Euler(0f, (float)(rnd.NextDouble() * 30 - 15), 0f) * d : d;
            var watch = StartCoroutine(Watch(r));
            IEnumerator WalkTo(Vector3 to, float tol, float timeout)
            {
                for (float t = 0f; t < timeout; t += Time.deltaTime)
                {
                    var d = Flat(to - s7.transform.position); if (d.magnitude < tol) break;
                    s7.ScriptMove = Err(d.normalized) * Mathf.Clamp01(d.magnitude * 1.5f + 0.25f); yield return null;
                }
                s7.ScriptMove = Vector3.zero;
            }
            IEnumerator Press(string what, System.Action press, System.Func<bool> done, float timeout)
            {
                for (int tries = 0; tries < 4; tries++)
                {
                    float late = J(); if (late > 0f) yield return new WaitForSeconds(late);
                    press();
                    for (float t = 0f; t < timeout && !done(); t += Time.deltaTime) yield return null;
                    if (done()) { r.Steps.Add(what); yield break; }
                    r.Retries++; L($"CLIMB TEST {S.Scene}: '{what}' try {tries + 1} not done - {s7.Current} at {s7.transform.position - O} (local), hang u {s7.HangU:0.00} on {(s7.HangLedge != null ? s7.HangLedge.Name : "-")}, state {s7.State}");
                    yield return new WaitForSeconds(0.4f);
                }
            }
            bool Hang(T.Ledge l) => s7.Current == ChapterSeven.Mode.Hang && s7.HangLedge == l;
            bool On(Ground.Zone z) => s7.Current == ChapterSeven.Mode.Free && z.Contains(s7.transform.position.x, s7.transform.position.z) && Mathf.Abs(s7.transform.position.y - z.Y0) < 0.25f;
            // 1. up the face onto the cornice
            yield return WalkTo(Climb.From, 0.3f, 8f);
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { s7.transform.rotation = Quaternion.RotateTowards(s7.transform.rotation, Quaternion.LookRotation(Vector3.forward), 400f * Time.deltaTime); yield return null; }
            yield return Press("jump to the cornice", () => s7.ScriptJump = true, () => Hang(Cornice), 3.5f);
            if (!Hang(Cornice)) { r.FailedAt = $"the jump to the cornice (she was {s7.Current}, {Flat(s7.transform.position - Climb.From).magnitude:0.00} m from its spot, {s7.transform.position.y - Climb.From.y:0.00} m above)"; goto done; }
            if (cap != null) yield return cap.Shot($"climb_{S.Scene.ToLower()}_{(r.Human ? "h" : "p")}_1_hanging_on_the_cornice");
            // 2. shimmy along under the tall block to the low tier (human: overshoots, then comes back)
            {
                float goal = (Cornice.ClimbFrom + Cornice.ClimbTo) * 0.5f + (r.Human ? 0.9f : 0f);
                for (float t = 0f; t < 14f && Mathf.Abs(s7.HangU - goal) > 0.12f && Hang(Cornice); t += Time.deltaTime) { s7.ScriptMove = Cornice.Dir * Mathf.Sign(goal - s7.HangU); yield return null; }
                s7.ScriptMove = Vector3.zero;
                if (r.Human) { goal = (Cornice.ClimbFrom + Cornice.ClimbTo) * 0.5f; for (float t = 0f; t < 6f && Mathf.Abs(s7.HangU - goal) > 0.15f && Hang(Cornice); t += Time.deltaTime) { s7.ScriptMove = Cornice.Dir * Mathf.Sign(goal - s7.HangU); yield return null; } s7.ScriptMove = Vector3.zero; }
                if (!Hang(Cornice)) { r.FailedAt = "the shimmy (let go)"; goto done; }
                r.Steps.Add("shimmy");
                if (cap != null) yield return cap.Shot($"climb_{S.Scene.ToLower()}_{(r.Human ? "h" : "p")}_2_shimmy");
            }
            // 3. up onto the low tier
            yield return Press("climb up", () => s7.ScriptJump = true, () => On(TopLow), 4f);
            if (!On(TopLow)) { r.FailedAt = "the climb up onto the low tier"; goto done; }
            // 4. the step up to the top
            yield return WalkTo(new Vector3(O.x + AX1 + 0.55f, 0f, O.z + D * 0.5f), 0.3f, 6f);
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { s7.transform.rotation = Quaternion.RotateTowards(s7.transform.rotation, Quaternion.LookRotation(Vector3.left), 400f * Time.deltaTime); yield return null; }
            yield return Press("the step (mantle)", () => s7.ScriptJump = true, () => On(TopHigh), 3.5f);
            if (!On(TopHigh)) { r.FailedAt = "the step up to the top"; goto done; }
            yield return new WaitForSeconds(1.2f);
            ImageGate.ExpectPoint(S.View, "the view from the top of the climb");
            if (cap != null) yield return cap.Shot($"climb_{S.Scene.ToLower()}_{(r.Human ? "h" : "p")}_3_at_the_top");
            // 5. down: over the back edge into a hang, then let go
            yield return WalkTo(DownBack.Spot, 0.25f, 6f);
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { s7.transform.rotation = Quaternion.RotateTowards(s7.transform.rotation, Quaternion.LookRotation(Vector3.forward), 400f * Time.deltaTime); yield return null; }
            yield return Press("climb down into a hang", () => s7.ScriptCrouch = true, () => Hang(BackLip), 3f);
            if (!Hang(BackLip)) { r.FailedAt = "the climb down"; goto done; }
            yield return new WaitForSeconds(0.6f);
            yield return Press("let go", () => s7.ScriptLetGo = true, () => s7.Current == ChapterSeven.Mode.Free && Mathf.Abs(s7.transform.position.y - O.y) < 0.3f, 4f);
            if (s7.Current != ChapterSeven.Mode.Free || Mathf.Abs(s7.transform.position.y - O.y) > 0.3f) { r.FailedAt = "the let go"; goto done; }
            r.Ok = true;
        done:
            s7.ScriptMove = Vector3.zero; s7.ScriptJump = s7.ScriptCrouch = s7.ScriptLetGo = false;
            r.Seconds = Time.time - t0;
            StopCoroutine(watch);
        }
        /// <summary>Every frame of a bot run: is 7 visible from the player's camera (in frame and not behind geometry)? Hangs
        /// that stop moving for 6 s count as stuck; a drop below the base counts as a fall.</summary>
        IEnumerator Watch(BotRun r)
        {
            float hangStill = 0f, t0 = Time.time; Vector3 last = _seven.transform.position;
            while (true)
            {
                var cam = Camera.main;
                if (cam != null && Time.time - t0 > 1f)             // (the first second = the test's teleport + camera snap, not play)
                {
                    r.Frames++;
                    var head = _seven.Bone(HumanBodyBones.Head); var chest = _seven.Bone(HumanBodyBones.Chest);
                    int seen = 0;
                    foreach (var b in new[] { head, chest }) { if (b == null) continue; var vp = cam.WorldToViewportPoint(b.position); if (vp.z > 0f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f && ChapterCamera.ClearLine(cam.transform.position, b.position)) seen++; }
                    if (seen == 0) { r.Hidden++; if (r.HiddenAt.Count < 6) r.HiddenAt.Add($"{Time.time - t0:0.0}s {_seven.Current}/{_seven.State} cam {cam.transform.position - O}"); }
                }
                if (_seven.Current == ChapterSeven.Mode.Hang) { hangStill = (_seven.transform.position - last).magnitude < 0.002f ? hangStill + Time.deltaTime : 0f; if (hangStill > 6f) { r.Stuck++; hangStill = 0f; } }
                if (_seven.transform.position.y < O.y - 1f) r.Falls++;
                last = _seven.transform.position;
                yield return new WaitForEndOfFrame();              // measured on the frame as rendered (bones after animation + IK, the camera placed)
            }
        }
        public static string Summary() => Current == null ? "CLIMB SECTION (QF40 3): none in this scene" : $"CLIMB SECTION (QF40 3) {Current.S.Scene}: {(Current.Placed ? "placed at " + Current.O : "NOT placed - " + Current.Why)}, reached the top {Reached}x";
    }
}
