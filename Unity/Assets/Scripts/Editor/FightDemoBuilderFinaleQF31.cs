using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using F = Seven.FinaleLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF31 (Dan's review of 1.0.0-rc2) - the finale's builder additions:
    /// (8) the fight space: the archive's table as its own piece over a hole in the floor (it lowers flush into a shallow light well once
    ///     the release begins; a ring of light round the well's rim), the wall panels his rig tears out, and the mining cavern beyond
    ///     (his worked-out excavation: a wide flat floor, rock walls and roof, spoil heaps, parked drill carts, light masts);
    /// (9) small looping copies of the seven visions for the chamber's screens;
    /// (A) the heaven clips (Art/Video/color/heaven_01..06.mp4, colour, dropped in by name), Dan's memory edit
    ///     (Art/Video/panther_memories.mp4, made grayscale) and the raw memory clips Code exported (Art/Video/memory_clips/*.mp4).
    /// </summary>
    public static partial class FightDemoBuilder
    {
        /// <summary>The archive's table (the QF30 lathe: a near-vertical carved base inside PlatformR + 0.3) as its own transform, a lining
        /// round the hole it sits in, and the well's rim light (a thin glowing ring, dark until the release).</summary>
        static Transform BuildArchivePlatform(Vector3 c0, Material stone, Material soft, Transform parent, out Renderer rim)
        {
            var root = new GameObject("ArchivePlatform").transform; root.SetParent(parent, false); root.position = c0;
            var k = new AK(); var (v, t) = k.Of(stone);
            // the table, now reaching down into the hole (its foot hidden below the floor while it is raised)
            Append(v, t, Lathe(new[] { new Vector2(0f, F.PlatformH), new Vector2(F.PlatformR, F.PlatformH), new Vector2(F.PlatformR + 0.12f, F.PlatformH - 0.08f), new Vector2(F.PlatformR + 0.12f, F.PlatformH - 0.2f), new Vector2(F.PlatformR + 0.04f, 0.26f), new Vector2(F.PlatformR + 0.24f, 0.1f), new Vector2(F.PlatformR + 0.28f, 0f), new Vector2(F.PlatformR + 0.28f, -1.1f), new Vector2(0f, -1.1f) }, 0f, 360f, 40, null, Vector3.zero));
            foreach (var g in k.Save("ArchivePlatform", root, false)) g.transform.localPosition = Vector3.zero;
            // the hole's lining (the well's wall below the floor) and its floor far down
            var lk = new AK(); var (lv, lt) = lk.Of(stone);
            Append(lv, lt, Lathe(new[] { new Vector2(F.WellR - 0.01f, -2.0f), new Vector2(F.WellR - 0.01f, -0.02f) }, 0f, 360f, 40, null, c0));
            lk.Save("ArchiveWellLining", parent, false);
            // the rim light: a narrow ring just inside the floor's edge
            var rv = new List<Vector3>(); var rt = new List<int>(); Append(rv, rt, Lathe(new[] { new Vector2(F.WellR + 0.02f, 0.012f), new Vector2(F.WellR + 0.16f, 0.012f) }, 0f, 360f, 48, null, Vector3.zero));
            var ring = MeshObject("ArchiveWellRim", SaveMesh(SmoothFinish(rv, rt), "Fin_ArchiveWellRim"), new Material(soft) { name = "Fin_well_rim" }, parent, false); ring.transform.position = c0;
            rim = ring.GetComponent<Renderer>();
            Log("QF31 (8): the archive's table is its own piece over a hole in the floor (it lowers flush into a shallow light well), the well's rim light built");
            return root;
        }

        /// <summary>The panels his rig tears out (either side of the old access + its lintel): one object each, dark rock-faced stone.</summary>
        static Transform[] BuildBreach(List<(Vector3 p, float a)> at, Material stone, Transform parent)
        {
            var list = new List<Transform>();
            int n = 0;
            foreach (var (p, a) in at)
            {
                var go = new GameObject("BreachPanel" + n++).transform; go.SetParent(parent, false); go.position = p;
                var k = new AK(); var (v, t) = k.Of(stone);
                if (a > 999f) MBox(v, t, Vector3.zero, new Vector3(8.6f, 3.2f, 0.9f), 180f, 0.03f);   // the lintel (wider: it spans the torn opening)
                else MBox(v, t, Vector3.zero, new Vector3(2.9f, F.ChamberH, 0.8f), a, 0.03f);
                foreach (var g in k.Save("BreachPanel" + (n - 1), go, false)) g.transform.localPosition = Vector3.zero;
                list.Add(go);
            }
            Log($"QF31 (8): {list.Count} breach pieces (the wall panels either side of the mining access + its lintel) - his rig tears them out in FIN_08");
            return list.ToArray();
        }

        /// <summary>The mining cavern beyond the chamber's south wall: his worked-out excavation. A wide flat floor (the "Cavern" ground
        /// zone), rough rock walls and roof, spoil heaps along the walls, parked drill carts, light masts with work lamps.</summary>
        static void BuildMiningCavern(MoonMats m, Material dust, Transform world)
        {
            var root = new GameObject("MiningCavern").transform; root.SetParent(world, false);
            var k = new AK();
            float x0 = F.CavernX0, x1 = F.CavernX1, z0 = F.CavernZ0, z1 = F.CavernZ1, y = F.Y, h = F.CavernH;
            { var (v, t) = k.Of(m.rockDark);
              // walls: rough blocks in a ring (each a little rotated / stepped so it reads as cut rock, not a box)
              var rnd = new System.Random(311);
              for (float x = x0 - 1f; x < x1 + 1f; x += 3.4f) { MBox(v, t, new Vector3(x, y + h * 0.5f, z0 - 1.2f - (float)rnd.NextDouble() * 0.8f), new Vector3(3.8f, h + 1f, 2.4f), (float)rnd.NextDouble() * 6f - 3f, 0.12f); }
              for (float z = z0; z < z1 + 3f; z += 3.4f) foreach (var sx in new[] { x0 - 1.2f, x1 + 1.2f }) MBox(v, t, new Vector3(sx + ((float)rnd.NextDouble() - 0.5f) * 0.8f, y + h * 0.5f, z), new Vector3(2.4f, h + 1f, 3.8f), (float)rnd.NextDouble() * 6f - 3f, 0.12f);
              // the north face either side of the chamber (it meets the chamber's outer wall)
              MBox(v, t, new Vector3((x0 + 1.4f) * 0.5f - 0.6f, y + h * 0.5f, z1 + 0.6f), new Vector3(1.4f - x0 + 1f, h + 1f, 2.0f), 0f, 0.1f);
              MBox(v, t, new Vector3((21.6f + x1) * 0.5f + 0.6f, y + h * 0.5f, z1 + 0.6f), new Vector3(x1 - 21.6f + 1f, h + 1f, 2.0f), 0f, 0.1f);
              MBox(v, t, new Vector3(11.5f, y + 13.6f + (h - 13.6f) * 0.5f, z1 + 0.6f), new Vector3(24f, h - 13.6f + 0.6f, 2.0f), 0f, 0.1f);   // above the chamber's ceiling ring
              // the roof: slabs with a ragged underside
              for (float x = x0; x < x1; x += 6f) for (float z = z0; z < z1 + 4f; z += 6f) MBox(v, t, new Vector3(x + 3f, y + h + 0.6f + (float)rnd.NextDouble() * 0.9f, z + 3f), new Vector3(6.6f, 1.6f, 6.6f), (float)rnd.NextDouble() * 20f, 0.2f);
              // QF40 (8): the spoil is in ORE BINS (riveted steel, rock heaped inside), never loose dark blobs: broken angular chunks
              // fill each bin and a few have spilled at its foot
              foreach (var o in F.CavernProps.Take(6))
              {
                  var bc = new Vector3(o.x, y, o.y); float bw = Mathf.Clamp(o.z * 1.1f, 1.6f, 3.2f);
                  for (int i = 0; i < 14; i++) { float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = (float)rnd.NextDouble() * bw * 0.36f, s = 0.3f + (float)rnd.NextDouble() * 0.35f; MBox(v, t, bc + new Vector3(Mathf.Cos(a) * r, 1.05f + (1f - r / (bw * 0.36f)) * 0.35f, Mathf.Sin(a) * r), new Vector3(s * 1.3f, s, s), (float)rnd.NextDouble() * 90f, 0.18f); }
                  for (int i = 0; i < 3; i++) { float a = (float)rnd.NextDouble() * Mathf.PI * 2f, s = 0.22f + (float)rnd.NextDouble() * 0.2f; MBox(v, t, bc + new Vector3(Mathf.Cos(a) * bw * 0.75f, s * 0.5f, Mathf.Sin(a) * bw * 0.75f), new Vector3(s * 1.4f, s, s), (float)rnd.NextDouble() * 90f, 0.18f); }
              }
            }
            { var (bv, bt) = k.Of(m.metalDark);
              foreach (var o in F.CavernProps.Take(6))
              {
                  var bc = new Vector3(o.x, y, o.y); float bw = Mathf.Clamp(o.z * 1.1f, 1.6f, 3.2f);
                  MBox(bv, bt, bc + Vector3.up * 0.08f, new Vector3(bw, 0.16f, bw * 0.8f), 0f, 0f);                                                   // floor plate
                  foreach (var sz in new[] { -1f, 1f }) MBox(bv, bt, bc + new Vector3(0f, 0.65f, sz * bw * 0.4f), new Vector3(bw, 1.3f, 0.1f), 0f, 0f);   // the sides
                  foreach (var sx in new[] { -1f, 1f }) MBox(bv, bt, bc + new Vector3(sx * bw * 0.5f, 0.65f, 0f), new Vector3(0.1f, 1.3f, bw * 0.8f), 0f, 0f);
                  for (int r = 0; r < 2; r++) foreach (var sz in new[] { -1f, 1f }) MBox(bv, bt, bc + new Vector3(0f, 0.35f + r * 0.6f, sz * (bw * 0.4f + 0.06f)), new Vector3(bw + 0.06f, 0.07f, 0.04f), 0f, 0f);   // riveted bands
              }
            }
            { var (v, t) = k.Of(dust); var rnd = new System.Random(77); for (int i = 0; i < 46; i++) Ellipsoid(v, t, new Vector3(Mathf.Lerp(x0 + 2f, x1 - 2f, (float)rnd.NextDouble()), y, Mathf.Lerp(z0 + 2f, z1 - 1.5f, (float)rnd.NextDouble())), new Vector3(0.6f + (float)rnd.NextDouble() * 1.4f, 0.03f, 0.5f + (float)rnd.NextDouble() * 1.2f), 3, 12); }
            k.Save("CavernWall", root, false);
            // parked drill carts (a chassis, a boom, a spiral bit) and light masts (a pole, a lamp head, a real light)
            var ck = new AK();
            foreach (var o in F.CavernProps.Skip(6).Take(2))
            {
                var c = new Vector3(o.x, y, o.y); var (mv, mt) = ck.Of(m.metalDark);
                MBox(mv, mt, c + Vector3.up * 0.55f, new Vector3(1.6f, 0.9f, 2.4f), 0f, 0.02f);
                Sweep(mv, mt, new[] { c + new Vector3(0f, 1.0f, 0.6f), c + new Vector3(0f, 2.4f, 1.6f) }, 0.16f, false, 8);
                var (bv, bt) = ck.Of(m.metal); var tip = c + new Vector3(0f, 2.4f, 1.6f);
                var coil = new List<Vector3>(); for (int i = 0; i <= 24; i++) { float u = i / 24f, a = u * Mathf.PI * 7f; coil.Add(tip + new Vector3(Mathf.Cos(a) * 0.22f * (1f - u), Mathf.Sin(a) * 0.22f * (1f - u), u * 1.1f)); }
                Sweep(bv, bt, coil, 0.05f, false, 5);
            }
            foreach (var o in F.CavernProps.Skip(8))
            {
                var c = new Vector3(o.x, y, o.y); var (mv, mt) = ck.Of(m.metalDark);
                Sweep(mv, mt, new[] { c, c + Vector3.up * 6.2f }, 0.09f, false, 6);
                var (gv, gt) = ck.Of(m.glow); Ellipsoid(gv, gt, c + Vector3.up * 6.3f, new Vector3(0.32f, 0.18f, 0.32f), 5, 10);
                PointLight("CavernWorkLamp", c + Vector3.up * 6.0f, 22f, 2.6f, root);
            }
            ck.Save("CavernProps", root, false);
            PointLight("CavernFill", F.CavernC + Vector3.up * 8f, 38f, 3.2f, root); PointLight("CavernFill2", F.CavernC + new Vector3(0f, 6f, 9f), 26f, 2.2f, root);
            Log($"QF31 (8): the mining cavern beyond the south wall - {x1 - x0:0} x {z1 - z0:0} m of open floor, rock walls + roof, {F.CavernProps.Length} props (spoil heaps, drill carts, light masts)");
        }

        /// <summary>QF31 (9): the seven visions' screen loops as frame sequences (6 s from 2 s in, 10 fps, 384 px, grayscale) in
        /// Resources/Finale/ScreenLoops/vis_0N/ - seven VideoPlayers at once stalled the game (a capture run crawled at ~2 fps: each player
        /// blocks for its frame); cycling textures costs nothing. Missing ffmpeg = the stills.</summary>
        static UnityEngine.Video.VideoClip[] VisionLoops()
        {
            var names = new[] { "vision_01_door", "vision_02_venus", "vision_03_europa", "vision_04_mars", "vision_05_asteroid", "vision_06_mothership", "vision_07_cattalus" };
            var ff = FindFfmpeg(); int made = 0, frames = 0;
            for (int i = 0; i < names.Length; i++)
            {
                var src = Path.Combine(RepoRoot, "Art", "Video", names[i] + ".mp4"); var dir = Path.Combine(ProjectRoot, $"Assets/Resources/Finale/ScreenLoops/vis_0{i + 1}");
                bool fresh = Directory.Exists(dir) && Directory.GetFiles(dir, "*.jpg").Length > 10 && File.Exists(src) && Directory.GetLastWriteTimeUtc(dir) >= File.GetLastWriteTimeUtc(src);
                if (File.Exists(src) && ff != null && !fresh)
                {
                    if (Directory.Exists(dir)) foreach (var f in Directory.GetFiles(dir)) File.Delete(f);
                    Directory.CreateDirectory(dir);
                    RunFfmpeg(ff, $"-y -loglevel error -ss 2 -i \"{src}\" -t 6 -an -vf \"fps=10,scale=384:-2,format=gray\" -q:v 4 \"{Path.Combine(dir, "f%03d.jpg")}\"");
                }
                if (Directory.Exists(dir)) { int n = Directory.GetFiles(dir, "*.jpg").Length; if (n > 0) { made++; frames += n; } }
            }
            AssetDatabase.Refresh();
            var old = Path.Combine(ProjectRoot, "Assets/Video/loops"); if (Directory.Exists(old)) { AssetDatabase.DeleteAsset("Assets/Video/loops"); }
            Log($"QF31 (9): the seven visions' screen loops - {made} of 7 as frame sequences ({frames} frames, 10 fps, 384 px, grayscale) in Resources/Finale/ScreenLoops");
            return new UnityEngine.Video.VideoClip[0];
        }
        static void RunFfmpeg(string ff, string args)
        {
            var p = new System.Diagnostics.Process { StartInfo = new System.Diagnostics.ProcessStartInfo(ff, args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true } };
            p.Start(); var err = p.StandardError.ReadToEnd(); p.WaitForExit(240000);
            if (p.ExitCode != 0) Log("ffmpeg failed: " + err);
        }

        /// <summary>QF31 (A7): Dan's heaven clips (Art/Video/color/heaven_01..06.mp4, colour) dropped in by name; any that exist play.</summary>
        static UnityEngine.Video.VideoClip[] HeavenClips()
        {
            // QF33: Dan's one edited heaven clip (heaven_color_game.mp4, colour, silent, 1920x1044) - made here from his .mov if only that is in
            var one = Path.Combine(RepoRoot, "Art", "Video", "color", "heaven_color_game.mp4");
            var mov = Path.Combine(RepoRoot, "Art", "Video", "color", "heaven_color_game.mov");
            if (!File.Exists(one) && File.Exists(mov)) { var ff0 = FindFfmpeg(); if (ff0 != null) RunFfmpeg(ff0, $"-y -loglevel error -i \"{mov}\" -an -vf \"scale=1920:-2,crop=1920:1044,format=yuv420p\" -c:v libx264 -preset medium -crf 18 \"{one}\""); }
            if (File.Exists(one))
            {
                File.Copy(one, Path.Combine(ProjectRoot, "Assets/Video/heaven_color_game.mp4"), true); AssetDatabase.ImportAsset("Assets/Video/heaven_color_game.mp4", ImportAssetOptions.ForceSynchronousImport);
                var c1 = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Video/heaven_color_game.mp4");
                if (c1 != null) { Log($"HEAVEN (QF33): Dan's edited heaven clip in ({c1.length:0.0} s, {c1.width}x{c1.height}, colour) - one clip, lines timed to its picture"); return new[] { c1 }; }
            }
            var list = new List<UnityEngine.Video.VideoClip>(); var missing = new List<string>();
            for (int i = 1; i <= 6; i++)
            {
                var f = $"heaven_0{i}.mp4"; var src = Path.Combine(RepoRoot, "Art", "Video", "color", f);
                if (!File.Exists(src)) { missing.Add(f); continue; }
                File.Copy(src, Path.Combine(ProjectRoot, "Assets/Video/" + f), true); AssetDatabase.ImportAsset("Assets/Video/" + f, ImportAssetOptions.ForceSynchronousImport);
                var c = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Video/" + f); if (c != null) list.Add(c);
            }
            Log($"HEAVEN (QF31 A7): {list.Count} of Dan's 6 heaven clips in{(missing.Count > 0 ? " - missing " + string.Join(", ", missing) + ": the stand-in (the colour master from 17.6 s) plays" : "")}");
            return list.Count == 6 ? list.ToArray() : new UnityEngine.Video.VideoClip[0];
        }
        /// <summary>QF31 (A3): Dan's own memory edit Art/Video/panther_memories.mp4 (made grayscale here), else null.</summary>
        static UnityEngine.Video.VideoClip MemoriesVideo()
        {
            var src = Path.Combine(RepoRoot, "Art", "Video", "panther_memories.mp4");
            // QF34 (2): without Dan's own edit, Code's flowing montage (Tools/Video/make_memory_montage.py: 7 and Panther together in every
            // shot, story order, soft dissolves) plays as one film
            if (!File.Exists(src)) src = Path.Combine(RepoRoot, "Art", "Video", "panther_memories_code.mp4");
            if (!File.Exists(src)) { Log("MEMORIES (QF31 A3): Art/Video/panther_memories.mp4 not in yet - the montage of Code's in-game clips plays"); return null; }
            Log($"MEMORIES (QF34 2): the montage {Path.GetFileName(src)}");
            var ff = FindFfmpeg(); var dst = Path.Combine(ProjectRoot, "Assets/Video/panther_memories_gray.mp4");
            if (ff != null) RunFfmpeg(ff, $"-y -loglevel error -i \"{src}\" -an -vf \"format=gray,format=yuv420p\" -c:v libx264 -preset veryfast -crf 20 \"{dst}\""); else File.Copy(src, dst, true);
            AssetDatabase.ImportAsset("Assets/Video/panther_memories_gray.mp4", ImportAssetOptions.ForceSynchronousImport);
            var c = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Video/panther_memories_gray.mp4");
            Log($"MEMORIES (QF31 A3): Dan's edit in ({(c != null ? c.length.ToString("0.0") : "?")} s, grayscale) - it plays over her");
            return c;
        }
        /// <summary>QF31 (A3): the raw memory clips Code exported from the game (Art/Video/memory_clips/*.mp4), in name order - the stand-in
        /// montage until Dan's edit is in.</summary>
        static UnityEngine.Video.VideoClip[] MemoryClipsIn()
        {
            var src = Path.Combine(RepoRoot, "Art", "Video", "memory_clips"); var list = new List<UnityEngine.Video.VideoClip>();
            if (Directory.Exists(src))
            {
                var dir = Path.Combine(ProjectRoot, "Assets/Video/memory_clips"); Directory.CreateDirectory(dir);
                foreach (var f in Directory.GetFiles(src, "*.mp4").OrderBy(x => x))
                {
                    var n = Path.GetFileName(f); File.Copy(f, Path.Combine(dir, n), true); AssetDatabase.ImportAsset("Assets/Video/memory_clips/" + n, ImportAssetOptions.ForceSynchronousImport);
                    var c = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Video/memory_clips/" + n); if (c != null) list.Add(c);
                }
            }
            Log($"MEMORIES (QF31 A3): {list.Count} of Code's raw in-game memory clips in ({string.Join(", ", list.Select(c => c.name))})");
            return list.ToArray();
        }
    }
}
