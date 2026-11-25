"""Round 20 (Grand Circuit): voice the racers' short taunts / cheers with ElevenLabs, in their existing saved voices
("seven", "Duchess", "Tunneler" - the story's voices).

Usage (repo root): python Tools/Voice/generate_gc_voices.py [--force] [--only <id> ...] [--list]
Key: ELEVENLABS_API_KEY environment variable only (never printed or written anywhere).
Reads Unity/Assets/Resources/GrandCircuit/gc_lines.json, writes Unity/Assets/Resources/GcVoice/<id>.mp3
(a missing file = the subtitle only).
"""
import json, os, sys, urllib.request

ROOT = os.getcwd()
KEY = os.environ.get("ELEVENLABS_API_KEY", "").strip()
if not KEY: sys.exit("ELEVENLABS_API_KEY is not set - stopping.")
API = "https://api.elevenlabs.io/v1"
OUT = os.path.join(ROOT, "Unity", "Assets", "Resources", "GcVoice")
V2 = {"duchess": {"stability": 0.6, "similarity_boost": 0.8, "style": 0.25, "use_speaker_boost": True, "speed": 0.9},
      "tunneler": {"stability": 0.55, "similarity_boost": 0.8, "style": 0.3, "use_speaker_boost": True}}
V3 = {"stability": 0.5, "similarity_boost": 0.8, "use_speaker_boost": True}


def req(path, body=None, accept="application/json"):
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(API + path, data=data, method="POST" if body is not None else "GET",
                               headers={"xi-api-key": KEY, "Content-Type": "application/json", "Accept": accept})
    with urllib.request.urlopen(r, timeout=180) as resp:
        return resp.read()


def main():
    force = "--force" in sys.argv
    only = [a for a in sys.argv[sys.argv.index("--only") + 1:] if not a.startswith("--")] if "--only" in sys.argv else None
    d = json.load(open(os.path.join(ROOT, "Unity/Assets/Resources/GrandCircuit/gc_lines.json"), encoding="utf-8"))
    allv = json.loads(req("/voices"))["voices"]
    saved = {v["name"].strip().lower(): v for v in allv if v.get("category") != "premade"}
    for name in sorted({l["voice"].lower() for l in d["lines"]}):
        print(f'{name}: {"saved voice" if name in saved else "MISSING"}')
    if "--list" in sys.argv: return
    os.makedirs(OUT, exist_ok=True)
    made, kept, failed = 0, 0, []
    for l in d["lines"]:
        if only and l["id"] not in only: continue
        v = saved.get(l["voice"].lower())
        if not v: failed.append(l["id"] + " (no voice)"); continue
        path = os.path.join(OUT, l["id"] + ".mp3")
        if os.path.exists(path) and not force: kept += 1; continue
        text = l["text"]
        if l["speaker"] == "seven" or l["direction"]:
            model, st = "eleven_v3", V3
            if l["direction"]: text = f'[{l["direction"]}] ' + text
        else:
            model, st = "eleven_multilingual_v2", V2[l["speaker"]]
        try:
            audio = req(f"/text-to-speech/{v['voice_id']}?output_format=mp3_44100_128", {"text": text, "model_id": model, "voice_settings": st}, "audio/mpeg")
        except Exception as e:
            failed.append(f'{l["id"]}: {type(e).__name__} {getattr(e, "code", "")}'); continue
        open(path, "wb").write(audio); made += 1
        print(f'{l["id"]}  {model}  {len(audio)//1024} KB  {text}')
    print(f"done: {made} made, {kept} kept; failed: {len(failed)} {failed}")


if __name__ == "__main__":
    main()
