using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>"System" - the receiving chamber's maintenance machine (AST_02). An obstacle, never an enemy: while the scanner
    /// holds 7 it keeps itself between her and the manual door control.
    /// QF21 (2): a cooler design of the Great Machine Age - a squat rounded brass-and-steel body that trundles on three jointed
    /// spindly legs (a tripod gait: one leg lifts at a time, the body bobs), one big lens-eye whose iris shutter narrows to
    /// scan and opens wide while it speaks, a speaker grille that pulses with the words (and the brass speaking-horns in the
    /// chamber walls with it), a fan of scanning light swept head to toe, a steam whistle from its stack on each announcement,
    /// a ticking dial on its back. It tilts its head (mechanical curiosity) at "Hello." and "Material?".</summary>
    public class AstMaintBot : MonoBehaviour
    {
        public Transform Lens; public Renderer Arrow; public Renderer LensGlow;
        public Transform Body, Head, Fan, Dial; public Transform[] Iris, Hips, Knees; public Renderer Grille; public Renderer[] Horns; public ParticleSystem Stack;
        public Vector3 Goal; public float Speed = 2.2f;
        public bool Busy;
        /// <summary>The voice while System speaks (the dialogue source): the iris, the grille and the wall horns follow it.</summary>
        [System.NonSerialized] public AudioSource Voice;
        [System.NonSerialized] public Transform ScanTarget;
        public int Tilts { get; private set; }
        public int Whistles { get; private set; }
        public float Walked { get; private set; }
        float _bob, _gait, _talk, _tilt, _tiltTo, _scan, _dialT; readonly float[] _buf = new float[256];
        Quaternion[] _hip0, _knee0; Vector3 _body0;

        void Start()
        {
            if (Hips != null) { _hip0 = new Quaternion[Hips.Length]; for (int i = 0; i < Hips.Length; i++) if (Hips[i] != null) _hip0[i] = Hips[i].localRotation; }
            if (Knees != null) { _knee0 = new Quaternion[Knees.Length]; for (int i = 0; i < Knees.Length; i++) if (Knees[i] != null) _knee0[i] = Knees[i].localRotation; }
            if (Body != null) _body0 = Body.localPosition;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var p = transform.position; var g = new Vector3(Goal.x, p.y, Goal.z);
            var to = g - p; float moved = 0f;
            if (to.magnitude > 0.05f)
            {
                var np = Vector3.MoveTowards(p, g, Speed * dt); moved = (np - p).magnitude; transform.position = np; Walked += moved;
                var f = new Vector3(to.x, 0f, to.z); if (f.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(f), dt * 4f);
            }
            _bob += dt;
            // the tripod gait: the cycle advances with the distance walked; one leg at a time lifts (hip swings, knee folds)
            float speed = moved / Mathf.Max(1e-4f, dt), walk = Mathf.Clamp01(speed / 0.6f);
            _gait += moved * 2.6f;
            if (Hips != null && _hip0 != null)
                for (int i = 0; i < Hips.Length; i++)
                {
                    if (Hips[i] == null) continue;
                    float ph = Mathf.Repeat(_gait + i / 3f, 1f), lift = ph < 0.33f ? Mathf.Sin(ph / 0.33f * Mathf.PI) : 0f;
                    float swing = (ph < 0.33f ? Mathf.Lerp(-1f, 1f, ph / 0.33f) : Mathf.Lerp(1f, -1f, (ph - 0.33f) / 0.67f)) * 16f * walk;
                    Hips[i].localRotation = _hip0[i] * Quaternion.Euler(-lift * 22f * walk, swing, 0f);
                    if (Knees != null && i < Knees.Length && Knees[i] != null) Knees[i].localRotation = _knee0[i] * Quaternion.Euler(lift * 30f * walk, 0f, 0f);
                }
            if (Body != null) Body.localPosition = _body0 + Vector3.up * (Mathf.Abs(Mathf.Sin(_gait * Mathf.PI * 3f)) * 0.04f * walk + Mathf.Sin(_bob * 2.1f) * 0.008f);
            // the voice: level from the dialogue source (muted runs: a steady stand-in so the captures show it)
            float lvl = 0f;
            if (Voice != null && Voice.isPlaying)
            {
                Voice.GetOutputData(_buf, 0); foreach (var s in _buf) lvl = Mathf.Max(lvl, Mathf.Abs(s));
                if (lvl <= 0.0001f && DemoBoot.Muted) lvl = 0.25f + 0.25f * Mathf.Abs(Mathf.Sin(Time.time * 7.1f)) * Mathf.Abs(Mathf.Sin(Time.time * 2.3f));
            }
            else Voice = null;
            _talk = Mathf.Lerp(_talk, Mathf.Clamp01(lvl * 2.5f), dt * 16f);
            // the iris: narrow while it scans, wide open with the words, a slow breath otherwise
            _scan = Mathf.MoveTowards(_scan, ScanTarget != null ? 1f : 0f, dt * 2f);
            float open = Mathf.Clamp01(0.55f + 0.08f * Mathf.Sin(_bob * 1.3f) - 0.4f * _scan + 0.45f * _talk);
            if (Iris != null) for (int i = 0; i < Iris.Length; i++) if (Iris[i] != null) Iris[i].localRotation = Quaternion.Euler(0f, 0f, i * 45f + Mathf.Lerp(38f, 0f, open));
            if (LensGlow != null) Glowing.Set(LensGlow, 0.7f + 0.9f * _talk + 0.4f * _scan + 0.15f * Mathf.Sin(_bob * 5f));
            if (Grille != null) Glowing.Set(Grille, 0.12f + 2.2f * _talk);
            if (Horns != null) foreach (var h in Horns) Glowing.Set(h, 0.08f + 1.8f * _talk);
            // the head: looks at what it scans (or gently about), plus the tilt of curiosity
            _tilt = Mathf.MoveTowards(_tilt, _tiltTo, dt * 70f);
            if (Head != null)
            {
                float pitch = 0f, yaw = Mathf.Sin(_bob * 1.1f) * 10f;
                if (ScanTarget != null)
                {
                    var tp = ScanTarget.position + Vector3.up * 1.0f; var lp = transform.InverseTransformPoint(tp) - Head.localPosition;
                    yaw = Mathf.Clamp(Mathf.Atan2(lp.x, lp.z) * Mathf.Rad2Deg, -45f, 45f); pitch = Mathf.Clamp(-Mathf.Atan2(lp.y, new Vector2(lp.x, lp.z).magnitude) * Mathf.Rad2Deg, -30f, 30f);
                }
                Head.localRotation = Quaternion.Euler(pitch, yaw, _tilt);
            }
            if (Lens != null) Lens.localRotation = Quaternion.identity;
            // the scanning fan: swept head to toe over what it scans
            if (Fan != null)
            {
                bool on = _scan > 0.05f;
                if (Fan.gameObject.activeSelf != on) Fan.gameObject.SetActive(on);
                if (on && ScanTarget != null)
                {
                    float d = Vector3.Distance(Fan.position, ScanTarget.position + Vector3.up);
                    float sweep = Mathf.Lerp(-12f, 34f, 0.5f + 0.5f * Mathf.Sin(_bob * 2.4f));
                    Fan.localRotation = Quaternion.Euler(sweep, 0f, 0f); Fan.localScale = new Vector3(0.6f, 1f, d) * _scan;
                }
            }
            // the ticking dial on its back
            if (Dial != null) { _dialT += dt; if (_dialT > 0.5f) { _dialT = 0f; Dial.localRotation *= Quaternion.Euler(0f, 0f, -12f); } }
        }

        public void Face(Vector3 at) { var f = at - transform.position; f.y = 0f; if (f.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(f); }
        public void ShowArrow(bool on, Vector3 at = default, Vector3 dir = default)
        {
            if (Arrow == null) return;
            Arrow.gameObject.SetActive(on);
            if (on) { Arrow.transform.position = at + Vector3.up * 0.03f; Arrow.transform.rotation = Quaternion.LookRotation(dir.sqrMagnitude > 0.01f ? dir : Vector3.forward); Glowing.Set(Arrow, 1.4f); }
        }
        /// <summary>A beat of mechanical curiosity: the head tilts to one side, holds, comes back.</summary>
        public void Tilt(float deg = 24f, float hold = 1.1f) { Tilts++; StartCoroutine(TiltCo(deg, hold)); }
        IEnumerator TiltCo(float deg, float hold) { _tiltTo = deg; AudioKit.Play("ast_servo_small", transform.position + Vector3.up, 0.5f, 0.05f, AudioKit.Bus.Sfx, 15f); for (float t = 0f; t < hold; t += Time.deltaTime) yield return null; _tiltTo = 0f; }
        /// <summary>An announcement: a whistle of steam from the stack.</summary>
        public void Whistle()
        {
            Whistles++;
            if (Stack != null) Stack.Play(true);
            AudioKit.Play("ast_steam_whistle", transform.position + Vector3.up * 1.7f, 0.6f, 0.05f, AudioKit.Bus.Sfx, 25f);
        }
    }
}
