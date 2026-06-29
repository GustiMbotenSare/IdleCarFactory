using CarFactoryIdle.Data;
using CarFactoryIdle.Events;
using CarFactoryIdle.State;

namespace CarFactoryIdle.Simulation
{
    /// <summary>The dedicated race-car production line. Unlike the gacha assembly line, race cars are
    /// engineered to FIXED (no-RNG) race stats: you spend top-tier parts + cash and get exactly the
    /// car on the spec sheet \u2014 no grade roll. Gated behind the Race Agency and a matching factory
    /// tier. Built on demand (no timer) and stored in inventory under their raw id (no "_grade"
    /// suffix) so they stay separate from showroom stock and only appear in the Race screen.
    ///
    /// Reuses VehicleDefinition: `recipe` = parts cost, `basePrice` = cash build cost, `tier` = the
    /// minimum factory tier (1-based) required to build.</summary>
    public class RaceWorkshopSystem
    {
        private readonly GameConfig _cfg;
        private readonly GameState _state;
        private readonly GameEventBus _bus;

        public RaceWorkshopSystem(GameConfig cfg, GameState state, GameEventBus bus)
        { _cfg = cfg; _state = state; _bus = bus; }

        public bool Unlocked => _state.ownedSpecialUnlocks.Contains(CircuitRaceSystem.RaceAgencyId);

        /// <summary>True if the current factory tier is high enough to build this race car.</summary>
        public bool MeetsTier(VehicleDefinition rc) => rc != null && (_state.factoryTierIndex + 1) >= rc.tier;

        public bool CanBuild(string raceCarId)
        {
            var rc = _cfg.GetRaceCar(raceCarId);
            if (rc == null || !Unlocked || !MeetsTier(rc)) return false;
            if (!_state.inventory.CanAfford(rc.recipe)) return false;
            if (rc.basePrice > 0 && _state.wallet.cash < rc.basePrice) return false;
            return true;
        }

        /// <summary>Builds one race car. Checks EVERYTHING before charging (no half-spent builds, and
        /// no "wait for a timer just to be told you can't afford it"). Returns false if blocked.</summary>
        public bool BuildRaceCar(string raceCarId)
        {
            var rc = _cfg.GetRaceCar(raceCarId);
            if (rc == null) return false;
            if (!Unlocked || !MeetsTier(rc)) return false;
            if (!_state.inventory.CanAfford(rc.recipe)) return false;
            if (rc.basePrice > 0 && _state.wallet.cash < rc.basePrice) return false;

            if (!_state.inventory.TrySpend(rc.recipe)) return false;   // atomic part spend
            if (rc.basePrice > 0)
            {
                _state.wallet.SpendCash(rc.basePrice);
                _bus?.CashChanged(_state.wallet.cash);
            }
            _state.inventory.Add(rc.id, 1);
            _bus?.Milestone("race_car_built_" + rc.id);
            return true;
        }
    }
}
