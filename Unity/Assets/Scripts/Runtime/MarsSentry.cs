using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF18 (17): one of Viper's sentries - an angular dark-metal drone (~1 m) with a hard white eye and a searchlight that
    /// sweeps, a buzzing hum. It flies in a clear line to the worker's control and attacks it (sparks); telekinesis knocks it
    /// aside: it tumbles, smokes and goes dark (a machine - nobody is hurt).
    /// </summary>
    public class MarsSentry : MonoBehaviour
    {
        public Renderer Eye; public Light Search; public Transform Body;
        public bool Out { get; private set; }
        public bool Attacking { get; private set; }
        public float Wear { get; private set; }                 // how long it has been hitting the control (s)
        public Vector3 Target;
        AudioSource _hum; float _t;
        public void Launch(Vector3 from, Vector3 target)
        {
            gameObject.SetActive(true); Out = false; Attacking = false; Wear = 0f;
            transform.position = from; Target = target;
            if (Search != null) Search.enabled = true;
            SetEye(2.4f);
            _hum = AudioKit.Loop("mars_barrier_hum", transform, null, 0.35f, AudioKit.Bus.Sfx, 3f, 40f);
        }
        void SetEye(float k) { if (Eye == null) return; var m = Eye.material; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white * k); }
        void Update()
        {
            if (Out) return;
            _t += Time.deltaTime;
            var to = Target - transform.position; float d = to.magnitude;
            var look = to; look.y = 0f;
            if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), 160f * Time.deltaTime);
            if (Body != null) Body.localRotation = Quaternion.Euler(Mathf.Sin(_t * 2.1f) * 6f, 0f, Mathf.Sin(_t * 1.7f) * 8f);
            // the searchlight sweeps ahead while it flies, then fixes on the control
            if (Search != null) Search.transform.localRotation = Quaternion.Euler(Attacking ? 32f : 24f + Mathf.Sin(_t * 2.6f) * 10f, Attacking ? 0f : Mathf.Sin(_t * 1.9f) * 28f, 0f);
            if (d > 2.0f) { transform.position += to.normalized * Mathf.Min(d - 1.9f, 3.0f * Time.deltaTime) + Vector3.up * Mathf.Sin(_t * 3f) * 0.004f; Attacking = false; }
            else
            {
                if (!Attacking) { Attacking = true; AudioKit.Play("mars_control_on", Target, 0.5f, 0.1f, AudioKit.Bus.Sfx, 30f); }
                Wear += Time.deltaTime;
                // it rams / cuts at the control: a jab toward it and a spark (the eye flares) every ~0.5 s
                float ph = (_t * 2f) % 1f;
                transform.position = Vector3.Lerp(transform.position, Target - to.normalized * (1.6f - Mathf.Sin(ph * Mathf.PI) * 0.7f), Time.deltaTime * 8f);
                SetEye(ph < 0.15f ? 5f : 2.4f);
                if (ph < Time.deltaTime * 2f) AudioKit.Play("fight_block", Target, 0.45f, 0.15f, AudioKit.Bus.Sfx, 25f);
            }
        }
        /// <summary>Telekinesis: knocked aside - it tumbles away, smokes, goes dark on the ground.</summary>
        public IEnumerator KnockAside(Vector3 from, System.Func<float, float, float> groundAt)
        {
            Out = true; Attacking = false;
            if (_hum != null) StartCoroutine(AudioKit.Fade(_hum, 0f, 0.3f, true));
            AudioKit.Play("tk_push", transform.position, 1f, 0.05f);
            AudioKit.Play("mars_barrier_off", transform.position, 0.8f, 0.05f, AudioKit.Bus.Sfx, 30f);
            var p0 = transform.position; var away = transform.position - from; away.y = 0f; if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
            var land = p0 + away.normalized * 7f; land.y = groundAt(land.x, land.z) + 0.45f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.0f)
            {
                transform.position = Vector3.Lerp(p0, land, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 2.6f;
                transform.Rotate(420f * Time.deltaTime, 90f * Time.deltaTime, 300f * Time.deltaTime);
                SetEye(Mathf.Lerp(2.4f, 0f, t) * (Random.value < 0.3f ? 1.8f : 1f));
                if (Search != null) Search.intensity = Mathf.Lerp(Search.intensity, 0f, t);
                yield return null;
            }
            transform.position = land; transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 160f);
            SetEye(0f); if (Search != null) Search.enabled = false;
            AudioKit.Play("stone_fall", land, 0.8f, 0.05f, AudioKit.Bus.Sfx, 30f);
            // smoke: a few slow soft puffs rising (additive grey, monochrome)
            var puff = Markers.Additive(0.25f);
            for (int k = 0; k < 5; k++)
            {
                var g = new GameObject("SentrySmoke"); g.AddComponent<MeshFilter>().sharedMesh = Markers.Rock(300 + k, Vector3.one * 0.18f);
                var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = puff; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                g.transform.position = land + new Vector3(Random.Range(-0.2f, 0.2f), 0.2f, Random.Range(-0.2f, 0.2f));
                g.AddComponent<SmokePuff>().Life = 2.2f + k * 0.3f;
                for (float t = 0f; t < 0.25f; t += Time.deltaTime) yield return null;
            }
        }
    }
}
