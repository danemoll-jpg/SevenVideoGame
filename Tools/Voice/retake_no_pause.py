"""QF8: re-take a 7 line that has an unwanted pause inside a phrase, and keep the take with the shortest one.

Usage (repo root):
  python Tools/Voice/retake_no_pause.py <line id> <word before the pause> [--takes 6]
e.g. python Tools/Voice/retake_no_pause.py s01_02_7 Before
Key: ELEVENLABS_API_KEY environment variable only (never printed or written anywhere).

Each take uses the ElevenLabs "with-timestamps" endpoint (character alignment), so the gap after the word is
measured exactly; the whole take is also scanned with ffmpeg silencedetect (-40 dB). A take is acceptable when the
gap after the word is <= 150 ms and no silence inside the sentence that holds the word is longer than 150 ms.
Variants tried in turn (the text itself never changes; the subtitle stays as written):
  v3 with a short calm tag, v3 without tags, v2 (multilingual) with the neighbouring lines as context.
The best take replaces Unity/Assets/Audio/Voice/<dir>/<id>.mp3 (the old one is kept as <id>_qf7.mp3 in
Docs/Previews/qf8/voice/); every take and its numbers go to Docs/Previews/qf8/voice/<id>_takes.txt.
"""
import base64, json, os, re, subprocess, sys, urllib.request

ROOT = os.getcwd()
KEY = os.environ.get("ELEVENLABS_API_KEY", "").strip()
if not KEY:
    sys.exit("ELEVENLABS_API_KEY is not set - stopping.")
API = "https://api.elevenlabs.io/v1"
OUT = os.path.join(ROOT, "Docs", "Previews", "qf8", "voice")


def req(path, body=None):
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(API + path, data=data, method="POST" if body is not None else "GET",
                               headers={"xi-api-key": KEY, "Content-Type": "application/json", "Accept": "application/json"})
    with urllib.request.urlopen(r, timeout=180) as resp:
        return json.loads(resp.read())


def silences(mp3):
    p = subprocess.run(["ffmpeg", "-hide_banner", "-i", mp3, "-af", "silencedetect=noise=-40dB:d=0.1", "-f", "null", "-"],
                       capture_output=True, text=True)
    st, out = None, []
    for line in p.stderr.splitlines():
        m = re.search(r"silence_start: ([\d.]+)", line)
        if m: st = float(m.group(1))
        m = re.search(r"silence_end: ([\d.]+) \| silence_duration: ([\d.]+)", line)
        if m and st is not None: out.append((st, float(m.group(1)))); st = None
    return out


def main():
    lid, word = sys.argv[1], sys.argv[2]
    takes = int(sys.argv[sys.argv.index("--takes") + 1]) if "--takes" in sys.argv else 6
    lines = json.load(open(os.path.join(ROOT, "Unity/Assets/Audio/Voice/voice_lines.json"), encoding="utf-8"))
    lines = lines if isinstance(lines, list) else lines["lines"]
    k = next(i for i, l in enumerate(lines) if l["id"] == lid)
    line = lines[k]; text = line["text"]
    voices = {v["name"].strip().lower(): v["voice_id"] for v in req("/voices")["voices"]}
    vid = voices["seven"]
    # the sentence that holds the word (gaps inside it count; the break between sentences does not)
    wi = text.index(word); s0 = text.rfind(".", 0, wi) + 1; s1 = text.find(".", wi); s1 = len(text) if s1 < 0 else s1
    variants = [("v3 [calm]", "eleven_v3", "[calm] " + text, {"stability": 0.5, "similarity_boost": 0.8, "use_speaker_boost": True}),
                ("v3 no tag", "eleven_v3", text, {"stability": 0.5, "similarity_boost": 0.8, "use_speaker_boost": True}),
                ("v2 context", "eleven_multilingual_v2", text, {"stability": 0.32, "similarity_boost": 0.8, "style": 0.45, "use_speaker_boost": True, "speed": 0.95})]
    os.makedirs(OUT, exist_ok=True)
    log = [f"{lid}: \"{text}\" - pause after \"{word}\"; acceptable <= 0.150 s"]
    best = None
    for n in range(takes):
        name, model, t, st = variants[n % len(variants)]
        body = {"text": t, "model_id": model, "voice_settings": st}
        if model != "eleven_v3":
            same = [l for l in lines if l["scene"] == line["scene"]]; j = same.index(line)
            if j > 0: body["previous_text"] = same[j - 1]["text"]
            if j + 1 < len(same): body["next_text"] = same[j + 1]["text"]
        r = req(f"/text-to-speech/{vid}/with-timestamps?output_format=mp3_44100_128", body)
        audio = base64.b64decode(r["audio_base64"])
        al = r.get("normalized_alignment") or r.get("alignment")
        chars = "".join(al["characters"]); starts, ends = al["character_start_times_seconds"], al["character_end_times_seconds"]
        off = chars.find(text[s0:s1].strip()[:12]); off = 0 if off < 0 else off
        w0 = chars.find(word, off); gap = -1.0
        if w0 >= 0:
            we = w0 + len(word) - 1
            nx = we + 1
            while nx < len(chars) and not chars[nx].isalnum(): nx += 1
            if nx < len(chars): gap = starts[nx] - ends[we]
        f = os.path.join(OUT, f"{lid}_take{n + 1}.mp3"); open(f, "wb").write(audio)
        # silences inside the word's sentence (from its first to its last character)
        a0 = chars.find(text[s0:s1].strip()[:6], 0); a1 = a0 + len(text[s0:s1].strip()) - 1
        t0 = starts[a0] if a0 >= 0 else 0.0; t1 = ends[min(a1, len(ends) - 1)] if a0 >= 0 else 99.0
        inner = [(a, b) for a, b in silences(f) if a > t0 + 0.02 and b < t1 - 0.02]
        worst = max([b - a for a, b in inner] + [0.0])
        ok = 0 <= gap <= 0.15 and worst <= 0.15
        score = max(gap, worst)
        log.append(f"take {n + 1} ({name}): gap after \"{word}\" {gap:.3f} s (alignment), longest silence inside the sentence {worst:.3f} s (ffmpeg -40 dB) -> {'OK' if ok else 'no'}")
        print(log[-1])
        if ok and (best is None or score < best[0]): best = (score, f, n + 1, name)
    if best is None:
        log.append("no acceptable take - the old file is kept")
    else:
        dst = os.path.join(ROOT, "Unity/Assets/Audio/Voice", line["dir"], lid + ".mp3")
        keep = os.path.join(OUT, lid + "_qf7.mp3")
        if os.path.exists(dst) and not os.path.exists(keep): os.replace(dst, keep)
        open(dst, "wb").write(open(best[1], "rb").read())
        log.append(f"chosen: take {best[2]} ({best[3]}), worst internal gap {best[0]:.3f} s -> {os.path.relpath(dst, ROOT)}")
    open(os.path.join(OUT, f"{lid}_takes.txt"), "w", encoding="utf-8").write("\n".join(log) + "\n")
    print(log[-1])


if __name__ == "__main__":
    main()
