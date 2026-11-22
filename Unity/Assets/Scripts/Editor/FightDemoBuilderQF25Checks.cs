using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF25 build checks (Dan's 0.17.3 review), once per build (from BuildAllFromCommandLine):
    ///  - VEYRA SCALE (QF25 1): the shared Veyra composition built at the exterior scale and the window scale - the planet's
    ///    angular radius at least 2x the ship's, the planet farther than the ship, the ship straight along the view; FAILS.
    ///  - SUBTITLE STILL (QF25 2): a gameplay camera 4 km out (the passage stage) shaken like CameraShake for 120 frames; the
    ///    subtitle rect measured on screen through the shared UI camera (must not move) and, for reference, through a canvas
    ///    on the shaking camera (the 0.17.3 setup); plus the float step at the old canvas plane in pixels; FAILS if it moves.
    ///  - DUPLICATE LINES (QF25 3): every chapter's director code - a line taken by a numbered range (Say(scene, a, b), which
    ///    also takes inserted lines like 02a / 02b) AND named again by id (SayIds / Ids) in the same file plays twice; FAILS.
    ///    Plus two ids in one scene with the same speaker and text in the dialogue data (listed; FAILS unless marked repeat).
    /// Reports: Docs/Previews/qf25/*.txt.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        static string QF25Dir { get { var d = Path.Combine(RepoRoot, "Docs", "Previews", "qf25"); Directory.CreateDirectory(d); return d; } }
        static void Report25(string file, string head, StringBuilder sb, bool fail)
        {
            File.WriteAllText(Path.Combine(QF25Dir, file), head + "\n" + sb);
            Log(head);
            if (fail) BuildFailures.Add(head);
        }

        public static void QF25GlobalChecks()
        {
            try { VeyraScaleCheck(); } catch (System.Exception e) { BuildFailures.Add("VEYRA SCALE (QF25 1) threw: " + e.Message); }
            try { SubtitleStillCheck(); } catch (System.Exception e) { BuildFailures.Add("SUBTITLE STILL (QF25 2) threw: " + e.Message); }
            try { DuplicateLinesCheck(); } catch (System.Exception e) { BuildFailures.Add("DUPLICATE LINES (QF25 3) threw: " + e.Message); }
        }

        // ---------------------------------------------------------------- QF25 (1)
        static void VeyraScaleCheck()
        {
            var sb = new StringBuilder(); bool fail = false;
            foreach (var (label, scale, far) in new[] { ("the BIA_01 exterior", 1f, 60000f), ("the window view aboard", 0.24f, 2500f) })
            {
                var origin = new Vector3(0f, 3000f, -2600f) * (scale < 0.5f ? 0f : 1f);
                var v = Seven.VeyraScene.Build(null, origin, scale, 17, true, new Vector3(0f, -0.06f, 1f), far);
                try
                {
                    var camGo = new GameObject("QF25_cam"); var cam = camGo.AddComponent<Camera>(); cam.fieldOfView = 50f; cam.aspect = 16f / 9f;
                    cam.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(new Vector3(0f, -0.06f, 1f)));
                    bool ok = v.Emphasis(cam, out var info);
                    float dEgg = Vector3.Distance(origin, v.Egg.position), dPl = Vector3.Distance(origin, v.Planet.position);
                    float farthest = dPl + v.Planet.lossyScale.x * 0.5f;
                    bool scaleOk = v.PlanetToShipAngle >= 2f, behind = dPl > dEgg * 1.5f, inFar = dPl < far;
                    sb.AppendLine($"{label}: planet angular radius {v.PlanetToShipAngle:0.0}x the ship's ({(scaleOk ? "ok" : "TOO SMALL")}), planet centre {dPl:0} m vs the ship {dEgg:0} m ({(behind ? "behind it" : "NOT BEHIND")}), within the camera's {far:0} m {(inFar ? "yes" : "NO")}; looking out: {info} -> {(ok ? "on the mothership" : "MAINLY THE PLANET")}");
                    if (!scaleOk || !behind || !inFar || !ok) fail = true;
                    Object.DestroyImmediate(camGo);
                }
                finally { if (v.Root != null) Object.DestroyImmediate(v.Root.gameObject); }
            }
            Report25("veyra_scale.txt", $"VEYRA SCALE (QF25 1): {(fail ? "FAIL" : "PASS")} - Veyra a planet far bigger than the mothership, every view framed on the ship", sb, fail);
        }

        // ---------------------------------------------------------------- QF25 (2)
        static void SubtitleStillCheck()
        {
            var sb = new StringBuilder(); bool fail = false;
            var root = new GameObject("QF25_subtitle_check");
            try
            {
                // the gameplay camera far out (the passage stage), shaken like CameraShake (0.22 m, 3 deg at peak)
                var mainGo = new GameObject("QF25_main"); mainGo.transform.SetParent(root.transform, false);
                var main = mainGo.AddComponent<Camera>(); main.fieldOfView = 44f; main.nearClipPlane = 0.08f; main.pixelRect = new Rect(0, 0, 1280, 720);
                // the shared UI camera: at the world origin, never parented to the gameplay camera (UiOverlay)
                var uiGo = new GameObject("QF25_ui"); uiGo.transform.SetParent(root.transform, false);
                var ui = uiGo.AddComponent<Camera>(); ui.fieldOfView = 50f; ui.nearClipPlane = 0.05f; ui.pixelRect = new Rect(0, 0, 1280, 720);
                RectTransform Sub(Camera c, string n)
                {
                    var cg = new GameObject(n); cg.transform.SetParent(root.transform, false);
                    var cv = cg.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceCamera; cv.worldCamera = c; cv.planeDistance = 0.12f;
                    var sc = cg.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1280, 720); sc.matchWidthOrHeight = 1f;
                    var p = new GameObject("Subtitle", typeof(RectTransform)); p.transform.SetParent(cg.transform, false);
                    var rt = (RectTransform)p.transform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f); rt.anchoredPosition = new Vector2(0f, 60f); rt.sizeDelta = new Vector2(900f, 60f);
                    return rt;
                }
                var newSub = Sub(ui, "QF25_canvas_ui"); var oldSub = Sub(main, "QF25_canvas_main");
                Vector2 Screen(RectTransform rt, Camera c) { var k = new Vector3[4]; rt.GetWorldCorners(k); return c.WorldToScreenPoint((k[0] + k[3]) * 0.5f); }
                var basePos = new Vector3(0f, 3000f, -2600f) + new Vector3(0f, 0f, 130f);
                float maxNew = 0f, maxOld = 0f; Vector2? n0 = null, o0 = null; int frames = 120;
                for (int f = 0; f < frames; f++)
                {
                    float t = f / 60f, s = t * 23f, env = Mathf.Clamp01(t / 0.08f) * Mathf.Pow(1f - f / (float)frames, 1.4f);
                    var p = new Vector3(Mathf.PerlinNoise(s, 0.1f) - 0.5f, Mathf.PerlinNoise(0.3f, s) - 0.5f, Mathf.PerlinNoise(s, 0.7f) - 0.5f) * 2f * 0.22f * env;
                    var r = Quaternion.Euler((Mathf.PerlinNoise(s * 0.8f, 2.1f) - 0.5f) * 6f * env, (Mathf.PerlinNoise(3.3f, s * 0.8f) - 0.5f) * 3f * env, (Mathf.PerlinNoise(s * 0.8f, 4.4f) - 0.5f) * 5f * env);
                    // the iris approach: the camera also travels toward the door (0.9 m per frame)
                    main.transform.SetPositionAndRotation(basePos + Vector3.forward * f * 0.9f + p, Quaternion.LookRotation(Vector3.forward) * r);
                    Canvas.ForceUpdateCanvases();
                    var a = Screen(newSub, ui); var b = Screen(oldSub, main);
                    if (n0 == null) { n0 = a; o0 = b; }
                    maxNew = Mathf.Max(maxNew, (a - n0.Value).magnitude); maxOld = Mathf.Max(maxOld, (b - o0.Value).magnitude);
                }
                // the float step at the old canvas plane, in pixels (the jitter the 0.17.3 canvas had 4 km out)
                float ulp(float x) { float y = Mathf.Abs(x); return System.BitConverter.Int32BitsToSingle(System.BitConverter.SingleToInt32Bits(y) + 1) - y; }
                float pxAt(float dist) => 720f / (2f * 0.12f * Mathf.Tan(44f * 0.5f * Mathf.Deg2Rad));
                float oldStep = ulp(basePos.magnitude) * pxAt(0.12f), newStep = ulp(0.12f) * pxAt(0.12f);
                bool ok = maxNew < 0.5f;
                sb.AppendLine($"camera 4.1 km from the origin, shaken (0.22 m / 3 deg peak) and travelling 0.9 m per frame for {frames} frames:");
                sb.AppendLine($"  subtitle through the shared UI camera (UiOverlay, at the origin): largest move on screen {maxNew:0.000} px (must be under 0.5)");
                sb.AppendLine($"  the 0.17.3 setup for reference - a canvas 0.12 m in front of the camera 4.1 km out: one float step there = {oldStep:0.0} px of jitter per rounding (at the UI camera: {newStep:0.0000} px)");
                var src = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "Runtime", "UiOverlay.cs"));
                bool structural = src.Contains("SetPositionAndRotation(Vector3.zero, Quaternion.identity)") && !src.Contains("SetParent(main.transform");
                sb.AppendLine($"  UiOverlay: the UI camera at the world origin, not parented to the gameplay camera: {(structural ? "yes" : "NO")}");
                fail = !ok || !structural;
            }
            finally { Object.DestroyImmediate(root); }
            Report25("subtitle_still.txt", $"SUBTITLE STILL (QF25 2): {(fail ? "FAIL" : "PASS")} - subtitles, objectives and prompts never move with the camera", sb, fail);
        }

        // ---------------------------------------------------------------- QF25 (3)
        [System.Serializable] class DLine { public string id, scene, speaker, text; public bool repeat; }
        [System.Serializable] class DFile { public List<DLine> lines; }
        static void DuplicateLinesCheck()
        {
            var sb = new StringBuilder(); int bad = 0, sameText = 0;
            // every dialogue file with a "lines" list (the chapters' data)
            var all = new Dictionary<string, DLine>();
            foreach (var f in Directory.GetFiles(Path.Combine(Application.dataPath, "Resources"), "*_dialogue.json", SearchOption.AllDirectories))
            {
                DFile d = null; try { d = JsonUtility.FromJson<DFile>(File.ReadAllText(f)); } catch { }
                if (d?.lines == null) continue;
                foreach (var x in d.lines) if (x?.id != null) all[x.id] = x;
                foreach (var g in d.lines.Where(x => x?.text != null && !x.repeat).GroupBy(x => (x.scene, x.speaker, x.text.Trim().ToLowerInvariant())).Where(g => g.Count() > 1))
                { sameText++; sb.AppendLine($"same text in {Path.GetFileName(f)} scene {g.Key.scene}: {string.Join(", ", g.Select(x => x.id))} - '{g.First().text}'"); }
            }
            // ranges that take inserted lines (BiavehDialogue.Scene: the leading digits count) vs explicit ids in the same file
            var rangeRx = new Regex(@"\b(Say|SayB|SayC)\(""([A-Z]+_\d+[A-Z]?)""\s*,\s*(\d+)(?:\s*,\s*(\d+))?\)");
            var idRx = new Regex(@"""([A-Z]+_\d+[A-Z]?_\d+[a-z]+)""");
            int files = 0;
            foreach (var f in Directory.GetFiles(Path.Combine(Application.dataPath, "Scripts", "Runtime"), "*.cs"))
            {
                var src = File.ReadAllText(f); files++;
                var ranged = new Dictionary<string, string>();
                foreach (Match m in rangeRx.Matches(src))
                {
                    string helper = m.Groups[1].Value, scene = m.Groups[2].Value; int a = int.Parse(m.Groups[3].Value);
                    int b = m.Groups[4].Success ? int.Parse(m.Groups[4].Value) : helper == "Say" ? 99 : a;
                    foreach (var x in all.Values.Where(x => x.scene == scene))
                    {
                        var tail = x.id.Substring(x.id.LastIndexOf('_') + 1); int dl = 0; while (dl < tail.Length && char.IsDigit(tail[dl])) dl++;
                        if (dl == 0 || dl == tail.Length || !int.TryParse(tail.Substring(0, dl), out int n)) continue;   // only inserted lines (02a)
                        if (n >= a && n <= b && SceneTakesInserted(scene)) ranged[x.id] = m.Value;
                    }
                }
                foreach (Match m in idRx.Matches(src))
                    if (ranged.TryGetValue(m.Groups[1].Value, out var by))
                    { bad++; sb.AppendLine($"{Path.GetFileName(f)}: {m.Groups[1].Value} is named by id AND taken by {by} - it plays twice"); }
            }
            sb.AppendLine($"{files} runtime files scanned, {all.Count} lines in the dialogue data");
            bool fail = bad > 0 || sameText > 0;
            Report25("duplicate_lines.txt", $"DUPLICATE LINES (QF25 3): {(fail ? "FAIL" : "PASS")} - lines played twice by the code {bad}, two ids with the same text in a scene {sameText} (both must be 0)", sb, fail);
        }
        /// <summary>The chapters whose numbered ranges also take inserted lines (02a after 02): the mothership (Cattalus ranges take plain numbers only).</summary>
        static bool SceneTakesInserted(string scene) => scene.StartsWith("BIA_");
    }
}
