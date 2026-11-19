using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 12 (2f, Dan: "the saucer looks different in Chapter 1 and Venus") — ONE saucer exterior for every place the
    /// ship is seen from outside: Chapter 1's impound bay / liftoff and the Venus landing (and later worlds). Chapter 1's
    /// look is the reference (Round 10): the ~12 m wide flat disc, polished silver hull, glowing rim band, low dome, the
    /// darker under-band and seam lines, the belly glow ring, the realtime reflection probe and the fill light; the belly
    /// iris + tractor beam stay in Chapter 1's bay builder (they hang off this root). Added: Venus's storage hatch (QF10) -
    /// a seamless panel in the hull toward the bike bay (the lathes skip its sector and the panel is the same surface, so
    /// closed it is invisible; its seam glow is off), the bay behind it (floor, ceiling, sides, a softly glowing back wall,
    /// a light). Built in the saucer's local frame (origin on the ground under its centre, the underside b0 above it).
    /// </summary>
    public static partial class FightDemoBuilder
    {
        public const float SaucerExtRadius = 6f;
        public const float SaucerHatchAngle = 41.25f, SaucerHatchSpan = 30f;     // edges on the 96-segment lathe (3.75 deg)
        const float BayInner = BayNotch;                                          // QF14 (3): the hatch panel covers r 3.45 .. rim only (the room is inside)

        static string ExtSfx = "";
        class SaucerExt { public Transform Panel, Sink; public Renderer Seam, BayBack; public Light BayLight, UnderLight; public Light[] BayLights; public ReflectionProbe BayProbe; public readonly List<Renderer> Glow = new List<Renderer>(); }

        /// <summary>Round 13 (0b, the saucer's fixed design): the tractor beam is how 7 gets in and out of the saucer on
        /// every world - an iris hatch in the belly (8 blades), its glow disc, a soft cone of light down to the ground, the
        /// floor glow and a spot light. Shared by Chapter 1 and Venus; driven at runtime by <see cref="Seven.SaucerBeam"/>.
        /// b0 = the hull underside (saucer-local), beamLen = from the hatch down to the ground.</summary>
        static Seven.SaucerBeam BuildSaucerBeam(Transform saucer, float b0, float beamLen, float hatchRadius, float floorRadius, Material metalDark, Material rim, Material field)
        {
            var hatch = new GameObject("Hatch").transform; hatch.SetParent(saucer, false); hatch.localPosition = new Vector3(0f, b0 - 0.105f, 0f);
            for (int i = 0; i < 8; i++)
            {
                var blade = new GameObject("Blade" + i).transform; blade.SetParent(hatch, false);
                blade.localRotation = Quaternion.Euler(0f, i * 45f, 0f);
                var hv = new List<Vector3>(); var ht = new List<int>();
                var c0 = Vector3.zero; var c1 = Polar(hatchRadius + 0.05f, -26f, 0f); var c2 = Polar(hatchRadius + 0.05f, 26f, 0f);
                Quad(hv, ht, c0, c2, c1, c0); Quad(hv, ht, c0, c1, c2, c0);
                MeshObject("BladeMesh", SaveMesh(Finish(hv, ht), "Saucer_HatchBlade" + i), metalDark, blade, false);
            }
            var hg = new List<Vector3>(); var hgt = new List<int>();
            Ellipsoid(hg, hgt, Vector3.zero, new Vector3(hatchRadius, 0.04f, hatchRadius), 6, 24);
            var hatchGlow = MeshObject("HatchGlow", SaveMesh(SmoothFinish(hg, hgt), "Saucer_HatchGlow"), rim, saucer, false).GetComponent<Renderer>();
            hatchGlow.transform.localPosition = new Vector3(0f, b0 - 0.02f, 0f);
            var beam = new GameObject("TractorBeam").transform; beam.SetParent(saucer, false); beam.localPosition = new Vector3(0f, b0 - 0.1f, 0f);
            string sfx = beamLen.ToString("0.00").Replace('.', '_');
            var cone = Lathe(new[] { new Vector2(hatchRadius * 0.95f, 0f), new Vector2(floorRadius, -beamLen) }, 0f, 360f, 48);
            MeshObject("Cone", SaveMesh(Colored(cone, pp => Mathf.Lerp(0.42f, 0.1f, Mathf.Clamp01(-pp.y / beamLen))), "Saucer_BeamCone_" + sfx), field, beam, false);
            var inner = Lathe(new[] { new Vector2(hatchRadius * 0.55f, 0f), new Vector2(floorRadius * 0.6f, -beamLen) }, 0f, 360f, 32);
            MeshObject("ConeCore", SaveMesh(Colored(inner, pp => Mathf.Lerp(0.3f, 0.06f, Mathf.Clamp01(-pp.y / beamLen))), "Saucer_BeamCore_" + sfx), field, beam, false);
            var fv = new List<Vector3>(); var ft = new List<int>();
            for (int k = 0; k < 48; k++)
            {
                var a = Polar(floorRadius * 1.25f, k * 7.5f, 0f); var b = Polar(floorRadius * 1.25f, (k + 1) * 7.5f, 0f);
                Quad(fv, ft, Vector3.zero, b, a, Vector3.zero);
            }
            var floorMesh = Finish(fv, ft);
            floorMesh.colors = floorMesh.vertices.Select(pp => new Color(1f, 1f, 1f, Mathf.Lerp(0.45f, 0f, pp.magnitude / (floorRadius * 1.25f)))).ToArray();
            var floorGo = MeshObject("BeamFloor", SaveMesh(floorMesh, "Saucer_BeamFloor_" + sfx), field, beam, false);
            floorGo.transform.localPosition = new Vector3(0f, -beamLen + 0.03f, 0f);
            var bl = new GameObject("BeamLight").AddComponent<Light>();
            bl.transform.SetParent(beam, false); bl.transform.localPosition = new Vector3(0f, -0.2f, 0f); bl.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            bl.type = LightType.Spot; bl.spotAngle = 60f; bl.range = beamLen + 6f; bl.intensity = 0f; bl.shadows = LightShadows.None;
            beam.gameObject.SetActive(false);
            var sb = saucer.gameObject.AddComponent<Seven.SaucerBeam>();
            sb.Hatch = hatch; sb.Beam = beam; sb.BeamLight = bl; sb.BeamFloor = floorGo.GetComponent<Renderer>(); sb.HatchGlow = hatchGlow;
            sb.HatchRadius = hatchRadius; sb.BellyY = b0;
            return sb;
        }

        static SaucerExt BuildSaucerExterior(Transform saucer, float b0, Material hull, Material hullUnder, Material metalDark, Material rim)
        {
            // QF12 (2): the hull meshes are built at this world's b0 (Moon 1.15, Venus 3.35) - their assets need their own
            // names, or the last world built overwrites the others' (the Moon hull floated 2.2 m above its beam hatch)
            ExtSfx = "_" + b0.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).Replace('.', '_');
            var ext = new SaucerExt();
            float ha0 = SaucerHatchAngle - SaucerHatchSpan * 0.5f, ha1 = SaucerHatchAngle + SaucerHatchSpan * 0.5f;
            bool InHatch(float a, float y) { a = Mathf.Repeat(a, 360f); return a > ha0 + 0.01f && a < ha1 - 0.01f; }
            bool InSector(float a) { a = Mathf.Repeat(a, 360f); return a >= ha0 - 0.01f && a <= ha1 + 0.01f; }
            var under = new[] { new Vector2(0f, b0 - 0.1f), new Vector2(1.6f, b0 - 0.08f), new Vector2(3.4f, b0 + 0.02f), new Vector2(5.0f, b0 + 0.22f), new Vector2(5.75f, b0 + 0.42f), new Vector2(6.0f, b0 + 0.56f) };
            var upper = new[] { new Vector2(6.0f, b0 + 0.82f), new Vector2(5.6f, b0 + 0.98f), new Vector2(4.4f, b0 + 1.22f), new Vector2(3.0f, b0 + 1.45f), new Vector2(1.9f, b0 + 1.75f), new Vector2(1.1f, b0 + 1.93f), new Vector2(0f, b0 + 2.0f) };
            var rimBand = new[] { new Vector2(6.0f, b0 + 0.56f), new Vector2(6.04f, b0 + 0.6f), new Vector2(6.04f, b0 + 0.78f), new Vector2(6.0f, b0 + 0.82f) };
            float UnderY(float r) { for (int i = 0; i < under.Length - 1; i++) if (r <= under[i + 1].x) return Mathf.Lerp(under[i].y, under[i + 1].y, Mathf.InverseLerp(under[i].x, under[i + 1].x, r)); return under[under.Length - 1].y; }
            float UpperY(float r) { for (int i = 0; i < upper.Length - 1; i++) if (r >= upper[i + 1].x) return Mathf.Lerp(upper[i].y, upper[i + 1].y, Mathf.InverseLerp(upper[i].x, upper[i + 1].x, r)); return upper[upper.Length - 1].y; }
            // the hull minus the hatch sector (outer part only: the panel covers r BayInner .. rim)
            var underR = Resample(under, 0.25f); var upperR = Resample(upper, 0.2f);
            var underIn = underR.Where(p => p.x <= BayInner + 1e-3f).ToList(); var underOut = underR.Where(p => p.x >= BayInner - 1e-3f).ToList();
            var upperIn = upperR.Where(p => p.x <= BayInner + 1e-3f).ToList(); var upperOut = upperR.Where(p => p.x >= BayInner - 1e-3f).ToList();
            if (underIn.Count < 2 || underIn[underIn.Count - 1].x < BayInner) underIn.Add(new Vector2(BayInner, UnderY(BayInner)));
            if (underOut.Count < 2 || underOut[0].x > BayInner) underOut.Insert(0, new Vector2(BayInner, UnderY(BayInner)));
            if (upperIn.Count < 2 || upperIn[0].x < BayInner) upperIn.Insert(0, new Vector2(BayInner, UpperY(BayInner)));
            if (upperOut.Count < 2 || upperOut[upperOut.Count - 1].x > BayInner) upperOut.Add(new Vector2(BayInner, UpperY(BayInner)));
            MeshObject("Hull_Under", SaveMesh(Lathe(underIn, 0f, 360f, 96), "Saucer_Ext_UnderIn" + ExtSfx), hullUnder, saucer);
            MeshObject("Hull_UnderOut", SaveMesh(Lathe(underOut, 0f, 360f, 96, InHatch), "Saucer_Ext_Under" + ExtSfx), hullUnder, saucer);
            MeshObject("Hull_Upper", SaveMesh(Lathe(upperOut, 0f, 360f, 96, InHatch), "Saucer_Ext_Upper" + ExtSfx), hull, saucer);
            MeshObject("Hull_UpperIn", SaveMesh(Lathe(upperIn, 0f, 360f, 96), "Saucer_Ext_UpperIn" + ExtSfx), hull, saucer);
            MeshObject("Hull_RimBand", SaveMesh(Lathe(rimBand, 0f, 360f, 96, InHatch), "Saucer_Ext_RimBand" + ExtSfx), hullUnder, saucer);
            var bandV = new List<Vector2>(); for (float r = 4.75f; r <= 5.98f; r += 0.08f) bandV.Add(new Vector2(r, UnderY(r) - 0.012f));
            MeshObject("Hull_UnderBand", SaveMesh(Lathe(bandV, 0f, 360f, 96, InHatch), "Saucer_Ext_UnderBand" + ExtSfx), metalDark, saucer);
            // seams (concentric rings + radial panel seams under, two fine rings over) - broken where the hatch panel is
            {
                var sv = new List<Vector3>(); var st = new List<int>();
                void Ring(float rr, float y0, float rad) { var run = new List<Vector3>(); for (int i = 0; i <= 96; i++) { float a = i * 3.75f; if (InSector(a) && rr > BayInner) { if (run.Count > 1) Sweep(sv, st, run, rad, false, 5); run = new List<Vector3>(); continue; } run.Add(Polar(rr, a, y0)); } if (run.Count > 1) Sweep(sv, st, run, rad, false, 5); }
                foreach (var rr in new[] { 1.65f, 2.9f, 3.9f, 4.7f }) Ring(rr, UnderY(rr) - 0.014f, 0.016f);
                for (int k = 0; k < 16; k++) { float a = k * 22.5f; if (InSector(a)) continue; Sweep(sv, st, Enumerable.Range(0, 9).Select(i => { float rr = Mathf.Lerp(1.65f, 4.7f, i / 8f); return Polar(rr, a, UnderY(rr) - 0.014f); }).ToList(), 0.012f, false, 5); }
                foreach (var rr in new[] { 2.4f, 4.9f }) Ring(rr, UpperY(rr) + 0.006f, 0.008f);
                MeshObject("Hull_Seams", SaveMesh(SmoothFinish(sv, st), "Saucer_Ext_Seams" + ExtSfx), metalDark, saucer, false);
            }
            var gv = new List<Vector3>(); var gt = new List<int>();
            { var run = new List<Vector3>(); for (int i = 0; i <= 120; i++) { float a = i * 3f; if (InSector(a)) { if (run.Count > 1) Sweep(gv, gt, run, 0.05f, false, 8); run = new List<Vector3>(); continue; } run.Add(Polar(6.07f, a, b0 + 0.69f)); } if (run.Count > 1) Sweep(gv, gt, run, 0.05f, false, 8); }
            ext.Glow.Add(MeshObject("RimGlow", SaveMesh(SmoothFinish(gv, gt), "Saucer_Ext_RimGlow" + ExtSfx), rim, saucer, false).GetComponent<Renderer>());
            var bv = new List<Vector3>(); var bt = new List<int>();
            // QF13 (1): the belly ring's arc under the hold rides on the hatch panel (else it crossed the open hold's bottom
            // as a white line)
            Sweep(bv, bt, Enumerable.Range(0, 96).Select(i => Polar(2.4f, i * 3.75f, b0 - 0.07f)).ToList(), 0.04f, true, 6);      // QF14: r 2.4 is no longer on the panel
            Sweep(bv, bt, Enumerable.Range(0, 48).Select(i => Polar(1.2f, i * 7.5f, b0 + 1.97f)).ToList(), 0.025f, true, 6);
            ext.Glow.Add(MeshObject("BellyGlow", SaveMesh(SmoothFinish(bv, bt), "Saucer_Ext_BellyGlow" + ExtSfx), rim, saucer, false).GetComponent<Renderer>());
            var ul = new GameObject("UnderLight").AddComponent<Light>();
            ul.transform.SetParent(saucer, false); ul.transform.localPosition = new Vector3(0f, b0 - 0.3f, 0f); ul.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ul.type = LightType.Spot; ul.spotAngle = 120f; ul.range = 8f; ul.intensity = 0.8f; ul.shadows = LightShadows.None;
            ext.UnderLight = ul;
            var probeGo = new GameObject("SaucerReflections"); probeGo.transform.SetParent(saucer, false); probeGo.transform.localPosition = new Vector3(0f, b0 + 1.0f, 0f);
            var rp = probeGo.AddComponent<ReflectionProbe>();
            rp.mode = ReflectionProbeMode.Realtime; rp.refreshMode = ReflectionProbeRefreshMode.ViaScripting; rp.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            rp.size = new Vector3(16f, 8f, 16f); rp.resolution = 128; rp.hdr = true; rp.importance = 2;
            rp.nearClipPlane = 0.3f; rp.farClipPlane = 45f; rp.intensity = 1.35f;
            rp.clearFlags = ReflectionProbeClearFlags.SolidColor; rp.backgroundColor = Gray(0.82f);
            rp.cullingMask = ~((1 << 20) | (1 << Seven.SaucerProbe.Layer));
            probeGo.AddComponent<Seven.SaucerProbe>();
            var fill = new GameObject("HullFill").AddComponent<Light>(); fill.transform.SetParent(saucer, false);
            fill.transform.localPosition = new Vector3(0f, b0 - 2.2f, 0f); fill.type = LightType.Point; fill.range = 9f; fill.intensity = 1.1f; fill.shadows = LightShadows.None;

            // ---- the storage hatch panel (the same surfaces over the hatch sector; it slides round under the hull to open)
            // QF13 (1): the panel's pivot is on the hull's mid-plane (rim height), so the sink (a uniform scale) draws every
            // surface INTO the hull - scaled about the ground it hung 5 cm below the neighbouring hull once slid round
            var panel = new GameObject("HatchPanel").transform; panel.SetParent(saucer, false); panel.localPosition = new Vector3(0f, b0 + 0.69f, 0f);
            // QF14 (3): the sink scales about the panel's inner edge (r = BayInner on the hatch axis), so the panel never moves
            // inward into the room's frame; it still goes ~8 cm inside the hull at the rim before it slides round
            var sink = new GameObject("PanelSink").transform; sink.SetParent(panel, false); sink.localPosition = Polar(BayInner, SaucerHatchAngle, 0f);
            var pm = new GameObject("PanelMeshes").transform; pm.SetParent(sink, false); pm.localPosition = -Polar(BayInner, SaucerHatchAngle, 0f) - new Vector3(0f, b0 + 0.69f, 0f);
            MeshObject("PanelUnder", SaveMesh(Lathe(underOut, ha0, ha1, 8), "Saucer_Ext_PanelUnder" + ExtSfx), hullUnder, pm);
            MeshObject("PanelUpper", SaveMesh(Lathe(upperOut, ha0, ha1, 8), "Saucer_Ext_PanelUpper" + ExtSfx), hull, pm);
            MeshObject("PanelRim", SaveMesh(Lathe(rimBand, ha0, ha1, 8), "Saucer_Ext_PanelRim" + ExtSfx), hullUnder, pm);
            MeshObject("PanelBand", SaveMesh(Lathe(bandV, ha0, ha1, 8), "Saucer_Ext_PanelBand" + ExtSfx), metalDark, pm);
            ext.Panel = panel; ext.Sink = sink;
            // the seam: hair-thin lines round the panel (white emission animated by the director; off when closed)
            var seamMat = Dissolve("Saucer_hatch_seam", Gray(0.85f), 0.6f, false); seamMat.EnableKeyword("_EMISSION"); seamMat.SetColor("_EmissionColor", Color.black); EditorUtility.SetDirty(seamMat);
            {
                var sv = new List<Vector3>(); var st = new List<int>();
                foreach (var a in new[] { ha0, ha1 })
                {
                    var edge = new List<Vector3>();
                    foreach (var p in upperOut.AsEnumerable().Reverse()) edge.Add(Polar(p.x + 0.004f, a, p.y + 0.004f));
                    foreach (var p in rimBand) edge.Add(Polar(p.x + 0.006f, a, p.y));
                    foreach (var p in underOut.AsEnumerable().Reverse()) edge.Add(Polar(p.x + 0.004f, a, p.y - 0.004f));
                    Sweep(sv, st, edge, 0.012f, false, 5);
                }
                Sweep(sv, st, Enumerable.Range(0, 9).Select(i => Polar(BayInner, Mathf.Lerp(ha0, ha1, i / 8f), UpperY(BayInner) + 0.006f)).ToList(), 0.012f, false, 5);
                Sweep(sv, st, Enumerable.Range(0, 9).Select(i => Polar(BayInner, Mathf.Lerp(ha0, ha1, i / 8f), UnderY(BayInner) - 0.006f)).ToList(), 0.012f, false, 5);
                var seam = MeshObject("HatchSeam", SaveMesh(SmoothFinish(sv, st), "Saucer_Ext_HatchSeam" + ExtSfx), seamMat, saucer, false);
                ext.Seam = seam.GetComponent<Renderer>(); ext.Seam.enabled = false;
            }
            // the bay behind it. History: R13 dark gray (two black bands), QF12 pale glow (white bands), QF13 flat unlit
            // (Dan, 0.14.1: "that awful gray mush" - a flat slab the bike poked through: the old bay was the hull sector
            // itself, 0.26 m thick at the rim). QF14 (3): a real room inside the hull's thick middle (FightDemoBuilderBikeBay),
            // and in front of it a full-height slot through the hull (its two cut faces), open top and bottom once the panel
            // has slid away, so the 0.95 m bike glides out without touching anything.
            {
                var bay = new GameObject("Bay").transform; bay.SetParent(saucer, false);
                bay.localRotation = Quaternion.LookRotation(Polar(1f, SaucerHatchAngle, 0f), Vector3.up);
                BuildBikeBay(bay, b0, UnderY, UpperY, SaucerHatchSpan * 0.5f, hull, metalDark, ext);
                var slot = new GameObject("BaySlot").transform; slot.SetParent(saucer, false);
                foreach (var a in new[] { ha0, ha1 })
                {
                    var wv = new List<Vector3>(); var wt = new List<int>();
                    for (float r = BayInner; r < 6.0f - 1e-3f; r += 0.1f)
                    {
                        float r2 = Mathf.Min(6.0f, r + 0.1f); int b = wv.Count;
                        wv.Add(Polar(r, a, UnderY(r) + 0.002f)); wv.Add(Polar(r2, a, UnderY(r2) + 0.002f)); wv.Add(Polar(r2, a, UpperY(r2) - 0.002f)); wv.Add(Polar(r, a, UpperY(r) - 0.002f));
                        wt.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
                    }
                    // the rim band's own thickness at the rim edge
                    { int b = wv.Count; wv.Add(Polar(6.0f, a, b0 + 0.56f)); wv.Add(Polar(6.04f, a, b0 + 0.6f)); wv.Add(Polar(6.04f, a, b0 + 0.78f)); wv.Add(Polar(6.0f, a, b0 + 0.82f)); wt.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 }); }
                    MeshObject($"BaySide{a:0}", SaveMesh(Finish(wv, wt), $"Saucer_Ext_BaySide{a:0}" + ExtSfx), hullUnder, slot, false);
                }
            }
            var hold = saucer.gameObject.AddComponent<Seven.SaucerHold>();
            hold.Panel = panel; hold.Sink = ext.Sink; hold.Seam = ext.Seam; hold.BayLight = ext.BayLight; hold.BayLights = ext.BayLights; hold.BayProbe = ext.BayProbe;
            return ext;
        }

        static Material HoldInteriorMat()
        {
            var path = MatDir + "/Saucer_hold_interior.mat";
            var sh = Shader.Find("Seven/HoldInterior");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh; m.SetFloat("_Gray", 0.2f); EditorUtility.SetDirty(m);       // a shadowed recess: reads as an opening, not a panel
            return m;
        }

        // ------------------------------------------------------------------------------------------ QF13 (16): ONE saucer
        // Dan saw different saucers on different worlds: every scene built its own (its own hover height baked into the
        // meshes, its own materials). Now the ship is built ONCE per build into Assets/Saucer/Saucer.prefab (exterior, belly
        // iris + beam, the bike hold + its seamless panel) and every scene places an unchanged instance of it; only where it
        // hovers may differ. CheckSaucer fails the build if a scene's saucer differs from the prefab.
        const string SaucerPrefabPath = "Assets/Saucer/Saucer.prefab";
        static GameObject _saucerPrefab;
        static readonly List<string> SaucerChecks = new List<string>();

        static GameObject SaucerPrefab()
        {
            if (_saucerPrefab != null) return _saucerPrefab;
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(ProjectRoot, "Assets/Saucer"));
            var hull = Dissolve("Saucer_hull", Gray(0.95f), 0.9f, false, 0.6f);
            var hullUnder = Dissolve("Saucer_under", Gray(0.74f), 0.8f, false, 0.45f);
            var rim = Dissolve("Saucer_rim", Color.white, 0.6f, false);
            var metalDark = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Moon_metal_dark.mat") ?? Dissolve("Moon_metal_dark", Gray(0.16f), 0.5f, false, 0.4f);
            var field = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Moon_field.mat") ?? AdditiveMat("Moon_field", 0.8f);
            var root = new GameObject("Saucer");
            float b0 = Seven.SaucerBeam.ShipBelly;
            BuildSaucerExterior(root.transform, b0, hull, hullUnder, metalDark, rim);
            BuildSaucerBeam(root.transform, b0, b0 - 0.1f, Seven.MoonLayout.HatchRadius, Seven.MoonLayout.BeamFloorRadius, metalDark, rim, field);
            foreach (var tr in root.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = Seven.SaucerProbe.Layer;   // its own probe never sees it
            _saucerPrefab = PrefabUtility.SaveAsPrefabAsset(root, SaucerPrefabPath);
            Object.DestroyImmediate(root);
            Log($"QF13 saucer prefab: {SaucerPrefabPath} ({_saucerPrefab.GetComponentsInChildren<Renderer>(true).Length} renderers, belly {b0} m, beam + hold)");
            return _saucerPrefab;
        }

        /// <summary>An unchanged instance of the one saucer, its root at `at` (the ground point under it when it hovers).</summary>
        static Transform PlaceSaucer(Transform parent, Vector3 at)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(SaucerPrefab(), parent);
            go.transform.position = at; go.transform.rotation = Quaternion.identity;
            return go.transform;
        }

        /// <summary>The build fails if this scene's saucer is not the prefab, unchanged: same meshes, materials, scales,
        /// the beam and the hold present (only the root's position may differ).</summary>
        static void CheckSaucer(string scene, Transform s)
        {
            var pre = SaucerPrefab();
            var src = PrefabUtility.GetCorrespondingObjectFromSource(s.gameObject);
            var errs = new List<string>();
            if (src != pre) errs.Add("not an instance of " + SaucerPrefabPath);
            else
            {
                string PathOf(Transform t, Transform r) { var p = t.name; while (t.parent != null && t.parent != r) { t = t.parent; p = t.name + "/" + p; } return p; }
                var want = pre.GetComponentsInChildren<Renderer>(true).ToDictionary(r => PathOf(r.transform, pre.transform), r => r);
                var have = s.GetComponentsInChildren<Renderer>(true).ToDictionary(r => PathOf(r.transform, s), r => r);
                foreach (var kv in want)
                {
                    if (!have.TryGetValue(kv.Key, out var r)) { errs.Add("missing " + kv.Key); continue; }
                    var mw = kv.Value.GetComponent<MeshFilter>(); var mh = r.GetComponent<MeshFilter>();
                    if (mw != null && (mh == null || mh.sharedMesh != mw.sharedMesh)) errs.Add("mesh differs: " + kv.Key);
                    if (!kv.Value.sharedMaterials.SequenceEqual(r.sharedMaterials)) errs.Add("materials differ: " + kv.Key);
                    if ((r.transform.localScale - kv.Value.transform.localScale).sqrMagnitude > 1e-8f) errs.Add("scale differs: " + kv.Key);
                }
                if (have.Count != want.Count) errs.Add($"renderer count {have.Count} vs prefab {want.Count}");
                if (s.localScale != Vector3.one) errs.Add("root scaled");
                if (s.GetComponent<Seven.SaucerBeam>() == null || s.Find("TractorBeam") == null || s.Find("Hatch") == null) errs.Add("no beam / iris");
                if (s.GetComponent<Seven.SaucerHold>() == null || s.Find("HatchPanel") == null || s.Find("Bay") == null) errs.Add("no bike hold");
                foreach (var mod in PrefabUtility.GetPropertyModifications(s.gameObject) ?? new PropertyModification[0])
                {
                    if (mod.target == pre.transform && (mod.propertyPath.StartsWith("m_LocalPosition") || mod.propertyPath.StartsWith("m_LocalRotation") || mod.propertyPath.StartsWith("m_LocalEulerAnglesHint"))) continue;
                    if (mod.target == pre && mod.propertyPath == "m_Name") continue;
                    if (mod.propertyPath.StartsWith("m_RootOrder")) continue;
                    errs.Add($"override {mod.target?.name}.{mod.propertyPath} = {mod.value}");
                }
            }
            string line = $"SAUCER CHECK (QF13 16) {scene}: " + (errs.Count == 0 ? $"the one prefab, unchanged ({s.GetComponentsInChildren<Renderer>(true).Length} renderers; beam + hold present; root at {s.position})" : "DIFFERS - " + string.Join("; ", errs.Take(8)));
            Log(line); SaucerChecks.Add(line);
            if (errs.Count > 0) throw new System.Exception(line);
        }
    }
}
