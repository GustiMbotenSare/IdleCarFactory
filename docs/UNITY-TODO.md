# Unity Implementation Direction and To-Do List

This document is the working brief for the Unity developer. It assumes the game logic in `Assets/_Project/Scripts/` is complete and stable. Your job is to build the presentation layer (scenes, UI, art, audio, input, and the WebGL build) on top of that logic.

Read `docs/GDD.md` first for the full design and exact numbers. Read `README.md` for the architecture summary. This document focuses on what to build and in what order.

Ground rules:

- The UI talks to the game only through `GameFacade`. Call Facade methods for every player action, read `GameFacade.State` and `GameFacade.Config` for display, and subscribe to `GameEventBus` channels for updates. Do not modify anything under `Simulation`, `State`, or `Data`.
- Do not change gameplay numbers or rules. If something feels off, raise it with the design owner rather than editing the logic.
- Keep the presentation reactive. Redraw on events, not by polling every frame where avoidable.

## Milestone 0: Project setup

- [ ] Confirm the project opens in Unity 2022.3 LTS or newer with no compile errors.
- [ ] Confirm the assembly definition references resolve (UnityEngine.UI and the Input System).
- [ ] Set the build target to WebGL and confirm a clean empty build completes.
- [ ] Create a main scene and add a single `GameRoot` object that owns the game lifecycle.
- [ ] Decide on a canvas and UI scaling strategy suited to CrazyGames (landscape, scalable to common web resolutions).

## Milestone 1: Bootstrap and core wiring

- [ ] Wire `GameRoot` so it boots the systems, loads any existing save, applies offline progress, and begins ticking. Confirm it autosaves every 30 seconds.
- [ ] Create the `GameEventBus` asset and the individual event channel assets, and assign them on `GameRoot` so events flow to the UI.
- [ ] Build a small UI service or controller layer that subscribes to the bus and dispatches refreshes to the relevant screens.
- [ ] Use `NumberFormat` for all currency and large-number display so formatting is consistent. Respect the short and full notation toggle it exposes.
- [ ] Verify the run-once flow end to end with no UI by checking `SimSmokeTest` output.

## Milestone 2: Factory and production screen

- [ ] List all stations grouped by category (Extractor, Manufacturing, Assembly, Sales) using `GameFacade.Config`.
- [ ] For each station show its state from `GameFacade.State`: locked or unlocked, automated or manual, speed level, capacity level, and current progress.
- [ ] Implement manual tap via `TapStation`. Show that extractors can always be tapped, and other stations only when not yet automated.
- [ ] Implement Unlock (`UnlockStation`), Buy Automation (`BuyAutomation`), Upgrade Speed (`UpgradeSpeed`), and Upgrade Capacity (`UpgradeCapacity`). Display costs from `SpeedUpgradeCost` and `CapacityUpgradeCost` and the relevant config fields, and respect the level caps.
- [ ] Implement vehicle selection via `SelectVehicle` and a manual build button via `BuildSelectedOnce`.
- [ ] Show the current factory tier and a Buy Next Tier action via `NextFactoryTier` and `BuyNextFactoryTier`.
- [ ] Animate or visibly indicate production cycles completing so the factory feels alive.

## Milestone 3: Assembly result and gacha presentation

- [ ] Subscribe to the car-assembled event and present the rolled grade (D through S+) with appropriate emphasis for rare grades (S and S+).
- [ ] Surface the milestone signal for rare assemblies so the mascot or a celebration effect can react.
- [ ] Show the Quality Control purchase via `BuyQualityControl` and reflect that it improves grade odds once owned.

## Milestone 4: Selling (showroom, auction, contracts)

Showroom:

- [ ] Render the display slots and their state, including locked slots and the auto-fill behavior for newly assembled cars.
- [ ] Implement display, clear, sale mode, asking price, auto-accept, accept offer, decline offer, and send to auction using the matching `GameFacade` methods.
- [ ] Implement the showroom upgrades (slot unlock, level, marketing, decor) and show their costs from the Facade cost methods.
- [ ] Visualize incoming visitor offers and their expiry timer.

Auction:

- [ ] Build the single auction lane UI driven by the auction state: current bid, time remaining, and bid activity.
- [ ] Implement Start Auction, Instant Sell, and the optional buyout.
- [ ] React to the car-sold event for both auction and showroom sales, including the success toast.

Contracts:

- [ ] List active contracts with type, target vehicle, quantity, rewards, and any countdown for timed (Rush) contracts.
- [ ] Implement Fulfill via `FulfillContract` and refresh the list on the contracts-refreshed event.

## Milestone 5: Racing

Design direction: model the race presentation on Kairosoft titles such as Grand Prix Story. Use a simple, readable, stat-driven race view rather than a physics simulation. A clean side-on or top-down track with car markers advancing along it fits the underlying logic, which is purely distance-and-stat based. Keep it charming and lightweight, with clear lap counters, position order, and moments of drama (boosts and drafting) surfaced as small popups or icons. The race continues in the background, so the screen should be able to show a live race the player returned to mid-event.

- [ ] Implement the Race Agency unlock via `BuyRaceAgency` and gate the race screen on `IsRaceUnlocked`.
- [ ] Build the car picker. It must list both dedicated race cars and the player's owned production cars, since both are valid entries. Production cars race with their grade multiplier applied; dedicated race cars use fixed stats.
- [ ] Build the dedicated race-car workshop UI using `CanBuildRaceCar` and `BuildRaceCar`.
- [ ] Implement track selection and Start Race via `StartRace`, showing entry fees and the min-tier requirement per track.
- [ ] Render the live race from the race state each frame: entrants, distance, lap, and current placement. Surface the aura boost and slipstream moments in a Kairosoft-style readable way.
- [ ] Implement Skip To Result (`SkipRaceToResult`) and Dismiss Result (`DismissRaceResult`).
- [ ] React to the race-finished event to present placement and rewards (cash, trophies, prestige), and respect the post-race cooldown.
- [ ] Build the trophy upgrades screen and purchase flow via `BuyTrophyUpgrade`, showing trophy costs and effects.

## Milestone 6: Offline progress

- [ ] On load, if an offline report is produced, show a welcome-back summary (time credited, raw produced, components produced, cars assembled). Only show it when the player was away long enough to matter.

## Milestone 7: Feedback, mascot, and tutorial

- [ ] Implement a toast notification system driven by the toast event channel, styled by kind (info, success, error).
- [ ] Route the error channel to clear, friendly messages.
- [ ] Implement the mascot tutorial flow for first-time players and have the mascot react to milestone signals.
- [ ] Add SFX for key actions (tap, build, sale, auction win, race finish) and basic background audio.

## Milestone 8: Platform integration (CrazyGames and WebGL)

- [ ] Implement `Platform/CrazyAds.cs`. Replace the stub bodies with the CrazyGames SDK (v3): initialize on `Init`, signal gameplay start and stop, and implement rewarded and interstitial ads. Decide and confirm with the design owner where rewarded ads grant a benefit.
- [ ] Place a `CrazyAds` component on the `GameRoot` object and assign it so lifecycle calls reach the SDK.
- [ ] Implement `Platform/WebGlSync.cs`. Replace the stub with a JavaScript interop call that flushes the WebGL filesystem so PlayerPrefs persist across refresh and tab close.
- [ ] Verify saves survive a page refresh and a tab close in a WebGL build.

## Milestone 9: Art and content polish

- [ ] Assign sprites and icons for items, stations, and vehicles. Icons are intentionally blank in code, so all visuals are defined on the Unity side.
- [ ] Apply a consistent visual theme and ensure readability at common web resolutions.
- [ ] Confirm vehicle and station naming in the UI matches the definitions.

## Milestone 10: Release readiness

- [ ] Remove the in-scene Dev Menu object (CFI > Build > Dev Menu) so the developer cheat actions on `GameFacade` are not reachable in the shipped build.
- [ ] Run a full playthrough across all factory tiers and confirm the economy, selling, and racing loops all function.
- [ ] Confirm save, load, version mismatch handling, and offline progress all behave correctly.
- [ ] Produce a final WebGL build and validate it against CrazyGames submission requirements.

## Questions to route back to design

If any of the following come up, raise them rather than changing logic: unclear numbers or balance, a desire to add or remove a vehicle or station, changes to grade odds or rewards, or any new mechanic. The logic side will make the change and update `docs/GDD.md`.
