"""Placeholder Elarim mark (Round 10): a small circle with three short curved strokes rising from it, like a sprout
("First Garden Age"). Kyle hasn't designed the real mark yet: replace Art/Marks/elarim_mark_placeholder.png (white on
transparent, square) or point MoonLayout.ElarimMarkFile at the new file. Usage: python Tools/Model/make_elarim_mark.py"""
from PIL import Image, ImageDraw
import math, os, sys
ROOT = sys.argv[1] if len(sys.argv) > 1 else os.getcwd()
S = 1024; W = 44
im = Image.new("LA", (S, S), (255, 0)); d = ImageDraw.Draw(im)
cx, cy, r = S / 2, S * 0.68, S * 0.11
d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=(255, 255), width=W)
top = cy - r
for side, L in ((-1, 0.24), (0, 0.30), (1, 0.24)):
    pts = []
    for i in range(25):
        t = i / 24
        x = cx + side * S * 0.15 * (t ** 1.6)        # the side strokes lean outward as they rise, like leaves
        y = top - W * 0.3 - t * S * L
        pts.append((x, y))
    d.line(pts, fill=(255, 255), width=W, joint="curve")
    for p in (pts[0], pts[-1]):
        d.ellipse([p[0] - W / 2, p[1] - W / 2, p[0] + W / 2, p[1] + W / 2], fill=(255, 255))
out = os.path.join(ROOT, "Art", "Marks", "elarim_mark_placeholder.png")
os.makedirs(os.path.dirname(out), exist_ok=True)
im.save(out)
print(out)
