using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 15 — a Mars character on Dan's Mixamo rigs (Humanoid): the Reptilians (one model for the guard, the worker, the
    /// caretaker, families, traders; ±10 % size and a few tones, guards with a helmet and a chest plate) and Viper. Animator
    /// states: idle, walk, wave, talk, point, carry (Reptilians; MODELS-NEEDED "Mars animation clips") and idle, walk, talk,
    /// point, roar, hit, down, dazed, kneel (Viper). Moves itself over the ground (Ground.Height), the walk played back at its
    /// real speed (no skating). While its voice is speaking the head and upper body move with the loudness (no lip-sync);
    /// Viper's hood flares on angry lines (<see cref="Flare"/>). The Reptilian walk is Dan's "Orc Walk": played slower and
    /// with the hip sway damped (<see cref="CalmSway"/>) so it reads as an ordinary person, not a brute.
    /// </summary>
    public class MarsFolk : MonoBehaviour
    {
        public Animator Anim;
        public string Role = "folk";                 // guard / worker / caretaker / trader / family / viper
        public float WalkSpeed = 1.15f;              // m/s
        public float ClipSpeed = 1.25f;              // the walk clip's own ground speed at this model's size (measured at start)
        public bool CalmSway = true;
        public AudioSource Voice;                    // set while one of its lines plays
        [System.NonSerialized] public float Flare;   // Viper: 0..1 hood flare (angry lines)
        /// <summary>QF18 (8): Viper's eyes glow white (two small emissive points + a faint light at the eyes) while he speaks
        /// down from his platform - readable in his close-ups.</summary>
        [System.NonSerialized] public bool EyeGlow;
        GameObject _eyeL, _eyeR; Light _eyeLight;
        void UpdateEyes()
        {
            bool on = EyeGlow && _head != null && gameObject.activeInHierarchy;
            if (!on) { if (_eyeL != null) { _eyeL.SetActive(false); _eyeR.SetActive(false); _eyeLight.enabled = false; } return; }
            if (_eyeL == null)
            {
                var m = Markers.Additive(2.2f);
                GameObject Eye(string n) { var g = new GameObject(n); g.AddComponent<MeshFilter>().sharedMesh = Markers.Rock(7, Vector3.one * 0.018f); var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return g; }
                _eyeL = Eye("ViperEyeL"); _eyeR = Eye("ViperEyeR");
                _eyeLight = new GameObject("ViperEyeLight").AddComponent<Light>(); _eyeLight.type = LightType.Point; _eyeLight.color = Color.white; _eyeLight.range = 0.9f; _eyeLight.intensity = 1.6f; _eyeLight.shadows = LightShadows.None;
            }
            _eyeL.SetActive(true); _eyeR.SetActive(true); _eyeLight.enabled = true;
            var f = transform.forward; var r2 = transform.right; float s = transform.lossyScale.y;
            var c = _head.position + (Vector3.up * 0.075f + f * 0.11f) * s;
            _eyeL.transform.position = c - r2 * 0.038f * s; _eyeR.transform.position = c + r2 * 0.038f * s; _eyeLight.transform.position = c + f * 0.05f * s;
        }
        public bool Busy => _co != null;
        public float Distance { get; private set; }
        string _state; Coroutine _co; Transform _head, _neck, _chest, _hips; float _talk, _nod; readonly float[] _buf = new float[256];
        Vector3 _hipsRest; bool _hipsSet;

        void Awake()
        {
            if (Anim == null) Anim = GetComponentInChildren<Animator>();
            if (Anim != null && Anim.isHuman)
            {
                _head = Anim.GetBoneTransform(HumanBodyBones.Head); _neck = Anim.GetBoneTransform(HumanBodyBones.Neck);
                _chest = Anim.GetBoneTransform(HumanBodyBones.UpperChest) ?? Anim.GetBoneTransform(HumanBodyBones.Chest);
                _hips = Anim.GetBoneTransform(HumanBodyBones.Hips);
                Anim.applyRootMotion = false;
            }
        }

        public bool HasState(string s) => Anim != null && Anim.HasState(0, Animator.StringToHash(s));
        public void Play(string s, float fade = 0.25f)
        {
            if (Anim == null || _state == s) return;
            if (!HasState(s)) { if (s != "idle" && HasState("idle")) s = "idle"; else return; }
            _state = s; Anim.CrossFadeInFixedTime(s, fade); Anim.speed = 1f;
        }
        public string State => _state;

        public void Stop() { if (_co != null) StopCoroutine(_co); _co = null; Play("idle"); }
        public void Face(Vector3 p, bool instant = false)
        {
            var d = p - transform.position; d.y = 0f; if (d.sqrMagnitude < 1e-4f) return;
            var q = Quaternion.LookRotation(d);
            if (instant) transform.rotation = q; else StartCoroutine(TurnTo(q));
        }
        IEnumerator TurnTo(Quaternion q) { for (float t = 0f; t < 0.6f; t += Time.deltaTime) { transform.rotation = Quaternion.RotateTowards(transform.rotation, q, 240f * Time.deltaTime); yield return null; } }

        public void Place(Vector3 p, Vector3 face)
        {
            p.y = Ground.Height(p.x, p.z);
            transform.position = p; face.y = 0f; if (face.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(face);
        }

        /// <summary>Walk to p (on the ground), then idle (or the given state).</summary>
        public Coroutine WalkTo(Vector3 p, float speed = -1f, string after = "idle")
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(Walk(new[] { p }, speed, after, false, 0f));
            return _co;
        }
        /// <summary>Walk a loop of points for ever (pausing at each), e.g. a trader between stalls.</summary>
        public void Loop(Vector3[] pts, float pause = 2f, float speed = -1f)
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(Walk(pts, speed, "idle", true, pause));
        }
        IEnumerator Walk(Vector3[] pts, float speed, string after, bool loop, float pause)
        {
            float v = speed > 0f ? speed : WalkSpeed;
            int k = 0;
            while (true)
            {
                var to = pts[k]; to.y = Ground.Height(to.x, to.z);
                Play("walk", 0.3f);
                if (Anim != null) Anim.speed = Mathf.Clamp(v / Mathf.Max(0.3f, ClipSpeed), 0.55f, 1.6f);
                for (int guard = 0; guard < 100000; guard++)
                {
                    var d = to - transform.position; d.y = 0f;
                    if (d.magnitude < 0.12f) break;
                    var step = Mathf.Min(d.magnitude, v * Time.deltaTime);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 200f * Time.deltaTime);
                    var np = transform.position + d.normalized * step; np.y = Ground.Height(np.x, np.z);
                    transform.position = np; Distance += step;
                    yield return null;
                }
                if (Anim != null) Anim.speed = 1f;
                k++;
                if (k >= pts.Length) { if (!loop) break; k = 0; }
                if (loop && pause > 0f) { Play(Random.value < 0.25f && HasState("talk") ? "talk" : "idle", 0.3f); for (float t = 0f; t < pause * Random.Range(0.6f, 1.4f); t += Time.deltaTime) yield return null; }
            }
            Play(after, 0.35f);
            _co = null;
        }

        void LateUpdate()
        {
            // the voice: the head and upper body move with the loudness (no lip-sync); a soft nod on stressed syllables
            float lvl = 0f;
            if (Voice != null && Voice.isPlaying)
            {
                Voice.GetOutputData(_buf, 0); foreach (var s in _buf) lvl = Mathf.Max(lvl, Mathf.Abs(s));
                if (lvl <= 0.0001f && DemoBoot.Muted) lvl = 0.2f + 0.2f * Mathf.Abs(Mathf.Sin(Time.time * 7.1f)) * Mathf.Abs(Mathf.Sin(Time.time * 2.3f));
            }
            _talk = Mathf.Lerp(_talk, Mathf.Clamp01(lvl * 2.5f), Time.deltaTime * 12f);
            _nod = Mathf.Lerp(_nod, _talk, Time.deltaTime * 6f);
            if (_head != null && _talk > 0.01f) _head.localRotation *= Quaternion.Euler(-_talk * 7f + _nod * 3f, Mathf.Sin(Time.time * 1.7f) * _nod * 3f, 0f);   // the chin lifts as the mouth would open
            if (_chest != null && _talk > 0.01f) _chest.localRotation *= Quaternion.Euler(_nod * 2f, 0f, 0f);
            // Viper's hood: a flare on angry lines (the head and neck widen a little)
            if (Role == "viper" && _head != null)
            {
                float f = 1f + 0.16f * Mathf.Clamp01(Flare);
                _head.localScale = new Vector3(f, 1f, 1f + 0.05f * Mathf.Clamp01(Flare));
                if (_neck != null) _neck.localScale = new Vector3(1f + 0.08f * Mathf.Clamp01(Flare), 1f, 1f);
            }
            UpdateEyes();
            // the Orc Walk is broad in the hips: damp its side sway by a third while walking
            if (CalmSway && _hips != null && _state == "walk")
            {
                if (!_hipsSet) { _hipsRest = _hips.localPosition; _hipsSet = true; }
                var lp = _hips.localPosition; lp.x = Mathf.Lerp(lp.x, _hipsRest.x, 0.35f); _hips.localPosition = lp;
                _hips.localRotation = Quaternion.Slerp(_hips.localRotation, Quaternion.Euler(0f, _hips.localEulerAngles.y, 0f) * Quaternion.identity, 0f);
            }
        }
    }
}
