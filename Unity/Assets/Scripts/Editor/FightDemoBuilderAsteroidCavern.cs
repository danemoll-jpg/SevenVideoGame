using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using A = Seven.AsteroidLayout;

namespace Seven.EditorTools
{
    /// <summary>Round 16 — the cavern: the city that still works (AST_03).</summary>
    public static partial class FightDemoBuilder
    {
        class CavRefs { public Seven.AstTurntable[] junctions; public Transform[] levers; public Renderer[] jlamps; public Seven.AstBird bird; public Renderer birdLamp; public List<Light> prodLights = new List<Light>(); public List<Renderer> prodLamps = new List<Renderer>(); }

        static CavRefs BuildCavern(Transform world, AsMats m)
        {
            var r = new CavRefs();
            var cav = new GameObject("Cavern").transform; cav.SetParent(world, false);
            var k = new AK();
            // the service tunnel from the receiving chamber: riveted walls, arched ribs, lamps
            MGround("Tunnel_Floor", new Vector3(-60.6f, -1.5f, -8f), new Vector3(-18f, 0f, 0f), m.floor, cav);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-18.4f, 0f, -8.3f), new Vector3(-60f, 0f, -8.3f), 0f, 7f, 0.5f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-18.4f, 0f, 0.3f), new Vector3(-60f, 0f, 0.3f), 0f, 7f, 0.5f);
            for (float x = -22f; x > -60f; x -= 4f) { var (v, t) = k.Of(m.metalDark); Sweep(v, t, Enumerable.Range(0, 9).Select(i => new Vector3(x, 7f + Mathf.Sin(i / 8f * Mathf.PI) * 1.6f, -8.3f + i / 8f * 8.6f)).ToList(), 0.22f, false, 6); }
            { var (v, t) = k.Of(m.plate); for (float x = -18.4f; x > -60f; x -= 6f) MBox(v, t, new Vector3(x - 3f, 8.4f, -4f), new Vector3(6f, 0.3f, 8.8f), 0f, 0f); }
            k.Save("Tunnel_Wall", cav);
            for (float x = -26f; x > -60f; x -= 12f) AstLamp($"TunnelLamp{(int)-x}", new Vector3(x, 6.4f, -4f), 14f, 1.4f, m.glow, m.metalDark, cav, 0.2f);

            // ---- the cavern shell (seen from inside), its floor far below, Kyle's glow sources in the haze
            {
                var v = new List<Vector3>(); var t = new List<int>();
                var c = new Vector3(-170f, 20f, 40f); var rad = new Vector3(305f, 112f, 270f);
                var rnd = new System.Random(16);
                int lat = 22, lon = 44;
                for (int i = 0; i <= lat; i++) for (int j = 0; j <= lon; j++)
                {
                    float th = i / (float)lat * Mathf.PI, ph = j / (float)lon * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    float wob = 1f + 0.06f * Mathf.Sin(ph * 5f + th * 3f) + 0.03f * (float)rnd.NextDouble();
                    v.Add(c + Vector3.Scale(n, rad) * wob);
                }
                for (int i = 0; i < lat; i++) for (int j = 0; j < lon; j++) { int a = i * (lon + 1) + j, b = a + lon + 1; t.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b }); }   // inward
                MeshObject("Cavern_Wall_Shell", SaveMesh(SmoothFinish(v, t), "Ast_CavernShell"), m.rockDark, cav, false);
                MGround("Cavern_Floor", new Vector3(-470f, A.CavernFloor - 4f, -230f), new Vector3(130f, A.CavernFloor, 310f), m.rock, cav);
            }
            // ---- the causeway decks (riveted, with girder sides), the columns down to the floor, the low railings
            var decks = new AK(); var rails = new AK();
            foreach (var z in A.Zones)
            {
                if (!(z.Name.StartsWith("Causeway") || z.Name.StartsWith("Bend") || z.Name == "HallPlaza" || z.Name == "Workstations")) continue;
                MGround("Causeway_Deck_" + z.Name, new Vector3(z.X0, -1.6f, z.Z0), new Vector3(z.X1, 0f, z.Z1), z.Name == "Workstations" ? m.plate : m.floor, cav);
                bool alongX = z.X1 - z.X0 > z.Z1 - z.Z0;
                float len = alongX ? z.X1 - z.X0 : z.Z1 - z.Z0;
                // girder sides under the deck edge + columns
                foreach (var side in new[] { 0, 1 })
                {
                    Vector3 a = alongX ? new Vector3(z.X0, -1.9f, side == 0 ? z.Z0 + 0.2f : z.Z1 - 0.2f) : new Vector3(side == 0 ? z.X0 + 0.2f : z.X1 - 0.2f, -1.9f, z.Z0);
                    Vector3 b = alongX ? new Vector3(z.X1, -1.9f, a.z) : new Vector3(a.x, -1.9f, z.Z1);
                    var (gv, gt) = decks.Of(m.metalDark); MBox(gv, gt, (a + b) * 0.5f, alongX ? new Vector3(len, 0.9f, 0.4f) : new Vector3(0.4f, 0.9f, len), 0f, 0f);
                    if (z.Name == "HallPlaza" || z.Name == "Workstations" || z.Name.StartsWith("Bend")) continue;   // bends: their open sides join the next deck
                    // the railing: posts + a top rail (low, 1.05 m)
                    var (pv, pt) = rails.Of(m.steel);
                    // the railing stands just outside the deck's walkable edge (her feet never reach its posts)
                    var ro = alongX ? new Vector3(0f, 0f, side == 0 ? -0.28f : 0.28f) : new Vector3(side == 0 ? -0.28f : 0.28f, 0f, 0f);
                    for (float s = 2f; s < len - 1f; s += 3f) { var p = Vector3.Lerp(a, b, s / len) + ro; p.y = 0f; Sweep(pv, pt, new[] { p, p + Vector3.up * 1.05f }, 0.035f, false, 5); }
                    Sweep(pv, pt, new[] { new Vector3(a.x, 1.05f, a.z) + ro, new Vector3(b.x, 1.05f, b.z) + ro }, 0.04f, false, 6);
                }
                for (float s = 6f; s < len; s += 16f)
                {
                    var p = alongX ? new Vector3(z.X0 + s, 0f, (z.Z0 + z.Z1) * 0.5f) : new Vector3((z.X0 + z.X1) * 0.5f, 0f, z.Z0 + s);
                    Column(decks, m.metal, p, A.CavernFloor, -1.9f, 0.9f);
                }
            }
            // the bends' outer rails only (Bend1: south + west; Bend2: north + east... the sides that lead nowhere)
            { var (pv, pt) = rails.Of(m.steel);
              foreach (var (a, b) in new[] { (new Vector3(-179.3f, 0f, -7.3f), new Vector3(-170f, 0f, -7.3f)), (new Vector3(-179.3f, 0f, -7.3f), new Vector3(-179.3f, 0f, 2.9f)),
                                             (new Vector3(-179.3f, 0f, 107.3f), new Vector3(-172.7f, 0f, 107.3f)), (new Vector3(-172.7f, 0f, 107.3f), new Vector3(-172.7f, 0f, 100.6f)) })
              { float len = Vector3.Distance(a, b); for (float s = 0.5f; s < len; s += 3f) { var p = Vector3.Lerp(a, b, s / len); Sweep(pv, pt, new[] { p, p + Vector3.up * 1.05f }, 0.035f, false, 5); } Sweep(pv, pt, new[] { a + Vector3.up * 1.05f, b + Vector3.up * 1.05f }, 0.04f, false, 6); } }
            decks.Save("Causeway_Supports", cav); rails.Save("Causeway_Railings", cav);
            // ---- the three rail junctions: a riveted span on a central pier that turns (misaligned: along the carriers' rails)
            r.junctions = new Seven.AstTurntable[3]; r.levers = new Transform[3]; r.jlamps = new Renderer[3];
            for (int i = 0; i < 3; i++)
            {
                var c = A.Junctions[i]; bool ax = A.JunctionAxis[i] == 0f;
                var pk = new AK(); Column(pk, m.metal, c, A.CavernFloor, -2.4f, 2.2f);
                { var (v, t) = pk.Of(m.metalDark); Append(v, t, Lathe(new[] { new Vector2(0f, -2.4f), new Vector2(3.4f, -2.4f), new Vector2(3.6f, -1.9f), new Vector2(0f, -1.9f) }, 0f, 360f, 24, null, c)); }
                pk.Save($"Junction{i + 1}_Pier", cav);
                var pivot = new GameObject($"Junction{i + 1}_Span").transform; pivot.SetParent(cav, false); pivot.position = c;
                var sk = new AK();
                var size = ax ? new Vector3(13.2f, 1.5f, 7f) : new Vector3(7f, 1.5f, 13.2f);
                { var (v, t) = sk.Of(m.floor); MBox(v, t, c + Vector3.down * 0.75f, size, 0f, 0f); }
                // girder sides + the carriers' two rails across the span (they show which way it is set)
                foreach (var sd in new[] { -1f, 1f })
                {
                    var (gv, gt) = sk.Of(m.metalDark);
                    MBox(gv, gt, c + (ax ? new Vector3(0f, -1.7f, sd * 3.3f) : new Vector3(sd * 3.3f, -1.7f, 0f)), ax ? new Vector3(13.2f, 1.0f, 0.5f) : new Vector3(0.5f, 1.0f, 13.2f), 0f, 0f);
                    var (rv, rt) = sk.Of(m.steel);
                    MBox(rv, rt, c + (ax ? new Vector3(0f, 0.05f, sd * 0.8f) : new Vector3(sd * 0.8f, 0.05f, 0f)), ax ? new Vector3(13.2f, 0.1f, 0.14f) : new Vector3(0.14f, 0.1f, 13.2f), 0f, 0f);
                }
                foreach (var g in sk.Save($"Junction{i + 1}_Platform", pivot)) g.transform.position = Vector3.zero;   // world-space vertices under the pivot at c
                var tt = pivot.gameObject.AddComponent<Seven.AstTurntable>(); tt.Misaligned = 90f;
                // the lever post + its lamp (white when aligned)
                var ctl = A.JunctionControls[i];
                var lk = new AK(); Column(lk, m.metalDark, ctl, 0f, 1.15f, 0.12f); lk.Save($"Junction{i + 1}_LeverPost", cav);
                var lever = new GameObject($"Junction{i + 1}_Lever").transform; lever.SetParent(cav, false); lever.position = ctl + Vector3.up * 1.15f;
                var lv = new List<Vector3>(); var lt = new List<int>(); Sweep(lv, lt, new[] { Vector3.zero, new Vector3(0f, 0.75f, 0.25f) }, 0.04f, false, 6); Ellipsoid(lv, lt, new Vector3(0f, 0.78f, 0.26f), Vector3.one * 0.08f, 4, 8);
                MeshObject($"Junction{i + 1}_LeverArm", SaveMesh(SmoothFinish(lv, lt), $"Ast_JLever{i + 1}"), m.steel, lever).transform.localPosition = Vector3.zero;
                var lamp = new List<Vector3>(); var lmt = new List<int>(); Ellipsoid(lamp, lmt, ctl + Vector3.up * 2.1f, Vector3.one * 0.16f, 6, 10);
                r.jlamps[i] = MeshObject($"Junction{i + 1}_Lamp", SaveMesh(SmoothFinish(lamp, lmt), $"Ast_JLamp{i + 1}"), new Material(m.glowSoft) { name = $"Ast_jlamp{i}" }, cav, false).GetComponent<Renderer>();
                { var pk2 = new AK(); Column(pk2, m.metalDark, ctl, 1.15f, 1.95f, 0.04f); pk2.Save($"Junction{i + 1}_LampStem", cav); }
                // the route lights that run ahead once it is aligned (along the deck edge to the next stop)
                var next = i < 2 ? A.JunctionStops[i + 1] : A.HallPlazaStop;
                var from = c + (ax ? Vector3.right * -7f : Vector3.forward * 7f);
                var path = i == 0 ? new[] { from + new Vector3(0f, 0.15f, 3.2f), new Vector3(-176f, 0.15f, -0.8f), new Vector3(-172.9f, 0.15f, 40f) }
                         : i == 1 ? new[] { from + new Vector3(3.2f, 0.15f, 0f), new Vector3(-172.9f, 0.15f, 100f), new Vector3(-222f, 0.15f, 107.2f) }
                         : new[] { from + new Vector3(0f, 0.15f, 3.2f), new Vector3(-300f, 0.15f, 107.2f) };
                tt.RouteLights = LightRun($"RouteLights{i + 1}", path, 14, 0.07f, m.glowSoft, cav);
                tt.Lamp = r.jlamps[i];
                r.junctions[i] = tt; r.levers[i] = lever;
            }
            // ---- the factory terraces climbing the walls (south, north, west): stepped tiers with machinery and lamps
            int tn = 0;
            foreach (var (o, dir, len) in new[] { (new Vector3(-60f, A.CavernFloor, -62f), Vector3.left, 290f), (new Vector3(-60f, A.CavernFloor, 168f), Vector3.left, 290f), (new Vector3(-458f, A.CavernFloor, -60f), Vector3.forward, 228f) })
            {
                var inward = dir == Vector3.left ? (o.z < 0f ? Vector3.forward : Vector3.back) : Vector3.right;
                var tk = new AK();
                for (int lvl = 0; lvl < 5; lvl++)
                {
                    float h = 14f, depth = 12f;
                    var c0 = o + Vector3.up * (h * lvl + h * 0.5f) - inward * (depth * lvl) + dir * len * 0.5f + inward * depth * 0.5f;
                    var (v, t) = tk.Of(lvl % 2 == 0 ? m.plate : m.metal);
                    MBox(v, t, c0, dir == Vector3.left ? new Vector3(len, h, depth) : new Vector3(depth, h, len), 0f, 0.01f);
                    // machinery on each tier: flywheels, pistons, the lit windows of control cabins
                    for (int j = 0; j < 7; j++)
                    {
                        var p = o + Vector3.up * (h * (lvl + 1)) - inward * (depth * lvl - depth * 0.35f) + dir * (len * (j + 0.5f) / 7f);
                        if ((j + lvl) % 3 == 0)
                        {
                            var g = Gear($"TerraceWheel{tn}_{lvl}_{j}", p + Vector3.up * 4.2f, dir, 3.8f, 0.7f, 14, m.metalDark, cav);
                            var sp = g.AddComponent<Seven.AstSpin>(); sp.Axis = Vector3.up; sp.Speed = 18f + 9f * ((j * 7 + lvl) % 4);
                        }
                        else if ((j + lvl) % 3 == 1)
                        {
                            var pg = new GameObject($"TerraceHammer{tn}_{lvl}_{j}").transform; pg.SetParent(cav, false); pg.position = p + Vector3.up * 3f;
                            var hk = new AK(); Riveted(hk, m.metal, m.metalDark, p + Vector3.up * 3f, new Vector3(2.2f, 3.2f, 2.2f)); Pipe(hk, m.steel, new[] { p + Vector3.up * 4.6f, p + Vector3.up * 9f }, 0.35f);
                            foreach (var g in hk.Save($"TerraceHammer{tn}_{lvl}_{j}_Body", pg)) g.transform.position = Vector3.zero;
                            var pi = pg.gameObject.AddComponent<Seven.AstPiston>(); pi.Travel = new Vector3(0f, 2.6f, 0f); pi.Period = 2.2f + 0.3f * (j % 3); pi.Phase = j * 0.17f; pi.Snap = true;
                        }
                        else
                        {
                            var cab = new AK(); Riveted(cab, m.metalDark, m.metal, p + Vector3.up * 2.2f, new Vector3(4f, 4.4f, 3f));
                            var (wv, wt) = cab.Of(m.glowSoft); MBox(wv, wt, p + Vector3.up * 2.6f + inward * 1.52f, dir == Vector3.left ? new Vector3(2.6f, 1.2f, 0.06f) : new Vector3(0.06f, 1.2f, 2.6f), 0f, 0f);
                            foreach (var g in cab.Save($"TerraceCabin{tn}_{lvl}_{j}", cav)) if (g.name.Contains("glow")) r.prodLamps.Add(g.GetComponent<Renderer>());
                        }
                    }
                    if (lvl % 2 == 1) r.prodLights.Add(PointLight($"TerraceLight{tn}_{lvl}", o + Vector3.up * (h * (lvl + 1) + 6f) - inward * (depth * lvl - 6f) + dir * len * 0.5f, 90f, 1.6f, cav));
                }
                tk.Save($"Terrace{tn}_Tiers", cav);
                tn++;
            }
            // ---- the rail carriers between the terraces (two loops of suspended cabins on overhead rails)
            foreach (var (name, y, x0, x1, z0, z1, cars, speed) in new[] { ("CarrierLoopHigh", 30f, -70f, -300f, -48f, 150f, 6, 9f), ("CarrierLoopLow", 13f, -120f, -250f, -36f, 136f, 4, 7f) })
            {
                var path = new[] { new Vector3(x0, y, z0), new Vector3(x1, y, z0), new Vector3(x1, y, z1), new Vector3(x0, y, z1) };
                var rk = new AK(); Pipe(rk, m.steel, path.Concat(new[] { path[0] }).ToList(), 0.3f);
                for (int i = 0; i < 4; i++) { var a = path[i]; Column(rk, m.metalDark, new Vector3(a.x, 0f, a.z), A.CavernFloor, y + 1f, 1.2f); }
                rk.Save(name + "_Rails", cav);
                var carRoots = new List<Transform>();
                for (int i = 0; i < cars; i++)
                {
                    var cr = new GameObject($"{name}_Car{i}").transform; cr.SetParent(cav, false);
                    var ck = new AK(); Riveted(ck, m.metal, m.metalDark, new Vector3(0f, -3.2f, 0f), new Vector3(2.6f, 2.8f, 5f)); Pipe(ck, m.steel, new[] { new Vector3(0f, -1.6f, -1.6f), new Vector3(0f, 0f, -1.6f) }, 0.12f); Pipe(ck, m.steel, new[] { new Vector3(0f, -1.6f, 1.6f), new Vector3(0f, 0f, 1.6f) }, 0.12f);
                    var (wv, wt) = ck.Of(m.glowSoft); MBox(wv, wt, new Vector3(1.32f, -3f, 0f), new Vector3(0.05f, 0.9f, 3.4f), 0f, 0f); MBox(wv, wt, new Vector3(-1.32f, -3f, 0f), new Vector3(0.05f, 0.9f, 3.4f), 0f, 0f);
                    foreach (var g in ck.Save($"{name}_Car{i}_Body", cr)) g.transform.localPosition = Vector3.zero;
                    carRoots.Add(cr);
                }
                var rc = new GameObject(name).AddComponent<Seven.AstRailCarrier>(); rc.transform.SetParent(cav, false); rc.Path = path; rc.Cars = carRoots.ToArray(); rc.Speed = speed;
            }
            // ---- great flywheels and pistons below the causeway (the repetitive, precise movements of the city)
            foreach (var (p, ax2, rr, spd) in new[] { (new Vector3(-140f, -20f, -26f), Vector3.forward, 11f, 14f), (new Vector3(-150f, -18f, 30f), Vector3.right, 9f, -18f), (new Vector3(-205f, -16f, 70f), Vector3.forward, 12f, 11f), (new Vector3(-260f, -22f, 128f), Vector3.right, 10f, 16f) })
            {
                var g = Gear($"Flywheel{(int)-p.x}", p, ax2, rr, 1.4f, 24, m.metalDark, cav);
                var sp = g.AddComponent<Seven.AstSpin>(); sp.Axis = Vector3.up; sp.Speed = spd;
                var bk = new AK(); Column(bk, m.metal, p + ax2 * -1.6f, A.CavernFloor, p.y, 1.4f); bk.Save($"Flywheel{(int)-p.x}_Pillar", cav);
            }
            // ---- glow sources inside the fog (Kyle: soft light sources in the haze), the essential lights
            foreach (var p in new[] { new Vector3(-120f, 8f, -60f), new Vector3(-230f, 4f, 20f), new Vector3(-180f, 20f, 150f), new Vector3(-300f, 10f, 40f), new Vector3(-80f, 14f, 120f) })
                r.prodLights.Add(PointLight($"FogGlow{(int)-p.x}_{(int)p.z}", p, 70f, 2.2f, cav));
            foreach (var p in new[] { new Vector3(-90f, 3.8f, -8.2f), new Vector3(-150f, 3.8f, -8.2f), new Vector3(-180.2f, 3.8f, 24f), new Vector3(-180.2f, 3.8f, 80f), new Vector3(-200f, 3.8f, 108.2f), new Vector3(-270f, 3.8f, 108.2f) })
            {
                var sk2 = new AK(); Column(sk2, m.metalDark, new Vector3(p.x, 0f, p.z), 0f, 3.5f, 0.1f); sk2.Save($"CausewayLampPost{(int)-p.x}_{(int)p.z}", cav);
                AstLamp($"CausewayLamp{(int)-p.x}_{(int)p.z}", p, 22f, 1.4f, m.glow, m.metalDark, cav, 0.22f);          // essential: they stay on
            }
            // ---- the empty workstations along the causeway: benches, stools, personal storage compartments
            {
                var wk = new AK();
                for (int i = 0; i < 3; i++)
                {
                    var bc = new Vector3(-186.2f, 0f, 72f + i * 4f);
                    Riveted(wk, m.metal, m.metalDark, bc + Vector3.up * 0.9f, new Vector3(1.1f, 0.12f, 2.4f));
                    foreach (var dz in new[] { -1f, 1f }) Column(wk, m.metalDark, bc + new Vector3(0f, 0f, dz * 1.05f), 0f, 0.84f, 0.05f);
                    Column(wk, m.metalDark, bc + new Vector3(1.3f, 0f, 0.4f), 0f, 0.62f, 0.05f);           // a stool
                    { var (v, t) = wk.Of(m.metal); Append(v, t, Lathe(new[] { new Vector2(0f, 0.6f), new Vector2(0.24f, 0.6f), new Vector2(0.24f, 0.68f), new Vector2(0f, 0.68f) }, 0f, 360f, 12, null, bc + new Vector3(1.3f, 0f, 0.4f))); }
                }
                for (int i = 0; i < 6; i++) { var lc = new Vector3(-187.7f, 1.05f, 71f + i * 1.9f); Riveted(wk, m.plate, m.metalDark, lc, new Vector3(0.6f, 2.1f, 1.7f)); var (dv, dt) = wk.Of(m.metalDark); MBox(dv, dt, lc + new Vector3(0.31f, 0f, 0f), new Vector3(0.02f, 1.9f, 0.04f), 0f, 0f); }
                wk.Save("Workstations", cav);
                AstLamp("WorkstationsLamp", new Vector3(-184f, 3.2f, 76f), 9f, 1.0f, m.glow, m.metalDark, cav, 0.16f);
                var bl = new List<Vector3>(); var blt = new List<int>(); Ellipsoid(bl, blt, A.BirdBench + new Vector3(0.3f, 1.2f, 0.9f), Vector3.one * 0.06f, 4, 8);
                r.birdLamp = MeshObject("BirdBench_Lamp", SaveMesh(SmoothFinish(bl, blt), "Ast_BirdLamp"), new Material(m.glowSoft) { name = "Ast_birdlamp" }, cav, false).GetComponent<Renderer>();
                r.bird = BuildBird(cav, m);
            }
            // ---- the temple signal on the hall's east face (the glyphs the archive panels carry too)
            return r;
        }

        /// <summary>The little mechanical bird: a riveted body, a head with a beak, the good wing and the repaired one
        /// (mismatched plates riveted over each other), a winding key on its back.</summary>
        static Seven.AstBird BuildBird(Transform parent, AsMats m)
        {
            var root = new GameObject("MechanicalBird").transform; root.SetParent(parent, false); root.position = A.BirdAt; root.rotation = Quaternion.LookRotation(Vector3.right);
            var bv = new List<Vector3>(); var bt = new List<int>();
            Ellipsoid(bv, bt, new Vector3(0f, 0.06f, 0f), new Vector3(0.05f, 0.05f, 0.085f), 6, 10);
            Sweep(bv, bt, new[] { new Vector3(0f, 0.06f, -0.08f), new Vector3(0f, 0.05f, -0.14f) }, 0.018f, false, 5);   // the tail
            foreach (var sx in new[] { -0.025f, 0.025f }) Sweep(bv, bt, new[] { new Vector3(sx, 0.02f, 0f), new Vector3(sx, -0.015f, 0.01f) }, 0.006f, false, 4);   // legs
            MeshObject("Bird_Body", SaveMesh(SmoothFinish(bv, bt), "Ast_BirdBody"), m.steel, root).transform.localPosition = Vector3.zero;
            var head = new GameObject("Bird_Head").transform; head.SetParent(root, false); head.localPosition = new Vector3(0f, 0.11f, 0.07f);
            var hv = new List<Vector3>(); var ht = new List<int>(); Ellipsoid(hv, ht, Vector3.zero, Vector3.one * 0.032f, 5, 8);
            Append(hv, ht, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.012f, 0f), new Vector2(0f, 0.04f) }, 0f, 360f, 6, null, Vector3.zero).Let(mm => RotX(mm, new Vector3(0f, -0.005f, 0.025f))));
            MeshObject("Bird_HeadMesh", SaveMesh(SmoothFinish(hv, ht), "Ast_BirdHead"), m.steel, head).transform.localPosition = Vector3.zero;
            Transform Wing(string n, float side, bool patched)
            {
                var w = new GameObject(n).transform; w.SetParent(root, false); w.localPosition = new Vector3(side * 0.045f, 0.08f, 0f);
                var wv = new List<Vector3>(); var wt = new List<int>();
                Ellipsoid(wv, wt, new Vector3(side * 0.05f, 0f, -0.01f), new Vector3(0.05f, 0.006f, 0.065f), 3, 8);
                if (patched)
                {
                    // repaired several times: mismatched plates of different sizes riveted over each other, a bent tip
                    MBox(wv, wt, new Vector3(side * 0.04f, 0.007f, 0.02f), new Vector3(0.05f, 0.004f, 0.035f), 18f * side, 0f);
                    MBox(wv, wt, new Vector3(side * 0.07f, 0.009f, -0.03f), new Vector3(0.035f, 0.004f, 0.05f), -25f * side, 0f);
                    for (int i = 0; i < 5; i++) Ellipsoid(wv, wt, new Vector3(side * (0.03f + i * 0.012f), 0.012f, 0.01f - i * 0.01f), Vector3.one * 0.004f, 2, 4);
                }
                MeshObject(n + "_Mesh", SaveMesh(SmoothFinish(wv, wt), "Ast_" + n), patched ? m.metal : m.steel, w).transform.localPosition = Vector3.zero;
                return w;
            }
            var wl = Wing("Bird_WingL", -1f, false); var wr = Wing("Bird_WingR", 1f, true);
            var key = new GameObject("Bird_Key").transform; key.SetParent(root, false); key.localPosition = new Vector3(0f, 0.12f, -0.03f);
            var kv = new List<Vector3>(); var kt = new List<int>(); Sweep(kv, kt, new[] { Vector3.zero, new Vector3(0f, 0.03f, 0f) }, 0.004f, false, 4); Ellipsoid(kv, kt, new Vector3(-0.012f, 0.035f, 0f), new Vector3(0.012f, 0.008f, 0.003f), 3, 6); Ellipsoid(kv, kt, new Vector3(0.012f, 0.035f, 0f), new Vector3(0.012f, 0.008f, 0.003f), 3, 6);
            MeshObject("Bird_KeyMesh", SaveMesh(SmoothFinish(kv, kt), "Ast_BirdKey"), m.steel, key).transform.localPosition = Vector3.zero;
            var b = root.gameObject.AddComponent<Seven.AstBird>(); b.Head = head; b.WingL = wl; b.WingR = wr; b.Key = key;
            return b;
        }
    }

    static class AstMeshExt { public static Mesh Let(this Mesh m, System.Func<Mesh, Mesh> f) => f(m); }
}
