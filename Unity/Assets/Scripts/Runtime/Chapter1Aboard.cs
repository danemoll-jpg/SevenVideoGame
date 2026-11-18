using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// Chapter 1 aboard the saucer (scenes 9-11), in the Round 3 saucer scene: the larger interior, the first meal at the
    /// galley refrigerator, the cat choosing to stay; then (Round 7) no end card: free exploration of the whole saucer
    /// (chair + reveal, gallery TV, sofa; the cat roams and naps). The menu's Continue starts straight in exploration.
    /// Only active when <see cref="Chapter1State.Aboard"/> is set (otherwise the saucer demo runs).
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

        bool Auto => DemoBoot.CaptureDir != null;
        ChapterCapture Cap => ChapterCapture.I;
        SaucerSeven Seven => Director.Seven;
        CatCompanion Cat => Director.Cat;
        public bool Active { get; private set; }

        void Start()
        {
            if (!Chapter1State.Aboard) { enabled = false; return; }
            Active = true;
            Director.ChapterMode = true;
            Cat.RoamScale = 1f; Cat.LapScale = 1f; Cat.StrollSpeed = 1.0f; Cat.TrotSpeed = 1.6f;
            Cat.transform.localScale = Vector3.one;
            UI.SetFade(1f);
            Seven.transform.SetPositionAndRotation(new Vector3(-1.2f, 0f, -4.6f), Quaternion.LookRotation(new Vector3(0.2f, 0f, 1f)));
            Cat.transform.SetPositionAndRotation(new Vector3(-0.2f, 0f, -5.4f), Quaternion.LookRotation(new Vector3(0.3f, 0f, 1f)));
            if (FridgeLight != null) FridgeLight.enabled = false;
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
                Dlg.LineStarted += l =>
                {
                    if (l.Id == "s09_01_cat") StartCoroutine(ShotLater("s09_larger_interior"));
                    if (l.Id == "s10_02_cat") StartCoroutine(ShotLater("s10_free_range"));
                    if (l.Id == "s11_08_cat") StartCoroutine(ShotLater("s11_perhaps_i_should_stay"));
                };
            }
            if (Auto) AudioKit.Log = s => Cap.Log("audio: " + s);
            StartCoroutine(Chapter1State.Explore ? Explore(true) : Flow());
        }

        IEnumerator ShotLater(string name) { yield return Wait(0.35f); yield return Cap.Shot(name); }
        IEnumerator Wait(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        IEnumerator Until(Func<bool> cond, string what, float autoTimeout)
        {
            float t0 = Time.time;
            while (!cond())
            {
                if (Auto && Time.time - t0 > autoTimeout) { Cap.Soft++; Cap.Log("SOFT-LOCK? timed out waiting for " + what); yield break; }
                yield return null;
            }
            if (Auto) Cap.Log($"ok: {what} ({Time.time - t0:0.0}s)");
        }

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

        void Shot(Vector3 pos, Vector3 look)
        {
            var cam = Director.Cam;
            cam.Hold = true;
            StartCoroutine(Ease(cam.transform, pos, look));
        }
        IEnumerator Ease(Transform cam, Vector3 pos, Vector3 look)
        {
            var p0 = cam.position; var r0 = cam.rotation; var r1 = Quaternion.LookRotation(look - pos);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t);
                cam.position = Vector3.Lerp(p0, pos, u); cam.rotation = Quaternion.Slerp(r0, r1, u);
                yield return null;
            }
            cam.position = pos; cam.rotation = r1;
        }

        void Objective(string s) { UI.SetObjective(s); if (Auto) Cap.Log("objective: " + s); }

        IEnumerator Flow()
        {
            Cat.ForceAction = "sit";
            yield return Wait(0.5f);
            yield return UI.Fade(0f, 1.6f);
            // Scene 9: the cat stops and looks around.
            yield return Dlg.PlayScene(9);
            Cat.ForceAction = null;
            Objective("Find the kitchen");
            Director.PromptOverride = null;
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
            if (Auto) Cap.Log("ok: fridge reached");

            // Scene 10: 7 opens the refrigerator. A fixed wide framing from the deck (the walk camera would be
            // squeezed against her in the nook).
            Seven.Frozen = true;
            Shot(GalleyCamPos, GalleyCamLook);
            var want = Quaternion.LookRotation(FridgeFacing);
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, want, 400f * Time.deltaTime); yield return null; }
            Cat.ForceTarget = CatMealSpot;
            var d0 = FridgeDoor.localRotation;
            if (FridgeLight != null) FridgeLight.enabled = true;
            AudioKit.Play("fridge_open", FridgeDoor.position, 0.9f, 0f, AudioKit.Bus.Sfx, 12f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.9f) { FridgeDoor.localRotation = d0 * Quaternion.Euler(0f, -105f * Mathf.SmoothStep(0f, 1f, t), 0f); yield return null; }
            yield return Dlg.PlayScene(10, 1, 1);
            // "She turns to look at him."
            var toCat = Flat(Cat.transform.position - Seven.transform.position);
            if (toCat.sqrMagnitude > 1e-3f)
                for (float t = 0f; t < 0.6f; t += Time.deltaTime) { Seven.transform.rotation = Quaternion.RotateTowards(Seven.transform.rotation, Quaternion.LookRotation(toCat), 300f * Time.deltaTime); yield return null; }
            yield return Dlg.PlayScene(10, 2, 4);
            Cat.ForceTarget = null; Cat.ForceAction = "sit";
            yield return Wait(1.2f);
            // Scene 11: choosing to stay.
            AudioKit.Play("fridge_close", FridgeDoor.position, 0.9f, 0f, AudioKit.Bus.Sfx, 12f);
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { FridgeDoor.localRotation = Quaternion.Slerp(FridgeDoor.localRotation, d0, t / 0.6f); yield return null; }
            FridgeDoor.localRotation = d0;
            if (FridgeLight != null) FridgeLight.enabled = false;
            yield return Dlg.PlayScene(11);
            Seven.Frozen = false;
            yield return Wait(1.5f);     // a quiet beat

            // Round 7 (3d): no "To be continued". The player keeps 7 and explores the whole saucer.
            yield return Explore(false);
        }

        /// <summary>
        /// Free exploration after scene 11 (or straight from the menu's Continue): deck, galley, her chair with the
        /// reveal, the gallery TV and sofa. The cat roams / naps as on the demo saucer (house-cat size) and still hops
        /// onto her lap when she sits. A quiet objective once. Check runs walk her to the chair, sit, open the pause menu.
        /// </summary>
        IEnumerator Explore(bool fromContinue)
        {
            if (fromContinue)
            {
                Seven.transform.SetPositionAndRotation(new Vector3(-1.2f, 0f, -4.6f), Quaternion.LookRotation(new Vector3(0.2f, 0f, 1f)));
                yield return Wait(0.3f);
                yield return UI.Fade(0f, 1.4f);
            }
            Seven.Frozen = false;
            Director.Cam.Hold = false;
            Director.ChapterMode = false;              // the chair's reveal works again
            Director.PromptOverride = null;
            Cat.ForceAction = null; Cat.ForceTarget = null;
            Cat.enabled = true;
            Cat.RoamScale = 1f; Cat.LapScale = 1f; Cat.transform.localScale = Vector3.one;
            Explored = true;
            if (Auto) Cap.Log($"free exploration of the saucer begins ({(fromContinue ? "Continue from the menu" : "after scene 11")}); music {MusicSystem.Current}");
            Objective("Explore the ship");
            yield return Wait(5f);
            Objective(null);
            if (!Auto) yield break;

            // ---- check run: explore a little, sit in the chair (the cat comes to her lap), pause menu, finish.
            Seven.ScriptDriven = true;
            yield return Cap.Shot(fromContinue ? "x01_continue_exploring" : "s11_free_exploration");
            yield return AutoWalk(new Vector3(1.5f, 0f, -2.5f), Seven.StandSpot);
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
            yield return Cap.Shot(fromContinue ? "x02_continue_seated_cat" : "s11_explore_seated_cat_on_lap");
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
