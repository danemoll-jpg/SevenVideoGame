using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Chapter 1 aboard the saucer, in the Round 3 saucer scene: scene 9 (the larger interior), then Round 10's scene 10
    /// "The galley" (Docs/DIALOGUE.md, replaces the old scenes 10-12) as an in-engine cutscene - 7 at the open
    /// refrigerator, the cat on the table; then at the table: she eats, he eats from his bowl (his mouth moves only then),
    /// the dodecahedron between them, the Elarim marking, "Venus, then." Then the quiet objective "Set course for Venus"
    /// and free exploration of the saucer (the chapter is complete; the menu's Continue starts there). At 7's seat in the
    /// galley, X "Set course" shows the honest placeholder: the Moon falls away behind the porthole, a "Chapter 2 —
    /// Venus (coming soon)" card, back to exploring. Only active when <see cref="Chapter1State.Aboard"/> is set.
    /// The cat stays house-cat size here (scale 1).
    /// </summary>
    public class Chapter1Aboard : MonoBehaviour
    {
        public SaucerDirector Director;
        public DialoguePlayer Dlg;
        public ChapterUI UI;
        public Transform FridgeDoor;
        public Light FridgeLight;
        public Vector3 FridgeSpot, FridgeFacing, CatMealSpot, SofaFront, SofaSeat;
        [Tooltip("Galley cutscene camera (scenes 10-11) and the sofa shot: position, look-at.")]
        public Vector3 GalleyCamPos, GalleyCamLook, SofaCamPos, SofaCamLook, SofaVia;
        [Header("Round 10: the galley set")]
        public Transform Seat7, SeatOther, Plate, Fork, Knife, Bowl, Moon, Porthole;
        public GameObject GalleyOutside;
        public ElarimShape Shape;
        public Vector3 TableCentre, TableAlong, TableOut;
        public float TableTopY = 0.745f;
        public CloseUpFocus Focus;
        [Tooltip("Renderer index of UI_Renderer (no fog pass) for the subtitle overlay camera; -1 = none.")]
        public int UiRendererIndex = -1;

        bool Auto => DemoBoot.CaptureDir != null && !DemoBoot.PlayerSim;     // QF6: the playtest plays it like a person
        ChapterCapture Cap => ChapterCapture.I;
        // QF6: read by the -playtest driver
        public static Chapter1Aboard I { get; private set; }
        public Vector3 SeatStandPos => SeatStand;
        public bool InGalleyScene { get; private set; }
        SaucerSeven Seven => Director.Seven;
        CatCompanion Cat => Director.Cat;
        public bool Active { get; private set; }

        SevenTableIK _ik;
        CloseUpFocus _focus;
        CatPoseOverlay _catPose;
        Vector3 _moonHome; Vector3 _moonScale;
        Vector3 _forkHome; Quaternion _forkHomeRot;

        // ---- galley layout (derived from the table frame)
        Vector3 Up => Vector3.up;
        float ShapeRest => MoonLayout.ShapeRadius * 0.795f;                       // dodecahedron inradius: resting on a face
        Vector3 BesidePlate => TableCentre + TableAlong * 0.26f + TableOut * 0.2f + Up * (TableTopY + ShapeRest);
        Vector3 Between => TableCentre + TableAlong * 0.1f + Up * (TableTopY + ShapeRest);
        Vector3 CatSitOnTable => TableCentre - TableAlong * 0.22f + TableOut * 0.12f + Up * TableTopY;
        Vector3 CatEatSpot => TableCentre - TableAlong * 0.25f + Up * TableTopY;
        Vector3 CatStudySpot => TableCentre - TableAlong * 0.12f + TableOut * 0.2f + Up * TableTopY;          // porthole side: the camera sees past him
        Vector3 SeatStand => Seat7.position + TableAlong * 0.45f - TableOut * 0.35f;        // where she stands beside her seat
        Vector3 PlateFood => Plate.position + Up * (TableTopY + 0.03f) - TableOut * 0.03f;

        void Start()
        {
            if (!Chapter1State.Aboard) { enabled = false; return; }
            I = this;
            Active = true;
            Director.ChapterMode = true;
            Cat.RoamScale = 1f; Cat.LapScale = 1f; Cat.StrollSpeed = 1.0f; Cat.TrotSpeed = 1.6f;
            Cat.transform.localScale = Vector3.one;
            UI.SetFade(1f);
            Seven.transform.SetPositionAndRotation(new Vector3(-1.2f, 0f, -4.6f), Quaternion.LookRotation(new Vector3(0.2f, 0f, 1f)));
            Cat.transform.SetPositionAndRotation(new Vector3(-0.2f, 0f, -5.4f), Quaternion.LookRotation(new Vector3(0.3f, 0f, 1f)));
            if (FridgeLight != null) FridgeLight.enabled = false;
            _ik = Seven.gameObject.AddComponent<SevenTableIK>();
            _focus = Focus != null ? Focus : new GameObject("GalleyFocus").AddComponent<CloseUpFocus>();
            StackSubtitleCamera();
            _catPose = Cat.gameObject.AddComponent<CatPoseOverlay>();
            if (Moon != null) { _moonHome = Moon.position; _moonScale = Moon.localScale; }
            if (Fork != null) { _forkHome = Fork.position; _forkHomeRot = Fork.rotation; }
            if (Shape != null)
            {
                if (Chapter1State.Explore) PlaceShape(BesidePlate, MarkingDown());
                else
                {
                    // QF5: she still carries it in the pouch at her belt (as on the Moon); she takes it out at the table
                    _carry = CarriedShape.Attach(Seven.gameObject, Shape.transform);
                    _carry.Carrying = true;
                    _carry.Set(CarriedShape.Where.Hip, 0f);
                }
            }
            Dlg.LineStarted += l => { if (!Chapter1State.Played.Contains(l.Id)) Chapter1State.Played.Add(l.Id); };
            AudioKit.Loop("amb_saucer", null, null, 1f, AudioKit.Bus.Ambience);
            MusicSystem.Play("aboard_saucer", 2.5f);
            if (Auto)
            {
                ChapterCapture.Ensure();
                Cap.Attach(Director.Cam.GetComponent<Camera>());
                Seven.ScriptDriven = true;
                Dlg.AutoSkipAfter = 0.9f;
                Director.Legend.AutoSwitch = false;
                Dlg.LineStarted += l => { if (l.Id == "s09_01_cat") StartCoroutine(ShotLater("s09_larger_interior")); };
                AudioKit.Log = s => Cap.Log("audio: " + s);
                Cap.Log($"galley: cat ear bones {(_catPose != null ? "pending" : "none")}; table at {TableCentre}, porthole at {(Porthole != null ? Porthole.position : Vector3.zero)}");
            }
            StartCoroutine(Chapter1State.Explore ? Explore(true) : Flow());
        }

        /// <summary>The galley's depth of field must not blur the subtitles: the chapter UI canvas moves to a stacked
        /// overlay camera (layer 5 only, no post-processing); the main camera stops drawing layer 5.</summary>
        void StackSubtitleCamera()
        {
            var main = Director.Cam.GetComponent<Camera>();
            var baseData = main != null ? main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() : null;
            UI.Build();
            var canvas = UI.GetComponentInChildren<Canvas>(true);
            if (main == null || baseData == null || canvas == null || UiRendererIndex < 0) { ChapterCapture.I?.Log("subtitle overlay camera: not set up (no UI renderer) - subtitles stay on the main camera"); return; }
            var go = new GameObject("SubtitleCamera"); go.transform.SetParent(main.transform, false);
            var ui = go.AddComponent<Camera>();
            ui.cullingMask = 1 << 5; ui.clearFlags = CameraClearFlags.Nothing; ui.nearClipPlane = 0.05f; ui.farClipPlane = 10f;
            ui.fieldOfView = main.fieldOfView;
            var uiData = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            uiData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Overlay; uiData.renderPostProcessing = false;
            uiData.SetRenderer(UiRendererIndex);                 // the plain renderer: no SevenHeightFog pass over the UI
            baseData.cameraStack.Add(ui);
            main.cullingMask &= ~(1 << 5);
            foreach (var t in canvas.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;
            canvas.worldCamera = ui;
            canvas.planeDistance = 0.12f;
        }

        void Update()
        {
            // The Moon / the dark outside the porthole and the dodecahedron don't dissolve: hide them during the ship reveal.
            bool whole = Director.Reveal == null || Director.Reveal.Progress < 0.03f;
            if (GalleyOutside != null && GalleyOutside.activeSelf != whole) GalleyOutside.SetActive(whole);
            if (Shape != null && Shape.gameObject.activeSelf != whole) Shape.gameObject.SetActive(whole);
        }

        // ------------------------------------------------------------------------------------- helpers
        IEnumerator ShotLater(string name) { yield return Wait(0.35f); yield return Cap.Shot(name); }
        IEnumerator Shot(string name) { if (Auto) yield return Cap.Shot(name); }
        IEnumerator Wait(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        /// <summary>Check runs: 7's moves are camera-relative, so convert a world direction.</summary>
        IEnumerator AutoWalk(params Vector3[] pts)
        {
            foreach (var p in pts)
            {
                float t0 = Time.time;
                while (Flat(p - Seven.transform.position).magnitude > 0.3f && Time.time - t0 < 20f)
                {
                    var d = Flat(p - Seven.transform.position).normalized;
                    var f = Flat(Director.Cam.transform.forward).normalized; var r = new Vector3(f.z, 0f, -f.x);
                    Seven.ScriptMove = new Vector2(Vector3.Dot(d, r), Vector3.Dot(d, f));
                    yield return null;
                }
            }
            Seven.ScriptMove = Vector2.zero;
        }

        /// <summary>Camera: ease (or cut with seconds 0) to a framing.</summary>
        void CamTo(Vector3 pos, Vector3 look, float seconds = 0.9f, float fov = -1f)
        {
            var cam = Director.Cam;
            cam.Hold = true;
            _shotSerial++;
            if (_camRoutine != null) StopCoroutine(_camRoutine);
            _camRoutine = StartCoroutine(Ease(cam.transform, pos, look, seconds, fov));
        }
        Coroutine _camRoutine;
        int _shotSerial;
        IEnumerator Ease(Transform cam, Vector3 pos, Vector3 look, float seconds, float fov)
        {
            var c = cam.GetComponent<Camera>();
            float f0 = c != null ? c.fieldOfView : 60f, f1 = fov > 0f ? fov : f0;
            var p0 = cam.position; var r0 = cam.rotation; var r1 = Quaternion.LookRotation(look - pos);
            for (float t = 0f; seconds > 0f && t < 1f; t += Time.deltaTime / seconds)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                cam.position = Vector3.Lerp(p0, pos, u); cam.rotation = Quaternion.Slerp(r0, r1, u);
                if (c != null) c.fieldOfView = Mathf.Lerp(f0, f1, u);
                yield return null;
            }
            cam.position = pos; cam.rotation = r1;
            if (c != null) c.fieldOfView = f1;
        }
        // compatibility with the scene 9 / exploration code
        void Shot(Vector3 pos, Vector3 look) => CamTo(pos, look, 0.8f);

        void Objective(string s) { UI.SetObjective(s); if (Auto) Cap.Log("objective: " + s); }

        IEnumerator Turn(Transform who, Vector3 toward, float seconds, float degPerSec = 300f)
        {
            var d = Flat(toward - who.position);
            if (d.sqrMagnitude < 1e-4f) yield break;
            var want = Quaternion.LookRotation(d);
            for (float t = 0f; t < seconds; t += Time.deltaTime) { who.rotation = Quaternion.RotateTowards(who.rotation, want, degPerSec * Time.deltaTime); yield return null; }
        }

        // ---- voice lines: one at a time, with the script's real silences (only the ship's hum and eating sounds)
        float _lineEnd = -1f;
        readonly List<string> _silenceLog = new List<string>();
        IEnumerator Say(string id, bool holdPause = true)
        {
            var l = Dlg.Bank.Get(id);
            if (l == null) { if (Auto) { Cap.Soft++; Cap.Log("MISSING voice line " + id); } yield break; }
            if (Auto && _lineEnd > 0f) Cap.Log($"line {id}: starts {Time.time - _lineEnd:0.00}s after the previous line ended");
            if (_eyeLines.TryGetValue(id, out var eyeShot)) StartCoroutine(EyeCloseUp(l, eyeShot));
            else if (id == "s10_21_cat") StartCoroutine(CatEyeCloseUp(l));
            else if (AutoCut && Time.time - _lastCut > 3.5f && l.IsCat != _lastSpeakerCat) { if (l.IsCat) OverSevensShoulder(); else BehindTheCat(); }
            _lastSpeakerCat = l.IsCat;
            float t0 = Time.time;
            yield return Dlg.PlayOne(l, false);
            _lineEnd = Time.time;
            if (Auto) Cap.Log($"line {id} played {Time.time - t0:0.00}s of {(l.Clip != null ? l.Clip.length : 0f):0.00}s, subtitle '{l.Text}' (cat mouth moving: {(l.IsCat ? (_catPose.Chewing && _catPose.Eat > 0.5f ? "chewing (eating)" : "no") : "-")})");
            if (holdPause) yield return Silence(Mathf.Max(Dlg.DefaultGap, l.PauseAfter), id);
        }
        // ---- Dan's camera direction for the conversation: gentle cuts on line changes (not every line), over-the-shoulder
        // from both sides as the main shots, eye close-ups at the key moments, the two-shot for the quiet beats.
        bool AutoCut;
        float _lastCut = -99f;
        bool _lastSpeakerCat;
        readonly System.Collections.Generic.Dictionary<string, string> _eyeLines = new System.Collections.Generic.Dictionary<string, string>
        {
            { "s10_06_7", "s10_eyes_1_those_people" },
            { "s10_14_7", "s10_eyes_2_not_somewhere_new" },
            { "s10_16_7", "s10_eyes_3_remembering" },
            { "s10_28_7", "s10_eyes_4_explain_what_i_saw" },
        };
        readonly System.Collections.Generic.HashSet<string> _shotOnce = new System.Collections.Generic.HashSet<string>();
        void Cut(Vector3 pos, Vector3 look, float fov, string capture = null, float focus = -1f, float focal = 70f, float aperture = 2.2f)
        {
            CamTo(pos, look, 0f, fov);
            _lastCut = Time.time;
            if (focus > 0f) _focus.Set(true, focus, focal, aperture); else _focus.Set(false);
            if (capture != null && Auto && _shotOnce.Add(capture)) StartCoroutine(ShotLater(capture));
        }
        Vector3 SevenHead => Seven.Bone(HumanBodyBones.Head) != null ? Seven.Bone(HumanBodyBones.Head).position : Seven.transform.position + Up * 1.2f;
        Vector3 SevenEyes => SevenHead + Up * 0.04f + Seven.transform.forward * 0.085f;
        /// <summary>Over 7's shoulder, across the table at the cat; her shoulder and hair soft in the foreground.</summary>
        void OverSevensShoulder()
        {
            var her = Seven.transform; var cat = _catPose.Eyes;
            var pos = SevenHead - her.forward * 0.62f + her.right * 0.3f + Up * 0.16f;
            Cut(pos, cat + Up * 0.02f, 34f, "s10_ots_over_her_shoulder", Vector3.Distance(pos, cat), 60f, 2.8f);
        }
        /// <summary>From behind the cat, low at table height, looking up across the table at 7; his ears soft in front.</summary>
        void BehindTheCat()
        {
            var cf = Flat(Cat.transform.forward).normalized; var cr = Vector3.Cross(Vector3.up, cf);
            var pos = _catPose.Eyes - cf * 0.32f - cr * 0.1f + Up * 0.01f;
            pos.y = Mathf.Max(pos.y, TableTopY + 0.16f);
            Cut(pos, SevenEyes - Up * 0.04f, 30f, "s10_ots_behind_the_cat", Vector3.Distance(pos, SevenEyes), 60f, 2.8f);
        }
        /// <summary>7's eyes and brow (energy point visible above), shallow focus, a slow subtle push-in over the line.</summary>
        IEnumerator EyeCloseUp(VoiceBank.Line l, string capture)
        {
            var f = Seven.transform.forward; var r = Seven.transform.right;
            Vector3 P(float d) => SevenEyes + f * d + r * 0.03f + Up * 0.01f;
            Cut(P(EyeDistance + 0.05f), SevenEyes - Up * 0.005f, EyeFov, null, EyeDistance + 0.05f, 50f, 8f);
            float len = l.Clip != null ? l.Clip.length : 2f;
            bool shot = false;
            int serial = _shotSerial;
            for (float t = 0f; t < len + 0.6f && serial == _shotSerial; t += Time.deltaTime)
            {
                float d = Mathf.Lerp(EyeDistance + 0.05f, EyeDistance, Mathf.SmoothStep(0f, 1f, t / (len + 0.6f)));
                var cam = Director.Cam.transform; cam.position = P(d); cam.rotation = Quaternion.LookRotation(SevenEyes - Up * 0.005f - cam.position);
                _focus.Focus(d);
                if (!shot && t > len * 0.6f) { shot = true; if (Auto) yield return Cap.Shot(capture); }
                yield return null;
            }
        }
        /// <summary>Backed off if the Tripo face texture is too soft at extreme close range (see the report).</summary>
        const float EyeDistance = 0.36f, EyeFov = 26f;   // backed off from 0.30 m / 24 deg: the Tripo face is soft / faceted closer
        IEnumerator CatEyeCloseUp(VoiceBank.Line l)
        {
            var cf = Flat(Cat.transform.forward).normalized; var cr = Vector3.Cross(Vector3.up, cf);
            var e = _catPose.Eyes;
            var pos = e + cf * 0.26f - cr * 0.05f + Up * 0.015f;
            Cut(pos, e, 22f, null, 0.26f, 50f, 8f);
            yield return Wait(Mathf.Min(1.4f, l.Clip != null ? l.Clip.length * 0.5f : 1f));
            if (Auto) yield return Cap.Shot("s10_cat_eyes_that_marking");
        }

        IEnumerator Silence(float seconds, string after)
        {
            float t0 = Time.time;
            yield return Wait(seconds);
            if (seconds >= 1.0f) { var s = $"silence after {after}: {Time.time - t0:0.00}s (script {seconds:0.0}s)"; _silenceLog.Add(s); if (Auto) Cap.Log(s); }
        }

        // ---- the dodecahedron
        Quaternion MarkingDown() => ElarimShape.FaceToward(Vector3.down, TableAlong);
        Quaternion MarkingUp() => ElarimShape.FaceToward(Vector3.up, -TableAlong);
        /// <summary>The marked face toward the cat (and up a little) - what he studies after "Turn it over".</summary>
        Quaternion MarkingToCat() => ElarimShape.FaceToward((Vector3.up * 0.8f - TableOut * 0.45f - TableAlong * 0.3f).normalized, TableOut);
        CarriedShape _carry;
        void PlaceShape(Vector3 at, Quaternion rot)
        {
            if (_carry != null) _carry.Set(CarriedShape.Where.Away, 0f);
            Shape.Hover = false;
            Shape.transform.SetParent(null, true);
            Shape.transform.SetPositionAndRotation(at, rot);
            Shape.Glow = 0.9f;
        }

        Transform Hand(bool right) => Seven.Bone(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
        Vector3 Palm(bool right)
        {
            var h = Hand(right); var m = Seven.Bone(right ? HumanBodyBones.RightMiddleProximal : HumanBodyBones.LeftMiddleProximal);
            return h == null ? Seven.transform.position + Vector3.up : m == null ? h.position : Vector3.Lerp(h.position, m.position, 0.55f);
        }
        Vector3 Mouth()
        {
            var head = Seven.Bone(HumanBodyBones.Head);
            if (head == null) return Seven.transform.position + Vector3.up * 1.15f;
            return head.position + Seven.transform.forward * 0.1f + Vector3.down * 0.08f;
        }

        bool _forkInHand, _shapeInLeft;
        void LateUpdate()
        {
            if (!Active) return;
            if (_forkInHand && Fork != null)
            {
                var p = Palm(true);
                var toMouth = (Mouth() - p).normalized; var toPlate = (PlateFood - p).normalized;
                float lift = Mathf.InverseLerp(TableTopY + 0.12f, Mouth().y - 0.05f, p.y);
                var dir = Vector3.Slerp(toPlate, toMouth, lift);
                Fork.SetPositionAndRotation(p - dir * 0.03f, Quaternion.LookRotation(dir, Vector3.up));
            }
            if (_shapeInLeft && Shape != null) Shape.transform.position = Palm(false) + Vector3.up * (MoonLayout.ShapeRadius * 0.6f) + Seven.transform.forward * 0.02f;
        }

        // ---- 7's small moves at the table (IK on the seated clip)
        Coroutine _eatRoutine;
        bool _eating;
        int _bites;
        void StartEating(int maxBites = 99) { StopEating(); _eating = true; _eatRoutine = StartCoroutine(EatLoop(maxBites)); }
        void StopEating() { _eating = false; if (_eatRoutine != null) StopCoroutine(_eatRoutine); _eatRoutine = null; }
        IEnumerator EatLoop(int maxBites)
        {
            int n = 0;
            while (_eating && n < maxBites)
            {
                // to the plate, a small bite, up to her mouth, a pause, back down; ~4-6 s apart
                _ik.LookPos = PlateFood; _ik.LookW = 0.7f;
                _ik.RightPos = PlateFood + Up * 0.02f; _ik.RightW = 0.85f;
                yield return Wait(0.9f);
                AudioKit.Play("fork_clink", PlateFood, 0.35f, 0.08f, AudioKit.Bus.Sfx, 6f);
                yield return Wait(0.3f);
                _ik.RightPos = Mouth(); _ik.LookW = 0.35f; _ik.LookPos = Mouth() + Seven.transform.forward * 0.6f;
                yield return Wait(1.0f);
                _bites++; n++;
                yield return Wait(0.5f);
                _ik.RightPos = PlateFood + Up * 0.07f + Seven.transform.right * 0.06f; _ik.RightW = 0.6f;
                yield return Wait(1.6f + UnityEngine.Random.Range(0.4f, 1.6f));
            }
        }

        // ---- the cat on the table (CatCompanion off; clips + overlay driven here)
        void CatClip(string clip, float fade = 0.3f, float speed = 1f) { Cat.Anim.speed = speed; Cat.Anim.CrossFadeInFixedTime(clip, fade, 0); }
        IEnumerator CatWalkTo(Vector3 to, Vector3 face, float seconds)
        {
            var p0 = Cat.transform.position;
            CatClip("walk", 0.2f, 0.55f);
            var dir = Flat(to - p0);
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
            {
                Cat.transform.position = Vector3.Lerp(p0, to, Mathf.SmoothStep(0f, 1f, t));
                if (dir.sqrMagnitude > 1e-4f) Cat.transform.rotation = Quaternion.RotateTowards(Cat.transform.rotation, Quaternion.LookRotation(dir), 240f * Time.deltaTime);
                yield return null;
            }
            Cat.transform.position = to;
            CatClip("idle", 0.3f);
            yield return Turn(Cat.transform, Cat.transform.position + face, 0.5f, 220f);
        }
        Vector3 _catEatPos;
        const float BowlRim = 0.212f;            // raised feeder: bowl on a 16 cm stand, rim 21 cm above the table
        /// <summary>Head down in the bowl: shift him so his muzzle is over the bowl's middle (the rim hides his mouth).</summary>
        void AlignCatToBowl()
        {
            var want = Bowl.position; var m = _catPose.Muzzle;
            var d = Flat(want - m);
            if (d.magnitude > 0.25f) d = d.normalized * 0.25f;
            Cat.transform.position += d;
            _catEatPos = Cat.transform.position;
            if (Auto) Cap.Log($"cat at his bowl: muzzle {Flat(want - _catPose.Muzzle).magnitude:0.000} m from the bowl centre after aligning (moved {d.magnitude:0.000} m), muzzle {_catPose.Muzzle.y - TableTopY:0.000} m above the table (bowl rim {BowlRim:0.000})");
        }
        AudioSource _catEatLoop;
        void CatEats(bool on)
        {
            _catPose.Eat = on ? 1f : 0f; _catPose.Chewing = on;
            if (on && _catEatLoop == null) _catEatLoop = AudioKit.Loop("cat_eating", null, Bowl != null ? Bowl.position : (Vector3?)null, 0.35f, AudioKit.Bus.Sfx, 1f, 8f);
            if (!on && _catEatLoop != null) { StartCoroutine(AudioKit.Fade(_catEatLoop, 0f, 0.6f, true)); _catEatLoop = null; }
        }

        // ------------------------------------------------------------------------------------- scene 9 + 10
        IEnumerator Flow()
        {
            if (Chapter1State.GalleyStart)
            {
                // QF6 Scene Select / Continue: straight into scene 10 at the open fridge (the cat already by the table)
                Seven.transform.SetPositionAndRotation(FridgeSpot, Quaternion.LookRotation(Flat(FridgeFacing)));
                Cat.transform.SetPositionAndRotation(TableCentre - TableAlong * 0.75f - TableOut * 0.55f, Quaternion.LookRotation(Flat(FridgeSpot - TableCentre)));
                yield return Wait(0.4f);
                StartCoroutine(UI.Fade(0f, 1.2f));
                ChapterCapture.I?.Log("aboard: galley start (scene 10)");
                yield return Galley();
                yield return Explore(false);
                yield break;
            }
            SaveGame.SaveProgress(Chapter1State.CpAboard);
            Cat.ForceAction = "sit";
            yield return Wait(0.5f);
            yield return UI.Fade(0f, 1.6f);
            // Scene 9: the cat stops and looks around.
            yield return Dlg.PlayScene(9);
            Cat.ForceAction = null;
            Objective("Find the kitchen");
            Director.PromptOverride = null;
            Cat.ForceTarget = TableCentre - TableAlong * 0.75f - TableOut * 0.55f;     // he heads for the table
            if (Auto) StartCoroutine(AutoWalk(new Vector3(-4.5f, 0f, -3.6f), FridgeSpot));
            bool opened = false;
            float t0 = Time.time;
            while (!opened)
            {
                float dF = Flat(Seven.transform.position - FridgeSpot).magnitude;
                bool near = Seven.Current == SaucerSeven.Mode.Free && dF < 1.1f;
                Director.PromptOverride = near ? "Open the fridge" : null;
                if (near && ((Auto && dF < 0.35f) || (!Auto && Director.Input.Sit.WasPressedThisFrame()))) opened = true;
                if (Auto && Time.time - t0 > 40f) { Cap.Soft++; Cap.Log("SOFT-LOCK? fridge not reached"); opened = true; }
                yield return null;
            }
            Director.PromptOverride = null;
            Objective(null);
            if (Auto) { Cap.Log("ok: fridge reached"); Dlg.AutoSkipAfter = -1f; Cap.Log("scene 10 plays every line in full with the script's pauses (check run too)"); }
            yield return Galley();
            yield return Explore(false);
        }

        /// <summary>Docs/DIALOGUE.md scene 10 "The galley" (Round 10), start to finish.</summary>
        IEnumerator Galley()
        {
            float sceneStart = Time.time;
            InGalleyScene = true;
            SaveGame.SaveProgress(Chapter1State.CpGalley);           // QF6: Continue / Scene Select can start here
            ChapterCapture.I?.Log("galley scene starts (scene 10)");
            MusicSystem.Stop(2.5f);                                  // only the ship's hum (and eating) under this scene
            Seven.Frozen = true; Seven.Cutscene = true;
            Director.Cam.Hold = true;
            // the cat hops onto the table and sits, tail curled around his feet
            Cat.ForceTarget = null; Cat.enabled = false;
            var hop0 = Cat.transform.position;
            CatClip("jump", 0.1f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.75f)
            {
                var p = Vector3.Lerp(hop0, CatSitOnTable, t); p.y = Mathf.Lerp(hop0.y, CatSitOnTable.y, Mathf.SmoothStep(0f, 1f, t)) + Mathf.Sin(t * Mathf.PI) * 0.35f;
                Cat.transform.position = p;
                Cat.transform.rotation = Quaternion.RotateTowards(Cat.transform.rotation, Quaternion.LookRotation(Flat(FridgeSpot - CatSitOnTable)), 360f * Time.deltaTime);
                yield return null;
            }
            Cat.transform.position = CatSitOnTable;
            CatClip("sit", 0.35f);
            // 7 at the fridge: the wide galley framing from the deck (fridge + table + the cat)
            // side-on, from the table end of the counter: her upper body at the open fridge (no body-focused angle)
            CamTo(PolarDeck(7.7f, 205.5f, 1.62f), FridgeSpot + Up * 1.42f + FridgeFacing * 0.3f, 0.9f, 42f);
            yield return Turn(Seven.transform, Seven.transform.position + FridgeFacing, 0.5f, 400f);
            bool clip = Seven.HasState("open_fridge");
            if (clip) Seven.PlayClip("open_fridge", 0.25f);
            if (clip) yield return Wait(0.55f);
            var d0 = FridgeDoor.localRotation;
            if (FridgeLight != null) FridgeLight.enabled = true;
            AudioKit.Play("fridge_open", FridgeDoor.position, 0.9f, 0f, AudioKit.Bus.Sfx, 12f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.9f) { FridgeDoor.localRotation = d0 * Quaternion.Euler(0f, -105f * Mathf.SmoothStep(0f, 1f, t), 0f); yield return null; }
            if (clip) { yield return Wait(Mathf.Max(0f, Seven.Length("open_fridge") * 0.6f - 1.45f)); Seven.PlayClip("idle", 0.6f); }
            if (Auto) Cap.Log($"fridge: 7 plays {(clip ? "open_fridge (Mixamo 'Opening')" : "idle (no open_fridge clip)")}; the cat sits on the table at {Cat.transform.position} (y {Cat.transform.position.y:0.00} = table top {TableTopY:0.00})");
            yield return Shot("s10_01_fridge_cat_on_table");
            yield return Say("s10_01_7");                         // So what do you eat? Canned food?
            yield return Say("s10_02_cat");                       // Chicken. But only free-range.
            // "She looks back at him."
            yield return Turn(Seven.transform, Cat.transform.position, 0.6f);
            _ik.LookPos = Cat.transform.position + Up * 0.2f; _ik.LookW = 0.8f;
            yield return Say("s10_03_cat");                       // Having experienced confinement... (+ she almost smiles)
            yield return Shot("s10_02_she_looks_back");
            // "...then returns to the refrigerator."
            _ik.LookW = 0f;
            yield return Turn(Seven.transform, Seven.transform.position + FridgeFacing, 0.6f);
            yield return Say("s10_04_7", false);                  // I'll see what we have.
            // "For a while, neither speaks." - the cut to the table happens inside this silence
            float silence0 = Time.time;
            float silenceLen = Dlg.Bank.Get("s10_04_7").PauseAfter;
            yield return UI.Fade(1f, 0.8f);
            AudioKit.Play("fridge_close", FridgeDoor.position, 0.6f, 0f, AudioKit.Bus.Sfx, 12f);
            FridgeDoor.localRotation = d0;
            if (FridgeLight != null) FridgeLight.enabled = false;
            yield return SitAtTable();
            // the cat stands over his bowl, eating with considerable attention
            Cat.transform.SetPositionAndRotation(CatEatSpot, Quaternion.LookRotation(TableAlong));
            CatClip("idle", 0.05f);
            CatEats(true);
            TwoShot(0f);
            yield return Wait(0.8f);
            AlignCatToBowl();
            yield return UI.Fade(0f, 1.0f);
            yield return TakeFromBelt();                            // QF5: from the pouch at her belt to beside her plate
            StartEating(2);                                         // "7 takes a few bites."
            yield return Wait(1.6f);
            yield return Shot("s10_03_eating_two_shot");
            CamTo(CatCloseCam(), Bowl.position + Up * (TableTopY + 0.08f), 1.2f, 40f);
            yield return Wait(1.6f);
            yield return Shot("s10_04_cat_eating_mouth_hidden");
            TwoShot(1.2f);
            while (Time.time - silence0 < silenceLen) yield return null;
            var s0 = $"silence after s10_04_7 ('For a while, neither speaks'): {Time.time - silence0:0.00}s (script {silenceLen:0.0}s), incl. the cut to the table";
            _silenceLog.Add(s0); if (Auto) Cap.Log(s0);
            // more bites until she has taken a few, then her eyes wander to the dodecahedron; the fork stops
            for (float t = 0f; _bites < 2 && t < 12f; t += Time.deltaTime) yield return null;
            StopEating();
            _ik.RightPos = PlateFood + Up * 0.1f + Seven.transform.right * 0.05f; _ik.RightW = 0.7f;   // fork held, stopped
            _ik.LookPos = Shape.transform.position; _ik.LookW = 1f;
            yield return Wait(1.4f);
            yield return Shot("s10_05_eyes_on_the_object");
            // "Cat notices her fork has stopped."
            _catPose.Eat = 0.45f; _catPose.Chewing = false;
            yield return Wait(0.5f);
            AutoCut = true;
            yield return Say("s10_05_cat");                       // You're letting it get cold.
            _catPose.Eat = 1f; _catPose.Chewing = true;
            yield return Say("s10_06_7");                         // Those people...
            // "One of his ears turns toward her."
            _catPose.EarToward = Mouth();
            _focus.Set(false); _lastCut = Time.time;
            CamTo(CatCloseCam(), Cat.transform.position + Up * 0.18f, 0.0f, 38f);
            yield return Wait(1.0f);
            yield return Shot("s10_06_ear_turns_toward_her");
            if (Auto) Cap.Log($"cat ear bones: {(_catPose.HasEars ? "earL + earR (cleanup_cat.py)" : "MISSING - no ear turn")}");
            yield return Say("s10_07_cat");                       // At the door?
            // "She nods."
            yield return Nod();
            yield return Say("s10_08_7");
            yield return Say("s10_09_cat");
            yield return Say("s10_10_7");
            // "She glances down at the object."
            _ik.LookPos = Shape.transform.position; _ik.LookW = 1f;
            yield return Say("s10_11_7");
            _ik.LookPos = Cat.transform.position + Up * 0.15f;
            yield return Say("s10_12_cat");                       // Useful.
            yield return Say("s10_13_7");                         // Yes.  (A pause.)
            yield return Say("s10_14_7");
            // "Cat lifts his head from the bowl."
            CatEats(false); _catPose.EarToward = null;
            yield return Wait(0.7f);
            yield return Say("s10_15_cat");                       // What did it feel like? (She searches for the answer.)
            _ik.LookPos = Porthole != null ? Porthole.position : Shape.transform.position;
            yield return Say("s10_16_7", false);                  // Remembering.  (The ship's hum fills the silence.)
            TwoShot(0f);
            yield return Silence(Mathf.Max(Dlg.DefaultGap, Dlg.Bank.Get("s10_16_7").PauseAfter), "s10_16_7");
            yield return Say("s10_17_cat");
            _ik.LookPos = Shape.transform.position;
            yield return Say("s10_18_7");                         // I don't know.
            // "7 places the dodecahedron beside her plate." (Dan's edit, 2026-11-17 21:47) - she puts the fork down to do it
            AutoCut = false;
            yield return SetDownFork();
            yield return MoveShape(Between, Shape.transform.rotation, 1.6f);
            _focus.Set(false); _lastCut = Time.time;
            CamTo(TableCentre - TableOut * 1.15f + Up * 1.15f + TableAlong * 0.05f, Between + Up * 0.05f, 1.0f, 44f);
            yield return Say("s10_19_7", false);                  // It happened when I touched this.
            yield return Shot("s10_07_object_beside_her_plate");
            yield return Silence(Mathf.Max(Dlg.DefaultGap, Dlg.Bank.Get("s10_19_7").PauseAfter), "s10_19_7");
            // "Cat leans closer."
            yield return CatWalkTo(CatStudySpot, Flat(Between - CatStudySpot), 1.3f);
            _catPose.Lean = 1f; _catPose.TiltSide(1f); _catPose.Tilt = 1f;
            yield return Wait(1.2f);
            yield return Shot("s10_08_cat_leans_closer");
            _catPose.TiltSide(-1f);
            yield return Wait(0.8f);
            yield return Say("s10_20_cat");                       // Turn it over.
            // "She rotates it." - the camera finds the marked face
            yield return RotateShape(MarkingToCat(), 1.5f);
            _catPose.Tilt = 0f;
            var mn = Shape.transform.rotation * ElarimShape.MarkNormal;
            var close = Shape.transform.position + (mn * 0.9f - TableOut * 0.3f).normalized * 0.34f;
            _focus.Set(false); _lastCut = Time.time;
            CamTo(close, Shape.transform.position, 0f, 30f);
            yield return Wait(0.8f);
            yield return Shot("s10_09_marking_closeup");
            if (Auto) LogMarkingVisible();
            yield return Wait(0.6f);
            AutoCut = true;
            yield return Say("s10_21_cat");                       // That marking. I've seen it before. (his eyes)
            // her attention sharpens
            _ik.LookPos = Shape.transform.position; _ik.LookW = 1f;
            yield return Say("s10_22_7");                         // Where?
            yield return Say("s10_23_cat");                       // On something from Venus. Elarim.
            yield return Say("s10_24_7");                         // So this is theirs?
            yield return Say("s10_25_cat");                       // I don't know. But their marking is on it.
            _catPose.Lean = 0f;
            // "She studies the object." (her thumb along its edge; over her shoulder)
            _ik.RightPos = Shape.transform.position + Up * (MoonLayout.ShapeRadius * 0.7f) - TableOut * 0.03f; _ik.RightW = 1f;
            _focus.Set(false); _lastCut = Time.time;
            CamTo(Seat7.position + TableAlong * 0.55f - TableOut * 0.75f + Up * 1.35f, Shape.transform.position + Up * 0.04f, 1.0f, 42f);
            yield return Wait(1.0f);
            yield return Shot("s10_10_she_studies_the_object");
            yield return Say("s10_26_7");                         // What was it doing on the Moon?
            yield return Say("s10_27_cat");                       // We could ask them.
            _ik.RightW = 0f;
            yield return Say("s10_28_7");                         // Maybe they can explain what I saw. (her eyes)
            // "She looks from the dodecahedron to him."
            _ik.LookPos = Cat.transform.position + Up * 0.18f;
            yield return Wait(0.5f);
            yield return Say("s10_29_7");                         // Us?
            // "Cat glances toward his bowl."
            yield return Turn(Cat.transform, Bowl.position, 0.5f, 220f);
            yield return Say("s10_30_cat");                       // Unless this was a farewell dinner.
            yield return Say("s10_31_7");
            yield return Say("s10_32_cat");                       // I did.
            yield return Say("s10_33_7");                         // You could go home.
            // "He settles beside the bowl."
            yield return CatWalkTo(_catEatPos, TableAlong, 1.2f);
            CatClip("sit", 0.4f);
            yield return Say("s10_34_cat", false);                // I'd like to know what happens next.
            TwoShot(0f);                                          // (She watches him for a moment.)
            yield return Silence(Mathf.Max(Dlg.DefaultGap, Dlg.Bank.Get("s10_34_cat").PauseAfter), "s10_34_cat");
            yield return Say("s10_35_7");                         // So would I.
            // "She sets the dodecahedron down, marking upward."
            AutoCut = false;
            yield return MoveShape(BesidePlate, MarkingUp(), 1.6f);
            _focus.Set(false); _lastCut = Time.time;
            CamTo(TableCentre - TableOut * 1.5f + Up * 1.25f - TableAlong * 0.1f, TableCentre + Up * 0.95f + TableAlong * 0.1f, 1.0f, 46f);
            yield return Say("s10_36_7", false);                  // Venus, then.
            yield return Shot("s10_11_venus_then");
            yield return Silence(1.2f, "s10_36_7");
            // "Cat returns to his food. 7 picks up her fork. This time, she eats."
            CatClip("idle", 0.4f);
            CatEats(true);
            yield return PickUpFork();
            if (Auto) Cap.Log($"cat back at his food: muzzle {Flat(Bowl.position - _catPose.Muzzle).magnitude:0.000} m from the bowl centre, {_catPose.Muzzle.y - TableTopY:0.000} m above the table");
            StartEating();
            yield return Wait(3.0f);
            yield return Shot("s10_12_this_time_she_eats");
            if (Auto) LogMarkingUp();
            yield return Wait(2.5f);
            yield return UI.Fade(1f, 2.0f);                       // fade softly
            _focus.Set(false);
            if (Auto) Cap.Log($"scene 10 'The galley' lasted {Time.time - sceneStart:0.0}s; bites {_bites}; cat chewing {_catPose.ChewSeconds:0.0}s (only while eating); silences: {string.Join(" | ", _silenceLog)}");
            // back to exploring: 7 beside her seat, the cat down from the table
            StopEating();
            CatEats(false); _catPose.Lean = 0f; _catPose.Tilt = 0f; _catPose.EarToward = null;
            _ik.RightW = 0f; _ik.LeftW = 0f; _ik.LookW = 0f;
            _forkInHand = false; Fork.SetPositionAndRotation(_forkHome, _forkHomeRot);
            yield return StandFromTable();
            Cat.transform.SetPositionAndRotation(Flat(TableCentre - TableAlong * 0.9f - TableOut * 0.6f), Quaternion.LookRotation(-TableOut));
            Cat.enabled = true;
            yield return Wait(0.4f);
        }

        Vector3 PolarDeck(float r, float a, float y) => new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * r, y, Mathf.Sin(a * Mathf.Deg2Rad) * r);
        /// <summary>Two-shot across the table from the deck: 7 side-on at left/right, the cat, the porthole with the Moon behind.</summary>
        void TwoShot(float seconds) { _lastCut = Time.time; _focus.Set(false); CamTo(TableCentre - TableOut * 2.15f + Up * 1.3f + TableAlong * 0.12f, TableCentre + Up * 0.95f + TableOut * 0.2f + TableAlong * 0.12f, seconds, 50f); }
        Vector3 CatCloseCam() => Cat.transform.position - TableOut * 0.75f + Up * 0.32f - TableAlong * 0.15f;

        IEnumerator Nod()
        {
            var basePos = _ik.LookPos; float w = _ik.LookW;
            _ik.LookW = 1f;
            for (float t = 0f; t < 0.8f; t += Time.deltaTime) { _ik.LookPos = basePos + Vector3.down * 0.35f * Mathf.Sin(t / 0.8f * Mathf.PI); yield return null; }
            _ik.LookPos = basePos; _ik.LookW = w;
        }

        IEnumerator SitAtTable()
        {
            var face = Flat(-TableAlong);
            Seven.transform.SetPositionAndRotation(Flat(Seat7.position), Quaternion.LookRotation(face));
            Seven.PlayClip("sitting_idle", 0f);
            yield return null; yield return null;
            // hips over the cushion (a little toward the table), height from the clip
            var hips = Seven.Hips.position; var want = Seat7.position - TableAlong * 0.04f;
            Seven.transform.position += Flat(want - hips);
            _forkInHand = _carry == null || Chapter1State.Explore;      // QF5: first the dodecahedron from her belt, then the fork
            if (Auto) Cap.Log($"seated at the table: hips {Seven.HipsAboveRoot:0.00} m above the floor (cushion top 0.50), facing the cat");
        }

        IEnumerator StandFromTable()
        {
            Seven.transform.SetPositionAndRotation(Flat(SeatStand), Quaternion.LookRotation(Flat(-TableOut)));
            Seven.PlayClip("idle", 0f);
            Seven.Cutscene = false; Seven.Frozen = false;
            yield return null;
        }

        IEnumerator SetDownFork()
        {
            _ik.RightPos = PlateFood + Seven.transform.right * 0.08f; _ik.RightW = 0.9f;
            yield return Wait(0.8f);
            _forkInHand = false;
            Fork.SetPositionAndRotation(Plate.position + Up * (TableTopY + 0.024f) + Seven.transform.right * 0.07f, Quaternion.LookRotation(Flat(-TableAlong + TableOut * 0.3f), Vector3.up));
            AudioKit.Play("fork_clink", Fork.position, 0.4f, 0.05f, AudioKit.Bus.Sfx, 6f);
            _ik.RightW = 0f;
            yield return Wait(0.4f);
        }

        /// <summary>QF5: seated, her right hand takes the dodecahedron out of the pouch at her belt and lifts it over the
        /// table; her left hand takes it and sets it beside her plate (marking down); then she picks up her fork.</summary>
        IEnumerator TakeFromBelt()
        {
            if (_carry == null || Shape == null) { if (Fork != null && !_forkInHand) yield return PickUpFork(); yield break; }
            _ik.RightPos = _carry.HipPoint + Up * 0.03f; _ik.RightW = 1f;
            _ik.LookPos = _carry.HipPoint; _ik.LookW = 0.55f;
            yield return Wait(0.8f);
            _carry.Set(CarriedShape.Where.Hand, 0.25f);
            AudioKit.Play("fork_clink", _carry.HipPoint, 0.08f, 0.3f, AudioKit.Bus.Sfx, 4f);
            yield return Wait(0.3f);
            if (Auto) { Cap.Log($"galley: she takes the dodecahedron from the pouch at her belt (hand {Vector3.Distance(_carry.Palm, _carry.HipPoint):0.00} m from the belt point)"); yield return Shot("qf5_07_galley_takes_it_from_her_belt"); }
            var front = Flat(Seven.transform.position) + Flat(Seven.transform.forward).normalized * 0.32f + Up * (TableTopY + 0.16f);
            _ik.RightPos = front; _ik.LookPos = front; _ik.LookW = 0.9f;
            yield return Wait(0.8f);
            if (Auto) yield return Shot("qf5_08_galley_over_the_table");
            var held = Shape.transform.position;
            _carry.Set(CarriedShape.Where.Away, 0f);
            Shape.transform.position = held;
            _ik.RightW = 0f;
            yield return MoveShape(BesidePlate, MarkingDown(), 1.1f);
            _carry.Carrying = true;                                 // the empty pouch stays on her belt
            if (Auto) Cap.Log($"galley: dodecahedron set beside her plate at {Shape.transform.position}");
            _ik.LookW = 0f;
            yield return PickUpFork();
        }

        IEnumerator PickUpFork()
        {
            _ik.RightPos = Fork.position + Up * 0.02f; _ik.RightW = 0.9f;
            yield return Wait(0.8f);
            _forkInHand = true;
            AudioKit.Play("fork_clink", Fork.position, 0.3f, 0.05f, AudioKit.Bus.Sfx, 6f);
        }

        /// <summary>Her left hand takes the dodecahedron from where it rests and sets it down at <paramref name="to"/>.</summary>
        IEnumerator MoveShape(Vector3 to, Quaternion endRot, float seconds)
        {
            _ik.LeftPos = Shape.transform.position + Up * 0.05f; _ik.LeftW = 1f;
            _ik.LookPos = Shape.transform.position; _ik.LookW = 1f;
            yield return Wait(0.7f);
            var p0 = Shape.transform.position; var r0 = Shape.transform.rotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                var p = Vector3.Lerp(p0, to, u) + Up * Mathf.Sin(u * Mathf.PI) * 0.07f;
                Shape.transform.SetPositionAndRotation(p, Quaternion.Slerp(r0, endRot, u));
                _ik.LeftPos = p + Up * 0.05f;
                _ik.LookPos = p;
                yield return null;
            }
            Shape.transform.SetPositionAndRotation(to, endRot);
            AudioKit.Play("fork_clink", to, 0.18f, 0.2f, AudioKit.Bus.Sfx, 6f);
            yield return Wait(0.25f);
            _ik.LeftW = 0f;
        }

        /// <summary>"She rotates it": lifted a little in both hands, turned, set down with the marking toward the cat.</summary>
        IEnumerator RotateShape(Quaternion endRot, float seconds)
        {
            var p0 = Shape.transform.position; var r0 = Shape.transform.rotation;
            _ik.LeftPos = p0 + TableOut * 0.07f; _ik.RightPos = p0 - TableOut * 0.07f; _ik.LeftW = 1f; _ik.RightW = 1f;
            _ik.LookPos = p0; _ik.LookW = 1f;
            yield return Wait(0.6f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                var p = p0 + Up * Mathf.Sin(u * Mathf.PI) * 0.06f;
                Shape.transform.SetPositionAndRotation(p, Quaternion.Slerp(r0, endRot, u));
                _ik.LeftPos = p + TableOut * 0.07f; _ik.RightPos = p - TableOut * 0.07f;
                yield return null;
            }
            // rest on the table: the lowest corner on the top (the marked face is not a resting face now)
            Shape.transform.rotation = endRot;
            Shape.transform.position = p0;
            SettleOnTable();
            _ik.LeftW = 0f; _ik.RightW = 0f;
        }
        void SettleOnTable()
        {
            float low = float.MaxValue;
            foreach (var v in PlatonicShape.Vertices(PlatonicShape.Kind.Dodecahedron))
                low = Mathf.Min(low, (Shape.transform.rotation * v * MoonLayout.ShapeRadius).y);
            var p = Shape.transform.position; p.y = TableTopY - low + 0.002f;
            Shape.transform.position = p;
        }

        void LogMarkingVisible()
        {
            var cam = Director.Cam.GetComponent<Camera>();
            var n = Shape.transform.rotation * ElarimShape.MarkNormal;
            float facing = Vector3.Dot(n, (cam.transform.position - Shape.transform.position).normalized);
            var vp = cam.WorldToViewportPoint(Shape.transform.position);
            Cap.Log($"marking close-up: marked face toward the camera {facing:0.00} (1 = straight on, must be > 0.5), shape on screen at ({vp.x:0.00}, {vp.y:0.00}), distance {vp.z:0.00} m");
        }
        void LogMarkingUp()
        {
            var n = Shape.transform.rotation * ElarimShape.MarkNormal;
            Cap.Log($"dodecahedron beside her plate at {Shape.transform.position}: marking up {Vector3.Dot(n, Vector3.up):0.00} (1 = straight up)");
        }

        /// <summary>The galley after the scene (Continue): the dodecahedron beside her plate, marking up, fork on the plate.</summary>
        void GalleyAfter()
        {
            if (Shape != null) PlaceShape(BesidePlate, MarkingUp());
            if (Fork != null) Fork.SetPositionAndRotation(_forkHome, _forkHomeRot);
            if (FridgeLight != null) FridgeLight.enabled = false;
        }

        // ------------------------------------------------------------------------------------- set course (placeholder)
        IEnumerator SetCourseWatch()
        {
            while (true)
            {
                bool near = Seven.Current == SaucerSeven.Mode.Free && !_settingCourse && Flat(Seven.transform.position - SeatStand).magnitude < 1.1f
                            && (Director.Reveal == null || Director.Reveal.Progress <= 0f) && !PauseMenu.IsOpen;
                Director.ExtraPrompt = near ? "Set course" : null;
                if (near && !Auto && Director.Input.Sit.WasPressedThisFrame()) yield return SetCourse();
                yield return null;
            }
        }
        bool _settingCourse;
        public int CourseSetCount { get; private set; }

        /// <summary>Honest placeholder: the Moon falls away behind the porthole, a "Chapter 2 — Venus (coming soon)" card,
        /// then back to free exploration (Venus itself is not built yet).</summary>
        IEnumerator SetCourse()
        {
            _settingCourse = true;
            Director.ExtraPrompt = null;
            Seven.Frozen = true;
            yield return Turn(Seven.transform, Porthole.position, 0.5f);
            CamTo(SeatStand - TableAlong * 0.2f - TableOut * 0.9f + Up * 1.55f, Porthole.position, 1.0f, 44f);
            yield return Wait(1.1f);
            yield return Shot("x03_set_course_porthole");
            AudioKit.Play("course_set", null, 0.8f);
            var p0 = Moon.position; var s0 = Moon.localScale; var outw = (p0 - Porthole.position).normalized;
            bool mid = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 4.2f)
            {
                float u = t * t * (3f - 2f * t);
                Moon.position = p0 + outw * 14f * u + Vector3.down * 2.2f * u + TableAlong * 1.2f * u;
                Moon.localScale = s0 * Mathf.Lerp(1f, 0.45f, u);
                if (!mid && t > 0.55f) { mid = true; StartCoroutine(Shot("x04_set_course_moon_falls_away")); }
                yield return null;
            }
            yield return UI.Fade(1f, 1.0f);
            if (Auto) StartCoroutine(ShotLater("x05_chapter2_card"));
            yield return UI.Title("Chapter 2 — Venus\n(coming soon)", 0.8f, 2.6f, 0.8f);
            Moon.position = _moonHome; Moon.localScale = _moonScale;
            Director.Cam.Hold = false;
            yield return UI.Fade(0f, 1.0f);
            Seven.Frozen = false;
            CourseSetCount++;
            if (Auto) Cap.Log($"set course placeholder shown ({CourseSetCount}x): the Moon fell away behind the porthole, card 'Chapter 2 — Venus (coming soon)', back to free exploration");
            _settingCourse = false;
        }

        // ------------------------------------------------------------------------------------- exploration
        /// <summary>
        /// Free exploration after scene 10 (or straight from the menu's Continue): deck, galley, her chair with the
        /// reveal, the gallery TV and sofa. The cat roams / naps as on the demo saucer (house-cat size) and still hops
        /// onto her lap when she sits. The quiet objective "Set course for Venus" once. Check runs try the set-course
        /// placeholder, walk her to the chair, sit, open the pause menu.
        /// </summary>
        IEnumerator Explore(bool fromContinue)
        {
            GalleyAfter();
            if (fromContinue)
            {
                Seven.transform.SetPositionAndRotation(new Vector3(-1.2f, 0f, -4.6f), Quaternion.LookRotation(new Vector3(0.2f, 0f, 1f)));
                yield return Wait(0.3f);
                yield return UI.Fade(0f, 1.4f);
            }
            else
            {
                Director.Cam.Hold = false;
                MusicSystem.Play("aboard_saucer", 4f);
                yield return UI.Fade(0f, 1.6f);
            }
            Seven.Frozen = false; Seven.Cutscene = false;
            Director.Cam.Hold = false;
            Director.ChapterMode = false;              // the chair's reveal works again
            Director.PromptOverride = null;
            Cat.ForceAction = null; Cat.ForceTarget = null;
            Cat.enabled = true;
            Cat.RoamScale = 1f; Cat.LapScale = 1f; Cat.transform.localScale = Vector3.one;
            Explored = true;
            InGalleyScene = false;
            SaveGame.SaveProgress(Chapter1State.CpExplore);
            ChapterCapture.I?.Log("aboard: free exploration (chapter done)");
            if (Auto) Cap.Log($"free exploration of the saucer begins ({(fromContinue ? "Continue from the menu" : "after scene 10 - chapter complete")}); music {MusicSystem.Current}");
            StartCoroutine(SetCourseWatch());
            Objective("Set course for Venus");
            yield return Wait(6f);
            Objective(null);
            if (!Auto) yield break;

            // ---- check run: the set-course placeholder at her seat, then the chair (the cat comes to her lap), pause menu.
            Seven.ScriptDriven = true;
            yield return Cap.Shot(fromContinue ? "x01_continue_exploring" : "s10_13_free_exploration");
            yield return AutoWalk(SeatStand);
            yield return Wait(0.6f);
            Cap.Log($"at her seat in the galley: context prompt '{Director.ExtraPrompt}' (legend context '{Director.Legend.ContextText}')");
            yield return Cap.Shot(fromContinue ? "x02_continue_set_course_prompt" : "x02_set_course_prompt");
            yield return SetCourse();
            yield return AutoWalk(new Vector3(-4.6f, 0f, -2.6f), new Vector3(-3.2f, 0f, 0.6f), new Vector3(-1.4f, 0f, 2.6f), Seven.StandSpot);
            if (!fromContinue)
            {
                // Round 9: the "X Sit" teaching prompt (once per playthrough), Xbox then keys
                yield return Wait(0.9f);
                Cap.Log($"teach prompt at the chair: '{Director.Legend.TeachText}' shown={Director.Legend.TeachShown}; permanent legend panels visible: {Director.Legend.VisiblePanels}");
                yield return Cap.Shot("t_sit_xbox");
                Director.Legend.SetMode(false); yield return Wait(0.6f);
                yield return Cap.Shot("t_sit_keys");
                Director.Legend.SetMode(true); yield return Wait(0.4f);
            }
            Seven.RequestSit();
            float t0 = Time.time;
            while (!Seven.IsSeated && Time.time - t0 < 8f) yield return null;
            Cap.Log($"sit in her chair after the chapter: seated={Seven.IsSeated}");
            t0 = Time.time;
            while (Cat.Current != CatCompanion.Mode.OnLap && Time.time - t0 < 25f) yield return null;
            Cap.Log($"cat on her lap after the chapter: {Cat.Current == CatCompanion.Mode.OnLap} (cat scale {Cat.Scale:0.00})");
            yield return Cap.Shot(fromContinue ? "x06_continue_seated_cat" : "s10_14_explore_seated_cat_on_lap");
            if (fromContinue) { SaveGame.ClearTestSave(); Cap.Finish($"continue check run finished: soft-lock suspicions {Cap.Soft}"); yield break; }
            // the pause menu (Start / Esc): Resume · Settings · Main menu · Quit
            PauseMenu.ScriptInput = true;
            PauseMenu.I.Open();
            yield return null;
            yield return Cap.Shot("s12_pause_menu");
            PauseMenu.I.Handle(1, 0, false, false); PauseMenu.I.Handle(0, 0, true, false);    // Settings
            PauseMenu.I.Handle(0, -1, false, false);                                          // music -10 %
            Cap.Log($"pause menu settings: music {AudioKit.MusicVolume:0.0} sfx {AudioKit.SfxVolume:0.0} voice {AudioKit.VoiceVolume:0.0} (check runs never save these)");
            yield return Cap.Shot("s12_pause_settings");
            PauseMenu.I.Handle(0, 1, false, false);                                           // back to 100 %
            PauseMenu.I.Handle(0, 0, false, true);                                            // back (Settings selected)
            PauseMenu.I.Handle(1, 0, false, false); PauseMenu.I.Handle(0, 0, true, false);    // Round 9: Controls page
            yield return null;
            yield return Cap.Shot("s12_pause_controls");
            PauseMenu.I.Handle(0, 0, false, true);                                            // back (Controls selected)
            PauseMenu.I.Handle(1, 0, false, false); PauseMenu.I.Handle(0, 0, true, false);    // QF6: Scene select page
            yield return null;
            yield return Cap.Shot("qf6_pause_scene_select");
            PauseMenu.I.Handle(0, 0, false, true);                                            // back (Scene select selected)
            PauseMenu.I.Handle(1, 0, false, false); PauseMenu.I.Handle(1, 0, false, false); PauseMenu.I.Handle(0, 0, true, false);   // Quit game -> confirm
            yield return Cap.Shot("s12_pause_quit_confirm");
            PauseMenu.I.Handle(0, 0, false, true);                                            // No / back
            PauseMenu.I.Close();
            Cap.Log($"pause menu closed; timeScale {Time.timeScale}");
            Finish();
        }
        public bool Explored { get; private set; }

        void Finish()
        {
            var bank = Dlg.Bank;
            var played = Chapter1State.Played;
            var missing = bank.Lines.Where(l => !played.Contains(l.Id)).Select(l => l.Id).ToList();
            var noClip = bank.Lines.Where(l => l.Clip == null).Select(l => l.Id).ToList();
            float total = bank.Lines.Where(l => l.Clip != null).Sum(l => l.Clip.length);
            Cap.Log($"voice lines: bank {bank.Lines.Count}, with audio {bank.Lines.Count - noClip.Count}, played+subtitled {played.Count}; " +
                    $"not played: [{string.Join(", ", missing)}]; no audio: [{string.Join(", ", noClip)}]; total voice length {total:0.0}s");
            Cap.Log($"cat max distance from 7 while following/leading on the Moon level: {Cap.CatMaxBehind:0.00} m");
            Cap.Log($"save written for Continue: {SaveGame.HasChapter1Save} (test key; removed now)");
            SaveGame.ClearTestSave();
            Cap.Finish($"chapter 1 check run finished: soft-lock suspicions {Cap.Soft}, shots {Chapter1State.ShotCount}, game time {Time.time:0.0}s since boot");
        }
    }
}
