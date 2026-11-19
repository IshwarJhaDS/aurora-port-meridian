// Single source of truth for generated content. The generator scripts in tools/
// read prompts from here; the game reads placements and audio ids from here.
// Every entry is optional at runtime: anything not yet generated falls back to
// the procedural art and browser speech that ship with the project.

export const MANIFEST_URL = './assets/generated/manifest.json';

// Shared style suffix keeps Tripo output consistent across the set.
const STYLE = 'Nepal, Kathmandu valley architecture, hand-crafted, weathered, realistic materials, game-ready prop, single object, no ground plane, no background';

/**
 * Tripo models. `height` is the in-game height in metres the model is scaled to.
 * `replaces` names a procedural landmark that is skipped once the model loads.
 * `solid` adds a box collider sized from the model footprint.
 */
export const MODELS = Object.freeze([
  {
    id: 'pagoda', height: 15, solid: true, replaces: 'pagoda', faceLimit: 60000,
    prompt: `Three-tiered Newari pagoda temple like those in Kathmandu Durbar Square: stepped red-brick and stone plinth with nine steps, carved dark sal-wood struts and lattice windows, three sloping copper-gold roofs with upturned corners and hanging bells, golden gajur finial on top. ${STYLE}`,
    placements: [{ x: 47, z: 47, yaw: Math.PI / 4 }]
  },
  {
    id: 'stupa', height: 17, solid: true, replaces: 'stupa', faceLimit: 60000,
    prompt: `Buddhist stupa like Boudhanath and Swayambhunath: wide white-washed dome on a three-level square mandala base, golden square harmika painted with the all-seeing Buddha eyes on each side, thirteen golden tiered rings up to a gilded parasol spire, strings of colourful prayer flags running from the spire. ${STYLE}`,
    placements: [{ x: -120, z: 214, yaw: 0 }]
  },
  {
    id: 'prayer_wheels', height: 2.3, solid: false, faceLimit: 15000,
    prompt: `Row of six brass and copper Tibetan Buddhist prayer wheels engraved with Om Mani Padme Hum, mounted in a red-painted wooden frame with a small green-tiled roof. ${STYLE}`,
    placements: [{ x: -131, z: 214, yaw: Math.PI / 2 }, { x: -109, z: 214, yaw: -Math.PI / 2 }, { x: -120, z: 226, yaw: Math.PI }, { x: 30, z: 52, yaw: 0 }]
  },
  {
    id: 'chautari', height: 9, solid: true, replaces: 'chautari', faceLimit: 40000,
    prompt: `Nepali chautari resting place: square two-tier stone platform with steps, a large leafy peepal tree growing from the middle, worn stone edges for sitting. ${STYLE}`,
    placements: [{ x: 54, z: 352, yaw: 0 }, { x: 55, z: 64, yaw: .4 }]
  },
  {
    id: 'shrine', height: 2.6, solid: false, faceLimit: 15000,
    prompt: `Small Nepali roadside Hindu shrine: red brick and carved stone base, little one-tier pagoda roof, stone Ganesh idol inside smeared with red tika powder, orange marigold garlands and brass oil lamps. ${STYLE}`,
    placements: [{ x: 24, z: 58, yaw: Math.PI / 2 }, { x: 92, z: 28, yaw: Math.PI / 2 }, { x: -148, z: 120, yaw: Math.PI / 2 }]
  },
  {
    id: 'tea_stall', height: 3.8, solid: false, faceLimit: 25000,
    prompt: `Small Nepali roadside tea shop (chiya pasal): wooden counter with steel kettles and glass jars of biscuits, rusty corrugated tin roof, hand-painted colourful signboard, wooden bench in front. ${STYLE}`,
    placements: [{ x: 96, z: 55, yaw: Math.PI / 2 }]
  },
  {
    id: 'doonga', height: 1.3, solid: false, replaces: 'boats', faceLimit: 15000, water: true,
    prompt: `Traditional wooden rowing boat (doonga) from Phewa Lake, Pokhara: long narrow hull painted in bright red, blue and yellow stripes, two wooden oars resting inside. ${STYLE}`,
    placements: [{ x: 468, z: -40, yaw: .3 }, { x: 476, z: 22, yaw: -.5 }, { x: 462, z: 92, yaw: 1.2 }, { x: 492, z: 160, yaw: .1 }, { x: 455, z: 48, yaw: 2.6 }]
  },
  // ── 2000s nostalgia set: the bazaar row east of the tea shop, and props used by minigames.
  {
    id: 'cyber_cafe', height: 4.2, solid: true, replaces: 'cyber', faceLimit: 40000,
    prompt: 'Small 2000s Nepali cyber cafe shopfront: rows of beige CRT computers in plywood cubicles, plastic chairs, hand-painted "CYBER" signboard, rolling shutter. Nepal, early 2000s, realistic materials, game-ready prop, no background',
    placements: [{ x: 140, z: 62, yaw: 0 }]
  },
  {
    id: 'salon_chair', height: 1.25, solid: false, replaces: 'salon_chair', faceLimit: 20000,
    prompt: 'Vintage red leather barber chair with chrome hydraulic base and footrest, from a 2000s Nepali roadside salon, single object. Nepal, early 2000s, realistic materials, game-ready prop, no background',
    placements: [{ x: 124, z: 61.4, yaw: Math.PI }]
  },
  {
    id: 'cassette_player', height: .34, solid: false, replaces: 'cassette', faceLimit: 8000,
    prompt: '2000s Chinese portable cassette player boombox, silver plastic, two speakers, antenna, single object. Nepal, early 2000s, realistic materials, game-ready prop, no background',
    placements: [{ x: 125.6, z: 60.4, yaw: Math.PI, y: 1.32 }, { x: 93.4, z: 55, yaw: Math.PI / 2, y: .82 }]
  },
  {
    id: 'lattu', height: .12, solid: false, faceLimit: 8000,
    prompt: 'Traditional Nepali wooden spinning top (lattu) with a pointed iron tip, painted red and green rings, with a coiled cotton string, single object. Nepal, early 2000s, realistic materials, game-ready prop, no background',
    placements: []
  },
  // Whole buildings: when present they replace the procedural blocks lot by lot.
  {
    id: 'building_newari', height: 13, solid: false, building: 'town', faceLimit: 45000,
    prompt: 'Four-storey traditional Newari townhouse from Kathmandu valley: exposed red brick walls, ornate carved dark sal-wood windows (ankhi jhyal) on every floor, a wooden balcony, sloping tiled roof with wide eaves, small shop with rolling metal shutter on the ground floor, weathered. Single building, game-ready, no ground plane, no background',
    placements: []
  },
  {
    id: 'building_concrete', height: 16, solid: false, building: 'town', faceLimit: 45000,
    prompt: 'Five-storey 2000s Nepali concrete house in a crowded bazaar: pastel painted plaster (pink or mint), small balconies with iron grills, black plastic water tank and TV antenna on the flat roof, rebar sticking out of an unfinished top floor, two shopfronts with rolling shutters and painted signboards. Single building, game-ready, no ground plane, no background',
    placements: []
  },
  {
    id: 'building_village', height: 6.5, solid: false, building: 'village', faceLimit: 30000,
    prompt: 'Two-storey Nepali hill village house: mud-plastered stone walls painted red-ochre below and white above, small wooden windows, corrugated tin roof weighted with stones, maize cobs hanging to dry under the eaves, a small porch. Single building, game-ready, no ground plane, no background',
    placements: []
  },
  {
    id: 'kite_changa', height: 1.0, solid: false, faceLimit: 8000,
    prompt: 'Nepali changa kite, diamond shape, bright red and yellow paper, bamboo frame, with a wooden spool of manja string (lattai), single object. Nepal, early 2000s, realistic materials, game-ready prop, no background',
    placements: []
  }
]);

/**
 * Optional character models (used by the Unity build). Generate them in Tripo as
 * T-pose humanoids; rig + animate in Tripo ("idle" and "walk") for animated NPCs.
 * Missing files fall back to simple built-in figures.
 */
const CHARACTER_STYLE = 'full body, standing in T-pose, arms out, stylized realistic game character, clean topology, neutral background';
export const CHARACTERS = Object.freeze([
  { id: 'player', height: 1.75, prompt: `Young Nepali man in his twenties, short black hair, faded blue jeans, plain white t-shirt under an open checked flannel shirt, worn sneakers, small backpack. ${CHARACTER_STYLE}` },
  { id: 'mira', height: 1.6, prompt: `Nepali woman in her thirties, community organizer, long black hair in a braid, maroon kurta suruwal with a cream shawl, small red tika on forehead, sandals. ${CHARACTER_STYLE}` },
  { id: 'leo', height: 1.7, prompt: `Friendly Nepali tea-shop owner in his fifties, thin moustache, colourful patterned dhaka topi cap, brown sweater over a collared shirt, grey trousers, rolled-up sleeves, apron. ${CHARACTER_STYLE}` },
  { id: 'anika', height: 1.62, prompt: `Nepali woman doctor in her forties, hair in a bun, white doctor coat over a green kurta, stethoscope around neck, glasses. ${CHARACTER_STYLE}` },
  { id: 'tess', height: 1.72, prompt: `Sherpa man in his thirties, courier dispatcher, short hair, red down puffer jacket, cargo trousers, hiking boots, clipboard in hand. ${CHARACTER_STYLE}` },
  { id: 'sanjay', height: 1.7, prompt: `Nepali electrician in his forties, Rai ethnicity, short hair, orange work overalls, tool belt with pliers and tape, rubber boots. ${CHARACTER_STYLE}` },
  { id: 'rosa', height: 1.58, prompt: `Newari woman shopkeeper in her fifties, black hair, traditional red and black haku patasi sari, shawl, gold earrings, sandals. ${CHARACTER_STYLE}` },
  { id: 'eli', height: 1.68, prompt: `Tamang gardener in his sixties, weathered face, woollen cap, brown jacket, khaki trousers, rubber boots, gardening gloves. ${CHARACTER_STYLE}` },
  { id: 'juno', height: 1.6, prompt: `Limbu woman photographer in her twenties, shoulder-length hair, denim jacket, yellow scarf, jeans, sneakers, DSLR camera on a strap. ${CHARACTER_STYLE}` },
  { id: 'dhane', height: 1.75, prompt: `Dhane, a Nepali trail scout. ${CHARACTER_STYLE}` },
  { id: 'resident_m', height: 1.7, prompt: `Ordinary Nepali man in his forties, black dhaka topi cap, grey suit jacket over a sweater, dark trousers, leather shoes. ${CHARACTER_STYLE}` },
  { id: 'resident_f', height: 1.58, prompt: `Ordinary Nepali woman in her forties, black hair tied back, bright green kurta suruwal with a dupatta scarf, sandals, cloth shopping bag. ${CHARACTER_STYLE}` },
  { id: 'car', height: 1.5, prompt: 'Small white 2000s Maruti Suzuki 800 style hatchback car as seen on Nepali streets, slightly dusty, Nepali red number plate, realistic, single vehicle, no background' }
]);

/** ElevenLabs sound effects. Loops crossfade by district, weather and time of day. */
export const AMBIENCE = Object.freeze([
  {
    "id": "bazaar",
    "loop": true,
    "duration": 20,
    "prompt": "Fictional coastal city Aurora, lively old-town street, layered indistinct crowd murmur, market stalls, soft distant traffic, pigeons and footsteps. Natural stereo ambience, no intelligible speech, no music, smooth seamless loop."
  },
  {
    "id": "temple",
    "loop": true,
    "duration": 20,
    "prompt": "Quiet weathered courtyard in a mysterious coastal city, gentle wind through hanging cloth, a distant resonant metal wind chime, soft pigeons, subdued faraway city activity. Natural stereo seamless loop, no music or intelligible words."
  },
  {
    "id": "lake",
    "loop": true,
    "duration": 20,
    "prompt": "Coastal harbor at dawn, gentle water against wooden piers, distant seabirds, quiet boat ropes creaking, broad soft sea breeze. Immersive natural stereo loop without music."
  },
  {
    "id": "hills",
    "loop": true,
    "duration": 20,
    "prompt": "Pine forest above a coastal city, soft branches in the breeze, scattered birds and insects, spacious peaceful atmosphere with a faint mysterious distant wind. Seamless natural stereo loop, no music."
  },
  {
    "id": "night",
    "loop": true,
    "duration": 20,
    "prompt": "Aurora city after dark, soft urban hum, cool harbor breeze, occasional distant dog and quiet insects, sparse distant cars, mysterious calm. Natural stereo seamless loop, no music or voices."
  },
  {
    "id": "rain",
    "loop": true,
    "duration": 20,
    "prompt": "Soft steady rainfall on old stone paving and sheet-metal awnings, detailed droplets and gentle runoff, immersive stereo seamless ambience, no thunder or music."
  }
]);

export const EFFECTS = Object.freeze([
  {
    "id": "engine_loop",
    "duration": 5,
    "loop": true,
    "prompt": "Steady warm idle of a compact vintage petrol car engine, low mechanical rumble with subtle cyclical vibration, close clean engine sound, absolutely constant RPM, seamless loop, no acceleration, horns, voices, or music."
  },
  {
    "id": "bell",
    "duration": 3,
    "prompt": "One resonant old brass courtyard bell, a clean single strike with a delicate natural decay. Isolated game effect, no background."
  },
  {
    "id": "success",
    "duration": 3,
    "prompt": "Original short hopeful game mission-complete sting, warm plucked strings, two bright bell notes and a restrained hand drum accent, elegant cinematic finish."
  },
  {
    "id": "bus_horn",
    "duration": 2,
    "prompt": "One short vintage city bus horn, warm mechanical two-tone honk, outdoor distance, no traffic background."
  },
  {
    "id": "step_stone",
    "duration": 0.5,
    "prompt": "One isolated shoe footstep on dry stone paving, crisp sole contact and low body weight, dry close Foley, no echo or background."
  },
  {
    "id": "step_road",
    "duration": 0.5,
    "prompt": "One isolated sneaker footstep on coarse asphalt, firm low thud with slight gritty scrape, dry close Foley, no background."
  },
  {
    "id": "step_grass",
    "duration": 0.5,
    "prompt": "One isolated boot footstep on soft grass and dirt, cushioned crunch of leaves and soil, close dry Foley, no background."
  },
  {
    "id": "land_soft",
    "duration": 0.7,
    "prompt": "One grounded human landing from a small jump onto stone, two shoe soles touch with a compact soft thud and brief cloth rustle, dry isolated Foley."
  },
  {
    "id": "land_heavy",
    "duration": 1,
    "prompt": "One heavy human landing on stone, deep grounded shoe thump, tiny grit scatter and clothing rustle. Restrained cinematic Foley, isolated, no voice or music."
  },
  {
    "id": "cloth_whoosh",
    "duration": 0.7,
    "prompt": "One quick martial arts arm swing with a loose cotton sleeve, compact airy cloth swish, dry close Foley, no impact or voice."
  },
  {
    "id": "blade_swing",
    "duration": 0.7,
    "prompt": "One fast sword swing through air, tight metallic airy swish, brief convincing sharp movement, isolated Foley, no impact, voice or background."
  },
  {
    "id": "bow_release",
    "duration": 0.7,
    "prompt": "One traditional wooden bow firing, taut string snaps forward and a quick arrow flight swish, dry close isolated game sound, no impact or voice."
  },
  {
    "id": "vehicle_impact",
    "duration": 1.4,
    "prompt": "One compact car bumping a stone barrier at moderate speed, short metal and bumper thud with a small rattle, believable restrained impact, no explosion or voices."
  },
  {
    "id": "ui_confirm",
    "duration": 0.6,
    "prompt": "One soft elegant game interface confirmation, a warm wooden click with a tiny bright glass chime, understated clean short single sound."
  }
]);

export const MUSIC = Object.freeze([
  {
    "id": "theme",
    "lengthMs": 60000,
    "prompt": "Original instrumental score for Aurora, a mysterious lively fictional coastal city and martial arts adventure. Warm analog synth atmosphere, expressive plucked strings, upright bass, brushed percussion and a delicate bell motif. 88 BPM. Curious, hopeful, cinematic and spacious. Strong memorable original motif, no vocals, no imitation of existing songs. Smooth recurring structure suitable for unobtrusive exploration."
  },
  {
    "id": "night",
    "lengthMs": 60000,
    "prompt": "Original instrumental nighttime game score for Aurora. Cool harbor air and neon reflected on stone streets, intimate electric piano, soft analog pads, low cello, sparse bamboo flute phrases and restrained brushed drums. 72 BPM, mysterious but comforting, gently suspenseful, no vocals. Spacious repeating arrangement with soft ending."
  },
  {
    "id": "exploration",
    "lengthMs": 60000,
    "prompt": "Original instrumental exploration game score, hill paths and a vast harbor above the fictional city Aurora. Warm plucked strings and acoustic guitar, spacious soft synth textures, breathy flute and sparse hand percussion. 82 BPM, wonder and discovery, quietly adventurous, no vocals, no dramatic finale, unobtrusive recurring motif."
  },
  {
    "id": "tension",
    "lengthMs": 45000,
    "prompt": "Original instrumental martial arts pursuit game score for Aurora, nimble hand drums and woodblocks, staccato low strings, driving bass pulse and short plucked string accents. 120 BPM. Energetic, playful determination with cinematic suspense, clear rhythmic movement, no vocals, no melodies from existing films, repeating arrangement with restrained ending."
  }
]);

/**
 * Voice casting for ElevenLabs. `voice` names an env variable holding the voice id,
 * so you can pick South Asian / Nepali-accented voices from your ElevenLabs library.
 */
export const VOICE_CAST = Object.freeze({
  mira: 'ELEVENLABS_VOICE_FEMALE', leo: 'ELEVENLABS_VOICE_MALE', anika: 'ELEVENLABS_VOICE_FEMALE_2', tess: 'ELEVENLABS_VOICE_MALE_2',
  sanjay: 'ELEVENLABS_VOICE_MALE_3', rosa: 'ELEVENLABS_VOICE_FEMALE_3', eli: 'ELEVENLABS_VOICE_MALE', juno: 'ELEVENLABS_VOICE_FEMALE_2',
  'resident-f': 'ELEVENLABS_VOICE_FEMALE', 'resident-m': 'ELEVENLABS_VOICE_MALE'
});

/** Stable key for a spoken sentence (FNV-1a over normalized text), shared by tools and runtime. */
export function voiceKey(speaker, sentence) {
  const text = `${speaker}|${sentence.trim().replace(/\s+/g, ' ').toLowerCase()}`;
  let h = 0x811c9dc5;
  for (let i = 0; i < text.length; i++) { h ^= text.charCodeAt(i); h = Math.imul(h, 0x01000193); }
  return (h >>> 0).toString(16).padStart(8, '0');
}

/** Sentences that change every minute (the clock) are shown but not voiced. */
export const isUnvoiced = sentence => /^It is \d{1,2}:\d{2}\.$/.test(sentence.trim());

/** Splits dialogue into sentences so clips can be reused across combined lines. */
export function sentences(text) {
  return (String(text).match(/[^.!?…]+[.!?…]+["”’)]*|[^.!?…]+$/g) || []).map(s => s.trim()).filter(Boolean);
}
