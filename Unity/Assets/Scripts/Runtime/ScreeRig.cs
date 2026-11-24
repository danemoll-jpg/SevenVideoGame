using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF4: the Scree's non-humanoid bones, moved procedurally after the Animator (Humanoid clips only drive the
    /// human bones). The lower arm pair (ScreeB_*) loosely follows the upper pair and braces forward while working;
    /// the abdomen chain and the antenna sway gently; <see cref="Twitch"/> (alert) makes the antenna flick.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ScreeRig : MonoBehaviour
    {
        [Range(0f, 1f)] public float Follow = 0.45f;
        [System.NonSerialized] public float Brace;       // 0 relaxed .. 1 lower arms forward (work loop)
        [System.NonSerialized] public float Twitch;      // 0 .. 1, decays (alert)

        Transform _chest, _upL, _upR, _bUpL, _bUpR, _bForeL, _bForeR;
        Transform[] _abd = new Transform[4], _ant = new Transform[2];
        Quaternion _relRestL, _relRestR, _bUpRestL, _bUpRestR, _bForeRestL, _bForeRestR;
        Quaternion[] _abdRest = new Quaternion[4], _antRest = new Quaternion[2];
        float _phase, _brace;

        Transform Find(string n)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == n) return t;
            return null;
        }

        void Awake()
        {
            _chest = Find("Chest");
            _upL = Find("LeftUpperArm"); _upR = Find("RightUpperArm");
            _bUpL = Find("ScreeB_Upper_Left"); _bUpR = Find("ScreeB_Upper_Right");
            _bForeL = Find("ScreeB_Fore_Left"); _bForeR = Find("ScreeB_Fore_Right");
            for (int i = 0; i < 4; i++) { _abd[i] = Find("ScreeAbd" + (i + 1)); if (_abd[i] != null) _abdRest[i] = _abd[i].localRotation; }
            for (int i = 0; i < 2; i++) { _ant[i] = Find("ScreeAnt" + (i + 1)); if (_ant[i] != null) _antRest[i] = _ant[i].localRotation; }
            if (_chest != null && _upL != null) _relRestL = Quaternion.Inverse(_chest.rotation) * _upL.rotation;
            if (_chest != null && _upR != null) _relRestR = Quaternion.Inverse(_chest.rotation) * _upR.rotation;
            if (_bUpL != null) _bUpRestL = _bUpL.localRotation;
            if (_bUpR != null) _bUpRestR = _bUpR.localRotation;
            if (_bForeL != null) _bForeRestL = _bForeL.localRotation;
            if (_bForeR != null) _bForeRestR = _bForeR.localRotation;
            _phase = Random.Range(0f, 10f);
        }

        // ------------------------------------------------------------------ P2 (10): calm arms
        // Dan: "The Scree's arms move too much." Their clips are borrowed (7's idle / look-around / alert and their own work loop) -
        // on a four-armed body every swing reads as waving, and the workers kept their work loop running through conversations.
        // Whenever a Scree is idle, looking round, alerted or in a conversation (a line playing within 12 m of it, or its own line)
        // the human arms settle toward a weary hang (shoulders low, forearms a little forward, the chest bowed a few degrees) and
        // what is left of the clip's motion is slowed (a low-pass on the arm bones); while it speaks, at most one small gesture
        // on about half its lines (a forearm lifting ~14 deg and back, ~1.4 s). Working at a station with nobody talking and the
        // fight keep their clips. Check runs log `SCREE ARMS (P2 10)` (arm speed in deg/s while calm vs before).
        Transform _loL, _loR, _hL, _hR, _neck; DialoguePlayer _dlg; ScreeGuard _guard; float _calm; bool _calmInit;
        Quaternion _pUL, _pUR, _pLL, _pLR; VoiceBank.Line _gLine; float _gAt = -1f, _gT; bool _gOn, _gRight;
        public static float CalmArmDegPerSec, RawArmDegPerSec; public static int CalmFrames;
        static float _calmSum, _rawSum;
        /// <summary>True when a conversation (or idle / look-around / alert) has this Scree's arms calmed.</summary>
        public bool Calm => _calm > 0.5f;
        bool WantsCalm(out bool speaking)
        {
            speaking = false;
            if (_guard == null) _guard = GetComponent<ScreeGuard>();
            if (_guard != null && (_guard.InFight || _guard.Engaged)) return false;
            if (_dlg == null) _dlg = FindFirstObjectByType<DialoguePlayer>();
            var line = _dlg != null ? _dlg.Current : null;
            if (line != null)
            {
                var who = DialogueFraming.Speaker != null ? DialogueFraming.Speaker(line.Speaker) : null;
                if (who == transform || (who != null && who.IsChildOf(transform))) { speaking = true; return true; }
                if (who != null && (who.position - transform.position).sqrMagnitude < 144f) return true;
                if (who == null && Camera.main != null && (Camera.main.transform.position - transform.position).sqrMagnitude < 144f) return true;
            }
            var st = _guard != null ? _guard.State : null;
            return st == "idle" || st == "look_around" || st == "alert";
        }
        void CalmArms(float t)
        {
            if (!_calmInit) { _calmInit = true; _loL = Find("LeftLowerArm"); _loR = Find("RightLowerArm"); _hL = Find("LeftHand"); _hR = Find("RightHand"); _neck = Find("Neck"); }
            bool want = WantsCalm(out bool speaking);
            _calm = Mathf.MoveTowards(_calm, want ? 1f : 0f, Time.deltaTime / 0.6f);
            float dt = Mathf.Max(1e-4f, Time.deltaTime);
            float raw = (_upL != null ? Quaternion.Angle(_pUL, _upL.rotation) : 0f) + (_upR != null ? Quaternion.Angle(_pUR, _upR.rotation) : 0f);
            if (_calm <= 0.001f || _chest == null) { Keep(); return; }
            float w = Mathf.SmoothStep(0f, 1f, _calm);
            var fwd = transform.forward; var right = transform.right;
            // the weary posture: the chest bowed a little, the neck lower
            _chest.rotation = Quaternion.AngleAxis(5f * w, right) * _chest.rotation;
            if (_neck != null) _neck.rotation = Quaternion.AngleAxis(4f * w, right) * _neck.rotation;
            // a single small gesture on about half of its own lines
            if (speaking && _dlg.Current != _gLine) { _gLine = _dlg.Current; _gOn = Random.value < 0.5f; _gAt = Time.time + Random.Range(0.4f, 1.2f); _gRight = Random.value < 0.5f; }
            float gest = 0f;
            if (_gOn && _gAt > 0f && Time.time >= _gAt) { float u = (Time.time - _gAt) / 1.4f; gest = u < 1f ? Mathf.Sin(u * Mathf.PI) : 0f; if (u >= 1f) _gOn = false; }
            foreach (var (up, lo, hand, sd) in new[] { (_upL, _loL, _hL, 1f), (_upR, _loR, _hR, -1f) })
            {
                if (up == null || lo == null) continue;
                // the hang: the upper arm down beside the body, slightly forward and out; the forearm bent a little forward
                var upDir = (-Vector3.up * 0.95f + fwd * 0.16f - right * sd * 0.12f).normalized;
                var loDir = (-Vector3.up * 0.72f + fwd * 0.55f - right * sd * 0.05f).normalized;
                Aim(up, lo, upDir, 0.85f * w);
                if (hand != null) Aim(lo, hand, loDir, 0.8f * w);
                bool g = gest > 0f && ((sd < 0f) == _gRight);
                if (g) lo.rotation = Quaternion.AngleAxis(-14f * gest * w, right) * lo.rotation;
            }
            // what is left of the clip's motion is slowed (low-pass ~0.35 s)
            float k = 1f - Mathf.Exp(-dt / 0.35f);
            if (_upL != null) _upL.rotation = Quaternion.Slerp(_pUL, _upL.rotation, Mathf.Lerp(1f, k, w));
            if (_upR != null) _upR.rotation = Quaternion.Slerp(_pUR, _upR.rotation, Mathf.Lerp(1f, k, w));
            if (_loL != null) _loL.rotation = Quaternion.Slerp(_pLL, _loL.rotation, Mathf.Lerp(1f, k, w));
            if (_loR != null) _loR.rotation = Quaternion.Slerp(_pLR, _loR.rotation, Mathf.Lerp(1f, k, w));
            if (DemoBoot.CaptureDir != null && w > 0.95f)
            {
                float calmNow = (_upL != null ? Quaternion.Angle(_pUL, _upL.rotation) : 0f) + (_upR != null ? Quaternion.Angle(_pUR, _upR.rotation) : 0f);
                CalmFrames++; _calmSum += calmNow / dt; _rawSum += raw / dt; CalmArmDegPerSec = _calmSum / CalmFrames; RawArmDegPerSec = _rawSum / CalmFrames;
            }
            Keep();
        }
        void Keep() { if (_upL != null) _pUL = _upL.rotation; if (_upR != null) _pUR = _upR.rotation; if (_loL != null) _pLL = _loL.rotation; if (_loR != null) _pLR = _loR.rotation; }
        static void Aim(Transform bone, Transform child, Vector3 dir, float w)
        {
            var cur = child.position - bone.position; if (cur.sqrMagnitude < 1e-6f || w <= 0f) return;
            bone.rotation = Quaternion.Slerp(bone.rotation, Quaternion.FromToRotation(cur.normalized, dir) * bone.rotation, w);
        }
        public static string Summary() => CalmFrames == 0 ? "SCREE ARMS (P2 10): no calm frames this run" :
            $"SCREE ARMS (P2 10): {CalmFrames} calm frames (idle / talk) - the two upper arms moved {CalmArmDegPerSec:0.0} deg/s on average (the clip alone {RawArmDegPerSec:0.0} deg/s, incl. the settle)";

        void LateUpdate()
        {
            float t = Time.time + _phase;
            CalmArms(t);
            _brace = Mathf.MoveTowards(_brace, Brace, Time.deltaTime * 2f);
            Twitch = Mathf.MoveTowards(Twitch, 0f, Time.deltaTime * 0.8f);
            var right = transform.right; var up = transform.up;
            if (_chest != null)
            {
                Arm(_upL, _bUpL, _bForeL, _relRestL, _bUpRestL, _bForeRestL, t, 1f);
                Arm(_upR, _bUpR, _bForeR, _relRestR, _bUpRestR, _bForeRestR, t + 1.3f, -1f);
            }
            for (int i = 0; i < 4; i++)
            {
                if (_abd[i] == null) continue;
                float k = 0.5f + 0.5f * i;
                _abd[i].localRotation = _abdRest[i];
                _abd[i].rotation = Quaternion.AngleAxis(Mathf.Sin(t * 0.9f + i * 0.6f) * 1.6f * k, right) *
                                   Quaternion.AngleAxis(Mathf.Sin(t * 0.55f + i * 0.8f) * 1.8f * k, up) * _abd[i].rotation;
            }
            float tw = Twitch;
            for (int i = 0; i < 2; i++)
            {
                if (_ant[i] == null) continue;
                _ant[i].localRotation = _antRest[i];
                float a = Mathf.Sin(t * (2.1f + tw * 9f) + i) * (5f + tw * 14f);
                float b = Mathf.Sin(t * (1.3f + tw * 7f) + i * 2f) * (4f + tw * 10f);
                _ant[i].rotation = Quaternion.AngleAxis(a, right) * Quaternion.AngleAxis(b, up) * _ant[i].rotation;
            }
        }

        /// <summary>QF4 check: smallest distance (m) between the tucked abdomen's centre line (its bone chain to the tip)
        /// (from halfway down its first segment) and the leg bones (mid-thigh, knee, ankle, toes), both sides. The abdomen + covers are ~0.15 m thick about the line.</summary>
        public float LegClearance()
        {
            var abd = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i < 4; i++) if (_abd[i] != null) abd.Add(_abd[i].position);
            if (_abd[3] != null && _abd[3].childCount == 0 && abd.Count > 1) abd.Add(abd[abd.Count - 1] + (abd[abd.Count - 1] - abd[abd.Count - 2]));
            // the waist joint itself (where the abdomen is attached between the hips) is not a collision: start halfway
            // down the first segment, and measure the legs from mid-thigh down
            if (abd.Count > 1) abd[0] = Vector3.Lerp(abd[0], abd[1], 0.5f);
            float best = float.MaxValue;
            foreach (var side in new[] { "Left", "Right" })
            {
                var leg = new System.Collections.Generic.List<Vector3>();
                foreach (var n in new[] { "UpperLeg", "LowerLeg", "Foot", "Toes" }) { var t = Find(side + n); if (t != null) leg.Add(t.position); }
                if (leg.Count > 1) leg[0] = Vector3.Lerp(leg[0], leg[1], 0.5f);
                for (int i = 0; i + 1 < abd.Count; i++)
                    for (int j = 0; j + 1 < leg.Count; j++)
                        for (int k = 0; k <= 10; k++)
                        {
                            var pnt = Vector3.Lerp(leg[j], leg[j + 1], k / 10f);
                            best = Mathf.Min(best, DistSeg(pnt, abd[i], abd[i + 1]));
                        }
            }
            return best;
        }

        static float DistSeg(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-8f));
            return (p - (a + ab * t)).magnitude;
        }

        void Arm(Transform upper, Transform bUp, Transform bFore, Quaternion relRest, Quaternion bUpRest, Quaternion bForeRest, float t, float side)
        {
            if (upper == null || bUp == null) return;
            // the upper arm's swing away from its rest, in the chest's frame
            var rel = Quaternion.Inverse(_chest.rotation) * upper.rotation;
            var delta = rel * Quaternion.Inverse(relRest);
            bUp.localRotation = bUpRest;
            var follow = _chest.rotation * Quaternion.Slerp(Quaternion.identity, delta, Follow) * Quaternion.Inverse(_chest.rotation);
            // brace: reach forward and down
            var brace = Quaternion.AngleAxis(-55f * _brace, transform.right) * Quaternion.AngleAxis(25f * _brace * side, transform.forward);
            var sway = Quaternion.AngleAxis(Mathf.Sin(t * 0.8f) * 3f, transform.right);
            bUp.rotation = brace * sway * follow * bUp.rotation;
            if (bFore != null)
            {
                bFore.localRotation = bForeRest;
                bFore.rotation = Quaternion.AngleAxis(-35f * _brace - Mathf.Sin(t * 1.1f) * 2f, transform.right) * bFore.rotation;
            }
        }
    }
}
