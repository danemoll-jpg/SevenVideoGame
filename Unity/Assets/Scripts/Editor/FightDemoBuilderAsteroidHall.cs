using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using A = Seven.AsteroidLayout;

namespace Seven.EditorTools
{
    /// <summary>Round 16 — the central maintenance hall, the temple archive and the factory area (AST_04-AST_09).</summary>
    public static partial class FightDemoBuilder
    {
        class HallRefs
        {
            public Seven.AstClamp cradle; public Seven.AstClamp[] stations; public Transform terminal; public Renderer terminalScreen;
            public Seven.AstArchiveLine line; public Transform cutterPanel; public Renderer panelGlyph, signalGlyph;
            public Seven.AstShutter hallDoor, hatch, archiveDoor, westDoor;
            public Transform rune, recordCubo, storageDoor; public Renderer memory; public Transform[] stored;
            public List<Light> prodLights = new List<Light>(); public List<Renderer> prodLamps = new List<Renderer>();
            // works
            public Renderer alcoveScreen, lockerLamp; public Transform lockerDoor, lockerBlades, transportLever, valveWheel, clampJaw, finalPlat, diverterLever;
            public Transform[] platforms; public Vector3[] parked, aligned; public Seven.PowerLine clampPower, coolant; public ParticleSystem[] steam; public Renderer[] controlLamps;
            public Seven.AstSecurity[] security;
        }

        static Seven.AstShutter Shutter(string name, Transform parent, AsMats m, Vector3 a, Vector3 b, float h, float th, float drop)
        {
            var p = new GameObject(name).transform; p.SetParent(parent, false);
            var k = new AK(); PlateWall(k, m.plate, m.metalDark, a, b, 0f, h, th, 3f); k.Save(name + "_Plates", p);
            var s = p.gameObject.AddComponent<Seven.AstShutter>(); s.Drop = drop; return s;
        }
        static Renderer GlowBlob(string name, Vector3 at, float r, AsMats m, Transform parent)
        {
            var v = new List<Vector3>(); var t = new List<int>(); Ellipsoid(v, t, at, Vector3.one * r, 6, 10);
            return MeshObject(name, SaveMesh(SmoothFinish(v, t), "Ast_" + name), new Material(m.glowSoft) { name = "Ast_" + name.ToLower() }, parent, false).GetComponent<Renderer>();
        }
        /// <summary>The temple glyphs: a ring cut by a crescent, three dots, a line of small marks (on the archive panels and the signal).</summary>
        static Mesh Glyph(float s)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            Sweep(v, t, Enumerable.Range(0, 25).Select(i => new Vector3(Mathf.Cos(i / 24f * Mathf.PI * 2f) * s, Mathf.Sin(i / 24f * Mathf.PI * 2f) * s, 0f)).ToList(), s * 0.05f, true, 5);
            Sweep(v, t, Enumerable.Range(0, 13).Select(i => { float a = Mathf.Lerp(-70f, 70f, i / 12f) * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(a) * s * 0.6f - s * 0.15f, Mathf.Sin(a) * s * 0.6f, 0f); }).ToList(), s * 0.045f, false, 5);
            for (int i = 0; i < 3; i++) Ellipsoid(v, t, new Vector3(-s * 0.35f, (i - 1) * s * 0.32f, 0f), new Vector3(s * 0.08f, s * 0.08f, s * 0.03f), 3, 6);
            for (int i = 0; i < 6; i++) Sweep(v, t, new[] { new Vector3(-s * 0.9f + i * s * 0.36f, -s * 1.3f, 0f), new Vector3(-s * 0.9f + i * s * 0.36f, -s * 1.15f - (i % 2) * s * 0.1f, 0f) }, s * 0.03f, false, 4);
            return SmoothFinish(v, t);
        }

        static HallRefs BuildHall(Transform world, AsMats m)
        {
            var r = new HallRefs();
            var hall = new GameObject("CentralHall").transform; hall.SetParent(world, false);
            var k = new AK();
            MGround("Hall_Floor", new Vector3(-400f, -1.5f, 70f), new Vector3(-320f, 0f, 140f), m.floor, hall);
            MGround("Hall_DoorSill_Floor", new Vector3(-321f, -1.5f, 100f), new Vector3(-318.8f, 0f, 108f), m.floor, hall);
            float H = 26f;
            // walls with openings: the east door (z 100-108), the maintenance opening (south, x -330..-326), the west passage
            // door (south, x -399..-393), the archive door (west, z 102-110)
            PlateWall(k, m.plate, m.metalDark, new Vector3(-319.6f, 0f, 69.6f), new Vector3(-319.6f, 0f, 100f), 0f, H);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-319.6f, 0f, 100f), new Vector3(-319.6f, 0f, 108f), 8.2f, H - 8.2f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-319.6f, 0f, 108f), new Vector3(-319.6f, 0f, 140.4f), 0f, H);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-319.6f, 0f, 140.4f), new Vector3(-400.4f, 0f, 140.4f), 0f, H);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-400.4f, 0f, 140.4f), new Vector3(-400.4f, 0f, 110f), 0f, H);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-400.4f, 0f, 110f), new Vector3(-400.4f, 0f, 102f), 7.2f, H - 7.2f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-400.4f, 0f, 102f), new Vector3(-400.4f, 0f, 69.6f), 0f, H);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-400.4f, 0f, 69.6f), new Vector3(-399f, 0f, 69.6f), 0f, H);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-399f, 0f, 69.6f), new Vector3(-393f, 0f, 69.6f), 5.2f, H - 5.2f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-393f, 0f, 69.6f), new Vector3(-330f, 0f, 69.6f), 0f, H);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-330f, 0f, 69.6f), new Vector3(-326f, 0f, 69.6f), 3.2f, H - 3.2f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-326f, 0f, 69.6f), new Vector3(-319.6f, 0f, 69.6f), 0f, H);
            k.Save("Hall_Wall", hall);
            // the roof: trusses and a riveted ceiling, high lamps (production lights) and essential lamps low on the walls
            for (float x = -396f; x < -322f; x += 9f) { var (v, t) = k.Of(m.metalDark); Sweep(v, t, Enumerable.Range(0, 9).Select(i => new Vector3(x, H + Mathf.Sin(i / 8f * Mathf.PI) * 3f, 70f + i / 8f * 70f)).ToList(), 0.35f, false, 6); }
            { var (v, t) = k.Of(m.plate); MBox(v, t, new Vector3(-360f, H + 3.4f, 105f), new Vector3(82f, 0.4f, 72f), 0f, 0f); }
            k.Save("Hall_Ceiling", hall);
            foreach (var x in new[] { -385f, -360f, -335f }) foreach (var z in new[] { 85f, 120f }) r.prodLights.Add(AstLamp($"HallLamp{(int)-x}_{(int)z}", new Vector3(x, H - 2f, z), 40f, 2.0f, m.glow, m.metalDark, hall, 0.45f));
            foreach (var p in new[] { new Vector3(-321f, 6f, 90f), new Vector3(-321f, 6f, 125f), new Vector3(-399f, 6f, 90f), new Vector3(-399f, 6f, 125f), new Vector3(-360f, 6f, 139f), new Vector3(-360f, 6f, 71f) })
                AstLamp($"HallWallLamp{(int)-p.x}_{(int)p.z}", p, 16f, 1.0f, m.glow, m.metalDark, hall, 0.18f);
            // the shutters (security: closed in AST_04) - built in place, open = raised into the lintel
            r.hallDoor = Shutter("HallDoorShutter", hall, m, new Vector3(-319.4f, 0f, 100f), new Vector3(-319.4f, 0f, 108f), 8.2f, 0.4f, 8.4f);
            r.hatch = Shutter("MaintHatchShutter", hall, m, new Vector3(-330f, 0f, 69.4f), new Vector3(-326f, 0f, 69.4f), 3.2f, 0.3f, 3.3f);
            r.westDoor = Shutter("WestPassShutter", hall, m, new Vector3(-399f, 0f, 69.4f), new Vector3(-393f, 0f, 69.4f), 5.2f, 0.35f, 5.4f);
            r.archiveDoor = Shutter("ArchiveShutter", hall, m, new Vector3(-400.2f, 0f, 102f), new Vector3(-400.2f, 0f, 110f), 7.2f, 0.4f, 7.4f);
            // the temple signal over the hall's east entrance (its glyphs) - seen from the causeway, the insert in AST_04
            r.signalGlyph = MeshObject("TempleSignal_Glyph", SaveMesh(Glyph(2.2f), "Ast_SignalGlyph"), new Material(m.glowSoft) { name = "Ast_signalglyph" }, hall, false).GetComponent<Renderer>();
            r.signalGlyph.transform.SetPositionAndRotation(new Vector3(-319.1f, 13.5f, 104f), Quaternion.LookRotation(Vector3.right));
            // ---- his repair cradle (the end of the fight): two uprights, padded arms (a clamp), a lamp
            {
                var cr = new GameObject("RepairCradle").transform; cr.SetParent(hall, false); cr.position = A.CradleAt;
                var ck = new AK();
                foreach (var sx in new[] { -2.6f, 2.6f }) Column(ck, m.metalDark, A.CradleAt + new Vector3(sx, 0f, 1.2f), 0f, 6.5f, 0.45f);
                Riveted(ck, m.metal, m.metalDark, A.CradleAt + new Vector3(0f, 6.6f, 1.2f), new Vector3(6.2f, 0.7f, 1.2f));
                Riveted(ck, m.plate, m.metalDark, A.CradleAt + new Vector3(0f, 0.03f, 0.4f), new Vector3(5.6f, 0.06f, 3.2f));   // a flush floor plate (he stands on it)
                foreach (var g in ck.Save("RepairCradle_Frame", cr)) g.transform.position = Vector3.zero;
                var ja = new GameObject("Cradle_ArmL").transform; ja.SetParent(cr, false); ja.localPosition = new Vector3(-2.4f, 3.8f, 0.6f);
                var jb = new GameObject("Cradle_ArmR").transform; jb.SetParent(cr, false); jb.localPosition = new Vector3(2.4f, 3.8f, 0.6f);
                foreach (var (j, sx) in new[] { (ja, 1f), (jb, -1f) }) { var jk = new AK(); Riveted(jk, m.metal, m.metalDark, new Vector3(sx * 1.0f, 0f, 0f), new Vector3(2.0f, 0.8f, 0.9f)); var (pv, pt) = jk.Of(m.panel); Ellipsoid(pv, pt, new Vector3(sx * 1.9f, 0f, 0f), new Vector3(0.3f, 0.55f, 0.55f), 4, 8); foreach (var g in jk.Save(j.name + "_Mesh", j)) g.transform.localPosition = Vector3.zero; }
                r.cradle = cr.gameObject.AddComponent<Seven.AstClamp>(); r.cradle.JawA = ja; r.cradle.JawB = jb; r.cradle.Open = 70f; r.cradle.Sound = "ast_cradle";
                r.cradle.Lamp = GlowBlob("Cradle_Lamp", A.CradleAt + new Vector3(0f, 7.3f, 1.2f), 0.25f, m, cr);
            }
            // ---- the archive conveyor into the cutting mechanism; the storage loop; the engraved panels
            {
                var ck = new AK();
                Riveted(ck, m.metalDark, m.metal, A.CutterAt + new Vector3(0f, 2.4f, 0f), new Vector3(4.4f, 4.8f, 4.2f));
                for (float x = -398f; x < -377f; x += 1f) { var (v, t) = ck.Of(m.steel); Append(v, t, RotX(Lathe(new[] { new Vector2(0f, -1f), new Vector2(0.12f, -1f), new Vector2(0.12f, 1f), new Vector2(0f, 1f) }, 0f, 360f, 8), new Vector3(x, 0.7f, A.ArchiveConveyorZ))); }
                // the storage loop (a rail ring north of the line)
                Pipe(ck, m.steel, new[] { new Vector3(-398f, 0.9f, 128f), new Vector3(-380f, 0.9f, 128f), new Vector3(-380f, 0.9f, 136f), new Vector3(-398f, 0.9f, 136f), new Vector3(-398f, 0.9f, 128f) }, 0.12f);
                Pipe(ck, m.steel, new[] { new Vector3(-394f, 0.9f, 122.6f), new Vector3(-394f, 0.9f, 128f) }, 0.12f);
                ck.Save("ArchiveLine_Frame", hall);
                var saw = Gear("Cutter_Saw", A.CutterAt + new Vector3(-2.3f, 1.4f, 0f), Vector3.forward, 1.3f, 0.12f, 36, m.steel, hall);
                var sawLight = PointLight("Cutter_SawLight", A.CutterAt + new Vector3(-3f, 1.6f, 0f), 7f, 1.2f, hall);
                var panels = new List<Transform>();
                for (int i = 0; i < 6; i++)
                {
                    var p = new GameObject("ArchivePanel" + i).transform; p.SetParent(hall, false);
                    var pk = new AK(); Riveted(pk, m.stone, null, new Vector3(0f, 1.05f, 0f), new Vector3(0.2f, 1.6f, 1.2f));
                    foreach (var g in pk.Save("ArchivePanel" + i + "_Slab", p)) g.transform.localPosition = Vector3.zero;
                    var gl = MeshObject("ArchivePanel" + i + "_Glyph", SaveMesh(Glyph(0.32f), "Ast_PanelGlyph" + i), m.panel, p, false); gl.transform.localPosition = new Vector3(0.11f, 1.1f, 0f); gl.transform.localRotation = Quaternion.LookRotation(Vector3.right);
                    p.position = new Vector3(-397f + i * 3.4f, 0.0f, A.ArchiveConveyorZ);
                    panels.Add(p);
                }
                var line = new GameObject("ArchiveLine").AddComponent<Seven.AstArchiveLine>(); line.transform.SetParent(hall, false);
                line.Panels = panels.ToArray(); line.Saw = saw.transform; line.SawLight = sawLight;
                line.ToCutter = new[] { new Vector3(-398f, 0f, A.ArchiveConveyorZ), new Vector3(-378f, 0f, A.ArchiveConveyorZ) };
                line.Loop = new[] { new Vector3(-394f, 0f, 128f), new Vector3(-380f, 0f, 128f), new Vector3(-380f, 0f, 136f), new Vector3(-398f, 0f, 136f), new Vector3(-398f, 0f, 128f) };
                r.line = line;
                // the special panel that goes into the cutter in AST_04 (its markings match the temple signal)
                var cp = new GameObject("CutterPanel").transform; cp.SetParent(hall, false);
                var cpk = new AK(); Riveted(cpk, m.stone, null, new Vector3(0f, 0.0f, 0f), new Vector3(0.24f, 1.8f, 1.4f)); foreach (var g in cpk.Save("CutterPanel_Slab", cp)) g.transform.localPosition = Vector3.zero;
                var cg = MeshObject("CutterPanel_Glyph", SaveMesh(Glyph(0.4f), "Ast_CutterPanelGlyph"), new Material(m.glowSoft) { name = "Ast_cutterpanelglyph" }, cp, false); cg.transform.localPosition = new Vector3(0.13f, 0.05f, 0f); cg.transform.localRotation = Quaternion.LookRotation(Vector3.right);
                cp.position = new Vector3(-384f, 1.0f, A.ArchiveConveyorZ);
                r.cutterPanel = cp; r.panelGlyph = cg.GetComponent<Renderer>();
            }
            // ---- the override terminal; the three service stations (clamps); protected storage; the memory surface
            {
                var tk = new AK(); Riveted(tk, m.metal, m.metalDark, A.TerminalAt + new Vector3(0f, 0.55f, 0f), new Vector3(2.2f, 1.1f, 1.2f));
                var (sv, st) = tk.Of(m.metalDark); MBox(sv, st, A.TerminalAt + new Vector3(0f, 1.5f, 0.3f), new Vector3(1.8f, 1.0f, 0.2f), 0f, 0f);
                tk.Save("OverrideTerminal", hall);
                r.terminal = new GameObject("OverrideTerminal_Point").transform; r.terminal.SetParent(hall, false); r.terminal.position = A.TerminalAt;
                var sv2 = new List<Vector3>(); var st2 = new List<int>(); Quad(sv2, st2, A.TerminalAt + new Vector3(-0.8f, 1.05f, 0.19f), A.TerminalAt + new Vector3(-0.8f, 1.95f, 0.19f), A.TerminalAt + new Vector3(0.8f, 1.95f, 0.19f), A.TerminalAt + new Vector3(0.8f, 1.05f, 0.19f));
                r.terminalScreen = MeshObject("OverrideTerminal_Screen", SaveMesh(Finish(sv2, st2), "Ast_TerminalScreen"), new Material(m.glowSoft) { name = "Ast_terminalscreen" }, hall, false).GetComponent<Renderer>();
                Seven.Glowing.Set(r.terminalScreen, 1.2f);
                var sts = new List<Seven.AstClamp>();
                for (int i = 0; i < A.Stations.Length; i++)
                {
                    var c = A.Stations[i];
                    var sp = new GameObject($"ServiceStation{i + 1}").transform; sp.SetParent(hall, false); sp.position = c;
                    var sk = new AK(); Riveted(sk, m.metalDark, m.metal, c + new Vector3(0f, 1.2f, 0f), new Vector3(2.2f, 2.4f, 2.2f)); Column(sk, m.metal, c, 2.4f, 4.6f, 0.4f); foreach (var g in sk.Save($"ServiceStation{i + 1}_Base", sp)) g.transform.position = Vector3.zero;
                    var ja = new GameObject("JawA").transform; ja.SetParent(sp, false); ja.localPosition = new Vector3(-0.5f, 4.8f, 0f);
                    var jb = new GameObject("JawB").transform; jb.SetParent(sp, false); jb.localPosition = new Vector3(0.5f, 4.8f, 0f);
                    foreach (var (j, sx) in new[] { (ja, 1f), (jb, -1f) }) { var jk = new AK(); Riveted(jk, m.steel, m.metalDark, new Vector3(sx * 0.1f, 0.7f, 0f), new Vector3(0.4f, 1.5f, 1.0f)); foreach (var g in jk.Save($"ServiceStation{i + 1}_{j.name}_Mesh", j)) g.transform.localPosition = Vector3.zero; }
                    var cl = sp.gameObject.AddComponent<Seven.AstClamp>(); cl.JawA = ja; cl.JawB = jb; cl.Open = 45f;
                    cl.Lamp = GlowBlob($"ServiceStation{i + 1}_Lamp", c + new Vector3(0f, 2.6f, 1.15f), 0.14f, m, sp);
                    sts.Add(cl);
                }
                r.stations = sts.ToArray();
                // protected storage: a vault door in the north wall that swings open (AST_09)
                var vk = new AK(); Riveted(vk, m.metalDark, m.metal, A.StorageAt + new Vector3(0f, 3.2f, 4f), new Vector3(8f, 6.4f, 0.6f)); vk.Save("ProtectedStorage_Frame", hall);
                var door = new GameObject("ProtectedStorage_Door").transform; door.SetParent(hall, false); door.position = A.StorageAt + new Vector3(-2.6f, 0f, 3.6f);
                var dk = new AK(); { var (dv, dt) = dk.Of(m.steel); Append(dv, dt, RotX(Lathe(new[] { new Vector2(0f, -0.3f), new Vector2(2.4f, -0.3f), new Vector2(2.5f, 0f), new Vector2(2.4f, 0.3f), new Vector2(0f, 0.3f) }, 0f, 360f, 24), new Vector3(2.6f, 2.8f, 0f))); for (int i = 0; i < 8; i++) Ellipsoid(dv, dt, new Vector3(2.6f, 2.8f, -0.32f) + Quaternion.Euler(0f, 0f, i * 45f) * new Vector3(1.9f, 0f, 0f), Vector3.one * 0.16f, 3, 6); }
                foreach (var g in dk.Save("ProtectedStorage_DoorMesh", door)) g.transform.localPosition = Vector3.zero;
                r.storageDoor = door;
                var stored = new List<Transform>();
                for (int i = 0; i < 3; i++) { var p = new GameObject("SurvivingPanel" + i).transform; p.SetParent(hall, false); var pk = new AK(); Riveted(pk, m.stone, null, Vector3.zero, new Vector3(1.2f, 1.6f, 0.2f)); foreach (var g in pk.Save("SurvivingPanel" + i + "_Slab", p)) g.transform.localPosition = Vector3.zero; p.position = A.StorageAt + new Vector3(4f + i * 1.6f, 0.95f, -4f); stored.Add(p); }
                r.stored = stored.ToArray();
                // the archive surface beside his cradle (the memory plays on it in AST_09)
                var mv = new List<Vector3>(); var mt = new List<int>(); var mc = A.MemorySurface;
                Quad(mv, mt, mc + new Vector3(-1.8f, -1.0f, 0f), mc + new Vector3(-1.8f, 1.0f, 0f), mc + new Vector3(1.8f, 1.0f, 0f), mc + new Vector3(1.8f, -1.0f, 0f));
                var memMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Ast_memory" }; memMat.SetColor("_BaseColor", Gray(0.25f));
                UnityEditor.AssetDatabase.CreateAsset(memMat, MatDir + "/Ast_memory.mat");
                var mg = MeshObject("MemorySurface", SaveMesh(Finish(mv, mt), "Ast_MemorySurface"), memMat, hall, false);
                var mfk = new AK(); Riveted(mfk, m.metalDark, m.metal, mc + new Vector3(0f, 0f, 0.15f), new Vector3(4.0f, 2.4f, 0.2f)); Column(mfk, m.metalDark, new Vector3(mc.x, 0f, mc.z + 0.2f), 0f, mc.y - 1.2f, 0.18f); mfk.Save("MemorySurface_Frame", hall);
                r.memory = mg.GetComponent<Renderer>();
                // the floor lanes (conveyor belts across the arena - the lane surges) and the hoppers overhead (scrap rain)
                var lk = new AK();
                foreach (var z in new[] { 84f, 95f, 106f }) { var (lv, lt) = lk.Of(m.metalDark); MBox(lv, lt, new Vector3(-360f, 0.01f, z), new Vector3(74f, 0.02f, 2.6f), 0f, 0f); for (float x = -396f; x < -324f; x += 2f) { var (gv, gt) = lk.Of(m.steel); MBox(gv, gt, new Vector3(x, 0.025f, z), new Vector3(0.12f, 0.02f, 2.4f), 0f, 0f); } }
                foreach (var x in new[] { -380f, -350f }) foreach (var z in new[] { 82f, 104f }) { Riveted(lk, m.metalDark, m.metal, new Vector3(x, H - 5f, z), new Vector3(4f, 3f, 4f)); var (hv, ht) = lk.Of(m.metal); Append(hv, ht, Lathe(new[] { new Vector2(2f, 0f), new Vector2(0.6f, -2.4f), new Vector2(0f, -2.4f) }, 0f, 360f, 4, null, new Vector3(x, H - 6.5f, z))); }
                lk.Save("Hall_FloorLanes", hall);
            }
            // ---- the temple archive behind the west wall: a round chamber, the central mechanism round the rune, mountings
            {
                var ak = new AK();
                MGround("Archive_Floor", new Vector3(-440f, -1.5f, 96f), new Vector3(-399.4f, 0f, 116f), m.stone, hall);
                PlateWall(ak, m.stone, null, new Vector3(-400.8f, 0f, 95.6f), new Vector3(-440.4f, 0f, 95.6f), 0f, 14f, 0.8f, 5f);
                PlateWall(ak, m.stone, null, new Vector3(-440.4f, 0f, 95.6f), new Vector3(-440.4f, 0f, 116.4f), 0f, 14f, 0.8f, 5f);
                PlateWall(ak, m.stone, null, new Vector3(-440.4f, 0f, 116.4f), new Vector3(-400.8f, 0f, 116.4f), 0f, 14f, 0.8f, 5f);
                { var (v, t) = ak.Of(m.stone); MBox(v, t, new Vector3(-420f, 14.4f, 106f), new Vector3(41f, 0.8f, 21f), 0f, 0.01f); }
                // mounting points round the walls: brackets, most of them empty (what has already been lost)
                for (int i = 0; i < 12; i++)
                {
                    bool north = i < 6; float x = -404f - (i % 6) * 6f; float z = north ? 115.7f : 96.3f;
                    var (bv, bt) = ak.Of(m.metalDark);
                    MBox(bv, bt, new Vector3(x, 2.4f, z), new Vector3(1.4f, 0.12f, 0.4f), 0f, 0f); MBox(bv, bt, new Vector3(x, 4.2f, z), new Vector3(1.4f, 0.12f, 0.4f), 0f, 0f);
                    if (i % 5 == 2) { var (sv, st) = ak.Of(m.stone); MBox(sv, st, new Vector3(x, 3.3f, z + (north ? -0.15f : 0.15f)), new Vector3(1.2f, 1.6f, 0.18f), 0f, 0f); }
                }
                ak.Save("Archive_Wall", hall);
                // the central mechanism: concentric riveted rings (slowly turning) round the rune on its pedestal
                for (int i = 0; i < 3; i++)
                {
                    var rv = new List<Vector3>(); var rt = new List<int>();
                    float rr = 0.55f + i * 0.13f;
                    Sweep(rv, rt, Enumerable.Range(0, 33).Select(j => new Vector3(Mathf.Cos(j / 32f * Mathf.PI * 2f) * rr, 0f, Mathf.Sin(j / 32f * Mathf.PI * 2f) * rr)).ToList(), 0.025f, true, 6);
                    for (int j = 0; j < 8; j++) Ellipsoid(rv, rt, new Vector3(Mathf.Cos(j * 45f * Mathf.Deg2Rad) * rr, 0.03f, Mathf.Sin(j * 45f * Mathf.Deg2Rad) * rr), Vector3.one * 0.018f, 3, 6);
                    var ring = MeshObject($"ArchiveMechanism_Ring{i}", SaveMesh(SmoothFinish(rv, rt), $"Ast_ArchiveRing{i}"), i == 1 ? m.steel : m.metal, hall);
                    ring.transform.position = A.RuneAt + Vector3.down * 0.12f; ring.transform.rotation = Quaternion.Euler(i < 2 ? 90f : 0f, i * 60f, i == 2 ? 90f : 0f);   // a gimbal: rings round it in three planes
                    var sp = ring.AddComponent<Seven.AstSpin>(); sp.Axis = Vector3.up; sp.Speed = 6f + i * 4f; sp.Essential = true;
                }
                var pk = new AK(); { var (v, t) = pk.Of(m.stone); Append(v, t, Lathe(new[] { new Vector2(0f, -1.25f), new Vector2(0.9f, -1.25f), new Vector2(0.5f, -0.5f), new Vector2(0.8f, -0.42f), new Vector2(0f, -0.42f) }, 0f, 360f, 20, null, A.RuneAt)); } pk.Save("Archive_RunePedestal", hall);
                PointLight("Archive_RuneLight", A.RuneAt + Vector3.up * 2f, 9f, 1.8f, hall);
                PointLight("Archive_Light", new Vector3(-420f, 11f, 106f), 30f, 1.2f, hall);
            }
            return r;
        }

        static void BuildWorks(Transform world, AsMats m, HallRefs r, Material bladeLight)
        {
            var w = new GameObject("FactoryArea").transform; w.SetParent(world, false);
            var k = new AK();
            // floors
            foreach (var z in A.Zones)
            {
                if (z.Name == "Platforms1" || z.Name == "FinalPlat") continue;
                if (z.Name == "MaintPass" || z.Name == "MaintHatchZ" || z.Name == "Alcove" || z.Name.StartsWith("Works") || z.Name == "CoolPass" || z.Name == "WestPass" || z.Name == "LowLedge")
                    MGround("Works_Floor_" + z.Name, new Vector3(z.X0, z.Y0 - 1.5f, z.Z0), new Vector3(z.X1, z.Y0, z.Z1), z.Name == "CoolPass" ? m.metalDark : m.floor, w);
            }
            // the outer walls of the works (a riveted shell, 10 m high), the pit between east and middle (the feed line below)
            float wh = 10f;
            PlateWall(k, m.plate, m.metalDark, new Vector3(-319.6f, 0f, 3.6f), new Vector3(-319.6f, 0f, 51f), 0f, wh);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-319.6f, 0f, 3.6f), new Vector3(-412.4f, 0f, 3.6f), 0f, wh);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-412.4f, 0f, 3.6f), new Vector3(-412.4f, 0f, 31f), 0f, wh);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-336.4f, 0f, 38.6f), new Vector3(-336.4f, 0f, 51f), 0f, 1.2f);   // the alcove's low sill (the glass over it)
            PlateWall(k, m.plate, m.metalDark, new Vector3(-336.4f, 0f, 51f), new Vector3(-330.4f, 0f, 51f), 0f, wh);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-330.4f, 0f, 51f), new Vector3(-325.6f, 0f, 51f), 4f, wh - 4f);     // the doorway from the maintenance passage
            PlateWall(k, m.plate, m.metalDark, new Vector3(-325.6f, 0f, 51f), new Vector3(-319.6f, 0f, 51f), 0f, wh);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-330.4f, 0f, 50.8f), new Vector3(-330.4f, 0f, 69.6f), 0f, 4f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-325.6f, 0f, 50.8f), new Vector3(-325.6f, 0f, 69.6f), 0f, 4f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-399.4f, 0f, 31f), new Vector3(-399.4f, 0f, 69.6f), 0f, 5.2f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-392.6f, 0f, 31f), new Vector3(-392.6f, 0f, 69.6f), 0f, 5.2f);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-412.4f, 0f, 31f), new Vector3(-399.4f, 0f, 31f), 0f, wh);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-392.6f, 0f, 31f), new Vector3(-372f, 0f, 34.4f), 0f, wh);
            PlateWall(k, m.plate, m.metalDark, new Vector3(-372f, 0f, 34.4f), new Vector3(-352f, 0f, 34.4f), 0f, wh);
            k.Save("Works_Wall", w);
            MGround("Works_PitBed", new Vector3(-352f, -12f, 0f), new Vector3(-336f, -10f, 70f), m.rockDark, w);
            MGround("Works_LedgeWall", new Vector3(-398.6f, -6f, 4f), new Vector3(-397.6f, 0f, 24f), m.plate, w);     // the face she climbs (AST_06)
            // the feed line in the pit (slabs moving south on rollers toward the foundry glow)
            {
                var fk = new AK(); for (float z = 2f; z < 68f; z += 1.2f) { var (v, t) = fk.Of(m.steel); Append(v, t, RotZ(Lathe(new[] { new Vector2(0f, -1.1f), new Vector2(0.1f, -1.1f), new Vector2(0.1f, 1.1f), new Vector2(0f, 1.1f) }, 0f, 360f, 8), new Vector3(-344f, -6f, z))); }
                fk.Save("FeedLine_Rollers", w);
                var slabs = new List<Transform>();
                for (int i = 0; i < 5; i++) { var s = new GameObject("FeedSlab" + i).transform; s.SetParent(w, false); var sk = new AK(); Riveted(sk, m.stone, null, new Vector3(0f, 0.5f, 0f), new Vector3(1.6f, 0.8f, 0.2f)); foreach (var g in sk.Save("FeedSlab" + i + "_Mesh", s)) g.transform.localPosition = Vector3.zero; slabs.Add(s); }
                var feed = new GameObject("FeedLine").AddComponent<Seven.AstRailCarrier>(); feed.transform.SetParent(w, false); feed.Path = new[] { new Vector3(-344f, -5.6f, 68f), new Vector3(-344f, -5.6f, 2f), new Vector3(-344.1f, -5.6f, 2f), new Vector3(-344.1f, -5.6f, 68f) }; feed.Cars = slabs.ToArray(); feed.Speed = 2f;
                var furnace = new AK(); Riveted(furnace, m.metalDark, m.metal, new Vector3(-344f, -4f, 1f), new Vector3(10f, 8f, 2f)); var (fv, ft) = furnace.Of(m.glow); MBox(fv, ft, new Vector3(-344f, -5f, 2.05f), new Vector3(4f, 3f, 0.05f), 0f, 0f); furnace.Save("Foundry_Mouth", w);
                r.prodLights.Add(PointLight("Foundry_Glow", new Vector3(-344f, -4f, 5f), 22f, 2.6f, w));
            }
            // the alcove: the glass over the feed line, the terminal + its display, the sealed maintenance locker (the blades)
            {
                var gv = new List<Vector3>(); var gt = new List<int>(); Quad(gv, gt, new Vector3(-336.4f, 1.2f, 38.6f), new Vector3(-336.4f, 4f, 38.6f), new Vector3(-336.4f, 4f, 51f), new Vector3(-336.4f, 1.2f, 51f));
                MeshObject("Alcove_Glass", SaveMesh(Finish(gv, gt), "Ast_AlcoveGlass"), m.glass, w, false);
                var tk = new AK(); Riveted(tk, m.metal, m.metalDark, A.AlcoveTerminal + new Vector3(0f, 0.55f, 0f), new Vector3(1.6f, 1.1f, 1.0f)); tk.Save("Alcove_Terminal", w);
                var sv = new List<Vector3>(); var st = new List<int>(); var sc = A.AlcoveTerminal + new Vector3(0f, 1.45f, -0.3f);
                Quad(sv, st, sc + new Vector3(0.7f, -0.35f, 0f), sc + new Vector3(0.7f, 0.35f, 0f), sc + new Vector3(-0.7f, 0.35f, 0f), sc + new Vector3(-0.7f, -0.35f, 0f));
                Quad(sv, st, sc + new Vector3(-0.7f, -0.35f, 0.005f), sc + new Vector3(-0.7f, 0.35f, 0.005f), sc + new Vector3(0.7f, 0.35f, 0.005f), sc + new Vector3(0.7f, -0.35f, 0.005f));   // both faces
                // the bypass route on it: three marks joined by a line (drawn as part of the screen)
                for (int i = 0; i < 3; i++) Ellipsoid(sv, st, sc + new Vector3(-0.45f + i * 0.45f, 0.1f - i * 0.08f, 0.02f), new Vector3(0.05f, 0.05f, 0.01f), 3, 6);
                r.alcoveScreen = MeshObject("Alcove_Display", SaveMesh(Finish(sv, st), "Ast_AlcoveDisplay"), new Material(m.glowSoft) { name = "Ast_alcovedisplay" }, w, false).GetComponent<Renderer>();
                Seven.Glowing.Set(r.alcoveScreen, 0.25f);
                var lk = new AK(); Riveted(lk, m.plate, m.metalDark, A.LockerAt + new Vector3(0.2f, 1.2f, 0f), new Vector3(0.5f, 2.4f, 1.4f)); lk.Save("Locker_Body", w);
                var door = new GameObject("Locker_Door").transform; door.SetParent(w, false); door.position = A.LockerAt + new Vector3(-0.08f, 0f, -0.7f);
                var dk = new AK(); Riveted(dk, m.metal, m.metalDark, new Vector3(0f, 1.2f, 0.7f), new Vector3(0.08f, 2.3f, 1.38f)); foreach (var g in dk.Save("Locker_DoorMesh", door)) g.transform.localPosition = Vector3.zero;
                r.lockerDoor = door;
                r.lockerLamp = GlowBlob("Locker_Lamp", A.LockerAt + new Vector3(-0.2f, 2.55f, 0f), 0.08f, m, w);
                // the blades inside: two crescent hilts with a faint light along them
                var bl = new GameObject("Locker_Blades").transform; bl.SetParent(w, false); bl.position = A.LockerAt + new Vector3(0.05f, 1.3f, 0f);
                foreach (var sz in new[] { -0.25f, 0.25f })
                {
                    var cv = new List<Vector3>(); var ct = new List<int>();
                    Sweep(cv, ct, Enumerable.Range(0, 11).Select(i => { float a = Mathf.Lerp(-55f, 55f, i / 10f) * Mathf.Deg2Rad; return new Vector3(0f, Mathf.Sin(a) * 0.45f, sz + (1f - Mathf.Cos(a)) * 0.35f * Mathf.Sign(sz)); }).ToList(), 0.015f, false, 5);
                    MeshObject("Locker_Blade" + (sz < 0 ? "L" : "R"), SaveMesh(SmoothFinish(cv, ct), "Ast_LockerBlade" + (sz < 0 ? "L" : "R")), bladeLight, bl, false).transform.localPosition = Vector3.zero;
                }
                r.lockerBlades = bl;
                AstLamp("AlcoveLamp", new Vector3(-328f, 3.6f, 44f), 10f, 1.0f, m.glow, m.metalDark, w, 0.16f);
                AstLamp("MaintPassLamp", new Vector3(-328f, 3.4f, 60f), 8f, 0.8f, m.glow, m.metalDark, w, 0.14f);
            }
            // control 1 (transport): a lever post by the pit; three cargo platforms parked low in the pit -> lined up across it
            {
                var c = A.TransportControl;
                var pk = new AK(); Column(pk, m.metalDark, c, 0f, 1.15f, 0.12f); pk.Save("TransportControl_Post", w);
                r.transportLever = new GameObject("TransportControl_Lever").transform; r.transportLever.SetParent(w, false); r.transportLever.position = c + Vector3.up * 1.15f;
                var lv = new List<Vector3>(); var lt = new List<int>(); Sweep(lv, lt, new[] { Vector3.zero, new Vector3(0f, 0.75f, 0.25f) }, 0.04f, false, 6); Ellipsoid(lv, lt, new Vector3(0f, 0.78f, 0.26f), Vector3.one * 0.08f, 4, 8);
                MeshObject("TransportControl_LeverArm", SaveMesh(SmoothFinish(lv, lt), "Ast_TLever"), m.steel, r.transportLever).transform.localPosition = Vector3.zero;
                var plats = new List<Transform>(); var parked = new List<Vector3>(); var aligned = new List<Vector3>();
                for (int i = 0; i < 3; i++)
                {
                    var p = new GameObject($"CargoPlatform{i + 1}").transform; p.SetParent(w, false);
                    var ck = new AK(); Riveted(ck, m.floor, m.metalDark, new Vector3(0f, -0.4f, 0f), new Vector3(5.5f, 0.8f, 6.2f)); var (gv2, gt2) = ck.Of(m.metalDark); MBox(gv2, gt2, new Vector3(0f, -1.1f, 0f), new Vector3(4.6f, 0.6f, 5.2f), 0f, 0f);
                    foreach (var g in ck.Save($"CargoPlatform{i + 1}_Deck", p)) g.transform.localPosition = Vector3.zero;
                    var al = new Vector3(-349.3f + i * 5.3f, 0f, 17f); var pa = new Vector3(al.x, -8f, 6f + i * 1.5f);
                    p.position = pa; plats.Add(p); parked.Add(pa); aligned.Add(al);
                }
                r.platforms = plats.ToArray(); r.parked = parked.ToArray(); r.aligned = aligned.ToArray();
            }
            // control 2 (cooling): the valve wheel held by a powered clamp, its power line from the wall; big coolant pipes to the
            // vents in the foundry passage (steam blocks it until the coolant runs)
            {
                var v0 = A.CoolingValve;
                var pk = new AK(); Pipe(pk, m.metal, new[] { new Vector3(v0.x, 0f, v0.z + 0.6f), new Vector3(v0.x, 1.4f, v0.z + 0.6f), new Vector3(-378f, 1.4f, v0.z + 0.6f), new Vector3(-378f, 1.4f, 20f), new Vector3(-384f, 1.4f, 20f) }, 0.32f);
                Riveted(pk, m.metalDark, m.metal, new Vector3(v0.x, 0.6f, v0.z + 0.6f), new Vector3(1.1f, 1.2f, 1.1f));
                pk.Save("Coolant_Pipes", w);
                r.valveWheel = new GameObject("CoolingValve_Wheel").transform; r.valveWheel.SetParent(w, false); r.valveWheel.position = v0 + new Vector3(0f, 1.4f, 0f); r.valveWheel.rotation = Quaternion.LookRotation(Vector3.back);
                var wv = new List<Vector3>(); var wt = new List<int>();
                Sweep(wv, wt, Enumerable.Range(0, 25).Select(i => new Vector3(Mathf.Cos(i / 24f * Mathf.PI * 2f) * 0.42f, Mathf.Sin(i / 24f * Mathf.PI * 2f) * 0.42f, 0f)).ToList(), 0.045f, true, 6);
                for (int s = 0; s < 3; s++) { float a = s * 60f * Mathf.Deg2Rad; Sweep(wv, wt, new[] { new Vector3(Mathf.Cos(a) * 0.42f, Mathf.Sin(a) * 0.42f, 0f), new Vector3(-Mathf.Cos(a) * 0.42f, -Mathf.Sin(a) * 0.42f, 0f) }, 0.03f, false, 5); }
                MeshObject("CoolingValve_WheelMesh", SaveMesh(SmoothFinish(wv, wt), "Ast_ValveWheel"), m.steel, r.valveWheel).transform.localPosition = Vector3.zero;
                r.clampJaw = new GameObject("ValveClamp_Jaw").transform; r.clampJaw.SetParent(w, false); r.clampJaw.position = v0 + new Vector3(0.3f, 1.4f, -0.15f);
                var jk = new AK(); Riveted(jk, m.metalDark, m.metal, new Vector3(0f, 0f, 0f), new Vector3(0.25f, 0.9f, 0.3f)); foreach (var g in jk.Save("ValveClamp_JawMesh", r.clampJaw)) g.transform.localPosition = Vector3.zero;
                r.clampPower = LightRun("ClampPowerLine", new[] { new Vector3(-366f, 3.2f, 34.0f), A.ClampLine, v0 + new Vector3(0.35f, 1.6f, -0.1f) }, 6, 0.06f, m.glowSoft, w);
                r.coolant = LightRun("CoolantRun", new[] { new Vector3(v0.x, 1.8f, v0.z + 0.6f), new Vector3(-378f, 1.8f, v0.z + 0.6f), new Vector3(-378f, 1.8f, 20f), new Vector3(-384f, 1.8f, 20f) }, 10, 0.08f, m.glowSoft, w);
                var steams = new List<ParticleSystem>();
                foreach (var p in A.SteamVents)
                {
                    var g = new GameObject("SteamVent").AddComponent<ParticleSystem>(); g.transform.SetParent(w, false); g.transform.position = p; g.transform.rotation = Quaternion.LookRotation(Vector3.up);
                    var mm = g.main; mm.startLifetime = 1.6f; mm.startSpeed = 5f; mm.startSize = 1.6f; mm.startColor = new Color(1f, 1f, 1f, 0.35f); mm.maxParticles = 300; mm.loop = true;
                    var em = g.emission; em.rateOverTime = 60f; var sh = g.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 18f; sh.radius = 0.3f;
                    g.GetComponent<ParticleSystemRenderer>().sharedMaterial = AdditiveMat("Ast_steam", 0.6f);
                    steams.Add(g);
                    var vk = new AK(); Riveted(vk, m.metalDark, m.metal, p + Vector3.up * 0.1f, new Vector3(1.2f, 0.2f, 1.2f)); vk.Save($"SteamVent{steams.Count}_Grate", w);
                }
                r.steam = steams.ToArray();
                // the foundry passage's walls (furnace mouths glowing behind grilles)
                var fk = new AK(); PlateWall(fk, m.metalDark, m.metal, new Vector3(-372f, 0f, 9.6f), new Vector3(-386f, 0f, 9.6f), 0f, 6f); PlateWall(fk, m.metalDark, m.metal, new Vector3(-372f, 0f, 18.4f), new Vector3(-386f, 0f, 18.4f), 0f, 6f);
                fk.Save("CoolPass_Wall", w);
            }
            // the final transport assembly (retracts in AST_06 into a slot in the south wall) and control 3 (the diverter)
            {
                var fp = new GameObject("FinalPlatform").transform; fp.SetParent(w, false); fp.position = Vector3.zero;
                var fk = new AK(); Riveted(fk, m.floor, m.metalDark, A.FinalPlatC + new Vector3(0f, -0.4f, 0f), new Vector3(13.2f, 0.8f, 8f)); var (gv, gt) = fk.Of(m.metalDark); MBox(gv, gt, A.FinalPlatC + new Vector3(0f, -1.2f, 0f), new Vector3(12f, 0.8f, 7f), 0f, 0f);
                fk.Save("FinalPlatform_Deck", fp);
                r.finalPlat = fp;
                var c = A.DiverterAt;
                var pk = new AK(); Riveted(pk, m.metal, m.metalDark, c + new Vector3(0f, 0.6f, 0f), new Vector3(1.2f, 1.2f, 0.8f)); pk.Save("Diverter_Housing", w);
                r.diverterLever = new GameObject("Diverter_Lever").transform; r.diverterLever.SetParent(w, false); r.diverterLever.position = c + Vector3.up * 1.2f;
                var lv = new List<Vector3>(); var lt = new List<int>(); Sweep(lv, lt, new[] { Vector3.zero, new Vector3(0f, 0.8f, 0.2f) }, 0.05f, false, 6); Ellipsoid(lv, lt, new Vector3(0f, 0.84f, 0.21f), Vector3.one * 0.09f, 4, 8);
                MeshObject("Diverter_LeverArm", SaveMesh(SmoothFinish(lv, lt), "Ast_DLever"), m.steel, r.diverterLever).transform.localPosition = Vector3.zero;
                r.controlLamps = new[] { GlowBlob("Control1_Lamp", A.TransportControl + new Vector3(0f, 2.0f, 0f), 0.14f, m, w), GlowBlob("Control2_Lamp", A.CoolingValve + new Vector3(0f, 2.5f, 0.6f), 0.14f, m, w), GlowBlob("Control3_Lamp", c + new Vector3(0f, 2.3f, 0f), 0.14f, m, w) };
                { var sk = new AK(); Column(sk, m.metalDark, A.TransportControl, 1.15f, 1.85f, 0.04f); Column(sk, m.metalDark, c, 1.2f, 2.15f, 0.04f); sk.Save("ControlLamp_Stems", w); }
                AstLamp("WorksLampE", new Vector3(-328f, 8f, 20f), 22f, 1.4f, m.glow, m.metalDark, w, 0.25f);
                AstLamp("WorksLampM", new Vector3(-362f, 8f, 20f), 22f, 1.4f, m.glow, m.metalDark, w, 0.25f);
                AstLamp("WorksLampW", new Vector3(-404f, 8f, 18f), 22f, 1.4f, m.glow, m.metalDark, w, 0.25f);
                AstLamp("WestPassLamp", new Vector3(-396f, 4.6f, 50f), 12f, 1.0f, m.glow, m.metalDark, w, 0.16f);
                PointLight("LedgeLight", new Vector3(-392f, -2f, 14f), 10f, 0.8f, w);
            }
            // the security machines: riveted domes on a drive wheel, padded restraint arms, a lamp eye
            var sec = new List<Seven.AstSecurity>();
            for (int i = 0; i < A.SecurityHomes.Length; i++)
            {
                var root = new GameObject($"SecurityMachine{i + 1}").transform; root.SetParent(w, false); root.position = A.SecurityHomes[i];
                var body = new GameObject("Body").transform; body.SetParent(root, false); body.localPosition = new Vector3(0f, 0.5f, 0f);
                var bv = new List<Vector3>(); var bt = new List<int>(); Append(bv, bt, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.55f, 0f), new Vector2(0.62f, 0.3f), new Vector2(0.55f, 0.9f), new Vector2(0.3f, 1.2f), new Vector2(0f, 1.25f) }, 0f, 360f, 16));
                for (int j = 0; j < 8; j++) Ellipsoid(bv, bt, Quaternion.Euler(0f, j * 45f, 0f) * new Vector3(0.6f, 0.32f, 0f), Vector3.one * 0.04f, 3, 6);
                MeshObject($"SecurityMachine{i + 1}_Dome", SaveMesh(SmoothFinish(bv, bt), $"Ast_SecDome{i}"), m.metal, body).transform.localPosition = Vector3.zero;
                var eye = new List<Vector3>(); var et = new List<int>(); Ellipsoid(eye, et, new Vector3(0f, 0.85f, 0.52f), new Vector3(0.14f, 0.1f, 0.06f), 4, 8);
                var eyeR = MeshObject($"SecurityMachine{i + 1}_Eye", SaveMesh(SmoothFinish(eye, et), $"Ast_SecEye{i}"), new Material(m.glowSoft) { name = $"Ast_seceye{i}" }, body, false); eyeR.transform.localPosition = Vector3.zero;
                Transform Arm(string n, float sx)
                {
                    var a = new GameObject(n).transform; a.SetParent(body, false); a.localPosition = new Vector3(sx * 0.6f, 0.7f, 0.1f);
                    var av = new List<Vector3>(); var at = new List<int>(); Sweep(av, at, new[] { Vector3.zero, new Vector3(sx * 0.25f, -0.1f, 0.5f) }, 0.06f, false, 6); Ellipsoid(av, at, new Vector3(sx * 0.27f, -0.1f, 0.56f), new Vector3(0.12f, 0.16f, 0.1f), 4, 8);
                    MeshObject(n + "_Mesh", SaveMesh(SmoothFinish(av, at), $"Ast_Sec{n}{i}"), m.metalDark, a).transform.localPosition = Vector3.zero;
                    return a;
                }
                var al = Arm("ArmL", -1f); var ar = Arm("ArmR", 1f);
                var wheel = new GameObject("Wheel").transform; wheel.SetParent(root, false); wheel.localPosition = new Vector3(0f, 0.35f, 0f);
                MeshObject($"SecurityMachine{i + 1}_WheelMesh", SaveMesh(RotZ(Lathe(new[] { new Vector2(0f, -0.15f), new Vector2(0.35f, -0.15f), new Vector2(0.36f, 0f), new Vector2(0.35f, 0.15f), new Vector2(0f, 0.15f) }, 0f, 360f, 14), Vector3.zero), $"Ast_SecWheel{i}"), m.metalDark, wheel).transform.localPosition = Vector3.zero;
                var s = root.gameObject.AddComponent<Seven.AstSecurity>(); s.Body = body; s.ArmL = al; s.ArmR = ar; s.Wheel = wheel; s.Eye = eyeR.GetComponent<Renderer>();
                sec.Add(s);
            }
            r.security = sec.ToArray();
        }
    }
}
