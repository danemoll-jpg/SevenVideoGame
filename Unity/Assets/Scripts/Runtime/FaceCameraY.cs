using UnityEngine;

namespace Seven
{
    /// <summary>QF18 (2): a vertical beam that turns round its own axis to face the camera.</summary>
    public class FaceCameraY : MonoBehaviour
    {
        void LateUpdate()
        {
            var c = Camera.main; if (c == null || transform.parent == null) return;
            var d = c.transform.position - transform.position; d = Vector3.ProjectOnPlane(d, transform.parent.up);
            if (d.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(d, transform.parent.up) * Quaternion.Euler(-90f, 0f, 0f);
        }
    }
}
