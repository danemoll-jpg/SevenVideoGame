using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// QF30 — the check-run checks Dan's review asked for, in every chapter (logged in capture_log.txt, summarised at the end):
    /// <list type="bullet">
    /// <item>HAND IK LEFT ON (QF30 3): a hand still driven by an IK target (SevenTableIK / HoldPose) 1 s after control came back.</item>
    /// <item>BODY vs PROPS (QF30 4): at every capture, 7's and Panther's limbs (hips to head / hands / knees / feet; his spine to head,
    /// paws, tail) must not pass through solid geometry (the platform's base, a control, a prop, the rig).</item>
    /// <item>NO NEXT STEP (QF30 17): control for over 15 s with no objective and no declared free moment.</item>
    /// <item>AUDIO CARRY (QF30 22): a sound still playing from the previous place after a scene change.</item>
    /// <item>OBJECTIVE VIEW (QF30 9): each new objective - is its marker on screen from where it is given?</item>
    /// </list>
    /// </summary>
    public class QF30Watch : MonoBehaviour
    {
        static QF30Watch _i;
        public static int HandLeftOn, BodyHits, BodyShots, NoNextStep, AudioCarry, ObjViews, ObjOnScreen;
        public static readonly List<string> Notes = new List<string>();
        static string _free;
        /// <summary>A moment deliberately without an objective (free exploration, the scripted silence of FIN_10).</summary>
        public static void Free(string why) { _free = why; }
        public static void EndFree() { _free = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (DemoBoot.CaptureDir == null || _i != null) return;
            var go = new GameObject("QF30Watch"); DontDestroyOnLoad(go); _i = go.AddComponent<QF30Watch>();
            SceneManager.sceneLoaded += (s, m) => { if (_i != null) _i.StartCoroutine(_i.CarryCheck(s.name)); _free = null; };
        }
        static void Log(string s) { if (ChapterCapture.I != null) ChapterCapture.I.Log(s); else Debug.Log("[Seven] " + s); }

        // ------------------------------------------------------------------ hands, next step, objective views
        float _handOn, _idle; bool _handLogged, _idleLogged; string _lastObj;
        void Update()
        {
            bool control = false; try { control = MusicSystem.HasControl != null && MusicSystem.HasControl(); } catch { }
            var seven = ChapterSeven.Live;
            // (3) a hand left on its target once control is back
            float w = 0f; string which = null;
            if (seven != null)
            {
                var ik = seven.GetComponent<SevenTableIK>(); if (ik != null && ik.isActiveAndEnabled && Mathf.Max(ik.RightNow, ik.LeftNow) > w) { w = Mathf.Max(ik.RightNow, ik.LeftNow); which = ik.RightNow >= ik.LeftNow ? "right hand (table IK)" : "left hand (table IK)"; }
                var hp = seven.GetComponent<HoldPose>(); if (hp != null && hp.Weight > w) { w = hp.Weight; which = "a hold (HoldPose)"; }
            }
            if (control && w > 0.05f && !HoldAllowed) { _handOn += Time.deltaTime; if (_handOn > 1f && !_handLogged) { _handLogged = true; HandLeftOn++; Log($"HAND IK LEFT ON (QF30 3): {SceneManager.GetActiveScene().name}: her {which} still driven (weight {w:0.00}) 1 s after control came back - 7 at {seven.transform.position}"); } }
            else { _handOn = 0f; _handLogged = false; }
            // (17) control with nothing to do
            var ui = ChapterUI.Live; string obj = ui != null ? ui.Objective : null;
            string scene = SceneManager.GetActiveScene().name;
            bool free = _free != null || scene == "Menu" || scene == "Saucer" || scene == "FightDemo" || scene == "FightDemo_Room" || scene == "Ratwing" || seven == null;
            if (control && string.IsNullOrEmpty(obj) && !free) { _idle += Time.deltaTime; if (_idle > 15f && !_idleLogged) { _idleLogged = true; NoNextStep++; Log($"NO NEXT STEP (QF30 17): {scene}: control for 15 s with no objective and no declared free moment - 7 at {(seven != null ? seven.transform.position.ToString() : "-")}"); } }
            else { _idle = 0f; _idleLogged = false; }
            // (9) a new objective: where it is, from where it is given
            if (!string.IsNullOrEmpty(obj) && obj != _lastObj) { _lastObj = obj; StartCoroutine(ObjView(obj)); }
            if (string.IsNullOrEmpty(obj)) _lastObj = null;
        }
        /// <summary>Holds that legitimately stay on while she has control (none in the finale; the Chapter 1 shape is carried by CarriedShape).</summary>
        public static bool HoldAllowed;
        System.Collections.IEnumerator ObjView(string obj)
        {
            for (float t = 0f; t < 1.2f; t += Time.deltaTime) yield return null;
            var ui = ChapterUI.Live; if (ui == null || ui.Objective != obj) yield break;
            ObjViews++; bool on = ui.MarkerState == "on"; if (on) ObjOnScreen++;
            Log($"OBJECTIVE VIEW (QF30 9): '{obj}' - its marker {(ui.MarkerState == "off" ? "not shown (the place is in view / spoken)" : on ? "on screen" : "at the screen's edge, pointing to it")}");
            if (ChapterCapture.I != null && (DemoBoot.ObjViews || SceneManager.GetActiveScene().name == "Finale"))
            {
                var slug = new string(obj.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray()); if (slug.Length > 40) slug = slug.Substring(0, 40);
                ImageGate.Expect(ChapterSeven.Live != null ? ChapterSeven.Live.transform : null, "7 (the player's view as the objective appears)", 0f, "obj_");
                yield return ChapterCapture.I.Shot("obj_" + slug.Trim('_'));
            }
        }

        // ------------------------------------------------------------------ (22) nothing carries over a scene change
        System.Collections.IEnumerator CarryCheck(string scene)
        {
            for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime) yield return null;
            var left = new List<string>();
            foreach (var a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (a == null || !a.isPlaying || a.volume < 0.001f || a.clip == null) continue;
                if (a.gameObject.scene.name != "DontDestroyOnLoad") continue;               // the new scene's own sounds
                if (a.GetComponentInParent<MusicSystem>() != null || a.name.Contains("Music")) continue;   // the music carries by design
                left.Add($"{a.name} ({a.clip.name})");
            }
            if (left.Count > 0) { AudioCarry += left.Count; Log($"AUDIO CARRY (QF30 22): entering {scene}, still playing from the previous place: {string.Join(", ", left)}"); }
        }
        /// <summary>Every sound playing now; anything not on <paramref name="allowed"/> (clip-name prefixes) is reported (FIN_14 + the credits).</summary>
        public static int AudioAudit(string when, params string[] allowed)
        {
            var bad = new List<string>(); var all = new List<string>();
            foreach (var a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (a == null || !a.isPlaying || a.volume < 0.001f || a.clip == null) continue;
                var n = a.clip.name; all.Add(n);
                if (!allowed.Any(p => n.StartsWith(p))) bad.Add($"{n} on {a.name}");
            }
            Log($"AUDIO AUDIT (QF30 22) {when}: playing [{string.Join(", ", all)}]; not on the ending's list: {bad.Count} (must be 0){(bad.Count > 0 ? " - " + string.Join(", ", bad) : "")}");
            return bad.Count;
        }

        // ------------------------------------------------------------------ (4) limbs through solid geometry, at every capture
        static readonly RaycastHit[] _hits = new RaycastHit[32];
        public static void BeforeShot(string name)
        {
            if (!CameraOccluders.Ready) return;
            BodyShots++;
            var seven = ChapterSeven.Live; var found = new List<string>();
            if (seven != null && seven.gameObject.activeInHierarchy && seven.AnimNow != null && seven.AnimNow.isHuman)
            {
                var a = seven.AnimNow; var hips = a.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null)
                    foreach (var b in new[] { HumanBodyBones.Head, HumanBodyBones.LeftHand, HumanBodyBones.RightHand, HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
                    { var t = a.GetBoneTransform(b); if (t != null) Seg("7", b.ToString(), hips.position, t.position, seven.transform, found); }
            }
            foreach (var cat in FindObjectsByType<ChapterCat>(FindObjectsSortMode.None))
            {
                if (!cat.gameObject.activeInHierarchy) continue;
                Transform spine = null; var ends = new List<Transform>();
                foreach (var t in cat.GetComponentsInChildren<Transform>()) { if (t.name == "spine" || t.name == "chest") spine = spine ?? t; if (t.name == "head" || t.name.EndsWith("_foot")) ends.Add(t); }
                if (spine != null) foreach (var e in ends) Seg("Panther", e.name, spine.position, e.position, cat.transform, found);
            }
            if (found.Count > 0) { BodyHits++; Log($"BODY vs PROPS (QF30 4) {name}: {string.Join("; ", found)}"); }
        }
        static void Seg(string who, string bone, Vector3 a, Vector3 b, Transform self, List<string> found)
        {
            var d = b - a; float len = d.magnitude - 0.03f; if (len <= 0.02f) return;
            int n = CameraOccluders.RaycastAll(a, d.normalized, len, _hits, CameraOccluders.SolidMask);
            for (int i = 0; i < n; i++)
            {
                var r = CameraOccluders.RendererOf(_hits[i].collider); var tr = r != null ? r.transform : _hits[i].collider.transform;
                if (tr.IsChildOf(self)) continue;
                found.Add($"{who}'s {bone} passes through {tr.name} ({(len - _hits[i].distance) * 100f:0} cm)"); return;
            }
        }

        public static string Summary() =>
            $"QF30 CHECKS: HAND IK LEFT ON (QF30 3) {HandLeftOn} (must be 0); BODY vs PROPS (QF30 4) {BodyHits} of {BodyShots} captures (must be 0); NO NEXT STEP (QF30 17) {NoNextStep}; AUDIO CARRY (QF30 22) {AudioCarry}; OBJECTIVE VIEW (QF30 9) {ObjViews} objectives, marker on screen at once {ObjOnScreen}; {SevenRestPose.DriftReport()}; posed-body wall push that rc1 applied (never now): {ChapterSeven.PosedPushWanted * 100f:0.0} cm {ChapterSeven.PosedPushBy}";
    }
}
