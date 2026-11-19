using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using V = Seven.VenusLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 12 — Venus part 2 in the Venus scene: VEN_04 -> VEN_05 the way down (the root gate, an open root pit cut into
    /// the hillside with steps, a root tunnel, the root chamber with hanging roots and glowing root bulbs, a deep tunnel
    /// east under the basin) and VEN_05 the control chamber (Wormmaster on his dais above the machinery, grayscale
    /// underlighting; three control devices with cables to the dais; three creature troughs with burrows at their ends;
    /// the raised ledge in the south-west corner; the containment barrier sunk round the dais and its console). Also the
    /// changed world after the threat is stopped (the repaired channel with water, workers back on the paths) and the
    /// small cube copy of Venus's record (C10). Numbers: Seven.VenusLayout.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        struct Part2 { public Seven.ControlDevice[] devices; public Transform crystal, barrier, console; public Renderer consoleGlow, barrierGlow; public Seven.WormmasterRig wm; public Seven.WormCreature[] worms; public GameObject gate, repaired, dry; public Transform record; public Light[] under; }

        static void BuildVenusPart2(Transform world, VenusMats m, ElarimMats em, Seven.VenusDirector d, GameObject worm, AnimatorController wormCtl,
            System.Func<string, Vector3, Vector3, string, Seven.ElarimNpc> makeElarim, Seven.DialoguePlayer dlg)
        {
            var root = new GameObject("VEN_05_Underground").transform; root.SetParent(world, false);
            var rnd = new System.Random(12);
            var rock = Dissolve("Venus_rock_under", Gray(0.2f), 0.18f, false);
            var rootMat = Dissolve("Venus_roots", Gray(0.34f), 0.25f, false);
            var machine = Dissolve("Venus_machine", Gray(0.42f), 0.55f, false, 0.4f);
            var trough = Dissolve("Venus_trough", Gray(0.1f), 0.1f, false);
            var glowSoft = Dissolve("Venus_glow_soft", Gray(0.9f), 0.5f, false); glowSoft.EnableKeyword("_EMISSION"); glowSoft.SetColor("_EmissionColor", Color.white * 1.1f); EditorUtility.SetDirty(glowSoft);

            // ---- the root gate (a grown root door; it sinks into the ground after C05) + the railing split for the pit
            Block("Obs_RailS_W", new Vector3(64f, -6f, 133.6f), new Vector3(66.5f, -5f, 134f), m.stone, world);
            Block("Obs_RailS_E", new Vector3(71.5f, -6f, 133.6f), new Vector3(88f, -5f, 134f), m.stone, world);
            var gate = new GameObject("RootGate").transform; gate.SetParent(world, false);
            Block("Door", new Vector3(66.5f, -6f, 133.75f), new Vector3(71.5f, -1.2f, 134.25f), m.stoneDark, gate);
            { var v = new List<Vector3>(); var t = new List<int>();
              for (int i = 0; i < 7; i++) { float x = 66.9f + i * 0.7f; Tube(v, t, new[] { new Vector3(x, -6f, 133.6f), new Vector3(x + 0.1f, -3.5f, 133.55f), new Vector3(x - 0.1f, -1.3f, 133.6f) }, 0.16f, 0.1f); }
              MeshObject("DoorRoots", SaveMesh(SmoothFinish(v, t), "Venus_RootGateRoots"), rootMat, gate); }
            var arch = new List<Vector3>(); var archT = new List<int>();
            var arcPts = new List<Vector3>(); for (int i = 0; i <= 12; i++) { float a = i / 12f * Mathf.PI; arcPts.Add(new Vector3(69f - Mathf.Cos(a) * 3.1f, -6.2f + Mathf.Sin(a) * 6f, 134.1f)); }
            Sweep(arch, archT, arcPts, 0.45f, false, 8);
            MeshObject("RootGate_Arch", SaveMesh(SmoothFinish(arch, archT), "Venus_RootGateArch"), rootMat, world);
            Lamp("RootGate_Lamp", new Vector3(69f, -0.6f, 134.6f), 9f, 0.8f, m.glow, world, new Vector3(0.22f, 0.22f, 0.22f));

            // ---- the root pit: a slot cut into the hillside, rock walls up to the ground, steps down
            for (float z = V.RootPitBotZ - 4.6f; z < V.RootPitTopZ; z += 1.8f)
                foreach (var (x0, x1, xs) in new[] { (64.9f, 66.5f, 65.2f), (71.5f, 73.1f, 72.8f) })
                {
                    float top = Mathf.Max(VenusTerrain.RawHeight(xs - 1.5f, z), VenusTerrain.RawHeight(xs - 1.5f, z + 1.8f), VenusTerrain.RawHeight(xs + 1.5f, z), VenusTerrain.RawHeight(xs + 1.5f, z + 1.8f)) + 0.6f;
                    float bot = (z < V.RootPitBotZ ? V.RootPitBotY - 3f : V.RootPitHeight(z)) - 1.2f;
                    Block($"Pit_Wall_{x0:0}_{z:0}", new Vector3(x0, bot, z), new Vector3(x1, top, z + 1.85f), rock, root);
                }
            { var v = new List<Vector3>(); var t = new List<int>();
              for (float z = V.RootPitBotZ + 0.3f; z < V.RootLandZ + 0.6f; z += 0.62f) AddBox(v, t, new Vector3(69f, V.RootPitHeight(z) + 0.06f, z), new Vector3(4.8f, 0.12f, 0.36f));
              MeshObject("Pit_Steps", SaveMesh(Finish(v, t), "Venus_PitSteps"), m.stoneMid, root, false); }
            // roots spilling down the pit walls
            { var v = new List<Vector3>(); var t = new List<int>();
              for (int i = 0; i < 18; i++) { float z = V.RootPitBotZ + (float)rnd.NextDouble() * 16f; float x = i % 2 == 0 ? 66.55f : 71.45f; float top = VenusTerrain.RawHeight(x + (x < 69f ? -1.5f : 1.5f), z);
                Tube(v, t, new[] { new Vector3(x, top, z), new Vector3(x + (x < 69 ? 0.15f : -0.15f), Mathf.Lerp(top, V.RootPitHeight(z), 0.5f), z + 0.4f), new Vector3(x + (x < 69 ? 0.3f : -0.3f), V.RootPitHeight(z) + 0.4f, z + 0.2f) }, 0.12f, 0.05f, 6); }
              MeshObject("Pit_Roots", SaveMesh(SmoothFinish(v, t), "Venus_PitRoots"), rootMat, root, false); }
            Lamp("Pit_Lamp", new Vector3(69f, V.RootPitBotY + 2.4f, V.RootPitBotZ + 1.5f), 10f, 1.0f, glowSoft, root, new Vector3(0.3f, 0.3f, 0.3f));

            // ---- the root tunnel (z 96 -> 117.6, falling to the root chamber)
            float TunY(float z) => Mathf.Lerp(V.ChamberY, V.RootPitBotY, Mathf.InverseLerp(V.TunnelBotZ, V.RootPitBotZ + 0.6f, z));
            Block("Tunnel_WallW", new Vector3(64.9f, V.ChamberY - 1f, V.TunnelBotZ), new Vector3(66.5f, V.RootPitBotY + 6f, V.RootPitBotZ + 0.2f), rock, root);
            Block("Tunnel_WallE", new Vector3(71.5f, V.ChamberY - 1f, V.TunnelBotZ), new Vector3(73.1f, V.RootPitBotY + 6f, V.RootPitBotZ + 0.2f), rock, root);
            SlopeSlab("Tunnel_Ceiling", 64.9f, V.TunnelBotZ, 73.1f, V.RootPitBotZ + 0.2f, V.ChamberY + 5.2f, V.RootPitBotY + 5.2f, 1, rock, root, -1.2f);
            // the portal: a frame of grown roots where the pit enters the hill
            { var v = new List<Vector3>(); var t = new List<int>(); var p = new List<Vector3>();
              for (int i = 0; i <= 10; i++) { float a = i / 10f * Mathf.PI; p.Add(new Vector3(69f - Mathf.Cos(a) * 2.6f, V.RootPitBotY - 0.1f + Mathf.Sin(a) * 4.9f, V.RootPitBotZ + 0.1f)); }
              Sweep(v, t, p, 0.35f, false, 8);
              for (float z = V.TunnelBotZ + 2f; z < V.RootPitBotZ; z += 3.2f) { float y = TunY(z); Tube(v, t, new[] { new Vector3(66.6f, y, z), new Vector3(67.4f, y + 4.4f, z + 0.3f), new Vector3(70.6f, y + 4.5f, z + 0.3f), new Vector3(71.4f, y, z) }, 0.2f, 0.14f, 6); }
              MeshObject("Tunnel_Roots", SaveMesh(SmoothFinish(v, t), "Venus_TunnelRoots"), rootMat, root); }
            for (float z = V.TunnelBotZ + 5f; z < V.RootPitBotZ; z += 7f) Lamp($"Tunnel_Bulb_{z:0}", new Vector3(z % 2 < 1 ? 66.9f : 71.1f, TunY(z) + 3.2f, z), 8f, 0.9f, glowSoft, root, new Vector3(0.25f, 0.32f, 0.25f));

            // ---- the root chamber (y -22): huge hanging roots, glowing root bulbs
            float cy = V.ChamberY;
            Block("RootCh_W", new Vector3(58.8f, cy - 1f, 82.8f), new Vector3(60f, cy + 11f, 97.8f), rock, root);
            Block("RootCh_S", new Vector3(58.8f, cy - 1f, 82.8f), new Vector3(81.2f, cy + 11f, 84f), rock, root);
            Block("RootCh_NW", new Vector3(58.8f, cy - 1f, 96.6f), new Vector3(66.5f, cy + 11f, 97.8f), rock, root);
            Block("RootCh_NE", new Vector3(71.5f, cy - 1f, 96.6f), new Vector3(81.2f, cy + 11f, 97.8f), rock, root);
            Block("RootCh_ES", new Vector3(80f, cy - 1f, 82.8f), new Vector3(81.2f, cy + 11f, 86f), rock, root);
            Block("RootCh_EN", new Vector3(80f, cy - 1f, 92f), new Vector3(81.2f, cy + 11f, 97.8f), rock, root);
            Block("RootCh_Ceiling", new Vector3(58.8f, cy + 10f, 82.8f), new Vector3(81.2f, cy + 11.2f, 97.8f), rock, root, false);
            { var v = new List<Vector3>(); var t = new List<int>();
              for (int i = 0; i < 26; i++)
              {
                  float x = 61f + (float)rnd.NextDouble() * 18f, z = 85f + (float)rnd.NextDouble() * 11f;
                  if (x > 65.5f && x < 72.5f && z > 93f) continue;                       // keep the way in clear
                  float low = cy + 2.4f + (float)rnd.NextDouble() * 4.5f;
                  Tube(v, t, new[] { new Vector3(x, cy + 10f, z), new Vector3(x + 0.4f, (cy + 10f + low) * 0.5f, z + 0.3f), new Vector3(x + 0.1f, low, z - 0.2f) }, 0.3f + (float)rnd.NextDouble() * 0.3f, 0.08f, 7);
              }
              foreach (var (x, z) in new[] { (60.2f, 85f), (60.2f, 95f), (79.8f, 84.5f), (79.8f, 96f) }) Tube(v, t, new[] { new Vector3(x, cy, z), new Vector3(x, cy + 5f, z), new Vector3(x + (x < 70 ? 2f : -2f), cy + 10f, z) }, 1.1f, 0.6f, 9);
              MeshObject("RootCh_Roots", SaveMesh(SmoothFinish(v, t), "Venus_RootChamberRoots"), rootMat, root); }
            foreach (var (x, z) in new[] { (63f, 87f), (77f, 94f), (70f, 85.5f), (76f, 88.5f) }) Lamp($"RootCh_Bulb_{x:0}_{z:0}", new Vector3(x, cy + 3.4f, z), 11f, 1.2f, glowSoft, root, new Vector3(0.35f, 0.45f, 0.35f));

            // ---- the deep tunnel east (y -22 -> -30)
            Block("Deep_WallS", new Vector3(80f, V.ControlY - 1f, 84.8f), new Vector3(103.4f, cy + 7f, 86f), rock, root);
            Block("Deep_WallN", new Vector3(80f, V.ControlY - 1f, 92f), new Vector3(103.4f, cy + 7f, 93.2f), rock, root);
            SlopeSlab("Deep_Ceiling", 79.4f, 84.8f, 104f, 93.2f, cy + 5.6f, V.ControlY + 5.6f, 0, rock, root, -1.2f);
            for (float x = 84f; x < 102f; x += 6f) Lamp($"Deep_Bulb_{x:0}", new Vector3(x, Mathf.Lerp(cy, V.ControlY, Mathf.InverseLerp(79.4f, 104f, x)) + 3.6f, x % 12 < 6 ? 86.5f : 91.5f), 8f, 0.8f, glowSoft, root, new Vector3(0.25f, 0.3f, 0.25f));

            // ---- VEN_05: the control chamber (y -30)
            float y0 = V.ControlY;
            Block("Ctrl_WallW_S", new Vector3(102.2f, y0 - 1f, 63f), new Vector3(103.4f, y0 + 17f, 86f), rock, root);
            Block("Ctrl_WallW_N", new Vector3(102.2f, y0 - 1f, 92f), new Vector3(103.4f, y0 + 17f, 113f), rock, root);
            Block("Ctrl_WallW_Top", new Vector3(102.2f, y0 + 5.4f, 86f), new Vector3(103.4f, y0 + 17f, 92f), rock, root);
            Block("Ctrl_WallE", new Vector3(140.6f, y0 - 1f, 63f), new Vector3(141.8f, y0 + 17f, 113f), rock, root);
            Block("Ctrl_WallS", new Vector3(102.2f, y0 - 1f, 63f), new Vector3(141.8f, y0 + 17f, 64.2f), rock, root);
            Block("Ctrl_WallN", new Vector3(102.2f, y0 - 1f, 111.8f), new Vector3(141.8f, y0 + 17f, 113f), rock, root);
            Block("Ctrl_Ceiling", new Vector3(102.2f, y0 + 15.5f, 63f), new Vector3(141.8f, y0 + 16.6f, 113f), rock, root, false);
            Block("Ctrl_LedgeBody", new Vector3(103.6f, y0 - 0.5f, 64.2f), new Vector3(111.8f, V.LedgeY, 71.4f), m.stoneMid, root);
            // the walls: grown ribs and roots (Elarim foundations above, torn by the creatures' tunnels)
            { var v = new List<Vector3>(); var t = new List<int>();
              for (float z = 66f; z < 111f; z += 5.5f) foreach (var x in new[] { 103.5f, 140.5f }) Tube(v, t, new[] { new Vector3(x, y0, z), new Vector3(x + (x < 120 ? 1.5f : -1.5f), y0 + 8f, z + 0.6f), new Vector3(x + (x < 120 ? 4f : -4f), y0 + 15.4f, z + 1f) }, 0.7f, 0.35f, 8);
              for (float x = 106f; x < 140f; x += 6f) foreach (var z in new[] { 64.3f, 111.7f }) Tube(v, t, new[] { new Vector3(x, y0, z), new Vector3(x + 0.5f, y0 + 8f, z + (z < 90 ? 1.4f : -1.4f)), new Vector3(x + 1f, y0 + 15.4f, z + (z < 90 ? 4f : -4f)) }, 0.6f, 0.3f, 8);
              MeshObject("Ctrl_Ribs", SaveMesh(SmoothFinish(v, t), "Venus_ControlRibs"), rootMat, root); }
            // the creature troughs (dark grooves) and their burrows (dark holes at each end)
            for (int i = 0; i < 3; i++)
            {
                var a = V.Trough[i, 0]; var b = V.Trough[i, 1]; var mid = (a + b) * 0.5f; var dir = (b - a).normalized; float len = (b - a).magnitude;
                var v = new List<Vector3>(); var t = new List<int>(); AddBox(v, t, mid + Vector3.up * 0.02f, new Vector3(Mathf.Abs(dir.x) > 0.5f ? len : 1.9f, 0.03f, Mathf.Abs(dir.x) > 0.5f ? 1.9f : len));
                MeshObject($"Trough_{i}", SaveMesh(Finish(v, t), $"Venus_Trough{i}"), trough, root, false);
                foreach (var e in new[] { a - dir * 1.2f, b + dir * 1.2f })
                    MeshObject($"Burrow_{i}_{e.x:0}_{e.z:0}", SaveMesh(Lathe(new[] { new Vector2(0f, 0.035f), new Vector2(1.3f, 0.035f), new Vector2(1.6f, 0.15f), new Vector2(1.9f, 0f) }, 0f, 360f, 20, null, e), $"Venus_Burrow{i}_{e.x:0}_{e.z:0}"), trough, root, false);
            }
            // the dais + machinery: rings of pipes, stalks, cables to each device
            MeshObject("Dais", SaveMesh(Lathe(new[] { new Vector2(0f, V.DaisHeight), new Vector2(V.DaisRadius - 0.6f, V.DaisHeight), new Vector2(V.DaisRadius, V.DaisHeight - 0.5f), new Vector2(V.DaisRadius + 0.3f, 0f), new Vector2(0f, 0f) }, 0f, 360f, 40, null, V.DaisAt), "Venus_Dais"), machine, root);
            { var v = new List<Vector3>(); var t = new List<int>();
              for (int k = 0; k < 3; k++) { var ring = new List<Vector3>(); float r = V.DaisRadius + 0.4f + k * 0.25f, y = 0.6f + k * 1.2f; for (int i = 0; i < 40; i++) { float a = i / 40f * Mathf.PI * 2f; ring.Add(V.DaisAt + new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r)); } Sweep(v, t, ring, 0.14f, true, 6); }
              for (int i = 0; i < 9; i++) { float a = i / 9f * Mathf.PI * 2f + 0.3f; var b = V.DaisAt + new Vector3(Mathf.Cos(a) * (V.DaisRadius + 1.4f), 0f, Mathf.Sin(a) * (V.DaisRadius + 1.4f));
                  if (Mathf.Cos(a) < -0.6f) continue;                                    // the front stays open (his view down over the chamber)
                  Tube(v, t, new[] { b, b + Vector3.up * 3f, V.DaisAt + new Vector3(Mathf.Cos(a) * 2.4f, V.DaisHeight + 2.8f, Mathf.Sin(a) * 2.4f) }, 0.28f, 0.12f, 7); }
              for (int i = 0; i < 3; i++) { var p = V.DeviceAt[i]; var path = new List<Vector3> { p + Vector3.up * 0.15f, Vector3.Lerp(p, V.DaisAt, 0.35f) + Vector3.up * 0.15f + Vector3.forward * 1.2f, Vector3.Lerp(p, V.DaisAt, 0.75f) + Vector3.up * 0.15f, V.DaisAt + (p - V.DaisAt).normalized * (V.DaisRadius + 0.3f) + Vector3.up * 0.4f };
                  if (i == 1) path.Insert(1, new Vector3(p.x + 1f, V.ControlY + 0.15f, 72f)); Sweep(v, t, path, 0.13f, false, 6); }
              MeshObject("Machinery", SaveMesh(SmoothFinish(v, t), "Venus_Machinery"), machine, root); }
            // underlighting (grayscale): soft white lights low on the dais rim, shining up at him
            var under = new List<Light>();
            foreach (var a in new[] { 150f, 180f, 210f })
            {
                var l = new GameObject($"Underlight_{a:0}").AddComponent<Light>(); l.transform.SetParent(root, false);
                l.transform.position = V.DaisAt + new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * (V.DaisRadius - 0.3f), V.DaisHeight + 0.15f, Mathf.Sin(a * Mathf.Deg2Rad) * (V.DaisRadius - 0.3f));
                l.type = LightType.Point; l.range = 9f; l.intensity = 4f; l.color = Color.white; l.shadows = LightShadows.None; under.Add(l);
            }
            foreach (var (x, z) in new[] { (106f, 66f), (106f, 110f), (139f, 66f), (139f, 110f), (121f, 64.8f), (121f, 111.2f) }) Lamp($"Ctrl_Bulb_{x:0}_{z:0}", new Vector3(x, y0 + 4.2f, z), 14f, 1.1f, glowSoft, root, new Vector3(0.35f, 0.45f, 0.35f));
            Lamp("Ctrl_EntryBulb", new Vector3(104.5f, y0 + 4.4f, 89f), 10f, 1.0f, glowSoft, root, new Vector3(0.3f, 0.4f, 0.3f));

            // the three control devices
            var devs = new List<Seven.ControlDevice>(); Transform crystal = null;
            for (int i = 0; i < 3; i++)
            {
                var p = V.DeviceAt[i]; var go = new GameObject("Device_" + V.DeviceIds[i]); go.transform.SetParent(root, false); go.transform.position = p;
                MeshObject("Pylon", SaveMesh(Lathe(new[] { new Vector2(0f, 2.7f), new Vector2(0.3f, 2.7f), new Vector2(0.45f, 1.2f), new Vector2(0.7f, 0f), new Vector2(0f, 0f) }, 0f, 360f, 16, null, Vector3.zero), "Venus_DevicePylon"), machine, go.transform).transform.localPosition = Vector3.zero;
                var cv = new List<Vector3>(); var ct = new List<int>(); Lump(cv, ct, new Vector3(0f, 3.0f, 0f), new Vector3(0.2f, 0.26f, 0.2f), 0.25f, 30 + i);
                var core = MeshObject("Core", SaveMesh(SmoothFinish(cv, ct), $"Venus_DeviceCore{i}"), Dissolve($"Venus_device_core{i}", Gray(0.95f), 0.6f, false), go.transform, false);
                var ring = MeshObject("Ring", SaveMesh(Lathe(new[] { new Vector2(0.92f, 0f), new Vector2(1f, 0.04f), new Vector2(1.08f, 0f), new Vector2(1f, -0.04f) }, 0f, 360f, 32), $"Venus_DeviceRing{i}"), Dissolve($"Venus_device_ring{i}", Gray(0.95f), 0.5f, false), go.transform, false);
                ring.transform.localPosition = new Vector3(0f, 3.05f, 0f);
                var lt = new GameObject("Lamp").AddComponent<Light>(); lt.transform.SetParent(go.transform, false); lt.transform.localPosition = new Vector3(0f, 3.2f, 0f); lt.type = LightType.Point; lt.range = 7f; lt.color = Color.white; lt.shadows = LightShadows.None;
                var dev = go.AddComponent<Seven.ControlDevice>(); dev.Id = V.DeviceIds[i]; dev.Glow = core.GetComponent<Renderer>(); dev.Ring = ring.transform; dev.Lamp = lt; dev.Phase = i * 1.7f;
                if (i == 2) { crystal = core.transform; core.name = "Crystal"; }                     // dev_crystal: telekinesis pulls its crystal out
                devs.Add(dev);
            }
            // the containment barrier: grown stems sunk in the floor round the dais; the console in front of it
            var barrier = new GameObject("Barrier").transform; barrier.SetParent(root, false); barrier.position = new Vector3(V.DaisAt.x, -7.5f, V.DaisAt.z);
            Renderer barrierGlow;
            { var v = new List<Vector3>(); var t = new List<int>();
              for (int i = 0; i < 16; i++) { float a = i / 16f * Mathf.PI * 2f; var b = new Vector3(Mathf.Cos(a) * V.BarrierRadius, 0f, Mathf.Sin(a) * V.BarrierRadius);
                  Tube(v, t, new[] { b, b + Vector3.up * 4f + new Vector3(-Mathf.Cos(a), 0f, -Mathf.Sin(a)) * 0.6f, b * 0.55f + Vector3.up * 7.2f }, 0.32f, 0.12f, 7); }
              MeshObject("Stems", SaveMesh(SmoothFinish(v, t), "Venus_BarrierStems"), m.stone, barrier);
              var gm = Dissolve("Venus_barrier_glow", Gray(0.95f), 0.5f, false); gm.EnableKeyword("_EMISSION"); gm.SetColor("_EmissionColor", Color.black); EditorUtility.SetDirty(gm);
              var ring = new List<Vector3>(); for (int i = 0; i < 48; i++) { float a = i / 48f * Mathf.PI * 2f; ring.Add(new Vector3(Mathf.Cos(a) * (V.BarrierRadius - 0.6f), 4.2f, Mathf.Sin(a) * (V.BarrierRadius - 0.6f))); }
              var gv = new List<Vector3>(); var gt = new List<int>(); Sweep(gv, gt, ring, 0.08f, true, 6);
              barrierGlow = MeshObject("Glow", SaveMesh(SmoothFinish(gv, gt), "Venus_BarrierGlow"), gm, barrier, false).GetComponent<Renderer>(); }
            var console = new GameObject("Console").transform; console.SetParent(root, false); console.position = V.ConsoleAt;
            Renderer consoleGlow;
            { var v = new List<Vector3>(); var t = new List<int>(); Tube(v, t, new[] { Vector3.zero, new Vector3(0.1f, 0.6f, 0f), new Vector3(0.25f, 1.05f, 0f) }, 0.35f, 0.22f, 9);
              MeshObject("Stem", SaveMesh(SmoothFinish(v, t), "Venus_ConsoleStem"), m.stone, console);
              var gm = Dissolve("Venus_console_glow", Gray(0.9f), 0.5f, false); gm.EnableKeyword("_EMISSION"); gm.SetColor("_EmissionColor", Color.white * 0.15f); EditorUtility.SetDirty(gm);
              var pv = new List<Vector3>(); var pt = new List<int>(); AddBox(pv, pt, new Vector3(0.3f, 1.12f, 0f), new Vector3(0.55f, 0.06f, 0.7f));
              consoleGlow = MeshObject("Plate", SaveMesh(Finish(pv, pt), "Venus_ConsolePlate"), gm, console, false).GetComponent<Renderer>(); }

            // Wormmaster above his machinery
            Seven.WormmasterRig wm = null;
            var wmModel = SetupWormmaster();
            if (wmModel != null)
            {
                var go = new GameObject("Wormmaster"); go.transform.SetPositionAndRotation(V.WormmasterAt, Quaternion.LookRotation(Vector3.left));
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(wmModel); inst.name = "WormmasterModel"; inst.transform.SetParent(go.transform, false);
                var mat = TexturedMat("Venus_wormmaster", CharRoot + "/wormmaster/Textures/wormmaster_v1_basecolor.jpg", 0.55f, 0.15f);
                mat.SetFloat("_Rim", 0.45f); EditorUtility.SetDirty(mat);
                foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>()) { smr.sharedMaterial = mat; smr.updateWhenOffscreen = true; }
                wm = go.AddComponent<Seven.WormmasterRig>(); wm.Dlg = dlg;
                var tl = new GameObject("TipLight").AddComponent<Light>(); tl.transform.SetParent(go.transform, false); tl.transform.localPosition = new Vector3(0f, 3.8f, 0f); tl.type = LightType.Point; tl.range = 3.5f; tl.intensity = 0.3f; tl.color = Color.white; tl.shadows = LightShadows.None;
                wm.TipLight = tl;
            }
            else Log("WARNING: Wormmaster model missing (run Tools/Blender/cleanup_r11.py wormmaster) - the encounter plays without him visible");

            // the chamber creatures: two per trough, each trough driven by one device
            var worms = new List<Seven.WormCreature>();
            string[] groupOf = { "dev_north", "dev_ledge", "dev_crystal" };
            for (int i = 0; i < 3; i++)
                for (int k = 0; k < 2; k++)
                {
                    var a = V.Trough[i, 0]; var b = V.Trough[i, 1];
                    var go = new GameObject($"ChamberCreature_{i}_{k}"); go.transform.SetParent(root, false);
                    go.transform.position = Vector3.Lerp(a, b, k == 0 ? 0.2f : 0.75f); go.transform.rotation = Quaternion.LookRotation((k == 0 ? b - a : a - b).normalized);
                    if (worm != null)
                    {
                        var wmi = (GameObject)PrefabUtility.InstantiatePrefab(worm); wmi.name = "WormModel"; wmi.transform.SetParent(go.transform, false); wmi.transform.localScale = Vector3.one * 0.42f;
                        var an = wmi.GetComponent<Animator>() ?? wmi.AddComponent<Animator>(); an.runtimeAnimatorController = wormCtl; an.applyRootMotion = false;
                        var wmat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Venus_worm.mat"); if (wmat != null) foreach (var r in wmi.GetComponentsInChildren<Renderer>()) r.sharedMaterial = wmat;
                    }
                    var w = go.AddComponent<Seven.WormCreature>();
                    w.Route = new[] { a, b }; w.PingPong = true; w.Telegraph = true; w.Group = groupOf[i];
                    w.Speed = 3.0f; w.Depth = 1.4f; w.Surface = 0.35f; w.Period = 6.5f + k * 0.9f;
                    var dust = new GameObject("Dust").AddComponent<ParticleSystem>(); dust.transform.SetParent(go.transform, false);
                    var main = dust.main; main.startColor = new Color(0.6f, 0.6f, 0.6f, 0.5f); main.startSize = 1.3f; main.startLifetime = 1.6f; main.startSpeed = 0.9f;
                    var emi = dust.emission; emi.rateOverTime = 18f;
                    dust.GetComponent<ParticleSystemRenderer>().sharedMaterial = AdditiveMat("Venus_dust", 0.35f);
                    w.Dust = dust;
                    worms.Add(w);
                }
            // obstacles in the chamber (the dais inside the barrier ring, the pylons, the console)
            var wg = Object.FindObjectOfType<Seven.WorldGround>();
            if (wg != null)
            {
                var pads = wg.ZoneObstacles.ToList();
                pads.Add(new Vector3(V.DaisAt.x, V.DaisAt.z, V.BarrierRadius + 0.5f));
                for (int i = 0; i < 3; i++) pads.Add(new Vector3(V.DeviceAt[i].x, V.DeviceAt[i].z, 0.75f));
                pads.Add(new Vector3(V.ConsoleAt.x, V.ConsoleAt.z, 0.45f));
                wg.ZoneObstacles = pads.ToArray();
            }

            // ---- the changed world (section 9): a repaired channel with water; workers back on the paths
            var dry = new GameObject("Channel_Damaged"); dry.transform.SetParent(world, false);
            var wet = new GameObject("Channel_Repaired"); wet.transform.SetParent(world, false);
            { // a grown stone channel along the west verge of the road, broken in the middle (dry); repaired, water runs in it
                var pts = new List<Vector3>(); for (float sP = 60f; sP < 95f; sP += 2.5f) { var c = VenusTerrain.AtS(sP); var dd = VenusTerrain.DirAtS(sP); var q = c - new Vector3(dd.z, 0f, -dd.x) * 6.6f; q.y = VenusTerrain.Height(q.x, q.z) + 0.25f; pts.Add(q); }
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    bool gap = i == pts.Count / 2 || i == pts.Count / 2 + 1;
                    var a = pts[i]; var b = pts[i + 1]; var mid = (a + b) * 0.5f; float yaw = Mathf.Atan2(b.x - a.x, b.z - a.z) * Mathf.Rad2Deg; float len = Vector3.Distance(a, b);
                    var v = new List<Vector3>(); var t = new List<int>();
                    AddBox(v, t, mid + Vector3.up * -0.05f, new Vector3(0.9f, 0.1f, len), yaw); AddBox(v, t, mid + Quaternion.Euler(0, yaw, 0) * new Vector3(-0.42f, 0.15f, 0f), new Vector3(0.12f, 0.35f, len), yaw); AddBox(v, t, mid + Quaternion.Euler(0, yaw, 0) * new Vector3(0.42f, 0.15f, 0f), new Vector3(0.12f, 0.35f, len), yaw);
                    var seg = MeshObject($"Channel_{i}", SaveMesh(Finish(v, t), $"Venus_Channel{i}"), m.stoneMid, gap ? wet.transform : world, true);
                    if (gap) { var rv = new List<Vector3>(); var rt = new List<int>(); for (int k = 0; k < 4; k++) Lump(rv, rt, mid + new Vector3((float)rnd.NextDouble() - 0.5f, 0.05f, (float)rnd.NextDouble() - 0.5f), new Vector3(0.45f, 0.25f, 0.4f), 0.5f, rnd.Next(1000)); MeshObject($"Channel_Broken_{i}", SaveMesh(SmoothFinish(rv, rt), $"Venus_ChannelBroken{i}"), m.stoneDark, dry.transform); }
                    var wv = new List<Vector3>(); var wt = new List<int>(); AddBox(wv, wt, mid + Vector3.up * 0.12f, new Vector3(0.7f, 0.02f, len), yaw);
                    MeshObject($"Water_{i}", SaveMesh(Finish(wv, wt), $"Venus_ChannelWater{i}"), m.water, wet.transform, false);
                }
                // dry: the water spills from the break (a wet stain) - stays visible after too (damage remains)
            }
            wet.SetActive(false);
            var returning = new List<Seven.ElarimNpc>();
            if (makeElarim != null)
                foreach (var (sP, lat, clip) in new[] { (42f, -5.5f, "carry"), (150f, 5.5f, "lift"), (172f, -5.5f, "carry") })
                {
                    var c = VenusTerrain.AtS(sP); var dd = VenusTerrain.DirAtS(sP); var q = c + new Vector3(dd.z, 0f, -dd.x) * lat; q.y = VenusTerrain.Height(q.x, q.z);
                    var n = makeElarim($"Elarim_Worker_Return_{sP:0}", q, -dd, clip); n.gameObject.SetActive(false); returning.Add(n);
                }

            // ---- the small cube: the copy of Venus's record the guardian gives her (C10)
            var rec = BuildVenusCube(world, m, em); rec.name = "RecordCopy"; rec.localScale = Vector3.one * 0.24f; rec.position = V.CubeAt + new Vector3(1.5f, 0f, -2f);
            var rl = rec.Find("CubeLight"); if (rl != null) Object.DestroyImmediate(rl.gameObject);
            rec.gameObject.SetActive(false);

            d.RootGate = gate.gameObject; d.Devices = devs.ToArray(); d.Crystal = crystal; d.Barrier = barrier; d.BarrierGlow = barrierGlow; d.Console = console; d.ConsoleGlow = consoleGlow;
            d.Wormmaster = wm; d.ChamberWorms = worms.ToArray(); d.UnderLights = under.ToArray(); d.RepairedChannel = wet; d.DamagedChannel = dry; d.ReturnWorkers = returning.ToArray(); d.RecordCopy = rec;
            d.Vision2 = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Video/vision_02_venus.mp4");
            if (d.Vision2 == null) Log("WARNING: Assets/Video/vision_02_venus.mp4 missing - C09 uses the still fallback");
            AddProbe(new Vector3(121f, -24f, 88f), new Vector3(40f, 16f, 50f), Vector3.zero);
            Log($"Venus part 2 built: way down (root gate, pit, tunnel, root chamber, deep tunnel), control chamber (dais, {devs.Count} devices, {worms.Count} creatures, barrier, console), Wormmaster {(wm != null ? "rigged" : "MISSING")}, repaired channel, {returning.Count} returning workers, record copy");
        }

        /// <summary>Wormmaster (Tools/Blender/cleanup_r11.py wormmaster): imported Humanoid with the explicit Mixamo map (his
        /// skeleton uses Mixamo names); his motion is procedural (WormmasterRig) - the extra arm bones and HeadTip ride along.</summary>
        static GameObject SetupWormmaster()
        {
            var path = CopyExport("wormmaster", "wormmaster_v1_rigged.fbx"); if (path == null) return null;
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            imp.animationType = ModelImporterAnimationType.Human; imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = false; imp.materialImportMode = ModelImporterMaterialImportMode.None; imp.SaveAndReimport();
            ApplyMixamoHumanMap(imp, path);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            Log($"{path}: Wormmaster avatar human={avatar?.isHuman} valid={avatar?.isValid}");
            if (avatar == null || !avatar.isValid)
            {
                imp.animationType = ModelImporterAnimationType.Generic; imp.SaveAndReimport();
                Log("Wormmaster: Humanoid avatar not valid - imported Generic (the procedural rig does not need it)");
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
    }
}
