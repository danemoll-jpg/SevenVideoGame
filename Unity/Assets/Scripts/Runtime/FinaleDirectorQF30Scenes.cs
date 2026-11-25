using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using L = Seven.FinaleLayout;

namespace Seven
{
    /// <summary>QF30 — Dan's review of 1.0.0-rc1, the ending restaged: FIN_09 (the danger is real: the drill at her back; "Seven!"; he grows
    /// mid-stride and leaps between them; a hard cut to black on the impact, the music dead; back on him already falling, still large),
    /// FIN_10 (he dies clearly and gently in her arms, shrinking as the life leaves him; "Panther, no!"), FIN_11 (she carries him and lays
    /// him on the archive; the Scree take Tunneler; the Archive asks her to continue), FIN_13 (she walks into the light and rises).</summary>
    public partial class FinaleDirector
    {
        Beacon _conduitBeacon, _barrierBeacon;
        AudioSource _purr, _barrierHum;
        bool _handsOn; bool _endingAudio;

        void ChapterSevenOff() { if (Seven.InFight) Seven.EndFight(); }
        void NoHud(bool on)
        {
            Objective(null); SetPrompt(null); HideGuide(); UI.HideBossBar();
            if (Legend != null) Legend.HidePanels = true;
        }
        /// <summary>An animated turn (never a snap): her facing eases to <paramref name="dir"/> over <paramref name="secs"/>.</summary>
        IEnumerator TurnTo(Vector3 dir, float secs = 0.6f)
        {
            dir = Flat(dir); if (dir.sqrMagnitude < 1e-4f) yield break;
            var q0 = Seven.transform.rotation; var q1 = Quaternion.LookRotation(dir);
            float ang = Quaternion.Angle(q0, q1); if (ang < 2f) yield break;
            for (float t = 0f; t < 1f; t += Time.deltaTime / secs) { Seven.transform.rotation = Quaternion.Slerp(q0, q1, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            Seven.transform.rotation = q1;
            Log($"TURN (QF30 14): she turns {ang:0} deg over {secs:0.0} s (animated, never a snap)");
        }
        SevenTableIK IK { get { var ik = Seven.GetComponent<SevenTableIK>() ?? Seven.gameObject.AddComponent<SevenTableIK>(); ik.enabled = true; return ik; } }

        IEnumerator FieldFlash(Renderer r)
        {
            var m = r.material; float k0 = m.HasProperty("_Intensity") ? m.GetFloat("_Intensity") : 0.18f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f) { if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", Mathf.Lerp(0.6f, k0, t)); yield return null; }
        }

        // ------------------------------------------------------------------------------------------ FIN_10: eight more (no HUD, no music)
        IEnumerator EightMore()
        {
            yield return Cutscene(true);
            NoHud(true);
            QF30Watch.Free("FIN_10 - Panther's death and the long silence (scripted)");
            MusicSystem.Silence("FIN_10 - Panther's death, the long hold", 240f);
            if (Cat == null) yield break;
            if (Cat.enabled || Cat.transform.localScale.x < 2f) PantherFallen(true);
            var ov = CatOv; if (ov != null) { ov.Alive = true; ov.Slack = 0f; ov.SettleTo = null; ov.Breath = 0.85f; ov.BreathRate = 0.55f; }
            // she reaches him and kneels at his head, gathering his head against her (he is still large)
            // QF30 (15): she kneels right at his head - measured from his head bone as he lies (rc2 draft: a fixed spot a metre short)
            // QF32 (1): she kneels RIGHT at his head - her knees against it (placed from his drawn fur and her own leg length), so his head
            // can be drawn into her lap and her hands rest on him (rc4: half a metre past his head, her hand hovered ~24 cm above him)
            var kneel = KneelAtHead(out var faceHim);
            Shoot(L.PantherFalls + new Vector3(2.4f, 1.4f, 4.2f), L.PantherFalls + Vector3.up * 0.5f + Vector3.right * 0.6f, 48f, 0.01f);
            Seven.Lock(false); yield return WalkSeven(kneel, 3f); Seven.Lock(true);
            yield return TurnTo(faceHim, 0.5f);
            Seven.Place(kneel, faceHim);
            var rest = SevenRestPose.On(Seven.gameObject, SevenRestPose.Kind.Kneel); rest.EaseIn = true; rest.PinHere(); rest.Bow = 0f; rest.Sob = 0f; rest.Drop = 0.18f;
            _purr = Purr.Loop(Cat.transform, 0.5f);
            _handsOn = true; StartCoroutine(HandsOnHim());
            StartCoroutine(BreathSounds());
            var fwd = Seven.transform.forward; var side = Vector3.Cross(Vector3.up, fwd);
            Shoot(P7 + fwd * 0.55f + side * 2.5f + Vector3.up * 0.9f, P7 + fwd * 0.5f + Vector3.up * 0.4f, 40f, 0.01f);
            ImageGate.Expect(Seven.transform, "7 kneeling at his head, his head against her");
            yield return Wait(1.4f);
            yield return Shot("f44_she_gathers_his_head_against_her");
            yield return Say("FIN_10", 1, 3);                           // Hey. Hey, I'm here. / I know. / We'll get you back to the ship.
            // FIN_10_04: he makes a small effort to press his head into her hand
            if (ov != null) ov.LookAt = HandR();
            Shoot(HandR() + side * 0.75f + fwd * 0.15f + Vector3.up * 0.3f, HandR(), 34f, 0.01f);
            ImageGate.Expect(Cat.transform, "his head pressed into her hand (insert)");
            yield return Wait(1.8f);
            yield return Shot("f45_insert_his_head_into_her_hand");
            Shoot(P7 + fwd * 1.0f + side * 1.3f + Vector3.up * 0.95f, HerHead + Vector3.down * 0.3f, 38f, 0.01f);
            yield return Say("FIN_10", 5, 5);                           // Just stay with me.
            Shoot(HandR() + side * 0.85f + Vector3.up * 0.35f, HandR(), 32f, 0.01f);
            yield return Wait(2.0f);
            yield return Shot("f45b_insert_her_hands_on_his_fur");
            Shoot(P7 + fwd * 0.55f + side * 2.5f + Vector3.up * 0.9f, P7 + fwd * 0.5f + Vector3.up * 0.4f, 40f, 0.01f);
            yield return Say("FIN_10", 6, 6);                           // Don't worry... I have eight more.
            PlayReaction("seven", "laugh_soft");                      // a tiny, involuntary laugh through tears
            yield return Wait(1.0f);
            yield return Say("FIN_10", 8, 8);                           // You'd better.
            if (ov != null) ov.LookAt = null;
            // his breathing slows; as the life leaves him he shrinks, slowly, back to his ordinary size - her arms follow him in
            Shoot(P7 + fwd * 0.75f + side * 1.9f + Vector3.up * 0.75f, P7 + fwd * 0.5f + Vector3.up * 0.3f, 40f, 0.01f);
            var head0 = CatBone("head"); var anchor = head0 != null ? head0.position : Cat.transform.position;
            float sc0 = Cat.transform.localScale.x; float shrink = Auto ? 5f : 8f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / shrink)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                if (ov != null) { ov.BreathRate = Mathf.Lerp(0.55f, 0.2f, u); ov.Breath = Mathf.Lerp(0.85f, 0.35f, u); }
                float s = Mathf.Lerp(sc0, 1f, u), k = s / Cat.transform.localScale.x;
                Cat.transform.position = anchor + (Cat.transform.position - anchor) * k; Cat.transform.localScale = Vector3.one * s;
                var b = CatBounds(); if (b.min.y < L.Y + 0.005f || b.min.y > L.Y + 0.03f) Cat.transform.position += Vector3.up * (L.Y + 0.012f - b.min.y);
                if (_purr != null) _purr.volume = Mathf.Lerp(0.5f, 0.15f, u) * AudioKit.Gain(AudioKit.Bus.Voice);
                if (t > 0.5f && !_shrinkShot)
                {
                    _shrinkShot = true; StartCoroutine(Shot("f45c_he_shrinks_as_the_life_leaves_him"));
                    // QF32 (1): a close-up of her hands following him in as he gets smaller (a second camera; the shot on screen is untouched)
                    if (ChapterCapture.I != null) { ImageGate.ExpectSide("f45d_", Cat.transform, "her hands on him as he shrinks (insert)"); ChapterCapture.I.StartCoroutine(ChapterCapture.I.ShotFrom("f45d_insert_her_hands_follow_him_as_he_shrinks", HandR() + side * 0.8f + Vector3.up * 0.4f, HandR(), 36f)); }
                }
                yield return null;
            }
            Cat.transform.localScale = Vector3.one;
            // QF32 (1): he is small again and has shrunk away from her hands (toward his head) - she draws him in against her knees, his
            // head into her lap (an eased slide; her hands ride on him the whole way)
            {
                var kL = Seven.Bone(HumanBodyBones.LeftLowerLeg); var kR = Seven.Bone(HumanBodyBones.RightLowerLeg); var hd = CatBone("head");
                if (kL != null && kR != null && hd != null)
                {
                    var knees = Flat((kL.position + kR.position) * 0.5f); var want = knees + Flat(Seven.transform.forward).normalized * 0.12f;
                    var delta = want - Flat(hd.position);
                    if (delta.magnitude > 0.03f)
                    {
                        var c0 = Cat.transform.position; var mv = Vector3.ClampMagnitude(delta, 0.6f);
                        for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f) { Cat.transform.position = c0 + mv * Mathf.SmoothStep(0f, 1f, t); yield return null; }
                        Cat.transform.position = c0 + mv;
                        Log($"FIN_10 GATHER (QF32 1): small again, she drew him {mv.magnitude:0.00} m in against her knees (his head into her lap)");
                    }
                    // QF34 (1): his head on the floor in front of her knees was ~0.55-0.6 m below / out from her shoulders - past her arms
                    // even bowed (her right hand hovered ~10 cm over him). She lifts his head and chest up onto her thighs: he pivots about
                    // his hips (hindquarters stay on the floor), eased, his head arriving at her lap point - both hands then rest on him.
                    var hips = CatBone("pelvis"); var hipsB = Seven.Bone(HumanBodyBones.Hips); hd = CatBone("head");
                    kL = Seven.Bone(HumanBodyBones.LeftLowerLeg); kR = Seven.Bone(HumanBodyBones.RightLowerLeg);
                    if (hips != null && hipsB != null && hd != null && kL != null && kR != null)
                    {
                        var lap = Vector3.Lerp(hipsB.position, (kL.position + kR.position) * 0.5f, 0.6f) + Vector3.up * 0.06f;
                        var pivot = hips.position; pivot.y = L.Y;
                        var from = hd.position - pivot; var to = lap - pivot;
                        var q = Quaternion.FromToRotation(from.normalized, to.normalized); q.ToAngleAxis(out float ang, out var ax); if (ang > 180f) ang -= 360f;
                        ang = Mathf.Clamp(ang, -45f, 45f);
                        var p0 = Cat.transform.position; var r0 = Cat.transform.rotation;
                        for (float t = 0f; t < 1f; t += Time.deltaTime / 1.4f)
                        {
                            var qq = Quaternion.AngleAxis(ang * Mathf.SmoothStep(0f, 1f, t), ax);
                            Cat.transform.SetPositionAndRotation(pivot + qq * (p0 - pivot), qq * r0); yield return null;
                        }
                        var qf = Quaternion.AngleAxis(ang, ax); Cat.transform.SetPositionAndRotation(pivot + qf * (p0 - pivot), qf * r0);
                        hd = CatBone("head");
                        _lapHeld = true; IK.OwnsArms = true;   // QF34b: from here the IK owns her arms (eased over 0.8 s)
                        Log($"FIN_10 INTO HER LAP (QF34 1): she lifted his head and chest onto her thighs ({ang:0} deg about his hips, which stay on the floor) - his head {Vector3.Distance(hd.position, lap) * 100f:0} cm from her lap point, {hd.position.y - L.Y:0.00} m up");
                    }
                }
            }
            // one last breath, then stillness: the breathing stops, his head settles into her hand, ears slack, the tail limp; the purr stops
            if (ov != null) { ov.BreathRate = 0.18f; for (float t = 0f; t < 2.4f; t += Time.deltaTime) yield return null; ov.Breath = 0f; ov.Alive = false; ov.Slack = 1f; ov.SettleTo = HandR(); }
            _breathing = false;
            if (_purr != null) { _purr.Stop(); Destroy(_purr.gameObject); _purr = null; }
            Log("FIN_10 (QF30 14/15): his breathing slowed and stopped after one last breath - he shrank to his ordinary size as the life left him; stillness (head settled in her hand, ears slack, tail limp), the purr stopped; total silence");
            Shoot(P7 + fwd * 0.6f + side * 1.6f + Vector3.up * 0.55f, P7 + fwd * 0.45f + Vector3.up * 0.25f, 36f, 0.01f);
            yield return Wait(2.6f);                                    // she waits for another breath. None comes.
            yield return Shot("f46a_stillness_none_comes");
            // "Panther, no!" - broken, disbelieving; she cries a little, her head bowed over him
            rest.Bow = 22f; rest.Sob = 1f;
            Shoot(P7 + fwd * 1.1f + side * 1.0f + Vector3.up * 0.85f, HerHead + Vector3.down * 0.35f, 40f, 0.01f);
            if (ChapterCapture.I != null) { ImageGate.ExpectSide("f46b_", Cat.transform, "her hands still on him at 'Panther, no!' (insert)"); ChapterCapture.I.StartCoroutine(ChapterCapture.I.ShotFrom("f46b_insert_her_hands_on_him_panther_no", HandR() + side * 0.8f + Vector3.up * 0.4f, HandR(), 36f)); }
            yield return SayIds("FIN_10_08b");                          // Panther, no!
            yield return Wait(Auto ? 1.5f : 3f);
            rest.Sob = 0.45f; AudioKit.Play("seven_sob_soft", HerHead, 0.55f, 0.03f, AudioKit.Bus.Voice, 10f);
            // her hand strokes him once, then is still
            _stroke = true; for (float t = 0f; t < 1.6f; t += Time.deltaTime) { _strokeU = Mathf.SmoothStep(0f, 1f, t / 1.6f); yield return null; } _stroke = false;
            Shoot(HandR() + side * 0.85f + Vector3.up * 0.35f, HandR(), 32f, 0.01f);
            yield return Wait(0.6f);
            yield return Shot("f46_her_hand_still_on_his_fur");
            // a substantial silence
            rest.Sob = 0f;
            Shoot(P7 + fwd * 1.6f + side * 3.4f + Vector3.up * 2.0f, P7 + fwd * 0.3f + Vector3.up * 0.4f, 42f, 2.5f);
            yield return Wait(Auto ? 2.5f : 4.5f);                      // QF31 (A3): the silence, then her grief and the memories (Grieve)
            Log("FIN_10: no HUD, no music; 'Panther, no!' and her tears; the silence");
            Complete("FIN_10");
        }
        bool _shrinkShot, _breathing, _stroke; float _strokeU;
        Vector3 HandR() { var a = Seven.AnimNow; var h = a != null ? a.GetBoneTransform(HumanBodyBones.RightHand) : null; return h != null ? h.position : P7 + Vector3.up * 0.5f; }
        /// <summary>FIN_10: her hands rest ON his fur (targets on his head and neck surface every frame - they follow his breathing and the
        /// shrink; palms toward him, fingers relaxed); a stroke runs the right hand from his head to his neck once.</summary>
        float _reachBow;
        /// <summary>QF32 (1): where she kneels so her knees rest against his head: the near side of his drawn head (FurContact) from her
        /// side, plus her own knee reach in the kneeling pose (thigh length x the pose's forward share + the knee cap), 2 cm short.</summary>
        Vector3 KneelAtHead(out Vector3 face)
        {
            var hb = CatBone("head"); var nb = CatBone("neck"); var headP = hb != null ? hb.position : L.PantherHead; var body = Cat != null ? CatBounds().center : L.PantherFalls;
            var away = Flat(headP - body); if (away.sqrMagnitude < 0.01f) away = Vector3.right; away.Normalize();
            // rc5 first pass: in front of his head she knelt at his MUZZLE (it reaches ~0.35 m past the head bone at 2.7x) and his head / neck
            // were beyond her reach. Now she kneels at the BACK of his head and neck (his spine side - he lies on his side), her knees against
            // it: his head, his neck and his shoulder all within her arms' reach.
            var aim = nb != null ? Vector3.Lerp(headP, nb.position, 0.35f) : headP;
            var dorsal = Cat != null ? Flat(Cat.transform.up) : Vector3.zero;
            if (dorsal.sqrMagnitude < 0.04f) dorsal = Vector3.Cross(Vector3.up, away);
            dorsal.Normalize();
            if (Cat != null && FurContact.Nearest(Cat.transform, aim + dorsal * 0.6f, out var bp, out _, out _) && FurContact.Nearest(Cat.transform, aim - dorsal * 0.6f, out var vp, out _, out _)
                && Vector3.Dot(Flat(bp - aim), dorsal) > Vector3.Dot(Flat(aim - vp), dorsal) + 0.05f) dorsal = -dorsal;   // his back is the side whose fur is closer (his legs reach out on the belly side)
            float dNear = 0.12f;
            if (Cat != null && FurContact.Nearest(Cat.transform, aim + dorsal * 0.6f + Vector3.up * 0.05f, out var sp, out _, out _))
                dNear = Mathf.Clamp(Vector3.Dot(Flat(sp - aim), dorsal), 0.03f, 0.35f);
            var ul = Seven.Bone(HumanBodyBones.RightUpperLeg); var ll = Seven.Bone(HumanBodyBones.RightLowerLeg);
            float thigh = ul != null && ll != null ? Vector3.Distance(ul.position, ll.position) : 0.42f;
            float kneeFwd = thigh * 0.94f + 0.06f;
            var kneel = Flat(aim) + dorsal * (dNear + kneeFwd - 0.06f); kneel.y = L.Y;        // her knees pressed 6 cm into his fur (against him)
            face = Flat(aim - kneel);
            Log($"FIN_10 STAGING (QF32 1): she kneels at the back of his head and neck, {Flat(kneel - aim).magnitude:0.00} m from it (his fur {dNear:0.00} m out on her side, her knees {kneeFwd:0.00} m ahead of her) - knees against him (rc4: past his head, out of reach)");
            return kneel;
        }
        int _cradleLogged;
        Transform _bindHead, _bindNeck, _bindChest; Vector3 _hLocal, _hNLocal, _sLocal, _sNLocal, _cLocal, _cNLocal, _hSm, _nSm, _hVel, _nVel; bool _rebind, _smoothInit; float _strokeBack, _offFor, _clipNext, _reachCheck; int _clipN, _bindLogs, _reBinds; Vector3 _palmR, _palmL; bool _ikSet; readonly System.Collections.Generic.List<float> _wristErrR = new System.Collections.Generic.List<float>(), _wristErrL = new System.Collections.Generic.List<float>();
        Vector3 _clipAt, _clipSide; int _clipRuns;
        /// <summary>QF34 (1): check runs - a close-up clip of her hands on him from the gather to "Panther, no!" (10 fps, frames written to
        /// &lt;capture dir&gt;/fin10_clip/, stitched by ffmpeg into Docs/Previews/qf34/fin10_hands.mp4) - a side camera of its own.</summary>
        IEnumerator Fin10Clip()
        {
            if (DemoBoot.CaptureDir == null) yield break;
            var dir = System.IO.Path.Combine(DemoBoot.CaptureDir, _clipRuns++ == 0 ? "fin10_clip" : "fin10_clip_grief"); System.IO.Directory.CreateDirectory(dir);
            var main = Cam.GetComponent<Camera>(); var go = new GameObject("Fin10ClipCam"); var c = go.AddComponent<Camera>(); c.CopyFrom(main); c.fieldOfView = 38f;
            var ud = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); ud.renderPostProcessing = true;
            var rt = new RenderTexture(960, 540, 24); c.targetTexture = rt; var tex = new Texture2D(960, 540, TextureFormat.RGB24, false); int n = 0; float next = 0f;
            while (_handsOn && n < 900)
            {
                if (_clipAt != Vector3.zero) { var p = _clipAt + _clipSide * 0.95f + Vector3.up * 0.6f; go.transform.SetPositionAndRotation(p, Quaternion.LookRotation(_clipAt - p)); }
                yield return new WaitForEndOfFrame();
                if (Time.time < next) continue; next = Time.time + 0.1f;
                var prev = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply(false); RenderTexture.active = prev;
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, $"f{++n:0000}.jpg"), tex.EncodeToJPG(88));
            }
            Destroy(go); rt.Release(); Destroy(tex);
            Log($"FIN_10 CLIP (QF34 1): {n} frames of her hands on him (10 fps) -> fin10_clip/; hand re-binds to the fur within her reach {_reBinds}");
        }
        int _inReachFrames; bool _lapHeld; Vector3 _hRaw, _nRaw, _hNSm, _nNSm;
        Vector3 InReach(Vector3 surf, Vector3 n, Vector3 palm, HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones hand, ref Vector3 nOut)
        {
            var sh = Seven.Bone(upper); var lo = Seven.Bone(lower); var hd = Seven.Bone(hand);
            if (sh == null || lo == null || hd == null || Cat == null) return surf;
            float arm = Vector3.Distance(sh.position, lo.position) + Vector3.Distance(lo.position, hd.position);
            var wrist = surf + n * 0.012f - palm; var d = wrist - sh.position;
            if (d.magnitude <= arm * 0.95f) return surf;
            var p = sh.position + d.normalized * arm * 0.9f + palm;              // where her palm can be, straight toward the point
            if (!FurContact.Nearest(Cat.transform, p, out var sp, out var sn, out _)) return surf;
            _inReachFrames++; nOut = sn; return sp;
        }
        IEnumerator HandsOnHim()
        {
            StartCoroutine(Fin10Clip());
            _reachBow = 0f; _cradleLogged = 0; _lapHeld = false; _hNSm = _nNSm = Vector3.zero; _bindHead = null; _smoothInit = false; _strokeBack = 0f; _offFor = 0f;
            var ik = IK; ik.Ease = 1.4f; ik.Alive = true; ik.Spread = 0.6f;
            while (_handsOn && Cat != null)
            {
                var head = CatBone("head"); var neck = CatBone("neck") ?? head; if (head == null) { yield return null; continue; }
                var her = P7 + Vector3.up * 0.6f; float s = Cat.transform.lossyScale.x;
                var outH = (her - head.position); outH.y = Mathf.Max(outH.y, 0.15f); outH.Normalize(); var up = (outH + Vector3.up).normalized;
                var under = (her - neck.position); under.y = 0f; under.Normalize();
                // P2 (8): the targets are ON his drawn surface (the side of his head / neck that faces her, found on the baked mesh) - the
                // old targets were his bones plus a guessed offset, which put her open hand in the air above his head (rc3 / rc4 captures)
                Vector3 OnFur(Vector3 from, Vector3 dirOut, out Vector3 n)
                {
                    n = dirOut;
                    if (FurContact.Nearest(Cat.transform, from + dirOut * 0.5f * Mathf.Max(1f, s), out var sp, out var sn, out _)) { n = sn; return sp; }
                    return from + dirOut * 0.06f * s;
                }
                // QF32 (1): his head drawn into her lap - his neck / head turn toward her thighs just above her knees (CatPoseOverlay.Cradle);
                // it stays there through his last breaths and the shrink (the point is hers, re-read every frame)
                var ov = CatOv; var hipsB = Seven.Bone(HumanBodyBones.Hips); var kL = Seven.Bone(HumanBodyBones.LeftLowerLeg); var kR = Seven.Bone(HumanBodyBones.RightLowerLeg);
                if (ov != null && hipsB != null && kL != null && kR != null)
                {
                    var knees = (kL.position + kR.position) * 0.5f;
                    ov.Cradle = Vector3.Lerp(hipsB.position, knees, 0.72f) + Vector3.up * 0.07f;
                    if (_cradleLogged < 2 && ov.CradleLift != 0f && Time.frameCount % 60 == 0) { _cradleLogged++; Log($"FIN_10 CRADLE (QF32 1): his head drawn toward her lap - lifted {ov.CradleLift * 100f:0} cm, head {Vector3.Distance(head.position, ov.Cradle.Value):0.00} m from her lap point"); }
                }
                // QF34 (1): the spasms - the targets were the NEAREST BAKED-MESH VERTEX to a guess, re-picked every frame: on a breathing,
                // shrinking body the pick hopped between vertices (cm-sized jumps), FurPalm then re-solved the same arms on top (two solvers),
                // and the stroke's target snapped from his neck back to his head in one frame. Now each hand is BOUND once to a point on his
                // fur in a bone's own space (head / neck for the right hand, the neck->chest for the left): it follows his breathing, the
                // gather and the shrink by construction; the stroke eases there and back; the result is critically damped; FurPalm only
                // measures. (Rebound, slowly, only if a bound point ever drifts > 4 cm off his fur for half a second.)
                // QF34 (1): how close each wrist came to the target it was given last frame (after every pass: IK, the kneel, FurPalm)
                { var rw = Seven.Bone(HumanBodyBones.RightHand); var lw = Seven.Bone(HumanBodyBones.LeftHand);
                  if (_ikSet && rw != null && lw != null) { _wristErrR.Add(Vector3.Distance(rw.position, ik.RightPos)); _wristErrL.Add(Vector3.Distance(lw.position, ik.LeftPos)); } }
                var chestB = CatBone("chest") ?? neck;
                var toHer = Flat(her - Vector3.Lerp(neck.position, chestB.position, 0.55f)).normalized;
                if (_bindHead == null || _rebind)
                {
                    _rebind = false;
                    var facing = up;   // (QF34b tried the side facing her: once he lies in her lap that side is tucked under her thighs)
                    var hs0 = OnFur(Vector3.Lerp(head.position, neck.position, 0.25f), facing, out var hn0); _bindHead = head; _hLocal = head.InverseTransformPoint(hs0); _hNLocal = head.InverseTransformDirection(hn0);
                    var ns0 = OnFur(Vector3.Lerp(head.position, neck.position, 1f), facing, out var nn0); _bindNeck = neck; _sLocal = neck.InverseTransformPoint(ns0); _sNLocal = neck.InverseTransformDirection(nn0);
                    var cs0 = OnFur(Vector3.Lerp(neck.position, chestB.position, 0.55f), (toHer + Vector3.up * 0.9f).normalized, out var cn0); _bindChest = neck; _cLocal = neck.InverseTransformPoint(cs0); _cNLocal = neck.InverseTransformDirection(cn0);
                    if (_bindLogs++ == 0) Log("FIN_10 HANDS (QF34 1): each hand bound to a point on his fur in his head / neck bones' space (no per-frame re-pick); the contact eased on by FurPalm");
                }
                float su = _stroke ? _strokeU : Mathf.MoveTowards(_strokeBack, 0f, Time.deltaTime / 1.0f); _strokeBack = su;   // the stroke goes there AND back (eased), never snaps
                var hSurfW = Vector3.Lerp(_bindHead.TransformPoint(_hLocal), _bindNeck.TransformPoint(_sLocal), su);
                var hNW = Vector3.Slerp(_bindHead.TransformDirection(_hNLocal), _bindNeck.TransformDirection(_sNLocal), su).normalized;
                var nSurfW = _bindChest.TransformPoint(_cLocal); var nNW = _bindChest.TransformDirection(_cNLocal).normalized;
                // QF34 (1): his head (after the shrink) lies 0.54-0.62 m from her right shoulder - past her 0.49 m arm even bowed: the hand
                // stopped short in the air. A point out of her reach slides along his fur to the nearest fur she CAN reach (toward her,
                // between her shoulder and the point) - eased by the smoothing below, so it never jumps
                _hRaw = hSurfW; _nRaw = nSurfW;   // the bow still leans toward his real head / neck
                if (_lapHeld) hSurfW = InReach(hSurfW, hNW, _palmR, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, ref hNW);
                if (_lapHeld) nSurfW = InReach(nSurfW, nNW, _palmL, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, ref nNW);
                if (!_smoothInit) { _hSm = hSurfW; _nSm = nSurfW; _smoothInit = true; }
                _hSm = Vector3.SmoothDamp(_hSm, hSurfW, ref _hVel, 0.3f); _nSm = Vector3.SmoothDamp(_nSm, nSurfW, ref _nVel, 0.3f);
                // a hand whose bound point stays out of her reach (its palm > 4 cm off his fur for 1.5 s) re-binds to the fur nearest the palm
                _reachCheck += Time.deltaTime;
                if (!_lapHeld && _reachCheck > 1.5f)   // while he is large (rc7's way); in her lap InReach takes over (this re-bind oscillated with her bow)
                {
                    _reachCheck = 0f;
                    foreach (var (right, hb, mb) in new[] { (true, HumanBodyBones.RightHand, HumanBodyBones.RightMiddleProximal), (false, HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleProximal) })
                    {
                        var hh = Seven.Bone(hb); var mm = Seven.Bone(mb); if (hh == null || mm == null) continue; var palmC = (hh.position + mm.position) * 0.5f;
                        if (FurContact.Nearest(Cat.transform, palmC, out var sp, out var snn, out var sgn) && sgn > 0.04f)
                        {
                            if (right) { _hLocal = _bindHead.InverseTransformPoint(sp); _hNLocal = _bindHead.InverseTransformDirection(snn); _sLocal = _bindNeck.InverseTransformPoint(sp); _sNLocal = _bindNeck.InverseTransformDirection(snn); }
                            else { _cLocal = _bindChest.InverseTransformPoint(sp); _cNLocal = _bindChest.InverseTransformDirection(snn); }
                            _reBinds++;
                        }
                    }
                }
                // the palms' facing eases too (a switch to another fur point turned the hand in one frame: the last spikes)
                if (_hNSm.sqrMagnitude < 0.5f) { _hNSm = hNW; _nNSm = nNW; }
                float kn = 1f - Mathf.Exp(-Time.deltaTime * 5f); _hNSm = Vector3.Slerp(_hNSm, hNW, kn).normalized; _nNSm = Vector3.Slerp(_nNSm, nNW, kn).normalized;
                var hSurf = _hSm; var hN = _hNSm; var nSurf = _nSm; var nN = _nNSm;
                // a bound point that drifted off his fur (it should not: the bones carry it) is re-bound, not chased
                if (FurContact.Nearest(Cat.transform, hSurfW, out _, out _, out var dOff) && Mathf.Abs(dOff) > 0.08f) { _offFor += Time.deltaTime; if (_offFor > 3f) { _offFor = 0f; _rebind = true; } } else _offFor = 0f;
                // QF34 (1): the WRIST is aimed so the PALM CENTRE lands on the bound point (the palm's offset from the wrist measured on the
                // pose, smoothed) - rc6 aimed the wrist 3 cm out with a guessed offset, so the palm stayed 5-11 cm off his fur
                { var rh = Seven.Bone(HumanBodyBones.RightHand); var rm = Seven.Bone(HumanBodyBones.RightMiddleProximal); var lh = Seven.Bone(HumanBodyBones.LeftHand); var lm = Seven.Bone(HumanBodyBones.LeftMiddleProximal);
                  if (rh != null && rm != null) _palmR = Vector3.Lerp(_palmR, (rm.position - rh.position) * 0.5f, 1f - Mathf.Exp(-Time.deltaTime * 5f));
                  if (lh != null && lm != null) _palmL = Vector3.Lerp(_palmL, (lm.position - lh.position) * 0.5f, 1f - Mathf.Exp(-Time.deltaTime * 5f)); }
                ik.RightPos = hSurf + hN * 0.012f - _palmR; ik.RightPalmN = hN; ik.RightPalmW = 1f; ik.RightFingerDir = Flat(Cat.transform.forward); ik.RightW = 1f;
                ik.LeftPos = nSurf + nN * 0.012f - _palmL; ik.LeftPalmN = nN; ik.LeftPalmW = 0.9f; ik.LeftFingerDir = (-toHer + Vector3.down * 0.5f).normalized; ik.LeftW = 1f;
                ik.LeftHint = P7 + Vector3.up * 0.7f - Seven.transform.right * 0.5f; ik.LeftHintW = 0.6f; ik.RightHint = P7 + Vector3.up * 0.8f + Seven.transform.right * 0.5f; ik.RightHintW = 0.6f;
                ik.LookPos = head.position; ik.LookW = 1f; _ikSet = true;
                _clipAt = (hSurf + nSurf) * 0.5f; _clipSide = Vector3.Cross(Vector3.up, Flat(head.position - P7).normalized);
                // and both palms held on him every frame (measured against his body after every other hand pass, never let go here)
                var fp = FurPalm.On(Seven.gameObject); fp.Set(true, Cat.transform, "FIN_10 hand on his head", 1f, true); fp.Set(false, Cat.transform, "FIN_10 hand on his shoulder", 1f, true);   // QF34 (1): the damped contact (FurPalm eases onto the fur)
                // P2 (8) / QF32 (1): she bows toward him as far as EITHER hand needs (kneeling at his head now, the bow is small)
                var rp = Seven.GetComponent<SevenRestPose>();
                float Need(HumanBodyBones a, HumanBodyBones b, HumanBodyBones c, Vector3 target)
                {
                    var sh = Seven.Bone(a); var lo = Seven.Bone(b); var hd = Seven.Bone(c);
                    if (sh == null || lo == null || hd == null) return 0f;
                    float arm = Vector3.Distance(sh.position, lo.position) + Vector3.Distance(lo.position, hd.position) + 0.06f;
                    return Vector3.Distance(sh.position, target) - arm * 0.92f;
                }
                if (rp != null)
                {
                    float need = Mathf.Max(Need(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, _hRaw),
                                           Need(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, _nRaw));
                    _reachBow = Mathf.MoveTowards(_reachBow, Mathf.Clamp(_reachBow + need * 80f, 0f, 85f), Time.deltaTime * 30f);
                    if (rp.Bow < _reachBow) rp.Bow = _reachBow;
                }
                yield return null;
            }
            Seven.GetComponent<FurPalm>()?.Off(); if (IK != null) IK.OwnsArms = false;
            if (CatOv != null) CatOv.Cradle = null;
            if (_wristErrR.Count > 10)
            {
                float Pct(System.Collections.Generic.List<float> l, float q) { var c = new System.Collections.Generic.List<float>(l); c.Sort(); return c[Mathf.Min(c.Count - 1, (int)(c.Count * q))]; }
                Log($"FIN_10 WRIST REACH (QF34 1): each wrist vs the target it was given, after every pass (IK, the kneel, FurPalm) - right (head) median {Pct(_wristErrR, 0.5f) * 100f:0.0} cm, p95 {Pct(_wristErrR, 0.95f) * 100f:0.0} cm; left (neck) median {Pct(_wristErrL, 0.5f) * 100f:0.0} cm, p95 {Pct(_wristErrL, 0.95f) * 100f:0.0} cm; held arms re-solved after the kneel {SevenTableIK.HeldSolves} frames; points out of her reach moved to the nearest fur she can reach {_inReachFrames} frames");
            }
            _wristErrR.Clear(); _wristErrL.Clear(); _ikSet = false;
        }
        IEnumerator BreathSounds()
        {
            _breathing = true; var ov = CatOv; float next = 0f;
            while (_breathing && Cat != null && ov != null)
            {
                next -= Time.deltaTime;
                if (next <= 0f && ov.Breath > 0.05f) { var head = CatBone("head"); AudioKit.Play("cat_breath_slow", head != null ? head.position : Cat.transform.position, Mathf.Lerp(0.25f, 0.6f, ov.Breath), 0.03f, AudioKit.Bus.Sfx, 8f); next = 1f / Mathf.Max(0.1f, ov.BreathRate); }
                yield return null;
            }
        }

        bool _trackCat;
        IEnumerator ShotAfter(float s, string n) { yield return Wait(s); yield return Shot(n); }
        IEnumerator TrackCat(Vector3 run)
        {
            var side = Vector3.Cross(Vector3.up, run);
            while (_trackCat && Cat != null) { var c = Cat.transform.position; var wallImg = L.ChamberC + Flat(c - L.ChamberC).normalized * (L.ChamberR - 0.2f) + Vector3.up * 3.2f; var back = Flat(c - wallImg).normalized; Shoot(c + back * 2.4f + Vector3.Cross(Vector3.up, back) * 0.6f + Vector3.up * 0.8f, Vector3.Lerp(c + Vector3.up * 0.3f, wallImg, 0.3f), 56f, 0.01f); /* side-on and low: him running, the chamber's wall behind */ yield return null; }
        }
        /// <summary>A proper two-armed carry: he lies across her forearms against her chest; both palms under him.</summary>
        IEnumerator CarryLift()
        {
            if (Cat == null) yield break;
            var from = Cat.transform.position; var q0 = Cat.transform.rotation;
            Cat.transform.SetParent(Seven.transform, true);
            var lp = new Vector3(0.02f, 1.0f, 0.3f); var lq = Quaternion.Euler(0f, -90f, 0f) * Quaternion.Euler(0f, 0f, 70f);
            var ik = IK; ik.Ease = 2f; ik.LeftPalmW = ik.RightPalmW = 1f; ik.LeftPalmN = ik.RightPalmN = Vector3.up;
            _carry = true; StartCoroutine(CarryHands());
            var lp0 = Cat.transform.localPosition; var lq0 = Cat.transform.localRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.1f) { float u = Mathf.SmoothStep(0f, 1f, t); Cat.transform.localPosition = Vector3.Lerp(lp0, lp, u); Cat.transform.localRotation = Quaternion.Slerp(lq0, lq, u); yield return null; }
            Log("FIN_11 (QF30 13/15): she lifts him gently in both arms against her chest (a two-armed carry, palms under him)");
        }
        bool _carry;
        /// <summary>Every hand this file drives lets go (his head / neck, the carry) - QF30 (3): called when each moment ends.</summary>
        void HandsOff(string why) { _handsOn = false; _carry = false; IK.ReleaseAll(why); Seven.GetComponent<FurPalm>()?.Off(); if (CatOv != null) CatOv.Cradle = null; }
        IEnumerator CarryHands()
        {
            var ik = IK;
            while (_carry && Cat != null)
            {
                var c = Cat.transform.position; var r = Seven.transform.right; var f = Seven.transform.forward;
                ik.RightPos = c + r * 0.13f - Vector3.up * 0.07f - f * 0.02f; ik.LeftPos = c - r * 0.15f - Vector3.up * 0.07f - f * 0.02f;
                ik.RightPalmN = Vector3.up; ik.LeftPalmN = Vector3.up; ik.RightFingerDir = -r; ik.LeftFingerDir = r;
                ik.RightHint = P7 + Vector3.up * 1.0f + r * 0.4f - f * 0.1f; ik.LeftHint = P7 + Vector3.up * 1.0f - r * 0.4f - f * 0.1f; ik.RightHintW = ik.LeftHintW = 0.7f;
                ik.RightW = 1f; ik.LeftW = 1f; ik.LookPos = c; ik.LookW = 0.8f;
                yield return null;
            }
        }
        /// <summary>QF34 (4): ONE straight vertical column of light directly over them, from the dark above straight down onto them (rc6:
        /// this column was tilted from her toward the chamber's oculus - the "diagonal beam" - and the PaleLight cylinder lit over the
        /// table at the same time).</summary>
        Material AscentColumn(Vector3 at)
        {
            var top = new Vector3(at.x, L.Y + L.ChamberH + 0.5f, at.z); var dir = Vector3.up; float len = top.y - at.y;
            var col = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(col.GetComponent<Collider>()); col.name = "AscentLight_Beam";
            col.transform.position = at + dir * len * 0.5f; col.transform.rotation = Quaternion.identity; col.transform.localScale = new Vector3(2.4f, len * 0.5f, 2.4f);
            var m = Markers.Additive(0f); col.GetComponent<Renderer>().sharedMaterial = m; col.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var disc = new GameObject("AscentLight_Glow"); disc.transform.position = at + Vector3.up * 0.02f; disc.AddComponent<MeshFilter>().sharedMesh = Markers.DiscMesh(1.3f, 32); disc.AddComponent<MeshRenderer>().sharedMaterial = m;
            return m;
        }
        /// <summary>QF30 (22): on the fade through white every sound of the Moon stops - the Scree's buzz, the chamber hum, the archive, the
        /// machinery, the drills, Tunneler, ambience loops, any 3D sound - except the music (it fades on its own) and the voice line playing.</summary>
        IEnumerator StopWorldAudio(float secs)
        {
            _endingAudio = true;
            var keep = new HashSet<AudioSource>();
            foreach (var m in FindObjectsByType<MusicSystem>(FindObjectsSortMode.None)) foreach (var a in m.GetComponentsInChildren<AudioSource>(true)) keep.Add(a);
            var voice = VoiceSource(); if (voice != null) keep.Add(voice);
            var all = FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Where(a => a != null && !keep.Contains(a) && !a.name.StartsWith("Voice") && !a.name.StartsWith("Reaction")).ToList();   // the dialogue's own voices stay (FIN_14's lines)
            var v0 = all.Select(a => a.volume).ToList();
            for (float t = 0f; t < 1f; t += Time.deltaTime / secs) { for (int i = 0; i < all.Count; i++) if (all[i] != null) all[i].volume = v0[i] * (1f - t); yield return null; }
            int n = 0; foreach (var a in all) if (a != null) { if (a.isPlaying) n++; a.Stop(); a.enabled = false; }
            // and the things that make them go quiet for good
            if (Rig != null) Rig.Silence();
            foreach (var w in new[] { Worker, Elder }) if (w != null) w.gameObject.SetActive(false);
            if (Workers != null) foreach (var w in Workers) if (w != null) w.gameObject.SetActive(false);
            if (Crew != null) foreach (var w in Crew) if (w != null) w.gameObject.SetActive(false);
            if (TunnelerCraft != null) TunnelerCraft.gameObject.SetActive(false);
            StopWorldDrillsNow();
            if (_barrierHum != null) _barrierHum.Stop();
            Log($"FIN_13 -> FIN_14 (QF30 22): every sound of the Moon faded out over the white and stopped ({all.Count} sources, {n} were playing); the Scree, Tunneler and the machinery switched off");
        }
    }
}
