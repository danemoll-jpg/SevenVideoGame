using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// QF8: the level's visual-only meshes (sculpted rock, ribs, door frames, ruin columns, chunks - built without
    /// colliders so they never change how 7 walks) as mesh colliders in a separate, never-simulated physics scene.
    /// The follow camera, the cutscene framings and 7's wall clearance query it next to the normal colliders, so a
    /// wall that is only a mesh blocks the camera exactly like a wall with a collider (Dan's 0.10.4 shot at the first
    /// "Climb down": the camera sat behind a mesh-only rock face). Gameplay physics never sees these colliders.
    /// Two kinds: <see cref="Solid"/> (walls, rock, ribs, frames: the camera pulls in / swings round) and
    /// <see cref="FadeOnly"/> (small props and the pipe run: they fade out of the way instead).
    /// Moving sources (door leaves, the lock rubble, the telekinesis slab) are re-synced every frame they move;
    /// hidden ones are switched off.
    /// </summary>
    public static class CameraOccluders
    {
        public const int Solid = 8, FadeOnly = 9, Probe = 10;
        public const int SolidMask = 1 << Solid, AllMask = (1 << Solid) | (1 << FadeOnly);
        /// <summary>Max size (m) of a prop that fades instead of blocking.</summary>
        public const float FadeSize = 2.6f;

        class Item { public Renderer R; public Transform Src, Dst; public Collider C; public Vector3 P; public Quaternion Q; public Vector3 S; public bool On; }
        static readonly List<Item> _items = new List<Item>();
        static readonly Dictionary<Collider, Renderer> _byCol = new Dictionary<Collider, Renderer>();
        static Scene _scene, _builtFor; static PhysicsScene _ps;
        public static int Count => _items.Count;
        public static bool Ready => _scene.IsValid() && _builtFor.IsValid() && _builtFor == SceneManager.GetActiveScene();

        static readonly string[] LooseProps = { "Chunk", "Rubble", "Lump", "Stone", "Crate", "Cart", "Debris", "Pipe", "Barricade", "Bracket", "Rock", "Shard", "Piece", "Fallen" };
        static readonly string[] Architecture = { "Door", "Frame", "Rim", "Leaf", "Wall", "Arch", "Rib", "Column", "Pillar", "Gate", "Jamb", "Lintel", "Emblem", "Mechanism", "Pedestal", "Alcove" };
        /// <summary>QF9: may this mesh fade out of the camera's way? Never architecture.</summary>
        /// <summary>-noqf9fix = 0.10.5's behaviour (architecture under 2.6 m could fade; solids faded when still blind) - repro only.</summary>
        public static bool QF9Fix = true;
        public static bool IsLooseProp(string n)
        {
            foreach (var a in Architecture) if (n.Contains(a)) return false;
            foreach (var p in LooseProps) if (n.Contains(p)) return true;
            return false;
        }

        static bool Skip(Renderer r, Transform[] exclude)
        {
            var go = r.gameObject;
            if (go.layer == 20 || go.layer == 22 || go.layer == 5) return true;
            foreach (var e in exclude) if (e != null && r.transform.IsChildOf(e)) return true;
            if (r.GetComponent<Collider>() != null && go.layer != 21) return true;        // already a real collider
            var n = r.name;
            if (n.Contains("Fog") || n.Contains("Glow") || n.Contains("Beam") || n.Contains("Halo") || n.Contains("Light")
                || n.Contains("Ghost") || n.Contains("Skylight") || n.Contains("Haze") || n.Contains("Mist") || n.Contains("Lines")
                || n.Contains("Engrav") || n.Contains("Seam") || n.Contains("Inscription") || n.Contains("Cracks")
                || n.StartsWith("Blast")) return true;                                   // QF15: the escape's explosion glow is light, not a surface
            var m = r.sharedMaterial;
            if (m == null || m.renderQueue > 2500) return true;                              // transparent sheets
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return true;
            var s = r.bounds.size;
            if (Mathf.Min(s.x, Mathf.Min(s.y, s.z)) < 0.015f && Mathf.Max(s.x, Mathf.Max(s.y, s.z)) < 1f) return true;   // decals
            // sky domes / far backdrops. QF15 (2): in Europa nothing is skipped by size - the chamber's ice ceiling and floor,
            // the ice passage, the approach / escape tubes, the temple hall, the pillars and structures were all over 60 m, so
            // the bike's sweep (which only sees this scene) never saw them and the swim volumes were all that held it in
            if (Mathf.Max(s.x, Mathf.Max(s.y, s.z)) > 60f && !(_huge && !n.Contains("Sky") && !n.Contains("Backdrop") && !n.Contains("Distant"))) return true;
            return false;
        }
        /// <summary>QF15 (2): the next Build keeps meshes larger than 60 m (Europa: every surface the bike can meet).</summary>
        public static bool IncludeHuge;
        static bool _huge;
        public static IEnumerable<Renderer> Renderers { get { foreach (var it in _items) if (it.R != null) yield return it.R; } }

        // QF15: raw queries (both faces) for the bike's sweep and the ride camera
        public static int SphereCastAll(Vector3 o, float r, Vector3 dir, float len, RaycastHit[] buf, int mask = AllMask)
        {
            if (!Ready || len <= 0f) return 0;
            SyncAll();
            bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            int n = _ps.SphereCast(o, r, dir.normalized, buf, len, mask, QueryTriggerInteraction.Ignore);
            Physics.queriesHitBackfaces = bf;
            return n;
        }
        public static int RaycastAll(Vector3 o, Vector3 dir, float len, RaycastHit[] buf, int mask = AllMask)
        {
            if (!Ready || len <= 0f) return 0;
            SyncAll();
            bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            int n = _ps.Raycast(o, dir.normalized, buf, len, mask, QueryTriggerInteraction.Ignore);
            Physics.queriesHitBackfaces = bf;
            return n;
        }
        public static int OverlapSphere(Vector3 p, float r, Collider[] buf, int mask = AllMask)
        {
            if (!Ready) return 0;
            SyncAll();
            return _ps.OverlapSphere(p, r, buf, mask, QueryTriggerInteraction.Ignore);
        }
        /// <summary>Move a helper object into the occluder scene (gone with it when the scene changes).</summary>
        public static bool Adopt(GameObject go) { if (!Ready) return false; SceneManager.MoveGameObjectToScene(go, _scene); return true; }
        static readonly Collider[] _obuf = new Collider[32];
        public static void OverlapRenderers(Vector3 p, float r, List<Renderer> into)
        {
            int n = OverlapSphere(p, r, _obuf);
            for (int i = 0; i < n; i++) { var rr = RendererOf(_obuf[i]); if (rr != null && !into.Contains(rr)) into.Add(rr); }
        }

        /// <summary>Build for the active scene (once per scene load). <paramref name="exclude"/>: 7, the cat, the guards.</summary>
        public static void Build(params Transform[] exclude)
        {
            var active = SceneManager.GetActiveScene();
            if (Ready) return;
            Clear();
            _huge = IncludeHuge; IncludeHuge = false;
            _scene = SceneManager.CreateScene("CameraOccluders_" + Time.frameCount + "_" + active.name, new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            _ps = _scene.GetPhysicsScene();
            _builtFor = active;
            var t0 = Time.realtimeSinceStartup;
            int solid = 0, fade = 0, skipped = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (r.gameObject.scene != active || Skip(r, exclude)) { skipped++; continue; }
                var mesh = r.GetComponent<MeshFilter>().sharedMesh;
                var s = r.bounds.size;
                // QF9 (Dan: door frames still flicker): architecture never fades - frames, walls, columns, ribs, arches
                // only make the camera pull in. Only loose props (chunks, rubble, crates, carts, pipes) may fade.
                bool small = (Mathf.Max(s.x, Mathf.Max(s.y, s.z)) < FadeSize && (IsLooseProp(r.name) || !QF9Fix)) || r.gameObject.layer == 21;
                var go = new GameObject("occ_" + r.name) { layer = small ? FadeOnly : Solid };
                SceneManager.MoveGameObjectToScene(go, _scene);
                Collider c;
                if (mesh.name == "Cube" || !mesh.isReadable) { var b = go.AddComponent<BoxCollider>(); b.center = mesh.bounds.center; b.size = mesh.bounds.size; c = b; }
                else { var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = _huge ? TwoSided(mesh) : mesh; c = mc; }
                var it = new Item { R = r, Src = r.transform, Dst = go.transform, C = c };
                Sync(it, true);
                _items.Add(it); _byCol[c] = r;
                if (small) fade++; else solid++;
            }
            Physics.SyncTransforms(); _ps.Simulate(0.0001f);
            Debug.Log($"[Seven] camera occluders (QF8): {solid} solid + {fade} fade-only visual meshes ({skipped} skipped) in {(Time.realtimeSinceStartup - t0) * 1000f:0} ms");
        }

        // QF15 (2): sphere sweeps do not reliably hit a triangle from behind (queriesHitBackfaces is for rays); the level's
        // tubes face inward and its solids outward, so the bike met many surfaces from their back. In Europa every collider
        // copy has both faces.
        static readonly Dictionary<Mesh, Mesh> _two = new Dictionary<Mesh, Mesh>();
        static Mesh TwoSided(Mesh m)
        {
            if (_two.TryGetValue(m, out var d) && d != null) return d;
            var v = m.vertices; var t = m.triangles; var t2 = new int[t.Length * 2];
            System.Array.Copy(t, t2, t.Length);
            for (int i = 0; i < t.Length; i += 3) { t2[t.Length + i] = t[i]; t2[t.Length + i + 1] = t[i + 2]; t2[t.Length + i + 2] = t[i + 1]; }
            d = new Mesh { name = m.name + "_2s", indexFormat = v.Length > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            d.vertices = v; d.triangles = t2; d.RecalculateBounds();
            _two[m] = d; return d;
        }

        static void Clear()
        {
            _items.Clear(); _byCol.Clear();
            if (_scene.IsValid() && _scene.isLoaded) SceneManager.UnloadSceneAsync(_scene);
            _builtFor = default;
        }

        static bool Sync(Item it, bool force)
        {
            bool on = it.R != null && it.R.enabled && it.R.gameObject.activeInHierarchy;
            bool changed = force || on != it.On;
            if (on != it.On || force) { it.Dst.gameObject.SetActive(on); it.On = on; }
            if (!on) return changed;
            var p = it.Src.position; var q = it.Src.rotation; var s = it.Src.lossyScale;
            if (force || p != it.P || q != it.Q || s != it.S)
            {
                it.Dst.SetPositionAndRotation(p, q); it.Dst.localScale = s;
                it.P = p; it.Q = q; it.S = s; changed = true;
            }
            return changed;
        }

        static int _syncedFrame = -1;
        /// <summary>Follow moving / hidden sources (cheap; once per frame).</summary>
        public static void SyncAll()
        {
            if (!Ready || _syncedFrame == Time.frameCount) return;
            _syncedFrame = Time.frameCount;
            bool any = false;
            foreach (var it in _items) any |= Sync(it, false);
            if (any) { Physics.SyncTransforms(); _ps.Simulate(0.0001f); }   // no bodies in it: this only syncs the moved colliders
        }

        public static Renderer RendererOf(Collider c) => c != null && _byCol.TryGetValue(c, out var r) ? r : null;

        // ---------------------------------------------------------------------------------------------- queries
        static readonly RaycastHit[] _buf = new RaycastHit[16];

        public static bool Raycast(Vector3 o, Vector3 dir, float len, out RaycastHit hit, int mask = AllMask)
        {
            hit = default;
            if (!Ready || len <= 0f) return false;
            SyncAll();
            bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            bool h = _ps.Raycast(o, dir.normalized, out hit, len, mask, QueryTriggerInteraction.Ignore);
            Physics.queriesHitBackfaces = bf;
            return h;
        }

        public static bool SphereCast(Vector3 o, float r, Vector3 dir, out RaycastHit hit, float len, int mask = SolidMask)
        {
            hit = default;
            if (!Ready || len <= 0f) return false;
            SyncAll();
            bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            bool h = _ps.SphereCast(o, r, dir.normalized, out hit, len, mask, QueryTriggerInteraction.Ignore);
            Physics.queriesHitBackfaces = bf;
            return h;
        }

        /// <summary>All occluders on the segment a -> b (fade-only and solid), as their renderers.</summary>
        public static int RenderersBetween(Vector3 a, Vector3 b, List<Renderer> into, int mask = AllMask)
        {
            if (!Ready) return 0;
            SyncAll();
            var d = b - a; float len = d.magnitude; if (len < 1e-3f) return 0;
            bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            int n = _ps.Raycast(a, d / len, _buf, len, mask, QueryTriggerInteraction.Ignore);
            Physics.queriesHitBackfaces = bf;
            int added = 0;
            for (int i = 0; i < n; i++) { var r = RendererOf(_buf[i].collider); if (r != null && !into.Contains(r)) { into.Add(r); added++; } }
            return added;
        }

        /// <summary>QF12 (7): stray geometry standing in the walkable ground - every enabled ground zone is sampled
        /// (0.7 m grid, 0.6 m in from its edges); at 0.5 and 1.3 m above the floor four short horizontal rays (0.3 m) look
        /// for any visual mesh. Returns "N blocked samples: name xK, ..." (walls along the edges are excluded by the inset;
        /// what is left stands in the middle of a walk path).</summary>
        public static string WalkBlockers(out int blocked, System.Func<Ground.Zone, bool> which = null)
        {
            blocked = 0;
            if (!Ready) return "occluders not ready";
            SyncAll();
            var count = new Dictionary<string, int>(); var at = new Dictionary<string, Vector3>();
            bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            var dirs = new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            foreach (var z in Ground.Zones)
            {
                if (!z.Usable || (which != null && !which(z))) continue;
                for (float x = z.Min.x + 0.6f; x <= z.Max.x - 0.6f; x += 0.7f)
                    for (float zz = z.Min.y + 0.6f; zz <= z.Max.y - 0.6f; zz += 0.7f)
                    {
                        if (!z.Contains(x, zz, 0.6f)) continue;
                        float y = z.HeightAt(x, zz);
                        string hitName = null; Vector3 hp = default;
                        foreach (var h in new[] { 0.5f, 1.3f })
                        {
                            var o = new Vector3(x, y + h, zz);
                            foreach (var d in dirs)
                                if (_ps.Raycast(o, d, out var hit, 0.3f, AllMask, QueryTriggerInteraction.Ignore))
                                { var r = RendererOf(hit.collider); if (r != null && r.name.Contains("Grass")) continue; hitName = r != null ? r.name + " (" + (r.transform.parent != null ? r.transform.parent.name : "-") + ")" : hit.collider.name; hp = o; break; }
                            if (hitName != null) break;
                        }
                        if (hitName == null) continue;
                        blocked++;
                        count[hitName] = count.TryGetValue(hitName, out int c) ? c + 1 : 1;
                        if (!at.ContainsKey(hitName)) at[hitName] = hp;
                    }
            }
            Physics.queriesHitBackfaces = bf;
            var sb = new System.Text.StringBuilder();
            foreach (var kv in System.Linq.Enumerable.Take(System.Linq.Enumerable.OrderByDescending(count, k => k.Value), 14)) sb.Append($"{kv.Key} x{kv.Value} at {at[kv.Key]:0.0}; ");
            return sb.ToString();
        }

        static readonly RaycastHit[] _sbuf = new RaycastHit[32];
        static readonly RaycastHit[] _lbuf = new RaycastHit[32];
        /// <summary>QF14 (6): the nearest hit along a sphere cast - level colliders (mask) and solid visual meshes - that
        /// is not one of the <paramref name="ignore"/> renderers (walls that are cut away instead of blocking the camera).</summary>
        public static bool SphereIgnoring(Vector3 o, float r, Vector3 dir, float len, int mask, HashSet<Renderer> ignore, out float dist)
        {
            dist = len; bool h = false;
            var d = dir.normalized;
            int n = Physics.SphereCastNonAlloc(o, r, d, _lbuf, len, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = _lbuf[i].collider; if (c == null) continue;
                var rr = c.GetComponent<Renderer>();
                if (rr != null && ignore != null && ignore.Contains(rr)) continue;
                if (_lbuf[i].distance <= 0f && _lbuf[i].point == Vector3.zero) continue;      // started inside: ignore
                if (_lbuf[i].distance < dist) { dist = _lbuf[i].distance; h = true; }
            }
            if (Ready)
            {
                SyncAll();
                bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
                int m = _ps.SphereCast(o, r, d, _sbuf, len, SolidMask, QueryTriggerInteraction.Ignore);
                Physics.queriesHitBackfaces = bf;
                for (int i = 0; i < m; i++)
                {
                    var rr = RendererOf(_sbuf[i].collider);
                    if (rr != null && ignore != null && ignore.Contains(rr)) continue;
                    if (_sbuf[i].distance <= 0f && _sbuf[i].point == Vector3.zero) continue;
                    if (_sbuf[i].distance < dist) { dist = _sbuf[i].distance; h = true; }
                }
            }
            return h;
        }

        // ------------------------------------------------------------------- combined (level colliders + occluders)
        /// <summary>Level colliders (mask) or solid visual meshes on the ray.</summary>
        public static bool AnyRay(Vector3 o, Vector3 dir, float len, int mask, out float dist, int occMask = SolidMask)
        {
            dist = len;
            bool h = false;
            if (Physics.Raycast(o, dir, out var a, len, mask, QueryTriggerInteraction.Ignore)) { dist = a.distance; h = true; }
            if (Raycast(o, dir, dist, out var b, occMask)) { dist = Mathf.Min(dist, b.distance); h = true; }
            return h;
        }

        public static bool AnySphere(Vector3 o, float r, Vector3 dir, float len, int mask, out float dist, int occMask = SolidMask)
        {
            dist = len;
            bool h = false;
            if (Physics.SphereCast(o, r, dir, out var a, len, mask, QueryTriggerInteraction.Ignore)) { dist = a.distance; h = true; }
            if (SphereCast(o, r, dir, out var b, dist, occMask)) { dist = Mathf.Min(dist, b.distance); h = true; }
            return h;
        }
    }
}
