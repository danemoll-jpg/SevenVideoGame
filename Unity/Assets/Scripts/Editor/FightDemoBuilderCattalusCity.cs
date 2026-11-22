using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using C = Seven.CattalusLayout;
using K = Seven.CattalusCourse;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 18 — the Cattalus kit (Kyle's misty monochrome; never a plain box): cardboard homes (corrugated edges, folded flaps,
    /// tape, cut doors and windows, printed paw glyphs, stacked and joined into tall mansions), cat-tree apartment towers (sisal
    /// trunks, carpeted platforms, hammocks, toys dangling on cords), rope-wrapped and clawed scratching-post columns, upholstered
    /// terraces (tufted pads), the fountain with its maintenance trickle, the old transit tower with its hanging ball, the
    /// Cat-a-tron, the meadow (grass, the low hill line and the leaning tree of vision 7), and the Grand Circuit (the road, rails,
    /// supports, the monumental column of the spiral, gates of hanging ornaments, the toy boulevard's arches).
    /// </summary>
    public static partial class FightDemoBuilder
    {
        struct CtMats { public Material card, cardDark, cardLight, tape, sisal, carpet, carpetDark, fabric, wood, stone, metal, glow, glowSoft, grass, grassDark, leaf, bark, hill, road, rail, paper, ground; }

        static void Q(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool twoSided = true)
        {
            int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            if (twoSided) t.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
        }

        /// <summary>A cardboard box home: the box (a slightly soft-edged body), corrugated ribs on its long faces, four folded
        /// flaps standing open at the top (or one folded in), a tape strip, a cut door and cut windows (dark), paw glyphs.</summary>
        static void CardboardBox(AK k, CtMats m, Vector3 at, Vector3 size, float yaw, int seed, bool door = true, bool flaps = true)
        {
            var q = Quaternion.Euler(0f, yaw, 0f); var rnd = new System.Random(seed);
            float R() => (float)rnd.NextDouble();
            var (v, t) = k.Of(seed % 3 == 0 ? m.cardLight : m.card);
            CardBody(v, t, at + Vector3.up * size.y * 0.5f, size, yaw);
            // corrugated ribs: faint vertical ridges just proud of the two long faces
            var (rv, rt) = k.Of(m.cardDark);
            for (float x = -size.x * 0.45f; x <= size.x * 0.45f; x += 0.9f)
                foreach (var sz in new[] { -1f, 1f })
                {
                    var p0 = at + q * new Vector3(x, 0.25f, sz * (size.z * 0.5f + 0.02f)); var p1 = at + q * new Vector3(x, size.y - 0.3f, sz * (size.z * 0.5f + 0.02f));
                    Sweep(rv, rt, new[] { p0, p1 }, 0.035f, false, 4);
                }
            // the top edge's torn corrugation: a ragged lip
            // flaps: four thin panels hinged at the top edges, opened outward at varied angles
            if (flaps)
            {
                var (fv, ft) = k.Of(m.card);
                for (int i = 0; i < 4; i++)
                {
                    bool alongX = i % 2 == 0; float sg = i < 2 ? 1f : -1f;
                    float len = alongX ? size.x : size.z, hw = (alongX ? size.z : size.x) * 0.5f, flapH = Mathf.Min(hw, 1.6f) * (0.7f + R() * 0.25f);
                    float open = Mathf.Lerp(25f, 75f, R()) * Mathf.Deg2Rad;
                    Vector3 Edge(float u) => alongX ? new Vector3(u, size.y, sg * hw) : new Vector3(sg * hw, size.y, u);
                    Vector3 Out() => alongX ? new Vector3(0f, 0f, sg) : new Vector3(sg, 0f, 0f);
                    var o = Out() * Mathf.Sin(open) * flapH + Vector3.up * Mathf.Cos(open) * flapH;
                    var a = Edge(-len * 0.48f); var b = Edge(len * 0.48f);
                    Q(fv, ft, at + q * a, at + q * b, at + q * (b + o), at + q * (a + o));
                }
            }
            // tape across the top
            var (tv, tt) = k.Of(m.tape);
            Q(tv, tt, at + q * new Vector3(-size.x * 0.5f, size.y + 0.03f, -0.12f), at + q * new Vector3(size.x * 0.5f, size.y + 0.03f, -0.12f), at + q * new Vector3(size.x * 0.5f, size.y + 0.03f, 0.12f), at + q * new Vector3(-size.x * 0.5f, size.y + 0.03f, 0.12f));
            Q(tv, tt, at + q * new Vector3(size.x * 0.5f + 0.03f, size.y, -0.12f), at + q * new Vector3(size.x * 0.5f + 0.03f, size.y * 0.6f, -0.12f), at + q * new Vector3(size.x * 0.5f + 0.03f, size.y * 0.6f, 0.12f), at + q * new Vector3(size.x * 0.5f + 0.03f, size.y, 0.12f));
            // the cut door (a rounded arch hole, dark) and cut windows (round or square, dark), paw glyphs printed
            var (dv, dt) = k.Of(m.cardDark);
            const float po = 0f;                                                          // a cardboard box has no plinth (CardBody)
            float face = -size.z * 0.5f - po - 0.03f;          // 30 mm proud: more than the coplanar fix can ever nudge a merged object (5 x 4 mm)
            if (door)
            {
                float dw = Mathf.Min(0.5f, size.x * 0.3f), dh = Mathf.Min(0.6f, size.y * 0.4f);   // a cat door (the city is built by cats)
                var arch = new List<Vector3>();
                for (int i = 0; i <= 12; i++) { float a = i / 12f * Mathf.PI; arch.Add(new Vector3(-Mathf.Cos(a) * dw * 0.5f, dh - dw * 0.5f + Mathf.Sin(a) * dw * 0.5f, face)); }
                for (int i = 0; i < 12; i++) Q(dv, dt, at + q * new Vector3(arch[i].x, 0.02f, face), at + q * arch[i], at + q * arch[i + 1], at + q * new Vector3(arch[i + 1].x, 0.02f, face));
            }
            int wins = Mathf.Clamp((int)(size.x / 2.2f), 1, 4);
            for (int i = 0; i < wins; i++)
            {
                float x = (i + 0.5f) / wins * size.x - size.x * 0.5f, y = size.y * (door && Mathf.Abs(x) < 0.9f ? 0.78f : 0.6f);
                if (y > size.y - 0.5f) continue;
                bool round = (seed + i) % 2 == 0; float r = Mathf.Min(0.45f, size.y * 0.12f);
                if (round) { var c = at + q * new Vector3(x, y, face); for (int s = 0; s < 10; s++) { float a0 = s / 10f * Mathf.PI * 2f, a1 = (s + 1) / 10f * Mathf.PI * 2f; Q(dv, dt, c, c, at + q * new Vector3(x + Mathf.Cos(a1) * r, y + Mathf.Sin(a1) * r, face), at + q * new Vector3(x + Mathf.Cos(a0) * r, y + Mathf.Sin(a0) * r, face)); } }
                else Q(dv, dt, at + q * new Vector3(x - r, y - r, face), at + q * new Vector3(x - r, y + r, face), at + q * new Vector3(x + r, y + r, face), at + q * new Vector3(x + r, y - r, face));
            }
            // printed glyphs: a paw print (pad + four toes) on the side face
            var gc = at + q * new Vector3(size.x * 0.5f + po + 0.04f, size.y * 0.42f, size.z * 0.15f);
            float gs = Mathf.Min(0.5f, size.y * 0.12f);
            Ellipsoid(dv, dt, gc, new Vector3(0.01f, gs * 0.6f, gs * 0.7f), 3, 8);
            for (int i = 0; i < 4; i++) Ellipsoid(dv, dt, gc + q * new Vector3(0f, gs * 0.85f, (i - 1.5f) * gs * 0.5f) + Vector3.up * (i == 0 || i == 3 ? -gs * 0.2f : 0f), new Vector3(0.01f, gs * 0.24f, gs * 0.2f), 3, 6);
        }

        /// <summary>A cardboard box body: straight walls (no masonry plinth - its outset face met the cut doors of other boxes), a
        /// slightly softened top edge.</summary>
        static void CardBody(List<Vector3> v, List<int> t, Vector3 c, Vector3 size, float yaw)
        {
            float h = size.y, ch = Mathf.Min(0.04f, h * 0.05f);
            Loft(v, t, c, size.x * 0.5f, size.z * 0.5f, yaw, new List<(float y, float inset)> { (-h * 0.5f, 0f), (h * 0.5f - ch, 0.004f), (h * 0.5f, ch) });
        }
        /// <summary>A cardboard mansion: boxes stacked and offset into a tall home, joined by cardboard tubes, a little ordinary box
        /// at its foot (the vignette's owner sleeps there).</summary>
        static void CardboardMansion(AK k, CtMats m, Vector3 at, float yaw, int levels, int seed)
        {
            var q = Quaternion.Euler(0f, yaw, 0f); var rnd = new System.Random(seed);
            float y = 0f; var w = new Vector3(7f + (float)rnd.NextDouble() * 3f, 3.4f, 6f + (float)rnd.NextDouble() * 2f);
            for (int i = 0; i < levels; i++)
            {
                var off = q * new Vector3(((float)rnd.NextDouble() - 0.5f) * 1.6f, 0f, ((float)rnd.NextDouble() - 0.5f) * 1.2f);
                CardboardBox(k, m, at + off + Vector3.up * y, w, yaw + ((float)rnd.NextDouble() - 0.5f) * 8f, seed * 7 + i, i == 0, i == levels - 1);
                if (i > 0) { var (tv, tt) = k.Of(m.cardLight); Append(tv, tt, Lathe(new[] { new Vector2(0.45f, 0f), new Vector2(0.45f, 3.4f) }, 0f, 360f, 12, null, at + q * new Vector3(w.x * 0.5f + 0.5f, y - 3.4f, 0f))); }
                y += w.y; w = new Vector3(w.x * 0.82f, w.y * 0.95f, w.z * 0.85f);
            }
        }

        /// <summary>A cat-tree apartment tower: a sisal-wrapped trunk, carpeted platforms (round, rimmed) at staggered heights with
        /// box rooms on some, a hammock, toys dangling on cords.</summary>
        static void CatTree(AK k, CtMats m, Vector3 at, float h, int seed, float trunkR = 0.9f)
        {
            var rnd = new System.Random(seed);
            var (sv, st) = k.Of(m.sisal);
            Append(sv, st, Lathe(new[] { new Vector2(0f, 0f), new Vector2(trunkR, 0f), new Vector2(trunkR, h), new Vector2(0f, h) }, 0f, 360f, 14, null, at));
            var (rv, rt) = k.Of(m.cardDark);
            var helix = new List<Vector3>(); for (float y = 0.2f; y < h; y += 0.22f) { float a = y / 0.22f * 0.9f; helix.Add(at + new Vector3(Mathf.Cos(a) * (trunkR + 0.04f), y, Mathf.Sin(a) * (trunkR + 0.04f))); }
            Sweep(rv, rt, helix, 0.05f, false, 4);
            int plats = Mathf.Max(2, (int)(h / 5.5f));
            for (int i = 1; i <= plats; i++)
            {
                float y = h * i / (plats + 0.3f); float pr = 2.4f + (float)rnd.NextDouble() * 2f; float ang = (float)rnd.NextDouble() * 360f;
                var c = at + Quaternion.Euler(0f, ang, 0f) * Vector3.forward * (pr * 0.45f) + Vector3.up * y;
                var (pv, pt) = k.Of(i % 2 == 0 ? m.carpet : m.carpetDark);
                Append(pv, pt, Lathe(new[] { new Vector2(0f, -0.25f), new Vector2(pr, -0.25f), new Vector2(pr + 0.12f, -0.1f), new Vector2(pr + 0.12f, 0.08f), new Vector2(pr, 0.15f), new Vector2(0f, 0.15f) }, 0f, 360f, 18, null, c));
                if (i % 3 == 1) CardboardBox(k, m, c + Vector3.up * 0.15f + Quaternion.Euler(0f, ang + 90f, 0f) * Vector3.forward * 0.6f, new Vector3(2.2f, 1.6f, 1.8f), ang, seed * 13 + i, true, false);
                if (i % 2 == 0)
                {
                    // a toy dangling on a cord from the platform's rim
                    var tip = c + Quaternion.Euler(0f, ang + 140f, 0f) * Vector3.forward * (pr * 0.95f);
                    Sweep(rv, rt, new[] { tip, tip + Vector3.down * 1.8f }, 0.02f, false, 3);
                    var (bv, bt) = k.Of(m.fabric); Ellipsoid(bv, bt, tip + Vector3.down * 2.0f, new Vector3(0.22f, 0.18f, 0.3f), 4, 8);
                }
            }
            // a hammock slung between the trunk and a post
            { float y = h * 0.35f; var a = at + new Vector3(trunkR, y, 0f); var b = a + new Vector3(3.2f, 0f, 0.8f);
              var (pv, pt) = k.Of(m.sisal); Append(pv, pt, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.18f, 0f), new Vector2(0.18f, y + 0.4f), new Vector2(0f, y + 0.4f) }, 0f, 360f, 8, null, new Vector3(b.x, at.y, b.z)));
              var (hv, ht) = k.Of(m.fabric); var sag = new List<Vector3>(); for (int i = 0; i <= 8; i++) { float u = i / 8f; sag.Add(Vector3.Lerp(a, b, u) + Vector3.down * Mathf.Sin(u * Mathf.PI) * 0.7f); }
              for (int i = 0; i < 8; i++) Q(hv, ht, sag[i] + Vector3.forward * 0.6f, sag[i] - Vector3.forward * 0.6f, sag[i + 1] - Vector3.forward * 0.6f, sag[i + 1] + Vector3.forward * 0.6f); }
        }

        /// <summary>A scratching-post column: a thick post wrapped in rope (a tight helix) with claw marks (short dark gouges).</summary>
        static void ScratchPost(AK k, CtMats m, Vector3 at, float r, float h, int seed)
        {
            var (sv, st) = k.Of(m.sisal);
            Append(sv, st, Lathe(new[] { new Vector2(0f, 0f), new Vector2(r * 1.25f, 0f), new Vector2(r * 1.2f, 0.5f), new Vector2(r, 0.6f), new Vector2(r, h - 0.6f), new Vector2(r * 1.2f, h - 0.5f), new Vector2(r * 1.25f, h), new Vector2(0f, h) }, 0f, 360f, 18, null, at));
            var (rv, rt) = k.Of(m.cardDark);
            float pitch = Mathf.Max(0.14f, r * 0.09f);
            var helix = new List<Vector3>(); for (float y = 0.7f; y < h - 0.7f; y += pitch / 6f) { float a = (y / pitch) * Mathf.PI * 2f; helix.Add(at + new Vector3(Mathf.Cos(a) * (r + 0.02f), y, Mathf.Sin(a) * (r + 0.02f))); }
            Sweep(rv, rt, helix, pitch * 0.18f, false, 4);
            var rnd = new System.Random(seed);
            for (int i = 0; i < 14; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, y = 0.8f + (float)rnd.NextDouble() * Mathf.Min(3f, h - 1.6f);
                for (int c = 0; c < 4; c++) { float aa = a + c * 0.06f / r; var p = at + new Vector3(Mathf.Cos(aa) * (r + 0.05f), y, Mathf.Sin(aa) * (r + 0.05f)); Sweep(rv, rt, new[] { p, p + Vector3.down * 0.55f }, 0.025f, false, 3); }
            }
        }

        /// <summary>An upholstered terrace: stepped pads with rounded edges and button tufts.</summary>
        static void Terrace(AK k, CtMats m, Vector3 at, float yaw, float w, float d, int steps)
        {
            var q = Quaternion.Euler(0f, yaw, 0f);
            for (int i = 0; i < steps; i++)
            {
                var c = at + q * new Vector3(0f, i * 0.55f + 0.27f, -i * d / steps * 0.5f);
                var (v, t) = k.Of(i % 2 == 0 ? m.fabric : m.carpetDark);
                MBox(v, t, c, new Vector3(w - i * 0.8f, 0.55f, d - i * d / steps * 0.9f), yaw, 0.02f);
                var (bv, bt) = k.Of(m.cardDark);
                for (float x = -w * 0.4f; x < w * 0.4f; x += 1.2f) Ellipsoid(bv, bt, c + q * new Vector3(x, 0.29f, 0f), new Vector3(0.07f, 0.03f, 0.07f), 2, 6);
            }
        }

        /// <summary>The fountain: a pristine round basin with a spout, and beside it the maintenance trickle (a little pipe).</summary>
        static void Fountain(AK k, CtMats m, Vector3 at)
        {
            var (v, t) = k.Of(m.stone);
            Append(v, t, Lathe(new[] { new Vector2(0f, 0.05f), new Vector2(3.4f, 0.05f), new Vector2(3.6f, 0.2f), new Vector2(3.6f, 0.8f), new Vector2(3.3f, 0.85f), new Vector2(3.2f, 0.45f), new Vector2(0f, 0.45f) }, 0f, 360f, 32, null, at));
            Append(v, t, Lathe(new[] { new Vector2(0f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(0.3f, 2.4f), new Vector2(0.9f, 2.6f), new Vector2(0.9f, 2.8f), new Vector2(0f, 2.75f) }, 0f, 360f, 18, null, at));
            var (pv, pt) = k.Of(m.metal); var pipe = at + new Vector3(4.6f, 0f, -0.8f);
            Sweep(pv, pt, new[] { pipe, pipe + Vector3.up * 0.9f, pipe + new Vector3(-0.4f, 1.0f, 0f) }, 0.07f, false, 6);
            var (wv, wt) = k.Of(m.glowSoft); Append(wv, wt, Lathe(new[] { new Vector2(0f, 0.47f), new Vector2(3.15f, 0.47f) }, 0f, 360f, 32, null, at));   // the still water
            Sweep(wv, wt, new[] { pipe + new Vector3(-0.42f, 0.98f, 0f), pipe + new Vector3(-0.44f, 0.05f, 0f) }, 0.018f, false, 4);   // the trickle
        }

        /// <summary>The old transit tower: a tapering lattice with a gantry arm at the top and the ball hanging from it on a cable.</summary>
        static Transform TransitTower(AK k, CtMats m, Vector3 at, float h, Transform parent)
        {
            var (v, t) = k.Of(m.metal);
            for (int leg = 0; leg < 4; leg++)
            {
                float a = leg * 90f + 45f; var b = at + Quaternion.Euler(0f, a, 0f) * Vector3.forward * 5f; var top = at + Quaternion.Euler(0f, a, 0f) * Vector3.forward * 1.2f + Vector3.up * h;
                Sweep(v, t, new[] { b, top }, 0.28f, false, 6);
            }
            for (float y = 4f; y < h; y += 5f) { float r = Mathf.Lerp(5f, 1.2f, y / h) * 0.72f; var ring = new List<Vector3>(); for (int i = 0; i <= 4; i++) ring.Add(at + Quaternion.Euler(0f, i * 90f + 45f, 0f) * Vector3.forward * r * 1.41f + Vector3.up * y); Sweep(v, t, ring, 0.14f, false, 4); }
            var armEnd = at + new Vector3(9f, h + 1.2f, 0f);
            Sweep(v, t, new[] { at + Vector3.up * (h + 1.2f) + Vector3.left * 2f, armEnd }, 0.35f, false, 6);
            var ball = new GameObject("TransitBall").transform; ball.SetParent(parent, false); ball.position = armEnd;
            var bv = new List<Vector3>(); var bt = new List<int>();
            Sweep(bv, bt, new[] { Vector3.zero, Vector3.down * 14f }, 0.07f, false, 4);
            Ellipsoid(bv, bt, Vector3.down * 16.4f, Vector3.one * 2.4f, 10, 16);
            MeshObject("Ball", SaveMesh(SmoothFinish(bv, bt), "Cat_TransitBall"), m.cardLight, ball, true);
            var lv = new List<Vector3>(); var lt = new List<int>(); Ellipsoid(lv, lt, Vector3.down * 16.4f, Vector3.one * 2.46f, 6, 12, d => Mathf.Abs(d.y) > 0.12f);
            MeshObject("BallBand", SaveMesh(SmoothFinish(lv, lt), "Cat_TransitBallBand"), m.glowSoft, ball, false);
            return ball;
        }

        /// <summary>The leaning tree of vision 7: a thick trunk leaning hard toward +x from its root (-x), the long low branch
        /// reaching on almost level to the right, smaller limbs up into a broad crown of leaf clusters.</summary>
        static void LeaningTree(AK k, CtMats m, Vector3 at, float s)
        {
            var (bv, bt) = k.Of(m.bark);
            Vector3 P(float x, float y, float z) => at + new Vector3(x, y, z) * s;
            void Limb(IList<Vector3> pts, float r0, float r1)
            {
                for (int i = 0; i < pts.Count - 1; i++) { float u0 = i / (float)(pts.Count - 1), u1 = (i + 1) / (float)(pts.Count - 1); var seg = new[] { pts[i], pts[i + 1] }; Sweep(bv, bt, seg, Mathf.Lerp(r0, r1, (u0 + u1) * 0.5f) * s, false, 8); }
                for (int i = 1; i < pts.Count - 1; i++) Ellipsoid(bv, bt, pts[i], Vector3.one * Mathf.Lerp(r0, r1, i / (float)(pts.Count - 1)) * s, 4, 8);
            }
            // the root flare and the trunk leaning right
            Append(bv, bt, Lathe(new[] { new Vector2(0f, 0f), new Vector2(2.6f * s, 0f), new Vector2(1.5f * s, 0.8f * s), new Vector2(1.15f * s, 1.6f * s), new Vector2(0f, 1.6f * s) }, 0f, 360f, 14, null, at));
            var trunk = new[] { P(0f, 0f, 0f), P(0.6f, 2.5f, 0f), P(1.8f, 4.6f, 0.2f), P(3.6f, 6.2f, 0.3f), P(6f, 7.4f, 0.2f) };
            Limb(trunk, 1.2f, 0.75f);
            // the long low branch: from the bend, out to the right almost level, dipping and rising at its tip
            var low = new[] { P(4.6f, 6.6f, 0.3f), P(8f, 6.2f, 0.6f), P(12f, 5.4f, 0.8f), P(16f, 4.6f, 1.0f), P(19.5f, 4.2f, 1.1f), P(22.5f, 4.5f, 1.2f) };
            Limb(low, 0.5f, 0.14f);
            // upper limbs into the crown
            Limb(new[] { P(6f, 7.4f, 0.2f), P(7.5f, 9.6f, -0.6f), P(8.6f, 11.6f, -0.9f) }, 0.55f, 0.22f);
            Limb(new[] { P(5.2f, 7.1f, 0.2f), P(3.8f, 9.4f, 0.8f), P(1.6f, 11f, 1.2f) }, 0.5f, 0.2f);
            Limb(new[] { P(6f, 7.4f, 0.2f), P(9f, 8.4f, 1.4f), P(12.4f, 9.4f, 1.8f) }, 0.45f, 0.16f);
            Limb(new[] { P(2.4f, 5.4f, 0.2f), P(0.4f, 7.6f, -0.4f), P(-2.4f, 9f, -0.6f) }, 0.45f, 0.16f);
            var (lv, lt) = k.Of(m.leaf);
            var rnd = new System.Random(77);
            // the crown: clusters over the upper limbs (broad, flat-topped), and a lighter spray along the low branch's tip
            for (int i = 0; i < 46; i++)
            {
                var c = P(-3f + (float)rnd.NextDouble() * 18f, 9.4f + (float)rnd.NextDouble() * 3.4f, -3.5f + (float)rnd.NextDouble() * 7f);
                Ellipsoid(lv, lt, c, new Vector3(1.9f, 1.0f, 1.8f) * s * (0.7f + (float)rnd.NextDouble() * 0.6f), 4, 8);
            }
            for (int i = 0; i < 12; i++) { var c = P(15f + i * 0.75f, 4.8f + (float)rnd.NextDouble() * 0.8f, 0.4f + (float)rnd.NextDouble() * 1.6f); Ellipsoid(lv, lt, c, new Vector3(1.2f, 0.6f, 1.1f) * s, 4, 8); }
        }

        /// <summary>The far hills: soft rounded ridges in layers behind the meadow (west), fading into the haze.</summary>
        static void HillLine(AK k, CtMats m, Vector3 c, float r, float h, int seed)
        {
            var (v, t) = k.Of(m.hill); var rnd = new System.Random(seed);
            int n = 60; var top = new List<Vector3>(); var bottom = new List<Vector3>();
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Lerp(100f, 260f, i / (float)n) * Mathf.Deg2Rad;
                float hh = h * (0.55f + 0.45f * Mathf.PerlinNoise(i * 0.12f + seed, 0.3f)) * Mathf.Sin(i / (float)n * Mathf.PI);
                var p = c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                top.Add(p + Vector3.up * hh); bottom.Add(p + Vector3.down * 4f);
            }
            for (int i = 0; i < n; i++) Q(v, t, bottom[i], top[i], top[i + 1], bottom[i + 1]);
        }

        // ---------------------------------------------------------------- the Grand Circuit
        /// <summary>The course road: a ribbon (top + side skirts) along the loop, rails on both edges, supports where it is raised.</summary>
        static void CourseRoad(Transform parent, CtMats m)
        {
            var pts = K.Points; int n = pts.Count;
            var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
            float hw = K.HalfWidth;
            for (int i = 0; i <= n; i++)
            {
                var p = K.At(i); var d = K.Dir(i); var side = Vector3.Cross(Vector3.up, d).normalized;
                v.Add(p - side * hw + Vector3.up * 0.02f); v.Add(p + side * hw + Vector3.up * 0.02f); v.Add(p - side * hw + Vector3.down * 0.6f); v.Add(p + side * hw + Vector3.down * 0.6f);
                float s = i * K.Step / 8f; uv.Add(new Vector2(0f, s)); uv.Add(new Vector2(1f, s)); uv.Add(new Vector2(0f, s)); uv.Add(new Vector2(1f, s));
            }
            for (int i = 0; i < n; i++)
            {
                int a = i * 4, b = (i + 1) * 4;
                t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });                       // top
                t.AddRange(new[] { a + 2, b + 2, a, a, b + 2, b });                       // left skirt
                t.AddRange(new[] { a + 1, b + 1, a + 3, a + 3, b + 1, b + 3 });           // right skirt
                t.AddRange(new[] { a + 2, a + 3, b + 2, a + 3, b + 3, b + 2 });           // underside
            }
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0); mesh.RecalculateNormals();
            MeshObject("CourseRoad", SaveMesh(mesh, "Cat_CourseRoad"), m.road, parent, true);
            // rails (rope-wrapped, scratching-post style) on both edges; center dashes; supports under raised parts
            var k = new AK();
            var (rv, rt) = k.Of(m.rail);
            foreach (var sg in new[] { -1f, 1f })
            {
                var rail = new List<Vector3>();
                for (int i = 0; i <= n; i += 2) { var p = K.At(i); var side = Vector3.Cross(Vector3.up, K.Dir(i)).normalized; rail.Add(p + side * sg * (hw + 0.25f) + Vector3.up * 0.75f); }
                Sweep(rv, rt, rail, 0.16f, false, 6);
                for (int i = 0; i < n; i += 6) { var p = K.At(i); var side = Vector3.Cross(Vector3.up, K.Dir(i)).normalized; Sweep(rv, rt, new[] { p + side * sg * (hw + 0.25f), p + side * sg * (hw + 0.25f) + Vector3.up * 0.8f }, 0.09f, false, 5); }
            }
            var (dv, dt) = k.Of(m.paper);
            for (int i = 0; i < n; i += 5) { var p = K.At(i) + Vector3.up * 0.04f; var d = K.Dir(i); var side = Vector3.Cross(Vector3.up, d).normalized; Q(dv, dt, p - side * 0.12f, p + side * 0.12f, p + side * 0.12f + d * 3.2f, p - side * 0.12f + d * 3.2f, false); }
            var (sv, st) = k.Of(m.sisal);
            for (int i = 0; i < n; i += 9)
            {
                var p = K.At(i); if (p.y < 1.5f) continue;
                if (K.Section[i] == 1)
                {
                    // the spiral hangs off its column: a rope-wrapped bracket from the column to the road's inner edge
                    var toC = new Vector3(K.ColumnC.x - p.x, 0f, K.ColumnC.z - p.z); float dc = toC.magnitude;
                    if (dc > 1f && dc < K.ColumnR + 4f) { toC /= dc; var (bv, bt) = k.Of(m.sisal); Sweep(bv, bt, new[] { p + toC * (hw - 0.3f) + Vector3.down * 0.5f, new Vector3(K.ColumnC.x, p.y - 2.2f, K.ColumnC.z) - toC * (K.ColumnR - 12f) }, 0.3f, false, 6); }
                }
                var side = Vector3.Cross(Vector3.up, K.Dir(i)).normalized;
                foreach (var sg in new[] { -1f, 1f })
                {
                    var q = new Vector3(p.x, 0f, p.z) + side * sg * (hw - 1.2f);
                    // never a post through another part of the course (the spiral's lower turns pass under its upper ones)
                    bool clash = false; for (int j = 0; j < n && !clash; j++) { var o = K.At(j); if (o.y < p.y - 2f && new Vector2(o.x - q.x, o.z - q.z).sqrMagnitude < (hw + 1.5f) * (hw + 1.5f)) clash = true; }
                    if (clash) continue;
                    Append(sv, st, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.55f, 0f), new Vector2(0.45f, 0.3f), new Vector2(0.45f, p.y - 0.6f), new Vector2(0f, p.y - 0.6f) }, 0f, 360f, 10, null, q));
                }
            }
            k.Save("Course", parent);
            // the monumental column of the scratching-post ascent
            var ck = new AK(); ScratchPost(ck, m, K.ColumnC, K.ColumnR - 12f, K.SpiralTop + 36f, 5); ck.Save("Column", parent);
        }

        /// <summary>A gate: two banner posts and an arch of hanging ornaments (balls, bells, feathers on cords); its glow ring is
        /// returned (the next gate glows softly in the race).</summary>
        static Renderer CourseGate(Transform parent, CtMats m, int i, int n)
        {
            var p = K.At(i); var d = K.Dir(i); var side = Vector3.Cross(Vector3.up, d).normalized; float hw = K.HalfWidth + 0.9f;
            var k = new AK(); var (pv, pt) = k.Of(m.sisal); var (cv, ct) = k.Of(m.cardDark); var (ov, ot) = k.Of(m.fabric);
            foreach (var sg in new[] { -1f, 1f }) Append(pv, pt, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.45f, 0f), new Vector2(0.35f, 0.4f), new Vector2(0.35f, 7.6f), new Vector2(0.6f, 7.8f), new Vector2(0f, 8.1f) }, 0f, 360f, 10, null, p + side * sg * hw));
            var arch = new List<Vector3>(); for (int a = 0; a <= 16; a++) { float u = a / 16f; arch.Add(p + side * Mathf.Lerp(-hw, hw, u) + Vector3.up * (7.6f + Mathf.Sin(u * Mathf.PI) * 1.6f)); }
            Sweep(cv, ct, arch, 0.12f, false, 6);
            for (int a = 1; a < 16; a += 2)
            {
                var top = arch[a]; float len = 1.2f + (a % 3) * 0.7f;
                Sweep(cv, ct, new[] { top, top + Vector3.down * len }, 0.02f, false, 3);
                if (a % 4 == 1) Ellipsoid(ov, ot, top + Vector3.down * (len + 0.25f), Vector3.one * 0.25f, 4, 8);
                else Ellipsoid(ov, ot, top + Vector3.down * (len + 0.35f), new Vector3(0.1f, 0.4f, 0.1f), 3, 6);
            }
            k.Save($"Gate{n:00}", parent);
            var gv = new List<Vector3>(); var gt = new List<int>(); Sweep(gv, gt, arch.Select(x => x + Vector3.down * 0.25f).ToList(), 0.07f, false, 5);
            var glow = MeshObject($"Gate{n:00}_Glow", SaveMesh(SmoothFinish(gv, gt), $"Cat_Gate{n:00}_Glow"), new Material(m.glowSoft) { name = $"Cat_gate_glow_{n:00}" }, parent, false);
            return glow.GetComponent<Renderer>();
        }

        /// <summary>A swinging toy over the toy boulevard: an arch frame and a big soft toy on a rope (its pivot moves at runtime).</summary>
        static Transform CourseToy(Transform parent, CtMats m, int i, int n)
        {
            var p = K.At(i); var d = K.Dir(i); var side = Vector3.Cross(Vector3.up, d).normalized; float hw = K.HalfWidth + 1.2f;
            var k = new AK(); var (fv, ft) = k.Of(m.sisal);
            foreach (var sg in new[] { -1f, 1f }) Append(fv, ft, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0.4f, 0.3f), new Vector2(0.4f, 10.5f), new Vector2(0f, 10.6f) }, 0f, 360f, 10, null, p + side * sg * hw));
            var (bv, bt) = k.Of(m.card); MBox(bv, bt, p + Vector3.up * 10.8f, new Vector3(hw * 2f + 1.2f, 0.8f, 0.9f), Quaternion.LookRotation(side).eulerAngles.y, 0.01f);
            // a warning stripe of glow on the beam (the readable signal: it swings across the road)
            var (gv, gt) = k.Of(m.glowSoft); Sweep(gv, gt, new[] { p - side * hw + Vector3.up * 10.35f, p + side * hw + Vector3.up * 10.35f }, 0.06f, false, 4);
            k.Save($"ToyArch{n}", parent);
            var pivot = new GameObject($"Toy{n}").transform; pivot.SetParent(parent, false); pivot.position = p + Vector3.up * 10.4f; pivot.rotation = Quaternion.LookRotation(d);
            var tv = new List<Vector3>(); var tt = new List<int>();
            Sweep(tv, tt, new[] { Vector3.zero, Vector3.down * 7.4f }, 0.06f, false, 4);
            // a big toy mouse (body, ears, tail) at the end of the rope
            Ellipsoid(tv, tt, Vector3.down * 8.4f, new Vector3(1.1f, 1.0f, 1.6f), 6, 10);
            Ellipsoid(tv, tt, Vector3.down * 7.6f + Vector3.forward * 0.9f + Vector3.left * 0.6f, new Vector3(0.12f, 0.4f, 0.4f), 3, 6);
            Ellipsoid(tv, tt, Vector3.down * 7.6f + Vector3.forward * 0.9f + Vector3.right * 0.6f, new Vector3(0.12f, 0.4f, 0.4f), 3, 6);
            Sweep(tv, tt, new[] { Vector3.down * 8.5f + Vector3.back * 1.5f, Vector3.down * 8.9f + Vector3.back * 2.6f, Vector3.down * 8.3f + Vector3.back * 3.4f }, 0.07f, false, 4);
            MeshObject("ToyBody", SaveMesh(SmoothFinish(tv, tt), $"Cat_Toy{n}"), m.fabric, pivot, true);
            return pivot;
        }
    }
}
