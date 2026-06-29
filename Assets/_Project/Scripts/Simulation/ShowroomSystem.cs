using System;
using System.Collections.Generic;
using CarFactoryIdle.Data;
using CarFactoryIdle.Events;
using CarFactoryIdle.State;

namespace CarFactoryIdle.Simulation
{
    /// <summary>Runs the dealership floor. Flat-price slots attract walk-in visitors who make offers;
    /// "Send to Auction" routes a displayed car into the single shared auction lane (AuctionSystem).
    /// A car is consumed out of inventory the moment it goes on display and only pays out at point of
    /// sale, so the same unit can never be sold twice. The newest assembled car auto-fills the first
    /// open unlocked slot. All purchases are atomic (check funds, then apply).</summary>
    public class ShowroomSystem
    {
        public const float BaseVisitorInterval = 8f; // seconds between visitors at level 1, no marketing
        public const float OfferTtl = 7f;            // how long a standing offer lasts

        private readonly GameConfig _cfg;
        private readonly GameState _state;
        private readonly GameEventBus _bus;
        private readonly AuctionSystem _auction;
        private readonly Random _rng;

        public ShowroomSystem(GameConfig cfg, GameState state, GameEventBus bus, AuctionSystem auction, Random rng)
        { _cfg = cfg; _state = state; _bus = bus; _auction = auction; _rng = rng ?? new Random(); }

        private ShowroomState S => _state.showroom;

        /// <summary>Seed the default 4 spots (first unlocked) on a new or migrated save.</summary>
        public void EnsureInitialized()
        {
            if (S.slots == null) S.slots = new List<ShowroomSlot>();
            if (S.slots.Count == 0)
                for (int i = 0; i < 4; i++)
                    S.slots.Add(new ShowroomSlot { unlocked = i == 0, mode = SaleMode.Flat });
        }

        public void Tick(float dt)
        {
            // Auto-fill the first open unlocked slot with the most recently built car.
            if (!string.IsNullOrEmpty(_state.lastAssembledCarKey))
            {
                var open = FirstOpenSlot();
                if (open != null && _state.inventory.Get(_state.lastAssembledCarKey) > 0)
                {
                    _state.inventory.TryConsume(_state.lastAssembledCarKey, 1);
                    PlaceInSlot(open, _state.lastAssembledCarKey);
                    _state.lastAssembledCarKey = null;
                }
            }

            for (int i = 0; i < S.slots.Count; i++)
            {
                var slot = S.slots[i];
                if (!slot.unlocked || slot.IsEmpty || slot.mode != SaleMode.Flat) continue;
                TickFlatSlot(slot, i, dt);
            }
        }

        private void TickFlatSlot(ShowroomSlot slot, int index, float dt)
        {
            if (slot.currentOffer > 0)
            {
                slot.offerExpiresIn -= dt;
                if (slot.offerExpiresIn <= 0f)
                {
                    slot.currentOffer = 0;
                    slot.nextVisitorIn = VisitorInterval();
                }
                return;
            }

            slot.nextVisitorIn -= dt;
            if (slot.nextVisitorIn <= 0f)
            {
                slot.currentOffer = GenerateOffer(slot);
                slot.offerExpiresIn = OfferTtl;
                if (slot.autoAccept && slot.currentOffer >= slot.autoAcceptThreshold)
                    AcceptOffer(index);
            }
        }

        private long GenerateOffer(ShowroomSlot slot)
        {
            // Visitors offer ~75-95% of asking, lifted by decor level.
            double frac = 0.75 + 0.04 * S.decorLevel + _rng.NextDouble() * 0.20;
            long offer = (long)Math.Floor(slot.askingPrice * frac);
            return offer < 1 ? 1 : offer;
        }

        private float VisitorInterval()
        {
            float mult = 1f + 0.15f * S.marketingLevel + 0.08f * (S.level - 1);
            return BaseVisitorInterval / Math.Max(0.0001f, mult);
        }

        // ---------------- player intents ----------------

        public bool DisplayCar(int slotIndex, string carKey)
        {
            var slot = Slot(slotIndex);
            if (slot == null || !slot.unlocked || string.IsNullOrEmpty(carKey)) return false;
            if (!slot.IsEmpty) ReturnCar(slot);            // swap out the current car first
            if (!_state.inventory.TryConsume(carKey, 1)) { _bus?.Error("That car isn't in your garage."); return false; }
            PlaceInSlot(slot, carKey);
            return true;
        }

        /// <summary>Display a car into the first open unlocked slot (used by the garage list).</summary>
        public bool DisplayToOpenSlot(string carKey)
        {
            if (string.IsNullOrEmpty(carKey)) return false;
            if (_state.inventory.Get(carKey) <= 0) { _bus?.Error("That car isn't in your garage."); return false; }
            for (int i = 0; i < S.slots.Count; i++)
            {
                if (S.slots[i].unlocked && S.slots[i].IsEmpty)
                    return DisplayCar(i, carKey);
            }
            _bus?.Error("No open display slot. Clear or unlock one first.");
            return false;
        }

        public bool ClearSlot(int slotIndex)
        {
            var slot = Slot(slotIndex);
            if (slot == null || slot.IsEmpty) return false;
            ReturnCar(slot);
            return true;
        }

        public bool SetSaleMode(int slotIndex, SaleMode mode)
        {
            var slot = Slot(slotIndex);
            if (slot == null) return false;
            slot.mode = mode;
            slot.currentOffer = 0;
            slot.nextVisitorIn = VisitorInterval();
            return true;
        }

        public bool SetAskingPrice(int slotIndex, long price)
        {
            var slot = Slot(slotIndex);
            if (slot == null || price < 0) return false;
            slot.askingPrice = price;
            return true;
        }

        public bool SetAutoAccept(int slotIndex, bool on, long threshold)
        {
            var slot = Slot(slotIndex);
            if (slot == null) return false;
            slot.autoAccept = on;
            slot.autoAcceptThreshold = threshold < 0 ? 0 : threshold;
            return true;
        }

        public bool AcceptOffer(int slotIndex)
        {
            var slot = Slot(slotIndex);
            if (slot == null || slot.IsEmpty || slot.currentOffer <= 0) return false;
            long amount = slot.currentOffer;
            string key = slot.carKey;
            _state.wallet.AddCash(amount);
            _bus?.CashChanged(_state.wallet.cash);
            _bus?.CarSold(new CarSoldEvent(key, amount, false));
            EmptySlot(slot);
            return true;
        }

        public bool DeclineOffer(int slotIndex)
        {
            var slot = Slot(slotIndex);
            if (slot == null || slot.currentOffer <= 0) return false;
            slot.currentOffer = 0;
            slot.nextVisitorIn = VisitorInterval();
            return true;
        }

        /// <summary>Move a displayed car into the single shared auction lane with an optional buyout.</summary>
        public bool SendToAuction(int slotIndex, long buyout)
        {
            var slot = Slot(slotIndex);
            if (slot == null || slot.IsEmpty) return false;
            if (_state.auction.active) { _bus?.Error("The auction lane is busy."); return false; }
            string key = slot.carKey;
            // Hand the car back to inventory so the auction lane can own + consume it on settle.
            _state.inventory.Add(key, 1);
            if (!_auction.StartAuction(key, buyout)) { _state.inventory.TryConsume(key, 1); return false; }
            EmptySlot(slot);
            return true;
        }

        // ---------------- upgrades ----------------

        public bool UnlockSlot(int slotIndex)
        {
            var slot = Slot(slotIndex);
            if (slot == null || slot.unlocked) return false;
            if (!Pay(Economy.ShowroomSlotUnlockCost(UnlockedSlotCount()))) return false;
            slot.unlocked = true;
            return true;
        }

        public bool UpgradeLevel()
        {
            if (S.level >= Economy.MaxShowroomLevel) return false;
            if (!Pay(Economy.ShowroomLevelCost(S.level))) return false;
            S.level++;
            return true;
        }

        public bool UpgradeMarketing()
        {
            if (S.marketingLevel >= Economy.MaxShowroomTrackLevel) return false;
            if (!Pay(Economy.ShowroomMarketingCost(S.marketingLevel))) return false;
            S.marketingLevel++;
            return true;
        }

        public bool UpgradeDecor()
        {
            if (S.decorLevel >= Economy.MaxShowroomTrackLevel) return false;
            if (!Pay(Economy.ShowroomDecorCost(S.decorLevel))) return false;
            S.decorLevel++;
            return true;
        }

        // ---------------- helpers ----------------

        private void PlaceInSlot(ShowroomSlot slot, string carKey)
        {
            slot.carKey = carKey;
            slot.askingPrice = _auction.GradedBasePrice(carKey); // default ask = fair graded value
            slot.currentOffer = 0;
            slot.offerExpiresIn = 0f;
            slot.nextVisitorIn = VisitorInterval();
        }

        private void ReturnCar(ShowroomSlot slot)
        {
            if (!slot.IsEmpty) _state.inventory.Add(slot.carKey, 1);
            EmptySlot(slot);
        }

        private void EmptySlot(ShowroomSlot slot)
        {
            slot.carKey = null;
            slot.currentOffer = 0;
            slot.offerExpiresIn = 0f;
            slot.nextVisitorIn = 0f;
        }

        private ShowroomSlot FirstOpenSlot()
        {
            for (int i = 0; i < S.slots.Count; i++)
                if (S.slots[i].unlocked && S.slots[i].IsEmpty) return S.slots[i];
            return null;
        }

        private int UnlockedSlotCount()
        {
            int n = 0;
            for (int i = 0; i < S.slots.Count; i++) if (S.slots[i].unlocked) n++;
            return n;
        }

        private ShowroomSlot Slot(int index)
            => (index >= 0 && index < S.slots.Count) ? S.slots[index] : null;

        private bool Pay(long cost)
        {
            if (!_state.wallet.SpendCash(cost)) { _bus?.Error("Not enough cash!"); return false; }
            _bus?.CashChanged(_state.wallet.cash);
            return true;
        }
    }
}
