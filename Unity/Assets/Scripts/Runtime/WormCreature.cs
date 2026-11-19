using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 11 — a burrowing creature (not menacing in itself; the creatures are victims, driven by a signal). It
    /// travels along its route: surfacing in long arcs (rise / wriggle / dive), ground ripples + dust where it
    /// breaks the surface. <see cref="Drive"/> (the signal pulse) turns it toward a point - visibly changing direction.
    /// Round 12: <see cref="BaseY"/> = the ground it moves under (the basin, or the control chamber's floor);
    /// <see cref="Telegraph"/> = a rumble + dust running ahead before it surfaces (the player always gets a warning);
    /// <see cref="Withdraw"/> = it sinks into a burrow and is gone (alive - it goes home); <see cref="Calm"/> = it no
    /// longer follows a control route (after the threat is stopped it wanders slowly, away from the foundations).
    /// Creatures never die and are never hurt.
    /// </summary>
    public class WormCreature : MonoBehaviour
    {
        public Vector3[] Route;
        public float Speed = 2.6f, Depth = 2.2f, Surface = 0.6f, Period = 9f;
        public ParticleSystem Dust;
        [System.NonSerialized] public float BaseY = float.NaN;
        /// <summary>Round 12: back and forth along the route (a trough) instead of a loop.</summary>
        public bool PingPong;
        public bool Telegraph;
        public string Group;                         // the control device that drives it (chamber creatures)
        Animator _anim; int _i = 0, _dir = 1; float _t; Vector3? _driven; float _drivenUntil;
        public int Turns { get; private set; }
        public Vector3 Heading { get; private set; }
        public bool Withdrawn { get; private set; }
        public bool Calm { get; private set; }
        /// <summary>Above ground (it can bump into 7).</summary>
        public bool Surfaced => !Withdrawn && transform.position.y > GroundY - 0.35f;
        float GroundY => float.IsNaN(BaseY) ? VenusLayout.BasinCentre.y : BaseY;
        AudioSource _rumble;
        Vector3? _hole; float _sink;

        void Awake() { _anim = GetComponentInChildren<Animator>(); if (_anim != null) _anim.Play("wriggle", 0, Random.value); _t = Random.value * Period; }

        /// <summary>The signal: head for <paramref name="at"/> for a while (a clear turn, logged for the check run).</summary>
        public void Drive(Vector3 at, float seconds) { if (Calm || Withdrawn) return; _driven = at; _drivenUntil = Time.time + seconds; Turns++; }

        /// <summary>Its control is gone: it heads for the nearest burrow, sinks into it and leaves (alive).</summary>
        public void Withdraw(Vector3 hole) { if (Withdrawn || _hole.HasValue) return; _hole = hole; _driven = null; }

        /// <summary>The threat is stopped: free of the signal, it wanders slowly (its route now leads away from the foundations).</summary>
        public void SetCalm(Vector3[] route) { Calm = true; _driven = null; if (route != null && route.Length > 0) { Route = route; _i = 0; } Speed *= 0.55f; Period *= 1.4f; }

        void Update()
        {
            if (Withdrawn || Route == null || Route.Length == 0) return;
            _t += Time.deltaTime;
            Vector3 target;
            if (_hole.HasValue) target = _hole.Value;
            else target = _driven.HasValue && Time.time < _drivenUntil ? _driven.Value : Route[_i];
            if (!_driven.HasValue || Time.time >= _drivenUntil) _driven = null;
            var d = target - transform.position; d.y = 0f;
            if (!_hole.HasValue && d.magnitude < 2f && _driven == null)
            {
                if (PingPong && Route.Length > 1) { if (_i + _dir >= Route.Length || _i + _dir < 0) _dir = -_dir; _i += _dir; }
                else _i = (_i + 1) % Route.Length;
            }
            if (d.sqrMagnitude > 1e-3f)
            {
                var want = Quaternion.LookRotation(d);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, (_driven.HasValue || PingPong || _hole.HasValue ? 90f : 25f) * Time.deltaTime);
            }
            Heading = transform.forward;
            var p = transform.position + transform.forward * Speed * Time.deltaTime;
            float phase = Mathf.Sin(_t / Period * Mathf.PI * 2f);
            float y = GroundY + Mathf.Lerp(-Depth, Surface, (phase + 1f) * 0.5f);
            if (_hole.HasValue)
            {
                // going home: it surfaces once more, then sinks into the burrow and is gone
                if (d.magnitude < 1.5f) _sink += Time.deltaTime / 1.6f;
                y = Mathf.Lerp(Mathf.Max(y, GroundY + Surface * 0.5f), GroundY - Depth - 1.5f, _sink);
                if (_sink >= 1f) { Withdrawn = true; if (Dust != null) { var e = Dust.emission; e.enabled = false; } if (_rumble != null) _rumble.Stop(); gameObject.SetActive(false); return; }
            }
            p.y = y;
            transform.position = p;
            if (Dust != null)
            {
                // Round 12 telegraph: dust and a rumble run ahead of it while it is under the floor, about to surface
                bool near = Mathf.Abs(p.y - GroundY) < 0.8f;
                bool coming = Telegraph && phase < 0f && (phase > -0.55f) && Mathf.Cos(_t / Period * Mathf.PI * 2f) > 0f;
                var em = Dust.emission; em.enabled = near || coming;
                Dust.transform.position = new Vector3(p.x, GroundY, p.z) + transform.forward * (coming && !near ? 2.2f : 3f);
                if (Telegraph)
                {
                    if (_rumble == null) _rumble = AudioKit.Loop("creature_rumble", transform, null, 0f, AudioKit.Bus.Sfx, 1f, 22f);
                    if (_rumble != null) _rumble.volume = Mathf.MoveTowards(_rumble.volume, near || coming ? 0.55f : 0.08f, Time.deltaTime * 1.5f);
                }
            }
        }

        void OnDisable() { if (_rumble != null) _rumble.Stop(); }
    }
}
