using System;
using System.Collections.Generic;
using CarFactoryIdle.Data;
using CarFactoryIdle.Events;
using CarFactoryIdle.State;

namespace CarFactoryIdle.Simulation
{
    /// <summary>Rolls up to 5 contracts, refreshing every 120s; max 3 active. Quantities, payouts,
    /// timers and trophy rewards scale with factory tier per ContractTypeDefinition.</summary>
    public class ContractSystem
    {
        public const float RefreshSeconds = 120f;
        public const int MaxActive = 3;
        public const int MaxPool = 5;

        private readonly GameConfig _cfg;
        private readonly GameState _state;
        private readonly GameEventBus _bus;
        private readonly Random _rng;

        public ContractSystem(GameConfig cfg, GameState state, GameEventBus bus, Random rng)
        { _cfg = cfg; _state = state; _bus = bus; _rng = rng ?? new Random(); }

        public void Tick(float dt)
        {
            // Rush countdowns
            for (int i = _state.contracts.Count - 1; i >= 0; i--)
            {
                var c = _state.contracts[i];
                if (!c.hasTimer) continue;
                c.timeRemaining -= dt;
                if (c.timeRemaining <= 0f) _state.contracts.RemoveAt(i);
            }

            _state.contractRefreshTimer -= dt;
            if (_state.contractRefreshTimer <= 0f)
            {
                _state.contractRefreshTimer = RefreshSeconds;
                Refresh();
            }
        }

        public void Refresh()
        {
            int target = Math.Min(MaxPool, MaxActive);
            while (_state.contracts.Count < target)
            {
                var rolled = RollOne();
                if (rolled == null) break;
                _state.contracts.Add(rolled);
            }
            _bus?.contractsRefreshed?.Raise();
        }

        private ContractState RollOne()
        {
            int tier = _state.factoryTierIndex + 1;
            var eligible = new List<ContractTypeDefinition>();
            foreach (var t in _cfg.contractTypes)
                if (t != null && tier >= t.minFactoryTier) eligible.Add(t);
            if (eligible.Count == 0) return null;

            var def = eligible[_rng.Next(eligible.Count)];
            var vehicle = PickVehicleForTier(tier);
            if (vehicle == null) return null;

            int maxQty = Math.Max(def.minQty, def.maxQtyBase + tier * def.maxQtyPerTier);
            int qty = _rng.Next(def.minQty, maxQty + 1);
            long cash = (long)Math.Floor(def.payoutMultiplier * vehicle.basePrice * qty);
            int trophies = 0;
            if (def.trophyMax > 0)
                trophies = _rng.Next(def.trophyMin, def.trophyMax + 1) + tier * def.trophyPerTier;
            bool hasTimer = def.baseTimerSeconds > 0f || def.timerPerTier > 0f;
            float timer = def.baseTimerSeconds + tier * def.timerPerTier;

            return new ContractState
            {
                type = def.type,
                vehicleId = vehicle.id,
                quantity = qty,
                cashReward = cash,
                trophyReward = trophies,
                hasTimer = hasTimer,
                timeRemaining = timer
            };
        }

        private VehicleDefinition PickVehicleForTier(int tier)
        {
            var pool = new List<VehicleDefinition>();
            foreach (var v in _cfg.vehicles)
                if (v != null && v.tier <= tier) pool.Add(v);
            if (pool.Count == 0) return null;
            return pool[_rng.Next(pool.Count)];
        }

        /// <summary>Fulfills a contract using cars of any grade of the required vehicle.</summary>
        public bool Fulfill(int contractIndex)
        {
            if (contractIndex < 0 || contractIndex >= _state.contracts.Count) return false;
            var c = _state.contracts[contractIndex];
            int remaining = c.quantity;
            var toConsume = new List<string>();
            foreach (Grade g in Enum.GetValues(typeof(Grade)))
            {
                if (remaining <= 0) break;
                string key = CarKey.Build(c.vehicleId, g);
                long have = _state.inventory.Get(key);
                long take = Math.Min(have, remaining);
                for (int i = 0; i < take; i++) toConsume.Add(key);
                remaining -= (int)take;
            }
            if (remaining > 0) { _bus?.Error("Not enough cars for this contract"); return false; }

            foreach (var key in toConsume) _state.inventory.TryConsume(key, 1);
            if (c.cashReward > 0) { _state.wallet.AddCash(c.cashReward); _bus?.CashChanged(_state.wallet.cash); }
            if (c.trophyReward > 0) { _state.wallet.AddTrophies(c.trophyReward); _bus?.TrophiesChanged(_state.wallet.trophies); }
            _state.contracts.RemoveAt(contractIndex);

            string reward = c.cashReward > 0 ? $"+{CarFactoryIdle.Core.NumberFormat.Currency(c.cashReward)}" : "";
            if (c.trophyReward > 0) reward += (reward.Length > 0 ? "  " : "") + $"+{c.trophyReward} trophies";
            _bus?.Toast($"Contract complete! {reward}".TrimEnd(), Events.ToastKind.Success);
            return true;
        }
    }
}
