using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF40 (6): THE guide light - one component for every chapter (Chapter 1, Venus, Mars, Europa, the asteroid, the mothership,
    /// Cattalus, the finale). Before, each director had its own copy (a teleporting orb at the objective + a billboard scale), and
    /// Chapter 1 and Venus had none. Behaviour, the same everywhere:
    ///  - it appears a few metres ahead of her, then waits ahead at a comfortable distance (4-7 m) on the way to the objective,
    ///    drifting (eased, never snapping) as she moves; when she is near the objective it settles over it;
    ///  - it never sits inside a wall and never leads through one: the way from her to the lead point is tested (physics + the
    ///    camera's occluder copies of visual-only meshes); blocked = it waits at the objective itself (the on-screen marker says
    ///    where); a director that knows a route (Europa's base rooms, the escape line past the supports) feeds its waypoints, so
    ///    the light follows the route and pauses at each turn;
    ///  - it fades out when she is close (it has done its job) and in every cutscene; it fades back in with control.
    /// The on-screen marker (ChapterUI.SetMarker) is driven from here too. Directors call <see cref="Drive"/> from LateUpdate with
    /// their NextMarker. A director without a builder-made orb gets one made at runtime (<see cref="Make"/>).
    /// </summary>
    public class GuideOrb : MonoBehaviour
    {
        public float Lift = 1.25f, MinScale = 0.6f, ScalePerM = 0.03f;
        Vector3 _vel, _herLast; float _fade, _herSpeed; bool _placed, _haveHer; Vector3? _lastTarget; float _pauseUntil;
        public static int Drives, Blocked, Shown;

        public static GuideOrb On(Transform orb)
        {
            if (orb == null) return null;
            return orb.GetComponent<GuideOrb>() ?? orb.gameObject.AddComponent<GuideOrb>();
        }

        /// <summary>A runtime orb (white emissive sphere + soft halo + a small point light) for a director whose scene has none.</summary>
        public static Transform Make(string scene, Material haloMat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = "GuideLight";
            Object.Destroy(go.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); mat.SetColor("_BaseColor", Color.white);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.transform.localScale = Vector3.one * 0.6f;
            if (haloMat != null)
            {
                var halo = GameObject.CreatePrimitive(PrimitiveType.Quad); halo.name = "GuideHalo"; Object.Destroy(halo.GetComponent<Collider>());
                halo.transform.SetParent(go.transform, false); halo.transform.localScale = Vector3.one * 5.2f;
                var hm = new Material(haloMat);
                var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false); var px = new Color[64 * 64];
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) { float r = new Vector2(x - 31.5f, y - 31.5f).magnitude / 32f; float a = Mathf.Clamp01(1f - r); px[y * 64 + x] = new Color(a * a * 0.55f, a * a * 0.55f, a * a * 0.55f, a * a * 0.55f); }
                tex.SetPixels(px); tex.Apply();
                if (hm.HasProperty("_BaseMap")) hm.SetTexture("_BaseMap", tex); if (hm.HasProperty("_MainTex")) hm.SetTexture("_MainTex", tex);
                halo.GetComponent<Renderer>().sharedMaterial = hm; halo.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var l = new GameObject("GuideLightLamp").AddComponent<Light>(); l.transform.SetParent(go.transform, false);
            l.type = LightType.Point; l.range = 24f; l.intensity = 3f; l.color = Color.white; l.shadows = LightShadows.None;
            go.SetActive(false);
            Debug.Log($"[Seven] GuideOrb (QF40 6): runtime guide light made for {scene}");
            return go.transform;
        }

        /// <summary>Called every LateUpdate by the director. target = the objective's place (NextMarker); route = optional
        /// waypoints (the light follows them, pausing at each turn).</summary>
        public static void Drive(Transform orb, Vector3? target, bool cut, Camera cam, Transform her, ChapterUI ui, float lift = 1.25f, float minScale = 0.6f, float perM = 0.03f, bool setMarker = true)
        {
            if (orb == null || cam == null) return;
            var g = On(orb); g.Lift = lift; g.MinScale = minScale; g.ScalePerM = perM;
            g.Step(target, cut, cam, her);
            if (setMarker && ui != null) ui.SetMarker(target.HasValue && !cut ? target : null);
        }

        void Step(Vector3? target, bool cut, Camera cam, Transform her)
        {
            Drives++;
            float dt = Mathf.Max(1e-4f, Time.deltaTime);
            bool want = target.HasValue && !cut && her != null;
            if (want)
            {
                var tgt = target.Value + Vector3.up * Lift;
                var hp = her.position + Vector3.up * 1.3f;
                float dHer = Vector3.Distance(Flat(her.position), Flat(target.Value));
                // the lead point: 5.5 m ahead of her toward the objective (further ahead the faster she goes: on the bike), never
                // past it, at eye height
                if (_haveHer) _herSpeed = Mathf.Lerp(_herSpeed, Vector3.Distance(her.position, _herLast) / dt, 1f - Mathf.Exp(-4f * dt));
                _herLast = her.position; _haveHer = true;
                float ahead = 5.5f + Mathf.Min(_herSpeed, 30f) * 0.7f;
                var dir = tgt - hp; float len = dir.magnitude;
                Vector3 lead = len < ahead + 1f ? tgt : hp + dir / len * ahead;
                lead.y = Mathf.Lerp(hp.y + 0.2f, tgt.y, len < ahead + 1f ? 1f : 0.3f);
                // never through or inside a wall: the way from her to the lead point must be clear
                if (len >= ahead + 1f && !Clear(hp, lead)) { lead = tgt; Blocked++; }
                // a new objective / waypoint: hold a moment where it is (a pause at the turn), then drift on
                if (!_lastTarget.HasValue || (_lastTarget.Value - target.Value).sqrMagnitude > 0.25f) { if (_placed) _pauseUntil = Time.time + 0.45f; _lastTarget = target; Shown++; }
                if (!_placed || !gameObject.activeSelf && _fade <= 0.01f)
                {
                    // it appears just ahead of her (where she can see it), not at a far objective
                    var f = cam.transform.forward; f.y = 0f; f = f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.forward;
                    var start = hp + f * 3f + Vector3.up * 0.2f;
                    transform.position = Clear(hp, start) ? start : lead; _vel = Vector3.zero; _placed = true;
                }
                if (Vector3.Distance(transform.position, lead) > 60f) { transform.position = lead; _vel = Vector3.zero; }
                if (Time.time >= _pauseUntil) transform.position = Vector3.SmoothDamp(transform.position, lead, ref _vel, 0.55f, 9f + _herSpeed * 1.3f, dt);
                // fades when she is close (its job done)
                float wantFade = dHer < 2.2f ? Mathf.InverseLerp(1.2f, 2.2f, dHer) : 1f;
                _fade = Mathf.MoveTowards(_fade, wantFade, dt * 2.5f);
            }
            else _fade = Mathf.MoveTowards(_fade, 0f, dt * 4f);
            bool show = _fade > 0.01f;
            if (gameObject.activeSelf != show) gameObject.SetActive(show);
            if (!show) { if (!want) _placed = false; return; }
            float d = Vector3.Distance(cam.transform.position, transform.position);
            float pulse = 1f + 0.06f * Mathf.Sin(Time.time * 3.1f);
            transform.localScale = Vector3.one * Mathf.Max(MinScale, d * ScalePerM) * _fade * pulse;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        static bool Clear(Vector3 a, Vector3 b)
        {
            if (Physics.Linecast(a, b, out _, ~0, QueryTriggerInteraction.Ignore)) return false;
            return ChapterCamera.ClearLine(a, b);
        }
        public static string Summary() => $"GUIDE LIGHT (QF40 6): one shared guide light - {Shown} objectives / waypoints shown, lead point blocked by a wall {Blocked}x (it waited at the objective instead)";
    }
}
