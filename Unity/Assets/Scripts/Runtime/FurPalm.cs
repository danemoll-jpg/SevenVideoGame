using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// P2 (8): "often she is completely just petting in the air" (Dan). Every hand on Panther - the lap stroke in every chair /
    /// staging, FIN_10, Cattalus - rides ON his fur:
    ///  - <see cref="FurContact"/> = his body's real surface: the drawn meshes (the skinned body, or the curled lap mesh) baked to
    ///    world space once per frame (so it follows his breathing, the shrink, the lap settle); the nearest surface point + normal
    ///    to any point, and the top of his fur under a point (the stroke path runs over his back, not over his bounding box);
    ///  - <see cref="FurPalm"/> (on 7, after every other hand pass): for each hand a moment has put on him, the palm's skin
    ///    (HandHold.Palm) is measured against his surface; within reach the arm is re-solved two-bone (HandHold.ArmTo, the wrist
    ///    angle kept) so the palm sits on the fur (a few mm), twice per frame; if he is out of reach (the palm > 15 cm off him
    ///    after the solve - he moved, or the arm cannot get there) the hand lets go (the IK weight blends out) and the stroke
    ///    does not play;
    ///  - check runs log `PETTING (P2 8) <moment>` per moment (frames on him, palm-to-fur median / p95 / worst, frames let go) and
    ///    a summary line (every moment's p95 must stay under ~1 cm).
    /// Why it floated before: BackTop put the palm at the TOP of his bounding box over the box's centre line - over the curled lap
    /// mesh that line crosses the ring's dip (the box's top is the highest point of the curl, the centre is the hollow inside
    /// it), and the old measure compared the palm with that same target, so it read "1 cm" while the hand was in the air.
    /// </summary>
    [DefaultExecutionOrder(2000)]   // after the animator, SevenTableIK's palm pass, CatPoseOverlay (200), HoldPose (260)
    public class FurPalm : MonoBehaviour
    {
        public const float Skin = 0.004f, Reach = 0.15f;
        public class Hand { public Transform Cat; public string Moment; public float Weight; public bool LetGo; public float OnFor; public bool Hold; }
        public readonly Hand Left = new Hand(), Right = new Hand();
        Animator _an; SevenTableIK _ik;

        public static FurPalm On(GameObject seven) { var f = seven.GetComponent<FurPalm>(); if (f == null) f = seven.AddComponent<FurPalm>(); return f; }
        /// <summary>Put a hand on him for a moment (weight 0 = off). The moment's name groups the measures.</summary>
        /// <param name="hold">A held moment (FIN_10: her hands on him as he dies): never let go - the arm is pulled onto him from up to
        /// <see cref="HoldReach"/> away.</param>
        public void Set(bool right, Transform cat, string moment, float weight, bool hold = false)
        {
            var h = right ? Right : Left; h.Cat = cat; h.Moment = moment; h.Weight = Mathf.Clamp01(weight); h.Hold = hold;
        }
        public const float HoldReach = 0.6f;
        public void Off() { Left.Weight = Right.Weight = 0f; Left.Cat = Right.Cat = null; }
        public static int BadSolves;
        /// <summary>-nofurpalm: the whole fur-contact layer off (diagnosis: the pre-P2 hands and BackTop).</summary>
        public static bool Disabled;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ReadSwitch() { foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-nofurpalm") Disabled = true; }
        static bool Finite(Vector3 v) => !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
        /// <summary>True when the hand was let go this frame because he was out of reach.</summary>
        public bool OutOfReach(bool right) => (right ? Right : Left).LetGo;

        void Awake() { _an = GetComponent<Animator>(); if (_an == null) _an = GetComponentInChildren<Animator>(); _ik = _an != null ? _an.GetComponent<SevenTableIK>() : null; }

        void LateUpdate()
        {
            if (_an == null || Disabled) return;
            Solve(false, Left); Solve(true, Right);
        }

        void Solve(bool right, Hand h)
        {
            h.LetGo = false;
            if (h.Cat == null || !h.Cat.gameObject.activeInHierarchy || h.Weight < 0.05f) return;
            float ikW = _ik != null ? (right ? _ik.RightW : _ik.LeftW) : 1f;
            float w = h.Weight * Mathf.Clamp01(ikW * 1.2f);
            if (w < 0.05f) return;
            var elbowB = _an.GetBoneTransform(right ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm);
            var handB = _an.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            if (elbowB == null || handB == null) return;
            float gap = float.MaxValue;
            for (int pass = 0; pass < 2; pass++)
            {
                if (!HandHold.Palm(_an, right, out var palm, out var pn, out _)) return;
                if (!FurContact.Nearest(h.Cat, palm, out var surf, out var sn, out var signed)) return;
                gap = signed - Skin;
                if (Mathf.Abs(gap) < 0.002f) break;
                if (signed > (h.Hold ? HoldReach : Reach * 2f)) break;               // far off him: no solve (judged below)
                var want = surf + sn * Skin;
                var delta = (want - palm) * w;
                // never feed the skeleton a bad target (a NaN or a jump of metres would reach the skinning)
                if (!Finite(delta) || delta.sqrMagnitude > 1f) { BadSolves++; break; }
                HandHold.ArmTo(_an, right, handB.position + delta, elbowB.position + Vector3.down * 0.05f, 1f);
                if (!Finite(handB.position)) { BadSolves++; break; }
            }
            if (HandHold.Palm(_an, right, out var p2, out _, out _) && FurContact.Nearest(h.Cat, p2, out _, out _, out var s2)) gap = s2 - Skin;
            if (gap > Reach && !h.Hold)
            {
                // he is not within reach: the hand lets go (the moment's IK weight blends out) - never a stroke in the air
                h.LetGo = true;
                if (_ik != null) { if (right) _ik.RightW = Mathf.MoveTowards(_ik.RightW, 0f, Time.deltaTime * 4f); else _ik.LeftW = Mathf.MoveTowards(_ik.LeftW, 0f, Time.deltaTime * 4f); }
                FurContact.NoteLetGo(h.Moment);
                return;
            }
            if (w > 0.9f)
            {
                FurContact.Note(h.Moment, Mathf.Abs(gap));
                // check runs: one close-up of each petting moment, its palm on him (a second camera - the player's view is untouched)
                h.OnFor += Time.deltaTime;
                if (DemoBoot.CaptureDir != null && h.OnFor > 0.8f && ChapterCapture.I != null && FurContact.FirstCapture(h.Moment) && HandHold.Palm(_an, right, out var pc, out var pnn, out var pal))
                {
                    var across = Vector3.Cross(pal, Vector3.up); if (across.sqrMagnitude < 0.01f) across = transform.right; across.Normalize();
                    var cam = pc + across * 0.75f + Vector3.up * 0.45f - pal * 0.2f;
                    ChapterCapture.I.StartCoroutine(PetShot(h.Moment, cam, pc, h.Cat));
                }
            }
            else h.OnFor = 0f;
        }
        static System.Collections.IEnumerator PetShot(string moment, Vector3 cam, Vector3 at, Transform cat)
        {
            ImageGate.ExpectSide("pet_insert_", cat, "Panther under her palm (petting close-up)");
            var safe = System.Text.RegularExpressions.Regex.Replace(moment.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');
            yield return ChapterCapture.I.ShotFrom("pet_insert_" + safe, cam, at, 34f);
        }
    }

    /// <summary>P2 (8): Panther's real body surface (see <see cref="FurPalm"/>) + the PETTING measures.</summary>
    public static class FurContact
    {
        class Baked { public int Frame = -1; public readonly List<Vector3> P = new List<Vector3>(); public readonly List<Vector3> N = new List<Vector3>(); }
        static readonly Dictionary<Transform, Baked> _cache = new Dictionary<Transform, Baked>();
        static Mesh _tmp; static readonly List<Vector3> _v = new List<Vector3>(), _n = new List<Vector3>(); static readonly HashSet<string> _sizeLogged = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook() { UnityEngine.SceneManagement.SceneManager.sceneLoaded += (sc, md) => { _cache.Clear(); if (_tmp != null) Object.Destroy(_tmp); _tmp = null; }; }
        static Baked Get(Transform root)
        {
            if (root == null) return null;
            if (!_cache.TryGetValue(root, out var b)) { b = new Baked(); _cache[root] = b; }
            if (b.Frame == Time.frameCount) return b;
            b.Frame = Time.frameCount; b.P.Clear(); b.N.Clear();
            if (_tmp == null) _tmp = new Mesh();
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!smr.enabled || !smr.gameObject.activeInHierarchy || smr.sharedMesh == null || smr.sharedMesh.vertexCount < 200 || smr.bones == null || smr.bones.Length == 0) continue;   // the body, not whiskers
                // baked without its scale, then placed by the full matrix (FUR SURFACE proved it: the baked body matches the renderer's
                // bounds at every scale of FIN_10's shrink; bake-with-scale + position / rotation lost his 2.7x size)
                smr.BakeMesh(_tmp, false);
                _tmp.GetVertices(_v); _tmp.GetNormals(_n);
                var m = smr.transform.localToWorldMatrix;
                int first = b.P.Count;
                for (int i = 0; i < _v.Count; i++) { b.P.Add(m.MultiplyPoint3x4(_v[i])); b.N.Add(i < _n.Count ? m.MultiplyVector(_n[i]).normalized : Vector3.up); }
                // check runs: once per cat and scale, the baked body's size vs the renderer's own bounds (proof the surface is where he is drawn)
                if (DemoBoot.CaptureDir != null && _v.Count > 0)
                {
                    float sc = smr.transform.lossyScale.x; string key = root.name + "@" + sc.ToString("0.0");
                    if (_sizeLogged.Add(key))
                    {
                        var bb = new Bounds(b.P[first], Vector3.zero); for (int i = first; i < b.P.Count; i++) bb.Encapsulate(b.P[i]);
                        ChapterCapture.I?.Log($"FUR SURFACE (P2 8) {root.name} scale {sc:0.00}: baked body {bb.size.x:0.00} x {bb.size.y:0.00} x {bb.size.z:0.00} m centred {bb.center}, the renderer's bounds {smr.bounds.size.x:0.00} x {smr.bounds.size.y:0.00} x {smr.bounds.size.z:0.00} m centred {smr.bounds.center}");
                    }
                }
            }
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())                  // the static curled lap cat
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || !mr.enabled || !mf.gameObject.activeInHierarchy || mf.sharedMesh == null || !mf.sharedMesh.isReadable || mf.sharedMesh.vertexCount < 200) continue;
                mf.sharedMesh.GetVertices(_v); mf.sharedMesh.GetNormals(_n);
                var m = mf.transform.localToWorldMatrix;
                for (int i = 0; i < _v.Count; i++) { b.P.Add(m.MultiplyPoint3x4(_v[i])); b.N.Add(i < _n.Count ? m.MultiplyVector(_n[i]).normalized : Vector3.up); }
            }
            return b;
        }

        /// <summary>The nearest point of his surface to <paramref name="p"/>: the nearest vertex, the distance taken along its
        /// normal (signed: + outside him). False when nothing of him is drawn.</summary>
        public static bool Nearest(Transform root, Vector3 p, out Vector3 surf, out Vector3 normal, out float signedDist)
        {
            surf = p; normal = Vector3.up; signedDist = float.MaxValue;
            if (FurPalm.Disabled) return false;
            var b = Get(root); if (b == null || b.P.Count == 0) return false;
            int best = -1; float bd = float.MaxValue;
            for (int i = 0; i < b.P.Count; i++) { float d = (b.P[i] - p).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
            // average the normal over the vertices within 2 cm (a smoother surface than one vertex)
            var c = b.P[best]; var nn = Vector3.zero; var cc = Vector3.zero; int k = 0;
            for (int i = 0; i < b.P.Count; i++) if ((b.P[i] - c).sqrMagnitude < 0.0004f) { nn += b.N[i]; cc += b.P[i]; k++; }
            normal = nn.sqrMagnitude > 1e-6f ? nn.normalized : b.N[best];
            if (k > 0) cc /= k; else cc = c;
            signedDist = Vector3.Dot(p - cc, normal);
            if (signedDist > Mathf.Sqrt(bd) * 1.5f) signedDist = Mathf.Sqrt(bd);          // off to the side of him: the straight distance
            surf = p - normal * signedDist;
            return true;
        }

        /// <summary>The top of his fur under <paramref name="p"/> (the highest surface within <paramref name="radius"/> in the
        /// horizontal plane - the nearest column of him if none is that close). False when nothing of him is drawn.</summary>
        public static bool TopAt(Transform root, Vector3 p, float radius, out Vector3 top, out Vector3 normal)
        {
            top = p; normal = Vector3.up;
            if (FurPalm.Disabled) return false;
            var b = Get(root); if (b == null || b.P.Count == 0) return false;
            float nearXZ = float.MaxValue;
            for (int i = 0; i < b.P.Count; i++) { var d = b.P[i] - p; d.y = 0f; nearXZ = Mathf.Min(nearXZ, d.sqrMagnitude); }
            float r = Mathf.Max(radius, Mathf.Sqrt(nearXZ) + 0.01f), r2 = r * r;
            int best = -1; float hy = float.MinValue;
            for (int i = 0; i < b.P.Count; i++) { var d = b.P[i] - p; d.y = 0f; if (d.sqrMagnitude <= r2 && b.P[i].y > hy && b.N[i].y > 0.2f) { hy = b.P[i].y; best = i; } }
            if (best < 0) return false;
            top = b.P[best]; var nn = Vector3.zero;
            for (int i = 0; i < b.P.Count; i++) if ((b.P[i] - top).sqrMagnitude < 0.0004f) nn += b.N[i];
            normal = nn.sqrMagnitude > 1e-6f ? nn.normalized : b.N[best];
            if (normal.y < 0.2f) normal = (normal + Vector3.up).normalized;
            return true;
        }

        // ------------------------------------------------------------------ the measures (check runs)
        class Stat { public readonly List<float> G = new List<float>(); public int LetGo; }
        static readonly Dictionary<string, Stat> _stats = new Dictionary<string, Stat>();
        static Stat S(string m) { m = string.IsNullOrEmpty(m) ? "?" : m; if (!_stats.TryGetValue(m, out var s)) { s = new Stat(); _stats[m] = s; } return s; }
        public static void Note(string moment, float gap) { if (DemoBoot.CaptureDir != null) S(moment).G.Add(gap); }
        public static void NoteLetGo(string moment) { if (DemoBoot.CaptureDir != null) S(moment).LetGo++; }
        public static bool Any => _stats.Count > 0;
        static readonly HashSet<string> _captured = new HashSet<string>();
        public static bool FirstCapture(string moment) => _captured.Add(moment ?? "?");
        public static string Summary()
        {
            if (_stats.Count == 0) return "PETTING (P2 8): no petting moments this run";
            var sb = new System.Text.StringBuilder(); int bad = 0; float worstP95 = 0f;
            foreach (var kv in _stats)
            {
                var g = new List<float>(kv.Value.G); g.Sort();
                float med = g.Count > 0 ? g[g.Count / 2] : 0f, p95 = g.Count > 0 ? g[Mathf.Min(g.Count - 1, (int)(g.Count * 0.95f))] : 0f, max = g.Count > 0 ? g[g.Count - 1] : 0f;
                if (p95 > 0.01f) bad++; worstP95 = Mathf.Max(worstP95, p95);
                sb.Append($"PETTING (P2 8) {kv.Key}: {g.Count} frames with the palm on him - palm-to-fur median {med * 100f:0.0} cm, p95 {p95 * 100f:0.0} cm, worst {max * 100f:0.0} cm; let go (out of reach) {kv.Value.LetGo} frames\n");
            }
            sb.Append($"PETTING (P2 8): {_stats.Count} moments, worst p95 {worstP95 * 100f:0.0} cm, moments over 1 cm {bad} (must be 0); solves refused (a NaN or a jump > 1 m) {FurPalm.BadSolves}");
            return sb.ToString();
        }
    }
}
