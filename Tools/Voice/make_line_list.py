"""Turn Docs/DIALOGUE.md scenes 1-11 (+ 6b) into Docs/VOICE-LINES.md + Unity/Assets/Audio/Voice/voice_lines.json.

Usage (repo root): python Tools/Voice/make_line_list.py
Ids: s<scene>_<line>_<speaker>  (e.g. s01_03_cat). Stage directions that ask for a pause
("short pause", "quiet beat", "She pauses", "No answer") become a longer pause after the previous line.
Scene 6b "The city reveal": primary exchange = scene number 65 (ids s06b_01_7 ...), the one-paragraph
"Earlier proposed alternative" = scene number 66 (ids s06b_a01_7 ...), both in dir s06b_city_reveal.
Delivery notes for 7's lines come from Tools/Voice/seven_delivery.json (field "delivery"; cat lines get none).
"""
import json, os, re, sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else os.getcwd()
SRC = os.path.join(ROOT, "Docs", "DIALOGUE.md")
SCENE_DIRS = {1: "s01_first_contact", 2: "s02_opening_cell", 3: "s03_what_he_knows", 4: "s04_escape_route",
              5: "s05_scree", 6: "s06_vision", 7: "s07_saucer", 8: "s08_boarding", 9: "s09_interior",
              10: "s10_meal", 11: "s11_stay", 65: "s06b_city_reveal", 66: "s06b_city_reveal"}
SCENE_TAGS = {65: "06b", 66: "06b"}         # id tag for scenes whose heading is not a plain number
SCENE_NUMBERS = {"6b": 65}                  # heading -> scene number (the alternative 6b exchange = 66)
PAUSE_WORDS = [("short pause", 2.0), ("quiet beat", 2.5), ("she pauses", 1.6), ("no answer", 2.0),
               ("she continues looking", 2.0), ("a quiet pause", 2.5),
               ("follows the rising curve", 2.5), ("machinery starts moving", 3.0)]
LINE_RE = r"\*\*(7|CAT):\*\*\s*(.+)"
DELIVERY = os.path.join(ROOT, "Tools", "Voice", "seven_delivery.json")
delivery = json.load(open(DELIVERY, encoding="utf-8")) if os.path.exists(DELIVERY) else {}

lines, scene, n = [], 0, 0


def clean(t):
    return t.replace("’", "'").replace("‘", "'").strip()


def add_line(sc, num, who, text, prefix=""):
    spk = "seven" if who == "7" else "cat"
    tag = SCENE_TAGS.get(sc, f"{sc:02d}")
    lid = f"s{tag}_{prefix}{num:02d}_{'7' if spk == 'seven' else 'cat'}"
    l = {"id": lid, "scene": sc, "dir": SCENE_DIRS[sc], "speaker": spk, "text": clean(text), "pauseAfter": 0.45}
    if spk == "seven" and lid in delivery:
        l["delivery"] = delivery[lid]
    lines.append(l)


def apply_pause(s, sc):
    low = s.lower()
    for word, p in PAUSE_WORDS:
        if word in low and lines and lines[-1]["scene"] == sc:
            lines[-1]["pauseAfter"] = p


for raw in open(SRC, encoding="utf-8"):
    s = raw.strip()
    m = re.match(r"^##\s+(\d+[a-z]?)\s", s)
    if m:
        h = m.group(1)
        scene, n = (SCENE_NUMBERS[h] if h in SCENE_NUMBERS else int(h) if h.isdigit() else -1), 0
        continue
    if scene not in SCENE_DIRS or scene == 66 or not s:
        continue
    if scene == 65 and s.lower().startswith("*earlier proposed alternative:*"):
        # One paragraph: "**7:** ... / **CAT:** ... / stage direction / ..." -> scene 66.
        k = 0
        for part in s.split(":*", 1)[1].split(" / "):
            part = part.strip()
            m = re.match("^" + LINE_RE + "$", part)
            if m:
                k += 1
                add_line(66, k, m.group(1), m.group(2), "a")
            else:
                apply_pause(part, 66)
        continue
    m = re.match("^" + LINE_RE + "$", s)
    if m:
        n += 1
        add_line(scene, n, m.group(1), m.group(2))
        continue
    apply_pause(s, scene)

missing = [l["id"] for l in lines if l["speaker"] == "seven" and "delivery" not in l]
if missing:
    print("note: no delivery note for " + ", ".join(missing))

out_json = os.path.join(ROOT, "Unity", "Assets", "Audio", "Voice", "voice_lines.json")
os.makedirs(os.path.dirname(out_json), exist_ok=True)
with open(out_json, "w", encoding="utf-8", newline="\n") as f:
    json.dump({"lines": lines}, f, indent=1, ensure_ascii=False)

md = ["# Voice lines — Chapter 1 \"The Moon Prison\"", "",
      "Generated from `Docs/DIALOGUE.md` scenes 1–11 and 6b by `Tools/Voice/make_line_list.py` (re-run after script edits,",
      "then `Tools/Voice/generate_voices.py`). Audio: `Unity/Assets/Audio/Voice/<dir>/<id>.mp3`. Speaker `7` = voice",
      "`Seven` (spoken aloud), `CAT` = voice `Cat` (telepathic treatment in Unity). Pause = seconds of silence after the line.",
      "Scene 6b \"The city reveal\" is numbered **65** (primary exchange, ids `s06b_NN_*`) and **66** (the earlier proposed",
      "alternative, ids `s06b_aNN_*`); both live in `s06b_city_reveal/` and sit between scenes 6 and 7 in the list.",
      "Delivery = performance note for 7's lines from `Tools/Voice/seven_delivery.json` (sent to Eleven v3 as audio tags,",
      "never shown in subtitles); cat lines have none.",
      "", f"{len(lines)} lines.", "", "| id | scene | speaker | text | delivery | pause |", "|---|---|---|---|---|---|"]
for l in lines:
    md.append(f"| `{l['id']}` | {l['scene']} | {'7' if l['speaker'] == 'seven' else 'CAT'} | {l['text']} | "
              f"{l.get('delivery', '')} | {l['pauseAfter']} |")
with open(os.path.join(ROOT, "Docs", "VOICE-LINES.md"), "w", encoding="utf-8", newline="\n") as f:
    f.write("\n".join(md) + "\n")
print(f"{len(lines)} lines -> {out_json}")
