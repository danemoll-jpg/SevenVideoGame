using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF31 (A3 / A4) check-run tool, two jobs, both started when a named capture is taken in any chapter's check run:
    ///  -memclips "shotA=mem_01_first_meeting:5;shotB=mem_02_free_range:4"  -> the RAW memory clips for Dan's own edit: from that shot on,
    ///     the player's camera is recorded for N seconds of game time at 25 fps, 1920x1044, no screen text, as JPG frames in
    ///     &lt;capture dir&gt;/memclips/&lt;name&gt;/ (Tools/Video/encode_memclips.py makes the h264 clips in Art/Video/memory_clips/).
    ///  -recstills "shotA=rec_1_venus;shotB=rec_2_mars"  -> FIN_12's "Received" stills: the same frame again with 7, Panther, their bike
    ///     and the saucer hidden (the montage shows the places and their people only - they are in the chamber at that moment), no
    ///     screen text; checked (renderers of theirs left in frame must be 0) and listed in &lt;capture dir&gt;/recstills/received_check.json.
    /// A shot name matches when it contains the key (the first match only).
    /// </summary>
    public static class ClipRecorder
    {
        static Dictionary<string, (string name, float secs)> _clips; static Dictionary<string, string> _stills;
        static readonly HashSet<string> _done = new HashSet<string>();
        public static bool Recording { get; private set; }

        static void Parse()
        {
            if (_clips != null) return;
            _clips = new Dictionary<string, (string, float)>(); _stills = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(DemoBoot.MemClips))
                foreach (var part in DemoBoot.MemClips.Split(';'))
                {
                    var kv = part.Split('='); if (kv.Length != 2) continue; var nv = kv[1].Split(':');
                    _clips[kv[0].Trim()] = (nv[0].Trim(), nv.Length > 1 && float.TryParse(nv[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s) ? s : 5f);
                }
            if (!string.IsNullOrEmpty(DemoBoot.RecStills))
                foreach (var part in DemoBoot.RecStills.Split(';')) { var kv = part.Split('='); if (kv.Length == 2) _stills[kv[0].Trim()] = kv[1].Trim(); }
        }

        /// <summary>QF34 (2): a memory can also start on a dialogue line (its id as the key) ...</summary>
        public static void OnLine(string id)
        {
            if (DemoBoot.CaptureDir == null || string.IsNullOrEmpty(id) || ChapterCapture.I == null) return; Parse();
            foreach (var kv in _clips) if (!_done.Contains("c:" + kv.Key) && kv.Key == id && !Recording) { _done.Add("c:" + kv.Key); ChapterCapture.I.StartCoroutine(Record(ChapterCapture.I, Camera.main, kv.Value.name, kv.Value.secs)); }
        }
        /// <summary>... or at a game time ("@tSECONDS" as the key; checked by ChapterCapture every frame).</summary>
        public static void Tick()
        {
            if (DemoBoot.CaptureDir == null || ChapterCapture.I == null) return; Parse();
            foreach (var kv in _clips)
            {
                if (!kv.Key.StartsWith("@t") || _done.Contains("c:" + kv.Key) || Recording) continue;
                if (float.TryParse(kv.Key.Substring(2), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var at) && Time.time >= at)
                { _done.Add("c:" + kv.Key); ChapterCapture.I.StartCoroutine(Record(ChapterCapture.I, Camera.main, kv.Value.name, kv.Value.secs)); }
            }
        }

        /// <summary>Called by ChapterCapture after every capture.</summary>
        public static void OnShot(ChapterCapture cap, Camera cam, string shot)
        {
            if (DemoBoot.CaptureDir == null || cam == null) return; Parse();
            foreach (var kv in _clips) if (!_done.Contains("c:" + kv.Key) && shot.Contains(kv.Key) && !Recording) { _done.Add("c:" + kv.Key); cap.StartCoroutine(Record(cap, cam, kv.Value.name, kv.Value.secs)); }
            foreach (var kv in _stills) if (!_done.Contains("s:" + kv.Key) && shot.Contains(kv.Key)) { _done.Add("s:" + kv.Key); cap.StartCoroutine(Still(cap, cam, kv.Value, shot)); }
        }
        public static bool AllDone { get { Parse(); return _done.Count >= _clips.Count + _stills.Count && !Recording; } }

        static List<Canvas> TextOff() { var l = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.enabled).ToList(); foreach (var c in l) c.enabled = false; return l; }

        // ---- QF34 (2): the memory's own camera - 7 and Panther TOGETHER in every frame (rc6's clips recorded whatever the check run's
        // camera happened to show at the capture: an energy-point close-up, a hand in a grip - Panther was missing from most)
        static Transform FindSeven() { var c = Object.FindAnyObjectByType<ChapterSeven>(); if (c != null) return c.transform; var s = Object.FindAnyObjectByType<SaucerSeven>(); if (s != null) return s.transform; return null; }
        static Transform FindCat() { var c = Object.FindAnyObjectByType<ChapterCat>(); if (c != null && c.gameObject.activeInHierarchy) return c.transform; var cc = Object.FindAnyObjectByType<CatCompanion>(); return cc != null && cc.gameObject.activeInHierarchy ? cc.transform : null; }
        static bool BoundsOf(Transform t, out Bounds b) { b = default; bool any = false; foreach (var r in t.GetComponentsInChildren<Renderer>()) { if (!r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); } return any; }
        static bool Clear(Vector3 a, Vector3 b) { bool c = true; try { c = ChapterCamera.ClearLine(a, b); } catch { } return c && !Physics.Linecast(a, b + (a - b).normalized * 0.4f, ~0, QueryTriggerInteraction.Ignore); }   // QF34 (2): real colliders too (the galley's walls)
        static bool _noClearSide;
        static Vector3 _side; static float _sideBlocked;
        static bool TwoShot(Camera mc, Transform seven, Transform cat, float u, bool first)
        {
            if (!BoundsOf(seven, out var sb) || !BoundsOf(cat, out var cb)) return false;
            var a = sb.center + Vector3.up * sb.extents.y * 0.35f; var b = cb.center + Vector3.up * cb.extents.y * 0.2f;
            var ab = b - a; var abF = new Vector3(ab.x, 0f, ab.z); float apart = abF.magnitude;
            Vector3 pos, look;
            if (apart > 8f)
            {
                // far apart (the race: he watches her from the square) - over his shoulder toward her, both in frame
                var toHer = (-abF).normalized; var sd = Vector3.Cross(Vector3.up, toHer);
                pos = b - toHer * (1.2f + cb.extents.magnitude) + sd * 0.55f + Vector3.up * 0.25f; look = Vector3.Lerp(b + Vector3.up * 0.3f, a, 0.7f);   // low behind him, her beyond
            }
            else
            {
                var mid = Vector3.Lerp(a, b, 0.5f); var perp = apart > 0.05f ? Vector3.Cross(Vector3.up, abF.normalized) : Vector3.forward;
                float span = apart + Mathf.Max(sb.extents.y, cb.extents.y) * 1.5f + 0.3f;   // framed on the two of them (tighter than rc6's first try)
                float htan = Mathf.Tan(mc.fieldOfView * 0.5f * Mathf.Deg2Rad) * Mathf.Max(1f, mc.aspect);                                    // across the frame (rc7's first take used the vertical lens: 1.8x too far)
                float dist = Mathf.Max(1.6f, span * 0.6f / htan, sb.extents.y * 1.3f / Mathf.Tan(mc.fieldOfView * 0.5f * Mathf.Deg2Rad)) * Mathf.Lerp(1.0f, 0.88f, u);   // a gentle push-in
                if (first || _sideBlocked > 0.5f)
                {
                    _sideBlocked = 0f; _side = perp;
                    // the side that sees her face (and his) first: candidates scored by how much they face 7's front and Panther's
                    var f7 = new Vector3(seven.forward.x, 0f, seven.forward.z).normalized; var fc = new Vector3(cat.forward.x, 0f, cat.forward.z).normalized;
                    var cands = new List<Vector3>(); foreach (var ang in new[] { 0f, 180f, 30f, -30f, 150f, -150f, 60f, -60f, 120f, -120f }) cands.Add(Quaternion.Euler(0f, ang, 0f) * perp);
                    cands.Sort((x, y) => (Vector3.Dot(y, f7) + 0.5f * Vector3.Dot(y, fc)).CompareTo(Vector3.Dot(x, f7) + 0.5f * Vector3.Dot(x, fc)));
                    bool found = false;
                    foreach (var d in cands) { var p = mid + d * dist + Vector3.up * 0.35f; if (Clear(p, a) && Clear(p, b) && !InsideAny(p, seven, cat)) { _side = d; found = true; break; } }
                    if (first) _noClearSide = !found;
                }
                pos = mid + _side * dist + Vector3.up * 0.35f; look = mid;
                if (!Clear(pos, a) || !Clear(pos, b)) _sideBlocked += Time.deltaTime; else _sideBlocked = 0f;
            }
            if (first) mc.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos));
            else { mc.transform.position = Vector3.Lerp(mc.transform.position, pos, 1f - Mathf.Exp(-Time.deltaTime * 3f)); mc.transform.rotation = Quaternion.Slerp(mc.transform.rotation, Quaternion.LookRotation(look - mc.transform.position), 1f - Mathf.Exp(-Time.deltaTime * 4f)); }
            return true;
        }
        /// <summary>The camera never stands inside a mesh (the galley's wall: rc7's first meal clip was a grey slab).</summary>
        static bool InsideAny(Vector3 p, Transform seven, Transform cat)
        {
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || r.transform.IsChildOf(seven) || r.transform.IsChildOf(cat)) continue;
                var b = r.bounds; if (b.size.magnitude > 40f || b.size.magnitude < 0.3f) continue;
                b.Expand(0.25f); if (b.Contains(p)) return true;
            }
            return false;
        }
        static bool CatVisible(Camera cam, Transform cat)
        {
            if (cat == null || !BoundsOf(cat, out var cb)) return false;
            if (!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(cam), cb)) return false;
            var vp = cam.WorldToViewportPoint(cb.center); if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) return false;
            return Clear(cam.transform.position, cb.center) || Clear(cam.transform.position, cb.center + Vector3.up * cb.extents.y * 0.6f);
        }

        static IEnumerator Record(ChapterCapture cap, Camera cam, string name, float secs)
        {
            Recording = true;
            var dir = Path.Combine(DemoBoot.CaptureDir, "memclips", name); Directory.CreateDirectory(dir);
            var hdr = ImageGate.NewRT(1920, 1044); var ldr = new RenderTexture(1920, 1044, 0, RenderTextureFormat.ARGB32); var tex = new Texture2D(1920, 1044, TextureFormat.RGB24, false);
            // QF34 (2): the memory camera (a copy of the player's camera's look, its own placement); the player's camera is untouched
            // "<name>@scene": a tight place (the galley nook: its walls are neither occluders nor colliders) - the scene's own two-shot records it
            bool useScene = name.EndsWith("@scene"); if (useScene) { name = name.Substring(0, name.Length - 6); dir = Path.Combine(DemoBoot.CaptureDir, "memclips", name); Directory.CreateDirectory(dir); }
            var seven = FindSeven(); var cat = FindCat(); Camera mc = null; GameObject mgo = null;
            if (seven != null && cat != null && cam != null && !useScene)
            {
                mgo = new GameObject("MemoryCamera"); mc = mgo.AddComponent<Camera>(); mc.CopyFrom(cam); mc.fieldOfView = 34f; mc.rect = new Rect(0, 0, 1, 1);
                var ud = mgo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); ud.renderPostProcessing = true; ud.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                mc.depth = cam.depth - 1f; TwoShot(mc, seven, cat, 0f, true);
                // no side of the place gives a clear two-shot (a tight nook): the scene's own camera, which frames them there, records it
                if (_noClearSide) { Object.Destroy(mgo); mgo = null; mc = null; cap.Log($"MEMORY CLIP (QF34 2): {name} - no clear two-shot side here; the scene's own camera records it"); }
            }
            var rec = mc != null ? mc : cam;
            var was = rec.targetTexture; rec.targetTexture = hdr;
            var canv = TextOff();
            float t0 = Time.time, next = 0f; int n = 0, catSeen = 0, bothSeen = 0; bool gated = false; var vis = new System.Text.StringBuilder();
            while (Time.time - t0 < secs)
            {
                if (mc != null && seven != null && cat != null) TwoShot(mc, seven, cat, (Time.time - t0) / secs, false);
                yield return new WaitForEndOfFrame();
                if (Time.time - t0 + 1e-4f < next) continue;
                next += 1f / 25f;
                bool both = CatVisible(rec, cat) && CatVisible(rec, seven); if (CatVisible(rec, cat)) catSeen++; if (both) bothSeen++; vis.Append(both ? '1' : '0');
                if (!gated && Time.time - t0 > secs * 0.5f && cat != null) { gated = true; ImageGate.Expect(cat, "7 and Panther together (memory " + name + ")", 0.5f); cap.Log(ImageGate.Save(hdr, rec, DemoBoot.CaptureDir, $"memclip_{name}.png")); }
                Graphics.Blit(hdr, ldr);
                var prev = RenderTexture.active; RenderTexture.active = ldr; tex.ReadPixels(new Rect(0, 0, 1920, 1044), 0, 0); tex.Apply(false); RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(dir, $"f{++n:0000}.jpg"), tex.EncodeToJPG(93));
            }
            foreach (var c in canv) if (c != null) c.enabled = true;
            rec.targetTexture = was; hdr.Release(); ldr.Release(); Object.Destroy(tex); if (mgo != null) Object.Destroy(mgo);
            File.WriteAllText(Path.Combine(dir, "both_visible.txt"), vis.ToString());   // per frame: 1 = 7 AND Panther in view (the montage picks from these)
            cap.Log($"MEMORY CLIP (QF34 2): {name} - {n} frames ({n / 25f:0.0} s at 25 fps, 1920x1044, no screen text) from {(mc != null ? "the memory camera (a two-shot of 7 and Panther)" : useScene ? "the scene's own two-shot camera" : "the player's camera (7 or Panther not found!)")} - Panther visible in {catSeen} / {n} frames, both of them in {bothSeen} / {n} -> memclips/{name}/");
            Recording = false;
        }

        /// <summary>The roots that must never be in a "Received" still: 7, Panther, their bike, the saucer (+ Panther's aboard cutaways).</summary>
        static List<GameObject> Theirs()
        {
            var l = new List<GameObject>();
            foreach (var c in Object.FindObjectsByType<ChapterSeven>(FindObjectsSortMode.None)) l.Add(c.gameObject);
            foreach (var c in Object.FindObjectsByType<ChapterCat>(FindObjectsSortMode.None)) l.Add(c.gameObject);
            foreach (var c in Object.FindObjectsByType<CatCompanion>(FindObjectsSortMode.None)) l.Add(c.gameObject);
            foreach (var c in Object.FindObjectsByType<HoverBike>(FindObjectsSortMode.None)) l.Add(c.gameObject);
            foreach (var c in Object.FindObjectsByType<SaucerBeam>(FindObjectsSortMode.None)) l.Add(c.gameObject);
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (t.parent == null && (t.name == "Saucer" || t.name == "7" || t.name.StartsWith("AboardCut"))) l.Add(t.gameObject);
            return l.Distinct().ToList();
        }
        static int InFrame(Camera cam, IEnumerable<GameObject> roots, bool enabledOnly)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(cam); int n = 0;
            foreach (var g in roots) if (g != null) foreach (var r in g.GetComponentsInChildren<Renderer>(true)) if ((!enabledOnly || (r.enabled && r.gameObject.activeInHierarchy)) && GeometryUtility.TestPlanesAABB(planes, r.bounds)) n++;
            return n;
        }
        static IEnumerator Still(ChapterCapture cap, Camera cam, string name, string shot)
        {
            var roots = Theirs(); int before = InFrame(cam, roots, true);
            var off = new List<Renderer>(); foreach (var g in roots) foreach (var r in g.GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; off.Add(r); }
            var canv = TextOff();
            yield return null; yield return null;                                    // (screens that film the level - the Cat-a-tron - update too)
            yield return new WaitForEndOfFrame();
            int after = InFrame(cam, roots, true);
            var dir = Path.Combine(DemoBoot.CaptureDir, "recstills"); Directory.CreateDirectory(dir);
            var gate = ImageGate.Save(cam.targetTexture, cam, dir, name + ".png");
            foreach (var r in off) if (r != null) r.enabled = true; foreach (var c in canv) if (c != null) c.enabled = true;
            var json = Path.Combine(dir, "received_check.json");
            var lines = File.Exists(json) ? File.ReadAllLines(json).Where(l => l.Contains("\":")).Select(l => l.TrimEnd(',')).ToList() : new List<string>();
            lines.RemoveAll(l => l.Contains("\"" + name + "\""));
            lines.Add($"  \"{name}\": {{ \"us_in_frame\": {after}, \"hidden\": {before}, \"source\": \"{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} check run, shot {shot}, 7 / Panther / bike / saucer hidden\" }}");
            File.WriteAllText(json, "{\n" + string.Join(",\n", lines) + "\n}\n");
            cap.Log($"RECEIVED STILL (QF31 4): {name} from '{shot}' - their renderers in frame before {before}, after hiding {after} (must be 0); {gate}");
        }
    }
}
