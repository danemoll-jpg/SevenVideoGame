"""Round 13: the conversation reaction bank (laughs, chuckles, "hmm", "mm-hmm", sighs, purrs) with ElevenLabs.

Usage (repo root): python Tools/Voice/generate_reactions.py [--force] [--only <who>_<kind>[_<n>] ...] [--check] [--level]
Key: ELEVENLABS_API_KEY environment variable only (never printed or written anywhere).
Voices by name in Dan's library: "Seven" (eleven_v3 + audio tags) and "Cat" (eleven_v3 too, so his reactions match his
v3 lines; if v3 fails for him, his multilingual v2 settings with the plain vocalisation, no tags).
Writes Unity/Assets/Resources/Reactions/<who>_<kind>_<n>.mp3 (3 takes per kind, each with its own wording so repeats
differ), trims leading / trailing silence with ffmpeg (on PATH) and re-takes any take that comes out empty or longer
than 2.5 s (up to 4 tries). --check = only measure the existing files; --level = level the
existing takes to the same mean loudness (new takes are levelled when made). Placement data: reactions.json (same folder);
runtime: Unity/Assets/Scripts/Runtime/Reactions.cs + DialoguePlayer.cs. Docs/REACTIONS.md lists the bank.
"""
import json, os, shutil, subprocess, sys, tempfile, urllib.request, urllib.error

ROOT = os.getcwd()
OUT = os.path.join(ROOT, "Unity", "Assets", "Resources", "Reactions")
API = "https://api.elevenlabs.io/v1"
MAX_LEN, MIN_LEN, TRIES = 2.5, 0.12, 4
TARGET_MEAN, PEAK_CEIL = -19.0, -1.5   # dB

# (who, kind) -> three takes (Eleven v3 audio tag + a tiny vocalisation; text with no words at all can come back empty)
TAKES = {
    ("seven", "laugh_soft"): ["[laughs softly] Ha.", "[a soft, warm laugh] Heh heh.", "[quietly laughing] Hah."],
    ("seven", "giggle"): ["[giggles] Hehe.", "[a small giggle] Hihi.", "[giggling softly] Heh-heh."],
    ("seven", "chuckle"): ["[chuckles quietly] Heh.", "[a low, quiet chuckle] Hm-hm.", "[chuckling] Heh heh."],
    ("seven", "nose_laugh"): ["[amused exhale through the nose] Hm.", "[a short amused breath] Hmph.", "[snorts softly, amused] Hm-hm."],
    ("seven", "hmm"): ["[thinking] Hmm.", "[thoughtfully] Hmmm.", "[considering] Hm-mm."],
    ("seven", "mmhmm"): ["[agreeing] Mm-hm.", "[softly agreeing] Mhm.", "[nodding] Mm-hmm."],
    ("seven", "hm_q"): ["[questioning] Hm?", "[curious] Hm?", "[puzzled, softly] Mm?"],
    ("seven", "sigh"): ["[small sigh] Hhh.", "[sighs softly] Haah.", "[a light, rueful sigh] Hhhh."],
    ("seven", "oh"): ["[softly, surprised] Oh.", "[quietly] Oh.", "[gently surprised] Oh?"],
    ("cat", "chuckle"): ["[dry low chuckle] Heh.", "[a dry, quiet chuckle] Heh heh.", "[chuckles drily] Hm-hm."],
    ("cat", "purrhum"): ["[amused, a purring hum] Mmm.", "[a low purring hum] Mmmrr.", "[pleased hum] Mm-mmm."],
    ("cat", "hm"): ["[thinking] Hm.", "[noncommittal] Hm.", "[quietly considering] Hmm."],
    ("cat", "mm"): ["[softly] Mm.", "[agreeing] Mm.", "[a low hum] Mmm."],
    ("cat", "purr"): ["[a short pleased purr] Prrr.", "[purrs contentedly] Rrrr.", "[a soft purr] Mrrr."],
    ("cat", "sigh"): ["[long-suffering sigh] Haaah.", "[sighs wearily] Hhhh.", "[a patient sigh] Haah."],
}
# v2 fallback for the cat: no tags (v2 would read them aloud), his Chapter 1 settings
V2_CAT = ("eleven_multilingual_v2", {"stability": 0.62, "similarity_boost": 0.8, "style": 0.2, "use_speaker_boost": True, "speed": 0.92})
V3 = {"stability": 0.5, "similarity_boost": 0.8, "use_speaker_boost": True}


def ffmpeg(name):
    p = shutil.which(name)
    if not p: sys.exit(f"{name} is not on PATH - stopping (needed to trim and measure).")
    return p


FF, FP = ffmpeg("ffmpeg"), ffmpeg("ffprobe")


def length(path):
    r = subprocess.run([FP, "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", path], capture_output=True, text=True)
    try: return float(r.stdout.strip())
    except ValueError: return 0.0


def levels(path):
    """(mean dB, max dB) from ffmpeg's volumedetect."""
    r = subprocess.run([FF, "-hide_banner", "-i", path, "-af", "volumedetect", "-f", "null", "-"], capture_output=True, text=True)
    mean = mx = -91.0
    for ln in r.stderr.splitlines():
        if "mean_volume:" in ln: mean = float(ln.split("mean_volume:")[1].split()[0])
        if "max_volume:" in ln: mx = float(ln.split("max_volume:")[1].split()[0])
    return mean, mx


def gain_for(path):
    """Level every take to about the same mean loudness (the game plays them quiet; this keeps them even)."""
    mean, mx = levels(path)
    return min(TARGET_MEAN - mean, PEAK_CEIL - mx) if mean > -90 else 0.0


def trim(src, dst):
    """Cut leading / trailing silence (below -45 dB), keep 30 ms of air at the start, a 60 ms fade at the end,
    and level the take to TARGET_MEAN (peaks stay under PEAK_CEIL)."""
    sil = "silenceremove=start_periods=1:start_duration=0.02:start_threshold=-45dB:start_silence=0.03"
    tmp = dst + ".tmp.mp3"
    subprocess.run([FF, "-y", "-v", "error", "-i", src, "-af", f"{sil},areverse,{sil},areverse", "-ar", "44100",
                    "-ac", "1", "-c:a", "libmp3lame", "-b:a", "128k", tmp], check=True)
    n = length(tmp)
    if n > 0.15:   # short fade-out on the tail so the cut never clicks
        g = gain_for(tmp)
        subprocess.run([FF, "-y", "-v", "error", "-i", tmp, "-af", f"volume={g:.2f}dB,afade=t=out:st={max(0.0, n - 0.06):.3f}:d=0.06",
                        "-c:a", "libmp3lame", "-b:a", "128k", dst], check=True)
        os.remove(tmp)
    else: os.replace(tmp, dst)


def relevel(path):
    """--level: level an existing take in place (for files made before levelling existed)."""
    g = gain_for(path)
    if abs(g) < 0.5: return g
    tmp = path + ".tmp.mp3"
    subprocess.run([FF, "-y", "-v", "error", "-i", path, "-af", f"volume={g:.2f}dB", "-c:a", "libmp3lame", "-b:a", "128k", tmp], check=True)
    os.replace(tmp, path)
    return g


def main():
    key = os.environ.get("ELEVENLABS_API_KEY", "").strip()
    check = "--check" in sys.argv
    if "--level" in sys.argv:
        for f in sorted(os.listdir(OUT)):
            if f.endswith(".mp3"):
                g = relevel(os.path.join(OUT, f)); m, x = levels(os.path.join(OUT, f))
                print(f"  {f:26s} gain {g:+5.1f} dB -> mean {m:6.1f} dB, max {x:5.1f} dB")
        return
    if not key and not check: sys.exit("ELEVENLABS_API_KEY is not set - stopping.")
    force = "--force" in sys.argv
    only = [a for a in sys.argv[sys.argv.index("--only") + 1:] if not a.startswith("--")] if "--only" in sys.argv else None
    os.makedirs(OUT, exist_ok=True)

    def req(path, body=None, accept="application/json"):
        data = json.dumps(body).encode() if body is not None else None
        r = urllib.request.Request(API + path, data=data, method="POST" if body is not None else "GET",
                                   headers={"xi-api-key": key, "Content-Type": "application/json", "Accept": accept})
        with urllib.request.urlopen(r, timeout=180) as resp:
            return resp.read()

    voices = {}
    if not check:
        voices = {v["name"].strip().lower(): v["voice_id"] for v in json.loads(req("/voices"))["voices"]}
        for n in ("seven", "cat"):
            if n not in voices: sys.exit(f'voice "{n}" not found in the ElevenLabs library - stopping.')
    cat_v3 = True
    report = []
    tmpdir = tempfile.mkdtemp(prefix="seven_reactions_")
    try:
        for (who, kind), texts in TAKES.items():
            for i, text in enumerate(texts, 1):
                name = f"{who}_{kind}_{i}"
                if only and not any(name == o or name.startswith(o + "_") for o in only): continue
                path = os.path.join(OUT, name + ".mp3")
                if check or (os.path.exists(path) and not force):
                    if os.path.exists(path): report.append((name, "kept", length(path), ""))
                    continue
                got, model = 0.0, ""
                for attempt in range(1, TRIES + 1):
                    if who == "cat" and not cat_v3: model, st, t = V2_CAT[0], V2_CAT[1], text.split("] ", 1)[-1]
                    else: model, st, t = "eleven_v3", V3, text
                    body = {"text": t, "model_id": model, "voice_settings": st}
                    try:
                        audio = req(f"/text-to-speech/{voices[who]}?output_format=mp3_44100_128", body, "audio/mpeg")
                    except urllib.error.HTTPError as e:
                        if who == "cat" and cat_v3:
                            print(f"{name}: eleven_v3 failed for the cat (HTTP {e.code}) - falling back to multilingual v2")
                            cat_v3 = False; continue
                        raise
                    raw = os.path.join(tmpdir, f"{name}_{attempt}.mp3")
                    open(raw, "wb").write(audio)
                    trim(raw, path)
                    got = length(path)
                    ok = MIN_LEN <= got <= MAX_LEN
                    print(f"{name}  try {attempt}  {model}  raw {length(raw):.2f} s -> {got:.2f} s  {'ok' if ok else 'RETAKE'}  {t}")
                    if ok: break
                report.append((name, model, got, "" if MIN_LEN <= got <= MAX_LEN else "OUT OF RANGE"))
    finally:
        shutil.rmtree(tmpdir, ignore_errors=True)
    print("\nbank:")
    for name, model, n, flag in report: print(f"  {name:22s} {n:5.2f} s  {model}  {flag}")
    bad = [r for r in report if r[3]]
    print(f"done: {len(report)} takes, {len(bad)} out of range {[b[0] for b in bad]}")


if __name__ == "__main__":
    main()
