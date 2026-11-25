"""Round 21 (Grand Circuit): close a track's layout by solving two straight pieces' lengths, then write them back into GcTrack.cs.
Usage (repo root): python Tools/Model/gc_close.py <TrackFn> <pieceA> <pieceB> [--plot out.png]
  pieceA / pieceB = 0-based index of a flat piece in that track's list (two with different headings). Prints the result and the
  self-clearance (gc_sim.py geom). --plot draws the plan (height as shade) for a look."""
import sys, re, os, importlib.util
import numpy as np
here = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("gc_sim", os.path.join(here, "gc_sim.py")); sim = importlib.util.module_from_spec(spec); spec.loader.exec_module(sim)
path = os.path.join(here, "..", "..", "Unity", "Assets", "Scripts", "Runtime", "GcTrack.cs")


def body_span(src, fn):
    a = src.index("public static GcTrack " + fn + "()"); b = src.index('Make("', a); return a, b


def set_len(fn, k, L):
    src = open(path, encoding="utf-8").read(); a, b = body_span(src, fn)
    ms = list(re.finditer(r"new Piece\((-?[\d.]+)f", src[a:b]))
    m = ms[k]; s0, s1 = a + m.start(1), a + m.end(1)
    src = src[:s0] + (f"{max(20.0, L):.0f}") + src[s1:]
    open(path, "w", encoding="utf-8").write(src)
    sim.src = src


def get_len(fn, k):
    return sim.pieces(fn)[k][0]


def err(fn):
    sim.src = open(path, encoding="utf-8").read(); _, _, e, _ = sim.make(fn); return np.array([e[0], e[2]])


def solve_all(fn, keep=()):
    """Every plain straight's length may change (20..450 m): the smallest set of changes that closes the plan (the error is linear
    in the straights' lengths: each moves everything after it along its own heading)."""
    from scipy.optimize import lsq_linear
    pcs = sim.pieces(fn); h = 0.0; slope = 0.0; cols = []; ks = []; L0 = []
    for i, (L, yaw, pit, rol, hw, fl, sh) in enumerate(pcs):
        level = abs(((slope + 180.0) % 360.0) - 180.0) < 0.01   # (a straight on a slope would change the heights too: never touched)
        if i > 0 and i not in keep and level and yaw == 0 and pit == 0 and rol == 0 and not any(k in fl for k in ("Gap", "Loop", "Fork", "Cork", "Press")):
            a = np.radians(h); cols.append([np.sin(a), np.cos(a)]); ks.append(i); L0.append(L)
        h += yaw; slope += pit
    e = err(fn); A = np.array(cols).T
    W = 50.0
    M = np.vstack([A * W, np.eye(len(ks))]); b = np.concatenate([e * W, np.zeros(len(ks))])
    lo = np.array([20 - l for l in L0]); hi = np.array([450 - l for l in L0])
    r = lsq_linear(M, b, bounds=(lo, hi))
    for k, l, d in zip(ks, L0, r.x):
        if abs(d) >= 0.5: set_len(fn, k, round(l + d)); print(f"  piece {k}: {l:.0f} -> {l + d:.0f} m")
    for _ in range(3):   # (round-off) a last touch on the two straights with the most different headings
        e = err(fn)
        if np.linalg.norm(e) < 1.0: break
        i0 = ks[0]; i1 = max(ks, key=lambda k: abs(cols[0][0] * cols[ks.index(k)][1] - cols[0][1] * cols[ks.index(k)][0]))
        Jx = np.array([cols[ks.index(i0)], cols[ks.index(i1)]]).T
        d = np.linalg.solve(Jx, e); set_len(fn, i0, round(get_len(fn, i0) + d[0])); set_len(fn, i1, round(get_len(fn, i1) + d[1]))


def main():
    fn = sys.argv[1]
    if sys.argv[2] == "all":
        keep = [int(x) for x in sys.argv[sys.argv.index("--keep") + 1].split(",")] if "--keep" in sys.argv else []
        solve_all(fn, keep); sim.geom(fn)
        if "--plot" in sys.argv: plot(fn)
        return
    ia, ib = int(sys.argv[2]), int(sys.argv[3])
    for it in range(4):
        e0 = err(fn); La, Lb = get_len(fn, ia), get_len(fn, ib)
        set_len(fn, ia, La + 10); ea = err(fn); set_len(fn, ia, La)
        set_len(fn, ib, Lb + 10); eb = err(fn); set_len(fn, ib, Lb)
        J = np.stack([(ea - e0) / 10, (eb - e0) / 10], 1)
        d = np.linalg.solve(J, -e0)
        nA, nB = La + d[0], Lb + d[1]
        if nA < 20 or nB < 20: print(f"cannot close with these pieces: lengths would be {nA:.0f}, {nB:.0f}"); return
        set_len(fn, ia, round(nA)); set_len(fn, ib, round(nB))
    print(f"{fn}: piece {ia} = {get_len(fn, ia):.0f} m, piece {ib} = {get_len(fn, ib):.0f} m")
    sim.geom(fn)
    if "--plot" in sys.argv: plot(fn)


def plot(fn):
    if True:
        import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
        ps, _, _, fl = sim.make(fn)
        plt.figure(figsize=(8, 8)); plt.scatter(ps[:, 0], ps[:, 2], c=ps[:, 1], s=2, cmap="gray"); plt.axis("equal")
        for i in range(0, len(ps), 100): plt.text(ps[i, 0], ps[i, 2], str(i), fontsize=7)
        plt.savefig(sys.argv[sys.argv.index("--plot") + 1], dpi=90)


if __name__ == "__main__":
    main()
