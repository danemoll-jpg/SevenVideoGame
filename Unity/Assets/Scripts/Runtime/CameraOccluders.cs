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
                || n.Contains("Engrav") || n.Contains("Seam") || n.Contains("Inscription") || n.Contains("Cracks")) return true;
            var m = r.sharedMaterial;
            if (m == null || m.renderQueue > 2500) return true;                              // transparent sheets
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return true;
            var s = r.bounds.size;
            if (Mathf.Min(s.x, Mathf.Min(s.y, s.z)) < 0.015f && Mathf.Max(s.x, Mathf.Max(s.y, s.z)) < 1f) return true;   // decals
            if (Mathf.Max(s.x, Mathf.Max(s.y, s.z)) > 60f) return true;                     // sky domes / far backdrops
            return false;
        }

        /// <summary>Build for the active scene (once per scene load). <paramref name="exclude"/>: 7, the cat, the guards.</summary>
        public static void Build(params Transform[] exclude)
        {
            var active = SceneManager.GetActiveScene();
            if (Ready) return;
            Clear();
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
                else { var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = mesh; c = mc; }
                var it = new Item { R = r, Src = r.transform, Dst = go.transform, C = c };
                Sync(it, true);
                _items.Add(it); _byCol[c] = r;
                if (small) fade++; else solid++;
            }
            Physics.SyncTransforms(); _ps.Simulate(0.0001f);
            Debug.Log($"[Seven] camera occluders (QF8): {solid} solid + {fade} fade-only visual meshes ({skipped} skipped) in {(Time.realtimeSinceStartup - t0) * 1000f:0} ms");
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
