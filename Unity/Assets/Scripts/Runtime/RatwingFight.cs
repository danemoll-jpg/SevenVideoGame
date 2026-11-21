using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using L = Seven.BiavehLayout;

namespace Seven
{
    /// <summary>
    /// Round 17 — BIA_09, Ratwing in the temple's transfer chamber, per Dan's Decisions (CHAPTER_BIAVEH.md) and the "Boss
    /// fights" rule. He FLIES: telegraphed swoops (he climbs, spreads his wings, screeches; a white line on the floor where the
    /// dive will run - dodge sideways), close claw passes (a low sweep - block or dodge) and, later, a wing gust (a ring on the
    /// floor - jump). A wasted swoop leaves him low and open: a telekinetic throw knocks him out of the air onto the floor,
    /// STUNNED (he reels, wings drooping) - she closes in: punches (X), kicks (Y), the arc blades (stun-only) drain his
    /// STAMINA bar. Phase 1 / 2 each have a partition station: X while he attacks closes a partition across the shaft (a
    /// visible shutter) and his flight space shrinks. She can lose (hits knock her back; she goes down → a quick retry; he
    /// keeps his damage). When his stamina is spent he FLEES - the director's stage three (the compartment, Panther's
    /// timing). Nobody is hurt for good: he ends contained, alive. Numbers: boss_tuning.json "ratwing".
    /// </summary>
    public class RatwingFight : BossFight
    {
        public RatwingRig Rig;
        public Transform[] PartitionDoors;            // the two partitions (phase 1 / 2): drop from above when closed
        public Renderer[] StationLamps;
        public readonly bool[] Closed = new bool[2];
        public int PartitionsClosed => (Closed[0] ? 1 : 0) + (Closed[1] ? 1 : 0);
        float _h = 5.5f;

        public void Setup(RatwingRig rig, Transform[] partitions, Renderer[] lamps, Mesh[] debris, Material mat)
        {
            Rig = rig; PartitionDoors = partitions; StationLamps = lamps;
            // things to throw: loose relay housings, conduit pieces and shell fragments on the chamber floor
            var spots = new[] { new Vector3(160f, 0f, 112f), new Vector3(170f, 0f, 108f), new Vector3(186f, 0f, 110f), new Vector3(192f, 0f, 122f), new Vector3(186f, 0f, 136f),
                                new Vector3(172f, 0f, 140f), new Vector3(160f, 0f, 134f), new Vector3(166f, 0f, 124f), new Vector3(180f, 0f, 126f) };
            for (int i = 0; i < spots.Length; i++)
            {
                var g = new GameObject("ChamberDebris" + i);
                var dm = debris != null && debris.Length > 0 ? debris[i % debris.Length] : Markers.Rock(80 + i, new Vector3(0.4f, 0.25f, 0.4f));
                g.AddComponent<MeshFilter>().sharedMesh = dm; g.AddComponent<MeshRenderer>().sharedMaterial = mat;
                g.transform.position = spots[i] + Vector3.up * (0.01f - dm.bounds.min.y); g.transform.rotation = Quaternion.Euler(0f, i * 71f, 0f);
                AddDebris(g.transform);
            }
            for (int i = 0; i < 2; i++) Glowing.Set(lamps != null && i < lamps.Length ? lamps[i] : null, 0.5f);
        }
        public void PlaceStart() { transform.position = new Vector3(L.RatwingStart.x, 0f, L.RatwingStart.z); transform.rotation = Quaternion.LookRotation(Vector3.back); _h = L.RatwingStart.y; }

        // ---------------------------------------------------------------- the arena (shrinks with each partition)
        float X0 => L.ArenaX0 + 2f; float X1 => L.ArenaX1 - 2f - (Closed[1] ? 10f : 0f); float Z0 => L.ArenaZ0 + 2f + (Closed[0] ? 10f : 0f); float Z1 => L.ArenaZ1 - 2f;
        protected override Vector3 ClampBoss(Vector3 p) => new Vector3(Mathf.Clamp(p.x, X0, X1), p.y, Mathf.Clamp(p.z, Z0, Z1));
        protected override Vector3 Centre => transform.position + Vector3.up * (_h + 0.9f);
        /// <summary>Her blows reach him only when he is down on the floor (stunned, or low and open after a wasted dive).</summary>
        protected override bool Reachable => _h < 1.9f;

        // ---------------------------------------------------------------- the partition stations (X while he attacks)
        protected override bool ObjectiveDone => Phase >= 2 || Closed[Phase];
        Vector3 Station => L.PartitionStations[Mathf.Clamp(Phase, 0, 1)];
        protected override string ObjectiveUseLabel() => Phase < 2 && !Closed[Phase] && Flat(Station - Her).magnitude < 3f ? "Close the partition" : null;
        protected override void ObjectiveUse()
        {
            if (Phase >= 2 || Closed[Phase]) return;
            int i = Phase; Closed[i] = true;
            Log($"boss {Def.id}: partition {i + 1} closed by X at its station - his flight space shrinks (the shaft is {X1 - X0:0} x {Z1 - Z0:0} m now)");
            StartCoroutine(Shut(i));
            ObjectiveProgress();
        }
        IEnumerator Shut(int i)
        {
            AudioKit.Play("bia_partition", L.Partitions[i] + Vector3.up * 6f, 1f, 0f, AudioKit.Bus.Sfx, 80f);
            if (StationLamps != null && i < StationLamps.Length) Glowing.Set(StationLamps[i], 1.8f);
            NudgeCamera(L.Partitions[i] + Vector3.up * 4f);
            var d = PartitionDoors != null && i < PartitionDoors.Length ? PartitionDoors[i] : null;
            if (d == null) yield break;
            var a = d.localPosition; var b = new Vector3(a.x, 0f, a.z);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f) { d.localPosition = Vector3.Lerp(a, b, t * t); yield return null; }
            d.localPosition = b; CameraShake.Quake(0.3f, 0.25f);
            // he is pushed back inside what is left of the shaft
            transform.position = ClampBoss(transform.position);
        }
        protected override void SkipObjectives() { for (int i = 0; i < 2; i++) if (!Closed[i]) { Closed[i] = true; StartCoroutine(Shut(i)); } }
        public override (Vector3 at, string how)? PilotObjective() => Phase < 2 && !Closed[Phase] ? (Station, "use") : ((Vector3, string)?)null;
        public override string PromptLabel => Paused ? null : ObjectiveUseLabel() ?? (Open && DebrisNear && !Reachable ? "Telekinesis: knock him down" : Open && DebrisNear ? "Telekinesis: throw" : null);
        public void ForceClosed(int i) { if (i >= 0 && i < 2 && !Closed[i]) { Closed[i] = true; if (PartitionDoors != null && i < PartitionDoors.Length && PartitionDoors[i] != null) { var p = PartitionDoors[i].localPosition; PartitionDoors[i].localPosition = new Vector3(p.x, 0f, p.z); } } }

        protected override IEnumerator PhaseBeat(int ended)
        {
            // a short beat: he climbs to the top of the shaft, screeches, and comes on faster
            float h0 = _h;
            AudioKit.Play("ratwing_screech", Centre, 1f, 0f, AudioKit.Bus.Sfx, 90f);
            if (Rig != null) Rig.Now = RatwingRig.Mode.Fly;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.6f) { _h = Mathf.Lerp(h0, 9f, Mathf.SmoothStep(0f, 1f, t)); Rig.Height = _h; yield return null; }
            if (Beat != null) yield return Beat(ended);
        }
        public System.Func<int, IEnumerator> Beat;
        protected override void ResetPhase()
        {
            // she went down: he climbs away to the far side of what is left of the shaft and gives her a moment
            transform.position = ClampBoss(new Vector3(L.RatwingStart.x, 0f, L.RatwingStart.z)); _h = 6f;
            Seven.Place(L.HerFightStart, Vector3.forward);
        }
        protected override Vector3 Origin(AttackDef a) => transform.position;

        // ---------------------------------------------------------------- how he moves (height, wings) per state
        protected override void Anim(St st, AttackDef a, float u, bool moving, float speed)
        {
            if (Rig == null) return;
            float want = 5.5f + Mathf.Sin(Time.time * 1.3f) * 0.5f, pitch = 0f; var mode = RatwingRig.Mode.Fly;
            switch (st)
            {
                case St.Telegraph:
                    if (a != null && a.kind == "lunge") { want = 7.5f; pitch = -12f; }                    // he climbs for the dive
                    else if (a != null && a.kind == "sweep") { want = Mathf.Lerp(5f, 1.6f, u); pitch = 10f; } // he drops for a low pass
                    else { want = 4f; pitch = -6f; }
                    break;
                case St.Active:
                    if (a != null && a.kind == "lunge") { mode = RatwingRig.Mode.Swoop; want = Mathf.Lerp(7.5f, 1.0f, Mathf.Clamp01(u * 2.2f)); pitch = 28f; }
                    else if (a != null && a.kind == "sweep") { want = 1.3f; pitch = 6f; }
                    else { want = 3.6f; pitch = -10f; }
                    break;
                case St.Recover:
                    want = Open ? 1.2f : Mathf.Lerp(1.4f, 5.5f, u); pitch = Open ? 0f : -8f;                 // wasted: low and open for a moment
                    break;
                case St.Staggered: case St.Dazed: case St.Pinned:
                    mode = RatwingRig.Mode.Stunned; want = 0f; pitch = 0f; break;
            }
            float rate = st == St.Active ? 14f : st == St.Staggered ? 9f : 3f;
            _h = Mathf.MoveTowards(_h, want, rate * Time.deltaTime);
            Rig.Height = _h; Rig.Now = mode; Rig.Pitch = pitch;
        }
        protected override void AttackStarted(AttackDef a) { AudioKit.Play("ratwing_flap", Centre, 0.9f, 0.05f, AudioKit.Bus.Sfx, 60f); }
        public float HeightNow => _h;
    }
}
