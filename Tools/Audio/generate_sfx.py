"""Generate the game's sound effects with the ElevenLabs sound-generation endpoint.

Usage (repo root): python Tools/Audio/generate_sfx.py [--force] [--md-only] [--only <id>...]
Key: ELEVENLABS_API_KEY environment variable only (never printed or written anywhere).
The list lives in SFX below (id, prompt, seconds, loop, where used); every run also rewrites Docs/SFX-LIST.md from it
(--md-only: just that, no API calls). Output: Unity/Assets/Audio/SFX/<id>.wav (16-bit stereo 44.1 kHz;
the API's pcm_44100 is interleaved stereo s16le).
Existing files are skipped unless --force. --only takes ids and must come last.

Request: POST /v1/sound-generation?output_format=pcm_44100 with model "eleven_text_to_sound_v2",
{"text", "duration_seconds", "prompt_influence": 0.45, "loop": true for loop ids}. If PCM is refused, it asks for
mp3_44100_128 and decodes with ffmpeg. Post-processing in Python: loop ids get a 120 ms equal-power crossfade of the
tail into the head (seamless loop point) and are normalized to -6 dBFS peak; one-shots get a 5 ms fade-in /
30 ms fade-out and are normalized to -3 dBFS.
"""
import array, json, math, os, shutil, subprocess, sys, tempfile, time, urllib.request, urllib.error, wave

ROOT = os.getcwd()
API = "https://api.elevenlabs.io/v1"
MODEL = "eleven_text_to_sound_v2"
RATE = 44100
CH = 2  # pcm_44100 from sound-generation is interleaved 16-bit stereo (176400 bytes per second)
OUT_DIR = os.path.join(ROOT, "Unity", "Assets", "Audio", "SFX")
MD = os.path.join(ROOT, "Docs", "SFX-LIST.md")
STYLE = " Quiet, atmospheric, natural and realistic sound. No music, no voices."
INFLUENCE = 0.45
XFADE_S = 0.12

# id, prompt, seconds, loop, where used
SFX = [
    ("amb_cavern", "Vast underground cavern ambience, low wind, distant mining drills and machinery far away, faint echoes.", 20, True, "Chapter 1 prison / ruins / mining caverns"),
    ("amb_city", "High stone terrace open to a gigantic hollow; soft wind; very distant heavy machinery, conveyors and freight lifts; faint far-off city life murmur.", 20, True, "Chapter 1 city reveal terrace (6b), saucer impound"),
    ("amb_saucer", "Calm spaceship interior ambience, soft low air-handling hum, gentle electronic warmth.", 20, True, "aboard the saucer"),
    ("laser_hum", "Steady electric hum of a science-fiction laser barrier field heard up close, with a faint high shimmer.", 5, True, "Chapter 1 cell door (laser bars)"),
    ("laser_power_down", "Laser barrier beams switching off one after another, descending soft electric zaps fading to silence.", 2.5, False, "Chapter 1 cell door opens"),
    ("release_click", "Heavy mechanical release clunk and click heard from around a corner, slightly muffled.", 1, False, "Chapter 1 hidden cell release (cat)"),
    ("energy_point", "Very soft glassy shimmer, a gentle rising chime-like glow, magical but subtle.", 2, False, "energy point pickup / ancient surface touch"),
    ("step_rock_1", "Single soft footstep, soft-soled boot on rough stone.", 0.5, False, "7 footsteps on stone"),
    ("step_rock_2", "One soft footstep of a soft-soled boot on rough stone floor.", 0.5, False, "7 footsteps on stone"),
    ("step_rock_3", "A single quiet boot step on rough stone ground.", 0.5, False, "7 footsteps on stone"),
    ("step_metal_1", "Single soft footstep on a metal grating catwalk.", 0.5, False, "7 footsteps on catwalks / saucer"),
    ("step_metal_2", "One soft boot step on a metal grating walkway.", 0.5, False, "7 footsteps on catwalks / saucer"),
    ("step_metal_3", "A single soft footstep on a metal grate catwalk, recorded up close.", 0.5, False, "7 footsteps on catwalks / saucer"),
    ("paw_1", "Single very soft cat paw step on stone, barely audible.", 0.5, False, "cat footsteps"),
    ("paw_2", "One very soft padded cat paw step on a stone floor, barely audible.", 0.5, False, "cat footsteps"),
    ("scree_step_1", "Single heavier shuffling footstep of a robed figure on stone.", 0.5, False, "Scree footsteps"),
    ("scree_step_2", "One heavy shuffling step of a robed figure dragging on a stone floor.", 0.5, False, "Scree footsteps"),
    ("tk_gather", "Telekinesis gathering: low soft rising hum with airy shimmer.", 1.5, False, "telekinesis charge (fight, Scree encounter)"),
    ("tk_push", "Soft airy telekinetic push whoosh.", 1, False, "telekinesis push / release"),
    ("stone_fall", "A loose stone falling off a pillar, hitting the stone floor and rolling.", 2, False, "Scree distraction (scene 5)"),
    ("vision_whoosh", "Soft white-out whoosh swelling into silence, dreamlike.", 2.5, False, "vision start / end (scene 6)"),
    ("vision_shimmer", "Soft glassy ethereal shimmer pad, quiet.", 6, True, "during the vision (scene 6)"),
    ("stone_door", "Heavy ancient stone slab sinking into the floor, deep grinding rumble.", 3.5, False, "old wall / ancient passage opens (scenes 4, 6)"),
    ("clamps_release", "Heavy hydraulic docking clamps releasing, hiss and clunk.", 2.5, False, "saucer impound release (scene 7-8)"),
    ("saucer_hum", "Smooth low spaceship hover hum, soft and warm.", 6, True, "saucer hovering (outside)"),
    ("tractor_beam", "Soft rising tone of a gentle tractor beam lifting someone, airy and calm.", 5, False, "tractor-beam boarding (scene 8)"),
    ("hatch_iris", "Small mechanical iris hatch sliding shut, smooth metallic whir.", 1.5, False, "saucer hatch closes (scene 8)"),
    ("liftoff", "Spaceship lifting off, rising hum and soft rumble accelerating away.", 6, False, "saucer escape (end of scene 8)"),
    ("city_stone_fall", "Very distant massive stone block breaking off and falling, deep far rumble and echo.", 4, False, "city reveal, ancient terrace collapses (6b)"),
    ("fridge_open", "Refrigerator door opening with a soft seal pop and faint hum.", 1.5, False, "saucer galley (scene 10)"),
    ("fridge_close", "Refrigerator door closing softly.", 1, False, "saucer galley (scene 10)"),
    ("menu_move", "Very soft subtle user interface tick.", 0.5, False, "menu"),
    ("menu_confirm", "Soft gentle user interface confirm chime, glassy.", 0.7, False, "menu"),
    # Round 9: traversal
    ("ledge_grab", "Hands catching hold of a rough stone ledge, a quick soft slap of palms and a faint cloth rustle.", 0.8, False, "7 grabs a ledge (Chapter 1 traversal)"),
    ("land_soft", "A person landing lightly on their feet on a stone floor after a short jump, soft thud, boots.", 0.8, False, "7 lands after a jump / short drop"),
    ("land_hard", "A person landing heavily on stone after a long drop, firm thud, boots and a short cloth rustle.", 1.0, False, "7 lands after a big drop"),
    ("climb_scuff", "Boots scuffing and scraping on a rough stone wall while someone climbs, two or three soft scrapes.", 1.2, False, "wall climb / climbing up a ledge"),
    ("slab_lift", "A heavy stone slab grinding as it lifts off a stone floor, low scrape fading into a soft hum.", 1.2, False, "telekinesis lifts the fallen slab (ruins)"),
    # Round 10: the galley scene (ship's hum + eating sounds only during the silences)
    ("fork_clink", "A fork lightly touching a ceramic dinner plate, one soft clink, quiet table sound.", 0.8, False, "Chapter 1 galley: 7 eats"),
    ("cat_eating", "A small cat eating wet food from a ceramic bowl, quiet soft chewing and licking, close.", 6, True, "Chapter 1 galley: the cat eats"),
    ("course_set", "Spaceship engines rising smoothly into a soft deep whoosh as the ship accelerates away, gentle.", 4, False, "saucer: Set course for Venus placeholder"),
]


def write_md():
    md = ["# Sound effects", "",
          "Generated by `Tools/Audio/generate_sfx.py` (ElevenLabs sound generation, model `eleven_text_to_sound_v2`,",
          f"prompt influence {INFLUENCE}) → `Unity/Assets/Audio/SFX/<id>.wav` (16-bit stereo 44.1 kHz). This table is rewritten",
          "from the list in the script on every run — edit the script, not this file. Every prompt gets the style suffix",
          f"\"{STYLE.strip()}\". Loops: generated with `loop: true`, then a {int(XFADE_S * 1000)} ms equal-power tail→head",
          "crossfade, peak -6 dBFS. One-shots: peak -3 dBFS. The game code loads these ids by name — keep them exactly.",
          "", f"{len(SFX)} sounds.", "", "| id | prompt | duration s | loop | where used |", "|---|---|---|---|---|"]
    for sid, prompt, sec, loop, where in SFX:
        md.append(f"| `{sid}` | {prompt} | {sec} | {'yes' if loop else ''} | {where} |")
    with open(MD, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(md) + "\n")


def request(key, body, fmt):
    r = urllib.request.Request(f"{API}/sound-generation?output_format={fmt}", data=json.dumps(body).encode(),
                               method="POST", headers={"xi-api-key": key, "Content-Type": "application/json"})
    with urllib.request.urlopen(r, timeout=300) as resp:
        return resp.read(), resp.headers.get("Content-Type", "")


def mp3_to_pcm(data):
    if not shutil.which("ffmpeg"):
        sys.exit("PCM was refused and ffmpeg is not on PATH - cannot decode mp3; stopping.")
    with tempfile.TemporaryDirectory() as d:
        src = os.path.join(d, "in.mp3")
        open(src, "wb").write(data)
        return subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-i", src, "-ac", str(CH), "-ar", str(RATE),
                               "-f", "s16le", "-"], capture_output=True, check=True).stdout


def process(pcm, loop):
    s = array.array("h")
    s.frombytes(pcm[: len(pcm) // 2 * 2])
    if sys.byteorder != "little":
        s.byteswap()
    x = [v / 32768.0 for v in s]
    x = x[: len(x) // CH * CH]
    if loop:
        n = int(XFADE_S * RATE)  # frames
        if len(x) > 4 * n * CH:
            head, tail = x[: n * CH], x[-n * CH:]
            body = x[: -n * CH]
            for i in range(n * CH):
                t = (i // CH + 0.5) / n
                body[i] = head[i] * math.sin(t * math.pi / 2) + tail[i] * math.cos(t * math.pi / 2)
            x = body
    else:
        fi, fo = int(0.005 * RATE), int(0.03 * RATE)
        for i in range(min(fi * CH, len(x))):
            x[i] *= (i // CH) / fi
        for i in range(min(fo * CH, len(x))):
            x[-1 - i] *= (i // CH) / fo
    peak = max((abs(v) for v in x), default=0.0)
    target = 10 ** ((-6.0 if loop else -3.0) / 20)
    g = target / peak if peak > 1e-6 else 1.0
    out = array.array("h", (max(-32767, min(32767, int(round(v * g * 32767)))) for v in x))
    if sys.byteorder != "little":
        out.byteswap()
    return out.tobytes(), len(out) / CH / RATE, 20 * math.log10(peak) if peak > 1e-6 else -120


def main():
    write_md()
    if "--md-only" in sys.argv:
        print(f"wrote {MD}")
        return
    key = os.environ.get("ELEVENLABS_API_KEY", "").strip()
    if not key:
        sys.exit("ELEVENLABS_API_KEY is not set - stopping (no files generated).")
    force = "--force" in sys.argv
    only = set(sys.argv[sys.argv.index("--only") + 1:]) if "--only" in sys.argv else None
    os.makedirs(OUT_DIR, exist_ok=True)
    fmt = "pcm_44100"
    made, skipped, failed, chars = 0, 0, [], 0
    for sid, prompt, sec, loop, where in SFX:
        if only and sid not in only:
            continue
        out = os.path.join(OUT_DIR, sid + ".wav")
        if os.path.exists(out) and not force:
            skipped += 1
            continue
        body = {"text": prompt + STYLE, "duration_seconds": sec, "prompt_influence": INFLUENCE, "model_id": MODEL}
        if loop:
            body["loop"] = True
        data = None
        for attempt in range(4):
            try:
                data, ctype = request(key, body, fmt)
                break
            except urllib.error.HTTPError as e:
                msg = e.read()[:300]
                if fmt == "pcm_44100" and e.code in (400, 403, 422) and b"format" in msg.lower():
                    print(f"PCM refused (HTTP {e.code}) - switching to mp3 + ffmpeg decode")
                    fmt = "mp3_44100_128"
                    continue
                if e.code in (429, 500, 502, 503) and attempt < 3:
                    time.sleep(4 * (attempt + 1)); continue
                print(f"FAILED {sid}: HTTP {e.code} {msg!r}")
                break
            except urllib.error.URLError as e:
                if attempt < 3:
                    time.sleep(4 * (attempt + 1)); continue
                print(f"FAILED {sid}: {e}")
                break
        if data is None:
            failed.append(sid)
            continue
        pcm = mp3_to_pcm(data) if fmt != "pcm_44100" or "mpeg" in ctype else data
        frames, secs, peak_db = process(pcm, loop)
        with wave.open(out, "wb") as w:
            w.setnchannels(CH); w.setsampwidth(2); w.setframerate(RATE)
            w.writeframes(frames)
        made += 1
        chars += len(body["text"])
        print(f"{sid:18s} {secs:5.2f} s  raw peak {peak_db:6.1f} dBFS  {'loop' if loop else ''}")
    print(f"done: {made} generated, {skipped} skipped, {len(failed)} failed {failed}, {chars} prompt characters, fmt {fmt}")
    if failed:
        sys.exit(1)


main()
