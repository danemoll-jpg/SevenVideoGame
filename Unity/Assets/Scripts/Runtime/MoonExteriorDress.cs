using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF32 (4): FIN_01's exterior is Chapter 1's real Moon level baked (Resources/Outside/Outside_moon) and seen from OUTSIDE - from
    /// there the host's pieces read as plain boxes (they were built to be seen from within). Every plain box piece of that exterior
    /// (a mesh of at most 24 vertices, at least 1.5 m thick, over 6 m long) is dressed at runtime as worked stone: a plinth at its
    /// foot, string courses every ~3 m up its sides, pilasters every ~5 m along each face, a cornice on top and a few fallen blocks
    /// at its foot - all in the piece's own material (monochrome; the detail reads through its shading), combined into ONE mesh per
    /// piece (no shadows, the exterior copies cast none). Protrusions are 6-22 cm, never coplanar with the face (no z-fighting).
    /// </summary>
    public static class MoonExteriorDress
    {
        public static int Dressed, Parts;

        public static void Dress(GameObject world)
        {
            Dressed = 0; Parts = 0;
            if (world == null) return;
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            if (cube == null) { var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube); cube = tmp.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(tmp); }
            var root = new GameObject("MoonExterior_Dressing").transform; root.SetParent(world.transform, false);
            var rnd = new System.Random(3207);
            foreach (var mr in world.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mf = mr.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                var m = mf.sharedMesh; if (m.vertexCount > 24 || mr.sharedMaterial == null || mr.sharedMaterial.renderQueue > 2500) continue;
                var t = mr.transform; var mb = m.bounds;
                // the piece as an oriented box in world space (its own rotation, its scaled size)
                var ax = t.TransformVector(new Vector3(mb.size.x, 0f, 0f)); var ay = t.TransformVector(new Vector3(0f, mb.size.y, 0f)); var az = t.TransformVector(new Vector3(0f, 0f, mb.size.z));
                float sx = ax.magnitude, sy = ay.magnitude, sz = az.magnitude;
                if (Mathf.Abs(Vector3.Dot(ay.normalized, Vector3.up)) < 0.95f) continue;           // upright pieces only (walls, blocks, towers)
                // solid blocks (>= 1.5 m thick, > 6 m long) and - QF32 (4), rc5's first pass missed them - the WALL SLABS of the rooms seen
                // from outside (0.2-1.5 m thick, >= 2.5 m tall, >= 4 m long): the big plain faces of FIN_01's approach
                bool wall = Mathf.Min(sx, sz) < 1.5f;
                if (wall ? (Mathf.Min(sx, sz) < 0.15f || sy < 2.5f || Mathf.Max(sx, sz) < 4f) : (sy < 1.5f || Mathf.Max(sx, Mathf.Max(sy, sz)) < 6f)) continue;
                var c = t.TransformPoint(mb.center); var rot = Quaternion.LookRotation(az.normalized, ay.normalized);
                var list = new List<CombineInstance>();
                void Box(Vector3 localCentre, Vector3 size)
                {
                    var w = Matrix4x4.TRS(c + rot * localCentre, rot, size);
                    list.Add(new CombineInstance { mesh = cube, transform = root.worldToLocalMatrix * w });
                }
                float hx = sx * 0.5f, hy = sy * 0.5f, hz = sz * 0.5f;
                // plinth (the foot, 0.6-1.2 m) and cornice (the top, 0.35 m, a little proud)
                float plinth = Mathf.Clamp(sy * 0.06f, 0.6f, 1.2f);
                Box(new Vector3(0f, -hy + plinth * 0.5f, 0f), new Vector3(sx + 0.36f, plinth, sz + 0.36f) * 1f);
                if (wall) Box(new Vector3(0f, hy - 0.14f, 0f), new Vector3(sx + 0.3f, 0.36f, sz + 0.3f));   // a wall's coping (its top is no floor)
                // the cornice is a rim of four stones (a full slab would sit on the top face: z-fighting / covering the berth)
                if (!wall) Box(new Vector3(0f, hy - 0.14f, hz + 0.1f), new Vector3(sx + 0.44f, 0.36f, 0.42f));
                Box(new Vector3(0f, hy - 0.14f, -hz - 0.1f), new Vector3(sx + 0.44f, 0.36f, 0.42f));
                Box(new Vector3(hx + 0.1f, hy - 0.14f, 0f), new Vector3(0.42f, 0.36f, sz + 0.44f));
                Box(new Vector3(-hx - 0.1f, hy - 0.14f, 0f), new Vector3(0.42f, 0.36f, sz + 0.44f));
                Box(new Vector3(0f, hy - 0.5f, 0f), new Vector3(sx + 0.2f, 0.16f, sz + 0.2f));
                // string courses every ~3 m between them
                int courses = Mathf.Clamp(Mathf.FloorToInt((sy - plinth - 0.8f) / 3f), 0, 14);
                for (int i = 1; i <= courses; i++) { float y = -hy + plinth + i * (sy - plinth - 0.6f) / (courses + 1); Box(new Vector3(0f, y, 0f), new Vector3(sx + 0.14f, 0.14f, sz + 0.14f)); }
                // pilasters every ~5 m along each face (corners included), plinth to cornice
                float ph = sy - plinth - 0.55f, py = -hy + plinth + ph * 0.5f;
                int nx = Mathf.Clamp(Mathf.RoundToInt(sx / 5f), 1, 12), nz = Mathf.Clamp(Mathf.RoundToInt(sz / 5f), 1, 12);
                for (int i = 0; i <= nx; i++)
                {
                    float x = -hx + sx * i / nx;
                    Box(new Vector3(x, py, hz + 0.06f), new Vector3(0.7f, ph, 0.24f));
                    Box(new Vector3(x, py, -hz - 0.06f), new Vector3(0.7f, ph, 0.24f));
                }
                for (int i = 0; i <= nz; i++)
                {
                    float z = -hz + sz * i / nz;
                    Box(new Vector3(hx + 0.06f, py, z), new Vector3(0.24f, ph, 0.7f));
                    Box(new Vector3(-hx - 0.06f, py, z), new Vector3(0.24f, ph, 0.7f));
                }
                // a few fallen blocks along its foot (broken stone, tilted)
                int fallen = Mathf.Clamp(Mathf.RoundToInt((sx + sz) / 9f), 1, 6);
                for (int i = 0; i < fallen; i++)
                {
                    float u = (float)rnd.NextDouble(); int face = rnd.Next(4); float s = 0.6f + (float)rnd.NextDouble() * 1.1f;
                    var along = face < 2 ? new Vector3(-hx + u * sx, 0f, (face == 0 ? 1f : -1f) * (hz + 0.5f + s * 0.5f)) : new Vector3((face == 2 ? 1f : -1f) * (hx + 0.5f + s * 0.5f), 0f, -hz + u * sz);
                    var w = Matrix4x4.TRS(c + rot * (along + new Vector3(0f, -hy + s * 0.35f, 0f)), rot * Quaternion.Euler((float)rnd.NextDouble() * 30f - 15f, (float)rnd.NextDouble() * 90f, (float)rnd.NextDouble() * 30f - 15f), new Vector3(s, s * 0.7f, s * 1.2f));
                    list.Add(new CombineInstance { mesh = cube, transform = root.worldToLocalMatrix * w });
                }
                var dm = new Mesh { name = "Dress_" + mr.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                dm.CombineMeshes(list.ToArray(), true, true);
                dm.RecalculateBounds();
                var g = new GameObject("Dress_" + mr.name); g.transform.SetParent(root, false);
                g.AddComponent<MeshFilter>().sharedMesh = dm;
                var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mr.sharedMaterial; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = mr.receiveShadows;
                g.layer = mr.gameObject.layer;
                Dressed++; Parts += list.Count;
            }
        }
    }
}
