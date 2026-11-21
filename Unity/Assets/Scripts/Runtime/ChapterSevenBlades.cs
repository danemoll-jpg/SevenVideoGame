using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 16 — 7's ARC BLADES (Docs/CHAPTER_ASTEROID.md, Decisions; Kyle's kyle_7_blades_sketch.jpg): a pair of curved,
    /// crescent-shaped blades of white light, flickering and ribbon-like, with a soft hum - not straight swords. The
    /// Machinists' repair tools for cutting and re-joining energy lines: against machines they sever power and short out
    /// systems; against living beings they only STUN (7 never kills). Found in AST_05's maintenance locker; she keeps them
    /// for later chapters (<see cref="AsteroidState.BladesOwned"/>).
    ///  - RB / G draws or sheathes them (the controls page + the first-use prompt say so). Sheathed: two dark crescent hilts
    ///    crossed on her back. Drawn: the light ribbons grow out of the hilts in her hands (0.25 s), hum, flicker.
    ///  - On foot (drawn): X near a target = a swing (<see cref="BladeSwing"/>; the director decides what it cuts).
    ///  - In a boss fight (drawn): X = the blade chain (slash in -> slash out -> the combo, round again; Y still kicks) - the
    ///    damage that matters on a machine's power couplings (BossDef.bladeDamage).
    /// Visuals are built in code at runtime on her hand bones (the blade leaves the fist on the thumb side).
    /// </summary>
    public partial class ChapterSeven
    {
        [System.NonSerialized] public bool BladesOwned;
        public bool BladesDrawn { get; private set; }
        public int BladeDraws { get; private set; }
        Transform _bladeR, _bladeL, _hiltBack; Renderer[] _bladeLight; float _bladeGrow; AudioSource _bladeHum;
        static Material _bladeMat, _hiltMat;

        /// <summary>Round 16: a scripted clip on 7 (AST_06's landing, sitting back to breathe) - back to free with Release().</summary>
        public void PlayAnim(string state, float fade = 0.2f, float speed = 1f) { if (HasState(state)) Play(state, fade, speed); }
        public void SetBladesOwned(bool owned) { BladesOwned = owned; if (!owned && BladesDrawn) DrawBlades(false, true); EnsureBlades(); }

        /// <summary>Draw / sheathe (RB / G, or a script). quiet = no sound.</summary>
        public void DrawBlades(bool on, bool quiet = false)
        {
            if (!BladesOwned && on) return;
            if (BladesDrawn == on) return;
            EnsureBlades();
            BladesDrawn = on; if (on) BladeDraws++;
            if (!quiet) AudioKit.Play(on ? "ast_blade_draw" : "ast_blade_sheath", transform.position + Vector3.up * 1.2f, 0.9f, 0.03f, AudioKit.Bus.Sfx, 20f);
            if (Current == Mode.Fight && Act == FightAct.Idle && HasState(on ? "blade_draw" : "blade_sheath")) Play(on ? "blade_draw" : "blade_sheath", 0.1f, 1.6f, true, 0.25f);
            FightEvent?.Invoke(on ? "blades_drawn" : "blades_sheathed");
        }

        /// <summary>An on-foot swing toward a point (the director checks what is in reach at <see cref="GesturePeak"/>).</summary>
        public void BladeSwing(Vector3 face)
        {
            if (!BladesDrawn) DrawBlades(true);
            _face = face; Current = Mode.Gesture;
            Play(HasState("blade_slash_in") ? "blade_slash_in" : "hook", 0.08f, 1.35f, true, 0.12f);
            AudioKit.Play("ast_blade_swing", transform.position + Vector3.up * 1.2f, 0.8f, 0.08f, AudioKit.Bus.Sfx, 15f);
        }

        /// <summary>Read the draw button (called from the free-mode and the fight update).</summary>
        void BladeInput()
        {
            if (!BladesOwned || ScriptDriven || Input == null || Input.Blades == null) return;
            if (Input.Blades.WasPressedThisFrame()) DrawBlades(!BladesDrawn);
        }
        /// <summary>Script presses (check runs).</summary>
        [System.NonSerialized] public bool AutoBlades;

        void EnsureBlades()
        {
            if (_bladeR != null || Anim == null || !Anim.isHuman) return;
            if (_bladeMat == null) { _bladeMat = Markers.Additive(1.6f); _bladeMat.name = "ArcBlade_light"; }
            if (_hiltMat == null) { _hiltMat = Markers.DarkGloss(); _hiltMat.name = "ArcBlade_hilt"; }
            var lights = new List<Renderer>();
            _bladeR = MakeBlade(HumanBodyBones.RightHand, HumanBodyBones.RightThumbDistal, HumanBodyBones.RightMiddleProximal, false, lights);
            _bladeL = MakeBlade(HumanBodyBones.LeftHand, HumanBodyBones.LeftThumbDistal, HumanBodyBones.LeftMiddleProximal, true, lights);
            _bladeLight = lights.ToArray();
            // sheathed: two dark crescent hilts crossed on her back
            var chest = Bone(HumanBodyBones.UpperChest) ?? Bone(HumanBodyBones.Chest);
            if (chest != null)
            {
                _hiltBack = new GameObject("ArcBlades_Back").transform; _hiltBack.SetParent(chest, false);
                _hiltBack.position = chest.position - transform.forward * 0.17f; _hiltBack.rotation = transform.rotation;
                foreach (var s in new[] { 1f, -1f })
                {
                    var h = new GameObject("Hilt").AddComponent<MeshFilter>(); h.transform.SetParent(_hiltBack, false);
                    h.sharedMesh = Crescent(0.34f, 0.022f, 0.016f, 70f);
                    h.gameObject.AddComponent<MeshRenderer>().sharedMaterial = _hiltMat;
                    h.transform.localRotation = Quaternion.Euler(0f, 90f, 35f * s) * Quaternion.Euler(0f, 0f, -90f);
                }
            }
            ApplyBladeVisual(0f);
        }

        Transform MakeBlade(HumanBodyBones hand, HumanBodyBones thumb, HumanBodyBones middle, bool left, List<Renderer> lights)
        {
            var h = Bone(hand); if (h == null) return null;
            var th = Bone(thumb); var mid = Bone(middle);
            // the blade leaves the fist on the thumb side, at right angles to the fingers; the crescent bows back over the wrist
            var fingers = mid != null ? (mid.position - h.position).normalized : h.up;
            var along = th != null ? Vector3.ProjectOnPlane(th.position - h.position, fingers).normalized : h.forward;
            if (along.sqrMagnitude < 0.01f) along = h.forward;
            var root = new GameObject(left ? "ArcBlade_L" : "ArcBlade_R").transform; root.SetParent(h, false);
            root.position = h.position + fingers * 0.07f;
            root.rotation = Quaternion.LookRotation(along, -fingers);
            // the light ribbon (in root space: grows along +z, bows toward -y = back over the wrist)
            var lg = new GameObject("Light").AddComponent<MeshFilter>(); lg.transform.SetParent(root, false);
            lg.sharedMesh = Crescent(0.78f, 0.06f, 0.006f, 64f);
            var lr = lg.gameObject.AddComponent<MeshRenderer>(); lr.sharedMaterial = _bladeMat; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lights.Add(lr);
            // a short dark grip guard at the fist
            var gd = new GameObject("Guard").AddComponent<MeshFilter>(); gd.transform.SetParent(root, false);
            gd.sharedMesh = Crescent(0.14f, 0.03f, 0.02f, 40f); gd.gameObject.AddComponent<MeshRenderer>().sharedMaterial = _hiltMat;
            gd.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            return root;
        }

        /// <summary>A crescent ribbon: from the root along +z, bowing toward -y; width tapers from w0 to w1.</summary>
        static Mesh Crescent(float len, float w0, float w1, float bowDeg)
        {
            int n = 20; var v = new List<Vector3>(); var t = new List<int>();
            float r = len / (bowDeg * Mathf.Deg2Rad);
            for (int i = 0; i <= n; i++)
            {
                float u = i / (float)n, a = u * bowDeg * Mathf.Deg2Rad;
                var c = new Vector3(0f, -(r - r * Mathf.Cos(a)), r * Mathf.Sin(a));
                var nrm = new Vector3(0f, Mathf.Cos(a), Mathf.Sin(a) * -1f);       // across the ribbon, in the bow plane
                float w = Mathf.Lerp(w0, w1, u) * (u < 0.08f ? Mathf.Lerp(0.5f, 1f, u / 0.08f) : 1f);
                v.Add(c + nrm * w * 0.5f + Vector3.right * 0.004f); v.Add(c - nrm * w * 0.5f + Vector3.right * 0.004f);
                v.Add(c + nrm * w * 0.5f - Vector3.right * 0.004f); v.Add(c - nrm * w * 0.5f - Vector3.right * 0.004f);
                if (i > 0) { int b = i * 4, p = b - 4; t.AddRange(new[] { p, b, p + 1, p + 1, b, b + 1, p + 2, p + 3, b + 2, p + 3, b + 3, b + 2 }); }
            }
            var m = new Mesh { name = "ArcBladeCrescent" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        void ApplyBladeVisual(float grow)
        {
            bool show = BladesOwned && grow > 0.01f;
            if (_bladeR != null) _bladeR.gameObject.SetActive(BladesOwned && BladesDrawn || grow > 0.01f);
            if (_bladeL != null) _bladeL.gameObject.SetActive(BladesOwned && BladesDrawn || grow > 0.01f);
            if (_hiltBack != null) _hiltBack.gameObject.SetActive(BladesOwned && !BladesDrawn && grow < 0.5f);
            if (_bladeLight != null)
                for (int bi = 0; bi < _bladeLight.Length; bi++)
                {
                    var r = _bladeLight[bi]; if (r == null) continue;
                    r.enabled = show;
                    float flick = 0.88f + 0.12f * Mathf.PerlinNoise(Time.time * 23f, bi * 3.7f);
                    float sk = ShrinkOf(r.transform.parent); if (sk < 0.99f) BladePullIns++;
                    r.transform.localScale = new Vector3(1f, 1f + 0.06f * (flick - 0.9f), Mathf.Max(0.001f, grow) * flick * sk);
                }
        }

        /// <summary>Per frame (LateUpdate of 7): the blades grow / shrink, flicker, hum.</summary>
        void BladesLateUpdate()
        {
            if (!BladesOwned && _bladeGrow <= 0f) { if (_hiltBack != null && _hiltBack.gameObject.activeSelf) _hiltBack.gameObject.SetActive(false); return; }
            EnsureBlades();
            if (AutoBlades) { AutoBlades = false; DrawBlades(!BladesDrawn); }
            _bladeGrow = Mathf.MoveTowards(_bladeGrow, BladesDrawn ? 1f : 0f, Time.deltaTime / 0.25f);
            BladesHoldOut(Time.deltaTime);
            ApplyBladeVisual(_bladeGrow);
            BladeBodyCheck();
            if (BladesDrawn && _bladeHum == null) _bladeHum = AudioKit.Loop("laser_hum", transform, null, 0.12f, AudioKit.Bus.Sfx, 2f, 12f);
            if (!BladesDrawn && _bladeHum != null) { StartCoroutine(AudioKit.Fade(_bladeHum, 0f, 0.3f, true)); _bladeHum = null; }
        }

        // ---------------------------------------------------------------- QF21 (9): held out and away from her body
        /// <summary>QF21 (9) - Dan: "the blades sit curved into her body when she isn't using them". Drawn but not striking
        /// (idle, walking, running, between strikes) her arms come slightly out from her sides and each blade angles OUTWARD
        /// and away: tips down and out, the crescent's curve bowing away from her - never into her legs, hips or torso. A
        /// strike uses the hand-held angle (the swing is the point). Sheathed: the hilts on her back, clear of her.</summary>
        float _bladeOut; static readonly float ArmOut = 14f;
        bool BladeStriking => (Current == Mode.Gesture) || (Current == Mode.Fight && Act != FightAct.Idle && Act != FightAct.Block);
        void BladesHoldOut(float dt)
        {
            if (_bladeR == null || _bladeL == null) return;
            float want = BladesDrawn && !BladeStriking ? 1f : 0f;
            _bladeOut = Mathf.MoveTowards(_bladeOut, want, dt / 0.15f);
            if (_bladeOut <= 0.001f && !_bladeHeldOut) return;
            _bladeHeldOut = _bladeOut > 0.001f;
            var fwd = transform.forward; var right = transform.right;
            // the arms a little out from her sides (abduction about her forward axis), then the blades angled out and down
            var ua = Bone(HumanBodyBones.RightUpperArm); var la = Bone(HumanBodyBones.LeftUpperArm);
            // in the fight's guard the fists sit at her chin: with the blades drawn the guard opens wider (fists out to the sides)
            float armOut = (Current == Mode.Fight ? 34f : ArmOut) * _bladeOut;
            if (ua != null) ua.rotation = Quaternion.AngleAxis(armOut, fwd) * ua.rotation;
            if (la != null) la.rotation = Quaternion.AngleAxis(-armOut, fwd) * la.rotation;
            if (Current == Mode.Fight)
            {
                var lr = Bone(HumanBodyBones.RightLowerArm); var ll = Bone(HumanBodyBones.LeftLowerArm);
                if (lr != null) lr.rotation = Quaternion.AngleAxis(20f * _bladeOut, fwd) * lr.rotation;
                if (ll != null) ll.rotation = Quaternion.AngleAxis(-20f * _bladeOut, fwd) * ll.rotation;
            }
            foreach (var (blade, side) in new[] { (_bladeR, 1f), (_bladeL, -1f) })
            {
                var hand = blade.parent; if (hand == null) continue;
                if (!_bladeBase.ContainsKey(blade)) _bladeBase[blade] = (blade.localPosition, blade.localRotation);
                var (lp, lr) = _bladeBase[blade];
                blade.localPosition = lp; blade.localRotation = lr;
                var outward = right * side;
                var baseRot = blade.rotation; var basePos = blade.position;
                // the tip down and out (on foot); in the fight's guard the hands are high, so out and forward-down first.
                // The first angle that clears her body by 4 cm wins (checked against her bone capsules this frame).
                var tries = Current == Mode.Fight
                    ? new[] { new Vector3(0.75f, -0.35f, 0.55f), new Vector3(0.9f, -0.2f, 0.4f), new Vector3(0.6f, -0.1f, 0.8f), new Vector3(1f, 0f, 0.1f), new Vector3(0.8f, 0.3f, 0.5f), new Vector3(0.7f, -0.5f, -0.3f), new Vector3(1f, 0.2f, -0.2f) }
                    : new[] { new Vector3(0.58f, -0.8f, 0.15f), new Vector3(0.8f, -0.55f, 0.2f), new Vector3(0.95f, -0.25f, 0.2f), new Vector3(0.7f, -0.6f, -0.35f), new Vector3(1f, -0.1f, -0.1f) };
                Vector3 hp = hand.position + outward * 0.08f;
                Quaternion held = baseRot, best = baseRot; float bestGap = -9f;
                for (int ti = 0; ti < tries.Length; ti++)
                {
                    var tv = tries[ti]; var dir = (outward * tv.x + Vector3.up * tv.y + fwd * tv.z).normalized;
                    var bowAway = Vector3.ProjectOnPlane(outward * 0.8f + fwd * 0.2f, dir).normalized;   // the curve bows away from her
                    held = Quaternion.LookRotation(dir, -bowAway);                                     // ribbon: +z along, bows toward -y
                    blade.SetPositionAndRotation(hp, held);
                    float g = BladeGap(blade);
                    if (g > bestGap) { bestGap = g; best = held; }
                    if (g > 0.04f) break;
                }
                held = best;
                blade.rotation = Quaternion.Slerp(baseRot, held, _bladeOut);
                blade.position = Vector3.Lerp(basePos, hp, _bladeOut);
                // still too close (a fast stance change, the ease out of a strike): the light ribbon pulls in for that moment
                {
                    float k = 1f; while (k > 0.12f && BladeGap(blade, k) < 0.03f) k -= 0.11f;
                    for (int push = 0; push < 4 && BladeGap(blade, k) < 0.03f; push++) blade.position += outward * 0.05f;   // the hilt end: out from her thigh
                    _shrink[blade] = k;
                }
            }
        }
        bool _bladeHeldOut;
        readonly Dictionary<Transform, float> _shrink = new Dictionary<Transform, float>();
        float ShrinkOf(Transform blade) => blade != null && BladesDrawn && !BladeStriking && _shrink.TryGetValue(blade, out var k) ? k : 1f;
        public static int BladePullIns;
        readonly Dictionary<Transform, (Vector3, Quaternion)> _bladeBase = new Dictionary<Transform, (Vector3, Quaternion)>();

        // the per-frame check: blade vs her body (capsules on her bones), by state
        public static int BladeFrames;
        static readonly string[] SegName = { "hips", "waist", "chest", "left thigh", "right thigh", "left shin", "right shin" };
        static readonly Dictionary<string, int> _bladeTouch = new Dictionary<string, int>();
        static readonly Dictionary<string, (int frames, int inside, float minGap)> _bladeStats = new Dictionary<string, (int, int, float)>();
        Vector3 _bladeLastPos;
        /// <summary>The smallest distance (m) from one blade's ribbon to her body capsules (negative = inside).</summary>
        float BladeGap(Transform blade, float k = 1f)
        {
            var segs = BodyCapsules(); float gap = 9f;
            float len = 0.78f, bow = 64f * Mathf.Deg2Rad, rr = len / bow;
            for (int i = 3; i <= 12; i += 1)
            {
                float a = i / 12f * bow; var p = blade.TransformPoint(new Vector3(0f, -(rr - rr * Mathf.Cos(a)), rr * Mathf.Sin(a) * k));
                foreach (var (sa, sb, r) in segs) { var ab = sb - sa; float u = Mathf.Clamp01(Vector3.Dot(p - sa, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); gap = Mathf.Min(gap, (p - (sa + ab * u)).magnitude - r); }
            }
            return gap;
        }
        readonly List<(Vector3, Vector3, float)> _caps = new List<(Vector3, Vector3, float)>();
        List<(Vector3 a, Vector3 b, float r)> BodyCapsules()
        {
            var segs = new List<(Vector3 a, Vector3 b, float r)>();
            void Seg(HumanBodyBones a, HumanBodyBones b, float r) { var ta = Bone(a); var tb = Bone(b); if (ta != null && tb != null) segs.Add((ta.position, tb.position, r)); }
            Seg(HumanBodyBones.Hips, HumanBodyBones.Spine, 0.15f); Seg(HumanBodyBones.Spine, HumanBodyBones.Chest, 0.14f); Seg(HumanBodyBones.Chest, HumanBodyBones.Neck, 0.13f);
            Seg(HumanBodyBones.Neck, HumanBodyBones.Head, 0.11f);
            Seg(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, 0.085f); Seg(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, 0.085f);
            Seg(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, 0.065f); Seg(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, 0.065f);
            return segs;
        }
        void BladeBodyCheck()
        {
            if (!BladesDrawn || _bladeGrow < 0.95f || _bladeR == null || Anim == null || !Anim.isHuman) return;
            string state;
            float sp = Time.deltaTime > 0f ? new Vector2(transform.position.x - _bladeLastPos.x, transform.position.z - _bladeLastPos.z).magnitude / Time.deltaTime : 0f;
            _bladeLastPos = transform.position;
            if (BladeStriking) return;
            if (Current == Mode.Fight) state = "between strikes";
            else if (Current != Mode.Free) return;
            else state = sp < 0.25f ? "idle" : sp < 3.2f ? "walk" : "run";
            // her body as capsules
            var segs = new List<(Vector3 a, Vector3 b, float r)>();
            void Seg(HumanBodyBones a, HumanBodyBones b, float r) { var ta = Bone(a); var tb = Bone(b); if (ta != null && tb != null) segs.Add((ta.position, tb.position, r)); }
            Seg(HumanBodyBones.Hips, HumanBodyBones.Spine, 0.15f); Seg(HumanBodyBones.Spine, HumanBodyBones.Chest, 0.14f); Seg(HumanBodyBones.Chest, HumanBodyBones.Neck, 0.13f);
            Seg(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, 0.085f); Seg(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, 0.085f);
            Seg(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, 0.065f); Seg(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, 0.065f);
            float gap = 9f; int worstSeg = -1;
            foreach (var blade in new[] { _bladeR, _bladeL })
            {
                if (blade == null) continue;
                float len = 0.78f, bow = 64f * Mathf.Deg2Rad, rr = len / bow;
                for (int i = 3; i <= 12; i++)      // from a quarter along: the first part is the hilt in her fist
                {
                    float a = i / 12f * bow;
                    var p = blade.TransformPoint(new Vector3(0f, -(rr - rr * Mathf.Cos(a)), rr * Mathf.Sin(a) * ShrinkOf(blade)));
                    for (int si = 0; si < segs.Count; si++)
                    {
                        var (sa, sb, r) = segs[si];
                        var ab = sb - sa; float u = Mathf.Clamp01(Vector3.Dot(p - sa, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                        float g = (p - (sa + ab * u)).magnitude - r; if (g < gap) { gap = g; worstSeg = si; }
                    }
                }
            }
            if (gap < 0f && worstSeg >= 0) { _bladeTouch.TryGetValue(SegName[Mathf.Min(worstSeg, SegName.Length - 1)], out var n); _bladeTouch[SegName[Mathf.Min(worstSeg, SegName.Length - 1)]] = n + 1; }
            BladeFrames++;
            _bladeStats.TryGetValue(state, out var st);
            if (st.frames == 0) st.minGap = 9f;
            st.frames++; if (gap < 0f) st.inside++; st.minGap = Mathf.Min(st.minGap, gap);
            _bladeStats[state] = st;
        }
        public static string BladeSummary()
        {
            var parts = new List<string>(); int inside = 0;
            foreach (var kv in _bladeStats) { parts.Add($"{kv.Key} {kv.Value.frames} frames, inside her {kv.Value.inside}, smallest gap {kv.Value.minGap * 100f:0.0} cm"); inside += kv.Value.inside; }
            var touch = new List<string>(); foreach (var kv in _bladeTouch) touch.Add($"{kv.Key} {kv.Value}");
            return $"BLADE vs BODY (QF21 9): {string.Join("; ", parts)} - frames with a blade inside her body {inside} (must be 0){(touch.Count > 0 ? "; touched: " + string.Join(", ", touch) : "")}; the light pulled in to stay clear in {BladePullIns} blade-frames";
        }

        // ---------------------------------------------------------------- the blade chain in a boss fight (X while drawn)
        static readonly string[] BladeSet = { "blade_slash_in", "blade_slash_out_mirror", "blade_slash_in_mirror", "blade_slash_out", "blade_combo" };
        public static bool IsBlade(string s) => s != null && s.StartsWith("blade");
        int _bladeN;
        public int BladeBlows { get; private set; }
        bool BladeKit => BladesDrawn && FightHop && HasState("blade_slash_in");
        string PickBladeBlow(bool chained) { if (!chained) { _bladeN = 0; } BladeBlows++; var b = BladeSet[_bladeN++ % BladeSet.Length]; return HasState(b) ? b : "blade_slash_in"; }
    }
}
