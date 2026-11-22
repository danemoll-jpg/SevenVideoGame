using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF26 build checks (Dan: Cattalus is unplayable, a white-out).
    /// Per scene (from QF23SceneChecks):
    ///  - NORMALS (QF26 1): every mesh in the scene - no vertex used by a real (non-degenerate) triangle may have a zero or NaN
    ///    normal. A zero normal lights as NaN (normalize(0)), the tonemapper turns NaN into a black speck and bloom's clamp turns
    ///    it into a 65472-bright source: the 0.18.0 white-out (the Cattalus kit's two-sided quads shared their vertices between
    ///    the two windings, so the normals summed to zero). FAILS.
    /// Once per build (from QF24GlobalChecks):
    ///  - NORMALS (QF26 1) over the baked window / exterior views (Resources/Outside/*.prefab). FAILS.
    ///  - IMAGE GATE (QF26 2): every capture path renders in HDR like the screen and goes through ImageGate (no 8-bit capture
    ///    target - URP renders the whole frame in a target texture's format, so an ARGB32 capture hides HDR blow-outs and NaN).
    ///    FAILS on a capture runner that bypasses it.
    /// Reports: Docs/Previews/qf26/*.txt.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        static string QF26Dir { get { var d = Path.Combine(RepoRoot, "Docs", "Previews", "qf26"); Directory.CreateDirectory(d); return d; } }

        static int BadNormals(Mesh m, out int verts)
        {
            verts = 0; if (m == null) return 0;
            var n = m.normals; var v = m.vertices; if (n == null || n.Length == 0) return 0;
            var used = new bool[v.Length];
            for (int s = 0; s < m.subMeshCount; s++)
            {
                if (m.GetTopology(s) != MeshTopology.Triangles) continue;
                var t = m.GetTriangles(s);
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    var a = v[t[i]]; var b = v[t[i + 1]]; var c = v[t[i + 2]];
                    if (Vector3.Cross(b - a, c - a).sqrMagnitude < 1e-14f) continue;     // degenerate: never rasterised
                    used[t[i]] = used[t[i + 1]] = used[t[i + 2]] = true;
                }
            }
            int bad = 0;
            for (int i = 0; i < n.Length; i++)
            {
                if (!used[i]) continue; verts++;
                float l = n[i].sqrMagnitude;
                if (float.IsNaN(l) || float.IsInfinity(l) || l < 0.25f) bad++;
            }
            return bad;
        }

        static void NormalsOver(IEnumerable<MeshFilter> filters, IEnumerable<SkinnedMeshRenderer> skins, string where, string file)
        {
            var sb = new StringBuilder(); int meshes = 0, bad = 0, badMeshes = 0, verts = 0;
            var seen = new HashSet<Mesh>();
            void One(Mesh m, string path)
            {
                if (m == null || !seen.Add(m)) return;
                meshes++; int b = BadNormals(m, out int vv); verts += vv;
                if (b > 0) { bad += b; badMeshes++; sb.AppendLine($"  {path}: mesh '{m.name}' {b} of {vv} vertices with a zero / NaN normal"); }
            }
            foreach (var f in filters) One(f.sharedMesh, f.transform.name);
            foreach (var s in skins) One(s.sharedMesh, s.transform.name);
            Report26(file, $"NORMALS (QF26 1) {where}: {meshes} meshes, {verts} vertices in use - zero / NaN normals {bad} in {badMeshes} meshes (must be 0){(bad > 0 ? " FAIL" : "")}", sb, bad > 0);
        }

        static void QF26SceneChecks(string scene)
        {
            NormalsOver(Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                        Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None), scene, $"normals_{scene}.txt");
        }

        public static void QF26GlobalChecks()
        {
            // the baked window / exterior views (the CAT_01 exterior is one of them)
            var filters = new List<MeshFilter>(); var skins = new List<SkinnedMeshRenderer>();
            foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Outside" }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)); if (go == null) continue;
                filters.AddRange(go.GetComponentsInChildren<MeshFilter>(true)); skins.AddRange(go.GetComponentsInChildren<SkinnedMeshRenderer>(true));
            }
            NormalsOver(filters, skins, "the window / exterior views (Resources/Outside)", "normals_outside.txt");

            // every capture path renders HDR and goes through the gate
            var sb = new StringBuilder(); int bad = 0;
            var dir = Path.Combine(Application.dataPath, "Scripts", "Runtime");
            foreach (var f in new[] { "ChapterCapture.cs", "CaptureRun.cs", "SaucerCaptureRun.cs", "RatwingCaptureRun.cs" })
            {
                var p = Path.Combine(dir, f); if (!File.Exists(p)) { sb.AppendLine($"  {f}: missing"); bad++; continue; }
                var src = File.ReadAllText(p);
                bool ldr = src.Contains("RenderTextureFormat.ARGB32"), gated = src.Contains("ImageGate.Save") && src.Contains("ImageGate.NewRT");
                if (ldr || !gated) { bad++; sb.AppendLine($"  {f}: {(ldr ? "an 8-bit (ARGB32) capture target " : "")}{(!gated ? "bypasses ImageGate" : "")}"); }
                else sb.AppendLine($"  {f}: HDR target, every shot gated");
            }
            Report26("image_gate_paths.txt", $"IMAGE GATE (QF26 2): capture paths HDR + gated - {4 - bad} of 4{(bad > 0 ? " FAIL" : "")}", sb, bad > 0);
        }

        static void Report26(string file, string head, StringBuilder sb, bool fail)
        {
            File.WriteAllText(Path.Combine(QF26Dir, file), head + "\n" + sb);
            Log(head);
            if (fail) BuildFailures.Add(head);
        }
    }
}
