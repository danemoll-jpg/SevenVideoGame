using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Seven
{
    /// <summary>
    /// P2 (5): where a scene's frame time goes, measured in the game itself (no profiler on Dan's PC): `-perfprobe <s>` - once per
    /// scene, <s> seconds after it loads (a calm moment), the median real frame time is taken for 3 s as it is, then with one thing
    /// switched off at a time (the grass, every shadow, post-processing, particles, reflection probes, skinned meshes, the height
    /// fog pass), each switched back on before the next. Logs `PERF PROBE (P2 5) <scene>: as is x ms; grass off y ms (-d); ...`.
    /// Check runs only; the numbers are relative (the run is minimized at Below Normal).
    /// </summary>
    public class PerfProbe : MonoBehaviour
    {
        public static float After = -1f;
        static readonly HashSet<string> _done = new HashSet<string>();
        public static void Install()
        {
            if (After < 0f || DemoBoot.CaptureDir == null) return;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) =>
            {
                if (s.name == "Menu" || _done.Contains(s.name)) return;
                _done.Add(s.name);
                var go = new GameObject("PerfProbe"); go.AddComponent<PerfProbe>();
            };
        }

        IEnumerator Start()
        {
            double t0 = Time.realtimeSinceStartupAsDouble; while (Time.realtimeSinceStartupAsDouble - t0 < After) yield return null;   // real seconds (check runs step a fixed game time)
            var scene = gameObject.scene.name; var sb = new System.Text.StringBuilder($"PERF PROBE (P2 5) {scene}: ");
            float baseMs = 0f;
            var tests = new (string name, System.Func<System.Action> off)[]
            {
                ("as is", () => () => { }),
                ("grass off", () => Toggle(r => r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.name.Contains("Grass"))),
                ("shadows off", ShadowsOff),
                ("post off", PostOff),
                ("particles off", () => Toggle(r => r is ParticleSystemRenderer)),
                ("reflection probes off", ProbesOff),
                ("skinned meshes off", () => Toggle(r => r is SkinnedMeshRenderer)),
                ("atmosphere component off", FogOff),
            };
            foreach (var (name, off) in tests)
            {
                var restore = off();
                for (int i = 0; i < 20; i++) yield return null;                       // settle
                var list = new List<float>();
                double a0 = Time.realtimeSinceStartupAsDouble, prev = a0; while (Time.realtimeSinceStartupAsDouble - a0 < 3.0) { yield return null; double now = Time.realtimeSinceStartupAsDouble; list.Add((float)((now - prev) * 1000.0)); prev = now; }
                restore();
                list.Sort(); float med = list.Count > 0 ? list[list.Count / 2] : 0f;
                if (name == "as is") { baseMs = med; sb.Append($"as is {med:0.0} ms"); }
                else sb.Append($"; {name} {med:0.0} ms ({med - baseMs:+0.0;-0.0})");
            }
            ChapterCapture.I?.Log(sb.ToString());
            Debug.Log("[Seven] " + sb);
            Destroy(gameObject);
        }

        static System.Action Toggle(System.Func<Renderer, bool> pick)
        {
            var off = new List<Renderer>();
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if (r.enabled && pick(r)) { r.enabled = false; off.Add(r); }
            return () => { foreach (var r in off) if (r != null) r.enabled = true; };
        }
        static System.Action ShadowsOff()
        {
            var keep = new List<(Light, LightShadows)>();
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.shadows != LightShadows.None) { keep.Add((l, l.shadows)); l.shadows = LightShadows.None; }
            return () => { foreach (var (l, s) in keep) if (l != null) l.shadows = s; };
        }
        static System.Action PostOff()
        {
            var keep = new List<UniversalAdditionalCameraData>();
            foreach (var c in FindObjectsByType<UniversalAdditionalCameraData>(FindObjectsSortMode.None)) if (c.renderPostProcessing) { c.renderPostProcessing = false; keep.Add(c); }
            return () => { foreach (var c in keep) if (c != null) c.renderPostProcessing = true; };
        }
        static System.Action ProbesOff()
        {
            var keep = new List<Behaviour>();
            foreach (var p in FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None)) if (p.enabled) { p.enabled = false; keep.Add(p); }
            return () => { foreach (var p in keep) if (p != null) p.enabled = true; };
        }
        static System.Action FogOff()
        {
            // the height fog full-screen pass reads a global switch the Atmosphere sets; switching the Atmosphere component off is enough
            var keep = new List<Behaviour>();
            foreach (var a in FindObjectsByType<Atmosphere>(FindObjectsSortMode.None)) if (a.enabled) { a.enabled = false; keep.Add(a); }
            return () => { foreach (var a in keep) if (a != null) a.enabled = true; };
        }
    }
}
