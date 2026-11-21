using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seven
{
    /// <summary>QF14: one chapter of the game as the menus show it (see <see cref="ChapterList"/>).</summary>
    public sealed class ChapterDef
    {
        /// <summary>Short id: the unlock key ("seven_unlocked_&lt;id&gt;").</summary>
        public string Id;
        /// <summary>The menu entry and the title of its page.</summary>
        public string Name;
        /// <summary>The save says she has got here (null = always open). Once true it is remembered (a new game keeps it).</summary>
        public System.Func<bool> Reached;
        /// <summary>"Play from the start" resets saved progress (null = never): the menus ask first with <see cref="Confirm"/>.</summary>
        public System.Func<bool> HasProgress;
        public string Confirm;
        public System.Action PlayFromStart;
        /// <summary>The chapter's sections (Scene Select entries), each with the state a real playthrough has there.</summary>
        public System.Func<(string label, System.Action start)[]> Sections;
    }

    /// <summary>
    /// QF14 (Dan): THE chapter list - the main menu (one entry per unlocked chapter → its page: "Play from the start", its
    /// sections, "Back") and the pause menu's Scene select both read this table. A new chapter = one new row here.
    /// Unlocks come from the existing saves (the player's own slots, read without switching a running Scene Select slot)
    /// and are remembered in PlayerPrefs "seven_unlocked_&lt;id&gt;" (check runs: "seven_test_unlocked_&lt;id&gt;"), so a
    /// new game never takes a chapter away again.
    /// </summary>
    public static class ChapterList
    {
        public const string PlayFromStartLabel = "Play from the start";

        public static readonly ChapterDef[] All =
        {
            new ChapterDef
            {
                Id = "moon", Name = "Chapter 1 — The Moon", Reached = null,
                // the new-game path (Round 13 0c): every chapter's progress is reset, the opening shot plays
                HasProgress = () => SaveGame.AnyProgress, Confirm = "Start a new game? Your saved progress will be reset.",
                PlayFromStart = () => { SaveGame.NewGame(); Chapter1State.Reset(); Chapter1State.Intro = true; SceneManager.LoadScene("Chapter1"); },
                Sections = MoonSections,
            },
            new ChapterDef
            {
                Id = "venus", Name = "Venus",
                // Chapter 1 has reached the galley (its last scene; Set course flies on to Venus), or Venus was reached
                Reached = () => SaveGame.Furthest >= Chapter1State.CpGalley || VenusState.OwnReached,
                HasProgress = () => VenusState.OwnHasProgress, Confirm = "Start Venus over? Your Venus progress will be reset.",
                PlayFromStart = () => { Chapter1State.Reset(); VenusState.StartFresh(); },
                Sections = VenusSections,
            },
            new ChapterDef
            {
                Id = "europa", Name = "Europa",
                // Europa was reached, or VEN_C12 has sent it to the ship's star map (the same rule as the star map)
                Reached = () => EuropaState.OwnReached || VenusState.OwnSceneDone("VEN_C12"),
                HasProgress = () => EuropaState.OwnHasProgress, Confirm = "Start Europa over? Your Europa progress will be reset.",
                PlayFromStart = () => { Chapter1State.Reset(); EuropaState.StartFresh(); },
                Sections = EuropaSections,
            },
            new ChapterDef
            {
                Id = "mars", Name = "Mars",
                // Mars was reached, or VEN_C12 has sent it to the ship's star map (the same rule as Europa)
                Reached = () => MarsState.OwnReached || VenusState.OwnSceneDone("VEN_C12"),
                HasProgress = () => MarsState.OwnHasProgress, Confirm = "Start Mars over? Your Mars progress will be reset.",
                PlayFromStart = () => { Chapter1State.Reset(); MarsState.StartFresh(); },
                Sections = MarsSections,
            },
            new ChapterDef
            {
                Id = "asteroid", Name = "The Asteroid",
                // Round 16: the asteroid was reached, or the travel event (TRV_AST) has found it
                Reached = () => AsteroidState.OwnReached || WorldFlags.AsteroidFound,
                HasProgress = () => AsteroidState.OwnHasProgress, Confirm = "Start the Asteroid over? Your progress there will be reset.",
                PlayFromStart = () => { Chapter1State.Reset(); AsteroidState.StartFresh(); },
                Sections = AsteroidSections,
            },
            new ChapterDef
            {
                Id = "biaveh", Name = "The Biavian Mothership",
                // Round 17: reached, or the combined records found the Biavian contact route (MAR_13C)
                Reached = () => BiavehState.OwnReached || WorldFlags.BiavehUnlocked,
                HasProgress = () => BiavehState.OwnHasProgress, Confirm = "Start the Biavian Mothership over? Your progress there will be reset.",
                PlayFromStart = () => { Chapter1State.Reset(); BiavehState.StartFresh(); },
                Sections = BiavehSections,
            },
        };

        static (string label, System.Action start)[] MoonSections()
        {
            var l = new List<(string label, System.Action start)>();
            for (int cp = Chapter1State.CpCell; cp <= Chapter1State.CpGalley; cp++)
            {
                int c = cp;
                l.Add((Chapter1State.CpName(c), () => { Chapter1State.StartAt(c); Chapter1State.Intro = c == Chapter1State.CpCell; }));
            }
            return l.ToArray();
        }
        static (string label, System.Action start)[] VenusSections()
        {
            var l = new List<(string label, System.Action start)>();
            foreach (var (label, id) in VenusState.SelectEntries) { string e = id; l.Add((label, () => { Chapter1State.Reset(); VenusState.StartAt(e); })); }
            return l.ToArray();
        }
        static (string label, System.Action start)[] EuropaSections()
        {
            var l = new List<(string label, System.Action start)>();
            foreach (var (label, cp) in EuropaState.SelectEntries) { int c = cp; l.Add((label, () => { Chapter1State.Reset(); EuropaState.StartAt(c); })); }
            return l.ToArray();
        }

        static (string label, System.Action start)[] MarsSections()
        {
            var l = new List<(string label, System.Action start)>();
            foreach (var (label, cp) in MarsState.SelectEntries) { int c = cp; l.Add((label, () => { Chapter1State.Reset(); MarsState.StartAt(c); })); }
            return l.ToArray();
        }

        static (string label, System.Action start)[] BiavehSections()
        {
            var l = new List<(string label, System.Action start)>();
            foreach (var (label, cp) in BiavehState.SelectEntries) { int c = cp; l.Add((label, () => { Chapter1State.Reset(); BiavehState.StartAt(c); })); }
            return l.ToArray();
        }
        static (string label, System.Action start)[] AsteroidSections()
        {
            var l = new List<(string label, System.Action start)>();
            foreach (var (label, cp) in AsteroidState.SelectEntries) { int c = cp; l.Add((label, () => { Chapter1State.Reset(); AsteroidState.StartAt(c); })); }
            return l.ToArray();
        }

        public static int IndexOf(ChapterDef c) => System.Array.IndexOf(All, c);
        public static ChapterDef ById(string id) => System.Array.Find(All, c => c.Id == id);

        public static bool IsUnlocked(ChapterDef c)
        {
            if (c.Reached == null) return true;
            if (SaveGame.WasUnlocked(c.Id)) return true;
            if (!c.Reached()) return false;
            SaveGame.MarkUnlocked(c.Id);
            return true;
        }
        public static List<ChapterDef> Unlocked()
        {
            var l = new List<ChapterDef>();
            foreach (var c in All) if (IsUnlocked(c)) l.Add(c);
            return l;
        }

        /// <summary>Check runs only (-menupages, -menupick): every chapter open, on the test keys (never the player's).</summary>
        public static void UnlockAllForTest()
        {
            if (DemoBoot.CaptureDir == null) return;
            foreach (var c in All) SaveGame.MarkUnlocked(c.Id);
        }

        /// <summary>Start a section, logged the same way from both menus.</summary>
        public static void StartSection(ChapterDef c, int i, string from)
        {
            var s = c.Sections();
            if (i < 0 || i >= s.Length) return;
            Debug.Log($"[Seven] scene select{from}: {c.Name} - {s[i].label}");
            s[i].start();
        }
        public static void StartFromBeginning(ChapterDef c, string from)
        {
            Debug.Log($"[Seven] {c.Name}: play from the start{from}");
            c.PlayFromStart();
        }
    }
}
