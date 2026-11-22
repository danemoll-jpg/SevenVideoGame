using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Saucer check run (-saucer -capture dir): walks 7 around, lets the cat roam, sits, waits for the cat on her lap,
    /// reveals and returns the ship, stands up. Renders the main camera off-screen into a RenderTexture, writes PNGs and
    /// capture_log.txt (audio level, feet / paws above the floor while walking, the cat on the lap vs. the top of her
    /// thighs, sizes and timings), then quits. Never needs the window or focus.
    /// </summary>
    public class SaucerCaptureRun : MonoBehaviour
    {
        SaucerDirector _d;
        string _dir;
        RenderTexture _rt;
        readonly StringBuilder _log = new StringBuilder();
        float _t0;

        public void Init(SaucerDirector d)
        {
            _d = d;
            _dir = DemoBoot.CaptureDir;
            Directory.CreateDirectory(_dir);
            _rt = ImageGate.NewRT();
            _d.Cam.GetComponent<Camera>().targetTexture = _rt;
            _d.Seven.ScriptDriven = true;
            _t0 = Time.realtimeSinceStartup;
            UnityEngine.Random.InitState(7);
            Log($"saucer capture start, muted={DemoBoot.Muted}, listenerVolume={AudioListener.volume}");
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
            Log(ImageGate.Save(_rt, _d.Cam.GetComponent<Camera>(), _dir, name + ".png"));   // QF26 (2): HDR like the screen + the image-quality gate
            Log($"shot {name}: seven={_d.Seven.State}/{_d.Seven.Current} cat={_d.Cat.Current}/{_d.Cat.Action} scale {_d.Cat.Scale:0.00} reveal={_d.Reveal.Progress:0.00}");
        }

        IEnumerator Until(Func<bool> cond, float timeout, string what)
        {
            float end = Time.time + timeout;
            while (!cond() && Time.time < end) yield return null;
            if (!cond()) Log($"TIMEOUT waiting for {what}");
        }

        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>Lowest point of the skinned meshes under a root, above the deck floor (y = 0).</summary>
        static float Lowest(Transform root, out Vector3 at)
        {
            float lowest = float.MaxValue; at = Vector3.zero;
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var m = new Mesh();
                smr.BakeMesh(m, true);
                var tr = smr.transform;
                foreach (var v in m.vertices)
                {
                    var w = tr.TransformPoint(v);
                    if (w.y < lowest) { lowest = w.y; at = w; }
                }
                Destroy(m);
            }
            return lowest;
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

        /// <summary>Park the camera at a pose (check shots).</summary>
        void Place(Vector3 pos, Vector3 look)
        {
            _d.Cam.Hold = true;
            _d.Cam.transform.position = pos;
            _d.Cam.transform.rotation = Quaternion.LookRotation(look - pos);
        }

        IEnumerator WalkTo(Vector3 target, float timeout)
        {
            var s = _d.Seven;
            float end = Time.time + timeout;
            float minL = float.MaxValue, maxL = float.MinValue;
            int samples = 0;
            while (Time.time < end)
            {
                var to = target - s.transform.position; to.y = 0f;
                if (to.magnitude < 0.15f) break;
                var f = _d.Cam.transform.forward; f.y = 0f; f.Normalize();
                var r = new Vector3(f.z, 0f, -f.x);
                to.Normalize();
                s.ScriptMove = new Vector2(Vector3.Dot(to, r), Vector3.Dot(to, f));
                if (Time.frameCount % 6 == 0 && s.State == "walk")
                {
                    float l = Lowest(s.transform, out _);
                    minL = Mathf.Min(minL, l); maxL = Mathf.Max(maxL, l); samples++;
                }
                yield return null;
            }
            s.ScriptMove = Vector2.zero;
            if (samples > 0) Log($"7 walking: lowest mesh point above the floor min {minL:0.000} max {maxL:0.000} m over {samples} samples (0 = touching)");
        }

        IEnumerator Run()
        {
            var seven = _d.Seven; var cat = _d.Cat; var cam = _d.Cam; var rev = _d.Reveal; var legend = _d.Legend;
            if (legend != null) { legend.AutoSwitch = false; legend.gameObject.SetActive(false); }
            yield return new WaitForSeconds(3f);
            LogAudio();

            // Interior wide shots (parked camera).
            Place(new Vector3(-5.6f, 2.3f, -5.0f), new Vector3(1.0f, 0.8f, 3.5f));
            yield return Frames(3);
            yield return Shot("01_ship_wide");
            Place(new Vector3(2.5f, 1.7f, 6.3f), new Vector3(-1.5f, 0.9f, -4.5f));
            yield return Frames(3);
            yield return Shot("01b_ship_wide_back");
            Place(new Vector3(0f, 1.25f, 1.0f), new Vector3(0f, 0.8f, 6.0f));
            yield return Frames(3);
            yield return Shot("01c_window_chair");
            Log($"ship bounds check: camera inside clamp radius {cam.ShellRadius}, ceiling at centre {cam.CeilingAt(0):0.00} m");
            cam.Hold = false;
            if (DemoBoot.Quick) { LogAudio(); Finish("done (quick: look shots only)"); yield break; }

            // 7 walks across the deck (feet check), the cat wanders.
            cat.ForceTarget = new Vector3(-2.5f, 0f, -1.5f);
            yield return WalkTo(new Vector3(-2.0f, 0f, -3.0f), 6f);
            yield return WalkTo(new Vector3(2.0f, 0f, -2.0f), 6f);
            // Paws while the cat walks.
            float pMin = float.MaxValue, pMax = float.MinValue; int ps = 0;
            cat.ForceTarget = new Vector3(3.0f, 0f, 1.5f);
            float end = Time.time + 4f;
            bool shot = false;
            while (Time.time < end && cat.Action == "goto")
            {
                if (Time.frameCount % 5 == 0)
                {
                    float l = Lowest(cat.transform, out _);
                    pMin = Mathf.Min(pMin, l); pMax = Mathf.Max(pMax, l); ps++;
                }
                if (!shot && Time.time > end - 2.5f)
                {
                    var c = cat.transform.position;
                    Place(c + new Vector3(-2.2f, 1.2f, -1.6f), c + Vector3.up * 0.3f);
                    yield return Shot("02_cat_wander");
                    shot = true;
                }
                yield return null;
            }
            cam.Hold = false;
            Log($"cat walking at scale {cat.Scale:0.00}: lowest mesh point above the floor min {pMin:0.000} max {pMax:0.000} m over {ps} samples");
            cat.ForceTarget = null;
            cat.ForceAction = "wash";
            yield return new WaitForSeconds(1.5f);
            {
                var c = cat.transform.position;
                Place(c + cat.transform.forward * 1.6f + new Vector3(0.6f, 0.9f, 0f), c + Vector3.up * 0.35f);
                yield return Frames(2);
                yield return Shot("02b_cat_wash");
                cam.Hold = false;
            }
            cat.ForceAction = null;

            // Walk to the chair: prompt + walking legends.
            yield return WalkTo(new Vector3(-2.2f, 0f, 0.8f), 8f);           // around the table
            yield return WalkTo(seven.StandSpot + Vector3.back * 0.9f, 8f);
            yield return Frames(10);
            Log($"near chair: {seven.NearChair}");
            if (legend != null)
            {
                legend.gameObject.SetActive(true);
                legend.SetMode(true); yield return Frames(3); yield return Shot("03_legend_walk_xbox");
                legend.SetMode(false); yield return Frames(3); yield return Shot("04_legend_walk_keys");
                legend.SetMode(true); legend.gameObject.SetActive(false);
            }

            // Sit.
            float tSit = Time.time;
            seven.RequestSit();
            yield return Until(() => seven.Current == SaucerSeven.Mode.SittingDown && seven.Tn > 0.45f, 10f, "sitting down");
            yield return Shot("05_sit_down");
            yield return Until(() => seven.IsSeated, 8f, "seated");
            Log($"seated {Time.time - tSit:0.00}s after the request; hips above root {seven.HipsAboveRoot:0.000} m, hips {seven.Hips.position}, seat target {_d.Seven.SeatPoint.position}");
            for (int i = 0; i < 12 && cat.Current != CatCompanion.Mode.HopUp; i++)
            {
                Log($"waiting for the hop: cat {cat.Current} at {cat.transform.position}, launch spot distance {Vector3.Distance(cat.transform.position, seven.ChairPivot.position + seven.ChairPivot.right * 0.95f + seven.ChairPivot.forward * 0.85f - Vector3.up * 0.25f):0.00} m");
                yield return Until(() => cat.Current == CatCompanion.Mode.HopUp, 1f, "-");
            }
            Log($"cat hop starts {Time.time - tSit:0.00}s after the sit request");
            yield return new WaitForSeconds(0.38f);
            yield return Shot("06_cat_hop_shimmer");
            Log($"cat mid-hop: scale {cat.Scale:0.00}");
            yield return Until(() => cat.Current == CatCompanion.Mode.OnLap, 5f, "cat on lap");
            yield return new WaitForSeconds(2.5f);
            yield return Shot("07_seated");
            LapCheck();

            // Lap close-up, over her shoulder.
            var hips = seven.Hips;
            var chair = _d.Seven.ChairPivot;
            Place(hips.position + chair.up * 0.75f + chair.forward * 0.55f + chair.right * 0.9f, cat.transform.position);
            yield return Frames(2);
            yield return Shot("08_lap_closeup");
            // QF3: the curled cat from the front, both sides and over her shoulder.
            foreach (var (nm, off) in new[] { ("08c_lap_front", chair.forward * 1.6f + chair.up * 0.75f), ("08d_lap_left", -chair.right * 1.0f + chair.forward * 0.25f + chair.up * 0.3f),
                                              ("08e_lap_right", chair.right * 1.0f + chair.forward * 0.25f + chair.up * 0.3f), ("08f_lap_above_left", chair.forward * 0.45f + chair.up * 1.1f - chair.right * 0.6f) })
            {
                Place(cat.transform.position + off, cat.transform.position);
                yield return Frames(2);
                yield return Shot(nm);
            }
            // Same view with 7 hidden: the cat's curled pose on its own.
            var sr = seven.GetComponentsInChildren<Renderer>();
            foreach (var r in sr) r.enabled = false;
            yield return Frames(1);
            yield return Shot("08b_lap_cat_only");
            foreach (var r in sr) r.enabled = true;
            cam.Hold = false;

            if (legend != null)
            {
                legend.gameObject.SetActive(true);
                legend.SetMode(true); yield return Frames(3); yield return Shot("09_legend_seated_xbox");
                legend.SetMode(false); yield return Frames(3); yield return Shot("10_legend_seated_keys");
                legend.SetMode(true); legend.gameObject.SetActive(false);
            }

            // Reveal.
            _d.PressReveal();
            yield return Until(() => rev.Progress >= 0.5f, 6f, "reveal half");
            yield return Shot("11_reveal_half");
            yield return Until(() => rev.Revealed, 6f, "revealed");
            yield return new WaitForSeconds(2.2f);
            yield return Shot("12_revealed_wide");
            LapCheck();
            Log($"revealed: chair tilt {Quaternion.Angle(Quaternion.identity, chair.localRotation):0.0} deg, camera pos {cam.transform.position}");
            // Over-the-shoulder looking down at the city.
            Place(hips.position + chair.up * 1.25f - chair.forward * 0.9f + chair.right * 0.7f, chair.position + chair.forward * 6f + Vector3.down * 4f);
            yield return Frames(2);
            yield return Shot("13_revealed_over_shoulder");
            Place(chair.position + new Vector3(9f, 3.5f, -7f), chair.position + Vector3.down * 1.5f);
            yield return Frames(2);
            yield return Shot("13b_revealed_far");
            // QF3: more angles on the revealed ship (nothing of it may remain: no band, no ring, no hull edge).
            foreach (var (nm, off) in new[] { ("13c_revealed_left", new Vector3(-7f, 1.5f, 4f)), ("13d_revealed_below", new Vector3(3f, -6f, 5f)), ("13e_revealed_above_behind", new Vector3(-2f, 7f, -6f)) })
            {
                Place(chair.position + off, chair.position + Vector3.up * 0.6f);
                yield return Frames(2);
                yield return Shot(nm);
            }
            RevealAudit(rev);
            cam.Hold = false;

            // Return.
            _d.PressReveal();
            yield return Until(() => rev.Progress <= 0f, 7f, "ship back");
            yield return new WaitForSeconds(1.5f);
            yield return Shot("14_ship_back");

            // Stand up while revealed: the ship comes back first, then she stands, the cat hops off.
            _d.PressReveal();
            yield return Until(() => rev.Revealed, 6f, "revealed again");
            float tStand = Time.time;
            _d.PressStand();
            yield return Until(() => seven.Current == SaucerSeven.Mode.StandingUp, 10f, "standing up");
            Log($"stand requested while revealed: ship back after {Time.time - tStand:0.00}s (reveal {rev.Progress:0.00}), then standing");
            yield return Until(() => cat.Current == CatCompanion.Mode.HopDown, 3f, "cat hop down");
            yield return new WaitForSeconds(0.35f);
            yield return Shot("15_stand_cat_hops_off");
            yield return Until(() => seven.Current == SaucerSeven.Mode.Free && cat.Current == CatCompanion.Mode.Roam, 6f, "free + roam");
            yield return new WaitForSeconds(0.8f);
            yield return Shot("16_after_stand");
            Log($"after standing: cat scale {cat.Scale:0.00}, 7 hips above root {seven.HipsAboveRoot:0.000}, 7 lowest {Lowest(seven.transform, out _):0.000} m");

            LogAudio();
            Finish("done");
        }

        /// <summary>
        /// QF3: the cat on her lap. Column by column (1.5 cm cells in the chair plane) the cat's underside vs. the top of
        /// her thighs: smallest gap (must be ~0, never below -0.5 cm = sinking in), median gap, and the clearance from
        /// the cat to her forearms / hands (mesh vertices near the lower-arm and hand bones).
        /// </summary>
        void LapCheck()
        {
            var cat = _d.Cat; var seven = _d.Seven;
            var lc = cat.MeasureLap();
            var catPts = CatCompanion.BakeWorld(cat.transform);
            var arms = new System.Collections.Generic.List<Vector3>();
            var segs = new[] { (HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand), (HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand),
                               (HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleDistal), (HumanBodyBones.RightHand, HumanBodyBones.RightMiddleDistal) };
            float SegD(Vector3 p, Vector3 a, Vector3 b) { var ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-8f)); return (p - (a + ab * t)).magnitude; }
            foreach (var p in CatCompanion.BakeWorld(seven.transform))
                foreach (var (ba, bb) in segs)
                {
                    var ta = seven.Bone(ba); var tb = seven.Bone(bb);
                    if (ta == null || tb == null) continue;
                    if (SegD(p, ta.position, tb.position) < 0.055f) { arms.Add(p); break; }
                }
            float armClear = float.MaxValue;
            foreach (var q in arms) foreach (var p in catPts) { float d = (p - q).sqrMagnitude; if (d < armClear) armClear = d; }
            armClear = arms.Count > 0 ? Mathf.Sqrt(armClear) : -1f;
            Log($"lap check: {lc.Columns} contact columns (cat {lc.CatVerts} verts, thigh {lc.ThighVerts} verts) -> smallest gap {lc.MinGap * 100f:+0.0;-0.0} cm, median gap {lc.MedianGap * 100f:0.0} cm; " +
                $"clearance to her forearms/hands {armClear * 100f:0.0} cm ({arms.Count} arm verts); cat scale {cat.Scale:0.00}, clip {cat.Current}");
        }

        /// <summary>QF3: every visible renderer of the ship that does not dissolve (or lies beyond the wave) when revealed.</summary>
        void RevealAudit(ShipReveal rev)
        {
            var keep = new System.Collections.Generic.List<Transform> { _d.Seven.transform, _d.Cat.transform, _d.Seven.ChairPivot };
            foreach (var n in new[] { "World", "Atmosphere_Above", "ShipReveal" }) { var g = GameObject.Find(n); if (g != null) keep.Add(g.transform); }
            bool Kept(Transform t) { foreach (var k in keep) if (t == k || t.IsChildOf(k)) return true; return false; }
            int stays = 0, checkedN = 0;
            var c = rev.Center.position;
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || r.forceRenderingOff || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer || Kept(r.transform)) continue;
                checkedN++;
                bool dissolves = true;
                foreach (var m in r.sharedMaterials) if (m == null || !m.HasProperty("_Dissolvable") || m.GetFloat("_Dissolvable") < 0.5f) dissolves = false;
                var b = r.bounds; float far = 0f;
                for (int i = 0; i < 8; i++) { var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)); far = Mathf.Max(far, Vector3.Distance(corner, c)); }
                if (dissolves && far <= rev.Radius) continue;
                stays++;
                string path = r.name; for (var t = r.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
                Log($"reveal audit: STAYS {path} (dissolvable={dissolves}, farthest {far:0.0} m vs wave {rev.Radius:0.0} m)");
            }
            Log($"reveal audit: {checkedN} ship renderers checked, {stays} remain visible when revealed (must be 0; 7, chair, cat, city kept)");
        }

        static Vector3[] BakeWorld(Transform root)
        {
            var list = new System.Collections.Generic.List<Vector3>();
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var m = new Mesh();
                smr.BakeMesh(m, true);
                foreach (var v in m.vertices) list.Add(smr.transform.TransformPoint(v));
                Destroy(m);
            }
            return list.ToArray();
        }

        IEnumerator Watchdog()
        {
            yield return new WaitForSecondsRealtime(240f);
            Finish("WATCHDOG timeout");
        }

        bool _finished;

        void Finish(string why)
        {
            if (_finished) return;
            _finished = true;
            Log($"capture {why}");
            Log(ImageGate.Summary(_dir)); File.WriteAllText(Path.Combine(_dir, "capture_log.txt"), _log.ToString());
            Application.Quit();
        }
    }
}
