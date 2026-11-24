using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using L = Seven.FinaleLayout;

namespace Seven
{
    /// <summary>
    /// Round 19 — "The Moon Finale" (Docs/CHAPTER_FINALE.md; FIN_02-FIN_15 here, FIN_01 aboard in Chapter1AboardFinale). Built on
    /// the shared systems: 7 on foot all chapter (the bike stays in its hold), Panther with her on foot (ChapterCat), the beam,
    /// the one saucer prefab, interactions and telekinesis, dialogue as data (FinaleDialogue), checkpoints (FinaleState), the
    /// boss-fight brain (TunnelerFight: short, three objectives, ended by the story). Every wait has a hint and a skip; story
    /// triggers fire at or past their place. Strictly monochrome until FIN_14 (the colour master: the one colour in the game).
    /// </summary>
    public partial class FinaleDirector : MonoBehaviour
    {
        public ChapterSeven Seven;
        public ChapterCamera Cam;
        public ChapterInput Input;
        public DialoguePlayer Dlg;
        public ChapterUI UI;
        public ControlsLegend Legend;
        public SaucerBeam SaucerBeam;
        public Transform Saucer;
        public Atmosphere Atmo;
        public Transform GuideLight;
        public ChapterCat Cat;
        [Header("The people")]
        public ScreeGuard Worker, Elder; public ScreeGuard[] Workers;
        public Transform Tunneler, TunnelerDrill, TunnelerCraft;
        [Header("FIN_03 / FIN_04")]
        public Renderer PowerPanelLamp, TerminalScreen; public Transform PowerLever, GateLockT, GateLeaf;
        public Transform DoorLeafL, DoorLeafR; public Renderer DoorMarkings;
        [Header("The chamber")]
        public Transform[] Sockets; public Renderer[] SocketLights; public Renderer PlatformSurface, ConduitGlow, ConduitBreak;
        public Renderer[] ProjectionWalls; public Transform BarrierField, BarrierControlT, Lock; public Renderer BarrierLight, PaleLight;
        public Transform[] MiningDrills; public Renderer[] CityTerminals;
        public Transform Dodecahedron;
        [Header("Video")]
        public UnityEngine.Video.VideoClip[] Visions; public UnityEngine.Video.VideoClip MeadowGray, MeadowColour;
        public Texture[] Memories;
        public Texture[] KyleArt; public Texture KyleThanks;

        public static FinaleDirector I { get; private set; }
        bool Auto => DemoBoot.CaptureDir != null && !DemoBoot.PlayerSim;
        bool Shots => DemoBoot.CaptureDir != null && Cap != null;
        ChapterCapture Cap => ChapterCapture.I;
        Vector3 P7 => Seven.transform.position;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        Vector3 HerHead => Seven.transform.position + Vector3.up * 1.62f;
        string _objective; bool _cut;
        public string ObjectiveNow => _objective;
        public Vector3? NextMarker { get; private set; }
        public string WaitingFor { get; private set; }
        public bool SkipOffered { get; private set; }
        public bool ChapterDone { get; private set; }
        public bool InCredits { get; private set; }
        public int Hints { get; private set; }
        public int Skips { get; private set; }
        public string Prompt { get; private set; }
        public ControlsLegend.Btn PromptBtn { get; private set; }
        public bool InCutscene => _cut;
        /// <summary>QF30 (3): a choice inside a scene (FIN_06 "Share it": her hand stays on the archive until she answers).</summary>
        public bool AwaitsChoice { get; private set; }
        public float HintAfter => DemoBoot.VenusHintFast ? 12f : 45f;
        public float SkipAfter => DemoBoot.VenusHintFast ? 30f : 120f;

        // ------------------------------------------------------------------------------------------ setup
        void Start()
        {
            I = this;
            if (DemoBoot.CaptureDir != null && !FinaleState.SceneSelectRun && !DemoBoot.Continue && DemoBoot.FinCp >= 0) FinaleState.ResetForTest();
            int startCp = DemoBoot.FinCp >= 0 && DemoBoot.CaptureDir != null ? DemoBoot.FinCp : FinaleState.Checkpoint;
            if (DemoBoot.FinCp >= 0 && DemoBoot.CaptureDir != null && !FinaleState.SceneSelectRun) FinaleState.Fake(startCp, true);
            if (startCp < L.CpBerth) startCp = L.CpBerth;
            UI.SetFade(1f);
            Legend.SetLayout(ControlsLegend.Layout.Chapter); Legend.HidePanels = true;
            if (DemoBoot.CaptureDir != null) { ChapterCapture.Ensure(); Cap.Attach(Cam.GetComponent<Camera>()); StartCoroutine(FirstFrame()); }
            if (Auto) { Seven.ScriptDriven = true; Dlg.AutoSkipAfter = DemoBoot.VenusSkip ? 0f : 0.9f; Cap.Log($"the finale's check run: {FinaleState.Summary()}"); }
            MoonTraversal.Use(new MoonTraversal.Ledge[0], null, null, null, null, null);
            var ex = new List<Transform> { Seven.transform };
            if (Cat != null) ex.Add(Cat.transform);
            foreach (var w in new[] { Worker, Elder }) if (w != null) ex.Add(w.transform);
            if (Workers != null) foreach (var w in Workers) if (w != null) ex.Add(w.transform);
            foreach (var t in new[] { GuideLight, Tunneler, TunnelerCraft, Dodecahedron, PowerLever, GateLockT, GateLeaf, DoorLeafL, DoorLeafR, BarrierField, Lock }) if (t != null) ex.Add(t);
            if (Sockets != null) foreach (var t in Sockets) if (t != null) ex.Add(t);
            if (ArchivePlatform != null) ex.Add(ArchivePlatform);                                   // QF31 (8): it lowers into the floor
            if (BreachPieces != null) foreach (var t in BreachPieces) if (t != null) ex.Add(t);   // QF31 (8): torn out in FIN_08
            if (WallDrills != null) foreach (var t in WallDrills) if (t != null) ex.Add(t);          // QF30 (6): they spin
            if (Crew != null) foreach (var c in Crew) if (c != null) ex.Add(c.transform);
            CameraOccluders.IncludeHuge = true;
            CameraOccluders.Build(ex.ToArray());
            ChapterCamera.RideIgnore = ex.ToArray();
            Dlg.LineStarted += OnLine;
            EnsureHover();                                                                            // QF30 (6): the rig hovers / sags
            if (Rig != null && Rig.Hull == null) Rig.Hull = _hover;
            if (TunnelerDrill != null) _dh = DrillHead.On(TunnelerDrill, Tunneler);                  // QF30 (5): the one shared drill
            // P2 (9): his body language - gestures aim at her, at the light well, at his workers' terminals
            var tbody = Tunneler != null ? Tunneler.GetComponent<TunnelerBody>() : null;
            if (tbody != null) tbody.Target = at => at == "her" ? P7 + Vector3.up * 1.2f : at == "platform" ? L.PlatformAt + Vector3.up * 0.3f
                : at == "terminals" ? TerminalsAt() : (Vector3?)null;
            SetupWallDrills();
            DialogueFraming.Speaker = id => id == "seven" ? Seven.transform : id == "cat" ? (Cat != null ? Cat.transform : null)
                : id == "worker" ? Worker?.transform : id == "worker2" ? Elder?.transform : id == "tunneler" ? Tunneler : null;
            _amb = AudioKit.Loop("amb_underground", null, null, 0.3f, AudioKit.Bus.Ambience);
            if (Cat != null) { Cat.Seven = Seven.transform; Cat.transform.localScale = Vector3.one; }
            if (AboardCut.I != null) AboardCut.I.AutoFirstLook = false;   // Panther is here with her (no cuts aboard)
            SaucerLocation.Set("moon");
            StartCoroutine(Flow(startCp));
        }
        AudioSource _amb;
        Vector3? TerminalsAt()
        {
            var sum = Vector3.zero; int n = 0;
            if (Workers != null) foreach (var w in Workers) if (w != null && w.gameObject.activeInHierarchy) { sum += w.transform.position; n++; }
            foreach (var w in new[] { Worker, Elder }) if (w != null && w.gameObject.activeInHierarchy) { sum += w.transform.position; n++; }
            return n > 0 ? sum / n + Vector3.up * 1.0f : (Vector3?)null;
        }
        void OnDestroy() { if (Dlg != null) Dlg.LineStarted -= OnLine; if (I == this) I = null; DialogueFraming.Speaker = null; MoonTraversal.UseMoon(); }
        IEnumerator FirstFrame() { Cap.Log($"first frame (the finale): fade {UI.FadeAlpha:0.00} (1 = black)"); yield return Cap.Shot("f00_first_frame"); }

        AudioSource _voiceSrc;
        AudioSource VoiceSource() { if (_voiceSrc == null) { var go = GameObject.Find("Voice_7"); if (go != null) _voiceSrc = go.GetComponent<AudioSource>(); } return _voiceSrc; }
        void OnLine(VoiceBank.Line l)
        {
            // the Scree speak with their heads turned to her; Tunneler's drill turns with his voice (as on Europa's screen)
            if (l.Speaker == "tunneler") StartCoroutine(TunnelerTalks(l));
        }

        void LateUpdate()
        {
            if (DemoBoot.CaptureDir != null && Seven != null && Seven.gameObject.activeInHierarchy) FeetProbe.Sample(Seven, "Finale " + L.ZoneAt(P7));
            if (GuideLight != null && Cam != null)
            {
                bool show = NextMarker.HasValue && !_cut;
                if (GuideLight.gameObject.activeSelf != show) GuideLight.gameObject.SetActive(show);
                UI.SetMarker(show ? NextMarker : null);
                if (show)
                {
                    float d = Vector3.Distance(Cam.transform.position, GuideLight.position);
                    GuideLight.localScale = Vector3.one * Mathf.Max(0.6f, d * 0.03f);
                    GuideLight.rotation = Quaternion.LookRotation(GuideLight.position - Cam.transform.position);
                }
            }
            // Tunneler's drill (QF30 5: the shared DrillHead - about the cone's own axis, never reversing) and his rig at rest
            TickRig();
            Ground.Obstacles.Clear();
            // QF31 (8): once the release begins the table is a light well (its rim kept clear) and the pedestals are flush (walk over them)
            Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(L.PlatformAt.x, L.PlatformAt.z), R = _sunk ? L.WellR + 0.05f : L.PlatformR + 0.3f });
            if (Sockets != null && !_sunk) foreach (var s in Sockets) if (s != null) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(s.position.x, s.position.z), R = 0.6f });
            if (_breached) foreach (var o in L.CavernProps) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(o.x, o.y), R = o.z });
            foreach (var w in new[] { Worker, Elder }) if (w != null && w.gameObject.activeInHierarchy) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(w.transform.position.x, w.transform.position.z), R = 0.45f });
            // QF30 (7): Tunneler's rig is solid - she (and Panther, who walks the same ground) stops at its rim; only while it sits on the
            // floor after a stun may she jump up onto its deck (item 8)
            if (TunnelerCraft != null && TunnelerCraft.gameObject.activeInHierarchy && Rig != null)
            {
                var c = TunnelerCraft.position; bool onDeck = Rig.DeckZone != null && Rig.DeckZone.Enabled && Flat(P7 - c).magnitude < Rig.DeckR + 0.1f;
                bool hop = Seven.InFight && Seven.Act == ChapterSeven.FightAct.Hop;
                bool walkOn = Rig.WalkOn && Rig.DeckZone != null && Rig.DeckZone.Enabled;                // QF31 (10): settled flush - she walks straight on
                if (!onDeck && !hop && !walkOn) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(c.x, c.z), R = Rig.Radius });
            }
            // QF30 (11): the controls are solid too
            foreach (var cp in new[] { L.BarrierControl, L.ConduitAt }) Ground.Obstacles.Add(new Ground.Circle { C = new Vector2(cp.x, cp.z), R = 0.62f });
        }
        float _drillWant;
        public float DrillSpeed { get => _drillWant; set => _drillWant = value; }

        // ------------------------------------------------------------------------------------------ helpers (as Cattalus)
        IEnumerator Wait(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }
        void Objective(string s) { _objective = s; UI.SetObjective(s); Log("objective: " + s); }
        void Log(string s) { Debug.Log("[Seven] " + s); if (DemoBoot.CaptureDir != null) Cap?.Log(s); }
        IEnumerator Shot(string name) { if (!Shots) yield break; yield return Cap.Shot(name); }
        void Shoot(Vector3 pos, Vector3 look, float fov = 50f, float ease = 1f) { Cam.Subjects.Clear(); Cam.Shot(pos, look, ease, ease <= 0.05f, fov); }
        IEnumerator Cutscene(bool on)
        {
            _cut = on;
            Seven.Lock(on);
            if (on) SetPrompt(null);
            if (!on)
            {
                if (Seven.Current == ChapterSeven.Mode.Touch || Seven.Current == ChapterSeven.Mode.Gesture) Seven.Release();
                // QF30 (3): control never comes back with a hand still on a target (1.0.0-rc1: her hand stayed on the archive while the
                // player chose "Share it" - the cutscene gave control back between the touch and her answer and nothing released it)
                var ik = Seven.GetComponent<SevenTableIK>(); if (ik != null) ik.ReleaseAll("control back (QF30 3)");
                var hp = Seven.GetComponent<HoldPose>(); if (hp != null) hp.Release("control back (QF30 3)");
                FollowCamera();
            }
            yield return null;
        }
        void FollowCamera() { Cam.Target = Seven.transform; Cam.Distance = 3.8f; Cam.LookHeight = 1.45f; Cam.AutoBehind = Auto; Cam.DefaultPitch = 15f; Cam.Follow(); }
        /// <summary>A two-shot from the side of the line between two people: the first framing with a clear view of both.</summary>
        void TwoShot(Vector3 mid, Vector3 side, float dist, float h, float lookH, float fov, float ease)
        {
            var axis = Vector3.Cross(side, Vector3.up).normalized; var look = mid + Vector3.up * lookH;
            foreach (var sg in new[] { 1f, -1f })
                foreach (var dk in new[] { 1f, 1.35f, 0.8f, 1.8f })
                    foreach (var hk in new[] { 0f, 1.2f, 2.6f })
                    {
                        var cam = mid + side * sg * dist * dk + Vector3.up * (h + hk);
                        if (!ChapterCamera.ClearLine(cam, look) || !ChapterCamera.ClearLine(cam, look + axis * 1.4f) || !ChapterCamera.ClearLine(cam, look - axis * 1.4f)) continue;
                        Shoot(cam, look, fov, ease); return;
                    }
            Shoot(mid + side * dist + Vector3.up * (h + 3f), look, fov, ease);
        }
        void HerFaceCloseUp() { var h = HerHead; var f = Flat(Seven.transform.forward).normalized; Shoot(h + f * 1.15f + Seven.transform.right * 0.35f + Vector3.down * 0.05f, h, 34f, 0.01f); }
        IEnumerator Say(string scene, int from = 1, int to = 99)
        {
            var ls = FinaleDialogue.Scene(scene, from, to);
            if (ls.Count == 0) { Log($"no lines for {scene} {from}-{to}"); yield break; }
            yield return Dlg.PlayLines(ls, false);
        }
        IEnumerator SayIds(params string[] ids) { var ls = FinaleDialogue.Ids(ids); if (ls.Count > 0) yield return Dlg.PlayLines(ls, false); }
        bool Complete(string id) { bool first = FinaleState.CompleteScene(id); Log($"scene {id} complete{(first ? "" : " (already done)")}"); return first; }
        void Checkpoint(int cp) { if (FinaleState.Reach(cp)) Log($"checkpoint {cp}: {L.CpNames[cp]} (saved for Continue)"); }
        bool SkipButton()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current; var gp = UnityEngine.InputSystem.Gamepad.current;
            return (kb != null && kb.enterKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame);
        }
        void SetPrompt(string label, ControlsLegend.Btn btn = ControlsLegend.Btn.Interact) { Prompt = label; PromptBtn = btn; Legend.Context(btn, label); }
        void SetZone(string name, bool on) { var z = Ground.FindZone(name); if (z != null) z.Enabled = on; }
        static void Glow(Renderer r, float v) { if (r != null) Glowing.Set(r, v); }
        void ShowGuide(Vector3 at) { NextMarker = at; if (GuideLight != null) { GuideLight.gameObject.SetActive(true); GuideLight.position = at + Vector3.up * 1.2f; } }
        void HideGuide() { NextMarker = null; UI.SetMarker(null); if (GuideLight != null) GuideLight.gameObject.SetActive(false); }

        /// <summary>Wait for a story condition: check runs log a soft-lock after autoTimeout; a player without progress gets the hint
        /// after HintAfter s (the guide light) and a skip after SkipAfter s (Y / Enter).</summary>
        IEnumerator Until(Func<bool> cond, string what, float autoTimeout, string hint = null, Vector3? at = null, Action skip = null)
        {
            float t0 = Time.time, since = Time.time, best = at.HasValue ? Vector3.Distance(P7, at.Value) : 0f; bool hinted = false;
            WaitingFor = what; SkipOffered = false;
            while (!cond())
            {
                if (Auto && Time.time - t0 > autoTimeout) { Cap.Soft++; Cap.Log($"SOFT-LOCK? waiting for '{what}' {autoTimeout:0}s (7 at {P7}, mode {Seven.Current}, prompt '{Prompt}')"); skip?.Invoke(); break; }
                bool busy = _cut || PauseMenu.IsOpen || Dlg.Busy;
                if (busy) { since += Time.deltaTime; yield return null; continue; }
                if (at.HasValue) { float d = Vector3.Distance(P7, at.Value); if (d < best - 2f) { best = d; since = Time.time; } }
                if (!Auto)
                {
                    if (!hinted && hint != null && Time.time - since > HintAfter) { hinted = true; Hints++; UI.Hint(hint, 8f); Log($"HINT: '{what}' -> \"{hint}\""); if (at.HasValue) ShowGuide(at.Value); }
                    if (!SkipOffered && skip != null && Time.time - since > SkipAfter) { SkipOffered = true; UI.Hint("Y: skip this step", 6f); Log($"SKIP OFFERED: '{what}'"); }
                    if (SkipOffered && SkipButton())
                    {
                        Skips++; Log($"SKIP taken: '{what}'");
                        yield return UI.Fade(1f, 0.5f, 1f);
                        try { skip?.Invoke(); } catch (Exception e) { Log("skip failed: " + e.Message); }
                        yield return null; yield return UI.Fade(0f, 0.7f, 1f);
                        break;
                    }
                }
                yield return null;
            }
            WaitingFor = null; SkipOffered = false;
            if (DemoBoot.CaptureDir != null) Cap?.Log($"ok: {what} ({Time.time - t0:0.0}s)");
        }
        int _walkGen;
        void StopWalks() { _walkGen++; Seven.ScriptMove = Vector3.zero; }
        IEnumerator WalkSeven(Vector3 to, float timeout, int gen = -1)
        {
            if (gen < 0) gen = ++_walkGen;
            bool sd = Seven.ScriptDriven; Seven.ScriptDriven = true; Seven.Lock(false);
            for (float t0 = Time.time; Flat(to - P7).magnitude > 0.35f && Time.time - t0 < timeout && gen == _walkGen;) { Seven.ScriptMove = Flat(to - P7).normalized * 0.8f; yield return null; }
            if (gen == _walkGen) Seven.ScriptMove = Vector3.zero;
            Seven.ScriptDriven = sd || Auto;
        }
        IEnumerator WalkPath(Vector3[] pts, float timeout) { int gen = ++_walkGen; float t0 = Time.time; foreach (var p in pts) { if (Time.time - t0 > timeout || gen != _walkGen) break; yield return WalkSeven(p, Mathf.Max(2f, timeout - (Time.time - t0)), gen); } }
        IEnumerator GuideWalk(Vector3[] pts, string what, float r = 1.6f)
        {
            int k = 0;
            if (Auto) StartCoroutine(WalkPath(pts, 40f * pts.Length));
            while (k < pts.Length)
            {
                for (int j = pts.Length - 1; j >= k; j--) if (Flat(P7 - pts[j]).magnitude < r) { k = j + 1; break; }
                if (k >= pts.Length) break;
                ShowGuide(pts[k]); int kk = k;
                yield return Until(() => Flat(P7 - pts[kk]).magnitude < r || GuideReached(pts, kk, r), $"{what} ({kk + 1}/{pts.Length})", 40f, "Follow the white light", pts[kk], () => Seven.Place(pts[kk], Vector3.back));
                k = kk + 1;
            }
            HideGuide();
        }
        bool GuideReached(Vector3[] pts, int k, float r) { for (int j = k + 1; j < pts.Length; j++) if (Flat(P7 - pts[j]).magnitude < r) return true; return false; }

        /// <summary>Walk up to a spot and press X.</summary>
        IEnumerator Interact(Vector3 spot, string label, string what, string hint, Vector3[] autoPath = null, Action skipped = null, float radius = 1.8f)
        {
            bool hit = false;
            IEnumerator P()
            {
                while (!hit)
                {
                    bool near = Flat(spot - P7).magnitude < radius && Mathf.Abs(Ground.Height(spot.x, spot.z) - P7.y) < 1.6f && (Seven.Current == ChapterSeven.Mode.Free || Auto);
                    if (!_cut) SetPrompt(near ? label : null);
                    if (near && (Auto || Input.Interact.WasPressedThisFrame())) hit = true;
                    yield return null;
                }
                SetPrompt(null);
            }
            StartCoroutine(P());
            ShowGuide(spot);
            if (Auto) StartCoroutine(WalkPath(autoPath ?? new[] { spot + Flat(P7 - spot).normalized * 1.0f }, 40f));
            yield return Until(() => hit, what, 60f, hint, spot, () => { Seven.Place(spot + Flat(P7 - spot).normalized * 1.0f, Flat(spot - P7)); hit = true; skipped?.Invoke(); });
            hit = true; if (Auto) StopWalks();
            HideGuide();
        }
        /// <summary>Telekinesis (RT) at something in range she looks at; its edges glow while it can be taken.</summary>
        IEnumerator Telekinesis(Vector3 target, float range, string what, string hint, Renderer glow = null, Vector3[] autoPath = null, Transform track = null)
        {
            bool hit = false;
            bool InView() { if (track != null) target = track.position; if (Auto) return Flat(target - P7).magnitude < range; var to = Flat(target - P7); if (to.magnitude > range) return false; if (to.magnitude < 4.5f) return true; return Vector3.Angle(Flat(Cam.transform.forward), to) < 50f || Vector3.Angle(Flat(Seven.transform.forward), to) < 50f; }
            IEnumerator P()
            {
                for (float t = 0f; !hit; t += Time.deltaTime)
                {
                    bool can = InView() && !_cut;
                    if (glow != null) Glow(glow, can ? 1.1f + 0.6f * Mathf.Sin(t * 3.5f) : 0.25f);
                    if (!_cut) SetPrompt(can ? "Telekinesis" : null, ControlsLegend.Btn.Tk);
                    if (can && (Auto || Input.Telekinesis.WasPressedThisFrame())) hit = true;
                    yield return null;
                }
                if (glow != null) Glow(glow, 0f);
                SetPrompt(null);
            }
            StartCoroutine(P());
            ShowGuide(target);
            if (Auto && autoPath != null) StartCoroutine(WalkPath(autoPath, 40f));
            yield return Until(() => hit, what, 60f, hint, target, () => { hit = true; });
            hit = true; if (Auto) StopWalks();
            TkAudit.Watch(this, what, track != null ? track.position : target, track);   // QF24 (14): did the visible object move?
            if (track != null) target = track.position;
            HideGuide();
            Seven.Gesture(target); Seven.SetEnergyPoint(1.9f, 0.25f);
            for (float t = 0f; t < 2.5f && !Seven.GesturePeak; t += Time.deltaTime) yield return null;
        }

        /// <summary>A short burst of white sparks.</summary>
        void Sparks(Vector3 at, int n)
        {
            var go = new GameObject("Sparks"); go.transform.position = at;
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main; m.duration = 0.3f; m.loop = false; m.startLifetime = 0.5f; m.startSpeed = 5f; m.startSize = 0.05f; m.gravityModifier = 1.2f; m.startColor = Color.white; m.maxParticles = 400;
            var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)n) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.1f;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Markers.Additive(2f);
            ps.Play(); Destroy(go, 2f);
        }
    }
}
