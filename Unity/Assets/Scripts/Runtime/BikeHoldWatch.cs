using System.Collections;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// QF17 (5): the bike-hold rule, checked on every world's arrival (Venus B01, Europa, Mars - every start path: the star
    /// map, VEN_C13 / MAR_13U, the main menu's chapter list, Scene Select "arrival", Continue): from the moment the world
    /// loads until the hatch opens, the bike must never be visible outside the hold. Every frame: is any of its renderers
    /// drawn, and is it outside the saucer's hull? Logs "BIKE HOLD CHECK (QF17 5) world: ... PASS / FAIL" (in every run that
    /// plays an arrival, and in the player-style runs) and, with -holdcheck, ends the run once she is down on the bike.
    /// </summary>
    public class BikeHoldWatch : MonoBehaviour
    {
        public static int Failures { get; private set; }
        public static string Last;
        string _world; SaucerHold _hold; HoverBike _bike; Transform _saucer; float _t0;
        int _frames, _outside; Vector3 _firstAt; bool _done;

        /// <summary>A director calls this as its world loads, on the path that plays the arrival (the bike still in the hold).</summary>
        public static void Begin(string world, SaucerHold hold, HoverBike bike, Transform saucer)
        {
            if (bike == null) return;
            var go = new GameObject("BikeHoldWatch"); var w = go.AddComponent<BikeHoldWatch>();
            w._world = world; w._hold = hold; w._bike = bike; w._saucer = saucer; w._t0 = Time.time;
            ChapterCapture.I?.Log($"BIKE HOLD CHECK (QF17 5) {world}: watching from the world's first frame until the hatch opens");
        }

        bool Drawn()
        {
            if (!_bike.gameObject.activeInHierarchy) return false;
            foreach (var r in _bike.GetComponentsInChildren<Renderer>()) if (r.enabled && !r.forceRenderingOff && r.gameObject.activeInHierarchy && !(r is ParticleSystemRenderer)) return true;
            return false;
        }
        bool InsideHull()
        {
            if (_saucer == null) return false;
            var d = _bike.transform.position - _saucer.position; float flat = new Vector2(d.x, d.z).magnitude;
            return flat < 6.2f && d.y > -2.5f && d.y < 3.5f;
        }

        void LateUpdate()
        {
            if (_done) return;
            bool opened = _hold == null || _hold.IsOpen || _hold.Slide > 0.02f;
            if (!opened)
            {
                _frames++;
                if (Drawn() && !InsideHull()) { if (_outside == 0) _firstAt = _bike.transform.position; _outside++; }
                return;
            }
            _done = true;
            bool pass = _outside == 0 && _hold != null;
            if (!pass) Failures++;
            Last = $"BIKE HOLD CHECK (QF17 5) {_world}: {(pass ? "PASS" : "FAIL")} - {_frames} frames from the world's first frame to the hatch opening, the bike drawn outside the hold in {_outside}{(_outside > 0 ? $" (first at {_firstAt:0.0})" : "")}{(_hold == null ? " - NO HOLD FOUND" : "")}";
            Debug.Log("[Seven] " + Last);
            ChapterCapture.I?.Log(Last);
            if (ChapterCapture.I != null && DemoBoot.CaptureDir != null) StartCoroutine(Shots());
            else Destroy(gameObject, 1f);
        }
        IEnumerator Shots()
        {
            if (_saucer != null) ImageGate.Expect(_saucer, "the saucer and its bike hold"); else if (_hold != null) ImageGate.Expect(_hold.transform, "the bike hold");     // QF26 (2)
            yield return ChapterCapture.I.Shot($"hold_{_world}_hatch_opens");
            for (float t = 0f; t < 3f; t += Time.deltaTime) yield return null;
            if (_bike != null) ImageGate.Expect(_bike.transform, "the bike"); else if (_hold != null) ImageGate.Expect(_hold.transform, "the bike hold");
            yield return ChapterCapture.I.Shot($"hold_{_world}_bike_lowers_out");
            if (DemoBoot.HoldCheck)
            {
                // the rest of the arrival: 7 comes down the beam to the bike, then the run ends
                for (float t = 0f; t < 25f; t += Time.deltaTime) { var b = _bike; if (b != null && b.Riding) break; yield return null; }
                yield return ChapterCapture.I.Shot($"hold_{_world}_she_is_on_the_bike");
                ChapterCapture.I.Finish(Last);
            }
            Destroy(gameObject);
        }
    }
}
