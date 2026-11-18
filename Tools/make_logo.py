"""Round 10 (Dan): the game logo - a large "7" with "THE OBSERVER" underneath, both Arial Bold, monochrome.
Typeset once from Windows' Arial Bold (C:/Windows/Fonts/arialbd.ttf) into images; the font itself is NEVER copied into
the repo or the build (its licence does not allow redistribution).

  python Tools/make_logo.py
-> Art/Logo/logo_7_observer.png        light grey (230) on transparent, 2048 px wide (the default)
   Art/Logo/logo_7_observer_white.png  white on transparent
   Art/Logo/logo_7_observer_dark.png   near-black (26) on transparent (for light backgrounds)
   Art/Logo/logo_7.png                 the "7" alone, white on transparent (icons)
   + copies of the white logo for the game: Unity/Assets/Resources/Logo/logo_7_observer_white.png
"""
from pathlib import Path
import shutil
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Art" / "Logo"
FONT = Path("C:/Windows/Fonts/arialbd.ttf")
W = 2048
SEVEN_PX = 1300          # "7" font size
SUB_PX = 120             # "THE OBSERVER" font size
TRACK = 0.38             # extra letter spacing, in em
GAP = 70                 # between the 7 and the line under it


def mask():
    f7, fs = ImageFont.truetype(str(FONT), SEVEN_PX), ImageFont.truetype(str(FONT), SUB_PX)
    big = Image.new("L", (W, 2600), 0); d = ImageDraw.Draw(big)
    l, t, r, b = d.textbbox((0, 0), "7", font=f7)
    x7 = (W - (r - l)) / 2 - l
    d.text((x7, 120 - t), "7", font=f7, fill=255)
    y = 120 + (b - t) + GAP
    text = "THE OBSERVER"
    adv = [d.textlength(c, font=fs) for c in text]
    total = sum(adv) + TRACK * SUB_PX * (len(text) - 1)
    x = (W - total) / 2
    _, st, _, _ = d.textbbox((0, 0), "T", font=fs)
    for c, a in zip(text, adv):
        d.text((x, y - st), c, font=fs, fill=255)
        x += a + TRACK * SUB_PX
    bb = big.getbbox()
    m = 60
    return big.crop((0, max(0, bb[1] - m), W, min(big.height, bb[3] + m))), f7


def save(m, gray, name):
    img = Image.new("LA", m.size, (gray, 0))
    img.putalpha(m)
    img = Image.merge("LA", (Image.new("L", m.size, gray), m))
    img.save(OUT / name)
    return OUT / name


def main():
    if not FONT.exists():
        raise SystemExit("Arial Bold not found at " + str(FONT))
    OUT.mkdir(parents=True, exist_ok=True)
    m, f7 = mask()
    save(m, 230, "logo_7_observer.png")
    white = save(m, 255, "logo_7_observer_white.png")
    save(m, 26, "logo_7_observer_dark.png")
    # the "7" alone, square
    s = 1024
    sq = Image.new("L", (s, s), 0); d = ImageDraw.Draw(sq)
    f = ImageFont.truetype(str(FONT), int(s * 0.95))
    l, t, r, b = d.textbbox((0, 0), "7", font=f)
    d.text(((s - (r - l)) / 2 - l, (s - (b - t)) / 2 - t), "7", font=f, fill=255)
    Image.merge("LA", (Image.new("L", (s, s), 255), sq)).save(OUT / "logo_7.png")
    res = ROOT / "Unity" / "Assets" / "Resources" / "Logo"
    res.mkdir(parents=True, exist_ok=True)
    shutil.copy2(white, res / "logo_7_observer_white.png")
    print("logo:", m.size, "->", OUT)


if __name__ == "__main__":
    main()
