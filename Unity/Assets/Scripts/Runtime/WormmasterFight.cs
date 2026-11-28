using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using V = Seven.VenusLayout;

namespace Seven
{
    /// <summary>
    /// QF17 (2): the Wormmaster's fight in the control chamber (Venus part 2), to the "Boss fights" rule. Numbers: boss
    /// "wormmaster" in Resources/Bosses/boss_tuning.json.
    ///  - Phase 1 "above his machinery": from his dais he whips one long boneless arm across the chamber (a lash along a
    ///    marked line - dodge sideways), sends a creature lunging at her along a marked line (the worm surfaces and runs it -
    ///    jump it or step aside) and slams the dais so a wave of surfacing worms rolls along a line at her (jump). QF43 (16):
    ///    she shuts ALL THREE devices down under attack, in any order - the north device (X), the ledge device (telekinesis
    ///    from below, RT) and the signal crystal (RT) - while he moves along his perch toward the device she heads for and
    ///    defends it. He stays up there until the third is off (QF17 had him come down after two: phase 1 held only the north
    ///    + ledge devices and the crystal belonged to phase 2).
    ///  - Phase 2 "he comes down": his perch loses power; he drops to the floor and fights - a wide arm sweep when she is
    ///    close (jump or block), the lash, the slam wave, the creatures. A wasted attack leaves him open: a telekinetic throw
    ///    of root debris (or three strikes) staggers him.
    ///  - Phase 3 "the signal fails": faster, combos (sweep -> slam wave, lash -> shock ring), three staggers - dazed; her
    ///    takedown subdues him. He drags himself back onto his dais; the console closes the containment barrier.
    /// The creatures are victims: they surface and withdraw, never harmed; he is subdued, not killed.
    /// </summary>
    public class WormmasterFight : BossFight
    {
        public WormmasterRig Rig;
        public ControlDevice[] Devices;
        public WormCreature[] Worms;
        public System.Action<int> DisableDevice;          // the director's device-off (saves, withdraws that trough's creatures)
        public System.Func<IEnumerator> CrystalOut;       // the crystal flies out of its device (no gesture: her fight throw is the gesture)
        public System.Func<IEnumerator> Beat;
        static readonly Vector3 HerStart = new Vector3(108.5f, V.ControlY, 89f), HerStart2 = new Vector3(110f, V.ControlY, 88f), HimStart2 = new Vector3(124f, V.ControlY, 88f);
        bool _onDais = true;
        readonly List<Transform> _humps = new List<Transform>();
        WormCreature _sent;

        /// <summary>QF43 (16): the order the check-run pilot shuts the devices down in - a different one each run (logged).</summary>
        public int[] PilotOrder = { 0, 1, 2 };
        public static int OrderSeed = -1;
        Vector3 _perch;

        public void Setup(WormmasterRig rig, ControlDevice[] devices, WormCreature[] worms, Material rootMat)
        {
            Rig = rig; Devices = devices; Worms = worms;
            var rnd = new System.Random(OrderSeed >= 0 ? OrderSeed : System.Environment.TickCount);
            for (int i = 2; i > 0; i--) { int j = rnd.Next(i + 1); (PilotOrder[i], PilotOrder[j]) = (PilotOrder[j], PilotOrder[i]); }
            var spots = new[] { new Vector3(109.5f, 0f, 91.5f), new Vector3(118f, 0f, 91f), new Vector3(122f, 0f, 85f), new Vector3(114f, 0f, 103f), new Vector3(124f, 0f, 104f), new Vector3(119f, 0f, 73.5f), new Vector3(133.5f, 0f, 72f), new Vector3(108f, 0f, 74f) };
            for (int i = 0; i < spots.Length; i++)
            {
                // root debris: long gnarled pieces (and a broken device casing or two) - never a box
                var g = new GameObject("RootDebris" + i);
                g.AddComponent<MeshFilter>().sharedMesh = Markers.Rock(120 + i, i % 3 == 0 ? new Vector3(0.4f, 0.35f, 0.4f) : new Vector3(0.75f, 0.22f, 0.25f));
                g.AddComponent<MeshRenderer>().sharedMaterial = rootMat;
                var p = spots[i]; p.y = V.ControlY + 0.25f;
                g.transform.position = p; g.transform.rotation = Quaternion.Euler(0f, i * 53f, i * 9f);
                AddDebris(g.transform);
            }
            for (int i = 0; i < 7; i++)
            {
                var h = new GameObject("WaveHump" + i);
                h.AddComponent<MeshFilter>().sharedMesh = Markers.Rock(200 + i, new Vector3(0.55f, 0.45f, 0.8f));
                h.AddComponent<MeshRenderer>().sharedMaterial = rootMat;
                h.SetActive(false); _humps.Add(h.transform);
            }
        }
        public void PlaceStart() { _onDais = true; _perch = V.WormmasterAt; transform.position = V.WormmasterAt; transform.rotation = Quaternion.LookRotation(Vector3.left); if (Rig != null) Rig.Current = WormmasterRig.Pose.Dominant; }

        bool Off(int i) => Devices == null || i >= Devices.Length || Devices[i] == null || !Devices[i].On;

        // ---------------------------------------------------------------- phases + objectives
        protected override bool CanMove => !_onDais;
        protected override bool Reachable => !_onDais;
        protected override bool ObjectiveDone => Phase == 0 ? Off(0) && Off(1) && Off(2) : true;
        protected override Vector3 Centre => transform.position + Vector3.up * (_onDais ? 2.6f : 2.2f);
        public override (Vector3 at, string how)? PilotObjective()
        {
            if (Phase != 0) return null;
            foreach (int i in PilotOrder)
            {
                if (Off(i)) continue;
                if (i == 0) return (V.DeviceUse[0], "use");
                if (i == 1) return (V.DeviceAt[1] + new Vector3(2.5f, 0f, 3.5f), "tk");
                return (V.DeviceUse[2], "tk");
            }
            return null;
        }
        // QF19: one press of X in reach (3 m), instant - no hold, nothing interrupts it
        protected override string ObjectiveUseLabel() => Phase == 0 && !Off(0) && NearUse(V.DeviceUse[0]) ? "Shut down device" : null;
        /// <summary>QF39 (2) / QF43 (16): phase 1 = all three devices, any order: "Devices n / 3"; then none.</summary>
        public static readonly int[] ObjectivesPerPhase = { 3, 0, 0 };
        public override int ObjectivesDone(int phase) => phase == 0 ? (Off(0) ? 1 : 0) + (Off(1) ? 1 : 0) + (Off(2) ? 1 : 0) : 0;
        protected override void ObjectiveUse()
        {
            if (ObjectiveUseLabel() == null) return;
            float dist = Flat(V.DeviceUse[0] - Her).magnitude;
            DisableDevice?.Invoke(0);
            NudgeCamera(V.DeviceAt[0] + Vector3.up * 1.2f);
            Log($"boss wormmaster: DEVICE (QF19) 1 shut down by her X - X presses on the pad at a device so far {PadUsePresses} (went to a strike instead {PadUseToStrike}); she is {dist:0.0} m from it; his attack now: {(Cur != null && State != St.Idle ? Cur.id + " " + State : "none")}");
            if (DemoBoot.CaptureDir != null && Cap != null) { StartCoroutine(Shoot("boss_wormmaster_device1_shutdown", 0.15f)); StartCoroutine(Shoot("boss_wormmaster_device1_after", 1.4f)); }
            ObjectiveProgress();
        }
        protected override Vector3? ObjectiveTk()
        {
            int k = TkDevice();
            return k == 1 ? V.DeviceAt[1] + Vector3.up * 1.2f : k == 2 ? V.DeviceAt[2] + Vector3.up * 1f : (Vector3?)null;
        }
        /// <summary>QF43 (16): the telekinesis device in reach (the ledge device or the crystal; the nearer if both), -1 = none.</summary>
        int TkDevice()
        {
            if (Phase != 0) return -1;
            float d1 = !Off(1) ? Flat(V.DeviceAt[1] - Her).magnitude : 1e9f, d2 = !Off(2) ? Flat(V.DeviceAt[2] - Her).magnitude : 1e9f;
            bool r1 = d1 < 9.5f, r2 = d2 < 10f;
            if (r1 && (!r2 || d1 <= d2)) return 1;
            return r2 ? 2 : -1;
        }
        protected override void ObjectiveTkFire()
        {
            int k = TkDevice();
            if (k == 1) { AudioKit.Play("tk_push", V.DeviceAt[1], 1f); DisableDevice?.Invoke(1); ObjectiveProgress(); return; }
            if (k == 2) StartCoroutine(Crystal());
        }
        IEnumerator Crystal() { if (CrystalOut != null) yield return CrystalOut(); else DisableDevice?.Invoke(2); ObjectiveProgress(); }
        public override string PromptLabel => Paused ? null : ObjectiveUseLabel() ?? (ObjectiveTk().HasValue ? (TkDevice() == 1 ? "Telekinesis: the ledge device" : "Telekinesis: the signal crystal") : Open && DebrisNear ? "Telekinesis: throw" : State == St.Dazed ? "Takedown" : null);
        protected override void SkipObjectives()
        {
            if (Phase == 0) for (int i = 0; i < 3; i++) if (!Off(i)) DisableDevice?.Invoke(i);
        }

        protected override IEnumerator PhaseBeat(int ended)
        {
            if (ended == 0)
            {
                // QF43 (16): the third device is off - his perch loses power (the machinery dies under him, he sags with it);
                // only now does he come down to deal with her himself
                Debug.Log($"[Seven] boss wormmaster: PHASE CHANGE (QF43 16) - all three devices off ({ObjectivesDone(0)} / 3); {DefendReport}; he leaves his perch only now");
                if (DemoBoot.CaptureDir != null && Cap != null) Cap.Log($"boss wormmaster: PHASE CHANGE (QF43 16) - all three devices off ({ObjectivesDone(0)} / 3), pilot order {PilotOrder[0] + 1}-{PilotOrder[1] + 1}-{PilotOrder[2] + 1}; {DefendReport}; he leaves his perch only now");
                if (Beat != null) StartCoroutine(Beat());
                AudioKit.Play("tremor_rumble", Centre, 0.8f, 0f, AudioKit.Bus.Sfx, 90f);
                CameraShake.Quake(0.9f, 0.35f);
                for (float t = 0f; t < 0.9f; t += Time.deltaTime) { transform.position = _perch + Vector3.down * 0.35f * Mathf.SmoothStep(0f, 1f, t / 0.9f) + new Vector3(Mathf.Sin(t * 37f), 0f, Mathf.Cos(t * 31f)) * 0.06f; yield return null; }
                if (Rig != null) Rig.PulseGesture();
                AudioKit.Play("signal_pulse", Centre, 1f, 0f, AudioKit.Bus.Sfx, 90f);
                var a = transform.position; var b = new Vector3(V.DaisAt.x - V.DaisRadius - 2.2f, V.ControlY, V.DaisAt.z);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 1.8f) { float u = Mathf.SmoothStep(0f, 1f, t); transform.position = Vector3.Lerp(a, b, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.9f; yield return null; }
                _onDais = false;
                CameraShake.Quake(0.6f, 0.3f); AudioKit.Play("land_hard", b, 1f);
                for (float t = 0f; t < 1.4f; t += Time.deltaTime) yield return null;
            }
            else
            {
                if (Rig != null) Rig.PulseGesture();
                AudioKit.Play("creature_rumble", Centre, 1f, 0f, AudioKit.Bus.Sfx, 80f);
                CameraShake.Quake(0.5f, 0.25f);
                for (float t = 0f; t < 1.6f; t += Time.deltaTime) yield return null;
            }
        }
        protected override void ResetPhase()
        {
            if (Phase == 0) { transform.position = _perch; Seven.Place(HerStart, Vector3.right); }
            else { transform.position = HimStart2; Seven.Place(HerStart2, Vector3.right); }
            transform.rotation = Quaternion.LookRotation(Vector3.left);
            foreach (var h in _humps) h.gameObject.SetActive(false);
        }
        protected override Vector3 ClampBoss(Vector3 p)
        {
            if (_onDais) return _perch;
            p.x = Mathf.Clamp(p.x, 106f, 129.5f); p.z = Mathf.Clamp(p.z, 67f, 109f); p.y = V.ControlY;
            return p;
        }
        protected override Vector3 Origin(AttackDef a)
        {
            if (a.id == "worm_lunge")
            {
                // the creature he sends: the nearest one still his (it comes up out of its trough toward her)
                _sent = null; float bd = 1e9f;
                if (Worms != null) foreach (var w in Worms) { if (w == null || !w.gameObject.activeSelf || w.Withdrawn || w.Stunned || w.Calm) continue; float d = Flat(w.transform.position - Her).magnitude; if (d < bd) { bd = d; _sent = w; } }
                if (_sent != null)
                {
                    var p = _sent.transform.position;
                    if (bd > 11f || bd < 5f) { var dir = Flat(Her - Centre).normalized; p = Her - dir * 9f; p = Ground.ClampToArena(p, 0.5f); }
                    return p;
                }
                return Her - Flat(Her - transform.position).normalized * 9f;
            }
            if (_onDais && (a.kind == "wave" || a.kind == "ring")) return transform.position + Flat(Her - transform.position).normalized * (V.DaisRadius + 0.5f);
            return transform.position;
        }

        /// <summary>QF43 (16): on his perch he defends the devices - he moves across the dais top toward the device still running
        /// that she is nearest (the one she is heading for), and turns to her; his attacks come from there.</summary>
        protected override void Arena(float dt)
        {
            if (!_onDais || Devices == null) return;
            int best = -1; float bd = 1e9f;
            for (int i = 0; i < 3; i++) { if (Off(i)) continue; float d = Flat(V.DeviceAt[i] - Her).magnitude; if (d < bd) { bd = d; best = i; } }
            var target = V.WormmasterAt;
            if (best >= 0) target += Flat(V.DeviceAt[best] - V.DaisAt).normalized * (V.DaisRadius - 1.3f);
            if (best != _defending && best >= 0)
            {
                _defending = best; _defendSwitches++;
                if (DemoBoot.CaptureDir != null && Cap != null) Cap.Log($"boss wormmaster: DEFENDS (QF43 16) device {best + 1} - she is {bd:0.0} m from it; he moves along his perch toward it");
            }
            _perch = Vector3.MoveTowards(_perch, target, 1.8f * dt);
            transform.position = _perch;
            var look = Flat(Her - _perch); if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), 120f * dt);
        }
        int _defending = -1, _defendSwitches;
        public string DefendReport => $"he moved along his perch to defend a device {_defendSwitches}x";

        // ---------------------------------------------------------------- the attacks' visuals (worms surfacing along the line)
        protected override void AttackStarted(AttackDef a)
        {
            if (a.id == "worm_lunge" && _sent != null) { _sent.Highlight(a.active / Tempo + 0.5f); _sent.Lunge(Aim, a.active / Tempo + 0.3f); }
            if (a.kind == "wave" || a.kind == "ring") { AudioKit.Play("worm_surface", From, 1f, 0.05f, AudioKit.Bus.Sfx, 50f); CameraShake.Quake(0.25f, 0.12f); }
        }
        protected override void AttackFx(AttackDef a, float u)
        {
            if (a.kind == "wave")
            {
                var front = From + AimDir * a.range * u;
                if (a.id == "worm_lunge" && _sent != null) { var p = front; p.y = _sent.transform.position.y; _sent.transform.position = p; _sent.transform.rotation = Quaternion.LookRotation(AimDir); }
                var side = Vector3.Cross(Vector3.up, AimDir);
                for (int i = 0; i < _humps.Count; i++)
                {
                    float lag = i * 0.9f, lat = (i % 3 - 1) * a.width * 0.32f;
                    var p = front - AimDir * lag + side * lat;
                    float k = Mathf.Clamp01(1f - lag / 4f) * Mathf.Sin(Mathf.Clamp01(u * 1.1f) * Mathf.PI);
                    p.y = V.ControlY - 0.45f + 0.75f * k;
                    _humps[i].gameObject.SetActive(u < 1f && k > 0.05f && Vector3.Dot(p - From, AimDir) > 0f);
                    _humps[i].position = p; _humps[i].rotation = Quaternion.LookRotation(AimDir) * Quaternion.Euler(-20f + 40f * k, 0f, i * 25f);
                }
            }
            else if (a.kind == "ring")
            {
                for (int i = 0; i < _humps.Count; i++)
                {
                    float ang = i / (float)_humps.Count * Mathf.PI * 2f; float r = a.range * u;
                    var p = From + new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * r; p.y = V.ControlY - 0.4f + 0.6f * Mathf.Sin(u * Mathf.PI);
                    _humps[i].gameObject.SetActive(u < 1f); _humps[i].position = p;
                }
            }
            if (u >= 1f) foreach (var h in _humps) h.gameObject.SetActive(false);
        }
        // the creatures in their troughs are victims: they only reach her when he SENDS one (worm_lunge, the slam's wave) -
        // touching a surfaced one does nothing (QF17 tuning: passive contact made phase 1 a lottery)
        public override IEnumerable<Vector3> Hazards()
        {
            if (Worms == null) yield break;
            foreach (var w in Worms) if (w != null && w.gameObject.activeSelf && (w.Surfaced || w.Lunging) && !w.Stunned) yield return w.transform.position;
        }
        // ---------------------------------------------------------------- animation (procedural rig)
        protected override void Anim(St st, AttackDef a, float u, bool moving, float speed)
        {
            if (Rig == null) return;
            Rig.Moving = moving ? Mathf.Clamp01(speed / 2f) : 0f;
            Rig.LashW = Rig.LashS = Rig.SlamW = Rig.SlamS = 0f;
            Rig.Dazed = st == St.Staggered || st == St.Dazed || st == St.Pinned ? 1f : 0f;
            if (a == null) return;
            bool slam = a.kind == "wave" && a.id != "worm_lunge" || a.kind == "ring";
            if (st == St.Telegraph)
            {
                if (slam) Rig.SlamW = u;
                else if (a.id == "worm_lunge") { if (u < 0.1f) Rig.PulseGesture(); }
                else { Rig.LashW = u; Rig.LashSide = a.kind == "sweep" && a.angle > 90f ? 1f : -1f; }
            }
            else if (st == St.Active)
            {
                if (slam) Rig.SlamS = 1f - u * 0.6f;
                else if (a.id != "worm_lunge") Rig.LashS = Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI * 0.5f + 0.2f);
            }
            else if (st == St.Recover) { if (slam) Rig.SlamS = Mathf.Clamp01(0.4f - u); else if (a.id != "worm_lunge") Rig.LashS = Mathf.Clamp01(0.6f - u); }
        }
    }
}
