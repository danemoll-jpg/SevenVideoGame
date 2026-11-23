using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF28 (5): how 7's hands hold a rune / object - one solver for every hold (Dan: "7's hands are all messed up when she holds
    /// the sphere": the palms were pushed against its sides with the clip's wrist angles and fixed finger curls, so fingers went
    /// through it and the wrists twisted).
    ///  - the object's real surface = a convex hull of its mesh (<see cref="Surface"/>: signed distance, + outside);
    ///  - <see cref="Cup"/>: both palms (or one) under / round the object, each hand turned so its palm faces the object and its
    ///    fingers run up round it, the arm solved two-bone to put the palm ON the surface (the elbow low and out), then
    ///    <see cref="CurlTo"/>: every finger joint closes a few degrees at a time until that finger touches the surface - never
    ///    past it (no finger bone inside), never more than a relaxed curl;
    ///  - <see cref="Measure"/>: the deepest finger point vs the surface (must be ≥ -3 mm) and the wrist bend vs the forearm
    ///    (natural ≤ 70°). Check runs log `HOLD (QF28 5)`; the build check runs the same solver on her real rig.
    /// Everything runs after the animator (LateUpdate), so it follows every clip.
    /// </summary>
    public static class HandHold
    {
        public const float Skin = 0.004f, WristMax = 70f, InsideMax = -0.003f;

        /// <summary>A convex surface built from a mesh (planes of its triangles, merged): signed distance in world space.</summary>
        public class Surface
        {
            readonly List<Vector4> _pl = new List<Vector4>(); Transform _t; Vector3 _c; float _r = -1f;
            public Surface(Transform t, Mesh m) { _t = t; if (m == null || !m.isReadable) return; var v = m.vertices; var tr = m.triangles; var c = Vector3.zero; foreach (var p in v) c += p; c /= Mathf.Max(1, v.Length); _c = c;
                for (int i = 0; i + 2 < tr.Length; i += 3) { var n = Vector3.Cross(v[tr[i + 1]] - v[tr[i]], v[tr[i + 2]] - v[tr[i]]); if (n.sqrMagnitude < 1e-12f) continue; n.Normalize(); float h = Vector3.Dot(n, v[tr[i]] - c); if (h < 0f) { n = -n; h = -h; }
                    bool dup = false; foreach (var q in _pl) if (Vector3.Dot(new Vector3(q.x, q.y, q.z), n) > 0.9995f && Mathf.Abs(q.w - h) < 1e-4f) { dup = true; break; } if (!dup) _pl.Add(new Vector4(n.x, n.y, n.z, h)); } }
            /// <summary>A sphere (no mesh): centre transform + radius.</summary>
            public Surface(Transform t, float radius) { _t = t; _r = radius; }
            public Vector3 Centre => _t != null ? _t.TransformPoint(_c) : Vector3.zero;
            public float Dist(Vector3 world)
            {
                if (_t == null) return 1f;
                if (_r > 0f) return (world - _t.position).magnitude - _r * _t.lossyScale.x;
                var p = _t.InverseTransformPoint(world) - _c; float s = _t.lossyScale.x, d = float.MinValue;
                foreach (var q in _pl) d = Mathf.Max(d, Vector3.Dot(new Vector3(q.x, q.y, q.z), p) - q.w);
                return _pl.Count == 0 ? 1f : d * s;
            }
            /// <summary>The outward normal of the face a direction from the centre meets (a sphere: the direction itself).</summary>
            public Vector3 NormalAlong(Vector3 dirWorld)
            {
                if (_r > 0f || _pl.Count == 0) return dirWorld.normalized;
                var d = _t.InverseTransformDirection(dirWorld).normalized; float best = float.MaxValue; Vector3 nb = d;
                foreach (var q in _pl) { var qn = new Vector3(q.x, q.y, q.z); float k = Vector3.Dot(qn, d); if (k > 1e-4f && q.w / k < best) { best = q.w / k; nb = qn; } }
                return _t.TransformDirection(nb).normalized;
            }
            /// <summary>Distance from the centre to the surface along a direction (the palm contact point).</summary>
            public float Along(Vector3 dirWorld)
            {
                if (_r > 0f) return _r * _t.lossyScale.x;
                var d = _t.InverseTransformDirection(dirWorld).normalized; float best = float.MaxValue;
                foreach (var q in _pl) { float k = Vector3.Dot(new Vector3(q.x, q.y, q.z), d); if (k > 1e-4f) best = Mathf.Min(best, q.w / k); }
                return (best == float.MaxValue ? 0.1f : best) * _t.lossyScale.x;
            }
        }

        // ---------------------------------------------------------------- bones
        static HumanBodyBones HB(bool right, string n) => (HumanBodyBones)System.Enum.Parse(typeof(HumanBodyBones), (right ? "Right" : "Left") + n);
        public static Transform[][] Fingers(Animator an, bool right)
        {
            var names = new[] { "Index", "Middle", "Ring", "Little", "Thumb" };
            var r = new Transform[5][];
            for (int f = 0; f < 5; f++) r[f] = new[] { an.GetBoneTransform(HB(right, names[f] + "Proximal")), an.GetBoneTransform(HB(right, names[f] + "Intermediate")), an.GetBoneTransform(HB(right, names[f] + "Distal")) };
            return r;
        }
        static Vector3 Tip(Transform[] ch) { var d = ch[2] != null ? ch[2] : ch[1]; var i = ch[2] != null ? ch[1] : ch[0]; if (d == null || i == null) return ch[0] != null ? ch[0].position : Vector3.zero; return d.position + (d.position - i.position) * 0.8f; }

        /// <summary>The palm: centre (on the skin, palm side), its normal (out of the palm), the finger direction, across.</summary>
        public static bool Palm(Animator an, bool right, out Vector3 centre, out Vector3 normal, out Vector3 along)
        {
            centre = normal = along = Vector3.zero;
            var hand = an.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            var idx = an.GetBoneTransform(HB(right, "IndexProximal")); var mid = an.GetBoneTransform(HB(right, "MiddleProximal")); var lit = an.GetBoneTransform(HB(right, "LittleProximal")); var th = an.GetBoneTransform(HB(right, "ThumbProximal"));
            if (hand == null || mid == null || idx == null || lit == null) return false;
            along = (mid.position - hand.position).normalized;
            var across = (lit.position - idx.position).normalized;
            normal = Vector3.Cross(along, across).normalized;
            var midC = Vector3.Lerp(hand.position, mid.position, 0.62f);
            // the palm side: relaxed fingers curl toward it (the middle finger's distal joint off the line of the hand) - the thumb's
            // base is nearly IN the palm plane on her rig, so it only decides when the fingers are straight (QF28 5: with the thumb
            // alone the sign flipped and the back of the hand was laid on the object, the thumb inside it)
            var midD = an.GetBoneTransform(HB(right, "MiddleDistal"));
            var curl = midD != null ? Vector3.ProjectOnPlane(midD.position - mid.position, along) : Vector3.zero;
            if (curl.magnitude > 0.004f) { if (Vector3.Dot(curl, normal) < 0f) normal = -normal; }
            else if (th != null && Vector3.Dot(th.position - midC, normal) < 0f) normal = -normal;
            centre = midC + normal * 0.012f;                                                         // the palm's skin, not the bone
            return true;
        }

        // ---------------------------------------------------------------- the arm and the hand
        /// <summary>Two-bone arm: the wrist to <paramref name="target"/>, the elbow toward <paramref name="pole"/> (weight w).</summary>
        public static void ArmTo(Animator an, bool right, Vector3 target, Vector3 pole, float w)
        {
            var u = an.GetBoneTransform(right ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm);
            var lo = an.GetBoneTransform(right ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm);
            var h = an.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            if (u == null || lo == null || h == null || w <= 0.001f) return;
            var hr = h.rotation;
            float la = Vector3.Distance(u.position, lo.position), lb = Vector3.Distance(lo.position, h.position);
            var toT = target - u.position; float d = Mathf.Clamp(toT.magnitude, Mathf.Abs(la - lb) + 0.01f, (la + lb) * 0.999f); var dir = toT.normalized;
            var pn = Vector3.ProjectOnPlane(pole - u.position, dir).normalized; if (pn.sqrMagnitude < 1e-4f) pn = Vector3.down;
            float A = Mathf.Acos(Mathf.Clamp((la * la + d * d - lb * lb) / (2f * la * d), -1f, 1f));
            var elbow = u.position + dir * Mathf.Cos(A) * la + pn * Mathf.Sin(A) * la;
            u.rotation = Quaternion.Slerp(u.rotation, Quaternion.FromToRotation(lo.position - u.position, elbow - u.position) * u.rotation, w);
            lo.rotation = Quaternion.Slerp(lo.rotation, Quaternion.FromToRotation(h.position - lo.position, u.position + dir * d - lo.position) * lo.rotation, w);
            h.rotation = hr;
        }
        /// <summary>The world rotation the hand needs so its palm faces along <paramref name="palmN"/> and its fingers run along
        /// <paramref name="fingerDir"/> (from the current pose).</summary>
        public static Quaternion HandRotation(Animator an, bool right, Vector3 palmN, Vector3 fingerDir)
        {
            var h = an.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            if (h == null || !Palm(an, right, out _, out var n, out var f)) return h != null ? h.rotation : Quaternion.identity;
            var wantF = Vector3.ProjectOnPlane(fingerDir, palmN).normalized; if (wantF.sqrMagnitude < 0.01f) return h.rotation;
            var from = Quaternion.LookRotation(f, n); var to = Quaternion.LookRotation(wantF, palmN);
            return to * Quaternion.Inverse(from) * h.rotation;
        }

        /// <summary>Close each finger joint by small steps until that finger touches the surface (never inside, at most a relaxed
        /// curl: 70 / 80 / 60 deg; the thumb 35 / 35 / 30). Each joint turns about the palm's across-axis, toward the palm.</summary>
        public static void CurlTo(Animator an, bool right, Surface s, float w)
        {
            if (w <= 0.001f || s == null || !Palm(an, right, out _, out var n, out var al)) return;
            var ch = Fingers(an, right);
            for (int f = 0; f < 5; f++)
            {
                var c = ch[f]; if (c[0] == null) continue;
                bool thumb = f == 4;
                var axis = thumb ? al : Vector3.Cross(n, al).normalized;
                float[] cap = thumb ? new[] { 35f, 35f, 30f } : new[] { 70f, 80f, 60f };
                for (int j = 0; j < 3; j++)
                {
                    var b = c[j]; if (b == null) continue;
                    // which way closes the finger (toward the palm side)
                    var tip0 = Tip(c); var test = b.position + Quaternion.AngleAxis(4f, axis) * (tip0 - b.position);
                    float sgn = Vector3.Dot(test - tip0, n) > 0f ? 1f : -1f;
                    float done = 0f, step = 4f;
                    while (done < cap[j] * w)
                    {
                        if (Touches(c, j, s)) break;
                        var before = b.rotation;
                        b.rotation = Quaternion.AngleAxis(sgn * step, axis) * b.rotation; done += step;
                        if (Inside(c, j, s)) { b.rotation = before; break; }
                    }
                }
                // a finger still inside (laid along the surface, or the thumb across it): open it from the base out until it is clear
                for (int j = 0; j < 3; j++)
                {
                    var b = c[j]; if (b == null || !Inside(c, j, s)) continue;
                    var tip0 = Tip(c); var test = b.position + Quaternion.AngleAxis(4f, axis) * (tip0 - b.position);
                    float open = Vector3.Dot(test - tip0, n) > 0f ? -1f : 1f;
                    for (float done = 0f; done < 60f && Inside(c, j, s); done += 3f) b.rotation = Quaternion.AngleAxis(open * 3f, axis) * b.rotation;
                    if (!Inside(c, j, s)) continue;
                    // still in: swing it off the surface about the other axis (a thumb lies across, not along)
                    var ax2 = thumb ? Vector3.Cross(n, al).normalized : n;
                    var t1 = Tip(c); float dPlus = s.Dist(b.position + Quaternion.AngleAxis(10f, ax2) * (t1 - b.position)), dMinus = s.Dist(b.position + Quaternion.AngleAxis(-10f, ax2) * (t1 - b.position));
                    float sw = dPlus > dMinus ? 1f : -1f;
                    for (float done = 0f; done < 50f && Inside(c, j, s); done += 3f) b.rotation = Quaternion.AngleAxis(sw * 3f, ax2) * b.rotation;
                }
            }
        }
        static bool Touches(Transform[] c, int from, Surface s) { for (int k = from + 1; k < 3; k++) if (c[k] != null && s.Dist(c[k].position) < Skin + 0.004f) return true; return s.Dist(Tip(c)) < Skin + 0.004f; }
        static bool Inside(Transform[] c, int from, Surface s) { for (int k = from + 1; k < 3; k++) if (c[k] != null && s.Dist(c[k].position) < InsideMax) return true; return s.Dist(Tip(c)) < InsideMax; }

        // ---------------------------------------------------------------- the whole hold
        /// <summary>One hand under / round the object at its centre: the palm on the surface, the hand turned, the fingers round it.</summary>
        public static void CupHand(Animator an, Transform root, bool right, Surface s, Vector3 palmDir, Vector3 fingerDir, float w)
        {
            if (w <= 0.001f || s == null) return;
            var h = an.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            if (h == null || !Palm(an, right, out var pc, out _, out _)) return;
            var c = s.Centre; palmDir = palmDir.normalized;
            var contact = c + palmDir * (s.Along(palmDir) + Skin);
            // the palm lies flat on the face it meets (a cube / octahedron face; on the sphere: the direction itself)
            var faceN = s.NormalAlong(palmDir); if (Vector3.Dot(faceN, palmDir) < 0.5f) faceN = palmDir;
            var q = HandRotation(an, right, -faceN, fingerDir);
            var offAfter = q * Quaternion.Inverse(h.rotation) * (pc - h.position);          // the palm skin relative to the wrist, turned
            float sd = right ? 1f : -1f;
            var sh = an.GetBoneTransform(right ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm);
            var pole = (sh != null ? sh.position : h.position) + root.right * sd * 0.35f - root.up * 0.55f - root.forward * 0.15f;
            ArmTo(an, right, contact - offAfter, pole, w);
            h.rotation = Quaternion.Slerp(h.rotation, q, w);
            CurlTo(an, right, s, w);
        }
        /// <summary>Both palms cupped under and round it (the sphere between her hands), or one palm under it (left / right).</summary>
        public static void Cup(Animator an, Transform root, Surface s, bool left, bool right, float w)
        {
            if (s == null || w <= 0.001f) return;
            var f = root.forward; var r = root.right; var up = root.up;
            if (left && right)
            {
                // a hand-sized thing is cupped from under its sides; a big one (over ~30 cm across: the cube) is held by its sides,
                // palms flat on them, fingers forward (cupping under a 42 cm box bent the wrists past 75 deg)
                bool big = s.Along(r) > 0.15f;
                if (big)
                {
                    CupHand(an, root, false, s, -r - up * 0.12f - f * 0.1f, f + up * 0.2f, w);
                    CupHand(an, root, true, s, r - up * 0.12f - f * 0.1f, f + up * 0.2f, w);
                }
                else
                {
                    CupHand(an, root, false, s, -r * 0.74f - up * 0.62f - f * 0.22f, f + up * 0.45f + r * 0.15f, w);
                    CupHand(an, root, true, s, r * 0.74f - up * 0.62f - f * 0.22f, f + up * 0.45f - r * 0.15f, w);
                }
            }
            else if (left) CupHand(an, root, false, s, -up - r * 0.18f - f * 0.1f, f + r * 0.3f, w);
            else if (right) CupHand(an, root, true, s, -up + r * 0.18f - f * 0.1f, f - r * 0.3f, w);
        }

        // ---------------------------------------------------------------- the proof
        public struct Report { public float Deepest, Wrist; public string Where; public int Points; public float Touching; }
        /// <summary>The deepest finger / palm point vs the surface (negative = inside) and the wrist bend vs the forearm.</summary>
        public static Report Measure(Animator an, bool right, Surface s)
        {
            var rep = new Report { Deepest = float.MaxValue, Where = "-" };
            if (an == null || s == null) return rep;
            var names = new[] { "index", "middle", "ring", "little", "thumb" };
            var ch = Fingers(an, right); int touch = 0;
            for (int f = 0; f < 5; f++)
            {
                for (int k = 0; k < 3; k++) { var b = ch[f][k]; if (b == null) continue; float d = s.Dist(b.position); rep.Points++; if (d < rep.Deepest) { rep.Deepest = d; rep.Where = $"{names[f]} joint {k + 1}"; } }
                if (ch[f][0] != null) { float d = s.Dist(Tip(ch[f])); rep.Points++; if (d < rep.Deepest) { rep.Deepest = d; rep.Where = $"{names[f]} tip"; } if (d < 0.012f) touch++; }
            }
            if (Palm(an, right, out var pc, out _, out _)) { float d = s.Dist(pc); if (d < rep.Deepest) { rep.Deepest = d; rep.Where = "palm"; } }
            rep.Touching = touch;
            var lo = an.GetBoneTransform(right ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm); var h = an.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand); var mid = an.GetBoneTransform(HB(right, "MiddleProximal"));
            if (lo != null && h != null && mid != null) rep.Wrist = Vector3.Angle(h.position - lo.position, mid.position - h.position);
            return rep;
        }

        // run-wide stats (check runs)
        public static int Frames; public static float WorstDeep = float.MaxValue, WorstWrist; public static string WorstWhere = "-";
        public static void Note(string what, Report r)
        {
            Frames++;
            if (r.Deepest < WorstDeep) { WorstDeep = r.Deepest; WorstWhere = $"{what}: {r.Where}"; }
            if (r.Wrist > WorstWrist) WorstWrist = r.Wrist;
        }
        public static string Summary() => Frames == 0 ? "HOLD (QF28 5): no holds this run" :
            $"HOLD (QF28 5): {Frames} held frames measured - deepest finger point {WorstDeep * 1000f:0.0} mm ({WorstWhere}; must be ≥ {InsideMax * 1000f:0} mm: never inside), largest wrist bend {WorstWrist:0} deg (natural ≤ {WristMax:0}) - {(WorstDeep >= InsideMax && WorstWrist <= WristMax ? "OK" : "FAIL")}";
    }
}
