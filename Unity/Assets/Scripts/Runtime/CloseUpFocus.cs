using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Seven
{
    /// <summary>
    /// Round 10 galley camera: a shallow depth of field for the over-the-shoulder shots and the eye close-ups
    /// (a high-priority global Volume with a Bokeh DepthOfField; weight 0 = off). Monochrome by construction.
    /// </summary>
    public class CloseUpFocus : MonoBehaviour
    {
        Volume _v;
        DepthOfField _dof;
        float _target;

        void Awake()
        {
            // The builder puts a Volume with a DepthOfField profile asset here, so the DOF shaders ship with the build
            // (URP strips post-processing shaders no profile in the build uses).
            _v = GetComponent<Volume>();
            if (_v != null && _v.sharedProfile != null && _v.profile.TryGet(out _dof)) { _v.weight = 0f; return; }
            if (_v == null) _v = gameObject.AddComponent<Volume>();
            _v.isGlobal = true; _v.priority = 100f; _v.weight = 0f;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            _dof = prof.Add<DepthOfField>(true);
            _dof.mode.Override(DepthOfFieldMode.Bokeh);
            _dof.focusDistance.Override(1f);
            _dof.focalLength.Override(70f);
            _dof.aperture.Override(2.2f);
            _v.sharedProfile = prof;
        }

        /// <summary>Focus at a distance (m) with a given lens; off = sharp everywhere again.</summary>
        public void Set(bool on, float focus = 1f, float focalLength = 70f, float aperture = 2.2f)
        {
            _target = on ? 1f : 0f;
            if (on) { _dof.focusDistance.Override(focus); _dof.focalLength.Override(focalLength); _dof.aperture.Override(aperture); }
            if (!on) _v.weight = 0f;
        }
        public void Focus(float focus) => _dof.focusDistance.Override(focus);
        public bool On => _target > 0.5f;

        void Update() { if (_target > 0f) _v.weight = Mathf.MoveTowards(_v.weight, _target, Time.deltaTime * 6f); }
    }
}
