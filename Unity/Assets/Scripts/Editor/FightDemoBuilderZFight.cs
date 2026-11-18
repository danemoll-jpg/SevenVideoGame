using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF9 (Dan: "door frames flicker between black and grey"): the level is built from overlapping stone blocks, and
    /// 207 pairs of them had faces in exactly the same plane, facing the same way (a wall block and the floor block under
    /// it, the passage lintel and the ledge block above it, two floor slabs meeting...). Where the two blocks have
    /// different materials (dark vs pale stone) the depth buffer cannot decide between them, so the surface switches black
    /// / grey as the camera moves (measured in motion by -flickertest). This pass runs on every generated Chapter 1 scene:
    /// it finds every such pair among the axis-aligned cube blocks and moves the later block's face outward by 4 mm (the
    /// block grows, so no gap can open), repeating until there are none. Report: Docs/Previews/qf9/zfight_check.txt + the
    /// build log (must end "0 left").
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const float ZfTol = 0.0015f, ZfStep = 0.004f;

        static void ZFightFix(string sceneName)
        {
            var blocks = new List<Transform>();
            var all = new List<MeshFilter>();
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) all.AddRange(go.GetComponentsInChildren<MeshFilter>(true));
            foreach (var mf in all)
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || mf.sharedMesh == null || !mf.sharedMesh.name.StartsWith("Cube")) continue;
                if (Quaternion.Angle(mf.transform.rotation, Quaternion.identity) > 0.01f) continue;
                if (mr.sharedMaterial != null && mr.sharedMaterial.renderQueue > 2500) continue;
                blocks.Add(mf.transform);
            }
            Bounds B(Transform t) => new Bounds(t.position, Vector3.Scale(t.lossyScale, Vector3.one));
            int first = -1, moved = 0, passes = 0, left = 0;
            var sb = new StringBuilder();
            sb.AppendLine($"QF9 z-fighting check ({sceneName}): coplanar, same-facing, overlapping faces of the cube blocks");
            for (; passes < 12; passes++)
            {
                int found = 0;
                for (int i = 0; i < blocks.Count; i++)
                    for (int j = i + 1; j < blocks.Count; j++)
                    {
                        var A = B(blocks[i]); var C = B(blocks[j]);
                        for (int ax = 0; ax < 3; ax++)
                            foreach (var side in new[] { -1, 1 })
                            {
                                float fa = side < 0 ? A.min[ax] : A.max[ax], fc = side < 0 ? C.min[ax] : C.max[ax];
                                if (Mathf.Abs(fa - fc) > ZfTol) continue;
                                int u = (ax + 1) % 3, v = (ax + 2) % 3;
                                float ou = Mathf.Min(A.max[u], C.max[u]) - Mathf.Max(A.min[u], C.min[u]);
                                float ov = Mathf.Min(A.max[v], C.max[v]) - Mathf.Max(A.min[v], C.min[v]);
                                if (ou <= 0.005f || ov <= 0.005f) continue;
                                found++;
                                if (passes == 0 && found <= 300) sb.AppendLine($"  {blocks[i].name} & {blocks[j].name}: {(side < 0 ? "-" : "+")}{"xyz"[ax]} at {fa:0.000} ({ou:0.00} x {ov:0.00} m)");
                                // grow block j's face outward by a few mm (j > i: deterministic)
                                var t = blocks[j]; var s = t.localScale; var p = t.position;
                                var d = Vector3.zero; d[ax] = ZfStep;
                                var parentScale = t.parent != null ? t.parent.lossyScale : Vector3.one;
                                var sl = s; sl[ax] += ZfStep / Mathf.Max(1e-6f, parentScale[ax]);
                                t.localScale = sl; t.position = p + d * 0.5f * side;
                                C = B(t); moved++;
                            }
                    }
                if (first < 0) first = found;
                left = found;
                if (found == 0) break;
            }
            sb.AppendLine($"found {first} at first, {moved} face moves (4 mm each) over {passes + 1} passes, {left} left (must be 0)");
            var dir = Path.Combine(ProjectRoot, "..", "Docs", "Previews", "qf9");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, $"zfight_check_{sceneName}.txt"), sb.ToString());
            Log($"QF9 z-fighting check {sceneName}: {first} coplanar block faces found, {moved} moved, {left} left (must be 0)");
        }
    }
}
