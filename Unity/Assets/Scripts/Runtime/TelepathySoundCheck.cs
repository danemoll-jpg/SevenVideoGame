using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace Seven
{
    /// <summary>
    /// The one short unmuted sound check of the telepathy effect (-mute -soundcheck -capture dir, from the menu scene):
    /// unmutes, plays one cat line (s01_05_cat, "The cat.") through the telepathic source, measures the output (peak, left
    /// vs. right = centred, the reverb tail after the dry line ends), mutes again, measures 0, writes soundcheck_log.txt
    /// and quits. Sound plays only between "unmuted" and "muted again" in the log.
    /// </summary>
    public class TelepathySoundCheck : MonoBehaviour
    {
        public static bool Running { get; private set; }
        void Awake() { Running = true; }

        IEnumerator Start()
        {
            if (DemoBoot.SoundCheckSfx) { yield return SfxCheck(); yield break; }
            var log = new StringBuilder();
            float t0 = Time.realtimeSinceStartup;
            void L(string s) { log.AppendLine($"[{Time.realtimeSinceStartup - t0,6:0.00}s] {s}"); Debug.Log("[Seven] soundcheck " + s); }
            var menu = GetComponent<DemoMenu>();
            var line = menu != null && menu.Bank != null ? menu.Bank.Get("s01_05_cat") : null;
            var dir = DemoBoot.CaptureDir ?? Path.Combine(Application.temporaryCachePath, "soundcheck");
            Directory.CreateDirectory(dir);
            if (line == null || line.Clip == null) { L("no clip for s01_05_cat - nothing played"); Write(dir, log); yield break; }
            var src = DialoguePlayer.MakeSource("SoundCheck_Telepathy", true, false, transform);
            src.clip = line.Clip;
            float[] l = new float[1024], r = new float[1024];
            L($"before: listenerVolume={AudioListener.volume} paused={AudioListener.pause}");
            AudioListener.pause = false; AudioListener.volume = 1f;
            L($"UNMUTED (volume 1) - playing '{line.Text}' ({line.Clip.length:0.00}s clip) with the telepathy treatment");
            src.Play();
            float peak = 0f, sumL = 0f, sumR = 0f, diff = 0f, tail = 0f;
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < line.Clip.length + 0.6f)
            {
                AudioListener.GetOutputData(l, 0); AudioListener.GetOutputData(r, 1);
                bool inTail = Time.realtimeSinceStartup - start > line.Clip.length + 0.05f;
                for (int i = 0; i < l.Length; i++)
                {
                    peak = Mathf.Max(peak, Mathf.Abs(l[i]), Mathf.Abs(r[i]));
                    sumL += l[i] * l[i]; sumR += r[i] * r[i]; diff = Mathf.Max(diff, Mathf.Abs(l[i] - r[i]));
                    if (inTail) tail = Mathf.Max(tail, Mathf.Abs(l[i]));
                }
                yield return null;
            }
            src.Stop();
            AudioListener.volume = 0f; AudioListener.pause = true;
            float played = Time.realtimeSinceStartup - start;
            L($"MUTED AGAIN after {played:0.00}s of sound");
            L($"output peak {peak:0.0000}; RMS left {Mathf.Sqrt(sumL):0.00} vs right {Mathf.Sqrt(sumR):0.00} (sums), max |L-R| {diff:0.00000} (0 = centred/mono); reverb tail peak after the dry line {tail:0.0000}");
            yield return null; yield return null;
            AudioListener.GetOutputData(l, 0);
            float after = 0f; foreach (var v in l) after = Mathf.Max(after, Mathf.Abs(v));
            L($"after mute: output peak {after:0.000000}, listenerVolume={AudioListener.volume}");
            Write(dir, log);
        }

        /// <summary>
        /// Round 7: the short unmuted SFX check (-soundcheck sfx): plays a few of the new sound effects one after
        /// another (each cut at <see cref="SfxSeconds"/>), measures each one's output peak, mutes again, measures 0.
        /// </summary>
        public static readonly string[] SfxIds = { "ledge_grab", "land_soft", "land_hard", "climb_scuff", "slab_lift" };   // Round 9: the new traversal SFX (Round 7 set: laser_power_down, release_click, energy_point, tractor_beam, menu_confirm)
        const float SfxSeconds = 1.2f;
        IEnumerator SfxCheck()
        {
            var log = new StringBuilder();
            float t0 = Time.realtimeSinceStartup;
            void L(string s) { log.AppendLine($"[{Time.realtimeSinceStartup - t0,6:0.00}s] {s}"); Debug.Log("[Seven] soundcheck " + s); }
            var dir = DemoBoot.CaptureDir ?? Path.Combine(Application.temporaryCachePath, "soundcheck");
            Directory.CreateDirectory(dir);
            float[] buf = new float[1024];
            L($"before: listenerVolume={AudioListener.volume} paused={AudioListener.pause}");
            AudioListener.pause = false; AudioListener.volume = 1f;
            L($"UNMUTED (volume 1) - {SfxIds.Length} SFX, each cut after {SfxSeconds:0.0}s, through the SFX bus (gain {AudioKit.Gain(AudioKit.Bus.Sfx):0.00})");
            float start = Time.realtimeSinceStartup;
            foreach (var id in SfxIds)
            {
                var src = AudioKit.Play(id);
                if (src == null) { L($"  {id}: no file - skipped"); continue; }
                float peak = 0f, s0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - s0 < Mathf.Min(SfxSeconds, src.clip.length))
                {
                    AudioListener.GetOutputData(buf, 0);
                    foreach (var v in buf) peak = Mathf.Max(peak, Mathf.Abs(v));
                    yield return null;
                }
                if (src != null) src.Stop();
                L($"  {id}: clip {src.clip.length:0.00}s, played {Time.realtimeSinceStartup - s0:0.00}s, output peak {peak:0.0000}");
            }
            AudioListener.volume = 0f; AudioListener.pause = true;
            L($"MUTED AGAIN after {Time.realtimeSinceStartup - start:0.00}s of sound in total");
            yield return null; yield return null;
            AudioListener.GetOutputData(buf, 0);
            float after = 0f; foreach (var v in buf) after = Mathf.Max(after, Mathf.Abs(v));
            L($"after mute: output peak {after:0.000000}, listenerVolume={AudioListener.volume}");
            Write(dir, log);
        }

        static void Write(string dir, StringBuilder log)
        {
            File.WriteAllText(Path.Combine(dir, "soundcheck_log.txt"), log.ToString());
            Application.Quit();
        }
    }
}
