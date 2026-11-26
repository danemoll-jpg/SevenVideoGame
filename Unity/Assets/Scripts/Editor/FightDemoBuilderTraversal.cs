using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using L = Seven.MoonLayout;
using T = Seven.MoonTraversal;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 9: Chapter 1 traversal geometry (numbers in <see cref="Seven.MoonTraversal"/>): the broken catwalk, the low
    /// pipe run, the bulkhead with its lip; the cracked wall, the shaft lip over the dark drop and the broken ruin
    /// terraces with the fallen slab and the steep stair down to the hall; the vision alcove with Kyle's floating
    /// shape; the mining cut after the sealed door; the broken ramp and scaffolds down to the bay. Climbable edges are
    /// pale worn stone / scuffed metal lips that catch the light - never painted markers.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        static Material _lip;

        static Material LipMat()
        {
            if (_lip == null) _lip = Dissolve("Moon_lip", Gray(0.66f), 0.55f, false, 0.2f);
            return _lip;
        }

        /// <summary>Pale lip along a ledge (the top edge of its wall face), slightly proud of the face.</summary>
        static void LipStrip(string name, T.Ledge l, Transform parent)
        {
            var a = l.A - l.Fwd * 0.035f + Vector3.down * 0.07f;
            var b = l.B + l.Fwd * 0.09f + Vector3.up * 0.012f;
            Block(name, Vector3.Min(a, b), Vector3.Max(a, b), LipMat(), parent, false);
        }

        static void ColliderBox(string name, Vector3 min, Vector3 max, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = (min + max) * 0.5f;
            var c = go.AddComponent<BoxCollider>(); c.size = max - min;
        }

        /// <summary>Thin darker crack lines on a wall face (face plane z = faceZ facing +z or -z, or x = faceX).</summary>
        static void Cracks(string name, Vector3 center, Vector3 right, Vector3 up, Vector3 outN, float w, float h, int seed, Material mat, Transform parent)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            var rnd = new System.Random(seed);
            for (int k = 0; k < 5; k++)
            {
                var pts = new List<Vector3>();
                var p = center + right * ((float)rnd.NextDouble() - 0.5f) * w + up * (h * 0.5f);
                for (int i = 0; i < 6; i++)
                {
                    pts.Add(p + outN * 0.006f);
                    p += up * (-h / 6f) + right * (((float)rnd.NextDouble() - 0.5f) * 0.35f);
                }
                Sweep(v, t, pts, 0.012f, false, 4);
            }
            MeshObject(name, SaveMesh(SmoothFinish(v, t), "Moon_" + name), mat, parent, false);
        }

        // ------------------------------------------------------------------------------------------ prison
        static void BuildPrisonTraversal(Transform root, MoonMats m)
        {
            float zf1 = L.CellWallZ1;
            // Broken catwalk: two grating sections with a gap (8.4 .. 10.0), bent flaps hanging into the haze.
            Block("Catwalk", new Vector3(-9f, -0.25f, zf1), new Vector3(T.GapX.x, 0f, 4.95f), m.grate, root);
            Block("CatwalkE", new Vector3(T.GapX.y, -0.25f, zf1), new Vector3(34f, 0f, 4.95f), m.grate, root);
            var fl = GameObject.CreatePrimitive(PrimitiveType.Cube); fl.name = "Gap_FlapW"; fl.transform.SetParent(root, false);
            fl.transform.position = new Vector3(T.GapX.x + 0.12f, -0.45f, 3.2f); fl.transform.localScale = new Vector3(0.6f, 0.04f, 2.3f);
            fl.transform.rotation = Quaternion.Euler(0f, 0f, -58f); fl.GetComponent<MeshRenderer>().sharedMaterial = m.grate; Object.DestroyImmediate(fl.GetComponent<Collider>());
            var fe = GameObject.CreatePrimitive(PrimitiveType.Cube); fe.name = "Gap_FlapE"; fe.transform.SetParent(root, false);
            fe.transform.position = new Vector3(T.GapX.y - 0.1f, -0.38f, 3.9f); fe.transform.localScale = new Vector3(0.5f, 0.04f, 1.6f);
            fe.transform.rotation = Quaternion.Euler(0f, 0f, 64f); fe.GetComponent<MeshRenderer>().sharedMaterial = m.grate; Object.DestroyImmediate(fe.GetComponent<Collider>());
            Railing("CatwalkRail", new Vector3(-9f, 0f, 4.8f), new Vector3(T.GapX.x - 0.1f, 0f, 4.8f), m.metal, root);
            Railing("CatwalkRailE", new Vector3(T.GapX.y + 0.1f, 0f, 4.8f), new Vector3(34f, 0f, 4.8f), m.metal, root);
            var tv = new List<Vector3>(); var tt = new List<int>();
            foreach (var z in new[] { 4.15f, 4.55f })
            {
                AddBox(tv, tt, new Vector3((-8.5f + T.GapX.x) * 0.5f, 0.04f, z), new Vector3(T.GapX.x + 8.5f, 0.07f, 0.07f));
                AddBox(tv, tt, new Vector3((T.GapX.y + 34f) * 0.5f, 0.04f, z), new Vector3(34f - T.GapX.y, 0.07f, 0.07f));
            }
            for (float x = -8.5f; x < 34f; x += 0.8f) if (x < T.GapX.x - 0.1f || x > T.GapX.y + 0.1f) AddBox(tv, tt, new Vector3(x, 0.02f, 4.35f), new Vector3(0.12f, 0.04f, 0.7f));
            MeshObject("RailTrack", SaveMesh(Finish(tv, tt), "Moon_RailTrack"), m.metalDark, root);

            // Low pipe run across the catwalk (bottom at 1.15 m): only crouched. Pipes run out of the rock face, over the
            // rail and bend down into the void; stacked up to a duct so there is no way over.
            var pv = new List<Vector3>(); var pt = new List<int>();
            foreach (var (x, y, r) in new[] { (11.62f, 1.27f, 0.12f), (12.06f, 1.27f, 0.12f), (12.5f, 1.29f, 0.14f), (12.88f, 1.27f, 0.11f),
                                              (11.8f, 1.6f, 0.15f), (12.4f, 1.62f, 0.16f), (12.85f, 1.6f, 0.13f) })
                Sweep(pv, pt, new[] { new Vector3(x, y, 1.3f), new Vector3(x, y, 5.15f), new Vector3(x, y - 0.25f, 5.5f), new Vector3(x, -6f, 5.6f) }, r, false, 10);
            MeshObject("PipeRun", SaveMesh(SmoothFinish(pv, pt), "Moon_PipeRun"), m.metal, root);
            // a low duct on top (too high to jump over: the jump arc + her body cannot clear 2.05 m)
            Block("PipeDuct", new Vector3(T.PipeX.x, 1.78f, 1.3f), new Vector3(T.PipeX.y, 2.05f, 5.3f), m.metalDark, root);
            ColliderBox("PipeRun_Collider", new Vector3(T.PipeX.x + 0.05f, T.PipeBottom, zf1), new Vector3(T.PipeX.y, 2.05f, 4.95f), root);
            // the camera passes through the pipes (layer 21) - a hanging / crouching 7 stays in view behind them
            foreach (var n in new[] { "PipeRun_Collider", "PipeDuct", "PipeRun" }) { var go = root.Find(n); if (go != null) go.gameObject.layer = 21; }
            foreach (var x in new[] { T.PipeX.x - 0.05f, T.PipeX.y + 0.05f })
                Block($"PipeBracket{x}", new Vector3(x - 0.05f, 1.0f, 4.95f), new Vector3(x + 0.05f, 2.05f, 5.1f), m.metalDark, root).layer = 21;
            Lamp("PipeLamp", new Vector3(12.2f, 2.6f, 2.05f), 4.5f, 0.6f, m.glow, root, new Vector3(0.35f, 0.05f, 0.08f));

            // The bulkhead: a closed blast-door frame across the catwalk, 2.4 m; a scuffed lip on its near top edge.
            Block("Bulkhead", new Vector3(T.BulkheadX.x, -0.25f, zf1), new Vector3(T.BulkheadX.y, T.BulkheadTop, 4.95f), m.metalDark, root);
            Block("Bulkhead_Band", new Vector3(T.BulkheadX.x - 0.02f, 1.1f, zf1), new Vector3(T.BulkheadX.x + 0.01f, 1.22f, 4.95f), m.metal, root, false);
            LipStrip("Bulkhead_Lip", T.Bulkhead, root);
            Lamp("BulkheadLamp", new Vector3(13.4f, 3.05f, 2.05f), 4.5f, 0.75f, m.glow, root, new Vector3(0.3f, 0.05f, 0.08f));
        }

        // ------------------------------------------------------------------------------------------ passage + ruins
        static void BuildPassageRuins(Transform root, MoonMats m, Props props)
        {
            float x0 = L.PassageX.x, x1 = L.PassageX.y, top = T.CrackWall.A.y;
            // Older passage behind the sinking slab, tall enough to climb in.
            Block("Passage0_Floor", new Vector3(x0 - 0.5f, -0.3f, -0.2f), new Vector3(x1 + 0.5f, 0f, L.CellWallZ0), m.ancientDark, root);
            Block("Passage0_WallW", new Vector3(x0 - 0.5f, -0.3f, -2.4f), new Vector3(x0, 5.8f, L.CellWallZ0), m.ancientDark, root);
            Block("Passage0_WallE", new Vector3(x1, -0.3f, -2.4f), new Vector3(x1 + 0.5f, 5.8f, L.CellWallZ0), m.ancientDark, root);
            Block("Passage0_Ceiling", new Vector3(x0 - 0.5f, 5.8f, -2.4f), new Vector3(x1 + 0.5f, 6.3f, L.CellWallZ0), m.ancientDark, root);
            // The cracked wall (pale old blocks) up to the shelf; its lip; cracks; a stone stub for the cat.
            Block("CrackedWall", new Vector3(x0, -10.5f, -2.4f), new Vector3(x1, top, -0.2f), m.ancient, root);
            Cracks("CrackedWall_Cracks", new Vector3(16.5f, 1.35f, -0.2f), Vector3.right, Vector3.up, Vector3.forward, 1.4f, 2.4f, 41, m.ancientDark, root);
            LipStrip("CrackedWall_Lip", T.CrackWall, root);
            Block("CatStub_Crack", new Vector3(17.0f, T.StubCrackWall.y - 0.1f, -0.2f), new Vector3(x1, T.StubCrackWall.y, 0.25f), LipMat(), root);
            PointLight("PassageGlow", new Vector3(16.5f, 2.2f, 0.9f), 6f, 0.75f, root);
            PointLight("ShelfGlow", new Vector3(16.5f, 4.6f, -1.4f), 6f, 0.6f, root);

            // The ruins cavern: shaft north rock with its lip (a narrow 0.3 m stone ledge, rock above set back), walls,
            // ceiling, a dark chasm bottom far below (falls never reach it: white fade first).
            Block("Shaft_NorthRock", new Vector3(5.5f, -10.5f, -2.4f), new Vector3(x0 - 0.5f, top, -0.6f), m.rock, root);
            Block("Shaft_NorthRockAbove", new Vector3(5.5f, top, -2.1f), new Vector3(x0 - 0.5f, 6.3f, -0.6f), m.rock, root);
            LipStrip("Shaft_Lip", T.ShaftLip, root);
            Block("Shaft_East", new Vector3(x1, -10.5f, -4.6f), new Vector3(x1 + 0.5f, 6.3f, -2.4f), m.rock, root);
            Block("Ruins_NERock", new Vector3(x1, -10.5f, -2.4f), new Vector3(20.9f, 6.3f, -0.6f), m.rock, root);
            Block("Ruins_WallW", new Vector3(5.5f, -10.5f, -8.6f), new Vector3(6.0f, 6.3f, -2.4f), m.rock, root);
            Block("Ruins_WallE", new Vector3(20.4f, -10.5f, -8.6f), new Vector3(20.9f, 6.3f, -4.6f), m.rock, root);
            Block("Ruins_WallSW", new Vector3(5.5f, -10.5f, -8.6f), new Vector3(L.HallMin.x - 0.5f, 6.3f, -8.1f), m.rock, root);
            Block("Ruins_WallSLow", new Vector3(L.HallMin.x - 0.5f, -10.5f, -8.6f), new Vector3(20.4f, L.Lower - 0.3f, -8.1f), m.rock, root);
            Block("Ruins_WallSHigh", new Vector3(L.HallMin.x - 0.5f, L.Lower + 6.4f, -8.6f), new Vector3(20.9f, 6.3f, -8.1f), m.rock, root);
            Block("Ruins_Ceiling", new Vector3(5.5f, 6.0f, -8.6f), new Vector3(20.9f, 6.5f, -0.6f), m.rock, root);
            Block("Ruins_ChasmBottom", new Vector3(5.5f, -11f, -8.6f), new Vector3(20.9f, -10.5f, -0.6f), m.rockDark, root);

            // Broken terraces over the dark (pale tops on darker bodies).
            Block("RuinsLedge", new Vector3(10.4f, -10.5f, -4.6f), new Vector3(13.0f, -1.6f, -2.6f), m.ancient, root);
            Block("TerraceA", new Vector3(7.0f, -10.5f, -4.8f), new Vector3(9.2f, -1.6f, -2.6f), m.ancient, root);
            Block("TerraceB", new Vector3(6.6f, -10.5f, -6.8f), new Vector3(9.6f, -0.7f, -4.8f), m.ancientDark, root);
            Block("TerraceB_Lip", new Vector3(7.0f, -0.76f, -4.84f), new Vector3(9.2f, -0.69f, -4.76f), LipMat(), root, false);
            Block("TerraceC", new Vector3(10.8f, -10.5f, -7.0f), new Vector3(13.8f, 0.8f, -5.0f), m.ancient, root);
            LipStrip("TerraceC_Lip", T.TerraceCLip, root);
            // fallen column on terrace C's west edge (blocks climbing up there: she shimmies north to the clear part)
            var colMesh = Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.36f, 0f), new Vector2(0.36f, 1.4f), new Vector2(0f, 1.4f) }, 0f, 360f, 16);
            var col = MeshObject("FallenColumn", SaveMesh(colMesh, "Moon_FallenColumn"), m.ancientDark, root);
            col.transform.position = new Vector3(11.25f, 0.8f + 0.36f, -6.95f); col.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            props.extraObstacles.Add(new Vector3(11.25f, -6.15f, 0.42f)); props.extraObstacles.Add(new Vector3(11.25f, -6.7f, 0.42f));
            // the steep ancient stair from terrace C down to the hall door (blocked by the fallen slab until telekinesis)
            for (int i = 0; i < 10; i++)
            {
                float sx = 13.8f + 0.4f * i, hc = 0.8f - 0.8f * (0.4f * i + 0.2f);
                Block($"Stair{i}", new Vector3(sx, -10.5f, -5.8f), new Vector3(sx + 0.4f, hc + 0.12f, -4.6f), i % 2 == 0 ? m.ancient : m.ancientDark, root);
            }
            Block("StairFoot", new Vector3(17.8f, -10.5f, -7.0f), new Vector3(19.6f, L.Lower, -4.7f), m.ancientDark, root);
            Block("RuinsFloor", new Vector3(15.0f, -10.5f, -8.6f), new Vector3(20.4f, L.Lower, -6.4f), m.ancientDark, root);
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube); slab.name = "FallenSlab"; slab.transform.SetParent(root, false);
            Object.DestroyImmediate(slab.GetComponent<Collider>());
            slab.transform.position = T.SlabRest; slab.transform.localScale = T.SlabSize; slab.transform.rotation = Quaternion.Euler(4f, 12f, -6f);
            slab.GetComponent<MeshRenderer>().sharedMaterial = m.ancient;
            props.slab = slab.transform;
            // a broken column in the shaft (the cat's stepping stone)
            // P3 (3): its top 10 cm under the stop - Panther's paws stood 10 cm INSIDE the old top (BODY vs PROPS at the shaft); and a
            // little wider, so his four paws are all on stone
            float stubTop = T.StubShaft.y - 0.1f + 10.5f;
            var stub = Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.26f, 0f), new Vector2(0.24f, stubTop - 0.2f), new Vector2(0.3f, stubTop - 0.05f), new Vector2(0.28f, stubTop), new Vector2(0f, stubTop) },
                0f, 360f, 14, null, new Vector3(T.StubShaft.x, -10.5f, T.StubShaft.z));
            MeshObject("ShaftColumn", SaveMesh(stub, "Moon_ShaftColumn"), m.ancientDark, root);
            var rv = new List<Vector3>(); var rt = new List<int>();
            var rnd = new System.Random(19);
            for (int i = 0; i < 14; i++) Lump(rv, rt, new Vector3(Mathf.Lerp(6.5f, 20f, (float)rnd.NextDouble()), -10.3f, Mathf.Lerp(-8f, -1.2f, (float)rnd.NextDouble())), Vector3.one * Mathf.Lerp(0.4f, 1.2f, (float)rnd.NextDouble()), 0.35f, 700 + i);
            MeshObject("ChasmRubble", SaveMesh(SmoothFinish(rv, rt), "Moon_ChasmRubble"), m.rockDark, root, false);
            PointLight("RuinsGlowA", new Vector3(9f, 1.4f, -4.4f), 7.5f, 0.75f, root);
            PointLight("RuinsGlowB", new Vector3(12.4f, 2.6f, -6.2f), 7f, 0.65f, root);
            PointLight("StairGlow", new Vector3(18.6f, 0.2f, -6.6f), 7f, 0.7f, root);
            PointLight("ChasmGlow", new Vector3(13f, -6.5f, -5f), 14f, 0.5f, root);
        }

        // ------------------------------------------------------------------------------------------ vision object
        /// <summary>Kyle's floating shape (wireframe, thin bright edges, small vertex knots) as local-space mesh data.</summary>
        static Mesh ShapeMesh(Seven.PlatonicShape.Kind kind, float radius, float tube, string name)
        {
            var verts = Seven.PlatonicShape.Vertices(kind);
            var v = new List<Vector3>(); var t = new List<int>();
            foreach (var (a, b) in Seven.PlatonicShape.Edges(verts)) Sweep(v, t, new[] { verts[a] * radius, verts[b] * radius }, tube, false, 6);
            foreach (var p in verts) Ellipsoid(v, t, p * radius, Vector3.one * tube * 1.9f, 5, 8);
            return SaveMesh(SmoothFinish(v, t), name);
        }

        /// <summary>The shape's outline projected flat (for the sealed door and the vision's frieze).</summary>
        static Mesh ShapeOutline(Seven.PlatonicShape.Kind kind, Vector3 center, Vector3 right, Vector3 up, Vector3 outN, float radius, float tube, string name)
        {
            var verts = Seven.PlatonicShape.Vertices(kind);
            var rot = Quaternion.Euler(18f, 24f, 0f);
            var v = new List<Vector3>(); var t = new List<int>();
            Vector3 P(Vector3 q) { q = rot * q * radius; return center + right * q.x + up * q.y + outN * 0.01f; }
            foreach (var (a, b) in Seven.PlatonicShape.Edges(verts)) Sweep(v, t, new[] { P(verts[a]), P(verts[b]) }, tube, false, 5);
            return SaveMesh(SmoothFinish(v, t), name);
        }

        static void BuildVisionShape(Transform root, MoonMats m, Props props, Transform door)
        {
            float y0 = L.Lower;
            // alcove in the chamber's east wall
            Block("ShapeAlcove_Back", new Vector3(L.AlcoveBackX, y0 - 0.3f, L.AlcoveZ.x - 0.5f), new Vector3(L.AlcoveBackX + 0.5f, y0 + 3.0f, L.AlcoveZ.y + 0.5f), m.ancientDark, root);
            Block("ShapeAlcove_S", new Vector3(L.VisionMax.x, y0 - 0.3f, L.AlcoveZ.x - 0.5f), new Vector3(L.AlcoveBackX, y0 + 3.0f, L.AlcoveZ.x), m.ancientDark, root);
            Block("ShapeAlcove_N", new Vector3(L.VisionMax.x, y0 - 0.3f, L.AlcoveZ.y), new Vector3(L.AlcoveBackX, y0 + 3.0f, L.AlcoveZ.y + 0.5f), m.ancientDark, root);
            Block("ShapeAlcove_Top", new Vector3(L.VisionMax.x, y0 + 2.6f, L.AlcoveZ.x - 0.5f), new Vector3(L.AlcoveBackX + 0.5f, y0 + 3.0f, L.AlcoveZ.y + 0.5f), m.ancientDark, root);
            Block("ShapeAlcove_Floor", new Vector3(L.VisionMax.x, y0 - 0.3f, L.AlcoveZ.x), new Vector3(L.AlcoveBackX, y0, L.AlcoveZ.y), m.ancientDark, root);
            // worn pale pedestal
            var ped = Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.36f, 0f), new Vector2(0.34f, 0.12f), new Vector2(0.24f, 0.2f), new Vector2(0.21f, 0.86f),
                                    new Vector2(0.3f, 0.94f), new Vector2(0.31f, 1.0f), new Vector2(0f, 1.0f) }, 0f, 360f, 20, null, L.Pedestal);
            MeshObject("ShapePedestal", SaveMesh(ped, "Moon_ShapePedestal"), m.ancient, root);
            // the shape: thin bright edges + knots, a soft light inside the alcove
            // Round 10: the hand-held dodecahedron (15 cm, pale stone, thin glowing edges, the Elarim marking on one
            // face); the director drives its hover / turn and the edge glow, and 7 takes it after the door scene.
            var es = BuildElarimShape("VisionShape", root, ElarimMaterials(false), "Moon_Dodecahedron");
            var shape = es.transform; shape.position = L.ShapeAt;
            var edges = es.Edges; es.Edges = null;                       // glow driven by Chapter1Director
            var sl = PointLight("ShapeLight", L.ShapeAt + Vector3.left * 0.15f, 3.6f, 1.1f, shape);
            props.shape = shape; props.inscription = edges; props.shapeLight = sl;
            // (vision addition: the door opens by touching the round mechanism - its glow ring is built with the door)
            PointLight("InscriptionLight", new Vector3(16.5f, L.Lower + 2.2f, -36.6f), 4.5f, 0.55f, root);
        }

        // ------------------------------------------------------------------------------------------ mining cut
        static void BuildVisionExitPit(Transform root, MoonMats m)
        {
            float x0 = L.PassageX.x, x1 = L.PassageX.y, z0 = L.VisionMin.y - 0.45f;
            float pitN = T.VisionExitLip.A.z, pitS = T.MineLip.A.z, floor = -6.4f;
            Block("VisionExit_Floor", new Vector3(x0 - 0.5f, floor - 0.3f, pitN), new Vector3(x1 + 0.5f, L.Lower, z0), m.ancientDark, root);
            Block("MinePit_Floor", new Vector3(x0 - 0.5f, floor - 0.5f, pitS), new Vector3(x1 + 0.5f, floor, pitN), m.rockDark, root);
            Block("MineTop", new Vector3(x0 - 0.5f, floor - 0.3f, -42.0f), new Vector3(x1 + 0.5f, L.Lower, pitS), m.ancient, root);
            Block("VisionExit_WallW", new Vector3(x0 - 0.5f, floor - 0.5f, -42.0f), new Vector3(x0, L.Lower + 3.0f, z0), m.ancientDark, root);
            Block("VisionExit_WallE", new Vector3(x1, floor - 0.5f, -42.0f), new Vector3(x1 + 0.5f, L.Lower + 3.0f, z0), m.ancientDark, root);
            Block("VisionExit_Ceiling", new Vector3(x0 - 0.5f, L.Lower + 3.0f, -42.0f), new Vector3(x1 + 0.5f, L.Lower + 3.4f, z0), m.ancientDark, root);
            LipStrip("VisionExit_Lip", T.VisionExitLip, root);
            LipStrip("Mine_Lip", T.MineLip, root);
            Cracks("Mine_Cracks", new Vector3(16.5f, floor + 2.0f, pitS), Vector3.right, Vector3.up, Vector3.forward, 1.4f, 3.6f, 77, m.ancientDark, root);
            Block("CatStub_Mine", new Vector3(16.95f, T.StubMine.y - 0.1f, pitS), new Vector3(x1, T.StubMine.y, pitS + 0.42f), LipMat(), root);
            // old mining marks: a rusted bracket and a dead work lamp in the cut
            Block("Mine_Bracket", new Vector3(15.65f, floor + 0.6f, -40.6f), new Vector3(15.75f, floor + 2.6f, -40.4f), m.metalDark, root);
            PointLight("PitGlow", new Vector3(16.5f, floor + 1.9f, -40.3f), 5.5f, 0.65f, root);
            PointLight("MineTopGlow", new Vector3(16.5f, L.Lower + 1.6f, -41.9f), 4f, 0.5f, root);
            // QF32 (3): the cut's walls were bare slabs (the checkpoint-6 start view - tr13 / t_move - read as a flat dark box). Old
            // mine shoring: steel ribs every ~0.8 m down both walls and two walers across them, 10 cm proud of the rock, no colliders
            // (her hang line runs down the middle, 0.6 m clear of them); a working lamp on the east wall lights them.
            {
                var rv = new List<Vector3>(); var rt = new List<int>();
                for (float z = -41.75f; z < z0 - 0.2f; z += 0.8f)
                    foreach (var x in new[] { x0 + 0.05f, x1 - 0.05f })
                        AddBox(rv, rt, new Vector3(x, (floor + L.Lower + 2.9f) * 0.5f, z), new Vector3(0.1f, L.Lower + 2.9f - floor, 0.14f));
                foreach (var y in new[] { floor + 1.6f, L.Lower + 0.4f, L.Lower + 2.2f })
                    foreach (var x in new[] { x0 + 0.11f, x1 - 0.11f })
                        AddBox(rv, rt, new Vector3(x, y, (-42.0f + z0) * 0.5f), new Vector3(0.06f, 0.18f, z0 + 42.0f - 0.2f));
                MeshObject("Mine_Shoring", SaveMesh(Finish(rv, rt), "Moon_MineShoring"), m.metalDark, root);
            }
            Lamp("MineWorkLamp", new Vector3(x1 - 0.14f, L.Lower + 1.2f, -40.2f), 6f, 1.6f, m.glow, root, new Vector3(0.08f, 0.1f, 0.34f));
        }

        // ------------------------------------------------------------------------------------------ bay descent
        static void BuildBayDescent(Transform root, MoonMats m)
        {
            float rz0 = -50.3f, rz1 = T.RampEnd.A.z, ry1 = T.RampEnd.A.y;
            Block("RampTop_Floor", new Vector3(L.RampX.x, L.Lower - 0.25f, rz0), new Vector3(L.RampX.y, L.Lower, L.LedgeMin.y), m.grate, root);
            SlopeBlock("Ramp", L.RampX.x, L.RampX.y, rz0, rz1, L.Lower, ry1, 0.25f, m.grate, root);
            foreach (var x in new[] { L.RampX.x - 0.05f, L.RampX.y + 0.05f })
            {
                var v = new List<Vector3>(); var t = new List<int>();
                for (int i = 0; i <= 4; i++)
                {
                    float u = i / 4f; var p = new Vector3(x, Mathf.Lerp(L.Lower, ry1, u), Mathf.Lerp(rz0, rz1 + 0.3f, u));
                    Sweep(v, t, new[] { p, p + Vector3.up * 1.05f }, 0.035f, false, 6);
                }
                Sweep(v, t, new[] { new Vector3(x, L.Lower + 1.05f, rz0), new Vector3(x, ry1 + 1.05f + 0.1f, rz1 + 0.3f), new Vector3(x + (x < 21.5f ? -0.2f : 0.25f), ry1 + 0.2f, rz1 - 0.2f) }, 0.04f, false, 6);
                MeshObject("RampRail" + x, SaveMesh(SmoothFinish(v, t), "Moon_RampRail" + x), m.metal, root);
            }
            // the broken end: torn grating flaps
            var fl = GameObject.CreatePrimitive(PrimitiveType.Cube); fl.name = "RampFlap"; fl.transform.SetParent(root, false);
            fl.transform.position = new Vector3(21.1f, ry1 - 0.45f, rz1 - 0.08f); fl.transform.localScale = new Vector3(0.8f, 0.9f, 0.04f);
            fl.transform.rotation = Quaternion.Euler(-12f, 0f, 9f); fl.GetComponent<MeshRenderer>().sharedMaterial = m.grate; Object.DestroyImmediate(fl.GetComponent<Collider>());
            LipStrip("RampEnd_Lip", T.RampEnd, root);
            var sup = new List<Vector3>(); var supt = new List<int>();
            foreach (var z in new[] { -52.0f, -54.4f }) { float y = Mathf.Lerp(L.Lower, ry1, Mathf.InverseLerp(rz0, rz1, z)); AddBox(sup, supt, new Vector3(21.5f, (y + L.Bay) * 0.5f - 0.2f, z), new Vector3(0.3f, y - L.Bay, 0.3f)); }
            // two scaffold platforms (grating on posts) down to the bay floor
            foreach (var (name, z0, z1) in new[] { ("Scaffold1", -58.2f, -55.1f), ("Scaffold2", -62.4f, -59.4f) })
            {
                Block(name, new Vector3(19.8f, -7.8f, z0), new Vector3(23.2f, -7.6f, z1), m.grate, root);
                foreach (var px in new[] { 19.9f, 23.1f }) foreach (var pz in new[] { z0 + 0.1f, z1 - 0.1f })
                    AddBox(sup, supt, new Vector3(px, (L.Bay - 7.8f) * 0.5f, pz), new Vector3(0.14f, -7.8f - L.Bay, 0.14f));
                AddBox(sup, supt, new Vector3(21.5f, -8.9f, z0 + 0.1f), new Vector3(3.2f, 0.1f, 0.1f), 0f);
            }
            MeshObject("BayDescentSupports", SaveMesh(Finish(sup, supt), "Moon_BayDescentSupports"), m.metalDark, root);
            LipStrip("Scaffold_Lip", T.ScaffoldLip, root);
            Lamp("ScaffoldLamp", new Vector3(23.1f, -6.1f, -58.8f), 8f, 1.4f, m.glow, root, new Vector3(0.12f, 0.3f, 0.12f));
            Block("ScaffoldLampPole", new Vector3(23.05f, -7.6f, -58.85f), new Vector3(23.15f, -6.25f, -58.75f), m.metal, root);
        }
    }
}
