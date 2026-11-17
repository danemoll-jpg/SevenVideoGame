using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Named audio clips (Round 7). Built by the editor builder from the folders Assets/Audio/SFX (*.wav) and
    /// Assets/Audio/Music (*.mp3) into Assets/Resources/Audio/SfxBank.asset / MusicBank.asset, loaded at runtime by
    /// name. A name with no file is simply missing: callers get null and play silence (never an error).
    /// </summary>
    public class AudioBank : ScriptableObject
    {
        [System.Serializable] public class Entry { public string Name; public AudioClip Clip; }
        public List<Entry> Entries = new List<Entry>();
        Dictionary<string, AudioClip> _by;

        public AudioClip Get(string name)
        {
            if (_by == null) { _by = new Dictionary<string, AudioClip>(); foreach (var e in Entries) if (e.Clip != null) _by[e.Name] = e.Clip; }
            return name != null && _by.TryGetValue(name, out var c) ? c : null;
        }

        /// <summary>All clips whose name starts with the prefix (footstep variants: step_rock_1..3).</summary>
        public List<AudioClip> Variants(string prefix)
        {
            var r = new List<AudioClip>();
            foreach (var e in Entries) if (e.Clip != null && e.Name.StartsWith(prefix)) r.Add(e.Clip);
            return r;
        }
    }
}
