"""QF41: the six Cat-a-tron commercials' sound (Dan's 12 s videos in Art/Video/catatron/, sound removed).

Usage (repo root): python Tools/Audio/make_catatron_ads.py [--only zoomies,bowl] [--force] [--mixonly]
Key: ELEVENLABS_API_KEY only (never printed or written anywhere).

For each ad:
  1. its OWN voice, designed with the ElevenLabs Voice Design API from Dan's description and saved to his library as Ad_<Name>
     (looked up by name first: never designed twice, never a story voice);
  2. the scripted lines (Eleven v3 with the line's delivery tags; multilingual v2 if v3 refuses), each placed at its start time;
  3. the sound effects (ElevenLabs Sound Effects) at their times;
  4. a 12 s music bed (ElevenLabs Music) from Dan's prompt;
  5. the mix (ffmpeg): music under the voice (side-chain ducked ~9 dB while anyone speaks), effects at their times, loudness
     normalised to -16 LUFS, exactly the video's length. A line too long for its slot is sped up (at most x1.15) and logged.
Parts: Art/Video/catatron/audio/<ad>/ ; mix: Art/Video/catatron/<ad>.ogg (beside the video) + a copy in
Unity/Assets/Resources/Catatron/<ad>.ogg (the game loads it from there). Report: Art/Video/catatron/audio/ads_report.txt.
"""
import base64, json, os, re, shutil, subprocess, sys, time, urllib.request, urllib.error

ROOT = os.getcwd()
KEY = os.environ.get("ELEVENLABS_API_KEY", "").strip()
API = "https://api.elevenlabs.io/v1"
VID = os.path.join(ROOT, "Art", "Video", "catatron")
RES = os.path.join(ROOT, "Unity", "Assets", "Resources", "Catatron")
LEN = 12.0

ADS = [
    dict(key="zoomies", file="ad_midnight_zoomies", voice="Ad_Zoomies",
         desc="A hyped-up male sports-promo announcer, booming, gravelly, over-the-top monster-truck energy.",
         lines=[(0.0, "Three A.M. The house is asleep.", "[low, dramatic]"), (3.0, "One cat... is not.", "[building]"),
                (5.0, "MIDNIGHT ZOOMIES!", "[shouting]"), (8.0, "Zero reason. Maximum speed.", "[intense]"),
                (10.0, "Every night, in a hallway near you.", "[booming]")],
         sfx=[(0.0, "a loud clock ticking in a silent house at night", 3.0), (5.2, "a cat's paws thundering down a wooden hallway at full speed", 2.5),
              (7.6, "a cat skidding on a wooden floor and a soft thud into a wall", 1.2), (8.6, "a table lamp wobbling and rattling, then settling", 1.4)],
         music="punchy sports promo sting, driving drums, brass hits, comedic tension, 12 seconds"),
    dict(key="bowl", file="ad_empty_bowl_crisis", voice="Ad_Bowl",
         desc="A gentle, sorrowful middle-aged British woman, soft charity-appeal delivery, earnest and slightly over-sentimental.",
         lines=[(0.0, "Every day, cats like Whiskers face a terrible truth.", "[sorrowful]"), (4.0, "Their bowl... is half empty.", "[soft, pained]"),
                (6.0, "The bottom... is showing.", "[barely holding back tears]"), (9.0, "No cat should see the bottom. Please. Give today.", "[earnest]")],
         sfx=[(5.0, "one small sad meow from a cat", 1.2), (8.0, "a soft sad sigh", 1.0)],
         music="melancholy charity-appeal solo piano, slow, overly sentimental, 12 seconds"),
    dict(key="tuna", file="ad_eau_de_tuna", voice="Ad_Tuna",
         desc="A sultry, breathy French-accented woman, slow luxury perfume-ad whisper, elegant and seductive, a little absurd.",
         lines=[(0.0, "Eau de Tuna.", "[French accent] [whispers]"), (2.0, "For the cat who has everything... except tuna.", "[French accent] [sultry]"),
                (6.0, "A whisper of the sea. A hint of the can opener.", "[French accent] [breathy]"), (10.0, "Eau de Tuna. Irresistible.", "[French accent] [whispers]")],
         sfx=[(8.0, "a single elegant perfume bottle spritz", 0.8), (10.6, "a chorus of cats purring in contented swoon", 1.4)],
         music="dreamy luxury perfume commercial, harp glissando, soft strings, breathy and elegant, 12 seconds"),
    dict(key="gravity", file="ad_gravity_championship", voice="Ad_Gravity",
         desc="An older refined British male golf commentator, hushed reverent whisper that can explode into a delighted shout.",
         lines=[(0.0, "The Gravity Testing Championship.", "[hushed]"), (3.0, "The challenger approaches the glass... a gentle nudge...", "[whispering, reverent]"),
                (6.0, "closer...", "[whispering, tense]"), (8.0, "AND IT'S OVER THE EDGE! Gravity confirmed!", "[shouting with delight]"),
                (11.0, "Tonight, on the Cat-a-tron.", "[calm]")],
         sfx=[(3.0, "a drinking glass sliding slowly across a wooden table", 3.0), (0.0, "a hushed golf tournament crowd murmuring quietly", 3.0),
              (7.9, "a drinking glass falling and shattering on a hard floor", 1.0), (8.4, "a huge stadium crowd roaring and cheering", 3.0)],
         music="hushed suspense strings building to a triumphant sports fanfare, 12 seconds"),
    dict(key="birds", file="ad_bird_channel", voice="Ad_Birds",
         desc="A calm, soothing male nature-documentary narrator, warm and slow, gently amused.",
         lines=[(0.0, "The Bird Channel.", "[calm]"), (2.0, "Birds. On a branch. Doing... bird things.", "[gently amused]"),
                (5.0, "No plot. No ending. Just bird.", "[warm]"), (8.0, "Now with squirrel!", "[delighted]"), (10.0, "Please don't touch the screen.", "[gently]")],
         sfx=[(0.0, "gentle birdsong, several small birds chirping in a garden", 8.0), (8.0, "a squirrel chattering excitedly", 1.5),
              (10.0, "a soft thump of a cat's paws hitting a glass screen", 0.8)],
         music="gentle nature-documentary flute and pizzicato, calm and pastoral, 12 seconds"),
    dict(key="box", file="ad_box_deluxe", voice="Ad_Box",
         desc="A deep, smooth, velvety male luxury-car-commercial voice, slow and confident, every word savoured.",
         lines=[(0.0, "Introducing... the Box Deluxe.", "[smooth]"), (3.0, "Four walls. One floor. Absolutely nothing else.", "[confident, slow]"),
                (7.0, "Engineered for sitting.", "[velvety]"), (9.0, "The Box Deluxe. If it fits... you sit.", "[savouring every word]")],
         sfx=[(3.6, "a cardboard box creaking as a cat climbs into it", 1.4), (9.0, "a deep contented cat purr", 3.0)],
         music="sleek luxury car commercial, smooth piano and soft synth pads, sophisticated, 12 seconds"),
]


def req(path, body=None, accept="application/json", method=None):
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(API + path, data=data, method=method or ("POST" if body is not None else "GET"),
                               headers={"xi-api-key": KEY, "Content-Type": "application/json", "Accept": accept})
    for attempt in range(4):
        try:
            with urllib.request.urlopen(r, timeout=300) as resp:
                return resp.read()
        except urllib.error.HTTPError as e:
            msg = e.read()[:400]
            if e.code in (429, 500, 502, 503) and attempt < 3:
                time.sleep(4 * (attempt + 1)); continue
            raise RuntimeError(f"HTTP {e.code} on {path}: {msg!r}")


def voices():
    return {v["name"].strip(): v["voice_id"] for v in json.loads(req("/voices"))["voices"]}


def design_voice(ad, have, log):
    if ad["voice"] in have:
        log(f"{ad['voice']}: already in the library (not designed again)"); return have[ad["voice"]]
    if DESIGN_BLOCKED[0]: return fallback(ad, have, log)
    sample = " ".join(t for _, t, _ in ad["lines"])
    while len(sample) < 110: sample += " " + sample
    try:
        r = json.loads(req("/text-to-voice/design", {"voice_description": ad["desc"], "text": sample[:900], "model_id": "eleven_multilingual_ttv_v2"}))
    except RuntimeError as e:
        if "voice_generation" not in str(e): raise
        DESIGN_BLOCKED[0] = True
        log("VOICE DESIGN BLOCKED: the API key lacks the 'voice_generation' permission (Dan: enable it for the key in ElevenLabs, then run this "
            "script again with --force - each ad then gets its own designed Ad_ voice). Until then: premade voices no story character uses.")
        return fallback(ad, have, log)
    prev = r["previews"][0]
    body = {"voice_name": ad["voice"], "voice_description": ad["desc"] + " (7: The Observer - Cat-a-tron commercial, QF41)", "generated_voice_id": prev["generated_voice_id"]}
    v = json.loads(req("/text-to-voice", body))
    log(f"{ad['voice']}: DESIGNED from \"{ad['desc']}\" -> voice id saved in the library ({len(r['previews'])} previews, the first kept)")
    return v["voice_id"]


# premade library voices no story character uses (checked against every Tools/Voice script and Docs/VOICES.md), closest to each
# description - used ONLY while the key cannot design voices
FALLBACK = {"zoomies": "Harry", "bowl": "Alice", "tuna": "Laura", "gravity": "George", "birds": "Roger", "box": "Brian"}
DESIGN_BLOCKED = [False]


def fallback(ad, have, log):
    name = FALLBACK[ad["key"]]
    vid = next(v for n, v in have.items() if n.split(" ")[0] == name)
    ad["fallback"] = name
    log(f"{ad['voice']}: FALLBACK premade voice '{name}' (not designed: no voice_generation permission)")
    return vid


def tts(vid, text, tag, out):
    try:
        a = req(f"/text-to-speech/{vid}?output_format=mp3_44100_128", {"text": f"{tag} {text}", "model_id": "eleven_v3",
                 "voice_settings": {"stability": 0.5, "similarity_boost": 0.85, "use_speaker_boost": True}}, "audio/mpeg")
        model = "eleven_v3"
    except RuntimeError:
        a = req(f"/text-to-speech/{vid}?output_format=mp3_44100_128", {"text": text, "model_id": "eleven_multilingual_v2",
                 "voice_settings": {"stability": 0.4, "similarity_boost": 0.85, "style": 0.5, "use_speaker_boost": True}}, "audio/mpeg")
        model = "eleven_multilingual_v2"
    open(out, "wb").write(a); return model


def trimmed_len(f):
    tmp = f + ".len.wav"
    subprocess.run(["ffmpeg", "-y", "-hide_banner", "-loglevel", "error", "-i", f, "-af", "silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.03,areverse,silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.08,areverse", tmp], check=True)
    L = dur(tmp); os.remove(tmp); return L


def fit(ad, d, vid, log):
    """QF41: re-take any line longer than its slot (the time to the next line) - v3 faster-paced twice, v2 at speed 1.15 - keep the shortest."""
    ts = [t for t, _, _ in ad["lines"]] + [LEN - 0.15]
    for i, (t0, text, tag) in enumerate(ad["lines"]):
        f = os.path.join(d, f"line_{i + 1}.mp3"); slot = ts[i + 1] - t0 - 0.1
        if not os.path.exists(f): continue
        cur = trimmed_len(f)
        if cur <= slot * 1.1: continue
        best = (cur, None)
        for k, (model, body) in enumerate([
                ("eleven_v3", {"text": f"{tag} [fast-paced] {text}", "model_id": "eleven_v3", "voice_settings": {"stability": 0.5, "similarity_boost": 0.85, "use_speaker_boost": True}}),
                ("eleven_v3", {"text": f"[fast-paced] {text.replace('...', ',')}", "model_id": "eleven_v3", "voice_settings": {"stability": 0.5, "similarity_boost": 0.85, "use_speaker_boost": True}}),
                ("eleven_multilingual_v2", {"text": text, "model_id": "eleven_multilingual_v2", "voice_settings": {"stability": 0.45, "similarity_boost": 0.85, "style": 0.45, "use_speaker_boost": True, "speed": 1.15}})]):
            alt = os.path.join(d, f"line_{i + 1}_alt{k + 1}.mp3")
            try: open(alt, "wb").write(req(f"/text-to-speech/{vid}?output_format=mp3_44100_128", body, "audio/mpeg"))
            except RuntimeError as e: log(f"  line {i + 1} retake {k + 1} failed: {e}"); continue
            L = trimmed_len(alt)
            if L < best[0]: best = (L, alt)
        if best[1]:
            shutil.copyfile(best[1], f)
            log(f"  line {i + 1}: {cur:.2f}s in a {slot:.2f}s slot -> re-taken, now {best[0]:.2f}s ({os.path.basename(best[1])})")


def dur(f):
    return float(subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", f], capture_output=True, text=True).stdout.strip() or 0)


def main():
    if not KEY: sys.exit("ELEVENLABS_API_KEY is not set - stopping.")
    only = sys.argv[sys.argv.index("--only") + 1].split(",") if "--only" in sys.argv else None
    force = "--force" in sys.argv; mixonly = "--mixonly" in sys.argv
    os.makedirs(RES, exist_ok=True)
    rep = []
    def log(s): print(s); rep.append(s)
    have = {} if mixonly else voices()
    for ad in ADS:
        if only and ad["key"] not in only: continue
        d = os.path.join(VID, "audio", ad["file"]); os.makedirs(d, exist_ok=True)
        log(f"== {ad['file']} ({ad['voice']})")
        if not mixonly:
            vid = design_voice(ad, have, log); have[ad["voice"]] = vid
            for i, (t0, text, tag) in enumerate(ad["lines"]):
                f = os.path.join(d, f"line_{i + 1}.mp3")
                if force or not os.path.exists(f): log(f"  line {i + 1} @ {t0:.1f}s [{tts(vid, text, tag, f)}] {tag} {text}")
            for i, (t0, prompt, sec) in enumerate(ad["sfx"]):
                f = os.path.join(d, f"sfx_{i + 1}.mp3")
                if force or not os.path.exists(f):
                    open(f, "wb").write(req("/sound-generation", {"text": prompt, "duration_seconds": max(0.5, min(22.0, sec)), "prompt_influence": 0.5}, "audio/mpeg"))
                    log(f"  sfx {i + 1} @ {t0:.1f}s ({sec:.1f}s) {prompt}")
            fit(ad, d, vid, log)
            f = os.path.join(d, "music.mp3")
            if force or not os.path.exists(f):
                try:
                    open(f, "wb").write(req("/music", {"prompt": ad["music"] + ", instrumental, no vocals", "music_length_ms": 12000}, "audio/mpeg"))
                    log(f"  music (ElevenLabs Music, 12 s): {ad['music']}")
                except RuntimeError as e:
                    log(f"  MUSIC FAILED ({e}) - the ad is mixed without a bed")
        mix(ad, d, log)
    rp = os.path.join(VID, "audio", "ads_report.txt")
    old = open(rp, encoding="utf-8").read() if os.path.exists(rp) and only else ""
    open(rp, "w", encoding="utf-8", newline="\n").write(old + "\n".join(rep) + "\n")


def mix(ad, d, log):
    """voice lines at their times (sped up to fit their slot if needed), effects at theirs, the bed ducked under the voice."""
    inputs, filters, vlabels, slabels = [], [], [], []
    files = []
    for i in range(len(ad["lines"])):          # each take trimmed of its leading / trailing silence (v3 pads both ends)
        src = os.path.join(d, f"line_{i + 1}.mp3"); trimmed = os.path.join(d, f"line_{i + 1}_trim.wav")
        if os.path.exists(src):
            subprocess.run(["ffmpeg", "-y", "-hide_banner", "-loglevel", "error", "-i", src, "-af",
                            "silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.03,areverse,silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.08,areverse",
                            trimmed], check=True)
        files.append(trimmed)
    lens = [dur(f) if os.path.exists(f) else 0.0 for f in files]
    def plan(tempo):
        st, e = [], 0.0
        for (t0, _, _), L in zip(ad["lines"], lens):
            s0 = max(t0, e + 0.08 if st else t0); st.append(s0); e = s0 + L / tempo
        if e > LEN - 0.1 and len(st) > 1:          # the last line may start a little early (the previous one has finished) to end in time
            prev_end = st[-2] + lens[len(st) - 2] / tempo
            st[-1] = max(prev_end + 0.08, LEN - 0.1 - lens[len(st) - 1] / tempo); e = st[-1] + lens[len(st) - 1] / tempo
        return st, e
    tempo = 1.0
    while plan(tempo)[1] > LEN - 0.1 and tempo < 1.25: tempo = round(tempo + 0.01, 2)
    starts, end = plan(tempo)
    log(f"  voice: speed x{tempo:.2f}, ends at {end:.2f}s" + ("" if end <= LEN - 0.1 else " (RUNS OVER - the last line is cut at 12 s)") + "; starts " +
        ", ".join(f"{a:.2f}" + ("" if abs(a - t0) < 0.05 else f" (script {t0:.1f})") for a, (t0, _, _) in zip(starts, ad["lines"])))
    for i, f in enumerate(files):
        if not os.path.exists(f): continue
        k = len(inputs) // 2; inputs += ["-i", f]; ms = int(starts[i] * 1000)
        filters.append(f"[{k}:a]aformat=sample_rates=44100:channel_layouts=stereo,{'atempo=%.3f,' % tempo if tempo > 1.0 else ''}adelay={ms}|{ms},apad[v{i}]")
        vlabels.append(f"[v{i}]")
    for i, (t0, prompt, sec) in enumerate(ad["sfx"]):
        f = os.path.join(d, f"sfx_{i + 1}.mp3")
        if not os.path.exists(f): continue
        k = len(inputs) // 2; inputs += ["-i", f]
        filters.append(f"[{k}:a]aformat=sample_rates=44100:channel_layouts=stereo,atrim=0:{sec + 0.4},afade=t=out:st={max(0.0, sec - 0.2)}:d=0.5,volume=0.55,adelay={int(t0 * 1000)}|{int(t0 * 1000)},apad[s{i}]")
        slabels.append(f"[s{i}]")
    music = os.path.join(d, "music.mp3"); has_m = os.path.exists(music)
    if has_m:
        k = len(inputs) // 2; inputs += ["-i", music]
        filters.append(f"[{k}:a]aformat=sample_rates=44100:channel_layouts=stereo,atrim=0:{LEN},afade=t=in:d=0.3,afade=t=out:st={LEN - 0.8}:d=0.8,volume=0.5,apad[m]")
    filters.append(f"{''.join(vlabels)}amix=inputs={len(vlabels)}:normalize=0,asplit=2[vo][vk]")
    if has_m:
        filters.append("[m][vk]sidechaincompress=threshold=0.03:ratio=6:attack=20:release=350[md]")
        bed = "[md]"
    else:
        filters.append("[vk]anullsink"); bed = ""
    n = 1 + len(slabels) + (1 if has_m else 0)
    filters.append(f"[vo]{''.join(slabels)}{bed}amix=inputs={n}:normalize=0,atrim=0:{LEN:.3f},loudnorm=I=-16:TP=-1.5:LRA=11,aresample=44100[out]")
    out = os.path.join(VID, ad["file"] + ".ogg")
    cmd = ["ffmpeg", "-y", "-hide_banner", "-loglevel", "error"] + inputs + ["-filter_complex", ";".join(filters), "-map", "[out]", "-c:a", "libvorbis", "-q:a", "6", "-t", f"{LEN:.3f}", out]
    p = subprocess.run(cmd, capture_output=True, text=True)
    if p.returncode != 0: log(f"  MIX FAILED: {p.stderr[-600:]}"); return
    shutil.copyfile(out, os.path.join(RES, ad["file"] + ".ogg"))
    log(f"  mixed -> {os.path.relpath(out, ROOT)} ({dur(out):.2f}s; voice {len(vlabels)} lines, effects {len(slabels)}, bed {'yes' if has_m else 'NO'}) + Resources/Catatron copy")


main()
