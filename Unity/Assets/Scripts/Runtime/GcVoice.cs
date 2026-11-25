using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>Round 20: the racers' short taunts / cheers (Resources/GrandCircuit/gc_lines.json, voiced by
    /// Tools/Voice/generate_gc_voices.py in the story's voices: 7, Duchess, Tunneler). One line at a time, at most one every 8 s;
    /// subtitled on every player's HUD with the speaker's name. start = at GO (player 1's racer), pass = a racer overtakes a human,
    /// pushed = a racer near a human is pushed, last = a human starts the final lap, win = the first one home.</summary>
    public static class GcVoice
    {
        [System.Serializable] class Line { public string id, speaker, voice, @event, direction, text; }
        [System.Serializable] class Bank { public string note; public Line[] lines; }
        static Bank _bank; static float _next; static AudioSource _src; static readonly Dictionary<GcRacer, int> _place = new Dictionary<GcRacer, int>();
        public static int Played; public static string LastSubtitle; public static float SubtitleUntil;
        static void Load() { if (_bank != null) return; var ta = Resources.Load<TextAsset>("GrandCircuit/gc_lines"); _bank = ta != null ? JsonUtility.FromJson<Bank>(ta.text) : new Bank { lines = new Line[0] }; }
        static string Key(GcRacer r) => r.Stats == GcStats.Duchess ? "duchess" : r.Stats == GcStats.Tunneler ? "tunneler" : "seven";
        static string Name(string sp) => sp == "duchess" ? "Duchess" : sp == "tunneler" ? "Tunneler" : "7";
        public static void Reset() { _next = 0f; _place.Clear(); Played = 0; }
        public static bool Say(GcRacer r, string ev, bool force = false)
        {
            Load(); if (r == null || (!force && Time.time < _next)) return false;
            if (_src != null && _src.isPlaying) return false;
            var sp = Key(r); Line pick = null; foreach (var l in _bank.lines) if (l.speaker == sp && l.@event == ev) { pick = l; break; }
            if (pick == null) return false;
            var clip = Resources.Load<AudioClip>("GcVoice/" + pick.id);
            if (clip != null)
            {
                if (_src == null) { var go = new GameObject("GcVoice"); Object.DontDestroyOnLoad(go); _src = go.AddComponent<AudioSource>(); _src.spatialBlend = 0f; }
                _src.clip = clip; AudioKit.Track(_src, AudioKit.Bus.Voice, 1f); _src.Play();
            }
            LastSubtitle = $"{Name(sp)}: {pick.text}"; SubtitleUntil = Time.time + Mathf.Max(1.8f, clip != null ? clip.length + 0.4f : 2.2f);
            _next = Time.time + 8f; Played++;
            GcGame.Log($"GC VOICE (R20): {pick.id} '{pick.text}' ({(clip != null ? $"{clip.length:0.0} s" : "subtitle only")})");
            return true;
        }
        public static void On(GcGame g, GcRacer r, string e)
        {
            if (e == "pushed" && Near(g, r)) Say(r, "pushed");
            else if (e == "lap" && r.Human && r.Lap == g.Laps) Say(r, "last");
            else if (e.StartsWith("finish") || (e == "lap" && r.Lap == g.Laps + 1 && r.Place == 1)) Say(r, "win", true);
        }
        public static void Start(GcGame g) { Reset(); var r = g.Humans.Count > 0 ? g.Humans[0] : g.Racers.Count > 0 ? g.Racers[0] : null; Say(r, "start", true); }
        /// <summary>Every frame of a race: an AI that moved ahead of a human taunts (once in a while).</summary>
        public static void Tick(GcGame g)
        {
            foreach (var r in g.Racers)
            {
                _place.TryGetValue(r, out int was); _place[r] = r.Place;
                if (r.Human || was == 0 || r.Place >= was) continue;
                foreach (var h in g.Humans) { if (h.Place == r.Place + 1 && Mathf.Abs(h.Distance - r.Distance) < 25f) { Say(r, "pass"); break; } }
            }
        }
        static bool Near(GcGame g, GcRacer r) { if (r.Human) return true; foreach (var h in g.Humans) if (Mathf.Abs(h.Distance - r.Distance) < 40f) return true; return false; }
    }
}
