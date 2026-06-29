# Car Factory Idle - Game Design Document

This document describes the current design of Car Factory Idle as implemented in this repository. It reflects the logic that actually ships in `Assets/_Project/Scripts/`, so it should be treated as the source of truth for gameplay rules, numbers, and systems. The repository contains the game logic only. Presentation (UI, art, audio, scenes) is built in Unity on top of this logic.

Ownership split:

- The game logic, structure, and core mechanics are owned and maintained on the design side and live in this repository as pure C#.
- The Unity implementation (screens, sprites, audio, input, build) is owned by the Unity developer and is built against the public API described under "Architecture".

Target platform: WebGL, distributed through CrazyGames. Engine: Unity 2022.3 LTS or newer.

## 1. Core loop

1. Extract raw materials from extractor stations.
2. Refine raw materials into components at manufacturing stations.
3. Assemble components into finished vehicles. Each finished vehicle receives a random quality grade (gacha).
4. Sell vehicles through the Showroom, the Auction, or Contracts.
5. Spend the earnings on upgrades and new factory tiers, which raise output and unlock higher-tier content. Repeat at a larger scale.

A separate progression track, Racing, opens once the player buys the Race Agency. Racing spends cash on entry fees and pays out cash, trophies, and prestige.

## 2. Items

There are 18 item definitions: 6 raw materials and 12 components.

Raw materials (by tier):

- Tier 1: Steel, Rubber, Copper
- Tier 2: Aluminum, Silicon
- Tier 3: Carbon

Components (4 categories, 3 tiers each):

- Engine: V4, V6, V8
- Chassis: Steel, Aluminum, Carbon
- Wheels: Standard, Performance, Hyper
- Wiring: Standard, Advanced, Premium

Item icons are intentionally left blank in code. The Unity developer assigns sprites.

## 3. Stations

Stations come in four categories: Extractor, Manufacturing, Assembly, and Sales. There are 6 extractors, 12 manufacturing stations, 4 assembly stations, and 1 sales office.

Extractors (base output / base interval / unlock notes):

| Station | Output | Interval | Unlock |
|---------|--------|----------|--------|
| Steel Works | 7 | 2.0s | Available from start |
| Rubber Plantation | 3 | 2.0s | Available from start |
| Copper Mine | 4 | 2.5s | Available from start |
| Aluminum Mine | 7 | 2.5s | Costs 8,000 |
| Silicon Quarry | 3 | 2.5s | Costs 8,000 |
| Carbon Fiber Lab | 6 | 3.0s | Costs 80,000 |

Manufacturing recipes (inputs consumed per cycle):

| Component | Recipe |
|-----------|--------|
| Engine V4 | 5 Steel |
| Engine V6 | 3 Steel, 3 Aluminum |
| Engine V8 | 4 Steel, 3 Aluminum, 2 Carbon |
| Chassis (Steel) | 5 Steel |
| Chassis (Aluminum) | 5 Aluminum |
| Chassis (Carbon) | 4 Carbon |
| Wheel (Standard) | 3 Rubber |
| Wheel (Performance) | 3 Rubber, 2 Aluminum |
| Wheel (Hyper) | 4 Rubber, 2 Carbon |
| Wiring (Standard) | 4 Copper |
| Wiring (Advanced) | 4 Copper, 3 Silicon |
| Wiring (Premium) | 8 Copper, 6 Silicon |

Assembly stations build the currently selected vehicle. There is one assembly station per factory tier (tiers 1 through 4); a station only builds a vehicle whose tier matches. The Sales office supports the selling flow.

Each station can be unlocked, automated, and then upgraded for speed and capacity (see Economy).

## 4. Vehicles

There are 12 sellable vehicles across 4 tiers. Names are deliberately generic to avoid trademark issues. Race stats are listed as top speed / acceleration / traction / launch.

| Tier | Vehicle | Base price | Race stats |
|------|---------|------------|------------|
| 1 | Tokyo Commuter | 800 | 10 / 8 / 8 / 8 |
| 1 | Tokyo Trekker | 1,200 | 12 / 9 / 10 / 8 |
| 2 | Hiroshima Breeze | 3,500 | 24 / 22 / 20 / 18 |
| 2 | Bavarian Series 3 | 4,500 | 20 / 18 / 16 / 16 |
| 2 | Britannia Rover | 6,000 | 22 / 16 / 18 / 16 |
| 3 | Stuttgart S-Class | 18,000 | 30 / 26 / 24 / 24 |
| 3 | Stuttgart G-Box | 25,000 | 28 / 24 / 26 / 22 |
| 3 | Autobahn 911 | 30,000 | 40 / 38 / 34 / 34 |
| 3 | Milano Toro | 50,000 | 55 / 52 / 48 / 46 |
| 4 | Maranello Rosso | 120,000 | 52 / 50 / 46 / 44 |
| 4 | Molsheim Royale | 350,000 | 70 / 66 / 60 / 58 |
| 4 | Angelholm Apex | 800,000 | 80 / 78 / 70 / 68 |

A vehicle recipe is one engine, one chassis, four wheels, and one wiring set, with the specific component tiers and quantities varying by vehicle.

## 5. Quality grades (gacha)

When a vehicle is assembled, it rolls a quality grade. The grade sets a price multiplier on the vehicle base price.

| Grade | Base weight | Multiplier |
|-------|-------------|------------|
| D | 15 | 0.8x |
| C | 50 | 1.0x |
| B | 20 | 1.2x |
| A | 10 | 1.5x |
| S | 4 | 2.5x |
| S+ | 1 | 5.0x |

Quality Control is a one-time purchase (cost 25,000) that improves the odds. After it is owned, the weights become D 10, C 45, B 20, A 15, S 7, S+ 3.

Finished cars are stored in inventory under a key of the form `vehicleId_grade` (for example `autobahn911_A`).

## 6. Factory tiers

The factory tier applies a global production multiplier and gates assembly.

| Tier | Name | Cost | Production multiplier |
|------|------|------|-----------------------|
| 1 | Garage Workshop | 0 | 1x |
| 2 | Small Factory | 10,000 | 2x |
| 3 | Industrial Plant | 100,000 | 5x |
| 4 | Mega Factory | 1,000,000 | 15x |

## 7. Selling

### Showroom

The player displays finished cars in unlocked slots (four slots by default, the first unlocked). A displayed car is removed from inventory immediately and only pays out at the point of sale, so a car can never be sold twice. The newest assembled car auto-fills the first open slot. Walk-in visitors arrive on a timer and make offers of roughly 75 to 95 percent of the asking price, improved by the decor level. Offers expire after a few seconds. Visitor frequency improves with the showroom level and the marketing level. An optional auto-accept threshold sells automatically when an offer is high enough.

### Auction

There is a single shared auction lane. An auction runs for 30 seconds. NPC bids arrive every 3 to 7 seconds and raise the price by 5 to 20 percent compounding. The highest bid is accepted automatically when the timer ends. An optional buyout closes the auction early once a bid reaches it. Instant Sell pays the graded base price immediately.

### Contracts

There are five contract archetypes: Standard, Bulk, Rush, Premium, and VIP. The system keeps up to five contracts in the pool and allows up to three active at once, refreshing every 120 seconds. Quantities, payouts, timers, and trophy rewards scale with the current factory tier. Contracts are fulfilled using owned cars of the required vehicle, of any grade.

## 8. Racing

Racing unlocks by purchasing the Race Agency for 75,000.

The live game uses circuit races. A circuit race is an arcade-style auto-racer with no physics: each car is a single distance value advanced per tick based on its stats. Straights run up to top speed, corners cap speed based on handling versus corner sharpness, an aura effect (driven by the launch stat) randomly grants a temporary boost, and drafting gives a catch-up bonus so the field stays close. A race runs in the background and continues even if the player leaves the race screen. The player races against three AI rivals whose strength is scaled per track. The entered car is not consumed.

Eligible cars. The player can enter either:

- A dedicated race car, which uses fixed engineered stats with no grade roll, or
- Any owned production car, which races using its base race stats scaled by its quality-grade multiplier (so a higher-grade car of the same model performs better).

This means normal production cars and dedicated race cars are both valid race entries.

Tracks:

| Track | Min tier | Laps | Entry fee | Win reward | AI strength |
|-------|----------|------|-----------|------------|-------------|
| Local | 1 | 2 | 500 | 4,000 | 0.80 |
| National | 2 | 3 | 4,000 | 22,000 | 0.95 |
| International | 3 | 3 | 20,000 | 120,000 | 1.08 |

Rewards are paid by finishing place: first place pays the full cash reward and trophies and the prestige reward, second place pays half cash and half trophies, third place pays a quarter of the cash, and lower places pay a small fraction of the cash. A short cooldown applies after each race.

Dedicated race cars (built on the race workshop line, fixed stats, stored separately from showroom stock):

| Race car | Min tier | Build cost | Race stats | Parts |
|----------|----------|------------|------------|-------|
| Vulcan GT | 2 | 8,000 | 60 / 58 / 52 / 40 | 2 Engine V6, 2 Chassis Aluminum, 2 Wheel Performance, 2 Wiring Advanced |
| Thunderbolt R | 3 | 40,000 | 85 / 82 / 72 / 58 | Top-tier parts |
| Hypernova X | 4 | 150,000 | 110 / 105 / 92 / 80 | Top-tier parts |

Trophies earned from racing are spent on trophy upgrades: Nitro Kit (5), Turbo Tuning (10), Racing Tires (8), Chrome Finish (3), and Sponsor Deal (15).

## 9. Offline progress

When the player returns after being away, the pipeline is fast-forwarded. Offline time is capped at 8 hours and runs at 25 percent efficiency. Extractors are capped at 200 of each raw material produced offline. Manufacturing and assembly run if their inputs allow, and assembly still rolls quality grades offline. An offline summary is shown when the player has been away at least 60 seconds.

## 10. Economy formulas

Station upgrades:

- Speed: maximum level 25, plus 10 percent speed per level. Cost is floor(100 * 1.15^currentLevel).
- Capacity: maximum level 10, plus 1 output per cycle per level. Cost is floor(500 * 1.40^currentLevel).
- Effective interval is baseInterval / ((1 + 0.10 * speedLevel) * factoryMultiplier).
- Effective output is baseOutput + capacityLevel.

Showroom upgrades:

- Maximum showroom level 10, maximum marketing and decor level 20.
- Slot unlock cost is floor(50,000 * 3^(slotsUnlocked - 1)).
- Level cost is floor(15,000 * 1.7^(level - 1)).
- Marketing cost is floor(4,000 * 1.6^level).
- Decor cost is floor(5,000 * 1.6^level).

Other: Quality Control costs 25,000. The Race Agency costs 75,000.

## 11. Saving

The game saves to PlayerPrefs as JSON under the key `cfi_save_v2`, at save version 2. A legacy key from an older version is wiped on load. If a save is corrupt or its version does not match, it is discarded and a new game starts, so a bad save never blocks play. On WebGL the save is flushed to persistent storage after each write.

## 12. Architecture

The code is organized by namespace under `CarFactoryIdle`:

- Core: bootstrap (`GameRoot`), the public API (`GameFacade`), saving (`SaveSystem`), the in-code content builder (`DefaultContent`), number formatting, and a headless smoke test.
- Data: definitions and tuning (items, stations, vehicles, factory tiers, contract types, gacha table, economy formulas).
- Events: the event bus that decouples logic from presentation.
- Simulation: the tick systems (production, assembly, auction, showroom, contracts, racing, offline).
- State: serializable save data.
- Platform: stubs for the CrazyGames ads bridge and the WebGL save flush, to be implemented on the Unity side.

Key rules for the Unity side:

- `GameFacade` is the single API surface. The UI calls the Facade for every player action, reads state for display, and subscribes to the event bus for updates. The UI must not modify simulation, state, or data directly.
- `GameRoot` boots every system and ticks them each frame, in the order production, assembly, auction, showroom, contracts, and racing. It autosaves every 30 seconds.
- `DefaultContent` builds the entire content database in code, so the game runs without any authored .asset files.
- The files in `Platform/` compile as no-ops so the game runs in the editor. Replace their bodies with the real CrazyGames SDK calls and WebGL save flush when setting up the build.

## 13. Handoff checklist for Unity

1. Create the main scene and add a `GameRoot` object.
2. Build the UI screens (factory, showroom, auction, contracts, race) against `GameFacade`.
3. Assign sprites and icons for items, stations, and vehicles.
4. Implement the `Platform/` stubs (CrazyGames ads and WebGL save flush).
5. Add audio, toast notifications, and the tutorial flow, driven by the existing event bus.
6. Use `SimSmokeTest` to confirm the logic runs end to end before wiring the UI.
