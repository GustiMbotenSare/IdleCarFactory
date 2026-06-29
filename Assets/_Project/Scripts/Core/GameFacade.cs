using System;
using System.Collections.Generic;
using CarFactoryIdle.Data;
using CarFactoryIdle.Events;
using CarFactoryIdle.Simulation;
using CarFactoryIdle.State;

namespace CarFactoryIdle.Core
{
    /// <summary>The single, stable API surface the UI calls. Every player intent (tap, buy, upgrade,
    /// sell, fulfill) goes through here so the presentation layer never reaches into systems or
    /// mutates state directly. UI views should ONLY call this class, read state, and subscribe to
    /// GameEventBus; they must not modify anything under Simulation/State/Data.
    ///
    /// All purchase methods are atomic: they check affordability first and return false (raising the
    /// error channel) if the player can't pay, so no half-applied transactions.</summary>
    public class GameFacade
    {
        private readonly GameConfig _cfg;
        private readonly GameState _state;
        private readonly GameEventBus _bus;
        private readonly ProductionSystem _production;
        private readonly AssemblySystem _assembly;
        private readonly AuctionSystem _auction;
        private readonly ContractSystem _contracts;
        private readonly ShowroomSystem _showroom;
        private readonly CircuitRaceSystem _circuit;
        private readonly RaceWorkshopSystem _workshop;

        public GameFacade(GameConfig cfg, GameState state, GameEventBus bus,
            ProductionSystem production, AssemblySystem assembly,
            AuctionSystem auction, ContractSystem contracts, ShowroomSystem showroom,
            CircuitRaceSystem circuit, RaceWorkshopSystem workshop)
        {
            _cfg = cfg; _state = state; _bus = bus;
            _production = production; _assembly = assembly;
            _auction = auction; _contracts = contracts; _showroom = showroom;
            _circuit = circuit; _workshop = workshop;
        }

        public GameState State => _state;
        public GameConfig Config => _cfg;

        // ---------------- Production intents ----------------

        /// <summary>Manual tap (extractors always; other stations only when not automated).</summary>
        public bool TapStation(string stationId)
        {
            bool ok = _production.ManualTap(stationId);
            if (ok) Changed();
            return ok;
        }

        public bool SelectVehicle(string vehicleId)
        {
            if (_cfg.GetVehicle(vehicleId) == null) return false;
            _state.selectedVehicleId = vehicleId;
            Changed();
            return true;
        }

        /// <summary>Manually build one of the selected vehicle (ignores automation).</summary>
        public bool BuildSelectedOnce()
        {
            var v = _cfg.GetVehicle(_state.selectedVehicleId);
            if (v == null) return false;
            bool ok = _assembly.AssembleOnce(v);
            if (!ok) Error("Missing parts to build.");
            else Changed();
            return ok;
        }

        // ---------------- Station purchases ----------------

        public bool UnlockStation(string stationId)
        {
            var ss = _state.GetStation(stationId);
            var def = _cfg.GetStation(stationId);
            if (ss == null || def == null || ss.unlocked) return false;
            if (!Pay(def.unlockCost)) return false;
            ss.unlocked = true;
            Changed();
            return true;
        }

        public bool BuyAutomation(string stationId)
        {
            var ss = _state.GetStation(stationId);
            var def = _cfg.GetStation(stationId);
            if (ss == null || def == null || !ss.unlocked || ss.automated) return false;
            if (!Pay(def.automationCost)) return false;
            ss.automated = true;
            Changed();
            return true;
        }

        public long SpeedUpgradeCost(string stationId)
        {
            var ss = _state.GetStation(stationId);
            return ss == null ? 0 : Economy.SpeedUpgradeCost(ss.speedLevel);
        }

        public bool UpgradeSpeed(string stationId)
        {
            var ss = _state.GetStation(stationId);
            if (ss == null || !ss.unlocked || ss.speedLevel >= Economy.MaxSpeedLevel) return false;
            if (!Pay(Economy.SpeedUpgradeCost(ss.speedLevel))) return false;
            ss.speedLevel++;
            Changed();
            return true;
        }

        public long CapacityUpgradeCost(string stationId)
        {
            var ss = _state.GetStation(stationId);
            return ss == null ? 0 : Economy.CapacityUpgradeCost(ss.capacityLevel);
        }

        public bool UpgradeCapacity(string stationId)
        {
            var ss = _state.GetStation(stationId);
            if (ss == null || !ss.unlocked || ss.capacityLevel >= Economy.MaxCapacityLevel) return false;
            if (!Pay(Economy.CapacityUpgradeCost(ss.capacityLevel))) return false;
            ss.capacityLevel++;
            Changed();
            return true;
        }

        // ---------------- Factory / global purchases ----------------

        public FactoryTierDefinition NextFactoryTier()
            => _cfg.GetFactoryTier(_state.factoryTierIndex + 1);

        public bool BuyNextFactoryTier()
        {
            var next = NextFactoryTier();
            if (next == null) return false; // already max tier
            if (!Pay(next.cost)) return false;
            _state.factoryTierIndex++;
            _bus?.milestone?.Raise($"Upgraded to {next.displayName}!");
            Changed();
            return true;
        }

        public bool BuyQualityControl()
        {
            if (_state.qualityControlOwned) return false;
            if (!Pay(GameConfig.QualityControlCost)) return false;
            _state.qualityControlOwned = true;
            Changed();
            return true;
        }

        public bool BuyTrophyUpgrade(string upgradeId)
        {
            if (_state.ownedTrophyUpgrades.Contains(upgradeId)) return false;
            TrophyUpgradeDefinition def = null;
            foreach (var t in _cfg.trophyUpgrades) if (t != null && t.id == upgradeId) { def = t; break; }
            if (def == null) return false;
            if (!_state.wallet.SpendTrophies(def.trophyCost)) { Error("Not enough trophies!"); return false; }
            _state.ownedTrophyUpgrades.Add(upgradeId);
            _bus?.TrophiesChanged(_state.wallet.trophies);
            Changed();
            return true;
        }

        // ---------------- Selling / contracts ----------------

        public long GradedPrice(string carKey) => _auction.GradedBasePrice(carKey);
        public bool StartAuction(string carKey) { bool ok = _auction.StartAuction(carKey); if (ok) Changed(); return ok; }
        public void InstantSell() { _auction.InstantSell(); Changed(); }
        public bool FulfillContract(int index) { bool ok = _contracts.Fulfill(index); if (ok) Changed(); return ok; }

        // ---------------- Showroom ----------------

        public bool DisplayCar(int slot, string carKey) { bool ok = _showroom.DisplayCar(slot, carKey); if (ok) Changed(); return ok; }
        public bool ClearShowroomSlot(int slot) { bool ok = _showroom.ClearSlot(slot); if (ok) Changed(); return ok; }
        public bool SetShowroomSaleMode(int slot, SaleMode mode) { bool ok = _showroom.SetSaleMode(slot, mode); if (ok) Changed(); return ok; }
        public bool SetAskingPrice(int slot, long price) { bool ok = _showroom.SetAskingPrice(slot, price); if (ok) Changed(); return ok; }
        public bool SetShowroomAutoAccept(int slot, bool on, long threshold) { bool ok = _showroom.SetAutoAccept(slot, on, threshold); if (ok) Changed(); return ok; }
        public bool AcceptShowroomOffer(int slot) { bool ok = _showroom.AcceptOffer(slot); if (ok) Changed(); return ok; }
        public bool DeclineShowroomOffer(int slot) { bool ok = _showroom.DeclineOffer(slot); if (ok) Changed(); return ok; }
        public bool SendShowroomCarToAuction(int slot, long buyout) { bool ok = _showroom.SendToAuction(slot, buyout); if (ok) Changed(); return ok; }
        public bool UnlockShowroomSlot(int slot) { bool ok = _showroom.UnlockSlot(slot); if (ok) Changed(); return ok; }
        public bool UpgradeShowroomLevel() { bool ok = _showroom.UpgradeLevel(); if (ok) Changed(); return ok; }
        public bool UpgradeShowroomMarketing() { bool ok = _showroom.UpgradeMarketing(); if (ok) Changed(); return ok; }
        public bool UpgradeShowroomDecor() { bool ok = _showroom.UpgradeDecor(); if (ok) Changed(); return ok; }
        public bool DisplayCarToOpenSlot(string carKey) { bool ok = _showroom.DisplayToOpenSlot(carKey); if (ok) Changed(); return ok; }

        // ---------------- Race ----------------

        public bool IsRaceUnlocked => _circuit.Unlocked;
        public long RaceAgencyCost => Economy.RaceAgencyCost;

        public bool BuyRaceAgency()
        {
            if (_circuit.Unlocked) return false;
            if (!Pay(Economy.RaceAgencyCost)) return false;
            _state.ownedSpecialUnlocks.Add(CircuitRaceSystem.RaceAgencyId);
            Changed();
            return true;
        }

        public bool StartRace(string carKey, int trackIndex)
        {
            bool ok = _circuit.StartRace(carKey, trackIndex);
            if (!ok) Error("Can't start race. Check your car, the entry fee, or the cooldown.");
            else Changed();
            return ok;
        }

        public void SkipRaceToResult() { _circuit.SkipToResult(); Changed(); }
        public void DismissRaceResult() { _circuit.DismissResult(); Changed(); }

        // ---- dedicated race-car production line ----
        public bool CanBuildRaceCar(string raceCarId) => _workshop.CanBuild(raceCarId);

        public bool BuildRaceCar(string raceCarId)
        {
            bool ok = _workshop.BuildRaceCar(raceCarId);
            if (!ok) Error("Can't build race car. Check parts, cash, factory tier, or unlock the Race Agency.");
            else Changed();
            return ok;
        }

        public long ShowroomSlotUnlockCost()
        {
            int unlocked = 0;
            var slots = _state.showroom.slots;
            for (int i = 0; i < slots.Count; i++) if (slots[i].unlocked) unlocked++;
            return Economy.ShowroomSlotUnlockCost(unlocked);
        }
        public long ShowroomLevelCost() => Economy.ShowroomLevelCost(_state.showroom.level);
        public long ShowroomMarketingCost() => Economy.ShowroomMarketingCost(_state.showroom.marketingLevel);
        public long ShowroomDecorCost() => Economy.ShowroomDecorCost(_state.showroom.decorLevel);

        // ---------------- Developer / testing tools ----------------
        // Wired to the in-scene Dev Menu (CFI > Build > Dev Menu). Delete that object to ship clean.

        public void DevAddCash(long amount)
        {
            _state.wallet.AddCash(amount);
            _bus?.CashChanged(_state.wallet.cash);
            Changed();
        }

        public void DevAddTrophies(long amount)
        {
            _state.wallet.AddTrophies(amount);
            _bus?.TrophiesChanged(_state.wallet.trophies);
            Changed();
        }

        public void DevAddPrestige(int amount) { _state.prestigePoints += amount; Changed(); }

        public void DevUnlockRaceAgency()
        {
            if (!_state.ownedSpecialUnlocks.Contains(CircuitRaceSystem.RaceAgencyId))
                _state.ownedSpecialUnlocks.Add(CircuitRaceSystem.RaceAgencyId);
            Changed();
        }

        public void DevFillMaterials(long each)
        {
            foreach (var it in _cfg.items) if (it != null) _state.inventory.Add(it.id, each);
            Changed();
        }

        public void DevGiveSampleCars()
        {
            foreach (var v in _cfg.vehicles)
                if (v != null) _state.inventory.Add(CarKey.Build(v.id, Grade.S), 1);
            Changed();
        }

        public void DevGiveRaceCars()
        {
            foreach (var rc in _cfg.raceCars) if (rc != null) _state.inventory.Add(rc.id, 1);
            Changed();
        }

        public void DevUnlockAllStations()
        {
            for (int i = 0; i < _state.stations.Count; i++)
            { _state.stations[i].unlocked = true; _state.stations[i].automated = true; }
            Changed();
        }

        public void DevMaxFactoryTier()
        {
            _state.factoryTierIndex = Math.Max(0, _cfg.factoryTiers.Count - 1);
            Changed();
        }

        public void DevClearRaceCooldown() { _state.raceLive.cooldownRemaining = 0f; Changed(); }

        // ---------------- helpers ----------------

        private bool Pay(long cost)
        {
            if (!_state.wallet.SpendCash(cost)) { Error("Not enough cash!"); return false; }
            _bus?.CashChanged(_state.wallet.cash);
            return true;
        }

        private void Error(string msg) => _bus?.Error(msg);
        private void Changed() => _bus?.stateChanged?.Raise();
    }
}
