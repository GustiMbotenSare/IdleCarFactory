using System;
using System.Collections.Generic;

namespace CarFactoryIdle.State
{
    /// <summary>One car in a live race. Reference type on purpose: the sim mutates these in place
    /// each tick. Plain serializable data so a race in progress survives save/load and keeps running
    /// in the background while the player is on another screen.</summary>
    [Serializable]
    public class RaceEntrant
    {
        public string name;
        public bool isPlayer;
        public string carKey;        // player's inventory key; empty for AI rivals

        // Derived sim stats (VehicleDefinition.baseRaceStats * grade multiplier).
        public float topSpeed;       // -> max unit speed
        public float acceleration;   // -> how fast it reaches target speed
        public float handling;       // -> speed kept through corners (was "traction")
        public float aura;           // -> boost / flaming-aura proc chance (was "launch")

        // Live race progress.
        public float distance;       // total meters travelled
        public float speed;          // current unit speed
        public int lap;              // laps completed
        public bool finished;
        public float finishTime;
        public int place;            // 1-based standing
        public float auraTimer;      // remaining boost seconds
        public bool auraActive;
    }

    /// <summary>The single live circuit race. Ticked every frame by GameRoot regardless of which
    /// screen is showing, so the player can wander off and come back. Only one race at a time.</summary>
    [Serializable]
    public class RaceState
    {
        public bool active;
        public bool finished;

        public int trackIndex;
        public string trackName;
        public float lapDistance;
        public int lapsTotal;
        public float elapsed;
        public float cooldownRemaining;

        public List<RaceEntrant> entrants = new();

        // Result payload (filled on finish).
        public int playerPlace;
        public long cashReward;
        public int trophyReward;
        public int prestigeReward;
        public bool rewardClaimed;
        public string resultMessage;
    }
}
