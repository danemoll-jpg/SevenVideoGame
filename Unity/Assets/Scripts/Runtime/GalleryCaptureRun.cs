using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// Round 5 check run (-gallery -capture dir, implies -saucer): the art TV across the deck (idle slideshow + a
    /// cross-fade), 7 walks to it (prompt + walking legends), viewing mode (both legends), next / previous / wrap-around,
    /// full view and back, an aspect-ratio measurement of every image (probe mode: image area drawn solid white, its
    /// bounding box measured in the rendered frame), leaving. Off-screen RenderTexture, writes G*.png + capture_log.txt,
    /// quits. Never needs the window or focus.
    /// </summary>
    public class GalleryCaptureRun : MonoBehaviour
    {
        SaucerDirector _d;
        GalleryTV _g;
        string _dir;
        RenderTexture _rt;
        readonly StringBuilder _log = new StringBuilder();
        float _t0;

        public void Init(SaucerDirector d)
        {
            _d = d; _g = d.Gallery;
            _dir = DemoBoot.CaptureDir;
            Directory.CreateDirectory(_dir);
            _rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            _d.Cam.GetComponent<Camera>().targetTexture = _rt;
            _d.Seven.ScriptDriven = true;
            _g.ScriptDriven = true;
            _t0 = Time.realtimeSinceStartup;
            Log($"gallery capture start, muted={DemoBoot.Muted}, listenerVolume={AudioListener.volume}");
            StartCoroutine(Run());
            StartCoroutine(Watchdog());
        }

        void Log(string s)
        {
            var line = $"[{Time.realtimeSinceStartup - _t0,6:0.00}s] {s}";
            _log.AppendLine(line);
            Debug.Log("[Seven] " + line);
        }

        Texture2D Grab()
        {
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            var tex = new Texture2D(_rt.width, _rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            return tex;
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = Grab();
            File.WriteAllBytes(Path.Combine(_dir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
            Log($"shot {name}: tv={_g.Current} full={_g.Full} index={_g.Index + 1}/{_g.Items.Count} fade={_g.FadeProgress:0.00} seven={_d.Seven.Current}");
        }

        IEnumerator Until(Func<bool> cond, float timeout, string what)
        {
            float end = Time.time + timeout;
            while (!cond() && Time.time < end) yield return null;
            if (!cond()) Log($"TIMEOUT waiting for {what}");
        }

        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        void LogAudio()
        {
            var buf = new float[512];
            double sum = 0; int n = 0;
            for (int ch = 0; ch < 2; ch++)
            {
                AudioListener.GetOutputData(buf, ch);
                foreach (var s in buf) { sum += s * s; n++; }
            }
            Log($"audio: listenerVolume={AudioListener.volume} paused={AudioListener.pause} outputRMS={Math.Sqrt(sum / n):0.000000}");
        }

        void Place(Vector3 pos, Vector3 look)
        {
            _d.Cam.Hold = true;
            _d.Cam.transform.position = pos;
            _d.Cam.transform.rotation = Quaternion.LookRotation(look - pos);
        }

        IEnumerator WalkTo(Vector3 target, float timeout)
        {
            var s = _d.Seven;
            float end = Time.time + timeout;
            while (Time.time < end)
            {
                var to = target - s.transform.position; to.y = 0f;
                if (to.magnitude < 0.2f) break;
                var f = _d.Cam.transform.forward; f.y = 0f; f.Normalize();
                var r = new Vector3(f.z, 0f, -f.x);
                to.Normalize();
                s.ScriptMove = new Vector2(Vector3.Dot(to, r), Vector3.Dot(to, f));
                yield return null;
            }
            s.ScriptMove = Vector2.zero;
            var left = target - s.transform.position; left.y = 0f;
            Log($"walk to {target}: ended {left.magnitude:0.00} m away");
        }

        static Vector3 Polar(float r, float a, float y) => new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * r, y, Mathf.Sin(a * Mathf.Deg2Rad) * r);

        IEnumerator Run()
        {
            var g = _g; var legend = _d.Legend; var seven = _d.Seven;
            if (legend != null) { legend.AutoSwitch = false; legend.gameObject.SetActive(false); }
            yield return Until(() => g.Loaded, 30f, "gallery loaded");
            Log(g.LoadLog.TrimEnd());
            int files = 0;
            if (Directory.Exists(GalleryTV.GalleryDir))
                foreach (var f in Directory.GetFiles(GalleryTV.GalleryDir))
                {
                    var e = Path.GetExtension(f).ToLowerInvariant();
                    if (e == ".jpg" || e == ".jpeg" || e == ".png") files++;
                }
            Log($"image count: loaded {g.Items.Count}, included in gallery.json {g.IncludedInJson}, image files in StreamingAssets/Gallery {files} -> {(g.Items.Count == g.IncludedInJson && files == g.IncludedInJson ? "MATCH" : "MISMATCH")}");
            yield return new WaitForSeconds(1f);
            LogAudio();

            var tv = g.transform;
            // 1. Across the deck: the idle slideshow reads as a lit screen.
            Place(Polar(5.2f, 150f, 1.75f), tv.position + Vector3.down * 0.3f);
            yield return Frames(3);
            yield return Shot("G01_tv_across_deck");
            // Idle timing: wait for the next automatic change, shoot mid-fade.
            int i0 = g.Index; float tw = Time.time;
            yield return Until(() => g.Index != i0, 12f, "idle slideshow step");
            Log($"idle slideshow stepped {i0 + 1} -> {g.Index + 1} after {Time.time - tw:0.0}s of waiting (interval {g.IdleSeconds}s, fade {g.IdleFade}s)");
            yield return Until(() => g.FadeProgress >= 0.5f, 3f, "idle fade half");
            yield return Shot("G02_tv_idle_crossfade");
            float tStep = Time.time; int i1 = g.Index;
            yield return Until(() => g.Index != i1, 14f, "second idle step");
            Log($"idle slideshow: next automatic step {Time.time - tStep:0.0}s later (expected ~{g.IdleSeconds + g.IdleFade * 0.5f:0.0}s = {g.IdleSeconds}s hold + rest of the {g.IdleFade}s fade)");
            // Nook close-up: sofa facing the screen.
            Place(Polar(2.3f, 335f, 2.15f), tv.position + Vector3.down * 0.5f);
            yield return Until(() => g.FadeProgress >= 1f, 3f, "idle fade done");
            yield return Frames(2);
            yield return Shot("G03_nook");
            _d.Cam.Hold = false;

            // 2. 7 walks to the TV (around the table and the end of the sofa).
            foreach (var wp in new[] { new Vector3(2.2f, 0f, -4.6f), new Vector3(3.6f, 0f, -1.0f), new Vector3(6.2f, 0f, -1.15f), Polar(7.7f, 335f, 0f) })
                yield return WalkTo(wp, 10f);
            yield return Frames(20);
            Log($"7 at {seven.transform.position}, near TV: {g.NearTV}");
            if (!g.NearTV)
            {
                seven.transform.position = Polar(7.7f, 335f, 0f);
                yield return Frames(5);
                Log($"(teleported to the TV spot) near TV: {g.NearTV}");
            }
            // Face the TV, camera behind her.
            var toTv = tv.position - seven.transform.position; toTv.y = 0f;
            seven.transform.rotation = Quaternion.LookRotation(toTv);
            _d.Cam.Yaw = Mathf.Atan2(toTv.x, toTv.z) * Mathf.Rad2Deg; _d.Cam.Pitch = _d.Cam.WalkPitch;
            yield return new WaitForSeconds(1.0f);
            Log($"walk camera at the TV: {Vector3.Distance(_d.Cam.transform.position, seven.transform.position + Vector3.up * _d.Cam.WalkLookHeight):0.00} m from 7's focus (wanted {_d.Cam.WalkDistance})");
            if (legend != null)
            {
                legend.gameObject.SetActive(true);
                legend.SetMode(true); yield return Frames(3); yield return Shot("G04_prompt_walk_xbox");
                legend.SetMode(false); yield return Frames(3); yield return Shot("G05_prompt_walk_keys");
                legend.SetMode(true);
            }

            // 3. Viewing mode.
            g.Enter();
            yield return Until(() => g.InView, 1f, "enter");
            yield return new WaitForSeconds(g.EaseSeconds * 0.45f);
            yield return Shot("G06_entering");
            yield return Until(() => g.Current == GalleryTV.State.Viewing, 3f, "viewing");
            yield return new WaitForSeconds(0.6f);
            int first = g.Index;
            yield return Shot("G07_view_xbox");
            if (legend != null) { legend.SetMode(false); yield return Frames(3); yield return Shot("G08_view_keys"); legend.SetMode(true); }
            var cam = _d.Cam.transform;
            Log($"view camera {Vector3.Distance(cam.position, tv.position):0.00} m from the screen centre, angle to its normal {Vector3.Angle(cam.forward, tv.forward):0.0} deg; 7 hidden: {!seven.GetComponentInChildren<Renderer>().enabled}");

            // Next / previous / wrap-around.
            g.Next();
            yield return new WaitForSeconds(g.StepFade * 0.5f);
            yield return Shot("G09_next_crossfade");
            yield return new WaitForSeconds(g.StepFade + 0.2f);
            yield return Shot("G10_next");
            Log($"next: {first + 1} -> {g.Index + 1}");
            g.Prev();
            yield return new WaitForSeconds(g.StepFade + 0.2f);
            yield return Shot("G11_prev");
            Log($"prev: back to {g.Index + 1} (expected {first + 1})");
            // From the first image, previous wraps to the last.
            while (g.Index != 0) { g.Prev(); yield return new WaitForSeconds(g.StepFade + 0.05f); }
            g.Prev();
            yield return new WaitForSeconds(g.StepFade + 0.2f);
            yield return Shot("G12_wrap_to_last");
            Log($"wrap: previous from 1 -> {g.Index + 1} (expected {g.Items.Count})");
            g.Next();
            yield return new WaitForSeconds(g.StepFade + 0.2f);
            Log($"wrap: next from {g.Items.Count} -> {g.Index + 1} (expected 1)");
            // Show a landscape-wide and a narrow piece on the screen.
            int wide = 0, narrow = 0;
            for (int i = 0; i < g.Items.Count; i++)
            {
                if (g.Items[i].Aspect > g.Items[wide].Aspect) wide = i;
                if (g.Items[i].Aspect < g.Items[narrow].Aspect) narrow = i;
            }
            yield return GoTo(narrow);
            yield return Shot("G13_view_narrowest");

            // 4. Full view.
            g.ToggleFull();
            yield return Frames(3);
            yield return Shot("G14_full_xbox");
            if (legend != null) { legend.SetMode(false); yield return Frames(3); yield return Shot("G15_full_keys"); legend.SetMode(true); }
            yield return GoTo(wide);
            yield return Shot("G16_full_widest");
            g.ToggleFull();
            yield return Frames(3);
            yield return Shot("G17_full_back_to_screen");

            // 5. Aspect ratios, measured: probe mode draws each image area solid white on black.
            yield return MeasureAspects();

            // 6. Leave.
            g.Leave();
            yield return new WaitForSeconds(g.EaseSeconds * 0.45f);
            yield return Shot("G20_leaving");
            yield return Until(() => g.Current == GalleryTV.State.Off, 3f, "left");
            yield return new WaitForSeconds(0.4f);
            yield return Shot("G21_left");
            Log($"left: camera hold={_d.Cam.Hold}, seven frozen={seven.Frozen}, seven visible={seven.GetComponentInChildren<Renderer>().enabled}, legend layout={legend?.Set}");
            // 7 can walk again.
            var p0 = seven.transform.position;
            yield return WalkTo(Polar(6.2f, 300f, 0f), 4f);
            Log($"after leaving 7 walked {Vector3.Distance(p0, seven.transform.position):0.00} m");
            yield return WalkTo(Polar(7.7f, 335f, 0f), 6f);
            yield return InputTest();
            LogAudio();
            Finish("done");
        }

        IEnumerator GoTo(int index)
        {
            int guard = 0;
            while (_g.Index != index && guard++ < 64) { _g.Next(); yield return new WaitForSeconds(_g.StepFade + 0.05f); }
            yield return new WaitForSeconds(0.25f);
        }

        IEnumerator MeasureAspects()
        {
            var g = _g; var legend = _d.Legend;
            bool legendWas = legend != null && legend.gameObject.activeSelf;
            if (legend != null) legend.gameObject.SetActive(false);
            var others = new System.Collections.Generic.List<Renderer>();
            foreach (var r in g.GetComponentsInChildren<Renderer>()) if (r != g.Screen && r.enabled) { r.enabled = false; others.Add(r); }
            g.Probe = true;
            float worstScreen = 0f, worstFull = 0f;
            var cam = _d.Cam.GetComponent<Camera>();
            for (int i = 0; i < g.Items.Count; i++)
            {
                yield return GoTo(i);
                foreach (bool full in new[] { false, true })
                {
                    if (g.Full != full) g.ToggleFull();
                    yield return Frames(3);
                    yield return new WaitForEndOfFrame();
                    var tex = Grab();
                    if (i == 0) File.WriteAllBytes(Path.Combine(_dir, full ? "G19_probe_full.png" : "G18_probe_screen.png"), tex.EncodeToPNG());
                    // Screen view: only look inside the projected screen quad, 1 px in (the wall around it is lit).
                    RectInt area = new RectInt(0, 0, tex.width, tex.height);
                    if (!full)
                    {
                        var t = g.transform; var hs = g.ScreenSize * 0.5f;
                        Vector2 a = cam.WorldToScreenPoint(t.TransformPoint(new Vector3(-hs.x, -hs.y, 0f)));
                        Vector2 b = cam.WorldToScreenPoint(t.TransformPoint(new Vector3(hs.x, hs.y, 0f)));
                        float sx = tex.width / (float)cam.pixelWidth, sy = tex.height / (float)cam.pixelHeight;
                        int x0 = Mathf.FloorToInt(Mathf.Min(a.x, b.x) * sx) + 1, x1 = Mathf.CeilToInt(Mathf.Max(a.x, b.x) * sx) - 1;
                        int y0 = Mathf.FloorToInt(Mathf.Min(a.y, b.y) * sy) + 1, y1 = Mathf.CeilToInt(Mathf.Max(a.y, b.y) * sy) - 1;
                        area = new RectInt(Mathf.Max(0, x0), Mathf.Max(0, y0), Mathf.Min(tex.width, x1) - Mathf.Max(0, x0), Mathf.Min(tex.height, y1) - Mathf.Max(0, y0));
                    }
                    var px = tex.GetPixels32();
                    int minX = int.MaxValue, maxX = -1, minY = int.MaxValue, maxY = -1;
                    for (int y = area.yMin; y < area.yMax; y++)
                        for (int x = area.xMin; x < area.xMax; x++)
                            if (px[y * tex.width + x].g > 170) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
                    Destroy(tex);
                    if (maxX < 0) { Log($"aspect {i + 1} {(full ? "full" : "screen")}: no image pixels found"); continue; }
                    float w = maxX - minX + 1, h = maxY - minY + 1;
                    float measured = w / h, want = g.Items[i].Aspect;
                    float err = Mathf.Abs(measured / want - 1f) * 100f;
                    if (full) worstFull = Mathf.Max(worstFull, err); else worstScreen = Mathf.Max(worstScreen, err);
                    Log($"aspect {i + 1,2} {(full ? "full  " : "screen")} {g.Items[i].File}: source {g.Items[i].Size.x}x{g.Items[i].Size.y} = {want:0.000}, shown {w}x{h} px = {measured:0.000} ({err:0.0}% off)");
                }
                if (g.Full) g.ToggleFull();
            }
            g.Probe = false;
            foreach (var r in others) r.enabled = true;
            if (legend != null) legend.gameObject.SetActive(legendWas);
            Log($"aspect summary: worst error on the screen {worstScreen:0.0}%, in full view {worstFull:0.0}% (1 px of rounding on a ~300 px tall image is ~0.3%)");
            yield return GoTo(0);
        }

        // ------------------------------------------------------------------ real input path (virtual devices)
        Keyboard _kb; Gamepad _gp;
        int _pass, _fail;

        void Check(string what, bool ok) { if (ok) _pass++; else _fail++; Log($"input {(ok ? "PASS" : "FAIL")}: {what}"); }

        IEnumerator PressKey(Key k)
        {
            InputSystem.QueueStateEvent(_kb, new KeyboardState(k));
            yield return Frames(2);
            InputSystem.QueueStateEvent(_kb, new KeyboardState());
            yield return Frames(2);
        }

        IEnumerator Pad(GamepadButton b)
        {
            InputSystem.QueueStateEvent(_gp, new GamepadState().WithButton(b));
            yield return Frames(2);
            InputSystem.QueueStateEvent(_gp, new GamepadState());
            yield return Frames(2);
        }

        IEnumerator Settle() { yield return new WaitForSeconds(_g.StepFade + 0.1f); }

        /// <summary>Drive the gallery through its real bindings (SaucerInput actions) with a virtual keyboard and pad.</summary>
        IEnumerator InputTest()
        {
            var g = _g;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;   // the run never has focus
            _kb = InputSystem.AddDevice<Keyboard>("CheckKeyboard");
            _gp = InputSystem.AddDevice<Gamepad>("CheckPad");
            g.ScriptDriven = false;
            _d.LiveInputInCapture = true;
            yield return Frames(3);
            Log($"input test: near TV {g.NearTV}");

            // Keyboard.
            yield return PressKey(UnityEngine.InputSystem.Key.E);
            Check("E enters viewing", g.InView);
            yield return Until(() => g.Current == GalleryTV.State.Viewing, 3f, "viewing (keys)");
            int i = g.Index, n = g.Items.Count;
            yield return PressKey(UnityEngine.InputSystem.Key.RightArrow); yield return Settle();
            Check($"right arrow = next ({i + 1} -> {g.Index + 1})", g.Index == (i + 1) % n);
            yield return PressKey(UnityEngine.InputSystem.Key.LeftArrow); yield return Settle();
            Check($"left arrow = previous (back to {g.Index + 1})", g.Index == i);
            yield return PressKey(UnityEngine.InputSystem.Key.F);
            Check("F = full view", g.Full);
            yield return PressKey(UnityEngine.InputSystem.Key.F);
            Check("F again = back to the screen", !g.Full);
            yield return PressKey(UnityEngine.InputSystem.Key.Escape);
            yield return Frames(5);
            Check($"Esc leaves the TV (state {g.Current}) and stays in the saucer (scene {SceneManager.GetActiveScene().name})",
                  !g.InView && SceneManager.GetActiveScene().name == "Saucer");
            yield return Until(() => g.Current == GalleryTV.State.Off, 3f, "off (keys)");
            yield return Frames(5);

            // Gamepad.
            yield return Pad(GamepadButton.South);
            Check("A enters viewing", g.InView);
            yield return Until(() => g.Current == GalleryTV.State.Viewing, 3f, "viewing (pad)");
            i = g.Index;
            yield return Pad(GamepadButton.RightShoulder); yield return Settle();
            Check($"RB = next ({i + 1} -> {g.Index + 1})", g.Index == (i + 1) % n);
            yield return Pad(GamepadButton.LeftShoulder); yield return Settle();
            Check("LB = previous", g.Index == i);
            yield return Pad(GamepadButton.DpadRight); yield return Settle();
            Check("d-pad right = next", g.Index == (i + 1) % n);
            yield return Pad(GamepadButton.DpadLeft); yield return Settle();
            Check("d-pad left = previous", g.Index == i);
            InputSystem.QueueStateEvent(_gp, new GamepadState { leftStick = new Vector2(1f, 0f) });
            yield return new WaitForSeconds(0.2f);
            InputSystem.QueueStateEvent(_gp, new GamepadState());
            yield return Settle();
            Check($"left stick right (0.2 s) = one step next ({g.Index + 1})", g.Index == (i + 1) % n);
            InputSystem.QueueStateEvent(_gp, new GamepadState { leftStick = new Vector2(-1f, 0f) });
            yield return new WaitForSeconds(0.2f);
            InputSystem.QueueStateEvent(_gp, new GamepadState());
            yield return Settle();
            Check("left stick left = one step previous", g.Index == i);
            yield return Pad(GamepadButton.North);
            Check("Y = full view", g.Full);
            yield return Pad(GamepadButton.North);
            Check("Y again = back to the screen", !g.Full);
            yield return Pad(GamepadButton.East);
            yield return Frames(3);
            Check("B leaves the TV", !g.InView);
            yield return Until(() => g.Current == GalleryTV.State.Off, 3f, "off (pad)");
            Check("7 can move again after leaving", !_d.Seven.Frozen);

            _d.LiveInputInCapture = false;
            g.ScriptDriven = true;
            InputSystem.RemoveDevice(_kb); InputSystem.RemoveDevice(_gp);
            Log($"input test: {_pass} passed, {_fail} failed");
        }

        IEnumerator Watchdog()
        {
            yield return new WaitForSecondsRealtime(300f);
            Finish("WATCHDOG timeout");
        }

        bool _finished;

        void Finish(string why)
        {
            if (_finished) return;
            _finished = true;
            Log($"capture {why}");
            File.WriteAllText(Path.Combine(_dir, "capture_log.txt"), _log.ToString());
            Application.Quit();
        }
    }
}
