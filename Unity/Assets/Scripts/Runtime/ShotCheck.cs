using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// QF8 (Dan: "over-the-shoulder shots from 7 are always blocked by her hair - you can't see the cat"): how much of a
    /// subject (the cat) a camera really sees past a character in the foreground (7, hair included). The foreground
    /// character's skinned meshes are baked in their current pose into mesh colliders in a private physics scene; points
    /// spread over the subject's own baked mesh are then tested: in the frame, and no ray from the camera hits the
    /// foreground body (or a level collider / visual mesh). Used to pick the over-the-shoulder framings and logged on
    /// every dialogue capture ("cat N % unobstructed", must be >= 80).
    /// </summary>
    public static class ShotCheck
    {
        static Scene _scene; static PhysicsScene _ps;
        static readonly List<MeshCollider> _cols = new List<MeshCollider>();
        static readonly List<Mesh> _meshes = new List<Mesh>();
        static readonly List<Vector3> _pts = new List<Vector3>();
        static readonly List<Vector3> _tmp = new List<Vector3>();
        const int Layer = 11;

        static void EnsureScene()
        {
            if (_scene.IsValid() && _scene.isLoaded) return;
            _cols.Clear(); _meshes.Clear();
            _scene = SceneManager.CreateScene("ShotCheck_" + Time.frameCount, new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            _ps = _scene.GetPhysicsScene();
        }

        /// <summary>Bake the foreground character (<paramref name="blocker"/>) and sample the subject. Call once per framing
        /// decision, then <see cref="Visible"/> for each candidate camera.</summary>
        public static void Prepare(Transform blocker, Transform subject, int samples = 180)
        {
            EnsureScene();
            int i = 0;
            if (blocker != null)
                foreach (var sr in blocker.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (!sr.enabled || sr.sharedMesh == null) continue;
                    if (i >= _cols.Count)
                    {
                        var go = new GameObject("shotcheck_blocker") { layer = Layer };
                        SceneManager.MoveGameObjectToScene(go, _scene);
                        _cols.Add(go.AddComponent<MeshCollider>()); _meshes.Add(new Mesh { name = "baked" });
                    }
                    var m = _meshes[i];
                    sr.BakeMesh(m, true);
                    var c = _cols[i];
                    c.transform.SetPositionAndRotation(sr.transform.position, sr.transform.rotation);
                    c.transform.localScale = Vector3.one;
                    c.sharedMesh = null; c.sharedMesh = m;
                    c.gameObject.SetActive(true);
                    i++;
                }
            for (; i < _cols.Count; i++) _cols[i].gameObject.SetActive(false);
            Physics.SyncTransforms(); _ps.Simulate(0.0001f);
            _pts.Clear();
            if (subject == null) return;
            var all = new List<Vector3>();
            foreach (var sr in subject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!sr.enabled || sr.sharedMesh == null) continue;
                var m = new Mesh(); sr.BakeMesh(m, true);
                m.GetVertices(_tmp);
                var mx = Matrix4x4.TRS(sr.transform.position, sr.transform.rotation, Vector3.one);
                foreach (var v in _tmp) all.Add(mx.MultiplyPoint3x4(v));
                Object.Destroy(m);
            }
            if (all.Count == 0)
                foreach (var r in subject.GetComponentsInChildren<Renderer>()) { var b = r.bounds; all.Add(b.center); all.Add(b.center + Vector3.up * b.extents.y * 0.7f); }
            int stride = Mathf.Max(1, all.Count / samples);
            for (int k = 0; k < all.Count; k += stride) _pts.Add(all[k]);
        }

        /// <summary>Share (0..1) of the subject's sample points inside the frame of a camera at pos/rot/fov and not hidden
        /// by the foreground character or the level.</summary>
        public static float Visible(Vector3 pos, Quaternion rot, float fov, float aspect)
        {
            if (_pts.Count == 0) return 1f;
            float tanV = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad), tanH = tanV * aspect;
            var inv = Quaternion.Inverse(rot);
            int ok = 0;
            bool bf = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            foreach (var p in _pts)
            {
                var l = inv * (p - pos);
                if (l.z < 0.05f || Mathf.Abs(l.x / l.z) > tanH || Mathf.Abs(l.y / l.z) > tanV) continue;
                var d = p - pos; float len = d.magnitude; if (len < 0.05f) continue;
                if (_ps.Raycast(pos, d / len, len - 0.015f, 1 << Layer, QueryTriggerInteraction.Ignore)) continue;
                if (Physics.Raycast(pos, d / len, len - 0.06f, ~((1 << 20) | (1 << 5) | (1 << 22)), QueryTriggerInteraction.Ignore)) continue;
                ok++;
            }
            Physics.queriesHitBackfaces = bf;
            return ok / (float)_pts.Count;
        }

        public static float Visible(Camera c) => Visible(c.transform.position, c.transform.rotation, c.fieldOfView, c.aspect);
    }
}
