using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 13 (item 2): the ship's console on the saucer's main deck, between the galley opening and 7's starting spot.
    /// An all-chrome retro-futuristic crescent desk (rounded polished chrome, curved glass screens with a soft white glow,
    /// chunky switches and dials, a glowing lip line), concave toward the deck: the standing station with the menu screen
    /// on the left, the navigation display (star map) on the right with the pilot's chair in front of it (VEN_C11 / VEN_C13).
    /// Everything dissolves with the reveal (Seven/DissolveLit, _Dissolvable = 1, under "Ship"); the runtime side is
    /// <see cref="Seven.ShipConsole"/>. Local frame of the console root: +Z toward the deck (the user side); the crescent is
    /// a lathe around the point C = (0, 0, ConsoleRc) on the user side, angle 270 deg = straight back toward the wall.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const float ConsoleAngle = 296f, ConsoleDeckR = 5.85f;   // Round 13 check: was 236 - it stood across her walk from the start to the galley (playtest soft-lock);   // deck polar position of the console root (sofa 229-311 starts at r 6.55)
        const float ConsoleRc = 2.2f;                             // crescent radius centre, on the user side
        const float ConsoleA0 = 230f, ConsoleA1 = 310f, ConsoleMid = 270f;
        const float MenuA0 = 240f, MenuA1 = 262f, NavA0 = 280f, NavA1 = 304f;
        const float StandAngle = 251f, StandR = 1.55f;            // where 7 stands (her feet), from C
        const float PilotAngle = 292f, PilotR = 1.15f;            // the pilot's chair base, from C

        struct ConsoleRefs
        {
            public Transform root, stand, pilotChair, pilotSeat, pilotLap, display, menuScreen, handL, handR;
            public Light light;
            public Vector2 menuSize, displaySize;
        }

        static Vector3 ConsoleC => new Vector3(0f, 0f, ConsoleRc);
        /// <summary>Local point at radius r / angle a (deg) around the crescent centre, height y.</summary>
        static Vector3 CP(float r, float a, float y) => ConsoleC + Polar(r, a, y);

        static ConsoleRefs BuildShipConsole(List<Vector3> obstacles, List<Light> lights)
        {
            var refs = new ConsoleRefs();
            var ship = GameObject.Find("Ship");
            var root = new GameObject("ShipConsole").transform;
            if (ship != null) root.SetParent(ship.transform, false);
            root.position = Polar(ConsoleDeckR, ConsoleAngle, 0f);
            root.rotation = Quaternion.LookRotation(-Polar(1f, ConsoleAngle, 0f));      // +Z toward the deck centre
            refs.root = root;

            var chrome = Dissolve("Console_chrome", Gray(0.82f), 0.88f, true, 0.55f);    // Round 13 check: fully metallic read black (the interior probe is dark) - silver now
            var satin = Dissolve("Console_satin", Gray(0.66f), 0.7f, true, 0.45f);
            var trim = Dissolve("Console_trim", Gray(0.1f), 0.75f);
            var glass = Dissolve("Console_glass", Gray(0.035f), 0.97f);
            glass.SetColor("_EmissionColor", Color.white * 0.22f);                            // the soft glow of the screens
            var glow = Dissolve("Console_glow", Gray(1f), 0.5f);
            glow.SetColor("_EmissionColor", Color.white * 2.2f);
            var cushion = Dissolve("Console_cushion", Gray(0.19f), 0.12f);
            foreach (var m in new[] { chrome, satin, trim, glass, glow, cushion }) UnityEditor.EditorUtility.SetDirty(m);

            GameObject Part(string name, Mesh mesh, Material mat, Transform parent, bool shadows = true)
                => MeshObject(name, SaveMesh(mesh, "Console_" + name), mat, parent, shadows);

            // ---- the crescent desk: standing station (high top) left, navigation desk (low, open for the knees) right
            var deskProf = Resample(new[]
            {
                new Vector2(1.96f, 0.0f), new Vector2(1.94f, 0.07f), new Vector2(1.93f, 0.72f), new Vector2(1.905f, 0.86f),
                new Vector2(1.915f, 0.92f), new Vector2(1.95f, 0.955f), new Vector2(2.01f, 0.968f), new Vector2(2.32f, 1.02f),
                new Vector2(2.40f, 1.005f), new Vector2(2.445f, 0.94f), new Vector2(2.455f, 0.0f),
            }, 0.025f);
            Part("DeskStation", Lathe(deskProf, ConsoleA0 + 4f, ConsoleMid, 40, null, ConsoleC), chrome, root);
            var navProf = Resample(new[]
            {
                new Vector2(2.30f, 0.0f), new Vector2(2.29f, 0.54f), new Vector2(2.05f, 0.60f), new Vector2(1.94f, 0.64f),
                new Vector2(1.915f, 0.70f), new Vector2(1.94f, 0.75f), new Vector2(2.01f, 0.772f), new Vector2(2.34f, 0.82f),
                new Vector2(2.42f, 0.805f), new Vector2(2.455f, 0.74f), new Vector2(2.465f, 0.0f),
            }, 0.025f);
            Part("DeskNav", Lathe(navProf, ConsoleMid, ConsoleA1 - 4f, 40, null, ConsoleC), chrome, root);
            // kick plate (dark) under both desks
            Part("Kick", Lathe(new[] { new Vector2(1.97f, 0.0f), new Vector2(1.955f, 0.07f) }, ConsoleA0 + 4f, ConsoleMid, 40, null, ConsoleC), trim, root);
            // rounded chrome pods: the two ends and the divider between the station and the navigation desk
            var pv = new List<Vector3>(); var pt = new List<int>();
            Ellipsoid(pv, pt, CP(2.2f, ConsoleA0 + 4f, 0.52f), new Vector3(0.3f, 0.54f, 0.3f), 18, 28);
            Ellipsoid(pv, pt, CP(2.2f, ConsoleA1 - 4f, 0.44f), new Vector3(0.3f, 0.46f, 0.3f), 18, 28);
            Ellipsoid(pv, pt, CP(2.2f, ConsoleMid, 0.56f), new Vector3(0.24f, 0.58f, 0.27f), 18, 28);
            Part("Pods", SmoothFinish(pv, pt), chrome, root);
            var cv = new List<Vector3>(); var ct = new List<int>();                                // pod crowns: a glowing ring on top
            foreach (var (a, y) in new[] { (ConsoleA0 + 4f, 1.05f), (ConsoleA1 - 4f, 0.89f), (ConsoleMid, 1.13f) })
                Sweep(cv, ct, Enumerable.Range(0, 25).Select(i => CP(2.2f, a, y) + Polar(0.11f, i * 15f, 0f)).ToList(), 0.012f, true, 6);
            Part("PodRings", SmoothFinish(cv, ct), glow, root, false);
            // the glowing lip line along the front of both desks (Kyle's soft light)
            var lv = new List<Vector3>(); var lt = new List<int>();
            Sweep(lv, lt, Enumerable.Range(0, 31).Select(i => CP(1.912f, Mathf.Lerp(ConsoleA0 + 9f, ConsoleMid - 4f, i / 30f), 0.885f)).ToList(), 0.008f, false, 6);
            Sweep(lv, lt, Enumerable.Range(0, 31).Select(i => CP(1.918f, Mathf.Lerp(ConsoleMid + 4f, ConsoleA1 - 9f, i / 30f), 0.675f)).ToList(), 0.008f, false, 6);
            Part("LipGlow", SmoothFinish(lv, lt), glow, root, false);

            // ---- curved glass screens with chrome bezels and housings
            void BuildScreen(string name, float a0, float a1, float y0, float y1, float r0, float r1)
            {
                var prof = Resample(new[] { new Vector2(r0, y0), new Vector2(r1, y1) }, 0.04f);
                Part(name + "_Glass", Lathe(prof, a0, a1, 24, null, ConsoleC), glass, root, false);
                Part(name + "_Housing", Lathe(Resample(new[] { new Vector2(r0 + 0.11f, y0 - 0.03f), new Vector2(r0 + 0.2f, y0 + 0.06f),
                    new Vector2(r1 + 0.13f, y1 - 0.05f), new Vector2(r1 + 0.03f, y1 + 0.04f) }, 0.04f), a0 - 1.5f, a1 + 1.5f, 24, null, ConsoleC), satin, root);
                var bv = new List<Vector3>(); var bt = new List<int>();
                var ring = new List<Vector3>();
                for (int i = 0; i <= 16; i++) ring.Add(CP(r0 - 0.008f, Mathf.Lerp(a0, a1, i / 16f), y0));
                for (int i = 1; i <= 6; i++) { float k = i / 6f; ring.Add(CP(Mathf.Lerp(r0, r1, k) - 0.008f, a1, Mathf.Lerp(y0, y1, k))); }
                for (int i = 15; i >= 0; i--) ring.Add(CP(r1 - 0.008f, Mathf.Lerp(a0, a1, i / 16f), y1));
                for (int i = 5; i >= 1; i--) { float k = i / 6f; ring.Add(CP(Mathf.Lerp(r0, r1, k) - 0.008f, a0, Mathf.Lerp(y0, y1, k))); }
                Sweep(bv, bt, ring, 0.024f, true, 10);
                Part(name + "_Bezel", SmoothFinish(bv, bt), chrome, root);
                // neck down to the desk top
                var nv = new List<Vector3>(); var nt = new List<int>();
                Ellipsoid(nv, nt, CP(r0 + 0.12f, (a0 + a1) * 0.5f, y0 - 0.02f), new Vector3(0.16f, 0.06f, 0.1f), 10, 18);
                Part(name + "_Neck", SmoothFinish(nv, nt), chrome, root);
            }
            BuildScreen("MenuScreen", MenuA0, MenuA1, 1.05f, 1.68f, 2.30f, 2.40f);
            BuildScreen("NavDisplay", NavA0, NavA1, 0.86f, 1.62f, 2.30f, 2.42f);

            // ---- chunky switches and dials (chrome toggles, dark dials with a glowing index), glowing buttons
            var sv = new List<Vector3>(); var st = new List<int>();
            var dv = new List<Vector3>(); var dt = new List<int>();
            var gv = new List<Vector3>(); var gt = new List<int>();
            float TopY(float r, bool nav) => nav ? Mathf.Lerp(0.772f, 0.82f, Mathf.InverseLerp(2.01f, 2.34f, r)) : Mathf.Lerp(0.968f, 1.02f, Mathf.InverseLerp(2.01f, 2.32f, r));
            foreach (var a in new[] { 233.5f, 236.5f, 239.5f })                                     // a row of toggles left of the station
            {
                var b = CP(2.14f, a, TopY(2.14f, false));
                AddBox(sv, st, b + Vector3.up * 0.012f, new Vector3(0.05f, 0.024f, 0.07f), -a);
                Sweep(sv, st, new[] { b + Vector3.up * 0.02f, b + Vector3.up * 0.075f + Polar(0.025f, a + 180f, 0f) }, 0.007f, false, 8);
                Ellipsoid(sv, st, b + Vector3.up * 0.078f + Polar(0.026f, a + 180f, 0f), Vector3.one * 0.013f, 6, 10);
            }
            foreach (var a in new[] { 258.5f, 262.5f })                                              // two big dials right of the hands
            {
                var b = CP(2.15f, a, TopY(2.15f, false));
                Ellipsoid(dv, dt, b + Vector3.up * 0.02f, new Vector3(0.042f, 0.022f, 0.042f), 8, 18);
                Ellipsoid(sv, st, b + Vector3.up * 0.005f, new Vector3(0.058f, 0.008f, 0.058f), 6, 18);
                Sweep(gv, gt, new[] { b + Vector3.up * 0.043f, b + Vector3.up * 0.043f + Polar(0.03f, a + 160f, 0f) }, 0.004f, false, 6);
            }
            for (int k = 0; k < 6; k++)                                                              // glowing buttons along the station top
                Ellipsoid(gv, gt, CP(2.24f, 243f + k * 2.6f, TopY(2.24f, false) + 0.006f), new Vector3(0.016f, 0.008f, 0.016f), 5, 10);
            for (int k = 0; k < 5; k++)                                                              // and on the navigation desk
                Ellipsoid(gv, gt, CP(2.22f, 283f + k * 4.2f, TopY(2.22f, true) + 0.006f), new Vector3(0.016f, 0.008f, 0.016f), 5, 10);
            {
                var b = CP(2.1f, 299f, TopY(2.1f, true));                                             // the navigation lever (chunky, chrome ball)
                AddBox(sv, st, b + Vector3.up * 0.015f, new Vector3(0.09f, 0.03f, 0.12f), -299f);
                Sweep(sv, st, new[] { b + Vector3.up * 0.03f, b + Vector3.up * 0.13f + Polar(0.03f, 299f + 180f, 0f) }, 0.009f, false, 8);
                Ellipsoid(sv, st, b + Vector3.up * 0.135f + Polar(0.032f, 299f + 180f, 0f), Vector3.one * 0.024f, 8, 14);
            }
            Part("Switches", SmoothFinish(sv, st), chrome, root);
            Part("Dials", SmoothFinish(dv, dt), trim, root);
            Part("Buttons", SmoothFinish(gv, gt), glow, root, false);

            // ---- the pilot's chair (VEN_C11 / VEN_C13): chrome swivel pedestal, padded seat, rounded chrome back shell
            var chair = new GameObject("PilotChair").transform; chair.SetParent(root, false);
            chair.localPosition = CP(PilotR, PilotAngle, 0f);
            chair.localRotation = Quaternion.LookRotation(Polar(1f, PilotAngle, 0f));               // facing the navigation display
            Part("PilotPedestal", Lathe(Resample(new[] { new Vector2(0f, 0f), new Vector2(0.34f, 0f), new Vector2(0.32f, 0.03f), new Vector2(0.09f, 0.09f),
                new Vector2(0.05f, 0.18f), new Vector2(0.045f, 0.36f), new Vector2(0.13f, 0.40f), new Vector2(0f, 0.41f) }, 0.03f), 0f, 360f, 40), chrome, chair);
            var seatV = new List<Vector3>(); var seatT = new List<int>();
            Ellipsoid(seatV, seatT, new Vector3(0f, 0.445f, 0.02f), new Vector3(0.29f, 0.075f, 0.28f), 12, 28);
            Part("PilotSeatCushion", SmoothFinish(seatV, seatT), cushion, chair);
            var shV = new List<Vector3>(); var shT = new List<int>();
            Ellipsoid(shV, shT, new Vector3(0f, 0.72f, -0.06f), new Vector3(0.36f, 0.46f, 0.3f), 22, 32, d => d.z > -0.08f && d.y > -0.62f);
            Part("PilotShell", SmoothFinish(shV, shT), chrome, chair);
            var liV = new List<Vector3>(); var liT = new List<int>();
            Ellipsoid(liV, liT, new Vector3(0f, 0.72f, -0.06f), new Vector3(0.335f, 0.43f, 0.275f), 22, 32, d => d.z > -0.08f && d.y > -0.62f);
            Part("PilotLining", SmoothFinish(liV, liT), cushion, chair);
            var arV = new List<Vector3>(); var arT = new List<int>();
            foreach (var sx in new[] { -1f, 1f })
                Sweep(arV, arT, new[] { new Vector3(0.31f * sx, 0.5f, -0.2f), new Vector3(0.32f * sx, 0.6f, -0.05f), new Vector3(0.31f * sx, 0.6f, 0.12f), new Vector3(0.29f * sx, 0.55f, 0.2f) }, 0.03f, false, 10);
            Part("PilotArms", SmoothFinish(arV, arT), chrome, chair);
            refs.pilotChair = chair;
            refs.pilotSeat = Marker("PilotSeat", chair, new Vector3(0f, 0.60f, -0.02f), Vector3.forward);
            refs.pilotLap = Marker("PilotLap", chair, new Vector3(0f, 0.70f, 0.22f), Vector3.forward);

            // ---- markers for the runtime
            float tiltMenu = Mathf.Atan2(0.10f, 0.63f) * Mathf.Rad2Deg, tiltNav = Mathf.Atan2(0.12f, 0.76f) * Mathf.Rad2Deg;
            Transform ScreenMarker(string name, float a0, float a1, float y0, float y1, float r0, float r1, float tilt)
            {
                float am = (a0 + a1) * 0.5f, half = (a1 - a0) * 0.5f;
                float rMid = (r0 + r1) * 0.5f * Mathf.Cos(half * Mathf.Deg2Rad) - 0.014f;          // on the chord, just in front of the glass
                var t = new GameObject(name).transform; t.SetParent(root, false);
                t.localPosition = CP(rMid, am, (y0 + y1) * 0.5f);
                var outward = -Polar(1f, am, 0f);                                                     // toward the user
                var n = (outward * Mathf.Cos(tilt * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(tilt * Mathf.Deg2Rad)).normalized;
                t.localRotation = Quaternion.LookRotation(n, Vector3.up);
                return t;
            }
            refs.menuScreen = ScreenMarker("MenuScreenCentre", MenuA0, MenuA1, 1.05f, 1.68f, 2.30f, 2.40f, tiltMenu);
            refs.display = ScreenMarker("NavDisplayCentre", NavA0, NavA1, 0.86f, 1.62f, 2.30f, 2.42f, tiltNav);
            float Chord(float a0, float a1, float r) => 2f * r * Mathf.Sin((a1 - a0) * 0.5f * Mathf.Deg2Rad);
            refs.menuSize = new Vector2(Chord(MenuA0, MenuA1, 2.30f) - 0.03f, Mathf.Sqrt(0.10f * 0.10f + 0.63f * 0.63f) - 0.03f);
            refs.displaySize = new Vector2(Chord(NavA0, NavA1, 2.30f) - 0.03f, Mathf.Sqrt(0.12f * 0.12f + 0.76f * 0.76f) - 0.03f);
            refs.stand = Marker("StandSpot", root, CP(StandR, StandAngle, 0f), Polar(1f, StandAngle, 0f));
            refs.handL = Marker("HandRestL", root, CP(2.0f, StandAngle - 4.3f, 0.985f), Polar(1f, StandAngle, 0f));
            refs.handR = Marker("HandRestR", root, CP(2.0f, StandAngle + 4.3f, 0.985f), Polar(1f, StandAngle, 0f));

            // soft white light from the screens (dims with the reveal like the other interior lights)
            var l = new GameObject("ConsoleScreenLight").AddComponent<Light>();
            l.transform.SetParent(root, false); l.transform.localPosition = CP(1.95f, ConsoleMid, 1.35f);
            l.type = LightType.Point; l.range = 2.6f; l.intensity = 0.55f; l.color = Color.white; l.shadows = LightShadows.None;
            lights.Add(l); refs.light = l;

            // ---- walkable deck: obstacle circles along the crescent + the pilot's chair (deck x, z, radius)
            int nObs = 0;
            for (float a = ConsoleA0 + 3f; a <= ConsoleA1 - 3f + 0.01f; a += 7.4f)
            {
                var w = root.TransformPoint(CP(2.2f, a, 0f));
                obstacles.Add(new Vector3(w.x, w.z, 0.34f)); nObs++;
            }
            var cw = chair.position; obstacles.Add(new Vector3(cw.x, cw.z, 0.38f)); nObs++;
            Log($"ship console: root {root.position} (deck r {ConsoleDeckR} at {ConsoleAngle} deg), stand spot {refs.stand.position}, pilot seat {refs.pilotSeat.position}, " +
                $"menu screen {refs.menuSize.x:0.00} x {refs.menuSize.y:0.00} m, navigation display {refs.displaySize.x:0.00} x {refs.displaySize.y:0.00} m, {nObs} deck obstacles");
            return refs;
        }

        static Transform Marker(string name, Transform parent, Vector3 localPos, Vector3 localForward)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localRotation = Quaternion.LookRotation(localForward, Vector3.up);
            return t;
        }

        /// <summary>Round 13: the ShipConsole component (runtime menu, star map, the pilot's chair markers for the lead's cutscenes).</summary>
        static void WireShipConsole(ConsoleRefs c, Seven.SaucerDirector dir, Seven.ChapterUI ui)
        {
            var sc = c.root.gameObject.AddComponent<Seven.ShipConsole>();
            sc.Director = dir; sc.UI = ui;
            sc.StandSpot = c.stand; sc.PilotChair = c.pilotChair; sc.PilotSeat = c.pilotSeat; sc.PilotLap = c.pilotLap;
            sc.Display = c.display; sc.MenuScreen = c.menuScreen; sc.HandLeft = c.handL; sc.HandRight = c.handR;
            sc.MenuSize = c.menuSize; sc.DisplaySize = c.displaySize; sc.ScreenLight = c.light;
            PreparePortraits();
        }

        /// <summary>The field guide portraits (Tools/Blender/render_portraits.py -> Resources/Console/Portraits/*.png) import
        /// uncompressed: block compression (565) would tint the strict grays (monochrome check: R = G = B).</summary>
        static void PreparePortraits()
        {
            const string dir = "Assets/Resources/Console/Portraits";
            if (!UnityEditor.AssetDatabase.IsValidFolder(dir)) { Log("console portraits: folder missing (" + dir + ") - the field guide shows plain cards"); return; }
            int n = 0;
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { dir }))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                if (imp == null) continue;
                bool dirty = imp.textureCompression != UnityEditor.TextureImporterCompression.Uncompressed || imp.mipmapEnabled || imp.npotScale != UnityEditor.TextureImporterNPOTScale.None
                             || imp.textureType != UnityEditor.TextureImporterType.Default || imp.maxTextureSize < 1024;
                if (dirty)
                {
                    imp.textureType = UnityEditor.TextureImporterType.Default;
                    imp.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
                    imp.mipmapEnabled = false; imp.npotScale = UnityEditor.TextureImporterNPOTScale.None; imp.maxTextureSize = 1024;
                    imp.alphaSource = UnityEditor.TextureImporterAlphaSource.None; imp.wrapMode = TextureWrapMode.Clamp;
                    imp.SaveAndReimport();
                }
                n++;
            }
            Log($"console portraits: {n} field guide pictures (uncompressed, grayscale)");
        }
    }
}
