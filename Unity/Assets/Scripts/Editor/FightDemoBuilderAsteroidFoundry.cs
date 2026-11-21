using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using A = Seven.AsteroidLayout;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF21 (10) — Dan: "the central hall is full of plain blocks". The no-boxes rule for the Machinist's hall: every block is
    /// a real Great Machine Age object that also has a job in the fight, and the hall reads as a living foundry. The fight's
    /// layout is unchanged (the arena A.ArenaX0..X1 / Z0..Z1 stays open; the three service stations, the lanes and the
    /// hoppers keep their places) - only the look changes:
    ///  - the service stations = clamp rigs (a turned pedestal, four pistons, a riveted crossbeam carrying the jaws);
    ///  - the floor lanes = belt conveyors (side rails, end drums with gears, rollers, flat parts riding along);
    ///  - the hoppers = turned, banded hoppers on chains; the cutter = a housing with a drive gear, pistons and a flue;
    ///  - the cradle = framed and lit (an arch with lamps, a light ring on the floor);
    ///  - cover outside the arena: flywheel casings (turning spokes), boiler drums (gauges, valves), crate stacks of archive
    ///    panels, a cooling tower venting steam; overhead gantries, chain hoists, a rail carrier crossing, hanging lamps;
    ///  - the throwables (telekinesis) = loose gears, pipe sections, gauge plates and small crates (DebrisMeshes).
    /// Obstacle circles for everything she could walk into: <see cref="A.HallProps"/> (the runtime adds them to Ground).
    /// </summary>
    public static partial class FightDemoBuilder
    {
        /// <summary>A clamp rig in place of the service station's block (the jaws are added by the caller at y 4.8).</summary>
        static void ClampRig(AK k, AsMats m, Vector3 c)
        {
            var (pv, pt) = k.Of(m.metalDark);
            Append(pv, pt, Lathe(new[] { new Vector2(0f, 0f), new Vector2(1.15f, 0f), new Vector2(1.2f, 0.15f), new Vector2(1.05f, 0.3f), new Vector2(0.95f, 1.0f), new Vector2(1.1f, 1.1f), new Vector2(1.1f, 1.25f), new Vector2(0f, 1.25f) }, 0f, 360f, 20, null, c));
            var (rv, rt) = k.Of(m.metal);
            for (int i = 0; i < 16; i++) Ellipsoid(rv, rt, c + Quaternion.Euler(0f, i * 22.5f, 0f) * new Vector3(1.1f, 1.18f, 0f), Vector3.one * 0.05f, 3, 6);
            // four pistons (cylinder + polished rod) up to the crossbeam
            foreach (var (sx, sz) in new[] { (-0.65f, -0.65f), (0.65f, -0.65f), (-0.65f, 0.65f), (0.65f, 0.65f) })
            {
                var b = c + new Vector3(sx, 1.25f, sz);
                Append(pv, pt, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.17f, 0f), new Vector2(0.17f, 1.6f), new Vector2(0.12f, 1.7f), new Vector2(0f, 1.7f) }, 0f, 360f, 10, null, b));
                var (sv, st) = k.Of(m.steel);
                Append(sv, st, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.07f, 0f), new Vector2(0.07f, 1.55f), new Vector2(0f, 1.55f) }, 0f, 360f, 8, null, b + Vector3.up * 1.7f));
            }
            Riveted(k, m.metal, m.metalDark, c + new Vector3(0f, 4.45f, 0f), new Vector3(2.0f, 0.35f, 1.9f));
            // a pressure gauge on the pedestal facing the arena
            var (gv, gt) = k.Of(m.steel);
            Append(gv, gt, RotX(Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.22f, 0f), new Vector2(0.22f, 0.06f), new Vector2(0f, 0.06f) }, 0f, 360f, 14), c + new Vector3(0f, 0.7f, -1.0f)));
        }

        /// <summary>A belt conveyor along x at z (the floor lane: flat, walkable - rails low, drums at the ends).</summary>
        static void BeltConveyor(AK k, AsMats m, float x0, float x1, float z, float w)
        {
            var (bv, bt) = k.Of(m.metalDark);
            MBox(bv, bt, new Vector3((x0 + x1) * 0.5f, 0.01f, z), new Vector3(x1 - x0, 0.02f, w), 0f, 0f);
            var (sv, st) = k.Of(m.steel);
            for (float x = x0 + 0.6f; x < x1 - 0.4f; x += 1.2f) MBox(sv, st, new Vector3(x, 0.024f, z), new Vector3(0.1f, 0.012f, w - 0.2f), 0f, 0f);   // belt slats
            foreach (var s in new[] { -1f, 1f })
            {
                Sweep(sv, st, new[] { new Vector3(x0, 0.045f, z + s * (w * 0.5f + 0.06f)), new Vector3(x1, 0.045f, z + s * (w * 0.5f + 0.06f)) }, 0.035f, false, 6);
                for (float x = x0 + 2f; x < x1; x += 4f) Ellipsoid(sv, st, new Vector3(x, 0.08f, z + s * (w * 0.5f + 0.06f)), new Vector3(0.09f, 0.07f, 0.09f), 3, 6);   // roller bosses
            }
            foreach (var xe in new[] { x0 + 0.5f, x1 - 0.5f })
            {
                var (dv, dt) = k.Of(m.metal);
                Append(dv, dt, RotX(Lathe(new[] { new Vector2(0f, -w * 0.5f - 0.2f), new Vector2(0.26f, -w * 0.5f - 0.2f), new Vector2(0.26f, w * 0.5f + 0.2f), new Vector2(0f, w * 0.5f + 0.2f) }, 0f, 360f, 14), new Vector3(xe, 0.0f, z)));   // low: she steps over
            }
        }

        /// <summary>A flywheel casing: a riveted housing with a big spoked wheel turning in its slot (the wheel is its own AstSpin).</summary>
        static void FlywheelCasing(string name, AK k, AsMats m, Vector3 c, float yaw, float r, Transform parent)
        {
            var q = Quaternion.Euler(0f, yaw, 0f);
            Riveted(k, m.metalDark, m.metal, c + new Vector3(0f, 0.5f, 0f), new Vector3(r * 2.4f, 1.0f, 2.0f), yaw);
            foreach (var s in new[] { -1f, 1f })
            {
                var (v, t) = k.Of(m.metal);
                var p = c + q * new Vector3(0f, 0f, s * 0.7f);
                MBox(v, t, p + Vector3.up * ((r + 0.6f) * 0.5f + 0.5f), new Vector3(0.5f, r + 0.6f, 0.25f), yaw, 0f);
            }
            var wheel = Gear(name + "_Wheel", c + Vector3.up * (r + 1.1f), q * Vector3.forward, r, 0.35f, 28, m.steel, parent);
            var sp = wheel.AddComponent<Seven.AstSpin>(); sp.Axis = Vector3.up; sp.Speed = 24f;
            var (bv, bt) = k.Of(m.steel); Sweep(bv, bt, new[] { c + Vector3.up * (r + 1.1f) - q * Vector3.forward * 0.9f, c + Vector3.up * (r + 1.1f) + q * Vector3.forward * 0.9f }, 0.18f, false, 10);   // the axle
        }

        /// <summary>A horizontal boiler drum on cradles: banded, with gauges and a valve wheel on top.</summary>
        static void BoilerDrum(AK k, AsMats m, Vector3 c, float yaw, float len, float r)
        {
            var q = Quaternion.Euler(0f, yaw, 0f);
            var (v, t) = k.Of(m.metal);
            var mesh = Lathe(new[] { new Vector2(0f, -len * 0.5f - r * 0.4f), new Vector2(r * 0.7f, -len * 0.5f - r * 0.3f), new Vector2(r, -len * 0.5f), new Vector2(r, len * 0.5f), new Vector2(r * 0.7f, len * 0.5f + r * 0.3f), new Vector2(0f, len * 0.5f + r * 0.4f) }, 0f, 360f, 20);
            var vs = mesh.vertices; for (int i = 0; i < vs.Length; i++) vs[i] = q * (Quaternion.Euler(0f, 0f, 90f) * vs[i]) + c + Vector3.up * (r + 0.5f); mesh.vertices = vs; Append(v, t, mesh);
            var (bv, bt) = k.Of(m.metalDark);
            for (int b = -2; b <= 2; b++)
            {
                var ring = Lathe(new[] { new Vector2(r + 0.04f, -0.08f), new Vector2(r + 0.04f, 0.08f) }, 0f, 360f, 20);
                var rv = ring.vertices; for (int i = 0; i < rv.Length; i++) rv[i] = q * (Quaternion.Euler(0f, 0f, 90f) * (rv[i] + Vector3.up * b * len * 0.22f)) + c + Vector3.up * (r + 0.5f); ring.vertices = rv; Append(bv, bt, ring);
            }
            foreach (var s in new[] { -0.32f, 0.32f }) MBox(bv, bt, c + q * new Vector3(s * len, 0.3f, 0f), new Vector3(0.4f, 0.6f, r * 1.6f), yaw, 0.02f);
            var (gv, gt) = k.Of(m.steel);
            for (int g = 0; g < 2; g++) { var gp = c + Vector3.up * (r * 2f + 0.65f) + q * new Vector3((g - 0.5f) * len * 0.4f, 0f, 0f); Append(gv, gt, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.05f, 0f), new Vector2(0.05f, 0.35f), new Vector2(0.2f, 0.35f), new Vector2(0.2f, 0.45f), new Vector2(0f, 0.45f) }, 0f, 360f, 12, null, gp)); }
            Sweep(gv, gt, Enumerable.Range(0, 17).Select(i => c + Vector3.up * (r * 2f + 0.9f) + q * new Vector3(len * 0.32f + Mathf.Cos(i / 16f * Mathf.PI * 2f) * 0.3f, 0f, Mathf.Sin(i / 16f * Mathf.PI * 2f) * 0.3f)).ToList(), 0.035f, true, 5);
            Sweep(gv, gt, new[] { c + Vector3.up * (r * 2f + 0.5f) + q * new Vector3(len * 0.32f, 0f, 0f), c + Vector3.up * (r * 2f + 0.9f) + q * new Vector3(len * 0.32f, 0f, 0f) }, 0.05f, false, 6);
        }

        /// <summary>A stack of riveted crates holding archive panels (their engraved edges showing).</summary>
        static void CrateStack(AK k, AsMats m, Vector3 c, float yaw, int n)
        {
            var q = Quaternion.Euler(0f, yaw, 0f);
            for (int i = 0; i < n; i++)
            {
                var off = i < 2 ? new Vector3(i == 0 ? -0.65f : 0.65f, 0.55f, 0f) : new Vector3(0f, 1.65f, 0.05f);
                Riveted(k, m.plate, m.metalDark, c + q * off, new Vector3(1.2f, 1.1f, 1.2f), yaw + i * 7f);
                var (sv, st) = k.Of(m.stone);
                for (int s = 0; s < 3; s++) MBox(sv, st, c + q * (off + new Vector3(-0.35f + s * 0.35f, 0.62f, 0f)), new Vector3(0.12f, 0.3f, 0.9f), yaw + i * 7f, 0f);
            }
        }

        /// <summary>A cooling tower: a banded flared stack with louvres, venting steam from the top.</summary>
        static void CoolingTower(string name, AK k, AsMats m, Vector3 c, float h, Transform parent)
        {
            var (v, t) = k.Of(m.metal);
            Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(1.9f, 0f), new Vector2(1.7f, h * 0.15f), new Vector2(1.25f, h * 0.6f), new Vector2(1.5f, h), new Vector2(1.35f, h), new Vector2(1.1f, h * 0.6f), new Vector2(0f, h * 0.6f) }, 0f, 360f, 22, null, c));
            var (bv, bt) = k.Of(m.metalDark);
            for (int b = 1; b < 6; b++) { float y = h * b / 6f; float rr = y < h * 0.6f ? Mathf.Lerp(1.7f, 1.25f, (y - h * 0.15f) / (h * 0.45f)) : Mathf.Lerp(1.25f, 1.5f, (y - h * 0.6f) / (h * 0.4f)); Append(bv, bt, Lathe(new[] { new Vector2(rr + 0.05f, y - 0.1f), new Vector2(rr + 0.05f, y + 0.1f) }, 0f, 360f, 22, null, c)); }
            var g = new GameObject(name + "_Steam").AddComponent<ParticleSystem>(); g.transform.SetParent(parent, false); g.transform.position = c + Vector3.up * h; g.transform.rotation = Quaternion.LookRotation(Vector3.up);
            var mm = g.main; mm.startLifetime = 3.5f; mm.startSpeed = 2.2f; mm.startSize = 2.4f; mm.startColor = new Color(1f, 1f, 1f, 0.22f); mm.maxParticles = 200; mm.loop = true;
            var em = g.emission; em.rateOverTime = 14f; var sh = g.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = 0.9f;
            g.GetComponent<ParticleSystemRenderer>().sharedMaterial = AdditiveMatTex("Ast_tower_steam", 0.5f);
        }

        /// <summary>The overhead: two gantry girders across the hall with a trolley and a chain hoist each, a rail carrier
        /// crossing high up along the hall, hanging lamps on chains.</summary>
        static void Overhead(AK k, AsMats m, Transform hall, float H)
        {
            foreach (var z in new[] { 80f, 114f })
            {
                Riveted(k, m.metalDark, m.metal, new Vector3(-360f, 17f, z), new Vector3(79f, 0.9f, 0.8f));
                var (tv, tt) = k.Of(m.metal);
                for (float x = -398f; x < -322f; x += 3f) Sweep(tv, tt, new[] { new Vector3(x, 16.6f, z - 0.4f), new Vector3(x + 1.5f, 17.4f, z - 0.4f), new Vector3(x + 3f, 16.6f, z - 0.4f) }, 0.05f, false, 5);   // the lattice
                float tx = z < 100f ? -374f : -340f;
                Riveted(k, m.metal, m.metalDark, new Vector3(tx, 16.2f, z), new Vector3(1.6f, 0.7f, 1.4f));            // the trolley
                var (cv, ct) = k.Of(m.steel);
                for (int i = 0; i < 18; i++) Ellipsoid(cv, ct, new Vector3(tx, 15.7f - i * 0.38f, z), i % 2 == 0 ? new Vector3(0.05f, 0.2f, 0.11f) : new Vector3(0.11f, 0.2f, 0.05f), 3, 6);   // the chain
                Sweep(cv, ct, Enumerable.Range(0, 9).Select(i => { float a = Mathf.Lerp(200f, -20f, i / 8f) * Mathf.Deg2Rad; return new Vector3(tx + Mathf.Cos(a) * 0.35f, 8.7f + Mathf.Sin(a) * 0.35f, z); }).ToList(), 0.07f, false, 6);   // the hook
            }
            // the rail carrier crossing high along the hall (a crate of panels on a monorail)
            var rk = new AK(); Pipe(rk, m.steel, new[] { new Vector3(-319f, 20f, 97f), new Vector3(-401f, 20f, 97f) }, 0.18f); rk.Save("Hall_HighRail", hall);
            var car = new GameObject("Hall_RailCrate").transform; car.SetParent(hall, false);
            var ck = new AK(); Riveted(ck, m.plate, m.metalDark, new Vector3(0f, -1.3f, 0f), new Vector3(1.6f, 1.2f, 1.2f)); var (hv, ht) = ck.Of(m.metalDark); Sweep(hv, ht, new[] { new Vector3(0f, -0.7f, 0f), new Vector3(0f, 0f, 0f) }, 0.06f, false, 6);
            foreach (var g in ck.Save("Hall_RailCrate_Mesh", car)) g.transform.localPosition = Vector3.zero;
            var rc = car.gameObject.AddComponent<Seven.AstRailCarrier>(); rc.Path = new[] { new Vector3(-319f, 20f, 97f), new Vector3(-401f, 20f, 97f) }; rc.Cars = new[] { car }; rc.Speed = 3f;
            // hanging lamps on chains
            foreach (var p in new[] { new Vector3(-392f, 0f, 92f), new Vector3(-328f, 0f, 92f), new Vector3(-360f, 0f, 74f), new Vector3(-360f, 0f, 118f) })
            {
                var (cv, ct) = k.Of(m.steel); for (int i = 0; i < 26; i++) Ellipsoid(cv, ct, new Vector3(p.x, H - 0.3f - i * 0.38f, p.z), i % 2 == 0 ? new Vector3(0.04f, 0.18f, 0.09f) : new Vector3(0.09f, 0.18f, 0.04f), 3, 6);
                AstLamp($"HallHangLamp{(int)-p.x}_{(int)p.z}", new Vector3(p.x, H - 10.4f, p.z), 18f, 0.9f, m.glow, m.metalDark, hall, 0.3f);
            }
        }

        /// <summary>The throwable debris (telekinesis): loose gears, pipe sections, gauge plates, small crates - clearly loose.</summary>
        static Mesh[] HallDebrisMeshes()
        {
            var list = new List<Mesh>();
            { var v = new List<Vector3>(); var t = new List<int>(); float r = 0.42f, th = 0.12f;
              Append(v, t, Lathe(new[] { new Vector2(r * 0.25f, -th * 0.5f), new Vector2(r, -th * 0.5f), new Vector2(r, th * 0.5f), new Vector2(r * 0.25f, th * 0.5f), new Vector2(r * 0.25f, -th * 0.5f) }, 0f, 360f, 24));
              for (int i = 0; i < 14; i++) { float a = i * 360f / 14f; MBox(v, t, Quaternion.Euler(0f, -a, 0f) * new Vector3(r + 0.05f, 0f, 0f), new Vector3(0.1f, th, 0.08f), -a, 0f); }
              list.Add(SaveMesh(Finish(v, t), "Ast_Debris_Gear")); }
            { var v = new List<Vector3>(); var t = new List<int>();
              Append(v, t, RotZ(Lathe(new[] { new Vector2(0.16f, -0.55f), new Vector2(0.22f, -0.55f), new Vector2(0.22f, -0.45f), new Vector2(0.17f, -0.45f), new Vector2(0.17f, 0.45f), new Vector2(0.22f, 0.45f), new Vector2(0.22f, 0.55f), new Vector2(0.16f, 0.55f) }, 0f, 360f, 14), Vector3.zero));
              list.Add(SaveMesh(Finish(v, t), "Ast_Debris_Pipe")); }
            { var v = new List<Vector3>(); var t = new List<int>();
              Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.38f, 0f), new Vector2(0.4f, 0.05f), new Vector2(0.36f, 0.09f), new Vector2(0f, 0.09f) }, 0f, 360f, 20));
              MBox(v, t, new Vector3(0.12f, 0.1f, 0f), new Vector3(0.26f, 0.02f, 0.03f), 25f, 0f);
              list.Add(SaveMesh(Finish(v, t), "Ast_Debris_Gauge")); }
            { var v = new List<Vector3>(); var t = new List<int>();
              MBox(v, t, new Vector3(0f, 0.25f, 0f), new Vector3(0.55f, 0.5f, 0.55f), 0f, 0.004f);
              foreach (var sy in new[] { 0.06f, 0.44f }) MBox(v, t, new Vector3(0f, sy, 0f), new Vector3(0.6f, 0.06f, 0.6f), 0f, 0f);
              list.Add(SaveMesh(Finish(v, t), "Ast_Debris_Crate")); }
            return list.ToArray();
        }

        /// <summary>The dressing that is not tied to a gameplay piece (cover outside the arena, walls, overhead).</summary>
        static void BuildFoundry(Transform hall, AsMats m, float H)
        {
            var root = new GameObject("Foundry").transform; root.SetParent(hall, false);
            var k = new AK();
            // north of the arena (outside the fight): two flywheel casings, the boiler drums, crate stacks, the cooling tower
            FlywheelCasing("Hall_FlywheelN", k, m, new Vector3(-332f, 0f, 134f), 0f, 3.2f, root);
            FlywheelCasing("Hall_FlywheelM", k, m, new Vector3(-372f, 0f, 137.2f), 0f, 2.6f, root);
            BoilerDrum(k, m, new Vector3(-360f, 0f, 137.6f), 0f, 5f, 1.2f);
            CrateStack(k, m, new Vector3(-351f, 0f, 137.6f), 8f, 3);
            CrateStack(k, m, new Vector3(-337f, 0f, 120.5f), -12f, 3);
            CoolingTower("Hall_CoolingTower", k, m, new Vector3(-324f, 0f, 128f), 13f, root);
            // the arena's edges: crate stacks in the two south corners (cover), a boiler drum along the east wall (outside the lanes)
            CrateStack(k, m, new Vector3(-395.6f, 0f, 74.6f), 20f, 3);
            CrateStack(k, m, new Vector3(-324.4f, 0f, 74.6f), -15f, 3);
            BoilerDrum(k, m, new Vector3(-322.4f, 0f, 118f), 90f, 5f, 1.0f);
            // wall flywheels high on the walls (above any jump), steam pipes with valves and gauges along the walls
            foreach (var (p, axis) in new[] { (new Vector3(-380f, 9f, 70.6f), Vector3.forward), (new Vector3(-345f, 9f, 70.6f), Vector3.forward), (new Vector3(-399.2f, 9f, 85f), Vector3.right), (new Vector3(-320.8f, 9f, 86f), Vector3.right), (new Vector3(-320.8f, 9f, 126f), Vector3.right) })
            {
                var w = Gear($"Hall_WallWheel{(int)-p.x}_{(int)p.z}", p, axis, 2.4f, 0.3f, 26, m.steel, root);
                var sp = w.AddComponent<Seven.AstSpin>(); sp.Axis = Vector3.up; sp.Speed = p.x < -360f ? 18f : -14f;
                var (hv, ht) = k.Of(m.metalDark); Append(hv, ht, axis == Vector3.forward ? RotX(Lathe(new[] { new Vector2(0f, -0.6f), new Vector2(0.5f, -0.6f), new Vector2(0.5f, 0.6f), new Vector2(0f, 0.6f) }, 0f, 360f, 12), p) : RotZ(Lathe(new[] { new Vector2(0f, -0.6f), new Vector2(0.5f, -0.6f), new Vector2(0.5f, 0.6f), new Vector2(0f, 0.6f) }, 0f, 360f, 12), p));
            }
            foreach (var (a, b) in new[] { (new Vector3(-398f, 5.5f, 71.2f), new Vector3(-322f, 5.5f, 71.2f)), (new Vector3(-398f, 4.6f, 71.4f), new Vector3(-322f, 4.6f, 71.4f)), (new Vector3(-399.6f, 5.5f, 72f), new Vector3(-399.6f, 5.5f, 100f)), (new Vector3(-320.4f, 5.5f, 72f), new Vector3(-320.4f, 5.5f, 99f)) })
            {
                Pipe(k, m.metal, new[] { a, b }, 0.22f);
                var d = (b - a); float len = d.magnitude;
                for (float s = 6f; s < len; s += 12f)
                {
                    var p = a + d.normalized * s; var (gv, gt) = k.Of(m.steel);
                    Append(gv, gt, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.06f, 0f), new Vector2(0.06f, 0.3f), new Vector2(0.24f, 0.3f), new Vector2(0.24f, 0.38f), new Vector2(0f, 0.38f) }, 0f, 360f, 12, null, p + Vector3.up * 0.2f));
                }
            }
            Overhead(k, m, root, H);
            k.Save("Foundry", root);
        }
    }
}
