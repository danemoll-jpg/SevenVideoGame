using System.Collections;
using UnityEngine;
using L = Seven.AsteroidLayout;

namespace Seven
{
    /// <summary>
    /// Round 16 — AST_07: the Machinist's fight in the central maintenance hall (the "Boss fights" rule + Dan's Decisions).
    /// Numbers: boss "machinist" in Resources/Bosses/boss_tuning.json. He tries to RESTRAIN her and restore the recycling
    /// line - nonlethal for both: nothing of him is destroyed, his tools are locked away, he is immobilized in his cradle.
    ///  - Phase 1 "the cutter arm": the cutter swing (jump / block / dodge back), his restraint grab (a long reach along the
    ///    marked line - dodge sideways; it can't be blocked), the floor slam (a shock ring - jump).
    ///  - Phase 2 "the other arm": the cutter is clamped away - the clamp arm swings instead; he starts the conveyor lanes
    ///    (a surge down a lane - jump or step off it).
    ///  - Phase 3 "the override": both tools are locked away; the hoppers drop scrap on marked spots, his override runs from
    ///    the terminal as a sweep of power across the floor, combos.
    /// The loop: an attack he wastes leaves him open -> telekinesis (scrap, a cargo canister) STUNS him -> close in: the arc
    /// blades on his glowing power couplings (+ punches / kicks) do the real damage -> he recovers and fights back. Each
    /// phase ends at its energy mark: he reels into a service station and its clamp locks the tool arm (stations 1 and 2);
    /// the last ends dazed - her takedown (Y) engages the maintenance cradle (AST_07_14 / 15). He keeps his damage on a retry.
    /// Animation: Dan's Mixamo rig with reused clips (tunneler idle / walk / roar / swipe, the YBot stance / punch / hit /
    /// dazed / knocked down, the Elarim talk / point) slowed for weight, plus procedural reach and slam on his spine / arms.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public class MachinistFight : BossFight
    {
        public MarsFolk Folk;
        public MachinistRig Rig;
        public AstClamp[] StationClamps;
        public Transform Terminal;
        public System.Func<int, IEnumerator> Beat;     // the director's camera beat between phases (Panther's lines)
        Transform _spine, _chest, _armR, _armL;
        Animator _an;

        public Mesh[] DebrisKit;
        public void Setup(MarsFolk folk, MachinistRig rig, AstClamp[] clamps, Transform terminal, Material scrapMat, Material canisterMat, Mesh canister)
        {
            Folk = folk; Rig = rig; StationClamps = clamps; Terminal = terminal;
            _an = folk.Anim;
            if (_an != null && _an.isHuman)
            {
                _spine = _an.GetBoneTransform(HumanBodyBones.Spine); _chest = _an.GetBoneTransform(HumanBodyBones.Chest);
                _armR = _an.GetBoneTransform(HumanBodyBones.RightUpperArm); _armL = _an.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            }
            // things to throw: scrap gears and cargo canisters lying about the hall floor (never a box)
            var spots = new[] { new Vector3(-384f, 0f, 90f), new Vector3(-372f, 0f, 78f), new Vector3(-348f, 0f, 76f), new Vector3(-336f, 0f, 96f),
                                new Vector3(-352f, 0f, 108f), new Vector3(-378f, 0f, 104f), new Vector3(-392f, 0f, 98f), new Vector3(-326f, 0f, 86f), new Vector3(-364f, 0f, 112f) };
            for (int i = 0; i < spots.Length; i++)
            {
                var g = new GameObject("HallScrap" + i);
                bool can = i % 3 == 0 && canister != null;
                // QF21 (10): loose gears, pipe sections, gauge plates and small crates (clearly loose), canisters among them
                var dm = DebrisKit != null && DebrisKit.Length > 0 ? DebrisKit[i % DebrisKit.Length] : null;
                g.AddComponent<MeshFilter>().sharedMesh = can ? canister : dm != null ? dm : Markers.Rock(60 + i, new Vector3(0.42f, 0.22f, 0.42f));
                g.AddComponent<MeshRenderer>().sharedMaterial = can ? canisterMat : scrapMat;
                g.transform.position = spots[i] + Vector3.up * (can ? 0.02f : dm != null ? 0.01f - dm.bounds.min.y : 0.2f); g.transform.rotation = Quaternion.Euler(can || dm != null ? 0f : i * 37f, i * 71f, 0f);
                AddDebris(g.transform);
            }
        }

        // ---------------------------------------------------------------- phases
        public int ToolsLocked { get; private set; }
        protected override bool Allowed(AttackDef a)
        {
            if (a.id == "cutter_swing") return ToolsLocked < 1;
            if (a.id == "clamp_swing") return ToolsLocked < 2;
            return true;
        }
        protected override IEnumerator PhaseBeat(int ended)
        {
            // he reels back into a service station; its clamp slams shut on the tool arm and keeps it
            int si = Mathf.Clamp(ended, 0, StationClamps != null ? StationClamps.Length - 1 : 0);
            var st = StationClamps != null && si < StationClamps.Length ? StationClamps[si] : null;
            var to = st != null ? st.transform.position + (Flat(transform.position - st.transform.position).normalized * 2.2f) : transform.position;
            to.y = Ground.Height(to.x, to.z);
            var from = transform.position;
            Folk.Play(Folk.HasState("hit") ? "hit" : "idle", 0.1f);
            if (Rig != null) Rig.Stunned = true;
            if (Beat != null) StartCoroutine(Beat(ended));
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.1f) { transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)); if (st != null) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Flat(transform.position - st.transform.position)), t); yield return null; }
            if (st != null)
            {
                var tool = ended == 0 ? Rig?.CutterTool : Rig?.ClampTool;
                if (tool != null) StartCoroutine(Rig.ToolInto(tool, st.JawA != null ? st.JawA.parent : st.transform));
                yield return st.Close();
                ToolsLocked++;
                Log($"boss machinist: PHASE {ended + 1} ends - service station {si + 1}'s clamp locks his {(ended == 0 ? "cutter" : "clamp tool")} arm assembly (kept, not broken); tools locked {ToolsLocked} / 2");
                if (DemoBoot.CaptureDir != null && Cap != null) StartCoroutine(Shoot($"boss_machinist_station{si + 1}_clamp_locks", 0.1f));
            }
            for (float t = 0f; t < 1.4f; t += Time.deltaTime) yield return null;
            if (Rig != null) Rig.Stunned = false;
            Folk.Play("roar", 0.2f);
            AudioKit.Play("ast_servo_heavy", transform.position, 1f, 0f, AudioKit.Bus.Sfx, 80f);
            for (float t = 0f; t < 1.4f; t += Time.deltaTime) yield return null;
        }
        protected override void ResetPhase()
        {
            // QF20: positions only - he backs off to his side of the hall; she gets up where the fight began
            transform.position = ClampBoss(L.MachinistStart); transform.rotation = Quaternion.LookRotation(Vector3.back);
            Seven.Place(L.HerFightStart, Vector3.forward);
        }
        public void PlaceStart() { transform.position = L.MachinistStart; transform.rotation = Quaternion.LookRotation(Vector3.back); }
        protected override Vector3 ClampBoss(Vector3 p)
        {
            p.x = Mathf.Clamp(p.x, L.ArenaX0 + 3f, L.ArenaX1 - 3f); p.z = Mathf.Clamp(p.z, L.ArenaZ0 + 3f, L.ArenaZ1 - 2f);
            return p;
        }
        protected override Vector3 Origin(AttackDef a)
        {
            if (a.id == "override_sweep" && Terminal != null) return Terminal.position;
            if (a.id == "lane_surge") { var p = transform.position; return new Vector3(p.x, 0f, Mathf.Round((Her.z - 84f) / 11f) * 11f + 84f); }   // down the lane she is on
            return transform.position;
        }
        protected override Vector3 Centre => transform.position + Vector3.up * 2.6f;
        protected override void StrikeLanded(string blow, bool counted)
        {
            if (Rig == null) return;
            if (ChapterSeven.IsBlade(blow)) { Rig.Flare(Seven.transform.position + Vector3.up * 1.4f); AudioKit.Play("ast_blade_hit", Centre, 0.9f, 0.06f, AudioKit.Bus.Sfx, 30f); }
        }
        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (Rig != null) { Rig.Energy = Energy; Rig.Stunned = State == St.Staggered || State == St.Dazed || State == St.Pinned; Rig.LookAt = Over ? (Vector3?)null : Her + Vector3.up * 1.5f; }
            // procedural reach / slam (after the animator and the rig)
            void Bend(Transform b, Vector3 axis, float deg) { if (b != null && Mathf.Abs(deg) > 0.01f) b.rotation = Quaternion.AngleAxis(deg, axis) * b.rotation; }
            Bend(_spine, transform.right, 14f * _reach - 10f * _wind + 18f * _slam);
            Bend(_chest, transform.right, 10f * _reach - 8f * _wind + 14f * _slam);
            Bend(_armR, transform.right, -55f * _reach * (_left ? 0f : 1f) + 40f * _slam);
            Bend(_armL, transform.right, -55f * _reach * (_left ? 1f : 0f) + 40f * _slam);
        }

        // ---------------------------------------------------------------- animation
        float _reach, _wind, _slam; bool _left;
        protected override void Anim(St st, AttackDef a, float u, bool moving, float speed)
        {
            if (Folk == null) return;
            _reach = _wind = _slam = 0f;
            float slow = 0.82f;     // deliberate, economical: his clips play a little slower
            switch (st)
            {
                case St.Idle:
                    if (moving) { Folk.Play("walk", 0.25f); if (_an != null) _an.speed = Mathf.Clamp(speed / Mathf.Max(0.3f, Folk.ClipSpeed), 0.5f, 1.4f); }
                    else { Folk.Play(Phase > 0 && Folk.HasState("fight_idle") ? "fight_idle" : "idle", 0.3f); if (_an != null) _an.speed = slow; }
                    break;
                case St.Telegraph:
                    if (_an != null) _an.speed = slow;
                    _left = a.id == "clamp_swing" || (a.id == "restrain_grab" && ToolsLocked >= 1);
                    switch (a.kind)
                    {
                        case "sweep": Folk.Play(Folk.HasState("swipe") ? "swipe" : "punch", 0.2f); if (_an != null) _an.speed = 0.5f; _wind = u; break;
                        case "lunge": Folk.Play(Folk.HasState("fight_idle") ? "fight_idle" : "idle", 0.2f); _wind = u; break;
                        case "ring": Folk.Play("roar", 0.2f); _wind = u * 0.6f; break;
                        default: Folk.Play(Folk.HasState("point") ? "point" : "idle", 0.25f); break;   // he works the machinery (lanes, hoppers, the override)
                    }
                    break;
                case St.Active:
                    switch (a.kind)
                    {
                        case "sweep": if (_an != null) _an.speed = 1.5f; break;
                        case "lunge": Folk.Play(Folk.HasState("punch") ? "punch" : "idle", 0.05f); _reach = 1f - Mathf.Abs(u - 0.5f); break;
                        case "ring": _slam = Mathf.Sin(Mathf.Clamp01(u * 2f) * Mathf.PI); if (u < 0.05f) CameraShake.Quake(0.35f, 0.25f); break;
                    }
                    break;
                case St.Recover:
                    Folk.Play(Folk.HasState("fight_idle") ? "fight_idle" : "idle", 0.35f); if (_an != null) _an.speed = slow;
                    _reach = a != null && a.kind == "lunge" ? Mathf.Clamp01(0.6f - u) : 0f;      // overreached - slow to pull back (open)
                    break;
                case St.Staggered: Folk.Play(u < 0.3f ? "hit" : "dazed", 0.15f); if (_an != null) _an.speed = 0.8f; break;
                case St.Dazed: Folk.Play("dazed", 0.3f); if (_an != null) _an.speed = 0.7f; break;
                case St.Pinned: Folk.Play(Folk.HasState("dazed") ? "dazed" : "idle", 0.3f); if (_an != null) _an.speed = 0.4f; break;
            }
        }
    }
}
