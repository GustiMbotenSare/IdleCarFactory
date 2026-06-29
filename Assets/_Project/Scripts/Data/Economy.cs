using System;

namespace CarFactoryIdle.Data
{
    /// <summary>Central, designer-confirmed economy formulas. Pure functions, no state.
    /// Values locked from the Godot prototype data tables (see claude_answers #6/#7).</summary>
    public static class Economy
    {
        public const int MaxSpeedLevel = 25;
        public const int MaxCapacityLevel = 10;

        public const float SpeedStepPerLevel = 0.10f;   // +10% speed per level
        public const int CapacityStepPerLevel = 1;       // +1 output per cycle per level

        // Speed upgrade: floor(100 * 1.15^currentLevel)
        public static long SpeedUpgradeCost(int currentLevel)
            => (long)Math.Floor(100.0 * Math.Pow(1.15, currentLevel));

        // Capacity upgrade: floor(500 * 1.40^currentLevel)
        public static long CapacityUpgradeCost(int currentLevel)
            => (long)Math.Floor(500.0 * Math.Pow(1.40, currentLevel));

        /// <summary>baseInterval / ((1 + 0.10 * speedLevel) * factoryMultiplier)</summary>
        public static float EffectiveInterval(float baseInterval, int speedLevel, float factoryMultiplier)
        {
            float denom = (1f + SpeedStepPerLevel * speedLevel) * Math.Max(0.0001f, factoryMultiplier);
            return baseInterval / denom;
        }

        /// <summary>baseOutput + (1 * capacityLevel)</summary>
        public static int EffectiveOutput(int baseOutput, int capacityLevel)
            => baseOutput + CapacityStepPerLevel * capacityLevel;

        // ---- Race ----
        /// <summary>One-time purchase that unlocks the Race section (the "Race Agency").</summary>
        public const long RaceAgencyCost = 75000;

        // ---- Showroom ----
        public const int MaxShowroomLevel = 10;
        public const int MaxShowroomTrackLevel = 20;

        /// <summary>Unlock the next display spot: 50k, 150k, 450k ... (x3 per slot already unlocked).</summary>
        public static long ShowroomSlotUnlockCost(int unlockedSlots)
            => (long)Math.Floor(50000.0 * Math.Pow(3.0, Math.Max(0, unlockedSlots - 1)));

        /// <summary>Showroom level (traffic + prestige): floor(15000 * 1.7^(level-1)).</summary>
        public static long ShowroomLevelCost(int currentLevel)
            => (long)Math.Floor(15000.0 * Math.Pow(1.7, Math.Max(0, currentLevel - 1)));

        /// <summary>Marketing (visitor rate): floor(4000 * 1.6^level).</summary>
        public static long ShowroomMarketingCost(int currentLevel)
            => (long)Math.Floor(4000.0 * Math.Pow(1.6, currentLevel));

        /// <summary>Decor / sales staff (offer quality): floor(5000 * 1.6^level).</summary>
        public static long ShowroomDecorCost(int currentLevel)
            => (long)Math.Floor(5000.0 * Math.Pow(1.6, currentLevel));
    }
}
