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

        /// <summary>Called by ChapterCapture after every capture.</summary>
        public static void OnShot(ChapterCapture cap, Camera cam, string shot)
        {
            if (DemoBoot.CaptureDir == null || cam == null) return; Parse();
            foreach (var kv in _clips) if (!_done.Contains("c:" + kv.Key) && shot.Contains(kv.Key) && !Recording) { _done.Add("c:" + kv.Key); cap.StartCoroutine(Record(cap, cam, kv.Value.name, kv.Value.secs)); }
            foreach (var kv in _stills) if (!_done.Contains("s:" + kv.Key) && shot.Contains(kv.Key)) { _done.Add("s:" + kv.Key); cap.StartCoroutine(Still(cap, cam, kv.Value, shot)); }
        }
        public static bool AllDone { get { Parse(); return _done.Count >= _clips.Count + _stills.Count && !Recording; } }

        static List<Canvas> TextOff() { var l = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.enabled).ToList(); foreach (var c in l) c.enabled = false; return l; }

        static IEnumerator Record(ChapterCapture cap, Camera cam, string name, float secs)
        {
            Recording = true;
            var dir = Path.Combine(DemoBoot.CaptureDir, "memclips", name); Directory.CreateDirectory(dir);
            var hdr = ImageGate.NewRT(1920, 1044); var ldr = new RenderTexture(1920, 1044, 0, RenderTextureFormat.ARGB32); var tex = new Texture2D(1920, 1044, TextureFormat.RGB24, false);
            var was = cam.targetTexture; cam.targetTexture = hdr;
            var canv = TextOff();
            float t0 = Time.time, next = 0f; int n = 0;
            while (Time.time - t0 < secs)
            {
                yield return new WaitForEndOfFrame();
                if (Time.time - t0 + 1e-4f < next) continue;
                next += 1f / 25f;
                Graphics.Blit(hdr, ldr);
                var prev = RenderTexture.active; RenderTexture.active = ldr; tex.ReadPixels(new Rect(0, 0, 1920, 1044), 0, 0); tex.Apply(false); RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(dir, $"f{++n:0000}.jpg"), tex.EncodeToJPG(93));
            }
            foreach (var c in canv) if (c != null) c.enabled = true;
            cam.targetTexture = was; hdr.Release(); ldr.Release(); Object.Destroy(tex);
            cap.Log($"MEMORY CLIP (QF31 A3): {name} - {n} frames ({n / 25f:0.0} s at 25 fps, 1920x1044, no screen text) from the player's camera -> memclips/{name}/");
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
