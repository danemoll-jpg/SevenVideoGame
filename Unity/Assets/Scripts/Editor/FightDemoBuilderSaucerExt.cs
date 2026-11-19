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
        const float BayInner = 1.6f;                                              // the bay's back wall radius

        class SaucerExt { public Transform Panel; public Renderer Seam, BayBack; public Light BayLight, UnderLight; public readonly List<Renderer> Glow = new List<Renderer>(); }

        static SaucerExt BuildSaucerExterior(Transform saucer, float b0, Material hull, Material hullUnder, Material metalDark, Material rim)
        {
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
            MeshObject("Hull_Under", SaveMesh(Lathe(underIn, 0f, 360f, 96), "Saucer_Ext_UnderIn"), hullUnder, saucer);
            MeshObject("Hull_UnderOut", SaveMesh(Lathe(underOut, 0f, 360f, 96, InHatch), "Saucer_Ext_Under"), hullUnder, saucer);
            MeshObject("Hull_Upper", SaveMesh(Lathe(upperOut, 0f, 360f, 96, InHatch), "Saucer_Ext_Upper"), hull, saucer);
            MeshObject("Hull_UpperIn", SaveMesh(Lathe(upperIn, 0f, 360f, 96), "Saucer_Ext_UpperIn"), hull, saucer);
            MeshObject("Hull_RimBand", SaveMesh(Lathe(rimBand, 0f, 360f, 96, InHatch), "Saucer_Ext_RimBand"), hullUnder, saucer);
            var bandV = new List<Vector2>(); for (float r = 4.75f; r <= 5.98f; r += 0.08f) bandV.Add(new Vector2(r, UnderY(r) - 0.012f));
            MeshObject("Hull_UnderBand", SaveMesh(Lathe(bandV, 0f, 360f, 96, InHatch), "Saucer_Ext_UnderBand"), metalDark, saucer);
            // seams (concentric rings + radial panel seams under, two fine rings over) - broken where the hatch panel is
            {
                var sv = new List<Vector3>(); var st = new List<int>();
                void Ring(float rr, float y0, float rad) { var run = new List<Vector3>(); for (int i = 0; i <= 96; i++) { float a = i * 3.75f; if (InSector(a) && rr > BayInner) { if (run.Count > 1) Sweep(sv, st, run, rad, false, 5); run = new List<Vector3>(); continue; } run.Add(Polar(rr, a, y0)); } if (run.Count > 1) Sweep(sv, st, run, rad, false, 5); }
                foreach (var rr in new[] { 1.65f, 2.9f, 3.9f, 4.7f }) Ring(rr, UnderY(rr) - 0.014f, 0.016f);
                for (int k = 0; k < 16; k++) { float a = k * 22.5f; if (InSector(a)) continue; Sweep(sv, st, Enumerable.Range(0, 9).Select(i => { float rr = Mathf.Lerp(1.65f, 4.7f, i / 8f); return Polar(rr, a, UnderY(rr) - 0.014f); }).ToList(), 0.012f, false, 5); }
                foreach (var rr in new[] { 2.4f, 4.9f }) Ring(rr, UpperY(rr) + 0.006f, 0.008f);
                MeshObject("Hull_Seams", SaveMesh(SmoothFinish(sv, st), "Saucer_Ext_Seams"), metalDark, saucer, false);
            }
            var gv = new List<Vector3>(); var gt = new List<int>();
            { var run = new List<Vector3>(); for (int i = 0; i <= 120; i++) { float a = i * 3f; if (InSector(a)) { if (run.Count > 1) Sweep(gv, gt, run, 0.05f, false, 8); run = new List<Vector3>(); continue; } run.Add(Polar(6.07f, a, b0 + 0.69f)); } if (run.Count > 1) Sweep(gv, gt, run, 0.05f, false, 8); }
            ext.Glow.Add(MeshObject("RimGlow", SaveMesh(SmoothFinish(gv, gt), "Saucer_Ext_RimGlow"), rim, saucer, false).GetComponent<Renderer>());
            var bv = new List<Vector3>(); var bt = new List<int>();
            Sweep(bv, bt, Enumerable.Range(0, 72).Select(i => Polar(2.4f, i * 5f, b0 - 0.07f)).ToList(), 0.04f, true, 6);
            Sweep(bv, bt, Enumerable.Range(0, 48).Select(i => Polar(1.2f, i * 7.5f, b0 + 1.97f)).ToList(), 0.025f, true, 6);
            ext.Glow.Add(MeshObject("BellyGlow", SaveMesh(SmoothFinish(bv, bt), "Saucer_Ext_BellyGlow"), rim, saucer, false).GetComponent<Renderer>());
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
            var panel = new GameObject("HatchPanel").transform; panel.SetParent(saucer, false); panel.localPosition = Vector3.zero;
            MeshObject("PanelUnder", SaveMesh(Lathe(underOut, ha0, ha1, 8), "Saucer_Ext_PanelUnder"), hullUnder, panel);
            MeshObject("PanelUpper", SaveMesh(Lathe(upperOut, ha0, ha1, 8), "Saucer_Ext_PanelUpper"), hull, panel);
            MeshObject("PanelRim", SaveMesh(Lathe(rimBand, ha0, ha1, 8), "Saucer_Ext_PanelRim"), hullUnder, panel);
            MeshObject("PanelBand", SaveMesh(Lathe(bandV, ha0, ha1, 8), "Saucer_Ext_PanelBand"), metalDark, panel);
            ext.Panel = panel;
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
                var seam = MeshObject("HatchSeam", SaveMesh(SmoothFinish(sv, st), "Saucer_Ext_HatchSeam"), seamMat, saucer, false);
                ext.Seam = seam.GetComponent<Renderer>(); ext.Seam.enabled = false;
            }
            // the bay behind it: dark floor / ceiling / sides, a softly glowing back wall, a light that comes up as it opens
            {
                var bayMat = Dissolve("Saucer_bay", Gray(0.2f), 0.3f, false); bayMat.SetFloat("_Cull", 0f); EditorUtility.SetDirty(bayMat);
                var bay = new GameObject("Bay").transform; bay.SetParent(saucer, false);
                var fl = underOut.Select(p => new Vector2(p.x, p.y + 0.03f)).ToList(); var ce = upperOut.Select(p => new Vector2(p.x, p.y - 0.03f)).ToList();
                MeshObject("BayFloor", SaveMesh(Lathe(fl, ha0, ha1, 8), "Saucer_Ext_BayFloor"), bayMat, bay, false);
                MeshObject("BayCeiling", SaveMesh(Lathe(ce, ha0, ha1, 8), "Saucer_Ext_BayCeiling"), bayMat, bay, false);
                var backMat = Dissolve("Saucer_bay_back", Gray(0.9f), 0.2f, false); backMat.EnableKeyword("_EMISSION"); backMat.SetColor("_EmissionColor", Color.white * 0.05f); EditorUtility.SetDirty(backMat);
                var back = MeshObject("BayBack", SaveMesh(Lathe(new[] { new Vector2(BayInner, UpperY(BayInner) - 0.03f), new Vector2(BayInner, UnderY(BayInner) + 0.03f) }, ha0, ha1, 8), "Saucer_Ext_BayBack"), backMat, bay, false);
                ext.BayBack = back.GetComponent<Renderer>();
                foreach (var a in new[] { ha0, ha1 })
                {
                    var wv = new List<Vector3>(); var wt = new List<int>();
                    for (float r = BayInner; r < 6.0f - 1e-3f; r += 0.2f)
                    {
                        float r2 = Mathf.Min(6.0f, r + 0.2f); int b = wv.Count;
                        wv.Add(Polar(r, a, UnderY(r) + 0.03f)); wv.Add(Polar(r2, a, UnderY(r2) + 0.03f)); wv.Add(Polar(r2, a, UpperY(r2) - 0.03f)); wv.Add(Polar(r, a, UpperY(r) - 0.03f));
                        wt.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
                    }
                    MeshObject($"BaySide{a:0}", SaveMesh(Finish(wv, wt), $"Saucer_Ext_BaySide{a:0}"), bayMat, bay, false);
                }
                var bl = new GameObject("BayLight").AddComponent<Light>(); bl.transform.SetParent(bay, false);
                bl.transform.localPosition = Polar(BayInner + 1.2f, SaucerHatchAngle, b0 + 0.7f);
                bl.type = LightType.Point; bl.range = 7f; bl.intensity = 0f; bl.color = Color.white; bl.shadows = LightShadows.None;
                ext.BayLight = bl;
            }
            return ext;
        }
    }
}
