using System.Collections;
using UnityEngine;
using L = Seven.FinaleLayout;

namespace Seven
{
    /// <summary>
    /// Round 19 — FIN_08, the short confrontation in the archive chamber (CHAPTER_FINALE.md, Decisions 8; the "Boss fights" rule).
    /// Tunneler on his mining craft: telegraphed drill strikes (a wide sweep - block or dodge), drill charges (a white lane on the
    /// floor - dodge sideways) and debris slams (a ring - jump). A wasted attack leaves him open: a telekinetic throw of the chamber's
    /// loose stonework / mining scrap stuns him (the craft lurches, the drill spins down); she closes in and fights hand to hand.
    /// The fight IS its three objectives, kept short (~2 min): (1) keep him off the platform - her blows drive him back (his bar
    /// to the first mark); (2) the old maintenance barrier - X at its control while he attacks opens the alcove (the Scree elder said
    /// it still has power); (3) he cuts the west conduit - X at its junction box restores it, and the story takes over (FIN_09: the
    /// leap). She can go down: a quick retry, his energy and the objectives done are kept (QF20). Nobody is hurt for good.
    /// Numbers: boss_tuning.json "tunneler".
    /// </summary>
    public class TunnelerFight : BossFight
    {
        public Transform Body;                       // Tunneler on the craft (tilts with his attacks)
        public FinaleDirector Dir;
        public TunnelerRig Rig;                      // QF30 (6/8): his mining rig
        public void AddThrowable(Transform t) => AddDebris(t);
        /// <summary>QF30 (8): stunned, the rig sits on the floor tilted toward her; its rim is a step she jumps onto.</summary>
        public override float StepUp => Rig != null && Rig.Dropped && _settled ? Rig.Radius : 0f;
        bool _wasDown, _settled; public int DeckLandings, DeckStrikes;
        /// <summary>QF30 (8) proof from the player's camera: "drop", "deck", "strike", "rise".</summary>
        public System.Action<string> StunBeat;
        public bool BarrierOpen, ConduitCut, ConduitRestored;
        float _h = 0.6f, _tilt, _yawKick; bool _onDeck;
        Vector3? _distract; float _distractUntil;

        public void Setup(Mesh[] debris, Material mat)
        {
            // things to throw: fallen facing stones and mining scrap on the chamber floor, where they'd have come down
            var spots = new[] { new Vector3(4.4f, 0f, -157f), new Vector3(5.6f, 0f, -167f), new Vector3(9f, 0f, -170.4f), new Vector3(15.2f, 0f, -170.2f), new Vector3(18.6f, 0f, -166f),
                                new Vector3(19.2f, 0f, -157.6f), new Vector3(15.6f, 0f, -153.4f), new Vector3(7.4f, 0f, -153.8f), new Vector3(3.2f, 0f, -162.4f) };
            for (int i = 0; i < spots.Length; i++)
            {
                var g = new GameObject("ChamberDebris" + i);
                var dm = debris != null && debris.Length > 0 ? debris[i % debris.Length] : Markers.Rock(190 + i, new Vector3(0.42f, 0.28f, 0.4f));
                g.AddComponent<MeshFilter>().sharedMesh = dm; g.AddComponent<MeshRenderer>().sharedMaterial = mat;
                g.transform.position = new Vector3(spots[i].x, L.Y, spots[i].z) + Vector3.up * (0.01f - dm.bounds.min.y); g.transform.rotation = Quaternion.Euler(0f, i * 67f, 0f);
                AddDebris(g.transform);
            }
        }
        public void PlaceStart() { transform.position = L.TunnelerStart; transform.rotation = Quaternion.LookRotation(Vector3.forward); }

        // ---------------------------------------------------------------- the arena: inside the wall, never onto the platform, never into the alcove
        protected override Vector3 ClampBoss(Vector3 p)
        {
            var c = L.ChamberC; var d = Flat(p - c);
            float r = d.magnitude, rMin = L.PlatformR + 2.4f, rMax = L.ChamberR - 2.2f;
            if (r < rMin) d = (r < 0.01f ? Vector3.back : d / r) * rMin; else if (r > rMax) d = d / r * rMax;
            var q = c + d; q.y = L.Y;
            if (q.x > L.BarrierAt.x - 3.4f) q.x = L.BarrierAt.x - 3.4f;          // the alcove is for later (FIN_09)
            return q;
        }
        protected override Vector3 SidePos(Vector3 mid, Vector3 side, float dist)
        {
            // inside the round chamber: the side of the line with more room, pulled in to stay 2 m inside the wall
            var c = L.ChamberC; Vector3 best = mid + Vector3.up * 3.2f; float room = -1f;
            dist = Mathf.Min(dist, 7f);
            foreach (var sg in new[] { 1f, -1f }) for (float d = dist; d >= 3f; d -= 1f)
            {
                var p = mid + side * sg * d; if (Flat(p - c).magnitude > L.ChamberR - 2f) continue;
                if (d > room) { room = d; best = p + Vector3.up * 3.2f; } break;
            }
            return best;
        }
        protected override Vector3 Centre => transform.position + Vector3.up * (_h + 1.6f);

        // ---------------------------------------------------------------- the objectives (X at the barrier control, X at the junction box)
        protected override bool ObjectiveDone => Phase == 1 ? BarrierOpen : Phase == 2 ? ConduitRestored : true;
        Vector3 ObjSpot => Phase == 1 ? L.BarrierControl : L.ConduitAt;
        protected override string ObjectiveUseLabel()
        {
            if (Phase == 1 && !BarrierOpen && Flat(L.BarrierControl - Her).magnitude < 3f) return "Open the barrier";
            if (Phase == 2 && ConduitCut && !ConduitRestored && Flat(L.ConduitAt - Her).magnitude < 3f) return "Restore the connection";
            return null;
        }
        public System.Action<string> OnObjective;
        protected override void ObjectiveUse()
        {
            if (Phase == 1 && !BarrierOpen) { BarrierOpen = true; StationLit(false); Log($"boss {Def.id}: the old maintenance barrier opened by X at its control (objective 2 of 3)"); OnObjective?.Invoke("barrier"); ObjectiveProgress(); }
            else if (Phase == 2 && ConduitCut && !ConduitRestored) { ConduitRestored = true; Log($"boss {Def.id}: the west conduit restored by X at its junction box (objective 3 of 3) - FIN_09 begins"); OnObjective?.Invoke("conduit"); FinishByStory("she restored the connection he cut - the leap (FIN_09)"); }
        }
        protected override void SkipObjectives() { if (!BarrierOpen) { BarrierOpen = true; OnObjective?.Invoke("barrier"); } }
        public override (Vector3 at, string how)? PilotObjective()
        {
            if (Phase == 1 && !BarrierOpen) return (L.BarrierControl, "use");
            if (Phase == 2 && ConduitCut && !ConduitRestored) return (L.ConduitAt, "use");
            return null;
        }
        public override string PromptLabel => Paused ? null : ObjectiveUseLabel() ?? (Open && DebrisNear ? "Telekinesis: throw" : null);
        public void ForceBarrierOpen() { BarrierOpen = true; }
        GameObject _beam;
        /// <summary>The place to go while his bar waits on the barrier: a soft beam of light down onto its control (QF23 19).</summary>
        public void StationLit(bool on)
        {
            if (on && _beam == null && !BarrierOpen && false)
            {
                _beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(_beam.GetComponent<Collider>()); _beam.name = "BarrierStationBeam";
                _beam.transform.position = L.BarrierControl + Vector3.up * 8f; _beam.transform.localScale = new Vector3(1.2f, 8f, 1.2f);
                var r = _beam.GetComponent<Renderer>(); r.sharedMaterial = Markers.Additive(0.45f); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (!on && _beam != null) { Destroy(_beam); _beam = null; }
        }

        /// <summary>Panther draws him away from the damaged control once (FIN_08: "Over here, you lump of scrap!").</summary>
        public void Distract(Vector3 toward, float secs) { _distract = toward; _distractUntil = Time.time + secs; Paused = true; Log($"boss {Def.id}: Panther draws him away for {secs:0.0} s"); }

        protected override IEnumerator PhaseBeat(int ended)
        {
            AudioKit.Play("europa_drill", Centre, 1f, 0f, AudioKit.Bus.Sfx, 60f);
            if (Dir != null) Dir.DrillSpeed = 900f;
            // he backs off and wheels round, then goes on
            var a = transform.position; var b = ClampBoss(transform.position + Flat(transform.position - L.ChamberC).normalized * 3f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.1f) { transform.position = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            if (Beat != null) yield return Beat(ended);
            if (Dir != null) Dir.DrillSpeed = 160f;
        }
        public System.Func<int, IEnumerator> Beat;
        protected override void ResetPhase()
        {
            transform.position = ClampBoss(L.TunnelerStart);
            Seven.Place(L.HerFightStart, Vector3.back);
        }
        protected override Vector3 Origin(AttackDef a) => transform.position;
        protected override void StrikeLanded(string blow, bool counted) { if (_onDeck) { DeckStrikes++; StunBeat?.Invoke("strike"); } }
        protected override void AttackStarted(AttackDef a) { if (Dir != null) Dir.DrillSpeed = 1100f; }

        // ---------------------------------------------------------------- how the craft moves (hover, tilt, the drill)
        protected override void Update()
        {
            if (_distract.HasValue)
            {
                var to = Flat(_distract.Value - transform.position);
                if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 4f);
                if (Time.time > _distractUntil) { _distract = null; Paused = false; }
            }
            base.Update();
            if (!_distract.HasValue && !Paused && (State == St.Idle || State == St.Telegraph))
            {
                var to = Flat(Her - transform.position);
                if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 3f);
            }
        }
        protected override void Anim(St st, AttackDef a, float u, bool moving, float speed)
        {
            float h = 0.6f + Mathf.Sin(Time.time * 1.6f) * 0.08f, tilt = moving ? 6f : 0f, drill = 160f;
            switch (st)
            {
                case St.Telegraph: tilt = a != null && a.kind == "lunge" ? -10f : a != null && a.kind == "ring" ? -18f : 8f; h = a != null && a.kind == "ring" ? Mathf.Lerp(0.6f, 2.6f, u) : 0.8f; drill = 900f; break;
                case St.Active:
                    if (a != null && a.kind == "ring") { h = Mathf.Lerp(2.6f, 0.2f, Mathf.Clamp01(u * 3f)); tilt = 10f; }
                    else if (a != null && a.kind == "lunge") { tilt = 18f; h = 0.5f; }
                    else { tilt = 14f; _yawKick = Mathf.Sin(u * Mathf.PI) * 50f; }
                    drill = 1400f; break;
                case St.Recover: tilt = Open ? -4f : 0f; h = Open ? 0.35f : 0.6f; drill = Open ? 60f : 300f; break;
                case St.Staggered: case St.Dazed: case St.Pinned:
                    // QF30 (8): the thrusters sputter out - the rig drops onto the floor, tilted toward her (his head comes down to her)
                    h = 0.16f; tilt = 9f; drill = 0f; break;
            }
            bool down = st == St.Staggered || st == St.Dazed || st == St.Pinned;
            if (Rig != null)
            {
                if (down && !_wasDown) { Rig.Drop(true); _settled = false; Log($"boss {Def.id}: QF30 (8) stunned - the rig drops onto the floor, tilted toward her"); StunBeat?.Invoke("drop"); }
                if (down) { if (!_settled && _h < 0.22f) { _settled = true; } Rig.UpdateDeck(_settled); }
                if (!down && _wasDown)
                {
                    // back up: she is pushed off its edge first (never left inside it), then the thrusters fire and it rises
                    var d = Flat(Her - transform.position); float r = d.magnitude;
                    if (r < Rig.Radius + 0.3f) { var o = r > 0.05f ? d / r : -transform.forward; Seven.Slide(o, Rig.Radius + 0.4f - r, 0.35f); Log($"boss {Def.id}: QF30 (8) he recovers - she is slid off the rig's edge ({r:0.00} m from its centre)"); }
                    Rig.UpdateDeck(false); Rig.Drop(false); _settled = false; StunBeat?.Invoke("rise");
                }
                _wasDown = down;
                if (down && _settled && Rig.DeckZone != null && Rig.DeckZone.Enabled && Flat(Her - transform.position).magnitude < Rig.DeckR && Her.y > transform.position.y + 0.2f && !_onDeck) { _onDeck = true; DeckLandings++; Log($"boss {Def.id}: QF30 (8) she jumped up onto his rig's deck"); StunBeat?.Invoke("deck"); }
                if (!down || Her.y < transform.position.y + 0.1f) _onDeck = false;
                Rig.ArmDrillWant = st == St.Active ? 1700f : st == St.Telegraph ? 900f : down ? 0f : 220f;
                if (Rig.Arms != null) for (int i = 0; i < Rig.Arms.Length; i++) if (Rig.Arms[i] != null) { float sw = st == St.Active ? -28f : st == St.Telegraph ? 12f : down ? 20f : 0f; Rig.Arms[i].localRotation = Quaternion.Slerp(Rig.Arms[i].localRotation, Quaternion.Euler(sw, 0f, 0f), Time.deltaTime * 6f); }
            }
            if (st != St.Active) _yawKick = Mathf.MoveTowards(_yawKick, 0f, 200f * Time.deltaTime);
            _h = Mathf.MoveTowards(_h, h, (st == St.Active ? 9f : st == St.Staggered || st == St.Dazed ? 4f : 2.5f) * Time.deltaTime);
            _tilt = Mathf.MoveTowards(_tilt, tilt, 60f * Time.deltaTime);
            if (Body != null) { Body.localPosition = Vector3.up * _h; Body.localRotation = Quaternion.Euler(_tilt, _yawKick, 0f); }
            if (Dir != null && st != St.Hold) Dir.DrillSpeed = drill;
        }
    }
}
