using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Round 13: conversation reactions (laughs, chuckles, "hmm", "mm-hmm", sighs, purrs). Placement data =
    /// Resources/Reactions/reactions.json (one sound by the listener after a spoken line id; Docs/REACTIONS.md); the
    /// bank = Resources/Reactions/&lt;who&gt;_&lt;kind&gt;_&lt;n&gt;.mp3 (Tools/Voice/generate_reactions.py, 3 takes each).
    /// <see cref="DialoguePlayer"/> plays them in the gap after the line (the next line waits for it). Directors can
    /// listen to <see cref="OnReaction"/> for a body cue; DialoguePlayer adds a small default one where it is safe.
    /// Switch: -noreactions (none play).
    /// </summary>
    public static class Reactions
    {
        [System.Serializable] public class Placement { public string after, who, kind, cue, why; public float delay; }
        [System.Serializable] class BankJson { public string[] seven, cat; }
        [System.Serializable] class FileJson { public BankJson bank; public Placement[] placements; }

        /// <summary>Base volume of a reaction relative to a spoken line on the same voice treatment (they are quiet).</summary>
        public const float Volume = 0.5f;
        /// <summary>Silence after a reaction before the next line may start (s).</summary>
        public const float GapAfter = 0.3f;
        /// <summary>Takes looked for per kind (who_kind_1 .. who_kind_MaxTakes).</summary>
        public const int MaxTakes = 6;

        /// <summary>who ("seven" | "cat"), kind (bank name), cue (head_tilt, shoulders, nod, look_down, tail, ears, none).</summary>
        public static event System.Action<string, string, string> OnReaction;

        static Dictionary<string, Placement> _byLine;
        static readonly Dictionary<string, AudioClip[]> _clips = new Dictionary<string, AudioClip[]>();
        static readonly Dictionary<string, int> _lastTake = new Dictionary<string, int>();
        static bool? _off;

        /// <summary>False with -noreactions on the command line.</summary>
        public static bool Enabled
        {
            get
            {
                if (_off == null) _off = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-noreactions") >= 0;
                return !_off.Value;
            }
        }

        /// <summary>Number of placements loaded (for logs).</summary>
        public static int Count { get { Load(); return _byLine.Count; } }

        static void Load()
        {
            if (_byLine != null) return;
            _byLine = new Dictionary<string, Placement>();
            var ta = Resources.Load<TextAsset>("Reactions/reactions");
            if (ta == null) { Debug.LogWarning("[Seven] reactions: Resources/Reactions/reactions.json not found (no reactions)"); return; }
            FileJson f = null;
            try { f = JsonUtility.FromJson<FileJson>(ta.text); }
            catch (System.Exception e) { Debug.LogWarning("[Seven] reactions: reactions.json could not be read: " + e.Message); }
            if (f?.placements == null) return;
            foreach (var p in f.placements)
            {
                if (p == null || string.IsNullOrEmpty(p.after) || string.IsNullOrEmpty(p.kind)) continue;
                p.who = p.who == "cat" ? "cat" : "seven";
                if (p.delay < 0f) p.delay = 0f;
                if (string.IsNullOrEmpty(p.cue)) p.cue = "none";
                _byLine[p.after] = p;
            }
        }

        /// <summary>The placement after this line id, or null.</summary>
        public static Placement After(string lineId)
        {
            if (!Enabled || string.IsNullOrEmpty(lineId)) return null;
            Load();
            return _byLine.TryGetValue(lineId, out var p) ? p : null;
        }

        static AudioClip[] Takes(string who, string kind)
        {
            string key = who + "_" + kind;
            if (_clips.TryGetValue(key, out var a)) return a;
            var l = new List<AudioClip>();
            for (int n = 1; n <= MaxTakes; n++) { var c = Resources.Load<AudioClip>($"Reactions/{key}_{n}"); if (c != null) l.Add(c); }
            a = l.ToArray();
            _clips[key] = a;
            return a;
        }

        /// <summary>A random take of that kind for that speaker, never the same take twice in a row; take = 1-based
        /// index (0 = none found).</summary>
        public static AudioClip Pick(string who, string kind, out int take)
        {
            take = 0;
            var a = Takes(who, kind);
            if (a.Length == 0) return null;
            string key = who + "_" + kind;
            int last = _lastTake.TryGetValue(key, out int li) ? li : -1;
            int i = Random.Range(0, a.Length);
            if (a.Length > 1 && i == last) i = (i + 1 + Random.Range(0, a.Length - 1)) % a.Length;   // any other take
            _lastTake[key] = i;
            // report the file's own take number (who_kind_<n>)
            var name = a[i].name; int us = name.LastIndexOf('_');
            take = us >= 0 && int.TryParse(name.Substring(us + 1), out int fileN) ? fileN : i + 1;
            return a[i];
        }

        internal static void Fire(string who, string kind, string cue)
        {
            try { OnReaction?.Invoke(who, kind, cue); }
            catch (System.Exception e) { Debug.LogException(e); }
        }
    }
}
