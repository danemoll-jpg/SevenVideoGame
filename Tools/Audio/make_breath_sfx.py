"""P2 (11): Panther's breathing as he is hurt / dies, made procedurally - band-passed noise with breath envelopes, so it can
never contain a meow (the ElevenLabs takes of cat_breath_slow / cat_breath_labored were tonal at 0.6-1.9 kHz: meows).
Writes Unity/Assets/Audio/SFX/cat_breath_slow.wav (one slow, soft, tired breath in and out, 1.6 s) and cat_breath_labored.wav
(two short rough laboured breaths, 2.4 s), 16-bit stereo 44.1 kHz like the other SFX.
Usage: python Tools/Audio/make_breath_sfx.py [repo]"""
import os, sys
import numpy as np
import scipy.io.wavfile as wav
from scipy.signal import butter, sosfilt

SR = 44100
rng = np.random.default_rng(1124)


def band(x, lo, hi, order=3):
    return sosfilt(butter(order, [lo, hi], btype="band", fs=SR, output="sos"), x)


def env(n, attack, release):
    t = np.linspace(0.0, 1.0, n)
    e = np.minimum(1.0, t / max(attack, 1e-3)) * np.minimum(1.0, (1.0 - t) / max(release, 1e-3))
    return np.clip(e, 0.0, 1.0) ** 1.6


def breath(dur, lo, hi, level, rough=0.0, attack=0.35, release=0.45):
    n = int(dur * SR)
    noise = rng.standard_normal(n)
    x = band(noise, lo, hi) + 0.35 * band(noise, lo * 0.5, lo * 1.2)      # airy body + a little chest
    if rough > 0:                                                           # a rasp: slow random amplitude flutter (no pitch)
        flutter = band(rng.standard_normal(n), 18.0, 45.0, 2)
        x *= 1.0 + rough * flutter / (np.abs(flutter).max() + 1e-9)
    x *= env(n, attack, release)
    return level * x / (np.abs(x).max() + 1e-9)


def place(total, parts):
    out = np.zeros(int(total * SR))
    for start, sig in parts:
        i = int(start * SR); j = min(len(out), i + len(sig)); out[i:j] += sig[: j - i]
    return out


def save(path, mono):
    mono = mono / max(1.0, np.abs(mono).max() / 0.9)
    st = np.stack([mono, mono * 0.97], axis=1)
    wav.write(path, SR, (st * 32767).astype(np.int16))
    print("wrote", path, f"{len(mono) / SR:.2f} s")


def main():
    repo = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "..", "..")
    sfx = os.path.join(repo, "Unity", "Assets", "Audio", "SFX")
    # one slow, soft, tired breath: in (higher, airy, quieter) then out (lower, longer)
    slow = place(1.6, [(0.05, breath(0.62, 700, 2600, 0.45)), (0.72, breath(0.82, 380, 1700, 0.6, attack=0.2, release=0.6))])
    save(os.path.join(sfx, "cat_breath_slow.wav"), slow)
    # two short rough laboured breaths: quicker, ragged, a catch at the start of each
    lab = place(2.4, [(0.02, breath(0.42, 600, 2400, 0.55, rough=0.8, attack=0.15)), (0.46, breath(0.55, 350, 1500, 0.7, rough=0.9, attack=0.12)),
                      (1.2, breath(0.4, 600, 2400, 0.5, rough=0.8, attack=0.15)), (1.62, breath(0.66, 330, 1400, 0.65, rough=1.0, attack=0.12, release=0.6))])
    save(os.path.join(sfx, "cat_breath_labored.wav"), lab)


if __name__ == "__main__":
    main()
