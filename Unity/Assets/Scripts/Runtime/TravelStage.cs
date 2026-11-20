using UnityEngine;

namespace Seven
{
    /// <summary>QF16 (2): the passage's parts in the Saucer scene (built by FightDemoBuilderOutside): the exterior saucer far
    /// above the deck (the one prefab, unchanged), the shapes' glow material and halo, a cube mesh.</summary>
    public class TravelStage : MonoBehaviour
    {
        public Transform Saucer; public Material Glow, Halo; public Mesh CubeMesh;
        public static TravelStage I { get; private set; }
        void Awake() { I = this; if (Saucer != null && Saucer.parent != null) Saucer.parent.gameObject.SetActive(false); }
    }
}
