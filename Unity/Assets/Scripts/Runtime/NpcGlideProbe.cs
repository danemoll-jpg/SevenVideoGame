using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// QF23 (13d) - rule for every NPC in every chapter: nobody moves without a walk / run animation playing at the matching
    /// speed (the seedlings resident glided across the bridge in a static carry pose). In every check run this probe samples
    /// every humanoid NPC 5x a second: when its root moves at a walking pace (0.35 - 8 m/s; faster = a placement) while its
    /// animator is not in a walk / run state, that is a GLIDE. Logged per NPC: <c>NPC GLIDE (QF23 13d)</c> (must be 0).
    /// </summary>
    public class NpcGlideProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook() { SceneManager.sceneLoaded += (s, m) => Attach(); Attach(); }
        static void Attach() { if (DemoBoot.CaptureDir == null || FindAnyObjectByType<NpcGlideProbe>() != null) return; new GameObject("NpcGlideProbe").AddComponent<NpcGlideProbe>(); }

        static readonly string[] Moving = { "walk", "run", "jog", "carry", "swim", "fly", "sneak", "strafe", "step", "walk_v2", "jump" };   // Round 18: a cat's hop is a jump
        readonly Dictionary<Animator, (Vector3 p, int samples, int glides)> _seen = new Dictionary<Animator, (Vector3, int, int)>();
        float _next, _report; List<Animator> _anims = new List<Animator>(); float _refind;
        public static int Knockbacks;   // P3 (3): knockback pushes seen (not glides)
        /// <summary>P3 (3): people riding a vehicle (declared by the director) - carried, not walking.</summary>
        public static readonly HashSet<Transform> Riding = new HashSet<Transform>();
        public static int Glides { get; private set; }

        void Update()
        {
            if (Time.time < _next) return; float dt = 0.2f; _next = Time.time + dt;
            if (Time.timeSinceLevelLoad < 3f) { _seen.Clear(); return; }   // Round 18: the scene placing its people at the start is not walking
            if (Time.time > _refind) { _refind = Time.time + 3f; _anims.Clear(); foreach (var a in FindObjectsByType<Animator>(FindObjectsSortMode.None)) if ((a.isHuman || a.GetComponentInParent<CatFolk>() != null) && a.GetComponentInParent<ChapterSeven>() == null && a.GetComponentInParent<SaucerSeven>() == null && a.name != "7") _anims.Add(a); }
            foreach (var a in _anims)
            {
                if (a == null || !a.isActiveAndEnabled) continue;
                var root = a.transform.parent != null && a.GetComponentInParent<MarsFolk>() != null ? a.GetComponentInParent<MarsFolk>().transform : a.GetComponentInParent<CatFolk>() != null ? a.GetComponentInParent<CatFolk>().transform : a.transform;   // Round 18: the city cats too
                var p = root.position;
                if (!_seen.TryGetValue(a, out var e)) { _seen[a] = (p, 0, 0); continue; }
                var d = p - e.p; d.y = 0f; float v = d.magnitude / dt;
                bool moving = v > 0.35f && v < 8f;
                bool walkAnim = false;
                if (moving)
                {
                    var st = a.GetCurrentAnimatorStateInfo(0); var nx = a.GetNextAnimatorStateInfo(0);
                    foreach (var n in Moving) if (st.IsName(n) || (a.IsInTransition(0) && nx.IsName(n))) { walkAnim = true; break; }
                    var mf = a.GetComponentInParent<MarsFolk>(); if (mf != null && mf.State != null) foreach (var n in Moving) if (mf.State.Contains(n)) walkAnim = true;
                    // P3 (3): a fighter knocked back by a blow (FighterBase.Slide: a short decelerating push under a hit reaction) is not walking
                    var fb = a.GetComponentInParent<FighterBase>(); if (!walkAnim && fb != null && fb.Sliding) { walkAnim = true; Knockbacks++; }
                    if (!walkAnim && (Riding.Contains(root) || Riding.Contains(a.transform))) walkAnim = true;   // carried by a vehicle
                }
                int g = e.glides + (moving && !walkAnim ? 1 : 0);
                if (moving && !walkAnim && e.glides == 0) { var ci = a.GetCurrentAnimatorClipInfo(0); Log($"NPC GLIDE (QF23 13d): '{root.name}' moves at {v:0.0} m/s with no walk playing (clip '{(ci.Length > 0 && ci[0].clip != null ? ci[0].clip.name : "?")}') at {p}"); }
                if (moving && !walkAnim) Glides++;
                _seen[a] = (p, e.samples + (moving ? 1 : 0), g);
            }
            if (Time.time > _report) { _report = Time.time + 60f; Report(); }
        }
        void OnDestroy() { Report(); }
        void Report()
        {
            int moving = 0, glides = 0; var bad = new List<string>();
            foreach (var kv in _seen) { if (kv.Key == null) continue; moving += kv.Value.samples; glides += kv.Value.glides; if (kv.Value.glides > 0) bad.Add($"{kv.Key.name} {kv.Value.glides}"); }
            if (moving == 0 && glides == 0) return;
            Log($"NPC GLIDE (QF23 13d) {SceneManager.GetActiveScene().name}: {moving} samples of NPCs moving at a walking pace, {glides} with no walk / run animation playing (must be 0){(bad.Count > 0 ? " - " + string.Join(", ", bad) : "")}; knockback pushes (a blow's slide, not walking) {Knockbacks}");
        }
        static void Log(string s) { Debug.Log("[Seven] " + s); ChapterCapture.I?.Log(s); }
    }
}
