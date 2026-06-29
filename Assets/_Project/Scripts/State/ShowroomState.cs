using System;
using System.Collections.Generic;

namespace CarFactoryIdle.State
{
    /// <summary>How a displayed car is being sold.</summary>
    public enum SaleMode { Flat = 0, Auction = 1 }

    /// <summary>One dealership display spot. Holds a single graded car (consumed out of inventory
    /// while on display) plus its sale settings. Flat-mode slots attract walk-in visitors who make
    /// offers; Auction-mode routes the car to the single shared auction lane (AuctionState).</summary>
    [Serializable]
    public class ShowroomSlot
    {
        public bool unlocked;
        public string carKey;          // null/empty when the spot is empty
        public SaleMode mode = SaleMode.Flat;
        public long askingPrice;       // player-set; defaults to the car's graded value

        // Flat-sale walk-in offer state.
        public long currentOffer;      // 0 when no visitor is currently offering
        public float offerExpiresIn;   // seconds the standing offer remains
        public float nextVisitorIn;    // seconds until the next visitor arrives

        // Optional idle automation.
        public bool autoAccept;
        public long autoAcceptThreshold;

        public bool IsEmpty => string.IsNullOrEmpty(carKey);
    }

    /// <summary>Persistent Showroom state: display slots + three upgrade tracks
    /// (level/prestige, marketing-traffic, decor-offer-quality). The live auction is stored
    /// separately in AuctionState (one shared lane).</summary>
    [Serializable]
    public class ShowroomState
    {
        public int level = 1;          // overall showroom level (traffic + prestige)
        public int marketingLevel = 0; // visitor arrival rate
        public int decorLevel = 0;     // offer quality multiplier
        public List<ShowroomSlot> slots = new();
    }
}
