using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF18 (20) - Dan: "it is almost not obvious that she is having a vision". ONE shared touch -> vision -> return sequence
    /// for every vision (the Moon door, Venus, Europa, Mars and later ones):
    ///  - the reach: a close insert of her hand reaching out and touching the shape (hand IK onto its surface, the gap
    ///    measured and logged), a breath;
    ///  - the shape answers: its glow surges, light runs up from her fingers along her hand and arm, her forehead energy point
    ///    lights bright, a rising tone / shimmer;
    ///  - into the vision: a quick push-in to her eyes (a steady gaze), the light blooms to white - then the caller plays the
    ///    vision video (from white);
    ///  - back (<see cref="Back"/>): the white fades back to her standing with her hand still on the shape, the glow settling,
    ///    then the insert of her hand leaving it.
    /// </summary>
    public static class VisionTouch
    {
        static ChapterCapture Cap => ChapterCapture.I;
        static void Log(string s) { Debug.Log("[Seven] " + s); if (DemoBoot.CaptureDir != null) Cap?.Log(s); }
        static IEnumerator Shot(string n) { if (DemoBoot.CaptureDir != null && Cap != null) yield return Cap.Shot(n); }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        /// <summary>Up to the white. reach = her hand goes onto the shape (false while she rides: a reaching gesture).</summary>
        public static IEnumerator Into(string tag, ChapterSeven s, ChapterCamera cam, ChapterUI ui, Vector3 shape, float radius, System.Action<float> glow, bool reach = true, System.Action<Vector3> floatTo = null)
        {
            // QF24 (20): the music belongs to this shared sequence - it fades DOWN for the vision (suspended, its place kept)
            // and Back brings it back. Before, BIA_11 / AST_08 stopped it outright before the touch: the suspend then had no
            // track to keep, nothing resumed, and the chapter stayed silent to its end.
            if (!MusicSystem.IsSuspended) MusicSystem.Suspend(1.2f);
            _musicTag = tag;
            var fwd = Flat(shape - s.transform.position).normalized; if (fwd.sqrMagnitude < 0.01f) fwd = s.transform.forward;
            var side = Vector3.Cross(Vector3.up, fwd);
            var tik = reach ? (s.GetComponent<SevenTableIK>() ?? s.gameObject.AddComponent<SevenTableIK>()) : null;
            var surf = RuneMesh(shape);                                                 // QF24 (20): the rune's REAL mesh (its surface, not a radius)
            var touch = shape - fwd * (radius + 0.07f) + Vector3.up * 0.01f;            // the wrist placed so the palm / fingers meet the near face
            // the reach - a close insert on her hand and the shape
            // QF23 (22) - the repeat (QF18 20): the sequence was shared, but nothing made sure the hand could ARRIVE: on the
            // mothership she was placed 1.4 m from the octahedron, the target was ~1 m from her shoulder, and the QF13 safety net
            // let the hand go (out of reach) - no contact. Now she steps into reach first, the palm faces the shape (fingers up),
            // and the fingertip gap is measured and corrected onto the surface (logged before / after: must end ~0).
            var tip = s.Bone(HumanBodyBones.RightMiddleDistal) ?? s.Bone(HumanBodyBones.RightMiddleIntermediate) ?? s.Bone(HumanBodyBones.RightHand);
            var hand = s.Bone(HumanBodyBones.RightHand); var shBone = s.Bone(HumanBodyBones.RightUpperArm); var lo = s.Bone(HumanBodyBones.RightLowerArm);
            float handLen = hand != null && tip != null ? Vector3.Distance(hand.position, tip.position) + 0.015f : 0.12f;
            touch = shape - fwd * (radius + handLen * 0.8f) - Vector3.up * handLen * 0.55f;   // QF24 (20): the wrist a little low so the fingers (pointing up) lie on the face at its middle, not above a small rune
            if (tik != null && shBone != null && lo != null && hand != null)
            {
                float arm = Vector3.Distance(shBone.position, lo.position) + Vector3.Distance(lo.position, hand.position);
                float d = Flat(touch - shBone.position).magnitude, want = arm * 0.72f;
                if (d > want)
                {
                    var a0 = s.transform.position; var a1 = a0 + fwd * (d - want);
                    Log($"vision touch (QF23 22) {tag}: she steps {d - want:0.00} m closer - the shape was {d:0.00} m from her shoulder (reach {arm:0.00} m)");
                    for (float t = 0f; t < 1f; t += Time.deltaTime / 0.55f) { s.Place(Vector3.Lerp(a0, a1, Mathf.SmoothStep(0f, 1f, t)), fwd); s.Lock(true); yield return null; }
                    s.Place(a1, fwd); s.Lock(true);
                    for (float t = 0f; t < 0.1f; t += Time.deltaTime) yield return null;
                    Log($"vision touch (QF23 22) {tag}: after the step the shape is {Flat(touch - shBone.position).magnitude:0.00} m from her shoulder");
                }
                // still out of reach (the set keeps her back - a plinth, an altar): she leans in from the waist
                float short2 = Flat(touch - shBone.position).magnitude - arm * 0.8f;
                if (short2 > 0.01f)
                {
                    tik.Lean = Mathf.Clamp(Mathf.Asin(Mathf.Clamp01(short2 / 0.5f)) * Mathf.Rad2Deg * 1.15f, 0f, 38f); tik.LeanDir = fwd;
                    Log($"vision touch (QF23 22) {tag}: she leans in {tik.Lean:0} deg to reach it ({short2:0.00} m short at her shoulder)");
                }
            }
            cam.Shot(touch + side * 0.75f - fwd * 0.25f + Vector3.up * 0.18f, Vector3.Lerp(touch, shape, 0.5f), 0.01f, true, 32f);
            if (tik != null)
            {
                tik.enabled = true; tik.Ease = 2.4f; tik.RightPos = touch; tik.RightW = 1f; tik.LookPos = shape; tik.LookW = 0.85f;
                tik.RightPalmN = -fwd; tik.RightFingerDir = (Vector3.up * 0.85f + fwd * 0.5f).normalized; tik.RightPalmW = 1f;
                tik.RightHint = s.transform.position + Vector3.up * 1.0f + side * 0.35f - fwd * 0.1f; tik.RightHintW = 0.8f;
            }
            else s.Gesture(shape);
            for (float t = 0f; t < 1.0f; t += Time.deltaTime) yield return null;
            // QF24 (20) - the repeat (QF18 20 / QF23 22): the gap was measured to a SPHERE of the given radius (the vertex
            // radius for most runes), so "0 cm" still had her fingertips inside the faces (the octahedron's faces sit at r/sqrt 3).
            // Now it is measured along her reach to the rune's real mesh surface; + = short of the face, - = inside it.
            var surfs = RuneMeshes(shape);                                              // every mesh of the rune (its body, not only its edge lines)
            // the contact is the CLOSEST point of her hand to the surface: the middle fingertip, the index tip and the palm centre
            var idx = s.Bone(HumanBodyBones.RightIndexDistal); var palmB = s.Bone(HumanBodyBones.RightMiddleProximal);
            float Gap() { if (tip == null) return -1f; float g = 1f; foreach (var pt in new[] { tip, idx, palmB }) { if (pt == null) continue; foreach (var sm in surfs) g = Mathf.Min(g, SurfaceGap(sm, pt.position, fwd)); } return g < 0.99f ? g : Vector3.Dot(shape - tip.position, fwd) - radius; }
            float gap0 = Gap(), gap = gap0;
            if (tik != null && tip != null)
                for (int k = 0; k < 6 && Mathf.Abs(gap) > 0.006f; k++)
                {
                    tik.RightPos += fwd * Mathf.Clamp(gap, -0.1f, 0.1f);
                    // R18: the arm at full stretch and still short - she leans in from the waist a little more
                    if (k >= 2 && gap > 0.01f) { tik.Lean = Mathf.Min(38f, tik.Lean + Mathf.Asin(Mathf.Clamp01(gap / 0.5f)) * Mathf.Rad2Deg * 1.3f); tik.LeanDir = fwd; }
                    for (float t = 0f; t < 0.3f; t += Time.deltaTime) yield return null;
                    gap = Gap();
                }
            // a FLOATING shape the set keeps beyond her full reach (the Moon's over its altar) drifts the last centimetres to
            // meet her fingertips - it answers her
            for (int it = 0; it < 2 && floatTo != null && gap > 0.01f && tip != null; it++)   // QF24 (20): twice if needed (measured on the mesh)
            {
                var from = shape; var to = from - fwd * gap;
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f) { shape = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)); floatTo(shape); yield return null; }
                shape = to; floatTo(shape); float g2 = Gap();
                // the insert again, on the hand and the shape where they meet now
                var meet = tip.position;
                cam.Shot(meet + side * 0.75f - fwd * 0.25f + Vector3.up * 0.18f, Vector3.Lerp(meet, shape, 0.5f), 0.01f, true, 32f);
                Log($"vision touch (QF23 22) {tag}: the floating shape drifts {gap * 100f:0.0} cm to meet her fingers (it answers her reach) - fingertips now {g2 * 100f:+0.0;-0.0} cm from its face");
                gap = g2;
            }
            Log($"vision touch (QF18 20 / QF23 22) {tag}: her fingertips {gap * 100f:+0.0;-0.0} cm from the shape's face (before the correction {gap0 * 100f:+0.0;-0.0} cm; must end within 1 cm){(reach ? "" : " - a reach from the bike")}; hand IK weight held (the net did not let go: {(SevenTableIK.LastNet ?? "none")})");
            yield return Shot($"vision_{tag}_1_the_reach");
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) yield return null;     // a breath
            // the shape answers: glow surges, light runs up her arm, her forehead point lights, a rising tone
            AudioKit.Play("energy_point", shape, 0.8f);
            var shimmer = AudioKit.Loop("vision_shimmer", null, null, 0f, AudioKit.Bus.Sfx);
            if (shimmer != null) s.StartCoroutine(AudioKit.Fade(shimmer, 0.7f, 1.6f));
            var runner = new GameObject("VisionArmLight"); var l = runner.AddComponent<Light>(); l.type = LightType.Point; l.color = Color.white; l.range = 0.9f; l.intensity = 0f; l.shadows = LightShadows.None;
            var dot = new GameObject("VisionArmGlow"); dot.AddComponent<MeshFilter>().sharedMesh = Markers.Rock(11, Vector3.one * 0.035f); var dr = dot.AddComponent<MeshRenderer>(); dr.sharedMaterial = Markers.Additive(2.2f); dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var path = new[] { HumanBodyBones.RightHand, HumanBodyBones.RightLowerArm, HumanBodyBones.RightUpperArm, HumanBodyBones.RightShoulder, HumanBodyBones.Neck, HumanBodyBones.Head };
            cam.Shot(touch + side * 1.5f - fwd * 0.9f + Vector3.up * 0.35f, (touch + s.transform.position + Vector3.up * 1.4f) * 0.5f, 0.01f, true, 40f);
            bool shotMid = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.3f)
            {
                glow?.Invoke(Mathf.Lerp(1f, 4.5f, Mathf.Clamp01(t * 1.6f)));
                float f = t * (path.Length - 1); int i = Mathf.Min(path.Length - 2, Mathf.FloorToInt(f));
                var a = s.Bone(path[i]); var b = s.Bone(path[i + 1]);
                if (a != null && b != null) { var p = Vector3.Lerp(a.position, b.position, f - i); l.transform.position = p; dot.transform.position = p; }
                l.intensity = 3.5f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) + 1f;
                if (t > 0.6f) s.SetEnergyPoint(2.8f, 0.3f);
                if (!shotMid && t > 0.5f) { shotMid = true; s.StartCoroutine(Shot($"vision_{tag}_2_light_runs_up_her_arm")); }
                yield return null;
            }
            Object.Destroy(runner); Object.Destroy(dot);
            // into the vision: a quick push-in to her eyes (a steady gaze), the light blooms to white
            var head = s.Bone(HumanBodyBones.Head); var eyes = head != null ? head.position + Vector3.up * 0.06f + Flat(s.transform.forward).normalized * 0.08f : s.transform.position + Vector3.up * 1.62f;
            var face = Flat(s.transform.forward).normalized; var camFrom = eyes + face * 0.95f + side * 0.25f; var camTo = eyes + face * 0.34f + side * 0.06f;
            // QF24 (20): the push-in came straight in front of her face - exactly where the rune is - so the rune filled a third
            // of the frame. Swing the approach round her until the line to her eyes passes clear of the rune (its real bounds).
            {
                var rm = surf != null ? surf.GetComponent<Renderer>() : null; float rr = rm != null ? rm.bounds.extents.magnitude : radius;
                float Clear(Vector3 a2) { var seg = eyes - a2; float u = Mathf.Clamp01(Vector3.Dot(shape - a2, seg) / seg.sqrMagnitude); return (a2 + seg * u - shape).magnitude - rr; }
                float best = Mathf.Min(Clear(camFrom), Clear(camTo)); float bestA = 0f;
                if (best < 0.08f)
                    foreach (var ang in new[] { 35f, -35f, 55f, -55f, 75f, -75f, 95f, -95f })
                    {
                        var d2 = Quaternion.Euler(0f, ang, 0f) * face; var f2 = eyes + d2 * 0.95f + Vector3.up * 0.04f; var t2 = eyes + d2 * 0.34f;
                        float c = Mathf.Min(Clear(f2), Clear(t2)); if (c > best) { best = c; bestA = ang; camFrom = f2; camTo = t2; }
                        if (c >= 0.08f) break;
                    }
                Log($"vision push-in (QF24 20) {tag}: the line to her eyes passes {best * 100f:0} cm clear of the rune (approach swung {bestA:0} deg)");
            }
            AudioKit.Play("vision_whoosh", null, 0.9f);
            ui.SetFade(0f, 1f);
            bool shotEyes = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.1f)
            {
                cam.Shot(Vector3.Lerp(camFrom, camTo, Mathf.SmoothStep(0f, 1f, t)), eyes, 0.01f, true, 30f);
                if (!shotEyes && t > 0.45f) { shotEyes = true; s.StartCoroutine(Shot($"vision_{tag}_3_push_in_to_her_eyes")); }
                if (t > 0.55f) ui.SetFade(Mathf.SmoothStep(0f, 1f, (t - 0.55f) / 0.45f), 1f);
                yield return null;
            }
            ui.SetFade(1f, 1f);
            if (shimmer != null) s.StartCoroutine(AudioKit.Fade(shimmer, 0f, 2.5f, true));
            Log($"vision touch (QF18 20) {tag}: the reach, the shape answering (glow x4.5, light up her arm, the forehead point bright), the push-in to her eyes, the bloom to white");
        }

        /// <summary>After the vision (the screen fading back from white): her hand still on the shape, the glow settling, then
        /// the insert of her hand leaving it.</summary>
        static string _musicTag;
        /// <summary>QF24 (20): the music comes back after every vision (the suspended track from where it left off; if none was
        /// playing, the area's theme) - never silent for the rest of the chapter.</summary>
        public static void MusicBack(string tag)
        {
            if (MusicSystem.IsSuspended) MusicSystem.Resume(3.5f);
            else if (MusicSystem.Current == null) { var th = MusicSystem.Theme(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name); if (th != null) MusicSystem.Play(th, 3.5f); }
            Log($"MUSIC AFTER VISION (QF24 20) {tag}: '{MusicSystem.Current ?? "(none)"}' playing again");
        }
        public static IEnumerator Back(string tag, ChapterSeven s, ChapterCamera cam, Vector3 shape, float radius, System.Action<float> glow, bool reach = true)
        {
            MusicBack(tag);
            var fwd = Flat(shape - s.transform.position).normalized; if (fwd.sqrMagnitude < 0.01f) fwd = s.transform.forward;
            var side = Vector3.Cross(Vector3.up, fwd);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f) { glow?.Invoke(Mathf.Lerp(4.5f, 1f, t)); s.SetEnergyPoint(Mathf.Lerp(2.8f, 1f, t), 0.1f); yield return null; }
            yield return Shot($"vision_{tag}_4_back_her_hand_still_on_it");
            var touch = shape - fwd * (radius + 0.07f);
            HideRuneFromFace(s, shape);                                                 // QF24 (20): never over her face afterwards
            cam.Shot(touch + side * 0.7f - fwd * 0.3f + Vector3.up * 0.16f, Vector3.Lerp(touch, shape, 0.4f), 0.01f, true, 32f);
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) yield return null;
            var tik = reach ? s.GetComponent<SevenTableIK>() : null;
            if (tik != null) { tik.RightPos = touch - fwd * 0.25f + Vector3.down * 0.12f; for (float t = 0f; t < 0.5f; t += Time.deltaTime) yield return null; yield return Shot($"vision_{tag}_5_her_hand_leaves_it"); tik.ReleaseAll("the vision touch ends"); }
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) yield return null;
            Log($"vision touch (QF18 20) {tag}: back - her hand on the shape as the white clears, the glow settling, the insert of her hand leaving it");
        }

        // ---- QF24 (20): the rune's real surface ----------------------------------------------------------------------------
        /// <summary>The rune's body mesh at <paramref name="shape"/> (the closest MeshFilter whose bounds hold the point).</summary>
        static MeshFilter RuneMesh(Vector3 shape)
        {
            MeshFilter best = null; float bd = 0.6f;
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (mf.sharedMesh == null || !mf.gameObject.activeInHierarchy) continue;
                var r = mf.GetComponent<Renderer>(); if (r == null || !r.enabled) continue;
                var b = r.bounds; if (!b.Contains(shape) || b.size.magnitude > 4.5f) continue;
                float d = (b.center - shape).magnitude; if (d < bd && mf.sharedMesh.isReadable) { bd = d; best = mf; }
            }
            return best;
        }
        /// <summary>QF24 (20): every readable mesh whose bounds hold the shape's centre (the body; the edge lines alone missed the ray).</summary>
        static System.Collections.Generic.List<MeshFilter> RuneMeshes(Vector3 shape)
        {
            var l = new System.Collections.Generic.List<MeshFilter>();
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (mf.sharedMesh == null || !mf.gameObject.activeInHierarchy || !mf.sharedMesh.isReadable) continue;
                var r = mf.GetComponent<Renderer>(); if (r == null || !r.enabled) continue;
                var b = r.bounds; if (b.Contains(shape) && b.size.magnitude < 4.5f) l.Add(mf);
            }
            return l;
        }
        /// <summary>Distance from a point along a direction to the mesh surface (ray-triangle, both faces), minus nothing:
        /// + = the surface is ahead of the point (short of it), - = the point is already inside (behind the surface).</summary>
        public static float SurfaceGap(MeshFilter mf, Vector3 p, Vector3 dir)
        {
            var m = mf.sharedMesh; var v = m.vertices; var t = m.triangles; var tr = mf.transform;
            var o = p - dir * 0.6f; float best = float.MaxValue;
            for (int i = 0; i + 2 < t.Length; i += 3)
            {
                var a = tr.TransformPoint(v[t[i]]); var b = tr.TransformPoint(v[t[i + 1]]); var c = tr.TransformPoint(v[t[i + 2]]);
                if (Ray(o, dir, a, b, c, out float d) && d < best) best = d;
            }
            return best == float.MaxValue ? 1f : best - 0.6f;
        }
        static bool Ray(Vector3 o, Vector3 d, Vector3 a, Vector3 b, Vector3 c, out float dist)
        {
            dist = 0f; var e1 = b - a; var e2 = c - a; var h = Vector3.Cross(d, e2); float det = Vector3.Dot(e1, h);
            if (Mathf.Abs(det) < 1e-8f) return false;
            float f = 1f / det; var s = o - a; float u = f * Vector3.Dot(s, h); if (u < 0f || u > 1f) return false;
            var q = Vector3.Cross(s, e1); float w = f * Vector3.Dot(d, q); if (w < 0f || u + w > 1f) return false;
            dist = f * Vector3.Dot(e2, q); return dist > 0f;
        }
        /// <summary>The rune registered as a prop that must never cover a speaker's face (the post-vision talk's framings).</summary>
        static void HideRuneFromFace(ChapterSeven s, Vector3 shape)
        {
            var mf = RuneMesh(shape); if (mf == null) return;
            var root = mf.transform.parent != null ? mf.transform.parent : mf.transform;
            DialogueFraming.Props.RemoveAll(x => x.t == null || x.t == root);
            DialogueFraming.Props.Add((root, mf.GetComponent<Renderer>().bounds.extents.magnitude * 0.85f));
        }
    }
}
