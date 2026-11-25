"""QF36 (7: The Observer): seamless loops for the Grand Circuit's racing music. Copied from ShiftStorm's Tools/Music/make_loops.py
(itself from Tee'd Off's; the loop search and the seam checks are unchanged, ShiftStorm's file is not touched).

    python Tools/Music/make_gc_loops.py [name ...]

For each track it finds the tempo and the bar grid, then a loop region on bar lines with matching sound and loudness either side of
the seam and before the song's ending / fade-out, and writes (Unity/Assets/Audio/Music/):

  <name>_intro.ogg   the song up to the loop start (played once, from the starting lights)
  <name>_loop.ogg    the loop body; its last 100 ms crossfaded into the audio just before the loop start, so it repeats with no seam
  Tools/Music/gc_loops.json   tempo, loop points, seam checks;  Builds/audio_check/<name>_3loops.wav + _seam.wav for listening

MusicSystem.Play("<name>") sees <name>_loop in the bank and plays the intro, then the body sample-accurately (PlayScheduled), looping.
The source MP3s stay as they are (gc_asteroid is made from asteroid_city.mp3: the Asteroid Foundry keeps Dan's story track).
"""
import json
import os
import subprocess
import sys

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
MUSIC = os.path.join(REPO, "Unity", "Assets", "Audio", "Music")
OUT = MUSIC
CHECK = os.path.join(REPO, "Builds", "audio_check")
SR = 44100
XFADE = 0.10
TRACKS = {n: True for n in ["gc_menu", "gc_cattalus", "gc_moon", "gc_venus", "gc_mars", "gc_europa", "gc_asteroid", "gc_mothership"]}
SOURCES = {"gc_asteroid": "asteroid_city"}
PINNED = {}


def find_source(name):
    p = os.path.join(MUSIC, SOURCES.get(name, name) + ".mp3")
    if not os.path.exists(p):
        raise SystemExit(f"{p} not found")
    return p


def decode(path):
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", path, "-f", "f32le", "-ac", "2", "-ar", str(SR), "-"],
                         capture_output=True, check=True).stdout
    return np.frombuffer(raw, dtype=np.float32).reshape(-1, 2).copy()


def write_wav(path, x):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    pcm = (np.clip(x, -1, 1) * 32767).astype("<i2").tobytes()
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "s16le", "-ar", str(SR), "-ac", "2", "-i", "-", path], input=pcm, check=True)


def write_ogg(path, x):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    pcm = (np.clip(x, -1, 1) * 32767).astype("<i2").tobytes()
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "s16le", "-ar", str(SR), "-ac", "2", "-i", "-",
                    "-c:a", "libvorbis", "-q:a", "6", path], input=pcm, check=True)


HOP, NFFT = 512, 2048


def stft_mag(mono):
    win = np.hanning(NFFT).astype(np.float32)
    n = 1 + (len(mono) - NFFT) // HOP
    frames = np.lib.stride_tricks.as_strided(mono, shape=(n, NFFT), strides=(mono.strides[0] * HOP, mono.strides[0]))
    return np.abs(np.fft.rfft(frames * win, axis=1)).astype(np.float32)


def bands(mag, nb=40):
    """Log-spaced band energies (a rough mel spectrum) per frame."""
    freqs = np.fft.rfftfreq(NFFT, 1 / SR)
    edges = np.geomspace(60, 14000, nb + 1)
    out = np.zeros((mag.shape[0], nb), np.float32)
    for i in range(nb):
        m = (freqs >= edges[i]) & (freqs < edges[i + 1])
        if m.any():
            out[:, i] = mag[:, m].mean(axis=1)
    return np.log1p(out * 10)


def tempo_and_phase(flux):
    fps = SR / HOP
    ac = np.correlate(flux - flux.mean(), flux - flux.mean(), "full")[len(flux) - 1:]
    lags = np.arange(len(ac))
    bpm_of = lambda lag: 60 * fps / lag
    lo, hi = int(60 * fps / 180), int(60 * fps / 70)
    lag = lo + int(np.argmax(ac[lo:hi]))
    # refine to sub-frame with a parabola
    if 0 < lag < len(ac) - 1:
        a, b, c = ac[lag - 1], ac[lag], ac[lag + 1]
        den = a - 2 * b + c
        if abs(den) > 1e-9:
            lag = lag + float(np.clip(0.5 * (a - c) / den, -0.5, 0.5))
    period = lag  # frames per beat
    best, phase = -1, 0.0
    for ph in np.linspace(0, period, 64, endpoint=False):
        idx = np.round(np.arange(ph, len(flux), period)).astype(int)
        idx = idx[idx < len(flux)]
        s = flux[idx].sum()
        if s > best:
            best, phase = s, ph
    return bpm_of(lag), period / fps, phase / fps


def find_loop(x, bpm, beat_s, phase_s):
    mono = x.mean(axis=1)
    mag = stft_mag(mono)
    bd = bands(mag)
    fps = SR / HOP
    rms = moving_rms(mono, SR)
    dur = len(mono) / SR
    med = np.median(rms[int(SR * 5):int(len(rms) * 0.8)])
    # The usable end: before the fade-out / ending (loudness stays near the body's).
    usable_end = dur
    for t in np.arange(dur - 1, dur * 0.5, -0.25):
        if rms[int(t * SR)] > 0.75 * med:
            usable_end = t - 0.5
            break
    bar = beat_s * 4
    beats = np.arange(phase_s, dur, beat_s)
    bars = beats[::4]
    best = None
    ranked = []  # Round 18: every candidate, best first (the caller takes the first with a clean seam)
    for a in bars:
        if a < 2.0 or a > dur * 0.45:
            continue
        for b in bars:
            n = round((b - a) / bar)
            if b <= a or n < 16 or b > usable_end:  # at least 16 bars, so the loop is not obviously short
                continue
            fa, fb = int(a * fps), int(b * fps)
            w = int(bar * fps)
            if fb + w >= len(bd) or fa - w < 0:
                continue
            # The sound just before / after each point should match (the seam joins "before b" to "after a").
            pre = np.corrcoef(bd[fa - w:fa].ravel(), bd[fb - w:fb].ravel())[0, 1]
            post = np.corrcoef(bd[fa:fa + w].ravel(), bd[fb:fb + w].ravel())[0, 1]
            loud = min(rms[int(a * SR)], rms[int(b * SR)]) / max(1e-6, max(rms[int(a * SR)], rms[int(b * SR)]))
            score = pre + post + 0.5 * loud + 0.004 * (b - a)  # prefer longer loops a little
            ranked.append((score, a, b, pre, post, loud))
            if best is None or score > best[0]:
                best = (score, a, b, pre, post, loud)
    ranked.sort(key=lambda c: -c[0])
    find_loop.ranked = ranked
    return best, usable_end


def moving_rms(mono, n):
    """RMS over a centred window of n samples (running sum: fast on long tracks)."""
    c = np.concatenate([[0.0], np.cumsum(mono.astype(np.float64) ** 2)])
    i = np.arange(len(mono))
    lo, hi = np.clip(i - n // 2, 0, len(mono)), np.clip(i + n // 2, 0, len(mono))
    return np.sqrt((c[hi] - c[lo]) / np.maximum(1, hi - lo)).astype(np.float32)


def zero_cross(x, i, radius=200):
    m = x[max(0, i - radius):i + radius].mean(axis=1)
    s = np.where(np.diff(np.signbit(m)))[0]
    if len(s) == 0:
        return i
    return max(0, i - radius) + int(s[np.argmin(np.abs(s - radius))])


def make_body(x, a, b):
    """Loop body x[a:b] whose last XFADE seconds crossfade (equal power) into x[a-X:a], so the end flows into sample 0."""
    X = int(XFADE * SR)
    body = x[a:b].copy()
    t = np.linspace(0, 1, X, dtype=np.float32)[:, None]
    fade_out, fade_in = np.cos(t * np.pi / 2), np.sin(t * np.pi / 2)
    body[-X:] = x[b - X:b] * fade_out + x[a - X:a] * fade_in
    return body


def seam_check(body):
    """Discontinuity at the wrap vs the body's normal sample-to-sample steps, and the spectral jump at the seam vs elsewhere."""
    loop = np.concatenate([body[-SR:], body[:SR]])
    d = np.abs(np.diff(loop.mean(axis=1)))
    seam_step = float(d[SR - 3:SR + 3].max())
    typical = float(np.percentile(np.abs(np.diff(body.mean(axis=1))), 99.9))
    bd = bands(stft_mag(np.concatenate([body[-SR * 2:], body[:SR * 2]]).mean(axis=1)))
    flux = np.r_[0, np.sqrt((np.diff(bd, axis=0).clip(0) ** 2).sum(axis=1))]
    centre = len(flux) // 2
    seam_flux = float(flux[centre - 3:centre + 3].max())
    flux_body = np.r_[0, np.sqrt((np.diff(bands(stft_mag(body.mean(axis=1))), axis=0).clip(0) ** 2).sum(axis=1))]
    return {"seam_step": seam_step, "typical_step_p99_9": typical, "step_ok": bool(seam_step <= typical * 1.05),
            "seam_flux": seam_flux, "flux_p95": float(np.percentile(flux_body, 95)), "flux_ok": bool(seam_flux <= np.percentile(flux_body, 99))}


def main():
    # Round 16: "python make_loops.py course_candy" makes only the named tracks (the others keep their files as they are).
    only = set(sys.argv[1:])
    report = {}
    if only and os.path.exists(os.path.join(HERE, "gc_loops.json")):
        with open(os.path.join(HERE, "gc_loops.json"), encoding="utf-8") as f:
            report = json.load(f)
    for name, loops in TRACKS.items():
        if only and name not in only:
            continue
        src = find_source(name)
        if src is None:
            print(f"{name}: not made yet (the game plays the course track in its place)")
            continue
        x = decode(src)
        dur = len(x) / SR
        if not loops:
            y = x.copy()
            g = np.linspace(0, 1, int(0.005 * SR), dtype=np.float32)[:, None]
            y[:len(g)] *= g
            y[-len(g):] *= g[::-1]
            write_ogg(os.path.join(OUT, name + "_loop.ogg"), y)
            write_wav(os.path.join(CHECK, name + "_once.wav"), y)
            report[name] = {"seconds": round(dur, 2), "loops": False}
            print(f"{name}: {dur:.1f} s, plays once")
            continue
        mono = x.mean(axis=1)
        bd = bands(stft_mag(mono))
        flux = np.r_[0, (np.diff(bd, axis=0).clip(0)).sum(axis=1)]
        bpm, beat_s, phase_s = tempo_and_phase(flux)
        best, usable_end = find_loop(x, bpm, beat_s, phase_s)
        if name in PINNED:
            pin = PINNED[name]
            bpm, beat_s = pin["bpm"], 60.0 / pin["bpm"]
            a, b = zero_cross(x, int(round(pin["start"] * SR))), zero_cross(x, int(round(pin["end"] * SR)))
            fa, fb, w = a // HOP, b // HOP, int(beat_s * 4 * SR / HOP)
            pre = np.corrcoef(bd[fa - w:fa].ravel(), bd[fb - w:fb].ravel())[0, 1]
            post = np.corrcoef(bd[fa:fa + w].ravel(), bd[fb:fb + w].ravel())[0, 1]
            rms = moving_rms(mono, SR)
            loud = min(rms[a], rms[b]) / max(1e-6, max(rms[a], rms[b]))
            best = (0.0, a / SR, b / SR, pre, post, loud)
            find_loop.ranked = []
        if best is None:
            raise SystemExit(f"{name}: no loop region found")
        # Round 18: the best-scoring loop's seam can still jump (a fill or an accent right on the join): try the next
        # candidates in score order and keep the first whose seam is clean (the best one if none is).
        chosen = None
        for cand in [best] + find_loop.ranked[:60]:
            score, a_s, b_s, pre, post, loud = cand
            a = zero_cross(x, int(round(a_s * SR)))
            b = a + int(round((b_s - a_s) / (beat_s * 4)) * beat_s * 4 * SR)  # whole bars, sample-exact length
            if name in PINNED:
                b = int(round(b_s * SR))  # the hand-picked end (already on a zero crossing)
            body = make_body(x, a, b)
            check = seam_check(body)
            if chosen is None:
                chosen = (cand, a, b, body, check)
            if check["step_ok"] and check["flux_ok"]:
                chosen = (cand, a, b, body, check)
                break
        (score, a_s, b_s, pre, post, loud), a, b, body, check = chosen
        intro = x[:a].copy()
        write_ogg(os.path.join(OUT, name + "_intro.ogg"), intro)
        write_ogg(os.path.join(OUT, name + "_loop.ogg"), body)
        write_wav(os.path.join(CHECK, name + "_3loops.wav"), np.concatenate([intro, body, body, body]))
        write_wav(os.path.join(CHECK, name + "_seam.wav"), np.concatenate([body[-4 * SR:], body[:4 * SR]]))  # the seam at 4.0 s
        # The encoded files must keep the exact lengths (no encoder padding = no gap) and the clean seam.
        enc_body, enc_intro = decode(os.path.join(OUT, name + "_loop.ogg")), decode(os.path.join(OUT, name + "_intro.ogg"))
        enc = seam_check(enc_body)
        check["encoded_exact"] = bool(len(enc_body) == len(body) and len(enc_intro) == len(intro))
        check["encoded_seam_ok"] = bool(enc["step_ok"] and enc["flux_ok"])
        report[name] = {
            "seconds": round(dur, 2), "loops": True, "bpm": round(bpm, 2), "bar_seconds": round(beat_s * 4, 4),
            "loop_start_s": round(a / SR, 4), "loop_end_s": round(b / SR, 4), "loop_bars": PINNED[name]["bars"] if name in PINNED else int(round((b - a) / SR / (beat_s * 4))),
            "intro_samples": int(a), "body_samples": int(b - a), "usable_end_s": round(float(usable_end), 2),
            "match_before": round(float(pre), 3), "match_after": round(float(post), 3), "loudness_match": round(float(loud), 3),
            "crossfade_ms": int(XFADE * 1000), "seam": check,
        }
        print(f"{name}: {dur:.1f} s, {bpm:.1f} bpm, loop {a / SR:.2f}-{b / SR:.2f} s ({report[name]['loop_bars']} bars), "
              f"match {pre:.2f}/{post:.2f}, loudness {loud:.2f}; seam step {check['seam_step']:.4f} vs typical {check['typical_step_p99_9']:.4f} "
              f"({'OK' if check['step_ok'] else 'SPIKE'}), seam flux {check['seam_flux']:.2f} vs p95 {check['flux_p95']:.2f} ({'OK' if check['flux_ok'] else 'JUMP'})")
    with open(os.path.join(HERE, "gc_loops.json"), "w", encoding="utf-8") as f:
        json.dump(report, f, indent=1)
    ok = all(not r.get("loops") or "seam" not in r or all(r["seam"][k] for k in ("step_ok", "flux_ok", "encoded_exact", "encoded_seam_ok")) for r in report.values())
    print("LOOPS OK" if ok else "LOOP SEAM CHECK FAILED")
    return 0 if ok else 3


if __name__ == "__main__":
    sys.exit(main())
