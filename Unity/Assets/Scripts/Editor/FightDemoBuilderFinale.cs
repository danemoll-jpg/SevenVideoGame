using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using F = Seven.FinaleLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 19 — the scene "Finale" (Docs/CHAPTER_FINALE.md): the hollow Moon of Chapter 1 (BuildCity: the same host, terraces,
    /// pyramids and Tunneler's machines round the same origin; Atmosphere_Moon + the Moon's own grade), the saucer berthed in the
    /// bay, and the lower service district built in code from the kit (no plain boxes: dressed masonry, lathed forms, swept
    /// conduits, mining dust): the berth and its overlook, the walkway, the service junction (the terminal with Tunneler's
    /// restricted markings, the power panel, the rusted gate), the narrow maintenance crossing, the landing and the ancient
    /// door (framed after vision_01), the circular archive chamber (seven sockets round the central platform, projection walls,
    /// conduits, an oculus for the pale light, the old maintenance barrier's alcove, the mining access passage). No new models:
    /// 7, Panther, the Scree, Tunneler (on his mining craft), the saucer. Videos: vision_01-07, the grey meadow clip and the
    /// colour game copy of Dan's master (FIN_14 - the one colour in the game).
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const string FinaleScenePath = "Assets/Scenes/Finale.unity";

        static void BuildRound19(AnimatorController sevenCtl, GameObject atmoPrefab, Seven.VoiceBank bank)
        {
            var visions = new[] { "vision_01_door.mp4", "vision_02_venus.mp4", "vision_03_europa.mp4", "vision_04_mars.mp4", "vision_05_asteroid.mp4", "vision_06_mothership.mp4", "vision_07_cattalus.mp4" };
            foreach (var v in visions) CopyVideo(v);
            CopyVideo("cattalus_meadow.mp4");
            // FIN_14: the colour game copy (1920x1044 h264) made beside Dan's master; the .mov itself is never read here
            var colourSrc = Path.Combine(RepoRoot, "Art", "Video", "color", "cattalus_meadow_color_game.mp4");
            UnityEngine.Video.VideoClip colour = null;
            if (File.Exists(colourSrc))
            {
                File.Copy(colourSrc, Path.Combine(ProjectRoot, "Assets/Video/cattalus_meadow_color_game.mp4"), true);
                AssetDatabase.ImportAsset("Assets/Video/cattalus_meadow_color_game.mp4", ImportAssetOptions.ForceSynchronousImport);
                colour = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Video/cattalus_meadow_color_game.mp4");
            }
            Log($"FIN_14 COLOUR (Round 19): {(colour != null ? $"the game copy of Dan's colour master in ({colour.length:0.0} s, {colour.width}x{colour.height})" : "MISSING Art/Video/color/cattalus_meadow_color_game.mp4 - the grey clip plays")}");
            var clips = visions.Select(v => AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Video/" + v)).ToArray();
            var meadow = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Video/cattalus_meadow.mp4");
            var catCtl = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimDir + "/Cat.controller");
            var catFur = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Cat_fur.mat");
            var catWhisker = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Cat_whisker.mat");
            var tun = SetupStaticModel("tunneler", "tunneler_v1.fbx");
            BuildFinaleScene(sevenCtl, bank, catCtl, catFur, catWhisker, tun, clips, meadow, colour);
        }

        static void BuildFinaleScene(AnimatorController sevenCtl, Seven.VoiceBank bank, AnimatorController catCtl, Material catFur, Material catWhisker, GameObject tunModel,
            UnityEngine.Video.VideoClip[] visions, UnityEngine.Video.VideoClip meadow, UnityEngine.Video.VideoClip colour)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null; RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.32f, 0.32f, 0.32f);   // QF29 pass: the chamber read as one dark grey at 0.2
            var shs = new SphericalHarmonicsL2(); shs.AddAmbientLight(RenderSettings.ambientLight); RenderSettings.ambientProbe = shs;
            var atmoP = AssetDatabase.LoadAssetAtPath<GameObject>(AtmoMoonPath);
            var atmo = (GameObject)PrefabUtility.InstantiatePrefab(atmoP); atmo.name = "Atmosphere_Moon";
            var moonVol = AssetDatabase.LoadAssetAtPath<VolumeProfile>(AtmoDir + "/Moon_Volume.asset") ?? AssetDatabase.LoadAssetAtPath<VolumeProfile>(AtmoDir + "/Cave_Volume.asset");
            if (moonVol != null) { var vgo = new GameObject("MoonVolume"); var vol = vgo.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 10f; vol.sharedProfile = moonVol; }
            var world = new GameObject("Finale").transform;
            var m = new MoonMats
            {
                rock = Dissolve("Moon_rock", Gray(0.14f), 0.12f, false), rockDark = Dissolve("Moon_rock_dark", Gray(0.07f), 0.1f, false),
                metal = Dissolve("Moon_metal", Gray(0.34f), 0.6f, false, 0.5f), metalDark = Dissolve("Moon_metal_dark", Gray(0.16f), 0.5f, false, 0.4f),
                grate = Dissolve("Moon_grate", Gray(0.22f), 0.45f, false, 0.3f), ancient = Dissolve("Moon_ancient", Gray(0.46f), 0.3f, false),
                ancientDark = Dissolve("Moon_ancient_dark", Gray(0.27f), 0.25f, false), glow = Dissolve("Moon_lamp", Color.white, 0.5f, false),
                crate = Dissolve("Moon_crate", Gray(0.24f), 0.2f, false), hull = Dissolve("Saucer_hull", Gray(0.95f), 0.9f, false, 0.6f),
                hullUnder = Dissolve("Saucer_under", Gray(0.74f), 0.8f, false, 0.45f), rim = Dissolve("Saucer_rim", Color.white, 0.6f, false),
                inscription = Dissolve("Moon_inscription", Color.white, 0.5f, false), scree = Dissolve("Scree_body", Gray(0.42f), 0.25f, false),
                screeHood = Dissolve("Scree_hood", Gray(0.2f), 0.1f, false), black = Dissolve("Moon_black", Gray(0.01f), 0.0f, false),
            };
            var soft = Dissolve("Fin_glow_soft", Gray(0.55f), 0.4f, false); soft.EnableKeyword("_EMISSION"); soft.SetColor("_EmissionColor", Color.white * 0.4f); EditorUtility.SetDirty(soft);
            var dust = Dissolve("Fin_mining_dust", Gray(0.52f), 0.05f, false);
            var stone = Dissolve("Fin_chamber_stone", Gray(0.5f), 0.25f, false);
            var stoneDark = Dissolve("Fin_chamber_stone_dark", Gray(0.36f), 0.2f, false);
            var screen = Dissolve("Fin_screen", Gray(0.08f), 0.7f, false); screen.EnableKeyword("_EMISSION"); screen.SetColor("_EmissionColor", Color.white * 0.25f); EditorUtility.SetDirty(screen);

            // ---------------------------------------------------------------- the hollow Moon (Chapter 1's city, the same origin)
            var city = BuildCity(world, m);

            // ---------------------------------------------------------------- the ground (the walkable floor)
            foreach (var z in F.Zones)
            {
                if (z.Name == "GateZ" || z.Name == "DoorZ" || z.Name.StartsWith("Chamber")) continue;   // the chamber's floor is one round slab (below)
                MGround("Ground_" + z.Name, new Vector3(z.X0, F.Y - 0.5f, z.Z0), new Vector3(z.X1, F.Y, z.Z1), z.Name == "Crossing" ? m.grate : z.Name == "Landing" || z.Name == "Alcove" ? m.ancient : z.Name == "Cavern" || z.Name == "Breach" ? m.rock : m.metal, world);
            }
            MGround("Ground_GateZ", new Vector3(9.6f, F.Y - 0.5f, -121.6f), new Vector3(13.4f, F.Y, -118.8f), m.metal, world);
            MGround("Ground_DoorZ", new Vector3(9.4f, F.Y - 0.5f, -151.2f), new Vector3(13.6f, F.Y, -140.4f), m.ancient, world);
            var wg = new GameObject("WorldGround").AddComponent<Seven.WorldGround>();
            wg.Shape = Seven.Ground.Shape.Zones;
            wg.Zones = F.Zones.Select(z => new Seven.WorldGround.ZoneDef { Name = z.Name, Min = new Vector2(z.X0, z.Z0), Max = new Vector2(z.X1, z.Z1), Y0 = F.Y, Y1 = F.Y, Axis = 0, Enabled = z.On }).ToArray();
            wg.ZoneObstacles = new Vector3[0];

            // ---------------------------------------------------------------- the berth, the overlook, the walkway
            var district = new GameObject("ServiceDistrict").transform; district.SetParent(world, false);
            var dk = new AK();
            { var (v, t) = dk.Of(m.metalDark); Append(v, t, Lathe(new[] { new Vector2(0f, -0.48f), new Vector2(16.4f, -0.48f), new Vector2(16.8f, -0.9f), new Vector2(15.6f, -2.6f), new Vector2(0f, -2.6f) }, 0f, 360f, 48, null, F.SaucerAt)); }
            { var (gv, gt) = dk.Of(soft); Append(gv, gt, Lathe(new[] { new Vector2(12.6f, 0.03f), new Vector2(13.3f, 0.03f) }, 0f, 360f, 48, null, F.SaucerAt)); }
            // railings round the berth (open toward the walkway and the overlook), the walkway's rails, lamp posts
            { var (rv, rt) = dk.Of(m.metal);
              var rail = new List<Vector3>(); for (int i = 0; i <= 40; i++) { float a = Mathf.Lerp(200f, 520f, i / 40f) * Mathf.Deg2Rad; rail.Add(F.SaucerAt + new Vector3(Mathf.Sin(a) * 15.8f, 1.05f, Mathf.Cos(a) * 15.8f)); }
              Sweep(rv, rt, rail, 0.06f, false, 6); foreach (var p in rail.Where((x, i) => i % 4 == 0)) Sweep(rv, rt, new[] { p - Vector3.up * 1.05f, p }, 0.05f, false, 5);
              foreach (var sx in new[] { 8.6f, 14.4f }) { Sweep(rv, rt, new[] { new Vector3(sx, F.Y + 1.05f, -93.6f), new Vector3(sx, F.Y + 1.05f, -104.4f) }, 0.06f, false, 6); for (float z = -94f; z > -105f; z -= 2.6f) Sweep(rv, rt, new[] { new Vector3(sx, F.Y, z), new Vector3(sx, F.Y + 1.05f, z) }, 0.05f, false, 5); }
              Sweep(rv, rt, new[] { new Vector3(4f, F.Y + 1.05f, -94f), new Vector3(8.6f, F.Y + 1.05f, -94f) }, 0.06f, false, 6); Sweep(rv, rt, new[] { new Vector3(14.4f, F.Y + 1.05f, -94f), new Vector3(19f, F.Y + 1.05f, -94f) }, 0.06f, false, 6); }
            dk.Save("Berth", district, false);
            foreach (var p in new[] { new Vector3(4.2f, F.Y, -92.6f), new Vector3(18.8f, F.Y, -92.6f), new Vector3(8.2f, F.Y, -100f), new Vector3(14.8f, F.Y, -100f) }) FinLamp(p, m, district);

            // ---------------------------------------------------------------- the junction (FIN_03)
            var jk = new AK();
            { var (v, t) = jk.Of(m.metalDark);
              // the back wall with the gate opening, side walls, a low ceiling of beams and pipes
              MBox(v, t, new Vector3(5.2f, F.Y + 2.5f, -119.8f), new Vector3(8.6f, 5f, 0.6f), 0f, 0.02f); MBox(v, t, new Vector3(17.8f, F.Y + 2.5f, -119.8f), new Vector3(8.6f, 5f, 0.6f), 0f, 0.02f);
              MBox(v, t, new Vector3(11.5f, F.Y + 4.4f, -119.8f), new Vector3(4.6f, 1.2f, 0.6f), 0f, 0.02f);
              MBox(v, t, new Vector3(1.2f, F.Y + 2.5f, -112f), new Vector3(0.6f, 5f, 15.4f), 0f, 0.02f); MBox(v, t, new Vector3(21.8f, F.Y + 2.5f, -112f), new Vector3(0.6f, 5f, 15.4f), 0f, 0.02f);
              for (float z = -105f; z > -120f; z -= 3.2f) MBox(v, t, new Vector3(11.5f, F.Y + 5.1f, z), new Vector3(21f, 0.3f, 0.35f), 0f, 0f); }
            { var (pv, pt) = jk.Of(m.metal); for (int i = 0; i < 4; i++) Sweep(pv, pt, new[] { new Vector3(1.7f, F.Y + 3.2f + i * 0.35f, -105f), new Vector3(1.7f, F.Y + 3.2f + i * 0.35f, -119.2f), new Vector3(21.3f, F.Y + 3.2f + i * 0.35f, -119.2f) }, 0.08f + 0.02f * (i % 2), false, 6); }
            // the damaged service junction the worker repairs: an open box of cable runs, a sparking break
            { var (bv, bt) = jk.Of(m.grate); MBox(bv, bt, F.WorkerAt + new Vector3(1.6f, 1.2f, -0.9f), new Vector3(1.6f, 2.4f, 0.7f), 0f, 0.01f); var (cv, ct) = jk.Of(m.metal); for (int i = 0; i < 5; i++) Sweep(cv, ct, new[] { F.WorkerAt + new Vector3(1.0f + i * 0.28f, 0.2f, -0.6f), F.WorkerAt + new Vector3(1.0f + i * 0.28f, 2.2f, -0.6f) }, 0.035f, false, 4); }
            jk.Save("Junction", district, false);
            // the terminal with Tunneler's restricted-access markings (bars and a crossed seal, white on the dark screen)
            var term = new GameObject("RestrictedTerminal").transform; term.SetParent(district, false); term.position = F.TerminalAt; term.rotation = Quaternion.LookRotation(Vector3.forward);
            { var tk = new AK(); var (v, t) = tk.Of(m.metalDark); MBox(v, t, new Vector3(0f, 0.8f, 0f), new Vector3(1.2f, 1.6f, 0.5f), 0f, 0.01f); var (sv, st) = tk.Of(screen); MBox(sv, st, new Vector3(0f, 1.45f, 0.24f), new Vector3(1.0f, 0.62f, 0.04f), 0f, 0f);
              var (mv, mt) = tk.Of(m.inscription); for (int i = 0; i < 4; i++) MBox(mv, mt, new Vector3(-0.3f + i * 0.2f, 1.45f, 0.27f), new Vector3(0.06f, 0.5f, 0.01f), 0f, 0f); Sweep(mv, mt, new[] { new Vector3(-0.38f, 1.2f, 0.27f), new Vector3(0.38f, 1.7f, 0.27f) }, 0.025f, false, 4); Sweep(mv, mt, new[] { new Vector3(-0.38f, 1.7f, 0.27f), new Vector3(0.38f, 1.2f, 0.27f) }, 0.025f, false, 4);
              foreach (var g in tk.Save("Terminal", term, false)) g.transform.localPosition = Vector3.zero; }
            // the power panel + its lever (reroute the old route's power)
            var panel = new GameObject("PowerPanel").transform; panel.SetParent(district, false); panel.position = F.PowerPanel; panel.rotation = Quaternion.LookRotation(Vector3.right);
            Transform lever; Renderer panelLamp;
            { var pk = new AK(); var (v, t) = pk.Of(m.metal); MBox(v, t, new Vector3(0f, 1.0f, -0.25f), new Vector3(1.1f, 2.0f, 0.4f), 0f, 0.01f); foreach (var g in pk.Save("PanelBody", panel, false)) g.transform.localPosition = Vector3.zero;
              lever = new GameObject("PowerLever").transform; lever.SetParent(panel, false); lever.localPosition = new Vector3(0f, 1.2f, 0.02f); lever.localRotation = Quaternion.Euler(-40f, 0f, 0f);
              var lv = new List<Vector3>(); var lt = new List<int>(); Sweep(lv, lt, new[] { Vector3.zero, Vector3.up * 0.55f }, 0.04f, false, 6); Ellipsoid(lv, lt, Vector3.up * 0.6f, Vector3.one * 0.07f, 4, 8); MeshObject("Lever", SaveMesh(SmoothFinish(lv, lt), "Fin_Lever"), m.metal, lever, false);
              var lampGo = new List<Vector3>(); var lampT = new List<int>(); Ellipsoid(lampGo, lampT, new Vector3(0f, 1.8f, 0.0f), Vector3.one * 0.09f, 5, 8); panelLamp = MeshObject("PanelLamp", SaveMesh(SmoothFinish(lampGo, lampT), "Fin_PanelLamp"), new Material(soft) { name = "Fin_panel_lamp" }, panel, false).GetComponent<Renderer>(); }
            // the gate (two barred leaves) and its rusted lock above them
            var gateLeaf = new GameObject("GateLeaf").transform; gateLeaf.SetParent(district, false); gateLeaf.position = F.GateAt;
            { var gk = new AK(); var (v, t) = gk.Of(m.metal); for (int i = 0; i < 9; i++) Sweep(v, t, new[] { new Vector3(-1.9f + i * 0.475f, 0.05f, 0f), new Vector3(-1.9f + i * 0.475f, 3.7f, 0f) }, 0.05f, false, 5); foreach (var y in new[] { 0.4f, 1.9f, 3.5f }) Sweep(v, t, new[] { new Vector3(-1.95f, y, 0f), new Vector3(1.95f, y, 0f) }, 0.06f, false, 5); foreach (var g in gk.Save("GateBars", gateLeaf, false)) g.transform.localPosition = Vector3.zero; }
            var gateLock = new GameObject("GateLock").transform; gateLock.SetParent(district, false); gateLock.position = F.GateLock;
            { var lk = new AK(); var (v, t) = lk.Of(m.crate); MBox(v, t, Vector3.zero, new Vector3(1.1f, 0.7f, 0.5f), 0f, 0.02f); var (rv, rt) = lk.Of(m.metalDark); Sweep(rv, rt, new[] { new Vector3(-0.5f, 0.42f, 0f), new Vector3(-0.5f, 0.8f, 0f), new Vector3(0.5f, 0.8f, 0f), new Vector3(0.5f, 0.42f, 0f) }, 0.06f, false, 5); foreach (var g in lk.Save("Lock", gateLock, false)) g.transform.localPosition = Vector3.zero; }
            foreach (var p in new[] { new Vector3(3f, F.Y, -106f), new Vector3(20f, F.Y, -106f), new Vector3(3f, F.Y, -118f), new Vector3(20f, F.Y, -118f) }) FinLamp(p, m, district);

            // ---------------------------------------------------------------- the maintenance crossing + the landing + the ancient door
            var ck = new AK();
            { var (rv, rt) = ck.Of(m.metal); foreach (var sx in new[] { F.CrossX0 - 0.05f, F.CrossX1 + 0.05f }) { Sweep(rv, rt, new[] { new Vector3(sx, F.Y + 0.95f, F.CrossZ1), new Vector3(sx, F.Y + 0.95f, F.CrossZ0) }, 0.04f, false, 5); for (float z = F.CrossZ1; z > F.CrossZ0; z -= 2f) Sweep(rv, rt, new[] { new Vector3(sx, F.Y, z), new Vector3(sx, F.Y + 0.95f, z) }, 0.035f, false, 4); }
              var (sv, st) = ck.Of(m.metalDark); foreach (var z in new[] { -123f, -127f, -131f }) Sweep(sv, st, new[] { new Vector3(F.CrossX0, F.Y - 0.5f, z), new Vector3(F.CrossX0 - 3f, F.Y - 9f, z) }, 0.12f, false, 6); }
            { var (v, t) = ck.Of(m.ancientDark);
              // the landing's old walls - older than the mine (the same carved masonry as the lunar door chamber)
              MBox(v, t, new Vector3(4.2f, F.Y + 3f, -137f), new Vector3(0.8f, 6f, 9f), 0f, 0.06f); MBox(v, t, new Vector3(18.8f, F.Y + 3f, -137f), new Vector3(0.8f, 6f, 9f), 0f, 0.06f);
              // the door's frame (after ref_lunar_door / vision_01): a deep stepped portal, the lintel with its band of marks
              MBox(v, t, new Vector3(8.4f, F.Y + 3.4f, -142f), new Vector3(2.2f, 6.8f, 1.6f), 0f, 0.04f); MBox(v, t, new Vector3(14.6f, F.Y + 3.4f, -142f), new Vector3(2.2f, 6.8f, 1.6f), 0f, 0.04f);
              MBox(v, t, new Vector3(11.5f, F.Y + 6.3f, -142f), new Vector3(8.4f, 1.4f, 1.6f), 0f, 0.04f);
              MBox(v, t, new Vector3(6.2f, F.Y + 3f, -141.2f), new Vector3(2.4f, 6f, 0.8f), 0f, 0.06f); MBox(v, t, new Vector3(16.8f, F.Y + 3f, -141.2f), new Vector3(2.4f, 6f, 0.8f), 0f, 0.06f); }
            { var (iv, it) = ck.Of(m.inscription); for (int i = 0; i < 11; i++) { var c = new Vector3(8.2f + i * 0.66f, F.Y + 6.3f, -141.15f); Ellipsoid(iv, it, c, new Vector3(0.12f, 0.12f, 0.02f), 3, 8); } }
            ck.Save("Crossing", district, false);
            Transform leafL, leafR; Renderer doorMarks;
            { var lm = new List<Renderer>();
              Transform Leaf(string n, float sx)
              {
                  var lf = new GameObject(n).transform; lf.SetParent(district, false); lf.position = new Vector3(11.5f + sx * 1.15f, F.Y, -142f);
                  var k = new AK(); var (v, t) = k.Of(m.ancient); MBox(v, t, new Vector3(0f, 2.6f, 0f), new Vector3(2.26f, 5.2f, 0.5f), 0f, 0.02f);
                  var (iv, it) = k.Of(m.inscription);
                  // the marks: concentric arcs and a five-sided figure (the dodecahedron's face) split between the two leaves
                  for (int r = 0; r < 3; r++) { var arc = new List<Vector3>(); for (int i = 0; i <= 16; i++) { float a = Mathf.Lerp(-80f, 80f, i / 16f) * Mathf.Deg2Rad; arc.Add(new Vector3(-sx * 1.13f + sx * Mathf.Cos(a) * (0.5f + r * 0.32f), 3.0f + Mathf.Sin(a) * (0.5f + r * 0.32f), 0.27f)); } Sweep(iv, it, arc, 0.018f, false, 4); }
                  var pent = new List<Vector3>(); for (int i = 0; i <= 5; i++) { float a = (90f + i * 72f) * Mathf.Deg2Rad; var p = new Vector3(-sx * 1.13f + Mathf.Cos(a) * 0.32f, 3.0f + Mathf.Sin(a) * 0.32f, 0.28f); if (Mathf.Sign(p.x + sx * 1.13f) == sx || Mathf.Abs(p.x + sx * 1.13f) < 0.01f) pent.Add(p); }
                  if (pent.Count > 1) Sweep(iv, it, pent, 0.02f, false, 4);
                  foreach (var g in k.Save(n, lf, false)) { g.transform.localPosition = Vector3.zero; if (g.name.Contains("inscription")) lm.Add(g.GetComponent<Renderer>()); }
                  return lf;
              }
              leafL = Leaf("DoorLeafL", -1f); leafR = Leaf("DoorLeafR", 1f); doorMarks = lm.FirstOrDefault(); }
            FinLamp(new Vector3(5.6f, F.Y, -134f), m, district); FinLamp(new Vector3(17.4f, F.Y, -134f), m, district);

            // ---------------------------------------------------------------- the archive chamber (FIN_05+)
            var chamber = new GameObject("ArchiveChamber").transform; chamber.SetParent(world, false);
            var ak = new AK();
            var c0 = F.ChamberC; var proj = new List<Renderer>(); var sockets = new List<Transform>(); var socketLights = new List<Renderer>();
            var breachPanelAt = new List<(Vector3 p, float a)>();
            { var (v, t) = ak.Of(stoneDark);
              // the wall: 32 dressed panels round the circle, openings at the entrance (north), the mining access (south) and the alcove (east)
              for (int i = 0; i < 32; i++)
              {
                  float a = (i + 0.5f) * 360f / 32f; float r = a * Mathf.Deg2Rad;
                  bool north = a < 12f || a > 348f, south = a > 168f && a < 192f, east = a > 79f && a < 101f;
                  if (north || south || east) continue;
                  var p = c0 + new Vector3(Mathf.Sin(r) * (F.ChamberR + 0.4f), F.ChamberH * 0.5f, Mathf.Cos(r) * (F.ChamberR + 0.4f));
                  if (F.BreachPanels.Any(b => Mathf.Abs(b - a) < 1f)) { breachPanelAt.Add((p, a)); continue; }   // QF31 (8): built on their own (his rig tears them out)
                  MBox(v, t, p, new Vector3(2.9f, F.ChamberH, 0.8f), a, 0.03f);
              }
              // over the openings: lintels; the ceiling ring with its oculus (the pale light falls through it in FIN_13)
              foreach (var a in new[] { 0f, 90f }) { float r = a * Mathf.Deg2Rad; MBox(v, t, c0 + new Vector3(Mathf.Sin(r) * (F.ChamberR + 0.4f), F.ChamberH - 1.6f, Mathf.Cos(r) * (F.ChamberR + 0.4f)), new Vector3(5.4f, 3.2f, 0.9f), a, 0.03f); }
              breachPanelAt.Add((c0 + new Vector3(0f, F.ChamberH - 1.6f, -(F.ChamberR + 0.4f)), 1000f));   // the access's lintel goes with the breach (QF31 8)
              Append(v, t, Lathe(new[] { new Vector2(F.ChamberR + 1.2f, F.ChamberH), new Vector2(3.6f, F.ChamberH + 2.2f), new Vector2(3.4f, F.ChamberH + 2.6f), new Vector2(F.ChamberR + 1.2f, F.ChamberH + 0.6f) }, 0f, 360f, 48, null, c0));
              // the floor: one round slab inside the wall (out to the mining access and the alcove)
              { var (flv, flt) = ak.Of(stone); Append(flv, flt, Lathe(new[] { new Vector2(F.WellR, -0.6f), new Vector2(F.WellR, 0f), new Vector2(F.ChamberR + 0.6f, 0f), new Vector2(F.ChamberR + 0.6f, -0.5f), new Vector2(F.WellR + 0.02f, -0.6f) }, 0f, 360f, 48, null, c0)); }   // QF31 (8): an annulus - the archive's table lowers into the hole (the light well)
              // the rim of the floor and the platform (stepped, the archive surface on top)
              // QF30 (4): the platform's base stays inside PlatformR + 0.3 (the 1.0.0-rc1 skirt out to +1.2 m swallowed her feet, Panther
              // and where she laid him - Ground's obstacle circle stopped them at +0.55, inside the visible skirt)
              }
            // QF31 (8): the archive's table is its own piece now (it lowers flush into the floor as a shallow light well once the release begins)
            var platform = BuildArchivePlatform(c0, stoneDark, soft, chamber, out var wellRim);
            // mining dust and rubble over the old floor; the barrier alcove's frame; the mining access passage's mouth
            { var (dv, dt) = ak.Of(dust); var rnd = new System.Random(19); for (int i = 0; i < 70; i++) { float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = 4f + (float)rnd.NextDouble() * 9.4f; Ellipsoid(dv, dt, c0 + new Vector3(Mathf.Sin(a) * r, 0.0f, Mathf.Cos(a) * r), new Vector3(0.5f + (float)rnd.NextDouble() * 0.9f, 0.035f, 0.4f + (float)rnd.NextDouble() * 0.8f), 3, 14); } }
            { var (v, t) = ak.Of(m.metalDark); MBox(v, t, F.BarrierAt + new Vector3(0f, 4.6f, 0f), new Vector3(1.2f, 0.8f, 6.4f), 0f, 0.02f); foreach (var sz in new[] { -3.1f, 3.1f }) MBox(v, t, F.BarrierAt + new Vector3(0f, 2.5f, sz), new Vector3(1.0f, 5f, 0.5f), 0f, 0.02f); MBox(v, t, F.BarrierAt + new Vector3(2.2f, 2.5f, 0f), new Vector3(0.6f, 5f, 6.4f), 0f, 0.02f); }
            ak.Save("Chamber", chamber, false);
            // QF31 (8): the wall panels his rig tears out + the mining cavern beyond (the fight spills into it)
            var breach = BuildBreach(breachPanelAt, stoneDark, chamber);
            BuildMiningCavern(m, dust, world);
            // the archive surface (a shallow light well), the conduits from the sockets, the conduit he damages
            var surf = new List<Vector3>(); var surfT = new List<int>(); surf.AddRange(Lathe(new[] { new Vector2(0f, 0.01f), new Vector2(F.PlatformR - 0.15f, 0.01f) }, 0f, 360f, 32, null, Vector3.zero).vertices); surfT.AddRange(Lathe(new[] { new Vector2(0f, 0.01f), new Vector2(F.PlatformR - 0.15f, 0.01f) }, 0f, 360f, 32, null, Vector3.zero).triangles);
            var surface = MeshObject("ArchiveSurface", SaveMesh(SmoothFinish(surf, surfT), "Fin_ArchiveSurface"), new Material(soft) { name = "Fin_archive_surface" }, platform, false); surface.transform.position = c0 + Vector3.up * F.PlatformH;
            var cond = new List<Vector3>(); var condT = new List<int>();
            for (int i = 0; i < 7; i++) { var s = F.Socket(i); Sweep(cond, condT, new[] { s + Vector3.up * 0.03f, Vector3.Lerp(s, c0, 0.5f) + Vector3.up * 0.03f, c0 + Flat(s - c0).normalized * (F.PlatformR + 0.32f) + Vector3.up * 0.03f }, 0.06f, false, 5); }
            var conduit = MeshObject("ArchiveConduits", SaveMesh(SmoothFinish(cond, condT), "Fin_ArchiveConduits"), new Material(soft) { name = "Fin_conduits" }, chamber, false);
            var cwd = Flat(F.ConduitAt - c0).normalized; var cwb = c0 + cwd * 13.9f; var cws = Vector3.Cross(Vector3.up, cwd);
            var brk = new List<Vector3>(); var brkT = new List<int>(); Sweep(brk, brkT, new[] { F.ConduitAt + cwd * 0.5f + Vector3.up * 0.5f, cwb + Vector3.up * 0.6f, cwb + Vector3.up * 3.6f, cwb + Vector3.up * 6.6f + cws * 1.4f }, 0.12f, false, 6);
            var conduitBreak = MeshObject("DamagedConduit", SaveMesh(SmoothFinish(brk, brkT), "Fin_DamagedConduit"), new Material(soft) { name = "Fin_conduit_west" }, chamber, false);
            var conduitConsole = ArchiveConsole("ConduitConsole", F.ConduitAt, Flat(c0 - F.ConduitAt), c0, m, stone, stoneDark, soft, chamber, true);   // QF30 (11)
            // the seven sockets (lathed pedestals with a cup) and their lights; a projection wall behind each
            for (int i = 0; i < 7; i++)
            {
                var sp = F.Socket(i);
                var sk = new AK(); var (v, t) = sk.Of(m.ancient);
                Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.55f, 0f), new Vector2(0.4f, 0.3f), new Vector2(0.28f, 0.95f), new Vector2(0.45f, 1.05f), new Vector2(0.42f, 1.12f), new Vector2(0.2f, 1.02f), new Vector2(0f, 1.0f) }, 0f, 360f, 20, null, Vector3.zero));
                var so = new GameObject("Socket" + i).transform; so.SetParent(chamber, false); so.position = sp; foreach (var g in sk.Save("Socket" + i, so, false)) g.transform.localPosition = Vector3.zero;
                sockets.Add(so);
                var lv = new List<Vector3>(); var lt = new List<int>(); Append(lv, lt, Lathe(new[] { new Vector2(0.43f, 1.1f), new Vector2(0.47f, 1.1f) }, 0f, 360f, 20, null, Vector3.zero));
                socketLights.Add(MeshObject("SocketLight" + i, SaveMesh(SmoothFinish(lv, lt), "Fin_SocketLight" + i), new Material(soft) { name = "Fin_socket_light_" + i }, so, false).GetComponent<Renderer>());
                // the projection wall: a tall panel in the chamber wall behind the socket (the world's image appears on it)
                // QF34 (3): Europa's screen (2) stood at 102.9 deg with its north metre inside the mouth of the barrier alcove (open 79-101 deg):
                // the alcove's frame post and top beam, its glowing BarrierFrameLines, the BarrierField sheet, the ExternalLock, the rig and
                // the crew parked there all crossed it. It moves round the wall to 114 deg (108-120 deg: solid wall, clear of the alcove).
                var dir = i == F.EuropaScreen ? Quaternion.Euler(0f, F.EuropaScreenYaw, 0f) * Vector3.forward : Flat(sp - c0).normalized; var wp = c0 + dir * (F.ChamberR - 0.05f) + Vector3.up * 4.2f;
                var pq = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(pq.GetComponent<Collider>()); pq.name = "ProjectionWall" + i; pq.transform.SetParent(chamber, false); pq.transform.position = wp; pq.transform.rotation = Quaternion.LookRotation(dir); pq.transform.localScale = new Vector3(3.2f, 4.4f, 1f);
                var pm = new Material(Shader.Find("Seven/GalleryScreen") ?? Shader.Find("Universal Render Pipeline/Unlit")) { name = "Fin_projection_" + i }; pq.GetComponent<Renderer>().sharedMaterial = pm; pq.GetComponent<Renderer>().enabled = false; proj.Add(pq.GetComponent<Renderer>());
            }
            // the old maintenance barrier (a pale field that closes the alcove) + its control; the external lock
            var barrier = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(barrier.GetComponent<Collider>()); barrier.name = "BarrierField"; barrier.transform.SetParent(chamber, false);
            barrier.transform.position = F.BarrierAt + new Vector3(-0.55f, 2.3f, 0f); barrier.transform.rotation = Quaternion.LookRotation(Vector3.left); barrier.transform.localScale = new Vector3(6f, 4.6f, 1f);
            barrier.GetComponent<Renderer>().sharedMaterial = Markers_Field(); barrier.SetActive(false);
            // QF30 (11): the barrier's control in the archive's style, at the room's edge; QF30 (10): the frame lights when it can be used
            var barrierConsole = ArchiveConsole("BarrierConsole", F.BarrierControl, Flat(c0 - F.BarrierControl), F.BarrierAt, m, stone, stoneDark, soft, chamber, false);
            var bctl = barrierConsole.root; Renderer barrierLight = barrierConsole.node;
            Renderer barrierFrameLines;
            { var fl = new List<Vector3>(); var ft = new List<int>();
              // QF34 (3): the south post's strip and the south end of the top strip stood in front of Europa's screen from the room (the
              // gameplay camera's rays: 664 frames) - the frame's light now runs on the north post and the top's north part only
              foreach (var sz in new[] { 2.8f }) Sweep(fl, ft, new[] { F.BarrierAt + new Vector3(-0.52f, 0.05f, sz), F.BarrierAt + new Vector3(-0.52f, 4.15f, sz) }, 0.03f, false, 4);
              Sweep(fl, ft, new[] { F.BarrierAt + new Vector3(-0.62f, 4.2f, -0.6f), F.BarrierAt + new Vector3(-0.62f, 4.2f, 2.8f) }, 0.03f, false, 4);
              barrierFrameLines = MeshObject("BarrierFrameLines", SaveMesh(SmoothFinish(fl, ft), "Fin_BarrierFrameLines"), new Material(soft) { name = "Fin_barrier_frame" }, chamber, false).GetComponent<Renderer>(); }
            // QF30 (9): the dead cables that lead to each power objective (they light when it is done)
            var powerCable = DeadCable("PowerCable", new[] { new Vector3(11.5f, F.Y + 0.02f, -118.6f), new Vector3(9.4f, F.Y + 0.02f, -117.6f), new Vector3(7.2f, F.Y + 0.02f, -115.6f), F.PowerPanel + new Vector3(0.45f, 0.02f, 0f) }, m, soft, district);
            var barrierCable = DeadCable("BarrierCable", new[] { F.BarrierAt + new Vector3(-0.6f, 0.02f, -3.1f), F.BarrierAt + new Vector3(-1.4f, 0.02f, -4.4f), F.BarrierControl + Flat(F.BarrierAt - F.BarrierControl).normalized * 0.75f + Vector3.up * 0.02f }, m, soft, chamber);
            // QF30 (6): the big mining drills boring into the old structure round the chamber and along the service route
            var wallDrills = new List<Transform>();
            for (int i = 0; i < F.ChamberDrills.Length; i++) { var at = F.ChamberDrillAt(i); wallDrills.Add(WallDrill("ChamberDrill" + i, at, Flat(c0 - at).normalized + Vector3.down * 0.18f, F.ChamberDrills[i].scale, m, chamber)); }
            for (int i = 0; i < F.RouteDrills.Length; i++) wallDrills.Add(WallDrill("RouteDrill" + i, F.RouteDrills[i].at, F.RouteDrills[i].dir, 0.7f, m, district));
            var lockT = new GameObject("ExternalLock").transform; lockT.SetParent(chamber, false); lockT.position = F.BarrierAt + new Vector3(-0.7f, 4.0f, -3.1f);
            { var lk = new AK(); var (v, t) = lk.Of(m.metalDark); MBox(v, t, Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f), 0f, 0.02f); foreach (var g in lk.Save("Lock", lockT, false)) g.transform.localPosition = Vector3.zero; }
            // the pale light (FIN_13): a soft column falling through the oculus - plain, no symbols, off until then
            var pale = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.DestroyImmediate(pale.GetComponent<Collider>()); pale.name = "PaleLight"; pale.transform.SetParent(chamber, false);
            pale.transform.position = c0 + Vector3.up * (F.ChamberH * 0.5f + 1.3f); pale.transform.localScale = new Vector3(5.2f, F.ChamberH * 0.5f + 1.3f, 5.2f);
            var paleMat = new Material(Shader.Find("Seven/Additive") ?? Shader.Find("Universal Render Pipeline/Unlit")) { name = "Fin_pale_light" }; if (paleMat.HasProperty("_Intensity")) paleMat.SetFloat("_Intensity", 0f);
            pale.GetComponent<Renderer>().sharedMaterial = paleMat; pale.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            // chamber light: low and soft from the platform, a cool key from the oculus
            PointLight("ChamberGlow", c0 + Vector3.up * 3.2f, 28f, 6f, chamber);
            for (int i = 0; i < 7; i++) { var sp = F.Socket(i); PointLight("SocketLamp" + i, c0 + Flat(sp - c0) * 1.15f + Vector3.up * 3.4f, 12f, 3.5f, chamber); }
            PointLight("DoorwayGlow", F.DoorAt + new Vector3(0f, 3f, -4f), 16f, 8f, chamber);
            var key = new GameObject("OculusKey").AddComponent<Light>(); key.transform.SetParent(chamber, false); key.transform.position = c0 + Vector3.up * (F.ChamberH + 2f); key.transform.rotation = Quaternion.Euler(90f, 0f, 0f); key.type = LightType.Spot; key.spotAngle = 85f; key.range = 26f; key.intensity = 10f; key.shadows = LightShadows.Soft;

            // ---------------------------------------------------------------- mining drills intruding on the city + city terminals
            var drills = new List<Transform>(); var terminals = new List<Renderer>();
            foreach (var (p, h) in new[] { (new Vector3(-16f, F.Y - 80f, -126f), 104f), (new Vector3(40f, F.Y - 70f, -130f), 96f), (new Vector3(-30f, F.Y - 90f, -60f), 120f), (new Vector3(52f, F.Y - 90f, -70f), 116f) })
            {
                // QF34 (5): a mining crane (rc6: a bare rod with plates and a drill bit at its foot, rising out of the haze and stopping in
                // mid-air above the berth - one of the "hanging things" Dan never understood): a lattice mast up out of the workings below, a
                // slewing jib at its head with a work light, a cable down from the trolley to an ore bucket; the jib slews slowly (runtime)
                var dr = new GameObject("MiningDrill").transform; dr.SetParent(world, false); dr.position = p;
                var mk = new AK(); var (v, t) = mk.Of(m.metalDark);
                foreach (var cx in new[] { -1.2f, 1.2f }) foreach (var cz in new[] { -1.2f, 1.2f }) Sweep(v, t, new[] { new Vector3(cx, 0f, cz), new Vector3(cx, h, cz) }, 0.22f, false, 6);
                for (float y = 0f; y < h - 5f; y += 5f) { Sweep(v, t, new[] { new Vector3(-1.2f, y, -1.2f), new Vector3(1.2f, y + 5f, -1.2f) }, 0.09f, false, 4); Sweep(v, t, new[] { new Vector3(1.2f, y, 1.2f), new Vector3(-1.2f, y + 5f, 1.2f) }, 0.09f, false, 4); Sweep(v, t, new[] { new Vector3(-1.2f, y, 1.2f), new Vector3(-1.2f, y + 5f, -1.2f) }, 0.09f, false, 4); Sweep(v, t, new[] { new Vector3(1.2f, y, -1.2f), new Vector3(1.2f, y + 5f, 1.2f) }, 0.09f, false, 4); }
                foreach (var g in mk.Save("CraneMast", dr, false)) g.transform.localPosition = Vector3.zero;
                var bit = new GameObject("CraneHead").transform; bit.SetParent(dr, false); bit.localPosition = Vector3.up * h;
                var hk = new AK(); var (hv, ht) = hk.Of(m.metalDark); var (mv, mt) = hk.Of(m.metal); var (gv0, gt0) = hk.Of(m.glow);
                MBox(hv, ht, new Vector3(0f, 0.6f, 0f), new Vector3(3.2f, 1.2f, 3.2f), 0f, 0.02f);                       // the slewing house
                MBox(mv, mt, new Vector3(0f, 1.4f, 7.5f), new Vector3(0.9f, 0.9f, 17f), 0f, 0f);                         // the jib
                MBox(mv, mt, new Vector3(0f, 1.4f, -4f), new Vector3(0.9f, 0.9f, 6f), 0f, 0f);                           // the counter-jib
                MBox(hv, ht, new Vector3(0f, 0.7f, -6.2f), new Vector3(2.2f, 1.8f, 1.6f), 0f, 0.02f);                    // its counterweight
                Sweep(mv, mt, new[] { new Vector3(0f, 1.8f, 15.5f), new Vector3(0f, 5.2f, 0f), new Vector3(0f, 1.8f, -6.8f) }, 0.06f, false, 4);   // the stays over the apex
                MBox(hv, ht, new Vector3(0f, 0.75f, 14.5f), new Vector3(1.3f, 0.5f, 1.3f), 0f, 0f);                     // the trolley
                Sweep(mv, mt, new[] { new Vector3(0f, 0.5f, 14.5f), new Vector3(0f, -13.5f, 14.5f) }, 0.05f, false, 4);  // the hoist cable
                Append(hv, ht, Lathe(new[] { new Vector2(0f, -16.8f), new Vector2(1.3f, -16.8f), new Vector2(1.7f, -14.2f), new Vector2(1.75f, -13.9f), new Vector2(0f, -13.9f) }, 0f, 360f, 14, null, new Vector3(0f, 0f, 14.5f)));   // the ore bucket
                var rnd3 = new System.Random(drills.Count + 5); for (int o = 0; o < 6; o++) Ellipsoid(mv, mt, new Vector3(((float)rnd3.NextDouble() - 0.5f) * 2f, -13.9f, 14.5f + ((float)rnd3.NextDouble() - 0.5f) * 2f), Vector3.one * 0.5f, 3, 6);   // ore heaped in it
                MBox(gv0, gt0, new Vector3(0f, 0.95f, 15.8f), new Vector3(0.7f, 0.25f, 0.5f), 0f, 0f);                  // the work light at the jib's end
                foreach (var g in hk.Save("CraneHead", bit, false)) g.transform.localPosition = Vector3.zero;
                PointLight("DrillWork", p + Vector3.up * (h + 0.4f) + Vector3.forward * 15.8f, 22f, 1.4f, bit);
                drills.Add(bit);
            }
            foreach (var (p, yaw) in new[] { (new Vector3(3.6f, F.Y, -111f), 90f), (new Vector3(19.4f, F.Y, -116f), -90f), (new Vector3(20.6f, F.Y, -78f), -90f), (new Vector3(2.4f, F.Y, -84f), 90f) })
            {
                var tr = new GameObject("CityTerminal").transform; tr.SetParent(district, false); tr.position = p; tr.rotation = Quaternion.Euler(0f, yaw, 0f);
                var tk = new AK(); var (v, t) = tk.Of(m.metalDark); MBox(v, t, new Vector3(0f, 0.7f, 0f), new Vector3(0.9f, 1.4f, 0.4f), 0f, 0.01f); foreach (var g in tk.Save("CityTerminalBody", tr, false)) g.transform.localPosition = Vector3.zero;
                var sq = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(sq.GetComponent<Collider>()); sq.name = "CityTerminalScreen"; sq.transform.SetParent(tr, false); sq.transform.localPosition = new Vector3(0f, 1.15f, 0.21f); sq.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); sq.transform.localScale = new Vector3(0.75f, 0.45f, 1f);
                sq.GetComponent<Renderer>().sharedMaterial = new Material(soft) { name = "Fin_city_terminal_" + terminals.Count }; terminals.Add(sq.GetComponent<Renderer>());
            }

            // ---------------------------------------------------------------- the saucer, 7, the camera, Panther
            var saucer = PlaceSaucer(world, F.SaucerAt);
            var beam = saucer.GetComponent<Seven.SaucerBeam>();
            CheckSaucer("Finale", saucer);
            var seven = Spawn(SevenDir + "/7_character.fbx", "7", F.BeamDown + new Vector3(0f, 0f, -2f), Vector3.back, sevenCtl);
            var camGo = new GameObject("ChapterCamera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            cam.fieldOfView = 54f; cam.nearClipPlane = 0.08f; cam.farClipPlane = 3000f; camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData(); camData.renderPostProcessing = true; camData.requiresDepthTexture = true; camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camGo.transform.position = F.BeamDown + new Vector3(-3f, 1.8f, 3f);
            var dirGo = new GameObject("FinaleDirector");
            var input = dirGo.AddComponent<Seven.ChapterInput>();
            var s7 = seven.AddComponent<Seven.ChapterSeven>(); s7.Input = input; s7.CameraTransform = camGo.transform;
            var ccam = camGo.AddComponent<Seven.ChapterCamera>(); ccam.Target = seven.transform; ccam.Input = input;
            var catRoot = SpawnCat("Cat", F.BeamDown + new Vector3(1f, 0f, -2.4f), Vector3.back, catCtl, catFur, catWhisker, out var catAnim);
            var cat = catRoot.AddComponent<Seven.ChapterCat>(); cat.Seven = seven.transform; cat.Anim = catAnim; cat.Cam = cam;
            catRoot.AddComponent<Seven.CatPoseOverlay>();

            // ---------------------------------------------------------------- the Scree: the worker, the elder, workers at the drills
            var folk = new GameObject("Scree").transform; folk.SetParent(world, false);
            Seven.ScreeGuard Scree(string n, Vector3 p, Vector3 f, string state)
            {
                var g = SpawnAnyScree(n, p, f, sevenCtl, m, false); g.transform.SetParent(folk, true);
                var sg = g.AddComponent<Seven.ScreeGuard>(); sg.Post = p; sg.PostFacing = f; sg.PostState = state; sg.ScanAngle = 0f; sg.UseGround = false; sg.Range = 0f;
                return sg;
            }
            var worker = Scree("Scree_Worker", F.WorkerAt, Vector3.right, "work");
            var elder = Scree("Scree_Elder", new Vector3(19f, F.Y, -114.5f), Vector3.left, "work"); elder.transform.localScale = Vector3.one * 1.08f;
            var others = new List<Seven.ScreeGuard> { Scree("Scree_BerthWorker", new Vector3(21f, F.Y, -76f), Vector3.left, "work"), Scree("Scree_BerthWorker2", new Vector3(2.2f, F.Y, -86f), Vector3.right, "work") };
            // QF30 (16): the crew who come down after FIN_11_05 and take Tunneler (hidden until then)
            var crew = new List<Seven.ScreeGuard>();
            for (int i = 0; i < 3; i++) { var cr = Scree("Scree_Crew" + i, new Vector3(10.5f + i, F.Y, -146f), Vector3.back, "idle"); cr.gameObject.SetActive(false); crew.Add(cr); }

            // ---------------------------------------------------------------- Tunneler on his mining craft (hidden until FIN_08)
            var craft = new GameObject("TunnelerCraft").transform; craft.SetParent(world, false); craft.position = F.TunnelerStart; craft.rotation = Quaternion.LookRotation(Vector3.forward);
            var rig = BuildTunnelerRig(craft, m, soft);
            Transform tunneler = null, drill = null;
            if (tunModel != null)
            {
                var tm = (GameObject)PrefabUtility.InstantiatePrefab(tunModel); tm.name = "Tunneler"; tm.transform.SetParent(rig.Hull, false); tm.transform.localPosition = Vector3.up * rig.DeckTop; tm.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                tunneler = tm.transform; drill = tm.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "DrillHead");
                var tmat = Dissolve("Europa_tunneler", Gray(0.5f), 0.35f, false); foreach (var r in tm.GetComponentsInChildren<Renderer>()) r.sharedMaterial = tmat;
                RigTunnelerBody(tm, "Finale");                                                                    // P2 (9): bones + body language
                if (drill != null) { var dh = Seven.DrillHead.On(drill, tunneler); dh.Bake(); DrillAxisCheck("Finale", dh); }   // QF30 (5): the one shared drill
            }
            var under = new GameObject("CraftLightFromBelow").AddComponent<Light>(); under.transform.SetParent(rig.Hull, false); under.transform.localPosition = new Vector3(0f, 0.4f, 1.6f); under.transform.localRotation = Quaternion.Euler(-60f, 180f, 0f);
            under.type = LightType.Spot; under.range = 8f; under.spotAngle = 70f; under.intensity = 5f; under.shadows = LightShadows.Soft;
            craft.gameObject.SetActive(false);

            // ---------------------------------------------------------------- the dodecahedron (FIN_04: it answers the door; FIN_05: the Moon's socket)
            var dodeca = new GameObject("Dodecahedron").transform; dodeca.SetParent(world, false); dodeca.position = F.DoorView + Vector3.up * 1.1f;
            { var dm = Seven.ElarimShape.BodyMesh(Seven.MoonLayout.ShapeRadius); MeshObject("Body", SaveMesh(dm, "Fin_Dodecahedron"), m.ancient, dodeca, false); }
            dodeca.gameObject.SetActive(false);

            // ---------------------------------------------------------------- UI, dialogue, the guide light, the director
            var ui = new GameObject("ChapterUI").AddComponent<Seven.ChapterUI>(); ui.Cam = cam;
            var dlg = dirGo.AddComponent<Seven.DialoguePlayer>(); dlg.Bank = bank; dlg.UI = ui;
            var legend = new GameObject("ControlsLegend").AddComponent<Seven.ControlsLegend>(); legend.Cam = cam; legend.Set = Seven.ControlsLegend.Layout.Chapter; legend.HidePanels = true;
            var focus = new GameObject("FinaleFocus"); var fv = focus.AddComponent<Volume>(); fv.isGlobal = true; fv.priority = 100f; fv.weight = 0f; fv.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Atmosphere/Galley_Focus.asset"); focus.AddComponent<Seven.CloseUpFocus>();
            var gl0 = new List<Vector3>(); var glt = new List<int>(); Ellipsoid(gl0, glt, Vector3.zero, Vector3.one * 0.5f, 10, 14);
            var guideLight = MeshObject("GuideLight", SaveMesh(SmoothFinish(gl0, glt), "Fin_GuideLight"), m.glow, world, false);
            PointLight("GuideLightLamp", Vector3.zero, 18f, 2.4f, guideLight.transform).transform.localPosition = Vector3.zero;
            guideLight.SetActive(false);
            var d = dirGo.AddComponent<Seven.FinaleDirector>();
            d.Seven = s7; d.Cam = ccam; d.Input = input; d.Dlg = dlg; d.UI = ui; d.Legend = legend; d.SaucerBeam = beam; d.Saucer = saucer;
            d.Atmo = atmo.GetComponent<Seven.Atmosphere>(); d.GuideLight = guideLight.transform; d.Cat = cat;
            d.Worker = worker; d.Elder = elder; d.Workers = others.ToArray(); d.Tunneler = tunneler; d.TunnelerDrill = drill; d.TunnelerCraft = craft;
            d.PowerPanelLamp = panelLamp; d.PowerLever = lever; d.GateLockT = gateLock; d.GateLeaf = gateLeaf; d.TerminalScreen = term.GetComponentsInChildren<Renderer>().FirstOrDefault(r => r.sharedMaterial == screen);
            d.DoorLeafL = leafL; d.DoorLeafR = leafR; d.DoorMarkings = doorMarks;
            d.Sockets = sockets.ToArray(); d.SocketLights = socketLights.ToArray(); d.PlatformSurface = surface.GetComponent<Renderer>(); d.ConduitGlow = conduit.GetComponent<Renderer>(); d.ConduitBreak = conduitBreak.GetComponent<Renderer>();
            d.ProjectionWalls = proj.ToArray(); d.BarrierField = barrier.transform; d.BarrierControlT = bctl; d.Lock = lockT; d.BarrierLight = barrierLight; d.PaleLight = pale.GetComponent<Renderer>();
            d.MiningDrills = drills.ToArray(); d.CityTerminals = terminals.ToArray(); d.Dodecahedron = dodeca;
            d.Rig = rig; d.WallDrills = wallDrills.ToArray(); d.Crew = crew.ToArray();
            d.BarrierFrameLines = barrierFrameLines; d.BarrierLines = barrierConsole.lines; d.ConduitLines = conduitConsole.lines; d.ConduitNode = conduitConsole.node; d.ConduitConsoleT = conduitConsole.root;
            d.PowerCable = powerCable; d.BarrierCable = barrierCable;
            d.ArchivePlatform = platform; d.WellRim = wellRim; d.BreachPieces = breach;                      // QF31 (8)
            d.VisionLoops = VisionLoops(); d.Heaven = HeavenClips(); d.MemoriesVideo = MemoriesVideo(); d.MemoryClips = MemoryClipsIn();   // QF31 (9 / A)
            StagingMarksCheck();
            d.Visions = visions; d.MeadowGray = meadow; d.MeadowColour = colour;
            d.Memories = Directory.GetFiles(Path.Combine(ProjectRoot, "Assets/Resources/Finale/Memories"), "*.png").OrderBy(x => x).Select(p => (Texture)AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Finale/Memories/" + Path.GetFileName(p))).Where(x => x != null).ToArray();
            d.KyleArt = Directory.GetFiles(Path.Combine(ProjectRoot, "Assets/Resources/Finale/Kyle"), "*.jpg").Where(p => !p.Contains("thank_you")).OrderBy(x => x).Select(p => (Texture)AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Finale/Kyle/" + Path.GetFileName(p))).Where(x => x != null).ToArray();
            d.KyleThanks = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Finale/Kyle/kyle_7_thank_you.jpg");
            AddProbe(new Vector3(11.5f, F.Y + 4f, -100f), new Vector3(40f, 14f, 60f), Vector3.zero);
            AddProbe(F.ChamberC + Vector3.up * 5f, new Vector3(32f, 14f, 32f), Vector3.zero);
            QF18SceneChecks("Finale", true);
            EditorSceneManager.SaveScene(scene, FinaleScenePath);
            Log($"scene saved {FinaleScenePath} (the Moon finale: the city {(city != null ? "in" : "MISSING")}, Tunneler {(tunModel != null ? "Dan's model on his craft" : "MISSING")}, visions {visions.Count(v => v != null)} / 7, the grey meadow {(meadow != null ? "in" : "MISSING")}, the colour master's game copy {(colour != null ? "in" : "MISSING")}, memories {d.Memories.Length}, Kyle's art {d.KyleArt.Length} + the thank-you)");
        }

        /// <summary>A district lamp: a lathed post with a soft white globe (white emission; a real light).</summary>
        static void FinLamp(Vector3 at, MoonMats m, Transform parent)
        {
            var k = new AK(); var (v, t) = k.Of(m.metalDark);
            Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.18f, 0f), new Vector2(0.08f, 0.2f), new Vector2(0.06f, 2.9f), new Vector2(0f, 2.9f) }, 0f, 360f, 10, null, at));
            var (gv, gt) = k.Of(m.glow); Ellipsoid(gv, gt, at + Vector3.up * 3.05f, Vector3.one * 0.16f, 6, 10);
            k.Save("Lamp", parent, true);
            PointLight("LampLight", at + Vector3.up * 3f, 11f, 0.8f, parent);
        }
        /// <summary>The barrier's field: a soft additive white sheet.</summary>
        static Material Markers_Field()
        {
            var sh = Shader.Find("Seven/Additive"); var mat = new Material(sh != null ? sh : Shader.Find("Universal Render Pipeline/Unlit")) { name = "Fin_barrier_field" };
            if (mat.HasProperty("_Intensity")) mat.SetFloat("_Intensity", 0.18f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white * 0.18f);
            var path = "Assets/Art/Materials/Fin_barrier_field.mat"; AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
