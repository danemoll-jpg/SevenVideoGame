"""Round 19: voice the Moon finale's lines with ElevenLabs (a copy of generate_cattalus_voices.py).

Usage (repo root): python Tools/Voice/generate_finale_voices.py [--force] [--only <id> ...] [--speaker <who>] [--list]
Key: ELEVENLABS_API_KEY environment variable only (never printed or written anywhere).
Reads Unity/Assets/Resources/Finale/finale_dialogue.json (make_finale_lines.py) and writes
Unity/Assets/Resources/FinaleVoice/<id>.mp3 (a missing file = subtitles only).
The Archive and RestVoice are always multilingual v2 (steady, never "performed"); the others as Cattalus.
Voices by saved name (Dan's ElevenLabs library, 2026-11-22): "seven", "cat" (Panther), "juniper", "Duchess", "steward" and
"TeedOff Barker" (the announcer + every Cat-a-tron advert, Dan's voice from Tee'd Off). A voice Dan re-designs later = save it
under the same name and run with --speaker <who> --force (one re-generation).
Models: 7 = Eleven v3 (performance notes as audio tags); the others = Eleven v3 with their note as a tag when the line has one,
else multilingual v2; the Barker = Eleven v3 with [showman] (as in Tee'd Off).
"""
import json, os, sys, urllib.request

ROOT = os.getcwd()
KEY = os.environ.get("ELEVENLABS_API_KEY", "").strip()
if not KEY: sys.exit("ELEVENLABS_API_KEY is not set - stopping.")
API = "https://api.elevenlabs.io/v1"
OUT = os.path.join(ROOT, "Unity", "Assets", "Resources", "FinaleVoice")
V2 = {
    "cat": {"stability": 0.62, "similarity_boost": 0.8, "style": 0.2, "use_speaker_boost": True, "speed": 0.92},
    "juniper": {"stability": 0.55, "similarity_boost": 0.8, "style": 0.25, "use_speaker_boost": True, "speed": 0.95},
    "duchess": {"stability": 0.6, "similarity_boost": 0.8, "style": 0.25, "use_speaker_boost": True, "speed": 0.88},
    "steward": {"stability": 0.7, "similarity_boost": 0.8, "style": 0.1, "use_speaker_boost": True, "speed": 1.0},
    "archive": {"stability": 0.8, "similarity_boost": 0.8, "style": 0.0, "use_speaker_boost": True, "speed": 0.9},
    "rest": {"stability": 0.75, "similarity_boost": 0.85, "style": 0.15, "use_speaker_boost": True, "speed": 0.82},
    "worker": {"stability": 0.6, "similarity_boost": 0.8, "style": 0.2, "use_speaker_boost": True, "speed": 0.95},
    "worker2": {"stability": 0.7, "similarity_boost": 0.8, "style": 0.15, "use_speaker_boost": True, "speed": 0.88},
    "keeper": {"stability": 0.7, "similarity_boost": 0.8, "style": 0.15, "use_speaker_boost": True, "speed": 0.85},
    "caretaker": {"stability": 0.6, "similarity_boost": 0.8, "style": 0.2, "use_speaker_boost": True, "speed": 0.95},
    "tunneler": {"stability": 0.6, "similarity_boost": 0.8, "style": 0.3, "use_speaker_boost": True, "speed": 0.95},
}
V3 = {"stability": 0.5, "similarity_boost": 0.8, "use_speaker_boost": True}


def req(path, body=None, accept="application/json"):
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(API + path, data=data, method="POST" if body is not None else "GET",
                               headers={"xi-api-key": KEY, "Content-Type": "application/json", "Accept": accept})
    with urllib.request.urlopen(r, timeout=180) as resp:
        return resp.read()


def main():
    force = "--force" in sys.argv
    only = None
    if "--only" in sys.argv: only = [a for a in sys.argv[sys.argv.index("--only") + 1:] if not a.startswith("--")]
    who = sys.argv[sys.argv.index("--speaker") + 1] if "--speaker" in sys.argv else None
    d = json.load(open(os.path.join(ROOT, "Unity/Assets/Resources/Finale/finale_dialogue.json"), encoding="utf-8"))
    allv = json.loads(req("/voices"))["voices"]
    saved = {v["name"].strip().lower(): v for v in allv if v.get("category") != "premade"}
    for name in sorted({l["voice"].lower() for l in d["lines"]}):
        v = saved.get(name)
        print(f'{name}: {"saved voice " + v["voice_id"] if v else "MISSING"}')
    if "--list" in sys.argv: return
    os.makedirs(OUT, exist_ok=True)
    made, kept, waiting, failed = 0, 0, [], []
    for l in d["lines"]:
        if only and l["id"] not in only: continue
        if who and l["speaker"] != who: continue
        v = saved.get(l["voice"].lower())
        if not v: waiting.append(f'{l["id"]} ({l["voice"]})'); continue
        path = os.path.join(OUT, l["id"] + ".mp3")
        if os.path.exists(path) and not force: kept += 1; continue
        text = l["text"].replace("’", "'").replace("‘", "'").replace("—", ", ").replace("…", "...")
        sp = l["speaker"]
        if l.get("tagged"):
            model, st, text = "eleven_v3", V3, l["tagged"]          # QF30 (15): several audio tags (crying)
        elif sp in ("archive", "rest"):
            model, st = "eleven_multilingual_v2", V2[sp]
        elif sp == "seven" or l["direction"]:
            model, st = "eleven_v3", V3
            if l["direction"]: text = f'[{l["direction"]}] ' + text
        else:
            model, st = "eleven_multilingual_v2", V2.get(sp, V2["cat"])
        try:
            audio = req(f"/text-to-speech/{v['voice_id']}?output_format=mp3_44100_128", {"text": text, "model_id": model, "voice_settings": st}, "audio/mpeg")
        except Exception as e:
            failed.append(f'{l["id"]}: {type(e).__name__} {getattr(e, "code", "")}'); continue
        open(path, "wb").write(audio); made += 1
        print(f'{l["id"]}  {l["label"]}  {model}  {len(audio)//1024} KB  {text}')
    print(f"done: {made} made, {kept} kept; waiting for a voice: {len(waiting)} {waiting}; failed: {len(failed)} {failed}")


if __name__ == "__main__":
    main()
