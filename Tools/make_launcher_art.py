"""Round 8: the launcher's art, strictly monochrome (made in grayscale mode 'L', so R = G = B in every pixel).
  python Tools/make_launcher_art.py
-> Art/Launcher/Seven.ico (16..256 px; also copied to Launcher/Seven.ico for launcher.rc), Seven-Icon.png (256 px),
   Seven-Cover.png (840 px square, shown in the launcher window), Seven-Poster.png (600 x 900, Steam library grid).
A white "7" on black with a soft glow in fog (the world look: bright haze behind dark ground)."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Art/Launcher'
FONTS = ['bahnschrift.ttf', 'segoeuil.ttf', 'segoeui.ttf', 'arial.ttf']

def font(size):
    for name in FONTS:
        try: return ImageFont.truetype(str(Path('C:/Windows/Fonts') / name), size)
        except OSError: pass
    return ImageFont.load_default()

def glow(w, h, cx, cy, radius, peak):
    g = Image.new('L', (w, h), 0); d = ImageDraw.Draw(g)
    d.ellipse([cx - radius, cy - radius, cx + radius, cy + radius], fill=peak)
    return g.filter(ImageFilter.GaussianBlur(radius * 0.45))

def centered(d, text, f, cx, cy, fill):
    l, t, r, b = d.textbbox((0, 0), text, font=f)
    d.text((cx - (l + r) / 2, cy - (t + b) / 2), text, font=f, fill=fill)

def seven(size, haze=True):
    img = glow(size, size, size // 2, int(size * 0.55), int(size * 0.42), 70) if haze else Image.new('L', (size, size), 0)
    d = ImageDraw.Draw(img); centered(d, '7', font(int(size * 0.86)), size / 2, size * 0.5, 255)
    return img

def poster(w, h):
    img = glow(w, h, w // 2, int(h * 0.62), int(w * 0.62), 90)
    fog = Image.linear_gradient('L').resize((w, h)).point(lambda v: int(v * 0.28))   # ground fog brightening downwards
    img = Image.composite(Image.new('L', (w, h), 255), img, fog)
    d = ImageDraw.Draw(img)
    d.rectangle([0, int(h * 0.80), w, h], fill=8)                                    # dark foreground silhouette
    centered(d, '7', font(int(h * 0.48)), w / 2, h * 0.40, 255)
    centered(d, 'THE OBSERVER', font(int(w * 0.085)), w / 2, h * 0.70, 235)
    return img

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    icon = seven(256, haze=False)
    icon.convert('RGBA').save(OUT / 'Seven.ico', sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    (ROOT / 'Launcher/Seven.ico').write_bytes((OUT / 'Seven.ico').read_bytes())
    icon.convert('RGB').save(OUT / 'Seven-Icon.png')
    seven(840).convert('RGB').save(OUT / 'Seven-Cover.png')
    poster(600, 900).convert('RGB').save(OUT / 'Seven-Poster.png')
    for p in sorted(OUT.glob('Seven*.png')):
        im = Image.open(p).convert('RGB'); r, g, b = (c.tobytes() for c in im.split())
        worst = max(max(abs(x - y), abs(y - z)) for x, y, z in zip(r, g, b))
        print(f'{p.name}: {im.size[0]}x{im.size[1]}, max |R-G|,|G-B| = {worst}')
        if worst: raise SystemExit('Not monochrome: ' + p.name)

if __name__ == '__main__': main()
