"""Image-quality gate (QF26 2) - the same measures the game logs per capture (Runtime/ImageGate.cs), run over capture folders.

Usage:  python Tools/image_gate.py <dir|png> [<dir|png> ...] [--report <file>] [--quiet]
Exit code 1 when any image fails. Measures on luminance (0..1) of a 640x360 downsample of each PNG:
  white  = share of the frame near-white (L >= 0.92)                   fails above WHITE_MAX (a blow-out / white-out)
  range  = tonal range, 95th - 5th percentile of L                      fails below RANGE_MIN (one flat tone: a fog bank, haze)
  edges  = share of pixels whose local difference |dx|+|dy| > 0.03      fails below EDGES_MIN when the range is also under
           RANGE_STRONG (nothing to see: a blank slab, a fog bank), or below EDGES_HARD whatever the range
  black  = share of the frame near-black (L <= 0.03)                    fails above BLACK_MAX (unless it is a fade / title)
Calibrated on Round 18's captures (QF26): the city reveal (edges 1.9 %), the public berth (5.2 %), the flat approach (range
0.27) and Dan's white-out (white 48 %) fail; medium shots of the characters (edges 10-17 %, range 0.4-0.6) pass.
The in-game gate adds what a PNG can't show: non-finite (NaN / Inf) pixels in the HDR frame, and the subject in frame.
Keep the thresholds here and in ImageGate.cs the same.
"""
import sys, os
import numpy as np
from PIL import Image

WHITE_MAX, RANGE_MIN, EDGES_MIN, BLACK_MAX = 0.20, 0.30, 0.06, 0.85
RANGE_STRONG, EDGES_HARD = 0.45, 0.01
# shots that are meant to be one tone: fades, title cards, the vision's bloom to white, video frames of the clips
EXEMPT = ("title", "fade", "first_frame", "black", "_to_white", "video_frame", "op01", "card", "menu")


def measure(path):
    im = Image.open(path).convert("L").resize((640, 360), Image.BILINEAR)
    L = np.asarray(im, dtype=np.float32) / 255.0
    white = float((L >= 0.92).mean())
    black = float((L <= 0.03).mean())
    p5, p95 = np.percentile(L, [5, 95])
    g = np.abs(np.diff(L, axis=0))[:, :-1] + np.abs(np.diff(L, axis=1))[:-1, :]
    return white, float(p95 - p5), float((g > 0.03).mean()), black


def verdict(name, white, rng, edges, black):
    n = name.lower()
    if any(e in n for e in EXEMPT):
        return True, "exempt (a fade / title / video)"
    why = []
    if white > WHITE_MAX: why.append(f"white {white*100:.0f}% > {WHITE_MAX*100:.0f}%")
    insert = "_insert_" in n          # a minimal close-up of one thing: range / edges do not apply (the game checks its subject)
    if not insert and rng < RANGE_MIN: why.append(f"range {rng:.2f} < {RANGE_MIN:.2f}")
    # "nothing to see": little detail AND little contrast (a smooth, strongly lit shape - the cave mouth, the asteroid - is fine)
    if not insert and ((edges < EDGES_MIN and rng < RANGE_STRONG) or edges < EDGES_HARD): why.append(f"edges {edges*100:.1f}% < {EDGES_MIN*100:.0f}% with range {rng:.2f} < {RANGE_STRONG:.2f} (or < {EDGES_HARD*100:.0f}%)")
    if black > BLACK_MAX: why.append(f"black {black*100:.0f}% > {BLACK_MAX*100:.0f}%")
    return not why, "; ".join(why) or "ok"


def main(argv):
    paths, report, quiet = [], None, False
    it = iter(argv)
    for a in it:
        if a == "--report": report = next(it)
        elif a == "--quiet": quiet = True
        else: paths.append(a)
    files = []
    for p in paths:
        if os.path.isdir(p):
            files += [os.path.join(p, f) for f in sorted(os.listdir(p)) if f.lower().endswith(".png")]
        elif p.lower().endswith(".png"):
            files.append(p)
    lines, fails = [], 0
    for f in files:
        w, rg, e, b = measure(f)
        ok, why = verdict(os.path.basename(f), w, rg, e, b)
        fails += 0 if ok else 1
        line = f"{'PASS' if ok else 'FAIL'}  white {w*100:5.1f}%  range {rg:4.2f}  edges {e*100:5.1f}%  black {b*100:5.1f}%  {f}  {'' if ok else '<- ' + why}"
        lines.append(line)
        if not quiet or not ok: print(line)
    summary = f"IMAGE GATE (QF26 2): {len(files)} images, failed {fails}"
    print(summary)
    if report:
        with open(report, "w", encoding="utf-8") as fh: fh.write("\n".join(lines + [summary]) + "\n")
    return 1 if fails else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
