using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 10: the galley scene set (Chapter 1 scene 10) and the hand-held dodecahedron shared by the Moon level and
    /// the saucer. Galley: a curved tulip table with two seats in front of the counter, a plate, fork and knife for 7, a
    /// bowl for the cat on the table, and a round porthole in the nook wall with the Moon hanging against the dark
    /// beyond it (a pale, softly glowing sphere; the saucer has left).
    /// </summary>
    public static partial class FightDemoBuilder
    {
        const string MarkDir = "Assets/Art/Marks";
        // Galley table layout (deck polar coordinates, like the rest of the saucer).
        const float TableAngle = 197f, TableR = 8.1f, TableTopY = 0.745f, SeatOff = 0.58f, PortholeY = 1.24f, PortholeRad = 0.255f;

        // --------------------------------------------------------------------------- the dodecahedron
        struct ElarimMats { public Material stone, edges, mark; }

        static Texture2D ElarimMarkTexture()
        {
            var src = Path.Combine(RepoRoot, "Art", "Marks", Seven.MoonLayout.ElarimMarkFile);
            Directory.CreateDirectory(Path.Combine(ProjectRoot, MarkDir));
            var dstAsset = MarkDir + "/" + Seven.MoonLayout.ElarimMarkFile;
            var dst = Path.Combine(ProjectRoot, dstAsset);
            if (!File.Exists(src)) { Log("ELARIM MARK MISSING: " + src + " (the marked face stays blank)"); return null; }
            if (!File.Exists(dst) || new FileInfo(dst).Length != new FileInfo(src).Length || File.GetLastWriteTimeUtc(src) > File.GetLastWriteTimeUtc(dst))
                File.Copy(src, dst, true);
            AssetDatabase.ImportAsset(dstAsset, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(dstAsset);
            imp.alphaIsTransparency = true; imp.alphaSource = TextureImporterAlphaSource.FromInput;
            imp.wrapMode = TextureWrapMode.Clamp; imp.mipmapEnabled = true; imp.maxTextureSize = 1024;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(dstAsset);
        }

        static ElarimMats ElarimMaterials(bool ship)
        {
            string sfx = ship ? "_ship" : "";
            var em = new ElarimMats
            {
                stone = Dissolve("Elarim_stone" + sfx, Gray(0.8f), 0.82f, ship, 0f),       // pale stone / crystal
                edges = Dissolve("Elarim_edges" + sfx, Color.white, 0.5f, ship),
            };
            em.stone.SetFloat("_Rim", 0.22f);
            em.edges.EnableKeyword("_EMISSION"); em.edges.SetColor("_EmissionColor", Color.white * 1.4f);
            // The engraving: URP Lit, alpha-clipped mark texture, dark grey lines with a faint white inner glow.
            var mk = Lit("Elarim_mark" + sfx, Gray(0.16f), 0.4f);
            var tex = ElarimMarkTexture();
            mk.SetTexture("_BaseMap", tex); mk.SetTexture("_MainTex", tex);
            mk.SetFloat("_AlphaClip", 1f); mk.SetFloat("_Cutoff", 0.5f); mk.EnableKeyword("_ALPHATEST_ON");
            mk.renderQueue = (int)RenderQueue.AlphaTest;
            mk.SetOverrideTag("RenderType", "TransparentCutout");
            mk.SetFloat("_Cull", 0f);
            mk.EnableKeyword("_EMISSION"); mk.SetTexture("_EmissionMap", tex); mk.SetColor("_EmissionColor", Color.white * 0.45f);
            mk.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(mk);
            em.mark = mk;
            return em;
        }

        /// <summary>The 15 cm dodecahedron: Body (stone), Edges (glow), Mark (engraving on face 0).</summary>
        static Seven.ElarimShape BuildElarimShape(string name, Transform parent, ElarimMats em, string meshPrefix)
        {
            float r = Seven.MoonLayout.ShapeRadius;
            var root = new GameObject(name).transform; root.SetParent(parent, false);
            var body = MeshObject("Body", SaveMesh(Seven.ElarimShape.BodyMesh(r), meshPrefix + "_Body"), em.stone, root);
            var edges = MeshObject("Edges", SaveMesh(Seven.ElarimShape.EdgeMesh(r, 0.0024f), meshPrefix + "_Edges"), em.edges, root, false);
            var mark = MeshObject("Mark", SaveMesh(Seven.ElarimShape.MarkMesh(r, 0.86f), meshPrefix + "_Mark"), em.mark, root, false);
            var es = root.gameObject.AddComponent<Seven.ElarimShape>();
            es.Edges = edges.GetComponent<Renderer>();
            return es;
        }

        // --------------------------------------------------------------------------- the galley table
        struct TableRefs
        {
            public Transform seat7, seatOther, plate, fork, knife, bowl, moon, outside, porthole;
            public Seven.ElarimShape shape;
            public Vector3 centre, along, outward;
        }

        /// <summary>Galley nook back wall: skip the porthole's opening.</summary>
        static bool PortholeSkip(float a, float y) => Mathf.Abs(Mathf.DeltaAngle(a, TableAngle)) < 1.95f && y > PortholeY - 0.31f && y < PortholeY + 0.31f;

        static TableRefs BuildGalleyTable(Transform nook, ShipMats m, List<Vector3> obstacles)
        {
            var g = new TableRefs();
            var C = Polar(TableR, TableAngle, 0f);
            var outw = Polar(1f, TableAngle, 0f);                                   // toward the porthole
            var along = new Vector3(-Mathf.Sin(TableAngle * Mathf.Deg2Rad), 0f, Mathf.Cos(TableAngle * Mathf.Deg2Rad));   // toward the fridge
            g.centre = C; g.along = along; g.outward = outw;
            var root = new GameObject("GalleyTable").transform; root.SetParent(nook, false);
            // tulip table: flared foot, slim stem, round top with a soft rounded edge
            Obj("Table_Pedestal", Lathe(Resample(new[] { new Vector2(0f, 0f), new Vector2(0.3f, 0f), new Vector2(0.29f, 0.025f), new Vector2(0.1f, 0.09f),
                new Vector2(0.055f, 0.3f), new Vector2(0.06f, 0.6f), new Vector2(0.12f, 0.7f), new Vector2(0f, 0.7f) }, 0.05f), 0f, 360f, 40, null, C), m.pedestal, root);
            Obj("Table_Top", Lathe(new[] { new Vector2(0f, 0.7f), new Vector2(0.44f, 0.7f), new Vector2(0.455f, 0.712f), new Vector2(0.455f, 0.733f), new Vector2(0.44f, TableTopY), new Vector2(0f, TableTopY) }, 0f, 360f, 64, null, C), m.table, root);
            obstacles.Add(new Vector3(C.x, C.z, 0.5f));
            // two tulip seats facing each other across the table: 7 on the fridge side, the empty one opposite
            foreach (var (nm, sgn) in new[] { ("Seat7", 1f), ("SeatOther", -1f) })
            {
                var sp = C + along * SeatOff * sgn;
                var seat = new GameObject(nm).transform; seat.SetParent(root, false);
                seat.position = sp;              // meshes are built around the seat's own origin (no rotation: the back arc is placed by angle)
                Obj(nm + "_Pedestal", Lathe(Resample(new[] { new Vector2(0f, 0f), new Vector2(0.22f, 0f), new Vector2(0.21f, 0.02f), new Vector2(0.06f, 0.08f),
                    new Vector2(0.04f, 0.38f), new Vector2(0.1f, 0.43f), new Vector2(0f, 0.43f) }, 0.05f), 0f, 360f, 32), m.pedestal, seat);
                Obj(nm + "_Shell", Lathe(new[] { new Vector2(0f, 0.42f), new Vector2(0.23f, 0.43f), new Vector2(0.25f, 0.47f), new Vector2(0f, 0.47f) }, 0f, 360f, 40), m.sofaBase, seat);
                Obj(nm + "_Cushion", Lathe(new[] { new Vector2(0f, 0.47f), new Vector2(0.21f, 0.47f), new Vector2(0.19f, 0.5f), new Vector2(0f, 0.505f) }, 0f, 360f, 40), m.fabric, seat);
                // curved back: an arc of the shell behind the sitter, rising and flaring outward
                float back = Mathf.Atan2((along * sgn).z, (along * sgn).x) * Mathf.Rad2Deg;
                Obj(nm + "_Back", Lathe(Resample(new[] { new Vector2(0.21f, 0.47f), new Vector2(0.24f, 0.7f), new Vector2(0.27f, 0.88f), new Vector2(0.245f, 0.9f),
                    new Vector2(0.215f, 0.7f), new Vector2(0.19f, 0.49f) }, 0.04f), back - 62f, back + 62f, 28), m.sofaBase, seat);
                obstacles.Add(new Vector3(sp.x, sp.z, 0.28f));
                if (sgn > 0) g.seat7 = seat; else g.seatOther = seat;
            }
            // 7's plate (a little food), fork + knife; the cat's bowl on the table opposite
            var plateAt = C + along * 0.24f;
            var plate = new GameObject("Plate").transform; plate.SetParent(root, false); plate.position = plateAt;
            Obj("PlateMesh", Lathe(new[] { new Vector2(0f, TableTopY + 0.003f), new Vector2(0.085f, TableTopY + 0.003f), new Vector2(0.125f, TableTopY + 0.024f),
                new Vector2(0.13f, TableTopY + 0.022f), new Vector2(0.12f, TableTopY + 0.016f), new Vector2(0.08f, TableTopY + 0.008f), new Vector2(0f, TableTopY + 0.008f) }, 0f, 360f, 40), m.pot, plate);
            var fv = new List<Vector3>(); var ft = new List<int>();
            var rnd = new System.Random(10);
            for (int i = 0; i < 6; i++)
            {
                float a = (float)rnd.NextDouble() * 360f, rr = 0.015f + 0.045f * (float)rnd.NextDouble();
                Ellipsoid(fv, ft, Polar(rr, a, TableTopY + 0.016f), new Vector3(0.024f, 0.012f, 0.02f) * (0.8f + 0.5f * (float)rnd.NextDouble()), 5, 8);
            }
            Obj("PlateFood", SmoothFinish(fv, ft), m.bench, plate);
            g.plate = plate;
            g.fork = Cutlery("Fork", root, m, true);
            g.knife = Cutlery("Knife", root, m, false);
            var right7 = -outw;      // 7 faces -along: her right hand is on the deck side
            g.fork.position = plateAt + right7 * 0.15f + Vector3.up * (TableTopY + 0.004f);
            g.fork.rotation = Quaternion.LookRotation(-along, Vector3.up);
            g.knife.position = plateAt + right7 * 0.19f + Vector3.up * (TableTopY + 0.004f);
            g.knife.rotation = Quaternion.LookRotation(-along, Vector3.up);
            var bowlAt = C - along * 0.06f;
            var bowl = new GameObject("CatBowl").transform; bowl.SetParent(root, false); bowl.position = bowlAt;
            // a small raised feeder (retro tulip stand, 10 cm) so his head goes into the bowl and the rim hides his mouth
            const float lift = 0.16f; float by = TableTopY + lift;
            Obj("BowlStand", Lathe(new[] { new Vector2(0f, TableTopY), new Vector2(0.06f, TableTopY), new Vector2(0.055f, TableTopY + 0.012f), new Vector2(0.022f, TableTopY + 0.03f),
                new Vector2(0.02f, by - 0.012f), new Vector2(0.05f, by), new Vector2(0f, by) }, 0f, 360f, 32), m.pedestal, bowl);
            Obj("BowlMesh", Lathe(new[] { new Vector2(0f, by), new Vector2(0.058f, by), new Vector2(0.082f, by + 0.048f), new Vector2(0.088f, by + 0.052f),
                new Vector2(0.078f, by + 0.05f), new Vector2(0.055f, by + 0.012f), new Vector2(0f, by + 0.012f) }, 0f, 360f, 36), m.frame, bowl);
            var bf = new List<Vector3>(); var bft = new List<int>();
            Ellipsoid(bf, bft, Vector3.up * (by + 0.02f), new Vector3(0.055f, 0.012f, 0.055f), 5, 12);
            Obj("BowlFood", SmoothFinish(bf, bft), m.bench, bowl);
            g.bowl = bowl;
            // the dodecahedron (placed by Chapter1Aboard: beside her plate / between them)
            g.shape = BuildElarimShape("Dodecahedron", root, ElarimMaterials(true), "Galley_Dodecahedron");
            g.shape.transform.position = plateAt + outw * 0.2f + Vector3.up * (TableTopY + Seven.MoonLayout.ShapeRadius * 0.795f);

            // ---- porthole in the nook wall behind the table, the Moon outside
            var pc = Polar(9.70f, TableAngle, PortholeY);
            var por = new GameObject("Porthole").transform; por.SetParent(nook, false); por.position = pc; por.rotation = Quaternion.LookRotation(outw);
            g.porthole = por;
            var pv = new List<Vector3>(); var pt = new List<int>();
            Vector3 side = along, up = Vector3.up;
            const int N = 48;
            for (int i = 0; i < N; i++)
            {
                float a0 = i * Mathf.PI * 2f / N, a1 = (i + 1) * Mathf.PI * 2f / N;
                Vector3 Dir(float a) => side * Mathf.Cos(a) + up * Mathf.Sin(a);
                Vector3 Sq(float a) { var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); float s = 0.34f / Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)); return pc + (side * d.x + up * d.y) * s; }
                Quad(pv, pt, pc + Dir(a0) * PortholeRad, Sq(a0), Sq(a1), pc + Dir(a1) * PortholeRad);
            }
            Obj("Porthole_Plate", Finish(pv, pt), m.wall, por);
            var sleeve = new List<Vector3>(); var st = new List<int>();
            for (int i = 0; i < N; i++)
            {
                float a0 = i * Mathf.PI * 2f / N, a1 = (i + 1) * Mathf.PI * 2f / N;
                Vector3 Dir(float a) => (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * PortholeRad;
                Quad(sleeve, st, pc + Dir(a0), pc + Dir(a0) + outw * 0.3f, pc + Dir(a1) + outw * 0.3f, pc + Dir(a1));
            }
            Obj("Porthole_Sleeve", Finish(sleeve, st), m.frame, por);
            var rv = new List<Vector3>(); var rt = new List<int>();
            Sweep(rv, rt, Enumerable.Range(0, 48).Select(i => pc - outw * 0.012f + (side * Mathf.Cos(i * Mathf.PI / 24f) + up * Mathf.Sin(i * Mathf.PI / 24f)) * (PortholeRad + 0.012f)).ToList(), 0.022f, true, 8);
            Obj("Porthole_Ring", SmoothFinish(rv, rt), m.rib, por);
            // outside: the dark and the Moon (not dissolvable: Chapter1Aboard hides them during the ship reveal)
            var outside = new GameObject("GalleyOutside").transform; outside.SetParent(nook, false);
            var dark = Lit("Galley_space", Gray(0.0f), 0f);
            dark.SetFloat("_Cull", 0f); dark.doubleSidedGI = true; EditorUtility.SetDirty(dark);       // seen from inside the ship
            // the dark and the Moon sit just behind the porthole, so the fog pass barely touches them
            Obj("Space", Lathe(new[] { new Vector2(10.55f, PortholeY - 1.2f), new Vector2(10.55f, PortholeY + 1.2f) }, TableAngle - 8f, TableAngle + 8f, 16), dark, outside, false);
            var sv2 = new List<Vector3>(); var st2 = new List<int>();
            var srnd = new System.Random(3);
            for (int i = 0; i < 26; i++)
            {
                float a = TableAngle + ((float)srnd.NextDouble() - 0.5f) * 9f, y = PortholeY + ((float)srnd.NextDouble() - 0.5f) * 1.4f, sz = 0.004f + 0.006f * (float)srnd.NextDouble();
                var c = Polar(10.53f, a, y);
                Quad(sv2, st2, c - along * sz - Vector3.up * sz, c - along * sz + Vector3.up * sz, c + along * sz + Vector3.up * sz, c + along * sz - Vector3.up * sz);
            }
            Obj("Stars", Finish(sv2, st2), m.cove, outside, false);
            var moon = GameObject.CreatePrimitive(PrimitiveType.Sphere); moon.name = "Moon";
            Object.DestroyImmediate(moon.GetComponent<Collider>());
            moon.transform.SetParent(outside, false);
            moon.transform.position = Polar(10.35f, TableAngle + 0.35f, PortholeY + 0.04f);
            moon.transform.localScale = Vector3.one * 0.19f;
            moon.transform.rotation = Quaternion.Euler(8f, TableAngle + 120f, 12f);
            var mm = Lit("Galley_moon", Gray(0.7f), 0.05f);
            var mt = MoonTexture();
            mm.SetTexture("_BaseMap", mt); mm.SetTexture("_MainTex", mt);
            Emissive(mm, 1.35f); mm.SetTexture("_EmissionMap", mt);
            moon.GetComponent<MeshRenderer>().sharedMaterial = mm;
            moon.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            g.moon = moon.transform; g.outside = outside;
            // a faint pale light from the porthole onto the table
            var pl = new GameObject("PortholeLight").AddComponent<Light>(); pl.transform.SetParent(por, false);
            pl.transform.position = pc - outw * 0.25f; pl.transform.rotation = Quaternion.LookRotation(-outw + Vector3.down * 0.5f);
            pl.type = LightType.Spot; pl.spotAngle = 70f; pl.range = 3.5f; pl.intensity = 0.7f; pl.shadows = LightShadows.None;
            return g;
        }

        /// <summary>Round 10 camera: the close-up depth-of-field Volume (a profile asset in the scene keeps URP's DOF
        /// shaders in the build).</summary>
        static Seven.CloseUpFocus BuildGalleyFocus()
        {
            const string path = "Assets/Atmosphere/Galley_Focus.asset";
            AssetDatabase.DeleteAsset(path);
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var dof = prof.Add<UnityEngine.Rendering.Universal.DepthOfField>(true);
            dof.mode.Override(UnityEngine.Rendering.Universal.DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(1f); dof.focalLength.Override(70f); dof.aperture.Override(2.2f);
            AssetDatabase.CreateAsset(prof, path);
            AssetDatabase.AddObjectToAsset(dof, prof);
            AssetDatabase.SaveAssets();
            var go = new GameObject("GalleyFocus");
            var v = go.AddComponent<Volume>(); v.isGlobal = true; v.priority = 100f; v.weight = 0f; v.sharedProfile = prof;
            return go.AddComponent<Seven.CloseUpFocus>();
        }

        /// <summary>Round 10: a plain renderer (no SevenHeightFog full-screen pass, no features) for the subtitle overlay
        /// camera, added to both pipeline assets' renderer lists; returns its index (-1 = not available).</summary>
        static int EnsureUiRenderer()
        {
            const string path = "Assets/Settings/UI_Renderer.asset";
            var rd = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(path);
            if (rd == null)
            {
                var pc = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
                rd = ScriptableObject.CreateInstance<UnityEngine.Rendering.Universal.UniversalRendererData>();
                if (pc != null) rd.postProcessData = pc.postProcessData;
                AssetDatabase.CreateAsset(rd, path);
                AssetDatabase.SaveAssets();
            }
            int index = -1;
            foreach (var ap in new[] { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Mobile_RPAsset.asset" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(ap);
                if (asset == null) continue;
                var so = new SerializedObject(asset);
                var list = so.FindProperty("m_RendererDataList");
                int found = -1;
                for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == rd) found = i;
                if (found < 0) { list.InsertArrayElementAtIndex(list.arraySize); found = list.arraySize - 1; list.GetArrayElementAtIndex(found).objectReferenceValue = rd; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(asset); }
                if (index < 0) index = found; else if (index != found) Log($"UI renderer index differs in {ap}: {found} vs {index}");
            }
            AssetDatabase.SaveAssets();
            Log($"UI renderer (no fog pass) at renderer index {index}");
            return index;
        }

        static Transform Cutlery(string name, Transform parent, ShipMats m, bool fork)
        {
            // pivot = where the hand holds it; +Z = toward the tip
            var t = new GameObject(name).transform; t.SetParent(parent, false);
            var v = new List<Vector3>(); var tr = new List<int>();
            AddBox(v, tr, new Vector3(0f, 0f, 0.02f), new Vector3(0.016f, 0.006f, 0.12f));
            if (fork) { AddBox(v, tr, new Vector3(0f, 0f, 0.09f), new Vector3(0.02f, 0.004f, 0.03f)); for (int i = 0; i < 4; i++) AddBox(v, tr, new Vector3(-0.0075f + i * 0.005f, 0f, 0.12f), new Vector3(0.0022f, 0.003f, 0.035f)); }
            else AddBox(v, tr, new Vector3(0f, 0f, 0.115f), new Vector3(0.016f, 0.002f, 0.09f));
            Obj(name + "Mesh", Finish(v, tr), m.rib, t, false);
            return t;
        }

        /// <summary>Grey Moon map (equirectangular): pale highlands, darker maria, soft craters. Generated once.</summary>
        static Texture2D MoonTexture()
        {
            var path = GenDir + "/Galley_moon_map.png";
            var full = Path.Combine(ProjectRoot, path);
            if (!File.Exists(full))
            {
                const int W = 512, H = 256;
                var px = new float[W * H];
                for (int i = 0; i < px.Length; i++) px[i] = 0.82f;
                var rnd = new System.Random(7);
                void Blob(float cx, float cy, float rad, float dv, bool ring)
                {
                    for (int y = Mathf.Max(0, (int)(cy - rad * 1.3f)); y < Mathf.Min(H, (int)(cy + rad * 1.3f)); y++)
                        for (int x = (int)(cx - rad * 1.3f); x < (int)(cx + rad * 1.3f); x++)
                        {
                            int xx = ((x % W) + W) % W;
                            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / rad;
                            float k = ring ? (d < 1f ? -0.6f * (1f - d) : 0f) + Mathf.Exp(-Mathf.Pow((d - 1f) * 5f, 2f)) * 0.7f : Mathf.Clamp01(1f - d * d);
                            px[y * W + xx] += dv * k;
                        }
                }
                for (int i = 0; i < 9; i++) Blob((float)rnd.NextDouble() * W, 60f + (float)rnd.NextDouble() * 130f, 25f + 45f * (float)rnd.NextDouble(), -0.24f, false);
                for (int i = 0; i < 70; i++) Blob((float)rnd.NextDouble() * W, 10f + (float)rnd.NextDouble() * 236f, 2.5f + 10f * (float)rnd.NextDouble(), 0.12f, true);
                var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
                var cols = new Color32[W * H];
                for (int i = 0; i < cols.Length; i++) { byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(px[i] * 255f), 0, 255); cols[i] = new Color32(b, b, b, 255); }
                tex.SetPixels32(cols); tex.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllBytes(full, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
