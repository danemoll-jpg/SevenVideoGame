using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF18 (2) - "show it, don't write it": the navigation display SHOWS what the spec describes instead of a caption. A
    /// glowing three-quarter hologram floating over the console: the actual level (Resources/Holo/Holo_&lt;world&gt;, copied
    /// from the real scene at build time), markers on it (the temple highlighted, the barricades on the bridges), zooms to a
    /// place, and the two records side by side (the Mars route, the Cetaceans' sequence as pulses, the intervals lining up,
    /// the route completing out to Biaveh). White additive light only.
    /// </summary>
    public class HoloDisplay : MonoBehaviour
    {
        public static HoloDisplay I { get; private set; }
        Transform _pivot, _model, _marks; GameObject _modelGo; string _world; Light _light;
        float _scale = 0.001f; Vector3 _focus; float _zoom = 1f, _spin; bool _spinOn = true;
        Material _add;
        public string Showing => _modelGo != null && _modelGo.activeSelf ? _world : null;

        /// <summary>The hologram over the console's navigation display (made on first use).</summary>
        public static HoloDisplay Get(Vector3? at = null, Vector3? facing = null)
        {
            if (I == null) { var go = new GameObject("NavHologram"); I = go.AddComponent<HoloDisplay>(); I.Build(); }
            var con = ShipConsole.I;
            if (at.HasValue) { I.transform.position = at.Value; var f = facing ?? Vector3.forward; f.y = 0f; I.transform.rotation = Quaternion.LookRotation(f.sqrMagnitude > 1e-4f ? f : Vector3.forward); }
            else if (con != null && con.Display != null) { I.transform.position = con.Display.position + Vector3.up * 0.55f - con.Display.forward * 0.25f; I.transform.rotation = Quaternion.Euler(0f, con.Display.eulerAngles.y, 0f); }
            return I;
        }
        void OnDestroy() { if (I == this) I = null; }
        void Build()
        {
            _pivot = new GameObject("Pivot").transform; _pivot.SetParent(transform, false);
            _model = new GameObject("Model").transform; _model.SetParent(_pivot, false);
            _marks = new GameObject("Marks").transform; _marks.SetParent(_pivot, false);
            _light = new GameObject("HoloLight").AddComponent<Light>(); _light.transform.SetParent(transform, false); _light.transform.localPosition = Vector3.up * 0.2f;
            _light.type = LightType.Point; _light.color = Color.white; _light.range = 2.2f; _light.intensity = 0f; _light.shadows = LightShadows.None;
            _add = Markers.Additive(0.9f);
            // the projector base: a soft disc of light under it
            var disc = new GameObject("HoloBase"); disc.transform.SetParent(transform, false); disc.transform.localPosition = Vector3.down * 0.18f;
            disc.AddComponent<MeshFilter>().sharedMesh = Markers.RingMesh(0.0f, 0.62f, 0f); var r = disc.AddComponent<MeshRenderer>(); r.sharedMaterial = Markers.Additive(0.18f); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            gameObject.SetActive(false);
        }

        /// <summary>Show a world's hologram: width = how wide (m) the level's region appears; tilt = the three-quarter view.</summary>
        public void Show(string world, float width = 1.15f, float regionWidth = 1200f, Vector3? centre = null)
        {
            gameObject.SetActive(true);
            if (_world != world || _modelGo == null)
            {
                if (_modelGo != null) Destroy(_modelGo);
                var pf = Resources.Load<GameObject>("Holo/Holo_" + world);
                if (pf != null) { _modelGo = Instantiate(pf, _model); _modelGo.transform.localPosition = Vector3.zero; _modelGo.transform.localRotation = Quaternion.identity; }
                _world = world;
                Log($"hologram (QF18 2): {world} - {(pf != null ? pf.transform.childCount + " pieces of the real level" : "MISSING Resources/Holo/Holo_" + world)}");
            }
            if (_modelGo != null) _modelGo.SetActive(true);
            ClearMarks();
            _scale = width / Mathf.Max(1f, regionWidth); _zoom = 1f; _focus = centre ?? Vector3.zero; _spinOn = true;
            Apply();
            StartCoroutine(FadeLight(1.6f));
        }
        public void Hide() { ClearMarks(); if (_modelGo != null) _modelGo.SetActive(false); StopAllCoroutines(); _light.intensity = 0f; gameObject.SetActive(false); }
        IEnumerator FadeLight(float to) { for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f) { _light.intensity = Mathf.Lerp(0f, to, t); yield return null; } }

        void Apply()
        {
            float s = _scale * _zoom;
            _pivot.localScale = Vector3.one * s;
            _pivot.localPosition = -(Quaternion.Euler(0f, _spin, 0f) * new Vector3(_focus.x, 0f, _focus.z)) * s;
            _pivot.localRotation = Quaternion.Euler(0f, _spin, 0f);
        }
        void Update() { if (_spinOn) _spin += Time.deltaTime * 4f; Apply(); }

        /// <summary>Ease the view onto a place in the level (zoom = how much closer).</summary>
        public IEnumerator Focus(Vector3 levelPoint, float zoom, float secs = 1.6f)
        {
            var f0 = _focus; float z0 = _zoom; float s0 = _spin; _spinOn = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.05f, secs)) { float u = Mathf.SmoothStep(0f, 1f, t); _focus = Vector3.Lerp(f0, levelPoint, u); _zoom = Mathf.Lerp(z0, zoom, u); _spin = Mathf.LerpAngle(s0, 0f, u); yield return null; }
            _focus = levelPoint; _zoom = zoom; _spin = 0f;
        }

        void ClearMarks() { if (_marks == null) return; for (int i = _marks.childCount - 1; i >= 0; i--) Destroy(_marks.GetChild(i).gameObject); }
        /// <summary>A highlight on the level: a pulsing ring + a beam of light rising from it (level coordinates).</summary>
        public void Mark(Vector3 levelPoint, float radius, string what, bool beam = true)
        {
            var g = new GameObject("Mark_" + what); g.transform.SetParent(_marks, false); g.transform.localPosition = levelPoint + Vector3.up * 2f;
            g.AddComponent<MeshFilter>().sharedMesh = Markers.RingMesh(radius * 0.8f, radius, 0f); var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = Markers.Additive(1.4f); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (beam)
            {
                var b = new GameObject("Beam"); b.transform.SetParent(g.transform, false);
                b.AddComponent<MeshFilter>().sharedMesh = Markers.RectMesh(radius * 0.18f, radius * 3.5f); b.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                var br = b.AddComponent<MeshRenderer>(); br.sharedMaterial = Markers.Additive(0.9f); br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                b.AddComponent<FaceCameraY>();
            }
            g.AddComponent<HoloPulse>();
        }
        /// <summary>A barricade marked across a road / bridge (a bright bar between two level points).</summary>
        public void Bar(Vector3 a, Vector3 b, float width, string what)
        {
            var g = new GameObject("Bar_" + what); g.transform.SetParent(_marks, false);
            var d = b - a; g.transform.localPosition = a + Vector3.up * 3f; g.transform.localRotation = Quaternion.LookRotation(new Vector3(d.x, 0f, d.z));
            g.AddComponent<MeshFilter>().sharedMesh = Markers.RectMesh(width, d.magnitude);
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = Markers.Additive(1.6f); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            g.AddComponent<HoloPulse>();
        }

        /// <summary>MAR_13C - the two records side by side: the Mars route (a line of nodes) and the Cetaceans' sequence (rings of
        /// pulses); they slide together until their intervals line up, a soft pulse runs along both, then the route completes
        /// out to a new far node - Biaveh.</summary>
        public IEnumerator Records()
        {
            gameObject.SetActive(true);
            if (_modelGo != null) _modelGo.SetActive(false);
            ClearMarks(); _spinOn = false; _spin = 0f; _focus = Vector3.zero; _zoom = 1f; _scale = 1f; Apply();
            StartCoroutine(FadeLight(1.4f));
            var root = new GameObject("Records").transform; root.SetParent(_marks, false); root.localPosition = Vector3.zero;
            float[] iv = { 0f, 0.13f, 0.21f, 0.37f, 0.45f, 0.61f };                  // the shared intervals
            var route = new List<Transform>(); var seq = new List<Transform>();
            Transform Dot(Transform parent, Vector3 p, float r, float k) { var g = new GameObject("Node"); g.transform.SetParent(parent, false); g.transform.localPosition = p; g.AddComponent<MeshFilter>().sharedMesh = Markers.RingMesh(0f, r, 0f); var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = Markers.Additive(k); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; g.transform.localRotation = Quaternion.Euler(-60f, 0f, 0f); return g.transform; }
            var lineA = new GameObject("MarsRoute").transform; lineA.SetParent(root, false); lineA.localPosition = new Vector3(-0.42f, 0.16f, 0f);
            var lineB = new GameObject("CetaceanSequence").transform; lineB.SetParent(root, false); lineB.localPosition = new Vector3(-0.42f, -0.08f, 0.0f);
            foreach (var x in iv) { route.Add(Dot(lineA, new Vector3(x, 0f, 0f), 0.022f, 1.6f)); seq.Add(Dot(lineB, new Vector3(x * 1.18f + 0.07f, 0f, 0f), 0.03f, 0.8f)); }
            // the route's line between its nodes
            for (int i = 0; i + 1 < iv.Length; i++) { var g = new GameObject("Seg"); g.transform.SetParent(lineA, false); g.transform.localPosition = new Vector3(iv[i], 0f, 0f); g.transform.localRotation = Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(-60f, 0f, 0f); g.AddComponent<MeshFilter>().sharedMesh = Markers.RectMesh(0.006f, iv[i + 1] - iv[i]); var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = Markers.Additive(1.2f); }
            Log("hologram (QF18 2): MAR_13C - the Mars route and the Cetaceans' sequence side by side");
            // the sequence pulses (rings expand) while it slides into line with the route
            for (float t = 0f; t < 1f; t += Time.deltaTime / 3.2f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                for (int i = 0; i < seq.Count; i++) { seq[i].localPosition = new Vector3(Mathf.Lerp(iv[i] * 1.18f + 0.07f, iv[i], u), 0f, 0f); seq[i].localScale = Vector3.one * (1f + 0.35f * Mathf.Abs(Mathf.Sin(Time.time * 5f - i))); }
                lineB.localPosition = new Vector3(-0.42f, Mathf.Lerp(-0.08f, 0.06f, u), 0f);
                yield return null;
            }
            // the intervals match: a soft pulse runs along both
            for (int i = 0; i < iv.Length; i++) { route[i].localScale = Vector3.one * 2.2f; seq[i].localScale = Vector3.one * 1.8f; AudioKit.Play("console_move", null, 0.35f); for (float t = 0f; t < 0.16f; t += Time.deltaTime) yield return null; route[i].localScale = Vector3.one; seq[i].localScale = Vector3.one; }
            Log("hologram (QF18 2): the intervals line up");
        }
        /// <summary>...then the route completes - the line runs on out to a new node far beyond (Biaveh).</summary>
        public IEnumerator RouteCompletes()
        {
            var lineA = _marks.Find("Records/MarsRoute"); if (lineA == null) yield break;
            var g = new GameObject("RouteOut"); g.transform.SetParent(lineA, false); g.transform.localPosition = new Vector3(0.61f, 0f, 0f); g.transform.localRotation = Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(-60f, 0f, 0f);
            var mf = g.AddComponent<MeshFilter>(); var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = Markers.Additive(1.4f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.8f) { mf.sharedMesh = Markers.RectMesh(0.006f, 0.3f * t); yield return null; }
            var end = new GameObject("Veyra"); end.transform.SetParent(lineA, false); end.transform.localPosition = new Vector3(0.91f, 0f, 0f); end.transform.localRotation = Quaternion.Euler(-60f, 0f, 0f);
            end.AddComponent<MeshFilter>().sharedMesh = Markers.RingMesh(0f, 0.04f, 0f); end.AddComponent<MeshRenderer>().sharedMaterial = Markers.Additive(2.4f); end.AddComponent<HoloPulse>();
            AudioKit.Play("console_select", null, 0.6f);
            Log("hologram (QF22): the route completes - out to the Biavian contact at Veyra");
        }
        static void Log(string s) { Debug.Log("[Seven] " + s); if (DemoBoot.CaptureDir != null) ChapterCapture.I?.Log(s); }
    }
}
