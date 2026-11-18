using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 11 — a burrowing creature (not menacing in itself; the creatures are victims, driven by a signal). It
    /// travels the basin along its route: surfacing in long arcs (rise / wriggle / dive), ground ripples + dust where it
    /// breaks the surface. <see cref="Drive"/> (the signal pulse) turns it toward a point - visibly changing direction.
    /// </summary>
    public class WormCreature : MonoBehaviour
    {
        public Vector3[] Route;
        public float Speed = 2.6f, Depth = 2.2f, Surface = 0.6f, Period = 9f;
        public ParticleSystem Dust;
        Animator _anim; int _i; float _t; Vector3? _driven; float _drivenUntil;
        public int Turns { get; private set; }
        public Vector3 Heading { get; private set; }

        void Awake() { _anim = GetComponentInChildren<Animator>(); if (_anim != null) _anim.Play("wriggle", 0, Random.value); _t = Random.value * Period; }

        /// <summary>The signal: head for <paramref name="at"/> for a while (a clear turn, logged for the check run).</summary>
        public void Drive(Vector3 at, float seconds) { _driven = at; _drivenUntil = Time.time + seconds; Turns++; }

        void Update()
        {
            if (Route == null || Route.Length == 0) return;
            _t += Time.deltaTime;
            var target = _driven.HasValue && Time.time < _drivenUntil ? _driven.Value : Route[_i];
            if (!_driven.HasValue || Time.time >= _drivenUntil) _driven = null;
            var d = target - transform.position; d.y = 0f;
            if (d.magnitude < 2f && _driven == null) _i = (_i + 1) % Route.Length;
            if (d.sqrMagnitude > 1e-3f)
            {
                var want = Quaternion.LookRotation(d);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, (_driven.HasValue ? 70f : 25f) * Time.deltaTime);
            }
            Heading = transform.forward;
            var p = transform.position + transform.forward * Speed * Time.deltaTime;
            float phase = Mathf.Sin(_t / Period * Mathf.PI * 2f);
            p.y = VenusLayout.BasinCentre.y + Mathf.Lerp(-Depth, Surface, (phase + 1f) * 0.5f);
            transform.position = p;
            if (Dust != null)
            {
                var em = Dust.emission; em.enabled = Mathf.Abs(p.y - VenusLayout.BasinCentre.y) < 0.8f;
                Dust.transform.position = new Vector3(p.x, VenusLayout.BasinCentre.y, p.z) + transform.forward * 3f;
            }
        }
    }
}
