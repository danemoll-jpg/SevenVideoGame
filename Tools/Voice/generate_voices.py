"""Generate Chapter 1 voice files with ElevenLabs.

Usage (repo root):
  python Tools/Voice/generate_voices.py [--speaker seven|cat] [--model v3|v2] [--force] [--only <id>...]
Key: ELEVENLABS_API_KEY environment variable only (never printed or written anywhere).
Voices are looked up by name in Dan's library: "Seven" and "Cat".
Reads Unity/Assets/Audio/Voice/voice_lines.json, writes Unity/Assets/Audio/Voice/<dir>/<id>.mp3 (mp3_44100_128).
Existing files are skipped unless --force. --only takes ids and must come last.

Models / settings (Round 7):
- 7 (Seven): Eleven v3 ("eleven_v3", default for --speaker seven). The line's "delivery" note
  (Tools/Voice/seven_delivery.json, merged into voice_lines.json) is sent as audio tags in front of the text,
  e.g. "[curious] Who said that?" — tags are performance only; the subtitle text in the JSON stays clean.
  v3 stability is a 3-step setting (0.0 creative / 0.5 natural / 1.0 robust): 7 uses 0.5 "natural" (soft, gentle,
  varied but not theatrical), similarity 0.8, speaker boost. v3 takes no previous_text / next_text.
  The first Chapter 1 takes (eleven_multilingual_v2, stability 0.55, style 0.15) were judged monotone; they are
  kept for comparison in Unity/Assets/Audio/Voice/_v1/.
- Fallback --model v2 ("eleven_multilingual_v2"): 7 = stability 0.32, style 0.45, similarity 0.8, speaker boost,
  speed 0.95, plus previous_text / next_text from neighbouring lines in the same scene (no tags).
- Cat: always eleven_multilingual_v2 with his original settings (dry and unhurried) unless --model v3 is
  given explicitly together with --speaker cat.
"""
import json, os, re, sys, time, urllib.request, urllib.error

ROOT = os.getcwd()
KEY = os.environ.get("ELEVENLABS_API_KEY", "").strip()
if not KEY:
    sys.exit("ELEVENLABS_API_KEY is not set - stopping (no files generated).")
API = "https://api.elevenlabs.io/v1"
MODELS = {"v3": "eleven_v3", "v2": "eleven_multilingual_v2"}
SETTINGS = {
    ("seven", "v3"): {"stability": 0.5, "similarity_boost": 0.8, "use_speaker_boost": True},
    ("seven", "v2"): {"stability": 0.32, "similarity_boost": 0.8, "style": 0.45, "use_speaker_boost": True, "speed": 0.95},
    ("cat", "v2"): {"stability": 0.62, "similarity_boost": 0.8, "style": 0.2, "use_speaker_boost": True, "speed": 0.92},
    ("cat", "v3"): {"stability": 1.0, "similarity_boost": 0.8, "use_speaker_boost": True},
}
DEFAULT_MODEL = {"seven": "v3", "cat": "v2"}


def req(path, body=None, accept="application/json"):
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(API + path, data=data, method="POST" if body is not None else "GET",
                               headers={"xi-api-key": KEY, "Content-Type": "application/json", "Accept": accept})
    with urllib.request.urlopen(r, timeout=180) as resp:
        return resp.read()


def find_voices():
    voices = json.loads(req("/voices"))["voices"]
    by = {}
    for v in voices:
        by.setdefault(v["name"].strip().lower(), v["voice_id"])
    ids = {}
    for spk, name in (("seven", "seven"), ("cat", "cat")):
        if name not in by:
            sys.exit(f'Voice "{name.title()}" not found in the ElevenLabs library (have: {sorted(by)}) - stopping.')
        ids[spk] = by[name]
    return ids


def tags(delivery):
    """'awed, quiet' -> '[awed] [quiet] '"""
    parts = [p.strip() for p in re.split(r"[,;/]", delivery or "") if p.strip()]
    return "".join(f"[{p}] " for p in parts)


def arg(name, default=None):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else default


def main():
    force = "--force" in sys.argv
    only = set(sys.argv[sys.argv.index("--only") + 1:]) if "--only" in sys.argv else None
    speaker = arg("--speaker")
    model_flag = arg("--model")
    if speaker not in (None, "seven", "cat") or model_flag not in (None, "v3", "v2"):
        sys.exit("usage: generate_voices.py [--speaker seven|cat] [--model v3|v2] [--force] [--only <id>...]")
    lines = json.load(open(os.path.join(ROOT, "Unity/Assets/Audio/Voice/voice_lines.json"), encoding="utf-8"))["lines"]
    try:
        ids = find_voices()
    except urllib.error.HTTPError as e:
        sys.exit(f"ElevenLabs refused the voice lookup: HTTP {e.code} - stopping.")
    made = skipped = chars = 0
    for i, l in enumerate(lines):
        if only and l["id"] not in only:
            continue
        if speaker and l["speaker"] != speaker:
            continue
        out = os.path.join(ROOT, "Unity/Assets/Audio/Voice", l["dir"], l["id"] + ".mp3")
        if os.path.exists(out) and not force:
            skipped += 1
            continue
        os.makedirs(os.path.dirname(out), exist_ok=True)
        mk = model_flag or DEFAULT_MODEL[l["speaker"]]
        body = {"model_id": MODELS[mk], "voice_settings": SETTINGS[(l["speaker"], mk)]}
        if mk == "v3":
            body["text"] = tags(l.get("delivery")) + l["text"]
        else:
            body["text"] = l["text"]
            same = [x for x in lines if x["scene"] == l["scene"]]
            k = same.index(l)
            if k > 0:
                body["previous_text"] = same[k - 1]["text"]
            if k + 1 < len(same):
                body["next_text"] = same[k + 1]["text"]
        for attempt in range(4):
            try:
                audio = req(f"/text-to-speech/{ids[l['speaker']]}?output_format=mp3_44100_128", body, "audio/mpeg")
                break
            except urllib.error.HTTPError as e:
                if e.code in (429, 500, 502, 503) and attempt < 3:
                    time.sleep(3 * (attempt + 1)); continue
                sys.exit(f"ElevenLabs refused {l['id']}: HTTP {e.code} {e.read()[:300]!r} - stopping after {made} files.")
        with open(out, "wb") as f:
            f.write(audio)
        made += 1
        chars += len(body["text"])
        print(f"{l['id']}  {MODELS[mk]}  {len(audio)//1024} KB  {body['text']}")
    print(f"done: {made} generated, {skipped} skipped, {chars} characters sent, {len(lines)} lines total")


main()
