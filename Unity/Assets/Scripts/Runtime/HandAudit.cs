using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// QF38 (1) — Dan: "7's hands are still often awkward or jittery, or she gets stuck in an animation" (a repeat of QF28 5,
    /// QF30 3 / 13, QF34 1, QF34b, P2 8: each of those fixed ONE moment and measured only that moment). This probe watches her
    /// arms in every scene of every check run, whoever drives them:
    /// <list type="bullet">
    /// <item><b>owners</b>: the drivers of each arm this frame - the table IK (touches, holds, the console, petting, the egg chair,
    /// the galley), a HoldPose cup, the bike's grips (ChapterSevenRide), the ledge hang, the arc blades. Two at once on one arm =
    /// a <b>conflict</b> (the rest pose + the IK and FurPalm's palm pass are designed partners, not counted).</item>
    /// <item><b>jitter</b>: her hands in her own frame (locomotion and cuts that re-place her taken out): a burst of
    /// back-and-forth reversals over 30 m/s² (3 in 0.6 s), or an IK-driven hand moving faster than 4 m/s.</item>
    /// <item><b>stuck</b>: with the player in control (on foot / in a fight, not locked, not frozen aboard) for over 2 s, something of a
    /// scene still holds her - an IK weight, a lean, a hold, a rest pose, the touch / gesture mode; or she is LOCKED while the
    /// player has control (4 s); or an IK / pose stays on for over 90 s in a chapter level (longer than any of its scenes).</item>
    /// </list>
    /// Each onset is logged (<c>HAND AUDIT (QF38 1): ...</c>, the first 40 per run in full), each scene ends with
    /// <c>HAND AUDIT (QF38 1) &lt;scene&gt;: ...</c>, and <see cref="Summary"/> gives the run's total line
    /// (<c>HAND AUDIT (QF38 1) SUMMARY: conflicts N, jitter N, stuck N ...</c>).
    /// </summary>
    public class HandAudit : MonoBehaviour
    {
        public static int Conflicts, Jitter, Stuck, Frames, Scenes;
        static int _sc, _sj, _ss, _lines;
        static readonly Dictionary<string, int> _by = new Dictionary<string, int>();
        static string _scene;
        static HandAudit _i;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (DemoBoot.CaptureDir == null || _i != null) return;
            var go = new GameObject("HandAudit"); DontDestroyOnLoad(go); _i = go.AddComponent<HandAudit>();
            _scene = SceneManager.GetActiveScene().name;
            SceneManager.sceneLoaded += (s, m) => { SceneEnd(); _scene = s.name; if (_i != null) _i.ResetTracking(); VisionTouch.Open = null; };
            Application.quitting += () => { SceneEnd(); Log(Summary()); };
        }
        static void Log(string s) { if (ChapterCapture.I != null) ChapterCapture.I.Log(s); else Debug.Log("[Seven] " + s); }
        static void Note(string kind, string what)
        {
            string key = kind + ": " + what; _by[key] = _by.TryGetValue(key, out var n) ? n + 1 : 1;
            if (_lines++ < 40) Log($"HAND AUDIT (QF38 1): {_scene}: {kind} - {what}");
        }
        static void SceneEnd()
        {
            if (string.IsNullOrEmpty(_scene)) return;
            if (VisionTouch.Open != null) { Stuck++; _ss++; Note("stuck", $"the {VisionTouch.Open} vision touch was never finished (VisionTouch.Finish) before the scene ended"); VisionTouch.Open = null; }
            Scenes++;
            Log($"HAND AUDIT (QF38 1) {_scene}: conflicts {_sc}, jitter {_sj}, stuck {_ss}; control-back releases so far {SevenTableIK.AutoReleases}");
            _sc = _sj = _ss = 0;
        }
        /// <summary>The run's line (ChapterCapture.Finish logs it; also on quit).</summary>
        public static string Summary()
        {
            var top = new List<string>(); foreach (var kv in _by) top.Add($"{kv.Key} x{kv.Value}");
            if (top.Count > 12) top = top.GetRange(0, 12);
            return $"HAND AUDIT (QF38 1) SUMMARY: conflicts {Conflicts}, jitter {Jitter}, stuck {Stuck} (all must be 0); {Frames} frames watched over {Scenes} scenes; control-back releases {SevenTableIK.AutoReleases} (a scene ended without releasing - each one named above), target jumps eased {SevenTableIK.TargetJumps}, vision touches {VisionTouch.Opened} opened / {VisionTouch.Finished} finished{(top.Count > 0 ? " - " + string.Join("; ", top) : "")}";
        }

        // ------------------------------------------------------------------ per frame
        Transform _root; Vector3 _lastRoot; Quaternion _lastRootR;
        readonly Vector3[] _p = new Vector3[2], _v = new Vector3[2]; readonly bool[] _have = new bool[2];
        readonly List<float>[] _rev = { new List<float>(), new List<float>() };
        float _cool, _settle;
        readonly string[] _conflictKeys = new string[2]; float _stuckFor, _lockedFor, _heldFor; bool _stuckLogged, _lockedLogged, _heldLogged;
        SaucerSeven _ss7; float _findT;

        void ResetTracking() { _root = null; _have[0] = _have[1] = false; _rev[0].Clear(); _rev[1].Clear(); _conflictKeys[0] = _conflictKeys[1] = null; _stuckFor = _lockedFor = _heldFor = 0f; _stuckLogged = _lockedLogged = _heldLogged = false; _settle = 0.5f; }

        void LateUpdate()
        {
            float dt = Time.deltaTime; if (dt <= 0f || Time.timeScale <= 0f) return;
            var cs = ChapterSeven.Live;
            if (cs == null && Time.time > _findT) { _findT = Time.time + 1f; if (_ss7 == null) _ss7 = FindFirstObjectByType<SaucerSeven>(); }
            FighterBase f = cs != null ? cs : (FighterBase)_ss7;
            if (f == null || !f.isActiveAndEnabled) return;
            var an = f.GetComponentInChildren<Animator>(); if (an == null || !an.isHuman) return;
            Frames++;
            var ik = f.GetComponent<SevenTableIK>(); var hp = f.GetComponent<HoldPose>(); var rp = f.GetComponent<SevenRestPose>();
            bool ikOn = ik != null && ik.isActiveAndEnabled;

            // ---- (a) owners per arm
            for (int side = 0; side < 2; side++)
            {
                bool right = side == 0; var d = new List<string>();
                if (ikOn && (right ? ik.RightNow : ik.LeftNow) > 0.05f && !(rp != null && rp.Posed && !ik.Holds(right))) d.Add("table IK");
                if (hp != null && hp.Weight > 0.05f && (right ? hp.Right : hp.Left)) d.Add("HoldPose");
                if (cs != null && cs.Riding) d.Add("the bike grips");
                if (cs != null && (cs.Current == ChapterSeven.Mode.Hang || cs.Current == ChapterSeven.Mode.Climb)) d.Add("the ledge");
                if (cs != null && cs.BladesDrawn) d.Add("the arc blades");
                if (d.Count > 1)
                {
                    string key = (right ? "right arm: " : "left arm: ") + string.Join(" + ", d);
                    if (key != _conflictKeys[side]) { _conflictKeys[side] = key; Conflicts++; _sc++; Note("conflict", $"{key} at once (mode {(cs != null ? cs.Current.ToString() : "aboard")}, state '{f.State}')"); }
                }
                else _conflictKeys[side] = null;      // (per arm: one key for both arms re-counted every frame - rc2's first run)
            }

            // ---- (b) jitter: hands in her own frame
            var root = f.transform;
            bool teleport = _root != root || (root.position - _lastRoot).magnitude > 0.5f || Quaternion.Angle(root.rotation, _lastRootR) > 30f;
            float rootSpeed = _root == root ? new Vector3(root.position.x - _lastRoot.x, 0f, root.position.z - _lastRoot.z).magnitude / dt : 0f;
            _root = root; _lastRoot = root.position; _lastRootR = root.rotation;
            if (teleport) { _settle = 0.3f; _have[0] = _have[1] = false; }
            _settle -= dt; _cool -= dt;
            bool moving = rootSpeed > 0.5f || (cs != null && (cs.Current == ChapterSeven.Mode.Fight || cs.Riding || cs.Current == ChapterSeven.Mode.Air || cs.Current == ChapterSeven.Mode.Land || cs.Current == ChapterSeven.Mode.Hang || cs.Current == ChapterSeven.Mode.Climb || cs.BladesDrawn));
            for (int side = 0; side < 2; side++)
            {
                var hb = an.GetBoneTransform(side == 0 ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand); if (hb == null) continue;
                var p = root.InverseTransformPoint(hb.position);
                if (!_have[side] || _settle > 0f || moving) { _p[side] = p; _v[side] = Vector3.zero; _have[side] = true; _rev[side].Clear(); continue; }
                var v = (p - _p[side]) / dt; var a = (v - _v[side]) / dt;
                bool driven = ikOn && (side == 0 ? ik.RightNow : ik.LeftNow) > 0.3f || hp != null && hp.Weight > 0.3f;
                if (v.magnitude > 0.25f && _v[side].magnitude > 0.25f && Vector3.Dot(v, _v[side]) < 0f && a.magnitude > 30f) _rev[side].Add(Time.time);
                _rev[side].RemoveAll(t => Time.time - t > 0.6f);
                string why = null;
                if (_rev[side].Count >= 3) why = $"{_rev[side].Count} back-and-forth reversals in 0.6 s (accel {a.magnitude:0} m/s^2)";
                else if (driven && v.magnitude > 4f) why = $"an IK-driven hand moving {v.magnitude:0.0} m/s";
                if (why != null && _cool <= 0f)
                {
                    _cool = 2f; Jitter++; _sj++; _rev[side].Clear();
                    Note("jitter", $"{(side == 0 ? "right" : "left")} hand: {why} (mode {(cs != null ? cs.Current.ToString() : "aboard")}, state '{f.State}', IK {(ikOn ? (side == 0 ? ik.RightNow : ik.LeftNow) : 0f):0.00}, hold {(hp != null ? hp.Weight : 0f):0.00})");
                }
                _p[side] = p; _v[side] = v;
            }

            // ---- (c) stuck
            bool free = ik != null ? ik.PlayerFree() : PlayerFreeNoIk(cs, _ss7);
            var held = new List<string>();
            if (ikOn && Mathf.Max(ik.RightNow, ik.LeftNow) > 0.05f) held.Add($"hand IK {Mathf.Max(ik.RightNow, ik.LeftNow):0.00}");
            if (ikOn && ik.LeanNow > 1f) held.Add($"a lean of {ik.LeanNow:0} deg");
            if (ikOn && !ik.Alive) held.Add("head life off");
            if (hp != null && hp.Weight > 0.05f) held.Add($"a hold {hp.Weight:0.00}");
            if (rp != null && rp.Posed) held.Add($"the {rp.Pose} pose");
            if (cs != null && (cs.Current == ChapterSeven.Mode.Touch || cs.Current == ChapterSeven.Mode.Gesture || cs.Current == ChapterSeven.Mode.Look)) held.Add($"the {cs.Current} mode ('{cs.State}')");
            if (VisionTouch.Open != null) held.Add($"the {VisionTouch.Open} vision touch never finished (VisionTouch.Finish)");
            if (free && held.Count > 0) { _stuckFor += dt; if (_stuckFor > 2f && !_stuckLogged) { _stuckLogged = true; Stuck++; _ss++; Note("stuck", $"control for 2 s and still held: {string.Join(", ", held)} - 7 at {root.position}"); } }
            else { _stuckFor = 0f; _stuckLogged = false; }
            bool control = false; try { control = MusicSystem.HasControl != null && MusicSystem.HasControl(); } catch { }
            if (control && cs != null && cs.Current == ChapterSeven.Mode.Locked) { _lockedFor += dt; if (_lockedFor > 4f && !_lockedLogged) { _lockedLogged = true; Stuck++; _ss++; Note("stuck", $"locked for 4 s while the player has control (state '{cs.State}') - 7 at {root.position}"); } }
            else { _lockedFor = 0f; _lockedLogged = false; }
            bool chapterLevel = _scene != "Saucer" && _scene != "Menu" && cs != null;
            bool longHeld = chapterLevel && (ikOn && Mathf.Max(ik.RightNow, ik.LeftNow, ik.LeanNow / 30f) > 0.05f || rp != null && rp.Posed || hp != null && hp.Weight > 0.05f);
            if (longHeld) { _heldFor += dt; if (_heldFor > 90f && !_heldLogged) { _heldLogged = true; Stuck++; _ss++; Note("stuck", $"held for 90 s without a break (longer than any scene): {string.Join(", ", held)}"); } }
            else { _heldFor = 0f; _heldLogged = false; }
        }
        static bool PlayerFreeNoIk(ChapterSeven cs, SaucerSeven ss)
        {
            bool control = false; try { control = MusicSystem.HasControl != null && MusicSystem.HasControl(); } catch { }
            if (!control) return false;
            if (cs != null) return cs.Current == ChapterSeven.Mode.Free || cs.Current == ChapterSeven.Mode.Fight;
            return ss != null && ss.Current == SaucerSeven.Mode.Free && !ss.Frozen && !ss.Cutscene;
        }
    }
}
