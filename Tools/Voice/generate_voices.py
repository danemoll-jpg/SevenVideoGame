"""Generate Chapter 1 voice files with ElevenLabs.

Usage (repo root): python Tools/Voice/generate_voices.py [--force] [--only <id>...]
Key: ELEVENLABS_API_KEY environment variable only (never printed or written anywhere).
Voices are looked up by name in Dan's library: "Seven" and "Cat".
Reads Unity/Assets/Audio/Voice/voice_lines.json, writes Unity/Assets/Audio/Voice/<dir>/<id>.mp3.
Existing files are skipped unless --force.
"""
import json, os, sys, time, urllib.request, urllib.error

ROOT = os.getcwd()
KEY = os.environ.get("ELEVENLABS_API_KEY", "").strip()
if not KEY:
    sys.exit("ELEVENLABS_API_KEY is not set - stopping (no files generated).")
API = "https://api.elevenlabs.io/v1"
MODEL = "eleven_multilingual_v2"
# 7: soft and gentle (higher stability, low style). Cat: dry and unhurried.
SETTINGS = {"seven": {"stability": 0.55, "similarity_boost": 0.8, "style": 0.15, "use_speaker_boost": True, "speed": 0.95},
            "cat": {"stability": 0.62, "similarity_boost": 0.8, "style": 0.2, "use_speaker_boost": True, "speed": 0.92}}


def req(path, body=None, accept="application/json"):
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(API + path, data=data, method="POST" if body is not None else "GET",
                               headers={"xi-api-key": KEY, "Content-Type": "application/json", "Accept": accept})
    with urllib.request.urlopen(r, timeout=120) as resp:
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


def main():
    force = "--force" in sys.argv
    only = set(sys.argv[sys.argv.index("--only") + 1:]) if "--only" in sys.argv else None
    lines = json.load(open(os.path.join(ROOT, "Unity/Assets/Audio/Voice/voice_lines.json"), encoding="utf-8"))["lines"]
    try:
        ids = find_voices()
    except urllib.error.HTTPError as e:
        sys.exit(f"ElevenLabs refused the voice lookup: HTTP {e.code} - stopping.")
    made = skipped = 0
    for i, l in enumerate(lines):
        if only and l["id"] not in only:
            continue
        out = os.path.join(ROOT, "Unity/Assets/Audio/Voice", l["dir"], l["id"] + ".mp3")
        if os.path.exists(out) and not force:
            skipped += 1
            continue
        os.makedirs(os.path.dirname(out), exist_ok=True)
        prev = next((x["text"] for x in reversed(lines[:i]) if x["speaker"] == l["speaker"] and x["scene"] == l["scene"]), None)
        body = {"text": l["text"], "model_id": MODEL, "voice_settings": SETTINGS[l["speaker"]]}
        if prev:
            body["previous_text"] = prev
        for attempt in range(4):
            try:
                audio = req(f"/text-to-speech/{ids[l['speaker']]}?output_format=mp3_44100_128", body, "audio/mpeg")
                break
            except urllib.error.HTTPError as e:
                if e.code in (429, 500, 502, 503) and attempt < 3:
                    time.sleep(3 * (attempt + 1)); continue
                sys.exit(f"ElevenLabs refused {l['id']}: HTTP {e.code} {e.read()[:200]!r} - stopping after {made} files.")
        with open(out, "wb") as f:
            f.write(audio)
        made += 1
        print(f"{l['id']}  {len(audio)//1024} KB")
    print(f"done: {made} generated, {skipped} skipped, {len(lines)} lines total")


main()
