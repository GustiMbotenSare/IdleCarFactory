using System;

namespace CarFactoryIdle.State
{
    /// <summary>The single active auction. One car on the block at a time (see answers #3).</summary>
    [Serializable]
    public class AuctionState
    {
        public bool active;
        public string carKey;       // e.g. "autobahn911_A"
        public long startingBid;    // graded base price (also the Instant Sell price)
        public long currentBid;
        public float timeRemaining; // counts down from 30s
        public float nextBidIn;     // seconds until next NPC bid (3-7s)
        public int bidCount;
        public long buyout;         // optional instant-buy price set from the showroom (0 = none)
    }
}
