using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Seven
{
    /// <summary>
    /// Round 12 (2d, Dan) — Chapter 1's opening shot: the Moon hanging in space (grayscale, stars, a soft white glow round
    /// it like the galley porthole's Moon), the logo ("7" + THE OBSERVER) fading in over it and out again, a slow push-in
    /// that accelerates toward the surface, the dive through the crust (rock strata, dust and darkness rushing past), the
    /// dark mine tunnels - and then the existing opening fades in on 7 in her dark cell. About 16 s, no dialogue,
    /// skippable (A / Esc / Enter). The hollow interior and the city are never shown (the dive goes from the surface
    /// straight into the tunnels). Drawn in screen space from procedural grayscale textures (no colour, no fog pass).
    /// Plays on "Chapter 1" from the menu and Scene Select's first entry, never on Continue (<see cref="Chapter1State.Intro"/>).
    /// </summary>
    public static class MoonOpening
    {
        public static bool Playing { get; private set; }
        public static bool Skipped { get; private set; }

        static Texture2D Stars(int w, int h, int seed)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false); var px = new Color32[w * h];
            var r = new System.Random(seed);
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 255);
            for (int k = 0; k < 900; k++)
            {
                int x = r.Next(w), y = r.Next(h); byte v = (byte)(90 + r.Next(165)); int s = r.NextDouble() < 0.06 ? 1 : 0;
                for (int dy = -s; dy <= s; dy++) for (int dx = -s; dx <= s; dx++) { int xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue; byte vv = (byte)(dx == 0 && dy == 0 ? v : v / 3); px[yy * w + xx] = new Color32(vv, vv, vv, 255); }
            }
            t.SetPixels32(px); t.Apply(); return t;
        }

        static float Noise(float x, float y) => Mathf.PerlinNoise(x, y);

        /// <summary>The Moon: a lit sphere, maria and craters, alpha outside the disc.</summary>
        static Texture2D MoonTex(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true); var px = new Color[n * n];
            var r = new System.Random(7);
            var craters = new Vector4[260];
            for (int i = 0; i < craters.Length; i++) { float rad = Mathf.Pow((float)r.NextDouble(), 3f) * 0.12f + 0.006f; craters[i] = new Vector4((float)r.NextDouble() * 2f - 1f, (float)r.NextDouble() * 2f - 1f, rad, 0f); }
            var light = new Vector3(-0.55f, 0.45f, 0.7f).normalized;
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = i / (n - 1f) * 2f - 1f, y = j / (n - 1f) * 2f - 1f, rr = x * x + y * y;
                    if (rr > 1f) { px[j * n + i] = new Color(0, 0, 0, 0); continue; }
                    float z = Mathf.Sqrt(1f - rr); var nrm = new Vector3(x, y, z);
                    float shade = Mathf.Clamp01(Vector3.Dot(nrm, light)) * 0.85f + 0.12f;
                    float albedo = 0.72f - 0.22f * Mathf.SmoothStep(0.45f, 0.62f, Noise(x * 2.2f + 3f, y * 2.2f + 7f)) + 0.06f * (Noise(x * 14f, y * 14f) - 0.5f);
                    foreach (var c in craters)
                    {
                        float d = Mathf.Sqrt((x - c.x) * (x - c.x) + (y - c.y) * (y - c.y)) / c.z;
                        if (d > 1.25f) continue;
                        albedo += d < 1f ? -0.08f * (1f - d * d) : 0.06f * (1f - Mathf.Abs(d - 1.1f) / 0.15f);
                    }
                    float edge = Mathf.SmoothStep(1f, 0.985f, rr);
                    float v = Mathf.Clamp01(albedo * shade);
                    px[j * n + i] = new Color(v, v, v, edge);
                }
            t.SetPixels(px); t.Apply(true); return t;
        }

        static Texture2D Glow(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false); var px = new Color[n * n];
            for (int j = 0; j < n; j++) for (int i = 0; i < n; i++) { float x = i / (n - 1f) * 2f - 1f, y = j / (n - 1f) * 2f - 1f; float d = Mathf.Sqrt(x * x + y * y); float a = Mathf.Clamp01(1f - d) ; a = a * a * 0.55f; px[j * n + i] = new Color(1f, 1f, 1f, a); }
            t.SetPixels(px); t.Apply(); return t;
        }

        /// <summary>Close surface: grey regolith full of craters (it fills the screen as the push-in reaches the ground).</summary>
        static Texture2D Surface(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true); var px = new Color[n * n];
            var r = new System.Random(11); var cr = new Vector3[180];
            for (int i = 0; i < cr.Length; i++) cr[i] = new Vector3((float)r.NextDouble(), (float)r.NextDouble(), Mathf.Pow((float)r.NextDouble(), 3f) * 0.09f + 0.005f);
            for (int j = 0; j < n; j++) for (int i = 0; i < n; i++)
                {
                    float x = i / (float)n, y = j / (float)n;
                    float v = 0.5f + 0.15f * (Noise(x * 9f, y * 9f) - 0.5f) + 0.08f * (Noise(x * 40f, y * 40f) - 0.5f);
                    foreach (var c in cr) { float d = Vector2.Distance(new Vector2(x, y), new Vector2(c.x, c.y)) / c.z; if (d > 1.25f) continue; float sh = (x - c.x) - (y - c.y); v += d < 1f ? -0.12f * (1f - d * d) + 0.1f * sh / c.z * (1f - d) : 0.07f * (1f - Mathf.Abs(d - 1.1f) / 0.15f); }
                    v = Mathf.Clamp01(v); px[j * n + i] = new Color(v, v, v, 1f);
                }
            t.SetPixels(px); t.Apply(true); t.wrapMode = TextureWrapMode.Repeat; return t;
        }

        /// <summary>The crust in section: rock strata (bands of grey, broken by noise), tiling vertically.</summary>
        static Texture2D Strata(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true); var px = new Color[w * h];
            for (int j = 0; j < h; j++) for (int i = 0; i < w; i++)
                {
                    float x = i / (float)w, y = j / (float)h;
                    float band = Mathf.PerlinNoise(0.5f, y * 14f + Noise(x * 3f, y * 3f) * 0.8f);
                    float v = 0.12f + 0.38f * band + 0.1f * (Noise(x * 30f, y * 60f) - 0.5f);
                    px[j * w + i] = new Color(v, v, v, 1f);
                }
            t.SetPixels(px); t.Apply(true); t.wrapMode = TextureWrapMode.Mirror; return t;     // mirrored: no seam where it tiles
        }

        static RawImage Img(Transform parent, string name, Texture tex)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            var ri = go.AddComponent<RawImage>(); ri.texture = tex; ri.raycastTarget = false; return ri;
        }
        static void Full(RawImage ri) { var rt = ri.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }

        static bool SkipPressed()
        {
            var kb = Keyboard.current; var gp = Gamepad.current;
            return (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) || (gp != null && gp.buttonSouth.wasPressedThisFrame);
        }

        /// <summary>Play it (on the given camera's screen); ends on black, ready for the cell's fade-in.</summary>
        public static IEnumerator Play(MonoBehaviour host, Camera cam, System.Func<string, IEnumerator> shot, System.Action<string> log)
        {
            Playing = true; Skipped = false;
            var prevEsc = PauseMenu.EscapeOwned; PauseMenu.EscapeOwned = () => Playing;
            MusicSystem.Stop(0.3f);
            var go = new GameObject("MoonOpening");
            var canvas = go.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = Mathf.Max(0.12f, cam.nearClipPlane + 0.05f); canvas.sortingOrder = 300;   // above the chapter UI (its black fade is 200 on the same camera)
            var sc = go.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1280, 720); sc.matchWidthOrHeight = 1f;
            var root = go.transform;
            var group = go.AddComponent<CanvasGroup>();
            var black = Img(root, "Black", Texture2D.whiteTexture); Full(black); black.color = Color.black;
            var stars = Img(root, "Stars", Stars(1280, 720, 3)); Full(stars);
            var glow = Img(root, "Glow", Glow(256)); var moon = Img(root, "Moon", MoonTex(1024));
            var surf = Img(root, "Surface", Surface(1024)); Full(surf); surf.color = new Color(1, 1, 1, 0);
            var strata = Img(root, "Strata", Strata(512, 1024)); Full(strata); strata.color = new Color(1, 1, 1, 0);
            var dark = Img(root, "Dark", Texture2D.whiteTexture); Full(dark); dark.color = new Color(0, 0, 0, 0);
            var logo = Logo.Add(root, new Vector2(0f, 230f), 300f, new Color(0.95f, 0.95f, 0.95f, 0f));
            if (logo != null) logo.color = new Color(1, 1, 1, 0);
            log?.Invoke("opening (2d): the Moon in space, the title, the push-in, the dive through the crust, the tunnels -> 7 in her cell");
            AudioKit.Play("saucer_descent", null, 0.35f);                     // a low swell under the space shot
            float T = 0f; bool shot1 = false, shot2 = false, shot3 = false, rumble = false;
            const float Len = 16f;
            float moonH0 = 300f;                                              // the disc's height in reference pixels at the start
            while (T < Len)
            {
                if (T > 0.6f && SkipPressed()) { Skipped = true; log?.Invoke($"opening skipped at {T:0.0} s"); break; }
                float dt = Time.deltaTime; T += dt;
                // 0-5 s: the Moon, slow push-in; the logo fades in, holds ~3 s, fades out before the push speeds up
                float push = T < 5f ? T / 5f * 0.25f : 0.25f + Mathf.Pow((T - 5f) / 4.5f, 2.6f) * 60f;     // accelerating
                float h = moonH0 * (1f + push);
                // aim the push at a point below the centre-right of the disc (the prison's surface), drifting to the centre
                var aim = new Vector2(0.18f, -0.12f) * h * Mathf.Clamp01(push / 3f);
                moon.rectTransform.sizeDelta = new Vector2(h, h); moon.rectTransform.anchoredPosition = -aim;
                glow.rectTransform.sizeDelta = new Vector2(h * 1.9f, h * 1.9f); glow.rectTransform.anchoredPosition = -aim; glow.color = new Color(1, 1, 1, Mathf.Clamp01(1f - push / 2f));
                stars.color = new Color(1, 1, 1, Mathf.Clamp01(1f - push / 1.5f));
                if (logo != null) logo.color = new Color(1, 1, 1, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 1.6f, T)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4.0f, 5.0f, T))));
                // ~8.5-10 s: the surface fills the screen
                float sA = Mathf.InverseLerp(8.0f, 9.0f, T); surf.color = new Color(1, 1, 1, sA * (1f - Mathf.InverseLerp(10.0f, 10.6f, T)));
                float zoom = Mathf.Lerp(0.6f, 0.05f, Mathf.InverseLerp(8f, 10.4f, T));
                surf.uvRect = new Rect(0.5f - zoom / 2f, 0.5f - zoom / 2f, zoom, zoom);
                // 10-13.2 s: through the crust - strata rushing up past the camera, darker as it goes deeper
                float cA = Mathf.InverseLerp(9.9f, 10.4f, T) * (1f - Mathf.InverseLerp(13.0f, 13.4f, T));
                strata.color = new Color(1, 1, 1, cA);
                strata.uvRect = new Rect(0f, -T * 2.6f, 1f, 0.35f);
                if (!rumble && T > 9.6f) { rumble = true; AudioKit.Play("tremor_rumble", null, 0.8f); }
                // 13.2-16 s: the tunnels - near dark, then black
                dark.color = new Color(0, 0, 0, Mathf.Clamp01(Mathf.InverseLerp(11.5f, 13.4f, T) * 0.75f + Mathf.InverseLerp(14.6f, 15.8f, T)));
                if (shot != null)
                {
                    if (!shot1 && T > 2.4f) { shot1 = true; yield return shot("op01_title_over_the_moon"); }
                    if (!shot2 && T > 7.4f) { shot2 = true; yield return shot("op02_push_in_to_the_surface"); }
                    if (!shot3 && T > 11.6f) { shot3 = true; yield return shot("op03_dive_through_the_crust"); }
                }
                yield return null;
            }
            dark.color = Color.black;
            yield return null;
            Object.Destroy(go);
            PauseMenu.EscapeOwned = prevEsc;
            Playing = false;
            log?.Invoke($"opening done ({(Skipped ? "skipped" : "watched")}, {T:0.0} s)");
        }
    }
}
