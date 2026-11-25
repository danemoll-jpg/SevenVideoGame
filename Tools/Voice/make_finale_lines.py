"""Round 19: the Moon finale's dialogue as data (a copy of make_cattalus_lines.py).

Usage (repo root): python Tools/Voice/make_finale_lines.py
Reads Docs/CHAPTER_FINALE.md (Dan's script FIN_01..FIN_14) + the lines below that live outside its tables (FIN_13's spoken line,
7's sign-off FIN_15_01..06 from TODO.md Round 19 item 15, and Code's proposed lines "_P" that give every objective a story
reason) and writes
  Unity/Assets/Resources/Finale/finale_dialogue.json  (loaded at runtime)
  Docs/FINALE-LINES.md                                 (the line list for Dan)
Speakers: 7 -> seven, Panther -> cat (telepathic), Scree Worker -> worker (ScreeWorker), Second Scree Voice -> worker2
(ScreeWorker2), Archive -> archive, Tunnler / Tunneler -> tunneler, Elarim Voice -> guardian (the Venus Guardian), Machinist ->
machinist, Biavian Guide -> nela, Voice -> rest (RestVoice; never labelled on screen).
"""
import json, os, re

ROOT = os.getcwd()
SRC = os.path.join(ROOT, "Docs", "CHAPTER_FINALE.md")
OUT = os.path.join(ROOT, "Unity", "Assets", "Resources", "Finale", "finale_dialogue.json")
DOC = os.path.join(ROOT, "Docs", "FINALE-LINES.md")
SPK = {"7": ("seven", "7", "seven"), "Panther": ("cat", "Panther", "cat"), "Scree Worker": ("worker", "Scree worker", "screeworker"),
       "Second Scree Voice": ("worker2", "Scree elder", "ScreeWorker2"), "Archive": ("archive", "Archive", "Archive"),
       "Tunnler": ("tunneler", "Tunneler", "Tunneler"), "Tunneler": ("tunneler", "Tunneler", "Tunneler"),
       "Elarim Voice": ("guardian", "Guardian", "Guardian"), "Machinist": ("machinist", "Machinist", "machinist"),
       "Biavian Guide": ("nela", "Nela", "Nela"), "Voice": ("rest", "", "RestVoice"),
       # QF30 (19): the worlds that were silent in FIN_12's montage get their own voices (Dan's saved voices)
       "Caretaker": ("caretaker", "Caretaker", "caretaker"), "Keeper": ("keeper", "Keeper", "Keeper"), "Juniper": ("juniper", "Juniper", "juniper")}
VOCAL = ("laugh", "chuckle", "breath", "quiet", "quietly", "soft", "softly", "gentle", "dry", "amused", "warm", "tired", "bitter",
         "urgent", "weak", "frightened", "firm", "resolute", "surprised", "determined", "calm", "controlled", "anger", "sharper",
         "simple", "reassur", "sincere", "open", "respectful", "wonder", "grief", "breathless", "breaks", "whisper", "worn",
         "uncertain", "certainty", "restrained", "purposeful", "deliberate", "realization", "practical", "recognition", "humour",
         "unhurried", "familiar", "caught", "patient", "almost", "barely", "tears", "smile", "small", "clear")
STAGE = ("looks", "glances", "watches", "steps", "moves", "stops", "shows", "indicates", "returns", "meets", "touches", "hand",
         "places", "advances", "holds", "stands", "lowers", "starts", "tail", "camera", "beside", "toward", "searching", "settles")
REPEAT = re.compile(r"^(FIN_08_P[1-3]|FIN_11_07|FIN_13_03|FIN_11_06b)$")      # QF30: the Archive asks "Continue?" once more if she lingers      # Panther's fight calls may repeat; "Yes." (FIN_11_07) and "Thank you." (FIN_13_03) are the script's own repeats of earlier words
row = re.compile(r"^\|\s*(FIN_\w+?)\s*\|\s*([^|]+?)\s*\|\s*(.+?)\s*\|\s*(.*?)\s*\|$")
head = re.compile(r"^#{2,4}\s+(FIN_\w+?)\s+—\s+(.+?)\s*$")

EXTRA = [
    # FIN_13: the one spoken line of the pale light (Dan 2026-11-22: used; RestVoice; never named on screen)
    # QF31 (Dan): the Voice moves into heaven - over the rising wide shot (heaven_05-06), as they grow small in the sunrise
    # QF35 (QF34 9, Dan approved 2026-11-24): the Voice speaks ONCE in the chamber, as the column falls on her and Panther - she did not die,
    # her watch is over; she lifts her head to it, draws him close, they rise. Not shown or named on screen.
    ("FIN_13_V1", "Voice", "Seven. Your watch is over. You kept their story through every age, and now it lives on beyond you. Rise, and come home to rest.", "Deep, warm, slow and unhurried; loving, with a large soft space round it"),
    ("FIN_13_04", "Voice", "Well done, good and faithful servant! You were faithful over a few things; I will put you in charge of many things. Share your master's joy.", "Deep, warm, slow and unhurried; loving, with a large soft space round it"),
    # 7's sign-off at the end of the credits (Dan 2026-11-22; closing lines from Kyle's book "7" (2014); his punctuation kept)
    ("FIN_15_01", "7", "Remember who you are!", "Warm and close, tender, a little smile"),
    ("FIN_15_02", "7", "You are beyond this life.", "Warm, tender"),
    ("FIN_15_03", "7", "You are forever...", "Soft, unhurried"),
    ("FIN_15_04", "7", "...in love...", "Soft, a smile in it"),
    ("FIN_15_05", "7", "this is 7...", "Warm, playful"),
    ("FIN_15_06", "7", "...signing out!", "Bright and playful"),
    # Code's proposed lines (Dan to review): every objective's reason before it, every payoff after it
    ("FIN_03_P1", "Scree Worker", "The power for the old route runs through that panel. Switch it back.", "Tired, practical"),
    ("FIN_03_P2", "Scree Worker", "The gate lock rusted shut years ago. We were never strong enough to lift it.", "Bitter, tired"),
    ("FIN_03_P3", "Panther", "Narrow walkway. Watch the edge.", "Dry, gentle"),
    ("FIN_03_P4", "Scree Worker", "There. The old route is open. Go carefully.", "Quiet, sincere"),
    ("FIN_05_P1", "Panther", "One for every place we've been.", "Soft, warm"),
    ("FIN_08_P1", "Panther", "The drill - move!", "Urgent"),
    ("FIN_08_P2", "Panther", "He's going for the platform!", "Urgent"),
    ("FIN_08_P3", "Panther", "Over here, you lump of scrap!", "Defiant, dry"),
    ("FIN_08_P4", "Panther", "He's cut the conduit. The light's gone out on the left.", "Urgent, practical"),
    # (QF30 10: FIN_08_P5 "The old maintenance barrier still has power..." replaced by Dan's worker line FIN_08_P7 - the old id would have clashed with FIN_08_P1)
    ("FIN_08_P6", "Archive", "Connection restored.", "Restrained"),
    # QF30 - Dan's review of 1.0.0-rc1 (proposed by Dan; used unless he changes them)
    ("FIN_05_04a", "Panther", "That frame across the side bay. It's an old maintenance barrier.", "Thoughtful, observant"),
    ("FIN_05_04b", "7", "For sealing off that end of the chamber?", "Curious, quiet"),
    ("FIN_05_04c", "Panther", "And whatever's in it.", "Dry, quiet"),
    ("FIN_08_P7", "Scree Worker", "The old maintenance barrier! Open it and get him inside. We can lock it from here.", "Urgent, over the network"),
    ("FIN_10_08b", "7", "Panther, no!", "Broken, disbelieving; then she cries a little"),
    ("FIN_11_05a", "7", "You're staying with me.", "Softly, to him"),
    ("FIN_11_05b", "Tunneler", "Fine. Keep your history. You'll still need someone to run the drills.", "Sneering, still sure of himself"),
    ("FIN_11_05c", "Second Scree Voice", "We always have.", "Calm, not gloating"),
    ("FIN_11_06b", "Archive", "Continue?", "Softer"),
    ("FIN_12_03a", "Caretaker", "Both kingdoms can read it now. The same record, on both sides of the crossing.", "Quiet wonder"),
    ("FIN_12_03b", "Keeper", "We hear it. We will carry it in our songs.", "Slow, calm"),
    ("FIN_12_06a", "Juniper", "Oh... Panther. You went all the way.", "Quiet, moved"),
]
# QF30 (15): the exact v3 performance for lines that need more than one tag (the subtitle text stays the plain line)
TAGGED = {"FIN_10_08b": "[broken, disbelieving] Panther, no! [crying] [soft sobs] [shaky breath]"}


def vocal(cell):
    keep = []
    for p in re.split(r"[;.]", cell):
        q = p.strip().lower()
        words = re.findall(r"[a-z]+", q)
        if q and any(any(x.startswith(w) for x in words) for w in VOCAL) and not any(w in q for w in STAGE):
            keep.append(p.strip())
    return re.sub(r"\s*\(.*?\)", "", ", ".join(k for k in keep if k)).strip(" ,")


lines, seen, titles, unknown = [], set(), {}, []
rows = []
for raw in open(SRC, encoding="utf-8").read().replace("\r\n", "\n").split("\n"):
    h = head.match(raw.strip())
    if h and h.group(1) not in titles: titles[h.group(1)] = h.group(2)
    m = row.match(raw.strip())
    if m: rows.append(m.groups())
for lid, who, said, act in EXTRA: rows.append((lid, who, said, act))
# QF31 (Dan's rework of the ending): the carry, "You're staying with me", the walk back to the archive, the separate memory step
# (the Archive's "Continue?" / "Yes." / "All of it.") and the worker's exchange after his death go; "We'll look after him." /
# "Thank you." go with them (she takes him with her). Their audio is removed by generate_finale_voices.py --prune.
CUT = {"FIN_11_01", "FIN_11_02", "FIN_11_03", "FIN_11_04", "FIN_11_05", "FIN_11_05a", "FIN_11_06", "FIN_11_06b", "FIN_11_07", "FIN_11_08",
       "FIN_13_01", "FIN_13_02", "FIN_13_03"}
for lid, who, said, act in rows:
    if lid in CUT: continue
    if who in ("—", "-") or said.strip() in ("—", "-"): continue
    if lid in seen: continue
    seen.add(lid)
    sid = "_".join(lid.split("_")[:2])
    if who not in SPK: unknown.append(f"{lid} ({who})"); continue
    speaker, label, voice = SPK[who]
    text = said.replace("Tunnler", "Tunneler")
    lines.append({"id": lid, "scene": sid, "speaker": speaker, "label": label, "text": text, "direction": vocal(act) or (act if lid.startswith(("FIN_13_04", "FIN_13_V", "FIN_15", "FIN_03_P", "FIN_05_P", "FIN_08_P")) or lid[-1].isalpha() else ""),
                  "tagged": TAGGED.get(lid, ""),
                  "action": act, "voice": voice, "repeat": bool(REPEAT.match(lid)), "proposed": "_P" in lid or lid[-1].isalpha()})

order = lambda l: (int(l["scene"].split("_")[1]), l["id"])
lines.sort(key=order)
scenes = []
for l in lines:
    if not any(s["id"] == l["scene"] for s in scenes):
        scenes.append({"id": l["scene"], "title": titles.get(l["scene"], "7's sign-off" if l["scene"] == "FIN_15" else l["scene"]), "round": 19})
os.makedirs(os.path.dirname(OUT), exist_ok=True)
json.dump({"scenes": scenes, "lines": lines}, open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
with open(DOC, "w", encoding="utf-8") as f:
    f.write("# The Moon finale — the lines (generated by Tools/Voice/make_finale_lines.py from Docs/CHAPTER_FINALE.md)\n\n")
    f.write("Voices: 7 = seven (Eleven v3) · Panther = cat · the Scree worker = screeworker · the second Scree voice = ScreeWorker2 · "
            "the Archive = Archive · Tunneler = Tunneler · the Venus Guardian (\"Elarim Voice\") = Guardian · the Machinist = machinist · "
            "the Biavian Guide = Nela · FIN_13's voice = RestVoice (never named on screen). `_P` ids = Code's proposed lines (Dan to "
            "review). Files: `Unity/Assets/Resources/FinaleVoice/<id>.mp3` (`Tools/Voice/generate_finale_voices.py`).\n\n")
    for sc in scenes:
        f.write(f"## {sc['id']} — {sc['title']}\n\n| id | speaker | line | direction |\n|---|---|---|---|\n")
        for l in [l for l in lines if l["scene"] == sc["id"]]:
            f.write(f"| `{l['id']}` | {l['label'] or '(the voice)'} | {l['text']} | {l['direction']} |\n")
        f.write("\n")
print(f"{len(scenes)} scenes, {len(lines)} lines -> {OUT}")
for sc in scenes: print(sc["id"], len([l for l in lines if l["scene"] == sc["id"]]), sc["title"])
if unknown: print("UNKNOWN SPEAKERS (skipped):", unknown)
