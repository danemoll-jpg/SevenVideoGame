using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Seven.EditorTools
{
    /// <summary>
    /// Round 7: named audio banks loaded at runtime from Resources (Seven.AudioBank): every Assets/Audio/SFX/*.wav
    /// (ElevenLabs sound effects, Tools/Audio/generate_sfx.py) and every Assets/Audio/Music/*.mp3 (Dan's Suno tracks).
    /// A file that is not there is simply not in the bank (the game plays silence for it).
    /// </summary>
    public static partial class FightDemoBuilder
    {
        static void BuildAudioBanks()
        {
            Directory.CreateDirectory(Path.Combine(ProjectRoot, "Assets/Resources/Audio"));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Bank("Assets/Audio/SFX", "*.wav|*.mp3", "Assets/Resources/Audio/SfxBank.asset", false);
            Bank("Assets/Audio/Music", "*.mp3|*.ogg", "Assets/Resources/Audio/MusicBank.asset", true);
        }

        static void Bank(string dir, string pattern, string bankPath, bool music)
        {
            var full = Path.Combine(ProjectRoot, dir);
            var bank = ScriptableObject.CreateInstance<Seven.AudioBank>();
            if (Directory.Exists(full))
                foreach (var f in pattern.Split('|').SelectMany(pt => Directory.GetFiles(full, pt)).OrderBy(x => x))
                {
                    var p = dir + "/" + Path.GetFileName(f);
                    var imp = AssetImporter.GetAtPath(p) as AudioImporter;
                    if (imp != null)
                    {
                        var s = imp.defaultSampleSettings;
                        bool loop = Path.GetFileNameWithoutExtension(f).StartsWith("amb_") || f.Contains("_hum") || f.Contains("shimmer");
                        s.loadType = music ? AudioClipLoadType.Streaming : loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                        s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = music ? 0.7f : 0.6f;
                        imp.defaultSampleSettings = s;
                        imp.forceToMono = !music && !loop;     // one-shots are placed in 3D: mono (QF10: Dan's 48 kHz stereo scree_chirp.mp3 too)
                        imp.loadInBackground = music;
                        imp.SaveAndReimport();
                    }
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                    if (clip != null) bank.Entries.Add(new Seven.AudioBank.Entry { Name = Path.GetFileNameWithoutExtension(f), Clip = clip });
                }
            AssetDatabase.DeleteAsset(bankPath);
            AssetDatabase.CreateAsset(bank, bankPath);
            AssetDatabase.SaveAssets();
            Log($"audio bank {bankPath}: {bank.Entries.Count} clips [{string.Join(", ", bank.Entries.Select(e => e.Name))}]");
        }
    }
}
