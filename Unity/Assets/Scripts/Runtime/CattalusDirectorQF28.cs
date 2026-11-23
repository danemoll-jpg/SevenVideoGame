using System.Collections;
using UnityEngine;
using L = Seven.CattalusLayout;

namespace Seven
{
    /// <summary>QF28 (5): -qf28holds - every hand pose that holds a rune / object, posed by the game's own hold code (HoldPose /
    /// CarriedShape + HandHold), close-ups from the player's side and from the side, the HOLD lines; then the run ends.</summary>
    public partial class CattalusDirector
    {
        IEnumerator HoldCheck()
        {
            yield return UI.Fade(0f, 0.3f);
            yield return Cutscene(true);
            if (Cat != null) Cat.gameObject.SetActive(false);
            var at = L.QuietCorner + new Vector3(-0.2f, 0f, -0.9f);
            Seven.Place(at, Vector3.forward); Seven.Lock(true); Seven.PlayAnim("idle", 0.1f);
            var f = Vector3.forward; var r = Vector3.right;
            var mat = Sphere != null ? Sphere.GetComponentInChildren<Renderer>()?.sharedMaterial : null;
            Transform Make(string n, Mesh m)
            {
                var go = new GameObject("QF28_" + n); go.AddComponent<MeshFilter>().sharedMesh = m; var mr = go.AddComponent<MeshRenderer>(); if (mat != null) mr.sharedMaterial = mat;
                return go.transform;
            }
            var hold = HoldPose.On(Seven.gameObject);
            // two-handed holds: the sphere (CAT_09), the cube and the octahedron (the solver on the bigger runes)
            foreach (var (name, mesh, rad, reach) in new (string, Mesh, float, float)[] { ("sphere", CattalusSphere.Mesh(RuneSizes.SphereR), RuneSizes.SphereR, 0.34f), ("cube", RecordSet.Cube(0.42f * 0.866f), 0f, 0.42f), ("octahedron", Octahedron.Mesh(RuneSizes.OctaR), 0f, 0.42f) })
            {
                var t = Make(name, mesh); t.position = P7 + Vector3.up * 1.18f + f * reach; t.rotation = Quaternion.Euler(0f, 20f, 0f);
                hold.Hold(t, true, true, $"the {name} (both hands)", rad);
                yield return Wait(1.2f);
                yield return HoldShots(name + "_both_hands", t, f, r, hold);
                if (name == "sphere")
                {
                    t.position += -r * 0.04f - Vector3.up * 0.02f; hold.Hold(t, true, false, "the sphere (left palm)", rad);
                    yield return Wait(1.0f);
                    yield return HoldShots("sphere_left_palm", t, f, r, hold);
                }
                hold.Release("QF28 hold check: next"); yield return Wait(0.6f);
                Destroy(t.gameObject);
            }
            // the one-handed hold: the dodecahedron in her right hand, then her left (CarriedShape - the Moon / the galley)
            var d = Make("dodecahedron", ElarimShape.BodyMesh(MoonLayout.ShapeRadius));
            var cs = CarriedShape.Attach(Seven.gameObject, d);
            foreach (var w in new[] { CarriedShape.Where.Hand, CarriedShape.Where.HandLeft })
            {
                cs.Set(w, 0f); yield return Wait(1.0f);
                var an = Seven.GetComponentInChildren<Animator>(); var surf = new HandHold.Surface(d, d.GetComponent<MeshFilter>().sharedMesh);
                var rep = HandHold.Measure(an, w == CarriedShape.Where.Hand, surf);
                Log($"HOLD (QF28 5) the dodecahedron, {(w == CarriedShape.Where.Hand ? "right" : "left")} hand: deepest finger point {rep.Deepest * 1000f:0.0} mm ({rep.Where}), fingertips touching {rep.Touching} of 5, wrist bend {rep.Wrist:0} deg - {(rep.Deepest >= HandHold.InsideMax && rep.Wrist <= HandHold.WristMax ? "ok" : "FAIL")}");
                var hand = an.GetBoneTransform(w == CarriedShape.Where.Hand ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand).position;
                var side = w == CarriedShape.Where.Hand ? r : -r;
                Shoot(hand + side * 0.55f + f * 0.45f + Vector3.up * 0.15f, d.position, 30f, 0.01f); yield return Wait(0.3f);
                ImageGate.Expect(d, "the dodecahedron in her hand", 0f, "hold_dodecahedron"); yield return Shot($"hold_dodecahedron_{(w == CarriedShape.Where.Hand ? "right" : "left")}_hand_close");
            }
            cs.Set(CarriedShape.Where.Away, 0f); d.gameObject.SetActive(false);
            yield return Cutscene(false);
            Cap.Finish("QF28 HOLD CHECK done: " + HandHold.Summary());
        }
        IEnumerator HoldShots(string name, Transform t, Vector3 f, Vector3 r, HoldPose hold)
        {
            var an = Seven.GetComponentInChildren<Animator>();
            var mf = t.GetComponent<MeshFilter>();
            var surf = hold != null && name.StartsWith("sphere") ? new HandHold.Surface(t, RuneSizes.SphereR) : new HandHold.Surface(t, mf.sharedMesh);
            foreach (var right in new[] { false, true })
            {
                if (right ? !hold.Right : !hold.Left) continue;
                var rep = HandHold.Measure(an, right, surf);
                Log($"HOLD (QF28 5) {name}, {(right ? "right" : "left")} hand: deepest finger point {rep.Deepest * 1000f:0.0} mm ({rep.Where}), fingertips touching {rep.Touching} of 5, wrist bend {rep.Wrist:0} deg - {(rep.Deepest >= HandHold.InsideMax && rep.Wrist <= HandHold.WristMax ? "ok" : "FAIL")}");
            }
            var c = t.position;
            // the player's view: in front of her, a little above, close
            Shoot(c + f * 0.8f + Vector3.up * 0.3f - r * 0.15f, c, 34f, 0.01f); yield return Wait(0.3f);
            ImageGate.Expect(t, $"the {name} in her hands", 0f, "hold_" + name); yield return Shot($"hold_{name}_close_front");
            // from her side (the fingers round it)
            Shoot(c + r * 0.75f + f * 0.2f + Vector3.up * 0.1f, c, 32f, 0.01f); yield return Wait(0.3f);
            ImageGate.Expect(t, $"the {name} in her hands", 0f, "hold_" + name); yield return Shot($"hold_{name}_close_side");
        }

        /// <summary>QF28 (4): Duchess in her racer - captured at the grid, mid-race (side + chase) and at the finish, each a second
        /// camera on her (the player's race camera is never moved).</summary>
        void DuchessShots(string m)
        {
            if (!Shots || Cap == null || DuchessRacer == null || Duchess == null) return;
            if (m != "duchess_grid" && m != "section:2" && m != "duchess_finish" && m != "duchess_out") return;
            var r = DuchessRacer; var p = r.position; var f = r.forward; var rt = r.right;
            void Two(string n, Vector3 pos, Vector3 look, float fov) { ImageGate.Expect(Duchess.transform, "Duchess in her racer", 0f, n); StartCoroutine(Cap.ShotFrom(n, pos, look, fov)); }
            switch (m)
            {
                case "duchess_grid": Two("c21d_duchess_in_her_racer_at_the_grid", p + f * 3.2f + rt * 2.4f + Vector3.up * 1.3f, p + Vector3.up * 0.5f, 40f); break;
                case "section:2": StartCoroutine(MidRace()); break;
                case "duchess_finish": Two("c26d_duchess_at_the_finish_in_her_racer", p + f * 3.6f - rt * 2.2f + Vector3.up * 1.4f, p + Vector3.up * 0.5f, 40f); break;
                case "duchess_out": { var d = Duchess.transform.position; var away = Flat(d - p).normalized; if (away.sqrMagnitude < 0.01f) away = rt;
                    Two("c26e_duchess_climbs_out_of_her_racer", d + away * 2.6f + f * 1.6f + Vector3.up * 1.0f, (d + p) * 0.5f + Vector3.up * 0.35f, 44f); break; }
            }
            Log($"DUCHESS RIDES (QF28 4): {m} - in her racer {(Race.DuchessSeated ? "yes" : "no")}, racer nose along its travel {Vector3.Angle(r.forward, CattalusCourse.AtDist(Race.DuchessDist + 4f) - CattalusCourse.AtDist(Race.DuchessDist)):0} deg");
        }
        IEnumerator MidRace()
        {
            yield return null;
            var r = DuchessRacer; var p = r.position; var f = r.forward; var rt = r.right;
            // from the side of the road she is NOT on (over the road, never from beyond its rail), and a high chase behind her
            var mid = CattalusCourse.AtDist(Race.DuchessDist); var toMid = Flat(mid - p); var sd = Vector3.Dot(toMid, rt) >= 0f ? rt : -rt;
            ImageGate.Expect(Duchess.transform, "Duchess racing", 0f, "c22d_"); yield return Cap.ShotFrom("c22d_duchess_mid_race_side", p + sd * 3.6f + f * 0.9f + Vector3.up * 1.0f, p + Vector3.up * 0.4f, 44f);
            yield return null; p = r.position; f = r.forward;
            ImageGate.Expect(Duchess.transform, "Duchess racing", 0f, "c22e_"); yield return Cap.ShotFrom("c22e_duchess_mid_race_chase", p - f * 6.5f + Vector3.up * 2.8f, p + f * 12f + Vector3.up * 1.5f, 50f);
        }
    }
}
