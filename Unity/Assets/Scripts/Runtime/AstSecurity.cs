using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>AST_05: a security machine - a riveted dome body on a single drive wheel, two padded restraint arms, a lamp
    /// eye. It patrols its post; when it sees 7 it rolls after her to "escort" her (its arms close on her - nonlethal: she is
    /// walked back to the works' entry). Answers: dodge out of its reach, telekinesis (pushed away, its lamp flickers, a few
    /// seconds stunned), or the arc blades (they sever its drive's power - it sags, its lamp dies; it powers back up much
    /// later). Never destroyed.</summary>
    public class AstSecurity : MonoBehaviour
    {
        public Transform Body, ArmL, ArmR, Wheel; public Renderer Eye;
        public Vector3 Home; public float SeeRange = 10f, Speed = 2.5f;
        public ChapterSeven Seven;
        public System.Action<AstSecurity> Caught;
        public bool Paused;
        public enum St { Patrol, Chase, Stunned, Disabled, Escort }
        public St State { get; private set; } = St.Patrol;
        public int Escorts, Disables, Stuns;
        float _until, _t; Vector3 _vel;
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        void Start() { Home = transform.position; }
        public bool Active => State == St.Patrol || State == St.Chase;
        public void Disable(float secs, string how)
        {
            if (State == St.Disabled) return;
            State = St.Disabled; _until = Time.time + secs; Disables++;
            AudioKit.Play("ast_power_down", transform.position, 0.9f, 0.05f, AudioKit.Bus.Sfx, 30f);
            Debug.Log($"[Seven] security machine {name}: disabled by {how} for {secs:0} s (power severed - not destroyed)");
        }
        public void Stun(Vector3 push, float secs)
        {
            if (State == St.Disabled) return;
            State = St.Stunned; _until = Time.time + secs; Stuns++; _vel = push;
            AudioKit.Play("ast_servo_small", transform.position, 0.9f, 0.05f, AudioKit.Bus.Sfx, 25f);
        }
        void Update()
        {
            _t += Time.deltaTime;
            float eye = 1.2f, sag = 0f, arms = 0f;
            var her = Seven != null ? Seven.transform.position : transform.position + Vector3.forward * 99f;
            var to = Flat(her - transform.position); float d = to.magnitude;
            switch (State)
            {
                case St.Patrol:
                {
                    var p = Home + new Vector3(Mathf.Sin(_t * 0.35f), 0f, Mathf.Cos(_t * 0.27f)) * 2.2f;
                    Move(p, Speed * 0.4f);
                    eye = 0.9f + 0.3f * Mathf.Sin(_t * 3f);
                    if (!Paused && d < SeeRange && Seven != null && Seven.Current == ChapterSeven.Mode.Free) { State = St.Chase; AudioKit.Play("ast_servo_small", transform.position, 0.8f, 0.1f, AudioKit.Bus.Sfx, 25f); }
                    break;
                }
                case St.Chase:
                    eye = 2.2f; arms = 0.5f;
                    if (Paused || d > SeeRange * 1.6f || Flat(transform.position - Home).magnitude > 22f) { State = St.Patrol; break; }
                    Move(her, Speed);
                    if (d < 1.25f && Seven.Current == ChapterSeven.Mode.Free) { State = St.Escort; _until = Time.time + 1.2f; Escorts++; Caught?.Invoke(this); }
                    break;
                case St.Escort:
                    eye = 2.4f; arms = 1f;
                    if (Time.time > _until) { State = St.Patrol; transform.position = Home; }
                    break;
                case St.Stunned:
                    eye = Mathf.PerlinNoise(_t * 20f, 0f) > 0.5f ? 1.5f : 0.1f; sag = 0.3f;
                    transform.position += _vel * Time.deltaTime; _vel = Vector3.MoveTowards(_vel, Vector3.zero, Time.deltaTime * 8f);
                    if (Time.time > _until) State = St.Patrol;
                    break;
                case St.Disabled:
                    eye = 0f; sag = 1f;
                    if (Time.time > _until) { State = St.Patrol; AudioKit.Play("ast_servo_small", transform.position, 0.6f, 0.1f, AudioKit.Bus.Sfx, 20f); }
                    break;
            }
            if (Eye != null) Glowing.Set(Eye, eye);
            if (Body != null) Body.localRotation = Quaternion.Euler(sag * 24f, 0f, 0f);
            if (ArmL != null) ArmL.localRotation = Quaternion.Euler(-40f * arms + sag * 30f, 0f, 20f - 30f * arms);
            if (ArmR != null) ArmR.localRotation = Quaternion.Euler(-40f * arms + sag * 30f, 0f, -20f + 30f * arms);
        }
        void Move(Vector3 to, float speed)
        {
            var d = Flat(to - transform.position); if (d.magnitude < 0.2f) return;
            var step = d.normalized * Mathf.Min(d.magnitude, speed * Time.deltaTime);
            var np = transform.position + step; var zn = Ground.ZoneName(np.x, np.z);
            if (zn != null) { np.y = Ground.Height(np.x, np.z); transform.position = np; }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 5f);
            if (Wheel != null) Wheel.Rotate(Vector3.right, step.magnitude / 0.35f * Mathf.Rad2Deg, Space.Self);
        }
    }
}
