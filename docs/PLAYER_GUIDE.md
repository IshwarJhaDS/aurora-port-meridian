# Aurora: Port Meridian — playing guide

Phase 2 turns the engine laboratory into a playable neighborhood: city streaming, walking, driving, citizens, conversation, jobs, and seven mission arcs. The visuals are stylized procedural geometry, and the simulation has a fixed, authored scope.

## Open the new chapter

1. Download and extract **Aurora_Phase2_Port_Meridian.zip**.
2. Open a terminal in the extracted **aurora-city** folder.
3. Run `npm run dev`. No `npm install` is needed. Node.js 20+ must already be installed.
4. Open **http://127.0.0.1:3001/**. Keep that terminal open.
5. Select **Enter the city**. Press **E** beside Mira, who starts three meters ahead of you.

If using a terminal from another directory, first type `cd `, drag the extracted **aurora-city** folder into the terminal, and press Enter. Then run `npm run dev`.

The older engine chapter can keep running on port 3000. This city starts on port 3001. If that port is occupied, stop its existing server or run `npm run dev -- --port 3002` and use the printed address. Saved visits belong to the browser’s exact address, including its port.

## Your first visit

Follow **A place to begin**: meet Mira, walk to Harbor Coffee, speak with Leo, then return to Mira. The HUD shows the next step, distance, and route. You receive $120 and four reputation after the final conversation.

Press **M** to see named places. Select a place to make it your waypoint. You can walk, drive, or board the city shuttle while on foot within 16 meters of a named stop. A trip costs $10 of game currency and advances city time eight minutes. It leaves your car where it was; **R** brings the car to a road near you.

Press **J** to start or track another story. Active stories can progress in parallel. A custom map waypoint temporarily takes priority over the tracked story; use **Clear waypoint** to show the story’s route again. A nearby person shows an **E** prompt. In a conversation, click a reply or press its number. The text box understands topics such as “Do you have any jobs?”, “Where is the café?”, “How is the weather?”, and “What happened at the harbor?” Replies come from the game’s authored conversation system.

## People and places

| Person | Where | Connection |
| --- | --- | --- |
| Mira Patel | Founders Square | Arrival and neighborhood stories |
| Leo Santos | Harbor Coffee | Café work and conversation |
| Dr. Anika Shah | Meridian Clinic | Clinic deliveries |
| Tess Morgan | Eastside Dispatch | Courier work and missing shipment |
| Sanjay Rai | Harbor Workshop | Tools, lighting repairs, shipment clues |
| Rosa Bennett | Westhaven Market | Neighborhood voices |
| Eli Brooks | Cedar Gardens | Neighborhood voices |
| Juno Park | Meridian Promenade | Neighborhood voices and photography |

Named contacts remain accessible around their locations. Other residents commute between homes, work, and leisure destinations; some circulate around the square. Rain changes their shelter behavior. Work is represented by street-side activity points and animations rather than enterable workplaces or a simulated economy.

## Missions and work

| Mission | What you do | Reward |
| --- | --- | --- |
| A place to begin | Mira → café → Leo → Mira | $120 / 4 reputation |
| The neighborhood route | Collect at dispatch; deliver to the assigned recipient | $95 / 2 reputation |
| The afternoon rush | Brew, pack, serve three café orders | $85 / 2 reputation |
| Neighborhood voices | Speak with Rosa, Eli, Juno; return to Mira | $150 / 4 reputation |
| Keep the lights on | Collect tools, repair three lamps, return to Sanjay | $180 / 5 reputation |
| The missing manifest | Tess → Warehouse 08 → Sanjay → Tess | $250 / 6 reputation |
| The city from above | Reach Cedar Hill Lookout and press V | $80 / 1 reputation |

The missing-manifest story unlocks at four reputation. Courier and café work can be repeated. Courier destinations rotate between the clinic, market, and harbor. The chosen route survives a saved reload.

For a café shift, speak to Leo or start it from the journal while the café is open, **07:00–22:00**. Stay by Harbor Coffee and use the **Brew → Pack → Serve** buttons for each of three orders. Brewing takes 2.6 simulation seconds; packing and serving take 1.4 each. The shift allows 150 simulation seconds. Walking away prevents remote work; an expired shift can be restarted. Menus and conversations pause the shift timer.

For lighting repairs, collect the tool kit first. Follow each objective to the correct lamp, stand close, and **hold E for 2.2 seconds**. Releasing E or moving away cancels the hold. Repairs stay saved, and those lamps illuminate at night.

For the lookout postcard, **V** opens framing mode and completes the framing objective when you are at the lookout. Drag to orbit, scroll to widen the shot, then select **Capture postcard** to download the scene as a PNG. V or Escape closes framing mode.

## Walking and driving

Walk with **WASD**, sprint with **Shift**, slow walk with **C**, and jump with **Space**. Drag the scene to turn the camera; movement follows its heading. You start beside Mira; your teal Aster GT begins on the square’s west side. E enters it when close and slow. E exits after you slow down and a side has room.

Inside the car, W accelerates, S brakes into reverse, A/D steer, B applies the service brake, and Space applies the handbrake. The player car uses four suspension rays and tire forces from the engine. Wet roads reduce grip. Traffic follows lanes, signals, and obstacle braking; it is a lighter kinematic simulation, not a fleet of fully simulated rigid cars.

R recovers your car. If you disturb street props with X or a vehicle impact, nearby residents flee. Bins and crates break, and the glass pane beside the square fractures into shards. Damage persists; temporary debris expires to keep the world bounded.

## Save and graphics

The game autosaves every ten simulation seconds and at mission progress changes. You can also choose **Save visit** in the menu. Progress is local to this browser and exact address. A blocked or full storage area produces **SESSION ONLY**. There is no account sync. The menu’s **Start a new visit** option asks before clearing this chapter’s progress.

**Performance**, **Balanced**, and **Cinematic** adjust resolution and screen-space effects. Start with Balanced; switch to Performance if needed. WebGL 2 and floating-point color buffers are required for the 3D renderer. A browser missing those capabilities receives **MAP VIEW**, which remains playable with the same missions and save system. This view is explicitly labeled and is not the 3D scene.

The city is roughly one kilometer across, with a bounded coastline and generated districts. It has 80 resident records, 32 traffic cars, one drivable player car, eight named contacts, and seven mission arcs. It has no enterable building interiors, unrestricted AI chat, combat/wanted system, multiplayer, or photorealistic asset set. These are not silently presented as completed features.

The included source, architecture, and 71 tests provide the foundation for continuing the project. The 3D scenes were rendered with the actual GLSL in native ES; full browser 3D gameplay and device performance still need checking on a WebGL-capable machine.
