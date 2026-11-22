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
            var fwd = Flat(shape - s.transform.position).normalized; if (fwd.sqrMagnitude < 0.01f) fwd = s.transform.forward;
            var side = Vector3.Cross(Vector3.up, fwd);
            var tik = reach ? (s.GetComponent<SevenTableIK>() ?? s.gameObject.AddComponent<SevenTableIK>()) : null;
            var touch = shape - fwd * (radius + 0.07f) + Vector3.up * 0.01f;            // the wrist placed so the palm / fingers meet the near face
            // the reach - a close insert on her hand and the shape
            // QF23 (22) - the repeat (QF18 20): the sequence was shared, but nothing made sure the hand could ARRIVE: on the
            // mothership she was placed 1.4 m from the octahedron, the target was ~1 m from her shoulder, and the QF13 safety net
            // let the hand go (out of reach) - no contact. Now she steps into reach first, the palm faces the shape (fingers up),
            // and the fingertip gap is measured and corrected onto the surface (logged before / after: must end ~0).
            var tip = s.Bone(HumanBodyBones.RightMiddleDistal) ?? s.Bone(HumanBodyBones.RightMiddleIntermediate) ?? s.Bone(HumanBodyBones.RightHand);
            var hand = s.Bone(HumanBodyBones.RightHand); var shBone = s.Bone(HumanBodyBones.RightUpperArm); var lo = s.Bone(HumanBodyBones.RightLowerArm);
            float handLen = hand != null && tip != null ? Vector3.Distance(hand.position, tip.position) + 0.015f : 0.12f;
            touch = shape - fwd * (radius + handLen * 0.8f) + Vector3.up * 0.02f;
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
            float Gap() => tip != null ? Vector3.Dot(shape - tip.position, fwd) - radius : -1f;     // + = short of the face, - = inside it
            float gap0 = Gap(), gap = gap0;
            if (tik != null && tip != null)
                for (int k = 0; k < 3 && Mathf.Abs(gap) > 0.006f; k++)
                {
                    tik.RightPos += fwd * Mathf.Clamp(gap, -0.1f, 0.1f);
                    for (float t = 0f; t < 0.3f; t += Time.deltaTime) yield return null;
                    gap = Gap();
                }
            // a FLOATING shape the set keeps beyond her full reach (the Moon's over its altar) drifts the last centimetres to
            // meet her fingertips - it answers her
            if (floatTo != null && gap > 0.01f && tip != null)
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
        public static IEnumerator Back(string tag, ChapterSeven s, ChapterCamera cam, Vector3 shape, float radius, System.Action<float> glow, bool reach = true)
        {
            var fwd = Flat(shape - s.transform.position).normalized; if (fwd.sqrMagnitude < 0.01f) fwd = s.transform.forward;
            var side = Vector3.Cross(Vector3.up, fwd);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f) { glow?.Invoke(Mathf.Lerp(4.5f, 1f, t)); s.SetEnergyPoint(Mathf.Lerp(2.8f, 1f, t), 0.1f); yield return null; }
            yield return Shot($"vision_{tag}_4_back_her_hand_still_on_it");
            var touch = shape - fwd * (radius + 0.07f);
            cam.Shot(touch + side * 0.7f - fwd * 0.3f + Vector3.up * 0.16f, Vector3.Lerp(touch, shape, 0.4f), 0.01f, true, 32f);
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) yield return null;
            var tik = reach ? s.GetComponent<SevenTableIK>() : null;
            if (tik != null) { tik.RightPos = touch - fwd * 0.25f + Vector3.down * 0.12f; for (float t = 0f; t < 0.5f; t += Time.deltaTime) yield return null; yield return Shot($"vision_{tag}_5_her_hand_leaves_it"); tik.ReleaseAll("the vision touch ends"); }
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) yield return null;
            Log($"vision touch (QF18 20) {tag}: back - her hand on the shape as the white clears, the glow settling, the insert of her hand leaving it");
        }
    }
}
