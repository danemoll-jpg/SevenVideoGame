"""QF31: turn a check run's raw captures into the game's / Dan's files.

Usage (repo root): python Tools/Video/encode_memclips.py <capture dir> [<capture dir> ...]
  <dir>/memclips/<name>/f0001.jpg ...  -> Art/Video/memory_clips/<name>.mp4  (h264, 1920x1044, 25 fps, grayscale, no sound) - Dan's raw
                                          clips for his own memory edit; Build everything also plays them as the stand-in montage
  <dir>/recstills/<name>.png            -> Unity/Assets/Resources/Finale/Received/<name>.png (FIN_12's "Received" stills, 7 / Panther /
                                          their bike / the saucer hidden) + its entry merged into received_check.json there
"""
import glob, json, os, re, shutil, subprocess, sys

ROOT = os.getcwd()
OUT = os.path.join(ROOT, "Art", "Video", "memory_clips")
REC = os.path.join(ROOT, "Unity", "Assets", "Resources", "Finale", "Received")


def ffmpeg():
    p = shutil.which("ffmpeg")
    if p: return p
    wg = os.path.join(os.environ.get("LOCALAPPDATA", ""), "Microsoft", "WinGet", "Packages")
    for f in glob.glob(os.path.join(wg, "**", "ffmpeg.exe"), recursive=True): return f
    sys.exit("ffmpeg not found")


def main():
    ff = ffmpeg(); os.makedirs(OUT, exist_ok=True)
    check_path = os.path.join(REC, "received_check.json")
    check = json.load(open(check_path, encoding="utf-8")) if os.path.exists(check_path) else {}
    for d in sys.argv[1:]:
        for clip in sorted(glob.glob(os.path.join(d, "memclips", "*"))):
            frames = sorted(glob.glob(os.path.join(clip, "f*.jpg")))
            if len(frames) < 10: print("skip (too few frames)", clip); continue
            name = os.path.basename(clip); dst = os.path.join(OUT, name + ".mp4")
            subprocess.run([ff, "-y", "-loglevel", "error", "-framerate", "25", "-i", os.path.join(clip, "f%04d.jpg"), "-vf", "format=gray,format=yuv420p",
                            "-c:v", "libx264", "-preset", "slow", "-crf", "17", "-movflags", "+faststart", dst], check=True)
            bv = os.path.join(clip, "both_visible.txt")
            if os.path.exists(bv): shutil.copy(bv, os.path.join(OUT, name + ".both.txt"))   # QF34 (2): per frame, 7 AND Panther in view
            print(f"{name}: {len(frames)} frames ({len(frames) / 25:.1f} s) -> {os.path.relpath(dst, ROOT)}")
        rs = os.path.join(d, "recstills")
        if os.path.isdir(rs):
            j = os.path.join(rs, "received_check.json")
            got = json.load(open(j, encoding="utf-8")) if os.path.exists(j) else {}
            for png in glob.glob(os.path.join(rs, "rec_*.png")):
                n = os.path.splitext(os.path.basename(png))[0]
                if n not in got: print("no check entry for", n, "- not installed"); continue
                if got[n].get("us_in_frame", 1) != 0: print(n, "has 7 / Panther in frame - NOT installed"); continue
                shutil.copy(png, os.path.join(REC, n + ".png")); check[n] = got[n]
                print(f"{n}: installed ({got[n]['source']})")
    if check:
        json.dump(check, open(check_path, "w", encoding="utf-8"), indent=1)
        print("received_check.json:", ", ".join(f"{k} {v['us_in_frame']}" for k, v in sorted(check.items())))


main()
