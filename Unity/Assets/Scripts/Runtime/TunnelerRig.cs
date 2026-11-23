using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF30 (6/7/8): Tunneler's mining rig (built by the finale's builder: ring-segmented hull, riveted plates, the spiral-drilled
    /// rim, hover thrusters underneath, work lights, cables into him, two articulated arms with small spiral drills, the yoke he
    /// grips) - the same rig at his entrance, in the fight and in the barrier. At runtime: the thrusters' exhaust + the dust they
    /// kick up, the arm drills (spinning when he attacks), the work lights, and the solid footprint the others can't pass
    /// (<see cref="Radius"/>: 7, Panther and the camera stay outside it; <see cref="DeckZone"/>: while it sits on the floor after a
    /// stun she can jump up onto its low deck).
    /// </summary>
    public class TunnelerRig : MonoBehaviour
    {
        public Transform Hull;                  // the part that bobs / tilts (the fight's Body)
        public Transform[] ArmDrills;           // the small spiral drills at the arms' ends
        public Transform[] Arms;                // the arms (they swing forward on a strike)
        public Light[] WorkLights;
        public Transform[] Thrusters;           // nozzle mouths (pointing down)
        public float Radius = 2.35f;            // the rim (the footprint nobody passes)
        public float DeckTop = 0.62f;           // the deck's height above the hull's base
        public float DeckR = 1.85f;             // the walkable deck inside the rim
        public float ArmDrillWant;              // deg/s (0 = stopped)
        public bool ThrustersOn = true;
        public bool Dropped { get; private set; }
        public float ArmDrillSpeed { get; private set; }
        public int ArmDrillReversals { get; private set; }
        readonly List<ParticleSystem> _exhaust = new List<ParticleSystem>();
        ParticleSystem _dust;
        AudioSource _hum;
        Ground.Zone _deck;
        public Ground.Zone DeckZone => _deck;
        float _armAngle;

        void Start()
        {
            if (Thrusters != null) foreach (var t in Thrusters) if (t != null) _exhaust.Add(Exhaust(t));
            _dust = Dust();
            _hum = AudioKit.Loop("europa_drill", transform, null, 0.12f, AudioKit.Bus.Sfx, 2f, 22f);
            if (_hum != null) _hum.pitch = 0.55f;
        }
        ParticleSystem Exhaust(Transform at)
        {
            var go = new GameObject("Exhaust"); go.transform.SetParent(at, false); go.transform.localRotation = Quaternion.identity;
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main; m.loop = true; m.startLifetime = 0.35f; m.startSpeed = 4f; m.startSize = 0.28f; m.startColor = new Color(1f, 1f, 1f, 0.35f); m.maxParticles = 120; m.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 40f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 8f; sh.radius = 0.12f;
            go.transform.rotation = Quaternion.LookRotation(Vector3.down);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Markers.AdditiveMatTex(0.6f);
            ps.Play(); return ps;
        }
        ParticleSystem Dust()
        {
            var go = new GameObject("RigDust"); go.transform.SetParent(transform, false); go.transform.localPosition = Vector3.up * 0.05f;
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main; m.loop = true; m.startLifetime = 1.6f; m.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.6f); m.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.2f); m.startColor = new Color(0.62f, 0.62f, 0.62f, 0.22f); m.maxParticles = 220; m.simulationSpace = ParticleSystemSimulationSpace.World; m.gravityModifier = -0.02f;
            var em = ps.emission; em.rateOverTime = 26f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = Radius * 0.9f; sh.radiusThickness = 0.2f; go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local; vel.radial = new ParticleSystem.MinMaxCurve(1.4f);
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0.0f, 0f), new GradientAlphaKey(0.25f, 0.2f), new GradientAlphaKey(0f, 1f) }); col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Markers.AdditiveMatTex(0.25f);
            ps.Play(); return ps;
        }

        /// <summary>Stunned: the thrusters sputter out and the rig drops onto the floor, tilted toward her - its low deck is a step she can
        /// jump onto. Back up: the thrusters fire and it rises.</summary>
        public void Drop(bool down)
        {
            if (Dropped == down) return;
            Dropped = down; ThrustersOn = !down;
            AudioKit.Play(down ? "mars_barrier_off" : "ast_steam_whistle", transform.position + Vector3.up, down ? 0.8f : 0.45f);
            if (down) AudioKit.Play("land_hard", transform.position, 1f);
            Debug.Log($"[Seven] Tunneler's rig (QF30 8): {(down ? "the thrusters sputter out - it drops onto the floor, tilted toward her (its deck is in reach)" : "the thrusters fire - it rises")}");
        }
        public void PowerDown()
        {
            ThrustersOn = false; ArmDrillWant = 0f;
            if (WorkLights != null) foreach (var l in WorkLights) if (l != null) l.enabled = false;
            if (_hum != null) StartCoroutine(AudioKit.Fade(_hum, 0f, 2.5f, true));
        }
        /// <summary>Every sound of the rig stops now (QF30 22: nothing carries into the ending).</summary>
        public void Silence() { if (_hum != null) _hum.Stop(); foreach (var e in _exhaust) if (e != null) e.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); if (_dust != null) _dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }

        void LateUpdate()
        {
            foreach (var e in _exhaust) if (e != null) { var em = e.emission; em.rateOverTime = ThrustersOn ? 40f : (Dropped && Random.value < 0.04f ? 300f : 0f); }   // a sputter now and then when dropped
            if (_dust != null) { var em = _dust.emission; em.rateOverTime = ThrustersOn ? 26f : 0f; }
            float dt = Time.deltaTime;
            ArmDrillSpeed = Mathf.MoveTowards(ArmDrillSpeed, Mathf.Max(0f, ArmDrillWant), (ArmDrillWant > ArmDrillSpeed ? 2600f : 420f) * dt);
            float step = ArmDrillSpeed * dt; if (step < 0f) ArmDrillReversals++;
            _armAngle += step;
            if (ArmDrills != null) foreach (var d in ArmDrills) if (d != null) d.localRotation = Quaternion.AngleAxis(_armAngle % 360f, Vector3.forward);
            if (_hum != null) _hum.pitch = Mathf.Lerp(0.55f, 1.1f, Mathf.Clamp01(ArmDrillSpeed / 1400f));
        }

        // ---------------------------------------------------------------- the footprint and the deck (QF30 7 / 8)
        /// <summary>The deck as a walkable zone (masked to its circle, its height following the tilt), switched on only while the rig sits
        /// on the floor; inserted first so it wins over the chamber floor.</summary>
        public void UpdateDeck(bool on)
        {
            if (_deck == null)
            {
                _deck = new Ground.Zone { Name = "RigDeck", Enabled = false };
                _deck.Mask = (x, z) => { var c = transform.position; return (new Vector2(x - c.x, z - c.z)).sqrMagnitude < DeckR * DeckR; };
                _deck.Field = (x, z) => DeckHeight(new Vector3(x, 0f, z));
                Ground.Zones.Insert(0, _deck);
            }
            var p = transform.position;
            _deck.Min = new Vector2(p.x - DeckR, p.z - DeckR); _deck.Max = new Vector2(p.x + DeckR, p.z + DeckR);
            _deck.Enabled = on;
        }
        /// <summary>The deck's surface under (x, z): the hull's deck plane (it tilts with the hull).</summary>
        public float DeckHeight(Vector3 q)
        {
            var h = Hull != null ? Hull : transform;
            var top = h.TransformPoint(Vector3.up * DeckTop); var n = h.up;
            if (Mathf.Abs(n.y) < 0.2f) return top.y;
            // plane through top with normal n: n.(p - top) = 0 -> y
            return top.y - (n.x * (q.x - top.x) + n.z * (q.z - top.z)) / n.y;
        }
        void OnDestroy() { if (_deck != null) Ground.Zones.Remove(_deck); }
    }
}
