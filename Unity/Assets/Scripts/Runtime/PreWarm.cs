using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seven
{
    /// <summary>
    /// P2 (5): the load warm-up hitches (the saucer's first 2 s after loading: 130-870 ms frames as shaders, pipeline states and
    /// textures were first used on screen). On every scene load, on its first frames (still behind the fade from black), the main
    /// camera's surroundings are rendered once in six directions (90 deg each, a small 256 px target, post-processing on) from the
    /// camera and from up to <see cref="MaxExtra"/> extra places the scene names (<see cref="Spots"/>: e.g. the finale's crossing),
    /// so everything in view has been drawn once before the player sees it. Logs `PREWARM (P2 5) <scene>: N views in X ms`.
    /// </summary>
    public class PreWarm : MonoBehaviour
    {
        public const int MaxExtra = 6;
        /// <summary>Extra places to warm from (scene name -> world points), set by a scene's director before or at load.</summary>
        public static readonly System.Collections.Generic.Dictionary<string, Vector3[]> Spots = new System.Collections.Generic.Dictionary<string, Vector3[]>();
        public static bool Off;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]   // so the first scene of a run gets it too
        static void Install()
        {
            foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-noprewarm") Off = true;
            // the finale's spikes were along the route from the berth over the crossing to the chamber (P1 4: 120-180 ms near 67 s)
            var up = Vector3.up * 1.6f;
            Spots["Finale"] = new[] { FinaleLayout.Overlook + up, FinaleLayout.WorkerAt + up, FinaleLayout.GateAt + up + Vector3.back * 6f, FinaleLayout.DoorView + up, FinaleLayout.ChamberC + up, FinaleLayout.CavernC + up };
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => { if (!Off && s.name != "Menu") new GameObject("PreWarm").AddComponent<PreWarm>(); };
        }

        IEnumerator Start()
        {
            yield return null; yield return null;                                 // the scene's cameras and directors are set up
            var main = Camera.main; if (main == null) { Destroy(gameObject); yield break; }
            var t0 = Time.realtimeSinceStartupAsDouble;
            var rt = new RenderTexture(256, 256, 24, RenderTextureFormat.DefaultHDR); rt.Create();
            var go = new GameObject("PreWarmCam"); var c = go.AddComponent<Camera>(); c.CopyFrom(main); c.enabled = false; c.fieldOfView = 90f; c.aspect = 1f;
            var src = main.GetComponent<UniversalAdditionalCameraData>(); var dst = go.AddComponent<UniversalAdditionalCameraData>();
            if (src != null) { dst.renderPostProcessing = src.renderPostProcessing; dst.antialiasing = AntialiasingMode.None; }
            var places = new System.Collections.Generic.List<Vector3> { main.transform.position };
            var sceneName = gameObject.scene.name;
            if (Spots.TryGetValue(sceneName, out var extra)) for (int i = 0; i < extra.Length && i < MaxExtra; i++) places.Add(extra[i]);
            var dirs = new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down };
            int views = 0;
            foreach (var p in places)
                foreach (var d in dirs)
                {
                    go.transform.SetPositionAndRotation(p, Quaternion.LookRotation(d, Mathf.Abs(d.y) > 0.5f ? Vector3.forward : Vector3.up));
                    var req = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
                    if (RenderPipeline.SupportsRenderRequest(c, req)) RenderPipeline.SubmitRenderRequest(c, req);
                    else { c.targetTexture = rt; c.Render(); c.targetTexture = null; }
                    views++;
                }
            rt.Release(); Destroy(rt); Destroy(go);
            var ms = (Time.realtimeSinceStartupAsDouble - t0) * 1000.0;
            var line = $"PREWARM (P2 5) {sceneName}: {views} views from {places.Count} place(s) drawn once on load in {ms:0} ms (behind the fade)";
            Debug.Log("[Seven] " + line); if (DemoBoot.CaptureDir != null) ChapterCapture.I?.Log(line);
            FrameProbe.CaptureFrame = Time.frameCount;                              // this load frame is not a play hitch
            Destroy(gameObject);
        }
    }
}
