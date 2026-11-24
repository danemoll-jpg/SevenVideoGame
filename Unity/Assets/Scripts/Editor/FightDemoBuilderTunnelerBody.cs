using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Seven.EditorTools
{
    /// <summary>
    /// P2 (9): Tunneler's body gets bones (see <see cref="Seven.TunnelerBody"/>). His model is one Tripo mesh with no rig (the drill head
    /// split off as "DrillHead"), shaped like a stack of rings with two segmented arms hanging free of the belly. Built here, on every
    /// instance (Europa's studio, the finale's rig):
    ///  - bones in the model's own frame: Root (feet) - Hips - Spine - Chest - Neck (where the drill sits), and per arm Shoulder ->
    ///    Elbow -> Claw, found from the mesh: each arm is flood-filled off the body from its outermost vertex (welded by position,
    ///    never above the shoulder line), its chain = the arm vertices' centres at the top, middle and bottom of the arm;
    ///  - weights: body vertices blend by height between the spine bones (the rings bend as one), arm vertices along their chain
    ///    (the shoulder blends into the chest, the elbow between upper arm and forearm, the claw on the hand);
    ///  - the body's MeshRenderer becomes a SkinnedMeshRenderer on that mesh, the drill head rides the neck bone (its axis is baked
    ///    after, in the neck's frame - DrillHead's own check runs on it unchanged).
    /// Logs `TUNNELER RIG (P2 9) <scene>`: vertices per bone, arm sizes, the chain.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        static Seven.TunnelerBody RigTunnelerBody(GameObject model, string scene)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(model)) PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(model), PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);   // components are swapped below
            var root = model.transform;
            var drill = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "DrillHead");
            var body = root.GetComponentsInChildren<MeshFilter>(true).Where(mf => mf.sharedMesh != null && (drill == null || !mf.transform.IsChildOf(drill)))
                           .OrderByDescending(mf => mf.sharedMesh.vertexCount).FirstOrDefault();
            if (body == null) { Log($"TUNNELER RIG (P2 9) {scene}: no body mesh - left unrigged"); return null; }
            var src = body.sharedMesh; var mr = body.GetComponent<MeshRenderer>(); var mat = mr != null ? mr.sharedMaterials : null;
            var toRoot = root.worldToLocalMatrix * body.transform.localToWorldMatrix;
            var verts = src.vertices; int n = verts.Length;
            var P = new Vector3[n]; for (int i = 0; i < n; i++) P[i] = toRoot.MultiplyPoint3x4(verts[i]);
            float minY = P.Min(p => p.y), maxY = P.Max(p => p.y), minX = P.Min(p => p.x), maxX = P.Max(p => p.x);
            float cx = (minX + maxX) * 0.5f, cz = P.Average(p => p.z), W = (maxX - minX) * 0.5f;
            float top = maxY;
            if (drill != null) { var dmf = drill.GetComponentInChildren<MeshFilter>(); if (dmf != null && dmf.sharedMesh != null) { var dm = root.worldToLocalMatrix * dmf.transform.localToWorldMatrix; top = dmf.sharedMesh.vertices.Min(v => dm.MultiplyPoint3x4(v).y); } }
            float H = top - minY;                                     // feet to the collar (where the drill starts)
            float shY = minY + H * 0.86f;                              // the shoulder line (arms never fill above it)
            // ---- connectivity, welded by position (Tripo splits vertices on UV seams)
            var key = new Dictionary<Vector3Int, int>(); var weld = new int[n];
            for (int i = 0; i < n; i++) { var k = Vector3Int.RoundToInt(P[i] * 2000f); if (!key.TryGetValue(k, out var w)) { w = key.Count; key[k] = w; } weld[i] = w; }
            var adj = new List<int>[key.Count]; for (int i = 0; i < adj.Length; i++) adj[i] = new List<int>();
            var tris = src.triangles;
            for (int t = 0; t < tris.Length; t += 3) for (int e = 0; e < 3; e++) { int a = weld[tris[t + e]], b = weld[tris[t + (e + 1) % 3]]; if (a != b) { adj[a].Add(b); adj[b].Add(a); } }
            var wpos = new Vector3[key.Count]; for (int i = 0; i < n; i++) wpos[weld[i]] = P[i];
            // ---- each arm: flood-fill from the outermost vertex on that side, below the shoulder line and outside the body's core
            var armOf = new int[key.Count];                            // 0 body, -1 left arm, +1 right arm
            var chain = new Dictionary<int, Vector3[]>();
            foreach (int side in new[] { -1, 1 })
            {
                int seed = -1; float best = float.MinValue;
                for (int i = 0; i < wpos.Length; i++) { float x = (wpos[i].x - cx) * side; if (wpos[i].y < shY - 0.1f * H && x > best) { best = x; seed = i; } }
                if (seed < 0) continue;
                // the armpit: the highest cut where the arm's fill (from the claw, below the cut) never reaches the body's middle
                // (|x| < 0.25 W) or the floor - below it the arm hangs free of the belly; the shoulder above it blends into the chest
                HashSet<int> Fill(float cut)
                {
                    var set = new HashSet<int> { seed }; var q = new Queue<int>(); q.Enqueue(seed);
                    while (q.Count > 0) { int a = q.Dequeue(); foreach (var b in adj[a]) if (!set.Contains(b) && wpos[b].y < cut) { set.Add(b); q.Enqueue(b); if (Mathf.Abs(wpos[b].x - cx) < W * 0.25f || wpos[b].y < minY + H * 0.08f || set.Count > wpos.Length / 4) return null; } }
                    return set;
                }
                HashSet<int> fill = null; float pit = shY;
                for (pit = shY; pit > minY + H * 0.35f; pit -= H * 0.02f) { fill = Fill(pit); if (fill != null) break; }
                if (fill == null)
                {
                    Log($"TUNNELER RIG (P2 9) {scene}: the {(side < 0 ? "left" : "right")} arm never separates from the body - geometric arm used");
                    fill = new HashSet<int>(); for (int i = 0; i < wpos.Length; i++) if (wpos[i].y < shY && (wpos[i].x - cx) * side > W * 0.66f) fill.Add(i);
                    pit = shY;
                }
                foreach (var i in fill) armOf[i] = side;
                Log($"TUNNELER RIG (P2 9) {scene}: {(side < 0 ? "left" : "right")} arm {fill.Count} vertices below the armpit at {pit - minY:0.00} m");
                var am = Enumerable.Range(0, wpos.Length).Where(i => armOf[i] == side).Select(i => wpos[i]).ToList();
                if (am.Count < 50) continue;
                float aTop = am.Max(p => p.y), aBot = am.Min(p => p.y), aH = aTop - aBot;
                Vector3 Band(float lo, float hi) { var s = am.Where(p => p.y >= aBot + aH * lo && p.y <= aBot + aH * hi).ToList(); return s.Count > 0 ? new Vector3(s.Average(p => p.x), s.Average(p => p.y), s.Average(p => p.z)) : Vector3.zero; }
                // the upper arm joins the belly (no gap above the elbow): its joints from his proportions (shoulder 0.885, elbow 0.625 of
                // the body's height), centred on the outer arm's vertices at those heights
                Vector3 Outer(float yFrac, float xMin, float fallbackX)
                {
                    float y0 = minY + H * (yFrac - 0.025f), y1 = minY + H * (yFrac + 0.025f);
                    var s = wpos.Where(p => p.y >= y0 && p.y <= y1 && (p.x - cx) * side > W * xMin).ToList();
                    var c = s.Count > 10 ? new Vector3(s.Average(p => p.x), s.Average(p => p.y), s.Average(p => p.z)) : new Vector3(cx + side * W * fallbackX, minY + H * yFrac, cz);
                    c.y = minY + H * yFrac; return c;
                }
                var shoulder = Outer(0.885f, 0.5f, 0.62f); shoulder.x -= side * W * 0.06f;
                var elbow = Outer(0.625f, 0.68f, 0.82f);
                var hand = Band(0.2f, 0.32f); var tip = Band(0f, 0.08f);
                if (hand.y > elbow.y - 0.1f) hand = elbow + Vector3.down * H * 0.2f;
                chain[side] = new[] { shoulder, elbow, hand, tip };
            }
            // ---- the bones (children of the model root, identity rotations, in the root's frame)
            Transform Bone(string name, Transform parent, Vector3 localInRoot) { var t = new GameObject(name).transform; t.SetParent(parent, false); t.position = root.TransformPoint(localInRoot); t.rotation = root.rotation; return t; }
            float yRoot = minY + H * 0.2f, yHips = minY + H * 0.36f, ySpine = minY + H * 0.52f, yChest = minY + H * 0.68f, yNeck = top;
            var bRoot = Bone("TunRoot", root, new Vector3(cx, yRoot, cz));
            var bHips = Bone("TunHips", bRoot, new Vector3(cx, yHips, cz));
            var bSpine = Bone("TunSpine", bHips, new Vector3(cx, ySpine, cz));
            var bChest = Bone("TunChest", bSpine, new Vector3(cx, yChest, cz));
            var bNeck = Bone("TunNeck", bChest, new Vector3(cx, yNeck, cz));
            var arm = new Dictionary<int, Transform[]>();
            foreach (var kv in chain)
            {
                string s = kv.Key < 0 ? "L" : "R"; var c = kv.Value;
                var u = Bone("TunUpperArm" + s, bChest, c[0]); var f = Bone("TunForeArm" + s, u, c[1]); var h = Bone("TunHand" + s, f, c[2]);
                arm[kv.Key] = new[] { u, f, h };
            }
            var bones = new List<Transform> { bRoot, bHips, bSpine, bChest, bNeck };
            foreach (var kv in arm) bones.AddRange(kv.Value);
            int iRoot = 0, iHips = 1, iSpine = 2, iChest = 3, iNeck = 4;
            int IA(int side, int j) => 5 + (arm.ContainsKey(-1) ? (side < 0 ? 0 : 3) : 0) + j;
            // ---- the skinned mesh (the same vertices in the body object's own space)
            var m = Object.Instantiate(src); m = SaveMesh(m, "Tunneler_Skinned_" + scene);
            var mv = m.vertices; int mn = mv.Length; var bw = new BoneWeight[mn];
            var heights = new[] { (yRoot, iRoot), (yHips, iHips), (ySpine, iSpine), (yChest, iChest), (yNeck, iNeck) };
            var counts = new int[bones.Count];
            for (int i = 0; i < mn; i++)
            {
                var p = toRoot.MultiplyPoint3x4(mv[i]);
                // which part: the nearest welded vertex's flag (the saved mesh may have split vertices)
                var k = Vector3Int.RoundToInt(p * 2000f); int side = key.TryGetValue(k, out var wi) ? armOf[wi] : 0;
                float armness = side != 0 ? 1f : 0f;
                if (side == 0)
                    foreach (var kv in arm)
                    {
                        var c = chain[kv.Key]; if ((p.x - cx) * kv.Key < W * 0.5f || p.y > c[0].y + H * 0.05f) continue;
                        float du = Seg(p, c[0], c[1], out _); float a1 = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.14f, 0.26f, du));
                        if (a1 > armness) { armness = a1; side = kv.Key; }
                    }
                var w = new List<(int b, float w)>();
                if (side != 0 && arm.ContainsKey(side) && armness > 0f)
                {
                    var c = chain[side]; float dU = Seg(p, c[0], c[1], out float tU), dF = Seg(p, c[1], c[2], out float tF), dH = Seg(p, c[2], c[3], out _);
                    if (dH < dF && dH < dU) w.Add((IA(side, 2), 1f));
                    else if (dF < dU) { float e = Mathf.Clamp01((tF - 0f) / 0.2f); w.Add((IA(side, 1), 0.5f + 0.5f * e)); if (e < 1f) w.Add((IA(side, 0), 0.5f - 0.5f * e)); if (tF > 0.85f) { float hh = (tF - 0.85f) / 0.15f * 0.4f; w.Add((IA(side, 2), hh)); } }
                    else { float e = Mathf.Clamp01(tU / 0.25f); w.Add((IA(side, 0), 0.4f + 0.6f * e)); if (e < 1f) w.Add((iChest, 0.6f * (1f - e))); if (tU > 0.85f) w.Add((IA(side, 1), (tU - 0.85f) / 0.15f * 0.5f)); }
                }
                if (armness < 1f)
                {
                    for (int q = 0; q < w.Count; q++) w[q] = (w[q].b, w[q].w * armness);
                    var bodyW = new List<(int b, float w)>();
                    if (p.y <= heights[0].Item1) bodyW.Add((iRoot, 1f));
                    else if (p.y >= yNeck) bodyW.Add((iNeck, 1f));
                    else for (int j = 0; j < heights.Length - 1; j++)
                        if (p.y >= heights[j].Item1 && p.y < heights[j + 1].Item1) { float u = Mathf.SmoothStep(0f, 1f, (p.y - heights[j].Item1) / (heights[j + 1].Item1 - heights[j].Item1)); bodyW.Add((heights[j].Item2, 1f - u)); bodyW.Add((heights[j + 1].Item2, u)); break; }
                    foreach (var bb in bodyW) w.Add((bb.b, bb.w * (1f - armness)));
                }
                bw[i] = Pack(w); counts[bw[i].boneIndex0]++;
            }
            var bind = new Matrix4x4[bones.Count];
            for (int b = 0; b < bones.Count; b++) bind[b] = bones[b].worldToLocalMatrix * body.transform.localToWorldMatrix;
            m.boneWeights = bw; m.bindposes = bind; m.RecalculateBounds(); EditorUtility.SetDirty(m);
            // ---- swap the renderer
            var go = body.gameObject;
            Object.DestroyImmediate(body); if (mr != null) Object.DestroyImmediate(mr);
            var smr = go.AddComponent<SkinnedMeshRenderer>(); smr.sharedMesh = m; smr.bones = bones.ToArray(); smr.rootBone = bRoot; if (mat != null) smr.sharedMaterials = mat;
            smr.updateWhenOffscreen = true; smr.quality = SkinQuality.Bone4;
            if (drill != null) drill.SetParent(bNeck, true);
            var tb = model.AddComponent<Seven.TunnelerBody>();
            tb.Root = bRoot; tb.Hips = bHips; tb.Spine = bSpine; tb.Chest = bChest; tb.Neck = bNeck;
            if (arm.TryGetValue(-1, out var al)) { tb.UpperL = al[0]; tb.ForeL = al[1]; tb.HandL = al[2]; }
            if (arm.TryGetValue(1, out var ar)) { tb.UpperR = ar[0]; tb.ForeR = ar[1]; tb.HandR = ar[2]; }
            string Ch(int sd) => chain.TryGetValue(sd, out var c) ? string.Join(" -> ", c.Select(v => $"({v.x:0.00},{v.y:0.00},{v.z:0.00})")) : "none";
            Log($"TUNNELER RIG (P2 9) {scene}: {mn} vertices skinned to {bones.Count} bones (main weight per bone: {string.Join(", ", bones.Select((b, i) => b.name + " " + counts[i]))}); height {H:0.00} m; left arm {Ch(-1)}; right arm {Ch(1)}; the drill head on the neck bone {(drill != null ? "yes" : "no drill")}");
            return tb;
        }

        static float Seg(Vector3 p, Vector3 a, Vector3 b, out float t)
        {
            var ab = b - a; t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); return (p - (a + ab * t)).magnitude;
        }
        static BoneWeight Pack(List<(int b, float w)> w)
        {
            var merged = w.GroupBy(x => x.b).Select(g => (b: g.Key, w: g.Sum(x => x.w))).Where(x => x.w > 1e-4f).OrderByDescending(x => x.w).Take(4).ToList();
            if (merged.Count == 0) merged.Add((0, 1f));
            float sum = merged.Sum(x => x.w); var r = new BoneWeight();
            for (int i = 0; i < merged.Count; i++)
            {
                float v = merged[i].w / sum;
                switch (i) { case 0: r.boneIndex0 = merged[i].b; r.weight0 = v; break; case 1: r.boneIndex1 = merged[i].b; r.weight1 = v; break; case 2: r.boneIndex2 = merged[i].b; r.weight2 = v; break; default: r.boneIndex3 = merged[i].b; r.weight3 = v; break; }
            }
            return r;
        }
    }
}
