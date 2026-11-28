"""QF40 (3): the climbing sections' lines (Panther, voice "Cat", his usual multilingual v2 settings) from the table in
Unity/Assets/Scripts/Runtime/ClimbSection.cs -> Unity/Assets/Resources/ClimbVoice/<id>.mp3. Key: ELEVENLABS_API_KEY only.
Usage (repo root): python Tools/Voice/generate_climb_voices.py [--force]"""
import json, os, re, sys, urllib.request
ROOT = os.getcwd(); KEY = os.environ.get("ELEVENLABS_API_KEY", "").strip()
if not KEY: sys.exit("ELEVENLABS_API_KEY is not set - stopping.")
API = "https://api.elevenlabs.io/v1"
def req(path, body=None, accept="application/json"):
    r = urllib.request.Request(API + path, data=json.dumps(body).encode() if body else None, method="POST" if body else "GET",
                               headers={"xi-api-key": KEY, "Content-Type": "application/json", "Accept": accept})
    with urllib.request.urlopen(r, timeout=180) as x: return x.read()
src = open(os.path.join(ROOT, "Unity/Assets/Scripts/Runtime/ClimbSection.cs"), encoding="utf-8").read()
lines = re.findall(r'Id([AB]) = "(CLB_[A-Z]+_[AB])", Line\1 = "([^"]+)"', src)
voice = {v["name"].strip().lower(): v["voice_id"] for v in json.loads(req("/voices"))["voices"]}["cat"]
out = os.path.join(ROOT, "Unity/Assets/Resources/ClimbVoice"); os.makedirs(out, exist_ok=True)
for _, lid, text in lines:
    f = os.path.join(out, lid + ".mp3")
    if os.path.exists(f) and "--force" not in sys.argv: continue
    a = req(f"/text-to-speech/{voice}?output_format=mp3_44100_128", {"text": text, "model_id": "eleven_multilingual_v2",
            "voice_settings": {"stability": 0.62, "similarity_boost": 0.8, "style": 0.2, "use_speaker_boost": True, "speed": 0.92}}, "audio/mpeg")
    open(f, "wb").write(a); print(lid, len(a) // 1024, "KB", text)
print(len(lines), "lines")
