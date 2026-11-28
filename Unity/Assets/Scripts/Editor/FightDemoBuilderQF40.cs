using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using L = Seven.MoonLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF40 (5): Tunneler established on the Moon. A wall screen in the prison corridor (between the bulkhead and the guarded gate)
    /// shows him the same way Europa's control room does: he stands in a lit "studio" far from the level, filmed by its own camera
    /// into a render texture on an unlit screen (dark = renderer hidden). His drill head is the one shared <see cref="Seven.DrillHead"/>
    /// (spinning with his voice, Chapter1Director.TunnelerTalks); his body is rigged by the shared RigTunnelerBody.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        public static readonly Vector3 Ch1ScreenAt = new Vector3(19.4f, 1.75f, 1.95f);   // centre of the picture, on the corridor's wall face

        static void BuildCh1TunnelerScreen(Seven.Chapter1Director d, Material frameMat)
        {
            var tun = SetupStaticModel("tunneler", "tunneler_v1.fbx");
            var host = new GameObject("QF40_TunnelerScreen").transform;
            var rt = new RenderTexture(640, 400, 16) { name = "Moon_TunnelerScreen" };
            AssetDatabase.DeleteAsset(GenDir + "/Moon_TunnelerScreen.renderTexture");
            AssetDatabase.CreateAsset(rt, GenDir + "/Moon_TunnelerScreen.renderTexture");
            var scrMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Moon_screen_unlit.mat");
            if (scrMat == null) { scrMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(scrMat, MatDir + "/Moon_screen_unlit.mat"); }
            scrMat.shader = Shader.Find("Universal Render Pipeline/Unlit"); scrMat.SetTexture("_BaseMap", rt); scrMat.SetColor("_BaseColor", Color.white); EditorUtility.SetDirty(scrMat);
            var c = Ch1ScreenAt;
            // a riveted housing proud of the rock face, the picture facing the corridor (+z)
            Block("TunScreen_Housing", c + new Vector3(-1.35f, -0.85f, 0f), c + new Vector3(1.35f, 0.85f, 0.12f), frameMat, host, false);
            Block("TunScreen_Hood", c + new Vector3(-1.45f, 0.85f, 0f), c + new Vector3(1.45f, 0.97f, 0.3f), frameMat, host, false);
            Block("TunScreen_Speaker", c + new Vector3(-0.35f, 1.0f, 0f), c + new Vector3(0.35f, 1.25f, 0.22f), frameMat, host, false);
            var scr = Block("TunScreen_Picture", c + new Vector3(-1.2f, -0.72f, 0.12f), c + new Vector3(1.2f, 0.72f, 0.14f), scrMat, host, false);
            var screenR = scr.GetComponent<Renderer>(); screenR.enabled = false;
            var studio = new GameObject("TunnelerStudio_Moon").transform; studio.SetParent(host, false); studio.position = new Vector3(-3000f, 600f, 0f);   // far and high: no fog, never seen
            Transform tunneler = null, drill = null;
            if (tun != null)
            {
                var tm = (GameObject)PrefabUtility.InstantiatePrefab(tun); tm.name = "Tunneler"; tm.transform.SetParent(studio, false); tm.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                tunneler = tm.transform; drill = tm.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "DrillHead");
                var tmat = Dissolve("Moon_tunneler", Gray(0.5f), 0.35f, false); foreach (var r in tm.GetComponentsInChildren<Renderer>()) r.sharedMaterial = tmat;
                RigTunnelerBody(tm, "Chapter1");
                if (drill != null) { var dh = Seven.DrillHead.On(drill, tunneler); dh.Bake(); DrillAxisCheck("Chapter1", dh); }
                tm.SetActive(false);
            }
            else Log("QF40 5: WARNING tunneler model missing - the prison screen stays dark (the line still plays over the speakers)");
            var under = new GameObject("StudioLightFromBelow").AddComponent<Light>(); under.transform.SetParent(studio, false); under.transform.position = studio.position + new Vector3(0f, -0.4f, 1.6f); under.transform.rotation = Quaternion.Euler(-60f, 180f, 0f);
            under.type = LightType.Spot; under.range = 8f; under.spotAngle = 80f; under.intensity = 9f; under.shadows = LightShadows.Soft;
            var scg = new GameObject("ScreenCam"); scg.transform.SetParent(studio, false); scg.transform.position = studio.position + new Vector3(0f, 1.0f, -3.4f); scg.transform.LookAt(studio.position + Vector3.up * 1.7f);
            var cam = scg.AddComponent<Camera>(); cam.targetTexture = rt; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.fieldOfView = 40f; cam.farClipPlane = 20f; cam.enabled = false;
            d.TunScreen = screenR; d.TunScreenCam = cam; d.TunModel = tunneler; d.TunDrill = drill;
            Log($"QF40 5: Tunneler's screen in the prison corridor at {c} ({(tunneler != null ? "his model in the studio" : "NO MODEL")}, drill {(drill != null ? "shared DrillHead" : "missing")})");
        }

        // ------------------------------------------------------------------------------------ QF40 (8): layered rock, not blobs
        /// <summary>A rock face built as strata: horizontal beds 0.45-1.2 m thick, each broken into 1.4-4 m blocks with their own
        /// set-back, a slight turn and battered / chamfered edges, dark crack lines in some bed joints, fallen pieces at its foot. It
        /// replaces the smooth noisy ellipsoids ("Lump") that read as dark potatoes hanging in the air (Dan's QF40 8 screenshot).
        /// faceSign = the direction the face looks along z. Blocks whose centre is between skipX0..skipX1 are left out.</summary>
        static void Strata(List<Vector3> v, List<int> t, List<Vector3> cv, List<int> ct, float x0, float x1, float y0, float y1,
            float faceZ, float depth, int faceSign, int seed, float skipX0 = 1e9f, float skipX1 = -1e9f, bool rubble = true)
        {
            var rnd = new System.Random(seed);
            float y = y0;
            while (y < y1 - 0.2f)
            {
                float h = Mathf.Min(y1 - y, 0.45f + (float)rnd.NextDouble() * 0.75f);
                float x = x0 - (float)rnd.NextDouble() * 1.2f;
                while (x < x1)
                {
                    float w = 1.4f + (float)rnd.NextDouble() * 2.6f, cx = x + w * 0.5f;
                    if (cx < skipX0 || cx > skipX1)
                    {
                        float set = (float)rnd.NextDouble() * 0.55f, d = depth - set;
                        float cz = faceZ - faceSign * set - faceSign * d * 0.5f;           // the block's front at faceZ - faceSign * set
                        MBox(v, t, new Vector3(cx, y + h * 0.5f, cz), new Vector3(w - 0.06f, h - 0.04f, d), ((float)rnd.NextDouble() - 0.5f) * 4f, 0.03f);
                        if (rnd.NextDouble() < 0.5) AddBox(cv, ct, new Vector3(cx, y + h - 0.02f, faceZ - faceSign * (set - 0.03f)), new Vector3(w * 0.8f, 0.035f, 0.05f));
                    }
                    x += w;
                }
                y += h;
            }
            if (!rubble) return;
            for (float x = x0; x < x1; x += 1.1f + (float)rnd.NextDouble() * 1.6f)       // fallen pieces lying at the foot of the face
            {
                if (x > skipX0 && x < skipX1) continue;
                float s = 0.25f + (float)rnd.NextDouble() * 0.45f;
                MBox(v, t, new Vector3(x, y0 + s * 0.5f, faceZ + faceSign * (0.15f + s * 0.5f + (float)rnd.NextDouble() * 0.6f)), new Vector3(s * 1.4f, s, s), (float)rnd.NextDouble() * 80f, 0.15f);
            }
        }

        /// <summary>QF40 (8) + (10): the impound bay. The BayCliff blobs become strata walls; the clamp release lever moves out of
        /// reach onto a gantry over the bay (6.6 m up: nobody can reach it, telekinesis is the only way): a big lever in a lit housing
        /// with a bright white handle and a banded warning plate, its cables running down the gantry leg and along the floor to every
        /// clamp. Ore skips (riveted steel buckets with rock inside) ride an overhead rail across the bay on trolleys, hung by chains,
        /// lit by the rail's lamps - the "mining carts" the cat jokes about (s07_03).</summary>
        static void BuildBayQF40(Transform root, MoonMats m, Props props)
        {
            var v = new List<Vector3>(); var t = new List<int>(); var cv = new List<Vector3>(); var ct = new List<int>();
            Strata(v, t, cv, ct, -4f, 34f, L.Bay, L.Lower - 0.6f, L.LedgeMin.y - 0.3f, 2.4f, -1, 321, 19.6f, 23.4f);              // below the ledge (the ramp gap open)
            Strata(v, t, cv, ct, -14f, 40f, L.Lower + 9f, L.Lower + 18f, L.LedgeMax.y + 1f, 3f, -1, 421, 1e9f, -1e9f, false);    // high above the ledge
            Strata(v, t, cv, ct, -14f, L.LedgeMin.x - 0.5f, L.Bay, L.Lower + 9f, L.LedgeMax.y + 1f, 3f, -1, 521, 1e9f, -1e9f, false);
            Strata(v, t, cv, ct, L.LedgeMax.x + 0.5f, 40f, L.Bay, L.Lower + 9f, L.LedgeMax.y + 1f, 3f, -1, 621, 1e9f, -1e9f, false);
            MeshObject("BayCliff", SaveMesh(Finish(v, t), "Moon_BayCliff"), m.rock, root);
            MeshObject("BayCliffCracks", SaveMesh(Finish(cv, ct), "Moon_BayCliffCracks"), m.black, root, false);

            // ---- the release gantry (QF40 10)
            var lv = L.Lever;                                   // the housing's base
            var g = new List<Vector3>(); var gt = new List<int>();
            foreach (var gx in new[] { -1.8f, 1.8f })
            {
                AddBox(g, gt, new Vector3(lv.x + gx, (L.Bay + lv.y) * 0.5f, lv.z), new Vector3(0.32f, lv.y - L.Bay, 0.32f));            // smooth legs: no handholds
                AddBox(g, gt, new Vector3(lv.x + gx, L.Bay + 0.12f, lv.z), new Vector3(1.0f, 0.24f, 1.0f));
                props.extraObstacles.Add(new Vector3(lv.x + gx, lv.z, 0.55f));
            }
            AddBox(g, gt, new Vector3(lv.x, lv.y - 0.2f, lv.z), new Vector3(4.6f, 0.4f, 0.7f));                                     // the girder
            for (int i = 0; i < 6; i++) AddBox(g, gt, new Vector3(lv.x - 1.8f + i * 0.72f, lv.y - 1.2f, lv.z), new Vector3(0.08f, 1.7f, 0.08f));   // truss struts
            AddBox(g, gt, new Vector3(lv.x, lv.y + 0.7f, lv.z), new Vector3(1.5f, 1.4f, 0.9f));                                     // the housing
            AddBox(g, gt, new Vector3(lv.x, lv.y + 1.5f, lv.z - 0.1f), new Vector3(1.8f, 0.14f, 1.2f));                              // its hood
            MeshObject("ReleaseGantry", SaveMesh(Finish(g, gt), "Moon_ReleaseGantry"), m.metalDark, root);
            Block("ReleaseWarningPlate", lv + new Vector3(-0.55f, 0.15f, 0.45f), lv + new Vector3(0.55f, 0.45f, 0.47f), m.glow, root, false);
            for (int i = 0; i < 4; i++) Block($"ReleaseWarningBand{i}", lv + new Vector3(-0.5f + i * 0.28f, 0.17f, 0.47f), lv + new Vector3(-0.38f + i * 0.28f, 0.43f, 0.48f), m.black, root, false);
            Lamp("ReleaseLamp", lv + new Vector3(0f, 1.38f, 0.35f), 9f, 2.6f, m.glow, root, new Vector3(1.0f, 0.06f, 0.2f));
            var lever = new GameObject("Lever").transform; lever.SetParent(root, false);
            lever.position = lv + new Vector3(0f, 1.0f, 0.5f);
            lever.rotation = Quaternion.Euler(-35f, 0f, 0f);
            var lgv = new List<Vector3>(); var lgt = new List<int>();
            Sweep(lgv, lgt, new[] { Vector3.zero, Vector3.up * 1.1f }, 0.07f, false, 8);
            MeshObject("LeverArm", SaveMesh(SmoothFinish(lgv, lgt), "Moon_LeverArm"), m.metal, lever);
            var lhv = new List<Vector3>(); var lht = new List<int>();
            Sweep(lhv, lht, new[] { Vector3.up * 0.95f + Vector3.left * 0.28f, Vector3.up * 0.95f + Vector3.right * 0.28f }, 0.09f, false, 10);
            MeshObject("LeverHandle", SaveMesh(SmoothFinish(lhv, lht), "Moon_Lever"), m.glow, lever);
            props.lever = lever;
            PointLight("ConsoleLight", lv + new Vector3(0f, 2.2f, 1.4f), 6f, 1.4f, root);
            var cb = new List<Vector3>(); var cbt = new List<int>();
            var legFoot = new Vector3(lv.x - 1.8f, L.Bay + 0.06f, lv.z + 0.25f);
            Sweep(cb, cbt, new[] { lv + new Vector3(-0.75f, 0.5f, 0.25f), lv + new Vector3(-1.8f, 0.1f, 0.25f), new Vector3(lv.x - 1.8f, L.Bay + 0.6f, lv.z + 0.25f), legFoot }, 0.06f, false, 6);
            foreach (var a in new[] { 45f, 135f, 225f, 315f })
            {
                var cp = L.SaucerCentre + Polar(6.6f, a, 0f) + Vector3.up * 0.06f;
                var mid = Vector3.Lerp(legFoot, cp, 0.5f); mid.y = L.Bay + 0.06f;
                Sweep(cb, cbt, new[] { legFoot, mid, cp, cp + Vector3.up * 0.9f }, 0.045f, false, 6);
            }
            MeshObject("ReleaseCables", SaveMesh(SmoothFinish(cb, cbt), "Moon_ReleaseCables"), m.metalDark, root, false);

            // ---- the ore skips on the overhead rail (QF40 8 / 9)
            float ry = L.Bay + 13f, rz = -64f;
            var rv = new List<Vector3>(); var rtt = new List<int>();
            AddBox(rv, rtt, new Vector3(15f, ry, rz), new Vector3(44f, 0.22f, 0.3f));
            AddBox(rv, rtt, new Vector3(15f, ry, rz + 1.4f), new Vector3(44f, 0.22f, 0.3f));
            foreach (var px in new[] { -6.5f, 36.5f }) { AddBox(rv, rtt, new Vector3(px, (L.Bay + ry) * 0.5f, rz + 0.7f), new Vector3(0.5f, ry - L.Bay, 0.5f)); AddBox(rv, rtt, new Vector3(px, ry, rz + 0.7f), new Vector3(0.7f, 0.5f, 2.0f)); }
            for (float hx = 1f; hx < 30f; hx += 9.5f) AddBox(rv, rtt, new Vector3(hx, ry + 6f, rz + 0.7f), new Vector3(0.12f, 12f, 0.12f));     // hangers up into the dark
            MeshObject("OreRail", SaveMesh(Finish(rv, rtt), "Moon_OreRail"), m.metalDark, root);
            for (float lx = 2f; lx < 32f; lx += 10f) Lamp($"OreRailLamp{lx}", new Vector3(lx, ry - 0.3f, rz + 0.7f), 10f, 1.4f, m.glow, root, new Vector3(0.4f, 0.08f, 0.4f));
            var skips = new List<Transform>();
            for (int i = 0; i < 6; i++)
            {
                var sk = new GameObject("OreSkip" + i).transform; sk.SetParent(root, false);
                var sv = new List<Vector3>(); var st = new List<int>();
                AddBox(sv, st, new Vector3(0f, 0.12f, 0f), new Vector3(0.7f, 0.24f, 0.4f));                      // trolley
                foreach (var cx in new[] { -0.2f, 0.2f }) Sweep(sv, st, new[] { new Vector3(cx, 0f, 0f), new Vector3(cx * 3.2f, -1.4f, 0f) }, 0.025f, false, 5);   // chains
                AddBox(sv, st, new Vector3(0f, -1.45f, 0f), new Vector3(1.6f, 0.1f, 0.1f));                        // the bail
                MBox(sv, st, new Vector3(0f, -2.55f, 0f), new Vector3(1.5f, 1.0f, 1.1f), 0f, -0.12f);             // the bucket (wider at the top)
                for (int r = 0; r < 3; r++) AddBox(sv, st, new Vector3(0f, -2.25f - r * 0.3f, 0.6f), new Vector3(1.58f, 0.05f, 0.04f));   // riveted bands
                MeshObject("Skip", SaveMesh(Finish(sv, st), "Moon_OreSkip" + i), m.metal, sk);
                var ov = new List<Vector3>(); var ot = new List<int>();
                for (int k = 0; k < 5; k++) MBox(ov, ot, new Vector3(-0.45f + k * 0.22f, -1.62f + (k % 2) * 0.05f, ((k * 37) % 5 - 2) * 0.08f), new Vector3(0.38f, 0.26f, 0.34f), k * 33f, 0.2f);   // rock heaped inside
                MeshObject("Ore", SaveMesh(Finish(ov, ot), "Moon_OreLoad" + i), m.rock, sk, false);
                skips.Add(sk);
            }
            var mot = root.gameObject.AddComponent<Seven.CityMotion>();
            mot.Conveyors = new[] { new Seven.CityMotion.Conveyor { Path = new[] { new Vector3(-5f, ry - 0.11f, rz), new Vector3(35f, ry - 0.11f, rz), new Vector3(35f, ry - 0.11f, rz + 1.4f), new Vector3(-5f, ry - 0.11f, rz + 1.4f), new Vector3(-5f, ry - 0.11f, rz) }, Carts = skips.ToArray(), Speed = 1.6f } };
            Log("QF40 (8/10): the bay - strata walls (no blobs), the release lever on its gantry with cables to the four clamps, 6 ore skips on the overhead rail");
        }

        /// <summary>QF40 (8): the prison face's crown (was "CliffAbove", ellipsoid lumps against the haze) as strata.</summary>
        static void PrisonCrownStrata(Transform root, MoonMats m, float zf1)
        {
            var v = new List<Vector3>(); var t = new List<int>(); var cv = new List<Vector3>(); var ct = new List<int>();
            Strata(v, t, cv, ct, -10f, 36f, 7f, 11.5f, zf1, 3.2f, 1, 1150, 1e9f, -1e9f, false);
            MeshObject("CliffAbove", SaveMesh(Finish(v, t), "Moon_CliffAbove"), m.rock, root);
            MeshObject("CliffAboveCracks", SaveMesh(Finish(cv, ct), "Moon_CliffAboveCracks"), m.black, root, false);
        }
    }
}
