using System.Collections;
using UnityEngine;
using L = Seven.MarsLayout;

namespace Seven
{
    /// <summary>
    /// QF17 (1): Viper's fight on the island plaza (spec 7, rebuilt to the "Boss fights" rule). Numbers: boss "viper" in
    /// Resources/Bosses/boss_tuning.json.
    ///  - Phase 1 "from his platform": he stands on his raised platform in front of the temple steps and works his
    ///    machinery - venom spit (arcing dark globs that leave burning puddles), the rotating energy sweep from the platform
    ///    (jump it), barriers that slam up round her (then a spit into the cage). She shuts his three pylons down under
    ///    fire - QF19: ONE press of X in reach (3 m) each, instant (bang, sparks, its power beam to the platform dies, the
    ///    panels blow open). QF39 (2, Dan): all THREE pylons, all required - "Pylons 0 / 3" -> "3 / 3" (QF19 had dropped the
    ///    third to make the counter right; Dan wanted the counter fixed, not a pylon removed). All three down: the platform
    ///    sinks, he drops.
    ///  - Phase 2 "he hunts her": he comes down and hunts her - the snake strike (coils, hood flares, a hiss, then a 7 m
    ///    lunge: dodge sideways or block), the tail / arm sweep when she is close (jump or block), spit (QF19: no machine
    ///    sweep any more - that machinery went down with the platform). A wasted attack leaves him open: a telekinetic throw of debris (or three strikes)
    ///    staggers him. Two staggers.
    ///  - Phase 3 "enraged": faster, combos (strike -> spit, sweep -> strike), three staggers - then dazed; her takedown pins
    ///    him (the containment ring, alive, left for local custody).
    /// Animation: Dan's Mixamo rig with the reused clips (tunneler roar / swipe / walk / idle, the YBot fight idle, punch,
    /// hit, dazed, knocked down, get up) + procedural coil / lunge / spit on his spine, neck and head, the hood flare.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public class ViperFight : BossFight
    {
        public MarsFolk Folk;
        public Transform Dais;                 // his raised platform (sinks at the end of phase 1)
        public Renderer[] PylonGlow;
        public MarsPylon[] PylonObjs;           // QF18 (19b): the pylons' done state (light dies, panels fall open)
        public System.Action<int> PylonCount;   // the counter "Pylons n / 3" changed (a hint; the objective text comes from BossObjectives)
        public bool[] PylonOff = new bool[Required];
        /// <summary>QF39 (2): all three pylons (MarsLayout.Pylons), all required - build check BOSS OBJECTIVES (QF39 2).</summary>
        public const int Required = 3;
        /// <summary>QF39 (2): the real number of objectives in each phase (the build check compares the text and counter with it).</summary>
        public static readonly int[] ObjectivesPerPhase = { Required, 0, 0 };
        public override int ObjectivesDone(int phase) => phase == 0 ? PylonsDown : 0;
        public int PylonsDown { get { int n = 0; for (int i = 0; i < Required; i++) if (PylonOff[i]) n++; return n; } }
        public System.Func<IEnumerator> Beat;  // the director's camera beat between phases (optional)
        public const float DaisTop = 3.2f;
        public static readonly Vector3 DaisAt = new Vector3(0f, 0f, 158.5f);
        public static readonly Vector3 HerStart = new Vector3(0f, 0f, 131f), HerStart2 = new Vector3(0f, 0f, 136f), HimStart2 = new Vector3(0f, 0f, 152f);
        Transform _spine, _chest, _neck, _head;
        Animator _an;
        bool _onDais = true;
        GameObject[] _globs;

        public void Setup(MarsFolk folk, Transform dais, Renderer[] pylons, Material rockMat)
        {
            Folk = folk; Dais = dais; PylonGlow = pylons;
            _an = folk.Anim;
            if (_an != null && _an.isHuman)
            {
                _spine = _an.GetBoneTransform(HumanBodyBones.Spine); _chest = _an.GetBoneTransform(HumanBodyBones.Chest);
                _neck = _an.GetBoneTransform(HumanBodyBones.Neck); _head = _an.GetBoneTransform(HumanBodyBones.Head);
            }
            // debris: broken masonry from the old lane walls, lying about the plaza (never a box)
            var spots = new[] { new Vector3(-15f, 0f, 140f), new Vector3(-4.5f, 0f, 133f), new Vector3(9.5f, 0f, 137.5f), new Vector3(16f, 0f, 149f), new Vector3(-12.5f, 0f, 153.5f), new Vector3(3.5f, 0f, 146.5f), new Vector3(-17.5f, 0f, 128.5f), new Vector3(16.5f, 0f, 128f) };
            for (int i = 0; i < spots.Length; i++)
            {
                var g = new GameObject("PlazaDebris" + i);
                g.AddComponent<MeshFilter>().sharedMesh = Markers.Rock(40 + i, new Vector3(0.45f, 0.32f, 0.38f));
                g.AddComponent<MeshRenderer>().sharedMaterial = rockMat;
                g.transform.position = spots[i] + Vector3.up * 0.28f; g.transform.rotation = Quaternion.Euler(i * 37f, i * 71f, 0f);
                AddDebris(g.transform);
            }
            _globs = new GameObject[4];
            for (int i = 0; i < _globs.Length; i++)
            {
                var g = new GameObject("VenomGlob" + i);
                g.AddComponent<MeshFilter>().sharedMesh = Markers.Rock(90 + i, Vector3.one * 0.22f);
                g.AddComponent<MeshRenderer>().sharedMaterial = Markers.DarkGloss();
                g.SetActive(false); _globs[i] = g;
            }
        }

        // ---------------------------------------------------------------- phases + objectives
        protected override bool CanMove => !_onDais;
        protected override bool Reachable => !_onDais;
        protected override bool ObjectiveDone => Phase != 0 || PylonsDown >= Required;
        public override (Vector3 at, string how)? PilotObjective()
        {
            if (Phase != 0) return null;
            for (int i = 0; i < Required; i++) if (!PylonOff[i]) return (L.Pylons[i], "use");
            return null;
        }
        int InReach()
        {
            if (Phase != 0) return -1;
            int best = -1; float bd = 1e9f;
            for (int i = 0; i < Required; i++) { if (PylonOff[i] || !NearUse(L.Pylons[i])) continue; float d = Flat(L.Pylons[i] - Her).magnitude; if (d < bd) { bd = d; best = i; } }
            return best;
        }
        protected override string ObjectiveUseLabel() => InReach() >= 0 ? "Shut down pylon" : null;
        /// <summary>QF19: one press, instant - nothing can interrupt it.</summary>
        protected override void ObjectiveUse() { int i = InReach(); if (i >= 0) PylonDown(i, true); }
        void PylonDown(int i, bool byHer)
        {
            if (PylonOff[i]) return;
            PylonOff[i] = true;
            float dist = Flat(L.Pylons[i] - Her).magnitude;
            if (PylonObjs != null && i < PylonObjs.Length && PylonObjs[i] != null) PylonObjs[i].Collapse();
            else if (PylonGlow != null && i < PylonGlow.Length && PylonGlow[i] != null) { var m = PylonGlow[i].material; m.SetColor("_EmissionColor", Color.black); }
            PylonCount?.Invoke(PylonsDown);
            if (byHer) NudgeCamera(L.Pylons[i] + Vector3.up * 1.6f);
            Log($"boss viper: PYLON (QF19) {i + 1} shut down {(byHer ? "by her X" : "(skip)")} - X presses on the pad at a pylon so far {PadUsePresses} (went to a strike instead {PadUseToStrike}); she is {dist:0.0} m from it; his attack now: {(Cur != null && State != St.Idle ? Cur.id + " " + State : "none")}; bang + sparks, the light and its power beam die, the panels blow open, the prompt is gone; Pylons {PylonsDown} / {Required}");
            if (byHer && DemoBoot.CaptureDir != null && Cap != null) { StartCoroutine(Shoot($"boss_viper_pylon{i + 1}_shutdown", 0.15f)); StartCoroutine(Shoot($"boss_viper_pylon{i + 1}_after", 1.4f)); }
            if (Folk != null) Folk.Flare = 1f;
            ObjectiveProgress();
        }
        protected override void SkipObjectives() { for (int i = 0; i < Required; i++) if (!PylonOff[i]) PylonDown(i, false); }
        /// <summary>QF19: the energy sweep is the platform's machinery - only while he is up there (the pylons feed it).</summary>
        protected override bool Allowed(AttackDef a) => !(a.id == "machine_sweep" && !_onDais);

        protected override IEnumerator PhaseBeat(int ended)
        {
            if (ended == 0)
            {
                // his machinery fails: the platform sinks, he drops down onto the plaza and roars
                AudioKit.Play("mars_platform_rise", Dais != null ? Dais.position : DaisAt, 1f, 0f, AudioKit.Bus.Sfx, 80f);
                if (Beat != null) StartCoroutine(Beat());
                var top = transform.position;
                for (float t = 0f; t < 1f; t += Time.deltaTime / 1.6f)
                {
                    float u = Mathf.SmoothStep(0f, 1f, t);
                    if (Dais != null) Dais.position = DaisAt + Vector3.down * DaisTop * u;
                    transform.position = top + Vector3.down * DaisTop * u;
                    Folk.Play("idle");
                    yield return null;
                }
                _onDais = false;
                var land = DaisAt + new Vector3(0f, 0f, -4.5f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f) { transform.position = Vector3.Lerp(new Vector3(DaisAt.x, 0f, DaisAt.z), land, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 1.4f; yield return null; }
                transform.position = land;
                Folk.Play("roar", 0.15f); Folk.Flare = 1f;
                CameraShake.Quake(0.5f, 0.25f);
                AudioKit.Play("land_hard", land, 1f);
                for (float t = 0f; t < 1.6f; t += Time.deltaTime) yield return null;
            }
            else
            {
                // enraged: the hood stays open, a roar
                Folk.Play("roar", 0.15f); Folk.Flare = 1f;
                AudioKit.Play("mars_defense_arm", transform.position, 1f, 0f, AudioKit.Bus.Sfx, 80f);
                CameraShake.Quake(0.4f, 0.2f);
                for (float t = 0f; t < 1.8f; t += Time.deltaTime) yield return null;
            }
        }
        protected override void ResetPhase()
        {
            if (Phase == 0) { transform.position = DaisAt + Vector3.up * DaisTop; Seven.Place(HerStart, Vector3.forward); }
            else { transform.position = HimStart2; Seven.Place(HerStart2, Vector3.forward); }
            transform.rotation = Quaternion.LookRotation(Vector3.back);
            foreach (var g in _globs) if (g != null) g.SetActive(false);
        }
        public void PlaceStart() { _onDais = true; if (Dais != null) Dais.position = DaisAt; transform.position = DaisAt + Vector3.up * DaisTop; transform.rotation = Quaternion.LookRotation(Vector3.back); }

        protected override Vector3 ClampBoss(Vector3 p)
        {
            if (_onDais) return new Vector3(DaisAt.x, DaisTop, DaisAt.z);
            p.x = Mathf.Clamp(p.x, -19.5f, 19.5f); p.z = Mathf.Clamp(p.z, 125.5f, 166f);
            return p;
        }
        protected override Vector3 Origin(AttackDef a)
        {
            // the machinery's sweep comes from the platform (phase 1 only, QF19)
            if (a.id == "machine_sweep") return DaisAt;
            return transform.position;
        }
        protected override Vector3 Centre => transform.position + Vector3.up * 1.5f;

        // ---------------------------------------------------------------- the attacks' visuals
        protected override void AttackStarted(AttackDef a)
        {
            if (a.kind == "spit") for (int i = 0; i < _globs.Length; i++) _globs[i].SetActive(i < SpitTargets.Count);
        }
        protected override void AttackFx(AttackDef a, float u)
        {
            if (a.kind == "spit")
            {
                var mouth = _head != null ? _head.position + transform.forward * 0.25f : Centre + Vector3.up * 0.6f;
                for (int i = 0; i < _globs.Length; i++)
                {
                    if (i >= SpitTargets.Count) { _globs[i].SetActive(false); continue; }
                    var q = SpitTargets[i];
                    float k = Mathf.Clamp01(u * (1f + i * 0.06f));
                    _globs[i].transform.position = Vector3.Lerp(mouth, q, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * (2.4f + Flat(q - mouth).magnitude * 0.12f);
                    _globs[i].transform.Rotate(400f * Time.deltaTime, 0f, 260f * Time.deltaTime);
                    if (u >= 1f) _globs[i].SetActive(false);
                }
            }
        }
        protected override void Arena(float dt) { }

        // ---------------------------------------------------------------- animation: clips + procedural coil / strike / spit
        float _coil, _strike, _rear, _jab;
        protected override void Anim(St st, AttackDef a, float u, bool moving, float speed)
        {
            if (Folk == null) return;
            _coil = _strike = _rear = _jab = 0f;
            switch (st)
            {
                case St.Idle:
                    if (moving) { Folk.Play("walk", 0.2f); if (_an != null) _an.speed = Mathf.Clamp(speed / Mathf.Max(0.3f, Folk.ClipSpeed), 0.6f, 1.8f); }
                    else { Folk.Play(Phase > 0 && Folk.HasState("fight_idle") ? "fight_idle" : "idle", 0.25f); if (_an != null) _an.speed = 1f; }
                    Folk.Flare = Mathf.MoveTowards(Folk.Flare, Phase >= 2 ? 0.7f : 0f, Time.deltaTime * 2f);
                    break;
                case St.Telegraph:
                    if (_an != null) _an.speed = 1f;
                    switch (a.kind)
                    {
                        case "lunge": Folk.Play("roar", 0.2f); _coil = u; Folk.Flare = u; break;                 // he coils, the hood flares
                        case "spit": Folk.Play(Folk.HasState("fight_idle") ? "fight_idle" : "idle", 0.2f); _rear = u; Folk.Flare = u; break;
                        case "sweep": Folk.Play(Folk.HasState("swipe") ? "swipe" : "point", 0.15f); if (_an != null && Folk.State == "swipe") _an.speed = 0.55f; _coil = u * 0.4f; break;
                        default: Folk.Play("point", 0.2f); Folk.Flare = u * 0.5f; break;                          // he works the machinery
                    }
                    break;
                case St.Active:
                    switch (a.kind)
                    {
                        case "lunge": Folk.Play(Folk.HasState("punch") ? "punch" : "roar", 0.05f); _strike = 1f - u * 0.5f; Folk.Flare = 1f; break;
                        case "spit": _jab = Mathf.Sin(Mathf.Clamp01(u * 3f) * Mathf.PI); Folk.Flare = 1f; break;
                        case "sweep": if (_an != null) _an.speed = 1.6f; break;
                    }
                    break;
                case St.Recover:
                    Folk.Play(Phase > 0 && Folk.HasState("fight_idle") ? "fight_idle" : "idle", 0.3f); if (_an != null) _an.speed = 1f;
                    _strike = a != null && a.kind == "lunge" ? Mathf.Clamp01(0.5f - u) : 0f;
                    Folk.Flare = Mathf.MoveTowards(Folk.Flare, 0f, Time.deltaTime * 1.5f);
                    break;
                case St.Staggered: Folk.Play(u < 0.35f ? "hit" : "dazed", 0.12f); if (_an != null) _an.speed = 1f; Folk.Flare = 0f; break;
                case St.Dazed: Folk.Play("dazed", 0.3f); Folk.Flare = 0f; break;
                case St.Pinned: Folk.Play("down", 0.25f); Folk.Flare = 0f; break;
            }
        }
        protected override void LateUpdate()
        {
            base.LateUpdate();
            // after the animator and MarsFolk's own LateUpdate (execution order 60): the coil, the strike, the spit
            void Bend(Transform b, float deg) { if (b != null && Mathf.Abs(deg) > 0.01f) b.rotation = Quaternion.AngleAxis(deg, transform.right) * b.rotation; }
            Bend(_spine, -18f * _coil + 22f * _strike - 10f * _rear + 8f * _jab);
            Bend(_chest, -14f * _coil + 18f * _strike - 12f * _rear + 10f * _jab);
            Bend(_neck, 20f * _coil - 10f * _strike - 16f * _rear + 18f * _jab);
            Bend(_head, 10f * _coil - 6f * _strike - 10f * _rear + 14f * _jab);
            // a slow serpentine sway while he is free
            if (!_onDais && _spine != null && State == St.Idle) _spine.rotation = Quaternion.AngleAxis(Mathf.Sin(Time.time * 2.2f) * 6f, transform.up) * _spine.rotation;
        }
    }
}
