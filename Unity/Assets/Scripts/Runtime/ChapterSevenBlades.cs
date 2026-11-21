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
                    r.transform.localScale = new Vector3(1f, 1f + 0.06f * (flick - 0.9f), Mathf.Max(0.001f, grow) * flick);
                }
        }

        /// <summary>Per frame (LateUpdate of 7): the blades grow / shrink, flicker, hum.</summary>
        void BladesLateUpdate()
        {
            if (!BladesOwned && _bladeGrow <= 0f) { if (_hiltBack != null && _hiltBack.gameObject.activeSelf) _hiltBack.gameObject.SetActive(false); return; }
            EnsureBlades();
            if (AutoBlades) { AutoBlades = false; DrawBlades(!BladesDrawn); }
            _bladeGrow = Mathf.MoveTowards(_bladeGrow, BladesDrawn ? 1f : 0f, Time.deltaTime / 0.25f);
            ApplyBladeVisual(_bladeGrow);
            if (BladesDrawn && _bladeHum == null) _bladeHum = AudioKit.Loop("laser_hum", transform, null, 0.12f, AudioKit.Bus.Sfx, 2f, 12f);
            if (!BladesDrawn && _bladeHum != null) { StartCoroutine(AudioKit.Fade(_bladeHum, 0f, 0.3f, true)); _bladeHum = null; }
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
