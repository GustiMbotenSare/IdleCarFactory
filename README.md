# Idle Car Factory - Core Logic

This repo holds the pure C# gameplay logic for an idle car factory game made in Unity.

There are no assets, no UI, no scenes in here. Just the brains of the game: how production works, how cars get built and sold, how the economy scales, and how progress is saved. The idea is that the logic lives here and stays stable, while the Unity side (visuals, screens, audio) gets built on top of it.

## How the folders are laid out

```
Assets/_Project/Scripts/
  Core/         Bootstrap, the Facade API, save system, and the in-code content builder
  Data/         Definitions and config: items, stations, vehicles, economy, gacha
  Events/       The event bus that keeps logic separate from the UI
  Simulation/   The tick systems: production, assembly, auction, showroom, racing
  State/        Serializable save data: inventory, wallet, stations, contracts
  Platform/     Stubs for CrazyGames ads and WebGL save flushing (wire these up in Unity)
```

## The core game loop

1. Extract raw materials (6 types).
2. Craft tiered parts (4 categories, 3 tiers each).
3. Assemble graded cars (12 vehicles, 6 gacha grades).
4. Sell them through the Showroom, the Auction, or Contracts.
5. Earn cash, upgrade the factory, unlock higher tiers, and repeat.

## The main systems

| System | File | What it does |
|--------|------|--------------|
| Production | `ProductionSystem.cs` | Ticks the extractor and manufacturing stations |
| Assembly | `AssemblySystem.cs` | Builds cars from parts and rolls the gacha grade |
| Auction | `AuctionSystem.cs` | Runs the 30 second bidding loop and pays out |
| Showroom | `ShowroomSystem.cs` | Display slots, walk-in visitors, and offers |
| Contracts | `ContractSystem.cs` | Generates and fulfills bulk, rush, and VIP orders |
| Racing | `CircuitRaceSystem.cs` | Circuit races based on car stats (no real physics) |
| Offline | `OfflineService.cs` | Fast-forwards the pipeline for offline earnings |
| Economy | `Economy.cs` | Every cost and scaling formula |
| Gacha | `GachaRoller.cs` | Grade probability, from D up to S+ |

## How the pieces fit together

- `DefaultContent.cs` builds the whole `GameConfig` in code, so you do not need any .asset files to run it.
- `GameRoot.cs` boots every system and ticks them each frame.
- `GameFacade.cs` is the one and only API the UI talks to. The UI calls the Facade, reads state, and listens for events. It never changes state on its own.
- `SaveSystem.cs` saves and loads JSON through PlayerPrefs, with a version check so old saves do not break the game.
- `SimSmokeTest.cs` runs the full pipeline with no UI, just to prove the logic works on its own.

## Production model (v2)

6 raw materials: Steel, Aluminum, Rubber, Copper, Silicon, Carbon.

4 part categories, 3 tiers each:

- Engine: V4, then V6, then V8
- Chassis: Steel, then Aluminum, then Carbon
- Wheels: Standard, then Performance, then Hyper
- Wiring: Standard, then Advanced, then Premium

12 vehicles across 4 factory tiers, plus 3 dedicated race cars.

## What the Unity developer needs to know

- The logic is UI-agnostic. Build your screens against `GameFacade` and subscribe to the channels in `Events/`.
- Item and station icons are intentionally left blank in `DefaultContent.cs`. Assign real sprites on the Unity side.
- The files in `Platform/` (`CrazyAds.cs` and `WebGlSync.cs`) are stubs. They compile and run as no-ops so the game works in the editor. Replace their bodies with the real CrazyGames SDK and WebGL save calls when you set up the build.
- There is a casual design doc written in Indonesian at `docs/GDD-ID.md` that explains the game and who owns what.

## Requirements

- Unity 2022.3 LTS or newer
- No external packages needed for the core logic
