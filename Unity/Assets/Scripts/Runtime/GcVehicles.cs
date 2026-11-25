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
        public static Transform Deep(Transform t, string name) { foreach (var c in t.GetComponentsInChildren<Transform>(true)) if (c.name == name) return c; return null; }
    }
}
