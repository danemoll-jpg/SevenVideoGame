"""Round 17: the Biavian mothership (Veyra) chapter's dialogue as data (a copy of make_asteroid_lines.py).

Usage (repo root): python Tools/Voice/make_biaveh_lines.py
Reads Docs/CHAPTER_BIAVEH.md (Dan's script BIA_01..BIA_13A / BIA_13B; the "SETTING CORRECTION + REVISED BIA_01 / BIA_02"
section replaces the original BIA_01 / BIA_02 - the first row of an id wins, and the revised section comes first) and writes
  Unity/Assets/Resources/Biaveh/biaveh_dialogue.json  (loaded at runtime)
  Docs/BIAVEH-LINES.md                                 (the line list for Dan)
Every table row "| BIA_.. | Speaker | Dialogue | Performance |" is a line; rows whose dialogue is "—" are beats (no line).
Scene id = the id up to its second "_" (BIA_13A_01 -> BIA_13A).
Speakers: 7 -> seven (voice "Seven", v3 + tags), Panther -> cat ("Cat", telepathic), Guide -> nela (label "Nela", a premade
voice, telepathic in Unity), Ratwing -> ratwing (saved voice "Ratwing" or a premade), Resident -> resident1 (each distinct
resident character gets its own id resident1, resident2, ... and its own premade voice in generate_biaveh_voices.py).
"""
import json, os, re

ROOT = os.getcwd()
SRC = os.path.join(ROOT, "Docs", "CHAPTER_BIAVEH.md")
OUT = os.path.join(ROOT, "Unity", "Assets", "Resources", "Biaveh", "biaveh_dialogue.json")
DOC = os.path.join(ROOT, "Docs", "BIAVEH-LINES.md")
SPK = {"7": ("seven", "7", "Seven"), "Panther": ("cat", "Panther", "Cat"), "Guide": ("nela", "Nela", "nela"),
       "Nela": ("nela", "Nela", "nela"), "Ratwing": ("ratwing", "Ratwing", "Ratwing")}
RESIDENTS = {"Resident": "resident1"}  # spec name -> id; add "Resident 2": "resident2" etc. for more resident characters
TITLES = {"BIA_13A": "Departure (asteroid unfinished)", "BIA_13B": "Departure (asteroid complete: the Cattalus lead)"}
VOCAL = ("laugh", "giggle", "chuckle", "exhale", "sigh", "breath", "snort", "hum", "dry", "soft", "quiet", "low", "amused",
         "gentle", "warm", "tired", "urgent", "tight", "frightened", "firm", "flat", "curious", "innocent", "delighted", "wonder",
         "practical", "focused", "exertion", "breathless", "patient", "measured", "serious", "pointed", "honest", "calm", "light", "wry",
         "relief", "relieved", "reassur", "concern", "anger", "astonished", "nervous", "weary", "dignified", "composed", "precise",
         "sharp", "sincere", "reflect", "intent", "impressed", "satisf", "disbelief", "thin", "casual", "matter", "unpressured",
         "accepting", "grins", "mild", "lightly", "softly", "softens", "immediate", "entirely", "absolutely", "very", "protest",
         "suppressed", "recognition", "humour", "pleased", "attentive", "compos", "hesita", "smile")
STAGE = ("leans", "looks", "glances", "turns", "steps", "holds", "points", "moves", "blocks", "stops", "scans", "waits", "searches",
         "pushes", "lifts", "places", "finds", "rubs", "selects", "brings", "sits", "watches", "indicates", "lets", "camera", "his ",
         "her hand", "studying", "looking at", "kneels", "mounts", "runs", "opens", "settles", "gesture", "strike", "fingers",
         "tail", "eyes", "shoulders", "approaches", "keeps", "checks", "walks", "inclines", "crouches", "spreads", "hand ", "starts")
row = re.compile(r"^\|\s*(BIA_\w+?)\s*\|\s*([^|]+?)\s*\|\s*(.+?)\s*\|\s*(.*?)\s*\|$")
head = re.compile(r"^#{2,4}\s+(?:Revised\s+)?(BIA_\d+)\s+—\s+(.+?)\s*$")


def vocal(cell):
    keep = []
    for p in re.split(r"[;.]", cell):
        q = p.strip().lower()
        words = re.findall(r"[a-z]+", q)
        if q and any(any(x.startswith(w) for x in words) for w in VOCAL) and not any(w in q for w in STAGE) and "slows" not in q:
            keep.append(re.sub(r"(?i),?\s*telepathic,?\s*", "", p.strip()))
    t = ", ".join(k for k in keep if k)
    t = re.sub(r"\s*\(.*?\)", "", t)
    t = re.split(r",? (?:after|once|without|hauling|followed by|then)", t)[0] if not t.lower().startswith("sigh") else t
    return t.strip(" ,")


lines, seen, titles, unknown = [], set(), dict(TITLES), []
for raw in open(SRC, encoding="utf-8").read().replace("\r\n", "\n").split("\n"):
    h = head.match(raw.strip())
    if h and h.group(1) not in titles: titles[h.group(1)] = h.group(2)
    m = row.match(raw.strip())
    if not m: continue
    lid, who, said, act = m.groups()
    if who == "—" or said.strip() in ("—", "-"): continue
    if lid in seen: continue  # the revised BIA_01 / BIA_02 come first: older duplicates are ignored
    seen.add(lid)
    sid = "_".join(lid.split("_")[:2])
    if who in SPK: speaker, label, voice = SPK[who]
    elif who in RESIDENTS: speaker, label, voice = RESIDENTS[who], who, RESIDENTS[who]
    else: unknown.append(f"{lid} ({who})"); continue
    lines.append({"id": lid, "scene": sid, "speaker": speaker, "label": label, "text": said, "direction": vocal(act), "action": act, "voice": voice})

scenes = []
for l in lines:
    if not any(s["id"] == l["scene"] for s in scenes): scenes.append({"id": l["scene"], "title": titles.get(l["scene"], l["scene"]), "round": 17})
os.makedirs(os.path.dirname(OUT), exist_ok=True)
json.dump({"scenes": scenes, "lines": lines}, open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
with open(DOC, "w", encoding="utf-8") as f:
    f.write("# The Biavian Mothership (Veyra) — the lines (generated by Tools/Voice/make_biaveh_lines.py from Docs/CHAPTER_BIAVEH.md)\n\n")
    f.write("Voices: 7 = Seven (Eleven v3, performance notes as audio tags) · Panther = Cat (telepathic treatment in Unity) · "
            "Nela (the Guide) = a premade calm, warm female voice (telepathic in Unity) · Ratwing = the saved voice \"Ratwing\" or a "
            "premade fallback · residents = one premade voice each (`Tools/Voice/generate_biaveh_voices.py`). "
            "Files: `Unity/Assets/Resources/BiavehVoice/<id>.mp3`.\n\n")
    for sc in scenes:
        ls = [l for l in lines if l["scene"] == sc["id"]]
        f.write(f"## {sc['id']} — {sc['title']}\n\n| id | speaker | line | direction |\n|---|---|---|---|\n")
        for l in ls: f.write(f"| `{l['id']}` | {l['label']} | {l['text']} | {l['direction']} |\n")
        f.write("\n")
print(f"{len(scenes)} scenes, {len(lines)} lines -> {OUT}")
for sc in scenes: print(sc["id"], len([l for l in lines if l["scene"] == sc["id"]]), sc["title"])
if unknown: print("UNKNOWN SPEAKERS (skipped):", unknown)
