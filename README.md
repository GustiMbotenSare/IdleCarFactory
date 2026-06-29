# Idle Car Factory — Core Logic

Pure C# gameplay systems for an idle car factory game built in Unity.  
**No assets, no UI, no scenes** — just the core game mechanics.

## Architecture

```
Assets/_Project/Scripts/
├── Core/           — Bootstrap, Facade API, Save System, Content Builder
├── Data/           — Definitions: items, stations, vehicles, economy, gacha
├── Events/         — Event bus (decouples logic from presentation)
├── Simulation/     — Tick systems: production, assembly, auction, showroom, race
└── State/          — Serializable save data: inventory, wallet, stations, contracts
```

## Game Loop

```
Extract raw materials (6 types)
    → Craft tiered parts (4 categories × 3 tiers)
        → Assemble graded cars (12 vehicles × 6 grades)
            → Sell via Showroom / Auction / Contracts
                → Earn cash → Upgrade factory → Unlock higher tiers
```

## Key Systems

| System | File | Description |
|--------|------|-------------|
| Production | `ProductionSystem.cs` | Ticks extractors + manufacturing stations |
| Assembly | `AssemblySystem.cs` | Builds cars from parts, rolls gacha grade |
| Auction | `AuctionSystem.cs` | Bid escalation + settlement |
| Showroom | `ShowroomSystem.cs` | Display slots, walk-in visitors, offers |
| Contracts | `ContractSystem.cs` | Generate & fulfill bulk/rush/VIP orders |
| Racing | `CircuitRaceSystem.cs` | Circuit races with traction/speed simulation |
| Offline | `OfflineService.cs` | Fast-forward pipeline for offline progress |
| Economy | `Economy.cs` | All cost/scaling formulas |
| Gacha | `GachaRoller.cs` | Grade probability (D → S+) |

## Data Flow

- **`DefaultContent.cs`** builds the entire `GameConfig` in code (no .asset files needed)
- **`GameRoot.cs`** bootstraps all systems and ticks them each frame
- **`GameFacade.cs`** is the single API surface — UI calls this, never mutates state directly
- **`SaveSystem.cs`** handles JSON save/load via PlayerPrefs with version guarding
- **`SimSmokeTest.cs`** proves the full pipeline works headless (no UI required)

## Production Model (v2)

**6 Raw Materials:** Steel, Aluminum, Rubber, Copper, Silicon, Carbon  
**4 Part Categories × 3 Tiers:**
- Engine: V4 → V6 → V8
- Chassis: Steel → Aluminum → Carbon
- Wheels: Standard → Performance → Hyper
- Wiring: Standard → Advanced → Premium

**12 Vehicles across 4 tiers** + 3 dedicated race cars.

## Requirements

- Unity 2022.3+ (LTS)
- No external packages required for core logic
