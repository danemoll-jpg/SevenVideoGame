"""QF34 (2): the memories of Panther as ONE flowing film - every shot 7 and Panther together, story order, soft dissolves.

Usage (repo root): python Tools/Video/make_memory_montage.py
  in : Art/Video/memory_clips/mem_NN_*.mp4 (Tools/Run-MemoryCaptures.ps1 + encode_memclips.py: the memory camera's two-shots, Panther
       visible in every frame) + the closing moment from Dan's meadow film (Art/Video/cattalus_meadow.mp4, 66.5-71.5 s: her sitting
       against him in the grass)
  out: Art/Video/panther_memories_code.mp4 (1920x1044, 25 fps, grayscale, silent) - played in the ending when Dan's own edit
       (Art/Video/panther_memories.mp4) is not there. Each shot SHOT s (its middle part), DISSOLVE s soft dissolves; ~45-60 s total;
       a drifting foggy border + a slight glow mark them as memories (the centre clear).
"""
import glob, os, shutil, subprocess, sys

ROOT = os.getcwd()
CLIPS = os.path.join(ROOT, "Art", "Video", "memory_clips")
OUT = os.path.join(ROOT, "Art", "Video", "panther_memories_code.mp4")
SHOT, DISSOLVE = 4.8, 0.8
MEADOW = (os.path.join(ROOT, "Art", "Video", "cattalus_meadow.mp4"), 66.5, 5.0)


def ff(name):
    p = shutil.which(name)
    if p: return p
    wg = os.path.join(os.environ.get("LOCALAPPDATA", ""), "Microsoft", "WinGet", "Packages")
    for f in glob.glob(os.path.join(wg, "**", name + ".exe"), recursive=True): return f
    sys.exit(name + " not found")


def dur(fp, path):
    return float(subprocess.run([fp, "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", path], capture_output=True, text=True).stdout.strip())


def main():
    fm, fp = ff("ffmpeg"), ff("ffprobe")
    shots = []
    report = []
    for c in sorted(glob.glob(os.path.join(CLIPS, "mem_*.mp4"))):
        d = dur(fp, c); want = min(SHOT, d); s = max(0.0, (d - want) / 2.0)
        bv = os.path.splitext(c)[0] + ".both.txt"
        if os.path.exists(bv):
            # the stretch of the clip where 7 AND Panther are both in view for the most frames (all of them, ideally)
            v = open(bv).read().strip()
            for w in (want, 4.2, 3.6):                      # a shorter shot when only a shorter stretch has both of them in view
                w = min(w, want); n = int(round(w * 25)); best, bi = -1, 0
                for i in range(0, max(1, len(v) - n + 1)):
                    k = v[i:i + n].count("1")
                    if k > best: best, bi = k, i
                if best >= n * 0.96: break
            want = n / 25.0; s = bi / 25.0; report.append(f"{os.path.basename(c)[:-4]} {best}/{n}")
            if best < n * 0.96: report[-1] += " DROPPED (7 and Panther must both be in every shot)"; continue
        shots.append((c, s, want))
    if os.path.exists(MEADOW[0]): shots.append(MEADOW)
    if len(shots) < 2: sys.exit("not enough clips")
    args = [fm, "-y", "-loglevel", "error"]
    for c, s, d in shots: args += ["-ss", f"{s:.2f}", "-t", f"{d:.2f}", "-i", c]
    parts = []
    for i, (c, s, d) in enumerate(shots):
        grade = ",eq=contrast=1.35:brightness=0.06:gamma=1.15" if "escape" in os.path.basename(c) else ""   # the prison corridor read as one dark tone
        parts.append(f"[{i}:v]scale=1920:1044:force_original_aspect_ratio=increase,crop=1920:1044,fps=25{grade},format=gray,format=yuv420p,setpts=PTS-STARTPTS[v{i}]")
    cur, t = "v0", shots[0][2]
    for i in range(1, len(shots)):
        off = t - DISSOLVE
        parts.append(f"[{cur}][v{i}]xfade=transition=fade:duration={DISSOLVE}:offset={off:.2f}[x{i}]")
        cur = f"x{i}"; t = off + shots[i][2]
    # QF34 (2, Dan): a foggy border marks them as memories - soft white-gray mist drifting in from the edges (the centre clear), a slight
    # glow / softened picture. The mist is computed small and scaled up (it is soft anyway); it never goes near white (max ~0.67).
    fog = ("st(0,(X/W-0.5)*2);st(1,(Y/H-0.5)*2.3);st(2,clip((hypot(ld(0),ld(1))-0.68)/0.55,0,1));st(2,ld(2)*ld(2)*(3-2*ld(2)));"
           "st(3,0.62+0.22*sin(X*0.055+T*0.33)*cos(Y*0.07-T*0.21)+0.16*sin((X+Y)*0.031-T*0.47));"
           "170*ld(2)*clip(ld(3),0,1)")
    parts.append(f"color=c=black:s=384x209:r=25:d={t:.2f},format=gray,geq=lum='{fog}',scale=1920:1044:flags=bicubic,gblur=sigma=10,"
                 f"fade=t=in:st=0:d=1.2,fade=t=out:st={t - 1.2:.2f}:d=1.2[fog]")
    parts.append(f"[{cur}]split[ma][mb];[mb]gblur=sigma=7[mg];[ma][mg]blend=all_mode=screen:all_opacity=0.16[mglow]")
    parts.append(f"[mglow]format=gray[mgg];[mgg][fog]blend=all_mode=screen:shortest=1,format=yuv420p[mem]")
    parts.append(f"[mem]fade=t=in:st=0:d=0.8,fade=t=out:st={t - 1.0:.2f}:d=1.0[out]")
    args += ["-filter_complex", ";".join(parts), "-map", "[out]", "-an", "-c:v", "libx264", "-preset", "slow", "-crf", "18", "-movflags", "+faststart", OUT]
    subprocess.run(args, check=True)
    print("both in view (frames of each shot): " + ", ".join(report))
    print(f"{len(shots)} shots ({', '.join(os.path.splitext(os.path.basename(c))[0] for c, _, _ in shots)}) -> {os.path.relpath(OUT, ROOT)}, {dur(fp, OUT):.1f} s")


if __name__ == "__main__":
    main()
