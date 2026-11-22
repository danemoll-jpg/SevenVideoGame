using System.Collections.Generic;
using UnityEngine;

namespace Seven.EditorTools
{
    public static partial class FightDemoBuilder
    {
        /// <summary>QF23 (18): throwables that belong on the mothership - smooth white maintenance canisters, cracked hull panels
        /// and fittings Ratwing tore loose forcing the archive entrance, a small planter, a light-globe housing, a segment of a
        /// damaged partition. Organic, pearly, Biavian (one pale material).</summary>
        static Mesh[] BiavianDebrisMeshes()
        {
            var list = new List<Mesh>();
            { var v = new List<Vector3>(); var t = new List<int>();     // a maintenance canister (a smooth capsule pod)
              Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.14f, 0.02f), new Vector2(0.2f, 0.1f), new Vector2(0.21f, 0.45f), new Vector2(0.15f, 0.56f), new Vector2(0.06f, 0.6f), new Vector2(0f, 0.6f) }, 0f, 360f, 18));
              list.Add(SaveMesh(SmoothFinish(v, t), "Bia_Debris_Canister")); }
            { var v = new List<Vector3>(); var t = new List<int>();     // a cracked hull panel torn from the entrance (two shards)
              MBox(v, t, new Vector3(-0.12f, 0.03f, 0f), new Vector3(0.46f, 0.05f, 0.62f), 8f, 0.02f); MBox(v, t, new Vector3(0.3f, 0.03f, 0.08f), new Vector3(0.28f, 0.05f, 0.4f), -21f, 0.02f);
              list.Add(SaveMesh(SmoothFinish(v, t), "Bia_Debris_Panel")); }
            { var v = new List<Vector3>(); var t = new List<int>();     // a small planter (a pot, a rounded bush)
              Append(v, t, Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.18f, 0f), new Vector2(0.24f, 0.26f), new Vector2(0.21f, 0.28f), new Vector2(0f, 0.28f) }, 0f, 360f, 16));
              Ellipsoid(v, t, new Vector3(0f, 0.4f, 0f), new Vector3(0.2f, 0.16f, 0.2f), 5, 9);
              list.Add(SaveMesh(SmoothFinish(v, t), "Bia_Debris_Planter")); }
            { var v = new List<Vector3>(); var t = new List<int>();     // a light-globe housing (a cage of three rings round a globe)
              Ellipsoid(v, t, new Vector3(0f, 0.24f, 0f), Vector3.one * 0.15f, 6, 10);
              for (int k = 0; k < 3; k++) { var pts = new List<Vector3>(); for (int j = 0; j <= 16; j++) { float a = j / 16f * Mathf.PI * 2f; pts.Add(Quaternion.Euler(0f, k * 60f, 0f) * new Vector3(Mathf.Cos(a) * 0.22f, 0.24f + Mathf.Sin(a) * 0.22f, 0f)); } Sweep(v, t, pts, 0.018f, false, 5); }
              list.Add(SaveMesh(SmoothFinish(v, t), "Bia_Debris_GlobeHousing")); }
            { var v = new List<Vector3>(); var t = new List<int>();     // a segment of a damaged partition (a curved slab)
              Append(v, t, Lathe(new[] { new Vector2(0.55f, 0f), new Vector2(0.62f, 0f), new Vector2(0.62f, 0.12f), new Vector2(0.55f, 0.12f), new Vector2(0.55f, 0f) }, 0f, 70f, 10));
              list.Add(SaveMesh(SmoothFinish(v, t), "Bia_Debris_PartitionSegment")); }
            Log("QF23 (18): the mothership's throwables - canister, cracked panel, planter, light-globe housing, partition segment (Biavian, pale)");
            return list.ToArray();
        }
    }
}
