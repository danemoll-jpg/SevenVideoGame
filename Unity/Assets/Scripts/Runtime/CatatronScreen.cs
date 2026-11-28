using System.Collections;
using System.Collections.Generic;
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
    /// QF41 (Dan): before CAT_04 and after it the screen plays Dan's six commercials (Art/Video/catatron, 12 s each) in a shuffled
    /// rotation (no ad repeats until all six have played), each with its own mixed track (voice / effects / music bed:
    /// Tools/Audio/make_catatron_ads.py) from the screen's position, ducked under any line. QF42 (Dan: "I don't want it to glitch"):
    /// when she reaches the square (CAT_04) the running ad FINISHES (<see cref="FinishAd"/>: no new one starts), then the RACE AD
    /// plays once: the announcer, the trophy with the sphere, the mark, the Deluxe Retreat. No channel change any more. Never race
    /// highlights in an ad (Dan: boring).
    /// </summary>
    public class CatatronScreen : MonoBehaviour
    {
        public enum Mode { Idle, Highlights, Trophy, Mark, Retreat, Announcer, Race, Ads }
        public UnityEngine.Video.VideoClip[] Ads; public AudioClip[] AdAudio; public string[] AdNames;
        public Camera ScreenCam;
        public Renderer Face;
        public Transform Trophy, MarkPoint, Retreat, AnnouncerDesk, GhostRacer;
        public Mode Now { get; private set; } = Mode.Ads;
        public int Frames { get; private set; }
        RenderTexture _rt; float _next, _t, _ghostS; bool _talking; Transform _raceFollow;

        void Start()
        {
            if (ScreenCam == null || Face == null) return;
            _rt = new RenderTexture(512, 288, 16) { name = "Catatron_RT" };
            ScreenCam.targetTexture = _rt; ScreenCam.enabled = false;
            _vrt = new RenderTexture(736, 384, 0) { name = "Catatron_Ads" };
            _vp = gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
            _vp.playOnAwake = false; _vp.isLooping = false; _vp.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture; _vp.targetTexture = _vrt;
            _vp.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None; _vp.skipOnDrop = true;
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false; _src.spatialBlend = 1f; _src.rolloffMode = AudioRolloffMode.Linear; _src.minDistance = 30f; _src.maxDistance = 260f; _src.dopplerLevel = 0f;
            SetFace(Now == Mode.Ads ? (Texture)_vrt : _rt);
            if (Now == Mode.Ads) { _stopAfter = false; StartCoroutine(NextAd()); }
        }
        void SetFace(Texture tex)
        {
            var m = Face.material;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_EmissionMap")) { m.SetTexture("_EmissionMap", tex); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white * 0.9f); }
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        }
        public void Show(Mode m)
        {
            if (m == Now) return;
            var was = Now; Now = m; _t = 0f;
            if (_vp == null) return;
            if (was == Mode.Ads) { StopAllCoroutines(); _vp.Stop(); _src.Stop(); _adOn = false; _rotating = false; }
            SetFace(m == Mode.Ads ? (Texture)_vrt : _rt);
            if (m == Mode.Ads) { _stopAfter = false; StartCoroutine(NextAd()); }
        }

        // ---- QF41: the commercials
        UnityEngine.Video.VideoPlayer _vp; RenderTexture _vrt; AudioSource _src;
        readonly List<int> _bag = new List<int>(); int _last = -1; bool _adOn; float _duck = 1f;
        public string CurrentAd => _adOn && _last >= 0 && AdNames != null && _last < AdNames.Length ? AdNames[_last] : null;
        public static readonly List<string> Played = new List<string>();
        bool _stopAfter, _rotating; float _adT, _adLen;
        /// <summary>QF42: seconds left of the ad on now (0 between ads).</summary>
        public float AdLeft => _adOn ? Mathf.Max(0f, _adLen - _adT) : 0f;
        /// <summary>QF42: the rotation is still running (an ad playing or about to start).</summary>
        public bool Rotating => _rotating && Now == Mode.Ads;
        /// <summary>QF42: let the ad on now play to its end, start no new one (at most <paramref name="max"/> s), then hold the last
        /// frame. The race ad follows with <see cref="Show"/>.</summary>
        public IEnumerator FinishAd(float max = 13f)
        {
            _stopAfter = true;
            for (float t = 0f; Rotating && t < max; t += Time.deltaTime) yield return null;
            if (Rotating) { StopAllCoroutines(); _rotating = false; _adOn = false; Debug.Log("[Seven] CAT-A-TRON (QF42): the ad did not end in time - stopped"); }
        }
        /// <summary>The next ad from a shuffled bag: every ad once before any repeats (and never the same one twice in a row).</summary>
        int Draw()
        {
            if (_bag.Count == 0)
            {
                for (int i = 0; i < Ads.Length; i++) _bag.Add(i);
                for (int i = _bag.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (_bag[i], _bag[j]) = (_bag[j], _bag[i]); }
                if (_bag.Count > 1 && _bag[0] == _last) (_bag[0], _bag[_bag.Count - 1]) = (_bag[_bag.Count - 1], _bag[0]);
            }
            int k = _bag[0]; _bag.RemoveAt(0); return k;
        }
        IEnumerator NextAd(int force = -1)
        {
            if (Ads == null || Ads.Length == 0) yield break;
            _rotating = true;
            while (Now == Mode.Ads && !_stopAfter)
            {
                int i = force >= 0 ? force : Draw(); force = -1; _last = i;
                _vp.clip = Ads[i]; _vp.Prepare();
                for (float t = 0f; !_vp.isPrepared && t < 3f; t += Time.unscaledDeltaTime) yield return null;
                if (_stopAfter) break;                 // QF42: asked to finish while the next one was loading - it never starts
                _vp.Play();
                if (AdAudio != null && i < AdAudio.Length && AdAudio[i] != null) { _src.clip = AdAudio[i]; _src.time = 0f; _src.Play(); }
                _adOn = true;
                var n = AdNames != null && i < AdNames.Length ? AdNames[i] : Ads[i].name; Played.Add(n);
                Debug.Log($"[Seven] CAT-A-TRON (QF41): ad '{n}' ({Played.Count} played)");
                _adLen = (float)Ads[i].length;
                for (_adT = 0f; _adT < _adLen && Now == Mode.Ads; _adT += Time.deltaTime) yield return null;
                _adOn = false;
                if (_stopAfter) Debug.Log($"[Seven] CAT-A-TRON (QF42): ad '{n}' played to its end");
            }
            _rotating = false;
        }
        /// <summary>Check runs: play this ad now (the capture of every ad on the screen).</summary>
        public void PlayAdNow(int i) { if (Now != Mode.Ads) Show(Mode.Ads); StopAllCoroutines(); _stopAfter = false; _rotating = false; _vp.Stop(); _src.Stop(); StartCoroutine(NextAd(i)); }
        void Update()
        {
            if (_src == null) return;
            // ducked under any line (never competing with a conversation), back up after it
            float want = DialoguePlayer.AnyBusy ? 0.15f : 1f;
            _duck = Mathf.MoveTowards(_duck, want, Time.deltaTime * 2.5f);
            _src.volume = 0.85f * _duck * AudioKit.Gain(AudioKit.Bus.Sfx);
        }
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
            if (Now == Mode.Ads || Now == Mode.Idle) return;          // QF41: the video plays itself; no level camera
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
                    // QF28 (2): the advert is framed on the trophy's top ring and the sphere in its cradle (0.18.2 aimed 1.4 m above the
                    // trophy's base: the trophy sat small at the bottom of the screen and its sphere was a few pale pixels). A slow orbit
                    // that alternates the whole trophy (the sphere crowning it) with a close push-in on the sphere.
                    if (Trophy != null)
                    {
                        var sph = TrophySphere(); var top = sph != null ? sph.position : Trophy.position + Vector3.up * 1.0f;
                        float a = _t * 24f, push = 0.5f + 0.5f * Mathf.Sin(_t * 0.35f);
                        var look = Vector3.Lerp(Vector3.Lerp(Trophy.position, top, 0.62f), top, push);
                        float dist = Mathf.Lerp(2.4f, 0.9f, push);
                        Aim(look + Quaternion.Euler(0f, a, 0f) * new Vector3(0f, Mathf.Lerp(0.35f, 0.12f, push), -dist), look, 30f);
                    }
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
        /// <summary>QF28 (2): is the sphere on the advert (the screen camera's frame) - in its cradle, in frame, how big.</summary>
        public string SphereReport()
        {
            var sph = TrophySphere();
            if (sph == null) return $"TROPHY SPHERE (QF28 2): mode {Now} - the sphere is NOT in the trophy's cradle (FAIL before CAT_09)";
            if (ScreenCam == null) return "TROPHY SPHERE (QF28 2): no screen camera";
            var v = ScreenCam.WorldToViewportPoint(sph.position); float h = RuneSizes.SphereR * 2f / (2f * Mathf.Max(0.01f, v.z) * Mathf.Tan(ScreenCam.fieldOfView * 0.5f * Mathf.Deg2Rad));
            bool inFrame = v.z > 0f && v.x > 0.05f && v.x < 0.95f && v.y > 0.05f && v.y < 0.95f;
            return $"TROPHY SPHERE (QF28 2): mode {Now} - the sphere in the trophy's cradle, {(inFrame ? "in the advert's frame" : "OUT of the advert's frame")} at ({v.x:0.00}, {v.y:0.00}), {h * 100f:0} % of the screen's height - {(Now == Mode.Trophy && inFrame && h > 0.06f ? "ok" : Now == Mode.Trophy ? "FAIL" : "-")}";
        }
        Transform _sph;
        /// <summary>QF28 (2): the sphere while it is still in the trophy's cradle (it leaves only at the touch, CAT_09).</summary>
        public Transform TrophySphere() { if (_sph == null && Trophy != null) _sph = Trophy.Find("Sphere"); return _sph != null && _sph.gameObject.activeInHierarchy && _sph.IsChildOf(Trophy) ? _sph : null; }
        void Aim(Vector3 pos, Vector3 look, float fov) { ScreenCam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos)); ScreenCam.fieldOfView = fov; }
        void OnDestroy() { if (_rt != null) _rt.Release(); }
    }
}
