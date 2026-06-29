using System;

namespace CarFactoryIdle.State
{
    /// <summary>Mutable, saved per-station progress. Pairs with a StationDefinition by id.</summary>
    [Serializable]
    public class StationState
    {
        public string definitionId;
        public bool unlocked;
        public int speedLevel;       // 0..25
        public int capacityLevel;    // 0..10
        public bool automated;
        public float progress;       // seconds accumulated toward next cycle
    }
}
