using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>The archive panels on their conveyor: by default toward the cutting mechanism (AST_04 - one goes in); once
    /// the diverter is set (AST_05) the intact panels turn onto the storage loop instead (the player sees them reach safety).</summary>
    public class AstArchiveLine : MonoBehaviour
    {
        public Transform[] Panels; public Vector3[] ToCutter, Loop; public float Speed = 1.4f;
        public bool Diverted, Stopped;
        public Transform Saw; public Light SawLight;
        readonly Dictionary<Transform, float> _s = new Dictionary<Transform, float>();
        void Start() { for (int i = 0; i < Panels.Length; i++) if (Panels[i] != null) _s[Panels[i]] = i * 5f; }
        static Vector3 Along(Vector3[] p, float s, bool loop, out Vector3 dir)
        {
            dir = Vector3.right;
            float total = 0f; int n = loop ? p.Length : p.Length - 1;
            for (int i = 0; i < n; i++) total += Vector3.Distance(p[i], p[(i + 1) % p.Length]);
            if (loop) s = ((s % total) + total) % total; else s = Mathf.Clamp(s, 0f, total);
            for (int i = 0; i < n; i++)
            {
                var a = p[i]; var b = p[(i + 1) % p.Length]; float l = Vector3.Distance(a, b);
                if (s <= l) { dir = (b - a).normalized; return Vector3.Lerp(a, b, s / Mathf.Max(0.01f, l)); }
                s -= l;
            }
            return p[n % p.Length];
        }
        void Update()
        {
            if (Saw != null) Saw.Rotate(Vector3.up, (Diverted || Stopped ? 40f : 520f) * AstProduction.Rate * Time.deltaTime, Space.Self);
            if (SawLight != null) SawLight.intensity = Diverted || Stopped ? 0.2f : 1.2f + 0.6f * Mathf.PerlinNoise(Time.time * 9f, 0f);
            if (Stopped) return;
            foreach (var p in Panels)
            {
                if (p == null || !p.gameObject.activeSelf) continue;
                float s = _s[p] + Speed * AstProduction.Rate * Time.deltaTime; _s[p] = s;
                Vector3 d; Vector3 at;
                if (Diverted) at = Along(Loop, s, true, out d);
                else { at = Along(ToCutter, s % (Length(ToCutter) + 0.01f), false, out d); }
                p.position = at; if (d.sqrMagnitude > 0.01f) p.rotation = Quaternion.LookRotation(d);
            }
        }
        static float Length(Vector3[] p) { float t = 0f; for (int i = 0; i + 1 < p.Length; i++) t += Vector3.Distance(p[i], p[i + 1]); return t; }
    }
}
