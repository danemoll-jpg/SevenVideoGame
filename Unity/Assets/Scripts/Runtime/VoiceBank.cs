using System.Collections.Generic;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// Every Chapter 1 voice line: id, scene, speaker, subtitle text, pause after, audio clip. Built by the editor
    /// builder from Assets/Audio/Voice/voice_lines.json (made by Tools/Voice/make_line_list.py from Docs/DIALOGUE.md)
    /// and the generated clips in Assets/Audio/Voice/&lt;scene dir&gt;/&lt;id&gt;.mp3.
    /// </summary>
    public class VoiceBank : ScriptableObject
    {
        [System.Serializable]
        public class Line
        {
            public string Id, Speaker, Text;
            public int Scene;
            public float PauseAfter;
            public AudioClip Clip;
            public bool IsCat => Speaker == "cat";
        }

        public List<Line> Lines = new List<Line>();
        Dictionary<string, Line> _byId;

        public Line Get(string id)
        {
            if (_byId == null) { _byId = new Dictionary<string, Line>(); foreach (var l in Lines) _byId[l.Id] = l; }
            return _byId.TryGetValue(id, out var x) ? x : null;
        }

        /// <summary>Lines of one scene in script order, optionally a 1-based range.</summary>
        public List<Line> Scene(int scene, int from = 1, int to = 999)
        {
            var r = new List<Line>();
            int n = 0;
            foreach (var l in Lines) if (l.Scene == scene) { n++; if (n >= from && n <= to) r.Add(l); }
            return r;
        }
    }
}
