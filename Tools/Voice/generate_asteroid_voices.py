"""Round 16: voice the asteroid chapter's lines with ElevenLabs (a copy of generate_mars_voices.py).

Usage (repo root): python Tools/Voice/generate_asteroid_voices.py [--force] [--only <id> ...] [--list]
Key: ELEVENLABS_API_KEY environment variable only (never printed or written anywhere).
Reads Unity/Assets/Resources/Asteroid/asteroid_dialogue.json (make_asteroid_lines.py) and writes
Unity/Assets/Resources/AsteroidVoice/<id>.mp3 (a missing file = subtitles only).
Voices: "Seven" (v3 + tags), "Cat" (Panther), "Machinist" (Dan's saved voice). The facility System announcer = Dan's SAVED voice "system" (2026-11-21); if missing, a PREMADE
voice picked here: a flat, clear, neutral recorded-announcement voice (labels: female + calm / clear / neutral / news).
"""
import json, os, sys, urllib.request

ROOT = os.getcwd()
KEY = os.environ.get("ELEVENLABS_API_KEY", "").strip()
if not KEY: sys.exit("ELEVENLABS_API_KEY is not set - stopping.")
API = "https://api.elevenlabs.io/v1"
OUT = os.path.join(ROOT, "Unity", "Assets", "Resources", "AsteroidVoice")
SET = {
    "seven": ("eleven_v3", {"stability": 0.5, "similarity_boost": 0.8, "use_speaker_boost": True}),
    "cat": ("eleven_multilingual_v2", {"stability": 0.62, "similarity_boost": 0.8, "style": 0.2, "use_speaker_boost": True, "speed": 0.92}),
    "machinist": ("eleven_multilingual_v2", {"stability": 0.8, "similarity_boost": 0.85, "style": 0.05, "use_speaker_boost": True, "speed": 0.86}),
    "system": ("eleven_multilingual_v2", {"stability": 0.95, "similarity_boost": 0.75, "style": 0.0, "use_speaker_boost": False, "speed": 0.9}),
}
V3 = {"stability": 0.5, "similarity_boost": 0.8, "use_speaker_boost": True}
WANT = {
    "system": ("female", ["calm", "clear", "neutral", "news", "narrat", "professional", "confident"], ["child", "old", "raspy", "seductive", "whisper"]),
}


def req(path, body=None, accept="application/json"):
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(API + path, data=data, method="POST" if body is not None else "GET",
                               headers={"xi-api-key": KEY, "Content-Type": "application/json", "Accept": accept})
    with urllib.request.urlopen(r, timeout=180) as resp:
        return resp.read()


def pick(all_voices, role, taken):
    gender, good, bad = WANT[role]
    best, bs = None, -99
    for v in all_voices:
        if v.get("category") != "premade" or v["voice_id"] in taken: continue
        lab = v.get("labels") or {}
        blob = (json.dumps(lab) + " " + (v.get("description") or "")).lower()
        if (lab.get("gender") or "").lower() != gender: continue
        s = sum(2 for w in good if w in blob) - sum(3 for w in bad if w in blob)
        if s > bs: best, bs = v, s
    return best


def main():
    force = "--force" in sys.argv
    only = sys.argv[sys.argv.index("--only") + 1:] if "--only" in sys.argv else None
    d = json.load(open(os.path.join(ROOT, "Unity/Assets/Resources/Asteroid/asteroid_dialogue.json"), encoding="utf-8"))
    allv = json.loads(req("/voices"))["voices"]
    voices = {v["name"].strip().lower(): v["voice_id"] for v in allv}
    taken = set()
    saved = {v["name"].strip().lower(): v for v in allv if v.get("category") != "premade"}
    for role in ("system",):
        sv = saved.get("system")   # Dan's saved voice wins
        if sv: voices[role] = sv["voice_id"]; taken.add(sv["voice_id"]); print(f'{role}: saved voice "{sv["name"]}"'); continue
        v = pick(allv, role, taken)
        if v: voices[role] = v["voice_id"]; taken.add(v["voice_id"]); print(f'{role}: premade voice "{v["name"]}" {json.dumps(v.get("labels"))}')
    if "--list" in sys.argv: return
    os.makedirs(OUT, exist_ok=True)
    made, kept, waiting = 0, 0, []
    for l in d["lines"]:
        if only and l["id"] not in only: continue
        vname = l["voice"].lower()
        if vname not in voices: waiting.append(f'{l["id"]} ({l["voice"]})'); continue
        path = os.path.join(OUT, l["id"] + ".mp3")
        if os.path.exists(path) and not force: kept += 1; continue
        model, st = SET[l["speaker"]]
        text = l["text"].replace("’", "'").replace("—", ", ").replace("…", "...")
        if l["direction"] and l["speaker"] in ("seven", "cat"):
            text = f'[{l["direction"]}] ' + text; model, st = "eleven_v3", V3
        elif l["speaker"] == "seven": model = "eleven_v3"
        audio = req(f"/text-to-speech/{voices[vname]}?output_format=mp3_44100_128", {"text": text, "model_id": model, "voice_settings": st}, "audio/mpeg")
        open(path, "wb").write(audio); made += 1
        print(f'{l["id"]}  {l["label"]}  {model}  {len(audio)//1024} KB  {text}')
    print(f"done: {made} made, {kept} kept; waiting for a voice: {len(waiting)} {waiting}")


if __name__ == "__main__":
    main()
