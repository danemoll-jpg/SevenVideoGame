"""QF40: make several takes of one voice line for Dan to choose from, measure them, and install the most energetic.

Usage (repo root):
  python Tools/Voice/make_takes.py s01_02_7            (Chapter 1 line ids from Unity/Assets/Audio/Voice/voice_lines.json)
  python Tools/Voice/make_takes.py s07_04_7
  python Tools/Voice/make_takes.py s08_05_7
Key: ELEVENLABS_API_KEY environment variable only (never printed or written anywhere).

Takes go to Docs/Previews/qf40/<id>_takes/take_<n>.mp3 + takes.txt (the text / tags sent and the numbers). Each take uses
the "with-timestamps" endpoint (character alignment) so every word gap is measured exactly. Numbers per take:
  - gaps: the silence between each pair of words (ms); RULES per line say which gap must be long / short;
  - loudness: integrated LUFS + loudness range (ffmpeg ebur128);
  - pitch movement: the f0 contour (numpy autocorrelation over voiced 40 ms frames) -> range + std in semitones;
  - laugh lines: the voiced time before the first word (a real laugh >= 0.6 s, not a breath).
Energy score = pitch std + 0.5 * pitch range / 12 + 0.25 * LRA (+ laugh seconds for laugh lines); takes that break a gap
rule are never chosen. The chosen take is copied over the game's file (Unity/Assets/Audio/Voice/<dir>/<id>.mp3).
"""
import base64, json, os, re, shutil, subprocess, sys, urllib.request
import numpy as np

ROOT = os.getcwd()
KEY = os.environ.get("ELEVENLABS_API_KEY", "").strip()
if not KEY:
    sys.exit("ELEVENLABS_API_KEY is not set - stopping.")
API = "https://api.elevenlabs.io/v1"
V3 = {"stability": 0.0, "similarity_boost": 0.8, "use_speaker_boost": True}       # v3 "creative": most expressive
V3N = {"stability": 0.5, "similarity_boost": 0.8, "use_speaker_boost": True}      # v3 "natural"

# Per line: the variants (label, text sent, settings) and the gap rules (word before the gap, min ms, max ms).
TAKES = {
    "s01_02_7": dict(variants=[
        ("urgent hushed, ellipsis", "[urgent] [whispers] Go on! ... Before somebody sees you!", V3),
        ("urgent emphatic, ellipsis", "[urgent] [emphatic] Go on! ... Before somebody sees you!", V3),
        ("hushed shooing, dash", "[hushed] [urgently] Go on! [short pause] Before somebody sees you!", V3),
        ("urgent, natural", "[urgent] [hushed] Go on! ... Before somebody sees you!", V3N),
    ], gaps=[("on!", 350, 800), ("Before", 0, 150), ("somebody", 0, 150)]),
    "s07_04_7": dict(variants=[
        ("laughs", "[laughs] No.", V3),
        ("laughs heartily", "[laughs heartily] [smiling] No.", V3),
        ("bursts out laughing", "[bursts out laughing] ... [smiling] No.", V3),
    ], gaps=[], laugh=True),
    "s08_05_7": dict(variants=[
        ("laughs", "[laughs] Get in!", V3),
        ("laughs, urgent", "[laughs] [urgent] Get in!", V3),
        ("short laugh, shouting", "[short laugh] [shouting] Get in!", V3),
    ], gaps=[], laugh=True),
}


def req(path, body=None):
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(API + path, data=data, method="POST" if body is not None else "GET",
                               headers={"xi-api-key": KEY, "Content-Type": "application/json", "Accept": "application/json"})
    with urllib.request.urlopen(r, timeout=180) as resp:
        return json.loads(resp.read())


def words(al, text):
    """alignment -> [(word, start, end)] over the spoken text (tags in [...] removed by the API's own alignment)."""
    chars, st, en = al["characters"], al["character_start_times_seconds"], al["character_end_times_seconds"]
    out, cur, s0, e0, inside = [], "", None, None, False
    for c, a, b in zip(chars, st, en):
        if c == "[": inside = True
        if inside:
            if c == "]": inside = False
            continue
        if c.isspace():
            if cur.strip(".") : out.append((cur, s0, e0))
            cur, s0 = "", None
            continue
        if s0 is None: s0 = a
        cur += c; e0 = b
    if cur.strip("."): out.append((cur, s0, e0))
    return [w for w in out if re.search(r"\w", w[0])]


def pcm(mp3):
    p = subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-i", mp3, "-ac", "1", "-ar", "16000", "-f", "f32le", "-"],
                       capture_output=True)
    return np.frombuffer(p.stdout, dtype=np.float32)


def pitch(x, sr=16000):
    f = []
    n = int(0.04 * sr)
    for i in range(0, len(x) - n, n // 2):
        w = x[i:i + n] * np.hanning(n)
        if np.sqrt(np.mean(w * w)) < 0.02: continue
        ac = np.correlate(w, w, "full")[n - 1:]
        lo, hi = sr // 400, sr // 80
        k = lo + int(np.argmax(ac[lo:hi]))
        if ac[k] > 0.35 * ac[0]: f.append(sr / k)
    if len(f) < 4: return 0.0, 0.0
    st = 12 * np.log2(np.array(f) / np.median(f))
    st = st[np.abs(st) < 12]
    return float(np.percentile(st, 95) - np.percentile(st, 5)), float(np.std(st))


def loud(mp3):
    p = subprocess.run(["ffmpeg", "-hide_banner", "-i", mp3, "-af", "ebur128", "-f", "null", "-"], capture_output=True, text=True)
    I = re.findall(r"I:\s+(-?[\d.]+) LUFS", p.stderr); L = re.findall(r"LRA:\s+([\d.]+) LU", p.stderr)
    return (float(I[-1]) if I else 0.0), (float(L[-1]) if L else 0.0)


def voiced_before(x, t, sr=16000):
    """seconds of voiced sound (40 ms frames above -35 dBFS) before time t"""
    n = int(0.04 * sr); k = 0
    for i in range(0, min(len(x), int(t * sr)) - n, n):
        if np.sqrt(np.mean(x[i:i + n] ** 2)) > 0.018: k += 1
    return k * 0.04


def main():
    lid = sys.argv[1]
    cfg = TAKES[lid]
    lines = json.load(open(os.path.join(ROOT, "Unity/Assets/Audio/Voice/voice_lines.json"), encoding="utf-8"))["lines"]
    line = next(l for l in lines if l["id"] == lid)
    vid = {v["name"].strip().lower(): v["voice_id"] for v in req("/voices")["voices"]}["seven"]
    out = os.path.join(ROOT, "Docs", "Previews", "qf40", lid + "_takes"); os.makedirs(out, exist_ok=True)
    rep = [f"# Takes for {lid}: \"{line['text']}\" (subtitle unchanged)", ""]
    best = None
    for n, (label, text, settings) in enumerate(cfg["variants"], 1):
        r = req(f"/text-to-speech/{vid}/with-timestamps?output_format=mp3_44100_128",
                {"text": text, "model_id": "eleven_v3", "voice_settings": settings})
        mp3 = os.path.join(out, f"take_{n}.mp3")
        open(mp3, "wb").write(base64.b64decode(r["audio_base64"]))
        ws = words(r.get("alignment") or r.get("normalized_alignment"), text)
        gaps = {ws[i][0]: int(round((ws[i + 1][1] - ws[i][2]) * 1000)) for i in range(len(ws) - 1)}
        ok = True; notes = []
        for w, lo, hi in cfg["gaps"]:
            g = next((v for k, v in gaps.items() if k.lower().startswith(w.lower().rstrip("!"))), None)
            good = g is not None and lo <= g <= hi
            ok &= good; notes.append(f"gap after '{w}' {g} ms ({'ok' if good else 'OUT'}: {lo}-{hi})")
        x = pcm(mp3)
        rng, sd = pitch(x)
        I, lra = loud(mp3)
        laugh = voiced_before(x, ws[0][1]) if cfg.get("laugh") and ws else 0.0
        if cfg.get("laugh"):
            good = laugh >= 0.6; ok &= good; notes.append(f"laugh {laugh:.2f} s voiced before '{ws[0][0] if ws else '?'}' ({'real laugh' if good else 'TOO SHORT'})")
        score = sd + 0.5 * rng / 12 + 0.25 * lra + laugh
        dur = len(x) / 16000
        rep += [f"## take_{n}.mp3 - {label}", f"- sent: `{text}`", f"- length {dur:.2f} s; words: " + ", ".join(f"{w} {a:.2f}-{b:.2f}" for w, a, b in ws),
                f"- " + "; ".join(notes) if notes else "- (no gap rules)",
                f"- loudness {I:.1f} LUFS, LRA {lra:.1f} LU; pitch range {rng:.1f} st, std {sd:.2f} st; energy score {score:.2f}" + ("" if ok else "  (breaks a rule: not chosen)"), ""]
        print(f"{lid} take {n} [{label}] score {score:.2f} {'ok' if ok else 'rule broken'} " + "; ".join(notes))
        if ok and (best is None or score > best[0]): best = (score, n, mp3)
    if best is None:
        rep.append("**No take passed every rule - the game keeps its current file.**")
    else:
        dst = os.path.join(ROOT, "Unity/Assets/Audio/Voice", line["dir"], lid + ".mp3")
        shutil.copyfile(best[2], dst)
        rep.append(f"**In the game now: take_{best[1]}** (the most energetic take that keeps every rule). To use another: copy it over `{os.path.relpath(dst, ROOT)}` and rebuild.")
    open(os.path.join(out, "takes.txt"), "w", encoding="utf-8", newline="\n").write("\n".join(rep) + "\n")
    print("\n".join(rep[-1:]))


main()
