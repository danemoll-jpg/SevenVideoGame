using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF16 (4) - Panther does cat things, "not always, but sometimes" (Dan, 2026-11-20). The dignified, dry-witted
    /// telepathic cat is still a cat. Wordless: body language (his rig + <see cref="CatPoseOverlay"/>'s paw / tail) and 7's
    /// reaction sounds from the reaction bank.
    /// Scripted (one each): Venus C13 - he strolls across the console in front of the screen, sits, she leans to see round
    /// him, he moves on; Mars MAR_13C - he bats at the route line, paw still raised on "Those intervals match."; Europa EUR_01 -
    /// he paws at the glass at a mote drifting outside, tail twitching; Mars MAR_01 - his tail flicks across her face, she
    /// blows it aside without looking.
    /// Ambient while she is free aboard (rare: one every 2-4 min, never the same twice in a row, never in a scene or a line,
    /// she can always walk away): asleep in the window light, kneading the sofa + turning + flopping down, sitting in the
    /// pilot's chair until she walks up (then down, with dignity), chasing a light reflection then grooming when she looks,
    /// a big stretch + yawn, a loaf staring at nothing. Not built: the bike cradle (the bay is not reachable on foot) and the
    /// record nudge (no records rest on the chair arm while exploring) - noted in TODO.
    /// </summary>
    public partial class Chapter1Aboard
    {
        public static int CatMomentsPlayed { get; private set; }
        public static string LastCatMoment { get; private set; }

        // ------------------------------------------------------------------------------------------ helpers
        void CatAnim(string s, float fade = 0.15f, float speed = 1f)
        {
            if (Cat.Anim == null) return;
            if (!Cat.Anim.HasState(0, Animator.StringToHash(s))) s = s == "lie_down" ? "sleep" : s == "stretch" ? "sit" : "idle";
            if (Cat.Anim.HasState(0, Animator.StringToHash(s))) { Cat.Anim.speed = speed; Cat.Anim.CrossFadeInFixedTime(s, fade, 0); }
        }
        /// <summary>He walks (his real ground speed) to <paramref name="to"/> at its height.</summary>
        IEnumerator CatWalk(Vector3 to, float speed = 0.45f)
        {
            var tr = Cat.transform; float s = Mathf.Max(0.3f, tr.localScale.x);
            CatAnim("walk", 0.2f, Mathf.Clamp(speed / (Cat.StrideSpeed * s), 0.6f, 3f));
            float limit = Vector3.Distance(tr.position, to) / speed + 3f;
            for (float t = 0f; t < limit; t += Time.deltaTime)
            {
                var d = to - tr.position; if (d.magnitude < 0.02f) break;
                var step = Vector3.MoveTowards(tr.position, to, speed * Time.deltaTime);
                var f = Flat(d); if (f.sqrMagnitude > 1e-5f) tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.LookRotation(f), Time.deltaTime * 6f);
                tr.position = step;
                yield return null;
            }
            CatAnim("idle", 0.2f);
        }
        IEnumerator CatTurnTo(Vector3 look, float seconds = 0.5f)
        {
            var tr = Cat.transform; var q0 = tr.rotation; var f = Flat(look - tr.position); if (f.sqrMagnitude < 1e-5f) yield break;
            var q1 = Quaternion.LookRotation(f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds) { tr.rotation = Quaternion.Slerp(q0, q1, t); yield return null; }
        }
        void SevenReacts(string kind)
        {
            var clip = Reactions.Pick("seven", kind, out _);
            if (clip == null) return;
            var head = Seven.Bone(HumanBodyBones.Head);
            var src = new GameObject("CatMomentReaction").AddComponent<AudioSource>();
            src.transform.position = head != null ? head.position : Seven.transform.position + Up * 1.6f;
            src.spatialBlend = 0.5f; src.clip = clip;
            AudioKit.Track(src, AudioKit.Bus.Voice, 0.8f); src.Play();
            Destroy(src.gameObject, clip.length + 0.2f);
        }
        void MomentLog(string name, string detail)
        {
            CatMomentsPlayed++; LastCatMoment = name;
            Debug.Log($"[Seven] cat moment (QF16 4): {name} - {detail}");
            if (DemoBoot.CaptureDir != null) Cap?.Log($"cat moment (QF16 4): {name} - {detail}");
        }
        IEnumerator MomentShot(string name, Vector3 camPos, Vector3 look, float fov = 40f)
        {
            if (DemoBoot.CaptureDir == null || Cap == null) yield break;
            yield return Cap.ShotFrom("qf16_04_" + name, camPos, look, fov);
        }

        // ------------------------------------------------------------------------------------------ scripted (in the scenes)
        /// <summary>Venus C13: he strolls across the console right in front of the screen, sits; she leans to see round him;
        /// he moves on, unhurried. Runs beside the lines.</summary>
        IEnumerator CatMomentC13()
        {
            if (!_staged || CurrentStage != Stage.ChairCatConsole || ShipConsole.I == null) yield break;
            var disp = ShipConsole.I.Display != null ? ShipConsole.I.Display.position : PilotHead + PilotFwd * 1.1f;
            var y = _stageCat.y; var right = PilotRight; _catScripted = true;
            var front = disp - PilotFwd * 0.18f; front.y = y;                        // on the desk, right in front of the screen
            var far = front + right * 0.55f;
            yield return CatWalk(front, 0.28f);
            yield return CatTurnTo(PilotHead, 0.5f);
            CatAnim("sit", 0.25f);
            yield return Wait(0.4f);
            // she leans sideways to see round him (her head and gaze move to the screen's edge)
            var edge = disp - right * 0.45f;
            _ik.LookPos = edge; _ik.LookW = 1f;
            var spine = Seven.Bone(HumanBodyBones.Spine);
            float lean = 0f;
            System.Action leanApply = () => { if (spine != null) spine.rotation = Quaternion.AngleAxis(-lean, Seven.transform.forward) * spine.rotation; };
            _leanHook = leanApply;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f) { lean = Mathf.SmoothStep(0f, 11f, t); yield return null; }
            MomentLog("C13 stroll across the console", $"he sits in front of the screen at {front:0.00}; she leans to see round him");
            yield return MomentShot("c13_cat_sits_in_front_of_the_screen", PilotHead - PilotFwd * 0.5f + right * 0.55f + Up * 0.2f, front + Up * 0.1f, 44f);
            yield return Wait(1.6f);
            SevenReacts("hm_q");
            yield return CatWalk(far, 0.24f);
            CatAnim("sit", 0.3f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f) { lean = Mathf.Lerp(11f, 0f, t); yield return null; }
            _leanHook = null; _ik.LookPos = disp; _ik.LookW = 0.6f;
            _stageCat = far; _catScripted = false; CatState("sit");
            yield return MomentShot("c13_cat_moves_on", PilotHead - PilotFwd * 0.6f - right * 0.3f + Up * 0.25f, disp, 46f);
        }
        System.Action _leanHook;
        bool _ambStarted;
        void LateUpdateCatMoments()
        {
            _leanHook?.Invoke(); if (_glintOn) MoveGlint();
            if (Active && !_ambStarted && !DemoBoot.QF16Check && (DemoBoot.CaptureDir == null || DemoBoot.PlayerSim)) { _ambStarted = true; StartCoroutine(CatAmbientLoop()); }
        }

        /// <summary>Mars MAR_13C: he bats at the glowing route line (line 2), then sits with the paw still raised (line 3).</summary>
        IEnumerator CatMomentRouteBat(bool held)
        {
            if (!_staged || CurrentStage != Stage.ChairCatConsole || ShipConsole.I == null || _catPose == null) yield break;
            var disp = ShipConsole.I.Display != null ? ShipConsole.I.Display.position : PilotHead + PilotFwd * 1.1f;
            _catScripted = true;
            if (!held) yield return CatTurnTo(disp, 0.4f);
            if (!held)
            {
                CatAnim("idle", 0.2f);
                for (float t = 0f; t < 2.2f; t += Time.deltaTime)
                {
                    float k = Mathf.Repeat(t, 0.55f) / 0.55f;
                    _catPose.PawTo = disp - PilotFwd * 0.03f + Up * (0.04f + Mathf.Sin(k * Mathf.PI) * 0.06f) - PilotRight * 0.08f * Mathf.Sin(t * 3f);
                    _catPose.PawW = 1f;
                    if (_pawHeld) yield break;                                 // line 3 started: the paw stays raised instead
                    yield return null;
                }
                MomentLog("MAR_13C bats at the route line", "two quick swipes at the display");
            }
            else
            {
                _pawHeld = true;
                CatAnim("sit", 0.25f);
                _catPose.PawTo = Cat.transform.position + Cat.transform.forward * 0.12f * Cat.transform.localScale.x + Up * 0.16f * Cat.transform.localScale.x;
                _catPose.PawW = 1f;
                yield return Wait(0.5f);
                MomentLog("MAR_13C paw still raised", "sitting, his paw raised, on \"Those intervals match.\"");
                yield return MomentShot("mar13c_paw_still_raised", Cat.transform.position + Cat.transform.right * 0.6f + Up * 0.25f - Cat.transform.forward * 0.2f, Cat.transform.position + Up * 0.12f, 38f);
            }
        }
        bool _pawHeld;
        void CatPawDown() { if (_catPose != null) { _catPose.PawTo = null; _catPose.PawW = 0f; } _pawHeld = false; _catScripted = false; }

        /// <summary>Europa EUR_01 at the window: something drifts past outside; he paws at the glass, tail twitching.</summary>
        IEnumerator CatMomentGlass()
        {
            if (!_staged || CurrentStage != Stage.Window || _catPose == null) yield break;
            _catScripted = true; CatAnim("sit", 0.2f);
            var mote = GameObject.CreatePrimitive(PrimitiveType.Sphere); mote.name = "DriftingMote";
            Destroy(mote.GetComponent<Collider>());
            mote.transform.localScale = Vector3.one * 0.05f;
            var mr = mote.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(0.95f, 0.95f, 0.95f) };
            float a0 = WindowA + 12f, a1 = WindowA - 2f;
            _catPose.TailTwitch = 1f;
            for (float t = 0f; t < 4.2f; t += Time.deltaTime)
            {
                float u = t / 4.2f;
                var p = Polar(8.6f, Mathf.Lerp(a0, a1, u), 0.55f + Mathf.Sin(t * 2.1f) * 0.12f);
                mote.transform.position = p;
                if (t > 0.8f)
                {
                    var glass = Polar(8.02f, Mathf.Lerp(a0, a1, u) , p.y);
                    _catPose.PawTo = Vector3.Lerp(Cat.transform.position + Up * 0.1f, glass, 0.85f) + Up * Mathf.Abs(Mathf.Sin(t * 6f)) * 0.04f;
                    _catPose.PawW = 1f;
                }
                if (t > 2.0f && t - Time.deltaTime <= 2.0f)
                {
                    MomentLog("EUR_01 paws at the glass", "a mote drifts past outside; he paws at the glass, tail twitching");
                    var c = Cat.transform.position;
                    yield return MomentShot("eur01_cat_paws_at_the_glass", c - Flat(Polar(1f, WindowA, 0f)) * 0.9f + Vector3.Cross(Up, Flat(Polar(1f, WindowA, 0f))) * 0.4f + Up * 0.3f, c + Up * 0.2f + Flat(Polar(1f, WindowA, 0f)) * 0.3f, 40f);
                }
                yield return null;
            }
            CatPawDown(); _catPose.TailTwitch = 0f;
            Destroy(mote);
        }

        /// <summary>Mars MAR_01 on her shoulder: his tail flicks across her face; she blows it aside without looking.</summary>
        IEnumerator CatMomentTailFlick()
        {
            if (!_staged || CurrentStage != Stage.WindowShoulder || _catPose == null) yield break;
            var head = Seven.Bone(HumanBodyBones.Head);
            if (head == null) yield break;
            _catPose.TailToward = head.position + Seven.transform.forward * 0.12f; _catPose.TailFlick = 1f;
            for (float t = 0f; t < 1.3f; t += Time.deltaTime) { _catPose.TailToward = head.position + Seven.transform.forward * 0.12f + Seven.transform.right * Mathf.Sin(t * 5f) * 0.06f; yield return null; }
            MomentLog("MAR_01 tail across her face", "his tail flicks across her face");
            var catSideV = Vector3.Dot(Cat.transform.position - head.position, Seven.transform.right) >= 0f ? Seven.transform.right : -Seven.transform.right;
            yield return MomentShot("mar01_tail_across_her_face", head.position + Seven.transform.forward * 0.75f + catSideV * 0.75f + Up * 0.08f, head.position + catSideV * 0.08f, 34f);
            // she blows it aside without looking (a short breath, the tail swings away)
            _catPose.TailToward = head.position - Seven.transform.right * 0.5f + Up * 0.2f;
            yield return Wait(0.5f);
            _catPose.TailToward = null; _catPose.TailFlick = 0f;
        }

        // ------------------------------------------------------------------------------------------ ambient (free aboard)
        static readonly string[] Ambient = { "window_sleep", "knead_flop", "pilot_chair", "chase_glint", "stretch_yawn", "loaf" };
        string _lastAmbient;
        bool _ambientRunning;
        bool AmbientAllowed()
            => Active && !InGalleyScene && !_staged && !TravelPassage.Running && !ShipConsole.StoryLock && Seven != null && !Seven.Frozen && !Seven.Cutscene
               && Cat != null && Cat.enabled && Cat.Current == CatCompanion.Mode.Roam && (Dlg == null || !Dlg.Busy);
        IEnumerator CatAmbientLoop()
        {
            while (true)
            {
                float wait = Random.Range(120f, 240f);
                for (float t = 0f; t < wait; t += Time.deltaTime) yield return null;
                while (!AmbientAllowed()) yield return new WaitForSeconds(2f);
                var pick = Ambient[Random.Range(0, Ambient.Length)];
                if (pick == _lastAmbient) pick = Ambient[(System.Array.IndexOf(Ambient, pick) + 1 + Random.Range(0, Ambient.Length - 1)) % Ambient.Length];
                yield return PlayAmbient(pick);
            }
        }
        /// <summary>One ambient moment; it ends at once if a scene / dialogue / travel starts, or she walks well away.</summary>
        IEnumerator PlayAmbient(string pick)
        {
            _lastAmbient = pick; _ambientRunning = true;
            Cat.enabled = false;
            var tr = Cat.transform; var s = tr.localScale.x;
            IEnumerator body = pick switch
            {
                "window_sleep" => AmbWindowSleep(),
                "knead_flop" => AmbKnead(),
                "pilot_chair" => AmbPilotChair(),
                "chase_glint" => AmbGlint(),
                "stretch_yawn" => AmbStretch(),
                _ => AmbLoaf(),
            };
            var run = StartCoroutine(body);
            _ambDone = false;
            while (!_ambDone)
            {
                if (!Active || InGalleyScene || _staged || TravelPassage.Running || ShipConsole.StoryLock || (Dlg != null && Dlg.Busy)) { StopCoroutine(run); break; }
                yield return null;
            }
            _glintOn = false; if (_glint != null) _glint.SetActive(false);
            Cat.ShowCurled(false);
            CatPawDown(); if (_catPose != null) { _catPose.TailTwitch = 0f; _catPose.TailToward = null; }
            if (Cat.Anim != null) Cat.Anim.speed = 1f;
            var p = tr.position; p.y = 0f; tr.position = p;                       // back on the deck (from the sofa / chair)
            Cat.enabled = true; _ambientRunning = false;
        }
        bool _ambDone;
        Vector3 CatDeck(Vector3 p) { p.y = 0f; return p; }

        IEnumerator AmbWindowSleep()
        {
            var spot = Polar(7.3f, WindowA + Random.Range(-14f, 14f), 0f);
            yield return CatWalk(spot);
            yield return CatTurnTo(Vector3.zero, 0.6f);
            CatAnim("lie_down", 0.4f); yield return Wait(1.2f); CatAnim("sleep", 0.6f); Cat.ShowCurled(true);
            MomentLog("ambient: asleep in the window light", $"at {spot:0.0}");
            var ow = Flat(spot).normalized;
            yield return MomentShot("amb_asleep_in_the_window_light", spot - ow * 1.5f + Vector3.Cross(Up, ow) * 0.7f + Up * 0.7f, spot + Up * 0.08f, 40f);
            for (float t = 0f; t < 25f && Vector3.Distance(Seven.transform.position, spot) > 0.6f; t += Time.deltaTime) yield return null;
            _ambDone = true;
        }
        IEnumerator AmbKnead()
        {
            float a = Random.Range(150f, 175f);
            var front = Polar(6.25f, a, 0f); var seat = Polar(6.9f, a, 0.5f);
            yield return CatWalk(front);
            CatAnim("jump", 0.1f); var tr = Cat.transform; var p0 = tr.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f) { tr.position = Vector3.Lerp(p0, seat, t) + Up * Mathf.Sin(t * Mathf.PI) * 0.25f; yield return null; }
            tr.rotation = Quaternion.LookRotation(Inward(a));
            CatAnim("idle", 0.2f);
            // kneading: front paws pressing in turn
            for (float t = 0f; t < 2.6f; t += Time.deltaTime)
            {
                if (_catPose != null) { _catPose.PawTo = tr.position + tr.forward * 0.14f * tr.localScale.x + Up * (0.02f + Mathf.Abs(Mathf.Sin(t * 4f)) * 0.04f); _catPose.PawW = 1f; }
                yield return null;
            }
            CatPawDown();
            MomentLog("ambient: kneading the sofa", "kneads, turns round twice, flops down");
            yield return MomentShot("amb_kneading_the_sofa", seat + Inward(a) * 1.3f + Tangent(a) * 0.5f + Up * 0.45f, seat + Up * 0.1f, 40f);
            for (int k = 0; k < 2; k++) { var q0 = tr.rotation; for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f) { tr.rotation = q0 * Quaternion.Euler(0f, 360f * t, 0f); yield return null; } }
            CatAnim("lie_down", 0.3f); yield return Wait(0.8f); CatAnim("sleep", 0.6f); Cat.ShowCurled(true);
            yield return MomentShot("amb_flopped_down_on_the_sofa", seat + Inward(a) * 1.4f + Up * 0.6f, seat + Up * 0.05f, 40f);
            for (float t = 0f; t < 20f; t += Time.deltaTime) yield return null;
            _ambDone = true;
        }
        IEnumerator AmbPilotChair()
        {
            if (ShipConsole.I == null || ShipConsole.I.PilotSeat == null) { _ambDone = true; yield break; }
            var seat = ShipConsole.I.PilotSeat.position; var beside = CatDeck(seat + PilotRight * 0.7f - PilotFwd * 0.2f);
            yield return CatWalk(beside);
            CatAnim("jump", 0.1f); var tr = Cat.transform; var p0 = tr.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f) { tr.position = Vector3.Lerp(p0, seat + Up * 0.02f, t) + Up * Mathf.Sin(t * Mathf.PI) * 0.3f; yield return null; }
            tr.rotation = Quaternion.LookRotation(Flat(PilotFwd));                   // sitting where she sits, facing the console
            CatAnim("sit", 0.3f);
            MomentLog("ambient: sitting in the pilot's chair", "waits there until she walks up");
            yield return MomentShot("amb_in_the_pilot_chair", seat + Up * 1.5f + PilotRight * 0.75f + PilotFwd * 0.45f, seat + Up * 0.1f, 46f);
            if (DemoBoot.QF16Check) { Seven.transform.position = CatDeck(seat - PilotFwd * 1.1f); yield return null; }
            for (float t = 0f; t < 60f && Vector3.Distance(Flat(Seven.transform.position), Flat(seat)) > 1.6f; t += Time.deltaTime) yield return null;
            // she walks up: he looks at her, then gets down slowly, with dignity
            yield return CatTurnTo(Seven.transform.position, 0.8f);
            yield return Wait(1.2f);
            yield return MomentShot("amb_pilot_chair_he_looks_at_her", seat + Up * 1.45f - PilotRight * 0.8f + PilotFwd * 0.4f, (seat + Seven.transform.position + Up * 1.2f) * 0.5f, 54f);
            var down = CatDeck(seat - PilotRight * 0.75f - PilotFwd * 0.3f);
            CatAnim("jump", 0.1f, 0.7f); p0 = tr.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f) { tr.position = Vector3.Lerp(p0, down, t) + Up * Mathf.Sin(t * Mathf.PI) * 0.12f; yield return null; }
            yield return CatWalk(CatDeck(down - PilotFwd * 0.9f), 0.2f);
            SevenReacts("nose_laugh");
            _ambDone = true;
        }
        GameObject _glint; bool _glintOn; float _glintT;
        void MoveGlint()
        {
            if (_glint == null) return;
            _glintT += Time.deltaTime;
            var c = new Vector3(-2.0f + Mathf.Sin(_glintT * 0.9f) * 1.1f, 0.006f, -1.6f + Mathf.Cos(_glintT * 1.3f) * 0.8f);   // the open deck
            _glint.transform.position = c;
        }
        IEnumerator AmbGlint()
        {
            if (_glint == null)
            {
                _glint = GameObject.CreatePrimitive(PrimitiveType.Quad); _glint.name = "LightReflection"; Destroy(_glint.GetComponent<Collider>());
                _glint.transform.rotation = Quaternion.Euler(90f, 0f, 0f); _glint.transform.localScale = Vector3.one * 0.12f;
                _glint.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(1f, 1f, 1f) };
            }
            _glint.SetActive(true); _glintOn = true; _glintT = Random.value * 10f; MoveGlint();
            var tr = Cat.transform;
            for (float t = 0f; t < 6f; t += Time.deltaTime)
            {
                var to = CatDeck(_glint.transform.position);
                var d = to - tr.position;
                if (d.magnitude > 0.12f) { CatAnim("walk", 0.1f, 2.2f); tr.position = Vector3.MoveTowards(tr.position, to, 1.0f * Time.deltaTime); tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.LookRotation(Flat(d)), Time.deltaTime * 8f); }
                if (t > 2.5f && t - Time.deltaTime <= 2.5f) { MomentLog("ambient: chasing a light reflection", "across the deck"); yield return MomentShot("amb_chasing_a_reflection", tr.position + Up * 3.2f + Flat(-tr.forward) * 0.9f, tr.position + Flat(tr.forward) * 0.3f, 50f); }
                yield return null;
            }
            // she looks: he stops dead, grooms as if nothing happened
            _glintOn = false; _glint.SetActive(false);
            _ik.LookPos = tr.position; _ik.LookW = 0.7f;
            CatAnim("idle", 0.05f); yield return Wait(0.6f);
            CatAnim("wash", 0.3f);
            MomentLog("ambient: stops dead when she looks", "grooms as if nothing happened");
            yield return MomentShot("amb_grooming_as_if_nothing_happened", tr.position + Up * 2.2f + Flat(tr.forward) * 1.0f, tr.position + Up * 0.1f, 44f);
            yield return Wait(4f);
            _ik.LookW = 0f;
            _ambDone = true;
        }
        IEnumerator AmbStretch()
        {
            var tr = Cat.transform;
            CatAnim("stretch", 0.3f);
            yield return Wait(1.2f);
            MomentLog("ambient: a big stretch and yawn", "stretch, then a yawn");
            yield return MomentShot("amb_big_stretch", tr.position + tr.right * 1.0f + Up * 0.35f, tr.position + Up * 0.12f, 40f);
            yield return Wait(1.6f);
            CatAnim("sit", 0.3f);
            _ambDone = true;
        }
        IEnumerator AmbLoaf()
        {
            var tr = Cat.transform;
            CatAnim("lie_down", 0.4f);
            yield return Wait(1.4f);
            if (Cat.Anim != null) Cat.Anim.speed = 0.0f;                        // a still loaf, paws tucked under
            MomentLog("ambient: a loaf staring at nothing", "still, eyes on nothing in particular");
            yield return MomentShot("amb_loaf_staring_at_nothing", tr.position + Flat(tr.forward) * 0.9f + Up * 1.4f, tr.position + Up * 0.1f, 44f);
            for (float t = 0f; t < 12f && Vector3.Distance(Seven.transform.position, tr.position) > 0.5f; t += Time.deltaTime) yield return null;
            if (Cat.Anim != null) Cat.Anim.speed = 1f;
            _ambDone = true;
        }

        // ------------------------------------------------------------------------------------------ the QF16 check run
        /// <summary>-qf16check (free exploration aboard, Chapter 1 Continue): every ambient moment once (captures), then the
        /// passage to each world in turn with the window view captured on arrival (QF16 1 + 2 + 4).</summary>
        IEnumerator QF16Check()
        {
            Cap.Log("QF16 CHECK: ambient cat moments, then journeys Moon -> Venus -> Europa -> Mars -> Moon with the window view at each");
            yield return Wait(1.5f);
            yield return Cap.ShotFrom("qf16_01_window_view_moon", new Vector3(0f, 1.6f, 4.2f), new Vector3(0f, -14f, 60f), 58f);
            foreach (var a in Ambient)
            {
                for (float t = 0f; t < 6f && !AmbientAllowed(); t += Time.deltaTime) yield return null;
                Cap.Log($"ambient '{a}': allowed {AmbientAllowed()}");
                yield return PlayAmbient(a);
            }
            Cap.Log($"CAT MOMENTS (QF16 4): {CatMomentsPlayed} logged so far");
            Seven.Frozen = true; Seven.Cutscene = true;
            foreach (var w in new[] { "venus", "europa", "mars", "moon" })
            {
                yield return TravelPassage.Run(w);
                yield return UI.Fade(0f, 0.3f);
                yield return Wait(1.0f);
                yield return Cap.ShotFrom($"qf16_01_window_view_{w}", new Vector3(0f, 1.6f, 4.2f), new Vector3(0f, -14f, 60f), 58f);
                yield return Cap.ShotFrom($"qf16_01_window_view_{w}_low", new Vector3(0f, 1.3f, 6.6f), new Vector3(0f, -30f, 60f), 62f);
                if (Porthole != null) yield return Cap.ShotFrom($"qf16_01_porthole_{w}", Porthole.position - Porthole.forward * 0.9f + Up * 0.05f, Porthole.position, 34f);
                Cap.Log($"window view (QF16 1): arrived at '{w}' - the saucer is at '{SaucerLocation.Saved}', showing '{SaucerOutside.I?.Showing}'");
            }
            Seven.Frozen = false; Seven.Cutscene = false;
            Cap.Finish($"QF16 CHECK: cat moments {CatMomentsPlayed}, journeys {TravelPassage.Journeys}, the saucer at '{SaucerLocation.Saved}'");
        }
    }
}
