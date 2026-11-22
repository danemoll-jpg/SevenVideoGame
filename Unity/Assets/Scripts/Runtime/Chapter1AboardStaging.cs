using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF16 (3) - Dan: "not every talk in the pilot's chair with the cat in her lap". Where each conversation aboard happens,
    /// in ONE table (<see cref="Table"/>) so any of them can be swapped. Each scene starts already in its pose after a short
    /// fade (no long get-up / lie-down transitions). Poses: 7 lying on the sofa / sitting on the floor = the
    /// poses <see cref="SevenRestPose"/> (her own bones, posed after the animator); at the window = her idle; the chair = as before.
    /// Panther: on her stomach, on the console beside the display, on the window ledge, curled on the cushion beside her
    /// head, or (small) perched on her shoulder - placed directly (his roaming companion is paused while posed).
    /// Non-sexual presentation applies fully to the lying poses: relaxed, fully clothed, the camera frames faces and the cat.
    /// Every staging ends with <see cref="ExitStaging"/>: hand IK released (QF13 15), Panther back to roaming, 7 on her feet.
    /// </summary>
    public partial class Chapter1Aboard
    {
        public enum Stage { Chair, ChairCatConsole, Sofa, FloorSofa, Window, WindowShoulder }
        /// <summary>The staging table (the chat's proposal, QF16 3): scene id → staging.</summary>
        public static readonly Dictionary<string, Stage> Table = new Dictionary<string, Stage>
        {
            { "VEN_C11", Stage.Sofa },             // the debrief after Venus: both lying on the sofa
            { "VEN_C13", Stage.ChairCatConsole },  // choosing the next world: the chair, Panther sitting on the console
            { "EUR_01", Stage.Chair },             // QF23 (3): every ARRIVAL talk = the pilot's chair at the front window, the hull faded away (HullAway), Panther in her lap
            { "EUR_17", Stage.FloorSofa },         // Europa debrief: on the floor, back against the sofa
            { "MAR_01", Stage.Chair },             // QF23 (3): Mars arrival - the chair, the vanish view over the plain
            { "MAR_12", Stage.Sofa },              // Mars, what she remembers: both lying on the sofa
            { "MAR_13", Stage.ChairCatConsole },   // the route display: the chair, Panther on the console
            { "AST_01", Stage.Chair },             // the asteroid: at the controls, Panther in her lap (he is lifted onto the adjoining seat)
            { "BIA_01", Stage.Chair },             // QF23 (3): the mothership arrival (BIA_01 + BIA_02 while aboard) - the chair, the vanish view among Veyra's rings
            { "AST_10", Stage.Chair },             // after the asteroid: the navigation chair, Panther in her lap
        };
        public static Stage StageOf(string scene) => Table.TryGetValue(scene, out var s) ? s : Stage.Chair;
        public Stage CurrentStage { get; private set; } = Stage.Chair;
        public string StagedScene { get; private set; }
        bool _staged;

        // ---- the places (deck polar positions; the sofa 140..182 deg, the window band 52..128 deg)
        const float SofaA0 = 146f, SofaA1 = 180f, SofaSeatR = 6.92f, SofaSeatY = 0.50f, WindowA = 90f;
        static Vector3 Polar(float r, float aDeg, float y) => new Vector3(Mathf.Cos(aDeg * Mathf.Deg2Rad) * r, y, Mathf.Sin(aDeg * Mathf.Deg2Rad) * r);
        static Vector3 Tangent(float aDeg) => new Vector3(-Mathf.Sin(aDeg * Mathf.Deg2Rad), 0f, Mathf.Cos(aDeg * Mathf.Deg2Rad));   // + = increasing angle
        static Vector3 Inward(float aDeg) => -Polar(1f, aDeg, 0f);
        Vector3 _stageHead, _stageCat, _stageLook;
        Transform _shoulder;

        /// <summary>Put 7 and Panther into the scene's staging (the screen is black / fading; the caller fades in).</summary>
        IEnumerator EnterStaging(string scene)
        {
            var st = StageOf(scene); CurrentStage = st; StagedScene = scene; _staged = true;
            Seven.Frozen = true; Seven.Cutscene = true;
            if (st == Stage.Chair || st == Stage.ChairCatConsole)
            {
                yield return SitInPilotChair();
                if (st == Stage.Chair)
                {
                    Cat.LapFrame = ShipConsole.I.PilotSeat; Cat.enabled = true; yield return null; yield return null;
                    Cat.PlaceOnLap();
                }
                else
                {
                    // Panther sits on the console's desk beside the navigation display, facing her
                    Cat.enabled = false; Cat.transform.localScale = Vector3.one * Cat.LapScale;
                    var disp = ShipConsole.I.Display != null ? ShipConsole.I.Display.position : PilotHips + PilotFwd * 1.1f + Up * 0.5f;
                    var desk = PilotHips + PilotFwd * 0.62f - PilotRight * 0.42f; desk.y = disp.y - 0.34f;
                    _stageCat = desk;
                    Cat.transform.SetPositionAndRotation(desk, Quaternion.LookRotation(Flat(PilotHips - desk)));
                    CatState("sit");
                }
                _stageHead = PilotHead; _stageLook = PilotHead;
            }
            else if (st == Stage.Sofa)
            {
                // 7 on her back along the sofa, her head on the armrest end (180 deg); Panther stretched out on her stomach
                Seven.OnSofa = true; Seven.SeatHips = null;
                var headEnd = Polar(SofaSeatR, SofaA1 - 3.5f, SofaSeatY + 0.11f);
                var along = Tangent(SofaA1 - 10f);                         // feet -> head
                var feet = headEnd - along * 1.62f;
                Seven.transform.SetPositionAndRotation(feet + Up * 0.02f, Quaternion.LookRotation(Up, along));   // face up, head toward the armrest
                Seven.PlayClip("idle", 0f); SevenRestPose.On(Seven.gameObject, SevenRestPose.Kind.Lie);
                yield return null; yield return null;
                var hips = Seven.Hips != null ? Seven.Hips.position : feet + along * 0.95f;
                _stageHead = SevenHead;
                Cat.enabled = false; Cat.transform.localScale = Vector3.one * Cat.LapScale;
                _stageCat = hips + along * 0.22f + Up * 0.1f;
                Cat.transform.SetPositionAndRotation(_stageCat, Quaternion.LookRotation(along, Up));
                CatState("sleep"); Cat.ShowCurled(true);                    // curled up on her stomach (the static curled body)
                _stageLook = (_stageHead + _stageCat) * 0.5f;
            }
            else if (st == Stage.FloorSofa)
            {
                // 7 on the deck, back against the sofa base, facing the deck centre; Panther curled on the cushion by her head
                Seven.OnSofa = true; Seven.SeatHips = null;
                float a = 166f; var at = Polar(6.12f, a, 0f);
                Seven.transform.SetPositionAndRotation(at, Quaternion.LookRotation(Inward(a)));
                Seven.PlayClip("idle", 0f); SevenRestPose.On(Seven.gameObject, SevenRestPose.Kind.SitFloor);
                yield return null; yield return null;
                _stageHead = SevenHead;
                Cat.enabled = false; Cat.transform.localScale = Vector3.one * Cat.LapScale;
                _stageCat = Polar(6.95f, a + 5.5f, SofaSeatY + 0.02f);
                Cat.transform.SetPositionAndRotation(_stageCat, Quaternion.LookRotation(Tangent(a), Up));
                CatState("sleep"); Cat.ShowCurled(true);
                _stageLook = (_stageHead + _stageCat) * 0.5f;
            }
            else
            {
                // standing at the window, looking down at the world outside
                var at = Polar(7.05f, WindowA - 4f, 0f);
                Seven.OnSofa = false; Seven.SeatHips = null;
                Seven.transform.SetPositionAndRotation(at, Quaternion.LookRotation(Flat(Polar(9f, WindowA - 4f, 0f) - at) + Up * 0f));
                Seven.PlayClip("idle", 0f);
                yield return null; yield return null;
                _stageHead = SevenHead;
                Cat.enabled = false;
                if (st == Stage.Window)
                {
                    Cat.transform.localScale = Vector3.one * Cat.LapScale;
                    _stageCat = Polar(7.45f, WindowA + 5f, 0.02f);
                    Cat.transform.SetPositionAndRotation(_stageCat, Quaternion.LookRotation(Flat(Polar(9f, WindowA + 5f, 0f) - _stageCat)));
                    CatState("sit");
                }
                else
                {
                    // small, perched on her left shoulder (follows the bone every frame)
                    Cat.transform.localScale = Vector3.one * 0.6f;                      // QF18 (4): small, but big enough to show over her pigtail
                    _shoulder = Seven.Bone(HumanBodyBones.LeftShoulder);
                    // QF18 (4): lying along the top of her shoulder (front paws forward, hind paws tucked), not sitting off it
                    CatState("sit");                                     // sitting ON TOP of the shoulder, balanced (the lie-down clip did not render him there)
                    StartCoroutine(HoldPerchPose());
                    PerchCat();
                    _stageCat = Cat.transform.position;
                }
                _stageLook = (_stageHead + _stageCat) * 0.5f;
            }
            if (Auto) Cap?.Log($"staging (QF16 3) '{scene}': {st} - 7's head {_stageHead:0.00}, Panther {_stageCat:0.00}");
        }

        string _stageCatAnim; bool _catScripted;
        void CatState(string s) { _stageCatAnim = s; if (Cat.Anim != null && Cat.Anim.HasState(0, Animator.StringToHash(s))) Cat.Anim.CrossFadeInFixedTime(s, 0.1f, 0); }
        /// <summary>QF18 (4) - Dan: "he hangs off the side of her head with his hind legs dangling". Before, his root was put at
        /// her clavicle joint (inside her, by the neck) facing forward in the sit pose, so his body rose beside her head and his
        /// hind legs hung below the joint. Now: on TOP of her shoulder - the point over the deltoid between the clavicle and the
        /// upper-arm joint, raised to the skin - his body along the shoulder line (rear toward her arm, head toward her neck),
        /// lying (paws on the shoulder), his head turned to look out of the window beside hers, his tail down her back.</summary>
        public const float PerchTop = 0.085f;
        void PerchCat()
        {
            if (_shoulder == null) return;
            var fwd = Flat(Seven.transform.forward).normalized;
            var arm = Seven.Bone(HumanBodyBones.LeftUpperArm); var neck = Seven.Bone(HumanBodyBones.Neck);
            var a = _shoulder.position; var b = arm != null ? arm.position : a - Seven.transform.right * 0.15f;
            var top = Vector3.Lerp(a, b, 1.0f) + (b - a).normalized * 0.03f - fwd * 0.03f; top.y = Mathf.Max(a.y, b.y) + PerchTop;   // the outer shoulder, clear of her pigtail (by the neck he vanished inside it)
            if (Auto && Time.frameCount % 120 == 0) { var rs = Cat.GetComponentsInChildren<Renderer>(); int on = 0; foreach (var r in rs) if (r.enabled && r.gameObject.activeInHierarchy) on++; Debug.Log($"[Seven] perch: cat renderers on {on}/{rs.Length}, cat active {Cat.gameObject.activeInHierarchy}, scale {Cat.transform.lossyScale.x:0.00}"); }
            var inward = Flat((neck != null ? neck.position : a) - b); if (inward.sqrMagnitude < 1e-4f) inward = Seven.transform.right; inward.Normalize();
            var dir = (inward * 0.35f + fwd * 0.65f).normalized;             // along the shoulder, his head turned out toward the window beside hers
            Cat.transform.SetPositionAndRotation(top, Quaternion.LookRotation(dir));
            PerchTopY = top.y; PerchShoulderTop = top;
        }
        public float PerchTopY { get; private set; }
        public Vector3 PerchShoulderTop { get; private set; }
        System.Collections.IEnumerator HoldPerchPose()
        {
            for (float t = 0f; t < 1.1f; t += Time.deltaTime) yield return null;
            if (Cat != null && Cat.Anim != null && CurrentStage == Stage.WindowShoulder && _stageCatAnim == "lie_down") Cat.Anim.speed = 0f;   // hold the lying pose
            if (Auto && Cap != null && CurrentStage == Stage.WindowShoulder)
            {
                var fw = Flat(Seven.transform.forward).normalized; var rt = Vector3.Cross(Vector3.up, fw);
                yield return Cap.ShotFrom("qf18_04_perch_front", PerchShoulderTop + fw * 1.1f - rt * 0.55f + Up * 0.2f, PerchShoulderTop + Up * 0.02f, 40f);
                yield return Cap.ShotFrom("qf18_04_perch_side", PerchShoulderTop - rt * 1.2f + Up * 0.25f - fw * 0.1f, PerchShoulderTop, 40f);
            }
            if (Auto) { var rs = Cat.GetComponentsInChildren<SkinnedMeshRenderer>(); string mb = rs.Length > 0 ? rs[0].bounds.center.ToString("0.000") + " size " + rs[0].bounds.size.ToString("0.000") : "-"; Cap?.Log($"perch (QF18 4): cat root {Cat.transform.position:0.000}, mesh bounds {mb}"); }
            if (Auto && _catPose != null) Cap?.Log($"perch (QF18 4): Panther sitting on her left shoulder top at {PerchShoulderTop:0.000}, his paws at {_catPose.PawL:0.000} / {_catPose.PawR:0.000} (shoulder top y {PerchTopY:0.000}: paw gaps {(_catPose.PawL.y - PerchTopY) * 100f:0} / {(_catPose.PawR.y - PerchTopY) * 100f:0} cm)");
        }
        void LateUpdateStaging()
        {
            if (_staged && CurrentStage == Stage.WindowShoulder && !Cat.enabled) PerchCat();
            if (_staged && (CurrentStage == Stage.Window || CurrentStage == Stage.WindowShoulder) && Seven.State != "idle") Seven.PlayClip("idle", 0.2f);   // standing still at the glass
            // the staged Panther stays in his pose (nothing else re-animates him) unless a scripted cat moment is moving him
            if (_staged && !_catScripted && _stageCatAnim != null && Cat.Anim != null && CurrentStage != Stage.Chair)
            {
                if (Cat.enabled) Cat.enabled = false;
                if (CurrentStage == Stage.Sofa || CurrentStage == Stage.FloorSofa)
                {
                    if (!Cat.LapMeshShown) Cat.ShowCurled(true);                   // asleep: the static curled body
                    if ((Cat.transform.position - _stageCat).sqrMagnitude > 1e-4f) Cat.transform.position = _stageCat;
                }
                var info = Cat.Anim.GetCurrentAnimatorStateInfo(0);
                if (!info.IsName(_stageCatAnim) && !Cat.Anim.IsInTransition(0) && Cat.Anim.HasState(0, Animator.StringToHash(_stageCatAnim))) Cat.Anim.CrossFadeInFixedTime(_stageCatAnim, 0.25f, 0);
            }
        }

        /// <summary>The scene's framings by keyword (the old chair keywords: two / lap / side / ots / wide / objects) for the
        /// staging in use - faces and the cat, never body-emphasis angles.</summary>
        bool CutStage(string kind)
        {
            if (!_staged || CurrentStage == Stage.Chair || CurrentStage == Stage.ChairCatConsole)
            {
                if (_staged && CurrentStage == Stage.ChairCatConsole && kind == "lap") { Cut(_stageCat + PilotRight * -0.8f + PilotFwd * -0.4f + Up * 0.3f, (_stageCat + PilotHead) * 0.5f, 38f); return true; }
                return false;
            }
            var head = _stageHead; var cat = _stageCat; var mid = (head + cat) * 0.5f;
            Vector3 side;
            switch (CurrentStage)
            {
                case Stage.Sofa:
                {
                    var along = Tangent(SofaA1 - 10f); var inn = Inward(SofaA1 - 10f);
                    switch (kind)
                    {
                        case "lap": Cut(cat + inn * 0.9f + Up * 0.55f + along * 0.15f, cat + Up * 0.08f, 34f); break;                      // Panther on her, her hand
                        case "ots": case "side": Cut(head + inn * 1.15f + Up * 0.5f - along * 0.25f, (head + cat) * 0.5f, 40f); break;       // her face and Panther
                        case "wide": Cut(mid + inn * 3.4f + Up * 1.5f, mid, 46f); break;
                        default: Cut(mid + inn * 1.9f + Up * 1.0f, mid, 42f); break;                                                            // the two of them, from the deck side
                    }
                    return true;
                }
                case Stage.FloorSofa:
                {
                    var inn = (Flat(head) * -1f).normalized; side = Vector3.Cross(Up, inn);
                    switch (kind)
                    {
                        case "lap": Cut(cat + inn * 0.8f + Up * 0.25f + side * 0.3f, cat + Up * 0.1f, 34f); break;
                        case "ots": case "side": Cut(head + inn * 1.3f + side * 0.6f + Up * 0.1f, (head + cat) * 0.5f, 40f); break;
                        case "wide": Cut(mid + inn * 3.6f + Up * 1.0f, mid, 46f); break;
                        default: Cut(mid + inn * 2.0f + side * -0.5f + Up * 0.4f, mid, 42f); break;
                    }
                    return true;
                }
                default:   // the window: from beside the glass, back toward her face and Panther, the world outside at the frame edge
                {
                    var outw = Flat(Polar(1f, WindowA - 4f, 0f)).normalized; side = Vector3.Cross(Up, outw);   // side = toward her right
                    var catSide = Vector3.Dot(cat - head, side) >= 0f ? side : -side;
                    switch (kind)
                    {
                        case "lap": Cut(cat + outw * 0.5f + catSide * 0.6f + Up * 0.45f, cat + Up * 0.12f, 40f); break;                        // Panther, close, from above at the glass
                        case "ots": Cut(head - outw * 0.75f - catSide * 0.45f + Up * 0.15f, head + outw * 6f - Up * 2.2f, 48f); break;        // past her head (the side away from Panther) at the view
                        case "side": Cut(head + outw * 0.75f - catSide * 0.8f + Up * 0.05f, head - Up * 0.05f, 36f); break;                    // her face, three-quarter, the other side
                        case "wide": Cut(head - outw * 1.4f + catSide * 0.5f + Up * 0.6f, head + outw * 6f - Up * 2.5f, 52f); break;           // high, over her shoulder: the world outside
                        default: Cut(head + outw * 0.75f + catSide * 0.8f + Up * 0.05f, CurrentStage == Stage.WindowShoulder ? (head + cat) * 0.5f : head - Up * 0.05f, 36f); break;   // her face, three-quarter front (Panther on her shoulder in frame)
                    }
                    return true;
                }
            }
        }

        /// <summary>Mid-scene move to another staging (a short fade): e.g. from the sofa to the chair for the route display.</summary>
        IEnumerator SwitchStaging(string scene)
        {
            if (_staged && StageOf(scene) == CurrentStage) yield break;
            yield return UI.Fade(1f, 0.5f);
            _stroke = 0f; if (_strokeRoutine != null) { StopCoroutine(_strokeRoutine); _strokeRoutine = null; }
            if (_ik != null) _ik.ReleaseAll("the staging changes");
            if (_staged && (CurrentStage == Stage.Chair || CurrentStage == Stage.ChairCatConsole)) { _seated = false; _inPilotChair = false; Seven.SeatHips = null; Cat.LapFrame = null; }
            Seven.OnSofa = false; _staged = false; _shoulder = null; _stageCatAnim = null; Cat.ShowCurled(false); SevenRestPose.On(Seven.gameObject, SevenRestPose.Kind.None);
            if (Cat.Current == CatCompanion.Mode.OnLap) Cat.enabled = false;
            yield return EnterStaging(scene);
            _strokeRoutine = StartCoroutine(Stroke()); _stroke = 0.6f;
            CutPilot("two");
            yield return UI.Fade(0f, 0.6f);
        }

        /// <summary>The scene ends: hand IK released, Panther back to himself, 7 on her feet (a short fade covers it).</summary>
        IEnumerator ExitStaging(string why)
        {
            _stroke = 0f; if (_strokeRoutine != null) { StopCoroutine(_strokeRoutine); _strokeRoutine = null; }
            if (_ik != null) _ik.ReleaseAll(why);
            if (!_staged) yield break;
            var st = CurrentStage; _staged = false; _shoulder = null; _stageCatAnim = null; Cat.ShowCurled(false); SevenRestPose.On(Seven.gameObject, SevenRestPose.Kind.None);
            if (Cat.Anim != null) Cat.Anim.speed = 1f; if (_catPose != null) { _catPose.LookAt = null; _catPose.TailToward = null; }   // QF18 (4)
            if (st == Stage.Chair || st == Stage.ChairCatConsole)
            {
                if (st == Stage.ChairCatConsole) { Cat.transform.localScale = Vector3.one * Cat.RoamScale; Cat.transform.position = Flat(PilotHips + PilotRight * 1.1f); Cat.enabled = true; }
                yield return StandFromPilotChair();
            }
            else
            {
                yield return UI.Fade(1f, 0.5f);
                Seven.OnSofa = false; Seven.SeatHips = null;
                var stand = st == Stage.Sofa || st == Stage.FloorSofa ? Polar(5.6f, 166f, 0f) : Polar(6.6f, WindowA - 4f, 0f);
                Seven.transform.SetPositionAndRotation(stand, Quaternion.LookRotation(Inward(166f)));
                Seven.PlayClip("idle", 0f);
                Cat.transform.localScale = Vector3.one * Cat.RoamScale;
                Cat.transform.SetPositionAndRotation(Flat(stand + Inward(166f) * 1.2f + Vector3.Cross(Up, Inward(166f)) * 0.8f), Quaternion.identity);
                Cat.enabled = true;
                Seven.Cutscene = false; Seven.Frozen = false;
                yield return UI.Fade(0f, 0.5f);
            }
            if (Auto) Cap?.Log($"staging (QF16 3) '{StagedScene}' ended: {st} - hand IK left {_ik?.LeftW:0.00} right {_ik?.RightW:0.00}, Panther roaming {Cat.enabled}");
            CurrentStage = Stage.Chair;
        }
    }
}
