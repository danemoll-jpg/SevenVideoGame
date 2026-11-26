using UnityEngine;

namespace Seven
{
    /// <summary>Round 20: a racer's vehicle + rider from the scene's templates (built once by the builder: forward +z, the underside
    /// at y 0, about 3-4 m long). 7 sits her bike in her real ride pose (<see cref="GcSevenRider"/>: hips on the saddle, palms in
    /// the grips, feet on the panels - the story game's contact points); Duchess crouches in her racer's padded bed (her ride pose
    /// from the story race); Tunneler stands on his drill rig as in the finale. A soft white hover glow under each.</summary>
    public static class GcVehicles
    {
        public static Transform Make(GcGame g, int racer, Transform parent, bool ghost = false)
        {
            var root = new GameObject("Vehicle").transform; root.SetParent(parent, false);
            GameObject Clone(GameObject tpl, string name) { if (tpl == null) return null; var c = Object.Instantiate(tpl, root); c.name = name; c.SetActive(true); c.transform.localPosition = Vector3.zero; c.transform.localRotation = Quaternion.identity; return c; }
            switch (racer)
            {
                case 0:
                {
                    var bike = Clone(g.BikeTpl, "Bike");
                    var seven = Clone(g.SevenTpl, "Seven");
                    if (bike != null && seven != null)
                    {
                        var an = seven.GetComponentInChildren<Animator>();
                        if (an != null)
                        {
                            var rd = an.gameObject.AddComponent<GcSevenRider>();
                            rd.Seat = bike.transform.Find("Seat") ?? Deep(bike.transform, "Seat"); rd.GripL = Deep(bike.transform, "GripL"); rd.GripR = Deep(bike.transform, "GripR"); rd.FootL = Deep(bike.transform, "FootL"); rd.FootR = Deep(bike.transform, "FootR");
                            rd.Root = seven.transform;
                        }
                    }
                    break;
                }
                case 1:
                {
                    var car = Clone(g.RacerTpl, "Racer8");
                    var d = Clone(g.DuchessTpl, "Duchess");
                    if (car != null && d != null)
                    {
                        var seat = Deep(car.transform, "DuchessSeat");
                        if (seat != null) { d.transform.SetParent(seat, false); d.transform.localPosition = Vector3.zero; d.transform.localRotation = Quaternion.identity; }
                        d.AddComponent<GcCatRider>();
                    }
                    break;
                }
                case 3: case 4: case 5:
                {
                    // Round 22 / QF37: Nela, Viper, a Scree astride the Scrambler jet - the bike sized to its rider (7's bike reach x the
                    // rider's height against hers), the rider in the same riding pose as 7 (GcSevenRider: hips on the saddle, palms on
                    // the grips, feet on the wing roots)
                    var tpl = racer == 3 ? g.NelaTpl : racer == 4 ? g.ViperTpl : g.ScreeTpl;   // QF37: slot 5 = a Scree
                    float h = racer < g.RiderHeight.Length && g.RiderHeight[racer] > 0.5f ? g.RiderHeight[racer] : 1.75f, k = h / SevenHeight;
                    var bike = Clone(g.ScramblerTpl, "Scrambler"); if (bike != null) bike.transform.localScale = Vector3.one * ScramblerFit * k;
                    var rider = Clone(tpl, GcStats.Of(racer).Racer);
                    if (bike != null && rider != null)
                    {
                        var an = rider.GetComponentInChildren<Animator>();
                        if (an != null)
                        {
                            var rd = an.gameObject.AddComponent<GcSevenRider>(); rd.K = k; rd.Who = GcStats.Of(racer).Racer; rd.KneeWidth = 0.24f; rd.Tuck = 18f; rd.Pose = "sitting_idle";   // upright on the saddle, leaning in a little (7's racing tuck curled the Machinist's long spine out of sight)
                            rd.Seat = Deep(bike.transform, "Seat"); rd.GripL = Deep(bike.transform, "GripL"); rd.GripR = Deep(bike.transform, "GripR"); rd.FootL = Deep(bike.transform, "FootL"); rd.FootR = Deep(bike.transform, "FootR");
                            rd.Root = rider.transform;
                            var sr = rider.GetComponentInChildren<ScreeRig>(); if (sr != null) sr.Brace = 1f;   // the Scree's lower arms forward along the tank
                        }
                    }
                    break;
                }
                case 6:
                {
                    // QF37: Ratwing in the Jetstream's open cockpit (was Juniper) - seated, wings folded along his sides, claws on the controls
                    var jet = Clone(g.JetTpl, "Jetstream");
                    var rw = Clone(g.RatwingTpl, "Ratwing");
                    if (jet != null && rw != null)
                    {
                        var rd = rw.GetComponent<GcRatRider>();
                        if (rd != null) { rd.Seat = Deep(jet.transform, "CatSeat"); rd.Jet = jet.transform; }
                    }
                    break;
                }
                default:
                {
                    var rig = Clone(g.RigTpl, "DrillRig");
                    // his rig's floor dust and exhaust are for hovering over rock in the finale - at racing speed they would trail a white
                    // cloud across the track: off here (the hover glow below stays)
                    var tr = rig != null ? rig.GetComponentInChildren<TunnelerRig>() : null; if (tr != null) tr.ThrustersOn = false;
                    break;
                }
            }
            if (!ghost)
            {
                // a soft round pool of white light on the road under it (additive, a soft dot - never a slab)
                var glow = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(glow.GetComponent<Collider>()); glow.name = "HoverGlow"; glow.transform.SetParent(root, false);
                glow.transform.localPosition = new Vector3(0f, -0.4f, 0f); glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); glow.transform.localScale = new Vector3(2.0f, 3.2f, 1f);
                glow.GetComponent<Renderer>().sharedMaterial = g.HoverMat; glow.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return root;
        }
        /// <summary>Round 22: 7's standing height (the riders' sizes are taken against it) and the Scrambler jet's scale for a 7-sized rider
        /// (the model's own size: a 7-sized rider reaches its grips 0.66 m from the hips sitting a little taller than on her bike;
        /// 1.2x, 7's bike's reach, made every rider look small, flat on an oversized tank).</summary>
        public const float SevenHeight = 1.75f, ScramblerFit = 1.0f;
        public static Transform Deep(Transform t, string name) { foreach (var c in t.GetComponentsInChildren<Transform>(true)) if (c.name == name) return c; return null; }
    }
}
