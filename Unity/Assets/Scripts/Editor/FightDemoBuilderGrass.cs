using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF10: soft grass for Venus's gardens, like Kyle's "Grassy rise in the fog" (Art/Reference/kyle_world_mood.jpg):
    /// tall, swaying blades in grayscale that catch the light toward the tip and fade into the fog. Cheap: the blades are
    /// baked into static chunk meshes (one per 24 m cell, so the camera culls whole chunks), drawn with Seven/Grass (wind in
    /// the vertex shader, no shadow casting, receives shadows). Never on the road surfaces: only on the patches the scene
    /// gives (terraces, verges, slopes, the grassy rises).
    /// </summary>
    public static partial class FightDemoBuilder
    {
        public class GrassPatch
        {
            public string Name; public Rect Area;            // x / z
            public Func<float, float, float> Height;           // surface height at (x, z); NaN = no grass there
            public float Density = 10f;                        // blades per m2
            public float MinH = 0.35f, MaxH = 0.85f;
        }

        static Material GrassMat()
        {
            var path = MatDir + "/Venus_grass.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find("Seven/Grass");
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh; m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Scatter blades over every patch, bake them into chunk meshes under the parent. Returns the blade count.</summary>
        static int BuildGrass(Transform parent, IList<GrassPatch> patches, int seed = 7)
        {
            var mat = GrassMat();
            var root = new GameObject("Grass").transform; root.SetParent(parent, false);
            var rnd = new System.Random(seed);
            const float Cell = 24f;
            var chunks = new Dictionary<Vector2Int, (List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<Color> c, List<int> t)>();
            int blades = 0;
            float R() => (float)rnd.NextDouble();
            foreach (var p in patches)
            {
                int count = Mathf.RoundToInt(p.Area.width * p.Area.height * p.Density);
                for (int i = 0; i < count; i++)
                {
                    float x = p.Area.xMin + R() * p.Area.width, z = p.Area.yMin + R() * p.Area.height;
                    float y = p.Height(x, z);
                    if (float.IsNaN(y)) continue;
                    var key = new Vector2Int(Mathf.FloorToInt(x / Cell), Mathf.FloorToInt(z / Cell));
                    if (!chunks.TryGetValue(key, out var ch)) { ch = (new List<Vector3>(), new List<Vector3>(), new List<Vector2>(), new List<Color>(), new List<int>()); chunks[key] = ch; }
                    if (ch.v.Count > 60000) continue;
                    // one blade: a tapered, slightly curved strip (root L/R, middle L/R, tip)
                    float h = Mathf.Lerp(p.MinH, p.MaxH, Mathf.Pow(R(), 0.8f));
                    float w = Mathf.Lerp(0.035f, 0.06f, R());
                    float yaw = R() * Mathf.PI * 2f;
                    var side = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
                    var lean = new Vector3(-side.z, 0f, side.x) * (0.08f + 0.22f * R()) * h;
                    var nrm = Vector3.Cross(side, Vector3.up).normalized;
                    var b = new Vector3(x, y - 0.03f, z);
                    var col = new Color(R(), R(), h / 2f, 1f);
                    int o = ch.v.Count;
                    ch.v.Add(b - side * w); ch.v.Add(b + side * w);
                    var mid = b + Vector3.up * h * 0.55f + lean * 0.35f;
                    ch.v.Add(mid - side * w * 0.6f); ch.v.Add(mid + side * w * 0.6f);
                    ch.v.Add(b + Vector3.up * h + lean);
                    for (int k = 0; k < 5; k++) { ch.n.Add(nrm); ch.c.Add(col); }
                    ch.uv.Add(new Vector2(0f, 0f)); ch.uv.Add(new Vector2(1f, 0f)); ch.uv.Add(new Vector2(0f, 0.55f)); ch.uv.Add(new Vector2(1f, 0.55f)); ch.uv.Add(new Vector2(0.5f, 1f));
                    ch.t.AddRange(new[] { o, o + 2, o + 1, o + 1, o + 2, o + 3, o + 2, o + 4, o + 3 });
                    blades++;
                }
            }
            foreach (var kv in chunks)
            {
                var (v, n, uv, c, t) = kv.Value;
                var m = new Mesh { indexFormat = IndexFormat.UInt32 };
                m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetColors(c); m.SetTriangles(t, 0);
                m.RecalculateBounds();
                var bb = m.bounds; bb.Expand(new Vector3(0.6f, 0.2f, 0.6f)); m.bounds = bb;   // room for the sway
                var go = MeshObject($"Grass_{kv.Key.x}_{kv.Key.y}", SaveMesh(m, $"Venus_Grass_{kv.Key.x}_{kv.Key.y}"), mat, root, false);
                go.GetComponent<MeshRenderer>().receiveShadows = true;
            }
            Log($"grass: {blades} blades in {chunks.Count} chunks over {patches.Count} patches");
            return blades;
        }
    }
}
