using System;
using CarFactoryIdle.Data;
using CarFactoryIdle.Events;
using CarFactoryIdle.State;

namespace CarFactoryIdle.Simulation
{
    /// <summary>30-second auction loop. NPC bids arrive every 3-7s and bump the price by
    /// +5% to +20% compounding; auto-accepts the highest bid at 0s. Instant Sell pays the
    /// graded base (the floor) immediately.</summary>
    public class AuctionSystem
    {
        public const float AuctionDuration = 30f;

        private readonly GameConfig _cfg;
        private readonly GameState _state;
        private readonly GameEventBus _bus;
        private readonly Random _rng;

        public AuctionSystem(GameConfig cfg, GameState state, GameEventBus bus, Random rng)
        { _cfg = cfg; _state = state; _bus = bus; _rng = rng ?? new Random(); }

        public long GradedBasePrice(string carKey)
        {
            if (!CarKey.TryParse(carKey, out var vehicleId, out var grade)) return 0;
            var v = _cfg.GetVehicle(vehicleId);
            if (v == null) return 0;
            return (long)Math.Floor(v.basePrice * _cfg.gachaTable.GetMultiplier(grade));
        }

        public bool StartAuction(string carKey, long buyout = 0)
        {
            var a = _state.auction;
            if (a.active) return false;
            if (_state.inventory.Get(carKey) <= 0) return false;
            long basePrice = GradedBasePrice(carKey);
            a.active = true;
            a.carKey = carKey;
            a.startingBid = basePrice;
            a.currentBid = basePrice;
            a.timeRemaining = AuctionDuration;
            a.nextBidIn = NextBidInterval();
            a.bidCount = 0;
            a.buyout = buyout < 0 ? 0 : buyout;
            return true;
        }

        public void Tick(float dt)
        {
            var a = _state.auction;
            if (!a.active) return;
            a.timeRemaining -= dt;
            a.nextBidIn -= dt;
            while (a.nextBidIn <= 0f && a.timeRemaining > 0f)
            {
                ApplyBid(a);
                a.nextBidIn += NextBidInterval();
                if (a.buyout > 0 && a.currentBid >= a.buyout) { Settle(a.buyout, true); return; }
            }
            if (a.timeRemaining <= 0f)
                Settle(a.currentBid, true);
        }

        public void InstantSell()
        {
            var a = _state.auction;
            if (!a.active) return;
            Settle(a.startingBid, false);
        }

        private void ApplyBid(AuctionState a)
        {
            float factor = 1.05f + (float)_rng.NextDouble() * 0.15f; // +5%..+20%
            a.currentBid = (long)Math.Floor(a.currentBid * factor);
            a.bidCount++;
        }

        private void Settle(long amount, bool viaAuction)
        {
            var a = _state.auction;
            if (_state.inventory.TryConsume(a.carKey, 1))
            {
                _state.wallet.AddCash(amount);
                _bus?.CashChanged(_state.wallet.cash);
                _bus?.CarSold(new CarSoldEvent(a.carKey, amount, viaAuction));
            }
            a.active = false;
            a.carKey = null;
            a.currentBid = 0;
            a.startingBid = 0;
            a.timeRemaining = 0;
            a.bidCount = 0;
            a.buyout = 0;
        }

        private float NextBidInterval() => 3f + (float)_rng.NextDouble() * 4f; // 3-7s
    }
}
