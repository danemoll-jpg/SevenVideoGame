using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seven
{
    /// <summary>
    /// QF26 (2): the image-quality gate. Every capture of every check run (all chapters, the saucer, the menus) goes through
    /// <see cref="Save"/>:
    /// - the frame is rendered in HDR exactly like the screen (<see cref="NewRT"/>: half-float). Before QF26 every capture
    ///   texture was 8-bit ARGB32 and URP renders the WHOLE frame in its target's format when a camera has a target texture
    ///   (UniversalRenderPipelineCore.CreateRenderTextureDescriptor), so captures were LDR: nothing could exceed 1, NaN / Inf
    ///   could not exist and bloom never saw what the screen sees - Dan's Cattalus white-out could not show up in them;
    /// - non-finite pixels (NaN / Inf) are counted; any = FAIL, and <see cref="HuntNaN"/> names the renderers that make them;
    /// - the picture is measured on its luminance at 640x360 (same measures as Tools/image_gate.py): near-white share (white-out),
    ///   tonal range p95-p5 (one flat tone / fog bank), edge density (nothing to see / a blank slab), near-black share;
    /// - a subject must be in frame at a readable size: the declared one (<see cref="Expect"/>), else any character (an animated
    ///   skinned figure: 7, Panther, the people of each world) - unblocked, at least 0.4 % of the frame.
    /// Each shot logs `IMAGE GATE <file>: PASS / FAIL ...`; the run ends with `IMAGE GATE (QF26 2): N shots, failed F`
    /// (+ image_gate.txt in the capture folder). Tools/image_gate.py runs the same picture measures over a folder.
    /// </summary>
    public static class ImageGate
    {
        public const float WhiteMax = 0.20f, RangeMin = 0.30f, EdgesMin = 0.06f, BlackMax = 0.85f, SubjectMin = 0.004f, RangeStrong = 0.45f, EdgesHard = 0.01f;
        /// <summary>Shots meant to be one tone (fades, title cards, the vision's bloom to white, video frames, menus).</summary>
        static readonly string[] Exempt = { "title", "fade", "first_frame", "_to_white", "video_frame", "op01", "card", "menu", "black" };
        public static int Shots, Failed, NonFiniteShots, Hunts;
        static readonly StringBuilder _report = new StringBuilder();
        static readonly List<string> _failNames = new List<string>();

        // ---- the subject the next shots must show (a director declares it before a view of a place / thing)
        static Transform _subject; static Vector3? _subjectPoint; static string _subjectLabel, _subjectFor; static float _subjectUntil;
        /// <summary>The next shot(s) are of this (until <paramref name="seconds"/> pass, default: the next shot only). QF27:
        /// <paramref name="forShot"/> = only a shot whose file name contains it (two captures in one frame never take each other's).</summary>
        public static void Expect(Transform t, string label, float seconds = 0f, string forShot = null) { _subject = t; _subjectPoint = null; _subjectLabel = label; _subjectFor = forShot; _subjectUntil = seconds > 0f ? Time.time + seconds : -1f; }
        /// <summary>A view of a place: the point must be in frame and unblocked (the city, the transit tower, the meadow).</summary>
        public static void ExpectPoint(Vector3 p, string label, float seconds = 0f, string forShot = null) { _subject = null; _subjectPoint = p; _subjectLabel = label; _subjectFor = forShot; _subjectUntil = seconds > 0f ? Time.time + seconds : -1f; }
        /// <summary>P2: a side capture (a petting close-up, Tunneler's gesture) declares its subject in its OWN slot, keyed by its file-name
        /// prefix - never touching the director's pending one (a save / restore round it lost the asteroid's a18-a20 and Mars's m11b
        /// subjects when the director declared in between).</summary>
        static readonly Dictionary<string, (Transform t, string label)> _side = new Dictionary<string, (Transform, string)>();
        public static void ExpectSide(string forShot, Transform t, string label) { _side[forShot] = (t, label); }
        static bool SubjectFor(string name) => _subjectFor == null || name.Contains(_subjectFor);
        static void SubjectUsed() { if (_subjectUntil < 0f || Time.time > _subjectUntil) { _subject = null; _subjectPoint = null; _subjectLabel = null; _subjectFor = null; } }

        public static RenderTexture NewRT(int w = 1280, int h = 720) => new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4, name = "ImageGateRT" };

        public struct Result { public bool Pass; public float White, Range, Edges, Black; public int NonFinite; public string Subject, Why; }

        /// <summary>Read <paramref name="rt"/> (rendered by <paramref name="cam"/>), gate it, write the PNG; returns the log line.</summary>
        public static string Save(RenderTexture rt, Camera cam, string dir, string file) => Save(rt, cam, dir, file, out _);
        public static string Save(RenderTexture rt, Camera cam, string dir, string file, out Result res)
        {
            var tex = ReadLdr(rt, out int nonFinite);
            // QF26 (1): the tonemapper saturates NaN to 0 (a black speck) before the picture reaches us, while bloom's clamp has
            // already turned the same pixel into a 65472-bright source - so NaN / Inf are counted in a render WITHOUT post-processing
            int raw = cam != null ? RawNonFinite(cam) : 0;
            nonFinite += raw;
            File.WriteAllBytes(Path.Combine(dir, file), tex.EncodeToPNG());
            res = Measure(tex, file, cam, nonFinite);
            Object.Destroy(tex);
            Shots++;
            if (!res.Pass) { Failed++; _failNames.Add(file); }
            if (nonFinite > 0) NonFiniteShots++;
            string line = $"IMAGE GATE {file}: {(res.Pass ? "PASS" : "FAIL")} - white {res.White * 100f:0.0} %, range {res.Range:0.00}, edges {res.Edges * 100f:0.0} %, black {res.Black * 100f:0.0} %, non-finite pixels {nonFinite}, subject {res.Subject}; {Atmosphere.Report(cam)}{(res.Pass ? "" : "  <- " + res.Why)}";
            _report.AppendLine(line);
            if (nonFinite > 0 && cam != null && Hunts < 4) { Hunts++; line += "\n" + HuntNaN(cam); _report.AppendLine(line.Substring(line.IndexOf('\n') + 1)); }
            return line;
        }

        /// <summary>The run's summary line (+ image_gate.txt in the capture folder).</summary>
        public static string Summary(string dir)
        {
            string s = $"IMAGE GATE (QF26 2): {Shots} shots, failed {Failed}{(Failed > 0 ? " (" + string.Join(", ", _failNames) + ")" : "")}; frames with NaN / Inf pixels {NonFiniteShots}";
            if (dir != null) { try { File.WriteAllText(Path.Combine(dir, "image_gate.txt"), _report + s + "\n"); } catch { } }
            return s;
        }

        static bool Linear => QualitySettings.activeColorSpace == ColorSpace.Linear;

        /// <summary>The HDR frame -> an 8-bit sRGB texture (clamped), counting NaN / Inf pixels on the way.</summary>
        static Texture2D ReadLdr(RenderTexture rt, out int nonFinite)
        {
            var prev = RenderTexture.active; RenderTexture.active = rt;
            bool hdr = rt.format == RenderTextureFormat.ARGBHalf || rt.format == RenderTextureFormat.ARGBFloat || rt.format == RenderTextureFormat.DefaultHDR;
            var raw = new Texture2D(rt.width, rt.height, hdr ? TextureFormat.RGBAHalf : TextureFormat.RGB24, false, hdr);
            raw.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); raw.Apply();
            RenderTexture.active = prev;
            nonFinite = 0;
            if (!hdr) return raw;
            var px = raw.GetPixels();
            var outPx = new Color32[px.Length];
            bool lin = Linear;
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                if (!Finite(c.r) || !Finite(c.g) || !Finite(c.b)) { nonFinite++; outPx[i] = new Color32(0, 0, 0, 255); continue; }
                outPx[i] = new Color32(Enc(c.r, lin), Enc(c.g, lin), Enc(c.b, lin), 255);
            }
            Object.Destroy(raw);
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.SetPixels32(outPx); tex.Apply();
            return tex;
        }
        static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        static byte Enc(float v, bool lin) { v = Mathf.Clamp01(v); if (lin) v = Mathf.LinearToGammaSpace(v); return (byte)Mathf.RoundToInt(v * 255f); }

        public static Result Measure(Texture2D tex, string name, Camera cam, int nonFinite)
        {
            var r = new Result { NonFinite = nonFinite };
            var px = tex.GetPixels32(); int W = tex.width, H = tex.height, w = W / 2, h = H / 2;
            var L = new float[w * h]; var hist = new int[256]; int white = 0, black = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float s = 0f;
                    for (int dy = 0; dy < 2; dy++) for (int dx = 0; dx < 2; dx++) { var c = px[(y * 2 + dy) * W + x * 2 + dx]; s += (c.r * 0.299f + c.g * 0.587f + c.b * 0.114f) / 255f; }
                    s *= 0.25f; L[y * w + x] = s; hist[Mathf.Clamp((int)(s * 255f), 0, 255)]++;
                    if (s >= 0.92f) white++; if (s <= 0.03f) black++;
                }
            int n = w * h, edges = 0;
            for (int y = 0; y < h - 1; y++) for (int x = 0; x < w - 1; x++) { float a = L[y * w + x]; if (Mathf.Abs(L[(y + 1) * w + x] - a) + Mathf.Abs(L[y * w + x + 1] - a) > 0.03f) edges++; }
            r.White = white / (float)n; r.Black = black / (float)n; r.Edges = edges / (float)((w - 1) * (h - 1));
            r.Range = Pct(hist, n, 0.95f) - Pct(hist, n, 0.05f);
            var why = new List<string>();
            bool exempt = false; string ln = name.ToLowerInvariant(); foreach (var e in Exempt) if (ln.Contains(e)) exempt = true;
            if (nonFinite > 0) why.Add($"{nonFinite} NaN / Inf pixels in the HDR frame (bloom turns them into a white-out on screen)");
            if (!exempt)
            {
                if (r.White > WhiteMax) why.Add($"white {r.White * 100f:0} % > {WhiteMax * 100f:0} %");
                // an insert is a deliberately minimal close-up of one thing (the mark on the box): its declared subject must be there,
                // but the range / edge measures (made for scenes) do not apply
                bool insert = ln.Contains("_insert_");
                if (!insert && r.Range < RangeMin) why.Add($"range {r.Range:0.00} < {RangeMin:0.00}");
                // "nothing to see": little detail AND little contrast (a smooth, strongly lit shape - the cave mouth, the asteroid - is fine)
                if (!insert && ((r.Edges < EdgesMin && r.Range < RangeStrong) || r.Edges < EdgesHard)) why.Add($"edges {r.Edges * 100f:0.0} % < {EdgesMin * 100f:0} % with range {r.Range:0.00} < {RangeStrong:0.00} (or < {EdgesHard * 100f:0} %)");
                if (r.Black > BlackMax) why.Add($"black {r.Black * 100f:0} % > {BlackMax * 100f:0} %");
                string sideKey = null; foreach (var kv in _side) if (ln.Contains(kv.Key)) { sideKey = kv.Key; break; }
                if (sideKey != null)
                {
                    var sd = _side[sideKey]; bool vis = false; float a = 0f; if (sd.t != null) a = Area(cam, sd.t, out vis); bool okSide = sd.t != null && a >= SubjectMin && vis;
                    r.Subject = $"'{sd.label}' {a * 100f:0.0} % of the frame{(okSide ? "" : ", blocked / too small")}";
                    if (!okSide) why.Add("no subject at a readable size (" + r.Subject + ")");
                }
                else
                {
                    bool mine = SubjectFor(ln);
                    if (!SubjectInFrame(cam, out r.Subject, mine)) why.Add("no subject at a readable size (" + r.Subject + ")");
                }
            }
            else r.Subject = "exempt shot (fade / title / video / menu)";
            bool sideShot = false; foreach (var kv in _side) if (ln.Contains(kv.Key)) sideShot = true;
            if (!sideShot && SubjectFor(ln)) SubjectUsed();
            r.Pass = why.Count == 0; r.Why = string.Join("; ", why);
            return r;
        }
        static float Pct(int[] hist, int n, float q) { int want = Mathf.CeilToInt(n * q), acc = 0; for (int i = 0; i < 256; i++) { acc += hist[i]; if (acc >= want) return i / 255f; } return 1f; }

        // ------------------------------------------------------------------ the subject
        static readonly List<Renderer> _rs = new List<Renderer>();
        static bool SubjectInFrame(Camera cam, out string what, bool useDeclared = true)
        {
            what = "no camera";
            if (cam == null) return true;
            if (useDeclared && _subjectPoint.HasValue)
            {
                bool ok = PointVisible(cam, _subjectPoint.Value, out float off);
                what = $"'{_subjectLabel}' (a place) {(ok ? "in frame, unblocked" : off > 0f ? "blocked" : "out of frame")}";
                return ok;
            }
            if (useDeclared && _subject != null)
            {
                float a = Area(cam, _subject, out bool vis);
                what = $"'{_subjectLabel}' {a * 100f:0.0} % of the frame{(vis ? "" : ", blocked")}";
                return a >= SubjectMin && vis;
            }
            // any character: an animated skinned figure in front of the camera
            float best = 0f; string bestName = null;
            var roots = new HashSet<Transform>();
            foreach (var an in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
                if (an.isActiveAndEnabled && an.GetComponentInChildren<SkinnedMeshRenderer>() != null) roots.Add(an.transform);
            foreach (var sm in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))   // figures driven without an Animator
                if (sm.enabled && sm.gameObject.activeInHierarchy && sm.GetComponentInParent<Animator>() == null && sm.transform.parent != null) roots.Add(sm.transform.parent);
            foreach (var rt in roots)
            {
                float a = Area(cam, rt, out bool vis);
                if (vis && a > best) { best = a; bestName = rt.name; }
            }
            what = bestName != null ? $"'{bestName}' {best * 100f:0.0} % of the frame" : "no character in frame and none declared";
            return best >= SubjectMin;
        }
        static float Area(Camera cam, Transform t, out bool visible)
        {
            visible = false; _rs.Clear(); t.GetComponentsInChildren(false, _rs);
            Bounds b = default; bool any = false;
            foreach (var r in _rs) { if (!r.enabled || r.forceRenderingOff || r is ParticleSystemRenderer) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            if (!any) return 0f;
            Vector2 mn = new Vector2(1f, 1f), mx = new Vector2(0f, 0f); bool front = false, behind = false;
            for (int i = 0; i < 8; i++)
            {
                var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var v = cam.WorldToViewportPoint(c); if (v.z <= cam.nearClipPlane) { behind = true; continue; } front = true;
                mn = Vector2.Min(mn, new Vector2(v.x, v.y)); mx = Vector2.Max(mx, new Vector2(v.x, v.y));
            }
            if (!front) return 0f;
            // QF28 pass: a subject that reaches past the camera (an insert right at the bike's grip) runs off the frame on that side -
            // its corners behind the camera project nowhere, so its in-front extent is widened to the frame's edge
            if (behind) { var vc = cam.WorldToViewportPoint(b.center); if (vc.x < 0.5f) mn.x = 0f; else mx.x = 1f; if (vc.y < 0.5f) mn.y = 0f; else mx.y = 1f; }
            mn = Vector2.Max(mn, Vector2.zero); mx = Vector2.Min(mx, Vector2.one);
            if (mx.x <= mn.x || mx.y <= mn.y) return 0f;
            // unblocked: a clear line to some part of it - a 3 x 3 x 3 grid across its bounds (QF28 pass: three points down its middle
            // ran through whatever sits INSIDE the subject - the bike in the saucer's bay - and called the saucer blocked) - or, for a
            // figure, its head, chest, hips or a hand
            var pts = new List<Vector3>();
            for (int gx = -1; gx <= 1; gx++) for (int gy = -1; gy <= 1; gy++) for (int gz = -1; gz <= 1; gz++) pts.Add(b.center + Vector3.Scale(b.extents * 0.7f, new Vector3(gx, gy, gz)));
            var an = t.GetComponentInChildren<Animator>();
            if (an != null && an.isHuman) foreach (var hb in new[] { HumanBodyBones.Head, HumanBodyBones.Chest, HumanBodyBones.Hips, HumanBodyBones.RightHand, HumanBodyBones.LeftHand }) { var bt = an.GetBoneTransform(hb); if (bt != null) pts.Add(bt.position); }
            foreach (var p in pts) if (PointVisible(cam, p, out _, t)) { visible = true; break; }
            return (mx.x - mn.x) * (mx.y - mn.y);
        }
        static readonly RaycastHit[] _hits = new RaycastHit[64];
        static bool PointVisible(Camera cam, Vector3 p, out float blockedAt, Transform self = null)
        {
            blockedAt = 0f;
            var v = cam.WorldToViewportPoint(p);
            if (v.z <= 0f || v.x < 0f || v.x > 1f || v.y < 0f || v.y > 1f) return false;
            var o = cam.transform.position; var d = p - o; float len = d.magnitude;
            if (len < 0.05f) return true;
            // solid geometry only: grass, loose props and the subject's own colliders never hide it
            if (!CameraOccluders.Ready || !CameraOccluders.AnyRay(o, d / len, len - 0.4f, CameraOccluders.SolidMask, out float dist, CameraOccluders.SolidMask)) return true;
            if (self == null) { blockedAt = dist; return false; }
            // QF26 (2) fix: the subject's OWN surfaces never hide it (a saucer's middle lies inside its hull) - only other geometry
            // does. Before, only real colliders were checked for that; the visual-mesh occluders of the subject itself "blocked" it.
            var dir = d / len; float lim = len - 0.4f;
            int n = Physics.RaycastNonAlloc(o, dir, _hits, lim, CameraOccluders.SolidMask, QueryTriggerInteraction.Ignore);
            // a small collision volume that ENCLOSES the probed point (the bike's hull round its rider) is not in front of it
            bool Encloses(Collider c) => c != null && c.bounds.size.magnitude < 6f && c.bounds.Contains(p);
            for (int i = 0; i < n; i++) if (!_hits[i].transform.IsChildOf(self) && !Encloses(_hits[i].collider)) { blockedAt = _hits[i].distance; return false; }
            n = CameraOccluders.RaycastAll(o, dir, lim, _hits, CameraOccluders.SolidMask);
            for (int i = 0; i < n; i++) { var r = CameraOccluders.RendererOf(_hits[i].collider); if ((r == null || !r.transform.IsChildOf(self)) && !(r != null && r.bounds.size.magnitude < 6f && r.bounds.Contains(p))) { blockedAt = _hits[i].distance; return false; } }
            return true;
        }

        // ------------------------------------------------------------------ the NaN hunt
        /// <summary>The frame has NaN / Inf pixels: re-render it with groups of renderers switched off (halving) until the
        /// renderers that make them are named. A full-screen pass (the fog, post-processing) shows as "none of the renderers".</summary>
        public static string HuntNaN(Camera cam)
        {
            var rt = new RenderTexture(640, 360, 24, RenderTextureFormat.ARGBHalf) { name = "NaNHuntRT" };
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            var all = new List<Renderer>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (r.enabled && !r.forceRenderingOff && r.gameObject.activeInHierarchy && GeometryUtility.TestPlanesAABB(planes, r.bounds)) all.Add(r);
            int renders = 0;
            int Count() { renders++; return CountRaw(cam, rt); }
            int baseCount = Count();
            var culprits = new List<(Renderer r, int fixedPx)>();
            void Hunt(List<Renderer> set)
            {
                if (set.Count == 0 || renders > 160) return;
                foreach (var r in set) r.forceRenderingOff = true;
                int c = Count();
                foreach (var r in set) r.forceRenderingOff = false;
                if (c >= baseCount) return;
                if (set.Count == 1) { culprits.Add((set[0], baseCount - c)); return; }
                int h = set.Count / 2; Hunt(set.GetRange(0, h)); Hunt(set.GetRange(h, set.Count - h));
            }
            if (baseCount > 0) Hunt(all);
            rt.Release(); Object.Destroy(rt);
            var sb = new StringBuilder();
            sb.Append($"NAN SOURCE (QF26 1): {baseCount} non-finite pixels (640x360, before post-processing) over {all.Count} renderers in view, {renders} renders: ");
            if (baseCount == 0) sb.Append("gone when re-rendered (timing / motion)");
            else if (culprits.Count == 0) sb.Append("none of the renderers - a full-screen pass (the fog / post-processing) or the sky");
            else foreach (var (r, px) in culprits)
                {
                    var m = r.sharedMaterial;
                    sb.Append($"\n    {TPath(r.transform)} ({r.GetType().Name}, material '{(m != null ? m.name : "-")}', shader '{(m != null ? m.shader.name : "-")}', mesh '{MeshName(r)}') -> {px} px");
                }
            return sb.ToString();
        }
        static RenderTexture _rawRT;
        /// <summary>Non-finite pixels in the camera's frame rendered at 640x360 WITHOUT post-processing (the fog pass still runs).</summary>
        public static int RawNonFinite(Camera cam)
        {
            if (_rawRT == null) _rawRT = new RenderTexture(640, 360, 24, RenderTextureFormat.ARGBHalf) { name = "ImageGateRawRT" };
            return CountRaw(cam, _rawRT);
        }
        static int CountRaw(Camera cam, RenderTexture rt)
        {
            var ad = cam.GetComponent<UniversalAdditionalCameraData>(); bool pp = ad != null && ad.renderPostProcessing;
            if (ad != null) ad.renderPostProcessing = false;
            try
            {
                var req = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
                if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
                else { var tt = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = tt; }
            }
            finally { if (ad != null) ad.renderPostProcessing = pp; }
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(rt.width, rt.height, TextureFormat.RGBAHalf, false, true); t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); t.Apply();
            RenderTexture.active = prev;
            int n = 0; foreach (var c in t.GetPixels()) if (!Finite(c.r) || !Finite(c.g) || !Finite(c.b)) n++;
            Object.Destroy(t); return n;
        }
        static string MeshName(Renderer r) { var mf = r.GetComponent<MeshFilter>(); if (mf != null && mf.sharedMesh != null) return mf.sharedMesh.name; var s = r as SkinnedMeshRenderer; return s != null && s.sharedMesh != null ? s.sharedMesh.name : "-"; }
        static string TPath(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }
    }
}
