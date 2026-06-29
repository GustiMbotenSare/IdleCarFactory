using System;
using CarFactoryIdle.Data;

namespace CarFactoryIdle.State
{
    /// <summary>A concrete, rolled contract currently on the board.</summary>
    [Serializable]
    public class ContractState
    {
        public ContractType type;
        public string vehicleId;     // required vehicle (any grade satisfies, unless extended)
        public int quantity;
        public long cashReward;
        public int trophyReward;
        public bool hasTimer;
        public float timeRemaining;  // only meaningful when hasTimer
    }
}
