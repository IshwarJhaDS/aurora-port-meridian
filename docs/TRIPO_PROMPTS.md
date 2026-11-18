# Tripo prompts for Phewa Valley

Copy each prompt into [Tripo](https://www.tripo3d.ai) (Text to 3D), download as **GLB**, and save it with the exact file name shown.

**Where the files go (Unity):** `unity/PhewaValley/Assets/StreamingAssets/PhewaValley/models/<file>.glb`. In the editor, use **Phewa Valley ▸ Open Tripo models folder**. Press Play; the game picks them up automatically and anything missing keeps its built-in look.

**Settings that work well:** Text to 3D, latest model, Texture **on**, PBR **on**, Download format **GLB**. Do not choose Draco or mesh compression.

**Polycount:** set Tripo’s *Face limit* (in the advanced generation settings) to the number shown for each model. All numbers are **triangles**: keep Tripo’s topology on *Triangle*. Quad mode counts quads (2 triangles each) and exports FBX, which the game cannot load. Smart Low Poly caps out at about 20,000 triangles, so use the normal generation for the 40k–60k landmarks (pagoda, stupa, chautari, salon, cyber cafe). If a model comes out heavier, run Tripo’s *Smart Low Poly* or *Retopology* before downloading. Keep textures at 2K; detail mostly comes from the texture, not the triangles. With every model installed, the worst case (all 48 residents in view) is about 1.4 M triangles, which a mid-range desktop GPU handles comfortably.

## 1. Landmarks and props (placed in the world now)

| File | Face limit | In-game height | Where it appears |
|---|---|---|---|
| `pagoda.glb` | 60,000 | 15 m | 1 placement · replaces the built-in pagoda |
| `stupa.glb` | 60,000 | 17 m | 1 placement · replaces the built-in stupa |
| `prayer_wheels.glb` | 15,000 | 2.3 m | 4 placements |
| `chautari.glb` | 40,000 | 9 m | 2 placements · replaces the built-in chautari |
| `shrine.glb` | 15,000 | 2.6 m | 3 placements |
| `tea_stall.glb` | 25,000 | 3.8 m | 1 placement |
| `doonga.glb` | 15,000 | 1.3 m | 5 placements · replaces the built-in boats |
| `cyber_cafe.glb` | 40,000 | 4.2 m | 1 placement · replaces the built-in cyber |
| `salon_chair.glb` | 20,000 | 1.25 m | 1 placement · replaces the built-in salon_chair |
| `cassette_player.glb` | 8,000 | 0.34 m | 2 placements · replaces the built-in cassette |
| `lattu.glb` | 8,000 | 0.12 m | 0 placement |
| `building_newari.glb` | 45,000 | 13 m | 0 placement |
| `building_concrete.glb` | 45,000 | 16 m | 0 placement |
| `building_village.glb` | 30,000 | 6.5 m | 0 placement |
| `kite_changa.glb` | 8,000 | 1 m | 0 placement |

### pagoda.glb

Face limit: **60,000**

```text
Three-tiered Newari pagoda temple like those in Kathmandu Durbar Square: stepped red-brick and stone plinth with nine steps, carved dark sal-wood struts and lattice windows, three sloping copper-gold roofs with upturned corners and hanging bells, golden gajur finial on top. Nepal, Kathmandu valley architecture, hand-crafted, weathered, realistic materials, game-ready prop, single object, no ground plane, no background
```

### stupa.glb

Face limit: **60,000**

```text
Buddhist stupa like Boudhanath and Swayambhunath: wide white-washed dome on a three-level square mandala base, golden square harmika painted with the all-seeing Buddha eyes on each side, thirteen golden tiered rings up to a gilded parasol spire, strings of colourful prayer flags running from the spire. Nepal, Kathmandu valley architecture, hand-crafted, weathered, realistic materials, game-ready prop, single object, no ground plane, no background
```

### prayer_wheels.glb

Face limit: **15,000**

```text
Row of six brass and copper Tibetan Buddhist prayer wheels engraved with Om Mani Padme Hum, mounted in a red-painted wooden frame with a small green-tiled roof. Nepal, Kathmandu valley architecture, hand-crafted, weathered, realistic materials, game-ready prop, single object, no ground plane, no background
```

### chautari.glb

Face limit: **40,000**

```text
Nepali chautari resting place: square two-tier stone platform with steps, a large leafy peepal tree growing from the middle, worn stone edges for sitting. Nepal, Kathmandu valley architecture, hand-crafted, weathered, realistic materials, game-ready prop, single object, no ground plane, no background
```

### shrine.glb

Face limit: **15,000**

```text
Small Nepali roadside Hindu shrine: red brick and carved stone base, little one-tier pagoda roof, stone Ganesh idol inside smeared with red tika powder, orange marigold garlands and brass oil lamps. Nepal, Kathmandu valley architecture, hand-crafted, weathered, realistic materials, game-ready prop, single object, no ground plane, no background
```

### tea_stall.glb

Face limit: **25,000**

```text
Small Nepali roadside tea shop (chiya pasal): wooden counter with steel kettles and glass jars of biscuits, rusty corrugated tin roof, hand-painted colourful signboard, wooden bench in front. Nepal, Kathmandu valley architecture, hand-crafted, weathered, realistic materials, game-ready prop, single object, no ground plane, no background
```

### doonga.glb

Face limit: **15,000**

```text
Traditional wooden rowing boat (doonga) from Phewa Lake, Pokhara: long narrow hull painted in bright red, blue and yellow stripes, two wooden oars resting inside. Nepal, Kathmandu valley architecture, hand-crafted, weathered, realistic materials, game-ready prop, single object, no ground plane, no background
```

### cyber_cafe.glb

Face limit: **40,000**

```text
Small 2000s Nepali cyber cafe shopfront: rows of beige CRT computers in plywood cubicles, plastic chairs, hand-painted "CYBER" signboard, rolling shutter. Nepal, early 2000s, realistic materials, game-ready prop, no background
```

### salon_chair.glb

Face limit: **20,000**

```text
Vintage red leather barber chair with chrome hydraulic base and footrest, from a 2000s Nepali roadside salon, single object. Nepal, early 2000s, realistic materials, game-ready prop, no background
```

### cassette_player.glb

Face limit: **8,000**

```text
2000s Chinese portable cassette player boombox, silver plastic, two speakers, antenna, single object. Nepal, early 2000s, realistic materials, game-ready prop, no background
```

### lattu.glb

Face limit: **8,000**

```text
Traditional Nepali wooden spinning top (lattu) with a pointed iron tip, painted red and green rings, with a coiled cotton string, single object. Nepal, early 2000s, realistic materials, game-ready prop, no background
```

### building_newari.glb

Face limit: **45,000**

```text
Four-storey traditional Newari townhouse from Kathmandu valley: exposed red brick walls, ornate carved dark sal-wood windows (ankhi jhyal) on every floor, a wooden balcony, sloping tiled roof with wide eaves, small shop with rolling metal shutter on the ground floor, weathered. Single building, game-ready, no ground plane, no background
```

### building_concrete.glb

Face limit: **45,000**

```text
Five-storey 2000s Nepali concrete house in a crowded bazaar: pastel painted plaster (pink or mint), small balconies with iron grills, black plastic water tank and TV antenna on the flat roof, rebar sticking out of an unfinished top floor, two shopfronts with rolling shutters and painted signboards. Single building, game-ready, no ground plane, no background
```

### building_village.glb

Face limit: **30,000**

```text
Two-storey Nepali hill village house: mud-plastered stone walls painted red-ochre below and white above, small wooden windows, corrugated tin roof weighted with stones, maize cobs hanging to dry under the eaves, a small porch. Single building, game-ready, no ground plane, no background
```

### kite_changa.glb

Face limit: **8,000**

```text
Nepali changa kite, diamond shape, bright red and yellow paper, bamboo frame, with a wooden spool of manja string (lattai), single object. Nepal, early 2000s, realistic materials, game-ready prop, no background
```

## 2. Characters and the car

In Tripo: Generate → then **Rig** (humanoid) → **Animate**, adding an *idle* and a *walk* animation → Export **GLB** (with animation). Unrigged models still work; they just do not animate.

| File | Face limit | Who |
|---|---|---|
| `player.glb` | 25,000 | You |
| `mira.glb` | 20,000 | Named character · id mira |
| `leo.glb` | 20,000 | Named character · id leo |
| `anika.glb` | 20,000 | Named character · id anika |
| `tess.glb` | 20,000 | Named character · id tess |
| `sanjay.glb` | 20,000 | Named character · id sanjay |
| `rosa.glb` | 20,000 | Named character · id rosa |
| `eli.glb` | 20,000 | Named character · id eli |
| `juno.glb` | 20,000 | Named character · id juno |
| `resident_m.glb` | 10,000 | Wandering residents (men) |
| `resident_f.glb` | 10,000 | Wandering residents (women) |
| `car.glb` | 20,000 | Your Maruti (not rigged) |

### player.glb

Face limit: **25,000**

```text
Young Nepali man in his twenties, short black hair, faded blue jeans, plain white t-shirt under an open checked flannel shirt, worn sneakers, small backpack. full body, standing in T-pose, arms out, stylized realistic game character, clean topology, neutral background
```

### mira.glb

Face limit: **20,000**

```text
Nepali woman in her thirties, community organizer, long black hair in a braid, maroon kurta suruwal with a cream shawl, small red tika on forehead, sandals. full body, standing in T-pose, arms out, stylized realistic game character, clean topology, neutral background
```

### leo.glb

Face limit: **20,000**

```text
Friendly Nepali tea-shop owner in his fifties, thin moustache, colourful patterned dhaka topi cap, brown sweater over a collared shirt, grey trousers, rolled-up sleeves, apron. full body, standing in T-pose, arms out, stylized realistic game character, clean topology, neutral background
```

### anika.glb

Face limit: **20,000**

```text
Nepali woman doctor in her forties, hair in a bun, white doctor coat over a green kurta, stethoscope around neck, glasses. full body, standing in T-pose, arms out, stylized realistic game character, clean topology, neutral background
```

### tess.glb

Face limit: **20,000**

```text
Sherpa man in his thirties, courier dispatcher, short hair, red down puffer jacket, cargo trousers, hiking boots, clipboard in hand. full body, standing in T-pose, arms out, stylized realistic game character, clean topology, neutral background
```

### sanjay.glb

Face limit: **20,000**

```text
Nepali electrician in his forties, Rai ethnicity, short hair, orange work overalls, tool belt with pliers and tape, rubber boots. full body, standing in T-pose, arms out, stylized realistic game character, clean topology, neutral background
```

### rosa.glb

Face limit: **20,000**

```text
Newari woman shopkeeper in her fifties, black hair, traditional red and black haku patasi sari, shawl, gold earrings, sandals. full body, standing in T-pose, arms out, stylized realistic game character, clean topology, neutral background
```

### eli.glb

Face limit: **20,000**

```text
Tamang gardener in his sixties, weathered face, woollen cap, brown jacket, khaki trousers, rubber boots, gardening gloves. full body, standing in T-pose, arms out, stylized realistic game character, clean topology, neutral background
```

### juno.glb

Face limit: **20,000**

```text
Limbu woman photographer in her twenties, shoulder-length hair, denim jacket, yellow scarf, jeans, sneakers, DSLR camera on a strap. full body, standing in T-pose, arms out, stylized realistic game character, clean topology, neutral background
```

### resident_m.glb

Face limit: **10,000**

```text
Ordinary Nepali man in his forties, black dhaka topi cap, grey suit jacket over a sweater, dark trousers, leather shoes. full body, standing in T-pose, arms out, stylized realistic game character, clean topology, neutral background
```

### resident_f.glb

Face limit: **10,000**

```text
Ordinary Nepali woman in her forties, black hair tied back, bright green kurta suruwal with a dupatta scarf, sandals, cloth shopping bag. full body, standing in T-pose, arms out, stylized realistic game character, clean topology, neutral background
```

### car.glb

Face limit: **20,000**

```text
Small white 2000s Maruti Suzuki 800 style hatchback car as seen on Nepali streets, slightly dusty, Nepali red number plate, realistic, single vehicle, no background
```

## 3. 2000s nostalgia set (next chapter)

These are for the upcoming street games, salon, cyber cafe and festivals. Generate them now if you like and keep them in the same folder; the next update will place them.

### chungi.glb

Face limit: **8,000**

```text
Nepali chungi: a small kick-ball made of bundled rubber bands, colourful red yellow and green rubber, single object. Nepal, early 2000s, realistic materials, game-ready prop, no background
```

### wrestling_cards.glb

Face limit: **8,000**

```text
Stack of 2000s WWE wrestling trading cards held by a rubber band, a few cards fanned out, shiny printed cards, single object. Nepal, early 2000s, realistic materials, game-ready prop, no background
```

### football_itta.glb

Face limit: **8,000**

```text
Two piles of red bricks used as a street football goal with a worn leather football, Nepali street game, single object. Nepal, early 2000s, realistic materials, game-ready prop, no background
```

### ping_swing.glb

Face limit: **15,000**

```text
Tall Dashain ping swing made from four long bamboo poles tied at the top with a rope seat, decorated with marigolds, single object. Nepal, early 2000s, realistic materials, game-ready prop, no background
```

### tihar_diyo.glb

Face limit: **8,000**

```text
Row of small clay oil lamps (diyo) burning with flames, on a stone step with marigold flowers, Tihar festival, single object. Nepal, early 2000s, realistic materials, game-ready prop, no background
```

### juju_dhau_stall.glb

Face limit: **20,000**

```text
Bhaktapur street stall selling juju dhau yogurt in clay pots, wooden table, tarpaulin roof, single object. Nepal, early 2000s, realistic materials, game-ready prop, no background
```

### tempo.glb

Face limit: **25,000**

```text
Electric safa tempo three-wheeler from Kathmandu, green and white, passengers bench in back, single vehicle. Nepal, early 2000s, realistic materials, game-ready prop, no background
```

### firefly_jar.glb

Face limit: **8,000**

```text
Glass jar with glowing fireflies (junkiri) inside, cloth lid tied with string, single object. Nepal, early 2000s, realistic materials, game-ready prop, no background
```

### river_rocks.glb

Face limit: **15,000**

```text
Cluster of large smooth river boulders from a Himalayan stream with moss, single object, no water. Nepal, early 2000s, realistic materials, game-ready prop, no background
```
