"""QF29 (2): the release image gate's scorer (called by Tools/Run-ImageGate.ps1).

Policy (Dan, QF29): the gate blocks NEW or WORSE failures, not the known baseline.
  - every check run's capture_log.txt lists "IMAGE GATE <file>: PASS / FAIL - white W %, range R, edges E %, black B %, ... subject ...".
  - a failing shot is allowed only if it is in Tools/image_gate_baseline.json (the shots that failed when the gate came in, QF26;
    the list only ever shrinks) AND it is not worse than its reference in Tools/image_gate_reference.json (recorded once, QF29, from
    the first deterministic run: which criteria it failed and by how much). Worse = it fails a criterion it did not fail before, or a
    failed measure moved further past its threshold (range by > 0.02, edges / white / black by > 1 point).
  - NaN / Inf pixels anywhere fail the release; a chapter with no baseline (Cattalus, the Moon finale) must pass every shot.
  - the thresholds are the in-game gate's (ImageGate.cs) and are never changed here.
Writes Builds/ImageGate/gate_<version>.txt ending "IMAGE GATE RELEASE (QF26 2): PASS / FAIL" with the baseline count; exit 1 on FAIL.
Usage: python Tools/image_gate_score.py <capture root> <version> <run names comma-separated> [--write-reference]
"""
import json, os, re, sys, pathlib

ROOT = pathlib.Path(__file__).resolve().parent.parent
BASE = ROOT / "Tools" / "image_gate_baseline.json"
REF = ROOT / "Tools" / "image_gate_reference.json"
LINE = re.compile(r"IMAGE GATE (\S+\.png): (PASS|FAIL) - white ([\d.]+) %, range ([\d.]+), edges ([\d.]+) %, black ([\d.]+) %(.*)$")


def criteria(white, rng, edges, black, rest):
    c = {}
    if white > 20.0: c["white"] = white - 20.0
    if rng < 0.30: c["range"] = 0.30 - rng
    if edges < 1.0 or (edges < 6.0 and rng < 0.45): c["edges"] = 6.0 - edges
    if black > 85.0: c["black"] = black - 85.0
    if "NaN / Inf" in rest and not re.search(r"non-finite pixels 0\b", rest): c["nan"] = 1.0
    if re.search(r"subject [^;]*(blocked|out of frame|no character|too small|readable size)", rest) and not re.search(r"in frame, unblocked", rest): c["subject"] = 1.0
    return c


def main():
    cap, version, names = pathlib.Path(sys.argv[1]), sys.argv[2], [n for n in sys.argv[3].split(",") if n]
    write_ref = "--write-reference" in sys.argv
    baseline = json.loads(BASE.read_text(encoding="utf-8-sig")) if BASE.exists() else {}
    ref = json.loads(REF.read_text(encoding="utf-8")) if REF.exists() else {}
    base_count = sum(len(v) for v in baseline.values())
    out, release, new_ref = [], True, {}
    for k in names:
        log = cap / k / "capture_log.txt"
        if not log.exists():
            out.append(f"{k}: no capture_log.txt - the run did not finish"); release = False; continue
        shots, fails, new, worse, nan = 0, [], [], [], 0
        known = set(baseline.get(k, []))
        for line in log.read_text(encoding="utf-8", errors="replace").splitlines():
            m = LINE.search(line)
            if not m: continue
            shots += 1
            name = re.sub(r"^\d+_", "", m.group(1))
            if m.group(2) == "PASS": continue
            c = criteria(float(m.group(3)), float(m.group(4)), float(m.group(5)), float(m.group(6)), m.group(7))
            if "non-finite pixels" in m.group(7) and not re.search(r"non-finite pixels 0\b", m.group(7)): nan += 1; c["nan"] = 1.0
            if not c: c["other"] = 1.0
            fails.append(name)
            if name not in known: new.append(name); continue
            r = ref.get(k, {}).get(name)
            if write_ref and r is None:
                new_ref.setdefault(k, {})[name] = c; continue
            if r is None: continue   # (a baseline shot without a reference yet: recorded on the next --write-reference)
            tol = {"range": 0.02, "edges": 1.0, "white": 1.0, "black": 1.0, "subject": 0.0, "nan": 0.0, "other": 0.0}
            bad = [f"{cr} (newly failing)" for cr in c if cr not in r] + [f"{cr} {r[cr]:.2f} -> {c[cr]:.2f}" for cr in c if cr in r and c[cr] > r[cr] + tol.get(cr, 0.0)]
            if bad: worse.append(f"{name}: {', '.join(bad)}")
        passing_known = [n for n in known if n not in fails]
        ok = nan == 0 and not new and not worse
        if not ok: release = False
        out.append(f"{k}: IMAGE GATE (QF26 2): {shots} shots, failed {len(fails)} (known baseline {len(fails) - len(new)}, new {len(new)}, worse {len(worse)}, NaN {nan}, baseline shots now passing {len(passing_known)}) - {'ok' if ok else 'FAIL'}")
        out += [f"    new failure: {n}" for n in new] + [f"    worse than its reference: {w}" for w in worse]
        if passing_known: out.append(f"    baseline shots passing in this run (may be removed from the baseline once they pass twice): {', '.join(sorted(passing_known))}")
    if write_ref and new_ref:
        for k, v in new_ref.items(): ref.setdefault(k, {}).update(v)
        REF.write_text(json.dumps(ref, indent=1), encoding="utf-8"); out.append(f"reference written for {sum(len(v) for v in new_ref.values())} baseline shots: {REF}")
    out.append(f"baseline: {base_count} known shots (Tools/image_gate_baseline.json; only ever shrinks)")
    out.append(f"IMAGE GATE RELEASE (QF26 2): {'PASS' if release else 'FAIL'} - version {version}; policy QF29: no new or worse failure, no NaN; baseline {base_count}")
    rep = ROOT / "Builds" / "ImageGate" / f"gate_{version}.txt"
    rep.parent.mkdir(parents=True, exist_ok=True)
    rep.write_text("\n".join(out) + "\n", encoding="utf-8")
    print("\n".join(out)); print(f"report: {rep}")
    sys.exit(0 if release else 1)


if __name__ == "__main__":
    main()
