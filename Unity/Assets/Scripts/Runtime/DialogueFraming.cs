using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF18 (7) - standing rule "every dialogue shot shows the speaker" (CLAUDE.md "Show, don't caption"). Dan: the worker's
    /// "Those are water filters" was a shot of the containers with the worker hidden behind them; Viper's lines were close-ups
    /// of a guard. Before every line that plays in a cutscene shot (any chapter: it runs inside DialoguePlayer.PlayOne), the
    /// speaker's head and chest must be inside the frame with a clear line from the camera (walls, props and other people
    /// all count). If not, another angle is chosen - over the listener's shoulder, a side two-shot either side, a single
    /// from the front - and the camera cuts there. Telepathic lines (Panther) are exempt. Every check run logs
    /// "DIALOGUE FRAMING (QF18 7)" with the lines checked, reframed and (must be 0) still blocked.
    /// </summary>
    public static class DialogueFraming
    {
        /// <summary>Optional: a director's own speaker lookup (speaker id -> transform). Falls back to scene objects by name.</summary>
        public static System.Func<string, Transform> Speaker;
        public static int Checked, Visible, Reframed, Blocked;
        /// <summary>QF24 (4 / 7): a line that plays deliberately OVER a shot of something else (the saucer flying inside the
        /// mothership, the memory she asks about): the reason, set by the director for that shot; null = the normal rule.</summary>
        public static string VoiceOver;
        /// <summary>QF24 (20): props that must never cover a speaker's face (the runes during the post-vision talk) - the
        /// segment camera -> face may not pass within the radius of any of them.</summary>
        public static readonly List<(Transform t, float r)> Props = new List<(Transform, float)>();
        public static int VoiceOvers, Telepathic, TelepathicReframed, PropBlocks;
        /// <summary>QF24 (7): play a line (or lines) OVER the shot of the thing it refers to ("What was that?", "Look at this",
        /// "There.") - the shot shows the thing, her voice off-screen; the speaker check does not cut away to her face.</summary>
        public static System.Collections.IEnumerator Over(string what, System.Collections.IEnumerator say)
        {
            var prev = VoiceOver; VoiceOver = what;
            try { yield return say; } finally { VoiceOver = prev; }
        }
        public static readonly List<string> Changes = new List<string>();
        static readonly Dictionary<string, Transform> _cache = new Dictionary<string, Transform>();
        static UnityEngine.SceneManagement.Scene _cacheScene;

        static Transform Find(string id, string label)
        {
            var t = Speaker?.Invoke(id);
            if (t != null) return t;
            var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (sc != _cacheScene) { _cache.Clear(); _cacheScene = sc; }
            string key = id + "|" + label;
            if (_cache.TryGetValue(key, out t) && t != null && t.gameObject.activeInHierarchy) return t;
            foreach (var n in new[] { id == "seven" ? "7" : null, label, id, Cap(id), Cap(label) })
            {
                if (string.IsNullOrEmpty(n)) continue;
                var go = GameObject.Find(n);
                if (go != null && go.activeInHierarchy) { _cache[key] = go.transform; return go.transform; }
            }
            return null;
        }
        static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        static Vector3 Head(Transform t)
        {
            var rr = t.GetComponentInChildren<RatwingRig>(); if (rr != null && rr.HeadBone != null) return rr.HeadBone.position;   // QF24 (9)
            var an = t.GetComponentInChildren<Animator>();
            if (an != null && an.isHuman) { var h = an.GetBoneTransform(HumanBodyBones.Head); if (h != null) return h.position + Vector3.up * 0.06f; }
            return t.position + Vector3.up * 1.6f * t.lossyScale.y;
        }
        static Vector3 Chest(Transform t)
        {
            var rr = t.GetComponentInChildren<RatwingRig>(); if (rr != null && rr.ChestBone != null) return rr.ChestBone.position;   // QF24 (9)
            var an = t.GetComponentInChildren<Animator>();
            if (an != null && an.isHuman) { var c = an.GetBoneTransform(HumanBodyBones.UpperChest) ?? an.GetBoneTransform(HumanBodyBones.Chest); if (c != null) return c.position; }
            return t.position + Vector3.up * 1.2f * t.lossyScale.y;
        }

        /// <summary>QF21 (7): the share of the frame a character's body takes (its renderers' bounds projected, clipped to the
        /// frame; 0..1). Behind the camera / straddling it counts as the whole frame.</summary>
        public static float Coverage(Camera cam, Vector3 pos, Vector3 look, float fov, Transform t)
        {
            if (t == null) return 0f;
            var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return 0f;
            Bounds b = default; bool any = false;
            foreach (var r in rs) { if (!r.enabled || r is ParticleSystemRenderer) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            if (!any) return 0f;
            var rot = Quaternion.LookRotation((look - pos).sqrMagnitude > 1e-6f ? look - pos : Vector3.forward); var inv = Quaternion.Inverse(rot);
            float tanV = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad), tanH = tanV * (cam != null ? cam.aspect : 16f / 9f);
            float x0 = 1f, x1 = -1f, y0 = 1f, y1 = -1f; int front = 0;
            if (b.Contains(pos)) return 1f;
            bool near = b.SqrDistance(pos) < 2.5f * 2.5f;
            for (int i = 0; i < 8; i++)
            {
                var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var l = inv * (c - pos);
                if (l.z < 0.15f) { if (near) return 0.85f; continue; }   // QF21: a body behind the camera, far away, covers nothing
                front++;
                float x = l.x / (l.z * tanH), y = l.y / (l.z * tanV);
                x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
            }
            if (front == 0) return 0f;
            x0 = Mathf.Max(x0, -1f); x1 = Mathf.Min(x1, 1f); y0 = Mathf.Max(y0, -1f); y1 = Mathf.Min(y1, 1f);
            if (x1 <= x0 || y1 <= y0) return 0f;
            return (x1 - x0) * (y1 - y0) / 4f;
        }
        static float HeightOf(Transform t) { var r = t.GetComponentInChildren<SkinnedMeshRenderer>(); return r != null ? r.bounds.size.y : 1.7f; }
        public static int BigBodyRejects;
        public static string LastFail;

        /// <summary>Is the speaker visible from a camera at pos looking at look (fov)? QF21 (7): a big character (Machinist,
        /// Tunneler, Viper, the keeper - over 2.4 m) who is not the speaker and fills more than half the frame fails the shot
        /// (the frame filled by his armour, the listener / speaker hidden).</summary>
        public static bool Sees(Camera cam, Vector3 pos, Vector3 look, float fov, Transform who, Transform[] others)
        {
            if (others != null)
                foreach (var o in others)
                {
                    if (o == null || o == who || !o.gameObject.activeInHierarchy || HeightOf(o) < 2.4f) continue;
                    if (Coverage(cam, pos, look, fov, o) > 0.5f) { BigBodyRejects++; LastFail = "big body " + o.name; return false; }
                }
            var rot = Quaternion.LookRotation((look - pos).sqrMagnitude > 1e-6f ? look - pos : Vector3.forward);
            foreach (var p in new[] { Head(who), Chest(who) })
            {
                var local = Quaternion.Inverse(rot) * (p - pos);
                if (local.z < 0.3f) { LastFail = "behind the camera"; return false; }
                float tanV = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad), tanH = tanV * (cam != null ? cam.aspect : 16f / 9f);
                if (Mathf.Abs(local.x / local.z) > tanH * 0.92f || Mathf.Abs(local.y / local.z) > tanV * 0.92f) { LastFail = "out of frame"; return false; }
                if (!ChapterCamera.ClearLine(pos, p)) { LastFail = "a wall / prop: " + ChapterCamera.LastBlocker; return false; }
                foreach (var pr in Props)
                {
                    if (pr.t == null || !pr.t.gameObject.activeInHierarchy) continue;
                    var seg2 = p - pos; float u2 = Mathf.Clamp01(Vector3.Dot(pr.t.position - pos, seg2) / seg2.sqrMagnitude);
                    if (u2 > 0.02f && u2 < 0.98f && (pos + seg2 * u2 - pr.t.position).magnitude < pr.r) { PropBlocks++; LastFail = "a prop over the face: " + pr.t.name; return false; }
                }
                // another person in the way
                if (others != null)
                    foreach (var o in others)
                    {
                        if (o == null || o == who || !o.gameObject.activeInHierarchy) continue;
                        var q = o.position + Vector3.up * 1.1f * o.lossyScale.y; var seg = p - pos;
                        float u = Mathf.Clamp01(Vector3.Dot(q - pos, seg) / seg.sqrMagnitude);
                        if (u > 0.05f && u < 0.95f && (pos + seg * u - q).magnitude < 0.42f * Mathf.Max(0.8f, o.lossyScale.x)) { LastFail = "person " + o.name; return false; }
                    }
            }
            return true;
        }

        /// <summary>Called at the start of every line (DialoguePlayer.PlayOne).</summary>
        public static void Check(VoiceBank.Line line)
        {
            if (line == null) return;
            var cam = Camera.main; if (cam == null) return;
            var cc = cam.GetComponent<ChapterCamera>();
            if (cc == null || !cc.InShot) return;                          // gameplay lines (follow camera) are not shots
            if (VoiceOver != null) { VoiceOvers++; Log($"framing (QF24 7): line {line.Id} ({line.Label}) plays over {VoiceOver}"); return; }
            // QF24 (13): a TELEPATHIC line (Panther; Nela when she is not on screen) is exempt from "the speaker in frame", but
            // never from "something in frame": 7 must be in the shot - or the cutaway to Panther aboard is showing
            bool telepathic = line.IsCat || line.Speaker == "nela";
            var who = telepathic ? null : Find(line.Speaker, line.Label);
            if (telepathic)
            {
                if (line.IsCat && AboardCut.I != null && AboardCut.I.Showing) { Telepathic++; return; }
                if (line.Speaker == "nela") who = Find(line.Speaker, line.Label);
                if (who == null) who = Find("seven", "7");
                if (who == null) return;
                Telepathic++;
            }
            if (who == null) return;
            Checked++;
            var people = People();
            var pos = cc.ShotPos; var look = cc.ShotLook; float fov = cam.fieldOfView;
            if (Sees(cam, pos, look, fov, who, people)) { Visible++; return; }
            string firstFail = LastFail;
            // pick another angle: over the listener's shoulder, side two-shots, a single from the front
            var listener = line.Speaker == "seven" ? Nearest(who, people) : Find("seven", "7");
            var h = Head(who);
            var cands = new List<(Vector3 p, Vector3 l, float f, string what)>();
            var fwd = Flat(who.forward); if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            if (listener != null)
            {
                var lh = Head(listener); var d = Flat(h - lh).normalized; if (d.sqrMagnitude < 0.01f) d = fwd; var side = Vector3.Cross(Vector3.up, d);
                foreach (var s in new[] { 1f, -1f })
                    cands.Add((lh - d * 1.15f + side * 0.6f * s + Vector3.up * 0.15f, h - Vector3.up * 0.08f, 40f, "over the listener's shoulder"));
                var mid = (h + lh) * 0.5f; float span = Flat(h - lh).magnitude;
                foreach (var s in new[] { 1f, -1f }) foreach (var k in new[] { 1f, 1.5f })
                    cands.Add((mid + side * s * Mathf.Max(2.6f, span * 0.9f) * k + Vector3.up * 0.1f, mid - Vector3.up * 0.15f, 46f, "side two-shot"));
            }
            foreach (var a in new[] { 0f, 35f, -35f, 70f, -70f })
                cands.Add((h + Quaternion.Euler(0f, a, 0f) * fwd * 2.6f - Vector3.up * 0.1f, h - Vector3.up * 0.15f, 38f, "single from the front"));
            foreach (var c in cands)
            {
                if (RideProbe.Overlap(c.p, 0.25f, null, null, false)) continue;
                if (!Sees(cam, c.p, c.l, c.f, who, people)) continue;
                cc.Shot(c.p, c.l, 0.01f, true, c.f);
                Reframed++; if (telepathic) TelepathicReframed++;
                var msg = $"framing (QF18 7): line {line.Id} ({line.Label}) - the speaker was hidden or out of frame; reframed: {c.what}";
                Changes.Add(line.Id + ": " + c.what);
                Log(msg);
                return;
            }
            Blocked++;
            Log($"framing (QF18 7): line {line.Id} ({line.Label}) - STILL BLOCKED (no clear angle found; the shot failed on: {firstFail})");
        }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        static Transform[] People()
        {
            var l = new List<Transform>();
            foreach (var a in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None)) if (a.isHuman && a.gameObject.activeInHierarchy) l.Add(a.transform);
            return l.ToArray();
        }
        static Transform Nearest(Transform who, Transform[] people)
        {
            Transform best = null; float bd = 12f;
            foreach (var p in people) { if (p == who) continue; float d = Vector3.Distance(p.position, who.position); if (d < bd) { bd = d; best = p; } }
            return best;
        }
        static void Log(string s) { Debug.Log("[Seven] " + s); if (DemoBoot.CaptureDir != null) ChapterCapture.I?.Log(s); }
        public static string Summary() => $"DIALOGUE FRAMING (QF18 7): {Checked} lines in cutscene shots checked, speaker visible {Visible}, reframed {Reframed}, still blocked {Blocked} (must be 0); QF21 (7): shots rejected for a big body filling over half the frame {BigBodyRejects}; QF24 (13): telepathic lines {Telepathic} (7 or Panther's cutaway in frame; reframed to 7 {TelepathicReframed}); QF24 (20): angles rejected for a prop over the face {PropBlocks}; QF24 (4/7): declared voice-overs {VoiceOvers}{(Changes.Count > 0 ? "; changed: " + string.Join("; ", Changes) : "")}";
    }
}
