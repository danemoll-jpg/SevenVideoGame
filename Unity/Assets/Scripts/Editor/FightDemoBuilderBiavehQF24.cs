using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using B = Seven.BiavehLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF24 - Dan's 0.17.2 review, the mothership's look:
    ///  (10) the relays redesigned (<see cref="RelayPylon"/>: a slender pearly pylon, a crystal node in a ring of petals, a
    ///       resonance plate for her palm, a floor channel the restored light runs along);
    ///  (12) the garden with Venus's real vegetation (Tree / Shrub / the swaying Seven/Grass), white-rimmed curving beds,
    ///       flowering trees, hanging plants, the seedlings' species in a nursery bed (<see cref="BiaGarden"/>);
    ///  (16) purposeful detail (<see cref="BiaDetail"/>): the transfer chamber (ribs + inset light channels, service platforms
    ///       with railings and floor lights, the partition machinery waiting above, maintenance openings, the grand carved
    ///       archive door with memory light behind it and Ratwing's damage, conduits up the shaft, hanging light globes, the city
    ///       beyond); the districts along the roads (planters, seating, glyph signs, memory-crystal shrines - outside the walkable
    ///       areas), the quiet passage's shielding ribs, the octahedron chamber's empty positions.
    /// Everything decorative stands outside the walkable zones or high overhead (no new obstacles on a route).
    /// </summary>
    public static partial class FightDemoBuilder
    {
        // ------------------------------------------------------------------------------------------------ (10) the relays
        static Renderer RelayPylon(BiMats m, Vector3 at, Vector3 facing, Vector3 district, string lampName, Transform parent)
        {
            facing.y = 0f; facing.Normalize(); district.y = 0f; district = district.sqrMagnitude > 0.01f ? district.normalized : -facing;
            var name = lampName.Replace("_Lamp", "");
            var root = new GameObject(name + "_Pylon").transform; root.SetParent(parent, false); root.position = at; root.rotation = Quaternion.LookRotation(facing);
            var dev = root.gameObject.AddComponent<Seven.RelayDevice>();
            var k = new AK();
            // the pylon: a flared foot, a slender waist rising, a cup holding the petal ring at 1.2 m
            { var (v, t) = k.Of(m.pale); Append(v, t, Lathe(new[] { new Vector2(0.36f, 0f), new Vector2(0.34f, 0.04f), new Vector2(0.12f, 0.16f), new Vector2(0.065f, 0.55f), new Vector2(0.075f, 0.95f), new Vector2(0.14f, 1.14f), new Vector2(0.2f, 1.2f), new Vector2(0.17f, 1.23f), new Vector2(0f, 1.2f) }, 0f, 360f, 28)); }
            // a fine light line up the waist (dark when broken - it is part of the channel)
            foreach (var g in k.Save("Bia_" + name + "_Pylon", root, true)) g.transform.localPosition = Vector3.zero;
            // the node: a tall crystal (an elongated octahedron) above the cup
            var nv = new List<Vector3>(); var nt = new List<int>();
            var oct = Seven.Octahedron.Mesh(1f); var ov = oct.vertices; var ot = oct.triangles;
            foreach (var p in ov) nv.Add(new Vector3(p.x * 0.085f, 1.4f + p.y * 0.15f, p.z * 0.085f)); nt.AddRange(ot);
            var node = MeshObject(lampName, SaveMesh(Finish(nv, nt), "Bia_" + lampName), new Material(m.glowSoft) { name = "Bia_" + lampName.ToLower() }, root, false);
            node.transform.localPosition = Vector3.zero; node.transform.localRotation = Quaternion.identity;
            // the cracks on the dead node: dark hairlines over its faces
            var cv = new List<Vector3>(); var ct = new List<int>();
            Sweep(cv, ct, new[] { new Vector3(-0.05f, 1.32f, 0.055f), new Vector3(0.0f, 1.41f, 0.07f), new Vector3(0.03f, 1.47f, 0.05f) }, 0.004f, false, 3);
            Sweep(cv, ct, new[] { new Vector3(0.06f, 1.35f, -0.03f), new Vector3(0.05f, 1.43f, 0.01f) }, 0.004f, false, 3);
            Sweep(cv, ct, new[] { new Vector3(-0.07f, 1.42f, -0.02f), new Vector3(-0.02f, 1.5f, -0.04f) }, 0.004f, false, 3);
            var cracks = MeshObject(name + "_NodeCracks", SaveMesh(Finish(cv, ct), "Bia_" + name + "_Cracks"), m.dark, root, false); cracks.transform.localPosition = Vector3.zero; cracks.transform.localRotation = Quaternion.identity;
            // the petals: six smooth leaves hinged round the cup's rim (each its own pivot - they open)
            var petals = new List<Transform>(); var jam = new List<float>();
            var pm = new List<Vector3>(); var ptr = new List<int>(); Ellipsoid(pm, ptr, new Vector3(0f, 0.13f, 0f), new Vector3(0.055f, 0.14f, 0.012f), 6, 10);
            var petalMesh = SaveMesh(SmoothFinish(pm, ptr), "Bia_" + name + "_Petal");
            var jams = new[] { 18f, 34f, 9f, 41f, 22f, 12f };
            for (int i = 0; i < 6; i++)
            {
                var piv = new GameObject("Petal" + i).transform; piv.SetParent(root, false); piv.localPosition = new Vector3(0f, 1.21f, 0f);
                var hinge = new GameObject("Hinge").transform; hinge.SetParent(piv, false); hinge.localPosition = new Vector3(0f, 0f, 0.16f);
                var pg = MeshObject("PetalMesh", petalMesh, m.pale, hinge, false); pg.transform.localPosition = Vector3.zero;
                petals.Add(piv); jam.Add(jams[i]);
            }
            // each petal: the pivot = its place on the ring (yaw), its hinge = how far it is open (pitch, outward)
            // the resonance plate: a small tilted disc on the front at hand height, a soft glow ring round it
            var plate = new GameObject(name + "_Plate").transform; plate.SetParent(root, false);
            plate.localPosition = new Vector3(0f, 1.02f, 0.14f); plate.localRotation = Quaternion.Euler(-38f, 0f, 0f);    // faces her, tilted up
            { var pv = new List<Vector3>(); var pt = new List<int>(); Ellipsoid(pv, pt, Vector3.zero, new Vector3(0.085f, 0.11f, 0.015f), 6, 14); var pg = MeshObject(name + "_PlateMesh", SaveMesh(SmoothFinish(pv, pt), "Bia_" + name + "_Plate"), m.pale, plate, false); pg.transform.localPosition = Vector3.zero; pg.transform.localRotation = Quaternion.identity; }
            Renderer plateGlow;
            { var gv = new List<Vector3>(); var gt = new List<int>(); var loop = new List<Vector3>(); for (int i = 0; i <= 24; i++) { float a = i / 24f * Mathf.PI * 2f; loop.Add(new Vector3(Mathf.Cos(a) * 0.088f, Mathf.Sin(a) * 0.113f, 0.012f)); } Sweep(gv, gt, loop, 0.006f, false, 4); var gg = MeshObject(name + "_PlateGlow", SaveMesh(Finish(gv, gt), "Bia_" + name + "_PlateGlow"), new Material(m.glowSoft) { name = "Bia_" + name.ToLower() + "_plateglow" }, plate, false); gg.transform.localPosition = Vector3.zero; gg.transform.localRotation = Quaternion.identity; plateGlow = gg.GetComponent<Renderer>(); }
            // the floor channel toward the district it reconnects: 16 flush segments, each lit in turn on restore
            var chan = new List<Renderer>();
            var chMat = new Material(m.glowSoft) { name = "Bia_" + name.ToLower() + "_channel" };
            for (int i = 0; i < 16; i++)
            {
                var a = at + district * (0.45f + i * 0.9f); var b = at + district * (0.45f + i * 0.9f + 0.75f);
                var sv = new List<Vector3>(); var st = new List<int>(); Sweep(sv, st, new[] { a + Vector3.up * 0.012f, b + Vector3.up * 0.012f }, 0.035f, false, 4);
                var sg = MeshObject(name + "_Channel" + i, SaveMesh(Finish(sv, st), "Bia_" + name + "_Channel" + i), new Material(chMat) { name = chMat.name + i }, root, false);
                sg.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); chan.Add(sg.GetComponent<Renderer>());
            }
            dev.Node = node.GetComponent<Renderer>(); dev.Cracks = cracks; dev.Plate = plate; dev.PlateGlow = plateGlow;
            dev.Petals = petals.ToArray(); dev.JamDeg = jam.ToArray(); dev.Channel = chan.ToArray();
            Log($"RELAY (QF24 10): {name} - pylon 1.55 m, crystal node in six petals, resonance plate at 1.02 m facing {facing}, a 15 m floor channel toward the district");
            return dev.Node;
        }

        // ------------------------------------------------------------------------------------------------ (12) the garden
        static Material _leafDark, _leafPale, _blossom, _soil;
        static void BiaGardenMats()
        {
            if (_leafDark == null) _leafDark = Dissolve("Bia_leaf_dark", Gray(0.2f), 0.2f, false);
            if (_leafPale == null) _leafPale = Dissolve("Bia_leaf_pale", Gray(0.42f), 0.25f, false);
            if (_blossom == null) { _blossom = Dissolve("Bia_blossom", Gray(0.86f), 0.3f, false); _blossom.EnableKeyword("_EMISSION"); _blossom.SetColor("_EmissionColor", Color.white * 0.12f); }
            if (_soil == null) _soil = Dissolve("Bia_soil", Gray(0.14f), 0.1f, false);
            foreach (var x in new[] { _leafDark, _leafPale, _blossom, _soil }) x.SetFloat("_Cull", 0f);
        }
        static bool BiaWalk(float x, float z, float pad, params string[] names)
        {
            foreach (var zz in B.Zones) { if (names.Length > 0 && !names.Contains(zz.Name)) continue; if (x > zz.X0 - pad && x < zz.X1 + pad && z > zz.Z0 - pad && z < zz.Z1 + pad) return true; }
            return false;
        }
        /// <summary>Plants for one planter spot: a white-rimmed bed with a flowering tree, a shrub cluster, or the nursery of seedlings.</summary>
        static void BiaBed(AK k, BiMats m, System.Random r, Vector3 p, int kind)
        {
            { var (v, t) = k.Of(m.pale); Append(v, t, Lathe(new[] { new Vector2(1.05f, 0f), new Vector2(1.32f, 0.02f), new Vector2(1.34f, 0.32f), new Vector2(1.25f, 0.42f), new Vector2(1.12f, 0.4f), new Vector2(1.08f, 0.3f) }, 0f, 360f, 32, null, p)); }   // the smooth white rim
            { var (v, t) = k.Of(_soil); Append(v, t, Lathe(new[] { new Vector2(0f, 0.32f), new Vector2(1.1f, 0.32f), new Vector2(1.1f, 0.2f), new Vector2(0f, 0.2f) }, 0f, 360f, 24, null, p)); }
            var top = p + Vector3.up * 0.3f;
            if (kind == 0)
            {
                // a flowering tree (Venus's garden tree) + pale blossom clusters in its crown
                var (tv, tt) = k.Of(_leafDark); float h = 3.4f + (float)r.NextDouble() * 1.6f; Tree(tv, tt, r, top, h);
                var (bv, bt) = k.Of(_blossom);
                for (int i = 0; i < 9; i++) { var d = Quaternion.Euler(0f, i * 40f + (float)r.NextDouble() * 20f, 0f) * Vector3.forward; Lump(bv, bt, top + Vector3.up * h * (0.5f + (float)r.NextDouble() * 0.35f) + d * h * 0.18f, Vector3.one * (0.14f + (float)r.NextDouble() * 0.1f), 0.3f, r.Next(1000)); }
                var (sv, st) = k.Of(m.leaf); for (int i = 0; i < 3; i++) Shrub(sv, st, r, top + Quaternion.Euler(0f, i * 120f, 0f) * Vector3.forward * 0.75f, 0.45f + (float)r.NextDouble() * 0.2f);
            }
            else if (kind == 1)
            {
                var (sv, st) = k.Of(m.leaf); for (int i = 0; i < 4; i++) Shrub(sv, st, r, top + Quaternion.Euler(0f, i * 90f + 20f, 0f) * Vector3.forward * (i == 0 ? 0f : 0.62f), 0.7f + (float)r.NextDouble() * 0.5f);
                var (bv, bt) = k.Of(_blossom); for (int i = 0; i < 6; i++) Lump(bv, bt, top + Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward * 0.55f + Vector3.up * (0.6f + (float)r.NextDouble() * 0.3f), Vector3.one * 0.09f, 0.3f, r.Next(1000));
            }
            else
            {
                // the nursery: rows of the seedlings' species (the same small plants the resident carried)
                var (sv, st) = k.Of(_leafPale);
                for (int i = 0; i < 9; i++) { var o = new Vector3((i % 3 - 1) * 0.55f, 0f, (i / 3 - 1) * 0.55f); Shrub(sv, st, r, top + o, 0.22f + (float)r.NextDouble() * 0.12f); }
            }
        }
        static void BiaGarden(Transform gar, BiMats m)
        {
            BiaGardenMats();
            var k = new AK(); var r = new System.Random(2417);
            int i = 0; foreach (var p in B.GardenPlanters()) BiaBed(k, m, r, p, (i++) % 3);
            // hanging plants spilling from the terraces along the garden wall (z 92.6) and from bowls overhead
            { var (lv, lt) = k.Of(_leafPale); for (float x = 238f; x < 283f; x += 1.7f) { if (x > 262f && x < 280f) continue; float len = 1.4f + (float)r.NextDouble() * 2.6f; var a = new Vector3(x, 9.6f, 92.0f); Sweep(lv, lt, new[] { a, a + new Vector3(0.1f, -len * 0.5f, -0.25f), a + new Vector3(-0.05f, -len, -0.1f) }, 0.07f, false, 4); Lump(lv, lt, a + Vector3.down * len, Vector3.one * 0.16f, 0.3f, r.Next(1000)); } }
            for (int j = 0; j < 5; j++) HangingGarden(k, m, new Vector3(242f + j * 9.5f, 13f + (j % 2) * 3f, 58f + (j % 3) * 10f), 2.2f);
            foreach (var g in k.Save("Bia_GardenQF24", gar, true)) g.transform.localPosition = Vector3.zero;
            // the soft grass that moves (Venus's Seven/Grass) on the beds' ground between the paths
            var patches = new List<GrassPatch> { new GrassPatch { Name = "BiaGarden", Area = new Rect(236.5f, 41.8f, 47f, 49f), Density = 9f, MinH = 0.25f, MaxH = 0.6f,
                Height = (x, z) => { if (BiaWalk(x, z, 0.4f, "GardenAvenue", "GardenRoad", "GardenGateZ", "RoadNorth")) return float.NaN; foreach (var p in B.GardenPlanters()) if ((new Vector2(x - p.x, z - p.z)).sqrMagnitude < 1.5f * 1.5f) return float.NaN; if (z > 88.5f && x > 262f) return float.NaN; return 0.0f; } } };
            int blades = BuildGrass(gar, patches, 31);
            Log($"GARDEN (QF24 12): Venus's vegetation in the mothership's garden - {i} white-rimmed beds (flowering trees, shrub clusters, the seedlings' nursery), hanging plants along the terraces + 5 hanging bowls, {blades} blades of swaying Seven/Grass");
        }
        /// <summary>The old garden in the memory echo: translucent versions of the same plants (Venus tree / shrub shapes).</summary>
        static void BiaEchoPlants(List<Vector3> v, List<int> t)
        {
            var r = new System.Random(77);
            for (int i = 0; i < 8; i++) { var p = new Vector3(242f + i * 5f, 0f, 60f + (i % 3) * 9f); if (i % 2 == 0) Tree(v, t, r, p, 3.6f); else Shrub(v, t, r, p, 1.1f); }
        }

        // ------------------------------------------------------------------------------------------------ (16) the detail
        static void BiaDetail(Transform world, BiMats m)
        {
            BiaGardenMats();
            var r = new System.Random(1624);
            float R() => (float)r.NextDouble();
            // ---- the transfer chamber (the shaft round (175, 0, 125), r 34, 70 m tall)
            var ch = new GameObject("TransferChamberDetail").transform; ch.SetParent(world, false);
            var k = new AK(); var c = new Vector3(175f, 0f, 125f);
            bool Open(float a, float y) => a > 330f || a < 20f || (a > 72f && a < 108f && y < 40f);
            Vector3 OnWall(float a, float rad, float y) => c + Quaternion.Euler(0f, a, 0f) * Vector3.forward * rad + Vector3.up * y;
            // ribs (bowing slightly inward at mid-height) and the inset light channels between them
            for (int i = 0; i < 36; i++)
            {
                float a = i * 10f + 5f; if (Open(a, 0f) && Open(a, 60f)) continue;
                var (v, t) = k.Of(m.mid);                                                 // mid grey: they silhouette against the bright haze
                var pts = new List<Vector3>(); for (int s = 0; s <= 8; s++) { float y = s / 8f * 70f; if (Open(a, y)) { if (pts.Count > 1) Sweep(v, t, pts, 0.32f, false, 6); pts = new List<Vector3>(); continue; } pts.Add(OnWall(a, 33.3f - Mathf.Sin(s / 8f * Mathf.PI) * 0.9f, y)); }
                if (pts.Count > 1) Sweep(v, t, pts, 0.32f, false, 6);
                if (i % 2 == 0) { var (gv, gt) = k.Of(m.glowSoft); float a2 = a + 5f; if (!Open(a2, 2f) && !Open(a2, 58f)) Sweep(gv, gt, new[] { OnWall(a2, 33.75f, 2f), OnWall(a2, 33.75f, 58f) }, 0.06f, false, 4); }
            }
            // horizontal ribbed rings
            foreach (var y in new[] { 18f, 36f, 54f, 68f })
            {
                var (v, t) = k.Of(m.mid); var pts = new List<Vector3>();
                for (int s = 0; s <= 72; s++) { float a = s * 5f; if (Open(a, y)) { if (pts.Count > 1) Sweep(v, t, pts, 0.28f, false, 6); pts = new List<Vector3>(); continue; } pts.Add(OnWall(a, 33.2f, y)); }
                if (pts.Count > 1) Sweep(v, t, pts, 0.28f, false, 6);
            }
            // service platforms at different heights: a curved deck, a railing along the inner edge, soft floor lights
            foreach (var (a0, y) in new[] { (150f, 8f), (205f, 16f), (250f, 24f), (290f, 33f), (180f, 44f), (230f, 56f), (120f, 28f) })
            {
                var (v, t) = k.Of(m.mid); Append(v, t, Lathe(new[] { new Vector2(30.2f, y - 0.35f), new Vector2(33.4f, y - 0.35f), new Vector2(33.4f, y), new Vector2(30.2f, y) }, a0, a0 + 22f, 10, null, c));
                var (rv, rt) = k.Of(m.dark); var rail = new List<Vector3>(); for (int s = 0; s <= 10; s++) rail.Add(OnWall(a0 + s * 2.2f, 30.35f, y + 1.05f)); Sweep(rv, rt, rail, 0.04f, false, 5);
                for (int s = 0; s <= 10; s += 2) Sweep(rv, rt, new[] { OnWall(a0 + s * 2.2f, 30.35f, y), OnWall(a0 + s * 2.2f, 30.35f, y + 1.05f) }, 0.03f, false, 4);
                var (lv, lt) = k.Of(m.glow); for (int s = 1; s < 10; s += 3) Ellipsoid(lv, lt, OnWall(a0 + s * 2.2f, 31.8f, y + 0.02f), new Vector3(0.18f, 0.03f, 0.18f), 3, 8);
            }
            // conduits up the shaft (gentle sag between the rings)
            for (int i = 0; i < 12; i++)
            {
                float a = 25f + i * 26f; if (Open(a, 10f)) continue; var (v, t) = k.Of(m.dark);
                var pts = new List<Vector3>(); for (int s = 0; s <= 14; s++) { float y = s * 5f; pts.Add(OnWall(a + Mathf.Sin(s * 0.9f) * 0.6f, 33.85f, y)); }
                Sweep(v, t, pts, 0.09f + (i % 3) * 0.03f, false, 5);
            }
            // maintenance openings: dark recesses in pale arched frames
            foreach (var a in new[] { 140f, 195f, 240f, 280f, 310f })
            {
                if (Open(a, 1f)) continue;
                var (dv, dt) = k.Of(m.dark); MBox(dv, dt, OnWall(a, 33.9f, 1.5f), new Vector3(2.2f, 3f, 0.3f), -a, 0f);
                var (fv, ft) = k.Of(m.pale); Sweep(fv, ft, Enumerable.Range(0, 11).Select(s => { float u = s / 10f * Mathf.PI; return OnWall(a + Mathf.Cos(u) * 2.1f, 33.5f, Mathf.Sin(u) * 1.0f + 2.6f); }).Concat(new[] { OnWall(a - 2.1f, 33.5f, 0f) }).Prepend(OnWall(a + 2.1f, 33.5f, 0f)).ToList(), 0.12f, false, 6);
            }
            // the partition machinery waiting above: housings with a warning glow strip, tracks down the sides
            foreach (var (p, span, alongX) in new[] { (B.Partitions[0], 46f, true), (B.Partitions[1], 44f, false) })
            {
                var dir = alongX ? Vector3.right : Vector3.forward; var (v, t) = k.Of(m.mid);
                MBox(v, t, p + Vector3.up * 16.2f, alongX ? new Vector3(span + 1f, 1.6f, 2.2f) : new Vector3(2.2f, 1.6f, span + 1f), 0f, 0.05f);
                var (gv, gt) = k.Of(m.glow); Sweep(gv, gt, new[] { p - dir * span * 0.5f + Vector3.up * 15.3f, p + dir * span * 0.5f + Vector3.up * 15.3f }, 0.05f, false, 4);
                var (tv, tt) = k.Of(m.dark); foreach (var s in new[] { -1f, 1f }) MBox(tv, tt, p + dir * s * (span * 0.5f + 0.4f) + Vector3.up * 8f, new Vector3(0.5f, 16f, 0.5f), 0f, 0f);
            }
            // the grand archive door: carved concentric arches, relief rings, memory light pulsing behind - forced by Ratwing
            {
                var d = B.ArchiveEntrance + new Vector3(0f, 0f, 0.4f);
                var (v, t) = k.Of(m.pale);
                foreach (var (rx, ry, rad) in new[] { (5.4f, 8.2f, 0.32f), (6.2f, 9.0f, 0.22f), (7.0f, 9.8f, 0.16f) })
                    Sweep(v, t, Enumerable.Range(0, 21).Select(s => { float u = s / 20f * Mathf.PI; return d + new Vector3(Mathf.Cos(u) * rx * 0.5f * 1.6f, Mathf.Sin(u) * (ry - 5.2f) + 5.2f, -0.5f); }).Prepend(d + new Vector3(rx * 0.8f, 0f, -0.5f)).Append(d + new Vector3(-rx * 0.8f, 0f, -0.5f)).ToList(), rad, false, 6);
                var (gv, gt) = k.Of(m.glowSoft); for (int i = 0; i < 3; i++) Sweep(gv, gt, Enumerable.Range(0, 25).Select(s => { float u = s / 24f * Mathf.PI * 2f; return d + new Vector3(Mathf.Cos(u) * (0.8f + i * 0.55f), 3.6f + Mathf.Sin(u) * (0.8f + i * 0.55f), -0.36f); }).ToList(), 0.035f, false, 4);
                // damage: scorch (dark blotches), claw marks (three gouges), a torn panel leaning at the foot
                var (sv, st) = k.Of(m.dark);
                Ellipsoid(sv, st, d + new Vector3(-2.3f, 2.2f, -0.33f), new Vector3(0.9f, 0.7f, 0.03f), 4, 10); Ellipsoid(sv, st, d + new Vector3(1.9f, 4.8f, -0.33f), new Vector3(0.6f, 0.5f, 0.03f), 4, 10);
                for (int i = 0; i < 3; i++) Sweep(sv, st, new[] { d + new Vector3(0.8f + i * 0.28f, 5.6f, -0.34f), d + new Vector3(1.3f + i * 0.3f, 3.3f, -0.34f) }, 0.05f, false, 4);
                var (pv, pt) = k.Of(m.paleSoft); MBox(pv, pt, d + new Vector3(-6.2f, 0.8f, -1.6f), new Vector3(1.8f, 1.6f, 0.12f), 28f, 0f);   // beside the doorway, never in it
                // the memory light behind the door (seen round its edges and through the carving)
                // (never a panel across the doorway: the memory light shows round its edges - strips just outside the 8 m gap and over it)
                var (mv, mt) = k.Of(m.glow); foreach (var sx in new[] { -4.35f, 4.35f }) MBox(mv, mt, d + new Vector3(sx, 3.6f, -0.2f), new Vector3(0.12f, 7.0f, 0.12f), 0f, 0f); MBox(mv, mt, d + new Vector3(0f, 7.35f, -0.2f), new Vector3(8.6f, 0.14f, 0.12f), 0f, 0f);
            }
            // foreground silhouettes: dark transfer machinery between the floor's edge and the wall (never on the walkable floor)
            int mach = 0;
            for (int i = 0; i < 24; i++)
            {
                float a = i * 15f + 7f; if (Open(a, 1f)) continue;
                var p = OnWall(a, 30.8f, 0f); bool onFloor = p.x > 150f - 1.2f && p.x < 214f + 1.2f && p.z > 102f - 1.2f && p.z < 148f + 1.2f;
                if (onFloor) continue;
                var (v, t) = k.Of(m.dark); MBox(v, t, p + Vector3.up * 1.9f, new Vector3(2.4f, 3.8f, 1.4f), -a, 0.04f);
                MBox(v, t, p + Vector3.up * 4.4f, new Vector3(1.6f, 1.2f, 1.0f), -a, 0.04f);
                var (gv, gt) = k.Of(m.glowSoft); Sweep(gv, gt, new[] { p + Vector3.up * 0.6f, p + Vector3.up * 3.6f }, 0.04f, false, 4);
                var (cv, ct) = k.Of(m.dark); Sweep(cv, ct, new[] { p + Vector3.up * 5f, OnWall(a, 33.6f, 9f), OnWall(a, 33.8f, 16f) }, 0.12f, false, 5);
                mach++;
            }
            Log($"DETAIL (QF24 16): {mach} dark transfer machines between the chamber floor and its wall (foreground silhouettes)");
            // hanging light globes on long cords
            for (int i = 0; i < 14; i++)
            {
                float a = i * 25.7f, rad = 9f + (i % 4) * 5f, y = 30f + (i % 5) * 6f;
                var p = c + Quaternion.Euler(0f, a, 0f) * Vector3.forward * rad + Vector3.up * y;
                var (cv, ct) = k.Of(m.dark); Sweep(cv, ct, new[] { p, p + Vector3.up * (72f - y) }, 0.025f, false, 3);
                var (gv, gt) = k.Of(m.glow); Ellipsoid(gv, gt, p, Vector3.one * (0.7f + (i % 3) * 0.25f), 6, 10);
            }
            // the city beyond: tall pale towers outside the shaft, seen through its openings and over its top, fading out
            for (int i = 0; i < 26; i++)
            {
                float a = i * 13.8f + R() * 6f, rad = 70f + R() * 90f, h = 40f + R() * 110f;
                var (v, t) = k.Of(rad > 120f ? m.paleSoft : m.pale); var p = c + Quaternion.Euler(0f, a, 0f) * Vector3.forward * rad;
                Append(v, t, Lathe(new[] { new Vector2(0f, -2f), new Vector2(4f + R() * 3f, -2f), new Vector2(3f, h * 0.7f), new Vector2(4.5f, h * 0.8f), new Vector2(0f, h) }, 0f, 360f, 12, null, p));
            }
            foreach (var g in k.Save("Bia_ChamberQF24", ch)) g.transform.localPosition = Vector3.zero;

            // ---- the districts along the routes: residents' things just OUTSIDE the walkable areas
            var dk = new AK(); var dist = new GameObject("DistrictDetail").transform; dist.SetParent(world, false);
            int placed = 0;
            foreach (var z in B.Zones)
            {
                if (!(z.Name.StartsWith("Road") || z.Name == "Berth" || z.Name == "Junction" || z.Name == "FarSide" || z.Name == "GardenRoad")) continue;
                bool alongX = (z.X1 - z.X0) >= (z.Z1 - z.Z0);
                float len = alongX ? z.X1 - z.X0 : z.Z1 - z.Z0;
                for (float s = 6f; s < len - 4f; s += 11f)
                    foreach (var side in new[] { -1f, 1f })
                    {
                        var p = alongX ? new Vector3(z.X0 + s, 0f, side < 0 ? z.Z0 - 1.6f : z.Z1 + 1.6f) : new Vector3(side < 0 ? z.X0 - 1.6f : z.X1 + 1.6f, 0f, z.Z0 + s);
                        if (BiaWalk(p.x, p.z, 1.3f)) continue;                                  // never on (or right at) a walkable area
                        if (Mathf.Abs(p.x - 175f) < 40f && Mathf.Abs(p.z - 125f) < 40f) continue;
                        var facing = alongX ? new Vector3(0f, 0f, -side) : new Vector3(-side, 0f, 0f);
                        int kind = (placed++) % 4;
                        if (kind == 0) { var (v, t) = dk.Of(m.pale); Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.75f, 0f), new Vector2(0.85f, 0.55f), new Vector2(0.78f, 0.62f), new Vector2(0f, 0.6f) }, 0f, 360f, 18, null, p)); var (sv, st) = dk.Of(m.leaf); Shrub(sv, st, r, p + Vector3.up * 0.55f, 0.8f + R() * 0.4f); }              // a planter
                        else if (kind == 1) { var (v, t) = dk.Of(m.pale); var q = Quaternion.LookRotation(facing); var sideV = q * Vector3.right; Sweep(v, t, new[] { p - sideV * 1.1f + Vector3.up * 0.45f, p + Vector3.up * 0.42f, p + sideV * 1.1f + Vector3.up * 0.45f }, 0.2f, false, 8); foreach (var e in new[] { -0.9f, 0.9f }) Sweep(v, t, new[] { p + sideV * e, p + sideV * e + Vector3.up * 0.42f }, 0.07f, false, 6); }   // a smooth bench
                        else if (kind == 2) { var (v, t) = dk.Of(m.paleSoft); MBox(v, t, p + Vector3.up * 1.4f, new Vector3(0.7f, 2.8f, 0.14f), Quaternion.LookRotation(facing).eulerAngles.y, 0f); var (gv, gt) = dk.Of(m.glowSoft); var q = Quaternion.LookRotation(facing); for (int g = 0; g < 4; g++) { var gp = p + Vector3.up * (0.8f + g * 0.5f) + facing * 0.08f; Sweep(gv, gt, Enumerable.Range(0, 9).Select(u => gp + q * new Vector3(Mathf.Cos(u / 8f * Mathf.PI * 2f) * 0.18f, Mathf.Sin(u / 8f * Mathf.PI * 2f) * 0.12f + (g % 2) * 0.03f, 0f)).ToList(), 0.015f, false, 3); } }   // a glyph sign
                        else { var (v, t) = dk.Of(m.pale); Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.4f, 0f), new Vector2(0.2f, 0.7f), new Vector2(0.32f, 0.82f), new Vector2(0f, 0.84f) }, 0f, 360f, 16, null, p)); var (gv, gt) = dk.Of(m.glow); for (int g = 0; g < 3; g++) { var gp = p + Vector3.up * 0.84f + Quaternion.Euler(0f, g * 120f, 0f) * Vector3.forward * 0.12f; Ellipsoid(gv, gt, gp + Vector3.up * 0.14f, new Vector3(0.04f, 0.14f, 0.04f), 3, 6); } }   // a memory-crystal shrine
                    }
            }
            foreach (var g in dk.Save("Bia_DistrictQF24", dist)) g.transform.localPosition = Vector3.zero;

            // ---- the quiet passage: the shielding's structure - ribs over the passage (overhead) and on the walls' outer faces
            var pk = new AK(); var pas = new GameObject("PassageShieldingDetail").transform; pas.SetParent(world, false);
            for (float x = 187f; x < 205f; x += 2.4f)
            {
                var (v, t) = pk.Of(m.mid); Sweep(v, t, Enumerable.Range(0, 9).Select(s => { float u = s / 8f * Mathf.PI; return new Vector3(x, 4.5f + Mathf.Sin(u) * 1.4f, 66f - Mathf.Cos(u) * 3.0f); }).ToList(), 0.14f, false, 6);
                foreach (var zf in new[] { 62.5f, 69.5f }) Sweep(v, t, new[] { new Vector3(x, 0f, zf), new Vector3(x, 4.5f, zf) }, 0.12f, false, 6);
                var (gv, gt) = pk.Of(m.glowSoft); Sweep(gv, gt, new[] { new Vector3(x + 1.2f, 5.9f, 63.2f), new Vector3(x + 1.2f, 5.9f, 68.8f) }, 0.03f, false, 3);
            }
            foreach (var g in pk.Save("Bia_PassageQF24", pas)) g.transform.localPosition = Vector3.zero;

            // ---- the octahedron chamber: the empty positions where memories are missing (open cradles in a ring)
            var ok = new AK(); var oc = new GameObject("OctaChamberDetail").transform; oc.SetParent(world, false);
            for (int i = 0; i < 10; i++)
            {
                var p = new Vector3(B.OctaAt.x, 0f, B.OctaAt.z) + Quaternion.Euler(0f, i * 36f + 18f, 0f) * Vector3.forward * 6.5f + Vector3.up * (3.2f + (i % 3) * 1.1f);
                var (v, t) = ok.Of(m.pale); Sweep(v, t, Enumerable.Range(0, 13).Select(s => p + Quaternion.Euler(0f, i * 36f, 0f) * new Vector3(Mathf.Cos(s / 12f * Mathf.PI * 2f) * 0.32f, Mathf.Sin(s / 12f * Mathf.PI * 2f) * 0.32f, 0f)).ToList(), 0.025f, false, 4);
                var (cv, ct) = ok.Of(m.dark); Sweep(cv, ct, new[] { p + Vector3.up * 0.32f, p + Vector3.up * 14f }, 0.012f, false, 3);
                if (i % 3 != 1) { var (gv, gt) = ok.Of(m.glowSoft); Ellipsoid(gv, gt, p, new Vector3(0.05f, 0.05f, 0.05f), 3, 6); }   // a faint mote where something was
            }
            foreach (var g in ok.Save("Bia_OctaQF24", oc)) g.transform.localPosition = Vector3.zero;
            Log($"DETAIL (QF24 16): the transfer chamber dressed (ribs + light channels, 7 service platforms with railings and floor lights, conduits, maintenance openings, the partition housings, the carved archive door with memory light and Ratwing's damage, 14 hanging globes, the city beyond); {placed} district pieces outside the routes; the passage's shielding ribs; 10 empty memory positions round the octahedron");
        }
    }
}
