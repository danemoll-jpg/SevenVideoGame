using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Ratwing check run (-ratwing -capture dir): lets the sequence play, renders the director's camera off-screen into
    /// a RenderTexture at fixed sequence times (asleep, eyes open, wings spread, mid-drop, landing, crouch, stance) plus a
    /// few parked close-ups, measures the feet against the ledge while hanging and against the floor after landing
    /// (baked skinned mesh + raycasts to the ledge / floor colliders), the eye-glint timing, the wingspan, replay and
    /// skip, the audio level, then quits. Never needs the window or focus.
    /// </summary>
    public class RatwingCaptureRun : MonoBehaviour
    {
        RatwingDirector _d;
        string _dir;
        RenderTexture _rt;
        readonly StringBuilder _log = new StringBuilder();
        float _t0;
        SkinnedMeshRenderer _body;
        int[] _footVerts;
        Transform _eyes;
        Vector3 Origin => _d.Model.parent.position;

        public void Init(RatwingDirector d)
        {
            _d = d;
            _dir = DemoBoot.CaptureDir;
            Directory.CreateDirectory(_dir);
            _rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            _d.Cam.targetTexture = _rt;
            Physics.queriesHitBackfaces = true;
            _t0 = Time.realtimeSinceStartup;
            _body = _d.Model.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(s => s.sharedMesh.vertexCount).First();
            var bw = _body.sharedMesh.boneWeights;
            var names = _body.bones.Select(b => b != null ? b.name : "").ToArray();
            _footVerts = Enumerable.Range(0, bw.Length).Where(i => names[bw[i].boneIndex0].StartsWith("foot") || names[bw[i].boneIndex0].StartsWith("toes")).ToArray();
            _eyes = _d.Model.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "eyes");
            Log($"ratwing capture start, muted={DemoBoot.Muted}, listenerVolume={AudioListener.volume}; body {_body.sharedMesh.vertexCount} verts, " +
                $"{_body.sharedMesh.GetIndexCount(0) / 3} tris, foot verts {_footVerts.Length}, bones {_body.bones.Length}");
            StartCoroutine(Run());
            StartCoroutine(Watchdog());
        }

        void Log(string s)
        {
            var line = $"[{Time.realtimeSinceStartup - _t0,6:0.00}s] {s}";
            _log.AppendLine(line);
            Debug.Log("[Seven] " + line);
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            var tex = new Texture2D(_rt.width, _rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(_dir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
            Log($"shot {name}: T={_d.T:0.00}s state={(_d.Anim.GetCurrentAnimatorStateInfo(0).IsName("sequence") ? "sequence" : "idle")}");
        }

        IEnumerator At(float t)
        {
            float end = Time.time + 30f;
            while (_d.T < t && Time.time < end) yield return null;
            if (_d.T < t) Log($"TIMEOUT waiting for T={t}");
        }

        /// <summary>One parked close-up, then back to the scripted camera.</summary>
        IEnumerator Close(string name, Vector3 pos, Vector3 look)
        {
            _d.CamOverride = new Pose(pos, Quaternion.LookRotation(look - pos));
            yield return null;
            yield return Shot(name);
            _d.CamOverride = null;
        }

        Vector3[] Baked()
        {
            var m = new Mesh();
            _body.BakeMesh(m, true);
            var tr = _body.transform;
            var v = m.vertices;
            for (int i = 0; i < v.Length; i++) v[i] = tr.TransformPoint(v[i]);
            Destroy(m);
            return v;
        }

        void LogAudio()
        {
            var buf = new float[512];
            double sum = 0; int n = 0;
            for (int ch = 0; ch < 2; ch++)
            {
                AudioListener.GetOutputData(buf, ch);
                foreach (var s in buf) { sum += s * s; n++; }
            }
            Log($"audio: listenerVolume={AudioListener.volume} paused={AudioListener.pause} outputRMS={Math.Sqrt(sum / n):0.000000}");
        }

        readonly List<float> _hangGap = new List<float>(), _hangRay = new List<float>(), _landGap = new List<float>(), _landRay = new List<float>();
        float _eyesOpenAt = -1f, _lowestHang = float.MaxValue, _maxSpan;

        /// <summary>Samples contact every few frames while the sequence plays.</summary>
        IEnumerator Sampler()
        {
            while (true)
            {
                float t = _d.T;
                if (_eyes != null && _eyesOpenAt < 0f && _eyes.lossyScale.x > 0.5f) _eyesOpenAt = t;
                bool hang = t > 0.2f && t < 6.4f, landed = t > 7.8f && t <= 10.5f;
                if (hang || landed || (t > 5f && t < 6.4f))
                {
                    var v = Baked();
                    float ceil = Origin.y + _d.CeilingY;
                    if (hang)
                    {
                        var top = _footVerts.Select(i => v[i]).OrderByDescending(p => p.y).First();
                        _hangGap.Add(ceil - top.y);
                        // the real ledge surface above that foot point
                        if (Physics.Raycast(top - Vector3.up * 0.3f, Vector3.up, out var hit, 2f)) _hangRay.Add(hit.point.y - top.y);
                        _lowestHang = Mathf.Min(_lowestHang, v.Min(p => p.y) - Origin.y);
                    }
                    if (landed)
                    {
                        var low = _footVerts.Select(i => v[i]).OrderBy(p => p.y).First();
                        _landGap.Add(low.y - Origin.y);
                        if (Physics.Raycast(low + Vector3.up * 0.5f, Vector3.down, out var hit, 2f)) _landRay.Add(low.y - hit.point.y);
                    }
                    if (t > 5f && t < 6.4f) _maxSpan = Mathf.Max(_maxSpan, v.Max(p => p.x) - v.Min(p => p.x));
                }
                for (int i = 0; i < 6; i++) yield return null;
            }
        }

        IEnumerator Run()
        {
            yield return null;
            LogAudio();
            var sampler = StartCoroutine(Sampler());
            var o = Origin;
            yield return At(0.6f); yield return Shot("01_cave_wide");
            yield return At(2.0f); yield return Shot("02_asleep");
            yield return Close("02b_asleep_close", o + new Vector3(0.6f, 2.4f, -3.2f), o + new Vector3(0f, 3.1f, 0f));
            yield return At(4.05f); yield return Shot("03_eyes_open");
            if (_d.Head != null) yield return Close("03b_eyes_close", _d.Head.position + new Vector3(0.18f, -0.05f, -0.95f), _d.Head.position + new Vector3(0f, -0.1f, 0f));
            yield return At(5.8f); yield return Shot("04_wings_spread");
            yield return Close("04b_wings_spread_close", o + new Vector3(0f, 2.3f, -4.3f), o + new Vector3(0f, 3.0f, 0f));
            yield return At(7.0f); yield return Shot("05_mid_drop");
            yield return At(7.7f); yield return Shot("06_landing");
            yield return At(8.3f); yield return Shot("07_crouch");
            yield return At(9.95f); yield return Shot("08_final_stance");
            yield return new WaitForSeconds(0.4f);
            _d.ForcePrompt(1); yield return null; yield return Shot("09_prompt_pad");
            _d.ForcePrompt(2); yield return null; yield return Shot("09b_prompt_keys");
            yield return Close("10_stance_side", o + new Vector3(4.6f, 1.5f, -0.9f), o + new Vector3(0f, 1.15f, -0.55f));
            yield return Close("10b_stance_back", o + new Vector3(-1.2f, 2.0f, 4.2f), o + new Vector3(0f, 1.3f, -0.55f));
            StopCoroutine(sampler);

            Log($"eye glints on at T={_eyesOpenAt:0.00}s (planned {RatwingDirector.EyesOpen:0.00}s)");
            if (_hangGap.Count > 0)
                Log($"hanging: feet top below the ledge plane (y={_d.CeilingY:0.00}) min {_hangGap.Min() * 1000:0.0} mm, max {_hangGap.Max() * 1000:0.0} mm over {_hangGap.Count} samples; " +
                    $"raycast foot -> ledge surface min {(_hangRay.Count > 0 ? _hangRay.Min() * 1000 : float.NaN):0.0} max {(_hangRay.Count > 0 ? _hangRay.Max() * 1000 : float.NaN):0.0} mm ({_hangRay.Count} hits); " +
                    $"lowest point while hanging {_lowestHang:0.00} m above the floor");
            if (_landGap.Count > 0)
                Log($"landed: lowest foot point above the floor min {_landGap.Min() * 1000:0.0} mm, max {_landGap.Max() * 1000:0.0} mm over {_landGap.Count} samples; " +
                    $"raycast floor gap min {(_landRay.Count > 0 ? _landRay.Min() * 1000 : float.NaN):0.0} max {(_landRay.Count > 0 ? _landRay.Max() * 1000 : float.NaN):0.0} mm");
            Log($"wingspan at the stretch (5-6.4 s): {_maxSpan:0.00} m");

            // replay + skip
            _d.Restart();
            yield return new WaitForSeconds(1.0f);
            Log($"replay: T={_d.T:0.00}s after 1.0 s, state sequence={_d.Anim.GetCurrentAnimatorStateInfo(0).IsName("sequence")}, prompt hidden={!_d.Finished}");
            Log($"skip allowed (seen once)={RatwingDirector.SeenOnce}");
            _d.Skip();
            yield return null; yield return null;
            Log($"skip: T={_d.T:0.00}s, state idle={_d.Anim.GetCurrentAnimatorStateInfo(0).IsName("idle")}, finished={_d.Finished}");
            yield return Shot("11_after_skip");
            LogAudio();
            File.WriteAllText(Path.Combine(_dir, "capture_log.txt"), _log.ToString());
            Application.Quit();
        }

        IEnumerator Watchdog()
        {
            yield return new WaitForSecondsRealtime(90f);
            Log("WATCHDOG: run took too long, quitting");
            File.WriteAllText(Path.Combine(_dir, "capture_log.txt"), _log.ToString());
            Application.Quit();
        }
    }
}
