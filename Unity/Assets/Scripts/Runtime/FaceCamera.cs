using UnityEngine;

namespace Seven
{
    /// <summary>A quad that always faces the camera (the passage shapes' soft halos).</summary>
    public class FaceCamera : MonoBehaviour
    {
        void LateUpdate() { var c = Camera.main; if (c != null) transform.rotation = Quaternion.LookRotation(transform.position - c.transform.position); }
    }
}
