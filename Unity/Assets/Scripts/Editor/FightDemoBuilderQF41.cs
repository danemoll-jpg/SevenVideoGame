using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Seven.EditorTools
{
    /// <summary>
    /// QF41: Dan's six Cat-a-tron commercials (Art/Video/catatron/ad_*.mp4, 12 s, grayscale, no sound) copied into the project
    /// (Assets/Video/catatron/) and wired to the screen with their mixed tracks (Assets/Resources/Catatron/ad_*.ogg, made by
    /// Tools/Audio/make_catatron_ads.py: each ad's own voice, its effects and its music bed) and the channel-change glitch.
    /// </summary>
    public static partial class FightDemoBuilder
    {
        static readonly string[] CatatronAdFiles = { "ad_midnight_zoomies", "ad_empty_bowl_crisis", "ad_eau_de_tuna", "ad_gravity_championship", "ad_bird_channel", "ad_box_deluxe" };

        static void CatatronAds(Seven.CatatronScreen screen)
        {
            var clips = new List<UnityEngine.Video.VideoClip>(); var audio = new List<AudioClip>(); var names = new List<string>();
            Directory.CreateDirectory(Path.Combine(ProjectRoot, "Assets/Video/catatron"));
            foreach (var f in CatatronAdFiles)
            {
                var src = Path.Combine(RepoRoot, "Art", "Video", "catatron", f + ".mp4");
                if (!File.Exists(src)) { Log($"QF41: WARNING {src} missing - that ad is left out"); continue; }
                var dst = "Assets/Video/catatron/" + f + ".mp4";
                File.Copy(src, Path.Combine(ProjectRoot, dst), true);
                AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceSynchronousImport);
                var vc = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(dst);
                var ac = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Catatron/" + f + ".ogg");
                if (vc == null) { Log($"QF41: WARNING {dst} did not import"); continue; }
                clips.Add(vc); audio.Add(ac); names.Add(f.Substring(3));
                Log($"QF41: the Cat-a-tron ad '{f}' ({vc.length:0.0} s, {vc.width}x{vc.height}) + its track {(ac != null ? $"({ac.length:0.0} s)" : "MISSING")}");
            }
            screen.Ads = clips.ToArray(); screen.AdAudio = audio.ToArray(); screen.AdNames = names.ToArray();
            Log($"CAT-A-TRON ADS (QF41): {clips.Count} / {CatatronAdFiles.Length} commercials on the screen, no channel-change glitch (QF42: the running ad finishes, then the race ad)");
        }
    }
}
