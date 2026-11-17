using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 5: Kyle's art gallery TV on the saucer. Copies Art/Gallery (gallery.json + every included image) to
    /// Assets/StreamingAssets/Gallery (the game loads them at runtime), and builds the TV nook: a wide rounded screen
    /// with a softly glowing bezel in the back nook (335 deg), a low curved console under it and a curved sofa out on the
    /// deck facing it.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const int TvNook = 1;                         // index into NookAngles (335 deg)
        const float TvR = 9.30f, TvY = 1.32f;         // screen centre: radius from the deck centre, height
        static readonly Vector2 TvSize = new Vector2(2.8f, 1.4f);
        const float TvBezel = 0.14f;
        const string GalleryUiMatPath = MatDir + "/Gallery_ui.mat";

        static string StreamingGalleryDir => Path.Combine(ProjectRoot, "Assets", "StreamingAssets", Seven.GalleryTV.Folder);

        /// <summary>Art/Gallery -> Assets/StreamingAssets/Gallery: gallery.json + the included images (rebuilt every build).</summary>
        static void CopyGallery()
        {
            var src = Path.Combine(RepoRoot, "Art", "Gallery");
            var json = Path.Combine(src, Seven.GalleryTV.JsonName);
            var dst = StreamingGalleryDir;
            if (Directory.Exists(dst)) Directory.Delete(dst, true);
            Directory.CreateDirectory(dst);
            if (!File.Exists(json)) { Debug.LogWarning("[FightDemoBuilder] no Art/Gallery/gallery.json: the TV will be empty"); return; }
            var g = JsonUtility.FromJson<Seven.GalleryFile>(File.ReadAllText(json));
            File.Copy(json, Path.Combine(dst, Seven.GalleryTV.JsonName), true);
            int on = 0, off = 0, missing = 0;
            foreach (var e in g.images ?? new Seven.GalleryEntry[0])
            {
                if (!e.include) { off++; continue; }
                var f = Path.Combine(src, e.file);
                if (!File.Exists(f)) { missing++; Debug.LogWarning("[FightDemoBuilder] gallery image missing: " + f); continue; }
                File.Copy(f, Path.Combine(dst, e.file), true);
                on++;
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Log($"gallery copied to StreamingAssets/Gallery: {on} included, {off} off, {missing} missing");
        }

        struct TvRefs { public Transform tv; public Renderer screen; public Light light; public Vector3 sofaRoot, sofaFacing; }

        /// <summary>Rounded rectangle (half sizes, corner radius) as a closed loop of points with outward normals.</summary>
        static List<(Vector2 p, Vector2 n)> RoundRectLoop(float hw, float hh, float r, int seg = 8)
        {
            var o = new List<(Vector2, Vector2)>();
            var corners = new[] { (new Vector2(hw - r, hh - r), 0f), (new Vector2(-hw + r, hh - r), 90f), (new Vector2(-hw + r, -hh + r), 180f), (new Vector2(hw - r, -hh + r), 270f) };
            foreach (var (c, a0) in corners)
                for (int i = 0; i <= seg; i++)
                {
                    float a = (a0 + 90f * i / seg) * Mathf.Deg2Rad;
                    var n = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    o.Add((c + n * r, n));
                }
            return o;
        }

        /// <summary>Sweep a (outward offset, z) profile around a loop: one smooth ring surface (local space).</summary>
        static void LoopSweep(List<Vector3> v, List<int> t, List<(Vector2 p, Vector2 n)> loop, IList<Vector2> prof)
        {
            int n = loop.Count, start = v.Count;
            foreach (var pr in prof)
                foreach (var (p, nn) in loop)
                {
                    var q = p + nn * pr.x;
                    v.Add(new Vector3(q.x, q.y, pr.y));
                }
            for (int j = 0; j < prof.Count - 1; j++)
                for (int i = 0; i < n; i++)
                {
                    int a = start + j * n + i, b = start + j * n + (i + 1) % n, c = a + n, d = b + n;
                    t.Add(a); t.Add(c); t.Add(b);
                    t.Add(b); t.Add(c); t.Add(d);
                }
        }

        static TvRefs BuildGalleryNook(Transform parent, ShipMats m, List<Vector3> obstacles)
        {
            float c = NookAngles[TvNook];
            var root = new GameObject("GalleryNook").transform;
            root.SetParent(parent, false);
            var bezelMat = Dissolve("TV_bezel", Gray(0.86f), 0.9f);

            // The TV: local +Z points into the wall, the screen faces -Z (toward the deck).
            var tv = new GameObject("GalleryTV").transform;
            tv.SetParent(root, false);
            tv.position = Polar(TvR, c, TvY);
            tv.rotation = Quaternion.LookRotation(Polar(1f, c, 0f));
            float hw = TvSize.x * 0.5f, hh = TvSize.y * 0.5f, b = TvBezel;
            var loop = RoundRectLoop(hw, hh, 0.04f);
            var bv = new List<Vector3>(); var bt = new List<int>();
            var prof = new[]
            {
                new Vector2(0f, 0.004f), new Vector2(0f, -0.03f), new Vector2(0.02f, -0.05f), new Vector2(b - 0.05f, -0.056f),
                new Vector2(b - 0.015f, -0.046f), new Vector2(b, -0.016f), new Vector2(b + 0.008f, 0.03f), new Vector2(b - 0.01f, 0.09f),
                new Vector2(b - 0.07f, 0.13f),
            };
            LoopSweep(bv, bt, loop, prof);
            // Back cap.
            int ring = bv.Count - loop.Count, centre = bv.Count;
            bv.Add(new Vector3(0f, 0f, 0.13f));
            for (int i = 0; i < loop.Count; i++) { bt.Add(centre); bt.Add(ring + i); bt.Add(ring + (i + 1) % loop.Count); }
            var bezel = MeshObject("TV_Bezel", SaveMesh(SmoothFinish(bv, bt), "Saucer_TV_Bezel"), bezelMat, tv);
            // Soft glowing line around the bezel's rounded outer edge.
            var gv = new List<Vector3>(); var gt = new List<int>();
            Sweep(gv, gt, loop.Select(l => { var q = l.p + l.n * (b + 0.004f); return new Vector3(q.x, q.y, -0.014f); }).ToList(), 0.011f, true, 6);
            MeshObject("TV_Glow", SaveMesh(SmoothFinish(gv, gt), "Saucer_TV_Glow"), m.cove, tv, false);
            // Screen surface.
            var sm = new Mesh { name = "Saucer_TV_Screen" };
            sm.SetVertices(new List<Vector3> { new Vector3(-hw, -hh, 0f), new Vector3(-hw, hh, 0f), new Vector3(hw, hh, 0f), new Vector3(hw, -hh, 0f) });
            sm.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) });
            sm.SetNormals(Enumerable.Repeat(Vector3.back, 4).ToList());
            sm.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            var screenMatPath = MatDir + "/Gallery_screen.mat";
            var smat = AssetDatabase.LoadAssetAtPath<Material>(screenMatPath);
            var sh = Shader.Find("Seven/GalleryScreen");
            if (sh == null) throw new System.Exception("Shader Seven/GalleryScreen not found (Assets/Gallery/SevenGalleryScreen.shader)");
            if (smat == null) { smat = new Material(sh); AssetDatabase.CreateAsset(smat, screenMatPath); }
            smat.shader = sh;
            EditorUtility.SetDirty(smat);
            var screen = MeshObject("TV_Screen", SaveMesh(sm, "Saucer_TV_Screen"), smat, tv, false);
            screen.GetComponent<MeshRenderer>().receiveShadows = false;
            // Light thrown by the picture.
            var lgo = new GameObject("TV_ScreenLight");
            lgo.transform.SetParent(tv, false);
            lgo.transform.localPosition = new Vector3(0f, -0.15f, -1.0f);
            var light = lgo.AddComponent<Light>();
            light.type = LightType.Point; light.range = 4.2f; light.intensity = 1.0f; light.color = Color.white; light.shadows = LightShadows.None;

            // Low curved console under the screen.
            var cv = new List<Vector3>(); var ct = new List<int>();
            AddRing(cv, ct, Vector3.zero, 8.98f, 9.55f, 0.40f, c - 9f, c + 9f, 18);
            Obj("TV_Console", Finish(cv, ct), m.sofaBase, root);
            var lv = new List<Vector3>(); var lt = new List<int>();
            Sweep(lv, lt, Enumerable.Range(0, 19).Select(i => Polar(8.97f, Mathf.Lerp(c - 8.6f, c + 8.6f, i / 18f), 0.03f)).ToList(), 0.012f, false, 6);
            Obj("TV_ConsoleGlow", SmoothFinish(lv, lt), m.cove, root, false);

            // Curved sofa out on the deck facing the screen (arc around the screen's floor point).
            var o = Polar(TvR, c, 0f);
            float f = c + 180f, a0 = f - 15f, a1 = f + 15f;
            var sv = new List<Vector3>(); var st = new List<int>();
            AddRing(sv, st, o, 3.55f, 4.35f, 0.36f, a0, a1, 30);
            Obj("TV_Sofa_Base", Finish(sv, st), m.sofaBase, root);
            var qv = new List<Vector3>(); var qt = new List<int>();
            AddRing(qv, qt, o + Vector3.up * 0.36f, 3.6f, 4.25f, 0.13f, a0 + 0.8f, a1 - 0.8f, 30);
            AddRing(qv, qt, o + Vector3.up * 0.36f, 4.0f, 4.33f, 0.58f, a0 + 0.8f, a1 - 0.8f, 30);
            Obj("TV_Sofa_Cushions", Finish(qv, qt), m.fabric, root);
            var av = new List<Vector3>(); var at = new List<int>();
            foreach (var a in new[] { a0, a1 }) Ellipsoid(av, at, o + Polar(3.95f, a, 0.42f), new Vector3(0.46f, 0.22f, 0.46f), 10, 16);
            Obj("TV_Sofa_Arms", SmoothFinish(av, at), m.sofaBase, root);
            const int nObs = 5;
            for (int i = 0; i <= nObs; i++) { var p = o + Polar(3.95f, Mathf.Lerp(a0, a1, (float)i / nObs), 0f); obstacles.Add(new Vector3(p.x, p.z, 0.45f)); }
            Log($"gallery TV at {tv.position} ({TvSize.x}x{TvSize.y} m), sofa ends at {o + Polar(3.95f, a0, 0f)} / {o + Polar(3.95f, a1, 0f)}");
            // Round 8: where 7 sits while browsing (middle of the sofa, facing the screen).
            var sofaRoot = o + Polar(3.8f, f, 0f);
            var sofaFacing = (o - sofaRoot); sofaFacing.y = 0f;
            return new TvRefs { tv = tv, screen = screen.GetComponent<Renderer>(), light = light, sofaRoot = sofaRoot, sofaFacing = sofaFacing.normalized };
        }

        static Material GalleryUiMaterial()
        {
            var sh = Shader.Find("Seven/GalleryUI");
            if (sh == null) throw new System.Exception("Shader Seven/GalleryUI not found (Assets/Gallery/SevenGalleryUI.shader)");
            var m = AssetDatabase.LoadAssetAtPath<Material>(GalleryUiMatPath);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, GalleryUiMatPath); }
            m.shader = sh;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
