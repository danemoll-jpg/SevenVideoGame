using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF16 (2) - the journey between worlds, Dan's pick: a "sacred-geometry passage". About 8 s, skippable (Y / Enter /
    /// A / Esc): out of the dark ahead, rings of glowing geometric shapes - the rune solids she has collected so far (the
    /// dodecahedron, the cube, the icosahedron, the tetrahedron) - unfold, turning slowly, and form a corridor; the saucer
    /// glides through it (seen from outside behind it, then through the window); on arrival the rings fold shut behind it and
    /// the destination fills the view. Monochrome, soft glow and haze; never star streaks or a warp tunnel. A soft swelling
    /// hum and a chime; the ship's music ducked. The corridor grows over the game: with one record, rings of that one shape.
    /// Built at runtime from <see cref="TravelStage"/> (the exterior saucer far above the deck, the two materials).
    /// </summary>
    public class TravelPassage : MonoBehaviour
    {
        public static bool Running { get; private set; }
        public static int Journeys { get; private set; }
        static TravelStage Stage => TravelStage.I;

        /// <summary>The shapes she has collected (meshes, unit circumradius).</summary>
        public static List<Mesh> Collected(out string names)
        {
            var l = new List<Mesh>(); var n = new List<string>();
            // QF23 (23): the one saved list of collected records (the dodecahedron always; the cuboctahedron and octahedron too)
            foreach (var r in RecordSet.Collected()) { l.Add(r.Name == "cube" ? CubeMesh() : r.Mesh(1f)); n.Add(r.Name); }
            l.RemoveAll(m => m == null);
            if (l.Count == 0) { l.Add(Icosahedron.Mesh(1f)); n.Add("(none yet: icosahedron stand-in)"); }
            names = string.Join(", ", n);
            return l;
        }

        static Mesh _cube;
        static Mesh CubeMesh()
        {
            if (Stage != null && Stage.CubeMesh != null) return Stage.CubeMesh;
            if (_cube == null) { var g = GameObject.CreatePrimitive(PrimitiveType.Cube); _cube = g.GetComponent<MeshFilter>().sharedMesh; Destroy(g); }
            return _cube;
        }
        /// <summary>Play the journey to <paramref name="to"/> (sets the saucer's location at the end). With
        /// <paramref name="holdOpen"/> the rings stay open (the caller plays a scene inside the passage, then <see cref="Close"/>).</summary>
        public static IEnumerator Run(string to, bool holdOpen = false)
        {
            WorldFlags.UseOwnSaves($"setting course for {to}");      // QF38 (8): a Scene Select session ends when the story travels on
            var go = new GameObject("TravelPassage"); var tp = go.AddComponent<TravelPassage>();
            yield return tp.Play(to, holdOpen);
            if (!holdOpen) Destroy(go);
        }
        public static IEnumerator Close(string to)
        {
            var tp = FindAnyObjectByType<TravelPassage>();
            if (tp == null) { if (to != null) SaucerLocation.Set(to); SaucerLocation.Travelling = false; yield break; }
            yield return tp.Fold(to);
            Destroy(tp.gameObject);
        }

        readonly List<(Transform t, float spin, float delay, float norm)> _shapes = new List<(Transform, float, float, float)>();
        Camera _cam; Vector3 _camPos0; Quaternion _camRot0; float _fov0; AudioSource _hum; bool _skip;
        bool _hold0;

        Transform Shape(Mesh m, Transform parent, Vector3 pos, float size, Material mat)
        {
            var g = new GameObject("PassageShape"); g.transform.SetParent(parent, false); g.transform.position = pos;
            g.AddComponent<MeshFilter>().sharedMesh = m;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            g.transform.localScale = Vector3.zero;
            if (Stage != null && Stage.Halo != null)
            {
                var h = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(h.GetComponent<Collider>());
                h.name = "Halo"; h.transform.SetParent(g.transform, false); h.transform.localScale = Vector3.one * 3.2f;
                h.GetComponent<Renderer>().sharedMaterial = Stage.Halo; h.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                h.AddComponent<FaceCamera>();
            }
            g.transform.rotation = Random.rotation;
            g.name = "PassageShape_" + size.ToString("0.0");
            _shapes.Add((g.transform, Random.Range(10f, 28f) * (Random.value < 0.5f ? -1f : 1f), 0f, 1f / Mathf.Max(0.01f, m.bounds.extents.magnitude)));
            return g.transform;
        }

        /// <summary>Rings of shapes along +Z from <paramref name="start"/>, radius r, spacing dz.</summary>
        void Rings(Transform parent, Vector3 start, Vector3 fwd, int rings, int per, float r, float dz, float size, List<Mesh> meshes, Material mat)
        {
            var side = Vector3.Cross(Vector3.up, fwd).normalized; var up = Vector3.Cross(fwd, side);
            for (int i = 0; i < rings; i++)
                for (int k = 0; k < per; k++)
                {
                    float a = (k + (i % 2) * 0.5f) / per * Mathf.PI * 2f;
                    var p = start + fwd * dz * i + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * r;
                    var t = Shape(meshes[(i + k) % meshes.Count], parent, p, size, mat);
                    _shapes[_shapes.Count - 1] = (t, _shapes[_shapes.Count - 1].spin, i * 0.22f + k * 0.03f, _shapes[_shapes.Count - 1].norm);
                }
        }

        bool SkipPressed()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current; var gp = UnityEngine.InputSystem.Gamepad.current;
            return (kb != null && (kb.enterKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) || (gp != null && (gp.buttonNorth.wasPressedThisFrame || gp.buttonSouth.wasPressedThisFrame));
        }

        void Animate(float t, float size)
        {
            foreach (var (tr, spin, delay, norm) in _shapes)
            {
                if (tr == null) continue;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - delay) / 1.2f));      // unfold
                tr.localScale = Vector3.one * size * norm * u;
                tr.Rotate(Vector3.up, spin * Time.deltaTime, Space.World);
                tr.Rotate(Vector3.right, spin * 0.35f * Time.deltaTime, Space.Self);
            }
        }

        IEnumerator Play(string to, bool holdOpen)
        {
            Running = true; Journeys++;
            var cap = ChapterCapture.I;
            var meshes = Collected(out var names);
            if (cap != null) cap.Log($"travel (QF16 2): the sacred-geometry passage to '{to}' - shapes: {names}");
            SaucerLocation.Travelling = true;
            MusicSystem.Suspend(1.0f);
            AudioKit.Play("course_set", null, 0.6f);
            _hum = AudioKit.Loop("vision_shimmer", null, null, 0f, AudioKit.Bus.Sfx);
            if (_hum != null) StartCoroutine(AudioKit.Fade(_hum, 0.55f, 2.5f));
            var dirc = FindAnyObjectByType<SaucerDirector>();
            _cam = dirc != null && dirc.Cam != null ? dirc.Cam.GetComponent<Camera>() : Camera.main;
            if (_cam == null) { Running = false; SaucerLocation.Set(to); SaucerLocation.Travelling = false; MusicSystem.Resume(); yield break; }
            _camPos0 = _cam.transform.position; _camRot0 = _cam.transform.rotation; _fov0 = _cam.fieldOfView;
            var ui = FindAnyObjectByType<ChapterUI>();
            if (dirc != null && dirc.Cam != null) { _hold0 = dirc.Cam.Hold; dirc.Cam.Hold = true; }
            // 1. outside, behind the saucer: the rings unfold ahead and it glides through
            var mat = Stage != null ? Stage.Glow : null;
            if (Stage != null && Stage.Saucer != null)
            {
                if (ui != null) yield return ui.Fade(1f, 0.4f);
                var root = Stage.Saucer; var holder = root.parent != null ? root.parent.gameObject : root.gameObject; holder.SetActive(true);
                var p0 = root.position; var fwd = Vector3.forward;
                var outer = new GameObject("PassageOuter").transform;
                Rings(outer, p0 + fwd * 40f, fwd, 9, 8, 16f, 20f, 2.2f, meshes, mat);
                if (ui != null) StartCoroutine(ui.Fade(0f, 0.5f));
                bool shot = false;
                for (float t = 0f; t < 4.6f && !_skip; t += Time.deltaTime)
                {
                    if (SkipPressed() && t > 0.5f) _skip = true;
                    root.position = p0 + fwd * (t * t * 4.5f + t * 6f);
                    _cam.transform.position = root.position - fwd * 24f + Vector3.up * 5.5f + Vector3.right * 3f;
                    _cam.transform.rotation = Quaternion.LookRotation(root.position + fwd * 30f - _cam.transform.position);
                    _cam.fieldOfView = 52f;
                    Animate(t, 2.2f);
                    if (!shot && t > 2.4f && cap != null) { shot = true; yield return cap.Shot("qf16_02_passage_a_outside_behind_the_saucer"); }
                    yield return null;
                }
                if (ui != null) yield return ui.Fade(1f, 0.35f);
                root.position = p0; holder.SetActive(false);
                Destroy(outer.gameObject); _shapes.Clear();
            }
            // 2. through the window: the corridor passes the ship
            var inner = new GameObject("PassageInner").transform; inner.SetParent(transform, false);
            Rings(inner, new Vector3(0f, 1.2f, 70f), Vector3.forward, 7, 9, 22f, 22f, 2.6f, meshes, mat);
            _cam.transform.position = new Vector3(0f, 1.6f, 4.0f); _cam.transform.rotation = Quaternion.LookRotation(new Vector3(0f, -0.08f, 1f)); _cam.fieldOfView = 60f;
            if (ui != null) StartCoroutine(ui.Fade(0f, 0.4f));
            bool shot2 = false;
            for (float t = 0f; t < (holdOpen ? 2.4f : 3.0f) && !_skip; t += Time.deltaTime)
            {
                if (SkipPressed() && t > 0.3f) _skip = true;
                inner.position = Vector3.back * t * 16f;
                Animate(t, 2.6f);
                if (!shot2 && t > 1.6f && cap != null) { shot2 = true; yield return cap.Shot("qf16_02_passage_b_through_the_window"); }
                yield return null;
            }
            if (!holdOpen) yield return Fold(to);
        }

        /// <summary>The rings fold shut behind the saucer; the destination fills the view.</summary>
        IEnumerator Fold(string to)
        {
            var cap = ChapterCapture.I;
            AudioKit.Play("console_select", null, 0.7f);                    // the chime
            var sizes = new Dictionary<Transform, Vector3>();
            foreach (var s in _shapes) if (s.t != null) sizes[s.t] = s.t.localScale;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.1f)
            {
                foreach (var s in _shapes) if (s.t != null && sizes.TryGetValue(s.t, out var sc)) { s.t.localScale = sc * (1f - Mathf.SmoothStep(0f, 1f, t)); s.t.Rotate(Vector3.up, s.spin * 4f * Time.deltaTime, Space.World); }
                yield return null;
            }
            if (to != null && to != "biaveh" && to != "asteroid") SaucerLocation.Set(to);
            SaucerLocation.Travelling = false;
            yield return null;
            if (cap != null && _cam != null) { yield return cap.Shot($"qf16_02_passage_c_arrived_{SaucerLocation.Current}"); }
            if (_hum != null) StartCoroutine(AudioKit.Fade(_hum, 0f, 1.2f, true));
            MusicSystem.Resume(2.5f);
            if (_cam != null) { _cam.transform.SetPositionAndRotation(_camPos0, _camRot0); _cam.fieldOfView = _fov0; }
            var dirc = FindAnyObjectByType<SaucerDirector>();
            if (dirc != null && dirc.Cam != null) dirc.Cam.Hold = _hold0;
            if (cap != null) cap.Log($"travel (QF16 2): arrived - the saucer is at '{SaucerLocation.Saved}' ({(_skip ? "skipped" : "played in full")})");
            Running = false;
        }
    }
}
