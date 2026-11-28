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


# Round 13 (Dan's B01 rewrite, VEN_B01_06-20): the delivery directions become Eleven v3 audio tags (the laughs / chuckle /
# hum are part of the lines). Lines with a direction - and every Panther line in B01, so his takes match - use v3.
TAGS = {
    "VEN_B01_06": "[curious] [lightly] By the way, what should I call you?",          # QF43 6
    "VEN_B01_01": "[looking into the distance] The temple's all the way over there. That's a long walk.",   # QF43 6
    "VEN_C01_03": "[quietly] [in awe] After all that dust and machinery... I'd forgotten a place could look like this.",
    "VEN_C01_08": "[amused] Mm-hmm.",
    "VEN_C05_00": "[quietly] Hear that?",                                           # QF43 13
    "VEN_B01_08": "[a small, surprised laugh] Panther?",
    "VEN_B01_09": "[dryly] Ahem. You could say it without looking down at me.",
    "VEN_B01_10": "[teasing] I'd have to lie on the floor.",
    "VEN_B01_11": "[perfectly composed] There. Saved you the trouble.",
    "VEN_B01_12": "[breathless] You can do that? [disbelieving laugh]",
    "VEN_B01_15": "[mildly] An excellent time to be small.",
    "VEN_B01_16": "[reluctant little chuckle] Mm. Fair point.",
    "VEN_B01_17": "[still amused] You're not getting a bigger bowl.",
    "VEN_B01_18": "[thoughtful hum] Mmm. We can discuss that when you return.",
    "VEN_B01_19": "[laughs softly] [warmly] Look after the ship, Panther.",
    "VEN_B01_20": "[quietly assured] I intend to.",
    # Round 13 (0m): VEN_C12 / VEN_C13 (Dan's directions)
    "VEN_C12_07": "[a quiet sigh] And now it's on someone's side of a border.",
    "VEN_C13_04": "[a small chuckle] Perhaps we'll arrive on a quiet day.",
    "VEN_C13_21": "[laughing softly] Fair point.",
    "VEN_C13_E2": "[a small sigh] Of course you do.",
}
V3_CAT = {"stability": 0.5, "similarity_boost": 0.8, "use_speaker_boost": True}


def take(l):
    """(model, settings, text) for a line."""
    model, st = SET[l["speaker"]]
    text = l["text"].replace("’", "'")
    v3cat = l["speaker"] == "cat" and ((l["scene"] == "VEN_B01" and l["id"] >= "VEN_B01_06") or l["scene"] in ("VEN_C12", "VEN_C13"))
    if l["id"] in TAGS or v3cat or (l.get("direction") and l["speaker"] == "seven"):
        text = TAGS.get(l["id"], (f'[{l["direction"]}] ' if l.get("direction") else "") + text)
        if l["speaker"] == "cat": model, st = "eleven_v3", V3_CAT
        elif l["speaker"] == "seven": model = "eleven_v3"
    return model, st, text


def req(path, body=None, accept="application/json"):
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(API + path, data=data, method="POST" if body is not None else "GET",
                               headers={"xi-api-key": KEY, "Content-Type": "application/json", "Accept": accept})
    with urllib.request.urlopen(r, timeout=180) as resp:
        return resp.read()


# Round 13 (0f): the Wormmaster's taunts during the encounter (not in the spec's scenes: short barks while he fights back;
# Dan can edit or drop them here and in VenusDirectorPart2.Taunts). --taunts makes them.
TAUNTS = [
    ("VEN_ENC_T1", "They answer to me. Not to you."),
    ("VEN_ENC_T2", "You cannot reach them all."),
    ("VEN_ENC_T3", "Turn back, little traveller."),
    ("VEN_ENC_T4", "Another one? I can wait."),
]


def main():
    force = "--force" in sys.argv
    if "--taunts" in sys.argv:
        voices = {v["name"].strip().lower(): v["voice_id"] for v in json.loads(req("/voices"))["voices"]}
        model, st = SET["wormmaster"]
        for i, t in TAUNTS:
            path = os.path.join(OUT, i + ".mp3")
            if os.path.exists(path) and not force: continue
            audio = req(f"/text-to-speech/{voices['wormmaster']}?output_format=mp3_44100_128", {"text": t, "model_id": model, "voice_settings": st}, "audio/mpeg")
            open(path, "wb").write(audio); print(i, len(audio) // 1024, "KB", t)
        return
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
        model, st, text = take(l)
        body = {"text": text, "model_id": model, "voice_settings": st}
        audio = req(f"/text-to-speech/{voices[vname]}?output_format=mp3_44100_128", body, "audio/mpeg")
        open(path, "wb").write(audio); made += 1
        print(f'{l["id"]}  {l["label"]}  {model}  {len(audio)//1024} KB  {text}')
    print(f"done: {made} made, {skipped} kept; waiting for a voice: {len(waiting)} {waiting}")


if __name__ == "__main__":
    main()
