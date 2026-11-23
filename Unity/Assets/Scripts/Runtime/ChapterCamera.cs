using UnityEngine;
using UnityEngine.InputSystem;

namespace Seven
{
    /// <summary>
    /// Chapter 1 camera: orbit around 7 (right stick / mouse - Round 9: the mouse orbits without a button, the cursor is
    /// locked during play; R3 / R / middle mouse re-centres behind her; it rises while she hangs or climbs)
    /// with a sphere-cast against the level's colliders so it never ends up inside rock. Cutscenes take over with
    /// <see cref="Shot"/> (a fixed position looking at a point, eased in) and hand back with <see cref="Follow"/>.
    /// </summary>
    public partial class ChapterCamera : MonoBehaviour
    {
        public Transform Target;
        public ChapterInput Input;
        public float Distance = 3.8f, LookHeight = 1.45f, DefaultPitch = 15f, MinPitch = -10f, MaxPitch = 75f;
        public float StickYawSpeed = 150f, StickPitchSpeed = 85f, MouseDegPerPixel = 0.18f;
        public float Yaw, Pitch;
        [System.NonSerialized] public bool AutoBehind;      // check runs: keep the camera behind her walk direction
        // QF14 (5) - Dan: "race camera keeps resetting". Cause: under water round the arches / pillars the follow camera's
        // wall logic called the spot "tight" and swung or CUT round (and snapped in for any mesh), then AutoBehind eased it
        // back - a reset every few seconds. In RaceMode there are no swings or cuts, pulling in is smoothed, and the camera
        // stays behind the bike following her steering; for a moment at the start and after each ring it turns smoothly
        // toward the next ring (RaceAim until RaceAimUntil).
        [System.NonSerialized] public bool RaceMode;
        [System.NonSerialized] public Vector3? RaceAim;
        [System.NonSerialized] public float RaceAimUntil;

        bool _shot;
        Vector3 _shotPos, _shotLook, _shotPosVel, _shotLookVel, _look;
        float _shotEase = 1f, _dist, _distVel, _yawVel, _pitchVel, _recYawVel, _recPitchVel;
        bool _recentering;
        Vector3 _focus, _focusVel;

        public bool InShot => _shot;
        public Vector3 ShotPos => _shotPos;
        public Vector3 ShotLook => _shotLook;
        public float Fov => GetComponent<Camera>().fieldOfView;
        /// <summary>The camera has eased (almost) onto its shot position.</summary>
        public bool Settled => !_shot || (transform.position - _shotPos).magnitude < 0.25f;

        /// <summary>QF4: who must stay in clear view during a conversation shot (transform + height offset). While set,
        /// a shot whose line of sight to any of them is blocked (or that leaves one out of the frame) is moved to the
        /// nearest clear framing around them - eased when the way there is open, cut when a wall is in between.</summary>
        public readonly System.Collections.Generic.List<(Transform t, float h)> Subjects = new System.Collections.Generic.List<(Transform, float)>();
        public static int Reframes;
        /// <summary>The clear-view rule applies while a line plays and until this time (set just before a conversation).</summary>
        [System.NonSerialized] public float TalkUntil;
        float _nextCheck; int _fails;
        bool _userShot;          // a cut shot set by the script (check-only views) is never reframed

        /// <summary>True when nothing solid lies between a and b (the last 0.3 m at b is ignored: the subject itself).</summary>
        public static string LastBlocker;   // QF21: what the last blocked line hit (framing logs)
        public static bool ClearLine(Vector3 a, Vector3 b)
        {
            var d = b - a; float len = d.magnitude;
            if (len < 0.35f) return true;
            if (Physics.Raycast(a, d / len, out var rh, len - 0.3f, ~0, QueryTriggerInteraction.Ignore)) { LastBlocker = rh.collider != null ? rh.collider.name : "?"; return false; }
            // QF8: visual-only meshes (rock, ribs, columns, frames) block a framing too
            bool clear = !CameraOccluders.Raycast(a, d, len - 0.3f, out var oh, CameraOccluders.AllMask);
            if (!clear) LastBlocker = "visual mesh " + (oh.collider != null ? oh.collider.name : "?");
            return clear;
        }

        bool SeesAll(Vector3 pos, Vector3 look, float fov, float margin = 0.97f)
        {
            var rot = Quaternion.LookRotation(look - pos);
            float tanV = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad), aspect = GetComponent<Camera>().aspect;
            foreach (var (t, h) in Subjects)
            {
                if (t == null) continue;
                var p = t.position + Vector3.up * h;
                if (!ClearLine(pos, p)) return false;
                // QF8: no other subject in front of this one (her head and hair - 0.26 m - covering the cat)
                foreach (var (t2, h2) in Subjects)
                {
                    if (t2 == null || t2 == t) continue;
                    var q = t2.position + Vector3.up * h2;
                    var seg = p - pos; float sl = seg.magnitude;
                    float along = Vector3.Dot(q - pos, seg / sl);
                    if (along <= 0f || along >= sl - 0.1f) continue;
                    if ((pos + seg / sl * along - q).magnitude < 0.26f) return false;
                }
                var lp = Quaternion.Inverse(rot) * (p - pos);
                if (lp.z < 0.3f) return false;
                if (Mathf.Abs(lp.y / lp.z) > tanV * margin || Mathf.Abs(lp.x / lp.z) > tanV * aspect * margin) return false;
            }
            return true;
        }

        void Reframe()
        {
            var cam = GetComponent<Camera>();
            if (SeesAll(_shotPos, _shotLook, cam.fieldOfView)) { _fails = 0; return; }
            if (++_fails < 2) return;           // blocked for two checks in a row (0.4 s) before moving
            var c = Vector3.zero; int n = 0;
            foreach (var (t, h) in Subjects) if (t != null) { c += t.position + Vector3.up * h; n++; }
            if (n == 0) return;
            c /= n;
            var look = c + Vector3.down * 0.1f;
            var away = _shotPos - c; away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            float d0 = Mathf.Clamp((_shotPos - c).magnitude, 2.4f, 6f);
            float fov0 = cam.fieldOfView;
            foreach (var fov in new[] { fov0, Mathf.Min(fov0 + 10f, 64f) })
            {
            cam.fieldOfView = fov;
            foreach (var dist in new[] { d0, d0 * 0.8f, d0 * 1.25f, d0 * 1.6f, d0 * 0.6f, 2.0f })
                foreach (var yaw in new[] { 0f, 25f, -25f, 50f, -50f, 75f, -75f, 105f, -105f, 140f, -140f, 180f })
                    foreach (var pitch in new[] { 16f, 28f, 6f, 40f })
                    {
                        var dir = Quaternion.AngleAxis(yaw, Vector3.up) * away.normalized;
                        dir = Quaternion.AngleAxis(-pitch, Vector3.Cross(Vector3.up, dir)) * dir;
                        var cand = c + dir.normalized * dist;
                        if (Physics.CheckSphere(cand, 0.25f, ~0, QueryTriggerInteraction.Ignore)) continue;
                        if (Physics.SphereCast(c, 0.2f, dir, out _, dist, ~0, QueryTriggerInteraction.Ignore)) continue;
                        // keep the authored aim if it still frames everyone, else aim between them
                        var aim = SeesAll(cand, _shotLook, cam.fieldOfView, 0.85f) ? _shotLook : Vector3.Lerp(look, _shotLook, 0.25f);
                        if (!SeesAll(cand, aim, cam.fieldOfView, 0.85f)) { aim = look; if (!SeesAll(cand, aim, cam.fieldOfView, 0.85f)) continue; }
                        bool cut = Physics.SphereCast(transform.position, 0.15f, cand - transform.position, out _, (cand - transform.position).magnitude, ~0, QueryTriggerInteraction.Ignore);
                        Reframes++;
                        Debug.Log($"[Seven] dialogue camera reframed: {_shotPos} -> {cand} ({(cut ? "cut" : "eased")})");
                        _shotPos = cand; _shotLook = aim; _fails = 0;
                        if (cut) { transform.position = cand; _look = aim; transform.rotation = Quaternion.LookRotation(aim - cand); _shotPosVel = _shotLookVel = Vector3.zero; }
                        return;
                    }
            }
            cam.fieldOfView = fov0;
        }

        ChapterSeven _seven;
        float _swingVel;
        float Lift => _seven != null ? _seven.CameraLift : 0f;

        void OnEnable() { CursorPolicy.WantLock = true; }
        void OnDisable() { CursorPolicy.WantLock = false; }

        void Start()
        {
            if (GetComponent<CameraBodyGuard>() == null) gameObject.AddComponent<CameraBodyGuard>();   // QF28 pass
            if (Target != null) _seven = Target.GetComponent<ChapterSeven>();
            Pitch = DefaultPitch;
            if (Target != null) { Yaw = Target.eulerAngles.y; _focus = Target.position + Vector3.up * LookHeight; }
            _dist = Distance;
            _look = _focus;
        }

        /// <summary>Cut or ease to a fixed framing.</summary>
        public void Shot(Vector3 pos, Vector3 look, float ease = 1.2f, bool cut = false, float fov = 0f)
        {
            var c = GetComponent<Camera>();
            if (_baseFov <= 0f) _baseFov = c.fieldOfView;
            c.fieldOfView = fov > 0f ? fov : _baseFov;
            _shot = true; _shotPos = pos; _shotLook = look; _shotEase = Mathf.Max(0.01f, ease);
            _userShot = cut && ease <= 0.05f; _nextCheck = 0f;
            ShotBegan(pos, cut);   // QF21 (6)
            if (cut) { transform.position = pos; _look = look; transform.rotation = Quaternion.LookRotation(look - pos); }
        }

        /// <summary>Back to following 7 (from wherever the camera is now).</summary>
        float _baseFov;
        public void Follow(bool behind = true)
        {
            Subjects.Clear(); ShotFollow = null; ShotEnded();   // QF21 (6)
            if (_baseFov > 0f) GetComponent<Camera>().fieldOfView = _baseFov;
            _shot = false; _rideOn = false;
            if (behind && Target != null)
            {
                var d = Target.position + Vector3.up * LookHeight - transform.position; d.y = 0f;
                if (d.sqrMagnitude > 0.01f) Yaw = Quaternion.LookRotation(d).eulerAngles.y;
            }
            _focus = Target != null ? Target.position + Vector3.up * LookHeight : _focus;
            _dist = Mathf.Max(0.5f, (transform.position - _focus).magnitude);
            // QF14 (6): back from a cutscene in cut-away mode the camera starts at its boom length (no ease in / out after
            // the cut) - and the bounce measure starts over: a cut is not a bounce
            if (Cutaway != null) { _dist = Distance; _cwTarget = Distance; _cwVel = 0f; _cwLast = -1f; _cwSign = 0; }
        }

        public const int CamMaskPublic = CamMask;
        /// <summary>QF8 sweep: jump to the current yaw / pitch at full distance (as if the player had just let go).</summary>
        public void SnapTo()
        {
            if (Target == null) return;
            _shot = false; _swinging = false; _occluded = 0f; _lastUserTurn = -99f;
            _focus = Target.position + Vector3.up * (LookHeight + Lift); _dist = Distance;
            Place(0.02f);
        }

        Transform _bikeOf; HoverBike _bike;
        /// <summary>QF13 (13): 0..1 how much of the speed push is on (FOV +9 deg at full).</summary>
        [System.NonSerialized] public float SpeedKick;
        public void SnapBehind()
        {
            if (Target == null) return;
            if (_bikeOf != Target) { _bikeOf = Target; _bike = Target.GetComponent<HoverBike>(); }
            if (RidingNow && !_shot) { RideSnapBehind(); return; }
            Yaw = Target.eulerAngles.y; Pitch = DefaultPitch;
            _focus = Target.position + Vector3.up * LookHeight; _dist = Distance;
            Place(1f);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (_shot)
            {
                if (_faded.Count > 0 || _fades.Count > 0) Unfade();
                if (_rideCut.Count > 0) RideUncutAll();
                _rideOn = false;                                 // QF15 (1): back from a cutscene the ride camera eases on from here
                if (Subjects.Count > 0 && !_userShot && (DialoguePlayer.AnyBusy || Time.time < TalkUntil) && Time.time >= _nextCheck) { _nextCheck = Time.time + 0.2f; Reframe(); }
                ShotTrack(dt);   // QF21 (6): no shot holds on empty space
                transform.position = Vector3.SmoothDamp(transform.position, _shotPos, ref _shotPosVel, _shotEase * 0.5f);
                _look = Vector3.SmoothDamp(_look, _shotLook, ref _shotLookVel, _shotEase * 0.5f);
                transform.rotation = Quaternion.LookRotation(_look - transform.position);
                return;
            }
            if (Target == null) return;
            if (_bikeOf != Target) { _bikeOf = Target; _bike = Target.GetComponent<HoverBike>(); }
            // QF15 (1): while she rides there is one bike camera (ChapterCameraRide.cs) - none of the on-foot logic below runs
            if (RidingNow) { LateUpdateRide(dt); return; }
            if (_rideOn) { _rideOn = false; RideUncutAll(); }
            // QF13 (13): a sense of speed on the bike - the field of view opens a little at speed and the camera sits a bit
            // further back and lower (the 2.4-3 m bike stays readable)
            {
                var cm = GetComponent<Camera>(); if (_baseFov <= 0f) _baseFov = cm.fieldOfView;
                float k = _bike != null && _bike.Riding ? Mathf.Clamp01((_bike.Speed - 8f) / 16f) : 0f;
                SpeedKick = Mathf.MoveTowards(SpeedKick, k, dt * 1.5f);
                cm.fieldOfView = Mathf.Lerp(cm.fieldOfView, _baseFov + 9f * SpeedKick, dt * 4f);
            }
            Vector2 stick = Input != null ? Input.Look.ReadValue<Vector2>() : Vector2.zero;
            _yawVel = Mathf.MoveTowards(_yawVel, stick.x * StickYawSpeed, 500f * dt);
            _pitchVel = Mathf.MoveTowards(_pitchVel, -stick.y * StickPitchSpeed, 500f * dt);
            float dYaw = _yawVel * dt, dPitch = _pitchVel * dt;
            var mouse = Mouse.current;
            if (mouse != null && DemoBoot.CaptureDir == null && !PauseMenu.IsOpen && Cursor.lockState == CursorLockMode.Locked)
            {
                var d = mouse.delta.ReadValue();
                dYaw += d.x * MouseDegPerPixel; dPitch -= d.y * MouseDegPerPixel;
            }
            if (Mathf.Abs(dYaw) + Mathf.Abs(dPitch) > 0.01f) { _recentering = false; _lastUserTurn = Time.time; _swinging = false; }
            if (Input != null && Input.Recenter.WasPressedThisFrame()) _recentering = true;
            Yaw += dYaw; Pitch = Mathf.Clamp(Pitch + dPitch, MinPitch, MaxPitch);
            if ((_recentering || AutoBehind) && !_swinging && (_occluded < 0.15f || RaceMode))
            {
                float target = Target.eulerAngles.y;
                if (RaceMode && RaceAim.HasValue && Time.time < RaceAimUntil)
                {
                    var to = RaceAim.Value - Target.position; var fl = new Vector3(to.x, 0f, to.z);
                    if (fl.sqrMagnitude > 4f)
                    {
                        // ease from her heading toward the ring and back as the moment ends
                        float k = Mathf.Clamp01((RaceAimUntil - Time.time) / 0.6f);
                        target = Mathf.LerpAngle(target, Quaternion.LookRotation(fl).eulerAngles.y, k);
                        float pt = Mathf.Clamp(DefaultPitch + Mathf.Atan2(-to.y, fl.magnitude) * Mathf.Rad2Deg * 0.6f, -5f, 40f);
                        Pitch = Mathf.SmoothDamp(Pitch, Mathf.Lerp(DefaultPitch, pt, k), ref _racePitchVel, 0.5f);
                    }
                }
                Yaw = Mathf.SmoothDampAngle(Yaw, target, ref _recYawVel, AutoBehind ? (Cutaway != null ? 0.9f : RaceMode ? 0.45f : 0.6f) : 0.25f);
                if (!(RaceMode && RaceAim.HasValue && Time.time < RaceAimUntil)) Pitch = Mathf.SmoothDamp(Pitch, Lift > 0.05f ? 28f : DefaultPitch, ref _recPitchVel, 0.4f);
                if (_recentering && Mathf.Abs(Mathf.DeltaAngle(Yaw, target)) < 0.5f) _recentering = false;
            }
            _focus = Vector3.SmoothDamp(_focus, Target.position + Vector3.up * (LookHeight + Lift), ref _focusVel, 0.15f);
            // Round 9: while she hangs / climbs the camera looks down at her a little more (over whatever is behind her)
            if (Lift > 0.05f && Pitch < 28f) Pitch = Mathf.SmoothDamp(Pitch, 28f, ref _swingVel, 0.3f);

            Place(dt);
        }

        // ---------------------------------------------------------------------------------------------------------
        // QF7 (Dan: "walls keep getting in the way" in the traversal stretch). Three causes, three fixes:
        //  1. the old sphere-cast started at the focus point, which while she hangs / climbs (focus raised by the lift)
        //     can sit inside the rock above a lip - a cast that starts inside a collider ignores it, so the camera went
        //     behind the wall. Now the focus itself is cast up from her body first (the body is never inside rock).
        //  2. the cast only kept the line focus -> camera clear, not camera -> her body: on a climb-down she hangs below
        //     the lip while the camera is still above the floor she left, so the wall she hangs on hid her. Now her head,
        //     chest and hips are checked from the camera every frame; if fewer than two are seen the camera pulls in
        //     along its line until they are, and (after a moment, or at once while hanging / climbing) swings round to
        //     the nearest open angle - behind her back, out over the drop - unless the player is turning it.
        //  3. thin props without colliders (the pipe run, ribs, fallen chunks, the cliff above the crack) were not seen
        //     by any cast: anything without a collider that comes between the camera and her fades out (its renderer
        //     draws shadows only) and comes back once the line is clear.
        // Check runs log every follow-camera frame in which her body was hidden (CameraBlockedFrames / per area).
        const int CamMask = ~((1 << 20) | (1 << 22) | (1 << 5));      // never the city shell, the saucer probe layer or UI
        public static int FollowFrames, BlockedFrames, Swings, Fades, Cuts;
        public static readonly System.Collections.Generic.Dictionary<string, int> BlockedByArea = new System.Collections.Generic.Dictionary<string, int>();
        public static string WorstBlock;
        /// <summary>Name of the stretch she is in (set by the chapter director; used for the blocked-view log).</summary>
        public static System.Func<Vector3, string> AreaName;
        float _occluded, _lastUserTurn = -99f, _swingYawVel;
        bool _swinging; float _swingTo;
        Animator _anim;
        readonly System.Collections.Generic.List<Renderer> _thin = new System.Collections.Generic.List<Renderer>();
        readonly System.Collections.Generic.Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode> _faded = new System.Collections.Generic.Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode>();
        readonly System.Collections.Generic.HashSet<Renderer> _fadeNow = new System.Collections.Generic.HashSet<Renderer>();
        bool _thinReady;

        void CollectThin()
        {
            _thinReady = true;
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (r.gameObject.layer == 20 || r.gameObject.layer == 22 || r.gameObject.layer == 5) continue;
                if (Target != null && r.transform.IsChildOf(Target.root)) continue;
                if (r.GetComponentInParent<Collider>() != null && r.gameObject.layer != 21) continue;
                var s = r.bounds.size;
                if (s.x > 9f || s.y > 9f || s.z > 9f) continue;       // big shells / fog sheets stay
                if (BigRib(r)) continue;                                 // the chamber's ribs: vertices, framing only, never faded
                if (r.name.Contains("Fog") || r.name.Contains("Glow") || r.name.Contains("Beam") || r.name.Contains("Halo") || r.name.Contains("Light")) continue;
                _thin.Add(r);
            }
        }

        Vector3[] _pts = new Vector3[3];
        void BodyPoints()
        {
            if (_anim == null && Target != null) _anim = Target.GetComponentInChildren<Animator>();
            Transform B(HumanBodyBones b) => _anim != null && _anim.isHuman ? _anim.GetBoneTransform(b) : null;
            var p = Target.position;
            var h = B(HumanBodyBones.Head); var c = B(HumanBodyBones.UpperChest) ?? B(HumanBodyBones.Chest); var hp = B(HumanBodyBones.Hips);
            _pts[0] = h != null ? h.position : p + Vector3.up * 1.6f;
            _pts[1] = c != null ? c.position : p + Vector3.up * 1.25f;
            _pts[2] = hp != null ? hp.position : p + Vector3.up * 0.95f;
        }

        /// <summary>How many of her head / chest / hips see the camera at <paramref name="cam"/>. QF8: cast from HER to the
        /// camera (a cast that starts inside rock ignores it; her body never is), against the level colliders AND the
        /// visual-only meshes (<see cref="CameraOccluders"/>), so a camera inside or behind a mesh-only wall counts as blind.</summary>
        int Seen(Vector3 cam, int mask)
        {
            int n = 0;
            foreach (var q in _pts)
            {
                var d = cam - q; float len = d.magnitude;
                if (len < 0.3f || !CameraOccluders.AnySphere(q, 0.04f, d / len, len - 0.05f, mask, out _)) n++;
            }
            return n;
        }

        // QF8: smooth fades (dithered dissolve on the level's shader, else shadows-only), held 0.5 s after the last frame
        // that blocked - no more toggling every frame (the door frames that flickered black / grey: their bounding box
        // covered the doorway, so every line through the opening switched them off and on).
        class FadeState { public float A = 1f, Last; public UnityEngine.Rendering.ShadowCastingMode Orig; public bool Dither; }
        readonly System.Collections.Generic.Dictionary<Renderer, FadeState> _fades = new System.Collections.Generic.Dictionary<Renderer, FadeState>();
        readonly System.Collections.Generic.List<Renderer> _blockers = new System.Collections.Generic.List<Renderer>();
        readonly System.Collections.Generic.List<Renderer> _done = new System.Collections.Generic.List<Renderer>();
        static MaterialPropertyBlock _mpb;
        static readonly int FadeId = Shader.PropertyToID("_Fade");

        void ApplyFade(Renderer r, FadeState f)
        {
            if (f.Dither)
            {
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                if (f.A >= 0.999f) { r.SetPropertyBlock(null); return; }
                r.GetPropertyBlock(_mpb); _mpb.SetFloat(FadeId, f.A); r.SetPropertyBlock(_mpb);
            }
            else r.shadowCastingMode = f.A < 0.5f ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : f.Orig;
        }

        void FadeBlockers(Vector3 cp, bool solidToo, float dt)
        {
            _blockers.Clear();
            int m = solidToo ? CameraOccluders.AllMask : 1 << CameraOccluders.FadeOnly;
            foreach (var q in _pts) CameraOccluders.RenderersBetween(q, cp, _blockers, m);
            float now = Time.time;
            foreach (var r in _blockers)
            {
                if (r == null) continue;
                if (!_fades.TryGetValue(r, out var f))
                {
                    var mat = r.sharedMaterial;
                    f = new FadeState { Orig = r.shadowCastingMode, Dither = mat != null && mat.HasProperty(FadeId) };
                    _fades[r] = f; Fades++;
                }
                f.Last = now;
            }
            _done.Clear();
            foreach (var kv in _fades)
            {
                var f = kv.Value;
                float target = now - f.Last < 0.5f ? 0f : 1f;
                float a = Mathf.MoveTowards(f.A, target, dt / (target < f.A ? 0.22f : 0.35f));
                if (a != f.A) { f.A = a; if (kv.Key != null) ApplyFade(kv.Key, f); }
                if (f.A >= 1f && target >= 1f) _done.Add(kv.Key);
            }
            foreach (var r in _done) _fades.Remove(r);
        }

        // ---------------------------------------------------------------------------------------------------------
        // QF14 (6) - Dan: inside Tunneler's base "the camera bounces into 7 again and again, then resets, and repeats".
        // Cause: QF13 (7) hid the base walls in the way (forceRenderingOff) but their colliders still drove the follow
        // camera - it pulled in at once for a wall that was no longer drawn ("pull in fast"), its body check then called
        // the spot "tight" and cut / swung round, and AutoBehind eased it straight back behind her into the same wall:
        // two systems fighting, every second or so. Now, while Cutaway is set (inside the base), walls are cut away
        // INSTEAD of moving the camera: they are left out of every cast, the camera keeps its distance (never closer than
        // CutawayMinDist), only the floor / anything else can shorten it, every change is smoothed with a dead band, and
        // there are no swings or cuts. Check runs measure the distance every frame (CutawayJitter / CutawayBounces).
        /// <summary>Renderers (walls, doors) cut away instead of blocking the camera; null = the normal follow camera.</summary>
        [System.NonSerialized] public System.Collections.Generic.HashSet<Renderer> Cutaway;
        public float CutawayMinDist = 1.7f;
        float _cwTarget = -1f, _cwVel;
        public static int CutawayFrames, CutawayJitter, CutawayBounces; public static float CutawayMin = 99f, CutawayMax;
        float _cwLast = -1f, _cwExtreme, _cwRevAt = -99f; int _cwSign;
        void PlaceCutaway(float dt)
        {
            BodyPoints();
            Pitch = Mathf.Clamp(Pitch, 4f, 40f);
            // the focus is on her (no trailing lag): her distance from the camera is the boom length, steady while she
            // starts, stops and turns (a lagging focus swung it +-0.2 m - a small bounce of its own)
            _focus = Target.position + Vector3.up * (LookHeight + Lift);
            var f = _focus;
            var dir = Quaternion.Euler(Pitch, Yaw, 0f) * Vector3.back;
            float want = Distance;
            if (CameraOccluders.SphereIgnoring(f, 0.2f, dir, Distance, CamMask, Cutaway, out var hd)) want = Mathf.Max(CutawayMinDist, hd - 0.05f);
            // dead band (hysteresis): small changes of the wanted distance are ignored
            if (_cwTarget < 0f || Mathf.Abs(want - _cwTarget) > 0.25f || (want >= Distance - 1e-3f && _cwTarget < Distance)) _cwTarget = want;
            _dist = Mathf.SmoothDamp(_dist, _cwTarget, ref _cwVel, 0.45f, 3f, Mathf.Max(dt, 1e-4f));
            _dist = Mathf.Max(_dist, Mathf.Min(CutawayMinDist, _cwTarget));
            _swinging = false; _occluded = 0f;
            transform.position = f + dir * _dist;
            transform.rotation = Quaternion.LookRotation(f - transform.position);
            _look = f;
            FollowFrames++;
            if (DemoBoot.CaptureDir != null || DemoBoot.PlayerSim) SampleVisibility(transform.position);
            // the measure: her distance from the camera every frame, and reversals of it (>= 5 cm swings) within 0.5 s
            float dd = (transform.position - (Target.position + Vector3.up * LookHeight)).magnitude;
            CutawayFrames++; CutawayMin = Mathf.Min(CutawayMin, dd); CutawayMax = Mathf.Max(CutawayMax, dd);
            if (_cwLast >= 0f)
            {
                int sg = dd > _cwLast + 1e-4f ? 1 : dd < _cwLast - 1e-4f ? -1 : 0;
                if (sg != 0)
                {
                    if (_cwSign == 0) { _cwSign = sg; _cwExtreme = _cwLast; }
                    else if (sg != _cwSign)
                    {
                        if (Mathf.Abs(_cwLast - _cwExtreme) >= 0.05f)
                        {
                            if (Time.time - _cwRevAt < 0.5f) { CutawayJitter++; if (Mathf.Abs(_cwLast - _cwExtreme) >= 0.15f) CutawayBounces++; }
                            _cwRevAt = Time.time;
                        }
                        _cwSign = sg; _cwExtreme = _cwLast;
                    }
                }
            }
            _cwLast = dd;
            LastCutawayDist = dd;
        }
        [System.NonSerialized] public float LastCutawayDist;

        void Place(float dt)
        {
            if (!_thinReady) CollectThin();
            if (!CameraOccluders.Ready && Target != null) CameraOccluders.Build(Target, CatRoot);
            if (Cutaway != null) { PlaceCutaway(dt); return; }
            _cwTarget = -1f;
            BodyPoints();
            bool hanging = Lift > 0.05f;
            int mask = hanging ? CamMask & ~(1 << 21) : CamMask;
            // 1. the focus, reached from inside her body (never starts inside rock)
            var body = _pts[2] + Vector3.up * 0.2f;
            var f = _focus;
            var bf = f - body; float bfl = bf.magnitude;
            if (bfl > 0.05f && CameraOccluders.AnySphere(body, 0.18f, bf / bfl, bfl, mask, out var fhd))
                f = body + bf / bfl * Mathf.Max(0f, fhd - 0.04f);
            var dir = Quaternion.Euler(Pitch, Yaw, 0f) * Vector3.back;
            float want = Distance;
            if (CameraOccluders.AnySphere(f, 0.22f, dir, Distance, mask, out var hd))
                want = Mathf.Max(0.45f, hd - 0.05f);
            // 2. her body must be in view from there: pull in along the line until two of three points are seen
            if (Seen(f + dir * want, mask) < 2)
            {
                float d = want;
                while (d > 0.45f && Seen(f + dir * d, mask) < 2) d -= 0.15f;
                want = Mathf.Max(0.45f, d);
                _occluded += dt;
            }
            else _occluded = Mathf.Max(0f, _occluded - dt * 2f);
            //    ... and swing round (and up) to the nearest open angle (not while the player is turning the camera).
            //    QF8: also when the pull-in had to come very close (a wall right behind her), sooner in tight spots.
            bool tight = want < Distance * 0.45f;
            if (!RaceMode && Time.time - _lastUserTurn > (hanging ? 0.15f : tight ? 0.4f : 0.8f) && !_swinging && (_occluded > 0.3f || tight && (hanging || _occluded > 0.05f || want < 1.0f)))
            {
                float best = 999f, bestP = Pitch;
                // QF8: steeper views too (in a tight corner of a chamber only the view from above is open); first a view
                // that sees all of her at 85 % distance, else one that sees two of three points at 60 %
                for (int pass = 0; pass < 2 && best > 900f; pass++)
                    foreach (var pitch in new[] { Mathf.Max(Pitch, hanging ? 24f : Pitch), Mathf.Max(Pitch, 32f), Mathf.Max(Pitch, 45f), Mathf.Max(Pitch, 60f), 72f })
                    {
                        float dist = Distance * (pass == 0 ? 0.85f : 0.6f);
                        for (int k = 0; k <= 9; k++)
                            foreach (var sg in new[] { 1f, -1f })
                            {
                                if (k == 0 && (sg < 0f || pitch <= Pitch + 1f)) continue;
                                float y = Yaw + sg * k * 20f;
                                var dd = Quaternion.Euler(pitch, y, 0f) * Vector3.back;
                                if (CameraOccluders.AnySphere(f, 0.22f, dd, dist, mask, out _)) continue;
                                if (Seen(f + dd * dist, mask) < (pass == 0 ? 3 : 2)) continue;
                                if (Mathf.Abs(y - Yaw) < Mathf.Abs(best - Yaw)) { best = y; bestP = pitch; }
                            }
                        if (best < 900f) break;
                    }
                if (best < 900f)
                {
                    _swinging = true; _swingTo = best; _swingPitch = bestP; Swings++;
                    // jammed against a wall (< 0.8 m): cut there at once instead of turning through the rock
                    if (want < 0.8f && !_inCut) { Yaw = best; Pitch = bestP; _swinging = false; _dist = Distance * 0.6f; Cuts++; }
                    Debug.Log($"[Seven] follow camera {(want < 0.8f ? "cuts" : "swings")} {Mathf.DeltaAngle(Yaw, best):0} deg (pitch {bestP:0}) to keep 7 in view at {Target.position}");
                    if (want < 0.8f && !_inCut) { _inCut = true; Place(dt); _inCut = false; return; }
                }
            }
            if (_swinging)
            {
                Yaw = Mathf.SmoothDampAngle(Yaw, _swingTo, ref _swingYawVel, 0.3f);
                Pitch = Mathf.SmoothDamp(Pitch, _swingPitch, ref _swingPitchVel, 0.3f);
                if (Mathf.Abs(Mathf.DeltaAngle(Yaw, _swingTo)) < 1f || Time.time - _lastUserTurn < 0.1f) _swinging = false;
            }
            // Pull in fast, ease back out slowly.
            _dist = want < _dist ? (RaceMode ? Mathf.SmoothDamp(_dist, want, ref _distVel, 0.12f, Mathf.Infinity, Mathf.Max(dt, 1e-4f)) : want) : Mathf.SmoothDamp(_dist, want, ref _distVel, 0.35f, Mathf.Infinity, Mathf.Max(dt, 1e-4f));
            if (RaceMode) _swinging = false;
            transform.position = f + dir * _dist;
            transform.rotation = Quaternion.LookRotation(f - transform.position);
            _look = f;
            // 3. props that are only meshes fade out of the way (and, if even the closest pull-in is still blocked, the wall)
            var cp = transform.position;
            bool stillBlind = Seen(cp, mask) < 2;
            FadeBlockers(cp, !CameraOccluders.QF9Fix && stillBlind, dt);   // QF9: loose props only, never architecture
            // check runs: was her body hidden this frame (after all of the above)?
            FollowFrames++;
            if (DemoBoot.CaptureDir != null || DemoBoot.PlayerSim) SampleVisibility(cp);
            if (stillBlind)
            {
                BlockedFrames++;
                string a = AreaName != null ? AreaName(Target.position) : "?";
                BlockedByArea[a] = BlockedByArea.TryGetValue(a, out var c) ? c + 1 : 1;
                if (WorstBlock == null || BlockedFrames % 30 == 1)
                {
                    var d = cp - _pts[1];
                    string by = Physics.Raycast(_pts[1], d.normalized, out var bh, d.magnitude, mask, QueryTriggerInteraction.Ignore) ? bh.collider.name
                              : CameraOccluders.Raycast(_pts[1], d, d.magnitude, out var oh) ? oh.collider.name + " (mesh)" : "?";
                    WorstBlock = $"{a}: 7 at {Target.position}, camera {cp}, by {by}";
                }
            }
        }
        float _swingPitch, _swingPitchVel, _racePitchVel;
        bool _inCut;
        /// <summary>The cat's root (excluded from the occluders); set by the chapter director.</summary>
        public static Transform CatRoot;

        // ---------------------------------------------------------------------------------------------------------
        // QF8 visibility measure (Dan: "0 points under ~60 % visible"): 17 points over her whole body (head to feet,
        // both arms) - a point counts when it is inside the frame and nothing solid (level collider or visual mesh,
        // faded props excluded) lies between it and the camera. Sampled every 0.25 s of follow camera.
        static readonly HumanBodyBones[] VisBones =
        {
            HumanBodyBones.Head, HumanBodyBones.Neck, HumanBodyBones.UpperChest, HumanBodyBones.Chest, HumanBodyBones.Spine, HumanBodyBones.Hips,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm,
            HumanBodyBones.LeftHand, HumanBodyBones.RightHand, HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,
            HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.LeftFoot,
        };
        public static int VisSamples, VisUnder60;
        public static float VisMin = 1f, VisSum;
        public static string VisWorst;
        public static readonly System.Collections.Generic.Dictionary<string, Vector2> VisByArea = new System.Collections.Generic.Dictionary<string, Vector2>();   // (min, samples)
        float _nextVis;

        /// <summary>Fraction (0..1) of her body points in the frame and in clear view from <paramref name="cam"/>.</summary>
        public float VisibleFraction(Vector3 cam, int mask = CamMask)
        {
            if (_anim == null && Target != null) _anim = Target.GetComponentInChildren<Animator>();
            if (_anim == null || !_anim.isHuman) return 1f;
            var c = GetComponent<Camera>();
            int n = 0, ok = 0;
            foreach (var bb in VisBones)
            {
                var t = _anim.GetBoneTransform(bb); if (t == null) continue;
                n++;
                var v = c.WorldToViewportPoint(t.position);
                if (v.z < c.nearClipPlane || v.x < 0f || v.x > 1f || v.y < 0f || v.y > 1f) continue;
                var d = cam - t.position; float len = d.magnitude;
                if (len < 0.2f) { ok++; continue; }
                if (Physics.Raycast(t.position, d / len, len - 0.05f, mask, QueryTriggerInteraction.Ignore)) continue;
                if (CameraOccluders.Raycast(t.position, d, len - 0.05f, out var oh, CameraOccluders.AllMask))
                {
                    var r = CameraOccluders.RendererOf(oh.collider);
                    if (!(r != null && _fades.TryGetValue(r, out var fs) && fs.A < 0.5f)) continue;   // faded out = not in the way
                }
                ok++;
            }
            return n > 0 ? ok / (float)n : 1f;
        }

        void SampleVisibility(Vector3 cp)
        {
            if (Time.time < _nextVis) return;
            _nextVis = Time.time + 0.25f;
            int mask = Lift > 0.05f ? CamMask & ~(1 << 21) : CamMask;
            float v = VisibleFraction(cp, mask);
            string a = AreaName != null ? AreaName(Target.position) : "?";
            VisSamples++; VisSum += v;
            var e = VisByArea.TryGetValue(a, out var ev) ? ev : new Vector2(1f, 0f);
            VisByArea[a] = new Vector2(Mathf.Min(e.x, v), e.y + 1f);
            if (v < 0.6f) { VisUnder60++; Debug.Log($"[Seven] follow camera: only {v * 100f:0} % of 7 visible in {a} at {Target.position} (camera {cp})"); }
            if (v < VisMin) { VisMin = v; VisWorst = $"{a}: 7 at {Target.position}, camera {cp}, {v * 100f:0} %"; }
        }

        public static string VisSummary()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kv in VisByArea) sb.Append($"{kv.Key} min {kv.Value.x * 100f:0} % ({kv.Value.y:0}), ");
            return $"follow camera visibility (QF8): {VisSamples} samples, mean {(VisSamples > 0 ? VisSum / VisSamples * 100f : 100f):0} %, worst {VisMin * 100f:0} %, points under 60 %: {VisUnder60} (must be 0); by area: {sb}{(VisWorst != null ? "worst at " + VisWorst : "")}";
        }

        public static string Summary()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kv in BlockedByArea) sb.Append(kv.Key).Append(' ').Append(kv.Value).Append(", ");
            return $"follow camera (QF7): 7's body hidden in {BlockedFrames} of {FollowFrames} follow frames ({(FollowFrames > 0 ? 100f * BlockedFrames / FollowFrames : 0f):0.00} %; by area: {(sb.Length > 0 ? sb.ToString() : "none")}), swings to an open angle {Swings} (cuts {Cuts}), props faded {Fades}{(WorstBlock != null ? "; last hidden: " + WorstBlock : "")}";
        }

        // big collider-less meshes (the chamber's flowing ribs): checked by their vertices, for cutscene framing only
        readonly System.Collections.Generic.List<Vector3[]> _bigVerts = new System.Collections.Generic.List<Vector3[]>();
        bool _bigReady;
        static bool BigRib(Renderer r) { var s = r.bounds.size; return r.name.Contains("Rib") && Mathf.Max(s.x, Mathf.Max(s.y, s.z)) > 4f; }
        void CollectBig()
        {
            _bigReady = true;
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (r.gameObject.layer != 0 || r.GetComponentInParent<Collider>() != null) continue;
                if (Target != null && r.transform.IsChildOf(Target.root)) continue;
                if (!BigRib(r)) continue;
                var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable) continue;
                var v = mf.sharedMesh.vertices; var m = r.transform.localToWorldMatrix;
                for (int i = 0; i < v.Length; i++) v[i] = m.MultiplyPoint3x4(v[i]);
                _bigVerts.Add(v);
            }
        }

        /// <summary>QF7: a prop without a collider (ruin columns, chunks, the chamber ribs) lies between a and b, or
        /// contains a.</summary>
        public bool PropBetween(Vector3 a, Vector3 b)
        {
            if (!_thinReady) CollectThin();
            if (!_bigReady) CollectBig();
            {
                var ab = b - a; float l2 = ab.sqrMagnitude;
                foreach (var verts in _bigVerts)
                    foreach (var v in verts)
                    {
                        float t = l2 > 1e-6f ? Mathf.Clamp01(Vector3.Dot(v - a, ab) / l2) : 0f;
                        if (t > 0.92f) continue;                               // the last bit at the subject
                        if ((a + ab * t - v).sqrMagnitude < 0.04f) return true;  // within 0.2 m of a rib's surface points
                    }
            }
            var d = b - a; float len = d.magnitude;
            foreach (var r in _thin)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                var bb = r.bounds; bb.Expand(0.1f);
                if (bb.Contains(a)) return true;
                if (bb.Contains(b)) continue;
                if (len > 0.3f && bb.IntersectRay(new Ray(a, d / len), out float at) && at < len - 0.25f) return true;
            }
            return false;
        }

        /// <summary>Restore every faded prop (cutscene shots see the level as built).</summary>
        void Unfade()
        {
            foreach (var kv in _faded) if (kv.Key != null) kv.Key.shadowCastingMode = kv.Value;
            _faded.Clear();
            foreach (var kv in _fades) if (kv.Key != null) { kv.Value.A = 1f; ApplyFade(kv.Key, kv.Value); }
            _fades.Clear();
        }
    }
}
