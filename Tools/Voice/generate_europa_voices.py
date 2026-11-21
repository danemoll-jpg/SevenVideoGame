"""Round 14: voice Europa's lines with ElevenLabs.

Usage (repo root): python Tools/Voice/generate_europa_voices.py [--force] [--only <id> ...]
Key: ELEVENLABS_API_KEY environment variable only (never printed or written anywhere).
Reads Unity/Assets/Resources/Europa/europa_dialogue.json (make_europa_lines.py) and writes
Unity/Assets/Resources/EuropaVoice/<id>.mp3 (a missing file = subtitles only).
Voices by name in Dan's library: "Seven" (eleven_v3; the spec's delivery directions become audio tags), "Cat" (Panther;
multilingual v2, or v3 when the line has a direction), "Keeper" (Dan's saved voice; slow, calm), "Tunneler" (his existing
voice). The Scree worker = Dan's SAVED voice "screeworker" (2026-11-21); if it is missing, a PREMADE voice picked here:
the first premade voice whose labels/description say hoarse / raspy / gravelly, else "Callum". Unity layers Dan's Scree
noise under each Scree line and a soft whale call under each Keeper line (EuropaDirector).
"""
import json, os, sys, urllib.request

ROOT = os.getcwd()
KEY = os.environ.get("ELEVENLABS_API_KEY", "").strip()
if not KEY: sys.exit("ELEVENLABS_API_KEY is not set - stopping.")
API = "https://api.elevenlabs.io/v1"
OUT = os.path.join(ROOT, "Unity", "Assets", "Resources", "EuropaVoice")
SET = {
    "seven": ("eleven_v3", {"stability": 0.5, "similarity_boost": 0.8, "use_speaker_boost": True}),
    "cat": ("eleven_multilingual_v2", {"stability": 0.62, "similarity_boost": 0.8, "style": 0.2, "use_speaker_boost": True, "speed": 0.92}),
    "keeper": ("eleven_multilingual_v2", {"stability": 0.7, "similarity_boost": 0.8, "style": 0.15, "use_speaker_boost": True, "speed": 0.85}),
    "tunneler": ("eleven_multilingual_v2", {"stability": 0.55, "similarity_boost": 0.8, "style": 0.3, "use_speaker_boost": True}),
    "scree": ("eleven_multilingual_v2", {"stability": 0.45, "similarity_boost": 0.75, "style": 0.3, "use_speaker_boost": True, "speed": 0.9}),
}
V3 = {"stability": 0.5, "similarity_boost": 0.8, "use_speaker_boost": True}


def req(path, body=None, accept="application/json"):
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(API + path, data=data, method="POST" if body is not None else "GET",
                               headers={"xi-api-key": KEY, "Content-Type": "application/json", "Accept": accept})
    with urllib.request.urlopen(r, timeout=180) as resp:
        return resp.read()


def pick_scree(all_voices):
    pre = [v for v in all_voices if v.get("category") == "premade"]
    for v in pre:
        blob = (json.dumps(v.get("labels") or {}) + " " + (v.get("description") or "")).lower()
        if any(w in blob for w in ("hoarse", "raspy", "gravel")): return v
    return next((v for v in pre if v["name"].lower().startswith("callum")), pre[0] if pre else None)


def main():
    force = "--force" in sys.argv
    only = sys.argv[sys.argv.index("--only") + 1:] if "--only" in sys.argv else None
    d = json.load(open(os.path.join(ROOT, "Unity/Assets/Resources/Europa/europa_dialogue.json"), encoding="utf-8"))
    allv = json.loads(req("/voices"))["voices"]
    voices = {v["name"].strip().lower(): v["voice_id"] for v in allv}
    sv = next((v for v in allv if v.get("category") != "premade" and v["name"].strip().lower() == "screeworker"), None)   # Dan's saved voice wins
    sc = sv or pick_scree(allv)
    if sc: voices["scree"] = sc["voice_id"]; print(f'Scree worker: {"saved" if sv else "premade"} voice "{sc["name"]}"')
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
