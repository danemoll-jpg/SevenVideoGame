using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF18 build checks, run on every generated scene before it is saved:
    ///  - COPLANAR CHECK (item 11b): every opaque mesh, any shape (QF9's ZFightFix only saw Unity cubes in Chapter 1) -
    ///    triangles of different renderers / materials in one plane, facing the same way, overlapping = a shimmer.
    ///  - HOLES CHECK (item 13): a dense grid of rays straight down over every walkable / ridable zone; a ray that finds no
    ///    visible surface at the zone's height (it sees through to the fog or the ground far below) is a hole. Before QF18 the
    ///    Mars ground blocks were battered masonry, so their tops pulled apart into open grooves (the checkpoint hole, the strip,
    ///    the ramp tops) while the zones - the actual walking surface - carried on across them.
    /// Both write Docs/Previews/qf18/&lt;check&gt;_&lt;scene&gt;.txt and a build log line; a failing scene is listed in BuildFailures
    /// and the command-line build exits non-zero.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        public static readonly List<string> BuildFailures = new List<string>();
        static string QF18Dir { get { var d = Path.Combine(RepoRoot, "Docs", "Previews", "qf18"); Directory.CreateDirectory(d); return d; } }

        static List<Renderer> SceneRenderers()
        {
            var l = new List<Renderer>();
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) l.AddRange(go.GetComponentsInChildren<MeshRenderer>(false));
            // the one saucer prefab is checked as a prefab (CheckSaucer: every scene's instance must stay unchanged) - never nudged here
            l.RemoveAll(r => r.GetComponentInParent<Seven.SaucerHold>() != null || r.GetComponentInParent<Seven.SaucerBeam>() != null);
            // QF23 (10): the cutaway set is the Saucer scene's interior (checked there) - not part of this level
            l.RemoveAll(r => r.GetComponentInParent<Seven.AboardCut>(true) != null);
            return l;
        }

        static int CoplanarCheck(string scene, bool fail)
        {
            var pairs = new List<string>();
            // QF18 (11): fix what is found - the smaller of each fighting pair moves 4 mm along the shared face's normal (its face
            // comes in front; nothing visible moves), then the scene is checked again, up to four passes
            int first = -1, moved = 0, n = 0;
            for (int pass = 0; pass < 5; pass++)
            {
                var fix = new List<(Renderer x, Renderer y, Vector3 n)>(); pairs.Clear();
                n = Seven.ZFightProbe.Count(SceneRenderers(), null, pairs, 400, fix);
                if (first < 0) first = n;
                if (n == 0 || pass == 4) break;
                var done = new HashSet<Renderer>(); var undo = new List<(Transform t, Vector3 p)>();
                foreach (var (x, y, nn) in fix)
                {
                    var small = x.bounds.size.sqrMagnitude <= y.bounds.size.sqrMagnitude ? x : y;
                    if (done.Contains(small) || small.GetComponent<Collider>() is MeshCollider) continue;
                    done.Add(small); undo.Add((small.transform, small.transform.position)); small.transform.position += nn.normalized * 0.004f; moved++;
                }
                // a pass that does not help is tried the other way (the face goes behind instead), else taken back
                int after = Seven.ZFightProbe.Count(SceneRenderers(), null, null, 0);
                if (after >= n)
                {
                    foreach (var (tt, pp) in undo) tt.position = pp - (tt.position - pp);
                    after = Seven.ZFightProbe.Count(SceneRenderers(), null, null, 0);
                    if (after >= n) { foreach (var (tt, pp) in undo) tt.position = pp; moved -= undo.Count; n = Seven.ZFightProbe.Count(SceneRenderers(), null, pairs, 400); break; }
                }
            }
            Log($"COPLANAR FIX (QF18 11) {scene}: {first} fighting faces at first, {moved} objects nudged 4 mm, {n} left");
            var sb = new StringBuilder($"COPLANAR CHECK (QF18 11) {scene}: {n} coplanar, same-facing, overlapping faces of different materials (must be 0)\n");
            foreach (var p in pairs) sb.AppendLine("  " + p);
            File.WriteAllText(Path.Combine(QF18Dir, $"coplanar_{scene}.txt"), sb.ToString());
            Log($"COPLANAR CHECK (QF18 11) {scene}: {n} fighting faces ({pairs.Count} renderer pairs){(pairs.Count > 0 ? " - first: " + string.Join("; ", pairs.Take(8)) : "")}");
            if (n > 0 && fail) BuildFailures.Add($"coplanar {scene}: {n}");
            return n;
        }

        /// <summary>The holes check over a zone-shaped scene. prepare / restore: put moving pieces in their open state.</summary>
        static int HolesCheck(string scene, Seven.WorldGround wg, System.Func<Seven.WorldGround.ZoneDef, bool> include = null, System.Action prepare = null, System.Action restore = null, bool fail = true, float step = 1.0f, IList<Transform> people = null)
        {
            if (wg == null || wg.Zones == null || wg.Zones.Length == 0) { Log($"HOLES CHECK (QF18 13) {scene}: no zones"); return 0; }
            prepare?.Invoke();
            // temporary colliders on every visible opaque mesh (removed again)
            var temp = new List<Collider>();
            foreach (var r in SceneRenderers())
            {
                if (r.sharedMaterial == null || r.sharedMaterial.renderQueue > 2500) continue;
                var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                if (r.GetComponent<Collider>() != null) continue;
                var mc = r.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; temp.Add(mc);
            }
            Physics.SyncTransforms();
            bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            int rays = 0, holes = 0; var sb = new StringBuilder(); var perZone = new Dictionary<string, int>();
            foreach (var z in wg.Zones)
            {
                if (!(include?.Invoke(z) ?? true)) continue;
                float x0 = Mathf.Min(z.Min.x, z.Max.x) + 0.3f, x1 = Mathf.Max(z.Min.x, z.Max.x) - 0.3f, z0 = Mathf.Min(z.Min.y, z.Max.y) + 0.3f, z1 = Mathf.Max(z.Min.y, z.Max.y) - 0.3f;
                if (x1 < x0 || z1 < z0) continue;
                // very large zones (the open plain) are sampled more coarsely
                float area = (x1 - x0) * (z1 - z0); float st = area > 20000f ? step * 4f : area > 5000f ? step * 2f : step;
                for (float x = x0; x <= x1 + 1e-3f; x += st)
                    for (float zz = z0; zz <= z1 + 1e-3f; zz += st)
                    {
                        float y = z.Axis == 0 ? z.Y0 : z.Axis == 1 ? Mathf.Lerp(z.Y0, z.Y1, Mathf.InverseLerp(z.Min.y, z.Max.y, zz)) : Mathf.Lerp(z.Y0, z.Y1, Mathf.InverseLerp(z.Min.x, z.Max.x, x));
                        if (z.Axis == 0 && z.Y0 != z.Y1) y = Mathf.Lerp(z.Y0, z.Y1, Mathf.InverseLerp(z.Min.x, z.Max.x, x));
                        rays++;
                        var o = new Vector3(x, y + 1.2f, zz);
                        bool ok = Physics.Raycast(o, Vector3.down, out var hit, 1.9f, ~0, QueryTriggerInteraction.Ignore) && hit.point.y > y - 0.6f;
                        if (ok) continue;
                        holes++; perZone[z.Name] = (perZone.TryGetValue(z.Name, out int c) ? c : 0) + 1;
                        if (holes <= 300) sb.AppendLine($"  hole in zone {z.Name} at ({x:0.0}, {y:0.00}, {zz:0.0}) - {(Physics.Raycast(o, Vector3.down, out var h2, 400f) ? $"first surface {y - h2.point.y:0.0} m below ('{h2.collider.name}')" : "nothing below (open to the fog)")}");
                    }
            }
            // QF18 (6): nobody stands over a hole or inside geometry - each person's feet on a surface, the body clear
            int npcBad = 0; var nb = new StringBuilder();
            if (people != null)
                foreach (var t in people)
                {
                    if (t == null || !t.gameObject.activeInHierarchy) continue;
                    var feet = t.position; var own = new HashSet<Collider>(t.GetComponentsInChildren<Collider>(true));
                    bool floor = Physics.Raycast(feet + Vector3.up * 1.0f, Vector3.down, out var fh, 1.5f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(fh.point.y - feet.y) < 0.35f;
                    var cols = Physics.OverlapSphere(feet + Vector3.up * 1.0f, 0.22f, ~0, QueryTriggerInteraction.Ignore).Where(c => !own.Contains(c)).ToArray();
                    if (floor && cols.Length == 0) continue;
                    npcBad++; nb.AppendLine($"  {t.name} at {feet}: {(floor ? "" : "NO FLOOR under the feet ")}{(cols.Length > 0 ? "inside '" + cols[0].name + "'" : "")}");
                }
            Physics.queriesHitBackfaces = bf;
            foreach (var c in temp) Object.DestroyImmediate(c);
            if (people != null)
            {
                var nh = $"NPC PLACEMENT (QF18 6) {scene}: {people.Count} people, {npcBad} over a hole or inside geometry (must be 0)";
                File.WriteAllText(Path.Combine(QF18Dir, $"npc_placement_{scene}.txt"), nh + System.Environment.NewLine + nb);
                Log(nh + (npcBad > 0 ? " - " + nb.ToString().Replace(System.Environment.NewLine, " ") : ""));
                if (npcBad > 0 && fail) BuildFailures.Add($"npc placement {scene}: {npcBad}");
            }
            restore?.Invoke();
            var head = $"HOLES CHECK (QF18 13) {scene}: {rays} rays over the walkable / ridable zones, {holes} holes (must be 0){(perZone.Count > 0 ? " - per zone: " + string.Join(", ", perZone.Select(kv => kv.Key + " " + kv.Value)) : "")}";
            File.WriteAllText(Path.Combine(QF18Dir, $"holes_{scene}.txt"), head + "\n" + sb);
            Log(head);
            if (holes > 0 && fail) BuildFailures.Add($"holes {scene}: {holes}");
            return holes;
        }
    
        /// <summary>Every scene: the coplanar check, the holes check over its zones, the NPC placement scan.</summary>
        static void QF18SceneChecks(string scene, bool failHoles, System.Action prepare = null, System.Action restore = null)
        {
            CoplanarCheck(scene, failHoles);
            var wg = Object.FindAnyObjectByType<Seven.WorldGround>();
            var people = Object.FindObjectsByType<Animator>().Where(a => a.gameObject.activeInHierarchy && a.isHuman && a.name != "7" && a.GetComponentInParent<Seven.ChapterSeven>() == null).Select(a => a.transform).ToList();
            if (wg != null && wg.Shape == Seven.Ground.Shape.Zones) HolesCheck(scene, wg, z => z.Name != "Valley", prepare, restore, failHoles, 1f, people);
            else Log($"HOLES CHECK (QF18 13) {scene}: not a zone level - skipped");
            KickStatesCheck(scene);
            QF23SceneChecks(scene);
        }

        /// <summary>QF20 (2): 7's animator in every chapter scene carries the kick states (Y = kick in boss fights).</summary>
        static void KickStatesCheck(string scene)
        {
            var seven = Object.FindObjectsByType<Seven.ChapterSeven>(FindObjectsInactive.Include).Select(c => c.GetComponentInChildren<Animator>(true)).FirstOrDefault(a => a != null);
            if (seven == null) { Log($"KICK STATES CHECK (QF20 2) {scene}: no ChapterSeven - skipped"); return; }
            var ac = seven.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            var names = ac != null ? ac.layers[0].stateMachine.states.Select(s => s.state.name).ToList() : new List<string>();
            var missing = new[] { "jab", "cross", "hook", "mma_kick", "roundhouse", "finisher" }.Where(n => !names.Contains(n)).ToList();
            var line = $"KICK STATES CHECK (QF20 2) {scene}: 7's animator '{(ac != null ? ac.name : "none")}' - " + (missing.Count == 0 ? "punches + mma_kick + roundhouse + finisher present" : "MISSING " + string.Join(", ", missing));
            Log(line);
            if (missing.Count > 0) BuildFailures.Add(line);
        }
    }
}
