using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF21 (6) — standing rule, every chapter: NO SHOT HOLDS ON EMPTY SPACE. Dan: "the camera stays fixed while the
    /// Machinist walks off screen ... the player stares at an empty shot". A moving character stays in frame.
    ///  - <see cref="ShotFollow"/>: a scripted tracking shot - the held framing (position + look) moves with that character.
    ///  - The automatic rule for every scripted shot in every chapter (Moon, Venus, Europa, Mars, the Asteroid): a person
    ///    (7, the cat, an NPC on MarsFolk / ElarimNpc) seen in this shot who is WALKING toward the frame's edge, with nobody
    ///    else left in the frame, makes the camera follow them (pan + track) until they stop - instead of an empty frame.
    ///  - Measured in every check run: `EMPTY SHOTS (QF21 6)`: shots where a walker left the frame and the frame stayed
    ///    empty (> 0.5 s; must be 0), follows started, per chapter.
    /// </summary>
    public partial class ChapterCamera
    {
        /// <summary>A scripted tracking shot: the held framing moves with this transform (null = a fixed shot).</summary>
        [System.NonSerialized] public Transform ShotFollow;
        Vector3 _sfLast; Transform _sfOf; bool _autoFollow; float _sfStill;
        public static int ShotFollows, EmptyShots, ShotsWatched; public static float EmptyWorst; public static string EmptyWorstWhat;
        public static readonly List<string> FollowLog = new List<string>();
        readonly HashSet<Transform> _seen = new HashSet<Transform>();
        readonly Dictionary<Transform, Vector3> _last = new Dictionary<Transform, Vector3>();
        readonly Dictionary<Transform, Vector3> _lastSeenAt = new Dictionary<Transform, Vector3>();
        readonly List<Transform> _people = new List<Transform>();
        float _peopleAt = -99f, _emptyFor; Vector3 _shotAnchor; bool _emptyCounted;
        public static bool AutoFollowOff;                       // -noqf21fix: the old fixed shots (repro)

        /// <summary>Called by Shot(): a new framing (a cut or a move of more than 2 m) starts a new watch.</summary>
        void ShotBegan(Vector3 pos, bool cut)
        {
            if (cut || (pos - _shotAnchor).magnitude > 2f)
            {
                _seen.Clear(); _lastSeenAt.Clear(); _emptyFor = 0f; _emptyCounted = false; ShotsWatched++;
                if (_autoFollow) { ShotFollow = null; _autoFollow = false; }
            }
            _shotAnchor = pos;
        }
        void ShotEnded() { if (_autoFollow) ShotFollow = null; _autoFollow = false; _seen.Clear(); }

        void RefreshPeople()
        {
            if (Time.time - _peopleAt < 1f) return;
            _peopleAt = Time.time; _people.Clear();
            foreach (var c in FindObjectsByType<ChapterSeven>(FindObjectsSortMode.None)) if (c.isActiveAndEnabled) _people.Add(c.transform);
            foreach (var c in FindObjectsByType<MarsFolk>(FindObjectsSortMode.None)) if (c.isActiveAndEnabled) _people.Add(c.transform);
            foreach (var c in FindObjectsByType<ElarimNpc>(FindObjectsSortMode.None)) if (c.isActiveAndEnabled) _people.Add(c.transform);
            foreach (var c in FindObjectsByType<ChapterCat>(FindObjectsSortMode.None)) if (c.isActiveAndEnabled) _people.Add(c.transform);
        }
        static Vector3 Center(Transform t)
        {
            var r = t.GetComponentInChildren<SkinnedMeshRenderer>();
            return r != null ? r.bounds.center : t.position + Vector3.up * 0.9f;
        }
        bool InFrame(Camera cam, Transform t, float m = 0.02f)
        {
            var v = cam.WorldToViewportPoint(Center(t));
            return v.z > 0.2f && v.x > m && v.x < 1f - m && v.y > m && v.y < 1f - m;
        }

        /// <summary>Per frame while a shot is held (before it eases): follow a walker out of an otherwise empty frame.</summary>
        void ShotTrack(float dt)
        {
            var cam = GetComponent<Camera>();
            // a scripted or automatic follow: the framing moves with the walker
            if (ShotFollow != null)
            {
                if (_sfOf != ShotFollow) { _sfOf = ShotFollow; _sfLast = ShotFollow.position; _sfStill = 0f; }
                var d = ShotFollow.position - _sfLast; _sfLast = ShotFollow.position;
                if (d.sqrMagnitude > 1e-8f)
                {
                    var np = _shotPos + d;
                    // never into geometry: then only pan
                    if (!RideProbe.Overlap(np, 0.25f, null, null, false)) { _shotPos = np; transform.position += d; }
                    _shotLook += d; _look += d;
                    _sfStill = 0f;
                }
                else _sfStill += dt;
                if (_autoFollow)
                {
                    // keep the walker framed: the look eases toward him
                    _shotLook = Vector3.Lerp(_shotLook, Center(ShotFollow), dt * 2.5f);
                    if (_sfStill > 1.2f) { _autoFollow = false; ShotFollow = null; }
                }
            }
            else _sfOf = null;
            if (AutoFollowOff || DemoBoot.NoQf21Fix) { WatchEmpty(cam, dt); return; }
            RefreshPeople();
            int inFrame = 0; Transform leaving = null;
            foreach (var p in _people)
            {
                if (p == null) continue;
                var pos = p.position; _last.TryGetValue(p, out var lp); _last[p] = pos;
                bool inF = InFrame(cam, p);
                if (inF) { _seen.Add(p); inFrame++; _lastSeenAt[p] = pos; }
                float speed = dt > 0f && lp != Vector3.zero ? new Vector2(pos.x - lp.x, pos.z - lp.z).magnitude / dt : 0f;
                if (!_seen.Contains(p) || speed < 0.35f || speed > 30f) continue;
                // walking and at / past the edge of the frame
                var v = cam.WorldToViewportPoint(Center(p));
                bool edge = v.z < 0.2f || v.x < 0.1f || v.x > 0.9f || v.y < 0.06f || v.y > 0.97f;
                if (edge) leaving = p;
            }
            if (leaving != null && ShotFollow == null)
            {
                int others = 0; foreach (var p in _people) if (p != null && p != leaving && InFrame(cam, p, 0.08f)) others++;
                if (others == 0)
                {
                    ShotFollow = leaving; _autoFollow = true; _sfOf = null; ShotFollows++;
                    string msg = $"SHOT FOLLOW (QF21 6): {leaving.name} walked toward the edge of a held shot with nobody else in it - the camera follows ({UnityEngine.SceneManagement.SceneManager.GetActiveScene().name})";
                    FollowLog.Add(leaving.name); Debug.Log("[Seven] " + msg); if (DemoBoot.CaptureDir != null) ChapterCapture.I?.Log(msg);
                }
            }
            WatchEmpty(cam, dt);
        }
        /// <summary>The measure: a walker seen in this shot is gone and nobody is in the frame.</summary>
        void WatchEmpty(Camera cam, float dt)
        {
            RefreshPeople();
            if (_seen.Count == 0) return;
            bool anyIn = false, walkerGone = false;
            foreach (var p in _people) { if (p == null) continue; if (InFrame(cam, p)) { anyIn = true; break; } }
            if (!anyIn)
                foreach (var p in _seen)
                    if (p != null && _lastSeenAt.TryGetValue(p, out var sp)) { float mv = new Vector2(p.position.x - sp.x, p.position.z - sp.z).magnitude; if (mv > 0.8f && mv < 8f) { walkerGone = true; break; } }
            if (!anyIn && walkerGone) _emptyFor += dt; else _emptyFor = 0f;
            if (_emptyFor > 0.5f && !_emptyCounted)
            {
                _emptyCounted = true; EmptyShots++;
                string what = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                EmptyWorstWhat = what; EmptyWorst = Mathf.Max(EmptyWorst, _emptyFor);
                string msg = $"EMPTY SHOT (QF21 6): a held shot in {what} has been empty for {_emptyFor:0.0} s after someone walked out of it";
                Debug.Log("[Seven] " + msg); if (DemoBoot.CaptureDir != null) ChapterCapture.I?.Log(msg);
            }
        }
        public static string ShotSummary() => $"EMPTY SHOTS (QF21 6): {ShotsWatched} held shots watched, the camera followed a walker {ShotFollows} times{(FollowLog.Count > 0 ? " (" + string.Join(", ", FollowLog) + ")" : "")}, shots left empty > 0.5 s after a walker left: {EmptyShots} (must be 0)";
    }
}
