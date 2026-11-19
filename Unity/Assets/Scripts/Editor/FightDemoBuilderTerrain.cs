using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF10 item 6f (Docs/VENUS-TERRAIN.md): Venus's sculpted ground from <see cref="Seven.VenusTerrain"/> - a fine mesh
    /// (1 m) over the playable valley and a coarse one (8 m) out to the fog in every direction, so nowhere is there an
    /// edge, an underside or a void. Materials per triangle: the worn path (pale, also the maintenance path and the
    /// observation ledge), rock where the ground is steep (outcrops, the gully walls), dark earth under the grass
    /// elsewhere. The fine mesh has a collider (the camera's line-of-sight tests see the hills).
    /// </summary>
    public static partial class FightDemoBuilder
    {
        static void BuildVenusTerrain(Transform world, VenusMats m)
        {
            var root = new GameObject("Terrain").transform; root.SetParent(world, false);
            var ga = FineArea;
            var mats = new[] { m.leafDark, m.path, m.stoneMid, m.stoneDark };
            // ---- fine: the valley and the hills you can see from it (split into strips so no mesh gets too big)
            const float fine = 0.8f;
            int nx = Mathf.RoundToInt(ga.width / fine);
            int strips = 8; int nzAll = Mathf.RoundToInt(ga.height / fine), per = Mathf.CeilToInt(nzAll / (float)strips);
            for (int sIdx = 0; sIdx < strips; sIdx++)
            {
                int j0 = sIdx * per, j1 = Mathf.Min(nzAll, j0 + per);
                var go = TerrainPiece($"Ground_{sIdx}", ga.xMin, ga.yMin + j0 * fine, nx, j1 - j0, fine, mats, root, false, null);
                go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            }
            // ---- coarse: out to the fog (skips the cells inside the fine area; sinks a little where it tucks under it)
            const float coarse = 8f; var far = new Rect(-320f, -300f, 700f, 860f);
            TerrainPiece("Ground_Far", far.xMin, far.yMin, Mathf.RoundToInt(far.width / coarse), Mathf.RoundToInt(far.height / coarse), coarse, mats, root, true, ga);
            BuildVenusWater(world, m.water);
            Log($"terrain: fine {ga} at {fine} m in {strips} strips + coarse {far} at {coarse} m; route {Seven.VenusTerrain.RouteLength:0} m");
        }

        /// <summary>The fine ground: the valley plus the hills seen from it (beyond, the coarse ground is lost in the fog).</summary>
        static readonly Rect FineArea = new Rect(-85f, -55f, 225f, 350f);

        /// <summary>Water: the stream beside the path (it pours into the gully) and the stream along the gully's bottom.</summary>
        static void BuildVenusWater(Transform world, Material water)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            void Ribbon(List<Vector3> centre, float half, System.Func<Vector3, Vector3> side)
            {
                int o = v.Count;
                foreach (var c in centre) { var sd = side(c); v.Add(c - sd * half); v.Add(c + sd * half); }
                for (int i = 0; i < centre.Count - 1; i++) { int a = o + i * 2; t.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 }); }
            }
            var st = new List<Vector3>();
            for (float sP = 0f; sP < Seven.VenusTerrain.RouteLength; sP += 1f)
            {
                var c = Seven.VenusTerrain.AtS(sP); if (c.z < 26f || c.z > 117.6f) continue;
                var d = Seven.VenusTerrain.DirAtS(sP); var r = new Vector3(d.z, 0f, -d.x);
                var q = c + r * Seven.VenusTerrain.StreamLat; q.y = Seven.VenusTerrain.RawHeight(q.x, q.z) + 0.3f;
                st.Add(q);
            }
            if (st.Count > 1) Ribbon(st, 0.75f, c => { Seven.VenusTerrain.OnRoute(c.x, c.z, out _, out float s2, out _, out _); var d = Seven.VenusTerrain.DirAtS(s2); return new Vector3(d.z, 0f, -d.x); });
            var gl = new List<Vector3>();
            for (float x = -60f; x <= 120f; x += 1.5f) { float z = (Seven.VenusTerrain.GullyZ0 + Seven.VenusTerrain.GullyZ1) * 0.5f + 0.8f * Mathf.Sin(x * 0.11f); gl.Add(new Vector3(x, Seven.VenusTerrain.RawHeight(x, z) + 0.25f, z)); }
            Ribbon(gl, 1.4f, c => Vector3.back);
            var m = new Mesh { indexFormat = IndexFormat.UInt32 }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals();
            MeshObject("Streams", SaveMesh(m, "Venus_Streams"), water, world, false);
        }

        static float VisualHeight(float x, float z, bool coarse)
        {
            float r0 = Mathf.Sqrt(x * x + z * z);
            if (r0 < 14.6f) return -0.4f;                                    // under the landing platform's disc
            return coarse ? Seven.VenusTerrain.RawHeight(x, z) : Seven.VenusTerrain.Height(x, z);
        }

        static GameObject TerrainPiece(string name, float x0, float z0, int nx, int nz, float cell, Material[] mats, Transform parent, bool coarse, Rect? hole)
        {
            var v = new List<Vector3>((nx + 1) * (nz + 1));
            for (int j = 0; j <= nz; j++)
                for (int i = 0; i <= nx; i++)
                {
                    float x = x0 + i * cell, z = z0 + j * cell;
                    float y = VisualHeight(x, z, coarse);
                    if (hole.HasValue)
                    {
                        var h = hole.Value;
                        float inside = Mathf.Min(Mathf.Min(x - h.xMin, h.xMax - x), Mathf.Min(z - h.yMin, h.yMax - z));
                        if (inside > -cell) y -= 5f;                                 // tucked well under the fine ground at the seam
                    }
                    v.Add(new Vector3(x, y, z));
                }
            var tris = new List<int>[mats.Length]; for (int k = 0; k < mats.Length; k++) tris[k] = new List<int>();
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    float cx = x0 + (i + 0.5f) * cell, cz = z0 + (j + 0.5f) * cell;
                    if (hole.HasValue) { var h = hole.Value; if (cx - cell * 0.5f >= h.xMin + cell && cx + cell * 0.5f <= h.xMax - cell && cz - cell * 0.5f >= h.yMin + cell && cz + cell * 0.5f <= h.yMax - cell) continue; }
                    int a = j * (nx + 1) + i, b = a + 1, c = a + nx + 1, d = c + 1;
                    var nrm = Vector3.Cross(v[c] - v[a], v[b] - v[a]).normalized;
                    int mi = 0;
                    if (!coarse && IsPath(cx, cz)) mi = 1;
                    else if (nrm.y < 0.5f) mi = 3;                                       // steep: dark rock (outcrops, shelf risers, the gully walls)
                    tris[mi].AddRange(new[] { a, c, b, b, c, d });
                }
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(v);
            mesh.subMeshCount = mats.Length;
            for (int k = 0; k < mats.Length; k++) mesh.SetTriangles(tris[k], k);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(mesh, "Venus_" + name);
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterials = mats; r.shadowCastingMode = ShadowCastingMode.On;
            return go;
        }

        /// <summary>The worn path: along the route (an irregular edge), the maintenance path, the observation ledge.</summary>
        static bool IsPath(float x, float z)
        {
            if (z > Seven.VenusTerrain.GullyZ0 - 0.4f && z < Seven.VenusTerrain.GullyZ1 + 0.4f) return false;
            Seven.VenusTerrain.OnRoute(x, z, out float dP, out _, out _, out _);
            if (dP < Seven.VenusTerrain.PathHalf + 0.6f * (Seven.VenusTerrain.Noise(x * 0.5f, z * 0.5f) - 0.3f)) return true;
            if (x > 44.5f && x < 80f && z > 196.2f && z < 201.8f) return true;                 // the maintenance path
            if (x > 74.2f && x < 78.8f && z > 150f && z < 197f) return true;
            if (x > 64.5f && x < 87.5f && z > 134.5f && z < 151.5f) return Seven.VenusTerrain.Noise(x * 0.4f, z * 0.4f) > 0.3f;   // the ledge, worn in patches
            return false;
        }
    }
}
