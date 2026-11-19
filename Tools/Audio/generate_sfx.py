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
    # QF9: the terrace fight (nonlethal sparring; no gore, no bone cracks)
    ("fight_swing", "Quick swish of a fist or arm swinging through the air, soft cloth whoosh.", 0.5, False, "QF9 fight: 7's blows"),
    ("fight_hit", "Muffled padded punch landing on a body, soft thump, no crack.", 0.5, False, "QF9 fight: a blow lands"),
    ("fight_block", "Forearm blocking a punch, dull firm thud and cloth rustle.", 0.5, False, "QF9 fight: blocked / defended"),
    ("scree_grunt", "Short non-verbal effort grunt of a large insect-like creature, low raspy chitter, not aggressive, no words.", 0.7, False, "QF9 fight: Scree effort / being hit"),
    ("body_fall", "A large body falling onto a stone floor and sliding a little, soft heavy thud, no crack.", 1.0, False, "QF9 fight: a Scree goes down / 7 knocked down"),
    # Round 11: Venus (Chapter 2, part 1)
    ("amb_venus_gardens", "Lush terraced garden ambience: flowing water in stone channels, soft wind in leaves, distant gentle birdlike calls, calm and airy.", 20, True, "Venus gardens ambience"),
    ("bike_hum", "Smooth low hover engine hum of a small futuristic hover bike, steady, soft electric whirr, no combustion.", 4, True, "the hover bike (loop; pitch / volume follow the speed)"),
    ("saucer_descent", "A large flying saucer descending slowly through clouds: deep smooth hum swelling, soft air rush, gentle settling.", 6, False, "Venus arrival: the descent under the clouds"),
    ("vessel_slosh", "Water sloshing inside a large stone vessel that is being shaken, a few splashes.", 1.5, False, "C02: water shakes in a vessel"),
    ("dust_fall", "Fine dust and small grit trickling down from a stone ceiling in a large hall.", 2.5, False, "C04: falling dust"),
    ("signal_pulse", "A distinct, slightly unsettling low pulsing tone with a faint metallic ring, rising then fading, synthetic, not loud.", 2.5, False, "C05: the signal pulse"),
    ("worm_surface", "A large creature breaking through soft earth and sliding back under, deep earth crumble and slide.", 2.5, False, "burrowing creatures surfacing / diving"),
    ("climb_scuff", "Boots scuffing and scraping on a rough stone wall while someone climbs, two or three soft scrapes.", 1.2, False, "wall climb / climbing up a ledge"),
    # Round 12 (2e): the city's distant hum for Dan's rewritten 6b (replaces the prison sounds; the terrace after)
    ("amb_city_life", "A vast inhabited underground city heard from a high terrace far away: distant flowing traffic hum, soft wind across an enormous space, faint indistinct activity, calm and huge.", 20, True, "Chapter 1 6b city reveal + the terrace after (Round 12)"),
    # Round 12: Venus part 2 (the way down, the control chamber, the encounter, the return)
    ("amb_underground", "Deep underground root chambers: very low steady drone, slow water drips, faint creak of huge roots, distant hollow wind.", 20, True, "Venus: the root chambers + control chamber"),
    ("machinery_hum", "Large organic machine humming in a cave, low throbbing pulse with soft wet mechanical clicks, steady.", 8, True, "VEN_05: Wormmaster's machinery (silent after C07)"),
    ("creature_rumble", "Something large moving through soft earth just under the floor, low continuous rumble and crumbling, close.", 6, True, "VEN_05: telegraph of a creature about to surface"),
    ("device_off", "A strange humming device powering down: the pulse winds down in pitch, a soft hollow clunk, then quiet.", 2.5, False, "VEN_05: a control device disabled"),
    ("barrier_close", "Living stone stems growing up out of the ground and closing into a ring, deep grinding creak, ending in a soft resonant hum.", 4, False, "VEN_05: the containment barrier closes"),
    ("creatures_withdraw", "Several large burrowing creatures sliding away deep into the earth, rumble receding into the distance, calm.", 4, False, "VEN_05 / C07: the creatures withdraw safely"),
    ("slab_lift", "A heavy stone slab grinding as it lifts off a stone floor, low scrape fading into a soft hum.", 1.2, False, "telekinesis lifts the fallen slab (ruins)"),
    # Round 10: the galley scene (ship's hum + eating sounds only during the silences)
    ("fork_clink", "A fork lightly touching a ceramic dinner plate, one soft clink, quiet table sound.", 0.8, False, "Chapter 1 galley: 7 eats"),
    ("cat_eating", "A small cat eating wet food from a ceramic bowl, quiet soft chewing and licking, close.", 6, True, "Chapter 1 galley: the cat eats"),
    ("course_set", "Spaceship engines rising smoothly into a soft deep whoosh as the ship accelerates away, gentle.", 4, False, "saucer: Set course for Venus placeholder"),
    # ---- Round 13
    ("bike_whoosh", "Soft airy wind rush of a fast hover vehicle passing through open air, smooth continuous whoosh, no engine.", 6, True, "hover bike: the whoosh that rises with speed (0h)"),
    ("bike_liftoff", "A small hover bike lifting off the ground: a soft rising electric hum swelling up with a gentle puff of air.", 1.6, False, "hover bike: lift-off (mount, B01 rise)"),
    ("bike_settle", "A small hover bike settling down to idle: a soft descending electric hum and a gentle sigh of air.", 1.6, False, "hover bike: settle (dismount, parking)"),
    ("bike_mount", "Someone swinging onto the padded saddle of a sleek vehicle, soft cloth and leather creak, a light click of hands into grips.", 1.0, False, "hover bike: mount"),
    ("bike_dismount", "Someone stepping off a sleek vehicle onto grass, soft cloth rustle and one light footstep.", 0.9, False, "hover bike: dismount"),
    ("bird_song_a", "A single small songbird singing a bright melodic phrase in a quiet garden, natural, mid distance.", 3, False, "Venus gardens: birds (0i, positional)"),
    ("bird_song_b", "A small bird trilling and chirping a short cheerful song among leaves, natural, mid distance.", 3, False, "Venus gardens: birds (0i, positional)"),
    ("bird_call_far", "A distant large bird calling twice across a wide valley, soft echo, natural.", 3, False, "Venus gardens: distant bird calls (0i)"),
    ("insects_loop", "Gentle daytime insects buzzing and chirring softly in tall grass, calm meadow, no birds.", 12, True, "Venus gardens: insects bed (0i)"),
    ("animal_call_far", "A distant unseen gentle animal calling once from far across a misty valley, low soft hooting, natural.", 3, False, "Venus gardens: distant animal calls (0i)"),
    ("grass_rustle", "A small animal scurrying through long grass nearby, quick soft rustle then stillness.", 1.5, False, "Venus gardens: rustling in the grass (0i)"),
    ("wing_flutter", "A few small birds taking off together from a bush, quick wing flutter.", 1.2, False, "Venus: birds crossing / taking off (0i)"),
    ("tremor_rumble", "A strong earthquake tremor: a deep powerful rumble rising from below, ground shaking, rattling stones, then slowly fading.", 5, False, "Venus tremors (0k): B03 + C04"),
    ("path_collapse", "A stone path cracking and collapsing into a gully: sharp crack, grinding break, heavy stones and earth falling, dust settling.", 4, False, "VEN_B03: the walkway breaks (0k)"),
    ("stone_rain", "Dust, grit and small stones raining down from a stone ceiling onto a hard floor during a quake.", 3.5, False, "C04 tremor: stones from the ceiling (0k)"),
    ("water_slosh", "Water sloshing heavily out of a stone channel and splashing onto paving.", 2, False, "B03 tremor: water slops from the channel (0k)"),
    ("cat_purr", "A house cat purring contentedly on someone's lap, close, soft and steady, gentle rumble.", 6, True, "VEN_C13: Panther purrs in her lap (close insert)"),
    ("console_open", "A retro-futuristic chrome control console powering up: soft rising electronic chime, gentle glassy hum, warm.", 1.5, False, "the ship's console opens (item 2)"),
    ("console_move", "A soft short electronic tick of a retro chrome console menu, glassy and quiet.", 0.5, False, "console: cursor moves"),
    ("console_select", "A soft confirming electronic blip of a retro chrome console, two gentle rising tones.", 0.5, False, "console: select"),
    # ---- Round 14: Europa (Docs/VOICES.md "Cetacean calls" + what the chapter needs)
    ("europa_call_deep", "Deep slow whale song call, long low moan rising and falling, underwater, distant echo.", 6, False, "Europa: Cetacean calls (the keeper's layer, the huge one passing)"),
    ("europa_call_answer", "Softer higher whale call answering in the distance, underwater, gentle.", 5, False, "Europa: the calls answering across open water"),
    ("europa_call_learning", "Young whale attempting a short three-note call, slightly wobbly, then an older deep whale repeating it clearly, underwater.", 10, False, "Europa: the vision (its video is nearly silent)"),
    ("europa_dolphin_chirp", "Friendly dolphin clicks and whistles, underwater.", 2.5, False, "Europa: the guide"),
    ("europa_amb_underwater", "Deep underwater ambience beneath ice, soft muffled rumble, slow distant creaks of ice, gentle flowing water, faint bubbles.", 20, True, "Europa: under the ice"),
    ("europa_bubbles", "A short burst of bubbles rising underwater, soft and close.", 1.5, False, "Europa: bubbles (helmet, bike)"),
    ("europa_bike_hum", "Smooth low electric hum of a small underwater vehicle, muffled by water, steady.", 6, True, "Europa: the bike underwater"),
    ("europa_extraction_pulse", "A heavy distant mechanical pulse thudding through water, industrial pressure wave, low and muffled.", 3, False, "Europa: Tunneler's extraction pulse"),
    ("europa_drill", "Industrial drilling machinery grinding steadily far away, heard through water and metal walls.", 10, True, "Europa: the machinery / the base"),
    ("europa_alarm", "Muffled industrial base alarm, slow two-tone warning siren inside metal corridors.", 4, True, "Europa: the base alarm"),
    ("europa_pressure_door", "Heavy pressure chamber door cycling: hiss of equalizing air, clunk of locks, water draining.", 4, False, "Europa: the pressure chamber"),
    ("europa_seal_tone", "A single soft clean confirmation tone, gentle electronic chime.", 1, False, "Europa: helmet seal check"),
    ("europa_demolition", "Series of deep muffled underwater explosions rolling one after another, structural metal groaning and collapsing.", 6, False, "Europa: the demolition / the escape"),
    ("europa_rupture", "Single heavy underwater rupture: a deep thump, metal tearing and a rush of bubbles.", 2, False, "Europa: ruptures chasing her"),
    ("europa_pressure_wave", "A deep underwater pressure wave whoosh, a heavy surge of water passing, then calm.", 3, False, "Europa: the pressure wave after the escape"),
    ("europa_device_place", "A small device clamps onto metal with a magnetic clunk and a soft beep.", 1, False, "Europa: placing a demolition device"),
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
