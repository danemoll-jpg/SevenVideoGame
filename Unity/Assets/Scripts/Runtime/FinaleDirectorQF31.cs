using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using L = Seven.FinaleLayout;

namespace Seven
{
    /// <summary>
    /// QF31 — Dan's review of 1.0.0-rc2: the ending reworked and the fight space opened up.
    /// (A2) FIN_09: Panther's sacrifice SHOWN - the drill strikes him mid-air (a brief slow-motion beat, sparks and rock dust, a heavy
    ///      impact, the music cut dead), he is thrown clear past her, hits the floor and slides, tries once to lift his head and can't;
    ///      the rig staggers back, the drill stalls; she sees it (her face) and runs to him. Then "The barrier.", the trap, the lock - and
    ///      Tunneler's last quip / "We always have." right after it, so nothing interrupts what follows.
    /// (A3/A4) after FIN_10 she stays kneeling over him and grieves; their memories play intercut with her bowed over him (Dan's edit
    ///      Art/Video/panther_memories.mp4 when it is in, else Code's in-game clips, else the stills) while the archive's light runs out
    ///      along the floor to her and under them both; FIN_12 "Record complete." -> every world receives it, with its voice (no 7 / no
    ///      Panther in any of those shots - checked) -> back to her.
    /// (A5/A6) no voice in the chamber: the light falls on her, she lifts her head; a column of pale warm light; she draws him close and
    ///      they rise together, weightless, the chamber falling away -> white.
    /// (A7) heaven: Dan's six colour clips (heaven_01..06) with soft dissolves, "Panther?" / "Hello." over the reunion, the Voice over the
    ///      rising wide shot - or, until they are in, the colour master from 17.6 s with the Voice near its end.
    /// (B8) the pedestals sink, the table lowers flush into a light well, his rig tears the far wall open onto the mining cavern.
    /// (B9) each rune's vision loops on its own screen. (C10) the stun: the rig settles flush - walk on, no jump (TunnelerFight).
    /// </summary>
    public partial class FinaleDirector
    {
        [Header("QF31")]
        public Transform ArchivePlatform; public Renderer WellRim; public Transform[] BreachPieces;
        public VideoClip[] VisionLoops, Heaven, MemoryClips; public VideoClip MemoriesVideo;

        bool _sunk, _breached;
        readonly List<Transform> _breachRubble = new List<Transform>();
        IEnumerator FallStraight(Transform t)
        {
            var v = Vector3.zero; float floor = L.Y + 0.25f;
            for (float u = 0f; u < 3f && t.position.y > floor; u += Time.deltaTime) { v.y -= 9.8f * Time.deltaTime; t.position += v * Time.deltaTime; t.Rotate(120f * Time.deltaTime, 60f * Time.deltaTime, 0f); yield return null; }
            t.position = new Vector3(t.position.x, floor, t.position.z); AudioKit.Play("stone_fall", t.position, 0.4f);
        }
        Vector3 _platHome; Vector3[] _sockHome;

        // ------------------------------------------------------------------------------------------ (9) the seven screens
        readonly Texture2D[][] _wallFrames = new Texture2D[7][]; readonly bool[] _wallOn = new bool[7];
        /// <summary>The rune's vision starts on its world's screen and keeps looping there softly: its frame sequence (Resources/Finale/
        /// ScreenLoops/vis_0N, 10 fps) cycled on the screen (QF31: seven VideoPlayers at once stalled the game).</summary>
        void PlayWall(int i)
        {
            if (ProjectionWalls == null || i >= ProjectionWalls.Length || ProjectionWalls[i] == null) return;
            if (_wallFrames[i] == null) { var f = Resources.LoadAll<Texture2D>($"Finale/ScreenLoops/vis_0{i + 1}"); System.Array.Sort(f, (a, b) => string.CompareOrdinal(a.name, b.name)); _wallFrames[i] = f; }
            _wallOn[i] = _wallFrames[i].Length > 0 && !DemoBoot.NoVideo;
            ShowWall(i, 1f);
        }
        void StopWalls() { for (int i = 0; i < 7; i++) _wallOn[i] = false; }
        /// <summary>The texture a screen shows: the current frame of its loop, else the still.</summary>
        Texture WallTex(int i)
        {
            if (i < 7 && _wallOn[i] && _wallFrames[i] != null && _wallFrames[i].Length > 0) return _wallFrames[i][(int)(Time.time * 10f + i * 7) % _wallFrames[i].Length];
            return Resources.Load<Texture2D>($"Finale/VisionStills/vis_0{i + 1}");
        }
        int _wallTick = -1;
        void TickWalls()
        {
            int k = (int)(Time.time * 10f); if (k == _wallTick || ProjectionWalls == null) return; _wallTick = k;
            for (int i = 0; i < 7 && i < ProjectionWalls.Length; i++) if (_wallOn[i] && ProjectionWalls[i] != null && ProjectionWalls[i].enabled) { var m = ProjectionWalls[i].material; if (m.HasProperty("_TexA")) m.SetTexture("_TexA", WallTex(i)); }
        }

        /// <summary>QF31 (9) gate check: every lit screen seen from <paramref name="cam"/> - nine points on its face, each with a clear line
        /// (geometry) and no light beam / column / field in the way (renderer bounds).</summary>
        int ScreenViewCheck(string when, Vector3 cam, bool onlyLit = true)
        {
            if (ProjectionWalls == null) return 0;
            var beams = FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy && (r.name.Contains("Beam") || r.name.Contains("Column") || r.name.Contains("PaleLight") || r.name.Contains("AscentLight") || r.name.Contains("BarrierField") || r.name.Contains("Light_Beam"))).ToList();
            int blocked = 0, hidden = 0, seen = 0;
            for (int i = 0; i < ProjectionWalls.Length; i++)
            {
                var w = ProjectionWalls[i]; if (w == null || (onlyLit && !w.enabled)) continue;
                int clear = 0, byBeam = 0, byWall = 0; string by = null;
                for (int a = -1; a <= 1; a++) for (int b = -1; b <= 1; b++)
                {
                    var p = w.transform.TransformPoint(new Vector3(a * 0.42f, b * 0.42f, 0f)); var dir = p - cam; float dist = dir.magnitude; var ray = new Ray(cam, dir / dist);
                    if (!ChapterCamera.ClearLine(cam, p - dir.normalized * 0.12f)) { byWall++; continue; }
                    bool beam = false; foreach (var r in beams) { if (r.bounds.IntersectRay(ray, out float hd) && hd < dist - 0.2f && hd > 0.3f) { beam = true; by = r.name; break; } }
                    if (beam) byBeam++; else clear++;
                }
                if (byWall == 9) { hidden++; continue; }                                 // not in view from here at all (behind the chamber's wall) - not a block
                seen++;
                if (byBeam > 0) { blocked++; Log($"SCREEN VIEW (QF31 9) {when}: screen {i + 1} ({L.SocketWorlds[i]}) - {byBeam}/9 points behind a light beam ({by}) - FAIL"); }
                else if (clear < 9) Log($"SCREEN VIEW (QF31 9) {when}: screen {i + 1} ({L.SocketWorlds[i]}) {clear}/9 points in view (the rest behind the chamber's own structure)");
            }
            Log($"SCREEN VIEW (QF31 9) {when}: {seen} screens in view, behind a light beam {blocked} (must be 0); {hidden} out of view from here");
            return blocked;
        }

        // ------------------------------------------------------------------------------------------ (8) the archive opens up the room
        void CaptureHomes()
        {
            if (_sockHome != null) return;
            _sockHome = Sockets != null ? Sockets.Select(s => s != null ? s.position : Vector3.zero).ToArray() : new Vector3[0];
            if (ArchivePlatform != null) _platHome = ArchivePlatform.position;
        }
        /// <summary>The pedestals sink into the floor (the runes stay set and glowing in flush sockets) and the table lowers flush into the
        /// floor as a shallow light well; the well's rim lights.</summary>
        IEnumerator SinkArchive(float secs)
        {
            CaptureHomes(); if (_sunk) yield break; _sunk = true;
            if (secs > 0f) { AudioKit.Play("ast_platform_retract", L.PlatformAt + Vector3.up, 1f); AudioKit.Play("slab_lift", L.PlatformAt, 0.7f); CameraShake.Quake(0.12f, secs); }
            var rune0 = _runes.Select(r => r != null ? r.position : Vector3.zero).ToArray();
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, secs))
            {
                Sink(Mathf.SmoothStep(0f, 1f, t), rune0); if (secs <= 0f) break; yield return null;
            }
            Sink(1f, rune0);
            Glow(WellRim, 0.7f); Glow(PlatformSurface, 0.22f);   // a soft well (it sits under the fight camera)
            Log($"QF31 (8): the rune pedestals sank flush into the floor (the runes set and glowing in their sockets), the archive's table lowered into a shallow light well ({L.WellDepth * 100f:0} cm), the rim lit - the open floor is clear");
        }
        void Sink(float u, Vector3[] rune0)
        {
            if (Sockets != null) for (int i = 0; i < Sockets.Length && i < _sockHome.Length; i++) if (Sockets[i] != null) { float k = Mathf.Clamp01(u * 1.25f - i * 0.035f); Sockets[i].position = _sockHome[i] + Vector3.down * L.SocketSink * Mathf.SmoothStep(0f, 1f, k); if (i < _runes.Count && _runes[i] != null && i < rune0.Length) _runes[i].position = rune0[i] + (Sockets[i].position - _sockHome[i]); }
            if (ArchivePlatform != null) ArchivePlatform.position = _platHome + Vector3.down * (L.PlatformH + L.WellDepth) * u;
            Glow(WellRim, u * 0.7f);
        }
        void SinkNow() { var r0 = _runes.Select(r => r != null ? r.position : Vector3.zero).ToArray(); CaptureHomes(); if (_sunk) return; _sunk = true; Sink(1f, r0); }

        /// <summary>His rig tears the far wall open: the panels round the old access burst inward as rubble (loose - thrown in the fight), dust
        /// fills the breach, and the cavern beyond becomes floor.</summary>
        IEnumerator TearOpen()
        {
            _breached = true;
            AudioKit.Play("europa_demolition", L.MiningAccess + Vector3.up * 3f, 1f); AudioKit.Play("path_collapse", L.MiningAccess, 0.9f); CameraShake.Quake(0.9f, 1.3f);
            DustBurst(L.MiningAccess + new Vector3(0f, 3f, 1.5f), 9f, 160);
            if (BreachPieces != null)
                foreach (var b in BreachPieces)
                {
                    if (b == null) continue;
                    var c = b.position; bool lintel = c.y > L.Y + 8f;
                    // the torn stone falls where the wall stood (along the breach's line, a little either side) - never flung across the open floor
                    for (int k = 0; k < (lintel ? 2 : 3); k++)
                    {
                        var go = new GameObject("BreachRubble"); go.AddComponent<MeshFilter>().sharedMesh = Markers.Rock(500 + _breachRubble.Count, new Vector3(0.5f, 0.38f, 0.45f) * Random.Range(0.8f, 1.15f)); go.AddComponent<MeshRenderer>().sharedMaterial = RockMat();
                        go.transform.position = new Vector3(Mathf.Clamp(c.x + Random.Range(-1.2f, 1.2f), 7.0f, 16.0f), L.Y + (lintel ? 7.5f : Random.Range(2f, 8f)), Mathf.Clamp(c.z + Random.Range(-1.6f, 1.2f), -178.6f, -172.6f)); go.transform.rotation = Random.rotation;
                        _breachRubble.Add(go.transform); StartCoroutine(FallStraight(go.transform));
                    }
                    foreach (var r in b.GetComponentsInChildren<Renderer>()) r.enabled = false;
                    b.gameObject.SetActive(false);
                }
            SetZone("Breach", true); SetZone("Cavern", true);
            Log("QF31 (8): his rig tore the far wall open - the panels round the old access burst in as rubble, the mining cavern beyond is open floor (the fight spills out into it)");
            yield return null;
        }
        void BreachNow()
        {
            _breached = true;
            if (BreachPieces != null) foreach (var b in BreachPieces) if (b != null) b.gameObject.SetActive(false);
            SetZone("Breach", true); SetZone("Cavern", true);
            if (_breachRubble.Count == 0)
                for (int k = 0; k < 8; k++)
                {
                    var go = new GameObject("BreachRubble"); var m = Markers.Rock(600 + k, new Vector3(0.5f, 0.38f, 0.45f) * (0.85f + (k % 3) * 0.12f)); go.AddComponent<MeshFilter>().sharedMesh = m; go.AddComponent<MeshRenderer>().sharedMaterial = RockMat();
                    go.transform.position = new Vector3(7.4f + k * 1.15f, L.Y + 0.01f - m.bounds.min.y, -175.4f - ((k * 37) % 5) * 0.55f); go.transform.rotation = Quaternion.Euler(0f, k * 57f, 0f); _breachRubble.Add(go.transform);
                }
        }
        void DustBurst(Vector3 at, float radius, int n)
        {
            var go = new GameObject("DustBurst"); go.transform.position = at;
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main; m.duration = 0.6f; m.loop = false; m.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.6f); m.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 5f); m.startSize = new ParticleSystem.MinMaxCurve(1.2f, radius * 0.4f); m.startColor = new Color(0.62f, 0.62f, 0.62f, 0.32f); m.maxParticles = n + 10; m.gravityModifier = 0.05f;
            var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)n) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = radius * 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0.35f, 0f), new GradientAlphaKey(0.25f, 0.3f), new GradientAlphaKey(0f, 1f) }); col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Markers.AdditiveMatTex(0.3f);
            ps.Play(); Destroy(go, 5f);
        }
        void QF31Restore(int cp)
        {
            CaptureHomes();
            if (cp > L.CpChamber) { SinkNow(); for (int i = 0; i < 7; i++) PlayWall(i); }
            if (cp > L.CpConfront || (cp == L.CpConfront && FinaleState.SceneDone("FIN_08_entrance"))) BreachNow();
        }

        // ------------------------------------------------------------------------------------------ A2: FIN_09 - the sacrifice, shown
        IEnumerator TheLeap()
        {
            yield return Cutscene(true);
            UI.HideBossBar();
            if (_fight != null) { _fight.End(); _fight.enabled = false; }
            ChapterSevenOff();
            if (Rig != null) { Rig.WalkOn = false; Rig.UpdateDeck(false); Rig.Drop(false); }
            _rigUp = true; _sag = 0f;
            if (!FinaleState.SceneDone("FIN_09_leap"))
            {
                BeaconDone(_conduitBeacon, 1.2f); Glow(ConduitBreak, 0.6f); Glow(ConduitLines, 1.2f); Glow(ConduitNode, 1.4f);
                if (Shots) { ImageGate.ExpectPoint(L.ConduitAt + Vector3.up * 0.9f, "the conduit's control, restored"); yield return Cap.ShotFrom("f38c_the_conduit_control_restored", L.ConduitAt + Flat(L.ChamberC - L.ConduitAt).normalized * 3.4f + Vector3.up * 1.9f + Vector3.forward * 1.2f, L.ConduitAt + Vector3.up * 0.5f, 50f); }
                StartCoroutine(DialogueFraming.Over("the restored conduit (the Archive's voice)", SayIds("FIN_08_P6")));   // Connection restored.
                // 1. the danger: she turns back toward the archive, unaware; the rig roars up behind her, the drill at her back
                var A = new Vector3(5.6f, L.Y, -159.6f); var toP = Flat(L.PlatformAt - A).normalized; var side = Vector3.Cross(Vector3.up, toP);
                Seven.Place(A, toP);
                var B0 = A - toP * 5.6f; var stopAt = A - toP * 3.4f;
                TunnelerCraft.position = B0; TunnelerCraft.rotation = Quaternion.LookRotation(toP);
                DrillSpeed = 1400f; if (Rig != null) Rig.ArmDrillWant = 1700f;
                var roar = AudioKit.Play("europa_drill", TunnelerCraft.position, 1f, 0f, AudioKit.Bus.Sfx, 40f);
                Shoot(A + toP * 1.0f + side * 0.6f + Vector3.up * 1.2f, TunnelerCraft.position + Vector3.up * 1.5f, 52f, 0.01f);
                ImageGate.Expect(TunnelerCraft, "his drill driving at her back");
                for (float t = 0f; t < 0.9f; t += Time.deltaTime) { TunnelerCraft.position = Vector3.MoveTowards(TunnelerCraft.position, stopAt, 2.6f * Time.deltaTime); yield return null; }
                yield return Shot("f39_the_drill_drives_at_her_back");
                HerFaceCloseUp();
                for (float t = 0f; t < 0.6f; t += Time.deltaTime) { TunnelerCraft.position = Vector3.MoveTowards(TunnelerCraft.position, stopAt, 2.6f * Time.deltaTime); yield return null; }
                yield return Shot("f39b_her_unaware");
                // where the drill's tip is: the cat meets it there, mid-air, between them
                var tip = DrillTip(); var G = Vector3.Lerp(A, Flat(tip) + Vector3.up * L.Y, 0.62f); G.y = L.Y;
                // 2. "Seven!" - Panther at the chamber's edge, already moving
                var P0 = new Vector3(3.0f, L.Y, -166.8f); var run = Flat(G - P0).normalized;
                if (Cat != null)
                {
                    Cat.enabled = true; Cat.gameObject.SetActive(true); Cat.transform.localScale = Vector3.one; Cat.Place(P0, run);
                    Cat.GoTo(G - run * 1.6f, 5.5f, G);
                    _trackCat = true; StartCoroutine(TrackCat(run));
                    ImageGate.Expect(Cat.transform, "Panther already running");
                    StartCoroutine(ShotAfter(0.25f, "f40a_panther_already_moving"));
                }
                yield return Say("FIN_09", 1, 1);                       // Seven!
                _trackCat = false;
                // 3. he grows mid-stride and leaps between them: wide, side-on, all three in frame (held - slower than QF30)
                var axis = Flat(TunnelerCraft.position - A).normalized; var sd = Vector3.Cross(Vector3.up, axis);
                var wideCam = G + sd * 8.4f + Vector3.up * 1.7f; Shoot(wideCam, G + Vector3.up * 1.2f, 60f, 0.01f);
                StartCoroutine(TurnTo(TunnelerCraft.position - P7, 0.7f));   // she starts to turn at his cry
                if (Cat != null)
                {
                    for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f) { Cat.transform.localScale = Vector3.one * Mathf.Lerp(1f, LargeScale, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
                    Cat.enabled = false; Cat.Scripted("jump"); Cat.enabled = false;
                    var c0 = Cat.transform.position; tip = DrillTip(); var meet = tip + Vector3.up * 0.05f;
                    var face = Quaternion.LookRotation(Flat(meet - c0));
                    // the leap: up and across toward the drill (seen side-on)
                    for (float t = 0f; t < 1f; t += Time.deltaTime / 0.55f)
                    {
                        var p = Vector3.Lerp(c0, meet - Vector3.up * CatMid(), t) + Vector3.up * Mathf.Sin(t * Mathf.PI * 0.5f) * 0.4f;
                        Cat.transform.SetPositionAndRotation(p, Quaternion.Slerp(Cat.transform.rotation, face * Quaternion.Euler(-14f, 0f, 0f), t));
                        if (t > 0.45f && !_leapShot) { _leapShot = true; ImageGate.Expect(Cat.transform, "Panther in the air between her and the drill"); StartCoroutine(Shot("f40_panther_leaps_between_them")); }
                        yield return null;
                    }
                    // 4. CONTACT - shown: the spinning drill strikes his flank mid-air (a brief slow-motion beat), sparks and rock dust off the
                    //    drill, a heavy impact, his body jolts with the blow. The music is cut dead on it. No blood, no wound, nothing hidden.
                    float ts0 = Time.timeScale; Time.timeScale = ts0 * 0.22f;
                    if (roar != null) roar.Stop();
                    MusicSystem.Silence("FIN_09 impact - Panther's death (FIN_10) and her grief", 300f); MusicSystem.Stop(0.05f); ThemeNow = null;
                    AudioKit.Play("drill_impact_heavy", meet, 1f); AudioKit.Play("land_hard", meet, 0.8f);
                    Sparks(meet, 220); DustBurst(meet, 2.4f, 60);
                    CameraShake.Quake(0.35f, 0.5f);
                    Shoot(wideCam + (G - wideCam) * 0.28f + Vector3.up * 0.1f, meet, 50f, 0.01f);   // wide enough: the drill, him on it, her beyond
                    ImageGate.Expect(Cat.transform, "the drill strikes Panther instead of her (contact)");
                    var q0 = Cat.transform.rotation;
                    for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
                    {
                        Cat.transform.rotation = q0 * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 40f) * 6f * (1f - t * 1.6f)) * Quaternion.Euler(Mathf.Lerp(0f, -22f, t * 2f), 0f, 0f);   // the jolt
                        if (t > 0.18f && !_hitShot) { _hitShot = true; StartCoroutine(Shot("f41_the_drill_strikes_him_mid_air")); }
                        yield return null;
                    }
                    Time.timeScale = ts0;
                    DrillSpeed = 0f; if (Rig != null) Rig.ArmDrillWant = 0f;   // the drill stalls against nothing
                    AudioKit.Play("mars_barrier_off", TunnelerCraft.position + Vector3.up, 0.7f);
                    // 5. she sees it happen: a close shot of her turning in time (her face)
                    StartCoroutine(TurnTo(meet - P7, 0.25f));                    // (already turning since his cry - never a snap)
                    var hf = Flat(meet - P7).normalized; Shoot(HerHead + hf * 1.05f + Vector3.Cross(Vector3.up, hf) * 0.32f + Vector3.down * 0.04f, HerHead, 36f, 0.01f);
                    ImageGate.Expect(Seven.transform, "her face: she sees him hit");
                    // meanwhile he is thrown clear (continues below); the rig staggers back from the force
                    var thrown = StartCoroutine(Thrown(meet, A));
                    StartCoroutine(RigStaggers(toP));
                    yield return Wait(0.35f);
                    yield return Shot("f41a_her_face_she_sees_it");
                    // 6. he tumbles through the air past her and hits the floor hard, sliding to a stop on his side
                    Shoot(L.PantherFalls + new Vector3(-3.6f, 1.5f, 5.6f), Vector3.Lerp(A, L.PantherFalls, 0.55f) + Vector3.up * 0.7f, 54f, 0.01f);
                    ImageGate.Expect(Cat.transform, "Panther thrown clear, tumbling past her");
                    yield return Wait(0.2f);
                    yield return Shot("f41b_he_is_thrown_clear_past_her");
                    yield return thrown;
                    ImageGate.Expect(Cat.transform, "Panther on his side on the floor, large");
                    yield return Shot("f41c_he_hits_the_floor_and_slides");
                    // 7. he doesn't get up: he tries once to lift his head and can't; his breathing is laboured
                    var ov = CatOv; if (ov != null) { ov.Alive = true; ov.Slack = 0f; ov.Breath = 1f; ov.BreathRate = 1.15f; }
                    Shoot(L.PantherFalls + new Vector3(1.6f, 0.75f, 3.0f), L.PantherHead + Vector3.up * 0.35f, 40f, 0.01f);
                    yield return TriesToRise();
                    yield return Shot("f41d_he_tries_to_lift_his_head_and_cannot");
                    Log("FIN_09 (QF31 A2): SHOWN - the drill at her back, 'Seven!', he grew and leapt between them, the spinning drill struck his flank mid-air (a 0.5 s slow-motion beat, sparks and rock dust off the drill, a heavy impact, the music cut dead), her face as she sees it, he was thrown clear past her, hit the floor and slid on his side, tried once to lift his head and couldn't (laboured breathing); the rig staggered back, the drill stalled. No blood, no wound.");
                }
                // she runs to him
                Shoot(L.PantherFalls + new Vector3(2.6f, 1.3f, 3.6f), Vector3.Lerp(L.PantherHead, P7, 0.4f) + Vector3.up * 0.4f, 46f, 0.01f);
                Seven.Lock(false); yield return WalkSeven(Vector3.Lerp(P7, L.KneelSpot, 0.6f), 1.6f); Seven.Lock(true);
                // Tunneler moves again, toward the archive
                _rigUp = true; DrillSpeed = 600f; if (Rig != null) Rig.ArmDrillWant = 600f;
                if (Cat != null) ImageGate.Expect(Cat.transform, "Panther, still conscious, and her");
                yield return Say("FIN_09", 2, 2);                       // The barrier.
                FinaleState.CompleteScene("FIN_09_leap");
            }
            else
            {
                PantherFallen(true);
                TunnelerCraft.position = L.ChamberC + new Vector3(-4.6f, 0f, 2.6f);
                MusicSystem.Silence("FIN_09 - the barrier (retry from the save)", 300f);
            }
            // the trap: she drives him into the prepared barrier (telekinesis), the worker locks it
            Objective("Drive him into the barrier");
            var bb = Highlight("the barrier", L.BarrierAt + new Vector3(-0.6f, 0f, 0f), null, BarrierFrameLines);
            if (Seven.InFight) Seven.EndFight(); if (Seven.Current != ChapterSeven.Mode.Free && Seven.Current != ChapterSeven.Mode.Locked) Seven.Release();
            yield return TurnTo(TunnelerCraft.position - P7, 0.5f);
            yield return Cutscene(false);
            Cam.SnapBehind();
            Log($"FIN_09: control back for the trap - 7 {Seven.Current}, {Flat(TunnelerCraft.position - P7).magnitude:0.0} m from him, facing him");
            yield return Telekinesis(TunnelerCraft.position + Vector3.up * 1.2f, 18f, "Tunneler into the barrier", "Drive him into the barrier's alcove (telekinesis)", null, null, TunnelerCraft);
            Objective(null); BeaconDone(bb, 1.4f);
            yield return Cutscene(true);
            var a2 = TunnelerCraft.position; var into = L.BarrierAt + new Vector3(0.6f, 0f, 0f);
            var mid = L.ChamberC + new Vector3(2f, 0f, 5.6f);
            Shoot(L.ChamberC + new Vector3(2f, 3.2f, 9.6f), L.BarrierAt + Vector3.up * 2f, 56f, 0.01f);
            AudioKit.Play("tk_push", a2, 1f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.6f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                var p = (1f - u) * (1f - u) * a2 + 2f * (1f - u) * u * mid + u * u * into;
                TunnelerCraft.position = p + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.6f;
                var d = Flat(into - p); if (d.sqrMagnitude > 0.01f) TunnelerCraft.rotation = Quaternion.Slerp(TunnelerCraft.rotation, Quaternion.LookRotation(-d), Time.deltaTime * 4f);
                yield return null;
            }
            TunnelerCraft.rotation = Quaternion.LookRotation(Vector3.left);
            if (BarrierField != null) BarrierField.gameObject.SetActive(true);
            AudioKit.Play("barrier_close", L.BarrierAt + Vector3.up * 2f, 1f);
            _barrierHum = AudioKit.Loop("bia_barrier", null, L.BarrierAt + Vector3.up * 2f, 0.22f, AudioKit.Bus.Sfx, 2f, 20f);
            ImageGate.Expect(TunnelerCraft, "Tunneler inside the barrier");
            yield return Shot("f42_the_barrier_closes_on_him");
            Shoot(Lock.position + new Vector3(-3f, -0.6f, -2.4f), Lock.position, 40f, 0.01f);
            Glow(BarrierLight, 2.0f); if (Lock != null) foreach (var r in Lock.GetComponentsInChildren<Renderer>()) Glow(r, 1.6f);
            yield return DialogueFraming.Over("the external lock (the worker over the network)", Say("FIN_09", 3, 3));   // We have the lock.
            Shoot(L.BarrierAt + new Vector3(-6.5f, 2.2f, 3.4f), TunnelerCraft.position + Vector3.up * 1.6f, 50f, 0.01f);
            var c00 = TunnelerCraft.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.7f) { TunnelerCraft.position = c00 + Vector3.left * Mathf.Sin(t * Mathf.PI) * 0.45f; yield return null; }
            AudioKit.Play("bia_barrier", L.BarrierAt + Vector3.up * 2f, 0.9f); if (BarrierField != null) foreach (var r in BarrierField.GetComponentsInChildren<Renderer>()) StartCoroutine(FieldFlash(r));
            DrillSpeed = 0f; _rigUp = false; _sag = 7f;
            if (Rig != null) Rig.PowerDown();
            foreach (var l in TunnelerCraft.GetComponentsInChildren<Light>()) l.enabled = false;
            AudioKit.Play("mars_barrier_off", TunnelerCraft.position, 0.6f);
            ImageGate.Expect(TunnelerCraft, "Tunneler contained");
            yield return Wait(1.2f);
            yield return Shot("f43_contained_the_rig_sagged_the_drills_spinning_down");
            // QF31 (A1): the Scree take him now (a cut - they're already there) and his last quip + their answer play right after the lock,
            // so nothing interrupts what follows (Panther's last moments)
            if (Crew != null) for (int i = 0; i < Crew.Length; i++) if (Crew[i] != null) { Crew[i].gameObject.SetActive(true); Crew[i].transform.position = L.CrewAt(i); Crew[i].transform.rotation = Quaternion.LookRotation(Flat(L.BarrierAt - L.CrewAt(i))); }
            Restrained(true);
            if (Elder != null) { Elder.gameObject.SetActive(true); Elder.transform.position = L.CrewAt(1) + new Vector3(0.4f, 0f, 1.2f); Elder.transform.rotation = Quaternion.LookRotation(Flat(L.BarrierAt - Elder.transform.position)); }
            var tb = Tunneler != null ? Tunneler.position : TunnelerCraft.position;
            Shoot(L.BarrierAt + new Vector3(-6.4f, 1.0f, 2.6f), tb + Vector3.up * 1.4f, 44f, 0.01f);
            if (_faceKey == null) { _faceKey = new GameObject("TunnelerFaceKey").AddComponent<Light>(); _faceKey.type = LightType.Point; _faceKey.range = 6f; _faceKey.intensity = 3.5f; _faceKey.shadows = LightShadows.None; }
            _faceKey.enabled = true; _faceKey.transform.position = tb + Vector3.up * 1.8f + Vector3.left * 1.6f;
            ImageGate.Expect(Tunneler != null ? Tunneler : TunnelerCraft, "Tunneler chained, the Scree round him");
            yield return Shot("f43b_tunneler_chained_the_scree_round_him");
            yield return SayIds("FIN_11_05b");                          // Fine. Keep your history. You'll still need someone to run the drills.
            if (Elder != null) Shoot(Elder.transform.position - Elder.transform.forward * 1.8f + Elder.transform.right * 0.9f + Vector3.up * 1.7f, tb + Vector3.up * 1.2f, 42f, 0.01f);
            yield return SayIds("FIN_11_05c");                          // We always have.
            KeyOff();
            Log("FIN_09 (QF31 A1): Tunneler contained, alive; the Scree took him (restraints, cables pulled) - his last quip and 'We always have.' right after the lock, before Panther's last moments");
            Complete("FIN_09");
            ChapterSevenOff();
        }
        bool _leapShot, _hitShot;
        /// <summary>The tip of his drill (the DrillHead's far end), world space.</summary>
        Vector3 DrillTip()
        {
            if (TunnelerDrill != null) { var r = TunnelerDrill.GetComponentInChildren<Renderer>(); if (r != null) { var b = r.bounds; var f = Flat(TunnelerCraft.forward).normalized; return b.center + f * Vector3.Dot(b.extents, new Vector3(Mathf.Abs(f.x), Mathf.Abs(f.y), Mathf.Abs(f.z))); } return TunnelerDrill.position; }
            return TunnelerCraft.position + TunnelerCraft.forward * 2.4f + Vector3.up * 1.8f;
        }
        float CatMid() { var b = CatBounds(); return b.center.y - Cat.transform.position.y; }
        /// <summary>Thrown clear: a tumbling arc past her, a hard landing, a slide to a stop on his side (large).</summary>
        IEnumerator Thrown(Vector3 from, Vector3 her)
        {
            var land = L.PantherFalls - Flat(L.PantherFalls - from).normalized * 1.6f;
            var start = Cat.transform.position; var q0 = Cat.transform.rotation;
            var qEnd = Quaternion.LookRotation(Flat(L.PantherHead - L.PantherFalls)) * Quaternion.Euler(0f, 0f, 78f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.75f)
            {
                var p = Vector3.Lerp(start, land, t); p.y = Mathf.Lerp(start.y, land.y, t) + Mathf.Sin(t * Mathf.PI) * 1.0f;
                Cat.transform.SetPositionAndRotation(p, Quaternion.Slerp(q0, qEnd, t) * Quaternion.Euler(t * 300f * (1f - t), 0f, 0f));
                yield return null;
            }
            AudioKit.Play("land_hard", land, 1f); AudioKit.Play("body_fall", land, 0.8f); DustBurst(land, 1.6f, 30);
            LayCat(land, L.PantherHead - L.PantherFalls, LargeScale, L.Y);
            var s0 = Cat.transform.position; var s1 = s0 + (L.PantherFalls - land);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.7f) { Cat.transform.position = Vector3.Lerp(s0, s1, 1f - (1f - t) * (1f - t)); yield return null; }
            AudioKit.Play("dust_fall", L.PantherFalls, 0.4f);
        }
        /// <summary>The rig staggers back from the force of the blow, nose up, the drill stalled.</summary>
        IEnumerator RigStaggers(Vector3 toP)
        {
            var a = TunnelerCraft.position; var b = a - toP * 1.6f; _sag = -14f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f) { TunnelerCraft.position = Vector3.Lerp(a, b, 1f - (1f - t) * (1f - t)); yield return null; }
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) yield return null;
            _sag = 0f;
        }
        /// <summary>He tries once to lift his head and can't; laboured breaths.</summary>
        IEnumerator TriesToRise()
        {
            var ov = CatOv; var head = CatBone("head");
            AudioKit.Play("cat_breath_labored", head != null ? head.position : Cat.transform.position, 0.8f);
            if (ov != null) ov.LookAt = HerHead;
            for (float t = 0f; t < 0.9f; t += Time.deltaTime) yield return null;
            if (ov != null) { ov.LookAt = null; ov.Slack = 0.35f; }
            AudioKit.Play("cat_breath_labored", head != null ? head.position : Cat.transform.position, 0.7f);
            for (float t = 0f; t < 0.8f; t += Time.deltaTime) yield return null;
        }

        // ------------------------------------------------------------------------------------------ A3 / A4: she grieves; the archive takes her memories
        IEnumerator Grieve()
        {
            yield return Cutscene(true);
            NoHud(true);
            QF30Watch.Free("FIN_10 -> FIN_12 - her grief, the memories, the record (scripted)");
            var rest = Seven.GetComponent<SevenRestPose>();
            if (rest == null || rest.Pose != SevenRestPose.Kind.Kneel) { KneelWithHim(); rest = Seven.GetComponent<SevenRestPose>(); }
            rest.Bow = 26f; rest.Sob = 0.55f;
            var fwd = Seven.transform.forward; var side = Vector3.Cross(Vector3.up, fwd);
            MusicSystem.EndSilence(); MusicSystem.Level = 0.45f; MusicSystem.Play("moon_transfer", 5f); ThemeNow = "moon_transfer";
            StartCoroutine(Sobs());
            // the archive's light: out of the well along the floor toward her, then under them both
            var light = StartCoroutine(ArchiveLightToHer(Auto ? 14f : 34f));
            // their moments together, intercut with her bowed over him
            int shown = 0; string src;
            if (MemoriesVideo != null && !DemoBoot.NoVideo)
            {
                src = "Dan's edit (panther_memories.mp4)";
                yield return HerOverHim(0, 2.4f);
                yield return PlayMemory(MemoriesVideo, 0f, Auto ? 8f : (float)MemoriesVideo.length, "f51_memory_dan_edit");
                shown = 1;
            }
            else if (MemoryClips != null && MemoryClips.Length > 0 && !DemoBoot.NoVideo)
            {
                src = $"Code's in-game clips ({MemoryClips.Length})";
                VideoPlayer nextVp = MakeVp(MemoryClips[0]);                          // P1 (4): the next clip loads while this one plays (no hitch)
                for (int i = 0; i < MemoryClips.Length; i++)
                {
                    var curVp = nextVp; nextVp = i + 1 < MemoryClips.Length ? MakeVp(MemoryClips[i + 1]) : null; _vpReady = curVp;
                    if (i % 2 == 0) yield return HerOverHim(i / 2, Auto ? 1.0f : 2.2f);
                    var c = MemoryClips[i];
                    float secs = Mathf.Min(Auto ? 1.6f : 4.6f, (float)c.length - 0.1f);
                    yield return PlayMemory(c, c.length > 8.0 ? (float)c.length - secs - 0.3f : 0f, secs, $"f51_memory_{i + 1}_{c.name}");   // a long raw clip: its last moments (the exchange lands there)
                    shown++;
                }
            }
            else
            {
                src = "the stills";
                if (Memories != null)
                    for (int i = 0; i < Memories.Length; i++)
                    {
                        if (i % 2 == 0) yield return HerOverHim(i / 2, Auto ? 1.0f : 2.2f);
                        var m = Memories[i];
                        for (float t = 0f; t < 0.5f; t += Time.deltaTime) { UI.SetVideo(m, m.width / (float)m.height, t / 0.5f); yield return null; }
                        UI.SetVideo(m, m.width / (float)m.height, 1f);
                        yield return Shot($"f51_memory_still_{i + 1}_{m.name}");
                        yield return Wait(Auto ? 1.2f : 2.8f);
                        for (float t = 0f; t < 0.5f; t += Time.deltaTime) { UI.SetVideo(m, m.width / (float)m.height, 1f - t / 0.5f); yield return null; }
                        shown++;
                    }
            }
            UI.SetVideo(null, 1f, 0f);
            yield return HerOverHim(3, Auto ? 1.2f : 2.6f);
            yield return light;
            ImageGate.Expect(Seven.transform, "the archive's light under them both");
            Shoot(P7 + fwd * 1.2f + side * 3.2f + Vector3.up * 1.6f, P7 + fwd * 0.5f + Vector3.up * 0.2f, 46f, 0.01f);
            yield return Wait(0.6f);
            yield return Shot("f51b_the_archive_light_under_them_both");
            Log($"FIN_10 -> FIN_12 (QF31 A3/A4): she stayed kneeling over him, crying softly; the memories of Panther intercut with her bowed over him - {shown} from {src}; the archive's light ran out along the floor to her and under them both (her testimony, Panther included, completes the record - no walk back, no carry)");
            yield return ReceivedQF31();
        }
        void KneelWithHim()
        {
            if (Cat != null && (Cat.enabled || Cat.transform.localScale.x > 1.2f)) { PantherFallen(false); var o = CatOv; if (o != null) { o.Breath = 0f; o.Alive = false; o.Slack = 1f; } }
            var hb = CatBone("head"); var headP = hb != null ? hb.position : L.PantherHead; var body = Cat != null ? CatBounds().center : L.PantherFalls;
            var away = Flat(headP - body); if (away.sqrMagnitude < 0.01f) away = Vector3.right; away.Normalize();
            var kneel = Flat(headP) + away * 0.5f; kneel.y = L.Y;
            Seven.Place(kneel, Flat(headP - kneel));
            var rest = SevenRestPose.On(Seven.gameObject, SevenRestPose.Kind.Kneel); rest.EaseIn = false; rest.PinHere();
            _handsOn = true; StartCoroutine(HandsOnHim());
        }
        /// <summary>Back on her, bowed over him (alternating close framings).</summary>
        IEnumerator HerOverHim(int k, float secs)
        {
            var fwd = Seven.transform.forward; var side = Vector3.Cross(Vector3.up, fwd);
            switch (k % 4)
            {
                case 0: Shoot(P7 + fwd * 1.1f + side * 1.0f + Vector3.up * 0.85f, HerHead + Vector3.down * 0.35f, 40f, 0.01f); break;
                case 1: Shoot(HandR() + side * 0.85f + Vector3.up * 0.35f, HandR(), 32f, 0.01f); break;
                case 2: Shoot(P7 + fwd * 1.5f - side * 1.1f + Vector3.up * 1.05f, HerHead + Vector3.down * 0.45f + fwd * 0.3f, 40f, 0.01f); break;   // in front of her, her face and his head (never from behind)
                default: Shoot(P7 + fwd * 1.6f + side * 3.4f + Vector3.up * 2.0f, P7 + fwd * 0.3f + Vector3.up * 0.4f, 44f, 0.01f); break;
            }
            ImageGate.Expect(Seven.transform, "7 bowed over him, grieving");
            yield return Wait(Mathf.Min(0.5f, secs));
            yield return Shot($"f50_she_grieves_over_him_{k + 1}");
            yield return Wait(Mathf.Max(0f, secs - 0.5f));
        }
        VideoPlayer _vpReady;
        VideoPlayer MakeVp(VideoClip clip)
        {
            var go = new GameObject("Memory_" + clip.name); var vp = go.AddComponent<VideoPlayer>();
            vp.playOnAwake = false; vp.clip = clip; vp.renderMode = VideoRenderMode.RenderTexture; vp.audioOutputMode = VideoAudioOutputMode.None; vp.timeUpdateMode = VideoTimeUpdateMode.GameTime;
            vp.targetTexture = new RenderTexture((int)clip.width, (int)clip.height, 0); vp.Prepare();
            return vp;
        }
        IEnumerator PlayMemory(VideoClip clip, float from, float secs, string shot)
        {
            var vp = _vpReady != null && _vpReady.clip == clip ? _vpReady : MakeVp(clip); _vpReady = null;
            var go = vp.gameObject; var rt = vp.targetTexture;
            for (float w = 0f; w < 3f && !vp.isPrepared; w += Time.deltaTime) yield return null;
            vp.time = from; vp.Play();
            float asp = clip.width / (float)clip.height;
            for (float t = 0f; t < 0.45f; t += Time.deltaTime) { UI.SetVideo(rt, asp, t / 0.45f); yield return null; }
            UI.SetVideo(rt, asp, 1f);
            yield return Wait(Mathf.Min(0.6f, secs * 0.4f));
            yield return Shot(shot);
            yield return Wait(Mathf.Max(0f, secs - 1.5f));
            for (float t = 0f; t < 0.45f; t += Time.deltaTime) { UI.SetVideo(rt, asp, 1f - t / 0.45f); yield return null; }
            UI.SetVideo(null, 1f, 0f); vp.Stop(); Destroy(go); rt.Release();
        }
        bool _sobbing;
        IEnumerator Sobs()
        {
            _sobbing = true; float next = 0.8f; int n = 0;
            while (_sobbing) { next -= Time.deltaTime; if (next <= 0f) { AudioKit.Play(n % 3 == 1 ? "seven_sob_soft" : "seven_breath_shaky", HerHead, n % 3 == 1 ? 0.5f : 0.4f, 0.04f, AudioKit.Bus.Voice, 10f); n++; next = Random.Range(3.5f, 6.5f); } yield return null; }
        }
        /// <summary>The archive's light runs out of the well along the floor to her, and spreads under them both.</summary>
        IEnumerator ArchiveLightToHer(float secs)
        {
            var mat = Markers.Additive(0f);
            var to = Flat(P7 - L.PlatformAt); float len = Mathf.Max(0.5f, to.magnitude - L.WellR); var dir = to.normalized;
            var strip = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(strip.GetComponent<Collider>()); strip.name = "ArchiveLight_Strip"; strip.GetComponent<Renderer>().sharedMaterial = mat; strip.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var disc = new GameObject("ArchiveLight_Pool"); disc.AddComponent<MeshFilter>().sharedMesh = Markers.DiscMesh(1.5f, 40); var dm = Markers.Additive(0f); disc.AddComponent<MeshRenderer>().sharedMaterial = dm;
            var under = new Vector3(P7.x, L.Y + 0.025f, P7.z) - dir * 0.6f; disc.transform.position = under;
            var start = L.PlatformAt + dir * L.WellR; start.y = L.Y + 0.02f;
            Glow(WellRim, 1.4f); Glow(PlatformSurface, 0.8f); Glow(ConduitGlow, 0.9f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / secs)
            {
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.7f)), l = Mathf.Max(0.05f, len * u);
                strip.transform.position = start + dir * l * 0.5f; strip.transform.rotation = Quaternion.LookRotation(Vector3.down, dir); strip.transform.localScale = new Vector3(0.32f, l, 1f);
                mat.SetFloat("_Intensity", 0.35f + 0.1f * Mathf.Sin(t * 30f));
                if (t > 0.7f) dm.SetFloat("_Intensity", Mathf.SmoothStep(0f, 0.32f, (t - 0.7f) / 0.3f));
                yield return null;
            }
            Log("QF31 (A4): the archive's light ran out of the well along the floor to where she kneels and spread under them both");
        }

        /// <summary>FIN_12, QF31: "Record complete." -> the Scree at their stations, then every world receives it, each with its own voice
        /// (the places and their people only - never 7 or Panther: checked) -> back to her, still kneeling over him.</summary>
        IEnumerator ReceivedQF31()
        {
            var fwd = Seven.transform.forward; var side = Vector3.Cross(Vector3.up, fwd);
            Shoot(P7 + fwd * 1.6f + side * 3.4f + Vector3.up * 2.0f, P7 + fwd * 0.3f + Vector3.up * 0.4f, 44f, 0.01f);
            yield return DialogueFraming.Over("the archive (its voice) over her, kneeling", Say("FIN_12", 7, 7));   // Record complete.
            ReceivedCheckStills();
            // the lunar city: the Scree stations connected (the city's own terminals, far from the chamber)
            var term = CityTerminals != null && CityTerminals.Length > 0 ? CityTerminals[0] : null;
            if (Worker != null && term != null) { Worker.gameObject.SetActive(true); Worker.transform.position = term.transform.position - term.transform.forward * 1.3f; Worker.transform.rotation = Quaternion.LookRotation(Flat(term.transform.position - Worker.transform.position)); }
            var elderWas = Elder != null ? Elder.transform.position : Vector3.zero;
            if (Elder != null && CityTerminals != null && CityTerminals.Length > 1) { Elder.gameObject.SetActive(true); var t2 = CityTerminals[1]; Elder.transform.position = t2.transform.position - t2.transform.forward * 1.3f; Elder.transform.rotation = Quaternion.LookRotation(Flat(t2.transform.position - Elder.transform.position)); }
            if (term != null) { Shoot(term.transform.position + new Vector3(2.6f, 1.3f, 2.8f), term.transform.position + Vector3.down * 0.2f, 46f, 0.01f); ImageGate.Expect(Worker != null ? Worker.transform : term.transform, "the Scree at their own stations"); }
            yield return Wait(0.4f);
            UsInFrame("f52_received_the_scree_at_their_stations");
            yield return Shot("f52_received_the_scree_at_their_stations");
            yield return Say("FIN_12", 1, 1);                           // We have it. Our records... they're here.
            if (Elder != null) { Shoot(Elder.transform.position + Elder.transform.forward * 2.6f + Vector3.up * 1.5f + Elder.transform.right * 0.8f, Elder.transform.position + Vector3.up * 1.4f, 42f, 0.01f); UsInFrame("the elder at the terminal"); }
            yield return Say("FIN_12", 2, 2);                           // Send them to the other districts.
            yield return Received("rec_1_venus", "Venus: the temple receives it", () => Say("FIN_12", 3, 3), null);
            yield return Received("rec_2_mars", "Mars: the temple of both kingdoms - the caretaker", () => SayIds("FIN_12_03a"), "mars_vision_stone");
            yield return Received("rec_3_europa", "Europa: the Cetaceans gathering, the familiar call - the Keeper", () => SayIds("FIN_12_03b"), "europa_call_deep");
            yield return Received("rec_4_asteroid", "the asteroid: the Machinist beside the archive", () => Say("FIN_12", 4, 5), null);
            yield return Received("rec_5_mothership", "the mothership: the memory structures", () => Say("FIN_12", 6, 6), "bia_barrier");
            yield return Received("rec_6_cattalus", "Cattalus: the city's cats", null, null, 1.4f);
            yield return Received("rec_7_cattalus_meadow", "Cattalus: the old meadow on the Cat-a-tron (the old vision) - Juniper", () => SayIds("FIN_12_06a"), "cat_meadow_breeze", 2.6f);
            UI.SetVideo(null, 1f, 0f);
            if (Worker != null) Worker.gameObject.SetActive(false);
            if (Elder != null) Elder.transform.position = elderWas;
            // back to her, still kneeling over him; the chamber's screens and sockets full
            for (int i = 0; i < 7; i++) Glow(i < SocketLights.Length ? SocketLights[i] : null, 2.0f);
            Shoot(P7 + fwd * 1.5f - side * 1.1f + Vector3.up * 1.05f, HerHead + Vector3.down * 0.45f + fwd * 0.3f, 40f, 0.01f);
            ImageGate.Expect(Seven.transform, "back to her, still kneeling over him");
            yield return Wait(0.5f);
            yield return Shot("f53_back_to_her_still_kneeling_over_him");
            yield return DialogueFraming.Over("the archive (its voice) over her", Say("FIN_12", 8, 8));   // All connected archives have received it.
            Complete("FIN_12");
            Log($"RECEIVED MONTAGE (QF31 4): the in-scene shots - 7 / Panther visible in {_usSeen} of {_usChecked} (must be 0)");
        }
        int _usSeen, _usChecked;
        /// <summary>QF31 (4): 7 or Panther in this camera's view (inside the frustum with a clear line to them) - the montage must show neither.</summary>
        void UsInFrame(string shot)
        {
            var cam = Cam.GetComponent<Camera>(); if (cam == null) return; _usChecked++;
            var planes = GeometryUtility.CalculateFrustumPlanes(cam); int seen = 0;
            foreach (var root in new[] { Seven != null ? Seven.transform : null, Cat != null ? Cat.transform : null })
                if (root != null) foreach (var r in root.GetComponentsInChildren<Renderer>()) if (r.enabled && r.gameObject.activeInHierarchy && GeometryUtility.TestPlanesAABB(planes, r.bounds) && ChapterCamera.ClearLine(cam.transform.position, r.bounds.center)) seen++;
            if (seen > 0) { _usSeen++; Log($"RECEIVED MONTAGE (QF31 4) {shot}: 7 / Panther IN FRAME ({seen} renderers) - FAIL"); }
        }
        /// <summary>QF31 (4): the world stills were captured with 7, Panther, their bike and the saucer hidden and checked at capture
        /// (Resources/Finale/Received/received_check.json, written by the capture run) - every still must be listed with 0.</summary>
        void ReceivedCheckStills()
        {
            var ta = Resources.Load<TextAsset>("Finale/Received/received_check");
            var names = new[] { "rec_1_venus", "rec_2_mars", "rec_3_europa", "rec_4_asteroid", "rec_5_mothership", "rec_6_cattalus", "rec_7_cattalus_meadow" };
            int bad = 0; var notes = new List<string>();
            foreach (var n in names)
            {
                var m = ta != null ? System.Text.RegularExpressions.Regex.Match(ta.text, "\"" + n + "\"\\s*:\\s*\\{[^}]*\"us_in_frame\"\\s*:\\s*(\\d+)[^}]*\"source\"\\s*:\\s*\"([^\"]*)\"") : null;
                if (m == null || !m.Success) { bad++; notes.Add(n + ": not checked"); continue; }
                int k = int.Parse(m.Groups[1].Value); if (k > 0) bad++;
                notes.Add($"{n}: 7/Panther {k} ({m.Groups[2].Value})");
            }
            Log($"RECEIVED MONTAGE (QF31 4): the world stills - {string.Join("; ", notes)} - failing {bad} (must be 0)");
        }

        // ------------------------------------------------------------------------------------------ A5 / A6: the light falls; they rise together
        IEnumerator RisenTogether()
        {
            yield return Cutscene(true);
            NoHud(true);
            QF30Watch.Free("FIN_13 - the light, the rise (scripted)");
            if (Cat != null && Cat.transform.parent != null) Cat.transform.SetParent(null, true);
            var rest = Seven.GetComponent<SevenRestPose>();
            if (rest == null || rest.Pose != SevenRestPose.Kind.Kneel) { KneelWithHim(); rest = Seven.GetComponent<SevenRestPose>(); rest.Bow = 24f; }
            if (MusicSystem.Current != "moon_transfer") { MusicSystem.EndSilence(); MusicSystem.Play("moon_transfer", 2f); }
            MusicSystem.Level = 0.6f;
            if (Crew != null) for (int i = 0; i < Crew.Length; i++) if (Crew[i] != null && !Crew[i].gameObject.activeSelf) { Crew[i].gameObject.SetActive(true); Crew[i].transform.position = L.CrewAt(i); Crew[i].transform.rotation = Quaternion.LookRotation(Flat(L.BarrierAt - L.CrewAt(i))); }
            if (Worker != null) { Worker.gameObject.SetActive(true); Worker.transform.position = new Vector3(11.5f, L.Y, -150.9f); Worker.transform.rotation = Quaternion.LookRotation(Flat(P7 - Worker.transform.position)); }
            Restrained(true);
            // the light simply begins to fall on her (no voice in the chamber); she lifts her head toward it
            var colAt = new Vector3(P7.x, L.Y, P7.z);
            var beam = AscentColumn(colAt);
            if (PaleLight != null) PaleLight.enabled = true;
            var ik = IK; ik.LookPos = colAt + Vector3.up * 12f; ik.LookW = 0f;
            var fwd = Seven.transform.forward; var side = Vector3.Cross(Vector3.up, fwd);
            { var toC = Flat(L.ChamberC - P7).normalized; var pp = Vector3.Cross(Vector3.up, toC); Shoot(P7 + pp * 2.9f + toC * 0.5f + Vector3.up * 1.0f, HerHead + Vector3.down * 0.15f, 36f, 0.01f); }   // side-on to the column's lean: her face as she lifts it, little of the light between   // from the chamber's centre side: the column of light (which leans in from the oculus) never behind her
            for (float t = 0f; t < 1f; t += Time.deltaTime / 3.2f)
            {
                beam.SetFloat("_Intensity", t * 0.13f);
                if (PaleLight != null) { var m = PaleLight.material; if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", t * 0.16f); }
                rest.Bow = Mathf.Lerp(24f, -8f, Mathf.SmoothStep(0f, 1f, (t - 0.4f) / 0.6f)); rest.Sob = Mathf.Lerp(0.4f, 0f, t);
                ik.LookW = Mathf.SmoothStep(0f, 1f, (t - 0.4f) / 0.6f);
                yield return null;
            }
            _sobbing = false;
            ImageGate.Expect(Seven.transform, "7 lifting her head to the light falling on her");
            yield return Shot("f55_the_light_falls_on_her_she_lifts_her_head");
            // a column of pale, warm light on them both (wide)
            Shoot(colAt + new Vector3(-4.8f, 1.6f, 6.2f), colAt + Vector3.up * 2.4f, 54f, 0.01f);
            ImageGate.Expect(Seven.transform, "the column of light on them both");
            yield return Wait(0.8f);
            yield return Shot("f56_a_column_of_light_on_them_both");
            // she draws Panther close in her arms and they rise together, weightless
            HandsOff("she draws him into her arms (FIN_13)");
            rest.RiseTime = 1.6f; rest.Rise();
            yield return CarryLift();
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) yield return null;
            { var toC = Flat(L.ChamberC - P7).normalized; var pp = Vector3.Cross(Vector3.up, toC); Shoot(P7 - pp * 3.0f + toC * 0.4f + Vector3.up * 1.25f, P7 + Vector3.up * 1.15f, 34f, 0.01f); }   // side-on to the column's lean   // from the centre side: the dark wall behind her, not the light
            ImageGate.Expect(Seven.transform, "she holds him close against her");
            yield return Wait(0.6f);
            yield return Shot("f57_she_holds_him_close");
            Seven.BeginFloat();
            var rends = Seven.GetComponentsInChildren<Renderer>().Concat(Cat != null ? Cat.GetComponentsInChildren<Renderer>() : new Renderer[0]).ToArray(); var mpb = new MaterialPropertyBlock();
            var y0 = Seven.transform.position.y; bool fadeStarted = false;
            StartCoroutine(StopWorldAudio(6.5f));
            Shoot(L.ChamberC + new Vector3(-10.4f, 1.0f, 9.0f), colAt + Vector3.up * 2.4f, 58f, 0.01f);   // wide and low: the chamber, the well, the Scree in the doorway
            for (float t = 0f; t < 1f; t += Time.deltaTime / 8f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                var p = Seven.transform.position; p.y = y0 + u * 5.2f; Seven.transform.position = p;
                beam.SetFloat("_Intensity", 0.13f + u * 0.7f);
                if (PaleLight != null) { var m = PaleLight.material; if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", 0.16f + u * 0.45f); }
                if (t > 0.5f) foreach (var r in rends) { if (r == null) continue; r.GetPropertyBlock(mpb); mpb.SetFloat("_Fade", Mathf.SmoothStep(0f, 1f, (t - 0.5f) / 0.5f)); r.SetPropertyBlock(mpb); }
                // the camera tilts up after them as they rise; the chamber falls away below
                if (t > 0.35f) { var camP = L.ChamberC + new Vector3(-10.4f, 1.0f + (t - 0.35f) * 6f, 9.0f); Shoot(camP, new Vector3(colAt.x, p.y + 0.8f, colAt.z), 58f, 0.01f); }
                if (!_softShot && t > 0.22f) { _softShot = true; ImageGate.Expect(Seven.transform, "they rise together in the light, the chamber below"); StartCoroutine(Shot("f58_they_rise_together_into_the_light")); }
                if (!fadeStarted && t > 0.6f) { fadeStarted = true; StartCoroutine(UI.Fade(1f, 3.4f, 1f)); }
                yield return null;
            }
            yield return Wait(0.8f);
            yield return Shot("f59_fade_through_white");
            MusicSystem.Stop(2f); MusicSystem.Level = 1f;
            HandsOff("they are in the light (FIN_13 -> heaven)");
            StopWalls();
            Log("FIN_13 (QF31 A5/A6): no voice in the chamber - the light fell on her and she lifted her head; a column of pale warm light on them both; she drew him close in her arms and they rose together, weightless, the chamber, the archive's well and the Scree falling away below, the light filling the frame -> white");
            Complete("FIN_13");
            Checkpoint(L.CpEnding);
        }

        // ------------------------------------------------------------------------------------------ A7: heaven
        IEnumerator HeavenQF31()
        {
            bool clips = Heaven != null && Heaven.Length == 6 && !DemoBoot.NoVideo;
            if (!clips) { Log("HEAVEN (QF31 A7): Dan's heaven clips are not in yet - the stand-in: the colour master from 17.6 s, the Voice near its end"); yield return ComeBackTo(); yield break; }
            var vols = FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None); var was = new float[vols.Length];
            for (int i = 0; i < vols.Length; i++) { was[i] = vols[i].weight; vols[i].weight = 0f; }
            UI.SetFade(1f, 1f);
            // two layers for the dissolves, above the game, under the subtitles
            var root = UI.CanvasRoot; var layers = new RawImage[2];
            for (int i = 0; i < 2; i++) { var g = new GameObject("HeavenLayer" + i, typeof(RectTransform)); g.transform.SetParent(root, false); var rt0 = (RectTransform)g.transform; rt0.anchorMin = Vector2.zero; rt0.anchorMax = Vector2.one; rt0.offsetMin = rt0.offsetMax = Vector2.zero; layers[i] = g.AddComponent<RawImage>(); layers[i].raycastTarget = false; layers[i].color = new Color(1f, 1f, 1f, 0f); }
            UI.SetVideo(null, 1f, 0f);   // (keeps the subtitles above the new layers)
            var vps = new VideoPlayer[6]; var rts = new RenderTexture[6];
            for (int i = 0; i < 6; i++) { var go = new GameObject("Heaven" + (i + 1)); go.transform.SetParent(transform, false); var vp = go.AddComponent<VideoPlayer>(); vp.playOnAwake = false; vp.clip = Heaven[i]; vp.renderMode = VideoRenderMode.RenderTexture; vp.audioOutputMode = VideoAudioOutputMode.None; vp.timeUpdateMode = VideoTimeUpdateMode.GameTime; rts[i] = new RenderTexture((int)Heaven[i].width, (int)Heaven[i].height, 0); vp.targetTexture = rts[i]; vp.Prepare(); vps[i] = vp; }
            for (float w = 0f; w < 6f && vps.Any(v => !v.isPrepared); w += Time.deltaTime) yield return null;
            MusicSystem.EndSilence(); MusicSystem.Level = 1f; MusicSystem.Play("cattalus_meadow", 3f);
            var breeze = AudioKit.Loop("cat_meadow_breeze", null, null, 0f, AudioKit.Bus.Ambience); if (breeze != null) StartCoroutine(AudioKit.Fade(breeze, 0.45f, 3f));
            DialogueFraming.VoiceOver = "FIN_14 (heaven, video)"; Reactions.Suppress = true;
            const float dis = 1.2f;
            var lens = Heaven.Select(c => (float)c.length).ToArray();
            var rest = FinaleDialogue.Ids("FIN_13_04"); float restLen = rest.Count > 0 && rest[0].Clip != null ? rest[0].Clip.length : 14f;
            float restAt = Mathf.Max(0.5f, lens[4] - dis + lens[5] - restLen - 2.8f);   // from heaven_05's start: the last words land as they're tiny in the wide shot
            AudioSource purr = null;
            for (int i = 0; i < 6; i++)
            {
                var img = layers[i % 2]; img.texture = rts[i]; Fill(img, Heaven[i]);
                vps[i].time = 0; vps[i].Play();
                float t0 = Time.time; bool linesA = false, linesB = false, restSaid = false, shotA = false, shotB = false;
                while (Time.time - t0 < lens[i] - (i < 5 ? dis : 0f))
                {
                    float t = Time.time - t0, k = Mathf.Clamp01(t / dis);
                    img.color = new Color(1f, 1f, 1f, i == 0 ? 1f : k); layers[(i + 1) % 2].color = new Color(1f, 1f, 1f, i == 0 ? 0f : 1f - k);
                    if (i == 0 && t < 0.1f) StartCoroutine(UI.Fade(0f, 2.4f, 1f));   // out of the white, onto his eye opening
                    if (!shotA && t > Mathf.Min(1.5f, lens[i] * 0.3f)) { shotA = true; StartCoroutine(Shot($"f60_heaven_{i + 1}_video_frame_a")); }
                    if (!shotB && t > lens[i] * 0.7f) { shotB = true; StartCoroutine(Shot($"f60_heaven_{i + 1}_video_frame_b")); }
                    if (i == 2 && !linesA && t > 0.8f) { linesA = true; StartCoroutine(SayIds("FIN_14_01")); }      // Panther?
                    if (i == 2 && !linesB && t > Mathf.Min(4.2f, lens[i] * 0.55f)) { linesB = true; StartCoroutine(SayIds("FIN_14_02")); if (purr == null) purr = Purr.Loop(Seven.transform, 0.6f); }   // Hello.
                    if (i == 3 && t < 0.1f) { AudioKit.Play("grass_run", null, 0.6f, 0.03f, AudioKit.Bus.Sfx); PlayReaction("seven", "laugh_soft"); }
                    if (i == 4 && !restSaid && t > restAt) { restSaid = true; StartCoroutine(SayIds("FIN_13_04")); Log($"HEAVEN (QF31 A7): the Voice over the rising wide shot ({restLen:0.0} s, timed to land as they are tiny in heaven_06)"); }
                    if (i == 5 && !restSaid && t + lens[4] - dis > restAt) { restSaid = true; StartCoroutine(SayIds("FIN_13_04")); }
                    yield return null;
                }
                if (i < 5) { /* the next clip dissolves in over this one's last second */ }
                if (i == 0) QF30Watch.AudioAudit("heaven (FIN_14)", EndingSounds);
            }
            // the last frame held: the sunrise, them tiny beneath the tree
            vps[5].Pause();
            while (Dlg.Busy) yield return null;
            yield return Shot("f66_heaven_the_final_image_held");
            yield return Wait(Auto ? 3f : 6f);
            MusicSystem.Stop(6f);
            yield return UI.Fade(1f, 3f, 0f);
            foreach (var l in layers) Destroy(l.gameObject); for (int i = 0; i < 6; i++) { vps[i].Stop(); Destroy(vps[i].gameObject); rts[i].Release(); }
            yield return Wait(2f);
            if (purr != null) StartCoroutine(AudioKit.Fade(purr, 0f, 2.5f, true));
            if (breeze != null) StartCoroutine(AudioKit.Fade(breeze, 0f, 2.5f, true));
            Reactions.Suppress = false; DialogueFraming.VoiceOver = null;
            for (int i = 0; i < vols.Length; i++) if (vols[i] != null) vols[i].weight = was[i];
            Log("HEAVEN (QF31 A7): Dan's six colour clips with soft dissolves - his eye opening, hers, the reunion ('Panther?' / 'Hello.'), the run, the tree, the rising wide sunrise with the Voice over it; held, then the credits");
            Complete("FIN_14");
        }

        // ------------------------------------------------------------------------------------------ C10: the stun proof (-stuntest n)
        /// <summary>n stuns exactly as a telekinetic hit gives them, from a random spot 5-9 m away round him (the arena: chamber or
        /// cavern); the boss pilot plays her like a player (walks / runs in, strikes). A try counts when one of her blows lands while he is
        /// stunned. Captured from the player's camera. His energy is put back between tries (the phases never end mid-test).</summary>
        IEnumerator StunTest(int n)
        {
            yield return Wait(3f);
            int ok = 0; var rnd = new System.Random(31); var firsts = new List<float>();
            BossPilot.StunTestMode = true;
            for (int k = 0; k < n; k++)
            {
                // a clean start: he is up (the rig risen, idle), she is on her feet, a moment's gap
                for (float w = 0f; w < 12f && (_fight.State != BossFight.St.Idle || (Rig != null && Rig.Dropped) || Seven.Act == ChapterSeven.FightAct.Down || Dlg.Busy); w += Time.deltaTime) yield return null;
                for (float w = 0f; w < 1.0f; w += Time.deltaTime) yield return null;
                _fight.TestSetEnergy(1f);
                float ang = (float)rnd.NextDouble() * 360f, dist = 5f + (float)rnd.NextDouble() * 4f;
                var p = L.ClampArena(_fight.transform.position + Quaternion.Euler(0f, ang, 0f) * Vector3.forward * dist, 1.4f, 1.0f);
                p.y = Ground.Height(p.x, p.z); Seven.transform.SetPositionAndRotation(p, Quaternion.LookRotation(Flat(_fight.transform.position - p))); Cam.SnapBehind();   // (not Place(): that releases her out of the fight)
                int r0 = _fight.StunReached; float t0 = Time.time; float d0 = Flat(_fight.transform.position - p).magnitude;
                _fight.TestStun($"the stun test, try {k + 1}");
                StartCoroutine(StunTryShots(k));
                string mid = null;
                var trace = new List<string>(); float nextT = 0.5f;
                while (Time.time - t0 < _fight.Def.stunTime + 0.3f && _fight.StunReached == r0)
                {
                    if (mid == null && Time.time - t0 > 1.5f) mid = $"{Flat(_fight.transform.position - P7).magnitude - (Rig != null ? Rig.Radius : 0f):0.0} m from his rim, {Seven.Current}/{Seven.Act}, '{BossPilot.LastWhy}', {_fight.State}";
                    if (Time.time - t0 > nextT && trace.Count < 12) { nextT += 0.5f; trace.Add($"{Time.time - t0:0.0}s her ({P7.x:0.0},{P7.z:0.0}) rim {Flat(_fight.transform.position - P7).magnitude - (Rig != null ? Rig.Radius : 0f):0.00} move ({BossPilot.LastMove.x:0.0},{BossPilot.LastMove.z:0.0}) {Seven.Act} '{BossPilot.LastWhy}' obstacles {Ground.Obstacles.Count}"); }
                    yield return null;
                }
                bool hit = _fight.StunReached > r0; if (hit) { ok++; firsts.Add(Time.time - t0); }
                float dEnd = Flat(_fight.transform.position - P7).magnitude - (Rig != null ? Rig.Radius : 0f);
                Log($"STUN TEST (QF31 10) try {k + 1}/{n}: (1.5 s in: {mid ?? "-"}; at the end: {dEnd:0.0} m from his rim, she {Seven.Current}/{Seven.Act}, the pilot '{BossPilot.LastWhy}', paused {_fight.Paused}) she started {d0:0.0} m from his centre ({(L.InCavern(p) ? "in the cavern" : "in the chamber")}) - {(hit ? $"reached him and her blow landed {Time.time - t0:0.0} s into the {_fight.Def.stunTime:0.0} s stun" : "DID NOT REACH HIM")}; the rig's near edge {_fight.NearEdge():0.00} m");
                if (hit && Shots) yield return Shot($"stun_test_{k + 1:00}_b_her_blow_lands");
                if (!hit) Log($"STUN TEST (QF31 10) try {k + 1} trace (him at ({_fight.transform.position.x:0.0},{_fight.transform.position.z:0.0})): " + string.Join(" | ", trace));
                for (float w = 0f; w < 8f && (_fight.State == BossFight.St.Staggered || _fight.State == BossFight.St.Dazed); w += Time.deltaTime) yield return null;
            }
            Log($"STUN TEST (QF31 10): {ok}/{n} - stun -> reach -> her blows land (first blow {(firsts.Count > 0 ? firsts.Average() : 0f):0.0} s after the stun on average, the window {_fight.Def.stunTime:0.0} s); walk-on, no jump; {_fight.Whiffs} blows whiffed");
            if (Cap != null) Cap.Finish($"STUN TEST (QF31 10): {ok}/{n}");
        }
        IEnumerator StunTryShots(int k)
        {
            if (!Shots) yield break;
            for (float t = 0f; t < 1.0f; t += Time.deltaTime) yield return null;
            ImageGate.Expect(TunnelerCraft, "the rig settled flush, stunned (the player's camera)");
            yield return Shot($"stun_test_{k + 1:00}_a_the_rig_settles_flush");
        }

        static void Fill(RawImage img, VideoClip c)
        {
            float screen = Screen.width / (float)Mathf.Max(1, Screen.height), asp = c.width / (float)Mathf.Max(1, c.height);
            float w = Mathf.Min(1f, screen / asp), h = Mathf.Min(1f, asp / screen);
            img.uvRect = new Rect((1f - w) * 0.5f, (1f - h) * 0.5f, w, h);
        }
    }
}
