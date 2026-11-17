using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>
    /// Chapter 1 aboard the saucer (scenes 9-11), in the Round 3 saucer scene: the larger interior, the first meal at the
    /// galley refrigerator, the cat choosing to stay; a quiet beat, the cat settles on the sofa, "To be continued", then
    /// back to the menu. Only active when <see cref="Chapter1State.Aboard"/> is set (otherwise the saucer demo runs).
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
            StartCoroutine(Flow());
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
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { FridgeDoor.localRotation = Quaternion.Slerp(FridgeDoor.localRotation, d0, t / 0.6f); yield return null; }
            FridgeDoor.localRotation = d0;
            if (FridgeLight != null) FridgeLight.enabled = false;
            yield return Dlg.PlayScene(11);
            Seven.Frozen = false;
            yield return Wait(1.5f);     // a quiet beat

            // The cat settles on the sofa (if furniture holds him up he simply appears there off-camera: never stuck).
            Shot(SofaCamPos, SofaCamLook);
            Cat.ForceAction = null; Cat.ForceTarget = SofaVia;
            float tc = Time.time;
            while (Flat(Cat.transform.position - SofaVia).magnitude > 0.35f && Time.time - tc < 6f) yield return null;
            Cat.ForceTarget = SofaFront;
            tc = Time.time;
            while (Flat(Cat.transform.position - SofaFront).magnitude > 0.3f && Time.time - tc < 6f) yield return null;
            if (Flat(Cat.transform.position - SofaFront).magnitude > 0.3f)
            {
                if (Auto) Cap.Log($"cat could not walk to the sofa (at {Cat.transform.position}) - placed there");
                Cat.transform.position = SofaFront;
            }
            else if (Auto) Cap.Log("ok: cat walked to the sofa");
            Cat.enabled = false;
            var anim = Cat.Anim;
            anim.speed = 1f; anim.CrossFadeInFixedTime("jump", 0.1f, 0);
            var from = Cat.transform.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f)
            {
                float u = Mathf.InverseLerp(0.3f, 0.85f, t);
                var p = Vector3.Lerp(from, SofaSeat, Mathf.SmoothStep(0f, 1f, u)); p.y += Mathf.Sin(u * Mathf.PI) * 0.3f;
                Cat.transform.position = p;
                var f = Flat(SofaSeat - from); if (f.sqrMagnitude > 1e-4f) Cat.transform.rotation = Quaternion.LookRotation(f);
                yield return null;
            }
            Cat.transform.position = SofaSeat;
            anim.CrossFadeInFixedTime("sleep", 0.6f, 0);
            var ff = Flat(Seven.transform.position - SofaSeat);
            yield return Wait(1.6f);
            if (Auto) yield return Cap.Shot("s11_cat_settles_on_sofa");
            yield return Wait(1.5f);
            yield return UI.Fade(1f, 2f);
            StartCoroutine(UI.Title("To be continued", 1.2f, 2.6f, 1.0f));
            yield return Wait(2.0f);
            if (Auto) yield return Cap.Shot("s11_to_be_continued");
            yield return Wait(2.8f);
            if (Auto) { Finish(); yield break; }
            Chapter1State.Reset();
            SceneManager.LoadScene(DemoMenu.SceneName);
        }

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
            Cap.Finish($"chapter 1 check run finished: soft-lock suspicions {Cap.Soft}, shots {Chapter1State.ShotCount}, game time {Time.time:0.0}s since boot");
        }
    }
}
