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
    ("cat_purr_a", "A real domestic cat purring loudly and deeply, close-miked right next to the cat, rich rumbling low-frequency purr, steady rhythm of in and out breaths, warm and intimate, no other sounds.", 8, True, "QF23 (11): Panther's loud purr, variation A (lap scenes, reunions)"),
    ("cat_purr_b", "A big contented house cat purring very loudly, close microphone, deep chesty rumble with a slight rattle on the in-breath, slow relaxed rhythm, no background.", 8, True, "QF23 (11): Panther's loud purr, variation B"),
    ("cat_purr_c", "A cat purring hard while being stroked, close-up recording, a deep motorboat rumble rising and falling with each breath, rich and loud, no background noise.", 8, True, "QF23 (11): Panther's loud purr, variation C (swells while she strokes him)"),
    ("cat_chatter", "A house cat chattering and chittering excitedly at a bird through a window: rapid soft clicking jaw chatter and tiny squeaky trills, close.", 2.5, False, "QF23 (10): Panther chatters at Ratwing on the display"),
    ("cat_paw_tap", "Small soft taps of a cat's paw batting at a glass display screen and a dangling cable, light quick thumps.", 1.2, False, "QF23 (10): Panther paws at Ratwing's marker / bats a cable"),
    ("bia_iris_snap_open", "A huge mechanical camera-shutter iris snapping open very fast: a sharp rising whirr and a crisp metallic click as overlapping blades retract, under one second.", 1, False, "QF23 (4): the mothership's iris snaps open just for a moment"),
    ("bia_iris_snap_shut", "A giant mechanical aperture spiralling shut very quickly: a fast whirr ending in one solid heavy metallic clack.", 1, False, "QF23 (4): the iris spirals shut behind the saucer"),
    ("blade_zap", "A sharp crackling electric zap, bright arcing static discharge with a short sizzle, punchy, no explosion.", 0.8, False, "QF23 (21): an arc-blade hit shocks Ratwing"),
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
    # Round 15 (Mars - The Divided Kingdoms)
    ("mars_amb_wind", "Dry wind across a vast open stone plain, soft gusts, faint grit hissing over rock, distant hollow resonance.", 20, True, "Mars: the plains, the ravine, the temple approach"),
    ("mars_amb_market", "A calm busy market street of a stone town: many soft distant voices murmuring (no words), footsteps, carts rolling, light clatter of goods.", 20, True, "Mars: the settlements (crowd / market ambience)"),
    ("mars_barrier_hum", "Steady low electric hum of a large energy barrier wall, with a faint crackle.", 5, True, "Mars: the checkpoint barrier, Viper's barriers"),
    ("mars_barrier_on", "An energy barrier wall snapping on: a rising electric surge and a firm hum.", 1.5, False, "Mars: a barrier rises (encounter)"),
    ("mars_barrier_off", "An energy barrier wall switching off: a descending electric whine fading out.", 1.5, False, "Mars: a barrier drops"),
    ("mars_machinery", "Old heavy stone-and-metal machinery working slowly, deep grinding gears and counterweights, measured clanks.", 10, True, "Mars: the ancient mechanism, the controls"),
    ("mars_bridge_align", "Enormous ancient stone bridge sections grinding and shifting into alignment, deep rumbling, heavy locking thuds, dust.", 6, False, "Mars: the ancient structures align (section 6)"),
    ("mars_defense_arm", "Defensive machinery powering up across a stone bridge: rising servo whine, heavy mechanical clanks, warning tones.", 3, False, "Mars: Viper activates the crossing's defenses (section 7)"),
    ("mars_section_slide", "A heavy metal bridge section sliding on rails, grinding, then a solid stop.", 2.5, False, "Mars: the moving sections (encounter)"),
    ("mars_platform_rise", "A heavy elevated metal platform rising on hydraulics, deep hum and hiss, a final clunk.", 4, False, "Mars: Viper arrives on his platform (section 3)"),
    ("mars_control_on", "An ancient control mechanism engaging: a deep stone clunk, then a resonant rising tone.", 2, False, "Mars: a control / relay restored"),
    ("mars_restraint", "A containment field closing around someone: soft electric shimmer tightening into a steady quiet hum.", 2.5, False, "Mars: Viper is contained"),
    ("mars_vision_stone", "Soft stone grinding slowly on stone, a gentle distant wind, very quiet, no music.", 12, False, "Mars: the vision (its video is nearly silent)"),
    ("mars_transport_hum", "A heavy cargo hover-transport moving slowly, low steady engine hum with a soft whine.", 6, True, "Mars: the supply transport"),
    # Round 16 - the asteroid, the Great Machine Age (riveted steam-age machinery)
    ("ast_amb_factory", "A vast enclosed factory cavern full of old steam-age machinery working on its own: rhythmic pistons, turning gear wheels, distant hissing steam, conveyors rattling, deep metallic echo, no people.", 20, True, "Asteroid: the city that still works (the cavern)"),
    ("ast_amb_quiet", "A vast metal cavern almost silent after machinery has stopped: a faint low electrical hum, a slow drip, distant settling metal ticks.", 20, True, "Asteroid: preservation mode (the cavern falls quiet)"),
    ("ast_steam_hiss", "A short strong burst of steam venting from a pressure valve, hiss fading.", 1.5, False, "Asteroid: steam vents, machines venting"),
    ("ast_conveyor", "A heavy industrial conveyor belt running steadily, rollers clattering, chain links ticking.", 8, True, "Asteroid: the receiving conveyor, the archive conveyor"),
    ("ast_rail_carrier", "A suspended rail carrier gliding past overhead on a steel rail, rolling wheels and a soft mechanical whir, passing by.", 4, False, "Asteroid: the rail carriers in the cavern"),
    ("ast_junction_turn", "A huge riveted steel turntable rotating on its bearing, deep grinding rumble, gears engaging, then a heavy locking clank.", 4, False, "Asteroid: a rail junction aligning (AST_03)"),
    ("ast_route_lights", "A row of electric route lamps switching on one after another down a long line, soft relay clicks and a faint rising hum.", 2.5, False, "Asteroid: the route lights run ahead"),
    ("ast_scanner", "A mechanical scanner arm humming while it scans, a soft oscillating electronic tone and small servo ticks.", 4, True, "Asteroid: the receiving scanner"),
    ("ast_scanner_turn", "A heavy mechanical arm swivelling on its joint, servo whine and a clunk as it stops.", 1.5, False, "Asteroid: the scanner redirected (telekinesis)"),
    ("ast_door_manual", "A large manual industrial door wheel being turned, ratchet clicks, then a heavy shutter rolling up.", 3, False, "Asteroid: the service entrance opens"),
    ("ast_pressurize", "A sealed metal chamber pressurizing: rising air rush, valves hissing, a soft final clunk.", 3, False, "Asteroid: the receiving chamber pressurizes"),
    ("ast_servo_heavy", "A huge machine's joint moving: deep hydraulic servo whine with metallic creak, precise and controlled.", 2, False, "Asteroid: the Machinist's servos"),
    # Round 17: the Biavian mothership (Veyra) and Ratwing
    ("bia_iris_open", "An enormous mechanical iris aperture opening in a spaceship hull: overlapping metal segments rotating and sliding apart, a deep smooth hum, soft precise clicks.", 4, False, "Round 17: the mothership's iris opens (BIA_02)"),
    ("bia_iris_close", "A giant mechanical iris closing: metal segments spiralling shut, smooth sliding, a final soft deep seal.", 3, False, "Round 17: the iris closes behind the saucer"),
    ("bia_relay_on", "A communication relay coming back to life: a soft rising harmonic hum, then a clear gentle chime, calm and clean.", 2.5, False, "Round 17: a relay restored"),
    ("bia_partition", "A large smooth security partition sliding shut across a tall shaft: soft hydraulic sigh, a deep sealing thud with a short echo.", 2.5, False, "Round 17: a partition closes"),
    ("bia_barrier", "An energy barrier sealing a doorway: rising electric hum ending in a firm low clunk.", 2, False, "Round 17: the compartment barrier seals"),
    ("bia_echo", "A faint ghostly shimmering tone like a distant memory, airy whispering texture, soft and unsettling but gentle.", 3, False, "Round 17: a memory echo appears"),
    ("bia_route_share", "A gentle sweeping shimmer of soft tones, like light passing along a path, calm and warm.", 2.5, False, "Round 17: Nela shares the route (navigation impression)"),
    ("bia_bridge", "Long suspended bridge segments swinging into line: smooth metallic sliding, then three soft locking clunks.", 3.5, False, "Round 17: the suspended bridge aligns"),
    ("bia_seam", "A hidden wall panel's latch clicking open and the panel sliding back smoothly.", 1.5, False, "Round 17: the seam's release"),
    ("bia_airlock", "A sleek airlock door sliding open with a short pressure hiss.", 1.5, False, "Round 17: the quiet passage's airlock doors"),
    ("ratwing_screech", "A sly raspy bat-like creature shriek, short, nasal and sharp, echoing in a large space.", 1.5, False, "Round 17: Ratwing's telegraph"),
    ("ratwing_flap", "Heavy leathery wing beats of a large bat-like creature taking off: three strong flaps with air whooshes.", 1.8, False, "Round 17: Ratwing's wings"),
    ("ratwing_strike", "Sharp claws striking a glass barrier hard, a cracking thud and a scrape.", 1, False, "Round 17: Ratwing hits a partition / barrier"),
    # QF21 (5): the Machinist's heavy steps (four takes so it never sounds looped), his servos, the piston hiss; System's whistle
    ("machinist_step_1", "One single heavy footstep of a four-metre iron machine on a metal floor: a deep metallic clunk with a short low boom, a brief piston hiss after it.", 1, False, "QF21: the Machinist's step (take 1)"),
    ("machinist_step_2", "A single very heavy iron foot landing on a steel deck plate: deep resonant clank, faint rattle of loose armour plates, low thud.", 1, False, "QF21: the Machinist's step (take 2)"),
    ("machinist_step_3", "One massive mechanical footfall on riveted metal flooring: a dull deep clunk, a short hydraulic sigh, metal plates rattling once.", 1, False, "QF21: the Machinist's step (take 3)"),
    ("machinist_step_4", "A giant robot's single footstep on an iron floor in a large hall: low heavy metallic impact, slight ring, a hiss of pistons.", 1, False, "QF21: the Machinist's step (take 4)"),
    ("machinist_servo", "Large industrial servo motors whirring as a heavy mechanical arm turns, smooth rising and falling whine, low gear hum.", 1.5, False, "QF21: the Machinist's servos on turns and arm moves"),
    ("machinist_hiss", "A short burst of pressurised steam escaping from heavy hydraulic pistons, sharp hiss fading out.", 1, False, "QF21: his piston legs"),
    ("ast_steam_whistle", "A small brass steam whistle giving one short cheerful toot with a puff of steam, old machinery.", 1, False, "QF21 (2): System's announcement whistle"),
    ("ast_servo_small", "A small machine's servo motors whirring and ticking briefly.", 1, False, "Asteroid: the maintenance machine, the security machines"),
    ("ast_cutter", "A large industrial cutting machine slicing metal plate: grinding, high metallic scream, sparks crackling.", 4, False, "Asteroid: the cutting mechanism (an archive panel)"),
    ("ast_shutters", "Heavy steel security shutters descending one after another, rumbling rollers and loud locking thuds.", 4, False, "Asteroid: the hall's security shutters close (AST_04)"),
    ("ast_alarm", "A low old-fashioned mechanical alarm horn sounding twice in a big metal hall, echoing.", 2.5, False, "Asteroid: the hall seals (AST_04)"),
    ("ast_static", "A radio communicator link breaking up: crackling static, distorted fragments, then dropping into silence.", 3, False, "Asteroid: the lost connection (AST_06)"),
    ("ast_platform_retract", "A heavy metal platform suddenly retracting on rails with a loud clank and a scraping slide.", 1.5, False, "Asteroid: the platform retracts (AST_06)"),
    ("ast_clamp", "A massive mechanical clamp slamming shut on steel: hydraulic hiss and a deep, final clang.", 2, False, "Asteroid: a service-station clamp locks a tool arm (AST_07)"),
    ("ast_cradle", "A large maintenance cradle closing its padded arms around a machine: servo whir, soft heavy locking clunks.", 3, False, "Asteroid: the cradle immobilizes the Machinist (AST_07)"),
    ("ast_blade_draw", "Two blades of energy igniting: a soft rising electric hum with a ribbon-like shimmer.", 1.2, False, "Asteroid: 7 draws the arc blades"),
    ("ast_blade_sheath", "Two energy blades switching off: a soft descending electric hum fading.", 1, False, "Asteroid: 7 sheathes the arc blades"),
    ("ast_blade_swing", "A quick swish of a humming energy blade through the air.", 0.6, False, "Asteroid: an arc blade swing"),
    ("ast_blade_hit", "An energy blade cutting a power line: a sharp electric crack, sparks fizzing, a short power-down whine.", 1.2, False, "Asteroid: an arc blade severs a power coupling / line"),
    ("ast_power_down", "A large machine losing power: lights buzzing out, a long descending electric whine, motors winding down.", 3, False, "Asteroid: a machine's power severed, preservation mode"),
    ("ast_coolant", "Coolant rushing through big pipes, a deep liquid flow, valves opening, steam dying down.", 4, False, "Asteroid: the cooling control (AST_05)"),
    ("ast_bird_chirp", "A small clockwork toy bird: a tiny wind-up ratchet, one uneven mechanical chirp, and a little tin wing flap.", 2.5, False, "Asteroid: the mechanical bird (AST_03_08; the vision)"),
    ("ast_vault_open", "A heavy protected storage vault unlocking: bolts withdrawing one after another, a big door swinging open with a deep groan.", 4, False, "Asteroid: protected storage opens (AST_09)"),
    # Round 18: Cattalus (Docs/SFX-LIST.md "Cattalus")
    ("cat_crowd_cheer", "A large outdoor crowd of cats cheering at a race: excited chirps, trills and happy meows over a festive crowd roar.", 5, False, "Round 18: CAT_07 finish, CAT_08"),
    ("cat_crowd_ambience", "A busy, relaxed outdoor plaza of cats: soft chirps, distant meows, padding feet, a gentle breeze, far-off chimes.", 10, True, "Round 18: Cattalus streets (loop)"),
    ("cardboard_rustle", "A cat climbing into a cardboard box: cardboard creaking and flexing, soft thumps, paws shuffling.", 2.5, False, "Round 18: the boxes, the Deluxe Retreat"),
    ("paper_crinkle", "Packing paper being pawed and arranged: crisp crinkling and rustling.", 2.5, False, "Round 18: CAT_08"),
    ("race_start_lights", "Race starting lights: three short low beeps and one long high beep.", 4, False, "Round 18: CAT_07_01"),
    ("cat_racer_hover", "A small hover racing vehicle passing: a smooth rising and falling electric whine with a soft air rush.", 3, False, "Round 18: Duchess's racer"),
    ("sphere_lines", "A smooth stone sphere awakening: a soft rising glassy shimmer with faint ringing harmonics.", 2.5, False, "Round 18: CAT_09 touch"),
    ("cradle_release", "A small ornate metal cradle opening: a soft precise click and a gentle metallic sigh.", 1.5, False, "Round 18: CAT_09"),
    ("giant_purr", "An enormous cat purring deeply and contentedly: a low, warm, rumbling purr.", 6, False, "Round 18: CAT_10"),
    ("grass_run", "Running through tall dry grass: swishing stalks and light footfalls with heavier soft thuds alongside.", 6, False, "Round 18: CAT_10 segment 4"),
    ("cat_gate_open", "A small ornate garden gate swinging open on a squeaky hinge, a light metal latch click.", 1.5, False, "Round 18: CAT_02 the public gate"),
    ("cat_screen_jingle", "A short bright cheerful sports television jingle with a whoosh, upbeat brass stab, two seconds.", 2.5, False, "Round 18: the Cat-a-tron (highlights, the prize ad)"),
    ("cat_meadow_breeze", "A warm open meadow: a soft steady breeze through tall grass, a few distant birds, insects humming gently.", 12, True, "Round 18: CAT_10 the meadow (loop)"),
    ("cat_toy_swing", "A large soft hanging toy swinging past on a rope: a whoosh and a creak of the rope, a little jingle bell.", 1.5, False, "Round 18: the toy boulevard's swinging obstacles"),
    ("cat_gate_chime", "A small bright chime as a racer passes through a glowing gate, a soft sparkle.", 1, False, "Round 18: a race gate passed")
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
