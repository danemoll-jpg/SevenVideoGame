"""Round 11 + 12: voice the Venus lines (every scene; --round 11|12 limits it) with ElevenLabs.

Usage (repo root): python Tools/Voice/generate_venus_voices.py [--force] [--only <id> ...]
Key: ELEVENLABS_API_KEY environment variable only (never printed or written anywhere).
Reads Unity/Assets/Resources/Venus/venus_dialogue.json (from make_venus_lines.py) and writes
Unity/Assets/Resources/VenusVoice/<id>.mp3, loaded by VenusDirector at runtime (a missing file = subtitles only).
Voices by name in Dan's library: "Seven" (eleven_v3, natural), "Cat" (multilingual v2, his Chapter 1 settings; he is
"Panther" in the subtitles from VEN_B01_07), "Guardian" and "Attendant" (multilingual v2, calm) - if one of these
does not exist yet its lines are skipped and listed as waiting for a voice (the scenes play with subtitles).
"""
import json, os, sys, urllib.request

ROOT = os.getcwd()
KEY = os.environ.get("ELEVENLABS_API_KEY", "").strip()
if not KEY: sys.exit("ELEVENLABS_API_KEY is not set - stopping.")
API = "https://api.elevenlabs.io/v1"
OUT = os.path.join(ROOT, "Unity", "Assets", "Resources", "VenusVoice")
R11 = {"VEN_C01", "VEN_C02", "VEN_C03", "VEN_C04", "VEN_C05", "VEN_B01", "VEN_B02", "VEN_B03", "VEN_B04", "VEN_B05"}
SET = {
    "seven": ("eleven_v3", {"stability": 0.5, "similarity_boost": 0.8, "use_speaker_boost": True}),
    "cat": ("eleven_multilingual_v2", {"stability": 0.62, "similarity_boost": 0.8, "style": 0.2, "use_speaker_boost": True, "speed": 0.92}),
    "guardian": ("eleven_multilingual_v2", {"stability": 0.65, "similarity_boost": 0.8, "style": 0.15, "use_speaker_boost": True, "speed": 0.9}),
    "attendant": ("eleven_multilingual_v2", {"stability": 0.55, "similarity_boost": 0.8, "style": 0.25, "use_speaker_boost": True}),
    # Round 12: Wormmaster (Dan's voice "wormmaster"): smooth, slow, condescending
    "wormmaster": ("eleven_multilingual_v2", {"stability": 0.6, "similarity_boost": 0.8, "style": 0.3, "use_speaker_boost": True, "speed": 0.9}),
}


def req(path, body=None, accept="application/json"):
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(API + path, data=data, method="POST" if body is not None else "GET",
                               headers={"xi-api-key": KEY, "Content-Type": "application/json", "Accept": accept})
    with urllib.request.urlopen(r, timeout=180) as resp:
        return resp.read()


def main():
    force = "--force" in sys.argv
    rnd = int(sys.argv[sys.argv.index("--round") + 1]) if "--round" in sys.argv else 0
    only = sys.argv[sys.argv.index("--only") + 1:] if "--only" in sys.argv else None
    d = json.load(open(os.path.join(ROOT, "Unity/Assets/Resources/Venus/venus_dialogue.json"), encoding="utf-8"))
    voices = {v["name"].strip().lower(): v["voice_id"] for v in json.loads(req("/voices"))["voices"]}
    os.makedirs(OUT, exist_ok=True)
    made, skipped, waiting = 0, 0, []
    for l in d["lines"]:
        if rnd == 11 and l["scene"] not in R11 or rnd == 12 and l["scene"] in R11 or (only and l["id"] not in only): continue
        vname = l["voice"].lower()
        if vname not in voices: waiting.append(f'{l["id"]} ({l["voice"]})'); continue
        path = os.path.join(OUT, l["id"] + ".mp3")
        if os.path.exists(path) and not force: skipped += 1; continue
        model, st = SET[l["speaker"]]
        body = {"text": l["text"], "model_id": model, "voice_settings": st}
        audio = req(f"/text-to-speech/{voices[vname]}?output_format=mp3_44100_128", body, "audio/mpeg")
        open(path, "wb").write(audio); made += 1
        print(f'{l["id"]}  {l["label"]}  {model}  {len(audio)//1024} KB  {l["text"]}')
    print(f"done: {made} made, {skipped} kept; waiting for a voice: {len(waiting)} {waiting}")


if __name__ == "__main__":
    main()
