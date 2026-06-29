using System;
using System.Collections.Generic;

namespace CarFactoryIdle.State
{
    /// <summary>The entire saved game. Pure data, no UnityEngine.Object references, so it can be
    /// JSON-serialized and fast-forwarded headless for offline progress.</summary>
    [Serializable]
    public class GameState
    {
        public Inventory inventory = new();
        public Wallet wallet = new();
        public List<StationState> stations = new();

        public int factoryTierIndex = 0;                 // 0 -> Tier 1
        public string selectedVehicleId = "tokyoCommuter";

        public List<ContractState> contracts = new();
        public float contractRefreshTimer = 0f;

        public AuctionState auction = new();
        public ShowroomState showroom = new();
        public string lastAssembledCarKey;   // newest built car, for showroom auto-display

        public bool qualityControlOwned;
        public List<string> ownedTrophyUpgrades = new();
        public List<string> ownedSpecialUnlocks = new();  // conveyor, robotic arms, research lab

        public float raceCooldownRemaining = 0f;   // legacy drag-race cooldown (unused by circuit races)
        public int racesCompleted = 0;
        public int prestigePoints = 0;              // earned from race wins; spend on sponsors/unlocks later
        public RaceState raceLive = new();          // the single live circuit race (ticks in background)

        public long lastSaveUnixSeconds;
        public int saveVersion = 2;

        public StationState GetStation(string definitionId)
        {
            for (int i = 0; i < stations.Count; i++)
                if (stations[i].definitionId == definitionId) return stations[i];
            return null;
        }
    }
}
