using System.Collections;
using UnityEngine;
using L = Seven.AsteroidLayout;

namespace Seven
{
    /// <summary>
    /// QF21 — Dan's Asteroid review, the director's side:
    ///  (6) <see cref="TrackWalk"/>: when the Machinist walks in a cutscene the camera moves with him (a tracking shot, low
    ///      for his scale) - never a fixed shot he walks out of (the general rule is ChapterCamera's shot follow);
    ///  (7) his conversations: her lines from his side but pulled back and out to the side (her face and body clear, at most
    ///      a little of his shoulder in the edge), his lines from low angles up at him, wide two-shots often (7 small at his
    ///      feet) - <see cref="HerFromHisSide"/>, <see cref="WideTwoShot"/>; every framing measured (his armour's share of
    ///      the frame, both of them visible);
    ///  (8) calm while he talks (<see cref="MachinistRig.Calm"/>, a single restrained gesture at most);
    ///  (11) the restrained Machinist (<see cref="MachinistRig.Restrained"/>, the clamp check every frame), the memory
    ///      on the archive surface synced to AST_09 (<see cref="MemorySync"/>), the clamps opening before he moves.
    /// </summary>
    public partial class AsteroidDirector
    {
        // ------------------------------------------------------------------------------------------ (8) calm in conversation
        bool _talkCalm; float _gestureUntil;
        void CalmTick()
        {
            if (MRig == null || Machinist == null) return;
            bool fighting = _fight != null && !_fight.Over && _fight.isActiveAndEnabled;
            MRig.Calm = _talkCalm && !Machinist.Busy && Time.time > _gestureUntil && !fighting;
        }
        /// <summary>One restrained gesture on a key line (the calm hold lets go for its length).</summary>
        void Gesture(string state, float secs = 2.4f) { if (Machinist == null) return; _gestureUntil = Time.time + secs; Machinist.Play(Machinist.HasState(state) ? state : "idle", 0.3f); Log($"MACHINIST (QF21 8): one restrained gesture '{state}' ({secs:0.0} s); otherwise still while he talks"); }

        // ------------------------------------------------------------------------------------------ (6) the camera follows him
        public static int Tracks;
        /// <summary>He walks to `to`; the camera rides along ahead of him and to one side (low: his scale) so he stays in
        /// frame the whole way; keep = prefer the side that keeps that point (7) in the shot too.</summary>
        IEnumerator TrackWalk(MarsFolk m, Vector3 to, float speed, string shot, Vector3? keep = null, float ahead = 6.5f, float sideDist = 5f)
        {
            if (m == null) yield break;
            // QF23 (9): never more than ~2 s of someone walking while 7 waits - a longer walk starts with a cut (he is already
            // most of the way there), the tracking shot covers the last stretch
            { var d0 = Flat(to - m.transform.position); float keepLen = speed * 2.0f; if (d0.magnitude > keepLen + 0.5f) { m.Place(to - d0.normalized * keepLen, d0); Log($"WALK CUT (QF23 9): '{shot}' - {d0.magnitude:0.0} m walk cut to the last {keepLen:0.0} m"); } }
            var start = m.transform.position; var dir = Flat(to - start); if (dir.sqrMagnitude < 0.01f) dir = m.transform.forward; dir.Normalize();
            var side = Vector3.Cross(Vector3.up, dir);
            float pref = keep.HasValue ? Mathf.Sign(Vector3.Dot(side, keep.Value - start) + 0.001f) : 1f;
            Vector3 off = dir * ahead + side * sideDist * pref + Vector3.up * 1.0f; bool found = false;
            foreach (var sg in new[] { pref, -pref })
                foreach (var a in new[] { ahead, ahead * 0.7f, ahead * 1.4f })
                    foreach (var sd in new[] { sideDist, sideDist * 1.5f, sideDist * 0.7f })
                    {
                        if (found) break;
                        var o = dir * a + side * sd * sg + Vector3.up * 1.0f; bool ok = true;
                        foreach (var k in new[] { 0f, 0.35f, 0.7f, 1f })
                        {
                            var b = Vector3.Lerp(start, to, k); var p = b + o; var c = b + Vector3.up * 2.4f;
                            if (RideProbe.Overlap(p, 0.3f, HoverBike.IgnoreRoots, null, false) || !ChapterCamera.ClearLine(p, c)) { ok = false; break; }
                        }
                        if (ok) { off = o; found = true; }
                    }
            var walk = m.WalkTo(to, speed);
            Shoot(start + off, start + Vector3.up * 2.4f, 52f, 0.01f);
            Cam.ShotFollow = m.transform;
            bool mid = false; float t0 = Time.time, dist = Flat(to - start).magnitude, outFrames = 0, frames = 0;
            var cam = Cam.GetComponent<Camera>();
            while (m.Busy)
            {
                frames++;
                var vp = cam.WorldToViewportPoint(m.transform.position + Vector3.up * 2f);
                if (vp.z < 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) outFrames++;
                if (!mid && Flat(m.transform.position - start).magnitude > dist * 0.5f) { mid = true; StartCoroutine(Shot(shot + "_tracking_midway")); }
                yield return null;
            }
            Cam.ShotFollow = null; Tracks++;
            Log($"TRACKING (QF21 6): '{shot}' - the camera moved with the Machinist {Flat(m.transform.position - start).magnitude:0.0} m in {Time.time - t0:0.0} s; he was out of frame {outFrames} of {frames} frames (must be 0){(found ? "" : " (fallback offset)")}");
            yield return Shot(shot + "_tracking_end");
        }

        // ------------------------------------------------------------------------------------------ (7) his conversations
        int _mLine;
        public static int FramedLines, FramedWide, FramedHer, FramedHim, ArmourShots; public static float ArmourWorst;
        /// <summary>Line framing for a Machinist conversation (AST_04 / 07 / 09): every third line a wide two-shot, his lines a
        /// low angle up at him, hers from his side pulled back and out (never a frame filled by his armour).</summary>
        IEnumerator Frame(string id)
        {
            var l = AsteroidDialogue.All; string who = null; foreach (var x in l) if (x.id == id) { who = x.speaker; break; }
            _talkCalm = true; _mLine++; FramedLines++;
            if (_mLine % 3 == 0 && WideTwoShot()) FramedWide++;
            else if (who == "machinist") { MachinistUp(); FramedHim++; }
            else if (who == "seven") { HerFromHisSide(); FramedHer++; }
            yield return null;
            if (who != "machinist") MeasureArmour(id);
        }
        /// <summary>The share of the frame his body takes (projected bounds) - logged per line; a frame filled by his armour
        /// (over ~half, with her hidden) is what DialogueFraming now rejects in every chapter.</summary>
        void MeasureArmour(string id)
        {
            if (Machinist == null) return;
            var cam = Cam.GetComponent<Camera>();
            float cov = DialogueFraming.Coverage(cam, Cam.ShotPos, Cam.ShotLook, cam.fieldOfView, Machinist.transform);
            bool her = DialogueFraming.Sees(cam, Cam.ShotPos, Cam.ShotLook, cam.fieldOfView, Seven.transform, new[] { Machinist.transform });
            ArmourShots++; ArmourWorst = Mathf.Max(ArmourWorst, her ? 0f : cov);
            if (DemoBoot.CaptureDir != null) Cap?.Log($"framing (QF21 7) {id}: his body {cov * 100f:0} % of the frame, 7 {(her ? "visible" : "HIDDEN")}");
        }
        /// <summary>Her line: the camera on his side of the line but pulled back and well out to the side, a little above her
        /// (looking down on her as she looks up at him) - her face and body clear, his armour at most in the edge.</summary>
        void HerFromHisSide()
        {
            if (Machinist == null) { OverHerShoulderUp(); return; }
            var cam = Cam.GetComponent<Camera>();
            var hh = HerHead; var toHim = Flat(Machinist.transform.position - P7).normalized; if (toHim.sqrMagnitude < 0.01f) toHim = Seven.transform.forward;
            var others = new[] { Machinist.transform };
            float best = 9f; Vector3 bp = default, bl = default; bool any = false;
            foreach (var a in new[] { 62f, -62f, 75f, -75f, 50f, -50f, 88f, -88f })
                foreach (var d in new[] { 4.6f, 5.6f, 3.8f })
                {
                    var dir = Quaternion.Euler(0f, a, 0f) * toHim;
                    var p = hh + dir * d + Vector3.up * 1.1f; var look = hh - Vector3.up * 0.45f + toHim * 0.4f;
                    if (RideProbe.Overlap(p, 0.25f, HoverBike.IgnoreRoots, null, false) || !ChapterCamera.ClearLine(p, hh)) continue;
                    if (!DialogueFraming.Sees(cam, p, look, 40f, Seven.transform, others)) continue;
                    float cov = DialogueFraming.Coverage(cam, p, look, 40f, Machinist.transform);
                    if (cov < best) { best = cov; bp = p; bl = look; any = true; }
                    if (cov < 0.12f) { Shoot(p, look, 40f, 0.01f); return; }
                }
            if (any) Shoot(bp, bl, 40f, 0.01f); else if (!WideTwoShot()) OverHerShoulderUp();
        }
        /// <summary>A wide two-shot: both of them from the side of the line between them, low - 7 small at his feet, him
        /// towering. False when no clear angle was found.</summary>
        bool WideTwoShot()
        {
            if (Machinist == null) return false;
            var cam = Cam.GetComponent<Camera>();
            var mp = Machinist.transform.position; var mid = (P7 + mp) * 0.5f; var axis = Flat(mp - P7).normalized; var side = Vector3.Cross(Vector3.up, axis);
            var people = new[] { Machinist.transform, Seven.transform };
            foreach (var sg in new[] { 1f, -1f })
                foreach (var d in new[] { 11f, 13.5f, 9f, 16f })
                    foreach (var sk in new[] { 0f, -0.25f, 0.25f })
                    {
                        var p = mid + (side * sg + axis * sk).normalized * d + Vector3.up * 1.1f; var look = mid + Vector3.up * 2.0f;
                        if (RideProbe.Overlap(p, 0.3f, HoverBike.IgnoreRoots, null, false)) continue;
                        if (!DialogueFraming.Sees(cam, p, look, 50f, Seven.transform, people) || !DialogueFraming.Sees(cam, p, look, 50f, Machinist.transform, people)) continue;
                        Shoot(p, look, 50f, 0.01f); return true;
                    }
            return false;
        }

        // ------------------------------------------------------------------------------------------ (9) the blades check run
        /// <summary>-qf21blades (with -astcp 5): the arc blades drawn - idle, walking, running - from the player's camera and
        /// from the front, then sheathed (the hilts on her back); the per-frame blade-vs-body check runs throughout.</summary>
        IEnumerator BladeDemo(int cp)
        {
            Restore(cp);
            yield return null; yield return null;
            yield return UI.Fade(0f, 0.6f);
            Seven.Place(new Vector3(-362f, 0f, 20f), Vector3.forward);
            Seven.SetBladesOwned(true); Seven.DrawBlades(true); FollowCamera(); Cam.SnapBehind();
            Seven.ScriptDriven = true; Seven.ScriptMove = Vector3.zero; Seven.ScriptRun = false;
            yield return Wait(2.0f);
            IEnumerator Both(string n) { yield return Shot(n + "_player_camera"); var f = Flat(Seven.transform.forward).normalized; var rgt = Vector3.Cross(Vector3.up, f); if (Shots) { yield return Cap.ShotFrom(n + "_front", P7 + f * 2.8f + rgt * 0.6f + Vector3.up * 1.2f, P7 + Vector3.up * 0.95f, 40f); yield return Cap.ShotFrom(n + "_side", P7 + rgt * 2.8f + Vector3.up * 1.1f, P7 + Vector3.up * 0.95f, 40f); } }
            yield return Both("qf21_b01_blades_drawn_idle");
            Seven.ScriptMove = Vector3.forward * 0.5f; yield return Wait(2.0f);
            yield return Both("qf21_b02_blades_drawn_walking");
            yield return Wait(1.0f);
            Seven.ScriptMove = Vector3.back * 1f; Seven.ScriptRun = true; yield return Wait(1.6f);
            yield return Both("qf21_b03_blades_drawn_running");
            yield return Wait(1.2f);
            Seven.ScriptMove = Vector3.zero; Seven.ScriptRun = false; yield return Wait(1.2f);
            Seven.DrawBlades(false); yield return Wait(1.0f);
            if (Shots) { var f = Flat(Seven.transform.forward).normalized; yield return Cap.ShotFrom("qf21_b04_sheathed_hilts_on_her_back", P7 - f * 2.4f + Vector3.up * 1.4f, P7 + Vector3.up * 1.2f, 40f); }
            Cap.Finish("QF21 BLADES CHECK done: " + ChapterSeven.BladeSummary());
        }

        // ------------------------------------------------------------------------------------------ (11b) the restrained Machinist
        Vector3 _clampHips0; int _clampFrames, _clampInside; float _clampDrift;
        IEnumerator RestrainAfter(float s)
        {
            yield return Wait(s);
            if (MRig == null || Machinist == null) yield break;
            MRig.Restrained = true;
            var h = Machinist.Anim != null && Machinist.Anim.isHuman ? Machinist.Anim.GetBoneTransform(HumanBodyBones.Hips) : null;
            _clampHips0 = h != null ? h.position : Machinist.transform.position;
            Log("MACHINIST (QF21 11): restrained in the cradle - body locked, only the head turns, the visor glows with his voice");
        }
        static readonly HumanBodyBones[] ClampBones = { HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, HumanBodyBones.Chest, HumanBodyBones.UpperChest, HumanBodyBones.Hips };
        /// <summary>Every frame while he is restrained: is any of his bones inside a cradle pad (the pads close round his
        /// upper body), and how far did his body move from where it was locked.</summary>
        void ClampCheck()
        {
            if (MRig == null || !MRig.Restrained || Cradle == null || Machinist == null || Machinist.Anim == null || !Machinist.Anim.isHuman) return;
            _clampFrames++;
            bool inside = false;
            foreach (var b in ClampBones)
            {
                var t = Machinist.Anim.GetBoneTransform(b); if (t == null) continue;
                if (Cradle.Closed && ((t.position - Cradle.PadA).magnitude < 0.28f || (t.position - Cradle.PadB).magnitude < 0.28f)) inside = true;
            }
            if (inside) _clampInside++;
            var h = Machinist.Anim.GetBoneTransform(HumanBodyBones.Hips);
            if (h != null) _clampDrift = Mathf.Max(_clampDrift, (h.position - _clampHips0).magnitude);
        }
        string ClampSummary() => $"CLAMPS (QF21 11b): {_clampFrames} restrained frames checked - a bone inside a cradle pad in {_clampInside} (must be 0), his body moved at most {_clampDrift * 100f:0.0} cm (locked), pose drift {(MRig != null ? MRig.HoldDrift : 0f):0.0} deg";

        // ------------------------------------------------------------------------------------------ (11a) the memory, synced
        UnityEngine.Video.VideoPlayer _mem; RenderTexture _memRT; int _memPhase; Coroutine _memSync;
        /// <summary>0 = the workshop and the bird (loops the first part), 1 = the production line stops (plays through, holds),
        /// 2 = the bird flutters (loops the end).</summary>
        void MemoryPhase(int p) { _memPhase = p; if (_mem != null) { if (p == 1) { _mem.time = Vision5.length * 0.26f; _mem.Play(); } if (p == 2) { _mem.time = Vision5.length * 0.62f; _mem.Play(); } } Log($"memory surface: phase {p} ({(p == 0 ? "the workshop and the bird" : p == 1 ? "the recorded production line stops" : "the bird flutters")})"); }
        IEnumerator MemoryOn()
        {
            if (MemoryScreen == null) yield break;
            var mat = MemoryScreen.material;
            Texture tex = null;
            if (Vision5 != null && !DemoBoot.NoVideo)
            {
                var go = new GameObject("MemoryVideo"); _mem = go.AddComponent<UnityEngine.Video.VideoPlayer>();
                _mem.playOnAwake = false; _mem.isLooping = false; _mem.clip = Vision5; _mem.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
                _memRT = new RenderTexture((int)Vision5.width, (int)Vision5.height, 0); _mem.targetTexture = _memRT;
                _mem.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None; _mem.timeUpdateMode = UnityEngine.Video.VideoTimeUpdateMode.GameTime;
                _mem.Prepare(); for (float t = 0f; t < 3f && !_mem.isPrepared; t += Time.deltaTime) yield return null;
                tex = _memRT; _mem.time = 0; _mem.Play();
            }
            else tex = Resources.Load<Texture2D>("Asteroid/vision_05_still");
            if (mat.HasProperty("_TexA")) { mat.SetTexture("_TexA", tex); mat.SetFloat("_Bright", 1.35f); }
            else { mat.SetColor("_BaseColor", Color.white); mat.SetTexture("_BaseMap", tex); }
            _memPhase = 0;
            if (_mem != null) _memSync = StartCoroutine(MemorySync());
            Log($"AST_09: the workshop memory plays on the archive surface beside him ({(_mem != null ? "the vision video, synced to the lines" : "the still")})");
            StartCoroutine(MemoryProbe());
        }
        IEnumerator MemorySync()
        {
            float L5 = (float)Vision5.length;
            while (_mem != null)
            {
                double t = _mem.time;
                if (_memPhase == 0 && t > L5 * 0.27f) _mem.time = 0.05;                     // the workshop: the bird on the bench, the line running
                else if (_memPhase == 1 && t > L5 * 0.6f) { _mem.Pause(); }                   // the line has stopped: hold on it
                else if (_memPhase == 2 && t > L5 - 0.2f) _mem.time = L5 * 0.62f;           // the bird flutters, again and again
                yield return null;
            }
        }
        /// <summary>Proof: the surface's picture is not black (the average brightness of the video frame on it).</summary>
        IEnumerator MemoryProbe()
        {
            yield return Wait(2f);
            float avg = -1f;
            if (_memRT != null)
            {
                var small = RenderTexture.GetTemporary(32, 18, 0); Graphics.Blit(_memRT, small);
                var prev = RenderTexture.active; RenderTexture.active = small;
                var t2 = new Texture2D(32, 18, TextureFormat.RGBA32, false); t2.ReadPixels(new Rect(0, 0, 32, 18), 0, 0); t2.Apply();
                RenderTexture.active = prev; RenderTexture.ReleaseTemporary(small);
                float s = 0f; foreach (var c in t2.GetPixels()) s += c.r; avg = s / (32 * 18); Destroy(t2);
            }
            Log($"MEMORY SURFACE (QF21 11a): the video frame on it averages {avg:0.00} brightness (0 = black; must be > 0.05), UVs + Seven/GalleryScreen");
        }
        void MemoryOff()
        {
            if (_memSync != null) StopCoroutine(_memSync);
            if (_mem != null) { _mem.Stop(); Destroy(_mem.gameObject); _mem = null; }
            if (MemoryScreen != null) { var mat = MemoryScreen.material; if (mat.HasProperty("_TexA")) mat.SetTexture("_TexA", Texture2D.blackTexture); }
        }
    }
}
