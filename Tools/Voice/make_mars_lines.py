"""Round 15: Mars's dialogue as data.

Usage (repo root): python Tools/Voice/make_mars_lines.py
Reads Docs/CHAPTER_MARS.md (Dan's script, sections 1-13) and writes
  Unity/Assets/Resources/Mars/mars_dialogue.json  (scenes + lines, loaded by MarsDialogue at runtime)
  Docs/MARS-LINES.md                              (the line list with ids, for Dan)
Line ids: MAR_<scene>_<nn>. Scenes are the spec's sections (MAR_01 ... MAR_13); section 13's versions are split into
MAR_13U (Europa unfinished), MAR_13C (Europa complete) and MAR_13B (in both versions).
"**7 — a small sigh:** ..." keeps "a small sigh" as the delivery direction (an Eleven v3 tag for 7 / Panther).
Speakers: 7 -> seven (voice "Seven"), PANTHER -> cat ("Cat", telepathic), VIPER -> viper (Dan's saved voice "Viper"),
GUARD / WORKER / CARETAKER -> premade ElevenLabs voices picked by generate_mars_voices.py.
"""
import json, os, re

ROOT = os.getcwd()
SRC = os.path.join(ROOT, "Docs", "CHAPTER_MARS.md")
OUT = os.path.join(ROOT, "Unity", "Assets", "Resources", "Mars", "mars_dialogue.json")
DOC = os.path.join(ROOT, "Docs", "MARS-LINES.md")
SPK = {"7": ("seven", "7", "Seven"), "PANTHER": ("cat", "Panther", "Cat"), "VIPER": ("viper", "Viper", "Viper"),
       "GUARD": ("guard", "Guard", "guard"), "WORKER": ("worker", "Worker", "worker"), "CARETAKER": ("caretaker", "Caretaker", "caretaker")}

text = open(SRC, encoding="utf-8").read().replace("\r\n", "\n")
scenes, lines = [], []
sec, sub, n = None, None, {}
# Dan's revised section 13 (2026-11-19) is written as tables: | MAR_13U_01 | 7 | line | direction |, plus the travel event
# TRV_AST. Their direction column mixes stage directions with vocal ones - only the vocal part becomes the audio tag
# ("direction"); the whole cell is kept as "action" for the animation.
row_re = re.compile(r"^\|\s*((?:MAR_13[UCB]|TRV_AST)_\d\d)\s*\|\s*(7|Panther)\s*\|\s*(.+?)\s*\|\s*(.*?)\s*\|$")
# QF18: named rows in any section - | MAR_03_11 | Worker | line | direction | (the chat's proposed lines, Dan's ids)
any_row_re = re.compile(r"^\|\s*(MAR_\d\d_[A-Z]?\d+)\s*\|\s*(7|Panther|Viper|Worker|Guard|Caretaker)\s*\|\s*(.+?)\s*\|\s*(.*?)\s*\|$")
WHO = {"7": "7", "Panther": "PANTHER", "Viper": "VIPER", "Worker": "WORKER", "Guard": "GUARD", "Caretaker": "CARETAKER"}
TONE = ("quiet", "surprised", "warm", "dry", "alarmed", "quick", "stiff", "formal", "calm", "startled", "laugh", "breath", "cold", "amused", "softly", "gently")
def tone(cell):
    keep = [p.strip() for p in re.split(r"[;,]", cell) if any(w in p.lower() for w in TONE) and not any(w in p.lower() for w in ("cut to", "glance", "pointing", "hands on", "to the worker", "loudspeaker"))]
    return ", ".join(keep)
VOCAL = ("laugh", "chuckle", "exhale", "sigh", "breath", "hum", "thoughtful", "dry", "softly", "soft", "quiet", "measured",
         "matter-of-fact", "amused", "gently", "attentive", "warm")
SCN13 = {"MAR_13U": "Departure routing (Europa unfinished)", "MAR_13C": "Departure routing (Europa complete)",
         "MAR_13B": "Departure routing (shared closing)", "TRV_AST": "Travel event: the asteroid discovery"}
def vocal(cell):
    keep = [p.strip() for p in cell.split(";") if any(w in p.lower() for w in VOCAL) and not any(w in p.lower() for w in ("display", "hand", "head", "lap", "eyes", "glances", "leans", "turns", "selects", "indicates", "reads", "studies", "looks"))]
    return ", ".join(keep)
line_re = re.compile(r"^\*\*(7|PANTHER|VIPER|GUARD|WORKER|CARETAKER)(?:\s*[—-]\s*([^:*]+?))?:\*\*\s*(.+)$")
for raw in text.split("\n"):
    s = raw.strip()
    m = re.match(r"^\*\*(\d+)\.\s+(.+?)\*\*(?:\s.*)?$", s)
    if m:
        sec = int(m.group(1)); sub = None
        scenes.append({"id": f"MAR_{sec:02d}", "title": m.group(2).capitalize(), "round": 15}); continue
    if s.startswith("**IMPLEMENTATION GUARDRAILS"): sec = None
    r = row_re.match(s)
    if r and sec == 13:
        lid, who, said, act = r.group(1), r.group(2), r.group(3), r.group(4)
        sid = lid[:lid.rfind("_")]
        if not any(x["id"] == sid for x in scenes): scenes.append({"id": sid, "title": SCN13[sid], "round": 15})
        speaker, label, voice = SPK["7" if who == "7" else "PANTHER"]
        lines.append({"id": lid, "scene": sid, "speaker": speaker, "label": label, "text": said, "direction": vocal(act), "action": act, "voice": voice})
        continue
    r2 = any_row_re.match(s)
    if r2 and sec is not None:
        lid, who, said, act = r2.group(1), r2.group(2), r2.group(3), r2.group(4)
        sid = lid[:lid.rfind("_")]
        speaker, label, voice = SPK[WHO[who]]
        lines.append({"id": lid, "scene": sid, "speaker": speaker, "label": label, "text": said, "direction": tone(act), "action": act, "voice": voice})
        continue
    m = line_re.match(s)
    if not m or sec is None: continue
    who, direction, said = m.group(1), (m.group(2) or "").strip(), m.group(3).strip()
    sid = f"MAR_{sec:02d}" + (sub if sec == 13 and sub else "")
    if sec == 13 and sub and not any(x["id"] == sid for x in scenes):
        scenes.append({"id": sid, "title": {"U": "Destination choice (Europa unfinished)", "C": "Destination choice (Europa complete)", "B": "Destination choice (both)"}[sub], "round": 15})
    n[sid] = n.get(sid, 0) + 1
    speaker, label, voice = SPK[who]
    lines.append({"id": f"{sid}_{n[sid]:02d}", "scene": sid, "speaker": speaker, "label": label, "text": said,
                  "direction": direction, "voice": voice})

os.makedirs(os.path.dirname(OUT), exist_ok=True)
json.dump({"scenes": scenes, "lines": lines}, open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
with open(DOC, "w", encoding="utf-8") as f:
    f.write("# Mars — the lines (generated by Tools/Voice/make_mars_lines.py from Docs/CHAPTER_MARS.md)\n\n")
    f.write("Voices: 7 = Seven (Eleven v3, directions as audio tags) · Panther = Cat (telepathic treatment in Unity) · Viper = "
            "Dan's saved voice \"Viper\" · Guard / Worker / Caretaker = premade ElevenLabs voices picked by "
            "`Tools/Voice/generate_mars_voices.py` (no new saved voices). Files: `Unity/Assets/Resources/MarsVoice/<id>.mp3`.\n\n")
    for sc in scenes:
        ls = [l for l in lines if l["scene"] == sc["id"]]
        f.write(f"## {sc['id']} — {sc['title']}\n\n")
        if not ls: f.write("(no lines)\n\n"); continue
        f.write("| id | speaker | line | direction |\n|---|---|---|---|\n")
        for l in ls: f.write(f"| `{l['id']}` | {l['label']} | {l['text']} | {l['direction']} |\n")
        f.write("\n")
print(f"{len(scenes)} scenes, {len(lines)} lines -> {OUT}")
for sc in scenes: print(sc["id"], len([l for l in lines if l["scene"] == sc["id"]]), sc["title"])
