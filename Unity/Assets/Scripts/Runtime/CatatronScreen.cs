using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 18 — the Cat-a-tron (CAT_04): the square's enormous screen shows what its cameras see of the REAL level (the
    /// show-don't-caption rule: a display is built from the level itself, so it always matches): race highlights = a camera
    /// flying the actual Grand Circuit behind a ghost racer; the prize display = a slow orbit round the real trophy on its
    /// display (the sphere in its cradle, the mark on the setting); the runner-up ad = the real Deluxe Retreat; the announcer
    /// at his desk (a small studio set far below the city). One camera renders into the screen's texture at 15 fps, only while
    /// the screen is in view. Grayscale like everything else.
    /// </summary>
    public class CatatronScreen : MonoBehaviour
    {
        public enum Mode { Idle, Highlights, Trophy, Mark, Retreat, Announcer, Race }
        public Camera ScreenCam;
        public Renderer Face;
        public Transform Trophy, MarkPoint, Retreat, AnnouncerDesk, GhostRacer;
        public Mode Now { get; private set; } = Mode.Highlights;
        public int Frames { get; private set; }
        RenderTexture _rt; float _next, _t, _ghostS; bool _talking; Transform _raceFollow;

        void Start()
        {
            if (ScreenCam == null || Face == null) return;
            _rt = new RenderTexture(512, 288, 16) { name = "Catatron_RT" };
            ScreenCam.targetTexture = _rt; ScreenCam.enabled = false;
            var m = Face.material;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", _rt);
            if (m.HasProperty("_EmissionMap")) { m.SetTexture("_EmissionMap", _rt); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white * 0.9f); }
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", _rt);
        }
        public void Show(Mode m) { if (m != Now) { Now = m; _t = 0f; } }
        public void Talking(bool on) { _talking = on; }
        /// <summary>CAT_07: the live race on the screen, following a racer.</summary>
        public void FollowRace(Transform racer) { _raceFollow = racer; Show(Mode.Race); }

        void LateUpdate()
        {
            if (ScreenCam == null) return;
            _t += Time.deltaTime;
            if (GhostRacer != null)
            {
                _ghostS += Time.deltaTime * 21f;
                var p = CattalusCourse.AtDist(_ghostS); var q = CattalusCourse.AtDist(_ghostS + 4f);
                GhostRacer.position = p + Vector3.up * 0.9f; var f = q - p; f.y = 0f; if (f.sqrMagnitude > 1e-4f) GhostRacer.rotation = Quaternion.LookRotation(f);
            }
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 1f / 15f;
            // only while it can be seen (the main camera near the square)
            var main = Camera.main; if (main != null && (main.transform.position - transform.position).sqrMagnitude > 400f * 400f && !_forced) return;
            Place();
            ScreenCam.Render(); Frames++;
        }
        bool _forced;
        /// <summary>Check runs: render now regardless of distance (the capture of the screen up close).</summary>
        public void ForceRender() { _forced = true; Place(); if (ScreenCam != null) { ScreenCam.Render(); Frames++; } _forced = false; }
        void Place()
        {
            switch (Now)
            {
                case Mode.Highlights:
                    if (GhostRacer != null) { var b = GhostRacer.position - GhostRacer.forward * 9f + Vector3.up * 3.2f + GhostRacer.right * Mathf.Sin(_t * 0.4f) * 3f; Aim(b, GhostRacer.position + GhostRacer.forward * 6f, 46f); }
                    break;
                case Mode.Trophy:
                    if (Trophy != null) { float a = _t * 28f; var c = Trophy.position + Vector3.up * 1.4f; Aim(c + Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.4f, -3.2f), c, 34f); }
                    break;
                case Mode.Mark:
                    if (MarkPoint != null) { var c = MarkPoint.position; var f = MarkPoint.forward; Aim(c + f * 0.9f + Vector3.up * 0.1f, c, 22f); }
                    break;
                case Mode.Retreat:
                    if (Retreat != null) { float a = 30f + _t * 22f; var c = Retreat.position + Vector3.up * 0.7f; Aim(c + Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.6f, -3.4f), c, 38f); }
                    break;
                case Mode.Announcer:
                    if (AnnouncerDesk != null) { var c = AnnouncerDesk.position + Vector3.up * 0.55f; var bob = _talking ? Mathf.Sin(_t * 9f) * 0.01f : 0f; Aim(c + AnnouncerDesk.forward * 1.5f + Vector3.up * (0.12f + bob), c, 30f); }
                    break;
                case Mode.Race:
                    if (_raceFollow != null) Aim(_raceFollow.position - _raceFollow.forward * 8f + Vector3.up * 3f, _raceFollow.position + _raceFollow.forward * 5f, 50f);
                    break;
            }
        }
        void Aim(Vector3 pos, Vector3 look, float fov) { ScreenCam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos)); ScreenCam.fieldOfView = fov; }
        void OnDestroy() { if (_rt != null) _rt.Release(); }
    }
}
