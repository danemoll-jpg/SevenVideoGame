"""Round 15: voice Mars's lines with ElevenLabs.

Usage (repo root): python Tools/Voice/generate_mars_voices.py [--force] [--only <id> ...] [--list]
Key: ELEVENLABS_API_KEY environment variable only (never printed or written anywhere).
Reads Unity/Assets/Resources/Mars/mars_dialogue.json (make_mars_lines.py) and writes
Unity/Assets/Resources/MarsVoice/<id>.mp3 (a missing file = subtitles only).
Voices by name in Dan's library: "Seven" (eleven_v3; the spec's delivery directions become audio tags), "Cat" (Panther;
multilingual v2, or v3 when the line has a direction), "Viper" (Dan's saved voice). The guard, the worker and the caretaker
use Dan's SAVED voices "marsguard" / "marsworker" / "caretaker" (2026-11-21); if one is missing, a premade picked here:
  guard     - a firm, clipped middle-aged male voice (labels: male + middle aged / authoritative / deep / firm)
  worker    - a warm, earnest female voice (labels: female + young / warm / friendly)
  caretaker - an older, calm, considered male voice (labels: male + old / calm / wise / grandfather)
The picks are printed and written to Docs/MARS-LINES.md's header by hand when they change.
"""
import json, os, sys, urllib.request

ROOT = os.getcwd()
KEY = os.environ.get("ELEVENLABS_API_KEY", "").strip()
if not KEY: sys.exit("ELEVENLABS_API_KEY is not set - stopping.")
API = "https://api.elevenlabs.io/v1"
OUT = os.path.join(ROOT, "Unity", "Assets", "Resources", "MarsVoice")
SET = {
    "seven": ("eleven_v3", {"stability": 0.5, "similarity_boost": 0.8, "use_speaker_boost": True}),
    "cat": ("eleven_multilingual_v2", {"stability": 0.62, "similarity_boost": 0.8, "style": 0.2, "use_speaker_boost": True, "speed": 0.92}),
    "viper": ("eleven_multilingual_v2", {"stability": 0.6, "similarity_boost": 0.85, "style": 0.35, "use_speaker_boost": True, "speed": 0.92}),
    "guard": ("eleven_multilingual_v2", {"stability": 0.65, "similarity_boost": 0.8, "style": 0.15, "use_speaker_boost": True, "speed": 0.95}),
    "worker": ("eleven_multilingual_v2", {"stability": 0.5, "similarity_boost": 0.8, "style": 0.3, "use_speaker_boost": True}),
    "caretaker": ("eleven_multilingual_v2", {"stability": 0.7, "similarity_boost": 0.8, "style": 0.2, "use_speaker_boost": True, "speed": 0.88}),
}
V3 = {"stability": 0.5, "similarity_boost": 0.8, "use_speaker_boost": True}
WANT = {
    "guard": ("male", ["middle", "authorit", "deep", "firm", "strong", "serious", "confident"], ["old", "young", "child"]),
    "worker": ("female", ["young", "warm", "friendly", "earnest", "soft", "pleasant"], ["old", "child"]),
    "caretaker": ("male", ["old", "calm", "wise", "grandfather", "gentle", "elderly", "raspy"], ["young", "child"]),
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
    d = json.load(open(os.path.join(ROOT, "Unity/Assets/Resources/Mars/mars_dialogue.json"), encoding="utf-8"))
    allv = json.loads(req("/voices"))["voices"]
    voices = {v["name"].strip().lower(): v["voice_id"] for v in allv}
    taken = set()
    saved = {v["name"].strip().lower(): v for v in allv if v.get("category") != "premade"}
    for role in ("guard", "worker", "caretaker"):
        sv = saved.get({"guard": "marsguard", "worker": "marsworker", "caretaker": "caretaker"}[role])   # Dan's saved voices win
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
        elif l["direction"] and l["speaker"] == "viper":
            text = f'[{l["direction"].replace("a dry, dismissive laugh", "dry dismissive laugh")}] ' + text; model, st = "eleven_v3", V3
        audio = req(f"/text-to-speech/{voices[vname]}?output_format=mp3_44100_128", {"text": text, "model_id": model, "voice_settings": st}, "audio/mpeg")
        open(path, "wb").write(audio); made += 1
        print(f'{l["id"]}  {l["label"]}  {model}  {len(audio)//1024} KB  {text}')
    print(f"done: {made} made, {kept} kept; waiting for a voice: {len(waiting)} {waiting}")


if __name__ == "__main__":
    main()
