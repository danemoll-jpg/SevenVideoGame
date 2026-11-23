using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using F = Seven.FinaleLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF30 — Dan's 1.0.0-rc1 review of the ending: the finale's new set pieces, built in code from the kit (no new models):
    /// Tunneler's mining rig (item 6: ring-segmented hull, riveted plates, the spiral-drilled rim, hover thrusters, work lights,
    /// cables and hoses into him, two articulated arms with small spiral drills, the yoke), the big mining drills boring into the
    /// old structure round the chamber and along the service route (item 6), the archive's controls in the chamber's style
    /// (item 11: a carved pedestal rising into a slanted face, the rune glyphs engraved on its inset panel, a ring of light-lines in
    /// grooves running to the floor conduits, a palm-plate crystal node; Tunneler's crude housing and cables bolted on), the
    /// barrier frame's light strips (item 10) and the dead cables that lead to each power objective (item 9).
    /// </summary>
    public static partial class FightDemoBuilder
    {
        static void RotAppended(List<Vector3> v, int from, Quaternion q, Vector3 pivot) { for (int i = from; i < v.Count; i++) v[i] = pivot + q * (v[i] - pivot); }

        /// <summary>A spiral drill along +y from the origin (base radius r, length len): a cone with a raised helical flute.</summary>
        static void SpiralDrill(AK k, Material metal, Material dark, Vector3 at, float r, float len, int turns = 5)
        {
            var (v, t) = k.Of(metal);
            Append(v, t, Lathe(new[] { new Vector2(0f, -0.05f * len), new Vector2(r * 1.05f, -0.05f * len), new Vector2(r, 0f), new Vector2(r * 0.12f, len * 0.94f), new Vector2(0f, len) }, 0f, 360f, 18, null, at));
            var (fv, ft) = k.Of(dark);
            var path = new List<Vector3>(); int n = turns * 18;
            for (int i = 0; i <= n; i++) { float u = i / (float)n; float a = u * turns * Mathf.PI * 2f; float rr = Mathf.Lerp(r * 1.02f, r * 0.14f, u); path.Add(at + new Vector3(Mathf.Sin(a) * rr, u * len * 0.92f, Mathf.Cos(a) * rr)); }
            Sweep(fv, ft, path, Mathf.Max(0.012f, r * 0.09f), false, 5);
        }

        // ---------------------------------------------------------------- item 6: Tunneler's mining rig
        static Seven.TunnelerRig BuildTunnelerRig(Transform craft, MoonMats m, Material soft)
        {
            var rigT = new GameObject("MiningRig").transform; rigT.SetParent(craft, false);
            var rig = craft.gameObject.AddComponent<Seven.TunnelerRig>();
            float R = 2.3f, deck = 0.48f;
            var k = new AK();
            // the hull: stacked ring segments (like his body), a skirt of riveted plates, the deck on top
            { var (v, t) = k.Of(m.metalDark);
              Append(v, t, Lathe(new[] { new Vector2(0f, 0.04f), new Vector2(1.3f, 0f), new Vector2(1.95f, 0.1f), new Vector2(2.22f, 0.22f), new Vector2(R, 0.33f), new Vector2(R - 0.04f, 0.42f), new Vector2(2.0f, deck), new Vector2(0f, deck) }, 0f, 360f, 36));
              foreach (var (r0, y) in new[] { (1.55f, 0.11f), (1.9f, 0.17f), (2.15f, 0.25f) }) Append(v, t, Lathe(new[] { new Vector2(r0, y - 0.025f), new Vector2(r0 + 0.07f, y), new Vector2(r0, y + 0.025f) }, 0f, 360f, 36)); }
            { var (v, t) = k.Of(m.metal);
              // the deck plating: ring bands and radial plate seams
              foreach (var r0 in new[] { 0.7f, 1.25f, 1.75f }) Append(v, t, Lathe(new[] { new Vector2(r0, deck), new Vector2(r0 + 0.05f, deck + 0.025f), new Vector2(r0 + 0.1f, deck) }, 0f, 360f, 32));
              for (int i = 0; i < 12; i++) { float a = i * 30f; var d = Quaternion.Euler(0f, a, 0f) * Vector3.forward; MBox(v, t, d * 1.6f + Vector3.up * (deck + 0.012f), new Vector3(0.05f, 0.025f, 1.1f), a, 0f); }
              // rivets round the skirt
              var (rv, rt) = k.Of(m.metal); for (int i = 0; i < 48; i++) { float a = i * 7.5f * Mathf.Deg2Rad; Ellipsoid(rv, rt, new Vector3(Mathf.Sin(a) * 2.27f, 0.3f, Mathf.Cos(a) * 2.27f), Vector3.one * 0.035f, 3, 6); } }
            // the spiral-drilled rim: a coil wound round the hull's edge
            { var (v, t) = k.Of(m.metal); var coil = new List<Vector3>(); int n = 720;
              for (int i = 0; i <= n; i++) { float u = i / (float)n, th = u * Mathf.PI * 2f, ph = u * Mathf.PI * 2f * 40f; float rr = R + 0.02f + 0.07f * Mathf.Cos(ph); coil.Add(new Vector3(Mathf.Sin(th) * rr, 0.36f + 0.07f * Mathf.Sin(ph), Mathf.Cos(th) * rr)); }
              Sweep(v, t, coil, 0.022f, false, 5); }
            // hover thrusters underneath (four nozzles) + their mouths
            var thr = new List<Transform>();
            { var (v, t) = k.Of(m.metalDark);
              for (int i = 0; i < 4; i++) { float a = (45f + i * 90f) * Mathf.Deg2Rad; var c = new Vector3(Mathf.Sin(a) * 1.35f, 0f, Mathf.Cos(a) * 1.35f);
                Append(v, t, Lathe(new[] { new Vector2(0.18f, 0.05f), new Vector2(0.26f, -0.06f), new Vector2(0.32f, -0.15f), new Vector2(0.27f, -0.16f), new Vector2(0.2f, -0.06f) }, 0f, 360f, 14, null, c));
                var mouth = new GameObject("Thruster" + i).transform; mouth.SetParent(rigT, false); mouth.localPosition = c + Vector3.down * 0.16f; thr.Add(mouth); }
              var (gv, gt) = k.Of(soft); for (int i = 0; i < 4; i++) { float a = (45f + i * 90f) * Mathf.Deg2Rad; var c = new Vector3(Mathf.Sin(a) * 1.35f, -0.12f, Mathf.Cos(a) * 1.35f); Append(gv, gt, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.2f, 0f) }, 0f, 360f, 12, null, c)); } }
            // cables and hoses from the deck up into his back; the control yoke he grips
            { var (v, t) = k.Of(m.metalDark);
              foreach (var sx in new[] { -1f, 1f })
              { Sweep(v, t, new[] { new Vector3(sx * 0.9f, deck, -0.9f), new Vector3(sx * 0.7f, deck + 0.3f, -1.05f), new Vector3(sx * 0.42f, deck + 0.95f, -0.75f), new Vector3(sx * 0.3f, deck + 1.35f, -0.42f) }, 0.055f, false, 6);
                Sweep(v, t, new[] { new Vector3(sx * 1.2f, deck, -0.4f), new Vector3(sx * 1.0f, deck + 0.4f, -0.7f), new Vector3(sx * 0.5f, deck + 1.1f, -0.55f), new Vector3(sx * 0.36f, deck + 1.6f, -0.36f) }, 0.035f, false, 6); }
              Sweep(v, t, new[] { new Vector3(0f, deck, 0.95f), new Vector3(0f, deck + 0.55f, 0.85f), new Vector3(0f, deck + 0.78f, 0.62f) }, 0.06f, false, 6);
              Sweep(v, t, new[] { new Vector3(-0.48f, deck + 0.8f, 0.6f), new Vector3(0.48f, deck + 0.8f, 0.6f) }, 0.04f, false, 6);
              foreach (var sx in new[] { -0.5f, 0.5f }) Sweep(v, t, new[] { new Vector3(sx, deck + 0.8f, 0.6f), new Vector3(sx, deck + 0.95f, 0.5f) }, 0.035f, false, 6); }
            // work lights on short posts at the front
            var lights = new List<Light>();
            foreach (var sx in new[] { -0.95f, 0.95f })
            {
                var (v, t) = k.Of(m.metalDark); Sweep(v, t, new[] { new Vector3(sx, deck, 1.5f), new Vector3(sx, deck + 0.55f, 1.55f) }, 0.04f, false, 6);
                MBox(v, t, new Vector3(sx, deck + 0.65f, 1.6f), new Vector3(0.28f, 0.2f, 0.22f), 0f, 0f);
                var (gv, gt) = k.Of(soft); Ellipsoid(gv, gt, new Vector3(sx, deck + 0.65f, 1.72f), new Vector3(0.1f, 0.07f, 0.03f), 4, 8);
                var l = new GameObject("RigWorkLight").AddComponent<Light>(); l.transform.SetParent(rigT, false); l.transform.localPosition = new Vector3(sx, deck + 0.65f, 1.75f); l.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
                l.type = LightType.Spot; l.range = 14f; l.spotAngle = 55f; l.intensity = 6f; l.color = Color.white; l.shadows = LightShadows.None; lights.Add(l);
            }
            foreach (var g in k.Save("TunnelerRig", rigT, false)) g.transform.localPosition = Vector3.zero;
            // two articulated arms ending in small spiral drills (pivot at the shoulder: they swing forward on a strike)
            var arms = new List<Transform>(); var bits = new List<Transform>();
            foreach (var sx in new[] { -1f, 1f })
            {
                var arm = new GameObject(sx < 0f ? "RigArmL" : "RigArmR").transform; arm.SetParent(rigT, false); arm.localPosition = new Vector3(sx * 1.45f, deck + 0.35f, 0.1f);
                var ak = new AK(); var (v, t) = ak.Of(m.metalDark);
                MBox(v, t, Vector3.down * 0.2f, new Vector3(0.5f, 0.45f, 0.5f), 0f, 0f);                      // the shoulder mount
                Sweep(v, t, new[] { Vector3.zero, new Vector3(sx * 0.35f, 0.4f, 0.75f) }, 0.11f, false, 8);       // upper arm
                Ellipsoid(v, t, new Vector3(sx * 0.35f, 0.4f, 0.75f), Vector3.one * 0.15f, 5, 8);                 // elbow
                Sweep(v, t, new[] { new Vector3(sx * 0.35f, 0.4f, 0.75f), new Vector3(sx * 0.1f, 0.05f, 1.55f) }, 0.09f, false, 8);   // forearm
                var (pv, pt) = ak.Of(m.metal); Sweep(pv, pt, new[] { new Vector3(sx * 0.2f, 0.15f, 0.2f), new Vector3(sx * 0.28f, 0.42f, 0.7f) }, 0.035f, false, 5);   // a hydraulic ram
                foreach (var g in ak.Save(arm.name, arm, false)) g.transform.localPosition = Vector3.zero;
                var bit = new GameObject(sx < 0f ? "RigArmDrillL" : "RigArmDrillR").transform; bit.SetParent(arm, false); bit.localPosition = new Vector3(sx * 0.1f, 0.05f, 1.55f);
                var bk = new AK(); SpiralDrill(bk, m.metal, m.metalDark, Vector3.zero, 0.2f, 0.75f, 4);
                var holder = new GameObject("Bit").transform; holder.SetParent(bit, false); holder.localRotation = Quaternion.Euler(90f, 0f, 0f);
                foreach (var g in bk.Save(bit.name, holder, false)) g.transform.localPosition = Vector3.zero;
                arms.Add(arm); bits.Add(bit);
            }
            rig.Hull = rigT; rig.Arms = arms.ToArray(); rig.ArmDrills = bits.ToArray(); rig.WorkLights = lights.ToArray(); rig.Thrusters = thr.ToArray();
            rig.Radius = R + 0.08f; rig.DeckTop = deck; rig.DeckR = 1.85f;
            Log($"QF30 (6): Tunneler's mining rig built - ring-segmented hull r {R} m, deck {deck} m, the spiral-drilled rim, {thr.Count} thrusters, {lights.Count} work lights, cables into him, two arms with spiral drills, the yoke");
            return rig;
        }

        // ---------------------------------------------------------------- item 6: the big mining drills boring into the old structure
        /// <summary>A drill boring in through a wall: the big spiral bit (spins about its local z = the boring direction) on its drive
        /// housing, broken stone round the hole, a mining work light. Returns the bit (spun at runtime).</summary>
        static Transform WallDrill(string name, Vector3 at, Vector3 dir, float scale, MoonMats m, Transform parent)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false); root.position = at; root.rotation = Quaternion.LookRotation(dir);
            var k = new AK();
            { var (v, t) = k.Of(m.metalDark);
              // the drive housing behind the bit (sticking out of the wall behind / through it), riveted collars
              Sweep(v, t, new[] { new Vector3(0f, 0f, -3.6f * scale), new Vector3(0f, 0f, -0.4f * scale) }, 0.95f * scale, false, 12);
              foreach (var z in new[] { -3.2f, -2.2f, -1.2f }) Append(v, t, Lathe(new[] { new Vector2(0.95f * scale, -0.08f * scale), new Vector2(1.08f * scale, 0f), new Vector2(0.95f * scale, 0.08f * scale) }, 0f, 360f, 16, null, Vector3.zero).Rotated(Quaternion.Euler(90f, 0f, 0f), new Vector3(0f, 0f, z * scale))); }
            { var (v, t) = k.Of(m.rockDark);
              // broken stone round the hole (the old masonry giving way)
              var rnd = new System.Random(name.GetHashCode());
              for (int i = 0; i < 14; i++) { float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = (1.25f + (float)rnd.NextDouble() * 0.9f) * scale; Ellipsoid(v, t, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, -0.3f * scale + (float)rnd.NextDouble() * 0.3f), new Vector3(0.35f, 0.25f, 0.3f) * scale * (0.6f + (float)rnd.NextDouble()), 3, 6); } }
            foreach (var g in k.Save(name + "Housing", root, false)) g.transform.localPosition = Vector3.zero;
            var bit = new GameObject("WallDrillBit").transform; bit.SetParent(root, false); bit.localPosition = new Vector3(0f, 0f, -0.45f * scale);
            var bk = new AK(); SpiralDrill(bk, m.metal, m.metalDark, Vector3.zero, 1.05f * scale, 2.6f * scale, 5);
            var holder = new GameObject("Bit").transform; holder.SetParent(bit, false); holder.localRotation = Quaternion.Euler(90f, 0f, 0f);
            foreach (var g in bk.Save(name + "Bit", holder, false)) g.transform.localPosition = Vector3.zero;
            var lamp = PointLight("MiningLight", at + dir * 1.6f * scale + Vector3.up * 1.4f * scale, 10f * scale, 1.6f, root);
            return bit;
        }

        // ---------------------------------------------------------------- item 11: the archive's controls (the chamber's own style)
        /// <summary>A control in the lunar archive's style; returns (root, light-lines renderer, crystal node renderer, the mining
        /// add-ons' root). <paramref name="face"/> = where its slanted face looks (toward the room).</summary>
        static (Transform root, Renderer lines, Renderer node, Transform addOns) ArchiveConsole(string name, Vector3 at, Vector3 face, Vector3 conduitTo, MoonMats m, Material stone, Material stoneDark, Material soft, Transform parent, bool bolted)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false); root.position = at; root.rotation = Quaternion.LookRotation(new Vector3(face.x, 0f, face.z));
            var k = new AK();
            { var (v, t) = k.Of(stone);
              // the carved pedestal: a stepped round foot rising into a tapering column
              Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.55f, 0f), new Vector2(0.55f, 0.08f), new Vector2(0.44f, 0.12f), new Vector2(0.36f, 0.3f), new Vector2(0.3f, 0.82f), new Vector2(0.4f, 0.9f), new Vector2(0f, 0.9f) }, 0f, 360f, 20));
              // the slanted face (tilted 35 deg toward the room)
              int s0 = v.Count; MBox(v, t, new Vector3(0f, 1.02f, 0.02f), new Vector3(0.95f, 0.14f, 0.66f), 0f, 0f); RotAppended(v, s0, Quaternion.Euler(-35f, 0f, 0f), new Vector3(0f, 1.0f, 0f)); }
            { var (v, t) = k.Of(stoneDark);
              // the inset panel on the face
              int s0 = v.Count; MBox(v, t, new Vector3(0f, 1.095f, 0.02f), new Vector3(0.78f, 0.02f, 0.5f), 0f, 0f); RotAppended(v, s0, Quaternion.Euler(-35f, 0f, 0f), new Vector3(0f, 1.0f, 0f)); }
            { var (v, t) = k.Of(m.inscription);
              // the rune shapes engraved as glyphs: a triangle, a square, a pentagon, a hexagon round the node
              int s0 = v.Count;
              void Poly(Vector2 c, int sides, float r, float rot) { var p = new List<Vector3>(); for (int i = 0; i <= sides; i++) { float a = (rot + i * 360f / sides) * Mathf.Deg2Rad; p.Add(new Vector3(c.x + Mathf.Cos(a) * r, 1.108f, 0.02f + c.y + Mathf.Sin(a) * r)); } Sweep(v, t, p, 0.008f, false, 4); }
              Poly(new Vector2(-0.27f, 0.12f), 3, 0.07f, 90f); Poly(new Vector2(0.27f, 0.12f), 4, 0.065f, 45f); Poly(new Vector2(-0.27f, -0.12f), 5, 0.07f, 90f); Poly(new Vector2(0.27f, -0.12f), 6, 0.07f, 0f);
              RotAppended(v, s0, Quaternion.Euler(-35f, 0f, 0f), new Vector3(0f, 1.0f, 0f)); }
            if (bolted)
            {
                // Tunneler's add-ons: a crude metal housing bolted to the column's side, cables running from it down into the floor
                var (v, t) = k.Of(m.metalDark);
                MBox(v, t, new Vector3(0.42f, 0.55f, -0.05f), new Vector3(0.26f, 0.5f, 0.34f), 0f, 0f);
                foreach (var (y, z) in new[] { (0.4f, 0.08f), (0.5f, -0.12f) }) Sweep(v, t, new[] { new Vector3(0.52f, y, z), new Vector3(0.75f, y - 0.2f, z), new Vector3(0.95f, 0.03f, z - 0.2f), new Vector3(1.6f, 0.03f, z - 0.5f) }, 0.025f, false, 5);
                var (rv, rt) = k.Of(m.metal); foreach (var y in new[] { 0.4f, 0.7f }) foreach (var z in new[] { 0.09f, -0.19f }) Ellipsoid(rv, rt, new Vector3(0.56f, y, z), Vector3.one * 0.022f, 3, 6);
            }
            var made = k.Save(name, root, false); foreach (var g in made) g.transform.localPosition = Vector3.zero;
            // the light-lines: a ring in a groove round the foot and three grooves running out toward the floor conduits ("Lines": never an occluder)
            var lv = new List<Vector3>(); var lt = new List<int>();
            { var ring = new List<Vector3>(); for (int i = 0; i <= 32; i++) { float a = i / 32f * Mathf.PI * 2f; ring.Add(new Vector3(Mathf.Sin(a) * 0.72f, 0.012f, Mathf.Cos(a) * 0.72f)); } Sweep(lv, lt, ring, 0.018f, false, 4);
              var toC = root.InverseTransformPoint(conduitTo); toC.y = 0f; var d = toC.normalized; var sd = Vector3.Cross(Vector3.up, d);
              foreach (var o in new[] { -0.18f, 0f, 0.18f }) Sweep(lv, lt, new[] { d * 0.72f + sd * o + Vector3.up * 0.012f, d * Mathf.Min(2.6f, toC.magnitude) + sd * o * 0.4f + Vector3.up * 0.012f }, 0.016f, false, 4);
              // and up the column onto the face's edge
              foreach (var sx in new[] { -0.2f, 0.2f }) Sweep(lv, lt, new[] { new Vector3(sx, 0.12f, 0.3f), new Vector3(sx * 0.85f, 0.85f, 0.28f) }, 0.012f, false, 4); }
            var lines = MeshObject(name + "Lines", SaveMesh(SmoothFinish(lv, lt), "Fin_" + name + "Lines"), new Material(soft) { name = "Fin_" + name + "_lines" }, root, false).GetComponent<Renderer>();
            // the palm-plate crystal node in the face's centre
            var nv = new List<Vector3>(); var nt = new List<int>(); Ellipsoid(nv, nt, new Vector3(0f, 1.12f, 0.02f), new Vector3(0.09f, 0.05f, 0.09f), 5, 8); RotAppended(nv, 0, Quaternion.Euler(-35f, 0f, 0f), new Vector3(0f, 1.0f, 0f));
            var node = MeshObject(name + "Node", SaveMesh(SmoothFinish(nv, nt), "Fin_" + name + "Node"), new Material(soft) { name = "Fin_" + name + "_node" }, root, false).GetComponent<Renderer>();
            PointLight(name + "Lamp", at + root.forward * 0.9f + Vector3.up * 1.9f, 4.5f, 2.4f, root);   // a soft work light on it: it reads against the chamber's haze
            return (root, lines, node, root);
        }

        /// <summary>item 9: a dead cable on the floor leading from where the objective is given to its control (it lights when done).</summary>
        static Renderer DeadCable(string name, IList<Vector3> path, MoonMats m, Material soft, Transform parent)
        {
            var k = new AK(); var (v, t) = k.Of(m.metalDark); Sweep(v, t, path, 0.04f, false, 6); foreach (var g in k.Save(name, parent, false)) g.transform.localPosition = Vector3.zero;
            var lv = new List<Vector3>(); var lt = new List<int>(); Sweep(lv, lt, path.Select(p => p + Vector3.up * 0.035f).ToList(), 0.012f, false, 4);
            return MeshObject(name + "Light", SaveMesh(SmoothFinish(lv, lt), "Fin_" + name + "Light"), new Material(soft) { name = "Fin_" + name + "_light" }, parent, false).GetComponent<Renderer>();
        }
    }

    static class QF30MeshExt
    {
        /// <summary>A copy of the mesh rotated by q and moved by offset.</summary>
        public static Mesh Rotated(this Mesh m, Quaternion q, Vector3 offset)
        {
            var c = Object.Instantiate(m); var v = c.vertices; for (int i = 0; i < v.Length; i++) v[i] = q * v[i] + offset; c.vertices = v; c.RecalculateNormals(); c.RecalculateBounds(); return c;
        }
    }
}

namespace Seven.EditorTools
{
    public static partial class FightDemoBuilder
    {
        /// <summary>QF30 (5) build check DRILL AXIS: spin the drill head in the editor (talking, a strike, the spin-down) and measure the
        /// cone's own axis from its mesh before and after - a drill turns about its own axis (the axis stays put); a tumble swings it.
        /// Fails the build on a wobble over 2 deg, a reversal, or an axis more than 35 deg off his body's up.</summary>
        static void DrillAxisCheck(string scene, Seven.DrillHead dh)
        {
            dh.ResetSpin();
            var a0 = dh.ConeAxisWorld(); float worst = 0f; float lastAngle = 0f; int back = 0;
            for (int i = 0; i < 240; i++)
            {
                dh.Want = i < 80 ? 0f : i < 160 ? 1400f : 0f; dh.Talk = i < 80 ? 0.6f : 0f;
                dh.Step(1f / 60f);
                if (dh.Angle < lastAngle - 1e-3f) back++; lastAngle = dh.Angle;
                if (i % 20 == 0) worst = Mathf.Max(worst, Vector3.Angle(a0, dh.ConeAxisWorld()));
            }
            float up = dh.Body != null ? Vector3.Angle(a0, dh.Body.up) : 0f;
            int turns = dh.Turns, rev = dh.Reversals + back;
            dh.ResetSpin();
            bool ok = worst < 2f && rev == 0 && up < 35f && turns >= 1;
            var line = $"DRILL AXIS (QF30 5) {scene}: the drill head turned {turns} full turns about the cone's own axis - axis wobble {worst:0.00} deg (must be < 2), reversals {rev} (must be 0), cone axis vs his body's up {up:0.0} deg - {(ok ? "OK" : "FAIL")}";
            Log(line); if (!ok) BuildFailures.Add(line);
        }
    }
}

namespace Seven.EditorTools
{
    public static partial class FightDemoBuilder
    {
        /// <summary>QF30 (4) build check STAGING MARKS (the finale): every staged spot is clear - a ray down finds the floor it should stand
        /// on (not a prop's top) and a body-sized capsule there touches no geometry (the platform's base, a socket, a control, a wall).
        /// Temporary mesh colliders on every renderer of the level, removed after.</summary>
        static void StagingMarksCheck()
        {
            var temp = new List<MeshCollider>();
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
                var n = mf.name; if (n.Contains("Lines") || n.Contains("Light") || n.Contains("Glow") || n.Contains("Projection") || n.Contains("Barrier") && n.Contains("Field") || n == "ArchiveConduits" || n.Contains("mining_dust") || n.Contains("Surface")) continue;   // glowing floor lines, dust and the archive's light well are not solid
                var r = mf.GetComponent<Renderer>(); if (r == null || r.sharedMaterial == null || r.sharedMaterial.renderQueue > 2500) continue;
                if (mf.transform.root.name == "7" || mf.transform.root.name == "Cat" || mf.GetComponentInParent<Seven.ScreeGuard>() != null || mf.GetComponentInParent<Seven.TunnelerRig>() != null) continue;
                temp.Add(mf.gameObject.AddComponent<MeshCollider>());
            }
            Physics.SyncTransforms();
            bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            int bad = 0; var lines = new List<string>();
            foreach (var (what, p, r, h, floor) in Seven.FinaleLayout.Marks())
            {
                string why = null;
                var hits = Physics.RaycastAll(new Vector3(p.x, floor + 2.2f, p.z), Vector3.down, 4f);
                if (hits.Length > 0) { var hit = hits.OrderByDescending(h => h.point.y).First(); if (Mathf.Abs(hit.point.y - floor) > 0.03f) why = $"the ray down lands on {hit.collider.name} at {hit.point.y - floor:+0.00;-0.00} m"; }
                else why = "no floor under it";
                var a = new Vector3(p.x, floor + r + 0.06f, p.z); var b = new Vector3(p.x, floor + Mathf.Max(r + 0.07f, h - r), p.z);
                var cols = Physics.OverlapCapsule(a, b, r);
                foreach (var c in cols) { why = (why != null ? why + "; " : "") + "the body is inside " + c.name; break; }
                if (why != null) bad++;
                lines.Add($"  {what} at {p}: {(why ?? "clear")}");
            }
            Physics.queriesHitBackfaces = bf;
            foreach (var c in temp) Object.DestroyImmediate(c);
            var head = $"STAGING MARKS (QF30 4) Finale: {Seven.FinaleLayout.Marks().Length} staged spots (7, Panther, the Scree) vs every prop: inside geometry or on a prop {bad} (must be 0) - {(bad == 0 ? "OK" : "FAIL")}";
            Log(head); foreach (var l in lines) Log(l);
            if (bad > 0) BuildFailures.Add(head);
        }
    }
}
