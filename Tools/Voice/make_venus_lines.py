"""Round 11: Venus dialogue as data, from Docs/CHAPTER2_VENUS.md.

Usage (repo root):  python Tools/Voice/make_venus_lines.py
Writes Unity/Assets/Resources/Venus/venus_dialogue.json (loaded by the game; Dan may edit it directly - lines, order,
labels, delivery, camera cues - and the game picks the edit up on the next build) and Docs/VENUS-LINES.md (readable list).

Per line: id (stable, exactly as in the spec), scene, speaker (seven / cat / guardian / attendant / wormmaster),
label (subtitle name: "7", "Cat", "Panther" from VEN_B01_07 on, "Guardian", ...), text, delivery
("spoken" | "telepathic" | "in-gameplay"; a cat line in an in-gameplay scene stays "telepathic"), voice (ElevenLabs
voice name), cue (optional camera cue, may be empty: cues are presentation only - a scene never waits on one).
Per scene: id, title, trigger, result, delivery, cues (the spec's Camera list), round (11 / 12).
Re-running overwrites the JSON: if Dan has edited it by hand, keep a copy first (or edit the spec instead).
"""
import json, os, re

ROOT = os.getcwd()
SPEC = os.path.join(ROOT, "Docs", "CHAPTER2_VENUS.md")
OUT = os.path.join(ROOT, "Unity", "Assets", "Resources", "Venus", "venus_dialogue.json")
DOC = os.path.join(ROOT, "Docs", "VENUS-LINES.md")
VOICE = {"seven": "Seven", "cat": "Cat", "guardian": "Guardian", "attendant": "Attendant", "wormmaster": "Wormmaster"}
ROUND11 = {"VEN_C01", "VEN_C02", "VEN_C03", "VEN_C04", "VEN_C05", "VEN_B01", "VEN_B02", "VEN_B03", "VEN_B04", "VEN_B05"}


def speaker_of(s):
    s = s.lower()
    if s.startswith("7"): return "seven"
    if s.startswith("cat") or s.startswith("panther"): return "cat"
    for k in ("guardian", "attendant", "wormmaster"):
        if s.startswith(k): return k
    raise ValueError(s)


def main():
    text = open(SPEC, encoding="utf-8").read().replace("\r\n", "\n")
    lines = text.split("\n")
    scenes, out, cur = {}, [], None
    in_cam = False
    i = 0
    while i < len(lines):
        l = lines[i].strip()
        m = re.match(r"^SCENE (VEN_[BC]\d\d) — (.+)$", l)
        if m:
            cur = {"id": m.group(1), "title": m.group(2).strip(), "trigger": "", "result": "", "delivery": "cutscene",
                   "cues": [], "round": 11 if m.group(1) in ROUND11 else 12}
            scenes[cur["id"]] = cur; in_cam = False
        elif cur is not None:
            if l.startswith("Trigger:"): cur["trigger"] = l[8:].strip()
            elif l.startswith("Result:"): cur["result"] = l[7:].strip()
            elif l.startswith("Delivery:") and "gameplay" in l.lower(): cur["delivery"] = "in-gameplay"
            elif l.startswith("Camera:"):
                in_cam = True
                rest = l[7:].strip()
                if rest: cur["cues"].append(rest); in_cam = False
            elif in_cam and l.startswith("- "): cur["cues"].append(l[2:].strip())
            elif in_cam and l and not l.startswith("- "): in_cam = False
        m = re.match(r"^(VEN_[BC]\d\d)_([A-Z]?\d+) \| (.+?)\s*$", l)
        if m:
            # Round 13: VEN_C13 has branch lines (VEN_C13_M1.. / VEN_C13_E1..) after the player's choice
            sid, suf, who = m.group(1), m.group(2), m.group(3)
            n = int(suf) if suf.isdigit() else 900 + int(suf[1:])
            j = i + 1
            while j < len(lines) and not lines[j].strip(): j += 1
            t = lines[j].strip().strip("“”\"")
            spk = speaker_of(who)
            sc = scenes.get(sid, {})
            tele = "telepathic" in who.lower()
            # the cat's name: "Cat" until VEN_B01_07, "Panther" from then on (every later scene and chapter)
            if spk == "cat":
                label = "Panther" if (sid == "VEN_B01" and n >= 7) or sid not in ("VEN_C01", "VEN_B01") else "Cat"
            else:
                label = {"seven": "7", "guardian": "Guardian", "attendant": "Attendant", "wormmaster": "Wormmaster"}[spk]
            delivery = "telepathic" if tele else ("in-gameplay" if sc.get("delivery") == "in-gameplay" else "spoken")
            # Round 13 (Dan's B01 rewrite): "7 — casually" / "Panther, telepathic — dryly": the words after the dash are
            # the delivery direction (used for the voice take: Eleven v3 audio tags)
            direction = who.split("—", 1)[1].strip() if "—" in who else ""
            out.append({"id": f"{sid}_{n:02d}" if suf.isdigit() else f"{sid}_{suf}", "scene": sid, "speaker": spk, "label": label, "text": t,
                        "delivery": delivery, "voice": VOICE[spk], "cue": "", "direction": direction})
            i = j
        i += 1
    # C02's trigger moved into the ride (VEN_B03); C01 cues on its first lines (presentation only)
    first_cue = {"VEN_C01_01": "over 7's shoulder toward the gardens", "VEN_C01_02": "medium two-shot of 7 and Cat",
                 "VEN_C01_04": "landscape insert", "VEN_C01_05": "cat head tracking flying creatures",
                 "VEN_C03_01": "wide two-shot at temple entrance", "VEN_C03_03": "insert of dodecahedron",
                 "VEN_C03_04": "over 7's shoulder toward guardian", "VEN_C03_10": "close view of object between them",
                 "VEN_C04_01": "over 7's shoulder toward cube", "VEN_C04_02": "matching inserts of related markings",
                 "VEN_C04_05": "brief eye close-up", "VEN_C04_07": "falling dust insert",
                 "VEN_B01_01": "wide - saucer and landscape", "VEN_B01_05": "low three-quarter on the bike",
                 "VEN_B01_19": "side medium - 7 mounting"}
    for o in out:
        o["cue"] = first_cue.get(o["id"], "")
    data = {"note": "Round 11 Venus dialogue - generated from Docs/CHAPTER2_VENUS.md by Tools/Voice/make_venus_lines.py; "
                    "editable: text, label, delivery, cue, order. Cues are presentation only.",
            "scenes": list(scenes.values()), "lines": out}
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    json.dump(data, open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    md = ["# Venus dialogue (Round 11 + 12) — generated by `Tools/Voice/make_venus_lines.py`", "",
          "Data: `Unity/Assets/Resources/Venus/venus_dialogue.json`. Round 11 builds the scenes marked R11.", ""]
    for s in scenes.values():
        md.append(f"## {s['id']} — {s['title']} (R{s['round']}, {s['delivery']})")
        if s["trigger"]: md.append(f"Trigger: {s['trigger']}  ")
        if s["result"]: md.append(f"Result: {s['result']}  ")
        if s["cues"]: md.append("Camera: " + "; ".join(s["cues"]))
        md.append("")
        for o in out:
            if o["scene"] == s["id"]:
                md.append(f"- `{o['id']}` **{o['label']}** ({o['delivery']}): {o['text']}" + (f" — *cue: {o['cue']}*" if o["cue"] else ""))
        md.append("")
    open(DOC, "w", encoding="utf-8").write("\n".join(md))
    r11 = [o for o in out if o["scene"] in ROUND11]
    print(f"{len(scenes)} scenes, {len(out)} lines ({len(r11)} in Round 11) -> {os.path.relpath(OUT, ROOT)}")
    for sp in VOICE:
        print(f"  {sp}: {sum(1 for o in r11 if o['speaker'] == sp)} Round 11 lines")


if __name__ == "__main__":
    main()
