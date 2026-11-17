"""Turn Docs/DIALOGUE.md scenes 1-11 into Docs/VOICE-LINES.md + Unity/Assets/Audio/Voice/voice_lines.json.

Usage (repo root): python Tools/Voice/make_line_list.py
Ids: s<scene>_<line>_<speaker>  (e.g. s01_03_cat). Stage directions that ask for a pause
("short pause", "quiet beat", "She pauses", "No answer") become a longer pause after the previous line.
"""
import json, os, re, sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else os.getcwd()
SRC = os.path.join(ROOT, "Docs", "DIALOGUE.md")
SCENE_DIRS = {1: "s01_first_contact", 2: "s02_opening_cell", 3: "s03_what_he_knows", 4: "s04_escape_route",
              5: "s05_scree", 6: "s06_vision", 7: "s07_saucer", 8: "s08_boarding", 9: "s09_interior",
              10: "s10_meal", 11: "s11_stay"}
PAUSE_WORDS = [("short pause", 2.0), ("quiet beat", 2.5), ("she pauses", 1.6), ("no answer", 2.0)]

lines, scene, n = [], 0, 0
for raw in open(SRC, encoding="utf-8"):
    s = raw.strip()
    m = re.match(r"^##\s+(\d+)\s", s)
    if m:
        scene, n = int(m.group(1)), 0
        continue
    if not (1 <= scene <= 11) or not s:
        continue
    m = re.match(r"^\*\*(7|CAT):\*\*\s*(.+)$", s)
    if m:
        n += 1
        spk = "seven" if m.group(1) == "7" else "cat"
        text = m.group(2).replace("’", "'").replace("‘", "'").strip()
        lines.append({"id": f"s{scene:02d}_{n:02d}_{'7' if spk == 'seven' else 'cat'}", "scene": scene,
                      "dir": SCENE_DIRS[scene], "speaker": spk, "text": text, "pauseAfter": 0.45})
        continue
    low = s.lower()
    for word, p in PAUSE_WORDS:
        if word in low and lines and lines[-1]["scene"] == scene:
            lines[-1]["pauseAfter"] = p

out_json = os.path.join(ROOT, "Unity", "Assets", "Audio", "Voice", "voice_lines.json")
os.makedirs(os.path.dirname(out_json), exist_ok=True)
with open(out_json, "w", encoding="utf-8", newline="\n") as f:
    json.dump({"lines": lines}, f, indent=1, ensure_ascii=False)

md = ["# Voice lines — Chapter 1 \"The Moon Prison\"", "",
      "Generated from `Docs/DIALOGUE.md` scenes 1–11 by `Tools/Voice/make_line_list.py` (re-run after script edits,",
      "then `Tools/Voice/generate_voices.py`). Audio: `Unity/Assets/Audio/Voice/<dir>/<id>.mp3`. Speaker `7` = voice",
      "`Seven` (spoken aloud), `CAT` = voice `Cat` (telepathic treatment in Unity). Pause = seconds of silence after the line.",
      "", f"{len(lines)} lines.", "", "| id | scene | speaker | text | pause |", "|---|---|---|---|---|"]
for l in lines:
    md.append(f"| `{l['id']}` | {l['scene']} | {'7' if l['speaker']=='seven' else 'CAT'} | {l['text']} | {l['pauseAfter']} |")
with open(os.path.join(ROOT, "Docs", "VOICE-LINES.md"), "w", encoding="utf-8", newline="\n") as f:
    f.write("\n".join(md) + "\n")
print(f"{len(lines)} lines -> {out_json}")
