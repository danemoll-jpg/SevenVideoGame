"""Round 17: voice the Biavian mothership (Veyra) chapter's lines with ElevenLabs (a copy of generate_asteroid_voices.py).

Usage (repo root): python Tools/Voice/generate_biaveh_voices.py [--force] [--only <id> ...] [--list]
Key: ELEVENLABS_API_KEY environment variable only (never printed or written anywhere).
Reads Unity/Assets/Resources/Biaveh/biaveh_dialogue.json (make_biaveh_lines.py) and writes
Unity/Assets/Resources/BiavehVoice/<id>.mp3 (a missing file = subtitles only).
Voices: "Seven" (v3 + tags), "Cat" (Panther), "Ratwing" (Dan's saved voice; a premade sly male fallback if it is missing).
Nela (the Guide) = a PREMADE calm, gentle, warm female voice (Dan's saved "Nela" voice wins (2026-11-21)), the same
voice for every Nela line. Each resident (resident1, resident2, ...) = its own premade voice. Nela's and the residents'
speech is telepathic in game (Unity's treatment) - nothing special in the files.
Premade picks: a preference list by name (stable between runs), then the label score as a fallback; never a voice another
role here already took, and not the premades other chapters use (Lily = the asteroid System; Daniel / Sarah / Bill = Mars).
"""
import json, os, sys, urllib.request

ROOT = os.getcwd()
KEY = os.environ.get("ELEVENLABS_API_KEY", "").strip()
if not KEY: sys.exit("ELEVENLABS_API_KEY is not set - stopping.")
API = "https://api.elevenlabs.io/v1"
OUT = os.path.join(ROOT, "Unity", "Assets", "Resources", "BiavehVoice")
SET = {
    "seven": ("eleven_v3", {"stability": 0.5, "similarity_boost": 0.8, "use_speaker_boost": True}),
    "cat": ("eleven_multilingual_v2", {"stability": 0.62, "similarity_boost": 0.8, "style": 0.2, "use_speaker_boost": True, "speed": 0.92}),
    "nela": ("eleven_multilingual_v2", {"stability": 0.7, "similarity_boost": 0.8, "style": 0.1, "use_speaker_boost": True, "speed": 0.9}),
    "ratwing": ("eleven_multilingual_v2", {"stability": 0.45, "similarity_boost": 0.85, "style": 0.35, "use_speaker_boost": True, "speed": 1.0}),
    "resident": ("eleven_multilingual_v2", {"stability": 0.6, "similarity_boost": 0.8, "style": 0.15, "use_speaker_boost": True, "speed": 0.95}),
}
V3 = {"stability": 0.5, "similarity_boost": 0.8, "use_speaker_boost": True}
USED_ELSEWHERE = ("lily", "daniel", "sarah", "bill")
# role -> (saved voice names that win, preferred premade names, gender, good words, bad words)
WANT = {
    "nela": (["biavianguide", "nela"], ["bella", "matilda", "alice"], "female",
             ["calm", "gentle", "warm", "soft", "soothing", "reassur", "professional"], ["child", "raspy", "seductive", "sassy", "quirky", "hyped"]),
    "ratwing": (["ratwing"], ["callum"], "male", ["trickster", "sly", "raspy", "gravel", "unsettling", "husky"], ["warm", "comforting", "calm"]),
    # QF23 (13e): a voice matches the look - the seedlings resident (resident1) uses the female Biavian look, so a female voice
    "resident": ([], ["matilda", "alice", "jessica"], "female",
                 ["casual", "conversational", "warm", "natural", "down-to-earth"], ["hyped", "fierce", "dominant"]),
}


def req(path, body=None, accept="application/json"):
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(API + path, data=data, method="POST" if body is not None else "GET",
                               headers={"xi-api-key": KEY, "Content-Type": "application/json", "Accept": accept})
    with urllib.request.urlopen(r, timeout=180) as resp:
        return resp.read()


def first(name):
    return name.split(" - ")[0].strip().lower()


def pick(allv, role, taken):
    saved, prefer, gender, good, bad = WANT[role]
    for v in allv:
        if v.get("category") != "premade" and v["name"].strip().lower() in saved: return v, "saved"
    pre = [v for v in allv if v.get("category") == "premade" and v["voice_id"] not in taken and first(v["name"]) not in USED_ELSEWHERE]
    for n in prefer:
        for v in pre:
            if first(v["name"]) == n: return v, "premade"
    best, bs = None, -99
    for v in pre:
        lab = v.get("labels") or {}
        blob = (json.dumps(lab) + " " + v["name"] + " " + (v.get("description") or "")).lower()
        if gender and (lab.get("gender") or "").lower() != gender: continue
        s = sum(2 for w in good if w in blob) - sum(3 for w in bad if w in blob)
        if s > bs: best, bs = v, s
    return best, "premade"


def main():
    force = "--force" in sys.argv
    only = sys.argv[sys.argv.index("--only") + 1:] if "--only" in sys.argv else None
    d = json.load(open(os.path.join(ROOT, "Unity/Assets/Resources/Biaveh/biaveh_dialogue.json"), encoding="utf-8"))
    allv = json.loads(req("/voices"))["voices"]
    voices = {v["name"].strip().lower(): v["voice_id"] for v in allv}
    taken = set()
    roles = ["nela", "ratwing"] + sorted({l["speaker"] for l in d["lines"] if l["speaker"].startswith("resident")})
    for role in roles:
        v, kind = pick(allv, "resident" if role.startswith("resident") else role, taken)
        if v:
            voices[role] = v["voice_id"]; taken.add(v["voice_id"])
            print(f'{role}: {kind} voice "{v["name"]}" {json.dumps(v.get("labels"))}')
    if "--list" in sys.argv: return
    os.makedirs(OUT, exist_ok=True)
    made, kept, waiting, failed = 0, 0, [], []
    for l in d["lines"]:
        if only and l["id"] not in only: continue
        vname = l["speaker"] if l["speaker"] in voices and l["speaker"] in roles else l["voice"].lower()
        if vname not in voices: waiting.append(f'{l["id"]} ({l["voice"]})'); continue
        path = os.path.join(OUT, l["id"] + ".mp3")
        if os.path.exists(path) and not force: kept += 1; continue
        model, st = SET["resident" if l["speaker"].startswith("resident") else l["speaker"]]
        text = l["text"].replace("’", "'").replace("—", ", ").replace("…", "...")
        if l["direction"] and l["speaker"] in ("seven", "cat"):
            text = f'[{l["direction"]}] ' + text; model, st = "eleven_v3", V3
        elif l["speaker"] == "seven": model = "eleven_v3"
        try:
            audio = req(f"/text-to-speech/{voices[vname]}?output_format=mp3_44100_128", {"text": text, "model_id": model, "voice_settings": st}, "audio/mpeg")
        except Exception as e:
            failed.append(f'{l["id"]}: {type(e).__name__} {getattr(e, "code", "")}'); continue
        open(path, "wb").write(audio); made += 1
        print(f'{l["id"]}  {l["label"]}  {model}  {len(audio)//1024} KB  {text}')
    print(f"done: {made} made, {kept} kept; waiting for a voice: {len(waiting)} {waiting}; failed: {len(failed)} {failed}")


if __name__ == "__main__":
    main()
